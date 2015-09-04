using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

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
    /// �������Ǻ���־�ָ���صĴ���
    /// </summary>
    public partial class LibraryApplication
    {
        // Borrow() API �ָ�����
        /* ��־��¼��ʽ����
<root>
  <operation>borrow</operation> ��������
  <readerBarcode>R0000002</readerBarcode> ����֤�����
  <itemBarcode>0000001</itemBarcode>  �������
  <borrowDate>Fri, 08 Dec 2006 04:17:31 GMT</borrowDate> ��������
  <borrowPeriod>30day</borrowPeriod> ��������
  <no>0</no> ���������0Ϊ�״���ͨ���ģ�1��ʼΪ����
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 04:17:31 GMT</operTime> ����ʱ��
  <confirmItemRecPath>...</confirmItemRecPath> �����ж��õĲ��¼·��
  
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
  <itemRecord recPath='...'>...</itemRecord>	���²��¼
</root>
         * */
        // parameters:
        //      bForce  �Ƿ�Ϊ�ݴ�״̬�����ݴ�״̬�£���������ظ��Ĳ�����ţ���������һ����
        public int RecoverBorrow(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            bool bForce,
            out string strError)
        {
            strError = "";

            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            DO_SNAPSHOT:

            // ���ջָ�
            if (level == RecoverLevel.Snapshot)
            {
                XmlNode node = null;
                string strReaderXml = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerRecord", 
                    out node);
                if (node == null)
                {
                    strError = "��־��¼��ȱ<readerRecord>Ԫ��";
                    return -1;
                }
                string strReaderRecPath = DomUtil.GetAttr(node, "recPath");

                string strItemXml = DomUtil.GetElementText(domLog.DocumentElement,
    "itemRecord",
    out node);
                if (node == null)
                {
                    strError = "��־��¼��ȱ<itemRecord>Ԫ��";
                    return -1;
                }
                string strItemRecPath = DomUtil.GetAttr(node, "recPath");

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                // д���߼�¼
                lRet = channel.DoSaveTextRes(strReaderRecPath,
    strReaderXml,
    false,
    "content,ignorechecktimestamp",
    timestamp,
    out output_timestamp,
    out strOutputPath,
    out strError);
                if (lRet == -1)
                {
                    strError = "д����߼�¼ '" + strReaderRecPath + "' ʱ��������: " + strError;
                    return -1;
                }

                // д���¼
                lRet = channel.DoSaveTextRes(strItemRecPath,
strItemXml,
false,
"content,ignorechecktimestamp",
timestamp,
out output_timestamp,
out strOutputPath,
out strError);
                if (lRet == -1)
                {
                    strError = "д����¼ '" + strItemRecPath + "' ʱ��������: " + strError;
                    return -1;
                }

                return 0;
            }

            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {
                string strRecoverComment = "";

                string strReaderBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerBarcode");
                if (String.IsNullOrEmpty(strReaderBarcode) == true)
                {
                    strError = "<readerBarcode>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

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
                    strError = "����֤����� '" + strReaderBarcode + "' ������";
                    goto ERROR1;
                }
                if (nRet == -1)
                {
                    strError = "����֤�����Ϊ '" + strReaderBarcode + "' �Ķ��߼�¼ʱ��������: " + strError;
                    goto ERROR1;
                }

                string strLibraryCode = "";
                // ��ö��߿�Ĺݴ���
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = GetLibraryCode(
                        strOutputReaderRecPath,
                        out strLibraryCode,
                        out strError);
                if (nRet == -1)
                    goto ERROR1;

                XmlDocument readerdom = null;
                nRet = LibraryApplication.LoadToDom(strReaderXml,
                    out readerdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                    goto ERROR1;
                }

                // ������¼
                string strConfirmItemRecPath = DomUtil.GetElementText(domLog.DocumentElement,
                    "confirmItemRecPath");
                string strItemBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "itemBarcode");
                if (String.IsNullOrEmpty(strItemBarcode) == true)
                {
                    strError = "<strItemBarcode>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

                string strItemXml = "";
                string strOutputItemRecPath = "";
                byte[] item_timestamp = null;

                // ����Ѿ���ȷ���Ĳ��¼·��
                if (String.IsNullOrEmpty(strConfirmItemRecPath) == false)
                {
                    string strMetaData = "";
                    lRet = channel.GetRes(strConfirmItemRecPath,
                        out strItemXml,
                        out strMetaData,
                        out item_timestamp,
                        out strOutputItemRecPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "����strConfirmItemRecPath '" + strConfirmItemRecPath + "' ��ò��¼ʧ��: " + strError;
                        goto ERROR1;
                    }

                    // ��Ҫ����¼�е�<barcode>Ԫ��ֵ�Ƿ�ƥ��������


                    // TODO: �����¼·�������ļ�¼�����ڣ�������<barcode>Ԫ��ֵ��Ҫ��Ĳ�����Ų�ƥ�䣬��ô��Ҫ�����߼�������Ҳ�������ò����������ü�¼��
                    // ��Ȼ����������£��ǳ�Ҫ������ȷ�����ݿ�����ʺܺã�����û��������ŵ�������֡�
                }
                else
                {
                    // �Ӳ�����Ż�ò��¼
                    List<string> aPath = null;

                    // ��ò��¼
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
                        strError = "������� '" + strItemBarcode + "' ������";
                        goto ERROR1;
                    }
                    if (nRet == -1)
                    {
                        strError = "����������Ϊ '" + strItemBarcode + "' �Ĳ��¼ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    if (aPath.Count > 1)
                    {
                        if (bForce == true)
                        {
                            // �ݴ�
                            strOutputItemRecPath = aPath[0];

                            strRecoverComment += "������� " + strItemBarcode + " �� "
                                + aPath.Count.ToString() + " ���ظ���¼�������ݴ�Ҫ�����ȣ�Ȩ�Ҳ������е�һ����¼ "
                                + strOutputItemRecPath + " �����н��Ĳ�����";
                        }
                        else
                        {
                            strError = "�������Ϊ '" + strItemBarcode + "' �Ĳ��¼�� " + aPath.Count.ToString() + " ��������ʱcomfirmItemRecPathȴΪ��";
                            goto ERROR1;
                        }
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
                    goto ERROR1;
                }

                // �޸Ķ��߼�¼
                // �޸Ĳ��¼

                // TODO: �ݴ�����������������������ظ��ģ�Ҫд��������־��
                nRet = BorrowChangeReaderAndItemRecord(
                    Channels,
                    strItemBarcode,
                    strReaderBarcode,
                    domLog,
                    strRecoverComment,
                    strLibraryCode,
                    ref readerdom,
                    ref itemdom,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // д�ض��ߡ����¼
                byte[] output_timestamp = null;
                string strOutputPath = "";


                // д�ض��߼�¼
                lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                    readerdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    reader_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                // д�ز��¼
                lRet = channel.DoSaveTextRes(strOutputItemRecPath,
                    itemdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    item_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }

            // �ݴ�ָ�
            if (level == RecoverLevel.Robust)
            {
                string strRecoverComment = "";

                string strReaderBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerBarcode");
                if (String.IsNullOrEmpty(strReaderBarcode) == true)
                {
                    strError = "<readerBarcode>Ԫ��ֵΪ��";
                    return -1;
                }

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
                    strError = "����֤����� '" + strReaderBarcode + "' ������";
                    // TODO: ������Ϣ�ļ�

                    // ����־��¼�л�ö��߼�¼
                    XmlNode node = null;
                    strReaderXml = DomUtil.GetElementText(domLog.DocumentElement,
                        "readerRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<readerRecord>Ԫ��";
                        return -1;
                    }
                    string strReaderRecPath = DomUtil.GetAttr(node, "recPath");
                    if (String.IsNullOrEmpty(strReaderRecPath) == true)
                    {
                        strError = "��־��¼��<readerRecord>Ԫ��ȱrecPath����";
                        return -1;
                    }

                    // ����һ�����߼�¼
                    strOutputReaderRecPath = ResPath.GetDbName(strReaderRecPath) + "/?";
                    reader_timestamp = null;
                }
                else
                {
                    if (nRet == -1)
                    {
                        strError = "����֤�����Ϊ '" + strReaderBarcode + "' �Ķ��߼�¼ʱ��������: " + strError;
                        return -1;
                    }
                }

                string strLibraryCode = "";
                // ��ö��߿�Ĺݴ���
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = GetLibraryCode(
                        strOutputReaderRecPath,
                        out strLibraryCode,
                        out strError);
                if (nRet == -1)
                    goto ERROR1;

                XmlDocument readerdom = null;
                nRet = LibraryApplication.LoadToDom(strReaderXml,
                    out readerdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                    return -1;
                }

                // ������¼
                string strConfirmItemRecPath = DomUtil.GetElementText(domLog.DocumentElement,
                    "confirmItemRecPath");
                string strItemBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "itemBarcode");
                if (String.IsNullOrEmpty(strItemBarcode) == true)
                {
                    strError = "<strItemBarcode>Ԫ��ֵΪ��";
                    return -1;
                }

                string strItemXml = "";
                string strOutputItemRecPath = "";
                byte[] item_timestamp = null;


                // �Ӳ�����Ż�ò��¼
                List<string> aPath = null;

                // ��ò��¼
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
                    strError = "������� '" + strItemBarcode + "' ������";
                    // TODO: ������Ϣ�ļ�

                    XmlNode node = null;
                    strItemXml = DomUtil.GetElementText(domLog.DocumentElement,
                        "itemRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<itemRecord>Ԫ��";
                        return -1;
                    }
                    string strItemRecPath = DomUtil.GetAttr(node, "recPath");
                    if (String.IsNullOrEmpty(strItemRecPath) == true)
                    {
                        strError = "��־��¼��<itemRecord>Ԫ��ȱrecPath����";
                        return -1;
                    }

                    // ����һ�����¼
                    strOutputItemRecPath = ResPath.GetDbName(strItemRecPath) + "/?";
                    item_timestamp = null;
                }
                else
                {

                    if (nRet == -1)
                    {
                        strError = "����������Ϊ '" + strItemBarcode + "' �Ĳ��¼ʱ��������: " + strError;
                        return -1;
                    }

                    Debug.Assert(aPath != null, "");

                    bool bNeedReload = false;

                    if (aPath.Count > 1)
                    {

                        // �������strConfirmItemRecPath��������ѡ
                        if (String.IsNullOrEmpty(strConfirmItemRecPath) == true)
                        {
                            // �ݴ�
                            strOutputItemRecPath = aPath[0];

                            strRecoverComment += "������� " + strItemBarcode + " �� "
                                + aPath.Count.ToString() + " ���ظ���¼�������ݴ�Ҫ�����ȣ�Ȩ�Ҳ������е�һ����¼ "
                                + strOutputItemRecPath + " �����н��Ĳ�����";

                            // �Ƿ���Ҫ����װ�أ�
                            bNeedReload = false;    // ��ȡ�õĵ�һ��·�������¼�Ѿ�װ��
                        }
                        else
                        {

                            ///// 
                            nRet = aPath.IndexOf(strConfirmItemRecPath);
                            if (nRet != -1)
                            {
                                strOutputItemRecPath = aPath[nRet];
                                strRecoverComment += "������� " + strItemBarcode + " �� "
                                    + aPath.Count.ToString() + " ���ظ���¼�������ҵ�strConfirmItemRecPath=[" + strConfirmItemRecPath + "]"
                                    + "�����н��Ĳ�����";

                                // �Ƿ���Ҫ����װ�أ�
                                if (nRet != 0)
                                    bNeedReload = true; // ��һ�������·������Ҫװ��

                            }
                            else
                            {
                                // �ݴ�
                                strOutputItemRecPath = aPath[0];

                                strRecoverComment += "������� " + strItemBarcode + " �� "
                                    + aPath.Count.ToString() + " ���ظ���¼���������޷��ҵ�strConfirmItemRecPath=[" + strConfirmItemRecPath + "]�ļ�¼"
                                    + "�����ݴ�Ҫ�����ȣ�Ȩ�Ҳ������е�һ����¼ "
                                    + strOutputItemRecPath + " �����н��Ĳ�����";

                                // �Ƿ���Ҫ����װ�أ�
                                bNeedReload = false;    // ��ȡ�õĵ�һ��·�������¼�Ѿ�װ��

                                /* 
                                                                    strError = "������� " + strItemBarcode + " �� "
                                                                        + aPath.Count.ToString() + " ���ظ���¼���������޷��ҵ�strConfirmItemRecPath=[" + strConfirmItemRecPath + "]�ļ�¼";
                                                                    return -1;
                                 * */

                            }
                        }


                    } // if (aPath.Count > 1)
                    else
                    {

                        Debug.Assert(nRet == 1, "");
                        Debug.Assert(aPath.Count == 1, "");

                        if (nRet == 1)
                        {
                            strOutputItemRecPath = aPath[0];

                            // �Ƿ���Ҫ����װ�أ�
                            bNeedReload = false;    // ��ȡ�õĵ�һ��·�������¼�Ѿ�װ��
                        }
                    }


                    // ����װ��
                    if (bNeedReload == true)
                    {
                        string strMetaData = "";
                        lRet = channel.GetRes(strOutputItemRecPath,
                            out strItemXml,
                            out strMetaData,
                            out item_timestamp,
                            out strOutputItemRecPath,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "����strOutputItemRecPath '" + strOutputItemRecPath + "' ���»�ò��¼ʧ��: " + strError;
                            return -1;
                        }

                        // ��Ҫ����¼�е�<barcode>Ԫ��ֵ�Ƿ�ƥ��������

                    }
                }

                ////

                XmlDocument itemdom = null;
                nRet = LibraryApplication.LoadToDom(strItemXml,
                    out itemdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�ز��¼����XML DOMʱ��������: " + strError;
                    goto ERROR1;
                }

                // �޸Ķ��߼�¼
                // �޸Ĳ��¼

                nRet = BorrowChangeReaderAndItemRecord(
                    Channels,
                    strItemBarcode,
                    strReaderBarcode,
                    domLog,
                    strRecoverComment,
                    strLibraryCode,
                    ref readerdom,
                    ref itemdom,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // д�ض��ߡ����¼
                byte[] output_timestamp = null;
                string strOutputPath = "";


                // д�ض��߼�¼
                lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                    readerdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    reader_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                // д�ز��¼
                lRet = channel.DoSaveTextRes(strOutputItemRecPath,
                    itemdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    item_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }


            return 0;
            ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        #region RecoverBorrow()�¼�����

        // ��ý�������
        // parameters:
        //      strLibraryCode  ���߼�¼�������Ķ���߿�Ĺݴ���
        // return:
        //      -1  ����
        //      0   û�л�ò���
        //      1   ����˲���
        int GetBorrowPeriod(
            string strLibraryCode,
            XmlDocument readerdom,
            XmlDocument itemdom,
            int nNo,
            out string strPeriod,
            out string strError
            )
        {
            strPeriod = "";
            strError = "";
            int nRet = 0;

            // ����Ҫ���ĵĲ���Ϣ�У��ҵ�ͼ������
            string strBookType = DomUtil.GetElementText(itemdom.DocumentElement, "bookType");

            // �Ӷ�����Ϣ��, �ҵ���������
            string strReaderType = DomUtil.GetElementText(readerdom.DocumentElement, "readerType");

            string strBorrowPeriodList = "";
            MatchResult matchresult;
            nRet = this.GetLoanParam(
                //null,
                strLibraryCode,
                strReaderType,
                strBookType,
                "����",
                out strBorrowPeriodList,
                out matchresult,
                out strError);
            if (nRet == -1)
            {
                strError = "��ý���ʧ�ܡ���� �ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ����ʱ��������: " + strError;
                return 0;
            }
            if (nRet < 4)  // nRet == 0
            {
                strError = "��ý���ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� �����޷����: " + strError;
                return 0;
            }

            // ���ն��ŷ���ֵ����Ҫ�������ȡ��ĳ������

            string[] aPeriod = strBorrowPeriodList.Split(new char[] { ',' });

            if (aPeriod.Length == 0)
            {
                strError = "��ý���ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ���� '" + strBorrowPeriodList + "'��ʽ����";
                return 0;
            }

            string strThisBorrowPeriod = "";
            string strLastBorrowPeriod = "";

            if (nNo > 0)
            {
                if (nNo >= aPeriod.Length)
                {
                    if (aPeriod.Length == 1)
                        strError = "����ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ����ֵ '" + strBorrowPeriodList + "' �涨���������衣(�������һ�����ޣ���ָ��һ�ν��ĵ�����)";
                    else
                        strError = "����ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ����ֵ '" + strBorrowPeriodList + "' �涨��ֻ������ " + Convert.ToString(aPeriod.Length - 1) + " �Ρ�";
                    return -1;
                }
                strThisBorrowPeriod = aPeriod[nNo].Trim();

                strLastBorrowPeriod = aPeriod[nNo - 1].Trim();

                if (String.IsNullOrEmpty(strThisBorrowPeriod) == true)
                {
                    strError = "����ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ���� '" + strBorrowPeriodList + "' ��ʽ���󣺵� " + Convert.ToString(nNo) + "������Ϊ�ա�";
                    return -1;
                }
            }
            else
            {
                strThisBorrowPeriod = aPeriod[0].Trim();

                if (String.IsNullOrEmpty(strThisBorrowPeriod) == true)
                {
                    strError = "����ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ���� '" + strBorrowPeriodList + "' ��ʽ���󣺵�һ����Ϊ�ա�";
                    return -1;
                }
            }

            // ���strBorrowPeriod�Ƿ�Ϸ�
            {
                long lPeriodValue = 0;
                string strPeriodUnit = "";
                nRet = LibraryApplication.ParsePeriodUnit(
                    strThisBorrowPeriod,
                    out lPeriodValue,
                    out strPeriodUnit,
                    out strError);
                if (nRet == -1)
                {
                    strError = "����ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ���� '" + strBorrowPeriodList + "' ��ʽ����'" +
                         strThisBorrowPeriod + "' ��ʽ����: " + strError;
                    return -1;
                }
            }

            strPeriod = strThisBorrowPeriod;
            return 1;
        }

        // �� XML Ԫ�����õ� XML ����
        static void SetAttribute(XmlDocument domLog,
            string strAttrName,
            XmlElement nodeBorrow)
        {
            string strValue = DomUtil.GetElementText(domLog.DocumentElement,
strAttrName);
            if (string.IsNullOrEmpty(strValue) == false)
                nodeBorrow.SetAttribute(strAttrName, strValue);
        }

        // �� XML Ԫ�����õ� XML ����
        static void SetAttribute(ref XmlDocument dom,
            string strElementName,
            XmlElement nodeBorrow,
            string strAttrName,
            bool bDeleteElement)
        {
            string strValue = DomUtil.GetElementText(dom.DocumentElement,
strElementName);
            if (string.IsNullOrEmpty(strValue) == false)
                nodeBorrow.SetAttribute(strAttrName, strValue);

            if (bDeleteElement == true)
                DomUtil.DeleteElement(dom.DocumentElement, strElementName);
        }

        // ȥ�����߼�¼��Ľ�����Ϣ����
        // return:
        //      -1  ����
        //      0   û�б�Ҫ�޸�
        //      1   �޸��ɹ�
        int RemoveReaderSideLink(
            RmsChannelCollection Channels,
            string strReaderBarcode,
            string strItemBarcode,
            out string strRemovedInfo,
            out string strError)
        {
            strError = "";
            strRemovedInfo = "";

            int nRedoCount = 0; // ��Ϊʱ�����ͻ, ���ԵĴ���

            REDO_REPAIR:

            // ������߼�¼
            string strReaderXml = "";
            string strOutputReaderRecPath = "";
            byte[] reader_timestamp = null;
            int nRet = this.GetReaderRecXml(
                Channels,
                strReaderBarcode,
                out strReaderXml,
                out strOutputReaderRecPath,
                out reader_timestamp,
                out strError);
            if (nRet == 0)
            {
                strError = "����֤����� '" + strReaderBarcode + "' ������";
                return 0;
            }
            if (nRet == -1)
            {
                strError = "������߼�¼ʱ��������: " + strError;
                return -1;
            }

            XmlDocument readerdom = null;
            nRet = LibraryApplication.LoadToDom(strReaderXml,
                out readerdom,
                out strError);
            if (nRet == -1)
            {
                strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                return -1;
            }

            // У�����֤����Ų����Ƿ��XML��¼����ȫһ��
            string strTempBarcode = DomUtil.GetElementText(readerdom.DocumentElement,
                "barcode");
            if (strReaderBarcode != strTempBarcode)
            {
                strError = "�޸��������ܾ��������֤����Ų��� '" + strReaderBarcode + "' �Ͷ��߼�¼��<barcode>Ԫ���ڵĶ���֤�����ֵ '" + strTempBarcode + "' ��һ�¡�";
                return -1;
            }

            XmlNode nodeBorrow = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcode + "']");
            if (nodeBorrow == null)
            {
                strError = "�ڶ��߼�¼ '"+strReaderBarcode+"' ��û���ҵ����ڲ������ '"+strItemBarcode+"' ����";
                return 0;
            }

            strRemovedInfo = nodeBorrow.OuterXml;

            // �Ƴ����߼�¼�����
            nodeBorrow.ParentNode.RemoveChild(nodeBorrow);

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            byte[] output_timestamp = null;
            string strOutputPath = "";

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
                        strError = "д�ض��߼�¼ '" + strOutputReaderRecPath + "' ��ʱ��,����ʱ�����ͻ,���������10��,��ʧ��...";
                        return -1;
                    }
                    goto REDO_REPAIR;
                }
                return -1;
            }

            return 1;
        }

        // ���Ĳ������޸Ķ��ߺͲ��¼
        // parameters:
        //      strItemBarcodeParam ������š�����ʹ�� @refID: ǰ׺
        //      strLibraryCode  ���߼�¼�������Ķ���߿�Ĺݴ���
        int BorrowChangeReaderAndItemRecord(
            RmsChannelCollection Channels,
            string strItemBarcodeParam,
            string strReaderBarcode,
            XmlDocument domLog,
            string strRecoverComment,
            string strLibraryCode,
            ref XmlDocument readerdom,
            ref XmlDocument itemdom,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strOperator = DomUtil.GetElementText(domLog.DocumentElement,
    "operator");

            // *** �޸Ķ��߼�¼


            string strNo = DomUtil.GetElementText(domLog.DocumentElement,
                "no");
            if (String.IsNullOrEmpty(strNo) == true)
            {
                strError = "��־��¼��ȱ<no>Ԫ��";
                return -1;
            }

            int nNo = 0;

            try
            {
                nNo = Convert.ToInt32(strNo);
            }
            catch (Exception /*ex*/)
            {
                strError = "<no>Ԫ��ֵ '" + strNo + "' Ӧ��Ϊ������";
                return -1;
            }

            XmlElement nodeBorrow = null;

            // ��Ȼ��־��¼�м��ص��� @refID: ����̬���Ƕ��߼�¼�� borrows �����Ʊؼ��ص�Ҳ�������̬
            nodeBorrow = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcodeParam + "']") as XmlElement;

#if NOOOOOOOOOOOOOOOOOO
            if (nNo >= 1)
            {
                // �����Ƿ��Ѿ�����ǰ�Ѿ����ĵĲ�
                // nodeBorrow = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcode + "']");
                if (nodeBorrow == null)
                {
                    strError = "�ö���δ�����Ĺ��� '" + strItemBarcode + "'������޷����衣";
                    return -1;
                }

                /*
                // ����ϴε���ţ����Լ���
                int nLastNo = 0;
                string strLastNo = DomUtil.GetAttr(nodeBorrow, "no");
                if (String.IsNullOrEmpty(strLastNo) == true)
                    nLastNo = 0;
                else
                {
                    try
                    {
                        nLastNo = Convert.ToInt32(strLastNo);
                    }
                    catch
                    {
                        strError = "���߼�¼��XMLƬ�� " + nodeBorrow.OuterXml + "����no����ֵ'" + strLastNo + "' ��ʽ����";
                        return -1;
                    }
                }

                if (nLastNo != nNo - 1)
                {
                    strError = "���߼�¼���Ѿ����ڵ��ϴν�α�� '" + nLastNo.ToString() + "' �������ñȱ��εĽ�α�� '" + nNo.ToString() + "' С1";
                    return -1;
                }
                 * */

            }
            else
            {
                if (nodeBorrow != null)
                {
                    // 2008/1/30 changed ���ݴ�
                    // strError = "�ö����Ѿ������˲� '" + strItemBarcode + "'�������ظ��衣";
                    // return -1;
                    // 
                }
                else
                {
                    // ���<borrows>Ԫ���Ƿ����
                    XmlNode root = readerdom.DocumentElement.SelectSingleNode("borrows");
                    if (root == null)
                    {
                        root = readerdom.CreateElement("borrows");
                        root = readerdom.DocumentElement.AppendChild(root);
                    }

                    // ������Ĳ���Ϣ
                    nodeBorrow = readerdom.CreateElement("borrow");
                    nodeBorrow = root.AppendChild(nodeBorrow);
                }
            }
#endif

            // 2008/2/1 changed Ϊ������ݴ��������������ʱ��ȥ׷����ǰ�Ƿ���Ĺ�

            if (nodeBorrow != null)
            {
                // 2008/1/30 changed ���ݴ�
                // strError = "�ö����Ѿ������˲� '" + strItemBarcode + "'�������ظ��衣";
                // return -1;
                // 
            }
            else
            {
                // ���<borrows>Ԫ���Ƿ����
                XmlNode root = readerdom.DocumentElement.SelectSingleNode("borrows");
                if (root == null)
                {
                    root = readerdom.CreateElement("borrows");
                    root = readerdom.DocumentElement.AppendChild(root);
                }

                // ������Ĳ���Ϣ
                nodeBorrow = readerdom.CreateElement("borrow");
                nodeBorrow = root.AppendChild(nodeBorrow) as XmlElement;
            }

            // 
            // barcode
            DomUtil.SetAttr(nodeBorrow, "barcode", strItemBarcodeParam);

            string strRenewComment = "";

            string strBorrowDate = DomUtil.GetElementText(domLog.DocumentElement,
                "borrowDate");

            if (nNo >= 1)
            {
                // ����ǰһ�ν��ĵ���Ϣ
                strRenewComment = DomUtil.GetAttr(nodeBorrow, "renewComment");

                if (strRenewComment != "")
                    strRenewComment += "; ";

                strRenewComment += "no=" + Convert.ToString(nNo - 1) + ", ";
                strRenewComment += "borrowDate=" + DomUtil.GetAttr(nodeBorrow, "borrowDate") + ", ";
                strRenewComment += "borrowPeriod=" + DomUtil.GetAttr(nodeBorrow, "borrowPeriod") + ", ";
                strRenewComment += "returnDate=" + strBorrowDate + ", ";
                strRenewComment += "operator=" + DomUtil.GetAttr(nodeBorrow, "operator");
            }

            // borrowDate
            DomUtil.SetAttr(nodeBorrow, "borrowDate",
                strBorrowDate);

            // no
            DomUtil.SetAttr(nodeBorrow, "no", Convert.ToString(nNo));

            // borrowPeriod
            string strBorrowPeriod = DomUtil.GetElementText(domLog.DocumentElement,
                "borrowPeriod");

            if (String.IsNullOrEmpty(strBorrowPeriod) == true)
            {
                // ��ý�������
                // return:
                //      -1  ����
                //      0   û�л�ò���
                //      1   ����˲���
                nRet = GetBorrowPeriod(
                    strLibraryCode,
                    readerdom,
                    itemdom,
                    nNo,
                    out strBorrowPeriod,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    strBorrowPeriod = DomUtil.GetElementText(domLog.DocumentElement,
    "defaultBorrowPeriod");
                    if (String.IsNullOrEmpty(strBorrowPeriod) == true)
                        strBorrowPeriod = "60day";
                }
            }

            DomUtil.SetAttr(nodeBorrow, "borrowPeriod", strBorrowPeriod);

            // returningDate
            SetAttribute(domLog,
                "returningDate",
                nodeBorrow);

            // renewComment
            {
                if (string.IsNullOrEmpty(strRenewComment) == false)
                    DomUtil.SetAttr(nodeBorrow, "renewComment", strRenewComment);
            }

            // operator
#if NO
            DomUtil.SetAttr(nodeBorrow, "operator", strOperator);
#endif
            SetAttribute(domLog,
    "operator",
    nodeBorrow);

            // recoverComment
            if (String.IsNullOrEmpty(strRecoverComment) == false)
                DomUtil.SetAttr(nodeBorrow, "recoverComment", strItemBarcodeParam);

            // type
            SetAttribute(domLog,
                "type",
                nodeBorrow);

            // price
            SetAttribute(domLog,
                "price",
                nodeBorrow);

            // *** �����¼��ǰ�Ƿ�����ڽ�ĺۼ���������ڵĻ���(���ָ��ǰ���ߵ����޷��˷������漴��Ҫ����) ��Ҫ����������ص���һ�����߼�¼�ĺۼ���Ҳ����˵�൱�ڰ���صĲ�����л������

            string strBorrower0 = DomUtil.GetElementInnerText(itemdom.DocumentElement,
                "borrower");
            if (string.IsNullOrEmpty(strBorrower0) == false 
                && strBorrower0 != strReaderBarcode)
            {
                string strRemovedInfo = "";

                // ȥ�����߼�¼��Ľ�����Ϣ����
                // return:
                //      -1  ����
                //      0   û�б�Ҫ�޸�
                //      1   �޸��ɹ�
                nRet = RemoveReaderSideLink(
                    Channels,
                    strBorrower0,
                    strItemBarcodeParam,
                    out strRemovedInfo,
                    out strError);
                if (nRet == -1)
                {
                    this.WriteErrorLog("�������Ϊ '" + strItemBarcodeParam + "' �Ĳ��¼���ڽ��н������(�ⱻ���� '" + strReaderBarcode + "' ����)��ǰ������������һ���� '" + strBorrower0 + "' ���У���������Զ�����(ɾ��)�˶��߼�¼�İ�������Ϣ������������ȥ�����߼�¼�������ʱ��������: " + strError);
                }
                else
                {
                    this.WriteErrorLog("�������Ϊ '"+strItemBarcodeParam+"' �Ĳ��¼���ڽ��н������(�ⱻ���� '"+strReaderBarcode+"' ����)��ǰ������������һ���� '"+strBorrower0+"' ���У�����Ѿ��Զ�����(ɾ��)�˴˶��߼�¼�İ�������Ϣ���������ߵ�Ƭ�� XML ��ϢΪ '"+strRemovedInfo+"'");
                }
            }

            // *** �޸Ĳ��¼
            DomUtil.SetElementText(itemdom.DocumentElement,
                "borrower", strReaderBarcode);

            DomUtil.SetElementText(itemdom.DocumentElement,
                "borrowDate",
                strBorrowDate);

            DomUtil.SetElementText(itemdom.DocumentElement,
                "no",
                Convert.ToString(nNo));

            DomUtil.SetElementText(itemdom.DocumentElement,
                "borrowPeriod",
                strBorrowPeriod);

            DomUtil.SetElementText(itemdom.DocumentElement,
                "renewComment",
                strRenewComment);

            DomUtil.SetElementText(itemdom.DocumentElement,
    "operator",
    strOperator);

            // recoverComment
            if (String.IsNullOrEmpty(strRecoverComment) == false)
            {
                DomUtil.SetElementText(itemdom.DocumentElement,
        "recoverComment",
        strRecoverComment);
            }

            return 0;
        }

        #endregion

        // Return() API �ָ�����
        /* ��־��¼��ʽ
<root>
  <operation>return</operation> ��������
  <itemBarcode>0000001</itemBarcode> �������
  <readerBarcode>R0000002</readerBarcode> ����֤�����
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 04:17:45 GMT</operTime> ����ʱ��
  <overdues>...</overdues> ������Ϣ ͨ������Ϊһ���ַ�����Ϊһ��<overdue>Ԫ��XML�ı�Ƭ��
  
  <confirmItemRecPath>...</confirmItemRecPath> �����ж��õĲ��¼·��
  
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
  <itemRecord recPath='...'>...</itemRecord>	���²��¼
  
</root>
         * * */
        // parameters:
        //      bForce  �Ƿ�Ϊ�ݴ�״̬�����ݴ�״̬�£���������ظ��Ĳ�����ţ���������һ����
        public int RecoverReturn(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            bool bForce,
            out string strError)
        {
            strError = "";

            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

        DO_SNAPSHOT:

            // ���ջָ�
            if (level == RecoverLevel.Snapshot)
            {
                XmlNode node = null;
                string strReaderXml = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerRecord",
                    out node);
                if (node == null)
                {
                    strError = "��־��¼��ȱ<readerRecord>Ԫ��";
                    return -1;
                }
                string strReaderRecPath = DomUtil.GetAttr(node, "recPath");

                string strItemXml = DomUtil.GetElementText(domLog.DocumentElement,
                    "itemRecord",
                    out node);
                if (node == null)
                {
                    strError = "��־��¼��ȱ<itemRecord>Ԫ��";
                    return -1;
                }
                string strItemRecPath = DomUtil.GetAttr(node, "recPath");

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                // д���߼�¼
                lRet = channel.DoSaveTextRes(strReaderRecPath,
                    strReaderXml,
                    false,
                    "content,ignorechecktimestamp",
                    timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    strError = "д����߼�¼ '" + strReaderRecPath + "' ʱ��������: " + strError;
                    return -1;
                }

                // д���¼
                lRet = channel.DoSaveTextRes(strItemRecPath,
                    strItemXml,
                    false,
                    "content,ignorechecktimestamp",
                    timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    strError = "д����߼�¼ '" + strReaderRecPath + "' ʱ��������: " + strError;
                    return -1;
                }


                return 0;
            }


            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {
                string strRecoverComment = "";

                string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                    "action");

                string strReaderBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerBarcode");


                // ������¼
                string strConfirmItemRecPath = DomUtil.GetElementText(domLog.DocumentElement,
                    "confirmItemRecPath");
                string strItemBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "itemBarcode");

                if (String.IsNullOrEmpty(strItemBarcode) == true)
                {
                    strError = "<strItemBarcode>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

                string strItemXml = "";
                string strOutputItemRecPath = "";
                byte[] item_timestamp = null;

                // ����Ѿ���ȷ���Ĳ��¼·��
                if (String.IsNullOrEmpty(strConfirmItemRecPath) == false)
                {
                    string strMetaData = "";
                    lRet = channel.GetRes(strConfirmItemRecPath,
                        out strItemXml,
                        out strMetaData,
                        out item_timestamp,
                        out strOutputItemRecPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "����strConfirmItemRecPath '" + strConfirmItemRecPath + "' ��ò��¼ʧ��: " + strError;
                        goto ERROR1;
                    }

                    // ��Ҫ����¼�е�<barcode>Ԫ��ֵ�Ƿ�ƥ��������
                }
                else
                {
                    // �Ӳ�����Ż�ò��¼
                    List<string> aPath = null;

                    // ��ò��¼
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
                        strError = "������� '" + strItemBarcode + "' ������";
                        goto ERROR1;
                    }
                    if (nRet == -1)
                    {
                        strError = "����������Ϊ '" + strItemBarcode + "' �Ĳ��¼ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    if (aPath.Count > 1)
                    {

                        if (string.IsNullOrEmpty(strReaderBarcode) == true)
                        {
                            // ����������ŵ�ʱ����û�ж���֤����Ÿ����ж�
                            if (bForce == false)
                            {
                                strError = "�������Ϊ '" + strItemBarcode + "' �Ĳ��¼�� " + aPath.Count.ToString() + " ��������ʱ��־��¼��û���ṩ����֤����Ÿ����жϣ��޷����л��������";
                                goto ERROR1;
                            }
                            // TODO: �Ǿ����ٿ�����Щ���У���Щ�������˽����ţ��������ֻ��һ���˽�����Ǿ�...��
                            strRecoverComment += "������� " + strItemBarcode + "�� " + aPath.Count.ToString() + " ���ظ���¼������û�ж���֤����Ž��и���ѡ��";
                        }

                        /*
                        strError = "�������Ϊ '" + strItemBarcode + "' �Ĳ��¼�� " + aPath.Count.ToString() + " ��������ʱcomfirmItemRecPathȴΪ��";
                        goto ERROR1;
                         * */
                        // bItemBarcodeDup = true; // ��ʱ�Ѿ���Ҫ����״̬����Ȼ������Խ�һ��ʶ��������Ĳ��¼

                        /*
                        // ����strDupBarcodeList
                        string[] pathlist = new string[aPath.Count];
                        aPath.CopyTo(pathlist);
                        strDupBarcodeList = String.Join(",", pathlist);
                         * */

                        List<string> aFoundPath = null;
                        List<byte[]> aTimestamp = null;
                        List<string> aItemXml = null;

                        // �������ظ�����ŵĲ��¼�У�ѡ�����з��ϵ�ǰ����֤����ŵ�
                        // return:
                        //      -1  ����
                        //      ����    ѡ��������
                        nRet = FindItem(
                            channel,
                            strReaderBarcode,
                            aPath,
                            true,   // �Ż�
                            out aFoundPath,
                            out aItemXml,
                            out aTimestamp,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ѡ���ظ�����ŵĲ��¼ʱ��������: " + strError;
                            goto ERROR1;
                        }

                        if (nRet == 0)
                        {
                            strError = "������� '" + strItemBarcode + "' �������� " + aPath.Count + " ����¼�У�û���κ�һ����<borrower>Ԫ�ر����˱����� '" + strReaderBarcode + "' ���ġ�";
                            goto ERROR1;
                        }

                        if (nRet > 1)
                        {
                            if (bForce == true)
                            {
                                // �ݴ�����£�ѡ���һ���������
                                strOutputItemRecPath = aFoundPath[0];
                                item_timestamp = aTimestamp[0];
                                strItemXml = aItemXml[0];

                                // TODO: ������Ӧ���ڼ�¼�м���ע�ͣ���ʾ�����ݴ���ʽ
                                if (string.IsNullOrEmpty(strReaderBarcode) == true)
                                {
                                    strRecoverComment += "����ɸѡ����Ȼ�� " + aFoundPath.Count.ToString() + " �����¼���н�������Ϣ(����ʲô����֤�����)����ô��ֻ��ѡ�����е�һ�����¼ " + strOutputItemRecPath + " ���л��������";
                                }
                                else
                                {
                                    strRecoverComment += "����ɸѡ����Ȼ�� " + aFoundPath.Count.ToString() + " �����¼���н����� '"+strReaderBarcode+"' ��Ϣ����ô��ֻ��ѡ�����е�һ�����¼ " + strOutputItemRecPath + " ���л��������";
                                }
                            }
                            else
                            {
                                strError = "�������Ϊ '" + strItemBarcode + "' ����<borrower>Ԫ�ر���Ϊ���� '" + strReaderBarcode + "' ���ĵĲ��¼�� " + aFoundPath.Count.ToString() + " �����޷����л��������";
                                /*
                                aDupPath = new string[aFoundPath.Count];
                                aFoundPath.CopyTo(aDupPath);
                                 * */
                                goto ERROR1;
                            }
                        }

                        Debug.Assert(nRet == 1, "");

                        strOutputItemRecPath = aFoundPath[0];
                        item_timestamp = aTimestamp[0];
                        strItemXml = aItemXml[0];
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
                    goto ERROR1;
                }

                ///
                if (String.IsNullOrEmpty(strReaderBarcode) == true)
                {
                    if (bForce == true)
                    {
                        // �ݴ������£��Ӳ��¼�л�ý���֤�����
                        strReaderBarcode = DomUtil.GetElementText(itemdom.DocumentElement,
                            "borrower");
                        if (String.IsNullOrEmpty(strReaderBarcode) == true)
                        {
                            strError = "�ڲ�֪������֤����ŵ�����£����¼�е�<borrower>Ԫ��ֵΪ�ա��޷����л��������";
                            goto ERROR1;
                        }

                    }
                    else
                    {
                        strError = "��־��¼��<readerBarcode>Ԫ��ֵΪ��";
                        goto ERROR1;
                    }
                }

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
                    strError = "����֤����� '" + strReaderBarcode + "' ������";
                    goto ERROR1;
                }
                if (nRet == -1)
                {
                    strError = "����֤�����Ϊ '" + strReaderBarcode + "' �Ķ��߼�¼ʱ��������: " + strError;
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

                // �޸Ķ��߼�¼
                // �޸Ĳ��¼
                nRet = ReturnChangeReaderAndItemRecord(
                    Channels,
                    strAction,
                    strItemBarcode,
                    strReaderBarcode,
                    domLog,
                    strRecoverComment,
                    ref readerdom,
                    ref itemdom,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // д�ض��ߡ����¼
                byte[] output_timestamp = null;
                string strOutputPath = "";


                // д�ض��߼�¼
                lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                    readerdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    reader_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                // д�ز��¼
                lRet = channel.DoSaveTextRes(strOutputItemRecPath,
                    itemdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    item_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }


            // �ݴ�ָ�
            if (level == RecoverLevel.Robust)
            {
                string strRecoverComment = "";

                string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                    "action");

                string strReaderBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerBarcode");


                // ������¼
                string strConfirmItemRecPath = DomUtil.GetElementText(domLog.DocumentElement,
                    "confirmItemRecPath");
                string strItemBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "itemBarcode");

                if (String.IsNullOrEmpty(strItemBarcode) == true)
                {
                    strError = "<strItemBarcode>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

                string strItemXml = "";
                string strOutputItemRecPath = "";
                byte[] item_timestamp = null;


                // �Ӳ�����Ż�ò��¼
                List<string> aPath = null;

                bool bDupItemBarcode = false;   // ��������Ƿ������ظ�

                // ��ò��¼
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
                    strError = "������� '" + strItemBarcode + "' ������";
                    // TODO: ������Ϣ�ļ�

                    XmlNode node = null;
                    strItemXml = DomUtil.GetElementText(domLog.DocumentElement,
                        "itemRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<itemRecord>Ԫ��";
                        return -1;
                    }
                    string strItemRecPath = DomUtil.GetAttr(node, "recPath");
                    if (String.IsNullOrEmpty(strItemRecPath) == true)
                    {
                        strError = "��־��¼��<itemRecord>Ԫ��ȱrecPath����";
                        return -1;
                    }

                    // ����һ�����¼
                    strOutputItemRecPath = ResPath.GetDbName(strItemRecPath) + "/?";
                    item_timestamp = null;
                }
                else
                {
                    if (nRet == -1)
                    {
                        strError = "����������Ϊ '" + strItemBarcode + "' �Ĳ��¼ʱ��������: " + strError;
                        return -1;
                    }

                    if (aPath.Count > 1)
                    {
                        bDupItemBarcode = true;

                        if (string.IsNullOrEmpty(strReaderBarcode) == true)
                        {
                            // ����������ŵ�ʱ����û�ж���֤����Ÿ����ж�
                            if (bForce == false)
                            {
                                strError = "�������Ϊ '" + strItemBarcode + "' �Ĳ��¼�� " + aPath.Count.ToString() + " ��������ʱ��־��¼��û���ṩ����֤����Ÿ����жϣ��޷����л��������";
                                return -1;
                            }
                            // TODO: �Ǿ����ٿ�����Щ���У���Щ�������˽����ţ��������ֻ��һ���˽�����Ǿ�...��
                            strRecoverComment += "������� " + strItemBarcode + " �� " + aPath.Count.ToString() + " ���ظ���¼������û�ж���֤����Ž��и���ѡ��";
                        }

                        List<string> aFoundPath = null;
                        List<byte[]> aTimestamp = null;
                        List<string> aItemXml = null;

                        // �������ظ�����ŵĲ��¼�У�ѡ�����з��ϵ�ǰ����֤����ŵ�
                        // return:
                        //      -1  ����
                        //      ����    ѡ��������
                        nRet = FindItem(
                            channel,
                            strReaderBarcode,
                            aPath,
                            true,   // �Ż�
                            out aFoundPath,
                            out aItemXml,
                            out aTimestamp,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ѡ���ظ�����ŵĲ��¼ʱ��������: " + strError;
                            return -1;
                        }

                        if (nRet == 0)
                        {
                            if (bDupItemBarcode == false)
                            {
                                // û���ظ�������ŵ�����²���
                                // ��Ҫ�Ѹ��ݡ����������š�������߼�¼�н�����Ϣ�Ķ�����ǰ����? �����������������Χʱ�����ٶ��߼�¼�е���Ϣ�Ǳ�����˵ģ������ݴ����Ҫ
                                string strError_1 = "";
                                nRet = ReturnAllReader(
                                        Channels,
                                        strItemBarcode,
                                        "",
                                        out strError_1);
                                if (nRet == -1)
                                {
                                    // ���ⲻ������������
                                }
                            }

                            strError = "������� '" + strItemBarcode + "' �������� " + aPath.Count + " ����¼�У�û���κ�һ����<borrower>Ԫ�ر����˱����� '" + strReaderBarcode + "' ���ġ�";
                            return -1;
                        }

                        if (nRet > 1)
                        {
                            if (bForce == true)
                            {
                                // �ݴ�����£�ѡ���һ���������
                                strOutputItemRecPath = aFoundPath[0];
                                item_timestamp = aTimestamp[0];
                                strItemXml = aItemXml[0];

                                // TODO: ������Ӧ���ڼ�¼�м���ע�ͣ���ʾ�����ݴ���ʽ
                                if (string.IsNullOrEmpty(strReaderBarcode) == true)
                                {
                                    strRecoverComment += "����ɸѡ����Ȼ�� " + aFoundPath.Count.ToString() + " �����¼���н�������Ϣ(����ʲô����֤�����)����ô��ֻ��ѡ�����е�һ�����¼ " + strOutputItemRecPath + " ���л��������";
                                }
                                else
                                {
                                    strRecoverComment += "����ɸѡ����Ȼ�� " + aFoundPath.Count.ToString() + " �����¼���н����� '" + strReaderBarcode + "' ��Ϣ����ô��ֻ��ѡ�����е�һ�����¼ " + strOutputItemRecPath + " ���л��������";
                                }
                            }
                            else
                            {
                                strError = "�������Ϊ '" + strItemBarcode + "' ����<borrower>Ԫ�ر���Ϊ���� '" + strReaderBarcode + "' ���ĵĲ��¼�� " + aFoundPath.Count.ToString() + " �����޷����л��������";
                                return -1;
                            }
                        }

                        Debug.Assert(nRet == 1, "");

                        strOutputItemRecPath = aFoundPath[0];
                        item_timestamp = aTimestamp[0];
                        strItemXml = aItemXml[0];
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


                ////

                XmlDocument itemdom = null;
                nRet = LibraryApplication.LoadToDom(strItemXml,
                    out itemdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�ز��¼����XML DOMʱ��������: " + strError;
                    goto ERROR1;
                }


                ///
                if (String.IsNullOrEmpty(strReaderBarcode) == true)
                {
                    if (bForce == true)
                    {
                        // �ݴ������£��Ӳ��¼�л�ý���֤�����
                        strReaderBarcode = DomUtil.GetElementText(itemdom.DocumentElement,
                            "borrower");
                        if (String.IsNullOrEmpty(strReaderBarcode) == true)
                        {
                            strError = "�ڲ�֪������֤����ŵ�����£����¼�е�<borrower>Ԫ��ֵΪ�ա��޷����л��������";
                            return -1;
                        }

                    }
                    else
                    {
                        strError = "��־��¼��<readerBarcode>Ԫ��ֵΪ��";
                        return -1;
                    }
                }

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
                    strError = "����֤����� '" + strReaderBarcode + "' ������";
                    // TODO: ������Ϣ�ļ�

                    // ����־��¼�л�ö��߼�¼
                    XmlNode node = null;
                    strReaderXml = DomUtil.GetElementText(domLog.DocumentElement,
                        "readerRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<readerRecord>Ԫ��";
                        return -1;
                    }
                    string strReaderRecPath = DomUtil.GetAttr(node, "recPath");
                    if (String.IsNullOrEmpty(strReaderRecPath) == true)
                    {
                        strError = "��־��¼��<readerRecord>Ԫ��ȱrecPath����";
                        return -1;
                    }

                    // ����һ�����߼�¼
                    strOutputReaderRecPath = ResPath.GetDbName(strReaderRecPath) + "/?";
                    reader_timestamp = null;
                }
                else
                {
                    if (nRet == -1)
                    {
                        strError = "����֤�����Ϊ '" + strReaderBarcode + "' �Ķ��߼�¼ʱ��������: " + strError;
                        return -1;
                    }
                }

                XmlDocument readerdom = null;
                nRet = LibraryApplication.LoadToDom(strReaderXml,
                    out readerdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                    return -1;
                }


                // �޸Ķ��߼�¼
                // �޸Ĳ��¼
                nRet = ReturnChangeReaderAndItemRecord(
                    Channels,
                    strAction,
                    strItemBarcode,
                    strReaderBarcode,
                    domLog,
                    strRecoverComment,
                    ref readerdom,
                    ref itemdom,
                    out strError);
                if (nRet == -1)
                    return -1;

                // ���ݴ�(����û���ظ�������ŵ������)������£���Ҫ���ö��߿�ġ����������š�����;�����ѳ��˵�ǰ��ע�Ķ��߼�¼�����Ǳ����ض��߼�¼������
                // �������е����<borrows/borrow>Ĩ����������ɶ�ͷ�Ľ�����Ϣ��
                if (bDupItemBarcode == false)
                {
                    nRet = ReturnAllReader(
                            Channels,
                            strItemBarcode,
                            strOutputReaderRecPath,
                            out strError);
                    if (nRet == -1)
                    {
                        // ���ⲻ������������
                    }
                }


                ////



                // д�ض��ߡ����¼
                byte[] output_timestamp = null;
                string strOutputPath = "";


                // д�ض��߼�¼
                lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                    readerdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    reader_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    return -1;

                // д�ز��¼
                lRet = channel.DoSaveTextRes(strOutputItemRecPath,
                    itemdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    item_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    return -1;

            }



            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        #region RecoverReturn()�¼�����


        // ���� ���������ţ�������ɶ��߼�¼�еĽ�����Ϣ
        int ReturnAllReader(
            RmsChannelCollection Channels,
            string strItemBarcode,
            string strExcludeReaderRecPath,
            out string strError)
        {
            strError = "";

            List<string> aReaderPath = null;

            string strTempReaderXml = "";
            byte[] temp_timestamp = null;
            // ��ö��߼�¼
            // return:
            //      -1  error
            //      0   not found
            //      1   ����1��
            //      >1  ���ж���1��
            int nRet = this.GetReaderRecXml(
                Channels,
                strItemBarcode,
                out strTempReaderXml,
                100,
                out aReaderPath,
                out temp_timestamp,
                out strError);
            if (nRet == -1)
            {
                strError = "�����������Ϊ '" + strItemBarcode + "' ��һ�����߶������¼ʱ��������: " + strError;
                return -1;
            }

            if (aReaderPath != null)
            {
                // ȥ���Ѿ�����Ϊ��ǰ��¼������
                while (aReaderPath.Count > 0)
                {
                    nRet = aReaderPath.IndexOf(strExcludeReaderRecPath);
                    if (nRet != -1)
                        aReaderPath.Remove(strExcludeReaderRecPath);
                    else
                        break;
                }

                if (aReaderPath.Count >= 1)
                {
                    RmsChannel channel = Channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "get channel error";
                        return -1;
                    }

                    nRet = ClearBorrowItem(
                        channel,
                        strItemBarcode,
                        aReaderPath,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ClearBorrowItem() error: " + strError;
                        return -1;
                    }

                }
            }

            return 0;
        }


        // �����ɶ��߼�¼�У�����ض�����������൱����Զ��߼�¼ִ���˻������
        // parameters:
        // return:
        //      -1  ����
        static int ClearBorrowItem(
            RmsChannel channel,
            string strBorrowItemBarcode,
            List<string> aPath,
            out string strError)
        {
            strError = "";

            for (int i = 0; i < aPath.Count; i++)
            {
                string strXml = "";
                string strMetaData = "";
                string strOutputPath = "";
                byte[] timestamp = null;

                string strPath = aPath[i];

                long lRet = channel.GetRes(strPath,
                    out strXml,
                    out strMetaData,
                    out timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                // װ��DOM
                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "��¼ '" + strPath + "' XMLװ��DOM����: " + ex.Message;
                    goto ERROR1;
                }

                bool bChanged = false;
                XmlNodeList nodes = dom.DocumentElement.SelectNodes("//borrows/borrow[@barcode='"+strBorrowItemBarcode+"']");
                for(int j=0;j<nodes.Count;j++)
                {
                    XmlNode node = nodes[j];
                    if (node.ParentNode != null)
                    {
                        node.ParentNode.RemoveChild(node);
                        bChanged = true;
                    }
                }

                if (bChanged == true)
                {
                    // д���߼�¼
                    byte[] output_timestamp = null;
                    lRet = channel.DoSaveTextRes(strPath,
                        dom.OuterXml,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "д����߼�¼ '" + strPath + "' ʱ��������: " + strError;
                        return -1;
                    }
                }
            }

            return 0;
        ERROR1:
            return -1;
        }

        // ����������޸Ķ��ߺͲ��¼
        // parameters:
        //      strItemBarcodeParam ������š�����ʹ�� @refID: ǰ׺
        int ReturnChangeReaderAndItemRecord(
            RmsChannelCollection Channels,
            string strAction,
            string strItemBarcodeParam,
            string strReaderBarcode,
            XmlDocument domLog,
            string strRecoverComment,
            ref XmlDocument readerdom,
            ref XmlDocument itemdom,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strReturnOperator = DomUtil.GetElementText(domLog.DocumentElement,
    "operator");

            string strOperTime = DomUtil.GetElementText(domLog.DocumentElement,
    "operTime");

            // *** �޸Ķ��߼�¼
            string strDeletedBorrowFrag = "";
            XmlNode dup_reader_history = null;

            // ��Ȼ��־��¼�м��ص��� @refID: ����̬���Ƕ��߼�¼�� borrows �����Ʊؼ��ص�Ҳ�������̬
            XmlNode nodeBorrow = readerdom.DocumentElement.SelectSingleNode(
                "borrows/borrow[@barcode='" + strItemBarcodeParam + "']");
            if (nodeBorrow != null)
            {
                if (String.IsNullOrEmpty(strRecoverComment) == false)
                {
                    string strText = strRecoverComment;
                    string strOldRecoverComment = DomUtil.GetAttr(nodeBorrow, "recoverComment");
                    if (String.IsNullOrEmpty(strOldRecoverComment) == false)
                        strText = "(����ʱԭע: " + strOldRecoverComment + ") " + strRecoverComment;
                    DomUtil.SetAttr(nodeBorrow, "recoverComment", strText);
                }
                strDeletedBorrowFrag = nodeBorrow.OuterXml;
                nodeBorrow.ParentNode.RemoveChild(nodeBorrow);

                // ��ü���������Ҫ�Ĳ���
                XmlDocument temp = new XmlDocument();
                temp.LoadXml(strDeletedBorrowFrag);
                string strItemBarcode = temp.DocumentElement.GetAttribute("barcode");
                string strBorrowDate = temp.DocumentElement.GetAttribute("borrowDate");
                string strBorrowPeriod = temp.DocumentElement.GetAttribute("borrowPeriod");

                dup_reader_history = readerdom.DocumentElement.SelectSingleNode("borrowHistory/borrow[@barcode='" + strItemBarcode + "' and @borrowDate='" + strBorrowDate + "' and @borrowPeriod='" + strBorrowPeriod + "']");
            }

            // ���뵽���߼�¼������ʷ�ֶ���

            if (string.IsNullOrEmpty(strDeletedBorrowFrag) == false
                && dup_reader_history == null)
            {
                // �����������Ƿ��� borrowHistory Ԫ��
                XmlNode root = readerdom.DocumentElement.SelectSingleNode("borrowHistory");
                if (root == null)
                {
                    root = readerdom.CreateElement("borrowHistory");
                    readerdom.DocumentElement.AppendChild(root);
                }

                XmlDocumentFragment fragment = readerdom.CreateDocumentFragment();
                fragment.InnerXml = strDeletedBorrowFrag;

                // ���뵽��ǰ��
                XmlNode temp = DomUtil.InsertFirstChild(root, fragment);
                // 2007/6/19
                if (temp != null)
                {
                    // returnDate ���뻹��ʱ��
                    DomUtil.SetAttr(temp, "returnDate", strOperTime);

                    // borrowOperator
                    string strBorrowOperator = DomUtil.GetAttr(temp, "operator");
                    // ��ԭ����operator����ֵ���Ƶ�borrowOperator������
                    DomUtil.SetAttr(temp, "borrowOperator", strBorrowOperator);


                    // operator ��ʱ��Ҫ��ʾ�����������
                    DomUtil.SetAttr(temp, "operator", strReturnOperator);

                }
                // �������100������ɾ�������
                while (root.ChildNodes.Count > 100)
                    root.RemoveChild(root.ChildNodes[root.ChildNodes.Count - 1]);

                // 2007/6/19
                // ��������������ֵ
                string strBorrowCount = DomUtil.GetAttr(root, "count");
                if (String.IsNullOrEmpty(strBorrowCount) == true)
                    strBorrowCount = "1";
                else
                {
                    long lCount = 1;
                    try
                    {
                        lCount = Convert.ToInt64(strBorrowCount);
                    }
                    catch { }
                    lCount++;
                    strBorrowCount = lCount.ToString();
                }
                DomUtil.SetAttr(root, "count", strBorrowCount);
            }

            // ��������Ϣ
            string strOverdueString = DomUtil.GetElementText(domLog.DocumentElement,
                "overdues");
            if (String.IsNullOrEmpty(strOverdueString) == false)
            {
                XmlDocumentFragment fragment = readerdom.CreateDocumentFragment();
                fragment.InnerXml = strOverdueString;

                List<string> existing_ids = new List<string>();

                // �����������Ƿ���overduesԪ��
                XmlNode root = readerdom.DocumentElement.SelectSingleNode("overdues");
                if (root == null)
                {
                    root = readerdom.CreateElement("overdues");
                    readerdom.DocumentElement.AppendChild(root);
                }
                else
                {
                    // ������ǰ�Ѿ����ڵ� id
                    XmlNodeList nodes = root.SelectNodes("overdue");
                    foreach (XmlElement node in nodes)
                    {
                        string strID = node.GetAttribute("id");
                        if (string.IsNullOrEmpty(strID) == false)
                            existing_ids.Add(strID);
                    }
                }

                // root.AppendChild(fragment);
                {
                    // һ��һ�����룬�����ظ� id ����ֵ�� overdue Ԫ��
                    XmlNodeList nodes = fragment.SelectNodes("overdue");
                    foreach (XmlElement node in nodes)
                    {
                        string strID = node.GetAttribute("id");
                        if (existing_ids.IndexOf(strID) != -1)
                            continue;
                        root.AppendChild(node);
                    }
                }
            }

            // *** �����¼����ǰ�ڽ�Ķ��ߣ��Ƿ�ָ������һ�����ߡ����������������Ҫ����������ص���һ�����߼�¼�ĺۼ���Ҳ����˵�൱�ڰ���صĲ�����л������
            string strBorrower0 = DomUtil.GetElementInnerText(itemdom.DocumentElement,
    "borrower");
            if (string.IsNullOrEmpty(strBorrower0) == false
                && strBorrower0 != strReaderBarcode)
            {
                string strRemovedInfo = "";

                // ȥ�����߼�¼��Ľ�����Ϣ����
                // return:
                //      -1  ����
                //      0   û�б�Ҫ�޸�
                //      1   �޸��ɹ�
                nRet = RemoveReaderSideLink(
                    Channels,
                    strBorrower0,
                    strItemBarcodeParam,
                    out strRemovedInfo,
                    out strError);
                if (nRet == -1)
                {
                    this.WriteErrorLog("�������Ϊ '" + strItemBarcodeParam + "' �Ĳ��¼���ڽ��л������(�ⱻ���� '" + strReaderBarcode + "' ����)��ǰ������������һ���� '" + strBorrower0 + "' ���У���������Զ�����(ɾ��)�˶��߼�¼�İ�������Ϣ������������ȥ�����߼�¼�������ʱ��������: " + strError);
                }
                else
                {
                    this.WriteErrorLog("�������Ϊ '" + strItemBarcodeParam + "' �Ĳ��¼���ڽ��л������(�ⱻ���� '" + strReaderBarcode + "' ����)��ǰ������������һ���� '" + strBorrower0 + "' ���У�����Ѿ��Զ�����(ɾ��)�˴˶��߼�¼�İ�������Ϣ���������ߵ�Ƭ�� XML ��ϢΪ '" + strRemovedInfo + "'");
                }
            }


            // *** �޸Ĳ��¼
            XmlElement nodeHistoryBorrower = null;

            string strBorrower = DomUtil.GetElementText(itemdom.DocumentElement, "borrower");

            XmlNode dup_item_history = null;
            // ������ͬ���ߡ��������ڡ��������ڵ� BorrowHistory/borrower Ԫ���Ƿ��Ѿ�����
            {
                string strBorrowDate = DomUtil.GetElementText(itemdom.DocumentElement, "borrowDate");
                dup_item_history = itemdom.DocumentElement.SelectSingleNode("borrowHistory/borrower[@barcode='" + strBorrower + "' and @borrowDate='" + strBorrowDate + "' and @returnDate='" + strOperTime + "']");
            }

            if (dup_item_history != null)
            {
                // ��ʷ��Ϣ�ڵ��Ѿ����ڣ��Ͳ��ؼ�����

                // ������Ԫ��
                DomUtil.DeleteElement(itemdom.DocumentElement,
    "borrower");
                DomUtil.DeleteElement(itemdom.DocumentElement,
"borrowDate");
                DomUtil.DeleteElement(itemdom.DocumentElement,
"returningDate");
                DomUtil.DeleteElement(itemdom.DocumentElement,
"borrowPeriod");
                DomUtil.DeleteElement(itemdom.DocumentElement,
"operator");
                DomUtil.DeleteElement(itemdom.DocumentElement,
"no");
                DomUtil.DeleteElement(itemdom.DocumentElement,
"renewComment");
            }
            else
            {
                // ������ʷ��Ϣ�ڵ�

                // TODO: Ҳ�ɴ� domLog ��ȡ����Ϣ������ borrowHistory �¼������Ҫ�����ظ���������
                // �����жϲ��¼�� borrower Ԫ���Ƿ�Ϊ�յ����������п��Ա����ظ����� borrowHistory �¼�������ŵ�
                if (string.IsNullOrEmpty(strBorrower) == false)
                {
                    // ���뵽������ʷ�ֶ���
                    {
                        // �����������Ƿ���borrowHistoryԪ��
                        XmlNode root = itemdom.DocumentElement.SelectSingleNode("borrowHistory");
                        if (root == null)
                        {
                            root = itemdom.CreateElement("borrowHistory");
                            itemdom.DocumentElement.AppendChild(root);
                        }

                        nodeHistoryBorrower = itemdom.CreateElement("borrower");

                        // ���뵽��ǰ��
                        nodeHistoryBorrower = DomUtil.InsertFirstChild(root, nodeHistoryBorrower) as XmlElement;  // 2015/1/12 ���ӵȺ���ߵĲ���

                        // �������100������ɾ�������
                        while (root.ChildNodes.Count > 100)
                            root.RemoveChild(root.ChildNodes[root.ChildNodes.Count - 1]);
                    }

#if NO
                DomUtil.SetAttr(nodeOldBorrower,
                    "barcode",
                    DomUtil.GetElementText(itemdom.DocumentElement, "borrower"));
                DomUtil.SetElementText(itemdom.DocumentElement,
                    "borrower", "");
#endif
                    SetAttribute(ref itemdom,
        "borrower",
        nodeHistoryBorrower,
        "barcode",
        true);

#if NO
                DomUtil.SetAttr(nodeOldBorrower,
                  "borrowDate",
                  DomUtil.GetElementText(itemdom.DocumentElement, "borrowDate"));
                DomUtil.SetElementText(itemdom.DocumentElement,
                    "borrowDate", "");
#endif
                    SetAttribute(ref itemdom,
    "borrowDate",
    nodeHistoryBorrower,
    "borrowDate",
    true);

#if NO
                DomUtil.SetAttr(nodeOldBorrower,
      "returningDate",
      DomUtil.GetElementText(itemdom.DocumentElement, "returningDate"));
                DomUtil.SetElementText(itemdom.DocumentElement,
                    "returningDate", "");
#endif
                    SetAttribute(ref itemdom,
    "returningDate",
    nodeHistoryBorrower,
    "returningDate",
    true);

#if NO
                DomUtil.SetAttr(nodeOldBorrower,
                   "borrowPeriod",
                   DomUtil.GetElementText(itemdom.DocumentElement, "borrowPeriod"));
                DomUtil.SetElementText(itemdom.DocumentElement,
                    "borrowPeriod", "");
#endif
                    SetAttribute(ref itemdom,
                        "borrowPeriod",
                        nodeHistoryBorrower,
                        "borrowPeriod",
                        true);

                    // borrowOperator
#if NO
                DomUtil.SetAttr(nodeOldBorrower,
      "borrowOperator",
      DomUtil.GetElementText(itemdom.DocumentElement, "operator"));
                DomUtil.SetElementText(itemdom.DocumentElement,
                    "operator", "");
#endif
                    SetAttribute(ref itemdom,
    "operator",
    nodeHistoryBorrower,
    "borrowOperator",
    true);

                    // operator ���λ���Ĳ�����
                    DomUtil.SetAttr(nodeHistoryBorrower,
                      "operator",
                      strReturnOperator);

                    DomUtil.SetAttr(nodeHistoryBorrower,
          "returnDate",
          strOperTime);

                    // TODO: 0 ��Ҫʡ��
#if NO
                DomUtil.SetAttr(nodeOldBorrower,
                    "no",
                    DomUtil.GetElementText(itemdom.DocumentElement, "no"));
                DomUtil.DeleteElement(itemdom.DocumentElement,
                    "no");
#endif
                    SetAttribute(ref itemdom,
    "no",
    nodeHistoryBorrower,
    "no",
    true);

                    // renewComment
#if NO
                {
                    string strTemp = DomUtil.GetElementText(itemdom.DocumentElement, "renewComment");
                    if (string.IsNullOrEmpty(strTemp) == true)
                        strTemp = null;

                    DomUtil.SetAttr(nodeOldBorrower,
                       "renewComment",
                       strTemp);

                    DomUtil.DeleteElement(itemdom.DocumentElement,
                        "renewComment");
                }
#endif
                    SetAttribute(ref itemdom,
    "renewComment",
    nodeHistoryBorrower,
    "renewComment",
    true);

                    {
                        string strText = strRecoverComment;
                        string strOldRecoverComment = DomUtil.GetElementText(itemdom.DocumentElement, "recoverComment");
                        if (String.IsNullOrEmpty(strOldRecoverComment) == false)
                            strText = "(����ʱԭע: " + strOldRecoverComment + ") " + strRecoverComment;

                        if (String.IsNullOrEmpty(strText) == false)
                        {
                            DomUtil.SetAttr(nodeHistoryBorrower,
                                "recoverComment",
                                strText);
                        }
                    }
                }

                if (strAction == "lost")
                {
                    // �޸Ĳ��¼��<state>
                    string strState = DomUtil.GetElementText(itemdom.DocumentElement,
                        "state");
                    if (nodeHistoryBorrower != null)
                    {
                        DomUtil.SetAttr(nodeHistoryBorrower,
        "state",
        strState);
                    }

                    if (String.IsNullOrEmpty(strState) == false)
                        strState += ",";
                    strState += "��ʧ";
                    DomUtil.SetElementText(itemdom.DocumentElement,
                        "state", strState);

                    // ����־��¼�е�<lostComment>����׷��д����¼��<comment>��
                    string strLostComment = DomUtil.GetElementText(domLog.DocumentElement,
                        "lostComment");

                    if (strLostComment != "")
                    {
                        string strComment = DomUtil.GetElementText(itemdom.DocumentElement,
                            "comment");

                        if (nodeHistoryBorrower != null)
                        {
                            DomUtil.SetAttr(nodeHistoryBorrower,
                                "comment",
                                strComment);
                        }

                        if (String.IsNullOrEmpty(strComment) == false)
                            strComment += "\r\n";
                        strComment += strLostComment;
                        DomUtil.SetElementText(itemdom.DocumentElement,
                            "comment", strComment);
                    }
                }
            }

            return 0;
        }

        #endregion

        // SetEntities() API �ָ�����
        /* ��־��¼��ʽ
<root>
  <operation>setEntity</operation> ��������
  <action>new</action> ���嶯������new change delete 3��
  <style>...</style> �����force nocheckdup noeventlog 3��
  <record recPath='����ͼ��ʵ��/3'><root><parent>2</parent><barcode>0000003</barcode><state>״̬2</state><location>������</location><price></price><bookType>��ѧ�ο�</bookType><registerNo></registerNo><comment>test</comment><mergeComment></mergeComment><batchNo>111</batchNo><borrower></borrower><borrowDate></borrowDate><borrowPeriod></borrowPeriod></root></record> ��¼��
  <oldRecord recPath='����ͼ��ʵ��/3'>...</oldRecord> �����ǻ���ɾ���ļ�¼ ����Ϊchange��deleteʱ�߱���Ԫ��
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 08:41:46 GMT</operTime> ����ʱ��
</root>

ע��1) ��<action>Ϊdeleteʱ��û��<record>Ԫ�ء�Ϊnewʱ��û��<oldRecord>Ԫ�ء�
	2) <record>�е�����, �漰����ͨ��<borrower><borrowDate><borrowPeriod>��, ����־�ָ��׶�, ��Ӧ����Ч, �⼸������Ӧ���ӵ�ǰλ�ÿ��м�¼��ȡ, ��<record>���������ݺϲ���, ��д�����ݿ�
	3) һ��SetEntities()API����, ���ܴ���������־��¼��
         
         * */
        // TODO: Ҫ����style��force nocheckdup����
        public int RecoverSetEntity(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            out string strError)
        {
            strError = "";

            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            bool bReuse = false;    // �Ƿ��ܹ�����RecorverLevel״̬�����ò��ִ���

        DO_SNAPSHOT:

            string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                "action");

            // ���ջָ�
            if (level == RecoverLevel.Snapshot
                || bReuse == true)
            {

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                if (strAction == "new" 
                    || strAction == "change"
                    || strAction == "move")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }

                    string strNewRecPath = DomUtil.GetAttr(node, "recPath");

                    // 
                    string strOldRecord = "";
                    string strOldRecPath = "";
                    if (strAction == "move")
                    {
                        strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                            "oldRecord",
                            out node);
                        if (node == null)
                        {
                            strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                            return -1;
                        }

                        strOldRecPath = DomUtil.GetAttr(node, "recPath");
                    }

                    // д���¼
                    lRet = channel.DoSaveTextRes(strNewRecPath,
                        strRecord,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "д����¼ '" + strNewRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }

                    if (strAction == "move")
                    {
                        // ɾ�����¼
                        int nRedoCount = 0;

                    REDO_DELETE:
                        lRet = channel.DoDeleteRes(strOldRecPath,
                            timestamp,
                            out output_timestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                return 0;   // ��¼�����Ͳ�����

                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                if (nRedoCount < 10)
                                {
                                    timestamp = output_timestamp;
                                    nRedoCount++;
                                    goto REDO_DELETE;
                                }
                            }
                            strError = "ɾ�����¼ '" + strOldRecPath + "' ʱ��������: " + strError;
                            return -1;

                        }
                    }
                     
                }
                else if (strAction == "delete")
                {
                    XmlNode node = null;
                    string strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                        return -1;
                    }
                    string strRecPath = DomUtil.GetAttr(node, "recPath");

                    int nRedoCount = 0;
                REDO:
                    // ɾ�����¼
                    lRet = channel.DoDeleteRes(strRecPath,
                        timestamp,
                        out output_timestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            return 0;   // ��¼�����Ͳ�����
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            if (nRedoCount < 10)
                            {
                                timestamp = output_timestamp;
                                nRedoCount++;
                                goto REDO;
                            }
                        }
                        strError = "ɾ�����¼ '" + strRecPath + "' ʱ��������: " + strError;
                        return -1;

                    }
                }
                else
                {
                    strError = "�޷�ʶ���<action>���� '" + strAction + "'";
                    return -1;
                }


                return 0;
            }

            bool bForce = false;
            bool bNoCheckDup = false;

            string strStyle = DomUtil.GetElementText(domLog.DocumentElement,
                "style");

            if (StringUtil.IsInList("force", strStyle) == true)
                bForce = true;

            if (StringUtil.IsInList("nocheckdup", strStyle) == true)
                bNoCheckDup = true;

            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {



                // �����ݿ������м�¼�ϲ���Ȼ�󱣴�
                if (strAction == "new"
                    || strAction == "change"
                    || strAction == "move")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }

                    string strNewRecPath = DomUtil.GetAttr(node, "recPath");

                    // 
                    string strOldRecord = "";
                    string strOldRecPath = "";
                    if (strAction == "move")
                    {
                        strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                            "oldRecord",
                            out node);
                        if (node == null)
                        {
                            strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                            return -1;
                        }

                        strOldRecPath = DomUtil.GetAttr(node, "recPath");
                    }


                    // �������ݿ���ԭ�еļ�¼
                    string strExistXml = "";
                    string strMetaData = "";
                    byte[] exist_timestamp = null;
                    string strOutputPath = "";

                    if ((strAction == "change" 
                        || strAction == "move")
                        && bForce == false) // 2008/10/6
                    {
                        string strSourceRecPath = "";

                        if (strAction == "change")
                            strSourceRecPath = strNewRecPath;
                        if (strAction == "move")
                            strSourceRecPath = strOldRecPath;

                        lRet = channel.GetRes(strSourceRecPath,
                            out strExistXml,
                            out strMetaData,
                            out exist_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            // �ݴ�
                            if (channel.ErrorCode == ChannelErrorCode.NotFound
                                && level == RecoverLevel.LogicAndSnapshot)
                            {
                                // �����¼������, ����һ���յļ�¼
                                // bExist = false;
                                strExistXml = "<root />";
                                exist_timestamp = null;
                            }
                            else
                            {
                                strError = "�ڶ���ԭ�м�¼ '"+strNewRecPath+"' ʱʧ��: " + strError;
                                goto ERROR1;
                            }
                        }
                    }

                    //
                    // ��������¼װ��DOM

                    XmlDocument domExist = new XmlDocument();
                    XmlDocument domNew = new XmlDocument();

                    try
                    {
                        // �����ռ�¼
                        if (String.IsNullOrEmpty(strExistXml) == true)
                            strExistXml = "<root />";

                        domExist.LoadXml(strExistXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "strExistXmlװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    try
                    {
                        domNew.LoadXml(strRecord);
                    }
                    catch (Exception ex)
                    {
                        strError = "strRecordװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    // �ϲ��¾ɼ�¼
                    string strNewXml = "";

                    if (bForce == false)
                    {
                        nRet = MergeTwoEntityXml(domExist,
                            domNew,
                            out strNewXml,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else
                    {
                        strNewXml = domNew.OuterXml;
                    }

                    // �����¼�¼
                    byte[] output_timestamp = null;

                    if (strAction == "move")
                    {
                        // ����Դ��¼��Ŀ��λ�ã�Ȼ���Զ�ɾ��Դ��¼
                        // ������δ��Ŀ��λ��д����������
                        lRet = channel.DoCopyRecord(strOldRecPath,
                            strNewRecPath,
                            true,   // bDeleteSourceRecord
                            out output_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;

                        exist_timestamp = output_timestamp; // ��ʱ����ʱ���
                    }


                    lRet = channel.DoSaveTextRes(strNewRecPath,
                        strNewXml,
                        false,   // include preamble?
                        "content,ignorechecktimestamp",
                        exist_timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    /*
                    if (strAction == "move")
                    {
                        // ɾ�����¼
                        int nRedoCount = 0;

                        byte[] timestamp = null;

                    REDO_DELETE:
                        lRet = channel.DoDeleteRes(strOldRecPath,
                            timestamp,
                            out output_timestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                return 0;   // ��¼�����Ͳ�����

                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                if (nRedoCount < 10)
                                {
                                    timestamp = output_timestamp;
                                    nRedoCount++;
                                    goto REDO_DELETE;
                                }
                            }
                            strError = "ɾ�����¼ '" + strRecPath + "' ʱ��������: " + strError;
                            return -1;

                        }
                    }
                     * */
                }
                else if (strAction == "delete")
                {
                    // ��SnapShot��ʽ��ͬ
                    bReuse = true;
                    goto DO_SNAPSHOT;
                }
                else
                {
                    strError = "�޷�ʶ���<action>���� '" + strAction + "'";
                    return -1;
                }
            }

            // �ݴ�ָ�
            if (level == RecoverLevel.Robust)
            {
                if (strAction == "move")
                {
                    strError = "�ݲ�֧��SetEntity��move�ָ�����";
                    return -1;
                }

                // �����ݿ������м�¼�ϲ���Ȼ�󱣴�
                if (strAction == "change" || strAction == "new")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }

                    // ȡ����־��¼�����Ƶ��¼�¼·�������������������·����
                    string strNewRecPath = DomUtil.GetAttr(node, "recPath");

                    // 
                    string strOldRecord = "";
                    string strOldRecPath = "";

                    string strOldItemBarcode = "";
                    string strNewItemBarcode = "";


                    string strExistXml = "";
                    byte[] exist_timestamp = null;


                    // ��־��¼�м��صľɼ�¼��
                    strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldRecord",
                        out node);
                    if (node == null)
                    {
                        if (strAction == "change")
                        {
                            strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                            return -1;
                        }
                    }

                    // ��־��¼�����Ƶľɼ�¼·�������������������·����
                    if (node != null)
                        strOldRecPath = DomUtil.GetAttr(node, "recPath");


                    // ����־��¼�м��صľɼ�¼���У���þɼ�¼�������
                    if (String.IsNullOrEmpty(strOldRecord) == false)
                    {
                        nRet = GetItemBarcode(strOldRecord,
                            out strOldItemBarcode,
                            out strError);
                    }

                    nRet = GetItemBarcode(strRecord,
                        out strNewItemBarcode,
                        out strError);

                    // TODO: ��Ҫ����¾ɼ�¼�У�<barcode>�Ƿ�һ�£������һ�£�����Ҫ��������Ž��в��أ�
                    if (strAction == "new" && strOldItemBarcode == "")
                    {
                        if (String.IsNullOrEmpty(strNewItemBarcode) == true)
                        {
                            strError = "��Ϊ���´����ļ�¼������û�а���������ţ�����new����������";
                            return -1;
                        }

                        strOldItemBarcode = strNewItemBarcode;
                    }


                    // ����оɼ�¼�Ĳ�����ţ�����Ҫ�����ݿ�����ȡ�����ʵľɼ�¼
                    // (���û�оɼ�¼�Ĳ�����ţ�������־��¼�еľɼ�¼)
                    if (String.IsNullOrEmpty(strOldItemBarcode) == false)
                    {
                        string strOutputItemRecPath = "";

                        // �Ӳ�����Ż�ò��¼
                        List<string> aPath = null;

                        // ��ò��¼
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   ����1��
                        //      >1  ���ж���1��
                        nRet = this.GetItemRecXml(
                            Channels,
                            strOldItemBarcode,
                            out strExistXml,
                            100,
                            out aPath,
                            out exist_timestamp,
                            out strError);
                        if (nRet == 0 || nRet == -1)
                        {
                            if (strAction == "change")
                            {
                                /*
                                // �ӿ���û���ҵ���ֻ������־��¼�м��صľɼ�¼
                                strExistXml = strOldRecord;
                                 * */
                                strExistXml = "";

                                // ��Ҫ����һ���¼�¼��strOldRecPath�е�·���ƺ�Ҳ�����ã�����Ҫ�ϸ������·���Ƿ��Ѿ����ڼ�¼ -- ֻ��������λ�ò����ڼ�¼ʱ�����á���Ȼ����鷳���ǾͲ��紿����һ����λ��
                                strOutputItemRecPath = ResPath.GetDbName(strOldRecPath) + "/?";
                            }
                            else
                            {
                                Debug.Assert(strAction == "new", "");
                                strExistXml = "";
                                strOutputItemRecPath = ResPath.GetDbName(strNewRecPath) + "/?";
                            }
                        }
                        else
                        {
                            // �ҵ�һ�����߶����ɼ�¼
                            Debug.Assert(aPath != null && aPath.Count >= 1, "");

                            bool bNeedReload = false;

                            if (aPath.Count == 1)
                            {
                                Debug.Assert(nRet == 1, "");

                                strOutputItemRecPath = aPath[0];

                                // �Ƿ���Ҫ����װ�أ�
                                bNeedReload = false;    // ��ȡ�õĵ�һ��·�������¼�Ѿ�װ��
                            }
                            else
                            {
                                // ����
                                Debug.Assert(aPath.Count > 1, "");

                                ///
                                // �������strOldRecPath��������ѡ
                                if (String.IsNullOrEmpty(strOldRecPath) == true)
                                {
                                    // �գ��޷���ѡ

                                    // �ݴ�
                                    strOutputItemRecPath = aPath[0];

                                    // �Ƿ���Ҫ����װ�أ�
                                    bNeedReload = false;    // ��ȡ�õĵ�һ��·�������¼�Ѿ�װ��
                                }
                                else
                                {

                                    ///// 
                                    nRet = aPath.IndexOf(strOldRecPath);
                                    if (nRet != -1)
                                    {
                                        // ѡ��
                                        strOutputItemRecPath = aPath[nRet];

                                        // �Ƿ���Ҫ����װ�أ�
                                        if (nRet != 0)
                                            bNeedReload = true; // ��һ�������·������Ҫװ��

                                    }
                                    else
                                    {
                                        // û��ѡ�У�ֻ������һ��

                                        // �ݴ�
                                        strOutputItemRecPath = aPath[0];

                                        // �Ƿ���Ҫ����װ�أ�
                                        bNeedReload = false;    // ��ȡ�õĵ�һ��·�������¼�Ѿ�װ��
                                    }
                                }

                                ///

                            }

                            // ����װ��
                            if (bNeedReload == true)
                            {
                                string strMetaData = "";
                                lRet = channel.GetRes(strOutputItemRecPath,
                                    out strExistXml,
                                    out strMetaData,
                                    out exist_timestamp,
                                    out strOutputItemRecPath,
                                    out strError);
                                if (lRet == -1)
                                {
                                    strError = "����strOutputItemRecPath '" + strOutputItemRecPath + "' ���»�ò��¼ʧ��: " + strError;
                                    return -1;
                                }

                                // ��Ҫ����¼�е�<barcode>Ԫ��ֵ�Ƿ�ƥ��������

                            }

                        }

                        // ����strOldRecPath
                        if (strOutputItemRecPath != "")
                            strOldRecPath = strOutputItemRecPath;
                        else
                            strOldRecPath = ""; // �ƻ�����������汻��

                        strNewRecPath = strOutputItemRecPath;

                    } // end if ����оɼ�¼�Ĳ������
                    else
                    {
                        // (���û�оɼ�¼�Ĳ�����ţ�������־��¼�еľɼ�¼)
                        // ���޷�ȷ���ɼ�¼��·����Ҳ���޷�ȷ������λ�á���˽�����������ض��ġ��޸Ĳ�������
                        strError = "��Ϊ��־��¼��û�м��ؾɼ�¼����ţ�����޷�ȷ����¼λ�ã����change����������";
                        return -1;
                    }

                    if (strAction == "change")
                    {
                        if (strNewItemBarcode != ""
                            && strNewItemBarcode != strOldItemBarcode)
                        {
                            // �¾ɼ�¼������Ų�һ�£���Ҫ��������Ž��в���
                            List<string> aPath = null;

                            string strTempXml = "";
                            byte[] temp_timestamp = null;
                            // ��ò��¼
                            // return:
                            //      -1  error
                            //      0   not found
                            //      1   ����1��
                            //      >1  ���ж���1��
                            nRet = this.GetItemRecXml(
                                Channels,
                                strNewItemBarcode,
                                out strTempXml,
                                100,
                                out aPath,
                                out temp_timestamp,
                                out strError);
                            if (nRet > 0)
                            {
                                // ���ظ���ȡ���һ������Ϊ�ϼ�¼���кϲ����������������λ��
                                strNewRecPath = aPath[0];
                                exist_timestamp = temp_timestamp;
                                strExistXml = strTempXml;
                            }
                        }
                    }


                    // ��������¼װ��DOM
                    XmlDocument domExist = new XmlDocument();
                    XmlDocument domNew = new XmlDocument();

                    try
                    {
                        // �����ռ�¼
                        if (String.IsNullOrEmpty(strExistXml) == true)
                            strExistXml = "<root />";

                        domExist.LoadXml(strExistXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "strExistXmlװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    try
                    {
                        domNew.LoadXml(strRecord);
                    }
                    catch (Exception ex)
                    {
                        strError = "strRecordװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }




                    // �ϲ��¾ɼ�¼
                    string strNewXml = "";

                    if (bForce == false)
                    {
                        nRet = MergeTwoEntityXml(domExist,
                            domNew,
                            out strNewXml,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else
                    {
                        strNewXml = domNew.OuterXml;
                    }

                    // �����¼�¼
                    byte[] output_timestamp = null;

                    string strOutputPath = "";

                    if (strAction == "move")
                    {
                        // ����Դ��¼��Ŀ��λ�ã�Ȼ���Զ�ɾ��Դ��¼
                        // ������δ��Ŀ��λ��д����������
                        lRet = channel.DoCopyRecord(strOldRecPath,
                            strNewRecPath,
                            true,   // bDeleteSourceRecord
                            out output_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;

                        exist_timestamp = output_timestamp; // ��ʱ����ʱ���
                    }

                    /*
                    // ����
                    {
                        string strRecID = ResPath.GetRecordId(strNewRecPath);

                        if (strRecID != "?")
                        {
                            try
                            {
                                long id = Convert.ToInt64(strRecID);
                                if (id > 150848)
                                {
                                    Debug.Assert(false, "id����β��");
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                     * */

                    lRet = channel.DoSaveTextRes(strNewRecPath,
                        strNewXml,
                        false,   // include preamble?
                        "content,ignorechecktimestamp",
                        exist_timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;


                }
                else if (strAction == "delete")
                {
                    XmlNode node = null;
                    string strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                        return -1;
                    }
                    string strRecPath = DomUtil.GetAttr(node, "recPath");

                    string strOldItemBarcode = "";
                    nRet = GetItemBarcode(strOldRecord,
                        out strOldItemBarcode,
                        out strError);
                    if (String.IsNullOrEmpty(strOldItemBarcode) == true)
                    {
                        strError = "��Ϊ��־��¼�еľɼ�¼��ȱ���ǿյ�<barcode>���ݣ������޷�������������Ŷ�λ��ɾ����delete����������";
                        return -1;
                    }

                    string strOutputItemRecPath = "";
                    string strExistXml = "";
                    byte[] exist_timestamp = null;

                    // �Ӳ�����Ż�ò��¼
                    List<string> aPath = null;

                    // ��ò��¼
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   ����1��
                    //      >1  ���ж���1��
                    nRet = this.GetItemRecXml(
                        Channels,
                        strOldItemBarcode,
                        out strExistXml,
                        100,
                        out aPath,
                        out exist_timestamp,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                    {
                        // �����Ͳ�����
                        return 0;
                    }
                    if (nRet >= 1)
                    {
                        ///
                        // �ҵ�һ�����߶����ɼ�¼
                        Debug.Assert(aPath != null && aPath.Count >= 1, "");

                        bool bNeedReload = false;

                        if (aPath.Count == 1)
                        {
                            Debug.Assert(nRet == 1, "");

                            /*
                            strOutputItemRecPath = aPath[0];

                            // �Ƿ���Ҫ����װ�أ�
                            bNeedReload = false;    // ��ȡ�õĵ�һ��·�������¼�Ѿ�װ��
                             * */
                            strError = "������� " + strOldItemBarcode + " Ŀǰ����Ψһһ����¼������ɾ��";
                            return -1;
                        }
                        else
                        {
                            // ����
                            Debug.Assert(aPath.Count > 1, "");

                            ///
                            // �������strRecPath��������ѡ
                            if (String.IsNullOrEmpty(strRecPath) == true)
                            {
                                strError = "������� '" + strOldItemBarcode + "' ���� " + aPath.Count.ToString() + " ����¼����<oldRecord>��recPath����ȱ��������޷����о�ȷɾ����delete����������";
                                return -1;
                            }
                            else
                            {

                                ///// 
                                nRet = aPath.IndexOf(strRecPath);
                                if (nRet != -1)
                                {
                                    // ѡ��
                                    strOutputItemRecPath = aPath[nRet];

                                    // �Ƿ���Ҫ����װ�أ�
                                    if (nRet != 0)
                                        bNeedReload = true; // ��һ�������·������Ҫװ��

                                }
                                else
                                {
                                    strError = "������� '" + strOldItemBarcode + "' ���� " + aPath.Count.ToString() + " ����¼��������(<oldRecord>Ԫ��������recPath��)ȷ��·�� '" + strRecPath + "' Ҳ�޷�ȷ�ϳ�����һ�����޷���ȷɾ�������delete����������";
                                    return -1;
                                }
                            }



                        }

                        ///

                        // ����װ��
                        if (bNeedReload == true)
                        {
                            string strMetaData = "";
                            lRet = channel.GetRes(strOutputItemRecPath,
                                out strExistXml,
                                out strMetaData,
                                out exist_timestamp,
                                out strOutputItemRecPath,
                                out strError);
                            if (lRet == -1)
                            {
                                strError = "����strOutputItemRecPath '" + strOutputItemRecPath + "' ���»�ò��¼ʧ��: " + strError;
                                return -1;
                            }

                            // ��Ҫ����¼�е�<barcode>Ԫ��ֵ�Ƿ�ƥ��������

                        }

                    }

                    // ��������¼װ��DOM
                    XmlDocument domExist = new XmlDocument();
                    try
                    {
                        // �����ռ�¼
                        if (String.IsNullOrEmpty(strExistXml) == true)
                            strExistXml = "<root />";

                        domExist.LoadXml(strExistXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "strExistXmlװ�ؽ���DOMʱ��������: " + ex.Message;
                        return -1;
                    }

                    string strDetail = "";
                    bool bHasCirculationInfo = IsEntityHasCirculationInfo(domExist,
                        out strDetail);


                    // �۲��Ѿ����ڵļ�¼�Ƿ�����ͨ��Ϣ
                    if (bHasCirculationInfo == true
                        && bForce == false)
                    {
                        strError = "��ɾ���Ĳ��¼ '" + strOutputItemRecPath + "' �а�������ͨ��Ϣ("+strDetail+")������ɾ����";
                        goto ERROR1;
                    }

                    int nRedoCount = 0;
                    byte[] timestamp = exist_timestamp;
                    byte[] output_timestamp = null; 

                REDO:
                    // ɾ�����¼
                    lRet = channel.DoDeleteRes(strOutputItemRecPath,
                        timestamp,
                        out output_timestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            return 0;   // ��¼�����Ͳ�����
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            if (nRedoCount < 10)
                            {
                                timestamp = output_timestamp;
                                nRedoCount++;
                                goto REDO;
                            }
                        }
                        strError = "ɾ�����¼ '" + strRecPath + "' ʱ��������: " + strError;
                        return -1;

                    }
                }
                else
                {
                    strError = "�޷�ʶ���<action>���� '" + strAction + "'";
                    return -1;
                }
            }

            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        // SetOrders() API �ָ�����
        /* ��־��¼��ʽ
<root>
  <operation>setOrder</operation> ��������
  <action>new</action> ���嶯������new change delete 3��
  <style>...</style> �����force nocheckdup noeventlog 3��
  <record recPath='����ͼ�鶩��/3'><root><parent>2</parent><barcode>0000003</barcode><state>״̬2</state><location>������</location><price></price><bookType>��ѧ�ο�</bookType><registerNo></registerNo><comment>test</comment><mergeComment></mergeComment><batchNo>111</batchNo><borrower></borrower><borrowDate></borrowDate><borrowPeriod></borrowPeriod></root></record> ��¼��
  <oldRecord recPath='����ͼ�鶩��/3'>...</oldRecord> �����ǻ���ɾ���ļ�¼ ����Ϊchange��deleteʱ�߱���Ԫ��
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 08:41:46 GMT</operTime> ����ʱ��
</root>

ע��1) ��<action>Ϊdeleteʱ��û��<record>Ԫ�ء�Ϊnewʱ��û��<oldRecord>Ԫ�ء�
	2) һ��SetOrders()API����, ���ܴ���������־��¼��
         
         * */
        // TODO: Ҫ����style��force nocheckdup����
        public int RecoverSetOrder(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            out string strError)
        {
            strError = "";

            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            bool bReuse = false;    // �Ƿ��ܹ�����RecorverLevel״̬�����ò��ִ���

        DO_SNAPSHOT:

            string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                "action");

            // ���ջָ�
            if (level == RecoverLevel.Snapshot
                || bReuse == true)
            {

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                if (strAction == "new"
                    || strAction == "change"
                    || strAction == "move")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }

                    string strNewRecPath = DomUtil.GetAttr(node, "recPath");

                    // 
                    string strOldRecord = "";
                    string strOldRecPath = "";
                    if (strAction == "move")
                    {
                        strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                            "oldRecord",
                            out node);
                        if (node == null)
                        {
                            strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                            return -1;
                        }

                        strOldRecPath = DomUtil.GetAttr(node, "recPath");
                    }

                    // д������¼
                    lRet = channel.DoSaveTextRes(strNewRecPath,
                        strRecord,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "д�붩����¼ '" + strNewRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }

                    if (strAction == "move")
                    {
                        // ɾ��������¼
                        int nRedoCount = 0;

                    REDO_DELETE:
                        lRet = channel.DoDeleteRes(strOldRecPath,
                            timestamp,
                            out output_timestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                return 0;   // ��¼�����Ͳ�����

                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                if (nRedoCount < 10)
                                {
                                    timestamp = output_timestamp;
                                    nRedoCount++;
                                    goto REDO_DELETE;
                                }
                            }
                            strError = "ɾ��������¼ '" + strOldRecPath + "' ʱ��������: " + strError;
                            return -1;

                        }
                    }

                }
                else if (strAction == "delete")
                {
                    XmlNode node = null;
                    string strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                        return -1;
                    }
                    string strRecPath = DomUtil.GetAttr(node, "recPath");

                    int nRedoCount = 0;
                REDO:
                    // ɾ��������¼
                    lRet = channel.DoDeleteRes(strRecPath,
                        timestamp,
                        out output_timestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            return 0;   // ��¼�����Ͳ�����
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            if (nRedoCount < 10)
                            {
                                timestamp = output_timestamp;
                                nRedoCount++;
                                goto REDO;
                            }
                        }
                        strError = "ɾ��������¼ '" + strRecPath + "' ʱ��������: " + strError;
                        return -1;

                    }
                }
                else
                {
                    strError = "�޷�ʶ���<action>���� '" + strAction + "'";
                    return -1;
                }


                return 0;
            }

            bool bForce = false;
            bool bNoCheckDup = false;

            string strStyle = DomUtil.GetElementText(domLog.DocumentElement,
                "style");

            if (StringUtil.IsInList("force", strStyle) == true)
                bForce = true;

            if (StringUtil.IsInList("nocheckdup", strStyle) == true)
                bNoCheckDup = true;

            // �߼��ָ����߻�ϻָ������ݴ�ָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot
                || level == RecoverLevel.Robust)    // �ݴ�ָ�û�е���ʵ��
            {
                // �����ݿ������м�¼�ϲ���Ȼ�󱣴�
                if (strAction == "new"
                    || strAction == "change"
                    || strAction == "move")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }

                    string strNewRecPath = DomUtil.GetAttr(node, "recPath");

                    // 
                    string strOldRecord = "";
                    string strOldRecPath = "";
                    if (strAction == "move")
                    {
                        strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                            "oldRecord",
                            out node);
                        if (node == null)
                        {
                            strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                            return -1;
                        }

                        strOldRecPath = DomUtil.GetAttr(node, "recPath");
                    }


                    // �������ݿ���ԭ�еļ�¼
                    string strExistXml = "";
                    string strMetaData = "";
                    byte[] exist_timestamp = null;
                    string strOutputPath = "";

                    if ((strAction == "change"
                        || strAction == "move")
                        && bForce == false) 
                    {
                        string strSourceRecPath = "";

                        if (strAction == "change")
                            strSourceRecPath = strNewRecPath;
                        if (strAction == "move")
                            strSourceRecPath = strOldRecPath;

                        lRet = channel.GetRes(strSourceRecPath,
                            out strExistXml,
                            out strMetaData,
                            out exist_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            // �ݴ�
                            if (channel.ErrorCode == ChannelErrorCode.NotFound
                                && level == RecoverLevel.LogicAndSnapshot)
                            {
                                // �����¼������, ����һ���յļ�¼
                                // bExist = false;
                                strExistXml = "<root />";
                                exist_timestamp = null;
                            }
                            else
                            {
                                strError = "�ڶ���ԭ�м�¼ '" + strNewRecPath + "' ʱʧ��: " + strError;
                                goto ERROR1;
                            }
                        }
                    }

                    //
                    // ��������¼װ��DOM

                    XmlDocument domExist = new XmlDocument();
                    XmlDocument domNew = new XmlDocument();

                    try
                    {
                        // �����ռ�¼
                        if (String.IsNullOrEmpty(strExistXml) == true)
                            strExistXml = "<root />";

                        domExist.LoadXml(strExistXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "strExistXmlװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    try
                    {
                        domNew.LoadXml(strRecord);
                    }
                    catch (Exception ex)
                    {
                        strError = "strRecordװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    // �ϲ��¾ɼ�¼
                    string strNewXml = "";

                    if (bForce == false)
                    {
                        nRet = this.OrderItemDatabase.MergeTwoItemXml(
                            null,
                            domExist,
                            domNew,
                            out strNewXml,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else
                    {
                        strNewXml = domNew.OuterXml;
                    }

                    // �����¼�¼
                    byte[] output_timestamp = null;

                    if (strAction == "move")
                    {
                        // ����Դ��¼��Ŀ��λ�ã�Ȼ���Զ�ɾ��Դ��¼
                        // ������δ��Ŀ��λ��д����������
                        lRet = channel.DoCopyRecord(strOldRecPath,
                            strNewRecPath,
                            true,   // bDeleteSourceRecord
                            out output_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;

                        exist_timestamp = output_timestamp; // ��ʱ����ʱ���
                    }


                    lRet = channel.DoSaveTextRes(strNewRecPath,
                        strNewXml,
                        false,   // include preamble?
                        "content,ignorechecktimestamp",
                        exist_timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;
                }
                else if (strAction == "delete")
                {
                    // ��SnapShot��ʽ��ͬ
                    bReuse = true;
                    goto DO_SNAPSHOT;
                }
                else
                {
                    strError = "�޷�ʶ���<action>���� '" + strAction + "'";
                    return -1;
                }
            }



            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }


        // SetIssues() API �ָ�����
        /* ��־��¼��ʽ
<root>
  <operation>setIssue</operation> ��������
  <action>new</action> ���嶯������new change delete 3��
  <style>...</style> �����force nocheckdup noeventlog 3��
  <record recPath='�����ڿ���/3'><root><parent>2</parent><barcode>0000003</barcode><state>״̬2</state><location>������</location><price></price><bookType>��ѧ�ο�</bookType><registerNo></registerNo><comment>test</comment><mergeComment></mergeComment><batchNo>111</batchNo><borrower></borrower><borrowDate></borrowDate><borrowPeriod></borrowPeriod></root></record> ��¼��
  <oldRecord recPath='�����ڿ���/3'>...</oldRecord> �����ǻ���ɾ���ļ�¼ ����Ϊchange��deleteʱ�߱���Ԫ��
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 08:41:46 GMT</operTime> ����ʱ��
</root>

ע��1) ��<action>Ϊdeleteʱ��û��<record>Ԫ�ء�Ϊnewʱ��û��<oldRecord>Ԫ�ء�
	2) һ��SetIssues()API����, ���ܴ���������־��¼��
         
         * */
        // TODO: Ҫ����style��force nocheckdup����
        public int RecoverSetIssue(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            out string strError)
        {
            strError = "";

            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            bool bReuse = false;    // �Ƿ��ܹ�����RecorverLevel״̬�����ò��ִ���

        DO_SNAPSHOT:

            string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                "action");

            // ���ջָ�
            if (level == RecoverLevel.Snapshot
                || bReuse == true)
            {

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                if (strAction == "new"
                    || strAction == "change"
                    || strAction == "move")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }

                    string strNewRecPath = DomUtil.GetAttr(node, "recPath");

                    // 
                    string strOldRecord = "";
                    string strOldRecPath = "";
                    if (strAction == "move")
                    {
                        strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                            "oldRecord",
                            out node);
                        if (node == null)
                        {
                            strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                            return -1;
                        }

                        strOldRecPath = DomUtil.GetAttr(node, "recPath");
                    }

                    // д�ڼ�¼
                    lRet = channel.DoSaveTextRes(strNewRecPath,
                        strRecord,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "д���ڼ�¼ '" + strNewRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }

                    if (strAction == "move")
                    {
                        // ɾ���ڼ�¼
                        int nRedoCount = 0;

                    REDO_DELETE:
                        lRet = channel.DoDeleteRes(strOldRecPath,
                            timestamp,
                            out output_timestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                return 0;   // ��¼�����Ͳ�����

                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                if (nRedoCount < 10)
                                {
                                    timestamp = output_timestamp;
                                    nRedoCount++;
                                    goto REDO_DELETE;
                                }
                            }
                            strError = "ɾ���ڼ�¼ '" + strOldRecPath + "' ʱ��������: " + strError;
                            return -1;

                        }
                    }

                }
                else if (strAction == "delete")
                {
                    XmlNode node = null;
                    string strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                        return -1;
                    }
                    string strRecPath = DomUtil.GetAttr(node, "recPath");

                    int nRedoCount = 0;
                REDO:
                    // ɾ���ڼ�¼
                    lRet = channel.DoDeleteRes(strRecPath,
                        timestamp,
                        out output_timestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            return 0;   // ��¼�����Ͳ�����
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            if (nRedoCount < 10)
                            {
                                timestamp = output_timestamp;
                                nRedoCount++;
                                goto REDO;
                            }
                        }
                        strError = "ɾ���ڼ�¼ '" + strRecPath + "' ʱ��������: " + strError;
                        return -1;

                    }
                }
                else
                {
                    strError = "�޷�ʶ���<action>���� '" + strAction + "'";
                    return -1;
                }


                return 0;
            }

            bool bForce = false;
            bool bNoCheckDup = false;

            string strStyle = DomUtil.GetElementText(domLog.DocumentElement,
                "style");

            if (StringUtil.IsInList("force", strStyle) == true)
                bForce = true;

            if (StringUtil.IsInList("nocheckdup", strStyle) == true)
                bNoCheckDup = true;

            // �߼��ָ����߻�ϻָ������ݴ�ָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot
                || level == RecoverLevel.Robust)    // �ݴ�ָ�û�е���ʵ��
            {
                // �����ݿ������м�¼�ϲ���Ȼ�󱣴�
                if (strAction == "new"
                    || strAction == "change"
                    || strAction == "move")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }

                    string strNewRecPath = DomUtil.GetAttr(node, "recPath");

                    // 
                    string strOldRecord = "";
                    string strOldRecPath = "";
                    if (strAction == "move")
                    {
                        strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                            "oldRecord",
                            out node);
                        if (node == null)
                        {
                            strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                            return -1;
                        }

                        strOldRecPath = DomUtil.GetAttr(node, "recPath");
                    }


                    // �������ݿ���ԭ�еļ�¼
                    string strExistXml = "";
                    string strMetaData = "";
                    byte[] exist_timestamp = null;
                    string strOutputPath = "";

                    if ((strAction == "change"
                        || strAction == "move")
                        && bForce == false)
                    {
                        string strSourceRecPath = "";

                        if (strAction == "change")
                            strSourceRecPath = strNewRecPath;
                        if (strAction == "move")
                            strSourceRecPath = strOldRecPath;

                        lRet = channel.GetRes(strSourceRecPath,
                            out strExistXml,
                            out strMetaData,
                            out exist_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            // �ݴ�
                            if (channel.ErrorCode == ChannelErrorCode.NotFound
                                && level == RecoverLevel.LogicAndSnapshot)
                            {
                                // �����¼������, ����һ���յļ�¼
                                // bExist = false;
                                strExistXml = "<root />";
                                exist_timestamp = null;
                            }
                            else
                            {
                                strError = "�ڶ���ԭ�м�¼ '" + strNewRecPath + "' ʱʧ��: " + strError;
                                goto ERROR1;
                            }
                        }
                    }

                    //
                    // ��������¼װ��DOM

                    XmlDocument domExist = new XmlDocument();
                    XmlDocument domNew = new XmlDocument();

                    try
                    {
                        // �����ռ�¼
                        if (String.IsNullOrEmpty(strExistXml) == true)
                            strExistXml = "<root />";

                        domExist.LoadXml(strExistXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "strExistXmlװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    try
                    {
                        domNew.LoadXml(strRecord);
                    }
                    catch (Exception ex)
                    {
                        strError = "strRecordװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    // �ϲ��¾ɼ�¼
                    string strNewXml = "";

                    if (bForce == false)
                    {
                        nRet = this.IssueItemDatabase.MergeTwoItemXml(
                            null,
                            domExist,
                            domNew,
                            out strNewXml,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else
                    {
                        strNewXml = domNew.OuterXml;
                    }

                    // �����¼�¼
                    byte[] output_timestamp = null;

                    if (strAction == "move")
                    {
                        // ����Դ��¼��Ŀ��λ�ã�Ȼ���Զ�ɾ��Դ��¼
                        // ������δ��Ŀ��λ��д����������
                        lRet = channel.DoCopyRecord(strOldRecPath,
                            strNewRecPath,
                            true,   // bDeleteSourceRecord
                            out output_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;

                        exist_timestamp = output_timestamp; // ��ʱ����ʱ���
                    }


                    lRet = channel.DoSaveTextRes(strNewRecPath,
                        strNewXml,
                        false,   // include preamble?
                        "content,ignorechecktimestamp",
                        exist_timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;
                }
                else if (strAction == "delete")
                {
                    // ��SnapShot��ʽ��ͬ
                    bReuse = true;
                    goto DO_SNAPSHOT;
                }
                else
                {
                    strError = "�޷�ʶ���<action>���� '" + strAction + "'";
                    return -1;
                }
            }

            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        // SetComments() API �ָ�����
        /* ��־��¼��ʽ
<root>
  <operation>setComment</operation> ��������
  <action>new</action> ���嶯������new change delete 3��
  <style>...</style> �����force nocheckdup noeventlog 3��
  <record recPath='����ͼ����ע/3'>...</record> ��¼��
  <oldRecord recPath='����ͼ����ע/3'>...</oldRecord> �����ǻ���ɾ���ļ�¼ ����Ϊchange��deleteʱ�߱���Ԫ��
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 08:41:46 GMT</operTime> ����ʱ��
</root>

ע��1) ��<action>Ϊdeleteʱ��û��<record>Ԫ�ء�Ϊnewʱ��û��<oldRecord>Ԫ�ء�
	2) һ��SetComments()API����, ���ܴ���������־��¼��
         
         * */
        // TODO: Ҫ����style��force nocheckdup����
        public int RecoverSetComment(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            out string strError)
        {
            strError = "";

            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            bool bReuse = false;    // �Ƿ��ܹ�����RecorverLevel״̬�����ò��ִ���

        DO_SNAPSHOT:

            string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                "action");

            // ���ջָ�
            if (level == RecoverLevel.Snapshot
                || bReuse == true)
            {

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                if (strAction == "new"
                    || strAction == "change"
                    || strAction == "move")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }

                    string strNewRecPath = DomUtil.GetAttr(node, "recPath");

                    // 
                    string strOldRecord = "";
                    string strOldRecPath = "";
                    if (strAction == "move")
                    {
                        strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                            "oldRecord",
                            out node);
                        if (node == null)
                        {
                            strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                            return -1;
                        }

                        strOldRecPath = DomUtil.GetAttr(node, "recPath");
                    }

                    // д��ע��¼
                    lRet = channel.DoSaveTextRes(strNewRecPath,
                        strRecord,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "д����ע��¼ '" + strNewRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }

                    if (strAction == "move")
                    {
                        // ɾ����ע��¼
                        int nRedoCount = 0;

                    REDO_DELETE:
                        lRet = channel.DoDeleteRes(strOldRecPath,
                            timestamp,
                            out output_timestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                return 0;   // ��¼�����Ͳ�����

                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                if (nRedoCount < 10)
                                {
                                    timestamp = output_timestamp;
                                    nRedoCount++;
                                    goto REDO_DELETE;
                                }
                            }
                            strError = "ɾ����ע��¼ '" + strOldRecPath + "' ʱ��������: " + strError;
                            return -1;

                        }
                    }

                }
                else if (strAction == "delete")
                {
                    XmlNode node = null;
                    string strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                        return -1;
                    }
                    string strRecPath = DomUtil.GetAttr(node, "recPath");

                    int nRedoCount = 0;
                REDO:
                    // ɾ����ע��¼
                    lRet = channel.DoDeleteRes(strRecPath,
                        timestamp,
                        out output_timestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            return 0;   // ��¼�����Ͳ�����
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            if (nRedoCount < 10)
                            {
                                timestamp = output_timestamp;
                                nRedoCount++;
                                goto REDO;
                            }
                        }
                        strError = "ɾ����ע��¼ '" + strRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }
                }
                else
                {
                    strError = "�޷�ʶ���<action>���� '" + strAction + "'";
                    return -1;
                }


                return 0;
            }

            bool bForce = false;
            bool bNoCheckDup = false;

            string strStyle = DomUtil.GetElementText(domLog.DocumentElement,
                "style");

            if (StringUtil.IsInList("force", strStyle) == true)
                bForce = true;

            if (StringUtil.IsInList("nocheckdup", strStyle) == true)
                bNoCheckDup = true;

            // �߼��ָ����߻�ϻָ������ݴ�ָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot
                || level == RecoverLevel.Robust)    // �ݴ�ָ�û�е���ʵ��
            {
                // �����ݿ������м�¼�ϲ���Ȼ�󱣴�
                if (strAction == "new"
                    || strAction == "change"
                    || strAction == "move")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }

                    string strNewRecPath = DomUtil.GetAttr(node, "recPath");

                    // 
                    string strOldRecord = "";
                    string strOldRecPath = "";
                    if (strAction == "move")
                    {
                        strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                            "oldRecord",
                            out node);
                        if (node == null)
                        {
                            strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                            return -1;
                        }

                        strOldRecPath = DomUtil.GetAttr(node, "recPath");
                    }


                    // �������ݿ���ԭ�еļ�¼
                    string strExistXml = "";
                    string strMetaData = "";
                    byte[] exist_timestamp = null;
                    string strOutputPath = "";

                    if ((strAction == "change"
                        || strAction == "move")
                        && bForce == false)
                    {
                        string strSourceRecPath = "";

                        if (strAction == "change")
                            strSourceRecPath = strNewRecPath;
                        if (strAction == "move")
                            strSourceRecPath = strOldRecPath;

                        lRet = channel.GetRes(strSourceRecPath,
                            out strExistXml,
                            out strMetaData,
                            out exist_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            // �ݴ�
                            if (channel.ErrorCode == ChannelErrorCode.NotFound
                                && level == RecoverLevel.LogicAndSnapshot)
                            {
                                // �����¼������, ����һ���յļ�¼
                                // bExist = false;
                                strExistXml = "<root />";
                                exist_timestamp = null;
                            }
                            else
                            {
                                strError = "�ڶ���ԭ�м�¼ '" + strNewRecPath + "' ʱʧ��: " + strError;
                                goto ERROR1;
                            }
                        }
                    }

                    //
                    // ��������¼װ��DOM

                    XmlDocument domExist = new XmlDocument();
                    XmlDocument domNew = new XmlDocument();

                    try
                    {
                        // �����ռ�¼
                        if (String.IsNullOrEmpty(strExistXml) == true)
                            strExistXml = "<root />";

                        domExist.LoadXml(strExistXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "strExistXmlװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    try
                    {
                        domNew.LoadXml(strRecord);
                    }
                    catch (Exception ex)
                    {
                        strError = "strRecordװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    // �ϲ��¾ɼ�¼
                    string strNewXml = "";

                    if (bForce == false)
                    {
                        nRet = this.CommentItemDatabase.MergeTwoItemXml(
                            null,
                            domExist,
                            domNew,
                            out strNewXml,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else
                    {
                        strNewXml = domNew.OuterXml;
                    }

                    // �����¼�¼
                    byte[] output_timestamp = null;

                    if (strAction == "move")
                    {
                        // ����Դ��¼��Ŀ��λ�ã�Ȼ���Զ�ɾ��Դ��¼
                        // ������δ��Ŀ��λ��д����������
                        lRet = channel.DoCopyRecord(strOldRecPath,
                            strNewRecPath,
                            true,   // bDeleteSourceRecord
                            out output_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;

                        exist_timestamp = output_timestamp; // ��ʱ����ʱ���
                    }


                    lRet = channel.DoSaveTextRes(strNewRecPath,
                        strNewXml,
                        false,   // include preamble?
                        "content,ignorechecktimestamp",
                        exist_timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;
                }
                else if (strAction == "delete")
                {
                    // ��SnapShot��ʽ��ͬ
                    bReuse = true;
                    goto DO_SNAPSHOT;
                }
                else
                {
                    strError = "�޷�ʶ���<action>���� '" + strAction + "'";
                    return -1;
                }
            }

            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        // ����ʵ���¼�е�<barcode>Ԫ��ֵ
        static int GetItemBarcode(string strXml,
            out string strItemBarcode,
            out string strError)
        {
            strItemBarcode = "";
            strError = "";

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "װ��XML����DOMʱ��������: " + ex.Message;
                return -1;
            }

            strItemBarcode = DomUtil.GetElementText(dom.DocumentElement, "barcode");

            return 1;
        }

        // ChangeReaderPassword() API �ָ�����
        /*
<root>
  <operation>changeReaderPassword</operation> 
  <readerBarcode>...</readerBarcode>	����֤�����
  <newPassword>5npAUJ67/y3aOvdC0r+Dj7SeXGE=</newPassword> 
  <operator>test</operator> 
  <operTime>Fri, 08 Dec 2006 09:01:38 GMT</operTime> 
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
</root>
         * */
        public int RecoverChangeReaderPassword(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            out string strError)
        {
            strError = "";

            // ��ʱ��Robust����Logic����
            if (level == RecoverLevel.Robust)
                level = RecoverLevel.Logic;


            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            DO_SNAPSHOT:

            // ���ջָ�
            if (level == RecoverLevel.Snapshot)
            {
                XmlNode node = null;
                string strReaderXml = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerRecord",
                    out node);
                if (node == null)
                {
                    strError = "��־��¼��ȱ<readerRecord>Ԫ��";
                    return -1;
                }
                string strReaderRecPath = DomUtil.GetAttr(node, "recPath");

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                // д���߼�¼
                lRet = channel.DoSaveTextRes(strReaderRecPath,
    strReaderXml,
    false,
    "content,ignorechecktimestamp",
    timestamp,
    out output_timestamp,
    out strOutputPath,
    out strError);
                if (lRet == -1)
                {
                    strError = "д����߼�¼ '" + strReaderRecPath + "' ʱ��������: " + strError;
                    return -1;
                }

                return 0;
            }

            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {

                // ����ԭ�ж��߼�¼���޸��������
                string strReaderBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerBarcode");
                if (String.IsNullOrEmpty(strReaderBarcode) == true)
                {
                    strError = "��־��¼��ȱ��<readerBarcode>Ԫ��";
                    goto ERROR1;
                }

                string strNewPassword = DomUtil.GetElementText(domLog.DocumentElement,
                    "newPassword");
                if (String.IsNullOrEmpty(strNewPassword) == true)
                {
                    strError = "��־��¼��ȱ��<newPassword>Ԫ��";
                    goto ERROR1;
                }

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
                    strError = "����֤����� '" + strReaderBarcode + "' ������";
                    goto ERROR1;
                }
                if (nRet == -1)
                {
                    strError = "����֤�����Ϊ '" + strReaderBarcode + "' �Ķ��߼�¼ʱ��������: " + strError;
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

                // strNewPassword�б�������SHA1��̬
                DomUtil.SetElementText(readerdom.DocumentElement,
                    "password", strNewPassword);

                byte[] output_timestamp = null;
                string strOutputPath = "";

                // д�ض��߼�¼
                lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                    readerdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    reader_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
            }


            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        // SetReaderInfo() API �ָ�����
        /*
<root>
	<operation>setReaderInfo</operation> ��������
	<action>...</action> ���嶯������new change delete move 4��
	<record recPath='...'>...</record> �¼�¼
    <oldRecord recPath='...'>...</oldRecord> �����ǻ���ɾ���ļ�¼ ����Ϊchange��deleteʱ�߱���Ԫ��
	<operator>test</operator> ������
	<operTime>Fri, 08 Dec 2006 09:01:38 GMT</operTime> ����ʱ��
</root>

ע: new ��ʱ��ֻ��<record>Ԫ�أ�delete��ʱ��ֻ��<oldRecord>Ԫ�أ�change��ʱ�����߶���

         * */
        public int RecoverSetReaderInfo(
            RmsChannelCollection Channels,
            RecoverLevel level_param,
            XmlDocument domLog,
            out string strError)
        {
            strError = "";

            string[] element_names = reader_element_names;

            RecoverLevel level = level_param;

            // ��ʱ��Robust����Logic����
            if (level == RecoverLevel.Robust)
                level = RecoverLevel.Logic;

            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            bool bReuse = false;    // �Ƿ��ܹ�����RecorverLevel״̬�����ò��ִ���

            DO_SNAPSHOT:

            string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                "action");

            // ���ջָ�
            if (level == RecoverLevel.Snapshot
                || bReuse == true)
            {
                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                if (strAction == "new" || strAction == "change")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }
                    string strRecPath = DomUtil.GetAttr(node, "recPath");

                    // д���߼�¼
                    lRet = channel.DoSaveTextRes(strRecPath,
        strRecord,
        false,
        "content,ignorechecktimestamp",
        timestamp,
        out output_timestamp,
        out strOutputPath,
        out strError);
                    if (lRet == -1)
                    {
                        strError = "д����߼�¼ '" + strRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }
                }
                else if (strAction == "delete")
                {
                    XmlNode node = null;
                    string strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                        return -1;
                    }
                    string strRecPath = DomUtil.GetAttr(node, "recPath");

                    int nRedoCount = 0;
                REDO:
                    // ɾ�����߼�¼
                    lRet = channel.DoDeleteRes(strRecPath,
                        timestamp,
                        out output_timestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            return 0;   // ��¼�����Ͳ�����
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            if (nRedoCount < 10)
                            {
                                timestamp = output_timestamp;
                                nRedoCount++;
                                goto REDO;
                            }
                        }
                        strError = "ɾ�����߼�¼ '" + strRecPath + "' ʱ��������: " + strError;
                        return -1;

                    }
                }
                else if (strAction == "move")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
    "record",
    out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }
                    string strRecPath = DomUtil.GetAttr(node, "recPath");
                    if (string.IsNullOrEmpty(strRecPath) == true)
                    {
                        strError = "��־��¼��<record>Ԫ����ȱrecPath����ֵ";
                        return -1;
                    }

                    string strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                        return -1;
                    }
                    string strOldRecPath = DomUtil.GetAttr(node, "recPath");
                    if (string.IsNullOrEmpty(strOldRecPath) == true)
                    {
                        strError = "��־��¼��<oldRecord>Ԫ����ȱrecPath����ֵ";
                        return -1;
                    }
                    /*
                    int nRedoCount = 0;
                REDO:
                     * */

                    // �ƶ����߼�¼
                    lRet = channel.DoCopyRecord(
                        strOldRecPath,
                        strRecPath,
                        true,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        // Դ��¼�����Ͳ����ڡ������ݴ���
                        if (channel.ErrorCode == ChannelErrorCode.NotFound
                            && level_param == RecoverLevel.Robust)
                        {
                            // ���������µļ�¼���ݸ�ԭ��ʵ��û�в��þɵļ�¼����
                            if (string.IsNullOrEmpty(strRecord) == true)
                                strRecord = strOldRecord;

                            if (string.IsNullOrEmpty(strRecord) == false)
                            {
                                // д���߼�¼
                                lRet = channel.DoSaveTextRes(strRecPath,
                    strRecord,
                    false,
                    "content,ignorechecktimestamp",
                    timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                                if (lRet == -1)
                                {
                                    strError = "Ϊ�ݴ�д����߼�¼ '" + strRecPath + "' ʱ��������: " + strError;
                                    return -1;
                                }

                                return 0;
                            }
                        }

                        strError = "�ƶ����߼�¼ '" + strOldRecPath + "' �� '" + strRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }

                    // <record>������м�¼�壬����Ҫд��һ��
                    // ����������Ҫע�⣬�ڴ�����־��¼��ʱ�����û����CopyRecord()��׷���޸Ĺ���¼����Ҫ����<record>��¼���Ĳ��֣���������������־�ָ�ʱд�붯��
                    if (string.IsNullOrEmpty(strRecord) == false)
                    {
                        lRet = channel.DoSaveTextRes(strRecPath,
            strRecord,
            false,
            "content,ignorechecktimestamp",
            timestamp,
            out output_timestamp,
            out strOutputPath,
            out strError);
                        if (lRet == -1)
                        {
                            strError = "д����߼�¼ '" + strRecPath + "' ʱ��������: " + strError;
                            return -1;
                        }
                    }
                }

                return 0;
            }

            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {
                // �����ݿ������м�¼�ϲ���Ȼ�󱣴�
                if (strAction == "new" || strAction == "change")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        return -1;
                    }

                    string strRecPath = DomUtil.GetAttr(node, "recPath");

                    // �������ݿ���ԭ�еļ�¼
                    string strExistXml = "";
                    string strMetaData = "";
                    byte[] exist_timestamp = null;
                    string strOutputPath = "";

                    if (strAction == "change")
                    {
                        lRet = channel.GetRes(strRecPath,
                            out strExistXml,
                            out strMetaData,
                            out exist_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            // �ݴ�
                            if (channel.ErrorCode == ChannelErrorCode.NotFound
                                && level == RecoverLevel.LogicAndSnapshot)
                            {
                                // �����¼������, ����һ���յļ�¼
                                // bExist = false;
                                strExistXml = "<root />";
                                exist_timestamp = null;
                            }
                            else
                            {
                                strError = "�ڶ���ԭ�м�¼ '" + strRecPath + "' ʱʧ��: " + strError;
                                goto ERROR1;
                            }
                        }
                    }

                    //
                    // ��������¼װ��DOM

                    XmlDocument domExist = new XmlDocument();
                    XmlDocument domNew = new XmlDocument();

                    try
                    {
                        // �����ռ�¼
                        if (String.IsNullOrEmpty(strExistXml) == true)
                            strExistXml = "<root />";

                        domExist.LoadXml(strExistXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "strExistXmlװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    try
                    {
                        domNew.LoadXml(strRecord);
                    }
                    catch (Exception ex)
                    {
                        strError = "strRecordװ�ؽ���DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    // �ϲ��¾ɼ�¼
                    string strNewXml = "";
                    nRet = MergeTwoReaderXml(
                        element_names,
                        "change",
                        domExist,
                        domNew,
                        out strNewXml,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // �����¼�¼
                    byte[] output_timestamp = null;
                    lRet = channel.DoSaveTextRes(strRecPath,
                        strNewXml,
                        false,   // include preamble?
                        "content,ignorechecktimestamp",
                        exist_timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        goto ERROR1;
                    }
                }
                else if (strAction == "delete")
                {
                    // ��SnapShot��ʽ��ͬ
                    bReuse = true;
                    goto DO_SNAPSHOT;
                }
                else if (strAction == "move")
                {
                    // ��SnapShot��ʽ��ͬ
                    bReuse = true;
                    goto DO_SNAPSHOT;
                }
                else
                {
                    strError = "�޷�ʶ���<action>���� '" + strAction + "'";
                    return -1;
                }
            }


            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        // Amerce() API �ָ�����
        /*
<root>
  <operation>amerce</operation> ��������
  <action>amerce</action> ���嶯������amerce undo modifyprice
  <readerBarcode>...</readerBarcode> ����֤�����
  <!-- <idList>...<idList> ID�б����ż�� �ѷ�ֹ -->
  <amerceItems>
	<amerceItem id="..." newPrice="..." newComment="..." /> newComment������׷�ӻ��滻ԭ����ע�����ݡ�������׷�ӻ��Ǹ��ǣ�ȡ���ڵ�һ���ַ��Ƿ�Ϊ'>'����'<'��ǰ��Ϊ׷��(��ʱ��һ���ַ�������������)�������һ���ַ�����������֮һ����Ĭ��Ϊ׷��
	...
  </amerceItems>
  <amerceRecord recPath='...'><root><itemBarcode>0000001</itemBarcode><readerBarcode>R0000002</readerBarcode><state>amerced</state><id>632958375041543888-1</id><over>31day</over><borrowDate>Sat, 07 Oct 2006 09:04:28 GMT</borrowDate><borrowPeriod>30day</borrowPeriod><returnDate>Thu, 07 Dec 2006 09:04:27 GMT</returnDate><returnOperator>test</returnOperator></root></amerceRecord> �ڷ�����д������¼�¼��ע��<amerceRecord>Ԫ�ؿ����ظ���<amerceRecord>Ԫ�����������<itemBarcode><readerBarcode><id>�Ⱦ߱����㹻����Ϣ��
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 10:09:36 GMT</operTime> ����ʱ��
  
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
</root>

<root>
  <operation>amerce</operation> 
  <action>undo</action> 
  <readerBarcode>...</readerBarcode> ����֤�����
  <!-- <idList>...<idList> ID�б����ż�� �ѷ�ֹ -->
  <amerceItems>
	<amerceItem id="..." newPrice="..."/>
	...
  </amerceItems>
  <amerceRecord recPath='...'><root><itemBarcode>0000001</itemBarcode><readerBarcode>R0000002</readerBarcode><state>amerced</state><id>632958375041543888-1</id><over>31day</over><borrowDate>Sat, 07 Oct 2006 09:04:28 GMT</borrowDate><borrowPeriod>30day</borrowPeriod><returnDate>Thu, 07 Dec 2006 09:04:27 GMT</returnDate><returnOperator>test</returnOperator></root></amerceRecord> Undo��ȥ���ķ�����¼
  <operator>test</operator> 
  <operTime>Fri, 08 Dec 2006 10:12:20 GMT</operTime> 
  
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼

</root>

<root>
  <operation>amerce</operation> 
  <action>modifyprice</action> 
  <readerBarcode>...</readerBarcode> ����֤�����
  <amerceItems>
	<amerceItem id="..." newPrice="..." newComment="..."/> newComment������׷�ӻ��滻ԭ����ע�����ݡ�������׷�ӻ��Ǹ��ǣ�ȡ���ڵ�һ���ַ��Ƿ�Ϊ'>'����'<'��ǰ��Ϊ׷��(��ʱ��һ���ַ�������������)�������һ���ַ�����������֮һ����Ĭ��Ϊ׷��
	...
  </amerceItems>
  <!-- modifyprice����ʱ������<amerceRecord>Ԫ�� -->
  <operator>test</operator> 
  <operTime>Fri, 08 Dec 2006 10:12:20 GMT</operTime> 
  
  <oldReaderRecord recPath='...'>...</oldReaderRecord>	����ǰ�ɵĶ��߼�¼��<oldReaderRecord>Ԫ����modifyprice����ʱ���е�Ԫ��
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼

</root>

2007/12/18
<root>
  <operation>amerce</operation> ��������
  <action>expire</action> ��ͣ������
  <readerBarcode>...</readerBarcode> ����֤�����
  <expiredOverdues> �Ѿ����ڵ�����<overdue>Ԫ��
	<overdue ... />
	...
  </expiredOverdues>
  <operator>test</operator> ������ ���Ϊ#readersMonitor����ʾΪ��̨�߳�
  <operTime>Fri, 08 Dec 2006 10:09:36 GMT</operTime> ����ʱ��
  
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
</root>
         * 
2008/6/20
<root>
  <operation>amerce</operation> 
  <action>modifycomment</action> 
  <readerBarcode>...</readerBarcode> ����֤�����
  <amerceItems>
	<amerceItem id="..." newComment="..."/> newComment������׷�ӻ��滻ԭ����ע�����ݡ�������׷�ӻ��Ǹ��ǣ�ȡ���ڵ�һ���ַ��Ƿ�Ϊ'>'����'<'��ǰ��Ϊ׷��(��ʱ��һ���ַ�������������)�������һ���ַ�����������֮һ����Ĭ��Ϊ׷��
	...
  </amerceItems>
  <!-- modifycomment����ʱ������<amerceRecord>Ԫ�� -->
  <operator>test</operator> 
  <operTime>Fri, 08 Dec 2006 10:12:20 GMT</operTime> 
  
  <oldReaderRecord recPath='...'>...</oldReaderRecord>	����ǰ�ɵĶ��߼�¼��<oldReaderRecord>Ԫ����modifycomment����ʱ���е�Ԫ��
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
</root>

         * * 
         * */
        public int RecoverAmerce(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            out string strError)
        {
            strError = "";

            // ��ʱ��Robust����Logic����
            if (level == RecoverLevel.Robust)
                level = RecoverLevel.Logic;


            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            DO_SNAPSHOT:

            string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                "action");

            // ���ջָ�
            if (level == RecoverLevel.Snapshot)
            {

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                if (strAction == "amerce")
                {
                    XmlNodeList nodes = domLog.DocumentElement.SelectNodes("amerceRecord");

                    int nErrorCount = 0;
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];
                        string strRecord = node.InnerText;
                        string strRecPath = DomUtil.GetAttr(node, "recPath");


                        // дΥԼ���¼
                        string strError0 = "";
                        lRet = channel.DoSaveTextRes(strRecPath,
            strRecord,
            false,
            "content,ignorechecktimestamp",
            timestamp,
            out output_timestamp,
            out strOutputPath,
            out strError0);
                        if (lRet == -1)
                        {
                            // ����ѭ��
                            if (strError != "")
                                strError += "\r\n";
                            strError += "д��ΥԼ���¼ '" + strRecPath + "' ʱ��������: " + strError0;
                            nErrorCount++;
                        }
                    }

                    if (nErrorCount > 0)
                        return -1;
                }
                else if (strAction == "undo")
                {
                    XmlNodeList nodes = domLog.DocumentElement.SelectNodes("amerceRecord");

                    int nErrorCount = 0;
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];
                        string strRecPath = DomUtil.GetAttr(node, "recPath");

                        int nRedoCount = 0;
                        string strError0 = "";
                    REDO:
                        // ɾ��ΥԼ���¼
                        lRet = channel.DoDeleteRes(strRecPath,
                            timestamp,
                            out output_timestamp,
                            out strError0);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                continue;   // ��¼�����Ͳ�����
                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                if (nRedoCount < 10)
                                {
                                    timestamp = output_timestamp;
                                    nRedoCount++;
                                    goto REDO;
                                }
                            }

                            // ����ѭ��
                            if (strError != "")
                                strError += "\r\n";
                            strError += "ɾ��ΥԼ���¼ '" + strRecPath + "' ʱ��������: " + strError0;
                            nErrorCount++;
                        }
                    } // end of for

                    if (nErrorCount > 0)
                        return -1;
                }
                else if (strAction == "modifyprice")
                {
                    // ����ʲô��������ֻ�Ⱥ����ÿ��յĶ��߼�¼���ָ�
                }
                else if (strAction == "expire")
                {
                    // ����ʲô��������ֻ�Ⱥ����ÿ��յĶ��߼�¼���ָ�

                }
                else if (strAction == "modifycomment")
                {
                    // ����ʲô��������ֻ�Ⱥ����ÿ��յĶ��߼�¼���ָ�
                }
                else if (strAction == "appendcomment")
                {
                    // ����ʲô��������ֻ�Ⱥ����ÿ��յĶ��߼�¼���ָ�
                }
                else
                {
                    strError = "δ֪��<action>����: " + strAction;
                    return -1;
                }

                {
                    XmlNode node = null;
                    // д����߼�¼
                    string strReaderRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "readerRecord",
                        out node);
                    string strReaderRecPath = DomUtil.GetAttr(node, "recPath");

                    // д���߼�¼
                    lRet = channel.DoSaveTextRes(strReaderRecPath,
        strReaderRecord,
        false,
        "content,ignorechecktimestamp",
        timestamp,
        out output_timestamp,
        out strOutputPath,
        out strError);
                    if (lRet == -1)
                    {
                        strError = "д����߼�¼ '" + strReaderRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }
                }

                return 0;
            }

            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {
                string strReaderBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerBarcode");
                if (String.IsNullOrEmpty(strReaderBarcode) == true)
                {
                    strError = "��־��¼��ȱ��<readerBarcode>Ԫ��";
                    return -1;
                }
                string strLibraryCode = DomUtil.GetElementText(domLog.DocumentElement,
                    "libraryCode");

                string strOperator = DomUtil.GetElementText(domLog.DocumentElement,
                    "operator");
                string strOperTime = DomUtil.GetElementText(domLog.DocumentElement,
                    "operTime");

                /*
                string strAmerceItemIdList = DomUtil.GetElementText(domLog.DocumentElement,
                    "idList");
                if (String.IsNullOrEmpty(strAmerceItemIdList) == true)
                {
                    strError = "��־��¼��ȱ��<idList>Ԫ��";
                    return -1;
                }
                 * */

                AmerceItem[] amerce_items = ReadAmerceItemList(domLog);


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
                    strError = "����֤����� '" + strReaderBarcode + "' ������";
                    goto ERROR1;
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

                byte[] output_timestamp = null;
                string strOutputPath = "";

                if (strAction == "amerce")
                {
                    List<string> NotFoundIds = null;
                    List<string> Ids = null;
                    List<string> AmerceRecordXmls = null;
                    // ��ΥԼ���ڶ��߼�¼��ȥ����ѡ��<overdue>Ԫ�أ����ҹ���һ���¼�¼׼������ΥԼ���
                    // return:
                    //      -1  error
                    //      0   ����domû�б仯
                    //      1   ����dom�����˱仯
                    nRet = DoAmerceReaderXml(
                        strLibraryCode,
                        ref readerdom,
                        amerce_items,
                        strOperator,
                        strOperTime,
                        out AmerceRecordXmls,
                        out NotFoundIds,
                        out Ids,
                        out strError);
                    if (nRet == -1)
                    {
                        // �ڴ�����Ϣ��������ÿ��id��Ӧ��amerce record
                        if (NotFoundIds != null && NotFoundIds.Count > 0)
                        {
                            strError += "������֤�����Ϊ " + strReaderBarcode + "����־��¼����ص�AmerceRecord���£�\r\n" + GetAmerceRecordStringByID(domLog, NotFoundIds);
                        }

                        goto ERROR1;
                    }

                    // ����о��������԰�AmerceRecordXmls����־��¼�е�<amerceRecord>������к˶�


                    // д��ΥԼ���¼
                    XmlNodeList nodes = domLog.DocumentElement.SelectNodes("amerceRecord");

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];
                        string strRecord = node.InnerText;
                        string strRecPath = DomUtil.GetAttr(node, "recPath");


                        // дΥԼ���¼
                        lRet = channel.DoSaveTextRes(strRecPath,
                            strRecord,
                            false,
                            "content,ignorechecktimestamp",
                            null,
                            out output_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "д��ΥԼ���¼ '" + strRecPath + "' ʱ��������: " + strError;
                            goto ERROR1;
                        }
                    }
                }

                if (strAction == "undo")
                {
                    XmlNodeList nodes = domLog.DocumentElement.SelectNodes("amerceRecord");

                    // �����������Ƿ���overduesԪ��
                    XmlNode root = readerdom.DocumentElement.SelectSingleNode("overdues");
                    if (root == null)
                    {
                        root = readerdom.CreateElement("overdues");
                        readerdom.DocumentElement.AppendChild(root);
                    }

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];
                        string strRecord = node.InnerText;
                        string strRecPath = DomUtil.GetAttr(node, "recPath");


                        // ����о��������԰�ΥԼ���¼�е�id����־��¼<amerceItems>�е�id�Աȼ��

                        // ΥԼ����Ϣ�ӻض��߼�¼
                        string strTempReaderBarcode = "";
                        string strOverdueString = "";

                        // ��ΥԼ���¼��ʽת��Ϊ���߼�¼�е�<overdue>Ԫ�ظ�ʽ
                        nRet = ConvertAmerceRecordToOverdueString(strRecord,
                            out strTempReaderBarcode,
                            out strOverdueString,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        if (strTempReaderBarcode != strReaderBarcode)
                        {
                            strError = "<amerceRecord>�еĶ���֤����ź���־��¼�е�<readerBarcode>����֤����Ų�һ��";
                            goto ERROR1;
                        }

                        XmlDocumentFragment fragment = readerdom.CreateDocumentFragment();
                        fragment.InnerXml = strOverdueString;

                        // 2008/11/13 changed
                        XmlNode node_added = root.AppendChild(fragment);
                        Debug.Assert(node_added != null, "");
                        string strReason = DomUtil.GetAttr(node_added, "reason");
                        if (strReason == "Ѻ��")
                        {
                            string strPrice = DomUtil.GetAttr(node_added, "price");

                            if (String.IsNullOrEmpty(strPrice) == false)
                            {
                                // ��Ҫ��<foregift>Ԫ���м�ȥ����۸�
                                string strContent = DomUtil.GetElementText(readerdom.DocumentElement,
                                    "foregift");


                                string strNegativePrice = "";
                                // ������"-123.4+10.55-20.3"�ļ۸��ַ�����ת������
                                // parameters:
                                //      bSum    �Ƿ�Ҫ˳�����? true��ʾҪ����
                                nRet = PriceUtil.NegativePrices(strPrice,
                                    false,
                                    out strNegativePrice,
                                    out strError);
                                if (nRet == -1)
                                {
                                    strError = "��ת�۸��ַ��� '" + strPrice + "ʱ��������: " + strError;
                                    goto ERROR1;
                                }

                                strContent = PriceUtil.JoinPriceString(strContent, strNegativePrice);

                                DomUtil.SetElementText(readerdom.DocumentElement,
                                    "foregift",
                                    strContent);
                                // bReaderDomChanged = true;
                            }
                        }


                        // ɾ��ΥԼ���¼
                        int nRedoCount = 0;
                        byte[] timestamp = null;
                    REDO:
                        // ɾ��ΥԼ���¼
                        lRet = channel.DoDeleteRes(strRecPath,
                            timestamp,
                            out output_timestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                continue;   // ��¼�����Ͳ�����
                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                if (nRedoCount < 10)
                                {
                                    timestamp = output_timestamp;
                                    nRedoCount++;
                                    goto REDO;
                                }
                            }

                            // �Ƿ���Ҫ����ѭ����
                            strError = "ɾ��ΥԼ���¼ '" + strRecPath + "' ʱ��������: " + strError;
                            goto ERROR1;
                        }
                    }

                }

                if (strAction == "modifyprice")
                {
                    nRet = ModifyPrice(ref readerdom,
                        amerce_items,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ModifyPrice()ʱ��������: " + strError;
                        goto ERROR1;
                    }
                }

                // 2008/6/20
                if (strAction == "modifycomment")
                {
                    nRet = ModifyComment(
                        ref readerdom,
                        amerce_items,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ModifyComment()ʱ��������: " + strError;
                        goto ERROR1;
                    }
                }

                if (strAction == "expire")
                {
                    // Ѱ��<expiredOverdues/overdue>Ԫ��
                    XmlNodeList nodes = domLog.DocumentElement.SelectNodes("//expiredOverdues/overdue");
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];
                        string strID = DomUtil.GetAttr(node, "id");

                        if (String.IsNullOrEmpty(strID) == true)
                            continue;

                        // �Ӷ��߼�¼��ȥ�����id��<overdue>Ԫ��
                        XmlNode nodeOverdue = readerdom.DocumentElement.SelectSingleNode("overdues/overdue[@id='"+strID+"']");
                        if (nodeOverdue != null)
                        {
                            if (nodeOverdue.ParentNode != null)
                                nodeOverdue.ParentNode.RemoveChild(nodeOverdue);
                        }
                    }
                }

                // д�ض��߼�¼
                strReaderXml = readerdom.OuterXml;

                lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                    strReaderXml,
                    false,
                    "content,ignorechecktimestamp",
                    reader_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
 
            }

            return 0;

        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        // ��ú�ָ��id��ص�AmerceRecord
        static string GetAmerceRecordStringByID(XmlDocument domLog,
            List<string> NotFoundIds)
        {
            string strResult = "";

            List<string> records = new List<string>();
            List<string> ids = new List<string>();
            XmlNodeList nodes = domLog.DocumentElement.SelectNodes("amerceRecord");
            for (int i = 0; i < nodes.Count; i++)
            {
                string strRecord = nodes[i].InnerText;
                if (String.IsNullOrEmpty(strRecord) == true)
                    continue;

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strRecord);
                }
                catch (Exception ex)
                {
                    strResult += "XML�ַ���װ��DOMʱ��������: " + ex.Message + "\r\n";
                    continue;
                }

                records.Add(strRecord);

                string strID = DomUtil.GetElementText(dom.DocumentElement, "id");

                ids.Add(strID);
            }

            for (int i = 0; i < NotFoundIds.Count; i++)
            {
                string strID = NotFoundIds[i];
                int index = ids.IndexOf(strID);
                if (index == -1)
                {
                    strResult += "id [" + strID + "] ����־��¼��û���ҵ���Ӧ��<amerceRecord>Ԫ��\r\n";
                    continue;
                }

                strResult += "id: " +strID + " -- " + records[index] + "\r\n";
            }

            return strResult;
        }

        // ��ø�����¼
        static int GetAttachmentRecord(
            Stream attachment,
            int nAttachmentIndex,
            out byte[] baRecord,
            out string strError)
        {
            baRecord = null;
            strError = "";

            if (attachment == null)
            {
                strError = "attachmentΪ��";
                return -1;
            }

            if (nAttachmentIndex < 0)
            {
                strError = "nAttachmentIndex����ֵ����>=0";
                return -1;
            }

            attachment.Seek(0, SeekOrigin.Begin);

            long lLength = 0;

            // �ҵ���¼��ͷ
            for (int i=0; i<=nAttachmentIndex; i++)
            {
                byte[] length = new byte[8];
                int nRet = attachment.Read(length, 0, 8);
                if (nRet != 8)
                {
                    strError = "������ʽ����1";
                    return -1;
                }
                lLength = BitConverter.ToInt64(length, 0);


                if (attachment.Length - attachment.Position < lLength)
                {
                    strError = "������ʽ����2";
                    return -1;
                }

                if (i == nAttachmentIndex)
                    break;

                attachment.Seek(lLength, SeekOrigin.Current);
            }

            if (lLength >= 1000 * 1024)
            {
                strError = "������¼����̫�󣬳���1000*1024���޷�����";
                return -1;
            }

            // �����¼����
            baRecord = new byte[(int)lLength];
            attachment.Read(baRecord, 0, (int)lLength);

            return 0;
        }


        /*
<root>
  <operation>devolveReaderInfo</operation> 
  <sourceReaderBarcode>...</sourceReaderBarcode> Դ����֤�����
  <targetReaderBarcode>...</targetReaderBarcode> Ŀ�����֤�����
  <borrows>...</borrows> �ƶ���ȥ��<borrows>���ݣ��¼�Ϊ<borrow>Ԫ��
  <overdues>...</overdues> �ƶ���ȥ��<overdue>���ݣ��¼�Ϊ<overdue>Ԫ��
  <sourceReaderRecord recPath='...'>...</sourceReaderRecord>	����Դ���߼�¼
  <targetReaderRecord recPath='...'>...</targetReaderRecord>	����Ŀ����߼�¼
  <changedEntityRecord recPath='...' attahchmentIndex='.'>...</changedEntityRecord> ��ǣ�����ķ������޸ĵ�ʵ���¼����Ԫ�ص��ı����Ǽ�¼�壬��ע��Ϊ��͸�����ַ�����HtmlEncoding��ļ�¼�ַ��������������attachmentIndex���ԣ������ʵ���¼���ڴ�Ԫ���ı��У�������־��¼�ĸ�����
  <operator>test</operator> 
  <operTime>Fri, 08 Dec 2006 10:12:20 GMT</operTime> 
</root>
         * * */
        public int RecoverDevolveReaderInfo(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            Stream attachmentLog,
            out string strError)
        {
            strError = "";

            // ��ʱ��Robust����Logic����
            if (level == RecoverLevel.Robust)
                level = RecoverLevel.Logic;

            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

        DO_SNAPSHOT:

            // ���ջָ�
            if (level == RecoverLevel.Snapshot)
            {
                /*
                // �۲��Ƿ���<warning>Ԫ��
                XmlNode nodeWarning = domLog.SelectSingleNode("warning");
                if (nodeWarning != null)
                {
                    // ���<warningԪ�ش��ڣ�����ֻ�ܲ����߼��ָ�>
                    strError = nodeWarning.InnerText;
                    return -1;
                }
                */

                // ��Դ���߼�¼
                XmlNode node = null;
                string strSourceReaderXml = DomUtil.GetElementText(
                    domLog.DocumentElement,
                    "sourceReaderRecord",
                    out node);
                if (node == null)
                {
                    strError = "��־��¼��ȱ<sourceReaderRecord>Ԫ��";
                    return -1;
                }
                string strSourceReaderRecPath = DomUtil.GetAttr(node, "recPath");

                byte[] timestamp = null;
                string strOutputPath = "";
                byte[] output_timestamp = null;

                // дԴ���߼�¼
                lRet = channel.DoSaveTextRes(strSourceReaderRecPath,
    strSourceReaderXml,
    false,
    "content,ignorechecktimestamp",
    timestamp,
    out output_timestamp,
    out strOutputPath,
    out strError);
                if (lRet == -1)
                {
                    strError = "д��Դ���߼�¼ '" + strSourceReaderRecPath + "' ʱ��������: " + strError;
                    return -1;
                }


                // ��Ŀ����߼�¼
                node = null;
                string strTargetReaderXml = DomUtil.GetElementText(
                    domLog.DocumentElement,
                    "targetReaderRecord",
                    out node);
                if (node == null)
                {
                    strError = "��־��¼��ȱ<targetReaderRecord>Ԫ��";
                    return -1;
                }
                string strTargetReaderRecPath = DomUtil.GetAttr(node, "recPath");


                // дĿ����߼�¼
                lRet = channel.DoSaveTextRes(strTargetReaderRecPath,
                    strTargetReaderXml,
                    false,
                    "content,ignorechecktimestamp",
                    timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    strError = "д��Դ���߼�¼ '" + strSourceReaderRecPath + "' ʱ��������: " + strError;
                    return -1;
                }

                // ѭ����д����ص�����ʵ���¼
                XmlNodeList nodeEntities = domLog.DocumentElement.SelectNodes("changedEntityRecord");
                for (int i = 0; i < nodeEntities.Count; i++)
                {
                    XmlNode nodeEntity = nodeEntities[i];

                    string strItemRecPath = DomUtil.GetAttr(nodeEntity,
                        "recPath");
                    string strAttachmentIndex = DomUtil.GetAttr(nodeEntity,
                        "attachmentIndex");

                    string strItemXml = "";

                    if (String.IsNullOrEmpty(strAttachmentIndex) == true)
                    {
                        strItemXml = nodeEntity.InnerText;
                        if (String.IsNullOrEmpty(strItemXml) == true)
                        {
                            strError = "<changedEntityRecord>Ԫ��ȱ���ı����ݡ�";
                            return -1;
                        }
                    }
                    else
                    {
                        // ʵ���¼�ڸ�����
                        int nAttachmentIndex = 0;
                        try
                        {
                            nAttachmentIndex = Convert.ToInt32(strAttachmentIndex);
                        }
                        catch
                        {
                            strError = "<changedEntityRecord>Ԫ�ص�attachmentIndex����ֵ'"+strAttachmentIndex+"'��ʽ����ȷ��Ӧ��Ϊ>=0�Ĵ�����";
                            return -1;
                        }

                        byte[] baItem = null;
                        nRet = GetAttachmentRecord(
                            attachmentLog,
                            nAttachmentIndex,
                            out baItem,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "���indexΪ "+nAttachmentIndex.ToString()+" ����־������¼ʱ����" + strError;
                            return -1;
                        }
                        strItemXml = Encoding.UTF8.GetString(baItem);
                    }


                    /*
                    byte[] timestamp = null;
                    byte[] output_timestamp = null;
                    string strOutputPath = "";
                     * */

                    // д���¼
                    lRet = channel.DoSaveTextRes(strItemRecPath,
                        strItemXml,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "д����¼ '" + strItemRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }
                }

                return 0;
            }

            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {
                string strOperTimeString = DomUtil.GetElementText(domLog.DocumentElement,
                    "operTime");

                string strSourceReaderBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "sourceReaderBarcode");
                if (String.IsNullOrEmpty(strSourceReaderBarcode) == true)
                {
                    strError = "<sourceReaderBarcode>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

                string strTargetReaderBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "targetReaderBarcode");
                if (String.IsNullOrEmpty(strTargetReaderBarcode) == true)
                {
                    strError = "<targetReaderBarcode>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

                // ����Դ���߼�¼
                string strSourceReaderXml = "";
                string strSourceOutputReaderRecPath = "";
                byte[] source_reader_timestamp = null;

                nRet = this.GetReaderRecXml(
                    Channels,
                    strSourceReaderBarcode,
                    out strSourceReaderXml,
                    out strSourceOutputReaderRecPath,
                    out source_reader_timestamp,
                    out strError);
                if (nRet == 0)
                {
                    strError = "Դ����֤����� '" + strSourceReaderBarcode + "' ������";
                    goto ERROR1;
                }
                if (nRet == -1)
                {
                    strError = "����֤�����Ϊ '" + strSourceReaderBarcode + "' ��Դ���߼�¼ʱ��������: " + strError;
                    goto ERROR1;
                }

                XmlDocument source_readerdom = null;
                nRet = LibraryApplication.LoadToDom(strSourceReaderXml,
                    out source_readerdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ��Դ���߼�¼����XML DOMʱ��������: " + strError;
                    goto ERROR1;
                }

                //
                // ����Ŀ����߼�¼
                string strTargetReaderXml = "";
                string strTargetOutputReaderRecPath = "";
                byte[] target_reader_timestamp = null;

                nRet = this.GetReaderRecXml(
                    Channels,
                    strTargetReaderBarcode,
                    out strTargetReaderXml,
                    out strTargetOutputReaderRecPath,
                    out target_reader_timestamp,
                    out strError);
                if (nRet == 0)
                {
                    strError = "Ŀ�����֤����� '" + strTargetReaderBarcode + "' ������";
                    goto ERROR1;
                }
                if (nRet == -1)
                {
                    strError = "����֤�����Ϊ '" + strTargetReaderBarcode + "' ��Ŀ����߼�¼ʱ��������: " + strError;
                    goto ERROR1;
                }

                XmlDocument target_readerdom = null;
                nRet = LibraryApplication.LoadToDom(strTargetReaderXml,
                    out target_readerdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ��Ŀ����߼�¼����XML DOMʱ��������: " + strError;
                    goto ERROR1;
                }

                Stream tempstream = null;

                // �ƶ���Ϣ
                XmlDocument domTemp = null;
                // �ƶ�������Ϣ -- <borrows>Ԫ������
                // return:
                //      -1  error
                //      0   not found brrowinfo
                //      1   found and moved
                nRet = DevolveBorrowInfo(
                    Channels,
                    strSourceReaderBarcode,
                    strTargetReaderBarcode,
                    strOperTimeString,
                    ref source_readerdom,
                    ref target_readerdom,
                    ref domTemp,
                    "",
                    out tempstream,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // �ƶ�����ΥԼ����Ϣ -- <overdues>Ԫ������
                // return:
                //      -1  error
                //      0   not found overdueinfo
                //      1   found and moved
                nRet = DevolveOverdueInfo(
                    strSourceReaderBarcode,
                    strTargetReaderBarcode,
                    strOperTimeString,
                    ref source_readerdom,
                    ref target_readerdom,
                    ref domTemp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;



                // д�ض��߼�¼
                byte[] output_timestamp = null;
                string strOutputPath = "";


                // д��Դ���߼�¼
                lRet = channel.DoSaveTextRes(strSourceOutputReaderRecPath,
                    source_readerdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    source_reader_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                // д��Ŀ����߼�¼
                lRet = channel.DoSaveTextRes(strTargetOutputReaderRecPath,
                    target_readerdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    source_reader_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }


            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }


        // SetBiblioInfo() API ��CopyBiblioInfo() API �Ļָ�����
        // �����ڣ�ʹ��return -1;����goto ERROR1; Ҫ����������ʱ���Ƿ��м�ֵ����̽��SnapShot���ԡ�����ǣ����ú��ߡ�
        /*
<root>
  <operation>setBiblioInfo</operation> 
  <action>...</action> ���嶯�� �� new/change/delete/onlydeletebiblio/onlydeletesubrecord �� onlycopybiblio/onlymovebiblio/copy/move
  <record recPath='����ͼ��/3'>...</record> ��¼�� ����Ϊnew/change/ *move* / *copy* ʱ���д�Ԫ��(��deleteʱû�д�Ԫ��)
  <oldRecord recPath='����ͼ��/3'>...</oldRecord> �����ǡ�ɾ�������ƶ��ļ�¼ ����Ϊchange/ *delete* / *move* / *copy* ʱ�߱���Ԫ��
  <deletedEntityRecords> ��ɾ����ʵ���¼(����)��ֻ�е�<action>Ϊdeleteʱ�������Ԫ�ء�
	  <record recPath='����ͼ��ʵ��/100'>...</record> ���Ԫ�ؿ����ظ���ע��Ԫ�����ı�����ĿǰΪ�ա�
	  ...
  </deletedEntityRecords>
  <copyEntityRecords> �����Ƶ�ʵ���¼(����)��ֻ�е�<action>Ϊ*copy*ʱ�������Ԫ�ء�
	  <record recPath='����ͼ��ʵ��/100' targetRecPath='����ͼ��ʵ��/110'>...</record> ���Ԫ�ؿ����ظ���ע��Ԫ�����ı�����ĿǰΪ�ա�recPath����ΪԴ��¼·����targetRecPathΪĿ���¼·��
	  ...
  </copyEntityRecords>
  <moveEntityRecords> ���ƶ���ʵ���¼(����)��ֻ�е�<action>Ϊ*move*ʱ�������Ԫ�ء�
	  <record recPath='����ͼ��ʵ��/100' targetRecPath='����ͼ��ʵ��/110'>...</record> ���Ԫ�ؿ����ظ���ע��Ԫ�����ı�����ĿǰΪ�ա�recPath����ΪԴ��¼·����targetRecPathΪĿ���¼·��
	  ...
  </moveEntityRecords>
  <copyOrderRecords /> <moveOrderRecords />
  <copyIssueRecords /> <moveIssueRecords />
  <copyCommentRecords /> <moveCommentRecords />
  <operator>test</operator> 
  <operTime>Fri, 08 Dec 2006 10:12:20 GMT</operTime> 
</root>

�߼��ָ�delete������ʱ�򣬼�����ȫ��������ʵ���¼ɾ����
���ջָ���ʱ�򣬿��Ը���operlogdomֱ��ɾ����¼��path����Щʵ���¼
         * */
        public int RecoverSetBiblioInfo(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            out string strError)
        {
            strError = "";

            // ��ʱ��Robust����Logic����
            if (level == RecoverLevel.Robust)
                level = RecoverLevel.Logic;

            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            bool bReuse = false;    // �Ƿ��ܹ�����RecorverLevel״̬�����ò��ִ���

        DO_SNAPSHOT:

            string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                "action");

            // ���ջָ�
            if (level == RecoverLevel.Snapshot 
                || bReuse == true)
            {
                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                if (strAction == "new" || strAction == "change")
                {
                    XmlNode node = null;
                    string strRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        goto ERROR1;
                    }
                    string strRecPath = DomUtil.GetAttr(node, "recPath");

                    // д��Ŀ��¼
                    lRet = channel.DoSaveTextRes(strRecPath,
                        strRecord,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "д����Ŀ��¼ '" + strRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }
                }
                else if (strAction == "onlymovebiblio"
                    || strAction == "onlycopybiblio"
                    || strAction == "move"
                    || strAction == "copy")
                {
                    XmlNode node = null;
                    string strTargetRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "record",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<record>Ԫ��";
                        goto ERROR1;
                    }
                    string strTargetRecPath = DomUtil.GetAttr(node, "recPath");

                    string strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                        return -1;
                    }
                    string strOldRecPath = DomUtil.GetAttr(node, "recPath");

                    string strMergeStyle = DomUtil.GetElementText(domLog.DocumentElement,
                        "mergeStyle");

                    bool bSourceExist = true;
                    // �۲�Դ��¼�Ƿ����
                    {
                        string strMetaData = "";
                        string strXml = "";
                        byte[] temp_timestamp = null;

                        lRet = channel.GetRes(strOldRecPath,
                            out strXml,
                            out strMetaData,
                            out temp_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            {
                                bSourceExist = false;
                            }
                        }
                    }

                    if (bSourceExist == true)
                    {
                        string strIdChangeList = "";

                        // ������Ŀ��¼
                        lRet = channel.DoCopyRecord(strOldRecPath,
                            strTargetRecPath,
                            strAction == "onlymovebiblio" ? true : false,   // bDeleteSourceRecord
                            strMergeStyle,
                            out strIdChangeList,
                            out output_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "DoCopyRecord() error :" + strError;
                            goto ERROR1;
                        }
                    }

                    /*
                    // д��Ŀ��¼
                    lRet = channel.DoSaveTextRes(strRecPath,
                        strRecord,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "������Ŀ��¼ '" + strOldRecPath + "' �� '" + strTargetRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }                     * */


                    if (bSourceExist == false)
                    {
                        if (String.IsNullOrEmpty(strTargetRecord) == true)
                        {
                            if (String.IsNullOrEmpty(strOldRecord) == true)
                            {
                                strError = "Դ��¼ '" + strOldRecPath + "' �����ڣ�����<record>Ԫ�����ı����ݣ���ʱ<oldRecord>Ԫ��Ҳ���ı����ݣ��޷����Ҫд��ļ�¼����";
                                return -1;
                            }

                            strTargetRecord = strOldRecord;
                        }
                    }

                    // ����С��¼�¼������
                    if (String.IsNullOrEmpty(strTargetRecord) == false)
                    {

                        // д��Ŀ��¼
                        lRet = channel.DoSaveTextRes(strTargetRecPath,
                            strTargetRecord,
                            false,
                            "content,ignorechecktimestamp",
                            timestamp,
                            out output_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "д��Ŀ��¼ '" + strTargetRecPath + "' ʱ��������: " + strError;
                            return -1;
                        }
                    }

                    // ���ƻ����ƶ��¼��Ӽ�¼
                    if (strAction == "move"
                    || strAction == "copy")
                    {
                        string[] element_names = new string[] {
                            "copyEntityRecords",
                            "moveEntityRecords",  
                            "copyOrderRecords", 
                            "moveOrderRecords",
                            "copyIssueRecords", 
                            "moveIssueRecords",   
                            "copyCommentRecords", 
                            "moveCommentRecords"     
                        };

                        for (int i = 0; i < element_names.Length; i++)
                        {
                            XmlNode node_subrecords = domLog.DocumentElement.SelectSingleNode(
                                element_names[i]);
                            if (node_subrecords != null)
                            {
                                nRet = CopySubRecords(
                                    channel,
                                    node_subrecords,
                                    strTargetRecPath,
                                    out strError);
                                if (nRet == -1)
                                    return -1;
                            }
                        }

                    }

                    // 2011/12/12
                    if (bSourceExist == true
                        && (strAction == "move" || strAction == "onlymovebiblio")
                        )
                    {
                        int nRedoCount = 0;
                    REDO_DELETE:
                        // ɾ��Դ��Ŀ��¼
                        lRet = channel.DoDeleteRes(strOldRecPath,
                            timestamp,
                            out output_timestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            {
                                // ��¼�����Ͳ�����
                            }
                            else if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                if (nRedoCount < 10)
                                {
                                    timestamp = output_timestamp;
                                    nRedoCount++;
                                    goto REDO_DELETE;
                                }
                            }
                            else
                            {
                                strError = "ɾ����Ŀ��¼ '" + strOldRecPath + "' ʱ��������: " + strError;
                                return -1;
                            }
                        }
                    }
                }
                else if (strAction == "delete"
                    || strAction == "onlydeletebiblio"
                    || strAction == "onlydeletesubrecord")
                {
                    XmlNode node = null;
                    string strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                        return -1;
                    }
                    string strRecPath = DomUtil.GetAttr(node, "recPath");

                    if (strAction != "onlydeletesubrecord")
                    {
                        int nRedoCount = 0;
                    REDO:
                        // ɾ����Ŀ��¼
                        lRet = channel.DoDeleteRes(strRecPath,
                            timestamp,
                            out output_timestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                goto DO_DELETE_CHILD_ENTITYRECORDS;   // ��¼�����Ͳ�����
                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                if (nRedoCount < 10)
                                {
                                    timestamp = output_timestamp;
                                    nRedoCount++;
                                    goto REDO;
                                }
                            }
                            strError = "ɾ����Ŀ��¼ '" + strRecPath + "' ʱ��������: " + strError;
                            return -1;
                        }
                    }

                DO_DELETE_CHILD_ENTITYRECORDS:
                    if (strAction == "delete" || strAction == "onlydeletesubrecord")
                    {
                        XmlNodeList nodes = domLog.DocumentElement.SelectNodes("deletedEntityRecords/record");
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            string strEntityRecPath = DomUtil.GetAttr(nodes[i], "recPath");

                            /*
                            if (String.IsNullOrEmpty(strEntityRecPath) == true)
                                continue;
                             * */
                            int nRedoDeleteCount = 0;
                        REDO_DELETE_ENTITY:
                            // ɾ��ʵ���¼
                            lRet = channel.DoDeleteRes(strEntityRecPath,
                                timestamp,
                                out output_timestamp,
                                out strError);
                            if (lRet == -1)
                            {
                                if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                    continue;   // ��¼�����Ͳ�����
                                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                                {
                                    if (nRedoDeleteCount < 10)
                                    {
                                        timestamp = output_timestamp;
                                        nRedoDeleteCount++;
                                        goto REDO_DELETE_ENTITY;
                                    }
                                }
                                strError = "ɾ��ʵ���¼ '" + strEntityRecPath + "' ʱ��������: " + strError;
                                return -1;
                            }
                        }

                        nodes = domLog.DocumentElement.SelectNodes("deletedOrderRecords/record");
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            string strOrderRecPath = DomUtil.GetAttr(nodes[i], "recPath");

                            if (String.IsNullOrEmpty(strOrderRecPath) == true)
                                continue;
                            int nRedoDeleteCount = 0;
                        REDO_DELETE_ORDER:
                            // ɾ��������¼
                            lRet = channel.DoDeleteRes(strOrderRecPath,
                                timestamp,
                                out output_timestamp,
                                out strError);
                            if (lRet == -1)
                            {
                                if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                    continue;   // ��¼�����Ͳ�����
                                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                                {
                                    if (nRedoDeleteCount < 10)
                                    {
                                        timestamp = output_timestamp;
                                        nRedoDeleteCount++;
                                        goto REDO_DELETE_ORDER;
                                    }
                                }
                                strError = "ɾ��������¼ '" + strOrderRecPath + "' ʱ��������: " + strError;
                                return -1;
                            }
                        }

                        nodes = domLog.DocumentElement.SelectNodes("deletedIssueRecords/record");
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            string strIssueRecPath = DomUtil.GetAttr(nodes[i], "recPath");

                            if (String.IsNullOrEmpty(strIssueRecPath) == true)
                                continue;
                            int nRedoDeleteCount = 0;
                        REDO_DELETE_ISSUE:
                            // ɾ���ڼ�¼
                            lRet = channel.DoDeleteRes(strIssueRecPath,
                                timestamp,
                                out output_timestamp,
                                out strError);
                            if (lRet == -1)
                            {
                                if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                    continue;   // ��¼�����Ͳ�����
                                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                                {
                                    if (nRedoDeleteCount < 10)
                                    {
                                        timestamp = output_timestamp;
                                        nRedoDeleteCount++;
                                        goto REDO_DELETE_ISSUE;
                                    }
                                }
                                strError = "ɾ���ڼ�¼ '" + strIssueRecPath + "' ʱ��������: " + strError;
                                return -1;
                            }
                        }

                    } // end if
                }


                return 0;
            }

            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {
                // �����ݿ������м�¼�ϲ���Ȼ�󱣴�
                if (strAction == "new" || strAction == "change")
                {
                    // ��SnapShot��ʽ��ͬ
                    bReuse = true;
                    goto DO_SNAPSHOT;
                }
                else if (strAction == "onlymovebiblio"
                    || strAction == "onlycopybiblio"
                    || strAction == "move"
                    || strAction == "copy")
                {
                    // ��SnapShot��ʽ��ͬ
                    bReuse = true;
                    goto DO_SNAPSHOT;
                }
                else if (strAction == "delete"
                    || strAction == "onlydeletebiblio"
                    || strAction == "onlydeletesubrecord")
                {
                    XmlNode node = null;
                    string strOldRecord = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<oldRecord>Ԫ��";
                        return -1;
                    }
                    string strRecPath = DomUtil.GetAttr(node, "recPath");

                    if (strAction != "onlydeletesubrecord")
                    {
                        int nRedoCount = 0;
                        byte[] timestamp = null;
                        byte[] output_timestamp = null;
                    REDO:
                        // ɾ����Ŀ��¼
                        lRet = channel.DoDeleteRes(strRecPath,
                            timestamp,
                            out output_timestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                goto DO_DELETE_CHILD_ENTITYRECORDS;   // ��¼�����Ͳ�����
                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                if (nRedoCount < 10)
                                {
                                    timestamp = output_timestamp;
                                    nRedoCount++;
                                    goto REDO;
                                }
                            }
                            strError = "ɾ����Ŀ��¼ '" + strRecPath + "' ʱ��������: " + strError;
                            goto ERROR1;
                        }
                    }

                DO_DELETE_CHILD_ENTITYRECORDS:

                    if (strAction == "delete" || strAction == "onlydeletesubrecord")
                    {
                        // ɾ������ͬһ��Ŀ��¼��ȫ��ʵ���¼
                        // return:
                        //      -1  error
                        //      0   û���ҵ�������Ŀ��¼���κ�ʵ���¼�����Ҳ���޴�ɾ��
                        //      >0  ʵ��ɾ����ʵ���¼��
                        nRet = DeleteBiblioChildEntities(channel,
                            strRecPath,
                            null,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ɾ����Ŀ��¼ '" + strRecPath + "' ������ʵ���¼ʱ����: " + strError;
                            goto ERROR1;
                        }

                        // return:
                        //      -1  error
                        //      0   û���ҵ�������Ŀ��¼���κ�ʵ���¼�����Ҳ���޴�ɾ��
                        //      >0  ʵ��ɾ����ʵ���¼��
                        nRet = this.OrderItemDatabase.DeleteBiblioChildItems(Channels,
                            strRecPath,
                            null,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ɾ����Ŀ��¼ '" + strRecPath + "' �����Ķ�����¼ʱ����: " + strError;
                            goto ERROR1;
                        }

                        // return:
                        //      -1  error
                        //      0   û���ҵ�������Ŀ��¼���κ�ʵ���¼�����Ҳ���޴�ɾ��
                        //      >0  ʵ��ɾ����ʵ���¼��
                        nRet = this.IssueItemDatabase.DeleteBiblioChildItems(Channels,
                            strRecPath,
                            null,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ɾ����Ŀ��¼ '" + strRecPath + "' �������ڼ�¼ʱ����: " + strError;
                            goto ERROR1;
                        }
                    }
                }
                else
                {
                    strError = "�޷�ʶ���<action>���� '" + strAction + "'";
                    return -1;
                }
            }
            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        // Դ��¼�����ڣ�Ӧ�ú��ԣ�Ŀ��ⲻ���ڣ�ҲӦ�ú���
        /*
  <copyEntityRecords> �����Ƶ�ʵ���¼(����)��ֻ�е�<action>Ϊ*copy*ʱ�������Ԫ�ء�
	  <record recPath='����ͼ��ʵ��/100' targetRecPath='����ͼ��ʵ��/110'>...</record> ���Ԫ�ؿ����ظ���ע��Ԫ�����ı�����ĿǰΪ�ա�recPath����ΪԴ��¼·����targetRecPathΪĿ���¼·��
	  ...
  </copyEntityRecords>
  <moveEntityRecords> ���ƶ���ʵ���¼(����)��ֻ�е�<action>Ϊ*move*ʱ�������Ԫ�ء�
	  <record recPath='����ͼ��ʵ��/100' targetRecPath='����ͼ��ʵ��/110'>...</record> ���Ԫ�ؿ����ظ���ע��Ԫ�����ı�����ĿǰΪ�ա�recPath����ΪԴ��¼·����targetRecPathΪĿ���¼·��
	  ...
  </moveEntityRecords>
         * */
        public int CopySubRecords(
            RmsChannel channel,
            XmlNode node,
            string strTargetBiblioRecPath,
            out string strError)
        {
            strError = "";

            string strAction = "";
            if (StringUtil.HasHead(node.Name, "copy") == true)
                strAction = "copy";
            else if (StringUtil.HasHead(node.Name, "move") == true) // 2011/12/5 ԭ����BUG "copy"
                strAction = "move";
            else
            {
                strError = "����ʶ���Ԫ���� '" + node.Name + "'";
                return -1;
            }

            XmlNodeList nodes = node.SelectNodes("record");
            foreach (XmlNode record_node in nodes)
            {
                string strSourceRecPath = DomUtil.GetAttr(record_node, "recPath");
                string strTargetRecPath = DomUtil.GetAttr(record_node, "targetRecPath");

                string strNewBarcode = DomUtil.GetAttr(record_node, "newBarcode");

                string strMetaData = "";
                string strXml = "";
                byte[] timestamp = null;
                string strOutputPath = "";

                long lRet = channel.GetRes(strSourceRecPath,
                    out strXml,
                    out strMetaData,
                    out timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.NotFound)
                        continue;   // �Ƿ񱨴�?

                    strError = "��ȡ�¼���¼ '" + strSourceRecPath + "' ʱ��������: " + strError;
                    return -1;
                    // goto CONTINUE;
                }

                DeleteEntityInfo entityinfo = new DeleteEntityInfo();

                entityinfo.RecPath = strOutputPath;
                entityinfo.OldTimestamp = timestamp;
                entityinfo.OldRecord = strXml;

                List<DeleteEntityInfo> entityinfos = new List<DeleteEntityInfo>();
                entityinfos.Add(entityinfo);

                // TODO: ���Ŀ�����ݿ��Ѿ������ڣ�Ҫ����

                List<string> target_recpaths = new List<string>();
                target_recpaths.Add(strTargetRecPath);
                List<string> newbarcodes = new List<string>();
                newbarcodes.Add(strNewBarcode);

                // ��������ͬһ��Ŀ��¼��ȫ��ʵ���¼
                // parameters:
                //      strAction   copy / move
                // return:
                //      -1  error
                //      >=0  ʵ�ʸ��ƻ����ƶ���ʵ���¼��
                int nRet = CopyBiblioChildRecords(channel,
                    strAction,
                    entityinfos,
                    target_recpaths,
                    strTargetBiblioRecPath,
                    newbarcodes,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return 0;
        }

        /*
hire ��������¼

API: Hire()

<root>
  <operation>hire</operation> ��������
  <action>...</action> ���嶯�� ��hire hirelate����
  <readerBarcode>R0000002</readerBarcode> ����֤�����
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 04:17:45 GMT</operTime> ����ʱ��
  <overdues>...</overdues> �����Ϣ ͨ������Ϊһ���ַ�����Ϊһ������<overdue>Ԫ��XML�ı�Ƭ��
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
</root>
         * */
        public int RecoverHire(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            out string strError)
        {
            strError = "";

            // ��ʱ��Robust����Logic����
            if (level == RecoverLevel.Robust)
                level = RecoverLevel.Logic;


            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

        DO_SNAPSHOT:

            // ���ջָ�
            if (level == RecoverLevel.Snapshot)
            {
                XmlNode node = null;
                string strReaderXml = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerRecord",
                    out node);
                if (node == null)
                {
                    strError = "��־��¼��ȱ<readerRecord>Ԫ��";
                    return -1;
                }
                string strReaderRecPath = DomUtil.GetAttr(node, "recPath");

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                // д���߼�¼
                lRet = channel.DoSaveTextRes(strReaderRecPath,
                    strReaderXml,
                    false,
                    "content,ignorechecktimestamp",
                    timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    strError = "д����߼�¼ '" + strReaderRecPath + "' ʱ��������: " + strError;
                    return -1;
                }

                return 0;
            }


            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {
                // string strRecoverComment = "";

                string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                    "action");

                string strReaderBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerBarcode");
                ///
                if (String.IsNullOrEmpty(strReaderBarcode) == true)
                {
                    strError = "��־��¼��<readerBarcode>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

                string strOperator = DomUtil.GetElementText(domLog.DocumentElement,
                    "operator");

                string strOperTime = DomUtil.GetElementText(domLog.DocumentElement,
                    "operTime");

                string strOverdues = DomUtil.GetElementText(domLog.DocumentElement,
                    "overdues");
                if (String.IsNullOrEmpty(strOverdues) == true)
                {
                    strError = "��־��¼��<overdues>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

                // ��overdues�ַ����з�����id
                XmlDocument tempdom = new XmlDocument();
                tempdom.LoadXml("<root />");
                XmlDocumentFragment fragment = tempdom.CreateDocumentFragment();
                fragment.InnerXml = strOverdues;
                tempdom.DocumentElement.AppendChild(fragment);

                XmlNode tempnode = tempdom.DocumentElement.SelectSingleNode("overdue");
                if (tempnode == null)
                {
                    strError = "<overdues>Ԫ����������ȱ��<overdue>Ԫ��";
                    goto ERROR1;
                }

                string strID = DomUtil.GetAttr(tempnode, "id");
                if (String.IsNullOrEmpty(strID) == true)
                {
                    strError = "��־��¼��<overdues>������<overdue>Ԫ����id����ֵΪ��";
                    goto ERROR1;
                }


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
                    strError = "����֤����� '" + strReaderBarcode + "' ������";
                    goto ERROR1;
                }
                if (nRet == -1)
                {
                    strError = "����֤�����Ϊ '" + strReaderBarcode + "' �Ķ��߼�¼ʱ��������: " + strError;
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


                // 
                string strOverdueString = "";
                // ����Hire() APIҪ���޸�readerdom
                nRet = DoHire(strAction,
                    readerdom,
                    ref strID,
                    strOperator,
                    strOperTime,
                    out strOverdueString,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // д�ض��ߡ����¼
                byte[] output_timestamp = null;
                string strOutputPath = "";


                // д�ض��߼�¼
                lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                    readerdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    reader_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;


            }


            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }


        /*
foregift ����Ѻ���¼

API: Foregift()

<root>
  <operation>foregift</operation> ��������
  <action>...</action> ���嶯�� Ŀǰ��foregift return (ע: return����ʱ��overdueԪ�������price���ԣ�����ʹ�ú� %return_foregift_price% ��ʾ��ǰʣ���Ѻ���)
  <readerBarcode>R0000002</readerBarcode> ����֤�����
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 04:17:45 GMT</operTime> ����ʱ��
  <overdues>...</overdues> Ѻ����Ϣ ͨ������Ϊһ���ַ�����Ϊһ������<overdue>Ԫ��XML�ı�Ƭ��
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
</root>
         * * */
        public int RecoverForegift(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            out string strError)
        {
            strError = "";

            // ��ʱ��Robust����Logic����
            if (level == RecoverLevel.Robust)
                level = RecoverLevel.Logic;


            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

        DO_SNAPSHOT:

            // ���ջָ�
            if (level == RecoverLevel.Snapshot)
            {
                XmlNode node = null;
                string strReaderXml = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerRecord",
                    out node);
                if (node == null)
                {
                    strError = "��־��¼��ȱ<readerRecord>Ԫ��";
                    return -1;
                }
                string strReaderRecPath = DomUtil.GetAttr(node, "recPath");

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                // д���߼�¼
                lRet = channel.DoSaveTextRes(strReaderRecPath,
                    strReaderXml,
                    false,
                    "content,ignorechecktimestamp",
                    timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    strError = "д����߼�¼ '" + strReaderRecPath + "' ʱ��������: " + strError;
                    return -1;
                }

                return 0;
            }


            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {
                // string strRecoverComment = "";

                string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                    "action");

                string strReaderBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerBarcode");
                ///
                if (String.IsNullOrEmpty(strReaderBarcode) == true)
                {
                    strError = "��־��¼��<readerBarcode>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

                string strOperator = DomUtil.GetElementText(domLog.DocumentElement,
                    "operator");

                string strOperTime = DomUtil.GetElementText(domLog.DocumentElement,
                    "operTime");

                string strOverdues = DomUtil.GetElementText(domLog.DocumentElement,
                    "overdues");
                if (String.IsNullOrEmpty(strOverdues) == true)
                {
                    strError = "��־��¼��<overdues>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

                // ��overdues�ַ����з�����id
                XmlDocument tempdom = new XmlDocument();
                tempdom.LoadXml("<root />");
                XmlDocumentFragment fragment = tempdom.CreateDocumentFragment();
                fragment.InnerXml = strOverdues;
                tempdom.DocumentElement.AppendChild(fragment);

                XmlNode tempnode = tempdom.DocumentElement.SelectSingleNode("overdue");
                if (tempnode == null)
                {
                    strError = "<overdues>Ԫ����������ȱ��<overdue>Ԫ��";
                    goto ERROR1;
                }

                string strID = DomUtil.GetAttr(tempnode, "id");
                if (String.IsNullOrEmpty(strID) == true)
                {
                    strError = "��־��¼��<overdues>������<overdue>Ԫ����id����ֵΪ��";
                    goto ERROR1;
                }

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
                    strError = "����֤����� '" + strReaderBarcode + "' ������";
                    goto ERROR1;
                }
                if (nRet == -1)
                {
                    strError = "����֤�����Ϊ '" + strReaderBarcode + "' �Ķ��߼�¼ʱ��������: " + strError;
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


                // 
                string strOverdueString = "";
                // ����Foregift() APIҪ���޸�readerdom
                nRet = DoForegift(strAction,
                    readerdom,
                    ref strID,
                    strOperator,
                    strOperTime,
                    out strOverdueString,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // д�ض��ߡ����¼
                byte[] output_timestamp = null;
                string strOutputPath = "";


                // д�ض��߼�¼
                lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                    readerdom.OuterXml,
                    false,
                    "content,ignorechecktimestamp",
                    reader_timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;


            }


            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        /*
settlement ����ΥԼ��

API: Settlement()

<root>
  <operation>settlement</operation> ��������
  <action>...</action> ���嶯�� ��settlement undosettlement delete 3��
  <id>1234567-1</id> ID
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 04:17:45 GMT</operTime> ����ʱ��
  
  <oldAmerceRecord recPath='...'>...</oldAmerceRecord>	��ΥԼ���¼
  <amerceRecord recPath='...'>...</amerceRecord>	��ΥԼ���¼ delete�����޴�Ԫ��
</root>
         * */
        public int RecoverSettlement(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            out string strError)
        {
            strError = "";

            // ��ʱ��Robust����Logic����
            if (level == RecoverLevel.Robust)
                level = RecoverLevel.Logic;

            long lRet = 0;
            int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

        DO_SNAPSHOT:

            // ���ջָ�
            if (level == RecoverLevel.Snapshot)
            {
                string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                    "action");

                if (strAction == "settlement"
                    || strAction == "undosettlement")
                {

                    XmlNode node = null;
                    string strAmerceXml = DomUtil.GetElementText(domLog.DocumentElement,
                        "amerceRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<amerceRecord>Ԫ��";
                        return -1;
                    }
                    string strAmerceRecPath = DomUtil.GetAttr(node, "recPath");

                    byte[] timestamp = null;
                    byte[] output_timestamp = null;
                    string strOutputPath = "";

                    // дΥԼ���¼
                    lRet = channel.DoSaveTextRes(strAmerceRecPath,
                        strAmerceXml,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "д��ΥԼ���¼ '" + strAmerceRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }

                }
                else if (strAction == "delete")
                {
                    XmlNode node = null;
                    string strOldAmerceXml = DomUtil.GetElementText(domLog.DocumentElement,
                        "oldAmerceRecord",
                        out node);
                    if (node == null)
                    {
                        strError = "��־��¼��ȱ<oldAmerceRecord>Ԫ��";
                        return -1;
                    }
                    string strOldAmerceRecPath = DomUtil.GetAttr(node, "recPath");

                    // ɾ��ΥԼ���¼
                    int nRedoCount = 0;
                    byte[] timestamp = null;
                    byte[] output_timestamp = null;

                REDO_DELETE:
                    lRet = channel.DoDeleteRes(strOldAmerceRecPath,
                        timestamp,
                        out output_timestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            return 0;   // ��¼�����Ͳ�����

                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            if (nRedoCount < 10)
                            {
                                timestamp = output_timestamp;
                                nRedoCount++;
                                goto REDO_DELETE;
                            }
                        }
                        strError = "ɾ��ΥԼ���¼ '" + strOldAmerceRecPath + "' ʱ��������: " + strError;
                        return -1;

                    }
                }
                else
                {
                    strError = "δ��ʶ���actionֵ '" + strAction + "'";
                }

                return 0;
            }

            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {
                string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                    "action");

                string strID = DomUtil.GetElementText(domLog.DocumentElement,
                    "id");

                ///
                if (String.IsNullOrEmpty(strID) == true)
                {
                    strError = "��־��¼��<id>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

                string strOperator = DomUtil.GetElementText(domLog.DocumentElement,
                    "operator");

                string strOperTime = DomUtil.GetElementText(domLog.DocumentElement,
                    "operTime");

                // ͨ��id���ΥԼ���¼��·��
                string strText = "";
                string strCount = "";

                strCount = "<maxCount>100</maxCount>";

                strText = "<item><word>"
    + StringUtil.GetXmlStringSimple(strID)
    + "</word>"
    + strCount
    + "<match>exact</match><relation>=</relation><dataType>string</dataType>"
    + "</item>";
                string strQueryXml = "<target list='"
                    + StringUtil.GetXmlStringSimple(this.AmerceDbName + ":" + "ID")       // 2007/9/14
                    + "'>" + strText
    + "<lang>zh</lang></target>";

                lRet = channel.DoSearch(strQueryXml,
                    "amerced",
                    "", // strOuputStyle
                    out strError);
                if (lRet == -1)
                {
                    strError = "����IDΪ '" + strID + "' ��ΥԼ���¼����: " + strError;
                    goto ERROR1;
                }

                if (lRet == 0)
                {
                    strError = "û���ҵ�idΪ '" + strID + "' ��ΥԼ���¼";
                    goto ERROR1;
                }

                List<string> aPath = null;
                lRet = channel.DoGetSearchResult(
                    "amerced",   // strResultSetName
                    0,
                    1,
                    "zh",
                    null,   // stop
                    out aPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (lRet == 0)
                {
                    strError = "��ȡ�����δ����";
                    goto ERROR1;
                }

                if (aPath.Count != 1)
                {
                    strError = "aPath.Count != 1";
                    goto ERROR1;
                }

                string strAmerceRecPath = aPath[0];

                // ����һ�����Ѽ�¼
                // parameters:
                //      bCreateOperLog  �Ƿ񴴽���־
                //      strOperTime ����Ĳ���ʱ��
                //      strOperator ����Ĳ�����
                // return:
                //      -2  �������������ټ���ѭ�����ñ�����
                //      -1  һ��������Լ���ѭ�����ñ�����
                //      0   ����
                nRet = SettlementOneRecord(
                    "", // ȷ������ִ��
                    false,  // ��������־
                    channel,
                    strAction,
                    strAmerceRecPath,
                    strOperTime,
                    strOperator,
                    "", // ��ʾ��������
                    out strError);
                if (nRet == -1 || nRet == -2)
                    goto ERROR1;

            }


            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        /*
<root>
<operation>writeRes</operation> 
<requestResPath>...</requestResPath> ��Դ·��������Ҳ��������API�ǵ�strResPath����ֵ��������·���еļ�¼ID���ְ����ʺţ���ʾҪ׷�Ӵ����µļ�¼
<resPath>...</resPath> ��Դ·������Դ��ȷ��·����
<ranges>...</ranges> �ֽڷ�Χ
<totalLength>...</totalLength> �ܳ���
<metadata>...</metadata> ��Ԫ�ص��ı����Ǽ�¼�壬��ע��Ϊ��͸�����ַ�����HtmlEncoding��ļ�¼�ַ�������
<style>...</style> �� style �а��� delete �Ӵ�ʱ��ʾҪɾ�������Դ 
<operator>test</operator> 
<operTime>Fri, 08 Dec 2006 10:12:20 GMT</operTime> 
</root>
         * ���ܻ���һ��attachment
 * * */
        public int RecoverWriteRes(
            RmsChannelCollection Channels,
            RecoverLevel level,
            XmlDocument domLog,
            Stream attachmentLog,
            out string strError)
        {
            strError = "";

            // ��ʱ��Robust����Logic����
            if (level == RecoverLevel.Robust)
                level = RecoverLevel.Logic;

            long lRet = 0;
            // int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            bool bReuse = false;    // �Ƿ��ܹ�����RecorverLevel״̬�����ò��ִ���

        DO_SNAPSHOT:

            // ���ջָ�
            if (level == RecoverLevel.Snapshot
                || bReuse == true)
            {
                string strResPath = DomUtil.GetElementText(
                    domLog.DocumentElement,
                    "resPath");
                if (string.IsNullOrEmpty(strResPath) == true)
                {
                    strError = "��־��¼��ȱ<resPath>Ԫ��";
                    return -1;
                }

                string strRanges = DomUtil.GetElementText(
    domLog.DocumentElement,
    "ranges");
                if (string.IsNullOrEmpty(strRanges) == true)
                {
                    strError = "��־��¼��ȱ<ranges>Ԫ��";
                    return -1;
                }

                string strTotalLength = DomUtil.GetElementText(
domLog.DocumentElement,
"totalLength");
                if (string.IsNullOrEmpty(strTotalLength) == true)
                {
                    strError = "��־��¼��ȱ<totalLength>Ԫ��";
                    return -1;
                }

                long lTotalLength = 0;
                try
                {
                    lTotalLength = Convert.ToInt64(strTotalLength);
                }
                catch
                {
                    strError = "lTotalLengthֵ '"+strTotalLength+"' ��ʽ����ȷ";
                    return -1;
                }
                string strMetadata = DomUtil.GetElementText(
domLog.DocumentElement,
"metadata");
                string strStyle = DomUtil.GetElementText(
domLog.DocumentElement,
"style");

                // �����¼����
                byte[] baRecord = null;

                if (attachmentLog != null && attachmentLog.Length > 0)
                {
                    baRecord = new byte[(int)attachmentLog.Length];
                    attachmentLog.Seek(0, SeekOrigin.Begin);
                    attachmentLog.Read(baRecord, 0, (int)attachmentLog.Length);
                }

                strStyle += ",ignorechecktimestamp";

                byte[] timestamp = null;
                string strOutputResPath = "";
                byte[] output_timestamp = null;

                if (StringUtil.IsInList("delete", strStyle) == true)
                {
                    // 2015/9/3 ����
                    lRet = channel.DoDeleteRes(strResPath,
                        timestamp,
                        strStyle,
                        out output_timestamp,
                        out strError);
                }
                else
                {
                    lRet = channel.WriteRes(strResPath,
        strRanges,
        lTotalLength,
        baRecord,
        strMetadata,
        strStyle,
        timestamp,
        out strOutputResPath,
        out output_timestamp,
        out strError);
                }
                if (lRet == -1)
                {
                    strError = "WriteRes() '" + strResPath + "' ʱ��������: " + strError;
                    return -1;
                }

                return 0;
            }

            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {
                // ��SnapShot��ʽ��ͬ
                bReuse = true;
                goto DO_SNAPSHOT;
            }
            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }

        /*
<root>
  <operation>repairBorrowInfo</operation> 
  <action>...</action> ���嶯�� �� repairreaderside repairitemside
  <readerBarcode>...</readerBarcode>
  <itemBarcode>...</itemBarcode>
  <confirmItemRecPath>...</confirmItemRecPath> �����ж��õĲ��¼·��
  <operator>test</operator> 
  <operTime>Fri, 08 Dec 2006 10:12:20 GMT</operTime> 
</root>
         * * 
         * */
        public int RecoverRepairBorrowInfo(
    RmsChannelCollection Channels,
    RecoverLevel level,
    XmlDocument domLog,
    Stream attachmentLog,
    out string strError)
        {
            strError = "";
            int nRet = 0;

            // ��ʱ��Robust����Logic����
            if (level == RecoverLevel.Robust)
                level = RecoverLevel.Logic;

            long lRet = 0;
            // int nRet = 0;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            bool bReuse = false;    // �Ƿ��ܹ�����RecorverLevel״̬�����ò��ִ���

DO_SNAPSHOT:

            // ���ջָ�
            if (level == RecoverLevel.Snapshot
                || bReuse == true)
            {
                string strAction = DomUtil.GetElementText(domLog.DocumentElement,
                    "action");

                string strReaderBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "readerBarcode");
                if (String.IsNullOrEmpty(strReaderBarcode) == true)
                {
                    strError = "<readerBarcode>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

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
                    if (strAction == "repairreaderside")
                    {
                        strError = "����֤����� '" + strReaderBarcode + "' ������";
                        goto ERROR1;
                    }

                    // ��ʵ���ָ���ʱ����������߼�¼�����ڵ�
                }
                if (nRet == -1)
                {
                    strError = "����֤�����Ϊ '" + strReaderBarcode + "' �Ķ��߼�¼ʱ��������: " + strError;
                    goto ERROR1;
                }

                XmlDocument readerdom = null;
                if (string.IsNullOrEmpty(strReaderXml) == false)
                {
                    nRet = LibraryApplication.LoadToDom(strReaderXml,
                        out readerdom,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                        goto ERROR1;
                    }
                }

                // У�����֤����Ų����Ƿ��XML��¼����ȫһ��
                if (readerdom != null)
                {
                    string strTempBarcode = DomUtil.GetElementText(readerdom.DocumentElement,
                        "barcode");
                    if (strReaderBarcode != strTempBarcode)
                    {
                        strError = "�޸��������ܾ��������֤����Ų��� '" + strReaderBarcode + "' �Ͷ��߼�¼��<barcode>Ԫ���ڵĶ���֤�����ֵ '" + strTempBarcode + "' ��һ�¡�";
                        goto ERROR1;
                    }
                }
                
                // ������¼
                string strConfirmItemRecPath = DomUtil.GetElementText(domLog.DocumentElement,
                    "confirmItemRecPath");
                string strItemBarcode = DomUtil.GetElementText(domLog.DocumentElement,
                    "itemBarcode");
                if (String.IsNullOrEmpty(strItemBarcode) == true)
                {
                    strError = "<strItemBarcode>Ԫ��ֵΪ��";
                    goto ERROR1;
                }

                string strItemXml = "";
                string strOutputItemRecPath = "";
                byte[] item_timestamp = null;

                // ����Ѿ���ȷ���Ĳ��¼·��
                if (String.IsNullOrEmpty(strConfirmItemRecPath) == false)
                {
                    string strMetaData = "";
                    lRet = channel.GetRes(strConfirmItemRecPath,
                        out strItemXml,
                        out strMetaData,
                        out item_timestamp,
                        out strOutputItemRecPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "����strConfirmItemRecPath '" + strConfirmItemRecPath + "' ��ò��¼ʧ��: " + strError;
                        goto ERROR1;
                    }

                    // ��Ҫ����¼�е�<barcode>Ԫ��ֵ�Ƿ�ƥ��������


                    // TODO: �����¼·�������ļ�¼�����ڣ�������<barcode>Ԫ��ֵ��Ҫ��Ĳ�����Ų�ƥ�䣬��ô��Ҫ�����߼�������Ҳ�������ò����������ü�¼��
                    // ��Ȼ����������£��ǳ�Ҫ������ȷ�����ݿ�����ʺܺã�����û��������ŵ�������֡�
                }
                else
                {
                    // �Ӳ�����Ż�ò��¼
                    List<string> aPath = null;

                    // ��ò��¼
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
                        if (strAction == "repairitemside")
                        {
                            strError = "������� '" + strItemBarcode + "' ������";
                            goto ERROR1;
                        }

                        // �Ӷ��߲�ָ���ʱ�򣬲�����Ų������������
                        goto CONTINUE_REPAIR;
                    }
                    if (nRet == -1)
                    {
                        strError = "����������Ϊ '" + strItemBarcode + "' �Ĳ��¼ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    if (aPath.Count > 1)
                    {

                        strError = "�������Ϊ '" + strItemBarcode + "' �Ĳ��¼�� " + aPath.Count.ToString() + " ��������ʱcomfirmItemRecPathȴΪ��";
                        goto ERROR1;
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

            CONTINUE_REPAIR:

                XmlDocument itemdom = null;
                if (string.IsNullOrEmpty(strItemXml) == false)
                {
                    nRet = LibraryApplication.LoadToDom(strItemXml,
                        out itemdom,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "װ�ز��¼����XML DOMʱ��������: " + strError;
                        goto ERROR1;
                    }

                    // У�������Ų����Ƿ��XML��¼����ȫһ��
                    string strTempItemBarcode = DomUtil.GetElementText(itemdom.DocumentElement,
                        "barcode");
                    if (strItemBarcode != strTempItemBarcode)
                    {
                        strError = "�޸��������ܾ����������Ų��� '" + strItemBarcode + "' �Ͳ��¼��<barcode>Ԫ���ڵĲ������ֵ '" + strTempItemBarcode + "' ��һ�¡�";
                        goto ERROR1;
                    }
                }

                if (strAction == "repairreaderside")
                {
                    XmlNode nodeBorrow = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcode + "']");
                    if (nodeBorrow == null)
                    {
                        strError = "�޸��������ܾ������߼�¼ " + strReaderBarcode + " �в��������йز� " + strItemBarcode + " �Ľ�����Ϣ��";
                        goto ERROR1;
                    }

                    if (itemdom != null)
                    {
                        // �������¼���Ƿ���ָ�ض��߼�¼����
                        string strBorrower = DomUtil.GetElementText(itemdom.DocumentElement,
                            "borrower");
                        if (strBorrower == strReaderBarcode)
                        {
                            strError = "�޸��������ܾ�����������Ҫ�޸�����������һ��������ȷ��������ֱ�ӽ�����ͨ���������";
                            goto ERROR1;
                        }
                    }

                    // �Ƴ����߼�¼�����
                    nodeBorrow.ParentNode.RemoveChild(nodeBorrow);

                    byte[] output_timestamp = null;
                    string strOutputPath = "";


                    // д�ض��߼�¼
                    lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                        readerdom.OuterXml,
                        false,
                        "content,ignorechecktimestamp",
                        reader_timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;
                }
                else if (strAction == "repairitemside")
                {
                    // �������¼���Ƿ���ָ����߼�¼����
                    string strBorrower = DomUtil.GetElementText(itemdom.DocumentElement,
                        "borrower");
                    if (String.IsNullOrEmpty(strBorrower) == true)
                    {
                        strError = "�޸��������ܾ�����������Ҫ�޸��Ĳ��¼�У�������û�н�����Ϣ�����̸�����޸���";
                        goto ERROR1;
                    }

                    if (strBorrower != strReaderBarcode)
                    {
                        strError = "�޸��������ܾ�����������Ҫ�޸��Ĳ��¼�У���û��ָ���������Ƕ��� " + strReaderBarcode + "��";
                        goto ERROR1;
                    }

                    // �������߼�¼���Ƿ���ָ��������
                    if (readerdom != null)
                    {
                        XmlNode nodeBorrow = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcode + "']");
                        if (nodeBorrow != null)
                        {
                            strError = "�޸��������ܾ�����������Ҫ�޸�����������һ��������ȷ��������ֱ�ӽ�����ͨ���������";
                            goto ERROR1;
                        }
                    }

                    // �Ƴ����¼�����
                    DomUtil.SetElementText(itemdom.DocumentElement,
                        "borrower", "");
                    DomUtil.SetElementText(itemdom.DocumentElement,
                        "borrowDate", "");
                    DomUtil.SetElementText(itemdom.DocumentElement,
                        "borrowPeriod", "");

                    byte[] output_timestamp = null;
                    string strOutputPath = "";

                    // д�ز��¼
                    lRet = channel.DoSaveTextRes(strOutputItemRecPath,
                        itemdom.OuterXml,
                        false,
                        "content,ignorechecktimestamp",
                        item_timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;
                }
                else
                {
                    strError = "����ʶ���strActionֵ '"+strAction+"'";
                    goto ERROR1;
                }

                return 0;
            }

            // �߼��ָ����߻�ϻָ�
            if (level == RecoverLevel.Logic
                || level == RecoverLevel.LogicAndSnapshot)
            {
                // ��SnapShot��ʽ��ͬ
                bReuse = true;
                goto DO_SNAPSHOT;
            }
            return 0;
        ERROR1:
            if (level == RecoverLevel.LogicAndSnapshot)
            {
                level = RecoverLevel.Snapshot;
                goto DO_SNAPSHOT;
            }
            return -1;
        }
    }

    public enum RecoverLevel
    {
        Logic = 0,  // �߼�����
        LogicAndSnapshot = 1,   // �߼���������ʧ����ת�ÿ��ջָ�
        Snapshot = 3,   // ����ȫ�ģ�����
        Robust = 4, // ��ǿ׳���ݴ�ָ���ʽ
    }
}
