using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Web;
using System.IO;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Marc;
using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.Text;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// �ᴰ / ������ / �ڴ� / ��ע��
    /// </summary>
    public partial class ItemInfoForm : MyForm
    {
        // 
        /// <summary>
        /// ���ݿ�����
        /// </summary>
        string m_strDbType = "item";  // comment order issue

        /// <summary>
        /// ���ݿ����͡�Ϊ item / order / issue / comment ֮һ
        /// </summary>
        public string DbType
        {
            get
            {
                return this.m_strDbType;
            }
            set
            {
                this.m_strDbType = value;

                if (this.m_strDbType == "comment")
                    this.toolStripButton_addSubject.Visible = true;
                else
                    this.toolStripButton_addSubject.Visible = false;

                this.Text = this.DbTypeCaption;
                this.comboBox_from.Items.Clear();   // ��ʹ����
            }
        }

        /// <summary>
        /// ��ǰ�Ѿ�װ�صļ�¼·��
        /// </summary>
        public string ItemRecPath = ""; // ��ǰ�Ѿ�װ�صĲ��¼·��
        /// <summary>
        /// ��ǰ��װ�ص���Ŀ��¼·��
        /// </summary>
        public string BiblioRecPath = "";   // ��ǰ��װ�ص���Ŀ��¼·��

        const int WM_LOAD_RECORD = API.WM_USER + 200;
        const int WM_PREV_RECORD = API.WM_USER + 201;
        const int WM_NEXT_RECORD = API.WM_USER + 202;

        Commander commander = null;
        WebExternalHost m_webExternalHost_item = new WebExternalHost();
        WebExternalHost m_webExternalHost_biblio = new WebExternalHost();

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;
#endif

        /// <summary>
        /// ���캯��
        /// </summary>
        public ItemInfoForm()
        {
            InitializeComponent();
        }

        private void ItemInfoForm_Load(object sender, EventArgs e)
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

            // webbrowser
            this.m_webExternalHost_item.Initial(this.MainForm, this.webBrowser_itemHTML);
            this.webBrowser_itemHTML.ObjectForScripting = this.m_webExternalHost_item;

            this.m_webExternalHost_biblio.Initial(this.MainForm, this.webBrowser_biblio);
            this.webBrowser_biblio.ObjectForScripting = this.m_webExternalHost_biblio;

            this.commander = new Commander(this);
            this.commander.IsBusy -= new IsBusyEventHandler(commander_IsBusy);
            this.commander.IsBusy += new IsBusyEventHandler(commander_IsBusy);

            this.Text = this.DbTypeCaption;
        }

        void commander_IsBusy(object sender, IsBusyEventArgs e)
        {
            e.IsBusy = this.m_webExternalHost_item.ChannelInUse || this.m_webExternalHost_biblio.ChannelInUse;
        }
#if NO
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }
#endif

        private void ItemInfoForm_FormClosing(object sender, FormClosingEventArgs e)
        {
#if NO
            if (stop != null)
            {
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }

            }
#endif
        }

        private void ItemInfoForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.commander.Destroy();

            if (this.m_webExternalHost_item != null)
                this.m_webExternalHost_item.Destroy();
            if (this.m_webExternalHost_biblio != null)
                this.m_webExternalHost_biblio.Destroy();

#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
            MainForm.AppInfo.SaveMdiChildFormStates(this,
   "mdi_form_state");
#endif
        }

        /*
        void SetXmlToWebbrowser(WebBrowser webbrowser,
            string strXml)
        {
            string strTargetFileName = MainForm.DataDir + "\\xml.xml";

            StreamWriter sw = new StreamWriter(strTargetFileName,
                false,	// append
                System.Text.Encoding.UTF8);
            sw.Write(strXml);
            sw.Close();

            webbrowser.Navigate(strTargetFileName);
        }
         * */

        /// <summary>
        /// ����װ�ص�ǰ��¼
        /// </summary>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int Reload()
        {
            return LoadRecordByRecPath(this.ItemRecPath, "");
        }

        // 
        /// <summary>
        /// ���ݲ�����ţ�װ����¼����Ŀ��¼
        /// ����ʽֻ�ܵ� DbType Ϊ "item" ʱ����
        /// </summary>
        /// <param name="strItemBarcode">�������</param>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int LoadRecord(string strItemBarcode)
        {
            Debug.Assert(this.m_strDbType == "item", "");

            string strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڳ�ʼ���������� ...");
            stop.BeginLoop();


            this.Update();
            this.MainForm.Update();


            Global.ClearHtmlPage(this.webBrowser_itemHTML,
                this.MainForm.DataDir);
            Global.ClearHtmlPage(this.webBrowser_itemXml,
                this.MainForm.DataDir);
            Global.ClearHtmlPage(this.webBrowser_biblio,
                this.MainForm.DataDir);
            // this.textBox_message.Text = "";
            this.toolStripLabel_message.Text = "";

            stop.SetMessage("����װ����¼ " + strItemBarcode + " ...");


            try
            {
                string strItemText = "";
                string strBiblioText = "";

                string strItemRecPath = "";
                string strBiblioRecPath = "";

                byte[] item_timestamp = null;

                long lRet = Channel.GetItemInfo(
                    stop,
                    strItemBarcode,
                    "html",
                    out strItemText,
                    out strItemRecPath,
                    out item_timestamp,
                    "html",
                    out strBiblioText,
                    out strBiblioRecPath,
                    out strError);
                if (lRet == -1 || lRet == 0)
                    goto ERROR1;

                this.ItemRecPath = strItemRecPath;    // 2009/10/18
                this.BiblioRecPath = strBiblioRecPath;  // 2013/3/4

                if (lRet > 1)
                {
                    this.textBox_queryWord.Text = strItemBarcode;
                    this.comboBox_from.Text = "������";

                    strError = "������� '" + strItemBarcode + "' ��������" + lRet.ToString() + " �����¼�����ǵ�·�����£�" + strItemRecPath + "��װ�������������\r\n\r\n����һ�����صĴ����뾡����ϵϵͳ����Ա��������⡣\r\n\r\n��Ҫװ�����е��κ�һ��������ü�¼·����ʽװ�롣";
                    goto ERROR1;
                }

#if NO
                Global.SetHtmlString(this.webBrowser_itemHTML,
                    strItemText,
                    this.MainForm.DataDir,
                    "iteminfoform_item");
#endif
                this.m_webExternalHost_item.SetHtmlString(strItemText,
                    "iteminfoform_item");

                if (String.IsNullOrEmpty(strBiblioText) == true)
                    Global.SetHtmlString(this.webBrowser_biblio,
                        "(��Ŀ��¼ '" + strBiblioRecPath + "' ������)");
                else
                {
#if NO
                    Global.SetHtmlString(this.webBrowser_biblio,
                        strBiblioText,
                        this.MainForm.DataDir,
                        "iteminfoform_biblio");
#endif
                    this.m_webExternalHost_biblio.SetHtmlString(strBiblioText,
                        "iteminfoform_biblio");
                }

                // this.textBox_message.Text = "���¼·��: " + strItemRecPath + " �����������(��Ŀ)��¼·��: " + strBiblioRecPath;
                this.toolStripLabel_message.Text = this.DbTypeCaption + "��¼·��: " + strItemRecPath + " �����������(��Ŀ)��¼·��: " + strBiblioRecPath;

                this.textBox_queryWord.Text = strItemBarcode;
                this.comboBox_from.Text = "�������";

                // �����item xml
                lRet = Channel.GetItemInfo(
                    stop,
                    strItemBarcode,
                    "xml",
                    out strItemText,
                    out strItemRecPath,
                    out item_timestamp,
                    null,   // "html",
                    out strBiblioText,
                    out strBiblioRecPath,
                    out strError);
                if (lRet == -1 || lRet == 0)
                {
                    Global.SetHtmlString(this.webBrowser_itemXml,
                        HttpUtility.HtmlEncode(strError));
                }
                else
                {
                    /*
                    SetXmlToWebbrowser(this.webBrowser_itemXml,
                        strItemText);
                     * */
                    // �� XML �ַ���װ��һ��Web������ؼ�
                    // ��������ܹ���Ӧ"<root ... />"������û��prolog��XML����
                    Global.SetXmlToWebbrowser(this.webBrowser_itemXml,
                        this.MainForm.DataDir,
                        "xml",
                        strItemText);
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);

                this.textBox_queryWord.SelectAll();
                this.textBox_queryWord.Focus();
            }

            return 1;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        /// <summary>
        /// ���ݿ����͵���ʾ����
        /// </summary>
        public string DbTypeCaption
        {
            get
            {
                if (this.m_strDbType == "item")
                    return "��";
                else if (this.m_strDbType == "comment")
                    return "��ע";
                else if (this.m_strDbType == "order")
                    return "����";
                else if (this.m_strDbType == "issue")
                    return "��";
                else
                    throw new Exception("δ֪��DbType '" + this.m_strDbType + "'");
            }
        }

        // 
        /// <summary>
        /// ���ݲ�/����/��/��ע��¼·����װ�������¼����Ŀ��¼
        /// </summary>
        /// <param name="strItemRecPath">�����¼·��</param>
        /// <param name="strPrevNextStyle">ǰ�󷭶����</param>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int LoadRecordByRecPath(string strItemRecPath,
            string strPrevNextStyle)
        {
            string strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڳ�ʼ���������� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            bool bPrevNext = false;

            string strRecPath = strItemRecPath;

            // 2009/10/18
            if (String.IsNullOrEmpty(strPrevNextStyle) == false)
            {
                strRecPath += "$" + strPrevNextStyle.ToLower();
                bPrevNext = true;
            }

            if (bPrevNext == false)
            {
                Global.ClearHtmlPage(this.webBrowser_itemHTML,
                    this.MainForm.DataDir);
                Global.ClearHtmlPage(this.webBrowser_itemXml,
                    this.MainForm.DataDir);
                Global.ClearHtmlPage(this.webBrowser_biblio,
                    this.MainForm.DataDir);
                // this.textBox_message.Text = "";
                this.toolStripLabel_message.Text = "";
            }

            stop.SetMessage("����װ��"+this.DbTypeCaption+"��¼ " + strItemRecPath + " ...");


            try
            {
                string strItemText = "";
                string strBiblioText = "";

                string strOutputItemRecPath = "";
                string strBiblioRecPath = "";

                byte[] item_timestamp = null;

                string strBarcode = "@path:" + strRecPath;

                long lRet = 0;
                
                if (this.m_strDbType == "item")
                lRet = Channel.GetItemInfo(
                     stop,
                     strBarcode,
                     "html",
                     out strItemText,
                     out strOutputItemRecPath,
                     out item_timestamp,
                     "html",
                     out strBiblioText,
                     out strBiblioRecPath,
                     out strError);
                else if (this.m_strDbType == "comment")
                    lRet = Channel.GetCommentInfo(
                         stop,
                         strBarcode,    // "@path:" + strItemRecPath,
                         // "",
                         "html",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "html",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else if (this.m_strDbType == "order")
                    lRet = Channel.GetOrderInfo(
                         stop,
                         strBarcode,    // "@path:" + strItemRecPath,
                         // "",
                         "html",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "html",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else if (this.m_strDbType == "issue")
                    lRet = Channel.GetIssueInfo(
                         stop,
                         strBarcode,    // "@path:" + strItemRecPath,
                         // "",
                         "html",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "html",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else
                    throw new Exception("δ֪��DbType '" + this.m_strDbType + "'");


                if (lRet == -1 || lRet == 0)
                {


                    if (bPrevNext == true
                        && this.Channel.ErrorCode == DigitalPlatform.CirculationClient.localhost.ErrorCode.NotFound)
                    {
                        strError += "\r\n\r\n�¼�¼û��װ�أ������л�������װ��ǰ�ļ�¼";
                        goto ERROR1;
                    }


                    this.ItemRecPath = strOutputItemRecPath;    // 2011/9/5
                    this.BiblioRecPath = strBiblioRecPath;  // 2013/3/4
#if NO
                    Global.SetHtmlString(this.webBrowser_itemHTML,
    strError,
    this.MainForm.DataDir,
    "iteminfoform_item");
#endif
                    this.m_webExternalHost_item.SetHtmlString(strError,
    "iteminfoform_item");

                }
                else
                {
                    this.ItemRecPath = strOutputItemRecPath;    // 2009/10/18
                    this.BiblioRecPath = strBiblioRecPath;  // 2013/3/4

#if NO
                    Global.SetHtmlString(this.webBrowser_itemHTML,
                        strItemText,
                        this.MainForm.DataDir,
                        "iteminfoform_item");
#endif
                    this.m_webExternalHost_item.SetHtmlString(strItemText,
                        "iteminfoform_item");
                }

                if (String.IsNullOrEmpty(strBiblioText) == true)
                    Global.SetHtmlString(this.webBrowser_biblio,
                        "(��Ŀ��¼ '" + strBiblioRecPath + "' ������)");
                else
                {
#if NO
                    Global.SetHtmlString(this.webBrowser_biblio,
                        strBiblioText,
                        this.MainForm.DataDir,
                        "iteminfoform_biblio");
#endif
                    this.m_webExternalHost_biblio.SetHtmlString(strBiblioText,
                        "iteminfoform_biblio");
                }

                // this.textBox_message.Text = "���¼·��: " + strOutputItemRecPath + " �����������(��Ŀ)��¼·��: " + strBiblioRecPath;
                this.toolStripLabel_message.Text = this.DbTypeCaption+"��¼·��: " + strOutputItemRecPath + " �����������(��Ŀ)��¼·��: " + strBiblioRecPath;
                this.textBox_queryWord.Text = this.ItemRecPath; // strItemRecPath;
                this.comboBox_from.Text = this.DbTypeCaption+"��¼·��";

                // �����item xml
                if (this.m_strDbType == "item")
                lRet = Channel.GetItemInfo(
                    stop,
                    "@path:" + strOutputItemRecPath, // strBarcode,
                    "xml",
                    out strItemText,
                    out strItemRecPath,
                    out item_timestamp,
                    null,   // "html",
                    out strBiblioText,
                    out strBiblioRecPath,
                    out strError);
                else if (this.m_strDbType == "comment")
                    lRet = Channel.GetCommentInfo(
                         stop,
                         "@path:" + strOutputItemRecPath,
                         // "",
                         "xml",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else if (this.m_strDbType == "order")
                    lRet = Channel.GetOrderInfo(
                         stop,
                         "@path:" + strOutputItemRecPath,
                         // "",
                         "xml",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else if (this.m_strDbType == "issue")
                    lRet = Channel.GetIssueInfo(
                         stop,
                         "@path:" + strOutputItemRecPath,
                         // "",
                         "xml",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else
                    throw new Exception("δ֪��DbType '" + this.m_strDbType + "'");


                if (lRet == -1 || lRet == 0)
                {
                    Global.SetHtmlString(this.webBrowser_itemXml,
                        HttpUtility.HtmlEncode(strError));
                }
                else
                {
                    /*
                    SetXmlToWebbrowser(this.webBrowser_itemXml,
                        strItemText);
                     * */
                    // �� XML �ַ���װ��һ��Web������ؼ�
                    // ��������ܹ���Ӧ"<root ... />"������û��prolog��XML����
                    Global.SetXmlToWebbrowser(this.webBrowser_itemXml,
                        this.MainForm.DataDir,
                        "xml",
                        strItemText);
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
            MessageBox.Show(this, strError);
            return -1;
        }

#if NO
        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }
#endif

        void SetMenuItemState()
        {
            // �˵�

            // ��������ť

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;

            this.MainForm.toolButton_refresh.Enabled = true;
        }

        private void ItemInfoForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            SetMenuItemState();
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_LOAD_RECORD:
                    this.toolStrip1.Enabled = false;
                    try
                    {
                        if (this.m_webExternalHost_item.CanCallNew(
                            this.commander,
                            m.Msg) == true
                            && this.m_webExternalHost_biblio.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            DoLoadRecord();
                        }
                    }
                    finally
                    {
                        this.toolStrip1.Enabled = true;
                    }
                    return;
                case WM_PREV_RECORD:
                    this.toolStrip1.Enabled = false;
                    try
                    {
                        if (this.m_webExternalHost_item.CanCallNew(
                            this.commander,
                            m.Msg) == true
                            && this.m_webExternalHost_biblio.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            LoadRecordByRecPath(this.ItemRecPath, "prev");
                        }
                    }
                    finally
                    {
                        this.toolStrip1.Enabled = true;
                    }
                    return;
                case WM_NEXT_RECORD:
                    this.toolStrip1.Enabled = false;
                    try
                    {
                        if (this.m_webExternalHost_item.CanCallNew(
                            this.commander,
                            m.Msg) == true
                            && this.m_webExternalHost_biblio.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            LoadRecordByRecPath(this.ItemRecPath, "next");
                        }
                    }
                    finally
                    {
                        this.toolStrip1.Enabled = true;
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            if (this.textBox_queryWord.Text == "")
            {
                MessageBox.Show(this, "��δ���������");
                return;
            }

            this.toolStrip1.Enabled = false;
            this.button_load.Enabled = false;

            this.m_webExternalHost_item.StopPrevious();
            this.webBrowser_itemHTML.Stop();

            this.m_webExternalHost_biblio.StopPrevious();
            this.webBrowser_biblio.Stop();

            this.commander.AddMessage(WM_LOAD_RECORD);
        }

        private void DoLoadRecord()
        {
            string strError;
            if (this.textBox_queryWord.Text == "")
            {
                strError = "��δ���������";
                goto ERROR1;
            }

            if (this.comboBox_from.Text == "������"
                || this.comboBox_from.Text == "�������")
            {
                if (this.m_strDbType != "item")
                {
                    strError = "ֻ�ܵ�DbTypeΪitemʱ����ʹ�� ������� ����;��";
                    goto ERROR1;
                }
                int nRet = this.textBox_queryWord.Text.IndexOf("/");
                if (nRet != -1)
                {
                    strError = "������ļ������ƺ�Ϊһ����¼·���������ǲ������";
                    MessageBox.Show(this, strError);
                }

                LoadRecord(this.textBox_queryWord.Text);
            }
            else if (this.comboBox_from.Text == this.DbTypeCaption + "��¼·��")
            {
                int nRet = this.textBox_queryWord.Text.IndexOf("/");
                if (nRet == -1)
                {
                    strError = "������ļ������ƺ�Ϊһ��������ţ�������"+this.DbTypeCaption+"��¼·��";
                    MessageBox.Show(this, strError);
                }

                // LoadRecord("@path:" + this.textBox_queryWord.Text);
                LoadRecordByRecPath(this.textBox_queryWord.Text, "");
            }
            else
            {
                strError = "�޷�ʶ��ļ���;�� '" + this.comboBox_from.Text + "'";
                goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.comboBox_from.Enabled = bEnable;
            this.textBox_queryWord.Enabled = bEnable;
            this.button_load.Enabled = bEnable;
            this.toolStrip1.Enabled = bEnable;  // ����ʹ�ù������ϵ����ť
        }

        private void toolStripButton_prevRecord_Click(object sender, EventArgs e)
        {
            this.toolStrip1.Enabled = false;
            this.button_load.Enabled = false;

            this.m_webExternalHost_item.StopPrevious();
            this.webBrowser_itemHTML.Stop();

            this.m_webExternalHost_biblio.StopPrevious();
            this.webBrowser_biblio.Stop();

            this.commander.AddMessage(WM_PREV_RECORD);
        }

        private void toolStripButton_nextRecord_Click(object sender, EventArgs e)
        {
            this.toolStrip1.Enabled = false;
            this.button_load.Enabled = false;

            this.m_webExternalHost_item.StopPrevious();
            this.webBrowser_itemHTML.Stop();

            this.m_webExternalHost_biblio.StopPrevious();
            this.webBrowser_biblio.Stop();

            this.commander.AddMessage(WM_NEXT_RECORD);

        }

        private void comboBox_from_DropDown(object sender, EventArgs e)
        {
            this.comboBox_from.Items.Clear();

            if (this.m_strDbType == "item")
                this.comboBox_from.Items.Add("�������");

            this.comboBox_from.Items.Add(this.DbTypeCaption + "��¼·��");
        }

        // �������ɴ�
        private void toolStripButton_addSubject_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ȡ��Ŀ��¼ ...");
            stop.BeginLoop();

            try
            {
                List<string> reserve_subjects = null;
                List<string> exist_subjects = null;
                byte[] biblio_timestamp = null;
                string strBiblioXml = "";

                nRet = GetExistSubject(
                    this.BiblioRecPath,
                    out strBiblioXml,
                    out reserve_subjects,
                    out exist_subjects,
                    out biblio_timestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                string strCommentState = "";
                string strNewSubject = "";
                byte[] item_timestamp = null;
                nRet = GetCommentContent(this.ItemRecPath,
            out strNewSubject,
            out strCommentState,
            out item_timestamp,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                AddSubjectDialog dlg = new AddSubjectDialog();
                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.ReserveSubjects = reserve_subjects;
                dlg.ExistSubjects = exist_subjects;
                dlg.HiddenNewSubjects = StringUtil.SplitList(strNewSubject.Replace("\\r", "\n"), '\n');
                if (StringUtil.IsInList("�Ѵ���", strCommentState) == false)
                    dlg.NewSubjects = dlg.HiddenNewSubjects;

                this.MainForm.AppInfo.LinkFormState(dlg, "iteminfoform_addsubjectdialog_state");
                dlg.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(dlg);

                if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return;

                List<string> subjects = new List<string>();
                subjects.AddRange(dlg.ExistSubjects);
                subjects.AddRange(dlg.NewSubjects);

                StringUtil.RemoveDupNoSort(ref subjects);   // ȥ��
                StringUtil.RemoveBlank(ref subjects);   // ȥ����Ԫ��

                // �޸�ָʾ��1Ϊ�յ���Щ 610 �ֶ�
                // parameters:
                //      strSubject  �����޸ĵ����ɴʵ��ܺ͡�������ǰ���ڵĺͱ�����ӵ�
                nRet = ChangeSubject(ref strBiblioXml,
                    subjects,
                    out strError);

                // ������Ŀ��¼
                byte[] output_timestamp = null;
                string strOutputBiblioRecPath = "";
                long lRet = Channel.SetBiblioInfo(
                    stop,
                    "change",
                    this.BiblioRecPath,
                    "xml",
                    strBiblioXml,
                    biblio_timestamp,
                    "",
                    out strOutputBiblioRecPath,
                    out output_timestamp,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                // �޸���ע��¼״̬
                // return:
                //       -1  ����
                //      0   û�з����޸�
                //      1   �������޸�
                nRet = ChangeCommentState(
                    this.BiblioRecPath,
                    this.ItemRecPath,
                    "�Ѵ���",
                    "",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            // ����װ������
            this.Reload();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �޸���ע��״̬
        // return:
        //       -1  ����
        //      0   û�з����޸�
        //      1   �������޸�
        /// <summary>
        /// �޸���ע��¼��״̬�ֶ�
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strCommentRecPath">��ע��¼·��</param>
        /// <param name="strAddList">Ҫ��״̬�ַ����м�����Ӵ��б�</param>
        /// <param name="strRemoveList">Ҫ��״̬�ַ�����ɾ�����Ӵ��б�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        ///       -1  ����
        ///      0   û�з����޸�
        ///      1   �������޸�
        /// </returns>
        public int ChangeCommentState(
            string strBiblioRecPath,
            string strCommentRecPath,
            string strAddList,
            string strRemoveList,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(strCommentRecPath) == true)
            {
                strError = "CommentRecPathΪ��";
                goto ERROR1;
            }

            if (String.IsNullOrEmpty(strBiblioRecPath) == true)
            {
                strError = "strBiblioRecPathΪ��";
                goto ERROR1;
            }

            // ��þɼ�¼
            string strOldXml = "";
            // byte[] timestamp = ByteArray.GetTimeStampByteArray(this.Timestamp);

            string strOutputPath = "";
            byte[] comment_timestamp = null;
            string strBiblio = "";
            string strTempBiblioRecPath = "";
            long lRet = Channel.GetCommentInfo(
null,
"@path:" + strCommentRecPath,
"xml", // strResultType
out strOldXml,
out strOutputPath,
out comment_timestamp,
"recpath",  // strBiblioType
out strBiblio,
out strTempBiblioRecPath,
out strError);
            if (lRet == -1)
            {
                strError = "���ԭ����ע��¼ '" + strCommentRecPath + "' ʱ����: " + strError;
                goto ERROR1;
            }

#if NO
            if (ByteArray.Compare(comment_timestamp, timestamp) != 0)
            {
                strError = "�޸ı��ܾ�����Ϊ��¼ '" + strCommentRecPath + "' �ڱ���ǰ�Ѿ����������޸Ĺ���������װ��";
                goto ERROR1;
            }
#endif


            XmlDocument dom = new XmlDocument();
            if (String.IsNullOrEmpty(strOldXml) == false)
            {
                try
                {
                    dom.LoadXml(strOldXml);
                }
                catch (Exception ex)
                {
                    strError = "װ�ؼ�¼XML����DOMʱ��������: " + ex.Message;
                    goto ERROR1;
                }
            }
            else
                dom.LoadXml("<root/>");

            // �����޸�״̬
            {
                string strState = DomUtil.GetElementText(dom.DocumentElement,
                    "state");
                string strOldState = strState;

                Global.ModifyStateString(ref strState,
    strAddList,
    strRemoveList);

                if (strState == strOldState)
                    return 0;   // û�б�Ҫ�޸�

                DomUtil.SetElementText(dom.DocumentElement,
                    "state", strState);

                // ��<operations>��д���ʵ���Ŀ
                string strComment = "'" + strOldState + "' --> '" + strState + "'";
                nRet = Global.SetOperation(
                    ref dom,
                    "stateModified",
                    Channel.UserName,
                    strComment,
                    true,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            string strNewCommentRecPath = "";
            string strNewXml = "";
            byte[] baNewTimestamp = null;

            {
                strNewCommentRecPath = strCommentRecPath;

                // ����
                nRet = ChangeCommentInfo(
                    strBiblioRecPath,
                    strCommentRecPath,
                    strOldXml,
                    dom.DocumentElement.OuterXml,
                    comment_timestamp,
                    out strNewXml,
                    out baNewTimestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            return 1;
        ERROR1:
            return -1;
        }

        // 
        /// <summary>
        /// �޸�һ����ע��¼
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strCommentRecPath">��ע��¼·��</param>
        /// <param name="strOldXml">��ע��¼�޸�ǰ�� XML</param>
        /// <param name="strCommentXml">��ע��¼Ҫ�޸ĳɵ� XML</param>
        /// <param name="timestamp">�޸�ǰ��ʱ���</param>
        /// <param name="strNewXml">����ʵ�ʱ���ɹ�����ע��¼ XML</param>
        /// <param name="baNewTimestamp">�����޸ĺ��ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int ChangeCommentInfo(
            string strBiblioRecPath,
            string strCommentRecPath,
            string strOldXml,
            string strCommentXml,
            byte[] timestamp,
            out string strNewXml,
            out byte[] baNewTimestamp,
            out string strError)
        {
            strError = "";
            strNewXml = "";
            baNewTimestamp = null;

            EntityInfo info = new EntityInfo();
            info.RefID = Guid.NewGuid().ToString();

            string strTargetBiblioRecID = Global.GetRecordID(strBiblioRecPath);

            XmlDocument comment_dom = new XmlDocument();
            try
            {
                comment_dom.LoadXml(strCommentXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ�ص�DOMʱ��������: " + ex.Message;
                return -1;
            }

            DomUtil.SetElementText(comment_dom.DocumentElement,
                "parent", strTargetBiblioRecID);

            info.Action = "change";
            info.OldRecPath = strCommentRecPath;
            info.NewRecPath = strCommentRecPath;
            info.OldRecord = strOldXml;
            info.OldTimestamp = timestamp;
            info.NewRecord = comment_dom.OuterXml;
            info.NewTimestamp = null;

            // 
            EntityInfo[] comments = new EntityInfo[1];
            comments[0] = info;

            EntityInfo[] errorinfos = null;

            long lRet = Channel.SetComments(
                null,
                strBiblioRecPath,
                comments,
                out errorinfos,
                out strError);
            if (lRet == -1)
            {
                return -1;
            }

            if (errorinfos != null && errorinfos.Length > 0)
            {
                int nErrorCount = 0;
                for (int i = 0; i < errorinfos.Length; i++)
                {
                    EntityInfo error = errorinfos[i];
                    if (error.ErrorCode != ErrorCodeValue.NoError)
                    {
                        if (String.IsNullOrEmpty(strError) == false)
                            strError += "; ";
                        strError += errorinfos[0].ErrorInfo;
                        nErrorCount++;
                    }
                    else
                    {
                        // strNewCommentRecPath = error.NewRecPath;
                        strNewXml = error.NewRecord;
                        baNewTimestamp = error.NewTimestamp;
                    }
                }
                if (nErrorCount > 0)
                {
                    return -1;
                }
            }

            return 1;
        }

        /// <summary>
        /// �����ע��¼����
        /// ��ο� dp2Library API GetCommentInfo() ����ϸ��Ϣ
        /// </summary>
        /// <param name="strCommentRecPath">��ע��¼·��</param>
        /// <param name="strContent">��������</param>
        /// <param name="strState">����״̬</param>
        /// <param name="item_timestamp">����ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        int GetCommentContent(string strCommentRecPath,
            out string strContent,
            out string strState,
            out byte[] item_timestamp,
            out string strError)
        {
            strError = "";
            strContent = "";
            strState = "";
            item_timestamp = null;

            string strCommentXml = "";
            string strOutputItemRecPath = "";
            string strBiblioText = "";
            string strBiblioRecPath = "";
            long lRet = Channel.GetCommentInfo(
     stop,
     "@path:" + strCommentRecPath,
                // "",
     "xml",
     out strCommentXml,
     out strOutputItemRecPath,
     out item_timestamp,
     null,
     out strBiblioText,
     out strBiblioRecPath,
     out strError);
            if (lRet == -1)
                return -1;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strCommentXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ�����: " + ex.Message;
                return -1;
            }

            strState = DomUtil.GetElementText(dom.DocumentElement, "state");
            strContent = DomUtil.GetElementText(dom.DocumentElement, "content");
            return 0;
        }

        // �޸�ָʾ��1Ϊ�յ���Щ 610 �ֶ�
        // parameters:
        //      subjects  �����޸ĵ����ɴʵ��ܺ͡�������ǰ���ڵĺͱ�����ӵ�
        static int ChangeSubject(ref string strBiblioXml,
            List<string> subjects,
            out string strError)
        {
            strError = "";

            // �������ȥ��

            string strMARC = "";
            string strMarcSyntax = "";
            // ��XML��ʽת��ΪMARC��ʽ
            // �Զ������ݼ�¼�л��MARC�﷨
            int nRet = MarcUtil.Xml2Marc(strBiblioXml,
                true,
                null,
                out strMarcSyntax,
                out strMARC,
                out strError);
            if (nRet == -1)
            {
                strError = "XMLת����MARC��¼ʱ����: " + strError;
                return -1;
            }

            nRet = ChangeSubject(ref strMARC,
                strMarcSyntax,
                subjects,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = MarcUtil.Marc2XmlEx(strMARC,
                strMarcSyntax,
                ref strBiblioXml,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        /// <summary>
        /// �����ṩ��������ַ��� �޸� MARC ��¼�е� 610 �� 653 �ֶ�
        /// </summary>
        /// <param name="strMARC">Ҫ������ MARC ��¼�ַ��������ڸ�ʽ</param>
        /// <param name="strMarcSyntax">MARC ��ʽ</param>
        /// <param name="subjects">������ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public static int ChangeSubject(ref string strMARC,
            string strMarcSyntax,
            List<string> subjects,
            out string strError)
        {
            strError = "";

            MarcRecord record = new MarcRecord(strMARC);
            MarcNodeList nodes = null;
            if (strMarcSyntax == "unimarc")
                nodes = record.select("field[@name='610' and @indicator1=' ']");
            else if (strMarcSyntax == "usmarc")
                nodes = record.select("field[@name='653' and @indicator1=' ']");
            else
            {
                strError = "δ֪�� MARC ��ʽ���� '" + strMarcSyntax + "'";
                return -1;
            }

            if (subjects == null || subjects.Count == 0)
            {
                // ɾ����Щ����ɾ���� 610 �ֶ�
                foreach (MarcNode node in nodes)
                {
                    MarcNodeList subfields = node.select("subfield[@name='a']");
                    if (subfields.count == node.ChildNodes.count)
                    {
                        // ������� $a ����û�������κ����ֶΣ����ֶο���ɾ��
                        node.detach();
                    }
                }
            }
            else
            {

                MarcNode field610 = null;

                // ֻ����һ�� 610 �ֶ�
                if (nodes.count > 1)
                {
                    int nCount = nodes.count;
                    foreach (MarcNode node in nodes)
                    {
                        MarcNodeList subfields = node.select("subfield[@name='a']");
                        if (subfields.count == node.ChildNodes.count)
                        {
                            // ������� $a ����û�������κ����ֶΣ����ֶο���ɾ��
                            node.detach();
                            nCount--;
                        }

                        if (nCount <= 1)
                            break;
                    }

                    // ����ѡ��
                    if (strMarcSyntax == "unimarc")
                        nodes = record.select("field[@name='610' and @indicator1=' ']");
                    else if (strMarcSyntax == "usmarc")
                        nodes = record.select("field[@name='653' and @indicator1=' ']");

                    field610 = nodes[0];
                }
                else if (nodes.count == 0)
                {
                    // ����һ���µ� 610 �ֶ�
                    if (strMarcSyntax == "unimarc")
                        field610 = new MarcField("610", "  ");
                    else if (strMarcSyntax == "usmarc")
                        field610 = new MarcField("653", "  ");

                    record.ChildNodes.insertSequence(field610);
                }
                else
                {
                    Debug.Assert(nodes.count == 1, "");
                    field610 = nodes[0];
                }

                // ɾ��ȫ�� $a ���ֶ�
                field610.select("subfield[@name='a']").detach();


                // ������ɸ� $a ���ֶ�
                Debug.Assert(subjects.Count > 0, "");
                MarcNodeList source = new MarcNodeList();
                for (int i = 0; i < subjects.Count; i++)
                {
                    source.add(new MarcSubfield("a", subjects[i]));
                }
                // Ѱ���ʵ�λ�ò���
                field610.ChildNodes.insertSequence(source[0]);
                if (source.count > 1)
                {
                    // �ڸղ���Ķ�������������Ķ���
                    MarcNodeList list = new MarcNodeList(source[0]);
                    source.removeAt(0); // �ų��ղ����һ��
                    list.after(source);
                }
            }

            strMARC = record.Text;
            return 0;
        }

        // parameters:
        //      reserve_subjects   ���������ɴʡ�ָָʾ��1Ϊ 0/1/2 �����ɴʡ���Щ���ɴʲ��öԻ����޸�(������ MARC �༭���޸�)
        //      subjects          ���޸ĵ����ɴʡ�ָʾ��1Ϊ �ա���Щ���ɴ��öԻ����޸�
        int GetExistSubject(
            string strBiblioRecPath,
            out string strBiblioXml,
            out List<string> reserve_subjects,
            out List<string> subjects,
            out byte [] timestamp,
            out string strError)
        {
            strError = "";
            reserve_subjects = new List<string>();
            subjects = new List<string>();
            timestamp = null;
            strBiblioXml = "";

            string[] results = null;

            // �����Ŀ��¼
            long lRet = Channel.GetBiblioInfos(
                stop,
                strBiblioRecPath,
                "",
                new string[] { "xml" },   // formats
                out results,
                out timestamp,
                out strError);
            if (lRet == 0)
                return -1;
            if (lRet == -1)
                return -1;

            if (results == null || results.Length == 0)
            {
                strError = "results error";
                return -1;
            }

            strBiblioXml = results[0];

            string strMARC = "";
            string strMarcSyntax = "";
            // ��XML��ʽת��ΪMARC��ʽ
            // �Զ������ݼ�¼�л��MARC�﷨
            int nRet = MarcUtil.Xml2Marc(strBiblioXml,
                true,
                null,
                out strMarcSyntax,
                out strMARC,
                out strError);
            if (nRet == -1)
            {
                strError = "XMLת����MARC��¼ʱ����: " + strError;
                return -1;
            }

            nRet = GetSubjectInfo(strMARC,
                strMarcSyntax,
                out reserve_subjects,
                out subjects,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        /// <summary>
        /// �� MARC �ַ����л���������Ϣ
        /// </summary>
        /// <param name="strMARC">MARC �ַ��������ڸ�ʽ</param>
        /// <param name="strMarcSyntax">MARC ��ʽ</param>
        /// <param name="reserve_subjects">����Ҫ����������ʼ��ϡ��ֶ�ָʾ��1 ��Ϊ�յ�</param>
        /// <param name="subjects">��������ʼ��ϡ��ֶ�ָʾ��1 Ϊ�յ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public static int GetSubjectInfo(string strMARC,
            string strMarcSyntax,
            out List<string> reserve_subjects,
            out List<string> subjects,
            out string strError)
        {
            strError = "";
            reserve_subjects = new List<string>();
            subjects = new List<string>();

            MarcRecord record = new MarcRecord(strMARC);
            MarcNodeList nodes = null;
            if (strMarcSyntax == "unimarc")
                nodes = record.select("field[@name='610']/subfield[@name='a']");
            else if (strMarcSyntax == "usmarc")
                nodes = record.select("field[@name='653']/subfield[@name='a']");
            else
            {
                strError = "δ֪�� MARC ��ʽ���� '" + strMarcSyntax + "'";
                return -1;
            }

            foreach (MarcNode node in nodes)
            {
                if (string.IsNullOrEmpty(node.Content.Trim()) == true)
                    continue;

                Debug.Assert(node.NodeType == NodeType.Subfield, "");

                if (node.Parent.Indicator1 == ' ')
                    subjects.Add(node.Content.Trim());
                else
                    reserve_subjects.Add(node.Content.Trim());
            }

            return 0;
        }
    }
}