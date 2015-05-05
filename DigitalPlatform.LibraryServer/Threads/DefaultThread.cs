using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// ������Ļ����߳�
    /// ����һЩ��Ҫ�϶�ʱ��Ϳ��Դ����С����
    /// </summary>
    public class DefaultThread : BatchTask
    {
        public DefaultThread(LibraryApplication app, 
            string strName)
            : base(app, strName)
        {
            this.Loop = true;
            this.PerTime = 5 * 60 * 1000;	// 5����
        }

        public override string DefaultName
        {
            get
            {
                return "�����߳�";
            }
        }

        DateTime m_lastRetryTime = DateTime.Now;
        int m_nRetryAfterMinutes = 5;   // ÿ������ٷ����Ժ�����һ��

        // һ�β���ѭ��
        public override void Worker()
        {

            // ���� Garden
            try
            {
                // TODO: �� hashtable �Ѿ����˵�ʱ����Ҫ���̴��ʱ��
                if (this.App.Garden.IsFull == true)
                    this.App.Garden.CleanPersons(new TimeSpan(0, 5, 0), this.App.Statis);    // 5���� ��������� REST Session������ʱ��
                else
                    this.App.Garden.CleanPersons(new TimeSpan(0, 20, 0), this.App.Statis);    // 20���� REST Session������ʱ��
            }
            catch (Exception ex)
            {
                string strErrorText = "DefaultTread�� CleanPersons() �����쳣: " + ExceptionUtil.GetDebugText(ex);
                this.App.WriteErrorLog(strErrorText);
            }

            // ��Ϊˢ��Statis
            if (this.App.Statis != null)
            {
                try
                {
                    this.App.Statis.Flush();
                }
                catch (Exception ex)
                {
                    string strErrorText = "DefaultTread�� this.App.Statis.Flush() �����쳣: " + ExceptionUtil.GetDebugText(ex);
                    this.App.WriteErrorLog(strErrorText);
                }
            }

            // ��ʱ����library.xml�ı仯
            if (this.App.Changed == true)
            {
                this.App.Flush();
            }

            // ����Sessions
            try
            {
                // TODO: �� hashtable �Ѿ����˵�ʱ����Ҫ���̴��ʱ��
                if (this.App.SessionTable.IsFull == true)
                    this.App.SessionTable.CleanSessions(new TimeSpan(0, 5, 0));    // 5���� ��������� REST Session������ʱ��
                else
                    this.App.SessionTable.CleanSessions(new TimeSpan(0, 20, 0));    // 20���� REST Session������ʱ��
            }
            catch (Exception ex)
            {
                string strErrorText = "DefaultTread�� CleanSessions() �����쳣: " + ExceptionUtil.GetDebugText(ex);
                this.App.WriteErrorLog(strErrorText);
            }

            int nRet = 0;
            string strError = "";

            if (this.App.kdbs == null
                        && (DateTime.Now - this.m_lastRetryTime).TotalMinutes >= m_nRetryAfterMinutes)
            {
                try
                {
                    nRet = this.App.InitialKdbs(this.RmsChannels,
            out strError);
                    if (nRet == -1)
                    {
                        this.App.WriteErrorLog("ERR003 ��ʼ��kdbsʧ��: " + strError);
                    }
                    else
                    {
                        // ��� dpKernel �汾��
                        nRet = this.App.CheckKernelVersion(this.RmsChannels,
                            out strError);
                        if (nRet == -1)
                            this.App.WriteErrorLog(strError);
                    }
                }
                catch (Exception ex)
                {
                    string strErrorText = "DefaultTread�� InitialKdbs() �����쳣: " + ExceptionUtil.GetDebugText(ex);
                    this.App.WriteErrorLog(strErrorText);
                }
            }

            // 
            if (this.App.vdbs == null
                        && (DateTime.Now - this.m_lastRetryTime).TotalMinutes >= m_nRetryAfterMinutes)
            {
                try
                {
                    nRet = this.App.InitialVdbs(this.RmsChannels,
                        out strError);
                    if (nRet == -1)
                    {
                        this.App.WriteErrorLog("ERR004 ��ʼ��vdbsʧ��: " + strError);
                    }
                }
                catch (Exception ex)
                {
                    string strErrorText = "DefaultTread�� InitialVdbs() �����쳣: " + ExceptionUtil.GetDebugText(ex);
                    this.App.WriteErrorLog(strErrorText);
                }
            }

            if (this.App.kdbs == null || this.App.vdbs == null)
            {
                m_nRetryAfterMinutes++;
            }

            // 2012/9/23
            if (this.App.OperLog != null && this.App.OperLog.Cache != null)
            {
                try
                {
                    this.App.OperLog.Cache.Shrink(new TimeSpan(0, 1, 0));    // һ����
                }
                catch (Exception ex)
                {
                    string strErrorText = "DefaultTread�� ѹ�� OperLog.Cache �����쳣: " + ExceptionUtil.GetDebugText(ex);
                    this.App.WriteErrorLog(strErrorText);
                }
            }
        }

        public void ClearRetryDelay()
        {
            this.m_nRetryAfterMinutes = 0;
        }
    }
}
