using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.CirculationClient;

using DigitalPlatform.IO;
using DigitalPlatform.CommonControl;
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ͼ��/�ڿ�(�ɹ�)���մ���
    /// </summary>
    public partial class AcceptForm : Form
    {
        /// <summary>
        /// ��ȡ���κ�key+countֵ�б�
        /// </summary>
        public event GetKeyCountListEventHandler GetBatchNoTable = null;

        OrderDbInfos db_infos = new OrderDbInfos();

        long m_lLoaded = 0; // �����Ѿ�װ������������
        long m_lHitCount = 0;   // �������н������

        /// <summary>
        /// ͨѶͨ��
        /// </summary>
        public LibraryChannel Channel = new LibraryChannel();

        /// <summary>
        /// ��ǰ��������
        /// </summary>
        public string Lang = "zh";

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;

        EntityForm m_detailWindow = null;

        int m_nMdiClientWidth = 0;
        int m_nMdiClientHeight = 0;
        int m_nAcceptWindowHeight = 0;

        // ����б�����Ŀ����
        const int COLUMN_RECPATH = 0;
        const int COLUMN_ROLE = 1;
        const int COLUMN_TARGETRECPATH = 2;

        const int RESERVE_COLUMN_COUNT = 3;

        const int WM_LOAD_DETAIL = API.WM_USER + 200;
#if NOOOOOOOOOOO
        const int WM_LOAD_FINISH = API.WM_USER + 201;
#endif
        const int WM_RESTORE_SELECTION = API.WM_USER + 202;

        // ListViewItem imageindexֵ
        const int TYPE_SOURCE = 0;
        const int TYPE_TARGET = 1;
        const int TYPE_SOURCE_AND_TARGET = 2;
        const int TYPE_SOURCEBIBLIO = 3;   // ������Դ��Ŀ�� 2009/11/5
        const int TYPE_NOT_ORDER = 4;   // ���ԺͲɹ��޹ص����ݿ� 2009/11/5 changed

        /*
        /// <summary>
        /// װ�ؽ����ź�
        /// </summary>
        public AutoResetEvent EventFinish = new AutoResetEvent(false);
         * */

        /// <summary>
        /// ���캯��
        /// </summary>
        public AcceptForm()
        {
            InitializeComponent();

            ListViewProperty prop = new ListViewProperty();
            this.listView_accept_records.Tag = prop;
            // ��һ�����⣬��¼·��
            prop.SetSortStyle(0, ColumnSortStyle.RecPath);
            prop.GetColumnTitles -= new GetColumnTitlesEventHandler(prop_GetColumnTitles);
            prop.GetColumnTitles += new GetColumnTitlesEventHandler(prop_GetColumnTitles);

        }

        void prop_GetColumnTitles(object sender, GetColumnTitlesEventArgs e)
        {
            if (e.DbName == "<blank>")
            {
                e.ColumnTitles = new ColumnPropertyCollection();
                e.ColumnTitles.Add("������");
                e.ColumnTitles.Add("����");
                return;
            }

            e.ColumnTitles = this.MainForm.GetBrowseColumnProperties(e.DbName);
        }

        /*
        public void WaitLoadFinish()
        {
            for (; ; )
            {
                Application.DoEvents();
                bool bRet = this.EventFinish.WaitOne(10, true);
                if (bRet == true)
                    break;
            }
        }
         * */

        private void AcceptForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            this.Channel.AfterLogin -= new AfterLoginEventHandle(Channel_AfterLogin);
            this.Channel.AfterLogin += new AfterLoginEventHandle(Channel_AfterLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������

            bool bRet = InitialSizeParam();
            Debug.Assert(bRet == true, "");

            int nAcceptWindowHeight = this.MainForm.AppInfo.GetInt(
                "AcceptForm",
                "accept_window_height",
                0);
            if (nAcceptWindowHeight <= 0 || nAcceptWindowHeight >= m_nMdiClientHeight)
                nAcceptWindowHeight = (int)((float)m_nMdiClientHeight * 0.3f);  // ��ʼ��Ϊ1/3 �ͻ����߶�

            this.m_nAcceptWindowHeight = nAcceptWindowHeight;

            this.Location = new Point(0, 0);
            this.Size = new Size(m_nMdiClientWidth, m_nAcceptWindowHeight);

            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            this.StartPosition = FormStartPosition.Manual;

            this.db_infos = new OrderDbInfos();
            this.db_infos.Build(this.MainForm);

            // batchno
            this.GetBatchNoTable -= new GetKeyCountListEventHandler(AcceptForm_GetBatchNoTable);
            this.GetBatchNoTable += new GetKeyCountListEventHandler(AcceptForm_GetBatchNoTable);

#if NOOOOOOOOOOOO
            API.PostMessage(this.Handle, WM_LOAD_FINISH, 0, 0);
#endif

#if NO
            this.tabComboBox_prepare_batchNo.Text = this.MainForm.AppInfo.GetString(
                "accept_form",
                "batchno",
                "");

            this.comboBox_prepare_type.Text = this.MainForm.AppInfo.GetString(
                "accept_form",
                "item_type",
                "ͼ��");

            this.comboBox_prepare_priceDefault.Text = this.MainForm.AppInfo.GetString(
    "accept_form",
    "price_default",
    "���ռ�");


            this.checkBox_prepare_inputItemBarcode.Checked = this.MainForm.AppInfo.GetBoolean(
                "accept_form",
                "input_item_barcode",
                true);

            this.checkBox_prepare_setProcessingState.Checked = this.MainForm.AppInfo.GetBoolean(
                "accept_form",
                "set_processing_state",
                true);

            this.checkBox_prepare_createCallNumber.Checked = this.MainForm.AppInfo.GetBoolean(
    "accept_form",
    "create_callnumber",
    false);


            string strFrom = this.MainForm.AppInfo.GetString(
                "accept_form",
                "search_from",
                "");
            if (String.IsNullOrEmpty(strFrom) == false)
                this.comboBox_accept_from.Text = strFrom;

            this.comboBox_accept_matchStyle.Text = this.MainForm.AppInfo.GetString(
                "accept_form",
                "match_style",
                "��ȷһ��");

            SetTabPageEnabled(this.tabPage_accept, false);
            SetTabPageEnabled(this.tabPage_finish, false);
#endif
            FillDbNameList();

            this.UiState = this.MainForm.AppInfo.GetString(
                "accept_form",
                "ui_state",
                "");
            if (string.IsNullOrEmpty(this.comboBox_accept_matchStyle.Text) == true)
                this.comboBox_accept_matchStyle.Text = "��ȷһ��";

            SetWindowTitle();

            this.SetNextButtonEnable();

#if NO
            string strWidths = this.MainForm.AppInfo.GetString(
                "accept_form",
                "record_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_accept_records,
                    strWidths,
                    true);
            }
#endif

            this.MainForm.FillBiblioFromList(this.comboBox_accept_from);
            comboBox_accept_matchStyle_TextChanged(null, null);

        }


        void AcceptForm_GetBatchNoTable(object sender, GetKeyCountListEventArgs e)
        {
            Global.GetBatchNoTable(e,
                this,
                this.comboBox_prepare_type.Text,    // �ͳ����������й�
                "item",
                this.stop,
                this.Channel);
        }

        bool InitialSizeParam()
        {
            /*
            // 2008/12/10 ���û����һ�䣬��1024X768С��������»��׳��쳣
            if (this.MdiParent == null)
                return;

            Type t = typeof(Form);
            PropertyInfo pi = t.GetProperty("MdiClient", BindingFlags.Instance | BindingFlags.NonPublic);
            MdiClient cli = (MdiClient)pi.GetValue(this.MdiParent, null);

            this.m_nMdiClientWidth = cli.Width - 4;
            this.m_nMdiClientHeight = cli.Height - 4;
             * */

            if (this.MainForm == null)
                return false;

            MdiClient cli = this.MainForm.MdiClient;
            this.m_nMdiClientWidth = cli.Width - 4;
            this.m_nMdiClientHeight = cli.Height - 4;
            return true;
        }

        private void AcceptForm_FormClosing(object sender, FormClosingEventArgs e)
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

            // �ر� ������EntityForm
            bool bRet = CloseDetailWindow();

            // ���û�йرճɹ�
            if (bRet == false)
                e.Cancel = true;
        }

        private void AcceptForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }

            //

            this.MainForm.AppInfo.SetInt(
                "AcceptForm",
                "accept_window_height",
                this.Size.Height);

#if NO
            this.MainForm.AppInfo.SetString(
                "accept_form",
                "batchno",
                this.tabComboBox_prepare_batchNo.Text);

            this.MainForm.AppInfo.SetString(
                "accept_form",
                "item_type",
                this.comboBox_prepare_type.Text);

            this.MainForm.AppInfo.SetString(
"accept_form",
"price_default",
this.comboBox_prepare_priceDefault.Text);

            this.MainForm.AppInfo.SetBoolean(
                "accept_form",
                "input_item_barcode",
                this.checkBox_prepare_inputItemBarcode.Checked);

            this.MainForm.AppInfo.SetBoolean(
                "accept_form",
                "set_processing_state",
                this.checkBox_prepare_setProcessingState.Checked);

            this.MainForm.AppInfo.SetBoolean(
"accept_form",
"create_callnumber",
this.checkBox_prepare_createCallNumber.Checked);

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_accept_records);
            this.MainForm.AppInfo.SetString(
                "accept_form",
                "record_list_column_width",
                strWidths);

            this.MainForm.AppInfo.SetString(
                "accept_form",
                "search_from",
                this.comboBox_accept_from.Text);

            this.MainForm.AppInfo.SetString(
                "accept_form",
                "match_style",
                this.comboBox_accept_matchStyle.Text);
#endif
            this.MainForm.AppInfo.SetString(
    "accept_form",
    "ui_state",
    this.UiState);
        }

        /// <summary>
        /// ��ȡ�����ÿؼ��ߴ�״̬
        /// </summary>
        public string UiState
        {
            get
            {
                List<object> controls = new List<object>();
                controls.Add(this.tabControl_main);
                controls.Add(this.tabComboBox_prepare_batchNo);
                controls.Add(this.comboBox_prepare_type);
                controls.Add(this.comboBox_prepare_priceDefault);
                controls.Add(this.checkBox_prepare_inputItemBarcode);
                controls.Add(checkBox_prepare_setProcessingState);
                controls.Add(checkBox_prepare_createCallNumber);
                controls.Add(listView_accept_records);
                controls.Add(new ComboBoxText(comboBox_accept_from));
                controls.Add(comboBox_accept_matchStyle);
                controls.Add(this.checkedListBox_prepare_dbNames);
                return GuiState.GetUiState(controls);
            }
            set
            {
                List<object> controls = new List<object>();
                controls.Add(this.tabControl_main);
                controls.Add(this.tabComboBox_prepare_batchNo);
                controls.Add(this.comboBox_prepare_type);
                controls.Add(this.comboBox_prepare_priceDefault);
                controls.Add(this.checkBox_prepare_inputItemBarcode);
                controls.Add(checkBox_prepare_setProcessingState);
                controls.Add(checkBox_prepare_createCallNumber);
                controls.Add(listView_accept_records);
                controls.Add(new ComboBoxText(comboBox_accept_from));
                controls.Add(comboBox_accept_matchStyle);
                controls.Add(this.checkedListBox_prepare_dbNames);
                GuiState.SetUiState(controls, value);
            }
        }

        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }

        void Channel_AfterLogin(object sender, AfterLoginEventArgs e)
        {
            this.MainForm.Channel_AfterLogin(this, e);
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public void EnableControls(bool bEnable)
        {

            // page prepare
            this.tabComboBox_prepare_batchNo.Enabled = bEnable;
            this.comboBox_prepare_type.Enabled = bEnable;
            this.checkBox_prepare_inputItemBarcode.Enabled = bEnable;
            this.checkBox_prepare_setProcessingState.Enabled = bEnable;
            this.checkedListBox_prepare_dbNames.Enabled = bEnable;

            // page accept
            this.textBox_accept_queryWord.Enabled = bEnable;
            this.button_accept_searchISBN.Enabled = bEnable;
            // this.listView_accept_records.Enabled = bEnable;

            this.comboBox_accept_from.Enabled = bEnable;
            this.comboBox_accept_matchStyle.Enabled = bEnable;

            // page finish
            this.button_finish_printAcceptList.Enabled = bEnable;

            // next button
            if (bEnable == true)
            {
                SetNextButtonEnable();
            }
            else
                this.button_next.Enabled = false;

        }

        static void SetTabPageEnabled(TabPage page,
            bool bEnable)
        {
            foreach (Control control in page.Controls)
            {
                control.Enabled = bEnable;
            }
        }

        void SetNextButtonEnable()
        {
            // string strError = "";

            this.button_next.Text = "��һ����(&N)";

            if (this.tabComboBox_prepare_batchNo.Text == ""
    || this.comboBox_prepare_type.Text == "")
            {
                this.button_next.Enabled = true;

                // this.button_next.Enabled = false;
                SetTabPageEnabled(this.tabPage_accept, false);
                SetTabPageEnabled(this.tabPage_finish, false);
            }
            else
            {
                this.button_next.Enabled = true;

                // this.button_next.Enabled = true;
                SetTabPageEnabled(this.tabPage_accept, true);
                SetTabPageEnabled(this.tabPage_finish, true);
            }


            if (this.tabControl_main.SelectedTab == this.tabPage_prepare)
            {
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_accept)
            {
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_finish)
            {
                this.button_next.Text = "�ر�(&X)";
            }
            else
            {
                Debug.Assert(false, "δ֪��tabpage״̬");
            }

        }

        private void textBox_accept_isbn_Enter(object sender, EventArgs e)
        {
            // �ѡ�������ť����Ϊȱʡ��ť
            Button oldButton = (Button)this.AcceptButton;

            this.AcceptButton = this.button_accept_searchISBN;

            ((Button)this.AcceptButton).Font = new Font(((Button)this.AcceptButton).Font,
                FontStyle.Bold);
            if (oldButton != null)
            {
                oldButton.Font = new Font(oldButton.Font,
                    FontStyle.Regular);
            }
        }

        private void textBox_accept_isbn_Leave(object sender, EventArgs e)
        {
            // �ѡ���һ���ڡ���Ϊȱʡ��ť
            Button oldButton = (Button)this.AcceptButton;

            this.AcceptButton = this.button_next;

            ((Button)this.AcceptButton).Font = new Font(((Button)this.AcceptButton).Font,
                FontStyle.Bold);
            if (oldButton != null)
            {
                oldButton.Font = new Font(oldButton.Font,
                    FontStyle.Regular);
            }
        }

        // 
        /// <summary>
        /// �� ListView ��ǰ�����һ��
        /// </summary>
        /// <param name="list">ListView ����</param>
        /// <param name="strID">ID������</param>
        /// <param name="others">����������</param>
        /// <returns>�²���� ListViewItem ����</returns>
        public static ListViewItem InsertNewLine(
            ListView list,
            string strID,
            string[] others)
        {
            if (others != null)
                ListViewUtil.EnsureColumns(list, others.Length + RESERVE_COLUMN_COUNT);

            ListViewItem item = new ListViewItem(strID, 0);

            // item.SubItems.Add("");  // ��ɫ
            list.Items.Insert(0, item);

            if (others != null)
            {
                for (int i = 0; i < others.Length; i++)
                {
                    ListViewUtil.ChangeItemText(item,
                        i + RESERVE_COLUMN_COUNT ,
                        others[i]);
                }
            }

            return item;
        }

        // 
        /// <summary>
        /// �� ListView ���׷��һ��
        /// </summary>
        /// <param name="list">ListView ����</param>
        /// <param name="strID">ID������</param>
        /// <param name="others">����������</param>
        /// <returns>�²���� ListViewItem ����</returns>
        public static ListViewItem AppendNewLine(
            ListView list,
            string strID,
            string[] others)
        {
            if (others != null)
                ListViewUtil.EnsureColumns(list, others.Length + RESERVE_COLUMN_COUNT);

            ListViewItem item = new ListViewItem(strID, 0);
            // item.SubItems.Add("");  // ��ɫ

            list.Items.Add(item);

            if (others != null)
            {
                for (int i = 0; i < others.Length; i++)
                {
                    ListViewUtil.ChangeItemText(item,
                        i + RESERVE_COLUMN_COUNT,
                        others[i]);
                }
            }

            return item;
        }

        string GetCurrentMatchStyle()
        {
            string strText = this.comboBox_accept_matchStyle.Text;

            // 2009/8/6
            if (strText == "��ֵ")
                return "null";

            if (String.IsNullOrEmpty(strText) == true)
                return "exact"; // ȱʡʱ��Ϊ�� ��ȷһ��

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

        // return:
        //      -1  error
        //      0   δ����
        //      >0  ���м�¼����
        int DoSearch(out string strError)
        {
            strError = "";
            long lHitCount = 0;

            bool bQuickLoad = false;

            // �޸Ĵ��ڱ���
            this.Text = "�������� " + this.textBox_accept_queryWord.Text;

            this.listView_accept_records.Items.Clear();
            ListViewUtil.ClearSortColumns(this.listView_accept_records);

            this.m_lHitCount = 0;
            this.m_lLoaded = 0;

            stop.HideProgress();
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� "+this.textBox_accept_queryWord.Text+" ...");
            stop.BeginLoop();

            this.EnableControls(false);
            try
            {
                if (this.comboBox_accept_from.Text == "")
                {
                    strError = "��δѡ������;��";
                    return -1;
                }

                string strFromStyle = "";

                try
                {
                    strFromStyle = this.MainForm.GetBiblioFromStyle(this.comboBox_accept_from.Text);
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }

                if (String.IsNullOrEmpty(strFromStyle) == true)
                {
                    strError = "GetFromStyle()û���ҵ� '" + this.comboBox_accept_from.Text + "' ��Ӧ��style�ַ���";
                    return -1;
                }

                /*
                string strFromStyle = "isbn";

                if (this.comboBox_prepare_type.Text == "ͼ��")
                    strFromStyle = "isbn";
                else
                {
                    Debug.Assert(this.comboBox_prepare_type.Text == "����������", "");
                    strFromStyle = "issn";
                }*/

                // ע��"null"ֻ����ǰ�˶��ݴ��ڣ����ں��ǲ��������ν��matchstyle��
                string strMatchStyle = GetCurrentMatchStyle();

                if (this.textBox_accept_queryWord.Text == "")
                {
                    if (strMatchStyle == "null")
                    {
                        this.textBox_accept_queryWord.Text = "";

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
                    // 2009/11/5
                    if (strMatchStyle == "null")
                    {
                        strError = "������ֵ��ʱ���뱣�ּ�����Ϊ��";
                        return -1;
                    }
                }

                string strQueryXml = "";
                long lRet = Channel.SearchBiblio(stop,
                    GetDbNameListString(),  // "<ȫ��>",
                    this.textBox_accept_queryWord.Text,
                    1000,   // this.MaxSearchResultCount,  // 1000
                    strFromStyle,
                    strMatchStyle,  // "exact",
                    this.Lang,
                    "accept",   // strResultSetName
                    "",    // strSearchStyle
                    "", // strOutputStyle
                    out strQueryXml,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                if (lRet == 0)
                {
                    strError = "δ����";
                    return 0;
                }

                lHitCount = lRet;

                this.m_lHitCount = lHitCount;

                // ��ʾǰ���
                stop.SetProgressRange(0, lHitCount * 2);

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                Record[] searchresults = null;

                bool bPushFillingBrowse = true; //  this.PushFillingBrowse;

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�";
                            return -1;
                        }
                    }

                    stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    string strStyle = "id,cols";

                    if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                        strStyle = "id";

                    lRet = Channel.GetSearchResult(
                        stop,
                        "accept",   // strResultSetName
                        lStart,
                        lPerCount,
                        strStyle,
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (lRet == 0)
                    {
                        strError = "δ����";
                        return 0;
                    }

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        ListViewItem item = null;
                        if (bPushFillingBrowse == true)
                        {
                            if (bQuickLoad == true)
                                item = InsertNewLine(
                                    (ListView)this.listView_accept_records,
                                    searchresults[i].Path,
                                    searchresults[i].Cols);
                            else
                                item = InsertNewLine(
                                    this.listView_accept_records,
                                    searchresults[i].Path,
                                    searchresults[i].Cols);
                        }
                        else
                        {
                            if (bQuickLoad == true)
                                item = AppendNewLine(
                                    (ListView)this.listView_accept_records,
                                    searchresults[i].Path,
                                    searchresults[i].Cols);
                            else
                                item = AppendNewLine(
                                    this.listView_accept_records,
                                    searchresults[i].Path,
                                    searchresults[i].Cols);
                        }

                        // 
                        // ���ݼ�¼·�������ListViewItem�����imageindex�±�
                        // return:
                        //      -2  ����������Ŀ��
                        //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
                        //      0   Դ
                        //      1   Ŀ��
                        //      2   ͬʱΪԴ��Ŀ��
                        //      3   ��Դ
                        int image_index = this.db_infos.GetItemType(searchresults[i].Path,
                            this.comboBox_prepare_type.Text);
                        Debug.Assert(image_index != -2, "��Ȼ����������Ŀ��ļ�¼?");
                        item.ImageIndex = image_index;

                        SetItemColor(item); //
                    }

                    lStart += searchresults.Length;
                    // lCount -= searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;

                    this.m_lLoaded = lStart;
                    stop.SetProgressValue(lStart);
                }

                // this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼";
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControls(true);
            }

            return (int)lHitCount;

        ERROR1:
            return -1;
        }

        int FilterOneItem(ListViewItem item,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strRecPath = ListViewUtil.GetItemText(item,
                COLUMN_RECPATH);
            // ���ݼ�¼·�������ListViewItem�����imageindex�±�
            // return:
            //      -2  ����������Ŀ��
            //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
            //      0   Դ
            //      1   Ŀ��
            //      2   ͬʱΪԴ��Ŀ��
            //      3   ��Դ
            int image_index = this.db_infos.GetItemType(strRecPath,
                            this.comboBox_prepare_type.Text);
            // ��Ǳ�ڵ�Դ
            if (image_index == 0 || image_index == 2)
            {
                // ���998$t
                string strTargetRecPath = "";
                long lRet = Channel.GetBiblioInfo(
                    stop,
                    strRecPath,
                    "", // strBiblioXml
                    "targetrecpath",   // strResultType
                    out strTargetRecPath,
                    out strError);
                ListViewUtil.ChangeItemText(item, COLUMN_TARGETRECPATH, strTargetRecPath);
            }

            // ��Ǳ�ڵ�Դ
            if (image_index == 0 || image_index == 2)
            {
                // ����Ƿ�߱��ɹ���Ϣ
                // װ�붩����¼������Ƿ��ж�����Ϣ
                // parameters:
                //      strSellerList   ���������б����ŷָ���ַ��������Ϊnull����ʾ�����������ƽ��й���
                // return:
                //      -1  ����
                //      0   û��(����Ҫ���)������Ϣ
                //      >0  ����ô��������Ҫ��Ķ�����Ϣ
                nRet = LoadOrderRecords(strRecPath,
                    null,   // strSellerList,
                    out strError);
                if (nRet == -1)
                    return -1;

                RecordInfo info = GetRecordInfo(item);
                info.HasOrderInfo = ((nRet == 0) ? false : true);

                if (nRet == 0)
                    SetItemColor(item);
            }

            return 0;
        }

        // ��������Ǳ��Դ��¼�����û�вɹ���Ϣ�����߲ɹ���Ϣ���ض��������Ǻϣ����б�Ϊ��ɫ
        int FilterOrderInfo(out string strError)
        {
            strError = "";
            int nRet = 0;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڹ��˼�¼ ...");
            stop.BeginLoop();

            this.EnableControls(false);


            try
            {
                // ��ʾ����
                stop.SetProgressRange(0, this.listView_accept_records.Items.Count * 2);
                for (int i = 0; i < this.listView_accept_records.Items.Count; i++)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                            return 0;
                    }


                    ListViewItem item = this.listView_accept_records.Items[i];

                    /*
                    string strRecPath = ListViewUtil.GetItemText(item,
                        COLUMN_RECPATH);
                    // ���ݼ�¼·�������ListViewItem�����imageindex�±�
                    // return:
                    //      -2  ����������Ŀ��
                    //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
                    //      0   Դ
                    //      1   Ŀ��
                    //      2   ͬʱΪԴ��Ŀ��
                    //      3   ��Դ
                    int image_index = this.db_infos.GetItemType(strRecPath,
                                    this.comboBox_prepare_type.Text);
                    // ��Ǳ�ڵ�Դ
                    if (image_index == 0 || image_index == 2)
                    {
                        // ����Ƿ�߱��ɹ���Ϣ
                        // װ�붩����¼������Ƿ��ж�����Ϣ
                        // parameters:
                        //      strSellerList   ���������б����ŷָ���ַ��������Ϊnull����ʾ�����������ƽ��й���
                        // return:
                        //      -1  ����
                        //      0   û��(����Ҫ���)������Ϣ
                        //      >0  ����ô��������Ҫ��Ķ�����Ϣ
                        nRet = LoadOrderRecords(strRecPath,
                            null,   // strSellerList,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        RecordInfo info = GetRecordInfo(item);
                        info.HasOrderInfo = ((nRet == 0) ? false : true);

                        if (nRet == 0)
                            SetItemColor(item);
                    }


                    // ��Ǳ�ڵ�Դ
                    if (image_index == 0 || image_index == 2)
                    {
                        // ���998$t
                        string strTargetRecPath = "";
                        long lRet = Channel.GetBiblioInfo(
                            stop,
                            strRecPath,
                            "", // strBiblioXml
                            "targetrecpath",   // strResultType
                            out strTargetRecPath,
                            out strError);
                        ListViewUtil.ChangeItemText(item, COLUMN_TARGETRECPATH, strTargetRecPath);
                    }
                     * */
                    nRet = FilterOneItem(item, out strError);
                    if (nRet == -1)
                        return -1;

                    /*
                    string strRole = ListViewUtil.GetItemText(item,
        COLUMN_ROLE);
                     * */

                    stop.SetProgressValue(this.listView_accept_records.Items.Count + i);
                }
            }
            finally
            {
                this.EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
            }

            return 1;
        }

        // װ�붩����¼������Ƿ��ж�����Ϣ
        // parameters:
        //      strSellerList   ���������б����ŷָ���ַ��������Ϊnull����ʾ�����������ƽ��й���
        // return:
        //      -1  ����
        //      0   û��(����Ҫ���)������Ϣ
        //      >0  ����ô��������Ҫ��Ķ�����Ϣ
        /*public*/ int LoadOrderRecords(string strBiblioRecPath,
            string strSellerList,
            out string strError)
        {
            int nCount = 0;

            stop.SetMessage("����װ����Ŀ��¼ '" + strBiblioRecPath + "' �����Ķ�����Ϣ ...");

            // string strHtml = "";
            long lStart = 0;
            long lResultCount = 0;
            long lCount = -1;

            // 2012/5/9 ��дΪѭ����ʽ
            for (; ; )
            {

                EntityInfo[] orders = null;

                long lRet = Channel.GetOrders(
                    stop,
                    strBiblioRecPath,
                    lStart,
                    lCount,
                    "",
                    "zh",
                    out orders,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (lRet == 0)
                    return 0;

                lResultCount = lRet;

                Debug.Assert(orders != null, "");

                // �Ż����������Ҫ�������������Ͳ���װ��XML��¼��DOM�н���������
                if (strSellerList == null)
                    return orders.Length;

                for (int i = 0; i < orders.Length; i++)
                {
                    if (orders[i].ErrorCode != ErrorCodeValue.NoError)
                    {
                        strError = "·��Ϊ '" + orders[i].OldRecPath + "' �Ķ�����¼װ���з�������: " + orders[i].ErrorInfo;  // NewRecPath
                        return -1;
                    }

                    // ����һ������xml��¼��ȡ���й���Ϣ����listview��
                    OrderItem orderitem = new OrderItem();

                    int nRet = orderitem.SetData(orders[i].OldRecPath, // NewRecPath
                             orders[i].OldRecord,
                             orders[i].OldTimestamp,
                             out strError);
                    if (nRet == -1)
                        return -1;

                    if (orders[i].ErrorCode == ErrorCodeValue.NoError)
                        orderitem.Error = null;
                    else
                        orderitem.Error = orders[i];

                    if (strSellerList != null)
                    {
                        if (StringUtil.IsInList(orderitem.Seller, strSellerList) == false)
                            continue;
                    }

                    // TODO: �Ѿ���������ģ��Ƿ񲻼��붩����Ϣ��

                    nCount++;
                    /*
                    this.orderitems.Add(orderitem);
                    orderitem.AddToListView(this.ListView);
                     * */
                }

                lStart += orders.Length;
                if (lStart >= lResultCount)
                    break;
            }
            return nCount;
        ERROR1:
            return -1;
        }

        private void button_accept_searchISBN_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.textBox_accept_queryWord.Text == ""
                && this.comboBox_accept_matchStyle.Text != "��ֵ")
            {
                strError = "��δ���������";
                goto ERROR1;
            }

            if (m_detailWindow != null
                && m_detailWindow.IsDisposed == false)
            {
                if (m_detailWindow.IsLoading == true)
                {
                    strError = "��ǰ�ֲᴰ����װ�ؼ�¼�����Ժ������Լ���";
                    goto ERROR1;
                }
            }

            // ��ʹdetailWindow����
            // TODO: �����Ƿ������û�checkbox�����Ƿ��Զ����桱?
            SaveDetailWindowChanges();

            // ��ǰdetailWindow�������
            ClearDetailWindow(true);

            ClearSourceTarget();    // 2009/6/2

            int nRet = DoSearch(out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
                MessageBox.Show(this, "������ '" + this.textBox_accept_queryWord.Text + "' δ�����κμ�¼");

            nRet = FilterOrderInfo(out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /*
        // ������ֱ/ˮƽ���ַ��
        private void splitContainer_accept_multiRecords_DoubleClick(object sender, EventArgs e)
        {
            if (this.splitContainer_accept_multiRecords.Orientation == Orientation.Horizontal)
                this.splitContainer_accept_multiRecords.Orientation = Orientation.Vertical;
            else
                this.splitContainer_accept_multiRecords.Orientation = Orientation.Horizontal;
        }*/

        bool CloseDetailWindow()
        {
            // �ر� ������EntityForm
            if (m_detailWindow != null)
            {
                if (m_detailWindow.IsDisposed == false)
                {
                    m_detailWindow.Close();

                    // 2009/2/3
                    if (m_detailWindow.IsDisposed == false)
                        return false;   // û�йرճɹ����ȷ�˵��������δ���棿�û�ѡ��Cancel


                    // TODO: Ҫ�����Ƿ���ر���?
                    // ͨ��Hashcode���߶���ָ�����۲�?

                    m_detailWindow = null;
                }
                else
                    m_detailWindow = null;
            }

            return true;
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
#if NOOOOOOOOOO
                case WM_LOAD_FINISH:
                    EventFinish.Set();
                    return;
#endif

                case WM_RESTORE_SELECTION:
                    RestoreSelection();
                    return;
                case WM_LOAD_DETAIL:
                    {
                        /*
                        if (this.listView_accept_records.Enabled == false)
                            return; // ��ʧ
                         * */
                        
                        int index = m.LParam.ToInt32();

                        if (index == -1)
                        {
                            this.LoadDetail(index);
                            return;
                        }

                        /*
                        // ���潹��״̬
                        bool bFouced = this.listView_accept_records.Focused;

                        this.listView_accept_records.Enabled = false;
                         * */

                        bool bRet = this.LoadDetail(index);

                        /*
                        this.listView_accept_records.Enabled = true;

                        if (bRet == false && index != -1)
                        {
                            API.PostMessage(this.Handle, WM_RESTORE_SELECTION, 0, 0);
                            return;
                        }

                        if (this.m_detailWindow != null)
                            this.m_detailWindow.TargetRecPath = this.GetTargetRecPath();

                        // �ָ�����״̬
                        if (bFouced == true)
                            this.listView_accept_records.Focus();
                         * */
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }

        // ��յ�ǰ��ϸ������
        // 2009/2/3
        bool ClearDetailWindow(bool bWarningNotSave)
        {
            if (this.m_detailWindow == null)
                return true;

            if (m_detailWindow != null
                && m_detailWindow.IsDisposed == true)
            {
                m_detailWindow = null;
                return true;
            }

            bool bRet = this.m_detailWindow.Clear(bWarningNotSave);
            if (bRet == false)
                return false;

            this.m_detailWindow.BiblioRecPath = "";
            return true;
        }

        // ǿ�Ƶ�ǰ��ϸ���ڱ��淢�������޸�
        // 2009/2/3
        void SaveDetailWindowChanges()
        {
            if (this.m_detailWindow == null)
                return;

            if (m_detailWindow != null
                && m_detailWindow.IsDisposed == true)
            {
                m_detailWindow = null;
                return;
            }

            if (this.m_detailWindow.Changed == true)
            {
                this.EnableControls(false); // ��ֹ������������ť
                try
                {
                    this.m_detailWindow.DoSaveAll();
                }
                finally
                {
                    this.EnableControls(true);
                }
            }

        }

        // ������ϸ���ڵ���Ŀ��¼·�����ָ���listview�ڶ�Ӧ�����ѡ��
        void RestoreSelection()
        {
            if (this.m_detailWindow == null)
                return;

            if (m_detailWindow != null
                && m_detailWindow.IsDisposed == true)
            {
                m_detailWindow = null;
                return;
            }

            string strBiblioRecPath = this.m_detailWindow.BiblioRecPath;

            ListViewItem item = ListViewUtil.FindItem(this.listView_accept_records,
                strBiblioRecPath,
                COLUMN_RECPATH);
            if (item == null)
                return;

            if (item.Selected == true && this.listView_accept_records.SelectedItems.Count == 1)
                return;

            // this.listView_accept_records.SelectedItems.Clear();
            for (int i = 0; i < this.listView_accept_records.Items.Count; i++)
            {
                ListViewItem cur_item = this.listView_accept_records.Items[i];
                if (cur_item == item)
                    continue;
                cur_item.Selected = false;
            }
            item.Selected = true;
        }

        /*public*/ delegate int Delegate_SafeLoadRecord(string strBiblioRecPath,
    string strPrevNextStyle);

        bool LoadDetail(int index)
        {
            if (index == -1 || index >= this.listView_accept_records.Items.Count)
            {
                if (this.SingleClickLoadDetail == true)
                {
                    EntityForm detail_window = this.DetailWindow;

                    if (detail_window != null)
                        detail_window.ReadOnly = true;
                }
                else
                    CloseDetailWindow();

                return false;
            }

            string strPath = this.listView_accept_records.Items[index].SubItems[0].Text;

            OpenDetailWindow();

            // ��ϸ���ڱ�������������¼�����÷���װ��
            if (m_detailWindow.BiblioRecPath == strPath)
                return true;

            Delegate_SafeLoadRecord d = new Delegate_SafeLoadRecord(m_detailWindow.SafeLoadRecord);
            m_detailWindow.BeginInvoke(d, new object[] { strPath,
                "" });
            return true;

            /*
            m_detailWindow.SafeLoadRecord(strPath, "");
            return true;
             * */

            /*

            // return:
            //      -1  �����Ѿ���MessageBox����
            //      0   û��װ��
            //      1   �ɹ�װ��
            int nRet = m_detailWindow.LoadRecord(strPath,
                "",
                true);
            if (nRet != 1)
                return false;


            return true;
             * */

            // TODO: �����Ƿ����գ�Ԥ�����EntityForm�ڵ�AcceptBatchNo?
        }

        // �û���list records�е�ѡ�˼�¼
        private void listView_accept_records_SelectedIndexChanged(object sender, EventArgs e)
        {

            List<int> protect_column_numbers = new List<int>();
            protect_column_numbers.Add(COLUMN_ROLE);  // ��������ɫ����
            protect_column_numbers.Add(COLUMN_TARGETRECPATH);  // ������Ŀ��·������
            ListViewUtil.OnSeletedIndexChanged(this.listView_accept_records, 0, protect_column_numbers);

            if (this.SingleClickLoadDetail == false)
                return;

            if (this.listView_accept_records.SelectedItems.Count == 0
                || this.listView_accept_records.SelectedItems.Count > 1)    // 2009/2/3 ��ѡʱҲҪ��ֹ������ϸ��
            {
                /*
                EntityForm detail_window = this.DetailWindow;

                if (detail_window != null)
                    detail_window.Enabled = false;
                */
                API.PostMessage(this.Handle, WM_LOAD_DETAIL, 0, -1);
                return;
            }

            /*
            string strPath = this.listView_accept_records.SelectedItems[0].SubItems[0].Text;

            OpenDetailWindow();

            // return:
            //      -1  �����Ѿ���MessageBox����
            //      0   û��װ��
            //      1   �ɹ�װ��
            m_detailWindow.LoadRecord(strPath,
                "");
             * */
            API.PostMessage(this.Handle, WM_LOAD_DETAIL, 0, this.listView_accept_records.SelectedIndices[0]);

        }

        void OpenDetailWindow()
        {
            if (m_detailWindow != null)
            {
                if (m_detailWindow.IsDisposed == true)
                {
                    m_detailWindow = null;
                }
            }


            // TODO: ��һ��EntityForm��Ȼ��λ��Ԥ����λ�á��͵�ǰ�������ֵܹ�ϵ
            if (m_detailWindow == null)
            {
                bool bExistOldEntityForm = false;
                if (this.MainForm.GetTopChildWindow<EntityForm>() != null)
                {
                    bExistOldEntityForm = true;
                }

                m_detailWindow = new EntityForm();

                m_detailWindow.AcceptMode = true;
                m_detailWindow.MainForm = this.MainForm;
                m_detailWindow.MdiParent = this.MainForm;
                #if ACCEPT_MODE

                m_detailWindow.FormBorderStyle = FormBorderStyle.None;

                m_detailWindow.Location = new Point(0, m_nAcceptWindowHeight);
                m_detailWindow.Size = new Size(m_nMdiClientWidth, m_nMdiClientHeight - m_nAcceptWindowHeight);
                m_detailWindow.StartPosition = FormStartPosition.Manual;
#else

#endif

                m_detailWindow.Show();
                if (true)
                {
                    /*
                     * 2011/4/14 �ʼ�
                     *2.2���ȴ򿪡����ա����ܴ���Ȼ���һ���������ڣ��硰�ֲ�
���������ֲᴰ��󻯣�֮���ٹرգ��ڡ����ա����ܴ������ա��׶�����ISBN�ż�
����˫��������¼�������Ϣ����ϸ�����Ӵ���û������������ͼ������2.jpg��
                     * */
#if ACCEPT_MODE
                    m_detailWindow.WindowState = FormWindowState.Normal;
                    m_detailWindow.Location = new Point(0, m_nAcceptWindowHeight);
                    m_detailWindow.Size = new Size(m_nMdiClientWidth, m_nMdiClientHeight - m_nAcceptWindowHeight);
#else
                    m_detailWindow.WindowState = FormWindowState.Maximized;
#endif
                }


                m_detailWindow.OrderControl.PrepareAccept -= new PrepareAcceptEventHandler(m_detailWindow_PrepareAccept);
                m_detailWindow.OrderControl.PrepareAccept += new PrepareAcceptEventHandler(m_detailWindow_PrepareAccept);

                m_detailWindow.IssueControl.PrepareAccept -= new PrepareAcceptEventHandler(m_detailWindow_PrepareAccept);
                m_detailWindow.IssueControl.PrepareAccept += new PrepareAcceptEventHandler(m_detailWindow_PrepareAccept);

                // 2011/4/18
                if (bExistOldEntityForm == true)
                {
                    /*
                     * 2011/4/14 �ʼ�
                    2.1���Զ�����Ϣ�������ղ����У��ȴ�һ���������ڣ��硰�ֲ�
������Ȼ��򿪡����ա����ܴ���������-���գ�������򿪵ġ��ֲᴰ����ʹ���ֲ�
������Ϊ��ǰ���ڣ���������󻯣�ʹ�ù��߲˵�������-���ա��л��ء����ա�����
�����ڡ����ա��׶�����ISBN�ż�����˫��������¼�������Ϣ����ϸ������ʱ����
���ֲᴰҲ��������������ͼ������1.jpg���� 
                     * */
                    this.Activate();
                    m_detailWindow.Activate();
                }
            }
            else
            {
                if (m_detailWindow.ReadOnly == true)
                    m_detailWindow.ReadOnly = false;
            }

        }

        // ���Դ��¼��998$t��������ǰ�б����Ƿ��Ѿ���������¼�����û�У�����Ҫװ��
        // return:
        //      -1  ����(������������Ѿ�����)
        //      0   Դ��¼û��998$t
        //      1   ����������ǰĿ���¼�Ѿ����б��д���
        //      2   ��װ����Ŀ���¼
        int AutoLoadTarget(string strSourceRecPath,
            out string strTargetRecPath,
            out string strError)
        {
            strError = "";
            strTargetRecPath = "";
            int nRet = 0;

            ListViewItem source_item = ListViewUtil.FindItem(this.listView_accept_records,
                strSourceRecPath, COLUMN_RECPATH);
            if (source_item == null)
            {
                strError = "��ǰ�б��о�Ȼû��·��Ϊ '" + strSourceRecPath + "' ������";
                return -1;
            }

            strTargetRecPath = ListViewUtil.GetItemText(source_item,
                COLUMN_TARGETRECPATH);
            if (String.IsNullOrEmpty(strTargetRecPath) == true)
                return 0;

            ListViewItem target_item = ListViewUtil.FindItem(this.listView_accept_records,
                strTargetRecPath, COLUMN_RECPATH);

            if (target_item != null)
                return 1;
            else
            {
                // ����һ�У���Դ��¼���Ժ�
                target_item = new ListViewItem();
                ListViewUtil.ChangeItemText(target_item, COLUMN_RECPATH, strTargetRecPath);
                int index = this.listView_accept_records.Items.IndexOf(source_item);
                Debug.Assert(index != -1, "");
                index++;
                this.listView_accept_records.Items.Insert(index, target_item);


                this.EnableControls(false);
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("����װ�ؼ�¼ '" + strTargetRecPath + "' ...");
                stop.BeginLoop();

                try
                {
                    nRet = RefreshBrowseLine(target_item, out strError);
                    if (nRet == -1)
                    {
                        ListViewUtil.ChangeItemText(target_item, 2, strError);
                        return -1;
                    }
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                    this.EnableControls(true);
                }

                return 2;
            }
        }

        // �Զ�׼��Ŀ���¼
        // parameters:
        //      strBiblioSourceRecPath  [out]��ǰ�б��в������趨����Դ��Ŀ��¼·��
        // return:
        //      -1  error
        //      0   ��������ǰ�Ѿ�ѡ����Ŀ����������Զ�׼��Ŀ��
        //      1   ׼������Ŀ������
        //      2   �޷�׼��Ŀ������������߱�
        int AutoPrepareAccept(
            string strSourceRecPath,
            out string strTargetRecPath,
            out string strBiblioSourceRecPath,
            out string strError)
        {
            strError = "";
            strTargetRecPath = "";
            strBiblioSourceRecPath = "";

            int nRet = 0;

            bool bSeriesMode = false;
            if (this.comboBox_prepare_type.Text == "����������")
                bSeriesMode = true;
            else
                bSeriesMode = false;

            strBiblioSourceRecPath = GetBiblioSourceRecPath();
            if (bSeriesMode == true
                && String.IsNullOrEmpty(strBiblioSourceRecPath) == false)
            {
                strError = "���������������֧�� ��Դ��ɫ";
                return -1;
            }

            if (String.IsNullOrEmpty(strSourceRecPath) == true)
            {
                strError = "strSourceRecPath����ֵ����Ϊ��";
                return -1;
            }

            string str998TargetRecPath = "";
            // ���Դ��¼��998$t��������ǰ�б����Ƿ��Ѿ���������¼�����û�У�����Ҫװ��
            // return:
            //      -1  ����(������������Ѿ�����)
            //      0   Դ��¼û��998$t
            //      1   ����������ǰĿ���¼�Ѿ����б��д���
            //      2   ��װ����Ŀ���¼
            nRet = AutoLoadTarget(strSourceRecPath,
                out str998TargetRecPath,
                out strError);
            if (nRet == -1)
                return -1;

            strTargetRecPath = GetTargetRecPath();
            if (String.IsNullOrEmpty(strTargetRecPath) == false)
            {
                // ��ǰ�������Ѿ�ѡ����Ŀ������Ͳ����ڱ�����Ҫ���ĵ�������
                strError = "��ǰ�Ѿ�ѡ����Ŀ������";
                return 0;
            }

            string strSourceDbName = Global.GetDbName(strSourceRecPath);
            OrderDbInfo source_dbinfo = this.db_infos.LocateByBiblioDbName(strSourceDbName);
            if (source_dbinfo == null)
            {
                strError = "��this.db_infos�о�Ȼû���ҵ�����Ϊ " + strSourceDbName + " ����Ŀ�����";
                return -1;
            }

#if DEBUG
            if (String.IsNullOrEmpty(source_dbinfo.IssueDbName) == false)
            {
                Debug.Assert(this.comboBox_prepare_type.Text == "����������", "");
            }
            else
            {
                Debug.Assert(this.comboBox_prepare_type.Text == "ͼ��", "");
            }
#endif

            // Դ��¼���� �ɹ�������
            if (source_dbinfo.IsOrderWork == true)
            {
                // ����Դ��¼��998$t��ָ��Ҳ�Զ�����Դ��¼��ΪĿ��
                strError = "Դ��¼��Ŀ���¼��ͬһ��: " + strSourceRecPath + "��Դ��¼���Բɹ�������";
                strTargetRecPath = strSourceRecPath;
                nRet = SetTarget(strTargetRecPath, out strError);
                if (nRet == -1)
                    return -1;
                return 1;
            }

            // Դ��¼���������Բɹ������⣬��ô��Ҫ������Դ��¼�е�998$t����
            if (String.IsNullOrEmpty(str998TargetRecPath) == false)
            {
                strTargetRecPath = str998TargetRecPath;
                nRet = SetTarget(strTargetRecPath, out strError);
                if (nRet == -1)
                    return -1;
                strError = "Դ��¼ '" + strSourceRecPath + "' �е�998$tָ�� '" + str998TargetRecPath + "'����ô�ͰѺ�����ΪĿ���¼��";
                return 1;
            }

            // ����Դ���ǲ���ͬʱҲ��Ŀ��⣿����ǣ�ֱ�Ӱ�Դ��¼��ΪĿ���¼
            if (source_dbinfo.IsSourceAndTarget == true)
            {
                strError = "Դ��¼�����ڿ�ͬʱ�߱�Դ��Ŀ��Ľ�ɫ�����Դ��¼��Ŀ���¼��ͬһ��: " + strSourceRecPath;
                strTargetRecPath = strSourceRecPath;
                nRet = SetTarget(strTargetRecPath, out strError);
                if (nRet == -1)
                    return -1;
                return 1;
            }

            int nSourceItemCount = 0;   // Դ��ɫ������Ŀ��������˫��ɫ������Ŀ
            int nTargetItemCount = 0;   // Ŀ���ɫ������Ŀ��������˫��ɫ������Ŀ
            int nSourceAndTargetItemCount = 0;    // ͬʱ�߱�������ɫ��������Ŀ
            for (int i = 0; i < this.listView_accept_records.Items.Count; i++)
            {
                ListViewItem item = this.listView_accept_records.Items[i];

                if (item.ImageIndex == TYPE_SOURCE)
                    nSourceItemCount++;
                if (item.ImageIndex == TYPE_TARGET)
                    nTargetItemCount++;
                if (item.ImageIndex == TYPE_SOURCE_AND_TARGET)
                    nSourceAndTargetItemCount++;
            }

            // ��ǰ�б��ڸ���û��Ǳ��Ŀ���¼
            if (nTargetItemCount + nSourceAndTargetItemCount == 0)
            {
                // ��Ҫ�ҵ�һ��Ŀ��⣬�����¼�¼·��
                // ���Ǳ�ڵ�Ŀ���ܶ࣬����Ҫ�û�ѡ��
                List<string> target_dbnames = this.db_infos.GetTargetDbNames();
                if (target_dbnames.Count == 0)
                {
                    strError = "��ǰ��������û�������ʺ���Ϊ����Ŀ���(Ҳ���ǰ���ʵ���)����Ŀ�⣬�޷���������";
                    return 2;
                }

                if (target_dbnames.Count == 1)
                {
                    strTargetRecPath = target_dbnames[0] + "/?";
                    strError = "���� "+target_dbnames[0]+" �д���һ���µ�Ŀ���¼";
                    return 1;
                }

                Debug.Assert(target_dbnames.Count > 1, "");

                GetAcceptTargetDbNameDlg dlg = new GetAcceptTargetDbNameDlg();
                MainForm.SetControlFont(dlg, this.Font, false);

                dlg.SeriesMode = bSeriesMode;
                dlg.Text = "��ѡ��һ��Ŀ����Ŀ�⣬����ʱ�������д���һ���µ���Ŀ��¼";
                dlg.MainForm = this.MainForm;
                dlg.MarcSyntax = source_dbinfo.Syntax;
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.ShowDialog(this);
                if (dlg.DialogResult == DialogResult.Cancel)
                {
                    strError = "�û�����ѡ��Ŀ���";
                    return -1;
                }

                strTargetRecPath = dlg.DbName + "/?";

                strError = "���� "+dlg.DbName+" �д���һ���µ�Ŀ���¼";
                return 1;
            }

            // �����ǰ����һ��Ǳ��Ŀ������
            if (nTargetItemCount + nSourceAndTargetItemCount == 1)
            {
                for (int i = 0; i < this.listView_accept_records.Items.Count; i++)
                {
                    ListViewItem item = this.listView_accept_records.Items[i];

                    if (item.ImageIndex == TYPE_SOURCE_AND_TARGET
                        || item.ImageIndex == TYPE_TARGET)
                    {
                        strTargetRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);
                        nRet = SetTarget(item, "set", out strError);
                        if (nRet == -1)
                            return -1;
                        strError = "Ψһ��Ǳ��Ŀ���¼ '" + strTargetRecPath + "' ���Զ���ΪĿ���¼��";
                        return 1;
                    }
                }

                strError = "TYPE_TARGET��TYPE_SOURCE_AND_TARGET�������Ȼû���ҵ�...";
                return -1;
            }

            if (nTargetItemCount + nSourceAndTargetItemCount > 1)
            {
                strError = "���������м�¼�б����趨��Ŀ���¼��Ȼ���ٽ�������";
                return 2;
            }

            strError = "���������м�¼�б����趨��Ŀ���¼��Ȼ���ٽ������� --";
            return 2;
        }

        // TODO: ��дһ����������û����ȷ�趨Ŀ���¼�����ǵ�ǰ���������Ƶ���Ŀ���¼��ʱ��
        // ����Ŀ���¼·����
        // ������1) ��ǰֻ��һ��Ǳ��Ŀ���¼ 2) ��ǰû��Ǳ��Ŀ���¼�����ǿ��Գ䵱Ŀ���Ŀ�ֻ��һ�������Դ��Ŀ����غϣ�������Դ��¼��ΪĿ���¼�������á�Ŀ���/?����ΪĿ���¼·��
        //  3) ��ǰû��Ǳ��Ŀ���¼�����ҿ��Գ䵱Ŀ�����ж������ʱ����Ҫ���ֶԻ����ò�����ѡ��һ��Ŀ��⡣�Ի�����Ҫ������ǰѡ����״̬���Ա��������߲����ٶ�
        //  4) ��ǰû��Ǳ��Ŀ���¼������û���κο���Գ䵱Ŀ��⡣��������������
        // Ӧ�����������������������һ����¼·������ǰ�б��С���������Ϊ�趨Դ����Ŀ����ṩ�˸�������������Ա��ⵥ��ͨ��ISBN�����ľ����ԡ�
        void m_detailWindow_PrepareAccept(object sender, 
            PrepareAcceptEventArgs e)
        {
            // MessageBox.Show(this, "Prepare accept");
            string strError = "";
            string strWarning = "";
            int nRet = 0;

            // �������κ�
            e.AcceptBatchNo = this.tabComboBox_prepare_batchNo.Text;

            // �Ƿ���Ҫ������ĩ�γ������������ŵĽ���
            e.InputItemsBarcode = this.checkBox_prepare_inputItemBarcode.Checked;

            // 2010/12/5
            e.PriceDefault = this.comboBox_prepare_priceDefault.Text;

            // Ϊ�´����Ĳ��¼���á��ӹ��С�״̬
            e.SetProcessingState = this.checkBox_prepare_setProcessingState.Checked;

            // 2012/5/7
            e.CreateCallNumber = this.checkBox_prepare_createCallNumber.Checked;


            Debug.Assert(String.IsNullOrEmpty(e.SourceRecPath) == false, "");
            // e.SourceRecPath �����ֲᴰ�ڵ�ǰ��¼����ǿ�ҵ����������ΪԴ�����Ǻ͵�ǰAcceprtForm������б��п����Ѿ��趨��Դ����ͬһ��

            string strTargetRecPath = "";
            string strBiblioSourceRecPath = ""; // ��ǰ�б��в������趨����Դ��Ŀ��¼·��

            // �Զ�׼��Ŀ���¼
            // return:
            //      -1  error
            //      0   ��������ǰ�Ѿ�ѡ����Ŀ����������Զ�׼��Ŀ��
            //      1   ׼������Ŀ������
            //      2   �޷�׼��Ŀ������������߱�
            nRet = AutoPrepareAccept(
                e.SourceRecPath,
                out strTargetRecPath,
                out strBiblioSourceRecPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 2)
            {
                goto ERROR1;
            }

            // 2009/2/16
            // ����AcceptForm���Ѿ��趨��Դ�Ƿ��e.SourceRecPath�е�һ��
            string strExistSourceRecPath = GetSourceRecPath();
            if (String.IsNullOrEmpty(strExistSourceRecPath) == false)
            {
                if (strExistSourceRecPath != e.SourceRecPath)
                {
                    /*
                    ListViewItem old_source_item = ListViewUtil.FindItem(this.listView_accept_records,
                        strExistSourceRecPath,
                        COLUMN_RECPATH);
                    if (old_source_item == null)
                    {
                        strError = "�б��о�Ȼû���ҵ�·��Ϊ '" + strExistSourceRecPath + "' ������";
                        goto ERROR1;
                    }
                    // ���������Դ�����Ƿ��Ѿ�Ϊб��
                    RecordInfo old_source_info = GetRecordInfo(old_source_item);
                    if (old_source_info.TitleMatch == false)
                    {
                        // ����title��һ������
                        strWarning = "Դ��¼ " + e.SourceRecPath + " ��������Ŀ���¼ " + strTargetRecPath + " ���������Ǻ�";
                    }
                    */

                    strWarning = "��ǰ�ֲᴰ�ڵļ�¼ " + e.SourceRecPath + " ���������մ������趨ΪԴ��ɫ�ļ�¼ " + strExistSourceRecPath + "��\r\n\r\nȷʵҪ����ǰ��ΪԴ��ɫ��������������?";
                    // TODO: ���������������У���ΪҪ���������Ƿ�ʵʩSetSource()����
                    DialogResult result = MessageBox.Show(this,
                        strWarning,
                        "AcceptForm",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }


            // ��Դ����Ϊ e.SourceRecPath
            ListViewItem source_item = ListViewUtil.FindItem(this.listView_accept_records,
                e.SourceRecPath,
                COLUMN_RECPATH);
            if (source_item == null)
            {
                strError = "�б��о�Ȼû���ҵ�·��Ϊ '" + e.SourceRecPath + "' ������";
                goto ERROR1;
            }

            // 2009/10/23
            // ��鵱ǰ���Ƿ��������ΪԴ��ɫ
            nRet = WarningSetSource(source_item);
            if (nRet == 0)
            {
                e.Cancel = true;
                return;
            }

            // 2009/2/16 �ƶ�������
            nRet = SetSource(source_item,
                "set",
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // ����Դ֮�󣬿���Դ�����Ƿ��Ѿ�Ϊб��
            RecordInfo source_info = GetRecordInfo(source_item);
            if (source_info.TitleMatch == false)
            {
                // ����title��һ������
                strWarning = "Դ��¼ '" + e.SourceRecPath + "' ��������Ŀ���¼ '" + strTargetRecPath + "' ���������Ǻ�";
                // TODO: ���������������У���ΪҪ���������Ƿ�ʵʩSetSource()����
                DialogResult result = MessageBox.Show(this,
                    strWarning + "\r\n\r\n��������? ",
                    "AcceptForm",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }


            ListViewItem target_item = ListViewUtil.FindItem(this.listView_accept_records,
                strTargetRecPath,
                COLUMN_RECPATH);
            if (target_item != null)    // 2008/12/3
            {
                // ����Ŀ�������Ƿ��Ѿ�Ϊб��
                RecordInfo target_info = GetRecordInfo(target_item);
                if (target_info.TitleMatch == false)
                {
                    strWarning = "Ŀ���¼ '" + strTargetRecPath + "' ��������Դ��¼ '" + e.SourceRecPath + "' ���������Ǻ�";
                    DialogResult result = MessageBox.Show(this,
                        strWarning + "\r\n\r\n��������? ",
                        "AcceptForm",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            e.TargetRecPath = strTargetRecPath;

            // 2009/11/5
            if (String.IsNullOrEmpty(strBiblioSourceRecPath) == false)
            {
                ListViewItem biblioSource_item = ListViewUtil.FindItem(this.listView_accept_records,
                    strBiblioSourceRecPath,
                    COLUMN_RECPATH);
                if (biblioSource_item != null)
                {
                    // ������Դ�����Ƿ��Ѿ�Ϊб��
                    RecordInfo biblioSource_info = GetRecordInfo(biblioSource_item);
                    if (biblioSource_info.TitleMatch == false)
                    {
                        strWarning = "��Դ��¼ '" + strBiblioSourceRecPath + "' ��������Դ��¼ '" + e.SourceRecPath + "' ���������Ǻ�";
                        DialogResult result = MessageBox.Show(this,
                            strWarning + "\r\n\r\n��������? ",
                            "AcceptForm",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result == DialogResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }

            e.BiblioSourceRecPath = strBiblioSourceRecPath;

            if (String.IsNullOrEmpty(e.BiblioSourceRecPath) == false)
            {
                string strXml = "";
                // ���һ����Ŀ��¼
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = GetBiblioXml(e.BiblioSourceRecPath,
                    out strXml,
                    out strError);
                if (nRet == 0 || nRet == -1)
                {
                    strError = "���ղ������ܾ�����ȡ��Դ��¼ '" + e.BiblioSourceRecPath + "' ʱ����: " + strError;
                    goto ERROR1;
                }
                e.BiblioSourceRecord = strXml;
                e.BiblioSourceSyntax = "xml";
            }

            bool bSeriesMode = false;
            if (this.comboBox_prepare_type.Text == "����������")
                bSeriesMode = true;

            // �ڿ�����£�Դ��ɫ��Ŀ���ɫ����Ϊͬһ��������һ�������Ҫ��2009/2/17
            if (bSeriesMode == true)
            {
                if (e.TargetRecPath != e.SourceRecPath)
                {
                    strError = "���ղ������ܾ�������������Ϊ�ڿ�ʱ��Դ��¼��Ŀ���¼����Ϊͬһ����(��������Դ��¼Ϊ "+e.SourceRecPath+"��Ŀ���¼Ϊ "+e.TargetRecPath+")";
                    goto ERROR1;
                }
            }

            string str998TargetRecPath = "";
            // ���Դ��¼��998$t��������ǰ�б����Ƿ��Ѿ���������¼�����û�У�����Ҫװ��
            // return:
            //      -1  ����(������������Ѿ�����)
            //      0   Դ��¼û��998$t
            //      1   ����������ǰĿ���¼�Ѿ����б��д���
            //      2   ��װ����Ŀ���¼
            nRet = AutoLoadTarget(e.SourceRecPath,
                out str998TargetRecPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string strSourceDbName = Global.GetDbName(e.SourceRecPath);
            OrderDbInfo source_dbinfo = this.db_infos.LocateByBiblioDbName(strSourceDbName);
            if (source_dbinfo == null)
            {
                strError = "��this.db_infos�о�Ȼû���ҵ�����Ϊ " + strSourceDbName + " ����Ŀ�����";
                goto ERROR1;
            }

            // ��� �ɹ������� ���
            if (bSeriesMode == false)
            {


                // Դ��¼���Բɹ������⣬Ŀ���¼��Դ��¼����ͬһ��
                if (source_dbinfo.IsOrderWork == true
                    && e.SourceRecPath != e.TargetRecPath)
                {
                    // ���ң�Ŀ���¼����Դ��¼998$tָ�������
                    if (String.IsNullOrEmpty(str998TargetRecPath) == false
                        && e.TargetRecPath != str998TargetRecPath)
                    {
                        strWarning = "��Ŀ�� '" + strSourceDbName + "' �Ľ�ɫΪ�ɹ������⣬һ������´˿��е�Դ��¼(" + e.SourceRecPath + ")ҲӦͬʱ��ΪĿ���¼��\r\n\r\n�Ƿ����Ҫ��(���趨��)��¼ '" + e.TargetRecPath + "' ��ΪĿ�ֱ꣬�������д�������Ϣ?\r\n\r\n------\r\n��(Yes): ����¼ '" + e.TargetRecPath + "' ��ΪĿ�꣬�������ղ����н�Դ��¼�е�998$t(Ŀǰ����Ϊ '" + str998TargetRecPath + "')����Ϊָ����ѡ�������Ŀ��(" + e.TargetRecPath + ")��\r\n��(No): ��������";
                        DialogResult result = MessageBox.Show(this,
                            strWarning,
                            "AcceptForm",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result == DialogResult.No)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }

                    // Ŀ���¼������Դ��¼998$tָ�������
                    else if (String.IsNullOrEmpty(str998TargetRecPath) == false
                        && e.TargetRecPath == str998TargetRecPath)
                    {
                        strWarning = "��Ŀ�� '" + strSourceDbName + "' �Ľ�ɫΪ�ɹ������⣬һ������´˿��е�Դ��¼(" + e.SourceRecPath + ")ҲӦͬʱ��ΪĿ���¼���������ڵ�ת�Ʋ�������Ȼ�Ὣ�����������¼ת�Ƶ����յ�Ŀ���¼ '" + str998TargetRecPath + "'�����������ڲ��ġ�\r\n\r\n�Ƿ����Ҫ��(���趨��)��¼ '" + e.TargetRecPath + "' ��ΪĿ�ֱ꣬�������д�������Ϣ?\r\n\r\n------\r\n��(Yes): ����¼ '" + e.TargetRecPath + "' ��ΪĿ�ꣻ\r\n��(No): ��������";
                        DialogResult result = MessageBox.Show(this,
                            strWarning,
                            "AcceptForm",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result == DialogResult.No)
                        {
                            e.Cancel = true;
                            return;
                        }

                    }
                    else
                    {
                        // Դ��¼û��998$t����ʱ������ڸ���Ѱ��һ�����յ�Ŀ���¼����������Ϊ�ڶ����Ժ������������ĳЩ��Ŀ��¼��������Ŀǰ��������һ��

                        // TODO: 
                        // ��Ҫ���e.SourceRecPath��¼�е�998$t�����������ָ��e.TargetRecPath����ôMessageBox����ʾ�ͼ��ԣ�
                        // �����ָ����e.TargetRecPath�ļ�¼������(yes)ѡ������£�Ҫ������ʾ�����£�
                        // ����(���������չ�����)����Դ��¼("+e.SourceRecPath+")�е�Ŀ����Ϣ(998$t)

                        strWarning = "��Ŀ�� '" + strSourceDbName + "' �Ľ�ɫΪ�ɹ������⣬һ������´˿��е�Դ��¼(" + e.SourceRecPath + ")ҲӦͬʱ��ΪĿ���¼��\r\n\r\n�Ƿ����Ҫ��(���趨��)��¼ '" + e.TargetRecPath + "' ��ΪĿ�ֱ꣬�������д�������Ϣ?\r\n\r\n------\r\n��(Yes): ����¼ '" + e.TargetRecPath + "' ��ΪĿ��\r\n��(No): ��Ϊ����¼ '" + e.SourceRecPath + "' ��ΪĿ��\r\nȡ��(Cancel): ��������";
                        DialogResult result = MessageBox.Show(this,
                            strWarning,
                            "AcceptForm",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result == DialogResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }

                        // �ָ�source��Ϊtarget
                        if (result == DialogResult.No)
                        {
                            e.TargetRecPath = e.SourceRecPath;
                            nRet = SetTarget(source_item,
                                "set",
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                            target_item = source_item;
                        }
                    }
                }
            }

            // ��� �ǲɹ������� ���
            // Դ��¼���� �ǲɹ������⣬Ŀ���¼��Դ��¼����ͬһ��
            if (source_dbinfo.IsOrderWork == false
                && e.SourceRecPath != e.TargetRecPath)
            {
                // ���ң�Ŀ���¼����Դ��¼998$tָ�������
                if (String.IsNullOrEmpty(str998TargetRecPath) == false
                    && e.TargetRecPath != str998TargetRecPath)
                {
                    strWarning = "Դ��¼ '" + e.SourceRecPath + "' ��998$tָ���Ŀ���¼Ϊ '" + str998TargetRecPath + "'�������趨��һ����ͬ��Ŀ���¼ '" + e.TargetRecPath + "'��\r\n\r\n�Ƿ����Ҫ��(���趨��)��¼ '" + e.TargetRecPath + "' ��ΪĿ�ֱ꣬�������д�������Ϣ?\r\n\r\n------\r\n��(Yes): ����¼ '" + e.TargetRecPath + "' ��ΪĿ�꣬�������ղ����н�Դ��¼�е�998$t(Ŀǰ����Ϊ '" + str998TargetRecPath + "')����Ϊָ����ѡ�������Ŀ��(" + e.TargetRecPath + ")��\r\n��(No): ��������";
                    DialogResult result = MessageBox.Show(this,
                        strWarning,
                        "AcceptForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }


            }


            return;
        ERROR1:
            e.ErrorInfo = strError;
            e.Cancel = true;
        }

        /// <summary>
        /// ��ǰ�������������ֲᴰ
        /// </summary>
        public EntityForm DetailWindow
        {
            get
            {
                if (m_detailWindow != null)
                {
                    if (m_detailWindow.IsDisposed == true)
                    {
                        m_detailWindow = null;
                        return null;
                    }
                    return m_detailWindow;
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// ��Ӧ MdiClient �ߴ�仯
        /// </summary>
        public void OnMdiClientSizeChanged()
        {
            AcceptForm_SizeChanged(this, null);
        }

        private void AcceptForm_SizeChanged(object sender, EventArgs e)
        {
            #if ACCEPT_MODE

            bool bRet = InitialSizeParam();

            if (bRet == true)
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                }

                this.m_nAcceptWindowHeight = this.Size.Height;


                this.Location = new Point(0, 0);
                this.Size = new Size(m_nMdiClientWidth, this.Size.Height);

                if (m_detailWindow != null)
                {
                    if (m_detailWindow.IsDisposed == true)
                    {
                        m_detailWindow = null;
                    }
                }

                if (m_detailWindow != null)
                {
                    m_detailWindow.Location = new Point(0, this.Size.Height);
                    m_detailWindow.Size = new Size(m_nMdiClientWidth, m_nMdiClientHeight - this.Size.Height);
                }
            }
#endif
        }

        public void EnableProgress()
        {
            this.MainForm.stopManager.Active(this.stop);
        }

        private void AcceptForm_Activated(object sender, EventArgs e)
        {
#if NO
            // 2009/8/13
            this.MainForm.stopManager.Active(this.stop);
#endif
            EnableProgress();

            if (m_detailWindow != null)
            {
                if (m_detailWindow.IsDisposed == true)
                {
                    m_detailWindow = null;
                }
            }

            if (m_detailWindow != null)
            {
                if (MainForm.IsTopTwoChildWindow(m_detailWindow) == false)
                    m_detailWindow.Activate();
            }
        }

        private void listView_accept_records_MouseDown(object sender, MouseEventArgs e)
        {

        }

        // popup menu
        private void listView_accept_records_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            string strRole = "";
            string strRecPath = "";
            if (this.listView_accept_records.SelectedItems.Count > 0)
            {
                strRole = ListViewUtil.GetItemText(this.listView_accept_records.SelectedItems[0],
                    COLUMN_ROLE);
                strRecPath = ListViewUtil.GetItemText(this.listView_accept_records.SelectedItems[0],
                    COLUMN_RECPATH);
            }

            // ���ݼ�¼·�������ListViewItem�����imageindex�±�
            // return:
            //      -2  ����������Ŀ��
            //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
            //      0   Դ
            //      1   Ŀ��
            //      2   ͬʱΪԴ��Ŀ��
            //      3   ��Դ
            int image_index = this.db_infos.GetItemType(strRecPath,
                            this.comboBox_prepare_type.Text);

            string strText = "";
            bool bEnable = true;

            if (StringUtil.IsInList("Դ", strRole) == true)
                strText = "ȥ����ɫ��Դ��(&S)";
            else
            {
                if (image_index != 0 && image_index != 2)
                    bEnable = false;
                strText = "���ý�ɫ��Դ��(&S)";
            }

            menuItem = new MenuItem(strText);
            menuItem.Click += new System.EventHandler(this.menu_toggleSource_Click);
            if (this.listView_accept_records.SelectedItems.Count == 0
                || bEnable == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            bEnable = true;
            if (StringUtil.IsInList("Ŀ��", strRole) == true)
                strText = "ȥ����ɫ��Ŀ�ꡱ(&T)";
            else
            {
                if (image_index != 1 && image_index != 2)
                    bEnable = false;
                strText = "���ý�ɫ��Ŀ�ꡱ(&T)";
            }

            menuItem = new MenuItem(strText);
            menuItem.Click += new System.EventHandler(this.menu_toggleTarget_Click);
            if (this.listView_accept_records.SelectedItems.Count == 0
                || bEnable == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            bEnable = true;
            if (StringUtil.IsInList("��Դ", strRole) == true)
                strText = "ȥ����ɫ����Դ��(&T)";
            else
            {
                if (image_index != 3)
                    bEnable = false;
                strText = "���ý�ɫ����Դ��(&T)";
            }

            menuItem = new MenuItem(strText);
            menuItem.Click += new System.EventHandler(this.menu_toggleBiblioSource_Click);
            if (this.listView_accept_records.SelectedItems.Count == 0
                || bEnable == false
                || this.comboBox_prepare_type.Text == "����������")
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ˢ����ѡ��� " + this.listView_accept_records.SelectedItems.Count.ToString() + " ������(&R)");
            menuItem.Click += new System.EventHandler(this.menu_refreshSelectedItems_Click);
            if (this.listView_accept_records.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            /*
            // cut ����
            menuItem = new MenuItem("����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_cutEntity_Click);
            if (this.listView_items.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // copy ����
            menuItem = new MenuItem("����(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copyEntity_Click);
            if (this.listView_items.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            bool bHasClipboardObject = false;
            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null
                || iData.GetDataPresent(typeof(ClipboardBookItemCollection)) == false)
                bHasClipboardObject = false;
            else
                bHasClipboardObject = true;

            // paste ճ��
            menuItem = new MenuItem("ճ��(&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteEntity_Click);
            if (bHasClipboardObject == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);
             * 
             * */

            menuItem = new MenuItem("�Ƴ���ѡ��� "+this.listView_accept_records.SelectedItems.Count.ToString()+" ������(&R)");
            menuItem.Click += new System.EventHandler(this.menu_removeSelectedItems_Click);
            if (this.listView_accept_records.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�ر��ֲᴰ(&C)");
            menuItem.Click += new System.EventHandler(this.menu_closeDetailWindow_Click);
            if (this.m_detailWindow == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.listView_accept_records, new Point(e.X, e.Y));
        }

        // ˢ����ѡ�������
        void menu_refreshSelectedItems_Click(object sender, EventArgs e)
        {
            this.EnableControls(false);
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ˢ��...");
            stop.BeginLoop();

            try
            {
                foreach (ListViewItem item in this.listView_accept_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                            return;
                    }

                    // ListViewItem item = this.listView_accept_records.SelectedItems[i];

                    string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

                    string strError = "";
                    int nRet = RefreshBrowseLine(item,
                        out strError);
                    if (nRet == -1)
                        ListViewUtil.ChangeItemText(item, 2, strError);
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                this.EnableControls(true);
            }
        }

        // ����ǰ����¼·�����Ѿ���ֵ
        /*public*/ int RefreshBrowseLine(ListViewItem item,
            out string strError)
        {
            strError = "";

            string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);
            string [] paths = new string[1];
            paths[0] = strRecPath;
            Record[] searchresults = null;

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
                    i + RESERVE_COLUMN_COUNT,
                    searchresults[0].Cols[i]);
            }

            int nRet = FilterOneItem(item, out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }


        void menu_closeDetailWindow_Click(object sender, EventArgs e)
        {
            // �ر� ������EntityForm
            CloseDetailWindow();
        }

        void menu_removeSelectedItems_Click(object sender, EventArgs e)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                for (int i = this.listView_accept_records.SelectedIndices.Count - 1; i >= 0; i--)
                {
                    ListViewItem item = this.listView_accept_records.SelectedItems[i];
                    string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

                    // �����Ƿ�Ϊ��Դ��ɫ
                    if (this.label_biblioSource.Tag != null)
                    {
                        if (strRecPath == (string)this.label_biblioSource.Tag)
                        {
                            this.SetLabelBiblioSource(null);
                            Debug.Assert(this.label_biblioSource.Tag == null, "");
                        }

                    }
                    // �����Ƿ�ΪԴ��ɫ
                    if (this.label_source.Tag != null)
                    {
                        if (strRecPath == (string)this.label_source.Tag)
                        {
                            this.SetLabelSource(null);
                            Debug.Assert(this.label_source.Tag == null, "");
                        }
                    }
                    // �����Ƿ�ΪĿ���ɫ
                    if (this.label_target.Tag != null)
                    {
                        if (strRecPath == (string)this.label_target.Tag)
                        {
                            this.SetLabelTarget(null);
                            Debug.Assert(this.label_target.Tag == null, "");
                        }
                    }

                    // �����Ƿ��Ѿ�װ���·����ֲᴰ
                    if (this.m_detailWindow != null && m_detailWindow.BiblioRecPath == strRecPath)
                    {
#if NO
                        if (this.SingleClickLoadDetail == true)
                            this.m_detailWindow.ReadOnly = true;
                        else
#endif
                            CloseDetailWindow();
                    }

                    this.listView_accept_records.Items.RemoveAt(this.listView_accept_records.SelectedIndices[i]);
                }

            }
            finally
            {
                this.Cursor = oldCursor;
            }
        }

        // TODO: ���õ�ʱ����Ҫ���һ�£�������ɫ�Ƿ�������ݿ�Ķ��塣
        // ���磬û�а������������Ŀ�⣬������ΪԴ��û�а���ʵ������Ŀ�⣬������ΪĿ��
        void menu_toggleSource_Click(object sender, EventArgs e)
        {
            string strError = "";
            if (this.listView_accept_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ�л���ɫ��Դ��������";
                goto ERROR1;
            }

            ListViewItem item = this.listView_accept_records.SelectedItems[0];

            int nRet = WarningSetSource(item);
            if (nRet == 0)
                return;
            /*
            RecordInfo info = GetRecordInfo(item);
            string strRole = ListViewUtil.GetItemText(item,
                COLUMN_ROLE);
            // �������á�Դ��
            if (StringUtil.IsInList("Դ",strRole) == false)
            {
                // �����Ƿ��ж�����Ϣ
                if (info.HasOrderInfo == false)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ��¼��δ������Ӧ�Ķ�����Ϣ��һ������²��ʺ���Ϊ��Դ����¼��\r\n\r\nʵ��Ҫǿ������Ϊ��Դ����",
                        "AcceptForm",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Cancel)
                        return;
                }
            }
             * */

            nRet = SetSource(item,
                "toggle",
                out strError);
            if (nRet == -1)
                goto ERROR1;

            /*
            string strRole = ListViewUtil.GetItemText(item,
                COLUMN_ROLE);

            if (StringUtil.IsInList("Դ", strRole) == true)
            {
                StringUtil.SetInList(ref strRole, "Դ", false);

                // ������label��
                SetLabelSource(null);
            }
            else
            {
                // ���ǰ�������������ݿ��Ƿ�߱���Ӧ������
                string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);
                // ���ݼ�¼·�������ListViewItem�����imageindex�±�
                // return:
                //      -2  ����������Ŀ��
                //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
                //      0   Դ
                //      1   Ŀ��
                //      2   ͬʱΪԴ��Ŀ��
        //      3   ��Դ
                int nRet = this.db_infos.GetItemType(strRecPath,
                            this.comboBox_prepare_type.Text);
                if (nRet == -2 || nRet == -1)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ�����Ͳɹ�ҵ���޹أ�������� Դ ��ɫ";
                    goto ERROR1;
                }
                if (nRet == 1)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ�Ϊ��Ŀ�ꡱ���ͣ�������� Դ ��ɫ";
                    goto ERROR1;
                }


                // ���
                StringUtil.SetInList(ref strRole, "Դ", true);

                // �������������������еĽ�ɫ��Դ��
                ClearRole("Դ", item);

                // ������label��
                SetLabelSource(ListViewUtil.GetItemText(item, COLUMN_RECPATH));
            }

            ListViewUtil.ChangeItemText(item,
                COLUMN_ROLE,
                strRole);
            SetItemColor(item);
             * */

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_toggleBiblioSource_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.comboBox_prepare_type.Text == "����������")
            {
                strError = "���������������֧�� ��Դ��ɫ";
                goto ERROR1;
            }

            if (this.listView_accept_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ�л���ɫ����Դ��������";
                goto ERROR1;
            }

            ListViewItem item = this.listView_accept_records.SelectedItems[0];

            int nRet = SetBiblioSource(item,
                "toggle",
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        void menu_toggleTarget_Click(object sender, EventArgs e)
        {
            string strError = "";
            if (this.listView_accept_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ�л���ɫ��Ŀ�ꡱ������";
                goto ERROR1;
            }

            ListViewItem item = this.listView_accept_records.SelectedItems[0];

            int nRet = SetTarget(item,
                "toggle",
                out strError);
            if (nRet == -1)
                goto ERROR1;

            /*

            string strRole = ListViewUtil.GetItemText(item,
                COLUMN_ROLE);

            if (StringUtil.IsInList("Ŀ��", strRole) == true)
            {
                // ȥ��
                StringUtil.SetInList(ref strRole, "Ŀ��", false);

                // ������label��
                SetLabelTarget(null);
            }
            else
            {
                // ���ǰ�������������ݿ��Ƿ�߱���Ӧ������
                string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);
                // ���ݼ�¼·�������ListViewItem�����imageindex�±�
                // return:
                //      -2  ����������Ŀ��
                //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
                //      0   Դ
                //      1   Ŀ��
                //      2   ͬʱΪԴ��Ŀ��
         //      3   ��Դ
               int nRet = this.db_infos.GetItemType(strRecPath,
                            this.comboBox_prepare_type.Text);
                if (nRet == -2 || nRet == -1)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ�����Ͳɹ�ҵ���޹أ�������� Ŀ�� ��ɫ";
                    goto ERROR1;
                }
                if (nRet == 0)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ�Ϊ��Դ�����ͣ�������� Ŀ�� ��ɫ";
                    goto ERROR1;
                }
  

                // ���
                StringUtil.SetInList(ref strRole, "Ŀ��", true);

                // �������������������еĽ�ɫ��Ŀ�ꡱ
                ClearRole("Ŀ��", item);

                // ������label��
                SetLabelTarget( ListViewUtil.GetItemText(item, COLUMN_RECPATH));
            }

            ListViewUtil.ChangeItemText(item,
                COLUMN_ROLE,
                strRole);
            SetItemColor(item);
             * */

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        static void SetItemColor(ListViewItem item)
        {
            string strRole = ListViewUtil.GetItemText(item, 1);

            RecordInfo info = GetRecordInfo(item);

            

            if (StringUtil.IsInList("Դ", strRole) == true)
            {
                // ���� Դ,Ŀ�� ����role�е����
                item.BackColor = Color.LightBlue;
                if (info.TitleMatch == true)
                    item.Font = new Font(item.Font, FontStyle.Bold);
                else
                    item.Font = new Font(item.Font, FontStyle.Bold | FontStyle.Italic);
            }
            else if (StringUtil.IsInList("Ŀ��", strRole) == true)
            {
                item.BackColor = Color.LightPink;
                if (info.TitleMatch == true)
                    item.Font = new Font(item.Font, FontStyle.Bold);
                else
                    item.Font = new Font(item.Font, FontStyle.Bold | FontStyle.Italic);
            }
            else if (StringUtil.IsInList("��Դ", strRole) == true)
            {
                item.BackColor = Color.LightGreen;
                if (info.TitleMatch == true)
                    item.Font = new Font(item.Font, FontStyle.Bold);
                else
                    item.Font = new Font(item.Font, FontStyle.Bold | FontStyle.Italic);
            }
            else
            {
                item.BackColor = SystemColors.Window;

                if (info.TitleMatch == true)
                    item.Font = new Font(item.Font, FontStyle.Regular);
                else
                    item.Font = new Font(item.Font, FontStyle.Regular | FontStyle.Italic);
            }

            // imageindex value:
            //      -2  ����������Ŀ��
            //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
            //      0   Դ
            //      1   Ŀ��
            //      2   ͬʱΪԴ��Ŀ��
            //      3   ��Դ
            if (item.ImageIndex == TYPE_NOT_ORDER
                || item.ImageIndex < 0
                || info.TitleMatch == false
                || (info.HasOrderInfo == false && item.ImageIndex == 0))  // ������Դ�����������������Ϣ���򷢻� 2009/10/23
                item.ForeColor = SystemColors.GrayText;
            else
                item.ForeColor = SystemColors.WindowText;
        }

        // ��������Դ��Ŀ����Ϣ
        void ClearSourceTarget()
        {
            this.label_source.Tag = null;
            this.label_target.Tag = null;
        }

        // �ɳ�tips source
        private void label_source_MouseHover(object sender, EventArgs e)
        {
            object o = this.label_source.Tag;
            string strText = "";
            if (o == null)
                strText = "Դ��δ����";
            else
                strText = "\r\nԴΪ " + (string)o + "\r\n";

            this.toolTip_info.Show(strText, this.label_source, 1000);
        }

        // �ɳ� tips target
        private void label_target_MouseHover(object sender, EventArgs e)
        {
            object o = this.label_target.Tag;
            string strText = "";
            if (o == null)
                strText = "Ŀ����δ����";
            else
                strText = "\r\nĿ��Ϊ " + (string)o + "\r\n";

            this.toolTip_info.Show(strText, this.label_target, 1000);
        }

        // ��һ����� source
        private void label_source_MouseClick(object sender, MouseEventArgs e)
        {
            OnClickLabel(this.label_source, false);
        }

        // ��һ����� target
        private void label_target_MouseClick(object sender, MouseEventArgs e)
        {
            OnClickLabel(this.label_target, false);
        }


        // ˫����� source
        private void label_source_DoubleClick(object sender, EventArgs e)
        {
            OnClickLabel(this.label_source, true);

        }

        // ˫����� target
        private void label_target_DoubleClick(object sender, EventArgs e)
        {
            OnClickLabel(this.label_target, true);
        }

        // ˫����� bibloSource
        private void label_biblioSource_DoubleClick(object sender, EventArgs e)
        {
            OnClickLabel(this.label_biblioSource, true);
        }
        private void label_biblioSource_MouseClick(object sender, MouseEventArgs e)
        {
            OnClickLabel(this.label_biblioSource, false);

        }

        // �ɳ� tips biblioSource
        private void label_biblioSource_MouseHover(object sender, EventArgs e)
        {
            object o = this.label_biblioSource.Tag;
            string strText = "";
            if (o == null)
                strText = "��Դ��δ����";
            else
                strText = "\r\n��ԴΪ " + (string)o + "\r\n";

            this.toolTip_info.Show(strText, this.label_target, 1000);
        }


        private void button_viewDatabaseDefs_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strOutputInfo = "";

            int nRet = GetAllDatabaseInfo(
                out strOutputInfo,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            XmlViewerForm xml_viewer = new XmlViewerForm();

            xml_viewer.MainForm = this.MainForm;
            xml_viewer.XmlString = strOutputInfo;
            xml_viewer.ShowDialog(this);

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        int GetAllDatabaseInfo(out string strOutputInfo,
    out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ȡȫ�����ݿ��� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.ManageDatabase(
                    stop,
                    "getinfo",
                    "",
                    "",
                    out strOutputInfo,
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

        ERROR1:
            return -1;
        }

        /// <summary>
        /// �����һ������ҳ
        /// </summary>
        public void ActivateFirstPage()
        {
            this.tabControl_main.SelectedTab = this.tabPage_prepare;
        }

        private void button_next_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_prepare)
            {
                if (String.IsNullOrEmpty(this.tabComboBox_prepare_batchNo.Text) == true)
                {
                    strError = "��δָ���������κ�";
                    goto ERROR1;
                }

                if (String.IsNullOrEmpty(this.comboBox_prepare_type.Text) == true)
                {
                    strError = "��δָ������������";
                    goto ERROR1;
                }

                if (this.comboBox_prepare_type.Text != "ͼ��"
                    && this.comboBox_prepare_type.Text != "����������")
                {
                    strError = "δ֪�ĳ��������� '" + this.comboBox_prepare_type.Text + "'";
                    goto ERROR1;
                }

                if (String.IsNullOrEmpty(GetDbNameListString()) == true)
                {
                    strError = "��δѡ�������������Ŀ��";
                    goto ERROR1;
                }

                // �л�����һ��page
                this.tabControl_main.SelectedTab = this.tabPage_accept;
                return;
            }
            if (this.tabControl_main.SelectedTab == this.tabPage_accept)
            {
                // ��ʹdetailWindow����
                SaveDetailWindowChanges();




                // �л�����һ��page
                this.tabControl_main.SelectedTab = this.tabPage_finish;
                return;
            }
            if (this.tabControl_main.SelectedTab == this.tabPage_finish)
            {
#if !ACCEPT_MODE
                this.MainForm.CurrentAcceptControl = null;
#endif
                this.Close();
                return;
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        #region source and target ���


        // �Զ����ø��еĽ�ɫ
        // TODO: �򻯸���
        void AutoSetLinesRole()
        {
            int nSourceItemCount = 0;   // Դ��ɫ������Ŀ��������˫��ɫ������Ŀ
            int nTargetItemCount = 0;   // Ŀ���ɫ������Ŀ��������˫��ɫ������Ŀ
            int nSourceAndTargetItemCount = 0;    // ͬʱ�߱�������ɫ��������Ŀ
            for (int i = 0; i < this.listView_accept_records.Items.Count; i++)
            {
                ListViewItem item = this.listView_accept_records.Items[i];

                if (item.ImageIndex == TYPE_SOURCE)
                    nSourceItemCount++;
                if (item.ImageIndex == TYPE_TARGET)
                    nTargetItemCount++;
                if (item.ImageIndex == TYPE_SOURCE_AND_TARGET)
                    nSourceAndTargetItemCount++;
            }

            // �����ǰ����һ��ͬʱΪԴ��Ŀ�������
            if (nSourceItemCount == 0 && nTargetItemCount == 0
                && nSourceAndTargetItemCount == 1)
            {
                string strRecPath = "";
                for (int i = 0; i < this.listView_accept_records.Items.Count; i++)
                {
                    ListViewItem item = this.listView_accept_records.Items[i];

                    if (item.ImageIndex == TYPE_SOURCE_AND_TARGET)
                    {
                        strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);
                        break;
                    }
                }

                SetLabelSource(strRecPath);
                SetLabelTarget(strRecPath);
                return;
            }

            // �����ǰ����һ��Դ���û��Ŀ�����Ҳû��˫������
            if (nSourceItemCount == 1 && nTargetItemCount == 0
                && nSourceAndTargetItemCount == 0)
            {
                string strRecPath = "";
                for (int i = 0; i < this.listView_accept_records.Items.Count; i++)
                {
                    ListViewItem item = this.listView_accept_records.Items[i];

                    if (item.ImageIndex == TYPE_SOURCE)
                    {
                        strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);
                        break;
                    }
                }

                SetLabelSource(strRecPath);
                SetLabelTarget("");
                // ������Ҫ����Ŀ���¼
                return;
            }


            // �����ǰ�������ɸ�Ŀ�����û��Դ���Ҳû��˫������
            // �����������֣�ֻ��˵��ָ��ISBN�ŵ�ͼ�鲢û�б�����(������ǰ�����ղع�)
            // ����һ������ʾ
            if (nSourceItemCount == 0 && nTargetItemCount >= 1
                && nSourceAndTargetItemCount == 0)
            {
            }


            // ��һ��Դ��������ж��Ŀ�������ʱ��ֻ���Զ�����Դ�������ʾѡ��Ŀ�����
            // �����ѡ��Ŀ��������������ʵ�������ա�
        }

        // ���б������ȫ���������ض��Ľ�ɫ�ַ���
        // parameters:
        //      exclude �����ʱ��Ҫ�����������
        void ClearRole(string strText,
            ListViewItem exclude)
        {
            for (int i = 0; i < this.listView_accept_records.Items.Count; i++)
            {
                ListViewItem item = this.listView_accept_records.Items[i];

                if (item == exclude)
                    continue;

                string strRole = ListViewUtil.GetItemText(item, COLUMN_ROLE);

                if (StringUtil.IsInList(strText, strRole) == true)
                {
                    StringUtil.SetInList(ref strRole, strText, false);
                    ListViewUtil.ChangeItemText(item, COLUMN_ROLE, strRole);
                    SetItemColor(item);
                }
            }
        }

        void OnClickLabel(System.Windows.Forms.Label label,
            bool bDoubleClick)
        {
            string strError = "";
            object o = label.Tag;
            if (o == null)
            {
                // ���������Ե�����
                Console.Beep();
                return;
            }

            string strRecPath = (string)o;

            ListViewItem item = ListViewUtil.FindItem(this.listView_accept_records, strRecPath, COLUMN_RECPATH);
            if (item == null)
            {
                strError = "�б��о�Ȼû���ҵ�·��Ϊ '" + strRecPath + "' ������";
                goto ERROR1;
            }

            if (bDoubleClick == false)
            {
                // ��ʾΪ��ǰ������(���ǲ���ֱ��ѡ���������⾪��listview������ϸ������)
                this.listView_accept_records.FocusedItem = item;
            }
            else
            {
                ListViewUtil.SelectLine(
                    item,
                    true);
            }
            item.EnsureVisible();

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �۲�һ��listview�����Ƿ��ܹ�������ΪbiblioSource?
        bool IsBiblioSourceable(ListViewItem item)
        {
            if (item == null)
                return false;

            string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

            // ���ݼ�¼·�������ListViewItem�����imageindex�±�
            // return:
            //      -2  ����������Ŀ��
            //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
            //      0   Դ
            //      1   Ŀ��
            //      2   ͬʱΪԴ��Ŀ��
            //      3   ��Դ
            int nRet = this.db_infos.GetItemType(strRecPath,
                        this.comboBox_prepare_type.Text);
            if (nRet == 3)
                return true;
            return false;
        }

        // �۲�һ��listview�����Ƿ��ܹ�������Ϊtarget?
        bool IsTargetable(ListViewItem item)
        {
            if (item == null)
                return false;

            string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

            // ���ݼ�¼·�������ListViewItem�����imageindex�±�
            // return:
            //      -2  ����������Ŀ��
            //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
            //      0   Դ
            //      1   Ŀ��
            //      2   ͬʱΪԴ��Ŀ��
            //      3   ��Դ
            int nRet = this.db_infos.GetItemType(strRecPath,
                        this.comboBox_prepare_type.Text);
            if (nRet == 1 || nRet == 2)
                return true;
            return false;
        }

        // �۲�һ��listview�����Ƿ��ܹ�������Ϊsource?
        bool IsSourceable(ListViewItem item)
        {
            if (item == null)
                return false;

            string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

            // ���ݼ�¼·�������ListViewItem�����imageindex�±�
            // return:
            //      -2  ����������Ŀ��
            //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
            //      0   Դ
            //      1   Ŀ��
            //      2   ͬʱΪԴ��Ŀ��
            //      3   ��Դ
            int nRet = this.db_infos.GetItemType(strRecPath,
                        this.comboBox_prepare_type.Text);
            if (nRet == 0 || nRet == 2)
                return true;
            return false;
        }

        static RecordInfo GetRecordInfo(ListViewItem item)
        {
            RecordInfo info = (RecordInfo)item.Tag;
            if (info == null)
            {
                info = new RecordInfo();
                item.Tag = info;
            }

            return info;
        }

        // return:
        //      -1  error
        //      0   not found title
        //      1   found title
        int GetRecordTitle(ListViewItem item,
            out string strTitle,
            out string strError)
        {
            strError = "";
            strTitle = "";

            RecordInfo info = GetRecordInfo(item);

            if (String.IsNullOrEmpty(info.BiblioTitle) == false)
            {
                // �Ѿ���ù���
                strTitle = info.BiblioTitle;
                return 1;
            }

            string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);
            // ���һ����Ŀ��¼��title
            // return:
            //      -1  error
            //      0   not found title
            //      1   found title
            int nRet = GetBiblioTitle(strRecPath,
                out strTitle,
                out strError);
            if (nRet == -1)
                return -1;

            return nRet;
        }

        // ����source�����title���������ϵ�target�������һ����ͻ��title���ϵģ�����title�����ϵ�
        // ���߸���target�����title���������ϵ�source�������һ����ͻ��title���ϵģ�����title�����ϵ�
        // parameters:
        //      start_item  ����仯��ListViewItem������Ϊsource����򱾺�����ȥ�޸�����target�������ʾ״̬�����Ϊtarget����򱾺�����ȥ�޸�����source�������ʾ״̬
        //      strRoles   Ҫȥ�޸ĵĽ�ɫ������ֵsource target biblioSource����start_item�Ľ�ɫ�෴������ʹ�ö��ż�����б�
        //      strAction   "set" ���� "reset"��"set"��ʾ��Ҫ����start_item��titleȥɸѡȷ�����������������ʾ״̬����"reset"��ʾȫ���ָ������ء���ʾ״̬���ɡ�
        int FilterTitle(
            ListViewItem start_item,
            string strRoles,
            string strAction,
            out string strError)
        {
            strError = "";

            Debug.Assert(strAction == "set" || strAction == "reset", "");

            string strSourceTitle = "";

            if (start_item != null)
            {
                Debug.Assert(strAction == "set", "������start_item��Ϊ�յ�ʱ��strAction����Ӧ��Ϊset");

                // return:
                //      -1  error
                //      0   not found title
                //      1   found title
                int nRet = GetRecordTitle(start_item,
                    out strSourceTitle,
                    out strError);
                if (nRet == -1)
                    return -1;

                RecordInfo source_info = GetRecordInfo(start_item);
                source_info.TitleMatch = true;

                SetItemColor(start_item);
            }
            else
            {
                Debug.Assert(strAction == "reset", "������start_itemΪ�յ�ʱ��strAction����Ӧ��Ϊreset");
            }


            for (int i = 0; i < this.listView_accept_records.Items.Count; i++)
            {
                ListViewItem item = this.listView_accept_records.Items[i];

                if (item == start_item)
                    continue;

                string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

                // 
                // ���ݼ�¼·�������ListViewItem�����imageindex�±�
                // return:
                //      -2  ����������Ŀ��
                //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
                //      0   Դ
                //      1   Ŀ��
                //      2   ͬʱΪԴ��Ŀ��
                //      3   ��Դ
                int nItemType = this.db_infos.GetItemType(strRecPath,
                    this.comboBox_prepare_type.Text);

                bool bFound = false;
                if (StringUtil.IsInList("target", strRoles) == true)
                {
                    if (nItemType == 1 || nItemType == 2)
                        bFound = true;
                }
                if (StringUtil.IsInList("source", strRoles) == true)
                {
                    if (nItemType == 0 || nItemType == 2)
                        bFound = true;
                }
                if (StringUtil.IsInList("biblioSource", strRoles) == true)
                {
                    if (nItemType == 3)
                        bFound = true;
                }

                if (bFound == false)
                    continue;

                string strCurrentTitle = "";

                // return:
                //      -1  error
                //      0   not found title
                //      1   found title
                int nRet = GetRecordTitle(item,
                    out strCurrentTitle,
                    out strError);
                if (nRet == -1)
                    return -1;

                RecordInfo info = GetRecordInfo(item);

                if (strAction == "reset")
                {
                    if (info.TitleMatch != true)
                    {
                        info.TitleMatch = true;
                        SetItemColor(item);
                    }
                    continue;
                }

                if (strSourceTitle.ToLower() == strCurrentTitle.ToLower())
                {
                    // ��ʾΪ ��
                    info.TitleMatch = true;
                }
                else
                {
                    // ��ʾΪ ��
                    info.TitleMatch = false;
                }

                SetItemColor(item);

            }

            return 0;
        }

        // 2008/10/22
        // ������ָ��·������������ΪĿ���¼
        int SetTarget(string strTargetRecPath,
            out string strError)
        {
            strError = "";

            ListViewItem item = ListViewUtil.FindItem(this.listView_accept_records,
                strTargetRecPath, COLUMN_RECPATH);
            if (item == null)
            {
                strError = "·��Ϊ " + strTargetRecPath + " ��ListViewItem����û���ҵ�";
                return -1;
            }

            int nRet = SetTarget(item,
                "set",
                out strError);
            if (nRet == -1)
                return -1;

            return nRet;
        }

        // ��һ��listview item�������á�ȥ�����л� biblioSource ��ɫ
        int SetBiblioSource(ListViewItem item,
            string strAction,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (item == null)
            {
                strError = "item == null";
                return -1;
            }

            if (this.comboBox_prepare_type.Text == "����������")
            {
                strError = "���������������֧�� ��Դ��ɫ";
                return -1;
            }

            Debug.Assert(item != null, "");
            Debug.Assert(strAction == "set" || strAction == "clear" || strAction == "toggle", "");

            string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

            string strRole = ListViewUtil.GetItemText(item,
                COLUMN_ROLE);

            if (StringUtil.IsInList("��Դ", strRole) == true)
            {
                if (strAction == "set")
                {
                    // �����ǲ���strRecPath
                    string strExistRecPath = GetTargetRecPath();
                    if (strExistRecPath == strRecPath)
                        return 0;

                    // ȥ��
                    StringUtil.SetInList(ref strRole, "��Դ", false);

                    // ������label��
                    SetLabelBiblioSource(null);

                    // ���
                    StringUtil.SetInList(ref strRole, "��Դ", true);

                    // ������label��
                    SetLabelBiblioSource(ListViewUtil.GetItemText(item, COLUMN_RECPATH));

                    nRet = FilterTitle(
                        item,
                        "source,target",
                        "set",
                        out strError);
                    if (nRet == -1)
                        return -1;


                    goto END1;

                }
                else if (strAction == "toggle" || strAction == "clear")
                {
                    // ȥ��
                    StringUtil.SetInList(ref strRole, "��Դ", false);

                    // ������label��
                    SetLabelBiblioSource(null);

                    nRet = FilterTitle(
                        null,
                        "source,target",
                        "reset",
                        out strError);
                    if (nRet == -1)
                        return -1;

                    goto END1;
                }

            }
            else
            {
                if (strAction == "clear")
                    return 0; // ������û��

                // ���ǰ�������������ݿ��Ƿ�߱���Ӧ������
                // string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

                // ���ݼ�¼·�������ListViewItem�����imageindex�±�
                // return:
                //      -2  ����������Ŀ��
                //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
                //      0   Դ
                //      1   Ŀ��
                //      2   ͬʱΪԴ��Ŀ��
                //      3   ��Դ
                nRet = this.db_infos.GetItemType(strRecPath,
                            this.comboBox_prepare_type.Text);
                if (nRet == -2 || nRet == -1)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ������" + this.comboBox_prepare_type.Text + "�ɹ�ҵ���޹أ�������� ��Դ ��ɫ";
                    goto ERROR1;
                }
                if (nRet == 0)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ�Ϊ��Դ�����ͣ�������� ��Դ ��ɫ";
                    goto ERROR1;
                }
                if (nRet == 1 || nRet == 2)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ�Ϊ��Ŀ�ꡱ���ͣ�������� ��Դ ��ɫ";
                    goto ERROR1;
                }

                // ���
                StringUtil.SetInList(ref strRole, "��Դ", true);

                // �������������������еĽ�ɫ����Դ��
                ClearRole("��Դ", item);

                // ������label��
                SetLabelBiblioSource(ListViewUtil.GetItemText(item, COLUMN_RECPATH));

                nRet = FilterTitle(
                    item,
                    "source,target",
                    "set",
                    out strError);
                if (nRet == -1)
                    return -1;

            }

        END1:
            ListViewUtil.ChangeItemText(item,
                COLUMN_ROLE,
                strRole);
            SetItemColor(item);

            return 0;
        ERROR1:
            return -1;
        }

        // ��һ��listview item�������á�ȥ�����л� target ��ɫ
        int SetTarget(ListViewItem item,
            string strAction,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (item == null)
            {
                strError = "item == null";
                return -1;
            }

            Debug.Assert(item != null, "");
            Debug.Assert(strAction == "set" || strAction == "clear" || strAction == "toggle", "");

            string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

            string strRole = ListViewUtil.GetItemText(item,
                COLUMN_ROLE);

            if (StringUtil.IsInList("Ŀ��", strRole) == true)
            {
                if (strAction == "set")
                {
                    // �����ǲ���strRecPath
                    string strExistRecPath = GetTargetRecPath();
                    if (strExistRecPath == strRecPath)
                        return 0;

                    // ȥ��
                    StringUtil.SetInList(ref strRole, "Ŀ��", false);

                    // ������label��
                    SetLabelTarget(null);

                    // ���
                    StringUtil.SetInList(ref strRole, "Ŀ��", true);

                    // ������label��
                    SetLabelTarget(ListViewUtil.GetItemText(item, COLUMN_RECPATH));

                    nRet = FilterTitle(
                        item,
                        "source,biblioSource", // "source",
                        "set",
                        out strError);
                    if (nRet == -1)
                        return -1;


                    goto END1;

                }
                else if (strAction == "toggle" || strAction == "clear")
                {
                    // ȥ��
                    StringUtil.SetInList(ref strRole, "Ŀ��", false);

                    // ������label��
                    SetLabelTarget(null);

                    nRet = FilterTitle(
                        null,
                        "source,biblioSource", // "source",
                        "reset",
                        out strError);
                    if (nRet == -1)
                        return -1;


                    goto END1;
                }

            }
            else
            {
                if (strAction == "clear")
                    return 0; // ������û��

                // ���ǰ�������������ݿ��Ƿ�߱���Ӧ������
                // string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

                // ���ݼ�¼·�������ListViewItem�����imageindex�±�
                // return:
                //      -2  ����������Ŀ��
                //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
                //      0   Դ
                //      1   Ŀ��
                //      2   ͬʱΪԴ��Ŀ��
                //      3   ��Դ
                nRet = this.db_infos.GetItemType(strRecPath,
                            this.comboBox_prepare_type.Text);
                if (nRet == -2 || nRet == -1)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ������" + this.comboBox_prepare_type.Text + "�ɹ�ҵ���޹أ�������� Ŀ�� ��ɫ";
                    goto ERROR1;
                }
                if (nRet == 0)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ�Ϊ��Դ�����ͣ�������� Ŀ�� ��ɫ";
                    goto ERROR1;
                }
                if (nRet == 3)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ�Ϊ����Դ�����ͣ�������� Ŀ�� ��ɫ";
                    goto ERROR1;
                }

                // ���
                StringUtil.SetInList(ref strRole, "Ŀ��", true);

                // �������������������еĽ�ɫ��Ŀ�ꡱ
                ClearRole("Ŀ��", item);

                // ������label��
                SetLabelTarget(ListViewUtil.GetItemText(item, COLUMN_RECPATH));

                nRet = FilterTitle(
                    item,
                    "source,biblioSource",  // "source",
                    "set",
                    out strError);
                if (nRet == -1)
                    return -1;

            }

        END1:
            ListViewUtil.ChangeItemText(item,
                COLUMN_ROLE,
                strRole);
            SetItemColor(item);

            return 0;
        ERROR1:
            return -1;
        }

        // ��һ��listview item��������(set)��ȥ��(clear)���л�(toggle) source ��ɫ
        // toggle����˼�����к���֮���л�
        int SetSource(ListViewItem item,
            string strAction,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (item == null)
            {
                strError = "item == null";
                return -1;
            }

            Debug.Assert(item != null, "");
            Debug.Assert(strAction == "set" || strAction == "clear" || strAction == "toggle", "");

            string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

            string strRole = ListViewUtil.GetItemText(item,
                COLUMN_ROLE);

            if (StringUtil.IsInList("Դ", strRole) == true)
            {

                if (strAction == "set")
                {
                    // �����ǲ���strRecPath
                    string strExistRecPath = GetSourceRecPath();
                    if (strExistRecPath == strRecPath)
                        return 0;

                    // ȥ��
                    StringUtil.SetInList(ref strRole, "Դ", false);

                    // ������label��
                    SetLabelSource(null);

                    // ���
                    StringUtil.SetInList(ref strRole, "Դ", true);

                    // ������label��
                    SetLabelSource(strRecPath);

                    nRet = FilterTitle(
                        item,
                        "target,biblioSource", // "target",
                        "set",
                        out strError);
                    if (nRet == -1)
                        return -1;


                    goto END1;

                }
                else if (strAction == "toggle" || strAction == "clear")
                {
                    // ȥ��
                    StringUtil.SetInList(ref strRole, "Դ", false);

                    // ������label��
                    SetLabelSource(null);

                    nRet = FilterTitle(
                        null,
                        "target,biblioSource", //"target",
                        "reset",
                        out strError);
                    if (nRet == -1)
                        return -1;


                    goto END1;
                }
            }
            else
            {
                if (strAction == "clear")
                    return 0; // ������û��

                // ���ǰ�������������ݿ��Ƿ�߱���Ӧ������
                // string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

                // ���ݼ�¼·�������ListViewItem�����imageindex�±�
                // return:
                //      -2  ����������Ŀ��
                //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
                //      0   Դ
                //      1   Ŀ��
                //      2   ͬʱΪԴ��Ŀ��
                //      3   ��Դ
                nRet = this.db_infos.GetItemType(strRecPath,
                            this.comboBox_prepare_type.Text);
                if (nRet == -2 || nRet == -1)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ������" + this.comboBox_prepare_type.Text + "�ɹ�ҵ���޹أ�������� Դ ��ɫ";
                    goto ERROR1;
                }
                if (nRet == 1)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ�Ϊ��Ŀ�ꡱ���ͣ�������� Դ ��ɫ";
                    goto ERROR1;
                }
                if (nRet == 3)
                {
                    strError = "��¼ '" + strRecPath + "' ���ڵ����ݿ�Ϊ����Դ�����ͣ�������� Դ ��ɫ";
                    goto ERROR1;
                }

                // ���
                StringUtil.SetInList(ref strRole, "Դ", true);

                // �������������������еĽ�ɫ��Դ��
                ClearRole("Դ", item);

                // ������label��
                SetLabelSource(ListViewUtil.GetItemText(item, COLUMN_RECPATH));

                nRet = FilterTitle(
                    item,
                    "target,biblioSource", // "target",
                    "set",
                    out strError);
                if (nRet == -1)
                    return -1;

            }

        END1:
            ListViewUtil.ChangeItemText(item,
                COLUMN_ROLE,
                strRole);
            SetItemColor(item);

            return 0;
        ERROR1:
            return -1;
        }

        void SetLabelSource(string strRecPath)
        {
            if (String.IsNullOrEmpty(strRecPath) == true)
            {
                this.label_source.Text = "Դ(��)";
                this.label_source.Font = new Font(this.label_source.Font, FontStyle.Regular);
                this.label_source.Tag = null;
            }
            else
            {
                this.label_source.Text = "Դ";
                this.label_source.Font = new Font(this.label_source.Font, FontStyle.Bold);
                this.label_source.Tag = strRecPath;
            }
        }

        void SetLabelBiblioSource(string strRecPath)
        {
            if (String.IsNullOrEmpty(strRecPath) == true)
            {
                this.label_biblioSource.Text = "��Դ(��)";
                this.label_biblioSource.Font = new Font(this.label_target.Font, FontStyle.Regular);
                this.label_biblioSource.Tag = null;
            }
            else
            {
                this.label_biblioSource.Text = "��Դ";
                this.label_biblioSource.Font = new Font(this.label_target.Font, FontStyle.Bold);
                this.label_biblioSource.Tag = strRecPath;
            }
        }

        void SetLabelTarget(string strRecPath)
        {
            if (String.IsNullOrEmpty(strRecPath) == true)
            {
                this.label_target.Text = "Ŀ��(��)";
                this.label_target.Font = new Font(this.label_target.Font, FontStyle.Regular);
                this.label_target.Tag = null;
            }
            else
            {
                this.label_target.Text = "Ŀ��";
                this.label_target.Font = new Font(this.label_target.Font, FontStyle.Bold);
                this.label_target.Tag = strRecPath;
            }
        }

        string GetBiblioSourceRecPath()
        {
            object o = this.label_biblioSource.Tag;
            if (o == null)
                return "";
            return (string)o;
        }

        string GetTargetRecPath()
        {
            object o = this.label_target.Tag;
            if (o == null)
                return "";
            return (string)o;
        }

        string GetSourceRecPath()
        {
            object o = this.label_source.Tag;
            if (o == null)
                return "";
            return (string)o;
        }

        // ������ Դ ��ɫǰ������û�ж�����Ϣ
        // return:
        //      0   ����
        //      1   ����
        int WarningSetSource(ListViewItem item)
        {
            RecordInfo info = GetRecordInfo(item);
            string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);
            string strRole = ListViewUtil.GetItemText(item,
                COLUMN_ROLE);
            // �������á�Դ��
            if (StringUtil.IsInList("Դ", strRole) == false)
            {
                // �����Ƿ��ж�����Ϣ
                if (info.HasOrderInfo == false)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ��¼ '" + strRecPath + "' ��δ������Ӧ�Ķ�����Ϣ��һ������²��ʺ���Ϊ��Դ����¼��\r\n\r\nʵ��Ҫǿ������Ϊ��Դ����",
                        "AcceptForm",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Cancel)
                        return 0;
                }
            }

            return 1;
        }

        #endregion

        #region drag and drop ���

        private void label_source_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                string strRecPath = (String)e.Data.GetData("Text");

                ListViewItem item = ListViewUtil.FindItem(this.listView_accept_records, strRecPath, COLUMN_RECPATH);

                // �۲�һ��listview�����Ƿ��ܹ�������Ϊsource?
                if (IsSourceable(item) == false)
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }

                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private void label_source_DragDrop(object sender, DragEventArgs e)
        {
            string strError = "";

            string strRecPath = (String)e.Data.GetData("Text");

            ListViewItem item = ListViewUtil.FindItem(this.listView_accept_records, strRecPath, COLUMN_RECPATH);
            if (item == null)
            {
                strError = "�б��о�Ȼû���ҵ�·��Ϊ '" + strRecPath + "' ������";
                goto ERROR1;
            }

            int nRet = WarningSetSource(item);
            if (nRet == 0)
                return;
            /*
            RecordInfo info = GetRecordInfo(item);
            string strRole = ListViewUtil.GetItemText(item,
                COLUMN_ROLE);
            // �������á�Դ��
            if (StringUtil.IsInList("Դ", strRole) == false)
            {
                // �����Ƿ��ж�����Ϣ
                if (info.HasOrderInfo == false)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ��¼ '"+strRecPath+"' ��δ������Ӧ�Ķ�����Ϣ��һ������²��ʺ���Ϊ��Դ����¼��\r\n\r\nʵ��Ҫǿ������Ϊ��Դ����",
                        "AcceptForm",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Cancel)
                        return;
                }
            }
             * */

            nRet = SetSource(item,
                "toggle",
                out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        private void label_target_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                string strRecPath = (String)e.Data.GetData("Text");

                ListViewItem item = ListViewUtil.FindItem(this.listView_accept_records, strRecPath, COLUMN_RECPATH);

                // �۲�һ��listview�����Ƿ��ܹ�������Ϊtarget?
                if (IsTargetable(item) == false)
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }

                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private void label_target_DragDrop(object sender, DragEventArgs e)
        {
            string strError = "";

            string strRecPath = (String)e.Data.GetData("Text");

            ListViewItem item = ListViewUtil.FindItem(this.listView_accept_records, strRecPath, COLUMN_RECPATH);
            if (item == null)
            {
                strError = "�б��о�Ȼû���ҵ�·��Ϊ '" + strRecPath + "' ������";
                goto ERROR1;
            }

            int nRet = SetTarget(item,
                "toggle",
                out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }



        private void label_biblioSource_DragDrop(object sender, DragEventArgs e)
        {
            string strError = "";

            string strRecPath = (String)e.Data.GetData("Text");

            ListViewItem item = ListViewUtil.FindItem(this.listView_accept_records, strRecPath, COLUMN_RECPATH);
            if (item == null)
            {
                strError = "�б��о�Ȼû���ҵ�·��Ϊ '" + strRecPath + "' ������";
                goto ERROR1;
            }

            int nRet = SetBiblioSource(item,
                "toggle",
                out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void label_biblioSource_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                string strRecPath = (String)e.Data.GetData("Text");

                ListViewItem item = ListViewUtil.FindItem(this.listView_accept_records, strRecPath, COLUMN_RECPATH);

                // �۲�һ��listview�����Ƿ��ܹ�������Ϊtarget?
                if (IsBiblioSourceable(item) == false)
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }

                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;
        }


        private void listView_accept_records_ItemDrag(object sender,
            ItemDragEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.listView_accept_records.DoDragDrop(
                    ListViewUtil.GetItemText((ListViewItem)e.Item, COLUMN_RECPATH),
                    DragDropEffects.Link);
            }

        }

        private void listView_accept_records_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private void listView_accept_records_DragDrop(object sender, DragEventArgs e)
        {
            // TODO: ���ʶ����Լ�drag���������?

            string strWhole = (String)e.Data.GetData("Text");

            DoPasteTabbedText(strWhole,
                false);
            return;
        }


        #endregion



        /*
        // ���(�ɹ�)Դ���ݿ����б�
        // ��νԴ���ݿ���ǿ����а������������Щ
        List<string> GetOrderSourceDbNames()
        {
            List<string> results = new List<string>();
            for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
            {
                BiblioDbProperty property = this.MainForm.BiblioDbProperties[i];
                if (String.IsNullOrEmpty(property.OrderDbName) == false)
                    results.Add(property.DbName);
            }

            return results;
        }

        // ���(�ɹ�)����Ŀ�����ݿ����б�
        // ��νĿ�����ݿ���ǿ����а���ʵ�������Щ
        // ���Դ���Ѿ�ȷ������ôĿ���(Ŀǰ)ֻ������Щ��Դ��MarcSyntax��ͬ��һ���֡�
        List<string> GetOrderTargetDbNames(string strSourceSyntax)
        {
            List<string> results = new List<string>();
            for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
            {
                BiblioDbProperty property = this.MainForm.BiblioDbProperties[i];
                if (String.IsNullOrEmpty(property.ItemDbName) == false)
                {
                    if (String.IsNullOrEmpty(strSourceSyntax) == false)
                    {
                        Debug.Assert(String.IsNullOrEmpty(property.Syntax) == true, "���ܳ��ֿ�ֵ��syntax");
                        if (property.Syntax.ToLower() == strSourceSyntax.ToLower())
                            results.Add(property.DbName);
                    }
                    else
                    {
                        results.Add(property.DbName);
                    }
                }
            }

            return results;
        }
         * */

        /*
        // �����ͨ���ݿⶨ��
        public int GetDatabaseInfo(
            string strDbName,
            out string strOutputInfo,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ȡ���ݿ� " + strDbName + " �Ķ���...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.GetSystemParameter(
                    stop,
                    "database_def",
                    strDbName,
                    out strOutputInfo,
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

        ERROR1:
            return -1;
        }
         * */

        // ���һ����Ŀ��¼
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetBiblioXml(string strRecPath,
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ȡ��Ŀ��¼ ...");
            stop.BeginLoop();

            try
            {
                string[] formats = new string[1];
                formats[0] = "xml";
                string[] results = null;
                byte[] timestamp = null;

                Debug.Assert(String.IsNullOrEmpty(strRecPath) == false, "strRecPathֵ����Ϊ��");

                long lRet = this.Channel.GetBiblioInfos(
                    stop,
                    strRecPath,
                    "",
                    formats,
                    out results,
                    out timestamp,
                    out strError);
                if (lRet == 0)
                {
                    if (String.IsNullOrEmpty(strError) == true)
                        strError = "��¼ '" + strRecPath + "' û���ҵ�";
                    return 0;
                }

                if (lRet == -1)
                {
                    strError = "�����Ŀxmlʱ��������: " + strError;
                    return -1;
                }
                Debug.Assert(results != null && results.Length == 1, "results�������1��Ԫ��");
                strXml = results[0];

                return 1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }
        }

        // ���һ����Ŀ��¼��title
        // return:
        //      -1  error
        //      0   not found title
        //      1   found title
        int GetBiblioTitle(string strRecPath,
            out string strTitle,
            out string strError)
        {
            strError = "";
            strTitle = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ȡ��Ŀ���� ...");
            stop.BeginLoop();

            try
            {
                string[] formats = new string[1];
                formats[0] = "@title";
                string[] results = null;
                byte[] timestamp = null;

                Debug.Assert(String.IsNullOrEmpty(strRecPath) == false, "strRecPathֵ����Ϊ��");

                long lRet = this.Channel.GetBiblioInfos(
                    stop,
                    strRecPath,
                    "",
                    formats,
                    out results,
                    out timestamp,
                    out strError);
                if (lRet == 0)
                {
                    return 0;
                }

                if (lRet == -1)
                {
                    strError = "�����Ŀtitleʱ��������: " + strError;
                    return -1;
                }
                Debug.Assert(results != null && results.Length == 1, "results�������1��Ԫ��");
                strTitle = results[0];

                return 1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }
        }

        private void tabControl_main_SelectedIndexChanged(object sender, EventArgs e)
        {
            // ������������ֵ
            if (this.comboBox_prepare_type.Text != "ͼ��"
                && this.comboBox_prepare_type.Text != "����������")
            {
                MessageBox.Show(this, "���������ͱ���Ϊ ͼ�� �� ����������");
                this.button_next.Enabled = false;
                return;
            }

            /*
            // ����ISBN/ISSN��ǩ
            if (this.comboBox_prepare_type.Text == "ͼ��")
                this.label_isbnIssn.Text = "ISBN(&I):";
            else
            {
                Debug.Assert(this.comboBox_prepare_type.Text == "����������", "");
                this.label_isbnIssn.Text = "ISSN(&I):";
            }
             * */
            if (this.tabControl_main.SelectedTab == this.tabPage_accept)
            {
                /*
                // ������;���Ƿ�ͳ���������ì��
                string strFromStyle = "";

                try
                {
                    strFromStyle = this.MainForm.GetBiblioFromStyle(this.comboBox_accept_from.Text);
                }
                catch
                {
                }

                if (this.comboBox_prepare_type.Text == "ͼ��")
                {
                    if (strFromStyle.ToLower() == "issn")
                    {
                        MessageBox.Show(this, "���棺����������Ϊ ͼ�� ʱ������ ISSN �����������С�������ѡ���ʵ��ļ���;��");
                        this.comboBox_accept_from.Focus();
                    }
                }
                else
                {
                    if (strFromStyle.ToLower() == "isbn")
                    {
                        MessageBox.Show(this, "���棺����������Ϊ ���������� ʱ������ ISBN �����������С�������ѡ���ʵ��ļ���;��");
                        this.comboBox_accept_from.Focus();
                    }
                }*/

                // �۲����;�������ͼ��ʱΪISSN���ڿ�ʱΪISBN�����޸���
                try
                {
                    string strFromStyle = "";
                    string strFromCaption = "";

                    strFromStyle = this.MainForm.GetBiblioFromStyle(this.comboBox_accept_from.Text);


                    if (this.comboBox_prepare_type.Text == "ͼ��")
                    {
                        if (strFromStyle.ToLower() == "issn")
                        {
                            strFromCaption = this.MainForm.GetBiblioFromCaption("isbn");
                            if (String.IsNullOrEmpty(strFromCaption) == false)
                                this.comboBox_accept_from.Text = strFromCaption;
                        }
                    }
                    else
                    {
                        if (strFromStyle.ToLower() == "isbn")
                        {
                            strFromCaption = this.MainForm.GetBiblioFromCaption("issn");
                            if (String.IsNullOrEmpty(strFromCaption) == false)
                                this.comboBox_accept_from.Text = strFromCaption;
                        }
                    }
                }
                catch
                {
                }


                if (this.comboBox_prepare_type.Text == "����������")
                {
                    this.label_biblioSource.Visible = false;
                }
                else
                {
                    this.label_biblioSource.Visible = true;
                }
            }

            this.SetNextButtonEnable();
        }

        private void button_finish_printAcceptList_Click(object sender, EventArgs e)
        {
            PrintAcceptForm print_form = this.MainForm.EnsurePrintAcceptForm();

            Debug.Assert(print_form != null, "");

            print_form.Activate();

            // 2009/2/4
            print_form.PublicationType = this.comboBox_prepare_type.Text;

            // �������κż���װ������
            // parameters:
            //      strDefaultBatchNo   ȱʡ�����κš����Ϊnull�����ʾ��ʹ�����������
            print_form.LoadFromAcceptBatchNo(this.tabComboBox_prepare_batchNo.Text);

        }

        private void tabComboBox_prepare_batchNo_TextChanged(object sender, EventArgs e)
        {
            SetWindowTitle();
            this.SetNextButtonEnable();
        }

        private void comboBox_prepare_type_TextChanged(object sender, EventArgs e)
        {
            SetWindowTitle();
            this.SetNextButtonEnable();
        }

        void SetWindowTitle()
        {
            this.Text = "����";

            if (this.tabComboBox_prepare_batchNo.Text != "")
                this.Text += " ���κ�: " + this.tabComboBox_prepare_batchNo.Text;
            if (this.comboBox_prepare_type.Text != "")
                this.Text += " ����: " + this.comboBox_prepare_type.Text;
        }

        int m_nInDropDown = 0;

        private void tabComboBox_prepare_batchNo_DropDown(object sender, EventArgs e)
        {
            // ��ֹ����
            if (this.m_nInDropDown > 0)
                return;

            ComboBox combobox = (ComboBox)sender;
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {
                if (combobox.Items.Count == 0
                    && this.GetBatchNoTable != null)
                {
                    GetKeyCountListEventArgs e1 = new GetKeyCountListEventArgs();
                    this.GetBatchNoTable(this, e1);

                    if (e1.KeyCounts != null)
                    {
                        for (int i = 0; i < e1.KeyCounts.Count; i++)
                        {
                            KeyCount item = e1.KeyCounts[i];
                            combobox.Items.Add(item.Key + "\t" + item.Count.ToString() + "��");
                        }
                    }
                    else
                    {
                        combobox.Items.Add("<not found>");
                    }
                }
            }
            finally
            {
                this.Cursor = oldCursor;
                this.m_nInDropDown--;
            }
        }


        // �����ַ�������ListViewItem��
        // ��������Global.BuildListViewItem��������������һ������ĵڶ���Ŀ��������ʾ��ɫ��Ϣ
        // �ַ����ĸ�ʽΪ\t�����
        // parameters:
        //      list    ����Ϊnull�����Ϊnull����û���Զ���չ�б�����Ŀ�Ĺ���
        static ListViewItem BuildAcceptListViewItem(
            ListView list,
            string strLine)
        {
            ListViewItem item = new ListViewItem();
            string[] parts = strLine.Split(new char[] { '\t' });
            for (int i = 0,j=0; i < parts.Length; i++,j++)
            {
                // �����ڶ���
                if (j == 1)
                    j++;

                ListViewUtil.ChangeItemText(item, j, parts[i]);

                // ȷ���б�����Ŀ��
                if (list != null)
                    ListViewUtil.EnsureColumns(list, parts.Length, 100);

            }

            return item;
        }

        void DoPasteTabbedText(string strWhole,
            bool bInsertBefore)
        {
            int index = -1;

            int nSkipCount = 0;

            if (this.listView_accept_records.SelectedIndices.Count > 0)
                index = this.listView_accept_records.SelectedIndices[0];

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.EnableControls(false);
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����װ�ؼ�¼ ...");
            stop.BeginLoop();

            try
            {

                // this.listView_accept_records.SelectedItems.Clear();

                string[] lines = strWhole.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    ListViewItem item = BuildAcceptListViewItem(
                        this.listView_accept_records,
                        lines[i]);

                    string strPath = item.Text;

                    // ������ݿ����Ƿ�Ϊ�Ϸ���Ŀ����
                    string strDbName = Global.GetDbName(strPath);
                    if (MainForm.IsBiblioDbName(strDbName) == false)
                    {
                        nSkipCount++;
                        continue;
                    }

                    if (index == -1)
                        this.listView_accept_records.Items.Add(item);
                    else
                    {
                        if (bInsertBefore == true)
                            this.listView_accept_records.Items.Insert(index, item);
                        else
                            this.listView_accept_records.Items.Insert(index + 1, item);

                        index++;
                    }

                    // 
                    // ���ݼ�¼·�������ListViewItem�����imageindex�±�
                    // return:
                    //      -2  ����������Ŀ��
                    //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
                    //      0   Դ
                    //      1   Ŀ��
                    //      2   ͬʱΪԴ��Ŀ��
                    //      3   ��Դ
                    int image_index = this.db_infos.GetItemType(strPath,
                        this.comboBox_prepare_type.Text);
                    // Debug.Assert(image_index != -2, "��Ȼ����������Ŀ��ļ�¼?");
                    item.ImageIndex = image_index;

                    SetItemColor(item); //

                    string strError = "";
                    int nRet = RefreshBrowseLine(item, out strError);
                    if (nRet == -1)
                    {
                        ListViewUtil.ChangeItemText(item, 2, strError);
                    }

                    item.Selected = true;
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                this.EnableControls(true);

                this.Cursor = oldCursor;
            }

            if (nSkipCount > 0)
            {
                MessageBox.Show(this, "�� " +nSkipCount.ToString()+" ��������Ŀ����������");
            }
        }

        private void comboBox_prepare_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            // ���������ͱ仯���б���ԭ�е����������������ⷢ�����
            this.listView_accept_records.Items.Clear();

            // ��ʹ���»���������κ��б�
            this.tabComboBox_prepare_batchNo.Items.Clear(); 

            // ˢ�²����������Ŀ�����б�
            this.FillDbNameList();
        }

        private void listView_accept_records_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewUtil.OnColumnClick(this.listView_accept_records, e);
        }

        private void checkedListBox_prepare_dbNames_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.CurrentValue == CheckState.Unchecked
                && e.NewValue == CheckState.Checked)
            {
                string strText = (string)this.checkedListBox_prepare_dbNames.Items[e.Index];
                if (strText.Length > 0 && strText[0] == '<')
                {
                    // �����������checked���
                    for(int i=0;i<this.checkedListBox_prepare_dbNames.Items.Count;i++)
                    {
                        if (i == e.Index)
                            continue;
                        if (this.checkedListBox_prepare_dbNames.GetItemChecked(i) == true)
                            this.checkedListBox_prepare_dbNames.SetItemChecked(i, false);
                    }
                }
                else
                {
                    // ��"<...>"�����checked���
                    string strFirstItemText = (string)this.checkedListBox_prepare_dbNames.Items[0];
                    if (strFirstItemText.Length > 0 && strFirstItemText[0] == '<')
                    {
                        if (this.checkedListBox_prepare_dbNames.GetItemChecked(0) == true)
                            this.checkedListBox_prepare_dbNames.SetItemChecked(0, false);
                    }
                }
            }
        }

        string GetDbNameListString()
        {
            string strResult = "";
            for (int i = 0; i < this.checkedListBox_prepare_dbNames.CheckedItems.Count; i++)
            {
                if (i > 0)
                    strResult += ",";
                strResult += (string)this.checkedListBox_prepare_dbNames.CheckedItems[i];
            }

            return strResult;
        }

        void FillDbNameList()
        {
            this.checkedListBox_prepare_dbNames.Items.Clear();

            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty prop = this.MainForm.BiblioDbProperties[i];

                    if (this.comboBox_prepare_type.Text == "ͼ��")
                    {
                        if (String.IsNullOrEmpty(prop.IssueDbName) == false)
                            continue;

                    }
                    else
                    {
                        if (String.IsNullOrEmpty(prop.IssueDbName) == true)
                            continue;

                    }

                    this.checkedListBox_prepare_dbNames.Items.Add(prop.DbName);
                }
            }

            // �����һ��
            if (this.checkedListBox_prepare_dbNames.Items.Count > 1)
            {
                if (this.comboBox_prepare_type.Text == "ͼ��")
                {
                    this.checkedListBox_prepare_dbNames.Items.Insert(0, "<ȫ��ͼ��>");
                }
                else
                {
                    this.checkedListBox_prepare_dbNames.Items.Insert(0, "<ȫ���ڿ�>");
                }

                // ȱʡ��ѡ��һ��
                this.checkedListBox_prepare_dbNames.SetItemChecked(0, true);
            }
            else
            {
                // ȱʡ��ѡȫ������
                for (int i = 0; i < this.checkedListBox_prepare_dbNames.Items.Count; i++)
                {
                    this.checkedListBox_prepare_dbNames.SetItemChecked(i, true);
                }
            }
        }

        private void comboBox_accept_matchStyle_TextChanged(object sender, EventArgs e)
        {
            if (this.comboBox_accept_matchStyle.Text == "��ֵ")
            {
                this.textBox_accept_queryWord.Text = "";
                this.textBox_accept_queryWord.Enabled = false;
            }
            else
            {
                this.textBox_accept_queryWord.Enabled = true;
            }
        }

        // �Ƿ񵥻�������б��м���װ����ϸ����
        // ���==false����ʾҪ˫������װ��
        /// <summary>
        /// �Ƿ񵥻�������б��м���װ����ϸ�������Ϊ false����ʾҪ˫������װ��
        /// </summary>
        public bool SingleClickLoadDetail
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "accept_form",
                    "single_click_load_detail",
                    false);
            }
        }

        private void listView_accept_records_DoubleClick(object sender, EventArgs e)
        {
            if (this.SingleClickLoadDetail == true)
                return;

            if (this.listView_accept_records.SelectedItems.Count == 0
                || this.listView_accept_records.SelectedItems.Count > 1)    // 2009/2/3 ��ѡʱҲҪ��ֹ������ϸ��
            {
                API.PostMessage(this.Handle, WM_LOAD_DETAIL, 0, -1);
                return;
            }
            API.PostMessage(this.Handle, WM_LOAD_DETAIL, 0, this.listView_accept_records.SelectedIndices[0]);
        }

        // ��¼��Ϣ
        // ������title
        /*public*/ class RecordInfo
        {
            public string BiblioTitle = "";

            // �������title�Ƿ�͹ؼ������titleƥ���ˣ����ƥ���ˣ���ʾΪʵ�ڵ�״̬��������ʾΪ�����״̬
            public bool TitleMatch = true;

            // �Ƿ��ж�����Ϣ?
            public bool HasOrderInfo = true;
        }

        /// <summary>
        /// �Ƿ��Ѿ�ͣ��
        /// </summary>
        public bool Docked = false;

        /// <summary>
        /// ����ͣ��
        /// </summary>
        /// <param name="bShowFixedPanel">�Ƿ�ͬʱ�ٳ���ʾ�̶����</param>
        public void DoDock(bool bShowFixedPanel)
        {
            if (this.MainForm.CurrentAcceptControl != this.panel_main)
                this.MainForm.CurrentAcceptControl = this.panel_main;
            if (bShowFixedPanel == true
                && this.MainForm.PanelFixedVisible == false)
                this.MainForm.PanelFixedVisible = true;

            this.MainForm.ActivateAcceptPage();

            this.Docked = true;
            this.Visible = false;
        }

        /// <summary>
        /// ��ͣ��״̬�ָ��ɸ���״̬
        /// </summary>
        public void DoFloating()
        {
            if (this.Docked == true)
            {
                if (this.MainForm.CurrentAcceptControl == this.panel_main)
                    this.MainForm.CurrentAcceptControl = null;

                this.Docked = false;

                if (this.Controls.IndexOf(this.panel_main) == -1)
                    this.Controls.Add(this.panel_main);

                this.Visible = true;
            }
        }

        /// <summary>
        /// TabControl
        /// </summary>
        public Control MainControl
        {
            get
            {
                return this.panel_main;
            }
        }

        /// <summary>
        /// ����Ի����
        /// </summary>
        /// <param name="keyData">System.Windows.Forms.Keys ֵ֮һ������ʾҪ����ļ���</param>
        /// <returns>����ؼ�����ʹ�û�������Ϊ true������Ϊ false���������һ������</returns>
        protected override bool ProcessDialogKey(
Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }

            if (keyData == Keys.Enter)
            {
                DoEnterKey();
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        /// <summary>
        /// ����س���
        /// </summary>
        public void DoEnterKey()
        {
            if (this.textBox_accept_queryWord.Focused == true)
                button_accept_searchISBN_Click(this, new EventArgs());
            else if (this.listView_accept_records.Focused == true)
                listView_accept_records_DoubleClick(this, new EventArgs());
        }
    }



    // �ɹ����ݿ���Ϣ����
    /*public*/ class OrderDbInfos : List<OrderDbInfo>
    {
        public void Build(MainForm mainform)
        {
            this.Clear();
            if (mainform.BiblioDbProperties != null)
            {
                for (int i = 0; i < mainform.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty property = mainform.BiblioDbProperties[i];

                    OrderDbInfo info = new OrderDbInfo();
                    info.BiblioDbName = property.DbName;
                    info.OrderDbName = property.OrderDbName;
                    info.EntityDbName = property.ItemDbName;
                    info.IssueDbName = property.IssueDbName;

                    info.Syntax = property.Syntax;
                    if (String.IsNullOrEmpty(info.Syntax) == true)
                        info.Syntax = "unimarc";
                    // info.InCirculation = property.InCirculation;

                    info.Role = property.Role;  // 2009/10/23

                    this.Add(info);
                }
            }
        }

        public OrderDbInfo LocateByBiblioDbName(string strBiblioDbName)
        {
            for (int i = 0; i < this.Count; i++)
            {
                OrderDbInfo info = this[i];

                if (info.BiblioDbName == strBiblioDbName)
                    return info;
            }

            return null;
        }

        // ��ÿ�����ΪĿ������Ŀ����
        public List<string> GetTargetDbNames()
        {
            List<string> results = new List<string>();
            for (int i = 0; i < this.Count; i++)
            {
                OrderDbInfo info = this[i];

                if (info.IsTarget == true || info.IsSourceAndTarget == true)
                    results.Add(info.BiblioDbName);
            }

            return results;
        }

        // ���ݼ�¼·�������ListViewItem�����imageindex�±�
        // return:
        //      -2  ����������Ŀ��
        //      -1  ���ǲɹ�Դ��Ŀ���������Ŀ��
        //      0   Դ
        //      1   Ŀ��
        //      2   ͬʱΪԴ��Ŀ��
        //      3   ��Դ
        public int GetItemType(string strRecPath,
            string strPubType)
        {
            Debug.Assert(strPubType == "����������" || strPubType == "ͼ��", "");

            string strDbName = Global.GetDbName(strRecPath);
            for (int i = 0; i < this.Count; i++)
            {
                OrderDbInfo info = this[i];


                if (info.BiblioDbName == strDbName)
                {
                    // ע�������Դ��Ŀ��⣬ǧ��Ҫ����Ϊ����Դ����ɫ����Ϊ��Դ��ɫ������
                    if (StringUtil.IsInList("biblioSource", info.Role) == true)
                        return 3;

                    if (info.IsSourceAndTarget == true)
                    {
                        if (strPubType == "ͼ��")
                        {
                            if (String.IsNullOrEmpty(info.IssueDbName) == true)
                                return 2;

                            return -1;   // �����ڿ⣬���ܵ�����ͼ�顱���͵�Ŀ�����Դ
                        }
                        else
                        {
                            Debug.Assert(strPubType == "����������", "");

                            if (String.IsNullOrEmpty(info.IssueDbName) == false)
                                return 2;

                            // return 0;   // ��û���ڿ⣬���ܵ�����������������͵�Ŀ�꣬���ǿ��Ե���Դ
                            return -1;  // ��û���ڿ⣬���ܵ�����������������͵�Ŀ���Դ 2009/2/3 changed
                        }
                        // return 2;
                    }
                    if (info.IsSource == true)
                    {
                        if (strPubType == "ͼ��")
                        {
                            if (String.IsNullOrEmpty(info.IssueDbName) == true)
                                return 0;

                            return -1;   // �����ڿ⣬���ܵ�����ͼ�顱���͵�Դ
                        }
                        else
                        {
                            Debug.Assert(strPubType == "����������", "");

                            if (String.IsNullOrEmpty(info.IssueDbName) == false)
                                return 0;

                            // return 0;   // ��û���ڿ⣬���ܵ�����������������͵�Ŀ�꣬���ǿ��Ե���Դ
                            return -1;   // ��û���ڿ⣬���ܵ�����������������͵�Ŀ���Դ 2009/2/3 changed
                        }

                        // return 0;
                    }
                    if (info.IsTarget == true)
                    {
                        if (strPubType == "ͼ��")
                        {
                            if (String.IsNullOrEmpty(info.IssueDbName) == true)
                                return 1;

                            return -1;   // �����ڿ⣬���ܵ�����ͼ�顱���͵�Ŀ��
                        }
                        else
                        {
                            Debug.Assert(strPubType == "����������", "");

                            if (String.IsNullOrEmpty(info.IssueDbName) == false)
                                return 1;

                            return -1;   // ��û���ڿ⣬���ܵ�����������������͵�Ŀ��
                        }

                        // return 1;
                    }

                    return -1;  // -1 ��ʾ�������ǲɹ���Դ����Ŀ��⣬Ҳ����˵����ƥ�������⣬��û�а��������⣬Ҳû�а���ʵ���
                }
            }

            return -2;  // ����û���ҵ������Ŀ����
        }
    }

    // �ɹ����ݿ���Ϣ
    /*public*/ class OrderDbInfo
    {
        public string BiblioDbName = "";
        public string OrderDbName = "";
        public string EntityDbName = "";
        public string IssueDbName = "";

        public string Syntax = "";
        public bool InCirculation = false;

        public string Role = "";    // ��ɫ 2009/10/23

        public bool IsOrderWork
        {
            get
            {
                if (StringUtil.IsInList("orderWork", this.Role) == true)
                    return true;
                return false;
            }
        }

        // �Ƿ�ΪԴ
        public bool IsSource
        {
            get
            {
                if (String.IsNullOrEmpty(this.OrderDbName) == false)
                    return true;
                return false;
            }
        }

        // �Ƿ�ΪĿ��
        public bool IsTarget
        {
            get
            {
                if (String.IsNullOrEmpty(this.EntityDbName) == false)
                    return true;
                return false;
            }
        }

        // �Ƿ����Դ��Ҳ��Ŀ�ꣿ
        public bool IsSourceAndTarget
        {
            get
            {
                if (this.IsSource == true && this.IsTarget == true)
                    return true;
                return false;
            }
        }
    }
}