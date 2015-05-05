using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ���ش�
    /// </summary>
    public partial class DupForm : MyForm
    {
        // ����������к�����
        SortColumns SortColumns = new SortColumns();

        /*
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;
         * */

        string m_strXmlRecord = "";

        /// <summary>
        /// ���������ź�
        /// </summary>
        public AutoResetEvent EventFinish = new AutoResetEvent(false);

        /// <summary>
        /// �Ƿ�Ҫ(�ڴ��ڴ򿪺�)�Զ���������
        /// </summary>
        public bool AutoBeginSearch = false;

        const int WM_INITIAL = API.WM_USER + 201;

        const int ITEMTYPE_NORMAL = 0;  // ��ͨ����
        const int ITEMTYPE_OVERTHRESHOLD = 1; // Ȩֵ������ֵ������

        #region �ⲿ�ӿ�

        /// <summary>
        /// ���ط�����
        /// </summary>
        public string ProjectName
        {
            get
            {
                return this.comboBox_projectName.Text;
            }
            set
            {
                this.comboBox_projectName.Text = value;
            }
        }

        /// <summary>
        /// ������صļ�¼·����id����Ϊ?����Ҫ����ģ���keys
        /// </summary>
        public string RecordPath
        {
            get
            {
                return this.textBox_recordPath.Text;
            }
            set
            {
                this.textBox_recordPath.Text = value;
                this.Text = "����: " + value;
            }
        }

        /// <summary>
        /// ������ص�XML��¼
        /// </summary>
        public string XmlRecord
        {
            get
            {
                return m_strXmlRecord;
            }
            set
            {
                m_strXmlRecord = value;
            }
        }

        /// <summary>
        /// ��ò��ؽ���������е�Ȩֵ������ֵ�ļ�¼·���ļ���
        /// </summary>
        public string[] DupPaths
        {
            get
            {
                int i;
                List<string> aPath = new List<string>();
                for (i = 0; i < this.listView_browse.Items.Count; i++)
                {
                    ListViewItem item = this.listView_browse.Items[i];

                    if (item.ImageIndex == ITEMTYPE_OVERTHRESHOLD)
                    {
                        aPath.Add(item.Text);
                    }
                    else
                        break;  // �ٶ�������ֵ�������ǰ������������Ż��ж�
                }

                if (aPath.Count == 0)
                    return new string[0];

                string[] result = new string[aPath.Count];
                aPath.CopyTo(result);

                return result;
            }
        }

        #endregion

        /// <summary>
        /// ���캯��
        /// </summary>
        public DupForm()
        {
            InitializeComponent();

            ListViewProperty prop = new ListViewProperty();
            this.listView_browse.Tag = prop;
            // ��һ�����⣬��¼·��
            prop.SetSortStyle(0, ColumnSortStyle.RecPath);
            // �ڶ������⣬Ȩֵ�ͣ��Ҷ���
            prop.SetSortStyle(1, ColumnSortStyle.RightAlign);
            prop.GetColumnTitles -= new GetColumnTitlesEventHandler(prop_GetColumnTitles);
            prop.GetColumnTitles += new GetColumnTitlesEventHandler(prop_GetColumnTitles);

        }

        void prop_GetColumnTitles(object sender, GetColumnTitlesEventArgs e)
        {
            e.ColumnTitles = new ColumnPropertyCollection();
            ColumnPropertyCollection temp = this.MainForm.GetBrowseColumnProperties(e.DbName);
            if (temp != null)
                e.ColumnTitles.AddRange(temp);  // Ҫ���ƣ���Ҫֱ��ʹ�ã���Ϊ������ܻ��޸ġ���Ӱ�쵽ԭ��

            e.ColumnTitles.Insert(0, "Ȩֵ��");
        }

        void ClearListViewPropertyCache()
        {
            ListViewProperty prop = (ListViewProperty)this.listView_browse.Tag;
            prop.ClearCache();
        }

        private void DupForm_Load(object sender, EventArgs e)
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

            this.checkBox_includeLowCols.Checked = this.MainForm.AppInfo.GetBoolean(
                "dup_form",
                "include_low_cols",
                true);
            this.checkBox_returnAllRecords.Checked = this.MainForm.AppInfo.GetBoolean(
    "dup_form",
    "return_all_records",
    true);

            if (String.IsNullOrEmpty(this.comboBox_projectName.Text) == true)
            {
                this.comboBox_projectName.Text = this.MainForm.AppInfo.GetString(
                        "dup_form",
                        "projectname",
                        "");
            }

            string strWidths = this.MainForm.AppInfo.GetString(
    "dup_form",
    "browse_list_column_width",
    "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_browse,
                    strWidths,
                    true);
            }

            // �Զ���������
            if (this.AutoBeginSearch == true)
            {
                API.PostMessage(this.Handle, WM_INITIAL, 0, 0);
            }
        }

        /// <summary>
        /// ��ʼ����
        /// </summary>
        public void BeginSearch()
        {
            API.PostMessage(this.Handle, WM_INITIAL, 0, 0);
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
                        this.button_search_Click(null, null);
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }

        private void DupForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void DupForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            this.MainForm.AppInfo.SetBoolean(
    "dup_form",
    "include_low_cols",
    this.checkBox_includeLowCols.Checked);
            this.MainForm.AppInfo.SetBoolean(
    "dup_form",
    "return_all_records",
    this.checkBox_returnAllRecords.Checked);

            this.MainForm.AppInfo.SetString(
                "dup_form",
                "projectname",
                this.comboBox_projectName.Text);

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_browse);
            this.MainForm.AppInfo.SetString(
                "dup_form",
                "browse_list_column_width",
                strWidths);

            EventFinish.Set();
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

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int DoSearch(out string strError)
        {
            strError = "";
            string strUsedProjectName = "";

            this.EventFinish.Reset();
            try
            {

                int nRet = DoSearch(this.comboBox_projectName.Text,
                    this.textBox_recordPath.Text,
                    this.XmlRecord,
                    out strUsedProjectName,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (String.IsNullOrEmpty(strUsedProjectName) == false)
                    this.ProjectName = strUsedProjectName;
            }
            finally
            {
                this.EventFinish.Set();
            }

            return 0;
        }

        private void button_search_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = this.DoSearch(out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
            }
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.button_search.Enabled = bEnable;
            // this.button_stop.Enabled = bEnable;

            this.comboBox_projectName.Enabled = bEnable;
            this.textBox_recordPath.Enabled = bEnable;
        }

 
        // ����
        // return:
        //      -1  error
        //      0   succeed
        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="strProjectName">���ط�����</param>
        /// <param name="strRecPath">�����¼·��</param>
        /// <param name="strXml">�����¼�� XML</param>
        /// <param name="strUsedProjectName">����ʵ��ʹ�õķ�����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; >=0: ���еļ�¼��</returns>
        public int DoSearch(string strProjectName,
            string strRecPath,
            string strXml,
            out string strUsedProjectName,
            out string strError)
        {
            strError = "";
            strUsedProjectName = "";

            if (strProjectName == "<Ĭ��>"
                || strProjectName == "<default>")
                strProjectName = "";

            EventFinish.Reset();

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ��в��� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                this.ClearDupState();

                this.listView_browse.Items.Clear();
                // 2008/11/22 new add
                this.SortColumns.Clear();
                SortColumns.ClearColumnSortDisplay(this.listView_browse.Columns);

                string strBrowseStyle = "cols";
                if (this.checkBox_includeLowCols.Checked == false)
                    strBrowseStyle += ",excludecolsoflowthreshold";

                long lRet = Channel.SearchDup(
                    stop,
                    strRecPath,
                    strXml,
                    strProjectName,
                    "includeoriginrecord", // includeoriginrecord
                    out strUsedProjectName,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                long lHitCount = lRet;

                if (lHitCount == 0)
                    goto END1;   // ���ط���û������

                if (stop != null)
                    stop.SetProgressRange(0, lHitCount);

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    DupSearchResult[] searchresults = null;

                    lRet = Channel.GetDupSearchResult(
                        stop,
                        lStart,
                        lPerCount,
                        strBrowseStyle, // "cols,excludecolsoflowthreshold",
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (lRet == 0)
                        break;

                    Debug.Assert(searchresults != null, "");

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        DupSearchResult result = searchresults[i];

                        ListViewUtil.EnsureColumns(this.listView_browse,
                            2 + (result.Cols == null ? 0 : result.Cols.Length),
                            200);

                        if (this.checkBox_returnAllRecords.Checked == false)
                        {
                            // ������һ��Ȩֵ�ϵ͵ģ����ж�ȫ����ȡ�������
                            if (result.Weight < result.Threshold)
                                goto END1;
                        }

                        ListViewItem item = new ListViewItem();
                        item.Text = result.Path;
                        item.SubItems.Add(result.Weight.ToString());
                        if (result.Cols != null)
                        {
                            for (int j = 0; j < result.Cols.Length; j++)
                            {
                                item.SubItems.Add(result.Cols[j]);
                            }
                        }
                        this.listView_browse.Items.Add(item);



                        if (item.Text == this.RecordPath)
                        {
                            // ������Ƿ����¼�Լ�  2008/2/29 new add
                            item.ImageIndex = ITEMTYPE_OVERTHRESHOLD;
                            item.BackColor = Color.LightGoldenrodYellow;
                            item.ForeColor = SystemColors.GrayText; // ��ʾ���Ƿ����¼�Լ�
                        }
                        else if (result.Weight >= result.Threshold)
                        {
                            item.ImageIndex = ITEMTYPE_OVERTHRESHOLD;
                            item.BackColor = Color.LightGoldenrodYellow;
                        }
                        else
                        {
                            item.ImageIndex = ITEMTYPE_NORMAL;
                        }

                        if (stop != null)
                            stop.SetProgressValue(lStart + i + 1);
                    }

                    lStart += searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;

                }

            END1:
                this.SetDupState();

                return (int)lHitCount;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                EventFinish.Set();

                EnableControls(true);
            }


        ERROR1:
            return -1;
        }

        private void comboBox_projectName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_projectName.Items.Count > 0)
                return;

            string strError = "";
            int nRet = 0;

            string[] projectnames = null;
            // �г����õĲ��ط�����
            nRet = ListProjectNames(this.RecordPath,
                out projectnames,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            for (int i = 0; i < projectnames.Length; i++)
            {
                this.comboBox_projectName.Items.Add(projectnames[i]);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        // 
        /// <summary>
        /// �г����õĲ��ط�����
        /// </summary>
        /// <param name="strRecPath">�����¼·��</param>
        /// <param name="projectnames">���ؿ��õĲ��ط������ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; >=0: �ɹ�</returns>
        public int ListProjectNames(string strRecPath,
            out string [] projectnames,
            out string strError)
        {
            strError = "";
            projectnames = null;

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ȡ���õĲ��ط����� ...");
            stop.BeginLoop();

            try
            {
                DupProjectInfo[] dpis = null;

                string strBiblioDbName = Global.GetDbName(strRecPath);

                long lRet = Channel.ListDupProjectInfos(
                    stop,
                    strBiblioDbName,
                    out dpis,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                projectnames = new string[dpis.Length];
                for (int i = 0; i < projectnames.Length; i++)
                {
                    projectnames[i] = dpis[i].Name;
                }

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

        private void textBox_recordPath_TextChanged(object sender, EventArgs e)
        {
            // ��¼·����Ӱ�쵽�������б�
            // �޸ļ�¼·����ʱ����ʹ�����������б���գ��������õ������б��ʱ����Զ�ȥ��ȡ������
            this.comboBox_projectName.Items.Clear();
        }

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

        private void button_viewXmlRecord_Click(object sender, EventArgs e)
        {
            XmlViewerForm dlg = new XmlViewerForm();

            dlg.Text = "��ǰXML����";
            dlg.MainForm = this.MainForm;
            dlg.XmlString = this.XmlRecord;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);   // ?? this
            return;
            /*
        ERROR1:
            MessageBox.Show(this, strError);
             * */
        }

        void ClearDupState()
        {
            this.label_dupMessage.Text = "��δ����";
        }

        /// <summary>
        /// ����ظ���
        /// </summary>
        /// <returns>�ظ���</returns>
        public int GetDupCount()
        {
            int nCount = 0;
            for (int i = 0; i < this.listView_browse.Items.Count; i++)
            {
                ListViewItem item = this.listView_browse.Items[i];

                if (item.Text == this.RecordPath)
                    continue;   // �����������¼�Լ� 2008/2/29 new add

                if (item.ImageIndex == ITEMTYPE_OVERTHRESHOLD)
                    nCount++;
                else
                    break;  // �ٶ�����Ȩֵ�������ǰ����һ������һ�����ǵ����ѭ���ͽ���
            }

            return nCount;
        }

        // ���ò���״̬
        void SetDupState()
        {
            /*
            int nCount = 0;
            for (int i = 0; i < this.listView_browse.Items.Count; i++)
            {
                ListViewItem item = this.listView_browse.Items[i];

                if (item.Text == this.RecordPath)
                    continue;   // �����������¼�Լ� 2008/2/29 new add

                if (item.ImageIndex == ITEMTYPE_OVERTHRESHOLD)
                    nCount++;
                else
                    break;  // �ٶ�����Ȩֵ�������ǰ����һ������һ�����ǵ����ѭ���ͽ���
            }
             * */

            int nCount = GetDupCount();

            if (nCount > 0)
                this.label_dupMessage.Text = "�� " + Convert.ToString(nCount) + " ���ظ���¼��";
            else
                this.label_dupMessage.Text = "û���ظ���¼��";

        }

        // ˫��
        private void listView_browse_DoubleClick(object sender, EventArgs e)
        {
            if (this.listView_browse.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ����ϸ��������");
                return;
            }
            string strPath = this.listView_browse.SelectedItems[0].SubItems[0].Text;

            EntityForm form = new EntityForm();

            form.MdiParent = this.MainForm;

            form.MainForm = this.MainForm;
            form.Show();
            form.LoadRecordOld(strPath, "", true);
        }

        private void listView_browse_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;

            ColumnSortStyle sortStyle = ColumnSortStyle.LeftAlign;

            // ��һ��Ϊ��¼·��������������
            if (nClickColumn == 0)
                sortStyle = ColumnSortStyle.RecPath;
            else if (nClickColumn == 1)
                sortStyle = ColumnSortStyle.RightAlign;

            this.SortColumns.SetFirstColumn(nClickColumn,
                sortStyle,
                this.listView_browse.Columns,
                true);

            // ����
            this.listView_browse.ListViewItemSorter = new SortColumnsComparer(this.SortColumns);

            this.listView_browse.ListViewItemSorter = null;

        }

        private void DupForm_Activated(object sender, EventArgs e)
        {
#if NO
            // 2009/8/13 new add
            this.MainForm.stopManager.Active(this.stop);
#endif

        }

        private void listView_browse_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView_browse.SelectedItems.Count == 1)
            {
                ListViewItem item = this.listView_browse.SelectedItems[0];
                int nLineNo = this.listView_browse.SelectedIndices[0] + 1;
                if (item.ImageIndex == ITEMTYPE_OVERTHRESHOLD)
                {
                    if (item.Text == this.RecordPath)
                    {
                        this.label_message.Text = "��� " + nLineNo.ToString() + ": ������صļ�¼(�Լ�)";
                    }
                    else
                    {
                        this.label_message.Text = "��� " + nLineNo.ToString() + ": �ظ��ļ�¼";
                    }
                }
                else
                {
                    this.label_message.Text = "��� " + nLineNo.ToString();
                }
            }
            else
            {
                this.label_message.Text = "";
            }

            // װ��(δװ���)�����
            if (this.listView_browse.SelectedItems.Count > 0)
            {
                List<string> pathlist = new List<string>();
                List<ListViewItem> itemlist = new List<ListViewItem>();
                foreach (ListViewItem item in this.listView_browse.SelectedItems)
                {
                    string strFirstCol = ListViewUtil.GetItemText(item, 2);
                    if (string.IsNullOrEmpty(strFirstCol) == false)
                        continue;
                    pathlist.Add(item.Text);
                    itemlist.Add(item);
                }

                if (pathlist.Count > 0)
                {
                    string strError = "";
                    int nRet = GetBrowseCols(pathlist,
                        itemlist,
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(this, strError);
                }
            }

            ListViewUtil.OnSeletedIndexChanged(this.listView_browse,
    0,
    null);
        }

        // �������˵�
        private void listView_browse_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("װ���¿����ֲᴰ(&N)");
            menuItem.Click += new System.EventHandler(this.menu_loadToNewDetailWindow_Click);
            if (this.listView_browse.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            menuItem.DefaultItem = true;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("װ���Ѿ��򿪵��ֲᴰ(&E)");
            menuItem.Click += new System.EventHandler(this.menu_loadToExistDetailWindow_Click);
            if (this.listView_browse.SelectedItems.Count == 0
                || this.MainForm.GetTopChildWindow<EntityForm>() == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("���ȫ�������(&F)");
            menuItem.Click += new System.EventHandler(this.menu_fillBrowseCols_Click);
            /*
            if (this.listView_browse.SelectedItems.Count == 0)
                menuItem.Enabled = false;
             * */
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView_browse, new Point(e.X, e.Y));
        }

        void menu_loadToNewDetailWindow_Click(object sender, EventArgs e)
        {
            if (this.listView_browse.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ���ֲᴰ������");
                return;
            }
            string strPath = this.listView_browse.SelectedItems[0].SubItems[0].Text;

            EntityForm form = new EntityForm();

            form.MdiParent = this.MainForm;
            form.MainForm = this.MainForm;
            form.Show();
            form.LoadRecordOld(strPath, "", true);
        }

        void menu_loadToExistDetailWindow_Click(object sender, EventArgs e)
        {
            if (this.listView_browse.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ���ֲᴰ������");
                return;
            }
            string strPath = this.listView_browse.SelectedItems[0].SubItems[0].Text;

            EntityForm form = this.MainForm.GetTopChildWindow<EntityForm>();
            if (form == null)
            {
                MessageBox.Show(this, "Ŀǰ��û���Ѿ��򿪵��ֲᴰ");
                return;
            }
            Global.Activate(form);
            form.LoadRecordOld(strPath, "", true);
        }

        void menu_fillBrowseCols_Click(object sender, EventArgs e)
        {
            List<string> pathlist = new List<string>();
            List<ListViewItem> itemlist = new List<ListViewItem>();
            for (int i = 0; i < this.listView_browse.Items.Count; i++)
            {
                ListViewItem item = this.listView_browse.Items[i];
                string strFirstCol = ListViewUtil.GetItemText(item, 2);
                if (string.IsNullOrEmpty(strFirstCol) == false)
                    continue;
                pathlist.Add(item.Text);
                itemlist.Add(item);
            }

            if (pathlist.Count > 0)
            {
                string strError = "";
                int nRet = GetBrowseCols(pathlist,
                    itemlist,
                    out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);
            }
        }

        int GetBrowseCols(List<string> pathlist,
            List<ListViewItem> itemlist,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("������������ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {


                int nStart = 0;
                int nCount = 0;
                for (; ; )
                {
                    nCount = pathlist.Count - nStart;
                    if (nCount > 100)
                        nCount = 100;
                    if (nCount <= 0)
                        break;

                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�";
                            return -1;
                        }
                    }

                    stop.SetMessage("����װ�������Ϣ " + (nStart + 1).ToString() + " - " + (nStart + nCount).ToString());




                    string[] paths = new string[nCount];
                    pathlist.CopyTo(nStart, paths, 0, nCount);

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

                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        Record record = searchresults[i];

                        ListViewUtil.EnsureColumns(this.listView_browse,
                            2 + (record.Cols == null ? 0 : record.Cols.Length),
                            200);

                        ListViewItem item = itemlist[nStart + i];
                        item.Text = record.Path;
                        if (record.Cols != null)
                        {
                            for (int j = 0; j < record.Cols.Length; j++)
                            {
                                item.SubItems.Add(record.Cols[j]);
                            }
                        }
                    }


                    nStart += searchresults.Length;
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            return 0;
        }
    }
}