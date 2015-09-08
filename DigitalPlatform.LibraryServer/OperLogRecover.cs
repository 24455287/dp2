using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform.Xml;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// ��־�ָ� ����������
    /// </summary>
    public class OperLogRecover : BatchTask
    {
        // ��־�ָ�����
        public RecoverLevel RecoverLevel = RecoverLevel.Snapshot;

        public OperLogRecover(LibraryApplication app,
            string strName)
            : base(app, strName)
        {
            this.PerTime = 0;
        }

        public override string DefaultName
        {
            get
            {
                return "��־�ָ�";
            }
        }

        // �Ƿ�Ӧ��ֹͣ����������־�ָ�����
        public override bool Stopped
        {
            get
            {
                return this.m_bClosed;
            }
        }

        // ���� ��ʼ ����
        static int ParseLogRecorverStart(string strStart,
            out long index,
            out string strFileName,
            out string strError)
        {
            strError = "";
            index = 0;
            strFileName = "";

            if (String.IsNullOrEmpty(strStart) == true)
                return 0;

            int nRet = strStart.IndexOf('@');
            if (nRet == -1)
            {
                try
                {
                    index = Convert.ToInt64(strStart);
                }
                catch (Exception)
                {
                    strError = "�������� '" + strStart + "' ��ʽ����" + "���û��@����ӦΪ�����֡�";
                    return -1;
                }
                return 0;
            }

            try
            {
                index = Convert.ToInt64(strStart.Substring(0, nRet).Trim());
            }
            catch (Exception)
            {
                strError = "�������� '" + strStart + "' ��ʽ����'" + strStart.Substring(0, nRet).Trim() + "' ����Ӧ��Ϊ�����֡�";
                return -1;
            }


            strFileName = strStart.Substring(nRet + 1).Trim();

            // ����ļ���û����չ�����Զ�����
            if (String.IsNullOrEmpty(strFileName) == false)
            {
                nRet = strFileName.ToLower().LastIndexOf(".log");
                if (nRet == -1)
                    strFileName = strFileName + ".log";
            }

            return 0;
        }

        // ����ͨ����������
        // ��ʽ
        /*
         * <root recoverLevel='...' clearFirst='...'/>
         * recoverLevelȱʡΪSnapshot
         * clearFirstȱʡΪfalse
         * 
         * 
         * */
        public static int ParseLogRecoverParam(string strParam,
            out string strRecoverLevel,
            out bool bClearFirst,
            out string strError)
        {
            strError = "";
            bClearFirst = false;
            strRecoverLevel = "";

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

            /*
            Logic = 0,  // �߼�����
            LogicAndSnapshot = 1,   // �߼���������ʧ����ת�ÿ��ջָ�
            Snapshot = 3,   // ����ȫ�ģ�����
            Robust = 4,
             * */

            strRecoverLevel = DomUtil.GetAttr(dom.DocumentElement,
                "recoverLevel");
            string strClearFirst = DomUtil.GetAttr(dom.DocumentElement,
                "clearFirst");
            if (strClearFirst.ToLower() == "yes"
                || strClearFirst.ToLower() == "true")
                bClearFirst = true;
            else
                bClearFirst = false;

            return 0;
        }


        // һ�β���ѭ��
        public override void Worker()
        {
            // ��ϵͳ����
            this.App.HangupReason = HangupReason.LogRecover;

            try
            {

                string strError = "";

                BatchTaskStartInfo startinfo = this.StartInfo;
                if (startinfo == null)
                    startinfo = new BatchTaskStartInfo();   // ����ȱʡֵ��

                long lStartIndex = 0;// ��ʼλ��
                string strStartFileName = "";// ��ʼ�ļ���
                int nRet = ParseLogRecorverStart(startinfo.Start,
                    out lStartIndex,
                    out strStartFileName,
                    out strError);
                if (nRet == -1)
                {
                    this.AppendResultText("����ʧ��: " + strError + "\r\n");
                    return;
                }

                //
                string strRecoverLevel = "";
                bool bClearFirst = false;
                nRet = ParseLogRecoverParam(startinfo.Param,
                    out strRecoverLevel,
                    out bClearFirst,
                    out strError);
                if (nRet == -1)
                {
                    this.AppendResultText("����ʧ��: " + strError + "\r\n");
                    return;
                }

                if (String.IsNullOrEmpty(strRecoverLevel) == true)
                    strRecoverLevel = "Snapshot";

                try
                {
                    this.RecoverLevel = (RecoverLevel)Enum.Parse(typeof(RecoverLevel), strRecoverLevel, true);
                }
                catch (Exception ex)
                {
                    this.AppendResultText("����ʧ��: ��������Param�е�recoverLevelö��ֵ '" + strRecoverLevel + "' ����: " + ex.Message + "\r\n");
                    return;
                }

                this.App.WriteErrorLog("��־�ָ� ����������");

                if (bClearFirst == true)
                {
                    nRet = this.App.ClearAllDbs(this.RmsChannels,
                        out strError);
                    if (nRet == -1)
                    {
                        this.AppendResultText("���ȫ�����ݿ��¼ʱ��������: " + strError + "\r\n");
                        return;
                    }
                }

                bool bStart = false;
                if (String.IsNullOrEmpty(strStartFileName) == true)
                {
                    // �������ļ�
                    bStart = true;
                }


                // �г�������־�ļ�
                DirectoryInfo di = new DirectoryInfo(this.App.OperLog.Directory);

                FileInfo[] fis = di.GetFiles("*.log");

                // BUG!!! ��ǰȱ������2008/2/1
                Array.Sort(fis, new FileInfoCompare());


                for (int i = 0; i < fis.Length; i++)
                {
                    if (this.Stopped == true)
                        break;

                    string strFileName = fis[i].Name;

                    this.AppendResultText("����ļ� " + strFileName + "\r\n");

                    if (bStart == false)
                    {
                        // ���ض��ļ���ʼ��
                        if (strStartFileName == strFileName)
                        {
                            bStart = true;
                            if (lStartIndex < 0)
                                lStartIndex = 0;
                            // lStartIndex = Convert.ToInt64(startinfo.Param);
                        }
                    }

                    if (bStart == true)
                    {
                        nRet = DoOneLogFile(strFileName,
                            lStartIndex,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        lStartIndex = 0;    // ��һ���ļ��Ժ���ļ���ȫ����
                    }

                }

                this.AppendResultText("ѭ������\r\n");
                
                this.App.WriteErrorLog("��־�ָ� ���������");

                return;

            ERROR1:
                return;
            }
            finally
            {
                this.App.HangupReason = HangupReason.None;
            }
        }

        public class FileInfoCompare : IComparer
        {

            // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
            int IComparer.Compare(Object x, Object y)
            {
                return ((new CaseInsensitiveComparer()).Compare(((FileInfo)x).Name, ((FileInfo)y).Name));
            }

        }

        // ����һ����־�ļ��Ļָ�����
        // parameters:
        //      strFileName ���ļ���
        //      lStartIndex ��ʼ�ļ�¼����0��ʼ������
        // return:
        //      -1  error
        //      0   file not found
        //      1   succeed
        int DoOneLogFile(string strFileName,
            long lStartIndex,
            out string strError)
        {
            strError = "";

            this.AppendResultText("���ļ� "+strFileName+"\r\n");

            Debug.Assert(this.App != null, "");
            string strTempFileName = this.App.GetTempFileName("logrecover");    // Path.GetTempFileName();
            try
            {

                long lIndex = 0;
                long lHint = -1;
                long lHintNext = -1;

                for (lIndex = lStartIndex; ; lIndex++)
                {
                    if (this.Stopped == true)
                        break;

                    string strXml = "";

                    if (lIndex != 0)
                        lHint = lHintNext;

                    SetProgressText(strFileName + " ��¼" + (lIndex + 1).ToString());

                    Stream attachment = File.Create(strTempFileName);

                    try
                    {
                        // Debug.Assert(!(lIndex == 182 && strFileName == "20071225.log"), "");


                        long lAttachmentLength = 0;
                        // ���һ����־��¼
                        // parameters:
                        //      strFileName ���ļ���,����·������
                        //      lHint   ��¼λ�ð�ʾ�Բ���������һ��ֻ�з������������׺����ֵ������ǰ����˵�ǲ�͸���ġ�
                        //              Ŀǰ�ĺ����Ǽ�¼��ʼλ�á�
                        // return:
                        //      -1  error
                        //      0   file not found
                        //      1   succeed
                        //      2   ������Χ
                        int nRet = this.App.OperLog.GetOperLog(
                            "*",
                            strFileName,
                            lIndex,
                            lHint,
                            "", // level-0
                            "", // strFilter
                            out lHintNext,
                            out strXml,
                            ref attachment,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 0)
                            return 0;
                        if (nRet == 2)
                        {
                            // ���һ��������ʾһ��
                            if (((lIndex-1) % 100) != 0)
                                this.AppendResultText("����־��¼ " + strFileName + " " + (lIndex).ToString() + "\r\n");
                            break;
                        }

                        // ����һ����־��¼

                        if ((lIndex % 100) == 0)
                            this.AppendResultText("����־��¼ " + strFileName + " " + (lIndex + 1).ToString() + "\r\n");

                        /*
                        // ����ʱ�������ﰲ������
                        if (lIndex == 1 || lIndex == 2)
                            continue;
 * */

                        nRet = DoOperLogRecord(strXml,
                            attachment,
                            out strError);
                        if (nRet == -1)
                        {
                            this.AppendResultText("��������" + strError + "\r\n");
                            // 2007/6/25
                            // ���Ϊ���߼��ָ������������ͣ����������ڽ��в��ԡ�
                            // ������ͣ����������ѡ���߼�+���ա���
                            if (this.RecoverLevel == RecoverLevel.Logic)
                                return -1;
                        }
                    }
                    finally
                    {
                        attachment.Close();
                    }
                }

                return 0;
            }
            finally
            {
                File.Delete(strTempFileName);
            }
        }

        // ִ��һ����־��¼�Ļָ�����
        int DoOperLogRecord(string strXml,
            Stream attachment,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "��־��¼װ�ص�DOMʱ����: " + ex.Message;
                return -1;
            }

            string strOperation = DomUtil.GetElementText(dom.DocumentElement,
                "operation");
            if (strOperation == "borrow")
            {
                nRet = this.App.RecoverBorrow(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    false,
                    out strError);
            }
            else if (strOperation == "return")
            {
                nRet = this.App.RecoverReturn(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    false,
                    out strError);
            }
            else if (strOperation == "setEntity")
            {
                nRet = this.App.RecoverSetEntity(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    out strError);
            }
            else if (strOperation == "setOrder")
            {
                nRet = this.App.RecoverSetOrder(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    out strError);
            }
            else if (strOperation == "setIssue")
            {
                nRet = this.App.RecoverSetIssue(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    out strError);
            }
            else if (strOperation == "setComment")
            {
                nRet = this.App.RecoverSetComment(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    out strError);
            }
            else if (strOperation == "changeReaderPassword")
            {
                nRet = this.App.RecoverChangeReaderPassword(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    out strError);
            }
            else if (strOperation == "changeReaderTempPassword")
            {
                // 2013/11/3
            }
            else if (strOperation == "setReaderInfo")
            {
                nRet = this.App.RecoverSetReaderInfo(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    out strError);
            }
            else if (strOperation == "devolveReaderInfo")
            {
                nRet = this.App.RecoverDevolveReaderInfo(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    attachment,
                    out strError);
            }
            else if (strOperation == "amerce")
            {
                nRet = this.App.RecoverAmerce(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    out strError);
            }
            else if (strOperation == "setBiblioInfo")
            {
                nRet = this.App.RecoverSetBiblioInfo(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    out strError);
            }
            else if (strOperation == "hire")
            {
                nRet = this.App.RecoverHire(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    out strError);
            }
            else if (strOperation == "foregift")
            {
                // 2008/11/11
                nRet = this.App.RecoverForegift(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    out strError);
            }
            else if (strOperation == "settlement")
            {
                nRet = this.App.RecoverSettlement(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    out strError);
            }
            else if (strOperation == "writeRes")
            {
                // 2011/5/26
                nRet = this.App.RecoverWriteRes(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    attachment,
                    out strError);
            }
            else if (strOperation == "repairBorrowInfo")
            {
                // 2012/6/21
                nRet = this.App.RecoverRepairBorrowInfo(this.RmsChannels,
                    this.RecoverLevel,
                    dom,
                    attachment,
                    out strError);
            }
            else if (strOperation == "reservation")
            {
                // ��δʵ��
            }
            else if (strOperation == "setUser")
            {
                // ��δʵ��
            }
            else if (strOperation == "passgate")
            {
                // ֻ��
            }
            else if (strOperation == "getRes")
            {
                // ֻ�� 2015/7/14
            }
            else if (strOperation == "crashReport")
            {
                // ֻ�� 2015/7/16
            }
            else if (strOperation == "memo")
            {
                // ע�� 2015/9/8
            }
            else
            {
                strError = "����ʶ�����־�������� '" + strOperation + "'";
                return -1;
            }

            if (nRet == -1)
            {
                string strAction = DomUtil.GetElementText(dom.DocumentElement,
                        "action");
                strError = "operation=" +strOperation + ";action=" + strAction + ": " + strError;
                return -1;
            }

            return 0;
        }


    }
}
