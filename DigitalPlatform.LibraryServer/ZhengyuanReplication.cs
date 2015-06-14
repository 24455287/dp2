using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Globalization;

using DigitalPlatform.Xml;
using DigitalPlatform.IO;


namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// ��Ԫһ��ͨ������Ϣͬ�� ����������
    /// </summary>
    public class ZhengyuanReplication : BatchTask
    {
        internal AutoResetEvent eventDownloadFinished = new AutoResetEvent(false);	// true : initial state is signaled 
        bool DownloadCancelled = false;
        Exception DownloadException = null;

        // ���캯��
        public ZhengyuanReplication(LibraryApplication app, 
            string strName)
            : base(app, strName)
        {
            this.Loop = true;
        }

        public override string DefaultName
        {
            get
            {
                return "��Ԫһ��ͨ������Ϣͬ��";
            }
        }

        // ���� ��ʼ ����
        static int ParseZhengyuanReplicationStart(string strStart,
            out string strError)
        {
            strError = "";

            return 0;
        }

        // ����ͨ����������
        // ��ʽ
        /*
         * <root dump='...' clearFirst='...' loop='...'/>
         * dumpȱʡΪfalse
         * clearFirstȱʡΪfalse
         * loopȱʡΪtrue
         * 
         * */
        public static int ParseZhengyuanReplicationParam(string strParam,
            out bool bForceDumpAll,
            out bool bForceDumpDay,
            out bool bAutoDumpDay,
            out bool bClearFirst,
            out bool bLoop,
            out string strError)
        {
            strError = "";
            bClearFirst = false;
            bForceDumpAll = false;
            bForceDumpDay = false;
            bAutoDumpDay = false;
            bLoop = true;

            if (String.IsNullOrEmpty(strParam) == true)
                return 0;

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strParam);
            }
            catch (Exception ex)
            {
                strError = "strParam����װ��XML DOMʱ����: " + ex.Message;
                return -1;
            }


            string strClearFirst = DomUtil.GetAttr(dom.DocumentElement,
                "clearFirst");
            if (strClearFirst.ToLower() == "yes"
                || strClearFirst.ToLower() == "true")
                bClearFirst = true;
            else
                bClearFirst = false;

            string strForceDumpAll = DomUtil.GetAttr(dom.DocumentElement,
                "forceDumpAll");
            if (strForceDumpAll.ToLower() == "yes"
                || strForceDumpAll.ToLower() == "true")
                bForceDumpAll = true;
            else
                bForceDumpAll = false;

            string strForceDumpDay = DomUtil.GetAttr(dom.DocumentElement,
    "forceDumpDay");
            if (strForceDumpDay.ToLower() == "yes"
                || strForceDumpDay.ToLower() == "true")
                bForceDumpDay = true;
            else
                bForceDumpDay = false;


            string strAutoDumpDay = DomUtil.GetAttr(dom.DocumentElement,
                "autoDumpDay");
            if (strAutoDumpDay.ToLower() == "yes"
                || strAutoDumpDay.ToLower() == "true")
                bAutoDumpDay = true;
            else
                bAutoDumpDay = false;


            // ȱʡΪtrue
            string strLoop = DomUtil.GetAttr(dom.DocumentElement,
    "loop");
            if (strLoop.ToLower() == "no"
                || strLoop.ToLower() == "false")
                bLoop = false;
            else
                bLoop = true;

            return 0;
        }

        public static string MakeZhengyuanReplicationParam(
            bool bForceDumpAll,
            bool bForceDumpDay,
            bool bAutoDumpDay,
            bool bClearFirst,
            bool bLoop)
        {
            XmlDocument dom = new XmlDocument();

            dom.LoadXml("<root />");

            DomUtil.SetAttr(dom.DocumentElement, "clearFirst",
                bClearFirst == true ? "yes" : "no");
            DomUtil.SetAttr(dom.DocumentElement, "forceDumpAll",
                bForceDumpAll == true ? "yes" : "no");

            DomUtil.SetAttr(dom.DocumentElement, "forceDumpDay",
                bForceDumpDay == true ? "yes" : "no");

            DomUtil.SetAttr(dom.DocumentElement, "autoDumpDay",
                bAutoDumpDay == true ? "yes" : "no");

            DomUtil.SetAttr(dom.DocumentElement, "loop",
                bLoop == true ? "yes" : "no");

            return dom.OuterXml;
        }

        // �����ǲ��Ƿ�����ÿ������ʱ��(�Ժ�)?
        // parameters:
        //      strLastTime ���һ��ִ�й���ʱ�� RFC1123��ʽ
        int IsNowAfterPerDayStart(
            string strLastTime,
            out bool bRet,
            out string strError)
        {
            strError = "";

            XmlNode node = this.App.LibraryCfgDom.DocumentElement.SelectSingleNode("//zhengyuan/dataCenter");

            if (node == null)
            {
                bRet = false;
                return 0;
            }

            string strStartTime = DomUtil.GetAttr(node, "startTime");
            if (String.IsNullOrEmpty(strStartTime) == true)
            {
                bRet = false;
                return 0;
            }

            string strHour = "";
            string strMinute = "";

            int nRet = strStartTime.IndexOf(":");
            if (nRet == -1)
            {
                strHour = strStartTime.Trim();
                strMinute = "00";
            }
            else
            {
                strHour = strStartTime.Substring(0, nRet).Trim();
                strMinute = strStartTime.Substring(nRet + 1).Trim();
            }

            int nHour = 0;
            int nMinute = 0;
            try
            {
                nHour = Convert.ToInt32(strHour);
                nMinute = Convert.ToInt32(strMinute);
            }
            catch
            {
                bRet = false;
                strError = "ʱ��ֵ " + strStartTime + " ��ʽ����ȷ��ӦΪ hh:mm";
                return -1;   // ��ʽ����ȷ
            }



            DateTime now1 = DateTime.Now;

            // �۲챾���Ƿ��Ѿ�������
            if (String.IsNullOrEmpty(strLastTime) == false)
            {
                try
                {
                    DateTime lasttime = DateTimeUtil.FromRfc1123DateTimeString(strLastTime);

                    if (lasttime.Year == now1.Year
                        && lasttime.Month == now1.Month
                        && lasttime.Day == now1.Day)
                    {
                        bRet = false;   // �����Ѿ�������
                        return 0;
                    }
                }
                catch
                {
                    bRet = false;
                    strError = "strLastTime " + strLastTime + " ��ʽ����";
                    return -1;
                }
            }

            DateTime now2 = new DateTime(now1.Year,
                now1.Month,
                now1.Day,
                nHour,
                nMinute,
                0);

            if (now1 >= now2)
                bRet = true;
            else
                bRet = false;

            return 0;
        }


        public override void Worker()
        {
            // ϵͳ�����ʱ�򣬲����б��߳�
            // 2007/12/18
            if (this.App.HangupReason == HangupReason.LogRecover)
                return;
            // 2012/2/4
            if (this.App.PauseBatchTask == true)
                return;

            string strError = "";

            BatchTaskStartInfo startinfo = this.StartInfo;
            if (startinfo == null)
                startinfo = new BatchTaskStartInfo();   // ����ȱʡֵ��

            int nRet = ParseZhengyuanReplicationStart(startinfo.Start,
                out strError);
            if (nRet == -1)
            {
                this.AppendResultText("����ʧ��: " + strError + "\r\n");
                return;
            }

            // ͨ����������
            bool bForceDumpAll = false;
            bool bForceDumpDay = false;
            bool bAutoDumpDay = false;
            bool bClearFirst = false;
            bool bLoop = true;
            nRet = ParseZhengyuanReplicationParam(startinfo.Param,
                out bForceDumpAll,
                out bForceDumpDay,
                out bAutoDumpDay,
                out bClearFirst,
                out bLoop,
                out strError);
            if (nRet == -1)
            {
                this.AppendResultText("����ʧ��: " + strError + "\r\n");
                return;
            }

            this.Loop = bLoop;

            if (bClearFirst == true)
            {
                // ɾ�����߿�������û�н�����Ϣ�Ķ�����Ϣ��
            }

            if (bForceDumpAll == true)
            {
                // ���¿�����Ϣ������(AccountsCompleteInfo_yyyymmdd.xml)
                string strDataFileName = "AccountsCompleteInfo_" + GetCurrentDate() + ".xml";
                string strLocalFilePath = PathUtil.MergePath(this.App.ZhengyuanDir, strDataFileName);

                try
                {
                    // return:
                    //      -1  ����
                    //      0   ��������
                    //      1   ���û��ж�
                    nRet = DownloadDataFile(strDataFileName,
                        strLocalFilePath,
                        out strError);
                    if (nRet == -1)
                    {
                        string strErrorText = "���������ļ�" + strDataFileName + "ʧ��: " + strError;
                        this.AppendResultText(strErrorText + "\r\n");
                        this.App.WriteErrorLog(strErrorText);
                        return;
                    }
                    if (nRet == 1)
                    {
                        this.AppendResultText("���������ļ�"+strDataFileName+"���ж�\r\n");
                        this.Loop = false;
                        return;
                    }

                    // �������ļ�д����ӳ���ϵ�Ķ��߿�
                    this.AppendResultText("ͬ�������ļ� " + strDataFileName + " ��ʼ\r\n");

                    // return:
                    //      -1  error
                    //      0   succeed
                    //      1   �ж�
                    nRet = WriteToReaderDb(strLocalFilePath,
                        out strError);
                    if (nRet == -1)
                    {
                        string strErrorText = "�ļ� " + strDataFileName + " д����߿�: " + strError;
                        this.AppendResultText(strErrorText + "\r\n");
                        this.App.WriteErrorLog(strErrorText);
                        return;
                    }
                    else if (nRet == 1)
                    {
                        this.AppendResultText("ͬ�������ļ� " + strDataFileName + "���ж�\r\n");
                        return;
                    }
                    else
                    {
                        this.AppendResultText("ͬ�������ļ� " + strDataFileName + "���\r\n");

                        bForceDumpAll = false;
                        startinfo.Param = MakeZhengyuanReplicationParam(
                            bForceDumpAll,
                            bForceDumpDay,
                            bAutoDumpDay,
                            bClearFirst,
                            bLoop);

                    }

                }
                finally
                {
                    // ɾ���ù��������ļ�? ���Ǳ����������Թ۲�?
                    File.Delete(strLocalFilePath);
                }
            }

            if (bAutoDumpDay == true || bForceDumpDay == true)
            {

                string strLastTime = "";


                if (bForceDumpDay == false)
                {
                    Debug.Assert(bAutoDumpDay == true, ""); // ���߱���һ��==true
                    nRet = ReadLastTime(out strLastTime,
                        out strError);
                    if (nRet == -1)
                    {
                        string strErrorText = "���ļ��л�ȡÿ������ʱ��ʱ��������: " + strError;
                        this.AppendResultText(strErrorText + "\r\n");
                        this.App.WriteErrorLog(strErrorText);
                        return;
                    }


                    bool bRet = false;
                    nRet = IsNowAfterPerDayStart(
                        strLastTime,
                        out bRet,
                        out strError);
                    if (nRet == -1)
                    {
                        string strErrorText = "��ȡÿ������ʱ��ʱ��������: " + strError;
                        this.AppendResultText(strErrorText + "\r\n");
                        this.App.WriteErrorLog(strErrorText);
                        return;
                    }

                    if (bRet == false)
                        return; // ��û�е�ÿ��ʱ��
                }


                // ���¿�����Ϣ����(ÿ��)��(AccountsCompleteInfo_yyyymmdd.xml)
                string strDataFileName = "AccountsBasicInfo_" + GetCurrentDate() + ".xml";
                string strLocalFilePath = PathUtil.MergePath(this.App.ZhengyuanDir, strDataFileName);

                try
                {
                    // return:
                    //      -1  ����
                    //      0   ��������
                    //      1   ���û��ж�
                    nRet = DownloadDataFile(strDataFileName,
                        strLocalFilePath,
                        out strError);
                    if (nRet == -1)
                    {
                        string strErrorText = "���������ļ�" + strDataFileName + "ʧ��: " + strError;
                        this.AppendResultText(strErrorText + "\r\n");
                        this.App.WriteErrorLog(strErrorText);
                        return;
                    }
                    if (nRet == 1)
                    {
                        this.AppendResultText("���������ļ�" + strDataFileName + "���ж�\r\n");
                        this.Loop = false;
                        return;
                    }

                    // �������ļ�д����ӳ���ϵ�Ķ��߿�
                    this.AppendResultText("ͬ�������ļ� " + strDataFileName + " ��ʼ\r\n");

                    // return:
                    //      -1  error
                    //      0   succeed
                    //      1   �ж�
                    nRet = WriteToReaderDb(strLocalFilePath,
                        out strError);
                    if (nRet == -1)
                    {
                        string strErrorText = "�ļ� " + strDataFileName + " д����߿�: " + strError;
                        this.AppendResultText(strErrorText + "\r\n");
                        this.App.WriteErrorLog(strErrorText);
                        return;
                    }
                    else if (nRet == 1)
                    {
                        this.AppendResultText("ͬ�������ļ� " + strDataFileName + "���ж�\r\n");
                        return;
                    }
                    else
                    {
                        this.AppendResultText("ͬ�������ļ� " + strDataFileName + "���\r\n");

                        Debug.Assert(this.App != null, "");

                        // д���ļ��������Ѿ������ĵ���ʱ��
                        strLastTime = DateTimeUtil.Rfc1123DateTimeStringEx(this.App.Clock.UtcNow.ToLocalTime()); // 2007/12/17 changed // DateTime.UtcNow
                        WriteLastTime(strLastTime);

                        if (bForceDumpDay == true)
                        {

                            bForceDumpDay = false;
                            startinfo.Param = MakeZhengyuanReplicationParam(
                                bForceDumpAll,
                                bForceDumpDay,
                                bAutoDumpDay,
                                bClearFirst,
                                bLoop);
                        }

                    }

                }
                finally
                {
                    // ɾ���ù��������ļ�? ���Ǳ����������Թ۲�?
                    File.Delete(strLocalFilePath);
                }
            }

        }

        // ��ȡ�ϴ�������ʱ��
        int ReadLastTime(out string strLastTime,
            out string strError)
        {
            strError = "";
            strLastTime = "";

            string strFileName = PathUtil.MergePath(this.App.ZhengyuanDir, "lasttime.txt");

            StreamReader sr = null;

            try
            {
                sr = new StreamReader(strFileName, Encoding.UTF8);
            }
            catch (FileNotFoundException /*ex*/)
            {
                return 0;   // file not found
            }
            catch (Exception ex)
            {
                strError = "open file '" + strFileName + "' error : " + ex.Message;
                return -1;
            }
            try
            {
                strLastTime = sr.ReadLine();  // ����ʱ����
            }
            finally
            {
                sr.Close();
            }

            return 1;
        }

        // д��ϵ�����ļ�
        public void WriteLastTime(string strLastTime)
        {
            string strFileName = PathUtil.MergePath(this.App.ZhengyuanDir, "lasttime.txt");

            // ɾ��ԭ�����ļ�
            File.Delete(strFileName);

            // д��������
            StreamUtil.WriteText(strFileName,
                strLastTime);
        }

        // �����¿�����Ϣ������(AccountsCompleteInfo_yyyymmdd.xml)д����߿�
        // return:
        //      -1  error
        //      0   succeed
        //      1   �ж�
        int WriteToReaderDb(string strLocalFilePath,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            XmlNode node = this.App.LibraryCfgDom.DocumentElement.SelectSingleNode("//zhengyuan/replication");

            if (node == null)
            {
                strError = "��δ����<zhangyuan><replication>����";
                return -1;
            }

            string strMapDbName = DomUtil.GetAttr(node, "mapDbName");
            if (String.IsNullOrEmpty(strMapDbName) == true)
            {
                strError = "��δ����<zhangyuan/replication>Ԫ�ص�mapDbName����";
                return -1;
            }

            Stream file = File.Open(strLocalFilePath,
                FileMode.Open,
                FileAccess.Read);

            if (file.Length == 0)
                return 0;

            try
            {

                XmlTextReader reader = new XmlTextReader(file);

                bool bRet = false;

                // ��ʱ��SessionInfo����
                SessionInfo sessioninfo = new SessionInfo(this.App);

                // ģ��һ���˻�
                Account account = new Account();
                account.LoginName = "replication";
                account.Password = "";
                account.Rights = "setreaderinfo";

                account.Type = "";
                account.Barcode = "";
                account.Name = "replication";
                account.UserID = "replication";
                account.RmsUserName = this.App.ManagerUserName;
                account.RmsPassword = this.App.ManagerPassword;

                sessioninfo.Account = account;

                // �ҵ���
                while (true)
                {
                    try
                    {
                        bRet = reader.Read();
                    }
                    catch (Exception ex)
                    {
                        strError = "��XML�ļ���������: " + ex.Message;
                        return -1;
                    }

                    if (bRet == false)
                    {
                        strError = "û�и�Ԫ��";
                        return -1;
                    }
                    if (reader.NodeType == XmlNodeType.Element)
                        break;
                }

                for (int i = 0; ; i++)
                {
                    if (this.Stopped == true)
                        return 1;


                    bool bEnd = false;
                    // �ڶ���Ԫ��
                    while (true)
                    {
                        bRet = reader.Read();
                        if (bRet == false)
                        {
                            bEnd = true;  // ����
                            break;
                        }
                        if (reader.NodeType == XmlNodeType.Element)
                            break;
                    }

                    if (bEnd == true)
                        break;

                    this.AppendResultText("���� " + (i + 1).ToString() + "\r\n");

                    // ��¼��
                    string strXml = reader.ReadOuterXml();

                    // return:
                    //      -1  error
                    //      0   �Ѿ�д��
                    //      1   û�б�Ҫд��
                    nRet = WriteOneReaderInfo(
                        sessioninfo,
                        strMapDbName,
                        strXml,
                        out strError);
                    if (nRet == -1)
                        return -1;

                }

                return 0;
            }
            finally
            {
                file.Close();
            }
        }

        // д��һ������
        // return:
        //      -1  error
        //      0   �Ѿ�д��
        //      1   û�б�Ҫд��
        int WriteOneReaderInfo(
            SessionInfo sessioninfo,
            string strReaderDbName,
            string strZhengyuanXml,
            out string strError)
        {
            strError = "";

            XmlDocument zhengyuandom = new XmlDocument();

            try
            {
                zhengyuandom.LoadXml(strZhengyuanXml);
            }
            catch (Exception ex)
            {
                strError = "����Ԫ�����ж�����XMLƬ��װ��DOMʧ��: " + ex.Message;
                return -1;
            }

            // AccType
            // ��������
            // 1��ʽ��,2 ��ʱ��
            string strAccType = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "ACCTYPE");
            if (strAccType != "1")
            {
                return 1;
            }

            string strBarcode = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "ACCNUM");
            if (String.IsNullOrEmpty(strBarcode) == true)
            {
                strError = "ȱ��<ACCNUM>Ԫ��";
                return -1;
            }

            strBarcode = strBarcode.PadLeft(10, '0');

            int nRet = 0;
            string strReaderXml = "";
            string strOutputPath = "";
            byte[] baTimestamp = null;

            // �Ӷ���
            // ���Ա����õ����߼�¼������;����ʱ״̬
            this.App.ReaderLocks.LockForRead(strBarcode);

            try
            {
                // ��ö��߼�¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                nRet = this.App.GetReaderRecXml(
                    this.RmsChannels, // sessioninfo.Channels,
                    strBarcode,
                    out strReaderXml,
                    out strOutputPath,
                    out baTimestamp,
                    out strError);

            }
            finally
            {
                this.App.ReaderLocks.UnlockForRead(strBarcode);
            }

            if (nRet == -1)
                return -1;
            if (nRet > 1)
            {
                strError = "����� " + strBarcode + "�ڶ��߿�Ⱥ�м������� " + nRet.ToString() + " �����뾡������˴���";
                return -1;
            }

            string strAction = "";
            string strRecPath = "";

            string strNewXml = "";  // �޸ĺ�ļ�¼

            if (nRet == 0)
            {
                // û�����У������¼�¼
                strAction = "new";
                strRecPath = strReaderDbName + "/?";
                strReaderXml = "";  // 2009/7/17 changed // "<root />";
            }
            else
            {
                Debug.Assert(nRet == 1, "");
                // ���У��޸ĺ󸲸�ԭ��¼

                strAction = "change";
                strRecPath = strOutputPath;
            }

            XmlDocument readerdom = new XmlDocument();
            try
            {
                readerdom.LoadXml(strReaderXml);
            }
            catch (Exception ex)
            {
                strError = "����XML��¼װ��DOM��������: " + ex.Message;
                return -1;
            }

            nRet = ModifyReaderRecord(ref readerdom,
                zhengyuandom,
                out strError);
            if (nRet == -1)
                return -1;

            if (nRet == 1)  // û�б�Ҫд��
            {
                Debug.Assert(strAction == "change", "");
                return 1;
            }

            if (nRet == 2) // û�б�Ҫд��
            {
                return 1;
            }

            strNewXml = readerdom.OuterXml;

            string strExistingXml = "";
            string strSavedXml = "";
            string strSavedRecPath = "";
            byte [] baNewTimestamp = null;
            DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue kernel_errorcode = DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue.NoError;

            LibraryServerResult result = this.App.SetReaderInfo(
                    sessioninfo,
                    strAction,
                    strRecPath,
                    strNewXml,
                    strReaderXml,
                    baTimestamp,
                    out strExistingXml,
                    out strSavedXml,
                    out strSavedRecPath,
                    out baNewTimestamp,
                    out kernel_errorcode);
            if (result.Value == -1)
            {
                strError = result.ErrorInfo;
                return -1;
            }



            return 0;   // ����д����
        }

        /*
        public static int Date8toRfc1123(string strOrigin,
out string strTarget,
out string strError)
        {
            strError = "";
            strTarget = "";

            strOrigin = strOrigin.Replace("-", "");

            // ��ʽΪ 20060625�� ��Ҫת��Ϊrfc
            if (strOrigin.Length != 8)
            {
                strError = "Դ�����ַ��� '" + strOrigin + "' ��ʽ����ȷ��ӦΪ8�ַ�";
                return -1;
            }


            IFormatProvider culture = new CultureInfo("zh-CN", true);

            DateTime time;
            try
            {
                time = DateTime.ParseExact(strOrigin, "yyyyMMdd", culture);
            }
            catch
            {
                strError = "�����ַ��� '" + strOrigin + "' �ַ���ת��ΪDateTime����ʱ����";
                return -1;
            }

            time = time.ToUniversalTime();
            strTarget = DateTimeUtil.Rfc1123DateTimeString(time);


            return 0;
        }
         * */

        // ������Ԫ�����޸Ļ��ߴ�����¼
        // return:
        //      -1  error
        //      0   ����
        //      1   ������Ԫ����Ϣreaderdom���Ѿ����ˣ���zhengyuandom�м���д���һģһ��
        //      2   û�б�Ҫд�����Ϣ
        int ModifyReaderRecord(ref XmlDocument readerdom,
            XmlDocument zhengyuandom,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            
            /*
    <Person>
        <ACCNUM>100</ACCNUM>
        <CARDID>3163593110</CARDID>
        <CARDCODE>1</CARDCODE>
        <ACCSTATUS>1</ACCSTATUS>
        <ACCTYPE>1</ACCTYPE>
        <PERCODE>a001</PERCODE>
        <AREANUM>1</AREANUM>
        <ACCNAME>����00100</ACCNAME>
        <DEPNUM>2</DEPNUM>
        <DEPNAME>����ѧԺ</DEPNAME>
        <CLSNUM>2</CLSNUM>
        <CLSNAME>�ƻ��Ȿ��</CLSNAME>
        <ACCSEX>0 </ACCSEX>
        <POSTDATE>2005-08-15</POSTDATE>
        <LOSTDATE>2009-08-14</LOSTDATE>
    </Person>
             * */

            // ����������Ԫ����Ϣ�ͼ�¼��ԭ�е���Ԫ��Ϣ�Ƿ�һ����
            // ���һ���Ͳ��ر����ˡ�

            XmlNode oldnode = readerdom.DocumentElement.SelectSingleNode("zhengyuan");
            if (oldnode != null)
            {
                if (oldnode.InnerXml == zhengyuandom.InnerXml)
                    return 1;   // ������Ԫ����Ϣ�Ѿ����ˣ��ͼ���д���һģһ��
            }

            // AccType
            // ��������
            // 1��ʽ��,2 ��ʱ��
            string strAccType = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "ACCTYPE");
            if (strAccType != "1")
            {
                return 2;
            }


            // ACCNUM �ʺ�
            string strBarcode = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "ACCNUM");

            // ȷ��Ϊ10λ
            strBarcode = strBarcode.PadLeft(10, '0');

            DomUtil.SetElementText(readerdom.DocumentElement,
                "barcode",
                strBarcode);


            // AccStatus
            // ����״̬
            // 0:�ѳ���,1:��Ч��,2:��ʧ��,3:���Ῠ,4:Ԥ����
            string strAccStatus = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "ACCSTATUS");
            // �޸Ķ���״̬
            if (strAccStatus != "1")
            {
                string strState = GetAccStatusString(strAccStatus);
                DomUtil.SetElementText(readerdom.DocumentElement,
                    "state",
                    strState);
            }
            else
            {
                DomUtil.SetElementText(readerdom.DocumentElement,
                    "state",
                    "");    // ����״̬
            }



            // AccName
            // ��������
            // 8������
            string strAccName = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "ACCNAME");
            DomUtil.SetElementText(readerdom.DocumentElement,
                "name",
                strAccName);


            // DepName
            // ��������
            string strDepName = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "DEPNAME");
            DomUtil.SetElementText(readerdom.DocumentElement,
                "department",
                strDepName);

            // AccSex
            // �����Ա�
            // ��/Ů/""
            string strAccSex = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "ACCSEX");
            DomUtil.SetElementText(readerdom.DocumentElement,
                "gender",
                strAccSex);

            // mobileCode
            // �ֻ�����
            string strMobileCode = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "MOBILECODE");
            DomUtil.SetElementText(readerdom.DocumentElement,
                "tel",
                strMobileCode);

            // EMail
            // email��ַ
            string strEmail = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "EMAIL");
            DomUtil.SetElementText(readerdom.DocumentElement,
                "email",
                strEmail);

            string strRfcTime = "";

            // PostDate
            // �俨����
            string strPostDate = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "POSTDATE");

            if (String.IsNullOrEmpty(strPostDate) == false)
            {
                //  8λ���ڸ�ʽת��ΪGMTʱ��
                nRet = DateTimeUtil.Date8toRfc1123(strPostDate,
                    out strRfcTime,
                    out strError);
                if (nRet == -1)
                {
                    strError = "<POSTDATE>�е�����ֵ '" + strPostDate + "' ��ʽ����ȷ: " + strError;
                    return -1;
                }
            }
            else
            {
                strRfcTime = "";
            }
            DomUtil.SetElementText(readerdom.DocumentElement,
                "createDate",
                strRfcTime);

            // LostDate
            // ʧЧ����
            string strLostDate = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "LOSTDATE");

            if (String.IsNullOrEmpty(strLostDate) == false)
            {
                //  8λ���ڸ�ʽת��ΪGMTʱ��
                nRet = DateTimeUtil.Date8toRfc1123(strLostDate,
                    out strRfcTime,
                    out strError);
                if (nRet == -1)
                {
                    strError = "<LOSTDATE>�е�����ֵ '" + strLostDate + "' ��ʽ����ȷ: " + strError;
                    return -1;
                }
            }
            else
            {
                strRfcTime = "";
            }

            DomUtil.SetElementText(readerdom.DocumentElement,
                "expireDate",
                strRfcTime);


            // BirthDay
            // ����
            string strBirthDay = DomUtil.GetElementText(zhengyuandom.DocumentElement,
                "BIRTHDAY");
            if (String.IsNullOrEmpty(strBirthDay) == false)
            {
                //  8λ���ڸ�ʽת��ΪGMTʱ��
                nRet = DateTimeUtil.Date8toRfc1123(strBirthDay,
                    out strRfcTime,
                    out strError);
                if (nRet == -1)
                {
                    strError = "<BIRTHDAY>�е�����ֵ '" + strBirthDay + "' ��ʽ����ȷ: " + strError;
                    return -1;
                }
            }
            else
            {
                strRfcTime = "";
            }

            if (String.IsNullOrEmpty(strRfcTime) == false)
            {
                DomUtil.SetElementText(readerdom.DocumentElement,
                    "dateOfBirth",  // birthday
                    strRfcTime);
            }

            if (oldnode == null)
            {
                // ȫ��������Ԫ������
                oldnode = readerdom.CreateElement("zhengyuan");
                readerdom.DocumentElement.AppendChild(oldnode);
            }

            oldnode.InnerXml = zhengyuandom.DocumentElement.InnerXml;
            DomUtil.SetAttr(oldnode, "lastModified", DateTime.Now.ToString());  // ��������޸�ʱ��

            return 0;
        }

        static string GetAccStatusString(string strAccStatus)
        {
            if (strAccStatus == "0")
                return "�ѳ���";
            if (strAccStatus == "1")
                return "��Ч��";
            if (strAccStatus == "2")
                return "��ʧ��";
            if (strAccStatus == "3")
                return "���Ῠ";
            if (strAccStatus == "4")
                return "Ԥ����";
            return strAccStatus;    // ����Ԥ�����ֵ
        }

        static string GetCurrentDate()
        {
            DateTime now = DateTime.Now;

            return now.Year.ToString().PadLeft(4, '0')
            + now.Month.ToString().PadLeft(2, '0')
            + now.Day.ToString().PadLeft(2, '0');
        }

        // ��������������ò���
        int GetDataCenterParam(
            out string strServerUrl,
            out string strUserName,
            out string strPassword,
            out string strError)
        {
            strError = "";
            strServerUrl = 
            strUserName = "";
            strPassword = "";

            XmlNode node = this.App.LibraryCfgDom.DocumentElement.SelectSingleNode("//zhengyuan/dataCenter");

            if (node == null)
            {
                strError = "��δ����<zhangyuan/dataCenter>Ԫ��";
                return -1;
            }

            strServerUrl = DomUtil.GetAttr(node, "url");
            strUserName = DomUtil.GetAttr(node, "username");
            strPassword = DomUtil.GetAttr(node, "password");

            return 0;
        }

        // ���������ļ�
        // parameters:
        //      strDataFileName �����ļ�����������ļ�����
        //      strLocalFilePath    �����ļ���
        // return:
        //      -1  ����
        //      0   ��������
        //      1   ���û��ж�
        int DownloadDataFile(string strDataFileName,
            string strLocalFilePath,
            out string strError)
        {
            strError = "";

            string strServerUrl = "";
            string strUserName = "";
            string strPassword = "";

            // ��������������ò���
            int nRet = GetDataCenterParam(
                out strServerUrl,
                out strUserName,
                out strPassword,
                out strError);
            if (nRet == -1)
                return -1;

            string strPath = strServerUrl + "/" + strDataFileName;

            Uri serverUri = new Uri(strPath);

            /*
            // The serverUri parameter should start with the ftp:// scheme.
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
            }
             * */


            // Get the object used to communicate with the server.
            WebClient request = new WebClient();

            this.DownloadException = null;
            this.DownloadCancelled = false;
            this.eventDownloadFinished.Reset();

            request.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(request_DownloadFileCompleted);
            request.DownloadProgressChanged += new DownloadProgressChangedEventHandler(request_DownloadProgressChanged);

            request.Credentials = new NetworkCredential(strUserName,
                strPassword);

            try
            {

                File.Delete(strLocalFilePath);

                request.DownloadFileAsync(serverUri,
                    strLocalFilePath);
            }
            catch (WebException ex)
            {
                strError = "���������ļ� " + strPath+ " ʧ��: " + ex.ToString();
                return -1;
            }

            // �ȴ����ؽ���

            WaitHandle[] events = new WaitHandle[2];

            events[0] = this.eventClose;
            events[1] = this.eventDownloadFinished;

            while (true)
            {
                if (this.Stopped == true)
                {
                    request.CancelAsync();
                }

                int index = WaitHandle.WaitAny(events, 1000, false);    // ÿ�볬ʱһ��

                if (index == WaitHandle.WaitTimeout)
                {
                    // ��ʱ
                }
                else if (index == 0)
                {
                    strError = "���ر��ر��ź���ǰ�ж�";
                    return -1;
                }
                else
                {
                    // �õ������ź�
                    break;
                }
            }

            if (this.DownloadCancelled == true)
                return 1;   // ���û��ж�

            if (this.DownloadException != null)
            {
                strError = this.DownloadException.Message;
                if (this.DownloadException is WebException)
                {
                    WebException webex = (WebException)this.DownloadException;
                    if (webex.Response is FtpWebResponse)
                    {
                        FtpWebResponse ftpr = (FtpWebResponse)webex.Response;
                        if (ftpr.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                        {
                            return -1;
                        }
                    }

                }
                return -1;
            }

            return 0;
        }

        void request_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if ((e.BytesReceived % 1024*100) == 0)
                this.AppendResultText("������: " + e.BytesReceived + "\r\n");
        }

        void request_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            this.DownloadException = e.Error;
            this.DownloadCancelled = e.Cancelled;
            this.eventDownloadFinished.Set();
        }

    }
}
