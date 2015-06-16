using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using System.IO;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// ��ݵǼǴ�
    /// </summary>
    public partial class PassGateForm : MyForm
    {
        Commander commander = null;

        WebExternalHost m_webExternalHost = new WebExternalHost();

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;
#endif

        ReaderWriterLock m_lock = new ReaderWriterLock();
        static int m_nLockTimeout = 5000;	// 5000=5��

        internal Thread threadWorker = null;
        internal AutoResetEvent eventClose = new AutoResetEvent(false);	// true : initial state is signaled 
        internal AutoResetEvent eventActive = new AutoResetEvent(false);	// �����ź�
        internal AutoResetEvent eventFinished = new AutoResetEvent(false);	// true : initial state is signaled 

        /// <summary>
        /// ��ѯ�ļ��ʱ�䣬��λ�� 1/1000 �롣ȱʡΪ 1 ����
        /// </summary>
        public int PerTime = 1 * 60 * 1000;	// 1����?
        internal bool m_bClosed = true;

        int m_nTail = 0;

        string HtmlString = "";

        const int WM_SETHTML = API.WM_USER + 201;
        const int WM_RESTOREFOCUS = API.WM_USER + 202;

        bool m_bActive = false;

        /// <summary>
        /// ���캯��
        /// </summary>
        public PassGateForm()
        {
            InitializeComponent();
        }



        private void PassGateForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

#if NO
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            // webbrowser
            this.m_webExternalHost.Initial(this.MainForm, this.webBrowser_readerInfo);
            this.webBrowser_readerInfo.ObjectForScripting = this.m_webExternalHost;

            this.commander = new Commander(this);
            this.commander.IsBusy -= new IsBusyEventHandler(commander_IsBusy);
            this.commander.IsBusy += new IsBusyEventHandler(commander_IsBusy);



            this.AcceptButton = this.button_passGate;

            this.textBox_gateName.Text = this.MainForm.AppInfo.GetString(
                "passgate_form",
                "gate_name",
                "");
            this.checkBox_displayReaderDetailInfo.Checked = this.MainForm.AppInfo.GetBoolean(
                "passgate_form",
                "display_reader_detail_info",
                true);
            this.checkBox_hideBarcode.Checked = this.MainForm.AppInfo.GetBoolean(
                "passgate_form",
                "hide_barcode",
                false);
            this.checkBox_hideReaderName.Checked = this.MainForm.AppInfo.GetBoolean(
                "passgate_form",
                "hide_readername",
                false);


            this.StartWorkerThread();
        }

        void commander_IsBusy(object sender, IsBusyEventArgs e)
        {
            e.IsBusy = this.m_webExternalHost.ChannelInUse;
        }

        private void PassGateForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.CloseThread();

            if (this.m_webExternalHost != null)
                this.m_webExternalHost.Destroy();

#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            this.MainForm.AppInfo.SetString(
                "passgate_form",
                "gate_name",
                this.textBox_gateName.Text);

            this.MainForm.AppInfo.SetBoolean(
                "passgate_form",
                "display_reader_detail_info",
                this.checkBox_displayReaderDetailInfo.Checked);
            this.MainForm.AppInfo.SetBoolean(
                "passgate_form",
                "hide_barcode",
                this.checkBox_hideBarcode.Checked);
            this.MainForm.AppInfo.SetBoolean(
                "passgate_form",
                "hide_readername",
                this.checkBox_hideReaderName.Checked); 

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

        void ClearList()
        {
            this.listView_list.Items.Clear();
        }

        // �ύ����֤�����
        private void button_passGate_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (String.IsNullOrEmpty(this.textBox_readerBarcode.Text) == true)
            {
                strError = "���������֤�����";
                goto ERROR1;
            }

            int nMaxListItemsCount = this.MaxListItemsCount;

            ListViewItem item = new ListViewItem();

            if (this.checkBox_hideBarcode.Checked == true)
            {
                string strText = "";
                item.Text = strText.PadLeft(this.textBox_readerBarcode.Text.Length, '*');
            }
            else
                item.Text = this.textBox_readerBarcode.Text;

            ReaderInfo info = new ReaderInfo();
            info.ReaderBarcode = this.textBox_readerBarcode.Text;
            item.Tag = info;

            item.SubItems.Add("");
            item.SubItems.Add("");
            item.SubItems.Add(DateTime.Now.ToString());

            item.ImageIndex = 0;    // ��δ�������

            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                if (nMaxListItemsCount != -1
                    && this.listView_list.Items.Count > nMaxListItemsCount)
                    ClearList();

                this.listView_list.Items.Add(item);
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

            item.EnsureVisible();

            this.eventActive.Set(); // ���߹����߳�

            this.textBox_readerBarcode.SelectAll();
            this.textBox_readerBarcode.Focus();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            this.textBox_readerBarcode.SelectAll();
            this.textBox_readerBarcode.Focus();
        }

        // ���������߳�
        /*public*/ void StartWorkerThread()
        {
            this.m_bClosed = false;

            this.eventActive.Set();
            this.eventClose.Reset(); 

            this.threadWorker =
                new Thread(new ThreadStart(this.ThreadMain));
            this.threadWorker.Start();
        }

        /*public*/ void CloseThread()
        {
            this.eventClose.Set();
            this.m_bClosed = true;

        }

        /*public*/ void Stop()
        {
            this.eventClose.Set();
            this.m_bClosed = true;
        }

        // �����߳�
        /*public virtual*/ void ThreadMain()
        {
            try
            {
                WaitHandle[] events = new WaitHandle[2];

                events[0] = eventClose;
                events[1] = eventActive;

                while (true)
                {
                    int index = WaitHandle.WaitAny(events, PerTime, false);

                    if (index == WaitHandle.WaitTimeout)
                    {
                        // ��ʱ
                        eventActive.Reset();
                        Worker();

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
                    }

                    /*
                    // �Ƿ�ѭ��?
                    if (this.Loop == false)
                        break;
                     * */
                }

                eventFinished.Set();
            }
            catch (Exception ex)
            {
                string strErrorText = "PassGateForm�����̳߳����쳣: " + ExceptionUtil.GetDebugText(ex);
            }

        }

        // listview imageindex 0:��δ��ʼ�� 1:�Ѿ���ʼ�� 2:����

        // �����߳�ÿһ��ѭ����ʵ���Թ���
        /*public*/ void Worker()
        {
            string strError = "";

            for (int i = this.m_nTail; i < this.listView_list.Items.Count; i++)
            {
                ListViewItem item = this.listView_list.Items[i];

                if (item.ImageIndex == 1)
                    continue;

                // string strBarcode = item.Text;
                ReaderInfo info = (ReaderInfo)item.Tag;
                string strBarcode = info.ReaderBarcode;

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڳ�ʼ���������� ...");
                stop.BeginLoop();


                string strTypeList = "xml";
                int nTypeCount = 1;

                if (this.checkBox_displayReaderDetailInfo.Checked == true)
                {
                    strTypeList += ",html";

                    if (this.MainForm.ServerVersion >= 2.25)
                        strTypeList += ":noborrowhistory";

                    nTypeCount = 2;
                }

                try
                {
                    string[] results = null;
                    long lRet = Channel.PassGate(stop,
                        strBarcode,
                        this.textBox_gateName.Text, // strGateName
                        strTypeList,
                        out results,
                        out strError);
                    if (lRet == -1)
                    {
                        OnError(item, strError);
                        goto CONTINUE;
                    }

                    this.textBox_counter.Text = lRet.ToString();

                    if (results.Length != nTypeCount)
                    {
                        strError = "results error...";
                        OnError(item, strError);
                        goto CONTINUE;
                    }

                    string strXml = results[0];

                    string strReaderName = "";
                    string strState = "";
                    int nRet = GetRecordInfo(strXml,
                        out strReaderName,
                        out strState,
                        out strError);
                    if (nRet == -1)
                    {
                        OnError(item, strError);
                        goto CONTINUE;
                    }

                    info.ReaderName = strReaderName;

                    if (this.checkBox_hideReaderName.Checked == true)
                    {
                        string strText = "";
                        item.SubItems[1].Text = strText.PadLeft(strReaderName.Length, '*');
                    }
                    else
                        item.SubItems[1].Text = strReaderName;

                    item.SubItems[2].Text = strState;
                    item.ImageIndex = 1;    // error


                    if (this.checkBox_displayReaderDetailInfo.Checked == true
                        && results.Length == 2)
                    {

                        this.m_webExternalHost.StopPrevious();
                        this.webBrowser_readerInfo.Stop();

                        this.HtmlString = results[1];

                        // this.commander.AddMessage(WM_SETHTML);
                        API.PostMessage(this.Handle, WM_SETHTML, 0, 0);
                    }

                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }

                CONTINUE:

                this.m_nTail = i;
            }

        }

        void OnError(ListViewItem item,
            string strError)
        {
            item.SubItems[1].Text = strError;
            item.ImageIndex = 2;    // error

            this.HtmlString = strError;
            API.PostMessage(this.Handle, WM_SETHTML, 0, 0);

            // ���������Ե�����
            Console.Beep();
        }

        static int GetRecordInfo(string strXml,
            out string strReaderName,
            out string strState,
            out string strError)
        {
            strError = "";
            strReaderName = "";
            strState = "";

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            strReaderName = DomUtil.GetElementText(dom.DocumentElement,
                "name");
            strState = DomUtil.GetElementText(dom.DocumentElement,
                "state");

            return 0;
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_SETHTML:
                    if (this.m_webExternalHost.CanCallNew(
                        this.commander,
                        m.Msg) == true)
                    {
#if NO
                        Global.SetHtmlString(this.webBrowser_readerInfo,
                            this.HtmlString,
                            this.MainForm.DataDir,
                            "passgateform_reader");
#endif
                        this.m_webExternalHost.SetHtmlString(this.HtmlString,
                            "passgateform_reader");
                    }
                    return;
                case WM_RESTOREFOCUS:
                    this.textBox_readerBarcode.Focus();
                    this.textBox_readerBarcode.SelectAll();
                    return;
            }
            base.DefWndProc(ref m);
        }

        private void checkBox_displayReaderDetailInfo_CheckedChanged(object sender, EventArgs e)
        {
#if NO
            Global.SetHtmlString(this.webBrowser_readerInfo,
                "(�հ�)");
#endif
            this.m_webExternalHost.ClearHtmlPage();
        }

        private void checkBox_hideBarcode_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_hideBarcode.Checked == true)
            {
                this.textBox_readerBarcode.PasswordChar = '*';
                this.textBox_readerBarcode.Text = "";

            }
            else
            {
                this.textBox_readerBarcode.PasswordChar = (char)0;
            }
            bool bChecked = this.checkBox_hideBarcode.Checked;
            // �޸�listview����
            for (int i = 0; i < this.listView_list.Items.Count; i++)
            {
                ListViewItem item = this.listView_list.Items[i];

                if (bChecked == true)
                {
                    int nLength = item.Text.Length;
                    string strText = "";
                    item.Text = strText.PadLeft(nLength, '*');
                }
                else
                {
                    ReaderInfo info = (ReaderInfo)item.Tag;
                    item.Text = info.ReaderBarcode;
                }
            }

        }

        private void checkBox_hideReaderName_CheckedChanged(object sender, EventArgs e)
        {
            bool bChecked = this.checkBox_hideReaderName.Checked;
            // �޸�listview����
            for (int i = 0; i < this.listView_list.Items.Count; i++)
            {
                ListViewItem item = this.listView_list.Items[i];

                if (item.ImageIndex == 2)
                    continue;

                if (bChecked == true)
                {
                    int nLength = item.SubItems[1].Text.Length;
                    string strText = "";
                    item.SubItems[1].Text = strText.PadLeft(nLength, '*');
                }
                else
                {
                    ReaderInfo info = (ReaderInfo)item.Tag;
                    item.SubItems[1].Text = info.ReaderName;
                }
            }

        }

        private void textBox_readerBarcode_Enter(object sender, EventArgs e)
        {
            this.MainForm.EnterPatronIdEdit(InputType.PQR);
        }

        private void textBox_readerBarcode_Leave(object sender, EventArgs e)
        {
            this.MainForm.LeavePatronIdEdit();

            if (m_bActive == false)
                return;

            if (Control.ModifierKeys == Keys.Control)
                return;

            API.PostMessage(this.Handle, WM_RESTOREFOCUS, 0, 0);
        }

        private void PassGateForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);

            m_bActive = true;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        private void PassGateForm_Deactivate(object sender, EventArgs e)
        {
            m_bActive = false;
        }

        /// <summary>
        /// �б��е����������ÿ���������������ʱ���б��Զ����һ�Ρ�
        /// -1 ��ʾ������
        /// </summary>
        public int MaxListItemsCount
        {
            get
            {
                return MainForm.AppInfo.GetInt(
                    "passgate_form",
                    "max_list_items_count",
                    1000);
            }
        }

        private void webBrowser_readerInfo_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            ((WebBrowser)sender).Document.Window.Error -= new HtmlElementErrorEventHandler(Window_Error);
            ((WebBrowser)sender).Document.Window.Error += new HtmlElementErrorEventHandler(Window_Error);
        }
        void Window_Error(object sender, HtmlElementErrorEventArgs e)
        {
            if (this.MainForm.SuppressScriptErrors == true)
                e.Handled = true;
        }
    }

    class ReaderInfo
    {
        public string ReaderBarcode = "";
        public string ReaderName = "";
    }
}