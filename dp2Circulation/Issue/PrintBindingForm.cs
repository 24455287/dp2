using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Web;
using System.Reflection;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.CirculationClient;

using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Marc;
using DigitalPlatform.Script;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ��ӡװ����
    /// </summary>
    public partial class PrintBindingForm : MyForm
    {
        // refid��XmlDocument֮��Ķ��ձ�
        Hashtable ItemXmlTable = new Hashtable();

        /// <summary>
        /// ��ֵ��
        /// </summary>
        public Hashtable ColumnTable = new Hashtable();

        Assembly AssemblyFilter = null;
        ColumnFilterDocument MarcFilter = null;

        string SourceStyle = "";    // "batchno" "barcodefile"

        string BatchNo = "";    // �����������κ�
        string LocationString = ""; // �������Ĺݲصص�

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
        /// ����ͼ���±�: ����
        /// </summary>
        public const int TYPE_ERROR = 0;
        /// <summary>
        /// ����ͼ���±�: ����
        /// </summary>
        public const int TYPE_NORMAL = 1;
        // public const int TYPE_VERIFIED = 2;

        /// <summary>
        /// ���ʹ�ù���������ļ�ȫ·��
        /// </summary>
        public string BarcodeFilePath = "";
        /// <summary>
        /// ���ʹ�ù��ļ�¼·���ļ�ȫ·��
        /// </summary>
        public string RecPathFilePath = "";

        // int m_nGreenItemCount = 0;

        // ����������к�����
        SortColumns SortColumns_parent = new SortColumns();
        SortColumns SortColumns_member = new SortColumns();

        #region �к�
        /// <summary>
        /// �к�: �������
        /// </summary>
        public static int COLUMN_BARCODE = 0;    // �������
        /// <summary>
        /// �к�: ժҪ
        /// </summary>
        public static int COLUMN_SUMMARY = 1;    // ժҪ
        /// <summary>
        /// �к�: ������Ϣ
        /// </summary>
        public static int COLUMN_ERRORINFO = 1;  // ������Ϣ
        /// <summary>
        /// �к�: ISBN/ISSN
        /// </summary>
        public static int COLUMN_ISBNISSN = 2;           // ISBN/ISSN

        /// <summary>
        /// �к�: ״̬
        /// </summary>
        public static int COLUMN_STATE = 3;      // ״̬

        /// <summary>
        /// �к�: ��ȡ��
        /// </summary>
        public static int COLUMN_ACCESSNO = 4; // ��ȡ��
        /// <summary>
        /// �к�: ����ʱ��
        /// </summary>
        public static int COLUMN_PUBLISHTIME = 5;          // ����ʱ��
        /// <summary>
        /// �к�: ����
        /// </summary>
        public static int COLUMN_VOLUME = 6;          // ����

        /// <summary>
        /// �к�: �ݲصص�
        /// </summary>
        public static int COLUMN_LOCATION = 7;   // �ݲصص�
        /// <summary>
        /// �к�: �۸�
        /// </summary>
        public static int COLUMN_PRICE = 8;      // �۸�
        /// <summary>
        /// �к�: ������
        /// </summary>
        public static int COLUMN_BOOKTYPE = 9;   // ������
        /// <summary>
        /// �к�: ��¼��
        /// </summary>
        public static int COLUMN_REGISTERNO = 10; // ��¼��
        /// <summary>
        /// �к�: ע��
        /// </summary>
        public static int COLUMN_COMMENT = 11;    // ע��
        /// <summary>
        /// �к�: �ϲ�ע��
        /// </summary>
        public static int COLUMN_MERGECOMMENT = 12;   // �ϲ�ע��
        /// <summary>
        /// �к�: ���κ�
        /// </summary>
        public static int COLUMN_BATCHNO = 13;    // ���κ�

        /*
        public static int COLUMN_BORROWER = 10;  // ������
        public static int COLUMN_BORROWDATE = 11;    // ��������
        public static int COLUMN_BORROWPERIOD = 12;  // ��������
         * */

        /// <summary>
        /// �к�: ���¼·��
        /// </summary>
        public static int COLUMN_RECPATH = 14;   // ���¼·��
        /// <summary>
        /// �к�: ��Ŀ��¼·��
        /// </summary>
        public static int COLUMN_BIBLIORECPATH = 15; // �ּ�¼·��

        /// <summary>
        /// �к�: �ο�ID
        /// </summary>
        public static int COLUMN_REFID = 16; // �ο�ID

        /*
        public static int MERGED_COLUMN_CLASS = 17;             // ���
        public static int MERGED_COLUMN_CATALOGNO = 18;          // ��Ŀ��
        public static int MERGED_COLUMN_ORDERTIME = 19;        // ����ʱ��
        public static int MERGED_COLUMN_ORDERID = 20;          // ������
         * */

        /// <summary>
        /// �к�: ����
        /// </summary>
        public static int COLUMN_SELLER = 17;             // ����
        /// <summary>
        /// �к�: ������Դ
        /// </summary>
        public static int COLUMN_SOURCE = 18;             // ������Դ
        /// <summary>
        /// �к�: �����
        /// </summary>
        public static int COLUMN_INTACT = 19;        // �����
        /*
        public static int COLUMN_BARCODE = 0;    // �������
        public static int COLUMN_SUMMARY = 1;    // ժҪ
        public static int COLUMN_ERRORINFO = 1;  // ������Ϣ
        public static int COLUMN_ISBNISSN = 2;           // ISBN/ISSN

        public static int COLUMN_STATE = 3;      // ״̬
        public static int COLUMN_LOCATION = 4;   // �ݲصص�
        public static int COLUMN_PRICE = 5;      // �۸�
        public static int COLUMN_BOOKTYPE = 6;   // ������
        public static int COLUMN_REGISTERNO = 7; // ��¼��
        public static int COLUMN_COMMENT = 8;    // ע��
        public static int COLUMN_MERGECOMMENT = 9;   // �ϲ�ע��
        public static int COLUMN_BATCHNO = 10;    // ���κ�
        public static int COLUMN_BORROWER = 11;  // ������
        public static int COLUMN_BORROWDATE = 12;    // ��������
        public static int COLUMN_BORROWPERIOD = 13;  // ��������
        public static int COLUMN_RECPATH = 14;   // ���¼·��
        public static int COLUMN_BIBLIORECPATH = 15; // �ּ�¼·��
        public static int COLUMN_ACCESSNO = 16; // ��ȡ��
        public static int COLUMN_TARGETRECPATH = 17; // Ŀ���¼·��
         * */
#endregion

        const int WM_LOADSIZE = API.WM_USER + 201;

        /// <summary>
        /// ���캯��
        /// </summary>
        public PrintBindingForm()
        {
            InitializeComponent();
        }

        private void PrintBindingForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
            CreateColumnHeader(this.listView_parent);

            CreateColumnHeader(this.listView_member);

#if NO
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            this.BarcodeFilePath = this.MainForm.AppInfo.GetString(
                "printbindingform",
                "barcode_filepath",
                "");

            this.BatchNo = this.MainForm.AppInfo.GetString(
                "printbindingform",
                "batchno",
                "");

            this.LocationString = this.MainForm.AppInfo.GetString(
                "printbindingform",
                "location_string",
                "");
            this.comboBox_sort_sortStyle.Text = this.MainForm.AppInfo.GetString(
                "printbindingform",
                "sort_style",
                "��Ŀ��¼·��");

            this.checkBox_print_barcodeFix.Checked = this.MainForm.AppInfo.GetBoolean(
                "printbindingform",
                "barcode_fix",
                false);

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);
        }

        private void PrintBindingForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            this.MainForm.AppInfo.SetString(
                "printbindingform",
                "barcode_filepath",
                this.BarcodeFilePath);

            this.MainForm.AppInfo.SetString(
                "printbindingform",
                "batchno",
                this.BatchNo);

            this.MainForm.AppInfo.SetString(
                "printbindingform",
                "location_string",
                this.LocationString);

            this.MainForm.AppInfo.SetString(
    "printbindingform",
    "sort_style",
    this.comboBox_sort_sortStyle.Text);

            this.MainForm.AppInfo.SetBoolean(
    "printbindingform",
    "barcode_fix",
    this.checkBox_print_barcodeFix.Checked);

            SaveSize();

            if (this.ErrorInfoForm != null)
            {
                try
                {
                    this.ErrorInfoForm.Close();
                }
                catch
                {
                }
            }
        }

        private void PrintBindingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
#if NO
            if (stop != null)
            {
                // Debug.Assert(false, "");
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }
            }
#endif

            /*
            if (this.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�������в���Ϣ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
                    "printbindingform",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
             * */
        }

        /*public*/ void LoadSize()
        {
#if NO
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state");
#endif

            string strWidths = this.MainForm.AppInfo.GetString(
                "printbindingform",
                "list_parent_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_parent,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
    "printbindingform",
    "list_member_width",
    "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_member,
                    strWidths,
                    true);
            }
        }

        /*public*/ void SaveSize()
        {
#if NO
            MainForm.AppInfo.SaveMdiChildFormStates(this,
                "mdi_form_state");
#endif
            /*
            // ���MDI�Ӵ��ڲ���MainForm�ո�׼���˳�ʱ��״̬���ָ�����Ϊ�˼���ߴ���׼��
            if (this.WindowState != this.MainForm.MdiWindowState)
                this.WindowState = this.MainForm.MdiWindowState;
             * */

            string strWidths = ListViewUtil.GetColumnWidthListStringExt(this.listView_parent);
            this.MainForm.AppInfo.SetString(
                "printbindingform",
                "list_parent_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListStringExt(this.listView_member);
            this.MainForm.AppInfo.SetString(
                "printbindingform",
                "list_member_width",
                strWidths);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_LOADSIZE:
                    LoadSize();
                    return;
            }
            base.DefWndProc(ref m);
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
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            // load page
            this.button_load_loadFromBatchNo.Enabled = bEnable;
            this.button_load_loadFromBarcodeFile.Enabled = bEnable;
            this.button_load_loadFromRecPathFile.Enabled = bEnable;

            // next button
            if (bEnable == true)
                SetNextButtonEnable();
            else
                this.button_next.Enabled = false;


            // print page
            this.button_print_option.Enabled = bEnable;
            this.button_print_print.Enabled = bEnable;

        }

        void SetNextButtonEnable()
        {
            string strError = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_load)
            {
                // �㱨����װ�������
                // return:
                //      0   ��δװ���κ�����    
                //      1   װ���Ѿ����
                //      2   ��Ȼװ�������ݣ����������д�������
                int nState = ReportLoadState(out strError);

                if (nState != 1)
                {
                    this.button_next.Enabled = false;
                }
                else
                    this.button_next.Enabled = true;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_sort)
            {
                this.button_next.Enabled = true;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
                this.button_next.Enabled = false;
            }
            else
            {
                Debug.Assert(false, "δ֪��tabpage״̬");
            }

        }

        // �㱨����װ�������
        // return:
        //      0   ��δװ���κ�����    
        //      1   װ���Ѿ����
        //      2   ��Ȼװ�������ݣ����������д�������
        int ReportLoadState(out string strError)
        {
            strError = "";

            int nRedCount = 0;
            int nWhiteCount = 0;

            for (int i = 0; i < this.listView_parent.Items.Count; i++)
            {
                ListViewItem item = this.listView_parent.Items[i];

                if (item.ImageIndex == TYPE_ERROR)
                    nRedCount++;
                else
                    nWhiteCount++;
            }

            if (nRedCount != 0)
            {
                strError = "�б����� " + nRedCount + " ����������(��ɫ��)�����޸����ݺ�����װ�ء�";
                return 2;
            }

            if (nWhiteCount == 0)
            {
                strError = "��δװ���������";
                return 0;
            }

            strError = "��������װ����ȷ��";
            return 1;
        }

        private void button_load_loadFromBatchNo_Click(object sender, EventArgs e)
        {
            SearchByBatchnoForm dlg = new SearchByBatchnoForm();
            MainForm.SetControlFont(dlg, this.Font, false);

            this.ClearErrorInfoForm();

            dlg.BatchNo = this.BatchNo;
            dlg.ItemLocation = this.LocationString;

            dlg.CfgSectionName = "PrintBindingForm_SearchByBatchnoForm";
            dlg.GetLocationValueTable -= new GetValueTableEventHandler(dlg_GetLocationValueTable);
            dlg.GetLocationValueTable += new GetValueTableEventHandler(dlg_GetLocationValueTable);

            dlg.GetBatchNoTable -= new GetKeyCountListEventHandler(dlg_GetBatchNoTable);
            dlg.GetBatchNoTable += new GetKeyCountListEventHandler(dlg_GetBatchNoTable);
            dlg.RefDbName = "";

            dlg.MainForm = this.MainForm;

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.SourceStyle = "batchno";

            // ����
            this.BatchNo = dlg.BatchNo;
            this.LocationString = dlg.ItemLocation;

            string strMatchLocation = dlg.ItemLocation;

            if (strMatchLocation == "<��ָ��>")
                strMatchLocation = null;    // null��""������ܴ�

            string strError = "";
            int nRet = 0;

            if (Control.ModifierKeys == Keys.Control)
            {
            }
            else
            {
                /*
                if (this.Changed == true)
                {
                    // ������δ����
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ�������в���Ϣ���޸ĺ���δ���档����ʱΪװ�������ݶ����ԭ����Ϣ����δ������Ϣ����ʧ��\r\n\r\nȷʵҪװ��������? ",
                        "ItemHandoverForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result != DialogResult.Yes)
                    {
                        return; // ����
                    }
                }
                 * */

                this.listView_parent.Items.Clear();
                this.SortColumns_parent.Clear();
                SortColumns.ClearColumnSortDisplay(this.listView_parent.Columns);

                this.listView_member.Items.Clear();
                this.SortColumns_member.Clear();
                SortColumns.ClearColumnSortDisplay(this.listView_member.Columns);
            }

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            try
            {
                stop.SetProgressRange(0, 100);
                // stop.SetProgressValue(0);

                long lRet = 0;
                if (this.BatchNo == "<��ָ��>")
                {
                    // 2013/3/25
                    lRet = Channel.SearchItem(
                    stop,
                        // 2010/2/25 changed
                     "<all series>",
                    "", //
                    -1,
                    "__id",
                    "left",
                    this.Lang,
                    "batchno",   // strResultSetName
                    "",    // strSearchStyle
                    "", // strOutputStyle
                    out strError);
                    if (lRet == 0)
                    {
                        // TODO: �Ż���ʾ
                        strError = "����ȫ�� '����������' ���͵Ĳ��¼û�����м�¼��";
                        goto ERROR1;
                    }
                }
                else
                    lRet = Channel.SearchItem(
                        stop,
                        // 2010/2/25 changed
                         "<all series>",
                        dlg.BatchNo,
                        -1,
                        "���κ�",
                        "exact",
                        this.Lang,
                        "batchno",   // strResultSetName
                        "",    // strSearchStyle
                        "", // strOutputStyle
                        out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (lRet == 0)
                {
                    strError = "���κ� '" + dlg.BatchNo + "' û�����м�¼��";
                    goto ERROR1;
                }

                int nDupCount = 0;

                long lHitCount = lRet;

                stop.SetProgressRange(0, lHitCount);
                stop.SetProgressValue(0);


                long lStart = 0;
                long lCount = lHitCount;
                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                // װ�������ʽ
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


                    lRet = Channel.GetSearchResult(
                        stop,
                        "batchno",   // strResultSetName
                        lStart,
                        lCount,
                        "id,cols",
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "GetSearchResult() error: " + strError;
                        // Debug.Assert(false, "");
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
                        DigitalPlatform.CirculationClient.localhost.Record result_item = searchresults[i];

                        string strBarcode = result_item.Cols[0];
                        string strRecPath = result_item.Path;

                        // ���·����������Ŀ���Ƿ�Ϊͼ��/�ڿ��⣿
                        // return:
                        //      -1  error
                        //      0   ������Ҫ����ʾ��Ϣ��strError��
                        //      1   ����Ҫ��
                        nRet = CheckItemRecPath("����������",
                            strRecPath,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 0)
                        {
                            GetErrorInfoForm().WriteHtml("·��Ϊ " + strRecPath + " �Ĳ��¼ " + strError + "\r\n");
                        }

                        /*
                        // ����������Ϊ�գ������·��װ��
                        // 2009/8/6 new add
                        if (String.IsNullOrEmpty(strBarcode) == true)
                        {
                            strBarcode = "@path:" + strRecPath;
                        }*/

                        // ����
                        strBarcode = "@path:" + strRecPath;


                        string strOutputItemRecPath = "";
                        // ���ݲ�����Ż��߼�¼·����װ����¼
                        // return: 
                        //      -2  ��������Ѿ���list�д�����
                        //      -1  ����
                        //      0   ��Ϊ�ݲصص㲻ƥ�䣬û�м���list��
                        //      1   �ɹ�
                        nRet = LoadOneItem(strBarcode,
                            this.listView_parent,
                            strMatchLocation,
                            out strOutputItemRecPath,
                            out strError);
                        if (nRet == -2)
                            nDupCount++;
                        /*
                        ReaderSearchForm.NewLine(
                            this.listView_records,
                            searchresults[i].Path,
                            searchresults[i].Cols);
                         * */
                        stop.SetProgressValue(lStart + i + 1);
                    }

                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    stop.SetMessage("������ " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " ��");

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                }

                if (this.listView_parent.Items.Count == 0
                    && strMatchLocation != null)
                {
                    strError = "��Ȼ���κ� '" + dlg.BatchNo + "' �����˼�¼ " + lHitCount.ToString() + " ��, �����Ǿ�δ��ƥ��ݲصص� '" + strMatchLocation + "' ��";
                    goto ERROR1;
                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                EnableControls(true);
                // MainForm.ShowProgress(false);
            }

            this.Text = "��ӡװ���� -- " + this.SourceDescription;

            // �㱨����װ�������
            // return:
            //      0   ��δװ���κ�����    
            //      1   װ���Ѿ����
            //      2   ��Ȼװ�������ݣ����������д�������
            int nState = ReportLoadState(out strError);
            if (nState != 1)
                goto ERROR1;


            return;
        ERROR1:
            this.Text = "��ӡװ����";
            MessageBox.Show(this, strError);
        }

        void dlg_GetBatchNoTable(object sender, GetKeyCountListEventArgs e)
        {
            Global.GetBatchNoTable(e,
                this,
                "����������",
                "item",
                this.stop,
                this.Channel);
        }

        void dlg_GetLocationValueTable(object sender, GetValueTableEventArgs e)
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

        private void button_next_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_load)
            {
                this.tabControl_main.SelectedTab = this.tabPage_sort;
                this.button_next.Enabled = true;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_sort)
            {
                // ��������
                // return:
                //      -1  ����
                //      0   û�б�Ҫ����
                //      1   ���������
                int nRet = DoSort(this.comboBox_sort_sortStyle.Text,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.tabControl_main.SelectedTab = this.tabPage_print;
                this.button_next.Enabled = false;
                this.button_print_print.Focus();
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
            }
            else
            {
                Debug.Assert(false, "δ֪��tabpage״̬");
            }

            this.SetNextButtonEnable();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // return:
        //      -1  ����
        //      0   û�б�Ҫ����
        //      1   ���������
        int DoSort(string strSortStyle,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strSortStyle) == true
                || strSortStyle == "<��>")
                return 0;

            if (strSortStyle == "��Ŀ��¼·��")
            {
                // ע��������������ֵ�һ���Ѿ����úã��򲻸ı��䷽�򡣲������Ⲣ����ζ���䷽��һ��������
                this.SortColumns_parent.SetFirstColumn(COLUMN_BIBLIORECPATH,
                    this.listView_parent.Columns,
                    false);
            }
            else
            {
                strError = "δ֪�������� '" + strSortStyle + "'";
                return -1;
            }

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            try
            {

                // ����
                this.listView_parent.ListViewItemSorter = new SortColumnsComparer(this.SortColumns_parent);
                this.listView_parent.ListViewItemSorter = null;
            }
            finally
            {
                this.Cursor = oldCursor;
            }

            SetGroupBackcolor(
                this.listView_parent,
                this.SortColumns_parent[0].No);

            return 1;
        }

        void ForceSortColumnsParent(int nClickColumn)
        {
            // ע��������������ֵ�һ���Ѿ����úã��򲻸ı��䷽�򡣲������Ⲣ����ζ���䷽��һ��������
            this.SortColumns_parent.SetFirstColumn(nClickColumn,
                this.listView_parent.Columns,
                false);

            // ����
            this.listView_parent.ListViewItemSorter = new SortColumnsComparer(this.SortColumns_parent);
            this.listView_parent.ListViewItemSorter = null;

            SetGroupBackcolor(
                this.listView_parent,
                nClickColumn);
        }

        // ���������ֵ�ı仯����������ɫ
        static void SetGroupBackcolor(
            ListView list,
            int nSortColumn)
        {
            string strPrevText = "";
            bool bDark = false;
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];

                if (item.ImageIndex == TYPE_ERROR)
                    continue;

                string strThisText = "";
                try
                {
                    strThisText = item.SubItems[nSortColumn].Text;
                }
                catch
                {
                }

                if (strThisText != strPrevText)
                {
                    // �仯��ɫ
                    if (bDark == true)
                        bDark = false;
                    else
                        bDark = true;

                    strPrevText = strThisText;
                }

                if (bDark == true)
                {
                    item.BackColor = Color.LightGray;

                    if (item.ForeColor == item.BackColor)
                        item.ForeColor = Color.Black;
                }
                else
                {
                    item.BackColor = Color.White;

                    if (item.ForeColor == item.BackColor)
                        item.ForeColor = Color.Black;
                }
            }
        }


        private void tabControl_main_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.tabControl_main.SelectedTab == this.tabPage_load)
            {
                this.button_next.Enabled = true;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_sort)
            {
                this.button_next.Enabled = true;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
                this.button_next.Enabled = false;
            }
            else
            {
                Debug.Assert(false, "δ֪��tabpage״̬");
            }
        }

        // ����listview��Ŀ����
        void CreateColumnHeader(ListView list)
        {
            ColumnHeader columnHeader_barcode = new ColumnHeader();
            ColumnHeader columnHeader_isbnIssn = new ColumnHeader();
            ColumnHeader columnHeader_state = new ColumnHeader();
            ColumnHeader columnHeader_location = new ColumnHeader();
            ColumnHeader columnHeader_price = new ColumnHeader();
            ColumnHeader columnHeader_bookType = new ColumnHeader();
            ColumnHeader columnHeader_comment = new ColumnHeader();
            ColumnHeader columnHeader_borrower = new ColumnHeader();
            ColumnHeader columnHeader_borrowDate = new ColumnHeader();
            ColumnHeader columnHeader_borrowPeriod = new ColumnHeader();
            ColumnHeader columnHeader_recpath = new ColumnHeader();
            ColumnHeader columnHeader_errorInfo = new ColumnHeader();
            ColumnHeader columnHeader_registerNo = new ColumnHeader();
            ColumnHeader columnHeader_mergeComment = new ColumnHeader();
            ColumnHeader columnHeader_batchNo = new ColumnHeader();
            ColumnHeader columnHeader_biblioRecpath = new ColumnHeader();
            ColumnHeader columnHeader_accessno = new ColumnHeader();

            ColumnHeader columnHeader_refID = new ColumnHeader();
            ColumnHeader columnHeader_publishTime = new ColumnHeader();
            ColumnHeader columnHeader_volume = new ColumnHeader();
            /*
            ColumnHeader columnHeader_class = new ColumnHeader();
            ColumnHeader columnHeader_catalogNo = new ColumnHeader();
            ColumnHeader columnHeader_orderTime = new ColumnHeader();
            ColumnHeader columnHeader_orderID = new ColumnHeader();
             * */
            ColumnHeader columnHeader_seller = new ColumnHeader();
            ColumnHeader columnHeader_source = new ColumnHeader();
            ColumnHeader columnHeader_intact = new ColumnHeader();


            list.Columns.Clear();

            list.Columns.AddRange(new ColumnHeader[] {
            columnHeader_barcode,
            columnHeader_errorInfo,
            columnHeader_isbnIssn,
            columnHeader_state,

                            columnHeader_accessno,
            columnHeader_publishTime,
            columnHeader_volume,


            columnHeader_location,
            columnHeader_price,
            columnHeader_bookType,
            columnHeader_registerNo,
            columnHeader_comment,
            columnHeader_mergeComment,
            columnHeader_batchNo,

                /*
            columnHeader_borrower,
            columnHeader_borrowDate,
            columnHeader_borrowPeriod,
                 * */

            columnHeader_recpath,
            columnHeader_biblioRecpath,
                            columnHeader_refID,

                /*
            columnHeader_class,
            columnHeader_catalogNo,
            columnHeader_orderTime,
            columnHeader_orderID,
                 * */
            columnHeader_seller,
            columnHeader_source,
            columnHeader_intact});

            // 
            // columnHeader_isbnIssn
            // 
            columnHeader_isbnIssn.Text = "ISBN/ISSN";
            columnHeader_isbnIssn.Width = 160;
            // 
            // columnHeader_volume
            // 
            columnHeader_volume.Text = "����";
            columnHeader_volume.Width = 100;
            // 
            // columnHeader_publishTime
            // 
            columnHeader_publishTime.Text = "����ʱ��";
            columnHeader_publishTime.Width = 100;
            /*
            // 
            // columnHeader_class
            // 
            columnHeader_class.Text = "���";
            columnHeader_class.Width = 100;

            // 
            // columnHeader_catalogNo
            // 
            columnHeader_catalogNo.Text = "��Ŀ��";
            columnHeader_catalogNo.Width = 100;

            // 
            // columnHeader_orderTime
            // 
            columnHeader_orderTime.Text = "����ʱ��";
            columnHeader_orderTime.Width = 150;
            // 
            // columnHeader_orderID
            // 
            columnHeader_orderID.Text = "������";
            columnHeader_orderID.Width = 150;
             * */
            // 
            // columnHeader_seller
            // 
            columnHeader_seller.Text = "����";
            columnHeader_seller.Width = 150;
            // 
            // columnHeader_source
            // 
            columnHeader_source.Text = "������Դ";
            columnHeader_source.Width = 150;
            // 
            // columnHeader_intact
            // 
            columnHeader_intact.Text = "�����";
            columnHeader_intact.Width = 150;
            // 
            // columnHeader_refID
            // 
            columnHeader_refID.Text = "�ο�ID";
            columnHeader_refID.Width = 100;



            // 
            // columnHeader_barcode
            // 
            columnHeader_barcode.Text = "�������";
            columnHeader_barcode.Width = 150;
            // 
            // columnHeader_errorInfo
            // 
            columnHeader_errorInfo.Text = "ժҪ/������Ϣ";
            columnHeader_errorInfo.Width = 200;
            // 
            // columnHeader_state
            // 
            columnHeader_state.Text = "״̬";
            columnHeader_state.Width = 100;
            // 
            // columnHeader_location
            // 
            columnHeader_location.Text = "�ݲصص�";
            columnHeader_location.Width = 150;
            // 
            // columnHeader_price
            // 
            columnHeader_price.Text = "��۸�";
            columnHeader_price.Width = 150;
            // 
            // columnHeader_bookType
            // 
            columnHeader_bookType.Text = "������";
            columnHeader_bookType.Width = 150;
            // 
            // columnHeader_registerNo
            // 
            columnHeader_registerNo.Text = "��¼��";
            columnHeader_registerNo.Width = 150;
            // 
            // columnHeader_comment
            // 
            columnHeader_comment.Text = "��ע";
            columnHeader_comment.Width = 150;
            // 
            // columnHeader_mergeComment
            // 
            columnHeader_mergeComment.Text = "�ϲ�ע��";
            columnHeader_mergeComment.Width = 150;
            // 
            // columnHeader_batchNo
            // 
            columnHeader_batchNo.Text = "���κ�";
            // 
            // columnHeader_borrower
            // 
            columnHeader_borrower.Text = "������";
            columnHeader_borrower.Width = 150;
            // 
            // columnHeader_borrowDate
            // 
            columnHeader_borrowDate.Text = "��������";
            columnHeader_borrowDate.Width = 150;
            // 
            // columnHeader_borrowPeriod
            // 
            columnHeader_borrowPeriod.Text = "��������";
            columnHeader_borrowPeriod.Width = 150;
            // 
            // columnHeader_recpath
            // 
            columnHeader_recpath.Text = "���¼·��";
            columnHeader_recpath.Width = 200;
            // 
            // columnHeader_biblioRecpath
            // 
            columnHeader_biblioRecpath.Text = "�ּ�¼·��";
            columnHeader_biblioRecpath.Width = 200;
            // 
            // columnHeader_accessno
            // 
            columnHeader_accessno.Text = "��ȡ��";
            columnHeader_accessno.Width = 200;
            /*
            // 
            // columnHeader_barcode
            // 
            columnHeader_barcode.Text = "�������";
            columnHeader_barcode.Width = 150;
            // 
            // columnHeader_errorInfo
            // 
            columnHeader_errorInfo.Text = "ժҪ/������Ϣ";
            columnHeader_errorInfo.Width = 200;
            // 
            // columnHeader_isbnIssn
            // 
            columnHeader_isbnIssn.Text = "ISBN/ISSN";
            columnHeader_isbnIssn.Width = 160;
            // 
            // columnHeader_state
            // 
            columnHeader_state.Text = "״̬";
            columnHeader_state.Width = 100;
            // 
            // columnHeader_location
            // 
            columnHeader_location.Text = "�ݲصص�";
            columnHeader_location.Width = 150;
            // 
            // columnHeader_price
            // 
            columnHeader_price.Text = "��۸�";
            columnHeader_price.Width = 150;
            // 
            // columnHeader_bookType
            // 
            columnHeader_bookType.Text = "������";
            columnHeader_bookType.Width = 150;
            // 
            // columnHeader_registerNo
            // 
            columnHeader_registerNo.Text = "��¼��";
            columnHeader_registerNo.Width = 150;
            // 
            // columnHeader_comment
            // 
            columnHeader_comment.Text = "��ע";
            columnHeader_comment.Width = 150;
            // 
            // columnHeader_mergeComment
            // 
            columnHeader_mergeComment.Text = "�ϲ�ע��";
            columnHeader_mergeComment.Width = 150;
            // 
            // columnHeader_batchNo
            // 
            columnHeader_batchNo.Text = "���κ�";
            // 
            // columnHeader_borrower
            // 
            columnHeader_borrower.Text = "������";
            columnHeader_borrower.Width = 150;
            // 
            // columnHeader_borrowDate
            // 
            columnHeader_borrowDate.Text = "��������";
            columnHeader_borrowDate.Width = 150;
            // 
            // columnHeader_borrowPeriod
            // 
            columnHeader_borrowPeriod.Text = "��������";
            columnHeader_borrowPeriod.Width = 150;
            // 
            // columnHeader_recpath
            // 
            columnHeader_recpath.Text = "���¼·��";
            columnHeader_recpath.Width = 200;
            // 
            // columnHeader_biblioRecpath
            // 
            columnHeader_biblioRecpath.Text = "�ּ�¼·��";
            columnHeader_biblioRecpath.Width = 200;
            // 
            // columnHeader_accessno
            // 
            columnHeader_accessno.Text = "��ȡ��";
            columnHeader_accessno.Width = 200;
            // 
            // columnHeader_targetRecpath
            // 
            columnHeader_targetRecpath.Text = "Ŀ����Ŀ��¼·��";
            columnHeader_targetRecpath.Width = 200;
             * */

        }

        // ���ݲ�����Ż��߼�¼·����װ����¼
        // parameters:
        //      strBarcodeOrRecPath ������Ż��߼�¼·�����������ǰ׺Ϊ"@path:"���ʾΪ·��
        //      strMatchLocation    ���ӵĹݲصص�ƥ�����������==null����ʾû�������������(ע�⣬""��null���岻ͬ��""��ʾȷʵҪƥ�����ֵ)
        // return: 
        //      -2  ��������Ѿ���list�д�����(��û�м���listview��)
        //      -1  ����(ע���ʾ��������Ѿ�����listview����)
        //      0   ��Ϊ�ݲصص㲻ƥ�䣬û�м���list��
        //      1   �ɹ�
        /*public*/ int LoadOneItem(
            string strBarcodeOrRecPath,
            ListView list,
            string strMatchLocation,
            out string strOutputItemRecPath,
            out string strError)
        {
            strError = "";
            strOutputItemRecPath = "";

            // �ж��Ƿ��� @path: ǰ׺�����ں����֧����
            bool bIsRecPath = StringUtil.HasHead(strBarcodeOrRecPath, "@path:"); ;

            string strItemXml = "";
            string strBiblioText = "";

            string strBiblioRecPath = "";

            byte[] item_timestamp = null;

            long lRet = Channel.GetItemInfo(
                stop,
                strBarcodeOrRecPath,
                "xml",
                out strItemXml,
                out strOutputItemRecPath,
                out item_timestamp,
                "recpath",
                out strBiblioText,
                out strBiblioRecPath,
                out strError);
            if (lRet == -1 || lRet == 0)
            {
                ListViewItem item = null;

                if (bIsRecPath == false)
                    item = new ListViewItem(strBarcodeOrRecPath, 0);
                else
                    item = new ListViewItem("", 0); // ��ʱ��û�а취֪������

                // 2009/10/29 new add
                OriginItemData data = new OriginItemData();
                item.Tag = data;
                data.Timestamp = item_timestamp;
                data.Xml = strItemXml;

                ListViewUtil.ChangeItemText(item,
                    COLUMN_ERRORINFO,
                    strError);
                // item.SubItems.Add(strError);

                SetItemColor(item, TYPE_ERROR);
                list.Items.Add(item);

                // ���¼�������������Ұ
                list.EnsureVisible(list.Items.Count - 1);

                goto ERROR1;
            }

            string strBiblioSummary = "";
            string strISBnISSN = "";
            // string strTargetRecPath = "";

            // ������������Ƿ����ظ�?
            // ˳����ͬ�ֵ�����
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem curitem = list.Items[i];

                if (bIsRecPath == false)
                {
                    if (strBarcodeOrRecPath == curitem.Text)
                    {
                        strError = "������� " + strBarcodeOrRecPath + " �����ظ�";
                        return -2;
                    }
                }
                else
                {
                    if (strBarcodeOrRecPath == ListViewUtil.GetItemText(curitem, COLUMN_RECPATH))
                    {
                        strError = "��¼·�� " + strBarcodeOrRecPath + " �����ظ�";
                        return -2;
                    }
                }

                if (strBiblioSummary == "" && curitem.ImageIndex != TYPE_ERROR)
                {
                    if (curitem.SubItems[COLUMN_BIBLIORECPATH].Text == strBiblioRecPath)
                    {
                        strBiblioSummary = ListViewUtil.GetItemText(curitem, COLUMN_SUMMARY);
                        strISBnISSN = ListViewUtil.GetItemText(curitem, COLUMN_ISBNISSN);
                        // strTargetRecPath = ListViewUtil.GetItemText(curitem, COLUMN_TARGETRECPATH);
                    }
                }
            }

            if (strBiblioSummary == "")
            {
                string[] formats = new string[2];
                formats[0] = "summary";
                formats[1] = "@isbnissn";
                string[] results = null;
                byte[] timestamp = null;

                Debug.Assert(String.IsNullOrEmpty(strBiblioRecPath) == false, "strBiblioRecPathֵ����Ϊ��");

                lRet = Channel.GetBiblioInfos(
                    stop,
                    strBiblioRecPath,
                    "",
                    formats,
                    out results,
                    out timestamp,
                    out strError);
                if (lRet == -1 || lRet == 0)
                {
                    if (lRet == 0 && String.IsNullOrEmpty(strError) == true)
                        strError = "��Ŀ��¼ '" + strBiblioRecPath + "' ������";

                    strBiblioSummary = "�����ĿժҪʱ��������: " + strError;
                }
                else
                {
                    Debug.Assert(results != null && results.Length == 2, "results�������3��Ԫ��");
                    strBiblioSummary = results[0];
                    strISBnISSN = results[1];
                    // strTargetRecPath = results[2];
                }
            }

            // ����һ�����xml��¼��ȡ���й���Ϣ����listview��

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strItemXml);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                goto ERROR1;
            }


            // ���ӵĹݲصص�ƥ��
            if (strMatchLocation != null)
            {
                string strLocation = DomUtil.GetElementText(dom.DocumentElement,
                    "location");

                // 2013/3/25
                if (strLocation == null)
                    strLocation = "";

                if (strMatchLocation != strLocation)
                    return 0;
            }

            {
                ListViewItem item = AddToListView(list,
                    dom,
                    strOutputItemRecPath,
                    strBiblioSummary,
                    strISBnISSN,
                    strBiblioRecPath);

                // ����timestamp/xml
                OriginItemData data = (OriginItemData)item.Tag;
                Debug.Assert(data != null, "");
                data.Timestamp = item_timestamp;
                data.Xml = strItemXml;

                // ����Ƿ�Ϊ�϶����¼
                // return:
                //      0   ���ǡ�ͼ���Ѿ�����ΪTYPE_ERROR
                //      1   �ǡ�ͼ����δ����
                int nRet = CheckBindingItem(item);
                if (nRet == 1)
                {
                    // ͼ��
                    // item.ImageIndex = TYPE_NORMAL;
                    SetItemColor(item, TYPE_NORMAL);
                }

                // ���¼�������������Ұ
                list.EnsureVisible(list.Items.Count - 1);

                // ��������
                if (bIsRecPath == false)
                {
                    string strBarcode = DomUtil.GetElementText(dom.DocumentElement,
                        "barcode");
                    if (strBarcode != strBarcodeOrRecPath)
                    {
                        if (strBarcode.ToUpper() == strBarcodeOrRecPath.ToUpper())
                            strError = "���ڼ���������� '" + strBarcodeOrRecPath + "' �Ͳ��¼�е������ '" + strBarcode + "' ��Сд��һ��";
                        else
                            strError = "���ڼ���������� '" + strBarcodeOrRecPath + "' �Ͳ��¼�е������ '" + strBarcode + "' ��һ��";
                        ListViewUtil.ChangeItemText(item,
                            COLUMN_ERRORINFO,
                            strError);
                        SetItemColor(item, TYPE_ERROR);
                        goto ERROR1;
                    }
                }
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ����Ƿ�Ϊ�϶����¼
        // return:
        //      0   ���ǡ�ͼ���Ѿ�����ΪTYPE_ERROR
        //      1   �ǡ�ͼ����δ����
        int CheckBindingItem(ListViewItem item)
        {
            string strError = "";
            string strPublishTime = ListViewUtil.GetItemText(item, COLUMN_PUBLISHTIME);
            if (strPublishTime.IndexOf("-") == -1)
            {
                strError = "���Ǻ϶��ᡣ�������� '"+strPublishTime+"' ���Ƿ�Χ��ʽ";
                goto ERROR1;
            }

            string strState = ListViewUtil.GetItemText(item, COLUMN_STATE);
            if (StringUtil.IsInList("�϶���Ա", strState) == true)
            {
                strError = "���Ǻ϶��ᡣ״̬ '" + strState + "' �о���'�϶���Ա'ֵ";
                goto ERROR1;
            }

            OriginItemData data = (OriginItemData)item.Tag;
            if (data != null && String.IsNullOrEmpty(data.Xml) == false)
            {
                // ��item record xmlװ��DOM��Ȼ��select��ÿ��<item>Ԫ��
                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(data.Xml);
                }
                catch
                {
                    // XMLװ��DOM�����Ͳ������
                    goto CONTINUE;
                }

                XmlNode node = dom.DocumentElement.SelectSingleNode("binding/bindingParent");
                if (node != null)
                {
                    strError = "���Ǻ϶��ᡣ<binding>Ԫ���о���<bindingParent>Ԫ��";
                    goto ERROR1;
                }
            }

        CONTINUE:
            // �����Ҫ�����������������

            return 1;
        ERROR1:
            SetItemColor(item, TYPE_ERROR);

            // ���ƻ�ԭ���������ݣ���ֻ��������ǰ��
            string strOldSummary = ListViewUtil.GetItemText(item, COLUMN_ERRORINFO);
            if (String.IsNullOrEmpty(strOldSummary) == false)
                strError = strError + " | " + strOldSummary;
            ListViewUtil.ChangeItemText(item, COLUMN_ERRORINFO, strError);
            return 0;
        }

        // ��������ı�����ǰ����ɫ����ͼ��
        static void SetItemColor(ListViewItem item,
            int nType)
        {
            item.ImageIndex = nType;    // 2009/11/1 new add

            if (nType == TYPE_ERROR)
            {
                item.BackColor = Color.Red;
                item.ForeColor = Color.White;
                item.ImageIndex = TYPE_ERROR;
            }
#if NO
            else if (nType == TYPE_VERIFIED)
            {
                item.BackColor = Color.Green;
                item.ForeColor = Color.White;
                item.ImageIndex = TYPE_VERIFIED;
            }
#endif
            else if (nType == TYPE_NORMAL)
            {
                item.BackColor = SystemColors.Window;
                item.ForeColor = SystemColors.WindowText;
                item.ImageIndex = TYPE_NORMAL;
            }
            else
            {
                Debug.Assert(false, "δ֪��image type");
            }

        }

        /*public*/ static ListViewItem AddToListView(ListView list,
    XmlDocument dom,
    string strRecPath,
    string strBiblioSummary,
    string strISBnISSN,
    string strBiblioRecPath)
        {
            string strBarcode = DomUtil.GetElementText(dom.DocumentElement,
    "barcode");

            ListViewItem item = new ListViewItem(strBarcode, 0);

            SetListViewItemText(dom,
                false,
                strRecPath,
                strBiblioSummary,
                strISBnISSN,
                strBiblioRecPath,
                item);
            list.Items.Add(item);

            return item;
        }

        // ���ݲ��¼DOM����ListViewItem����һ�����������
        // ���������Զ��������data.Changed����Ϊfalse
        // parameters:
        //      bSetBarcodeColumn   �Ƿ�Ҫ��������������(��һ��)
        /*public*/ static void SetListViewItemText(XmlDocument dom,
            bool bSetBarcodeColumn,
            string strRecPath,
            string strBiblioSummary,
            string strISBnISSN,
            string strBiblioRecPath,
            ListViewItem item)
        {
            OriginItemData data = null;
            data = (OriginItemData)item.Tag;
            if (data == null)
            {
                data = new OriginItemData();
                item.Tag = data;
            }
            else
            {
                data.Changed = false;
            }

            string strState = DomUtil.GetElementText(dom.DocumentElement,
                "state");
            string strPublishTime = DomUtil.GetElementText(dom.DocumentElement,
                "publishTime");
            string strVolume = DomUtil.GetElementText(dom.DocumentElement,
                "volume");


            string strLocation = DomUtil.GetElementText(dom.DocumentElement,
                "location");
            string strPrice = DomUtil.GetElementText(dom.DocumentElement,
                "price");
            string strBookType = DomUtil.GetElementText(dom.DocumentElement,
                "bookType");
            string strRegisterNo = DomUtil.GetElementText(dom.DocumentElement,
                "registerNo");
            string strComment = DomUtil.GetElementText(dom.DocumentElement,
                "comment");
            string strMergeComment = DomUtil.GetElementText(dom.DocumentElement,
                "mergeComment");
            string strBatchNo = DomUtil.GetElementText(dom.DocumentElement,
                "batchNo");
            string strAccessNo = DomUtil.GetElementText(dom.DocumentElement,
                "accessNo");
            string strRefID = DomUtil.GetElementText(dom.DocumentElement,
                "refID");
            /*
            string strBinding = DomUtil.GetElementInnerXml(dom.DocumentElement,
                "binding");
             * */

            string strIntact = DomUtil.GetElementText(dom.DocumentElement,
                "intact");
            string strSeller = DomUtil.GetElementText(dom.DocumentElement,
                "seller");
            string strSource = DomUtil.GetElementText(dom.DocumentElement,
                "source");
            /*
            string strBorrower = DomUtil.GetElementText(dom.DocumentElement,
                "borrower");
            string strBorrowDate = DomUtil.GetElementText(dom.DocumentElement,
                "borrowDate");
            // 2007/6/20 new add
            strBorrowDate = DateTimeUtil.LocalTime(strBorrowDate, "u");
            string strBorrowPeriod = DomUtil.GetElementText(dom.DocumentElement,
                "borrowPeriod");
            strBorrowDate = DateTimeUtil.LocalTime(strBorrowDate, "u");
            string strBorrowPeriod = DomUtil.GetElementText(dom.DocumentElement,
                "borrowPeriod");
             * */



            ListViewUtil.ChangeItemText(item, COLUMN_SUMMARY, strBiblioSummary);

            ListViewUtil.ChangeItemText(item, COLUMN_STATE, strState);
            ListViewUtil.ChangeItemText(item, COLUMN_ACCESSNO, strAccessNo);
            ListViewUtil.ChangeItemText(item, COLUMN_PUBLISHTIME, strPublishTime);
            ListViewUtil.ChangeItemText(item, COLUMN_VOLUME, strVolume);

            ListViewUtil.ChangeItemText(item, COLUMN_ISBNISSN, strISBnISSN);


            ListViewUtil.ChangeItemText(item, COLUMN_LOCATION, strLocation);
            ListViewUtil.ChangeItemText(item, COLUMN_PRICE, strPrice);
            ListViewUtil.ChangeItemText(item, COLUMN_BOOKTYPE, strBookType);
            ListViewUtil.ChangeItemText(item, COLUMN_REGISTERNO, strRegisterNo);
            ListViewUtil.ChangeItemText(item, COLUMN_COMMENT, strComment);
            ListViewUtil.ChangeItemText(item, COLUMN_MERGECOMMENT, strMergeComment);
            ListViewUtil.ChangeItemText(item, COLUMN_BATCHNO, strBatchNo);

            /*
            ListViewUtil.ChangeItemText(item, COLUMN_BORROWER, strBorrower);
            ListViewUtil.ChangeItemText(item, COLUMN_BORROWDATE, strBorrowDate);
            ListViewUtil.ChangeItemText(item, COLUMN_BORROWPERIOD, strBorrowPeriod);
             * */

            ListViewUtil.ChangeItemText(item, COLUMN_RECPATH, strRecPath);
            ListViewUtil.ChangeItemText(item, COLUMN_BIBLIORECPATH, strBiblioRecPath);
            ListViewUtil.ChangeItemText(item, COLUMN_REFID, strRefID);

            ListViewUtil.ChangeItemText(item, COLUMN_SELLER, strSeller);
            ListViewUtil.ChangeItemText(item, COLUMN_SOURCE, strSource);
            ListViewUtil.ChangeItemText(item, COLUMN_INTACT, strIntact);

            if (bSetBarcodeColumn == true)
            {
                string strBarcode = DomUtil.GetElementText(dom.DocumentElement,
                    "barcode");
                ListViewUtil.ChangeItemText(item, COLUMN_BARCODE, strBarcode);
            }
        }

        // ׼���ű�����
        int PrepareMarcFilter(string strFilterFileName,
            out ColumnFilterDocument filter,
            out string strError)
        {
            strError = "";
            filter = null;

            if (FileUtil.FileExist(strFilterFileName) == false)
            {
                strError = "�ļ� '" + strFilterFileName + "' ������";
                goto ERROR1;
            }

            string strWarning = "";

            string strLibPaths = "\"" + this.MainForm.DataDir + "\"";
            Type entryClassType = this.GetType();



            filter = new ColumnFilterDocument();
            filter.Host = new ColumnFilterHost();
            filter.Host.ColumnTable = this.ColumnTable;

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
                MessageBox.Show(this, strWarning);
            }

            filter.Assembly = assemblyFilter;

            return 0;
        ERROR1:
            return -1;
        }

        // ����������ʽ��tab�ַ���
        static string IndentString(int nLevel)
        {
            if (nLevel <= 0)
                return "";
            return new string('\t', nLevel);
        }

        // ������Դ��������
        // ���Ϊ"batchno"��ʽ����Ϊ���κţ����Ϊ"barcodefile"��ʽ����Ϊ������ļ���(���ļ���); ���Ϊ"recpathfile"��ʽ����Ϊ��¼·���ļ���(���ļ���)
        /// <summary>
        /// ������Դ��������
        /// ���Ϊ"batchno"��ʽ����Ϊ���κţ����Ϊ"barcodefile"��ʽ����Ϊ������ļ���(���ļ���); ���Ϊ"recpathfile"��ʽ����Ϊ��¼·���ļ���(���ļ���)
        /// </summary>
        public string SourceDescription
        {
            get
            {
                if (this.SourceStyle == "batchno")
                {
                    string strText = "";

                    if (String.IsNullOrEmpty(this.BatchNo) == false)
                        strText += "���κ� " + this.BatchNo;

                    if (String.IsNullOrEmpty(this.LocationString) == false
                        && this.LocationString != "<��ָ��>")
                    {
                        if (String.IsNullOrEmpty(strText) == false)
                            strText += "; ";
                        strText += "�ݲص� " + this.LocationString;
                    }

                    return this.BatchNo;
                }
                else if (this.SourceStyle == "barcodefile")
                {
                    return "������ļ� " + Path.GetFileName(this.BarcodeFilePath);
                }
                else if (this.SourceStyle == "recpathfile")
                {
                    return "��¼·���ļ� " + Path.GetFileName(this.RecPathFilePath);
                }
                else
                {
                    Debug.Assert(this.SourceStyle == "", "");
                    return "";
                }
            }
        }

        private void button_print_print_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strWarning = "";
            int nRet = 0;

            if (this.listView_parent.Items.Count == 0)
            {
                strError = "Ŀǰû�пɴ�ӡ������";
                goto ERROR1;
            }

            // TODO: �Ƿ�Ҫ������TYPE_ERROR������������ӡ?
            int nSkipCount = 0;

            this.ItemXmlTable.Clear();  // ��ֹ����Ĳ���Ϣ������������֮�䱣��

            Hashtable macro_table = new Hashtable();

            // ���κŻ��ļ��� ����겻�����жϺ�ѡ�ã�ֻ�úϲ�Ϊһ����
            macro_table["%sourcedescription%"] = this.SourceDescription;

            macro_table["%libraryname%"] = this.MainForm.LibraryName;
            macro_table["%date%"] = DateTime.Now.ToLongDateString();
            macro_table["%datadir%"] = this.MainForm.DataDir;   // ��������datadir��templatesĿ¼�ڵ�ĳЩ�ļ�
            ////macro_table["%libraryserverdir%"] = this.MainForm.LibraryServerDir;  // �������÷������˵�CSS�ļ�

            // ��ô�ӡ����
            string strPubType = "����������";
            PrintBindingPrintOption option = new PrintBindingPrintOption(this.MainForm.DataDir,
                strPubType);
            option.LoadData(this.MainForm.AppInfo,
                "printbinding_printoption");

            /*
            macro_table["%pagecount%"] = nPageCount.ToString();
            macro_table["%linesperpage%"] = option.LinesPerPage.ToString();
             * */

            string strMarcFilterFilePath = option.GetTemplatePageFilePath("MARC������");
            if (String.IsNullOrEmpty(strMarcFilterFilePath) == false)
            {
                ColumnFilterDocument filter = null;

                this.ColumnTable = new Hashtable();
                nRet = PrepareMarcFilter(strMarcFilterFilePath,
                    out filter,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                //
                if (filter != null)
                    this.AssemblyFilter = filter.Assembly;
                else
                    this.AssemblyFilter = null;

                this.MarcFilter = filter;
            }

            List<string> filenames = new List<string>();

            try
            {
                EnableControls(false);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڹ���HTMLҳ�� ...");
                stop.BeginLoop();

                try
                {
                    stop.SetProgressRange(0, this.listView_parent.Items.Count);
                    stop.SetProgressValue(0);
                    for (int i = 0; i < this.listView_parent.Items.Count; i++)
                    {
                        ListViewItem item = this.listView_parent.Items[i];
                        if (item.ImageIndex == TYPE_ERROR)
                        {
                            nSkipCount++;
                            continue;
                        }

                        string strFilename = "";
                        string strOneWarning = "";
                        nRet = PrintOneBinding(
                                option,
                                macro_table,
                                item,
                                i,
                                out strFilename,
                                out strOneWarning,
                                out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        if (String.IsNullOrEmpty(strOneWarning) == false)
                        {
                            if (String.IsNullOrEmpty(strWarning) == false)
                                strWarning += "\r\n";
                            strWarning += strOneWarning;
                        }

                        filenames.Add(strFilename);

                        stop.SetProgressValue(i + 1);
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

                if (nSkipCount > 0)
                {
                    if (String.IsNullOrEmpty(strWarning) == false)
                        strWarning += "\r\n";
                    strWarning += "��ӡ�����У��� "+nSkipCount.ToString()+" ������״̬���������";
                }

                if (String.IsNullOrEmpty(strWarning) == false)
                {
                    // TODO: ����������ֵ�����̫�࣬��Ҫ�ضϣ��Ա�������ʾ��MessageBox()�С����ǽ����ļ�������û�б�Ҫ�ض�
                    MessageBox.Show(this, "����:\r\n" + strWarning);
                    string strErrorFilename = this.MainForm.DataDir + "\\~printbinding_" + "warning.txt";
                    StreamUtil.WriteText(strErrorFilename, "����:\r\n" + strWarning);
                    filenames.Insert(0, strErrorFilename);
                }

                HtmlPrintForm printform = new HtmlPrintForm();

                printform.Text = "��ӡװ����";
                printform.MainForm = this.MainForm;
                printform.Filenames = filenames;
                this.MainForm.AppInfo.LinkFormState(printform, "printbinding_htmlprint_formstate");
                printform.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(printform);

            }
            finally
            {
                if (filenames != null)
                {
                    Global.DeleteFiles(filenames);
                    filenames.Clear();
                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        // ��ӡһ���϶����װ����
        int PrintOneBinding(
            PrintBindingPrintOption option,
            Hashtable macro_table,
            ListViewItem item,
            int nIndex,
            out string strFilename,
            out string strWarning,
            out string strError)
        {
            strWarning = "";
            strError = "";
            strFilename = "";

            int nRet = 0;

            macro_table["%bindingissn%"] = ListViewUtil.GetItemText(item, COLUMN_ISBNISSN);
            macro_table["%bindingsummary%"] = ListViewUtil.GetItemText(item, COLUMN_SUMMARY);
            macro_table["%bindingaccessno%"] = ListViewUtil.GetItemText(item, COLUMN_ACCESSNO);

            // 2012/5/29
            string strBidningBarcode = ListViewUtil.GetItemText(item, COLUMN_BARCODE);
            string strBindingBarcodeStyle = "";
            if (this.checkBox_print_barcodeFix.Checked == true)
            {
                if (string.IsNullOrEmpty(strBidningBarcode) == false)
                    strBidningBarcode = "*" + strBidningBarcode + "*";

                // strStyle = " style=\"font-family: C39HrP24DhTt; \"";
            }
            macro_table["%bindingbarcode%"] = strBidningBarcode;

            macro_table["%bindingintact%"] = ListViewUtil.GetItemText(item, COLUMN_INTACT);
            macro_table["%bindingprice%"] = ListViewUtil.GetItemText(item, COLUMN_PRICE);
            macro_table["%bindingvolume%"] = ListViewUtil.GetItemText(item, COLUMN_VOLUME);
            macro_table["%bindingpublishtime%"] = ListViewUtil.GetItemText(item, COLUMN_PUBLISHTIME);
            macro_table["%bindinglocation%"] = ListViewUtil.GetItemText(item, COLUMN_LOCATION);
            macro_table["%bindingrefid%"] = ListViewUtil.GetItemText(item, COLUMN_REFID);

            // string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);
            // TODO: �����Ŀ��¼������ĳ�ָ�ʽ�������ĳЩ�����ݣ�

            if (this.MarcFilter != null)
            {
                string strMARC = "";
                string strOutMarcSyntax = "";

                // TODO: �д���Ҫ���Ա����������������ڴ�ӡ������ŷ��֣�������

                // ���MARC��ʽ��Ŀ��¼
                nRet = GetMarc(item,
                    out strMARC,
                    out strOutMarcSyntax,
                    out strError);
                if (nRet == -1)
                    return -1;

                /*
                // ����ϴ��������뵽macro_table�е�����
                foreach (string key in this.ColumnTable.Keys)
                {
                    macro_table.Remove("%" + key + "%");
                }
                 * */

                this.ColumnTable.Clear();   // �����һ��¼����ʱ���������

                // �ýű��ܹ���֪��׼���ĺ�
                foreach (string key in macro_table.Keys)
                {
                    this.ColumnTable.Add(key.Replace("%", ""), macro_table[key]);
                }

                // ����filter�е�Record��ض���
                nRet = this.MarcFilter.DoRecord(
                    null,
                    strMARC,
                    strOutMarcSyntax,
                    nIndex,
                    out strError);
                if (nRet == -1)
                    return -1;

                // ׷�ӵ�macro_table��
                foreach(string key in this.ColumnTable.Keys)
                {
                    macro_table.Remove("%" + key + "%");

                    macro_table.Add("%" + key + "%", this.ColumnTable[key]);
                }
            }

            // ��Ҫ�����ں϶�����ļ���ǰ׺������
            string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);
            string strFileNamePrefix = this.MainForm.DataDir + "\\~printbinding_" + strRecPath.Replace("/", "_") + "_";

            strFilename = strFileNamePrefix + "0" + ".html";

            // ��ǰ������Ա�������ݣ���Ϊ����ҲҪ˳�㴴�����ɺ�ͳ�������йصĺ�ֵ
            // ������ʱ�����
            string strMemberTableResult = "";
            // ����������Ա��������
            // return:
            //      ʵ�ʵ���ĳ�Ա����
            nRet = BuildMembersTable(
                option,
                macro_table,
                item,
                out strMemberTableResult,
                out strError);
            if (nRet == -1)
                return -1;

            if (nRet == 0)
            {
                strWarning = "�϶��� (��¼·��="+strRecPath+"; �ο�ID="
                    +ListViewUtil.GetItemText(item, COLUMN_REFID)+") ��û�а����κ�(ʵ����)��Ա��";
            }

            BuildPageTop(option,
                macro_table,
                strFilename);

            // ����ź�����
            {

                /*
                // �ڿ�����
                macro_table["%seriescount%"] = seller.Count.ToString();
                // ��ص�����
                macro_table["%issuecount%"] = GetIssueCount(seller).ToString();
                // ȱ�Ĳ���
                macro_table["%missingitemcount%"] = GetMissingItemCount(seller).ToString();
                */


                string strTemplateFilePath = option.GetTemplatePageFilePath("�϶�������");
                if (String.IsNullOrEmpty(strTemplateFilePath) == false)
                {
                    /*
                     * �ڿ��� ISSN 
                     * ��ȡ��
                     * �϶���Ĳ������
                     * �϶���������
                     * �϶���ļ۸�
                     * �������ڷ�Χ
                     * ȱ�ڷ�Χ
                     * */


                    /*
<div class='binding_table_title'>�϶���</div>
<table class='binding'>
<tr class='biblio'>
	<td class='name'>�ڿ�</td>
	<td class='value'>%bindingsummary%</td>
</tr>
<tr class='issn'>
	<td class='name'>ISSN</td>
	<td class='value'>%bindingissn%</td>
</tr>
<tr class='accessno'>
	<td class='name'>��ȡ��</td>
	<td class='value'>%bindingaccessno%</td>
</tr>
<tr class='location'>
	<td class='name'>�ݲصص�</td>
	<td class='value'>%bindinglocation%</td>
</tr>
<tr class='barcode'>
	<td class='name'>�������</td>
	<td class='value'>%bindingbarcode%</td>
</tr>
<tr class='refid'>
	<td class='name'>�ο�ID</td>
	<td class='value'>%bindingrefid%</td>
</tr>
<tr class='intact'>
	<td class='name'>�����</td>
	<td class='value'>%bindingintact%</td>
</tr>
<tr class='bindingprice'>
	<td class='name'>�϶��۸�</td>
	<td class='value'>%bindingprice%</td>
</tr>
<tr class='publishtime'>
	<td class='name'>����ʱ��</td>
	<td class='value'>%bindingpublishtime%</td>
</tr>
<tr class='bindingissuecount'>
	<td class='name'>����</td>
	<td class='value'>ʵ����: %arrivecount%; ȱ����: %missingcount%; ������: %issuecount%; ȱ�ں�: %missingvolume%</td>
</tr>
<tr class='volume'>
	<td class='name'>�����ں�</td>
	<td class='value'>%bindingvolume%</td>
</tr>
</table>
                     * */

                    // ����ģ���ӡ
                    string strContent = "";
                    // ���Զ�ʶ���ļ����ݵı��뷽ʽ�Ķ����ı��ļ�����ģ��
                    // return:
                    //      -1  ����
                    //      0   �ļ�������
                    //      1   �ļ�����
                    nRet = Global.ReadTextFileContent(strTemplateFilePath,
                        out strContent,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    string strResult = Global.MacroString(macro_table,
                        strContent);
                    StreamUtil.WriteText(strFilename,
                        strResult);
                }
                else
                {
                    // ȱʡ�Ĺ̶����ݴ�ӡ

                    string strTableTitle = option.TableTitle;

                    if (String.IsNullOrEmpty(strTableTitle) == false)
                    {
                        strTableTitle = Global.MacroString(macro_table,
                            strTableTitle);
                    }

                    // ���ʼ
                    StreamUtil.WriteText(strFilename,
                        "<div class='binding_table_title'>" + HttpUtility.HtmlEncode(strTableTitle) + "</div>");


                    // ���ʼ
                    StreamUtil.WriteText(strFilename,
                        "<table class='binding'>");

                    // �ڿ���Ϣ
                    StreamUtil.WriteText(strFilename,
                        "<tr class='biblio'>");
                    StreamUtil.WriteText(strFilename,
                       "<td class='name'>�ڿ�</td>");
                    StreamUtil.WriteText(strFilename,
                      "<td class='value'>"+HttpUtility.HtmlEncode(
                      (string)macro_table["%bindingsummary%"]) + "</td>");
                    StreamUtil.WriteText(strFilename,
                        "</tr>");

                    // ISSN
                    StreamUtil.WriteText(strFilename,
                        "<tr class='issn'>");
                    StreamUtil.WriteText(strFilename,
                       "<td class='name'>ISSN</td>");
                    StreamUtil.WriteText(strFilename,
                      "<td class='value'>" + HttpUtility.HtmlEncode(
                      (string)macro_table["%bindingissn%"]) + "</td>");
                    StreamUtil.WriteText(strFilename,
                        "</tr>");


                    // ��ȡ��
                    StreamUtil.WriteText(strFilename,
                        "<tr class='accessno'>");
                    StreamUtil.WriteText(strFilename,
                       "<td class='name'>��ȡ��</td>");
                    StreamUtil.WriteText(strFilename,
                      "<td class='value'>" + HttpUtility.HtmlEncode(
                      (string)macro_table["%bindingaccessno%"]) + "</td>");
                    StreamUtil.WriteText(strFilename,
                        "</tr>");

                    // �ݲصص�
                    StreamUtil.WriteText(strFilename,
                        "<tr class='location'>");
                    StreamUtil.WriteText(strFilename,
                       "<td class='name'>�ݲصص�</td>");
                    StreamUtil.WriteText(strFilename,
                      "<td class='value'>" + HttpUtility.HtmlEncode(
                      (string)macro_table["%bindinglocation%"]) + "</td>");
                    StreamUtil.WriteText(strFilename,
                        "</tr>");

                    // �����
                    StreamUtil.WriteText(strFilename,
                        "<tr class='barcode'>");
                    StreamUtil.WriteText(strFilename,
                       "<td class='name'>�������</td>");

                    StreamUtil.WriteText(strFilename,
                      "<td class='value' " + strBindingBarcodeStyle + " >" + HttpUtility.HtmlEncode((string)macro_table["%bindingbarcode%"])
                        + "</td>");
                    StreamUtil.WriteText(strFilename,
                        "</tr>");

                    // �ο�ID
                    StreamUtil.WriteText(strFilename,
                        "<tr class='refid'>");
                    StreamUtil.WriteText(strFilename,
                       "<td class='name'>�ο�ID</td>");
                    StreamUtil.WriteText(strFilename,
                      "<td class='value'>" + HttpUtility.HtmlEncode(
                      (string)macro_table["%bindingrefid%"]) + "</td>");
                    StreamUtil.WriteText(strFilename,
                        "</tr>");

                    // �����
                    StreamUtil.WriteText(strFilename,
                        "<tr class='intact'>");
                    StreamUtil.WriteText(strFilename,
                       "<td class='name'>�����</td>");
                    StreamUtil.WriteText(strFilename,
                      "<td class='value'>" + HttpUtility.HtmlEncode(
                      (string)macro_table["%bindingintact%"]) + "</td>");
                    StreamUtil.WriteText(strFilename,
                        "</tr>");

                    // �۸�
                    StreamUtil.WriteText(strFilename,
                        "<tr class='bindingprice'>");
                    StreamUtil.WriteText(strFilename,
                       "<td class='name'>�϶��۸�</td>");
                    StreamUtil.WriteText(strFilename,
                      "<td class='value'>" + HttpUtility.HtmlEncode(
                      (string)macro_table["%bindingprice%"]) + "</td>");
                    StreamUtil.WriteText(strFilename,
                        "</tr>");

                    // ����ʱ��
                    StreamUtil.WriteText(strFilename,
                        "<tr class='publishtime'>");
                    StreamUtil.WriteText(strFilename,
                       "<td class='name'>����ʱ��</td>");
                    StreamUtil.WriteText(strFilename,
                      "<td class='value'>" + HttpUtility.HtmlEncode(
                      (string)macro_table["%bindingpublishtime%"]) + "</td>");
                    StreamUtil.WriteText(strFilename,
                        "</tr>");

                    // ����
                    string strValue = "ʵ����: " + (string)macro_table["%arrivecount%"] + "; "
                    + "ȱ����: " + (string)macro_table["%missingcount%"] + "; "
                    + "������: " + (string)macro_table["%issuecount%"];
                    string strMissingVolume = (string)macro_table["%missingvolume%"];
                    if (String.IsNullOrEmpty(strMissingVolume) == false)
                        strValue += "; ȱ�ں�: " + strMissingVolume;
                    StreamUtil.WriteText(strFilename,
                        "<tr class='bindingissuecount'>");
                    StreamUtil.WriteText(strFilename,
                       "<td class='name'>����</td>");
                    StreamUtil.WriteText(strFilename,
                      "<td class='value'>" + HttpUtility.HtmlEncode(strValue) + "</td>");
                    StreamUtil.WriteText(strFilename,
                        "</tr>");


                    // ��������
                    StreamUtil.WriteText(strFilename,
                        "<tr class='volume'>");
                    StreamUtil.WriteText(strFilename,
                       "<td class='name'>�����ں�</td>");
                    StreamUtil.WriteText(strFilename,
                      "<td class='value'>" + HttpUtility.HtmlEncode(
                      (string)macro_table["%bindingvolume%"]) + "</td>");
                    StreamUtil.WriteText(strFilename,
                        "</tr>");

                    // ������
                    StreamUtil.WriteText(strFilename,
                        "</table>");
                }

            }

            // ��ʱ��������Ա�������
            StreamUtil.WriteText(strFilename,
                strMemberTableResult);


            BuildPageBottom(option,
                macro_table,
                strFilename);

            return 0;
        }

        // ���css�ļ���·��(����http:// ��ַ)���������Ƿ���С�ͳ��ҳ�����Զ�����
        // parameters:
        //      strDefaultCssFileName   ��css��ģ��ȱʡ����£������õ�����Ŀ¼�е�css�ļ��������ļ���
        string GetAutoCssUrl(PrintOption option,
            string strDefaultCssFileName)
        {
            string strCssFilePath = option.GetTemplatePageFilePath("css");  // ��Сд������
            if (String.IsNullOrEmpty(strCssFilePath) == false)
                return strCssFilePath;
            else
            {
                // return this.MainForm.LibraryServerDir + "/" + strDefaultCssFileName;    // ȱʡ��
                return PathUtil.MergePath(this.MainForm.DataDir, strDefaultCssFileName);    // ȱʡ��
            }
        }

        int BuildPageTop(PrintOption option,
    Hashtable macro_table,
    string strFileName)
        {
            string strCssUrl = GetAutoCssUrl(option, "printbinding.css");
            string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";

            StreamUtil.WriteText(strFileName,
                "<!DOCTYPE html PUBLIC \" -//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">"
                + "<html><head>" + strLink + "</head><body>");

            // ҳü
            string strPageHeaderText = option.PageHeader;

            if (String.IsNullOrEmpty(strPageHeaderText) == false)
            {
                strPageHeaderText = Global.MacroString(macro_table,
                    strPageHeaderText);

                StreamUtil.WriteText(strFileName,
                    "<div class='pageheader'>" + strPageHeaderText + "</div>");
            }

            /*
            // ��������
            StreamUtil.WriteText(strFileName,
    "<div class='seller'>" + GetPureSellerName(seller.Seller) + "</div>");
             * */

            return 0;
        }


        int BuildPageBottom(PrintOption option,
            Hashtable macro_table,
            string strFileName)
        {

            // ҳ��
            string strPageFooterText = option.PageFooter;

            if (String.IsNullOrEmpty(strPageFooterText) == false)
            {
                strPageFooterText = Global.MacroString(macro_table,
                    strPageFooterText);

                StreamUtil.WriteText(strFileName,
        "<div class='pagefooter'>" + strPageFooterText + "</div>");
            }

            StreamUtil.WriteText(strFileName, "</body></html>");

            return 0;
        }

        // ���������ںš����ںš���ŵ��ַ���
        // ע������һ������汾���ܹ�ʶ�������"y."
        /*public*/ static void ParseItemVolumeString(string strVolumeString,
            out string strYear,
            out string strIssue,
            out string strZong,
            out string strVolume)
        {
            strYear = "";
            strIssue = "";
            strZong = "";
            strVolume = "";

            string[] segments = strVolumeString.Split(new char[] { ';',',','=' });
            for (int i = 0; i < segments.Length; i++)
            {
                string strSegment = segments[i].Trim();

                if (StringUtil.HasHead(strSegment, "y.") == true)
                    strYear = strSegment.Substring(2).Trim();
                else if (StringUtil.HasHead(strSegment, "no.") == true)
                    strIssue = strSegment.Substring(3).Trim();
                else if (StringUtil.HasHead(strSegment, "��.") == true)
                    strZong = strSegment.Substring(2).Trim();
                else if (StringUtil.HasHead(strSegment, "v.") == true)
                    strVolume = strSegment.Substring(2).Trim();
            }
        }

        // ������ʾ��Χ�� �ںţ���ţ����ں��ַ���
        // �������м��õȺ�����
        /*public*/ static string BuildVolumeRangeString(List<string> volumes)
        {
            Hashtable no_list_table = new Hashtable();
            List<string> volumn_list = new List<string>();
            List<string> zong_list = new List<string>();

            for(int i=0;i<volumes.Count;i++)
            {
                // ���������volumestring
                string strYear = "";
                string strNo = "";
                string strZong = "";
                string strSingleVolume = "";

                ParseItemVolumeString(volumes[i],
                    out strYear,
                    out strNo,
                    out strZong,
                    out strSingleVolume);

                List<string> no_list = (List<string>)no_list_table[strYear];
                if (no_list == null)
                {
                    no_list = new List<string>();
                    no_list_table[strYear] = no_list;
                }

                no_list.Add(strNo);
                volumn_list.Add(strSingleVolume);
                zong_list.Add(strZong);
            }


            List<string> keys = new List<string>();
            foreach (string key in no_list_table.Keys)
            {
                keys.Add(key);
            }
            keys.Sort();

            string strNoString = "";
            for (int i = 0; i < keys.Count; i++)
            {
                string strYear = keys[i];
                List<string> no_list = (List<string>)no_list_table[strYear];
                Debug.Assert(no_list != null);

                if (String.IsNullOrEmpty(strNoString) == false)
                    strNoString += ";";
                strNoString += (String.IsNullOrEmpty(strYear) == false ? strYear + ":" : "")
                    + "no." + Global.BuildNumberRangeString(no_list);
            }

            string strVolumnString = Global.BuildNumberRangeString(volumn_list);
            string strZongString = Global.BuildNumberRangeString(zong_list);

            string strValue = strNoString;

            if (String.IsNullOrEmpty(strZongString) == false)
            {
                if (String.IsNullOrEmpty(strValue) == false)
                    strValue += "=";
                strValue += "��." + strZongString;
            }

            if (String.IsNullOrEmpty(strVolumnString) == false)
            {
                if (String.IsNullOrEmpty(strValue) == false)
                    strValue += "=";
                strValue += "v." + strVolumnString;
            }

            return strValue;
        }

        // ����������Ա��������
        // return:
        //      ʵ�ʵ���ĳ�Ա����
        int BuildMembersTable(
            // string strFilename,
            PrintOption option,
            Hashtable macro_table,
            ListViewItem parent_item,
            out string strResult,
            out string strError)
        {
            strError = "";
            strResult = "";


            OriginItemData data = (OriginItemData)parent_item.Tag;
            Debug.Assert(data != null, "");

            if (String.IsNullOrEmpty(data.Xml) == true)
            {
                strError = "data.XmlΪ��";
                return -1;
            }

            // ��item record xmlװ��DOM��Ȼ��select��ÿ��<item>Ԫ��
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(data.Xml);
            }
            catch (Exception ex)
            {
                string strRecPath = ListViewUtil.GetItemText(parent_item, COLUMN_RECPATH);
                strError = "·��Ϊ '" + strRecPath + "' �Ĳ��¼XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("binding/item");
            if (nodes.Count == 0)
                return 0;

            int nArriveCount = 0;   // ����(��������װ����)�Ĳ���
            int nMissingCount = 0;  // ȱ�ڵĲ���
            int nIssueCount = nodes.Count;   // ������Ӧ�����ٲ�

            // ���ʼ
            strResult +=
                "<div class='members_table_title'>��������</div>";


            // ���ʼ
            strResult +=
                "<table class='members'>";

            // ��Ŀ����
            strResult +=
                "<tr class='column'>";

            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];

                string strCaption = column.Caption;

                // ���û��caption���壬��Ų��name����
                if (String.IsNullOrEmpty(strCaption) == true)
                    strCaption = column.Name;

                string strClass = StringUtil.GetLeft(column.Name);

            strResult +=
                    "<td class='" + strClass + "'>" + strCaption + "</td>";
            }

            strResult += "</tr>";

            List<string> missing_volumes = new List<string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                Hashtable value_table = new Hashtable();

                bool bMissing = false;
                // ��ò����͵����Բ���ֵ
                // return:
                //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
                //      0   ���������ȷ����Ĳ���ֵ
                //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
                DomUtil.GetBooleanParam(node,
                    "missing",
                    false,
                    out bMissing,
                    out strError);
                /*
                if (bMissing == true)
                    continue;
                 * */

                if (bMissing == true)
                    nMissingCount++;
                else
                    nArriveCount++;

                string strRefID = DomUtil.GetAttr(node, "refID");
                string strVolumeString = DomUtil.GetAttr(node, "volume");
                string strPublishTime = DomUtil.GetAttr(node, "publishTime");
                string strBarcode = DomUtil.GetAttr(node, "barcode");
                string strRegisterNo = DomUtil.GetAttr(node, "registerNo");

                if (bMissing == true)
                {
                    // ��
                    string strYear = IssueUtil.GetYearPart(strPublishTime);
                    missing_volumes.Add("y." + strYear + "," + strVolumeString);
                }

                value_table["%missing%"] = bMissing == true ? "ȱ" : "";
                value_table["%refID%"] = strRefID;
                value_table["%volume%"] = strVolumeString;
                value_table["%publishTime%"] = BindingControl.GetDisplayPublishTime(strPublishTime);
                if (String.IsNullOrEmpty(strBarcode) == false)
                    value_table["%barcode%"] = strBarcode;
                if (String.IsNullOrEmpty(strRegisterNo) == false)
                    value_table["%registerNo%"] = strRegisterNo;

                if (bMissing == true)
                    strResult += "<tr class='content missing'>";
                else
                    strResult += "<tr class='content'>";

                for (int j = 0; j < option.Columns.Count; j++)
                {
                    Column column = option.Columns[j];

                    List<Hashtable> value_tables = new List<Hashtable>();
                    value_tables.Add(value_table);
                    value_tables.Add(macro_table);
                    value_tables.Add(this.ColumnTable);

                    string strContent = GetColumnContent(value_tables,
                        strRefID,
                        StringUtil.GetLeft(column.Name));

                    string strClass = StringUtil.GetLeft(column.Name);
            strResult +=
                        "<td class='" + strClass + "'>" + strContent + "</td>";

                }

                strResult += "</tr>";
            }


            // ������
            strResult +=
                "</table>";

            macro_table["%arrivecount%"] = nArriveCount.ToString();
            macro_table["%missingcount%"] = nMissingCount.ToString();
            macro_table["%issuecount%"] = nIssueCount.ToString();
            macro_table["%missingvolume%"] = BuildVolumeRangeString(missing_volumes);

            return nArriveCount;
        }

        /*public*/ int GetItemXmlByRefID(
            string strRefID,
            out string strItemXml,
            out string strError)
        {
            strError = "";
            strItemXml = "";

            string strBiblioText = "";
            string strItemRecPath = "";
            string strBiblioRecPath = "";
            byte[] item_timestamp = null;
            string strBarcode = "@refID:" + strRefID;

            if (this.stop != null)
                this.stop.SetMessage("���ڻ�ȡ�ο�IDΪ '"+strRefID+"' ��ʵ���¼... ");

            long lRet = Channel.GetItemInfo(
                stop,
                strBarcode,
                "xml",
                out strItemXml,
                out strItemRecPath,
                out item_timestamp,
                "recpath",
                out strBiblioText,
                out strBiblioRecPath,
                out strError);

            if (this.stop != null)
                this.stop.SetMessage("");

            return (int)lRet;
        }

        // �����Ŀ����
        // paramters:
        //      strRefID    ��Ա��Ĳο�ID
        string GetColumnContent(List<Hashtable> value_tables,
            string strRefID,
            string strColumnName)
        {
            string strName = StringUtil.GetLeft(strColumnName);

            for (int i = 0; i < value_tables.Count; i++)
            {
                Hashtable value_table = value_tables[i];
                if (value_table.ContainsKey(strName) == true)
                    return (string)value_table[strName];

                if (strName.Length > 0 && strName[0] != '%')
                {
                    string strTemp = "%" + strName + "%";
                    if (value_table.ContainsKey(strTemp) == true)
                        return (string)value_table[strTemp];
                }
            }

            // 
            if (String.IsNullOrEmpty(strRefID) == true)
                return "";

            // TODO: �ȼ��strName�Ƿ���ʵ���¼������ֶ���֮��

            int nRet = 0;
            string strError = "";


            string strItemXml = "";
            XmlDocument dom = (XmlDocument)this.ItemXmlTable[strRefID];
            if (dom == null)
            {
                if (this.ItemXmlTable.ContainsKey(strRefID) == true)
                    return "";  // �������Ѿ����������Ŀ����Ϊ��ǰ�ҹ�����û���ҵ����߳���

                nRet = GetItemXmlByRefID(
                    strRefID,
                    out strItemXml,
                    out strError);
                if (nRet == 0 || nRet == -1)
                {
                    this.ItemXmlTable[strRefID] = null;
                    if (nRet == -1)
                        return "error: " + strError;
                    Debug.Assert(nRet == 0, "");
                    return "";  // û���ҵ�ʵ���¼
                }
                else
                {
                    dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strItemXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "ItemXmlװ��DOMʱ����: " + ex.Message;
                        return strError;
                    }

                    // ��ֹ����������
                    if (this.ItemXmlTable.Count > 100)
                        this.ItemXmlTable.Clear();

                    this.ItemXmlTable[strRefID] = dom;
                }
            }

            Debug.Assert(dom != null, "");
            return DomUtil.GetElementText(dom.DocumentElement,
                strName);
        }


        // ���MARC��ʽ��Ŀ��¼
        int GetMarc(ListViewItem item,
            out string strMARC,
            out string strOutMarcSyntax,
            out string strError)
        {
            strError = "";
            strMARC = "";
            strOutMarcSyntax = "";
            int nRet = 0;

            string[] formats = new string[1];
            formats[0] = "xml";

            string[] results = null;
            byte[] timestamp = null;

            string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);

            Debug.Assert(String.IsNullOrEmpty(strBiblioRecPath) == false, "strBiblioRecPathֵ����Ϊ��");

            long lRet = Channel.GetBiblioInfos(
                    null, // stop,
                    strBiblioRecPath,
                    "",
                    formats,
                    out results,
                    out timestamp,
                    out strError);
            if (lRet == -1 || lRet == 0)
            {
                if (lRet == 0 && String.IsNullOrEmpty(strError) == true)
                    strError = "��Ŀ��¼ '" + strBiblioRecPath + "' ������";

                strError = "�����Ŀ��¼ʱ��������: " + strError;
                return -1;
            }

            string strXml = results[0];

            // ת��ΪMARC��ʽ
            // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
            // parameters:
            //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
            //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
            //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
            nRet = MarcUtil.Xml2Marc(strXml,
                true,   // 2013/1/12 �޸�Ϊtrue
                "", // strMarcSyntax
                out strOutMarcSyntax,
                out strMARC,
                out strError);
            if (nRet == -1)
                return -1;

            return 1;
        }

        private void button_print_option_Click(object sender, EventArgs e)
        {
            // ���ñ���ͷ��
            string strNamePath = "printbinding_printoption";
            string strPubType = "����������";

            PrintBindingPrintOption option = new PrintBindingPrintOption(this.MainForm.DataDir,
                strPubType);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);


            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.MainForm = this.MainForm;
            dlg.DataDir = this.MainForm.DataDir;
            dlg.Text = strPubType + " װ���� ��ӡ����";
            dlg.PrintOption = option;
            dlg.ColumnItems = new string[] {
                "missing -- ȱ��״̬",
                "publishTime -- ��������",
                "volume -- ���ں�",
                "barcode -- �������",
                "intact -- �����",
                "refID -- �ο�ID",
            };


            this.MainForm.AppInfo.LinkFormState(dlg, "printbinding_printoption_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            option.SaveData(this.MainForm.AppInfo,
                strNamePath);
        }

        /// <summary>
        /// ������Ϣ��
        /// </summary>
        public HtmlViewerForm ErrorInfoForm = null;

        // ��ô�����Ϣ��
        HtmlViewerForm GetErrorInfoForm()
        {
            if (this.ErrorInfoForm == null
                || this.ErrorInfoForm.IsDisposed == true
                || this.ErrorInfoForm.IsHandleCreated == false)
            {
                this.ErrorInfoForm = new HtmlViewerForm();
                this.ErrorInfoForm.ShowInTaskbar = false;
                this.ErrorInfoForm.Text = "������Ϣ";
                this.ErrorInfoForm.Show(this);
                this.ErrorInfoForm.WriteHtml("<pre>");  // ׼���ı����
            }

            return this.ErrorInfoForm;
        }

        void ClearErrorInfoForm()
        {
            // ���������Ϣ�����в��������
            if (this.ErrorInfoForm != null)
            {
                try
                {
                    this.ErrorInfoForm.HtmlString = "<pre>";
                }
                catch
                {
                }
            }
        }

        // ���·����������Ŀ���Ƿ�Ϊͼ��/�ڿ��⣿
        // return:
        //      -1  error
        //      0   ������Ҫ����ʾ��Ϣ��strError��
        //      1   ����Ҫ��
        int CheckItemRecPath(string strLoadType,
            string strItemRecPath,
            out string strError)
        {
            strError = "";

            string strItemDbName = Global.GetDbName(strItemRecPath);
            string strBiblioDbName = this.MainForm.GetBiblioDbNameFromItemDbName(strItemDbName);
            if (String.IsNullOrEmpty(strBiblioDbName) == true)
            {
                strError = "ʵ��� '" + strItemDbName + "' δ�ҵ���Ӧ����Ŀ����";
                return -1;
            }

            string strIssueDbName = this.MainForm.GetIssueDbName(strBiblioDbName);

            if (strLoadType == "ͼ��")
            {
                if (String.IsNullOrEmpty(strIssueDbName) == false)
                {
                    strError = "·�� '" + strItemRecPath + "' ����������Ŀ�� '" + strBiblioDbName + "' Ϊ�ڿ��ͣ��͵�ǰ���������� '" + strLoadType + "' ��һ��";
                    return 0;
                }
                return 1;
            }

            if (strLoadType == "����������")
            {
                if (String.IsNullOrEmpty(strIssueDbName) == true)
                {
                    strError = "·�� '" + strItemRecPath + "' ����������Ŀ�� '" + strBiblioDbName + "' Ϊͼ���ͣ��͵�ǰ���������� '" + strLoadType + "' ��һ��";
                    return 0;
                }
                return 1;
            }

            strError = "CheckItemRecPath() δ֪�ĳ��������� '" + strLoadType + "'";
            return -1;
        }

        private void button_load_loadFromRecPathFile_Click(object sender, EventArgs e)
        {
            int nRet = 0;

            this.ClearErrorInfoForm();

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵ļ�¼·���ļ���";
            dlg.FileName = this.RecPathFilePath;
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.SourceStyle = "recpathfile";

            int nDupCount = 0;

            string strError = "";
            StreamReader sr = null;
            try
            {
                // ���ļ�
                sr = new StreamReader(dlg.FileName);

                EnableControls(false);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڳ�ʼ���������� ...");
                stop.BeginLoop();
                this.Update();
                this.MainForm.Update();

                try
                {
                    if (Control.ModifierKeys == Keys.Control)
                    {
                    }
                    else
                    {
                        this.listView_parent.Items.Clear();
                        this.SortColumns_parent.Clear();
                        SortColumns.ClearColumnSortDisplay(this.listView_parent.Columns);

                        /*
                        this.refid_table.Clear();
                        this.orderxml_table.Clear();
                         * */
                    }


                    // this.m_nGreenItemCount = 0;

                    // ���ж����ļ�����
                    // �����ļ�����
                    int nLineCount = 0;
                    for (; ; )
                    {
                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�1";
                                goto ERROR1;
                            }
                        }


                        string strLine = "";
                        strLine = sr.ReadLine();

                        if (strLine == null)
                            break;

                        nLineCount++;
                    }

                    // ���ý��ȷ�Χ
                    stop.SetProgressRange(0, nLineCount);

                    sr.Close();

                    sr = new StreamReader(dlg.FileName);
                    for (int i = 0; ; i++)
                    {
                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�1";
                                goto ERROR1;
                            }
                        }

                        string strLine = "";
                        strLine = sr.ReadLine();

                        stop.SetProgressValue(i);

                        if (strLine == null)
                            break;

                        strLine = strLine.Trim();
                        if (String.IsNullOrEmpty(strLine) == true)
                            continue;

                        if (strLine[0] == '#')
                            continue;   // ע����

                        // ���·����������Ŀ���Ƿ�Ϊͼ��/�ڿ��⣿
                        // return:
                        //      -1  error
                        //      0   ������Ҫ����ʾ��Ϣ��strError��
                        //      1   ����Ҫ��
                        nRet = CheckItemRecPath("����������",
                            strLine,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 0)
                        {
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                        }

                        stop.SetMessage("����װ��·�� " + strLine + " ��Ӧ�ļ�¼...");


                        string strOutputItemRecPath = "";
                        // ���ݲ�����ţ�װ����¼
                        // return: 
                        //      -2  ��������Ѿ���list�д�����
                        //      -1  ����
                        //      1   �ɹ�
                        nRet = LoadOneItem(
                            "@path:" + strLine,
                            this.listView_parent,
                            null,
                            out strOutputItemRecPath,
                            out strError);
                        if (nRet == -2)
                            nDupCount++;
                    }
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("װ����ɡ�");
                    stop.HideProgress();

                    EnableControls(true);
                }
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                goto ERROR1;
            }
            finally
            {
                sr.Close();
            }

            // �����ļ���
            this.RecPathFilePath = dlg.FileName;
            // this.Text = "��ӡװ���� " + Path.GetFileName(this.RecPathFilePath);
            this.Text = "��ӡװ���� -- " + this.SourceDescription;

            if (nDupCount != 0)
            {
                MessageBox.Show(this, "װ��������� " + nDupCount.ToString() + "���ظ�������ԡ�");
            }

            // �㱨����װ�������
            // return:
            //      0   ��δװ���κ�����    
            //      1   װ���Ѿ����
            //      2   ��Ȼװ�������ݣ����������д�������
            int nState = ReportLoadState(out strError);
            if (nState != 1)
                goto ERROR1;
            return;
        ERROR1:
            this.Text = "��ӡװ����";
            MessageBox.Show(this, strError);
        }

        private void button_load_loadFromBarcodeFile_Click(object sender, EventArgs e)
        {
            int nRet = 0;

            this.ClearErrorInfoForm();

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵�������ļ���";
            dlg.FileName = this.BarcodeFilePath;
            // dlg.InitialDirectory = 
            dlg.Filter = "������ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.SourceStyle = "barcodefile";

            int nDupCount = 0;

            string strError = "";
            StreamReader sr = null;
            try
            {
                // ���ļ�
                sr = new StreamReader(dlg.FileName);


                EnableControls(false);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڳ�ʼ���������� ...");
                stop.BeginLoop();
                this.Update();
                this.MainForm.Update();

                try
                {
                    if (Control.ModifierKeys == Keys.Control)
                    {
                    }
                    else
                    {
                        this.listView_parent.Items.Clear();
                        this.SortColumns_parent.Clear();
                        SortColumns.ClearColumnSortDisplay(this.listView_parent.Columns);

                        /*
                        this.refid_table.Clear();
                        this.orderxml_table.Clear();
                         * */
                    }

                    // ���ж����ļ�����
                    // �����ļ�����
                    int nLineCount = 0;
                    for (; ; )
                    {
                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�1";
                                goto ERROR1;
                            }
                        }


                        string strLine = "";
                        strLine = sr.ReadLine();

                        if (strLine == null)
                            break;

                        nLineCount++;
                    }

                    // ���ý��ȷ�Χ
                    stop.SetProgressRange(0, nLineCount);

                    sr.Close();

                    sr = new StreamReader(dlg.FileName);

                    for (int i = 0; ; i++)
                    {
                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�1";
                                goto ERROR1;
                            }
                        }

                        string strLine = "";
                        strLine = sr.ReadLine();

                        stop.SetProgressValue(i);

                        if (strLine == null)
                            break;

                        strLine = strLine.Trim();
                        if (String.IsNullOrEmpty(strLine) == true)
                            continue;

                        if (strLine[0] == '#')
                            continue;   // ע����

                        stop.SetMessage("����װ�������� " + strLine + " ��Ӧ�ļ�¼...");


                        string strOutputItemRecPath = "";
                        // ���ݲ�����ţ�װ����¼
                        // return: 
                        //      -2  ��������Ѿ���list�д�����
                        //      -1  ����
                        //      1   �ɹ�
                        nRet = LoadOneItem(
                            strLine,
                            this.listView_parent,
                            null,
                            out strOutputItemRecPath,
                            out strError);
                        if (nRet == -2)
                            nDupCount++;

                        // ���·����������Ŀ���Ƿ�Ϊͼ��/�ڿ��⣿
                        // return:
                        //      -1  error
                        //      0   ������Ҫ����ʾ��Ϣ��strError��
                        //      1   ����Ҫ��
                        nRet = CheckItemRecPath("����������",
                            strOutputItemRecPath,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 0)
                        {
                            GetErrorInfoForm().WriteHtml("�������Ϊ " + strLine + " �Ĳ��¼ " + strError + "\r\n");
                        }
                    }
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("װ����ɡ�");
                    stop.HideProgress();

                    EnableControls(true);
                }
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                goto ERROR1;
            }
            finally
            {
                sr.Close();
            }

            // �����ļ���
            this.BarcodeFilePath = dlg.FileName;
            // this.Text = "��ӡװ���� " + Path.GetFileName(this.BarcodeFilePath);
            this.Text = "��ӡװ���� -- " + this.SourceDescription;

            if (nDupCount != 0)
            {
                MessageBox.Show(this, "װ��������� " + nDupCount.ToString() + "���ظ�����������ԡ�");
            }

            // �㱨����װ�������
            // return:
            //      0   ��δװ���κ�����    
            //      1   װ���Ѿ����
            //      2   ��Ȼװ�������ݣ����������д�������
            int nState = ReportLoadState(out strError);
            if (nState != 1)
                goto ERROR1;
            return;
        ERROR1:
            this.Text = "��ӡװ����";
            MessageBox.Show(this, strError);
        }

        void FillMemberListViewItems(ListViewItem parent_item)
        {
            string strError = "";

            this.listView_member.Items.Clear();

            OriginItemData data = (OriginItemData)parent_item.Tag;

            // ��item record xmlװ��DOM��Ȼ��select��ÿ��<item>Ԫ��
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(data.Xml);
            }
            catch (Exception ex)
            {
                string strRecPath = ListViewUtil.GetItemText(parent_item, COLUMN_RECPATH);
                strError = "·��Ϊ '" + strRecPath + "' �Ĳ��¼XMLװ��DOMʱ����: " + ex.Message;
                ListViewItem item = new ListViewItem();
                item.Text = strError;
                item.ImageIndex = TYPE_ERROR;
                this.listView_member.Items.Add(item);
                return;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("binding/item");
            if (nodes.Count == 0)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                bool bMissing = false;
                // ��ò����͵����Բ���ֵ
                // return:
                //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
                //      0   ���������ȷ����Ĳ���ֵ
                //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
                DomUtil.GetBooleanParam(node,
                    "missing",
                    false,
                    out bMissing,
                    out strError);
                string strRefID = DomUtil.GetAttr(node, "refID");
                string strVolumeString = DomUtil.GetAttr(node, "volume");
                string strPublishTime = DomUtil.GetAttr(node, "publishTime");
                string strBarcode = DomUtil.GetAttr(node, "barcode");
                string strRegisterNo = DomUtil.GetAttr(node, "registerNo");

                ListViewItem item = new ListViewItem();
                item.ImageIndex = TYPE_NORMAL;
                this.listView_member.Items.Add(item);

                if (bMissing == true)
                    ListViewUtil.ChangeItemText(item, COLUMN_STATE, "ȱ");

                ListViewUtil.ChangeItemText(item, COLUMN_REFID, strRefID);
                ListViewUtil.ChangeItemText(item, COLUMN_VOLUME, strVolumeString);
                ListViewUtil.ChangeItemText(item, COLUMN_PUBLISHTIME, strPublishTime);
                ListViewUtil.ChangeItemText(item, COLUMN_BARCODE, strBarcode);
                ListViewUtil.ChangeItemText(item, COLUMN_REGISTERNO, strRegisterNo);
            }

        }

        private void listView_parent_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView_parent.SelectedItems.Count == 1)
            {
                FillMemberListViewItems(this.listView_parent.SelectedItems[0]);
            }
            else
            {
                this.listView_member.Items.Clear();
            }
        }

        private void listView_parent_DoubleClick(object sender, EventArgs e)
        {
            LoadToEntityForm(this.listView_parent);

        }

        private void listView_member_DoubleClick(object sender, EventArgs e)
        {
            LoadToEntityForm(this.listView_member);
        }

        void LoadToEntityForm(ListView list)
        {
            if (list.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ�ص�����");
                return;
            }

            string strBarcode = list.SelectedItems[0].Text;
            string strRecPath = ListViewUtil.GetItemText(list.SelectedItems[0], COLUMN_RECPATH);
            string strRefID = ListViewUtil.GetItemText(list.SelectedItems[0], COLUMN_REFID);

            EntityForm form = new EntityForm();

            form.MainForm = this.MainForm;
            form.MdiParent = this.MainForm;
            form.Show();

            if (String.IsNullOrEmpty(strBarcode) == false)
                form.LoadItemByBarcode(strBarcode, false);
            else if (String.IsNullOrEmpty(strRecPath) == false)
                form.LoadItemByRecPath(strRecPath, false);
            else if (String.IsNullOrEmpty(strRefID) == false)
                form.LoadItemByRefID(strRefID, false);
            else
            {
                form.Close();
                MessageBox.Show(this, "��ѡ���е�����š���¼·�����ο�IDȫ��Ϊ�գ��޷���λ��¼");
            }
        }

        private void listView_parent_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            int nSelectedCount = this.listView_parent.SelectedItems.Count;

            // 
            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Click += new System.EventHandler(this.menu_parent_selectAll_Click);
            if (nSelectedCount == this.listView_parent.Items.Count)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ѡ��ȫ������״̬������(&E)");
            menuItem.Click += new System.EventHandler(this.menu_parent_selectAllErrorLines_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // 
            menuItem = new MenuItem("�Ƴ�(&R)");
            menuItem.Click += new System.EventHandler(this.menu_parent_removeSelectedLines_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.listView_parent, new Point(e.X, e.Y));		

        }

        // �Ƴ�ѡ������
        void menu_parent_removeSelectedLines_Click(object sender, EventArgs e)
        {
            if (this.listView_parent.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ���κ���Ҫ�Ƴ�������");
                return;
            }

            DialogResult result = MessageBox.Show(
                this,
                "ȷʵҪ���б����Ƴ���ѡ���� " + this.listView_parent.SelectedItems.Count.ToString() + " ������?\r\n\r\n(ע����������������ݿ���ɾ����¼)",
                "SettlementForm",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Cancel)
                return;

            ListViewUtil.DeleteSelectedItems(this.listView_parent);

            SetNextButtonEnable();
        }

        // ѡ������״̬Ϊ�������
        void menu_parent_selectAllErrorLines_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this.listView_parent.Items)
            {
                if (item.ImageIndex == TYPE_ERROR)
                    item.Selected = true;
                else
                {
                    if (item.Selected == true)
                        item.Selected = false;
                }
            }
        }

        // ȫѡ
        void menu_parent_selectAll_Click(object sender, EventArgs e)
        {
            ListViewUtil.SelectAllLines(this.listView_parent);
        }

        private void listView_member_MouseUp(object sender, MouseEventArgs e)
        {

        }
    }

    // װ������ӡ �������ض�ȱʡֵ��PrintOption������
    internal class PrintBindingPrintOption : PrintOption
    {
        string PublicationType = "����������"; // ͼ�� ����������

        public override void LoadData(ApplicationInfo ai,
            string strPath)
        {
            string strNamePath = strPath;
            if (this.PublicationType != "ͼ��")
                strNamePath = "series_" + strNamePath;
            base.LoadData(ai, strNamePath);
        }

        public override void SaveData(ApplicationInfo ai,
            string strPath)
        {
            string strNamePath = strPath;
            if (this.PublicationType != "ͼ��")
                strNamePath = "series_" + strNamePath;
            base.SaveData(ai, strNamePath);
        }

        public PrintBindingPrintOption(string strDataDir,
            string strPublicationType)
        {
            Debug.Assert(this.PublicationType == "����������", "Ŀǰ��֧������������");


            this.DataDir = strDataDir;
            this.PublicationType = strPublicationType;

            this.PageHeaderDefault = "%date% װ���� - ����: %sourcedescription%";
            this.PageFooterDefault = "";

            this.TableTitleDefault = "�϶���";

            this.LinesPerPageDefault = 20;

            // Columnsȱʡֵ
            Columns.Clear();

            // "missing -- ȱ��״̬",
            Column column = new Column();
            column.Name = "missing -- ȱ��״̬";
            column.Caption = "ȱ��״̬";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "publishTime -- ��������",
            column = new Column();
            column.Name = "publishTime -- ��������";
            column.Caption = "��������";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "volume -- ���ں�"
            column = new Column();
            column.Name = "volume -- ���ں�";
            column.Caption = "���ں�";
            column.MaxChars = -1;
            this.Columns.Add(column);


            // "barcode -- �������"
            column = new Column();
            column.Name = "barcode -- �������";
            column.Caption = "�������";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "intact -- �����"
            column = new Column();
            column.Name = "intact -- �����";
            column.Caption = "�����";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "refID -- �ο�ID"
            column = new Column();
            column.Name = "refID -- �ο�ID";
            column.Caption = "�ο�ID";
            column.MaxChars = -1;
            this.Columns.Add(column);

        }
    }
}