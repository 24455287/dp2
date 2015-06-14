using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.CommonControl;
using DigitalPlatform.Marc;

using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.IO;
using DigitalPlatform.Script;
using System.Web;
using DigitalPlatform.Text;

namespace dp2Circulation
{
    /// <summary>
    /// ��Ŀ��ѯ��
    /// </summary>
    public partial class BiblioSearchForm : MyForm
    {
        Commander commander = null;

        CommentViewerForm m_commentViewer = null;

        Hashtable m_biblioTable = new Hashtable(); // ��Ŀ��¼·�� --> ��Ŀ��Ϣ

        const int WM_SELECT_INDEX_CHANGED = API.WM_USER + 200;

        // ���ʹ�ù��ļ�¼·���ļ���
        string m_strUsedRecPathFilename = "";

        bool m_bFirstColumnIsKey = false; // ��ǰlistview����еĵ�һ���Ƿ�ӦΪkey

        long m_lLoaded = 0; // �����Ѿ�װ������������
        long m_lHitCount = 0;   // �������н������

        /*
        // ����������к�����
        SortColumns SortColumns = new SortColumns();
         * */

        /// <summary>
        /// ����������ļ�¼·���ļ�·��
        /// </summary>
        public string ExportRecPathFilename = "";   // ʹ�ù��ĵ���·���ļ�
        /// <summary>
        /// ����������Ĳ��¼·���ļ�·��
        /// </summary>
        public string ExportEntityRecPathFilename = ""; // ʹ�ù��ĵ���ʵ���¼·���ļ�


        // BiblioDbFromInfo[] DbFromInfos = null;
        /// <summary>
        /// ���캯��
        /// </summary>
        public BiblioSearchForm()
        {
            InitializeComponent();

            ListViewProperty prop = new ListViewProperty();
            this.listView_records.Tag = prop;
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

            // e.ColumnTitles = this.MainForm.GetBrowseColumnNames(e.DbName);

            e.ColumnTitles = new ColumnPropertyCollection();
            ColumnPropertyCollection temp = this.MainForm.GetBrowseColumnProperties(e.DbName);
            if (temp != null)
                e.ColumnTitles.AddRange(temp);  // Ҫ���ƣ���Ҫֱ��ʹ�ã���Ϊ������ܻ��޸ġ���Ӱ�쵽ԭ��

            if (this.m_bFirstColumnIsKey == true)
                e.ColumnTitles.Insert(0, "���еļ�����");
        }

        void ClearListViewPropertyCache()
        {
            ListViewProperty prop = (ListViewProperty)this.listView_records.Tag;
            prop.ClearCache();
        }

        private void BiblioSearchForm_Load(object sender, EventArgs e)
        {
            this.commander = new Commander(this);
            this.commander.IsBusy -= new IsBusyEventHandler(commander_IsBusy);
            this.commander.IsBusy += new IsBusyEventHandler(commander_IsBusy);

            this.MainForm.AppInfo.LoadMdiSize += new EventHandler(AppInfo_LoadMdiSize);
            this.MainForm.AppInfo.SaveMdiSize += new EventHandler(AppInfo_SaveMdiSize);

            this.MainForm.FillBiblioFromList(this.comboBox_from);

            this.m_strUsedMarcQueryFilename = this.MainForm.AppInfo.GetString(
                "bibliosearchform",
                "usedMarcQueryFilename",
                "");

            // �ָ��ϴ��˳�ʱ�����ļ���;��
            string strFrom = this.MainForm.AppInfo.GetString(
                "bibliosearchform",
                "search_from",
                "");
            if (String.IsNullOrEmpty(strFrom) == false)
                this.comboBox_from.Text = strFrom;

            this.checkedComboBox_biblioDbNames.Text = this.MainForm.AppInfo.GetString(
                "bibliosearchform",
                "biblio_db_name",
                "<ȫ��>");

            this.comboBox_matchStyle.Text = this.MainForm.AppInfo.GetString(
                "bibliosearchform",
                "match_style",
                "ǰ��һ��");

            bool bHideMatchStyle = this.MainForm.AppInfo.GetBoolean(
                "biblio_search_form",
                "hide_matchstyle",
                false);

            if (bHideMatchStyle == true)
            {
                this.label_matchStyle.Visible = false;
                this.comboBox_matchStyle.Visible = false;
                this.comboBox_matchStyle.Text = "ǰ��һ��"; // ���غ󣬲���ȱʡֵ
            }

            string strSaveString = this.MainForm.AppInfo.GetString(
"bibliosearchform",
"query_lines",
"^^^");
            this.dp2QueryControl1.Restore(strSaveString);

            comboBox_matchStyle_TextChanged(null, null);

            /*
            // FillFromList();
            // this.BeginInvoke(new Delegate_FillFromList(FillFromList));
            EnableControls(false);
            API.PostMessage(this.Handle, API.WM_USER + 100, 0, 0);
             */
            if (this.MainForm != null)
                this.MainForm.FixedSelectedPageChanged += new EventHandler(MainForm_FixedSelectedPageChanged);

#if NO
            if (this.MainForm.NormalDbProperties == null
                || this.MainForm.BiblioDbFromInfos == null
                || this.MainForm.BiblioDbProperties == null)
            {
                this.tableLayoutPanel_main.Enabled = false;
            }
#endif
        }

        void MainForm_FixedSelectedPageChanged(object sender, EventArgs e)
        {
            // �̶��������������ʾ������
            if (this.MainForm.ActiveMdiChild == this && this.MainForm.CanDisplayItemProperty() == true)
            {
                RefreshPropertyView(false);
            }
        }

        void commander_IsBusy(object sender, IsBusyEventArgs e)
        {
            e.IsBusy = this.m_nInViewing > 0;
        }

        void AppInfo_SaveMdiSize(object sender, EventArgs e)
        {
            if (sender != this)
                return;

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_records);
            this.MainForm.AppInfo.SetString(
                "bibliosearchform",
                "record_list_column_width",
                strWidths);

            this.MainForm.SaveSplitterPos(
this.splitContainer_main,
"bibliosearchform",
"splitContainer_main_ratio");
        }

        void AppInfo_LoadMdiSize(object sender, EventArgs e)
        {
            if (sender != this)
                return;

            string strWidths = this.MainForm.AppInfo.GetString(
    "bibliosearchform",
    "record_list_column_width",
    "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_records,
                    strWidths,
                    true);
            }


            this.MainForm.LoadSplitterPos(
this.splitContainer_main,
"bibliosearchform",
"splitContainer_main_ratio");
        }

        private void BiblioSearchForm_FormClosing(object sender, FormClosingEventArgs e)
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
                    "BiblioSearchForm",
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

        private void BiblioSearchForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.commander.Destroy();

            this.MainForm.AppInfo.SetString(
    "bibliosearchform",
    "usedMarcQueryFilename",
    this.m_strUsedMarcQueryFilename);

            // �������;��
            this.MainForm.AppInfo.SetString(
                "bibliosearchform",
                "search_from",
                this.comboBox_from.Text);

            this.MainForm.AppInfo.SetString(
                "bibliosearchform",
                "biblio_db_name",
                this.checkedComboBox_biblioDbNames.Text);

            this.MainForm.AppInfo.SetString(
                "bibliosearchform",
                "match_style",
                this.comboBox_matchStyle.Text);



            this.MainForm.AppInfo.SetString(
"bibliosearchform",
"query_lines",
this.dp2QueryControl1.GetSaveString());

            if (this.m_commentViewer != null)
                this.m_commentViewer.Close();

            this.MainForm.AppInfo.LoadMdiSize -= new EventHandler(AppInfo_LoadMdiSize);
            this.MainForm.AppInfo.SaveMdiSize -= new EventHandler(AppInfo_SaveMdiSize);

            this.MainForm.FixedSelectedPageChanged -= new EventHandler(MainForm_FixedSelectedPageChanged);
        }


        /*
        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case API.WM_USER + 100:
                    {
                        FillFromList();
                    }
                    break;

            }
            base.DefWndProc(ref m);
        }
         */



        // public delegate void Delegate_FillFromList();

        /// <summary>
        /// �������е�����¼�����Ʋ�����-1 ��ʾ������
        /// </summary>
        public int MaxSearchResultCount
        {
            get
            {
                return (int)this.MainForm.AppInfo.GetInt(
                    "biblio_search_form",
                    "max_result_count",
                    -1);

            }
        }

        // �Ƿ����ƶ��ķ�ʽװ������б�
        // 2008/1/20
        /// <summary>
        /// �Ƿ����ƶ��ķ�ʽװ������б�
        /// </summary>
        public bool PushFillingBrowse
        {
            get
            {
                return MainForm.AppInfo.GetBoolean(
                    "biblio_search_form",
                    "push_filling_browse",
                    false);
            }
        }

        // 
        /// <summary>
        /// ��ÿ����ڼ���ʽ��ƥ�䷽ʽ�ַ���
        /// </summary>
        /// <param name="strText">��Ͽ��е�ƥ�䷽ʽ�ַ���</param>
        /// <returns>�����ڼ���ʽ��ƥ�䷽ʽ�ַ���</returns>
        public static string GetCurrentMatchStyle(string strText)
        {
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
#if NO
        string GetCurrentMatchStyle()
        {
            string strText = this.comboBox_matchStyle.Text;

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
#endif

        // ���ⲿʹ��
        /// <summary>
        /// ����� ListView ����
        /// </summary>
        public ListView ListViewRecords
        {
            get
            {
                return this.listView_records;
            }
        }

        void ClearListViewItems()
        {
            this.listView_records.Items.Clear();

            ListViewUtil.ClearSortColumns(this.listView_records);

            // ���������Ҫȷ����������
            for (int i = 1; i < this.listView_records.Columns.Count; i++)
            {
                this.listView_records.Columns[i].Text = i.ToString();
            }

            this.m_biblioTable = new Hashtable();
            this.m_nChangedCount = 0;

            if (this.m_commentViewer != null)
                this.m_commentViewer.Clear();
        }

        /// <summary>
        /// ����Ի����
        /// </summary>
        /// <param name="keyData">System.Windows.Forms.Keys ֵ֮һ������ʾҪ����ļ���</param>
        /// <returns>����ؼ�����ʹ�û�������Ϊ true������Ϊ false���������һ������</returns>
        protected override bool ProcessDialogKey(
Keys keyData)
        {
            if (keyData == Keys.Enter
                && this.tabControl_query.SelectedTab == this.tabPage_logic)
            {
                this.DoLogicSearch(false);
                return true;
            }

            /*
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
             * */

            return base.ProcessDialogKey(keyData);
        }

        void DoLogicSearch(bool bOutputKeyID)
        {
            string strError = "";
            bool bQuickLoad = false;    // �Ƿ����װ��
            bool bClear = true; // �Ƿ��������������е�����

            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                bQuickLoad = true;

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                bClear = false;


            // �޸Ĵ��ڱ���
            this.Text = "��Ŀ��ѯ �߼�����";

            if (bOutputKeyID == true)
                this.m_bFirstColumnIsKey = true;
            else
                this.m_bFirstColumnIsKey = false;
            this.ClearListViewPropertyCache();

            if (bClear == true)
            {
                if (this.m_nChangedCount > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ���м�¼�б����� " + this.m_nChangedCount.ToString() + " ���޸���δ���档\r\n\r\n�Ƿ��������?\r\n\r\n(Yes �����Ȼ�����������No ��������)",
                        "BiblioSearchForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;
                }
                this.ClearListViewItems();

                ListViewUtil.ClearSortColumns(this.listView_records);
            }

            this.m_lHitCount = 0;
            this.m_lLoaded = 0;
            stop.HideProgress();

            this.label_message.Text = "";

            stop.Style = StopStyle.None;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);
            try
            {

                // string strBrowseStyle = "id,cols";
                string strOutputStyle = "";
                if (bOutputKeyID == true)
                {
                    strOutputStyle = "keyid";
                    // strBrowseStyle = "keyid,id,key,cols";
                }

                string strQueryXml = "";
                int nRet = dp2QueryControl1.BuildQueryXml(
    this.MaxSearchResultCount,
    "zh",
    out strQueryXml,
    out strError);
                if (nRet == -1)
                    goto ERROR1;

                long lRet = Channel.Search(stop,
                    strQueryXml,
                    "default",
                    strOutputStyle,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                long lHitCount = lRet;

                this.m_lHitCount = lHitCount;

                this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼";

                stop.SetProgressRange(0, lHitCount);
                stop.Style = StopStyle.EnableHalfStop;

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                bool bPushFillingBrowse = this.PushFillingBrowse;

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            // MessageBox.Show(this, "�û��ж�");
                            this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼����װ�� " + lStart.ToString() + " �����û��ж�...";
                            return;
                        }
                    }

                    stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    bool bTempQuickLoad = bQuickLoad;

                    if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                        bTempQuickLoad = true;


                    string strBrowseStyle = "id,cols";
                    if (bTempQuickLoad == true)
                    {
                        if (bOutputKeyID == true)
                            strBrowseStyle = "keyid,id,key";
                        else
                            strBrowseStyle = "id";
                    }
                    else
                    {
                        // 
                        if (bOutputKeyID == true)
                            strBrowseStyle = "keyid,id,key,cols";
                        else
                            strBrowseStyle = "id,cols";
                    }

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
                    {
                        this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼����װ�� " + lStart.ToString() + " ����" + strError;
                        goto ERROR1;
                    }

                    if (lRet == 0)
                    {
                        MessageBox.Show(this, "δ����");
                        return;
                    }

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        DigitalPlatform.CirculationClient.localhost.Record searchresult = searchresults[i];

                        string[] cols = null;
                        if (bOutputKeyID == true)
                        {
                            // ���keys
                            if (searchresult.Cols == null
                                && bTempQuickLoad == false)
                            {
                                strError = "Ҫʹ�û�ȡ�����㹦�ܣ��뽫 dp2Library Ӧ�÷������� dp2Kernel ���ݿ��ں����������°汾";
                                goto ERROR1;
                            }
                            cols = new string[(searchresult.Cols == null ? 0 : searchresult.Cols.Length) + 1];
                            cols[0] = LibraryChannel.BuildDisplayKeyString(searchresult.Keys);
                            if (cols.Length > 1)
                                Array.Copy(searchresult.Cols, 0, cols, 1, cols.Length - 1);
                        }
                        else
                        {
                            cols = searchresult.Cols;
                        }


                        if (bPushFillingBrowse == true)
                        {
                            if (bTempQuickLoad == true)
                                Global.InsertNewLine(
                                    (ListView)this.listView_records,
                                    searchresult.Path,
                                    cols);
                            else
                                Global.InsertNewLine(
                                    this.listView_records,
                                    searchresult.Path,
                                    cols);
                        }
                        else
                        {
                            if (bTempQuickLoad == true)
                                Global.AppendNewLine(
                                    (ListView)this.listView_records,
                                    searchresult.Path,
                                    cols);
                            else
                                Global.AppendNewLine(
                                    this.listView_records,
                                    searchresult.Path,
                                    cols);
                        }
                    }

                    lStart += searchresults.Length;
                    // lCount -= searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;

                    this.m_lLoaded = lStart;
                    stop.SetProgressValue(lStart);
                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
                this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼����ȫ��װ��";
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

        List<ItemQueryParam> m_queries = new List<ItemQueryParam>();
        int m_nQueryIndex = -1;

        void QueryToPanel(ItemQueryParam query)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                this.textBox_queryWord.Text = query.QueryWord;
                this.checkedComboBox_biblioDbNames.Text = query.DbNames;
                this.comboBox_from.Text = query.From;
                this.comboBox_matchStyle.Text = query.MatchStyle;

                if (this.m_nChangedCount > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ���м�¼�б����� " + this.listView_records.Items.Count.ToString() + " ���޸���δ���档\r\n\r\n�Ƿ��������?\r\n\r\n(Yes �����Ȼ�����������No ��������)",
                        "BiblioSearchForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;
                }
                this.ClearListViewItems();

                this.listView_records.BeginUpdate();
                for (int i = 0; i < query.Items.Count; i++)
                {
                    this.listView_records.Items.Add(query.Items[i]);
                }
                this.listView_records.EndUpdate();

                this.m_bFirstColumnIsKey = query.FirstColumnIsKey;
                this.ClearListViewPropertyCache();
            }
            finally
            {
                this.Cursor = oldCursor;
            }
        }

        ItemQueryParam PanelToQuery()
        {
            ItemQueryParam query = new ItemQueryParam();

            query.QueryWord = this.textBox_queryWord.Text;
            query.DbNames = this.checkedComboBox_biblioDbNames.Text;
            query.From = this.comboBox_from.Text;
            query.MatchStyle = this.comboBox_matchStyle.Text;
            query.FirstColumnIsKey = this.m_bFirstColumnIsKey;
            return query;
        }

        void PushQuery(ItemQueryParam query)
        {
            if (query == null)
                throw new Exception("queryֵ����Ϊ��");

            // �ض�β�������
            if (this.m_nQueryIndex < this.m_queries.Count - 1)
                this.m_queries.RemoveRange(this.m_nQueryIndex + 1, this.m_queries.Count - (this.m_nQueryIndex + 1));

            if (this.m_queries.Count > 100)
            {
                int nDelta = this.m_queries.Count - 100;
                this.m_queries.RemoveRange(0, nDelta);
                if (this.m_nQueryIndex >= 0)
                {
                    this.m_nQueryIndex -= nDelta;
                    Debug.Assert(this.m_nQueryIndex >= 0, "");
                }
            }

            this.m_queries.Add(query);
            this.m_nQueryIndex++;
            SetQueryPrevNextState();
        }

        ItemQueryParam PrevQuery()
        {
            if (this.m_queries.Count == 0)
                return null;

            if (this.m_nQueryIndex <= 0)
                return null;

            this.m_nQueryIndex--;
            ItemQueryParam query = this.m_queries[this.m_nQueryIndex];

            SetQueryPrevNextState();

            this.m_bFirstColumnIsKey = query.FirstColumnIsKey;
            this.ClearListViewPropertyCache();
            return query;
        }

        ItemQueryParam NextQuery()
        {
            if (this.m_queries.Count == 0)
                return null;

            if (this.m_nQueryIndex >= this.m_queries.Count - 1)
                return null;

            this.m_nQueryIndex++;
            ItemQueryParam query = this.m_queries[this.m_nQueryIndex];

            SetQueryPrevNextState();

            this.m_bFirstColumnIsKey = query.FirstColumnIsKey;
            this.ClearListViewPropertyCache();
            return query;
        }

        void SetQueryPrevNextState()
        {
            if (this.m_nQueryIndex < 0)
            {
                toolStripButton_nextQuery.Enabled = false;
                toolStripButton_prevQuery.Enabled = false;
                return;
            }

            if (this.m_nQueryIndex >= this.m_queries.Count - 1)
            {
                toolStripButton_nextQuery.Enabled = false;
            }
            else
                toolStripButton_nextQuery.Enabled = true;

            if (this.m_nQueryIndex <= 0)
            {
                toolStripButton_prevQuery.Enabled = false;
            }
            else
                toolStripButton_prevQuery.Enabled = true;

        }


        public void DoSearch(bool bOutputKeyCount,
            bool bOutputKeyID,
            ItemQueryParam input_query = null)
        {
            string strError = "";

            if (bOutputKeyCount == true
    && bOutputKeyID == true)
            {
                strError = "bOutputKeyCount��bOutputKeyID����ͬʱΪtrue";
                goto ERROR1;
            }

            bool bQuickLoad = false;    // �Ƿ����װ��
            bool bClear = true; // �Ƿ��������������е�����

            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                bQuickLoad = true;

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                bClear = false;


            // �޸Ĵ��ڱ���
            this.Text = "��Ŀ��ѯ " + this.textBox_queryWord.Text;

            if (input_query != null)
            {
                QueryToPanel(input_query);
            }

            // �����¼���ʽ
            this.m_bFirstColumnIsKey = bOutputKeyID;
            this.ClearListViewPropertyCache();

            ItemQueryParam query = PanelToQuery();
            PushQuery(query);

            if (bClear == true)
            {
                if (this.m_nChangedCount > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ���м�¼�б����� " + this.listView_records.Items.Count.ToString() + " ���޸���δ���档\r\n\r\n�Ƿ��������?\r\n\r\n(Yes �����Ȼ�����������No ��������)",
                        "BiblioSearchForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;
                }
                this.ClearListViewItems();

                /*
                // 2008/11/22
                this.SortColumns.Clear();
                SortColumns.ClearColumnSortDisplay(this.listView_records.Columns);
                 * */
                ListViewUtil.ClearSortColumns(this.listView_records);
            }

            this.m_lHitCount = 0;
            this.m_lLoaded = 0;
            stop.HideProgress();

            this.label_message.Text = "";

            stop.Style = StopStyle.None;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� '" + this.textBox_queryWord.Text + "' ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);
            try
            {
                if (this.comboBox_from.Text == "")
                {
                    strError = "��δѡ������;��";
                    goto ERROR1;
                }

                string strFromStyle = "";

                try
                {
                    strFromStyle = this.MainForm.GetBiblioFromStyle(this.comboBox_from.Text);
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    goto ERROR1;
                }

                if (String.IsNullOrEmpty(strFromStyle) == true)
                {
                    strError = "GetFromStyle()û���ҵ� '" + this.comboBox_from.Text + "' ��Ӧ��style�ַ���";
                    goto ERROR1;
                }

                // ע��"null"ֻ����ǰ�˶��ݴ��ڣ����ں��ǲ��������ν��matchstyle��
                string strMatchStyle = GetCurrentMatchStyle(this.comboBox_matchStyle.Text);

                if (this.textBox_queryWord.Text == "")
                {
                    if (strMatchStyle == "null")
                    {
                        this.textBox_queryWord.Text = "";

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
                        goto ERROR1;
                    }
                }

                // string strBrowseStyle = "id,cols";
                string strOutputStyle = "";
                if (bOutputKeyCount == true)
                {
                    strOutputStyle = "keycount";
                } 
                else if (bOutputKeyID == true)
                {
                    strOutputStyle = "keyid";
                    // strBrowseStyle = "keyid,id,key,cols";
                }

                string strQueryXml = "";
                long lRet = Channel.SearchBiblio(stop,
                    this.checkedComboBox_biblioDbNames.Text,
                    this.textBox_queryWord.Text,
                    this.MaxSearchResultCount,  // 1000
                    strFromStyle,
                    strMatchStyle,  // "left",
                    this.Lang,
                    null,   // strResultSetName
                    "",    // strSearchStyle
                    strOutputStyle,
                    out strQueryXml,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                long lHitCount = lRet;

                this.m_lHitCount = lHitCount;

                this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼";

                stop.SetProgressRange(0, lHitCount);
                stop.Style = StopStyle.EnableHalfStop;

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                bool bPushFillingBrowse = this.PushFillingBrowse;

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            // MessageBox.Show(this, "�û��ж�");
                            this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼����װ�� " + lStart.ToString() + " �����û��ж�...";
                            return;
                        }
                    }

                    stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    bool bTempQuickLoad = bQuickLoad;

                    if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                        bTempQuickLoad = true;


                    string strBrowseStyle = "id,cols";
                    if (bOutputKeyCount == true)
                        strBrowseStyle = "keycount";
                    else
                    {
                        if (bTempQuickLoad == true)
                        {
                            if (bOutputKeyID == true)
                                strBrowseStyle = "keyid,id,key";
                            else
                                strBrowseStyle = "id";
                        }
                        else
                        {
                            // 
                            if (bOutputKeyID == true)
                                strBrowseStyle = "keyid,id,key,cols";
                            else
                                strBrowseStyle = "id,cols";
                        }
                    }


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
                    {
                        this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼����װ�� " + lStart.ToString() + " ����" + strError;
                        goto ERROR1;
                    }

                    if (lRet == 0)
                    {
                        MessageBox.Show(this, "δ����");
                        return;
                    }

                    this.listView_records.BeginUpdate();
                    try
                    {
                        // ����������
                        for (int i = 0; i < searchresults.Length; i++)
                        {
                            ListViewItem item = null;

                            DigitalPlatform.CirculationClient.localhost.Record searchresult = searchresults[i];

                            string[] cols = null;
                            if (bOutputKeyCount == true)
                            {
                                // ���keys
                                if (searchresult.Cols == null)
                                {
                                    strError = "Ҫʹ�û�ȡ�����㹦�ܣ��뽫 dp2Library Ӧ�÷������� dp2Kernel ���ݿ��ں����������°汾";
                                    goto ERROR1;
                                }
                                cols = new string[(searchresult.Cols == null ? 0 : searchresult.Cols.Length) + 1];
                                cols[0] = searchresult.Path;
                                if (cols.Length > 1)
                                    Array.Copy(searchresult.Cols, 0, cols, 1, cols.Length - 1);

                                if (bPushFillingBrowse == true)
                                    item = Global.InsertNewLine(
                                        this.listView_records,
                                        "",
                                        cols);
                                else
                                    item = Global.AppendNewLine(
                                        this.listView_records,
                                        "",
                                        cols);
                                item.Tag = query;
                                goto CONTINUE;
                            }
                            else if (bOutputKeyID == true)
                            {

                                // ���keys
                                if (searchresult.Cols == null
                                    && bTempQuickLoad == false)
                                {
                                    strError = "Ҫʹ�û�ȡ�����㹦�ܣ��뽫 dp2Library Ӧ�÷������� dp2Kernel ���ݿ��ں����������°汾";
                                    goto ERROR1;
                                }

                                cols = new string[(searchresult.Cols == null ? 0 : searchresult.Cols.Length) + 1];
                                cols[0] = LibraryChannel.BuildDisplayKeyString(searchresult.Keys);
                                if (cols.Length > 1)
                                    Array.Copy(searchresult.Cols, 0, cols, 1, cols.Length - 1);
                            }
                            else
                            {
                                cols = searchresult.Cols;
                            }


                            if (bPushFillingBrowse == true)
                            {
                                if (bTempQuickLoad == true)
                                    item = Global.InsertNewLine(
                                        (ListView)this.listView_records,
                                        searchresult.Path,
                                        cols);
                                else
                                    item = Global.InsertNewLine(
                                        this.listView_records,
                                        searchresult.Path,
                                        cols);
                            }
                            else
                            {
                                if (bTempQuickLoad == true)
                                    item = Global.AppendNewLine(
                                        (ListView)this.listView_records,
                                        searchresult.Path,
                                        cols);
                                else
                                    item = Global.AppendNewLine(
                                        this.listView_records,
                                        searchresult.Path,
                                        cols);
                            }

                        CONTINUE:
                            query.Items.Add(item);
                        }
                    }
                    finally
                    {
                        this.listView_records.EndUpdate();
                    }

                    lStart += searchresults.Length;
                    // lCount -= searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;

                    this.m_lLoaded = lStart;
                    stop.SetProgressValue(lStart);
                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
                this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼����ȫ��װ��";
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

        private void listView_records_DoubleClick(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ��ʵ�崰�ڵ�����");
                return;
            }

            string strPath = this.listView_records.SelectedItems[0].SubItems[0].Text;

            if (String.IsNullOrEmpty(strPath) == false)
            {
                EntityForm form = null;

                if (this.LoadToExistDetailWindow == true)
                {
                    form = MainForm.GetTopChildWindow<EntityForm>();
                    if (form != null)
                        Global.Activate(form);

                }

                if (form == null)
                {
                    form = new EntityForm();

                    form.MdiParent = this.MainForm;

                    form.MainForm = this.MainForm;
                    form.Show();
                }

                Debug.Assert(form != null, "");

                form.LoadRecordOld(strPath, "", true);
            }
            else
            {
                ItemQueryParam query = (ItemQueryParam)this.listView_records.SelectedItems[0].Tag;
                Debug.Assert(query != null, "");

                this.textBox_queryWord.Text = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);
                if (query != null)
                {
                    this.checkedComboBox_biblioDbNames.Text = query.DbNames;
                    this.comboBox_from.Text = query.From;
                }

                if (this.textBox_queryWord.Text == "")
                    this.comboBox_matchStyle.Text = "��ֵ";
                else
                    this.comboBox_matchStyle.Text = "��ȷһ��";

                DoSearch(false, false, null);
            }

        }

        void menu_loadToOpenedEntityForm_Click(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ��ʵ�崰�ڵ�����");
                return;
            }
            string strPath = this.listView_records.SelectedItems[0].SubItems[0].Text;

            EntityForm form = null;

            form = MainForm.GetTopChildWindow<EntityForm>();
            if (form != null)
                Global.Activate(form);

            if (form == null)
            {
                form = new EntityForm();

                form.MdiParent = this.MainForm;
                form.MainForm = this.MainForm;
                form.Show();
            }

            Debug.Assert(form != null, "");

            form.LoadRecordOld(strPath, "", true);
        }

        void menu_loadToNewEntityForm_Click(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ��ʵ�崰�ڵ�����");
                return;
            }
            string strPath = this.listView_records.SelectedItems[0].SubItems[0].Text;

            EntityForm form = null;

            if (form == null)
            {
                form = new EntityForm();

                form.MdiParent = this.MainForm;
                form.MainForm = this.MainForm;
                form.Show();
            }

            Debug.Assert(form != null, "");

            form.LoadRecordOld(strPath, "", true);
        }

        // �Ƿ�����װ���Ѿ��򿪵���ϸ��?
        /// <summary>
        /// �Ƿ�����װ���Ѿ��򿪵���ϸ��?
        /// </summary>
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

        // ����listview
        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            EnableControlsInSearching(bEnable);
            this.listView_records.Enabled = bEnable;

            /*
            this.toolStrip_search.Enabled = bEnable;
            this.listView_records.Enabled = bEnable;

            this.comboBox_from.Enabled = bEnable;
            this.checkedComboBox_biblioDbNames.Enabled = bEnable;
            this.comboBox_matchStyle.Enabled = bEnable;

            if (this.comboBox_matchStyle.Text == "��ֵ")
                this.textBox_queryWord.Enabled = false;
            else
                this.textBox_queryWord.Enabled = bEnable;
            */
        }

        bool InSearching
        {
            get
            {
                if (this.comboBox_from.Enabled == true)
                    return false;
                return true;
            }
        }

        // ע: listview����
        void EnableControlsInSearching(bool bEnable)
        {
            // this.button_search.Enabled = bEnable;
            this.toolStrip_search.Enabled = bEnable;

            this.comboBox_from.Enabled = bEnable;
            this.checkedComboBox_biblioDbNames.Enabled = bEnable;
            this.comboBox_matchStyle.Enabled = bEnable;

            if (this.comboBox_matchStyle.Text == "��ֵ")
                this.textBox_queryWord.Enabled = false;
            else
                this.textBox_queryWord.Enabled = bEnable;

            if (this.m_lHitCount <= this.listView_records.Items.Count)
                this.ToolStripMenuItem_continueLoad.Enabled = false;
            else
                this.ToolStripMenuItem_continueLoad.Enabled = true;

            this.dp2QueryControl1.Enabled = bEnable;
        }

        private void BiblioSearchForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;

            RefreshPropertyView(false);
        }

        private void button_viewBiblioDbProperty_Click(object sender, EventArgs e)
        {

        }

        // ��Ŀ������
        private void MenuItem_viewBiblioDbProperty_Click(object sender, EventArgs e)
        {
            HtmlViewerForm dlg = new HtmlViewerForm();
            MainForm.SetControlFont(dlg, this.Font, false);

            string strText = "<html><body>";

            // Debug.Assert(false, "");

            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty property = this.MainForm.BiblioDbProperties[i];

                    strText += "<p>��Ŀ����: " + property.DbName + "; �﷨: " + property.Syntax + "</p>";
                }
            }

            strText += "</body></html>";

            dlg.HtmlString = strText;

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog();   // ? this
        }

        private void checkedComboBox_biblioDbNames_DropDown(object sender, EventArgs e)
        {
            if (this.checkedComboBox_biblioDbNames.Items.Count > 0)
                return;

            this.checkedComboBox_biblioDbNames.Items.Add("<ȫ��>");

            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty property = this.MainForm.BiblioDbProperties[i];
                    this.checkedComboBox_biblioDbNames.Items.Add(property.DbName);
                }
            }
        }

        // listview�ϵ��������˵�
        private void listView_records_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            int nSeletedItemCount = this.listView_records.SelectedItems.Count;
            string strFirstColumn = "";
            if (nSeletedItemCount > 0)
            {
                strFirstColumn = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 0);
            }

            menuItem = new MenuItem("װ���Ѵ򿪵��ֲᴰ(&L)");
            if (this.LoadToExistDetailWindow == true
                && this.MainForm.GetTopChildWindow<EntityForm>() != null)
                menuItem.DefaultItem = true;
            menuItem.Click += new System.EventHandler(this.menu_loadToOpenedEntityForm_Click);
            if (this.listView_records.SelectedItems.Count == 0
                || this.MainForm.GetTopChildWindow<EntityForm>() == null
                || String.IsNullOrEmpty(strFirstColumn) == true)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("װ���¿����ֲᴰ(&L)");
            if (this.LoadToExistDetailWindow == false
                || this.MainForm.GetTopChildWindow<EntityForm>() == null)
                menuItem.DefaultItem = true;
            menuItem.Click += new System.EventHandler(this.menu_loadToNewEntityForm_Click);
            if (this.listView_records.SelectedItems.Count == 0
                || String.IsNullOrEmpty(strFirstColumn) == true)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            if (String.IsNullOrEmpty(strFirstColumn) == true
    && nSeletedItemCount > 0)
            {
                string strKey = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);

                menuItem = new MenuItem("���� '" + strKey + "' (&S)");
                menuItem.DefaultItem = true;
                menuItem.Click += new System.EventHandler(this.listView_records_DoubleClick);
                contextMenu.MenuItems.Add(menuItem);

                menuItem = new MenuItem("���¿�����Ŀ��ѯ���� ���� '" + strKey + "' (&N)");
                menuItem.Click += new System.EventHandler(this.listView_searchKeysAtNewWindow_Click);
                contextMenu.MenuItems.Add(menuItem);

            }

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_cutToClipboard_Click);
            if (this.listView_records.SelectedIndices.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("����(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copyToClipboard_Click);
            if (this.listView_records.SelectedIndices.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("���Ƶ���(&S)");
            // menuItem.Click += new System.EventHandler(this.menu_copySingleColumnToClipboard_Click);
            if (this.listView_records.SelectedIndices.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            for (int i = 0; i < this.listView_records.Columns.Count; i++)
            {
                MenuItem subMenuItem = new MenuItem("������ '" + this.listView_records.Columns[i].Text + "'");
                subMenuItem.Tag = i;
                subMenuItem.Click += new System.EventHandler(this.menu_copySingleColumnToClipboard_Click);
                menuItem.MenuItems.Add(subMenuItem);
            }

            bool bHasClipboardObject = false;
            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null
                || iData.GetDataPresent(typeof(string)) == false)
                bHasClipboardObject = false;
            else
                bHasClipboardObject = true;

            menuItem = new MenuItem("ճ��[ǰ��](&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteFromClipboard_insertBefore_Click);
            if (bHasClipboardObject == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ճ��[���](&V)");
            menuItem.Click += new System.EventHandler(this.menu_pasteFromClipboard_insertAfter_Click);
            if (bHasClipboardObject == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Click += new System.EventHandler(this.menu_selectAllLines_Click);
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("��תѡ��(&R)");
            menuItem.Click += new System.EventHandler(this.menu_reverseSelectAllLines_Click);
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("�Ƴ���ѡ��� " + this.listView_records.SelectedItems.Count.ToString() + " ������(&R)");
            menuItem.Click += new System.EventHandler(this.menu_removeSelectedItems_Click);
            if (this.listView_records.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            {
                menuItem = new MenuItem("����(&F)");
                contextMenu.MenuItems.Add(menuItem);

                MenuItem subMenuItem = new MenuItem("��ӡ��ѯ�� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&C)");
                subMenuItem.Click += new System.EventHandler(this.menu_printClaim_Click);
                if (this.listView_records.SelectedItems.Count == 0 || this.InSearching == true
                    )
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

            }


            // bool bLooping = (stop != null && stop.State == 0);    // 0 ��ʾ���ڴ���

            // ������
            // ���ڼ�����ʱ�򣬲���������������������Ϊstop.BeginLoop()Ƕ�׺��Min Max Value֮��ı���ָ����⻹û�н��
            {
                menuItem = new MenuItem("������(&B)");
                contextMenu.MenuItems.Add(menuItem);

                MenuItem subMenuItem = new MenuItem("�����޸���Ŀ��¼ [" + this.listView_records.SelectedItems.Count.ToString() + "] (&Q)");
                subMenuItem.Click += new System.EventHandler(this.menu_quickChangeRecords_Click);
                if (this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("ִ�� MarcQuery �ű� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&M)");
                subMenuItem.Click += new System.EventHandler(this.menu_quickMarcQueryRecords_Click);
                if (this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("�����޸� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&C)");
                subMenuItem.Click += new System.EventHandler(this.menu_clearSelectedChangedRecords_Click);
                if (this.m_nChangedCount == 0 || this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("����ȫ���޸� [" + this.m_nChangedCount.ToString() + "] (&L)");
                subMenuItem.Click += new System.EventHandler(this.menu_clearAllChangedRecords_Click);
                if (this.m_nChangedCount == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("����ѡ�����޸� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&S)");
                subMenuItem.Click += new System.EventHandler(this.menu_saveSelectedChangedRecords_Click);
                if (this.m_nChangedCount == 0 || this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("����ȫ���޸� [" + this.m_nChangedCount.ToString() + "] (&A)");
                subMenuItem.Click += new System.EventHandler(this.menu_saveAllChangedRecords_Click);
                if (this.m_nChangedCount == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("�����µ� MarcQuery �ű��ļ� (&C)");
                subMenuItem.Click += new System.EventHandler(this.menu_createMarcQueryCsFile_Click);
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);


                subMenuItem = new MenuItem("ִ�� .fltx �ű� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&F)");
                subMenuItem.Click += new System.EventHandler(this.menu_quickFilterRecords_Click);
                if (this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("���Ƶ�������Ŀ�� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&N)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveBiblioRecToAnotherDatabase_Click);
                if (this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("�ƶ���������Ŀ�� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&M)...");
                subMenuItem.Click += new System.EventHandler(this.menu_moveBiblioRecToAnotherDatabase_Click);
                if (this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("ɾ����Ŀ��¼ [" + this.listView_records.SelectedItems.Count.ToString() + "] (&D)");
                subMenuItem.Click += new System.EventHandler(this.menu_deleteSelectedRecords_Click);
                if (this.listView_records.SelectedItems.Count == 0
                    || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);
            }

            // ����
            {
                menuItem = new MenuItem("����(&X)");
                contextMenu.MenuItems.Add(menuItem);

                MenuItem subMenuItem = new MenuItem("��������¼·���ļ� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&S)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveToRecordPathFile_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);


                subMenuItem = new MenuItem("������Ŀ��¼�����Ĳ��¼·����(ʵ���)��¼·���ļ� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&E)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveToEntityRecordPathFile_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("������Ŀ��¼�����Ķ�����¼·����(������)��¼·���ļ� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&X)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveToOrderRecordPathFile_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("������Ŀ��¼�������ڼ�¼·����(�ڿ�)��¼·���ļ� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&X)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveToIssueRecordPathFile_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("������Ŀ��¼��������ע��¼·����(��ע��)��¼·���ļ� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&X)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveToCommentRecordPathFile_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("������ MARC(ISO2709) �ļ� [" + this.listView_records.SelectedItems.Count.ToString() + " ] (&M)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveToMarcFile_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("������ XML �ļ� [" + this.listView_records.SelectedItems.Count.ToString() + " ] (&M)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveToXmlFile_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);
            }

            // װ��������ѯ��
            {
                menuItem = new MenuItem("װ��������ѯ�� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&L)");
                contextMenu.MenuItems.Add(menuItem);

                MenuItem subMenuItem = new MenuItem("��Ŀ��ѯ��");
                subMenuItem.Click += new System.EventHandler(this.menu_exportToBiblioSearchForm_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);


                subMenuItem = new MenuItem("ʵ���ѯ��");
                subMenuItem.Click += new System.EventHandler(this.menu_exportToItemSearchForm_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("������ѯ��");
                subMenuItem.Click += new System.EventHandler(this.menu_exportToOrderSearchForm_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("�ڲ�ѯ��");
                subMenuItem.Click += new System.EventHandler(this.menu_exportToIssueSearchForm_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("��ע��ѯ��");
                subMenuItem.Click += new System.EventHandler(this.menu_exportToCommentSearchForm_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);
            }

            // ��ǿ��¼���¼������
            {
                menuItem = new MenuItem("��ǳ��¼���¼Ϊ�յ����� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&L)");
                contextMenu.MenuItems.Add(menuItem);

                MenuItem subMenuItem = new MenuItem("���¼Ϊ��");
                subMenuItem.Click += new System.EventHandler(this.menu_maskEmptySubItems_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("������¼Ϊ��");
                subMenuItem.Click += new System.EventHandler(this.menu_maskEmptySubOrders_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("�ڼ�¼Ϊ��");
                subMenuItem.Click += new System.EventHandler(this.menu_maskEmptySubIssues_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("��ע��¼Ϊ��");
                subMenuItem.Click += new System.EventHandler(this.menu_maskEmptySubComments_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);
            }


            // ����
            {
                menuItem = new MenuItem("����(&I)");
                contextMenu.MenuItems.Add(menuItem);

                MenuItem subMenuItem = new MenuItem("�Ӽ�¼·���ļ��е���(&I)...");
                subMenuItem.Click += new System.EventHandler(this.menu_importFromRecPathFile_Click);
                if (this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);
            }

            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            /*

            menuItem = new MenuItem("ˢ�¿ؼ�");
            menuItem.Click += new System.EventHandler(this.menu_refreshControls_Click);
            contextMenu.MenuItems.Add(menuItem);
             * */

            menuItem = new MenuItem("ˢ������� [" + nSeletedItemCount.ToString() + "] (&R)");
            menuItem.Click += new System.EventHandler(this.menu_refreshSelectedItems_Click);
            if (nSeletedItemCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView_records, new Point(e.X, e.Y));
        }

        void menu_printClaim_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫװ���ӡ��ѯ������";
                goto ERROR1;
            }

            string strIssueDbName = "";
            if (this.listView_records.SelectedItems.Count > 0)
            {
                string strFirstRecPath = "";
                strFirstRecPath = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 0);
                string strDbName = Global.GetDbName(strFirstRecPath);
                if (string.IsNullOrEmpty(strDbName) == false)
                    strIssueDbName = this.MainForm.GetIssueDbName(strDbName);
            }

            List<string> recpaths = new List<string>();
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                string strRecPath = ListViewUtil.GetItemText(item, 0);
                if (string.IsNullOrEmpty(strRecPath) == false)
                    recpaths.Add(strRecPath);
            }

            PrintClaimForm form = new PrintClaimForm();
            form.MdiParent = this.MainForm;
            form.Show();

            if (string.IsNullOrEmpty(strIssueDbName) == false)
                form.PublicationType = PublicationType.Series;
            else
                form.PublicationType = PublicationType.Book;

            form.EnableControls(false);
            form.SetBiblioRecPaths(recpaths);
            form.EnableControls(true);

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
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
#if NO
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
#endif
                    ClearOneChange(item);
                }
            }
            finally
            {
                this.Cursor = oldCursor;
            }

            RefreshPropertyView(false);
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
                foreach (ListViewItem item in this.listView_records.Items)
                {
#if NO
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
#endif
                    ClearOneChange(item);
                }
            }
            finally
            {
                this.Cursor = oldCursor;
            }

            RefreshPropertyView(false);
        }


        // ����ѡ��������޸�
        void menu_saveSelectedChangedRecords_Click(object sender, EventArgs e)
        {
            // TODO: ȷʵҪ?

            if (this.m_nChangedCount == 0)
            {
                MessageBox.Show(this, "��ǰû���κ��޸Ĺ���������Ҫ����");
                return;
            }

            string strError = "";

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_records.SelectedItems)
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
        void menu_saveAllChangedRecords_Click(object sender, EventArgs e)
        {
            // TODO: ȷʵҪ?

            if (this.m_nChangedCount == 0)
            {
                MessageBox.Show(this, "��ǰû���κ��޸Ĺ���������Ҫ����");
                return;
            }

            string strError = "";

            List<ListViewItem> items = new List<ListViewItem>();
            foreach(ListViewItem item in this.listView_records.Items)
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
            this.listView_records.Enabled = false;
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

                    string strOutputPath = "";

                    stop.SetMessage("���ڱ�����Ŀ��¼ " + strRecPath);

                    byte[] baNewTimestamp = null;

                    long lRet = Channel.SetBiblioInfo(
                        stop,
                        "change",
                        strRecPath,
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
    "������Ŀ��¼ "+strRecPath+" ʱ����ʱ�����ƥ��: " + strError + "��\r\n\r\n�˼�¼���޷������档\r\n\r\n���������Ƿ�Ҫ˳������װ�ش˼�¼? \r\n\r\n(Yes ����װ�أ�\r\nNo ������װ�ء��������������ļ�¼����; \r\nCancel �ж������������)",
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
                                strRecPath,
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
                            strRecPath,
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
                this.listView_records.Enabled = true;
            }

            // 2013/10/22
            int nRet = RefreshListViewLines(items,
    out strError);
            if (nRet == -1)
                return -1;

            RefreshPropertyView(false);

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

        /// <summary>
        /// ˢ�������
        /// </summary>
        /// <param name="items_param">Ҫˢ�µ� ListViewItem ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
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
                loader.Channel = Channel;
                loader.Stop = stop;
                loader.RecPaths = recpaths;
                loader.Format = "id,cols";

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

        /// <summary>
        /// ˢ��ȫ����
        /// </summary>
        public void RefreshAllLines()
        {
            string strError = "";
            int nRet = 0;

            int nChangedCount = 0;
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_records.Items)
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
    "Ҫˢ�µ� " + this.listView_records.SelectedItems.Count.ToString() + " ���������� " + nChangedCount.ToString() + " ���޸ĺ���δ���档���ˢ�����ǣ��޸����ݻᶪʧ��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
    "BiblioSearchForm",
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

            RefreshPropertyView(false);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        // ˢ����ѡ�������С�Ҳ�������´����ݿ���װ�������
        void menu_refreshSelectedItems_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫˢ�µ������";
                goto ERROR1;
            }

            int nChangedCount = 0;
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_records.SelectedItems)
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
    "Ҫˢ�µ� " + this.listView_records.SelectedItems.Count.ToString() + " ���������� " + nChangedCount.ToString() + " ���޸ĺ���δ���档���ˢ�����ǣ��޸����ݻᶪʧ��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
    "BiblioSearchForm",
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

            RefreshPropertyView(false);
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

#if NO
        // ˢ����ѡ����С�Ҳ�������´����ݿ���װ�������
        void menu_refreshSelectedItems_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫˢ������е�����";
                goto ERROR1;
            }

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ˢ������� ...");
            stop.BeginLoop();

            this.EnableControls(false);
            try
            {
                if (stop != null)
                    stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);
                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null
                        && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    if (string.IsNullOrEmpty(item.Text) == true)
                        continue;

                    if (stop != null)
                    {
                        stop.SetMessage("����ˢ������� " + item.Text + " ...");
                        stop.SetProgressValue(i++);
                    }
                    nRet = RefreshOneBrowseLine(item,
    out strError);
                    if (nRet == -1)
                    {
                        DialogResult result = MessageBox.Show(this,
                            "ˢ���������ʱ����: " + strError + "��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
                            "BiblioSearchForm",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.Cancel)
                            break;
                    }
                }
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

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif

        // ��һ���¿�����Ŀ��ѯ���ڼ���key
        void listView_searchKeysAtNewWindow_Click(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫ����������");
                return;
            }

            BiblioSearchForm form = new BiblioSearchForm();
            form.MdiParent = this.MainForm;
            // form.MainForm = this.MainForm;
            form.Show();

            ItemQueryParam query = (ItemQueryParam)this.listView_records.SelectedItems[0].Tag;
            Debug.Assert(query != null, "");

            ItemQueryParam input_query = new ItemQueryParam();

            input_query.QueryWord = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);
            input_query.DbNames = query.DbNames;
            input_query.From = query.From;
            input_query.MatchStyle = "��ȷһ��";

            // �������м�¼(������key)
            form.DoSearch(false, false, input_query);
        }

        string m_strUsedMarcQueryFilename = "";

        // װ����Ŀ���������XMLƬ��
        static int LoadXmlFragment(string strXml,
            out XmlDocument domXmlFragment,
            out string strError)
        {
            strError = "";

            domXmlFragment = null;

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

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//unimarc:leader | //unimarc:controlfield | //unimarc:datafield | //usmarc:leader | //usmarc:controlfield | //usmarc:datafield", nsmgr); // | //dprms:file
            foreach (XmlNode node in nodes)
            {
                node.ParentNode.RemoveChild(node);
            }

            domXmlFragment = new XmlDocument();
            domXmlFragment.LoadXml("<root />");
            domXmlFragment.DocumentElement.InnerXml = dom.DocumentElement.InnerXml;

            return 0;
        }

        // ����һ���µ� MarcQuery �ű��ļ�
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

        internal int GetItemsChangeCount(List<ListViewItem> items)
        {
            if (this.m_nChangedCount == 0)
                return 0;   // ����ٶ�

            int nResult = 0;
            foreach (ListViewItem item in items)
            {
                if (IsItemChanged(item) == true)
                    nResult++;
            }
            return nResult;
        }

        int m_nChangedCount = 0;

        void menu_quickMarcQueryRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫִ�� MarcQuery �ű�������";
                goto ERROR1;
            }

            // ��Ŀ��Ϣ����
            // ����Ѿ���ʼ�����򱣳�
            if (this.m_biblioTable == null)
                this.m_biblioTable = new Hashtable();

            // this.m_biblioTable.Clear();

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

            host.CodeFileName = this.m_strUsedMarcQueryFilename;
            {
                host.MainForm = this.MainForm;
                host.UiForm = this;
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

            this.MainForm.OperHistory.AppendHtml("<div class='debug begin'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ��ʼִ�нű� " + dlg.FileName + "</div>");

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���������Ŀ��¼ִ�� MarcQuery �ű� ...");
            stop.BeginLoop();

            this.EnableControls(false);

            this.listView_records.Enabled = false;
            try
            {
                if (stop != null)
                    stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                {
                    host.MainForm = this.MainForm;
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
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    if (string.IsNullOrEmpty(item.Text) == true)
                        continue;

                    items.Add(item);
                }

                bool bOldSource = true; // �Ƿ�Ҫ�� OldXml ��ʼ����

                int nChangeCount = this.GetItemsChangeCount(items);
                if (nChangeCount > 0)
                {
                    bool bHideMessageBox = true;
                    DialogResult result = MessageDialog.Show(this,
                        "��ǰѡ���� " + items.Count.ToString() + " ���������� " + nChangeCount + " ���޸���δ���档\r\n\r\n������ν����޸�? \r\n\r\n(�����޸�) ���½����޸ģ�������ǰ�ڴ��е��޸�; \r\n(�����޸�) ���ϴε��޸�Ϊ���������޸�; \r\n(����) ������������",
    MessageBoxButtons.YesNoCancel,
    MessageBoxDefaultButton.Button1,
    null,
    ref bHideMessageBox,
    new string[] { "�����޸�", "�����޸�", "����" });
                    if (result == DialogResult.Cancel)
                    {
                        // strError = "����";
                        return;
                    }
                    if (result == DialogResult.No)
                    {
                        bOldSource = false;
                    }
                }


                ListViewBiblioLoader loader = new ListViewBiblioLoader(this.Channel,
                    stop,
                    items,
                    this.m_biblioTable);
                loader.Prompt -= new MessagePromptEventHandler(loader_Prompt);
                loader.Prompt += new MessagePromptEventHandler(loader_Prompt);


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

                    string strXml = "";
                    if (bOldSource == true)
                    {
                        strXml = info.OldXml;
                        // ������һ�ε��޸�
                        if (string.IsNullOrEmpty(info.NewXml) == false)
                        {
                            info.NewXml = "";
                            this.m_nChangedCount--;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(info.NewXml) == false)
                            strXml = info.NewXml;
                        else
                            strXml = info.OldXml;
                    }

                    string strMARC = "";
                    string strMarcSyntax = "";
                    // ��XML��ʽת��ΪMARC��ʽ
                    // �Զ������ݼ�¼�л��MARC�﷨
                    nRet = MarcUtil.Xml2Marc(strXml,    // info.OldXml,
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

                    host.MainForm = this.MainForm;
                    host.RecordPath = info.RecPath;
                    host.MarcRecord = new MarcRecord(strMARC);
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
                        strXml = info.OldXml;
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
                        }

                        item.ListViewItem.BackColor = SystemColors.Info;
                        item.ListViewItem.ForeColor = SystemColors.InfoText;
                    }

                    // ��ʾΪ��������ʽ
                    i++;
                }

                {
                    host.MainForm = this.MainForm;
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
                strError = "ִ�нű��Ĺ����г����쳣: " + ExceptionUtil.GetDebugText(ex);
                goto ERROR1;
            }
            finally
            {
                if (host != null)
                    host.FreeResources();

                this.listView_records.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControls(true);

                this.MainForm.OperHistory.AppendHtml("<div class='debug end'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ����ִ�нű� " + dlg.FileName + "</div>");
            }

            RefreshPropertyView(false);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void loader_Prompt(object sender, MessagePromptEventArgs e)
        {
            // TODO: ���ٳ��ִ˶Ի��򡣲��������и��������ƣ�ͬһλ��ʧ�ܶ�κ���Ҫ���ֶԻ���ź�
            if (e.Actions == "yes,no,cancel")
            {
#if NO
                DialogResult result = MessageBox.Show(this,
    e.MessageText + "\r\n\r\n�Ƿ����Բ���?\r\n\r\n(��: ����;  ��: �������β�������������Ĳ���; ȡ��: ֹͣȫ������)",
    "ReportForm",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                    e.ResultAction = "yes";
                else if (result == DialogResult.Cancel)
                    e.ResultAction = "cancel";
                else
                    e.ResultAction = "no";
#endif
                DialogResult result = AutoCloseMessageBox.Show(this,
    e.MessageText + "\r\n\r\n���Զ����Բ���\r\n\r\n(�����Ͻǹرհ�ť�����ж�������)",
    20 * 1000,
    "BiblioSearchForm");
                if (result == DialogResult.Cancel)
                    e.ResultAction = "no";
                else
                    e.ResultAction = "yes";
            }
        }

#if NO
        void menu_quickMarcQueryRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
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

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���������Ŀ��¼ִ�� MarcQuery �ű� ...");
            stop.BeginLoop();

            this.EnableControls(false);

            this.listView_records.Enabled = false;
            try
            {
                if (stop != null)
                    stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null
                        && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    string strRecPath = item.Text;

                    if (string.IsNullOrEmpty(strRecPath) == true)
                        continue;

                    if (stop != null)
                    {
                        stop.SetMessage("���ڻ�ȡ��Ŀ��¼ " + strRecPath);
                        stop.SetProgressValue(i);
                    }

                    BiblioInfo info = null;
                    nRet = GetBiblioInfo(
                        false,
                        item,
                        out info,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (info == null)
                        continue;
#if NO
                    string[] results = null;
                    byte[] baTimestamp = null;
                    // �����Ŀ��¼
                    long lRet = Channel.GetBiblioInfos(
                        stop,
                        strRecPath,
                    "",
                        new string[] { "xml" },   // formats
                        out results,
                        out baTimestamp,
                        out strError);
                    if (lRet == 0)
                        goto ERROR1;
                    if (lRet == -1)
                        goto ERROR1;

                    if (results == null || results.Length == 0)
                    {
                        strError = "results error";
                        goto ERROR1;
                    }

                    string strXml = results[0];

                    XmlDocument domXmlFragment = null;

                    // װ����Ŀ���������XMLƬ��
                    nRet = LoadXmlFragment(strXml,
            out domXmlFragment,
            out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    string strMARC = "";
                    string strMarcSyntax = "";
                    // ��XML��ʽת��ΪMARC��ʽ
                    // �Զ������ݼ�¼�л��MARC�﷨
                    nRet = MarcUtil.Xml2Marc(strXml,
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

                    // �洢�������Ŀ��¼ XML
                    BiblioInfo info = null;
                    if (this.m_biblioTable != null)
                    {
                        info = (BiblioInfo)this.m_biblioTable[strRecPath];
                        if (info == null)
                        {
                            info = new BiblioInfo();
                            info.RecPath = strRecPath;
                            this.m_biblioTable[strRecPath] = info;
                        }

                        info.OldXml = strXml;
                        info.NewXml = "";
                    }
#endif
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

                    host.MainForm = this.MainForm;
                    host.MarcRecord = new MarcRecord(strMARC);
                    host.MarcSyntax = strMarcSyntax;
                    host.Changed = false;

                    host.Main();

                    if (host.Changed == true)
                    {
                        string strXml = info.OldXml;
                        nRet = MarcUtil.Marc2XmlEx(host.MarcRecord.Text,
                            strMarcSyntax,
                            ref strXml,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        // �ϳ�����XMLƬ��
                        if (domXmlFragment != null
                            && string.IsNullOrEmpty(domXmlFragment.DocumentElement.InnerXml) == false)
                        {
                            XmlDocumentFragment fragment = domMarc.CreateDocumentFragment();
                            try
                            {
                                fragment.InnerXml = domXmlFragment.DocumentElement.InnerXml;
                            }
                            catch (Exception ex)
                            {
                                strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                                goto ERROR1;
                            }

                            domMarc.DocumentElement.AppendChild(fragment);
                        }


                        strXml = domMarc.OuterXml;
#endif

                        if (info != null)
                        {
                            if (string.IsNullOrEmpty(info.NewXml) == true)
                                this.m_nChangedCount++;
                            info.NewXml = strXml;
                        }

                        item.BackColor = SystemColors.Info;
                        item.ForeColor = SystemColors.InfoText;
#if NO
                        byte[] baNewTimestamp = null;
                        string strOutputPath = "";
                        lRet = Channel.SetBiblioInfo(
                            stop,
                            "change",
                            strRecPath,
                            "xml",
                            strXml,
                            baTimestamp,
                            "",
                            out strOutputPath,
                            out baNewTimestamp,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;
#endif
                    }

                    this.MainForm.OperHistory.AppendHtml("<p>" + HttpUtility.HtmlEncode(strRecPath) + "</p>");

                    // ��ʾΪ��������ʽ

#if NO
                    if (nRet == -1)
                    {
                        DialogResult result = MessageBox.Show(this,
        "ˢ���������ʱ����: " + strError + "��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
        "BiblioSearchForm",
        MessageBoxButtons.OKCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.Cancel)
                            break;
                    }
#endif
                    i++;
                }
            }
            finally
            {
                this.listView_records.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControls(true);
            }

            DoViewComment(false);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
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

            string strWarningInfo = "";
            string[] saAddRef = {
                                    // 2011/4/20 ����
                                    "system.dll",
                                    "system.drawing.dll",
                                    "system.windows.forms.dll",
                                    "system.xml.dll",
                                    "System.Runtime.Serialization.dll",

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
                Environment.CurrentDirectory + "\\dp2circulation.exe",
            };

            // 2013/12/16
            nRet = ScriptManager.GetRef(strCsFileName,
                ref saAddRef,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // ֱ�ӱ��뵽�ڴ�
            // parameters:
            //		refs	���ӵ�refs�ļ�·����·���п��ܰ�����%installdir%
            nRet = ScriptManager.CreateAssembly_1(strContent,
                saAddRef,
                "",
                out assembly,
                out strError,
                out strWarningInfo);
            if (nRet == -1)
                goto ERROR1;

            // �õ�Assembly��Host������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                assembly,
                "dp2Circulation.MarcQueryHost");
            if (entryClassType == null)
            {
                strError = strCsFileName + " ��û���ҵ� dp2Circulation.MarcQueryHost ������";
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



        string m_strUsedMarcFilterFilename = "";

        void menu_quickFilterRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ����ִ�нű�������";
                goto ERROR1;
            }

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ�� MARC �������ű��ļ�";
            dlg.FileName = this.m_strUsedMarcFilterFilename;
            dlg.Filter = "MARC�������ű��ļ� (*.fltx)|*.fltx|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.m_strUsedMarcFilterFilename = dlg.FileName;

            ColumnFilterDocument filter = new ColumnFilterDocument();

            nRet = PrepareMarcFilter(
                this,
                this.MainForm.DataDir,
                this.m_strUsedMarcFilterFilename,
                filter,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���������Ŀ��¼ִ�� .fltx �ű� ...");
            stop.BeginLoop();

            this.EnableControls(false);

            this.listView_records.Enabled = false;
            try
            {
                if (stop != null)
                    stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null
                        && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    string strRecPath = item.Text;

                    if (string.IsNullOrEmpty(strRecPath) == true)
                        continue;

                    if (stop != null)
                    {
                        stop.SetMessage("���ڻ�ȡ��Ŀ��¼ " + strRecPath);
                        stop.SetProgressValue(i);
                    }

                    string[] results = null;
                    byte[] baTimestamp = null;
                    // �����Ŀ��¼
                    long lRet = Channel.GetBiblioInfos(
                        stop,
                        strRecPath,
                    "",
                        new string[] { "xml" },   // formats
                        out results,
                        out baTimestamp,
                        out strError);
                    if (lRet == 0)
                        goto ERROR1;
                    if (lRet == -1)
                        goto ERROR1;

                    if (results == null || results.Length == 0)
                    {
                        strError = "results error";
                        goto ERROR1;
                    }

                    string strXml = results[0];

                    string strMARC = "";
                    string strMarcSyntax = "";
                    // ��XML��ʽת��ΪMARC��ʽ
                    // �Զ������ݼ�¼�л��MARC�﷨
                    nRet = MarcUtil.Xml2Marc(strXml,
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

                    filter.Host = new ColumnFilterHost();
                    filter.Host.ColumnTable = new System.Collections.Hashtable();
                    nRet = filter.DoRecord(
    null,
    strMARC,
    strMarcSyntax,
    i,
    out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    this.MainForm.OperHistory.AppendHtml("<p>" + HttpUtility.HtmlEncode(strRecPath) + "</p>");
                    foreach(string key in filter.Host.ColumnTable.Keys)
                    {
                        string strHtml = "<p>" + HttpUtility.HtmlEncode(key + "=" + (string)filter.Host.ColumnTable[key]) + "</p>";
                        this.MainForm.OperHistory.AppendHtml(strHtml);
                    }

#if NO
                    if (nRet == -1)
                    {
                        DialogResult result = MessageBox.Show(this,
        "ˢ���������ʱ����: " + strError + "��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
        "BiblioSearchForm",
        MessageBoxButtons.OKCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.Cancel)
                            break;
                    }
#endif
                    i++;
                }
            }
            finally
            {
                this.listView_records.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControls(true);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ׼���ű�����
        static int PrepareMarcFilter(
            IWin32Window owner,
            string strDataDir,
            string strFilterFileName,
            ColumnFilterDocument filter,
            out string strError)
        {
            strError = "";

            if (FileUtil.FileExist(strFilterFileName) == false)
            {
                strError = "�ļ� '" + strFilterFileName + "' ������";
                goto ERROR1;
            }

            string strWarning = "";

            string strLibPaths = "\"" + strDataDir + "\"";

            filter.strOtherDef = "dp2Circulation.ColumnFilterHost Host = null;";
            filter.strPreInitial = " ColumnFilterDocument doc = (ColumnFilterDocument)this.Document;\r\n";
            filter.strPreInitial += " Host = doc.Host;\r\n";
            /*
           filter.strOtherDef = entryClassType.FullName + " Host = null;";

           filter.strPreInitial = " ColumnFilterDocument doc = (ColumnFilterDocument)this.Document;\r\n";
           filter.strPreInitial += " Host = ("
               + entryClassType.FullName + ")doc.Host;\r\n";
            * */

            try
            {
                filter.Load(strFilterFileName);
            }
            catch (Exception ex)
            {
                strError = "�ļ� " + strFilterFileName + " װ�ص�MarcFilterʱ��������: " + ex.Message;
                goto ERROR1;
            }

            string strCode = "";    // c#����
            int nRet = filter.BuildScriptFile(out strCode,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // һЩ��Ҫ�����ӿ�
            string[] saAddRef1 = {
										 Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
										 //Environment.CurrentDirectory + "\\digitalplatform.rms.client.dll",
										 //Environment.CurrentDirectory + "\\digitalplatform.library.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
										 // Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
										 Environment.CurrentDirectory + "\\dp2circulation.exe"
                };

            // fltx�ļ�����ʽ���������ӿ�
            string[] saAdditionalRef = filter.GetRefs();

            // �ϲ������ӿ�
            string[] saTotalFilterRef = new string[saAddRef1.Length + saAdditionalRef.Length];
            Array.Copy(saAddRef1, saTotalFilterRef, saAddRef1.Length);
            Array.Copy(saAdditionalRef, 0,
                saTotalFilterRef, saAddRef1.Length,
                saAdditionalRef.Length);

            Assembly assemblyFilter = null;

            // ����Script��Assembly
            // �������ڶ�saRef���ٽ��к��滻
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saTotalFilterRef,
                strLibPaths,
                out assemblyFilter,
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
                MessageBox.Show(owner, strWarning);
            }

            filter.Assembly = assemblyFilter;
            return 0;
        ERROR1:
            return -1;
        }

        void menu_quickChangeRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ�����޸ĵ�����";
                goto ERROR1;
            }

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����޸���Ŀ��¼ ...");
            stop.BeginLoop();

            this.EnableControls(false);

            this.listView_records.Enabled = false;
            try
            {
                StringBuilder strLines = new StringBuilder(4096);

                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    strLines.Append(item.Text + "\r\n");
                }

                // �´�һ�������޸���Ŀ����
                QuickChangeBiblioForm form = new QuickChangeBiblioForm();
                // form.MainForm = this.MainForm;
                form.MdiParent = this.MainForm;
                form.Show();

                form.RecPathLines = strLines.ToString();
                if (form.SetChangeParameters() == false)
                {
                    form.Close();
                    return;
                }

                // return:
                //      -1  ����
                //      0   ��������
                //      1   ��������
                nRet = form.DoRecPathLines();
                form.Close();

                if (nRet == 0)
                    return;

                if (nRet == -1)
                {
                    DialogResult result = MessageBox.Show(this,
"���ּ�¼�Ѿ��������޸ģ��Ƿ���Ҫˢ�������? (OK ˢ�£�Cancel ����ˢ��)",
"BiblioSearchForm",
MessageBoxButtons.OKCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                    if (result == System.Windows.Forms.DialogResult.Cancel)
                        return;
                }

                if (stop != null)
                    stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null
                        && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    if (string.IsNullOrEmpty(item.Text) == true)
                        continue;

                    if (stop != null)
                    {
                        stop.SetMessage("����ˢ������� " + item.Text + " ...");
                        stop.SetProgressValue(i++);
                    }
                    nRet = RefreshBrowseLine(item,
    out strError);
                    if (nRet == -1)
                    {
                        DialogResult result = MessageBox.Show(this,
        "ˢ���������ʱ����: " + strError + "��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
        "BiblioSearchForm",
        MessageBoxButtons.OKCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.Cancel)
                            break;
                    }
                }
            }
            finally
            {
                this.listView_records.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControls(true);
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ���浽������Ŀ��
        // parameters:
        //      bCopy   �Ƿ�Ϊ���ơ������ false����ʾ�ƶ�
        // return:
        //      -1  ����
        //      0   ����
        //      1   �ɹ�
        int CopyToAnotherDatabase(
            bool bCopy,
            out string strError)
        {
            strError = "";
            // int nRet = 0;

            string strActionName = "";
            if (bCopy)
                strActionName = "����";
            else
                strActionName = "�ƶ�";

            // ѡ��Ŀ�����������ѡ���Ƿ�Ҫ��������¼Ҳ�����ȥ
            // ��Ҫѯ�ʱ����·��
            BiblioSaveToDlg dlg = new BiblioSaveToDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.Text = strActionName + "��Ŀ��¼�����ݿ�";

            dlg.MessageText = "��ָ����Ŀ��¼Ҫ׷��"+strActionName+"����λ��";
            dlg.EnableCopyChildRecords = true;

            dlg.BuildLink = false;

            if (bCopy)
            dlg.CopyChildRecords = false;
            else
                dlg.CopyChildRecords = true;

            if (bCopy)
                dlg.MessageText += "\r\n\r\n(ע��������*��ѡ��*�Ƿ�����Ŀ��¼�����Ĳᡢ�ڡ�������ʵ���¼�Ͷ�����Դ)\r\n\r\n��ѡ������Ŀ��¼���Ƶ�:";
            else
            {
                dlg.MessageText += "\r\n\r\nע��\r\n1) ��ǰִ�е����ƶ������Ǹ��Ʋ���;\r\n2) ��Ŀ��¼�����Ĳᡢ�ڡ�������ʵ���¼�Ͷ�����Դ�ᱻһ���ƶ���Ŀ��λ��";
                dlg.EnableCopyChildRecords = false;
            }
            // TODO: Ҫ�ü�¼IDΪ�ʺţ����Ҳ��ɸĶ�

            dlg.CurrentBiblioRecPath = "";  // Դ��¼·���� ���������������Դ��¼·������ȷ����
            this.MainForm.AppInfo.LinkFormState(dlg, "BiblioSearchform_BiblioSaveToDlg_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return 0;

            if (Global.IsAppendRecPath(dlg.RecPath) == false)
            {
                strError = "Ŀ���¼·�� '"+dlg.RecPath+"' ���Ϸ���������׷�ӷ�ʽ��·����Ҳ����˵ ID ���ֱ���Ϊ�ʺ�";
                goto ERROR1;
            }
            // TODO: ���Դ��Ŀ�������ͬ��Ҫ����

            string strAction = "";
            if (bCopy)
            {
                if (dlg.CopyChildRecords == false)
                    strAction = "onlycopybiblio";
                else
                    strAction = "copy";
            }
            else
            {
                if (dlg.CopyChildRecords == false)
                    strAction = "onlymovebiblio";
                else
                    strAction = "move";
            }

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����"+strActionName+"��Ŀ��¼�����ݿ� ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);
            this.listView_records.Enabled = false;
            try
            {
                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                List<ListViewItem> moved_items = new List<ListViewItem>();
                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    string strRecPath = ListViewUtil.GetItemText(item, 0);

                    if (string.IsNullOrEmpty(strRecPath) == true)
                        continue;

                    // �۲�Դ��¼�Ƿ���998$t ?

                    // �Ƿ�Ҫ�Զ�����998�ֶ�����?

                    string strOutputBiblioRecPath = "";
                    byte[] baOutputTimestamp = null;
                    string strOutputBiblio = "";

                    stop.SetMessage("����"+strActionName+"��Ŀ��¼ '" + strRecPath + "' �� '" + dlg.RecPath + "' ...");

                    // result.Value:
                    //      -1  ����
                    //      0   �ɹ���û�о�����Ϣ��
                    //      1   �ɹ����о�����Ϣ��������Ϣ�� result.ErrorInfo ��
                    long lRet = this.Channel.CopyBiblioInfo(
                        this.stop,
                        strAction,
                        strRecPath,
                        "xml",
                        null,
                        null,    // this.BiblioTimestamp,
                        dlg.RecPath,
                        null,   // strXml,
                        "",
                        out strOutputBiblio,
                        out strOutputBiblioRecPath,
                        out baOutputTimestamp,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (bCopy == false)
                        moved_items.Add(item);

                    stop.SetProgressValue(++i);
                }

                foreach (ListViewItem item in moved_items)
                {
                    item.Remove();
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
                this.listView_records.Enabled = true;
            }

            return 1;
        ERROR1:
            // MessageBox.Show(this, strError);
            return -1;
        }

        // ���浽������Ŀ��
        void menu_saveBiblioRecToAnotherDatabase_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ���浽������Ŀ��
            // parameters:
            //      bCopy   �Ƿ�Ϊ���ơ������ false����ʾ�ƶ�
            // return:
            //      -1  ����
            //      0   ����
            //      1   �ɹ�
            nRet = CopyToAnotherDatabase(
                true,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �ƶ���������Ŀ��
        void menu_moveBiblioRecToAnotherDatabase_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ���浽������Ŀ��
            // parameters:
            //      bCopy   �Ƿ�Ϊ���ơ������ false����ʾ�ƶ�
            // return:
            //      -1  ����
            //      0   ����
            //      1   �ɹ�
            nRet = CopyToAnotherDatabase(
                false,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_maskEmptySubItems_Click(object sender, EventArgs e)
        {
            string strError = "";

            List<ListViewItem> items = new List<ListViewItem>();
            int nRet = GetEmptySubItems("item",
                out items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            ListViewUtil.ClearSelection(this.listView_records);
            foreach (ListViewItem item in items)
            {
                item.Selected = true;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_maskEmptySubOrders_Click(object sender, EventArgs e)
        {
            string strError = "";

            List<ListViewItem> items = new List<ListViewItem>();
            int nRet = GetEmptySubItems("order",
                out items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            ListViewUtil.ClearSelection(this.listView_records);
            foreach (ListViewItem item in items)
            {
                item.Selected = true;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_maskEmptySubIssues_Click(object sender, EventArgs e)
        {
            string strError = "";

            List<ListViewItem> items = new List<ListViewItem>();
            int nRet = GetEmptySubItems("issue",
                out items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            ListViewUtil.ClearSelection(this.listView_records);
            foreach (ListViewItem item in items)
            {
                item.Selected = true;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_maskEmptySubComments_Click(object sender, EventArgs e)
        {
            string strError = "";

            List<ListViewItem> items = new List<ListViewItem>();
            int nRet = GetEmptySubItems("comment",
                out items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            ListViewUtil.ClearSelection(this.listView_records);
            foreach (ListViewItem item in items)
            {
                item.Selected = true;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_exportToBiblioSearchForm_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫװ��������ѯ������";
                goto ERROR1;
            }

            BiblioSearchForm form = new BiblioSearchForm();
            form.MdiParent = this.MainForm;
            form.Show();

            form.EnableControls(false);
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                string strLine = Global.BuildLine(item);
                form.AddLineToBrowseList(strLine);
            }
            form.EnableControls(true);

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_exportToItemSearchForm_Click(object sender, EventArgs e)
        {
            string strError = "";

            int nRet = ExportToItemSearchForm("item",
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }



        void menu_exportToOrderSearchForm_Click(object sender, EventArgs e)
        {
            string strError = "";

            int nRet = ExportToItemSearchForm("order",
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_exportToIssueSearchForm_Click(object sender, EventArgs e)
        {
            string strError = "";

            int nRet = ExportToItemSearchForm("issue",
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_exportToCommentSearchForm_Click(object sender, EventArgs e)
        {
            string strError = "";

            int nRet = ExportToItemSearchForm("comment",
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ����ѡ��� ? ����Ŀ��¼�����Ĳ��¼·��������(ʵ���)��¼·���ļ�
        void menu_saveToEntityRecordPathFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            int nRet = SaveToEntityRecordPathFile("item",
                null,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_saveToOrderRecordPathFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            int nRet = SaveToEntityRecordPathFile("order",
                null,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_saveToIssueRecordPathFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            int nRet = SaveToEntityRecordPathFile("issue",
                null,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_saveToCommentRecordPathFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            int nRet = SaveToEntityRecordPathFile("comment",
                null,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        int ExportToItemSearchForm(
    string strDbType,
    out string strError)
        {
            strError = "";
            string strTempFileName = Path.Combine(this.MainForm.DataDir, "~export_to_searchform.txt");
            int nRet = SaveToEntityRecordPathFile(strDbType,
                strTempFileName,
                out strError);
            if (nRet == -1)
                return -1;

#if NO
            // TODO: ���Ϊ�������͵� SearchForm �ࡣ�����Ƴ�ʱ�������������E�����Ͳ���
            ItemSearchForm form = new ItemSearchForm();
            form.DbType = strDbType;
            form.MdiParent = this.MainForm;
            form.Show();
#endif
            ItemSearchForm form = this.MainForm.OpenItemSearchForm(strDbType);

            nRet = form.ImportFromRecPathFile(strTempFileName,
            out strError);
            if (nRet == -1)
                return -1;
            return 0;
        }

        int SaveToEntityRecordPathFile(
            string strDbType,
            string strFileName,
            out string strError)
        {
            strError = "";
            int nCount = 0;
            int nRet = 0;

            string strDbTypeName = "";
            if (strDbType == "item")
                strDbTypeName = "ʵ��";
            else if (strDbType == "order")
                strDbTypeName = "����";
            else if (strDbType == "issue")
                strDbTypeName = "��";
            else if (strDbType == "comment")
                strDbTypeName = "��ע";

            bool bAppend = true;

            if (string.IsNullOrEmpty(strFileName) == true)
            {
                // ѯ���ļ���
                SaveFileDialog dlg = new SaveFileDialog();

                dlg.Title = "��ָ��Ҫ�����(" + strDbTypeName + "��)��¼·���ļ���";
                dlg.CreatePrompt = false;
                dlg.OverwritePrompt = false;
                dlg.FileName = this.ExportEntityRecPathFilename;
                // dlg.InitialDirectory = Environment.CurrentDirectory;
                dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";

                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() != DialogResult.OK)
                    return 0;

                this.ExportEntityRecPathFilename = dlg.FileName;

                if (File.Exists(this.ExportEntityRecPathFilename) == true)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��¼·���ļ� '" + this.ExportEntityRecPathFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel ��������)",
                        "BiblioSearchForm",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.Cancel)
                        return 0;
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
            }
            else
            {
                this.ExportEntityRecPathFilename = strFileName;

                bAppend = false;
            }



            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ü�¼·�� ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);
            this.listView_records.Enabled = false;

            // �����ļ�
            StreamWriter sw = new StreamWriter(this.ExportEntityRecPathFilename,
                bAppend,	// append
                System.Text.Encoding.UTF8);
            try
            {
                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
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

                    string strBiblioRecPath = item.Text;

                    if (string.IsNullOrEmpty(strBiblioRecPath) == true)
                        continue;

                    List<string> recpaths = null;

                    if (strDbType == "item")
                    {
                        // ���һ����Ŀ��¼������ȫ��ʵ���¼·��
                        // return:
                        //      -1  ����
                        //      0   û��װ��
                        //      1   �Ѿ�װ��
                        nRet = EntityControl.GetEntityRecPaths(
                            stop,
                            this.Channel,
                            strBiblioRecPath,
                            out recpaths,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else if (strDbType == "order")
                    {
                        // return:
                        //      -1  ����
                        //      0   û��װ��
                        //      1   �Ѿ�װ��
                        nRet = OrderControl.GetOrderRecPaths(
                            stop,
                            this.Channel,
                            strBiblioRecPath,
                            out recpaths,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else if (strDbType == "issue")
                    {
                        // return:
                        //      -1  ����
                        //      0   û��װ��
                        //      1   �Ѿ�װ��
                        nRet = IssueControl.GetIssueRecPaths(
                            stop,
                            this.Channel,
                            strBiblioRecPath,
                            out recpaths,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else if (strDbType == "comment")
                    {
                        // return:
                        //      -1  ����
                        //      0   û��װ��
                        //      1   �Ѿ�װ��
                        nRet = CommentControl.GetCommentRecPaths(
                            stop,
                            this.Channel,
                            strBiblioRecPath,
                            out recpaths,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }

                    nCount += recpaths.Count;
                    foreach (string recpath in recpaths)
                    {
                        sw.WriteLine(recpath);
                    }

                    stop.SetProgressValue(++i);
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
                this.listView_records.Enabled = true;

                if (sw != null)
                    sw.Close();
            }

            string strExportStyle = "����";
            if (bAppend == true)
                strExportStyle = "׷��";

            this.MainForm.StatusBarMessage = strDbTypeName + "��¼��¼·�� " + nCount.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportEntityRecPathFilename;
            return 1;
        ERROR1:
            return -1;
        }

        int GetEmptySubItems(
            string strDbType,
            out List<ListViewItem> items,
            out string strError)
        {
            strError = "";
            int nCount = 0;
            int nRet = 0;

            items = new List<ListViewItem>();

            string strDbTypeName = "";
            if (strDbType == "item")
                strDbTypeName = "ʵ��";
            else if (strDbType == "order")
                strDbTypeName = "����";
            else if (strDbType == "issue")
                strDbTypeName = "��";
            else if (strDbType == "comment")
                strDbTypeName = "��ע";

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ü�¼·�� ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);
            this.listView_records.Enabled = false;

            try
            {
                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
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

                    string strBiblioRecPath = item.Text;

                    if (string.IsNullOrEmpty(strBiblioRecPath) == true)
                        continue;

                    List<string> recpaths = null;

                    if (strDbType == "item")
                    {
                        // ���һ����Ŀ��¼������ȫ��ʵ���¼·��
                        // return:
                        //      -1  ����
                        //      0   û��װ��
                        //      1   �Ѿ�װ��
                        nRet = EntityControl.GetEntityRecPaths(
                            stop,
                            this.Channel,
                            strBiblioRecPath,
                            out recpaths,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else if (strDbType == "order")
                    {
                        // return:
                        //      -1  ����
                        //      0   û��װ��
                        //      1   �Ѿ�װ��
                        nRet = OrderControl.GetOrderRecPaths(
                            stop,
                            this.Channel,
                            strBiblioRecPath,
                            out recpaths,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else if (strDbType == "issue")
                    {
                        // return:
                        //      -1  ����
                        //      0   û��װ��
                        //      1   �Ѿ�װ��
                        nRet = IssueControl.GetIssueRecPaths(
                            stop,
                            this.Channel,
                            strBiblioRecPath,
                            out recpaths,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else if (strDbType == "comment")
                    {
                        // return:
                        //      -1  ����
                        //      0   û��װ��
                        //      1   �Ѿ�װ��
                        nRet = CommentControl.GetCommentRecPaths(
                            stop,
                            this.Channel,
                            strBiblioRecPath,
                            out recpaths,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }

                    if (recpaths.Count == 0)
                    {
                        items.Add(item);
                        nCount++;
                    }
                    stop.SetProgressValue(++i);
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
                this.listView_records.Enabled = true;
            }

            this.MainForm.StatusBarMessage = "ͳ�Ƴ��¼�" + strDbTypeName + "��¼Ϊ�յ���Ŀ��¼ " + nCount.ToString() + "��";
            return 1;
        ERROR1:
            return -1;
        }

        // �Ӽ�¼·���ļ��е���
        void menu_importFromRecPathFile_Click(object sender, EventArgs e)
        {
            int nRet = 0;
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
            // bool bSkipBrowse = false;

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

            string strLibraryUrl = StringUtil.CanonicalizeHostUrl(this.MainForm.LibraryServerUrl);

            // ��Ҫˢ�µ���
            List<ListViewItem> items = new List<ListViewItem>();

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ����¼·�� ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);

            try
            {
                // �����������û����ģ������Ҫ������е������־
                ListViewUtil.ClearSortColumns(this.listView_records);
                stop.SetProgressRange(0, sr.BaseStream.Length);

                if (this.listView_records.Items.Count > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "����ǰ�Ƿ�Ҫ������м�¼�б��е����е� " + this.listView_records.Items.Count.ToString() + " ��?\r\n\r\n(�������������µ�����н�׷���������к���)\r\n\r\n(Yes �����No �����(׷��)��Cancel ��������)",
                        "BiblioSearchForm",
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


                    string strLine = sr.ReadLine();

                    stop.SetProgressValue(sr.BaseStream.Position);

                    if (strLine == null)
                        break;

                    string strRecPath = "";
                    bool bOtherCols = false;
                    nRet = strLine.IndexOf("\t");
                    if (nRet == -1)
                        strRecPath = strLine;
                    else
                    {
                        strRecPath = strLine.Substring(0, nRet);
                        bOtherCols = true;
                    }

                    // ���ݳ�·��
                    if (strRecPath.IndexOf("@") != -1)
                    {
                        string strPureRecPath = "";
                        string strUrl = "";
                        ParseLongRecPath(strRecPath,
                            out strUrl,
                            out strPureRecPath);
                        string strUrl0 = StringUtil.CanonicalizeHostUrl(strUrl);

                        if (string.Compare(strUrl0, strLibraryUrl, true) == 0)
                            strRecPath = strPureRecPath;
                        else
                        {
                            strError = "��·�� '"+strRecPath+"' �еķ����� URL ���� '"+strUrl+"' �͵�ǰ dp2Circulation ������ URL '"+this.MainForm.LibraryServerUrl+"' ��ƥ�䣬����޷����������¼·���ļ�";
                            goto ERROR1;
                        }
                    }


                    // ���·������ȷ�ԣ�������ݿ��Ƿ�Ϊ��Ŀ��֮һ
                    // �ж�������Ŀ��¼·��������ʵ���¼·����
                    string strDbName = Global.GetDbName(strRecPath);
                    if (string.IsNullOrEmpty(strDbName) == true)
                    {
                        strError = "'" + strRecPath + "' ���ǺϷ��ļ�¼·��";
                        goto ERROR1;
                    }

                    if (this.MainForm.IsBiblioDbName(strDbName) == false)
                    {
                        strError = "·�� '" + strRecPath + "' �е����ݿ��� '" + strDbName + "' ���ǺϷ�����Ŀ�������ܿ�����ָ�����ļ�������Ŀ��ļ�¼·���ļ�";
                        goto ERROR1;
                    }

                    ListViewItem item = null;

                    if (bOtherCols == true)
                    {
                        item = Global.BuildListViewItem(
                    this.listView_records,
                    strLine);
                        this.listView_records.Items.Add(item);
                    }
                    else
                    {
                        item = new ListViewItem();
                        item.Text = strRecPath;

                        this.listView_records.Items.Add(item);

                        items.Add(item);
                    }

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

                if (sr != null)
                    sr.Close();
            }

            if (items.Count > 0)
            {
                nRet = RefreshListViewLines(items,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        static void ParseLongRecPath(string strRecPath,
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
#if NO
        // �Ӽ�¼·���ļ��е���
        void menu_importFromRecPathFile_Click(object sender, EventArgs e)
        {
            int nRet = 0;
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
            bool bSkipBrowse = false;

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

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ����¼·�� ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);

            try
            {
                // �����������û����ģ������Ҫ������е������־
                ListViewUtil.ClearSortColumns(this.listView_records);
                stop.SetProgressRange(0, sr.BaseStream.Length);

                if (this.listView_records.Items.Count > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "����ǰ�Ƿ�Ҫ������м�¼�б��е����е� " + this.listView_records.Items.Count.ToString() + " ��?\r\n\r\n(�������������µ�����н�׷���������к���)\r\n\r\n(Yes �����No �����(׷��)��Cancel ��������)",
                        "BiblioSearchForm",
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


                    string strLine = sr.ReadLine();

                    stop.SetProgressValue(sr.BaseStream.Position);

                    if (strLine == null)
                        break;

                    string strRecPath = "";
                    bool bOtherCols = false;
                    nRet = strLine.IndexOf("\t");
                    if (nRet == -1)
                        strRecPath = strLine;
                    else
                    {
                        strRecPath = strLine.Substring(0, nRet);
                        bOtherCols = true;
                    }

                    // ���·������ȷ�ԣ�������ݿ��Ƿ�Ϊ��Ŀ��֮һ
                    // �ж�������Ŀ��¼·��������ʵ���¼·����
                    string strDbName = Global.GetDbName(strRecPath);
                    if (string.IsNullOrEmpty(strDbName) == true)
                    {
                        strError = "'" + strRecPath + "' ���ǺϷ��ļ�¼·��";
                        goto ERROR1;
                    }

                    if (this.MainForm.IsBiblioDbName(strDbName) == false)
                    {
                        strError = "·�� '"+strRecPath+"' �е����ݿ��� '" + strDbName + "' ���ǺϷ�����Ŀ�������ܿ�����ָ�����ļ�������Ŀ��ļ�¼·���ļ�";
                        goto ERROR1;
                    }

                    ListViewItem item = null;

                    if (bOtherCols == true)
                    {
                        item = Global.BuildListViewItem(
                    this.listView_records,
                    strLine);
                        this.listView_records.Items.Add(item);
                    }
                    else
                    {
                        item = new ListViewItem();
                        item.Text = strRecPath;

                        this.listView_records.Items.Add(item);

                        if (bSkipBrowse == false
                            && !(Control.ModifierKeys == Keys.Shift))
                        {
                            nRet = RefreshOneBrowseLine(item,
                    out strError);
                            if (nRet == -1)
                            {
                                DialogResult result = MessageBox.Show(this,
            "����������ʱ����: " + strError + "��\r\n\r\n�Ƿ������ȡ�������? (Yes ��ȡ��No ����ȡ��Cancel ��������)",
            "BiblioSearchForm",
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
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControlsInSearching(true);

                if (sr != null)
                    sr.Close();
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif


        // ����ǰ����¼·�����Ѿ���ֵ
        /// <summary>
        /// ˢ��һ������С�����ǰ����¼·�����Ѿ���ֵ
        /// </summary>
        /// <param name="item">����� ListViewItem ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int RefreshBrowseLine(ListViewItem item,
            out string strError)
        {
            strError = "";

            string strRecPath = ListViewUtil.GetItemText(item, 0);
            string[] paths = new string[1];
            paths[0] = strRecPath;
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

        void menu_reverseSelectAllLines_Click(object sender, EventArgs e)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                foreach (ListViewItem item in this.ListViewRecords.Items)
                {
                    if (item.Selected == true)
                        item.Selected = false;
                    else
                        item.Selected = true;
                }
            }
            finally
            {
                this.Cursor = oldCursor;
            }
        }

        void menu_selectAllLines_Click(object sender, EventArgs e)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                ListViewUtil.SelectAllLines(this.listView_records);
            }
            finally
            {
                this.Cursor = oldCursor;
            }
        }

        void menu_copyToClipboard_Click(object sender, EventArgs e)
        {
            Global.CopyLinesToClipboard(this, this.listView_records, false);
        }

        void menu_copySingleColumnToClipboard_Click(object sender, EventArgs e)
        {
            int nColumn = (int)((MenuItem)sender).Tag;

            Global.CopyLinesToClipboard(this, nColumn, this.listView_records, false);
        }

        void menu_cutToClipboard_Click(object sender, EventArgs e)
        {
            this.listView_records.SelectedIndexChanged -= new System.EventHandler(this.listView_records_SelectedIndexChanged);

            Global.CopyLinesToClipboard(this, this.listView_records, true);

            this.listView_records.SelectedIndexChanged += new System.EventHandler(this.listView_records_SelectedIndexChanged);
            listView_records_SelectedIndexChanged(null, null);
        }

        void menu_pasteFromClipboard_insertBefore_Click(object sender, EventArgs e)
        {
            this.listView_records.SelectedIndexChanged -= new System.EventHandler(this.listView_records_SelectedIndexChanged);

            Global.PasteLinesFromClipboard(this, this.listView_records, true);

            this.listView_records.SelectedIndexChanged += new System.EventHandler(this.listView_records_SelectedIndexChanged);
            listView_records_SelectedIndexChanged(null, null);

        }

        void menu_pasteFromClipboard_insertAfter_Click(object sender, EventArgs e)
        {
            this.listView_records.SelectedIndexChanged -= new System.EventHandler(this.listView_records_SelectedIndexChanged);

            Global.PasteLinesFromClipboard(this, this.listView_records, false);

            this.listView_records.SelectedIndexChanged += new System.EventHandler(this.listView_records_SelectedIndexChanged);
            listView_records_SelectedIndexChanged(null, null);
        }

        void menu_refreshControls_Click(object sender, EventArgs e)
        {
            Global.InvalidateAllControls(this);
        }

        // ɾ����ѡ�����Ŀ��¼
        void menu_deleteSelectedRecords_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(this,
    "ȷʵҪ�����ݿ���ɾ����ѡ���� " + this.listView_records.SelectedItems.Count.ToString() + " ����Ŀ��¼?\r\n\r\n(���棺��Ŀ��¼��ɾ�����޷��ָ������ɾ����Ŀ��¼�����������Ĳᡢ�ڡ���������ע��¼�Ͷ�����Դ��һ��ɾ��)\r\n\r\n(OK ɾ����Cancel ȡ��)",
    "BiblioSearchForm",
    MessageBoxButtons.OKCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
            if (result == System.Windows.Forms.DialogResult.Cancel)
                return;

            List<ListViewItem> items = new List<ListViewItem>();
            foreach(ListViewItem item in this.listView_records.SelectedItems)
            {
                items.Add(item);
            }

            string strError = "";
            int nDeleteCount = 0;

            // ���ǰ��Ȩ��
            bool bDeleteSub = StringUtil.IsInList("client_deletebibliosubrecords", this.Channel.Rights);

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ɾ����Ŀ��¼ ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);
            this.listView_records.Enabled = false;
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

                    string[] results = null;
                    byte[] baTimestamp = null;
                    string strOutputPath = "";
                    string[] formats = null;
                    if (bDeleteSub == false && this.MainForm.Version >= 2.30)
                    {
                        formats = new string[1];
                        formats[0] = "subcount";
                    }

                    stop.SetMessage("����ɾ����Ŀ��¼ " + strRecPath);

                    long lRet = Channel.GetBiblioInfos(
                        stop,
                        strRecPath,
                        "",
                        formats,   // formats
                        out results,
                        out baTimestamp,
                        out strError);
                    if (lRet == 0)
                        goto ERROR1;
                    if (lRet == -1)
                    {
                        result = MessageBox.Show(this,
    "�ڻ�ü�¼ '" + strRecPath + "' ��ʱ����Ĺ����г��ִ���: "+strError+"��\r\n\r\n�Ƿ����ǿ��ɾ���˼�¼? (Yes ǿ��ɾ����No ��ɾ����Cancel ������ǰδ��ɵ�ȫ��ɾ������)",
    "BiblioSearchForm",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.Cancel)
                            goto ERROR1;
                        if (result == System.Windows.Forms.DialogResult.No)
                            continue;
                    }

                    if (bDeleteSub == false)
                    {
                        string strSubCount = "";
                        if (results != null && results.Length > 0)
                            strSubCount = results[0];

                        if (string.IsNullOrEmpty(strSubCount) == true || strSubCount == "0")
                        {
                        }
                        else
                        {
                            result = MessageBox.Show(this,
"��Ŀ��¼ '" + strRecPath + "' ���� " + strSubCount + " ���¼���¼������ǰ�û������߱� client_deletebibliosubrecords Ȩ�ޣ��޷�ɾ��������Ŀ��¼��\r\n\r\n�Ƿ��������Ĳ���? \r\n\r\n(Yes ������No ��ֹδ��ɵ�ȫ��ɾ������)",
"BiblioSearchForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                            if (result == System.Windows.Forms.DialogResult.No)
                            {
                                strError = "�жϲ���";
                                goto ERROR1;
                            }
                            continue;
                        }
                    }

                    byte[] baNewTimestamp = null;

                    lRet = Channel.SetBiblioInfo(
                        stop,
                        "delete",
                        strRecPath,
                        "xml",
                        "", // strXml,
                        baTimestamp,
                        "",
                        out strOutputPath,
                        out baNewTimestamp,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    nDeleteCount++;

                    stop.SetProgressValue(i);

                    this.listView_records.Items.Remove(item);
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
                this.listView_records.Enabled = true;
            }

            MessageBox.Show(this, "�ɹ�ɾ����Ŀ��¼ " + nDeleteCount + " ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �Ӵ�����������ѡ�������
        void menu_removeSelectedItems_Click(object sender, EventArgs e)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            for (int i = this.listView_records.SelectedIndices.Count - 1; i >= 0; i--)
            {
                this.listView_records.Items.RemoveAt(this.listView_records.SelectedIndices[i]);
            }

            this.Cursor = oldCursor;
        }

        /*
        void menu_selectAll_Click(object sender, EventArgs e)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            for (int i = 0; i < this.listView_records.Items.Count; i++)
            {
                this.listView_records.Items[i].Selected = true;
            }

            this.Cursor = oldCursor;
        }*/

        // ��ǰȱʡ�ı��뷽ʽ
        Encoding CurrentEncoding = Encoding.UTF8;

        // Ϊ�˱���ISO2709�ļ�����ļ�������

        /// <summary>
        /// ���������� ISO2709 �ļ�ȫ·��
        /// </summary>
        public string LastIso2709FileName
        {
            get
            {
                return this.MainForm.AppInfo.GetString(
                    "bibliosearchform",
                    "last_iso2709_filename",
                    "");
            }
            set
            {
                this.MainForm.AppInfo.SetString(
                    "bibliosearchform",
                    "last_iso2709_filename",
                    value);
            }
        }

        /// <summary>
        /// ������� ISO2709 �ļ�ʱ�Ƿ���� CRLF
        /// </summary>
        public bool LastCrLfIso2709
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "bibliosearchform",
                    "last_iso2709_crlf",
                    false);
            }
            set
            {
                this.MainForm.AppInfo.SetBoolean(
                    "bibliosearchform",
                    "last_iso2709_crlf",
                    value);
            }
        }

        /// <summary>
        /// ������� ISO2709 �ļ�ʱ�Ƿ�ɾ�� 998 �ֶ�
        /// </summary>
        public bool LastRemoveField998
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "bibliosearchform",
                    "last_iso2709_removefield998",
                    false);
            }
            set
            {
                this.MainForm.AppInfo.SetBoolean(
                    "bibliosearchform",
                    "last_iso2709_removefield998",
                    value);
            }
        }

        /// <summary>
        /// ������� ISO2709 �ļ�ʱ�ù��ı��뷽ʽ����
        /// </summary>
        public string LastEncodingName
        {
            get
            {
                return this.MainForm.AppInfo.GetString(
                    "bibliosearchform",
                    "last_encoding_name",
                    "");
            }
            set
            {
                this.MainForm.AppInfo.SetString(
                    "bibliosearchform",
                    "last_encoding_name",
                    value);
            }
        }

        /// <summary>
        /// ������� ISO2709 �ļ�ʱ�ù��ı�Ŀ����
        /// </summary>
        public string LastCatalogingRule
        {
            get
            {
                return this.MainForm.AppInfo.GetString(
                    "bibliosearchform",
                    "last_cataloging_rule",
                    "<������>");
            }
            set
            {
                this.MainForm.AppInfo.SetString(
                    "bibliosearchform",
                    "last_cataloging_rule",
                    value);
            }
        }

        void menu_saveToXmlFile_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ����������";
                goto ERROR1;
            }

            Encoding preferredEncoding = this.CurrentEncoding;

            {
                // �۲�Ҫ����ĵ�һ����¼��marc syntax
            }

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ������ XML �ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = true;
            dlg.FileName = "";
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "XML �ļ� (*.xml)|*.xml|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

#if NO
            if (bExist == true)
            {
                DialogResult result = MessageBox.Show(this,
        "�ļ� '" + dlg.FileName + "' �Ѵ��ڣ��Ƿ���׷�ӷ�ʽд���¼?\r\n\r\n--------------------\r\nע��(��)׷��  (��)����  (ȡ��)����",
        "BiblioSearchForm",
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

#endif


            this.EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ����� XML �ļ� ...");
            stop.BeginLoop();

            XmlTextWriter writer = null;

            try
            {
                writer = new XmlTextWriter(dlg.FileName, Encoding.UTF8);

            }
            catch (Exception ex)
            {
                strError = "�����ļ� " + dlg.FileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }

            try
            {
                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                writer.Formatting = Formatting.Indented;
                writer.Indentation = 4;

                writer.WriteStartDocument();
                writer.WriteStartElement("dprms", "collection", DpNs.dprms);

                writer.WriteAttributeString("xmlns", "dprms", null, DpNs.dprms);
#if NO
                writer.WriteAttributeString("xmlns", "unimarc", null, DigitalPlatform.Xml.Ns.unimarcxml);
                writer.WriteAttributeString("xmlns", "marc21", null, DigitalPlatform.Xml.Ns.usmarcxml);
#endif

                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    string strRecPath = item.Text;

                    if (string.IsNullOrEmpty(strRecPath) == true)
                        continue;

                    string[] results = null;
                    byte[] baTimestamp = null;

                    stop.SetMessage("���ڻ�ȡ��Ŀ��¼ " + strRecPath);

                    long lRet = Channel.GetBiblioInfos(
                        stop,
                        strRecPath,
                        "",
                        new string[] { "xml" },   // formats
                        out results,
                        out baTimestamp,
                        out strError);
                    if (lRet == 0)
                        goto ERROR1;
                    if (lRet == -1)
                        goto ERROR1;

                    if (results == null || results.Length == 0)
                    {
                        strError = "results error";
                        goto ERROR1;
                    }

                    string strXml = results[0];

                    if (string.IsNullOrEmpty(strXml) == false)
                    {
                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(strXml);
                        }
                        catch (Exception ex)
                        {
                            strError = "XML װ�� DOM ʱ����: " + ex.Message;
                            goto ERROR1;
                        }

                        if (dom.DocumentElement != null)
                        {
                            // ����Ԫ�����ü�������
                            DomUtil.SetAttr(dom.DocumentElement, "path", DpNs.dprms, this.MainForm.LibraryServerUrl + "?" + strRecPath);
                            DomUtil.SetAttr(dom.DocumentElement, "timestamp", DpNs.dprms, ByteArray.GetHexTimeStampString(baTimestamp));

                            dom.DocumentElement.WriteTo(writer);
                        }
                    }

                    stop.SetProgressValue(++i);
                }

                writer.WriteEndElement();   // </collection>
                writer.WriteEndDocument();
            }
            catch (Exception ex)
            {
                strError = "д���ļ� " + dlg.FileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }
            finally
            {
                writer.Close();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControls(true);
            }

            MainForm.StatusBarMessage = this.listView_records.SelectedItems.Count.ToString()
                + "����¼�ɹ����浽�ļ� " + dlg.FileName;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        // 2012/2/14
        // ���浽 MARC �ļ�
        void menu_saveToMarcFile_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ����������";
                goto ERROR1;
            }

            Encoding preferredEncoding = this.CurrentEncoding;

            {
                // �۲�Ҫ����ĵ�һ����¼��marc syntax
            }

            OpenMarcFileDlg dlg = new OpenMarcFileDlg();
            MainForm.SetControlFont(dlg, this.Font);
            dlg.IsOutput = true;
            dlg.AddG01Visible = false;
            dlg.RuleVisible = true;
            dlg.Rule = this.LastCatalogingRule;
            dlg.FileName = this.LastIso2709FileName;
            dlg.CrLf = this.LastCrLfIso2709;
            dlg.RemoveField998 = this.LastRemoveField998;
            dlg.EncodingListItems = Global.GetEncodingList(false);
            dlg.EncodingName =
                (String.IsNullOrEmpty(this.LastEncodingName) == true ? Global.GetEncodingName(preferredEncoding) : this.LastEncodingName);
            dlg.EncodingComment = "ע: ԭʼ���뷽ʽΪ " + Global.GetEncodingName(preferredEncoding);
            dlg.MarcSyntax = "<�Զ�>";    // strPreferedMarcSyntax;
            dlg.EnableMarcSyntax = false;
            dlg.ShowDialog(this);
            if (dlg.DialogResult != DialogResult.OK)
                return;

            string strCatalogingRule = dlg.Rule;
            if (strCatalogingRule == "<������>")
                strCatalogingRule = null;

            Encoding targetEncoding = null;

            nRet = Global.GetEncoding(dlg.EncodingName,
                out targetEncoding,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }

            string strLastFileName = this.LastIso2709FileName;
            string strLastEncodingName = this.LastEncodingName;

            bool bExist = File.Exists(dlg.FileName);
            bool bAppend = false;

            if (bExist == true)
            {
                DialogResult result = MessageBox.Show(this,
        "�ļ� '" + dlg.FileName + "' �Ѵ��ڣ��Ƿ���׷�ӷ�ʽд���¼?\r\n\r\n--------------------\r\nע��(��)׷��  (��)����  (ȡ��)����",
        "BiblioSearchForm",
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
                        "BiblioSearchForm",
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

            this.LastIso2709FileName = dlg.FileName;
            this.LastCrLfIso2709 = dlg.CrLf;
            this.LastEncodingName = dlg.EncodingName;
            this.LastCatalogingRule = dlg.Rule;
            this.LastRemoveField998 = dlg.RemoveField998;

            this.EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ����� MARC �ļ� ...");
            stop.BeginLoop();

            Stream s = null;

            try
            {
                s = File.Open(this.LastIso2709FileName,
                     FileMode.OpenOrCreate);
                if (bAppend == false)
                    s.SetLength(0);
                else
                    s.Seek(0, SeekOrigin.End);
            }
            catch (Exception ex)
            {
                strError = "�򿪻򴴽��ļ� " + this.LastIso2709FileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }

            try
            {
                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
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

                    string strRecPath = item.Text;

                    if (string.IsNullOrEmpty(strRecPath) == true)
                        continue;

                    string[] results = null;
                    byte[] baTimestamp = null;

                    stop.SetMessage("���ڻ�ȡ��Ŀ��¼ " + strRecPath);

                    long lRet = Channel.GetBiblioInfos(
                        stop,
                        strRecPath,
                        "",
                        new string[] { "xml" },   // formats
                        out results,
                        out baTimestamp,
                        out strError);
                    if (lRet == 0)
                        goto ERROR1;
                    if (lRet == -1)
                        goto ERROR1;

                    if (results == null || results.Length == 0)
                    {
                        strError = "results error";
                        goto ERROR1;
                    }

                    string strXml = results[0];

                    string strMARC = "";
                    string strMarcSyntax = "";
                    // ��XML��ʽת��ΪMARC��ʽ
                    // �Զ������ݼ�¼�л��MARC�﷨
                    nRet = MarcUtil.Xml2Marc(strXml,
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

                    byte[] baTarget = null;

                    Debug.Assert(strMarcSyntax != "", "");

                    // ���ձ�Ŀ�������
                    // ���һ���ض����� MARC ��¼
                    // parameters:
                    //      strStyle    Ҫƥ���styleֵ�����Ϊnull����ʾ�κ�$*ֵ��ƥ�䣬ʵ����Ч����ȥ��$*������ȫ���ֶ�����
                    // return:
                    //      0   û��ʵ�����޸�
                    //      1   ��ʵ�����޸�
                    nRet = MarcUtil.GetMappedRecord(ref strMARC,
                        strCatalogingRule);

                    if (dlg.RemoveField998 == true)
                    {
                        MarcRecord record = new MarcRecord(strMARC);
                        record.select("field[@name='998']").detach();
                        strMARC = record.Text;
                    }
                    if (dlg.Mode880 == true && strMarcSyntax == "usmarc")
                    {
                        MarcRecord record = new MarcRecord(strMARC);
                        MarcQuery.To880(record);
                        strMARC = record.Text;
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
                        strMARC,
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

                    stop.SetProgressValue(++i);
                }
            }
            catch (Exception ex)
            {
                strError = "д���ļ� " + this.LastIso2709FileName + " ʧ�ܣ�ԭ��: " + ex.Message;
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

            // 
            if (bAppend == true)
                MainForm.StatusBarMessage = this.listView_records.SelectedItems.Count.ToString()
                    + "����¼�ɹ�׷�ӵ��ļ� " + this.LastIso2709FileName + " β��";
            else
                MainForm.StatusBarMessage = this.listView_records.SelectedItems.Count.ToString()
                    + "����¼�ɹ����浽���ļ� " + this.LastIso2709FileName;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ���浽��¼·���ļ�
        void menu_saveToRecordPathFile_Click(object sender, EventArgs e)
        {
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
                    "BiblioSearchForm",
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

                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    sw.WriteLine(item.Text);
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

            this.MainForm.StatusBarMessage = "��Ŀ��¼·�� " + this.listView_records.SelectedItems.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportRecPathFilename;
        }

        private void listView_records_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            /*
            int nClickColumn = e.Column;

            ColumnSortStyle sortStyle = ColumnSortStyle.LeftAlign;

            // ��һ��Ϊ��¼·��������������
            if (nClickColumn == 0)
                sortStyle = ColumnSortStyle.RecPath;

            this.SortColumns.SetFirstColumn(nClickColumn,
                sortStyle,
                this.listView_records.Columns);

            // ����
            this.listView_records.ListViewItemSorter = new SortColumnsComparer(this.SortColumns);

            this.listView_records.ListViewItemSorter = null;
            */
            ListViewUtil.OnColumnClick(this.listView_records, e);
        }

        public override void OnSelectedIndexChanged()
        {
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

            ListViewUtil.OnSeletedIndexChanged(this.listView_records,
                0,
                null);

            if (this.m_biblioTable != null)
            {
                // if (CanCallNew(commander, WM_SELECT_INDEX_CHANGED) == true)
                    RefreshPropertyView(false);
            }
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_SELECT_INDEX_CHANGED:
                    {
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

                        ListViewUtil.OnSeletedIndexChanged(this.listView_records,
                            0,
                            null);

                        if (this.m_biblioTable != null)
                        {
                            if (CanCallNew(commander, m.Msg) == true)
                                RefreshPropertyView(false);
                        }
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }

        /*public*/ bool CanCallNew(Commander commander, int msg)
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


        private void listView_records_SelectedIndexChanged(object sender, EventArgs e)
        {
#if NO
            API.MSG msg = new API.MSG();
            bool bRet = API.PeekMessage(ref msg,
                this.Handle,
                (uint)WM_SELECT_INDEX_CHANGED,
                (uint)WM_SELECT_INDEX_CHANGED,
                0);
            if (bRet == false)
                API.PostMessage(this.Handle, WM_SELECT_INDEX_CHANGED, 0, 0);


            /*
            // �����ǰ�ۻ�����Ϣ
            while (API.PeekMessage(ref msg,
                this.Handle,
                (uint)WM_SELECT_INDEX_CHANGED,
                (uint)WM_SELECT_INDEX_CHANGED,
                API.PM_REMOVE)) ;
            API.PostMessage(this.Handle, WM_SELECT_INDEX_CHANGED, 0, 0);
            */
#endif

            // this.commander.AddMessage(WM_SELECT_INDEX_CHANGED);
            this.TriggerSelectedIndexChanged();
        }



        private void textBox_queryWord_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_search;
        }

        private void textBox_queryWord_Leave(object sender, EventArgs e)
        {
            this.AcceptButton = null;
        }

        private void textBox_queryWord_TextChanged(object sender, EventArgs e)
        {
            this.Text = "��Ŀ��ѯ " + this.textBox_queryWord.Text;
        }

        private void listView_records_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                string strTotal = "";
                if (this.listView_records.SelectedIndices.Count > 0)
                {
                    for (int i = 0; i < this.listView_records.SelectedIndices.Count; i++)
                    {
                        int index = this.listView_records.SelectedIndices[i];

                        ListViewItem item = this.listView_records.Items[index];
                        string strLine = Global.BuildLine(item);
                        strTotal += strLine + "\r\n";
                    }
                }
                else
                {
                    strTotal = Global.BuildLine((ListViewItem)e.Item);
                }

                this.listView_records.DoDragDrop(
                    strTotal,
                    DragDropEffects.Link);
            }
        }

        private void comboBox_matchStyle_TextChanged(object sender, EventArgs e)
        {
            if (this.comboBox_matchStyle.Text == "��ֵ")
            {
                this.textBox_queryWord.Text = "";
                this.textBox_queryWord.Enabled = false;
            }
            else
            {
                this.textBox_queryWord.Enabled = true;
            }
        }

        // ���ڸ�������֮��Ĳ����ͻ
        private void checkedComboBox_biblioDbNames_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            ListView list = e.Item.ListView;

            if (e.Item.Text == "<ȫ��>" || e.Item.Text.ToLower() == "<all>")
            {
                if (e.Item.Checked == true)
                {
                    // �����ǰ��ѡ�ˡ�ȫ���������������ȫ������Ĺ�ѡ
                    for (int i = 0; i < list.Items.Count; i++)
                    {
                        ListViewItem item = list.Items[i];
                        if (item.Text == "<ȫ��>" || item.Text.ToLower() == "<all>")
                            continue;
                        if (item.Checked != false)
                            item.Checked = false;
                    }
                }
            }
            else
            {
                if (e.Item.Checked == true)
                {
                    // �����ѡ�Ĳ��ǡ�ȫ��������Ҫ�����ȫ�����Ͽ��ܵĹ�ѡ
                    for (int i = 0; i < list.Items.Count; i++)
                    {
                        ListViewItem item = list.Items[i];
                        if (item.Text == "<ȫ��>" || item.Text.ToLower() == "<all>")
                        {
                            if (item.Checked != false)
                                item.Checked = false;
                        }
                    }
                }
            }
        }

        // �������ͼ��
        private void comboBox_from_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_from.Invalidate();
        }

        // �������ͼ��
        private void comboBox_matchStyle_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_matchStyle.Invalidate();
        }

        private void ToolStripMenuItem_searchKeyID_Click(object sender, EventArgs e)
        {
        }



        // ��ͨ����(������key����)
        private void toolStripButton_search_Click(object sender, EventArgs e)
        {
            DoSearch(false, false);
        }

#if NO
        // ��������м���װ��û��װ��Ĳ���
        private void MenuItem_continueLoad_Click(object sender, EventArgs e)
        {

        }
#endif

        // ����װ��
        private void ToolStripMenuItem_continueLoad_Click(object sender, EventArgs e)
        {
            string strError = "";

            long lHitCount = this.m_lHitCount;
            if (this.listView_records.Items.Count >= lHitCount)
            {
                strError = "�����Ϣ�Ѿ�ȫ��װ������ˣ�û�б�Ҫ����װ��";
                goto ERROR1;
            }


            bool bQuickLoad = false;
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                bQuickLoad = true;

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            this.EnableControlsInSearching(false);
            try
            {
                stop.SetProgressRange(0, lHitCount);

                long lStart = this.listView_records.Items.Count;
                long lPerCount = Math.Min(50, lHitCount);
                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                bool bPushFillingBrowse = this.PushFillingBrowse;

                this.label_message.Text = "���ڼ���װ�������Ϣ...";

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            // MessageBox.Show(this, "�û��ж�");
                            this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼����װ�� " + lStart.ToString() + " �����û��ж�...";
                            return;
                        }
                    }

                    stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    /*
                    string strStyle = "id,cols";
                    if (this.m_bFirstColumnIsKey == true)
                        strStyle = "keyid,id,key,cols";
                     * */

                    bool bTempQuickLoad = bQuickLoad;

                    if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                        bTempQuickLoad = true;

                    string strStyle = "id,cols";
                    if (bTempQuickLoad == true)
                    {
                        if (this.m_bFirstColumnIsKey == true)
                            strStyle = "keyid,id,key";    // û����cols����
                        else
                            strStyle = "id";
                    }
                    else
                    {
                        // 
                        if (this.m_bFirstColumnIsKey == true)
                            strStyle = "keyid,id,key,cols";
                        else
                            strStyle = "id,cols";
                    }

                    long lRet = Channel.GetSearchResult(
                        stop,
                        null,   // strResultSetName
                        lStart,
                        lPerCount,
                        strStyle,
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                    {
                        this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼����װ�� " + lStart.ToString() + " ����" + strError;
                        goto ERROR1;
                    }

                    if (lRet == 0)
                    {
                        MessageBox.Show(this, "δ����");
                        return;
                    }

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        DigitalPlatform.CirculationClient.localhost.Record searchresult = searchresults[i];

                        string[] cols = null;
                        if (this.m_bFirstColumnIsKey == true)
                        {
                            // ���keys
                            if (searchresult.Cols == null
                                && bTempQuickLoad == false)
                            {
                                strError = "Ҫʹ�û�ȡ�����㹦�ܣ��뽫 dp2Library Ӧ�÷������� dp2Kernel ���ݿ��ں����������°汾";
                                goto ERROR1;
                            }
                            cols = new string[(searchresult.Cols == null ? 0 : searchresult.Cols.Length) + 1];
                            cols[0] = LibraryChannel.BuildDisplayKeyString(searchresult.Keys);
                            if (cols.Length > 1)
                                Array.Copy(searchresult.Cols, 0, cols, 1, cols.Length - 1);
                        }
                        else
                        {
                            cols = searchresult.Cols;
                        }

                        if (bPushFillingBrowse == true)
                        {
                            if (bTempQuickLoad == true)
                                Global.InsertNewLine(
                                    (ListView)this.listView_records,
                                    searchresult.Path,
                                    cols);
                            else
                                Global.InsertNewLine(
                                    this.listView_records,
                                    searchresult.Path,
                                    cols);
                        }
                        else
                        {
                            if (bTempQuickLoad == true)
                                Global.AppendNewLine(
                                    (ListView)this.listView_records,
                                    searchresult.Path,
                                    cols);
                            else
                                Global.AppendNewLine(
                                this.listView_records,
                                searchresult.Path,
                                cols);
                        }
                    }

                    lStart += searchresults.Length;
                    // lCount -= searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;

                    this.m_lLoaded = lStart;
                    stop.SetProgressValue(lStart);
                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
                this.label_message.Text = "���������� " + lHitCount.ToString() + " ����Ŀ��¼����ȫ��װ��";
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

        private void toolStripMenuItem_searchKeyID_Click(object sender, EventArgs e)
        {
            DoSearch(false, true);
        }

        // �ߴ�Ϊ0,0�İ�ť��Ϊ������this.AcceptButton
        private void button_search_Click(object sender, EventArgs e)
        {
            DoSearch(false, false);
        }

        private void dp2QueryControl1_GetList(object sender, DigitalPlatform.CommonControl.GetListEventArgs e)
        {
            // ����������ݿ���
            if (string.IsNullOrEmpty(e.Path) == true)
            {
                if (this.MainForm.BiblioDbProperties != null)
                {
                    for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                    {
                        BiblioDbProperty property = this.MainForm.BiblioDbProperties[i];
                        e.Values.Add(property.DbName);
                    }
                }
            }
            else
            {
                // ����ض����ݿ�ļ���;��
                // ÿ���ⶼһ��
                for (int i = 0; i < this.MainForm.BiblioDbFromInfos.Length; i++)
                {
                    BiblioDbFromInfo info = this.MainForm.BiblioDbFromInfos[i];
                    e.Values.Add(info.Caption);   // + "\t" + info.Style);
                }
            }
        }

        private void dp2QueryControl1_ViewXml(object sender, EventArgs e)
        {
            string strError = "";
            string strQueryXml = "";

            int nRet = dp2QueryControl1.BuildQueryXml(
this.MaxSearchResultCount,
"zh",
out strQueryXml,
out strError);
            if (nRet == -1)
            {
                strError = "�ڴ���XML����ʽ�Ĺ����г���: " + strError;
                goto ERROR1;
            }

            XmlViewerForm dlg = new XmlViewerForm();

            dlg.Text = "����ʽXML";
            dlg.MainForm = this.MainForm;
            dlg.XmlString = strQueryXml;
            // dlg.StartPosition = FormStartPosition.CenterScreen;

            this.MainForm.AppInfo.LinkFormState(dlg, "bibliosearchform_viewqueryxml");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void dp2QueryControl1_AppendMenu(object sender, AppendMenuEventArgs e)
        {
            MenuItem menuItem = null;

            menuItem = new MenuItem("����(&S)");
            menuItem.Click += new System.EventHandler(this.menu_logicSearch_Click);
            e.ContextMenu.MenuItems.Add(menuItem);

            // ����װ��
            menuItem = new MenuItem("����װ��(&C)");
            menuItem.Click += new System.EventHandler(this.ToolStripMenuItem_continueLoad_Click);
            if (this.m_lHitCount <= this.listView_records.Items.Count)
                menuItem.Enabled = false;
            else
                menuItem.Enabled = true;
            e.ContextMenu.MenuItems.Add(menuItem);

            // ��������ļ���
            menuItem = new MenuItem("��������ļ���(&K)");
            menuItem.Click += new System.EventHandler(this.menu_logicSearchKeyID_Click);
            e.ContextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            e.ContextMenu.MenuItems.Add(menuItem);
        }

        void menu_logicSearch_Click(object sender, EventArgs e)
        {
            this.DoLogicSearch(false);
        }

        void menu_logicSearchKeyID_Click(object sender, EventArgs e)
        {
            this.DoLogicSearch(true);
        }

        // 
        /// <summary>
        /// �������ĩβ�¼���һ��
        /// </summary>
        /// <param name="strLine">Ҫ����������ݡ�ÿ������֮�����ַ� '\t' ���</param>
        /// <returns>�´����� ListViewItem ����</returns>
        public ListViewItem AddLineToBrowseList(string strLine)
        {
            ListViewItem item = Global.BuildListViewItem(
    this.listView_records,
    strLine);

            this.listView_records.Items.Add(item);
            return item;
        }

        private void ToolStripMenuItem_rfc1123Single_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            dlg.RangeMode = false;
            try
            {
                dlg.Rfc1123String = this.textBox_queryWord.Text;
            }
            catch
            {
                this.textBox_queryWord.Text = "";
            }
            this.MainForm.AppInfo.LinkFormState(dlg, "searchbiblioform_gettimedialog_single");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_queryWord.Text = dlg.Rfc1123String;
        }

        private void ToolStripMenuItem_uSingle_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            dlg.RangeMode = false;
            try
            {
                dlg.uString = this.textBox_queryWord.Text;
            }
            catch
            {
                this.textBox_queryWord.Text = "";
            }

            this.MainForm.AppInfo.LinkFormState(dlg, "searchbiblioform_gettimedialog_single");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_queryWord.Text = dlg.uString;

        }

        private void ToolStripMenuItem_rfc1123Range_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            dlg.RangeMode = true;
            // �ָ�Ϊ�����ַ���
            try
            {
                dlg.Rfc1123String = this.textBox_queryWord.Text;
            }
            catch
            {
                this.textBox_queryWord.Text = "";
            }
            this.MainForm.AppInfo.LinkFormState(dlg, "searchbiblioform_gettimedialog_range");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_queryWord.Text = dlg.Rfc1123String;

        }

        private void ToolStripMenuItem_uRange_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            dlg.RangeMode = true;
            try
            {
                dlg.uString = this.textBox_queryWord.Text;
            }
            catch
            {
                this.textBox_queryWord.Text = "";
            }

            this.MainForm.AppInfo.LinkFormState(dlg, "searchbiblioform_gettimedialog_range");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_queryWord.Text = dlg.uString;

        }

        private void toolStripMenuItem_searchKeys_Click(object sender, EventArgs e)
        {
            DoSearch(true, false);
        }

        private void toolStripButton_prevQuery_Click(object sender, EventArgs e)
        {
            ItemQueryParam query = PrevQuery();
            if (query != null)
            {
                QueryToPanel(query);
            }
        }

        private void toolStripButton_nextQuery_Click(object sender, EventArgs e)
        {
            ItemQueryParam query = NextQuery();
            if (query != null)
            {
                QueryToPanel(query);
            }
        }

        private void dp2QueryControl1_GetFromStyle(object sender, GetFromStyleArgs e)
        {
            e.FromStyles = this.MainForm.GetBiblioFromStyle(e.FromCaption);
        }

        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        public int GetItemMarc(
            ListViewItem item,
            out string strMARC,
            out string strMarcSyntax,
            out string strError)
        {
            strError = "";
            strMARC = "";
            strMarcSyntax = "";

            BiblioInfo info = null;

            int nRet = GetBiblioInfo(
                true,
                item,
                out info,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;

            // ��XML��ʽת��ΪMARC��ʽ
            // �Զ������ݼ�¼�л��MARC�﷨
            nRet = MarcUtil.Xml2Marc(info.OldXml,    // info.OldXml,
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

            return 1;
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
            if (info == null)
            {
                info = new BiblioInfo();
                info.RecPath = strRecPath;
                this.m_biblioTable[strRecPath] = info;
            }

            if (string.IsNullOrEmpty(info.OldXml) == true)
            {
                if (bCheckSearching == true)
                {
                    if (this.InSearching == true)
                        return 0;
                }

                string[] results = null;
                byte[] baTimestamp = null;
                // �����Ŀ��¼
                long lRet = Channel.GetBiblioInfos(
                    stop,
                    strRecPath,
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
                info.OldXml = strXml;
                info.Timestamp = baTimestamp;
                info.RecPath = strRecPath;
            }

            return 1;
        }

        int m_nInViewing = 0;

        /// <summary>
        /// ��ʾ��ǰѡ���е�����
        /// </summary>
        /// <param name="bOpenWindow">�Ƿ�Ҫ�򿪸����Ի���</param>
        public void RefreshPropertyView(bool bOpenWindow)
        {
            m_nInViewing++;
            try
            {
                ListViewItem item = null;
                if (this.listView_records.SelectedItems.Count == 1)
                    item = this.listView_records.SelectedItems[0];

                _doViewProperty(item, bOpenWindow);
            }
            finally
            {
                m_nInViewing--;
            }
        }

        public void DisplayProperty(ListViewItem item, bool bOpenWindow)
        {
            m_nInViewing++;
            try
            {
                _doViewProperty(item, bOpenWindow);
            }
            finally
            {
                m_nInViewing--;
            }
        }

        void _doViewProperty(ListViewItem item, bool bOpenWindow)
        {
            string strError = "";
            string strHtml = "";
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
                || item == null)
            {
                if (this.m_commentViewer != null)
                    this.m_commentViewer.Clear();
                return;
            }

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
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            strHtml = "<html>" +
    GetHeadString() +
    "<body>" +
    strHtml2 +
    EntityForm.GetTimestampHtml(info.Timestamp) +
    "</body></html>";
            bool bNew = false;
            if (this.m_commentViewer == null
                || (bOpenWindow == true && this.m_commentViewer.Visible == false))
            {
                m_commentViewer = new CommentViewerForm();
                MainForm.SetControlFont(m_commentViewer, this.Font, false);
                bNew = true;
            }

            m_commentViewer.MainForm = this.MainForm;  // �����ǵ�һ��

            if (bNew == true)
                m_commentViewer.InitialWebBrowser();

            m_commentViewer.Text = "MARC���� '" + info.RecPath + "'";
            m_commentViewer.HtmlString = strHtml;
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

#if NO
        void _doViewProperty(bool bOpenWindow)
        {
            string strError = "";
            string strHtml = "";
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
                || this.listView_records.SelectedItems.Count != 1)
            {
                if (this.m_commentViewer != null)
                    this.m_commentViewer.Clear();
                return;
            }

            ListViewItem item = this.listView_records.SelectedItems[0];
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
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            strHtml = "<html>" +
    GetHeadString() +
    "<body>" +
    strHtml2 +
    EntityForm.GetTimestampHtml(info.Timestamp) +
    "</body></html>";
            bool bNew = false;
            if (this.m_commentViewer == null
                || (bOpenWindow == true && this.m_commentViewer.Visible == false))
            {
                m_commentViewer = new CommentViewerForm();
                MainForm.SetControlFont(m_commentViewer, this.Font, false);
                bNew = true;
            }

            m_commentViewer.MainForm = this.MainForm;  // �����ǵ�һ��

            if (bNew == true)
                m_commentViewer.InitialWebBrowser();

            m_commentViewer.Text = "MARC���� '" + info.RecPath + "'";
            m_commentViewer.HtmlString = strHtml;
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
#endif

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
            out string strHtml2,
            out string strError)
        {
            strError = "";
            strXml1 = "";
            strXml2 = "";
            strHtml2 = "";
            int nRet = 0;

            strXml1 = info.OldXml;
            strXml2 = info.NewXml;

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

            }


            if (string.IsNullOrEmpty(strOldMARC) == false
                && string.IsNullOrEmpty(strNewMARC) == false)
            {
                string strOldImageFragment = GetImageHtmlFragment(
    info.RecPath,
    strOldMARC);
                string strNewImageFragment = GetImageHtmlFragment(
info.RecPath,
strNewMARC);

                // ����չʾ���� MARC ��¼����� HTML �ַ���
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = MarcDiff.DiffHtml(
                    strOldMARC,
                    strOldFragmentXml,
                    strOldImageFragment,
                    strNewMARC,
                    strNewFragmentXml,
                    strNewImageFragment,
                    out strHtml2,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            else if (string.IsNullOrEmpty(strOldMARC) == false
    && string.IsNullOrEmpty(strNewMARC) == true)
            {
                string strImageFragment = GetImageHtmlFragment(
                    info.RecPath,
                    strOldMARC);
                strHtml2 = MarcUtil.GetHtmlOfMarc(strOldMARC,
                    strOldFragmentXml,
                    strImageFragment,
                    false);
            }
            else if (string.IsNullOrEmpty(strOldMARC) == true
                && string.IsNullOrEmpty(strNewMARC) == false)
            {
                string strImageFragment = GetImageHtmlFragment(
    info.RecPath,
    strNewMARC);
                strHtml2 = MarcUtil.GetHtmlOfMarc(strNewMARC,
                    strNewFragmentXml,
                    strImageFragment,
                    false);
            }
            return 0;
        }

        public static string GetImageHtmlFragment(
            string strBiblioRecPath,
            string strMARC)
        {
            string strImageUrl = ScriptUtil.GetCoverImageUrl(strMARC, "MediumImage");

            if (string.IsNullOrEmpty(strImageUrl) == true)
                return "";

            if (StringUtil.HasHead(strImageUrl, "http:") == true)
                return "<img src='" + strImageUrl + "'></img>";

            string strUri = ScriptUtil.MakeObjectUrl(strBiblioRecPath,
                  strImageUrl);
            return "<img class='pending' name='"
            + (strBiblioRecPath == "?" ? "?" : "object-path:" + strUri)
            + "' src='%mappeddir%\\images\\ajax-loader.gif' alt='����ͼƬ'></img>";
        }

        void marc_viewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_commentViewer != null)
            {
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
                    "<script type='text/javascript' charset='UTF-8' src='%datadir%\\getsummary.js" + "'></script>" +
                    "</head>";
            return
    "<head>" +
    "<LINK href='" + strCssFilePath + "' type='text/css' rel='stylesheet'>" +
    "</head>";
        }

        public string QueryWord
        {
            get
            {
                return this.textBox_queryWord.Text;
            }
            set
            {
                this.textBox_queryWord.Text = value;
            }
        }

        public string From
        {
            get
            {
                return this.comboBox_from.Text;
            }
            set
            {
                this.comboBox_from.Text = value;
            }
        }

        public string MatchStyle
        {
            get
            {
                return this.comboBox_matchStyle.Text;
            }
            set
            {
                this.comboBox_matchStyle.Text = value;
            }
        }
    }

    // Ϊһ�д洢����Ŀ��Ϣ
    /// <summary>
    /// ���ڴ��л���һ����Ŀ��Ϣ���ܹ���ʾ�¾ɼ�¼���޸Ĺ�ϵ
    /// </summary>
    public class BiblioInfo
    {
        /// <summary>
        /// ��¼·��
        /// </summary>
        public string RecPath = "";
        /// <summary>
        /// �ɵļ�¼ XML
        /// </summary>
        public string OldXml = "";
        /// <summary>
        /// �µļ�¼ XML
        /// </summary>
        public string NewXml = "";
        /// <summary>
        /// ʱ���
        /// </summary>
        public byte[] Timestamp = null;

        public BiblioInfo()
        {
        }

        public BiblioInfo(string strRecPath,
            string strOldXml,
            string strNewXml,
            byte[] timestamp)
        {
            this.RecPath = strRecPath;
            this.OldXml = strOldXml;
            this.NewXml = strNewXml;
            this.Timestamp = timestamp;
        }

        // ��������
        public BiblioInfo(BiblioInfo ref_obj)
        {
            this.RecPath = ref_obj.RecPath;
            this.OldXml = ref_obj.OldXml;
            this.NewXml = ref_obj.NewXml;
            this.Timestamp = ref_obj.Timestamp;
        }

    }

    /// <summary>
    /// һ�� loader ����
    /// </summary>
    public class LoaderItem
    {
        /// <summary>
        /// ��Ŀ��Ϣ
        /// </summary>
        public BiblioInfo BiblioInfo = null;
        /// <summary>
        /// ������ ListViewItem ����
        /// </summary>
        public ListViewItem ListViewItem = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="info">��Ŀ��Ϣ</param>
        /// <param name="item">������ ListViewItem ����</param>
        public LoaderItem(BiblioInfo info, ListViewItem item)
        {
            this.BiblioInfo = info;
            this.ListViewItem = item;
        }
    }

    /// <summary>
    /// ���� ListViewItem ��������Ŀ��¼��Ϣ��ö������
    /// �������û������
    /// </summary>
    public class ListViewBiblioLoader : IEnumerable
    {
        /// <summary>
        /// ��ʾ���¼�
        /// </summary>
        public event MessagePromptEventHandler Prompt = null;

        /// <summary>
        /// ListViewItem ����
        /// </summary>
        public List<ListViewItem> Items
        {
            get;
            set;
        }

        /// <summary>
        /// ���ڻ���� Hashtable ����
        /// </summary>
        public Hashtable CacheTable
        {
            get;
            set;
        }

        BiblioLoader m_loader = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="channel">ͨѶͨ��</param>
        /// <param name="stop">ֹͣ����</param>
        /// <param name="items">ListViewItem ����</param>
        /// <param name="cacheTable">���ڻ���� Hashtable ����</param>
        public ListViewBiblioLoader(LibraryChannel channel,
            Stop stop,
            List<ListViewItem> items,
            Hashtable cacheTable)
        {
            m_loader = new BiblioLoader();
            m_loader.Channel = channel;
            m_loader.Stop = stop;
            m_loader.Format = "xml";
            m_loader.GetBiblioInfoStyle = GetBiblioInfoStyle.Timestamp; // ������Ϣֻȡ�� timestamp
            m_loader.Prompt += new MessagePromptEventHandler(m_loader_Prompt);

            this.Items = items;
            this.CacheTable = cacheTable;
        }

        void m_loader_Prompt(object sender, MessagePromptEventArgs e)
        {
            if (this.Prompt != null)
                this.Prompt(sender, e);
        }

        /// <summary>
        /// ���ö�ٽӿ�
        /// </summary>
        /// <returns>ö�ٽӿ�</returns>
        public IEnumerator GetEnumerator()
        {
            Debug.Assert(m_loader != null, "");

            Hashtable dup_table = new Hashtable();  // ȷ�� recpaths �в�������ظ���·��

            List<string> recpaths = new List<string>(); // ������û�а�������Щ��¼
            foreach (ListViewItem item in this.Items)
            {
                string strRecPath = item.Text;
                Debug.Assert(string.IsNullOrEmpty(strRecPath) == false, "");

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