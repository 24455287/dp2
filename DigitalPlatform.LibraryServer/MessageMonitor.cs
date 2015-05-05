using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Web;

using DigitalPlatform.IO;
using DigitalPlatform.Xml;
using DigitalPlatform.rms.Client;

namespace DigitalPlatform.LibraryServer
{
    public class MessageMonitor : BatchTask
    {
        public MessageMonitor(LibraryApplication app,
            string strName)
            : base(app, strName)
        {
            this.Loop = true;
        }

        public override string DefaultName
        {
            get
            {
                return "��Ϣ���";
            }
        }

        // ����ϵ��ַ���
        static string MakeBreakPointString(
            string strRecordID)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            // <loop>
            XmlNode nodeLoop = dom.CreateElement("loop");
            dom.DocumentElement.AppendChild(nodeLoop);

            DomUtil.SetAttr(nodeLoop, "recordid", strRecordID);

            return dom.OuterXml;
        }

        // ���� ��ʼ ����
        // parameters:
        //      strStart    �����ַ�������ʽһ��Ϊindex.offsetstring@logfilename
        //                  ����Զ��ַ���Ϊ"!breakpoint"����ʾ�ӷ���������Ķϵ���Ϣ��ʼ
        int ParseMessageMonitorStart(string strStart,
            out string strRecordID,
            out string strError)
        {
            strError = "";
            strRecordID = "";

            int nRet = 0;

            if (String.IsNullOrEmpty(strStart) == true)
            {
                // strError = "������������Ϊ��";
                // return -1;
                strRecordID = "1";
                return 0;
            }

            if (strStart == "!breakpoint")
            {
                // �Ӷϵ�����ļ��ж�����Ϣ
                // return:
                //      -1  error
                //      0   file not found
                //      1   found
                nRet = this.App.ReadBatchTaskBreakPointFile(
                    this.DefaultName,
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
                    strError = "��ǰ������û�з��� "+this.DefaultName+" �ϵ���Ϣ���޷���������";
                    return -1;
                }

                Debug.Assert(nRet == 1, "");
                this.AppendResultText("����������� "+this.DefaultName+" �ϴζϵ��ַ���Ϊ: "
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

            XmlNode nodeLoop = dom.DocumentElement.SelectSingleNode("loop");
            if (nodeLoop != null)
            {
                strRecordID = DomUtil.GetAttr(nodeLoop, "recordid");
            }

            return 0;
        }

        public static string MakeMessageMonitorParam(
    bool bLoop)
        {
            XmlDocument dom = new XmlDocument();

            dom.LoadXml("<root />");

            DomUtil.SetAttr(dom.DocumentElement, "loop",
                bLoop == true ? "yes" : "no");

            return dom.OuterXml;
        }


        // ����ͨ����������
        // ��ʽ
        /*
         * <root loop='...'/>
         * loopȱʡΪtrue
         * 
         * */
        public static int ParseMessageMonitorParam(string strParam,
            out bool bLoop,
            out string strError)
        {
            strError = "";
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
            // 2012/2/4
            if (this.App.PauseBatchTask == true)
                return;

            bool bFirst = true;
            string strError = "";
            int nRet = 0;

            BatchTaskStartInfo startinfo = this.StartInfo;
            if (startinfo == null)
                startinfo = new BatchTaskStartInfo();   // ����ȱʡֵ��

            // ͨ����������
            bool bLoop = true;
            nRet = ParseMessageMonitorParam(startinfo.Param,
                out bLoop,
                out strError);
            if (nRet == -1)
            {
                this.AppendResultText("����ʧ��: " + strError + "\r\n");
                return;
            }

            this.Loop = bLoop;

            string strID = "";
            nRet = ParseMessageMonitorStart(startinfo.Start,
                out strID,
                out strError);
            if (nRet == -1)
            {
                this.AppendResultText("����ʧ��: " + strError + "\r\n");
                this.Loop = false;
                return;
            }

            // 
            bool bPerDayStart = false;  // �Ƿ�Ϊÿ��һ������ģʽ
            string strMonitorName = "messageMonitor";
            {
                string strLastTime = "";

                nRet = ReadLastTime(
                    strMonitorName,
                    out strLastTime,
                    out strError);
                if (nRet == -1)
                {
                    string strErrorText = "���ļ��л�ȡ " + strMonitorName + " ÿ������ʱ��ʱ��������: " + strError;
                    this.AppendResultText(strErrorText + "\r\n");
                    this.App.WriteErrorLog(strErrorText);
                    return;
                }

                string strStartTimeDef = "";
                //      bRet    �Ƿ���ÿ������ʱ��
                bool bRet = false;
                string strOldLastTime = strLastTime;

                // return:
                //      -1  error
                //      0   û���ҵ�startTime���ò���
                //      1   �ҵ���startTime���ò���
                nRet = IsNowAfterPerDayStart(
                    strMonitorName,
                    ref strLastTime,
                    out bRet,
                    out strStartTimeDef,
                    out strError);
                if (nRet == -1)
                {
                    string strErrorText = "��ȡ " + strMonitorName + " ÿ������ʱ��ʱ��������: " + strError;
                    this.AppendResultText(strErrorText + "\r\n");
                    this.App.WriteErrorLog(strErrorText);
                    return;
                }

                // ���nRet == 0����ʾû��������ز����������ԭ����ϰ�ߣ�ÿ�ζ���
                if (nRet == 0)
                {

                }
                else if (nRet == 1)
                {
                    if (bRet == false)
                    {
                        if (this.ManualStart == true)
                            this.AppendResultText("����̽�������� '" + this.Name + "'������û�е�ÿ������ʱ�� " + strStartTimeDef + " ��δ��������(�ϴ����������ʱ��Ϊ " + DateTimeUtil.LocalTime(strLastTime) + ")\r\n");

                        // 2014/3/31
                        if (string.IsNullOrEmpty(strOldLastTime) == true
                            && string.IsNullOrEmpty(strLastTime) == false)
                        {
                            this.AppendResultText("ʷ���״������������Ѱѵ�ǰʱ�䵱���ϴ����������ʱ�� " + DateTimeUtil.LocalTime(strLastTime) + " д���˶ϵ�����ļ�\r\n");
                            WriteLastTime(strMonitorName, strLastTime);
                        }

                        return; // ��û�е�ÿ��ʱ��
                    }

                    bPerDayStart = true;
                }

                this.App.WriteErrorLog((bPerDayStart == true ? "(��ʱ)" : "(����ʱ)") + strMonitorName + " ������");
            }

            AppendResultText("��ʼ��һ��ѭ��");

            RmsChannel channel = this.RmsChannels.GetChannel(this.App.WsUrl);

            string strMessageDbName = this.App.MessageDbName;

            if (String.IsNullOrEmpty(strMessageDbName) == true)
            {
                AppendResultText("��δ������Ϣ����(<message dbname='...' />)");
                this.Loop = false;
                return;
            }

            if (String.IsNullOrEmpty(this.App.MessageReserveTimeSpan) == true)
            {
                AppendResultText("��δ������Ϣ��������(<message reserveTimeSpan='...' />");
                this.Loop = false;
                return;
            }

            // ��������ֵ
            string strPeriodUnit = "";
            long lPeriodValue = 0;

            nRet = LibraryApplication.ParsePeriodUnit(
                this.App.MessageReserveTimeSpan,
                out lPeriodValue,
                out strPeriodUnit,
                out strError);
            if (nRet == -1)
            {
                strError = "��Ϣ�������� ֵ '" + this.App.MessageReserveTimeSpan + "' ��ʽ����: " + strError;
                AppendResultText(strError);
                this.Loop = false;
                return;
            }

            AppendResultText("��ʼ������Ϣ�� " + strMessageDbName + " ��ѭ��");

            // string strID = "1";
            int nRecCount = 0;
            for (; ; nRecCount++)
            {
                // ϵͳ�����ʱ�򣬲����б��߳�
                // 2008/2/4
                if (this.App.HangupReason == HangupReason.LogRecover)
                    break;
                // 2012/2/4
                if (this.App.PauseBatchTask == true)
                    break;

                if (this.Stopped == true)
                    break;

                string strStyle = "";
                strStyle = "data,content,timestamp,outputpath";

                if (bFirst == true)
                    strStyle += "";
                else
                {
                    strStyle += ",next";
                }

                string strPath = strMessageDbName + "/" + strID;

                string strXmlBody = "";
                string strMetaData = "";
                string strOutputPath = "";
                byte[] baOutputTimeStamp = null;

                // 
                SetProgressText((nRecCount + 1).ToString() + " " + strPath);

                // �����Դ
                // return:
                //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
                //		0	�ɹ�
                long lRet = channel.GetRes(strPath,
                    strStyle,
                    out strXmlBody,
                    out strMetaData,
                    out baOutputTimeStamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.NotFound)
                    {
                        if (bFirst == true)
                        {
                            // ��һ��û���ҵ�, ����Ҫǿ��ѭ������
                            bFirst = false;
                            goto CONTINUE;
                        }
                        else
                        {
                            if (bFirst == true)
                            {
                                strError = "���ݿ� " + strMessageDbName + " ��¼ " + strID + " �����ڡ����������";

                            }
                            else
                            {
                                strError = "���ݿ� " + strMessageDbName + " ��¼ " + strID + " ����ĩһ����¼�����������";
                            }
                            break;
                        }

                    }
                    else if (channel.ErrorCode == ChannelErrorCode.EmptyRecord)
                    {
                        bFirst = false;
                        // ��id��������
                        strID = ResPath.GetRecordId(strOutputPath);
                        goto CONTINUE;

                    }

                    goto ERROR1;
                }

                bFirst = false;

                // ��id��������
                strID = ResPath.GetRecordId(strOutputPath);

                try
                {
                    // ����
                    nRet = DoOneRecord(
                        lPeriodValue,
                        strPeriodUnit,
                        strOutputPath,
                        strXmlBody,
                        baOutputTimeStamp,
                        out strError);
                }
                catch (Exception ex)
                {
                    strError = "DoOneRecord exception: " + ExceptionUtil.GetDebugText(ex);
                    this.AppendResultText(strError + "\r\n");
                    this.SetProgressText(strError);
                    nRet = -1;
                }
                if (nRet == -1)
                {
                    AppendResultText("DoOneRecord() error : " + strError + "��\r\n");
                }


            CONTINUE:
                continue;

            } // end of for

            // ������������λ�ϵ�
            this.App.RemoveBatchTaskBreakPointFile(this.Name);
            this.StartInfo.Start = "";

            AppendResultText("�����Ϣ�� " + strMessageDbName + " ��ѭ�������������� " + nRecCount.ToString() + " ����¼��\r\n");

            {

                Debug.Assert(this.App != null, "");

                // д���ļ��������Ѿ������ĵ���ʱ��
                string strLastTime = DateTimeUtil.Rfc1123DateTimeStringEx(this.App.Clock.UtcNow.ToLocalTime());  // 2007/12/17 changed // DateTime.UtcNow // 2012/5/27
                WriteLastTime(strMonitorName,
                    strLastTime);
                string strErrorText = (bPerDayStart == true ? "(��ʱ)" : "(����ʱ)") + strMonitorName + "�������������¼ " + nRecCount.ToString() + " ����";
                this.App.WriteErrorLog(strErrorText);

            }

            return;

        ERROR1:
            // ����ϵ�
            this.StartInfo.Start = MemoBreakPoint(
                strID //strRecordID,
                );


            this.Loop = true;   // �����Ժ��������ѭ��?
            startinfo.Param = MakeMessageMonitorParam(
                bLoop);


            AppendResultText("MessageMonitor thread error : " + strError + "\r\n");
            this.App.WriteErrorLog("MessageMonitor thread error : " + strError + "\r\n");
            return;
        }

        // ����һ�¶ϵ㣬�Ա�����
        string MemoBreakPoint(
            string strRecordID)
        {
            string strBreakPointString = "";

            strBreakPointString = MakeBreakPointString(
                strRecordID);

            // д��ϵ��ļ�
            this.App.WriteBatchTaskBreakPointFile(this.Name,
                strBreakPointString);

            return strBreakPointString;
        }

        // ����һ����¼
        int DoOneRecord(
            long lPeriodValue,
            string strPeriodUnit,
            string strPath,
            string strRecXml,
            byte[] baTimeStamp,
            out string strError)
        {
            strError = "";
            long lRet = 0;
            int nRet = 0;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strRecXml);
            }
            catch (Exception ex)
            {
                strError = "װ��XML��DOM����: " + ex.Message;
                return -1;
            }

            string strDate = DomUtil.GetElementText(dom.DocumentElement,
                "date");

            bool bDelete = false;

            //
            DateTime date;

            try
            {
                date = DateTimeUtil.FromRfc1123DateTimeString(strDate);
            }
            catch
            {
                strError = "��¼ "+strPath+" ��Ϣ����ֵ '" + strDate + "' ��ʽ����";
                this.App.WriteErrorLog(strError);
                // ע����ȻҪɾ��
                bDelete = true;
                goto DO_DELETE;
            }


            // ���滯ʱ��date
            nRet = LibraryApplication.RoundTime(strPeriodUnit,
                ref date,
                out strError);
            if (nRet == -1)
            {
                strError = "���滯dateʱ�� " +date.ToString()+ " (ʱ�䵥λ: "+strPeriodUnit+") ʱ����: " + strError;
                return -1;
            }

            DateTime now = this.App.Clock.UtcNow;  //  DateTime.UtcNow;

            // ���滯ʱ��now
            nRet = LibraryApplication.RoundTime(strPeriodUnit,
                ref now,
                out strError);
            if (nRet == -1)
            {
                strError = "���滯nowʱ�� " + now.ToString() + " (ʱ�䵥λ: " + strPeriodUnit + ") ʱ����: " + strError;
                return -1;
            }

            TimeSpan delta = now - date;

            long lDelta = 0;

            nRet = LibraryApplication.ParseTimeSpan(
                delta,
                strPeriodUnit,
                out lDelta,
                out strError);
            if (nRet == -1)
                return -1;

            if (lDelta >= lPeriodValue)
                bDelete = true;

        DO_DELETE:

            if (bDelete == true)
            {
                RmsChannel channel = this.RmsChannels.GetChannel(this.App.WsUrl);

                byte[] output_timestamp = null;
                lRet = channel.DoDeleteRes(
    strPath,
    baTimeStamp,
    out output_timestamp,
    out strError);
                if (lRet == -1)
                {
                    // ������β�ɾ�����Ժ��л���
                    strError = "ɾ����¼ " + strPath + "ʱ����: " + strError;
                    return -1;
                }

                // ���ָ��û�а��ֹ�������
                if (this.App.Statis != null)
                    this.App.Statis.IncreaseEntryValue(
                    "",
                    "��Ϣ���",
                    "ɾ��������Ϣ����",
                    1);

            }

            return 0;
        }

    }
}
