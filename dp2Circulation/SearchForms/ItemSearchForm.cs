using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.IO;
using DigitalPlatform.Marc;
using DigitalPlatform.CommonControl;

using DigitalPlatform.CirculationClient.localhost;
using System.Reflection;
using DigitalPlatform.Script;
using System.Web;
using System.Threading;
using DigitalPlatform.dp2.Statis;

// 2013/3/16 ��� XML ע��

namespace dp2Circulation
{
    /// <summary>
    /// ʵ���ѯ����������ѯ�����ڲ�ѯ������ע��ѯ��
    /// </summary>
    public partial class ItemSearchForm : ItemSearchFormBase
    {
        // const int WM_SELECT_INDEX_CHANGED = API.WM_USER + 200;

        List<ItemQueryParam> m_queries = new List<ItemQueryParam>();
        int m_nQueryIndex = -1;

        /// <summary>
        /// ���һ�ε�����������ļ�ʱʹ�ù����ļ���
        /// </summary>
        public string ExportBarcodeFilename = "";

        /// <summary>
        /// ���һ�ε�������¼·���ļ�ʱʹ�ù����ļ���
        /// </summary>
        public string ExportRecPathFilename = "";

        /// <summary>
        /// ���һ�ε������ı��ļ�ʱʹ�ù����ļ���
        /// </summary>
        public string ExportTextFilename = "";

        /// <summary>
        /// ���һ�ε�������Ŀ���¼·���ļ�ʱʹ�ù����ļ���
        /// </summary>
        public string ExportBiblioRecPathFilename = "";

        /// <summary>
        /// ���һ�ε�����ʵ����¼·���ļ�ʱʹ�ù����ļ���
        /// </summary>
        public string ExportItemRecPathFilename = "";

        /// <summary>
        /// �������ʾ�������м�¼�������ʽ
        /// </summary>
        public ListView ListViewRecords
        {
            get
            {
                return this.listView_records;
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public ItemSearchForm()
        {
            InitializeComponent();

            _listviewRecords = this.listView_records;

            ListViewProperty prop = new ListViewProperty();
            this.listView_records.Tag = prop;
            // ��һ�����⣬��¼·��
            prop.SetSortStyle(0, ColumnSortStyle.RecPath);
            prop.GetColumnTitles -= new GetColumnTitlesEventHandler(prop_GetColumnTitles);
            prop.GetColumnTitles += new GetColumnTitlesEventHandler(prop_GetColumnTitles);

            prop.CompareColumn -= new CompareEventHandler(prop_CompareColumn);
            prop.CompareColumn += new CompareEventHandler(prop_CompareColumn); 
        }

        void prop_CompareColumn(object sender, CompareEventArgs e)
        {
            if (e.Column.SortStyle.Name == "call_number")
            {
                // �Ƚ�������ȡ�ŵĴ�С
                // return:
                //      <0  s1 < s2
                //      ==0 s1 == s2
                //      >0  s1 > s2
                e.Result = StringUtil.CompareAccessNo(e.String1, e.String2, true);
            }
            else if (e.Column.SortStyle.Name == "parent_id")
            {
                // �Ҷ���Ƚ��ַ���
                // parameters:
                //      chFill  ����õ��ַ�
                e.Result = StringUtil.CompareRecPath(e.String1, e.String2);
            }
            else
                e.Result = string.Compare(e.String1, e.String2);
        }

        void ClearListViewPropertyCache()
        {
            ListViewProperty prop = (ListViewProperty)this.listView_records.Tag;
            prop.ClearCache();
        }

        void prop_GetColumnTitles(object sender, GetColumnTitlesEventArgs e)
        {
            if (e.DbName == "<blank>")
            {
                e.ColumnTitles = new ColumnPropertyCollection();
                e.ColumnTitles.Add("������");
                e.ColumnTitles.Add("����");
                // �����е�����
                e.ListViewProperty.SetSortStyle(2, ColumnSortStyle.RightAlign);
                return;
            }

            e.ColumnTitles = new ColumnPropertyCollection();
            ColumnPropertyCollection temp = this.MainForm.GetBrowseColumnProperties(e.DbName);
            if (temp != null)
            {
                if (m_bBiblioSummaryColumn == true)
                    e.ColumnTitles.Insert(0, "��ĿժҪ");
                e.ColumnTitles.AddRange(temp);  // Ҫ���ƣ���Ҫֱ��ʹ�ã���Ϊ������ܻ��޸ġ���Ӱ�쵽ԭ��
            }

            if (this.m_bFirstColumnIsKey == true)
                e.ColumnTitles.Insert(0, "���еļ�����");

            // e.ListViewProperty.SetSortStyle(2, ColumnSortStyle.LeftAlign);   // Ӧ�ø��� typeΪitem_barcode ����������ʽ
        }

        private void ItemSearchForm_Load(object sender, EventArgs e)
        {
            /*
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
             * */
            this.FillFromList();

            string strDefaulFrom = "";
            if (this.DbType == "item")
                strDefaulFrom = "������";
            else if (this.DbType == "comment")
                strDefaulFrom = "����";
            else if (this.DbType == "order")
                strDefaulFrom = "����";
            else if (this.DbType == "issue")
                strDefaulFrom = "�ں�";
            else if (this.DbType == "arrive")
                strDefaulFrom = "�������";
            else
                throw new Exception("δ֪��DbType '" + this.DbType + "'");


            this.comboBox_from.Text = this.MainForm.AppInfo.GetString(
                this.DbType + "_search_form",
                "from",
                strDefaulFrom);

            this.comboBox_entityDbName.Text = this.MainForm.AppInfo.GetString(
                this.DbType + "_search_form",
                "entity_db_name",
                "<ȫ��>");

            this.comboBox_matchStyle.Text = this.MainForm.AppInfo.GetString(
                this.DbType + "_search_form",
                "match_style",
                "��ȷһ��");

            if (this.DbType != "arrive")
            {
                bool bHideMatchStyle = this.MainForm.AppInfo.GetBoolean(
                    this.DbType + "_search_form",
                    "hide_matchstyle_and_dbname",
                    true);
                if (bHideMatchStyle == true)
                {
                    this.label_matchStyle.Visible = false;
                    this.comboBox_matchStyle.Visible = false;
                    this.comboBox_matchStyle.Text = "��ȷһ��"; // ���غ󣬲���ȱʡֵ

                    this.label_entityDbName.Visible = false;
                    this.comboBox_entityDbName.Visible = false;
                    this.comboBox_entityDbName.Text = "<ȫ��>"; // ���غ󣬲���ȱʡֵ

                    string strName = this.DbTypeCaption;
                    if (this.DbType == "item")
                        strName = "ʵ��";

                    this.label_message.Text = "��ǰ���������õ�ƥ�䷽ʽΪ '��ȷһ��'�����ȫ��" + strName + "��";
                }
            }

#if NO
            string strWidths = this.MainForm.AppInfo.GetString(
                this.DbType + "_search_form",
                "record_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_records,
                    strWidths,
                    true);
            }
#endif
            this.UiState = this.MainForm.AppInfo.GetString(
    this.DbType + "_search_form",
    "ui_state",
    "");
            string strSaveString = this.MainForm.AppInfo.GetString(
this.DbType + "_search_form",
"query_lines",
"^^^");
            this.dp2QueryControl1.Restore(strSaveString);

            comboBox_matchStyle_TextChanged(null, null);

            this.SetWindowTitle();
        }

        public string UiState
        {
            get
            {
                List<object> controls = new List<object>();
                controls.Add(this.splitContainer_main);
                controls.Add(this.tabControl_query);
                controls.Add(this.listView_records);

                return GuiState.GetUiState(controls);
            }
            set
            {
                List<object> controls = new List<object>();
                controls.Add(this.splitContainer_main);
                controls.Add(this.tabControl_query);
                controls.Add(this.listView_records);
                GuiState.SetUiState(controls, value);
            }
        }


        void SetWindowTitle()
        {
            string strLogic = "";
            if (this.tabControl_query.SelectedTab == this.tabPage_logic)
                strLogic = " �߼�����";

            if (this.DbType == "item")
            {
                this.Text = "ʵ���ѯ" + strLogic;
                this.label_entityDbName.Text = "ʵ���(&D)";
            }
            else
            {
                this.Text = this.DbTypeCaption + "��ѯ" + strLogic;
                this.label_entityDbName.Text = this.DbTypeCaption + "��(&D)";
            }
        }

        static string[] item_froms = {
                                       "������",
                                       "���κ�",
                                       "��¼��",
                                       "��ȡ��",
                                       "�ο�ID",
                                       "�ݲصص�",
                                       "��ȡ���",
                                       "����¼",
                                       "״̬",
                                       "__id"
                                   };


        static string[] comment_froms = {
                "����",
                "����",
                "������ʾ��",
                "����",
                "�ο�ID",
                "����޸�ʱ��",
                "����¼",
                "״̬",
                "__id"
                                      };
        static string[] order_froms = {
                "����",
                "���κ�",
                "��ο�ID",
                "����ʱ��",
                "�ο�ID",
                "����¼",
                "״̬",
                "__id"
                                    };
        static string[] issue_froms = {
                "����ʱ��",
                "�ں�",
                "���ں�",
                "���",
                "��ο�ID",
                "�ο�ID",
                "���κ�",
                "����¼",
                "״̬",
                "__id"
                                    };

        List<string> GetFromList()
        {
            List<string> results = new List<string>();

            // ����ӷ�������õ�����froms
            if (this.MainForm != null)
            {
                BiblioDbFromInfo[] infos = null;
                if (this.DbType == "item")
                    infos = this.MainForm.ItemDbFromInfos;
                else if (this.DbType == "comment")
                    infos = this.MainForm.CommentDbFromInfos;
                else if (this.DbType == "order")
                    infos = this.MainForm.OrderDbFromInfos;
                else if (this.DbType == "issue")
                    infos = this.MainForm.IssueDbFromInfos;
                else if (this.DbType == "arrive")
                    infos = this.MainForm.ArrivedDbFromInfos;
                else
                    throw new Exception("δ֪��DbType '" + this.DbType + "'");

                if (infos != null && infos.Length > 0)
                {
                    for (int i = 0; i < infos.Length; i++)
                    {
                        string strCaption = infos[i].Caption;
                        results.Add(strCaption);
                    }

                    return results;
                }
            }

            if (this.DbType == "item")
                return new List<string>(item_froms);
            else if (this.DbType == "comment")
                return new List<string>(comment_froms);
            else if (this.DbType == "order")
                return new List<string>(order_froms);
            else if (this.DbType == "issue")
                return new List<string>(issue_froms);
            else if (this.DbType == "arrive")
                return new List<string>(arrive_froms);
            else
                throw new Exception("δ֪��DbType '" + this.DbType + "'");
        }

        void FillFromList()
        {
#if NO
            string[] item_froms = {
                                       "������",
                                       "���κ�",
                                       "��¼��",
                                       "��ȡ��",
                                       "�ο�ID",
                                       "�ݲصص�",
                                       "��ȡ���",
                                       "����¼",
                                       "״̬",
                                       "__id"
                                   };
            string[] comment_froms = {
                "����",
                "����",
                "������ʾ��",
                "����",
                "�ο�ID",
                "����޸�ʱ��",
                "����¼",
                "״̬",
                "__id"
                                      };
            string[] order_froms = {
                "����",
                "���κ�",
                "��ο�ID",
                "����ʱ��",
                "�ο�ID",
                "����¼",
                "״̬",
                "__id"
                                    };
            string[] issue_froms = {
                "����ʱ��",
                "�ں�",
                "���ں�",
                "���",
                "��ο�ID",
                "�ο�ID",
                "���κ�",
                "����¼",
                "״̬",
                "__id"
                                    };

            string[] froms = null;

            if (this.DbType == "item")
                froms = item_froms;
            else if (this.DbType == "comment")
                froms = comment_froms;
            else if (this.DbType == "order")
                froms = order_froms;
            else if (this.DbType == "issue")
                froms = issue_froms;
            else
                throw new Exception("δ֪��DbType '" + this.DbType + "'");

            this.comboBox_from.Items.Clear();
            foreach (string from in froms)
            {
                this.comboBox_from.Items.Add(from);
            }

            // ����ӷ�������õ�����froms
            if (this.MainForm != null)
            {
                BiblioDbFromInfo[] infos = null;
                if (this.DbType == "item")
                    infos = this.MainForm.ItemDbFromInfos;
                else if (this.DbType == "comment")
                    infos = this.MainForm.CommentDbFromInfos;
                else if (this.DbType == "order")
                    infos = this.MainForm.OrderDbFromInfos;
                else if (this.DbType == "issue")
                    infos = this.MainForm.IssueDbFromInfos;
                else
                    throw new Exception("δ֪��DbType '" + this.DbType + "'");

                if (infos != null && infos.Length > 0)
                {
                    this.comboBox_from.Items.Clear();
                    for (int i = 0; i < infos.Length; i++)
                    {
                        string strCaption = infos[i].Caption;
                        this.comboBox_from.Items.Add(strCaption);
                    }
                }
            }
#endif
            this.comboBox_from.Items.Clear();
            List<string> froms = GetFromList();
            foreach (string from in froms)
            {
                this.comboBox_from.Items.Add(from);
            }
            // this.comboBox_from.Items.AddRange(GetFromList());
            
        }

        /*
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }*/

        private void ItemSearchForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            /*
            if (stop != null)
            {
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }

            }
            */
        }

        private void ItemSearchForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            /*
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }*/

            this.MainForm.AppInfo.SetString(
                this.DbType + "_search_form",
                "from",
                this.comboBox_from.Text);

            this.MainForm.AppInfo.SetString(
                this.DbType + "_search_form",
                "entity_db_name",
                this.comboBox_entityDbName.Text);

            this.MainForm.AppInfo.SetString(
                this.DbType + "_search_form",
                "match_style",
                this.comboBox_matchStyle.Text);

#if NO
            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_records);
            this.MainForm.AppInfo.SetString(
                this.DbType + "_search_form",
                "record_list_column_width",
                strWidths);
#endif
            this.MainForm.AppInfo.SetString(
                this.DbType + "_search_form",
                "ui_state",
                this.UiState);
            this.MainForm.AppInfo.SetString(
this.DbType + "_search_form",
"query_lines",
this.dp2QueryControl1.GetSaveString());
        }

        /// <summary>
        /// ������ò�������ǰ��ѯ�����μ���������м�¼�������塣-1��ʾ������
        /// </summary>
        public int MaxSearchResultCount
        {
            get
            {
                return (int)this.MainForm.AppInfo.GetInt(
                this.DbType + "_search_form",
                "max_result_count",
                -1);
            }
        }

        // 2008/1/20 
        /// <summary>
        /// ������ò�������ǰ��ѯ������ʱ���Ƿ����ƶ��ķ�ʽװ������б�
        /// </summary>
        public bool PushFillingBrowse
        {
            get
            {
                return MainForm.AppInfo.GetBoolean(
                this.DbType + "_search_form",
                    "push_filling_browse",
                    false);
            }
        }

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

        private void button_search_Click(object sender, EventArgs e)
        {
            DoSearch(false, false, null);
        }

        ItemQueryParam PanelToQuery()
        {
            ItemQueryParam query = new ItemQueryParam();

            query.QueryWord = this.textBox_queryWord.Text;
            query.DbNames = this.comboBox_entityDbName.Text;
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

        public static int SearchOneLocationItems(
            MainForm main_form,
            LibraryChannel channel,
            Stop stop,
            string strLocation,
            string strOutputStyle,
            out List<string> results,
            out string strError)
        {
            strError = "";
            results = new List<string>();

            long lRet = channel.SearchItem(stop,
                "<all>",
                strLocation, // 
                -1,
                "�ݲصص�",
                "left", // this.textBox_queryWord.Text == "" ? "left" : "exact",    // ԭ��Ϊleft 2007/10/18 changed
                "zh",
                null,   // strResultSetName
                "",    // strSearchStyle
                "", //strOutputStyle, // (bOutputKeyCount == true ? "keycount" : ""),
                out strError);
            if (lRet == -1)
                return -1;
            long lHitCount = lRet;

            long lStart = 0;
            long lCount = lHitCount;
            DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

            bool bOutputBiblioRecPath = false;
            bool bOutputItemRecPath = false;
            string strStyle = "";
            if (strOutputStyle == "bibliorecpath")
            {
                bOutputBiblioRecPath = true;
                strStyle = "id,cols,format:@coldef:*/parent";
            }
            else
            {
                bOutputItemRecPath = true;
                strStyle = "id";
            }

            // ʵ����� --> ��Ŀ����
            Hashtable dbname_table = new Hashtable();

            // ��Ŀ���¼·��������ȥ��
            Hashtable bilio_recpath_table = new Hashtable();

            // װ�������ʽ
            for (; ; )
            {
                Application.DoEvents();	// ���ý������Ȩ

                if (stop != null && stop.State != 0)
                {
                    strError = "���������� " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " �����û��ж�...";
                    return -1;
                }


                lRet = channel.GetSearchResult(
                    stop,
                    null,   // strResultSetName
                    lStart,
                    lCount,
                    strStyle, // bOutputKeyCount == true ? "keycount" : "id,cols",
                    "zh",
                    out searchresults,
                    out strError);
                if (lRet == -1)
                {
                    strError = "���������� " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " ����" + strError;
                    return -1;
                }

                if (lRet == 0)
                {
                    return 0;
                }

                // ����������

                for (int i = 0; i < searchresults.Length; i++)
                {
                    DigitalPlatform.CirculationClient.localhost.Record searchresult = searchresults[i];

                    if (bOutputBiblioRecPath == true)
                    {
                        string strItemDbName = Global.GetDbName(searchresult.Path);
                        string strBiblioDbName = (string)dbname_table[strItemDbName];
                        if (string.IsNullOrEmpty(strBiblioDbName) == true)
                        {
                            strBiblioDbName = main_form.GetBiblioDbNameFromItemDbName(strItemDbName);
                            dbname_table[strItemDbName] = strBiblioDbName;
                        }

                        string strBiblioRecPath = strBiblioDbName + "/" + searchresult.Cols[0];

                        if (bilio_recpath_table.ContainsKey(strBiblioRecPath) == false)
                        {
                            results.Add(strBiblioRecPath);
                            bilio_recpath_table[strBiblioRecPath] = true;
                        }
                    }
                    else if (bOutputItemRecPath == true)
                        results.Add(searchresult.Path);
                }

                lStart += searchresults.Length;
                lCount -= searchresults.Length;

                stop.SetMessage("������ " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " ��");

                if (lStart >= lHitCount || lCount <= 0)
                    break;
            }

            return 0;
        }

        /// <summary>
        /// ִ��һ�μ���
        /// </summary>
        /// <param name="bOutputKeyCount">�Ƿ�Ҫ���Ϊ key+count ��̬</param>
        /// <param name="bOutputKeyID">�Ƿ�Ϊ keyid ��̬</param>
        /// <param name="input_query">����ʽ</param>
        /// <param name="bClearList">�Ƿ�Ҫ�ڼ�����ʼʱ�������б�</param>
        /// <returns>-1:���� 0:�ж� 1:��������</returns>
        public int DoSearch(bool bOutputKeyCount,
            bool bOutputKeyID,
            ItemQueryParam input_query,
            bool bClearList = true)
        {
            string strError = "";
            int nRet = 0;

            if (bOutputKeyCount == true
                && bOutputKeyID == true)
            {
                strError = "bOutputKeyCount��bOutputKeyID����ͬʱΪtrue";
                goto ERROR1;
            }

            /*
            bool bOutputKeyCount = false;
            if (Control.ModifierKeys == Keys.Control)
                bOutputKeyCount = true;
             * */

            if (input_query != null)
            {
                QueryToPanel(input_query, bClearList);
            }

            // �����¼���ʽ
            this.m_bFirstColumnIsKey = bOutputKeyID;
            this.ClearListViewPropertyCache();

            ItemQueryParam query = PanelToQuery();
            PushQuery(query);

            if (bClearList == true)
            {
                ClearListViewItems();
                m_tableSummaryColIndex.Clear();
            }


            EnableControls(false);
            this.label_message.Text = "";

            stop.Style = StopStyle.None;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            try
            {
                string strMatchStyle = "";

                strMatchStyle = GetCurrentMatchStyle();

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

                string strBrowseStyle = "id, cols";
                string strOutputStyle = "";
                if (bOutputKeyCount == true)
                {
                    strOutputStyle = "keycount";
                    strBrowseStyle = "keycount";
                }
                else if (bOutputKeyID == true)
                {
                    strOutputStyle = "keyid";
                    strBrowseStyle = "keyid,key,id,cols";
                }

                long lRet = 0;

                if (this.DbType == "item")
                {
                    lRet = Channel.SearchItem(stop,
                        this.comboBox_entityDbName.Text, // "<all>",
                        this.textBox_queryWord.Text,
                        this.MaxSearchResultCount,
                        this.comboBox_from.Text,
                        strMatchStyle, // this.textBox_queryWord.Text == "" ? "left" : "exact",    // ԭ��Ϊleft 2007/10/18 changed
                        this.Lang,
                        null,   // strResultSetName
                        "",    // strSearchStyle
                        strOutputStyle, // (bOutputKeyCount == true ? "keycount" : ""),
                        out strError);
                }
                else if (this.DbType == "comment")
                {
                    lRet = Channel.SearchComment(stop,
                        this.comboBox_entityDbName.Text,
                        this.textBox_queryWord.Text,
                        this.MaxSearchResultCount,
                        this.comboBox_from.Text,
                        strMatchStyle, 
                        this.Lang,
                        null,
                        "",
                        strOutputStyle,
                        out strError);
                }
                else if (this.DbType == "order")
                {
                    lRet = Channel.SearchOrder(stop,
                        this.comboBox_entityDbName.Text,
                        this.textBox_queryWord.Text,
                        this.MaxSearchResultCount,
                        this.comboBox_from.Text,
                        strMatchStyle,
                        this.Lang,
                        null,
                        "",
                        strOutputStyle,
                        out strError);
                }
                else if (this.DbType == "issue")
                {
                    lRet = Channel.SearchIssue(stop,
                        this.comboBox_entityDbName.Text,
                        this.textBox_queryWord.Text,
                        this.MaxSearchResultCount,
                        this.comboBox_from.Text,
                        strMatchStyle,
                        this.Lang,
                        null,
                        "",
                        strOutputStyle,
                        out strError);
                }
                else if (this.DbType == "arrive")
                {
#if NO
                    string strArrivedDbName = "";
                    // return:
                    //      -1  ����
                    //      0   û������
                    //      1   �ҵ�
                    nRet = GetArrivedDbName(false, out strArrivedDbName, out strError);
                    if (nRet == -1 || nRet == 0)
                        goto ERROR1;
#endif
                    if (string.IsNullOrEmpty(this.MainForm.ArrivedDbName) == true)
                    {
                        strError = "��ǰ��������δ����ԤԼ�������";
                        goto ERROR1;
                    }

                    string strQueryXml = "<target list='" + this.MainForm.ArrivedDbName + ":" + this.comboBox_from.Text + "'><item><word>"
        + StringUtil.GetXmlStringSimple(this.textBox_queryWord.Text)
        + "</word><match>" + strMatchStyle + "</match><relation>=</relation><dataType>string</dataType><maxCount>"
                    + this.MaxSearchResultCount + "</maxCount></item><lang>" + this.Lang + "</lang></target>";
                    // strOutputStyle ?
                    lRet = Channel.Search(stop,
                        strQueryXml,
                        "",
                        strOutputStyle,
                        out strError);
                }
                else
                    throw new Exception("δ֪��DbType '" + this.DbType + "'");

                if (lRet == -1)
                    goto ERROR1;

                long lHitCount = lRet;

                // return:
                //      -1  ����
                //      0   �û��ж�
                //      1   �������
                nRet = FillBrowseList(
                    query,
                    lHitCount,
                    bOutputKeyCount,
                    bOutputKeyID,
                    out strError);
                if (nRet == 0)
                    return 0;

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
                this.label_message.Text = "���������� " + lHitCount.ToString() + " ������ȫ��װ��";
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                EnableControls(true);
            }

            return 1;
        ERROR1:
            MessageBox.Show(this, strError);
        return -1;
        }

        #region ԤԼ��������

        static string[] arrive_froms = {
                "�������",
                "����֤�����",
                "��ο�ID",
                "״̬",
                "__id"};

#if NO
        /// <summary>
        /// ���ԤԼ�����Ŀ���
        /// ע��ʹ�ô����Ե�ʱ�򣬻� BeginLoop������������ BeginLoop������� Stop ��״̬���Ժ�������ԸĽ�Ϊʹ�� Pooling Channel �ͺ���
        /// </summary>
        string ArrivedDbName
        {
            get
            {

                string strError = "";
                string strArrivedDbName = "";
                // return:
                //      -1  ����
                //      0   û������
                //      1   �ҵ�
                int nRet = GetArrivedDbName(true, out strArrivedDbName, out strError);
                if (nRet == -1 || nRet == 0)
                    throw new Exception(strError);

                return strArrivedDbName;
            }
        }

        string _arrivedDbName = "";

        // return:
        //      -1  ����
        //      0   û������
        //      1   �ҵ�
        int GetArrivedDbName(
            bool bBeginLoop,
            out string strDbName,
            out string strError)
        {
            strError = "";
            strDbName = "";

            if (string.IsNullOrEmpty(this._arrivedDbName) == false)
            {
                strDbName = this._arrivedDbName;
                return 1;
            }

            if (bBeginLoop == true)
            {
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.SetMessage("���ڻ�ȡԤԼ������� ...");
                stop.BeginLoop();
            }

            try
            {
                long lRet = Channel.GetSystemParameter(
                    stop,
                    "arrived",
                    "dbname",
                    out strDbName,
                    out strError);
                if (lRet == -1)
                    return -1;
                if (lRet == 0
                    || string.IsNullOrEmpty(strDbName) == true)
                {
                    strError = "ԤԼ�������û������";
                    return 0;   // not found
                }
                this._arrivedDbName = strDbName;
                return 1;
            }
            finally
            {
                if (bBeginLoop)
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }
            }
        }
#endif

        #endregion

        // return:
        //      -1  ����
        //      0   �û��жϻ���δ����
        //      1   �������
        int FillBrowseList(
            ItemQueryParam query,
            long lHitCount,
            bool bOutputKeyCount,
            bool bOutputKeyID,
            out string strError)
        {
            strError = "";

            bool bAccessBiblioSummaryDenied = false;

            string strBrowseStyle = "id, cols";
            //string strOutputStyle = "";
            if (bOutputKeyCount == true)
            {
                //strOutputStyle = "keycount";
                strBrowseStyle = "keycount";
            }
            else if (bOutputKeyID == true)
            {
                //strOutputStyle = "keyid";
                strBrowseStyle = "keyid,key,id,cols";
            }
            //
            this.label_message.Text = "���������� " + lHitCount.ToString() + " ��";
            stop.SetProgressRange(0, lHitCount);
            stop.Style = StopStyle.EnableHalfStop;

            bool bSelectFirstLine = false;
            long lStart = 0;
            long lCount = lHitCount;
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
                        this.label_message.Text = "���������� " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " �����û��ж�...";
                        return 0;
                    }
                }


                long lRet = Channel.GetSearchResult(
                    stop,
                    null,   // strResultSetName
                    lStart,
                    lCount,
                    strBrowseStyle, // bOutputKeyCount == true ? "keycount" : "id,cols",
                    this.Lang,
                    out searchresults,
                    out strError);
                if (lRet == -1)
                {
                    this.label_message.Text = "���������� " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " ����" + strError;
                    goto ERROR1;
                }

                if (lRet == 0)
                {
                    MessageBox.Show(this, "δ����");
                    return 0;
                }

                // ����������
                this.listView_records.BeginUpdate();
                try
                {
                    List<ListViewItem> items = new List<ListViewItem>();
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        ListViewItem item = null;

                        DigitalPlatform.CirculationClient.localhost.Record searchresult = searchresults[i];

                        if (bOutputKeyCount == false
                            && bOutputKeyID == false)
                        {
                            if (bPushFillingBrowse == true)
                                item = Global.InsertNewLine(
                                    this.listView_records,
                                    searchresult.Path,
                                    this.m_bBiblioSummaryColumn == true ? InsertBlankColumn(searchresult.Cols) : searchresult.Cols);
                            else
                                item = Global.AppendNewLine(
                                    this.listView_records,
                                    searchresult.Path,
                                    this.m_bBiblioSummaryColumn == true ? InsertBlankColumn(searchresult.Cols) : searchresult.Cols);
                        }
                        else if (bOutputKeyCount == true)
                        {
                            // ���keys
                            if (searchresult.Cols == null)
                            {
                                strError = "Ҫʹ�û�ȡ�����㹦�ܣ��뽫 dp2Library Ӧ�÷������� dp2Kernel ���ݿ��ں����������°汾";
                                goto ERROR1;
                            }
                            string[] cols = new string[(searchresult.Cols == null ? 0 : searchresult.Cols.Length) + 1];
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
                        }
                        else if (bOutputKeyID == true)
                        {
                            if (searchresult.Cols == null)
                            {
                                strError = "Ҫʹ�ô��м�����ļ������ܣ��뽫 dp2Library Ӧ�÷������� dp2Kernel ���ݿ��ں����������°汾";
                                goto ERROR1;
                            }


#if NO
                                string[] cols = new string[(searchresult.Cols == null ? 0 : searchresult.Cols.Length) + 1];
                                cols[0] = LibraryChannel.BuildDisplayKeyString(searchresult.Keys);
                                if (cols.Length > 1)
                                    Array.Copy(searchresult.Cols, 0, cols, 1, cols.Length - 1);
#endif
                            string[] cols = this.m_bBiblioSummaryColumn == true ? InsertBlankColumn(searchresult.Cols, 2) : searchresult.Cols;
                            cols[0] = LibraryChannel.BuildDisplayKeyString(searchresult.Keys);

                            if (bPushFillingBrowse == true)
                                item = Global.InsertNewLine(
                                    this.listView_records,
                                    searchresult.Path,
                                    cols);
                            else
                                item = Global.AppendNewLine(
                                    this.listView_records,
                                    searchresult.Path,
                                    cols);
                            item.Tag = query;
                        }

                        query.Items.Add(item);
                        items.Add(item);
                        stop.SetProgressValue(lStart + i);
                    }

                    if (bOutputKeyCount == false
                        && bAccessBiblioSummaryDenied == false)
                    {
                        // return:
                        //      -2  �����ĿժҪ��Ȩ�޲���
                        //      -1  ����
                        //      0   �û��ж�
                        //      1   ���
                        int nRet = _fillBiblioSummaryColumn(items,
                            0,
                            false,
                            true,   // false,  // bAutoSearch
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == -2)
                            bAccessBiblioSummaryDenied = true;

                        if (nRet == 0)
                        {
                            this.label_message.Text = "���������� " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " �����û��ж�...";
                            return 0;
                        }
                    }

                }
                finally
                {
                    this.listView_records.EndUpdate();
                }

                if (bSelectFirstLine == false && this.listView_records.Items.Count > 0)
                {
                    if (this.listView_records.SelectedItems.Count == 0)
                        this.listView_records.Items[0].Selected = true;
                    bSelectFirstLine = true;
                }

                lStart += searchresults.Length;
                lCount -= searchresults.Length;

                stop.SetMessage("������ " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " ��");

                if (lStart >= lHitCount || lCount <= 0)
                    break;
                stop.SetProgressValue(lStart);
            }

            if (bAccessBiblioSummaryDenied == true)
                MessageBox.Show(this, "��ǰ�û����߱���ȡ��ĿժҪ��Ȩ��");

            return 1;
        ERROR1:
            return -1;
        }

        // �ڵ�һ��ǰ�����һ���հ���
        static string[] InsertBlankColumn(string [] cols,
            int nDelta = 1)
        {
            string[] results = new string[cols == null ? nDelta : cols.Length + nDelta];
            for (int i = 0; i < nDelta; i++)
            {
                results[i] = "";
            }
            if (results.Length > 1)
                Array.Copy(cols, 0, results, nDelta, results.Length - nDelta);
            return results;
        }


        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.button_search.Enabled = bEnable;
            this.comboBox_from.Enabled = bEnable;

            // 2008/11/21 
            this.comboBox_entityDbName.Enabled = bEnable;
            this.comboBox_matchStyle.Enabled = bEnable;

            this.toolStrip_search.Enabled = bEnable;

            if (this.comboBox_matchStyle.Text == "��ֵ")
            {
                this.textBox_queryWord.Enabled = false;
            }
            else
            {
                this.textBox_queryWord.Enabled = bEnable;
            }

            this.dp2QueryControl1.Enabled = bEnable;
        }

        /*
        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }*/


        private void textBox_queryWord_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_search;
        }

        private void listView_records_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = null;
        }


#if NOOOOOOOOOOOOOOOOOOOOOOOOOO

        void menu_loadToItemInfoFormByRecPath_Click(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫװ�����Ϣ���Ĳ�����");
                return;
            }

            string strRecPath = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 0);

            ItemInfoForm form = null;

            if (this.LoadToExistDetailWindow == true)
            {
                form = MainForm.TopItemInfoForm;
                if (form != null)
                    form.Activate();
            }

            if (form == null)
            {
                form = new ItemInfoForm();

                form.MdiParent = this.MainForm;

                form.MainForm = this.MainForm;
                form.Show();
            }

            form.LoadRecordByRecPath(strRecPath);
        }

        void menu_loadToItemInfoFormByBarcode_Click(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫװ�����Ϣ���Ĳ�����");
                return;
            }

            string strBarcode = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);

            ItemInfoForm form = null;

            if (this.LoadToExistDetailWindow == true)
            {
                form = MainForm.TopItemInfoForm;
                if (form != null)
                    form.Activate();
            }

            if (form == null)
            {
                form = new ItemInfoForm();

                form.MdiParent = this.MainForm;

                form.MainForm = this.MainForm;
                form.Show();
            }

            form.LoadRecord(strBarcode);
        }

        // װ���ֲᴰ���ò��¼·��
        // �Զ��жϸô��´��ڻ���ռ�����еĴ���
        void menu_loadToEntityFormByRecPath_Click(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫװ���ֲᴰ�Ĳ�����");
                return;
            }

            string strItemRecPath = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 0);

            EntityForm form = null;

            if (this.LoadToExistDetailWindow == true)
            {
                form = MainForm.TopEntityForm;
                if (form != null)
                    form.Activate();
            }

            if (form == null)
            {
                form = new EntityForm();

                form.MdiParent = this.MainForm;

                form.MainForm = this.MainForm;
                form.Show();
            }

            // parameters:
            //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            form.LoadItemByRecPath(strItemRecPath, false);
        }

        // װ���ֲᴰ���ò������
        void menu_loadToEntityFormByBarcode_Click(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫװ���ֲᴰ�Ĳ�����");
                return;
            }

            string strBarcode = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);

            EntityForm form = null;

            if (this.LoadToExistDetailWindow == true)
            {
                form = MainForm.TopEntityForm;
                if (form != null)
                    form.Activate();
            }

            if (form == null)
            {
                form = new EntityForm();

                form.MdiParent = this.MainForm;

                form.MainForm = this.MainForm;
                form.Show();
            }

            // װ��һ���ᣬ����װ����
            // parameters:
            //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            form.LoadItem(strBarcode, false);
        }

#endif

        /// <summary>
        /// ������ò����������������˫�����������߻س���һ�����ڹ۲쵱ǰ�е�ʱ���Ƿ�����װ���Ѿ��򿪵��ֲᴰ/�ᴰ/������/�ڴ�/��ע��?
        /// ������ò����Ƕ��������͵Ĳ�ѯ���������õ�
        /// </summary>
        public bool LoadToExistWindow
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "all_search_form",
                    "load_to_exist_detailwindow",
                    true);
            }
        }

        /// <summary>
        /// ������ò������Ƿ�װ��ᴰ/������/�ڴ�/��ע��(�������ֲᴰ)?
        /// ������ò������� ItemSearchForm ����
        /// </summary>
        public bool LoadToItemWindow
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "item_search_form",
                    "load_to_itemwindow",
                    false);
            }
            set
            {
                this.MainForm.AppInfo.SetBoolean(
                    "item_search_form",
                    "load_to_itemwindow",
                    value);
            }

        }

        private void listView_records_DoubleClick(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫ����������");
                return;
            }

            string strFirstColumn = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 0);

            if (String.IsNullOrEmpty(strFirstColumn) == false)
            {
                string strOpenStyle = "new";
                if (this.LoadToExistWindow == true)
                    strOpenStyle = "exist";

                bool bLoadToItemWindow = this.LoadToItemWindow;

                if (bLoadToItemWindow == true)
                {
                    LoadRecord("ItemInfoForm",
                        "recpath",
                        strOpenStyle);
                    return;
                }

                // װ���ֲᴰ/ʵ�崰���ò������/��¼·��
                // parameters:
                //      strTargetFormType   Ŀ�괰������ "EntityForm" "ItemInfoForm"
                //      strIdType   ��ʶ���� "barcode" "recpath"
                //      strOpenType �򿪴��ڵķ�ʽ "new" "exist"
                LoadRecord("EntityForm",
                    "recpath",
                    strOpenStyle);
            }
            else
            {
                ItemQueryParam query = (ItemQueryParam)this.listView_records.SelectedItems[0].Tag;
                Debug.Assert(query != null, "");

                this.textBox_queryWord.Text = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);
                if (query != null)
                {
                    this.comboBox_entityDbName.Text = query.DbNames;
                    this.comboBox_from.Text = query.From;
                }

                if (this.textBox_queryWord.Text == "")    // 2009/8/6 
                    this.comboBox_matchStyle.Text = "��ֵ";
                else
                    this.comboBox_matchStyle.Text = "��ȷһ��";

                DoSearch(false, false, null);
            }
        }

        private void ItemSearchForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            // this.MainForm.MenuItem_font.Enabled = false;
            // this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        // װ���ֲᴰ/ʵ�崰���ò������/��¼·��
        // parameters:
        //      strTargetFormType   Ŀ�괰������ "EntityForm" "ItemInfoForm"
        //      strIdType   ��ʶ���� "barcode" "recpath"
        //      strOpenType �򿪴��ڵķ�ʽ "new" "exist"
        void LoadRecord(string strTargetFormType,
            string strIdType,
            string strOpenType)
        {
            string strTargetFormName = "�ֲᴰ";

            if (strTargetFormType == "ItemInfoForm")
                strTargetFormName = "ʵ�崰";

            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫװ��" + strTargetFormName + "����");
                return;
            }

            string strBarcodeOrRecPath = "";

            if (strIdType == "barcode")
            {
                // barcode
                // strBarcodeOrRecPath = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);
                string strError = "";
                // ���� ListViewItem ���󣬻�ò�������е�����
                int nRet = GetItemBarcodeOrRefID(
                    this.listView_records.SelectedItems[0],
                    true,
                    out strBarcodeOrRecPath,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return;
                }
            }
            else
            {
                Debug.Assert(strIdType == "recpath", "");
                // recpath
                strBarcodeOrRecPath = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 0);
            }

            if (strTargetFormType == "EntityForm")
            {
                EntityForm form = null;

                if (strOpenType == "exist")
                {
                    form = MainForm.GetTopChildWindow<EntityForm>();
                    if (form != null)
                        Global.Activate(form);
                }
                else
                {
                    Debug.Assert(strOpenType == "new", "");
                }

                if (form == null)
                {
                    form = new EntityForm();

                    form.MdiParent = this.MainForm;

                    form.MainForm = this.MainForm;
                    form.Show();
                }

                if (strIdType == "barcode")
                {
                    // װ��һ���ᣬ����װ����
                    // parameters:
                    //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   found
                    form.LoadItemByBarcode(strBarcodeOrRecPath, false);
                }
                else
                {
                    Debug.Assert(strIdType == "recpath", "");

                    if (this.DbType == "item")
                    {
                        // parameters:
                        //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   found
                        form.LoadItemByRecPath(strBarcodeOrRecPath, false);
                    }
                    else if (this.DbType == "comment")
                        form.LoadCommentByRecPath(strBarcodeOrRecPath, false);
                    else if (this.DbType == "order")
                        form.LoadOrderByRecPath(strBarcodeOrRecPath, false);
                    else if (this.DbType == "issue")
                        form.LoadIssueByRecPath(strBarcodeOrRecPath, false);
                    else
                        throw new Exception("δ֪��DbType '" + this.DbType + "'");
                }
            }
            else
            {
                Debug.Assert(strTargetFormType == "ItemInfoForm", "");

                ItemInfoForm form = null;

                if (strOpenType == "exist")
                {
                    form = MainForm.GetTopChildWindow<ItemInfoForm>();
                    if (form != null)
                        Global.Activate(form);
                }
                else
                {
                    Debug.Assert(strOpenType == "new", "");
                }

                if (form == null)
                {
                    form = new ItemInfoForm();

                    form.MdiParent = this.MainForm;

                    form.MainForm = this.MainForm;
                    form.Show();
                }

                if (this.DbType == "arrive")
                    form.DbType = "item";
                else
                    form.DbType = this.DbType;

                if (strIdType == "barcode")
                {
                    Debug.Assert(this.DbType == "item" || this.DbType == "arrive", "");
                    form.LoadRecord(strBarcodeOrRecPath);
                }
                else
                {
                    Debug.Assert(strIdType == "recpath", "");

                    // TODO: ��Ҫ����Ϊ��Ӧ���ּ�¼
                    form.LoadRecordByRecPath(strBarcodeOrRecPath, "");
                }
            }
        }

        void menu_itemInfoForm_recPath_newly_Click(object sender, EventArgs e)
        {
            LoadRecord("ItemInfoForm",
                "recpath",
                "new");
            this.LoadToItemWindow = true;
        }

        void menu_itemInfoForm_barcode_newly_Click(object sender, EventArgs e)
        {
            LoadRecord("ItemInfoForm",
                "barcode",
                "new");
            this.LoadToItemWindow = true;
        }

        void menu_entityForm_recPath_newly_Click(object sender, EventArgs e)
        {
            LoadRecord("EntityForm",
                "recpath",
                "new");
            this.LoadToItemWindow = false;
        }

        void menu_entityForm_barcode_newly_Click(object sender, EventArgs e)
        {
            LoadRecord("EntityForm",
                "barcode",
                "new");
            this.LoadToItemWindow = false;
        }

        void menu_itemInfoForm_recPath_exist_Click(object sender, EventArgs e)
        {
            LoadRecord("ItemInfoForm",
                "recpath",
                "exist");
            this.LoadToItemWindow = true;
        }

        void menu_itemInfoForm_barcode_exist_Click(object sender, EventArgs e)
        {
            LoadRecord("ItemInfoForm",
                "barcode",
                "exist");
            this.LoadToItemWindow = true;
        }

        void menu_entityForm_recPath_exist_Click(object sender, EventArgs e)
        {
            LoadRecord("EntityForm",
                "recpath",
                "exist");
            this.LoadToItemWindow = false;
        }

        void menu_entityForm_barcode_exist_Click(object sender, EventArgs e)
        {
            LoadRecord("EntityForm",
                "barcode",
                "exist");
            this.LoadToItemWindow = false;
        }

        int GetItemBarcodeOrRefID(ListViewItem item,
            bool bWarning,
            out string strBarcode,
            out string strError)
        {
            // strBarcode = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);
            int nRet = GetItemBarcode(
item,
bWarning,
out strBarcode,
out strError);
            if (nRet == -1)
                return -1;
            // 2015/6/14
            // ����������Ϊ�գ������� �ο�ID
            if (string.IsNullOrEmpty(strBarcode) == true)
            {
                // return:
                //      -2  û���ҵ��� type
                //      -1  ����
                //      >=0 �к�
                nRet = GetColumnText(item,
"item_refid",
out strBarcode,
out strError);
                if (nRet >= 0 && string.IsNullOrEmpty(strBarcode) == false)
                {
                    strBarcode = "@refID:" + strBarcode;
                }
            }

            return 0;
        }

        private void listView_records_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            int nSeletedItemCount = this.listView_records.SelectedItems.Count;
            bool bSelected = false;
            string strFirstColumn = "";
            if (nSeletedItemCount > 0)
            {
                bSelected = true;
                strFirstColumn = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 0);
            }

            if (String.IsNullOrEmpty(strFirstColumn) == false)
            {
                string strRecPath = "";
                if (bSelected == true)
                {
                    if (this.DbType != "arrive")
                        strRecPath = this.listView_records.SelectedItems[0].Text;
                }

                string strOpenStyle = "�¿���";
                if (this.LoadToExistWindow == true)
                    strOpenStyle = "�Ѵ򿪵�";

                bool bLoadToItemWindow = this.LoadToItemWindow;

                menuItem = new MenuItem("�� [����" + this.DbTypeCaption + "��¼·�� '" + strRecPath + "' װ�뵽" + strOpenStyle
                    + (bLoadToItemWindow == true ? DbTypeCaption + "��" : "�ֲᴰ")
                    + "](&O)");
                menuItem.DefaultItem = true;
                menuItem.Click += new System.EventHandler(this.listView_records_DoubleClick);
                if (String.IsNullOrEmpty(strRecPath) == true)
                    menuItem.Enabled = false;
                contextMenu.MenuItems.Add(menuItem);

                string strBarcode = "";
                if (bSelected == true)
                {
                    string strError = "";
                    // strBarcode = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);
                    int nRet = GetItemBarcodeOrRefID(
    this.listView_records.SelectedItems[0],
    false,
    out strBarcode,
    out strError);
                }

                bool bExistEntityForm = (this.MainForm.GetTopChildWindow<EntityForm>() != null);
                bool bExistItemInfoForm = (this.MainForm.GetTopChildWindow<ItemInfoForm>() != null);

                //
                menuItem = new MenuItem("�򿪷�ʽ(&T)");
                contextMenu.MenuItems.Add(menuItem);

                // ��һ���Ӳ˵�

                strOpenStyle = "�¿���";

                // ���ᴰ����¼·��
                MenuItem subMenuItem = new MenuItem("װ��" + strOpenStyle + this.DbTypeCaption + "�������ݼ�¼·�� '" + strRecPath + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_itemInfoForm_recPath_newly_Click);
                if (String.IsNullOrEmpty(strRecPath) == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ���ᴰ������
                // if (this.DbType == "item")
                if (string.IsNullOrEmpty(strBarcode) == false)
                {
                    subMenuItem = new MenuItem("װ��" + strOpenStyle + this.DbTypeCaption + "�������ݲ������ '" + strBarcode + "'");
                    subMenuItem.Click += new System.EventHandler(this.menu_itemInfoForm_barcode_newly_Click);
                    if (String.IsNullOrEmpty(strBarcode) == true)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);
                }

                // ���ֲᴰ����¼·��
                subMenuItem = new MenuItem("װ��" + strOpenStyle + "�ֲᴰ�����ݼ�¼·�� '" + strRecPath + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_entityForm_recPath_newly_Click);
                if (String.IsNullOrEmpty(strRecPath) == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ���ֲᴰ������
                // if (this.DbType == "item")
                if (string.IsNullOrEmpty(strBarcode) == false)
                {
                    subMenuItem = new MenuItem("װ��" + strOpenStyle + "�ֲᴰ�����ݲ������ '" + strBarcode + "'");
                    subMenuItem.Click += new System.EventHandler(this.menu_entityForm_barcode_newly_Click);
                    if (String.IsNullOrEmpty(strBarcode) == true)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);
                }

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                strOpenStyle = "�Ѵ򿪵�";

                // ���ᴰ����¼·��
                subMenuItem = new MenuItem("װ��" + strOpenStyle + this.DbTypeCaption + "�������ݼ�¼·�� '" + strRecPath + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_itemInfoForm_recPath_exist_Click);
                if (String.IsNullOrEmpty(strRecPath) == true
                    || bExistItemInfoForm == false)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ���ᴰ������
                //if (this.DbType == "item")
                if (string.IsNullOrEmpty(strBarcode) == false)
                {
                    subMenuItem = new MenuItem("װ��" + strOpenStyle + this.DbTypeCaption + "�������ݲ������ '" + strBarcode + "'");
                    subMenuItem.Click += new System.EventHandler(this.menu_itemInfoForm_barcode_exist_Click);
                    if (String.IsNullOrEmpty(strBarcode) == true
                        || bExistItemInfoForm == false)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);
                }

                // ���ֲᴰ����¼·��
                subMenuItem = new MenuItem("װ��" + strOpenStyle + "�ֲᴰ�����ݼ�¼·�� '" + strRecPath + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_entityForm_recPath_exist_Click);
                if (String.IsNullOrEmpty(strRecPath) == true
                    || bExistEntityForm == false)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ���ֲᴰ������
//                if (this.DbType == "item")
                if (string.IsNullOrEmpty(strBarcode) == false)
                {
                    subMenuItem = new MenuItem("װ��" + strOpenStyle + "�ֲᴰ�����ݲ������ '" + strBarcode + "'");
                    subMenuItem.Click += new System.EventHandler(this.menu_entityForm_barcode_exist_Click);
                    if (String.IsNullOrEmpty(strBarcode) == true
                        || bExistEntityForm == false)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);
                }

            }

            if (String.IsNullOrEmpty(strFirstColumn) == true
                && this.listView_records.SelectedItems.Count > 0)
            {
                string strKey = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);

                menuItem = new MenuItem("���� '" + strKey + "' (&S)");
                menuItem.DefaultItem = true;
                menuItem.Click += new System.EventHandler(this.listView_records_DoubleClick);
                contextMenu.MenuItems.Add(menuItem);

                menuItem = new MenuItem("���¿���" + this.DbTypeCaption + "��ѯ���� ���� '" + strKey + "' (&N)");
                menuItem.Click += new System.EventHandler(this.listView_searchKeysAtNewWindow_Click);
                contextMenu.MenuItems.Add(menuItem);
            }

            // // //

#if NOOOOOOOOOOO
            menuItem = new MenuItem("���ݲ������ '" + strBarcode + "' װ�뵽"+strOpenStyle+"�ֲᴰ(&E)");
            menuItem.Click += new System.EventHandler(this.menu_loadToEntityFormByBarcode_Click);
            if (String.IsNullOrEmpty(strBarcode) == true)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("���ݲ��¼·�� '" + strRecPath + "' װ�뵽"+strOpenStyle+"ʵ�崰(&P)");
            menuItem.Click += new System.EventHandler(this.menu_loadToItemInfoFormByRecPath_Click);
            if (String.IsNullOrEmpty(strRecPath) == true)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("���ݲ������ '" + strBarcode + "' װ�뵽"+strOpenStyle+"ʵ�崰(&B)");
            menuItem.Click += new System.EventHandler(this.menu_loadToItemInfoFormByBarcode_Click);
            if (String.IsNullOrEmpty(strBarcode) == true)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

#endif

            /*
            int nPathItemCount = 0;
            int nKeyItemCount = 0;
            GetSelectedItemCount(out nPathItemCount,
                out nKeyItemCount);
             * */
            int nPathItemCount = nSeletedItemCount;
            if (nSeletedItemCount > 0 && String.IsNullOrEmpty(strFirstColumn) == true)
                nPathItemCount = -1;    // ��ʾ�����

            if (contextMenu.MenuItems.Count > 0)
            {
                // ---
                menuItem = new MenuItem("-");
                contextMenu.MenuItems.Add(menuItem);
            }

            menuItem = new MenuItem("����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_cutToClipboard_Click);
            if (nSeletedItemCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("����(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copyToClipboard_Click);
            if (nSeletedItemCount == 0)
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


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            bool bLooping = (stop != null && stop.State == 0);    // 0 ��ʾ���ڴ���

            {
                menuItem = new MenuItem("����(&F)");
                contextMenu.MenuItems.Add(menuItem);

                MenuItem subMenuItem = null;

                if (this.DbType == "item")
                {
#if NO
                    subMenuItem = new MenuItem("�����޸Ĳ��¼ [" + this.listView_records.SelectedItems.Count.ToString() + "] (&Q)");
                    subMenuItem.Click += new System.EventHandler(this.menu_quickChangeRecords_Click);
                    if (this.listView_records.SelectedItems.Count == 0 || bLooping == true)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);
#endif

                    subMenuItem = new MenuItem("������ȡ�� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&C)");
                    subMenuItem.Click += new System.EventHandler(this.menu_createCallNumber_Click);
                    if (this.listView_records.SelectedItems.Count == 0 || bLooping == true)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);

                    // ---
                    subMenuItem = new MenuItem("-");
                    menuItem.MenuItems.Add(subMenuItem);

                    subMenuItem = new MenuItem("ģ����� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&B)");
                    subMenuItem.Click += new System.EventHandler(this.menu_borrow_Click);
                    if (this.listView_records.SelectedItems.Count == 0 || bLooping == true)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);

                    subMenuItem = new MenuItem("ģ�⻹�� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&R)");
                    subMenuItem.Click += new System.EventHandler(this.menu_return_Click);
                    if (this.listView_records.SelectedItems.Count == 0 || bLooping == true)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);
                }

                if (this.DbType == "order")
                {
                    subMenuItem = new MenuItem("��ӡ���� (&O)");
                    subMenuItem.Click += new System.EventHandler(this.menu_printOrderForm_Click);
                    if (this.listView_records.SelectedItems.Count == 0 || bLooping == true)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);

                    subMenuItem = new MenuItem("��ӡ����[�������] (&O)");
                    subMenuItem.Click += new System.EventHandler(this.menu_printOrderFormAccept_Click);
                    if (this.listView_records.SelectedItems.Count == 0 || bLooping == true)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);

                    subMenuItem = new MenuItem("��ӡ���յ� (&A)");
                    subMenuItem.Click += new System.EventHandler(this.menu_printAcceptForm_Click);
                    if (this.listView_records.SelectedItems.Count == 0 || bLooping == true)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);

                    subMenuItem = new MenuItem("��ӡ��ѯ�� (&C)");
                    subMenuItem.Click += new System.EventHandler(this.menu_printClaimForm_Click);
                    if (this.listView_records.SelectedItems.Count == 0 || bLooping == true)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);

                }
            }


            // if (this.DbType == "item" || this.DbType == "order")
            {
                menuItem = new MenuItem("������(&B)");
                contextMenu.MenuItems.Add(menuItem);

                MenuItem subMenuItem = null;

                {
                    subMenuItem = new MenuItem("�����޸�" + this.DbTypeCaption + "��¼ [" + this.listView_records.SelectedItems.Count.ToString() + "] (&Q)");
                    subMenuItem.Click += new System.EventHandler(this.menu_quickChangeItemRecords_Click);
                    if (this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                        subMenuItem.Enabled = false;
                    menuItem.MenuItems.Add(subMenuItem);
                }

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("ִ�� C# �ű� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&M)");
                subMenuItem.Click += new System.EventHandler(this.menu_quickMarcQueryRecords_Click);
                if (this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

#if NO
                subMenuItem = new MenuItem("�ڴ��н����޸� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&C)");
                subMenuItem.Click += new System.EventHandler(this.menu_acceptSelectedChangedRecords_Click);
                if (this.m_nChangedCount == 0 || this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);
#endif

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

                subMenuItem = new MenuItem("�����µ� C# �ű��ļ� (&C)");
                subMenuItem.Click += new System.EventHandler(this.menu_createMarcQueryCsFile_Click);
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("ɾ��" + this.DbTypeCaption + "��¼ [" + this.listView_records.SelectedItems.Count.ToString() + "] (&D)");
                subMenuItem.Click += new System.EventHandler(this.menu_deleteSelectedRecords_Click);
                if (this.listView_records.SelectedItems.Count == 0
                    || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                menuItem = null;
            }


            MenuItem menuItemExport = new MenuItem("����(&X)");
            contextMenu.MenuItems.Add(menuItemExport);

            {
                MenuItem subMenuItem = null;

                if (this.DbType == "item")
                {
                    subMenuItem = new MenuItem("��������ļ� [" + (nPathItemCount == -1 ? "?" : nPathItemCount.ToString()) + "] (&B)...");
                    subMenuItem.Click += new System.EventHandler(this.menu_exportBarcodeFile_Click);
                    if (nPathItemCount == 0)
                        subMenuItem.Enabled = false;
                    menuItemExport.MenuItems.Add(subMenuItem);
                }

                subMenuItem = new MenuItem("����¼·���ļ� [" + (nPathItemCount == -1 ? "?" : nPathItemCount.ToString()) + "] (&S)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveToRecordPathFile_Click);
                if (nPathItemCount == 0)
                    subMenuItem.Enabled = false;
                menuItemExport.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("���ı��ļ� [" + nSeletedItemCount.ToString() + "] (&T)...");
                subMenuItem.Click += new System.EventHandler(this.menu_exportTextFile_Click);
                if (nSeletedItemCount == 0)
                    subMenuItem.Enabled = false;
                menuItemExport.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("�� Excel �ļ� [" + nSeletedItemCount.ToString() + "] (&E)...");
                subMenuItem.Click += new System.EventHandler(this.menu_exportExcelFile_Click);
                if (nSeletedItemCount == 0)
                    subMenuItem.Enabled = false;
                menuItemExport.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItemExport.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("װ����Ŀ��ѯ����������������Ŀ��¼ [" + (nPathItemCount == -1 ? "?" : nPathItemCount.ToString()) + "] (&B)...");
                subMenuItem.Click += new System.EventHandler(this.menu_exportToBiblioSearchForm_Click);
                if (nPathItemCount == 0)
                    subMenuItem.Enabled = false;
                menuItemExport.MenuItems.Add(subMenuItem);


                subMenuItem = new MenuItem("������������Ŀ��¼·���鲢�󵼳���(��Ŀ��)��¼·���ļ� [" + (nPathItemCount == -1 ? "?" : nPathItemCount.ToString()) + "] (&B)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveToBiblioRecordPathFile_Click);
                if (nPathItemCount == 0)
                    subMenuItem.Enabled = false;
                menuItemExport.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("������������Ŀ��¼������ MARC(ISO2709)�ļ� [" + (nPathItemCount == -1 ? "?" : nPathItemCount.ToString()) + "] (&M)...");
                subMenuItem.Click += new System.EventHandler(this.menu_saveBiblioRecordToMarcFile_Click);
                if (nPathItemCount == 0)
                    subMenuItem.Enabled = false;
                menuItemExport.MenuItems.Add(subMenuItem);

                if (this.DbType == "order")
                {
                    // ---
                    subMenuItem = new MenuItem("-");
                    menuItemExport.MenuItems.Add(subMenuItem);

                    subMenuItem = new MenuItem("�������������ղ��¼·��������(��)��¼·���ļ� [" + (nPathItemCount == -1 ? "?" : nPathItemCount.ToString()) + "] (&I)...");
                    subMenuItem.Click += new System.EventHandler(this.menu_saveToAcceptItemRecordPathFile_Click);
                    if (nPathItemCount == 0)
                        subMenuItem.Enabled = false;
                    menuItemExport.MenuItems.Add(subMenuItem);
                }

                if (this.DbType == "item")
                {
                    // ---
                    subMenuItem = new MenuItem("-");

                    menuItemExport.MenuItems.Add(subMenuItem); subMenuItem = new MenuItem("װ����߲�ѯ���������߼�¼ [" + (nPathItemCount == -1 ? "?" : nPathItemCount.ToString()) + "] (&B)...");
                    subMenuItem.Click += new System.EventHandler(this.menu_exportToReaderSearchForm_Click);
                    if (nPathItemCount == 0)
                        subMenuItem.Enabled = false;
                    menuItemExport.MenuItems.Add(subMenuItem);

                    menuItemExport.MenuItems.Add(subMenuItem); subMenuItem = new MenuItem("����� Excel �ļ��������߼�¼ [" + (nPathItemCount == -1 ? "?" : nPathItemCount.ToString()) + "] (&R)...");
                    subMenuItem.Click += new System.EventHandler(this.menu_exportToReaderExcelFile_Click);
                    if (nPathItemCount == 0)
                        subMenuItem.Enabled = false;
                    menuItemExport.MenuItems.Add(subMenuItem);

                }
            }

            MenuItem menuItemImport = new MenuItem("����(&I)");
            contextMenu.MenuItems.Add(menuItemImport);

            {
                MenuItem subMenuItem = new MenuItem("�Ӽ�¼·���ļ��е���(&I)...");
                subMenuItem.Click += new System.EventHandler(this.menu_importFromRecPathFile_Click);
                menuItemImport.MenuItems.Add(subMenuItem);

                if (this.DbType == "item")
                {
                    // ---
                    subMenuItem = new MenuItem("-");
                    menuItemImport.MenuItems.Add(subMenuItem);

                    subMenuItem = new MenuItem("��������ļ��е���(&R)...");
                    subMenuItem.Click += new System.EventHandler(this.menu_importFromBarcodeFile_Click);
                    menuItemImport.MenuItems.Add(subMenuItem);
                }
            }

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�Ƴ� [" + nSeletedItemCount.ToString() + "] (&D)");
            menuItem.Click += new System.EventHandler(this.menu_removeSelectedItems_Click);
            if (nSeletedItemCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("��������б�(&C)");
            menuItem.Click += new System.EventHandler(this.menu_clearList_Click);
            if (nSeletedItemCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ˢ�� [" + nSeletedItemCount.ToString() + "] (&R)");
            menuItem.Click += new System.EventHandler(this.menu_refreshSelectedItems_Click);
            if (nSeletedItemCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ˢ����ĿժҪ [" + nSeletedItemCount.ToString() + "] (&R)");
            menuItem.Click += new System.EventHandler(this.menu_refreshSelectedItemsBiblioSummary_Click);
            if (nSeletedItemCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.listView_records, new Point(e.X, e.Y));
        }

        void menu_borrow_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            nRet = DoCirculation("borrow", out strError);
            if (nRet == -1)
                goto ERROR1;
            MessageBox.Show(this, "������ " + nRet.ToString() + " �����¼");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // TODO: ��ʾ����ķѵ�ʱ��
        void menu_return_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            nRet = DoCirculation("return", out strError);
            if (nRet == -1)
                goto ERROR1;
            MessageBox.Show(this, "������ " + nRet.ToString() + " �����¼");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ������ͨ����
        int DoCirculation(string strAction,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            Debug.Assert(this.DbType == "item", "");

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ���������������";
                return -1;
            }

            if (stop != null && stop.State == 0)    // 0 ��ʾ���ڴ���
            {
                strError = "Ŀǰ�г��������ڽ��У��޷����н�����߻���Ĳ���";
                return -1;
            }

            string strOperName = "";
            if (strAction == "borrow")
                strOperName = "����";
            else if (strAction == "return")
                strOperName = "����";
            else
            {
                strError = "δ֪�� strAction ֵ '"+strAction+"'";
                return -1;
            }

            int nCount = 0;

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ���" + strOperName + "���� ...");
            stop.BeginLoop();

            this.EnableControls(false);
            try
            {

                // �����Ҫ����ʹ��¼һ�Σ��Ա�۲쵱ǰ�û���Ȩ��
                if (string.IsNullOrEmpty(this.Channel.Rights) == true)
                {
                    string strValue = "";
                    long lRet = Channel.GetSystemParameter(stop,
                        "library",
                        "name",
                        out strValue,
                        out strError);
                }

                // ���ǰ��Ȩ��
                if (StringUtil.IsInList("client_simulateborrow", this.Channel.Rights) == false)
                {
                    strError = "��ǰ�û����߱� client_simulateborrow Ȩ�ޣ��޷�����ģ��" + strOperName + "�Ĳ���";
                    return -1;
                }

                // ��һ���µĿ�ݳ��ɴ�
                QuickChargingForm form = new QuickChargingForm();
                form.MdiParent = this.MainForm;
                form.MainForm = this.MainForm;
                form.Show();

                string strReaderBarcode = "";

                if (strAction == "borrow")
                {
                    strReaderBarcode = InputDlg.GetInput(
         this,
         "������" + strOperName,
         "���������֤�����:",
         "",
         this.MainForm.DefaultFont);
                    if (strReaderBarcode == null)
                    {
                        form.Close();
                        strError = "�û���������";
                        return -1;
                    }

                    form.SmartFuncState = FuncState.Borrow;
                    string strID = Guid.NewGuid().ToString();
                    form.AsyncDoAction(FuncState.Borrow, strReaderBarcode, strID);
                    DateTime start = DateTime.Now;
                    // �ȴ��������
                    while (true)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null && stop.State != 0)
                        {
                            strError = "�û��ж�";
                            return -1;
                        }

                        string strState = form.GetTaskState(strID);
                        if (strState != null)
                        {
                            if (strState == "error")
                            {
                                strError = "�������֤����ŵĹ�������";
                                return -1;
                            }

                            if (strState == "finish" || strState == "error")
                                break;
                        }
                        Thread.Sleep(1000);
                        TimeSpan delta = DateTime.Now - start;
                        if (delta.TotalSeconds > 30)
                        {
                            strError = "����ʱ��û�з�Ӧ����̲���������";
                            return -1;
                        }
                    }
                }
                else if (strAction == "return")
                {
                    form.SmartFuncState = FuncState.Return;
                }

                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }

                    string strRecPath = ListViewUtil.GetItemText(item, 0);

                    string strItemBarcode = "";
                    // ���� ListViewItem ���󣬻�ò�������е�����
                    nRet = GetItemBarcodeOrRefID(
                        item,
                        true,
                        out strItemBarcode,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    if (String.IsNullOrEmpty(strItemBarcode) == true)
                        continue;

                    form.AsyncDoAction(form.SmartFuncState, strItemBarcode);

                    stop.SetProgressValue(++i);

                    nCount++;
                }

                // form.Close();
                return nCount;
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

        int GetSelectedReaderBarcodes(out List<string> reader_barcodes,
    ref int nWarningLineCount,
    ref int nDupCount,
    out string strError)
        {
            strError = "";
            int nRet = 0;

            int nDelta = 0;
            if (m_bBiblioSummaryColumn == false)
                nDelta += 1;
            else
                nDelta += 2;

            stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

            reader_barcodes = new List<string>();
            Hashtable table = new Hashtable();
            int i = 0;
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                stop.SetProgressValue(i++);

                Application.DoEvents();	// ���ý������Ȩ

                if (stop != null
                    && stop.State != 0)
                {
                    strError = "�û��ж�";
                    return -1;
                }

                if (string.IsNullOrEmpty(item.Text) == true)
                    continue;

                string strReaderBarcode = "";


                // ���ָ�����͵ĵ��е�ֵ
                // ͨ�� browse �����ļ��е�������ָ��
                // return:
                //      -1  ����
                //      0   ָ������û���ҵ�
                //      1   �ҵ�
                nRet = GetTypedColumnText(
                    item,
                    "borrower",
                    nDelta,
                    out strReaderBarcode,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    // TODO: ת�����ü����������Ŀ��¼·��
                    nWarningLineCount++;
                    continue;
                }

                if (string.IsNullOrEmpty(strReaderBarcode) == true)
                    continue;

                // ȥ�أ�������ԭʼ˳��
                if (table.ContainsKey(strReaderBarcode) == false)
                {
                    reader_barcodes.Add(strReaderBarcode);
                    table[strReaderBarcode] = 1;
                }
                else
                    nDupCount++;
            }

            return 0;
        }

        int GetSelectedBiblioRecPath(out List<string> biblio_recpaths,
            ref int nWarningLineCount,
            ref int nDupCount,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

            biblio_recpaths = new List<string>();
            Hashtable table = new Hashtable();
            int i = 0;
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                stop.SetProgressValue(i++);

                Application.DoEvents();	// ���ý������Ȩ

                if (stop != null
                    && stop.State != 0)
                {
                    strError = "�û��ж�";
                    return -1;
                }

                if (string.IsNullOrEmpty(item.Text) == true)
                    continue;

                int nCol = -1;
                string strBiblioRecPath = "";
                // �����������������Ŀ��¼��·��
                // return:
                //      -1  ����
                //      0   ������ݿ�û������ parent id �����
                //      1   �ҵ�
                nRet = GetBiblioRecPath(item,
                    true,
                    out nCol,
                    out strBiblioRecPath,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    // TODO: ת�����ü����������Ŀ��¼·��
                    nWarningLineCount++;
                    continue;
                }

                // ȥ�أ�������ԭʼ˳��
                if (table.ContainsKey(strBiblioRecPath) == false)
                {
                    biblio_recpaths.Add(strBiblioRecPath);
                    table[strBiblioRecPath] = 1;
                }
                else
                    nDupCount++;
            }

            return 0;
        }

        // ����������鵽 Excel �ļ������������ĵĸ���ͼ��
        void menu_exportToReaderExcelFile_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;
            int nWarningLineCount = 0;
            int nDupCount = 0;
            string strText = "";

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫװ����߲�ѯ������";
                goto ERROR1;
            }

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����װ���¼�����߲�ѯ�� ...");
            stop.BeginLoop();

            try
            {
                List<string> reader_barcodes = new List<string>();
                nRet = GetSelectedReaderBarcodes(out reader_barcodes,
            ref nWarningLineCount,
            ref nDupCount,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nWarningLineCount > 0)
                    strText = "�� " + nWarningLineCount.ToString() + " ����Ϊ��ؿ������ʽû�ж��� type Ϊ borrower ������������";
                if (nDupCount > 0)
                {
                    if (string.IsNullOrEmpty(strText) == false)
                        strText += "\r\n\r\n";
                    strText += "���߼�¼�� " + nDupCount.ToString() + " ���ظ�������";
                }

                if (reader_barcodes.Count == 0)
                {
                    strError = "û���ҵ���صĶ��߼�¼";
                    goto ERROR1;
                }

                ReaderSearchForm form = new ReaderSearchForm();
                form.MdiParent = this.MainForm;
                form.Show();

                // return:
                //      -1  ����
                //      0   �û��ж�
                //      1   �ɹ�
                nRet = form.CreateReaderDetailExcelFile(reader_barcodes,
                    true,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

            }
            finally
            {

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;
            }

            if (string.IsNullOrEmpty(strText) == false)
                MessageBox.Show(this, strText);

            return;
        ERROR1:
            if (string.IsNullOrEmpty(strText) == false)
                MessageBox.Show(this, strText);

            MessageBox.Show(this, strError);
        }

        // װ����߲�ѯ����
        void menu_exportToReaderSearchForm_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;
            int nWarningLineCount = 0;
            int nDupCount = 0;
            string strText = "";

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫװ����߲�ѯ������";
                goto ERROR1;
            }

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����װ���¼�����߲�ѯ�� ...");
            stop.BeginLoop();

            try
            {
                List<string> reader_barcodes = new List<string>();
                nRet = GetSelectedReaderBarcodes(out reader_barcodes,
            ref nWarningLineCount,
            ref nDupCount,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nWarningLineCount > 0)
                    strText = "�� " + nWarningLineCount.ToString() + " ����Ϊ��ؿ������ʽû�ж��� type Ϊ borrower ������������";
                if (nDupCount > 0)
                {
                    if (string.IsNullOrEmpty(strText) == false)
                        strText += "\r\n\r\n";
                    strText += "���߼�¼�� " + nDupCount.ToString() + " ���ظ�������";
                }

                if (reader_barcodes.Count == 0)
                {
                    strError = "û���ҵ���صĶ��߼�¼";
                    goto ERROR1;
                }

                ReaderSearchForm form = new ReaderSearchForm();
                form.MdiParent = this.MainForm;
                form.Show();

                form.EnableControls(false);
                foreach (string barcode in reader_barcodes)
                {
                    form.AddBarcodeToBrowseList(barcode);
                }
                form.EnableControls(true);
                form.RrefreshAllItems();

            }
            finally
            {

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;
            }

            if (string.IsNullOrEmpty(strText) == false)
                MessageBox.Show(this, strText);

            return;
        ERROR1:
            if (string.IsNullOrEmpty(strText) == false)
                MessageBox.Show(this, strText);

            MessageBox.Show(this, strError);
        }


        // TODO: �Ż��󣬺͵�����Ŀ��¼·���ļ��ϲ�����
        // ����������Ŀ��¼װ����Ŀ��ѯ��
        void menu_exportToBiblioSearchForm_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫװ����Ŀ��ѯ������";
                goto ERROR1;
            }

            BiblioSearchForm form = new BiblioSearchForm();
            form.MdiParent = this.MainForm;
            form.Show();

            int nWarningLineCount = 0;
            int nDupCount = 0;

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����װ���¼����Ŀ��ѯ�� ...");
            stop.BeginLoop();

            form.EnableControls(false);
            try
            {
#if NO
                List<string> biblio_recpaths = new List<string>();
                Hashtable table = new Hashtable();
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null
                        && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    int nCol = -1;
                    string strBiblioRecPath = "";
                    // �����������������Ŀ��¼��·��
                    // return:
                    //      -1  ����
                    //      0   ������ݿ�û������ parent id �����
                    //      1   �ҵ�
                    nRet = GetBiblioRecPath(item,
                        true,
                        out nCol,
                        out strBiblioRecPath,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == 0)
                    {
                        // TODO: ת�����ü����������Ŀ��¼·��
                        nWarningLineCount++;
                        continue;
                    }

                    // ȥ�أ�������ԭʼ˳��
                    if (table.ContainsKey(strBiblioRecPath) == false)
                    {
                        biblio_recpaths.Add(strBiblioRecPath);
                        table[strBiblioRecPath] = 1;
                    }
                    else
                        nDupCount++;
                }
#endif
                List<string> biblio_recpaths = new List<string>();
                nRet = GetSelectedBiblioRecPath(out biblio_recpaths,
            ref nWarningLineCount,
            ref nDupCount,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                foreach (string path in biblio_recpaths)
                {
                    form.AddLineToBrowseList(path);
                }
            }
            finally
            {
                form.EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;
            }

            form.RefreshAllLines();

            string strText = "";
            if (nWarningLineCount > 0)
                strText = "�� " + nWarningLineCount.ToString()+ " ����Ϊ��ؿ������ʽû�а�������¼ ID �ж�������";
            if (nDupCount > 0)
            {
                if (string.IsNullOrEmpty(strText) == false)
                    strText += "\r\n\r\n";
                strText += "��Ŀ��¼�� "+nDupCount.ToString()+" ���ظ�������";
            }

            if (string.IsNullOrEmpty(strText) == false)
                MessageBox.Show(this, strText);

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ɾ����ѡ��ļ�¼
        void menu_deleteSelectedRecords_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(this,
    "ȷʵҪ�����ݿ���ɾ����ѡ���� " + this.listView_records.SelectedItems.Count.ToString() + " ��"+this.DbTypeCaption+"��¼?\r\n\r\n(OK ɾ����Cancel ȡ��)",
    "BiblioSearchForm",
    MessageBoxButtons.OKCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
            if (result == System.Windows.Forms.DialogResult.Cancel)
                return;

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                items.Add(item);
            }

            string strError = "";

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ɾ��" + this.DbTypeCaption + "��¼ ...");
            stop.BeginLoop();

            this.EnableControls(false);
            this.listView_records.Enabled = false;
            try
            {
                stop.SetProgressRange(0, items.Count);

                ListViewPatronLoader loader = new ListViewPatronLoader(this.Channel,
    stop,
    items,
    this.m_biblioTable);
                loader.DbTypeCaption = this.DbTypeCaption;


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

                    Debug.Assert(item.ListViewItem ==  items[i], "");
                    //string strRecPath = ListViewUtil.GetItemText(item, 0);

                    EntityInfo entity = new EntityInfo();

                    EntityInfo[] entities = new EntityInfo[1];
                    entities[0] = entity;
                    entity.Action = "delete";
                    entity.OldRecPath = info.RecPath;
                    entity.NewRecord = "";
                    entity.NewTimestamp = null;
                    entity.OldRecord = info.OldXml;
                    entity.OldTimestamp = info.Timestamp;
#if NO
                    entity.RefID = "";

                    if (String.IsNullOrEmpty(entity.RefID) == true)
                        entity.RefID = BookItem.GenRefID();
#endif

                    stop.SetMessage("����ɾ��"+this.DbTypeCaption+"��¼ " + info.RecPath);

                    string strBiblioRecPath = "";
                    EntityInfo[] errorinfos = null;

                    long lRet = 0;

                    if (this.DbType == "item")
                    {
                        lRet = Channel.SetEntities(
                             stop,
                             strBiblioRecPath,
                             entities,
                             out errorinfos,
                             out strError);
                    }
                    else if (this.DbType == "order")
                    {
                        lRet = Channel.SetOrders(
        stop,
        strBiblioRecPath,
        entities,
        out errorinfos,
        out strError);
                    }
                    else if (this.DbType == "issue")
                    {
                        lRet = Channel.SetIssues(
        stop,
        strBiblioRecPath,
        entities,
        out errorinfos,
        out strError);
                    }
                    else if (this.DbType == "comment")
                    {
                        lRet = Channel.SetComments(
        stop,
        strBiblioRecPath,
        entities,
        out errorinfos,
        out strError);
                    }
                    else
                    {
                        strError = "δ֪���������� '" + this.DbType + "'";
                        goto ERROR1;
                    }

                    if (lRet == -1)
                        goto ERROR1;
                    if (errorinfos != null)
                    {
                        foreach (EntityInfo error in errorinfos)
                        {
                            if (error.ErrorCode != ErrorCodeValue.NoError)
                                strError += error.ErrorInfo;
                            goto ERROR1;
                        }
                    }

                    stop.SetProgressValue(i);

                    this.listView_records.Items.Remove(item.ListViewItem);
                    i++;
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
                this.listView_records.Enabled = true;
            }

            MessageBox.Show(this, "�ɹ�ɾ��" + this.DbTypeCaption + "��¼ " + items.Count + " ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        // �����޸ļ�¼
        void menu_quickChangeItemRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            nRet = QuickChangeItemRecords(out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet != 0)
                MessageBox.Show(this, strError);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

#if NO
        // �����޸ļ�¼
        void menu_quickChangeItemRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;
            // bool bSkipUpdateBrowse = false; // �Ƿ�Ҫ�������������

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ�����޸ĵ�"+this.DbTypeCaption+"��¼����";
                goto ERROR1;
            }

            List<OneAction> actions = null;
            XmlDocument cfg_dom = null;

            if (this.DbType == "item"
                || this.DbType == "order"
                || this.DbType == "issue"
                || this.DbType == "comment")
            {
                ChangeItemActionDialog dlg = new ChangeItemActionDialog();
                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.DbType = this.DbType;
                dlg.Text = "�����޸�" + this.DbTypeCaption + "��¼ -- ��ָ����������";
                dlg.MainForm = this.MainForm;
                dlg.GetValueTable -= new GetValueTableEventHandler(dlg_GetValueTable);
                dlg.GetValueTable += new GetValueTableEventHandler(dlg_GetValueTable);

                this.MainForm.AppInfo.LinkFormState(dlg, "itemsearchform_quickchange"+this.DbType+"dialog_state");
                dlg.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(dlg);

                if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return;

                actions = dlg.Actions;
                cfg_dom = dlg.CfgDom;
            }

            DateTime now = DateTime.Now;

            // TODO: ���һ�£������Ƿ�һ���޸Ķ�����û��
            this.MainForm.OperHistory.AppendHtml("<div class='debug begin'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ��ʼִ�п����޸�" + this.DbTypeCaption + "��¼</div>");

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����޸�" + this.DbTypeCaption + "��¼ ...");
            stop.BeginLoop();

            EnableControls(false);
            this.listView_records.Enabled = false;

            int nProcessCount = 0;
            int nChangedCount = 0;
            try
            {
                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                List<ListViewItem> items = new List<ListViewItem>();
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    if (string.IsNullOrEmpty(item.Text) == true)
                        continue;

                    items.Add(item);
                }

                ListViewPatronLoader loader = new ListViewPatronLoader(this.Channel,
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

                    this.MainForm.OperHistory.AppendHtml("<div class='debug recpath'>" + HttpUtility.HtmlEncode(info.RecPath) + "</div>");

                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(info.OldXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "װ��XML��DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    string strDebugInfo = "";
                    if (this.DbType == "item"
                || this.DbType == "order"
                || this.DbType == "issue"
                || this.DbType == "comment")
                    {
                        // �޸�һ��������¼ XmlDocument
                        // return:
                        //      -1  ����
                        //      0   û��ʵ�����޸�
                        //      1   �������޸�
                        nRet = ModifyOrderRecord(
                            cfg_dom,
                            actions,
                            ref dom,
                            now,
                            out strDebugInfo,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }

                    this.MainForm.OperHistory.AppendHtml("<div class='debug normal'>" + HttpUtility.HtmlEncode(strDebugInfo).Replace("\r\n", "<br/>") + "</div>");

                    nProcessCount++;

                    if (nRet == 1)
                    {
                        string strXml = dom.OuterXml;
                        if (info != null)
                        {
                            if (string.IsNullOrEmpty(info.NewXml) == true)
                                this.m_nChangedCount++;
                            info.NewXml = strXml;
                        }

                        item.ListViewItem.BackColor = SystemColors.Info;
                        item.ListViewItem.ForeColor = SystemColors.InfoText;
                    }

                    i++;
                    nChangedCount++;
                }
            }
            finally
            {
                EnableControls(true);
                this.listView_records.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.MainForm.OperHistory.AppendHtml("<div class='debug end'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ���������޸�" + this.DbTypeCaption + "��¼</div>");
            }

            DoViewComment(false);
            MessageBox.Show(this, "�޸�" + this.DbTypeCaption + "��¼ " + nChangedCount.ToString() + " �� (������ " + nProcessCount.ToString() + " ��)\r\n\r\n(ע���޸Ĳ�δ�Զ����档���ڹ۲�ȷ�Ϻ�ʹ�ñ�������޸ı����" + this.DbTypeCaption + "��)");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void dlg_GetValueTable(object sender, GetValueTableEventArgs e)
        {
            string strError = "";
            string[] values = null;
            int nRet = MainForm.GetValueTable(e.TableName,
                e.DbName,
                out values,
                out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
            e.values = values;
        }
#endif




#if NO
        // �޸�һ��������¼ XmlDocument
        // return:
        //      -1  ����
        //      0   û��ʵ�����޸�
        //      1   �������޸�
        int ModifyOrderRecord(
            List<OneAction> actions,
            ref XmlDocument dom,
            DateTime now,
            out string strDebugInfo,
            out string strError)
        {
            strError = "";
            strDebugInfo = "";

            bool bChanged = false;

            StringBuilder debug = new StringBuilder(4096);

            // state
            string strStateAction = this.MainForm.AppInfo.GetString(
                "change_order_param",
                "state",
                "<���ı�>");
            if (strStateAction != "<���ı�>")
            {
                string strState = DomUtil.GetElementText(dom.DocumentElement,
                    "state");

                if (strStateAction == "<������>")
                {
                    string strAdd = this.MainForm.AppInfo.GetString(
                "change_order_param",
                "state_add",
                "");
                    string strRemove = this.MainForm.AppInfo.GetString(
            "change_order_param",
            "state_remove",
            "");

                    string strOldState = strState;

                    if (String.IsNullOrEmpty(strAdd) == false)
                        StringUtil.SetInList(ref strState, strAdd, true);
                    if (String.IsNullOrEmpty(strRemove) == false)
                        StringUtil.SetInList(ref strState, strRemove, false);

                    if (strOldState != strState)
                    {
                        DomUtil.SetElementText(dom.DocumentElement,
                            "state",
                            strState);
                        bChanged = true;

                        debug.Append("<state> '" + strOldState + "' --> '" + strState + "'\r\n");
                    }
                }
                else
                {
                    if (strStateAction != strState)
                    {
                        DomUtil.SetElementText(dom.DocumentElement,
                            "state",
                            strStateAction);
                        bChanged = true;

                        debug.Append("<state> '" + strState + "' --> '" + strStateAction + "'\r\n");
                    }
                }
            }

            // �����ֶ�
            string strFieldName = this.MainForm.AppInfo.GetString(
"change_order_param",
"field_name",
"<��ʹ��>");

            if (strFieldName != "<��ʹ��>")
            {
                string strFieldValue = this.MainForm.AppInfo.GetString(
    "change_order_param",
    "field_value",
    "");
                if (strFieldName == "���")
                {
                    ChangeField(ref dom,
            "index",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "��Ŀ��")
                {
                    ChangeField(ref dom,
            "catalogNo",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "����")
                {
                    ChangeField(ref dom,
            "seller",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "������Դ")
                {
                    ChangeField(ref dom,
            "source",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "ע��")
                {
                    ChangeField(ref dom,
            "comment",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "ʱ�䷶Χ")
                {
                    ChangeField(ref dom,
            "range",
            strFieldValue,
            ref debug,
            ref bChanged);
                }



                if (strFieldName == "��������")
                {
                    ChangeField(ref dom,
            "issueCount",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "������")
                {
                    ChangeField(ref dom,
            "copy",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "����")
                {
                    ChangeField(ref dom,
            "price",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "�ܼ۸�")
                {
                    ChangeField(ref dom,
            "totalPrice",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "����ʱ��")
                {
                    ChangeField(ref dom,
            "orderTime",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "������")
                {
                    ChangeField(ref dom,
            "orderID",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "�ݲط���")
                {
                    ChangeField(ref dom,
            "distribute",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "���")
                {
                    ChangeField(ref dom,
            "class",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "������ַ")
                {
                    ChangeField(ref dom,
            "sellerAddress",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "���κ�")
                {
                    ChangeField(ref dom,
            "batchNo",
            strFieldValue,
            ref debug,
            ref bChanged);
                }
            }

            strDebugInfo = debug.ToString();

            if (bChanged == true)
                return 1;

            return 0;
        }

#endif

        // ���ô�ӡ�������� [�������]
        void menu_printOrderFormAccept_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strFilename = PathUtil.MergePath(this.MainForm.DataDir, "~orderrecpath.txt");
            bool bAppend = false;   // ��ϣ������ѯ��׷�ӵĶԻ���ֱ�Ӹ���
            // ������������¼·���ļ�
            // return:
            //      -1  ����
            //      0   ��������
            //      >0  �����ɹ����������Ѿ�����������
            int nRet = ExportToRecPathFile(
                strFilename,
                ref bAppend,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0)
            {
                strError = "��ָ���Ķ�����¼��û�а����κ������յĲ��¼��Ϣ";
                goto ERROR1;
            }

            PrintOrderForm form = new PrintOrderForm();
            // form.MainForm = this.MainForm;
            form.MdiParent = this.MainForm;
            form.Show();

            form.AcceptCondition = true;

            // ��(�����յ�)���¼·���ļ�װ��
            // return:
            //      -1  ����
            //      0   ����
            //      1   װ�سɹ�
            nRet = form.LoadFromOrderRecPathFile(
                true,
                strFilename,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        // ���ô�ӡ��������
        void menu_printOrderForm_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strFilename = PathUtil.MergePath(this.MainForm.DataDir, "~orderrecpath.txt");
            bool bAppend = false;   // ��ϣ������ѯ��׷�ӵĶԻ���ֱ�Ӹ���
            // ������������¼·���ļ�
            // return:
            //      -1  ����
            //      0   ��������
            //      >0  �����ɹ����������Ѿ�����������
            int nRet = ExportToRecPathFile(
                strFilename,
                ref bAppend,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0)
            {
                strError = "��ָ���Ķ�����¼��û�а����κ������յĲ��¼��Ϣ";
                goto ERROR1;
            }

            PrintOrderForm form = new PrintOrderForm();
            // form.MainForm = this.MainForm;
            form.MdiParent = this.MainForm;
            form.Show();

            form.AcceptCondition = false;

            // ��(�����յ�)���¼·���ļ�װ��
            // return:
            //      -1  ����
            //      0   ����
            //      1   װ�سɹ�
            nRet = form.LoadFromOrderRecPathFile(
                true,
                strFilename,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

                // ���ô�ӡ��ѯ������
        void menu_printClaimForm_Click(object sender, EventArgs e)
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
                string strOrderDbName = Global.GetDbName(strFirstRecPath);
                if (string.IsNullOrEmpty(strOrderDbName) == false)
                {
                    string strBiblioDbName = this.MainForm.GetBiblioDbNameFromOrderDbName(strOrderDbName);
                    strIssueDbName = this.MainForm.GetIssueDbName(strBiblioDbName);
                }
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
            form.SetOrderRecPaths(recpaths);
            form.EnableControls(true);

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        // ���ô�ӡ���յ�����
        void menu_printAcceptForm_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strFilename = PathUtil.MergePath(this.MainForm.DataDir, "~itemrecpath.txt");
            bool bAppend = false;   // ��ϣ������ѯ��׷�ӵĶԻ���ֱ�Ӹ���
            // ������(�����յ�)���¼·���ļ�
            // return:
            //      -1  ����
            //      0   ��������
            //      >0  �����ɹ����������Ѿ�����������
            int nRet = ExportToItemRecPathFile(
                strFilename,
                ref bAppend,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0)
            {
                strError = "��ָ���Ķ�����¼��û�а����κ������յĲ��¼��Ϣ";
                goto ERROR1;
            }

            PrintAcceptForm form = new PrintAcceptForm();
            // form.MainForm = this.MainForm;
            form.MdiParent = this.MainForm;
            form.Show();

            // ��(�����յ�)���¼·���ļ�װ��
            // return:
            //      -1  ����
            //      0   ����
            //      1   װ�سɹ�
            nRet = form.LoadFromItemRecPathFile(
                true,
                strFilename,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ������(�����յ�)���¼·���ļ�
        void menu_saveToAcceptItemRecordPathFile_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ����Ĳ��¼·���ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = this.ExportItemRecPathFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "���¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.ExportItemRecPathFilename = dlg.FileName;

            bool bAppend = true;    // ϣ�����ֶԻ���ѯ��׷�ӣ�����ļ��Ѿ����ڵĻ�
            // ������(�����յ�)���¼·���ļ�
            // return:
            //      -1  ����
            //      0   ��������
            //      >0  �����ɹ����������Ѿ�����������
            nRet = ExportToItemRecPathFile(
                this.ExportItemRecPathFilename,
                ref bAppend,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string strExportStyle = "����";
            if (bAppend == true)
                strExportStyle = "׷��";

            this.MainForm.StatusBarMessage = "���¼·�� " + nRet.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportItemRecPathFilename;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ������(�����յ�)���¼·���ļ�
        // parameters:
        //      bAppend [in]������ܵĻ��Ƿ���׷�ӵ��Ѿ����ڵ��ļ�ĩβ [out]ʵ�ʲ��õ��Ƿ�Ϊ׷�ӷ�ʽ
        // return:
        //      -1  ����
        //      0   ��������
        //      >0  �����ɹ����������Ѿ�����������
        int ExportToItemRecPathFile(
            string strFilename,
            ref bool bAppend,
            out string strError)
        {
            strError = "";
            int nCount = 0;

            if (File.Exists(strFilename) == true && bAppend == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "���¼·���ļ� '" + strFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel ��������)",
                    this.DbType + "SearchForm",
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

            // �����ļ�
            StreamWriter sw = new StreamWriter(strFilename,
                bAppend,	// append
                System.Text.Encoding.UTF8);
            try
            {

                stop.Style = StopStyle.EnableHalfStop;
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڵ��������յĲ��¼·�� ...");
                stop.BeginLoop();

                this.EnableControls(false);
                try
                {
                    foreach (ListViewItem item in this.listView_records.SelectedItems)
                    {
                        if (String.IsNullOrEmpty(item.Text) == true)
                            continue;

                        List<string> itemrecpaths = null;

                        // ���ݶ�����¼·����������������¼�����һ��������������ղ��¼·��
                        // parameters:
                        // return: 
                        //      -1  ����
                        //      1   �ɹ�
                        int nRet = LoadOrderItem(item.Text,
                out itemrecpaths,
                out strError);
                        if (nRet == -1)
                            return -1;

                        foreach (string strPath in itemrecpaths)
                        {
                            sw.WriteLine(strPath);
                            nCount++;
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

            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }


            return nCount;
        }

        // ���ݶ�����¼·����������������¼�����һ��������������ղ��¼·��
        // parameters:
        //      strRecPath  �������¼·��
        // return: 
        //      -1  ����
        //      1   �ɹ�
        int LoadOrderItem(string strRecPath,
            out List<string> itemrecpaths,
            out string strError)
        {
            strError = "";
            itemrecpaths = new List<string>();
            int nRet = 0;

            string strOrderXml = "";
            string strBiblioText = "";

            string strOutputOrderRecPath = "";
            string strOutputBiblioRecPath = "";

            byte[] item_timestamp = null;

            long lRet = Channel.GetOrderInfo(
                stop,
                "@path:" + strRecPath,
                // "",
                "xml",
                out strOrderXml,
                out strOutputOrderRecPath,
                out item_timestamp,
                "recpath",
                out strBiblioText,
                out strOutputBiblioRecPath,
                out strError);
            if (lRet == -1 || lRet == 0)
            {
                strError = "��ȡ������¼ " + strRecPath + " ʱ����: " + strError;
                return -1;
            }

            // ����һ������xml��¼��ȡ���й���Ϣ
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strOrderXml);
            }
            catch (Exception ex)
            {
                strError = "������¼XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            // �۲충�����ǲ����ڿ�������
            // return:
            //      -1  ���Ƕ�����
            //      0   ͼ������
            //      1   �ڿ�����
            nRet = this.MainForm.IsSeriesTypeFromOrderDbName(Global.GetDbName(strRecPath));
            if (nRet == -1)
            {
                strError = "IsSeriesTypeFromOrderDbName() '" + strRecPath + "' error";
                return -1;
            }


            List<string> distributes = new List<string>();

            if (nRet == 1)
            {
                string strRefID = DomUtil.GetElementText(dom.DocumentElement, "refID");
                if (string.IsNullOrEmpty(strRefID) == true)
                {
                    strError = "������¼ '" + strRecPath + "' ��û��<refID>Ԫ��";
                    return -1;
                }

                string strBiblioDbName = this.MainForm.GetBiblioDbNameFromOrderDbName(Global.GetDbName(strRecPath));
                string strIssueDbName = this.MainForm.GetIssueDbName(strBiblioDbName);

                // ������ڿ��Ķ����⣬����Ҫͨ��������¼��refid����ڼ�¼�����ڼ�¼�в��ܵõ��ݲط�����Ϣ
                string strOutputStyle = "";
                lRet = Channel.SearchIssue(stop,
    strIssueDbName, // "<ȫ��>",
    strRefID,
    -1,
    "�����ο�ID",
    "exact",
    this.Lang,
    "tempissue",
    "",
    strOutputStyle,
    out strError);
                if (lRet == -1 || lRet == 0)
                {
                    strError = "���� �����ο�ID Ϊ " + strRefID + " ���ڼ�¼ʱ����: " + strError;
                    return -1;
                }

                long lHitCount = lRet;
                long lStart = 0;
                long lCount = lHitCount;
                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                // ��ȡ���н��
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }

                    lRet = Channel.GetSearchResult(
                        stop,
                        "tempissue",
                        lStart,
                        lCount,
                        "id",
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "��ȡ�����ʱ����: " + strError;
                        return -1;
                    }
                    if (lRet == 0)
                    {
                        strError = "��ȡ�����ʱ����: lRet = 0";
                        return -1;
                    }

                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        DigitalPlatform.CirculationClient.localhost.Record searchresult = searchresults[i];

                        string strIssueRecPath = searchresult.Path;

                        string strIssueXml = "";
                        string strOutputIssueRecPath = "";

                        lRet = Channel.GetIssueInfo(
    stop,
    "@path:" + strIssueRecPath,
                            // "",
    "xml",
    out strIssueXml,
    out strOutputIssueRecPath,
    out item_timestamp,
    "recpath",
    out strBiblioText,
    out strOutputBiblioRecPath,
    out strError);
                        if (lRet == -1 || lRet == 0)
                        {
                            strError = "��ȡ�ڼ�¼ " + strIssueRecPath + " ʱ����: " + strError;
                            return -1;
                        }

                        // ����һ���ڿ�xml��¼��ȡ���й���Ϣ
                        XmlDocument issue_dom = new XmlDocument();
                        try
                        {
                            issue_dom.LoadXml(strIssueXml);
                        }
                        catch (Exception ex)
                        {
                            strError = "�ڼ�¼ '" + strOutputIssueRecPath + "' XMLװ��DOMʱ����: " + ex.Message;
                            return -1;
                        }

                        // Ѱ�� /orderInfo/* Ԫ��
                        XmlNode nodeRoot = issue_dom.DocumentElement.SelectSingleNode("orderInfo/*[refID/text()='" + strRefID + "']");
                        if (nodeRoot == null)
                        {
                            strError = "�ڼ�¼ '" + strOutputIssueRecPath + "' ��û���ҵ�<refID>Ԫ��ֵΪ '" + strRefID + "' �Ķ������ݽڵ�...";
                            return -1;
                        }

                        string strDistribute = DomUtil.GetElementText(nodeRoot, "distribute");

                        distributes.Add(strDistribute);
                    }

                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                }
            }
            else
            {
                string strDistribute = DomUtil.GetElementText(dom.DocumentElement, "distribute");
                distributes.Add(strDistribute);
            }

            if (distributes.Count == 0)
                return 0;

            foreach (string strDistribute in distributes)
            {
                LocationCollection locations = new LocationCollection();
                nRet = locations.Build(strDistribute,
                    out strError);
                if (nRet == -1)
                    return -1;

                for (int i = 0; i < locations.Count; i++)
                {
                    Location location = locations[i];

                    if (string.IsNullOrEmpty(location.RefID) == true)
                        continue;

                    // 2012/9/4
                    string[] parts = location.RefID.Split(new char[] { '|' });
                    foreach (string text in parts)
                    {
                        string strRefID = text.Trim();
                        if (string.IsNullOrEmpty(strRefID) == true)
                            continue;

                        // ���ݲ��¼��refidװ����¼
                        string strItemXml = "";
                        string strOutputItemRecPath = "";

                        lRet = Channel.GetItemInfo(
                            stop,
                            "@refID:" + strRefID,
                            "", // "xml",
                            out strItemXml,
                            out strOutputItemRecPath,
                            out item_timestamp,
                            "recpath",
                            out strBiblioText,
                            out strOutputBiblioRecPath,
                            out strError);
                        if (lRet == -1 || lRet == 0)
                        {
                            strError = "��ȡ���¼ " + strRefID + " ʱ����: " + strError;
                        }

                        itemrecpaths.Add(strOutputItemRecPath);
                    }
                }
            }

            return 1;
        }

        void menu_createCallNumber_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            Debug.Assert(this.DbType == "item", "");

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ���������������";
                goto ERROR1;
            }

            if (stop != null && stop.State == 0)    // 0 ��ʾ���ڴ���
            {
                strError = "Ŀǰ�г��������ڽ��У��޷����д�����ȡ�ŵĲ���";
                goto ERROR1;
            }

            bool bOverwrite = false;
            {
                DialogResult result = MessageBox.Show(this,
    "�ں��漴�����еĴ�������У����Ѿ�������ȡ�ŵĲ��¼���Ƿ�Ҫ���´�����ȡ��?\r\n\r\n(Yes: Ҫ���´�����No: �����´���������������Cancel: ���ھͷ�������������)",
    this.DbType + "SearchForm",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result == System.Windows.Forms.DialogResult.Yes)
                    bOverwrite = true;
                else if (result == System.Windows.Forms.DialogResult.No)
                    bOverwrite = false;
                else
                    return;

            }

            int nCount = 0;

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڴ�����ȡ�� ...");
            stop.BeginLoop();

            this.EnableControls(false);
            try
            {

                // ��һ���µ��ֲᴰ
                EntityForm form = null;

                form = new EntityForm();

                form.MdiParent = this.MainForm;

                form.MainForm = this.MainForm;
                form.Show();

                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                ItemSearchForm itemsearchform = null;
                bool bHideMessageBox = false;

                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
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

                    string strRecPath = ListViewUtil.GetItemText(item, 0);

                    // parameters:
                    //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   found
                    form.LoadItemByRecPath(strRecPath, true);

                    // Ϊ��ǰѡ�����������ȡ��
                    // return:
                    //      -1  ����
                    //      0   ��������
                    //      1   �Ѿ�����
                    nRet = form.EntityControl.CreateCallNumber(bOverwrite, out strError);
                    if (nRet == -1)
                        goto ERROR;

                    if (nRet == 1)
                    {
                        nCount++;
                        // form.DoSaveAll();
                        nRet = form.EntityControl.SaveItems(out strError);
                        if (nRet == -1)
                            goto ERROR;

                        nRet = RefreshBrowseLine(item,
    out strError);
                        if (nRet == -1)
                        {
                            strError = "ˢ���������ʱ����: " + strError;
                            goto ERROR;
#if NO
                            DialogResult result = MessageBox.Show(this,
                                "ˢ���������ʱ����: " + strError + "��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
                                this.DbType + "SearchForm",
                                MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button1);
                            if (result == System.Windows.Forms.DialogResult.Cancel)
                                break;
#endif
                        }
                    }

                ERROR:
                    if (nRet == -1)
                    {
                        if (itemsearchform == null)
                        {
                            Form active_mdi = this.MainForm.ActiveMdiChild;

                            itemsearchform = new ItemSearchForm();
                            itemsearchform.MdiParent = this.MainForm;
                            itemsearchform.MainForm = this.MainForm;
                            itemsearchform.Show();
                            itemsearchform.QueryWordString = "������ȡ�Ź����г���Ĳ��¼";

                            active_mdi.Activate();
                        }

                        ListViewItem new_item = itemsearchform.AddLineToBrowseList(Global.BuildLine(item));
                        ListViewUtil.ChangeItemText(new_item, 1, strError);

                        this.OutputText(strRecPath + " : " + strError, 2);

                        strError = "��Ϊ���¼ " + strRecPath + " ������ȡ��ʱ����: " + strError;

                        if (bHideMessageBox == false)
                        {
                            DialogResult result = MessageDialog.Show(this,
                                strError + "��\r\n\r\n�Ƿ�����������ļ�¼? (������ ������ֹͣ�� ֹͣ����������)",
            MessageBoxButtons.OKCancel,
            MessageBoxDefaultButton.Button1,
            "���ٳ��ִ˶Ի���",
            ref bHideMessageBox,
            new string[] { "����", "ֹͣ" });
                            if (result == System.Windows.Forms.DialogResult.Cancel)
                                goto ERROR1;
                        }
                        form.EntitiesChanged = false;
                    }

                    stop.SetProgressValue(++i);
                }

                // form.DoSaveAll();
                form.Close();
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

            MessageBox.Show(this, "������ "+nCount.ToString()+" �����¼");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        public string QueryWordString
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

#if NO
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
#endif

        // string m_strTempQuickBarcodeFilename = "";

#if NO
        void menu_quickChangeRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            Debug.Assert(this.DbType == "item", "");

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ�����޸ĵ�����";
                goto ERROR1;
            }

            if (stop != null && stop.State == 0)    // 0 ��ʾ���ڴ���
            {
                strError = "Ŀǰ�г��������ڽ��У��޷����п����޸�";
                goto ERROR1;
            }


            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����޸Ĳ��¼ ...");
            stop.BeginLoop();

            this.EnableControls(false);
            try
            {
                if (string.IsNullOrEmpty(m_strTempQuickBarcodeFilename) == true)
                {
                    m_strTempQuickBarcodeFilename = PathUtil.MergePath(this.MainForm.DataDir, "~" + Guid.NewGuid().ToString());
                }

                File.Delete(m_strTempQuickBarcodeFilename);
                using (StreamWriter sr = new StreamWriter(m_strTempQuickBarcodeFilename))
                {
                    if (stop != null)
                        stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);
                    {
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
                                stop.SetMessage("���ڹ����¼·���ļ� " + item.Text + " ...");
                                stop.SetProgressValue(i++);
                            }

                            /*
                        // ���������д���ļ�
                            string strBarcode = ListViewUtil.GetItemText(item, 1);
                            if (string.IsNullOrEmpty(strBarcode) == true)
                                continue;

                            sr.WriteLine(strBarcode);
                             * */
                            // ����¼·��д���ļ�
                            string strRecPath = ListViewUtil.GetItemText(item, 0);
                            if (string.IsNullOrEmpty(strRecPath) == true)
                                continue;

                            sr.WriteLine(strRecPath);
                        }
                    }
                }

                if (stop != null)
                {
                    stop.SetMessage("���ڵ��ÿ����޸Ĳᴰ���������� ...");
                    stop.SetProgressValue(0);
                }

                // �´�һ�������޸Ĳᴰ��
                QuickChangeEntityForm form = new QuickChangeEntityForm();
                // form.MainForm = this.MainForm;
                form.MdiParent = this.MainForm;
                form.Show();

                if (form.SetChangeParameters() == false)
                {
                    form.Close();
                    return;
                }

                // form.DoBarcodeFile(m_strTempQuickBarcodeFilename);
                // return:
                //      -1  ����
                //      0   ��������
                //      >=1 ���������
                nRet = form.DoRecPathFile(m_strTempQuickBarcodeFilename);
                form.Close();

                if (nRet == 0)
                    return;

                if (nRet == -1)
                {
                    DialogResult result = MessageBox.Show(this,
"���ּ�¼�Ѿ��������޸ģ��Ƿ���Ҫˢ�������? (OK ˢ�£�Cancel ����ˢ��)",
this.DbType + "SearchForm",
MessageBoxButtons.OKCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                    if (result == System.Windows.Forms.DialogResult.Cancel)
                        return;
                }

                if (stop != null)
                    stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                {
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
                                this.DbType + "SearchForm",
                                MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button1);
                            if (result == System.Windows.Forms.DialogResult.Cancel)
                                break;
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

                this.EnableControls(true);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif

        void menu_selectAllLines_Click(object sender, EventArgs e)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            // m_nInSelectedIndexChanged++;    // ��ֹ�¼���Ӧ
            try
            {
                this.listView_records.SelectedIndexChanged -= new System.EventHandler(this.listView_records_SelectedIndexChanged);

                ListViewUtil.SelectAllLines(this.listView_records);

                this.listView_records.SelectedIndexChanged += new System.EventHandler(this.listView_records_SelectedIndexChanged);
                listView_records_SelectedIndexChanged(null, null);
            }
            finally
            {
                // m_nInSelectedIndexChanged--;
                this.Cursor = oldCursor;
            }
        }

        // ˢ����ĿժҪ
        void menu_refreshSelectedItemsBiblioSummary_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this._listviewRecords.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫˢ�µ������";
                goto ERROR1;
            }

            int nChangedCount = 0;
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this._listviewRecords.SelectedItems)
            {
                if (string.IsNullOrEmpty(item.Text) == true)
                    continue;
                items.Add(item);

                if (IsItemChanged(item) == true)
                    nChangedCount++;
            }

            // ����δ��������ݻᶪʧ
            if (nChangedCount > 0)
            {
                DialogResult result = MessageBox.Show(this,
    "Ҫˢ�µ� " + this._listviewRecords.SelectedItems.Count.ToString() + " ���������� " + nChangedCount.ToString() + " ���޸ĺ���δ���档���ˢ�����ǣ��޸����ݻᶪʧ��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
    "ItemSearchForm",
    MessageBoxButtons.OKCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return;
            }

            m_tableSummaryColIndex.Clear();

            nRet = FillBiblioSummaryColumn(items,
                true,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            DoViewComment(false);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ˢ����ѡ����С�Ҳ�������´����ݿ���װ�������
        void menu_refreshSelectedItems_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";
            int nRet = 0;

            Debug.Assert(this.DbType == "item", "");

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
                    nRet = RefreshBrowseLine(item,
    out strError);
                    if (nRet == -1)
                    {
                        DialogResult result = MessageBox.Show(this,
                            "ˢ���������ʱ����: " + strError + "��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
                            this.DbType + "SearchForm",
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
#endif
            RrefreshSelectedItems();
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

        // ��һ���¿���ʵ���ѯ���ڼ���key
        void listView_searchKeysAtNewWindow_Click(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫ����������");
                return;
            }

            ItemSearchForm form = new ItemSearchForm();
            form.DbType = this.DbType;
            form.MdiParent = this.MainForm;
            // form.MainForm = this.MainForm;
            form.Show();

            int i = 0;
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                ItemQueryParam query = (ItemQueryParam)item.Tag;
                Debug.Assert(query != null, "");

                ItemQueryParam input_query = new ItemQueryParam();

                input_query.QueryWord = ListViewUtil.GetItemText(item, 1);
                input_query.DbNames = query.DbNames;
                input_query.From = query.From;
                input_query.MatchStyle = "��ȷһ��";

                // 2015/1/17
                if (string.IsNullOrEmpty(input_query.QueryWord) == true)
                    input_query.MatchStyle = "��ֵ";
                else
                    input_query.MatchStyle = "��ȷһ��";


                // �������м�¼(������key)
                int nRet = form.DoSearch(false, false, input_query, i == 0 ? true : false);
                if (nRet != 1)
                    break;

                i++;
            }
        }

#if NO
        void GetSelectedItemCount(out int nPathItemCount,
            out int nKeyItemCount)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            nPathItemCount = 0;
            nKeyItemCount = 0;
            for (int i = 0; i < this.listView_records.SelectedItems.Count; i++)
            {
                if (String.IsNullOrEmpty(this.listView_records.SelectedItems[i].Text) == false)
                    nPathItemCount++;
                else
                    nKeyItemCount++;
            }

            this.Cursor = oldCursor;
        }
#endif

        void menu_clearList_Click(object sender, EventArgs e)
        {
            ClearListViewItems();
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
            DateTime start_time = DateTime.Now;
            this.listView_records.SelectedIndexChanged -= new System.EventHandler(this.listView_records_SelectedIndexChanged);

            Global.CopyLinesToClipboard(this, this.listView_records, true);

            this.listView_records.SelectedIndexChanged += new System.EventHandler(this.listView_records_SelectedIndexChanged);
            listView_records_SelectedIndexChanged(null, null);
            TimeSpan delta = DateTime.Now - start_time;
            this.MainForm.StatusBarMessage = "���в��� ��ʱ " + delta.TotalSeconds.ToString() + " ��";
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

        // TODO: �Ż��ٶ�
        void menu_importFromBarcodeFile_Click(object sender, EventArgs e)
        {
            SetStatusMessage("");   // �����ǰ��������ʾ

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵�������ļ���";
            dlg.FileName = this.m_strUsedBarcodeFilename;
            dlg.Filter = "������ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.m_strUsedBarcodeFilename = dlg.FileName;

            StreamReader sr = null;
            string strError = "";

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

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ�������� ...");
            stop.BeginLoop();

            try
            {
                // �����������û����ģ������Ҫ������е������־
                ListViewUtil.ClearSortColumns(this.listView_records);


                if (this.listView_records.Items.Count > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "����ǰ�Ƿ�Ҫ������м�¼�б��е����е� " + this.listView_records.Items.Count.ToString() + " ��?\r\n\r\n(�������������µ�����н�׷���������к���)\r\n(Yes �����No �����(׷��)��Cancel ��������)",
                        this.DbType + "SearchForm",
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

                stop.SetProgressRange(0, sr.BaseStream.Length);

                List<ListViewItem> items = new List<ListViewItem>();

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

                    string strBarcode = sr.ReadLine();

                    stop.SetProgressValue(sr.BaseStream.Position);


                    if (strBarcode == null)
                        break;

                    // 


                    ListViewItem item = new ListViewItem();
                    item.Text = "";
                    // ListViewUtil.ChangeItemText(item, 1, strBarcode);

                    this.listView_records.Items.Add(item);

                    FillLineByBarcode(
                        strBarcode, item);

                    items.Add(item);
                }

                // ˢ�������
                int nRet = RefreshListViewLines(items,
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // 2014/1/15
                // ˢ����ĿժҪ
                nRet = FillBiblioSummaryColumn(items,
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                if (sr != null)
                    sr.Close();
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }



        // �Ӽ�¼·���ļ��е���
        void menu_importFromRecPathFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            int nRet = ImportFromRecPathFile(null,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        // ����ǰ����¼·�����Ѿ���ֵ
        /// <summary>
        /// ˢ��һ������еĸ�����Ϣ��
        /// Ҳ���Ǵ����ݿ������»�ȡ�����Ϣ��
        /// ��ˢ����ĿժҪ��
        /// </summary>
        /// <param name="item">Ҫˢ�µ� ListViewItem ����</param>
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
                if (this.m_bBiblioSummaryColumn == false)
                    ListViewUtil.ChangeItemText(item,
                    i + 1,
                    searchresults[0].Cols[i]);
                else
                    ListViewUtil.ChangeItemText(item,
                    i + 2,
                    searchresults[0].Cols[i]);
            }

            return 0;
        }



        // ����ѡ���������·���Ĳ����� ������������ Ϊ������ļ�
        void menu_exportBarcodeFile_Click(object sender, EventArgs e)
        {
            Debug.Assert(this.DbType == "item", "");

            string strError = "";

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ�����������ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = this.ExportBarcodeFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "������ļ� (*.txt)|*.txt|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.ExportBarcodeFilename = dlg.FileName;

            bool bAppend = true;

            if (File.Exists(this.ExportBarcodeFilename) == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ļ� '" + this.ExportBarcodeFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel ��������)",
                    this.DbType + "SearchForm",
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

            // m_tableBarcodeColIndex.Clear();
            ClearColumnIndexCache();

            // �����ļ�
            StreamWriter sw = new StreamWriter(this.ExportBarcodeFilename,
                bAppend,	// append
                System.Text.Encoding.UTF8);
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {

                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    if (String.IsNullOrEmpty(item.Text) == true)
                        continue;

#if NO
                    string strRecPath = ListViewUtil.GetItemText(item, 0);
                    // ���ݼ�¼·��������ݿ���
                    string strItemDbName = Global.GetDbName(strRecPath);
                    // �������ݿ������ ������� �к�

                    int nCol = -1;
                    object o = m_tableBarcodeColIndex[strItemDbName];
                    if (o == null)
                    {
                        ColumnPropertyCollection temp = this.MainForm.GetBrowseColumnProperties(strItemDbName);
                        nCol = temp.FindColumnByType("item_barcode");
                        if (nCol == -1)
                        {
                            // ���ʵ���û���� browse �ļ��� ������� ��
                            strError = "���棺ʵ��� '"+strItemDbName+"' �� browse �����ļ���û�ж��� type Ϊ item_barcode ���С���ע��ˢ�»��޸Ĵ������ļ�";
                            MessageBox.Show(this, strError);

                            nCol = 0;   // ����󲿷��������Ч
                        }
                        if (m_bBiblioSummaryColumn == false)
                            nCol += 1;
                        else 
                            nCol += 2;

                        m_tableBarcodeColIndex[strItemDbName] = nCol;   // ��������
                    }
                    else
                        nCol = (int)o;

                    Debug.Assert(nCol > 0, "");

                    string strBarcode = ListViewUtil.GetItemText(item, nCol);
#endif

                    string strBarcode = "";
                    // ���� ListViewItem ���󣬻�ò�������е�����
                    int nRet = GetItemBarcodeOrRefID(
                        item,
                        true,
                        out strBarcode,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (String.IsNullOrEmpty(strBarcode) == true)
                        continue;
                    sw.WriteLine(strBarcode);   // BUG!!!
                }

            }
            finally
            {
                this.Cursor = oldCursor;
                if (sw != null)
                    sw.Close();
            }

            string strExportStyle = "����";
            if (bAppend == true)
                strExportStyle = "׷��";

            this.MainForm.StatusBarMessage = "������� " + this.listView_records.SelectedItems.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportBarcodeFilename;
            return;
        ERROR1:
            MessageBox.Show(strError);
        }


        // ��ǰȱʡ�ı��뷽ʽ
        Encoding CurrentEncoding = Encoding.UTF8;

        // Ϊ�˱���ISO2709�ļ�����ļ�������
        /// <summary>
        /// ��ȡ���������ò��������һ��ʹ�ù��� ISO2709 �ļ���
        /// ���� ItemSearchForm �� BiblioSearchForm �����õ�һ�����ò���
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
        /// ��ȡ���������ò����������һ������� ISO2709 �ļ�ʱ�Ƿ�Ҫ�ڼ�¼�����س����з���
        /// ���� ItemSearchForm �� BiblioSearchForm �����õ�һ�����ò���
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
        /// ��ȡ���������ò��������һ��ʹ�ù��ı��뷽ʽ����
        /// ���� ItemSearchForm �� BiblioSearchForm �����õ�һ�����ò���
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
        /// ��ȡ���������ò��������һ��ʹ�ù��ı�Ŀ��������
        /// ���� ItemSearchForm �� BiblioSearchForm �����õ�һ�����ò���
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

        // ���ݲصص����ͱ�Ŀ�������Ķ��ձ�װ���ڴ�
        // return:
        //      -1  ����
        //      0   �ļ�������
        //      1   �ɹ�
        static int LoadRuleNameTable(string strFilename,
            out Hashtable table,
            out string strError)
        {
            strError = "";
            table = new Hashtable();

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.Load(strFilename);
            }
            catch (FileNotFoundException)
            {
                return 0;
            }
            catch (Exception ex)
            {
                strError = "װ��XML�ļ� '" + strFilename + "' ʱ��������: " + ex.Message;
                return -1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("location");
            foreach (XmlNode node in nodes)
            {
                string strLocationName = DomUtil.GetAttr(node, "name");
                string strRuleName = DomUtil.GetAttr(node, "catalogingRule");

                table[strLocationName] = strRuleName;
            }

            return 1;
        }

        // ����������Ŀ��¼���浽MARC�ļ�
        void menu_saveBiblioRecordToMarcFile_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ���������";
                goto ERROR1;
            }

            // Ϊ����Ŀ��¼·��ȥ�ط���
            Hashtable biblio_recpath_table = new Hashtable();

            Hashtable rule_name_table = null;
            bool bTableExists = false;
            // ���ݲصص����ͱ�Ŀ�������Ķ��ձ�װ���ڴ�
            // return:
            //      -1  ����
            //      0   �ļ�������
            //      1   �ɹ�
            nRet = LoadRuleNameTable(PathUtil.MergePath(this.MainForm.DataDir, "cataloging_rules.xml"),
                out rule_name_table,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 1)
                bTableExists = true;

            Debug.Assert(rule_name_table != null, "");

            Encoding preferredEncoding = this.CurrentEncoding;

            {
                // �۲�Ҫ����ĵ�һ����¼��marc syntax
            }

            OpenMarcFileDlg dlg = new OpenMarcFileDlg();
            MainForm.SetControlFont(dlg, this.Font);

            dlg.IsOutput = true;
            dlg.AddG01Visible = false;
            if (bTableExists == false)
            {
                dlg.RuleVisible = true;
                dlg.Rule = this.LastCatalogingRule;
            }
            dlg.FileName = this.LastIso2709FileName;
            dlg.CrLf = this.LastCrLfIso2709;
            dlg.EncodingListItems = Global.GetEncodingList(false);
            dlg.EncodingName =
                (String.IsNullOrEmpty(this.LastEncodingName) == true ? Global.GetEncodingName(preferredEncoding) : this.LastEncodingName);
            dlg.EncodingComment = "ע: ԭʼ���뷽ʽΪ " + Global.GetEncodingName(preferredEncoding);
            dlg.MarcSyntax = "<�Զ�>";    // strPreferedMarcSyntax;
            dlg.EnableMarcSyntax = false;
            dlg.ShowDialog(this);
            if (dlg.DialogResult != DialogResult.OK)
                return;

            string strCatalogingRule = "";

            if (bTableExists == false)
            {
                strCatalogingRule = dlg.Rule;
                if (strCatalogingRule == "<������>")
                    strCatalogingRule = null;
            }

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
                    this.DbType + "SearchForm",
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
                        this.DbType + "SearchForm",
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

            this.EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ��浽 MARC �ļ� ...");
            stop.BeginLoop();

            Stream s = null;

            int nOutputCount = 0;

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

                    if (String.IsNullOrEmpty(strRecPath) == true)
                        goto CONTINUE;

                    stop.SetMessage("����׼����¼ " + strRecPath + " ����Ŀ��¼·�� ...");
                    stop.SetProgressValue(i);

                    string strItemRecPath = "";
                    string strBiblioRecPath = "";
                    string strLocation = "";
                    if (this.DbType == "item")
                    {
                        Debug.Assert(this.DbType == "item", "");

                        nRet = SearchTwoRecPathByBarcode(
                            this.stop,
                            this.Channel,
                            "@path:" + strRecPath,
                out strItemRecPath,
                out strLocation,
                out strBiblioRecPath,
                out strError);
                    }
                    else
                    {
                        nRet = SearchBiblioRecPath(
                            this.stop,
                            this.Channel,
                            this.DbType,
                            strRecPath,
out strBiblioRecPath,
out strError);
                    }
                    if (nRet == -1)
                    {
                        goto ERROR1;
                    }
                    else if (nRet == 0)
                    {
                        strError = "��¼·�� '" + strRecPath + "' û���ҵ���¼";
                        goto ERROR1;
                    }
                    else if (nRet == 1)
                    {
                        item.Text = strItemRecPath;
                    }
                    else if (nRet > 1) // ���з����ظ�
                    {
                        strError = "��¼·�� '" + strRecPath + "' ���� " + nRet.ToString() + " ����¼������һ�����ش���";
                    }

                    // ȥ��
                    if (biblio_recpath_table.ContainsKey(strBiblioRecPath) == true)
                        goto CONTINUE;

                    if (bTableExists == true)
                    {
                        strCatalogingRule = "";
                        // ���ݹݲصص��ñ�Ŀ������
                        if (this.DbType == "item"
                            && string.IsNullOrEmpty(strLocation) == false)
                        {
                            // 
                            strCatalogingRule = (string)rule_name_table[strLocation];
                            if (string.IsNullOrEmpty(strCatalogingRule) == true)
                            {
                                strCatalogingRule = InputDlg.GetInput(
                                    this,
                                    null,
                                    "������ݲصص� '" + strLocation + "' ����Ӧ�ı�Ŀ��������:",
                                    "NLC",
                                    this.MainForm.DefaultFont);
                                if (strCatalogingRule == null)
                                {
                                    DialogResult result = MessageBox.Show(this,
                                        "������û��ָ���ݲصص� '" + strLocation + "' ����Ӧ�ı�Ŀ���������˹ݲصص㱻���� <������> ��Ŀ����������\r\n\r\n�Ƿ��������? (OK ������Cancel ����������������)",
                                        this.DbType + "SearchForm",
                                        MessageBoxButtons.OKCancel,
                                        MessageBoxIcon.Question,
                                        MessageBoxDefaultButton.Button1);
                                    if (result == System.Windows.Forms.DialogResult.Cancel)
                                        break;
                                    strCatalogingRule = "";
                                }

                                rule_name_table[strLocation] = strCatalogingRule; // ���浽�ڴ棬����Ͳ�������ͬ��ѯ����
                            }
                        }
                    }

                    string[] results = null;
                    byte[] baTimestamp = null;

                    stop.SetMessage("���ڻ�ȡ��Ŀ��¼ " + strBiblioRecPath);

                    long lRet = Channel.GetBiblioInfos(
                        stop,
                        strBiblioRecPath,
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

                    stop.SetProgressValue(i + 1);

                    nOutputCount++;

                CONTINUE:
                    i++;
                    // biblio_recpath_table[strBiblioRecPath] = 1;
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
                MainForm.StatusBarMessage = nOutputCount.ToString()
                    + "����¼�ɹ�׷�ӵ��ļ� " + this.LastIso2709FileName + " β��";
            else
                MainForm.StatusBarMessage = nOutputCount.ToString()
                    + "����¼�ɹ����浽���ļ� " + this.LastIso2709FileName + " β��";

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

         // ����ѡ������е���·���Ĳ����� ����Ŀ���¼·���ļ�
        void menu_saveToBiblioRecordPathFile_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ�����(��Ŀ��)��¼·���ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = this.ExportBiblioRecPathFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.ExportBiblioRecPathFilename = dlg.FileName;

            bool bAppend = true;

            // List<string> paths = new List<string>();
            List<string> biblio_recpaths = new List<string>();

            if (File.Exists(this.ExportBiblioRecPathFilename) == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "(��Ŀ��)��¼·���ļ� '" + this.ExportBiblioRecPathFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel ��������)",
                    this.DbType + "SearchForm",
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

            int nWarningLineCount = 0;
            int nDupCount = 0;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ�����Ŀ��¼·�� ...");
            stop.BeginLoop();

            // �����ļ�
            StreamWriter sw = new StreamWriter(this.ExportBiblioRecPathFilename,
                bAppend,	// append
                System.Text.Encoding.UTF8);
            try
            {

#if NO
                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count
                    + this.listView_records.SelectedItems.Count / 10);

                {
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

                        if (String.IsNullOrEmpty(strRecPath) == true)
                            continue;

                        stop.SetMessage("����׼����¼ " + strRecPath + " ����Ŀ��¼·�� ...");
                        stop.SetProgressValue(i++);

                        string strItemRecPath = "";
                        string strBiblioRecPath = "";
                        int nRet = 0;
                        if (this.DbType == "item")
                        {
                            nRet = SearchTwoRecPathByBarcode("@path:" + strRecPath,
                    out strItemRecPath,
                    out strBiblioRecPath,
                    out strError);
                        }
                        else
                        {
                            nRet = SearchBiblioRecPath(strRecPath,
    out strBiblioRecPath,
    out strError);
                        }
                        if (nRet == -1)
                        {
                            goto ERROR1;
                        }
                        else if (nRet == 0)
                        {
                            strError = "��¼·�� '" + strRecPath + "' û���ҵ���¼";
                            goto ERROR1;
                        }
                        else if (nRet == 1)
                        {
                            item.Text = strItemRecPath;
                        }
                        else if (nRet > 1) // ���з����ظ�
                        {
                            strError = "��¼·�� '" + strRecPath + "' ���� " + nRet.ToString() + " ����¼������һ�����ش���";
                        }

                        paths.Add(strBiblioRecPath);
                    }
                }

                /*
                paths.Sort();
                StringUtil.RemoveDup(ref paths);
                 * */
                stop.SetMessage("���ڹ鲢...");
                StringUtil.RemoveDupNoSort(ref paths);

                stop.SetMessage("����д���ļ�...");
                int nBase = this.listView_records.SelectedItems.Count;

                stop.SetProgressRange(0, nBase + paths.Count / 10);

                for (int i = 0; i < paths.Count; i++)
                {
                    Application.DoEvents();

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }
                    }

                    sw.WriteLine(paths[i]);
                    stop.SetProgressValue(nBase + i / 10);
                }
#endif
                nRet = GetSelectedBiblioRecPath(out biblio_recpaths,
            ref nWarningLineCount,
            ref nDupCount,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                foreach (string path in biblio_recpaths)
                {
                    Application.DoEvents();

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }
                    }

                    sw.WriteLine(path);
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                if (sw != null)
                    sw.Close();
            }

            string strExportStyle = "����";
            if (bAppend == true)
                strExportStyle = "׷��";

            this.MainForm.StatusBarMessage = "��Ŀ��¼·�� " + biblio_recpaths.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportBiblioRecPathFilename;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��������¼·���ļ����͵�ǰ���ڵ������й�
        // parameters:
        //      bAppend [in]������ܵĻ��Ƿ���׷�ӵ��Ѿ����ڵ��ļ�ĩβ [out]ʵ�ʲ��õ��Ƿ�Ϊ׷�ӷ�ʽ
        // return:
        //      -1  ����
        //      0   ��������
        //      >0  �����ɹ����������Ѿ�����������
        /// <summary>
        /// ����ǰѡ�������������¼·���ļ�
        /// </summary>
        /// <param name="strFilename">��¼·���ļ���</param>
        /// <param name="bAppend">[in]������ܵĻ��Ƿ���׷�ӵ��Ѿ����ڵ��ļ�ĩβ [out]ʵ�ʲ��õ��Ƿ�Ϊ׷�ӷ�ʽ</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��; 0: ��������; >0 �����ɹ����������Ĵ�ʱ�ķ���ֵ���Ѿ������ļ�¼��</returns>
        public int ExportToRecPathFile(string strFilename,
            ref bool bAppend,
            out string strError)
        {
            strError = "";
            int nCount = 0;

            if (File.Exists(this.ExportRecPathFilename) == true && bAppend == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "��¼·���ļ� '" + this.ExportRecPathFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel ��������)",
                    this.DbType + "SearchForm",
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

            // �����ļ�
            StreamWriter sw = new StreamWriter(strFilename,
    bAppend,	// append
    System.Text.Encoding.UTF8);
            try
            {
                stop.Style = StopStyle.EnableHalfStop;
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڵ�����¼·�� ...");
                stop.BeginLoop();

                this.EnableControls(false);
                try
                {

                    foreach (ListViewItem item in this.listView_records.SelectedItems)
                    {
                        if (String.IsNullOrEmpty(item.Text) == true)
                            continue;
                        sw.WriteLine(item.Text);
                        nCount++;
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
            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }

            return nCount;
        }

        // ����ѡ������е���·���Ĳ����� ����¼·���ļ�
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

            // return:
            //      -1  ����
            //      0   ��������
            //      >0  �����ɹ����������Ѿ�����������
            int nRet = ExportToRecPathFile(
                this.ExportRecPathFilename,
                ref bAppend,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string strExportStyle = "����";
            if (bAppend == true)
                strExportStyle = "׷��";

            this.MainForm.StatusBarMessage = "���¼·�� " + nRet.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportRecPathFilename;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

#if NOOOOOOOOOOO

        // ��ListViewItem�ı����ݹ���Ϊtab�ַ��ָ���ַ���
        static string BuildLine(ListViewItem item)
        {
            string strLine = "";
            for (int i = 0; i < item.SubItems.Count; i++)
            {
                if (i != 0)
                    strLine += "\t";
                strLine += item.SubItems[i].Text;
            }

            return strLine;
        }

        // �����ַ�������ListViewItem��
        // �ַ����ĸ�ʽΪ\t�����
        static ListViewItem BuildListViewItem(string strLine)
        {
            ListViewItem item = new ListViewItem();
            string[] parts = strLine.Split(new char[] {'\t'});
            for (int i = 0; i < parts.Length; i++)
            {
                ListViewUtil.ChangeItemText(item, i, parts[i]);
            }

            return item;
        }

#endif

        // ����ѡ����е��ı��ļ�
        void menu_exportTextFile_Click(object sender, EventArgs e)
        {
            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ������ı��ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = this.ExportTextFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "�ı��ļ� (*.txt)|*.txt|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.ExportTextFilename = dlg.FileName;

            bool bAppend = true;

            if (File.Exists(this.ExportTextFilename) == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ı��ļ� '" + this.ExportTextFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel ��������)",
                    this.DbType + "SearchForm",
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
            StreamWriter sw = new StreamWriter(this.ExportTextFilename,
                bAppend,	// append
                System.Text.Encoding.UTF8);
            try
            {
                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    string strLine = Global.BuildLine(item);
                    sw.WriteLine(strLine);
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

            this.MainForm.StatusBarMessage = "������ " + this.listView_records.SelectedItems.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ı��ļ� " + this.ExportTextFilename;
        }

        // ����ѡ����е� Excel �ļ�
        void menu_exportExcelFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ����������";
                goto ERROR1;
            }

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                items.Add(item);
            }
            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ���ѡ������� Excel �ļ� ...");
            stop.BeginLoop();

            this.EnableControls(false);
            try
            {
                int nRet = ClosedXmlUtil.ExportToExcel(
                    stop,
                    items,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
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

        private void comboBox_entityDbName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_entityDbName.Items.Count > 0)
                return;

            this.comboBox_entityDbName.Items.Add("<ȫ��>");

            if (this.DbType == "arrive"
                && string.IsNullOrEmpty(this.MainForm.ArrivedDbName) == false)
            {
                this.comboBox_entityDbName.Items.Add(this.MainForm.ArrivedDbName);
                return;
            }

            if (this.DbType != "issue")
                this.comboBox_entityDbName.Items.Add("<ȫ��ͼ��>");

            this.comboBox_entityDbName.Items.Add("<ȫ���ڿ�>");

            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty property = this.MainForm.BiblioDbProperties[i];

                    if (this.DbType == "item")
                    {
                        if (String.IsNullOrEmpty(property.ItemDbName) == false)
                            this.comboBox_entityDbName.Items.Add(property.ItemDbName);
                    }
                    else if (this.DbType == "comment")
                    {
                        if (String.IsNullOrEmpty(property.CommentDbName) == false)
                            this.comboBox_entityDbName.Items.Add(property.CommentDbName);
                    }
                    else if (this.DbType == "order")
                    {
                        if (String.IsNullOrEmpty(property.OrderDbName) == false)
                            this.comboBox_entityDbName.Items.Add(property.OrderDbName);
                    }
                    else if (this.DbType == "issue")
                    {
                        if (String.IsNullOrEmpty(property.IssueDbName) == false)
                            this.comboBox_entityDbName.Items.Add(property.IssueDbName);
                    }
                    else
                        throw new Exception("δ֪��DbType '" + this.DbType + "'");

                }
            }
        }

        private void toolStripButton_search_Click(object sender, EventArgs e)
        {
            if (CheckProperties() == true)
                DoSearch(false, false, null);
        }

        bool CheckProperties()
        {
            string strError = "";
            if (this.MainForm.NormalDbProperties == null)
            {
                strError = "��ͨ���ݿ�������δ��ʼ��";
                goto ERROR1;
            }

            return true;
        ERROR1:
            MessageBox.Show(this, strError);
            return false;
        }

        private void ToolStripMenuItem_searchKeys_Click(object sender, EventArgs e)
        {
            DoSearch(true, false, null);
        }

        // �� ItemQueryParam �е���Ϣ�ָ��������
        void QueryToPanel(ItemQueryParam query, 
            bool bClearList = true)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                this.textBox_queryWord.Text = query.QueryWord;
                this.comboBox_entityDbName.Text = query.DbNames;
                this.comboBox_from.Text = query.From;
                this.comboBox_matchStyle.Text = query.MatchStyle;

                if (bClearList == true)
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

        private void textBox_queryWord_TextChanged(object sender, EventArgs e)
        {
            if (this.DbType == "item")
                this.Text = "ʵ���ѯ " + this.textBox_queryWord.Text;
            else
                this.Text = this.DbTypeCaption + "��ѯ " + this.textBox_queryWord.Text;
        }

#if NO
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
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }
#endif

        // int m_nInSelectedIndexChanged = 0;

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
#endif
            OnListViewSelectedIndexChanged(sender, e);
        }

        private void listView_records_ItemDrag(object sender,
            ItemDragEventArgs e)
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

        // ��������ͼ��
        private void comboBox_entityDbName_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_entityDbName.Invalidate();
        }

        // ��������ͼ��
        private void comboBox_from_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_from.Invalidate();
        }

        // ��������ͼ��
        private void comboBox_matchStyle_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_matchStyle.Invalidate();
        }

        private void ToolStripMenuItem_searchKeyID_Click(object sender, EventArgs e)
        {
            DoSearch(false, true, null);
        }

        private void textBox_queryWord_KeyPress(object sender, KeyPressEventArgs e)
        {

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
            this.MainForm.AppInfo.LinkFormState(dlg, "searchitemform_gettimedialog_single");
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

            this.MainForm.AppInfo.LinkFormState(dlg, "searchitemform_gettimedialog_single");
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
            this.MainForm.AppInfo.LinkFormState(dlg, "searchitemform_gettimedialog_range");
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

            this.MainForm.AppInfo.LinkFormState(dlg, "searchitemform_gettimedialog_range");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_queryWord.Text = dlg.uString;
        }

        private void listView_records_ColumnContextMenuClicked(object sender, ColumnHeader columnHeader)
        {
            ColumnClickEventArgs e = new ColumnClickEventArgs(this.listView_records.Columns.IndexOf(columnHeader));
            ListViewUtil.OnColumnContextMenuClick(this.listView_records, e);
        }

        ////////

        internal override bool InSearching
        {
            get
            {
                if (this.comboBox_from.Enabled == true)
                    return false;
                return true;
            }
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

        internal override string GetHeadString(bool bAjax = true)
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


        internal override int GetXmlHtml(BiblioInfo info,
    out string strXml,
    out string strHtml2,
    out string strError)
        {
            strError = "";
            strXml = "";
            strHtml2 = "";

            string strOldXml = "";
            string strNewXml = "";

            int nRet = 0;

            strOldXml = info.OldXml;
            strNewXml = info.NewXml;

            if (string.IsNullOrEmpty(strOldXml) == false
                && string.IsNullOrEmpty(strNewXml) == false)
            {
                // ����չʾ���� MARC ��¼����� HTML �ַ���
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = MarcDiff.DiffXml(
                    strOldXml,
                    strNewXml,
                    out strHtml2,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            else if (string.IsNullOrEmpty(strOldXml) == false
    && string.IsNullOrEmpty(strNewXml) == true)
            {
                strHtml2 = MarcUtil.GetHtmlOfXml(strOldXml,
                    false);
            }
            else if (string.IsNullOrEmpty(strOldXml) == true
                && string.IsNullOrEmpty(strNewXml) == false)
            {
                strHtml2 = MarcUtil.GetHtmlOfXml(strNewXml,
                    false);
            }

            strXml = MergeXml(strOldXml, strNewXml);

            return 0;
        }


        // ���һ����¼
        //return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        internal override int GetRecord(
            string strRecPath,
            out string strXml,
            out byte[] baTimestamp,
            out string strError)
        {
            strError = "";
            strXml = "";

            baTimestamp = null;
            string strOutputRecPath = "";
            string strBiblio = "";
            string strBiblioRecPath = "";
            // ��ò��¼
            long lRet = 0;

            if (this.DbType == "item")
            {
                lRet = Channel.GetItemInfo(
     stop,
     "@path:" + strRecPath,
     "xml",
     out strXml,
     out strOutputRecPath,
     out baTimestamp,
     "",
     out strBiblio,
     out strBiblioRecPath,
     out strError);
            }
            else if (this.DbType == "order")
            {
                lRet = Channel.GetOrderInfo(
     stop,
     "@path:" + strRecPath,
     "xml",
     out strXml,
     out strOutputRecPath,
     out baTimestamp,
     "",
     out strBiblio,
     out strBiblioRecPath,
     out strError);
            }
            else if (this.DbType == "issue")
            {
                lRet = Channel.GetIssueInfo(
     stop,
     "@path:" + strRecPath,
     "xml",
     out strXml,
     out strOutputRecPath,
     out baTimestamp,
     "",
     out strBiblio,
     out strBiblioRecPath,
     out strError);
            }
            else if (this.DbType == "comment")
            {
                lRet = Channel.GetCommentInfo(
     stop,
     "@path:" + strRecPath,
     "xml",
     out strXml,
     out strOutputRecPath,
     out baTimestamp,
     "",
     out strBiblio,
     out strBiblioRecPath,
     out strError);
            }

            if (lRet == 0)
                return 0;  // �Ƿ��趨Ϊ����״̬?
            if (lRet == -1)
                return -1;

            return 1;
        }

        // ����һ����¼
        // ����ɹ��� info.Timestamp �ᱻ����
        // return:
        //      -2  ʱ�����ƥ��
        //      -1  ����
        //      0   �ɹ�
        internal override int SaveRecord(string strRecPath,
            BiblioInfo info,
            out byte[] baNewTimestamp,
            out string strError)
        {
            strError = "";
            baNewTimestamp = null;

            List<EntityInfo> entityArray = new List<EntityInfo>();

            {
                EntityInfo item_info = new EntityInfo();


                item_info.OldRecPath = strRecPath;
                item_info.Action = "change";
                item_info.NewRecPath = strRecPath;

                item_info.NewRecord = info.NewXml;
                item_info.NewTimestamp = null;

                item_info.OldRecord = info.OldXml;
                item_info.OldTimestamp = info.Timestamp;

                entityArray.Add(item_info);
            }

            // ���Ƶ�Ŀ��
            EntityInfo[] entities = null;
            entities = new EntityInfo[entityArray.Count];
            for (int i = 0; i < entityArray.Count; i++)
            {
                entities[i] = entityArray[i];
            }

            EntityInfo[] errorinfos = null;

            long lRet = 0;

            if (this.DbType == "item")
                lRet = this.Channel.SetEntities(
                     null,   // this.BiblioStatisForm.stop,
                     "",
                     entities,
                     out errorinfos,
                     out strError);
            else if (this.DbType == "order")
                lRet = this.Channel.SetOrders(
                     null,   // this.BiblioStatisForm.stop,
                     "",
                     entities,
                     out errorinfos,
                     out strError);
            else if (this.DbType == "issue")
                lRet = this.Channel.SetIssues(
                     null,   // this.BiblioStatisForm.stop,
                     "",
                     entities,
                     out errorinfos,
                     out strError);
            else if (this.DbType == "comment")
                lRet = this.Channel.SetComments(
                     null,   // this.BiblioStatisForm.stop,
                     "",
                     entities,
                     out errorinfos,
                     out strError);
            else
            {
                strError = "δ֪�����ݿ����� '" + this.DbType + "'";
                return -1;
            }
            if (lRet == -1)
                return -1;

            // string strWarning = ""; // ������Ϣ

            if (errorinfos == null)
                return 0;

            strError = "";
            for (int i = 0; i < errorinfos.Length; i++)
            {
#if NO
                if (String.IsNullOrEmpty(errorinfos[i].RefID) == true)
                {
                    strError = "���������ص�EntityInfo�ṹ��RefIDΪ��";
                    return -1;
                }
#endif
                if (i == 0)
                    baNewTimestamp = errorinfos[i].NewTimestamp;

                // ������Ϣ����
                if (errorinfos[i].ErrorCode == ErrorCodeValue.NoError)
                    continue;

                strError += errorinfos[i].RefID + "���ύ��������з������� -- " + errorinfos[i].ErrorInfo + "\r\n";
            }

            info.Timestamp = baNewTimestamp;    // 2013/10/17

            if (String.IsNullOrEmpty(strError) == false)
                return -1;

            return 0;
        }

        // ��״̬����ʾ������Ϣ
        internal override void SetStatusMessage(string strMessage)
        {
            this.label_message.Text = strMessage;
        }

        // ����ѡ�����޸�
        void menu_clearSelectedChangedRecords_Click(object sender, EventArgs e)
        {
            ClearSelectedChangedRecords();
        }

#if NO
        // ����ѡ�����޸�
        // �˹������Ա�һ���û���⡣�����˵�Ϊ�η������ܱ����ˣ�
        void menu_acceptSelectedChangedRecords_Click(object sender, EventArgs e)
        {
            AcceptSelectedChangedRecords();
        }
#endif

        // ����ȫ���޸�
        void menu_clearAllChangedRecords_Click(object sender, EventArgs e)
        {
            ClearAllChangedRecords();
        }

        // ����ѡ��������޸�
        void menu_saveSelectedChangedRecords_Click(object sender, EventArgs e)
        {
            SaveSelectedChangedRecords();
        }

        // ����ȫ���޸�����
        void menu_saveAllChangedRecords_Click(object sender, EventArgs e)
        {
            SaveAllChangedRecords();
        }

        // ����һ���µ� C# �ű��ļ�
        void menu_createMarcQueryCsFile_Click(object sender, EventArgs e)
        {
            CreateStartCsFile();
        }

        // 
        /// <summary>
        /// ����һ���µ� C# �ű��ļ����ᵯ���Ի���ѯ���ļ�����
        /// �����е���� ItemHost ������
        /// </summary>
        public void CreateStartCsFile()
        {
            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ�����Ľű��ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = true;
            dlg.FileName = "";
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "C# �ű��ļ� (*.cs)|*.cs|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                ItemHost.CreateStartCsFile(dlg.FileName, this.DbType, this.DbTypeCaption);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return;
            }

            System.Diagnostics.Process.Start("notepad.exe", dlg.FileName);

            this.m_strUsedMarcQueryFilename = dlg.FileName;
        }

        void menu_quickMarcQueryRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫִ�� C# �ű�������";
                goto ERROR1;
            }

            // ������Ϣ����
            // ����Ѿ���ʼ�����򱣳�
            if (this.m_biblioTable == null)
                this.m_biblioTable = new Hashtable();

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ�� C# �ű��ļ�";
            dlg.FileName = this.m_strUsedMarcQueryFilename;
            dlg.Filter = "C# �ű��ļ� (*.cs)|*.cs|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.m_strUsedMarcQueryFilename = dlg.FileName;

            ItemHost host = null;
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
                host.DbType = this.DbType;
                host.RecordPath = "";
                host.ItemDom = null;
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
            stop.Initial("�������"+this.DbTypeCaption+"��¼ִ�� C# �ű� ...");
            stop.BeginLoop();

            this.EnableControls(false);

            this.listView_records.Enabled = false;
            try
            {
                if (stop != null)
                    stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                {
                    host.MainForm = this.MainForm;
                    host.DbType = this.DbType;
                    host.RecordPath = "";
                    host.ItemDom = null;
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
                        return;
                    if (result == DialogResult.No)
                    {
                        bOldSource = false;
                    }
                }

                ListViewPatronLoader loader = new ListViewPatronLoader(this.Channel,
                    stop,
                    items,
                    this.m_biblioTable);
                loader.DbTypeCaption = this.DbTypeCaption;

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

                    this.MainForm.OperHistory.AppendHtml("<div class='debug recpath'>" + HttpUtility.HtmlEncode(info.RecPath) + "</div>");

                    host.MainForm = this.MainForm;
                    host.DbType = this.DbType;
                    host.RecordPath = info.RecPath;
                    host.ItemDom = new XmlDocument();
                    if (bOldSource == true)
                    {
                        host.ItemDom.LoadXml(info.OldXml);
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
                            host.ItemDom.LoadXml(info.NewXml);
                        else
                            host.ItemDom.LoadXml(info.OldXml);
                    }
                    // host.ItemDom.LoadXml(info.OldXml);
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
                        string strXml = host.ItemDom.OuterXml;
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
                    host.DbType = this.DbType;
                    host.RecordPath = "";
                    host.ItemDom = null;
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

            DoViewComment(false);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ׼���ű�����
        int PrepareMarcQuery(string strCsFileName,
            out Assembly assembly,
            out ItemHost host,
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
                "dp2Circulation.ItemHost");
            if (entryClassType == null)
            {
                strError = strCsFileName + " ��û���ҵ� dp2Circulation.ItemHost ������";
                goto ERROR1;
            }

            // newһ��Host��������
            host = (ItemHost)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            return 0;
        ERROR1:
            return -1;
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
                this.DoLogicSearch(false, false, null);
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        long m_lLoaded = 0; // �����Ѿ�װ������������
        long m_lHitCount = 0;   // �������н������


        /// <summary>
        /// ִ��һ���߼�����
        /// </summary>
        /// <param name="bOutputKeyCount">�Ƿ�Ҫ���Ϊ key+count ��̬</param>
        /// <param name="bOutputKeyID">�Ƿ�Ϊ keyid ��̬</param>
        /// <param name="input_query">����ʽ</param>
        public void DoLogicSearch(bool bOutputKeyCount,
            bool bOutputKeyID,
            ItemQueryParam input_query)
        {
            string strError = "";

            if (bOutputKeyCount == true
    && bOutputKeyID == true)
            {
                strError = "bOutputKeyCount��bOutputKeyID����ͬʱΪtrue";
                goto ERROR1;
            }

            if (input_query != null)
            {
                QueryToPanel(input_query);
            }

            bool bQuickLoad = false;    // �Ƿ����װ��
            bool bClear = true; // �Ƿ��������������е�����

            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                bQuickLoad = true;

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                bClear = false;


            // �޸Ĵ��ڱ���
            // this.Text = "��Ŀ��ѯ �߼�����";

            this.m_bFirstColumnIsKey = bOutputKeyID;
            this.ClearListViewPropertyCache();

            ItemQueryParam query = PanelToQuery();
            PushQuery(query);

            if (bClear == true)
            {
                if (this.m_nChangedCount > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ���м�¼�б����� " + this.m_nChangedCount.ToString() + " ���޸���δ���档\r\n\r\n�Ƿ��������?\r\n\r\n(Yes �����Ȼ�����������No ��������)",
                        "ItemSearchForm",
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

            this.EnableControls(false);
            try
            {

                // string strBrowseStyle = "id,cols";
                string strOutputStyle = "";
                if (bOutputKeyCount == true)
                {
                    strOutputStyle = "keycount";
                    // strBrowseStyle = "keycount";
                }
                else if (bOutputKeyID == true)
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

                // return:
                //      -1  ����
                //      0   �û��ж�
                //      1   �������
                nRet = FillBrowseList(
                    query,
                    lHitCount,
                    bOutputKeyCount,
                    bOutputKeyID,
                    out strError);
                if (nRet == 0)
                    return;

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
                this.label_message.Text = "���������� " + lHitCount.ToString() + " ������ȫ��װ��";
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

        private void dp2QueryControl1_GetList(object sender, GetListEventArgs e)
        {
            // ����������ݿ���
            if (string.IsNullOrEmpty(e.Path) == true)
            {
                if (this.MainForm.BiblioDbProperties != null)
                {
                    for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                    {
                        BiblioDbProperty property = this.MainForm.BiblioDbProperties[i];

                        if (this.DbType == "item")
                        {
                            if (String.IsNullOrEmpty(property.ItemDbName) == false)
                                e.Values.Add(property.ItemDbName);
                        }
                        else if (this.DbType == "comment")
                        {
                            if (String.IsNullOrEmpty(property.CommentDbName) == false)
                                e.Values.Add(property.CommentDbName);
                        }
                        else if (this.DbType == "order")
                        {
                            if (String.IsNullOrEmpty(property.OrderDbName) == false)
                                e.Values.Add(property.OrderDbName);
                        }
                        else if (this.DbType == "issue")
                        {
                            if (String.IsNullOrEmpty(property.IssueDbName) == false)
                                e.Values.Add(property.IssueDbName);
                        }
                        else
                            throw new Exception("δ֪��DbType '" + this.DbType + "'");


                    }
                }
            }
            else
            {
                // ����ض����ݿ�ļ���;��
                // ÿ���ⶼһ��
                List<string> froms = GetFromList();
                foreach (string from in froms)
                {
                    e.Values.Add(from);
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

#if NO
            // ����װ��
            menuItem = new MenuItem("����װ��(&C)");
            menuItem.Click += new System.EventHandler(this.ToolStripMenuItem_continueLoad_Click);
            if (this.m_lHitCount <= this.listView_records.Items.Count)
                menuItem.Enabled = false;
            else
                menuItem.Enabled = true;
            e.ContextMenu.MenuItems.Add(menuItem);
#endif

            // ����ü�����
            menuItem = new MenuItem("����ü�����(&C)");
            menuItem.Click += new System.EventHandler(this.menu_logicSearchKeyCount_Click);
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
            this.DoLogicSearch(false, false, null);
        }

        // ����ü�����
        void menu_logicSearchKeyCount_Click(object sender, EventArgs e)
        {
            this.DoLogicSearch(true, false, null);
        }

        // key + id ���������м�����ļ���
        void menu_logicSearchKeyID_Click(object sender, EventArgs e)
        {
            this.DoLogicSearch(false, true, null);
        }
    }

    /// <summary>
    /// ��ѯʱʹ�õ�һ����������
    /// </summary>
    public class ItemQueryParam
    {
        /// <summary>
        /// ������
        /// </summary>
        public string QueryWord = "";

        /// <summary>
        /// ���ݿ���
        /// </summary>
        public string DbNames = "";

        /// <summary>
        /// ����;��
        /// </summary>
        public string From = "";

        /// <summary>
        /// ƥ�䷽ʽ
        /// </summary>
        public string MatchStyle = "";

        /// <summary>
        /// ����еĵ�һ���Ƿ�Ϊkey
        /// </summary>
        public bool FirstColumnIsKey = false;    // ����еĵ�һ���Ƿ�Ϊkey

        /// <summary>
        /// �������к�װ�� ListView �� ListViewItem ����
        /// </summary>
        public List<ListViewItem> Items = new List<ListViewItem>();
    }


}