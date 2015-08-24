using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Threading;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Text;
using DigitalPlatform.Xml;
using DigitalPlatform.Marc;
using DigitalPlatform.CommonControl;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.CirculationClient.localhost;
using System.Collections;
using System.Web;
using DigitalPlatform.IO;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Script;


using System.Reflection;
using Microsoft.Win32;
using DigitalPlatform.dp2.Statis;
// using DocumentFormat.OpenXml.Packaging;


namespace dp2Catalog
{
    public partial class dp2SearchForm : Form, ISearchForm
    {
        public string ExportRecPathFilename = "";
        // ���ʹ�ù��ļ�¼·���ļ���
        string m_strUsedRecPathFilename = "";

        Commander commander = null;
        BiblioViewerForm m_commentViewer = null;

        Hashtable m_biblioTable = new Hashtable(); // ��Ŀ��¼·�� --> ��Ŀ��Ϣ
        int m_nChangedCount = 0;
        // MarcFilter���󻺳��
        public FilterCollection Filters = new FilterCollection();
        public string BinDir = "";

        // ����������к�����
        SortColumns SortColumns = new SortColumns();

        bool m_bInSearching = false;

        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;

        public LibraryChannelCollection Channels = null;
        internal LibraryChannel Channel = null;

        public string Lang = "zh";

        const int WM_SELECT_INDEX_CHANGED = API.WM_USER + 200;
        const int WM_LOADSIZE = API.WM_USER + 201;
        const int WM_INITIAL_FOCUS = API.WM_USER + 202;

        // ��ǰȱʡ�ı��뷽ʽ
        Encoding CurrentEncoding = Encoding.UTF8;

        /// <summary>
        /// ���������ź�
        /// </summary>
        public AutoResetEvent EventLoadFinish = new AutoResetEvent(false);


        public dp2SearchForm()
        {
            InitializeComponent();

            ListViewProperty prop = new ListViewProperty();
            this.listView_browse.Tag = prop;
            // ��һ�����⣬��¼·��
            prop.SetSortStyle(0, ColumnSortStyle.RecPath);
            prop.GetColumnTitles -= new GetColumnTitlesEventHandler(prop_GetColumnTitles);
            prop.GetColumnTitles += new GetColumnTitlesEventHandler(prop_GetColumnTitles);
            prop.ParsePath -= new ParsePathEventHandler(prop_ParsePath);
            prop.ParsePath += new ParsePathEventHandler(prop_ParsePath);


        }

        void prop_ParsePath(object sender, ParsePathEventArgs e)
        {
            string strServerName = "";
            string strPurePath = "";
            // ������¼·����
            // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
            ParseRecPath(e.Path,
                out strServerName,
                out strPurePath);

            e.DbName = strServerName + "|" + GetDbName(strPurePath);
        }

        void prop_GetColumnTitles(object sender, GetColumnTitlesEventArgs e)
        {
            if (this._linkMarcFile != null
                || StringUtil.HasHead(e.DbName, "mem|") == true
                || StringUtil.HasHead(e.DbName, "file|") == true)
            {
                e.ColumnTitles = new ColumnPropertyCollection();
                e.ColumnTitles.Add("����");
                e.ColumnTitles.Add("������");
                e.ColumnTitles.Add("������");
                e.ColumnTitles.Add("��������");
                return;
            }

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
            string [] parts = strPrefix.Split(new char[] {'|'});
            if (parts.Length < 2)
            {
                // return new ColumnPropertyCollection();
                return null;
            }

            return this.dp2ResTree1.GetBrowseColumnNames(parts[0], parts[1]);
        }

        void ClearListViewPropertyCache()
        {
            ListViewProperty prop = (ListViewProperty)this.listView_browse.Tag;
            prop.ClearCache();
        }

        public void RefreshResTree()
        {
            if (this.dp2ResTree1 != null)
                this.dp2ResTree1.Refresh(dp2ResTree.RefreshStyle.All);
        }

        private void dp2SearchForm_Load(object sender, EventArgs e)
        {
#if NO
            if (this.MainForm.TestMode == true)
            {
                MessageBox.Show(this.MainForm, "dp2 ��������Ҫ���������к�(��ʽģʽ)����ʹ��");
                API.PostMessage(this.Handle, API.WM_CLOSE, 0, 0);
                return;
            }
#endif

#if SN

            // ������к�
            // DateTime start_day = new DateTime(2014, 11, 15);    // 2014/11/15 �Ժ�ǿ���������кŹ���
            // if (DateTime.Now >= start_day || this.MainForm.IsExistsSerialNumberStatusFile() == true)
            {
                // ���û�Ŀ¼��д��һ�������ļ�����ʾ���кŹ����Ѿ�����
                this.MainForm.WriteSerialNumberStatusFile();

                string strError = "";
                int nRet = this.MainForm.VerifySerialCode("dp2 ��������Ҫ���������кŲ���ʹ��",
                    "",
                    false,
                    out strError);
                if (nRet == -1)
                {
#if NO
                    MessageBox.Show(this.MainForm, "dp2 ��������Ҫ���������кŲ���ʹ��");
                    API.PostMessage(this.Handle, API.WM_CLOSE, 0, 0);
                    return;
#endif
                }
                else
                {
                    // Ϊȫ������������ verified ��־
                    this.MainForm.Servers.SetAllVerified(true);
                }
            }
#else
            // Ϊȫ������������ verified ��־
            this.MainForm.Servers.SetAllVerified(true);
#endif

            //
            EventLoadFinish.Reset();
            this.BinDir = Environment.CurrentDirectory;

            this.commander = new Commander(this);
            this.commander.IsBusy -= new IsBusyEventHandler(commander_IsBusy);
            this.commander.IsBusy += new IsBusyEventHandler(commander_IsBusy);

            SetLayout(this.LayoutName);

            this.MainForm.AppInfo.LoadMdiSize += new EventHandler(AppInfo_LoadMdiSize);
            this.MainForm.AppInfo.SaveMdiSize += new EventHandler(AppInfo_SaveMdiSize);

            LoadSize();


            this.Channels = new LibraryChannelCollection();
            this.Channels.BeforeLogin += new BeforeLoginEventHandle(Channels_BeforeLogin);
            this.Channels.AfterLogin += new AfterLoginEventHandle(Channels_AfterLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������

            //
            this.dp2ResTree1.TestMode = this.MainForm.TestMode;

            this.dp2ResTree1.stopManager = MainForm.stopManager;

            this.dp2ResTree1.Servers = MainForm.Servers;	// ����

            this.dp2ResTree1.Channels = this.Channels;	// ����

            this.dp2ResTree1.cfgCache = this.MainForm.cfgCache;

            string strSortTables = this.MainForm.AppInfo.GetString(
               "dp2_search",
               "sort_tables",
               ""); 
            this.dp2ResTree1.sort_tables = dp2ResTree.RestoreSortTables(strSortTables);

            this.dp2ResTree1.CheckBoxes = this.MainForm.AppInfo.GetBoolean(
               "dp2_search",
               "enable_checkboxes",
               false); 

            this.dp2ResTree1.Fill(null);

            this.textBox_simple_queryWord.Text = this.MainForm.AppInfo.GetString(
                "dp2_search_simple_query",
                "word",
                "");
            this.comboBox_simple_matchStyle.Text = this.MainForm.AppInfo.GetString(
    "dp2_search_simple_query",
    "matchstyle",
    "ǰ��һ��");

            this.textBox_mutiline_queryContent.Text = this.MainForm.AppInfo.GetString(
                "dp2_search_muline_query",
                "content",
                "");
            this.comboBox_multiline_matchStyle.Text = this.MainForm.AppInfo.GetString(
"dp2_search_muline_query",
"matchstyle",
"ǰ��һ��");

            string strWidths = this.MainForm.AppInfo.GetString(
    "dp2searchform",
    "record_list_column_width",
    "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_browse,
                    strWidths,
                    true);
            }

            string strSaveString = this.MainForm.AppInfo.GetString(
    "dp2searchform",
    "query_lines",
    "^^^");
            this.dp2QueryControl1.Restore(strSaveString);
            /*
            for (int i = 0; i < nQueryLineCount; i++)
            {
                this.dp2QueryControl1.AddLine();
            }
             * */


            // API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);

            // �����ϴα����·��չ��resdircontrol��
            string strResDirPath = this.MainForm.AppInfo.GetString(
                "dp2_search_simple_query",
                "resdirpath",
                "");
            if (strResDirPath != null)
            {
                this.Update();

                object[] pList = { strResDirPath };

                this.BeginInvoke(new Delegate_ExpandResDir(ExpandResDir),
                    pList);
            }
            else
            {
                this.EventLoadFinish.Set();
            }

            comboBox_matchStyle_TextChanged(null, null);
        }

        void Channels_AfterLogin(object sender, AfterLoginEventArgs e)
        {
            LibraryChannel channel = (LibraryChannel)sender;

            dp2Server server = this.MainForm.Servers[channel.Url];
            if (server == null)
            {
                e.ErrorInfo = "û���ҵ� URL Ϊ " + channel.Url + " �ķ���������";
                return;
            }

#if SN
            if (server.Verified == false && StringUtil.IsInList("serverlicensed", channel.Rights) == false)
            {
                string strError = "";
                string strTitle = "dp2 ��������Ҫ���������кŲ��ܷ��ʷ����� " + server.Name + " " + server.Url;
                int nRet = this.MainForm.VerifySerialCode(strTitle, 
                    "",
                    true,
                    out strError);
                if (nRet == -1)
                {
                    channel.Close();
                    e.ErrorInfo = strTitle;
#if NO
                    MessageBox.Show(this.MainForm, "dp2 ��������Ҫ���������кŲ���ʹ��");
                    MainForm.AppInfo.SetString(
    "dp2_search_simple_query",
    "resdirpath",
    "");
                    API.PostMessage(this.Handle, API.WM_CLOSE, 0, 0);
#endif
                    return;
                }
            }
#endif
            server.Verified = true;
        }


        void commander_IsBusy(object sender, IsBusyEventArgs e)
        {
            e.IsBusy = this.m_nInViewing > 0;
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

                // 2014/11/10
                if (this.MainForm.TestMode == true)
                    e.Parameters += ",testmode=true";

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

            // 2014/11/10
            e.Parameters += ",mac=" + StringUtil.MakePathList(SerialCodeForm.GetMacAddress(), "|");

#if SN
            {
                // �����к��л�� expire= ����ֵ
                string strExpire = this.MainForm.GetExpireParam();
                if (string.IsNullOrEmpty(strExpire) == false)
                    e.Parameters += ",expire=" + strExpire;
            }
#endif

            // 2014/11/10
            if (this.MainForm.TestMode == true)
                e.Parameters += ",testmode=true";

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
            this.Activate();    // �� MDI �Ӵ��ڷ�������ǰ��
            dlg.ShowDialog(owner);

            this.MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult == DialogResult.Cancel)
            {
                return null;
            }

            bool bChanged = false;

            if (server.DefaultUserName != dlg.UserName)
            {
                server.DefaultUserName = dlg.UserName;
                bChanged = true;
            }

            string strNewPassword = (dlg.SavePassword == true) ?
            dlg.Password : "";
            if (server.DefaultPassword != strNewPassword)
            {
                server.DefaultPassword = strNewPassword;
                bChanged = true;
            }


            if (server.SavePassword != dlg.SavePassword)
            {
                server.SavePassword = dlg.SavePassword;
                bChanged = true;
            }

            if (server.Url != dlg.ServerUrl)
            {
                server.Url = dlg.ServerUrl;
                bChanged = true;
            }

            if (bChanged == true)
                this.MainForm.Servers.Changed = true;

            return dlg;
        }


        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                    /*
                case WM_LOADSIZE:
                    LoadSize();
                    return;
                     * */
                case WM_INITIAL_FOCUS:
                    this.textBox_simple_queryWord.Focus();
                    return;
                case WM_SELECT_INDEX_CHANGED:
                    {
#if NO
                        if (this.listView_records.SelectedIndices.Count == 0)
                            this.label_message.Text = "";
                        else
                        {
                            if (this.listView_records.SelectedIndices.Count == 1)
                            {
                                this.label_message.Text = "�� " + (this.listView_records.SelectedIndices[0] + 1).ToString() + " ��";
                            }
                            else
                            {
                                this.label_message.Text = "�� " + (this.listView_records.SelectedIndices[0] + 1).ToString() + " �п�ʼ����ѡ�� " + this.listView_records.SelectedIndices.Count.ToString() + " ������";
                            }
                        }
#endif
                        // �˵���̬�仯
                        if (this.listView_browse.SelectedItems.Count == 0)
                        {
                            MainForm.toolButton_saveTo.Enabled = false;
                            MainForm.toolButton_delete.Enabled = false;

                            MainForm.MenuItem_saveOriginRecordToIso2709.Enabled = false;
                            MainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = false;

                            MainForm.StatusBarMessage = "";
                        }
                        else
                        {
                            MainForm.toolButton_saveTo.Enabled = true;
                            MainForm.toolButton_delete.Enabled = true;

                            MainForm.MenuItem_saveOriginRecordToIso2709.Enabled = true;
                            MainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = true;

                            if (this.listView_browse.SelectedItems.Count == 1)
                            {
                                MainForm.StatusBarMessage = "�� " + (this.listView_browse.SelectedIndices[0] + 1).ToString() + " ��";
                            }
                            else
                            {
                                MainForm.StatusBarMessage = "�� " + (this.listView_browse.SelectedIndices[0] + 1).ToString() + " �п�ʼ����ѡ�� " + this.listView_browse.SelectedItems.Count.ToString() + " ������";
                            }
                        }

                            ListViewUtil.OnSeletedIndexChanged(this.listView_browse,
                                0,
                                null);

                        if (this.m_biblioTable != null)
                        {
                            if (CanCallNew(commander, m.Msg) == true)
                                DoViewComment(false);
                        }
                    }
                    return;

            }
            base.DefWndProc(ref m);
        }

        public bool CanCallNew(Commander commander, int msg)
        {
            if (this.m_nInViewing > 0)
            {
                // ����֮��
                // this.Stop();
                commander.AddMessage(msg);
                return false;   // ����������
            }

            return true;    // ��������
        }

        public void AppInfo_LoadMdiSize(object sender, EventArgs e)
        {
            if (sender != this)
                return;



#if NO
            // ���splitContainer_main��״̬
            int nValue = MainForm.AppInfo.GetInt(
            "dp2searchform",
            "splitContainer_main",
            -1);
            if (nValue != -1)
            {
                try
                {
                    this.splitContainer_main.SplitterDistance = nValue;
                }
                catch
                {
                }
            }

            // ���splitContainer_up��״̬
            nValue = MainForm.AppInfo.GetInt(
            "dp2searchform",
            "splitContainer_up",
            -1);
            if (nValue != -1)
            {
                try
                {
                    this.splitContainer_up.SplitterDistance = nValue;
                }
                catch
                {
                }
            }

            /*
            // ���splitContainer_queryAndResultInfo��״̬
            nValue = MainForm.AppInfo.GetInt(
            "dp2searchform",
            "splitContainer_queryAndResultInfo",
            -1);
            if (nValue != -1)
            {
                try
                {
                    this.splitContainer_queryAndResultInfo.SplitterDistance = nValue;
                }
                catch
                {
                }
            }
            */
#endif

            try
            {
                // ���splitContainer_main��״̬
                this.MainForm.LoadSplitterPos(
                    this.splitContainer_main,
                    "dp2searchform",
                    "splitContainer_main");

                // ���splitContainer_up��״̬
                this.MainForm.LoadSplitterPos(
                    this.splitContainer_up,
                    "dp2searchform",
                    "splitContainer_up");

                // ���splitContainer_queryAndResultInfo��״̬
                this.MainForm.LoadSplitterPos(
                    this.splitContainer_queryAndResultInfo,
                    "dp2searchform",
                    "splitContainer_queryAndResultInfo");
            }
            catch
            {
            }
        }

        void AppInfo_SaveMdiSize(object sender, EventArgs e)
        {
            if (sender != this)
                return;

#if NO
            // ����splitContainer_main��״̬
            MainForm.AppInfo.SetInt(
                "dp2searchform",
                "splitContainer_main",
                this.splitContainer_main.SplitterDistance);
            // ����splitContainer_up��״̬
            MainForm.AppInfo.SetInt(
                "dp2searchform",
                "splitContainer_up",
                this.splitContainer_up.SplitterDistance);
            /*
            // ����splitContainer_queryAndResultInfo��״̬
            MainForm.AppInfo.SetInt(
                "dp2searchform",
                "splitContainer_queryAndResultInfo",
                this.splitContainer_queryAndResultInfo.SplitterDistance);
             * */
#endif

            // �ָ���λ��
            // ����splitContainer_main��״̬
            this.MainForm.SaveSplitterPos(
                this.splitContainer_main,
                "dp2searchform",
                "splitContainer_main");
            // ����splitContainer_up��״̬
            this.MainForm.SaveSplitterPos(
                this.splitContainer_up,
                "dp2searchform",
                "splitContainer_up");
            // ����splitContainer_queryAndResultInfo��״̬
            this.MainForm.SaveSplitterPos(
                this.splitContainer_queryAndResultInfo,
                "dp2searchform",
                "splitContainer_queryAndResultInfo");

        }

        public void LoadSize()
        {
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state");

            /*
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "dp2_search_state",
                MainForm.DefaultMdiWindowWidth,
                MainForm.DefaultMdiWindowHeight);
            */
        }

        public void SaveSize()
        {
            /*
            MainForm.AppInfo.SaveMdiChildFormStates(this,
                "dp2_search_state");
            */
            if (this.WindowState != FormWindowState.Minimized)
            {
                MainForm.AppInfo.SaveMdiChildFormStates(this,
        "mdi_form_state");
            }
        }

        private void dp2SearchForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (stop != null)
            {
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }
            }

            if (this.m_nChangedCount > 0)
            {

                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�������� " + m_nChangedCount + " ���޸���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
                    "dp2SearchForm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

        }

        private void dp2SearchForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.commander != null)
                this.commander.Destroy();

            if (this.dp2ResTree1 != null)
                this.dp2ResTree1.Stop();

            if (stop != null) // �������
            {
                stop.Style = StopStyle.None;    // ��Ҫǿ���ж�
                stop.DoStop();

                stop.Unregister();	// ����������
                stop = null;
            }

            // ����ǰ�ָ��򵥼�����壬���ڱ���ָ�����λ��
            this.tabControl_query.SelectedTab = this.tabPage_simple;

            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                MainForm.AppInfo.SetString(
                    "dp2_search_simple_query",
                    "word",
                    this.textBox_simple_queryWord.Text);
                this.MainForm.AppInfo.SetString(
    "dp2_search_simple_query",
    "matchstyle",
    this.comboBox_simple_matchStyle.Text);


                this.MainForm.AppInfo.SetString(
                    "dp2_search_muline_query",
                    "content",
                    StringUtil.GetSomeLines(this.textBox_mutiline_queryContent.Text, 100)
                    );

                this.MainForm.AppInfo.SetString(
    "dp2_search_muline_query",
    "matchstyle",
    this.comboBox_multiline_matchStyle.Text);


                // ����resdircontrol����ѡ��
                ResPath respath = new ResPath(this.dp2ResTree1.SelectedNode);
                MainForm.AppInfo.SetString(
                    "dp2_search_simple_query",
                    "resdirpath",
                    respath.FullPath);

                if (this.dp2ResTree1.SortTableChanged == true)
                {
                    string strSortTables = dp2ResTree.SaveSortTables(this.dp2ResTree1.sort_tables);

                    this.MainForm.AppInfo.SetString(
           "dp2_search",
           "sort_tables",
           strSortTables);
                }
                this.MainForm.AppInfo.SetBoolean(
       "dp2_search",
       "enable_checkboxes",
       this.dp2ResTree1.CheckBoxes);


                string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_browse);
                this.MainForm.AppInfo.SetString(
                    "dp2searchform",
                    "record_list_column_width",
                    strWidths);

                this.MainForm.AppInfo.SetString(
    "dp2searchform",
    "query_lines",
    this.dp2QueryControl1.GetSaveString());


                SaveSize();

                this.MainForm.AppInfo.LoadMdiSize -= new EventHandler(AppInfo_LoadMdiSize);
                this.MainForm.AppInfo.SaveMdiSize -= new EventHandler(AppInfo_SaveMdiSize);

            }

            if (this.Channels != null)
                this.Channels.BeforeLogin -= new BeforeLoginEventHandle(Channels_BeforeLogin);

            if (this.m_commentViewer != null)
                this.m_commentViewer.Close();
        }

        public delegate void Delegate_ExpandResDir(string strResDirPath);

        void ExpandResDir(string strResDirPath)
        {
            try
            {
                this.Update();
                ResPath respath = new ResPath(strResDirPath);

                this.EnableControlsInSearching(false);

                // չ����ָ���Ľڵ�
                this.dp2ResTree1.ExpandPath(respath);

                this.EnableControlsInSearching(true);

                this.EventLoadFinish.Set();

                API.PostMessage(this.Handle, WM_INITIAL_FOCUS, 0, 0);
            }
            catch
            {
            }
        }

        /// <summary>
        /// �ȴ�װ�ؽ���
        /// </summary>
        public void WaitLoadFinish()
        {
            for (; ; )
            {
                Application.DoEvents();
                bool bRet = this.EventLoadFinish.WaitOne(10, true);
                if (bRet == true)
                    break;
            }
        }

        // ������߽�ֹ���пؼ�
        void EnableControls(bool bEnable)
        {
            this.listView_browse.Enabled = bEnable;
            EnableControlsInSearching(bEnable);
        }

        // ������߽�ֹ�󲿷ֿؼ�����listview����
        void EnableControlsInSearching(bool bEnable)
        {
            if (this.comboBox_simple_matchStyle.Text == "��ֵ")
                this.textBox_simple_queryWord.Enabled = false;
            else
                this.textBox_simple_queryWord.Enabled = bEnable;

            this.comboBox_simple_matchStyle.Enabled = bEnable;

            this.comboBox_multiline_matchStyle.Enabled = bEnable;

            this.button_searchSimple.Enabled = bEnable;

            this.textBox_mutiline_queryContent.Enabled = bEnable;

            this.dp2ResTree1.Enabled = bEnable;

            this.dp2QueryControl1.Enabled = bEnable;

            if (bEnable == false)
            {
                this.timer1.Start();
            }
            else
            {
                this.timer1.Stop();
            }
        }

        private void dp2ResTree1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            this.textBox_resPath.Text = e.Node.FullPath;
        }

        public LibraryChannel GetChannel(string strServerUrl)
        {
            return this.Channels.GetChannel(strServerUrl);
        }

        public string GetServerUrl(string strServerName)
        {
            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);

            if (server == null)
                return null;    // not found

            return server.Url;
        }

        public int SearchMaxCount
        {
            get
            {
                return MainForm.AppInfo.GetInt(
                    "dp2library",
                    "search_max_count",
                    1000);

            }
        }

        // parameters:
        //      strAction   ������ʽ  auto / simple / multiline /logic������ auto ��ʾ���յ�ǰѡ���������м���
        public int DoSearch(string strAction = "auto")
        {
            bool bClear = true; // �Ƿ��������������е�����

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                bClear = false;

            ClearListViewPropertyCache();
            if (bClear == true)
            {
                if (this.m_nChangedCount > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ���м�¼�б����� " + this.m_nChangedCount.ToString() + " ���޸���δ���档\r\n\r\n�Ƿ��������?\r\n\r\n(Yes �����Ȼ�����������No ��������)",
                        "dp2SearchForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return 0;
                }
                this.ClearListViewItems();

                ListViewUtil.ClearSortColumns(this.listView_browse);
            }

            this._linkMarcFile = null;

            if (this.tabControl_query.SelectedTab == this.tabPage_simple
                || strAction == "simple")
            {
                if (this.dp2ResTree1.CheckBoxes == true)
                    return DoCheckedSimpleSearch();
                else
                    return DoSimpleSearch();
            }
            else if (this.tabControl_query.SelectedTab == this.tabPage_multiline
                || strAction == "multiline")
            {
                if (this.dp2ResTree1.CheckBoxes == true)
                    return DoCheckedMutilineSearch();
                else
                    return DoMutilineSearch();
            }
            else if (this.tabControl_query.SelectedTab == this.tabPage_logic
                || strAction == "login")
            {
                return DoLogicSearch();
            }
            return 0;
        }

        void ClearListViewItems()
        {
            this.listView_browse.Items.Clear();

            ListViewUtil.ClearSortColumns(this.listView_browse);

            // ���������Ҫȷ����������
            for (int i = 1; i < this.listView_browse.Columns.Count; i++)
            {
                this.listView_browse.Columns[i].Text = i.ToString();
            }

            this.m_biblioTable = new Hashtable();
            this.m_nChangedCount = 0;

            if (this.m_commentViewer != null)
                this.m_commentViewer.Clear();
        }

        public static string GetMatchStyle(string strText)
        {
            // string strText = this.comboBox_matchStyle.Text;

            // 2009/8/6
            if (strText == "��ֵ")
                return "null";

            if (String.IsNullOrEmpty(strText) == true)
                return "left"; // ȱʡʱ��Ϊ�� ǰ��һ��

            if (strText == "ǰ��һ��")
                return "left";
            if (strText == "�м�һ��")
                return "middle";
            if (strText == "��һ��")
                return "right";
            if (strText == "��ȷһ��")
                return "exact";

            return strText; // ֱ�ӷ���ԭ��
        }

        // �������������������б��е���������
        int DoCheckedSimpleSearch()
        {
            string strError = "";
            int nRet = 0;

            bool bOutputKeyID = false;
            long lTotalCount = 0;	// ���м�¼����

            string strMatchStyle = GetMatchStyle(this.comboBox_multiline_matchStyle.Text);
            TargetItemCollection targets = null;

            // ��һ�׶�
            // return:
            //      -1  ����
            //      0   ��δѡ������Ŀ��
            //      1   �ɹ�
            nRet = this.dp2ResTree1.GetSearchTarget(out targets,
                out strError);
            if (nRet == -1 || nRet == 0)
                goto ERROR1;

            Debug.Assert(targets != null, "GetSearchTarget() �쳣");
            if (targets.Count == 0)
            {
                Debug.Assert(false, "");
                strError = "��δѡ������Ŀ��";
                goto ERROR1;
            }


            // �ڶ��׶�
            for (int i = 0; i < targets.Count; i++)
            {
                TargetItem item = (TargetItem)targets[i];
                item.Words = this.textBox_simple_queryWord.Text;
            }
            targets.MakeWordPhrases(
                strMatchStyle,
                false,   // Convert.ToBoolean(MainForm.applicationInfo.GetInt("simple_query_property", "auto_split_words", 1)),
                false,   // Convert.ToBoolean(MainForm.applicationInfo.GetInt("simple_query_property", "auto_detect_range", 0)),
                false    // Convert.ToBoolean(MainForm.applicationInfo.GetInt("simple_query_property", "auto_detect_relation", 0))
                );


            // ����
            for (int i = 0; i < targets.Count; i++)
            {
                TargetItem item = (TargetItem)targets[i];
                item.MaxCount = this.SearchMaxCount;
            }

            // �����׶�
            targets.MakeXml();

            // ��ʽ����

            // �޸Ĵ��ڱ���
            this.Text = "dp2������ " + this.textBox_simple_queryWord.Text;
            ClearListViewPropertyCache();

#if NO
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                // ��סCtrl����ʱ�򣬲����listview�е�ԭ������
            }
            else
            {
                ClearListViewItems();
            }
#endif

            bool bFillBrowseLine = true;
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                bFillBrowseLine = false;
            }

            this.textBox_resultInfo.Clear();

            this.EnableControlsInSearching(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            this.m_bInSearching = true;

            long lFillCount = 0;    // �Ѿ�װ��Ĳ���

            this.listView_browse.BeginUpdate();
            try
            {
                for (int i = 0; i < targets.Count; i++)
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


                    TargetItem item = (TargetItem)targets[i];

                    this.Channel = this.Channels.GetChannel(item.Url);
                    Debug.Assert(this.Channel != null, "Channels.GetChannel �쳣");

                    // textBox_simpleQuery_comment.Text += "����ʽXML:\r\n" + item.Xml + "\r\n";

                    // 2010/5/18
                    string strBrowseStyle = "id,cols";
                    string strOutputStyle = "";
                    if (bOutputKeyID == true)
                    {
                        strOutputStyle = "keyid";
                        strBrowseStyle = "keyid,id,key,cols";
                    }

                    if (bFillBrowseLine == false)
                        StringUtil.SetInList(ref strBrowseStyle, "cols", false);

                    // MessageBox.Show(this, item.Xml);
                    long lRet = this.Channel.Search(
                        stop,
                        item.Xml,
                        "default",
                        strOutputStyle,
                        out strError);
                    if (lRet == -1)
                    {
                        // textBox_simpleQuery_comment.Text += "����: " + strError + "\r\n";
                        MessageBox.Show(this, strError);
                        continue;
                    }
                    long lHitCount = lRet;
                    lTotalCount += lRet;

                    stop.SetProgressRange(0, lTotalCount);

                    // textBox_simpleQuery_comment.Text += "���м�¼��: " + Convert.ToString(nRet) + "\r\n";
                    this.textBox_resultInfo.Text += "������ '" + this.textBox_simple_queryWord.Text + "' ���� " + lTotalCount.ToString() + " ����¼\r\n";

                    if (lHitCount == 0)
                        continue;

                    long lStart = 0;
                    long lPerCount = Math.Min(50, lHitCount);
                    DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

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

                        stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                        lRet = Channel.GetSearchResult(
                            stop,
                            null,   // strResultSetName
                            lStart,
                            lPerCount,
                            strBrowseStyle,
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
                        for (int j = 0; j < searchresults.Length; j++)
                        {

                            NewLine(
                                this.listView_browse,
                                searchresults[j].Path + "@" + item.ServerName,
                                searchresults[j].Cols);
                        }

                        lStart += searchresults.Length;
                        lFillCount += searchresults.Length;
                        // lCount -= searchresults.Length;
                        if (lStart >= lHitCount || lPerCount <= 0)
                            break;

                        stop.SetProgressValue(lFillCount);
                    }
                }

                /*
            if (targets.Count > 1)
            {
                textBox_simpleQuery_comment.Text += "����������: " + Convert.ToString(lTotalCount) + "\r\n";
            }
                 * */

            }
            finally
            {
                this.listView_browse.EndUpdate();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();


                this.EnableControlsInSearching(true);

                this.m_bInSearching = false;
            }

            if (lTotalCount > 0)
                this.listView_browse.Focus();
            else
                this.textBox_simple_queryWord.Focus();

            return 0;

        ERROR1:
            MessageBox.Show(this, strError);
            this.textBox_simple_queryWord.Focus();
            return -1;
        }

        // �߼�����
        // �������������������б��е���������
        int DoLogicSearch()
        {
            string strError = "";
            int nRet = 0;

            long lHitCount = 0;
            long lLoaded = 0;

            List<QueryItem> items = null;

            nRet = this.dp2QueryControl1.BuildQueryXml(
            this.SearchMaxCount,
            "zh",
            out items,
            out strError);
            if (nRet == -1)
                goto ERROR1;


            // �޸Ĵ��ڱ���
            this.Text = "dp2������ �߼�����";

#if NO
            ClearListViewPropertyCache();

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                // ��סCtrl����ʱ�򣬲����listview�е�ԭ������
            }
            else
            {
                // this.listView_browse.Items.Clear();
                ClearListViewItems();
            }
#endif

            bool bFillBrowseLine = true;
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                bFillBrowseLine = false;
            }

            this.textBox_resultInfo.Clear();

            this.EnableControlsInSearching(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            this.m_bInSearching = true;

            long lTotalHitCount = 0;
            int nErrorCount = 0;

            this.listView_browse.BeginUpdate();
            try
            {
                for (int j = 0; j < items.Count; j++)
                {
                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }
                    }
                    QueryItem item = items[j];

                    string strServerName = item.ServerName;

                    dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
                    if (server == null)
                    {
                        strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                        goto ERROR1;
                    }

                    string strServerUrl = server.Url;
                    this.Channel = this.Channels.GetChannel(strServerUrl);

                    string strOutputStyle = "id";

                    long lRet = Channel.Search(stop,
                        item.QueryXml,
                        "default",
                        strOutputStyle,
                        out strError);
                    if (lRet == -1)
                    {
                        this.textBox_resultInfo.Text += "����ʽ '" + item.QueryXml + "' ����ʱ��������" + strError + "\r\n";
                        nErrorCount++;
                        continue;
                    }

                    lHitCount = lRet;

                    lTotalHitCount += lHitCount;

                    stop.SetProgressRange(0, lTotalHitCount);

                    this.textBox_resultInfo.Text += "������ " + lTotalHitCount.ToString() + " ����������δ����...\r\n";

                    if (lHitCount == 0)
                        continue;

                    long lStart = 0;
                    long lPerCount = Math.Min(50, lHitCount);
                    DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

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

                        stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                        lRet = Channel.GetSearchResult(
                            stop,
                            null,   // strResultSetName
                            lStart,
                            lPerCount,
                            bFillBrowseLine == true ? "id,cols" : "id",
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
                            NewLine(
                                this.listView_browse,
                                searchresults[i].Path + "@" + strServerName,
                                searchresults[i].Cols);

                            lLoaded++;
                            stop.SetProgressValue(lLoaded);
                        }

                        lStart += searchresults.Length;
                        // lCount -= searchresults.Length;
                        if (lStart >= lHitCount || lPerCount <= 0)
                            break;

                    }
                } // end of items

                if (nErrorCount == 0)
                    this.textBox_resultInfo.Text = "������ " + lTotalHitCount.ToString() + " ��";
                else
                    this.textBox_resultInfo.Text += "������ɡ������� " + lTotalHitCount.ToString() + " ��";
            }
            finally
            {
                this.listView_browse.EndUpdate();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControlsInSearching(true);

                this.m_bInSearching = false;
            }

            if (lTotalHitCount > 0)
                this.listView_browse.Focus();
            else
                this.dp2QueryControl1.Focus();

            return 0;

        ERROR1:
            this.textBox_resultInfo.Text += strError;
            MessageBox.Show(this, strError);
            this.dp2QueryControl1.Focus();
            return -1;
        }

        // ���м�������Ϊ��CheckBox״̬
        // �������������������б��е���������
        int DoSimpleSearch()
        {
            string strError = "";
            int nRet = 0;

            long lHitCount = 0;

            string strServerName = "";
            string strServerUrl = "";
            string strDbName = "";
            string strFrom = "";
            string strFromStyle = "";

            nRet = dp2ResTree.GetNodeInfo(this.dp2ResTree1.SelectedNode,
                out strServerName,
                out strServerUrl,
                out strDbName,
                out strFrom,
                out strFromStyle,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            strFromStyle = dp2ResTree.GetDisplayFromStyle(strFromStyle, true, false);   // ע�⣬ȥ�� __ ��ͷ����Щ��Ӧ�û�ʣ������һ�� style��_ ��ͷ�Ĳ�Ҫ�˳�

            this.Channel = this.Channels.GetChannel(strServerUrl);

            // �޸Ĵ��ڱ���
            this.Text = "dp2������ " + this.textBox_simple_queryWord.Text;


#if NO
            ClearListViewPropertyCache();

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                // ��סCtrl����ʱ�򣬲����listview�е�ԭ������
            }
            else
            {
                // this.listView_browse.Items.Clear();
                ClearListViewItems();
            }
#endif

            bool bFillBrowseLine = true;
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                bFillBrowseLine = false;
            }

            this.textBox_resultInfo.Clear();

            this.EnableControlsInSearching(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            this.m_bInSearching = true;

            this.listView_browse.BeginUpdate();
            try
            {
                if (String.IsNullOrEmpty(strDbName) == true)
                    strDbName = "<all>";

                if (String.IsNullOrEmpty(strFrom) == true)
                {
                    strFrom = "<all>";
                }

                if (String.IsNullOrEmpty(strFromStyle) == true)
                {
                    strFromStyle = "<all>";
                }

                // ע��"null"ֻ����ǰ�˶��ݴ��ڣ����ں��ǲ��������ν��matchstyle��
                string strMatchStyle = GetMatchStyle(this.comboBox_simple_matchStyle.Text);

                if (this.textBox_simple_queryWord.Text == "")
                {
                    if (strMatchStyle == "null")
                    {
                        this.textBox_simple_queryWord.Text = "";

                        // ר�ż�����ֵ
                        strMatchStyle = "exact";
                    }
                    else
                    {
                        // Ϊ���ڼ�����Ϊ�յ�ʱ�򣬼�����ȫ���ļ�¼
                        strMatchStyle = "left";
                    }
                }
                else
                {
                    if (strMatchStyle == "null")
                    {
                        strError = "������ֵ��ʱ���뱣�ּ�����Ϊ��";
                        goto ERROR1;
                    }
                }

                string strQueryXml = "";
                long lRet = Channel.SearchBiblio(stop,
                    strDbName,
                    this.textBox_simple_queryWord.Text,
                    this.SearchMaxCount,    // 1000,
                    strFromStyle,
                    strMatchStyle,
                    this.Lang,
                    null,   // strResultSetName
                    "", // strSearchStyle
                    "", // strOutputStyle
                    out strQueryXml,
                    out strError);
                if (lRet == -1)
                {
                    this.textBox_resultInfo.Text += "������ '" + this.textBox_simple_queryWord.Text + "' ����ʱ��������" + strError + "\r\n";
                    goto ERROR1;
                }

                lHitCount = lRet;

                this.textBox_resultInfo.Text += "������ '" + this.textBox_simple_queryWord.Text + "' ���� " + lHitCount.ToString() + " ����¼\r\n";

                stop.SetProgressRange(0, lHitCount);

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                this.listView_browse.Focus();

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

                    stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    lRet = Channel.GetSearchResult(
                        stop,
                        null,   // strResultSetName
                        lStart,
                        lPerCount,
                        bFillBrowseLine == true ? "id,cols" : "id",
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

                        NewLine(
                            this.listView_browse,
                            searchresults[i].Path + "@" + strServerName,
                            searchresults[i].Cols);
                    }

                    lStart += searchresults.Length;
                    // lCount -= searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;

                    stop.SetProgressValue(lStart);

                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
            }
            finally
            {
                this.listView_browse.EndUpdate();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControlsInSearching(true);

                this.m_bInSearching = false;
            }

            if (lHitCount > 0)
                this.listView_browse.Focus();
            else
                this.textBox_simple_queryWord.Focus();

            return 0;

        ERROR1:
            MessageBox.Show(this, strError);
            this.textBox_simple_queryWord.Focus();
            return -1;
        }

        // ���м���
        // �������������������б��е���������
        int DoCheckedMutilineSearch()
        {
            string strError = "";
            int nRet = 0;

            long lTotalCount = 0;	// ���м�¼����
            long lFillCount = 0;
            long lHitCount = 0;
            int nLineCount = 0;

            List<string> hited_lines = new List<string>(4096);
            List<string> nothited_lines = new List<string>(4096);

            string strMatchStyle = GetMatchStyle(this.comboBox_multiline_matchStyle.Text);

            TargetItemCollection targets = null;

            // ��һ�׶�
            // return:
            //      -1  ����
            //      0   ��δѡ������Ŀ��
            //      1   �ɹ�
            nRet = this.dp2ResTree1.GetSearchTarget(out targets,
                out strError);
            if (nRet == -1 || nRet == 0)
                goto ERROR1;

            Debug.Assert(targets != null, "GetSearchTarget() �쳣");
            if (targets.Count == 0)
            {
                Debug.Assert(false, "");
                strError = "��δѡ������Ŀ��";
                goto ERROR1;
            }

            bool bDontAsk = this.MainForm.AppInfo.GetBoolean(
                "dp2_search_muline_query",
                "matchstyle_middle_dontask",
                false);
            if (strMatchStyle == "middle" && bDontAsk == false)
            {
                MessageDialog.Show(this,
                    "��ѡ���� �м�һ�� ƥ�䷽ʽ���м���������ƥ�䷽ʽ�����ٶ�������������ܣ���ò�������ƥ�䷽ʽ���Ա���߼����ٶȡ�",
                    "�´β��ٳ��ִ˶Ի���",
                    ref bDontAsk);
                if (bDontAsk == true)
                {
                    this.MainForm.AppInfo.SetBoolean(
                        "dp2_search_muline_query",
                        "matchstyle_middle_dontask",
                        bDontAsk);
                }
            }

            List<string> dbnames = new List<string>();
            nRet = targets.GetDbNameList(out dbnames, out strError);
            if (nRet == -1)
                goto ERROR1;

            if (dbnames.Count > 1)
            {
                DbSelectListDialog select_dlg = new DbSelectListDialog();
                GuiUtil.SetControlFont(select_dlg, this.Font);
                select_dlg.DbNames = dbnames;
                select_dlg.SelectAllDb = this.SelectAllDb;
                this.MainForm.AppInfo.LinkFormState(select_dlg, "dp2searchform_DbSelectListDialog_state");
                select_dlg.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(select_dlg);
                if (select_dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return 0;

                this.SelectAllDb = select_dlg.SelectAllDb;

                if (select_dlg.SelectAllDb == true)
                    dbnames = null; // ȫ�����ж�Ҫ
                else
                    dbnames = select_dlg.DbNames;   // ˳��ѡ��
            }
            else
                dbnames = null;

            // �޸Ĵ��ڱ���
            this.Text = "dp2������ " + this.textBox_simple_queryWord.Text;

#if NO
            ClearListViewPropertyCache();

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                // ��סCtrl����ʱ�򣬲����listview�е�ԭ������
            }
            else
            {
                // this.listView_browse.Items.Clear();
                ClearListViewItems();
            }
#endif

            bool bFillBrowseLine = true;
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                bFillBrowseLine = false;
            }

            this.textBox_resultInfo.Clear();

            this.EnableControlsInSearching(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            this.m_bInSearching = true;

            // long lTotalHitCount = 0;

            this.listView_browse.BeginUpdate();
            try
            {
                DateTime start_time = DateTime.Now;
                stop.SetProgressRange(0, this.textBox_mutiline_queryContent.Lines.Length);

                for (int j = 0; j < this.textBox_mutiline_queryContent.Lines.Length; j++)
                {
                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }
                    }

                    string strLine = this.textBox_mutiline_queryContent.Lines[j].Trim();

                    stop.SetProgressValue(j);

                    if (String.IsNullOrEmpty(strLine) == true)
                        continue;

                    nLineCount++;

                    // �ڶ��׶�
                    for (int i = 0; i < targets.Count; i++)
                    {
                        TargetItem item = (TargetItem)targets[i];
                        item.Words = strLine;
                    }
                    targets.MakeWordPhrases(
                        strMatchStyle,
                        false,   // Convert.ToBoolean(MainForm.applicationInfo.GetInt("simple_query_property", "auto_split_words", 1)),
                        false,   // Convert.ToBoolean(MainForm.applicationInfo.GetInt("simple_query_property", "auto_detect_range", 0)),
                        false    // Convert.ToBoolean(MainForm.applicationInfo.GetInt("simple_query_property", "auto_detect_relation", 0))
                        );

                    // ����
                    for (int i = 0; i < targets.Count; i++)
                    {
                        TargetItem item = (TargetItem)targets[i];
                        item.MaxCount = this.SearchMaxCount;
                    }

                    // �����׶�
                    targets.MakeXml();

                    long lPerLineHitCount = 0;

                    List<ListViewItem> new_items = new List<ListViewItem>();

                    for (int i = 0; i < targets.Count; i++)
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


                        TargetItem item = (TargetItem)targets[i];

                        this.Channel = this.Channels.GetChannel(item.Url);
                        Debug.Assert(this.Channel != null, "Channels.GetChannel �쳣");

                        long lRet = this.Channel.Search(
            stop,
            item.Xml,
            "default",
            "", // strOutputStyle,
            out strError);
                        if (lRet == -1)
                        {
                            this.textBox_resultInfo.Text += "������ '" + strLine + "' ����ʱ��������" + strError + "\r\n";
                            continue;
                        }

                        lHitCount = lRet;
                        lTotalCount += lHitCount;
                        lPerLineHitCount += lHitCount;

                        if (lHitCount == 0)
                            continue;

                        // stop.SetProgressRange(0, lTotalCount);

                        long lStart = 0;
                        long lPerCount = Math.Min(50, lHitCount);
                        DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                        // this.listView_browse.Focus();

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

                            stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " ('" + strLine + "' ���� " + lHitCount.ToString() + " ����¼) ...");

                            lRet = Channel.GetSearchResult(
                                stop,
                                "default",   // strResultSetName
                                lStart,
                                lPerCount,
                                bFillBrowseLine == true ? "id,cols" : "id",
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
                            for (int k = 0; k < searchresults.Length; k++)
                            {
                                ListViewItem new_item = NewLine(
                                    this.listView_browse,
                                    searchresults[k].Path + "@" + item.ServerName,
                                    searchresults[k].Cols);
                                new_items.Add(new_item);
                            }

                            lStart += searchresults.Length;
                            lFillCount += searchresults.Length;
                            // stop.SetProgressValue(lFillCount);
                            // lCount -= searchresults.Length;
                            if (lStart >= lHitCount || lPerCount <= 0)
                                break;

                        }
                    }

                    int nEndLine = this.listView_browse.Items.Count;

                    // ��Ҫɸѡ
                    if (dbnames != null && new_items.Count > 1)
                    {
                        int nRemoved = RemoveMultipleItems(dbnames, new_items);
                        lPerLineHitCount -= nRemoved;
                        lTotalCount -= nRemoved;
                    }


                    // this.textBox_resultInfo.Text += "������ '" + strLine + "' ���� " + lPerLineHitCount.ToString() + " ����¼\r\n";
                    if (lPerLineHitCount == 0)
                        nothited_lines.Add(strLine);
                    else
                        hited_lines.Add(strLine + "\t" + lPerLineHitCount.ToString());

                } // end of lines

                TimeSpan delta = DateTime.Now - start_time;

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
                // �����ʾ�������ע��
                string strComment = "���� " + nLineCount.ToString() + " ����ʱ " + delta.ToString() + "\r\n";
                if (hited_lines.Count > 0)
                {
                    strComment += "*** ���¼����ʹ����� " + lTotalCount.ToString() + " ��:\r\n";
                    foreach (string strLine in hited_lines)
                    {
                        strComment += strLine + "\r\n";
                    }
                }
                if (nothited_lines.Count > 0)
                {
                    strComment += "*** ���¼�����û������:\r\n";
                    foreach (string strLine in nothited_lines)
                    {
                        strComment += strLine + "\r\n";
                    }
                }
                this.textBox_resultInfo.Text = strComment;
            }
            finally
            {
                this.listView_browse.EndUpdate();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControlsInSearching(true);

                this.m_bInSearching = false;
            }

            if (lTotalCount > 0)
                this.listView_browse.Focus();
            else
                this.textBox_mutiline_queryContent.Focus();

            return 0;

        ERROR1:
            MessageBox.Show(this, strError);
            this.textBox_simple_queryWord.Focus();
            return -1;
        }

        // ȥ����������м�¼��
        int RemoveMultipleItems(List<string> dbnames,
            List<ListViewItem> items)
        {
            List<string> hit_dbnames = new List<string>();
            // �г��Ѿ����е����ݿ���
            foreach (ListViewItem item in items)
            {
                string strNameString = GetDbNameString(item.Text);
                hit_dbnames.Add(strNameString);
            }

            string strFoundDbName = "";
            foreach (string strDbName in dbnames)
            {
                if (hit_dbnames.IndexOf(strDbName) != -1)
                {
                    strFoundDbName = strDbName;
                    break;
                }
            }

            if (string.IsNullOrEmpty(strFoundDbName) == true)
            {
                Debug.Assert(false, "");
                return 0;
            }

            // ����������ݿ�������Ķ�ɾ��
            int nDeleteCount = 0;
            foreach (ListViewItem item in items)
            {
                string strNameString = GetDbNameString(item.Text);

                if (strNameString != strFoundDbName)
                {
                    this.listView_browse.Items.Remove(item);
                    nDeleteCount++;
                }
            }

            return nDeleteCount;
        }

        // ��·���ַ����л�ñ�ʾ���ݿ���ַ���
        // '���ݿ���@��������'
        static string GetDbNameString(string strRecPath)
        {
            string strServerName = "";
            string strPath = "";
            ParseRecPath(strRecPath,
                out strServerName,
                out strPath);
            string strDbName = GetDbName(strPath);

            return strDbName + "@" + strServerName;
        }

        // ���м�������Ϊ��CheckBox״̬
        // �������������������б��е���������
        int DoMutilineSearch()
        {
            string strError = "";
            int nRet = 0;

            long lHitCount = 0;

            List<string> hited_lines = new List<string>(4096);
            List<string> nothited_lines = new List<string>(4096);

            string strServerName = "";
            string strServerUrl = "";
            string strDbName = "";
            string strFrom = "";
            string strFromStyle = "";

            nRet = dp2ResTree.GetNodeInfo(this.dp2ResTree1.SelectedNode,
                out strServerName,
                out strServerUrl,
                out strDbName,
                out strFrom,
                out strFromStyle,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.Channel = this.Channels.GetChannel(strServerUrl);

            // �޸Ĵ��ڱ���
            this.Text = "dp2������ " + this.textBox_simple_queryWord.Text;

#if NO
            ClearListViewPropertyCache();

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                // ��סCtrl����ʱ�򣬲����listview�е�ԭ������
            }
            else
            {
                // this.listView_browse.Items.Clear();
                ClearListViewItems();
            }
#endif

            bool bFillBrowseLine = true;
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                bFillBrowseLine = false;
            }
            
            this.textBox_resultInfo.Clear();

            this.EnableControlsInSearching(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            this.m_bInSearching = true;

            long lTotalHitCount = 0;
            int nLineCount = 0;

            this.listView_browse.BeginUpdate();
            try
            {
                if (String.IsNullOrEmpty(strDbName) == true)
                    strDbName = "<all>";

                if (String.IsNullOrEmpty(strFrom) == true)
                {
                    strFrom = "<all>";
                }

                if (String.IsNullOrEmpty(strFromStyle) == true)
                {
                    strFromStyle = "<all>";
                }

                string strMatchStyle = GetMatchStyle(this.comboBox_multiline_matchStyle.Text);

                bool bDontAsk = this.MainForm.AppInfo.GetBoolean(
"dp2_search_muline_query",
"matchstyle_middle_dontask",
false);
                if (strMatchStyle == "middle" && bDontAsk == false)
                {
                    MessageDialog.Show(this,
                        "��ѡ���� �м�һ�� ƥ�䷽ʽ���м���������ƥ�䷽ʽ�����ٶ�������������ܣ���ò�������ƥ�䷽ʽ���Ա���߼����ٶȡ�",
                        "�´β��ٳ��ִ˶Ի���",
                        ref bDontAsk);
                    if (bDontAsk == true)
                    {
                        this.MainForm.AppInfo.SetBoolean(
                            "dp2_search_muline_query",
                            "matchstyle_middle_dontask",
                            bDontAsk);
                    }
                }

                DateTime start_time = DateTime.Now;

                stop.SetProgressRange(0, this.textBox_mutiline_queryContent.Lines.Length);

                for (int j = 0; j < this.textBox_mutiline_queryContent.Lines.Length; j++)
                {
                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }
                    }

                    string strLine = this.textBox_mutiline_queryContent.Lines[j].Trim();

                    stop.SetProgressValue(j);

                    if (String.IsNullOrEmpty(strLine) == true)
                        continue;

                    string strQueryXml = "";
                    long lRet = Channel.SearchBiblio(stop,
                        strDbName,
                        strLine,
                        this.SearchMaxCount,    // 1000,
                        strFromStyle,
                        strMatchStyle,
                        this.Lang,
                        null,   // strResultSetName
                        "",    // strSearchStyle
                        "", // strOutputStyle
                        out strQueryXml,
                        out strError);
                    if (lRet == -1)
                    {
                        this.textBox_resultInfo.Text += "������ '" + strLine + "' ����ʱ��������" + strError + "\r\n";
                        continue;
                    }

                    lHitCount = lRet;

                    nLineCount++;

                    lTotalHitCount += lHitCount;
                    // this.textBox_resultInfo.Text += "������ '" + strLine + "' ���� " + lHitCount.ToString() + " ����¼\r\n";
                    if (lHitCount == 0)
                        nothited_lines.Add(strLine);
                    else
                        hited_lines.Add(strLine + "\t" + lHitCount.ToString());

                    if (lHitCount == 0)
                        continue;

                    long lStart = 0;
                    long lPerCount = Math.Min(50, lHitCount);
                    DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

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

                        stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " ('" + strLine + "' ���� " + lHitCount.ToString() + " ����¼) ...");

                        lRet = Channel.GetSearchResult(
                            stop,
                            null,   // strResultSetName
                            lStart,
                            lPerCount,
                            bFillBrowseLine == true ? "id,cols" : "id",
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
                            NewLine(
                                this.listView_browse,
                                searchresults[i].Path + "@" + strServerName,
                                searchresults[i].Cols);
                        }

                        lStart += searchresults.Length;
                        // lCount -= searchresults.Length;
                        if (lStart >= lHitCount || lPerCount <= 0)
                            break;

                    }
                } // end of lines

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);

                TimeSpan delta = DateTime.Now - start_time;

                // �����ʾ�������ע��
                string strComment = "���� "+nLineCount.ToString()+" ����ʱ " + delta.ToString() + "\r\n";
                if (hited_lines.Count > 0)
                {
                    strComment += "*** ���¼����ʹ����� " + lTotalHitCount.ToString() + " ��:\r\n";
                    foreach (string strLine in hited_lines)
                    {
                        strComment += strLine + "\r\n";
                    }
                }
                if (nothited_lines.Count > 0)
                {
                    strComment += "*** ���¼�����û������:\r\n";
                    foreach (string strLine in nothited_lines)
                    {
                        strComment += strLine + "\r\n";
                    }
                }
                this.textBox_resultInfo.Text = strComment;
            }
            finally
            {
                this.listView_browse.EndUpdate();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControlsInSearching(true);

                this.m_bInSearching = false;
            }

            if (lTotalHitCount > 0)
                this.listView_browse.Focus();
            else
                this.textBox_mutiline_queryContent.Focus();

            return 0;

        ERROR1:
            MessageBox.Show(this, strError);
            this.textBox_simple_queryWord.Focus();
            return -1;
        }

        // ������¼·����
        // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
        public static void ParseRecPath(string strRecPath,
            out string strServerName,
            out string strPath)
        {
            int nRet = strRecPath.IndexOf("@");
            if (nRet == -1)
            {
                strServerName = "";
                strPath = strRecPath;
                return;
            }
            strServerName = strRecPath.Substring(nRet + 1).Trim();
            strPath = strRecPath.Substring(0, nRet).Trim();
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

        // ��listview���׷��һ��
        public static ListViewItem NewLine(
            ListView list,
            string strID,
            string[] others)
        {
            if (others == null)
                ListViewUtil.EnsureColumns(list, 1);
            else
                ListViewUtil.EnsureColumns(list, others.Length + 1);

            ListViewItem item = new ListViewItem(strID, 0);

            list.Items.Add(item);

            if (others != null)
            {
                for (int i = 0; i < others.Length; i++)
                {
                    item.SubItems.Add(others[i]);
                }
            }

            return item;
        }

        public static void ChangeCols(ListViewItem item,
            string strRecPath,
            string[] cols)
        {
            ListViewUtil.ChangeItemText(item, 0, strRecPath);

            int nCol = 1;
            foreach (string s in cols)
            {
                ListViewUtil.ChangeItemText(item, nCol, s);
                nCol++;
            }
            // TODO: ��ն������
        }

        /*
        // ȷ���б��������㹻
        public static void EnsureColumns(ListView list,
            int nCount)
        {
            if (list.Columns.Count >= nCount)
                return;

            for (int i = list.Columns.Count; i < nCount; i++)
            {
                string strText = "";
                if (i == 0)
                {
                    strText = "��¼·��";
                }
                else
                {
                    strText = Convert.ToString(i);
                }

                ColumnHeader col = new ColumnHeader();
                col.Text = strText;
                col.Width = 200;
                list.Columns.Add(col);
            }

        }
         * */

        #region ISearchForm �ӿں���

        // ���󡢴����Ƿ���Ч?
        public bool IsValid()
        {
            if (this.IsDisposed == true)
                return false;

            return true;
        }

        public string CurrentProtocol
        {
            get
            {
                return "dp2library";
            }
        }

        public string CurrentResultsetPath
        {
            get
            {
                string strServerName = "";
                string strServerUrl = "";
                string strDbName = "";
                string strFrom = "";
                string strFromStyle = "";

                string strError = "";

                int nRet = dp2ResTree.GetNodeInfo(this.dp2ResTree1.SelectedNode,
                    out strServerName,
                    out strServerUrl,
                    out strDbName,
                    out strFrom,
                    out strFromStyle,
                    out strError);
                if (nRet == -1)
                    return "";

                return strServerName
                    + "/" + strDbName
                    + "/" + strFrom
                    + "/" + this.textBox_simple_queryWord.Text
                    + "/default";
            }
        }

        // ˢ��һ��MARC��¼
        // return:
        //      -2  ��֧��
        //      -1  error
        //      0   ��ش����Ѿ����٣�û�б�Ҫˢ��
        //      1   �Ѿ�ˢ��
        //      2   �ڽ������û���ҵ�Ҫˢ�µļ�¼
        public int RefreshOneRecord(
            string strPathParam,
            string strAction,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            int index = -1;
            string strPath = "";
            string strDirection = "";
            nRet = Global.ParsePathParam(strPathParam,
                out index,
                out strPath,
                out strDirection,
                out strError);
            if (nRet == -1)
                return -1;

            List<ListViewItem> items = new List<ListViewItem>();

            if (index == -1)
            {
                ListViewItem item = ListViewUtil.FindItem(this.listView_browse, strPath, 0);
                if (item == null)
                {
                    strError = "·��Ϊ '" + strPath + "' ���������б���û���ҵ�";
                    return 2;
                }
                items.Add(item);
            }
            else
            {
                if (index >= this.listView_browse.Items.Count)
                {
                    strError = "index ["+index.ToString()+"] Խ�������β��";
                    return -1;
                }
                items.Add(this.listView_browse.Items[index]);
            }

            if (strAction == "refresh")
            {
                nRet = RefreshListViewLines(items,
        out strError);
                if (nRet == -1)
                    return -1;

                DoViewComment(false);
                return 1;
            }

            return 0;
        }


        // ɾ��һ��MARC/XML��¼
        // parameters:
        //      strSavePath ����Ϊ"����ͼ��/1@���ط�����"��û��Э�������֡�
        // return:
        //      -1  error
        //      0   suceed
        public int DeleteOneRecord(
            string strSavePath,
            byte[] baTimestamp,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";

            // ������¼·��
            string strServerName = "";
            string strPurePath = "";
            ParseRecPath(strSavePath,
                out strServerName,
                out strPurePath);

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            } 
            string strServerUrl = server.Url;

            this.Channel = this.Channels.GetChannel(strServerUrl);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ɾ����¼ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                stop.SetMessage("����ɾ����Ŀ��¼ " + strPurePath + " ...");


                string[] formats = null;
                formats = new string[1];
                formats[0] = "xml";

                // string[] results = null;
                //                 byte[] baTimestamp = null;

                string strOutputBibilioRecPath = "";

                long lRet = Channel.SetBiblioInfo(
                    stop,
                    "delete",
                    strPurePath,
                    "xml",
                    "",
                    baTimestamp,
                    "",
                    out strOutputBibilioRecPath,
                    out baOutputTimestamp,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }
            return 0;
        ERROR1:
            return -1;
        }

        // ͬ��һ�� MARC/XML ��¼
        // ��� Lversion �ȼ������еļ�¼�£����� strMARC ���ݸ��¼������ڵļ�¼
        // ��� lVersion �ȼ������еļ�¼��(Ҳ����˵ Lverion ��ֵƫС)����ô�� strMARC ��ȡ����¼���µ���¼��
        // parameters:
        //      lVersion    [in]��¼���� Version [out] �������ļ�¼ Version
        // return:
        //      -1  ����
        //      0   û�б�Ҫ����
        //      1   �Ѿ����µ� ������
        //      2   ��Ҫ�� strMARC ��ȡ�����ݸ��µ���¼��
        public int SyncOneRecord(string strPath,
            ref long lVersion,
            ref string strSyntax,
            ref string strMARC,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            BiblioInfo info = null;

            // �洢�������Ŀ��¼ XML
            info = (BiblioInfo)this.m_biblioTable[strPath];
            if (info == null)
            {
                // ���������ڴ���δ�洢��������൱�� version = 0
                if (lVersion > 0)
                {
                    // Ԥ��׼���� info 
                    // �ҵ� Item ��
                    ListViewItem item = ListViewUtil.FindItem(this.listView_browse, strPath, 0);
                    if (item == null)
                    {
                        strError = "·��Ϊ '"+strPath+"' ���������б���û���ҵ�";
                        return -1;
                    }

                    nRet = GetBiblioInfo(
                        true,
                        item,
                        out info,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // �������ִ��
                }
                else
                    return 0;
            }

            if (info != null)
            {
                if (lVersion == info.NewVersion)
                    return 0;

                string strXml = "";
                if (string.IsNullOrEmpty(info.NewXml) == true)
                    strXml = info.OldXml;
                else
                    strXml = info.NewXml;

                if (lVersion > info.NewVersion)
                {
                    // ���� strMARC �ĸ���һ��
                    info.NewVersion = lVersion;

                    if (strSyntax == "xml")
                        strXml = strMARC;
                    else
                    {
                        XmlDocument domMarc = new XmlDocument();
                        domMarc.LoadXml(strXml);

                        nRet = MarcUtil.Marc2Xml(strMARC,
                            strSyntax,
                            out domMarc,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        strXml = domMarc.OuterXml;
                    }
                }
                else
                {
                    // ���� info �ĸ���һ��
                    lVersion = info.NewVersion;

                    if (strSyntax == "xml")
                        strMARC = strXml;
                    else
                    {
                        // ��XML��ʽת��ΪMARC��ʽ
                        // �Զ������ݼ�¼�л��MARC�﷨
                        nRet = MarcUtil.Xml2Marc(strXml,
                            true,
                            null,
                            out strSyntax,
                            out strMARC,
                            out strError);
                        if (nRet == -1)
                            return -1;
                    }
                    return 2;
                }

                /*
                if (string.IsNullOrEmpty(info.NewXml) == true)
                    info.OldXml = strXml;
                else
                    info.NewXml = strXml;
                 * */
                if (string.IsNullOrEmpty(info.NewXml) == true)
                    this.m_nChangedCount++;
                info.NewXml = strXml;

                DoViewComment(false);
                return 1;
            }

            return 0;
        }

        // ���һ��MARC/XML��¼
        // return:
        //      -1  error ����not found
        //      0   found
        //      1   Ϊ��ϼ�¼
        public int GetOneRecord(
            string strStyle,
            int nTest,
            string strPathParam,
            string strParameters,   // bool bHilightBrowseLine,
            out string strSavePath,
            out string strRecord,
            out string strXmlFragment,
            out string strOutStyle,
            out byte[] baTimestamp,
            out long lVersion,
            out DigitalPlatform.Z3950.Record record,
            out Encoding currrentEncoding,
            out LoginInfo logininfo,
            out string strError)
        {
            strXmlFragment = "";
            strRecord = "";
            record = null;
            strError = "";
            currrentEncoding = this.CurrentEncoding;
            baTimestamp = null;
            strSavePath = "";
            strOutStyle = "marc";
            logininfo = new LoginInfo();
            lVersion = 0;

#if NO
            // ��ֹ����
            if (m_bInSearching == true)
            {
                strError = "��ǰ�������ڱ�һ��δ�����ĳ�����ʹ�ã��޷���ü�¼�����Ժ����ԡ�";
                return -1;
            }
#endif

            if (strStyle != "marc" && strStyle != "xml")
            {
                strError = "dp2SearchFormֻ֧�ֻ�ȡMARC��ʽ��¼��xml��ʽ��¼����֧�� '" + strStyle + "' ��ʽ�ļ�¼";
                return -1;
            }
            int nRet = 0;

            int index = -1;
            string strPath = "";
            string strDirection = "";
            nRet = Global.ParsePathParam(strPathParam,
                out index,
                out strPath,
                out strDirection,
                out strError);
            if (nRet == -1)
                return -1;

            if (index == -1)
            {
                string strOutputPath = "";
                nRet = InternalGetOneRecord(
                    true,
                    strStyle,
                    strPath,
                    strDirection,
                    strParameters,  // 2013/9/22
                    out strRecord,
                    out strXmlFragment,
                    out strOutputPath,
                    out strOutStyle,
                    out baTimestamp,
                    out record,
                    out currrentEncoding,
                    out strError);
                if (string.IsNullOrEmpty(strOutputPath) == false)
                    strSavePath = this.CurrentProtocol + ":" + strOutputPath;
                return nRet;
            }

            bool bHilightBrowseLine = StringUtil.IsInList("hilight_browse_line", strParameters);

            if (index >= this.listView_browse.Items.Count)
            {
                // ������������жϹ���������Դ�����������
                strError = "Խ�������β��";
                return -1;
            }

            ListViewItem curItem = this.listView_browse.Items[index];

            if (bHilightBrowseLine == true)
            {
                // �޸�listview�������ѡ��״̬
                for (int i = 0; i < this.listView_browse.SelectedItems.Count; i++)
                {
                    this.listView_browse.SelectedItems[i].Selected = false;
                }

                curItem.Selected = true;
                curItem.EnsureVisible();
            }

#if NO
            if (this.linkMarcFile != null)
            {
                BiblioInfo info = null;
                int nRet = GetBiblioInfo(
                    true,
                    curItem,
                    out info,
                    out strError);
                if (info == null)
                {
                    strError = "not found";
                    return -1;
                }

                if (strStyle == "marc")
                {
                    string strMarcSyntax = "";
                    string strOutMarcSyntax = "";
                    // �����ݼ�¼�л��MARC��ʽ
                    nRet = MarcUtil.Xml2Marc(info.OldXml,
                        true,
                        strMarcSyntax,
                        out strOutMarcSyntax,
                        out strRecord,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "XMLת����MARC��¼ʱ����: " + strError;
                        return -1;
                    }

                    record = new DigitalPlatform.Z3950.Record();
                    if (strOutMarcSyntax == "unimarc" || strOutMarcSyntax == "")
                        record.m_strSyntaxOID = "1.2.840.10003.5.1";
                    else if (strOutMarcSyntax == "usmarc")
                        record.m_strSyntaxOID = "1.2.840.10003.5.10";
                    else if (strOutMarcSyntax == "dc")
                        record.m_strSyntaxOID = "?";
                    else
                    {
                        strError = "δ֪��MARC syntax '" + strOutMarcSyntax + "'";
                        return -1;
                    }

                    // �����Ŀ���������XMLƬ��
                    nRet = GetXmlFragment(info.OldXml,
            out strXmlFragment,
            out strError);
                    if (nRet == -1)
                        return -1;
                }
                else
                {
                    strRecord = info.OldXml;
                    strOutStyle = strStyle;

                    record = new DigitalPlatform.Z3950.Record();
                    record.m_strSyntaxOID = "1.2.840.10003.5.109.10";
                }

                return 0;
            }
#endif

            strPath = curItem.Text;

            strSavePath = this.CurrentProtocol + ":" + strPath;

            {
                string strOutputPath = "";

                nRet = InternalGetOneRecord(
                    true,
                    strStyle,
                    strPath,
                    "",
                    strParameters,  // 2013/9/22
                    out strRecord,
                    out strXmlFragment,
                    out strOutputPath,
                    out strOutStyle,
                    out baTimestamp,
                    out record,
                    out currrentEncoding,
                    out strError);
                if (string.IsNullOrEmpty(strOutputPath) == false)
                    strSavePath = this.CurrentProtocol + ":" + strOutputPath;
                return nRet;
            }

        }

        #endregion

        // �Ƿ�Ϊ��δ��ʼ���� BiblioInfo
        static bool IsNullBiblioInfo(BiblioInfo info)
        {
            if (string.IsNullOrEmpty(info.OldXml) == true
                && string.IsNullOrEmpty(info.NewXml) == true)
                return true;
            return false;
        }

        // ���һ��MARC/XML��¼
        // parameters:
        //      strPath ��¼·������ʽΪ"����ͼ��/1 @��������"
        //      strDirection    ����Ϊ prev/next/current֮һ��current����ȱʡ��
        //      strOutputPath   [out]���ص�ʵ��·������ʽ��strPath��ͬ��������Э�����Ʋ��֡�
        //      strXmlFragment  ��Ŀ�����XML����Ƭ�ϡ���strStyle����"marc"��ʱ�򣬲��������
        // return:
        //      -1  error ����not found
        //      0   found
        //      1   Ϊ��ϼ�¼
        int InternalGetOneRecord(
            bool bUseLoop,
            string strStyle,
            string strPath,
            string strDirection,
            string strParameters,   // "reload" �����ݿ����»�ȡ
            out string strRecord,
            out string strXmlFragment,
            out string strOutputPath,
            out string strOutStyle,
            out byte[] baTimestamp,
            out DigitalPlatform.Z3950.Record record,
            out Encoding currrentEncoding,
            out string strError)
        {
            strXmlFragment = "";
            strRecord = "";
            strOutputPath = "";
            record = null;
            strError = "";
            currrentEncoding = this.CurrentEncoding;
            baTimestamp = null;
            strOutStyle = "marc";

            if (strStyle != "marc" && strStyle != "xml")
            {
                strError = "dp2SearchFormֻ֧�ֻ�ȡMARC��ʽ��¼��xml��ʽ��¼����֧�� '" + strStyle + "' ��ʽ�ļ�¼";
                return -1;
            }

            bool bReload = StringUtil.IsInList("reload", strParameters);

            string strXml = "";

            // if (this.linkMarcFile != null)
            if (bReload == false)
            {
                BiblioInfo info = null;

                // �洢�������Ŀ��¼ XML
                info = (BiblioInfo)this.m_biblioTable[strPath];
                if (info != null)
                {
                    if (string.IsNullOrEmpty(info.NewXml) == true)
                        strXml = info.OldXml;
                    else
                        strXml = info.NewXml;

                    Debug.Assert(string.IsNullOrEmpty(strXml) == false, "");

                    strOutputPath = info.RecPath;
                    baTimestamp = info.Timestamp;
                    goto SKIP0;
                }
            }

            // ������¼·��
            string strServerName = "";
            string strPurePath = "";
            ParseRecPath(strPath,
                out strServerName,
                out strPurePath);

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            } 
            string strServerUrl = server.Url;

            Stop temp_stop = this.stop;
            LibraryChannel channel = null;

            bool bUseNewChannel = false;
            if (m_bInSearching == true)
            {
                channel = this.Channels.NewChannel(strServerUrl);
                bUseNewChannel = true;

                temp_stop = new Stop();
                temp_stop.Register(MainForm.stopManager, true);	// ����������
            }
            else
            {
                this.Channel = this.Channels.GetChannel(strServerUrl);
                channel = this.Channel;
            }

            if (bUseLoop == true)
            {
                temp_stop.OnStop += new StopEventHandler(this.DoStop);
                temp_stop.Initial("���ڳ�ʼ���������� ...");
                temp_stop.BeginLoop();

                this.Update();
                this.MainForm.Update();
            }

            try
            {
                temp_stop.SetMessage("����װ����Ŀ��¼ " + strPath + " ...");

                string[] formats = null;
                formats = new string[2];
                formats[0] = "xml";
                formats[1] = "outputpath";  // ���ʵ��·��

                string[] results = null;
                //                 byte[] baTimestamp = null;

                Debug.Assert(string.IsNullOrEmpty(strPurePath) == false, "");

                string strCmd = strPurePath;
                if (String.IsNullOrEmpty(strDirection) == false)
                    strCmd += "$" + strDirection;

                long lRet = channel.GetBiblioInfos(
                    temp_stop,
                    strCmd,
                    "",
                    formats,
                    out results,
                    out baTimestamp,
                    out strError);
                if (lRet == 0)
                {
                    if (String.IsNullOrEmpty(strDirection) == true
                        || strDirection == "current")
                        strError = "·��Ϊ '" + strPath + "' ����Ŀ��¼û���ҵ� ...";
                    else
                    {
                        string strText = strDirection;
                        if (strDirection == "prev")
                            strText = "ǰһ��";
                        else if (strDirection == "next")
                            strText = "��һ��";
                        strError = "·��Ϊ '" + strPath + "' ��"+strText+"��Ŀ��¼û���ҵ� ...";
                    }

                    goto ERROR1;   // not found
                }

                if (lRet == -1)
                    goto ERROR1;

                // this.BiblioTimestamp = baTimestamp;

                if (results == null)
                {
                    strError = "results == null";
                    goto ERROR1;
                }
                if (results.Length != formats.Length)
                {
                    strError = "result.Length != formats.Length";
                    goto ERROR1;
                }

                strXml = results[0];
                strOutputPath = results[1] + "@" + strServerName;
                Debug.Assert(string.IsNullOrEmpty(strXml) == false, "");
            }
            finally
            {
                if (bUseLoop == true)
                {
                    temp_stop.EndLoop();
                    temp_stop.OnStop -= new StopEventHandler(this.DoStop);
                    temp_stop.Initial("");
                }

                if (bUseNewChannel == true)
                {
                    this.Channels.RemoveChannel(channel);
                    channel = null;

                    temp_stop.Unregister();	// ����������
                    temp_stop = null;
                }
            }

        SKIP0:
            if (strStyle == "marc")
            {

                string strMarcSyntax = "";
                string strOutMarcSyntax = "";
                // �����ݼ�¼�л��MARC��ʽ
                int nRet = MarcUtil.Xml2Marc(strXml,
                    true,
                    strMarcSyntax,
                    out strOutMarcSyntax,
                    out strRecord,
                    out strError);
                if (nRet == -1)
                {
                    strError = "XMLת����MARC��¼ʱ����: " + strError;
                    goto ERROR1;
                }

                Debug.Assert(string.IsNullOrEmpty(strRecord) == false, "");

                record = new DigitalPlatform.Z3950.Record();
                if (strOutMarcSyntax == "unimarc" || strOutMarcSyntax == "")
                    record.m_strSyntaxOID = "1.2.840.10003.5.1";
                else if (strOutMarcSyntax == "usmarc")
                    record.m_strSyntaxOID = "1.2.840.10003.5.10";
                else if (strOutMarcSyntax == "dc")
                    record.m_strSyntaxOID = "?";
                else
                {
                    strError = "δ֪��MARC syntax '" + strOutMarcSyntax + "'";
                    goto ERROR1;
                }

                // �����Ŀ���������XMLƬ��
                nRet = GetXmlFragment(strXml,
        out strXmlFragment,
        out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            else
            {
                strRecord = strXml;
                strOutStyle = strStyle;

                record = new DigitalPlatform.Z3950.Record();
                record.m_strSyntaxOID = "1.2.840.10003.5.109.10";
            }

            return 0;
        ERROR1:
            return -1;
        }

        // �����Ŀ���������XMLƬ��
        public static int GetXmlFragment(string strXml,
            out string strXmlFragment,
            out string strError)
        {
            strXmlFragment = "";
            strError = "";

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

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);
            nsmgr.AddNamespace("unimarc", DpNs.unimarcxml);
            nsmgr.AddNamespace("usmarc", Ns.usmarcxml);

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//unimarc:leader | //unimarc:controlfield | //unimarc:datafield | //usmarc:leader | //usmarc:controlfield | //usmarc:datafield", nsmgr);
            foreach (XmlNode node in nodes)
            {
                node.ParentNode.RemoveChild(node);
            }

            strXmlFragment = dom.DocumentElement.InnerXml;

            return 0;
        }


        private void listView_browse_DoubleClick(object sender, EventArgs e)
        {
            int nIndex = -1;
            if (this.listView_browse.SelectedIndices.Count > 0)
                nIndex = this.listView_browse.SelectedIndices[0];
            else
            {
                if (this.listView_browse.FocusedItem == null)
                    return;
                nIndex = this.listView_browse.Items.IndexOf(this.listView_browse.FocusedItem);
            }

            LoadDetail(nIndex,
                LoadToExistDetailWindow == true? false : true);
        }

        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetOneRecordSyntax(int index,
            bool bUseNewChannel,
            out string strSyntax,
            out string strError)
        {
            strError = "";
            strSyntax = "";
            int nRet = 0;

            if (index >= this.listView_browse.Items.Count)
            {
                // ������������жϹ���������Դ�����������
                strError = "Խ�������β��";
                return -1;
            }

            if (this._linkMarcFile != null)
            {
                if (_linkMarcFile.MarcSyntax == "<�Զ�>"
                    || _linkMarcFile.MarcSyntax.ToLower() == "<auto>")
                {
                    // 
                }

                strSyntax = this._linkMarcFile.MarcSyntax;
                if (string.IsNullOrEmpty(strSyntax) == false)
                    return 1;
                else
                    return 0;
            }

            ListViewItem curItem = this.listView_browse.Items[index];

            string strPath = curItem.Text;
            // ������¼·��
            string strServerName = "";
            string strPurePath = "";
            ParseRecPath(strPath,
                out strServerName,
                out strPurePath);

            if (string.Compare(strServerName, "mem", true) == 0 
                || string.Compare(strServerName, "file", true) == 0)
            {
                // �� hashtable ��̽�� MARC ��ʽ
                BiblioInfo info = null;

                // �洢�������Ŀ��¼ XML
                info = (BiblioInfo)this.m_biblioTable[strPath];
                if (info == null)
                {
                    strError = "·���� '"+strPath+"' �ļ�¼��Ϣ�� m_biblioTable ��û���ҵ�";
                    return -1;
                }

                string strXml = "";
                if (string.IsNullOrEmpty(info.NewXml) == true)
                    strXml = info.OldXml;
                else
                    strXml = info.NewXml;

                string strMARC = "";
                // ��XML��ʽת��ΪMARC��ʽ
                // �Զ������ݼ�¼�л��MARC�﷨
                nRet = MarcUtil.Xml2Marc(strXml,
                    true,
                    null,
                    out strSyntax,
                    out strMARC,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (string.IsNullOrEmpty(strSyntax) == false)
                    return 1;

                return 0;
            }

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            }

            string strServerUrl = server.Url;

            string strBiblioDbName = GetDbName(strPurePath);

            // ���һ�����ݿ������syntax
            // parameters:
            //      stop    ���!=null����ʾʹ�����stop�����Ѿ�OnStop +=
            //              ���==null����ʾ���Զ�ʹ��this.stop�����Զ�OnStop+=
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = GetDbSyntax(
                null,
                bUseNewChannel,
                strServerName,
                strServerUrl,
                strBiblioDbName,
                out strSyntax,
                out strError);

            if (nRet == -1)
                return -1;

            return nRet;
        }

        // �����������������ϸ��
        bool HasTopDetailForm(int index)
        {
            // ȡ����¼·����������Ŀ������Ȼ�������Ŀ���syntax
            // ����װ��MARC��DC���ֲ�ͬ�Ĵ���
            string strError = "";

#if NO
            // ��ֹ����
            if (m_bInSearching == true)
            {
                strError = "��ǰ�������ڱ�һ��δ�����ĳ�����ʹ�ã��޷�װ�ؼ�¼�����Ժ����ԡ�";
                goto ERROR1;
            }
#endif

            string strSyntax = "";
            int nRet = GetOneRecordSyntax(index,
                this.m_bInSearching,
                out strSyntax,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (strSyntax == "" // default = unimarc
                || strSyntax.ToLower() == "unimarc"
                || strSyntax.ToLower() == "usmarc")
            {
                if (this.MainForm.TopMarcDetailForm != null)
                    return true;
            }
            else if (strSyntax.ToLower() == "dc")
            {
                if (this.MainForm.TopDcForm != null)
                    return true;
            }
            else
            {
                strError = "δ֪��syntax '" + strSyntax + "'";
                goto ERROR1;
            }

            return false;
        ERROR1:
            // MessageBox.Show(this, strError);
        return false;
        }

        // parameters:
        //      bOpendNew   �Ƿ���µ���ϸ��
        void LoadDetail(int index,
            bool bOpenNew = true)
        {
            // ȡ����¼·����������Ŀ������Ȼ�������Ŀ���syntax
            // ����װ��MARC��DC���ֲ�ͬ�Ĵ���
            string strError = "";

#if NO
            // ��ֹ����
            if (m_bInSearching == true)
            {
                strError = "��ǰ�������ڱ�һ��δ�����ĳ�����ʹ�ã��޷�װ�ؼ�¼�����Ժ����ԡ�";
                goto ERROR1;
            }
#endif

            string strSyntax = "";
            int nRet = GetOneRecordSyntax(index,
                this.m_bInSearching,
                out strSyntax,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (strSyntax == "" // default = unimarc
                || strSyntax.ToLower() == "unimarc"
                || strSyntax.ToLower() == "usmarc")
            {

                MarcDetailForm form = null;

                if (bOpenNew == false)
                    form = this.MainForm.TopMarcDetailForm;

                if (form == null)
                {
                    form = new MarcDetailForm();

                    form.MdiParent = this.MainForm;
                    form.MainForm = this.MainForm;

                    form.Show();
                }
                else
                    form.Activate();


                // MARC Syntax OID
                // ��Ҫ�������ݿ����ò��������еõ�MARC��ʽ
                ////form.AutoDetectedMarcSyntaxOID = "1.2.840.10003.5.1";   // UNIMARC

                form.LoadRecord(this, index);
            }
            else if (strSyntax.ToLower() == "dc")
            {

                DcForm form = null;

                if (bOpenNew == false)
                    form = this.MainForm.TopDcForm;

                if (form == null)
                {
                    form = new DcForm();

                    form.MdiParent = this.MainForm;
                    form.MainForm = this.MainForm;

                    form.Show();
                }
                else
                    form.Activate();

                form.LoadRecord(this, index);
            }
            else
            {
                strError = "δ֪��syntax '" + strSyntax + "'";
                goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        /*
        // �����Ŀ��¼��XML��ʽ
        // parameters:
        //      strMarcSyntax Ҫ������XML��¼��marcsyntax��
        public static int GetBiblioXml(
            string strMarcSyntax,
            string strMARC,
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

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
            else // ����
            {
                writer.MarcNameSpaceUri = DpNs.unimarcxml;
                writer.MarcPrefix = "unimarc";
            }

            // string strMARC = this.MarcEditor.Marc;
            string strDebug = strMARC.Replace((char)Record.FLDEND, '#');
            int nRet = writer.WriteRecord(strMARC,
                out strError);
            if (nRet == -1)
                return -1;

            writer.Flush();
            s.Flush();

            s.Seek(0, SeekOrigin.Begin);

            XmlDocument domMarc = new XmlDocument();
            try
            {
                domMarc.Load(s);
            }
            catch (Exception ex)
            {
                strError = "XML����װ��DOMʱ����: " + ex.Message;
                return -1;
            }
            finally
            {
                //File.Delete(strTempFileName);
                s.Close();
            }

            strXml = domMarc.OuterXml;
            return 0;
        }
         * */

        // ���һ�����ݿ������syntax
        // parameters:
        //      stop    ���!=null����ʾʹ�����stop�����Ѿ�OnStop +=
        //              ���==null����ʾ���Զ�ʹ��this.stop�����Զ�OnStop+=
        //      bUseNewChannel  �Ƿ�ʹ���µ�Channel�������==false����ʾ����ʹ����ǰ��
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetDbSyntax(
            Stop stop,
            bool bUseNewChannel,
            string strServerName,
            string strServerUrl,
            string strDbName,
            out string strSyntax,
            out string strError)
        {
            strSyntax = "";
            strError = "";

            bool bInitialStop = false;
            if (stop == null)
            {
                if (bUseNewChannel == true)
                {
                    stop = new Stop();
                    stop.Register(MainForm.stopManager, true);	// ����������
                }
                else
                    stop = this.stop;

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڻ�÷����� "+strServerUrl+" ����Ϣ ...");
                stop.BeginLoop();

                bInitialStop = true;
            }
            
            dp2ServerInfo info = null;

            try
            {
                info = this.MainForm.ServerInfos.GetServerInfo(stop,
                    bUseNewChannel,
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

                    if (bUseNewChannel == true)
                    {
                        stop.Unregister();	// ����������
                        stop = null;
                    }
                }
            }

            for (int i = 0; i < info.BiblioDbProperties.Count; i++)
            {
                BiblioDbProperty prop = info.BiblioDbProperties[i];
                if (prop.DbName == strDbName)
                {
                    strSyntax = prop.Syntax;
                    return 1;
                }
            }

            return 0;   // not found dbname
        }


        // ���publisher��ʵ�ÿ�Ŀ���
        public int GetUtilDbName(
            Stop stop,
            string strServerName,
            string strServerUrl,
            string strFuncName, // "publisher"
            out string strUtilDbName,
            out string strError)
        {
            strUtilDbName = "";
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
                    this.m_bInSearching,
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

            for (int i = 0; i < info.UtilDbProperties.Count; i++)
            {
                UtilDbProperty prop = info.UtilDbProperties[i];
                if (prop.Type == "publisher")
                {
                    strUtilDbName = prop.DbName;
                    return 1;
                }

            }

            return 0;    // not found
        }


        // ��ó����������Ϣ
        public int GetPublisherInfo(
            string strServerName,
            string strPublisherNumber,
            out string str210,
            out string strError)
        {
            strError = "";
            str210 = "";

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            } 
            string strServerUrl = server.Url;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ó�������Ϣ ...");
            stop.BeginLoop();

            try
            {
                string strDbName = "";

                // ���publisher��ʵ�ÿ�Ŀ���
                int nRet = GetUtilDbName(
                    stop,
                    strServerName,
                    strServerUrl,
                    "publisher",
                    out strDbName,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (String.IsNullOrEmpty(strDbName) == true)
                {
                    strError = "��δ����publisher���͵�ʵ�ÿ���";
                    return -1;
                }

                this.Channel = this.Channels.GetChannel(strServerUrl);

                string strAction = "";

                long lRet = Channel.GetUtilInfo(
                    stop,
                    strAction,
                    strDbName,
                    "ISBN",
                    strPublisherNumber,
                    "v210",
                    out str210,
                    out strError);
                if (lRet == -1)
                    return -1;

                if (lRet == 0)
                    return 0;

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }


            return 1;
        }

        // ���ó����������Ϣ
        public int SetPublisherInfo(
            string strServerName,
            string strPublisherNumber,
            string str210,
            out string strError)
        {
            strError = "";

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            } 
            string strServerUrl = server.Url;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�������ó�������Ϣ ...");
            stop.BeginLoop();

            try
            {

                string strDbName = "";

                // ���publisher��ʵ�ÿ�Ŀ���
                int nRet = GetUtilDbName(
                    stop,
                    strServerName,
                    strServerUrl,
                    "publisher",
                    out strDbName,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (String.IsNullOrEmpty(strDbName) == true)
                {
                    strError = "��δ����publisher���͵�ʵ�ÿ���";
                    return -1;
                }

                this.Channel = this.Channels.GetChannel(strServerUrl);

                string strAction = "";

                long lRet = Channel.SetUtilInfo(
                    stop,
                    strAction,
                    strDbName,
                    "ISBN",
                    "r",
                    "i",
                    "v210",
                    strPublisherNumber,
                    str210,
                    out strError);
                if (lRet == -1)
                    return -1;

                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

        }

        // ���102�����Ϣ
        public int Get102Info(
            string strServerName,
            string strPublisherNumber,
            out string str102,
            out string strError)
        {
            strError = "";
            str102 = "";

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            } 
            string strServerUrl = server.Url;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ��102��Ϣ ...");
            stop.BeginLoop();

            try
            {
                string strDbName = "";

                // ���publisher��ʵ�ÿ�Ŀ���
                int nRet = GetUtilDbName(
                    stop,
                    strServerName,
                    strServerUrl,
                    "publisher",
                    out strDbName,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (String.IsNullOrEmpty(strDbName) == true)
                {
                    strError = "��δ����publisher���͵�ʵ�ÿ���";
                    return -1;
                }

                this.Channel = this.Channels.GetChannel(strServerUrl);


                string strAction = "";

                long lRet = Channel.GetUtilInfo(
                    stop,
                    strAction,
                    strDbName,
                    "ISBN",
                    strPublisherNumber,
                    "v102",
                    out str102,
                    out strError);
                if (lRet == -1)
                    return -1;

                if (lRet == 0)
                    return 0;

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }


            return 1;
        }

        // ����102�����Ϣ
        public int Set102Info(
            string strServerName,
            string strPublisherNumber,
            string str102,
            out string strError)
        {
            strError = "";

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            } 
            string strServerUrl = server.Url;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("��������102��Ϣ ...");
            stop.BeginLoop();

            try
            {
                string strDbName = "";

                // ���publisher��ʵ�ÿ�Ŀ���
                int nRet = GetUtilDbName(
                    stop,
                    strServerName,
                    strServerUrl,
                    "publisher",
                    out strDbName,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (String.IsNullOrEmpty(strDbName) == true)
                {
                    strError = "��δ����publisher���͵�ʵ�ÿ���";
                    return -1;
                }

                this.Channel = this.Channels.GetChannel(strServerUrl);

                string strAction = "";

                long lRet = Channel.SetUtilInfo(
                    stop,
                    strAction,
                    strDbName,
                    "ISBN",
                    "r",
                    "i",
                    "v102",
                    strPublisherNumber,
                    str102,
                    out strError);
                if (lRet == -1)
                    return -1;

                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

        }

        /*
        static string GetDbName(string strPurePath)
        {
            int nRet = 0;

            nRet = strPurePath.IndexOf("/");
            if (nRet != -1)
                return strPurePath.Substring(0, nRet).Trim();

            return strPurePath;
        }*/
        // ��·����ȡ����������
        // parammeters:
        //      strPath ·��������"����ͼ��/3"
        public static string GetDbName(string strPurePath)
        {
            int nRet = strPurePath.LastIndexOf("/");
            if (nRet == -1)
                return strPurePath;

            return strPurePath.Substring(0, nRet).Trim();
        }

        // ��·����ȡ����¼�Ų���
        // parammeters:
        //      strPath ·��������"����ͼ��/3"
        static string GetRecordID(string strPath)
        {
            int nRet = strPath.LastIndexOf("/");
            if (nRet == -1)
                return "";

            return strPath.Substring(nRet + 1).Trim();
        }

        // ��¼·���Ƿ�Ϊ׷���ͣ�
        // ��ν׷���ͣ����Ǽ�¼ID����Ϊ'?'������û�м�¼ID����
        public static bool IsAppendRecPath(string strPath)
        {
            if (String.IsNullOrEmpty(strPath) == true)
                return true;

            string strRecordID = GetRecordID(strPath);
            if (String.IsNullOrEmpty(strRecordID) == true
                || strRecordID == "?")
                return true;

            return false;
        }

        public int GetChannelRights(
            string strServerName,
            out string strRights,               
            out string strError)
        {
            strError = "";
            strRights = "";

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            }
            string strServerUrl = server.Url;

            this.Channel = this.Channels.GetChannel(strServerUrl);
            if (string.IsNullOrEmpty(this.Channel.Rights) == true)
            {
                string strValue = "";
                long lRet = this.Channel.GetSystemParameter(stop,
                    "biblio",
                    "dbnames",
                    out strValue,
                    out strError);
                if (lRet == -1)
                    return -1;
            }

            strRights = this.Channel.Rights;

            return 0;
        }


        public int ForceLogin(
    Stop stop,
    string strServerName,
    out string strError)
        {

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            }
            string strServerUrl = server.Url;

            this.Channel = this.Channels.GetChannel(strServerUrl);
            string strValue = "";
            long lRet = this.Channel.GetSystemParameter(stop,
                "biblio",
                "dbnames",
                out strValue,
                out strError);
            if (lRet == -1)
                return -1;

            return 0;
        }

        // ���һ�����ݿ������syntax
        // parameters:
        //      stop    ���!=null����ʾʹ�����stop�����Ѿ�OnStop +=
        //              ���==null����ʾ���Զ�ʹ��this.stop�����Զ�OnStop+=
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int GetDbSyntax(
            Stop stop,
            string strServerName,
            string strDbName,
            out string strSyntax,
            out string strError)
        {
            strSyntax = "";

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            }
            string strServerUrl = server.Url;

            // ���һ�����ݿ������syntax
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            int nRet = GetDbSyntax(stop,
                this.m_bInSearching,
                strServerName,
                strServerUrl,
                strDbName,
                out strSyntax,
                out strError);
            if (nRet == -1)
                return -1;

            if (String.IsNullOrEmpty(strSyntax) == true)
                strSyntax = "unimarc";

            return 1;
        }

        // �����¼
        // parameters:
        //      strPath ��ʽΪ"����ͼ��/7@���ط�����"
        //      strOriginSyntax Ҫ���� MARC ��¼��ԭʼ MARC ��ʽ�����Ϊ�գ���ʾ�ں����в��˶Լ�¼����Ŀ��� MARC ��ʽ�Ƿ�һ��
        //      strOutputPath   ��ʽΪ"����ͼ��/7@���ط�����"
        //      strXmlFragment  ��Ŀ���������XMLƬ�ϡ�ע�⣬û�и�Ԫ��
        // return:
        //      -2  timestamp mismatch
        //      -1  error
        //      0   succeed
        public int SaveMarcRecord(
            bool bUseLoop,
            string strPath,
            string strMARC,
            string strOriginSyntax,
            byte[] baTimestamp,
            string strXmlFragment,  // ��Ŀ���������XMLƬ��
            string strComment,
            out string strOutputPath,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            strError = "";
            baOutputTimestamp = null;
            strOutputPath = "";

            int nRet = 0;

#if NO
            // ��ֹ����
            if (m_bInSearching == true)
            {
                strError = "��ǰ�������ڱ�һ��δ�����ĳ�����ʹ�ã��޷���ü�¼�����Ժ����ԡ�";
                return -1;
            }
#endif

            // ��strPath����Ϊserver url��local path��������
            string strServerName = "";
            string strPurePath = "";
            ParseRecPath(strPath,
                out strServerName,
                out strPurePath);

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            }

            string strServerUrl = server.Url;

            Stop temp_stop = this.stop;
            LibraryChannel channel = null;

            bool bUseNewChannel = false;
            if (m_bInSearching == true)
            {
                channel = this.Channels.NewChannel(strServerUrl);
                bUseNewChannel = true;
                temp_stop = new Stop();
                temp_stop.Register(MainForm.stopManager, true);	// ����������
            }
            else
            {
                this.Channel = this.Channels.GetChannel(strServerUrl);
                channel = this.Channel;
            }

            if (bUseLoop == true)
            {
                temp_stop.OnStop += new StopEventHandler(this.DoStop);
                temp_stop.Initial("���ڳ�ʼ���������� ...");
                temp_stop.BeginLoop();

                this.Update();
                this.MainForm.Update();
            }


            try
            {
                string strDbName = GetDbName(strPurePath);
                string strSyntax = "";

                // ���һ�����ݿ������syntax
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = GetDbSyntax(this.stop,
                    bUseNewChannel,
                    strServerName,
                    strServerUrl,
                    strDbName,
                    out strSyntax,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (String.IsNullOrEmpty(strSyntax) == true)
                    strSyntax = "unimarc";

                // �˶� MARC ��ʽ
                if (string.IsNullOrEmpty(strOriginSyntax) == false)
                {
                    if (strOriginSyntax != strSyntax)
                    {
                        strError = "�Ᵽ��ļ�¼�� MARC ��ʽΪ '"+strOriginSyntax+"'����Ŀ����Ŀ�� '"+strDbName+"' �� MARC ��ʽ '"+strSyntax+"' �����ϣ��޷����� ";
                        goto ERROR1;
                    }
                }
/*
                nRet = MarcUtil.Marc2Xml(
    strMARC,
    strSyntax,
    out strXml,
    out strError);

                if (nRet == -1)
                    goto ERROR1;
 * */
                XmlDocument domMarc = null;
                nRet = MarcUtil.Marc2Xml(strMARC,
                    strSyntax,
                    out domMarc,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                Debug.Assert(domMarc != null, "");

                // �ϳ�����XMLƬ��
                if (string.IsNullOrEmpty(strXmlFragment) == false)
                {
                    XmlDocumentFragment fragment = domMarc.CreateDocumentFragment();
                    try
                    {
                        fragment.InnerXml = strXmlFragment;
                    }
                    catch (Exception ex)
                    {
                        strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                        return -1;
                    }

                    domMarc.DocumentElement.AppendChild(fragment);
                }

                string strXml = domMarc.OuterXml;

                string strAction = "change";

                if (IsAppendRecPath(strPurePath) == true)
                    strAction = "new";

                temp_stop.SetMessage("���ڱ�����Ŀ��¼ " + strPath + " ...");

                string strOutputBiblioRecPath = "";

                long lRet = channel.SetBiblioInfo(
                    temp_stop,
                    strAction,
                    strPurePath,
                    "xml",
                    strXml,
                    baTimestamp,
                    strComment,
                    out strOutputBiblioRecPath,
                    out baOutputTimestamp,
                    out strError);

                // �������Ҳ�п����Ѿ�������·�� 2013/6/17
                if (string.IsNullOrEmpty(strOutputBiblioRecPath) == false)
                    strOutputPath = strOutputBiblioRecPath + "@" + strServerName;

                if (lRet == -1)
                {
                    if (channel.ErrorCode == ErrorCode.TimestampMismatch)
                        return -2;   // timestamp mismatch
                    goto ERROR1;
                }

                // this.BiblioTimestamp = baTimestamp;


            }
            finally
            {
                temp_stop.Initial("");

                if (bUseLoop == true)
                {
                    temp_stop.EndLoop();
                    temp_stop.OnStop -= new StopEventHandler(this.DoStop);
                }

                if (bUseNewChannel == true)
                {
                    this.Channels.RemoveChannel(channel);
                    channel = null;

                    temp_stop.Unregister();	// ����������
                    temp_stop = null;
                }
            }
            return 0;
        ERROR1:
            return -1;
        }


        // �����¼
        // parameters:
        //      strPath ��ʽΪ"����ͼ��/7@���ط�����"
        //      strOutputPath   ��ʽΪ"����ͼ��/7@���ط�����"
        public int SaveXmlRecord(
            string strPath,
            string strXml,
            byte[] baTimestamp,
            out string strOutputPath,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            strError = "";
            baOutputTimestamp = null;
            strOutputPath = "";

            // int nRet = 0;

            // ��strPath����Ϊserver url��local path��������
            string strServerName = "";
            string strPurePath = "";
            ParseRecPath(strPath,
                out strServerName,
                out strPurePath);

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            } 
            string strServerUrl = server.Url;

            this.Channel = this.Channels.GetChannel(strServerUrl);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڳ�ʼ���������� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                string strAction = "change";

                if (IsAppendRecPath(strPath) == true)
                    strAction = "new";


                stop.SetMessage("���ڱ�����Ŀ��¼ " + strPath + " ...");

                string strOutputBiblioRecPath = "";

                long lRet = Channel.SetBiblioInfo(
                    stop,
                    strAction,
                    strPurePath,
                    "xml",
                    strXml,
                    baTimestamp,
                    "",
                    out strOutputBiblioRecPath,
                    out baOutputTimestamp,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                strOutputPath = strOutputBiblioRecPath + "@" + strServerName;
                // this.BiblioTimestamp = baTimestamp;


            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }
            return 0;
        ERROR1:
            return -1;
        }

        // ��װ��İ汾
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int GetCfgFile(
            string strPath,
            out string strContent,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            return GetCfgFile(this.m_bInSearching,  // false,
                strPath,
                out strContent,
                out baOutputTimestamp,
                out strError);
        }


        // ��������ļ�
        // ���õ���CfgCache��
        // parameters:
        //      bNewChannel �Ƿ�Ҫʹ���´�����Channel?
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int GetCfgFile(
            bool bNewChannel,
            string strPath,
            out string strContent,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            strError = "";
            baOutputTimestamp = null;
            strContent = "";

            // ��strPath����Ϊserver url��local path��������
            string strServerName = "";
            string strPurePath = "";
            ParseRecPath(strPath,
                out strServerName,
                out strPurePath);

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            } 
            string strServerUrl = server.Url;

            Stop temp_stop = this.stop;
            LibraryChannel channel = null;

            if (bNewChannel == false)
            {
                this.Channel = this.Channels.GetChannel(strServerUrl);
                channel = this.Channel;
            }
            else
            {
                channel = this.Channels.NewChannel(strServerUrl);
                temp_stop = new Stop();
                temp_stop.Register(MainForm.stopManager, true);	// ����������
            }


            temp_stop.OnStop += new StopEventHandler(this.DoStop);
            temp_stop.Initial("�������������ļ� ...");
            temp_stop.BeginLoop();

            try
            {
                // string strPath = strBiblioDbName + "/cfgs/" + strCfgFileName;

                temp_stop.SetMessage("�������������ļ� " + strPurePath + " ...");
                string strMetaData = "";
                string strOutputPath = "";

                string strStyle = "content,data,metadata,timestamp,outputpath";

                long lRet = channel.GetRes(temp_stop,
                    MainForm.cfgCache,
                    strPurePath,
                    strStyle,
                    null,
                    out strContent,
                    out strMetaData,
                    out baOutputTimestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    // 2011/6/21
                    if (channel.ErrorCode == ErrorCode.NotFound)
                        return 0;
                    goto ERROR1;
                }

            }
            finally
            {
                temp_stop.EndLoop();
                temp_stop.OnStop -= new StopEventHandler(this.DoStop);
                temp_stop.Initial("");

                if (bNewChannel == true)
                {
                    this.Channels.RemoveChannel(channel);
                    channel = null;

                    temp_stop.Unregister();	// ����������
                    temp_stop = null;
                }
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ���������ļ�
        public int SaveCfgFile(
            string strPath,
            /*
             * string strBiblioDbName,
            string strCfgFileName,
             * */
            string strContent,
            byte[] baTimestamp,
            out string strError)
        {
            strError = "";

            // ��strPath����Ϊserver url��local path��������
            string strServerName = "";
            string strPurePath = "";
            ParseRecPath(strPath,
                out strServerName,
                out strPurePath);

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                return -1;
            } 
            string strServerUrl = server.Url;

            this.Channel = this.Channels.GetChannel(strServerUrl);



            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ��������ļ� ...");
            stop.BeginLoop();

            try
            {
                // string strPath = strBiblioDbName + "/cfgs/" + strCfgFileName;

                stop.SetMessage("���ڱ��������ļ� " + strPurePath + " ...");

                byte[] output_timestamp = null;
                string strOutputPath = "";

                long lRet = Channel.WriteRes(
                    stop,
                    strPurePath,
                    strContent,
                    true,
                    "",	// style
                    baTimestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

        private void listView_browse_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;

            ColumnSortStyle sortStyle = ColumnSortStyle.LeftAlign;

            // ��һ��Ϊ��¼·��������������
            if (nClickColumn == 0)
                sortStyle = ColumnSortStyle.LongRecPath;

            this.SortColumns.SetFirstColumn(nClickColumn,
                sortStyle,
                this.listView_browse.Columns,
                true);

            // ����
            this.listView_browse.ListViewItemSorter = new SortColumnsComparer(this.SortColumns);

            this.listView_browse.ListViewItemSorter = null;
        }

        // ������̵�����
        protected override bool ProcessDialogKey(
            Keys keyData)
        {
            // �س�
            if (keyData == Keys.Enter)
            {
                bool bClear = true; // �Ƿ��������������е�����

                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    bClear = false;

                ClearListViewPropertyCache();
                if (bClear == true)
                {
                    if (this.m_nChangedCount > 0)
                    {
                        DialogResult result = MessageBox.Show(this,
                            "��ǰ���м�¼�б����� " + this.m_nChangedCount.ToString() + " ���޸���δ���档\r\n\r\n�Ƿ��������?\r\n\r\n(Yes �����Ȼ�����������No ��������)",
                            "dp2SearchForm",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result == DialogResult.No)
                            return true;
                    }
                    this.ClearListViewItems();

                    ListViewUtil.ClearSortColumns(this.listView_browse);
                }

                this._linkMarcFile = null;


                if (this.textBox_simple_queryWord.Focused == true
                    || this.textBox_mutiline_queryContent.Focused == true)
                {
                    // ����������س�
                    this.DoSearch();
                }
                else if (this.tabControl_query.SelectedTab == this.tabPage_logic
                    && this.dp2QueryControl1.Focused == true)
                {
                    this.DoLogicSearch();
                }
                else if (this.listView_browse.Focused == true)
                {
                    // ������лس�
                    listView_browse_DoubleClick(this, null);
                }

                return true;
            }

            return false;
        }

        private void dp2SearchForm_Activated(object sender, EventArgs e)
        {
            if (stop != null)
                MainForm.stopManager.Active(this.stop);

            MainForm.SetMenuItemState();

            // �˵�
            if (this.listView_browse.SelectedItems.Count == 0)
            {
                MainForm.MenuItem_saveOriginRecordToIso2709.Enabled = false;
                MainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = false;
            }
            else
            {
                MainForm.MenuItem_saveOriginRecordToIso2709.Enabled = true;
                MainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = true;
            }

            MainForm.MenuItem_font.Enabled = false;

            // ��������ť
            if (this.listView_browse.SelectedItems.Count == 0)
            {
                MainForm.toolButton_saveTo.Enabled = false;
                MainForm.toolButton_delete.Enabled = false;
            }
            else
            {
                MainForm.toolButton_saveTo.Enabled = true;
                MainForm.toolButton_delete.Enabled = true;
            }

            MainForm.toolButton_refresh.Enabled = true;
            MainForm.toolButton_loadFullRecord.Enabled = false;
        }

        private void listView_browse_SelectedIndexChanged(object sender, EventArgs e)
        {
#if NO
            // �˵���̬�仯
            if (this.listView_browse.SelectedItems.Count == 0)
            {
                MainForm.toolButton_saveTo.Enabled = false;
                MainForm.toolButton_delete.Enabled = false;

                MainForm.MenuItem_saveOriginRecordToIso2709.Enabled = false;
                MainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = false;

                MainForm.StatusBarMessage = "";
            }
            else
            {
                MainForm.toolButton_saveTo.Enabled = true;
                MainForm.toolButton_delete.Enabled = true;

                MainForm.MenuItem_saveOriginRecordToIso2709.Enabled = true;
                MainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = true;

                if (this.listView_browse.SelectedItems.Count == 1)
                {
                    MainForm.StatusBarMessage = "�� " + (this.listView_browse.SelectedIndices[0] + 1).ToString() + " ��";
                }
                else
                {
                    MainForm.StatusBarMessage = "�� " + (this.listView_browse.SelectedIndices[0] + 1).ToString() + " �п�ʼ����ѡ�� " + this.listView_browse.SelectedItems.Count.ToString() + " ������";
                }
            }

            ListViewUtil.OnSeletedIndexChanged(this.listView_browse,
    0,
    null);
#endif

            this.commander.AddMessage(WM_SELECT_INDEX_CHANGED);

        }

        public void SaveOriginRecordToWorksheet()
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_browse.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ����ļ�¼";
                goto ERROR1;
            }

            // Encoding preferredEncoding = this.CurrentEncoding;

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ����Ĺ������ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = MainForm.LastWorksheetFileName;
            dlg.Filter = "�������ļ� (*.wor)|*.wor|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            /*
            Encoding targetEncoding = null;
            nRet = this.MainForm.GetEncoding(dlg.EncodingName,
                out targetEncoding,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }
             * */

            bool bExist = File.Exists(dlg.FileName);
            bool bAppend = false;

            if (bExist == true)
            {
                DialogResult result = MessageBox.Show(this,
        "�ļ� '" + dlg.FileName + "' �Ѵ��ڣ��Ƿ���׷�ӷ�ʽд���¼?\r\n\r\n--------------------\r\nע��(��)׷��  (��)����  (ȡ��)����",
        "dp2SearchForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                    bAppend = true;

                if (result == DialogResult.No)
                    bAppend = false;

                if (result == DialogResult.Cancel)
                {
                    strError = "��������...";
                    goto ERROR1;
                }
            }

            MainForm.LastWorksheetFileName = dlg.FileName;

            StreamWriter sw = null;

            try
            {
                // �����ļ�
                sw = new StreamWriter(MainForm.LastWorksheetFileName,
                    bAppend,	// append
                    System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                strError = "�򿪻򴴽��ļ� " + MainForm.LastWorksheetFileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }

            this.EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ��浽��������ʽ ...");
            stop.BeginLoop();

            try
            {
                stop.SetProgressRange(0, this.listView_browse.SelectedItems.Count);

                for (int i = 0; i < this.listView_browse.SelectedItems.Count; i++)
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

                    string strPath = this.listView_browse.SelectedItems[i].Text;

                    // byte[] baTarget = null;

                    string strRecord = "";
                    string strOutputPath = "";
                    string strOutStyle = "";
                    byte[] baTimestamp = null;
                    DigitalPlatform.Z3950.Record record = null;
                    Encoding currrentEncoding;
                    string strXmlFragment = "";

                    // ���һ��MARC/XML��¼
                    // parameters:
                    //      strPath ��¼·������ʽΪ"����ͼ��/1 @��������"
                    //      strDirection    ����Ϊ prev/next/current֮һ��current����ȱʡ��
                    //      strOutputPath   [out]���ص�ʵ��·������ʽ��strPath��ͬ��
                    // return:
                    //      -1  error ����not found
                    //      0   found
                    //      1   Ϊ��ϼ�¼
                    nRet = InternalGetOneRecord(
                        false,
                        "marc",
                        strPath,
                        "current",
                        "",
                        out strRecord,
                        out strXmlFragment,
                        out strOutputPath,
                        out strOutStyle,
                        out baTimestamp,
                        out record,
                        out currrentEncoding,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    string strMarcSyntax = "";
                    if (record.m_strSyntaxOID == "1.2.840.10003.5.1")
                        strMarcSyntax = "unimarc";
                    if (record.m_strSyntaxOID == "1.2.840.10003.5.10")
                        strMarcSyntax = "usmarc";

                    Debug.Assert(strMarcSyntax != "", "");

                    List<string> lines = null;
                    // �����ڸ�ʽ�任Ϊ��������ʽ 
                    // return:
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = MarcUtil.CvtJineiToWorksheet(
                        strRecord,
                        -1,
                        out lines,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    foreach(string line in lines)
                    {
                        sw.WriteLine(line);
                    }

                    stop.SetProgressValue(i + 1);
                }


            }
            catch (Exception ex)
            {
                strError = "д���ļ� " + MainForm.LastWorksheetFileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }
            finally
            {
                sw.Close();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControls(true);
            }

            // 
            if (bAppend == true)
                MainForm.MessageText = this.listView_browse.SelectedItems.Count.ToString()
                    + "����¼�ɹ�׷�ӵ��ļ� " + MainForm.LastWorksheetFileName + " β��";
            else
                MainForm.MessageText = this.listView_browse.SelectedItems.Count.ToString()
                    + "����¼�ɹ����浽���ļ� " + MainForm.LastWorksheetFileName + " β��";

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        public void SaveOriginRecordToIso2709()
        {
            string strError = "";
            int nRet = 0;

            bool bControl = Control.ModifierKeys == Keys.Control;

            if (this.listView_browse.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ����ļ�¼";
                goto ERROR1;
            }

            Encoding preferredEncoding = this.CurrentEncoding;

            string strPreferedMarcSyntax = "";

            if (this._linkMarcFile != null)
                strPreferedMarcSyntax = this._linkMarcFile.MarcSyntax;
            else
            {
                // �۲�Ҫ����ĵ�һ����¼��marc syntax
                nRet = GetOneRecordSyntax(0,
                    this.m_bInSearching,
                    out strPreferedMarcSyntax,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

            }

            OpenMarcFileDlg dlg = new OpenMarcFileDlg();
            GuiUtil.SetControlFont(dlg, this.Font);
            dlg.IsOutput = true;
            dlg.FileName = MainForm.LastIso2709FileName;
            dlg.CrLf = MainForm.LastCrLfIso2709;
            dlg.RemoveField998 = MainForm.LastRemoveField998;
            dlg.EncodingListItems = Global.GetEncodingList(true);
            dlg.EncodingName =
                (String.IsNullOrEmpty(MainForm.LastEncodingName) == true ? GetEncodingForm.GetEncodingName(preferredEncoding) : MainForm.LastEncodingName);
            dlg.EncodingComment = "ע: ԭʼ���뷽ʽΪ " + GetEncodingForm.GetEncodingName(preferredEncoding);
            
            if (string.IsNullOrEmpty(strPreferedMarcSyntax) == false)
                dlg.MarcSyntax = strPreferedMarcSyntax;
            else
                dlg.MarcSyntax = "<�Զ�>";    // strPreferedMarcSyntax;
            
            if (bControl == false)
                dlg.EnableMarcSyntax = false;
            dlg.ShowDialog(this);
            if (dlg.DialogResult != DialogResult.OK)
                return;

            Encoding targetEncoding = null;

            if (dlg.EncodingName == "MARC-8"
                && preferredEncoding.Equals(this.MainForm.Marc8Encoding) == false)
            {
                strError = "��������޷����С�ֻ���ڼ�¼��ԭʼ���뷽ʽΪ MARC-8 ʱ������ʹ��������뷽ʽ�����¼��";
                goto ERROR1;
            }

            nRet = this.MainForm.GetEncoding(dlg.EncodingName,
                out targetEncoding,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }

            string strLastFileName = MainForm.LastIso2709FileName;
            string strLastEncodingName = MainForm.LastEncodingName;

            bool bExist = File.Exists(dlg.FileName);
            bool bAppend = false;

            if (bExist == true)
            {
                DialogResult result = MessageBox.Show(this,
        "�ļ� '" + dlg.FileName + "' �Ѵ��ڣ��Ƿ���׷�ӷ�ʽд���¼?\r\n\r\n--------------------\r\nע��(��)׷��  (��)����  (ȡ��)����",
        "dp2SearchForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                    bAppend = true;

                if (result == DialogResult.No)
                    bAppend = false;

                if (result == DialogResult.Cancel)
                {
                    strError = "��������...";
                    goto ERROR1;
                }
            }

            // ���ͬһ���ļ�������ʱ��ı��뷽ʽһ����
            if (strLastFileName == dlg.FileName
                && bAppend == true)
            {
                if (strLastEncodingName != ""
                    && strLastEncodingName != dlg.EncodingName)
                {
                    DialogResult result = MessageBox.Show(this,
                        "�ļ� '" + dlg.FileName + "' ������ǰ�Ѿ��� " + strLastEncodingName + " ���뷽ʽ�洢�˼�¼���������Բ�ͬ�ı��뷽ʽ " + dlg.EncodingName + " ׷�Ӽ�¼�����������ͬһ�ļ��д��ڲ�ͬ���뷽ʽ�ļ�¼�����ܻ������޷�����ȷ��ȡ��\r\n\r\n�Ƿ����? (��)׷��  (��)��������",
                        "dp2SearchForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                    {
                        strError = "��������...";
                        goto ERROR1;
                    }

                }
            }

            MainForm.LastIso2709FileName = dlg.FileName;
            MainForm.LastCrLfIso2709 = dlg.CrLf;
            MainForm.LastEncodingName = dlg.EncodingName;
            MainForm.LastRemoveField998 = dlg.RemoveField998;


            this.EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ��浽MARC�ļ� ...");
            stop.BeginLoop();


            Stream s = null;

            try
            {
                s = File.Open(MainForm.LastIso2709FileName,
                     FileMode.OpenOrCreate);
                if (bAppend == false)
                    s.SetLength(0);
                else
                    s.Seek(0, SeekOrigin.End);
            }
            catch (Exception ex)
            {
                strError = "�򿪻򴴽��ļ� " + MainForm.LastIso2709FileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }

            int nCount = 0;

            try
            {
                stop.SetProgressRange(0, this.listView_browse.SelectedItems.Count);
                bool bAsked = false;
                for (int i = 0; i < this.listView_browse.SelectedItems.Count; i++)
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

                    string strPath = this.listView_browse.SelectedItems[i].Text;

                    byte[] baTarget = null;

                    string strRecord = "";
                    string strOutputPath = "";
                    string strOutStyle = "";
                    byte[] baTimestamp = null;
                    DigitalPlatform.Z3950.Record record = null;
                    Encoding currrentEncoding;
                    string strXmlFragment = "";

                    // ���һ��MARC/XML��¼
                    // parameters:
                    //      strPath ��¼·������ʽΪ"����ͼ��/1 @��������"
                    //      strDirection    ����Ϊ prev/next/current֮һ��current����ȱʡ��
                    //      strOutputPath   [out]���ص�ʵ��·������ʽ��strPath��ͬ��
                    // return:
                    //      -1  error ����not found
                    //      0   found
                    //      1   Ϊ��ϼ�¼
                    nRet = InternalGetOneRecord(
                        false,
                        "marc",
                        strPath,
                        "current",
                        "",
                        out strRecord,
                        out strXmlFragment,
                        out strOutputPath,
                        out strOutStyle,
                        out baTimestamp,
                        out record,
                        out currrentEncoding,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    string strMarcSyntax = "";

                    if (dlg.MarcSyntax == "<�Զ�>")
                    {
                        if (record.m_strSyntaxOID == "1.2.840.10003.5.1")
                            strMarcSyntax = "unimarc";
                        if (record.m_strSyntaxOID == "1.2.840.10003.5.10")
                            strMarcSyntax = "usmarc";

                        if (strMarcSyntax == "unimarc" && dlg.Mode880 == true
                            && bAsked == false)
                        {
                            DialogResult result = MessageBox.Show(this,
"��Ŀ��¼ " + strPath + " �� MARC ��ʽΪ UNIMARC���ڱ���Ի���ѡ��<�Զ�>��������£��ڱ���ǰ�����ᱻ����Ϊ 880 ģʽ�����ȷ���ڱ���ǰ����Ϊ 880 ģʽ������ֹ��ǰ���������½���һ�α��棬ע���ڱ���Ի�������ȷѡ�� ��USMARC�� ��ʽ��\r\n\r\n�����Ƿ��������? \r\n\r\n(Yes ��������UNIMARC ��ʽ��¼���ᴦ��Ϊ 880 ģʽ��\r\nNo �ж������������)",
"BiblioSearchForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                            if (result == System.Windows.Forms.DialogResult.No)
                                goto END1;
                            bAsked = true;
                        }
                    }
                    else
                    {
                        strMarcSyntax = dlg.MarcSyntax;
                        // TODO: ��鳣���ֶ�������ѡ���� MARC ��ʽ�Ƿ�ì�ܡ����ì�ܸ�������
                    }

                    Debug.Assert(strMarcSyntax != "", "");

                    if (dlg.RemoveField998 == true)
                    {
                        MarcRecord temp = new MarcRecord(strRecord);
                        temp.select("field[@name='998']").detach();
                        strRecord = temp.Text;
                    }

                    if (dlg.Mode880 == true && strMarcSyntax == "usmarc")
                    {
                        MarcRecord temp = new MarcRecord(strRecord);
                        MarcQuery.To880(temp);
                        strRecord = temp.Text;
                    }

                    // ��MARC���ڸ�ʽת��ΪISO2709��ʽ
                    // parameters:
                    //      strSourceMARC   [in]���ڸ�ʽMARC��¼��
                    //      strMarcSyntax   [in]Ϊ"unimarc"��"usmarc"
                    //      targetEncoding  [in]���ISO2709�ı��뷽ʽ��ΪUTF8��codepage-936�ȵ�
                    //      baResult    [out]�����ISO2709��¼�����뷽ʽ��targetEncoding�������ơ�ע�⣬������ĩβ������0�ַ���
                    // return:
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = MarcUtil.CvtJineiToISO2709(
                        strRecord,
                        strMarcSyntax,
                        targetEncoding,
                        out baTarget,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    /*
                    Encoding sourceEncoding = connection.GetRecordsEncoding(
                        this.MainForm,
                        record.m_strSyntaxOID);


                    if (sourceEncoding.Equals(targetEncoding) == true)
                    {
                        // source��target���뷽ʽ��ͬ������ת��
                        baTarget = record.m_baRecord;
                    }
                    else
                    {
                        nRet = ChangeIso2709Encoding(
                            sourceEncoding,
                            record.m_baRecord,
                            targetEncoding,
                            strMarcSyntax,
                            out baTarget,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }*/

                    s.Write(baTarget, 0,
                        baTarget.Length);

                    if (dlg.CrLf == true)
                    {
                        byte[] baCrLf = targetEncoding.GetBytes("\r\n");
                        s.Write(baCrLf, 0,
                            baCrLf.Length);
                    }

                    nCount++;

                    stop.SetProgressValue(i + 1);
                }
            }
            catch (Exception ex)
            {
                strError = "д���ļ� " + MainForm.LastIso2709FileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }
            finally
            {
                s.Close();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControls(true);

            }

        END1:
            // 
            if (bAppend == true)
                MainForm.MessageText = nCount.ToString()
                    + "����¼�ɹ�׷�ӵ��ļ� " + MainForm.LastIso2709FileName + " β��";
            else
                MainForm.MessageText = nCount.ToString()
                    + "����¼�ɹ����浽���ļ� " + MainForm.LastIso2709FileName + " β��";

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �Ƿ�����װ���Ѿ��򿪵���ϸ��?
        public bool LoadToExistDetailWindow
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "all_search_form",
                    "load_to_exist_detailwindow",
                    true);
            }
        }

        private void listView_browse_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripMenuItem menuItem = null;

            int nSelectedCount = 0;
            nSelectedCount = this.listView_browse.SelectedItems.Count;

            bool bHasTopDetailForm = false;
            if (nSelectedCount > 0)
                bHasTopDetailForm = HasTopDetailForm(this.listView_browse.SelectedIndices[0]);

            menuItem = new ToolStripMenuItem("װ���Ѵ򿪵ļ�¼��(&L)");
            if (this.LoadToExistDetailWindow == true
                && bHasTopDetailForm == true)
                menuItem.Font = new Font(menuItem.Font, FontStyle.Bold);
            menuItem.Click += new System.EventHandler(this.menu_loadToOpenedDetailForm_Click);
            if (nSelectedCount == 0
                || bHasTopDetailForm == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("װ���¿��ļ�¼��(&L)");
            if (this.LoadToExistDetailWindow == false
                || bHasTopDetailForm == false)
                menuItem.Font = new Font(menuItem.Font, FontStyle.Bold);
            menuItem.Click += new System.EventHandler(this.menu_loadToNewDetailForm_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ---
            ToolStripSeparator sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            menuItem = new ToolStripMenuItem("����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_cutToClipboard_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);


            menuItem = new ToolStripMenuItem("����(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copyToClipboard_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("���Ƶ���(&S)");
            if (this.listView_browse.SelectedIndices.Count == 0)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            for (int i = 0; i < this.listView_browse.Columns.Count; i++)
            {
                ToolStripMenuItem subMenuItem = new ToolStripMenuItem("������ '" + this.listView_browse.Columns[i].Text + "'");
                subMenuItem.Tag = i;
                subMenuItem.Click += new System.EventHandler(this.menu_copySingleColumnToClipboard_Click);
                menuItem.DropDownItems.Add(subMenuItem);
            }

            bool bHasClipboardObject = false;
            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null
                || iData.GetDataPresent(typeof(string)) == false)
                bHasClipboardObject = false;
            else
                bHasClipboardObject = true;

            menuItem = new ToolStripMenuItem("ճ��[ǰ��](&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteFromClipboard_insertBefore_Click);
            if (bHasClipboardObject == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("ճ��[���](&V)");
            menuItem.Click += new System.EventHandler(this.menu_pasteFromClipboard_insertAfter_Click);
            if (bHasClipboardObject == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            // ȫѡ
            menuItem = new ToolStripMenuItem("ȫѡ(&A)");
            menuItem.Click += new EventHandler(menuItem_selectAll_Click);
            contextMenu.Items.Add(menuItem);

            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            menuItem = new ToolStripMenuItem("ˢ����ѡ��� " + nSelectedCount.ToString() + " �������(&B)");
            menuItem.Click += new System.EventHandler(this.menu_refreshSelectedItems_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);


            menuItem = new ToolStripMenuItem("�Ƴ���ѡ��� " + nSelectedCount.ToString() + " ������(&R)");
            menuItem.Click += new System.EventHandler(this.menu_removeSelectedItems_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);


            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            // bool bLooping = (stop != null && stop.State == 0);    // 0 ��ʾ���ڴ���

            // ������
            // ���ڼ�����ʱ�򣬲���������������������Ϊstop.BeginLoop()Ƕ�׺��Min Max Value֮��ı���ָ����⻹û�н��
            {
                menuItem = new ToolStripMenuItem("������(&B)");
                contextMenu.Items.Add(menuItem);

#if NO
                ToolStripMenuItem subMenuItem = new MenuItem("�����޸���Ŀ��¼ [" + this.listView_records.SelectedItems.Count.ToString() + "] (&Q)");
                subMenuItem.Click += new System.EventHandler(this.menu_quickChangeRecords_Click);
                if (this.listView_browse.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.DropDownItems.Add(subMenuItem);

                
                // ---
                sep = new ToolStripSeparator();
                menuItem.DropDownItems.Add(sep);
#endif


                ToolStripMenuItem subMenuItem = new ToolStripMenuItem("ִ�� MarcQuery �ű� [" + this.listView_browse.SelectedItems.Count.ToString() + "] (&M)");
                subMenuItem.Click += new System.EventHandler(this.menu_quickMarcQueryRecords_Click);
                if (this.listView_browse.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.DropDownItems.Add(subMenuItem);

                subMenuItem = new ToolStripMenuItem("�����޸� [" + this.listView_browse.SelectedItems.Count.ToString() + "] (&C)");
                subMenuItem.Click += new System.EventHandler(this.menu_clearSelectedChangedRecords_Click);
                if (this.m_nChangedCount == 0 || this.listView_browse.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.DropDownItems.Add(subMenuItem);

                subMenuItem = new ToolStripMenuItem("����ȫ���޸� [" + this.m_nChangedCount.ToString() + "] (&L)");
                subMenuItem.Click += new System.EventHandler(this.menu_clearAllChangedRecords_Click);
                if (this.m_nChangedCount == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.DropDownItems.Add(subMenuItem);

                subMenuItem = new ToolStripMenuItem("����ѡ�����޸� [" + this.listView_browse.SelectedItems.Count.ToString() + "] (&S)");
                subMenuItem.Click += new System.EventHandler(this.menu_saveSelectedChangedRecords_Click);
                if (this._linkMarcFile != null || this.m_nChangedCount == 0 || this.listView_browse.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.DropDownItems.Add(subMenuItem);

                subMenuItem = new ToolStripMenuItem("����ȫ���޸� [" + this.m_nChangedCount.ToString() + "] (&A)");
                subMenuItem.Click += new System.EventHandler(this.menu_saveAllChangedRecords_Click);
                if (this._linkMarcFile != null || this.m_nChangedCount == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.DropDownItems.Add(subMenuItem);

                subMenuItem = new ToolStripMenuItem("�����µ� MarcQuery �ű��ļ� (&C)");
                subMenuItem.Click += new System.EventHandler(this.menu_createMarcQueryCsFile_Click);
                menuItem.DropDownItems.Add(subMenuItem);

                // ---
                sep = new ToolStripSeparator();
                menuItem.DropDownItems.Add(sep);

                subMenuItem = new ToolStripMenuItem("ɾ����ѡ��� " + nSelectedCount.ToString() + " ����Ŀ��¼(&D)");
                subMenuItem.Click += new System.EventHandler(this.menu_deleteSelectedRecords_Click);
                if (this._linkMarcFile != null
                    || nSelectedCount == 0
                    || this.m_bInSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.DropDownItems.Add(subMenuItem);


                // ---
                sep = new ToolStripSeparator();
                menuItem.DropDownItems.Add(sep);

                // ׷�ӱ��浽���ݿ�
                subMenuItem = new ToolStripMenuItem("��ѡ���� " + nSelectedCount.ToString() + " ����¼��׷�ӷ�ʽ���浽���ݿ�(&A)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveToDatabase_Click);
                if (nSelectedCount == 0)
                    subMenuItem.Enabled = false;
                menuItem.DropDownItems.Add(subMenuItem);
            }

            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            menuItem = new ToolStripMenuItem("������ѡ��� " + nSelectedCount.ToString() + " �������¼·���ļ�(&S)...");
            menuItem.Click += new System.EventHandler(this.menu_saveToRecordPathFile_Click);
            if (this._linkMarcFile != null || nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);


            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            menuItem = new ToolStripMenuItem("�Ӽ�¼·���ļ�����(&I)...");
            menuItem.Click += new System.EventHandler(this.menu_importFromRecPathFile_Click);
            if (this.m_bInSearching == true)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("�� MARC �ļ�����(&M)...");
            menuItem.Click += new System.EventHandler(this.menu_importFromMarcFile_Click);
            if (this.m_bInSearching == true)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            // ����ԭʼ��¼���������ļ�
            menuItem = new ToolStripMenuItem("����ѡ���� "
                + nSelectedCount.ToString()
                + " ����¼���������ļ�(&W)");
            if (nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            menuItem.Click += new EventHandler(menuItem_saveOriginRecordToWorksheet_Click);
            contextMenu.Items.Add(menuItem);


            // ����ԭʼ��¼��ISO2709�ļ�
            menuItem = new ToolStripMenuItem("����ѡ���� "
                + nSelectedCount.ToString()
                + " ����¼�� MARC �ļ�(&S)");
            if (nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            menuItem.Click += new EventHandler(menuItem_saveOriginRecordToIso2709_Click);
            contextMenu.Items.Add(menuItem);

            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            menuItem = new ToolStripMenuItem("����������������(&C)");
            menuItem.Click += new System.EventHandler(this.menu_clearBrowseFltxCache_Click);
            if (this.m_bInSearching == true)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            contextMenu.Show(this.listView_browse, e.Location);
        }

        void menu_clearBrowseFltxCache_Click(object sender, EventArgs e)
        {
            this.Filters.Clear();
        }

        void menu_copySingleColumnToClipboard_Click(object sender, EventArgs e)
        {
            int nColumn = (int)((ToolStripMenuItem)sender).Tag;

            Global.CopyLinesToClipboard(this, nColumn, this.listView_browse, false);
        }

        #region MarcQuery


#if NO
        // �� .ref ��ȡ���ӵĿ��ļ�·��
        int GetRef(string strCsFileName,
            ref string[] refs,
            out string strError)
        {
            strError = "";

            string strRefFileName = strCsFileName + ".ref";

            // .ref�ļ�����ȱʡ
            if (File.Exists(strRefFileName) == false)
                return 0;   // .ref �ļ�������

            string strRef = "";
            try
            {
                using (StreamReader sr = new StreamReader(strRefFileName, true))
                {
                    strRef = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            // ��ǰ���
            string[] add_refs = null;
            int nRet = ScriptManager.GetRefsFromXml(strRef,
                out add_refs,
                out strError);
            if (nRet == -1)
            {
                strError = strRefFileName + " �ļ�����(ӦΪXML��ʽ)��ʽ����: " + strError;
                return -1;
            }

            // ���ֺ�
            if (add_refs != null)
            {
                for (int i = 0; i < add_refs.Length; i++)
                {
                    add_refs[i] = add_refs[i].Replace("%bindir%", Environment.CurrentDirectory);
                }
            }

            refs = Append(refs, add_refs);
            return 1;
        }
#endif

        // ׼���ű�����
        // TODO: ���ͬ���� .ref �ļ�
        int PrepareMarcQuery(string strCsFileName,
            out Assembly assembly,
            out MarcQueryHost host,
            out string strError)
        {
            assembly = null;
            strError = "";
            host = null;


            string strContent = "";
            Encoding encoding;
            // ���Զ�ʶ���ļ����ݵı��뷽ʽ�Ķ����ı��ļ�����ģ��
            // parameters:
            //      lMaxLength  װ�����󳤶ȡ�����������򳬹��Ĳ��ֲ�װ�롣���Ϊ-1����ʾ������װ�볤��
            // return:
            //      -1  ���� strError���з���ֵ
            //      0   �ļ������� strError���з���ֵ
            //      1   �ļ�����
            //      2   ��������ݲ���ȫ��
            int nRet = FileUtil.ReadTextFileContent(strCsFileName,
                -1,
                out strContent,
                out encoding,
                out strError);
            if (nRet == -1)
                return -1;

            string[] saAddRef = {
                                    // 2011/4/20 ����
                                    "system.dll",
                                    "system.drawing.dll",
                                    "system.windows.forms.dll",
                                    "system.xml.dll",
                                    "System.Runtime.Serialization.dll",
                                    // "D:\\Program Files\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.0\\WindowsBase.dll",
                                    ExcelUtil.GetWindowsBaseDllPath(),

									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
   									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Script.dll",  // 2011/8/25 ����
									Environment.CurrentDirectory + "\\digitalplatform.dp2.statis.dll",
									Environment.CurrentDirectory + "\\documentformat.openxml.dll",
                                    Environment.CurrentDirectory + "\\dp2catalog.exe",
            };

            nRet = ScriptManager.GetRef(strCsFileName,
                ref saAddRef,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string strTemp = ExcelUtil.GetWindowsBaseDllPath();

            string strWarningInfo = "";
            // ֱ�ӱ��뵽�ڴ�
            // parameters:
            //		refs	���ӵ�refs�ļ�·����·���п��ܰ�����%installdir%
            nRet = ScriptManager.CreateAssembly_1(strContent,
                saAddRef,
                "", // strLibPath,
                out assembly,
                out strError,
                out strWarningInfo);
            if (nRet == -1)
                goto ERROR1;

            // �õ�Assembly��Host������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                assembly,
                "dp2Catalog.MarcQueryHost");
            if (entryClassType == null)
            {
                strError = strCsFileName + " ��û���ҵ� dp2Catalog.MarcQueryHost ������";
                goto ERROR1;
            }

            // newһ��Host��������
            host = (MarcQueryHost)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            return 0;
        ERROR1:
            return -1;
        }

        #endregion

        void menu_quickMarcQueryRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_browse.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫִ�� MarcQuery �ű�������";
                goto ERROR1;
            }

            // ��Ŀ��Ϣ����
            // ����Ѿ���ʼ�����򱣳�
            if (this.m_biblioTable == null)
                this.m_biblioTable = new Hashtable();

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ�� MarcQuery �ű��ļ�";
            dlg.FileName = this.m_strUsedMarcQueryFilename;
            dlg.Filter = "MarcQuery �ű��ļ� (*.cs)|*.cs|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.m_strUsedMarcQueryFilename = dlg.FileName;

            MarcQueryHost host = null;
            Assembly assembly = null;

            nRet = PrepareMarcQuery(this.m_strUsedMarcQueryFilename,
                out assembly,
                out host,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            host.MainForm = this.MainForm;
            host.UiForm = this;
            host.CodeFileName = this.m_strUsedMarcQueryFilename;    // 2013/10/8

            this.MainForm.OperHistory.AppendHtml("<div class='debug begin'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ��ʼִ�нű� " + dlg.FileName + "</div>");

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���������Ŀ��¼ִ�� MarcQuery �ű� ...");
            stop.BeginLoop();

            this.EnableControls(false);

            this.listView_browse.Enabled = false;
            try
            {

                // Initial
            {
                host.RecordPath = "";
                host.MarcRecord = null;
                host.MarcSyntax = "";
                host.Changed = false;
                host.UiItem = null;

                StatisEventArgs args = new StatisEventArgs();
                host.OnInitial(this, args);
                if (args.Continue == ContinueType.SkipAll)
                    return;
                if (args.Continue == ContinueType.Error)
                {
                    strError = args.ParamString;
                    goto ERROR1;
                }
            }


                if (stop != null)
                    stop.SetProgressRange(0, this.listView_browse.SelectedItems.Count);

                {
                    host.RecordPath = "";
                    host.MarcRecord = null;
                    host.MarcSyntax = "";
                    host.Changed = false;
                    host.UiItem = null;

                    StatisEventArgs args = new StatisEventArgs();
                    host.OnBegin(this, args);
                    if (args.Continue == ContinueType.SkipAll)
                        return;
                    if (args.Continue == ContinueType.Error)
                    {
                        strError = args.ParamString;
                        goto ERROR1;
                    }
                }

                List<ListViewItem> items = new List<ListViewItem>();
                foreach (ListViewItem item in this.listView_browse.SelectedItems)
                {
                    if (string.IsNullOrEmpty(item.Text) == true)
                        continue;

                    items.Add(item);
                }

                ListViewBiblioLoader loader = new ListViewBiblioLoader(this.Channels,
                    this.dp2ResTree1.Servers,
                    stop,
                    items,
                    this.m_biblioTable);

                int i = 0;
                foreach (LoaderItem item in loader)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null
                        && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    stop.SetProgressValue(i);

                    BiblioInfo info = item.BiblioInfo;

                    string strMARC = "";
                    string strMarcSyntax = "";
                    // ��XML��ʽת��ΪMARC��ʽ
                    // �Զ������ݼ�¼�л��MARC�﷨
                    nRet = MarcUtil.Xml2Marc(info.OldXml,
                        true,
                        null,
                        out strMarcSyntax,
                        out strMARC,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "XMLת����MARC��¼ʱ����: " + strError;
                        goto ERROR1;
                    }

                    this.MainForm.OperHistory.AppendHtml("<div class='debug recpath'>" + HttpUtility.HtmlEncode(info.RecPath) + "</div>");

                    string strOuterFieldDef = "";
                    if (strMarcSyntax == "unimarc")
                        strOuterFieldDef = "4**";

                    host.RecordPath = info.RecPath;
                    host.MarcRecord = new MarcRecord(strMARC, strOuterFieldDef);
                    host.MarcSyntax = strMarcSyntax;
                    host.Changed = false;
                    host.UiItem = item.ListViewItem;

                    StatisEventArgs args = new StatisEventArgs();
                    host.OnRecord(this, args);
                    if (args.Continue == ContinueType.SkipAll)
                        break;
                    if (args.Continue == ContinueType.Error)
                    {
                        strError = args.ParamString;
                        goto ERROR1;
                    }

                    if (host.Changed == true)
                    {
                        string strXml = info.OldXml;
                        nRet = MarcUtil.Marc2XmlEx(host.MarcRecord.Text,
                            strMarcSyntax,
                            ref strXml,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        if (info != null)
                        {
                            if (string.IsNullOrEmpty(info.NewXml) == true)
                                this.m_nChangedCount++;
                            info.NewXml = strXml;
                            info.NewVersion = DateTime.Now.Ticks;
                        }

                        item.ListViewItem.BackColor = SystemColors.Info;
                        item.ListViewItem.ForeColor = SystemColors.InfoText;
                    }

                    // ��ʾΪ��������ʽ
                    i++;
                }

                {
                    host.RecordPath = "";
                    host.MarcRecord = null;
                    host.MarcSyntax = "";
                    host.Changed = false;
                    host.UiItem = null;

                    StatisEventArgs args = new StatisEventArgs();
                    host.OnEnd(this, args);
                    if (args.Continue == ContinueType.Error)
                    {
                        strError = args.ParamString;
                        goto ERROR1;
                    }
                }
            }
            catch (Exception ex)
            {
                strError = "ִ�� MarcQuery �ű��Ĺ����г����쳣: " + ExceptionUtil.GetDebugText(ex);
                goto ERROR1;
            }
            finally
            {
                if (host != null)
                    host.FreeResources();

                this.listView_browse.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControls(true);

                this.MainForm.OperHistory.AppendHtml("<div class='debug end'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ����ִ�нű� " + dlg.FileName + "</div>");
            }

            DoViewComment(false);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ����ѡ�����޸�
        void menu_clearSelectedChangedRecords_Click(object sender, EventArgs e)
        {
            // TODO: ȷʵҪ?

            if (this.m_nChangedCount == 0)
            {
                MessageBox.Show(this, "��ǰû���κ��޸Ĺ�������ɶ���");
                return;
            }
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                foreach (ListViewItem item in this.listView_browse.SelectedItems)
                {
                    string strRecPath = item.Text;
                    if (string.IsNullOrEmpty(strRecPath) == true)
                        continue;

                    BiblioInfo info = (BiblioInfo)this.m_biblioTable[strRecPath];
                    if (info == null)
                        continue;

                    if (String.IsNullOrEmpty(info.NewXml) == false)
                    {
                        info.NewXml = "";

                        item.BackColor = SystemColors.Window;
                        item.ForeColor = SystemColors.WindowText;

                        this.m_nChangedCount--;
                        Debug.Assert(this.m_nChangedCount >= 0, "");
                    }

                }
            }
            finally
            {
                this.Cursor = oldCursor;
            }

            DoViewComment(false);
        }

        // ����ȫ���޸�
        void menu_clearAllChangedRecords_Click(object sender, EventArgs e)
        {
            // TODO: ȷʵҪ?

            if (this.m_nChangedCount == 0)
            {
                MessageBox.Show(this, "��ǰû���κ��޸Ĺ�������ɶ���");
                return;
            }
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                foreach (ListViewItem item in this.listView_browse.Items)
                {
                    string strRecPath = item.Text;
                    if (string.IsNullOrEmpty(strRecPath) == true)
                        continue;

                    BiblioInfo info = (BiblioInfo)this.m_biblioTable[strRecPath];
                    if (info == null)
                        continue;

                    if (String.IsNullOrEmpty(info.NewXml) == false)
                    {
                        info.NewXml = "";

                        item.BackColor = SystemColors.Window;
                        item.ForeColor = SystemColors.WindowText;

                        this.m_nChangedCount--;
                        Debug.Assert(this.m_nChangedCount >= 0, "");
                    }
                }
            }
            finally
            {
                this.Cursor = oldCursor;
            }

            DoViewComment(false);
        }

        // ����ѡ��������޸�
        // ע:���ܱ���ص�(ԭ��װ�����Ե�) MARC �ļ�
        void menu_saveSelectedChangedRecords_Click(object sender, EventArgs e)
        {
            // TODO: ȷʵҪ?
            string strError = "";

            if (this._linkMarcFile != null)
            {
                strError = "�ݲ�������� MARC �ļ�";
                goto ERROR1;
            }
            if (this.m_nChangedCount == 0)
            {
                strError = "��ǰû���κ��޸Ĺ���������Ҫ����";
                goto ERROR1;
            }

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_browse.SelectedItems)
            {
                items.Add(item);
            }

            int nRet = SaveChangedRecords(items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            strError = "������ɡ�\r\n\r\n" + strError;
            MessageBox.Show(this, strError);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        // ����ȫ���޸�����
        // ע:���ܱ���ص�(ԭ��װ�����Ե�) MARC �ļ�
        void menu_saveAllChangedRecords_Click(object sender, EventArgs e)
        {
            // TODO: ȷʵҪ?
            string strError = "";

            if (this._linkMarcFile != null)
            {
                strError = "�ݲ�������� MARC �ļ�";
                goto ERROR1;
            }
            if (this.m_nChangedCount == 0)
            {
                strError = "��ǰû���κ��޸Ĺ���������Ҫ����";
                goto ERROR1;
            }

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_browse.Items)
            {
                items.Add(item);
            }

            int nRet = SaveChangedRecords(items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            strError = "������ɡ�\r\n\r\n" + strError;
            MessageBox.Show(this, strError);
            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        int SaveChangedRecords(List<ListViewItem> items,
    out string strError)
        {
            strError = "";

            int nReloadCount = 0;
            int nSavedCount = 0;

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ�����Ŀ��¼ ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);
            this.listView_browse.Enabled = false;
            try
            {
                stop.SetProgressRange(0, items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    if (stop != null && stop.State != 0)
                    {
                        strError = "���ж�";
                        return -1;
                    }

                    ListViewItem item = items[i];
                    string strRecPath = item.Text;
                    if (string.IsNullOrEmpty(strRecPath) == true)
                    {
                        stop.SetProgressValue(i);
                        goto CONTINUE;
                    }

                    BiblioInfo info = (BiblioInfo)this.m_biblioTable[strRecPath];
                    if (info == null)
                        goto CONTINUE;

                    if (string.IsNullOrEmpty(info.NewXml) == true)
                        goto CONTINUE;

                    stop.SetMessage("���ڱ�����Ŀ��¼ " + strRecPath);

                    // ������¼·��
                    string strServerName = "";
                    string strPurePath = "";
                    ParseRecPath(strRecPath,
                        out strServerName,
                        out strPurePath);

                    // ���server url
                    dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
                    if (server == null)
                    {
                        strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                        return -1;
                    }
                    string strServerUrl = server.Url;

                    this.Channel = this.Channels.GetChannel(strServerUrl);

                    string strOutputPath = "";

                    byte[] baNewTimestamp = null;

                    long lRet = Channel.SetBiblioInfo(
                        stop,
                        "change",
                        strPurePath,
                        "xml",
                        info.NewXml,
                        info.Timestamp,
                        "",
                        out strOutputPath,
                        out baNewTimestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        if (Channel.ErrorCode == ErrorCode.TimestampMismatch)
                        {
                            DialogResult result = MessageBox.Show(this,
    "������Ŀ��¼ " + strRecPath + " ʱ����ʱ�����ƥ��: " + strError + "��\r\n\r\n�˼�¼���޷������档\r\n\r\n���������Ƿ�Ҫ˳������װ�ش˼�¼? \r\n\r\n(Yes ����װ�أ�\r\nNo ������װ�ء��������������ļ�¼����; \r\nCancel �ж������������)",
    "BiblioSearchForm",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                            if (result == System.Windows.Forms.DialogResult.Cancel)
                                break;
                            if (result == System.Windows.Forms.DialogResult.No)
                                goto CONTINUE;

                            // ����װ����Ŀ��¼�� OldXml
                            string[] results = null;
                            // byte[] baTimestamp = null;
                            lRet = Channel.GetBiblioInfos(
                                stop,
                                strPurePath,
                                "",
                                new string[] { "xml" },   // formats
                                out results,
                                out baNewTimestamp,
                                out strError);
                            if (lRet == 0)
                            {
                                // TODO: ����󣬰� item ���Ƴ���
                                return -1;
                            }
                            if (lRet == -1)
                                return -1;
                            if (results == null || results.Length == 0)
                            {
                                strError = "results error";
                                return -1;
                            }
                            info.OldXml = results[0];
                            info.Timestamp = baNewTimestamp;
                            nReloadCount++;
                            goto CONTINUE;
                        }

                        return -1;
                    }

                    // ����Ƿ��в����ֶα��ܾ�
                    if (Channel.ErrorCode == ErrorCode.PartialDenied)
                    {
                        DialogResult result = MessageBox.Show(this,
"������Ŀ��¼ " + strRecPath + " ʱ�����ֶα��ܾ���\r\n\r\n�˼�¼�Ѳ��ֱ���ɹ���\r\n\r\n���������Ƿ�Ҫ˳������װ�ش˼�¼�Ա�۲�? \r\n\r\n(Yes ����װ��(���ɼ�¼����)��\r\nNo ������װ�ء��������������ļ�¼����; \r\nCancel �ж������������)",
"BiblioSearchForm",
MessageBoxButtons.YesNoCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.Cancel)
                            break;
                        if (result == System.Windows.Forms.DialogResult.No)
                            goto CONTINUE;
                        // ����װ����Ŀ��¼�� OldXml
                        string[] results = null;
                        // byte[] baTimestamp = null;
                        lRet = Channel.GetBiblioInfos(
                            stop,
                            strPurePath,
                            "",
                            new string[] { "xml" },   // formats
                            out results,
                            out baNewTimestamp,
                            out strError);
                        if (lRet == 0)
                        {
                            // TODO: ����󣬰� item ���Ƴ���
                            return -1;
                        }
                        if (lRet == -1)
                            return -1;
                        if (results == null || results.Length == 0)
                        {
                            strError = "results error";
                            return -1;
                        }
                        info.OldXml = results[0];
                        info.Timestamp = baNewTimestamp;
                        nReloadCount++;
                        goto CONTINUE;
                    }

                    info.Timestamp = baNewTimestamp;
                    info.OldXml = info.NewXml;
                    info.NewXml = "";

                    item.BackColor = SystemColors.Window;
                    item.ForeColor = SystemColors.WindowText;

                    nSavedCount++;

                    this.m_nChangedCount--;
                    Debug.Assert(this.m_nChangedCount >= 0, "");

                CONTINUE:
                    stop.SetProgressValue(i);
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControlsInSearching(true);
                this.listView_browse.Enabled = true;
            }

            //2013/10/22
            int nRet = RefreshListViewLines(items,
    out strError);
            if (nRet == -1)
                return -1;

            DoViewComment(false);

            strError = "";
            if (nSavedCount > 0)
                strError += "��������Ŀ��¼ " + nSavedCount + " ��";
            if (nReloadCount > 0)
            {
                if (string.IsNullOrEmpty(strError) == false)
                    strError += " ; ";
                strError += "�� " + nReloadCount + " ����Ŀ��¼��Ϊʱ�����ƥ��򲿷��ֶα��ܾ�������װ�ؾɼ�¼����(��۲�����±���)";
            }

            return 0;
        }

        string m_strUsedMarcQueryFilename = "";

        // �����µ� MarcQuery �ű��ļ�
        void menu_createMarcQueryCsFile_Click(object sender, EventArgs e)
        {
            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ�����Ľű��ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = true;
            dlg.FileName = "";
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "C#�ű��ļ� (*.cs)|*.cs|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                MarcQueryHost.CreateStartCsFile(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return;
            }

            System.Diagnostics.Process.Start("notepad.exe", dlg.FileName);

            this.m_strUsedMarcQueryFilename = dlg.FileName;

        }

        // ɾ����ѡ�����Ŀ��¼
        public void DeleteSelectedRecords()
        {
            string strError = "";

            if (this._linkMarcFile != null)
            {
                strError = "�ݲ�֧�ִ� MARC �ļ���ֱ��ɾ����¼";
                goto ERROR1;
            }

            if (this.listView_browse.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫɾ������Ŀ��¼";
                goto ERROR1;
            }

            DialogResult result = MessageBox.Show(this,
"ȷʵҪ�����ݿ���ɾ����ѡ���� " + this.listView_browse.SelectedItems.Count.ToString() + " ����Ŀ��¼?\r\n\r\n(���棺��Ŀ��¼��ɾ�����޷��ָ������ɾ����Ŀ��¼�����������Ĳᡢ�ڡ���������ע��¼�Ͷ�����Դ��һ��ɾ��)\r\n\r\n(OK ɾ����Cancel ȡ��)",
"dp2SearchForm",
MessageBoxButtons.OKCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
            if (result == System.Windows.Forms.DialogResult.Cancel)
                return;

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_browse.SelectedItems)
            {
                items.Add(item);
            }

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ɾ����Ŀ��¼ ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);

            // ��ʱ��ֹ��Ϊ listview ѡ�����ı��Ƶ��ˢ��״̬��
            this.listView_browse.SelectedIndexChanged -= new System.EventHandler(this.listView_browse_SelectedIndexChanged);

            try
            {
                stop.SetProgressRange(0, items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "���ж�";
                            goto ERROR1;
                        }
                    }

                    ListViewItem item = items[i];
                    string strRecPath = ListViewUtil.GetItemText(item, 0);

                    // ������¼·��
                    string strServerName = "";
                    string strPurePath = "";
                    ParseRecPath(strRecPath,
                        out strServerName,
                        out strPurePath);

                    // ���server url
                    dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
                    if (server == null)
                    {
                        strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                        goto ERROR1;
                    }
                    string strServerUrl = server.Url;

                    this.Channel = this.Channels.GetChannel(strServerUrl);

                    string[] results = null;
                    byte[] baTimestamp = null;
                    string strOutputPath = "";

                    stop.SetMessage("����ɾ����Ŀ��¼ " + strPurePath);

                    long lRet = Channel.GetBiblioInfos(
                        stop,
                        strPurePath,
                        "",
                        null,   // formats
                        out results,
                        out baTimestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        result = MessageBox.Show(this,
    "�ڻ�ü�¼ '" + strRecPath + "' ��ʱ����Ĺ����г��ִ���: " + strError + "��\r\n\r\n�Ƿ����ǿ��ɾ���˼�¼? (Yes ǿ��ɾ����No ��ɾ����Cancel ������ǰδ��ɵ�ȫ��ɾ������)",
    "dp2SearchForm",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.Cancel)
                            goto ERROR1;
                        if (result == System.Windows.Forms.DialogResult.No)
                            continue;
                    } 
                    if (lRet == -1 || lRet == 0)
                        goto ERROR1;

                    byte[] baNewTimestamp = null;

                    lRet = Channel.SetBiblioInfo(
                        stop,
                        "delete",
                        strPurePath,
                        "xml",
                        "", // strXml,
                        baTimestamp,
                        "",
                        out strOutputPath,
                        out baNewTimestamp,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    stop.SetProgressValue(i);

                    this.listView_browse.Items.Remove(item);
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControlsInSearching(true);
                this.listView_browse.SelectedIndexChanged += new System.EventHandler(this.listView_browse_SelectedIndexChanged);
            }

            MessageBox.Show(this, "�ɹ�ɾ����Ŀ��¼ " + items.Count + " ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_deleteSelectedRecords_Click(object sender, EventArgs e)
        {
            DeleteSelectedRecords();
        }

        LinkMarcFile _linkMarcFile = null;

        public LinkMarcFile LinkMarcFile
        {
            get
            {
                return this._linkMarcFile;
            }
        }

        // �� MARC �ļ��е���
        void menu_importFromMarcFile_Click(object sender, EventArgs e)
        {
            OpenMarcFileDlg dlg = new OpenMarcFileDlg();
            GuiUtil.SetControlFont(dlg, this.Font);
            dlg.IsOutput = false;
            dlg.GetEncoding -= new GetEncodingEventHandler(dlg_GetEncoding);
            dlg.GetEncoding += new GetEncodingEventHandler(dlg_GetEncoding);
            dlg.FileName = this.MainForm.LinkedMarcFileName;
            // dlg.CrLf = MainForm.LastCrLfIso2709;
            dlg.EncodingListItems = Global.GetEncodingList(true);
            // dlg.EncodingName = ""; GetEncodingForm.GetEncodingName(preferredEncoding);
            // dlg.EncodingComment = "ע: ԭʼ���뷽ʽΪ " + GetEncodingForm.GetEncodingName(preferredEncoding);
            // dlg.MarcSyntax = "<�Զ�>";    // strPreferedMarcSyntax;
            dlg.EnableMarcSyntax = true;

            if (String.IsNullOrEmpty(this.MainForm.LinkedEncodingName) == false)
                dlg.EncodingName = this.MainForm.LinkedEncodingName;
            if (String.IsNullOrEmpty(this.MainForm.LinkedMarcSyntax) == false)
                dlg.MarcSyntax = this.MainForm.LinkedMarcSyntax;

            dlg.ShowDialog(this);
            if (dlg.DialogResult != DialogResult.OK)
                return;

            // �����ù����ļ���
            // 2009/9/21
            this.MainForm.LinkedMarcFileName = dlg.FileName;
            this.MainForm.LinkedEncodingName = dlg.EncodingName;
            this.MainForm.LinkedMarcSyntax = dlg.MarcSyntax;

            string strError = "";


            _linkMarcFile = new LinkMarcFile();
            int nRet = _linkMarcFile.Open(dlg.FileName,
                out strError);
            if (nRet == -1)
                goto ERROR1;



            _linkMarcFile.Encoding = dlg.Encoding;
            _linkMarcFile.MarcSyntax = dlg.MarcSyntax;

            ClearListViewItems();

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڴ� MARC �ļ����� ...");
            stop.BeginLoop();


            this.listView_browse.BeginUpdate();
            try
            {

                ListViewUtil.ClearSortColumns(this.listView_browse);
                stop.SetProgressRange(0, _linkMarcFile.Stream.Length);


                bool bEOF = false;
                for (int i = 0; bEOF == false; i++)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    string strMARC = "";
                    byte[] baRecord = null;
                    // �����һ����¼
                    // return:
                    //      -1  error
                    //      0   succeed
                    //      1   reach end(��ǰ���صļ�¼��Ч)
                    //	    2	����(��ǰ���صļ�¼��Ч)
                    nRet = _linkMarcFile.NextRecord(out strMARC,
                        out baRecord,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == 1)
                        bEOF = true;
                    if (nRet == 2)
                        break;

                    if (_linkMarcFile.MarcSyntax == "<�Զ�>"
    || _linkMarcFile.MarcSyntax.ToLower() == "<auto>")
                    {
                        // �Զ�ʶ��MARC��ʽ
                        string strOutMarcSyntax = "";
                        // ̽���¼��MARC��ʽ unimarc / usmarc / reader
                        // return:
                        //      0   û��̽�������strMarcSyntaxΪ��
                        //      1   ̽�������
                        nRet = MarcUtil.DetectMarcSyntax(strMARC,
                            out strOutMarcSyntax);
                        _linkMarcFile.MarcSyntax = strOutMarcSyntax;    // �п���Ϊ�գ���ʾ̽�ⲻ����
                        if (String.IsNullOrEmpty(_linkMarcFile.MarcSyntax) == true)
                        {
                            MessageBox.Show(this, "����޷�ȷ���� MARC �ļ��� MARC ��ʽ");
                        }
                    }

                    if (dlg.Mode880 == true && _linkMarcFile.MarcSyntax == "usmarc")
                    {
                        MarcRecord temp = new MarcRecord(strMARC);
                        MarcQuery.ToParallel(temp);
                        strMARC = temp.Text;
                    }

                    string strXml = "";
                    nRet = MarcUtil.Marc2XmlEx(strMARC,
        _linkMarcFile.MarcSyntax,
        ref strXml,
        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    string strRecPath = i.ToString() + " @file";
                    BiblioInfo info = new BiblioInfo();
                    info.RecPath = strRecPath;
                    this.m_biblioTable[strRecPath] = info;

                    info.OldXml = strXml;
                    info.Timestamp = null;
                    info.RecPath = strRecPath;

                    string strSytaxOID = "";

                    if (_linkMarcFile.MarcSyntax == "unimarc")
                        strSytaxOID = "1.2.840.10003.5.1";                // unimarc
                    else if (_linkMarcFile.MarcSyntax == "usmarc")
                        strSytaxOID = "1.2.840.10003.5.10";               // usmarc

                    string strBrowseText = "";
                    if (strSytaxOID == "1.2.840.10003.5.1"    // unimarc
        || strSytaxOID == "1.2.840.10003.5.10")  // usmarc
                    {
                        nRet = BuildMarcBrowseText(
                            strSytaxOID,
                            strMARC,
                            out strBrowseText,
                            out strError);
                        if (nRet == -1)
                            strBrowseText = strError;
                    }

                    string[] cols = strBrowseText.Split(new char[] { '\t' });

                    // ���������
                    NewLine(
        this.listView_browse,
        strRecPath,
        cols);
                    stop.SetMessage(i.ToString());
                    stop.SetProgressValue(_linkMarcFile.Stream.Position);
                }
            }
            finally
            {
                this.listView_browse.EndUpdate();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                _linkMarcFile.Close();
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
            _linkMarcFile = null;
        }

        void dlg_GetEncoding(object sender, GetEncodingEventArgs e)
        {
            string strError = "";
            Encoding encoding = null;
            int nRet = this.MainForm.GetEncoding(e.EncodingName,
                out encoding,
                out strError);
            if (nRet == -1)
            {
                e.ErrorInfo = strError;
                return;
            }
            e.Encoding = encoding;
        }

        // ����MARC��ʽ��¼�������ʽ
        // paramters:
        //      strMARC MARC���ڸ�ʽ
        public int BuildMarcBrowseText(
            string strSytaxOID,
            string strMARC,
            out string strBrowseText,
            out string strError)
        {
            strBrowseText = "";
            strError = "";

            FilterHost host = new FilterHost();
            host.ID = "";
            host.MainForm = this.MainForm;

            BrowseFilterDocument filter = null;

            string strFilterFileName = this.MainForm.DataDir + "\\" + strSytaxOID.Replace(".", "_") + "\\marc_browse.fltx";

            int nRet = this.PrepareMarcFilter(
                host,
                strFilterFileName,
                out filter,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            try
            {
                nRet = filter.DoRecord(null,
        strMARC,
        0,
        out strError);
                if (nRet == -1)
                    goto ERROR1;

                strBrowseText = host.ResultString;

            }
            finally
            {
                // �黹����
                this.Filters.SetFilter(strFilterFileName, filter);
            }

            return 0;
        ERROR1:
            // ���û��棬��Ϊ���ܳ����˱������
            // TODO: ��ȷ���ֱ������
            this.Filters.ClearFilter(strFilterFileName);
            return -1;
        }

        public int PrepareMarcFilter(
FilterHost host,
string strFilterFileName,
out BrowseFilterDocument filter,
out string strError)
        {
            strError = "";

            // �����Ƿ����ֳɿ��õĶ���
            filter = (BrowseFilterDocument)this.Filters.GetFilter(strFilterFileName);

            if (filter != null)
            {
                filter.FilterHost = host;
                return 1;
            }

            // �´���
            // string strFilterFileContent = "";

            filter = new BrowseFilterDocument();

            filter.FilterHost = host;
            filter.strOtherDef = "FilterHost Host = null;";

            filter.strPreInitial = " BrowseFilterDocument doc = (BrowseFilterDocument)this.Document;\r\n";
            filter.strPreInitial += " Host = ("
                + "FilterHost" + ")doc.FilterHost;\r\n";

            // filter.Load(strFilterFileName);

            try
            {
                filter.Load(strFilterFileName);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            string strCode = "";    // c#����

            int nRet = filter.BuildScriptFile(out strCode,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string[] saAddRef1 = {
										 this.BinDir + "\\digitalplatform.marcdom.dll",
										 // this.BinDir + "\\digitalplatform.marckernel.dll",
										 // this.BinDir + "\\digitalplatform.libraryserver.dll",
										 this.BinDir + "\\digitalplatform.dll",
										 this.BinDir + "\\digitalplatform.Text.dll",
										 this.BinDir + "\\digitalplatform.IO.dll",
										 this.BinDir + "\\digitalplatform.Xml.dll",
										 this.BinDir + "\\dp2catalog.exe" };

            Assembly assembly = null;
            string strWarning = "";
            string strLibPaths = "";

            string[] saRef2 = filter.GetRefs();

            string[] saRef = new string[saRef2.Length + saAddRef1.Length];
            Array.Copy(saRef2, saRef, saRef2.Length);
            Array.Copy(saAddRef1, 0, saRef, saRef2.Length, saAddRef1.Length);

            // ����Script��Assembly
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saRef,
                strLibPaths,
                out assembly,
                out strError,
                out strWarning);

            if (nRet == -2)
                goto ERROR1;
            if (nRet == -1)
            {
                if (strWarning == "")
                {
                    goto ERROR1;
                }
                // MessageBox.Show(this, strWarning);
            }

            filter.Assembly = assembly;

            return 0;
        ERROR1:
            return -1;
        }


        // �Ӽ�¼·���ļ��е���
        void menu_importFromRecPathFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵���Ŀ��¼·���ļ���";
            dlg.FileName = this.m_strUsedRecPathFilename;
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.m_strUsedRecPathFilename = dlg.FileName;

            StreamReader sr = null;
            string strError = "";
            bool bSkipBrowse = false;    // ����

            try
            {
                // TODO: ����Զ�̽���ļ��ı��뷽ʽ?
                sr = new StreamReader(dlg.FileName, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                strError = "���ļ� " + dlg.FileName + " ʧ��: " + ex.Message;
                goto ERROR1;
            }

            int nDupCount = 0;  // ���ֵ�ȫ���ظ��ļ�¼����������¼�����ظ�����һ����������
            int nSkipDupCount = 0;  // �Ѿ��������ظ���¼��

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ����¼·�� ...");
            stop.BeginLoop();

            this.m_bInSearching = true; // ��ֹ�м��ĳ�е��� GetBiblioInfo() �������ѭ����ͻ

            this.EnableControlsInSearching(false);
            this.listView_browse.BeginUpdate();
            try
            {
                // �����������û����ģ������Ҫ������е������־
                ListViewUtil.ClearSortColumns(this.listView_browse);
                stop.SetProgressRange(0, sr.BaseStream.Length);

                if (this.listView_browse.Items.Count > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "����ǰ�Ƿ�Ҫ������м�¼�б��е����е� " + this.listView_browse.Items.Count.ToString() + " ��?\r\n\r\n(�������������µ�����н�׷���������к���)\r\n\r\n(Yes �����No �����(׷��)��Cancel ��������)",
                        "dp2SearchForm",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.Cancel)
                        return;
                    if (result == DialogResult.Yes)
                    {
                        ClearListViewItems();
                    }
                }

                bool bHideMessageBox = false;
                DialogResult dup_result = System.Windows.Forms.DialogResult.OK;
                Hashtable dup_table = new Hashtable();  // ����¼·���Ƿ����ظ��� Hashtable

                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            MessageBox.Show(this, "�û��ж�");
                            return;
                        }
                    }


                    string strRecPath = sr.ReadLine();

                    stop.SetProgressValue(sr.BaseStream.Position);

                    if (strRecPath == null)
                        break;

                    // TODO: ���·������ȷ�ԣ�������ݿ��Ƿ�Ϊ��Ŀ��֮һ
                    // ������¼·��
                    string strServerUrl = "";
                    string strPurePath = "";
                    ParseRecPath(strRecPath,
                        out strServerUrl,
                        out strPurePath);
                    dp2Server server = this.dp2ResTree1.Servers.GetServer(strServerUrl);
                    if (server == null)
                    {
                        strError = "URL Ϊ '" + strServerUrl + "' �ķ������ڼ���������δ����...";
                        goto ERROR1;
                    }

                    strRecPath = strPurePath + "@" + server.Name;

                    // 2014/4/4
                    if (dup_table[strRecPath] != null)
                    {
                        nDupCount++;
                        if (bHideMessageBox == false)
                        {
                            // this.listView_browse.ForceUpdate();
                            Application.DoEvents();

                            dup_result = MessageDialog.Show(this,
    "��Ŀ��¼ " + strRecPath + " ���Ѿ�װ��ļ�¼�ظ��ˡ������Ƿ�װ���ظ��ļ�¼?",
    MessageBoxButtons.YesNoCancel,
    MessageBoxDefaultButton.Button1,
    "�Ժ�����ʾ�������ε�ѡ����",
    ref bHideMessageBox,
    new string[] { "װ��", "����", "�ж�" });
                        }

                        if (dup_result == System.Windows.Forms.DialogResult.No)
                        {
                            nSkipDupCount++;
                            continue;
                        }
                        if (dup_result == System.Windows.Forms.DialogResult.Cancel)
                            break;
                    }
                    else
                        dup_table[strRecPath] = true;

                    if (nSkipDupCount > 0)
                        stop.SetMessage("���ڵ���·�� " + strRecPath + " (�������ظ���¼ " + nSkipDupCount.ToString() + " ��)" );
                    else
                        stop.SetMessage("���ڵ���·�� " + strRecPath);

                    ListViewItem item = new ListViewItem();
                    item.Text = strRecPath;

                    this.listView_browse.Items.Add(item);

                    if (bSkipBrowse == false
                        && !(Control.ModifierKeys == Keys.Control))
                    {
                        int nRet = RefreshOneLine(item,
                out strError);
                        if (nRet == -1)
                        {
                            DialogResult result = MessageBox.Show(this,
        "����������ʱ����: " + strError + "��\r\n\r\n�Ƿ������ȡ�������? (Yes ��ȡ��No ����ȡ��Cancel ��������)",
        "dp2SearchForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                            if (result == System.Windows.Forms.DialogResult.No)
                                bSkipBrowse = true;
                            if (result == System.Windows.Forms.DialogResult.Cancel)
                            {
                                strError = "���ж�";
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                this.listView_browse.EndUpdate();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.m_bInSearching = false;

                this.EnableControlsInSearching(true);

                if (sr != null)
                    sr.Close();
            }

            if (nSkipDupCount > 0)
                MessageBox.Show(this, "װ��ɹ��������ظ���¼ "+nSkipDupCount+" ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ����ǰ����¼·�����Ѿ���ֵ
        int RefreshOneLine(ListViewItem item,
            out string strError)
        {
            strError = "";

            string strPath = ListViewUtil.GetItemText(item, 0);

            // ������¼·��
            string strServerName = "";
            string strPurePath = "";
            ParseRecPath(strPath,
                out strServerName,
                out strPurePath);

            // ���server url
            dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
            if (server == null)
            {
                strError = "��Ϊ '"+strServerName+"' �ķ������ڼ���������δ����...";
                return -1;
            }
            string strServerUrl = server.Url;

            this.Channel = this.Channels.GetChannel(strServerUrl);


            string[] paths = new string[1];
            paths[0] = strPurePath;
            DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

            long lRet = this.Channel.GetBrowseRecords(
                this.stop,
                paths,
                "id,cols",
                out searchresults,
                out strError);
            if (lRet == -1)
                return -1;

            if (searchresults == null || searchresults.Length == 0)
            {
                strError = "searchresults == null || searchresults.Length == 0";
                return -1;
            }

            for (int i = 0; i < searchresults[0].Cols.Length; i++)
            {
                ListViewUtil.ChangeItemText(item,
                    i + 1,
                    searchresults[0].Cols[i]);
            }

            return 0;
        }

        DtlpSearchForm GetDtlpSearchForm()
        {
            DtlpSearchForm dtlp_searchform = null;

            dtlp_searchform = this.MainForm.TopDtlpSearchForm;

            if (dtlp_searchform == null)
            {
                // �¿�һ��dtlp������
                FormWindowState old_state = this.WindowState;

                dtlp_searchform = new DtlpSearchForm();
                dtlp_searchform.MainForm = this.MainForm;
                dtlp_searchform.MdiParent = this.MainForm;
                dtlp_searchform.WindowState = FormWindowState.Minimized;
                dtlp_searchform.Show();

                this.WindowState = old_state;
                this.Activate();

                // ��Ҫ�ȴ���ʼ�������������
                dtlp_searchform.WaitLoadFinish();
            }

            return dtlp_searchform;
        }

        void dlg_GetDp2SearchParam(object sender, GetDp2SearchParamEventArgs e)
        {
            e.dp2Channels = this.Channels;
            e.MainForm = this.MainForm;
        }

        void dlg_GetDtlpSearchParam(object sender, GetDtlpSearchParamEventArgs e)
        {
            DtlpSearchForm dtlp_searchform = this.GetDtlpSearchForm();

            e.DtlpChannels = dtlp_searchform.DtlpChannels;
            e.DtlpChannel = dtlp_searchform.DtlpChannel;
        }

        // ׷�ӱ��浽���ݿ�
        void menu_saveToDatabase_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            string strPreferedMarcSyntax = "";
            if (this._linkMarcFile != null)
                strPreferedMarcSyntax = this._linkMarcFile.MarcSyntax;
            else
            {
                // �۲�Ҫ����ĵ�һ����¼��marc syntax
                nRet = GetOneRecordSyntax(0,
                    this.m_bInSearching,
                    out strPreferedMarcSyntax,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            string strLastSavePath = MainForm.LastSavePath;
            if (String.IsNullOrEmpty(strLastSavePath) == false)
            {
                string strOutputPath = "";
                nRet = MarcDetailForm.ChangePathToAppendStyle(strLastSavePath,
                    out strOutputPath,
                    out strError);
                if (nRet == -1)
                {
                    MainForm.LastSavePath = ""; // �����´μ�������
                    goto ERROR1;
                }
                strLastSavePath = strOutputPath;
            }


            SaveRecordDlg dlg = new SaveRecordDlg();
            GuiUtil.SetControlFont(dlg, this.Font);

            dlg.SaveToDbMode = true;    // ��������textbox���޸�·��

            dlg.MainForm = this.MainForm;
            dlg.GetDtlpSearchParam += new GetDtlpSearchParamEventHandle(dlg_GetDtlpSearchParam);
            dlg.GetDp2SearchParam += new GetDp2SearchParamEventHandle(dlg_GetDp2SearchParam);
            {
                dlg.RecPath = strLastSavePath;
                dlg.Text = "��ѡ��Ŀ�����ݿ�";
            }
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);
            if (dlg.DialogResult != DialogResult.OK)
                return;

            MainForm.LastSavePath = dlg.RecPath;

            string strProtocol = "";
            string strPath = "";
            nRet = Global.ParsePath(dlg.RecPath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;
#if NO
            string strDp2ServerName = "";
            string strPurePath = "";
            // ������¼·����
            // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
            dp2SearchForm.ParseRecPath(strPath,
                out strDp2ServerName,
                out strPurePath);

            string strDbName = GetDbName(strPurePath);
            string strSyntax = "";

            // ���һ�����ݿ������syntax
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = GetDbSyntax(this.stop,
                bUseNewChannel,
                strServerName,
                strServerUrl,
                strDbName,
                out strSyntax,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // ���һ�����ݿ������syntax
            // parameters:
            //      stop    ���!=null����ʾʹ�����stop�����Ѿ�OnStop +=
            //              ���==null����ʾ���Զ�ʹ��this.stop�����Զ�OnStop+=
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = this.GetDbSyntax(
                null,
                strServerName,
                strBiblioDbName,
                out strSyntax,
                out strError);
            if (nRet == -1)
            {
                strError = "��ȡ��Ŀ�� '" + strBiblioDbName + "�����ݸ�ʽʱ��������: " + strError;
                goto ERROR1;
            }
#endif

            // TODO: ��ֹ�ʺ����������ID

            this.stop.BeginLoop();

            this.EnableControlsInSearching(false);
            try
            {

                // dtlpЭ��ļ�¼����
                if (strProtocol.ToLower() == "dtlp")
                {
                    DtlpSearchForm dtlp_searchform = this.GetDtlpSearchForm();

                    if (dtlp_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�DTLP���������޷������¼";
                        goto ERROR1;
                    }
                    if (stop != null)
                        stop.SetProgressRange(0, this.listView_browse.SelectedItems.Count);

                    for (int i = 0; i < this.listView_browse.SelectedItems.Count; i++)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                goto ERROR1;
                            }
                            stop.SetProgressValue(i);
                        }

                        ListViewItem item = this.listView_browse.SelectedItems[i];
                        string strSourcePath = item.Text;

                        string strRecord = "";
                        string strOutputPath = "";
                        string strOutStyle = "";
                        byte[] baTimestamp = null;
                        DigitalPlatform.Z3950.Record record = null;
                        Encoding currrentEncoding;
                        string strXmlFragment = "";
                        nRet = InternalGetOneRecord(
                            false,
    "marc",
    strSourcePath,
    "current",
                    "",
    out strRecord,
    out strXmlFragment,
    out strOutputPath,
    out strOutStyle,
    out baTimestamp,
    out record,
    out currrentEncoding,
    out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        string strMarcSyntax = "";
                        if (record.m_strSyntaxOID == "1.2.840.10003.5.1")
                            strMarcSyntax = "unimarc";
                        if (record.m_strSyntaxOID == "1.2.840.10003.5.10")
                            strMarcSyntax = "usmarc";
                        if (string.IsNullOrEmpty(strMarcSyntax) == true)
                        {
                            strError = "��¼ '"+strSourcePath+"' ����MARC��ʽ���޷����浽DTLP������";
                            goto ERROR1;
                        }

                        byte[] baOutputTimestamp = null;
                        nRet = dtlp_searchform.SaveMarcRecord(
                            strPath,
                            strRecord,
                            baTimestamp,
                            out strOutputPath,
                            out baOutputTimestamp,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }

                    MessageBox.Show(this, "����ɹ�");
                    return;
                }
                else if (strProtocol.ToLower() == "dp2library")
                {
                    if (stop != null)
                        stop.SetProgressRange(0, this.listView_browse.SelectedItems.Count);

                    for (int i = 0; i < this.listView_browse.SelectedItems.Count; i++)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                goto ERROR1;
                            }

                            stop.SetProgressValue(i);
                        }

                        ListViewItem item = this.listView_browse.SelectedItems[i];
                        string strSourcePath = item.Text;

                        string strRecord = "";
                        string strOutputPath = "";
                        string strOutStyle = "";
                        byte[] baTimestamp = null;
                        DigitalPlatform.Z3950.Record record = null;
                        Encoding currrentEncoding;
                        string strXmlFragment = "";
                        nRet = InternalGetOneRecord(
                            false,
    "marc",
    strSourcePath,
    "current",
                    "",
    out strRecord,
    out strXmlFragment,
    out strOutputPath,
    out strOutStyle,
    out baTimestamp,
    out record,
    out currrentEncoding,
    out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        string strMarcSyntax = MarcDetailForm.GetMarcSyntax(record.m_strSyntaxOID);
                        if (string.IsNullOrEmpty(strMarcSyntax) == true)
                            strMarcSyntax = "unimarc";

                        byte[] baOutputTimestamp = null;
                        string strComment = "copy from " + strSourcePath;
                        // return:
                        //      -2  timestamp mismatch
                        //      -1  error
                        //      0   succeed
                        nRet = this.SaveMarcRecord(
                            false,
                            strPath,
                            strRecord,
                            strMarcSyntax,
                            baTimestamp,
                            strXmlFragment,
                            strComment,
                            out strOutputPath,
                            out baOutputTimestamp,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                    }
                    MessageBox.Show(this, "����ɹ�");
                    return;
                }
                else if (strProtocol.ToLower() == "z3950")
                {
                    strError = "Ŀǰ�ݲ�֧��Z39.50Э��ı������";
                    goto ERROR1;
                }
                else
                {
                    strError = "�޷�ʶ���Э���� '" + strProtocol + "'";
                    goto ERROR1;
                }

            }
            finally
            {
                this.stop.EndLoop();
                this.stop.HideProgress();

                this.EnableControlsInSearching(true);
            }

            // return 0;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ���浽��¼·���ļ�
        void menu_saveToRecordPathFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ����ļ�¼·���ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = this.ExportRecPathFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.ExportRecPathFilename = dlg.FileName;

            bool bAppend = true;

            if (File.Exists(this.ExportRecPathFilename) == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "��¼·���ļ� '" + this.ExportRecPathFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel ��������)",
                    "dp2SearchForm",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Cancel)
                    return;
                if (result == DialogResult.No)
                    bAppend = false;
                else if (result == DialogResult.Yes)
                    bAppend = true;
                else
                {
                    Debug.Assert(false, "");
                }
            }
            else
                bAppend = false;

            // �����ļ�
            StreamWriter sw = new StreamWriter(this.ExportRecPathFilename,
                bAppend,	// append
                System.Text.Encoding.UTF8);
            try
            {
                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                for (int i = 0; i < this.listView_browse.SelectedItems.Count; i++)
                {
                    ListViewItem item = this.listView_browse.SelectedItems[i];

                    // ������¼·��
                    string strServerName = "";
                    string strPurePath = "";
                    ParseRecPath(item.Text,
                        out strServerName,
                        out strPurePath);
                    // ���server url
                    dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
                    if (server == null)
                    {
                        strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                        goto ERROR1;
                    }

                    string strPath = strPurePath + "@" + server.Url;

                    sw.WriteLine(strPath);
                }

                this.Cursor = oldCursor;
            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }

            string strExportStyle = "����";
            if (bAppend == true)
                strExportStyle = "׷��";

            this.MainForm.StatusBarMessage = "��Ŀ��¼·�� " + this.listView_browse.SelectedItems.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportRecPathFilename;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

#if NO
        // ˢ����ѡ��������
        void menu_refreshSelectedItems_Click(object sender, EventArgs e)
        {
            string strError = "";

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ˢ������� ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);

            try
            {
                // �����������û����ģ������Ҫ������е������־
                stop.SetProgressRange(0, this.listView_browse.SelectedItems.Count);

                for (int i=0; i<this.listView_browse.SelectedItems.Count; i++)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            MessageBox.Show(this, "�û��ж�");
                            return;
                        }
                    }

                    ListViewItem item = this.listView_browse.SelectedItems[i];

                    string strRecPath = ListViewUtil.GetItemText(item, 0);

                    // TODO: ���·������ȷ�ԣ�������ݿ��Ƿ�Ϊ��Ŀ��֮һ

                    int nRet = RefreshOneLine(item,
            out strError);
                    if (nRet == -1)
                    {
                        DialogResult result = MessageBox.Show(this,
    "����������ʱ����: " + strError + "��\r\n\r\n�Ƿ������ȡ�������? (Yes ������ȡ��No ����ˢ��)",
    "dp2SearchForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.No)
                            goto ERROR1;
                    }

                    stop.SetProgressValue(i + 1);
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControlsInSearching(true);
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif
        // ˢ����ѡ�������С�Ҳ�������´����ݿ���װ�������
        void menu_refreshSelectedItems_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_browse.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫˢ�µ������";
                goto ERROR1;
            }

            int nChangedCount = 0;
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_browse.SelectedItems)
            {
                if (string.IsNullOrEmpty(item.Text) == true)
                    continue;
                items.Add(item);

                if (IsItemChanged(item) == true)
                    nChangedCount++;
            }
            if (nChangedCount > 0)
            {
                DialogResult result = MessageBox.Show(this,
    "Ҫˢ�µ� " + this.listView_browse.SelectedItems.Count.ToString() + " ���������� " + nChangedCount.ToString() + " ���޸ĺ���δ���档���ˢ�����ǣ��޸����ݻᶪʧ��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
    "dp2SearchForm",
    MessageBoxButtons.OKCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return;
            } 
            
            nRet = RefreshListViewLines(items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            DoViewComment(false);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �۲�һ�������Ƿ����ڴ����޸Ĺ�
        bool IsItemChanged(ListViewItem item)
        {
            string strRecPath = item.Text;
            if (string.IsNullOrEmpty(strRecPath) == true)
                return false;

            BiblioInfo info = (BiblioInfo)this.m_biblioTable[strRecPath];
            if (info == null)
                return false;

            if (string.IsNullOrEmpty(info.NewXml) == false)
                return true;

            return false;
        }

        // ���һ��������޸���Ϣ
        // parameters:
        //      bClearBiblioInfo    �Ƿ�˳���������� BiblioInfo ��Ϣ
        void ClearOneChange(ListViewItem item,
            bool bClearBiblioInfo = false)
        {
            string strRecPath = item.Text;
            if (string.IsNullOrEmpty(strRecPath) == true)
                return;

            BiblioInfo info = (BiblioInfo)this.m_biblioTable[strRecPath];
            if (info == null)
                return;

            if (String.IsNullOrEmpty(info.NewXml) == false)
            {
                info.NewXml = "";

                item.BackColor = SystemColors.Window;
                item.ForeColor = SystemColors.WindowText;

                this.m_nChangedCount--;
                Debug.Assert(this.m_nChangedCount >= 0, "");
            }

            if (bClearBiblioInfo == true)
                this.m_biblioTable.Remove(strRecPath);
        }

        public int RefreshListViewLines(List<ListViewItem> items_param,
    out string strError)
        {
            strError = "";

            if (items_param.Count == 0)
                return 0;

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ˢ������� ...");
            stop.BeginLoop();

            this.EnableControls(false);
            try
            {
                List<ListViewItem> items = new List<ListViewItem>();
                List<string> recpaths = new List<string>();
                foreach (ListViewItem item in items_param)
                {
                    if (string.IsNullOrEmpty(item.Text) == true)
                        continue;
                    items.Add(item);
                    recpaths.Add(item.Text);

                    ClearOneChange(item, true);
                }

                if (stop != null)
                    stop.SetProgressRange(0, items.Count);

                BrowseLoader loader = new BrowseLoader();
                loader.Channels = this.Channels;
                loader.Servers = this.dp2ResTree1.Servers;
                loader.Stop = stop;
                loader.RecPaths = recpaths;

                int i = 0;
                foreach (DigitalPlatform.CirculationClient.localhost.Record record in loader)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null
                        && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }

                    Debug.Assert(record.Path == recpaths[i], "");

                    if (stop != null)
                    {
                        stop.SetMessage("����ˢ������� " + record.Path + " ...");
                        stop.SetProgressValue(i);
                    }

                    ListViewItem item = items[i];
                    if (record.Cols == null)
                    {
                        int c = 0;
                        foreach (ListViewItem.ListViewSubItem subitem in item.SubItems)
                        {
                            if (c != 0)
                                subitem.Text = "";
                            c++;
                        }
                    }
                    else
                    {
                        for (int c = 0; c < record.Cols.Length; c++)
                        {
                            ListViewUtil.ChangeItemText(item,
                            c + 1,
                            record.Cols[c]);
                        }

                        // TODO: �Ƿ�������µ�������?
                    }


                    i++;
                }

                return 0;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControls(true);
            }
        }



        // �Ӵ�����������ѡ�������
        void menu_removeSelectedItems_Click(object sender, EventArgs e)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            for (int i = this.listView_browse.SelectedIndices.Count - 1; i >= 0; i--)
            {
                this.listView_browse.Items.RemoveAt(this.listView_browse.SelectedIndices[i]);
            }

            this.Cursor = oldCursor;
        }

        void menu_copyToClipboard_Click(object sender, EventArgs e)
        {
            Global.CopyLinesToClipboard(this, 
                "dp2SearchForm",
                this.listView_browse,
                false);
        }

        void menu_cutToClipboard_Click(object sender, EventArgs e)
        {
            Global.CopyLinesToClipboard(this,
                "dp2SearchForm",
                this.listView_browse, 
                true);
        }

        void menu_pasteFromClipboard_insertBefore_Click(object sender, EventArgs e)
        {
            Global.PasteLinesFromClipboard(this,
                "dp2SearchForm,AmazonSearchForm",
                this.listView_browse,
                true);

            ConvertPastedLines();
        }

        void menu_pasteFromClipboard_insertAfter_Click(object sender, EventArgs e)
        {
            Global.PasteLinesFromClipboard(this,
                "dp2SearchForm,AmazonSearchForm",
                this.listView_browse,
                false);

            ConvertPastedLines();
        }


        // ���ո� paste ��������У����д����Ա�ﵽ����������ˮƽ
        // TODO: ��ʵ��ǰ����б�����Ӧ������ UNIMARC �� USMARC �����¼�����
        void ConvertPastedLines()
        {
            string strError = "";
            int nRet = 0;

            AmazonSearchForm amazon_searchform = this.MainForm.GetAmazonSearchForm();

            foreach (ListViewItem item in this.listView_browse.SelectedItems)
            {
                if (item.Tag is AmazonSearchForm.ItemInfo)
                {
                    AmazonSearchForm.ItemInfo origin = (AmazonSearchForm.ItemInfo)item.Tag;
                    string strRecPath = ListViewUtil.GetItemText(item, 0);

                    string strMARC = "";
                    nRet = amazon_searchform.GetItemInfo(origin,
            out strMARC,
            out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    string strXml = "";
                    nRet = MarcUtil.Marc2Xml(strMARC,
    "unimarc",
    out strXml,
    out strError);
                    if (nRet == -1)
                        goto ERROR1;


                    strRecPath = strRecPath + " @mem";
                    BiblioInfo info = new BiblioInfo();
                    info.RecPath = strRecPath;
                    this.m_biblioTable[strRecPath] = info;

                    info.OldXml = strXml;
                    info.Timestamp = null;
                    info.RecPath = strRecPath;

                    string strSytaxOID = "1.2.840.10003.5.1";

                    string strBrowseText = "";
                    nRet = BuildMarcBrowseText(
                        strSytaxOID,
                        strMARC,
                        out strBrowseText,
                        out strError);

                    string[] cols = strBrowseText.Split(new char[] { '\t' });

                    // �޸������
                    ChangeCols(
                        item,
                        strRecPath,
                        cols);

                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_loadToOpenedDetailForm_Click(object sender, EventArgs e)
        {
            int nIndex = -1;
            if (this.listView_browse.SelectedIndices.Count > 0)
                nIndex = this.listView_browse.SelectedIndices[0];
            else
            {
                if (this.listView_browse.FocusedItem == null)
                    return;
                nIndex = this.listView_browse.Items.IndexOf(this.listView_browse.FocusedItem);
            }
            LoadDetail(nIndex,
        false);
        }


        void menu_loadToNewDetailForm_Click(object sender, EventArgs e)
        {
            int nIndex = -1;
            if (this.listView_browse.SelectedIndices.Count > 0)
                nIndex = this.listView_browse.SelectedIndices[0];
            else
            {
                if (this.listView_browse.FocusedItem == null)
                    return;
                nIndex = this.listView_browse.Items.IndexOf(this.listView_browse.FocusedItem);
            }
            LoadDetail(nIndex,
        true);
        }

        void menuItem_selectAll_Click(object sender,
            EventArgs e)
        {
            this.listView_browse.BeginUpdate();

            for (int i = 0; i < this.listView_browse.Items.Count; i++)
            {
                this.listView_browse.Items[i].Selected = true;
            }

            this.listView_browse.EndUpdate();

        }

        void menuItem_saveOriginRecordToWorksheet_Click(object sender, EventArgs e)
        {
            this.SaveOriginRecordToWorksheet();
        }

        void menuItem_saveOriginRecordToIso2709_Click(object sender, EventArgs e)
        {
            this.SaveOriginRecordToIso2709();
        }

        private void button_searchSimple_Click(object sender, EventArgs e)
        {
#if NO
            if (this.dp2ResTree1.CheckBoxes == true)
                DoCheckedSimpleSearch();
            else
                DoSimpleSearch();
#endif

            DoSearch("simple");
        }

        private void comboBox_matchStyle_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_simple_matchStyle.Invalidate();
        }

        private void comboBox_matchStyle_TextChanged(object sender, EventArgs e)
        {
            if (this.comboBox_simple_matchStyle.Text == "��ֵ")
            {
                this.textBox_simple_queryWord.Text = "";
                this.textBox_simple_queryWord.Enabled = false;
            }
            else
            {
                this.textBox_simple_queryWord.Enabled = true;
            }
        }

        public bool SetLayout(string strLayoutName)
        {
            if (strLayoutName != "��Դ�����"
                && strLayoutName != "��������(����)"
                && strLayoutName != "��������(����)")
                return false;

            // ����ǰ�ָ��򵥼��������߶���
            if (this.tabControl_query.SelectedTab == this.tabPage_logic)
                this.tabControl_query.SelectedTab = this.tabPage_simple;

            this.splitContainer_main.Panel1.Controls.RemoveAt(0);
            this.splitContainer_main.Panel2.Controls.RemoveAt(0);

            this.splitContainer_up.Panel1.Controls.RemoveAt(0);
            this.splitContainer_up.Panel2.Controls.RemoveAt(0);


            if (strLayoutName == "��Դ�����")
            {
                this.splitContainer_main.Orientation = Orientation.Vertical;
                this.splitContainer_main.Panel1.Controls.Add(this.panel_resTree);

                this.splitContainer_up.Orientation = Orientation.Horizontal;
                this.splitContainer_up.Panel1.Controls.Add(this.splitContainer_queryAndResultInfo);
                this.splitContainer_up.Panel2.Controls.Add(this.listView_browse);

                this.splitContainer_main.Panel2.Controls.Add(this.splitContainer_up);
            }
            else if (strLayoutName == "��������(����)"
                || strLayoutName == "��������(����)")
            {

                this.splitContainer_up.Panel1.Controls.Add(this.panel_resTree);
                this.splitContainer_up.Panel2.Controls.Add(this.splitContainer_queryAndResultInfo);

                this.splitContainer_main.Panel1.Controls.Add(this.splitContainer_up);

                this.splitContainer_main.Panel2.Controls.Add(this.listView_browse);
            }
            else 
            {
                Debug.Assert(false, "");
            }

            if (strLayoutName == "��������(����)")
            {
                this.splitContainer_main.Orientation = Orientation.Horizontal;
                this.splitContainer_up.Orientation = Orientation.Vertical;
                //this.splitContainer_queryAndResultInfo.Orientation = Orientation.Horizontal;
            }
            else if (strLayoutName == "��������(����)")
            {
                this.splitContainer_main.Orientation = Orientation.Vertical;
                this.splitContainer_up.Orientation = Orientation.Horizontal;
                //this.splitContainer_queryAndResultInfo.Orientation = Orientation.Horizontal;
            }


            return true;
        }

        public void Reload()
        {
            if (this.dp2ResTree1.Focused == true)
            {
                this.dp2ResTree1.Refresh(dp2ResTree.RefreshStyle.All);
                // this.dp2ResTree1.Focus();
            }
            else if (this.listView_browse.Focused == true)
            {
                // TODO: ����װ��ȫ��
            }
        }

        // ���һ�����ݿ������
        // ���ܻ��׳��쳣
        public NormalDbProperty GetDbProperty(string strServerName,
            string strDbName)
        {
            return this.dp2ResTree1.GetDbProperty(strServerName,
                strDbName);
        }

        private void dp2QueryControl1_GetList(object sender, GetListEventArgs e)
        {
            // �г�������
            if (string.IsNullOrEmpty(e.Path) == true)
            {
                e.Values = this.dp2ResTree1.GetServerNames();
                return;
            }

            try
            {

                string[] parts = e.Path.Split(new char[] { '/' });
                if (parts.Length == 1)
                {
                    e.Values = this.dp2ResTree1.GetDbNames(parts[0]);
                }
                else if (parts.Length == 2)
                {
                    e.Values = this.dp2ResTree1.GetFromNames(parts[0], parts[1]);
                }
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(ex.Message) == true)
                    e.ErrorInfo = "error";
                else
                    e.ErrorInfo = ex.Message;
                return;
            }
        }

        private void tabControl_query_Selected(object sender, TabControlEventArgs e)
        {
            string strLayoutName = this.LayoutName;

            if (this.tabControl_query.SelectedTab == this.tabPage_logic)
            {
                this.dp2ResTree1.Visible = false;

                if (strLayoutName == "��Դ�����")
                    this.splitContainer_main.Panel1Collapsed = true;
                else if (strLayoutName == "��������(����)")
                    this.splitContainer_up.Panel1Collapsed = true;
                else if (strLayoutName == "��������(����)")
                    this.splitContainer_up.Panel1Collapsed = true;
            }
            else
            {
                this.dp2ResTree1.Visible = true;
                if (strLayoutName == "��Դ�����")
                    this.splitContainer_main.Panel1Collapsed = false;
                else if (strLayoutName == "��������(����)")
                    this.splitContainer_up.Panel1Collapsed = false;
                else if (strLayoutName == "��������(����)")
                    this.splitContainer_up.Panel1Collapsed = false;
            }
        }

        private void dp2QueryControl1_ViewXml(object sender, EventArgs e)
        {
            string strError = "";
            List<QueryItem> items = null;

            int nRet = this.dp2QueryControl1.BuildQueryXml(
            this.SearchMaxCount,
            "zh",
            out items,
            out strError);
            if (nRet == -1)
            {
                strError = "�ڴ���XML����ʽ�Ĺ����г���: " + strError;
                goto ERROR1;
            }

            string strFileName = this.MainForm.DataDir + "\\~logic_queries.txt";
            using (StreamWriter sw = new StreamWriter(strFileName,
                false,
                Encoding.UTF8))
            {
                for (int j = 0; j < items.Count; j++)
                {

                    QueryItem item = items[j];
                    sw.WriteLine("---\r\n��Է����� " + item.ServerName + ":");
                    string strXml = "";
                    nRet = DomUtil.GetIndentXml(item.QueryXml, out strXml, out strError);
                    if (nRet == -1)
                    {
                        strError = "XML����ʽ '"+item.QueryXml+"' XML��ʽ�д�: " + strError;
                        goto ERROR1;
                    }
                    sw.WriteLine(strXml);
                }
            }

            System.Diagnostics.Process.Start("notepad.exe", strFileName);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        public string LayoutName
        {
            get
            {
                return MainForm.AppInfo.GetString(
    "dp2searchform",
    "layout",
    "��Դ�����");
            }
        }

        // �Ӷ�����ݿ�������ʱ���Ƿ�Ҫ
        public bool SelectAllDb
        {
            get
            {
                return MainForm.AppInfo.GetBoolean(
    "dp2searchform",
    "sekect_all_db",
    true);
            }
            set
            {
                MainForm.AppInfo.SetBoolean(
    "dp2searchform",
    "sekect_all_db",
    value);
            }
        }

        private void label_simple_queryWord_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripMenuItem menuItem = null;

            menuItem = new ToolStripMenuItem("RFC1123ʱ��ֵ...");
            menuItem.Click += new System.EventHandler(this.ToolStripMenuItem_rfc1123Single_Click);
            contextMenu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("uʱ��ֵ...");
            menuItem.Click += new System.EventHandler(this.ToolStripMenuItem_uSingle_Click);
            contextMenu.Items.Add(menuItem);


            // ---
            ToolStripSeparator sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            menuItem = new ToolStripMenuItem("RFC1123ʱ��ֵ��Χ...");
            menuItem.Click += new System.EventHandler(this.ToolStripMenuItem_rfc1123Range_Click);
            contextMenu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("uʱ��ֵ��Χ...");
            menuItem.Click += new System.EventHandler(this.ToolStripMenuItem_uRange_Click);
            contextMenu.Items.Add(menuItem);

            contextMenu.Show(this.label_simple_queryWord, e.Location);
        }

        private void ToolStripMenuItem_rfc1123Single_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            GuiUtil.SetControlFont(dlg, this.Font);
            dlg.RangeMode = false;
            try
            {
                dlg.Rfc1123String = this.textBox_simple_queryWord.Text;
            }
            catch
            {
                this.textBox_simple_queryWord.Text = "";
            }
            this.MainForm.AppInfo.LinkFormState(dlg, "dp2searchform_gettimedialog_single");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_simple_queryWord.Text = dlg.Rfc1123String;
        }

        private void ToolStripMenuItem_uSingle_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            GuiUtil.SetControlFont(dlg, this.Font);
            dlg.RangeMode = false;
            try
            {
                dlg.uString = this.textBox_simple_queryWord.Text;
            }
            catch
            {
                this.textBox_simple_queryWord.Text = "";
            }

            this.MainForm.AppInfo.LinkFormState(dlg, "dp2searchform_gettimedialog_single");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_simple_queryWord.Text = dlg.uString;

        }

        private void ToolStripMenuItem_rfc1123Range_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            GuiUtil.SetControlFont(dlg, this.Font);
            dlg.RangeMode = true;
            // �ָ�Ϊ�����ַ���
            try
            {
                dlg.Rfc1123String = this.textBox_simple_queryWord.Text;
            }
            catch
            {
                this.textBox_simple_queryWord.Text = "";
            }
            this.MainForm.AppInfo.LinkFormState(dlg, "dp2searchform_gettimedialog_range");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_simple_queryWord.Text = dlg.Rfc1123String;

        }

        private void ToolStripMenuItem_uRange_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            GuiUtil.SetControlFont(dlg, this.Font);
            dlg.RangeMode = true;
            try
            {
                dlg.uString = this.textBox_simple_queryWord.Text;
            }
            catch
            {
                this.textBox_simple_queryWord.Text = "";
            }

            this.MainForm.AppInfo.LinkFormState(dlg, "dp2searchform_gettimedialog_range");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_simple_queryWord.Text = dlg.uString;
        }

        bool InSearching
        {
            get
            {
                return m_bInSearching;
            }
        }

        int GetBiblioInfo(
    bool bCheckSearching,
    ListViewItem item,
    out BiblioInfo info,
    out string strError)
        {
            strError = "";
            info = null;

            if (this.m_biblioTable == null)
                return 0;

            string strRecPath = item.Text;
            if (string.IsNullOrEmpty(strRecPath) == true)
                return 0;


            // �洢�������Ŀ��¼ XML
            info = (BiblioInfo)this.m_biblioTable[strRecPath];


            if (info == null || string.IsNullOrEmpty(info.OldXml) == true)
            {
                if (bCheckSearching == true && this._linkMarcFile == null)
                {
                    if (this.InSearching == true)
                        return 0;
                }

                // ������¼·��
                string strServerName = "";
                string strPurePath = "";
                ParseRecPath(strRecPath,
                    out strServerName,
                    out strPurePath);
                // ���server url
                dp2Server server = this.dp2ResTree1.Servers.GetServerByName(strServerName);
                if (server == null)
                {
                    strError = "��Ϊ '" + strServerName + "' �ķ������ڼ���������δ����...";
                    return -1;
                }
                string strServerUrl = server.Url;

                this.Channel = this.Channels.GetChannel(strServerUrl);

                string[] results = null;
                byte[] baTimestamp = null;
                // �����Ŀ��¼
                long lRet = Channel.GetBiblioInfos(
                    stop,
                    strPurePath,
                    "",
                    new string[] { "xml" },   // formats
                    out results,
                    out baTimestamp,
                    out strError);
                if (lRet == 0)
                    return -1;  // �Ƿ��趨Ϊ����״̬?
                if (lRet == -1)
                    return -1;

                if (results == null || results.Length == 0)
                {
                    strError = "results error";
                    return -1;
                }

                string strXml = results[0];

                // �ͺ󴴽��¶��󣬱����� hashtable �д���һ����δ��ʼ���Ķ��󣬶��������߳�����ʹ����
                if (info == null)
                {
                    info = new BiblioInfo();
                    info.RecPath = strRecPath;
                    this.m_biblioTable[strRecPath] = info;
                }

                info.OldXml = strXml;
                info.Timestamp = baTimestamp;
                info.RecPath = strRecPath;
            }

            return 1;
        }

        int m_nInViewing = 0;

        void DoViewComment(bool bOpenWindow)
        {
            m_nInViewing++;
            try
            {
                _doViewComment(bOpenWindow);
            }
            finally
            {
                m_nInViewing--;
            }
        }


        void _doViewComment(bool bOpenWindow)
        {
            string strError = "";
            string strMarcHtml = "";
            // string strXml = "";

            // �Ż���������ν�ؽ��з���������
            if (bOpenWindow == false)
            {
                if (this.MainForm.PanelFixedVisible == false
                    && (m_commentViewer == null || m_commentViewer.Visible == false))
                    return;
                // 2013/3/7
                if (this.MainForm.CanDisplayItemProperty() == false)
                    return;
            }

            if (this.m_biblioTable == null
                || this.listView_browse.SelectedItems.Count != 1)
            {
                if (this.m_commentViewer != null)
                    this.m_commentViewer.Clear();
                return;
            }

            ListViewItem item = this.listView_browse.SelectedItems[0];
#if NO
            string strRecPath = this.listView_records.SelectedItems[0].Text;
            if (string.IsNullOrEmpty(strRecPath) == true)
            {
                if (this.m_commentViewer != null)
                    this.m_commentViewer.Clear();
                return;
            }
#endif

            // BiblioInfo info = (BiblioInfo)this.m_biblioTable[strRecPath];
            BiblioInfo info = null;
            int nRet = GetBiblioInfo(
                true,
                item,
                out info,
                out strError);
            if (info == null)
            {
                if (this.m_commentViewer != null)
                    this.m_commentViewer.Clear();
                return;
            }

            string strXml1 = "";
            string strHtml2 = "";
            string strXml2 = "";
            string strBiblioHtml = "";

            if (nRet == -1)
            {
                strHtml2 = HttpUtility.HtmlEncode(strError);
            }
            else
            {
                nRet = GetXmlHtml(info,
                    out strXml1,
                    out strXml2,
                    out strHtml2,
                    out strBiblioHtml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            strMarcHtml = "<html>" +
    GetHeadString() +
    "<body>" +
    strHtml2 +
    "</body></html>";




            bool bNew = false;
            if (this.m_commentViewer == null
                || (bOpenWindow == true && this.m_commentViewer.Visible == false))
            {
                m_commentViewer = new BiblioViewerForm();
                GuiUtil.SetControlFont(m_commentViewer, this.Font, false);
                bNew = true;
            }

            m_commentViewer.MainForm = this.MainForm;  // �����ǵ�һ��

            if (bNew == true)
            {
                // m_commentViewer.InitialWebBrowser();
            }

            m_commentViewer.Text = "MARC���� '" + info.RecPath + "'";
            m_commentViewer.HtmlString = strBiblioHtml;
            m_commentViewer.MarcString = strMarcHtml;
            m_commentViewer.XmlString = MergeXml(strXml1, strXml2);
            m_commentViewer.FormClosed -= new FormClosedEventHandler(marc_viewer_FormClosed);
            m_commentViewer.FormClosed += new FormClosedEventHandler(marc_viewer_FormClosed);
            // this.MainForm.AppInfo.LinkFormState(m_viewer, "comment_viewer_state");
            // m_viewer.ShowDialog(this);
            // this.MainForm.AppInfo.UnlinkFormState(m_viewer);
            if (bOpenWindow == true)
            {
                if (m_commentViewer.Visible == false)
                {
                    this.MainForm.AppInfo.LinkFormState(m_commentViewer, "marc_viewer_state");
                    m_commentViewer.Show(this);
                    m_commentViewer.Activate();

                    this.MainForm.CurrentPropertyControl = null;
                }
                else
                {
                    if (m_commentViewer.WindowState == FormWindowState.Minimized)
                        m_commentViewer.WindowState = FormWindowState.Normal;
                    m_commentViewer.Activate();
                }
            }
            else
            {
                if (m_commentViewer.Visible == true)
                {

                }
                else
                {
                    if (this.MainForm.CurrentPropertyControl != m_commentViewer.MainControl)
                        m_commentViewer.DoDock(false); // �����Զ���ʾFixedPanel
                }
            }
            return;
        ERROR1:
            MessageBox.Show(this, "DoViewComment() ����: " + strError);
        }
        static string MergeXml(string strXml1,
    string strXml2)
        {
            if (string.IsNullOrEmpty(strXml1) == true)
                return strXml2;
            if (string.IsNullOrEmpty(strXml2) == true)
                return strXml1;

            return strXml1; // ��ʱ����
        }

        int GetXmlHtml(BiblioInfo info,
            out string strXml1,
            out string strXml2,
            out string strMarcHtml,
            out string strBiblioHtml,
            out string strError)
        {
            strError = "";
            strXml1 = "";
            strXml2 = "";
            strMarcHtml = "";
            strBiblioHtml = "";
            int nRet = 0;

            strXml1 = info.OldXml;
            strXml2 = info.NewXml;

            string strMarcSyntax = "";

            string strOldMARC = "";
            string strOldFragmentXml = "";
            if (string.IsNullOrEmpty(strXml1) == false)
            {
                string strOutMarcSyntax = "";
                // ��XML��ʽת��ΪMARC��ʽ
                // �Զ������ݼ�¼�л��MARC�﷨
                nRet = MarcUtil.Xml2Marc(strXml1,
                    MarcUtil.Xml2MarcStyle.Warning | MarcUtil.Xml2MarcStyle.OutputFragmentXml,
                    "",
                    out strOutMarcSyntax,
                    out strOldMARC,
                    out strOldFragmentXml,
                    out strError);
                if (nRet == -1)
                {
                    strError = "XMLת����MARC��¼ʱ����: " + strError;
                    return -1;
                }
                strMarcSyntax = strOutMarcSyntax;
            }

            string strNewMARC = "";
            string strNewFragmentXml = "";
            if (string.IsNullOrEmpty(strXml2) == false)
            {
                string strOutMarcSyntax = "";
                // ��XML��ʽת��ΪMARC��ʽ
                // �Զ������ݼ�¼�л��MARC�﷨
                nRet = MarcUtil.Xml2Marc(strXml2,
                    MarcUtil.Xml2MarcStyle.Warning | MarcUtil.Xml2MarcStyle.OutputFragmentXml,
                    "",
                    out strOutMarcSyntax,
                    out strNewMARC,
                    out strNewFragmentXml,
                    out strError);
                if (nRet == -1)
                {
                    strError = "XMLת����MARC��¼ʱ����: " + strError;
                    return -1;
                }
                strMarcSyntax = strOutMarcSyntax;

            }

            string strMARC = "";
            if (string.IsNullOrEmpty(strOldMARC) == false
                && string.IsNullOrEmpty(strNewMARC) == false)
            {
                // ����չʾ���� MARC ��¼����� HTML �ַ���
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = MarcDiff.DiffHtml(
                    strOldMARC,
                    strOldFragmentXml,
                    "",
                    strNewMARC,
                    strNewFragmentXml,
                    "",
                    out strMarcHtml,
                    out strError);
                if (nRet == -1)
                    return -1;
                strMARC = strNewMARC;
            }
            else if (string.IsNullOrEmpty(strOldMARC) == false
    && string.IsNullOrEmpty(strNewMARC) == true)
            {
                strMarcHtml = MarcUtil.GetHtmlOfMarc(strOldMARC,
                    strOldFragmentXml,
                    "",
                    false);
                strMARC = strOldMARC;
            }
            else if (string.IsNullOrEmpty(strOldMARC) == true
                && string.IsNullOrEmpty(strNewMARC) == false)
            {
                strMarcHtml = MarcUtil.GetHtmlOfMarc(strNewMARC,
                    strNewFragmentXml,
                    "",
                    false);
                strMARC = strNewMARC;
            }

            // return:
            //      -1  ����
            //      0   .fltx �ļ�û���ҵ�
            //      1   �ɹ�
            nRet = this.MainForm.BuildMarcHtmlText(
                MarcDetailForm.GetSyntaxOID(strMarcSyntax),
                strMARC,
                out strBiblioHtml,
                out strError);
            if (nRet == -1)
                strBiblioHtml = strError.Replace("\r\n", "<br/>");

            return 0;
        }

        void marc_viewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_commentViewer != null)
            {
                if (this.MainForm != null && this.MainForm.AppInfo != null)
                    this.MainForm.AppInfo.UnlinkFormState(m_commentViewer);
                this.m_commentViewer = null;
            }
        }

        string GetHeadString(bool bAjax = true)
        {
            string strCssFilePath = PathUtil.MergePath(this.MainForm.DataDir, "operloghtml.css");

            if (bAjax == true)
                return
                    "<head>" +
                    "<LINK href='" + strCssFilePath + "' type='text/css' rel='stylesheet'>" +
                    "<link href=\"%mappeddir%/jquery-ui-1.8.7/css/jquery-ui-1.8.7.css\" rel=\"stylesheet\" type=\"text/css\" />" +
                    "<script type=\"text/javascript\" src=\"%mappeddir%/jquery-ui-1.8.7/js/jquery-1.4.4.min.js\"></script>" +
                    "<script type=\"text/javascript\" src=\"%mappeddir%/jquery-ui-1.8.7/js/jquery-ui-1.8.7.min.js\"></script>" +
                    //"<script type='text/javascript' src='%datadir%/jquery.js'></script>" +
                    "<script type='text/javascript' charset='UTF-8' src='%datadir%\\getsummary.js" + "'></script>" +
                    "</head>";
            return
    "<head>" +
    "<LINK href='" + strCssFilePath + "' type='text/css' rel='stylesheet'>" +
    "</head>";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.listView_browse.ForceUpdate();
        }


    }
    // Ϊһ�д洢����Ŀ��Ϣ
    public class BiblioInfo
    {
        public string RecPath = "";
        public string OldXml = "";
        public byte[] Timestamp = null; // �������ݿ��ʱ���
        public string NewXml = "";
        public long NewVersion = 0; // NewXml�޸ĺ�İ汾�� datetime ticks
    }

    public class LoaderItem
    {
        public BiblioInfo BiblioInfo = null;
        public ListViewItem ListViewItem = null;

        public LoaderItem(BiblioInfo info, ListViewItem item)
        {
            this.BiblioInfo = info;
            this.ListViewItem = item;
        }
    }

    /// <summary>
    /// ���� ListViewItem ��������Ŀ��¼��Ϣ��ö����
    /// �������û������
    /// </summary>
    public class ListViewBiblioLoader : IEnumerable
    {
        public List<ListViewItem> Items
        {
            get;
            set;
        }

        public Hashtable CacheTable
        {
            get;
            set;
        }

        BiblioLoader m_loader = null;

        public ListViewBiblioLoader(LibraryChannelCollection channels,
            dp2ServerCollection servers,
            Stop stop,
            List<ListViewItem> items,
            Hashtable cacheTable)
        {
            m_loader = new BiblioLoader();
            m_loader.Channels = channels;
            m_loader.Servers = servers;
            m_loader.Stop = stop;
            m_loader.Format = "xml";
            m_loader.GetBiblioInfoStyle = GetBiblioInfoStyle.Timestamp; // ������Ϣֻȡ�� timestamp

            this.Items = items;
            this.CacheTable = cacheTable;
        }

        public IEnumerator GetEnumerator()
        {
            Debug.Assert(m_loader != null, "");

            Hashtable dup_table = new Hashtable();  // ȷ�� recpaths �в�������ظ���·��

            List<string> recpaths = new List<string>(); // ������û�а�������Щ��¼
            foreach (ListViewItem item in this.Items)
            {
                string strRecPath = item.Text;
                Debug.Assert(string.IsNullOrEmpty(strRecPath) == false, "");

#if NO
                BiblioInfo info = (BiblioInfo)this.CacheTable[strRecPath];
                if (info == null || string.IsNullOrEmpty(info.OldXml) == true)
                    recpaths.Add(strRecPath);
#endif

                if (dup_table.ContainsKey(strRecPath) == true)
                    continue;
                BiblioInfo info = (BiblioInfo)this.CacheTable[strRecPath];
                if (info == null || string.IsNullOrEmpty(info.OldXml) == true)
                {
                    recpaths.Add(strRecPath);
                    dup_table[strRecPath] = true;
                }
            }

            // ע�� Hashtable ����һ��ʱ���ڲ�Ӧ�ñ��޸ġ�������ƻ� m_loader �� items ֮���������Ӧ��ϵ

            m_loader.RecPaths = recpaths;

            var enumerator = m_loader.GetEnumerator();

            // ��ʼѭ��
            foreach (ListViewItem item in this.Items)
            {
                string strRecPath = item.Text;
                Debug.Assert(string.IsNullOrEmpty(strRecPath) == false, "");

                BiblioInfo info = (BiblioInfo)this.CacheTable[strRecPath];
                if (info == null || string.IsNullOrEmpty(info.OldXml) == true)
                {
                    if (m_loader.Stop != null)
                    {
                        m_loader.Stop.SetMessage("���ڻ�ȡ��Ŀ��¼ " + strRecPath);
                    }
                    bool bRet = enumerator.MoveNext();
                    if (bRet == false)
                    {
                        Debug.Assert(false, "��û�е���β, MoveNext() ��Ӧ�÷��� false");
                        // TODO: ��ʱ��Ҳ���Բ��÷���һ����û���ҵ��Ĵ������Ԫ��
                        yield break;
                    }

                    BiblioItem biblio = (BiblioItem)enumerator.Current;
                    Debug.Assert(biblio.RecPath == strRecPath, "m_loader �� items ��Ԫ��֮�� ��¼·�������ϸ��������Ӧ��ϵ");

                    // ��Ҫ���뻺��
                    if (info == null)
                    {
                        info = new BiblioInfo();
                        info.RecPath = biblio.RecPath;
                    }
                    info.OldXml = biblio.Content;
                    info.Timestamp = biblio.Timestamp;
                    this.CacheTable[strRecPath] = info;
                    yield return new LoaderItem(info, item);
                }
                else
                    yield return new LoaderItem(info, item);
            }
        }
    }

}