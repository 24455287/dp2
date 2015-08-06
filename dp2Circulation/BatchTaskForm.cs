using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Threading;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /*
     * 2011/2/23
�������������񱻷�ֹ�ˣ�
����DTLP���ݿ�
��������
     * */
    /// <summary>
    /// ����������
    /// </summary>
    public partial class BatchTaskForm : MyForm
    {
        int m_nInRefresh = 0;

        internal ReaderWriterLock m_lock = new ReaderWriterLock();
        internal static int m_nLockTimeout = 5000;	// 5000=5��

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;
#endif

        string MonitorTaskName = "";    // Ҫ��ص�������

        Decoder ResultTextDecoder = Encoding.UTF8.GetDecoder();
        long CurResultOffs = 0;

        long CurResultVersion = 0;

        const int WM_INITIAL = API.WM_USER + 201;

        MessageStyle m_messageStyle = MessageStyle.Progress | MessageStyle.Result;

        /// <summary>
        /// ��Ϣ���
        /// </summary>
        public MessageStyle MessageStyle
        {
            get
            {
                return this.m_messageStyle;
            }
            set
            {
                this.m_messageStyle = value;

                this.ToolStripMenuItem_progress.Checked = false;
                this.ToolStripMenuItem_result.Checked = false;

                this.label_progress.Text = "";  // ÿ�α䶯�������������ʾ��

                if ((this.m_messageStyle & MessageStyle.Progress) != 0)
                    this.ToolStripMenuItem_progress.Checked = true;
                if ((this.m_messageStyle & MessageStyle.Result) != 0)
                    this.ToolStripMenuItem_result.Checked = true;
               
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public BatchTaskForm()
        {
            InitializeComponent();
        }

        private void BatchTaskForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
#if NO
            MainForm.AppInfo.LoadMdiChildFormStates(this,
    "mdi_form_state");
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            this.comboBox_taskName.Text = MainForm.AppInfo.GetString(
"BatchTaskForm",
"BatchTaskName",
    "");

            this.webBrowser_info.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser_info_DocumentCompleted);


            // ʹ�ò˵���ʾ��ȷ
            this.MessageStyle = this.MessageStyle;

            // 
            API.PostMessage(this.Handle, WM_INITIAL, 0, 0);
        }

        void webBrowser_info_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Global.ScrollToEnd(this.webBrowser_info);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_INITIAL:
                    {
                        stop.SetMessage("���ڳ�ʼ��������ؼ�...");
                        this.Update();
                        this.MainForm.Update();

                        ClearWebBrowser(this.webBrowser_info, true);

                        if (this.toolStripButton_monitoring.Checked == true)
                        {
                            StartMonitor(this.comboBox_taskName.Text,
                                this.toolStripButton_monitoring.Checked);
                        }
                        stop.SetMessage("");
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }

        private void BatchTaskForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.webBrowser_info.DocumentCompleted -= new WebBrowserDocumentCompletedEventHandler(webBrowser_info_DocumentCompleted);

            this.timer_monitorTask.Stop();

            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                MainForm.AppInfo.SetString(
    "BatchTaskForm",
    "BatchTaskName",
    this.comboBox_taskName.Text);
            }

        }

#if NO
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }
#endif

        private void button_start_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = StartBatchTask(this.comboBox_taskName.Text,
                out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
            else
                MessageBox.Show(this, "���� '" +this.comboBox_taskName.Text+ "' �ѳɹ�����");

        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = StopBatchTask(this.comboBox_taskName.Text,
                out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
            else
                MessageBox.Show(this, "���� '" + this.comboBox_taskName.Text + "' ��ֹͣ");

        }

#if NO
        // *** �Ѿ�ɾ��
        private void checkBox_monitoring_CheckedChanged(object sender, EventArgs e)
        {
            if (this.comboBox_taskName.Text == "")
                return;

                StartMonitor(this.comboBox_taskName.Text,
                    this.checkBox_monitoring.Checked);

        }
#endif

        // ������ֹͣ���һ������
        void StartMonitor(string strTaskName,
            bool bStart)
        {
            if (String.IsNullOrEmpty(strTaskName) == true)
                return;

            this.MonitorTaskName = strTaskName;

            // ���´������������Ա������������Ϣ��
            this.ResultTextDecoder = Encoding.UTF8.GetDecoder();
            this.CurResultOffs = 0;

            if (bStart == true)
            {
                this.timer_monitorTask.Start();
            }
            else
            {
                this.timer_monitorTask.Stop();
            }
        }

        // ��������������
        int StartBatchTask(string strTaskName,
            out string strError)
        {
            strError = "";

            BatchTaskStartInfo startinfo = new BatchTaskStartInfo();
            if (strTaskName == "��־�ָ�")
            {
                StartLogRecoverDlg dlg = new StartLogRecoverDlg();
                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.StartInfo = startinfo;
                dlg.ShowDialog(this);
                if (dlg.DialogResult != DialogResult.OK)
                {
                    strError = "�û���������";
                    return -1;
                }
            }
            else if (strTaskName == "dp2Library ͬ��")
            {
                StartReplicationDlg dlg = new StartReplicationDlg();
                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.StartInfo = startinfo;
                dlg.ShowDialog(this);
                if (dlg.DialogResult != DialogResult.OK)
                {
                    strError = "�û���������";
                    return -1;
                }
            }
            else if (strTaskName == "�ؽ�������")
            {
                StartRebuildKeysDlg dlg = new StartRebuildKeysDlg();
                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.StartInfo = startinfo;
                dlg.ShowDialog(this);
                if (dlg.DialogResult != DialogResult.OK)
                {
                    strError = "�û���������";
                    return -1;
                }
            }
            else if (strTaskName == "ԤԼ�������")
            {
                StartArriveMonitorDlg dlg = new StartArriveMonitorDlg();
                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.StartInfo = startinfo;
                dlg.ShowDialog(this);
                if (dlg.DialogResult != DialogResult.OK)
                {
                    strError = "�û���������";
                    return -1;
                }
            }
            /*
        else if (strTaskName == "����DTLP���ݿ�")
        {
            StartTraceDtlpDlg dlg = new StartTraceDtlpDlg();
        MainForm.SetControlFont(dlg, this.Font, false);
            dlg.StartInfo = startinfo;
            dlg.ShowDialog(this);
            if (dlg.DialogResult != DialogResult.OK)
            {
                strError = "�û���������";
                return -1;
            }
        }
             * */
            else if (strTaskName == "��Ԫһ��ͨ������Ϣͬ��")
            {
                StartZhengyuanReplicationDlg dlg = new StartZhengyuanReplicationDlg();
                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.StartInfo = startinfo;
                dlg.ShowDialog(this);
                if (dlg.DialogResult != DialogResult.OK)
                {
                    strError = "�û���������";
                    return -1;
                }
            }
            else if (strTaskName == "�Ͽ�Զ��һ��ͨ������Ϣͬ��")
            {
                StartDkywReplicationDlg dlg = new StartDkywReplicationDlg();
                MainForm.SetControlFont(dlg, this.Font, false);
                startinfo.Start = "!breakpoint";    // һ��ʼ�����ʵ���ȱʡֵ�������ͷ��ʼ����
                dlg.StartInfo = startinfo;
                dlg.ShowDialog(this);
                if (dlg.DialogResult != DialogResult.OK)
                {
                    strError = "�û���������";
                    return -1;
                }
            }
            else if (strTaskName == "������Ϣͬ��")
            {
#if NO
                StartPatronReplicationDlg dlg = new StartPatronReplicationDlg();
                MainForm.SetControlFont(dlg, this.Font, false);
                startinfo.Start = "!breakpoint";    // һ��ʼ�����ʵ���ȱʡֵ�������ͷ��ʼ����
                dlg.StartInfo = startinfo;
                dlg.ShowDialog(this);
                if (dlg.DialogResult != DialogResult.OK)
                {
                    strError = "�û���������";
                    return -1;
                }
#endif
                startinfo.Start = "activate";   // ��ʾ�������������Է�����ԭ�ж�ʱ��������
            }

            this.m_lock.AcquireWriterLock(m_nLockTimeout);

            try
            {
                EnableControls(false);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("����"
                    + "����"
                    + "���� '" + strTaskName + "' ...");
                stop.BeginLoop();

                this.Update();
                this.MainForm.Update();

                try
                {
                    BatchTaskInfo param = new BatchTaskInfo();
                    param.StartInfo = startinfo;

                    BatchTaskInfo resultInfo = null;

                    // return:
                    //      -1  ����
                    //      0   �����ɹ�
                    //      1   ����ǰ�����Ѿ�����ִ��״̬�����ε��ü������������
                    long lRet = Channel.BatchTask(
                        stop,
                        strTaskName,
                        "start",
                        param,
                        out resultInfo,
                        out strError);
                    if (lRet == -1 || lRet == 1)
                        goto ERROR1;

                    if (resultInfo != null)
                    {
                        Global.WriteHtml(this.webBrowser_info,
                            GetResultText(resultInfo.ResultText));
                        ScrollToEnd();
                    }

                    this.label_progress.Text = resultInfo.ProgressText;
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");

                    EnableControls(true);
                }

                return 1;
            ERROR1:
                return -1;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
        }

        // ֹͣ����������
        int StopBatchTask(string strTaskName,
            out string strError)
        {
            strError = "";

            this.m_lock.AcquireWriterLock(m_nLockTimeout);

            try
            {

                EnableControls(false);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("����"
                    + "ֹͣ"
                    + "���� '" + strTaskName + "' ...");
                stop.BeginLoop();

                this.Update();
                this.MainForm.Update();

                try
                {
                    BatchTaskInfo param = new BatchTaskInfo();
                    BatchTaskInfo resultInfo = null;

                    long lRet = Channel.BatchTask(
                        stop,
                        strTaskName,
                        "stop",
                        param,
                        out resultInfo,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    Global.WriteHtml(this.webBrowser_info,
                        GetResultText(resultInfo.ResultText));
                    ScrollToEnd();

                    this.label_progress.Text = resultInfo.ProgressText;
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");

                    EnableControls(true);
                }

                return 1;
            ERROR1:
                return -1;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
        }

        // ��������������
        int ContinueAllBatchTask(out string strError)
        {
            strError = "";

            BatchTaskStartInfo startinfo = new BatchTaskStartInfo();

            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                EnableControls(false);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڼ���ȫ������������ ...");
                stop.BeginLoop();

                this.Update();
                this.MainForm.Update();

                try
                {
                    BatchTaskInfo param = new BatchTaskInfo();
                    param.StartInfo = startinfo;

                    BatchTaskInfo resultInfo = null;

                    long lRet = Channel.BatchTask(
                        stop,
                        "",
                        "continue",
                        param,
                        out resultInfo,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (resultInfo != null)
                    {
                        Global.WriteHtml(this.webBrowser_info,
                            GetResultText(resultInfo.ResultText));
                        ScrollToEnd();

                        this.label_progress.Text = resultInfo.ProgressText;
                    }
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");

                    EnableControls(true);
                }

                return 1;
            ERROR1:
                return -1;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
        }

        // ��ͣ����������
        int PauseAllBatchTask(out string strError)
        {
            strError = "";

            BatchTaskStartInfo startinfo = new BatchTaskStartInfo();

            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                EnableControls(false);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("������ͣȫ������������ ...");
                stop.BeginLoop();

                this.Update();
                this.MainForm.Update();

                try
                {
                    BatchTaskInfo param = new BatchTaskInfo();
                    param.StartInfo = startinfo;

                    BatchTaskInfo resultInfo = null;

                    long lRet = Channel.BatchTask(
                        stop,
                        "",
                        "pause",
                        param,
                        out resultInfo,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (resultInfo != null)
                    {
                        Global.WriteHtml(this.webBrowser_info,
                            GetResultText(resultInfo.ResultText));
                        ScrollToEnd();

                        this.label_progress.Text = resultInfo.ProgressText;
                    }
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");

                    EnableControls(true);
                }

                return 1;
            ERROR1:
                return -1;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
        }


        private void comboBox_taskName_TextChanged(object sender, EventArgs e)
        {
            if (this.comboBox_taskName.Text == "")
                this.toolStripButton_monitoring.Enabled = false;
            else
                this.toolStripButton_monitoring.Enabled = true;

            if (this.MonitorTaskName != "")
            {
                this.MonitorTaskName = this.comboBox_taskName.Text;
                this.CurResultOffs = 0;
            }
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.comboBox_taskName.Enabled = bEnable;

            if (this.comboBox_taskName.Text != "")
                this.toolStripButton_monitoring.Enabled = bEnable;
            else
                this.toolStripButton_monitoring.Enabled = false;

            this.button_start.Enabled = bEnable;
            this.button_stop.Enabled = bEnable;

            /*
            this.button_refresh.Enabled = bEnable;
            this.button_rewind.Enabled = bEnable;
            this.button_clear.Enabled = bEnable;
             * */

            this.toolStripButton_refresh.Enabled = bEnable;
            this.toolStripButton_rewind.Enabled = bEnable;
            this.toolStripButton_clear.Enabled = bEnable;
            this.toolStripButton_continue.Enabled = bEnable;
            this.toolStripButton_pauseAll.Enabled = bEnable;
        }


        string GetResultText(byte[] baResult)
        {
            if (baResult == null)
                return "";
            if (baResult.Length == 0)
                return "";

            // Decoder ResultTextDecoder = Encoding.UTF8.GetDecoder;
            char[] chars = new char[baResult.Length];

            int nCharCount = this.ResultTextDecoder.GetChars(
                baResult,
                    0,
                    baResult.Length,
                    chars,
                    0);
            Debug.Assert(nCharCount <= baResult.Length, "");

            return new string(chars, 0, nCharCount);
        }

        private void timer_monitorTask_Tick(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(MonitorTaskName) == true)
                return;

            if (m_nInRefresh == 0)
            {
                // Global.ScrollToEnd(this.webBrowser_info);
                // ScrollToEnd();


                DoRefresh();
            }
        }

        void ScrollToEnd()
        {
            this.webBrowser_info.Document.Window.ScrollTo(0,
this.webBrowser_info.Document.Body.ScrollRectangle.Height);
        }

        // *** �Ѿ�ɾ��
        private void button_refresh_Click(object sender, EventArgs e)
        {
            if (m_nInRefresh > 0)
            {
                MessageBox.Show(this, "����ˢ����");
                return;
            }

            DoRefresh();
        }

        void DoRefresh()
        {
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            this.m_nInRefresh++;
            this.toolStripButton_refresh.Enabled = false;
            // this.EnableControls(false);
            try
            {

                string strError = "";

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڻ�ȡ���� '" + MonitorTaskName + "' ��������Ϣ ...");
                stop.BeginLoop();

                try
                {

                    for (int i=0;i<10;i++)  // ���ѭ����ȡ10��
                    {
                        Application.DoEvents();
                        if (stop != null && stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }

                        BatchTaskInfo param = new BatchTaskInfo();
                        BatchTaskInfo resultInfo = null;

                        if ((this.MessageStyle & MessageStyle.Result) == 0)
                        {
                            param.MaxResultBytes = 0;
                        }
                        else
                        {
                            param.MaxResultBytes = 4096;
                            if (i >= 5)  // ���������δ���ü���ȡ������̫�࣬�ͼ�ʱ���󡰴��ڡ��ߴ�
                                param.MaxResultBytes = 100 * 1024;
                        }

                        param.ResultOffset = this.CurResultOffs;

                        stop.SetMessage("���ڻ�ȡ���� '" + MonitorTaskName + "' ��������Ϣ (�� "+(i+1).ToString()+" �� ��10��)...");

                        long lRet = Channel.BatchTask(
                            stop,
                            MonitorTaskName,
                            "getinfo",
                            param,
                            out resultInfo,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;

                        Global.WriteHtml(this.webBrowser_info,
                            GetResultText(resultInfo.ResultText));
                        ScrollToEnd();

                        // DateTime now = DateTime.Now;

                        if ((this.MessageStyle & MessageStyle.Progress) != 0)
                        {
                            this.label_progress.Text = // now.ToLongTimeString() + " -- " + 
                                resultInfo.ProgressText;
                        }

                        if ((this.MessageStyle & MessageStyle.Result) == 0)
                        {
                            // û�б�Ҫ��ʾ�ۻ�
                            break;
                        }

                        if (this.CurResultOffs == 0)
                            this.CurResultVersion = resultInfo.ResultVersion;
                        else if (this.CurResultVersion != resultInfo.ResultVersion)
                        {
                            // ˵����������result�ļ���ʵ�Ѿ�����
                            this.CurResultOffs = 0; // rewind
                            Global.WriteHtml(this.webBrowser_info,
                                "***������ version=" + resultInfo.ResultVersion.ToString() + " ***\r\n");
                            ScrollToEnd();
                            goto COINTINU1;
                        }

                        if (resultInfo.ResultTotalLength < param.ResultOffset)
                        {
                            // ˵����������result�ļ���ʵ�Ѿ�����
                            this.CurResultOffs = 0; // rewind
                            Global.WriteHtml(this.webBrowser_info,
                                "***������***\r\n");
                            ScrollToEnd();
                            goto COINTINU1;
                        }
                        else
                        {
                            // �洢�����´�
                            this.CurResultOffs = resultInfo.ResultOffset;
                        }

                    COINTINU1:

                        // ������β�û�С����ס�����Ҫ����ѭ����ȡ�µ���Ϣ������ѭ����һ������������Ӧ�Է������������Ϣ�����Ρ�
                        if (resultInfo.ResultOffset >= resultInfo.ResultTotalLength)
                            break;
                    }
                    

                }
                finally
                {
                    this.toolStripButton_refresh.Enabled = true;
                    // this.EnableControls(true);
                    this.m_nInRefresh--;
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }

                return;
            ERROR1:
                this.label_progress.Text = strError;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
        }

        // *** �Ѿ�ɾ��
        private void button_clear_Click(object sender, EventArgs e)
        {
            ClearWebBrowser(this.webBrowser_info, false);
        }

        // *** �Ѿ�ɾ��
        private void button_rewind_Click(object sender, EventArgs e)
        {
            ClearWebBrowser(this.webBrowser_info, true);
        }

        // parameters:
        //      bRewind �Ƿ�˳���ָ�벦���ͷ��ʼ��ȡ
        void ClearWebBrowser(WebBrowser webBrowser,
            bool bRewind)
        {
            HtmlDocument doc = webBrowser.Document;

            if (doc == null)
            {
                webBrowser.Navigate("about:blank");
                doc = webBrowser.Document;
            }

            doc = doc.OpenNew(true);
            doc.Write("<pre>");
            if (bRewind == true)
                this.CurResultOffs = 0; // ��ͷ��ʼ��ȡ?
        }

        private void BatchTaskForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        private void ToolStripMenuItem_result_Click(object sender, EventArgs e)
        {
            if ((this.MessageStyle & MessageStyle.Result) != 0)
                this.MessageStyle -= MessageStyle.Result;
            else
                this.MessageStyle |= MessageStyle.Result;
        }

        private void ToolStripMenuItem_progress_Click(object sender, EventArgs e)
        {
            if ((this.MessageStyle & MessageStyle.Progress) != 0)
                this.MessageStyle -= MessageStyle.Progress;
            else
                this.MessageStyle |= MessageStyle.Progress;

        }

        // ˢ��
        private void toolStripButton_refresh_Click(object sender, EventArgs e)
        {
            if (m_nInRefresh > 0)
            {
                MessageBox.Show(this, "����ˢ����");
                return;
            }

            DoRefresh();
        }

        // һֱ��ʾ����
        private void toolStripButton_monitoring_CheckedChanged(object sender, EventArgs e)
        {
            if (this.comboBox_taskName.Text == "")
                return;

            StartMonitor(this.comboBox_taskName.Text,
                this.toolStripButton_monitoring.Checked);
        }

        // ��ͷ���»�ȡ
        private void toolStripButton_rewind_Click(object sender, EventArgs e)
        {
            ClearWebBrowser(this.webBrowser_info, true);
        }

        // ���
        private void toolStripButton_clear_Click(object sender, EventArgs e)
        {
            ClearWebBrowser(this.webBrowser_info, false);
        }

        // ��ͣ
        private void toolStripButton_pauseAll_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = PauseAllBatchTask(out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
            else
                MessageBox.Show(this, "��ͣȫ������������ɹ�");

        }

        // ����
        private void toolStripButton_continue_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = ContinueAllBatchTask(out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
            else
                MessageBox.Show(this, "����ȫ������������ɹ�");
        }


    }

    /// <summary>
    /// ��Ϣ���
    /// </summary>
    [Flags]
    public enum MessageStyle
    {
        /// <summary>
        /// �ۻ�����
        /// </summary>
        Result = 0x01,  // �ۻ�����

        /// <summary>
        /// ����
        /// </summary>
        Progress = 0x02,    // ����
    }
}