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

using System.Resources;
using System.Globalization;
using System.Runtime.Serialization;

using DigitalPlatform;	// Stop��
using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Marc;
using DigitalPlatform.Range;
using DigitalPlatform.Drawing;  // ShrinkPic()

using DigitalPlatform.Message;
using DigitalPlatform.rms.Client.rmsws_localhost;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// ����������ͨҵ����صĴ���
    /// </summary>
    public partial class LibraryApplication
    {

        // Ϊͳ��ָ��"����/������"���ݴ��(��ͨ����)���һλ���ߵ�֤���롣���ܲ�̫׼ȷ
        // string m_strLastReaderBarcode = "";

        // ���߼�¼�У�������ʷ����󱣴����
        public int MaxPatronHistoryItems = 100;

        // ���¼�У�������ʷ����󱣴����
        public int MaxItemHistoryItems = 100;


        public bool VerifyBarcode = false;  // �������޸Ķ��߼�¼�����¼��ʱ���Ƿ���֤�����

        public bool AcceptBlankItemBarcode = true;
        public bool AcceptBlankReaderBarcode = true;

        public bool VerifyBookType = false;  // �������޸Ĳ��¼��ʱ���Ƿ���֤ͼ������
        public bool VerifyReaderType = false;  // �������޸Ķ��߼�¼��ʱ���Ƿ���֤��������
        public bool BorrowCheckOverdue = true;  // �����ʱ���Ƿ���δ�����ڲ�

#if NO
        // ������Դ
        // return:
        //		-1	error
        //		0	�������ص��ļ���ʵΪ�գ����ر�����
        //		1	�Ѿ�����
        public static int SaveUploadFile(
            System.Web.UI.Page page,
            RmsChannel channel,
            string strXmlRecPath,
            string strFileID,
            string strResTimeStamp,
            HttpPostedFile postedFile,
            int nLogoLimitW,
            int nLogoLimitH,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(postedFile.FileName) == true
                && postedFile.ContentLength == 0)
            {
                return 0;	// û�б�Ҫ����
            }

            WebPageStop stop = new WebPageStop(page);

            string strResPath = strXmlRecPath + "/object/" + strFileID;

            string strLocalFileName = Path.GetTempFileName();
            try
            {
                using (Stream t = File.Create(strLocalFileName))
                {
                    // ��С�ߴ�
                    int nRet = GraphicsUtil.ShrinkPic(postedFile.InputStream,
                            postedFile.ContentType,
                            nLogoLimitW,
                            nLogoLimitH,
                            true,
                            t,
                            out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)  // û�б�Ҫ����
                    {
                        postedFile.InputStream.Seek(0, SeekOrigin.Begin); // 2012/5/20
                        StreamUtil.DumpStream(postedFile.InputStream, t);
                    }
                }

            // t.Close();


                // ����ļ��ߴ�
                FileInfo fi = new FileInfo(strLocalFileName);

                if (fi.Exists == false)
                {
                    strError = "�ļ� '" + strLocalFileName + "' ������...";
                    return -1;
                }

                string[] ranges = null;

                if (fi.Length == 0)
                { // ���ļ�
                    ranges = new string[1];
                    ranges[0] = "";
                }
                else
                {
                    string strRange = "";
                    strRange = "0-" + Convert.ToString(fi.Length - 1);

                    // ����100K��Ϊһ��chunk
                    ranges = RangeList.ChunkRange(strRange,
                        100 * 1024);
                }

                byte[] timestamp = ByteArray.GetTimeStampByteArray(strResTimeStamp);
                byte[] output_timestamp = null;

                // 2007/12/13 new add
                string strLastModifyTime = DateTime.UtcNow.ToString("u");

                string strLocalPath = postedFile.FileName;

                // page.Response.Write("<br/>���ڱ���" + strLocalPath);

            REDOWHOLESAVE:
                string strWarning = "";

                for (int j = 0; j < ranges.Length; j++)
                {
                REDOSINGLESAVE:

                    // Application.DoEvents();	// ���ý������Ȩ

                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    string strWaiting = "";
                    if (j == ranges.Length - 1)
                        strWaiting = " �����ĵȴ�...";

                    string strPercent = "";
                    RangeList rl = new RangeList(ranges[j]);
                    if (rl.Count >= 1)
                    {
                        double ratio = (double)((RangeItem)rl[0]).lStart / (double)fi.Length;
                        strPercent = String.Format("{0,3:N}", ratio * (double)100) + "%";
                    }

                    if (stop != null)
                        stop.SetMessage("�������� " + ranges[j] + "/"
                            + Convert.ToString(fi.Length)
                            + " " + strPercent + " " + strLocalFileName + strWarning + strWaiting);

                    // page.Response.Write(".");	// ��ֹǰ����ȴ����ö���ʱ

                    long lRet = channel.DoSaveResObject(strResPath,
                        strLocalFileName,
                        strLocalPath,
                        postedFile.ContentType,
                        strLastModifyTime,
                        ranges[j],
                        j == ranges.Length - 1 ? true : false,	// ��βһ�β��������ѵײ�ע�����������WebService API��ʱʱ��
                        timestamp,
                        out output_timestamp,
                        out strError);

                    timestamp = output_timestamp;

                    // DomUtil.SetAttr(node, "__timestamp",	ByteArray.GetHexTimeStampString(timestamp));

                    strWarning = "";

                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {

                            timestamp = new byte[output_timestamp.Length];
                            Array.Copy(output_timestamp, 0, timestamp, 0, output_timestamp.Length);
                            strWarning = " (ʱ�����ƥ��, �Զ�����)";
                            if (ranges.Length == 1 || j == 0)
                                goto REDOSINGLESAVE;
                            goto REDOWHOLESAVE;
                        }

                        goto ERROR1;
                    }


                }


                return 1;	// �Ѿ�����
            ERROR1:
                return -1;
            }
            finally
            {
                // ��Ҫ����ɾ����ʱ�ļ�
                File.Delete(strLocalFileName);
            }
        }

#endif

        // ���ݶ���֤������ҵ�ͷ����Դ·��
        // parameters:
        //      strReaderBarcode    ����֤�����
        //      strEncryptBarcode   ���strEncryptBarcode�����ݣ���������������strReaderBarcode
        //      strDisplayName  ����֤����ʾ��������Ϊnull����ʾ����֤
        // return:
        //      -1  ����
        //      0   û���ҵ����������߼�¼�����ڣ����߶��߼�¼����û��ͷ�����
        //      1   �ҵ�
        public int GetReaderPhotoPath(
            SessionInfo sessioninfo,
            string strReaderBarcode,
            string strEncyptBarcode,
            string strDisplayName,
            out string strPhotoPath,
            out string strError)
        {
            strError = "";
            strPhotoPath = "";

            if (String.IsNullOrEmpty(strEncyptBarcode) == false)
            {
                string strTemp = LibraryApplication.DecryptPassword(strEncyptBarcode);
                if (strTemp == null)
                {
                    strError = "strEncyptBarcode�а��������ָ�ʽ����ȷ";
                    return -1;
                }
                strReaderBarcode = strTemp;
            }

            // ������߼�¼
            string strReaderXml = "";
            byte[] reader_timestamp = null;
            string strOutputReaderRecPath = "";
            int nRet = this.GetReaderRecXml(
                sessioninfo.Channels,
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
                // text-level: �ڲ�����
                strError = "������߼�¼ʱ��������: " + strError;
                return -1;
            }

            if (nRet > 1)
            {
                // text-level: �ڲ�����
                strError = "������߼�¼ʱ�����ֶ���֤����� '" + strReaderBarcode + "' ���� " + nRet.ToString() + " ��������һ�����ش�����ϵͳ����Ա���촦��";
                return -1;
            }

            XmlDocument readerdom = null;
            nRet = LibraryApplication.LoadToDom(strReaderXml,
                out readerdom,
                out strError);
            if (nRet == -1)
            {
                // text-level: �ڲ�����
                strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                return -1;
            }

            // ��֤��ʾ��
            if (String.IsNullOrEmpty(strDisplayName) == false)
            {
                string strDisplayNameValue = DomUtil.GetElementText(readerdom.DocumentElement,
                        "displayName");
                if (strDisplayName.Trim() != strDisplayNameValue.Trim())
                {
                    strError = "��Ȼ���߼�¼�ҵ��ˣ�������ʾ���Ѿ���ƥ��";
                    return 0;
                }
            }

            // �����ǲ����Ѿ���ͼ�����

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);

            // ȫ��<dprms:file>Ԫ��
            XmlNodeList nodes = readerdom.DocumentElement.SelectNodes("//dprms:file[@usage='photo']", nsmgr);

            if (nodes.Count == 0)
            {
                strError = "���߼�¼��û��ͷ�����";
                return 0;
            }

            strPhotoPath = strOutputReaderRecPath + "/object/" + DomUtil.GetAttr(nodes[0], "id");

            return 1;
        }

        // ��ö��߿�Ĺݴ���
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int GetLibraryCode(
            string strReaderRecPath,
            out string strLibraryCode,
            out string strError)
        {
            strLibraryCode = "";
            strError = "";

            string strReaderDbName = ResPath.GetDbName(strReaderRecPath);
            bool bReaderDbInCirculation = true;
            if (this.IsReaderDbName(strReaderDbName,
                out bReaderDbInCirculation,
                out strLibraryCode) == false)
            {
                // text-level: �ڲ�����
                strError = "���߼�¼·�� '" + strReaderRecPath + "' �е����ݿ��� '" + strReaderDbName + "' ���ڶ���Ķ��߿�֮�С�";
                return -1;
            }
            return 0;
        }

        static string GetBorrowActionName(string strAction)
        {
            if (strAction == "borrow")
            {
                return "����";
            }
            else if (strAction == "renew")
            {
                return "����";
            }
            else return strAction;
        }

        static string GetLibLocCode(string strLibraryUid)
        {
            if (string.IsNullOrEmpty(strLibraryUid) == true)
                return "";
            string strResult = "";
            if (strLibraryUid.Length > 0)
                strResult = strLibraryUid.Substring(0, 1);
            if (strLibraryUid.Length > 1)
                strResult += strLibraryUid.Substring(strLibraryUid.Length - 1, 1);

            return strResult;
        }

        // ���ݶ���֤����Ź����ά���ַ���
        public static string BuildQrCode(string strReaderBarcode,
            string strLibraryUid)
        {
            DateTime now = DateTime.UtcNow;
            // ʱЧ�ַ��� 20130101
            string strDateString = DateTimeUtil.DateTimeToString8(now);
            string strSalt = strDateString + "|" + strReaderBarcode + "|" + GetLibLocCode(strLibraryUid);
            string strHash = BuildPqrHash(strSalt);
            return "PQR:" + strReaderBarcode + "@" + strHash;
        }

        static string BuildPqrHash(string strText)
        {
            return Cryptography.GetSHA1(strText).ToUpper().Replace("+", "").Replace("/", "").Replace("=", "");
        }

        public int DecodeQrCode(string strCode,
            out string strReaderBarcode,
            out string strError)
        {
            strError = "";
            strReaderBarcode = "";


            if (string.IsNullOrEmpty(strCode) == true
                || strCode.Length < "PQR:".Length
                || StringUtil.HasHead(strCode, "PQR:") == false)
            {
                strError = "���Ƕ���֤�Ŷ�ά��";
                return 0;
            }

            strCode = strCode.Substring("PQR:".Length);

            string strHashcode = "";

            int nRet = strCode.IndexOf("@");
            if (nRet != -1)
            {
                strReaderBarcode = strCode.Substring(0, nRet);
                strHashcode = strCode.Substring(nRet + 1);
            }
            else
            {
                strError = "PQR �����ʽ����: ȱ���ַ� '@'";
                return -1;
            }

            string strLibraryUid = this.UID;
            string strSalt = DateTimeUtil.DateTimeToString8(DateTime.Now) + "|" + strReaderBarcode + "|" + GetLibLocCode(strLibraryUid);
            string strVerify = BuildPqrHash(strSalt);

            if (strVerify != strHashcode)
            {
                strError = "PQR �����ʽ����: У��ʧ��";
                return -1;
            }

            return 1;
        }

#if NO
        // ���ݶ���֤����Ź����ά���ַ���
        public static string BuildQrCode(string strReaderBarcode,
            string strLibraryUid)
        {
            DateTime now = DateTime.UtcNow;
            // ʱЧ�ַ��� ��ʼ��:���� �� ���� ticks ����
            string strDateString = now.Ticks.ToString() + ":" + new TimeSpan(24, 0, 0).Ticks.ToString();
            return "PQR:" + Cryptography.Encrypt(strDateString + "|" + strReaderBarcode + "@" + strLibraryUid, LibraryApplication.qrkey);
        }

        public int DecodeQrCode(string strCode,
            out string strReaderBarcode,
            out string strError)
        {
            string strLibraryUid = "";
            int nRet = DecodeQrCode(strCode,
                out strReaderBarcode,
                out strLibraryUid,
                out strError);
            if (nRet != 1)
                return nRet;
            if (strLibraryUid != this.UID)
            {
                strError = "���Ǳ��ݵĶ���֤�Ŷ�ά��";
                return -1;
            }

            return 1;
        }


        // �Ѷ�ά���ַ���ת��Ϊ����֤�����
        // parameters:
        //      strReaderBcode  [out]����֤�����
        // return:
        //      -1      ����
        //      0       ���������ַ������Ƕ���֤�Ŷ�ά��
        //      1       �ɹ�      
        public static int DecodeQrCode(string strCode,
            out string strReaderBarcode,
            out string strLibraryUid,
            out string strError)
        {
            strError = "";
            strReaderBarcode = "";
            strLibraryUid = "";

            if (string .IsNullOrEmpty(strCode) == true
                || strCode.Length < "PQR:".Length
                || StringUtil.HasHead(strCode, "PQR:") == false)
            {
                strError = "���Ƕ���֤�Ŷ�ά��";
                return 0;
            }

            strCode = strCode.Substring("PQR:".Length);

            // ����
            try
            {
                string strPlainText = Cryptography.Decrypt(strCode, LibraryApplication.qrkey);

                // ʱЧ����
                int nRet = strPlainText.IndexOf("|");
                if (nRet == -1)
                {
                    strError = "�����ʽ����";
                    return -1;
                }
                string strTimeString = strPlainText.Substring(0, nRet);
                string strTemp = strPlainText.Substring(nRet + 1);

                // ���ʱЧ��
                nRet = strTimeString.IndexOf(":");
                if (nRet == -1)
                {
                    strError = "�����ʽ����";
                    return -1;
                }
                string strStart = strTimeString.Substring(0, nRet);
                string strLength = strTimeString.Substring(nRet + 1);
                long lStart = 0;
                if (long.TryParse(strStart, out lStart) == false)
                {
                    // ��һ�����ֲ��ָ�ʽ����
                    strError = "�����ʽ����";
                    return -1;
                }
                long lLength = 0;
                if (long.TryParse(strLength, out lLength) == false)
                {
                    // �ڶ������ֲ��ָ�ʽ����
                    strError = "�����ʽ����";
                    return -1;
                }
                DateTime start = new DateTime(lStart);
                TimeSpan delta = new TimeSpan(lLength);
                DateTime now = DateTime.UtcNow;
                if (now < start || now >= start + delta)
                {
                    strError = "�����Ѿ�ʧЧ";
                    return -1;
                }

                nRet = strTemp.IndexOf("@");
                if (nRet != -1)
                {
                    strReaderBarcode = strTemp.Substring(0, nRet);
                    strLibraryUid = strTemp.Substring(nRet + 1);
                }
                else
                    strReaderBarcode = strTemp;
                return 1;
            }
            catch(Exception ex)
            {
                strError = "�����ʽ����";
                return -1;
            }
        }

#endif

        // �������ģʽ
        // return:
        //      -1  �����̳���
        //      0   ����ͨ��
        //      1   ������ͨ��
        public static int CheckTestModePath(string strPath,
            out string strError)
        {
            strError = "";

            string strRecID = ResPath.GetRecordId(strPath);
            if (string.IsNullOrEmpty(strRecID) == true
                || strRecID == "?")
                return 0;
                                // if (StringUtil.IsPureNumber(strID) == false)


            long id = 0;
            if (long.TryParse(strRecID, out id) == false)
            {
                strError = "�������ģʽ��¼·���Ĺ��̳���·�� '"+strPath+"' �еļ�¼ID '"+strRecID+"' ��������";
                return -1;
            }

            if (id >= 1 && id <= 1000)
                return 0;
            strError = "����ģʽֻ��ʹ��ID �� 1-1000 ��Χ�ڵļ�¼ (��ǰ��¼·��Ϊ '"+strPath+"')";
            return 1;
        }

        // API: ����
        // text-level: �û���ʾ OPAC���蹦��Ҫ���ô˺���
        // parameters:
        //      strReaderBarcode    ����֤����š������ʱ�����Ϊ��
        //      strItemBarcode  �������
        //      strConfirmItemRecPath  ���¼·�����ڲ�������ظ�������£�����Ҫʹ�����������ƽʱΪnull����
        //      saBorrowedItemBarcode   ͬһ������ǰ�Ѿ����ĳɹ��Ĳ�����ż��ϡ������ڷ��صĶ���html����ʾ���ض�����ɫ���ѡ�
        //      strStyle    �������"item"��ʾ�����ز��¼��"reader"��ʾ�����ض��߼�¼
        //      strItemFormat   �涨strItemRecord���������ص����ݸ�ʽ
        //      strItemRecord   ���ز��¼
        //      strReaderFormat �涨strReaderRecord���������ص����ݸ�ʽ
        //      strReaderRecord ���ض��߼�¼
        //      aDupPath    �������������ظ������ﷵ������ز��¼��·��
        //      return_time ���ν���Ҫ��������ʱ�䡣GMTʱ�䡣
        // Ȩ�ޣ����۹�����Ա���Ƕ��ߣ�����Ӧ�߱�borrow��renewȨ�ޡ�
        //      ���ڶ��ߣ�����Ҫ�����еĽ���(����)����������Լ��ģ���strReaderBarcode������˻���Ϣ�е�֤�����һ�¡�
        //      Ҳ����˵�����߲����������˽���(����)ͼ�飬�����涨��Ϊ�˷�ֹ���ߵ��ҡ�
        public LibraryServerResult Borrow(
            SessionInfo sessioninfo,
            bool bRenew,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            bool bForce,
            string[] saBorrowedItemBarcode,
            string strStyle,
            string strItemFormatList,
            out string [] item_records,
            string strReaderFormatList,
            out string [] reader_records,

            string strBiblioFormatList, // 2008/5/9 new add
            out string[] biblio_records, // 2008/5/9 new add

            out string[] aDupPath,
            out string strOutputReaderBarcodeParam, // 2011/9/25
            out BorrowInfo borrow_info   // 2007/12/6 new add
            )
        {
            item_records = null;
            reader_records = null;
            biblio_records = null;
            aDupPath = null;
            strOutputReaderBarcodeParam = "";
            borrow_info = new BorrowInfo();

            DateTime start_time = DateTime.Now;

            LibraryServerResult result = new LibraryServerResult();

            string strAction = "borrow";
            if (bRenew == true)
                strAction = "renew";

            string strActionName = GetBorrowActionName(strAction);

            // ������ի��
            string strPersonalLibrary = "";
            if (sessioninfo.UserType == "reader"
                && sessioninfo.Account != null)
                strPersonalLibrary = sessioninfo.Account.PersonalLibrary;

            // Ȩ���ж�
            if (bRenew == false)
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("borrow", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "���Ĳ������ܾ������߱�borrowȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    // return result;
                }

                // �Զ�����ݵĸ����ж�
                // ע�����и�����ի�ģ������Լ������ִ��
                if (sessioninfo.UserType == "reader"
                    && sessioninfo.Account != null && strReaderBarcode != sessioninfo.Account.Barcode
                    && string.IsNullOrEmpty(strPersonalLibrary) == true)
                {
                    result.Value = -1;
                    result.ErrorInfo = "���Ĳ������ܾ�����Ϊ���߲��ܴ����˽��н��Ĳ�����";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }
            else
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("renew", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    // text-level: �û���ʾ
                    result.ErrorInfo = this.GetString("����������ܾ������߱�renewȨ�ޡ�"); // "����������ܾ������߱�renewȨ�ޡ�"
                    result.ErrorCode = ErrorCode.AccessDenied;
                    // return result;
                }

                // �Զ�����ݵĸ����ж�
                // ע�����и�����ի�ģ������Լ������ִ��
                if (sessioninfo.UserType == "reader"
                    && sessioninfo.Account != null && strReaderBarcode != sessioninfo.Account.Barcode
                    && string.IsNullOrEmpty(strPersonalLibrary) == true)
                {
                    result.Value = -1;
                    // text-level: �û���ʾ
                    result.ErrorInfo = this.GetString("����������ܾ�����Ϊ���߲��ܴ����˽������������");  // "����������ܾ�����Ϊ���߲��ܴ����˽������������"
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }

            // ���û����ͨ��Ȩ�ޣ���ҪԤ����ȡȨ��
            LibraryServerResult result_save = null;
            if (result.Value == -1 && String.IsNullOrEmpty(sessioninfo.Access) == false)
            {
                string strAccessActionList = GetDbOperRights(sessioninfo.Access,
                        "", // ��ʱ����֪��ʵ���������ȡ�õ�ǰ�ʻ���������һ��ʵ���Ĵ�ȡ����
                        "circulation");
                if (string.IsNullOrEmpty(strAccessActionList) == true)
                    return result;

                // ͨ��������һ�����󣬺�����ȻҪ����ȡȨ�ޡ�
                // ����������У���ȷ���ĳ��ʵ���Ĵ�ȡȨ�޴��ڣ�������ȡȨ�ޣ���������ڣ�������ͨȨ��
                result_save = result.Clone();
            }
            else if (result.Value == -1)
                return result;  // �ӳٱ��� 2014/9/16

            result = new LibraryServerResult();

            string strError = "";

            if (bForce == true)
            {
                strError = "bForce��������Ϊtrue";
                goto ERROR1;
            }

            int nRet = 0;
            long lRet = 0;
            string strIdcardNumber = "";    // ���֤��
            string strQrCode = "";  //

            if (string.IsNullOrEmpty(strReaderBarcode) == false)
            {
                string strOutputCode = "";
                // �Ѷ�ά���ַ���ת��Ϊ����֤�����
                // parameters:
                //      strReaderBcode  [out]����֤�����
                // return:
                //      -1      ����
                //      0       ���������ַ������Ƕ���֤�Ŷ�ά��
                //      1       �ɹ�      
                nRet = DecodeQrCode(strReaderBarcode,
                    out strOutputCode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 1)
                {
                    strQrCode = strReaderBarcode;
                    strReaderBarcode = strOutputCode;
                }
            }


            int nRedoCount = 0; // ��Ϊʱ�����ͻ, ���ԵĴ���
            string strLockReaderBarcode = strReaderBarcode; // ����ר���ַ��������º��汻�޸���

            REDO_BORROW:

            bool bReaderLocked = false;

            string strOutputReaderXml = "";
            string strOutputItemXml = "";
            string strBiblioRecID = "";
            string strOutputItemRecPath = "";
            string strOutputReaderRecPath = "";
            string strLibraryCode = "";

            // �Ӷ��߼�¼��
            // this.ReaderLocks.LockForWrite(strLockReaderBarcode);
            if (String.IsNullOrEmpty(strReaderBarcode) == false)
            {
                // �Ӷ��߼�¼��
                strLockReaderBarcode = strReaderBarcode;
                this.ReaderLocks.LockForWrite(strReaderBarcode);
                bReaderLocked = true;
                strOutputReaderBarcodeParam = strReaderBarcode;
            }

            try // ���߼�¼������Χ��ʼ
            {

                // ��ȡ���߼�¼
                XmlDocument readerdom = null;
                byte[] reader_timestamp = null;
                string strOldReaderXml = "";

                if (string.IsNullOrEmpty(strReaderBarcode) == false)
                {
                    LibraryServerResult result1 = GetReaderRecord(
                sessioninfo,
                strActionName,
                ref strReaderBarcode,
                ref strIdcardNumber,
                ref strLibraryCode,
                out readerdom,
                out strOutputReaderRecPath,
                out reader_timestamp);
                    if (result1.Value == 0)
                    {
                    }
                    else
                    {
                        return result1;
                    }

                    // �����޸�ǰ�Ķ��߼�¼
                    strOldReaderXml = readerdom.OuterXml;

                    if (String.IsNullOrEmpty(strIdcardNumber) == false
                        || string.IsNullOrEmpty(strReaderBarcode) == true /* 2013/5/23 */)
                    {
                        // ��ö���֤�����
                        strReaderBarcode = DomUtil.GetElementText(readerdom.DocumentElement,
                            "barcode");
                    }
                    strOutputReaderBarcodeParam = DomUtil.GetElementText(readerdom.DocumentElement,
                            "barcode");

                    string strReaderDbName = ResPath.GetDbName(strOutputReaderRecPath);

                    // ��鵱ǰ�û���Ͻ�Ķ��߷�Χ
                    // return:
                    //      -1  ����
                    //      0   �����������
                    //      1   Ȩ�����ƣ�������������ʡ�strError ����˵��ԭ�������
                    nRet = CheckReaderRange(sessioninfo,
                        readerdom,
                        strReaderDbName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == 1)
                    {
                        // strError = "��ǰ�û� '" + sessioninfo.UserID + "' �Ĵ�ȡȨ�޻���ѹ�ϵ��ֹ��������(֤�����Ϊ " + strReaderBarcode + ")������ԭ��" + strError;
                        result.Value = -1;
                        result.ErrorInfo = strError;
                        result.ErrorCode = ErrorCode.AccessDenied;
                        return result;
                    }
                }

                byte[] item_timestamp = null;
                List<string> aPath = null;
                string strItemXml = "";
                // string strOutputItemRecPath = "";

                RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                if (channel == null)
                {
                    // text-level: �ڲ�����
                    strError = "get channel error";
                    goto ERROR1;
                }

                // �Ӳ��¼��
                this.EntityLocks.LockForWrite(strItemBarcode);

                try // ���¼������Χ��ʼ
                {
                    // ������¼
                    DateTime start_time_read_item = DateTime.Now;

                    // ����Ѿ���ȷ���Ĳ��¼·��
                    if (String.IsNullOrEmpty(strConfirmItemRecPath) == false)
                    {
                        // ���·���еĿ������ǲ���ʵ�����
                        // return:
                        //      -1  error
                        //      0   ����ʵ�����
                        //      1   ��ʵ�����
                        nRet = this.CheckItemRecPath(strConfirmItemRecPath,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 0)
                        {
                            strError = strConfirmItemRecPath + strError;
                            goto ERROR1;
                        }

                        string strMetaData = "";

                        lRet = channel.GetRes(strConfirmItemRecPath,
                            out strItemXml,
                            out strMetaData,
                            out item_timestamp,
                            out strOutputItemRecPath,
                            out strError);
                        if (lRet == -1)
                        {
                            // text-level: �ڲ�����
                            strError = "����strConfirmItemRecPath '" + strConfirmItemRecPath + "' ��ò��¼ʧ��: " + strError;
                            goto ERROR1;
                        }
                    }
                    else
                    {
                        // �Ӳ�����Ż�ò��¼

                        // ��ò��¼
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   ����1��
                        //      >1  ���ж���1��
                        nRet = this.GetItemRecXml(
                            sessioninfo.Channels,
                            strItemBarcode,
                            out strItemXml,
                            100,
                            out aPath,
                            out item_timestamp,
                            out strError);
                        if (nRet == 0)
                        {
                            result.Value = -1;
                            // text-level: �û���ʾ
                            result.ErrorInfo = string.Format(this.GetString("�������s������"),   // "������� '{0}' ������"
                                strItemBarcode);

                            // "������� '" + strItemBarcode + "' ������";
                            result.ErrorCode = ErrorCode.ItemBarcodeNotFound;
                            return result;
                        }
                        if (nRet == -1)
                        {
                            // text-level: �ڲ�����
                            strError = "������¼ʱ��������: " + strError;
                            goto ERROR1;
                        }

                        if (aPath.Count > 1)
                        {
                            if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(
                                strLibraryCode,
                                "����",
                                "��������������ظ�����",
                                1);

                            result.Value = -1;
                            // text-level: �û���ʾ
                            result.ErrorInfo = string.Format(this.GetString("�������Ϊs�Ĳ��¼��s�����޷����н��Ĳ���"),  // "�������Ϊ '{0}' �Ĳ��¼�� "{1}" �����޷����н��Ĳ��������ڸ��Ӳ��¼·���������ύ���Ĳ�����"
                                strItemBarcode,
                                aPath.Count.ToString());
                            this.WriteErrorLog(result.ErrorInfo);   // 2012/12/30

                            // "�������Ϊ '" + strItemBarcode + "' �Ĳ��¼�� " + aPath.Count.ToString() + " �����޷����н��Ĳ��������ڸ��Ӳ��¼·���������ύ���Ĳ�����";
                            result.ErrorCode = ErrorCode.ItemBarcodeDup;

                            aDupPath = new string[aPath.Count];
                            aPath.CopyTo(aDupPath);
                            return result;
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

                    string strItemDbName = "";

                    // �������¼�����������ݿ⣬�Ƿ��ڲ�����ͨ��ʵ���֮��
                    // 2008/6/4 new add
                    if (String.IsNullOrEmpty(strOutputItemRecPath) == false)
                    {
                        strItemDbName = ResPath.GetDbName(strOutputItemRecPath);
                        bool bItemDbInCirculation = true;
                        if (this.IsItemDbName(strItemDbName, out bItemDbInCirculation) == false)
                        {
                            // text-level: �ڲ�����
                            strError = "���¼·�� '" + strOutputItemRecPath + "' �е����ݿ��� '" + strItemDbName + "' ��Ȼ���ڶ����ʵ���֮�С�";
                            goto ERROR1;
                        }

                        if (bItemDbInCirculation == false)
                        {
                            // text-level: �ڲ�����
                            strError = "����������ܾ���������� '" + strItemBarcode + "' ���ڵĲ��¼ '" + strOutputItemRecPath + "' �������ݿ� '" + strItemDbName + "' ����δ������ͨ��ʵ���";
                            goto ERROR1;
                        }
                    }

                    // ����ȡȨ��
                    string strAccessParameters = "";

                    {

                        // ����ȡȨ�� circulation
                        if (String.IsNullOrEmpty(sessioninfo.Access) == false)
                        {
                            string strAccessActionList = "";
                            strAccessActionList = GetDbOperRights(sessioninfo.Access,
                                strItemDbName,
                                "circulation");
#if NO
                            if (String.IsNullOrEmpty(strAccessActionList) == true && result_save != null)
                            {
                                // TODO: Ҳ����ֱ�ӷ��� result_save
                                strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strItemDbName + "' ִ�� ���� �����Ĵ�ȡȨ��";
                                result.Value = -1;
                                result.ErrorInfo = strError;
                                result.ErrorCode = ErrorCode.AccessDenied;
                                return result;
                            }
#endif
                            if (strAccessActionList == null)
                            {
                                strAccessActionList = GetDbOperRights(sessioninfo.Access,
            "", // ��ʱ����֪��ʵ���������ȡ�õ�ǰ�ʻ���������һ��ʵ���Ĵ�ȡ����
            "circulation");
                                if (strAccessActionList == null)
                                {
                                    // ������ʵ��ⶼû�ж����κδ�ȡȨ�ޣ���ʱ��Ҫ�˶�ʹ����ͨȨ��
                                    strAccessActionList = sessioninfo.Rights;

                                    // ע����ʵ��ʱ result_save == null ��������ͨȨ�޼���Ѿ�ͨ���˵�
                                }
                                else
                                {
                                    // ������ʵ��ⶨ���˴�ȡȨ�ޣ����� strItemDbName û�ж���
                                    strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strItemDbName + "' ִ�� ���� �����Ĵ�ȡȨ��";
                                    result.Value = -1;
                                    result.ErrorInfo = strError;
                                    result.ErrorCode = ErrorCode.AccessDenied;
                                    return result;
                                }
                            }

                            if (strAccessActionList == "*")
                            {
                                // ͨ��
                            }
                            else
                            {
                                if (IsInAccessList(strAction, strAccessActionList, out strAccessParameters) == false)
                                {
                                    strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strItemDbName + "' ִ�� ����  " + strActionName + " �����Ĵ�ȡȨ��";
                                    result.Value = -1;
                                    result.ErrorInfo = strError;
                                    result.ErrorCode = ErrorCode.AccessDenied;
                                    return result;
                                }
                            }
                        }
                    }



                    XmlDocument itemdom = null;
                    nRet = LibraryApplication.LoadToDom(strItemXml,
                        out itemdom,
                        out strError);
                    if (nRet == -1)
                    {
                        // text-level: �ڲ�����
                        strError = "װ�ز��¼����XML DOMʱ��������: " + strError;
                        goto ERROR1;
                    }

                    WriteTimeUsed(start_time_read_item, "Borrow()�ж�ȡ���¼ ��ʱ ");

                    DateTime start_time_process = DateTime.Now;

                    // �������ģʽ����Ŀ��¼·��
                    if (this.TestMode == true || sessioninfo.TestMode == true)
                    {
                        string strBiblioDbName = "";
                        // ����ʵ�����, �ҵ���Ӧ����Ŀ����
                        // return:
                        //      -1  ����
                        //      0   û���ҵ�
                        //      1   �ҵ�
                        nRet = this.GetBiblioDbNameByItemDbName(strItemDbName,
                            out strBiblioDbName,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "����ʵ����� '"+strItemDbName+"' �����Ŀ����ʱ����: " + strError;
                            goto ERROR1;
                        }

                        string strParentID = DomUtil.GetElementText(itemdom.DocumentElement,
    "parent");
                        // �������ģʽ
                        // return:
                        //      -1  �����̳���
                        //      0   ����ͨ��
                        //      1   ������ͨ��
                        nRet = CheckTestModePath(strBiblioDbName + "/" + strParentID,
                            out strError);
                        if (nRet != 0)
                        {
                            strError = strActionName + "�������ܾ�: " + strError;
                            goto ERROR1;
                        }
                    }

                    // ***
                    // �ӳٻ�ö���֤�����
                    if (string.IsNullOrEmpty(strReaderBarcode) == true)
                    {
                        if (bRenew == false)
                        {
                            strError = "�����ṩ strReaderBarcode ����ֵ���ܽ��� " +strActionName+ " ����";
                            goto ERROR1;
                        }

                        string strOutputReaderBarcode = ""; // ���صĽ�����֤�����
                        // �ڲ��¼�л�ý�����֤�����
                        // return:
                        //      -1  ����
                        //      0   �ò�Ϊδ���״̬
                        //      1   �ɹ�
                        nRet = GetBorrowerBarcode(itemdom,
                            out strOutputReaderBarcode,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = strError + " (���¼·��Ϊ '" + strOutputItemRecPath + "')";
                            goto ERROR1;
                        }

                        if (nRet == 0 || string.IsNullOrEmpty(strOutputReaderBarcode) == true)
                        {
                            strError = "�� '"+strItemBarcode+"' ��ǰδ�����κζ��߽��ģ������޷�����"+strActionName+"����";
                            goto ERROR1;
                        }
#if NO
                    // ����ṩ�˶���֤����ţ�����Ҫ��ʵ
                    if (String.IsNullOrEmpty(strReaderBarcode) == false)
                    {
                        if (strOutputReaderBarcode != strReaderBarcode)
                        {
                            // ��ʱ�������ͺ���֤
                            bDelayVerifyReaderBarcode = true;
                            strIdcardNumber = strReaderBarcode;
                        }
                    }
#endif

                        if (String.IsNullOrEmpty(strReaderBarcode) == true)
                            strReaderBarcode = strOutputReaderBarcode;

                        // *** ������߼�¼��ǰ��û������, ����������
                        if (bReaderLocked == false)
                        {
#if NO
                        // �Ӷ��߼�¼��
                        strLockReaderBarcode = strReaderBarcode;
                        this.ReaderLocks.LockForWrite(strLockReaderBarcode);
                        bReaderLocked = true;
                        strOutputReaderBarcodeParam = strReaderBarcode;
#endif
                            Debug.Assert(string.IsNullOrEmpty(strReaderBarcode) == false, "");
                            goto REDO_BORROW;
                        }
                    }

                    // ***


                    // У�����֤����Ų����Ƿ��XML��¼����ȫһ��
                    string strTempReaderBarcode = DomUtil.GetElementText(readerdom.DocumentElement,
                        "barcode");

                    if (string.IsNullOrEmpty(strReaderBarcode) == false
                        && strReaderBarcode != strTempReaderBarcode)
                    {
                        // text-level: �ڲ�����
                        strError = "���Ĳ������ܾ��������֤����Ų��� '" + strReaderBarcode + "' �Ͷ��߼�¼��<barcode>Ԫ���ڵĶ���֤�����ֵ '" + strTempReaderBarcode + "' ��һ�¡�";
                        goto ERROR1;
                    }

                    // 2007/1/2 new add
                    // У�������Ų����Ƿ��XML��¼����ȫһ��

                    string strRefID = "";
                    string strHead = "@refID:";
                    // string strFrom = "������";
                    if (StringUtil.HasHead(strItemBarcode, strHead, true) == true)
                    {
                        // strFrom = "�ο�ID";
                        strRefID = strItemBarcode.Substring(strHead.Length);

                        string strTempRefID = DomUtil.GetElementText(itemdom.DocumentElement,
    "refID");
                        if (strRefID != strTempRefID)
                        {
                            // text-level: �ڲ�����
                            strError = "���Ĳ������ܾ������ο�ID���� '" + strRefID + "' �Ͳ��¼��<refID>Ԫ���ڵĲ������ֵ '" + strTempRefID + "' ��һ�¡�";
                            goto ERROR1;
                        }
                    }
                    else
                    {
                        string strTempItemBarcode = DomUtil.GetElementText(itemdom.DocumentElement,
    "barcode");
                        if (strItemBarcode != strTempItemBarcode)
                        {
                            // text-level: �ڲ�����
                            strError = "���Ĳ������ܾ����������Ų��� '" + strItemBarcode + "' �Ͳ��¼��<barcode>Ԫ���ڵĲ������ֵ '" + strTempItemBarcode + "' ��һ�¡�";
                            goto ERROR1;
                        }
                    }




                    string strReaderType = DomUtil.GetElementText(readerdom.DocumentElement,
                        "readerType");

                    Calendar calendar = null;
                    nRet = GetReaderCalendar(strReaderType,
                        strLibraryCode,
                        out calendar,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;



                    /*
                    string strBookType = DomUtil.GetElementText(itemdom.DocumentElement,
                        "bookType");
                     */

                    bool bReaderDomChanged = false;

                    // ˢ����ͣ��������
                    if (StringUtil.IsInList("pauseBorrowing", this.OverdueStyle) == true)
                    {
                        //
                        // ������ͣ������
                        // return:
                        //      -1  error
                        //      0   readerdomû���޸�
                        //      1   readerdom�������޸�
                        nRet = ProcessPauseBorrowing(
                            strLibraryCode,
                            ref readerdom,
                            strOutputReaderRecPath,
                            sessioninfo.UserID,
                            "refresh",
                            sessioninfo.ClientAddress,  // ǰ�˴���
                            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �ڲ�����
                            strError = "��ˢ����ͣ����Ĺ����з�������: " + strError;
                            goto ERROR1;
                        }

                        if (nRet == 1)
                            bReaderDomChanged = true;
                    }

                    byte[] output_timestamp = null;
                    string strOutputPath = "";

                    // ������Ȩ��
                    // return:
                    //      -1  ���ò�������
                    //      0   Ȩ�޲��������Ĳ���Ӧ�����ܾ�
                    //      1   Ȩ�޹�
                    nRet = CheckBorrowRights(
                        sessioninfo.Account,
                        calendar,
                        bRenew,
                        strLibraryCode, // ���߼�¼���ڶ��߿�Ĺݴ���
                        strAccessParameters,
                        ref  readerdom,
                        ref  itemdom,
                        out  strError);
                    if (nRet == -1 || nRet == 0)
                    {
                        // ����б�Ҫ����ض��߼�¼(��ǰ��ˢ������ͣ��������)
                        if (bReaderDomChanged == true)
                        {
                            string strError_1 = "";
                            /*
                            byte[] output_timestamp = null;
                            string strOutputPath = "";
                             * */

                            // д�ض��߼�¼
                            lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                                readerdom.OuterXml,
                                false,
                                "content",  // ,ignorechecktimestamp
                                reader_timestamp,
                                out output_timestamp,
                                out strOutputPath,
                                out strError_1);
                            if (lRet == -1)
                            {
                                // text-level: �ڲ�����
                                strError = strError + "��Ȼ����д����߼�¼�����У���������: " + strError_1;
                                goto ERROR1;
                            }
                        }

                        if (nRet == 0)
                        {
                            result.Value = -1;
                            result.ErrorInfo = strError;
                            result.ErrorCode = ErrorCode.AccessDenied;  // Ȩ�޲���
                            return result;
                        }

                        goto ERROR1;
                    }



                    XmlDocument domOperLog = new XmlDocument();
                    domOperLog.LoadXml("<root />");
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "libraryCode",
                        strLibraryCode);    // �������ڵĹݴ���
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "operation",
                        "borrow");
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "action",
                        bRenew == true ? "renew" : "borrow");
                    // ԭ��������






                    // ����API�Ĵ�������
                    // ���ԤԼ�����Ϣ
                    // return:
                    //      -1  error
                    //      0   ����
                    //      1   ���ָòᱻ����)�� ���ܽ���
                    //      2   ���ָò�ԤԼ�� ��������
                    //      3   ���ָòᱻ������ ���ܽ��ġ����ұ������޸��˲��¼(<location>Ԫ�ط����˱仯)����Ҫ���������غ󣬰Ѳ��¼���档
                    nRet = DoBorrowReservationCheck(
                        sessioninfo,
                        bRenew,
                        ref readerdom,
                        ref itemdom,
                        bForce,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == 1 || nRet == 2)
                    {
                        // ��ԤԼ����, ���ܽ���
                        result.Value = -1;
                        result.ErrorInfo = strError;
                        if (nRet == 1)
                            result.ErrorCode = ErrorCode.BorrowReservationDenied;
                        if (nRet == 2)
                            result.ErrorCode = ErrorCode.RenewReservationDenied;
                        return result;
                    }

                    if (nRet == 3)
                    {
                        result.Value = -1;
                        result.ErrorInfo = strError;
                        result.ErrorCode = ErrorCode.BorrowReservationDenied;

                        /*
                        byte[] output_timestamp = null;
                        string strOutputPath = "";
                         * */

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
                            // text-level: �ڲ�����
                            strError = "���Ĳ��������ڼ�ԤԼͼ����Ҫд�ز��¼ " + strOutputItemRecPath + " ʱ����: " + strError;
                            this.WriteErrorLog(strError);
                            strError += "�����Ĳ������ܾ���";
                            goto ERROR1;
                        }

                        return result;
                    }

                    // �ƶ�������

                    // �ڶ��߼�¼�Ͳ��¼����ӽ�����Ϣ
                    // string strNewReaderXml = "";
                    nRet = DoBorrowReaderAndItemXml(
                        bRenew,
                        strLibraryCode,
                        ref readerdom,
                        ref itemdom,
                        bForce,
                        sessioninfo.UserID,
                        strOutputItemRecPath,
                        strOutputReaderRecPath,
                        ref domOperLog,
                        out borrow_info,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;


                    WriteTimeUsed(start_time_process, "Borrow()�н��и������ݴ��� ��ʱ ");


                    DateTime start_time_write_reader = DateTime.Now;
                    // ԭ�����xml��xml������ڴ�


                    // д�ض��ߡ����¼
                    // byte[] timestamp = null;


                    // д�ض��߼�¼
                    lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
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
                                // text-level: �ڲ�����
                                strError = "д�ض��߼�¼��ʱ��,����ʱ�����ͻ,���������10��,��ʧ��...";
                                goto ERROR1;
                            }
                            goto REDO_BORROW;
                        }
                        goto ERROR1;
                    }

                    WriteTimeUsed(start_time_write_reader, "Borrow()��д�ض��߼�¼ ��ʱ ");

                    DateTime start_time_write_item = DateTime.Now;

                    // ��ʱ����ʱ���
                    reader_timestamp = output_timestamp;

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
                        // ҪUndo�ղŶԶ��߼�¼��д��
                        string strError1 = "";
                        lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                            strOldReaderXml,
                            false,
                            "content",  // ,ignorechecktimestamp
                            reader_timestamp,
                            out output_timestamp,
                            out strOutputPath,
                            out strError1);
                        if (lRet == -1) // ����Undoʧ��
                        {
                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                // ���߼�¼Undo��ʱ��, ����ʱ�����ͻ��
                                // ��ʱ��Ҫ�����ִ��¼, ��ͼɾ�������ӵ�<borrows><borrow>Ԫ��
                                // return:
                                //      -1  error
                                //      0   û�б�ҪUndo
                                //      1   Undo�ɹ�
                                nRet = UndoBorrowReaderRecord(
                                    channel,
                                    strOutputReaderRecPath,
                                    strReaderBarcode,
                                    strItemBarcode,
                                    out strError);
                                if (nRet == -1)
                                {
                                    // text-level: �ڲ�����
                                    strError = "Undo���߼�¼ '" + strOutputReaderRecPath + "' (����֤�����Ϊ'" + strReaderBarcode + "') ���Ĳ������ '" + strItemBarcode + "' ���޸�ʱ�����������޷�Undo: " + strError;
                                    this.WriteErrorLog(strError);
                                    goto ERROR1;
                                }

                                // �ɹ�
                                goto REDO_BORROW;
                            }

                            // ����Ϊ ����ʱ�����ͻ��������������
                            // text-level: �ڲ�����
                            strError = "Undo���߼�¼ '" + strOutputReaderRecPath + "' (����֤�����Ϊ'" + strReaderBarcode + "') ���Ĳ������ '" + strItemBarcode + "' ���޸�ʱ�����������޷�Undo: " + strError;
                            // strError = strError + ", ����Undoд�ؾɶ��߼�¼Ҳʧ��: " + strError1;
                            this.WriteErrorLog(strError);
                            goto ERROR1;
                        } // end of ����Undoʧ��

                        // ����ΪUndo�ɹ�������
                        goto REDO_BORROW;

                    } // end of д�ز��¼ʧ��

                    WriteTimeUsed(start_time_write_item, "Borrow()��д�ز��¼ ��ʱ ");

                    DateTime start_time_write_operlog = DateTime.Now;

                    // д����־
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "confirmItemRecPath", strConfirmItemRecPath);

                    if (string.IsNullOrEmpty(strIdcardNumber) == false)
                    {
                        // ������ʹ�����֤������ɽ��Ĳ�����
                        DomUtil.SetElementText(domOperLog.DocumentElement,
        "idcardNumber", strIdcardNumber);
                    }

                    // д����߼�¼
                    XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                        "readerRecord", readerdom.OuterXml);
                    DomUtil.SetAttr(node, "recPath", strOutputReaderRecPath);

                    // д����¼
                    node = DomUtil.SetElementText(domOperLog.DocumentElement,
                        "itemRecord", itemdom.OuterXml);
                    DomUtil.SetAttr(node, "recPath", strOutputItemRecPath);

                    nRet = this.OperLog.WriteOperLog(domOperLog,
                        sessioninfo.ClientAddress,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "Borrow() API д����־ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    WriteTimeUsed(start_time_write_operlog, "Borrow()��д������־ ��ʱ ");

                    DateTime start_time_write_statis = DateTime.Now;

                    // д��ͳ��ָ��
#if NO
                    if (this.m_strLastReaderBarcode != strReaderBarcode)
                    {
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            "����",
                            "������",
                            1);
                        this.m_strLastReaderBarcode = strReaderBarcode;

                    }
#endif
                    if (this.Garden != null)
                        this.Garden.Activate(strReaderBarcode,
                            strLibraryCode);

                    if (this.Statis != null)
                        this.Statis.IncreaseEntryValue(
                        strLibraryCode,
                        "����",
                        "���",
                        1);

                    WriteTimeUsed(start_time_write_statis, "Borrow()��дͳ��ָ�� ��ʱ ");


                    strOutputItemXml = itemdom.OuterXml;
                    strOutputReaderXml = readerdom.OuterXml;
                    strBiblioRecID = DomUtil.GetElementText(itemdom.DocumentElement, "parent"); //

                } // ���¼������Χ����
                finally
                {
                    // ����¼��
                    this.EntityLocks.UnlockForWrite(strItemBarcode);    // strItemBarcode �����������в������޸�
                }

            } // ���߼�¼������Χ����
            finally
            {
                // this.ReaderLocks.UnlockForWrite(strLockReaderBarcode);
                if (bReaderLocked == true)
                    this.ReaderLocks.UnlockForWrite(strLockReaderBarcode);
            }

            // �������
            // ��������ݲ��ַ��ڶ��������ⷶΧ����Ϊ�˾�������������ʱ�䣬��߲�������Ч��

            if (String.IsNullOrEmpty(strOutputReaderXml) == false
                && StringUtil.IsInList("reader", strStyle) == true)
            {
                DateTime start_time_1 = DateTime.Now;

                string[] reader_formats = strReaderFormatList.Split(new char[] { ',' });
                reader_records = new string[reader_formats.Length];

                for (int i = 0; i < reader_formats.Length; i++)
                {
                    string strReaderFormat = reader_formats[i];
                    // �����߼�¼���ݴ�XML��ʽת��ΪHTML��ʽ
                    // if (String.Compare(strReaderFormat, "html", true) == 0)
                    if (IsResultType(strReaderFormat, "html") == true)
                    {
                        string strReaderRecord = "";
                        nRet = this.ConvertReaderXmlToHtml(
                            sessioninfo,
                            this.CfgDir + "\\readerxml2html.cs",
                            this.CfgDir + "\\readerxml2html.cs.ref",
                            strLibraryCode,
                            strOutputReaderXml,
                            strOutputReaderRecPath, // 2009/10/18 new add
                            OperType.Borrow,
                            saBorrowedItemBarcode,
                            strItemBarcode,
                            strReaderFormat,
                            out strReaderRecord,
                            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �û���ʾ
                            strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                                strError);
                            // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: " + strError;
                            goto ERROR1;
                        }
                        reader_records[i] = strReaderRecord;

                    }
                    // �����߼�¼���ݴ�XML��ʽת��Ϊtext��ʽ
                    // else if (String.Compare(strReaderFormat, "text", true) == 0)
                    else if (IsResultType(strReaderFormat, "text") == true)
                    {
                        string strReaderRecord = "";
                        nRet = this.ConvertReaderXmlToHtml(
                            sessioninfo,
                            this.CfgDir + "\\readerxml2text.cs",
                            this.CfgDir + "\\readerxml2text.cs.ref",
                            strLibraryCode,
                            strOutputReaderXml,
                            strOutputReaderRecPath, // 2009/10/18 new add
                            OperType.Borrow,
                            saBorrowedItemBarcode,
                            strItemBarcode,
                            strReaderFormat,
                            out strReaderRecord,
                            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �û���ʾ
                            strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                                strError);
                            goto ERROR1;
                        }
                        reader_records[i] = strReaderRecord;
                    }
                    // else if (String.Compare(strReaderFormat, "xml", true) == 0)
                    else if (IsResultType(strReaderFormat, "xml") == true)
                    {
                        // reader_records[i] = strOutputReaderXml;
                        string strResultXml = "";
                        nRet = GetItemXml(strOutputReaderXml,
            strReaderFormat,
            out strResultXml,
            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �û���ʾ
                            strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                                strError);
                            goto ERROR1;
                        }
                        reader_records[i] = strResultXml;
                    }
                    else if (IsResultType(strReaderFormat, "summary") == true)
                    {
                        // 2013/12/15
                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(strOutputReaderXml);
                        }
                        catch (Exception ex)
                        {
                            strError = "���� XML װ�� DOM ����: " + ex.Message;
                            // text-level: �û���ʾ
                            strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                                strError);
                            goto ERROR1;
                        }
                        reader_records[i] = DomUtil.GetElementText(dom.DocumentElement, "name");
                    }
                    else
                    {
                        strError = "strReaderFormatList���������˲�֧�ֵ����ݸ�ʽ���� '" + strReaderFormat + "'";
                        // text-level: �û���ʾ
                        strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                            strError);
                        goto ERROR1;
                    }


                } // end of for

                WriteTimeUsed(start_time_1, "Borrow()��ת��xml��¼ ��ʱ ");
            }

            if (String.IsNullOrEmpty(strOutputItemXml) == false
                && StringUtil.IsInList("item", strStyle) == true)
            {
                string[] item_formats = strItemFormatList.Split(new char[] { ',' });
                item_records = new string[item_formats.Length];

                for (int i = 0; i < item_formats.Length; i++)
                {
                    string strItemFormat = item_formats[i];

                    // �����¼���ݴ�XML��ʽת��ΪHTML��ʽ
                    //if (String.Compare(strItemFormat, "html", true) == 0)
                    if (IsResultType(strItemFormat, "html") == true)
                    {
                        string strItemRecord = "";
                        nRet = this.ConvertItemXmlToHtml(
                            this.CfgDir + "\\itemxml2html.cs",
                            this.CfgDir + "\\itemxml2html.cs.ref",
                            strOutputItemXml,
                            strOutputItemRecPath,   // 2009/10/18 new add
                            out strItemRecord,
                            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �û���ʾ
                            strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                                strError);
                            goto ERROR1;
                        }
                        item_records[i] = strItemRecord;
                    }
                    // �����¼���ݴ�XML��ʽת��Ϊtext��ʽ
                    // else if (String.Compare(strItemFormat, "text", true) == 0)
                    else if (IsResultType(strItemFormat, "text") == true)
                    {
                        string strItemRecord = "";
                        nRet = this.ConvertItemXmlToHtml(
                            this.CfgDir + "\\itemxml2text.cs",
                            this.CfgDir + "\\itemxml2text.cs.ref",
                            strOutputItemXml,
                            strOutputItemRecPath,   // 2009/10/18 new add
                            out strItemRecord,
                            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �û���ʾ
                            strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                                strError);
                            goto ERROR1;
                        }
                        item_records[i] = strItemRecord;
                    }
                    // else if (String.Compare(strItemFormat, "xml", true) == 0)
                    else if (IsResultType(strItemFormat, "xml") == true)
                    {
                        string strResultXml = "";
                        nRet = GetItemXml(strOutputItemXml,
            strItemFormat,
            out strResultXml,
            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �û���ʾ
                            strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                                strError);
                            goto ERROR1;
                        }
                        item_records[i] = strResultXml;
                    }
                    else
                    {
                        strError = "strItemFormatList���������˲�֧�ֵ����ݸ�ʽ���� '" + strItemFormat + "'";
                        // text-level: �û���ʾ
                        strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                            strError);
                        goto ERROR1;
                    }
                } // end of for
            }

            // 2008/5/9 new add
            if (StringUtil.IsInList("biblio", strStyle) == true)
            {
                if (String.IsNullOrEmpty(strBiblioRecID) == true)
                {
                    strError = "���¼XML��<parent>Ԫ��ȱ������ֵΪ��, ����޷���λ�ּ�¼ID";
                    // text-level: �û���ʾ
                    strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                        strError);
                    goto ERROR1;
                }

                string strItemDbName = ResPath.GetDbName(strOutputItemRecPath);

                string strBiblioDbName = "";
                // ����ʵ�����, �ҵ���Ӧ����Ŀ����
                // return:
                //      -1  ����
                //      0   û���ҵ�
                //      1   �ҵ�
                nRet = this.GetBiblioDbNameByItemDbName(strItemDbName,
                    out strBiblioDbName,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    strError = "ʵ����� '" + strItemDbName + "' û���ҵ���Ӧ����Ŀ����";
                    // text-level: �û���ʾ
                    strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                        strError);
                    goto ERROR1;
                }

                string strBiblioRecPath = strBiblioDbName + "/" + strBiblioRecID;

                string[] biblio_formats = strBiblioFormatList.Split(new char[] { ',' });
                biblio_records = new string[biblio_formats.Length];

                string strBiblioXml = "";
                // ������html xml text֮һ���Ż�ȡstrBiblioXml
                if (StringUtil.IsInList("html", strBiblioFormatList) == true
                    || StringUtil.IsInList("xml", strBiblioFormatList) == true
                    || StringUtil.IsInList("text", strBiblioFormatList) == true)
                {
                    RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "get channel error";
                        // text-level: �û���ʾ
                        strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                            strError);
                        goto ERROR1;
                    }

                    string strMetaData = "";
                    byte[] timestamp = null;
                    string strTempOutputPath = "";
                    lRet = channel.GetRes(strBiblioRecPath,
                        out strBiblioXml,
                        out strMetaData,
                        out timestamp,
                        out strTempOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "����ּ�¼ '" + strBiblioRecPath + "' ʱ����: " + strError;
                        // text-level: �û���ʾ
                        strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                            strError);
                        goto ERROR1;
                    }
                }

                for (int i = 0; i < biblio_formats.Length; i++)
                {
                    string strBiblioFormat = biblio_formats[i];

                    // ��Ҫ���ں�ӳ������ļ�
                    string strLocalPath = "";
                    string strBiblio = "";

                    // ����Ŀ��¼���ݴ�XML��ʽת��ΪHTML��ʽ
                    if (String.Compare(strBiblioFormat, "html", true) == 0)
                    {
                        // TODO: ����cache
                        nRet = this.MapKernelScriptFile(
                            sessioninfo,
                            strBiblioDbName,
                            "./cfgs/loan_biblio.fltx",
                            out strLocalPath,
                            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �û���ʾ
                            strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                                strError);
                            goto ERROR1;
                        }

                        // ���ּ�¼���ݴ�XML��ʽת��ΪHTML��ʽ
                        string strFilterFileName = strLocalPath;    // app.CfgDir + "\\biblio.fltx";
                        if (string.IsNullOrEmpty(strBiblioXml) == false)
                        {
                            nRet = this.ConvertBiblioXmlToHtml(
                                strFilterFileName,
                                strBiblioXml,
                                    null,
                                strBiblioRecPath,
                                out strBiblio,
                                out strError);
                            if (nRet == -1)
                            {
                                // text-level: �û���ʾ
                                strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                                    strError);
                                goto ERROR1;
                            }

                        }
                        else
                            strBiblio = "";

                        biblio_records[i] = strBiblio;
                    }
                    // �����¼���ݴ�XML��ʽת��Ϊtext��ʽ
                    else if (String.Compare(strBiblioFormat, "text", true) == 0)
                    {
                        // TODO: ����cache
                        nRet = this.MapKernelScriptFile(
                            sessioninfo,
                            strBiblioDbName,
                            "./cfgs/loan_biblio_text.fltx",
                            out strLocalPath,
                            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �û���ʾ
                            strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                                strError);
                            goto ERROR1;
                        }
                        // ���ּ�¼���ݴ�XML��ʽת��ΪTEXT��ʽ
                        string strFilterFileName = strLocalPath;    // app.CfgDir + "\\biblio.fltx";
                        if (string.IsNullOrEmpty(strBiblioXml) == false)
                        {
                            nRet = this.ConvertBiblioXmlToHtml(
                                strFilterFileName,
                                strBiblioXml,
                                    null,
                                strBiblioRecPath,
                                out strBiblio,
                                out strError);
                            if (nRet == -1)
                            {
                                // text-level: �û���ʾ
                                strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                                    strError);
                                goto ERROR1;
                            }

                        }
                        else
                            strBiblio = "";

                        biblio_records[i] = strBiblio;
                    }
                    else if (String.Compare(strBiblioFormat, "xml", true) == 0)
                    {
                        biblio_records[i] = strBiblioXml;
                    }
                    else if (String.Compare(strBiblioFormat, "recpath", true) == 0)
                    {
                        biblio_records[i] = strBiblioRecPath;
                    }
                    else if (string.IsNullOrEmpty(strBiblioFormat) == true)
                    {
                        biblio_records[i] = "";
                    }
                    else
                    {
                        strError = "strBiblioFormatList���������˲�֧�ֵ����ݸ�ʽ���� '" + strBiblioFormat + "'";
                        // text-level: �û���ʾ
                        strError = string.Format(this.GetString("��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�s"),   // "��Ȼ���������д��󣬵��ǽ��Ĳ����Ѿ��ɹ�: {0}";
                            strError);
                        goto ERROR1;
                    }
                } // end of for
            }

            WriteTimeUsed(start_time, "Borrow() ��ʱ ");
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        LibraryServerResult GetReaderRecord(
            SessionInfo sessioninfo,
            string strActionName,
            ref string strReaderBarcode,    // 2015/1/4 ���� ref
            ref string strIdcardNumber,
            ref string strLibraryCode,
            out XmlDocument readerdom,
            out string strOutputReaderRecPath,
            out byte[] reader_timestamp)
        {
            string strError = "";
            int nRet = 0;

            LibraryServerResult result = new LibraryServerResult();

            strOutputReaderRecPath = "";
            readerdom = null;
            reader_timestamp = null;

                DateTime start_time_read_reader = DateTime.Now;

                // ������߼�¼
                string strReaderXml = "";
                nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
                    strReaderBarcode,
                    out strReaderXml,
                    out strOutputReaderRecPath,
                    out reader_timestamp,
                    out strError);
                if (nRet == -1)
                {
                    // text-level: �ڲ�����
                    strError = "������߼�¼ʱ��������: " + strError;
                    goto ERROR1;
                }

                if (nRet == 0)
                {
                    // ��������֤�ţ�����̽���������֤�š�;��
                    if (StringUtil.IsIdcardNumber(strReaderBarcode) == true)
                    {
                        strIdcardNumber = strReaderBarcode;
                        strReaderBarcode = ""; // ��ʹ�������غ����»�� reader barcode

                        // ͨ���ض�����;����ö��߼�¼
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   ����1��
                        //      >1  ���ж���1��
                        nRet = this.GetReaderRecXmlByFrom(
                            sessioninfo.Channels,
                            strIdcardNumber,
                            "���֤��",
                            out strReaderXml,
                            out strOutputReaderRecPath,
                            out reader_timestamp,
                            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �ڲ�����
                            strError = "�����֤�� '" + strIdcardNumber + "' ������߼�¼ʱ��������: " + strError;
                            goto ERROR1;
                        }
                        if (nRet == 0)
                        {
                            result.Value = -1;
                            // text-level: �û���ʾ
                            result.ErrorInfo = string.Format(this.GetString("���֤��s������"),   // "���֤�� '{0}' ������"
                                strIdcardNumber);
                            result.ErrorCode = ErrorCode.IdcardNumberNotFound;
                            return result;
                        }
                        if (nRet > 1)
                        {
                            // text-level: �û���ʾ
                            result.Value = -1;
                            result.ErrorInfo = "�����֤�� '" + strIdcardNumber + "' �������߼�¼���� " + nRet.ToString() + " ��������޷������֤�������н軹�����������֤����������н軹������";
                            result.ErrorCode = ErrorCode.IdcardNumberDup;
                            return result;
                        }
                        Debug.Assert(nRet == 1, "");
                        goto SKIP0;
                    }
                    else
                    {
                        // 2013/5/24
                        // �����Ҫ���Ӷ���֤�ŵȸ���;�����м���
                        foreach (string strFrom in this.PatronAdditionalFroms)
                        {
                            nRet = this.GetReaderRecXmlByFrom(
                                sessioninfo.Channels,
                                null,
                                strReaderBarcode,
                                strFrom,
                                out strReaderXml,
                                out strOutputReaderRecPath,
                                out reader_timestamp,
                                out strError);
                            if (nRet == -1)
                            {
                                // text-level: �ڲ�����
                                strError = "��" + strFrom + " '" + strReaderBarcode + "' ������߼�¼ʱ��������: " + strError;
                                goto ERROR1;
                            }
                            if (nRet == 0)
                                continue;
                            if (nRet > 1)
                            {
                                // text-level: �û���ʾ
                                result.Value = -1;
                                result.ErrorInfo = "��" + strFrom + " '" + strReaderBarcode + "' �������߼�¼���� " + nRet.ToString() + " ��������޷���"+strFrom+"�����н軹�����������֤����������н軹������";
                                result.ErrorCode = ErrorCode.IdcardNumberDup;
                                return result;
                            }

                            strReaderBarcode = "";

#if NO
                            result.ErrorInfo = strError;
                            result.Value = nRet;
#endif
                            goto SKIP0;
                        }
                    }

                    result.Value = -1;
                    // text-level: �û���ʾ
                    result.ErrorInfo = string.Format(this.GetString("����֤�����s������"),   // "����֤����� '{0}' ������"
                        strReaderBarcode);
                    // "����֤����� '" + strReaderBarcode + "' ������";
                    result.ErrorCode = ErrorCode.ReaderBarcodeNotFound;
                    return result;
                }

                // 2008/6/17 new add
                if (nRet > 1)
                {
                    // text-level: �ڲ�����
                    strError = "������߼�¼ʱ�����ֶ���֤����� '" + strReaderBarcode + "' ���� " + nRet.ToString() + " ��������һ�����ش�����ϵͳ����Ա���촦��";
                    goto ERROR1;
                }

            SKIP0:

                // �������߼�¼�����������ݿ⣬�Ƿ��ڲ�����ͨ�Ķ��߿�֮��
                // 2008/6/4 new add
                if (String.IsNullOrEmpty(strOutputReaderRecPath) == false)
                {
                    if (this.TestMode == true || sessioninfo.TestMode == true)
                    {
                        // �������ģʽ
                        // return:
                        //      -1  �����̳���
                        //      0   ����ͨ��
                        //      1   ������ͨ��
                        nRet = CheckTestModePath(strOutputReaderRecPath,
                            out strError);
                        if (nRet != 0)
                        {
                            strError = strActionName + "�������ܾ�: " + strError;
                            goto ERROR1;
                        }
                    }

                    string strReaderDbName = ResPath.GetDbName(strOutputReaderRecPath);
                    bool bReaderDbInCirculation = true;
                    if (this.IsReaderDbName(strReaderDbName,
                        out bReaderDbInCirculation,
                        out strLibraryCode) == false)
                    {
                        // text-level: �ڲ�����
                        strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �е����ݿ��� '" + strReaderDbName + "' ��Ȼ���ڶ���Ķ��߿�֮�С�";
                        goto ERROR1;
                    }

                    if (bReaderDbInCirculation == false)
                    {
                        // text-level: �û���ʾ
                        strError = string.Format(this.GetString("����������ܾ�������֤�����s���ڵĶ��߼�¼s�������ݿ�s����δ������ͨ�Ķ��߿�"),  // "����������ܾ�������֤����� '{0}' ���ڵĶ��߼�¼ '{1}' �������ݿ� '{2}' ����δ������ͨ�Ķ��߿�"
                            strReaderBarcode,
                            strOutputReaderRecPath,
                            strReaderDbName);

                        // "����������ܾ�������֤����� '" + strReaderBarcode + "' ���ڵĶ��߼�¼ '" +strOutputReaderRecPath + "' �������ݿ� '" +strReaderDbName+ "' ����δ������ͨ�Ķ��߿�";
                        goto ERROR1;
                    }
                    // ��鵱ǰ�������Ƿ��Ͻ������߿�
                    // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                    if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
            sessioninfo.LibraryCodeList) == false)
                    {
                        strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                        goto ERROR1;
                    }
                }


                nRet = LibraryApplication.LoadToDom(strReaderXml,
                    out readerdom,
                    out strError);
                if (nRet == -1)
                {
                    // text-level: �ڲ�����
                    strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                    goto ERROR1;
                }

                WriteTimeUsed(start_time_read_reader, "Borrow()�ж�ȡ���߼�¼ ��ʱ ");

                return result;
            ERROR1:
                result.Value = -1;
                result.ErrorInfo = strError;
                result.ErrorCode = ErrorCode.SystemError;
                return result;
        }

        // ���߼�¼�� �������ڶ��߷�Χ���˵�Ԫ�����б�
        static string[] reader_content_fields = new string[] {
                "barcode",
                "state",
                "readerType",
                "createDate",
                "expireDate",
                "name",
                "namePinyin",
                "gender",
                "birthday",
                "dateOfBirth",
                "idCardNumber",
                "department",
                "post",
                "address",
                "tel",
                "email",
                "comment",
                "cardNumber",
                "displayName",  // ��ʾ��
                "nation",
                "rights",
                "personalLibrary",
                "friends",
            };

        // ��鵱ǰ�û���Ͻ�Ķ��߷�Χ
        // parameters:
        //      
        // return:
        //      -1  ����
        //      0   �����������
        //      1   Ȩ�����ƣ�������������ʡ�strError ����˵��ԭ�������
        static int CheckReaderRange(
            SessionInfo sessioninfo,
            XmlDocument reader_dom,
            string strReaderDbName,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strAccessString = sessioninfo.Access;
            string strReaderBarcode = DomUtil.GetElementText(reader_dom.DocumentElement, "barcode");

            if (String.IsNullOrEmpty(strAccessString) == false)
            {
                // return:
                //      -1  ����
                //      0   �����������
                //      1   Ȩ�����ƣ�������������ʡ�strError ����˵��ԭ�������
                //      2   û�ж�����صĴ�ȡ�������
                nRet = AccessReaderRange(
                    strAccessString,
                    reader_dom,
                    strReaderDbName,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 1)
                {
                    strError = "��ǰ�û� '" + sessioninfo.UserID + "' �Ĵ�ȡȨ�޽�ֹ��������(֤�����Ϊ " + strReaderBarcode + ")������ԭ��" + strError;
                    return 1;
                }
                if (nRet == 0)
                    return 0;
            }

            if (sessioninfo.UserType == "reader")
            {
                // ���� ������ �Ƿ���ǲ������Լ�?
                if (sessioninfo.UserID == strReaderBarcode)
                    return 0;

                // û��ƥ��� reader ���룬��ô�Ϳ� reader_dom �е� fiends Ԫ��
                string strFields = DomUtil.GetElementText(reader_dom.DocumentElement, "friends");
                if (string.IsNullOrEmpty(strFields) == false)
                {
                    // �жϵ�ǰ�û��Ƿ�Ϊ reader_dom ���ߵ� friends
                    if (StringUtil.IsInList(sessioninfo.Account.Barcode, strFields) == true)
                        return 0;
                    strError = "'" + sessioninfo.Account.Barcode + "' ���� '" + strReaderBarcode + "' �ĺ����б���";
                    strError = "������ (֤�����Ϊ " + strReaderBarcode + ") �ĺ��ѹ�ϵ��ֹ��ǰ�û� '" + sessioninfo.UserID + "' ���в���)������ԭ��" + strError;
                    return 1;
                }
                else
                {
                    // û�ж����κκ��ѹ�ϵ
                    strError = "'" + strReaderBarcode + "' ��δ��������б�";
                    strError = "������ (֤�����Ϊ " + strReaderBarcode + ") �ĺ��ѹ�ϵ��ֹ��ǰ�û� '" + sessioninfo.UserID + "' ���в���)������ԭ��" + strError;
                    return 1;
                }
            }
            else
                return 0;
        }

        // ����ȡȨ���е� reader
        // parameters:
        //      
        // return:
        //      -1  ����
        //      0   �����������
        //      1   Ȩ�����ƣ�������������ʡ�strError ����˵��ԭ�������
        //      2   û�ж�����صĴ�ȡ�������
        static int AccessReaderRange(
            string strAccessString,
            XmlDocument reader_dom,
            string strReaderDbName,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(strAccessString) == true)
                return 2;

            string strAccessActionList = "";
            strAccessActionList = GetDbOperRights(strAccessString,
                strReaderDbName,
                "reader");
            if (strAccessActionList == "*")
            {
                // ͨ��
                return 0;
            }

            if (strAccessActionList == null)
                return 2;

            foreach (string name in reader_content_fields)
            {
                string strAccessParameters = "";
                if (IsInAccessList(name, strAccessActionList, out strAccessParameters) == false)
                    continue;

                // ƥ��һ�������ֶ�
                // parameters:
                //      strName     �ֶ���
                //      strMatchCase  �ֶ�����ƥ��ģʽ @��������������ʽ����������ͨ�Ǻ�ģ��ƥ�䷽ʽ
                // return:
                //      -1  ����
                //      0   û��ƥ����
                //      1   ƥ������
                nRet = MatchReaderField(reader_dom,
                    name,
                    strAccessParameters,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                    return 1;
            }

            return 0;
        }

        // ƥ��һ�������ֶ�
        // parameters:
        //      strName     �ֶ���
        //      strMatchCase  �ֶ�����ƥ��ģʽ @��������������ʽ����������ͨ�Ǻ�ģ��ƥ�䷽ʽ
        // return:
        //      -1  ����
        //      0   û��ƥ���ϡ�strError ����˵��ԭ�������
        //      1   ƥ������
        static int MatchReaderField(XmlDocument reader_dom,
            string strName,
            string strMatchCase,
            out string strError)
        {
            strError = "";

            string strValue = DomUtil.GetElementText(reader_dom.DocumentElement, strName);

            string strPattern = "";

            // Regular expression
            if (strMatchCase.Length >= 1
                && strMatchCase[0] == '@')
            {
                strPattern = strMatchCase.Substring(1);
            }
            else
                strPattern = WildcardToRegex(strMatchCase);

            if (StringUtil.RegexCompare(strPattern,
                    RegexOptions.None,
                    strValue) == true)
                return 1;

            strError = "�ֶ� " + strName + " ���� '" + strValue + "' �޷�ƥ�� '" + strMatchCase + "'";
            return 0;
        }

        static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".")
            + "$";
        }

        int GetItemXml(string strItemXml,
            string strFormat,
            out string strResultXml,
            out string strError)
        {
            strError = "";
            strResultXml = "";
            int nRet = 0;

            if (string.IsNullOrEmpty(strItemXml) == true)
                return 0;

            string strSubType = "";
            string strType = "";
            StringUtil.ParseTwoPart(strFormat,
                ":",
                out strType,
                out strSubType);

            if (string.IsNullOrEmpty(strSubType) == true)
            {
                strResultXml = strItemXml;
                return 0;
            }

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strItemXml);
            }
            catch (Exception ex)
            {
                strError = "XML װ�� DOM ʱ����: " + ex.Message;
                return -1;
            }

            if (dom.DocumentElement == null)
            {
                strResultXml = strItemXml;
                return 0;
            }

            // ȥ�� <borrowHistory> ���¼�Ԫ��
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("borrowHistory/*");
            foreach (XmlNode node in nodes)
            {
                node.ParentNode.RemoveChild(node);
            }

            strResultXml = dom.OuterXml;
            return 0;
        }

        // �ڵ����ļ���д��ķ�ʱ����Ϣ
        void WriteTimeUsed(DateTime start_time,
            string strPrefix)
        {
            if (this.DebugMode == false)
                return;
            TimeSpan delta = DateTime.Now - start_time;
            string strTiming = strPrefix + " " + delta.ToString();
            this.WriteDebugInfo(strTiming);
        }

        #region Borrow()�¼�����

        // ����API�Ĵ�������
        // ������Ȩ��
        // text-level: �û���ʾ OPAC������Ҫ����Borrow()�������������ñ�����
        // parameters:
        //      strLibraryCode  ���߼�¼���ڶ��߿�Ĺݴ���
        //      strAccessParameters ��ɲ����Ĺݲصص��б����Ϊ �� ���� "*"����ʾȫ�����
        // return:
        //      -1  ���ò�������
        //      0   Ȩ�޲��������Ĳ���Ӧ�����ܾ�
        //      1   Ȩ�޹�
        int CheckBorrowRights(
            Account account,
            Calendar calendar,
            bool bRenew,
            string strLibraryCode,
            string strAccessParameters,
            ref XmlDocument readerdom,
            ref XmlDocument itemdom,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            LibraryApplication app = this;

            if (StringUtil.IsInList("pauseBorrowing", this.OverdueStyle) == true)
            {
                /* ��һ���Ѿ��ƶ�������������ȥ���ˣ���Ϊ�漰����readerdom���޸�����
                //
                // ������ͣ������
                // return:
                //      -1  error
                //      0   readerdomû���޸�
                //      1   readerdom�������޸�
                nRet = ProcessPauseBorrowing(ref readerdom,
                    "refresh",
                    out strError);
                if (nRet == -1)
                {
                    strError = "��ˢ����ͣ����Ĺ����з�������: " + strError;
                    return -1;
                }
                 * */

                // �Ƿ������ͣ�������
                string strMessage = "";
                nRet = HasPauseBorrowing(
                    calendar,
                    strLibraryCode,
                    readerdom,
                    out strMessage,
                    out strError);
                if (nRet == -1)
                {
                    // text-level: �ڲ�����
                    strError = "�ڼ�����ͣ����Ĺ����з�������: " + strError;
                    return -1;
                }
                if (nRet == 1)
                {
                    // text-level: �û���ʾ
                    strError = string.Format(this.GetString("���Ĳ������ܾ�����ö���s"),   // "���Ĳ������ܾ�����ö���{0}"
                        strMessage);

                    // "���Ĳ������ܾ�����ö���" + strMessage;
                    return 0;
                }
            }

            string strOperName = this.GetString("����");

            if (bRenew == true)
                strOperName = this.GetString("����");

            string strRefID = DomUtil.GetElementText(itemdom.DocumentElement,
"refID"); 
            string strReaderBarcode = DomUtil.GetElementText(readerdom.DocumentElement, "barcode");
            string strItemBarcode = DomUtil.GetElementText(itemdom.DocumentElement, "barcode");

            string strItemBarcodeParam = strItemBarcode;
            if (String.IsNullOrEmpty(strItemBarcode) == true)
            {
#if NO
                // text-level: �ڲ�����
                strError = "���¼�в�����Ų���Ϊ��";
                return -1;
#endif
                // ����������Ϊ�գ���ʹ�� �ο�ID
                if (String.IsNullOrEmpty(strRefID) == true)
                {
                    // text-level: �ڲ�����
                    strError = "���¼�в�����źͲο�ID��ӦͬʱΪ��";
                    return -1;
                }
                strItemBarcodeParam = "@refID:" + strRefID;
            }

            // �ݲصص�
            string strLocation = DomUtil.GetElementText(itemdom.DocumentElement, "location");

            // ȥ��#reservation����
            // StringUtil.RemoveFromInList("#reservation", true, ref strLocation);
            strLocation = StringUtil.GetPureLocationString(strLocation);

            // ���������Ĺݲصص��Ƿ�϶������ڵĹݲصص��Ǻ�
            string strRoom = "";
            string strCode = "";
            {

                // ����
                ParseCalendarName(strLocation,
            out strCode,
            out strRoom);
                if (strCode != strLibraryCode)
                {
                    strError = "���Ĳ������ܾ�������¼�Ĺݲص� '" + strLocation + "' �����ڶ������ڹݴ��� '" + strLibraryCode + "' ";
                    return 0;
                }
            }

            // ���ݲص��б�
            if (string.IsNullOrEmpty(strAccessParameters) == false && strAccessParameters != "*")
            {
                bool bFound = false;
                List<string> locations = StringUtil.SplitList(strAccessParameters);
                foreach (string s in locations)
                {
                    string c = "";
                    string r = "";
                    ParseCalendarName(s,
                        out c,
                        out r);
                    if (/*string.IsNullOrEmpty(c) == false && */ c != "*")
                    {
                        if (c != strCode)
                            continue;
                    }

                    if (/*string.IsNullOrEmpty(r) == false && */ r != "*")
                    {
                        if (r != strRoom)
                            continue;
                    }

                    bFound = true;
                    break;
                }

                if (bFound == false)
                {
                    strError = "���Ĳ������ܾ�������¼�Ĺݲص� '" + strLocation + "' ���ڵ�ǰ�û���ȡ����涨�Ĺݲص���ɷ�Χ '" + strAccessParameters + "' ֮��";
                    return 0;
                }
            }

            // 2006/12/29
            // �����Ƿ��ܹ������
            bool bResultValue = false;
            string strMessageText = "";

            // ִ�нű�����ItemCanBorrow
            // parameters:
            // return:
            //      -2  not found script
            //      -1  ����
            //      0   �ɹ�
            nRet = app.DoItemCanBorrowScriptFunction(
                bRenew,
                account,
                itemdom,
                out bResultValue,
                out strMessageText,
                out strError);
            if (nRet == -1)
            {
                // text-level: �ڲ�����
                strError = "ִ��CanBorrow()�ű�����ʱ����: " + strError;
                return -1;
            }
            if (nRet == -2)
            {
                // ���û�����ýű��������͸��ݹݲصص�쿴�ص����������������Ƿ��������
                List<string> locations = app.GetLocationTypes(strLibraryCode, true);
                if (locations.IndexOf(strRoom) == -1)
                {
                    // text-level: �û���ʾ
                    strError = string.Format(this.GetString("��s�Ĺݲصص�Ϊs�����涨�˲᲻�������"),  // "�� {0} �Ĺݲصص�Ϊ {1}�����涨(<locationTypes>����)�˲᲻������衣"
                        strItemBarcodeParam,
                        strLocation);

                    // "�� " + strItemBarcode + " �Ĺݲصص�Ϊ " + strLocation + "�����涨(<locationTypes>����)�˲᲻������衣";
                    return 0;
                }
            }
            else
            {
                // ���ݽű����ؽ��
                if (bResultValue == false)
                {
                    strError = string.Format(this.GetString("������s����Ϊ��s��״̬Ϊs"),   // "������ {0}����Ϊ�� {1} ��״̬Ϊ {2}"
                        strOperName,
                        strItemBarcodeParam,
                        strMessageText);
                    /*
                    strError = "������" 
                        + (bRenew == true ? "����" : "���")
                        + "����Ϊ�� " + strItemBarcode + " ��״̬Ϊ "+strMessageText;
                     * */
                    return 0;
                }

            }

            // 
            // ������ի�ļ��
            string strPersonalLibrary = "";
            if (account != null)
                strPersonalLibrary = account.PersonalLibrary;

            if (string.IsNullOrEmpty(strPersonalLibrary) == false)
            {
                if (strRoom != "*" && StringUtil.IsInList(strRoom, strPersonalLibrary) == false)
                {
                    strError = "��ǰ�û� '" + account.Barcode + "' ֻ�ܲ����ݴ��� '" + strLibraryCode + "' �еص�Ϊ '" + strPersonalLibrary + "' ��ͼ�飬���ܲ����ص�Ϊ '" + strRoom + "' ��ͼ��";
                    // text-level: �û���ʾ
                    strError = string.Format(this.GetString("s�������ܾ���ԭ��s"),  // "{0} �������ܾ���ԭ��: {1}"
                        strOperName,
                        strError);
                    return 0;
                }
            }

            if (bRenew == false)
            {
                // �����߼�¼���Ƿ��Ѿ����˶�Ӧ���<borrow>
                XmlNode node = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcodeParam + "']");
                if (node != null)
                {
                    if (string.IsNullOrEmpty(strItemBarcode) == false)
                        node = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcode + "']");
                    if (node != null)
                    {
                        // text-level: �û���ʾ

                        // string strReaderBarcode = DomUtil.GetElementText(readerdom.DocumentElement, "barcode");
                        strError = "���Ĳ������ܾ������� '" + strReaderBarcode + "' �����Ѿ������˲� '" + strItemBarcodeParam + "' ��(���߼�¼���Ѵ��ڶ�Ӧ��<borrow>Ԫ��)";
                        // strError = "����ǰ�ڶ��߼�¼�з��־�Ȼ�Ѵ��ڱ������߽����˲�'"+strItemBarcode+"'���ֶ���Ϣ " + node.OuterXml;
                        return -1;
                    }
                }
            }

            // ������֤�Ƿ��ڣ��Ƿ��й�ʧ��״̬
            // return:
            //      -1  �����̷����˴���Ӧ�������ܽ���������
            //      0   ���Խ���
            //      1   ֤�Ѿ�����ʧЧ�ڣ����ܽ���
            //      2   ֤�в��ý��ĵ�״̬
            nRet = CheckReaderExpireAndState(readerdom,
                out strError);
            if (nRet != 0)
            {
                // text-level: �û���ʾ
                strError = string.Format(this.GetString("s�������ܾ���ԭ��s"),  // "{0} �������ܾ���ԭ��: {1}"
                    strOperName,
                    strError);
                // strOperName + "�������ܾ���ԭ��: " + strError;
                return -1;
            }

            // ����Ƿ��Ѿ��м����˵�<overdue>�ֶ�
            XmlNodeList nodes = readerdom.DocumentElement.SelectNodes("overdues/overdue");
            if (nodes.Count > 0)
            {
                // text-level: �û���ʾ
                strError = string.Format(this.GetString("�ö��ߵ�ǰ��s���ѻ�ΥԼ��¼��δ����"), // "�ö��ߵ�ǰ�� {0} ���ѻ�ΥԼ��¼��δ�������{1}�������ܾ�������߾������ΥԼ������������罻��ΥԼ�𣩣�Ȼ�󷽿ɽ���{2}��"
                    nodes.Count.ToString(),
                    strOperName,
                    strOperName);
                // "�ö��ߵ�ǰ�� " + Convert.ToString(nodes.Count) + " ���ѻ�ΥԼ��¼��δ�������" + strOperName + "�������ܾ�������߾������ΥԼ������������罻��ΥԼ�𣩣�Ȼ�󷽿ɽ���" + strOperName + "��";
                return 0;
            }

            if (this.BorrowCheckOverdue == true)
            {
                // ��鵱ǰ�Ƿ���Ǳ�ڵĳ��ڲ�
                // return:
                //      -1  error
                //      0   û�г��ڲ�
                //      1   �г��ڲ�
                nRet = CheckOverdue(
                    calendar,
                    readerdom,
                    false,  // bForce,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 1)
                {
                    // text-level: �û���ʾ
                    strError = string.Format(this.GetString("��Ϊ���ڣ�����s���ܾ�"),   //  + "{0}�����{1}�������ܾ�������߾��콫��Щ�ѳ��ڲ����л���������"
                        strError,
                        strOperName);
                    // strError + "�����" + strOperName + "�������ܾ�������߾��콫��Щ�ѳ��ڲ����л���������";
                    return 0;
                }
            }


            // 2008/4/14 new add
            string strBookState = DomUtil.GetElementText(itemdom.DocumentElement, "state");
            if (String.IsNullOrEmpty(strBookState) == false)
            {
                // text-level: �û���ʾ
                strError = string.Format(this.GetString("s�������ܾ�����Ϊ��״̬Ϊs"),  // "{0}�������ܾ���ԭ��: �� '{1}' ��״̬Ϊ '{2}'��"
                    strOperName,
                    strItemBarcodeParam,
                    strBookState);
                // strOperName + "�������ܾ���ԭ��: �� '" + strItemBarcode + "' ��״̬Ϊ '"+ strBookState + "'��";
                return 0;
            }

            // 2010/3/19
            XmlNode nodeParentItem = itemdom.DocumentElement.SelectSingleNode("binding/bindingParent");
            if (nodeParentItem != null)
            {
                // text-level: �û���ʾ
                strError = string.Format(this.GetString("s�������ܾ�����Ϊ�϶���Ա�᲻�ܵ������"),  // "{0}�������ܾ���ԭ��: �϶���Ա�� {1} ���ܵ�����衣"
                    strOperName,
                    strItemBarcodeParam);
                return 0;
            }


            // ����Ҫ���ĵĲ���Ϣ�У��ҵ�ͼ������
            string strBookType = DomUtil.GetElementText(itemdom.DocumentElement, "bookType");

            // �Ӷ�����Ϣ��, �ҵ���������
            string strReaderType = DomUtil.GetElementText(readerdom.DocumentElement, "readerType");

            // �״ν����������Ҫ�жϲ�����������
            // �������������Ϊ��ǰ�Ľ����Ѿ��жϹ����Ȩ���ˣ���˲����ж���
            if (bRenew == false)
            {
                // �Ӷ�����Ϣ�У��ҳ��ö�����ǰ�Ѿ����Ĺ���ͬ��ͼ��Ĳ���
                nodes = readerdom.DocumentElement.SelectNodes("borrows/borrow[@type='" + strBookType + "']");

                int nThisTypeCount = nodes.Count;

                // �õ�����ͼ��Ĳ�����������
                MatchResult matchresult;
                string strParamValue = "";
                // return:
                //      reader��book���;�ƥ�� ��4��
                //      ֻ��reader����ƥ�䣬��3��
                //      ֻ��book����ƥ�䣬��2��
                //      reader��book���Ͷ���ƥ�䣬��1��
                nRet = app.GetLoanParam(
                    //null,
                    strLibraryCode,
                    strReaderType,
                    strBookType,
                    "�ɽ����",
                    out strParamValue,
                    out matchresult,
                    out strError);
                if (nRet == -1 || nRet < 4)
                {
                    // text-level: �û���ʾ
                    strError = "�ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ͼ������ '" + strBookType + "' ��δ���� �ɽ���� ����, ��˾ܾ�" + strOperName + "����";
                    return -1;
                }

                // �����Ǵ���񳬹���������
                int nThisTypeMax = 0;
                try
                {
                    nThisTypeMax = Convert.ToInt32(strParamValue);
                }
                catch
                {
                    strError = "�ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ͼ������ '" + strBookType + "' �� �ɽ���� ����ֵ '" + strParamValue + "' ��ʽ������, ��˾ܾ�" + strOperName + "����";
                    return -1;
                }

                if (nThisTypeCount + 1 > nThisTypeMax)
                {
                    strError = "���� '" + strReaderBarcode + "' ���� '" + strBookType + "' ��ͼ������������ �ݴ��� '" + strLibraryCode + "' �� �ö������� '" + strReaderType + "' �Ը�ͼ������ '" + strBookType + "' ����� �ɽ���� ֵ '" + strParamValue + "'����˱���" + strOperName + "�������ܾ�";
                    return 0;
                }

                // �õ��ö������������������ͼ����ܲ�����������
                // return:
                //      reader��book���;�ƥ�� ��4��
                //      ֻ��reader����ƥ�䣬��3��
                //      ֻ��book����ƥ�䣬��2��
                //      reader��book���Ͷ���ƥ�䣬��1��
                nRet = app.GetLoanParam(
                    //null,
                    strLibraryCode,
                    strReaderType,
                    "",
                    "�ɽ��ܲ���",
                    out strParamValue,
                    out matchresult,
                    out strError);
                if (nRet == -1)
                {
                    // text-level: �û���ʾ
                    strError = "�ڻ�ȡ�ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' �� �ɽ��ܲ��� ���������г���: "+strError+"����˾ܾ�" + strOperName + "����";
                    return -1;
                }
                if (nRet < 3)
                {
                    // text-level: �û���ʾ
                    strError = "�ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ��δ���� �ɽ��ܲ��� ����, ��˾ܾ�" + strOperName + "����";
                    return -1;
                }


                // Ȼ�󿴿��ܲ����Ƿ��Ѿ���������
                int nMax = 0;
                try
                {
                    nMax = Convert.ToInt32(strParamValue);
                }
                catch
                {
                    // text-level: �û���ʾ
                    strError = "�ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' �� �ɽ��ܲ��� ����ֵ '" + strParamValue + "' ��ʽ������, ��˾ܾ�" + strOperName + "����";
                    return -1;
                }

                // �Ӷ�����Ϣ�У��ҳ��ö����Ѿ����Ĺ��Ĳ���
                nodes = readerdom.DocumentElement.SelectNodes("borrows/borrow");

                int nCount = nodes.Count;

                if (nCount + 1 > nMax)
                {
                    // text-level: �û���ʾ
                    strError = "���� '" + strReaderBarcode + "' ������������� �ݴ��� '" + strLibraryCode + "' �� ���� '" + strReaderType + "' �ɽ��ܲ��� ֵ'" + strParamValue + "'����˱���" + strOperName + "�������ܾ�";
                    return 0;
                }
            }

            if (bRenew == false)
            {
                // �������ͼ����ܼ۸��Ƿ񳬹�Ѻ�����
                // return:
                //      -1  error
                //      0   û�г���
                //      1   ����
                nRet = CheckTotalPrice(readerdom,
                    itemdom,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (nRet == 1)
                    return 0;
            }

            return 1;
        }

        // ���<foregift borrowStyle="????"/>��ֵ(????����)
        public string GetForegiftBorrowStyle()
        {
            if (this.LibraryCfgDom == null)
                return "";

            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("foregift");
            if (node == null)
                return "";

            return DomUtil.GetAttr(node, "borrowStyle");
        }

        // �������ͼ����ܼ۸��Ƿ񳬹�Ѻ�����
        // return:
        //      -1  error
        //      0   û�г���
        //      1   ����
        int CheckTotalPrice(XmlDocument readerdom,
            XmlDocument itemdom,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strCfgStyle = GetForegiftBorrowStyle();
            if (StringUtil.IsInList("checkSum", strCfgStyle) == false)
                return 0;   // û�����ü���۸��ܶ��Ƿ񳬹�Ѻ�����Ĺ���

            // ��ö���Ѻ�����
            string strForegift = DomUtil.GetElementText(readerdom.DocumentElement,
                "foregift");
            if (String.IsNullOrEmpty(strForegift) == true)
            {
                strError = "����û��Ѻ�������ܽ��顣����Ѻ��󣬲��ܽ��顣";
                return -1;
            }

            List<string> foregift_results = null;
            nRet = PriceUtil.SumPrices(strForegift,
                out foregift_results,
                out strError);
            if (nRet == -1)
            {
                strError = "���ܶ���Ѻ������ַ��� '"+ strForegift + "' �Ĺ��̷�������: " + strError;
                return -1;
            }

            if (foregift_results.Count == 0)
            {
                strError = "����Ѻ������ַ��� '" + strForegift + "' �����ܺ���Ϊ�գ����ܽ��顣����Ѻ�����ܽ��顣";
                return -1;
            }

            if (foregift_results.Count > 1)
            {
                strError = "����Ѻ������ַ��� '" + strForegift + "' �����ܺ����ж��ֻ���(��"+foregift_results.Count.ToString()+"��)���޷������۸�Ƚϣ���˲��ܽ��顣";
                return -1;
            }

            // �����Ѿ��ڽ�Ĳ�ļ۸�����
            List<string> prices = new List<string>();
            XmlNodeList nodes = readerdom.DocumentElement.SelectNodes("borrows/borrow");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strPrice = DomUtil.GetAttr(node, "price");

                if (strPrice != null)
                    strPrice = strPrice.Trim();

                if (String.IsNullOrEmpty(strPrice) == true)
                    continue;

                prices.Add(strPrice);
            }

            // �ټ��ϼ���Ҫ���һ��
            string strThisPrice = DomUtil.GetElementText(itemdom.DocumentElement,
                "price");
            if (String.IsNullOrEmpty(strThisPrice) == false)
                prices.Add(strThisPrice);

            if (prices.Count == 0)
                return 0;   // ��û�м۸��ַ�����Ҳ���޷����м�����

            List<string> results = null;

            nRet = PriceUtil.TotalPrice(prices,
                out results,
                out strError);
            if (nRet == -1)
                return -1;

            if (results.Count == 0)
            {
                strError = "TotalPrice()������۸���ܺ��ȻΪ�ա�";
                return -1;
            }


            if (results.Count > 1)
            {
                strError = "�ö��߽��ĵ�ͼ���У���۸�ı���Ϊ " + results.Count.ToString() + " �����޷��򵥼�����ܼ�";
                return -1;
            }

            // �Ƚ������۸��ַ���
            // return:
            //      -3  ���ֲ�ͬ���޷�ֱ�ӱȽ� strError����˵��
            //      -2  error strError����˵��
            //      -1  strPrice1С��strPrice2
            //      0   ����
            //      1   strPrice1����strPrice2
            nRet = PriceUtil.Compare(foregift_results[0],
                results[0],
                out strError);
            if (nRet == -2)
            {
                strError = "�������۸��Ѻ�������бȽϵ�ʱ�������󣬽��Ĳ������ܾ������飺" + strError;
                return -1;
            }

            if (nRet == -3)
            {
                strError = "�����۸��Ѻ�����ı��ֲ�ͬ���޷����м۸�Ƚϣ���˽��Ĳ������ܾ������飺" + strError;
                return -1;
            }

            if (nRet == -1)
            {
                strError = "�������Ѿ����ĵ�ͼ��͵�ǰ���ͼ��Ĳ�۸�Ϊ " + results[0] + "����������Ѻ����� " + foregift_results[0] + "�����Ĳ������ܾ���";
                return 1;
            }

            return 0;
        }


        // Borrow()�¼�����
        // �����Ѿ�д����߼�¼�Ľ�����Ϣ
        // �����¼�Ѿ������ڣ��Ƿ���Ҫ�ö���֤������ٲ����λ�õĶ��߼�¼����
        // ��Ϊû�н�����Ϣ�Ķ��߼�¼��ȷʵ���ܱ�������û��ƶ�λ�á�
        // parameters:
        //      strReaderRecPath    ���߼�¼·��
        //      strReaderBarcode    ����֤����š�����Ҫ����¼����������������Ƿ��Ѿ��仯�ˣ���ʹ�������������������飬����null
        //      strItemBarcode  �Ѿ���Ĳ������
        // return:
        //      -1  error
        //      0   û�б�ҪUndo
        //      1   Undo�ɹ�
        int UndoBorrowReaderRecord(
            RmsChannel channel,
            string strReaderRecPath,
            string strReaderBarcode,
            string strItemBarcode,
            out string strError)
        {
            strError = "";
            long lRet = 0;
            int nRet = 0;

            string strMetaData = "";
            byte[] reader_timestamp = null;
            string strOutputPath = "";

            string strReaderXml = "";

            int nRedoCount = 0;

        REDO:

            lRet = channel.GetRes(strReaderRecPath,
    out strReaderXml,
    out strMetaData,
    out reader_timestamp,
    out strOutputPath,
    out strError);
            if (lRet == -1)
            {
                strError = "����ԭ��¼ '" + strReaderRecPath + "' ʱ����";
                return -1;
            }

            XmlDocument readerdom = null;
            nRet = LibraryApplication.LoadToDom(strReaderXml,
                out readerdom,
                out strError);
            if (nRet == -1)
            {
                strError = "װ�ؿ��ж��߼�¼ '" + strReaderRecPath + "' ����XML DOMʱ��������: " + strError;
                return -1;
            }

            // ������֤������ֶ� �Ƿ����仯
            if (String.IsNullOrEmpty(strReaderBarcode) == false)
            {
                string strReaderBarcodeContent = DomUtil.GetElementText(readerdom.DocumentElement,
                    "barcode");
                if (strReaderBarcode != strReaderBarcodeContent)
                {
                    strError = "���ִ����ݿ��ж����Ķ��߼�¼ '" + strReaderRecPath + "' ����<barcode>�ֶ����� '" + strReaderBarcodeContent + "' ��ҪUndo�Ķ��߼�¼֤����� '" + strReaderBarcode + "' �Ѳ�ͬ��";
                    return -1;
                }
            }

            // ȥ��dom�б�ʾ���ĵĽڵ�
            XmlNode node = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcode + "']");
            if (node == null)
                return 0;   // �Ѿ�û�б�ҪUndo��

            node.ParentNode.RemoveChild(node);

            byte[] output_timestamp = null;
            // string strOutputPath = "";

            // д�ض��߼�¼
            lRet = channel.DoSaveTextRes(strReaderRecPath,
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
                        strError = "д�ض��߼�¼��ʱ����ʱ�����ͻ�������Ѿ�����10�Σ��Է�������ֻ��ֹͣ����";
                        return -1;
                    }
                    goto REDO;
                }

                strError = "д�ض��߼�¼��ʱ��������" + strError;
                return -1;
            }

            return 1;   // Undo�Ѿ��ɹ�
        }

        // �������ظ�����ŵĲ��¼�У�ѡ�����з��ϵ�ǰ����֤����ŵ�
        // parameters:
        //      bOnlyGetFirstItemXml    ���Ϊtrue��������aItemXml��ֻװ��ƥ���ϵĵ�һ����¼��XML������Ϊ�˷�ֹ�ڴ������
        //                              ���Ϊfalse������ȫ��ƥ���¼������aItemXml
        // return:
        //      -1  ����
        //      ����    ѡ��������
        static int FindItem(
            RmsChannel channel,
            string strReaderBarcode,
            List<string> aPath,
            bool bOnlyGetFirstItemXml,
            out List<string> aFoundPath,
            out List<string> aItemXml,
            out List<byte[]> aTimestamp,
            out string strError)
        {
            aFoundPath = new List<string>();
            aTimestamp = new List<byte[]>();
            aItemXml = new List<string>();
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

                // ���<borrower>
                string strBorrower = DomUtil.GetElementText(dom.DocumentElement,
                    "borrower");
                if (String.IsNullOrEmpty(strBorrower) == true)
                    continue;

                if (
                    (String.IsNullOrEmpty(strReaderBarcode) == false
                    && strBorrower == strReaderBarcode)
                    // ����û���ṩ����֤������������Ǿ���ȡ�����˽�������в�
                    || (String.IsNullOrEmpty(strReaderBarcode) == true
                    && String.IsNullOrEmpty(strBorrower) == false)
                    )
                {
                    aFoundPath.Add(strPath);
                    if (bOnlyGetFirstItemXml == true && aItemXml.Count >= 1)
                    {
                        // ���Ż�����
                    }
                    else
                    {
                        aItemXml.Add(strXml);
                    }
                    aTimestamp.Add(timestamp);
                }
            }

            return (aFoundPath.Count);
        ERROR1:
            return -1;
        }


        #endregion

        // 2009/10/27 new add
        // ��ö�������
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetReaderName(
            SessionInfo sessioninfo,
            string strReaderBarcode,
            out string strReaderName,
            out string strError)
        {
            strError = "";
            strReaderName = "";
            int nRet = 0;

            // ������߼�¼
            string strReaderXml = "";
            byte[] reader_timestamp = null;
            string strOutputReaderRecPath = "";

            nRet = this.GetReaderRecXml(
                sessioninfo.Channels,
                strReaderBarcode,
                out strReaderXml,
                out strOutputReaderRecPath,
                out reader_timestamp,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;

            if (nRet > 1)
            {
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

            strReaderName = DomUtil.GetElementText(readerdom.DocumentElement, "name");

            return 1;
        }

        static string GetReturnActionName(string strAction)
        {
            if (strAction == "return")
            {
                return "����";
            }
            else if (strAction == "lost")
            {
                return "��ʧ����";
            }
            else return strAction;
        }

        // API: ����
        // Ȩ�ޣ�  ������Ա��ҪreturnȨ�ޣ�����Ƕ�ʧ������ҪlostȨ�ޣ����ж��߾����߱��������Ȩ�ޡ�
        // parameters:
        //      strAction   return/lost
        // return:
        //      Result.Value    -1  ���� 0 �����ɹ� 1 �����ɹ�������ֵ�ò�����Ա�������������г������������������ظ�����Ҫ����ԤԼ��
        public LibraryServerResult Return(
            SessionInfo sessioninfo,
            string strAction,
            string strReaderBarcodeParam,
            string strItemBarcodeParam,
            string strConfirmItemRecPath,
            bool bForce,
            string strStyle,

            string strItemFormatList,   // 2008/5/9 new add
            out string[] item_records,  // 2008/5/9 new add

            string strReaderFormatList,
            out string [] reader_records,

            string strBiblioFormatList, // 2008/5/9 new add
            out string[] biblio_records,    // 2008/5/9 new add

            out string[] aDupPath,
            out string strOutputReaderBarcodeParam,
            out ReturnInfo return_info)
        {
            item_records = null;
            reader_records = null;
            biblio_records = null;
            aDupPath = null;
            strOutputReaderBarcodeParam = "";
            return_info = new ReturnInfo();

            string strError = "";

            DateTime start_time = DateTime.Now;

            LibraryServerResult result = new LibraryServerResult();

            string strActionName = GetReturnActionName(strAction);

            // ������ի��
            string strPersonalLibrary = "";
            if (sessioninfo.UserType == "reader"
                && sessioninfo.Account != null)
                strPersonalLibrary = sessioninfo.Account.PersonalLibrary;

            // Ȩ���ж�
            if (strAction == "return")
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("return", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = strActionName + "�������ܾ������߱�returnȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    // return result;
                }
            }
            else if (strAction == "lost")
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("lost", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = strActionName + " �������ܾ������߱�lostȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    // return result;
                }
            }
            else
            {
                strError = "�޷�ʶ��� strAction ����ֵ '"+strAction+"'��";
                goto ERROR1;
            }

            // �Զ�����ݵĸ����ж�
            // ע�����и�����ի�ģ������Լ������ִ��
            if (sessioninfo.UserType == "reader"
                && string.IsNullOrEmpty(strPersonalLibrary) == true)
            {
                result.Value = -1;
                result.ErrorInfo = strActionName + "�������ܾ�����Ϊ���߲��ܽ��л��������";
                result.ErrorCode = ErrorCode.AccessDenied;
                return result;
            }

            // ���û����ͨ��Ȩ�ޣ���ҪԤ����ȡȨ��
            LibraryServerResult result_save = null;
            if (result.Value == -1 && String.IsNullOrEmpty(sessioninfo.Access) == false)
            {
                string strAccessActionList = GetDbOperRights(sessioninfo.Access,
                        "", // ��ʱ����֪��ʵ���������ȡ�õ�ǰ�ʻ���������һ��ʵ���Ĵ�ȡ����
                        "circulation");
                if (string.IsNullOrEmpty(strAccessActionList) == true)
                    return result;

                // ͨ��������һ�����󣬺�����ȻҪ����ȡȨ�ޡ�
                // ����������У���ȷ���ĳ��ʵ���Ĵ�ȡȨ�޴��ڣ�������ȡȨ�ޣ���������ڣ�������ͨȨ��
                result_save = result.Clone();
            }
            else if (result.Value == -1)
                return result;  // �ӳٱ��� 2014/9/16

            result = new LibraryServerResult();

            string strReservationReaderBarcode = "";

            string strReaderBarcode = strReaderBarcodeParam;

            long lRet = 0;
            int nRet = 0;
            string strIdcardNumber = "";
            string strQrCode = "";  //
            bool bDelayVerifyReaderBarcode = false; // �Ƿ��ӳ���֤
            string strLockReaderBarcode = "";

            if (bForce == true)
            {
                strError = "bForce��������Ϊtrue";
                goto ERROR1;
            }

            if (string.IsNullOrEmpty(strReaderBarcode) == false)
            {
                string strOutputCode = "";
                // �Ѷ�ά���ַ���ת��Ϊ����֤�����
                // parameters:
                //      strReaderBcode  [out]����֤�����
                // return:
                //      -1      ����
                //      0       ���������ַ������Ƕ���֤�Ŷ�ά��
                //      1       �ɹ�      
                nRet = this.DecodeQrCode(strReaderBarcode,
                    out strOutputCode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 1)
                {
                    strQrCode = strReaderBarcode;
                    strReaderBarcode = strOutputCode;
                }
            }

            int nRedoCount = 0;

        REDO_RETURN:

            bool bReaderLocked = false;
            bool bEntityLocked = false;

            if (String.IsNullOrEmpty(strReaderBarcodeParam) == false)
            {
                // �Ӷ��߼�¼��
                strLockReaderBarcode = strReaderBarcodeParam;
                this.ReaderLocks.LockForWrite(strReaderBarcodeParam);
                bReaderLocked = true;
                strOutputReaderBarcodeParam = strReaderBarcode;
            }

            string strOutputReaderXml = "";
            string strOutputItemXml = "";
            string strBiblioRecID = "";
            string strOutputItemRecPath = "";
            string strOutputReaderRecPath = "";
            string strLibraryCode = "";

            try // ���߼�¼������Χ(����)��ʼ
            {
                List<string> aPath = null;

                string strItemXml = "";
                byte[] item_timestamp = null;

                RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    goto ERROR1;
                }

                // *** ��ò��¼ ***
                bool bItemBarcodeDup = false;   // �Ƿ�����������ظ����
                string strDupBarcodeList = "";  // ������󷵻�ErrorInfo���ظ���������б�

                // ���¼���ܼ���
                // ������߼�¼��ʱ�Ѿ�����, ��Ϊ���¼����
                if (bReaderLocked == true)
                {
                    this.EntityLocks.LockForWrite(strItemBarcodeParam);
                    bEntityLocked = true;
                }

                try // ���¼������Χ��ʼ
                {
                    WriteTimeUsed(start_time, "Return()��ǰ�ڼ������� ��ʱ ");

                    DateTime start_time_read_item = DateTime.Now;

                    // ����Ѿ���ȷ���Ĳ��¼·��
                    if (String.IsNullOrEmpty(strConfirmItemRecPath) == false)
                    {
                        // ���·���еĿ������ǲ���ʵ�����
                        // return:
                        //      -1  error
                        //      0   ����ʵ�����
                        //      1   ��ʵ�����
                        nRet = this.CheckItemRecPath(strConfirmItemRecPath,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 0)
                        {
                            strError = strConfirmItemRecPath + strError;
                            goto ERROR1;
                        }

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
                    }
                    else
                    {

                        /*
                        // ��ò��¼
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   ����1��
                        //      >1  ���ж���1��
                        nRet = this.GetItemXml(
                            sessioninfo.Channels,
                            strItemBarcode,
                            out strItemXml,
                            out strOutputItemRecPath,
                            out strError);
                        if (nRet == 0)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "������� '" + strItemBarcode + "' ������";
                            result.ErrorCode = ErrorCode.ItemBarcodeNotFound;
                            return result;
                        }
                        if (nRet == -1)
                        {
                            strError = "������¼ʱ��������: " + strError;
                            goto ERROR1;
                        }
                         */

                        // �Ӳ�����Ż�ò��¼

                        // ��ò��¼
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   ����1��
                        //      >1  ���ж���1��
                        nRet = this.GetItemRecXml(
                            sessioninfo.Channels,
                            strItemBarcodeParam,
                            out strItemXml,
                            100,
                            out aPath,
                            out item_timestamp,
                            out strError);
                        if (nRet == 0)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "������� '" + strItemBarcodeParam + "' ������";
                            result.ErrorCode = ErrorCode.ItemBarcodeNotFound;
                            return result;
                        }
                        if (nRet == -1)
                        {
                            strError = "������¼ʱ��������: " + strError;
                            goto ERROR1;
                        }

                        if (aPath.Count > 1)
                        {
                            if (this.Statis != null)
                                this.Statis.IncreaseEntryValue(
                                strLibraryCode,
                                "����",
                                "��������������ظ�����",
                                1);

                            bItemBarcodeDup = true; // ��ʱ�Ѿ���Ҫ����״̬����Ȼ������Խ�һ��ʶ��������Ĳ��¼

                            // ����strDupBarcodeList
                            /*
                            string[] pathlist = new string[aPath.Count];
                            aPath.CopyTo(pathlist);
                            strDupBarcodeList = String.Join(",", pathlist);
                             * */
                            strDupBarcodeList = StringUtil.MakePathList(aPath);

                            List<string> aFoundPath = null;
                            List<byte[]> aTimestamp = null;
                            List<string> aItemXml = null;

                            if (String.IsNullOrEmpty(strReaderBarcodeParam) == true)
                            {
                                if (this.Statis != null)
                                    this.Statis.IncreaseEntryValue(
                                    strLibraryCode,
                                    "����",
                                    "��������������ظ����޶���֤����Ÿ����жϴ���",
                                    1);

                                // ���û�и�������֤����Ų���
                                result.Value = -1;
                                result.ErrorInfo = "�������Ϊ '" + strItemBarcodeParam + "' ���¼�� " + aPath.Count.ToString() + " �����޷����л�����������ڸ��Ӳ��¼·���������ύ���������";
                                result.ErrorCode = ErrorCode.ItemBarcodeDup;

                                aDupPath = new string[aPath.Count];
                                aPath.CopyTo(aDupPath);
                                return result;
                            }

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
                                result.Value = -1;
                                result.ErrorInfo = "������� '" + strItemBarcodeParam + "' �������� " + aPath.Count + " ����¼�У�û���κ�һ����<borrower>Ԫ�ر����˱����� '" + strReaderBarcode + "' ���ġ�";
                                result.ErrorCode = ErrorCode.ItemBarcodeNotFound;
                                return result;
                            }

                            if (nRet > 1)
                            {
                                if (this.Statis != null)
                                    this.Statis.IncreaseEntryValue(
                                    strLibraryCode,
                                    "����",
                                    "��������������ظ�������֤�����Ҳ�޷�ȥ�ش���",
                                    1);

                                result.Value = -1;
                                result.ErrorInfo = "�������Ϊ '" + strItemBarcodeParam + "' ����<borrower>Ԫ�ر���Ϊ���� '" + strReaderBarcode + "' ���ĵĲ��¼�� " + aFoundPath.Count.ToString() + " �����޷����л�����������ڸ��Ӳ��¼·���������ύ���������";
                                result.ErrorCode = ErrorCode.ItemBarcodeDup;
                                this.WriteErrorLog(result.ErrorInfo);   // 2012/12/30

                                aDupPath = new string[aFoundPath.Count];
                                aFoundPath.CopyTo(aDupPath);
                                return result;
                            }

                            Debug.Assert(nRet == 1, "");

                            if (this.Statis != null)
                                this.Statis.IncreaseEntryValue(strLibraryCode,
                                "����",
                                "��������������ظ������ݶ���֤����ųɹ�ȥ�ش���",
                                1);

                            this.WriteErrorLog("������������� '" + strItemBarcodeParam + "' �ظ������ݶ���֤����� '" + strReaderBarcode + "' �ɹ�ȥ��");   // 2012/12/30

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
                                // strItemXml�Ѿ��в��¼��
                            }
                        }

                        // �������غ�����
                        aDupPath = new string[1];
                        aDupPath[0] = strOutputItemRecPath;
                    }


                    // �������¼�����������ݿ⣬�Ƿ��ڲ�����ͨ��ʵ���֮��
                    // 2008/6/4 new add
                    bool bItemDbInCirculation = true;
                    string strItemDbName = "";
                    if (String.IsNullOrEmpty(strOutputItemRecPath) == false)
                    {
                        strItemDbName = ResPath.GetDbName(strOutputItemRecPath);
                        if (this.IsItemDbName(strItemDbName, out bItemDbInCirculation) == false)
                        {
                            strError = "���¼·�� '" + strOutputItemRecPath + "' �е����ݿ��� '" + strItemDbName + "' ��Ȼ���ڶ����ʵ���֮�С�";
                            goto ERROR1;
                        }
                    }

                    // ����ȡȨ��
                    string strAccessParameters = "";

                    {

                        // ����ȡȨ��
                        if (String.IsNullOrEmpty(sessioninfo.Access) == false)
                        {
                            string strAccessActionList = "";
                            strAccessActionList = GetDbOperRights(sessioninfo.Access,
                                strItemDbName,
                                "circulation");
#if NO
                            if (String.IsNullOrEmpty(strAccessActionList) == true && result_save != null)
                            {
                                // TODO: Ҳ����ֱ�ӷ��� result_save
                                strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strItemDbName + "' ִ�� ���� �����Ĵ�ȡȨ��";
                                result.Value = -1;
                                result.ErrorInfo = strError;
                                result.ErrorCode = ErrorCode.AccessDenied;
                                return result;
                            }
#endif
                            if (strAccessActionList == null)
                            {
                                strAccessActionList = GetDbOperRights(sessioninfo.Access,
            "", // ��ʱ����֪��ʵ���������ȡ�õ�ǰ�ʻ���������һ��ʵ���Ĵ�ȡ����
            "circulation");
                                if (strAccessActionList == null)
                                {
                                    // ������ʵ��ⶼû�ж����κδ�ȡȨ�ޣ���ʱ��Ҫ�˶�ʹ����ͨȨ��
                                    strAccessActionList = sessioninfo.Rights;

                                    // ע����ʵ��ʱ result_save == null ��������ͨȨ�޼���Ѿ�ͨ���˵�
                                }
                                else
                                {
                                    // ������ʵ��ⶨ���˴�ȡȨ�ޣ����� strItemDbName û�ж���
                                    strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strItemDbName + "' ִ�� ���� �����Ĵ�ȡȨ��";
                                    result.Value = -1;
                                    result.ErrorInfo = strError;
                                    result.ErrorCode = ErrorCode.AccessDenied;
                                    return result;
                                }
                            }


                            if (strAccessActionList == "*")
                            {
                                // ͨ��
                            }
                            else
                            {
                                if (IsInAccessList(strAction, strAccessActionList, out strAccessParameters) == false)
                                {
                                    strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strItemDbName + "' ִ�� ����  " + strActionName + " �����Ĵ�ȡȨ��";
                                    result.Value = -1;
                                    result.ErrorInfo = strError;
                                    result.ErrorCode = ErrorCode.AccessDenied;
                                    return result;
                                }
                            }
                        }
                    }


                    XmlDocument itemdom = null;
                    nRet = LibraryApplication.LoadToDom(strItemXml,
                        out itemdom,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "װ�ز��¼���� XML DOM ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    WriteTimeUsed(start_time_read_item, "Return()�ж�ȡ���¼ ��ʱ ");

                    DateTime start_time_lock = DateTime.Now;

                    // �������ģʽ����Ŀ��¼·��
                    if (this.TestMode == true || sessioninfo.TestMode == true)
                    {
                        string strBiblioDbName = "";
                        // ����ʵ�����, �ҵ���Ӧ����Ŀ����
                        // return:
                        //      -1  ����
                        //      0   û���ҵ�
                        //      1   �ҵ�
                        nRet = this.GetBiblioDbNameByItemDbName(strItemDbName,
                            out strBiblioDbName,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "����ʵ����� '" + strItemDbName + "' �����Ŀ����ʱ����: " + strError;
                            goto ERROR1;
                        }

                        string strParentID = DomUtil.GetElementText(itemdom.DocumentElement,
    "parent");
                        // �������ģʽ
                        // return:
                        //      -1  �����̳���
                        //      0   ����ͨ��
                        //      1   ������ͨ��
                        nRet = CheckTestModePath(strBiblioDbName + "/" + strParentID,
                            out strError);
                        if (nRet != 0)
                        {
                            strError = strActionName + "�������ܾ�: " + strError;
                            goto ERROR1;
                        }
                    }

                    string strOutputReaderBarcode = ""; // ���صĽ�����֤�����
                    // �ڲ��¼�л�ý�����֤�����
                    // return:
                    //      -1  ����
                    //      0   �ò�Ϊδ���״̬
                    //      1   �ɹ�
                    nRet = GetBorrowerBarcode(itemdom,
                        out strOutputReaderBarcode,
                        out strError);
                    if (nRet == -1 || nRet == 0)
                    {
                        strError = strError + " (���¼·��Ϊ '" + strOutputItemRecPath + "')";
                        goto ERROR1;
                    }

                    // ����ṩ�˶���֤����ţ�����Ҫ��ʵ
                    if (String.IsNullOrEmpty(strReaderBarcodeParam) == false)
                    {
                        if (strOutputReaderBarcode != strReaderBarcodeParam)
                        {
#if NO
                            if (StringUtil.IsIdcardNumber(strReaderBarcodeParam) == true)
                            {
                                // ��ʱ�������ͺ���֤
                                bDelayVerifyReaderBarcode = true;
                                strIdcardNumber = strReaderBarcodeParam;
                            }
                            else
                            {
                                strError = "���¼�������� " + strItemBarcode + " ʵ�ʱ����� " + strOutputReaderBarcode + " �����ģ�����������ǰ����Ķ���(֤�����) " + strReaderBarcodeParam + "�����������������";
                                goto ERROR1;
                            }
#endif
                            // ��ʱ�������ͺ���֤
                            bDelayVerifyReaderBarcode = true;
                            strIdcardNumber = strReaderBarcodeParam;
                        }
                    }

                    if (String.IsNullOrEmpty(strReaderBarcode) == true)
                        strReaderBarcode = strOutputReaderBarcode;

                    // *** ������߼�¼��ǰ��û������, ����������
                    if (bReaderLocked == false)
                    {
                        // �Ӷ��߼�¼��
                        strLockReaderBarcode = strReaderBarcode;
                        this.ReaderLocks.LockForWrite(strLockReaderBarcode);
                        bReaderLocked = true;
                        strOutputReaderBarcodeParam = strReaderBarcode;
                    }

                    // *** ������¼��ǰ��û��������������������
                    if (bEntityLocked == false)
                    {
                        this.EntityLocks.LockForWrite(strItemBarcodeParam);
                        bEntityLocked = true;

                        // ��Ϊǰ����ڲ��¼һֱû�м�������������������Ҫ
                        // ���ʱ�����ȷ����¼����û�У�ʵ���ԣ��ı�
                        byte[] temp_timestamp = null;
                        string strTempOutputPath = "";
                        string strTempItemXml = "";
                        string strMetaData = "";

                        lRet = channel.GetRes(
                            strOutputItemRecPath,
                            out strTempItemXml,
                            out strMetaData,
                            out temp_timestamp,
                            out strTempOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "�������(�ͺ�)������������ȡ���¼ '" + strOutputItemRecPath + "' ʱ��������: " + strError;
                            goto ERROR1;
                        }

                        // ���ʱ����������ı�
                        if (ByteArray.Compare(item_timestamp, temp_timestamp) != 0)
                        {
                            // װ���¼�¼����DOM
                            XmlDocument temp_itemdom = null;
                            nRet = LibraryApplication.LoadToDom(strTempItemXml,
                                out temp_itemdom,
                                out strError);
                            if (nRet == -1)
                            {
                                strError = "װ�ز��¼strTempItemXml ·��'" + strOutputItemRecPath + "' ����XML DOMʱ��������: " + strError;
                                goto ERROR1;
                            }

                            // ����¾ɲ��¼����Ҫ���Ըı䣿
                            if (IsItemRecordSignificantChanged(itemdom,
                                temp_itemdom) == true)
                            {

                                // ��ֻ������
                                nRedoCount++;
                                if (nRedoCount > 10)
                                {
                                    strError = "�������(�ͺ�)������������ȡ���¼��ʱ��,����ʱ�����ͻ,���������10��,��ʧ��...";
                                    goto ERROR1;
                                }
                                /*
                                // �����������5�Σ��������޸Ķ���֤����Ų������������У���������ȡ�ģ�ȷ����ֵ�������Ͳ����ͺ������
                                if (nRedoCount > 5)
                                    strReaderBarcodeParam = strReaderBarcode;
                                 * */
                                goto REDO_RETURN;
                            }

                            // ���û��Ҫ���Ըı䣬��ˢ����ز�����Ȼ�����������
                            itemdom = temp_itemdom;
                            item_timestamp = temp_timestamp;
                            strItemXml = strTempItemXml;

                        }

                        // ���ʱ���û�з������ı䣬�򲻱�ˢ���κβ���
                    }

                    WriteTimeUsed(start_time_lock, "Return()�в������� ��ʱ ");

                    // ������߼�¼
                    DateTime start_time_read_reader = DateTime.Now;

                    string strReaderXml = "";
                    byte[] reader_timestamp = null;

                    nRet = this.GetReaderRecXml(
                        sessioninfo.Channels,
                        strReaderBarcode,
                        out strReaderXml,
                        out strOutputReaderRecPath,
                        out reader_timestamp,
                        out strError);
                    if (nRet == 0)
                    {
                        // ��������֤�ţ�����̽���������֤�š�;��
                        if (StringUtil.IsIdcardNumber(strReaderBarcode) == true)
                        {
                            strIdcardNumber = strReaderBarcode;
                            strReaderBarcode = "";

                            // ͨ���ض�����;����ö��߼�¼
                            // return:
                            //      -1  error
                            //      0   not found
                            //      1   ����1��
                            //      >1  ���ж���1��
                            nRet = this.GetReaderRecXmlByFrom(
                                sessioninfo.Channels,
                                strIdcardNumber,
                                "���֤��",
                                out strReaderXml,
                                out strOutputReaderRecPath,
                                out reader_timestamp,
                                out strError);
                            if (nRet == -1)
                            {
                                // text-level: �ڲ�����
                                strError = "�����֤�� '" + strIdcardNumber + "' ������߼�¼ʱ��������: " + strError;
                                goto ERROR1;
                            }
                            if (nRet == 0)
                            {
                                result.Value = -1;
                                // text-level: �û���ʾ
                                result.ErrorInfo = string.Format(this.GetString("���֤��s������"),   // "���֤�� '{0}' ������"
                                    strIdcardNumber);
                                result.ErrorCode = ErrorCode.IdcardNumberNotFound;
                                return result;
                            }
                            if (nRet > 1)
                            {
                                // text-level: �û���ʾ
                                result.Value = -1;
                                result.ErrorInfo = "�����֤�� '" + strIdcardNumber + "' �������߼�¼���� " + nRet.ToString() + " ��������޷������֤�������н軹�����������֤����������н軹������";
                                result.ErrorCode = ErrorCode.IdcardNumberDup;
                                return result;
                            }
                            Debug.Assert(nRet == 1, "");
                            goto SKIP0;
                        }
                        else
                        {
                            // 2013/5/24
                            // �����Ҫ���Ӷ���֤�ŵȸ���;�����м���
                            foreach (string strFrom in this.PatronAdditionalFroms)
                            {
                                nRet = this.GetReaderRecXmlByFrom(
                                    sessioninfo.Channels,
                                    null,
                                    strReaderBarcode,
                                    strFrom,
                                out strReaderXml,
                                out strOutputReaderRecPath,
                                out reader_timestamp,
                                    out strError);
                                if (nRet == -1)
                                {
                                    // text-level: �ڲ�����
                                    strError = "��" + strFrom + " '" + strReaderBarcode + "' ������߼�¼ʱ��������: " + strError;
                                    goto ERROR1;
                                }
                                if (nRet == 0)
                                    continue;
                                if (nRet > 1)
                                {
                                    // text-level: �û���ʾ
                                    result.Value = -1;
                                    result.ErrorInfo = "��" + strFrom + " '" + strReaderBarcode + "' �������߼�¼���� " + nRet.ToString() + " ��������޷���" + strFrom + "�����н軹�����������֤����������н軹������";
                                    result.ErrorCode = ErrorCode.IdcardNumberDup;
                                    return result;
                                }

                                Debug.Assert(nRet == 1, "");

                                strIdcardNumber = "";
                                strReaderBarcode = "";

                                goto SKIP0;
                            }

                        }

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

                    // 2008/6/17 new add
                    if (nRet > 1)
                    {
                        strError = "������߼�¼ʱ�����ֶ���֤����� '" + strReaderBarcode + "' ���� " + nRet.ToString() + " ��������һ�����ش�����ϵͳ����Ա���촦��";
                        goto ERROR1;
                    }

                    SKIP0:

                    // �������߼�¼�����������ݿ⣬�Ƿ��ڲ�����ͨ�Ķ��߿�֮��
                    // 2008/6/4 new add
                    bool bReaderDbInCirculation = true;
                    string strReaderDbName = "";
                    if (String.IsNullOrEmpty(strOutputReaderRecPath) == false)
                    {
                        if (this.TestMode == true || sessioninfo.TestMode == true)
                        {
                            // �������ģʽ
                            // return:
                            //      -1  �����̳���
                            //      0   ����ͨ��
                            //      1   ������ͨ��
                            nRet = CheckTestModePath(strOutputReaderRecPath,
                                out strError);
                            if (nRet != 0)
                            {
                                strError = strActionName + "�������ܾ�: " + strError;
                                goto ERROR1;
                            }
                        }

                        strReaderDbName = ResPath.GetDbName(strOutputReaderRecPath);
                        if (this.IsReaderDbName(strReaderDbName, 
                            out bReaderDbInCirculation,
                            out strLibraryCode) == false)
                        {
                            strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �е����ݿ��� '" + strReaderDbName + "' ��Ȼ���ڶ���Ķ��߿�֮�С�";
                            goto ERROR1;
                        }
                    }

                    // ���㲻�ǲ�����ͨ�����ݿ⣬Ҳ�û���?

                    // ��鵱ǰ�������Ƿ��Ͻ������߿�
                    // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                    if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
            sessioninfo.LibraryCodeList) == false)
                    {
                        strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
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
                    WriteTimeUsed(start_time_read_reader, "Return()�ж�ȡ���߼�¼ ��ʱ ");

                    // string strReaderDbName = ResPath.GetDbName(strOutputReaderRecPath);

                    // �۲���߼�¼�Ƿ��ڲ�����Χ��
                    // return:
                    //      -1  ����
                    //      0   �����������
                    //      1   Ȩ�����ƣ�������������ʡ�strError ����˵��ԭ�������
                    nRet = CheckReaderRange(sessioninfo,
                        readerdom,
                        strReaderDbName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == 1)
                    {
                        // strError = "��ǰ�û� '" + sessioninfo.UserID + "' �Ĵ�ȡȨ�޽�ֹ��������(֤�����Ϊ " + strReaderBarcode + ")������ԭ��" + strError;
                        result.Value = -1;
                        result.ErrorInfo = strError;
                        result.ErrorCode = ErrorCode.AccessDenied;
                        return result;
                    }

                    DateTime start_time_process = DateTime.Now;

                    string strReaderName = DomUtil.GetElementText(readerdom.DocumentElement, "name");

                    if (bDelayVerifyReaderBarcode == true)
                    {
                        // ˳����֤һ�����֤��
                        if (string.IsNullOrEmpty(strIdcardNumber) == false)
                        {
                            Debug.Assert(string.IsNullOrEmpty(strIdcardNumber) == false, "");

                            string strTempIdcardNumber = DomUtil.GetElementText(readerdom.DocumentElement, "idCardNumber");
                            if (strIdcardNumber != strTempIdcardNumber)
                            {
                                strError = "���¼�������� " + strItemBarcodeParam + " ʵ�ʱ�����(֤�����) " + strOutputReaderBarcode + " �����ģ��˶��ߵ����֤��Ϊ " + strTempIdcardNumber + "����������ǰ�����(��֤��)���֤�� " + strIdcardNumber + "�����������������";
                                goto ERROR1;
                            }
                        }
                        // ���»�ȡ����֤�����
                        strReaderBarcode = DomUtil.GetElementText(readerdom.DocumentElement, "barcode");
                        strOutputReaderBarcodeParam = strReaderBarcode; // Ϊ�˷���ֵ

                        {
                            if (strOutputReaderBarcode != strReaderBarcode)
                            {
                                strError = "���¼�������� " + strItemBarcodeParam + " ʵ�ʱ����� " + strOutputReaderBarcode + " �����ģ�����������ǰ�ƶ��Ķ���(֤�����) " + strReaderBarcodeParam + "�����������������";
                                goto ERROR1;
                            }
                        }
                    }

                    XmlDocument domOperLog = new XmlDocument();
                    domOperLog.LoadXml("<root />");
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "libraryCode",
                        strLibraryCode);    // �������ڵĹݴ���
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operation", "return");
                    DomUtil.SetElementText(domOperLog.DocumentElement, "action", strAction);

                    // �Ӷ�����Ϣ��, �ҵ���������
                    string strReaderType = DomUtil.GetElementText(readerdom.DocumentElement,
                        "readerType");

                    // ֤״̬ 2009/1/29 new add
                    string strReaderState = DomUtil.GetElementText(readerdom.DocumentElement,
                        "state");

                    // ����������
                    Calendar calendar = null;
                    nRet = GetReaderCalendar(strReaderType,
                        strLibraryCode,
                        out calendar,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    string strOperTime = this.Clock.GetClock();
                    string strWarning = "";

                    // ������¼
                    string strOverdueString = "";
                    string strLostComment = "";
                    nRet = DoReturnItemXml(
                        strAction,
                        sessioninfo,    // sessioninfo.Account,
                        calendar,
                        strReaderType,
                        strLibraryCode,
                        strAccessParameters,
                        readerdom,  // Ϊ�˵���GetLost()�ű�����
                        ref itemdom,
                        bForce,
                        bItemBarcodeDup,  // ����������Զ�λ���򲻼���ʵ���¼·��
                        strOutputItemRecPath,
                        sessioninfo.UserID, // ���������
                        strOperTime,
                        out strOverdueString,
                        out strLostComment,
                        out return_info,
                        out strWarning,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (string.IsNullOrEmpty(strWarning) == false)
                    {
                        if (String.IsNullOrEmpty(result.ErrorInfo) == false)
                            result.ErrorInfo += "\r\n";
                        result.ErrorInfo += strWarning;
                        result.Value = 1;
                    }

                    string strItemBarcode = DomUtil.GetElementText(itemdom.DocumentElement, "barcode");

                    // ������־��¼
                    DomUtil.SetElementText(domOperLog.DocumentElement, "itemBarcode",
                        string.IsNullOrEmpty(strItemBarcode) == false ? strItemBarcode : strItemBarcodeParam);
                    /* �����д��<overdues>
                    if (nRet == 1)
                    {
                        // ����г��ںͻ�ʧ������Ϣ
                        DomUtil.SetElementText(domOperLog.DocumentElement, "overdueString",
                        strOverdueString);
                    }
                     * */

                    bool bOverdue = false;
                    string strOverdueInfo = "";

                    if (nRet == 1)
                    {
                        bOverdue = true;
                        strOverdueInfo = strError;
                    }


                    // ������߼�¼
                    // string strNewReaderXml = "";
                    string strDeletedBorrowFrag = "";
                    nRet = DoReturnReaderXml(
                        strLibraryCode,
                        ref readerdom,
                        strItemBarcodeParam,
                        strItemBarcode,
                        strOverdueString,
                        sessioninfo.UserID, // ���������
                        strOperTime,
                        sessioninfo.ClientAddress,  // ǰ�˴���
                        out strDeletedBorrowFrag,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // ������־��¼
                    Debug.Assert(string.IsNullOrEmpty(strReaderBarcode) == false, "");
                    DomUtil.SetElementText(domOperLog.DocumentElement, "readerBarcode",
                        strReaderBarcode);
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                        sessioninfo.UserID);
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                        strOperTime);

                    WriteTimeUsed(start_time_process, "Return()�н��и������ݴ��� ��ʱ ");


                    // ԭ���������xml��html��ʽ�Ĵ����ڴ�

                    DateTime start_time_reservation_check = DateTime.Now;

                    // �쿴����ԤԼ���, �����г�������
                    // ���Ϊ��ʧ������Ҫ֪ͨ�ȴ��ߣ����Ѿ���ʧ�ˣ������ٵȴ�
                    // return:
                    //      -1  error
                    //      0   û���޸�
                    //      1   ���й��޸�
                    nRet = DoItemReturnReservationCheck(
                        (strAction == "lost") ? true : false,
                        ref itemdom,
                        out strReservationReaderBarcode,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 1 && return_info != null)
                    {
                        // <location>Ԫ���п��������� #reservation ����
                        return_info.Location = DomUtil.GetElementText(itemdom.DocumentElement,
                            "location");
                    }


                    WriteTimeUsed(start_time_reservation_check, "Return()�н���ԤԼ��� ��ʱ ");

                    /*
                    bool bFoundReservation = false; // �Ƿ񱾴λ����Ϊ��ԤԼ֮��

                    if (nRet == 1)
                        bFoundReservation = true;
                     * */


                    // д�ض��ߡ����¼
                    // byte[] timestamp = null;
                    byte[] output_timestamp = null;
                    string strOutputPath = "";

                    /*
                    Channel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "get channel error";
                        goto ERROR1;
                    }
                     * */
                    DateTime start_time_write_reader = DateTime.Now;

                    lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
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
                            goto REDO_RETURN;
                        }

                        goto ERROR1;
                    }

                    reader_timestamp = output_timestamp;

                    WriteTimeUsed(start_time_write_reader, "Return()��д�ض��߼�¼ ��ʱ ");

                    DateTime start_time_write_item = DateTime.Now;

                    lRet = channel.DoSaveTextRes(strOutputItemRecPath,
                        itemdom.OuterXml,
                        false,
                        "content,ignorechecktimestamp",
                        item_timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        // ҪUndo�ղŶԶ��߼�¼��д��
                        string strError1 = "";
                        lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                            strReaderXml,
                            false,
                            "content,ignorechecktimestamp",
                            reader_timestamp,
                            out output_timestamp,
                            out strOutputPath,
                            out strError1);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {
                                // ���߼�¼Undo��ʱ��, ����ʱ�����ͻ��
                                // ��ʱ��Ҫ�����ִ��¼, ��ͼ���ӻظ�ɾ����<borrows><borrow>Ԫ��
                                // return:
                                //      -1  error
                                //      0   û�б�ҪUndo
                                //      1   Undo�ɹ�
                                nRet = UndoReturnReaderRecord(
                                    channel,
                                    strOutputReaderRecPath,
                                    strReaderBarcode,
                                    strItemBarcodeParam,
                                    strDeletedBorrowFrag,
                                    strOverdueString,
                                    out strError);
                                if (nRet == -1)
                                {
                                    strError = "Undo���߼�¼ '" + strOutputReaderRecPath + "' (����֤�����Ϊ '" + strReaderBarcode + "' ��������Ϊ '"+strReaderName+"') ���������� '" + strItemBarcodeParam + "' ���޸�ʱ�����������޷�Undo: " + strError;
                                    this.WriteErrorLog(strError);
                                    goto ERROR1;
                                }

                                // �ɹ�
                                goto REDO_RETURN;
                            }


                            // ����Ϊ ����ʱ�����ͻ��������������
                            strError = "Undo���߼�¼ '" + strOutputReaderRecPath + "' (����֤�����Ϊ '" + strReaderBarcode + "' ��������Ϊ '"+strReaderName+"') ���������� '" + strItemBarcodeParam + "' ���޸�ʱ�����������޷�Undo: " + strError;
                            // strError = strError + ", ����Undoд�ؾɶ��߼�¼Ҳʧ��: " + strError1;
                            this.WriteErrorLog(strError);
                            goto ERROR1;
                        }

                        // ����ΪUndo�ɹ�������
                        goto REDO_RETURN;
                    }

                    WriteTimeUsed(start_time_write_item, "Return()��д�ز��¼ ��ʱ ");

                    DateTime start_time_write_operlog = DateTime.Now;

                    // д����־

                    // overdue��Ϣ
                    if (String.IsNullOrEmpty(strOverdueString) == false)
                    {
                        DomUtil.SetElementText(domOperLog.DocumentElement,
                            "overdues", strOverdueString);
                    }

                    // ȷ�ϲ�·��
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "confirmItemRecPath", strConfirmItemRecPath);

                    if (string.IsNullOrEmpty(strIdcardNumber) == false)
                    {
                        // ������ʹ�����֤������ɻ��������
                        DomUtil.SetElementText(domOperLog.DocumentElement,
        "idcardNumber", strIdcardNumber);
                    }

                    // д����߼�¼
                    XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                        "readerRecord", readerdom.OuterXml);
                    DomUtil.SetAttr(node, "recPath", strOutputReaderRecPath);

                    // д����¼
                    node = DomUtil.SetElementText(domOperLog.DocumentElement,
                        "itemRecord", itemdom.OuterXml);
                    DomUtil.SetAttr(node, "recPath", strOutputItemRecPath);

                    if (strLostComment != "")
                    {
                        DomUtil.SetElementText(domOperLog.DocumentElement,
                            "lostComment",
                            strLostComment);
                    }

                    nRet = this.OperLog.WriteOperLog(domOperLog,
                        sessioninfo.ClientAddress,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "Return() API д����־ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    WriteTimeUsed(start_time_write_operlog, "Return()��д������־ ��ʱ ");

                    DateTime start_time_write_statis = DateTime.Now;


                    // д��ͳ��ָ��
#if NO
                    if (this.m_strLastReaderBarcode != strReaderBarcode)
                    {
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(strLibraryCode,
                            "����",
                            "������",
                            1);
                        this.m_strLastReaderBarcode = strReaderBarcode;
                    }
#endif
                    if (this.Garden != null)
                        this.Garden.Activate(strReaderBarcode,
                            strLibraryCode);

                    if (this.Statis != null)
                        this.Statis.IncreaseEntryValue(strLibraryCode,
                        "����",
                        "����",
                        1);

                    if (strAction == "lost")
                    {
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(strLibraryCode,
                            "����",
                            "������ʧ",
                            1);
                    }
                    WriteTimeUsed(start_time_write_statis, "Return()��дͳ��ָ�� ��ʱ ");

                    result.ErrorInfo = strActionName + "�����ɹ���" + result.ErrorInfo;  // 2013/11/13

                    if (bReaderDbInCirculation == false)
                    {
                        if (String.IsNullOrEmpty(result.ErrorInfo) == false)
                            result.ErrorInfo += "\r\n";
                        result.ErrorInfo += "����֤����� '" + strReaderBarcode + "' ���ڵĶ��߼�¼ '" + strOutputReaderRecPath + "' �����ݿ� '" + strReaderDbName + "' ����δ������ͨ�Ķ��߿⡣";
                        result.Value = 1;
                    }

                    if (bItemDbInCirculation == false)
                    {
                        if (String.IsNullOrEmpty(result.ErrorInfo) == false)
                            result.ErrorInfo += "\r\n";
                        result.ErrorInfo += "������� '" + strItemBarcodeParam + "' ���ڵĲ��¼ '" + strOutputItemRecPath + "' �����ݿ� '" + strReaderDbName + "' ����δ������ͨ��ʵ��⡣";
                        result.Value = 1;
                    }

                    if (bOverdue == true)
                    {
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(strLibraryCode,
                            "����",
                            "�����ڲ�",
                            1);

                        if (String.IsNullOrEmpty(result.ErrorInfo) == false)
                            result.ErrorInfo += "\r\n";

                        result.ErrorInfo += strOverdueInfo;
                        result.ErrorCode = ErrorCode.Overdue;
                        result.Value = 1;
                    }

                    if (bItemBarcodeDup == true)
                    {
                        if (String.IsNullOrEmpty(result.ErrorInfo) == false)
                            result.ErrorInfo += "\r\n";
                        result.ErrorInfo += "***����***: "+strActionName+"���������з������в��¼���ǵĲ�����ŷ������ظ�: " + strDupBarcodeList + "����֪ͨϵͳ����Ա���������ݴ���";
                        result.Value = 1;
                    }

                    if (String.IsNullOrEmpty(strReservationReaderBarcode) == false // 2009/10/19 changed  //bFoundReservation == true
                        && strAction != "lost")
                    {
                        // Ϊ����ʾ��Ϣ�г��ֶ����������������Ի�ȡ��������
                        string strReservationReaderName = "";

                        if (strReaderBarcode == strReservationReaderBarcode)
                            strReservationReaderName = strReaderName;
                        else
                        {
                            DateTime start_time_getname = DateTime.Now;

                            // ��ö�������
                            // return:
                            //      -1  error
                            //      0   not found
                            //      1   found
                            nRet = GetReaderName(
                                sessioninfo,
                                strReservationReaderBarcode,
                                out strReservationReaderName,
                                out strError);

                            WriteTimeUsed(start_time_getname, "Return()�л��ԤԼ�ߵ����� ��ʱ ");
                        }

                        if (String.IsNullOrEmpty(result.ErrorInfo) == false)
                            result.ErrorInfo += "\r\n";
                        result.ErrorInfo += "�򱾲�ͼ���ѱ����� " + strReservationReaderBarcode + " "
                            + strReservationReaderName + " ԤԼ�������ԤԼ�����ܡ�";    // 2009/10/10 changed
                        result.Value = 1;
                    }

                    // ����֤״̬��Ϊ������µ���ʾ
                    // 2008/1/29 new add
                    if (String.IsNullOrEmpty(strReaderState) == false)
                    {
                        if (String.IsNullOrEmpty(result.ErrorInfo) == false)
                            result.ErrorInfo += "\r\n";
                        result.ErrorInfo += "***����***: ��ǰ����֤״̬Ϊ: " + strReaderState + "����ע����к�������";
                        result.Value = 1;
                    }

                    strOutputItemXml = itemdom.OuterXml;
                    strOutputReaderXml = readerdom.OuterXml;
                    strBiblioRecID = DomUtil.GetElementText(itemdom.DocumentElement, "parent"); //

                } // ���¼������Χ����
                finally
                {
                    // ���¼����
                    if (bEntityLocked == true)
                        this.EntityLocks.UnlockForWrite(strItemBarcodeParam);
                }

            } // ���߼�¼������Χ����
            finally
            {
                if (bReaderLocked == true)
                    this.ReaderLocks.UnlockForWrite(strLockReaderBarcode);
            }

            // TODO: �������ԸĽ�Ϊ����ʧʱ��������ԤԼ��Ҳ֪ͨ������֪ͨ��������Ҫ���߲��ٵȴ��ˡ�
            if (String.IsNullOrEmpty(strReservationReaderBarcode) == false
                && strAction != "lost")
            {
                List<string> DeletedNotifyRecPaths = null;  // ��ɾ����֪ͨ��¼�����á�
                // ֪ͨԤԼ����Ĳ���
                // ���ڶԶ��߿��������ı�������, �������˴˺���
                // return:
                //      -1  error
                //      0   û���ҵ�<request>Ԫ��
                nRet = DoReservationNotify(
                    sessioninfo.Channels,
                    strReservationReaderBarcode,
                    true,   // ��Ҫ�����ڼ���
                    strItemBarcodeParam,
                    false,  // ���ڴ��
                    false,  // ����Ҫ���޸ĵ�ǰ���¼����Ϊǰ���Ѿ��޸Ĺ���
                    out DeletedNotifyRecPaths,
                    out strError);
                if (nRet == -1)
                {
                    strError = "��������Ѿ��ɹ�, ����ԤԼ����֪ͨ����ʧ��, ԭ��: " + strError;
                    goto ERROR1;
                }
                /*
                            if (this.Statis != null)
                this.Statis.IncreaseEntryValue(strLibraryCode,
                    "����",
                    "ԤԼ�����",
                    1);
                 * */


                /* ǰ���Ѿ�֪ͨ����
                result.Value = 1;
                result.ErrorCode = ErrorCode.ReturnReservation;
                if (result.ErrorInfo != "")
                    result.ErrorInfo += "\r\n";

                result.ErrorInfo += "��������ɹ�����˲�ͼ�鱻���� " + strReservationReaderBarcode + " ԤԼ�������ԤԼ�����ܡ�";
                 * */

                // ��ó��ںͱ�������״̬����Բ���?
            }

            // �������
            // ��������ݲ��ַ��ڶ��������ⷶΧ����Ϊ�˾�������������ʱ�䣬��߲�������Ч��
            if (String.IsNullOrEmpty(strOutputReaderXml) == false
                && StringUtil.IsInList("reader", strStyle) == true)
            {
                    string[] reader_formats = strReaderFormatList.Split(new char[] {','});
                    reader_records = new string[reader_formats.Length];

                    for (int i = 0; i < reader_formats.Length; i++)
                    {
                        string strReaderFormat = reader_formats[i];

                        // �����߼�¼���ݴ�XML��ʽת��ΪHTML��ʽ
                        // if (String.Compare(strReaderFormat, "html", true) == 0)
                        if (IsResultType(strReaderFormat, "html") == true)
                        {
                            string strReaderRecord = "";
                            nRet = this.ConvertReaderXmlToHtml(
                                sessioninfo,
                                this.CfgDir + "\\readerxml2html.cs",
                                this.CfgDir + "\\readerxml2html.cs.ref",
                                strLibraryCode,
                                strOutputReaderXml,
                                strOutputReaderRecPath, // 2009/10/18 new add
                                OperType.Return,
                                null,
                                strItemBarcodeParam,
                                strReaderFormat,
                                out strReaderRecord,
                                out strError);
                            if (nRet == -1)
                            {
                                strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                                goto ERROR1;
                            }
                            reader_records[i] = strReaderRecord;
                        }
                        // �����߼�¼���ݴ�XML��ʽת��Ϊtext��ʽ
                        // else if (String.Compare(strReaderFormat, "text", true) == 0)
                        else if (IsResultType(strReaderFormat, "text") == true)
                        {
                            string strReaderRecord = "";
                            nRet = this.ConvertReaderXmlToHtml(
                                sessioninfo,
                                this.CfgDir + "\\readerxml2text.cs",
                                this.CfgDir + "\\readerxml2text.cs.ref",
                                strLibraryCode,
                                strOutputReaderXml,
                                strOutputReaderRecPath, // 2009/10/18 new add
                                OperType.Return,
                                null,
                                strItemBarcodeParam,
                                strReaderFormat,
                                out strReaderRecord,
                                out strError);
                            if (nRet == -1)
                            {
                                strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                                goto ERROR1;
                            }
                            reader_records[i] = strReaderRecord;
                        }
                        // else if (String.Compare(strReaderFormat, "xml", true) == 0)
                        else if (IsResultType(strReaderFormat, "xml") == true)
                        {
                            // reader_records[i] = strOutputReaderXml;
                            string strResultXml = "";
                            nRet = GetItemXml(strOutputReaderXml,
                strReaderFormat,
                out strResultXml,
                out strError);
                            if (nRet == -1)
                            {
                                strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                                goto ERROR1;
                            }
                            reader_records[i] = strResultXml;
                        }
                        else if (IsResultType(strReaderFormat, "summary") == true)
                        {
                            // 2013/12/15
                            XmlDocument dom = new XmlDocument();
                            try
                            {
                                dom.LoadXml(strOutputReaderXml);
                            }
                            catch (Exception ex)
                            {
                                strError = "���� XML װ�� DOM ����: " + ex.Message;
                                strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                                goto ERROR1;
                            }
                            reader_records[i] = DomUtil.GetElementText(dom.DocumentElement, "name");
                        }
                        else
                        {
                            strError = "strReaderFormatList���������˲�֧�ֵ����ݸ�ʽ���� '" + strReaderFormat + "'";
                            strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                            goto ERROR1;
                        }
                    } // end of for
                
            } // end if

            // 2008/5/9 new add
            if (String.IsNullOrEmpty(strOutputItemXml) == false
                && StringUtil.IsInList("item", strStyle) == true)
            {
                string[] item_formats = strItemFormatList.Split(new char[] { ',' });
                item_records = new string[item_formats.Length];

                for (int i = 0; i < item_formats.Length; i++)
                {
                    string strItemFormat = item_formats[i];

                    // �����¼���ݴ�XML��ʽת��ΪHTML��ʽ
                    // if (String.Compare(strItemFormat, "html", true) == 0)
                    if (IsResultType(strItemFormat, "html") == true)
                    {
                        string strItemRecord = "";
                        nRet = this.ConvertItemXmlToHtml(
                            this.CfgDir + "\\itemxml2html.cs",
                            this.CfgDir + "\\itemxml2html.cs.ref",
                            strOutputItemXml,
                            strOutputItemRecPath,   // 2009/10/18 new add
                            out strItemRecord,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                            goto ERROR1;
                        }
                        item_records[i] = strItemRecord;
                    }
                    // �����¼���ݴ�XML��ʽת��Ϊtext��ʽ
                    // else if (String.Compare(strItemFormat, "text", true) == 0)
                    else if (IsResultType(strItemFormat, "text") == true)
                    {
                        string strItemRecord = "";
                        nRet = this.ConvertItemXmlToHtml(
                            this.CfgDir + "\\itemxml2text.cs",
                            this.CfgDir + "\\itemxml2text.cs.ref",
                            strOutputItemXml,
                            strOutputItemRecPath,   // 2009/10/18 new add
                            out strItemRecord,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                            goto ERROR1;
                        }
                        item_records[i] = strItemRecord;
                    }
                    // else if (String.Compare(strItemFormat, "xml", true) == 0)
                    else if (IsResultType(strItemFormat, "xml") == true)
                    {
                        // item_records[i] = strOutputItemXml;
                        string strResultXml = "";
                        nRet = GetItemXml(strOutputItemXml,
            strItemFormat,
            out strResultXml,
            out strError);
                        if (nRet == -1)
                        {
                            strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                            goto ERROR1;
                        }
                        item_records[i] = strResultXml;
                    }
                    else
                    {
                        strError = "strItemFormatList���������˲�֧�ֵ����ݸ�ʽ���� '" + strItemFormat + "'";
                        strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                        goto ERROR1;
                    }
                } // end of for
            }

            // 2008/5/9 new add
            if (StringUtil.IsInList("biblio", strStyle) == true)
            {
                if (String.IsNullOrEmpty(strBiblioRecID) == true)
                {
                    strError = "���¼XML��<parent>Ԫ��ȱ������ֵΪ��, ����޷���λ�ּ�¼ID";
                    strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                    goto ERROR1;
                }

                string strItemDbName = ResPath.GetDbName(strOutputItemRecPath);

                string strBiblioDbName = "";
                // ����ʵ�����, �ҵ���Ӧ����Ŀ����
                // return:
                //      -1  ����
                //      0   û���ҵ�
                //      1   �ҵ�
                nRet = this.GetBiblioDbNameByItemDbName(strItemDbName,
                    out strBiblioDbName,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    strError = "ʵ����� '" + strItemDbName + "' û���ҵ���Ӧ����Ŀ����";
                    strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                    goto ERROR1;
                }

                string strBiblioRecPath = strBiblioDbName + "/" + strBiblioRecID;

                string[] biblio_formats = strBiblioFormatList.Split(new char[] { ',' });
                biblio_records = new string[biblio_formats.Length];

                string strBiblioXml = "";
                // ������html xml text֮һ���Ż�ȡstrBiblioXml
                if (StringUtil.IsInList("html", strBiblioFormatList) == true
                    || StringUtil.IsInList("xml", strBiblioFormatList) == true
                    || StringUtil.IsInList("text", strBiblioFormatList) == true)
                {
                    RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "get channel error";
                        strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                        goto ERROR1;
                    }

                    string strMetaData = "";
                    byte[] timestamp = null;
                    string strTempOutputPath = "";
                    lRet = channel.GetRes(strBiblioRecPath,
                        out strBiblioXml,
                        out strMetaData,
                        out timestamp,
                        out strTempOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "����ּ�¼ '" + strBiblioRecPath + "' ʱ����: " + strError;
                        strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                        goto ERROR1;
                    }
                }

                for (int i = 0; i < biblio_formats.Length; i++)
                {
                    string strBiblioFormat = biblio_formats[i];

                    // ��Ҫ���ں�ӳ������ļ�
                    string strLocalPath = "";
                    string strBiblio = "";

                    // ����Ŀ��¼���ݴ�XML��ʽת��ΪHTML��ʽ
                    if (String.Compare(strBiblioFormat, "html", true) == 0)
                    {
                        // TODO: ����cache
                        nRet = this.MapKernelScriptFile(
                            sessioninfo,
                            strBiblioDbName,
                            "./cfgs/loan_biblio.fltx",
                            out strLocalPath,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                            goto ERROR1;
                        }

                        // ���ּ�¼���ݴ�XML��ʽת��ΪHTML��ʽ
                        string strFilterFileName = strLocalPath;    // app.CfgDir + "\\biblio.fltx";

                        if (string.IsNullOrEmpty(strBiblioXml) == false)
                        {
                            nRet = this.ConvertBiblioXmlToHtml(
                                strFilterFileName,
                                strBiblioXml,
                                    null,
                                strBiblioRecPath,
                                out strBiblio,
                                out strError);
                            if (nRet == -1)
                            {
                                strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                                goto ERROR1;
                            }

                        }
                        else
                            strBiblio = "";

                        biblio_records[i] = strBiblio;
                    }
                    // �����¼���ݴ�XML��ʽת��Ϊtext��ʽ
                    else if (String.Compare(strBiblioFormat, "text", true) == 0)
                    {
                        // TODO: ����cache
                        nRet = this.MapKernelScriptFile(
                            sessioninfo,
                            strBiblioDbName,
                            "./cfgs/loan_biblio_text.fltx",
                            out strLocalPath,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                            goto ERROR1;
                        }
                        // ���ּ�¼���ݴ�XML��ʽת��ΪTEXT��ʽ
                        string strFilterFileName = strLocalPath;    // app.CfgDir + "\\biblio.fltx";
                        if (string.IsNullOrEmpty(strBiblioXml) == false)
                        {
                            nRet = this.ConvertBiblioXmlToHtml(
                                strFilterFileName,
                                strBiblioXml,
                                    null,
                                strBiblioRecPath,
                                out strBiblio,
                                out strError);
                            if (nRet == -1)
                            {
                                strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                                goto ERROR1;
                            }

                        }
                        else
                            strBiblio = "";

                        biblio_records[i] = strBiblio;
                    }
                    else if (String.Compare(strBiblioFormat, "xml", true) == 0)
                    {
                        biblio_records[i] = strBiblioXml;
                    }
                    else if (String.Compare(strBiblioFormat, "recpath", true) == 0)
                    {
                        biblio_records[i] = strBiblioRecPath;
                    }
                    else if (string.IsNullOrEmpty(strBiblioFormat) == true)
                    {
                        biblio_records[i] = "";
                    }
                    else
                    {
                        strError = "strBiblioFormatList���������˲�֧�ֵ����ݸ�ʽ���� '" + strBiblioFormat + "'";
                        strError = "��Ȼ���������д��󣬵��ǻ�������Ѿ��ɹ�: " + strError;
                        goto ERROR1;
                    }
                } // end of for
            }


            this.WriteTimeUsed(start_time, "Return() ��ʱ ");
            // result.Valueֵ��ǰ����ܱ����ó�1
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        #region Return()�¼�����

        // �����¾ɲ��¼�Ƿ���ʵ���Ըı�
        // ��νʵ���Ըı䣬����<barcode>��<borrower>�����ֶε����ݷ����˱仯
        static bool IsItemRecordSignificantChanged(XmlDocument domOld,
            XmlDocument domNew)
        {
            string strOldBarcode = DomUtil.GetElementText(domOld.DocumentElement,
                "barcode");
            string strOldBorrower = DomUtil.GetElementText(domOld.DocumentElement,
                "borrower");

            string strNewBarcode = DomUtil.GetElementText(domNew.DocumentElement,
    "barcode");
            string strNewBorrower = DomUtil.GetElementText(domNew.DocumentElement,
                "borrower");

            if (strOldBarcode != strNewBarcode)
                return true;

            if (strOldBorrower != strNewBorrower)
                return true;

            return false;
        }

        // �����Զ��߼�¼�Ľ�����Ϣɾ������(��������)
        // parameters:
        //      strReaderRecPath    ���߼�¼·��
        //      strReaderBarcode    ����֤����š�����Ҫ����¼����������������Ƿ��Ѿ��仯�ˣ���ʹ�������������������飬����null
        //      strItemBarcode  �Ѿ���Ĳ������
        //      strDeleteBorrowFrag ��ɾ������<borrow>Ԫ��Ƭ��
        //      strAddedOverdueFrag �Ѿ������<overdue>Ԫ��Ƭ��
        // return:
        //      -1  error
        //      0   û�б�ҪUndo
        //      1   Undo�ɹ�
        int UndoReturnReaderRecord(
            RmsChannel channel,
            string strReaderRecPath,
            string strReaderBarcode,
            string strItemBarcode,
            string strDeleteBorrowFrag,
            string strAddedOverdueFrag,
            out string strError)
        {
            strError = "";
            long lRet = 0;
            int nRet = 0;

            string strMetaData = "";
            byte[] reader_timestamp = null;
            string strOutputPath = "";

            string strReaderXml = "";

            int nRedoCount = 0;

        REDO:

            lRet = channel.GetRes(strReaderRecPath,
    out strReaderXml,
    out strMetaData,
    out reader_timestamp,
    out strOutputPath,
    out strError);
            if (lRet == -1)
            {
                strError = "����ԭ��¼ '" + strReaderRecPath + "' ʱ����";
                return -1;
            }

            XmlDocument readerdom = null;
            nRet = LibraryApplication.LoadToDom(strReaderXml,
                out readerdom,
                out strError);
            if (nRet == -1)
            {
                strError = "װ�ؿ��ж��߼�¼ '" + strReaderRecPath + "' ����XML DOMʱ��������: " + strError;
                return -1;
            }

            // ������֤������ֶ� �Ƿ����仯
            if (String.IsNullOrEmpty(strReaderBarcode) == false)
            {
                string strReaderBarcodeContent = DomUtil.GetElementText(readerdom.DocumentElement,
                    "barcode");
                if (strReaderBarcode != strReaderBarcodeContent)
                {
                    strError = "���ִ����ݿ��ж����Ķ��߼�¼ '" + strReaderRecPath + "' ����<barcode>�ֶ����� '" + strReaderBarcodeContent + "' ��ҪUndo�Ķ��߼�¼֤����� '" + strReaderBarcode + "' �Ѳ�ͬ��";
                    return -1;
                }
            }

            // �۲�dom�б�ʾ���ĵĽڵ�
            XmlNode node = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcode + "']");
            if (node != null)
                return 0;   // �Ѿ�û�б�ҪUndo��


            // ���<borrows>Ԫ���Ƿ����
            XmlNode root = readerdom.DocumentElement.SelectSingleNode("borrows");
            if (root == null)
            {
                root = readerdom.CreateElement("borrows");
                root = readerdom.DocumentElement.AppendChild(root);
            }

            // �ӻ�<borrow>Ԫ��
            XmlDocumentFragment fragment = readerdom.CreateDocumentFragment();
            fragment.InnerXml = strAddedOverdueFrag;

            root.AppendChild(fragment);


            // ɾ���Ѿ������<overdue>Ԫ��
            {
                XmlDocument tempdom = new XmlDocument();
                tempdom.LoadXml(strAddedOverdueFrag);
                // �����id����
                string strID = DomUtil.GetAttr(tempdom.DocumentElement,
                    "id");

                if (String.IsNullOrEmpty(strID) == false)
                {
                    XmlNode nodeOverdue = readerdom.DocumentElement.SelectSingleNode(
                        "overdues/overdue[@id='" + strID + "']");
                    if (nodeOverdue != null)
                        nodeOverdue.ParentNode.RemoveChild(nodeOverdue);
                }

            }

            // TODO: ɾ���Ѿ����뵽<borrowHistory>�е�<borrow>Ԫ�أ�

            byte[] output_timestamp = null;
            // string strOutputPath = "";

            // д�ض��߼�¼
            lRet = channel.DoSaveTextRes(strReaderRecPath,
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
                        strError = "д�ض��߼�¼��ʱ����ʱ�����ͻ�������Ѿ�����10�Σ��Է�������ֻ��ֹͣ����";
                        return -1;
                    }
                    goto REDO;
                }

                strError = "д�ض��߼�¼��ʱ��������" + strError;
                return -1;
            }

            return 1;   // Undo�Ѿ��ɹ�
        }

        #endregion

        // ��װ�汾,Ϊ�˼��ݽű�ʹ��
        public int GetReaderCalendar(string strReaderType,
    out Calendar calendar,
    out string strError)
        {
            return GetReaderCalendar(strReaderType,
                "",
                out calendar,
                out strError);
        }

        // �������ȫ��
        // ����أ�"./��������"��ָ��ǰ�ݴ���Ļ������������統ǰ�ݴ���Ϊ������ֹݡ�����Ӧ�ù淶Ϊ������ֹ�/����������
        public static string GetCalencarFullName(string strName,
            string strLibraryCodeParam)
        {
            string strLibraryCode = "";
            string strPureName = "";

            // ����������
            ParseCalendarName(strName,
        out strLibraryCode,
        out strPureName);

            if (strLibraryCode == ".")
            {
                if (string.IsNullOrEmpty(strLibraryCode) == true)
                    return strPureName;

                return strLibraryCodeParam + "/" + strPureName;
            }

            return strName;
        }

        // ��ú�һ���ض��������������������
        // return:
        //      -1  error
        //      0   succeed
        public int GetReaderCalendar(string strReaderType,
            string strLibraryCode,
            out Calendar calendar,
            out string strError)
        {
            strError = "";
            calendar = null;

            // ��� '����������' ���ò���
            string strCalendarName = "";
            MatchResult matchresult;
            // return:
            //      reader��book���;�ƥ�� ��4��
            //      ֻ��reader����ƥ�䣬��3��
            //      ֻ��book����ƥ�䣬��2��
            //      reader��book���Ͷ���ƥ�䣬��1��
            int nRet = this.GetLoanParam(
                //null,
                strLibraryCode,
                strReaderType,
                "",
                "����������",
                out strCalendarName,
                out matchresult,
                out strError);
            if (nRet == -1)
            {
                strError = "��� �ݴ��� '"+strLibraryCode+"' �� �������� '" + strReaderType + "' �� ���������� ����ʱ��������: " + strError;
                return -1;
            }
            if (nRet < 3)
            {
                strError = "�ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' �� ���������� �����޷����: " + strError;
                return -1;
            }

            // ����أ�"./��������"��ָ��ǰ�ݴ���Ļ������������統ǰ�ݴ���Ϊ������ֹݡ�����Ӧ���á�����ֹ�/����������ȥѰ��
            strCalendarName = GetCalencarFullName(strCalendarName, strLibraryCode);

            string strXPath = "";

            strXPath = "calendars/calendar[@name='" + strCalendarName + "']";
            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes(strXPath);

            if (nodes.Count == 0)
            {
                strError = "��Ϊ '" + strCalendarName + "' ���������ò�����";
                return -1;
            }

            string strName = DomUtil.GetAttr(nodes[0], "name");
            string strData = nodes[0].InnerText;

            try
            {
                calendar = new Calendar(strName, strData);
            }
            catch (Exception ex)
            {
                strError = "���� '" + strCalendarName + "' �����ݹ���Calerdar����ʱ����: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // (Ϊ����Ŀ��)�������
        // �ֹ��û�Ҳ�ܿ���ȫ������
        // parameters:
        //      strAction   get list getcount
        public int GetCalendar(string strAction,
            string strLibraryCodeList,
            string strName,
            int nStart,
            int nCount,
            out List<CalenderInfo> contents,
            out string strError)
        {
            contents = new List<CalenderInfo>();
            strError = "";

            string strXPath = "";

#if NO
            if (strAction == "list" || strAction == "getcount")
                strXPath = "calendars/calendar";    // �г�����
            else if (strAction == "get")
            {
                if (string.IsNullOrEmpty(strName) == false)
                    strXPath = "calendars/calendar[@name='" + strName + "']";
                else
                    strXPath = "calendars/calendar";    // �г�����
            }
            else
            {
                strError = "����ʶ���strAction���� '" + strAction + "'";
                return -1;
            }
#endif
            // 2014/3/2
            if (string.IsNullOrEmpty(strName) == false)
                strXPath = "calendars/calendar[@name='" + strName + "']";
            else
                strXPath = "calendars/calendar";    // �г�����

            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes(strXPath);

            // ������Ҫ�õ�����
            if (strAction == "getcount")
                return nodes.Count;

            if (nCount == -1)
                nCount = nodes.Count - nStart;

            for (int i = nStart; i < Math.Min(nodes.Count, nStart + nCount); i++)
            {
                XmlNode node = nodes[i];

                string strCurName = DomUtil.GetAttr(node, "name");
                string strComment = DomUtil.GetAttr(node, "comment");
                string strRange = DomUtil.GetAttr(node, "range");

                CalenderInfo info = new CalenderInfo();
                info.Name = strCurName;
                info.Range = strRange;
                info.Comment = strComment;

                if (strAction == "list")
                {
                    // ����������
                    contents.Add(info);
                    continue;
                }


                info.Content = node.InnerText;
                contents.Add(info);
            }

            return nodes.Count; // ��������
        }

        // ����������
        public static void ParseCalendarName(string strName,
            out string strLibraryCode,
            out string strPureName)
        {
            strLibraryCode = "";
            strPureName = "";
            int nRet = strName.IndexOf("/");
            if (nRet == -1)
            {
                strPureName = strName;
                return;
            }
            strLibraryCode = strName.Substring(0, nRet).Trim();
            strPureName = strName.Substring(nRet + 1).Trim();
        }


        // �޸�����
        // �ֹ��û�ֻ���޸��Լ���Ͻ�ķֹݵ�����
        // parameters:
        //      strAction   change new delete overwirte(2008/8/23 new add)
        public int SetCalendar(string strAction,
            string strLibraryCodeList,
            CalenderInfo info,
            out string strError)
        {
            strError = "";

            {
                string strLibraryCode = "";
                string strPureName = "";

                // ����������
                ParseCalendarName(info.Name,
            out strLibraryCode,
            out strPureName);

                // ����������йݴ��롣����ʹ�õ����ݴ���
                if (strLibraryCode.IndexOf(",") != -1)
                {
                    strError = "�������йݴ��벿�ֲ������ж���";
                    return -1;
                }
                // ����������йݴ��롣����ʹ��.
                if (strLibraryCode.IndexOf(".") != -1)
                {
                    strError = "�������йݴ��벿�ֲ�����ʹ�÷��� '.' ";
                    return -1;
                }

                if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                {

                    if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û���Ͻ�Ĺݴ���Ϊ '" + strLibraryCodeList + "'���������������еĹݴ��� '" + strLibraryCode + "'���޸Ĳ������ܾ�";
                        return -1;
                    }
                }
            }

            string strXPath = "";

            strXPath = "calendars/calendar[@name='" + info.Name + "']";

            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes(strXPath);

            XmlNode node = null;

            // 2008/8/23 new add
            if (strAction == "overwrite")
            {
                if (String.IsNullOrEmpty(info.Name) == true)
                {
                    strError = "����������Ϊ��";
                    return -1;
                }

                if (nodes.Count == 0)
                {
                    XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("calendars");
                    if (root == null)
                    {
                        root = this.LibraryCfgDom.CreateElement("calendars");
                        this.LibraryCfgDom.DocumentElement.AppendChild(root);
                    }

                    node = this.LibraryCfgDom.CreateElement("calendar");
                    root.AppendChild(node);
                }
                else if (nodes.Count > 1)
                {
                    // ��ǿ��׳��
                    for (int i = 1; i < nodes.Count; i++)
                    {
                        nodes[i].ParentNode.RemoveChild(nodes[i]);
                    }
                    node = nodes[0];

                }
                else
                {
                    Debug.Assert(nodes.Count == 1, "");
                    node = nodes[0];
                }

                DomUtil.SetAttr(node, "name", info.Name);   // 2008/10/8 ���ӡ�ԭ��ȱ�ٱ��У�Ϊһ��bug
                DomUtil.SetAttr(node, "range", info.Range);
                DomUtil.SetAttr(node, "comment", info.Comment);
                node.InnerText = info.Content;
                this.Changed = true;
                return 0;
            }


            if (strAction == "change")
            {
                if (nodes.Count == 0)
                {
                    strError = "������ '" + info.Name + "' ������";
                    return -1;
                }
                if (nodes.Count > 1)
                {
                    strError = "������ '" + info.Name + "' ����  " + nodes.Count.ToString() + " �����޸Ĳ������ܾ���";
                    return -1;
                }
                node = nodes[0];
                DomUtil.SetAttr(node, "range", info.Range);
                DomUtil.SetAttr(node, "comment", info.Comment);
                node.InnerText = info.Content;
                this.Changed = true;
                return 0;
            }

            if (strAction == "new")
            {
                if (String.IsNullOrEmpty(info.Name) == true)
                {
                    strError = "����������Ϊ��";
                    return -1;
                }

                if (nodes.Count > 0)
                {
                    strError = "������ '" + info.Name + "' �Ѿ�����";
                    return -1;
                }

                XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("calendars");
                if (root == null)
                {
                    root = this.LibraryCfgDom.CreateElement("calendars");
                    this.LibraryCfgDom.DocumentElement.AppendChild(root);
                }

                node = this.LibraryCfgDom.CreateElement("calendar");
                root.AppendChild(node);

                DomUtil.SetAttr(node, "name", info.Name);
                DomUtil.SetAttr(node, "range", info.Range);
                DomUtil.SetAttr(node, "comment", info.Comment);
                node.InnerText = info.Content;
                this.Changed = true;
                return 0;
            }

            if (strAction == "delete")
            {
                if (nodes.Count == 0)
                {
                    strError = "������ '" + info.Name + "' ������";
                    return -1;
                }

                for (int i = 0; i < nodes.Count; i++)
                {
                    node = nodes[i];
                    node.ParentNode.RemoveChild(node);
                }
                this.Changed = true;
                return 0;
            }

            strError = "�޷�ʶ���strAction����ֵ '" + strAction + "' ";
            return -1;
        }

#if DEBUG_LOAN_PARAM
        public int GetLoanParam(
            XmlDocument cfg_dom,
            string strReaderType,
            string strBookType,
            string strParamName,
            out string strParamValue,
            out MatchResult matchresult,
            out string strError)
        {
            string strDebug = "";
            return GetLoanParam(
                cfg_dom,
                strReaderType,
                strBookType,
                strParamName,
                out strParamValue,
                out matchresult,
                out strDebug,
                out strError);
        }
#endif

        // ��װ��İ汾
        // �����ͨ����
        // parameters:
        //      strLibraryCode  ͼ��ݴ���, ���Ϊ��,��ʾʹ��<library>Ԫ�������Ƭ��
        // return:
        //      reader��book���;�ƥ�� ��4��
        //      ֻ��reader����ƥ�䣬��3��
        //      ֻ��book����ƥ�䣬��2��
        //      reader��book���Ͷ���ƥ�䣬��1��
        public int GetLoanParam(
            string strLibraryCode,
            string strReaderType,
            string strBookType,
            string strParamName,
            out string strParamValue,
            out MatchResult matchresult,
#if DEBUG_LOAN_PARAM
            out string strDebug,
#endif
 out string strError)
        {
            strParamValue = "";
            strError = "";
            matchresult = MatchResult.None;

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("rightsTable");
            if (root == null)
            {
                strError = "library.xml �����ļ�����δ���� <rightsTable> Ԫ��";
                return -1;
            }

            return LoanParam.GetLoanParam(
                   root,    // this.LibraryCfgDom,
                   strLibraryCode,
                   strReaderType,
                   strBookType,
                   strParamName,
                    out strParamValue,
                    out matchresult,
#if DEBUG_LOAN_PARAM
                    out strDebug,
#endif
 out strError);
        }


        /*
        public List<string> GetReaderTypes()
        {
            List<string> result = new List<string>();
            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("//readerTypes/item");   // 0.02��ǰΪreadertypes

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                result.Add(node.InnerText);
            }

            return result;
        }

        public List<string> GetBookTypes()
        {
            List<string> result = new List<string>();
            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("//bookTypes/item");   // 0.02��ǰΪbooktypes

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                result.Add(node.InnerText);
            }

            return result;
        }
         * */

        public int SetValueTablesXml(
string strLibraryCodeList,
string strFragment,
out string strError)
        {
            return SetLibraryFragmentXml(
                "valueTables",
                strLibraryCodeList,
                strFragment,
                out strError);
        }

        // ��ǰ�˷�����Ȩ��XML������µ�library.xml��
        public int SetRightsTableXml(
string strLibraryCodeList,
string strFragment,
out string strError)
        {
            return SetLibraryFragmentXml(
                "rightsTable",
                strLibraryCodeList,
                strFragment,
                out strError);
        }

        // ��ǰ�˷�����Ƭ��XML������µ�library.xml��
        public int SetLibraryFragmentXml(
            string strRootElementName,
string strLibraryCodeList,
string strFragment,
out string strError)
        {
            strError = "";

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode(strRootElementName);   // 0.02ǰΪrightstable
            if (root == null)
            {
                root = this.LibraryCfgDom.CreateElement(strRootElementName);
                this.LibraryCfgDom.DocumentElement.AppendChild(root);
            }

            XmlDocument source_dom = new XmlDocument();
            source_dom.LoadXml("<" + strRootElementName + " />");

            XmlDocumentFragment fragment = source_dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = strFragment;
            }
            catch (Exception ex)
            {
                strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                return -1;
            }

            source_dom.DocumentElement.AppendChild(fragment);

            // �������<library>Ԫ�ص�code����ֵ
            // parameters:
            // return:
            //      -1  ���Ĺ��̳���
            //      0   û�д���
            //      1   �����ִ���
            int nRet = CheckLibraryCodeAttr(source_dom.DocumentElement,
                strLibraryCodeList,
                out strError);
            if (nRet != 0)
                return -1;

            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == true)
            {
                try
                {
                    root.InnerXml = strFragment;
                }
                catch (Exception ex)
                {
                    strError = "����<" + strRootElementName + ">Ԫ�ص�InnerXmlʱ��������: " + ex.Message;
                    return -1;
                }

                return 0;
            }
            else
            {
                // ����Ƿ��в������κ�<library>Ԫ�ص�Ԫ��
                XmlNodeList nodes = source_dom.DocumentElement.SelectNodes("descendant::*[count(ancestor-or-self::library) = 0]");
                if (nodes.Count > 0)
                {
                    strError = "��ǰ�û��ķֹ��û���ݲ������Ᵽ���<" + strRootElementName + ">�����г��ַ�<library>Ԫ���¼�������Ԫ��";
                    return -1;
                }
            }

            List<string> librarycodes = StringUtil.FromListString(strLibraryCodeList);

            // �Ե�ǰ�û��ܹ�Ͻ��ÿ���ݴ�����д��� -- ɾ��ÿ��libraryԪ��
            foreach (string strLibraryCode in librarycodes)
            {
                XmlNode target = root.SelectSingleNode("descendant::library[@code='" + strLibraryCode + "']");
                if (target != null)
                {
                    target.ParentNode.RemoveChild(target);
                }
            }

            // �Ե�ǰ�û��ܹ�Ͻ��ÿ���ݴ�����д��� -- ����ǰ�˷�����<library>Ԫ��
            foreach (string strLibraryCode in librarycodes)
            {
                XmlNode source = source_dom.DocumentElement.SelectSingleNode("descendant::library[@code='" + strLibraryCode + "']");
                if (source == null)
                    continue;   // Դû�����Ԫ��

                Debug.Assert(source != null, "");


                XmlNode target = root.OwnerDocument.CreateElement("library");
                root.AppendChild(target);
                DomUtil.SetAttr(target, "code", strLibraryCode);

                target.InnerXml = source.InnerXml;
            }

            return 0;
        }

        public int GetValueTablesXml(
string strLibraryCodeList,
out string strValue,
out string strError)
        {
            return GetiLibraryFragmentXml(
                "valueTables",
                strLibraryCodeList,
                out strValue,
                out strError);
        }

        public int GetRightsTableXml(
string strLibraryCodeList,
out string strValue,
out string strError)
        {
            return GetiLibraryFragmentXml(
                "rightsTable",
                strLibraryCodeList,
                out strValue,
                out strError);
        }

        public int GetiLibraryFragmentXml(
            string strRootElementName,
            string strLibraryCodeList,
            out string strValue,
            out string strError)
        {
            strError = "";
            strValue = "";

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode(strRootElementName);   // 0.02ǰΪrightstable
            if (root == null)
                return 0;
            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == true)
            {
                strValue = root.InnerXml;
                return 0;
            }

            XmlDocument domNew = new XmlDocument();
            domNew.LoadXml("<" + strRootElementName + " />");

            List<string> librarycodes = StringUtil.FromListString(strLibraryCodeList);
            foreach (string strLibraryCode in librarycodes)
            {
                XmlNode source = root.SelectSingleNode("descendant::library[@code='" + strLibraryCode + "']");
                if (source == null)
                    continue;

                XmlNode target = domNew.CreateElement("library");
                domNew.DocumentElement.AppendChild(target);
                DomUtil.SetAttr(target, "code", strLibraryCode);

                target.InnerXml = source.InnerXml;
            }

            strValue = domNew.DocumentElement.InnerXml;
            return 0;
        }

        // �������<library>Ԫ�ص�code����ֵ
        // parameters:
        // return:
        //      -1  ���Ĺ��̳���
        //      0   û�д���
        //      1   �����ִ���
        public static int CheckLibraryCodeAttr(XmlNode root,
            string strLibraryCodeList,
            out string strError)
        {
            strError = "";

            List<string> all_librarycodes = new List<string>();
            XmlNodeList nodes = root.SelectNodes("descendant::library");
            foreach (XmlNode node in nodes)
            {
                string strCode = DomUtil.GetAttr(node, "code");
                if (string.IsNullOrEmpty(strCode) == true)
                    continue;
                if (strCode.IndexOf(" ") != -1)
                {
                    strError = "<library>Ԫ�ص�code����ֵ '"+strCode+"' �в�Ӧ�����ո��ַ�";
                    return 1;
                }
                if (strCode.IndexOf(",") != -1)
                {
                    strError = "<library>Ԫ�ص�code����ֵ '" + strCode + "' �в�Ӧ���������ַ�";
                    return 1;
                }
                if (strCode.IndexOf("*") != -1)
                {
                    strError = "<library>Ԫ�ص�code����ֵ '" + strCode + "' �в�Ӧ�����Ǻ��ַ�";
                    return 1;
                }

                all_librarycodes.Add(strCode);
            }

            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
            {
                List<string> range = StringUtil.FromListString(strLibraryCodeList);
                // �۲�all_librarycodes�����Ƿ񳬹�strLibraryCodeList��Χ
                foreach (string strCode in all_librarycodes)
                {
                    if (range.IndexOf(strCode) == -1)
                    {
                        strError = "<library>Ԫ�ص�code����ֵ '" + strCode + "' ������Χ '" + strLibraryCodeList + "'�����ǲ������";
                        return 1;
                    }
                }
            }

            return 0;
        }

        // ȡ�������ϵĽ��沿��
        static List<string> AND(List<string> list1, List<string> list2)
        {
            List<string> result = new List<string>();
            foreach (string s in list1)
            {
                if (list2.IndexOf(s) != -1)
                    result.Add(s);
            }

            return result;
        }

        // ���¾�����<location>Ԫ�ذ���name���Խ�����ײ���ó���������
        // parameters:
        //      create_nodes    [out]���������Ľڵ� (���� new_nodes)
        //      delete_nodes    [out]����ɾ���Ľڵ� (���� old_nodes)
        //      remain_nodes    [out]�¾�֮�乲ͬ�Ľڵ� (���� old_nodes)
        static int GetThreeLocationCollections(XmlNode new_root,
            XmlNode old_root,
            out List<XmlNode> create_nodes,
            out List<XmlNode> delete_nodes,
            out List<XmlNode> remain_nodes,
            out string strError)
        {
            strError = "";

            create_nodes = new List<XmlNode>();
            delete_nodes = new List<XmlNode>();
            remain_nodes = new List<XmlNode>();

            XmlNodeList new_nodes = new_root.SelectNodes("*");
            XmlNodeList old_nodes = old_root.SelectNodes("*");

            if (new_nodes.Count == 0)
            {
                foreach (XmlNode node in old_nodes)
                {
                    delete_nodes.Add(node);
                }
                return 0;
            }

            if (old_nodes.Count == 0)
            {
                foreach (XmlNode node in new_nodes)
                {
                    create_nodes.Add(node);
                }
                return 0;
            }

            List<string> old_names = new List<string>();
            List<string> new_names = new List<string>();


            foreach (XmlNode node in old_nodes)
            {
                string strName = DomUtil.GetAttr(node, "name");
                /*
                if (string.IsNullOrEmpty(strName) == true)
                {
                    strError = "�ݲصص�������Ϊ�ա�'" + node.OuterXml + "'";
                    return -1;
                }
                 * */
                if (old_names.IndexOf(strName) != -1)
                {
                    strError = "�ݲصص��� '" + strName + "' ��Ӧ�ظ�ʹ��";
                    return -1;
                }
                old_names.Add(strName);
            }

            foreach (XmlNode node in new_nodes)
            {
                string strName = DomUtil.GetAttr(node, "name");
                /*
                if (string.IsNullOrEmpty(strName) == true)
                {
                    strError = "�ݲصص�������Ϊ�ա�'" + node.OuterXml + "'";
                    return -1;
                }
                 * */
                if (new_names.IndexOf(strName) != -1)
                {
                    strError = "�ݲصص��� '" + strName + "' ��Ӧ�ظ�ʹ��";
                    return -1;
                }
                new_names.Add(strName);
            }

            // ��������
            List<string> common_names = AND(old_names, new_names);

            foreach (string strName in common_names)
            {
                XmlNode node = old_root.SelectSingleNode("location[@name='" + strName + "']");
                if (node == null)
                {
                    strError = "����� old_root ��û���ҵ� name ����Ϊ '" + strName + "' ��<location>Ԫ��";
                    return -1;
                }
                remain_nodes.Add(node);
            }

            // Ҫ�����Ĳ���
            foreach (XmlNode node in new_nodes)
            {
                string strName = DomUtil.GetAttr(node, "name");
                if (common_names.IndexOf(strName) == -1)
                {
                    create_nodes.Add(node);
                }
            }

            // Ҫɾ���Ĳ���
            foreach (XmlNode node in old_nodes)
            {
                string strName = DomUtil.GetAttr(node, "name");
                if (common_names.IndexOf(strName) == -1)
                {
                    delete_nodes.Add(node);
                }
            }
            return 0;
        }

        // ���¾�����<group>Ԫ�ذ���name���Խ�����ײ���ó���������
        // parameters:
        //      create_nodes    [out]���������Ľڵ� (���� new_nodes)
        //      delete_nodes    [out]����ɾ���Ľڵ� (���� old_nodes)
        //      remain_nodes    [out]�¾�֮�乲ͬ�Ľڵ� (���� old_nodes)
        static int GetThreeGroupCollections(XmlNode new_root,
            XmlNode old_root,
            out List<XmlNode> create_nodes,
            out List<XmlNode> delete_nodes,
            out List<XmlNode> remain_nodes,
            out string strError)
        {
            strError = "";

            create_nodes = new List<XmlNode>();
            delete_nodes = new List<XmlNode>();
            remain_nodes = new List<XmlNode>();

            XmlNodeList new_nodes = new_root.SelectNodes("*");
            XmlNodeList old_nodes = old_root.SelectNodes("*");

            if (new_nodes.Count == 0)
            {
                foreach (XmlNode node in old_nodes)
                {
                    delete_nodes.Add(node);
                }
                return 0;
            }

            if (old_nodes.Count == 0)
            {
                foreach (XmlNode node in new_nodes)
                {
                    create_nodes.Add(node);
                }
                return 0;
            }

            List<string> old_names = new List<string>();
            List<string> new_names = new List<string>();


            foreach (XmlNode node in old_nodes)
            {
                string strName = DomUtil.GetAttr(node, "name");
                if (string.IsNullOrEmpty(strName) == true)
                {
                    strError = "�ż���ϵ������Ϊ�ա�'"+node.OuterXml+"'";
                    return -1;
                }
                if (old_names.IndexOf(strName) != -1)
                {
                    strError = "�ż���ϵ�� '"+strName+"' ��Ӧ�ظ�ʹ��";
                    return -1;
                }
                old_names.Add(strName);
            }

            foreach (XmlNode node in new_nodes)
            {
                string strName = DomUtil.GetAttr(node, "name");
                if (string.IsNullOrEmpty(strName) == true)
                {
                    strError = "�ż���ϵ������Ϊ�ա�'" + node.OuterXml + "'";
                    return -1;
                }
                if (new_names.IndexOf(strName) != -1)
                {
                    strError = "�ż���ϵ�� '" + strName + "' ��Ӧ�ظ�ʹ��";
                    return -1;
                }
                new_names.Add(strName);
            }

            // ��������
            List<string> common_names = AND(old_names, new_names);

            foreach (string strName in common_names)
            {
                XmlNode node = old_root.SelectSingleNode("group[@name='"+strName+"']");
                if (node == null)
                {
                    strError = "����� old_root ��û���ҵ� name ����Ϊ '"+strName+"' ��<group>Ԫ��";
                    return -1;
                }
                remain_nodes.Add(node);
            }

            // Ҫ�����Ĳ���
            foreach (XmlNode node in new_nodes)
            {
                string strName = DomUtil.GetAttr(node, "name");
                if (common_names.IndexOf(strName) == -1)
                {
                    create_nodes.Add(node);
                }
            }

            // Ҫɾ���Ĳ���
            foreach (XmlNode node in old_nodes)
            {
                string strName = DomUtil.GetAttr(node, "name");
                if (common_names.IndexOf(strName) == -1)
                {
                    delete_nodes.Add(node);
                }
            }
            return 0;
        }

        // �۲�����XmlNode�������Ƿ���ȫһ��
        static bool AttrEqual(XmlNode node1, XmlNode node2)
        {
            if (node1.Attributes.Count != node2.Attributes.Count)
                return false;

            List<String> attrs1 = new List<string>();
            List<string> attrs2 = new List<string>();

            foreach (XmlAttribute attr in node1.Attributes)
            {
                attrs1.Add(attr.Name + "=" + attr.Value);
            }

            foreach (XmlAttribute attr in node2.Attributes)
            {
                attrs2.Add(attr.Name + "=" + attr.Value);
            }

            Debug.Assert(attrs1.Count == attrs2.Count, "");

            attrs1.Sort();
            attrs2.Sort();

            for(int i=0;i<attrs1.Count; i++)
            {
                if (attrs1[i] != attrs2[i])
                    return false;
            }

            return true;
        }

        // �޸� <callNumber> Ԫ�ض��塣������ר���ڷֹ��û���ȫ���û�����ֱ���޸����Ԫ�ص� InnerXml ����
        public int SetCallNumberXml(
string strLibraryCodeList,
string strFragment,
out string strError)
        {
            strError = "";

            XmlDocument source_dom = new XmlDocument();
            source_dom.LoadXml("<root />");

            XmlDocumentFragment fragment = source_dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = strFragment;
            }
            catch (Exception ex)
            {
                strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                return -1;
            }

            source_dom.DocumentElement.AppendChild(fragment);
            XmlNode source_root = source_dom.DocumentElement;

            XmlNode exist_root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("callNumber");
            if (exist_root == null)
            {
                exist_root = this.LibraryCfgDom.CreateElement("callNumber");
                this.LibraryCfgDom.DocumentElement.AppendChild(exist_root);
                this.Changed = true;
            }

            // �ֱ������ද����
            // 1) �������µ�<group>Ԫ��
            // 2) ɾ����ԭ�е�<group>Ԫ��
            // 3) �޸���ԭ�е�<group>Ԫ��

            // ���¾�����<group>Ԫ�ذ���name���Խ�����ײ���ó���������

            List<XmlNode> create_group_nodes = null;
            List<XmlNode> delete_group_nodes = null;
            List<XmlNode> remain_group_nodes = null;

            // parameters:
            //      create_nodes    [out]���������Ľڵ� (���� new_nodes)
            //      delete_nodes    [out]����ɾ���Ľڵ� (���� old_nodes)
            //      remain_nodes    [out]�¾�֮�乲ͬ�Ľڵ� (���� old_nodes)
            int nRet = GetThreeGroupCollections(source_root,
                exist_root,
                out create_group_nodes,
                out delete_group_nodes,
                out remain_group_nodes,
                out strError);

            // �۲�����´�����<group>Ԫ�أ��Ƿ�������<location>Ԫ�ض��ǵ�ǰ�û���Ͻ��Χ�ڵĹݲصص�����
            foreach (XmlNode group_node in create_group_nodes)
            {
                XmlNodeList location_nodes = group_node.SelectNodes("location");
                foreach (XmlNode location in location_nodes)
                {
                    string strLocationName = DomUtil.GetAttr(location, "name");

                    string strLibraryCode = "";
                    string strPureName = "";

                    // ����
                    ParseCalendarName(strLocationName,
                out strLibraryCode,
                out strPureName);

                    if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                    {
                        strError = "Ҫ������name����ֵΪ '" + DomUtil.GetAttr(group_node, "name") + "' ��<group>Ԫ������������<location>Ԫ��name�����еĹݲصص� '" + strLocationName + "' ���ڵ�ǰ�û���Ͻ��Χ '" + strLibraryCodeList + "' �ڣ��޸�<callNumber>����������ܾ�";
                        return -1;
                    }
                }
            }

            // �۲����ɾ����<group>Ԫ�أ��Ƿ�������<location>Ԫ�ض��ǵ�ǰ�û���Ͻ��Χ�ڵĹݲصص�����
            foreach (XmlNode group_node in delete_group_nodes)
            {
                XmlNodeList location_nodes = group_node.SelectNodes("location");
                foreach (XmlNode location in location_nodes)
                {
                    string strLocationName = DomUtil.GetAttr(location, "name");

                    string strLibraryCode = "";
                    string strPureName = "";

                    // ����
                    ParseCalendarName(strLocationName,
                out strLibraryCode,
                out strPureName);

                    if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                    {
                        strError = "Ҫɾ����name����ֵΪ '"+DomUtil.GetAttr(group_node, "name")+"' ��<group>Ԫ������������<location>Ԫ��name�����еĹݲصص� '" + strLocationName + "' ���ڵ�ǰ�û���Ͻ��Χ '" + strLibraryCodeList + "' �ڣ��޸�<callNumber>����������ܾ�";
                        return -1;
                    }
                }
            }

            // �۲�����޸ĵ�ÿ��<group>Ԫ�أ������������ӵ�<location>Ԫ�غ�ɾ����<location>Ԫ�ض��������ڵ�ǰ�û��Ĺ�Ͻ��Χ��
            // �������Ҫ�޸�<group>Ԫ�ر���ĳ���name������κ�һ�����ԣ���Ҫ��������<location>ȫ���ڵ�ǰ�û��Ĺ�Ͻ��Χ�ڲ���
            foreach (XmlNode group_node in remain_group_nodes)
            {
                // ע�� node ���� old_nodes ����
                string strGroupName = DomUtil.GetAttr(group_node, "name");

                XmlNode new_group = source_root.SelectSingleNode("group[@name='"+strGroupName+"']");
                if (new_group == null)
                {
                    strError = "name����ֵΪ '"+strGroupName+"' ��<group>Ԫ�������ύ��<callNumber> XMLƬ���о�Ȼû���ҵ�";
                    return -1;
                }

                XmlNode old_group = exist_root.SelectSingleNode("group[@name='" + strGroupName + "']");
                if (old_group == null)
                {
                    strError = "name����ֵΪ '" + strGroupName + "' ��<group>Ԫ����ԭ�е�<callNumber> XMLƬ���о�Ȼû���ҵ�";
                    return -1;
                }

                List<XmlNode> create_location_nodes = null;
                List<XmlNode> delete_location_nodes = null;
                List<XmlNode> remain_location_nodes = null;

                // ���¾�����<location>Ԫ�ذ���name���Խ�����ײ���ó���������
                // parameters:
                //      create_nodes    [out]���������Ľڵ� (���� new_nodes)
                //      delete_nodes    [out]����ɾ���Ľڵ� (���� old_nodes)
                //      remain_nodes    [out]�¾�֮�乲ͬ�Ľڵ� (���� old_nodes)
                nRet = GetThreeLocationCollections(new_group,
                    old_group,
                    out create_location_nodes,
                    out delete_location_nodes,
                    out remain_location_nodes,
                    out strError);
                if (nRet == -1)
                    return -1;

                // �۲�����´�����<location>Ԫ�أ��Ƿ��ǵ�ǰ�û���Ͻ��Χ�ڵĹݲصص�����
                foreach (XmlNode location in create_location_nodes)
                {
                    string strLocationName = DomUtil.GetAttr(location, "name");

                    string strLibraryCode = "";
                    string strPureName = "";

                    // ����
                    ParseCalendarName(strLocationName,
                out strLibraryCode,
                out strPureName);

                    if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                    {
                        strError = "name����ֵΪ '"+strGroupName+"' ��<group>Ԫ���£���������<location>Ԫ��name����ֵ�еĹݲصص� '" + strLocationName + "' ���ڵ�ǰ�û���Ͻ��Χ '" + strLibraryCodeList + "' �ڣ��޸�<callNumber>����������ܾ�";
                        return -1;
                    }
                }


                // �۲����ɾ����<location>Ԫ�أ��Ƿ��ǵ�ǰ�û���Ͻ��Χ�ڵĹݲصص�����
                foreach (XmlNode location in delete_location_nodes)
                {
                    string strLocationName = DomUtil.GetAttr(location, "name");

                    string strLibraryCode = "";
                    string strPureName = "";

                    // ����
                    ParseCalendarName(strLocationName,
                out strLibraryCode,
                out strPureName);

                    if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                    {
                        strError = "name����ֵΪ '" + strGroupName + "' ��<group>Ԫ���£���ɾ����ԭ��<location>Ԫ��name����ֵ�еĹݲصص� '" + strLocationName + "' ���ڵ�ǰ�û���Ͻ��Χ '" + strLibraryCodeList + "' �ڣ��޸�<callNumber>����������ܾ�";
                        return -1;
                    }
                }

                // �۲�<group>Ԫ�ر���������޸����
                if (AttrEqual(old_group, new_group) == false)
                {
                    // new_root������<location>Ԫ�أ����붼�ڵ�ǰ�û��Ĺ�Ͻ��Χ��
                    XmlNodeList locations = new_group.SelectNodes("location");
                    foreach (XmlNode location in locations)
                    {
                        string strLocationName = DomUtil.GetAttr(location, "name");

                        string strLibraryCode = "";
                        string strPureName = "";

                        // ����
                        ParseCalendarName(strLocationName,
                    out strLibraryCode,
                    out strPureName);

                        if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                        {
                            strError = "name����ֵΪ '" + strGroupName + "' ��<group>Ԫ�أ��������Է������޸ģ���Ҫ�����µ�����<location>Ԫ��Ӧ�ڵ�ǰ�û��Ĺ�Ͻ��Χ�ڡ����������<group>Ԫ�����µ�<location>Ԫ��name����ֵ�еĹݲصص� '" + strLocationName + "' ���ڵ�ǰ�û���Ͻ��Χ '" + strLibraryCodeList + "' �ڣ��޸�<callNumber>����������ܾ�";
                            return -1;
                        }

                    }
                }
            }

            // ��û�������ˣ������޸�
            exist_root.InnerXml = source_root.InnerXml;
            this.Changed = true;
            return 0;
        }

        public int SetLocationTypesXml(
    string strLibraryCodeList,
    string strFragment,
    out string strError)
        {
            strError = "";

            XmlDocument source_dom = new XmlDocument();
            source_dom.LoadXml("<root />");

            XmlDocumentFragment fragment = source_dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = strFragment;
            }
            catch (Exception ex)
            {
                strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                return -1;
            }

            source_dom.DocumentElement.AppendChild(fragment);

            // �������<library>Ԫ�ص�code����ֵ
            // parameters:
            // return:
            //      -1  ���Ĺ��̳���
            //      0   û�д���
            //      1   �����ִ���
            int nRet = CheckLibraryCodeAttr(source_dom.DocumentElement,
                strLibraryCodeList,
                out strError);
            if (nRet != 0)
                return -1;

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("locationTypes");
            if (root == null)
            {
                root = this.LibraryCfgDom.CreateElement("locationTypes");
                this.LibraryCfgDom.DocumentElement.AppendChild(root);
                this.Changed = true;
            }
            // �ѵ�ǰ�û��ܹ�Ͻ��ȫ������Ƭ��ɾ����Ȼ��һ��һ������
            // ע�⣬listΪ�ջ���"*"����Ͻȫ������
            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == true)
            {
                // root.RemoveAll();

                root.InnerXml = source_dom.DocumentElement.InnerXml;
                this.Changed = true;
                return 0;
            }
            else
            {
                // ����Ƿ��в������κ�<library>Ԫ�ص�Ԫ��
                XmlNodeList nodes = source_dom.DocumentElement.SelectNodes("descendant::*[count(ancestor-or-self::library) = 0]");
                if (nodes.Count > 0)
                {
                    strError = "��ǰ�û��ķֹ��û���ݲ������Ᵽ���<locationTypes>�����г��ַ�<library>Ԫ���¼�������Ԫ��";
                    return -1;
                }

                List<string> librarycodes = StringUtil.FromListString(strLibraryCodeList);
                foreach (string strLibraryCode in librarycodes)
                {
                    XmlNode node = root.SelectSingleNode("library[@code='" + strLibraryCode + "']");
                    if (node != null)
                    {
                        node.ParentNode.RemoveChild(node);
                    }
                }
            }

            // ��һ��<item>����
            {

            }

            // һ��һ��<library>Ԫ�صز���
            {
                List<string> librarycodes = StringUtil.FromListString(strLibraryCodeList);
                foreach (string strLibraryCode in librarycodes)
                {
                    XmlNodeList nodes = source_dom.DocumentElement.SelectNodes("library[@code='" + strLibraryCode + "']");
                    foreach (XmlNode node in nodes)
                    {
                        XmlNode new_node = this.LibraryCfgDom.CreateElement("library");
                        root.AppendChild(new_node);
                        DomUtil.SetAttr(new_node, "code", strLibraryCode);
                        new_node.InnerXml = node.InnerXml;
                    }
                }
                this.Changed = true;
            }

            return 0;
        }

        // ���չݴ����б�����<locationTypes>�ڵ��ʵ�Ƭ��
        public int GetLocationTypesXml(
            string strLibraryCodeList,
            out string strValue,
            out string strError)
        {
            strError = "";
            strValue = "";
#if NO
            XmlNode root = app.LibraryCfgDom.DocumentElement.SelectSingleNode("locationTypes"); // 0.02ǰΪlocationtypes
            if (root == null)
            {
                nRet = 0;
                goto END1;
            }

            strValue = root.InnerXml;
#endif
            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == true)
            {
                XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("locationTypes"); // 0.02ǰΪlocationtypes
                if (root == null)
                    return 0;
                strValue = root.InnerXml;

                return 0;
            }

            XmlDocument domNew = new XmlDocument();
            domNew.LoadXml("<locationTypes />");

            List<string> librarycodes = StringUtil.FromListString(strLibraryCodeList);
            foreach (string strLibraryCode in librarycodes)
            {
                string strXPath = "//locationTypes/library[@code='" + strLibraryCode + "']";
                XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes(strXPath);

                foreach (XmlNode node in nodes)
                {
                    XmlDocumentFragment fragment = domNew.CreateDocumentFragment();
                    fragment.InnerXml = node.OuterXml;

                    domNew.DocumentElement.AppendChild(fragment);
                }
            }

#if NO
            // ������ǰ��ϰ�ߡ��ѵ�һ����<item>Ԫ��Ҳ����
            if (string.IsNullOrEmpty(strLibraryCodeList) == true
                || strLibraryCodeList == "*")
            {
                string strXPath = "//locationTypes/item";
                XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes(strXPath);
                foreach (XmlNode node in nodes)
                {
                    XmlDocumentFragment fragment = domNew.CreateDocumentFragment();
                    fragment.InnerXml = node.OuterXml;

                    domNew.DocumentElement.AppendChild(fragment);
                }
            }
#endif

            strValue = domNew.DocumentElement.InnerXml;
            return 0;
        }

        // ��ùݲصص������б�
        // parameters:
        //      strLibraryCode  һ��ͼ��ݴ���
        //      bOnlyCanBorrow  �ǽ����г�canborrow����Ϊ'yes'��<item>����
        //`return:
        //      ����Ĺݲصص����ַ������顣��ν���⣬���ǡ��ݴ���/�ص������еĵص�������
        public List<string> GetLocationTypes(string strLibraryCode,
            bool bOnlyCanBorrow)
        {
            List<string> result = new List<string>();
            string strXPath = "";
            if (bOnlyCanBorrow == true)
                strXPath = "//locationTypes/library[@code='"+strLibraryCode+"']/item[@canborrow='yes']";
            else
                strXPath = "//locationTypes/library[@code='" + strLibraryCode + "']/item";

            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes(strXPath);

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                result.Add(node.InnerText);
            }

            // ����ԭ����ϰ�ߡ��ҵ���Щ������<library>Ԫ�غ����<item>Ԫ��
            if (string.IsNullOrEmpty(strLibraryCode) == true)
            {
                strXPath = "";
                if (bOnlyCanBorrow == true)
                    strXPath = "//locationTypes/item[@canborrow='yes'][count(ancestor::library) = 0]";
                else
                    strXPath = "//locationTypes/item[count(ancestor::library) = 0]";
                nodes = this.LibraryCfgDom.DocumentElement.SelectNodes(strXPath);
                foreach (XmlNode node in nodes)
                {
                    result.Add(node.InnerText);
                }
            }

            return result;
        }

        ResourceManager m_rm = null;

        ResourceManager GetRm()
        {
            if (this.m_rm != null)
                return this.m_rm;

            this.m_rm = new ResourceManager("DigitalPlatform.LibraryServer.res.LibraryApplication",
                typeof(LibraryApplication).Module.Assembly);

            return this.m_rm;
        }

        public string GetString(string strID)
        {
            CultureInfo ci = new CultureInfo(Thread.CurrentThread.CurrentUICulture.Name);

            // TODO: ����׳��쳣����Ҫ����ȡzh-cn���ַ��������߷���һ��������ַ���
            try
            {

                string s = GetRm().GetString(strID, ci);
                if (String.IsNullOrEmpty(s) == true)
                    return strID;
                return s;
            }
            catch (Exception /*ex*/)
            {
                return strID + " �� " + Thread.CurrentThread.CurrentUICulture.Name + " ��û���ҵ���Ӧ����Դ��";
            }
        }

        // ��鳬�������
        // return:
        //      -1  ���ݸ�ʽ����
        //      0   û�з��ֳ���    strErrorҲ��δ�����ڵ���ʾ��Ϣ
        //      1   ���ֳ���   strError������ʾ��Ϣ
        public int CheckPeriod(
            Calendar calendar,
            string strBorrowDate,
            string strPeriod,
            out string strError)
        {
            long lOver = 0;
            string strPeriodUnit = "";

            return CheckPeriod(
                calendar,
                strBorrowDate,
                strPeriod,
                out lOver,
                out strPeriodUnit,
                out strError);
        }


        // ��鳬��������������ɼ�����Ϣ����ģ�����ڽ���ǰ�����м�飨���������ڻ��飩��
        // return:
        //      -1  ���ݸ�ʽ����
        //      0   û�з��ֳ���
        //      1   ���ֳ���   strError������ʾ��Ϣ
        //      2   �Ѿ��ڿ������ڣ������׳��� 2009/3/13 new add
        public int CheckPeriod(
            Calendar calendar,
            string strBorrowDate,
            string strPeriod,
            out long lOver,
            out string strPeriodUnit,
            out string strError)
        {
            DateTime borrowdate;
            lOver = 0;
            strPeriodUnit = "";

            LibraryApplication app = this;

            try
            {
                borrowdate = DateTimeUtil.FromRfc1123DateTimeString(strBorrowDate);
            }
            catch
            {
                // text-level: �ڲ�����
                strError = string.Format(this.GetString("��������ֵs��ʽ����"), // "��������ֵ '{0}' ��ʽ����"
                    strBorrowDate);

                // "��������ֵ '" + strBorrowDate + "' ��ʽ����";
                return -1;
            }

            // ��������ֵ
            // string strPeriodUnit = "";
            long lPeriodValue = 0;

            int nRet = ParsePeriodUnit(strPeriod,
                out lPeriodValue,
                out strPeriodUnit,
                out strError);
            if (nRet == -1)
            {
                // text-level: �ڲ�����
                strError = string.Format(this.GetString("��������ֵs��ʽ����s"),    // "�������� ֵ '{0}' ��ʽ����: {1}" 
                    strPeriod,
                    strError);
                    // "�������� ֵ '" + strPeriod + "' ��ʽ����: " + strError;
                return -1;
            }

            DateTime timeEnd = DateTime.MinValue;   // �����������
            DateTime nextWorkingDay = DateTime.MinValue;   // ��������������������һ���ǹ������ϣ���ô��������һ��������

            // ���㻹������
            // parameters:
            //      calendar    �������������Ϊnull����ʾ���������зǹ������жϡ�
            // return:
            //      -1  ����
            //      0   �ɹ���timeEnd�ڹ����շ�Χ�ڡ�
            //      1   �ɹ���timeEnd�����ڷǹ����ա�nextWorkingDay�Ѿ���������һ�������յ�ʱ��
            nRet = GetReturnDay(
                calendar,
                borrowdate,
                lPeriodValue,
                strPeriodUnit,
                out timeEnd,
                out nextWorkingDay,
                out strError);
            if (nRet == -1)
            {
                // text-level: �ڲ�����
                strError = "���㻹��ʱ����̷�������: " + strError;
                return -1;
            }
            bool bEndInNonWorkingDay = false;
            if (nRet == 1)
            {
                // now�ڷǹ�����
                bEndInNonWorkingDay = true;
            }

            DateTime now_rounded = app.Clock.UtcNow;  //  ����

            // ���滯ʱ��
            nRet = RoundTime(strPeriodUnit,
                ref now_rounded,
                out strError);
            if (nRet == -1)
                return -1;

            TimeSpan delta = now_rounded - timeEnd;

            long lDelta = 0;
            long lDelta1 = 0;   // У�������ǹ����գ���Ĳ��

            nRet = ParseTimeSpan(
                delta,
                strPeriodUnit,
                out lDelta,
                out strError);
            if (nRet == -1)
                return -1;

            TimeSpan delta1 = new TimeSpan(0);
            if (bEndInNonWorkingDay == true)
            {
                delta1 = now_rounded - nextWorkingDay;

                nRet = ParseTimeSpan(
    delta1,
    strPeriodUnit,
    out lDelta1,
    out strError);
                if (nRet == -1)
                    return -1;
            }
            else
            {
                delta1 = delta;
                lDelta1 = lDelta;
            }


            strError = "";

            if (lDelta1 > 0)
            {
                if (bEndInNonWorkingDay == true)
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("�ѳ����������޶�����"), // "�ѳ����������� ({0}) {1} {2}��",
                        timeEnd.ToLongDateString(),
                        Convert.ToString(lDelta1),
                        GetDisplayTimeUnitLang(strPeriodUnit));
                    // �����Ѿ����ڣ����һ���ǲ����ڷǹ����վ�û�б�Ҫ������

                    // "�ѳ����������� (" + timeEnd.ToLongDateString() + ") " + Convert.ToString(lDelta1) + GetDisplayTimeUnit(strPeriodUnit) + "��";
                    lOver = lDelta1;    // 2009/8/5 new add
                    return 1;
                }
                else
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("�ѳ����������޶�����"), // "�ѳ����������� ({0}) {1} {2}��",
                        timeEnd.ToLongDateString(),
                        Convert.ToString(lDelta),
                        GetDisplayTimeUnitLang(strPeriodUnit));
                        
                        // "�ѳ����������� (" + timeEnd.ToLongDateString() + ") " + Convert.ToString(lDelta) + GetDisplayTimeUnit(strPeriodUnit) + "��";
                    lOver = lDelta;    // 2009/8/5 new add
                    return 1;
                }
            }

            if (lDelta == 0 || lDelta1 == 0)
            {
                if (strPeriodUnit == "day")
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("������ǻ�����������"), // "������ǻ����������� ({0})��"
                        timeEnd.ToLongDateString());
                        // "������ǻ����������� (" + timeEnd.ToLongDateString() + ")��";
                }
                else if (strPeriodUnit == "hour")
                {
                    // text-level: �û���ʾ
                    strError += this.GetString("��ǰ���Сʱ���ǻ�����������");
                        // "��ǰ���Сʱ���ǻ����������ޡ�";
                }
                else
                {
                    // text-level: �û���ʾ
                    strError += this.GetString("���ھ��ǻ�����������");
                        // "���ھ��ǻ����������ޡ�";
                }

                if (bEndInNonWorkingDay && lDelta1 < 0)
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("������"),
                        calendar.Name,
                        now_rounded.ToLongDateString(),
                        nextWorkingDay.ToLongDateString());
                    // "������ {0} ��ʾ������({1})�Ƿǹ����գ��������������һ��������({2})ȥͼ��ݻ��顣"

                    // "������ '" + calendar.Name + "' ��ʾ������(" + now_rounded.ToLongDateString() + ")�Ƿǹ����գ��������������һ��������(" + nextWorkingDay.ToLongDateString() + ")ȥͼ��ݻ��顣";
                }

                lOver = 0;    // 2009/8/5 new add
            }
            else
            {
                Debug.Assert(lDelta1 < 0, "");

                bool bOverdue = false;
                // �������Ѿ�����������ޣ����ǻ��ڿ���������
                if (lDelta > 0)
                {
                    Debug.Assert(bEndInNonWorkingDay == true, "");

                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("���ѳ�����������"),
                        timeEnd.ToLongDateString(),
                        Convert.ToString(lDelta),
                        GetDisplayTimeUnitLang(strPeriodUnit));
                        
                        // "���ѳ����������� ({0}) {1}{2}����";

                        // "���ѳ����������� (" + timeEnd.ToLongDateString() + ") " + Convert.ToString(lDelta) + GetDisplayTimeUnit(strPeriodUnit) + "����";
                    bOverdue = true;

                    lOver = lDelta;    // 2009/8/5 new add
                }
                else
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("��������޻���"),
                        timeEnd.ToLongDateString(),
                        Convert.ToString(-lDelta),  // lDelta1 BUG!!!
                        GetDisplayTimeUnitLang(strPeriodUnit));
                    // "��������� ({0}) ���� {1}{2}��";

                        // "��������� (" + timeEnd.ToLongDateString() + ") ���� " + Convert.ToString(-lDelta1) + GetDisplayTimeUnit(strPeriodUnit) + "��";

                    lOver = lDelta1;    // 2009/8/5 new add
                }

                if (bEndInNonWorkingDay == true)
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("���ݵ�֪"),
                        calendar.Name,
                        timeEnd.ToLongDateString(),
                        nextWorkingDay.ToLongDateString());
                    // "���� '{0}' ��֪�������ֹ�� ({1}) ǡ��ͼ��ݷǹ����գ�������ѡ������ڽ�ֹ�պ�ĵ�һ�������� ({2}) ȥͼ��ݻ��顣";

                        // "���� '" + calendar.Name + "' ��֪�������ֹ�� (" + timeEnd.ToLongDateString() + ") ǡ��ͼ��ݷǹ����գ�������ѡ������ڽ�ֹ�պ�ĵ�һ�������� (" + nextWorkingDay.ToLongDateString() + ") ȥͼ��ݻ��顣";
                }

                if (bOverdue == true)
                {
                    strError += "";
                    return 2;
                }
            }

            return 0;
        }

        // ��û�������
        // return:
        //      -1  ���ݸ�ʽ����
        //      0   û�з��ֳ���
        //      1   ���ֳ���   strError������ʾ��Ϣ
        //      2   �Ѿ��ڿ������ڣ������׳��� 
        public int GetReturningTime(
            Calendar calendar,
            string strBorrowDate,
            string strPeriod,
            out DateTime timeReturning,
            out DateTime timeNextWorkingDay,
            out long lOver,
            out string strPeriodUnit,
            out string strError)
        {
            DateTime borrowdate;
            lOver = 0;
            strPeriodUnit = "";

            timeReturning = DateTime.MinValue;   // �����������
            timeNextWorkingDay = DateTime.MinValue;   // ��������������������һ���ǹ������ϣ���ô��������һ��������


            LibraryApplication app = this;

            try
            {
                // ���Ŀ�ʼ�գ�GMTʱ��
                borrowdate = DateTimeUtil.FromRfc1123DateTimeString(strBorrowDate);
            }
            catch
            {
                // text-level: �ڲ�����
                strError = string.Format(this.GetString("��������ֵs��ʽ����"), // "��������ֵ '{0}' ��ʽ����"
                    strBorrowDate);

                // "��������ֵ '" + strBorrowDate + "' ��ʽ����";
                return -1;
            }

            // ��������ֵ
            // string strPeriodUnit = "";
            long lPeriodValue = 0;

            int nRet = ParsePeriodUnit(strPeriod,
                out lPeriodValue,
                out strPeriodUnit,
                out strError);
            if (nRet == -1)
            {
                // text-level: �ڲ�����
                strError = string.Format(this.GetString("��������ֵs��ʽ����s"),    // "�������� ֵ '{0}' ��ʽ����: {1}" 
                    strPeriod,
                    strError);
                // "�������� ֵ '" + strPeriod + "' ��ʽ����: " + strError;
                return -1;
            }


            // ���㻹������
            // parameters:
            //      calendar    �������������Ϊnull����ʾ���������зǹ������жϡ�
            // return:
            //      -1  ����
            //      0   �ɹ���timeEnd�ڹ����շ�Χ�ڡ�
            //      1   �ɹ���timeEnd�����ڷǹ����ա�nextWorkingDay�Ѿ���������һ�������յ�ʱ��
            nRet = GetReturnDay(
                calendar,
                borrowdate,
                lPeriodValue,
                strPeriodUnit,
                out timeReturning,
                out timeNextWorkingDay,
                out strError);
            if (nRet == -1)
            {
                // text-level: �ڲ�����
                strError = "���㻹��ʱ����̷�������: " + strError;
                return -1;
            }
            bool bEndInNonWorkingDay = false;
            if (nRet == 1)
            {
                // now�ڷǹ�����
                bEndInNonWorkingDay = true;
            }

            DateTime now_rounded = app.Clock.UtcNow;  //  ����

            // ���滯ʱ��
            nRet = RoundTime(strPeriodUnit,
                ref now_rounded,
                out strError);
            if (nRet == -1)
                return -1;

            TimeSpan delta = now_rounded - timeReturning;

            long lDelta = 0;
            long lDelta1 = 0;   // У�������ǹ����գ���Ĳ��

            nRet = ParseTimeSpan(
                delta,
                strPeriodUnit,
                out lDelta,
                out strError);
            if (nRet == -1)
                return -1;

            TimeSpan delta1 = new TimeSpan(0);
            if (bEndInNonWorkingDay == true)
            {
                delta1 = now_rounded - timeNextWorkingDay;

                nRet = ParseTimeSpan(
    delta1,
    strPeriodUnit,
    out lDelta1,
    out strError);
                if (nRet == -1)
                    return -1;
            }
            else
            {
                delta1 = delta;
                lDelta1 = lDelta;
            }


            strError = "";

            if (lDelta1 > 0)
            {
                if (bEndInNonWorkingDay == true)
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("�ѳ����������޶�����"), // "�ѳ����������� ({0}) {1} {2}��",
                        timeReturning.ToLongDateString(),
                        Convert.ToString(lDelta1),
                        GetDisplayTimeUnitLang(strPeriodUnit));
                    // �����Ѿ����ڣ����һ���ǲ����ڷǹ����վ�û�б�Ҫ������

                    // "�ѳ����������� (" + timeEnd.ToLongDateString() + ") " + Convert.ToString(lDelta1) + GetDisplayTimeUnit(strPeriodUnit) + "��";
                    lOver = lDelta1;    // 2009/8/5 new add
                    return 1;
                }
                else
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("�ѳ����������޶�����"), // "�ѳ����������� ({0}) {1} {2}��",
                        timeReturning.ToLongDateString(),
                        Convert.ToString(lDelta),
                        GetDisplayTimeUnitLang(strPeriodUnit));

                    // "�ѳ����������� (" + timeEnd.ToLongDateString() + ") " + Convert.ToString(lDelta) + GetDisplayTimeUnit(strPeriodUnit) + "��";
                    lOver = lDelta;    // 2009/8/5 new add
                    return 1;
                }
            }

            if (lDelta == 0 || lDelta1 == 0)
            {
                if (strPeriodUnit == "day")
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("������ǻ�����������"), // "������ǻ����������� ({0})��"
                        timeReturning.ToLongDateString());
                    // "������ǻ����������� (" + timeEnd.ToLongDateString() + ")��";
                }
                else if (strPeriodUnit == "hour")
                {
                    // text-level: �û���ʾ
                    strError += this.GetString("��ǰ���Сʱ���ǻ�����������");
                    // "��ǰ���Сʱ���ǻ����������ޡ�";
                }
                else
                {
                    // text-level: �û���ʾ
                    strError += this.GetString("���ھ��ǻ�����������");
                    // "���ھ��ǻ����������ޡ�";
                }

                if (bEndInNonWorkingDay && lDelta1 < 0)
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("������"),
                        calendar.Name,
                        now_rounded.ToLongDateString(),
                        timeNextWorkingDay.ToLongDateString());
                    // "������ {0} ��ʾ������({1})�Ƿǹ����գ��������������һ��������({2})ȥͼ��ݻ��顣"

                    // "������ '" + calendar.Name + "' ��ʾ������(" + now_rounded.ToLongDateString() + ")�Ƿǹ����գ��������������һ��������(" + nextWorkingDay.ToLongDateString() + ")ȥͼ��ݻ��顣";
                }

                lOver = 0;    // 2009/8/5 new add
            }
            else
            {
                Debug.Assert(lDelta1 < 0, "");

                bool bOverdue = false;
                // �������Ѿ�����������ޣ����ǻ��ڿ���������
                if (lDelta > 0)
                {
                    Debug.Assert(bEndInNonWorkingDay == true, "");

                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("���ѳ�����������"),
                        timeReturning.ToLongDateString(),
                        Convert.ToString(lDelta),
                        GetDisplayTimeUnitLang(strPeriodUnit));

                    // "���ѳ����������� ({0}) {1}{2}����";

                    // "���ѳ����������� (" + timeEnd.ToLongDateString() + ") " + Convert.ToString(lDelta) + GetDisplayTimeUnit(strPeriodUnit) + "����";
                    bOverdue = true;

                    lOver = lDelta;    // 2009/8/5 new add
                }
                else
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("��������޻���"),
                        timeReturning.ToLongDateString(),
                        Convert.ToString(-lDelta1),
                        GetDisplayTimeUnitLang(strPeriodUnit));
                    // "��������� ({0}) ���� {1}{2}��";

                    // "��������� (" + timeEnd.ToLongDateString() + ") ���� " + Convert.ToString(-lDelta1) + GetDisplayTimeUnit(strPeriodUnit) + "��";

                    lOver = lDelta1;    // 2009/8/5 new add
                }

                if (bEndInNonWorkingDay == true)
                {
                    // text-level: �û���ʾ
                    strError += string.Format(this.GetString("���ݵ�֪"),
                        calendar.Name,
                        timeReturning.ToLongDateString(),
                        timeNextWorkingDay.ToLongDateString());
                    // "���� '{0}' ��֪�������ֹ�� ({1}) ǡ��ͼ��ݷǹ����գ�������ѡ������ڽ�ֹ�պ�ĵ�һ�������� ({2}) ȥͼ��ݻ��顣";

                    // "���� '" + calendar.Name + "' ��֪�������ֹ�� (" + timeEnd.ToLongDateString() + ") ǡ��ͼ��ݷǹ����գ�������ѡ������ڽ�ֹ�պ�ĵ�һ�������� (" + nextWorkingDay.ToLongDateString() + ") ȥͼ��ݻ��顣";
                }

                if (bOverdue == true)
                {
                    strError += "";
                    return 2;
                }
            }

            return 0;
        }

        // ���ÿ��֪ͨ�㣬���ص�ǰʱ���Ѿ��ﵽ���߳�����֪ͨ�����Щ������±�
        // return:
        //      -1  ���ݸ�ʽ����
        //      0   �ɹ�
        public int CheckNotifyPoint(
            Calendar calendar,
            string strBorrowDate,
            string strPeriod,
            string strNotifyDef,
            out List<int> indices,
            out string strError)
        {
            strError = "";

            indices = new List<int>();

            // long lOver = 0;
            string strPeriodUnit = "";

            DateTime borrowdate;

            LibraryApplication app = this;

            try
            {
                // ע�ⷵ�ص���GMTʱ��
                borrowdate = DateTimeUtil.FromRfc1123DateTimeString(strBorrowDate);
            }
            catch
            {
                // text-level: �ڲ�����
                strError = string.Format(this.GetString("��������ֵs��ʽ����"), // "��������ֵ '{0}' ��ʽ����"
                    strBorrowDate);

                // "��������ֵ '" + strBorrowDate + "' ��ʽ����";
                return -1;
            }

            // ��������ֵ
            // string strPeriodUnit = "";
            long lPeriodValue = 0;

            int nRet = ParsePeriodUnit(strPeriod,
                out lPeriodValue,
                out strPeriodUnit,
                out strError);
            if (nRet == -1)
            {
                // text-level: �ڲ�����
                strError = string.Format(this.GetString("��������ֵs��ʽ����s"),    // "�������� ֵ '{0}' ��ʽ����: {1}" 
                    strPeriod,
                    strError);
                // "�������� ֵ '" + strPeriod + "' ��ʽ����: " + strError;
                return -1;
            }

            DateTime timeEnd = DateTime.MinValue;   // �����������
            DateTime nextWorkingDay = DateTime.MinValue;   // ��������������������һ���ǹ������ϣ���ô��������һ��������

            // ���㻹������
            // parameters:
            //      calendar    �������������Ϊnull����ʾ���������зǹ������жϡ�
            // return:
            //      -1  ����
            //      0   �ɹ���timeEnd�ڹ����շ�Χ�ڡ�
            //      1   �ɹ���timeEnd�����ڷǹ����ա�nextWorkingDay�Ѿ���������һ�������յ�ʱ��
            nRet = GetReturnDay(
                calendar,
                borrowdate,
                lPeriodValue,
                strPeriodUnit,
                out timeEnd,
                out nextWorkingDay,
                out strError);
            if (nRet == -1)
            {
                // text-level: �ڲ�����
                strError = "���㻹��ʱ����̷�������: " + strError;
                return -1;
            }

#if NO
            bool bEndInNonWorkingDay = false;
            if (nRet == 1)
            {
                // now�ڷǹ�����
                bEndInNonWorkingDay = true;
            }
#endif
            DateTime now = this.Clock.UtcNow;

            // ���滯ʱ��
            nRet = RoundTime(strPeriodUnit,
                ref borrowdate,
                out strError);
            if (nRet == -1)
                return -1;
            nRet = RoundTime(strPeriodUnit,
    ref timeEnd,
    out strError);
            if (nRet == -1)
                return -1;
            nRet = RoundTime(strPeriodUnit,
    ref now,
    out strError);
            if (nRet == -1)
                return -1;

            string[] points = strNotifyDef.Split(new char[] { ',' });
            int index = 0;
            foreach (string strOnePoint in points)
            {
                // �۲쵱���ǲ��Ǵ��ڵ��ڼ���ʱ��
                // parameters:
                //      strCheckPoint   �����ļ��㶨�塣-1day,1hour,-19%,10%
                // return:
                //      -1  ����
                //      0   ������
                //      1   ����
                nRet = GetCheckPoint(borrowdate,
                    timeEnd,
                    now,
                    strOnePoint,
                    out strError);
                if (nRet == -1)
                {
                    strError = "����֪ͨ�����ַ��� '" + strNotifyDef + "' �� '" + strOnePoint + "' ���ָ�ʽ����: " + strError;
                    return -1;
                }

                if (nRet == 1)
                    indices.Add(index);

                index++;
            }

            return 0;
        }

        // �۲쵱���ǲ��Ǵ��ڵ��ڼ���ʱ��
        // parameters:
        //      start   ����ʱ��(GMTʱ��)������ǰӦ���Ѿ����ݻ�����λ���������滯
        //      end     Ӧ��ʱ��(GMTʱ��)������ǰӦ���Ѿ����ݻ�����λ���������滯
        //      now     ��ǰʱ��(GMTʱ��)������ǰӦ���Ѿ����ݻ�����λ���������滯
        //      strCheckPoint   �����ļ��㶨�塣-1day,1hour,-19%,10%
        // return:
        //      -1  ����
        //      0   ������
        //      1   ����
        static int GetCheckPoint(DateTime start,
            DateTime end,
            DateTime now,
            string strCheckPoint,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(strCheckPoint) == true)
            {
                strError = "strCheckPoint ֵ����Ϊ��";
                return -1;
            }

            bool bReverse = false;  // �Ƿ��ĩβ��ʼ�� ?
            DateTime point;
            string strValue = strCheckPoint.Trim();

            if (strValue[0] == '-')
            {
                strValue = strValue.Substring(1).Trim();
                bReverse = true;

                if (string.IsNullOrEmpty(strValue) == true)
                {
                    strError = "�����ұ߲���Ϊ��";
                    return -1;
                }
            }

            // �Ƿ�Ϊ�ٷֺ���ʽ?
            if (strValue[strValue.Length - 1] == '%')
            {
                strValue = strValue.Substring(0, strValue.Length - 1).Trim();
                if (string.IsNullOrEmpty(strValue) == true)
                {
                    strError = "�ٷֺ���߲���Ϊ��";
                    return -1;
                }

                // ��ֵ
                float v = 0;
                if (float.TryParse(strValue, out v) == false)
                {
                    strError = "���㶨�� '"+strCheckPoint+"' ��ʽ�������� '"+strValue+"' ����Ӧ��Ϊ��ֵ��̬";
                    return -1;
                }

                // �����ʱ���
                if (bReverse == true)
                    point = end - new TimeSpan((long)((end - start).Ticks * (v / 100))); 
                else
                    point = start + new TimeSpan((long)((end - start).Ticks * (v / 100)));

                // point �Ƿ���Ҫ���滯�� �����Ǵ�ʱ���߱�ʱ���������

                if (point <= start || point >= end)
                    return 0;

                if (now >= point)
                    return 1;

                return 0;
            }

            // ��������ֵ
            string strPeriodUnit = "";
            long lPeriodValue = 0;

            int nRet = ParsePeriodUnit(strValue,
                out lPeriodValue,
                out strPeriodUnit,
                out strError);
            if (nRet == -1)
                return -1;

            TimeSpan delta;

            if (strPeriodUnit == "day")
                delta = new TimeSpan((int)lPeriodValue, 0, 0, 0);
            else if (strPeriodUnit == "hour")
                delta = new TimeSpan((int)lPeriodValue, 0, 0);
            else
            {
                strError = "δ֪��ʱ�䵥λ '" + strPeriodUnit + "'";
                return -1;
            }

            // �����ʱ���
            if (bReverse == true)
                point = end - delta;
            else
                point = start + delta;

            if (point <= start || point >= end)
                return 0;

            if (now >= point)
                return 1;

            return 0;
        }

        // ��ʱ�䵥λ�任Ϊ�ɶ�����̬
        // ��ǰ�İ汾
        public static string GetDisplayTimeUnit(string strUnit)
        {
            if (strUnit == "day")
                return "��";
            if (strUnit == "hour")
                return "Сʱ";

            return strUnit; // �޷������
        }


        // ��ʱ�䵥λ�任Ϊ�ɶ�����̬
        // �°汾���ܹ��Զ���Ӧ��ǰ����
        public string GetDisplayTimeUnitLang(string strUnit)
        {
            if (strUnit == "day")
                return this.GetString("��");
            if (strUnit == "hour")
                return this.GetString("Сʱ");

            return strUnit; // �޷������
        }

        // �������ַ����е�ʱ�䵥λ�任Ϊ�ɶ�����̬
        // ������ص����°汾
        public string GetDisplayTimePeriodStringEx(string strText)
        {
            strText = strText.Replace("day", this.GetString("��"));

            return strText.Replace("hour", this.GetString("Сʱ"));
        }


        // �������ַ����е�ʱ�䵥λ�任Ϊ�ɶ�����̬
        // Ϊ�˼���ĳЩ�ɵĽű��������İ汾��������Ҫ���ˣ�������GetDisplayTimePeriodStringEx()
        public static string GetDisplayTimePeriodString(string strText)
        {
            strText = strText.Replace("day", "��");

            return strText.Replace("hour", "Сʱ");
        }

        // ����strPeriod�е�ʱ�䵥λ(day/hour)�����ر������ڻ���ʱ���ַ���
        // parameters:
        //      strPeriod   ԭʼ��ʽ��ʱ�䳤���ַ�����Ҳ����˵��ʱ�䵥λ����������أ���"day"��"hour"
        public static string LocalDateOrTime(string strTimeString, 
            string strPeriod)
        {
            string strError = "";
            long lValue = 0;
            string strUnit = "";
            int nRet = LibraryApplication.ParsePeriodUnit(strPeriod,
                        out lValue,
                        out strUnit,
                        out strError);
            if (nRet == -1)
                strUnit = "day";
            if (strUnit == "day")
                return DateTimeUtil.LocalDate(strTimeString);

            return DateTimeUtil.LocalTime(strTimeString);
        }

        // ����strPeriod�е�ʱ�䵥λ(day/hour)�����ر������ڻ���ʱ���ַ���
        // parameters:
        //      strPeriod   ԭʼ��ʽ��ʱ�䳤���ַ�����Ҳ����˵��ʱ�䵥λ����������أ���"day"��"hour"
        public static string LocalDateOrTime(DateTime time,
            string strPeriod)
        {
            string strError = "";
            long lValue = 0;
            string strUnit = "";
            int nRet = LibraryApplication.ParsePeriodUnit(strPeriod,
                        out lValue,
                        out strUnit,
                        out strError);
            if (nRet == -1)
                strUnit = "day";
            if (strUnit == "day")
                return time.ToString("d");  // ��ȷ����

            return time.ToString("g");  // ��ȷ�����ӡ�G��ȷ����
            // http://www.java2s.com/Tutorial/CSharp/0260__Date-Time/UsetheToStringmethodtoconvertaDateTimetoastringdDfFgGmrstTuUy.htm
        }

        // �����۸����
        // 2006/10/11
        public static int ParsePriceUnit(string strString,
            out string strPrefix,
            out double fValue,
            out string strPostfix,
            out string strError)
        {
            strPrefix = "";
            fValue = 0.0F;
            strPostfix = "";
            strError = "";

            strString = strString.Trim();

            if (String.IsNullOrEmpty(strString) == true)
            {
                strError = "�۸��ַ���Ϊ��";
                return -1;
            }

            string strValue = "";

            bool bInPrefix = true;

            for (int i = 0; i < strString.Length; i++)
            {
                if ((strString[i] >= '0' && strString[i] <= '9')
                    || strString[i] == '.')
                {
                    bInPrefix = false;
                    strValue += strString[i];
                }
                else
                {
                    if (bInPrefix == true)
                        strPrefix += strString[i];
                    else
                    {
                        strPostfix = strString.Substring(i).Trim();
                        break;
                    }
                }
            }

            // ��strValueת��Ϊ����
            try
            {
                fValue = Convert.ToDouble(strValue);
            }
            catch (Exception)
            {
                strError = "�۸�������ֲ���'" + strValue + "'��ʽ���Ϸ�";
                return -1;
            }

            /*
            if (String.IsNullOrEmpty(strUnit) == true)
                strUnit = "CNY";   // ȱʡ��λΪ �����Ԫ

            strUnit = strUnit.ToUpper();    // ͳһת��Ϊ��д
             * */

            return 0;
        }

        // �������޲���
        public static int ParsePeriodUnit(string strPeriod,
            out long lValue,
            out string strUnit,
            out string strError)
        {
            lValue = 0;
            strUnit = "";
            strError = "";

            strPeriod = strPeriod.Trim();

            if (String.IsNullOrEmpty(strPeriod) == true)
            {
                strError = "�����ַ���Ϊ��";
                return -1;
            }

            string strValue = "";


            for (int i = 0; i < strPeriod.Length; i++)
            {
                if (strPeriod[i] >= '0' && strPeriod[i] <= '9')
                {
                    strValue += strPeriod[i];
                }
                else
                {
                    strUnit = strPeriod.Substring(i).Trim();
                    break;
                }
            }

            // ��strValueת��Ϊ����
            try
            {
                lValue = Convert.ToInt64(strValue);
            }
            catch (Exception)
            {
                strError = "���޲������ֲ���'" + strValue + "'��ʽ���Ϸ�";
                return -1;
            }

            if (String.IsNullOrEmpty(strUnit) == true)
                strUnit = "day";   // ȱʡ��λΪ"��"

            strUnit = strUnit.ToLower();    // ͳһת��ΪСд

            return 0;
        }

        
        // ����ʱ�䵥λ,��ʱ��ֵ��ͷȥ��,���滯,���ں��������
        /// <summary>
        /// ����ʱ�������λ��ȥ����ͷ�����ڻ������(����λ��)��
        /// �㷨����ת��Ϊ����ʱ�䣬ȥ����ͷ����ת���� GMT ʱ��
        /// </summary>
        /// <param name="strUnit">ʱ�䵥λ��day/hour֮һ�����Ϊ�գ��൱�� day</param>
        /// <param name="time">Ҫ�����ʱ�䡣Ϊ GMT ʱ��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public static int RoundTime(string strUnit,
            ref DateTime time,
            out string strError)
        {
            strError = "";

            time = time.ToLocalTime();
            if (strUnit == "day" || string.IsNullOrEmpty(strUnit) == true)
            {
                time = new DateTime(time.Year, time.Month, time.Day,
                    12, 0, 0, 0);
            }
            else if (strUnit == "hour")
            {
                time = new DateTime(time.Year, time.Month, time.Day,
                    time.Hour, 0, 0, 0);
            }
            else
            {
                strError = "δ֪��ʱ�䵥λ '" + strUnit + "'";
                return -1;
            }
            time = time.ToUniversalTime();

            return 0;
        }

        public static int ParseTimeSpan(
            TimeSpan delta,
            string strUnit,
            out long lValue,
            out string strError)
        {
            lValue = 0;
            strError = "";

            if (strUnit == "day")
                lValue = (long)delta.TotalDays;
            else if (strUnit == "hour")
                lValue = (long)delta.TotalHours;
            else
            {
                strError = "����ʶ���ʱ�䵥λ '" + strUnit + "'";
                return -1;
            }

            return 0;
        }

        // ����TimeSpan
        public static int BuildTimeSpan(
            long lPeriod,
            string strUnit,
            out TimeSpan delta,
            out string strError)
        {
            strError = "";

            if (strUnit == "day")
                delta = new TimeSpan((int)lPeriod, 0, 0, 0);
            else if (strUnit == "hour")
                delta = new TimeSpan((int)lPeriod, 0, 0);
            else
            {
                delta = new TimeSpan(0);
                strError = "δ֪��ʱ�䵥λ '" + strUnit + "'";
                return -1;
            }

            return 0;
        }

        // ���ԤԼ����ĩ��ʱ��
        // �м�Ҫ�ų����зǹ�����
        public static int GetOverTime(
            Calendar calendar,
            DateTime timeStart,
            long lPeriod,
            string strUnit,
            out DateTime timeEnd,
            out string strError)
        {
            strError = "";
            timeEnd = DateTime.MinValue;

            // ���滯ʱ��
            int nRet = RoundTime(strUnit,
                ref timeStart,
                out strError);
            if (nRet == -1)
                return -1;

            if (calendar == null)
            {
                TimeSpan delta;

                if (strUnit == "day")
                    delta = new TimeSpan((int)lPeriod, 0, 0, 0);
                else if (strUnit == "hour")
                    delta = new TimeSpan((int)lPeriod, 0, 0);
                else
                {
                    strError = "δ֪��ʱ�䵥λ '" + strUnit + "'";
                    return -1;
                }

                timeEnd = timeStart + delta;

                // ���滯ʱ��
                nRet = RoundTime(strUnit,
                    ref timeEnd,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            else
            {
                TimeSpan delta;

                if (strUnit == "day")
                    delta = new TimeSpan((int)lPeriod, 0, 0, 0);
                else if (strUnit == "hour")
                    delta = new TimeSpan((int)lPeriod, 0, 0);
                else
                {
                    strError = "δ֪��ʱ�䵥λ '" + strUnit + "'";
                    return -1;
                }

                timeEnd = calendar.GetEndTime(timeStart,
                    delta);

                // ���滯ʱ��
                nRet = RoundTime(strUnit,
                    ref timeEnd,
                    out strError);
                if (nRet == -1)
                    return -1;

            }


            return 0;
        }

        // ���㻹������
        // parameters:
        //      calendar    �������������Ϊnull����ʾ���������зǹ������жϡ�
        //      timeStart   ���Ŀ�ʼʱ�䡣GMTʱ��
        //      timeEnd     ����Ӧ���ص����ʱ�䡣GMTʱ��
        // return:
        //      -1  ����
        //      0   �ɹ���timeEnd�ڹ����շ�Χ�ڡ�
        //      1   �ɹ���timeEnd�����ڷǹ����ա�nextWorkingDay�Ѿ���������һ�������յ�ʱ��
        public static int GetReturnDay(
            Calendar calendar,
            DateTime timeStart,
            long lPeriod,
            string strUnit,
            out DateTime timeEnd,
            out DateTime nextWorkingDay,
            out string strError)
        {
            strError = "";
            timeEnd = DateTime.MinValue;
            nextWorkingDay = DateTime.MinValue;

            // ���滯ʱ��
            int nRet = RoundTime(strUnit,
                ref timeStart,
                out strError);
            if (nRet == -1)
                return -1;

            TimeSpan delta;

            if (strUnit == "day")
                delta = new TimeSpan((int)lPeriod, 0, 0, 0);
            else if (strUnit == "hour")
                delta = new TimeSpan((int)lPeriod, 0, 0);
            else
            {
                strError = "δ֪��ʱ�䵥λ '" + strUnit + "'";
                return -1;
            }

            timeEnd = timeStart + delta;

            // ���滯ʱ��
            nRet = RoundTime(strUnit,
                ref timeEnd,
                out strError);
            if (nRet == -1)
                return -1;

            bool bInNonWorkingDay = false;

            // ����ĩ���Ƿ������ڷǹ�����
            if (calendar != null)
            {
                bInNonWorkingDay = calendar.IsInNonWorkingDay(timeEnd,
                    out nextWorkingDay);
                nRet = RoundTime(strUnit,
    ref nextWorkingDay,
    out strError);
                if (nRet == -1)
                    return -1;
            }

            if (bInNonWorkingDay == true)
            {
                Debug.Assert(nextWorkingDay != DateTime.MinValue, "");
                return 1;
            }

            return 0;

        }

        // ����ʱ��֮��ľ���
        // parameters:
        //      calendar    �������������Ϊnull����ʾ���������зǹ������жϡ�
        // return:
        //      -1  ����
        //      0   �ɹ���timeEnd�ڹ����շ�Χ�ڡ�
        //      1   �ɹ���timeEnd�����ڷǹ����ա�nextWorkingDay�Ѿ���������һ�������յ�ʱ��
        public static int GetTimeDistance(
            Calendar calendar,
            string strUnit,
            DateTime timeStart,
            DateTime timeEnd,
            out long lValue,
            out DateTime nextWorkingDay,
            out string strError)
        {
            lValue = 0;
            strError = "";
            nextWorkingDay = DateTime.MinValue;


            int nRet = RoundTime(strUnit,
                ref timeStart,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = RoundTime(strUnit,
                ref timeEnd,
                out strError);
            if (nRet == -1)
                return -1;

            bool bInNonWorkingDay = false;

            // ����ĩ���Ƿ������ڷǹ�����
            if (calendar != null)
            {
                bInNonWorkingDay = calendar.IsInNonWorkingDay(timeEnd,
                    out nextWorkingDay);
                nRet = RoundTime(strUnit,
    ref nextWorkingDay,
    out strError);
                if (nRet == -1)
                    return -1;
            }

            TimeSpan delta;

            delta = timeEnd - timeStart;

            if (strUnit == "day")
            {
                lValue = (long)delta.TotalDays;
            }
            else if (strUnit == "hour")
            {
                lValue = (long)delta.TotalHours;
            }
            else
            {
                strError = "δ֪��ʱ�䵥λ '" + strUnit + "'";
                return -1;
            }

            if (bInNonWorkingDay == true)
            {
                Debug.Assert(nextWorkingDay != DateTime.MinValue, "");
                return 1;
            }

            return 0;
        }



        // ���һ��������Ƿ����б���
        static bool IsInBarcodeList(string strBarcode,
            string strBarcodeList)
        {
            string[] barcodes = strBarcodeList.Split(new char[] { ',' });
            for (int i = 0; i < barcodes.Length; i++)
            {
                string strPerBarcode = barcodes[i].Trim();
                if (String.IsNullOrEmpty(strPerBarcode) == true)
                    continue;

                if (strPerBarcode == strBarcode)
                    return true;
            }

            return false;
        }

        // Undoһ���ѽ��Ѽ�¼
        int UndoOneAmerce(SessionInfo sessioninfo,
            string strReaderBarcode,
            string strAmercedItemId,
            out string strReaderXml,
            out string strError)
        {
            strError = "";
            strReaderXml = "";

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            long lRet = 0;
            int nRet = 0;

            string strFrom = "ID";
            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(this.AmerceDbName + ":" + strFrom)       // 2007/9/14 new add
                + "'><item><word>"
                + strAmercedItemId + "</word><match>" + "exact" + "</match><relation>=</relation><dataType>string</dataType><maxCount>100</maxCount></item><lang>" + "zh" + "</lang></target>";

            lRet = channel.DoSearch(strQueryXml,
                "amerced",
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
            {
                strError = "����IDΪ '" + strAmercedItemId + "' ���Ѹ�ΥԼ���¼����: " + strError;
                return -1;
            }

            if (lRet == 0)
            {
                strError = "û���ҵ�IDΪ '" + strAmercedItemId + "' ���Ѹ�ΥԼ���¼";
                return -1;
            }

            List<string> aPath = null;
            lRet = channel.DoGetSearchResult("amerced",
                100,
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
            {
                strError = "����IDΪ '" + strAmercedItemId + "' ���Ѹ�ΥԼ���¼����ȡ�����ʽ�׶γ���: " + strError;
                return -1;
            }

            if (lRet == 0)
            {
                strError = "����IDΪ '" + strAmercedItemId + "' ���Ѹ�ΥԼ���¼���Ѽ������У����ǻ�ȡ�����ʽû���ҵ�";
                return -1;
            }

            if (aPath.Count == 0)
            {
                strError = "����IDΪ '" + strAmercedItemId + "' ���Ѹ�ΥԼ���¼���Ѽ������У����ǻ�ȡ�����ʽû���ҵ�";
                return -1;
            }

            if (aPath.Count > 1)
            {
                strError = "IDΪ '" + strAmercedItemId + "' ���Ѹ�ΥԼ���¼��������������ϵͳ����Ա��ʱ�����˴���";
                return -1;
            }

            string strAmercedRecPath = aPath[0];

            string strMetaData = "";
            byte[] amerced_timestamp = null;
            string strOutputPath = "";
            string strAmercedXml = "";

            lRet = channel.GetRes(strAmercedRecPath,
                out strAmercedXml,
                out strMetaData,
                out amerced_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                strError = "��ȡ�Ѹ�ΥԼ���¼ '" + strAmercedRecPath + "' ʱ����: " + strError;
                return -1;
            }

            string strOverdueString = "";
            string strOutputReaderBarcode = "";

            // ��ΥԼ���¼��ʽת��Ϊ���߼�¼�е�<overdue>Ԫ�ظ�ʽ
            // return:
            //      -1  error
            //      0   strAmercedXml��<state>Ԫ�ص�ֵΪ*��*"settlemented"
            //      1   strAmercedXml��<state>Ԫ�ص�ֵΪ"settlemented"
            nRet = ConvertAmerceRecordToOverdueString(strAmercedXml,
                out strOutputReaderBarcode,
                out strOverdueString,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 1)
            {
                strError = "IDΪ " + strAmercedItemId + " (·��Ϊ '" + strOutputPath + "' ) ��ΥԼ����¼��״̬Ϊ �ѽ���(settlemented)�����ܳ��ؽ��Ѳ���";
                return -1;
            }

            // ���strReaderBarcode����ֵ�ǿգ���Ҫ���һ�¼����������Ѹ�ΥԼ���¼�Ƿ���������������
            if (String.IsNullOrEmpty(strReaderBarcode) == false
                && strReaderBarcode != strOutputReaderBarcode)
            {
                strError = "IDΪ '" + strAmercedItemId + "' ���Ѹ�ΥԼ���¼��������������ָ���Ķ��� '" + strReaderBarcode + "'������������һ���� '" + strOutputReaderBarcode + "'";
                return -1;
            }


            // �Ӷ��߼�¼��
            this.ReaderLocks.LockForWrite(strReaderBarcode);

            try
            {
                // ������߼�¼
                strReaderXml = "";
                string strOutputReaderRecPath = "";
                byte[] reader_timestamp = null;

                nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
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

                string strLibraryCode = "";
                // �������߼�¼�������Ķ��߿�Ĺݴ��룬�Ƿ񱻵�ǰ�û���Ͻ
                if (String.IsNullOrEmpty(strOutputReaderRecPath) == false)
                {
                    // ��鵱ǰ�������Ƿ��Ͻ������߿�
                    // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                    if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
            sessioninfo.LibraryCodeList,
            out strLibraryCode) == false)
                    {
                        strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �����Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                        goto ERROR1;
                    }
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

                // ׼����־DOM
                XmlDocument domOperLog = new XmlDocument();
                domOperLog.LoadXml("<root />");
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "libraryCode",
                    strLibraryCode);    // �������ڵĹݴ���
                DomUtil.SetElementText(domOperLog.DocumentElement, "operation",
                    "amerce");

                bool bReaderDomChanged = false;

                // �޸Ķ��߼�¼
                // ��������Ϣ
                if (String.IsNullOrEmpty(strOverdueString) != true)
                {
                    XmlDocumentFragment fragment = readerdom.CreateDocumentFragment();
                    fragment.InnerXml = strOverdueString;

                    // �����������Ƿ���overduesԪ��
                    XmlNode root = readerdom.DocumentElement.SelectSingleNode("overdues");
                    if (root == null)
                    {
                        root = readerdom.CreateElement("overdues");
                        readerdom.DocumentElement.AppendChild(root);
                    }


                    // 2008/11/11 new add
                    // undo��Ѻ��
                    XmlNode node_added = root.AppendChild(fragment);
                    bReaderDomChanged = true;

                    Debug.Assert(node_added != null, "");
                    string strReason = DomUtil.GetAttr(node_added, "reason");
                    if (strReason == "Ѻ��")
                    {
                        string strPrice = "";

                        strPrice = DomUtil.GetAttr(node_added, "newPrice");
                        if (String.IsNullOrEmpty(strPrice) == true)
                            strPrice = DomUtil.GetAttr(node_added, "price");
                        else
                        {
                            Debug.Assert(strPrice.IndexOf('%') == -1, "��newPrice������ȡ�����ļ۸��ַ��������ܰ���%����");
                        }

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
                            bReaderDomChanged = true;
                        }
                    }
                }



                if (bReaderDomChanged == true)
                {
                    byte[] output_timestamp = null;

                    strReaderXml = readerdom.OuterXml;
                    // Ұ��д��
                    lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                        strReaderXml,
                        false,
                        "content,ignorechecktimestamp", // ?????
                        reader_timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    int nRedoDeleteCount = 0;
                REDO_DELETE:
                    // ɾ���Ѹ�ΥԼ���¼
                    lRet = channel.DoDeleteRes(strAmercedRecPath,
                        amerced_timestamp,
                        out output_timestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                            && nRedoDeleteCount < 10)
                        {
                            nRedoDeleteCount++;
                            amerced_timestamp = output_timestamp;
                            goto REDO_DELETE;
                        }
                        strError = "ɾ���Ѹ�ΥԼ���¼ '" + strAmercedRecPath + "' ʧ��: " + strError;
                        this.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    // ���嶯��
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "action", "undo");

                    // id list
                    /*
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "idList", strAmercedItemId);
                     * */
                    AmerceItem[] amerce_items = new AmerceItem[1];
                    amerce_items[0] = new AmerceItem();
                    amerce_items[0].ID = strAmercedItemId;
                    WriteAmerceItemList(domOperLog,
                        amerce_items);


                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "readerBarcode", strReaderBarcode);

                    /*
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "amerceItemID", strAmercedItemId);
                     */

                    // ɾ������ΥԼ���¼
                    XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                        "amerceRecord", strAmercedXml);
                    DomUtil.SetAttr(node, "recPath", strAmercedRecPath);

                    // ���µĶ��߼�¼
                    node = DomUtil.SetElementText(domOperLog.DocumentElement,
                        "readerRecord", strReaderXml);
                    DomUtil.SetAttr(node, "recPath", strOutputReaderRecPath);


                    string strOperTime = this.Clock.GetClock();
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                        sessioninfo.UserID);   // ������
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                        strOperTime);   // ����ʱ��

                    nRet = this.OperLog.WriteOperLog(domOperLog,
                        sessioninfo.ClientAddress,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "Amerce() API д����־ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    if (this.Statis != null)
                        this.Statis.IncreaseEntryValue(strLibraryCode,
                        "ΥԼ��",
                        "ȡ����",
                        1);

                    {
                        string strPrice = "";
                            // ȡ��ΥԼ���¼�еĽ������
                        nRet = GetAmerceRecordPrice(strAmercedXml,
                            out strPrice,
                            out strError);
                        if (nRet != -1)
                        {
                            string strPrefix = "";
                            string strPostfix = "";
                            double fValue = 0.0;
                            // �����۸����
                            nRet = ParsePriceUnit(strPrice,
                                out strPrefix,
                                out fValue,
                                out strPostfix,
                                out strError);
                            if (nRet != -1)
                            {
                                if (this.Statis != null)
                                    this.Statis.IncreaseEntryValue(
                                    strLibraryCode,
                                    "ΥԼ��",
                                    "ȡ��Ԫ",
                                    fValue);
                            }
                        }
                    }
                }
            }
            finally
            {
                this.ReaderLocks.UnlockForWrite(strReaderBarcode);
            }

            return 0;
        ERROR1:
            return -1;
        }

        // UNDOΥԼ����
        // return:
        //      -1  error
        //      0   succeed
        //      1   ���ֳɹ���strError���б�����Ϣ��failed_item������Щû�б������item���б�
        int UndoAmerces(
            SessionInfo sessioninfo,
            string strReaderBarcode,
            AmerceItem[] amerce_items,
            out AmerceItem[] failed_items,
            out string strReaderXml,
            out string strError)
        {
            strError = "";
            strReaderXml = "";
            failed_items = null;
            int nErrorCount = 0;

            List<string> OverdueStrings = new List<string>();
            List<string> AmercedRecPaths = new List<string>();

            // string[] ids = strAmercedItemIdList.Split(new char[] { ',' });
            List<AmerceItem> failed_list = new List<AmerceItem>();
            for (int i = 0; i < amerce_items.Length; i++)
            {
                AmerceItem item = amerce_items[i];

                /*
                string strID = ids[i].Trim();
                 * */
                if (String.IsNullOrEmpty(item.ID) == true)
                    continue;

                string strTempError = "";

                int nRet = UndoOneAmerce(sessioninfo,
                    strReaderBarcode,
                    item.ID,
                    out strReaderXml,
                    out strTempError);
                if (nRet == -1)
                {
                    if (String.IsNullOrEmpty(strError) == false)
                        strError += ";\r\n";
                    strError += strTempError;
                    nErrorCount++;
                    // return -1;
                    failed_list.Add(item);
                }
            }

            // ÿ��ID�������˴���
            if (nErrorCount >= amerce_items.Length)
                return -1;

            // ���ַ�������
            if (nErrorCount > 0)
            {
                failed_items = new AmerceItem[failed_list.Count];
                failed_list.CopyTo(failed_items);

                strError = "�������ֳɹ���(���ύ�� " + amerce_items.Length + " ���������������� "+nErrorCount+" ��) \r\n" + strError;
                return 1;
            }

            return 0;
        }

        // ��ΥԼ��/������ΥԼ��
        // parameters:
        //      strReaderBarcode    ���������"undo"�����Խ��˲�������Ϊnull������˲�����Ϊnull�������Ҫ���к˶ԣ��������������ߵ��Ѹ�ΥԼ���¼����Ҫ����
        //      strAmerceItemIdList id�б�, �Զ��ŷָ�
        // Ȩ�ޣ���Ҫ��amerce/amercemodifyprice/amerceundo/amercemodifycomment��Ȩ��
        // ��־��
        //      Ҫ������־
        // return:
        //      result.Value    0 �ɹ���1 ���ֳɹ�(result.ErrorInfo������Ϣ)
        public LibraryServerResult Amerce(
            SessionInfo sessioninfo,
            string strFunction,
            string strReaderBarcode,
            AmerceItem [] amerce_items,
            out AmerceItem[] failed_items,
            out string strReaderXml)
        {
            strReaderXml = "";
            failed_items = null;

            LibraryServerResult result = new LibraryServerResult();

            if (String.Compare(strFunction, "amerce", true) == 0)
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("amerce", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "��ΥԼ��������ܾ������߱�amerceȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }

            if (String.Compare(strFunction, "modifyprice", true) == 0)
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("amercemodifyprice", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "�޸�ΥԼ���Ĳ������ܾ������߱�amercemodifypriceȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }

            if (String.Compare(strFunction, "modifycomment", true) == 0)
            {
                /*
                // Ȩ���ַ���
                if (StringUtil.IsInList("amercemodifycomment", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "�޸�ΥԼ��֮ע�͵Ĳ������ܾ������߱�amercemodifycommentȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
                 * */
            }

            if (String.Compare(strFunction, "undo", true) == 0)
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("amerceundo", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "������ΥԼ��������ܾ������߱�amerceundoȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }

            if (String.Compare(strFunction, "rollback", true) == 0)
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("amerce", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "���ؽ�ΥԼ������Ĳ������ܾ������߱�amerceȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }

            if (strFunction != "rollback")
            {
                // ����amerce_items���Ƿ��м۸�����ע�ͱ�������
                bool bHasNewPrice = false;
                bool bHasOverwriteComment = false;    // NewComment���С�����Ϊ���ǡ�Ҳ����˵����NewPrice��NewCommentͬʱ���е����
                for (int i = 0; i < amerce_items.Length; i++)
                {
                    AmerceItem item = amerce_items[i];

                    // NewPrice������ֵ
                    if (String.IsNullOrEmpty(item.NewPrice) == false)
                    {
                        bHasNewPrice = true;
                    }

                    // NewComment������ֵ
                    if (String.IsNullOrEmpty(item.NewComment) == false)
                    {
                        string strNewComment = item.NewComment;

                        bool bAppend = true;
                        if (string.IsNullOrEmpty(strNewComment) == false
                            && strNewComment[0] == '<')
                        {
                            bAppend = false;
                            strNewComment = strNewComment.Substring(1);
                        }
                        else if (string.IsNullOrEmpty(strNewComment) == false
                            && strNewComment[0] == '>')
                        {
                            bAppend = true;
                            strNewComment = strNewComment.Substring(1);
                        }

                        if (bAppend == false)
                            bHasOverwriteComment = true;
                    }
                }

                // ���Ҫ����۸�����Ҫ�����amercemodifypriceȨ�ޡ�
                // amercemodifyprice�ڹ���amerce��modifyprice�ж������õ����ؼ��ǿ��Ƿ��ύ�����¼۸�Ĳ���
                if (bHasNewPrice == true)
                {
                    if (StringUtil.IsInList("amercemodifyprice", sessioninfo.RightsOrigin) == false)
                    {
                        result.Value = -1;
                        result.ErrorInfo = "���м۸���Ҫ��Ľ�ΥԼ��������ܾ������߱�amercemodifypriceȨ�ޡ�(�����߱�amerceȨ�޻�������)";
                        result.ErrorCode = ErrorCode.AccessDenied;
                        return result;
                    }
                }

                if (bHasOverwriteComment == true)
                {
                    // �������amerceȨ�ޣ��򰵺�����amerceappendcomment��Ȩ��

                    if (StringUtil.IsInList("amercemodifycomment", sessioninfo.RightsOrigin) == false)
                    {
                        result.Value = -1;
                        result.ErrorInfo = "����ΥԼ��ע��(������)���Ҫ��Ĳ������ܾ������߱�amercemodifycommentȨ�ޡ�(�����߱�amerceȨ�޻�������)";
                        result.ErrorCode = ErrorCode.AccessDenied;
                        return result;
                    }
                }
            }

            int nRet = 0;
            string strError = "";

            if (String.Compare(strFunction, "amerce", true) != 0
                && String.Compare(strFunction, "undo", true) != 0
                && String.Compare(strFunction, "modifyprice", true) != 0
                && String.Compare(strFunction, "modifycomment", true) != 0
                && String.Compare(strFunction, "rollback", true) != 0)
            {
                result.Value = -1;
                result.ErrorInfo = "δ֪��strFunction����ֵ '" + strFunction + "'";
                result.ErrorCode = ErrorCode.InvalidParameter;
                return result;
            }

            // �����undo, ��Ҫ�ȼ�����ָ��id��ΥԼ����¼��Ȼ��Ӽ�¼�еõ�<readerBarcode>���Ͳ����˶�
            if (String.Compare(strFunction, "undo", true) == 0)
            {
                // UNDOΥԼ����
                // return:
                //      -1  error
                //      0   succeed
                //      1   ���ֳɹ���strError���б�����Ϣ
                nRet = UndoAmerces(
                    sessioninfo,
                    strReaderBarcode,
                    amerce_items,
                    out failed_items,
                    out strReaderXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // 2009/10/10 changed
                result.Value = nRet;
                if (nRet == 1)
                    result.ErrorInfo = strError;
                return result;
            }

            // �ع�
            // 2009/7/14 new add
            if (String.Compare(strFunction, "rollback", true) == 0)
            {
                if (amerce_items != null)
                {
                    strError = "����rollback����ʱamerce_item��������Ϊ��";
                    goto ERROR1;
                }

                if (sessioninfo.AmerceIds == null
                    || sessioninfo.AmerceIds.Count == 0)
                {
                    strError = "��ǰû�п���rollback��ΥԼ������";
                    goto ERROR1;
                }

                // strReaderBarcode����ֵһ��Ϊ�ռ��ɡ������ֵ����Ҫ���SessionInfo�����д�������һ�ε�Amerce��������֤�����һ��
                if (String.IsNullOrEmpty(strReaderBarcode) == false)
                {
                    if (sessioninfo.AmerceReaderBarcode != strReaderBarcode)
                    {
                        strError = "����rollback����ʱstrReaderBarcode���������һ��Amerce�����Ķ���֤����Ų�һ��";
                        goto ERROR1;
                    }
                }

                amerce_items = new AmerceItem[sessioninfo.AmerceIds.Count];

                for (int i = 0; i < sessioninfo.AmerceIds.Count; i++)
                {
                    AmerceItem item = new AmerceItem();
                    item.ID = sessioninfo.AmerceIds[i];

                    amerce_items[i] = item;
                }

                // UNDOΥԼ����
                // return:
                //      -1  error
                //      0   succeed
                //      1   ���ֳɹ���strError���б�����Ϣ
                nRet = UndoAmerces(
                    sessioninfo,
                    sessioninfo.AmerceReaderBarcode,
                    amerce_items,
                    out failed_items,
                    out strReaderXml,
                    out strError);
                if (nRet == -1 || nRet == 1)
                    goto ERROR1;

                // ���ids
                sessioninfo.AmerceIds = new List<string>();
                sessioninfo.AmerceReaderBarcode = "";

                result.Value = 0;
                return result;
            }

            // �Ӷ��߼�¼��
            this.ReaderLocks.LockForWrite(strReaderBarcode);

            try
            {
                // ������߼�¼
                strReaderXml = "";
                string strOutputReaderRecPath = "";
                byte[] reader_timestamp = null;
                nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
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

                // �������Ķ��߿�¹ݴ���
                string strLibraryCode = "";

                // �������߼�¼�������Ķ��߿�Ĺݴ��룬�Ƿ񱻵�ǰ�û���Ͻ
                if (String.IsNullOrEmpty(strOutputReaderRecPath) == false)
                {
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

                    // ��鵱ǰ�������Ƿ��Ͻ������߿�
                    // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                    if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
            sessioninfo.LibraryCodeList) == false)
                    {
                        strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �����Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                        goto ERROR1;
                    }
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

                // ׼����־DOM
                XmlDocument domOperLog = new XmlDocument();
                domOperLog.LoadXml("<root />");
                DomUtil.SetElementText(domOperLog.DocumentElement,
    "libraryCode",
    strLibraryCode);    // �������ڵĹݴ���
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "operation",
                    "amerce");

                // ���嶯��
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "action", strFunction.ToLower());

                // ����֤�����
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "readerBarcode", strReaderBarcode);

                //
                List<string> AmerceRecordXmls = null;
                List<string> CreatedNewPaths = null;

                List<string> Ids = null;


                string strOperTimeString = this.Clock.GetClock();   // RFC1123��ʽ


                bool bReaderDomChanged = false; // ����dom�Ƿ����˱仯����Ҫ�ش�

                {
                    // ����־�б����ɵĶ��߼�¼
                    XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
        "oldReaderRecord", strReaderXml);
                    DomUtil.SetAttr(node, "recPath", strOutputReaderRecPath);
                }

                if (String.Compare(strFunction, "modifyprice", true) == 0)
                {
                    /*
                    // ����־�б����ɵĶ��߼�¼
                    XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
        "oldReaderRecord", strReaderXml);
                    DomUtil.SetAttr(node, "recPath", strOutputReaderRecPath);
                     * */

                    nRet = ModifyPrice(ref readerdom,
                        amerce_items,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet != 0)
                    {
                        bReaderDomChanged = true;
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            "ΥԼ��",
                            "�޸Ĵ�",
                            nRet);
                    }
                    else
                    {
                        // ���һ������Ҳû�з����޸ģ�����Ҫ���ش�����Ϣ��������ǰ�˵ľ���
                        strError = "���棺û���κ�����ļ۸�(��ע��)���޸ġ�";
                        goto ERROR1;
                    }

                    goto SAVERECORD;
                }

                if (String.Compare(strFunction, "modifycomment", true) == 0)
                {
                    /*
                    // ����־�б����ɵĶ��߼�¼
                    XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
        "oldReaderRecord", strReaderXml);
                    DomUtil.SetAttr(node, "recPath", strOutputReaderRecPath);
                     * */

                    nRet = ModifyComment(
                        ref readerdom,
                        amerce_items,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet != 0)
                    {
                        bReaderDomChanged = true;
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            "ΥԼ��֮ע��",
                            "�޸Ĵ�",
                            nRet);
                    }
                    else
                    {
                        // ���һ������Ҳû�з����޸ģ�����Ҫ���ش�����Ϣ��������ǰ�˵ľ���
                        strError = "���棺û���κ������ע�ͱ��޸ġ�";
                        goto ERROR1;
                    }

                    goto SAVERECORD;
                }

                List<string> NotFoundIds = null;
                Ids = null;

                // ��ΥԼ���ڶ��߼�¼��ȥ����ѡ��<overdue>Ԫ�أ����ҹ���һ���¼�¼׼������ΥԼ���
                // return:
                //      -1  error
                //      0   ����domû�б仯
                //      1   ����dom�����˱仯
                nRet = DoAmerceReaderXml(
                    strLibraryCode,
                    ref readerdom,
                    amerce_items,
                    sessioninfo.UserID,
                    strOperTimeString,
                    out AmerceRecordXmls,
                    out NotFoundIds,
                    out Ids,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nRet == 1)
                    bReaderDomChanged = true;


                // ��ΥԼ�����ݿ��д��������µ�ΥԼ���¼
                // parameters:
                //      AmerceRecordXmls    ��Ҫд����¼�¼������
                //      CreatedNewPaths �Ѿ��������¼�¼��·�����顣��������Undo(ɾ���ոմ������¼�¼)
                nRet = CreateAmerceRecords(
                    sessioninfo.Channels,
                    AmerceRecordXmls,
                    out CreatedNewPaths,
                    out strError);
                if (nRet == -1)
                {
                    // undo�Ѿ�д��Ĳ��ּ�¼
                    if (CreatedNewPaths != null
                        && CreatedNewPaths.Count != 0)
                    {
                        string strNewError = "";
                        nRet = DeleteAmerceRecords(
                            sessioninfo.Channels,
                            CreatedNewPaths,
                            out strNewError);
                        if (nRet == -1)
                        {
                            string strList = "";
                            for (int i = 0; i < CreatedNewPaths.Count; i++)
                            {
                                if (strList != "")
                                    strList += ",";
                                strList += CreatedNewPaths[i];
                            }
                            strError = "�ڴ����µ�ΥԼ���¼�Ĺ����з�������: " + strError + "����Undo�´�����ΥԼ���¼�Ĺ����У��ַ�������: " + strNewError + ", ��ϵͳ����Ա�ֹ�ɾ���´����ķ����¼: " + strList;
                            goto ERROR1;
                        }
                    }

                    goto ERROR1;
                }

            SAVERECORD:

                // Ϊд�ض��ߡ����¼��׼��
                // byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";

                RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    goto ERROR1;
                }
                long lRet = 0;

                if (bReaderDomChanged == true)
                {
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


                    // id list
                    /*
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "idList", strAmerceItemIdList);
                     * */
                    WriteAmerceItemList(domOperLog, amerce_items);


                    /*
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "readerBarcode", strReaderBarcode);
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "itemBarcodeList", strItemBarcodeList);
                     */

                    // ��������Ϊamerceʱ���Űѱ��޸ĵ�ʵ���¼д����־��
                    if (String.Compare(strFunction, "amerce", true) == 0)
                    {

                        Debug.Assert(AmerceRecordXmls.Count == CreatedNewPaths.Count, "");

                        // д�����ظ���<amerceRecord>Ԫ��
                        for (int i = 0; i < AmerceRecordXmls.Count; i++)
                        {
                            XmlNode nodeAmerceRecord = domOperLog.CreateElement("amerceRecord");
                            domOperLog.DocumentElement.AppendChild(nodeAmerceRecord);
                            nodeAmerceRecord.InnerText = AmerceRecordXmls[i];

                            DomUtil.SetAttr(nodeAmerceRecord, "recPath", CreatedNewPaths[i]);
                            /*
                            DomUtil.SetElementText(domOperLog.DocumentElement,
                                "record", AmerceRecordXmls[i]);
                             **/

                            if (this.Statis != null)
                                this.Statis.IncreaseEntryValue(
                                strLibraryCode,
                                "ΥԼ��",
                                "������",
                                1);

                            {
                                string strPrice = "";
                                // ȡ��ΥԼ���¼�еĽ������
                                nRet = GetAmerceRecordPrice(AmerceRecordXmls[i],
                                    out strPrice,
                                    out strError);
                                if (nRet != -1)
                                {
                                    string strPrefix = "";
                                    string strPostfix = "";
                                    double fValue = 0.0;
                                    // �����۸����
                                    nRet = ParsePriceUnit(strPrice,
                                        out strPrefix,
                                        out fValue,
                                        out strPostfix,
                                        out strError);
                                    if (nRet != -1)
                                    {
                                        if (this.Statis != null)
                                            this.Statis.IncreaseEntryValue(
                                            strLibraryCode,
                                            "ΥԼ��",
                                            "����Ԫ",
                                            fValue);
                                    }
                                    else
                                    {
                                        // 2012/11/15
                                        this.WriteErrorLog("�ۼ� ΥԼ�� ����Ԫ [" + strPrice + "] ʱ����: " + strError);
                                    }
                                }
                            }

                        } // end of for

                    }

                    // ���µĶ��߼�¼
                    XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
        "readerRecord", strReaderXml);
                    DomUtil.SetAttr(node, "recPath", strOutputReaderRecPath);


                    string strOperTime = this.Clock.GetClock();
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                        sessioninfo.UserID);   // ������
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                        strOperTime);   // ����ʱ��

                    nRet = this.OperLog.WriteOperLog(domOperLog,
                        sessioninfo.ClientAddress,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "Amerce() API д����־ʱ��������: " + strError;
                        goto ERROR1;
                    }
                }

                // ���������һ��Amerce������ID�Ͷ���֤�����
                if (strFunction != "rollback"
                    && Ids != null
                    && Ids.Count != 0)
                {
                    sessioninfo.AmerceReaderBarcode = strReaderBarcode;
                    sessioninfo.AmerceIds = Ids;
                }
            }
            finally
            {
                this.ReaderLocks.UnlockForWrite(strReaderBarcode);
            }

            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        // ����AmerceItem���飬�޸�readerdom�е�<amerce>Ԫ���еļ۸�price���ԡ�
        // Ϊ����"modifyprice"����
        int ModifyPrice(ref XmlDocument readerdom,
            AmerceItem[] amerce_items,
            out string strError)
        {
            strError = "";
            int nChangedCount = 0;

            for (int i = 0; i < amerce_items.Length; i++)
            {
                AmerceItem item = amerce_items[i];

                // ����NewPrice��ֵΪ�յģ�ֱ��������
                // ��˵�����������޸ļ۸�Ϊ��ȫ�յ��ַ�����
                if (String.IsNullOrEmpty(item.NewPrice) == true)
                {
                    if (String.IsNullOrEmpty(item.NewComment) == false)
                    {
                        strError = "������modifyprice�ӹ����������޸�ע��(�����޸ļ۸�)�������appendcomment��modifycomment�ӹ���";
                        return -1;
                    }

                    continue;
                }

                // ͨ��idֵ�ڶ��߼�¼���ҵ���Ӧ��<overdue>Ԫ��
                XmlNode nodeOverdue = readerdom.DocumentElement.SelectSingleNode("overdues/overdue[@id='"+item.ID+"']");
                if (nodeOverdue == null)
                {
                    strError = "IDΪ '"+item.ID+"' ��<overdues/overdue>Ԫ��û���ҵ�...";
                    return -1;
                }

                string strOldPrice = DomUtil.GetAttr(nodeOverdue, "price");

                if (strOldPrice != item.NewPrice)
                {
                    // �޸�price����
                    DomUtil.SetAttr(nodeOverdue, "price", item.NewPrice);
                    nChangedCount++;

                    // ����ע��
                    string strNewComment = item.NewComment;
                    string strExistComment = DomUtil.GetAttr(nodeOverdue, "comment");

                    // ����׷�ӱ�־
                    bool bAppend = true;
                    if (string.IsNullOrEmpty(strNewComment) == false
                        && strNewComment[0] == '<')
                    {
                        bAppend = false;
                        strNewComment = strNewComment.Substring(1);
                    }
                    else if (string.IsNullOrEmpty(strNewComment) == false
                        && strNewComment[0] == '>')
                    {
                        bAppend = true;
                        strNewComment = strNewComment.Substring(1);
                    }

                    if (String.IsNullOrEmpty(strNewComment) == false
                        && bAppend == true)
                    {
                        string strText = "";
                        if (String.IsNullOrEmpty(strExistComment) == false)
                            strText += strExistComment;
                        if (String.IsNullOrEmpty(strNewComment) == false)
                        {
                            if (String.IsNullOrEmpty(strText) == false)
                                strText += "��";
                            strText += strNewComment;
                        }

                        DomUtil.SetAttr(nodeOverdue, "comment", strText);
                    }
                    else if (bAppend == false)
                    {
                        DomUtil.SetAttr(nodeOverdue, "comment", strNewComment);
                    }
                }
            }

            return nChangedCount;
        }

        // 2008/6/19 new add
        // ����AmerceItem���飬�޸�readerdom�е�<amerce>Ԫ���е�comment���ԡ�
        // Ϊ����"modifycomment"����
        int ModifyComment(
            ref XmlDocument readerdom,
            AmerceItem[] amerce_items,
            out string strError)
        {
            strError = "";
            int nChangedCount = 0;

            for (int i = 0; i < amerce_items.Length; i++)
            {
                AmerceItem item = amerce_items[i];

                // ����ͬʱ�޸ļ۸�
                if (String.IsNullOrEmpty(item.NewPrice) == false)
                {
                    strError = "������modifycomment�ӹ������޸ļ۸������modifyprice�ӹ���";
                    return -1;
                }

                /*
                // ����NewComment��ֵΪ�ա�����Ϊ׷�ӵģ�ֱ������
                if (String.IsNullOrEmpty(item.NewComment) == true
                    && strFunction == "appendcomment")
                {
                    continue;
                }*/

                // ͨ��idֵ�ڶ��߼�¼���ҵ���Ӧ��<overdue>Ԫ��
                XmlNode nodeOverdue = readerdom.DocumentElement.SelectSingleNode("overdues/overdue[@id='" + item.ID + "']");
                if (nodeOverdue == null)
                {
                    strError = "IDΪ '" + item.ID + "' ��<overdues/overdue>Ԫ��û���ҵ�...";
                    return -1;
                }


                {
                    string strExistComment = DomUtil.GetAttr(nodeOverdue, "comment");

                    // �������޸�ע��
                    string strNewComment = item.NewComment;

                    // ����׷�ӱ�־
                    bool bAppend = true;
                    if (string.IsNullOrEmpty(strNewComment) == false
                        && strNewComment[0] == '<')
                    {
                        bAppend = false;
                        strNewComment = strNewComment.Substring(1);
                    }
                    else if (string.IsNullOrEmpty(strNewComment) == false
                        && strNewComment[0] == '>')
                    {
                        bAppend = true;
                        strNewComment = strNewComment.Substring(1);
                    }

                    if (String.IsNullOrEmpty(strNewComment) == false
                        && bAppend == true)
                    {
                        string strText = "";
                        if (String.IsNullOrEmpty(strExistComment) == false)
                            strText += strExistComment;
                        if (String.IsNullOrEmpty(strNewComment) == false)
                        {
                            if (String.IsNullOrEmpty(strText) == false)
                                strText += "��";
                            strText += strNewComment;
                        }

                        DomUtil.SetAttr(nodeOverdue, "comment", strText);
                        nChangedCount++;
                    }
                    else if (bAppend == false)
                    {
                        DomUtil.SetAttr(nodeOverdue, "comment", strNewComment);
                        nChangedCount++;    // BUG!!! 2011/12/1ǰ������仰
                    }
                }
            }

            return nChangedCount;
        }

        // ����־DOM�ж���ΥԼ��������Ϣ
        public static AmerceItem[] ReadAmerceItemList(XmlDocument domOperLog)
        {
            XmlNodeList nodes = domOperLog.DocumentElement.SelectNodes("amerceItems/amerceItem");
            AmerceItem[] results = new AmerceItem[nodes.Count];

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strID = DomUtil.GetAttr(node, "id");
                string strNewPrice = DomUtil.GetAttr(node, "newPrice");
                string strComment = DomUtil.GetAttr(node, "newComment");

                results[i] = new AmerceItem();
                results[i].ID = strID;
                results[i].NewPrice = strNewPrice;
                results[i].NewComment = strComment;    // 2007/4/17 new add
            }

            return results;
        }

        // ����־DOM��д��ΥԼ��������Ϣ
        static void WriteAmerceItemList(XmlDocument domOperLog,
            AmerceItem[] amerce_items)
        {
            XmlNode root = domOperLog.CreateElement("amerceItems");
            domOperLog.DocumentElement.AppendChild(root);

            for (int i = 0; i < amerce_items.Length; i++)
            {
                AmerceItem item = amerce_items[i];

                XmlNode node = domOperLog.CreateElement("amerceItem");
                root.AppendChild(node);

                DomUtil.SetAttr(node, "id", item.ID);

                if (String.IsNullOrEmpty(item.NewPrice) == false)
                    DomUtil.SetAttr(node, "newPrice", item.NewPrice);

                // 2007/4/17 new add
                if (String.IsNullOrEmpty(item.NewComment) == false)
                    DomUtil.SetAttr(node, "newComment", item.NewComment);

            }

            /*

            // id list
            DomUtil.SetElementText(domOperLog.DocumentElement,
                "idList", strAmerceItemIdList);
            */
        }

        // �ڲ��¼�л�ý�����֤�����
        // return:
        //      -1  ����
        //      0   �ò�Ϊδ���״̬
        //      1   �ɹ�
        static int GetBorrowerBarcode(XmlDocument dom,
            out string strOutputReaderBarcode,
            out string strError)
        {
            strOutputReaderBarcode = "";
            strError = "";

            strOutputReaderBarcode = DomUtil.GetElementText(dom.DocumentElement,
                "borrower");

            if (String.IsNullOrEmpty(strOutputReaderBarcode) == true)
            {
                strError = "�ò�Ϊδ���״̬";   // "���¼��<borrower>Ԫ��ֵ�����òᲢδ�����κζ��������Ĺ�";
                return 0;
            }

            return 1;
        }


        // ɾ���ոմ�������ΥԼ���¼
        int DeleteAmerceRecords(
            RmsChannelCollection channels,
            List<string> CreatedNewPaths,
            out string strError)
        {
            strError = "";

            RmsChannel channel = channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            for (int i = 0; i < CreatedNewPaths.Count; i++)
            {
                string strPath = CreatedNewPaths[i];

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                int nRedoCount = 0;
            REDO:

                long lRet = channel.DoDeleteRes(strPath,
                    timestamp,
                    out output_timestamp,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                        && nRedoCount < 5) // ���Դ���С��5��
                    {
                        timestamp = output_timestamp;
                        nRedoCount++;
                        goto REDO;
                    }

                    return -1;
                }

            }


            return 0;
        }

        // ��ΥԼ�����ݿ��д��������µ�ΥԼ���¼
        // parameters:
        //      AmerceRecordXmls    ��Ҫд����¼�¼������
        //      CreatedNewPaths �Ѿ��������¼�¼��·�����顣��������Undo(ɾ���ոմ������¼�¼)
        int CreateAmerceRecords(
            RmsChannelCollection channels,
            List<string> AmerceRecordXmls,
            out List<string> CreatedNewPaths,
            out string strError)
        {
            strError = "";
            CreatedNewPaths = new List<string>();
            long lRet = 0;

            if (string.IsNullOrEmpty(this.AmerceDbName) == true)
            {
                strError = "��δ����ΥԼ�����";
                return -1;
            }

            RmsChannel channel = channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            for (int i = 0; i < AmerceRecordXmls.Count; i++)
            {
                string strXml = AmerceRecordXmls[i];

                string strPath = this.AmerceDbName + "/?";

                string strOutputPath = "";
                byte[] timestamp = null;
                byte[] output_timestamp = null;

                // д�¼�¼
                lRet = channel.DoSaveTextRes(
                    strPath,
                    strXml,
                    false,
                    "content,ignorechecktimestamp",
                    timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    return -1;

                CreatedNewPaths.Add(strOutputPath);
            }

            return 0;
        }

        // ȡ��ΥԼ���¼�еĽ������
        static int GetAmerceRecordPrice(string strAmercedXml,
            out string strPrice,
            out string strError)
        {
            strPrice = "";
            strError = "";

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strAmercedXml);
            }
            catch (Exception ex)
            {
                strError = "XML��¼װ��DOMʱ����: " + ex.Message;
                return -1;
            }

            strPrice = DomUtil.GetElementText(dom.DocumentElement,
                "price");
            return 0;
        }

        // ��ΥԼ���¼��ʽת��Ϊ���߼�¼�е�<overdue>Ԫ�ظ�ʽ
        // return:
        //      -1  error
        //      0   strAmercedXml��<state>Ԫ�ص�ֵΪ*��*"settlemented"
        //      1   strAmercedXml��<state>Ԫ�ص�ֵΪ"settlemented"
        static int ConvertAmerceRecordToOverdueString(string strAmercedXml,
            out string strReaderBarcode,
            out string strOverdueString,
            out string strError)
        {
            strReaderBarcode = "";
            strOverdueString = "";
            strError = "";

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strAmercedXml);
            }
            catch (Exception ex)
            {
                strError = "XML��¼װ��DOMʱ����: " + ex.Message;
                return -1;
            }

            string strItemBarcode = DomUtil.GetElementText(dom.DocumentElement,
                "itemBarcode");
            string strItemRecPath = DomUtil.GetElementText(dom.DocumentElement,
                "itemRecPath");

            strReaderBarcode = DomUtil.GetElementText(dom.DocumentElement,
                "readerBarcode");
            string strState = DomUtil.GetElementText(dom.DocumentElement,
                "state");

            string strID = DomUtil.GetElementText(dom.DocumentElement,
                "id");
            string strReason = DomUtil.GetElementText(dom.DocumentElement,
                "reason");

            // 2007/12/17
            string strOverduePeriod = DomUtil.GetElementText(dom.DocumentElement,
                "overduePeriod");

            string strPrice = DomUtil.GetElementText(dom.DocumentElement,
                "price");
            string strOriginPrice = DomUtil.GetElementText(dom.DocumentElement,
                "originPrice");
            string strComment = DomUtil.GetElementText(dom.DocumentElement,
                "comment");

            string strBorrowDate = DomUtil.GetElementText(dom.DocumentElement,
                "borrowDate");
            string strBorrowPeriod = DomUtil.GetElementText(dom.DocumentElement,
                "borrowPeriod");
            string strBorrowOperator = DomUtil.GetElementText(dom.DocumentElement,
                "borrowOperator");  // 2006/3/27 new add


            string strReturnDate = DomUtil.GetElementText(dom.DocumentElement,
                "returnDate");
            string strReturnOperator = DomUtil.GetElementText(dom.DocumentElement,
                "returnOperator");

            // 2008/6/23 new add
            string strPauseStart = DomUtil.GetElementText(dom.DocumentElement,
                "pauseStart");

            // д��DOM
            XmlDocument domOutput = new XmlDocument();
            domOutput.LoadXml("<overdue />");
            XmlNode nodeOverdue = domOutput.DocumentElement;

            DomUtil.SetAttr(nodeOverdue, "barcode", strItemBarcode);
            if (String.IsNullOrEmpty(strItemRecPath) == false)
                DomUtil.SetAttr(nodeOverdue, "recPath", strItemRecPath);

            DomUtil.SetAttr(nodeOverdue, "reason", strReason);

            // 2007/12/17 new add
            if (String.IsNullOrEmpty(strOverduePeriod) == false)
                DomUtil.SetAttr(nodeOverdue, "overduePeriod", strOverduePeriod);



            if (String.IsNullOrEmpty(strOriginPrice) == false)
            {
                DomUtil.SetAttr(nodeOverdue, "price", strOriginPrice);
                DomUtil.SetAttr(nodeOverdue, "newPrice", strPrice);
            }
            else
                DomUtil.SetAttr(nodeOverdue, "price", strPrice);


            // ���ص�ʱ�򲻶�ʧע�͡���Ϊ�Ѿ��޷��ֱ��Ĵ�׷�ӵ�ע�ͣ�����ԭ��������
            // 2007/4/19
            if (String.IsNullOrEmpty(strComment) == false)
                DomUtil.SetAttr(nodeOverdue, "comment", strComment);

            // TODO: ����ֵ���о�һ�¡����AmerceItem.Comment�ܸ��������е�comment��Ϣ��
            // ��ô���ص�ʱ��Ͳ�Ҫ��ʧע�͡�

            DomUtil.SetAttr(nodeOverdue, "borrowDate", strBorrowDate);
            DomUtil.SetAttr(nodeOverdue, "borrowPeriod", strBorrowPeriod);
            DomUtil.SetAttr(nodeOverdue, "returnDate", strReturnDate);
            DomUtil.SetAttr(nodeOverdue, "borrowOperator", strBorrowOperator);
            DomUtil.SetAttr(nodeOverdue, "operator", strReturnOperator);
            DomUtil.SetAttr(nodeOverdue, "id", strID);

            // 2008/6/23 new add
            if (String.IsNullOrEmpty(strPauseStart) == false)
                DomUtil.SetAttr(nodeOverdue, "pauseStart", strPauseStart);

            strOverdueString = nodeOverdue.OuterXml;

            if (strState == "settlemented")
                return 1;

            return 0;
        }

        // �����߼�¼�е�<overdue>Ԫ�غ�����ת��ΪΥԼ���ļ�¼��ʽ
        // parameters:
        //      strLibraryCode  ���߼�¼�����Ĺݴ���
        //      strState    һ��Ϊ"amerced"����ʾ��δ����
        //      strNewPrice ����ļ۸����Ϊ�գ����ʾ����ԭ���ļ۸�
        //      strComment  ǰ�˸�����ע�͡�
        static int ConvertOverdueStringToAmerceRecord(XmlNode nodeOverdue,
            string strLibraryCode,
            string strReaderBarcode,
            string strState,
            string strNewPrice,
            string strNewComment,
            string strOperator,
            string strOperTime,
            string strForegiftPrice,    // ���Զ��߼�¼<foregift>Ԫ���ڵļ۸��ַ���
            out string strFinalPrice,   // ����ʹ�õļ۸��ַ���
            out string strAmerceRecord,
            out string strError)
        {
            strAmerceRecord = "";
            strError = "";
            strFinalPrice = "";
            int nRet = 0;


            string strItemBarcode = DomUtil.GetAttr(nodeOverdue, "barcode");
            string strItemRecPath = DomUtil.GetAttr(nodeOverdue, "recPath");
            string strReason = DomUtil.GetAttr(nodeOverdue, "reason");

            // 2007/12/17 new add
            string strOverduePeriod = DomUtil.GetAttr(nodeOverdue, "overduePeriod");

            string strPrice = "";
            string strOriginPrice = "";

            if (String.IsNullOrEmpty(strNewPrice) == true)
                strPrice = DomUtil.GetAttr(nodeOverdue, "price");
            else
            {
                strPrice = strNewPrice;
                strOriginPrice = DomUtil.GetAttr(nodeOverdue, "price");
            }

            // 2008/11/15 new add
            // �����۸��ַ����Ƿ�Ϊ��?
            if (strPrice == "%return_foregift_price%")
            {
                // ������ȡ��ı仯
                if (String.IsNullOrEmpty(strOriginPrice) == true)
                    strOriginPrice = strPrice;

                // ������"-123.4+10.55-20.3"�ļ۸��ַ�����ת������
                // parameters:
                //      bSum    �Ƿ�Ҫ˳�����? true��ʾҪ����
                nRet = PriceUtil.NegativePrices(strForegiftPrice,
                    true,
                    out strPrice,
                    out strError);
                if (nRet == -1)
                {
                    strError = "��ת(���Զ��߼�¼�е�<foregift>Ԫ�ص�)�۸��ַ��� '" + strForegiftPrice + "' ʱ����: " + strError;
                    return -1;
                }

                // ���������ת��ļ۸��ַ���Ϊ�գ�����Ҫ�ر��滻Ϊ��0����������滷�ڱ�����û��ֵ�Ŀ��ַ�����������������ģ���ʾ�˿�(�����ǽ���)Ӵ
                if (String.IsNullOrEmpty(strPrice) == true)
                    strPrice = "-0";

            }

            if (strPrice.IndexOf('%') != -1)
            {
                strError = "�۸��ַ��� '" + strPrice + "' ��ʽ���󣺳���ʹ�ú�%return_foregift_price%���⣬�۸��ַ����в��������%����";
                return -1;
            }

            strFinalPrice = strPrice;

            string strBorrowDate = DomUtil.GetAttr(nodeOverdue, "borrowDate");
            string strBorrowPeriod = DomUtil.GetAttr(nodeOverdue, "borrowPeriod");
            string strReturnDate = DomUtil.GetAttr(nodeOverdue, "returnDate");
            string strBorrowOperator = DomUtil.GetAttr(nodeOverdue, "borrowOperator");
            string strReturnOperator = DomUtil.GetAttr(nodeOverdue, "operator");
            string strID = DomUtil.GetAttr(nodeOverdue, "id");
            string strExistComment = DomUtil.GetAttr(nodeOverdue, "comment");

            // 2008/6/23 new add
            string strPauseStart = DomUtil.GetAttr(nodeOverdue, "pauseStart");

            // д��DOM
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");
            DomUtil.SetElementText(dom.DocumentElement,
                "itemBarcode", strItemBarcode);

            if (String.IsNullOrEmpty(strItemRecPath) == false)
            {
                DomUtil.SetElementText(dom.DocumentElement,
    "itemRecPath", strItemRecPath);
            }

            DomUtil.SetElementText(dom.DocumentElement,
                "readerBarcode", strReaderBarcode);

            // 2012/9/15
            DomUtil.SetElementText(dom.DocumentElement,
    "libraryCode", strLibraryCode);

            DomUtil.SetElementText(dom.DocumentElement,
                "state", strState);
            DomUtil.SetElementText(dom.DocumentElement,
                "id", strID);
            DomUtil.SetElementText(dom.DocumentElement,
                "reason", strReason);

            // 2007/12/17
            if (String.IsNullOrEmpty(strOverduePeriod) == false)
                DomUtil.SetElementText(dom.DocumentElement,
                    "overduePeriod", strOverduePeriod);

            DomUtil.SetElementText(dom.DocumentElement,
                "price", strPrice);
            if (String.IsNullOrEmpty(strOriginPrice) == false)
            {
                DomUtil.SetElementText(dom.DocumentElement,
                    "originPrice", strOriginPrice);
            }

            // 2008/6/25 new add
            {
                bool bAppend = true;
                if (string.IsNullOrEmpty(strNewComment) == false
                    && strNewComment[0] == '<')
                {
                    bAppend = false;
                    strNewComment = strNewComment.Substring(1);
                }
                else if (string.IsNullOrEmpty(strNewComment) == false
                    && strNewComment[0] == '>')
                {
                    bAppend = true;
                    strNewComment = strNewComment.Substring(1);
                }

                if (bAppend == true)
                {
                    string strText = "";
                    if (String.IsNullOrEmpty(strExistComment) == false)
                        strText += strExistComment;
                    if (String.IsNullOrEmpty(strNewComment) == false)
                    {
                        if (String.IsNullOrEmpty(strText) == false)
                            strText += "��";
                        strText += strNewComment;
                    }

                    DomUtil.SetElementText(dom.DocumentElement,
                        "comment",
                        strText);
                }
                else  
                {
                    Debug.Assert(bAppend == false, "");

                    DomUtil.SetElementText(dom.DocumentElement,
                        "comment",
                        strNewComment);
                }
            }

            /*
            if (String.IsNullOrEmpty(strNewComment) == false
                || String.IsNullOrEmpty(strExistComment) == false)
            {
                string strText = "";
                if (String.IsNullOrEmpty(strExistComment) == false)
                    strText += strExistComment;
                if (String.IsNullOrEmpty(strNewComment) == false)
                {
                    if (String.IsNullOrEmpty(strText) == false)
                        strText += "��";
                    strText += strNewComment;
                }

                // 2008/6/25 ��SetElementInnerXml()�޸Ķ���
                DomUtil.SetElementText(dom.DocumentElement,
                    "comment",
                    strText);
            }
             * */

            DomUtil.SetElementText(dom.DocumentElement,
                "borrowDate", strBorrowDate);
            DomUtil.SetElementText(dom.DocumentElement,
                "borrowPeriod", strBorrowPeriod);
            DomUtil.SetElementText(dom.DocumentElement,
                "borrowOperator", strBorrowOperator);   // 2006/3/27 new add

            DomUtil.SetElementText(dom.DocumentElement,
                "returnDate", strReturnDate);
            DomUtil.SetElementText(dom.DocumentElement,
                "returnOperator", strReturnOperator);

            DomUtil.SetElementText(dom.DocumentElement,
                "operator", strOperator);   // ���������
            DomUtil.SetElementText(dom.DocumentElement,
                "operTime", strOperTime);

            // 2008/6/23 new add
            if (String.IsNullOrEmpty(strPauseStart) == false)
            {
                DomUtil.SetElementText(dom.DocumentElement,
                    "pauseStart", strPauseStart);
            }


            strAmerceRecord = dom.OuterXml;

            return 0;
        }

        // ��ΥԼ���ڶ��߼�¼��ȥ����ѡ��<overdue>Ԫ�أ����ҹ���һ���¼�¼׼������ΥԼ���
        // parameters:
        //      strLibraryCode  ���߼�¼�����Ĺݴ���
        // return:
        //      -1  error
        //      0   ����domû�б仯
        //      1   ����dom�����˱仯
        static int DoAmerceReaderXml(
            string strLibraryCode,
            ref XmlDocument readerdom,
            AmerceItem[] amerce_items,
            string strOperator,
            string strOperTimeString,
            out List<string> AmerceRecordXmls,
            out List<string> NotFoundIds,
            out List<string> Ids,
            out string strError)
        {
            strError = "";
            AmerceRecordXmls = new List<string>();
            NotFoundIds = new List<string>();
            Ids = new List<string>();
            int nRet = 0;

            string strReaderBarcode = DomUtil.GetElementText(readerdom.DocumentElement,
                "barcode");
            if (String.IsNullOrEmpty(strReaderBarcode) == true)
            {
                strError = "���߼�¼�о�Ȼû��<barcode>Ԫ��ֵ";
                return -1;
            }

            bool bChanged = false;  // ����dom�Ƿ����˸ı�

            // string strNotFoundIds = "";

            for (int i = 0; i < amerce_items.Length; i++)
            {
                AmerceItem item = amerce_items[i];

                // string strID = ids[i].Trim();
                if (String.IsNullOrEmpty(item.ID) == true)
                    continue;

                XmlNode node = readerdom.DocumentElement.SelectSingleNode("overdues/overdue[@id='" + item.ID + "']");
                if (node == null)
                {
                    NotFoundIds.Add(item.ID);

                    /*
                    if (strNotFoundIds != "")
                        strNotFoundIds += ",";
                    strNotFoundIds += item.ID;
                     * */
                    continue;
                }

                string strForegiftPrice = DomUtil.GetElementText(readerdom.DocumentElement,
                    "foregift");

                string strFinalPrice = "";  // ����ʹ�õļ۸��ַ��������Ǵ�item.NewPrice��node�ڵ��price������ѡ����������Ҿ���ȥ���������һ�����۸��ַ���
                string strAmerceRecord = "";
                // �����߼�¼�е�<overdue>Ԫ�غ�����ת��ΪΥԼ���ļ�¼��ʽ
                nRet = ConvertOverdueStringToAmerceRecord(node,
                    strLibraryCode,
                    strReaderBarcode,
                    "amerced",
                    item.NewPrice,
                    item.NewComment,
                    strOperator,
                    strOperTimeString,
                    strForegiftPrice,
                    out strFinalPrice,
                    out strAmerceRecord,
                    out strError);
                if (nRet == -1)
                    return -1;

                AmerceRecordXmls.Add(strAmerceRecord);

                Ids.Add(item.ID);

                // �����Ѻ����Ҫ��/��<foregift>Ԫ���ڵļ۸�ֵ������Ϊ�����˷�Ϊ���������������Ѿ����ڼ۸��ַ����У����Զ����Ϊ����
                string strReason = "";
                strReason = DomUtil.GetAttr(node, "reason");

                // 2008/11/11 new add
                if (strReason == "Ѻ��")
                {
                    string strNewPrice = "";

                    /*
                    string strOldPrice = DomUtil.GetElementText(readerdom.DocumentElement,
                        "foregift");

                    if (strOldPrice.IndexOf('%') != -1)
                    {
                        strError = "���Զ��߼�¼<foregift>Ԫ�صļ۸��ַ��� '" + strOldPrice + "' ��ʽ���󣺼۸��ַ����в��������%����";
                        return -1;
                    }

                    string strPrice = "";

                    if (String.IsNullOrEmpty(item.NewPrice) == true)
                        strPrice = DomUtil.GetAttr(node, "price");
                    else
                        strPrice = item.NewPrice;

                    // �����۸��ַ����Ƿ�Ϊ��?
                    if (strPrice == "%return_foregift_price%")
                    {
                        // ������"-123.4+10.55-20.3"�ļ۸��ַ�����ת������
                        // parameters:
                        //      bSum    �Ƿ�Ҫ˳�����? true��ʾҪ����
                        nRet = PriceUtil.NegativePrices(strOldPrice,
                            true,
                            out strPrice,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "��ת(���Զ��߼�¼�е�<foregift>Ԫ�ص�)�۸��ַ��� '" + strOldPrice + "' ʱ����: " + strError;
                            return -1;
                        }
                    }

                    if (strPrice.IndexOf('%') != -1)
                    {
                        strError = "�۸��ַ��� '" + strPrice + "' ��ʽ���󣺳���ʹ�ú�%return_foregift_price%���⣬�۸��ַ����в��������%����";
                        return -1;
                    }

                    if (String.IsNullOrEmpty(strOldPrice) == false)
                    {
                        strNewPrice = PriceUtil.JoinPriceString(strOldPrice, strPrice);
                    }
                    else
                    {
                        strNewPrice = strPrice;
                    }
                     * */
                    if (String.IsNullOrEmpty(strForegiftPrice) == false)
                    {
                        strNewPrice = PriceUtil.JoinPriceString(strForegiftPrice, strFinalPrice);
                    }
                    else
                    {
                        strNewPrice = strFinalPrice;
                    }


                    DomUtil.SetElementText(readerdom.DocumentElement,
                        "foregift",
                        strNewPrice);

                    // �Ƿ�˳��д�����һ�εĽ���ʱ��?
                    bChanged = true;
                }

                // �ڶ��߼�¼��ɾ������ڵ�
                node.ParentNode.RemoveChild(node);
                bChanged = true;
            }

            /*
            if (strNotFoundIds != "")
            {
                strError = "����idû����ƥ���<overdue>Ԫ��" + strNotFoundIds;
                return -1;
            }*/
            if (NotFoundIds.Count > 0)
            {
                strError = "����idû����ƥ���<overdue>Ԫ��: " + StringUtil.MakePathList(NotFoundIds);
                return -1;
            }


            if (bChanged == true)
                return 1;
            return 0;
        }
        /*
        // �Ƿ������ͣ�������
        static bool InPauseBorrowing(XmlDocument readerdom,
            out string strMessage)
        {
            strMessage = "";

            XmlNodeList nodes = readerdom.DocumentElement.SelectNodes("overdues/overdue");
            if (nodes.Count == 0)
                return false;

            XmlNode node = null;
            int nTotalCount = 0;

            string strPauseStart = "";

            // ������ͣ������������Ŀ
            for (int i = 0; i < nodes.Count; i++)
            {
                node = nodes[i];
                strPauseStart = DomUtil.GetAttr(node, "pauseStart");
                if (String.IsNullOrEmpty(strPauseStart) == false)
                    nTotalCount++;
            }

            // �ҵ���һ������������
            for (int i = 0; i < nodes.Count; i++)
            {
                node = nodes[i];
                strPauseStart = DomUtil.GetAttr(node, "pauseStart");
                if (String.IsNullOrEmpty(strPauseStart) == false)
                    goto FOUND;
            }

            if (nTotalCount > 0)
            {
                strMessage = "��δ������ " + nTotalCount.ToString() + " ����ͣ��������";
                return true;
            }


            return false;   // û���ҵ�������������
        FOUND:
            string strOverduePeriod = DomUtil.GetAttr(node, "overduePeriod");
            strMessage = "��һ���� " + DateTimeUtil.LocalDate(strPauseStart) + " ��ʼ�ģ�Ϊ�� " + strOverduePeriod + " ����ͣ�������";

            if (nTotalCount > 1)
                strMessage += "(���⻹��δ������ "+(nTotalCount-1).ToString()+" ��)";

            return true;
        }
         * */

        // Ϊ�˼�����ǰ�İ汾��������У����ʹ���⣬������Ҫʹ����
        // ������ͣ�����ͣ������ֵ
        public int ComputePausePeriodValue(string strReaderType,
            long lValue,
            out long lResultValue,
            out string strPauseCfgString,
            out string strError)
        {
            return ComputePausePeriodValue(strReaderType,
                "",
                lValue,
                out lResultValue,
                out strPauseCfgString,
                out strError);
        }

        // ������ͣ�����ͣ������ֵ
        public int ComputePausePeriodValue(string strReaderType,
            string strLibraryCode,
            long lValue,
            out long lResultValue,
            out string strPauseCfgString,
            out string strError)
        {
            strError = "";
            strPauseCfgString = "1.0";
            lResultValue = lValue;

            // ��� '��ͣ��������' ���ò���
            MatchResult matchresult;
            // return:
            //      reader��book���;�ƥ�� ��4��
            //      ֻ��reader����ƥ�䣬��3��
            //      ֻ��book����ƥ�䣬��2��
            //      reader��book���Ͷ���ƥ�䣬��1��
            int nRet = this.GetLoanParam(
                //null,
                strLibraryCode,
                strReaderType,
                "",
                "��ͣ��������",
                out strPauseCfgString,
                out matchresult,
                out strError);
            if (nRet == -1)
            {
                strError = "��� �ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' �� ��ͣ�������� ����ʱ��������: " + strError;
                return -1;
            }

            if (nRet < 3 || string.IsNullOrEmpty(strPauseCfgString) == true)
            {
                // û���ҵ�ƥ��������͵Ķ��壬���� 1.0 ����
                strPauseCfgString = "1.0";
                return 0;
            }

            double ratio = 1.0;

            try
            {
                ratio = Convert.ToDouble(strPauseCfgString);
            }
            catch
            {
                strError = "��ͣ�������� �����ַ��� '" + strPauseCfgString + "' ��ʽ����Ӧ��Ϊһ��С����";
                return -1;
            }

            lResultValue = (long)((double)lValue * ratio);
            return 1;
        }

        // ��װ�汾��Ϊ�˼�����ǰ�ű���һ�δ����в�Ҫʹ���������
        public int HasPauseBorrowing(
    Calendar calendar,
    XmlDocument readerdom,
    out string strMessage,
    out string strError)
        {
            return HasPauseBorrowing(
                calendar,
                "",
                readerdom,
                out strMessage,
                out strError);
        }

        // �Ƿ������ͣ�������
        // text-level: �û���ʾ
        // return:
        //      -1  error
        //      0   ������
        //      1   ����
        public int HasPauseBorrowing(
            Calendar calendar,
            string strLibraryCode,
            XmlDocument readerdom,
            out string strMessage,
            out string strError)
        {
            strError = "";
            strMessage = "";

            XmlNodeList nodes = readerdom.DocumentElement.SelectNodes("overdues/overdue");
            if (nodes.Count == 0)
                return 0;

            string strReaderType = DomUtil.GetElementText(readerdom.DocumentElement,
                "readerType");

            int nRet = 0;
            XmlNode node = null;
            int nTotalCount = 0;

            string strFirstPauseStart = "";


            // �ҵ���һ������������
            for (int i = 0; i < nodes.Count; i++)
            {
                node = nodes[i];
            string strPauseStart = "";
                strPauseStart = DomUtil.GetAttr(node, "pauseStart");
                if (String.IsNullOrEmpty(strPauseStart) == false)
                {
                    // 2008/1/16 ������
                    // �����pauseStart���ԣ�����û��overduePeriod���ԣ����ڸ�ʽ����
                    // ��Ҫ�������Ѱ�Ҹ�ʽ��ȷ�ĵ�һ��
                    string strOverduePeriod = DomUtil.GetAttr(node, "overduePeriod");
                    if (String.IsNullOrEmpty(strOverduePeriod) == true)
                    {
                        strPauseStart = "";
                        continue;
                    }

                    strFirstPauseStart = strPauseStart;
                    break;
                }
            }

            long lTotalOverduePeriod = 0;
            string strTotalUnit = "";

            // ������ͣ�����������ʱ���ܳ��Ⱥ�����������
            for (int i = 0; i < nodes.Count; i++)
            {
                node = nodes[i];
                string strOverduePeriod = DomUtil.GetAttr(node, "overduePeriod");
                if (String.IsNullOrEmpty(strOverduePeriod) == true)
                    continue;

                string strUnit = "";
                long lOverduePeriod = 0;

                // �������޲���
                nRet = ParsePeriodUnit(strOverduePeriod,
                    out lOverduePeriod,
                    out strUnit,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (strTotalUnit == "")
                    strTotalUnit = strUnit;
                else
                {
                    if (strTotalUnit != strUnit)
                    {
                        // ������ʱ�䵥λ�Ĳ�һ��
                        if (strTotalUnit == "day" && strUnit == "hour")
                            lOverduePeriod = lOverduePeriod / 24;
                        else if (strTotalUnit == "hour" && strUnit == "day")
                            lOverduePeriod = lOverduePeriod * 24;
                        else
                        {
                            // text-level: �ڲ�����
                            strError = "ʱ�䵥λ '" + strUnit + "' ��ǰ�����ù���ʱ�䵥λ '" + strTotalUnit + "' ��һ�£��޷����мӷ�����";
                            return -1;
                        }

                    }
                }

                long lResultValue = 0;
                string strPauseCfgString = "";
                // ������ͣ�����ͣ������ֵ
                nRet = ComputePausePeriodValue(strReaderType,
                    strLibraryCode,
                    lOverduePeriod,
                    out lResultValue,
                    out strPauseCfgString,
                    out strError);
                if (nRet == -1)
                    return -1;


                lTotalOverduePeriod += lResultValue;    //  lOverduePeriod;

                nTotalCount++;
            }

            // 2008/1/16 changed strPauseStart -->strFirstPauseStart
            if (String.IsNullOrEmpty(strFirstPauseStart) == true)
            {
                if (nTotalCount > 0)
                {
                    // text-level: �û���ʾ
                    strMessage = string.Format(this.GetString("��s��δ��������ͣ��������"), // "�� {0} ��δ��������ͣ��������"
                        nTotalCount.ToString());
                        // "�� " + nTotalCount.ToString() + " ��δ��������ͣ��������";
                    return 1;
                }

                return 0;
            }

            DateTime pause_start;
            try
            {
                pause_start = DateTimeUtil.FromRfc1123DateTimeString(strFirstPauseStart);
            }
            catch
            {
                // text-level: �ڲ�����
                strError = "ͣ�迪ʼ���� '" + strFirstPauseStart + "' ��ʽ����";
                return -1;
            }

            DateTime timeEnd;   // ��ͣ���������Ľ�������
            DateTime nextWorkingDay;

            // ���㻹������
            // parameters:
            //      calendar    �������������Ϊnull����ʾ���������зǹ������жϡ�
            // return:
            //      -1  ����
            //      0   �ɹ���timeEnd�ڹ����շ�Χ�ڡ�
            //      1   �ɹ���timeEnd�����ڷǹ����ա�nextWorkingDay�Ѿ���������һ�������յ�ʱ��
            nRet = GetReturnDay(
                calendar,
                pause_start,
                lTotalOverduePeriod,
                strTotalUnit,
                out timeEnd,
                out nextWorkingDay,
                out strError);
            if (nRet == -1)
            {
                // text-level: �ڲ�����
                strError = "������ͣ����������ڹ��̷�������: " + strError;
                return -1;
            }

            bool bEndInNonWorkingDay = false;
            if (nRet == 1)
            {
                // end�ڷǹ�����
                bEndInNonWorkingDay = true;
            }

            DateTime now_rounded = this.Clock.UtcNow;  //  ����

            // ���滯ʱ��
            nRet = RoundTime(strTotalUnit,
                ref now_rounded,
                out strError);
            if (nRet == -1)
                return -1;

            TimeSpan delta = now_rounded - timeEnd;

            long lDelta = 0;
            nRet = ParseTimeSpan(
                delta,
                strTotalUnit,
                out lDelta,
                out strError);
            if (nRet == -1)
                return -1;

            if (strTotalUnit == "hour")
            {
                // text-level: �û���ʾ
                strMessage = string.Format(this.GetString("����s����ͣ���������s��ʼ���ܼ�Ӧ��ͣ����s, ��s����"),
                    // "���� {0} ����ͣ��������� {1} ��ʼ���ܼ�Ӧ��ͣ���� {2}, �� {3} ������"
                    nTotalCount.ToString(),
                    pause_start.ToString("s"),
                    lTotalOverduePeriod.ToString() + GetDisplayTimeUnitLang(strTotalUnit),
                    timeEnd.ToString("s"));
                    // "���� " + nTotalCount.ToString() + " ����ͣ��������� " + pause_start.ToString("s") + " ��ʼ���ܼ�Ӧ��ͣ���� " + lTotalOverduePeriod.ToString() + GetDisplayTimeUnitLang(strTotalUnit) + ", �� " + timeEnd.ToString("s") + " ������";
            }
            else
            {
                // text-level: �û���ʾ

                strMessage = string.Format(this.GetString("����s����ͣ���������s��ʼ���ܼ�Ӧ��ͣ����s, ��s����"),
                    // "���� {0} ����ͣ��������� {1} ��ʼ���ܼ�Ӧ��ͣ���� {2}, �� {3} ������"
                    nTotalCount.ToString(),
                    pause_start.ToString("d"),  // "yyyy-MM-dd"
                    lTotalOverduePeriod.ToString() + GetDisplayTimeUnitLang(strTotalUnit),
                    timeEnd.ToString("d")); // "yyyy-MM-dd"
                // "���� " + nTotalCount.ToString() + " ����ͣ��������� " + pause_start.ToString("yyyy-MM-dd") + " ��ʼ���ܼ�Ӧ��ͣ���� " + lTotalOverduePeriod.ToString() + GetDisplayTimeUnitLang(strTotalUnit) + ", �� " + timeEnd.ToString("yyyy-MM-dd") + " ������";
            }

            if (lDelta > 0)
            {
                // text-level: �û���ʾ
                strMessage += this.GetString("����ǰʱ�̣�����������ͣ���������Ѿ�������"); // "����ǰʱ�̣�����������ͣ���������Ѿ�������"
            }


            return 1;
        }

        // ������ͣ������
        // TODO: �������������־�ָ�������ã������ڲ�����UtcNow��Ϊ��ǰʱ����ǲ���ȷ�ġ�Ӧ������־�м��صĽ��ĵ�ʱʱ��
        // TODO: д����־��ͬʱ��Ҳ��Ҫд��<overdues>Ԫ����һ��˵���Ե�λ�ã�������ʱ���
        // parameters:
        //      strReaderRecPath    ��strActionΪ"refresh"ʱ����Ҫ������������ݡ��Ա�д����־��
        // return:
        //      -1  error
        //      0   readerdomû���޸�
        //      1   readerdom�������޸�
        public int ProcessPauseBorrowing(
            string strLibraryCode,
            ref XmlDocument readerdom,
            string strReaderRecPath,
            string strUserID,
            string strAction,
            string strClientAddress,
            out string strError)
        {
            strError = "";
            int nRet = 0;


            // ����
            if (strAction == "start")
            {
                XmlNodeList nodes = readerdom.DocumentElement.SelectNodes("overdues/overdue");
                if (nodes.Count == 0)
                    return 0;


                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];
                    string strPauseStart = DomUtil.GetAttr(node, "pauseStart");
                    if (String.IsNullOrEmpty(strPauseStart) == false)
                        return 0;   // �Ѿ��������˵��������������
                }

                // 2008/1/16 changed
                // Ѱ�ҵ�һ������overduePeriod����ֵ���������Ϊ����
                bool bFound = false;
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];
                    string strOverduePeriod = DomUtil.GetAttr(node, "overduePeriod");
                    if (String.IsNullOrEmpty(strOverduePeriod) == false)
                    {
                        // �ѵ�һ������overduePeriod����ֵ����������Ϊ����
                        DomUtil.SetAttr(node, "pauseStart", this.Clock.GetClock());
                        bFound = true;
                        break;
                    }
                }

                if (bFound == false)
                    return 0;   // û���ҵ�����overduePeriod����ֵ������


                // д��ͳ��ָ��
                // �����������������Ƕ��߸���
                if (this.Statis != null)
                    this.Statis.IncreaseEntryValue(
                    strLibraryCode,
                    "����",
                    "��ͣ������������",
                    1);


                // TODO: �����¼���־����¼��������Ķ���
                return 1;
            }

            // ˢ��
            if (strAction == "refresh")
            {
                if (String.IsNullOrEmpty(strReaderRecPath) == true)
                {
                    strError = "refreshʱ�����ṩstrReaderRecPath����ֵ�������޷�������־��¼";
                    return -1;
                }

                int nExpiredCount = 0;

                string strReaderBarcode = DomUtil.GetElementText(readerdom.DocumentElement,
                    "barcode");
                string strOldReaderXml = readerdom.OuterXml;

                XmlDocument domOperLog = new XmlDocument();
                domOperLog.LoadXml("<root />");
                DomUtil.SetElementText(domOperLog.DocumentElement,
    "libraryCode",
    strLibraryCode);    // �������ڵĹݴ���
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "operation",
                    "amerce");
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "action",
                    "expire");
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "readerBarcode",
                    strReaderBarcode);

                XmlNode node_expiredOverdues = domOperLog.CreateElement("expiredOverdues");
                domOperLog.DocumentElement.AppendChild(node_expiredOverdues);

                string strReaderType = DomUtil.GetElementText(readerdom.DocumentElement,
    "readerType");

                bool bChanged = false;

                for (; ; )
                {

                    XmlNodeList nodes = readerdom.DocumentElement.SelectNodes("overdues/overdue");
                    if (nodes.Count == 0)
                        break;


                    // �ҵ���һ������������
                    XmlNode node = null;
                    string strPauseStart = "";
                    XmlNode node_firstOverdueItem = null;   // ��һ����ϳ�������(����һ�������˵�)��<overdue>Ԫ��
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        node = nodes[i];
                        string strTempOverduePeriod = DomUtil.GetAttr(node, "overduePeriod");
                        if (String.IsNullOrEmpty(strTempOverduePeriod) == true)
                            continue;   // ������Щû��overduePeriod��Ԫ��

                        if (node_firstOverdueItem == null)
                            node_firstOverdueItem = node;
                        strPauseStart = DomUtil.GetAttr(node, "pauseStart");
                        if (String.IsNullOrEmpty(strPauseStart) == false)
                            goto FOUND;
                    }

                    // û���ҵ����������������Ҫ�ѵ�һ��������������������
                    if (node_firstOverdueItem != null)
                    {
                        DomUtil.SetAttr(node_firstOverdueItem,
                            "pauseStart",
                            this.Clock.GetClock());
                        bChanged = true;
                        continue;   // ����ִ��ˢ�²����ƺ�û�б�Ҫ����Ϊû�иտ�ʼ�����������ģ�
                    }
                    break;
                FOUND:

                    string strUnit = "";
                    long lOverduePeriod = 0;

                    string strOverduePeriod = DomUtil.GetAttr(node, "overduePeriod");

                    // �������޲���
                    nRet = ParsePeriodUnit(strOverduePeriod,
                        out lOverduePeriod,
                        out strUnit,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    long lResultValue = 0;
                    string strPauseCfgString = "";

                    // ������ͣ�����ͣ������ֵ
                    nRet = ComputePausePeriodValue(strReaderType,
                        strLibraryCode,
                        lOverduePeriod,
                        out lResultValue,
                        out strPauseCfgString,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    lOverduePeriod = lResultValue;

                    DateTime timeStart = DateTimeUtil.FromRfc1123DateTimeString(strPauseStart);

                    nRet = RoundTime(strUnit,
                        ref timeStart,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    DateTime timeNow = this.Clock.UtcNow;
                    nRet = RoundTime(strUnit,
                        ref timeNow,
                        out strError);
                    if (nRet == -1)
                        return -1;


                    DateTime nextWorkingDay = new DateTime(0);
                    long lDistance = 0;
                    // ����ʱ��֮��ľ���
                    // parameters:
                    //      calendar    �������������Ϊnull����ʾ���������зǹ������жϡ�
                    // return:
                    //      -1  ����
                    //      0   �ɹ���timeEnd�ڹ����շ�Χ�ڡ�
                    //      1   �ɹ���timeEnd�����ڷǹ����ա�nextWorkingDay�Ѿ���������һ�������յ�ʱ��
                    nRet = GetTimeDistance(
                        null,   // Calendar calendar,
                        strUnit,
                        timeStart,
                        timeNow,
                        out lDistance,
                        out nextWorkingDay,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    long lDelta = lDistance - lOverduePeriod;

                    if (lDelta < 0)
                        break;  // �Ѿ������õ�������δ����

                    // �����Ѿ��ͷ����ڵ�<overdue>Ԫ��
                    DomUtil.SetAttr(node, "pauseStart", "");
                    Debug.Assert(node.ParentNode != null);
                    if (node.ParentNode != null)
                    {
                        // �����¼���־
                        XmlDocumentFragment fragment = domOperLog.CreateDocumentFragment();
                        fragment.InnerXml = node.OuterXml;
                        node_expiredOverdues.AppendChild(fragment);

                        nExpiredCount++;

                        // �����ڵ�<overdue>Ԫ�شӶ��߼�¼��ɾ��
                        node.ParentNode.RemoveChild(node);
                        bChanged = true;

                        // д��ͳ��ָ��
                        // �����������������Ƕ��߸���
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            "����",
                            "��ͣ���������",
                            1);
                    }

                    // TODO: �����¼���־����¼������������Ķ���


                    // ������һ������overduePeriod���Ե�<overdue>Ԫ��
                    nodes = readerdom.DocumentElement.SelectNodes("overdues/overdue");
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        node = nodes[i];
                        strPauseStart = DomUtil.GetAttr(node, "pauseStart");
                        if (String.IsNullOrEmpty(strPauseStart) == true)
                            goto FOUND_1;
                    }

                    break;// û���ҵ���һ����������������
                FOUND_1:

                    TimeSpan delta;

                    // ����TimeSpan
                    nRet = BuildTimeSpan(
                        lDelta,
                        strUnit,
                        out delta,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    DateTime timeLastEnd = timeNow - delta;

                    // �ѵ�һ����������Ϊ����
                    // ��������������һ������ڵ����ӣ������ǽ���
                    DomUtil.SetAttr(nodes[0],
                        "pauseStart",
                        DateTimeUtil.Rfc1123DateTimeStringEx(timeLastEnd.ToLocalTime()));
                    bChanged = true;

                    // д��ͳ��ָ��
                    // �����������������Ƕ��߸���
                    if (this.Statis != null)
                        this.Statis.IncreaseEntryValue(
                        strLibraryCode,
                        "����",
                        "��ͣ������������",
                        1);


                    // TODO: �����¼���־����¼��������Ķ���

                    // ��Ҫ����ˢ�£���Ϊ������������������Ͼ͵���
                } // end of for

                if (nExpiredCount > 0)
                {
                    string strOperTime = this.Clock.GetClock();

                    DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                        strUserID);
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                        strOperTime);

                    // 2012/5/7
                    // �޸�ǰ�Ķ��߼�¼
                    XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
    "oldReaderRecord", strOldReaderXml);   // 2014/3/8 ��ǰ oldReeaderRecord
                    DomUtil.SetAttr(node, "recPath", strReaderRecPath);

                    // ��־�а����޸ĺ�Ķ��߼�¼
                    node = DomUtil.SetElementText(domOperLog.DocumentElement,
                        "readerRecord", readerdom.OuterXml);
                    DomUtil.SetAttr(node, "recPath", strReaderRecPath);

                    nRet = this.OperLog.WriteOperLog(domOperLog,
                        strClientAddress,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "Refresh Pause Borrowing ���� д����־ʱ��������: " + strError;
                        return -1;
                    }
                }

                return bChanged == true ? 1 : 0;

            } // end of if 

            return 0;
        }

        // ���飺�ڶ��߼�¼��ȥ��������Ϣ����ȥ���Ľ�����Ϣ������ʷ�ֶ�
        // ���볬�ڼ�龯��
        // parameters:
        //      strItemBarcodeParam return() API �е� strItemBarcodeParam�����ܰ��� @refID: ǰ׺����
        //      strItemBarcode  ���¼�е� <barcode> Ԫ������
        //      strDeletedBorrowFrag ���شӶ��߼�¼��ɾ����<borrow>Ԫ��xmlƬ���ַ���(OuterXml)
        int DoReturnReaderXml(
            string strLibraryCode,
            ref XmlDocument readerdom,
            string strItemBarcodeParam,
            string strItemBarcode,
            string strOverdueString,
            string strReturnOperator,
            string strOperTime,
            string strClientAddress,
            out string strDeletedBorrowFrag,
            out string strError)
        {
            strError = "";
            strDeletedBorrowFrag = "";
            int nRet = 0;

            // ��ʱ strItemBarcodeParam �п����� refID: ǰ׺����

            if (String.IsNullOrEmpty(strItemBarcodeParam) == true)
            {
                strError = "������Ų���Ϊ��";
                return -1;
            }

            XmlNode nodeBorrow = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcodeParam + "']");
            if (nodeBorrow == null)
            {
                // �ٳ���һ��ֱ���� �������
                if (string.IsNullOrEmpty(strItemBarcode) == false)
                    nodeBorrow = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcode + "']");
                if (nodeBorrow == null)
                {
                    strError = "�ڶ��߼�¼�����ö��߲�δ�����Ĺ��� '" + strItemBarcodeParam + "'��";
                    return -1;
                }
            }

            // ɾ��ǰ��������
            strDeletedBorrowFrag = nodeBorrow.OuterXml;

            // ɾ�����Ĳ���Ϣ
            nodeBorrow.ParentNode.RemoveChild(nodeBorrow);

            // ��������Ϣ
            if (String.IsNullOrEmpty(strOverdueString) != true)
            {
                XmlDocumentFragment fragment = readerdom.CreateDocumentFragment();
                fragment.InnerXml = strOverdueString;

                // �����������Ƿ���overduesԪ��
                XmlNode root = readerdom.DocumentElement.SelectSingleNode("overdues");
                if (root == null)
                {
                    root = readerdom.CreateElement("overdues");
                    readerdom.DocumentElement.AppendChild(root);
                }

                // root.AppendChild(fragment);
                // ���뵽��ǰ��
                DomUtil.InsertFirstChild(root, fragment);


                if (StringUtil.IsInList("pauseBorrowing", this.OverdueStyle) == true)
                {
                    //
                    // ������ͣ������
                    // return:
                    //      -1  error
                    //      0   readerdomû���޸�
                    //      1   readerdom�������޸�
                    nRet = ProcessPauseBorrowing(
                        strLibraryCode,
                        ref readerdom,
                        "", // ��ΪactionΪstart������ʡ��
                        strReturnOperator,
                        "start",
                        strClientAddress,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "��������ͣ����Ĺ����з�������: " + strError;
                        return -1;
                    }
                }

            }

            // ���뵽������ʷ�ֶ���
            {
                // �����������Ƿ���borrowHistoryԪ��
                XmlNode root = readerdom.DocumentElement.SelectSingleNode("borrowHistory");
                if (root == null)
                {
                    root = readerdom.CreateElement("borrowHistory");
                    readerdom.DocumentElement.AppendChild(root);
                }

                if (this.MaxPatronHistoryItems > 0)
                {
                    XmlDocumentFragment fragment = readerdom.CreateDocumentFragment();
                    fragment.InnerXml = strDeletedBorrowFrag;

                    // ���뵽��ǰ��
                    XmlNode temp = DomUtil.InsertFirstChild(root, fragment);
                    if (temp != null)
                    {
                        // ���뻹��ʱ��
                        DomUtil.SetAttr(temp, "returnDate", strOperTime);

                        string strBorrowOperator = DomUtil.GetAttr(temp, "operator");
                        // ��ԭ����operator����ֵ���Ƶ�borrowOperator������
                        DomUtil.SetAttr(temp, "borrowOperator", strBorrowOperator);
                        // operator��ʱ��Ҫ��ʾ�����������
                        DomUtil.SetAttr(temp, "operator", strReturnOperator);

                    }
                }

                // �������100������ɾ�������
                while (root.ChildNodes.Count > this.MaxPatronHistoryItems)
                    root.RemoveChild(root.ChildNodes[root.ChildNodes.Count - 1]);

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

            return 0;
        }

        // ���һ������ǰ���������Ψһ���ַ���
        public string GetOverdueID()
        {
            // ���һ���Դ�Ӧ�������������������
            long lNumber = Interlocked.Increment(ref m_lSeed);

            // ��ô���ǰʱ���ticks
            long lTicks = DateTime.Now.Ticks;

            return lTicks.ToString() + "-" + lNumber.ToString();
        }

        // ���������ΥԼ��ļ۸��ַ���
        // �㷨��
        // 1) ���ַ��� RMB0.5YUAN/day ���Ϊ�������֡�prefix=RMB single_price=0.5 postfix=YUAN unit=day
        // 2) ���ճ��ڵ�ʱ�䣬���� singgle_price��Ȼ����� prefix �� postfic ���֣��Ϳ��Եõ�����ַ���������������Ҫ����һ��ʱ�䵥λ���㡣Ŀǰֻ֧���� day �� hour ֮�任��
        // ע���������� RMB0.5YUAN/day������ RMB0.5YUAN �мȰ�����ǰ׺��Ҳ�����˺�׺���Ǹ���������ӡ���ʵ��Ӧ���У�һ��ֻ��ǰ׺���֣����� "CNY0.5"
        // parameters:
        //      strPriceCfgString   ΥԼ�������ַ�������̬Ϊ 'CNY0.5/day'
        //      lDistance   ����ʱ����
        //      strPeriodUnit   ����ʱ�䵥λ
        //      strOverduePrice �������������ΥԼ��۸��ַ���
        // return:
        //      -1  error
        //      0   succeed
        int ComputeOverduePrice(
            string strPriceCfgString,
            long lDistance,
            string strPeriodUnit,
            out string strOverduePrice,
            out string strError)
        {
            strOverduePrice = "";
            strError = "";
            int nRet = 0;

            // ����strPriceCfgString����
            string strPriceBase = "";
            string strPerUnit = "day";

            // '/'����Ǽ۸��ұ���ʱ�䵥λ������ '0.5yuan/day'
            nRet = strPriceCfgString.IndexOf("/");
            if (nRet == -1)
            {
                strPriceBase = strPriceCfgString;
                strPerUnit = "day";
            }
            else
            {
                strPriceBase = strPriceCfgString.Substring(0, nRet).Trim();
                strPerUnit = strPriceCfgString.Substring(nRet + 1).Trim();
            }

            double fSinglePrice = 0.0F;
            string strPrefix = "";
            string strPostfix = "";

            nRet = ParsePriceUnit(strPriceBase,
                out strPrefix,
                out fSinglePrice,
                out strPostfix,
                out strError);
            if (nRet == -1)
            {
                strError = "��������ַ��� '" + strPriceBase + "' ʱ��������: " + strError;
                return -1;
            }

            // �������ʱ����Ŀ�ĵ�λ �� ����ΥԼ����ʱ�䵥λ ���÷���
            if (strPeriodUnit.ToLower() == strPerUnit.ToLower())
            {
                strOverduePrice = strPrefix + ((double)(fSinglePrice * lDistance)).ToString() + strPostfix;
                return 0;
            }

            if (strPeriodUnit.ToLower() == "day"
                && strPerUnit.ToLower() == "hour")
            {

                strOverduePrice = strPrefix + ((double)(fSinglePrice * lDistance * 24)).ToString() + strPostfix;
                return 0;
            }

            if (strPeriodUnit.ToLower() == "hour"
                && strPerUnit.ToLower() == "day")
            {

                strOverduePrice = strPrefix + ((double)((fSinglePrice * lDistance) / 24)).ToString() + strPostfix;
                return 0;
            }

            strError = "���õ� ����ʱ�䵥λ '" + strPeriodUnit + "' �� ΥԼ���ʱ�䵥λ '" + strPerUnit + "' ֮���޷����л��㡣";
            return -1;
        }

        // �������ʧͼ���ΥԼ��ļ۸��ַ���
        // parameters:
        //      strPriceCfgString   ΥԼ���ʡ���̬Ϊһ��С�������� '10.5'
        //      strItemPrice   ��ԭ�۸�
        //      strLostPrice �������������ΥԼ��۸��ַ���
        // return:
        //      -1  error
        //      0   succeed
        //      1   ��Ϊȱԭʼ�۸񣬴Ӷ�ֻ�ô����˴��ʺŵ���ʽ
        int ComputeLostPrice(
            string strPriceCfgString,
            string strItemPrice,
            out string strLostPrice,
            out string strError)
        {
            strLostPrice = "";
            strError = "";
            int nRet = 0;

            double ratio = 1.0;

            try
            {
                ratio = Convert.ToDouble(strPriceCfgString);
            }
            catch
            {
                strError = "ΥԼ�����������ַ��� '"+strPriceCfgString+"' ��ʽ����Ӧ��Ϊһ��С����";
                return -1;
            }

            // ���ԭʼ�۸�Ϊ��
            if (String.IsNullOrEmpty(strItemPrice) == true)
            {
                strLostPrice = "?*" + strPriceCfgString;
                return 1;
            }

            /*
            // ����۸��ַ����п��ܴ��ڵĳ˺š�����
            List<string> temp_prices = new List<string>();
            temp_prices.Add(strItemPrice);

            string strOutputPrice = "";
            // TODO: �ƺ�����SumPrices()
            nRet = PriceUtil.TotalPrice(temp_prices,
                out strOutputPrice,
                out strError);
            if (nRet == -1)
            {
                strError = "�����۸��ַ��� '" + strItemPrice + "' ʱ��������1: " + strError;
                return -1;
            }

            strItemPrice = strOutputPrice;
             * */
            // ���滯�۸��ַ���
            // ����۸��ַ����п��ܴ��ڵĳ˺š�����
            nRet = CanonicalizeItemPrice(ref strItemPrice,
                out strError);
            if (nRet == -1)
                return -1;

            double fItemPrice = 0.0F;
            string strPrefix = "";
            string strPostfix = "";   

            // ����ܹ�ͬʱ����ǰ׺�ͺ�׺
            nRet = ParsePriceUnit(strItemPrice,
                out strPrefix,
                out fItemPrice,
                out strPostfix,
                out strError);
            if (nRet == -1)
            {
                strError = "�����۸��ַ��� '" + strItemPrice + "' ʱ��������2: " + strError;
                return -1;
            }

            strLostPrice = strPrefix + (fItemPrice * ratio).ToString() + strPostfix;
            return 0;
        }

        // ���滯�۸��ַ���
        // ����۸��ַ����п��ܴ��ڵĳ˺š�����
        public static int CanonicalizeItemPrice(ref string strPrice,
            out string strError)
        {
            strError = "";

            List<string> temp_prices = new List<string>();
            temp_prices.Add(strPrice);

            string strOutputPrice = "";
            // TODO: �ƺ�����SumPrices()
            int nRet = PriceUtil.TotalPrice(temp_prices,
                out strOutputPrice,
                out strError);
            if (nRet == -1)
            {
                strError = "���滯�۸��ַ��� '" + strPrice + "' ʱ��������1: " + strError;
                return -1;
            }

            strPrice = strOutputPrice;
            return 0;
        }


        // �ڲ��¼��ɾ��������Ϣ
        // parameters:
        //      strOverdueString    ��ʾ������Ϣ���ַ�����borrowOperator���Ա�ʾ���Ĳ����ߣ�operator���Ա�ʾ���������
        // return:
        //      -1  ����
        //      0   ����
        //      1   ���ڻ�����߶�ʧ��������
        int DoReturnItemXml(
            // bool bLost,
            string strAction,
            SessionInfo sessioninfo,
            // Account account,
            Calendar calendar,
            string strReaderType,
            string strLibraryCode,
            string strAccessParameters,
            XmlDocument readerdom,
            ref XmlDocument itemdom,
            bool bForce,
            bool bItemBarcodeDup,
            string strItemRecPath,
            string strReturnOperator,
            string strOperTime,
            out string strOverdueString,
            out string strLostComment,
            out ReturnInfo return_info,
            out string strWarning,
            out string strError)
        {
            int nRet = 0;
            strError = "";
            strOverdueString = "";
            strLostComment = "";
            strWarning = "";

            Debug.Assert(String.IsNullOrEmpty(strItemRecPath) == false, "");

            string strActionName = GetReturnActionName(strAction);

            return_info = new ReturnInfo();

            LibraryApplication app = this;

            string strItemBarcode = DomUtil.GetElementText(itemdom.DocumentElement, "barcode");
            if (String.IsNullOrEmpty(strItemBarcode) == true)
            {
#if NO
                // text-level: �ڲ�����
                strError = "���¼�в�����Ų���Ϊ��";
                return -1;
#endif
                // ����������Ϊ�գ������ �ο�ID
                string strRefID = DomUtil.GetElementText(itemdom.DocumentElement,
    "refID");
                if (String.IsNullOrEmpty(strRefID) == true)
                {
                    // text-level: �ڲ�����
                    strError = "���¼�в�����źͲο�ID��ӦͬʱΪ��";
                    return -1;
                }
                strItemBarcode = "@refID:" + strRefID;
            }

            // �ݲصص�
            string strLocation = DomUtil.GetElementText(itemdom.DocumentElement, "location");
            // ȥ��#reservation����
            strLocation = StringUtil.GetPureLocationString(strLocation);

            // ��Ȼһ�����¼�Ѿ���������ˣ��Ǿ�������Ҫ���������ܲ�Ĺݲصص��Ƿ���������������ڵĹݲصص㡣������ֲ�һ�£���Ҫ����
            // ���������Ĺݲصص��Ƿ�϶������ڵĹݲصص��Ǻ�
                string strCode = "";
                string strRoom = "";
            {

                // ����
                ParseCalendarName(strLocation,
            out strCode,
            out strRoom);
                if (strCode != strLibraryCode)
                {
                    strWarning += "���¼�Ĺݲص� '" + strLocation + "' �����ڶ������ڹݴ��� '" + strLibraryCode + "'����ע���������";
                }
            }

            // ����ȡ����ݲص��б�
            if (string.IsNullOrEmpty(strAccessParameters) == false && strAccessParameters != "*")
            {
                bool bFound = false;
                List<string> locations = StringUtil.SplitList(strAccessParameters);
                foreach (string s in locations)
                {
                    string c = "";
                    string r = "";
                    ParseCalendarName(s,
                        out c,
                        out r);
                    if (/*string.IsNullOrEmpty(c) == false && */ c != "*")
                    {
                        if (c != strCode)
                            continue;
                    }

                    if (/*string.IsNullOrEmpty(r) == false && */ r != "*")
                    {
                        if (r != strRoom)
                            continue;
                    }

                    bFound = true;
                    break;
                }

                if (bFound == false)
                {
                    strError = strActionName + "�������ܾ�������¼�Ĺݲص� '" + strLocation + "' ���ڵ�ǰ�û���ȡ����涨�� "+strActionName+" �����Ĺݲص���ɷ�Χ '" + strAccessParameters + "' ֮��";
                    return -1;
                }
            }
            ///
            // �����Ƿ��ܹ�������
            bool bResultValue = false;
            string strMessageText = "";

            // ִ�нű�����ItemCanReturn
            // parameters:
            // return:
            //      -2  not found script
            //      -1  ����
            //      0   �ɹ�
            nRet = app.DoItemCanReturnScriptFunction(
                sessioninfo.Account,
                itemdom,
                out bResultValue,
                out strMessageText,
                out strError);
            if (nRet == -1)
            {
                strError = "ִ��CanReturn()�ű�����ʱ����: " + strError;
                return -1;
            }
            if (nRet == -2)
            {
            }
            else
            {
                // ���ݽű����ؽ��
                if (bResultValue == false)
                {
                    strError = "����ʧ�ܡ���Ϊ�� " + strItemBarcode + " ��״̬Ϊ " + strMessageText;
                    return -1;
                }
            }

            // 
            // ������ի�ļ��
            string strPersonalLibrary = "";
            if (sessioninfo.UserType == "reader"
                && sessioninfo.Account != null)
                strPersonalLibrary = sessioninfo.Account.PersonalLibrary;

            if (string.IsNullOrEmpty(strPersonalLibrary) == false)
            {
                if (strRoom != "*" && StringUtil.IsInList(strRoom, strPersonalLibrary) == false)
                {
                    strError = "����ʧ�ܡ���ǰ�û� '" + sessioninfo.Account.Barcode + "' ֻ�ܲ����ݴ��� '" + strLibraryCode + "' �еص�Ϊ '" + strPersonalLibrary + "' ��ͼ�飬���ܲ����ص�Ϊ '" + strRoom + "' ��ͼ��";
                    return -1;
                }
            }

            bool bOverdue = false;

            string strOverdueMessage = "";

            // ͼ������
            string strBookType = DomUtil.GetElementText(itemdom.DocumentElement, "bookType");

            string strBorrowDate = DomUtil.GetElementText(itemdom.DocumentElement, "borrowDate");
            string strPeriod = DomUtil.GetElementText(itemdom.DocumentElement, "borrowPeriod");


            // ���ǽ���ʱ�Ĳ�����
            string strBorrowOperator = DomUtil.GetElementText(itemdom.DocumentElement, "operator");

            // ��״̬
            string strState = DomUtil.GetElementText(itemdom.DocumentElement,
                "state");
            string strComment = DomUtil.GetElementText(itemdom.DocumentElement,
                "comment");
            string strBorrower = DomUtil.GetElementText(itemdom.DocumentElement,
                "borrower");

            // ��۸�
            string strItemPrice = DomUtil.GetElementText(itemdom.DocumentElement, "price");


            if (strAction == "lost"
                && String.IsNullOrEmpty(strItemPrice) == true
                && bForce == false)
            {
                strError = "��۸�(<price>Ԫ��)Ϊ�գ��޷����㶪ʧͼ��ΥԼ����������Ϊ�ò����۸���Ϣ�������½��ж�ʧ��������";
                return -1;
            }

            DateTime borrowdate = new DateTime((long)0);

            try
            {
                borrowdate = DateTimeUtil.FromRfc1123DateTimeString(strBorrowDate);
            }
            catch
            {
                if (bForce == true)
                    goto DOCHANGE;
                strError = "���������ַ��� '" + strBorrowDate + "' ��ʽ����";
                return -1;
            }

            // �����Ƿ���
            string strPeriodUnit = "";
            long lPeriodValue = 0;

            nRet = LibraryApplication.ParsePeriodUnit(strPeriod,
                out lPeriodValue,
                out strPeriodUnit,
                out strError);
            if (nRet == -1)
            {
                if (bForce == true)
                    goto DOCHANGE;
                strError = "���¼�н�������ֵ '" + strPeriod + "' ��ʽ����: " + strError;
                return -1;
            }

            DateTime timeEnd = DateTime.MinValue;
            DateTime nextWorkingDay = DateTime.MinValue;

            // ���㻹������
            // parameters:
            //      calendar    �������������Ϊnull����ʾ���������зǹ������жϡ�
            // return:
            //      -1  ����
            //      0   �ɹ���timeEnd�ڹ����շ�Χ�ڡ�
            //      1   �ɹ���timeEnd�����ڷǹ����ա�nextWorkingDay�Ѿ���������һ�������յ�ʱ��
            nRet = LibraryApplication.GetReturnDay(
                calendar,
                borrowdate,
                lPeriodValue,
                strPeriodUnit,
                out timeEnd,
                out nextWorkingDay,
                out strError);
            if (nRet == -1)
            {
                if (bForce == true)
                    goto DOCHANGE;
                strError = "���㻹�����ڹ��̷�������: " + strError;
                return -1;
            }

            return_info.LatestReturnTime = DateTimeUtil.Rfc1123DateTimeStringEx(timeEnd.ToLocalTime());

            bool bEndInNonWorkingDay = false;
            if (nRet == 1)
            {
                // �����ڷǹ�����
                bEndInNonWorkingDay = true;
            }

            DateTime now = app.Clock.UtcNow;  //  ����  ����

            // ���滯ʱ��
            DateTime now_rounded = now;
            nRet = RoundTime(strPeriodUnit,
                ref now_rounded,
                out strError);
            if (nRet == -1)
                return -1;

            TimeSpan delta = now_rounded - timeEnd;

            long lOver = 0;
            long lDelta = 0;
            long lDelta1 = 0;   // У�������ǹ����գ���Ĳ��

            nRet = ParseTimeSpan(
                delta,
                strPeriodUnit,
                out lDelta,
                out strError);
            if (nRet == -1)
                return -1;

            TimeSpan delta1 = new TimeSpan(0);
            if (bEndInNonWorkingDay == true)
            {
                delta1 = now_rounded - nextWorkingDay;

                nRet = ParseTimeSpan(
    delta1,
    strPeriodUnit,
    out lDelta1,
    out strError);
                if (nRet == -1)
                    return -1;
            }
            else
            {
                delta1 = delta;
                lDelta1 = lDelta;
            }

            if (lDelta1 > 0)
            {
                string strOverduePrice = "";


                // ��� '����ΥԼ������' ���ò���
                string strPriceCfgString = "";
                MatchResult matchresult;
                // return:
                //      reader��book���;�ƥ�� ��4��
                //      ֻ��reader����ƥ�䣬��3��
                //      ֻ��book����ƥ�䣬��2��
                //      reader��book���Ͷ���ƥ�䣬��1��
                nRet = app.GetLoanParam(
                    //null,
                    strLibraryCode,
                    strReaderType,
                    strBookType,
                    "����ΥԼ������",
                    out strPriceCfgString,
                    out matchresult,
                    out strError);
                if (nRet == -1)
                {
                    if (bForce == true)
                        goto CONTINUE_OVERDUESTRING;
                    strError = "����ʧ�ܡ���� �ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ����ΥԼ������ ����ʱ��������: " + strError;
                    return -1;
                }
                if (nRet < 4) // nRet == 0
                {
                    if (bForce == true)
                        goto CONTINUE_OVERDUESTRING;

                    strError = "����ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ����ΥԼ������ �����޷����: " + strError;
                    return -1;
                }

                // long lOver = 0;
                // ���<amerce overdueStyle="...">�а�����includeNoneworkingDay����ʾ�����������հ�����ĩβ�ǹ����յ��㷨�����򣬾��ǲ�����ĩβ�ǹ����գ��ӵ�һ�������տ�ʼ����ĳ���������
                if (StringUtil.IsInList("includeNoneworkingDay", this.OverdueStyle) == true)
                    lOver = lDelta;
                else
                    lOver = lDelta1;

                nRet = ComputeOverduePrice(
                    strPriceCfgString,
                    lDelta1,    // ���յ�����Ĳ�����
                    strPeriodUnit,
                    out strOverduePrice,
                    out strError);
                if (nRet == -1)
                {
                    if (bForce == true)
                        goto CONTINUE_OVERDUESTRING;

                    strError = "����ʧ�ܡ����㳬��ΥԼ��۸�ʱ����: " + strError;
                    return -1;
                }

            CONTINUE_OVERDUESTRING:

                strOverdueMessage += "����ʱ�ѳ����������� " + Convert.ToString(lOver) + GetDisplayTimeUnitLang(strPeriodUnit) + "�������г���������";

                // �����XmlTextWriter����DOM������strOverdueString
                XmlDocument tempdom = new XmlDocument();
                tempdom.LoadXml("<overdue />");
                DomUtil.SetAttr(tempdom.DocumentElement, "barcode", strItemBarcode);

                if (bItemBarcodeDup == true)
                {
                    // ����������Զ�λ���򲻼���ʵ���¼·��
                    DomUtil.SetAttr(tempdom.DocumentElement, "recPath", strItemRecPath);
                }

                string strReason = "���ڡ��� " + (lOver).ToString() + GetDisplayTimeUnitLang(strPeriodUnit) + "; ΥԼ������: " + strPriceCfgString;
                DomUtil.SetAttr(tempdom.DocumentElement, "reason", strReason);

                // ����ʱ�䳤�� 2007/12/17 new add
                DomUtil.SetAttr(tempdom.DocumentElement, "overduePeriod", (lOver).ToString() + strPeriodUnit);

                DomUtil.SetAttr(tempdom.DocumentElement, "price", strOverduePrice);
                DomUtil.SetAttr(tempdom.DocumentElement, "borrowDate", strBorrowDate);
                DomUtil.SetAttr(tempdom.DocumentElement, "borrowPeriod", strPeriod);
                DomUtil.SetAttr(tempdom.DocumentElement, "returnDate", DateTimeUtil.Rfc1123DateTimeStringEx(now.ToLocalTime()));
                DomUtil.SetAttr(tempdom.DocumentElement, "borrowOperator", strBorrowOperator);
                DomUtil.SetAttr(tempdom.DocumentElement, "operator", strReturnOperator);
                // id������Ψһ��, Ϊ��ΥԼ��C/S���洴������������
                string strOverdueID = GetOverdueID();
                DomUtil.SetAttr(tempdom.DocumentElement, "id", strOverdueID);

                strOverdueString = tempdom.DocumentElement.OuterXml;

                /*
                strOverdueString = "<overdue barcode='" + strItemBarcode
                    + "' over='" + Convert.ToString(lOver) + strPeriodUnit
                    + "' borrowDate='" + strBorrowDate
                    + "' borrowPeriod='" + strPeriod 
                    + "' returnDate='" + DateTimeUtil.Rfc1123DateTimeString(now) 
                    + "' operator='" + strOperator
                    + "' id='" + GetOverdueID() + "'/>";
                 */

                bOverdue = true;
            }

            if (strAction == "lost")
            {
                string strLostPrice = "?";
                string strReason = "��ʧ��";

                string strBiblioRecID = DomUtil.GetElementText(itemdom.DocumentElement, "parent");  //
                string strItemDbName = ResPath.GetDbName(strItemRecPath);
                string strBiblioDbName = "";
                // ����ʵ�����, �ҵ���Ӧ����Ŀ����
                // return:
                //      -1  ����
                //      0   û���ҵ�
                //      1   �ҵ�
                nRet = this.GetBiblioDbNameByItemDbName(strItemDbName,
                    out strBiblioDbName,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    strError = "ʵ����� '" + strItemDbName + "' û���ҵ���Ӧ����Ŀ����";
                    return -1;
                }
                string strBiblioRecPath = strBiblioDbName + "/" + strBiblioRecID;


                int nResultValue = 0;
                string strTempReason = "";
                // ִ�нű�����GetLost
                // ���ݵ�ǰ���߼�¼��ʵ���¼����Ŀ��¼���������ʧ����⳥���
                // parameters:
                // return:
                //      -2  not found script
                //      -1  ����
                //      0   �ɹ�
                nRet = this.DoGetLostScriptFunction(
                    sessioninfo,
            readerdom,
            itemdom,
            strBiblioRecPath,
            out nResultValue,
            out strLostPrice,
            out strTempReason,
            out strError);
                if (nRet == -1)
                {
                    strError = "���ýű�����GetLost()ʱ����: " + strError;
                    return -1;
                }

                if (nRet == 0)
                {
                    if (nResultValue == -1)
                    {
                        strError = "(�ű�����)���㶪ʧ�⳥���ʱ����: " + strError;
                        return -1;
                    }

                    strReason += strTempReason;
                }
                // û�з��ֽű�������������ñ����ҵ�����������
                else if (nRet == -2)
                {
                    // ��� '��ʧΥԼ������' ���ò���
                    string strPriceCfgString = "";
                    MatchResult matchresult;
                    // return:
                    //      reader��book���;�ƥ�� ��4��
                    //      ֻ��reader����ƥ�䣬��3��
                    //      ֻ��book����ƥ�䣬��2��
                    //      reader��book���Ͷ���ƥ�䣬��1��
                    nRet = app.GetLoanParam(
                        //null,
                        strLibraryCode,
                        strReaderType,
                        strBookType,
                        "��ʧΥԼ������",
                        out strPriceCfgString,
                        out matchresult,
                        out strError);
                    if (nRet == -1)
                    {
                        if (bForce == true)
                            goto CONTINUE_LOSTING;
                        strError = "��ʧ����ʧ�ܡ���� �ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ��ʧΥԼ������ ����ʱ��������: " + strError;
                        return -1;
                    }
                    if (nRet < 4)  // nRet == 0
                    {
                        if (bForce == true)
                            goto CONTINUE_LOSTING;

                        strError = "����ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ��ʧΥԼ������ �����޷����: " + strError;
                        return -1;
                    }


                    nRet = ComputeLostPrice(
                        strPriceCfgString,
                        strItemPrice,
                        out strLostPrice,
                        out strError);
                    if (nRet == -1)
                    {
                        if (bForce == true)
                            goto CONTINUE_LOSTING;

                        strError = "��ʧ����ʧ�ܡ����㶪ʧΥԼ��۸�ʱ����: " + strError;
                        return -1;
                    }

                    strReason = "��ʧ��ԭ�۸�: " + strItemPrice + "; ΥԼ������:" + strPriceCfgString;
                }
                else
                {
                    Debug.Assert(false, "");
                }

            CONTINUE_LOSTING:

                strOverdueMessage += "�ж�ʧΥԼ�� " + strLostPrice + "�������и�ΥԼ��������";

                // ����strOverdueString
                XmlDocument tempdom = new XmlDocument();
                tempdom.LoadXml("<overdue />");
                DomUtil.SetAttr(tempdom.DocumentElement, "barcode", strItemBarcode);
                DomUtil.SetAttr(tempdom.DocumentElement, "reason", strReason);
                DomUtil.SetAttr(tempdom.DocumentElement, "price", strLostPrice);
                DomUtil.SetAttr(tempdom.DocumentElement, "borrowDate", strBorrowDate);
                DomUtil.SetAttr(tempdom.DocumentElement, "borrowPeriod", strPeriod);
                DomUtil.SetAttr(tempdom.DocumentElement, "returnDate", DateTimeUtil.Rfc1123DateTimeStringEx(now.ToLocalTime()));
                DomUtil.SetAttr(tempdom.DocumentElement, "borrowOperator", strBorrowOperator);
                DomUtil.SetAttr(tempdom.DocumentElement, "operator", strReturnOperator);
                // id������Ψһ��, Ϊ��ΥԼ��C/S���洴������������
                string strOverdueID = GetOverdueID();
                DomUtil.SetAttr(tempdom.DocumentElement, "id", strOverdueID);

                strOverdueString += tempdom.DocumentElement.OuterXml;

                strLostComment = "������ " + DateTimeUtil.Rfc1123DateTimeStringEx(now.ToLocalTime()) + " �ɶ��� " + strBorrower + " ������ʧ��ΥԼ���¼idΪ " + strOverdueID + "�����һ�ν��ĵ��������: ��������: " + strBorrowDate + "; ��������: " + strPeriod + "��";
            }


        DOCHANGE:

            XmlNode nodeOldBorrower = null;

            // ���뵽������ʷ�ֶ���
            {
                // �����������Ƿ���borrowHistoryԪ��
                XmlNode root = itemdom.DocumentElement.SelectSingleNode("borrowHistory");
                if (root == null)
                {
                    root = itemdom.CreateElement("borrowHistory");
                    itemdom.DocumentElement.AppendChild(root);
                }


                if (this.MaxItemHistoryItems > 0)
                {
                    nodeOldBorrower = itemdom.CreateElement("borrower");
                    // ���뵽��ǰ��
                    XmlNode temp = DomUtil.InsertFirstChild(root, nodeOldBorrower);
                    if (temp != null)
                    {
                        // ���뻹��ʱ��
                        DomUtil.SetAttr(temp, "returnDate", strOperTime);
                    }
                }

                // �������100������ɾ�������
                while (root.ChildNodes.Count > this.MaxItemHistoryItems)
                    root.RemoveChild(root.ChildNodes[root.ChildNodes.Count - 1]);

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

            if (nodeOldBorrower != null)
                DomUtil.SetAttr(nodeOldBorrower,
                    "barcode",
                    DomUtil.GetElementText(itemdom.DocumentElement, "borrower"));
            // DomUtil.SetElementText(itemdom.DocumentElement, "borrower", "");
            DomUtil.DeleteElement(itemdom.DocumentElement,
    "borrower");

            // 2009/9/18 new add
            //DomUtil.SetElementText(itemdom.DocumentElement,
            //    "borrowerReaderType", "");
            DomUtil.DeleteElement(itemdom.DocumentElement,
    "borrowerReaderType");
            // 2012/9/8
            //DomUtil.SetElementText(itemdom.DocumentElement,
            //    "borrowerRecPath", "");
            DomUtil.DeleteElement(itemdom.DocumentElement,
"borrowerRecPath");

            if (nodeOldBorrower != null)
                DomUtil.SetAttr(nodeOldBorrower,
               "borrowDate",
               DomUtil.GetElementText(itemdom.DocumentElement, "borrowDate"));
            //DomUtil.SetElementText(itemdom.DocumentElement,
            //    "borrowDate", "");
            DomUtil.DeleteElement(itemdom.DocumentElement,
"borrowDate");

            if (nodeOldBorrower != null)
                DomUtil.SetAttr(nodeOldBorrower,
               "borrowPeriod",
               DomUtil.GetElementText(itemdom.DocumentElement, "borrowPeriod"));
            //DomUtil.SetElementText(itemdom.DocumentElement,
            //    "borrowPeriod", "");
            DomUtil.DeleteElement(itemdom.DocumentElement,
"borrowPeriod");

            // 2014/11/14
            if (nodeOldBorrower != null)
            {
                string strValue = DomUtil.GetElementText(itemdom.DocumentElement, "returningDate");
                if (string.IsNullOrEmpty(strValue) == false)
                    DomUtil.SetAttr(nodeOldBorrower,
                        "returningDate",
                        strValue);
            }
            DomUtil.DeleteElement(itemdom.DocumentElement,
                "returningDate");

            // 2014/11/14
            DomUtil.DeleteElement(itemdom.DocumentElement,
                "lastReturningDate");

            // string strBorrowOperator = DomUtil.GetElementText(itemdom.DocumentElement, "operator");
            //DomUtil.SetElementText(itemdom.DocumentElement,
            //    "operator", "");    // ���
            DomUtil.DeleteElement(itemdom.DocumentElement,
"operator");


            // item��ԭoperatorԪ��ֵ��ʾ���Ĳ����ߣ���ʱӦת����ʷ�е�borrowOperatorԪ����
            if (nodeOldBorrower != null)
                DomUtil.SetAttr(nodeOldBorrower,
                "borrowOperator",
                strBorrowOperator);

            // ������ʷ��operator����ֵ��ʾ���������
            if (nodeOldBorrower != null)
                DomUtil.SetAttr(nodeOldBorrower,
                "operator",
                strReturnOperator);

            // 2011/6/28
            return_info.BorrowOperator = strBorrowOperator;
            return_info.ReturnOperator = strReturnOperator;

            string strNo = DomUtil.GetElementText(itemdom.DocumentElement, "no");
            if (nodeOldBorrower != null)
            {
                if (string.IsNullOrEmpty(strNo) == false
                && strNo != "0")    // 2013/12/23
                    DomUtil.SetAttr(nodeOldBorrower,
                        "no",
                        strNo);
            }
            DomUtil.DeleteElement(itemdom.DocumentElement,
                "no");

            string strRenewComment = DomUtil.GetElementText(itemdom.DocumentElement, "renewComment");
            if (nodeOldBorrower != null)
            {
                if (string.IsNullOrEmpty(strRenewComment) == false) // 2013/12/23
                    DomUtil.SetAttr(nodeOldBorrower,
                        "renewComment",
                        strRenewComment);
            }
            DomUtil.DeleteElement(itemdom.DocumentElement,
                "renewComment");

            if (nodeOldBorrower != null)
            {
                if (strAction == "lost"
                && strLostComment != "")
                {
                    DomUtil.SetAttr(nodeOldBorrower,
        "state",
        strState);
                    DomUtil.SetAttr(nodeOldBorrower,
                        "comment",
                        strComment);

                    /*
                    if (String.IsNullOrEmpty(strState) == false)
                        strState += ",";
                    strState += "��ʧ";
                     * */

                    StringUtil.SetInList(ref strState,
                "��ʧ",
                true);

                    DomUtil.SetElementText(itemdom.DocumentElement,
                        "state", strState);
                    if (strLostComment != "")
                    {
                        if (String.IsNullOrEmpty(strComment) == false)
                            strComment += "\r\n";
                        strComment += strLostComment;
                        DomUtil.SetElementText(itemdom.DocumentElement,
                            "comment", strComment);
                    }
                }
            }

            //  ͳ��ָ��
            {
                TimeSpan delta_0 = this.Clock.UtcNow - borrowdate;
                if (delta_0.TotalDays < 1)
                {
                    if (this.Statis != null)
                        this.Statis.IncreaseEntryValue(
                        strLibraryCode,
                        "����",
                        "��������������",
                        1);
                }

            }

            // return_info
            return_info.BorrowTime = strBorrowDate;
            return_info.Period = strPeriod;
            return_info.OverdueString = strOverdueString;
            // string strNo = DomUtil.GetElementText(itemdom.DocumentElement, "no");
            return_info.BorrowCount = 0;
     
            // 2012/3/28
            if (string.IsNullOrEmpty(strNo) == false)
                Int64.TryParse(strNo, out return_info.BorrowCount);
#if NO
            try
            {
                return_info.BorrowCount = Convert.ToInt32(strNo);
            }
            catch
            {
                return_info.BorrowCount = 0;
            }
#endif

            return_info.BookType = strBookType;
            /*
            string strLocation = DomUtil.GetElementText(itemdom.DocumentElement,
                "location");
             * */
            return_info.Location = strLocation;

            if (bOverdue == true
                || strAction == "lost")
            {
                strError = strOverdueMessage;
                return 1;
            }

            return 0;
        }

        // �����Բ��¼�е�ԤԼ��Ϣ���м��ʹ���
        // �㷨�ǣ��ҵ���һ��û�г���(expireDate)����state����arrived��<request>Ԫ�أ�
        // �������Ԫ�ص�reader���ԣ��������һ��ԤԼ�ߣ������Ұ�����ҵ���<request>
        // Ԫ�ص�state���Դ���arrived��ǡ�
        // ���Ϊ��ʧ�����������ĵ�������Ҫ֪ͨ�ȴ��ߣ����Ѿ���ʧ�ˣ������ٵȴ�
        // parameters:
        //      bMaskLocationReservation    ��Ҫ��<location>����#reservation���
        // return:
        //      -1  error
        //      0   û���޸�
        //      1   �Բ��¼���й��޸ġ����й��޸ģ���һ������strReservationReaderBarcode���޸Ŀ�����˳��ɾ���˹��ڵ�<request>Ԫ��
        internal int DoItemReturnReservationCheck(
            bool bDontMaskLocationReservation,
            ref XmlDocument itemdom,
            out string strReservationReaderBarcode,
            out string strError)
        {
            strReservationReaderBarcode = "";
            strError = "";
            bool bChanged = false;

            // �ҵ�����<reservations/request>Ԫ��
            XmlNodeList nodes = itemdom.DocumentElement.SelectNodes("reservations/request");
            if (nodes.Count == 0)
                return 0;   // û���ҵ�<request>Ԫ��, Ҳ����˵��û�б�ԤԼ

            XmlNode node = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                node = nodes[i];
                string strExpireDate = DomUtil.GetAttr(node, "expireDate");
                // ���������Ƿ����
                if (String.IsNullOrEmpty(strExpireDate) == false)
                {
                    DateTime expiredate = DateTimeUtil.FromRfc1123DateTimeString(strExpireDate);
                    DateTime now = this.Clock.UtcNow;   // 2007/12/17 changed //  DateTime.UtcNow;
                    if (expiredate > now)
                    {
                        // TODO: ���ڵ�ԤԼ�����Ƿ��Զ�ɾ��?
                        node.ParentNode.RemoveChild(node);
                        bChanged = true;
                        continue;
                    }
                }

                // ����״̬�ǲ���arrived
                string strState = DomUtil.GetAttr(node, "state");
                if (strState == "arrived")
                {
                    // ɾ����ǰ�����ģ�״̬Ϊarrived��<request>Ԫ��
                    node.ParentNode.RemoveChild(node);
                    bChanged = true;
                    continue;
                }

                goto FOUND;
            }

            if (bChanged == false)
                return 0;   // not changed
            return 1;   // ��Ȼû���ҵ������ǲ��¼�������޸�
        FOUND:

            Debug.Assert(node != null, "");
            strReservationReaderBarcode = DomUtil.GetAttr(node, "reader");

            if (String.IsNullOrEmpty(strReservationReaderBarcode) == true)
            {
                strError = "<request>Ԫ����reader����ֵΪ��";
                return -1;
            }

            /*
            // ɾ��<request>Ԫ��
            node.ParentNode.RemoveChild(node);
             * */
            // ��<request>Ԫ�ص�state����ֵ�޸�Ϊarrived
            DomUtil.SetAttr(node, "state", "arrived");
            // ����ʱ��
            DomUtil.SetAttr(node, "arrivedDate", this.Clock.GetClock());

            bChanged = true;

            if (bDontMaskLocationReservation == false)
            {
                // �޸�<location>Ԫ��,����һ��#reservation�о�ֵ
                string strText = DomUtil.GetElementText(itemdom.DocumentElement, "location");
                if (strText == null)
                    strText = "";

                if (StringUtil.IsInList("#reservation", strText) == false)
                {
                    if (strText != "")
                        strText += ",";
                    strText += "#reservation";
                }

                DomUtil.SetElementText(itemdom.DocumentElement, "location", strText);
                bChanged = true;
            }

            return 1;
        }

        // ���smtp������������Ϣ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int GetSmtpServerCfg(
            out string strAddress,
            out string strManagerEmail,
            out string strError)
        {
            strAddress = "";
            strManagerEmail = "";
            strError = "";
            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode(
                "smtpServer");
            if (node == null)
            {
                strError = "��library.xml��û���ҵ�<smtpServer>Ԫ��";
                return 0;
            }
            strAddress = DomUtil.GetAttr(node, "address");

            if (String.IsNullOrEmpty(strAddress) == true)
            {
                strError = "<smtpServer>δ����address����ֵ��";
                return -1;
            }

            strManagerEmail = DomUtil.GetAttr(node, "managerEmail");

            if (String.IsNullOrEmpty(strManagerEmail) == true)
            {
                strError = "<smtpServer>δ����managerEmail����ֵ��";
                return -1;
            }

            return 1;
        }

#if NOOOOOOOOOOOOOO
        // ����֪ͨemail
        // return:
        //      -1  error
        //      0   not found smtp server cfg
        //      1   succeed
        public int SendEmail(string strUserEmail,
            string strSubject,
            string strBody,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strManagerEmail = "";

            string strSmtpServerAddress = "";
                    // ���smtp������������Ϣ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
            nRet = GetSmtpServerCfg(
                out strSmtpServerAddress,
                out strManagerEmail,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;   // not found cfg

            if (String.IsNullOrEmpty(strSmtpServerAddress) == true)
                strSmtpServerAddress = "127.0.0.1";
            

            MailMessage Message = new MailMessage();
            Message.To = strUserEmail;	// To
            Message.From = strManagerEmail; // From
            Message.Subject = strSubject;
            Message.Body = strBody;	// Body

            try
            {
                SmtpMail.SmtpServer = strSmtpServerAddress;
                SmtpMail.Send(Message);
            }
            catch (Exception ex/*System.Web.HttpException ehttp*/)
            {
                strError = GetInnerMessage(ex);
                return -1;
            }


            return 1;
        }
#endif

        // ����֪ͨemail
        // return:
        //      -1  error
        //      0   not found smtp server cfg
        //      1   succeed
        public int SendEmail(string strUserEmail,
            string strSubject,
            string strBody,
            string strMime,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strManagerEmail = "";

            string strSmtpServerAddress = "";
            // ���smtp������������Ϣ
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = GetSmtpServerCfg(
                out strSmtpServerAddress,
                out strManagerEmail,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;   // not found cfg

            if (String.IsNullOrEmpty(strSmtpServerAddress) == true)
                strSmtpServerAddress = "127.0.0.1";

            try
            {

            // System.Net.Mail �����ֿռ�
            MailMessage message = new MailMessage(
                strManagerEmail,
                strUserEmail,
                strSubject,
                strBody);
            if (strMime == "html")
                message.IsBodyHtml = true;

                SmtpClient client = new SmtpClient(strSmtpServerAddress);
                // Credentials are necessary if the server requires the client 
                // to authenticate before it will send e-mail on the client's behalf.
                client.UseDefaultCredentials = true;
                client.Send(message);
            }
            catch (Exception ex/*System.Web.HttpException ehttp*/)
            {
                strError = GetInnerMessage(ex);
                return -1;
            }


            return 1;
        }

        public static string GetInnerMessage(Exception ex)
        {
            string strResult = "";
            for (; ; )
            {
                strResult += "|" + ex.Message;
                ex = ex.InnerException;
                if (ex == null)
                    return strResult;
            }
        }


        // ����ʼ�ģ��
        int GetMailTemplate(
            string strType, // dpmail email ��
            string strTemplateName,
            out string strText,
            out string strError)
        {
            strError = "";
            strText = "";

            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("mailTemplates/template[@name='"+strTemplateName+"']");
            if (nodes.Count == 0)
                return 0;   // not found

            foreach (XmlNode node in nodes)
            {
                string strCurrentType = DomUtil.GetAttr(node, "type");
                if (strType == strCurrentType)
                {
                    strText = node.InnerText;
                    return 1;
                }
            }

            // ���û���ҵ����Ͳ��õ�һ��
            // ����Ϊ�˼�����ǰû��type���Ե�<template>�÷�
            strText = nodes[0].InnerText;
            return 1;
        }

        // ����ģ���ֵ���滻�����յ�����
        int GetMailText(string strTemplate,
            Hashtable valueTable,
            out string strText,
            out string strError)
        {
            strError = "";

            strText = strTemplate;
            foreach (string strKey in valueTable.Keys)
            {
                string strValue = (string)valueTable[strKey];

                strText = strText.Replace(strKey, strValue);
            }

            return 0;
        }

        // ����API�Ĵ�������
        // ���ԤԼ�����Ϣ
        // text-level: �û���ʾ
        // return:
        //      -1  error
        //      0   ����
        //      1   ���ָòᱻ������ ���ܽ���
        //      2   ���ָò�ԤԼ�� ��������
        //      3   ���ָòᱻ������ ���ܽ��ġ����ұ������޸��˲��¼(<location>Ԫ�ط����˱仯)����Ҫ���������غ󣬰Ѳ��¼���档
        int DoBorrowReservationCheck(
            SessionInfo sessioninfo,
            bool bRenew,
            ref XmlDocument readerdom,
            ref XmlDocument itemdom,
            bool bForce,
            out string strError)
        {
            strError = "";

            int nRet = 0;
            long lRet = 0;

            // ������в���
            string strRefID = DomUtil.GetElementText(itemdom.DocumentElement,
"refID");
            string strReaderBarcode = DomUtil.GetElementText(readerdom.DocumentElement,
                "barcode");

            string strItemBarcode = DomUtil.GetElementText(itemdom.DocumentElement,
                "barcode");
            string strItemBarcodeParam = strItemBarcode;

            if (String.IsNullOrEmpty(strItemBarcode) == true)
            {
#if NO
                // text-level: �ڲ�����
                strError = "���¼�в�����Ų���Ϊ��";
                return -1;
#endif
                // ����������Ϊ�գ���ʹ�� �ο�ID
                if (String.IsNullOrEmpty(strRefID) == true)
                {
                    // text-level: �ڲ�����
                    strError = "���¼�в�����źͲο�ID��ӦͬʱΪ��";
                    return -1;
                }
                strItemBarcodeParam = "@refID:" + strRefID;
            }

            if (String.IsNullOrEmpty(strReaderBarcode) == true)
            {
                // text-level: �ڲ�����
                strError = "���߼�¼�ж���֤����Ų���Ϊ��";
                return -1;
            }

            XmlNodeList nodesReservationRequest = itemdom.DocumentElement.SelectNodes("reservations/request");

            // ���账��
            if (bRenew == true)
            {

                if (nodesReservationRequest.Count > 0)
                {
                    string strList = "";
                    for (int i = 0; i < nodesReservationRequest.Count; i++)
                    {
                        XmlNode node = nodesReservationRequest[i];

                        string strReader = DomUtil.GetAttr(node, "reader");

                        if (strList != "")
                            strList += ",";
                        strList += strReader;
                    }

                    // ���ԤԼ������Ϊ��ͨ���ߣ���strList������ʾ
                    if (sessioninfo.UserType == "reader")
                    {
                        // text-level: �û���ʾ
                        strError = string.Format(this.GetString("����������ܾ������s��ǰ�ѱ�sλ����ԤԼ"),    // "����������ܾ����� �� {0} ��ǰ�ѱ� {1} λ����ԤԼ��Ϊ�������ˣ��뾡�绹�ش��飬лл��"
                            strItemBarcodeParam,
                            nodesReservationRequest.Count.ToString());
                            // "����������ܾ����� �� " + strItemBarcode + " ��ǰ�ѱ� " + nodesReservationRequest.Count.ToString() + " λ����ԤԼ��Ϊ�������ˣ��뾡�绹�ش��飬лл��";
                    }
                    else
                    {
                        // text-level: �û���ʾ
                        strError = string.Format(this.GetString("����������ܾ������s��ǰ�ѱ����ж���ԤԼs"),  // "����������ܾ����� �� {0} ��ǰ�ѱ����ж���ԤԼ: {1}��Ϊ�������ˣ��뾡�绹�ش��飬лл��"
                            strItemBarcodeParam,
                            strList);
                            // "����������ܾ����� �� " + strItemBarcode + " ��ǰ�ѱ����ж���ԤԼ: " + strList + "��Ϊ�������ˣ��뾡�绹�ش��飬лл��";
                    }
                    // ��ǰ���д���Ķ����޷����裬��ֻ���ڵ���ǰ�黹ͼ��ݡ�
                    return 2;
                }
            }

            string strLocation = DomUtil.GetElementText(itemdom.DocumentElement,
                "location");

            // �ò��¼<location>���Ƿ���#reservation�������ж��ƺ�������֡�
            // Ӧ��Ҳ����<reservations/request>�Ƿ���ڡ�
            if (nodesReservationRequest.Count == 0
                && StringUtil.IsInList("#reservation", strLocation) == false)// ��������Ƿ�������ԤԼ�������ϵ�
                return 0;

            int nRedoLoadCount = 0;

        REDO_LOAD_QUEUE_REC:

            // ��һ������ԤԼ�����, �����Ƿ������Ѿ�֪ͨ��ȡ��Ĳ�, �����ǵȴ�����ͨ�ܵ�ԤԼ����δȡ��
            string strNotifyXml = "";
            string strOutputPath = "";
            byte[] timestamp = null;
            // ���ԤԼ������м�¼
            // return:
            //      -1  error
            //      0   not found
            //      1   ����1��
            //      >1  ���ж���1��
            nRet = GetArrivedQueueRecXml(
                sessioninfo.Channels,
                strItemBarcodeParam,    // strItemBarcode
                out strNotifyXml,
                out timestamp,
                out strOutputPath,
                out strError);
            if (nRet == -1)
                return -1;

            if (nRet == 0)
            {
                // ��Ȼ��location����#reservation������֪ͨ�����в�û�������¼
                // ��ס�ı���¼��location
                goto CHANGEITEMLOCATION;
            }

            XmlDocument notifydom = new XmlDocument();
            try
            {
                notifydom.LoadXml(strNotifyXml);
            }
            catch (Exception ex)
            {
                // text-level: �ڲ�����
                strError = "װ��ԤԼ����֪ͨ��¼XML��DOMʱ����: " + ex.Message;
                return -1;
            }

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);

            string strState = DomUtil.GetElementText(notifydom.DocumentElement,
                "state");
            if (StringUtil.IsInList("outof", strState) == true)
            {
                // ֪ͨ��¼����, �����Ѿ�����, ���õ�ǰ���߽������, ����ɾ�����֪ͨ��¼�ˡ�ע���޸Ĳ��¼��location��ȥ��#reservation


            }
            else
            {
                // ����ǲ�������֪ͨ�ı�����ȡ��
                string strNotifyReaderBarcode = DomUtil.GetElementText(notifydom.DocumentElement,
                    "readerBarcode");

                // �����Ǳ�������ȡ����
                if (strNotifyReaderBarcode == strReaderBarcode)
                {
                    // ɾ�����߼�¼�е�reservation֪ͨ��


                    // �ڲ��¼�У�ɾ����ص�<reservations/request>Ԫ�� 2007/1/17 new add
                    XmlNodeList nodes = itemdom.DocumentElement.SelectNodes("reservations/request[@reader='" + strReaderBarcode + "']");
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];
                        node.ParentNode.RemoveChild(node);
                    }
                }
                else // ����ԤԼ��������
                {

                    // ������ڼ�ԤԼ��֪ͨ����
                    if (StringUtil.IsInList("#reservation", strLocation) == false)
                    {
                        // ��Ҫ�޸Ĳ��¼��<location>Ϊ����#reservation��־�����ҰѶ��м�¼��<itemBarcode>��onShelf����Ϊfalse��
                        if (strLocation != "")
                            strLocation += ",";
                        strLocation += "#reservation";
                        DomUtil.SetElementText(itemdom.DocumentElement, "location", strLocation);

                        // �޸�ԤԼ֪ͨ��¼
                        //XmlNode nodeItemBarcode = notifydom.DocumentElement.SelectSingleNode("itemBarcode");
                        //if (nodeItemBarcode != null)
                        {
                            // DomUtil.SetAttr(nodeItemBarcode, "onShelf", "false");
                            DomUtil.SetElementText(notifydom.DocumentElement, "onShelf", "false");  // 2015/5/7

                            byte[] output_timestamp = null;
                            string strTempOutputPath = "";

                            lRet = channel.DoSaveTextRes(strOutputPath,
                                notifydom.OuterXml,
                                false,
                                "content",  // ,ignorechecktimestamp",
                                timestamp,
                                out output_timestamp,
                                out strTempOutputPath,
                                out strError);
                            if (lRet == -1)
                            {
                                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                                    && nRedoLoadCount < 10)
                                {
                                    nRedoLoadCount++;
                                    goto REDO_LOAD_QUEUE_REC;
                                }

                                // text-level: �ڲ�����
                                strError = "д�ض��м�¼ '" + strOutputPath + "' ʱ�������� : " + strError;
                                this.WriteErrorLog("���Ĳ��������ڼ�ԤԼͼ����Ҫд�ض��м�¼ " + strOutputPath + " ʱ����: " + strError);
                                return -1;
                            }
                        }

                        // text-level: �û���ʾ
                        strError = string.Format(this.GetString("���Ĳ������ܾ�����Ϊ��sΪ����s���ڼ�ԤԼ���Ѵ��ڱ�����֪ͨȡ��״̬"),
                            // "���Ĳ������ܾ�����Ϊ �� {0} Ϊ���� {1} ��(�ڼ�)ԤԼ���Ѵ��ڱ�����֪ͨȡ��״̬��\r\nͼ���Ա��ע�⣺��Ȼ���ν��Ĳ������ܾ������˲�ص���Ϣ�ѱ�����Զ��޸�Ϊ��ԤԼ������(��������ԭ������ͨ��)�������´�������ԤԼ������(���ض�λ�ã����硰���ڼܡ�����)��"
                            strItemBarcodeParam,
                            strNotifyReaderBarcode);
                            // "���Ĳ������ܾ�����Ϊ �� " + strItemBarcode + " Ϊ���� " + strNotifyReaderBarcode + " ��(�ڼ�)ԤԼ���Ѵ��ڱ�����֪ͨȡ��״̬��\r\nͼ���Ա��ע�⣺��Ȼ���ν��Ĳ������ܾ������˲�ص���Ϣ�ѱ�����Զ��޸�Ϊ��ԤԼ������(��������ԭ������ͨ��)�������´�������ԤԼ������(���ض�λ�ã����硰���ڼܡ�����)��";
                        return 3;
                    }

                    // text-level: �û���ʾ
                    strError = string.Format(this.GetString("���Ĳ������ܾ�����Ϊ��sΪ����s��ԤԼ���Ѵ��ڱ�����֪ͨȡ��״̬"),
                        // "���Ĳ������ܾ�����Ϊ �� {0} Ϊ���� {1} ��ԤԼ���Ѵ��ڱ�����֪ͨȡ��״̬��"
                        strItemBarcodeParam,
                        strNotifyReaderBarcode);
                        // "���Ĳ������ܾ�����Ϊ �� " + strItemBarcode + " Ϊ���� " + strNotifyReaderBarcode + " ��ԤԼ���Ѵ��ڱ�����֪ͨȡ��״̬��";
                    return 1;
                }
            }

            // ɾ��֪ͨ���м�¼
            {
                byte[] output_timestamp = null;
                int nRedoCount = 0;
            REDO_DELETE:
                lRet = channel.DoDeleteRes(strOutputPath,
                    timestamp,
                    out output_timestamp,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                        && nRedoCount < 10)
                    {
                        nRedoCount++;
                        timestamp = output_timestamp;
                        goto REDO_DELETE;
                    }
                    // д�������־?
                    this.WriteErrorLog("�ڽ��Ĳ����У�ɾ��ԤԼ������¼ '" + strOutputPath + "' ����: " + strError);
                }
            }

        CHANGEITEMLOCATION:

            // StringUtil.RemoveFromInList("#reservation", true, ref strLocation);
            strLocation = StringUtil.GetPureLocationString(strLocation);

            DomUtil.SetElementText(itemdom.DocumentElement,
                "location", strLocation);

            // ������Σ�����̽��������߼�¼�п��ܵ�ԤԼ��Ϣ

            // �ڶ��߼�¼�м����ɾ��ԤԼ��Ϣ
            // parameters:
            //      strFunction "new"����ԤԼ��Ϣ��"delete"ɾ��ԤԼ��Ϣ; "merge"�ϲ�; "split"��ɢ
            // return:
            //      -1  error
            //      0   unchanged
            //      1   changed
            nRet = DoReservationReaderXml(
                "delete",
                strItemBarcodeParam,    // strItemBarcode
                sessioninfo.Account.UserID,
                ref readerdom,
                out strError);
            if (nRet == -1)
            {
                // д�������־?
                this.WriteErrorLog("���Ĳ�����, �ڶ��߼�¼��ɾ��Ǳ�ڵ�ԤԼ��Ϣʱ(����DoReservationReaderXml() function=delete itembarcode=" + strItemBarcodeParam + ")����: " + strError);
            }

            return 0;
        }

        // ����API�Ĵ�������
        // �ڶ��߼�¼�Ͳ��¼�м��������Ϣ
        // text-level: �û���ʾ
        // parameters:
        //      domOperLog ������־��¼DOM
        //      this_return_time    ���ν��ĵ�Ӧ�����ʱ�䡣GMTʱ�䡣
        // return:
        //      -1  error
        //      0   ����
        //      // 1   ������ǰ���ĵ�ͼ��Ŀǰ�г������
        int DoBorrowReaderAndItemXml(
            bool bRenew,
            string strLibraryCode,
            ref XmlDocument readerdom,
            ref XmlDocument itemdom,
            bool bForce,
            string strOperator,
            string strItemRecPath,
            string strReaderRecPath,
            ref XmlDocument domOperLog,
            out BorrowInfo borrow_info,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            borrow_info = new BorrowInfo();

            DateTime this_return_time = new DateTime(0);

            LibraryApplication app = this;

            // ������в���
            string strReaderBarcode = DomUtil.GetElementText(readerdom.DocumentElement,
                "barcode");

            string strItemBarcode = DomUtil.GetElementText(itemdom.DocumentElement,
                "barcode");

            if (String.IsNullOrEmpty(strItemBarcode) == true)
            {
#if NO
                // text-level: �ڲ�����
                strError = "���¼�в�����Ų���Ϊ��";
                return -1;
#endif
                // ����������Ϊ�գ������ �ο�ID
                string strRefID = DomUtil.GetElementText(itemdom.DocumentElement,
    "refID");
                if (String.IsNullOrEmpty(strRefID) == true)
                {
                    // text-level: �ڲ�����
                    strError = "���¼�в�����źͲο�ID��ӦͬʱΪ��";
                    return -1;
                }
                strItemBarcode = "@refID:" + strRefID;
            }

            if (String.IsNullOrEmpty(strReaderBarcode) == true)
            {
                // text-level: �ڲ�����
                strError = "���߼�¼�ж���֤����Ų���Ϊ��";
                return -1;
            }

            // ����Ҫ���ĵĲ���Ϣ�У��ҵ�ͼ������
            string strBookType = DomUtil.GetElementText(itemdom.DocumentElement, "bookType");

            // �Ӷ�����Ϣ��, �ҵ���������
            string strReaderType = DomUtil.GetElementText(readerdom.DocumentElement, "readerType");


            // �޸Ķ��߼�¼
            int nNo = 0;

            XmlNode nodeBorrow = null;

            if (bRenew == true)
            {
                // �����Ƿ��Ѿ�����ǰ�Ѿ����ĵĲ�
                nodeBorrow = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='" + strItemBarcode + "']");
                if (nodeBorrow == null)
                {
                    // text-level: �û���ʾ
                    strError = string.Format(this.GetString("�ö���δ�����Ĺ���s������޷�����"),
                        // "�ö���δ�����Ĺ��� '{0}'������޷����衣"
                        strItemBarcode);
                        // "�ö���δ�����Ĺ��� '" + strItemBarcode + "'������޷����衣";
                    return -1;
                }

                // ����ϴε����
                string strNo = DomUtil.GetAttr(nodeBorrow, "no");
                if (String.IsNullOrEmpty(strNo) == true)
                    nNo = 0;
                else
                {
                    try
                    {
                        nNo = Convert.ToInt32(strNo);
                    }
                    catch
                    {
                        if (bForce == false)
                        {
                            // text-level: �ڲ�����
                            strError = "���߼�¼�� XML Ƭ�� " + nodeBorrow.OuterXml + "���� no ����ֵ'" + strNo + "' ��ʽ����";
                            return -1;
                        }
                        nNo = 0;
                    }
                }

            }
            else // bRenew == false
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
                nodeBorrow = DomUtil.InsertFirstChild(root, nodeBorrow); // 2006/12/24 changed��2015/1/12 ���ӵȺ���ߵĲ��� 
                // nodeBorrow = root.AppendChild(nodeBorrow);
            }

            //
            string strThisBorrowPeriod = "10day";   // ���ν��ĵ�����
            string strLastBorrowPeriod = "";    // �ϴν��ĵ�����

            // barcode
            DomUtil.SetAttr(nodeBorrow, "barcode", strItemBarcode);

            // ���ز��¼·��
            if (String.IsNullOrEmpty(strItemRecPath) == false)
                DomUtil.SetAttr(nodeBorrow, "recPath", strItemRecPath); // 2006/12/24 new add


            // ��������ֶ�
            // ���߼�¼�еĽ����ֶΣ�Ŀ����Ϊ�˲�ѯ���㣬��ע��û�з���Ч����
            // �����Գ����ж������õģ��ǲ��¼�еĽ����ֶΡ�
            string strBorrowPeriodList = "";
            MatchResult matchresult;
            // return:
            //      reader��book���;�ƥ�� ��4��
            //      ֻ��reader����ƥ�䣬��3��
            //      ֻ��book����ƥ�䣬��2��
            //      reader��book���Ͷ���ƥ�䣬��1��
            nRet = app.GetLoanParam(
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
                if (bForce == true)
                    goto DOCHANGE;
                // text-level: �ڲ�����
                strError = "����ʧ�ܡ���� �ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ����ʱ��������: " + strError;
                return -1;
            }
            if (nRet < 4)  // nRet == 0
            {
                if (bForce == true)
                    goto DOCHANGE;

                // text-level: �ڲ�����
                strError = "����ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� �����޷����: " + strError;
                return -1;
            }

            // ���ն��ŷ���ֵ����Ҫ�������ȡ��ĳ������

            string[] aPeriod = strBorrowPeriodList.Split(new char[] { ',' });

            if (aPeriod.Length == 0)
            {
                if (bForce == true)
                    goto DOCHANGE;

                // text-level: �ڲ�����
                strError = "����ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ���� '" + strBorrowPeriodList + "'��ʽ����";
                return -1;
            }


            if (bRenew == true)
            {
                nNo++;
                if (nNo >= aPeriod.Length)
                {
                    if (aPeriod.Length == 1)
                    {
                        // text-level: �û���ʾ
                        strError = string.Format(this.GetString("����ʧ�ܡ���������s���ͼ������s�Ľ��ڲ���ֵs�涨����������"),
                            // "����ʧ�ܡ��������� '{0}' ���ͼ������ '{1}' �� ���� ����ֵ '{2}' �涨���������衣(�������һ�����ޣ���ָ��һ�ν��ĵ�����)"
                            strReaderType,
                            strBookType,
                            strBorrowPeriodList);

                        // "����ʧ�ܡ��������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ����ֵ '" + strBorrowPeriodList + "' �涨���������衣(�������һ�����ޣ���ָ��һ�ν��ĵ�����)";
                    }
                    else
                    {
                        // text-level: �û���ʾ
                        strError = string.Format(this.GetString("����ʧ�ܡ���������s���ͼ������s�Ľ��ڲ���ֵs�涨��ֻ������s��"),
                            // "����ʧ�ܡ��������� '{0}' ���ͼ������ '{1}' �� ���� ����ֵ '{2}' �涨��ֻ������ {3} �Ρ�"
                            strReaderType,
                            strBookType,
                            strBorrowPeriodList,
                            Convert.ToString(aPeriod.Length - 1));
                            // "����ʧ�ܡ��������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ����ֵ '" + strBorrowPeriodList + "' �涨��ֻ������ " + Convert.ToString(aPeriod.Length - 1) + " �Ρ�";
                    }
                    return -1;
                }
                strThisBorrowPeriod = aPeriod[nNo].Trim();

                strLastBorrowPeriod = aPeriod[nNo-1].Trim();


                if (String.IsNullOrEmpty(strThisBorrowPeriod) == true)
                {
                    if (bForce == true)
                        goto DOCHANGE;

                    // text-level: �ڲ�����
                    strError = "����ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ���� '" + strBorrowPeriodList + "' ��ʽ���󣺵� " + Convert.ToString(nNo) + "������Ϊ�ա�";
                    return -1;
                }
            }
            else
            {
                strThisBorrowPeriod = aPeriod[0].Trim();

                if (String.IsNullOrEmpty(strThisBorrowPeriod) == true)
                {
                    if (bForce == true)
                        goto DOCHANGE;

                    // text-level: �ڲ�����
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
                    if (bForce == true)
                        goto DOCHANGE;

                    // text-level: �ڲ�����
                    strError = "����ʧ�ܡ��ݴ��� '" + strLibraryCode + "' �� �������� '" + strReaderType + "' ���ͼ������ '" + strBookType + "' �� ���� ���� '" + strBorrowPeriodList + "' ��ʽ����'" +
                         strThisBorrowPeriod + "' ��ʽ����: " + strError;
                    return -1;
                }
            }

        DOCHANGE:

            // ���㱾�� ����/���� ��Ӧ����ʱ��
            DateTime now = app.Clock.UtcNow;  //  ���죬���¡�GMTʱ��

            {
                long lPeriodValue = 0;
                string strPeriodUnit = "";
                nRet = LibraryApplication.ParsePeriodUnit(
                    strThisBorrowPeriod,
                    out lPeriodValue,
                    out strPeriodUnit,
                    out strError);
                if (nRet == -1)
                    goto SKIP_CHECK_RENEW_PERIOD;

                DateTime nextWorkingDay;

                // parameters:
                //      calendar    �������������Ϊnull����ʾ���������зǹ������жϡ�
                // return:
                //      -1  ����
                //      0   �ɹ���timeEnd�ڹ����շ�Χ�ڡ�
                //      1   �ɹ���timeEnd�����ڷǹ����ա�nextWorkingDay�Ѿ���������һ�������յ�ʱ��
                nRet = GetReturnDay(
                    null,
                    now,
                    lPeriodValue,
                    strPeriodUnit,
                    out this_return_time,
                    out nextWorkingDay,
                    out strError);
                if (nRet == -1)
                {
                    // text-level: �ڲ�����
                    strError = "������λ���ʱ����̷�������: " + strError;
                    return -1;
                }

                // ���滯ʱ��
                nRet = RoundTime(strPeriodUnit,
                    ref this_return_time,
                    out strError);
                if (nRet == -1)
                {
                    // text-level: �ڲ�����
                    strError = "������λ���ʱ����̷�������: " + strError;
                    return -1;
                }

            }

            // ��������裬��鲻�����Ӧ��������ں�������Ӧ����������ĸ�����
            // �������������ڻ���������������衣
            if (bRenew == true)
            {
                // �ϴν�����
                string strLastBorrowDate = DomUtil.GetAttr(nodeBorrow, "borrowDate");
                if (String.IsNullOrEmpty(strLastBorrowDate) == true)
                    goto SKIP_CHECK_RENEW_PERIOD;

                DateTime last_borrowdate;
                try
                {
                    last_borrowdate = DateTimeUtil.FromRfc1123DateTimeString(strLastBorrowDate);
                }
                catch
                {
                    goto SKIP_CHECK_RENEW_PERIOD;
                }


                long lLastPeriodValue = 0;
                string strLastPeriodUnit = "";
                nRet = ParsePeriodUnit(strLastBorrowPeriod,
                    out lLastPeriodValue,
                    out strLastPeriodUnit,
                    out strError);
                if (nRet == -1)
                {
                    // text-level: �ڲ�����
                    strError = "�������� ֵ '" + strLastBorrowPeriod + "' ��ʽ����: " + strError;
                    goto SKIP_CHECK_RENEW_PERIOD;
                }

                DateTime nextWorkingDay;

                DateTime last_return_time;
                // �����ϴν���Ļ�������
                nRet = GetReturnDay(
                    null,
                    last_borrowdate,
                    lLastPeriodValue,
                    strLastPeriodUnit,
                    out last_return_time,
                    out nextWorkingDay,
                    out strError);
                if (nRet == -1)
                {
                    // text-level: �ڲ�����
                    strError = "�����ϴλ���ʱ����̷�������: " + strError;
                    goto SKIP_CHECK_RENEW_PERIOD;
                }


                // ���滯ʱ��
                nRet = RoundTime(strLastPeriodUnit,
                    ref last_return_time,
                    out strError);
                if (nRet == -1)
                    goto SKIP_CHECK_RENEW_PERIOD;

                TimeSpan delta = last_return_time - this_return_time;

                if (delta.Ticks > 0)
                {
                    strError = string.Format(this.GetString("��������������ܾ��������������ʵ�к�Ӧ�����ڽ�������ǰ��"),
                        // "��������������ܾ��������������ʵ�к�Ӧ�����ڽ�Ϊ {0} (ע��������ڴӵ��տ�ʼ���㡣�ӽ��տ�ʼ����Ϊ {1})��������������裬Ӧ�����ڱ���Ϊ {2} (ע���� {3} ��ʼ����Ϊ {4} )������������Ӧ�����ڷ�����ǰ�ˡ�"
                        GetLocalTimeString(strLastPeriodUnit, this_return_time),
                        GetDisplayTimePeriodStringEx(strThisBorrowPeriod),
                        GetLocalTimeString(strLastPeriodUnit, last_return_time),
                        GetLocalTimeString(strLastPeriodUnit, last_borrowdate),
                        GetDisplayTimePeriodStringEx(strLastBorrowPeriod));
                    /*
                    // 2008/5/8 changed
                    strError = "��������������ܾ��������������ʵ�к�Ӧ�����ڽ�Ϊ "
                        + GetLocalTimeString(strLastPeriodUnit, this_return_time)
                        + " (ע��������ڴӵ��տ�ʼ���㡣�ӽ��տ�ʼ����Ϊ "
                        + GetDisplayTimePeriodString(strThisBorrowPeriod)
                        + " )��������������裬Ӧ�����ڱ���Ϊ " 
                        + GetLocalTimeString(strLastPeriodUnit, last_return_time) // this_return_time.ToString() BUG!!!
                        + " (ע���� "
                        + GetLocalTimeString(strLastPeriodUnit, last_borrowdate)
                        + " ��ʼ����Ϊ "
                        + GetDisplayTimePeriodString(strLastBorrowPeriod)
                        + " )������������Ӧ�����ڷ�����ǰ�ˡ�";
                     * */
                    return -1;
                }
            }

        SKIP_CHECK_RENEW_PERIOD:

            string strRenewComment = "";

            string strBorrowDate = app.Clock.GetClock();

            string strLastReturningDate = "";   // �ϴε�Ӧ��ʱ��

            if (bRenew == true)
            {
                strLastReturningDate = DomUtil.GetAttr(nodeBorrow, "returningDate");

                // �������
                nNo = Math.Max(nNo, 1);

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

            if (nNo > 0)    // 2013/12/23
                DomUtil.SetAttr(nodeBorrow, "no", Convert.ToString(nNo));

            DomUtil.SetAttr(nodeBorrow, "borrowPeriod", strThisBorrowPeriod);

            // 2014/11/14
            // returningDate
            string strReturningDate = DateTimeUtil.Rfc1123DateTimeStringEx(this_return_time.ToLocalTime());
            DomUtil.SetAttr(nodeBorrow, "returningDate",
                strReturningDate);

            // 2014/11/14
            // lastReturningDate
            
            if (nNo > 0)
                DomUtil.SetAttr(nodeBorrow, "lastReturningDate",
                    strLastReturningDate);

            if (string.IsNullOrEmpty(strRenewComment) == false)    // 2013/12/23
                DomUtil.SetAttr(nodeBorrow, "renewComment", strRenewComment);

            DomUtil.SetAttr(nodeBorrow, "operator", strOperator);

            // 2007/11/5 new add
            DomUtil.SetAttr(nodeBorrow, "type", strBookType);   // �ڶ��߼�¼<borrows/borrow>Ԫ����д��type���ԣ�����Ϊͼ������ͣ����ں��������ʱ���ж�ĳһ�ֲ������Ƿ񳬹�����Ȩ�޹涨ֵ�����ַ�ʽ���Խ�ʡʱ�䣬���شӶ�����¼��ȥ��ò������ֶ�

            // 2006/11/12 new add
            string strBookPrice = DomUtil.GetElementText(itemdom.DocumentElement, "price");
            DomUtil.SetAttr(nodeBorrow, "price", strBookPrice);   // �ڶ��߼�¼<borrows/borrow>Ԫ����д��price���ԣ�����Ϊͼ���۸����ͣ����ں��������ʱ���ж��Ѿ���ĺͼ�������ܼ۸��Ƿ񳬹����ߵ�Ѻ�������ַ�ʽ���Խ�ʡʱ�䣬���شӶ�����¼��ȥ��ò�۸��ֶ�

            // �޸Ĳ��¼
            string strOldReaderBarcode = "";

            strOldReaderBarcode = DomUtil.GetElementText(itemdom.DocumentElement,
                "borrower");

            if (bRenew == false)
            {
                if (bForce == false
                    && String.IsNullOrEmpty(strOldReaderBarcode) == false)
                {
                    // 2007/1/2 new add
                    if (strOldReaderBarcode == strReaderBarcode)
                    {
                        // text-level: �û���ʾ
                        strError = "���Ĳ������ܾ������ '" + strItemBarcode + "' �ڱ��β���ǰ�Ѿ�����ǰ���� '" + strReaderBarcode + "' �����ˡ�";
                        return -1;
                    }

                    // text-level: �û���ʾ
                    strError = "���Ĳ������ܾ������ '" + strItemBarcode + "' �ڱ��β���ǰ�Ѿ����ڱ����� '" + strOldReaderBarcode + "' ����(����)״̬(��δ�黹)��\r\n��������ô�������빤����Ա�����������飬�跨�����������ˣ�\r\n���ȷϵ(��������ͬ��)��������Ҫת��˲ᣬ�������л���������\r\n���������Ҫ����˲ᣬ����������������";
                    return -1;
                }
            }

            DomUtil.SetElementText(itemdom.DocumentElement,
                "borrower", strReaderBarcode);

            // 2008/9/18 new add
            DomUtil.SetElementText(itemdom.DocumentElement,
                "borrowerReaderType", strReaderType);

            // 2012/9/8
            DomUtil.SetElementText(itemdom.DocumentElement,
                "borrowerRecPath", strReaderRecPath);

            DomUtil.SetElementText(itemdom.DocumentElement,
                "borrowDate",
                strBorrowDate);

            if (nNo > 0)    // 2013/12/23
                DomUtil.SetElementText(itemdom.DocumentElement,
                    "no",
                    Convert.ToString(nNo));

            DomUtil.SetElementText(itemdom.DocumentElement,
                "borrowPeriod",
                strThisBorrowPeriod);   // strBorrowPeriod�����Ѿ��Ǹ�����������Ƕ��ŷָ����о�ֵ��

            DomUtil.SetElementText(itemdom.DocumentElement,
                "returningDate",
                strReturningDate);

            if (nNo > 0)
                DomUtil.SetElementText(itemdom.DocumentElement,
                    "lastReturningDate",
                    strLastReturningDate);  

            if (string.IsNullOrEmpty(strRenewComment) == false) // 2013/12/23
                DomUtil.SetElementText(itemdom.DocumentElement,
                    "renewComment",
                    strRenewComment);

            DomUtil.SetElementText(itemdom.DocumentElement,
                "operator",
                strOperator);

            // ������־��¼
            DomUtil.SetElementText(domOperLog.DocumentElement, "readerBarcode",
                strReaderBarcode);     // ����֤�����
            DomUtil.SetElementText(domOperLog.DocumentElement, "itemBarcode",
                strItemBarcode);    // �������
            DomUtil.SetElementText(domOperLog.DocumentElement, "borrowDate",
                strBorrowDate);     // ��������
            DomUtil.SetElementText(domOperLog.DocumentElement, "borrowPeriod",
                strThisBorrowPeriod);   // ��������
            DomUtil.SetElementText(domOperLog.DocumentElement, "returningDate",
                strReturningDate);     // Ӧ������

            // 2015/1/12
            DomUtil.SetElementText(domOperLog.DocumentElement, "type",
    strBookType);    // ͼ������
            DomUtil.SetElementText(domOperLog.DocumentElement, "price",
strBookPrice);    // ͼ��۸�

            // TODO: 0 ��Ҫ����д��
            DomUtil.SetElementText(domOperLog.DocumentElement, "no",
                Convert.ToString(nNo)); // �������

            if (nNo > 0)
                DomUtil.SetElementText(domOperLog.DocumentElement, "lastReturningDate",
    strLastReturningDate);     // �ϴ�Ӧ������

            DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                strOperator);   // ������
            DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                strBorrowDate);   // ����ʱ��

            // ���ؽ��ĳɹ�����Ϣ

            // ��������RFC1123��ʱ��ֵ�ַ��� GMTʱ��
            borrow_info.LatestReturnTime = DateTimeUtil.Rfc1123DateTimeStringEx(this_return_time.ToLocalTime());
            borrow_info.Period = strThisBorrowPeriod;
            borrow_info.BorrowCount = nNo;

            // 2011/6/26
            borrow_info.BorrowOperator = strOperator;
            /*
            borrow_info.BookType = strBookType;
            string strLocation = DomUtil.GetElementText(itemdom.DocumentElement,
                "location");
            borrow_info.Location = strLocation;
             * */

            return 0;
        }

        // ��ʱ��ֵ�ı���ʱ�䣬���յ�λת��Ϊ�ʵ�����ʾ�ַ���
        // 2008/5/7 new add
        static string GetLocalTimeString(string strUnit,
            DateTime time)
        {
            if (strUnit == "day")
                return time.ToLocalTime().ToString("d");   // "yyyy-MM-dd"
            if (strUnit == "hour")
                return time.ToLocalTime().ToString("G");

            return time.ToLocalTime().ToString("G");
        }

        // ����API�Ĵ�������
        // ��鵱ǰ�Ƿ���Ǳ�ڵĳ��ڲ�
        // text-level: �û���ʾ
        // return:
        //      -1  error
        //      0   û�г��ڲ�
        //      1   �г��ڲ�
        int CheckOverdue(
            Calendar calendar,
            XmlDocument readerdom,
            bool bForce,
            out string strError)
        {
            strError = "";
            int nOverCount = 0;
            int nRet = 0;

            LibraryApplication app = this;


            string strOverdueItemBarcodeList = "";

            XmlNodeList nodes = readerdom.DocumentElement.SelectNodes("borrows/borrow");
            XmlNode node = null;
            if (nodes.Count > 0)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    node = nodes[i];
                    string strBarcode = DomUtil.GetAttr(node, "barcode");
                    string strBorrowDate = DomUtil.GetAttr(node, "borrowDate");
                    string strPeriod = DomUtil.GetAttr(node, "borrowPeriod");
                    string strOperator = DomUtil.GetAttr(node, "operator");

                    // return:
                    //      -1  ���ݸ�ʽ����
                    //      0   û�з��ֳ���
                    //      1   ���ֳ���   strError������ʾ��Ϣ
                    //      2   �Ѿ��ڿ������ڣ������׳��� 2009/3/13 new add
                    nRet = app.CheckPeriod(
                        calendar,
                        strBorrowDate,
                        strPeriod,
                        out strError);
                    if (nRet == -1)
                    {
                        if (bForce == true)
                            continue;
                        // text-level: �ڲ�����
                        strError = "���߼�¼�� �йز� '" + strBarcode + "' �Ľ���������Ϣ�����ִ���" + strError;
                    }

                    if (nRet == 1)
                    {
                        if (strOverdueItemBarcodeList != "")
                            strOverdueItemBarcodeList += ",";
                        strOverdueItemBarcodeList += strBarcode;
                        nOverCount++;
                    }


                }

                // ����δ�黹�Ĳ��г����˳������
                if (nOverCount > 0)
                {
                    // strError = "�ö��ߵ�ǰ�� " + Convert.ToString(nOverCount) + " ��δ�����ڲ�: " + strOverdueItemBarcodeList + " ����˽��Ĳ������ܾ�������߾��콫��Щ�ѳ��ڲ����л���������";

                    // text-level: �û���ʾ
                    strError = string.Format(this.GetString("�ö��ߵ�ǰ��s��δ�����ڲ�"),   // "�ö��ߵ�ǰ�� {0} ��δ�����ڲ�: {1}"
                        Convert.ToString(nOverCount),
                        strOverdueItemBarcodeList);

                        // "�ö��ߵ�ǰ�� " + Convert.ToString(nOverCount) + " ��δ�����ڲ�: " + strOverdueItemBarcodeList + ""; // ����˽���(������)�������ܾ�������߾��콫��Щ�ѳ��ڲ����л���������
                    return 1;
                }
            }

            return 0;
        }

        // ������֤�Ƿ��ڣ��Ƿ��й�ʧ��״̬
        // 2006/8/23 new add ���� ��δ����
        // text-level: �û���ʾ OPACԤԼ����Ҫ���ô˺���
        // return:
        //      -1  �����̷����˴���Ӧ�������ܽ���������
        //      0   ���Խ���
        //      1   ֤�Ѿ�����ʧЧ�ڣ����ܽ���
        //      2   ֤�в��ý��ĵ�״̬
        public int CheckReaderExpireAndState(XmlDocument readerdom,
            out string strError)
        {
            strError = "";

            string strExpireDate = DomUtil.GetElementText(readerdom.DocumentElement, "expireDate");
            if (String.IsNullOrEmpty(strExpireDate) == false)
            {
                DateTime expireDate;
                try
                {
                    expireDate = DateTimeUtil.FromRfc1123DateTimeString(strExpireDate);
                }
                catch
                {
                    // text-level: �ڲ�����
                    strError = string.Format(this.GetString("����֤ʧЧ��ֵs��ʽ����"), // "����֤ʧЧ��<expireDate>ֵ '{0}' ��ʽ����"
                        strExpireDate);

                        // "����֤ʧЧ��<expireDate>ֵ '" + strExpireDate + "' ��ʽ����";
                    return -1;
                }

                DateTime now = this.Clock.UtcNow;

                if (expireDate <= now)
                {
                    // text-level: �û���ʾ
                    strError = string.Format(this.GetString("����s�Ѿ���������֤ʧЧ��s"),  // "����({0})�Ѿ���������֤ʧЧ��({1})��"
                        now.ToLocalTime().ToLongDateString(),
                        expireDate.ToLocalTime().ToLongDateString());
                        // "����(" + now.ToLocalTime().ToLongDateString() + ")�Ѿ���������֤ʧЧ��(" + expireDate.ToLocalTime().ToLongDateString() + ")��";
                    return 1;
                }

            }

            string strState = DomUtil.GetElementText(readerdom.DocumentElement, "state");
            if (String.IsNullOrEmpty(strState) == false)
            {
                // text-level: �û���ʾ
                strError = string.Format(this.GetString("����֤��״̬Ϊs"), // "����֤��״̬Ϊ '{0}'��"
                    strState);
                    // "����֤��״̬Ϊ '" + strState + "'��";
                return 2;
            }

            return 0;
        }


        // ������ߺͲ��¼�е��ѵ�ԤԼ�������ȡ��һ��ԤԼ����֤�����
        // ������������������¼����ǰ������state=arrived��<request>Ԫ��
        // parameters:
        //      strItemBarcode  ������š�֧�� "@refID:" ǰ׺�÷�
        //      bMaskLocationReservation    ��Ҫ�����¼<location>����#reservation���
        //      strReservationReaderBarcode ������һ��ԤԼ���ߵ�֤�����
        public int ClearArrivedInfo(
            RmsChannelCollection channels,
            string strReaderBarcode,
            string strItemBarcode,
            bool bDontMaskLocationReservation,
            out string strReservationReaderBarcode,
            out string strError)
        {
            strError = "";

            byte[] timestamp = null;
            byte[] output_timestamp = null;
            string strOutputPath = "";
            long lRet = 0;
            int nRet = 0;
            strReservationReaderBarcode = "";

            RmsChannel channel = null;
            channel = channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            bool bDontLock = false;

            // �Ӷ��߼�¼��
            try
            {
                this.ReaderLocks.LockForWrite(strReaderBarcode);
            }
            catch (System.Threading.LockRecursionException)
            {
                // 2012/5/31
                // �п��ܱ�������DigitalPlatform.LibraryServer.LibraryApplication.Reservation()����ʱ���Ѿ��Զ��߼�¼������
                bDontLock = true;
            }

            try
            {

                // ������߼�¼
                string strReaderXml = "";
                string strOutputReaderRecPath = "";
                nRet = this.GetReaderRecXml(
                    channels,
                    strReaderBarcode,
                    out strReaderXml,
                    out strOutputReaderRecPath,
                    out strError);
                if (nRet == 0)
                {
                    strError = "����֤����� '" + strReaderBarcode + "' ������";
                    goto DOITEM;
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


                // �ӵ�ǰ���߼�¼��ɾ���й��ֶ�
                XmlNodeList nodes = readerdom.DocumentElement.SelectNodes("reservations/request");
                XmlNode readerRequestNode = null;
                string strItems = "";
                bool bFound = false;
                for (int i = 0; i < nodes.Count; i++)
                {
                    readerRequestNode = nodes[i];
                    strItems = DomUtil.GetAttr(readerRequestNode, "items");
                    if (IsInBarcodeList(strItemBarcode, strItems) == true)
                    {
                        bFound = true;
                        break;
                    }
                }

                if (bFound == true)
                {
                    Debug.Assert(readerRequestNode != null, "");

                    // ������������޸�״̬��ǲ�����һ�Σ�
                    // ����������������ͬʱд����־��ã��Ա㽫����ѯ
                    readerRequestNode.ParentNode.RemoveChild(readerRequestNode);
                }

                // д�ض��߼�¼
                if (bFound == true)
                {
                    lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                        readerdom.OuterXml,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "д�ض��߼�¼ '" + strOutputReaderRecPath + "' ʱ�������� : " + strError;
                        return -1;
                    }
                }

            DOITEM:
                // ˳������һ��ԤԼ����֤�����
                string strItemXml = "";
                string strOutputItemRecPath = "";
                // ��ò��¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                nRet = this.GetItemRecXml(
                    channel,
                    strItemBarcode,
                    out strItemXml,
                    out strOutputItemRecPath,
                    out strError);
                if (nRet == 0)
                {
                    strError = "������� '" + strItemBarcode + "' ������";
                    return 0;
                }
                if (nRet == -1)
                {
                    strError = "������¼ʱ��������: " + strError;
                    return -1;
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
                // �쿴����ԤԼ���, ����У���ȡ����һ��ԤԼ���ߵ�֤�����
                // �ú��������������ǰ������state=arrived��<request>Ԫ��
                // return:
                //      -1  error
                //      0   û���޸�
                //      1   ���й��޸�
                nRet = DoItemReturnReservationCheck(
                    bDontMaskLocationReservation,
                    ref itemdom,
                    out strReservationReaderBarcode,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (nRet == 1)
                {
#if NO
                    channel = channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "get channel error";
                        return -1;
                    }
#endif

                    lRet = channel.DoSaveTextRes(strOutputItemRecPath,
                        itemdom.OuterXml,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "д�ز��¼ '" + strOutputItemRecPath + "' ʱ��������: " + strError;
                        return -1;
                    }

                }

            }
            finally
            {
                if (bDontLock == false)
                    this.ReaderLocks.UnlockForWrite(strReaderBarcode);
            }

            return 1;
        }

        // ת�ƽ�����Ϣ
        // ��Դ���߼�¼�е�<borrows>��<overdues>ת�Ƶ�Ŀ����߼�¼��
        // result.Value:
        //      -1  error
        //      0   û�б�Ҫת�ơ���Դ���߼�¼��û����Ҫת�ƵĽ�����Ϣ
        //      1   �Ѿ��ɹ�ת��
        public LibraryServerResult DevolveReaderInfo(
            SessionInfo sessioninfo,
            string strSourceReaderBarcode,
            string strTargetReaderBarcode)
        {
            string strError = "";
            int nRet = 0;
            long lRet = 0;
            bool bChanged = false;  // �Ƿ�����ʵ���ԸĶ�

            LibraryServerResult result = new LibraryServerResult();

            // Ȩ���ַ���
            if (StringUtil.IsInList("devolvereaderinfo", sessioninfo.RightsOrigin) == false)
            {
                result.Value = -1;
                result.ErrorInfo = "ת�ƽ�����Ϣ�������ܾ������߱�devolvereaderinfoȨ�ޡ�";
                result.ErrorCode = ErrorCode.AccessDenied;
                return result;
            }

            // ���Դ��Ŀ������Ų�����ͬ
            if (strSourceReaderBarcode == strTargetReaderBarcode)
            {
                strError = "Դ��Ŀ����߼�¼֤����Ų�����ͬ";
                goto ERROR1;
            }

            if (String.IsNullOrEmpty(strSourceReaderBarcode) == true)
            {
                strError = "Դ���߼�¼֤����Ų���Ϊ��";
                goto ERROR1;
            }

            if (String.IsNullOrEmpty(strTargetReaderBarcode) == true)
            {
                strError = "Ŀ����߼�¼֤����Ų���Ϊ��";
                goto ERROR1;
            }


            // ��Դ��Ŀ������֤����ż����������������߼�¼
            // �������Ⱥ��м��ɣ��ȼ�����Ž�С�����������������������
            string strBarcode1 = "";
            string strBarcode2 = "";
            if (String.Compare(strSourceReaderBarcode, strTargetReaderBarcode) < 0)
            {
                strBarcode1 = strSourceReaderBarcode;
                strBarcode2 = strTargetReaderBarcode;
            }
            else
            {
                strBarcode1 = strTargetReaderBarcode;
                strBarcode2 = strSourceReaderBarcode;
            }

            try
            {

                // �Ӷ��߼�¼��1
                this.ReaderLocks.LockForWrite(strBarcode1);
                try // ���߼�¼����1��Χ��ʼ
                {
                    // �Ӷ��߼�¼��2
                    this.ReaderLocks.LockForWrite(strBarcode2);
                    try // ���߼�¼����2��Χ��ʼ
                    {

                        // ����Դ���߼�¼
                        string strSourceReaderXml = "";
                        string strSourceOutputReaderRecPath = "";
                        byte[] source_reader_timestamp = null;
                        nRet = this.GetReaderRecXml(
                            sessioninfo.Channels,
                            strSourceReaderBarcode,
                            out strSourceReaderXml,
                            out strSourceOutputReaderRecPath,
                            out source_reader_timestamp,
                            out strError);
                        if (nRet == 0)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "Դ����֤����� '" + strSourceReaderBarcode + "' ������";
                            result.ErrorCode = ErrorCode.SourceReaderBarcodeNotFound;
                            return result;
                        }
                        if (nRet == -1)
                        {
                            strError = "����Դ���߼�¼ʱ��������: " + strError;
                            goto ERROR1;
                        }

                        // 2008/6/17 new add
                        if (nRet > 1)
                        {
                            strError = "����Դ���߼�¼ʱ�����ֶ���֤����� " + strSourceReaderBarcode + " ���� " + nRet.ToString() + " ��������һ�����ش�����ϵͳ����Ա���촦��";
                            goto ERROR1;
                        }

                        string strSourceLibraryCode = "";

                        // �������߼�¼�������Ķ��߿�Ĺݴ��룬�Ƿ񱻵�ǰ�û���Ͻ
                        if (String.IsNullOrEmpty(strSourceOutputReaderRecPath) == false)
                        {
                            // ��鵱ǰ�������Ƿ��Ͻ������߿�
                            // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                            if (this.IsCurrentChangeableReaderPath(strSourceOutputReaderRecPath,
                    sessioninfo.LibraryCodeList,
                    out strSourceLibraryCode) == false)
                            {
                                strError = "Դ���߼�¼·�� '" + strSourceOutputReaderRecPath + "' �����Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                                goto ERROR1;
                            }
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

                        // ����Ŀ����߼�¼
                        string strTargetReaderXml = "";
                        string strTargetOutputReaderRecPath = "";
                        byte[] target_reader_timestamp = null;
                        nRet = this.GetReaderRecXml(
                            sessioninfo.Channels,
                            strTargetReaderBarcode,
                            out strTargetReaderXml,
                            out strTargetOutputReaderRecPath,
                            out target_reader_timestamp,
                            out strError);
                        if (nRet == 0)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "Ŀ�����֤����� '" + strTargetReaderBarcode + "' ������";
                            result.ErrorCode = ErrorCode.TargetReaderBarcodeNotFound;
                            return result;
                        }
                        if (nRet == -1)
                        {
                            strError = "����Ŀ����߼�¼ʱ��������: " + strError;
                            goto ERROR1;
                        }

                        // 2008/6/17 new add
                        if (nRet > 1)
                        {
                            strError = "����Ŀ����߼�¼ʱ�����ֶ���֤����� " + strTargetReaderBarcode + " ���� " + nRet.ToString() + " ��������һ�����ش�����ϵͳ����Ա���촦��";
                            goto ERROR1;
                        }

                        string strTargetLibraryCode = "";

                        // �������߼�¼�������Ķ��߿�Ĺݴ��룬�Ƿ񱻵�ǰ�û���Ͻ
                        if (String.IsNullOrEmpty(strTargetOutputReaderRecPath) == false)
                        {
                            // ��鵱ǰ�������Ƿ��Ͻ������߿�
                            // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                            if (this.IsCurrentChangeableReaderPath(strTargetOutputReaderRecPath,
                    sessioninfo.LibraryCodeList,
                    out strTargetLibraryCode) == false)
                            {
                                strError = "Դ���߼�¼·�� '" + strTargetOutputReaderRecPath + "' �����Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                                goto ERROR1;
                            }
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

                        XmlDocument domOperLog = new XmlDocument();
                        domOperLog.LoadXml("<root />");
                        DomUtil.SetElementText(domOperLog.DocumentElement,
    "libraryCode",
    strSourceLibraryCode + "," + strTargetLibraryCode );    // �������ڵĹݴ���
                        DomUtil.SetElementText(domOperLog.DocumentElement,
                            "operation", "devolveReaderInfo");

                        // ���߼���־�ĽǶȣ�Ӧ��˵��ֻҪ��Դ����֤����ź�
                        // Ŀ�����֤����ţ������Ը�ԭ
                        DomUtil.SetElementText(domOperLog.DocumentElement,
                            "sourceReaderBarcode",
                            strSourceReaderBarcode);
                        DomUtil.SetElementText(domOperLog.DocumentElement,
                            "targetReaderBarcode",
                            strTargetReaderBarcode);

                        string strOperTimeString = this.Clock.GetClock();   // RFC1123��ʽ

                        DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                sessioninfo.UserID);
                        DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                            strOperTimeString);

                        // ׼�����������õ�����ʱ�ļ���
                        string strAttachmentFileName = this.GetTempFileName("attach");  //  Path.GetTempFileName();
                        Stream attachment = null;

                        try // �ڴ˷�Χ�ڣ���ע�����ɾ����ʱ�ļ�
                        {


                            // �ƶ�������Ϣ -- <borrows>Ԫ������
                            // return:
                            //      -1  error
                            //      0   not found brrowinfo
                            //      1   found and moved
                            nRet = DevolveBorrowInfo(
                                sessioninfo.Channels,
                                strSourceReaderBarcode,
                                strTargetReaderBarcode,
                                strOperTimeString,
                                ref source_readerdom,
                                ref target_readerdom,
                                ref domOperLog,
                                strAttachmentFileName,
                                out attachment,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                            if (nRet == 1)
                                bChanged = true;

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
                                ref domOperLog,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                            if (nRet == 1)
                                bChanged = true;

                            // û��ʵ���Ըı�
                            if (bChanged == false)
                            {
                                result.Value = 0;
                                return result;
                            }


                            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                            if (channel == null)
                            {
                                strError = "get channel error";
                                goto ERROR1;
                            }


                            // �����������߼�¼
                            // д�ض��߼�¼
                            byte[] output_timestamp = null;
                            string strOutputPath = "";

                            int nRedoCount = 0;

                            // Ӧ���ȱ���target���߼�¼����Ϊ����˺��жϣ���ԭ�Ŀ�����Ҫ��һЩ



                        // REDO_WRITE_TARGET:
                            lRet = channel.DoSaveTextRes(strTargetOutputReaderRecPath,
                                target_readerdom.OuterXml,
                                false,
                                "content,ignorechecktimestamp",
                                target_reader_timestamp,
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
                                        strError = "д��Ŀ����߼�¼��ʱ��,����ʱ�����ͻ,���������10��,��ʧ��...";
                                        goto ERROR1;
                                    }
                                    target_reader_timestamp = output_timestamp;
                                    goto REDO_WRITE_SOURCE;
                                }
                                goto ERROR1;
                            }

                        REDO_WRITE_SOURCE:
                            nRedoCount = 0;
                            lRet = channel.DoSaveTextRes(strSourceOutputReaderRecPath,
                                source_readerdom.OuterXml,
                                false,
                                "content,ignorechecktimestamp",
                                source_reader_timestamp,
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
                                        strError = "д��Դ���߼�¼��ʱ��,����ʱ�����ͻ,���������10��,��ʧ��...";
                                        goto ERROR1;
                                    }
                                    source_reader_timestamp = output_timestamp;
                                    goto REDO_WRITE_SOURCE;
                                }
                                goto ERROR1;
                            }

                            // ��������д���Դ���߼�¼����־
                            XmlNode nodeRecord = DomUtil.SetElementText(domOperLog.DocumentElement,
                                "sourceReaderRecord",
                                source_readerdom.OuterXml);
                            DomUtil.SetAttr(nodeRecord,
                                "recPath",
                                strSourceOutputReaderRecPath);

                            // ��������д���Ŀ����߼�¼
                            nodeRecord = DomUtil.SetElementText(domOperLog.DocumentElement,
                                "targetReaderRecord",
                                target_readerdom.OuterXml);
                            DomUtil.SetAttr(nodeRecord,
                                "recPath",
                                strTargetOutputReaderRecPath);

                            if (attachment != null)
                            {
                                // ���ļ�ָ�븴λ��ͷ��
                                attachment.Seek(0, SeekOrigin.Begin);
                            }

                            nRet = this.OperLog.WriteOperLog(domOperLog,
                                sessioninfo.ClientAddress,
                                attachment,
                                out strError);
                            if (nRet == -1)
                            {
                                strError = "DevolveReaderInfo() API д����־ʱ��������: " + strError;
                                goto ERROR1;
                            }
                        }
                        finally //  end of �ڴ˷�Χ�ڣ���ע�����ɾ����ʱ�ļ�
                        {
                            if (attachment != null)
                            {
                                attachment.Close();
                                attachment = null;
                            }
                            File.Delete(strAttachmentFileName);
                        }

                    }// ���߼�¼����2��Χ����
                    finally
                    {
                        this.ReaderLocks.UnlockForWrite(strBarcode2);
                    }


                }// ���߼�¼����1��Χ����
                finally
                {
                    this.ReaderLocks.UnlockForWrite(strBarcode1);
                }

            }
            catch (Exception ex)
            {
                strError = ex.Message;
                goto ERROR1;
            }

            result.Value = 1;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        // �ƶ�������Ϣ -- <borrows>Ԫ������
        // �˺���Ҳ����־�ָ�ģ����ʹ�ã�ֻ�ǻָ�ʱdomOperLogΪnull
        // parameters:
        //      domOperLog      ������־DOM�����������ʱΪnull����ʾ������������־��������־������
        //      strAttachmentFileName   ��־�����ļ���������б�Ҫ������־�������򴴽������ļ���������֡�
        //      attachment              [out]�����������־���������ش򿪵�����
        // return:
        //      -1  error
        //      0   not found brrowinfo
        //      1   found and moved
        int DevolveBorrowInfo(
            RmsChannelCollection Channels,
            string strSourceReaderBarcode,
            string strTargetReaderBarcode,
            string strOperTimeString,
            ref XmlDocument source_dom,
            ref XmlDocument target_dom,
            ref XmlDocument domOperLog,
            string strAttachmentFileName,
            out Stream attachment,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            attachment = null;

            XmlNode nodeSourceBorrows = source_dom.DocumentElement.SelectSingleNode("borrows");
            if (nodeSourceBorrows == null)
                return 0;

            XmlNodeList nodesSourceBorrow = nodeSourceBorrows.SelectNodes("borrow");
            if (nodesSourceBorrow.Count == 0)
                return 0;


            XmlNode nodeTargetBorrows = target_dom.DocumentElement.SelectSingleNode("borrows");
            if (nodeTargetBorrows == null)
            {
                nodeTargetBorrows = target_dom.CreateElement("borrows");
                target_dom.DocumentElement.AppendChild(nodeTargetBorrows);
            }

            int nAttachmentIndex = 0;
            if (domOperLog != null)
            {
                if (nodesSourceBorrow.Count > 10)
                {
                    // �漰��ʵ���¼̫�࣬�޷�ֱ��д����־��¼

                    // ����������
                    attachment = File.Create(strAttachmentFileName);
                }
                else
                {
                    attachment = null;  // ��ʹ�ø���
                }
            }

            for (int i = 0; i<nodesSourceBorrow.Count; i++)
            {
                XmlNode source = nodesSourceBorrow[i];

                // ��<borrow>Ԫ��
                XmlDocumentFragment fragment = target_dom.CreateDocumentFragment();
                fragment.InnerXml = source.OuterXml;

                XmlNode target = nodeTargetBorrows.AppendChild(fragment);
                // ����һ��ע��Ԫ��
                DomUtil.SetAttr(target, "devolveComment", "�Ӷ��� " + strSourceReaderBarcode + " ת�ƶ���������ʱ�� " + strOperTimeString);


                string strEntityBarcode = DomUtil.GetAttr(source, "barcode");

                if (String.IsNullOrEmpty(strEntityBarcode) == true)
                    continue;

                // ͬ���޸Ĳ��¼�еĽ���֤�����
                // return:
                //      -1  error
                //      0   entitybarcode not found
                //      1   found and changed
                nRet = ChangeEntityBorrower(
                    Channels,
                    strEntityBarcode,
                    strSourceReaderBarcode,
                    strTargetReaderBarcode,
                    strOperTimeString,
                    ref domOperLog,
                    attachment,
                    ref nAttachmentIndex,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    // ��ʱ�����
                }

            }

            // Դ��¼�и��´���һ��ע��Ԫ��
            // ����ԭ<borrows>Ԫ���е��������룬�����պ󱸲�
            XmlNode nodeComment = source_dom.CreateElement("devolvedBorrows");
            source_dom.DocumentElement.AppendChild(nodeComment);
            nodeComment.InnerXml = nodeSourceBorrows.InnerXml;

            DomUtil.SetAttr(nodeComment, "comment", "���� " + strOperTimeString + " �����н�����Ϣת�Ƶ����� " + strTargetReaderBarcode + " ����");

            // ������־��¼��ϢҪ��
            if (domOperLog != null)
            {
                // ��־��¼�е�<borrows>Ԫ���ڴ��������Դ���߼�¼�����ƶ���Ŀ����߼�¼����Щ<borrow>Ԫ��
                DomUtil.SetElementInnerXml(domOperLog.DocumentElement,
                    "borrows",
                    nodeSourceBorrows.InnerXml);
            }

            // ɾ��Դ��¼�е�<borrows/borrow>Ԫ��
            nodeSourceBorrows.InnerXml = "";


            return 1;
        }

        // �ƶ�����ΥԼ����Ϣ -- <overdues>Ԫ������
        // �˺���Ҳ����־�ָ�ģ����ʹ�ã�ֻ�ǻָ�ʱdomOperLogΪnull
        // return:
        //      -1  error
        //      0   not found overdueinfo
        //      1   found and moved
        int DevolveOverdueInfo(
            string strSourceReaderBarcode,
            string strTargetReaderBarcode,
            string strOperTimeString,
            ref XmlDocument source_dom,
            ref XmlDocument target_dom,
            ref XmlDocument domOperLog,
            out string strError)
        {
            strError = "";
            // int nRet = 0;

            // �ƶ�����ΥԼ����Ϣ
            XmlNode nodeSourceOverdues = source_dom.DocumentElement.SelectSingleNode("overdues");
            if (nodeSourceOverdues == null)
                return 0;

            XmlNodeList nodesSourceOverdue = nodeSourceOverdues.SelectNodes("overdue");
            if (nodesSourceOverdue.Count == 0)
                return 0;

            XmlNode nodeTargetOverdues = target_dom.DocumentElement.SelectSingleNode("overdues");
            if (nodeTargetOverdues == null)
            {
                nodeTargetOverdues = target_dom.CreateElement("overdues");
                target_dom.DocumentElement.AppendChild(nodeTargetOverdues);
            }

            for (int i = 0; i < nodesSourceOverdue.Count; i++)
            {
                XmlNode source = nodesSourceOverdue[i];

                // ��<overdue>Ԫ��
                XmlDocumentFragment fragment = target_dom.CreateDocumentFragment();
                fragment.InnerXml = source.OuterXml;

                XmlNode target = nodeTargetOverdues.AppendChild(fragment);

                // ����һ��ע��Ԫ��
                DomUtil.SetAttr(target, "devolveComment", "�Ӷ��� " +strSourceReaderBarcode+ " ת�ƶ���������ʱ�� " + strOperTimeString);


            }

            // Դ��¼�и��´���һ��ע��Ԫ��
            // ����ԭ<overdues>Ԫ���е��������룬�����պ󱸲�
            XmlNode nodeComment = source_dom.CreateElement("devolvedOverdues");
            source_dom.DocumentElement.AppendChild(nodeComment);
            nodeComment.InnerXml = nodeSourceOverdues.InnerXml;

            DomUtil.SetAttr(nodeComment, "comment", "���� " 
                +strOperTimeString
                + " �����г�����Ϣת�Ƶ����� " +strTargetReaderBarcode+ " ����");

            // ������־��¼��ϢҪ��
            if (domOperLog != null)
            {
                // ��־��¼�е�<overdues>Ԫ���ڴ��������Դ���߼�¼�����ƶ���Ŀ����߼�¼����Щ<overdue>Ԫ��
                DomUtil.SetElementInnerXml(domOperLog.DocumentElement,
                    "overdues",
                    nodeSourceOverdues.InnerXml);
            }


            // ɾ��Դ��¼�е�<overdues/overdue>Ԫ��
            nodeSourceOverdues.InnerXml = "";

            return 1;
        }

        // �޸Ĳ��¼�еĽ���֤�����
        // parameters:
        //      domOperLog  ��־��¼DOM�������==null����ʾ������������־��������־DOM��
        //      attachment    ���!=null��ʾҪ��ʵ���¼���浽��־��attachment�С����==null����ʾֱ�Ӱ�ʵ���¼���浽��־��¼(DOM)��
        //      nAttachmentIndex    ��־������¼index����һ�ε��õ�ʱ�򣬴�ֵӦΪ0��Ȼ�������������������־������¼�����Զ��������ֵ
        // return:
        //      -1  error
        //      0   entitybarcode not found
        //      1   found and changed
        int ChangeEntityBorrower(
            RmsChannelCollection Channels,
            string strEntityBarcode,
            string strOldReaderBarcode,
            string strNewReaderBarcode,
            string strOperTimeString,
            ref XmlDocument domOperLog,
            Stream attachment,
            ref int nAttachmentIndex,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            long lRet = 0;

            // �Ӳ��¼��
            this.EntityLocks.LockForWrite(strEntityBarcode);

            try // ���¼������Χ��ʼ
            {

                // �Ӳ�����Ż�ò��¼
                byte[] item_timestamp = null;
                List<string> aPath = null;
                string strItemXml = "";
                string strOutputItemRecPath = "";

                int nRedoCount = 0;
                REDO:

                // ��ò��¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                nRet = this.GetItemRecXml(
                    Channels,
                    strEntityBarcode,
                    out strItemXml,
                    100,
                    out aPath,
                    out item_timestamp,
                    out strError);
                if (nRet == 0)
                {
                    strError = "������� '" + strEntityBarcode + "' ������";
                    return 0;
                }
                if (nRet == -1)
                {
                    strError = "������¼ '"+strEntityBarcode+"' ʱ��������: " + strError;
                    goto ERROR1;
                }

                RmsChannel channel = null;

                if (aPath.Count > 1)
                {
                    /*
                    strError = "�������Ϊ '" + strEntityBarcode + "' �Ĳ��¼�� " + aPath.Count.ToString() + " �����޷������޸�";
                    return -1;
                     * */

                    // bItemBarcodeDup = true; // ��ʱ�Ѿ���Ҫ����״̬����Ȼ������Խ�һ��ʶ��������Ĳ��¼

                    // ����strDupBarcodeList
                    /*
                    string[] pathlist = new string[aPath.Count];
                    aPath.CopyTo(pathlist);
                    string strDupBarcodeList = String.Join(",", pathlist);
                     * */
                    string strDupBarcodeList = StringUtil.MakePathList(aPath);

                    List<string> aFoundPath = null;
                    List<byte[]> aTimestamp = null;
                    List<string> aItemXml = null;

                    if (String.IsNullOrEmpty(strOldReaderBarcode) == true)
                    {
                        strError = "strOldReaderBarcode����ֵ����Ϊ��";
                        goto ERROR1;
                    }

                    channel = Channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "get channel error";
                        goto ERROR1;
                    }

                    // �������ظ�����ŵĲ��¼�У�ѡ�����з��ϵ�ǰ����֤����ŵ�
                    // return:
                    //      -1  ����
                    //      ����    ѡ��������
                    nRet = FindItem(
                        channel,
                        strOldReaderBarcode,
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
                        strError = "������� '" + strEntityBarcode + "' �������� " + aPath.Count + " ����¼�У�û���κ�һ����<borrower>Ԫ�ر����˱����� '" + strOldReaderBarcode + "' ���ġ�";
                        goto ERROR1;
                    }

                    if (nRet > 1)
                    {
                        strError = "�������Ϊ '" + strEntityBarcode + "' ����<borrower>Ԫ�ر���Ϊ���� '" + strOldReaderBarcode + "' ���ĵĲ��¼�� " + aFoundPath.Count.ToString() + " �����޷������ƶ�������";
                        goto ERROR1;
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

                XmlDocument itemdom = null;
                nRet = LibraryApplication.LoadToDom(strItemXml,
                    out itemdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�ز��¼����XML DOMʱ��������: " + strError;
                    goto ERROR1;
                }

                string strBorrower = DomUtil.GetElementText(itemdom.DocumentElement,
                    "borrower");

                if (String.IsNullOrEmpty(strBorrower) == true)
                {
                    strError = "ʵ���¼��û�н�����Ϣ(<borrower>Ԫ������)";
                    goto ERROR1;
                }

                // �˶Ծɶ���֤�����
                if (strBorrower != strOldReaderBarcode)
                {
                    strError = "ʵ���¼�У����н���֤����� '" + strBorrower + "' �������ĸ�ǰ֤����� '" + strOldReaderBarcode + "' ��һ��...";
                    goto ERROR1;
                }

                // �޸�Ϊ�¶���֤�����
                DomUtil.SetElementText(itemdom.DocumentElement,
                    "borrower",
                    strNewReaderBarcode);

                // ����һ��ע��
                DomUtil.SetElementText(itemdom.DocumentElement,
                    "devolveComment",
                    "����ԭΪ���� " + strOldReaderBarcode + " �����ģ����� "
                    +strOperTimeString+" ��ת�Ƶ����� "+strNewReaderBarcode+" ����");

                if (channel == null)
                {
                    channel = Channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "get channel error";
                        goto ERROR1;
                    }
                }

                // ����ʵ���¼
                byte[] output_timestamp = null;
                string strOutputPath = "";

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
                        if (nRedoCount > 10)
                            goto ERROR1;
                        nRedoCount++;
                        item_timestamp = output_timestamp;
                        goto REDO;
                    }
                }

                // �������˵ļ�¼д����־
                if (domOperLog != null)
                {
                    XmlNode nodeLogRecord = domOperLog.CreateElement("changedEntityRecord");
                    domOperLog.DocumentElement.AppendChild(nodeLogRecord);
                    DomUtil.SetAttr(nodeLogRecord, "recPath", strOutputPath);

                    if (attachment == null)
                    {
                        // ʵ���¼��ȫ���浽��־��¼��

                        nodeLogRecord.InnerText = itemdom.OuterXml;
                    }
                    else
                    {
                        // ʵ���¼���浽�����У�ֻ����־��¼���������

                        // ���渽�����
                        DomUtil.SetAttr(nodeLogRecord, "attachmentIndex", nAttachmentIndex.ToString());

                        byte [] content = Encoding.UTF8.GetBytes(itemdom.OuterXml);
                        byte[] length = BitConverter.GetBytes((long)content.LongLength);
                        attachment.Write(length, 0, length.Length);
                        attachment.Write(content, 0, content.Length);

                        nAttachmentIndex ++;
                    }
                }

            }
            finally
            {
                this.EntityLocks.UnlockForWrite(strEntityBarcode);
            }
            return 1;
        ERROR1:
            return -1;
        }

        // ���һ�����߼�¼�Ľ軹��Ϣ�Ƿ��쳣��
        // parameters:
        //      nStart      �ӵڼ������ĵĲ����ʼ����
        //      nCount      �����������ĵĲ�����
        //      nProcessedBorrowItems   [out]���δ����˶��ٸ����Ĳ�����
        //      nTotalBorrowItems   [out]��ǰ����һ�������ж��ٸ����Ĳ�����
        // result.Value
        //      -1  ����
        //      0   ����޴�
        //      1   ��鷢���д�
        public LibraryServerResult CheckReaderBorrowInfo(
            RmsChannelCollection Channels,
            string strReaderBarcode,
            int nStart,
            int nCount,
            out int nProcessedBorrowItems,
            out int nTotalBorrowItems)
        {
            string strError = "";
            int nRet = 0;
            nTotalBorrowItems = 0;
            nProcessedBorrowItems = 0;

            string strCheckError = "";

            LibraryServerResult result = new LibraryServerResult();
            int nErrorCount = 0;

            // �Ӷ��߼�¼��
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


                    XmlNode node = nodesBorrow[i];

                    string strItemBarcode = DomUtil.GetAttr(node, "barcode");

                    nProcessedBorrowItems++;

                    string strOutputReaderBarcode_0 = "";

                    string[] aDupPath = null;
                   // ���һ��ʵ���¼�Ľ軹��Ϣ�Ƿ��쳣��
                    LibraryServerResult result_1 = CheckItemBorrowInfo(
                        Channels,
                        strReaderBarcode,
                        readerdom,
                        strOutputReaderRecPath,
                        strItemBarcode,
                        null,
                        out strOutputReaderBarcode_0,
                        out aDupPath);
                    if (result_1.Value == -1 || result_1.Value == 1)
                    {
                        if (result_1.ErrorCode == ErrorCode.ItemBarcodeDup)
                        {
                            List<string> linkedPath = new List<string>();

                            for (int j = 0; j < aDupPath.Length; j++)
                            {
                                string[] aDupPathTemp = null;
                                string strOutputReaderBarcode = "";
                                LibraryServerResult result_2 = CheckItemBorrowInfo(
                                    Channels,
                                    strReaderBarcode,
                                    readerdom,
                                    strOutputReaderRecPath,
                                    strItemBarcode,
                                    aDupPath[j],
                                    out strOutputReaderBarcode,
                                    out aDupPathTemp);
                                if (result_2.Value == -1)
                                {
                                    strError = result_2.ErrorInfo;
                                    goto ERROR1;
                                }



                                if (strOutputReaderBarcode == strReaderBarcode)
                                {
                                    linkedPath.Add(aDupPath[j]);

                                    if (result_2.Value == 1)
                                    {
                                        strCheckError += "�����߼�¼�н��Ĳ������ " + strItemBarcode + " �����Ĳ��¼(��¼·�� " + aDupPath[j] + ") ʱ���ִ���: " + result_1.ErrorInfo + "��";
                                        nErrorCount++;
                                    }
                                }

                            } // end of for

                            if (linkedPath.Count == 0)
                            {
                                strCheckError += "���߼�¼�н��Ĳ������ " + strItemBarcode + " ������ " + aDupPath.Length + " �����¼��������Щ���¼��û���κ�һ�����й����ض���֤����ŵĽ�����Ϣ��";
                                nErrorCount++;
                            }

                            if (linkedPath.Count > 1)
                            {
                                strCheckError += "���߼�¼�н��Ĳ������ " + strItemBarcode + " ������ " + aDupPath.Length + " �����¼����Щ���¼���� " + linkedPath.Count.ToString() + "�����й����ض���֤����ŵĽ�����Ϣ��";
                                nErrorCount++;
                            }

                            continue;
                        }

                        if (result_1.ErrorCode == ErrorCode.ReaderBarcodeNotFound)
                        {
                            strCheckError += "���߼�¼�н��Ĳ������ " + strItemBarcode + " �����Ĳ��¼�У���<borrower>�ֶι����صĶ���֤������� " + strOutputReaderBarcode_0 + "�������ǳ����Ķ���֤����� " + strReaderBarcode + "������֤�����Ϊ " + strOutputReaderBarcode_0 + " �Ķ��߼�¼�����ڡ�";
                            nErrorCount++;
                            continue;
                        }

                        if (result_1.Value == -1)
                        {
                            strCheckError += "�����߼�¼�н��Ĳ������ " + strItemBarcode + " �����Ĳ��¼ʱ��������: " + result_1.ErrorInfo + "��";
                            nErrorCount++;
                        }
                        if (result_1.Value == 1)
                        {
                            strCheckError += "�����߼�¼�н��Ĳ������ " + strItemBarcode + " �����Ĳ��¼ʱ���ִ���: " + result_1.ErrorInfo + "��";
                            nErrorCount++;
                        }
                        continue;
                    } // end of return -1


                    if (strOutputReaderBarcode_0 != strReaderBarcode)
                    {
                        strCheckError += "���߼�¼�н��Ĳ������ " + strItemBarcode + " �����Ĳ��¼�У���<borrower>�ֶι����صĶ���֤������� " + strOutputReaderBarcode_0 + "�������ǳ����Ķ���֤����� " + strReaderBarcode + "��";
                        nErrorCount++;
                    }
                }


            }
            finally
            {
                this.ReaderLocks.UnlockForWrite(strReaderBarcode);
            }

            if (String.IsNullOrEmpty(strCheckError) == false)
            {
                result.Value = 1;
                result.ErrorInfo = strCheckError;
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

        // ���һ��ʵ���¼�Ľ軹��Ϣ�Ƿ��쳣��
        // parameters:
        //      strLockedReaderBarcode  ����Ѿ�������������š����������������Ϣ�����Ա����ظ�������
        //      exist_readerdom �Ѿ�װ����DOM�Ķ��߼�¼�������֤�������strLockedReaderBarcode������ṩ�����ֵ�����������Ż����ܡ�
        // result.Value
        //      -1  ����
        //      0   ʵ���¼��û�н�����Ϣ�����߼�鷢���޴�
        //      1   ��鷢���д�
        public LibraryServerResult CheckItemBorrowInfo(
            RmsChannelCollection Channels,
            string strLockedReaderBarcode,
            XmlDocument exist_readerdom,
            string strExistReaderRecPath,
            string strItemBarcode,
            string strConfirmItemRecPath,
            out string strOutputReaderBarcode,
            out string[] aDupPath)
        {
            string strError = "";
            aDupPath = null;
            strOutputReaderBarcode = "";
            long lRet = 0;
            int nRet = 0;

            string strCheckError = "";

            LibraryServerResult result = new LibraryServerResult();

            if (exist_readerdom != null)
            {
                if (String.IsNullOrEmpty(strExistReaderRecPath) == true)
                {
                    strError = "���exist_readerdom������Ϊ�գ���strExistReaderRecPathҲ��ӦΪ�ա�";
                    goto ERROR1;
                }
            }



            string strOutputItemRecPath = "";
            byte[] item_timestamp = null;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            string strItemXml = "";

            // ����Ѿ���ȷ���Ĳ��¼·��
            if (String.IsNullOrEmpty(strConfirmItemRecPath) == false)
            {
                // ���·���еĿ������ǲ���ʵ�����
                // return:
                //      -1  error
                //      0   ����ʵ�����
                //      1   ��ʵ�����
                nRet = this.CheckItemRecPath(strConfirmItemRecPath,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    strError = strConfirmItemRecPath + strError;
                    goto ERROR1;
                }

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
                    result.Value = -1;
                    result.ErrorInfo = "������� '" + strItemBarcode + "' ������";
                    result.ErrorCode = ErrorCode.ItemBarcodeNotFound;
                    return result;
                }
                if (nRet == -1)
                {
                    strError = "������¼ʱ��������: " + strError;
                    goto ERROR1;
                }

                if (aPath.Count > 1)
                {
                    /*
                    // ����strDupBarcodeList
                    string[] pathlist = new string[aPath.Count];
                    aPath.CopyTo(pathlist);
                    strDupBarcodeList = String.Join(",", pathlist);
                     * */

                    result.Value = -1;
                    result.ErrorInfo = "�������Ϊ '" + strItemBarcode + "' �Ĳ��¼�� " + aPath.Count.ToString() + " �����޷������޸����������ڸ��Ӳ��¼·���������ύ�޸�������";
                    result.ErrorCode = ErrorCode.ItemBarcodeDup;

                    aDupPath = new string[aPath.Count];
                    aPath.CopyTo(aDupPath);
                    return result;
                }
                else
                {
                    Debug.Assert(nRet == 1, "");
                    Debug.Assert(aPath.Count == 1, "");
                    if (nRet == 1)
                    {
                        strOutputItemRecPath = aPath[0];
                        // strItemXml�Ѿ��в��¼��
                    }
                }

                // �������غ�����
                aDupPath = new string[1];
                aDupPath[0] = strOutputItemRecPath;
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

            strOutputReaderBarcode = ""; // ������֤�����

            strOutputReaderBarcode = DomUtil.GetElementText(itemdom.DocumentElement,
    "borrower");
            if (String.IsNullOrEmpty(strOutputReaderBarcode) == true)
            {
                strError = "���¼��<borrower>Ԫ��ֵ�����òᵱǰ��δ���κζ��߽���";
                result.Value = 0;   // 2008/1/25 comment ��ʱ�޷��϶��Ƿ�Ϊ���󡣻���ҪstrOutputReaderBarcode���غ���бȽϲ���ȷ��
                result.ErrorInfo = strError;
                return result;
            }

            // �������߼�¼�������Ƿ���borrows/borrowԪ�ر���������������
            // �Ӷ��߼�¼��
            if (strLockedReaderBarcode != strOutputReaderBarcode)
                this.ReaderLocks.LockForWrite(strOutputReaderBarcode);

            try // ���߼�¼������Χ��ʼ
            {
                // ������߼�¼
                string strReaderXml = "";
                string strOutputReaderRecPath = "";
                byte[] reader_timestamp = null;

                XmlDocument readerdom = null;

                if (exist_readerdom == null)
                {
                    nRet = this.GetReaderRecXml(
                        Channels,
                        strOutputReaderBarcode,
                        out strReaderXml,
                        out strOutputReaderRecPath,
                        out reader_timestamp,
                        out strError);
                    if (nRet == 0)
                    {
                        result.Value = 1;
                        result.ErrorInfo = "����֤����� '" + strOutputReaderBarcode + "' ������";
                        result.ErrorCode = ErrorCode.ReaderBarcodeNotFound;
                        return result;
                    }
                    if (nRet == -1)
                    {
                        strError = "������߼�¼ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    nRet = LibraryApplication.LoadToDom(strReaderXml,
                        out readerdom,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                        goto ERROR1;
                    }
                }
                else
                {
                    readerdom = exist_readerdom;
                    strOutputReaderRecPath = strExistReaderRecPath;
                }

                XmlNodeList nodesBorrow = readerdom.DocumentElement.SelectNodes("borrows/borrow[@barcode='"+strItemBarcode+"']");
                if (nodesBorrow.Count == 0)
                {
                    strCheckError += "��Ȼ���¼ " + strOutputItemRecPath + " �б����˱����� '" + strOutputReaderBarcode + "' ���ģ����Ƕ��߼�¼ " + strOutputReaderRecPath + " �в�û�й��ڲ������ '" + strItemBarcode + "' �Ľ��ļ�¼��";
                    goto END1;
                }
                if (nodesBorrow.Count > 1)
                {
                    strCheckError = "���߼�¼�� " + strOutputReaderRecPath + " �й��ڲ������ '" + strItemBarcode + "' �� " + nodesBorrow.Count.ToString() + " ���������ļ�¼��";
                    goto END1;
                }

                Debug.Assert(nodesBorrow.Count == 1, "");


            }
            finally
            {
                if (strLockedReaderBarcode != strOutputReaderBarcode)
                    this.ReaderLocks.UnlockForWrite(strOutputReaderBarcode);
            }

        END1:
            if (String.IsNullOrEmpty(strCheckError) == false)
            {
                result.Value = 1;
                result.ErrorInfo = strCheckError;
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

        // �޸����߼�¼һ��Ľ�����Ϣ��������
        // ������˵�����Ƕ�������н�����Ϣ������ָ���ʵ�岻���ڣ�������Ȼ���ڵ���
        // ����û��ָ�ص�����
        public LibraryServerResult RepairReaderSideError(
            SessionInfo sessioninfo,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            out string [] aDupPath)
        {
            string strError = "";
            aDupPath = null;
            int nRet = 0;
            long lRet = 0;

            LibraryServerResult result = new LibraryServerResult();

            int nRedoCount = 0; // ��Ϊʱ�����ͻ, ���ԵĴ���

            if (String.IsNullOrEmpty(strReaderBarcode) == true)
            {
                strError = "����֤����Ų���Ϊ�ա�";
                goto ERROR1;
            }
            if (String.IsNullOrEmpty(strItemBarcode) == true)
            {
                strError = "������Ų���Ϊ�ա�";
                goto ERROR1;
            }
REDO_REPAIR:

    /*
            string strOutputReaderXml = "";
            string strOutputItemXml = "";
     * */


            // �Ӷ��߼�¼��
            this.ReaderLocks.LockForWrite(strReaderBarcode);

            try // ���߼�¼������Χ��ʼ
            {
                // ������߼�¼
                string strReaderXml = "";
                string strOutputReaderRecPath = "";
                byte[] reader_timestamp = null;
                nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
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

                string strLibraryCode = "";
                // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
                    sessioninfo.LibraryCodeList,
                    out strLibraryCode) == false)
                {
                    strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
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

                // У�����֤����Ų����Ƿ��XML��¼����ȫһ��
                string strTempBarcode = DomUtil.GetElementText(readerdom.DocumentElement,
                    "barcode");
                if (strReaderBarcode != strTempBarcode)
                {
                    strError = "�޸��������ܾ��������֤����Ų��� '" + strReaderBarcode + "' �Ͷ��߼�¼��<barcode>Ԫ���ڵĶ���֤�����ֵ '" + strTempBarcode + "' ��һ�¡�";
                    goto ERROR1;
                }

                XmlNode nodeBorrow = readerdom.DocumentElement.SelectSingleNode("borrows/borrow[@barcode='"+strItemBarcode+"']");
                if (nodeBorrow == null)
                {
                    strError = "�޸��������ܾ������߼�¼ "+strReaderBarcode+" �в��������йز� "+strItemBarcode+" �Ľ�����Ϣ��";
                    goto ERROR1;
                }

                byte[] item_timestamp = null;
                List<string> aPath = null;
                string strItemXml = "";
                string strOutputItemRecPath = "";

                RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    goto ERROR1;
                }

                // �Ӳ��¼��
                this.EntityLocks.LockForWrite(strItemBarcode);

                try // ���¼������Χ��ʼ
                {
                    // ������¼

                    // ����Ѿ���ȷ���Ĳ��¼·��
                    if (String.IsNullOrEmpty(strConfirmItemRecPath) == false)
                    {
                        // ���·���еĿ������ǲ���ʵ�����
                        // return:
                        //      -1  error
                        //      0   ����ʵ�����
                        //      1   ��ʵ�����
                        nRet = this.CheckItemRecPath(strConfirmItemRecPath, out strError);
                        if (nRet != 1)
                            goto ERROR1;

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
                    }
                    else
                    {
                        // �Ӳ�����Ż�ò��¼

                        // ��ò��¼
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   ����1��
                        //      >1  ���ж���1��
                        nRet = this.GetItemRecXml(
                            sessioninfo.Channels,
                            strItemBarcode,
                            out strItemXml,
                            100,
                            out aPath,
                            out item_timestamp,
                            out strError);
                        if (nRet == 0)
                        {
                            /*
                            result.Value = -1;
                            result.ErrorInfo = "������� '" + strItemBarcode + "' ������";
                            result.ErrorCode = ErrorCode.ItemBarcodeNotFound;
                            return result;
                             * */
                            // ������Ų�����Ҳ����Ҫ�޸������֮һ��
                            // bItemRecordNotFound = true;
                            goto DELETE_CHAIN;
                        }
                        if (nRet == -1)
                        {
                            strError = "������¼ʱ��������: " + strError;
                            goto ERROR1;
                        }

                        if (aPath.Count > 1)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "�������Ϊ '" + strItemBarcode + "' �Ĳ��¼�� " + aPath.Count.ToString() + " �����޷������޸����������ڸ��Ӳ��¼·���������ύ�޸�������";
                            result.ErrorCode = ErrorCode.ItemBarcodeDup;

                            aDupPath = new string[aPath.Count];
                            aPath.CopyTo(aDupPath);
                            return result;

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

                    // У�������Ų����Ƿ��XML��¼����ȫһ��
                    string strTempItemBarcode = DomUtil.GetElementText(itemdom.DocumentElement,
                        "barcode");
                    if (strItemBarcode != strTempItemBarcode)
                    {
                        strError = "�޸��������ܾ����������Ų��� '" + strItemBarcode + "' �Ͳ��¼��<barcode>Ԫ���ڵĲ������ֵ '" + strTempItemBarcode + "' ��һ�¡�";
                        goto ERROR1;
                    }

                    // �������¼���Ƿ���ָ�ض��߼�¼����
                    string strBorrower = DomUtil.GetElementText(itemdom.DocumentElement,
                        "borrower");
                    if (strBorrower == strReaderBarcode)
                    {
                        strError = "�޸��������ܾ�����������Ҫ�޸�����������һ��������ȷ��������ֱ�ӽ�����ͨ���������";
                        goto ERROR1;
                    }

                    DELETE_CHAIN:

                    // �Ƴ����߼�¼�����
                    nodeBorrow.ParentNode.RemoveChild(nodeBorrow);

                    byte[] output_timestamp = null;
                    string strOutputPath = "";

                    // д�ض��߼�¼
                    lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
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
                            goto REDO_REPAIR;
                        }
                        goto ERROR1;
                    }

                    // ��ʱ����ʱ���
                    reader_timestamp = output_timestamp;

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
                     * * */

                    XmlDocument domOperLog = new XmlDocument();
                    domOperLog.LoadXml("<root />");
                    DomUtil.SetElementText(domOperLog.DocumentElement,
    "libraryCode",
    strLibraryCode);    // �������ڵĹݴ���
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operation", "repairBorrowInfo");
                    DomUtil.SetElementText(domOperLog.DocumentElement, "action", "repairreaderside");
                    DomUtil.SetElementText(domOperLog.DocumentElement, "readerBarcode", strReaderBarcode);
                    DomUtil.SetElementText(domOperLog.DocumentElement, "itemBarcode", strItemBarcode);
                    if (String.IsNullOrEmpty(strConfirmItemRecPath) == false)
                        DomUtil.SetElementText(domOperLog.DocumentElement, "confirmItemRecPath", strConfirmItemRecPath);
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
    sessioninfo.UserID);
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                        this.Clock.GetClock());

                    nRet = this.OperLog.WriteOperLog(domOperLog,
                        sessioninfo.ClientAddress,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "RepairReaderSideError() API д����־ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    // д��ͳ��ָ��
                    if (this.Statis != null)
                        this.Statis.IncreaseEntryValue(
                        strLibraryCode,
                        "�޸�������Ϣ",
                        "���߲����",
                        1);

                } // ���¼������Χ����
                finally
                {
                    // ����¼��
                    this.EntityLocks.UnlockForWrite(strItemBarcode);
                }

            }
            finally
            {
                this.ReaderLocks.UnlockForWrite(strReaderBarcode);
            }

            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        // �޸����¼һ��Ľ�����Ϣ��������
        // ������˵�����ǲ�����н�����Ϣ������ָ��Ķ��߼�¼�����ڣ�������Ȼ���ڵ���
        // ����û��ָ�ص�����
        public LibraryServerResult RepairItemSideError(
            SessionInfo sessioninfo,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            out string[] aDupPath)
        {
            string strError = "";
            aDupPath = null;
            int nRet = 0;
            long lRet = 0;

            LibraryServerResult result = new LibraryServerResult();

            int nRedoCount = 0; // ��Ϊʱ�����ͻ, ���ԵĴ���

            if (String.IsNullOrEmpty(strReaderBarcode) == true)
            {
                strError = "����֤����Ų���Ϊ�ա�";
                goto ERROR1;
            }
            if (String.IsNullOrEmpty(strItemBarcode) == true)
            {
                strError = "������Ų���Ϊ�ա�";
                goto ERROR1;
            }
        REDO_REPAIR:

            // �Ӷ��߼�¼��
            this.ReaderLocks.LockForWrite(strReaderBarcode);

            try // ���߼�¼������Χ��ʼ
            {
                // ������߼�¼
                string strReaderXml = "";
                string strOutputReaderRecPath = "";
                byte[] reader_timestamp = null;
                nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
                    strReaderBarcode,
                    out strReaderXml,
                    out strOutputReaderRecPath,
                    out reader_timestamp,
                    out strError);
                if (nRet == -1)
                {
                    strError = "������߼�¼ʱ��������: " + strError;
                    goto ERROR1;
                }

                string strLibraryCode = "";
                // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
                    sessioninfo.LibraryCodeList,
                    out strLibraryCode) == false)
                {
                    strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                    goto ERROR1;
                }

                XmlDocument readerdom = null;
                if (nRet == 0)
                {
                    /*
                    strError = "����֤����� '" + strReaderBarcode + "' ������";
                    goto ERROR1;
                     * */
                }
                else
                {
                    nRet = LibraryApplication.LoadToDom(strReaderXml,
                    out readerdom,
                    out strError);
                    if (nRet == -1)
                    {
                        strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                        goto ERROR1;
                    }


                    // У�����֤����Ų����Ƿ��XML��¼����ȫһ��
                    string strTempBarcode = DomUtil.GetElementText(readerdom.DocumentElement,
                        "barcode");
                    if (strReaderBarcode != strTempBarcode)
                    {
                        strError = "�޸��������ܾ��������֤����Ų��� '" + strReaderBarcode + "' �Ͷ��߼�¼��<barcode>Ԫ���ڵĶ���֤�����ֵ '" + strTempBarcode + "' ��һ�¡�";
                        goto ERROR1;
                    }
                }

                byte[] item_timestamp = null;
                List<string> aPath = null;
                string strItemXml = "";
                string strOutputItemRecPath = "";

                RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    goto ERROR1;
                }

                // �Ӳ��¼��
                this.EntityLocks.LockForWrite(strItemBarcode);

                try // ���¼������Χ��ʼ
                {
                    // ������¼

                    // ����Ѿ���ȷ���Ĳ��¼·��
                    if (String.IsNullOrEmpty(strConfirmItemRecPath) == false)
                    {
                        // ���·���еĿ������ǲ���ʵ�����
                        // return:
                        //      -1  error
                        //      0   ����ʵ�����
                        //      1   ��ʵ�����
                        nRet = this.CheckItemRecPath(strConfirmItemRecPath, out strError);
                        if (nRet != 1)
                            goto ERROR1;

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
                    }
                    else
                    {
                        // �Ӳ�����Ż�ò��¼

                        // ��ò��¼
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   ����1��
                        //      >1  ���ж���1��
                        nRet = this.GetItemRecXml(
                            sessioninfo.Channels,
                            strItemBarcode,
                            out strItemXml,
                            100,
                            out aPath,
                            out item_timestamp,
                            out strError);
                        if (nRet == 0)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "������� '" + strItemBarcode + "' ������";
                            result.ErrorCode = ErrorCode.ItemBarcodeNotFound;
                            return result;
                        }
                        if (nRet == -1)
                        {
                            strError = "������¼ʱ��������: " + strError;
                            goto ERROR1;
                        }

                        if (aPath.Count > 1)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "�������Ϊ '" + strItemBarcode + "' �Ĳ��¼�� " + aPath.Count.ToString() + " �����޷������޸����������ڸ��Ӳ��¼·���������ύ�޸�������";
                            result.ErrorCode = ErrorCode.ItemBarcodeDup;

                            aDupPath = new string[aPath.Count];
                            aPath.CopyTo(aDupPath);
                            return result;

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

                    // У�������Ų����Ƿ��XML��¼����ȫһ��
                    string strTempItemBarcode = DomUtil.GetElementText(itemdom.DocumentElement,
                        "barcode");
                    if (strItemBarcode != strTempItemBarcode)
                    {
                        strError = "�޸��������ܾ����������Ų��� '" + strItemBarcode + "' �Ͳ��¼��<barcode>Ԫ���ڵĲ������ֵ '" + strTempItemBarcode + "' ��һ�¡�";
                        goto ERROR1;
                    }

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
                        strError = "�޸��������ܾ�����������Ҫ�޸��Ĳ��¼�У���û��ָ���������Ƕ��� "+strReaderBarcode+"��";
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

                // DELETE_CHAIN:

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
                                strError = "д�ز��¼��ʱ��,����ʱ�����ͻ,���������10��,��ʧ��...";
                                goto ERROR1;
                            }
                            goto REDO_REPAIR;
                        }
                        goto ERROR1;
                    } // end of д�ز��¼ʧ��

                    XmlDocument domOperLog = new XmlDocument();
                    domOperLog.LoadXml("<root />");
                    DomUtil.SetElementText(domOperLog.DocumentElement,
    "libraryCode",
    strLibraryCode);    // �������ڵĹݴ���
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operation", "repairBorrowInfo");
                    DomUtil.SetElementText(domOperLog.DocumentElement, "action", "repairitemside");
                    DomUtil.SetElementText(domOperLog.DocumentElement, "readerBarcode", strReaderBarcode);
                    DomUtil.SetElementText(domOperLog.DocumentElement, "itemBarcode", strItemBarcode);
                    if (String.IsNullOrEmpty(strConfirmItemRecPath) == false)
                        DomUtil.SetElementText(domOperLog.DocumentElement, "confirmItemRecPath", strConfirmItemRecPath);
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                        sessioninfo.UserID);
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                        this.Clock.GetClock());

                    nRet = this.OperLog.WriteOperLog(domOperLog,
                        sessioninfo.ClientAddress,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "RepairItemSideError() API д����־ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    // д��ͳ��ָ��
                    if (this.Statis != null)
                        this.Statis.IncreaseEntryValue(
                        strLibraryCode,
                        "�޸�������Ϣ",
                        "ʵ������",
                        1);


                } // ���¼������Χ����
                finally
                {
                    // ����¼��
                    this.EntityLocks.UnlockForWrite(strItemBarcode);
                }

            }
            finally
            {
                this.ReaderLocks.UnlockForWrite(strReaderBarcode);
            }

            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        // ��ݵǼ�
        // result.Value -1 ���� ���� �ض���(strGateName)�ı��ε��ۼ���
        public LibraryServerResult PassGate(
            SessionInfo sessioninfo,
            string strReaderBarcode,
            string strGateName,
            string strResultTypeList,
            out string[] results)
        {
            string strError = "";
            int nRet = 0;
            results = null;

            int nResultValue = 0;

            LibraryServerResult result = new LibraryServerResult();

            if (string.IsNullOrEmpty(strReaderBarcode) == false)
            {
                string strOutputCode = "";
                // �Ѷ�ά���ַ���ת��Ϊ����֤�����
                // parameters:
                //      strReaderBcode  [out]����֤�����
                // return:
                //      -1      ����
                //      0       ���������ַ������Ƕ���֤�Ŷ�ά��
                //      1       �ɹ�      
                nRet = DecodeQrCode(strReaderBarcode,
                    out strOutputCode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 1)
                {
                    // strQrCode = strBarcode;
                    strReaderBarcode = strOutputCode;
                }
            }

            if (sessioninfo.UserType == "reader")
            {
                // TODO: ���ʹ�����֤�ţ��ƺ�����������谭
                if (strReaderBarcode != sessioninfo.Account.Barcode)
                {
                    result.Value = -1;
                    result.ErrorInfo = "��ö�����Ϣ���ܾ�����Ϊ����ֻ�ܶ��Լ�������ݵǼǲ���";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }


            string strReaderXml = "";
            string strOutputReaderRecPath = "";
            string strLibraryCode = "";

            // �Ӷ��߼�¼��
            this.ReaderLocks.LockForRead(strReaderBarcode);
            try // ���߼�¼������Χ��ʼ
            {

                // ������߼�¼
                byte[] reader_timestamp = null;
                nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
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

                nRet = this.GetLibraryCode(strOutputReaderRecPath,
                    out strLibraryCode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (string.IsNullOrEmpty(strLibraryCode) == false)
                    strGateName = strLibraryCode + ":" + strGateName;

                // �������߼�¼�������Ķ��߿�Ĺݴ��룬�Ƿ񱻵�ǰ�û���Ͻ
                if (String.IsNullOrEmpty(strOutputReaderRecPath) == false)
                {
                    // ��鵱ǰ�������Ƿ��Ͻ������߿�
                    // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                    if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
            sessioninfo.LibraryCodeList) == false)
                    {
                        strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �����Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                        goto ERROR1;
                    }
                }

                /*
                XmlDocument readerdom = null;
                nRet = LibraryApplication.LoadToDom(strReaderXml,
                    out readerdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                    goto ERROR1;
                }
                 * */

                // ��������
                if (this.Statis != null)
                    this.Statis.IncreaseEntryValue(
                    strLibraryCode,
                    "����˴�",
                    "������֮����",
                    1);


                // �����ض��ŵ��ۼ���
                if (this.Statis != null)
                    nResultValue = this.Statis.IncreaseEntryValue(
                    strLibraryCode,
                    "����˴�", 
                    String.IsNullOrEmpty(strGateName) == true ? "(blank)" : strGateName,
                    (int)1);

                XmlDocument domOperLog = new XmlDocument();
                domOperLog.LoadXml("<root />");
                DomUtil.SetElementText(domOperLog.DocumentElement,
    "libraryCode",
    strLibraryCode);    // �������ڵĹݴ���
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "operation",
                    "passgate");
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "readerBarcode",
                    strReaderBarcode);
                DomUtil.SetElementText(domOperLog.DocumentElement,
    "libraryCode",
    strLibraryCode);
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "gateName",
                    strGateName);

                DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                    sessioninfo.UserID);

                string strOperTime = this.Clock.GetClock();

                DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                    strOperTime);

                if (this.PassgateWriteToOperLog == true)
                {
                    nRet = this.OperLog.WriteOperLog(domOperLog,
                        sessioninfo.ClientAddress,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "PassGate() API д����־ʱ��������: " + strError;
                        goto ERROR1;
                    }
                }


            } // ���߼�¼������Χ����
            finally
            {
                this.ReaderLocks.UnlockForRead(strReaderBarcode);
            }

            if (String.IsNullOrEmpty(strResultTypeList) == true)
            {
                results = null; // �������κν��
                goto END1;
            }

            string[] result_types = strResultTypeList.Split(new char[] { ',' });
            results = new string[result_types.Length];

            for (int i = 0; i < result_types.Length; i++)
            {
                string strResultType = result_types[i];

                if (String.Compare(strResultType, "xml", true) == 0)
                {
                    results[i] = strReaderXml;
                }
                // else if (String.Compare(strResultType, "html", true) == 0)
                else if (IsResultType(strResultType, "html") == true)
                {
                    string strReaderRecord = "";
                    // �����߼�¼���ݴ�XML��ʽת��ΪHTML��ʽ
                    nRet = this.ConvertReaderXmlToHtml(
                        sessioninfo,
                        this.CfgDir + "\\readerxml2html.cs",
                        this.CfgDir + "\\readerxml2html.cs.ref",
                        strLibraryCode,
                        strReaderXml,
                        strOutputReaderRecPath, // 2009/10/18 new add
                        OperType.None,
                        null,
                        "",
                        strResultType,
                        out strReaderRecord,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ConvertReaderXmlToHtml()����(�ű�����Ϊ" + this.CfgDir + "\\readerxml2html.cs" + "): " + strError;
                        goto ERROR1;
                    }
                    results[i] = strReaderRecord;
                }
                // else if (String.Compare(strResultType, "text", true) == 0)
                else if (IsResultType(strResultType, "text") == true)
                {
                    string strReaderRecord = "";
                    // �����߼�¼���ݴ�XML��ʽת��Ϊtext��ʽ
                    nRet = this.ConvertReaderXmlToHtml(
                        sessioninfo,
                        this.CfgDir + "\\readerxml2text.cs",
                        this.CfgDir + "\\readerxml2text.cs.ref",
                        strLibraryCode,
                        strReaderXml,
                        strOutputReaderRecPath, // 2009/10/18 new add
                        OperType.None,
                        null,
                        "",
                        strResultType,
                        out strReaderRecord,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ConvertReaderXmlToHtml()����(�ű�����Ϊ" + this.CfgDir + "\\readerxml2html.cs" + "): " + strError;
                        goto ERROR1;
                    }
                    results[i] = strReaderRecord;
                }
                else
                {
                    strError = "δ֪�Ľ������ '" + strResultType + "'";
                    goto ERROR1;
                }
            }

        END1:
            result.Value = nResultValue;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }

        // ����Ѻ�𽻷�����
        // parameters:
        //      strOutputReaderXml �����޸ĺ�Ķ��߼�¼
        //      strOutputID ���ر��δ����Ľ�������� ID
        // result.Value -1 ���� ���� ���δ����Ľ�����������
        public LibraryServerResult Foregift(
            SessionInfo sessioninfo,
            string strAction,
            string strReaderBarcode,
            out string strOutputReaderXml,
            out string strOutputID)
        {
            strOutputReaderXml = "";
            strOutputID = "";

            string strError = "";
            int nRet = 0;

            LibraryServerResult result = new LibraryServerResult();

            string strReaderXml = "";

            strAction = strAction.ToLower();

            if (strAction == "foregift")
            {
                // Ȩ���ж�
                if (StringUtil.IsInList("foregift", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "����Ѻ�𽻷�����Ĳ������ܾ������߱�foregiftȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }
            else if (strAction == "return")
            {
                // Ȩ���ж�
                if (StringUtil.IsInList("returnforegift", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "�����˻�Ѻ��(����)����Ĳ������ܾ������߱�returnforegiftȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }
            else 
            {
                strError = "δ֪��strActionֵ '" + strAction + "'";
                goto ERROR1;
            }


            int nRedoCount = 0; // ��Ϊʱ�����ͻ, ���ԵĴ���
        REDO_FOREGIFT:


            // �Ӷ��߼�¼��
            this.ReaderLocks.LockForRead(strReaderBarcode);

            try // ���߼�¼������Χ��ʼ
            {

                // ������߼�¼
                string strOutputReaderRecPath = "";
                byte[] reader_timestamp = null;
                nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
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

                string strLibraryCode = "";
                // �������߼�¼�������Ķ��߿�Ĺݴ��룬�Ƿ񱻵�ǰ�û���Ͻ
                if (String.IsNullOrEmpty(strOutputReaderRecPath) == false)
                {
                    // ��鵱ǰ�������Ƿ��Ͻ������߿�
                    // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                    if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
            sessioninfo.LibraryCodeList,
            out strLibraryCode) == false)
                    {
                        strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �����Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                        goto ERROR1;
                    }
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

                // ��鵱ǰ�ǲ����Ѿ�����Ѻ�𽻷�����
                XmlNodeList nodeOverdues = readerdom.DocumentElement.SelectNodes("overdues/overdue");
                for (int i = 0; i < nodeOverdues.Count; i++)
                {
                    XmlNode node = nodeOverdues[i];

                    string strWord = "Ѻ��";

                    string strReason = DomUtil.GetAttr(node, "reason");
                    if (strReason.Length < strWord.Length)
                        continue;
                    string strPart = strReason.Substring(0, strWord.Length);
                    if (strPart == strWord)
                    {
                        strError = "���� '" + strReaderBarcode + "' �Ѿ�����Ѻ�𽻷�������Ҫ�Ƚ���Ѻ�����󽻷���ɺ󣬲��ܴ����µ�Ѻ�𽻷�����";
                        goto ERROR1;
                    }
                }

                string strOperTime = this.Clock.GetClock();

                string strOverdueString = "";
                // ����Foregift() APIҪ���޸�readerdom
                nRet = DoForegift(strAction,
                    readerdom,
                    ref strOutputID,   // Ϊ�ձ�ʾ�������Զ�����id
                    sessioninfo.UserID,
                    strOperTime,
                    out strOverdueString,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;


                // ***
                RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    goto ERROR1;
                }

                byte[] output_timestamp = null;
                string strOutputPath = "";

                strOutputReaderXml = readerdom.OuterXml;

                // д�ض��߼�¼
                long lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                    strOutputReaderXml,
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
                        goto REDO_FOREGIFT;
                    }
                    goto ERROR1;
                }

                // ��������
                if (this.Statis != null)
                    this.Statis.IncreaseEntryValue(
                    strLibraryCode,
                    "Ѻ��", 
                    "�������������",
                    1);
                // TODO: ���Ӽ۸���?

                XmlDocument domOperLog = new XmlDocument();
                domOperLog.LoadXml("<root />");
                DomUtil.SetElementText(domOperLog.DocumentElement,
    "libraryCode",
    strLibraryCode);    // �������ڵĹݴ���
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "operation",
                    "foregift");
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "action",
                    strAction);
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "readerBarcode",
                    strReaderBarcode);

                // ������ϸ���ַ��� һ�����߶��<overdue> OuterXml����
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "overdues", strOverdueString/*nodeOverdue.OuterXml*/);

                // �µĶ��߼�¼
                XmlNode nodeReaderRecord = DomUtil.SetElementText(domOperLog.DocumentElement,
                    "readerRecord", readerdom.OuterXml);
                DomUtil.SetAttr(nodeReaderRecord, "recPath", strOutputReaderRecPath);

                DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                    sessioninfo.UserID);

                // string strOperTime = this.Clock.GetClock();

                DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                    strOperTime);

                nRet = this.OperLog.WriteOperLog(domOperLog,
                    sessioninfo.ClientAddress,
                    out strError);
                if (nRet == -1)
                {
                    strError = "PassGate() API д����־ʱ��������: " + strError;
                    goto ERROR1;
                }


            } // ���߼�¼������Χ����
            finally
            {
                this.ReaderLocks.UnlockForRead(strReaderBarcode);
            }

            // END1:
            result.Value = 1;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }

        // ����Foregift() APIҪ���޸�readerdom
        // parameters:
        //      strAction   Ϊforegift��return֮һ
        //      strID   ΥԼ���¼ID������˲���Ϊnull����ʾ�������Զ�����һ��id��������ò���ֵ
        int DoForegift(
            string strAction,
            XmlDocument readerdom,
            ref string strID,
            string strOperator,
            string strOperTime,
            out string strOverdueString,
            out string strError)
        {
            strOverdueString = "";
            strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(strID) == true)
                strID = GetOverdueID();

            // �����ز���
            XmlNode nodeForegift = readerdom.DocumentElement.SelectSingleNode("foregift");
            if (nodeForegift == null)
            {
                nodeForegift = readerdom.CreateElement("foregift");
                readerdom.DocumentElement.AppendChild(nodeForegift);
            }

            string strExistPrice = nodeForegift.InnerText;

            string strCurrentDate = strOperTime; 
            DateTime current_date = DateTimeUtil.FromRfc1123DateTimeString(strCurrentDate);

            if (strAction == "foregift")
            {

            }
            else if (strAction == "return")
            {
                // Ҫ���е�overdues/overdueԪ����ʧ��borrows/borrowԪ����ʧ�����ܽ���return��������������Ϊ�˱����˻�Ѻ������г��ڻ���������Ҫ�۳�Ѻ��
                XmlNodeList overdue_nodes = readerdom.DocumentElement.SelectNodes("overdues/overdue");
                XmlNodeList borrow_nodes = readerdom.DocumentElement.SelectNodes("borrows/borrow");

                string strMessage = "";
                if (overdue_nodes.Count > 0)
                {
                    strMessage += " " + overdue_nodes.Count.ToString() + " ��δ��������";
                }

                if (borrow_nodes.Count > 0)
                {
                    if (String.IsNullOrEmpty(strMessage) == false)
                        strMessage += "��";

                    strMessage += " " + borrow_nodes.Count.ToString() + " ���ѽ�δ��������";
                }

                if (overdue_nodes.Count + borrow_nodes.Count > 0)
                {
                    strError = "�����ߵ�ǰ��" + strMessage + "����˲��ܴ����˻�Ѻ����������ȹ黹ȫ��ͼ��ͽ�������Ƿ�ѡ�";
                    return -1;
                }
            }
            else 
            {
                strError = "δ֪��strActionֵ '" + strAction + "'";
                goto ERROR1;
            }

            int nResultValue = 0;
            string strForegiftPrice = "";
            // ִ�нű�����GetForegift
            // �������м۸񣬼������Ҫ�½��ļ۸�
            // parameters:
            // return:
            //      -2  not found script
            //      -1  ����
            //      0   �ɹ�
            nRet = this.DoGetForegiftScriptFunction(
                strAction,
                readerdom,
                strExistPrice,
                out nResultValue,
                out strForegiftPrice,
                out strError);
            if (nRet == -1 || nRet == -2)
                goto ERROR1;

            if (nResultValue == -1)
            {
                // strError?
                goto ERROR1;
            }

            // *** �޸Ķ��߼�¼

            // action "foregift" �� "return" ���������޸ĵ�ǰ���߼�¼<foregit>����ļ�Ǯ������Ҫ�ȵ����Ѷ�����ʱ��Ŷ���

            // �����������Ƿ���overduesԪ��
            XmlNode root = readerdom.DocumentElement.SelectSingleNode("overdues");
            if (root == null)
            {
                root = readerdom.CreateElement("overdues");
                readerdom.DocumentElement.AppendChild(root);
            }

            // ���һ��overdueԪ��
            XmlNode nodeOverdue = readerdom.CreateElement("overdue");
            root.AppendChild(nodeOverdue);

            DomUtil.SetAttr(nodeOverdue, "reason", "Ѻ��");
            DomUtil.SetAttr(nodeOverdue, "price", strForegiftPrice);    // ע��strForegiftPrice��ֵ����Ϊ"%return_foregift_price%"����ʾ��ǰʣ���Ѻ���ĸ���
            DomUtil.SetAttr(nodeOverdue, "borrowDate", strCurrentDate);   // borrowDate�з���ʼ���ڲ���
            DomUtil.SetAttr(nodeOverdue, "borrowPeriod", "");
            DomUtil.SetAttr(nodeOverdue, "returnDate", "");
            DomUtil.SetAttr(nodeOverdue, "borrowOperator", strOperator);  // ���������������

            // id������Ψһ��, Ϊ��ΥԼ��C/S���洴������������
            DomUtil.SetAttr(nodeOverdue, "id", strID);

            if (strAction == "return")
                DomUtil.SetAttr(nodeOverdue, "comment", "�˻�Ѻ��");


            strOverdueString = nodeOverdue.OuterXml;
            return 0;
        ERROR1:
            return -1;
        }

        // ������𽻷�����
        // parameters:
        //      strOutputReaderXml �����޸ĺ�Ķ��߼�¼
        //      strOutputID ���ر��δ����Ľ�������� ID
        // result.Value -1 ���� ���� ���δ����Ľ�����������
        public LibraryServerResult Hire(
            SessionInfo sessioninfo,
            string strAction,
            string strReaderBarcode,
            out string strOutputReaderXml,
            out string strOutputID)
        {
            strOutputReaderXml = "";
            strOutputID = "";

            string strError = "";
            int nRet = 0;

            LibraryServerResult result = new LibraryServerResult();

            string strReaderXml = "";

            strAction = strAction.ToLower();

            if (strAction == "hire")
            {
                // Ȩ���ж�
                if (StringUtil.IsInList("hire", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "������𽻷�����Ĳ������ܾ������߱�hireȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }
            else if (strAction == "hirelate")
            {
                // Ȩ���ж�
                if (StringUtil.IsInList("hirelate", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "(�ӳ�)������𽻷�����Ĳ������ܾ������߱�hirelateȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }


            int nRedoCount = 0; // ��Ϊʱ�����ͻ, ���ԵĴ���
        REDO_HIRE:


            // �Ӷ��߼�¼��
            this.ReaderLocks.LockForRead(strReaderBarcode);

            try // ���߼�¼������Χ��ʼ
            {

                // ������߼�¼
                string strOutputReaderRecPath = "";
                byte[] reader_timestamp = null;
                nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
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

                string strLibraryCode = "";
                // �������߼�¼�������Ķ��߿�Ĺݴ��룬�Ƿ񱻵�ǰ�û���Ͻ
                if (String.IsNullOrEmpty(strOutputReaderRecPath) == false)
                {
                    // ��鵱ǰ�������Ƿ��Ͻ������߿�
                    // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                    if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
            sessioninfo.LibraryCodeList,
            out strLibraryCode) == false)
                    {
                        strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �����Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                        goto ERROR1;
                    }
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

                // ��鵱ǰ�ǲ����Ѿ�������𽻷�����
                XmlNodeList nodeOverdues = readerdom.DocumentElement.SelectNodes("overdues/overdue");
                for (int i = 0; i < nodeOverdues.Count; i++)
                {
                    XmlNode node = nodeOverdues[i];

                    string strWord = "���";

                    string strReason = DomUtil.GetAttr(node, "reason");
                    if (strReason.Length < strWord.Length)
                        continue;
                    string strPart = strReason.Substring(0, strWord.Length);
                    if (strPart == strWord)
                    {
                        strError = "���� '" + strReaderBarcode + "' �Ѿ�������𽻷�������Ҫ�Ƚ���������󽻷���ɺ󣬲��ܴ����µ���𽻷�����";
                        goto ERROR1;
                    }
                }

                string strOperTime = this.Clock.GetClock();

                string strOverdueString = "";
                // ����Hire() APIҪ���޸�readerdom
                nRet = DoHire(strAction,
                    readerdom,
                    ref strOutputID,   // Ϊ�ձ�ʾ�������Զ�����id
                    sessioninfo.UserID,
                    strOperTime,
                    out strOverdueString,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;


                // ***
                RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    goto ERROR1;
                }

                byte[] output_timestamp = null;
                string strOutputPath = "";

                strOutputReaderXml = readerdom.OuterXml;

                // д�ض��߼�¼
                long lRet = channel.DoSaveTextRes(strOutputReaderRecPath,
                    strOutputReaderXml,
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
                        goto REDO_HIRE;
                    }
                    goto ERROR1;
                }

                // ��������
                if (this.Statis != null)
                    this.Statis.IncreaseEntryValue(
                    strLibraryCode,
                    "���",
                    "�������������",
                    1);
                // TODO: ���Ӽ۸���?

                XmlDocument domOperLog = new XmlDocument();
                domOperLog.LoadXml("<root />");
                DomUtil.SetElementText(domOperLog.DocumentElement,
    "libraryCode",
    strLibraryCode);    // �������ڵĹݴ���
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "operation",
                    "hire");
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "action",
                    strAction);
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "readerBarcode",
                    strReaderBarcode);

                // ������ϸ���ַ��� һ�����߶��<overdue> OuterXml����
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "overdues", strOverdueString/*nodeOverdue.OuterXml*/);

                // �µĶ��߼�¼
                XmlNode nodeReaderRecord = DomUtil.SetElementText(domOperLog.DocumentElement,
                    "readerRecord", readerdom.OuterXml);
                DomUtil.SetAttr(nodeReaderRecord, "recPath", strOutputReaderRecPath);

                DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                    sessioninfo.UserID);

                // string strOperTime = this.Clock.GetClock();

                DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                    strOperTime);

                nRet = this.OperLog.WriteOperLog(domOperLog,
                    sessioninfo.ClientAddress,
                    out strError);
                if (nRet == -1)
                {
                    strError = "PassGate() API д����־ʱ��������: " + strError;
                    goto ERROR1;
                }


            } // ���߼�¼������Χ����
            finally
            {
                this.ReaderLocks.UnlockForRead(strReaderBarcode);
            }

        // END1:
            result.Value = 1;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }

        // ����Hire() APIҪ���޸�readerdom
        // parameters:
        //      strID   ΥԼ���¼ID������˲���Ϊnull����ʾ�������Զ�����һ��id��������ò���ֵ
        int DoHire(
            string strAction,
            XmlDocument readerdom,
            ref string strID,
            string strOperator,
            string strOperTime,
            out string strOverdueString,
            out string strError)
        {
            strOverdueString = "";
            strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(strID) == true)
                strID = GetOverdueID();

            // �����ز���
            XmlNode nodeHire = readerdom.DocumentElement.SelectSingleNode("hire");
            if (nodeHire == null)
            {
                // 2013/6/16
                nodeHire = readerdom.CreateElement("hire");
                readerdom.DocumentElement.AppendChild(nodeHire);
                //strError = "���߼�¼��û������������ (<hire>Ԫ��)������޷�������𽻷�����";
                //goto ERROR1;
            }

            string strHirePeriod = DomUtil.GetAttr(nodeHire, "period");
            string strStartDate = "";

            string strCurrentDate = strOperTime;   //this.Clock.GetClock();
            DateTime current_date = DateTimeUtil.FromRfc1123DateTimeString(strCurrentDate);

            if (strAction == "hire")
            {
                strStartDate = DomUtil.GetAttr(nodeHire, "expireDate");

                // �����¼�е�ĩ�����ʧЧ��Ϊ�գ�����ȡ��֤���ں͵�ǰʱ��Ŀ�����
                if (String.IsNullOrEmpty(strStartDate) == true)
                {
                    string strCreateDate = DomUtil.GetElementText(readerdom.DocumentElement,
                        "createDate");

                    // �������û�а�֤ʱ��
                    if (String.IsNullOrEmpty(strCreateDate) == true)
                    {
                        strStartDate = strCurrentDate;
                    }
                    else
                    {
                        // ����а�֤ʱ��
                        DateTime createdate = new DateTime(0);
                        try
                        {
                            createdate = DateTimeUtil.FromRfc1123DateTimeString(strCreateDate);
                        }
                        catch
                        {
                            strError = "��֤���� <createDate> '" + strCreateDate + "' ��ʽ����";
                            goto ERROR1;
                        }

                        if (createdate > current_date)
                            strStartDate = strCreateDate;   // ���ð�֤ʱ��
                        else
                            strStartDate = strCurrentDate;  // ���õ�ǰʱ��
                    }
                }
            }
            else if (strAction == "hirelate")   // hire��hirelate��ʲô����?
            {
                strStartDate = strCurrentDate;

                // �Ѿ����ڵ�ĩ�����ʧЧ�ڣ��ο�
                string strExistStartDate = DomUtil.GetAttr(nodeHire, "expireDate");

                if (String.IsNullOrEmpty(strExistStartDate) == true)
                    goto SKIP_HIRE_LATE;

                DateTime exist_expiredate = new DateTime(0);
                try
                {
                    exist_expiredate = DateTimeUtil.FromRfc1123DateTimeString(strExistStartDate);
                }
                catch
                {
                    goto SKIP_HIRE_LATE;
                }

                DateTime temp_startdate = DateTimeUtil.FromRfc1123DateTimeString(strStartDate);

                // �����ǰ���ڱ��Ѿ����ڵ�ĩ�����ʧЧ�ڻ���ǰ����ȡ�����һ����������߳Կ�
                if (exist_expiredate > temp_startdate)
                    strStartDate = strExistStartDate;
            }

        SKIP_HIRE_LATE:

            int nResultValue = 0;
            string strHireExpireDate = "";
            string strHirePrice = "";
            // ִ�нű�����GetHire
            // ���ݵ�ǰʱ�䡢���ڣ������ʧЧ�ںͼ۸�
            // parameters:
            // return:
            //      -2  not found script
            //      -1  ����
            //      0   �ɹ�
            nRet = this.DoGetHireScriptFunction(
                readerdom,
                strStartDate,
                strHirePeriod,
                out nResultValue,
                out strHireExpireDate,
                out strHirePrice,
                out strError);
            if (nRet == -1 || nRet == -2)
                goto ERROR1;

            if (nResultValue == -1)
            {
                // strError?
                goto ERROR1;
            }

            // *** �޸Ķ��߼�¼

            // �޸����ʧЧ��
            DomUtil.SetAttr(nodeHire, "expireDate", strHireExpireDate);

            // �ƶ�֤ʧЧ��
            string strReaderExpireDate = DomUtil.GetElementText(readerdom.DocumentElement,
                "expireDate");
            if (String.IsNullOrEmpty(strReaderExpireDate) == true)
                strReaderExpireDate = strHireExpireDate;

            // 
            DateTime reader_expiredate = new DateTime(0);
            try
            {
                reader_expiredate = DateTimeUtil.FromRfc1123DateTimeString(strReaderExpireDate);
            }
            catch
            {
                strError = "֤ʧЧ�� '" + strReaderExpireDate + "' ���Ϸ�";
                goto ERROR1;
            }

            // 
            DateTime hire_expiredate = new DateTime(0);
            try
            {
                hire_expiredate = DateTimeUtil.FromRfc1123DateTimeString(strHireExpireDate);
            }
            catch
            {
                strError = "���ʧЧ�� '" + strHireExpireDate + "' ���Ϸ�";
                goto ERROR1;
            }

            // ������ʧЧ�ڴ���֤ʧЧ�ڣ����߶��߼�¼������ʧЧ��Ϊ��
            if (hire_expiredate > reader_expiredate
                || String.IsNullOrEmpty(DomUtil.GetElementText(readerdom.DocumentElement,"expireDate")) == true
                )
            {
                DomUtil.SetElementText(readerdom.DocumentElement,
                    "expireDate", 
                    DateTimeUtil.Rfc1123DateTimeStringEx(hire_expiredate.ToLocalTime()));
            }

            // �����������Ƿ���overduesԪ��
            XmlNode root = readerdom.DocumentElement.SelectSingleNode("overdues");
            if (root == null)
            {
                root = readerdom.CreateElement("overdues");
                readerdom.DocumentElement.AppendChild(root);
            }

            // ���һ��overdueԪ��
            XmlNode nodeOverdue = readerdom.CreateElement("overdue");
            root.AppendChild(nodeOverdue);

            // DomUtil.SetAttr(nodeOverdue, "barcode", "");    // �������Ϊ��
            // DomUtil.SetAttr(nodeOverdue, "recPath", strItemRecPath);


            DomUtil.SetAttr(nodeOverdue, "reason", "����� " + strStartDate + " ���� " + strHirePeriod + " �����ʧЧ��Ϊ " + strHireExpireDate);
            DomUtil.SetAttr(nodeOverdue, "price", strHirePrice);
            DomUtil.SetAttr(nodeOverdue, "borrowDate", strStartDate);   // borrowDate�з���ʼ���ڲ���
            DomUtil.SetAttr(nodeOverdue, "borrowPeriod", strHirePeriod);    // borrowperiod�з�������ڲ���
            DomUtil.SetAttr(nodeOverdue, "returnDate", strHireExpireDate);  // returnDate�з�ʧЧ�ڲ���
            DomUtil.SetAttr(nodeOverdue, "borrowOperator", strOperator);  // ���������������
            // DomUtil.SetAttr(nodeOverdue, "operator", strOperator);
            // id������Ψһ��, Ϊ��ΥԼ��C/S���洴������������
            DomUtil.SetAttr(nodeOverdue, "id", strID);

            strOverdueString = nodeOverdue.OuterXml;
            return 0;
        ERROR1:
            return -1;
        }

        // ����
        public LibraryServerResult Settlement(
            SessionInfo sessioninfo,
            string strAction,
            string [] ids)
        {
            string strError = "";
            int nRet = 0;

            LibraryServerResult result = new LibraryServerResult();

            strAction = strAction.ToLower();

            if (strAction == "settlement")
            {
                // Ȩ���ж�
                if (StringUtil.IsInList("settlement", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "����������ܾ������߱�settlementȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }
            else if (strAction == "undosettlement")
            {
                // Ȩ���ж�
                if (StringUtil.IsInList("undosettlement", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "��������Ĳ������ܾ������߱�undosettlementȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }
            else if (strAction == "delete")
            {
                // Ȩ���ж�
                if (StringUtil.IsInList("deletesettlement", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "ɾ�������¼�Ĳ������ܾ������߱�deletesettlementȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }
            else
            {
                strError = "�޷�ʶ���strAction����ֵ '" + strAction + "'";
                goto ERROR1;
            }

            string strOperTime = this.Clock.GetClock();
            string strOperator = sessioninfo.UserID;

            //
            string strText = "";
            string strCount = "";

            strCount = "<maxCount>100</maxCount>";

            for (int i = 0; i < ids.Length; i++)
            {
                string strID = ids[i];

                if (i != 0)
                {
                    strText += "<operator value='OR' />";
                }

                strText += "<item><word>"
                    + StringUtil.GetXmlStringSimple(strID)
                    + "</word>"
                    + strCount
                    + "<match>exact</match><relation>=</relation><dataType>string</dataType>"
                    + "</item>";
            }

            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(this.AmerceDbName + ":" + "ID")       // 2007/9/14 new add
                + "'>" + strText
                + "<lang>zh</lang></target>";

            string strIds = String.Join(",", ids);

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            long lRet = channel.DoSearch(strQueryXml,
                "amerced",
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
            {
                strError = "����IDΪ '" + strIds + "' ��ΥԼ���¼����: " + strError;
                goto ERROR1;
            }

            if (lRet == 0)
            {
                strError = "û���ҵ�idΪ '" + strIds + "' ��ΥԼ���¼";
                goto ERROR1;
            }

            long lHitCount = lRet;

            long lStart = 0;
            long lPerCount = Math.Min(50, lHitCount);
            List<string> aPath = null;

            // ��ý�������������¼���д���
            for (; ; )
            {
                lRet = channel.DoGetSearchResult(
                    "amerced",   // strResultSetName
                    lStart,
                    lPerCount,
                    "zh",
                    null,   // stop
                    out aPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (lRet == 0)
                {
                    strError = "δ����";
                    break;  // ??
                }

                // ����������
                for (int i = 0; i < aPath.Count; i++)
                {
                    string strPath = aPath[i];

                    string strCurrentError = "";

                    // ����һ�����Ѽ�¼
                    nRet = SettlementOneRecord(
                        sessioninfo.LibraryCodeList,
                        true,   // Ҫ������־
                        channel,
                        strAction,
                        strPath,
                        strOperTime,
                        strOperator,
                        sessioninfo.ClientAddress,
                        out strCurrentError);
                    // ����һ�����Ӧ����������
                    if (nRet == -1)
                    {
                        strError += strAction + "ΥԼ���¼ '" +strPath+ "' ʱ��������: " + strCurrentError + "\r\n";
                    }
                    // ����������־�ռ��������Ĵ���Ͳ��ܼ���������
                    if (nRet == -2)
                    {
                        strError = strCurrentError;
                        goto ERROR1;
                    }
                }

                lStart += aPath.Count;
                if (lStart >= lHitCount || lPerCount <= 0)
                    break;
            }

            if (strError != "")
                goto ERROR1;

            result.Value = 1;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;

        }

        // ����һ�����Ѽ�¼
        // parameters:
        //      strLibraryCodeList  ��ǰ�����߹�Ͻ��ͼ��ݴ���
        //      bCreateOperLog  �Ƿ񴴽���־
        //      strOperTime ����Ĳ���ʱ��
        //      strOperator ����Ĳ�����
        // return:
        //      -2  �������������ټ���ѭ�����ñ�����
        //      -1  һ��������Լ���ѭ�����ñ�����
        //      0   ����
        int SettlementOneRecord(
            string strLibraryCodeList,
            bool bCreateOperLog,
            RmsChannel channel,
            string strAction,
            string strAmercedRecPath,
            string strOperTime,
            string strOperator,
            string strClientAddress,
            out string strError)
        {
            strError = "";

            string strMetaData = "";
            byte[] amerced_timestamp = null;
            string strOutputPath = "";
            string strAmercedXml = "";

            // ׼����־DOM
            XmlDocument domOperLog = null;

            if (bCreateOperLog == true)
            {

            }

            int nRedoCount = 0;
        REDO:

            long lRet = channel.GetRes(strAmercedRecPath,
                out strAmercedXml,
                out strMetaData,
                out amerced_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                strError = "��ȡΥԼ���¼ '" + strAmercedRecPath + "' ʱ����: " + strError;
                return -1;
            }

            XmlDocument amerced_dom = null;
            int nRet = LibraryApplication.LoadToDom(strAmercedXml,
                out amerced_dom,
                out strError);
            if (nRet == -1)
            {
                strError = "װ��ΥԼ���¼����XML DOMʱ��������: " + strError;
                return -1;
            }

            string strLibraryCode = DomUtil.GetElementText(amerced_dom.DocumentElement, "libraryCode");
            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
            {
                if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                {
                    strError = "��ǰ�û�δ�ܹ�ϽΥԼ���¼ '"+strAmercedRecPath+"' ���ڵĹݴ��� '"+strLibraryCode+"'";
                    return -1;
                }
            }

            if (bCreateOperLog == true)
            {
                domOperLog = new XmlDocument();
                domOperLog.LoadXml("<root />");

                // 2012/10/2
                // ��ض������ڵĹݴ���
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "libraryCode", strLibraryCode);

                DomUtil.SetElementText(domOperLog.DocumentElement, "operation",
                    "settlement");
                DomUtil.SetElementText(domOperLog.DocumentElement, "action",
                    strAction);


                // ����־�м��� id
                string strID = DomUtil.GetElementText(amerced_dom.DocumentElement,
                    "id");
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "id", strID);
            }

            string strOldState = DomUtil.GetElementText(amerced_dom.DocumentElement,
                "state");

            if (strAction == "settlement")
            {
                if (strOldState != "amerced")
                {
                    strError = "�������ǰ����¼״̬����Ϊamerced��(������Ϊ'" + strOldState + "')";
                    return -1;
                }
                if (strOldState == "settlemented")
                {
                    strError = "�������ǰ����¼״̬�Ѿ�Ϊsettlemented";
                    return -1;
                }
            }
            else if (strAction == "undosettlement")
            {
                if (strOldState != "settlemented")
                {
                    strError = "�����������ǰ����¼״̬����Ϊsettlemented��(������Ϊ'" + strOldState + "')";
                    return -1;
                }
                if (strOldState == "amerced")
                {
                    strError = "�����������ǰ����¼״̬�Ѿ�Ϊsettlemented";
                    return -1;
                }
            }
            else if (strAction == "delete")
            {
                if (strOldState != "settlemented")
                {
                    strError = "ɾ���������ǰ����¼״̬����Ϊsettlemented��(������Ϊ'" + strOldState + "')";
                    return -1;
                }
            }
            else
            {
                strError = "�޷�ʶ���strAction����ֵ '" + strAction + "'";
                return -1;
            }

            byte[] output_timestamp = null;

            if (bCreateOperLog == true)
            {
                // oldAmerceRecord
                XmlNode nodeOldAmerceRecord = DomUtil.SetElementText(domOperLog.DocumentElement,
    "oldAmerceRecord", strAmercedXml);
                DomUtil.SetAttr(nodeOldAmerceRecord, "recPath", strAmercedRecPath);
            }

            if (strAction == "delete")
            {
                // ɾ���ѽ���ΥԼ���¼
                lRet = channel.DoDeleteRes(strAmercedRecPath,
                    amerced_timestamp,
                    out output_timestamp,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                        && nRedoCount < 10)
                    {
                        nRedoCount++;
                        amerced_timestamp = output_timestamp;
                        goto REDO;
                    }
                    strError = "ɾ���ѽ���ΥԼ���¼ '" + strAmercedRecPath + "' ʧ��: " + strError;
                    this.WriteErrorLog(strError);
                    return -1;
                }

                goto END1;  // д��־
            }

            // �޸�״̬
            if (strAction == "settlement")
            {
                DomUtil.SetElementText(amerced_dom.DocumentElement,
                    "state", "settlemented");


                // ���������Ϣ
                DomUtil.DeleteElement(amerced_dom.DocumentElement,
                    "undoSettlementOperTime");
                DomUtil.DeleteElement(amerced_dom.DocumentElement,
                    "undoSettlementOperator");


                DomUtil.SetElementText(amerced_dom.DocumentElement,
                    "settlementOperTime", strOperTime);
                DomUtil.SetElementText(amerced_dom.DocumentElement,
                    "settlementOperator", strOperator);
            }
            else
            {
                Debug.Assert(strAction == "undosettlement", "");

                DomUtil.SetElementText(amerced_dom.DocumentElement,
                    "state", "amerced");


                // ���������Ϣ
                DomUtil.SetElementText(amerced_dom.DocumentElement,
                    "settlementOperTime", "");
                DomUtil.SetElementText(amerced_dom.DocumentElement,
                    "settlementOperator", "");


                DomUtil.SetElementText(amerced_dom.DocumentElement,
                    "undoSettlementOperTime", strOperTime);
                DomUtil.SetElementText(amerced_dom.DocumentElement,
                    "undoSettlementOperator", strOperator);

            }

            if (bCreateOperLog == true)
            {
                DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                    strOperator);   // ������
                DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                    strOperTime);   // ����ʱ��
            }


            // ��������ݿ�
            lRet = channel.DoSaveTextRes(strAmercedRecPath,
                amerced_dom.OuterXml,
                false,
                "content", // ?????,ignorechecktimestamp
                amerced_timestamp,
                out output_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                strError = "д��ΥԼ���¼ '" + strAmercedRecPath + "' ʱ����: " + strError;
                return -1;
            }

            if (bCreateOperLog == true)
            {
                // amerceRecord
                XmlNode nodeAmerceRecord = DomUtil.SetElementText(domOperLog.DocumentElement,
                    "amerceRecord", amerced_dom.OuterXml);
                DomUtil.SetAttr(nodeAmerceRecord, "recPath", strAmercedRecPath);
            }


        END1:
            if (bCreateOperLog == true)
            {
                if (this.Statis != null)
                {
                    if (strAction == "settlement")
                        this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            "���ý���", "�����¼��", 1);
                    else if (strAction == "undosettlement")
                        this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            "���ý���", "���������¼��", 1);
                    else if (strAction == "delete")
                        this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            "���ý���", "ɾ�������¼��", 1);
                }


                nRet = this.OperLog.WriteOperLog(domOperLog,
                    strClientAddress,
                    out strError);
                if (nRet == -1)
                {
                    strError = "settlement() API д����־ʱ��������: " + strError;
                    return -2;
                }
            }

            return 0;
        }


        static Hashtable ParseMedaDataXml(string strXml,
    out string strError)
        {
            strError = "";
            Hashtable result = new Hashtable();

            if (strXml == "")
                return result;

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return null;
            }

            XmlAttributeCollection attrs = dom.DocumentElement.Attributes;
            for (int i = 0; i < attrs.Count; i++)
            {
                string strName = attrs[i].Name;
                string strValue = attrs[i].Value;

                result.Add(strName, strValue);
            }


            return result;
        }

        // ���ض�����Դ
        // return:
        //      -1  ����
        //      0   304����
        //      1   200����
        public int DownloadObject(System.Web.UI.Page Page,
            FlushOutput flushOutputMethod,
    RmsChannelCollection channels,
    string strPath,
    bool bSaveAs,
    out string strError)
        {
            strError = "";

            WebPageStop stop = new WebPageStop(Page);

            RmsChannel channel = channels.GetChannel(this.WsUrl);

            if (channel == null)
            {
                strError = "GetChannel() Error...";
                return -1;
            }

            // strPath = boards.GetCanonicalUri(strPath);

            // �����Դ��д���ļ��İ汾���ر������ڻ����Դ��Ҳ�����ڻ������¼�塣
            // parameters:
            //		fileTarget	�ļ���ע���ڵ��ú���ǰ�ʵ������ļ�ָ��λ�á�����ֻ���ڵ�ǰλ�ÿ�ʼ���д��д��ǰ���������ı��ļ�ָ�롣
            //		strStyleParam	һ������Ϊ"content,data,metadata,timestamp,outputpath";
            //		input_timestamp	��!=null���򱾺�����ѵ�һ�����ص�timestamp�ͱ��������ݱȽϣ��������ȣ��򱨴�
            // return:
            //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
            //		0	�ɹ�
            string strMetaData = "";
            string strOutputPath;
            byte[] baOutputTimeStamp = null;

            // ���ý������
            long lRet = channel.GetRes(
                strPath,
                null,	// Response.OutputStream,
                stop,
                "metadata",
                null,	// byte [] input_timestamp,
                out strMetaData,
                out baOutputTimeStamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                strError = "GetRes() (for metadata) Error : " + strError;
                return -1;
            }

            if (Page.Response.IsClientConnected == false)
                return -1;

            // ȡmetadata�е�mime������Ϣ
            Hashtable values = ParseMedaDataXml(strMetaData,
                out strError);

            if (values == null)
            {
                strError = "ParseMedaDataXml() Error :" + strError;
                return -1;
            }

            string strLastModifyTime = (string)values["lastmodifytime"];
            if (String.IsNullOrEmpty(strLastModifyTime) == false)
            {
                DateTime lastmodified = DateTime.Parse(strLastModifyTime);
                string strIfHeader = Page.Request.Headers["If-Modified-Since"];

                if (String.IsNullOrEmpty(strIfHeader) == false)
                {
                    DateTime isModifiedSince = DateTimeUtil.FromRfc1123DateTimeString(strIfHeader).ToLocalTime();

                    if (isModifiedSince != lastmodified)
                    {
                        // �޸Ĺ�
                    }
                    else
                    {
                        // û���޸Ĺ�
                        Page.Response.StatusCode = 304;
                        Page.Response.SuppressContent = true;
                        return 0;
                    }

                }

                Page.Response.AddHeader("Last-Modified", DateTimeUtil.Rfc1123DateTimeString(lastmodified.ToUniversalTime()));
/*
                Page.Response.Cache.SetLastModified(lastmodified);
                Page.Response.Cache.SetCacheability(HttpCacheability.Public);
 * */
            }

            string strMime = (string)values["mimetype"];
            string strClientPath = (string)values["localpath"];
            if (strClientPath != "")
                strClientPath = PathUtil.PureName(strClientPath);

            // TODO: ����Ƿ�image/????���ͣ���Ҫ����content-disposition
            // �Ƿ�������Ϊ�Ի���
            if (bSaveAs == true)
            {
                string strEncodedFileName = HttpUtility.UrlEncode(strClientPath, Encoding.UTF8);
                Page.Response.AddHeader("content-disposition", "attachment; filename=" + strEncodedFileName);
            }

            /*
            Page.Response.AddHeader("Accept-Ranges", "bytes");
            Page.Response.AddHeader("Last-Modified", "Wed, 21 Nov 2007 07:10:54 GMT");
             * */

            // �� text/plain IE XML ����google
            // http://support.microsoft.com/kb/329661
            // http://support.microsoft.com/kb/239750/EN-US/
            /*
To use this fix, you must add the following registry value to the key listed below: 
Key: HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings

Value name: IsTextPlainHonored
Value type: DWORD
Value data: HEX 0x1 
             * */

            /*

            Page.Response.CacheControl = "no-cache";    // ������ô˾䣬text/plain�ᱻ����xml�ļ���
            Page.Response.AddHeader("Pragma", "no-cache");
            Page.Response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate, post-check=0, pre-check=0");
//            Page.Response.AddHeader("Cache-Control", "public");
            Page.Response.AddHeader("Expires", "0");
            Page.Response.AddHeader("Content-Transfer-Encoding", "binary");
             * */


            // ����ý������
            if (strMime == "text/plain")
                strMime = "text";
            Page.Response.ContentType = strMime;

            string strSize = (string)values["size"];
            if (String.IsNullOrEmpty(strSize) == false)
            {
                Page.Response.AddHeader("Content-Length", strSize);
            }


            if (Page.Response.IsClientConnected == false)
                return -1;

            // ��������

            lRet = channel.GetRes(
                strPath,
                Page.Response.OutputStream,
                flushOutputMethod,
                stop,
                "content,data",
                null,	// byte [] input_timestamp,
                out strMetaData,
                out baOutputTimeStamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                strError = "GetRes() (for res) Error : " + strError;
                return -1;
            }


            return 1;
        }


    }

    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class AmerceItem
    {
        [DataMember]
        public string ID = "";  // ʶ��id
        [DataMember]
        public string NewPrice = "";    // ����ļ۸�
        [DataMember]
        public string NewComment = ""; // ע��
    }

    public class WebPageStop : Stop
    {
        System.Web.UI.Page Page = null;

        public WebPageStop(System.Web.UI.Page page)
        {
            this.Page = page;
        }

        public override int State
        {
            get
            {
                if (this.Page == null)
                    return -1;

                if (this.Page.Response.IsClientConnected == false)
                    return 2;

                return 0;
            }
        }

    }


    // ����ɹ������Ϣ
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class BorrowInfo
    {
        // Ӧ������/ʱ��
        [DataMember]
        public string LatestReturnTime = "";    // RFC1123��ʽ��GMTʱ��

        // �������ޡ����硰20day��
        [DataMember]
        public string Period = "";

        // ��ǰΪ����ĵڼ��Σ�0��ʾ���ν���
        [DataMember]
        public long BorrowCount = 0;

        // ���������
        [DataMember]
        public string BorrowOperator = "";

        /*
        // 2008/5/9 new add
        // ����Ĳ��ͼ������
        public string BookType = "";

        // 2008/5/9 new add
        // ����Ĳ�Ĺݲصص�
        public string Location = "";
         * */
    }

    // ����ɹ������Ϣ
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class ReturnInfo
    {
        // ��������/ʱ��
        [DataMember]
        public string BorrowTime = "";    // RFC1123��ʽ��GMTʱ��

        // Ӧ������/ʱ��
        [DataMember]
        public string LatestReturnTime = "";    // RFC1123��ʽ��GMTʱ��

        // ԭ�������ޡ����硰20day��
        [DataMember]
        public string Period = "";

        // ��ǰΪ����ĵڼ��Σ�0��ʾ���ν���
        [DataMember]
        public long BorrowCount = 0;

        // ΥԼ�������ַ�����XML��ʽ
        [DataMember]
        public string OverdueString = "";

        // ���������
        [DataMember]
        public string BorrowOperator = "";

        // ���������
        [DataMember]
        public string ReturnOperator = "";

        // 2008/5/9 new add
        /// <summary>
        /// �����Ĳ��ͼ������
        /// </summary>
        [DataMember]
        public string BookType = "";

        // 2008/5/9 new add
        /// <summary>
        /// �����Ĳ�Ĺݲصص�
        /// </summary>
        [DataMember]
        public string Location = "";
    }

}
