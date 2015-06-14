using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.GUI;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Catalog
{
    public partial class ZhongcihaoForm : Form
    {
        // string EncryptKey = "dp2catalog_client_password_key";

        /*
        public string LibraryServerName = "";
        public string LibraryServerUrl = "";
         * */

        public LibraryChannelCollection Channels = null;
        LibraryChannel Channel = null;

        public string Lang = "zh";

        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;

        /// <summary>
        /// ���������ź�
        /// </summary>
        public AutoResetEvent EventFinish = new AutoResetEvent(false);

        string m_strMaxNumber = null;
        string m_strTailNumber = null;

        public bool AutoBeginSearch = false;

        const int WM_INITIAL = API.WM_USER + 201;

        public string MyselfBiblioRecPath = "";    // ����ȡ�ŵ���Ŀ��¼��·��������У��ͳ�ƹ��̣��ų��Լ���


        public ZhongcihaoForm()
        {
            InitializeComponent();

            ListViewProperty prop = new ListViewProperty();
            this.listView_number.Tag = prop;
            // ��һ�����⣬��¼·��
            prop.SetSortStyle(0, ColumnSortStyle.RecPath);
            prop.GetColumnTitles -= new GetColumnTitlesEventHandler(prop_GetColumnTitles);
            prop.GetColumnTitles += new GetColumnTitlesEventHandler(prop_GetColumnTitles);
#if NO
            prop.ParsePath -= new ParsePathEventHandler(prop_ParsePath);
            prop.ParsePath += new ParsePathEventHandler(prop_ParsePath);
#endif

        }

#if NO
        void prop_ParsePath(object sender, ParsePathEventArgs e)
        {
            string strServerName = "";
            string strPurePath = "";
            // ������¼·����
            // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
            dp2SearchForm.ParseRecPath(e.Path,
                out strServerName,
                out strPurePath);

            e.DbName = strServerName + "|" + dp2SearchForm.GetDbName(strPurePath);
        }
#endif

        void prop_GetColumnTitles(object sender, GetColumnTitlesEventArgs e)
        {
            if (e.DbName == "<blank>")
            {
                e.ColumnTitles = new ColumnPropertyCollection();
                e.ColumnTitles.Add("������");
                e.ColumnTitles.Add("����");
                return;
            }

            // e.ColumnTitles = this.MainForm.GetBrowseColumnNames(e.DbName);

            e.ColumnTitles = new ColumnPropertyCollection();
            ColumnPropertyCollection titles = this.GetBrowseColumnNames(e.DbName);
            if (titles == null) // ��������ݿ���
                return;
            e.ColumnTitles.AddRange(titles);  // Ҫ���ƣ���Ҫֱ��ʹ�ã���Ϊ������ܻ��޸ġ���Ӱ�쵽ԭ��

            /*
            if (this.m_bFirstColumnIsKey == true)
                e.ColumnTitles.Insert(0, "���еļ�����");
             * */

        }

        ColumnPropertyCollection GetBrowseColumnNames(string strPrefix)
        {
            dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

            if (dp2_searchform == null)
            {
                // return new List<string>();
                return null;
            }

            return dp2_searchform.dp2ResTree1.GetBrowseColumnNames(this.textBox_serverName.Text, strPrefix);

            /*
            string[] parts = strPrefix.Split(new char[] { '|' });
            if (parts.Length < 2)
                return new List<string>();

            return dp2_searchform.dp2ResTree1.GetBrowseColumnNames(parts[0], parts[1]);
             * */
        }

        dp2SearchForm GetDp2SearchForm()
        {
            dp2SearchForm dp2_searchform = null;

            dp2_searchform = this.MainForm.TopDp2SearchForm;

            if (dp2_searchform == null)
            {
                // �¿�һ��dp2������
                FormWindowState old_state = this.WindowState;

                dp2_searchform = new dp2SearchForm();
                dp2_searchform.MainForm = this.MainForm;
                dp2_searchform.MdiParent = this.MainForm;
                dp2_searchform.WindowState = FormWindowState.Minimized;
                dp2_searchform.Show();

                this.WindowState = old_state;
                this.Activate();

                // ��Ҫ�ȴ���ʼ�������������
                dp2_searchform.WaitLoadFinish();
            }

            return dp2_searchform;
        }

        private void ZhongcihaoForm_Load(object sender, EventArgs e)
        {
            LoadSize();

            this.Channels = new LibraryChannelCollection();
            this.Channels.BeforeLogin += new BeforeLoginEventHandle(Channels_BeforeLogin);


            /*
            // this.Channel.Url = this.MainForm.LibraryServerUrl;
            this.Channel.Url = this.LibraryServerUrl;
            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);
             * */

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������

            // ��������
            if (string.IsNullOrEmpty(this.textBox_serverName.Text) == true)
            {
                this.textBox_serverName.Text = this.MainForm.AppInfo.GetString(
    "zhongcihao_form",
    "servername",
    "");
            }

            // ���
            if (String.IsNullOrEmpty(this.textBox_classNumber.Text) == true)
            {
                this.textBox_classNumber.Text = this.MainForm.AppInfo.GetString(
                    "zhongcihao_form",
                    "classnumber",
                    "");
            }

            // ������Ŀ����
            if (String.IsNullOrEmpty(this.comboBox_biblioDbName.Text) == true)
            {
                this.comboBox_biblioDbName.Text = this.MainForm.AppInfo.GetString(
                    "zhongcihao_form",
                    "biblio_dbname",
                    "");
            }

            // �Ƿ�Ҫ���������
            this.checkBox_returnBrowseCols.Checked = this.MainForm.AppInfo.GetBoolean(
                    "zhongcihao_form",
                    "return_browse_cols",
                    true);


            string strWidths = this.MainForm.AppInfo.GetString(
"zhongcihao_form",
"record_list_column_width",
"");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_number,
                    strWidths,
                    true);
            }

            if (this.AutoBeginSearch == true)
            {
                API.PostMessage(this.Handle, WM_INITIAL, 0, 0);
            }
        }

        void Channels_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            LibraryChannel channel = (LibraryChannel)sender;

            dp2Server server = this.MainForm.Servers[channel.Url];
            if (server == null)
            {
                e.ErrorInfo = "û���ҵ� URL Ϊ " + channel.Url + " �ķ���������";
                e.Failed = true;
                e.Cancel = true;
                return;
            }

            if (e.FirstTry == true)
            {
                e.UserName = server.DefaultUserName;
                e.Password = server.DefaultPassword;
                e.Parameters = "location=dp2Catalog,type=worker";
                /*
                e.IsReader = false;
                e.Location = "dp2Catalog";
                 * */
                // 2014/9/13
                e.Parameters += ",mac=" + StringUtil.MakePathList(SerialCodeForm.GetMacAddress(), "|");

#if SN
                // �����к��л�� expire= ����ֵ
                string strExpire = this.MainForm.GetExpireParam();
                if (string.IsNullOrEmpty(strExpire) == false)
                    e.Parameters += ",expire=" + strExpire;
#endif

                if (String.IsNullOrEmpty(e.UserName) == false)
                    return; // ��������, �Ա�����һ�� ������ �Ի�����Զ���¼
            }

            // 
            IWin32Window owner = this;

            ServerDlg dlg = SetDefaultAccount(
                e.LibraryServerUrl,
                null,
                e.ErrorInfo,
                owner);
            if (dlg == null)
            {
                e.Cancel = true;
                return;
            }


            e.UserName = dlg.UserName;
            e.Password = dlg.Password;
            e.SavePasswordShort = false;
            e.Parameters = "location=dp2Catalog,type=worker";

            /*
            e.IsReader = false;
            e.Location = "dp2Catalog";
             * */
            e.SavePasswordLong = true;
            e.LibraryServerUrl = dlg.ServerUrl;
        }

        ServerDlg SetDefaultAccount(
    string strServerUrl,
    string strTitle,
    string strComment,
    IWin32Window owner)
        {
            dp2Server server = this.MainForm.Servers[strServerUrl];

            ServerDlg dlg = new ServerDlg();
            GuiUtil.SetControlFont(dlg, this.Font);

            if (String.IsNullOrEmpty(strServerUrl) == true)
            {
            }
            else
            {
                dlg.ServerUrl = strServerUrl;
            }

            if (owner == null)
                owner = this;


            if (String.IsNullOrEmpty(strTitle) == false)
                dlg.Text = strTitle;

            dlg.Comment = strComment;
            dlg.UserName = server.DefaultUserName;

            this.MainForm.AppInfo.LinkFormState(dlg,
                "dp2_logindlg_state");

            dlg.ShowDialog(owner);

            this.MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult == DialogResult.Cancel)
            {
                return null;
            }

            server.DefaultUserName = dlg.UserName;
            server.DefaultPassword =
                (dlg.SavePassword == true) ?
                dlg.Password : "";

            server.SavePassword = dlg.SavePassword;

            server.Url = dlg.ServerUrl;
            return dlg;
        }


#if NOOOOOOOOOOOOOOOOOOOOOOOO
        public void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {

            if (e.FirstTry == true)
            {
                e.UserName = this.MainForm.AppInfo.GetString(
                    "default_account",
                    "username",
                    "");
                e.Password = this.MainForm.AppInfo.GetString(
                    "default_account",
                    "password",
                    "");
                e.Password = this.DecryptPasssword(e.Password);

                e.IsReader =
                    this.MainForm.AppInfo.GetBoolean(
                    "default_account",
                    "isreader",
                    false);
                e.Location = this.MainForm.AppInfo.GetString(
                    "default_account",
                    "location",
                    "");
                if (String.IsNullOrEmpty(e.UserName) == false)
                    return; // ��������, �Ա�����һ�� ������ �Ի�����Զ���¼
            }

            // 
            IWin32Window owner = null;

            if (sender is Form)
                owner = (Form)sender;
            else
                owner = this;

            CirculationLoginDlg dlg = SetDefaultAccount(
                e.CirculationServerUrl,
                null,
                e.ErrorInfo,
                owner);
            if (dlg == null)
            {
                e.Cancel = true;
                return;
            }


            e.UserName = dlg.UserName;
            e.Password = dlg.Password;
            e.SavePasswordShort = dlg.SavePasswordShort;
            e.IsReader = dlg.IsReader;
            e.Location = dlg.OperLocation;
            e.SavePasswordLong = dlg.SavePasswordLong;
            e.CirculationServerUrl = dlg.ServerUrl;
        }

        CirculationLoginDlg SetDefaultAccount(
    string strServerUrl,
    string strTitle,
    string strComment,
    IWin32Window owner)
        {
            CirculationLoginDlg dlg = new CirculationLoginDlg();

            if (String.IsNullOrEmpty(strServerUrl) == true)
            {
                dlg.ServerUrl =
        this.MainForm.AppInfo.GetString("config",
        "circulation_server_url",
        "http://localhost/dp2libraryws/library.asmx");
            }
            else
            {
                dlg.ServerUrl = strServerUrl;
            }

            if (owner == null)
                owner = this;

            if (String.IsNullOrEmpty(strTitle) == false)
                dlg.Text = strTitle;

            dlg.Comment = strComment;
            dlg.UserName = this.MainForm.AppInfo.GetString(
                "default_account",
                "username",
                "");

            dlg.SavePasswordShort =
    this.MainForm.AppInfo.GetBoolean(
    "default_account",
    "savepassword_short",
    false);

            dlg.SavePasswordLong =
                this.MainForm.AppInfo.GetBoolean(
                "default_account",
                "savepassword_long",
                false);

            if (dlg.SavePasswordShort == true || dlg.SavePasswordLong == true)
            {
                dlg.Password = this.MainForm.AppInfo.GetString(
        "default_account",
        "password",
        "");
                dlg.Password = this.DecryptPasssword(dlg.Password);
            }
            else
            {
                dlg.Password = "";
            }


            dlg.IsReader =
                this.MainForm.AppInfo.GetBoolean(
                "default_account",
                "isreader",
                false);
            dlg.OperLocation = this.MainForm.AppInfo.GetString(
                "default_account",
                "location",
                "");

            this.MainForm.AppInfo.LinkFormState(dlg,
                "logindlg_state");

            dlg.ShowDialog(owner);

            this.MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult == DialogResult.Cancel)
            {
                return null;
            }

            this.MainForm.AppInfo.SetString(
                "default_account",
                "username",
                dlg.UserName);
            this.MainForm.AppInfo.SetString(
                "default_account",
                "password",
                (dlg.SavePasswordShort == true || dlg.SavePasswordLong == true) ?
                this.EncryptPassword(dlg.Password) : "");

            this.MainForm.AppInfo.SetBoolean(
    "default_account",
    "savepassword_short",
    dlg.SavePasswordShort);

            this.MainForm.AppInfo.SetBoolean(
                "default_account",
                "savepassword_long",
                dlg.SavePasswordLong);

            this.MainForm.AppInfo.SetBoolean(
                "default_account",
                "isreader",
                dlg.IsReader);
            this.MainForm.AppInfo.SetString(
                "default_account",
                "location",
                dlg.OperLocation);


            // 2006/12/30
            this.MainForm.AppInfo.SetString(
                "config",
                "circulation_server_url",
                dlg.ServerUrl);


            return dlg;
        }

        public string DecryptPasssword(string strEncryptedText)
        {
            if (String.IsNullOrEmpty(strEncryptedText) == false)
            {
                try
                {
                    string strPassword = Cryptography.Decrypt(
        strEncryptedText,
        EncryptKey);
                    return strPassword;
                }
                catch
                {
                    return "errorpassword";
                }

            }

            return "";
        }

        public string EncryptPassword(string strPlainText)
        {
            return Cryptography.Encrypt(strPlainText, this.EncryptKey);
        }
#endif

        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_INITIAL:
                    {
                        this.button_searchDouble_Click(null, null);
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }

        public void LoadSize()
        {
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state");
        }

        public void SaveSize()
        {
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                MainForm.AppInfo.SaveMdiChildFormStates(this,
        "mdi_form_state");
            }

        }

        private void ZhongcihaoForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }

            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                // ��������
                this.MainForm.AppInfo.SetString(
    "zhongcihao_form",
    "servername",
    this.textBox_serverName.Text);

                // ���
                this.MainForm.AppInfo.SetString(
                    "zhongcihao_form",
                    "classnumber",
                    this.textBox_classNumber.Text);

                // ������Ŀ����
                this.MainForm.AppInfo.SetString(
                    "zhongcihao_form",
                    "biblio_dbname",
                    this.comboBox_biblioDbName.Text);

                // �Ƿ�Ҫ���������
                this.MainForm.AppInfo.SetBoolean(
                        "zhongcihao_form",
                        "return_browse_cols",
                        this.checkBox_returnBrowseCols.Checked);

                // ��������
                this.MainForm.AppInfo.GetString(
                        "zhongcihao_form",
                        "servername",
                        this.textBox_serverName.Text);

                string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_number);
                this.MainForm.AppInfo.SetString(
                    "zhongcihao_form",
                    "record_list_column_width",
                    strWidths);
            }

            this.Channels.BeforeLogin -= new BeforeLoginEventHandle(Channels_BeforeLogin);

            EventFinish.Set();

            SaveSize();
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

        /// <summary>
        /// ��Ŀ����
        /// </summary>
        public string BiblioDbName
        {
            get
            {
                return this.comboBox_biblioDbName.Text;
            }
            set
            {
                this.comboBox_biblioDbName.Text = value;
            }
        }

        /// <summary>
        /// ���
        /// </summary>
        public string ClassNumber
        {
            get
            {
                return this.textBox_classNumber.Text;
            }
            set
            {
                this.textBox_classNumber.Text = value;
            }
        }


        /// <summary>
        /// ����
        /// </summary>
        public string MaxNumber
        {
            get
            {
                if (String.IsNullOrEmpty(m_strMaxNumber) == true)
                {
                    string strError = "";

                    int nRet = FillList(true, out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    return m_strMaxNumber;
                ERROR1:
                    throw (new Exception(strError));
                }
                return m_strMaxNumber;
            }
            set
            {
                this.textBox_maxNumber.Text = value;
                m_strMaxNumber = value;
            }
        }

 
        /// <summary>
        /// β��
        /// </summary>
        public string TailNumber
        {
            get
            {
                if (String.IsNullOrEmpty(m_strTailNumber) == true)
                {
                    string strError = "";

                    string strTailNumber = "";
                    int nRet = SearchTailNumber(out strTailNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    m_strTailNumber = strTailNumber;
                    return m_strTailNumber;
                ERROR1:
                    throw (new Exception(strError));

                }
                return m_strTailNumber;

            }
            set
            {
                string strError = "";
                string strOutputNumber = "";
                int nRet = SaveTailNumber(value,
                    out strOutputNumber,
                    out strError);
                if (nRet == -1)
                    throw (new Exception(strError));
                else
                    m_strTailNumber = strOutputNumber;	// ˢ�¼���
            }
        }
 

        // ����
        private void button_search_Click(object sender, EventArgs e)
        {
            string strError = "";

            EventFinish.Reset();
            try
            {
                int nRet = FillList(true, out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ����ñ���β��
                return;
            }
            finally
            {
                EventFinish.Set();
            }

        ERROR1:
            MessageBox.Show(this, strError);
        }

        void EnableControls(bool bEnable)
        {
            this.comboBox_biblioDbName.Enabled = bEnable;
            this.textBox_classNumber.Enabled = bEnable;
            this.textBox_maxNumber.Enabled = bEnable;
            this.textBox_tailNumber.Enabled = bEnable;

            this.button_copyMaxNumber.Enabled = bEnable;
            this.button_getTailNumber.Enabled = bEnable;
            this.button_pushTailNumber.Enabled = bEnable;
            this.button_saveTailNumber.Enabled = bEnable;
            this.button_searchClass.Enabled = bEnable;
            this.button_searchDouble.Enabled = bEnable;
        }

        int FillList(bool bSort,
            out string strError)
        {
            strError = "";
            // int nRet = 0;

            this.listView_number.Items.Clear();
            this.MaxNumber = "";

            // ���server url
            if (String.IsNullOrEmpty(this.LibraryServerName) == true)
            {
                strError = "��δָ����������";
                goto ERROR1;
            }
            dp2Server server = this.MainForm.Servers.GetServerByName(this.LibraryServerName);
            if (server == null)
            {
                strError = "��������Ϊ '" + this.LibraryServerName + "' �ķ�����������...";
                goto ERROR1;
            }

            string strServerUrl = server.Url;

            this.Channel = this.Channels.GetChannel(strServerUrl);

            /*
            if (dom == null)
            {
                strError = "���ȵ���GetGlobalCfgFile()����";
                return -1;
            }
             * */

            if (this.ClassNumber == "")
            {
                strError = "��δָ�������";
                return -1;
            }

            if (this.BiblioDbName == "")
            {
                strError = "��δָ����Ŀ����";
                return -1;
            }

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ���ͬ�����¼ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                string strQueryXml = "";

                long lRet = Channel.SearchUsedZhongcihao(
                    stop,
                    GetZhongcihaoDbGroupName(this.BiblioDbName),
                    // "!" + this.BiblioDbName,
                    this.ClassNumber,
                    "zhongcihao",
                    out strQueryXml,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                if (lRet == 0)
                {
                    strError = "û�����еļ�¼��";
                    return 0;   // not found
                }


                long lHitCount = lRet;

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                ZhongcihaoSearchResult[] searchresults = null;

                if (stop != null)
                    stop.SetProgressRange(0, lHitCount);

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }
                    }

                    long lCurrentPerCount = lPerCount;

                    bool bShift = Control.ModifierKeys == Keys.Shift;
                    string strBrowseStyle = "cols";
                    if (bShift == true || this.checkBox_returnBrowseCols.Checked == false)
                    {
                        strBrowseStyle = "";
                        lCurrentPerCount = lPerCount * 10;
                    }


                    stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    lRet = Channel.GetZhongcihaoSearchResult(
                        stop,
                        GetZhongcihaoDbGroupName(this.BiblioDbName),
                        // "!" + this.BiblioDbName,
                        "zhongcihao",   // strResultSetName
                        lStart,
                        lPerCount,
                        strBrowseStyle, // style
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (lRet == 0)
                    {
                        strError = "δ����";
                        goto ERROR1;
                    }

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        ZhongcihaoSearchResult result_item = searchresults[i];
                        ListViewItem item = new ListViewItem();
                        item.Text = result_item.Path;
                        item.SubItems.Add(result_item.Zhongcihao);

                        if (result_item.Cols != null)
                        {
                            ListViewUtil.EnsureColumns(this.listView_number, result_item.Cols.Length + 1);
                            for (int j = 0; j < result_item.Cols.Length; j++)
                            {
                                ListViewUtil.ChangeItemText(item, j + 2, result_item.Cols[j]);
                            }
                        }

                        this.listView_number.Items.Add(item);
                        if (stop != null)
                            stop.SetProgressValue(lStart + i + 1);
                    }

                    lStart += searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                EnableControls(true);
            }

            if (bSort == true)
            {
                // ����
                this.listView_number.ListViewItemSorter = new ZhongcihaoListViewItemComparer();
                this.listView_number.ListViewItemSorter = null;

                // ���ظ��ִκŵ�������������ɫ�����
                ColorDup();

                this.MaxNumber = GetTopNumber(this.listView_number);    // this.listView_number.Items[0].SubItems[1].Text;
            }

            return 0;
        ERROR1:
            return -1;
        }

        // ���Ѿ�����������У�ȡ��λ�����������ִκš�
        // ���������Զ��ų�MyselfBiblioRecPath������¼
        string GetTopNumber(ListView list)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];
                string strRecPath = item.Text;
                if (strRecPath != this.MyselfBiblioRecPath)
                    return item.SubItems[1].Text;
            }

            // TODO: ��������Լ����⣬��û������������Ч�ִκŵ������ˣ���Ҳֻ�����Լ����ִκ�-1���䵱��

            return "";  // û���ҵ�
        }

        // ʹ�����ظ��б�ɫ
        void ColorDup()
        {
            string strPrevNumber = "";
            Color color1 = Color.FromArgb(220, 220, 220);
            Color color2 = Color.FromArgb(230, 230, 230);
            Color color = color1;
            int nDupCount = 0;
            for (int i = 0; i < this.listView_number.Items.Count; i++)
            {
                string strNumber = this.listView_number.Items[i].SubItems[1].Text;

                if (strNumber == strPrevNumber)
                {
                    if (i >= 1 && nDupCount == 0)
                        this.listView_number.Items[i - 1].BackColor = color;

                    this.listView_number.Items[i].BackColor = color;
                    nDupCount++;
                }
                else
                {
                    if (nDupCount >= 1)
                    {
                        // ��һ����ɫ
                        if (color == color1)
                            color = color2;
                        else
                            color = color1;
                    }

                    nDupCount = 0;

                    this.listView_number.Items[i].BackColor = SystemColors.Window;

                }


                strPrevNumber = strNumber;
            }

        }


        // ����β�ţ���������н���Ԫ��
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int PanelGetTailNumber(out string strError)
        {
            strError = "";
            this.textBox_tailNumber.Text = "";

            string strTailNumber = "";
            int nRet = SearchTailNumber(out strTailNumber,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;

            this.textBox_tailNumber.Text = strTailNumber;
            // this.label_tailNumberTitle.Text = "��'" + this.ZhongcihaoDbName + "'�е�β��(&T):";
            return 1;
        }


                /// <summary>
        ///  ��������ִκſ��ж�Ӧ��Ŀ��β�š��˹��ܱȽϵ���������õĽ����������������Ԫ��
        /// </summary>
        /// <param name="strTailNumber">����β��</param>
        /// <param name="strError">���ش�����Ϣ</param>
        /// <returns>-1����;0û���ҵ�;1�ҵ�</returns>
        public int SearchTailNumber(
            out string strTailNumber,
            out string strError)
        {
            strTailNumber = "";


            // ���server url
            if (String.IsNullOrEmpty(this.LibraryServerName) == true)
            {
                strError = "��δָ����������";
                goto ERROR1;
            }
            dp2Server server = this.MainForm.Servers.GetServerByName(this.LibraryServerName);
            if (server == null)
            {
                strError = "��������Ϊ '" + this.LibraryServerName + "' �ķ�����������...";
                goto ERROR1;
            }

            string strServerUrl = server.Url;
            this.Channel = this.Channels.GetChannel(strServerUrl);

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ��β�� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.GetZhongcihaoTailNumber(
                    stop,
                    GetZhongcihaoDbGroupName(this.BiblioDbName),
                    // "!" + this.BiblioDbName,
                    this.ClassNumber,
                    out strTailNumber,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            // return 0;
        ERROR1:
            return -1;
        }

        // �ƶ�β�š�����Ѿ����ڵ�β�ű�strTestNumber��Ҫ�����ƶ�
        public int PushTailNumber(string strTestNumber,
            out string strOutputNumber,
            out string strError)
        {
            strOutputNumber = "";

            // ���server url
            if (String.IsNullOrEmpty(this.LibraryServerName) == true)
            {
                strError = "��δָ����������";
                goto ERROR1;
            }
            dp2Server server = this.MainForm.Servers.GetServerByName(this.LibraryServerName);
            if (server == null)
            {
                strError = "��������Ϊ '" + this.LibraryServerName + "' �ķ�����������...";
                goto ERROR1;
            }

            string strServerUrl = server.Url;
            this.Channel = this.Channels.GetChannel(strServerUrl);


            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����ƶ�β�� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.SetZhongcihaoTailNumber(
                    stop,
                    "conditionalpush",
                    GetZhongcihaoDbGroupName(this.BiblioDbName),
                    // "!" + this.BiblioDbName,
                    this.ClassNumber,
                    strTestNumber,
                    out strOutputNumber,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            // return 0;
        ERROR1:
            return -1;
        }

        public int SaveTailNumber(
            string strTailNumber,
            out string strOutputNumber,
            out string strError)
        {
            strOutputNumber = "";

            // ���server url
            if (String.IsNullOrEmpty(this.LibraryServerName) == true)
            {
                strError = "��δָ����������";
                goto ERROR1;
            }
            dp2Server server = this.MainForm.Servers.GetServerByName(this.LibraryServerName);
            if (server == null)
            {
                strError = "��������Ϊ '" + this.LibraryServerName + "' �ķ�����������...";
                goto ERROR1;
            }

            string strServerUrl = server.Url;
            this.Channel = this.Channels.GetChannel(strServerUrl);


            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ���β�� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.SetZhongcihaoTailNumber(
                    stop,
                    "save",
                    GetZhongcihaoDbGroupName(this.BiblioDbName),
                    // "!" + this.BiblioDbName,
                    this.ClassNumber,
                    strTailNumber,
                    out strOutputNumber,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            // return 0;
        ERROR1:
            return -1;
        }

        // ���β��
        private void button_getTailNumber_Click(object sender, EventArgs e)
        {
            string strError = "";

            EventFinish.Reset();
            try
            {
                this.textBox_tailNumber.Text = "";   // Ԥ����գ��Է����

                // ��ñ���β��
                int nRet = PanelGetTailNumber(out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    strError = "�� '" + this.ClassNumber + "' ��β���в�����";
                    goto ERROR1;
                }

                return;
            }
            finally
            {
                EventFinish.Set();
            }

        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ����β��
        private void button_saveTailNumber_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.textBox_tailNumber.Text == "")
            {
                strError = "��δ����Ҫ�����β��";
                goto ERROR1;
            }

            EventFinish.Reset();
            try
            {
                string strOutputNumber = "";

                // ���汾��β��
                int nRet = SaveTailNumber(this.textBox_tailNumber.Text,
                    out strOutputNumber,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                return;
            }
            finally
            {
                EventFinish.Set();
            }

        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �ü����õ���ͬ������ʵ���õ������ţ���̽���ƶ��ִκſ��е�β��
        private void button_pushTailNumber_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strOutputNumber = "";
            // �ƶ�β��
            int nRet = PushTailNumber(this.textBox_maxNumber.Text,
                out strOutputNumber,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.textBox_tailNumber.Text = strOutputNumber;
            // MessageBox.Show(this, "�ƶ�β�ųɹ�");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
        // return:
        //      -1  error
        //      0   not found
        //      1   succeed
        public int GetMaxNumberPlusOne(out string strResult,
            out string strError)
        {
            strResult = "";
            strError = "";
            string strMaxNumber = "";

            try
            {
                strMaxNumber = this.MaxNumber;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                goto ERROR1;
            }

            if (String.IsNullOrEmpty(strMaxNumber) == true)
                return 0;

            int nRet = StringUtil.IncreaseLeadNumber(strMaxNumber,
                1,
                out strResult,
                out strError);
            if (nRet == -1)
            {
                strError = "Ϊ���� '" + strMaxNumber + "' ����ʱ��������: " + strError;
                goto ERROR1;

            }
            return 1;
        ERROR1:
            return -1;
        }

        // ���Ʊȵ�ǰ��Ŀ��ͳ�Ƴ��������Ż���1�ĺ�
        private void button_copyMaxNumber_Click(object sender, EventArgs e)
        {
            string strResult = "";
            string strError = "";

            // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
            // return:
            //      -1  error
            //      1   succeed
            int nRet = GetMaxNumberPlusOne(out strResult,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }

            if (nRet == 0)
                strResult = "1";    // �����ǰ����Ŀ���޷�ͳ�Ƴ����ţ�����Ϊ�õ�"0"������1�Ժ�����Ϊ"1"

            Clipboard.SetDataObject(strResult);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �������֣�ͬ���顢β��
        private void button_searchDouble_Click(object sender, EventArgs e)
        {
            string strError = "";

            EventFinish.Reset();
            try
            {
                this.textBox_tailNumber.Text = "";   // Ԥ��filllist ��ǰ�˳�, ���Ǵ���

                int nRet = FillList(true, out strError);
                if (nRet == -1)
                    goto ERROR1;

                // һ����ñ���β��
                nRet = PanelGetTailNumber(out strError);
                if (nRet == -1)
                    goto ERROR1;

                return;
            }
            finally
            {
                EventFinish.Set();
            }

        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ͼ��ݷ�������
        public string LibraryServerName
        {
            get
            {
                return this.textBox_serverName.Text;
            }
            set
            {
                this.textBox_serverName.Text = value;
            }
        }

        private void comboBox_biblioDbName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_biblioDbName.Items.Count > 0)
                return;

            // this.comboBox_biblioDbName.Items.Add("<ȫ��>");

            /*
            for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
            {
                BiblioDbProperty property = this.MainForm.BiblioDbProperties[i];
                this.comboBox_biblioDbName.Items.Add(property.DbName);
            }
             * */
            string strError = "";

            if (String.IsNullOrEmpty(this.LibraryServerName) == true)
            {
                strError = "��δָ����������";
                goto ERROR1;
            }

            dp2Server server = this.MainForm.Servers.GetServerByName(this.LibraryServerName);
            if (server == null)
            {
                strError = "��������Ϊ '" + this.LibraryServerName + "' �ķ�����������...";
                goto ERROR1;
            }

            // ���server url
            string strServerUrl = server.Url;

            List<string> dbnames = null;
            int nRet = GetBiblioDbNames(
                null,   // this.stop,
                this.LibraryServerName,
                strServerUrl,
                out dbnames,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            for (int i = 0; i < dbnames.Count; i++)
            {
                this.comboBox_biblioDbName.Items.Add(dbnames[i]);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ���һ����Ŀ�����б�
        // parameters:
        //      stop    ���!=null����ʾʹ�����stop�����Ѿ�OnStop +=
        //              ���==null����ʾ���Զ�ʹ��this.stop�����Զ�OnStop+=
        // return:
        //      -1  error
        //      0   OK
        int GetBiblioDbNames(
            Stop stop,
            string strServerName,
            string strServerUrl,
            out List<string> dbnames,
            out string strError)
        {
            dbnames  = new List<string>();
            strError = "";

            bool bInitialStop = false;
            if (stop == null)
            {
                stop = this.stop;

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڻ�÷����� " + strServerUrl + " ����Ϣ ...");
                stop.BeginLoop();

                bInitialStop = true;
            }

            dp2ServerInfo info = null;

            try
            {
                info = this.MainForm.ServerInfos.GetServerInfo(stop,
                    false,
                    this.Channels,
                    strServerName,
                    strServerUrl,
                    this.MainForm.TestMode,
                    out strError);
                if (info == null)
                    return -1;
            }
            finally
            {
                if (bInitialStop == true)
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }
            }

            for (int i = 0; i < info.BiblioDbProperties.Count; i++)
            {
                BiblioDbProperty prop = info.BiblioDbProperties[i];

                dbnames.Add(prop.DbName);
            }

            return 0;
        }

        // �������������������ݿ��������ִκŷ������任ΪAPIʹ�õ���̬
        static string GetZhongcihaoDbGroupName(string strText)
        {
            if (String.IsNullOrEmpty(strText) == true)
                return "";

            // �����һ���ַ���!���ţ������Ƿ�����
            if (strText[0] == '!')
                return strText.Substring(1);

            // û�У����ţ��������������ݿ���
            return "!" + strText;
        }

        // ����β��
        public int IncreaseTailNumber(string strDefaultNumber,
            out string strOutputNumber,
            out string strError)
        {
            strOutputNumber = "";

            // ���server url
            if (String.IsNullOrEmpty(this.LibraryServerName) == true)
            {
                strError = "��δָ����������";
                goto ERROR1;
            }
            dp2Server server = this.MainForm.Servers.GetServerByName(this.LibraryServerName);
            if (server == null)
            {
                strError = "��������Ϊ '" + this.LibraryServerName + "' �ķ�����������...";
                goto ERROR1;
            }

            string strServerUrl = server.Url;
            this.Channel = this.Channels.GetChannel(strServerUrl);


            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("��������β�� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.SetZhongcihaoTailNumber(
                    stop,
                    "increase",
                    GetZhongcihaoDbGroupName(this.BiblioDbName),
                    // "!" + this.BiblioDbName,
                    this.ClassNumber,
                    strDefaultNumber,
                    out strOutputNumber,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            // return 0;
        ERROR1:
            return -1;
        }

        #region Э���ⲿ���õĺ���

        /// <summary>
        /// �ȴ���������
        /// </summary>
        public void WaitSearchFinish()
        {
            for (; ; )
            {
                Application.DoEvents();
                bool bRet = this.EventFinish.WaitOne(10, true);
                if (bRet == true)
                    break;
            }
        }

        #endregion

        // ����һ���Ĳ��ԣ�����ִκ�
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        public int GetNumber(
            ZhongcihaoStyle style,
            string strClass,
            string strBiblioDbName,
            out string strNumber,
            out string strError)
        {
            strNumber = "";
            strError = "";
            int nRet = 0;

            this.ClassNumber = strClass;
            this.BiblioDbName = strBiblioDbName;

        // ��������Ŀͳ������
            if (style == ZhongcihaoStyle.Biblio)
            {
                // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
                // return:
                //      -1  error
                //      1   succeed
                nRet = GetMaxNumberPlusOne(out strNumber,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                return 1;
            }



            // ÿ�ζ�������Ŀͳ�����������顢У��β��
            if (style == ZhongcihaoStyle.BiblioAndSeed
                || style == ZhongcihaoStyle.SeedAndBiblio)
            {

                string strTailNumber = this.TailNumber;

                // ���������δ�����ִκ���Ŀ
                if (String.IsNullOrEmpty(strTailNumber) == true)
                {
                     // �Ͼ���ʼֵ����������ͳ�ƽ��
                    string strTestNumber = "";
                    // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   succeed
                    nRet = GetMaxNumberPlusOne(out strTestNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 0)
                        strTestNumber = "1";

                    // �������û�й���¼����ǰ�ǵ�һ��
                    strNumber = InputDlg.GetInput(
                        this,
                        null,
                        "�������� '" + strClass + "' �ĵ�ǰ�ִκ�����:",
                        strTestNumber);
                    if (strNumber == null)
                        return 0;	// ������������

                    // dlg.TailNumber = strNumber;	

                    nRet = PushTailNumber(strNumber,
                        out strNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    return 1;
                }
                else // �����Ѿ����ִκ���Ŀ
                {
                    // ����ͳ��ֵ�Ĺ�ϵ
                    string strTestNumber = "";
                    // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   succeed
                    nRet = GetMaxNumberPlusOne(out strTestNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 0)
                    {
                        // ��������β����������
                        nRet = this.IncreaseTailNumber("1",
                            out strNumber,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        return 1;
                    }

                    // ��ͳ�Ƴ����ĺ��ƶ���ǰβ�ţ������˼��������
                    nRet = PushTailNumber(strTestNumber,
                        out strNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // ���������ͷ��أ�Ч��Ϊ�������������������ǰ��¼����ȡ�Ŷ������棬��β�Ų�äĿ��������Ȼȱ��Ҳ�Ǻ����Ե� -- �п��ܶ������ȡ���غ���
                    if (style == ZhongcihaoStyle.BiblioAndSeed)
                        return 1;

                    if (strTailNumber != strNumber)  // ���ʵ�ʷ������ƶ�����Ҫ����ţ�����������
                        return 1;

                    // ��������β������
                    nRet = this.IncreaseTailNumber("1",
                        out strNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    return 1;
                }

                // return 1;
            }

            // ������(�ִκſ�)β��
            if (style == ZhongcihaoStyle.Seed)
            {
                string strTailNumber = this.TailNumber;

                // ���������δ�����ִκ���Ŀ
                if (String.IsNullOrEmpty(strTailNumber) == true)
                {
                    // �Ͼ���ʼֵ����������ͳ�ƽ��
                    string strTestNumber = "";
                    // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   succeed
                    nRet = GetMaxNumberPlusOne(out strTestNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 0)
                        strTestNumber = "1";
                    // �������û�й���¼����ǰ�ǵ�һ��
                    strNumber = InputDlg.GetInput(
                        this,
                        null,
                        "�������� '" + strClass + "' �ĵ�ǰ�ִκ�����:",
                        strTestNumber);
                    if (strNumber == null)
                        return 0;	// ������������

                    // dlg.TailNumber = strNumber;	

                    nRet = PushTailNumber(strNumber,
                        out strNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    return 1;
                }
                else // �����Ѿ����ִκ���Ŀ����������
                {
                    nRet = this.IncreaseTailNumber("1",
                        out strNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }

                return 1;
            }





            return 1;
        ERROR1:
            return -1;
        }

        // ˫��������Ŀ��¼װ����ϸ��
        private void listView_number_DoubleClick(object sender, EventArgs e)
        {
            if (this.listView_number.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ����ϸ��������");
                return;
            }
            string strPath = this.listView_number.SelectedItems[0].SubItems[0].Text;

            MessageBox.Show(this, "��δʵ��");
            /*
            EntityForm form = new EntityForm();

            form.MdiParent = this.MainForm;

            form.MainForm = this.MainForm;
            form.Show();
            form.LoadRecord(strPath);
             * */

        }

        private void button_findServerName_Click(object sender, EventArgs e)
        {
            GetDp2ResDlg dlg = new GetDp2ResDlg();
            GuiUtil.SetControlFont(dlg, this.Font);

            dlg.dp2Channels = this.Channels;
            dlg.Servers = this.MainForm.Servers;
            dlg.EnabledIndices = new int[] { dp2ResTree.RESTYPE_SERVER };
            dlg.Path = this.textBox_serverName.Text;

            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.textBox_serverName.Text = dlg.Path;
        }

        private void listView_number_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListViewUtil.OnSeletedIndexChanged(this.listView_number,
    0,
    new List<int> { 1 });
        }
    }

    // ����
    // Implements the manual sorting of items by columns.
    class ZhongcihaoListViewItemComparer : IComparer
    {
        public ZhongcihaoListViewItemComparer()
        {
        }

        public int Compare(object x, object y)
        {
            // �ִκ��ַ�����Ҫ�Ҷ��� 2007/10/12
            string s1 = ((ListViewItem)x).SubItems[1].Text;
            string s2 = ((ListViewItem)y).SubItems[1].Text;

            int nMaxLength = Math.Max(s1.Length, s2.Length);
            s2 = s2.PadLeft(nMaxLength, '0');
            s1 = s1.PadLeft(nMaxLength, '0');

            return -1 * String.Compare(s1, s2);
        }
    }

    // �ִκ�ȡ�ŵķ��
    public enum ZhongcihaoStyle
    {
        Biblio = 1, // ��������Ŀͳ������
        BiblioAndSeed = 2,  // ÿ�ζ�������Ŀͳ�����������顢У��β�š�ƫ����Ŀͳ��ֵ����äĿ����β�š�
        SeedAndBiblio = 3, // ÿ�ζ�������Ŀͳ�����������顢У��β�š�ƫ��β�ţ�ÿ�ζ�����β��
        Seed = 4, // ������(�ִκſ�)β��
    }

}