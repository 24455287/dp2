using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Globalization;

using DigitalPlatform.Xml;
using DigitalPlatform.DTLP;
using DigitalPlatform.Marc;
using DigitalPlatform.Text;
using DigitalPlatform.rms.Client;
using DigitalPlatform.IO;

namespace DigitalPlatform.LibraryServer
{
#if NOOOOOOOOOO
    /// <summary>
    /// ����DTLP���ݿ� ����������
    /// </summary>
    public class TraceDTLP : BatchTask
    {
        DtlpChannelArray DtlpChannels = new DtlpChannelArray();
        DtlpChannel DtlpChannel = null;

        // ��ʱ����ϵ���Ϣ�����ⷱ���������������
        string m_strStartFileName = "";
        int m_nStartIndex = 0;
        string m_strStartOffset = "";

        string m_strWarningFileName = "";


        // ���캯��
        public TraceDTLP(LibraryApplication app, 
            string strName)
            : base(app, strName)
        {
            // this.App = app;

            this.PerTime = 1 * 60 * 1000;	// 1����

            this.Loop = true;

            this.DtlpChannels.GUI = false;

            this.DtlpChannels.AskAccountInfo -= new AskDtlpAccountInfoEventHandle(DtlpChannels_AskAccountInfo);
            this.DtlpChannels.AskAccountInfo += new AskDtlpAccountInfoEventHandle(DtlpChannels_AskAccountInfo);

            this.DtlpChannel = this.DtlpChannels.CreateChannel(0);

            // ������Ϣ�ļ���ע�⾭��ɾ������ļ��������Խ��Խ��
            m_strWarningFileName = PathUtil.MergePath(app.LogDir, "dtlp_warning.txt");
        }

        void DtlpChannels_AskAccountInfo(object sender, AskDtlpAccountInfoEventArgs e)
        {
            e.Owner = null;

            string strUserName = "";
            string strPassword = "";
            string strError = "";
            int nRet = this.App.GetDtlpAccountInfo(e.Path,
                out strUserName,
                out strPassword,
                out strError);
            if (nRet == -1 || nRet == 0)
            {
                e.ErrorInfo = strError;
                e.Result = -1;
                return;
            }

            ///
            e.UserName = strUserName;
            e.Password = strPassword;
            e.Result = 1;
        }

        public override string DefaultName
        {
            get
            {
                return "����DTLP���ݿ�";
            }
        }

        // ����ϵ��ַ���
        static string MakeBreakPointString(
            int indexLog,
            string strLogStartOffset,
            string strLogFileName,
            string strRecordID,
            string strOriginDbName)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            // <dump>
            XmlNode nodeDump = dom.CreateElement("dump");
            dom.DocumentElement.AppendChild(nodeDump);

            DomUtil.SetAttr(nodeDump, "recordid", strRecordID);
            DomUtil.SetAttr(nodeDump, "origindbname", strOriginDbName);

            // <trace>
            XmlNode nodeTrace = dom.CreateElement("trace");
            dom.DocumentElement.AppendChild(nodeTrace);

            DomUtil.SetAttr(nodeTrace, "index", indexLog.ToString());
            DomUtil.SetAttr(nodeTrace, "startoffset", strLogStartOffset);
            DomUtil.SetAttr(nodeTrace, "logfilename", strLogFileName);

            return dom.OuterXml;
        }

        // ���� ��ʼ ����
        // Ӧ����һ�ְ취��ָ��������ϴ��ж�λ�����¿�ʼ����������ж�λ����������������Լ�����
        // ���Down���������ǲ���ͬһ����
        // parameters:
        //      strStart    �����ַ�������ʽһ��Ϊindex.offsetstring@logfilename
        //                  ����Զ��ַ���Ϊ"!breakpoint"����ʾ�ӷ���������Ķϵ���Ϣ��ʼ
        int ParseTraceDtlpStart(string strStart,
            out int indexLog,
            out string strLogStartOffset,
            out string strLogFileName,
            out string strRecordID,
            out string strOriginDbName,
            out string strError)
        {
            strError = "";
            indexLog = 0;
            strLogFileName = "";
            strLogStartOffset = "";
            strRecordID = "";
            strOriginDbName = "";

            int nRet = 0;

            if (String.IsNullOrEmpty(strStart) == true)
            {
                strError = "������������Ϊ��";
                return -1;
            }

            if (strStart == "!breakpoint")
            {
                // �Ӷϵ�����ļ��ж�����Ϣ
                // return:
                //      -1  error
                //      0   file not found
                //      1   found
                nRet = this.App.ReadBatchTaskBreakPointFile(
                    "����DTLP���ݿ�",
                    out strStart,
                    out strError);
                if (nRet == -1)
                {
                    strError = "ReadBatchTaskBreakPointFileʱ����" + strError;
                    this.App.WriteErrorLog(strError);
                    return -1;
                }

                // ���nRet == 0����ʾû�жϵ��ļ����ڣ�Ҳ��û�б�Ҫ�Ĳ����������������
                if (nRet == 0)
                {
                    strError = "��ǰ������û�з��ֶϵ���Ϣ���޷���������";
                    return -1;
                }

                Debug.Assert(nRet == 1, "");
                this.AppendResultText("������������ϴζϵ��ַ���Ϊ: "
                    + HttpUtility.HtmlEncode(strStart)
                    + "\r\n");

            }

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strStart);
            }
            catch (Exception ex)
            {
                strError = "װ��XML�ַ�������DOMʱ��������: " + ex.Message;
                return -1;
            }

            XmlNode nodeDump = dom.DocumentElement.SelectSingleNode("dump");
            if (nodeDump != null)
            {
                strRecordID = DomUtil.GetAttr(nodeDump, "recordid");
                strOriginDbName = DomUtil.GetAttr(nodeDump, "origindbname");
            }

            XmlNode nodeTrace = dom.DocumentElement.SelectSingleNode("trace");
            if (nodeTrace != null)
            {
                string strIndex = DomUtil.GetAttr(nodeTrace, "index");
                if (String.IsNullOrEmpty(strIndex) == true)
                    indexLog = 0;
                else {
                try
                {
                    indexLog = Convert.ToInt32(strIndex);
                }
                catch
                {
                    strError = "<trace>Ԫ����index����ֵ '" +strIndex+ "' ��ʽ����Ӧ��Ϊ������";
                    return -1;
                }
                }

                strLogStartOffset = DomUtil.GetAttr(nodeTrace, "startoffs");
                strLogFileName = DomUtil.GetAttr(nodeTrace, "logfilename");
            }

            return 0;
        }

        /*
        int ParseTraceStartString(string strStart,
            out int index,
            out string strStartOffset,
            out string strFileName,
            out string strError)
        {
            nIndex = 0;
            strFileName = "";
            strStartOffset = "";

            string strIndex = "";

            nRet = strStart.IndexOf('@');
            if (nRet == -1)
            {
                nRet = strStart.IndexOf('.');
                if (nRet != -1)
                {
                    strIndex = strStart.Substring(0, nRet);
                    strStartOffset = strStart.Substring(nRet + 1);
                }
                else
                {
                    strIndex = strStart;
                    strStartOffset = "";
                }

                try
                {
                    index = Convert.ToInt32(strIndex);
                }
                catch (Exception)
                {
                    strError = "�������� '" + strIndex + "' ��ʽ����" + "���û��@����ӦΪ�����֡�";
                    return -1;
                }
                return 0;
            }

            strIndex = strStart.Substring(0, nRet).Trim();
            strFileName = strStart.Substring(nRet + 1).Trim();

            nRet = strIndex.IndexOf('.');
            if (nRet != -1)
            {
                strStartOffset = strIndex.Substring(nRet + 1);
                strIndex = strIndex.Substring(0, nRet);
            }
            else
            {
                strStartOffset = "";
            }

            try
            {
                index = Convert.ToInt32(strIndex);
            }
            catch (Exception)
            {
                strError = "�������� '" + strIndex + "' ��ʽ����'" + strIndex + "' ����Ӧ��Ϊ�����֡�";
                return -1;
            }

            if (strFileName == "")
            {
                strError = "�������� '" + strStart + "' ��ʽ����ȱ����־�ļ���";
                return -1;
            }

            return 0;
        }
         * */


        public static string MakeTraceDtlpParam(
            bool bDump,
            bool bClearFirst,
            bool bLoop)
        {
            XmlDocument dom = new XmlDocument();

            dom.LoadXml("<root />");

            DomUtil.SetAttr(dom.DocumentElement, "clearFirst",
                bClearFirst == true ? "yes" : "no");
            DomUtil.SetAttr(dom.DocumentElement, "dump",
                bDump == true ? "yes" : "no");
            DomUtil.SetAttr(dom.DocumentElement, "loop",
                bLoop == true ? "yes" : "no");

            return dom.OuterXml;
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
        public static int ParseTraceDtlpParam(string strParam,
            out bool bDump,
            out bool bClearFirst,
            out bool bLoop,
            out string strError)
        {
            strError = "";
            bClearFirst = false;
            bDump = false;
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

            string strDump = DomUtil.GetAttr(dom.DocumentElement,
    "dump");
            if (strDump.ToLower() == "yes"
                || strDump.ToLower() == "true")
                bDump = true;
            else
                bDump = false;

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



        // һ�β���ѭ��
        public override void Worker()
        {
            // ϵͳ�����ʱ�򣬲����б��߳�
            // 2007/12/18 new add
            if (this.App.HangupReason == HangupReason.LogRecover)
                return;

            string strError = "";
            int nRet = 0;

            // ÿһ��ѭ������һ���µ�DtlpChannel����ֹ������һ������
            this.DtlpChannel = this.DtlpChannels.CreateChannel(0);

            //

            BatchTaskStartInfo startinfo = this.StartInfo;
            if (startinfo == null)
                startinfo = new BatchTaskStartInfo();   // ����ȱʡֵ��

            // ͨ����������
            bool bDump = false;
            bool bClearFirst = false;
            bool bLoop = true;
            nRet = ParseTraceDtlpParam(startinfo.Param,
                out bDump,
                out bClearFirst,
                out bLoop,
                out strError);
            if (nRet == -1)
            {
                this.AppendResultText("����ʧ��: " + strError + "\r\n");
                return;
            }

            this.Loop = bLoop;


            int nStartIndex = 0;// ��ʼλ��
            string strStartFileName = "";// ��ʼ�ļ���
            string strStartOffset = "";
            string strDumpRecordID = "";
            string strDumpOriginDbName = "";
            nRet = ParseTraceDtlpStart(startinfo.Start,
                out nStartIndex,
                out strStartOffset,
                out strStartFileName,
                out strDumpRecordID,
                out strDumpOriginDbName,
                out strError);
            if (nRet == -1)
            {
                this.AppendResultText("����ʧ��: " + strError + "\r\n");
                this.Loop = false;
                return;
            }

            if (strDumpRecordID != "" && strDumpOriginDbName != "")
                bDump = true;

            if (bClearFirst == true)
            {
                nRet = ClearAllServerDbs(out strError);
                if (nRet == -1)
                {
                    string strErrorText = "��ʼ������Ŀ���ʧ��: " + strError;
                    this.AppendResultText(strErrorText + "\r\n");
                    this.Loop = false;
                    this.App.WriteErrorLog(strErrorText);
                    return;
                }
                this.AppendResultText("���и���Ŀ����Ѿ�����ʼ��\r\n");

                // ����ͨ��������Ϣ�е�clearfirstֵ����ָ���´δ���
                bClearFirst = false;
                startinfo.Param = MakeTraceDtlpParam(
                    bDump,
                    bClearFirst,
                    bLoop);

            }


            if (bDump == true)
            {
                // ����������ʱ��Ҫ�����ϴ����ĸ��׶��жϵģ�Ҫ����ȷ�Ľ׶�����ִ��

                this.AppendResultText("Dump��ʼ\r\n");
                this.SetProgressText("Dump��ʼ");

                // д���ı��ļ�
                if (String.IsNullOrEmpty(this.m_strWarningFileName) == false)
                {
                    StreamUtil.WriteText(this.m_strWarningFileName,
                        "Dump��ʼ��ʱ�� "+DateTime.Now.ToString()+"\r\n");
                }

                string strBreakRecordID = "";
                string strBreakOriginDbName = "";

                // ��ʱ����
                m_strStartFileName = strStartFileName;
                m_nStartIndex = nStartIndex;
                m_strStartOffset = strStartOffset;

                try
                {
                    nRet = DumpAllServerDbs(
                        strDumpRecordID,
                        strDumpOriginDbName,
                        out strBreakRecordID,
                        out strBreakOriginDbName,
                        out strError);
                }
                catch (Exception ex)
                {
                    strError = "DumpAllServerDbs exception: " + ExceptionUtil.GetDebugText(ex);
                    this.DtlpChannel = null;
                    this.AppendResultText(strError + "\r\n");
                    this.SetProgressText(strError);
                    // д���ı��ļ�
                    if (String.IsNullOrEmpty(this.m_strWarningFileName) == false)
                    {
                        StreamUtil.WriteText(this.m_strWarningFileName,
                            strError + "\r\n");
                    }
                    nRet = -1;
                }

                if (nRet == -1)
                {
                    Debug.Assert(strBreakOriginDbName != "", "");
                    Debug.Assert(strBreakRecordID != "", "");
                    if (strBreakRecordID == ""
                        || strBreakOriginDbName == "")
                    {
                        strError = "dump�����ʱ��strBreakRecordID[" + strBreakRecordID + "]��strBreakOriginDbName[" + strBreakOriginDbName + "]ֵ����ӦΪ��";
                        this.App.WriteErrorLog(strError);
                    }
                }

                if (nRet == 0)
                {
                    /*
                    Debug.Assert(strBreakOriginDbName == "", "");
                    Debug.Assert(strBreakRecordID == "", "");
                     * */
                    strBreakOriginDbName = "";
                    strBreakRecordID = "";
                }
                if (nRet == 1)
                {
                    Debug.Assert(strBreakOriginDbName != "", "");
                    Debug.Assert(strBreakRecordID != "", "");
                    if (strBreakRecordID == ""
                        || strBreakOriginDbName == "")
                    {
                        strError = "dump�жϵ�ʱ��strBreakRecordID[" + strBreakRecordID + "]��strBreakOriginDbName[" + strBreakOriginDbName + "]ֵ����ӦΪ��";
                        this.App.WriteErrorLog(strError);
                    }

                }

                // ����ϵ�
                this.StartInfo.Start = MemoBreakPoint(
                    strStartFileName,
                    nStartIndex,
                    strStartOffset,
                    strBreakRecordID, //strRecordID,
                    strBreakOriginDbName //strOriginDbName
                    );

                if (nRet == -1)
                {
                    string strErrorText = "Dumpʧ��: " + strError;
                    this.AppendResultText(strErrorText + "\r\n");
                    this.App.WriteErrorLog(strErrorText);

                    this.Loop = true;   // �����Ժ��������ѭ��?
                    startinfo.Param = MakeTraceDtlpParam(
                        bDump,
                        bClearFirst,
                        bLoop);
                    return;
                }

                if (nRet == 1)
                {
                    this.AppendResultText("Dump�жϡ��ϵ�Ϊ"
                        + HttpUtility.HtmlEncode(this.StartInfo.Start)
                        + "\r\n");
                    this.SetProgressText("Dump�жϡ��ϵ�Ϊ"
                        + HttpUtility.HtmlEncode(this.StartInfo.Start));

                    this.Loop = false;
                    return;
                }
                else {
                    this.AppendResultText("Dump����\r\n");
                    this.SetProgressText("Dump����");

                    // д���ı��ļ�
                    if (String.IsNullOrEmpty(this.m_strWarningFileName) == false)
                    {
                        StreamUtil.WriteText(this.m_strWarningFileName,
                            "Dump������ʱ�� " + DateTime.Now.ToString() + "\r\n");
                        this.AppendResultText("������Ϣ�Ѿ�д���ļ� " + this.m_strWarningFileName + "\r\n");
                    }

                    /*
                    // ����ϵ㣬����dump�������
                    this.StartInfo.Start = MemoBreakPoint(
                        strStartFileName,
                        nStartIndex,
                        strStartOffset,
                        "", //strRecordID,
                        "" //strOriginDbName
                        );
                     * */

                    // ����ͨ��������Ϣ�е�dumpֵ����ָ���´δ���
                    bDump = false;
                    startinfo.Param = MakeTraceDtlpParam(
                        bDump,
                        bClearFirst,
                        bLoop);

                }
            } // end of -- if (bDump == true)

            //

            string strStartLogFileName = strStartFileName;

            string strFinishLogFileName = "";
            int nFinishIndex = -1;
            string strFinishOffset = "";

            // ��÷������б�
            XmlNodeList originNodes = this.App.LibraryCfgDom.DocumentElement.SelectNodes("//traceDTLP/origin");

            for (int i = 0; i < originNodes.Count; i++)
            {
                if (this.Stopped == true)
                    break;


                XmlNode originNode = originNodes[i];
                string strServerAddr = DomUtil.GetAttr(originNode, "serverAddr");

                if (String.IsNullOrEmpty(strServerAddr) == true)
                    continue;

                // �����������������־��ֱ��������־�ļ���ĩβ
                nRet = TraceOneServerLogs(strServerAddr,
                    strStartLogFileName,
                    nStartIndex,
                    strStartOffset,
                    out strFinishLogFileName,
                    out nFinishIndex,
                    out strFinishOffset,
                    out strError);
                if (nRet == -1)
                {
                    string strErrorText = "���ٷ����� " + strServerAddr + " ʧ��: " + strError + "\r\n";
                    this.AppendResultText(strErrorText);

                    this.App.WriteErrorLog(strErrorText);
                }
            }

            if (strFinishLogFileName == "")
                strFinishLogFileName = strStartFileName;

            if (nFinishIndex == -1)
            {
                nFinishIndex = 0;
                strFinishOffset = "";
            }

            Debug.Assert(strFinishLogFileName != "", "");

            /*
            if (strFinishOffset != "")
                this.StartInfo.Start = nFinishIndex.ToString() + "." + strFinishOffset // .������ƫ������ʾֵ
                    + "@" + strFinishLogFileName;  // ��ʹ��һ��ѭ��������ֵ +1��
            else
                this.StartInfo.Start = nFinishIndex.ToString() + "@" + strFinishLogFileName;  // ��ʹ��һ��ѭ��������ֵ

            // д��ϵ��ļ�
            // Ϊ����ǿ��̬Ч����������ѭ����;ÿ�����������д��һ��
            this.App.WriteBatchTaskBreakPointFile(this.Name,
                this.StartInfo.Start);
             * */
            this.StartInfo.Start = MemoBreakPoint(
                strFinishLogFileName,
                nFinishIndex,
                strFinishOffset,
                "",
                "");


            if (this.Stopped == true)
            {
                this.AppendResultText("������ֹͣ���ϵ�Ϊ " 
                    + HttpUtility.HtmlEncode(this.StartInfo.Start) 
                    + "\r\n");
                this.ProgressText = "������ֹͣ";
            }
        }

        // ����һ�¶ϵ㣬�Ա�����
        string MemoBreakPoint(
            string strFinishLogFileName,
            int nFinishIndex,
            string strFinishOffset,
            string strRecordID,
            string strOriginDbName)
        {
            string strBreakPointString = "";

            /*
            if (strFinishOffset != "")
                strBreakPointString = nFinishIndex.ToString() + "." + strFinishOffset // .������ƫ������ʾֵ
                    + "@" + strFinishLogFileName;  // ��ʹ��һ��ѭ��������ֵ +1��
            else
                strBreakPointString = nFinishIndex.ToString() + "@" + strFinishLogFileName;  // ��ʹ��һ��ѭ��������ֵ
             * */

            strBreakPointString = MakeBreakPointString(
                nFinishIndex,
                strFinishOffset,
                strFinishLogFileName,
                strRecordID,
                strOriginDbName);

            // д��ϵ��ļ�
            this.App.WriteBatchTaskBreakPointFile(this.Name,
                strBreakPointString);

            return strBreakPointString;
        }

        // ����һ������������־��ֱ��������־�ļ���ĩβ
        // parameters:
        //      strFinishLogFileName    ����ʱ��һ���ļ���
        //      nFinishIndex    �����ļ�¼ƫ��(�Ѿ��ɹ�����ļ�¼�������Ѿ��ɹ�����)
        // return:
        //      -1  error
        //      0   succeed
        public int TraceOneServerLogs(string strServerAddr,
            string strStartLogFileName,
            int nStartIndex,
            string strStartOffset,
            out string strFinishLogFileName,
            out int nFinishIndex,
            out string strFinishOffset,
            out string strError)
        {
            strError = "";
            strFinishLogFileName = "";
            nFinishIndex = -1;
            strFinishOffset = "";

            int nRet = 0;

            string strLogFileName = strStartLogFileName;

            for (int i = 0; ; i++)
            {
                if (this.Stopped == true)
                    break;

                // ����ʱ�����ĺô��ǣ��������GetOneFileLogRecords()ʧ�ܣ�
                // �ϴ�������finish���������ڣ���ͱ����ˡ�����ɹ���һ�Ρ��Ĳ���
                int nTempFinishIndex = -1;
                string strTempFinishOffset = "";

                // ���һ���ض���־�ļ��ڵ�ȫ����־��¼
                // return:
                //      -1  ����
                //      0   ��־�ļ�������
                //      1   ��־�ļ�����
                nRet = GetOneFileLogRecords(strServerAddr,
                    strLogFileName,
                    nStartIndex,
                    strStartOffset,
                    out nTempFinishIndex,
                    out strTempFinishOffset,
                    out strError);
                if (nRet == -1)
                    return -1;

                strFinishLogFileName = strLogFileName;
                strFinishOffset = strTempFinishOffset;
                nFinishIndex = nTempFinishIndex;

                if (nFinishIndex == -1)
                {
                    nFinishIndex = 0;
                    strFinishOffset = "";   // dt1500�ں���bug������������һ������ĺܴ�ֵ������ʵ���ļ��ĳ��Ⱥܶ࣬�ͻᵼ���ں˿��
                }

                // ����һ�¶ϵ㣬�Ա�����
                MemoBreakPoint(
                    strFinishLogFileName,
                    nFinishIndex,
                    strFinishOffset,
                    "",
                    "");


                // ��һ���ļ���

                string strNextLogFileName = "";
                // ��ã������ϣ���һ����־�ļ���
                // return:
                //      -1  error
                //      0   ��ȷ
                //      1   ��ȷ������strLogFileName�Ѿ��ǽ����������
                nRet = NextLogFileName(strLogFileName,
                    out strNextLogFileName,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (nRet == 1)
                    break;

                nStartIndex = 0;    // �ӵڶ����ļ��Ժ����Ϊ0
                strStartOffset = "";    // dt1500�ں���bug������������һ������ĺܴ�ֵ������ʵ���ļ��ĳ��Ⱥܶ࣬�ͻᵼ���ں˿��

                strLogFileName = strNextLogFileName;
            }

            return 0;
        }

        // ��ã������ϣ���һ����־�ļ���
        // return:
        //      -1  error
        //      0   ��ȷ
        //      1   ��ȷ������strLogFileName�Ѿ��ǽ����������
        static int NextLogFileName(string strLogFileName,
            out string strNextLogFileName,
            out string strError)
        {
            strError = "";
            strNextLogFileName = "";
            int nRet = 0;

            string strYear = strLogFileName.Substring(0, 4);
            string strMonth = strLogFileName.Substring(4, 2);
            string strDay = strLogFileName.Substring(6, 2);

            int nYear = 0;
            int nMonth = 0;
            int nDay = 0;

            try
            {
                nYear = Convert.ToInt32(strYear);
            }
            catch
            {
                strError = "��־�ļ��� '" + strLogFileName + "' �е� '"
                    + strYear + "' ���ָ�ʽ����";
                return -1;
            }

            try
            {
                nMonth = Convert.ToInt32(strMonth);
            }
            catch
            {
                strError = "��־�ļ��� '" + strLogFileName + "' �е� '"
                    + strMonth + "' ���ָ�ʽ����";
                return -1;
            }

            try
            {
                nDay = Convert.ToInt32(strDay);
            }
            catch
            {
                strError = "��־�ļ��� '" + strLogFileName + "' �е� '"
                    + strDay + "' ���ָ�ʽ����";
                return -1;
            }

            DateTime time = DateTime.Now;
            try
            {
                time = new DateTime(nYear, nMonth, nDay);
            }
            catch (Exception ex)
            {
                strError = "���� "+strLogFileName+" ��ʽ����: " + ex.Message;
                return -1;
            }

            DateTime now = DateTime.Now;

            // ���滯ʱ��
            nRet = LibraryApplication.RoundTime("day",
                ref now,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = LibraryApplication.RoundTime("day",
                ref time,
                out strError);
            if (nRet == -1)
                return -1;

            bool bNow = false;
            if (time >= now)
                bNow = true;
            
            time = time + new TimeSpan(1, 0, 0, 0); // ����һ��

            strNextLogFileName = time.Year.ToString().PadLeft(4, '0')
            + time.Month.ToString().PadLeft(2, '0')
            + time.Day.ToString().PadLeft(2, '0');

            if (bNow == true)
                return 1;

            return 0;
        }

        // ���һ���ض���־�ļ��ڵ�ȫ����־��¼
        // return:
        //      -1  ����
        //      0   ��־�ļ�������
        //      1   ��־�ļ�����
        int GetOneFileLogRecords(string strServerAddr,
            string strLogFileName,
            int nStartIndex,
            string strStartOffset,
            out int nFinishIndex,
            out string strFinishOffset,
            out string strError)
        {
            strError = "";
            nFinishIndex = -1;  // -1��ʾ��δ����
            strFinishOffset = "";

            string strPath = strServerAddr + "/log/" + strLogFileName + "/" + nStartIndex.ToString();

            if (strStartOffset != "")
                strPath += "@" + strStartOffset;

            bool bFirst = true;

            string strDate = "";
            int nRecID = -1;
            string strOffset = "";

            int nStyle = 0;

            if (nStartIndex < 0)
                nStartIndex = 0;

            for (nRecID = nStartIndex; ;)
            {
                if (this.Stopped == true)
                    break;

                byte[] baPackage = null;

                if (bFirst == true)
                {
                }
                else
                {
                    strPath = strServerAddr + "/log/" + strDate/*strLogFileName*/ + "/" + nRecID.ToString() + "@" + strOffset;
                }

                Encoding encoding = this.DtlpChannel.GetPathEncoding(strPath);

                this.AppendResultText("�� " + strPath + "\r\n");
                this.ProgressText = strPath;


                int nRet = this.DtlpChannel.Search(strPath,
                    DtlpChannel.RIZHI_STYLE | nStyle,
                    out baPackage);
                if (nRet == -1)
                {
                    int errorcode = this.DtlpChannel.GetLastErrno();
                    if (errorcode == DtlpChannel.GL_NOTEXIST)
                    {
                        if (bFirst == true)
                            break;
                    }
                    strError = "��ȡ��־��¼:\r\n"
                        + "·��: " + strPath + "\r\n"
                        + "������: " + errorcode + "\r\n"
                        + "������Ϣ: " + this.DtlpChannel.GetErrorString(errorcode) + "\r\n";
                    return -1;
                }


                // ��������¼
                Package package = new Package();
                package.LoadPackage(baPackage,
                    encoding);
                package.Parse(PackageFormat.Binary);

                // �����һ·��
                string strNextPath = "";
                strNextPath = package.GetFirstPath();
                if (String.IsNullOrEmpty(strNextPath) == true)
                {
                    if (bFirst == true)
                    {
                        strError = "�ļ� " + strLogFileName + "������";
                        return 0;
                    }
                    // strError = "���� '" + strPath + "' ��Ӧ����·�����ֲ����� ...";
                    // return -1;
                    break;
                }

                // ��ü�¼����
                byte[] baContent = null;
                nRet = package.GetFirstBin(out baContent);
                if (nRet != 1)
                {
                    baContent = null;	// ����Ϊ�հ�
                }

                // �����¼
                /*
                Debug.Assert(nRecID == i, "");
                if (nRecID != i)
                {
                    strError = "nRecID=" + nRecID.ToString() + " ��i=" + i.ToString() + " ��ͬ��";
                    return -1;
                    // �Ƿ�����������������?
                }*/

                // �������
                nFinishIndex = nRecID + 1;  // ��Ϊ�����offset��������ȡ��һ���ģ�����Ӧ������һ����id����
                strFinishOffset = strOffset;


                // ÿ����100������һ�¶ϵ㣬�Ա�����
                if ((nRecID - nStartIndex) % 100 == 0)
                {
                    MemoBreakPoint(
                        strLogFileName,
                        nFinishIndex,
                        strFinishOffset,
                        "",
                        "");
                }



                string strMARC = DtlpChannel.GetDt1000LogRecord(baContent, encoding);

                string strOperCode = "";
                string strOperComment = "";
                string strOperPath = "";

                nRet = DtlpChannel.ParseDt1000LogRecord(strMARC,
                    out strOperCode,
                    out strOperComment,
                    out strOperPath,
                    out strError);
                if (nRet == -1)
                {
                    strOperComment = strError;
                }


                if (strOperCode == "00" || strOperCode == "02")
                {
                    try
                    {
                        // return:
                        //      -1  error
                        //      0   ���ֿ�������Ŀ�����ݿ�֮��
                        //      1   �ɹ�д��
                        nRet = WriteMarcRecord(
                            strOperCode,
                            strServerAddr,
                            strOperPath,
                            strMARC,
                            out strError);
                    }
                    catch (Exception ex)
                    {
                        strError = "\r\nstrOperCode='" + strOperCode + "'\r\n"
                            + "strServerAddr='" + strServerAddr + "'\r\n"
                            + "strOperPath='" + strOperPath + "'\r\n"
                            + "strMARC='" + strMARC + "'\r\n"
                            + "------\r\n";
                        this.App.WriteErrorLog("-- WriteMarcRecord()�׳��쳣: " + ex.Message + " --" + strError);
                        nRet = -2;
                    }

                    if (nRet == -2)
                    {
                        // ·����ʽ����
                        // ����
                    }
                    if (nRet == -1)
                    {
                        this.App.WriteErrorLog(strError);
                        // ������������
                    }


                }
                else if (strOperCode == "12")
                {
                    nRet = InitialDB(
                        strServerAddr,
                        strOperPath,
                        out strError);
                    if (nRet == -1)
                    {
                        this.App.WriteErrorLog(strError);
                        // ������������
                    }
                }


                // ����־��¼·������Ϊ���ڡ���š�ƫ��
                // һ����־��¼·��������Ϊ:
                // /ip/log/19991231/0@1234~5678
                // parameters:
                //		strLogPath		����������־��¼·��
                //		strDate			������������
                //		nRecID			�������ļ�¼��
                //		strOffset		�������ļ�¼ƫ�ƣ�����1234~5678
                // return:
                //		-1		����
                //		0		��ȷ
                nRet = DtlpChannel.ParseLogPath(strNextPath,
                    out strDate,
                    out nRecID,
                    out strOffset,
                    out strError);
                if (nRet == -1)
                    return -1;

                // CONTINUE:

                bFirst = false;
            }


            return 1;   // ��־�ļ����ڣ��ѻ���˼�¼
        }

        // ���Ŀ�����ݿ���
        int GetTargetDbName(string strTargetServerAddr,
            string strOriginDbName,
            out string strTargetDbName,
            out string strMarcSyntax,
            out string strError)
        {
            strTargetDbName = "";
            strError = "";
            strMarcSyntax = "";

            XmlNode node = this.App.LibraryCfgDom.DocumentElement.SelectSingleNode("//traceDTLP/origin[@serverAddr='" + strTargetServerAddr + "']/databaseMap/item[@originDatabase='"+strOriginDbName+"']");
            if (node == null)
                return 0;   // not found

            strTargetDbName = DomUtil.GetAttr(node, "targetDatabase");
            strMarcSyntax = DomUtil.GetAttr(node, "marcSyntax");
            return 1;   // found
        }

        // ��ʼ�����ݿ�
        // parameters:
        // return:
        //      -1  error
        //      0   ���ֿ�������Ŀ�����ݿ�֮��
        //      1   �ɹ���ʼ��
        int InitialDB(
            string strTargetServerAddr,
            string strOriginDbName,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            long lRet = 0;

            // ӳ�䵽Ŀ�����ݿ���
            string strTargetDbName = "";
            string strMarcSyntax = "";

            // ���Ŀ�����ݿ���
            nRet = GetTargetDbName(strTargetServerAddr,
                strOriginDbName,
                out strTargetDbName,
                out strMarcSyntax,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;   // ����Ŀ�����ݿ�֮��

            RmsChannel channel = this.RmsChannels.GetChannel(this.App.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }


            lRet = channel.DoInitialDB(strTargetDbName,
                out strError);
            if (lRet == -1)
            {
                strError = "channel.DoInitialDB() [dbname="+strTargetDbName+"] error :" + strError;
                return -1;
            }

            this.App.Statis.IncreaseEntryValue("����DTLP", "��ʼ�����ݿ����", 1);

            return 0;
        }

        // ������־��¼�еı���·��
        // return:
        //      -1  ����
        //      0   �ɹ�
        public static int ParseLogPath(string strPathParam,
            out string strDbName,
            out string strNumber,
            out string strError)
        {
            strError = "";
            strDbName = "";
            strNumber = "";

            // ������
            if (String.IsNullOrEmpty(strPathParam) == true)
            {
                strError = "ParseLogPath()��strPathParam����ֵ����Ϊ��";
                return -1;
            }

            string strPath = strPathParam;

            // ����
            int nRet = strPath.IndexOf('/');
            if (nRet == -1)
            {
                strDbName = strPath;
                return 0;
            }

            strDbName = strPath.Substring(0, nRet);

            strPath = strPath.Substring(nRet + 1);


            string strTemp = "";

            // '��¼������'����
            nRet = strPath.IndexOf('/');
            if (nRet == -1)
            {
                strTemp = strPath;
                return 0;
            }

            strTemp = strPath.Substring(0, nRet);

            if (strTemp != "ctlno" && strTemp != "��¼������")
            {
                strError = "·�� '" + strPathParam + "' ��ʽ����ȷ";
                return -1;
            }

            strPath = strPath.Substring(nRet + 1);

            // ����
            nRet = strPath.IndexOf('/');
            if (nRet == -1)
            {
                strNumber = strPath;
                return 0;
            }

            strNumber = strPath.Substring(0, nRet);

            return 0;
        }


        // д�����ɾ����¼
        // parameters:
        //      strOriginPath   ԭʼMARC��¼·������̬Ϊ"ͼ���Ŀ/ctlno/0000001"��ע�⣬û�з����������֡�
        // return:
        //      -2  ·������ȷ
        //      -1  error
        //      0   ���ֿ�������Ŀ�����ݿ�֮��
        //      1   �ɹ�д��
        int WriteMarcRecord(
            string strOperCode,
            string strTargetServerAddr,
            string strOriginPath,
            string strMARC,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            long lRet = 0;

            // string strOriginServerAddr = "";
            string strOriginDbName = "";
            string strOriginNumber = "";

            // ��������·��
            nRet = ParseLogPath(strOriginPath,
                out strOriginDbName,
                out strOriginNumber,
                out strError);
            if (nRet == -1)
                return -2;
            if (strOriginDbName == "")
            {
                strError = "·�� '" + strOriginPath + "' ��ȱ�����ݿ���";
                return -2;
            }
            if (strOriginNumber.Length != 7)
            {
                strError = "·�� '"+strOriginPath+"' ��ԭʼ������ '" +strOriginNumber+ "' ����7λ";
                return -2;
            }

            // ����������ǲ��Ǵ�����
            if (StringUtil.IsPureNumber(strOriginNumber) == false)
            {
                strError = "ԭʼ������ '" + strOriginNumber + "' ���Ǵ�����";
                return -2;
            }

            // ӳ�䵽Ŀ�����ݿ���
            string strTargetDbName = "";
            string strMarcSyntax = "";

            // ���Ŀ�����ݿ���
            nRet = GetTargetDbName(strTargetServerAddr,
                strOriginDbName,
                out strTargetDbName,
                out strMarcSyntax,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;   // ����Ŀ�����ݿ�֮��

            string strXml = "";
            if (strOperCode == "00")
            {
                // ��MARC��ʽת��ΪXML��ʽ
                /*
                nRet = ConvertMarcToXml(
                    strMarcSyntax,
                    strMARC,
                    out strXml,
                    out strError);
                 * */
                // 2008/5/16 changed
                nRet = MarcUtil.Marc2Xml(
    strMARC,
    strMarcSyntax,
    out strXml,
    out strError);

                if (nRet == -1)
                    return -1;
            }

            RmsChannel channel = this.RmsChannels.GetChannel(this.App.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            byte[] timestamp = null;
            byte[] output_timestamp = null;
            string strOutputPath = "";

            if (strOperCode == "00")
            {
                // д��¼
                lRet = channel.DoSaveTextRes(strTargetDbName + "/" + strOriginNumber,
                    strXml,
                    false,
                    "content,ignorechecktimestamp",
                    timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                    {
                        // �����ܷ�����
                        strError = "ʱ�����ͻ���������ܷ�������";
                        return -1;
                    }

                    strError = "channel.DoSaveTextRes() [path=" + strTargetDbName + "/" + strOriginNumber + "] error : " + strError;
                    return -1;
                }

                this.App.Statis.IncreaseEntryValue("����DTLP", "���Ǽ�¼����", 1);

            }
            if (strOperCode == "02")
            {
                int nRedoCount = 0;
            REDO_DEL:
                lRet = channel.DoDeleteRes(strTargetDbName + "/" + strOriginNumber,
                    timestamp,
                    out output_timestamp,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.NotFound)
                    {
                        // �����������������������
                        return 0;
                    }
                    if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                    {
                        if (nRedoCount >= 10)
                            return -1;
                        timestamp = output_timestamp;
                        nRedoCount++;
                        goto REDO_DEL;
                    }
                    strError = "channel.DoDeleteRes() [path=" + strTargetDbName + "/" + strOriginNumber + "] error : " + strError;
                    return -1;
                }

                this.App.Statis.IncreaseEntryValue("����DTLP", "ɾ����¼����", 1);
            }

            return 0;
        }

#if NOOOOOOOOOOOOOOOOO
        // ��MARC��¼�л�ü۸��ַ���
        // �����ǣ���Ҫ֪��USMARC�ļ۸����ĸ����ֶΣ���������Ƿ���Ҫ����
        int GetTitlePrice(
            string strMarcSyntax,
            string strMARC,
            out string strPrice,
            out string strError)
        {

        }
#endif

        /*
        // ��MARC��ʽת��ΪXML��ʽ
        int ConvertMarcToXml(
            string strMarcSyntax,
            string strMARC,
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            int nRet = 0;

            MemoryStream s = new MemoryStream();

            MarcXmlWriter writer = new MarcXmlWriter(s, Encoding.UTF8);

            // �ڵ�ǰû�ж���MARC�﷨������£�Ĭ��unimarc
            if (String.IsNullOrEmpty(strMarcSyntax) == true)
                strMarcSyntax = "unimarc";

            if (strMarcSyntax == "unimarc")
            {
                writer.MarcNameSpaceUri = DpNs.unimarcxml;
                writer.MarcPrefix = strMarcSyntax;
            }
            else if (strMarcSyntax == "usmarc")
            {
                writer.MarcNameSpaceUri = Ns.usmarcxml;
                writer.MarcPrefix = strMarcSyntax;
            }
            else
            {
                strError = "strMarcSyntaxֵӦ��Ϊunimarc��usmarc֮һ";
                return -1;
            }

            // string strDebug = strMARC.Replace((char)Record.FLDEND, '#');
            nRet = writer.WriteRecord(strMARC,
                out strError);
            if (nRet == -1)
                return -1;

            writer.Flush();
            s.Flush();

            // strXml = Encoding.UTF8.GetString(s.ToArray()); // BUG!!! �������ַ��������XmlDocument.LoadXml()װ�أ��ᱨ��

            // 2008/5/16 changed
            byte[] baContent = s.ToArray();
            strXml = ByteArray.ToString(baContent, Encoding.UTF8);

            return 0;
        }
         * */

        // ���ƶ��������Դ�����������ݿ⵽dp2�����
        // return:
        //      -1  error
        //      0   ��������
        //      1   ���ж�
        int DumpAllServerDbs(
            string strDumpStartRecordID,
            string strDumpStartOriginDbName,
            out string strBreakRecordID,
            out string strBreakOriginDbName,
            out string strError)
        {
            strError = "";
            strBreakRecordID = "";
            strBreakOriginDbName = "";

            // ��÷������б�
            XmlNodeList originNodes = this.App.LibraryCfgDom.DocumentElement.SelectNodes("//traceDTLP/origin");

            for (int i = 0; i < originNodes.Count; i++)
            {
                XmlNode originNode = originNodes[i];
                string strOriginServerAddr = DomUtil.GetAttr(originNode, "serverAddr");

                if (String.IsNullOrEmpty(strOriginServerAddr) == true)
                    continue;

                // ��ͬ�ķ�����֮�������������ݿ���ô�죿Ŀǰ��δ����������
                int nRet = DumpOneServerDbs(strOriginServerAddr,
                    strDumpStartRecordID,
                    strDumpStartOriginDbName,
                    out strBreakRecordID,
                    out strBreakOriginDbName,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 1)
                    return 1;

            }


            return 0;
        }

        // ����һ��Դ�������ڵ��������ݿ�
        // return:
        //      -1  error
        //      0   ��������
        //      1   ���ж�
        int DumpOneServerDbs(string strOriginServerAddr,
            string strDumpStartRecordID,
            string strDumpStartOriginDbName,
            out string strBreakRecordID,
            out string strBreakOriginDbName,
            out string strError)
        {
            strError = "";
            strBreakRecordID = "";
            strBreakOriginDbName = "";


            // ������ݿ��б�
            XmlNodeList itemNodes = this.App.LibraryCfgDom.DocumentElement.SelectNodes("//traceDTLP/origin[@serverAddr='"+strOriginServerAddr+"']/databaseMap/item");
            for (int i = 0; i < itemNodes.Count; i++)
            {
                XmlNode node = itemNodes[i];
                string strOriginDbName = DomUtil.GetAttr(node, "originDatabase");
                string strTargetDbName = DomUtil.GetAttr(node, "targetDatabase");
                string strMarcSyntax = DomUtil.GetAttr(node, "marcSyntax");
                string strTargetEntityDbName = DomUtil.GetAttr(node, "targetEntityDatabase");
                string strNoBiblio = DomUtil.GetAttr(node, "noBiblio");

                bool bNoBiblio = false;

                strNoBiblio = strNoBiblio.ToLower();

                if (strNoBiblio == "yes" || strNoBiblio == "true"
                    || strNoBiblio == "on")
                    bNoBiblio = true;

                if (bNoBiblio == true)
                {
                    this.AppendResultText("���Դ���ݿ� " + strOriginDbName + "��dump������������Ŀ��Ϣд��Ŀ��� " + strTargetDbName + "�Ĳ�����\r\n");
                }

                if (String.IsNullOrEmpty(strTargetEntityDbName) == false)
                {
                    this.AppendResultText("���Դ���ݿ� " + strOriginDbName + "��dump������ͬʱ�����軹��Ϣ��д��ʵ��� " + strTargetEntityDbName + "��\r\n");
                }

                if (strMarcSyntax == "readerxml")
                {
                    this.AppendResultText("���Դ���ݿ� " + strOriginDbName + "��dump����������������Ϣ��dp2��XML��ʽ��д��Ŀ��� " + strTargetDbName + "��\r\n");
                }

                // �����Ҫ��ָ��������ʼ
                if (String.IsNullOrEmpty(strDumpStartOriginDbName) == false)
                {
                    if (strOriginDbName != strDumpStartOriginDbName)
                        continue;
                }

                strDumpStartOriginDbName = "";  // һ���ҵ�����⿪ʼ������Ҫ���������������п⣬������Ҫ���������������������ֻ����һ����

                int nRet = DumpOneDb(strOriginServerAddr,
                    strOriginDbName,
                    strDumpStartRecordID,
                    strTargetDbName,
                    strMarcSyntax,
                    strTargetEntityDbName,
                    bNoBiblio,
                    out strBreakRecordID,
                    out strError);
                if (nRet == -1)
                {
                    strBreakOriginDbName = strOriginDbName;
                    return -1;
                }
                if (nRet == 1)
                {
                    strBreakOriginDbName = strOriginDbName;
                    return 1;
                }

                strDumpStartRecordID = "";   // �Ժ���Ŀ�Ͳ���������
            }


            return 0;
        }

        // ����һ�����ݿ��ȫ����¼
        // parameters:
        //      strTargetEntityDbName   Ŀ��ʵ��⡣����Ҫ��MARC��Ŀ�����е�986����ͨ��Ϣ������dp2ʵ���ʱ������Ҫ������������==null����ʾ����������ֻд��MARC��Ŀ����
        //      bNoBiblio   �Ƿ�Ҫд����Ŀ�⡣�������true����ʾ��д�룻�������false����ʾҪд�롣
        // return:
        //      -1  error
        //      0   ��������
        //      1   ���ж�
        int DumpOneDb(string strOriginServerAddr,
            string strOriginDbName,
            string strDumpStartRecordID,
            string strTargetDbName,
            string strMarcSyntax,
            string strTargetEntityDbName,
            bool bNoBiblio,
            out string strBreakRecordID,
            out string strError)
        {
            strError = "";
            strBreakRecordID = "";

            // ����DumpIO��

            DtlpIO DumpRecord = new DtlpIO();

            int nRet = 0;

            if (String.IsNullOrEmpty(strDumpStartRecordID) == true)
            {
                nRet = DumpRecord.Initial(this.DtlpChannels,
                     strOriginServerAddr + "/" + strOriginDbName,
                     "0000001",
                     "9999999");
                strDumpStartRecordID = "0000001";
            }
            else
            {
                nRet = DumpRecord.Initial(this.DtlpChannels,
                     strOriginServerAddr + "/" + strOriginDbName,
                     strDumpStartRecordID,
                     "9999999");
            }

            strBreakRecordID = strDumpStartRecordID;

            // У׼ת����Χ����β��
            // return:
            //		-1	����
            //		0	û�иı���β��
            //		1	У׼��ı�����β��
            //		2	��Ŀ����û�м�¼
            nRet = DumpRecord.VerifyRange(out strError);
            if (nRet == -1)
            {
                if (DumpRecord.ErrorNo == DtlpChannel.GL_INVALIDCHANNEL)
                    this.DtlpChannel = null;    // ��ʹ����Channel

                return -1;
            }
            if (nRet == 2)
            {	
                // ��Ŀ��Ϊ��
                strError = "���ݿ� " +
                    strOriginDbName
                    + " ��û�м�¼...";
                return 0;
            }
            if (nRet == 1)
            {
                // ��β�ŷ����˸ı�
                strBreakRecordID = DumpRecord.m_strStartNumber;
            }

            int nRecordCount = -1;

            for (; ; )
            {


                if (this.Stopped == true)
                {
                    return 1;
                }

                try
                {

                    // �õ���һ����¼
                    // return:
                    //		-1	����
                    //		0	����
                    //		1	����ĩβ(����m_strEndNumber)
                    //		2	û���ҵ���¼
                    nRet = DumpRecord.NextRecord(ref nRecordCount,
                        out strError);
                    if (nRet == -1)
                    {
                        if (DumpRecord.ErrorNo == DtlpChannel.GL_INVALIDCHANNEL)
                            this.DtlpChannel = null;

                        strError = "NextRecord error: " + strError;
                        return -1;
                    }
                }
                catch (Exception ex)
                {
                    strError = "NextRecord exception: " + ExceptionUtil.GetDebugText(ex);
                    this.DtlpChannel = null;
                    return -1;
                }

                // ׼���ϵ���Ϣ
                strBreakRecordID = DumpRecord.m_strCurNumber;

                if (String.IsNullOrEmpty(strBreakRecordID) == false)
                {
                    // ȡ�ú�һ������
                    try
                    {
                        strBreakRecordID = (Convert.ToInt64(strBreakRecordID) + 1).ToString().PadLeft(strBreakRecordID.Length, '0');
                    }
                    catch
                    {
                    }
                }

                if (nRet == 1)
                    return 0;

                // û���ҵ���¼
                if (nRet == 2)
                {
                    // ����ֹѭ������̽�Եض�����ļ�¼
                    DumpRecord.m_strStartNumber = DumpRecord.m_strCurNumber;
                    /*
                    m_ValueStaticSTRINGMessage = "��̽:" + pDumpRecord->m_strCurNumber;
                    UpdateData(FALSE);
                    */
                    nRecordCount = -1;
                    // ����ѭ��
                    continue;
                }

                string strCurRecordName = strOriginDbName + "//" + DumpRecord.m_strCurNumber;

                this.AppendResultText("����: " + strCurRecordName + "\r\n");
                this.SetProgressText(DateTime.Now.ToString() + " ����: " + strCurRecordName);

                string strXml = "";

                if (strMarcSyntax == "xmlreader")
                {
                    string strWarning = "";
                    // ��MARC��ʽת��ΪXML��ʽ
                    nRet = ConvertMarcToReaderXml(
                        DumpRecord.m_strRecord,
                        /*
                        0,
                        0,
                         * */
                        out strXml,
                        out strWarning,
                        out strError);
                    if (String.IsNullOrEmpty(strWarning) == false)
                    {
                        this.AppendResultText("ת������: " + strWarning + "\r\n");

                        // д���ı��ļ�
                        if (String.IsNullOrEmpty(this.m_strWarningFileName) == false)
                        {
                            StreamUtil.WriteText(this.m_strWarningFileName,
                                strCurRecordName + ": " + strWarning + "\r\n");
                        }
                    }
                }
                else
                {
                    Debug.Assert(strMarcSyntax == "unimarc" || strMarcSyntax == "usmarc", "");
                    // ��MARC��ʽת��ΪMARCXML��ʽ
                    /*
                    nRet = ConvertMarcToXml(
                        strMarcSyntax,
                        DumpRecord.m_strRecord,
                        out strXml,
                        out strError);
                     * */
                    // 2008/5/16 changed
                    nRet = MarcUtil.Marc2Xml(
    DumpRecord.m_strRecord,
    strMarcSyntax,
    out strXml,
    out strError);

                }
                if (nRet == -1)
                    return -1;

                // д��Ŀ���

                RmsChannel channel = this.RmsChannels.GetChannel(this.App.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    return -1;
                }

                string strOutputPath = "";

                if (bNoBiblio == false)
                {

                    byte[] timestamp = null;
                    byte[] output_timestamp = null;

                    // д��¼
                    long lRet = channel.DoSaveTextRes(strTargetDbName + "/" + DumpRecord.m_strCurNumber,
                        strXml,
                        false,
                        "content,ignorechecktimestamp",
                        timestamp,
                        out output_timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            // �����ܷ�����
                            strError = "ʱ�����ͻ���������ܷ�������";
                            return -1;
                        }

                        strError = "channel.DoSaveTextRes() [path=" + strTargetDbName + "/" + DumpRecord.m_strCurNumber + "] error : " + strError;
                        return -1;
                    }
                }
                else
                {
                    strOutputPath = strTargetDbName + "/" + DumpRecord.m_strCurNumber;
                }


                if (String.IsNullOrEmpty(strTargetEntityDbName) == false)
                {
                    string strParentRecordID = ResPath.GetRecordId(strOutputPath);
                    int nThisEntityCount = 0;
                    string strWarning = "";

                    try
                    {
                        // ��һ��MARC��¼�а�����ʵ����Ϣ���XML��ʽ���ϴ�
                        // parameters:
                        //      strEntityDbName ʵ�����ݿ���
                        //      strParentRecordID   ����¼ID
                        //      strMARC ����¼MARC
                        nRet = DoEntityRecordsUpload(
                            channel,
                            strTargetEntityDbName,
                            strParentRecordID,
                            DumpRecord.m_strRecord,
                            /*
                            0,  // nEntityBarcodeLength,
                            0,  // nReaderBarcodeLength,
                             * */
                            out nThisEntityCount,
                            out strWarning,
                            out strError);
                    }
                    catch (Exception ex)
                    {
                        strError = strCurRecordName + " DoEntityRecordsUpload() exception: " + ExceptionUtil.GetDebugText(ex);
                        // д���ı��ļ�
                        if (String.IsNullOrEmpty(this.m_strWarningFileName) == false)
                        {
                            StreamUtil.WriteText(this.m_strWarningFileName,
                                strCurRecordName + strError + "\r\n");
                        }
                        return -1;
                    }

                    if (nRet == -1)
                    {
                        strError = "ת��ʵ���¼ʱ [��Ŀ��¼path=" + strTargetDbName + "/" + DumpRecord.m_strCurNumber + "] �������� : " + strError;
                        return -1;
                    }
                    if (String.IsNullOrEmpty(strWarning) == false)
                    {
                        this.AppendResultText("ת������: " + strWarning + "\r\n");

                        // д���ı��ļ�
                        if (String.IsNullOrEmpty(this.m_strWarningFileName) == false)
                        {
                            StreamUtil.WriteText(this.m_strWarningFileName,
                                strCurRecordName + ": " + strWarning + "\r\n");
                        }
                    }

                    // ����ϵ�
                    if ((nRecordCount % 100) == 0)
                    {
                        // ÿ��100������һ�ζϵ���Ϣ
                        // 2006/12/20 new add
                        this.StartInfo.Start = MemoBreakPoint(
                            this.m_strStartFileName,
                            this.m_nStartIndex,
                            this.m_strStartOffset,
                            strBreakRecordID,
                            strOriginDbName
                            );
                    }
                }

            }

            // return 0;   // ��������
        }

        int ClearAllServerDbs(
            out string strError)
        {
            strError = "";

            // ��÷������б�
            XmlNodeList originNodes = this.App.LibraryCfgDom.DocumentElement.SelectNodes("//traceDTLP/origin");

            for (int i = 0; i < originNodes.Count; i++)
            {
                XmlNode originNode = originNodes[i];
                string strOriginServerAddr = DomUtil.GetAttr(originNode, "serverAddr");

                if (String.IsNullOrEmpty(strOriginServerAddr) == true)
                    continue;

                int nRet = ClearOneServerDbs(strOriginServerAddr,
                    out strError);
                if (nRet == -1)
                    return -1;
            }


            return 0;
        }

        // ��ʼ��һ���������ڵ����и���Ŀ��(������Դ)���ݿ�
        int ClearOneServerDbs(string strOriginServerAddr,
            out string strError)
        {
            strError = "";

            // ������ݿ��б�
            XmlNodeList itemNodes = this.App.LibraryCfgDom.DocumentElement.SelectNodes("//traceDTLP/origin[@serverAddr='" + strOriginServerAddr + "']/databaseMap/item");
            for (int i = 0; i < itemNodes.Count; i++)
            {
                XmlNode node = itemNodes[i];
                // string strOriginDbName = DomUtil.GetAttr(node, "originDatabase");
                string strTargetDbName = DomUtil.GetAttr(node, "targetDatabase");
                // string strMarcSyntax = DomUtil.GetAttr(node, "marcSyntax");
                string strTargetEntityDbName = DomUtil.GetAttr(node, "targetEntityDatabase");
                string strNoBiblio = DomUtil.GetAttr(node, "noBiblio");

                bool bNoBiblio = false;

                strNoBiblio = strNoBiblio.ToLower();

                if (strNoBiblio == "yes" || strNoBiblio == "true"
                    || strNoBiblio == "on")
                    bNoBiblio = true;

                RmsChannel channel = this.RmsChannels.GetChannel(this.App.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    return -1;
                }

                long lRet = 0;
                if (bNoBiblio == false)
                {
                    lRet = channel.DoInitialDB(strTargetDbName,
                        out strError);
                    if (lRet == -1)
                        return -1;
                    this.AppendResultText("��ʼ���� '" + strTargetDbName + "'��\r\n");
                }
                else
                {
                    // noBiblio�������������Ʋ�Ҫ��ʼ����Ŀ�⣬����ʵ����
                    this.AppendResultText("*û��*��ʼ����Ŀ�� '" + strTargetDbName + "'��\r\n");
                }

                // ������ʼ��ʵ���
                if (String.IsNullOrEmpty(strTargetEntityDbName) == false)
                {
                    lRet = channel.DoInitialDB(strTargetEntityDbName,
                        out strError);
                    if (lRet == -1)
                        return -1;
                    this.AppendResultText("��ʼ��ʵ��� '" + strTargetEntityDbName + "'��\r\n");
                }
            }

            return 0;
        }

        #region ����dt1000/dt1500�������ݵ�dp2��ع���

        // ��dt1000/dt1500����MARC��ʽת��Ϊdp2�Ķ���XML��ʽ
        /*
        int ConvertMarcToReaderXml(
            string strMARC,
            out string strXml,
            out string strError)
        {
            strError = "";

            return 0;
        }*/

        // return:
        //      -1  error
        //      0   OK
        //      1   Invalid
        int VerifyBarcode(
            bool bReader,
            string strBarcode,
            out string strError)
        {
            strError = "";

            int nResultValue = -1;
            // ִ�нű�����VerifyBarcode
            // parameters:
            // return:
            //      -2  not found script
            //      -1  ����
            //      0   �ɹ�
            int nRet = this.App.DoVerifyBarcodeScriptFunction(
                strBarcode,
                out nResultValue,
                out strError);
            if (nRet == -2)
                return 0;
            if (nRet == -1)
                return -1;
            if (nResultValue == 0)
            {
                return 1;
            }
            if (nRet == 1)
            {
                if (bReader == false)
                {
                    strError = "�������Ƕ���֤����š�";
                    return 1;
                }
            }

            if (nRet == 2)
            {
                if (bReader == true)
                {
                    strError = "�������ǲ�����š�";
                    return 1;
                }
            }

            return 0;
        }

        // ��MARC���߼�¼ת��ΪXML��ʽ
        // parameters:
        //      nReaderBarcodeLength    ����֤����ų��ȡ����==0����ʾ��У�鳤��

        public int ConvertMarcToReaderXml(
            string strMARC,
            /*
            int nReaderBarcodeLength,
            int nEntityBarcodeLength,
             * */
            out string strXml,
            out string strWarning,
            out string strError)
        {
            strXml = "";
            strError = "";
            strWarning = "";

            int nRet = 0;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            // ����֤�����
            string strBarcode = "";

            // ���ֶ�/���ֶ����Ӽ�¼�еõ���һ�����ֶ����ݡ�
            // parameters:
            //		strMARC	���ڸ�ʽMARC��¼
            //		strFieldName	�ֶ���������Ϊ�ַ�
            //		strSubfieldName	���ֶ���������Ϊ1�ַ�
            // return:
            //		""	���ַ�������ʾû���ҵ�ָ�����ֶλ����ֶΡ�
            //		����	���ֶ����ݡ�ע�⣬�������ֶδ����ݣ����в����������ֶ�����
            strBarcode = MarcUtil.GetFirstSubfield(strMARC,
                "100",
                "a");

            if (String.IsNullOrEmpty(strBarcode) == true)
            {
                strWarning += "MARC��¼��ȱ��100$a����֤�����; ";
            }
            else
            {
                // return:
                //      -1  error
                //      0   OK
                //      1   Invalid
                nRet = VerifyBarcode(
                    true,
                    strBarcode,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (nRet != 0)
                {
                    strWarning += "100$�еĶ���֤����� '" + strBarcode + "' ���Ϸ� -- "+strError+"; ";
                }
            }

            DomUtil.SetElementText(dom.DocumentElement, "barcode", strBarcode);


            // ����
            string strPassword = "";
            strPassword = MarcUtil.GetFirstSubfield(strMARC,
    "080",
    "a");
            if (String.IsNullOrEmpty(strPassword) == false)
            {
                try
                {
                    strPassword = Cryptography.GetSHA1(strPassword);
                }
                catch
                {
                    strError = "����������ת��ΪSHA1ʱ��������";
                    return -1;
                }

                DomUtil.SetElementText(dom.DocumentElement, "password", strPassword);
            }

            // ��������
            string strReaderType = "";
            strReaderType = MarcUtil.GetFirstSubfield(strMARC,
    "110",
    "a");

            DomUtil.SetElementText(dom.DocumentElement, "readerType", strReaderType);

            /*
            // ��֤����
            DomUtil.SetElementText(dom.DocumentElement, "createDate", strCreateDate);
             * */

            // ʧЧ��
            string strExpireDate = "";
            strExpireDate = MarcUtil.GetFirstSubfield(strMARC,
    "110",
    "d");
            if (String.IsNullOrEmpty(strExpireDate) == false)
            {
                if (strExpireDate.Length != 8)
                {
                    strWarning += "110$d�е�ʧЧ��  '" + strExpireDate + "' ӦΪ8�ַ�; ";
                }


                string strTarget = "";
                nRet = DateTimeUtil.Date8toRfc1123(strExpireDate,
                    out strTarget,
                    out strError);
                if (nRet == -1)
                {
                    strWarning += "MARC������110$d�����ַ���ת����ʽΪrfc1123ʱ��������: " + strError;
                    strExpireDate = "";
                }
                else
                {
                    strExpireDate = strTarget;
                }

                DomUtil.SetElementText(dom.DocumentElement, "expireDate", strExpireDate);
            }

            // ͣ��ԭ��
            string strState = "";
            strState = MarcUtil.GetFirstSubfield(strMARC,
    "982",
    "b");
            if (String.IsNullOrEmpty(strState) == false)
            {

                DomUtil.SetElementText(dom.DocumentElement, "state", strState);
            }

            // ����
            string strName = "";
            strName = MarcUtil.GetFirstSubfield(strMARC,
    "200",
    "a");
            if (String.IsNullOrEmpty(strName) == true)
            {
                strWarning += "MARC��¼��ȱ��200$a��������; ";
            }

            DomUtil.SetElementText(dom.DocumentElement, "name", strName);


            // ����ƴ��
            string strNamePinyin = "";
            strNamePinyin = MarcUtil.GetFirstSubfield(strMARC,
    "200",
    "A");
            if (String.IsNullOrEmpty(strNamePinyin) == false)
            {
                DomUtil.SetElementText(dom.DocumentElement, "namePinyin", strNamePinyin);
            }

            // �Ա�
            string strGender = "";
            strGender = MarcUtil.GetFirstSubfield(strMARC,
    "200",
    "b");

            DomUtil.SetElementText(dom.DocumentElement, "gender", strGender);

            /*
            // ����
            string strBirthday = "";
            strBirthday = MarcUtil.GetFirstSubfield(strMARC,
    "???",
    "?");

            DomUtil.SetElementText(dom.DocumentElement, "birthday", strBirthday);
             * */

            // ���֤��

            // ��λ
            string strDepartment = "";
            strDepartment = MarcUtil.GetFirstSubfield(strMARC,
    "300",
    "a");

            DomUtil.SetElementText(dom.DocumentElement, "department", strDepartment);

            // ��ַ
            string strAddress = "";
            strAddress = MarcUtil.GetFirstSubfield(strMARC,
    "400",
    "b");

            DomUtil.SetElementText(dom.DocumentElement, "address", strAddress);

            // ��������
            string strZipCode = "";
            strZipCode = MarcUtil.GetFirstSubfield(strMARC,
    "400",
    "a");

            DomUtil.SetElementText(dom.DocumentElement, "zipcode", strZipCode);

            // �绰
            string strTel = "";
            strTel = MarcUtil.GetFirstSubfield(strMARC,
    "300",
    "b");

            DomUtil.SetElementText(dom.DocumentElement, "tel", strTel);

            // email

            // �����ĵĸ���
            string strField986 = "";
            string strNextFieldName = "";
            // �Ӽ�¼�еõ�һ���ֶ�
            // parameters:
            //		strMARC		���ڸ�ʽMARC��¼
            //		strFieldName	�ֶ��������������==null����ʾ���ȡ�����ֶ��еĵ�nIndex��
            //		nIndex		ͬ���ֶ��еĵڼ�������0��ʼ����(�����ֶ��У����0�����ʾͷ����)
            //		strField	[out]����ֶΡ������ֶ�������Ҫ���ֶ�ָʾ�����ֶ����ݡ��������ֶν�������
            //					ע��ͷ��������һ���ֶη��أ���ʱstrField�в������ֶ���������һ��ʼ����ͷ��������
            //		strNextFieldName	[out]˳�㷵�����ҵ����ֶ�����һ���ֶε�����
            // return:
            //		-1	����
            //		0	��ָ�����ֶ�û���ҵ�
            //		1	�ҵ����ҵ����ֶη�����strField������
            nRet = MarcUtil.GetField(strMARC,
    "986",
    0,
    out strField986,
    out strNextFieldName);
            if (nRet == -1)
            {
                strError = "��MARC��¼�л��986�ֶ�ʱ����";
                return -1;
            }
            if (nRet == 1)
            {
                XmlNode nodeBorrows = dom.CreateElement("borrows");
                nodeBorrows = dom.DocumentElement.AppendChild(nodeBorrows);

                string strWarningParam = "";
                nRet = CreateBorrowsNode(nodeBorrows,
                    strField986,
                    out strWarningParam,
                    out strError);
                if (nRet == -1)
                {
                    strError = "����986�ֶ����ݴ���<borrows>�ڵ�ʱ����: " + strError;
                    return -1;
                }

                if (String.IsNullOrEmpty(strWarningParam) == false)
                    strWarning += strWarningParam + "; ";

            }


            string strField988 = "";
            // ΥԼ���¼
            nRet = MarcUtil.GetField(strMARC,
    "988",
    0,
    out strField988,
    out strNextFieldName);
            if (nRet == -1)
            {
                strError = "��MARC��¼�л��988�ֶ�ʱ����";
                return -1;
            }
            if (nRet == 1)
            {
                XmlNode nodeOverdues = dom.CreateElement("overdues");
                nodeOverdues = dom.DocumentElement.AppendChild(nodeOverdues);

                string strWarningParam = "";
                nRet = CreateOverduesNode(nodeOverdues,
                    strField988,
                    out strWarningParam,
                    out strError);
                if (nRet == -1)
                {
                    strError = "����988�ֶ����ݴ���<overdues>�ڵ�ʱ����: " + strError;
                    return -1;
                }

                if (String.IsNullOrEmpty(strWarningParam) == false)
                    strWarning += strWarningParam + "; ";

            }


            string strField984 = "";
            // ԤԼ��Ϣ
            nRet = MarcUtil.GetField(strMARC,
    "984",
    0,
    out strField984,
    out strNextFieldName);
            if (nRet == -1)
            {
                strError = "��MARC��¼�л��984�ֶ�ʱ����";
                return -1;
            }
            if (nRet == 1)
            {
                XmlNode nodeReservations = dom.CreateElement("reservations");
                nodeReservations = dom.DocumentElement.AppendChild(nodeReservations);

                string strWarningParam = "";
                nRet = CreateReservationsNode(nodeReservations,
                    strField984,
                    out strWarningParam,
                    out strError);
                if (nRet == -1)
                {
                    strError = "����984�ֶ����ݴ���<reservations>�ڵ�ʱ����: " + strError;
                    return -1;
                }

                if (String.IsNullOrEmpty(strWarningParam) == false)
                    strWarning += strWarningParam + "; ";

            }

            // �ڸ�MARC��¼�е�808$a����
            strPassword = MarcUtil.GetFirstSubfield(strMARC,
"080",
"a");
            if (String.IsNullOrEmpty(strPassword) == false)
            {
                MarcUtil.ReplaceField(ref strMARC,
                    "080",
                    0,
                    "080  " + new String(MarcUtil.SUBFLD, 1) + "a********");
            }

            // ����ԭʼ��¼���ο�
            string strPlainText = strMARC.Replace(MarcUtil.SUBFLD, '$');
            strPlainText = strPlainText.Replace(new String(MarcUtil.FLDEND, 1), "#\r\n");
            if (strPlainText.Length > 24)
                strPlainText = strPlainText.Insert(24, "\r\n");

            DomUtil.SetElementText(dom.DocumentElement, "originMARC", strPlainText);

            strXml = dom.OuterXml;

            return 0;
        }

        /*
        public static int Date8toRfc1123(string strOrigin,
out string strTarget,
out string strError)
        {
            strError = "";
            strTarget = "";

            // strOrigin = strOrigin.Replace("-", "");

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

        // ����<borrows>�ڵ���¼�����
        int CreateBorrowsNode(XmlNode nodeBorrows,
            string strField986,
            // int nEntityBarcodeLength,
            out string strWarning,
            out string strError)
        {
            strError = "";
            strWarning = "";

            int nRet = 0;

            // �������ֶ���ѭ��
            for (int g = 0; ; g++)
            {
                string strGroup = "";
                // ���ֶ��еõ����ֶ���
                // parameters:
                //		strField	�ֶΡ����а����ֶ�������Ҫ��ָʾ�����ֶ����ݡ�Ҳ���ǵ���GetField()�������õ����ֶΡ�
                //		nIndex	���ֶ�����š���0��ʼ������
                //		strGroup	[out]�õ������ֶ��顣���а����������ֶ����ݡ�
                // return:
                //		-1	����
                //		0	��ָ�������ֶ���û���ҵ�
                //		1	�ҵ����ҵ������ֶ��鷵����strGroup������
                nRet = MarcUtil.GetGroup(strField986,
                    g,
                    out strGroup);
                if (nRet == -1)
                {
                    strError = "��MARC��¼�ֶ��л�����ֶ��� " + Convert.ToString(g) + " ʱ����";
                    return -1;
                }

                if (nRet == 0)
                    break;

                string strSubfield = "";
                string strNextSubfieldName = "";

                string strBarcode = "";

                // ���ֶλ����ֶ����еõ�һ�����ֶ�
                // parameters:
                //		strText		�ֶ����ݣ��������ֶ������ݡ�
                //		textType	��ʾstrText�а��������ֶ����ݻ��������ݡ���ΪItemType.Field����ʾstrText������Ϊ�ֶΣ���ΪItemType.Group����ʾstrText������Ϊ���ֶ��顣
                //		strSubfieldName	���ֶ���������Ϊ1λ�ַ������==null����ʾ�������ֶ�
                //					��ʽΪ'a'�����ġ�
                //		nIndex			��Ҫ���ͬ�����ֶ��еĵڼ�������0��ʼ���㡣
                //		strSubfield		[out]������ֶΡ����ֶ���(1�ַ�)�����ֶ����ݡ�
                //		strNextSubfieldName	[out]��һ�����ֶε����֣�����һ���ַ�
                // return:
                //		-1	����
                //		0	��ָ�������ֶ�û���ҵ�
                //		1	�ҵ����ҵ������ֶη�����strSubfield������
                nRet = MarcUtil.GetSubfield(strGroup,
                    ItemType.Group,
                    "a",
                    0,
                    out strSubfield,
                    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strBarcode = strSubfield.Substring(1);
                }

                if (String.IsNullOrEmpty(strBarcode) == true)
                    continue;

                // return:
                //      -1  error
                //      0   OK
                //      1   Invalid
                nRet = VerifyBarcode(
                    false,
                    strBarcode,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (nRet != 0)
                {
                    strWarning += "986�ֶ��� ������� '" + strBarcode + "' ���Ϸ� -- " + strError + "; ";
                }

                XmlNode nodeBorrow = nodeBorrows.OwnerDocument.CreateElement("borrow");
                nodeBorrow = nodeBorrows.AppendChild(nodeBorrow);

                DomUtil.SetAttr(nodeBorrow, "barcode", strBarcode);

                // borrowDate����
                // ��һ�ν�������
                string strBorrowDate = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "t",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strBorrowDate = strSubfield.Substring(1);

                    if (strBorrowDate.Length != 8)
                    {
                        strWarning += "986$t���ֶ����� '" + strBorrowDate + "' �ĳ��Ȳ���8�ַ�; ";
                    }

                }

                if (String.IsNullOrEmpty(strBorrowDate) == false)
                {
                    string strTarget = "";
                    nRet = DateTimeUtil.Date8toRfc1123(strBorrowDate,
                        out strTarget,
                        out strError);
                    if (nRet == -1)
                    {
                        strWarning += "986�ֶ���$t�����ַ���ת����ʽΪrfc1123ʱ��������: " + strError;
                        strBorrowDate = "";
                    }
                    else
                    {
                        strBorrowDate = strTarget;
                    }

                }

                if (String.IsNullOrEmpty(strBorrowDate) == false)
                {
                    DomUtil.SetAttr(nodeBorrow, "borrowDate", strBorrowDate);
                }

                // no����
                // ��ʲô���ֿ�ʼ������
                string strNo = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "y",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strNo = strSubfield.Substring(1);
                }

                if (String.IsNullOrEmpty(strNo) == false)
                {
                    DomUtil.SetAttr(nodeBorrow, "no", strNo);
                }




                // borrowPeriod����

                // ����Ӧ�����ڼ������?

                // Ӧ������
                string strReturnDate = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "v",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strReturnDate = strSubfield.Substring(1);

                }

                if (String.IsNullOrEmpty(strReturnDate) == false)
                {
                    if (strReturnDate.Length != 8)
                    {
                        strWarning += "986$v���ֶ����� '" + strReturnDate + "' �ĳ��Ȳ���8�ַ�; ";
                    }
                }
                else
                {
                    if (strBorrowDate != "")
                    {
                        strWarning += "986�ֶ������ֶ��� " + Convert.ToString(g + 1) + " �� $t ���ֶ����ݶ�û�� $v ���ֶ�����, ������; ";
                    }
                }

                if (String.IsNullOrEmpty(strReturnDate) == false)
                {
                    string strTarget = "";
                    nRet = DateTimeUtil.Date8toRfc1123(strReturnDate,
                        out strTarget,
                        out strError);
                    if (nRet == -1)
                    {
                        strWarning += "986�ֶ���$v�����ַ���ת����ʽΪrfc1123ʱ��������: " + strError;
                        strReturnDate = "";
                    }
                    else
                    {
                        strReturnDate = strTarget;
                    }

                }

                if (String.IsNullOrEmpty(strReturnDate) == false
                    && String.IsNullOrEmpty(strBorrowDate) == false)
                {
                    // ����������
                    DateTime timestart = DateTimeUtil.FromRfc1123DateTimeString(strBorrowDate);
                    DateTime timeend = DateTimeUtil.FromRfc1123DateTimeString(strReturnDate);

                    TimeSpan delta = timeend - timestart;

                    string strBorrowPeriod = Convert.ToString(delta.TotalDays) + "day";
                    DomUtil.SetAttr(nodeBorrow, "borrowPeriod", strBorrowPeriod);
                }

                // ���������
                if (strNo != "")
                {
                    string strRenewDate = "";
                    nRet = MarcUtil.GetSubfield(strGroup,
                        ItemType.Group,
                        "x",
                        0,
                        out strSubfield,
                        out strNextSubfieldName);
                    if (strSubfield.Length >= 1)
                    {
                        strRenewDate = strSubfield.Substring(1);

                        if (strRenewDate.Length != 8)
                        {
                            strWarning += "986$x���ֶ����� '" + strRenewDate + "' �ĳ��Ȳ���8�ַ�; ";
                        }

                    }

                    if (String.IsNullOrEmpty(strRenewDate) == false)
                    {
                        string strTarget = "";
                        nRet = DateTimeUtil.Date8toRfc1123(strRenewDate,
                            out strTarget,
                            out strError);
                        if (nRet == -1)
                        {
                            strWarning += "986�ֶ���$x�����ַ���ת����ʽΪrfc1123ʱ��������: " + strError;
                            strRenewDate = "";
                        }
                        else
                        {
                            strRenewDate = strTarget;
                        }

                    }

                    if (String.IsNullOrEmpty(strRenewDate) == false)
                    {
                        DomUtil.SetAttr(nodeBorrow, "borrowDate", strRenewDate);
                    }

                    if (String.IsNullOrEmpty(strRenewDate) == false
    && String.IsNullOrEmpty(strBorrowDate) == false)
                    {
                        // ���¼���������
                        DateTime timestart = DateTimeUtil.FromRfc1123DateTimeString(strRenewDate);
                        DateTime timeend = DateTimeUtil.FromRfc1123DateTimeString(strReturnDate);

                        TimeSpan delta = timeend - timestart;

                        string strBorrowPeriod = Convert.ToString(delta.TotalDays) + "day";
                        DomUtil.SetAttr(nodeBorrow, "borrowPeriod", strBorrowPeriod);
                    }

                }


            }

            return 0;
        }


        // ����<overdues>�ڵ���¼�����
        int CreateOverduesNode(XmlNode nodeOverdues,
            string strField988,
            // int nEntityBarcodeLength,
            out string strWarning,
            out string strError)
        {
            strError = "";
            strWarning = "";

            int nRet = 0;

            // �������ֶ���ѭ��
            for (int g = 0; ; g++)
            {
                string strGroup = "";
                // ���ֶ��еõ����ֶ���
                // parameters:
                //		strField	�ֶΡ����а����ֶ�������Ҫ��ָʾ�����ֶ����ݡ�Ҳ���ǵ���GetField()�������õ����ֶΡ�
                //		nIndex	���ֶ�����š���0��ʼ������
                //		strGroup	[out]�õ������ֶ��顣���а����������ֶ����ݡ�
                // return:
                //		-1	����
                //		0	��ָ�������ֶ���û���ҵ�
                //		1	�ҵ����ҵ������ֶ��鷵����strGroup������
                nRet = MarcUtil.GetGroup(strField988,
                    g,
                    out strGroup);
                if (nRet == -1)
                {
                    strError = "��MARC��¼�ֶ��л�����ֶ��� " + Convert.ToString(g) + " ʱ����";
                    return -1;
                }

                if (nRet == 0)
                    break;

                string strSubfield = "";
                string strNextSubfieldName = "";

                string strBarcode = "";

                // ���ֶλ����ֶ����еõ�һ�����ֶ�
                // parameters:
                //		strText		�ֶ����ݣ��������ֶ������ݡ�
                //		textType	��ʾstrText�а��������ֶ����ݻ��������ݡ���ΪItemType.Field����ʾstrText������Ϊ�ֶΣ���ΪItemType.Group����ʾstrText������Ϊ���ֶ��顣
                //		strSubfieldName	���ֶ���������Ϊ1λ�ַ������==null����ʾ�������ֶ�
                //					��ʽΪ'a'�����ġ�
                //		nIndex			��Ҫ���ͬ�����ֶ��еĵڼ�������0��ʼ���㡣
                //		strSubfield		[out]������ֶΡ����ֶ���(1�ַ�)�����ֶ����ݡ�
                //		strNextSubfieldName	[out]��һ�����ֶε����֣�����һ���ַ�
                // return:
                //		-1	����
                //		0	��ָ�������ֶ�û���ҵ�
                //		1	�ҵ����ҵ������ֶη�����strSubfield������
                nRet = MarcUtil.GetSubfield(strGroup,
                    ItemType.Group,
                    "a",
                    0,
                    out strSubfield,
                    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strBarcode = strSubfield.Substring(1);
                }

                if (String.IsNullOrEmpty(strBarcode) == true)
                    continue;

                // return:
                //      -1  error
                //      0   OK
                //      1   Invalid
                nRet = VerifyBarcode(
                    false,
                    strBarcode,
                    out strError);
                if (nRet == -1)
                    return -1;


                if (nRet != 0)
                {
                    strWarning += "988�ֶ��� ������� '" + strBarcode + "' ���Ϸ� -- " + strError + "; ";
                }

                string strCompleteDate = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "d",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strCompleteDate = strSubfield.Substring(1);
                }

                if (String.IsNullOrEmpty(strCompleteDate) == false)
                    continue; // ����Ѿ����˷���������ֶ���ͺ�����

                XmlNode nodeOverdue = nodeOverdues.OwnerDocument.CreateElement("overdue");
                nodeOverdue = nodeOverdues.AppendChild(nodeOverdue);

                DomUtil.SetAttr(nodeOverdue, "barcode", strBarcode);

                // borrowDate����
                string strBorrowDate = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "e",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strBorrowDate = strSubfield.Substring(1);

                    if (strBorrowDate.Length != 8)
                    {
                        strWarning += "988$e���ֶ����� '" + strBorrowDate + "' �ĳ��Ȳ���8�ַ�; ";
                    }
                }

                if (String.IsNullOrEmpty(strBorrowDate) == false)
                {
                    string strTarget = "";
                    nRet = DateTimeUtil.Date8toRfc1123(strBorrowDate,
                        out strTarget,
                        out strError);
                    if (nRet == -1)
                    {
                        strWarning += "988�ֶ���$e�����ַ���ת����ʽΪrfc1123ʱ��������: " + strError;
                        strBorrowDate = "";
                    }
                    else
                    {
                        strBorrowDate = strTarget;
                    }

                }

                if (String.IsNullOrEmpty(strBorrowDate) == false)
                {
                    DomUtil.SetAttr(nodeOverdue, "borrowDate", strBorrowDate);
                }

                // returnDate����
                string strReturnDate = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "t",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strReturnDate = strSubfield.Substring(1);

                    if (strReturnDate.Length != 8)
                    {
                        strWarning += "988$t���ֶ����� '" + strReturnDate + "' �ĳ��Ȳ���8�ַ�; ";
                    }
                }

                if (String.IsNullOrEmpty(strReturnDate) == false)
                {
                    string strTarget = "";
                    nRet = DateTimeUtil.Date8toRfc1123(strReturnDate,
                        out strTarget,
                        out strError);
                    if (nRet == -1)
                    {
                        strWarning += "988�ֶ���$t�����ַ���ת����ʽΪrfc1123ʱ��������: " + strError;
                        strReturnDate = "";
                    }
                    else
                    {
                        strReturnDate = strTarget;
                    }

                }

                if (String.IsNullOrEmpty(strReturnDate) == false)
                {
                    DomUtil.SetAttr(nodeOverdue, "returnDate", strReturnDate);  // 2006/12/29 changed
                }

                // borrowPeriodδ֪
                //   DomUtil.SetAttr(nodeOverdue, "borrowPeriod", strBorrowPeriod);

                // price��type������Ϊ����dt1000���ݶ�����������
                // ��over�����������ԾͿ�ȱ��

                // price����
                string strPrice = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "c",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strPrice = strSubfield.Substring(1);
                }

                if (String.IsNullOrEmpty(strPrice) == false)
                {
                    // �Ƿ���Ҫת��Ϊ�����ҵ�λ��, ��С�����ֵ��ַ���?

                    DomUtil.SetAttr(nodeOverdue, "price", strPrice);
                }

                // type����
                string strType = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "b",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strType = strSubfield.Substring(1);
                }

                if (String.IsNullOrEmpty(strType) == false)
                {
                    DomUtil.SetAttr(nodeOverdue, "type", strType);
                }

                // 2007/9/27 new add
                DomUtil.SetAttr(nodeOverdue, "id", "upgrade-" + this.App.GetOverdueID());   // 2008/2/8 new add "upgrade-"
            }

            return 0;
        }


        // ����<reservations>�ڵ���¼�����
        // �������ݣ�
        // 1)���ʵ����Ѿ����ڣ�������Ҫ������ز���ʵ���Ĵ��롣
        // Ҳ����ר����һ�����߼�¼��ʵ���¼�����޸ĵĽ׶Σ��������໥�Ĺ�ϵ
        // 2)��ʱû�д����ѵ���ԤԼ�����Ϣ�������ܣ����Ƕ�������Щ��Ϣ
        int CreateReservationsNode(XmlNode nodeReservations,
            string strField984,
            // int nEntityBarcodeLength,
            out string strWarning,
            out string strError)
        {
            strError = "";
            strWarning = "";

            int nRet = 0;

            // �������ֶ���ѭ��
            for (int g = 0; ; g++)
            {
                string strGroup = "";
                // ���ֶ��еõ����ֶ���
                // parameters:
                //		strField	�ֶΡ����а����ֶ�������Ҫ��ָʾ�����ֶ����ݡ�Ҳ���ǵ���GetField()�������õ����ֶΡ�
                //		nIndex	���ֶ�����š���0��ʼ������
                //		strGroup	[out]�õ������ֶ��顣���а����������ֶ����ݡ�
                // return:
                //		-1	����
                //		0	��ָ�������ֶ���û���ҵ�
                //		1	�ҵ����ҵ������ֶ��鷵����strGroup������
                nRet = MarcUtil.GetGroup(strField984,
                    g,
                    out strGroup);
                if (nRet == -1)
                {
                    strError = "��MARC��¼�ֶ��л�����ֶ��� " + Convert.ToString(g) + " ʱ����";
                    return -1;
                }

                if (nRet == 0)
                    break;

                string strSubfield = "";
                string strNextSubfieldName = "";

                string strBarcode = "";

                // ���ֶλ����ֶ����еõ�һ�����ֶ�
                // parameters:
                //		strText		�ֶ����ݣ��������ֶ������ݡ�
                //		textType	��ʾstrText�а��������ֶ����ݻ��������ݡ���ΪItemType.Field����ʾstrText������Ϊ�ֶΣ���ΪItemType.Group����ʾstrText������Ϊ���ֶ��顣
                //		strSubfieldName	���ֶ���������Ϊ1λ�ַ������==null����ʾ�������ֶ�
                //					��ʽΪ'a'�����ġ�
                //		nIndex			��Ҫ���ͬ�����ֶ��еĵڼ�������0��ʼ���㡣
                //		strSubfield		[out]������ֶΡ����ֶ���(1�ַ�)�����ֶ����ݡ�
                //		strNextSubfieldName	[out]��һ�����ֶε����֣�����һ���ַ�
                // return:
                //		-1	����
                //		0	��ָ�������ֶ�û���ҵ�
                //		1	�ҵ����ҵ������ֶη�����strSubfield������
                nRet = MarcUtil.GetSubfield(strGroup,
                    ItemType.Group,
                    "a",
                    0,
                    out strSubfield,
                    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strBarcode = strSubfield.Substring(1);
                }

                if (String.IsNullOrEmpty(strBarcode) == true)
                    continue;

                // return:
                //      -1  error
                //      0   OK
                //      1   Invalid
                nRet = VerifyBarcode(
                    false,
                    strBarcode,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (nRet != 0)
                {
                    strWarning += "984�ֶ��� ������� '" + strBarcode + "' ���Ϸ� -- " + strError + "; ";
                }

                string strArriveDate = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "c",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strArriveDate = strSubfield.Substring(1);
                }

                if (String.IsNullOrEmpty(strArriveDate) == false)
                    continue; // ����Ѿ����飬������ֶ���ͺ�����

                XmlNode nodeRequest = nodeReservations.OwnerDocument.CreateElement("request");
                nodeRequest = nodeReservations.AppendChild(nodeRequest);

                DomUtil.SetAttr(nodeRequest, "items", strBarcode);

                // requestDate����
                string strRequestDate = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "b",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strRequestDate = strSubfield.Substring(1);

                    if (strRequestDate.Length != 8)
                    {
                        strWarning += "984$b���ֶ����� '" + strRequestDate + "' �ĳ��Ȳ���8�ַ�; ";
                    }
                }

                if (String.IsNullOrEmpty(strRequestDate) == false)
                {
                    string strTarget = "";
                    nRet = DateTimeUtil.Date8toRfc1123(strRequestDate,
                        out strTarget,
                        out strError);
                    if (nRet == -1)
                    {
                        strWarning += "984�ֶ���$b�����ַ���ת����ʽΪrfc1123ʱ��������: " + strError;
                        strRequestDate = "";
                    }
                    else
                    {
                        strRequestDate = strTarget;
                    }

                }

                if (String.IsNullOrEmpty(strRequestDate) == false)
                {
                    DomUtil.SetAttr(nodeRequest, "requestDate", strRequestDate);
                }

            }

            return 0;
        }

        #endregion


        #region ����dt1000/dt1500��Ŀ���ݵ�dp2ʵ�����ع���

        // ��һ��MARC��¼�а�����ʵ����Ϣ���XML��ʽ���ϴ�
        // parameters:
        //      strEntityDbName ʵ�����ݿ���
        //      strParentRecordID   ����¼ID
        //      strMARC ����¼MARC
        int DoEntityRecordsUpload(
            RmsChannel channel,
            string strEntityDbName,
            string strParentRecordID,
            string strMARC,
            /*
            int nEntityBarcodeLength,
            int nReaderBarcodeLength,
             * */
            out int nThisEntityCount,
            out string strWarning,
            out string strError)
        {
            strError = "";
            strWarning = "";
            nThisEntityCount = 0;

            int nRet = 0;

            string strField906 = "";
            string strField986 = "";
            string strNextFieldName = "";

            string strWarningParam = "";

            // �淶��parent id��ȥ��ǰ���'0'
            strParentRecordID = strParentRecordID.TrimStart(new char[] {'0'});
            if (String.IsNullOrEmpty(strParentRecordID) == true)
                strParentRecordID = "0";


            // ���906�ֶ�

            nRet = MarcUtil.GetField(strMARC,
                "906",
                0,
                out strField906,
                out strNextFieldName);
            if (nRet == -1)
            {
                strError = "��MARC��¼�л��906�ֶ�ʱ����";
                return -1;
            }
            if (nRet == 0)
                strField906 = "";

            // ���986�ֶ�



            // �Ӽ�¼�еõ�һ���ֶ�
            // parameters:
            //		strMARC		���ڸ�ʽMARC��¼
            //		strFieldName	�ֶ��������������==null����ʾ���ȡ�����ֶ��еĵ�nIndex��
            //		nIndex		ͬ���ֶ��еĵڼ�������0��ʼ����(�����ֶ��У����0�����ʾͷ����)
            //		strField	[out]����ֶΡ������ֶ�������Ҫ���ֶ�ָʾ�����ֶ����ݡ��������ֶν�������
            //					ע��ͷ��������һ���ֶη��أ���ʱstrField�в������ֶ���������һ��ʼ����ͷ��������
            //		strNextFieldName	[out]˳�㷵�����ҵ����ֶ�����һ���ֶε�����
            // return:
            //		-1	����
            //		0	��ָ�����ֶ�û���ҵ�
            //		1	�ҵ����ҵ����ֶη�����strField������
            nRet = MarcUtil.GetField(strMARC,
                "986",
                0,
                out strField986,
                out strNextFieldName);
            if (nRet == -1)
            {
                strError = "��MARC��¼�л��986�ֶ�ʱ����";
                return -1;
            }

            if (nRet == 0)
            {
                // return 0;   // û���ҵ�986�ֶ�
                strField986 = "";
            }
            else
            {


                // ����986�ֶ�����
                if (strField986.Length <= 5 + 2)
                    strField986 = "";
                else
                {
                    string strPart = strField986.Substring(5, 2);

                    string strDollarA = new string(MarcUtil.SUBFLD, 1) + "a";

                    if (strPart != strDollarA)
                    {
                        strField986 = strField986.Insert(5, strDollarA);
                    }
                }

            }

            List<Group> groups = null;

            // �ϲ�906��986�ֶ�����
            nRet = MergField906and986(strField906,
            strField986,
            out groups,
            out strWarningParam,
            out strError);
            if (nRet == -1)
                return -1;

            if (String.IsNullOrEmpty(strWarningParam) == false)
                strWarning += strWarningParam + "; ";

            // �������ֶ���ѭ��
            for (int g = 0; g < groups.Count; g++)
            {
                Group group = groups[g];

                string strGroup = group.strValue;

                // ����һ��item

                string strXml = "";

                // ����ʵ��XML��¼
                // parameters:
                //      strParentID ����¼ID
                //      strGroup    ��ת����ͼ���ּ�¼��986�ֶ���ĳ���ֶ���Ƭ��
                //      strXml      �����ʵ��XML��¼
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = BuildEntityXmlRecord(strParentRecordID,
                    strGroup,
                    strMARC,
                    group.strMergeComment,
                    // nReaderBarcodeLength,
                    out strXml,
                    out strWarningParam,
                    out strError);
                if (nRet == -1)
                {
                    strError = "������¼id " + strParentRecordID + " ֮ʵ��(���) " + Convert.ToString(g + 1) + "ʱ��������: " + strError;
                    return -1;
                }

                if (String.IsNullOrEmpty(strWarningParam) == false)
                    strWarning += strWarningParam + "; ";

                byte[] timestamp = null;
                byte[] output_timestamp = null;
                string strOutputPath = "";
                string strTargetPath = strEntityDbName + "/?";

                // RmsChannel channel = this.MainForm.Channels.GetChannel(strServerUrl);

                // ����Xml��¼
                long lRet = channel.DoSaveTextRes(strTargetPath,
                    strXml,
                    false,	// bIncludePreamble
                    "",//strStyle,
                    timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);

                if (lRet == -1)
                {
                    return -1;
                }

                nThisEntityCount++;

            }

            return 0;
        }

        // ����ʵ��XML��¼
        // parameters:
        //      strParentID ����¼ID
        //      strGroup    ��ת����ͼ���ּ�¼��986�ֶ���ĳ���ֶ���Ƭ��
        //      strXml      �����ʵ��XML��¼
        // return:
        //      -1  ����
        //      0   �ɹ�
        int BuildEntityXmlRecord(string strParentID,
            string strGroup,
            string strMARC,
            string strMergeComment,
            // int nReaderBarcodeLength,
            out string strXml,
            out string strWarning,
            out string strError)
        {
            strXml = "";
            strWarning = "";
            strError = "";

            // 
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            // ����¼id
            DomUtil.SetElementText(dom.DocumentElement, "parent", strParentID);

            // �������

            string strSubfield = "";
            string strNextSubfieldName = "";
            // ���ֶλ����ֶ����еõ�һ�����ֶ�
            // parameters:
            //		strText		�ֶ����ݣ��������ֶ������ݡ�
            //		textType	��ʾstrText�а��������ֶ����ݻ��������ݡ���ΪItemType.Field����ʾstrText������Ϊ�ֶΣ���ΪItemType.Group����ʾstrText������Ϊ���ֶ��顣
            //		strSubfieldName	���ֶ���������Ϊ1λ�ַ������==null����ʾ�������ֶ�
            //					��ʽΪ'a'�����ġ�
            //		nIndex			��Ҫ���ͬ�����ֶ��еĵڼ�������0��ʼ���㡣
            //		strSubfield		[out]������ֶΡ����ֶ���(1�ַ�)�����ֶ����ݡ�
            //		strNextSubfieldName	[out]��һ�����ֶε����֣�����һ���ַ�
            // return:
            //		-1	����
            //		0	��ָ�������ֶ�û���ҵ�
            //		1	�ҵ����ҵ������ֶη�����strSubfield������
            int nRet = MarcUtil.GetSubfield(strGroup,
                ItemType.Group,
                "a",
                0,
                out strSubfield,
                out strNextSubfieldName);
            if (strSubfield.Length >= 1)
            {
                string strBarcode = strSubfield.Substring(1);

                DomUtil.SetElementText(dom.DocumentElement, "barcode", strBarcode);
            }


            // ��¼��
            nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "h",
    0,
    out strSubfield,
    out strNextSubfieldName);
            if (strSubfield.Length >= 1)
            {
                string strRegisterNo = strSubfield.Substring(1);

                if (String.IsNullOrEmpty(strRegisterNo) == false)
                {
                    DomUtil.SetElementText(dom.DocumentElement, "registerNo", strRegisterNo);
                }
            }



            // ״̬?
            DomUtil.SetElementText(dom.DocumentElement, "state", "");

            // �ݲصص�

            nRet = MarcUtil.GetSubfield(strGroup,
                ItemType.Group,
                "b",
                0,
                out strSubfield,
                out strNextSubfieldName);
            if (strSubfield.Length >= 1)
            {
                string strLocation = strSubfield.Substring(1);

                DomUtil.SetElementText(dom.DocumentElement, "location", strLocation);
            }

            // �۸�
            // �������ֶ����е�$d �Ҳ�������982$b

            nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "d",
    0,
    out strSubfield,
    out strNextSubfieldName);
            string strPrice = "";

            if (strSubfield.Length >= 1)
            {
                strPrice = strSubfield.Substring(1);
            }

            // �����$d�л�õļ۸�����Ϊ�գ����982$b�л��
            if (String.IsNullOrEmpty(strPrice) == true)
            {
                // ���ֶ�/���ֶ����Ӽ�¼�еõ���һ�����ֶ����ݡ�
                // parameters:
                //		strMARC	���ڸ�ʽMARC��¼
                //		strFieldName	�ֶ���������Ϊ�ַ�
                //		strSubfieldName	���ֶ���������Ϊ1�ַ�
                // return:
                //		""	���ַ�������ʾû���ҵ�ָ�����ֶλ����ֶΡ�
                //		����	���ֶ����ݡ�ע�⣬�������ֶδ����ݣ����в����������ֶ�����
                strPrice = MarcUtil.GetFirstSubfield(strMARC,
                    "982",
                    "b");
            }

            DomUtil.SetElementText(dom.DocumentElement, "price", strPrice);

            // ͼ�������
            // ���������$f ���û�У�����982$a?
            nRet = MarcUtil.GetSubfield(strGroup,
ItemType.Group,
"f",
0,
out strSubfield,
out strNextSubfieldName);
            string strBookType = "";
            if (strSubfield.Length >= 1)
            {
                strBookType = strSubfield.Substring(1);
            }

            // �����$f�л�õĲ�����Ϊ�գ����982$a�л��
            if (String.IsNullOrEmpty(strBookType) == true)
            {
                // ���ֶ�/���ֶ����Ӽ�¼�еõ���һ�����ֶ����ݡ�
                // parameters:
                //		strMARC	���ڸ�ʽMARC��¼
                //		strFieldName	�ֶ���������Ϊ�ַ�
                //		strSubfieldName	���ֶ���������Ϊ1�ַ�
                // return:
                //		""	���ַ�������ʾû���ҵ�ָ�����ֶλ����ֶΡ�
                //		����	���ֶ����ݡ�ע�⣬�������ֶδ����ݣ����в����������ֶ�����
                strBookType = MarcUtil.GetFirstSubfield(strMARC,
                    "982",
                    "a");
            }

            DomUtil.SetElementText(dom.DocumentElement, "bookType", strBookType);

            // ע��
            nRet = MarcUtil.GetSubfield(strGroup,
ItemType.Group,
"z",
0,
out strSubfield,
out strNextSubfieldName);
            string strComment = "";
            if (strSubfield.Length >= 1)
            {
                strComment = strSubfield.Substring(1);
            }

            if (String.IsNullOrEmpty(strComment) == false)
            {
                DomUtil.SetElementText(dom.DocumentElement, "comment", strComment);
            }

            // ������
            nRet = MarcUtil.GetSubfield(strGroup,
ItemType.Group,
"r",
0,
out strSubfield,
out strNextSubfieldName);
            string strBorrower = "";
            if (strSubfield.Length >= 1)
            {
                strBorrower = strSubfield.Substring(1);
            }

            if (String.IsNullOrEmpty(strBorrower) == false)
            {
                // return:
                //      -1  error
                //      0   OK
                //      1   Invalid
                nRet = VerifyBarcode(
                    true,
                    strBorrower,
                    out strError);
                if (nRet == -1)
                    return -1;


                // �������ų���
                if (nRet != 0)
                {
                    strWarning += "$r�ж���֤����� '" + strBorrower + "' ���Ϸ� -- " + strError + "; ";
                }

                DomUtil.SetElementText(dom.DocumentElement, "borrower", strBorrower);
            }

            // ��������
            nRet = MarcUtil.GetSubfield(strGroup,
ItemType.Group,
"t",
0,
out strSubfield,
out strNextSubfieldName);
            string strBorrowDate = "";
            if (strSubfield.Length >= 1)
            {
                strBorrowDate = strSubfield.Substring(1);

                // ��ʽΪ 20060625�� ��Ҫת��Ϊrfc
                if (strBorrowDate.Length == 8)
                {
                    /*
                    IFormatProvider culture = new CultureInfo("zh-CN", true);

                    DateTime time;
                    try
                    {
                        time = DateTime.ParseExact(strBorrowDate, "yyyyMMdd", culture);
                    }
                    catch
                    {
                        strError = "���ֶ�����$t�����еĽ������� '" + strBorrowDate + "' �ַ���ת��ΪDateTime����ʱ����";
                        return -1;
                    }

                    time = time.ToUniversalTime();
                    strBorrowDate = DateTimeUtil.Rfc1123DateTimeString(time);
                     * */

                    string strTarget = "";

                    nRet = DateTimeUtil.Date8toRfc1123(strBorrowDate,
                    out strTarget,
                    out strError);
                    if (nRet == -1)
                    {
                        strWarning += "���ֶ�����$t�����еĽ������� '" + strBorrowDate + "' ��ʽ����: " + strError;
                        strBorrowDate = "";
                    }
                    else
                    {
                        strBorrowDate = strTarget;
                    }

                }
                else if (String.IsNullOrEmpty(strBorrowDate) == false)
                {
                    strWarning += "$t������ֵ '" + strBorrowDate + "' ��ʽ���󣬳���ӦΪ8�ַ� ";
                    strBorrowDate = "";
                }
            }

            if (String.IsNullOrEmpty(strBorrowDate) == false)
            {
                DomUtil.SetElementText(dom.DocumentElement, "borrowDate", strBorrowDate);
            }

            // ��������
            if (String.IsNullOrEmpty(strBorrowDate) == false)
            {
                DomUtil.SetElementText(dom.DocumentElement, "borrowPeriod", "1day"); // �����Ե�Ϊ1�졣��Ϊ<borrowDate>�е�ֵʵ��ΪӦ������
            }

            // ����ע��
            if (String.IsNullOrEmpty(strMergeComment) == false)
            {
                DomUtil.SetElementText(dom.DocumentElement, "mergeComment", strMergeComment);
            }

            // ״̬
            nRet = MarcUtil.GetSubfield(strGroup,
                ItemType.Group,
                "s",
                0,
                out strSubfield,
                out strNextSubfieldName);
            string strState = "";
            if (strSubfield.Length >= 1)
            {
                strState = strSubfield.Substring(1);
            }

            if (String.IsNullOrEmpty(strState) == false)
            {
                DomUtil.SetElementText(dom.DocumentElement, "state", strState);
            }

            strXml = dom.OuterXml;

            return 0;
        }

        // ���һ�����ֶ��������
        public class Group
        {
            public string strBarcode = "";
            public string strRegisterNo = "";
            public string strValue = "";
            public string strMergeComment = ""; // �ϲ�����ϸ��ע��

            // ����һGroup�����кϲ���Ҫ�����ֶ�ֵ����
            // 2008/4/14 new add
            public void MergeValue(Group group)
            {
                int nRet = 0;
                string strSubfieldNames = "b";  // ���ɸ���Ҫ�ϲ������ֶ���

                for (int i = 0; i < strSubfieldNames.Length; i++)
                {
                    char subfieldname = strSubfieldNames[i];

                    string strSubfieldName = new string (subfieldname, 1);

                    string strSubfield = "";
                    string strNextSubfieldName = "";

                    string strValue = "";

                    // ���ֶλ����ֶ����еõ�һ�����ֶ�
                    // parameters:
                    //		strText		�ֶ����ݣ��������ֶ������ݡ�
                    //		textType	��ʾstrText�а��������ֶ����ݻ��������ݡ���ΪItemType.Field����ʾstrText������Ϊ�ֶΣ���ΪItemType.Group����ʾstrText������Ϊ���ֶ��顣
                    //		strSubfieldName	���ֶ���������Ϊ1λ�ַ������==null����ʾ�������ֶ�
                    //					��ʽΪ'a'�����ġ�
                    //		nIndex			��Ҫ���ͬ�����ֶ��еĵڼ�������0��ʼ���㡣
                    //		strSubfield		[out]������ֶΡ����ֶ���(1�ַ�)�����ֶ����ݡ�
                    //		strNextSubfieldName	[out]��һ�����ֶε����֣�����һ���ַ�
                    // return:
                    //		-1	����
                    //		0	��ָ�������ֶ�û���ҵ�
                    //		1	�ҵ����ҵ������ֶη�����strSubfield������
                    nRet = MarcUtil.GetSubfield(this.strValue,
                        ItemType.Group,
                        strSubfieldName,
                        0,
                        out strSubfield,
                        out strNextSubfieldName);
                    if (strSubfield.Length >= 1)
                    {
                        strValue = strSubfield.Substring(1).Trim();   // ȥ�����Ҷ���Ŀհ�
                    }

                    // ���Ϊ�գ�����Ҫ��������
                    if (String.IsNullOrEmpty(strValue) == true)
                    {
                        string strOtherValue = "";

                        strSubfield = "";
                        nRet = MarcUtil.GetSubfield(group.strValue,
                            ItemType.Group,
                            strSubfieldName,
                            0,
                            out strSubfield,
                            out strNextSubfieldName);
                        if (strSubfield.Length >= 1)
                        {
                            strOtherValue = strSubfield.Substring(1).Trim();   // ȥ�����Ҷ���Ŀհ�
                        }

                        if (String.IsNullOrEmpty(strOtherValue) == false)
                        {
                            // �滻�ֶ��е����ֶΡ�
                            // parameters:
                            //		strField	[in,out]���滻���ֶ�
                            //		strSubfieldName	Ҫ�滻�����ֶε���������Ϊ1�ַ������==null����ʾ�������ֶ�
                            //					��ʽΪ'a'�����ġ�
                            //		nIndex		Ҫ�滻�����ֶ�������š����Ϊ-1����ʼ��Ϊ���ֶ���׷�������ֶ����ݡ�
                            //		strSubfield	Ҫ�滻�ɵ������ֶΡ�ע�⣬���е�һ�ַ�Ϊ���ֶ���������Ϊ���ֶ�����
                            // return:
                            //		-1	����
                            //		0	ָ�������ֶ�û���ҵ�����˽�strSubfieldzhogn�����ݲ��뵽�ʵ��ط��ˡ�
                            //		1	�ҵ���ָ�����ֶΣ�����Ҳ�ɹ���strSubfield�����滻���ˡ�
                            nRet = MarcUtil.ReplaceSubfield(ref this.strValue,
                                strSubfieldName,
                                0,
                                strSubfieldName + strOtherValue);
                        }
                    }
                }


            }
        }

        // ����һ��MARC�ֶΣ�����Group����
        public int BuildGroups(string strField,
            // int nEntityBarcodeLength,
            out List<Group> groups,
            out string strWarning,
            out string strError)
        {
            strError = "";
            strWarning = "";
            groups = new List<Group>();
            int nRet = 0;

            // �������ֶ���ѭ��
            for (int g = 0; ; g++)
            {
                string strGroup = "";
                // ���ֶ��еõ����ֶ���
                // parameters:
                //		strField	�ֶΡ����а����ֶ�������Ҫ��ָʾ�����ֶ����ݡ�Ҳ���ǵ���GetField()�������õ����ֶΡ�
                //		nIndex	���ֶ�����š���0��ʼ������
                //		strGroup	[out]�õ������ֶ��顣���а����������ֶ����ݡ�
                // return:
                //		-1	����
                //		0	��ָ�������ֶ���û���ҵ�
                //		1	�ҵ����ҵ������ֶ��鷵����strGroup������
                nRet = MarcUtil.GetGroup(strField,
                    g,
                    out strGroup);
                if (nRet == -1)
                {
                    strError = "��MARC��¼�ֶ��л�����ֶ��� " + Convert.ToString(g) + " ʱ����";
                    return -1;
                }

                if (nRet == 0)
                    break;

                // �������

                string strSubfield = "";
                string strNextSubfieldName = "";

                string strBarcode = "";
                string strRegisterNo = "";

                // ���ֶλ����ֶ����еõ�һ�����ֶ�
                // parameters:
                //		strText		�ֶ����ݣ��������ֶ������ݡ�
                //		textType	��ʾstrText�а��������ֶ����ݻ��������ݡ���ΪItemType.Field����ʾstrText������Ϊ�ֶΣ���ΪItemType.Group����ʾstrText������Ϊ���ֶ��顣
                //		strSubfieldName	���ֶ���������Ϊ1λ�ַ������==null����ʾ�������ֶ�
                //					��ʽΪ'a'�����ġ�
                //		nIndex			��Ҫ���ͬ�����ֶ��еĵڼ�������0��ʼ���㡣
                //		strSubfield		[out]������ֶΡ����ֶ���(1�ַ�)�����ֶ����ݡ�
                //		strNextSubfieldName	[out]��һ�����ֶε����֣�����һ���ַ�
                // return:
                //		-1	����
                //		0	��ָ�������ֶ�û���ҵ�
                //		1	�ҵ����ҵ������ֶη�����strSubfield������
                nRet = MarcUtil.GetSubfield(strGroup,
                    ItemType.Group,
                    "a",
                    0,
                    out strSubfield,
                    out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strBarcode = strSubfield.Substring(1).Trim();   // ȥ�����Ҷ���Ŀհ�
                }

                if (String.IsNullOrEmpty(strBarcode) == false)
                {
                    // ȥ����ߵ�'*'�� 2006/9/2 add
                    if (strBarcode[0] == '*')
                        strBarcode = strBarcode.Substring(1);

                    // return:
                    //      -1  error
                    //      0   OK
                    //      1   Invalid
                    nRet = VerifyBarcode(
                        false,
                        strBarcode,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // ��������ų���
                    if (nRet != 0)
                    {
                        strWarning += "������� '" + strBarcode + "' ���Ϸ� -- " + strError + "; ";
                    }
                }


                // ��¼��
                nRet = MarcUtil.GetSubfield(strGroup,
        ItemType.Group,
        "h",
        0,
        out strSubfield,
        out strNextSubfieldName);
                if (strSubfield.Length >= 1)
                {
                    strRegisterNo = strSubfield.Substring(1);
                }

                // TODO: ��Ҫ�������¼�ų��ȵĴ���


                Group group = new Group();
                group.strValue = strGroup;
                group.strBarcode = strBarcode;
                group.strRegisterNo = strRegisterNo;

                groups.Add(group);
            }

            return 0;
        }
        // �ϲ�906��986�ֶ�����
        int MergField906and986(string strField906,
            string strField986,
            // int nEntityBarcodeLength,
            out List<Group> groups,
            out string strWarning,
            out string strError)
        {
            strError = "";
            groups = null;
            strError = "";
            strWarning = "";

            int nRet = 0;

            List<Group> groups_906 = null;
            List<Group> groups_986 = null;

            string strWarningParam = "";

            nRet = BuildGroups(strField906,
                // nEntityBarcodeLength,
                out groups_906,
                out strWarningParam,
                out strError);
            if (nRet == -1)
            {
                strError = "�ڽ�906�ֶη�������groups��������з�������: " + strError;
                return -1;
            }

            if (String.IsNullOrEmpty(strWarningParam) == false)
                strWarning += "906�ֶ� " + strWarningParam + "; ";

            nRet = BuildGroups(strField986,
                // nEntityBarcodeLength,
                out groups_986,
                out strWarningParam,
                out strError);
            if (nRet == -1)
            {
                strError = "�ڽ�986�ֶη�������groups��������з�������: " + strError;
                return -1;
            }

            if (String.IsNullOrEmpty(strWarningParam) == false)
                strWarning += "986�ֶ� " + strWarningParam + "; ";


            List<Group> new_groups = new List<Group>(); // ��������

            for (int i = 0; i < groups_906.Count; i++)
            {
                Group group906 = groups_906[i];

                bool bFound = false;
                for (int j = 0; j < groups_986.Count; j++)
                {
                    Group group986 = groups_986[j];

                    if (group906.strBarcode != "")
                    {
                        if (group906.strBarcode == group986.strBarcode)
                        {
                            bFound = true;

                            // �ظ�������£�����986��ȱ���������ֶ�
                            group986.MergeValue(group906);

                            break;
                        }
                    }
                    else if (group906.strRegisterNo != "")
                    {
                        if (group906.strRegisterNo == group986.strRegisterNo)
                        {
                            bFound = true;

                            // �ظ�������£�����986��ȱ���������ֶ�
                            group986.MergeValue(group906);

                            break;
                        }
                    }
                }

                if (bFound == true)
                {
                    continue;
                }

                group906.strMergeComment = "��906�ֶ�����������";
                new_groups.Add(group906);
            }

            groups = new List<Group>(); // �������
            groups.AddRange(groups_986);    // �ȼ���986�ڵ���������

            if (new_groups.Count > 0)
                groups.AddRange(new_groups);    // Ȼ�������������


            return 0;
        }


        #endregion
    }
#endif
}
