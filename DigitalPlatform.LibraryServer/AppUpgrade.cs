using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Net.Mail;
using System.Web;

using DigitalPlatform;	// Stop��
using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Marc;
using DigitalPlatform.Range;

using DigitalPlatform.Message;
using DigitalPlatform.rms.Client.rmsws_localhost;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// �������Ǻ� ��dt1000���� ��صĴ���
    /// </summary>
    public partial class LibraryApplication
    {
        long m_lIdSeed = 0;

        // ���մ�dt1000���������Ķ��ߺ�ʵ���¼���н��洦��
        // parameters:
        //      nStart      �ӵڼ������ĵĲ����ʼ����
        //      nCount      �����������ĵĲ�����
        //      nProcessedBorrowItems   [out]���δ����˶��ٸ����Ĳ�����
        //      nTotalBorrowItems   [out]��ǰ����һ�������ж��ٸ����Ĳ�����
        // result.Value
        //      -1  ����
        //      0   �ɹ���
        //      1   �о���
        public LibraryServerResult CrossRefBorrowInfo(
            RmsChannelCollection Channels,
            string strReaderBarcode,
            int nStart,
            int nCount,
            out int nProcessedBorrowItems,
            out int nTotalBorrowItems)
        {
            string strError = "";
            nTotalBorrowItems = 0;
            nProcessedBorrowItems = 0;

            int nRet = 0;
            string strWarning = "";
            int nRedoCount = 0;

            // string strCheckError = "";

            LibraryServerResult result = new LibraryServerResult();
            // int nErrorCount = 0;

        REDO_CHANGE_READERREC:

            // �Ӷ��߼�¼��
#if DEBUG_LOCK_READER
            this.WriteErrorLog("CrossRefBorrowInfo ��ʼΪ���߼�д�� '" + strReaderBarcode + "'");
#endif
            this.ReaderLocks.LockForWrite(strReaderBarcode);

            try // ���߼�¼������Χ��ʼ
            {
                // ������߼�¼
                string strReaderXml = "";
                string strOutputReaderRecPath = "";
                byte[] reader_timestamp = null;
                nRet = this.GetReaderRecXml(
                    Channels,
                    strReaderBarcode,
                    out strReaderXml,
                    out strOutputReaderRecPath,
                    out reader_timestamp,
                    out strError);
                if (nRet == 0)
                {
                    result.Value = -1;
                    result.ErrorInfo = "����֤����� '" + strReaderBarcode + "' ������";
                    result.ErrorCode = ErrorCode.ReaderBarcodeNotFound;
                    return result;
                }
                if (nRet == -1)
                {
                    strError = "������߼�¼ʱ��������: " + strError;
                    goto ERROR1;
                }

                XmlDocument readerdom = null;
                nRet = LibraryApplication.LoadToDom(strReaderXml,
                    out readerdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                    goto ERROR1;
                }

                bool bReaderRecChanged = false;

                // �޸Ķ��߼�¼��overdues/overdue�еļ۸�λ��������id
                // return:
                //      -1  error
                //      0   not changed
                //      1   changed
                nRet = ModifyReaderRecord(
                    ref readerdom,
                    out strWarning,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 1)
                    bReaderRecChanged = true;

                // TODO: strWarning������δ���
                XmlNodeList nodesBorrow = readerdom.DocumentElement.SelectNodes("borrows/borrow");

                nTotalBorrowItems = nodesBorrow.Count;

                if (nTotalBorrowItems == 0)
                {
                    result.Value = 0;
                    result.ErrorInfo = "���߼�¼��û�н軹��Ϣ��";
                    return result;
                }

                if (nStart >= nTotalBorrowItems)
                {
                    strError = "nStart����ֵ" + nStart.ToString() + "���ڵ�ǰ���߼�¼�еĽ��Ĳ����" + nTotalBorrowItems.ToString();
                    goto ERROR1;
                }

                nProcessedBorrowItems = 0;
                for (int i = nStart; i < nTotalBorrowItems; i++)
                {
                    if (nCount != -1 && nProcessedBorrowItems >= nCount)
                        break;

                    // һ��API�����10��
                    if (nProcessedBorrowItems >= 10)
                        break;

                    XmlNode nodeBorrow = nodesBorrow[i];

                    string strItemBarcode = DomUtil.GetAttr(nodeBorrow, "barcode");

                    nProcessedBorrowItems++;

                    if (String.IsNullOrEmpty(strItemBarcode) == true)
                    {
                        strWarning += "���߼�¼��<borrow>Ԫ��barcode����ֵ����Ϊ��; ";
                        continue;
                    }

                    string strBorrowDate = DomUtil.GetAttr(nodeBorrow, "borrowDate");
                    string strBorrowPeriod = DomUtil.GetAttr(nodeBorrow, "borrowPeriod");

                    if (String.IsNullOrEmpty(strBorrowDate) == true)
                    {
                        strWarning += "���߼�¼��<borrow>Ԫ��borrowDate���Բ���Ϊ��; ";
                        continue;
                    }


                    if (String.IsNullOrEmpty(strBorrowPeriod) == true)
                    {
                        strWarning += "���߼�¼��<borrow>Ԫ��borrowPeriod���Բ���Ϊ��; ";
                        continue;
                    }

                    // ��ʵ���¼������Ϣ��ϸ��
                    // return:
                    //      0   �������û���ҵ���Ӧ�Ĳ��¼
                    //      1   �ɹ�
                    nRet = ModifyEntityRecord(
                        Channels,
                        null,   // strEntityRecPath
                        strItemBarcode,
                        strReaderBarcode,
                        strBorrowDate,
                        strBorrowPeriod,
                        out strError);
                    if (nRet == -1)
                    {
                        strWarning += "ModifyEntityRecord() [strItemBarcode='" + strItemBarcode + "' strReaderBarcode='" + strReaderBarcode + "'] error : " + strError + "; ";
                        continue;
                    }

                    // 2008/10/7
                    if (nRet == 0)
                    {
                        strWarning += "������� '" + strItemBarcode + "' ��Ӧ�ļ�¼������; ";
                        continue;
                    }
                }


                if (bReaderRecChanged == true)
                {
                    byte[] output_timestamp = null;
                    string strOutputPath = "";

                    RmsChannel channel = Channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "get channel error";
                        goto ERROR1;
                    }

                    // д�ض��߼�¼
                    long lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                        readerdom.OuterXml,
                        false,
                        "content",  // ,ignorechecktimestamp
                        reader_timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            nRedoCount++;
                            if (nRedoCount > 10)
                            {
                                strError = "д�ض��߼�¼��ʱ��,����ʱ�����ͻ,���������10��,��ʧ��...";
                                goto ERROR1;
                            }
                            goto REDO_CHANGE_READERREC;
                        }
                        goto ERROR1;
                    }

                    // ��ʱ����ʱ���
                    reader_timestamp = output_timestamp;
                }


            }
            finally
            {
                this.ReaderLocks.UnlockForWrite(strReaderBarcode);
#if DEBUG_LOCK_READER
                this.WriteErrorLog("CrossRefBorrowInfo ����Ϊ���߼�д�� '" + strReaderBarcode + "'");
#endif
            }

            if (String.IsNullOrEmpty(strWarning) == false)
            {
                result.Value = 1;
                result.ErrorInfo = strWarning;
            }
            else
                result.Value = 0;

            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }


        // �޸Ķ��߼�¼��overdues/overdue�еļ۸�λ��������id
        // return:
        //      -1  error
        //      0   not changed
        //      1   changed
        int ModifyReaderRecord(
            ref XmlDocument readerdom,
            out string strWarning,
            out string strError)
        {
            strError = "";
            strWarning = "";

            // int nRet = 0;
            bool bChanged = false;

            // �г�����<overdue>�ڵ�
            XmlNodeList nodes = readerdom.DocumentElement.SelectNodes("overdues/overdue");

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strID = DomUtil.GetAttr(node, "id");

                // 2008/5/21
                if (String.IsNullOrEmpty(strID) == true)
                {
                    // ����id
                    strID = "upgrade_dt1000_" + this.m_lIdSeed.ToString();
                    this.m_lIdSeed++;
                    DomUtil.SetAttr(node, "id", strID);
                }

                /* 2008/5/21 commented
                if (string.IsNullOrEmpty(strID) == false)
                    continue;   // �¸�ʽ���������ˡ�
                 * */

                string strPrice = DomUtil.GetAttr(node, "price");

                if (String.IsNullOrEmpty(strPrice) == false
                    && StringUtil.IsPureNumber(strPrice) == true)
                {
                    // ֻ�д����ֲ���
                }
                else
                    continue;

                long lOldPrice = 0;

                try
                {
                    lOldPrice = Convert.ToInt64(strPrice);
                }
                catch
                {
                    strWarning += "�۸��ַ��� '' ��ʽ����ȷ��Ӧ��Ϊ�����֡�";
                    continue;
                }

                // ת��ΪԪ
                double dPrice = ((double)lOldPrice) / 100;

                strPrice = "CNY" + dPrice.ToString();   // +"yuan";

                DomUtil.SetAttr(node, "price", strPrice);

                // 2008/5/21
                string strType = DomUtil.GetAttr(node, "type");
                if (String.IsNullOrEmpty(strType) == false)
                    DomUtil.SetAttr(node, "reason", strType + "����dt1000��������");

                /*
                // ����id
                strID = "upgrade_dt1000_" + this.m_lIdSeed.ToString();
                this.m_lIdSeed++;

                DomUtil.SetAttr(node, "id", strID);

                string strReason = "����ΥԼ��(��dt1000��������)";

                DomUtil.SetAttr(node, "reason", strReason);
                 * */

                bChanged = true;
            }

            if (bChanged == true)
                return 1;

            return 0;
        }

        // ��ʵ���¼������Ϣ��ϸ��
        // parameters:
        //      strEntityRecPath    ���¼·�������������ֵΪ�գ����ʾϣ��ͨ��strItemBarcode�������ҵ����¼
        // return:
        //      -1  error
        //      0   �������û���ҵ���Ӧ�Ĳ��¼
        //      1   �ɹ�
        int ModifyEntityRecord(
            RmsChannelCollection Channels,
            string strEntityRecPath,
            string strItemBarcode,
            string strReaderBarcode,
            string strBorrowDate,
            string strBorrowPeriod,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            long lRet = 0;
            int nRedoCount = 0;

            if (String.IsNullOrEmpty(strItemBarcode) == true)
            {
                strError = "strItemBarcode��������Ϊ��";
                return -1;
            }

            RmsChannel channel = null;

            REDO_CHANGE:

            string strOutputItemRecPath = "";
            byte[] item_timestamp = null;


            string strItemXml = "";
            List<string> aPath = null;


            if (String.IsNullOrEmpty(strEntityRecPath) == false)
            {
                if (channel == null)
                {
                    channel = Channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "get channel error";
                        return -1;
                    }
                }

                string strStyle = "content,data,metadata,timestamp,outputpath";
                string strMetaData = "";
                lRet = channel.GetRes(strEntityRecPath,
                    strStyle,
                    out strItemXml,
                    out strMetaData,
                    out item_timestamp,
                    out strOutputItemRecPath,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.NotFound)
                    {
                        return 0;
                    }
                    strError = "ModifyEntityRecord()ͨ�����¼·�� '"+strEntityRecPath+"' ������¼ʱ��������: " + strError;
                    return -1;
                }
            }
            else
            {

                // �Ӳ�����Ż�ò��¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                nRet = this.GetItemRecXml(
                    Channels,
                    strItemBarcode,
                    out strItemXml,
                    100,
                    out aPath,
                    out item_timestamp,
                    out strError);
                if (nRet == 0)
                {
                    // ������Ų�����Ҳ����Ҫ�޸������֮һ��
                    return 0;
                }
                if (nRet == -1)
                {
                    strError = "ModifyEntityRecord()������¼ʱ��������: " + strError;
                    return -1;
                }

                if (aPath.Count > 1)
                {
                    // TODO: ��Ҫ����Χ�ļ�¼ȫ����ȡ������Ȼ��borrower���϶���֤����ŵ���һ��(���߶���?)
                    // ���Բο�UpgradeDt1000Loan�е�SearchEntityRecord()����
                    /*
                    strError = "�������� '" + strItemBarcode + "' �������ж������¼: " + StringUtil.MakePathList(aPath) + "���޸Ĳ��¼�Ĳ���ModifyEntityRecord()�޷�����";
                    return -1;
                     * */

                    int nSuccessCount = 0;
                    string strTempError = "";
                    // �ݹ�
                    for (int i = 0; i < aPath.Count; i++)
                    {
                        string strTempPath = aPath[i];

                        if (String.IsNullOrEmpty(strTempPath) == true)
                        {
                            Debug.Assert(false, "");
                            continue;
                        }

                        // return:
                        //      -1  error
                        //      0   �������û���ҵ���Ӧ�Ĳ��¼
                        //      1   �ɹ�
                        nRet = ModifyEntityRecord(
                            Channels,
                            strTempPath,
                            strItemBarcode,
                            strReaderBarcode,
                            strBorrowDate,
                            strBorrowPeriod,
                            out strError);
                        if (nRet == -1 && nSuccessCount == 0)
                        {
                            if (String.IsNullOrEmpty(strTempError) == false)
                                strTempError += "; ";
                            strTempError += "��̽���¼ '" + strTempPath + "' ʱ��������: " + strError;
                        }

                        if (nRet == 1)
                        {
                            // ��Ϊ�洢�ɹ���Ϣ
                            if (nSuccessCount == 0)
                                strTempError = "";

                            if (String.IsNullOrEmpty(strTempError) == false)
                                strTempError += "; ";
                            strTempError += strTempPath;

                            nSuccessCount++;
                        }
                    }

                    if (nSuccessCount > 0)
                    {
                        strError = "������� '" + strItemBarcode + "' ��������" + aPath.Count.ToString() + "�����¼: " + StringUtil.MakePathList(aPath) + "����������ǽ�����������̽���� " + nSuccessCount.ToString() + " ����¼����Ԥ�ڵ�Ҫ�󣬲��н�����Ϣ�õ���ǿ��������Ϣ�õ���ǿ�Ĳ��¼·������: " + strTempError;
                        return 1;
                    }
                    else
                    {
                        strError = "������� '" + strItemBarcode + "' ��������" + aPath.Count.ToString() + "�����¼: " + StringUtil.MakePathList(aPath) + "����������ǽ�����������̽������û��һ����¼����Ԥ�ڵ�Ҫ����̽���̱�������: " + strTempError;
                        return -1;
                    }

                    /*
                    result.Value = -1;
                    result.ErrorInfo = "�������Ϊ '" + strItemBarcode + "' �Ĳ��¼�� " + aPath.Count.ToString() + " �����޷������޸����������ڸ��Ӳ��¼·���������ύ�޸�������";
                    result.ErrorCode = ErrorCode.ItemBarcodeDup;

                    aDupPath = new string[aPath.Count];
                    aPath.CopyTo(aDupPath);
                    return result;
                     * */

                }
                else
                {
                    Debug.Assert(nRet == 1, "");
                    Debug.Assert(aPath.Count == 1, "");

                    if (nRet == 1)
                    {
                        strOutputItemRecPath = aPath[0];
                    }
                }
            }


            XmlDocument itemdom = null;
            nRet = LibraryApplication.LoadToDom(strItemXml,
                out itemdom,
                out strError);
            if (nRet == -1)
            {
                strError = "װ�ز��¼����XML DOMʱ��������: " + strError;
                return -1;
            }

            // У�������Ų����Ƿ��XML��¼����ȫһ��
            string strTempItemBarcode = DomUtil.GetElementText(itemdom.DocumentElement,
                "barcode");
            if (strItemBarcode != strTempItemBarcode)
            {
                strError = "�޸Ĳ��¼ModifyEntityRecord()�������ܾ����������Ų��� '" + strItemBarcode + "' �Ͳ��¼��<barcode>Ԫ���ڵĲ������ֵ '" + strTempItemBarcode + "' ��һ�¡�";
                return -1;
            }

            // �������¼���Ƿ���ָ�ض��߼�¼����
            string strBorrower = DomUtil.GetElementText(itemdom.DocumentElement,
                "borrower");
            if (strBorrower != strReaderBarcode)
            {
                // strError = "ModifyEntityRecord()�������ܾ�����������Ҫ�޸�����������һ��������ȷ��������ֱ�ӽ�����ͨ���������";
                strError = "�޸Ĳ��¼ModifyEntityRecord()�������ܾ�������¼ " + strOutputItemRecPath + " �е�[borrower]ֵ '" + strBorrower + "' �ͷ�Դ(���Ҳ������ '" + strItemBarcode + "')�Ķ���֤����� '" + strReaderBarcode + "' ��һ�£����ܹ���һ��������ȷ�������뼰ʱ�ų��˹��ϡ�";
                return -1;
            }


            // 2007/1/1ע��Ӧ��������¼��<borrower>Ԫ���Ƿ������ݲŸ�д<borrowDate>��<borrowPeriod>Ԫ�ء�

            DomUtil.SetElementText(itemdom.DocumentElement, "borrowDate", strBorrowDate);
            DomUtil.SetElementText(itemdom.DocumentElement, "borrowPeriod", strBorrowPeriod);

            if (channel == null)
            {
                channel = Channels.GetChannel(this.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    return -1;
                }
            }

            byte[] output_timestamp = null;
            string strOutputPath = "";

            // д�ز��¼
            lRet = channel.DoSaveTextRes(strOutputItemRecPath,
                itemdom.OuterXml,
                false,
                "content",  // ,ignorechecktimestamp
                item_timestamp,
                out output_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                {
                    nRedoCount++;
                    if (nRedoCount > 10)
                    {
                        strError = "ModifyEntityRecord()д�ز��¼��ʱ��,����ʱ�����ͻ,���������10��,��ʧ��...";
                        return -1;
                    }
                    goto REDO_CHANGE;
                }
                return -1;
            } // end of д�ز��¼ʧ��


            return 1;
        }
    }
}
