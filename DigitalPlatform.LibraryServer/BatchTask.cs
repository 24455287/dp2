using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Web;

using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;

namespace DigitalPlatform.LibraryServer
{
    // ����������
    public class BatchTask
    {
        public bool ManualStart = false;    // �����Ƿ�Ϊ�ֶ�������

        public bool Loop = false;
        int m_nPrevLoop = -1;   // 3̬ -1:��δ��ʼ�� 0:false 1:true

        // ��������
        public BatchTaskStartInfo StartInfo = null;

        // ������������
        // ����������ִ�е�ʱ��׷����������������������������ִ��
        public List<BatchTaskStartInfo> StartInfos = new List<BatchTaskStartInfo>();

        // ������
        public string Name = "";

        // �����ļ�
        Stream m_stream = null;
        public string ProgressFileName = "";
        public long ProgressFileVersion = 0;

        public string ProgressText = "";

        // //

        internal bool m_bClosed = true;

        internal LibraryApplication App = null;
        internal RmsChannelCollection RmsChannels = new RmsChannelCollection();

        internal ReaderWriterLock m_lock = new ReaderWriterLock();
        internal static int m_nLockTimeout = 5000;	// 5000=5��

        internal Thread threadWorker = null;
        internal AutoResetEvent eventClose = new AutoResetEvent(false);	// true : initial state is signaled 
        internal AutoResetEvent eventActive = new AutoResetEvent(false);	// �����ź�
        internal AutoResetEvent eventFinished = new AutoResetEvent(false);	// true : initial state is signaled 

        public int PerTime = 60 * 60 * 1000;	// 1Сʱ

        public void Activate()
        {
            eventActive.Set();
        }

        // �Ƿ��ֹͣ����������־�ָ��������������
        public virtual bool Stopped
        {
            get
            {
                if (this.App.HangupReason == HangupReason.LogRecover)
                    return true;
                if (this.App.PauseBatchTask == true)
                    return true;
                return this.m_bClosed;
            }
        }

        // ��ȡ�ϴ�������ʱ��
        public int ReadLastTime(
            string strMonitorName,
            out string strLastTime,
            out string strError)
        {
            strError = "";
            strLastTime = "";

            string strFileName = PathUtil.MergePath(this.App.LogDir, strMonitorName + "_lasttime.txt");

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
        public void WriteLastTime(string strMonitorName,
            string strLastTime)
        {
            string strFileName = PathUtil.MergePath(this.App.LogDir, strMonitorName + "_lasttime.txt");

            // ɾ��ԭ�����ļ�
            File.Delete(strFileName);

            // д��������
            StreamUtil.WriteText(strFileName,
                strLastTime);
        }

        // �����ǲ��Ƿ�����ÿ������ʱ��(�Ժ�)?
        // TODO: ����ϴμ��ص�ʱ�䣬��󳬹���ǰ���ڣ���һֱ�������������Ƿ���������������ǿ���������Ա�ﵽ��ʹ�����ϴβ���ʱ���Ŀ�ģ�
        // parameters:
        //      strLastTime ���һ��ִ�й���ʱ�� RFC1123��ʽ
        //      strStartTimeDef ���ض����ÿ������ʱ��
        //      bRet    �Ƿ���ÿ������ʱ��
        // return:
        //      -1  error
        //      0   û���ҵ�startTime���ò���
        //      1   �ҵ���startTime���ò���
        public int IsNowAfterPerDayStart(
            string strMonitorName,
            ref string strLastTime,
            out bool bRet,
            out string strStartTimeDef,
            out string strError)
        {
            strError = "";
            strStartTimeDef = "";

            XmlNode node = this.App.LibraryCfgDom.DocumentElement.SelectSingleNode("//monitors/" + strMonitorName);
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

            strStartTimeDef = strStartTime;

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

            // ��ǰʱ��
            DateTime now1 = DateTime.Now;

            // �۲챾���Ƿ��Ѿ�������
            if (String.IsNullOrEmpty(strLastTime) == false)
            {
                DateTime lasttime;

                try
                {
                    lasttime = DateTimeUtil.FromRfc1123DateTimeString(strLastTime);

                    if (lasttime.Year == now1.Year
                        && lasttime.Month == now1.Month
                        && lasttime.Day == now1.Day)
                    {
                        bRet = false;   // �����Ѿ�������
                        return 1;
                    }

                }
                catch
                {
                    bRet = false;
                    strError = "strLastTime " + strLastTime + " ��ʽ����";
                    return -1;
                }

                // 2014/3/22
                TimeSpan delta = new DateTime(now1.Year, now1.Month, now1.Day)
                    - new DateTime(lasttime.Year, lasttime.Month, lasttime.Day);
                // �ϴ��������Ѿ���������ǰ
                if (delta.TotalDays > 1)
                {
                    bRet = true;
                    return 1;
                }
            }
            else
            {
                // strLastTime Ϊ��
                // �ѵ�ǰʱ����Ϊ�ϴδ����ʱ�䡣�������Ա����Ժ���Զ�ֲ�������ʱ��
                strLastTime = DateTimeUtil.Rfc1123DateTimeStringEx(this.App.Clock.UtcNow.ToLocalTime());
            }

            // ����Ķ���ʱ��
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
            return 1;
        }

        public BatchTask(LibraryApplication app,
            string strName)
        {
            if (String.IsNullOrEmpty(strName) == true)
                this.Name = this.DefaultName;
            else
                this.Name = strName;

            this.App = app;
            this.RmsChannels.GUI = false;

            this.RmsChannels.AskAccountInfo -= new AskAccountInfoEventHandle(RmsChannels_AskAccountInfo);
            this.RmsChannels.AskAccountInfo += new AskAccountInfoEventHandle(RmsChannels_AskAccountInfo);

            Debug.Assert(this.App != null, "");
            this.ProgressFileName = this.App.GetTempFileName("batch_progress"); //  Path.GetTempFileName();
            try
            {
                // ����ļ����ڣ��ʹ򿪣�����ļ������ڣ��ʹ���һ���µ�
                m_stream = File.Open(
    this.ProgressFileName,
    FileMode.OpenOrCreate,
    FileAccess.ReadWrite,
    FileShare.ReadWrite);
                this.ProgressFileVersion = DateTime.Now.Ticks;
            }
            catch (Exception ex)
            {
                string strError = "�򿪻򴴽��ļ� '" + this.ProgressFileName + "' ��������: " + ex.Message;
                throw new Exception(strError);
            }

            m_stream.Seek(0, SeekOrigin.End);
        }

        // ��������ļ�����
        public void ClearProgressFile()
        {
            if (String.IsNullOrEmpty(this.ProgressFileName) == false)
            {
                if (this.m_stream != null)
                {
                    this.m_stream.SetLength(0);
                }
            }

            this.ProgressFileVersion = DateTime.Now.Ticks;  // 2009/7/16 new add
        }

        public virtual string DefaultName
        {
            get
            {
                throw new Exception("DefaltName��δʵ��");
            }
        }

        public string Dp2UserName
        {
            get
            {
                return App.ManagerUserName;
            }
        }

        public string Dp2Password
        {
            get
            {
                return App.ManagerPassword;
            }
        }

        void RmsChannels_AskAccountInfo(object sender, AskAccountInfoEventArgs e)
        {
            e.Owner = null;

            ///
            e.UserName = this.Dp2UserName;
            e.Password = this.Dp2Password;
            e.Result = 1;
        }

        // ���������߳�
        public void StartWorkerThread()
        {
            if (this.threadWorker != null
                && this.threadWorker.IsAlive == true)
            {
                this.eventActive.Set();
                this.eventClose.Reset();    // 2006/11/24
                return;
            }

            this.m_bClosed = false;

            this.eventActive.Set();
            this.eventClose.Reset();    // 2006/11/24

            this.threadWorker =
                new Thread(new ThreadStart(this.ThreadMain));

            // Thread.Sleep(1);

            if (this.m_nPrevLoop != -1)
                this.Loop = this.m_nPrevLoop == 1 ? true : false;   // �ָ���һ�ε�Loopֵ
            try
            {
                this.threadWorker.Start();
            }
            catch (Exception ex)
            {
                string strErrorText = "StartWorkerThread()�����쳣: " + ExceptionUtil.GetDebugText(ex);
                this.App.WriteErrorLog(strErrorText);

                try
                {
                    this.threadWorker.Abort();
                }
                catch
                {
                }

                try
                {
                    // ����ԭ�����̡߳����´���һ���߳�
                    this.threadWorker =
                        new Thread(new ThreadStart(this.ThreadMain));
                    this.threadWorker.Start();
                }
                catch
                {
                }
            }
        }

        // ������������߳�
        public void ActivateWorkerThread()
        {
            if (this.threadWorker != null
    && this.threadWorker.IsAlive == true)
            {
                this.eventActive.Set();
                return;
            }

            StartWorkerThread();

            /*
            this.eventActive.Set();
            if (this.threadWorker == null)
            {
                this.threadWorker =
                    new Thread(new ThreadStart(this.ThreadMain));
            }
            if (this.threadWorker.IsAlive == false)
            {
                try
                {
                    this.threadWorker.Start();
                }
                catch (Exception ex)
                {
                    string strErrorText = "ActivateWorkerThread()�����쳣: " + ExceptionUtil.GetDebugText(ex);
                    this.App.WriteErrorLog(strErrorText);
                }
            }
             * */
        }


        public void Close()
        {
            this.eventClose.Set();
            this.m_bClosed = true;

            if (this.m_stream != null)
            {
                this.m_stream.Close();
                this.m_stream = null;
            }

            if (String.IsNullOrEmpty(this.ProgressFileName) == false)
            {
                File.Delete(this.ProgressFileName);
                this.ProgressFileVersion++;
            }
        }

        public void Stop()
        {
            this.eventClose.Set();
            this.m_bClosed = true;

            this.m_nPrevLoop = this.Loop == true ? 1: 0;   // ����ǰһ�ε�Loopֵ
            this.Loop = false;  // ��ֹ��һ�����ѭ��
        }

        // ���ý����ı�
        // ���̣߳���ȫ
        internal void SetProgressText(string strText)
        {
            this.m_lock.AcquireWriterLock(m_nLockTimeout);

            try
            {
                this.ProgressText = strText;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

        }

        // ׷�ӽ���ı�
        // ��װ�汾
        internal void AppendResultText(string strText)
        {
            AppendResultText(true, strText);
        }

        // ׷�ӽ���ı�
        // ��װ�汾
        internal void AppendResultTextNoTime(string strText)
        {
            AppendResultText(false, strText);
        }

        // ׷�ӽ���ı�
        // ���̣߳���ȫ
        internal void AppendResultText(bool bDisplayTime,
            string strText)
        {
            if (String.IsNullOrEmpty(strText) == true)
                return;
            if (m_stream == null)
                return;

            this.m_lock.AcquireWriterLock(m_nLockTimeout);

            try
            {
                strText = (bDisplayTime == true ? DateTime.Now.ToString() + " " : "")
                    + HttpUtility.HtmlEncode(strText);  // 2007/10/10 new add htmlencode()
                byte[] buffer = Encoding.UTF8.GetBytes(strText);

                m_stream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
        }

        // �������ǰ��Ϣ
        // ���̣߳���ȫ
        public BatchTaskInfo GetCurrentInfo(long lResultStart,
            int nMaxResultBytes)
        {
            this.m_lock.AcquireReaderLock(m_nLockTimeout);

            try
            {
                BatchTaskInfo info = new BatchTaskInfo();
                info.Name = this.Name;
                if (this.m_bClosed == false)
                    info.State = "������";
                else
                    info.State = "ֹͣ";

                if (this.App.PauseBatchTask == true)
                    info.ProgressText = "[ע�⣺ȫ�������������Ѿ�����ͣ] " + this.ProgressText;
                else
                    info.ProgressText = this.ProgressText;

                byte[] baResultText = null;
                long lOffset = 0;
                long lTotalLength = 0;
                this.GetResultText(lResultStart,
                    nMaxResultBytes,
                    out baResultText,
                    out lOffset,
                    out lTotalLength);
                info.ResultText = baResultText;
                info.ResultOffset = lOffset;
                info.ResultTotalLength = lTotalLength;
                info.ResultVersion = this.ProgressFileVersion;

                return info;
            }
            finally
            {
                this.m_lock.ReleaseReaderLock();
            }

        }

        // ����������ı�
        // parameters:
        //      lEndOffset  ���λ�ȡ��ĩβƫ��
        //      lTotalLength    ����������󳤶�
        public void GetResultText(long lStart,
            int nMaxBytes,
            out byte[] baResult,
            out long lEndOffset,
            out long lTotalLength)
        {
            baResult = null;
            lEndOffset = 0;

            lTotalLength = this.m_stream.Length;

            long lLength = this.m_stream.Length - lStart;

            if (lLength <= 0)
            {
                lEndOffset = this.m_stream.Length;
                return;
            }

            baResult = new byte[Math.Min(nMaxBytes, (int)lLength)];

            this.m_stream.Seek(lStart, SeekOrigin.Begin);
            try
            {
                int nByteReaded = this.m_stream.Read(baResult, 0, baResult.Length);

                Debug.Assert(nByteReaded == baResult.Length);

                lEndOffset = lStart + nByteReaded;
            }
            finally
            {
                // ָ��ص��ļ�ĩβ
                this.m_stream.Seek(0, SeekOrigin.End);
            }

            return;
        }

        // �����߳�
        public virtual void ThreadMain()
        {
            try
            {
                WaitHandle[] events = new WaitHandle[2];

                events[0] = eventClose;
                events[1] = eventActive;

                while (true)
                {
                    int index = 0;
                    try
                    {
                        index = WaitHandle.WaitAny(events, PerTime, false);
                    }
                    catch (System.Threading.ThreadAbortException /*ex*/)
                    {
                        /*
                        // ������
                        LibraryApplication.WriteWindowsLog("BatchTask������ThreadAbortException�쳣", EventLogEntryType.Information);
                         * */
                        this.App.Save(null, false);    // ��������
                        this.App.WriteErrorLog("�ղ���ThreadAbortException�����������ļ�����");
                        break;
                    }

                    if (index == WaitHandle.WaitTimeout)
                    {
                        // ��ʱ
                        eventActive.Reset();
                        Worker();
                        eventActive.Reset();    // 2013/11/23 ֻ�ö�ס��ʱ�򷢻�����

                    }
                    else if (index == 0)
                    {
                        break;
                    }
                    else
                    {
                        // �õ������ź�
                        eventActive.Reset();
                        Worker();
                        eventActive.Reset();    // 2013/11/23 ֻ�ö�ס��ʱ�򷢻�����
                    }

                    // �Ƿ�ѭ��?
                    if (this.Loop == false)
                        break;
                }
                this.ManualStart = false;   // �������ֻ��һ�ִ����й���
            }
            catch (Exception ex)
            {
                string strErrorText = "BatchTask�����̳߳����쳣: " + ExceptionUtil.GetDebugText(ex);
                try
                {
                    this.App.WriteErrorLog(strErrorText);
                }
                catch
                {
                    LibraryApplication.WriteWindowsLog(strErrorText);
                }
            }
            finally
            {
                // 2009/7/16 �ƶ�������
                eventFinished.Set();

                // 2009/7/16 ����
                this.m_bClosed = true;
            }

        }

        // �����߳�ÿһ��ѭ����ʵ���Թ���
        public virtual void Worker()
        {

        }

    }
}
