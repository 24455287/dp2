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
using DigitalPlatform.CommonControl;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Script;
using DigitalPlatform.Marc;

using DigitalPlatform.IO;
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.dp2.Statis;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using ClosedXML.Excel;

namespace dp2Circulation
{
    /// <summary>
    /// ��ӡ�Ʋ��˴�
    /// </summary>
    public partial class AccountBookForm : BatchPrintFormBase
    {
        /// <summary>
        /// �����ù��ļ�¼·���ļ�ȫ·��
        /// </summary>
        public string RecPathFilePath = "";
        /// <summary>
        /// �����ù��Ĳ�������ļ�ȫ·��
        /// </summary>
        public string BarcodeFilePath = "";


        // ******************************
        // WordXML�������
        bool WordXmlTruncate = false;
        bool WordXmlOutputStatisPart = true;

        string m_strWordMlNsUri = "http://schemas.microsoft.com/office/word/2003/wordml";
        string m_strWxUri = "http://schemas.microsoft.com/office/word/2003/auxHint";

        // �����ù���WordXml����ļ���
        string ExportWordXmlFilename = "";

        // *******************************
        // �ı��������
        bool TextTruncate = false;
        bool TextOutputStatisPart = true;

        // װ������ʱ�ķ�ʽ
        string SourceStyle = "";    // "batchno" "barcodefile" "recpathfile"

        // �����ù�������ı��ļ���
        string ExportTextFilename = "";

        // refid -- ������¼path ���ձ�
        Hashtable refid_table = new Hashtable();
        // ������¼path -- ������¼XML���ձ�
        Hashtable orderxml_table = new Hashtable();

        string BatchNo = "";    // �����������κ�
        string LocationString = ""; // �������Ĺݲصص�

        /// <summary>
        /// ������� ImageIndex ���� : ����
        /// </summary>
        public const int TYPE_ERROR = 0;
        /// <summary>
        /// ������� ImageIndex ���� : ��ͨ
        /// </summary>
        public const int TYPE_NORMAL = 1;
        /// <summary>
        /// ������� ImageIndex ���� : �ѹ�ѡ
        /// </summary>
        public const int TYPE_CHECKED = 2;

        // ����������к�����
        SortColumns SortColumns_in = new SortColumns();

        #region �к�
        /// <summary>
        /// ��������: �������
        /// </summary>
        public static int COLUMN_BARCODE = 0;    // �������
        /// <summary>
        /// ��������: ժҪ
        /// </summary>
        public static int COLUMN_SUMMARY = 1;    // ժҪ
        /// <summary>
        /// ��������: ������Ϣ
        /// </summary>
        public static int COLUMN_ERRORINFO = 1;  // ������Ϣ
        /// <summary>
        /// ��������: ISBN/ISSN
        /// </summary>
        public static int COLUMN_ISBNISSN = 2;           // ISBN/ISSN
        /// <summary>
        /// ��������: ���¼״̬
        /// </summary>
        public static int COLUMN_STATE = 3;      // ״̬
        /// <summary>
        /// ��������: ��ȡ��
        /// </summary>
        public static int COLUMN_ACCESSNO = 4; // ��ȡ��
        /// <summary>
        /// ��������: ����ʱ��
        /// </summary>
        public static int MERGED_COLUMN_PUBLISHTIME = 5;          // ����ʱ��
        /// <summary>
        /// ��������: ����
        /// </summary>
        public static int MERGED_COLUMN_VOLUME = 6;          // ����
        /// <summary>
        /// ��������: �ݲصص�
        /// </summary>
        public static int COLUMN_LOCATION = 7;   // �ݲصص�
        /// <summary>
        /// ��������: ��۸�
        /// </summary>
        public static int COLUMN_PRICE = 8;      // �۸�
        /// <summary>
        /// ��������: ������
        /// </summary>
        public static int COLUMN_BOOKTYPE = 9;   // ������
        /// <summary>
        /// ��������: ��¼��
        /// </summary>
        public static int COLUMN_REGISTERNO = 10; // ��¼��
        /// <summary>
        /// ��������: ע��
        /// </summary>
        public static int COLUMN_COMMENT = 11;    // ע��
        /// <summary>
        /// ��������: �ϲ�ע��
        /// </summary>
        public static int COLUMN_MERGECOMMENT = 12;   // �ϲ�ע��
        /// <summary>
        /// ��������: ���κ�
        /// </summary>
        public static int COLUMN_BATCHNO = 13;    // ���κ�
        /// <summary>
        /// ��������: ���¼·��
        /// </summary>
        public static int COLUMN_RECPATH = 14;   // ���¼·��
        /// <summary>
        /// ��������: �ּ�¼·��
        /// </summary>
        public static int COLUMN_BIBLIORECPATH = 15; // �ּ�¼·��
        /// <summary>
        /// ��������: ��ο�ID
        /// </summary>
        public static int COLUMN_REFID = 16; // �ο�ID

        /// <summary>
        /// ��������: ��� (�Ӷ�����¼����)
        /// </summary>
        public static int EXTEND_COLUMN_CLASS = 17;             // ���
        /// <summary>
        /// ��������: ��Ŀ�� (�Ӷ�����¼����)
        /// </summary>
        public static int EXTEND_COLUMN_CATALOGNO = 18;          // ��Ŀ��
        /// <summary>
        /// ��������: ����ʱ�� (�Ӷ�����¼����)
        /// </summary>
        public static int EXTEND_COLUMN_ORDERTIME = 19;        // ����ʱ��
        /// <summary>
        /// ��������: ������ (�Ӷ�����¼����)
        /// </summary>
        public static int EXTEND_COLUMN_ORDERID = 20;          // ������
        /// <summary>
        /// ��������: ���� (�Ӷ�����¼����)
        /// </summary>
        public static int EXTEND_COLUMN_SELLER = 21;             // ����
        /// <summary>
        /// ��������: ������Դ (�Ӷ�����¼����)
        /// </summary>
        public static int EXTEND_COLUMN_SOURCE = 22;             // ������Դ

        /// <summary>
        /// ��������: ������
        /// </summary>
        public static int EXTEND_COLUMN_ORDERPRICE = 23;    // (������¼�е�)������

        /// <summary>
        /// ��������: �����
        /// </summary>
        public static int EXTEND_COLUMN_ACCEPTPRICE = 24;    // (������¼�е�)�����


        #endregion

        // const int WM_LOADSIZE = API.WM_USER + 201;

        /// <summary>
        /// ���캯��
        /// </summary>
        public AccountBookForm()
        {
            InitializeComponent();
        }

        private void AccountBookForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

            this.MainForm.AppInfo.LoadMdiSize += new EventHandler(AppInfo_LoadMdiSize);
            this.MainForm.AppInfo.SaveMdiSize += new EventHandler(AppInfo_SaveMdiSize);

            CreateColumnHeader(this.listView_in);

#if NO
            LoadSize();

            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            // 2009/2/2 new add
            this.comboBox_load_type.Text = this.MainForm.AppInfo.GetString(
                "accountbookform",
                "publication_type",
                "ͼ��");

            // 2012/11/26
            this.checkBox_load_fillOrderInfo.Checked = this.MainForm.AppInfo.GetBoolean(
    "accountbookform",
    "fillOrderInfo",
    true);

            this.checkBox_load_fillBiblioSummary.Checked = this.MainForm.AppInfo.GetBoolean(
    "accountbookform",
    "fillBiblioSummary",
    true);

            this.BarcodeFilePath = this.MainForm.AppInfo.GetString(
                "accountbookform",
                "barcode_filepath",
                "");

            this.BatchNo = this.MainForm.AppInfo.GetString(
                "accountbookform",
                "batchno",
                "");

            this.LocationString = this.MainForm.AppInfo.GetString(
                "accountbookform",
                "location_string",
                "");

            this.comboBox_sort_sortStyle.Text = this.MainForm.AppInfo.GetString(
                "accountbookform",
                "sort_style",
                "<��>");

            // API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);
        }

        void AppInfo_SaveMdiSize(object sender, EventArgs e)
        {
            if (sender != this)
                return;

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_in);
            this.MainForm.AppInfo.SetString(
                "accountbookform",
                "list_in_width",
                strWidths);
        }

        void AppInfo_LoadMdiSize(object sender, EventArgs e)
        {
            if (sender != this)
                return;

            string strWidths = this.MainForm.AppInfo.GetString(
               "accountbookform",
               "list_in_width",
               "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_in,
                    strWidths,
                    true);
            }
        }

        private void AccountBookForm_FormClosing(object sender, 
            FormClosingEventArgs e)
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
        }

        private void AccountBookForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                // 2009/2/2 new add
                this.MainForm.AppInfo.SetString(
                    "accountbookform",
                    "publication_type",
                    this.comboBox_load_type.Text);

                // 2012/11/26
                this.MainForm.AppInfo.SetBoolean(
        "accountbookform",
        "fillOrderInfo",
        this.checkBox_load_fillOrderInfo.Checked);

                this.MainForm.AppInfo.SetBoolean(
        "accountbookform",
        "fillBiblioSummary",
        this.checkBox_load_fillBiblioSummary.Checked);

                this.MainForm.AppInfo.SetString(
                    "accountbookform",
                    "barcode_filepath",
                    this.BarcodeFilePath);

                this.MainForm.AppInfo.SetString(
                    "accountbookform",
                    "batchno",
                    this.BatchNo);

                this.MainForm.AppInfo.SetString(
                    "accountbookform",
                    "location_string",
                    this.LocationString);

                this.MainForm.AppInfo.SetString(
                    "accountbookform",
                    "sort_style",
                    this.comboBox_sort_sortStyle.Text);

                CloseErrorInfoForm();

                this.MainForm.AppInfo.LoadMdiSize -= new EventHandler(AppInfo_LoadMdiSize);
                this.MainForm.AppInfo.SaveMdiSize -= new EventHandler(AppInfo_SaveMdiSize);
            }
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
                    /*
            switch (m.Msg)
            {
                case WM_LOADSIZE:
                    LoadSize();
                    return;
            }
                     * */
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
            this.button_load_loadFromRecPathFile.Enabled = bEnable;
            this.button_load_loadFromBarcodeFile.Enabled = bEnable;

            this.checkBox_load_fillBiblioSummary.Enabled = bEnable;
            this.checkBox_load_fillOrderInfo.Enabled = bEnable;

            this.comboBox_load_type.Enabled = bEnable;

            // next button
            if (bEnable == true)
                SetNextButtonEnable();
            else
                this.button_next.Enabled = false;

            // print page
            this.button_print_optionHTML.Enabled = bEnable;
            this.button_print_optionText.Enabled = bEnable;
            this.button_print_optionWordXml.Enabled = bEnable;

            this.button_print_outputTextFile.Enabled = bEnable;
            this.button_print_printNormalList.Enabled = bEnable;
            this.button_print_outputWordXmlFile.Enabled = bEnable;

            this.button_print_outputExcelFile.Enabled = bEnable;

            this.button_print_runScript.Enabled = bEnable;
            this.button_print_createNewScriptFile.Enabled = bEnable;

        }



        // ���·����������Ŀ���Ƿ�Ϊͼ��/�ڿ��⣿
        // return:
        //      -1  error
        //      0   ������Ҫ����ʾ��Ϣ��strError��
        //      1   ����Ҫ��
        internal override int CheckItemRecPath(string strPubType,
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

            if (strPubType == "ͼ��")
            {
                if (String.IsNullOrEmpty(strIssueDbName) == false)
                {
                    strError = "·�� '" + strItemRecPath + "' ����������Ŀ�� '" + strBiblioDbName + "' Ϊ�ڿ��ͣ��͵�ǰ���������� '" + strPubType + "' ��һ��";
                    return 0;
                }
                return 1;
            }

            if (strPubType == "����������")
            {
                if (String.IsNullOrEmpty(strIssueDbName) == true)
                {
                    strError = "·�� '" + strItemRecPath + "' ����������Ŀ�� '" + strBiblioDbName + "' Ϊͼ���ͣ��͵�ǰ���������� '" + strPubType + "' ��һ��";
                    return 0;
                }
                return 1;
            }

            strError = "CheckItemRecPath() δ֪�ĳ��������� '" + strPubType + "'";
            return -1;
        }



#if NO
        class RecordInfo
        {
            public DigitalPlatform.CirculationClient.localhost.Record Record = null;    // ���¼
            public XmlDocument Dom = null;  // ���¼XMLװ��DOM
            public string BiblioRecPath = "";
            public SummaryInfo SummaryInfo = null;  // ժҪ��Ϣ
        }

        // ׼��DOM����ĿժҪ��
        int GetSummaries(
            List<DigitalPlatform.CirculationClient.localhost.Record> records,
            out List<RecordInfo> infos,
            out string strError)
        {
            strError = "";
            infos = new List<RecordInfo>();

            // ׼��DOM����ĿժҪ
            for (int i = 0; i < records.Count; i++)
            {
                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�1";
                        return -1;
                    }
                }

                RecordInfo info = new RecordInfo();
                info.Record = records[i];
                infos.Add(info);

                if (info.Record.RecordBody == null)
                {
                    strError = "������dp2Kernel�����°汾";
                    return -1;
                }

                if (info.Record.RecordBody.Result.ErrorCode != ErrorCodeValue.NoError)
                    continue;

                info.Dom = new XmlDocument();
                try
                {
                    info.Dom.LoadXml(info.Record.RecordBody.Xml);
                }
                catch (Exception ex)
                {
                    strError = "���¼��XMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                // ׼����Ŀ��¼·��
                string strParentID = DomUtil.GetElementText(info.Dom.DocumentElement,
"parent");
                string strBiblioDbName = this.MainForm.GetBiblioDbNameFromItemDbName(Global.GetDbName(info.Record.Path));
                if (string.IsNullOrEmpty(strBiblioDbName) == true)
                {
                    strError = "���ݲ��¼·�� '" + info.Record.Path + "' �����Ŀ����ʱ����";
                    return -1;
                }
                info.BiblioRecPath = strBiblioDbName + "/" + strParentID;


            }

            // ׼��ժҪ
            if (this.checkBox_load_fillBiblioSummary.Checked == true)
            {
                // �鲢��Ŀ��¼·��
                List<string> bibliorecpaths = new List<string>();
                foreach (RecordInfo info in infos)
                {
                    bibliorecpaths.Add(info.BiblioRecPath);
                }

                // ȥ��
                StringUtil.RemoveDupNoSort(ref bibliorecpaths);

                // ����cache���Ƿ��Ѿ����ڣ�����Ѿ��������ٴӷ�����ȡ
                for (int i = 0; i < bibliorecpaths.Count; i++ )
                {
                    string strPath = bibliorecpaths[i];
                    SummaryInfo summary = (SummaryInfo)this.m_summaryTable[strPath];
                    if (summary != null)
                    {
                        bibliorecpaths.RemoveAt(i);
                        i--;
                    }
                }

                // �ӷ�������ȡ
                if (bibliorecpaths.Count > 0)
                {
                REDO_GETBIBLIOINFO_0:
                    string strCommand = "@path-list:" + StringUtil.MakePathList(bibliorecpaths);

                    string[] formats = new string[2];
                    formats[0] = "summary";
                    formats[1] = "@isbnissn";
                    string[] results = null;
                    byte[] timestamp = null;

                    // stop.SetMessage("����װ����Ŀ��¼ '" + bibliorecpaths[0] + "' �ȵ�ժҪ ...");

                    // TODO: ��û�п���ϣ��ȡ��������Ŀһ����ȡ��û��ȡ��?
                REDO_GETBIBLIOINFO:
                    long lRet = Channel.GetBiblioInfos(
                        stop,
                        strCommand,
                    "",
                        formats,
                        out results,
                        out timestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        DialogResult temp_result = MessageBox.Show(this,
        strError + "\r\n\r\n�Ƿ�����?",
        "AccountBookForm",
        MessageBoxButtons.RetryCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                        if (temp_result == DialogResult.Retry)
                            goto REDO_GETBIBLIOINFO;
                    }
                    if (lRet == -1 || lRet == 0)
                    {
                        if (lRet == 0 && String.IsNullOrEmpty(strError) == true)
                            strError = "��Ŀ��¼ '" + StringUtil.MakePathList(bibliorecpaths) + "' ������";

                        strError = "�����ĿժҪʱ��������: " + strError;
                        // ���results.Length������������ʵ�����Լ�������
                        if (results != null /* && results.Length == 2 * bibliorecpaths.Count */)
                        {
                        }
                        else
                            return -1;
                    }


                    if (results != null/* && results.Length == 2 * bibliorecpaths.Count*/)
                    {
                        // Debug.Assert(results != null && results.Length == 2 * bibliorecpaths.Count, "results������� " + (2 * bibliorecpaths.Count).ToString() + " ��Ԫ��");

                        // ���뻺��
                        for (int i = 0; i < results.Length / 2; i++)
                        {
                            SummaryInfo summary = new SummaryInfo();

                            summary.Summary = results[i*2];
                            summary.ISBnISSn = results[i*2+1];

                            this.m_summaryTable[bibliorecpaths[i]] = summary;
                        }
                    }

                    if (results != null && results.Length != 2 * bibliorecpaths.Count)
                    {
                        // û��ȡ������Ҫ��������
                        bibliorecpaths.RemoveRange(0, results.Length / 2);
                        goto REDO_GETBIBLIOINFO_0;
                    }
                }

                // �ҽӵ�ÿ����¼����
                foreach (RecordInfo info in infos)
                {
                    SummaryInfo summary = (SummaryInfo)this.m_summaryTable[info.BiblioRecPath];
                    if (summary == null)
                    {
                        strError = "�������Ҳ�����Ŀ��¼ '" + info.BiblioRecPath + "' ��ժҪ����";
                        return -1;
                    }

                    info.SummaryInfo = summary;
                }

                // ����cacheռ�ݵ��ڴ�̫��
                if (this.m_summaryTable.Count > 1000)
                    this.m_summaryTable.Clear();
            }

            return 0;
        }
#endif

        // ����һС����¼��װ��
        internal override int DoLoadRecords(List<string> lines,
            List<ListViewItem> items,
            bool bFillSummaryColumn,
            string [] summary_col_names,
            out string strError)
        {
            strError = "";

#if DEBUG
            if (items != null)
            {
                Debug.Assert(lines.Count == items.Count, "");
            }
#endif

            List<DigitalPlatform.CirculationClient.localhost.Record> records = new List<DigitalPlatform.CirculationClient.localhost.Record>();

            // ���л�ȡȫ�����¼��Ϣ
            for (; ; )
            {
                if (stop != null && stop.State != 0)
                {
                    strError = "�û��ж�1";
                    return -1;
                }

                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                string[] paths = new string[lines.Count];
                lines.CopyTo(paths);
            REDO_GETRECORDS:
                long lRet = this.Channel.GetBrowseRecords(
                    this.stop,
                    paths,
                    "id,xml",
                    out searchresults,
                    out strError);
                if (lRet == -1)
                {
                    DialogResult temp_result = MessageBox.Show(this,
    strError + "\r\n\r\n�Ƿ�����?",
    "AccountBookForm",
    MessageBoxButtons.RetryCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                    if (temp_result == DialogResult.Retry)
                        goto REDO_GETRECORDS;
                    return -1;
                }


                records.AddRange(searchresults);

                // ȥ���Ѿ�������һ����
                /*
                for (int i = 0; i < searchresults.Length; i++)
                {
                    lines.RemoveAt(0);
                }
                */
                lines.RemoveRange(0, searchresults.Length);

                if (lines.Count == 0)
                    break;
            }

            // ׼��DOM����ĿժҪ��
            List<RecordInfo> infos = null;
            int nRet = GetSummaries(
                bFillSummaryColumn,
                summary_col_names,
                records,
                out infos,
                out strError);
            if (nRet == -1)
                return -1;

            Debug.Assert(records.Count == infos.Count, "");

            List<OrderInfo> orderinfos = new List<OrderInfo>();

            this.listView_in.BeginUpdate();
            try
            {

                for (int i = 0; i < infos.Count; i++)
                {
                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�1";
                            return -1;
                        }
                    }

                    RecordInfo info = infos[i];

                    if (info.Record.RecordBody == null)
                    {
                        strError = "������ dp2Kernel �����°汾";
                        return -1;
                    }
                    // stop.SetMessage("����װ��·�� " + strLine + " ��Ӧ�ļ�¼...");


                    string strOutputItemRecPath = "";
                    ListViewItem item = null;

                    if (items != null)
                        item = items[i];

                    // ���ݲ�����ţ�װ����¼
                    // return: 
                    //      -2  ��������Ѿ���list�д�����
                    //      -1  ����
                    //      1   �ɹ�
                    nRet = LoadOneItem(
                        this.comboBox_load_type.Text,
                        bFillSummaryColumn,
                        summary_col_names,
                        "@path:" + info.Record.Path,
                        info,
                        this.listView_in,
                        null,
                        out strOutputItemRecPath,
                        ref item,
                        out strError);
                    /*
                    if (nRet == -2)
                        nDupCount++;
                     * */
                    /*
                    if (nRet == -1)
                        goto ERROR1;
                     * */


                    // ׼��װ�붩����Ϣ
                    if (nRet != -1 && this.checkBox_load_fillOrderInfo.Checked == true)
                    {
                        Debug.Assert(item != null, "");
                        string strRefID = ListViewUtil.GetItemText(item, COLUMN_REFID);
                        if (String.IsNullOrEmpty(strRefID) == false)
                        {
                            OrderInfo order_info = new OrderInfo();
                            order_info.ItemRefID = strRefID;
                            order_info.ListViewItem = item;
                            orderinfos.Add(order_info);
                        }
                    }

                }
            }
            finally
            {
                this.listView_in.EndUpdate();
            }

            // �ӷ�������ö�����¼��·��
            if (orderinfos.Count > 0)
            {
                nRet = LoadOrderInfo(
                    orderinfos,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return 0;
        }

        // ���ݲ��¼refid��ת��Ϊ������¼��recpath��Ȼ���ö�����¼XML
        int LoadOrderInfo(
            List<OrderInfo> orderinfos,
            out string strError)
        {
            strError = "";

            List<string> refids = new List<string>();
            foreach (OrderInfo info in orderinfos)
            {
                Debug.Assert(string.IsNullOrEmpty(info.ItemRefID) == false, "");
                refids.Add(info.ItemRefID);
            }

            // TODO: ���ֻ��һ����refid�����Լ�Ϊֱ�ӻ�ȡ����ԭ���ķ���

            // �Ѿ���һС��������һ���Ի�ȡ
            string strBiblio = "";
            string strResult = "";
            string strItemRecPath = "";
            byte[] item_timestamp = null;
            string strBiblioRecPath = "";
            long lRet = 0;
            string strRecordName = "";

            if (this.comboBox_load_type.Text == "ͼ��")
            {
                strRecordName = "������¼";
                lRet = this.Channel.GetOrderInfo(stop,
                     "@item-refid-list:" + StringUtil.MakePathList(refids),
                     "get-path-list",
                     out strResult,
                     out strItemRecPath,
                     out item_timestamp,
                     "", // strBiblioType,
                     out strBiblio,
                     out strBiblioRecPath,
                     out strError);
            }
            else
            {
                strRecordName = "�ڼ�¼";
                lRet = this.Channel.GetIssueInfo(stop,
                     "@item-refid-list:" + StringUtil.MakePathList(refids),
                     "get-path-list",
                     out strResult,
                     out strItemRecPath,
                     out item_timestamp,
                     "", // strBiblioType,
                     out strBiblio,
                     out strBiblioRecPath,
                     out strError);
            }

            if (lRet == -1)
                return -1;

            List<string> recpaths = new List<string>(strResult.Split(new char [] {','}));
            Debug.Assert(refids.Count == recpaths.Count, "");

            // List<string> notfound_refids = new List<string>();
            List<string> errors = new List<string>();
            {
                int i = 0;
                foreach (string recpath in recpaths)
                {
                    OrderInfo info = orderinfos[i];

                    if (string.IsNullOrEmpty(recpath) == true)
                    {
                        // notfound_refids.Add(recpaths[i]);
                        ListViewUtil.ChangeItemText(info.ListViewItem,
                            EXTEND_COLUMN_CATALOGNO,
                            "��ο�ID '" + info.ItemRefID + "' û���ҵ���Ӧ��" + strRecordName);
                    }
                    else if (recpath[0] == '!')
                        errors.Add(recpath.Substring(1));
                    else
                        info.OrderRecPath = recpath;

                    i++;
                }
            }

            if (errors.Count > 0)
                strError = "���"+strRecordName+"�Ĺ��̷�������: " + StringUtil.MakePathList(errors);

#if NO
            if (notfound_refids.Count > 0)
            {
                if (string.IsNullOrEmpty(strError) == false)
                    strError += ";\r\n";

                strError += "���� ���¼�ο�ID û���ҵ�: " + StringUtil.MakePathList(notfound_refids);
            }
#endif

            if (string.IsNullOrEmpty(strError) == false)
                return -1;

            // ������ö�����¼
            List<string> order_recpaths = new List<string>();
            foreach (OrderInfo info in orderinfos)
            {
                if (String.IsNullOrEmpty(info.OrderRecPath) == false)
                    order_recpaths.Add(info.OrderRecPath);
            }

            if (order_recpaths.Count > 0)
            {
                List<string> lines = new List<string>();
                lines.AddRange(order_recpaths);

                List<DigitalPlatform.CirculationClient.localhost.Record> records = new List<DigitalPlatform.CirculationClient.localhost.Record>();

                // ���л�ȡȫ�����¼��Ϣ
                for (; ; )
                {
                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�1";
                            return -1;
                        }
                    }

                    DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                    string[] paths = new string[lines.Count];
                    lines.CopyTo(paths);
                REDO_GETRECORDS:
                    lRet = this.Channel.GetBrowseRecords(
                        this.stop,
                        paths,
                        "id,xml",
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                    {
                        DialogResult temp_result = MessageBox.Show(this,
        strError + "\r\n\r\n�Ƿ�����?",
        "AccountBookForm",
        MessageBoxButtons.RetryCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                        if (temp_result == DialogResult.Retry)
                            goto REDO_GETRECORDS;
                        return -1;
                    }


                    records.AddRange(searchresults);

                    // ȥ���Ѿ�������һ����
                    /*
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        lines.RemoveAt(0);
                    }
                     * */
                    lines.RemoveRange(0, searchresults.Length);

                    if (lines.Count == 0)
                        break;
                }

                // �����е�XML��¼���ص�orderinfos�Ķ�Ӧλ��
                foreach (OrderInfo info in orderinfos)
                {
                    if (String.IsNullOrEmpty(info.OrderRecPath) == true)
                        continue;
                    int index = order_recpaths.IndexOf(info.OrderRecPath);
                    if (index == -1)
                    {
                        Debug.Assert(false, "");
                        strError = strRecordName + "·���� order_recpaths ��û���ҵ�";
                        return -1;
                    }

                    DigitalPlatform.CirculationClient.localhost.Record record = records[index];
                    if (record.RecordBody != null
                        && record.RecordBody.Result != null
                        && record.RecordBody.Result.ErrorCode != ErrorCodeValue.NoError)
                    {
                        ListViewUtil.ChangeItemText(info.ListViewItem,
    EXTEND_COLUMN_CATALOGNO,
    "��ȡ" + strRecordName + " '" + info.OrderRecPath + "' ʱ����: " + record.RecordBody.Result.ErrorString);
                        continue;
                    }

                    if (record.RecordBody != null)
                    {
                        info.OrderXml = record.RecordBody.Xml;

                        FillOrderColumns(info, this.comboBox_load_type.Text);
                    }
                }
            }

            return 0;
        }

        internal class OrderInfo
        {
            public ListViewItem ListViewItem = null;    // �б�����
            public string ItemRefID = "";       // ���¼REFID
            public string OrderRecPath = "";    // ������¼·��
            public string OrderXml = "";    // ������¼XML
        }

        /// <summary>
        /// ����б����ִ�����ݣ�׼��װ��������
        /// </summary>
        public override void ClearBefore()
        {
            base.ClearBefore();

            this.listView_in.Items.Clear();
            this.SortColumns_in.Clear();
            SortColumns.ClearColumnSortDisplay(this.listView_in.Columns);

            this.refid_table.Clear();
            this.orderxml_table.Clear();
        }

#if NO
        // �Ӽ�¼·���ļ�װ��
        /// <summary>
        /// �Ӽ�¼·���ļ�װ��
        /// </summary>
        /// <param name="strRecPathFilename">��¼·���ļ���(ȫ·��)</param>
        /// <param name="bClearBefore">�Ƿ�Ҫ��װ��ǰ�������б�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��������; 0: �ɹ�</returns>
        public int LoadFromRecPathFile(string strRecPathFilename,
            bool bClearBefore,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (bClearBefore == true)
                ClearBefore();

            string strTimeMessage = "";

            StreamReader sr = null;
            try
            {
                // ���ļ�
                sr = new StreamReader(strRecPathFilename);

                EnableControls(false);
                // MainForm.ShowProgress(true);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڳ�ʼ���������� ...");
                stop.BeginLoop();
                this.Update();
                this.MainForm.Update();

                try
                {
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

                        strLine = strLine.Trim();
                        if (String.IsNullOrEmpty(strLine) == true)
                            continue;

                        // ���·����������Ŀ���Ƿ�Ϊͼ��/�ڿ��⣿
                        // return:
                        //      -1  error
                        //      0   ������Ҫ����ʾ��Ϣ��strError��
                        //      1   ����Ҫ��
                        nRet = CheckItemRecPath(this.comboBox_load_type.Text,
                            strLine,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 0)
                        {
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                        }

                        nLineCount++;
                        // stop.SetMessage("����װ�������� " + strLine + " ��Ӧ�ļ�¼...");
                    }

                    // ���ý��ȷ�Χ
                    stop.SetProgressRange(0, nLineCount);
                    sr.Close();

                    ProgressEstimate estimate = new ProgressEstimate();
                    estimate.SetRange(0, nLineCount);
                    estimate.Start();

                    List<string> lines = new List<string>();
                    // ��ʽ��ʼ����
                    sr = new StreamReader(strRecPathFilename);
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

                        lines.Add(strLine);
                        if (lines.Count >= 100)
                        {
                            if (lines.Count > 0)
                                stop.SetMessage("(" + i.ToString() + " / " + nLineCount.ToString() + ") ����װ��·�� " + lines[0] + " �ȼ�¼��"
                                    + "ʣ��ʱ�� " + ProgressEstimate.Format(estimate.Estimate(i)) + " �Ѿ���ʱ�� " + ProgressEstimate.Format(estimate.delta_passed));

                            // ����һС����¼��װ��
                            nRet = DoLoadRecords(lines,
                                null,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                            lines.Clear();
                        }
                    }

                    // ���ʣ�µ�һ��
                    if (lines.Count > 0)
                    {
                        if (lines.Count > 0)
                            stop.SetMessage("(" + nLineCount.ToString() + " / " + nLineCount.ToString() + ") ����װ��·�� " + lines[0] + " �ȼ�¼...");

                        // ����һС����¼��װ��
                        nRet = DoLoadRecords(lines,
                            null,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        lines.Clear();
                    }

                    strTimeMessage = "��װ����¼ " + nLineCount.ToString() + " �����ķ�ʱ��: " + estimate.GetTotalTime().ToString();
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("װ����ɡ�");
                    stop.HideProgress();

                    EnableControls(true);
                    // MainForm.ShowProgress(false);
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

            this.MainForm.StatusBarMessage = strTimeMessage;

            return 0;
        ERROR1:
            return -1;
        }
#endif

        // ���ݼ�¼·���ļ�װ��
        // TODO: �Ƿ�Ҫ����¼·���ظ�?
        private void button_load_loadFromRecPathFile_Click(object sender, EventArgs e)
        {
            int nRet = 0;
            string strError = "";

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵ļ�¼·���ļ���";
            dlg.FileName = this.RecPathFilePath;
            // dlg.InitialDirectory = 
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.SourceStyle = "recpathfile";

            // int nDupCount = 0;
            nRet = LoadFromRecPathFile(dlg.FileName,
                this.comboBox_load_type.Text,
                this.checkBox_load_fillBiblioSummary.Checked,
                new string[] { "summary", "@isbnissn" },
                (System.Windows.Forms.Control.ModifierKeys == Keys.Control ? false : true),
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // �����ļ���
            this.RecPathFilePath = dlg.FileName;
            this.Text = "��ӡ�Ʋ��� " + Path.GetFileName(this.RecPathFilePath);

            /*
            if (nDupCount != 0)
            {
                MessageBox.Show(this, "װ��������� " + nDupCount.ToString() + "���ظ�����������ԡ�");
            }
             * */

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
            this.Text = "��ӡ�Ʋ���";
            MessageBox.Show(this, strError);
        }


#if NO
        // ���ݼ�¼·���ļ�װ��
        private void button_load_loadFromRecPathFile_Click(object sender, EventArgs e)
        {
            int nRet = 0;

            this.ClearErrorInfoForm();

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵ļ�¼·���ļ���";
            dlg.FileName = this.RecPathFilePath;
            // dlg.InitialDirectory = 
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
                // MainForm.ShowProgress(true);

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
                        this.listView_in.Items.Clear();
                        this.SortColumns_in.Clear();
                        SortColumns.ClearColumnSortDisplay(this.listView_in.Columns);

                        this.refid_table.Clear();
                        this.orderxml_table.Clear();
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
                        // stop.SetMessage("����װ�������� " + strLine + " ��Ӧ�ļ�¼...");
                    }

                    // ���ý��ȷ�Χ
                    stop.SetProgressRange(0, nLineCount);
                    // stop.SetProgressValue(0);

                    // ���д���
                    // �ļ���ͷ?
                    // sr.BaseStream.Seek(0, SeekOrigin.Begin);

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
                        nRet = CheckItemRecPath(this.comboBox_load_type.Text,
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
                            this.comboBox_load_type.Text,
                            "@path:" + strLine,
                            this.listView_in,
                            null,
                            out strOutputItemRecPath,
                            out strError);
                        if (nRet == -2)
                            nDupCount++;
                        /*
                        if (nRet == -1)
                            goto ERROR1;
                         * */

                    }
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("װ����ɡ�");
                    stop.HideProgress();

                    EnableControls(true);
                    // MainForm.ShowProgress(false);
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
            this.Text = "��ӡ�Ʋ��� " + Path.GetFileName(this.RecPathFilePath);

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
            this.Text = "��ӡ�Ʋ���";
            MessageBox.Show(this, strError);
        }
#endif

#if NO
        int ConvertItemBarcodeToRecPath(
            List<string> barcodes,
            out List<string> recpaths,
            out string strError)
        {
            strError = "";
            recpaths = null;

        REDO_GETITEMINFO:
            string strBiblio = "";
            string strResult = "";
            long lRet = this.Channel.GetItemInfo(stop,
                "@barcode-list:" + StringUtil.MakePathList(barcodes),
                "get-path-list",
                out strResult,
                "", // strBiblioType,
                out strBiblio,
                out strError);
            if (lRet == -1)
                return -1;
            recpaths = StringUtil.SplitList(strResult);
            Debug.Assert(barcodes.Count == recpaths.Count, "");

            List<string> notfound_barcodes = new List<string>();
            List<string> errors = new List<string>();
            {
                int i = 0;
                foreach (string recpath in recpaths)
                {
                    if (string.IsNullOrEmpty(recpath) == true)
                        notfound_barcodes.Add(barcodes[i]);
                    else if (recpath[0] == '!')
                        errors.Add(recpath.Substring(1));
                    i++;
                }
            }

            if (errors.Count > 0)
            {
                strError = "ת��������ŵĹ��̷�������: " + StringUtil.MakePathList(errors);

                DialogResult temp_result = MessageBox.Show(this,
strError + "\r\n\r\n�Ƿ�����?",
"AccountBookForm",
MessageBoxButtons.RetryCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (temp_result == DialogResult.Retry)
                    goto REDO_GETITEMINFO;
                return -1;
            }

            if (notfound_barcodes.Count > 0)
            {
                if (string.IsNullOrEmpty(strError) == false)
                    strError += ";\r\n";

                strError += "���в������û���ҵ�: " + StringUtil.MakePathList(notfound_barcodes);
                DialogResult temp_result = MessageBox.Show(this,
strError + "\r\n\r\n�Ƿ��������?",
"AccountBookForm",
MessageBoxButtons.OKCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (temp_result == DialogResult.Cancel)
                    return -1;
            }

            /*
            if (string.IsNullOrEmpty(strError) == false)
                return -1;
             * */
            // �ѿ��ַ����� �� ��ͷ�Ķ�ȥ��
            for (int i = 0; i < recpaths.Count; i++)
            {
                string recpath = recpaths[i];
                if (string.IsNullOrEmpty(recpath) == true)
                {
                    recpaths.RemoveAt(i);
                    i--;
                }
                else if (recpath[0] == '!')
                {
                    recpaths.RemoveAt(i);
                    i--;
                }
            }

            return 0;
        }
#endif

#if NO
        // ���ݲ�������ļ��õ���¼·���ļ�
        int ConvertBarcodeFile(string strBarcodeFilename,
            string strRecPathFilename,
            out int nDupCount,
            out string strError)
        {
            nDupCount = 0;
            strError = "";
            int nRet = 0;

            StreamReader sr = null;
            StreamWriter sw = null;

            try
            {
                // ���ļ�
                sr = new StreamReader(strBarcodeFilename);

                sw = new StreamWriter(strRecPathFilename);

                EnableControls(false);
                // MainForm.ShowProgress(true);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڽ��������ת��Ϊ��¼·�� ...");
                stop.BeginLoop();
                this.Update();
                this.MainForm.Update();

                try
                {
#if NO
                    if (Control.ModifierKeys == Keys.Control)
                    {
                    }
                    else
                    {
                        this.listView_in.Items.Clear();
                        this.SortColumns_in.Clear();
                        SortColumns.ClearColumnSortDisplay(this.listView_in.Columns);

                        this.refid_table.Clear();
                        this.orderxml_table.Clear();
                    }
#endif

                    Hashtable barcode_table = new Hashtable();
                    // this.m_nGreenItemCount = 0;

                    // ���ж����ļ�����
                    // �����ļ�����
                    int nLineCount = 0;
                    List<string> lines = new List<string>();
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

                        strLine = strLine.Trim();
                        if (String.IsNullOrEmpty(strLine) == true)
                            continue;

                        if (strLine[0] == '#')
                            continue;   // ע����

                        if (barcode_table[strLine] != null)
                        {
                            nDupCount++;
                            continue;
                        }

                        barcode_table[strLine] = true;
                        lines.Add(strLine);
                        nLineCount++;
                        // stop.SetMessage("����װ�������� " + strLine + " ��Ӧ�ļ�¼...");
                    }

                    barcode_table.Clear(); // �ڳ��ռ�

                    // ���ý��ȷ�Χ
                    stop.SetProgressRange(0, nLineCount);
                    // stop.SetProgressValue(0);

                    // ���д���
                    // �ļ���ͷ?
                    // sr.BaseStream.Seek(0, SeekOrigin.Begin);

                    sr.Close();
                    sr = null;


                    int i = 0;
                    List<string> temp_lines = new List<string>();
                    foreach (string strLine in lines)
                    {
                        if (stop != null && stop.State != 0)
                        {
                            strError = "�û��ж�1";
                            goto ERROR1;
                        }

                        stop.SetProgressValue(i++);

                        // stop.SetMessage("����װ�������� " + strLine + " ��Ӧ�ļ�¼...");

                        temp_lines.Add(strLine);
                        if (temp_lines.Count >= 100)
                        {
                            // ���������ת��Ϊ���¼·��
                            List<string> recpaths = null;
                            nRet = ConvertItemBarcodeToRecPath(
                                temp_lines,
                                out recpaths,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                            foreach (string recpath in recpaths)
                            {
                                sw.WriteLine(recpath);
                            }
                            temp_lines.Clear();
                        }
                    }

                    // ���һ��
                    if (temp_lines.Count > 0)
                    {
                        // ���������ת��Ϊ���¼·��
                        List<string> recpaths = null;
                        nRet = ConvertItemBarcodeToRecPath(
                            temp_lines,
                            out recpaths,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        foreach (string recpath in recpaths)
                        {
                            sw.WriteLine(recpath);
                        }
                        temp_lines.Clear();
                    }
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("װ����ɡ�");
                    stop.HideProgress();

                    EnableControls(true);
                    // MainForm.ShowProgress(false);
                }
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                goto ERROR1;
            }
            finally
            {
                if (sr != null)
                    sr.Close();
                if (sw != null)
                    sw.Close();

            }

            return 0;
        ERROR1:
            return -1;
        }
#endif

        // ����������ļ�װ��
        private void button_load_loadFromBarcodeFile_Click(object sender, EventArgs e)
        {
            int nRet = 0;
            string strError = "";

            bool bClearBefore = true;
            if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                bClearBefore = false;

            if (bClearBefore == true)
                ClearBefore();

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
            string strRecPathFilename = Path.GetTempFileName();
            try
            {
                nRet = ConvertBarcodeFile(dlg.FileName,
                    strRecPathFilename,
                    out nDupCount,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                nRet = LoadFromRecPathFile(strRecPathFilename,
                    this.comboBox_load_type.Text,
                    this.checkBox_load_fillBiblioSummary.Checked,
                    new string[] { "summary", "@isbnissn" },
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            finally
            {
                if (string.IsNullOrEmpty(strRecPathFilename) == false)
                {
                    File.Delete(strRecPathFilename);
                    strRecPathFilename = "";
                }
            }

            if (nDupCount != 0)
            {
                MessageBox.Show(this, "װ��������� " + nDupCount.ToString() + "���ظ�����������ԡ�");
            }

            // �����ļ���
            this.BarcodeFilePath = dlg.FileName;
            this.Text = "��ӡ�Ʋ��� " + Path.GetFileName(this.BarcodeFilePath);

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
            this.Text = "��ӡ�Ʋ���";
            MessageBox.Show(this, strError);
        }


#if NO
        // ����������ļ�װ��
        private void button_load_loadFromBarcodeFile_Click(object sender, EventArgs e)
        {
            int nRet = 0;

            this.ClearErrorInfoForm();
            this.m_summaryTable.Clear();

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
                // MainForm.ShowProgress(true);

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
                        this.listView_in.Items.Clear();
                        this.SortColumns_in.Clear();
                        SortColumns.ClearColumnSortDisplay(this.listView_in.Columns);

                        this.refid_table.Clear();
                        this.orderxml_table.Clear();
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
                        // stop.SetMessage("����װ�������� " + strLine + " ��Ӧ�ļ�¼...");
                    }

                    // ���ý��ȷ�Χ
                    stop.SetProgressRange(0, nLineCount);
                    // stop.SetProgressValue(0);

                    // ���д���
                    // �ļ���ͷ?
                    // sr.BaseStream.Seek(0, SeekOrigin.Begin);

                    sr.Close();

                    sr = new StreamReader(dlg.FileName);


                    for (int i=0; ; i++)
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
                            this.comboBox_load_type.Text,
                            strLine,
                            null,
                            this.listView_in,
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
                        nRet = CheckItemRecPath(this.comboBox_load_type.Text,
                            strOutputItemRecPath,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 0)
                        {
                            GetErrorInfoForm().WriteHtml("�������Ϊ " + strLine + " �Ĳ��¼ " + strError + "\r\n");
                        }

                        /*
                        if (nRet == -1)
                            goto ERROR1;
                         * */

                    }
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("װ����ɡ�");
                    stop.HideProgress();

                    EnableControls(true);
                    // MainForm.ShowProgress(false);
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
            this.Text = "��ӡ�Ʋ��� " + Path.GetFileName(this.BarcodeFilePath);

            if (nDupCount != 0)
            {
                MessageBox.Show(this, "װ��������� " +nDupCount.ToString() + "���ظ�����������ԡ�");
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
            this.Text = "��ӡ�Ʋ���";
            MessageBox.Show(this, strError);
        }
#endif

        // ����listview��Ŀ����
        void CreateColumnHeader(ListView list)
        {
            ColumnHeader columnHeader_barcode = new ColumnHeader();
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
            ColumnHeader columnHeader_refID = new ColumnHeader();
            ColumnHeader columnHeader_accessno = new ColumnHeader();

            ColumnHeader columnHeader_isbnIssn = new ColumnHeader();
            ColumnHeader columnHeader_publishTime = new ColumnHeader();
            ColumnHeader columnHeader_volume = new ColumnHeader();
            ColumnHeader columnHeader_class = new ColumnHeader();
            ColumnHeader columnHeader_catalogNo = new ColumnHeader();
            ColumnHeader columnHeader_orderTime = new ColumnHeader();
            ColumnHeader columnHeader_orderID = new ColumnHeader();
            ColumnHeader columnHeader_seller = new ColumnHeader();
            ColumnHeader columnHeader_source = new ColumnHeader();
            ColumnHeader columnHeader_orderPrice = new ColumnHeader();
            ColumnHeader columnHeader_acceptPrice = new ColumnHeader();


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

            columnHeader_class,
            columnHeader_catalogNo,
            columnHeader_orderTime,
            columnHeader_orderID,
            columnHeader_seller,
            columnHeader_source,
            columnHeader_orderPrice,
            columnHeader_acceptPrice
            });

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
            // columnHeader_orderPrice
            // 
            columnHeader_orderPrice.Text = "������";
            columnHeader_orderPrice.Width = 150;

            // 
            // columnHeader_acceptPrice
            // 
            columnHeader_acceptPrice.Text = "�����";
            columnHeader_acceptPrice.Width = 150;



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
        }

#if NO
        // ��Ŀ��¼·�� --> SummaryInfo
        Hashtable m_summaryTable = new Hashtable();
        class SummaryInfo
        {
            public string Summary = "";
            public string ISBnISSn = "";
        }
#endif

        internal override void SetError(ListView list,
            ref ListViewItem item,
            string strBarcodeOrRecPath,
            string strError)
        {
            if (item == null)
            {
                item = new ListViewItem(strBarcodeOrRecPath, 0);
                list.Items.Add(item);
            }
            else
            {
                Debug.Assert(item.ListView == list, "");
            }

            // item.SubItems.Add(strError);
            ListViewUtil.ChangeItemText(item, COLUMN_ERRORINFO, strError);

            SetItemColor(item, TYPE_ERROR);

            // ���¼�������������Ұ
            list.EnsureVisible(list.Items.IndexOf(item));
        }

#if NO
        // ���ݲ�����Ż��߼�¼·����װ����¼
        // parameters:
        //      strBarcodeOrRecPath ������Ż��߼�¼·�����������ǰ׺Ϊ"@path:"���ʾΪ·��
        //      strMatchLocation    ���ӵĹݲصص�ƥ�����������==null����ʾû�������������(ע�⣬""��null���岻ͬ��""��ʾȷʵҪƥ�����ֵ)
        // return: 
        //      -2  ������Ż��߼�¼·���Ѿ���list�д�����(��û�м���listview��)
        //      -1  ����(ע���ʾ��������Ѿ�����listview����)
        //      0   ��Ϊ�ݲصص㲻ƥ�䣬û�м���list��
        //      1   �ɹ�
        int LoadOneItem(
            string strPubType,
            string strBarcodeOrRecPath,
            RecordInfo info,
            ListView list,
            string strMatchLocation,
            out string strOutputItemRecPath,
            ref ListViewItem item,
            out string strError)
        {
            strError = "";
            strOutputItemRecPath = "";
            long lRet = 0;

            // �ж��Ƿ��� @path: ǰ׺�����ں����֧����
            bool bIsRecPath = StringUtil.HasHead(strBarcodeOrRecPath, "@path:");

            string strItemText = "";
            string strBiblioText = "";

            // string strItemRecPath = "";
            string strBiblioRecPath = "";
            XmlDocument item_dom = null;
            string strBiblioSummary = "";
            string strISBnISSN = "";

            if (info == null)
            {
                byte[] item_timestamp = null;

            REDO_GETITEMINFO:
                lRet = Channel.GetItemInfo(
                    stop,
                    strBarcodeOrRecPath,
                    "xml",
                    out strItemText,
                    out strOutputItemRecPath,
                    out item_timestamp,
                    "recpath",
                    out strBiblioText,
                    out strBiblioRecPath,
                    out strError);
                if (lRet == -1)
                {
                    DialogResult temp_result = MessageBox.Show(this,
    strError + "\r\n\r\n�Ƿ�����?",
    "AccountBookForm",
    MessageBoxButtons.RetryCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                    if (temp_result == DialogResult.Retry)
                        goto REDO_GETITEMINFO;
                }
                if (lRet == -1 || lRet == 0)
                {
#if NO
                    if (item == null)
                    {
                        item = new ListViewItem(strBarcodeOrRecPath, 0);
                        list.Items.Add(item);
                    }
                    else
                    {
                        Debug.Assert(item.ListView == list, "");
                    }

                    // item.SubItems.Add(strError);
                    ListViewUtil.ChangeItemText(item, COLUMN_ERRORINFO, strError);

                    SetItemColor(item, TYPE_ERROR);

                    // ���¼�������������Ұ
                    list.EnsureVisible(list.Items.IndexOf(item));
#endif
                    SetError(list,
                        ref item,
                        strBarcodeOrRecPath,
                        strError);
                    goto ERROR1;
                }

                SummaryInfo summary = (SummaryInfo)this.m_summaryTable[strBiblioRecPath];
                if (summary != null)
                {
                    strBiblioSummary = summary.Summary;
                    strISBnISSN = summary.ISBnISSn;
                }

                if (strBiblioSummary == ""
                    && this.checkBox_load_fillBiblioSummary.Checked == true)
                {
                    string[] formats = new string[2];
                    formats[0] = "summary";
                    formats[1] = "@isbnissn";
                    string[] results = null;
                    byte[] timestamp = null;

                    stop.SetMessage("����װ����Ŀ��¼ '" + strBiblioRecPath + "' ��ժҪ ...");

                    Debug.Assert(String.IsNullOrEmpty(strBiblioRecPath) == false, "strBiblioRecPathֵ����Ϊ��");
                REDO_GETBIBLIOINFO:
                    lRet = Channel.GetBiblioInfos(
                        stop,
                        strBiblioRecPath,
                    "",
                        formats,
                        out results,
                        out timestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        DialogResult temp_result = MessageBox.Show(this,
        strError + "\r\n\r\n�Ƿ�����?",
        "AccountBookForm",
        MessageBoxButtons.RetryCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                        if (temp_result == DialogResult.Retry)
                            goto REDO_GETBIBLIOINFO;
                    }
                    if (lRet == -1 || lRet == 0)
                    {
                        if (lRet == 0 && String.IsNullOrEmpty(strError) == true)
                            strError = "��Ŀ��¼ '" + strBiblioRecPath + "' ������";

                        strBiblioSummary = "�����ĿժҪʱ��������: " + strError;
                    }
                    else
                    {
                        Debug.Assert(results != null && results.Length == 2, "results�������2��Ԫ��");
                        strBiblioSummary = results[0];
                        strISBnISSN = results[1];

                        // ����cacheռ�ݵ��ڴ�̫��
                        if (this.m_summaryTable.Count > 1000)
                            this.m_summaryTable.Clear();

                        if (summary == null)
                        {
                            summary = new SummaryInfo();
                            summary.Summary = strBiblioSummary;
                            summary.ISBnISSn = strISBnISSN;
                            this.m_summaryTable[strBiblioRecPath] = summary;
                        }
                    }
                }

                // ����һ�����xml��¼��ȡ���й���Ϣ����listview��
                if (item_dom == null)
                {
                    item_dom = new XmlDocument();
                    try
                    {
                        item_dom.LoadXml(strItemText);
                    }
                    catch (Exception ex)
                    {
                        strError = "���¼��XMLװ��DOMʱ����: " + ex.Message;
                        goto ERROR1;
                    }
                }

            }
            else
            {
                // record ��Ϊ�յ���ʱ���Ե���ʱ����strBarcodeOrRecPath����Ҫ��

                strBarcodeOrRecPath = "@path:" + info.Record.Path;
                bIsRecPath = true;

                if (info.Record.RecordBody.Result.ErrorCode != ErrorCodeValue.NoError)
                {
#if NO
                    if (item == null)
                        item = new ListViewItem(strBarcodeOrRecPath, 0);


                    item.SubItems.Add(info.Record.RecordBody.Result.ErrorString);

                    SetItemColor(item, TYPE_ERROR);
                    list.Items.Add(item);

                    // ���¼�������������Ұ
                    list.EnsureVisible(list.Items.Count - 1);
#endif
                    SetError(list,
    ref item,
    strBarcodeOrRecPath,
    info.Record.RecordBody.Result.ErrorString);
                    goto ERROR1;
                }

                strItemText = info.Record.RecordBody.Xml;
                strOutputItemRecPath = info.Record.Path;

                //
                item_dom = info.Dom;
                strBiblioRecPath = info.BiblioRecPath;
                if (info.SummaryInfo != null)
                {
                    strBiblioSummary = info.SummaryInfo.Summary;
                    strISBnISSN = info.SummaryInfo.ISBnISSn;
                }
            }


            // ���ӵĹݲصص�ƥ��
            if (strMatchLocation != null)
            {
                // TODO: #reservation, �����δ���?
                string strLocation = DomUtil.GetElementText(item_dom.DocumentElement,
                    "location");

                // 2013/3/26
                if (strLocation == null)
                    strLocation = "";

                if (strMatchLocation != strLocation)
                    return 0;
            }

            if (item == null)
            {
                item = AddToListView(list,
                    item_dom,
                    strOutputItemRecPath,
                    strBiblioSummary,
                    strISBnISSN,
                    strBiblioRecPath);

                // ͼ��
                // item.ImageIndex = TYPE_NORMAL;
                SetItemColor(item, TYPE_NORMAL);

                // ���¼�������������Ұ
                list.EnsureVisible(list.Items.Count - 1);

#if NO
                // �����Ҫ�Ӷ������õ���Ŀ��Ϣ
                if (this.checkBox_load_fillOrderInfo.Checked == true)
                    FillOrderColumns(item, strPubType);
#endif
            }
            else
            {
                SetListViewItemText(item_dom,
    true,
    strOutputItemRecPath,
    strBiblioSummary,
    strISBnISSN,
    strBiblioRecPath,
    item);
                SetItemColor(item, TYPE_NORMAL);
            }

            return 1;
        ERROR1:
            return -1;
        }
#endif

        // ����¾�ֵ���²���
        static string GetNewPart(string strValue)
        {
            string strOldValue = "";
            string strNewValue = "";

            // ���� "old[new]" �ڵ�����ֵ
            OrderDesignControl.ParseOldNewValue(strValue,
                out strOldValue,
                out strNewValue);

            return strNewValue;
        }

        // ��ǰ�İ汾
        // �����Ҫ�Ӷ������õ���Ŀ��Ϣ
        void FillOrderColumns(ListViewItem item,
            string strPubType)
        {
            string strRefID = ListViewUtil.GetItemText(item, COLUMN_REFID);
            if (String.IsNullOrEmpty(strRefID) == true)
                return;

            bool bSeries = false;
            if (strPubType == "����������")
                bSeries = true;


            string strError = "";
            int nRet = 0;

            XmlDocument dom = new XmlDocument();
            string strOrderOrIssueRecPath = "";

            // ͼ��
            if (bSeries == false)
            {
                // ��������ӵ�һ��������¼(��·��)
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = GetLinkedOrderRecordRecPath(strRefID,
                out strOrderOrIssueRecPath,
                out strError);
                if (nRet != 1)
                {
                    goto ERROR1;
                }

                string strItemXml = "";
                // ���ݼ�¼·�����һ��������¼
                nRet = GetOrderRecord(strOrderOrIssueRecPath,
                    out strItemXml,
                    out strError);
                if (nRet != 1)
                {
                    goto ERROR1;
                }

                try
                {
                    dom.LoadXml(strItemXml);
                }
                catch (Exception ex)
                {
                    strError = "������¼XMLװ��DOMʱ����: " + ex.Message;
                    goto ERROR1;
                }
            }
            else
            {
                // �ڿ�

                string strPublishTime = ListViewUtil.GetItemText(item, MERGED_COLUMN_PUBLISHTIME);

                if (String.IsNullOrEmpty(strPublishTime) == true)
                {
                    strError = "��������Ϊ�գ��޷���λ�ڼ�¼";
                    goto ERROR1;
                }

                // ��������ӵ�һ���ڼ�¼(��·��)
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = GetLinkedIssueRecordRecPath(strRefID,
                    strPublishTime,
                    out strOrderOrIssueRecPath,
                    out strError);
                if (nRet != 1)
                {
                    goto ERROR1;
                }

                string strItemXml = "";
                // ���ݼ�¼·�����һ���ڼ�¼
                nRet = GetIssueRecord(strOrderOrIssueRecPath,
                    out strItemXml,
                    out strError);
                if (nRet != 1)
                {
                    goto ERROR1;
                }

                try
                {
                    dom.LoadXml(strItemXml);
                }
                catch (Exception ex)
                {
                    strError = "�ڼ�¼XMLװ��DOMʱ����: " + ex.Message;
                    goto ERROR1;
                }

                // Ҫͨ��refid��λ�������һ������xmlƬ��

                string strOrderXml = "";
                // ���ڼ�¼�л�ú�һ��refid�йصĶ�����¼Ƭ��
                // parameters:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = GetSubOrderRecord(dom,
                    strRefID,
                    out strOrderXml,
                    out strError);
                if (nRet == -1)
                {
                    strError = "���ڼ�¼�л�ð��� '" + strRefID + "' �Ķ�����¼Ƭ��ʱ����: " + strError;
                    goto ERROR1;
                }
                if (nRet == 0)
                {
                    strError = "���ڼ�¼��û���ҵ����� '" + strRefID + "' �Ķ�����¼Ƭ��";
                    goto ERROR1;
                }

                try
                {
                    dom.LoadXml(strOrderXml);
                }
                catch (Exception ex)
                {
                    strError = "(���ڼ�¼��)��õĶ���Ƭ��XMLװ��DOMʱ����: " + ex.Message;
                    goto ERROR1;
                }
            }

            string strCatalogNo = DomUtil.GetElementText(dom.DocumentElement,
                "catalogNo");
            string strOrderID = DomUtil.GetElementText(dom.DocumentElement,
                "orderID");
            string strClass = DomUtil.GetElementText(dom.DocumentElement,
                "class");
            string strOrderTime = DomUtil.GetElementText(dom.DocumentElement,
                "orderTime");
            string strPrice = DomUtil.GetElementText(dom.DocumentElement,
                "price");
            // 2009/7/24 new add
            string strSeller = DomUtil.GetElementText(dom.DocumentElement,
                "seller");
            string strSource = DomUtil.GetElementText(dom.DocumentElement,
                "source");
            strSource = GetNewPart(strSource);  // ֻ��Ҫ�µ�ֵ

#if NO
            // ���total price�Ƿ���ȷ
            {
                string strCurrentOldPrice = "";
                string strCurrentNewPrice = "";

                // ���� "old[new]" �ڵ�����ֵ
                OrderDesignControl.ParseOldNewValue(strPrice,
                    out strCurrentOldPrice,
                    out strCurrentNewPrice);

                strPrice = strCurrentOldPrice;  // ԭʼ������
            }
#endif
            string strOrderPrice = "";  // ������¼�еĶ�����
            string strAcceptPrice = "";    // ������¼�еĵ����

            {
                string strCurrentOldPrice = "";
                string strCurrentNewPrice = "";

                // ���� "old[new]" �ڵ�����ֵ
                OrderDesignControl.ParseOldNewValue(strPrice,
                    out strCurrentOldPrice,
                    out strCurrentNewPrice);

                strOrderPrice = strCurrentOldPrice;  // ������¼�еĶ�����
                strAcceptPrice = strCurrentNewPrice;    // ������¼�еĵ����
            }

            try
            {
                strOrderTime = DateTimeUtil.LocalTime(strOrderTime);
            }
            catch (Exception ex)
            {
                strOrderTime = "ʱ���ַ��� '" + strOrderTime + "' ��ʽ����: " + ex.Message;
            }

            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_CATALOGNO, strCatalogNo);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_ORDERID, strOrderID);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_CLASS, strClass);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_ORDERTIME, strOrderTime);

            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_ORDERPRICE, strOrderPrice);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_ACCEPTPRICE, strAcceptPrice);

            // ListViewUtil.ChangeItemText(item, MERGED_COLUMN_ORDERRECPATH, strOrderOrIssueRecPath);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_SELLER, strSeller);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_SOURCE, strSource);

            string strDistribute = DomUtil.GetElementText(dom.DocumentElement,
                "distribute");
            if (String.IsNullOrEmpty(strDistribute) == false)
            {
                // ����refid -- ��¼·�����չ�ϵ
                List<string> refids = PrintAcceptForm.GetLocationRefIDs(strDistribute);

                for (int i = 0; i < refids.Count; i++)
                {
                    string strCurrentRefID = refids[i];
                    this.refid_table[strCurrentRefID] = strOrderOrIssueRecPath;
                }
            }

            return;
        ERROR1:
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_CATALOGNO, strError);
        }


        // �����Ҫ�Ӷ������õ���Ŀ��Ϣ
        void FillOrderColumns(OrderInfo info,
            string strPubType)
        {
            string strError = "";
            int nRet = 0; 
            
            Debug.Assert(string.IsNullOrEmpty(info.OrderXml) == false, "");

            ListViewItem item = info.ListViewItem;

            bool bSeries = false;
            if (strPubType == "����������")
                bSeries = true;

            XmlDocument dom = new XmlDocument();
            string strOrderOrIssueRecPath = "";

            // ͼ��
            if (bSeries == false)
            {
                try
                {
                    dom.LoadXml(info.OrderXml);
                }
                catch (Exception ex)
                {
                    strError = "������¼XMLװ��DOMʱ����: " + ex.Message;
                    goto ERROR1;
                }
            }
            else
            {
                try
                {
                    dom.LoadXml(info.OrderXml);
                }
                catch (Exception ex)
                {
                    strError = "�ڼ�¼XMLװ��DOMʱ����: " + ex.Message;
                    goto ERROR1;
                }

                string strOrderXml = "";
                // ���ڼ�¼�л�ú�һ��refid�йصĶ�����¼Ƭ��
                // parameters:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = GetSubOrderRecord(dom,
                    info.ItemRefID,
                    out strOrderXml,
                    out strError);
                if (nRet == -1)
                {
                    strError = "���ڼ�¼ "+info.OrderRecPath+" �л�ð��� '" + info.ItemRefID + "' �Ķ�����¼Ƭ��ʱ����: " + strError;
                    goto ERROR1;
                }
                if (nRet == 0)
                {
                    strError = "���ڼ�¼ " + info.OrderRecPath + " ��û���ҵ����� '" + info.ItemRefID + "' �Ķ�����¼Ƭ��";
                    goto ERROR1;
                }

                try
                {
                    dom.LoadXml(strOrderXml);
                }
                catch (Exception ex)
                {
                    strError = "(���ڼ�¼ " + info.OrderRecPath + " ��)��õĶ���Ƭ��XMLװ��DOMʱ����: " + ex.Message;
                    goto ERROR1;
                }
            }

            string strCatalogNo = DomUtil.GetElementText(dom.DocumentElement,
                "catalogNo");
            string strOrderID = DomUtil.GetElementText(dom.DocumentElement,
                "orderID");
            string strClass = DomUtil.GetElementText(dom.DocumentElement,
                "class");
            string strOrderTime = DomUtil.GetElementText(dom.DocumentElement,
                "orderTime");
            string strPrice = DomUtil.GetElementText(dom.DocumentElement,
                "price");
            // 2009/7/24 new add
            string strSeller = DomUtil.GetElementText(dom.DocumentElement,
                "seller");
            string strSource = DomUtil.GetElementText(dom.DocumentElement,
                "source");
            strSource = GetNewPart(strSource);  // ֻ��Ҫ�µ�ֵ

#if NO
            // ���total price�Ƿ���ȷ
            {
                string strCurrentOldPrice = "";
                string strCurrentNewPrice = "";

                // ���� "old[new]" �ڵ�����ֵ
                OrderDesignControl.ParseOldNewValue(strPrice,
                    out strCurrentOldPrice,
                    out strCurrentNewPrice);

                strPrice = strCurrentOldPrice;  // ԭʼ������
            }
#endif
            string strOrderPrice = "";  // ������¼�еĶ�����
            string strAcceptPrice = "";    // ������¼�еĵ����

            {
                string strCurrentOldPrice = "";
                string strCurrentNewPrice = "";

                // ���� "old[new]" �ڵ�����ֵ
                OrderDesignControl.ParseOldNewValue(strPrice,
                    out strCurrentOldPrice,
                    out strCurrentNewPrice);

                strOrderPrice = strCurrentOldPrice;  // ������¼�еĶ�����
                strAcceptPrice = strCurrentNewPrice;    // ������¼�еĵ����
            }


            try
            {
                strOrderTime = DateTimeUtil.LocalTime(strOrderTime);
            }
            catch (Exception ex)
            {
                strOrderTime = "ʱ���ַ��� '" + strOrderTime + "' ��ʽ����: " + ex.Message;
            }

            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_CATALOGNO, strCatalogNo);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_ORDERID, strOrderID);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_CLASS, strClass);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_ORDERTIME, strOrderTime);

            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_ORDERPRICE, strOrderPrice);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_ACCEPTPRICE, strAcceptPrice);

            // ListViewUtil.ChangeItemText(item, MERGED_COLUMN_ORDERRECPATH, strOrderOrIssueRecPath);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_SELLER, strSeller);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_SOURCE, strSource);

            string strDistribute = DomUtil.GetElementText(dom.DocumentElement,
                "distribute");
            if (String.IsNullOrEmpty(strDistribute) == false)
            {
                // ����refid -- ��¼·�����չ�ϵ
                List<string> refids = PrintAcceptForm.GetLocationRefIDs(strDistribute);

                for (int i = 0; i < refids.Count; i++)
                {
                    string strCurrentRefID = refids[i];
                    this.refid_table[strCurrentRefID] = strOrderOrIssueRecPath;
                }
            }

            return;
        ERROR1:
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_CATALOGNO, strError);
        }


        // ���ڼ�¼�л�ú�һ��refid�йصĶ�����¼Ƭ��
        // parameters:
        //      -1  error
        //      0   not found
        //      1   found
        static int GetSubOrderRecord(XmlDocument dom,
            string strRefID,
            out string strOrderXml,
            out string strError)
        {
            strError = "";
            strOrderXml = "";

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("orderInfo/*/distribute");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strDistribute = node.InnerText.Trim();
                if (String.IsNullOrEmpty(strDistribute) == true)
                    continue;

                LocationCollection locations = new LocationCollection();
                int nRet = locations.Build(strDistribute,
                    out strError);
                if (nRet == -1)
                    return -1;

                for (int j = 0; j < locations.Count; j++)
                {
                    DigitalPlatform.Location location = locations[j];

                    if (location.RefID == strRefID)
                    {
                        strOrderXml = node.ParentNode.OuterXml;
                        return 1;
                    }
                }
            }

            return 0;
        }

        // 2009/2/2
        // ��������ӵ�һ���ڼ�¼(��·��)
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetLinkedIssueRecordRecPath(string strRefID,
            string strPublishTime,
            out string strIssueRecPath,
            out string strError)
        {
            strError = "";
            strIssueRecPath = "";

            // �ȴ�cache����
            strIssueRecPath = (string)this.refid_table[strRefID];
            if (String.IsNullOrEmpty(strIssueRecPath) == false)
                return 1;

            long lRet = Channel.SearchIssue(
                stop,
                "<all>",
                strRefID,
                -1,
                "��ο�ID",
                "exact",
                this.Lang,
                "refid",   // strResultSetName
                "",    // strSearchStyle
                "", // strOutputStyle
                out strError);
            if (lRet == -1)
                return -1;

            if (lRet == 0)
            {
                strError = "�ο�ID '" + strRefID + "' û�������κ��ڼ�¼";
                return 0;
            }

            if (lRet > 1)
            {
                strError = "�ο�ID '" + strRefID + "' ���ж���(" + lRet.ToString() + ")������¼";
                return -1;
            }

            long lHitCount = lRet;

            DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

            // װ�������ʽ

            lRet = Channel.GetSearchResult(
                stop,
                "refid",   // strResultSetName
                0,
                lHitCount,
                "id",   // "id,cols",
                this.Lang,
                out searchresults,
                out strError);
            if (lRet == -1)
            {
                strError = "GetSearchResult() error: " + strError;
                return -1;
            }

            if (lRet == 0)
            {
                strError = "GetSearchResult() error : δ����";
                return -1;
            }

            strIssueRecPath = searchresults[0].Path;

            // ����cache
            if (this.refid_table.Count > 1000)
                this.refid_table.Clear();   // ����hashtable�����ߴ�
            this.refid_table[strRefID] = strIssueRecPath;

            return (int)lHitCount;
        }

        // 2009/2/2
        // ���ݼ�¼·�����һ���ڿ���¼
        int GetIssueRecord(string strRecPath,
            out string strItemXml,
            out string strError)
        {
            strError = "";
            strItemXml = "";

            // �ȴ�cache����
            strItemXml = (string)this.orderxml_table[strRecPath];
            if (String.IsNullOrEmpty(strItemXml) == false)
                return 1;


            string strBiblioText = "";

            string strItemRecPath = "";
            string strBiblioRecPath = "";

            byte[] item_timestamp = null;

            string strIndex = "@path:" + strRecPath;

            long lRet = Channel.GetIssueInfo(
                stop,
                strIndex,   // strPublishTime
                // "", // strBiblioRecPath
                "xml",
                out strItemXml,
                out strItemRecPath,
                out item_timestamp,
                "recpath",
                out strBiblioText,
                out strBiblioRecPath,
                out strError);
            if (lRet == -1 || lRet == 0)
                return (int)lRet;

            // ����cache
            if (this.orderxml_table.Count > 500)
                this.orderxml_table.Clear();   // ����hashtable�����ߴ�
            this.orderxml_table[strRecPath] = strItemXml;

            return 1;
        }

        // ��������ӵ�һ��������¼(��·��)
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetLinkedOrderRecordRecPath(string strRefID,
            out string strOrderRecPath,
            out string strError)
        {
            strError = "";
            strOrderRecPath = "";

            // �ȴ�cache����
            strOrderRecPath = (string)this.refid_table[strRefID];
            if (String.IsNullOrEmpty(strOrderRecPath) == false)
                return 1;

            long lRet = Channel.SearchOrder(
                stop,
                "<all>",
                strRefID,
                -1,
                "��ο�ID",
                "exact",
                this.Lang,
                "refid",   // strResultSetName
                "",    // strSearchStyle
                "", // strOutputStyle
                out strError);
            if (lRet == -1)
                return -1;

            if (lRet == 0)
            {
                strError = "�ο�ID '" + strRefID + "' û�������κζ�����¼";
                return 0;
            }

            if (lRet > 1)
            {
                strError = "�ο�ID '" + strRefID + "' ���ж���(" + lRet.ToString() + ")������¼";
                return -1;
            }

            long lHitCount = lRet;

            DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

            // װ�������ʽ

            lRet = Channel.GetSearchResult(
                stop,
                "refid",   // strResultSetName
                0,
                lHitCount,
                "id",   // "id,cols",
                this.Lang,
                out searchresults,
                out strError);
            if (lRet == -1)
            {
                strError = "GetSearchResult() error: " + strError;
                return -1;
            }

            if (lRet == 0)
            {
                strError = "GetSearchResult() error : δ����";
                return -1;
            }

            strOrderRecPath = searchresults[0].Path;

            // ����cache
            if (this.refid_table.Count > 1000)
                this.refid_table.Clear();   // ����hashtable�����ߴ�
            this.refid_table[strRefID] = strOrderRecPath;

            return (int)lHitCount;
        }

        // ���ݼ�¼·�����һ��������¼
        int GetOrderRecord(string strRecPath,
            out string strItemXml,
            out string strError)
        {
            strError = "";
            strItemXml = "";

            // �ȴ�cache����
            strItemXml = (string)this.orderxml_table[strRecPath];
            if (String.IsNullOrEmpty(strItemXml) == false)
                return 1;


            string strBiblioText = "";

            string strItemRecPath = "";
            string strBiblioRecPath = "";

            byte[] item_timestamp = null;

            string strIndex = "@path:" + strRecPath;

            long lRet = Channel.GetOrderInfo(
                stop,
                strIndex,
                // "",
                "xml",
                out strItemXml,
                out strItemRecPath,
                out item_timestamp,
                "recpath",
                out strBiblioText,
                out strBiblioRecPath,
                out strError);
            if (lRet == -1 || lRet == 0)
                return (int)lRet;

            // ����cache
            if (this.orderxml_table.Count > 500)
                this.orderxml_table.Clear();   // ����hashtable�����ߴ�
            this.orderxml_table[strRecPath] = strItemXml;

            return 1;
        }

        // ��������ı�����ǰ����ɫ����ͼ��
        static void SetItemColor(ListViewItem item,
            int nType)
        {
            if (nType == TYPE_ERROR)
            {
                item.BackColor = System.Drawing.Color.Red;
                item.ForeColor = System.Drawing.Color.White;
                item.ImageIndex = TYPE_ERROR;
            }
            else if (nType == TYPE_CHECKED)
            {
                item.BackColor = System.Drawing.Color.Green;
                item.ForeColor = System.Drawing.Color.White;
                item.ImageIndex = TYPE_CHECKED;
            }
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

        // ���ݲ��¼ DOM ���� ListViewItem ����һ�����������
        // parameters:
        //      bSetBarcodeColumn   �Ƿ�Ҫ��������������(��һ��)
        internal override void SetListViewItemText(XmlDocument dom,
            byte[] baTimestamp,
            bool bSetBarcodeColumn,
            string strRecPath,
            string strBiblioRecPath,
            string[] summary_col_names,
            SummaryInfo summary,
            ListViewItem item)
        {
            string strBiblioSummary = "";
            string strISBnISSN = "";

            if (summary != null && summary.Values != null)
            {
                if (summary.Values.Length > 0)
                    strBiblioSummary = summary.Values[0];
                if (summary.Values.Length > 1)
                    strISBnISSN = summary.Values[1];
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
            string strBorrower = DomUtil.GetElementText(dom.DocumentElement,
                "borrower");
            string strBorrowDate = DomUtil.GetElementText(dom.DocumentElement,
                "borrowDate");
            // 2007/6/20 new add
            strBorrowDate = DateTimeUtil.LocalTime(strBorrowDate, "u");
            string strBorrowPeriod = DomUtil.GetElementText(dom.DocumentElement,
                "borrowPeriod");
             * */
            // 2011/6/13
            string strSeller = DomUtil.GetElementText(dom.DocumentElement,
                "seller");
            string strSource = DomUtil.GetElementText(dom.DocumentElement,
                "source");

            ListViewUtil.ChangeItemText(item, COLUMN_SUMMARY, strBiblioSummary);

            ListViewUtil.ChangeItemText(item, COLUMN_STATE, strState);
            ListViewUtil.ChangeItemText(item, COLUMN_ACCESSNO, strAccessNo);
            ListViewUtil.ChangeItemText(item, MERGED_COLUMN_PUBLISHTIME, strPublishTime);
            ListViewUtil.ChangeItemText(item, MERGED_COLUMN_VOLUME, strVolume);

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

            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_SELLER, strSeller);
            ListViewUtil.ChangeItemText(item, EXTEND_COLUMN_SOURCE, strSource);


            if (bSetBarcodeColumn == true)
            {
                string strBarcode = DomUtil.GetElementText(dom.DocumentElement,
                    "barcode");
                ListViewUtil.ChangeItemText(item, COLUMN_BARCODE, strBarcode);
            }

            SetItemColor(item, TYPE_NORMAL);
        }

        internal override ListViewItem AddToListView(ListView list,
            XmlDocument dom,
            byte[] baTimestamp,
            string strRecPath,
            string strBiblioRecPath,
            string[] summary_col_names,
            SummaryInfo summary)
        {
            string strBarcode = DomUtil.GetElementText(dom.DocumentElement,
                "barcode");

            ListViewItem item = new ListViewItem(strBarcode, 0);

            SetListViewItemText(dom,
                baTimestamp,
                false,
                strRecPath,
                strBiblioRecPath,
                summary_col_names,
                summary,
                item);
            list.Items.Add(item);
            // ͼ��
            // item.ImageIndex = TYPE_NORMAL;
            // SetItemColor(item, TYPE_NORMAL);

            return item;
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

        // ��һ��
        private void button_next_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_load)
            {
                this.tabControl_main.SelectedTab = this.tabPage_sort;
                this.button_next.Enabled = true;
                this.comboBox_sort_sortStyle.Focus();
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
                this.button_print_printNormalList.Focus();
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

            for (int i = 0; i < this.listView_in.Items.Count; i++)
            {
                ListViewItem item = this.listView_in.Items[i];

                if (item.ImageIndex == TYPE_ERROR)
                    nRedCount++;
                else 
                    nWhiteCount++;
            }

            if (nRedCount != 0)
            {
                strError = "�б����� " +nRedCount+ " ����������(��ɫ��)�����޸����ݺ�����װ�ء�";
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


        // ������ɫ�������Ŀ
        int GetGreenItemCount()
        {
            int nCount = 0;
            for (int i = 0; i < this.listView_in.Items.Count; i++)
            {
                if (this.listView_in.Items[i].ImageIndex == TYPE_CHECKED)
                    nCount++;
            }
            return nCount;
        }

        // ��������ƥ���ListViewItem����
        static ListViewItem FindItem(ListView list,
            string strBarcode)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];
                if (strBarcode == item.Text)
                    return item;
            }

            return null;    // not found
        }

        // ��ӡȫ�������嵥
        private void button_print_printNormalList_Click(object sender, EventArgs e)
        {
            // string strError = "";

            EnableControls(false);

            try
            {

                int nErrorCount = 0;
                int nUncheckedCount = 0;

                List<ListViewItem> items = new List<ListViewItem>();
                for (int i = 0; i < this.listView_in.Items.Count; i++)
                {
                    ListViewItem item = this.listView_in.Items[i];

                    items.Add(item);

                    if (item.ImageIndex == TYPE_ERROR)
                        nErrorCount++;
                    if (item.ImageIndex == TYPE_NORMAL)
                        nUncheckedCount++;
                }

                PrintList(this.comboBox_load_type.Text + " ȫ�������嵥", items);
                return;

            }
            finally
            {
                EnableControls(true);
            }
            /*
        ERROR1:
            MessageBox.Show(this, strError);
             * */
        }

        private static Stylesheet GenerateStyleSheet()
        {
            return new Stylesheet(
                new Fonts(
                    new DocumentFormat.OpenXml.Spreadsheet.Font(                                                               // Index 0 - The default font.
                        new FontSize() { Val = 11 },
                        new DocumentFormat.OpenXml.Spreadsheet.Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new DocumentFormat.OpenXml.Spreadsheet.Font(                                                               // Index 1 - The bold font.
                        new Bold(),
                        new FontSize() { Val = 11 },
                        new DocumentFormat.OpenXml.Spreadsheet.Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new DocumentFormat.OpenXml.Spreadsheet.Font(                                                               // Index 2 - The Italic font.
                        new Italic(),
                        new FontSize() { Val = 11 },
                        new DocumentFormat.OpenXml.Spreadsheet.Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new DocumentFormat.OpenXml.Spreadsheet.Font(                                                               // Index 2 - The Times Roman font. with 16 size
                        new FontSize() { Val = 16 },
                        new DocumentFormat.OpenXml.Spreadsheet.Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Times New Roman" })
                ),
                new Fills(
                    new Fill(                                                           // Index 0 - The default fill.
                        new PatternFill() { PatternType = PatternValues.None }),
                    new Fill(                                                           // Index 1 - The default fill of gray 125 (required)
                        new PatternFill() { PatternType = PatternValues.Gray125 }),
                    new Fill(                                                           // Index 2 - The yellow fill.
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "FFFFFF00" } }
                        ) { PatternType = PatternValues.Solid })
                ),
                new Borders(
                    new Border(                                                         // Index 0 - The default border.
                        new LeftBorder(),
                        new RightBorder(),
                        new TopBorder(),
                        new BottomBorder(),
                        new DiagonalBorder()),
                    new Border(                                                         // Index 1 - Applies a Left, Right, Top, Bottom border to a cell
                        new LeftBorder(
                            new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new RightBorder(
                            new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new TopBorder(
                            new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder())
                ),
                new CellFormats(
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 0 },                          // Index 0 - The default cell style.  If a cell does not have a style index applied it will use this style combination instead
                    new CellFormat() { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 1 - Bold 
                    new CellFormat() { FontId = 2, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 2 - Italic
                    new CellFormat() { FontId = 3, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 3 - Times Roman
                    new CellFormat() { FontId = 0, FillId = 2, BorderId = 0, ApplyFill = true },       // Index 4 - Yellow Fill
                    // 5 textwrap
                    new CellFormat(                                                                   // Index 5 - Alignment
                        new Alignment() { Vertical = VerticalAlignmentValues.Center, WrapText = BooleanValue.FromBoolean(true) }
                    ) { /*FontId = 1, FillId = 0, BorderId = 0, */ApplyAlignment = true },

                    // 6 align center
                    new CellFormat(                                                                  
                        new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                    ) { ApplyAlignment = true },

                    
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 1, ApplyBorder = true }      // Index 6 - Border
                )
            ); // return
        }

        string ExportExcelFilename = "";

        void PrintList(
            string strTitle,
            List<ListViewItem> items)
        {
            string strError = "";

            // ����һ��html�ļ�������ʾ��HtmlPrintForm�С�

            List<string> filenames = null;
            try
            {
                // Debug.Assert(false, "");

                // ����htmlҳ��
                int nRet = BuildHtml(
                    items,
                    out filenames,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                HtmlPrintForm printform = new HtmlPrintForm();

                printform.Text = "��ӡ" + strTitle;
                printform.MainForm = this.MainForm;
                printform.Filenames = filenames;

                this.MainForm.AppInfo.LinkFormState(printform, "printform_state");
                printform.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(printform);
            }
            finally
            {
                if (filenames != null)
                    Global.DeleteFiles(filenames);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void WriteWordXmlHead(XmlTextWriter writer)
        {
            writer.WriteStartDocument();

            // <?mso-application progid="Word.Document"?>
            writer.WriteProcessingInstruction("mso-application",
                "progid=\"Word.Document\"");

            // <w:wordDocument>
            writer.WriteStartElement("w", "wordDocument", m_strWordMlNsUri);

            writer.WriteAttributeString(
                "xmlns",
                "v",
                null,
                "urn:schemas-microsoft-com:vml");

            writer.WriteAttributeString(
    "xmlns",
    "w10",
    null,
    "urn:schemas-microsoft-com:office:word");

            writer.WriteAttributeString(
"xmlns",
"sl",
null,
"http://schemas.microsoft.com/schemaLibrary/2003/core");

            writer.WriteAttributeString(
"xmlns",
"aml",
null,
"http://schemas.microsoft.com/aml/2001/core");


            writer.WriteAttributeString(
                "xmlns",
                "wx",
                null,
                m_strWxUri);

            writer.WriteAttributeString(
"xmlns",
"o",
null,
"urn:schemas-microsoft-com:office:office");

            // <w:body>
            writer.WriteStartElement("w", "body", m_strWordMlNsUri);

            // <wx:sect>
            writer.WriteStartElement("wx", "sect", m_strWxUri);
        }

        void WriteWordXmlTail(XmlTextWriter writer)
        {
            // <wx:sect>
            writer.WriteEndElement();

            // <w:body>
            writer.WriteEndElement();

            // <w:wordDocument>
            writer.WriteEndElement();

            writer.WriteEndDocument();
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

        // �����Word Xml�ļ�
        int OutputToWordXmlFile(
            List<ListViewItem> items,
            XmlTextWriter writer,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            Hashtable macro_table = new Hashtable();

            string strNamePath = "accountbook_printoption_wordxml";

            // ��ô�ӡ����
            PrintOption option = new AccountBookPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);

            // ��鵱ǰ����״̬�Ͱ����ּ۸���֮���Ƿ����ì��
            if (bHasBiblioPriceColumn(option) == true)
            {

                if (this.SortColumns_in.Count != 0
                    && this.SortColumns_in[0].No == COLUMN_BIBLIORECPATH)
                {
                }
                else
                {
                    MessageBox.Show(this, "���ڵ�ǰ��ӡ�õ��� ���ּ۸��У�Ϊ��֤��ӡ�����׼ȷ�������Զ��� ���ּ�¼·���� �ж�ȫ���б��������һ���Զ�����\r\n\r\nΪ����������Զ����򣬿��ڴ�ӡǰ������������������з����Լ���Ը������ֻҪ���һ�ε���ǡ��ּ�¼·���������⼴�ɡ�");
                    ForceSortColumnsIn(COLUMN_BIBLIORECPATH);
                }
            }

            // 2009/7/24 changed
            if (this.SourceStyle == "batchno")
            {
                macro_table["%batchno%"] = HttpUtility.HtmlEncode(this.BatchNo); // ���κ�
                macro_table["%location%"] = HttpUtility.HtmlEncode(this.LocationString); // �ݲصص� ��HtmlEncode()��ԭ����Ҫ��ֹ������ֵġ�<��ָ��>������
            }
            else
            {
                Debug.Assert(this.SourceStyle == "barcodefile"
                    || this.SourceStyle == "recpathfile"
                    || this.SourceStyle == "",
                    "");

                macro_table["%batchno%"] = "";
                macro_table["%location%"] = "";
            }

            // macro_table["%pagecount%"] = nPageCount.ToString();
            // macro_table["%linesperpage%"] = option.LinesPerPage.ToString();
            macro_table["%date%"] = DateTime.Now.ToLongDateString();

            // 2009/7/24 changed
            if (this.SourceStyle == "barcodefile")
            {
                macro_table["%barcodefilepath%"] = this.BarcodeFilePath;
                macro_table["%barcodefilename%"] = Path.GetFileName(this.BarcodeFilePath);
            }
            else
            {
                Debug.Assert(this.SourceStyle == "batchno"
                    || this.SourceStyle == "recpathfile"
                    || this.SourceStyle == "",
                    "");

                macro_table["%barcodefilepath%"] = "";
                macro_table["%barcodefilename%"] = "";
            }

            // 2009/7/30 new add
            if (this.SourceStyle == "recpathfile")
            {
                macro_table["%recpathfilepath%"] = this.RecPathFilePath;
                macro_table["%recpathfilename%"] = Path.GetFileName(this.RecPathFilePath);
            }
            else
            {
                Debug.Assert(this.SourceStyle == "batchno"
                    || this.SourceStyle == "barcodefile"
                    || this.SourceStyle == "",
                    "");

                macro_table["%recpathfilepath%"] = "";
                macro_table["%recpathfilename%"] = "";
            }

            macro_table["%sourcedescription%"] = this.SourceDescription;

            WriteWordXmlHead(writer);

            // ���ͳ����Ϣҳ
            if (this.WordXmlOutputStatisPart == true)
            {
                int nItemCount = items.Count;
                int nBiblioCount = GetBiblioCount(items);
                string strTotalPrice = GetTotalPrice(items);

                macro_table["%itemcount%"] = nItemCount.ToString();
                macro_table["%bibliocount%"] = nBiblioCount.ToString();
                macro_table["%totalprice%"] = strTotalPrice;

                macro_table["%pageno%"] = "1";

                // 2008/11/23 new add
                macro_table["%datadir%"] = this.MainForm.DataDir;   // ��������datadir��templatesĿ¼�ڵ�ĳЩ�ļ�
                ////macro_table["%libraryserverdir%"] = this.MainForm.LibraryServerDir;  // �������÷������˵�CSS�ļ�

                string strTemplateFilePath = option.GetTemplatePageFilePath("ͳ��ҳ");
                if (String.IsNullOrEmpty(strTemplateFilePath) == false)
                {
                    /*
%date% �Ʋ��ʲ� -- %sourcedescription%
����: %itemcount%
����: %bibliocount%
�ܼ�: %totalprice%
------------
���κ�: %batchno%
�ݲصص�: %location%
������ļ�: %barcodefilepath%
��¼·���ļ�: %recpathfilepath%
                     * * */

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
                    string[] lines = strResult.Split(new string[] {"\r\n"},
                        StringSplitOptions.None);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        WriteParagraph(writer, lines[i]);
                    }
                }
                else
                {
                    // ȱʡ�Ĺ̶����ݴ�ӡ

                    /*
                    BuildPageTop(option,
                        macro_table,
                        strFileName,
                        false);
                     * */

                    // ������

                    WriteParagraph(writer, "����\t" + nItemCount.ToString());
                    WriteParagraph(writer, "����\t" + nBiblioCount.ToString());
                    WriteParagraph(writer, "�ܼ�\t" + strTotalPrice);

                    WriteParagraph(writer, "----------");


                    if (this.SourceStyle == "batchno")
                    {
                        // 2008/11/22 new add
                        if (String.IsNullOrEmpty(this.BatchNo) == false)
                        {
                            WriteParagraph(writer, "���κ�\t" + this.BatchNo);
                        }
                        if (String.IsNullOrEmpty(this.LocationString) == false
                            && this.LocationString != "<��ָ��>")
                        {
                            WriteParagraph(writer, "�ݲصص�\t" + this.LocationString);
                        }
                    }

                    if (this.SourceStyle == "barcodefile")
                    {
                        if (String.IsNullOrEmpty(this.BarcodeFilePath) == false)
                        {
                            WriteParagraph(writer, "������ļ�\t" + this.BarcodeFilePath);
                        }
                    }

                    // 2009/7/30 new add
                    if (this.SourceStyle == "recpathfile")
                    {
                        if (String.IsNullOrEmpty(this.RecPathFilePath) == false)
                        {
                            WriteParagraph(writer, "��¼·���ļ�\t" + this.RecPathFilePath);
                        }
                    }

                    WriteParagraph(writer, "----------");
                    WriteParagraph(writer, "");
                }
            }

            string strMarcFilterFilePath = option.GetTemplatePageFilePath("MARC������");
            if (String.IsNullOrEmpty(strMarcFilterFilePath) == false)
            {
#if NO
                ColumnFilterDocument filter = null;

                this.ColumnTable = new Hashtable();
                nRet = PrepareMarcFilter(strMarcFilterFilePath,
                    out filter,
                    out strError);
                if (nRet == -1)
                    return -1;

                //
                if (filter != null)
                    this.AssemblyFilter = filter.Assembly;
                else
                    this.AssemblyFilter = null;

                this.MarcFilter = filter;
#endif
                nRet = PrepareMarcFilter(strMarcFilterFilePath, out strError);
                if (nRet == -1)
                    return -1;
            }

            // ���������ͱ�����
            WriteTableBegin(writer,
                option,
                macro_table);

            // �����ѭ��
            for (int i = 0; i < items.Count; i++)
            {
                BuildWordXmlTableLine(option,
                    items,
                    i,
                    writer,
                    this.WordXmlTruncate);
            }

            WriteTableEnd(writer);

            WriteWordXmlTail(writer);

            return 0;
        }

        int BuildWordXmlTableLine(PrintOption option,
            List<ListViewItem> items,
            int nIndex,
            XmlTextWriter writer,
            bool bCutText)
        {
            string strError = "";
            int nRet = 0;

            if (nIndex >= items.Count)
            {
                strError = "error: nIndex(" + nIndex.ToString() + ") >= items.Count(" + items.Count.ToString() + ")";
                goto ERROR1;
            }

            ListViewItem item = items[nIndex];
            string strMARC = "";
            string strOutMarcSyntax = "";

            if (this.MarcFilter != null
                || option.HasEvalue() == true)
            {

                // TODO: �д���Ҫ���Ա����������������ڴ�ӡ������ŷ��֣�������

                // ���MARC��ʽ��Ŀ��¼
                string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);
                nRet = GetMarc(strBiblioRecPath,
                    out strMARC,
                    out strOutMarcSyntax,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (this.MarcFilter != null)
                {
                    this.ColumnTable.Clear();   // �����һ��¼����ʱ���������
                    this.MarcFilter.Host.UiItem = item; // ��ǰ���ڴ���� ListViewItem

                    // ����filter�е�Record��ض���
                    nRet = this.MarcFilter.DoRecord(
                        null,
                        strMARC,
                        strOutMarcSyntax,
                        nIndex,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }
            }

            // <w:tr>
            writer.WriteStartElement("w", "tr", m_strWordMlNsUri);

            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];

                // int nIndex = nPage * option.LinesPerPage + nLine;

                /*
                if (nIndex >= items.Count)
                    break;

                ListViewItem item = items[nIndex];
                 * */

                string strContent = "";
                if (string.IsNullOrEmpty(column.Evalue) == false)
                {
                    Jurassic.ScriptEngine engine = new Jurassic.ScriptEngine();
                    engine.EnableExposedClrTypes = true;
                    engine.SetGlobalValue("syntax", strOutMarcSyntax);
                    engine.SetGlobalValue("biblio", new MarcRecord(strMARC));
                    strContent = engine.Evaluate(column.Evalue).ToString();
                }
                else
                {
                    strContent = GetColumnContent(item,
                        column.Name);

                    if (strContent == "!!!#")
                    {
                        // strContent = ((nPage * option.LinesPerPage) + nLine + 1).ToString();
                        strContent = (nIndex + 1).ToString();
                    }

                    if (strContent == "!!!biblioPrice")
                    {
                        // �����Լ��ǲ��Ǵ����л�����
                        string strCurLineBiblioRecPath = GetColumnContent(item, "biblioRecpath");

                        string strNextLineBiblioRecPath = "";

                        if (nIndex < items.Count - 1)
                        {
                            ListViewItem next_item = items[nIndex + 1];
                            strNextLineBiblioRecPath = GetColumnContent(next_item, "biblioRecpath");
                        }

                        if (strCurLineBiblioRecPath != strNextLineBiblioRecPath)
                        {
                            // �����л�����

                            // ����ǰ��Ĳ�۸�
                            strContent = ComputeBiblioPrice(items, nIndex).ToString();
                            // bBiblioSumLine = true;
                        }
                        else
                        {
                            // ������ͨ��
                            strContent = "";    //  "&nbsp;";
                        }
                    }
                }

                if (bCutText == true)
                {
                    // �ض��ַ���
                    if (column.MaxChars != -1)
                    {
                        if (strContent.Length > column.MaxChars)
                        {
                            strContent = strContent.Substring(0, column.MaxChars);
                            strContent += "...";
                        }
                    }
                }

                if (String.IsNullOrEmpty(strContent) == true)
                    strContent = "";    //  "&nbsp;";

                // string strClass = Global.GetLeft(column.Name);

                // <w:tc>
                writer.WriteStartElement("w", "tc", m_strWordMlNsUri);

                WriteParagraph(writer, strContent);

                // <w:tc>
                writer.WriteEndElement();
            }

            /*
            if (bBiblioSumLine == false)
            {
                StreamUtil.WriteText(strFileName,
        "<tr class='content'>");
            }
            else
            {
                StreamUtil.WriteText(strFileName,
        "<tr class='content_biblio_sum'>");
            }*/

            // <w:tr>
            writer.WriteEndElement();
            // sw.WriteLine(strLineContent);
            return 0;
        ERROR1:
            // <w:tr>
            writer.WriteStartElement("w", "tr", m_strWordMlNsUri);

            // <w:tc>
            writer.WriteStartElement("w", "tc", m_strWordMlNsUri);

            WriteParagraph(writer, strError);

            // <w:tc>
            writer.WriteEndElement();

            // <w:tr>
            writer.WriteEndElement();

            return -1;
        }

        void WriteBorderDef(XmlTextWriter writer,
            string strElementName,
            int nSz,
            int nBorderWidth)
        {
            // <w:top w:val="single" w:sz="10" wx:bdrwidth="10" w:space="0" w:color="auto" /> 
            writer.WriteStartElement("w", strElementName, m_strWordMlNsUri);

            // w:val="single" ������
            writer.WriteAttributeString("w", "val", m_strWordMlNsUri,
                "single");

            // w:sz="10"
            writer.WriteAttributeString("w", "sz", m_strWordMlNsUri,
                nSz.ToString());

            // wx:bdrwidth="10"
            writer.WriteAttributeString("wx", "bdrwidth", m_strWxUri,
                nBorderWidth.ToString());

            // w:color="auto"
            writer.WriteAttributeString("w", "color", m_strWordMlNsUri,
                "auto");

            // <w:top>
            writer.WriteEndElement();
        }

        void WriteMarginDef(XmlTextWriter writer,
            string strElementName,
            int nWidth)
        {
            // <w:left w:w="10" w:type="dxa" />
            writer.WriteStartElement("w", strElementName, m_strWordMlNsUri);

            // w:w="10"
            writer.WriteAttributeString("w", "w", m_strWordMlNsUri,
                nWidth.ToString());

            // w:type="dxa"
            writer.WriteAttributeString("w", "type", m_strWordMlNsUri,
                "dxa");

            writer.WriteEndElement();
        }

        void WriteTableProperty(XmlTextWriter writer)
        {
            // <w:tblPr>
            writer.WriteStartElement("w", "tblPr", m_strWordMlNsUri);

            // ���Ԫ����
            // <w:tblCellMar>
            writer.WriteStartElement("w", "tblCellMar", m_strWordMlNsUri);

            WriteMarginDef(writer,
                "left",
                100);
            WriteMarginDef(writer,
                "right",
                100);

            // </w:tblCellMar>
            writer.WriteEndElement();


            // ���߿���
            // <w:tblBorders>
            writer.WriteStartElement("w", "tblBorders", m_strWordMlNsUri);

            // ��
            WriteBorderDef(writer, "top", 10, 10);
            WriteBorderDef(writer, "left", 10, 10);
            WriteBorderDef(writer, "bottom", 10, 10);
            WriteBorderDef(writer, "right", 10, 10);

            WriteBorderDef(writer, "insideH", 1, 1);
            WriteBorderDef(writer, "insideV", 1, 1);

            // </w:tblBorders>
            writer.WriteEndElement();

            // </w:tblPr>
            writer.WriteEndElement();
        }

        void WriteParagraph(XmlTextWriter writer,
            string strText)
        {
            // <w:p>
            writer.WriteStartElement("w", "p", m_strWordMlNsUri);
            // <w:r>
            writer.WriteStartElement("w", "r", m_strWordMlNsUri);
            // <w:t>
            writer.WriteStartElement("w", "t", m_strWordMlNsUri);

            writer.WriteString(strText);

            // <w:t>
            writer.WriteEndElement();
            // <w:r>
            writer.WriteEndElement();
            // <w:p>
            writer.WriteEndElement();
        }

        // ���������ͱ�����
        int WriteTableBegin(
            XmlTextWriter writer,
            PrintOption option,
            Hashtable macro_table)
        {

            // ������
            string strTableTitleText = option.TableTitle;

            if (String.IsNullOrEmpty(strTableTitleText) == false)
            {

                strTableTitleText = Global.MacroString(macro_table,
                    strTableTitleText);

                WriteParagraph(writer, strTableTitleText);
            }

            // <w:tbl>
            writer.WriteStartElement("w", "tbl", m_strWordMlNsUri);

            WriteTableProperty(writer);

            // <w:tr>
            writer.WriteStartElement("w", "tr", m_strWordMlNsUri);

            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];

                string strCaption = column.Caption;

                // ���û��caption���壬��Ų��name����
                if (String.IsNullOrEmpty(strCaption) == true)
                    strCaption = column.Name;

                // string strClass = Global.GetLeft(column.Name);

                // <w:tc>
                writer.WriteStartElement("w", "tc", m_strWordMlNsUri);

                WriteParagraph(writer, strCaption);

                // <w:tc>
                writer.WriteEndElement();
            }

            // <w:tr>
            writer.WriteEndElement();

            return 0;
        }

        void WriteTableEnd(XmlTextWriter writer)
        {
            writer.WriteEndElement();
        }

        static void WriteValuePair(IXLWorksheet sheet,
            int nRowIndex,
            string strName,
            string strValue)
        {
            sheet.Cell(nRowIndex, 1).Value = strName;
            sheet.Cell(nRowIndex, 2).Value = strValue;
        }

        // ������ı��ļ�
        int OutputToTextFile(
            List<ListViewItem> items,
            StreamWriter sw,
            ref XLWorkbook doc,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            Hashtable macro_table = new Hashtable();

            string strNamePath = "accountbook_printoption_text";

            // ��ô�ӡ����
            PrintOption option = new AccountBookPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);

            // ��鵱ǰ����״̬�Ͱ����ּ۸���֮���Ƿ����ì��
            if (bHasBiblioPriceColumn(option) == true)
            {

                if (this.SortColumns_in.Count != 0
                    && this.SortColumns_in[0].No == COLUMN_BIBLIORECPATH)
                {
                }
                else
                {
                    MessageBox.Show(this, "���ڵ�ǰ��ӡ�õ��� ���ּ۸��У�Ϊ��֤��ӡ�����׼ȷ�������Զ��� ���ּ�¼·���� �ж�ȫ���б��������һ���Զ�����\r\n\r\nΪ����������Զ����򣬿��ڴ�ӡǰ������������������з����Լ���Ը������ֻҪ���һ�ε���ǡ��ּ�¼·���������⼴�ɡ�");
                    ForceSortColumnsIn(COLUMN_BIBLIORECPATH);
                }
            }

            // 2009/7/24 changed
            if (this.SourceStyle == "batchno")
            {
                macro_table["%batchno%"] = HttpUtility.HtmlEncode(this.BatchNo); // ���κ�
                macro_table["%location%"] = HttpUtility.HtmlEncode(this.LocationString); // �ݲصص� ��HtmlEncode()��ԭ����Ҫ��ֹ������ֵġ�<��ָ��>������
            }
            else
            {
                macro_table["%batchno%"] = "";
                macro_table["%location%"] = "";
            }

            // macro_table["%pagecount%"] = nPageCount.ToString();
            // macro_table["%linesperpage%"] = option.LinesPerPage.ToString();
            macro_table["%date%"] = DateTime.Now.ToLongDateString();

            // 2009/7/24 changed
            if (this.SourceStyle == "barcodefile")
            {
                macro_table["%barcodefilepath%"] = this.BarcodeFilePath;
                macro_table["%barcodefilename%"] = Path.GetFileName(this.BarcodeFilePath);
            }
            else
            {
                macro_table["%barcodefilepath%"] = "";
                macro_table["%barcodefilename%"] = "";
            }

            // 2009/7/30 new add
            if (this.SourceStyle == "recpathfile")
            {
                macro_table["%recpathfilepath%"] = this.RecPathFilePath;
                macro_table["%recpathfilename%"] = Path.GetFileName(this.RecPathFilePath);
            }
            else
            {
                Debug.Assert(this.SourceStyle == "batchno" || this.SourceStyle == "barcodefile", "");

                macro_table["%recpathfilepath%"] = "";
                macro_table["%recpathfilename%"] = "";
            }

            macro_table["%sourcedescription%"] = this.SourceDescription;

            IXLWorksheet sheet = null;

            // ���ͳ����Ϣҳ
            if (this.TextOutputStatisPart == true)
            {
                if (doc != null)
                {
                    sheet = doc.Worksheets.Add("ͳ��ҳ");
                    sheet.Style.Font.FontName = this.Font.Name;
                }

                int nItemCount = items.Count;
                int nBiblioCount = GetBiblioCount(items);
                string strTotalPrice = GetTotalPrice(items);

                macro_table["%itemcount%"] = nItemCount.ToString();
                macro_table["%bibliocount%"] = nBiblioCount.ToString();
                macro_table["%totalprice%"] = strTotalPrice;

                macro_table["%pageno%"] = "1";

                // 2008/11/23 new add
                macro_table["%datadir%"] = this.MainForm.DataDir;   // ��������datadir��templatesĿ¼�ڵ�ĳЩ�ļ�
                ////macro_table["%libraryserverdir%"] = this.MainForm.LibraryServerDir;  // �������÷������˵�CSS�ļ�

                string strTemplateFilePath = option.GetTemplatePageFilePath("ͳ��ҳ");
                if (String.IsNullOrEmpty(strTemplateFilePath) == false)
                {
                    /*
                     * TODO���޸�Ϊ���ı���ʽ
<html>
<head>
	<LINK href='%libraryserverdir%/accountbook.css' type='text/css' rel='stylesheet'>
</head>
<body>
	<div class='pageheader'>%date% �Ʋ��ʲ� -- %sourcedescription% -- (�� %pagecount% ҳ)</div>
	<div class='tabletitle'>%date% �Ʋ��ʲ� -- %sourcedescription%</div>
	<div class='itemcount'>����: %itemcount%</div>
	<div class='bibliocount'>����: %bibliocount%</div>
	<div class='totalprice'>�ܼ�: %totalprice%</div>
	<div class='sepline'><hr/></div>
	<div class='batchno'>���κ�: %batchno%</div>
	<div class='location'>�ݲصص�: %location%</div>
	<div class='location'>������ļ�: %barcodefilepath%</div>
	<div class='location'>��¼·���ļ�: %recpathfilepath%</div>
	<div class='pagefooter'>%pageno%/%pagecount%</div>
</body>
</html>
                     * * */

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

                    if (sw != null)
                        sw.WriteLine(strResult);

                    // TODO: string --> excel page
                }
                else
                {
                    // ȱʡ�Ĺ̶����ݴ�ӡ

                    // ������
                    if (sw != null)
                    {
                        sw.WriteLine("����\t" + nItemCount.ToString());
                        sw.WriteLine("����\t" + nBiblioCount.ToString());
                        sw.WriteLine("�ܼ�\t" + strTotalPrice);

                        sw.WriteLine("----------");


                        if (this.SourceStyle == "batchno")
                        {
                            // 2008/11/22 new add
                            if (String.IsNullOrEmpty(this.BatchNo) == false)
                            {
                                sw.WriteLine("���κ�\t" + this.BatchNo);
                            }
                            if (String.IsNullOrEmpty(this.LocationString) == false
                                && this.LocationString != "<��ָ��>")
                            {
                                sw.WriteLine("�ݲصص�\t" + this.LocationString);
                            }
                        }

                        if (this.SourceStyle == "barcodefile")
                        {
                            if (String.IsNullOrEmpty(this.BarcodeFilePath) == false)
                            {
                                sw.WriteLine("������ļ�\t" + this.BarcodeFilePath);
                            }
                        }

                        // 2009/7/30 new add
                        if (this.SourceStyle == "recpathfile")
                        {
                            if (String.IsNullOrEmpty(this.RecPathFilePath) == false)
                            {
                                sw.WriteLine("��¼·���ļ�\t" + this.RecPathFilePath);
                            }
                        }


                        sw.WriteLine("----------");
                        sw.WriteLine("");
                    }

                    if (doc != null)
                    {
#if NO
                        int nLineIndex = 2;

                        doc.WriteExcelLine(
    nLineIndex++,
    "����",
    nItemCount.ToString());

                        doc.WriteExcelLine(
    nLineIndex++,
    "����",
    nBiblioCount.ToString());

                        doc.WriteExcelLine(
nLineIndex++,
"�ܼ�",
strTotalPrice);
#endif
                        
                        int nLineIndex = 2;

                        WriteValuePair(sheet,
    nLineIndex++,
    "����",
    nItemCount.ToString());

                        WriteValuePair(sheet,
    nLineIndex++,
    "����",
    nBiblioCount.ToString());

                        WriteValuePair(sheet,
nLineIndex++,
"�ܼ�",
strTotalPrice);           
                    }

                }

            }

            string strMarcFilterFilePath = option.GetTemplatePageFilePath("MARC������");
            if (String.IsNullOrEmpty(strMarcFilterFilePath) == false)
            {
                nRet = PrepareMarcFilter(strMarcFilterFilePath, out strError);
                if (nRet == -1)
                    return -1;
            }

            if (doc != null)
            {
                sheet = doc.Worksheets.Add("�Ʋ���");
                sheet.Style.Font.FontName = this.Font.Name;

#if NO
                Columns columns = new Columns();
                DocumentFormat.OpenXml.Spreadsheet.Column column = new DocumentFormat.OpenXml.Spreadsheet.Column();
                column.Min = 4;
                column.Max = 4;
                column.Width = 40;
                column.CustomWidth = true;
                columns.Append(column);

                doc.WorkSheet.InsertAt(columns, 0);
#endif
#if NO
                List<int> widths = new List<int>(new int [] {4,4,4,40});
                SetColumnWidth(doc, widths);
#endif
            }

            // ���������ͱ�����
            BuildTextPageTop(option,
                macro_table,
                sw,
                sheet);

            stop.SetProgressValue(0);
            stop.SetProgressRange(0, items.Count);

            // �����ѭ��
            for (int i = 0; i < items.Count; i++)
            {
                Application.DoEvents();	// ���ý������Ȩ

                if (stop != null && stop.State != 0)
                {
                    strError = "�û��ж�";
                    return -1;
                }

                BuildTextTableLine(option,
                    items,
                    i,
                    sw,
                    // ref doc,
                    sheet,
                    this.TextTruncate);

                stop.SetProgressValue(i + 1);
            }

            return 0;
        }

        static void SetColumnWidth(ExcelDocument doc,
            List<int> widths)
        {
            Columns columns = new Columns();
            uint i = 1;
            foreach (int width in widths)
            {
                DocumentFormat.OpenXml.Spreadsheet.Column column = new DocumentFormat.OpenXml.Spreadsheet.Column();
                if (width != -1)
                {
                    // min max ��ʾ�з�Χ���
                    column.Min = UInt32Value.FromUInt32(i);
                    column.Max = UInt32Value.FromUInt32(i);

                    column.Width = width;
                    column.CustomWidth = true;
                    columns.Append(column);
                }
                i++;
            }

            doc.WorkSheet.InsertAt(columns, 0);
        }

        // ���������ͱ�����
        int BuildTextPageTop(PrintOption option,
            Hashtable macro_table,
            StreamWriter sw,
            // ref ExcelDocument doc
            IXLWorksheet sheet
            )
        {
            // ������
            string strTableTitleText = option.TableTitle;

            if (String.IsNullOrEmpty(strTableTitleText) == false)
            {

                strTableTitleText = Global.MacroString(macro_table,
                    strTableTitleText);

                if (sw != null)
                {
                    sw.WriteLine(strTableTitleText);
                    sw.WriteLine("");
                }

                if (sheet != null)
                {
#if NO
                    doc.WriteExcelTitle(0,
    option.Columns.Count,  // nTitleCols,
    strTableTitleText,
    6);
#endif
                    var header = sheet.Range(1, 1,
                        1, option.Columns.Count).Merge();
                    header.Value = strTableTitleText;
                    header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    // header.Style.Font.FontName = "΢���ź�";
                    header.Style.Font.Bold = true;
                    header.Style.Font.FontSize = 16;
                }
            }

            string strColumnTitleLine = "";

            List<int> widths = new List<int>();

            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];

                widths.Add(column.WidthChars);

                string strCaption = column.Caption;

                // ���û��caption���壬��Ų��name����
                if (String.IsNullOrEmpty(strCaption) == true)
                    strCaption = column.Name;

                // string strClass = Global.GetLeft(column.Name);

                if (i != 0)
                    strColumnTitleLine += "\t";

                strColumnTitleLine += strCaption;

                if (sheet != null)
                {
#if NO
                                    doc.WriteExcelCell(
            2,
            i,
            strCaption,
            true);
#endif
                    var cell = sheet.Cell(2+1, i+1);
                    cell.Value = strCaption;
                    // cell.Style.Font.FontName = "΢���ź�";
                    cell.Style.Font.Bold = true;

                    if (column.WidthChars != -1)
                        sheet.Column(i + 1).Width = column.WidthChars;
                }
            }

            if (sw != null)
                sw.WriteLine(strColumnTitleLine);

#if NO
            if (doc != null)
                SetColumnWidth(doc, widths);
#endif


            return 0;
        }

        const int _nTopIndex = 3;

        int BuildTextTableLine(PrintOption option,
            List<ListViewItem> items,
            int nIndex,
            StreamWriter sw,
            // ref ExcelDocument doc,
            IXLWorksheet sheet,
            bool bCutText)
        {
            string strError = "";
            int nRet = 0;

            if (nIndex >= items.Count)
            {
                strError = "error: nIndex(" + nIndex.ToString() + ") >= items.Count(" + items.Count.ToString() + ")";
                goto ERROR1;
            }

            ListViewItem item = items[nIndex];
            string strMARC = "";
            string strOutMarcSyntax = "";

            if (this.MarcFilter != null
                || option.HasEvalue() == true)
            {

                // TODO: �д���Ҫ���Ա����������������ڴ�ӡ������ŷ��֣�������

                // ���MARC��ʽ��Ŀ��¼
                string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);
                nRet = GetMarc(strBiblioRecPath,
                    out strMARC,
                    out strOutMarcSyntax,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (this.MarcFilter != null)
                {
                    this.ColumnTable.Clear();   // �����һ��¼����ʱ���������
                    this.MarcFilter.Host.UiItem = item; // ��ǰ���ڴ���� ListViewItem

                    // ����filter�е�Record��ض���
                    nRet = this.MarcFilter.DoRecord(
                        null,
                        strMARC,
                        strOutMarcSyntax,
                        nIndex,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }
            }

            // ��Ŀ����
            string strLineContent = "";

            // bool bBiblioSumLine = false;    // �Ƿ�Ϊ�ֵ����һ��(������)
            List<CellData> cells = new List<CellData>();
            int nColIndex = 0;

            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];
                bool bNumber = false;

                // int nIndex = nPage * option.LinesPerPage + nLine;

                /*
                if (nIndex >= items.Count)
                    break;

                ListViewItem item = items[nIndex];
                 * */
                string strContent = "";
                if (string.IsNullOrEmpty(column.Evalue) == false)
                {
                    Jurassic.ScriptEngine engine = new Jurassic.ScriptEngine();
                    engine.EnableExposedClrTypes = true;
                    engine.SetGlobalValue("syntax", strOutMarcSyntax);
                    engine.SetGlobalValue("biblio", new MarcRecord(strMARC));
                    strContent = engine.Evaluate(column.Evalue).ToString();

                }
                else
                {
                    strContent = GetColumnContent(item,
                        column.Name);

                    if (strContent == "!!!#")
                    {
                        // strContent = ((nPage * option.LinesPerPage) + nLine + 1).ToString();
                        strContent = (nIndex + 1).ToString();
                        bNumber = true;
                    }

                    if (strContent == "!!!biblioPrice")
                    {
                        // �����Լ��ǲ��Ǵ����л�����
                        string strCurLineBiblioRecPath = GetColumnContent(item, "biblioRecpath");

                        string strNextLineBiblioRecPath = "";

                        if (nIndex < items.Count - 1)
                        {
                            ListViewItem next_item = items[nIndex + 1];
                            strNextLineBiblioRecPath = GetColumnContent(next_item, "biblioRecpath");
                        }

                        if (strCurLineBiblioRecPath != strNextLineBiblioRecPath)
                        {
                            // �����л�����

                            // ����ǰ��Ĳ�۸�
                            strContent = ComputeBiblioPrice(items, nIndex).ToString();
                            // bBiblioSumLine = true;
                        }
                        else
                        {
                            // ������ͨ��
                            strContent = "";    //  "&nbsp;";
                        }

                    }
                }

                if (bCutText == true)
                {
                    // �ض��ַ���
                    if (column.MaxChars != -1)
                    {
                        if (strContent.Length > column.MaxChars)
                        {
                            strContent = strContent.Substring(0, column.MaxChars);
                            strContent += "...";
                        }
                    }
                }

                if (String.IsNullOrEmpty(strContent) == true)
                    strContent = "";    //  "&nbsp;";

                // string strClass = Global.GetLeft(column.Name);

                if (i != 0)
                    strLineContent += "\t";

                strLineContent += strContent;

                if (sheet != null)
                {
#if NO
                    CellData cell = new CellData(nColIndex++, strContent,
            !bNumber,
            5);
                    cells.Add(cell);
#endif
                    IXLCell cell = sheet.Cell(nIndex + _nTopIndex + 1, nColIndex + 1);
                    if (bNumber == true)
                        cell.Value = strContent;
                    else
                        cell.SetValue(strContent);
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    nColIndex++;
                }
            }

            /*
            if (bBiblioSumLine == false)
            {
                StreamUtil.WriteText(strFileName,
        "<tr class='content'>");
            }
            else
            {
                StreamUtil.WriteText(strFileName,
        "<tr class='content_biblio_sum'>");
            }*/
            if (sw != null)
                sw.WriteLine(strLineContent);

#if NO
            if (doc != null)
                doc.WriteExcelLine(nIndex + _nTopIndex,
                    cells,
                    WriteExcelLineStyle.AutoString);  // WriteExcelLineStyle.None
#endif

            return 0;
        ERROR1:
            if (sw != null)
                sw.WriteLine(strError);
        if (sheet != null)
        {
#if NO
            List<CellData> temp_cells = new List<CellData>();
            temp_cells.Add(new CellData(0, strError));
            doc.WriteExcelLine(nIndex + _nTopIndex, temp_cells);
#endif
            IXLCell cell = sheet.Cell(nIndex + _nTopIndex + 1, 1);
            cell.Value = strError;

        }
            return -1;
        }

        // ����htmlҳ��
        int BuildHtml(
            List<ListViewItem> items,
            out List<string> filenames,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            Hashtable macro_table = new Hashtable();

            string strNamePath = "accountbook_printoption_html";

            // ��ô�ӡ����
            PrintOption option = new AccountBookPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);

            // ��鵱ǰ����״̬�Ͱ����ּ۸���֮���Ƿ����ì��
            if (bHasBiblioPriceColumn(option) == true)
            {

                if (this.SortColumns_in.Count != 0
                    && this.SortColumns_in[0].No == COLUMN_BIBLIORECPATH)
                {
                }
                else
                {
                    MessageBox.Show(this, "���ڵ�ǰ��ӡ�õ��� ���ּ۸��У�Ϊ��֤��ӡ�����׼ȷ�������Զ��� ���ּ�¼·���� �ж�ȫ���б��������һ���Զ�����\r\n\r\nΪ����������Զ����򣬿��ڴ�ӡǰ������������������з����Լ���Ը������ֻҪ���һ�ε���ǡ��ּ�¼·���������⼴�ɡ�");
                    ForceSortColumnsIn(COLUMN_BIBLIORECPATH);
                }


            }


            // �����ҳ����
            int nTablePageCount = items.Count / option.LinesPerPage;
            if ((items.Count % option.LinesPerPage) != 0)
                nTablePageCount++;

            int nPageCount = nTablePageCount + 1;

            // 2009/7/24 changed
            if (this.SourceStyle == "batchno")
            {
                // 2008/11/22 new add
                macro_table["%batchno%"] = HttpUtility.HtmlEncode(this.BatchNo); // ���κ�
                macro_table["%location%"] = HttpUtility.HtmlEncode(this.LocationString); // �ݲصص� ��HtmlEncode()��ԭ����Ҫ��ֹ������ֵġ�<��ָ��>������
            }
            else
            {
                macro_table["%batchno%"] = "";
                macro_table["%location%"] = "";
            }

            macro_table["%pagecount%"] = nPageCount.ToString();
            macro_table["%linesperpage%"] = option.LinesPerPage.ToString();
            macro_table["%date%"] = DateTime.Now.ToLongDateString();

            // 2009/7/24 changed
            if (this.SourceStyle == "barcodefile")
            {
                macro_table["%barcodefilepath%"] = this.BarcodeFilePath;
                macro_table["%barcodefilename%"] = Path.GetFileName(this.BarcodeFilePath);
            }
            else
            {
                macro_table["%barcodefilepath%"] = "";
                macro_table["%barcodefilename%"] = "";
            }

            // 2009/7/30 new add
            if (this.SourceStyle == "recpathfile")
            {
                macro_table["%recpathfilepath%"] = this.RecPathFilePath;
                macro_table["%recpathfilename%"] = Path.GetFileName(this.RecPathFilePath);
            }
            else
            {
                Debug.Assert(this.SourceStyle == "batchno"
                    || this.SourceStyle == "barcodefile"
                    || this.SourceDescription == "",
                    "");

                macro_table["%recpathfilepath%"] = "";
                macro_table["%recpathfilename%"] = "";
            }

            macro_table["%sourcedescription%"] = this.SourceDescription;

            filenames = new List<string>();    // ÿҳһ���ļ�������������������ļ���

            string strFileNamePrefix = this.MainForm.DataDir + "\\~accountbook";

            string strFileName = "";

            // ���ͳ����Ϣҳ
            {
                int nItemCount = items.Count;
                int nBiblioCount = GetBiblioCount(items);
                string strTotalPrice = GetTotalPrice(items);

                macro_table["%itemcount%"] = nItemCount.ToString();
                macro_table["%bibliocount%"] = nBiblioCount.ToString();
                macro_table["%totalprice%"] = strTotalPrice;

                macro_table["%pageno%"] = "1";

                // 2008/11/23 new add
                macro_table["%datadir%"] = this.MainForm.DataDir;   // ��������datadir��templatesĿ¼�ڵ�ĳЩ�ļ�
                ////macro_table["%libraryserverdir%"] = this.MainForm.LibraryServerDir;  // �������÷������˵�CSS�ļ�
                // 2009/10/10 new add
                macro_table["%cssfilepath%"] = this.GetAutoCssUrl(option, "accountbook.css");  // �������÷������˻�css��ģ���CSS�ļ�

                // strFileName = strFileNamePrefix + "0" + ".html";
                strFileName = strFileNamePrefix + "0-" + Guid.NewGuid().ToString()+ ".html";

                filenames.Add(strFileName);

                string strTemplateFilePath = option.GetTemplatePageFilePath("ͳ��ҳ");
                if (String.IsNullOrEmpty(strTemplateFilePath) == false)
                {
                    /*
<html>
<head>
	���÷�<LINK href='%libraryserverdir%/accountbook.css' type='text/css' rel='stylesheet'>
	���÷�<LINK href='%cssfilepath%' type='text/css' rel='stylesheet'>
</head>
<body>
	<div class='pageheader'>%date% �Ʋ��ʲ� -- %sourcedescription% -- (�� %pagecount% ҳ)</div>
	<div class='tabletitle'>%date% �Ʋ��ʲ� -- %barcodefilepath%</div>
	<div class='itemcount'>����: %itemcount%</div>
	<div class='bibliocount'>����: %bibliocount%</div>
	<div class='totalprice'>�ܼ�: %totalprice%</div>
	<div class='sepline'><hr/></div>
	<div class='batchno'>���κ�: %batchno%</div>
	<div class='location'>�ݲصص�: %location%</div>
	<div class='location'>������ļ�: %barcodefilepath%</div>
	<div class='location'>��¼·���ļ�: %recpathfilepath%</div>
	<div class='sepline'><hr/></div>
	<div class='pagefooter'>%pageno%/%pagecount%</div>
</body>
</html>
                     * * */

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
                    StreamUtil.WriteText(strFileName,
                        strResult);
                }
                else
                {
                    // ȱʡ�Ĺ̶����ݴ�ӡ

                    BuildHtmlPageTop(option,
                        macro_table,
                        strFileName,
                        false);

                    // ������

                    StreamUtil.WriteText(strFileName,
                        "<div class='itemcount'>����: " + nItemCount.ToString() + "</div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='bibliocount'>����: " + nBiblioCount.ToString() + "</div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='totalprice'>�ܼ�: " + strTotalPrice + "</div>");

                    StreamUtil.WriteText(strFileName,
                        "<div class='sepline'><hr/></div>");


                    if (this.SourceStyle == "batchno")
                    {

                        // 2008/11/22 new add
                        if (String.IsNullOrEmpty(this.BatchNo) == false)
                        {
                            StreamUtil.WriteText(strFileName,
                                "<div class='batchno'>���κ�: " + this.BatchNo + "</div>");
                        }
                        if (String.IsNullOrEmpty(this.LocationString) == false
                            && this.LocationString != "<��ָ��>")
                        {
                            StreamUtil.WriteText(strFileName,
                                "<div class='location'>�ݲصص�: " + this.LocationString + "</div>");
                        }
                    }


                    if (this.SourceStyle == "barcodefile")
                    {
                        if (String.IsNullOrEmpty(this.BarcodeFilePath) == false)
                        {
                            StreamUtil.WriteText(strFileName,
                                "<div class='barcodefilepath'>������ļ�: " + this.BarcodeFilePath + "</div>");
                        }
                    }

                    // 2009/7/30
                    if (this.SourceStyle == "recpathfile")
                    {
                        if (String.IsNullOrEmpty(this.RecPathFilePath) == false)
                        {
                            StreamUtil.WriteText(strFileName,
                                "<div class='recpathfilepath'>��¼·���ļ�: " + this.RecPathFilePath + "</div>");
                        }
                    }

                    /*
                    StreamUtil.WriteText(strFileName,
                        "<div class='sepline'><hr/></div>");


                    StreamUtil.WriteText(strFileName,
                        "<div class='sender'>�ƽ���: </div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='recipient'>������: </div>");
                     * */


                    BuildHtmlPageBottom(option,
                        macro_table,
                        strFileName,
                        false);
                }

            }


            string strMarcFilterFilePath = option.GetTemplatePageFilePath("MARC������");
            if (String.IsNullOrEmpty(strMarcFilterFilePath) == false)
            {
#if NO
                ColumnFilterDocument filter = null;

                this.ColumnTable = new Hashtable();
                nRet = PrepareMarcFilter(strMarcFilterFilePath,
                    out filter,
                    out strError);
                if (nRet == -1)
                    return -1;

                //
                if (filter != null)
                    this.AssemblyFilter = filter.Assembly;
                else
                    this.AssemblyFilter = null;

                this.MarcFilter = filter;
#endif
                nRet = PrepareMarcFilter(strMarcFilterFilePath, out strError);
                if (nRet == -1)
                    return -1;
            }


            // ���ҳѭ��
            for (int i = 0; i < nTablePageCount; i++)
            {
                macro_table["%pageno%"] = (i + 1 + 1).ToString();

                // strFileName = strFileNamePrefix + (i + 1).ToString() + ".html";
                strFileName = strFileNamePrefix + (i + 1).ToString() + "-" + Guid.NewGuid().ToString() + ".html";

                filenames.Add(strFileName);

                BuildHtmlPageTop(option,
                    macro_table,
                    strFileName,
                    true);
                // ��ѭ��
                for (int j = 0; j < option.LinesPerPage; j++)
                {
                    BuildHtmlTableLine(option,
                        items,
                        strFileName, i, j);
                }

                BuildHtmlPageBottom(option,
                    macro_table,
                    strFileName,
                    true);
            }

            /*
            for (int i = 0; i < this.listView_in.Items.Count; i++)
            {

            }
             * */


            return 0;
        }


        // ����������ʽ��tab�ַ���
        static string IndentString(int nLevel)
        {
            if (nLevel <= 0)
                return "";
            return new string('\t', nLevel);
        }

        /*
        // 2009/10/10 new add
        // ���css�ļ���·��(����http:// ��ַ)���������Ƿ���С�ͳ��ҳ�����Զ�����
        string GetAutoCssUrl(PrintOption option)
        {
            string strCssFilePath = option.GetTemplatePageFilePath("css");  // ��Сд������
            if (String.IsNullOrEmpty(strCssFilePath) == false)
                return strCssFilePath;
            else
                return this.MainForm.LibraryServerDir + "/accountbook.css";    // ȱʡ��
        }*/

        // 2009/10/10 new add
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

        int BuildHtmlPageTop(PrintOption option,
            Hashtable macro_table,
            string strFileName,
            bool bOutputTable)
        {
            /*
            string strLibraryServerUrl = this.MainForm.AppInfo.GetString(
    "config",
    "circulation_server_url",
    "");
            int pos = strLibraryServerUrl.LastIndexOf("/");
            if (pos != -1)
                strLibraryServerUrl = strLibraryServerUrl.Substring(0, pos);
             * */

            // 2009/10/10 changed
            string strCssUrl = GetAutoCssUrl(option, "accountbook.css");

            string strLink = IndentString(2) + "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />\r\n";

            StreamUtil.WriteText(strFileName,
                "<html>\r\n"
                + IndentString(1) + "<head>\r\n" + strLink
                + IndentString(1) + "</head>\r\n"
                + IndentString(1) + "<body>\r\n");

           
            // ҳü
            string strPageHeaderText = option.PageHeader;

            if (String.IsNullOrEmpty(strPageHeaderText) == false)
            {
                strPageHeaderText = Global.MacroString(macro_table,
                    strPageHeaderText);

                StreamUtil.WriteText(strFileName,
                    IndentString(2) + "<div class='pageheader'>" + strPageHeaderText + "</div><!-- ҳü -->\r\n");

                /*
                StreamUtil.WriteText(strFileName,
                    "<hr class='pageheader' />");
                 * */
            }

            // ������
            string strTableTitleText = option.TableTitle;

            if (String.IsNullOrEmpty(strTableTitleText) == false)
            {

                strTableTitleText = Global.MacroString(macro_table,
                    strTableTitleText);

                StreamUtil.WriteText(strFileName,
                    IndentString(2) + "<div class='tabletitle'>" + strTableTitleText + "</div><!-- ������ -->\r\n");
            }

            if (bOutputTable == true)
            {

                // ���ʼ
                StreamUtil.WriteText(strFileName,
                    IndentString(2) + "<table class='table'><!-- ���ݱ��ʼ -->\r\n");   //   border='1'

                // ��Ŀ����
                StreamUtil.WriteText(strFileName,
                    IndentString(3) + "<tr class='column'><!-- ��Ŀ�����п�ʼ -->\r\n");

                for (int i = 0; i < option.Columns.Count; i++)
                {
                    Column column = option.Columns[i];

                    string strCaption = column.Caption;

                    // ���û��caption���壬��Ų��name����
                    if (String.IsNullOrEmpty(strCaption) == true)
                        strCaption = column.Name;

                    string strClass = StringUtil.GetLeft(column.Name);
                    if (strClass.Length > 0 && strClass[0] == '@')
                    {
                        strClass = "ext_" + strClass.Substring(1);
                    }

                    StreamUtil.WriteText(strFileName,
                        IndentString(4) + "<td class='" + strClass + "'>" + strCaption + "</td>\r\n");
                }

                StreamUtil.WriteText(strFileName,
                    IndentString(3) + "</tr><!-- ��Ŀ�����н��� -->\r\n");

            }

            return 0;
        }

        // �����ּ۸񡣼ٶ�nIndex�����л���(ͬһ�������һ��)
        decimal ComputeBiblioPrice(List<ListViewItem> items,
            int nIndex)
        {
            decimal total = 0;
            string strBiblioRecPath = GetColumnContent(items[nIndex], "biblioRecpath");
            for (int i = nIndex; i>=0; i--)
            {
                ListViewItem item = items[i];

                string strCurBiblioRecPath = GetColumnContent(item, "biblioRecpath");

                if (strCurBiblioRecPath != strBiblioRecPath)
                    break;

                string strPrice = GetColumnContent(item, "price");

                // ��ȡ��������
                string strPurePrice = PriceUtil.GetPurePrice(strPrice);

                if (String.IsNullOrEmpty(strPurePrice) == true)
                    continue;

                total += Convert.ToDecimal(strPurePrice);
            }

            return total;
        }

        // �Ƿ�����ּ۸���?
        static bool bHasBiblioPriceColumn(PrintOption option)
        {
            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];

                string strText = StringUtil.GetLeft(column.Name);


                if (strText == "biblioPrice"
                    || strText == "�ּ۸�")
                    return true;
            }

            return false;
        }

#if NO
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
                false,
                "", // strMarcSyntax
                out strOutMarcSyntax,
                out strMARC,
                out strError);
            if (nRet == -1)
                return -1;

            return 1;
        }
#endif

        int BuildHtmlTableLine(PrintOption option,
            List<ListViewItem> items,
            string strFileName,
            int nPage,
            int nLine)
        {
            // ��Ŀ����
            string strLineContent = "";
            int nRet = 0;

            bool bBiblioSumLine = false;    // �Ƿ�Ϊ�ֵ����һ��(������)

            int nIndex = nPage * option.LinesPerPage + nLine;

            if (nIndex >= items.Count)
                goto END1;

            ListViewItem item = items[nIndex];

            string strMARC = "";
            string strOutMarcSyntax = "";

            if (this.MarcFilter != null
                || option.HasEvalue() == true)
            {
                string strError = "";

                // TODO: �д���Ҫ���Ա����������������ڴ�ӡ������ŷ��֣�������

                // ���MARC��ʽ��Ŀ��¼
                string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);

                // TODO: ���� cache������ٶ�
                nRet = GetMarc(strBiblioRecPath,
                    out strMARC,
                    out strOutMarcSyntax,
                    out strError);
                if (nRet == -1)
                {
                    strLineContent = strError;
                    goto END1;
                }

                if (this.MarcFilter != null)
                {
                    this.ColumnTable.Clear();   // �����һ��¼����ʱ���������
                    this.MarcFilter.Host.UiItem = item; // ��ǰ���ڴ���� ListViewItem

                    // ����filter�е�Record��ض���
                    nRet = this.MarcFilter.DoRecord(
                        null,
                        strMARC,
                        strOutMarcSyntax,
                        nIndex,
                        out strError);
                    if (nRet == -1)
                    {
                        strLineContent = strError;
                        goto END1;
                    }
                }
            }

            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];

                /*
                int nIndex = nPage * option.LinesPerPage + nLine;

                if (nIndex >= items.Count)
                    break;

                ListViewItem item = items[nIndex];
                 * */

                string strContent = "";

                if (string.IsNullOrEmpty(column.Evalue) == false)
                {
                    Jurassic.ScriptEngine engine = new Jurassic.ScriptEngine();
                    engine.EnableExposedClrTypes = true;
                    engine.SetGlobalValue("syntax", strOutMarcSyntax);
                    engine.SetGlobalValue("biblio", new MarcRecord(strMARC));
                    strContent = engine.Evaluate(column.Evalue).ToString();

                }
                else
                {

                    strContent = GetColumnContent(item,
                        column.Name);

                    if (strContent == "!!!#")
                        strContent = ((nPage * option.LinesPerPage) + nLine + 1).ToString();

                    if (strContent == "!!!biblioPrice")
                    {
                        // �����Լ��ǲ��Ǵ����л�����
                        string strCurLineBiblioRecPath = GetColumnContent(item, "biblioRecpath");

                        string strNextLineBiblioRecPath = "";

                        if (nIndex < items.Count - 1)
                        {
                            ListViewItem next_item = items[nIndex + 1];
                            strNextLineBiblioRecPath = GetColumnContent(next_item, "biblioRecpath");
                        }

                        if (strCurLineBiblioRecPath != strNextLineBiblioRecPath)
                        {
                            // �����л�����

                            // ����ǰ��Ĳ�۸�
                            strContent = ComputeBiblioPrice(items, nIndex).ToString();
                            bBiblioSumLine = true;
                        }
                        else
                        {
                            // ������ͨ��
                            strContent = "&nbsp;";
                        }
                    }

                }

                // �ض��ַ���
                if (column.MaxChars != -1)
                {
                    if (strContent.Length > column.MaxChars)
                    {
                        strContent = strContent.Substring(0, column.MaxChars);
                        strContent += "...";
                    }
                }

                if (String.IsNullOrEmpty(strContent) == true)
                    strContent = "&nbsp;";

                string strClass = StringUtil.GetLeft(column.Name);
                if (strClass.Length > 0 && strClass[0] == '@')
                {
                    strClass = "ext_" + strClass.Substring(1);
                }

                strLineContent +=
                    IndentString(4) + "<td class='" + strClass + "'>" + strContent + "</td>\r\n";
            }

        END1:

            string strOdd = "";
            if (((nLine + 1) % 2) != 0) // ��ÿҳ�ڵ��к�����������
                strOdd = " odd";

            string strBiblioSum = "";
            if (bBiblioSumLine == true)
                strBiblioSum = " biblio_sum";

            // 2009/10/10 changed
            StreamUtil.WriteText(strFileName,
                IndentString(3) + "<tr class='content" + strBiblioSum + strOdd + "'><!-- ������"
                + (bBiblioSumLine == true ? "(��Ŀ����)" : "")
                + (nIndex + 1).ToString() + " -->\r\n");

            StreamUtil.WriteText(strFileName,
                strLineContent);

            StreamUtil.WriteText(strFileName,
                IndentString(3) + "</tr>\r\n");

            return 0;
        }


        // �����Ŀ����
        string GetColumnContent(ListViewItem item,
            string strColumnName)
        {
            // ȥ��"-- ?????"����
            /*
            string strText = strColumnName;
            int nRet = strText.IndexOf("--", 0);
            if (nRet != -1)
                strText = strText.Substring(0, nRet).Trim();
             * */

            string strText = StringUtil.GetLeft(strColumnName);

            // 2009/10/8
            // Ҫ��ColumnTableֵ
            if (strText.Length > 0 && strText[0] == '@')
            {
                strText = strText.Substring(1);

                /*
                if (this.ColumnTable.Contains(strText) == false)
                    return "error:�� '" + strText + "' ��ColumnTable��û���ҵ�";
                 * */

                return (string)this.ColumnTable[strText];
            }

            try
            {

                // Ҫ��Ӣ�Ķ�����
                switch (strText)
                {
                    case "no":
                    case "���":
                        return "!!!#";  // ����ֵ����ʾ���
                    case "barcode":
                    case "�������":
                        return item.SubItems[COLUMN_BARCODE].Text;
                    case "errorInfo":
                    case "summary":
                    case "ժҪ":
                        return item.SubItems[COLUMN_SUMMARY].Text;

                    case "isbnIssn":
                    case "ISBN/ISSN":
                        return item.SubItems[COLUMN_ISBNISSN].Text;
                    case "publishTime":
                    case "����ʱ��":
                        return item.SubItems[MERGED_COLUMN_PUBLISHTIME].Text;
                    case "volume":
                    case "����":
                        return item.SubItems[MERGED_COLUMN_VOLUME].Text;
                    case "orderClass":
                    case "�������":
                        return item.SubItems[EXTEND_COLUMN_CLASS].Text;
                    case "catalogNo":
                    case "��Ŀ��":
                        return item.SubItems[EXTEND_COLUMN_CATALOGNO].Text;
                    case "orderTime":
                    case "����ʱ��":
                        return item.SubItems[EXTEND_COLUMN_ORDERTIME].Text;
                    case "orderID":
                    case "������":
                        return item.SubItems[EXTEND_COLUMN_ORDERID].Text;
                    case "seller":
                    case "����":
                    case "����":
                        return item.SubItems[EXTEND_COLUMN_SELLER].Text;
                    case "source":
                    case "������Դ":
                        return item.SubItems[EXTEND_COLUMN_SOURCE].Text;

                    case "orderPrice":
                    case "������":
                        return ListViewUtil.GetItemText(item, EXTEND_COLUMN_ORDERPRICE);

                    case "acceptPrice":
                    case "�����":
                        return ListViewUtil.GetItemText(item, EXTEND_COLUMN_ACCEPTPRICE);

                    case "state":
                        return item.SubItems[COLUMN_STATE].Text;
                    case "location":
                    case "�ݲصص�":
                        return item.SubItems[COLUMN_LOCATION].Text;
                    case "price":
                    case "��۸�":
                        return item.SubItems[COLUMN_PRICE].Text;
                    case "bookType":
                        return item.SubItems[COLUMN_BOOKTYPE].Text;
                    case "registerNo":
                        return item.SubItems[COLUMN_REGISTERNO].Text;
                    case "comment":
                        return item.SubItems[COLUMN_COMMENT].Text;
                    case "mergeComment":
                        return item.SubItems[COLUMN_MERGECOMMENT].Text;
                    case "batchNo":
                        return item.SubItems[COLUMN_BATCHNO].Text;
                        /*
                    case "borrower":
                        return item.SubItems[COLUMN_BORROWER].Text;
                    case "borrowDate":
                        return item.SubItems[COLUMN_BORROWDATE].Text;
                    case "borrowPeriod":
                        return item.SubItems[COLUMN_BORROWPERIOD].Text;
                         * */
                    case "recpath":
                        return item.SubItems[COLUMN_RECPATH].Text;
                    case "biblioRecpath":
                    case "�ּ�¼·��":
                        return item.SubItems[COLUMN_BIBLIORECPATH].Text;
                    case "accessNo":
                    case "�����":
                    case "��ȡ��":
                        return item.SubItems[COLUMN_ACCESSNO].Text;
                    case "biblioPrice":
                    case "�ּ۸�":
                        return "!!!biblioPrice";  // ����ֵ����ʾ�ּ۸�
                    case "refID":
                    case "�ο�ID":
                        return item.SubItems[COLUMN_REFID].Text;
                    default:
                        {
                            if (this.ColumnTable.Contains(strText) == false)
                                return "δ֪��Ŀ '" + strText + "'";

                            return (string)this.ColumnTable[strText];
                        }
                }
            }

            catch
            {
                return null;    // ��ʾû�����subitem�±�
            }

        }

        int BuildHtmlPageBottom(PrintOption option,
            Hashtable macro_table,
            string strFileName,
            bool bOutputTable)
        {


            if (bOutputTable == true)
            {
                // ������
                StreamUtil.WriteText(strFileName,
                    IndentString(2) + "</table><!-- ���ݱ����� -->\r\n");
            }

            // ҳ��
            string strPageFooterText = option.PageFooter;

            if (String.IsNullOrEmpty(strPageFooterText) == false)
            {
                /*
                StreamUtil.WriteText(strFileName,
                    "<hr class='pagefooter' />");
                 * */


                strPageFooterText = Global.MacroString(macro_table,
                    strPageFooterText);

                StreamUtil.WriteText(strFileName,
                    IndentString(2) + "<div class='pagefooter'>" + strPageFooterText + "</div><!-- ҳ�� -->\r\n");
            }


            StreamUtil.WriteText(strFileName, IndentString(1) + "</body>\r\n</html>");

            return 0;
        }

        static int GetBiblioCount(List<ListViewItem> items)
        {
            List<string> paths = new List<string>();

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strText = "";

                try
                {
                    strText = item.SubItems[COLUMN_BIBLIORECPATH].Text;
                }
                catch
                {
                    continue;
                }
                paths.Add(strText);
            }

            // ����
            paths.Sort();

            int nCount = 0;
            string strPrev = "";
            for (int i = 0; i < paths.Count; i++)
            {
                if (strPrev != paths[i])
                {
                    nCount++;
                    strPrev = paths[i];
                }
            }

            return nCount;
        }

#if NO
        static decimal GetTotalPrice(List<ListViewItem> items)
        {
            decimal total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strPrice = "";

                try
                {
                    strPrice = item.SubItems[COLUMN_PRICE].Text;
                }
                catch
                {
                    continue;
                }

                if (String.IsNullOrEmpty(strPrice) == true)
                    continue;

                // ��ȡ��������
                string strPurePrice = PriceUtil.GetPurePrice(strPrice);

                if (String.IsNullOrEmpty(strPurePrice) == true)
                    continue;

                total += Convert.ToDecimal(strPurePrice);
            }

            return total;
        }
#endif

        static string GetTotalPrice(List<ListViewItem> items)
        {
            List<string> prices = new List<string>();
            foreach (ListViewItem item in items)
            {
                string strPrice = "";

                try
                {
                    strPrice = item.SubItems[COLUMN_PRICE].Text;
                }
                catch
                {
                    continue;
                }

                if (String.IsNullOrEmpty(strPrice) == true)
                    continue;

                prices.Add(strPrice);
            }

            string strError = "";
            string strTotalPrice = "";
                            // ���ܼ۸�
        // ���ҵ�λ��ͬ�ģ��������
        // ��������������һ���汾���Ƿ���List<string>��
        // return:
        //      -1  error
        //      0   succeed
            int nRet = PriceUtil.TotalPrice(prices,
            out strTotalPrice,
            out strError);
            if (nRet == -1)
                return strError;

            return strTotalPrice;
        }

        private void listView_in_DoubleClick(object sender, EventArgs e)
        {
            LoadToEntityForm(this.listView_in);
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

            EntityForm form = new EntityForm();

            form.MainForm = this.MainForm;
            form.MdiParent = this.MainForm;
            form.Show();

            if (String.IsNullOrEmpty(strBarcode) == false)
                form.LoadItemByBarcode(strBarcode, false);
            else
                form.LoadItemByRecPath(strRecPath, false);
        }

#if NO
        // ���� ���κ� �� �ݲصص� �����еļ�¼·��д���ļ�
        // parameters:
        //      strBatchNo Ҫ�޶������κš����Ϊ "" ��ʾ���κ�Ϊ�գ��� null ��ʾ��ָ�����κ�
        //      strLocation Ҫ�޶��Ĺݲصص����ơ����Ϊ "" ��ʾ�ݲصص�Ϊ�գ��� null ��ʾ��ָ���ݲصص�
        int SearchBatchNoAndLocation(
            string strBatchNo,
            string strLocation,
            string strOutputFilename,
            out string strError)
        {
            strError = "";
            long lRet = 0;

            EnableControls(false);
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ������κ� '"+strBatchNo+"' �͹ݲصص� '"+strLocation+"' ...");
            stop.BeginLoop();

            try
            {


                string strQueryXml = "";

                if (strBatchNo != null
                    && strLocation != null)
                {
                    string strBatchNoQueryXml = "";
                    lRet = Channel.SearchItem(
        stop,
         this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
        strBatchNo,
        -1,
        "���κ�",
        "exact",
        this.Lang,
        "null",   // strResultSetName
        "",    // strSearchStyle
        "__buildqueryxml", // strOutputStyle
        out strError);
                    if (lRet == -1)
                        return -1;
                    strBatchNoQueryXml = strError;

                    string strLocationQueryXml = "";
                    lRet = Channel.SearchItem(
        stop,
         this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
        strLocation,
        -1,
        "�ݲصص�",
        "exact",
        this.Lang,
        "null",   // strResultSetName
        "",    // strSearchStyle
        "__buildqueryxml", // strOutputStyle
        out strError);
                    if (lRet == -1)
                        return -1;
                    strLocationQueryXml = strError;

                    // �ϲ���һ������ʽ
                    strQueryXml = "<group>" + strBatchNoQueryXml + "<operator value='AND'/>" + strLocationQueryXml + "</group>";    // !!!
#if DEBUG
                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strQueryXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "�ϲ�����ʽ����DOMʱ����: " + ex.Message;
                        return -1;
                    }
#endif


                }
                else if (strBatchNo != null)
                {
                    stop.SetMessage("���ڼ������κ� '" + strBatchNo + "' ...");

                    lRet = Channel.SearchItem(
        stop,
         this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
        strBatchNo,
        -1,
        "���κ�",
        "exact",
        this.Lang,
        "null",   // strResultSetName
        "",    // strSearchStyle
        "__buildqueryxml", // strOutputStyle
        out strError);
                    if (lRet == -1)
                        return -1;
                    strQueryXml = strError;
                }
                else if (strLocation != null)
                {
                    stop.SetMessage("���ڼ����ݲصص� '" + strLocation + "' ...");

                    lRet = Channel.SearchItem(
    stop,
    this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
    strLocation,    // strBatchNo, BUG !!!
    -1,
    "�ݲصص�",
    "exact",
    this.Lang,
    "null",   // strResultSetName
    "",    // strSearchStyle
    "__buildqueryxml", // strOutputStyle
    out strError);
                    if (lRet == -1)
                        return -1;
                    strQueryXml = strError;
                }
                else
                {
                    Debug.Assert(strBatchNo == null && strLocation == null,
                        "");
                    lRet = Channel.SearchItem(
    stop,
    this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
    "", // strBatchNo,
    -1,
    "__id",
    "left",
    this.Lang,
    "null",   // strResultSetName
    "",    // strSearchStyle
    "__buildqueryxml", // strOutputStyle
    out strError);
                    if (lRet == -1)
                        return -1;
                    strQueryXml = strError;
                }

                long lHitCount = 0;

                using (StreamWriter sw = new StreamWriter(strOutputFilename))
                {
                    lRet = Channel.Search(stop,
        strQueryXml,
        "default",
        "id",   // ֻҪ��¼·��
        out strError);
                    if (lRet == -1)
                        return -1;
                    if (lRet == 0)
                        return 0;   // û������

                    lHitCount = lRet;

                    stop.SetProgressRange(0, lHitCount);

                    long lStart = 0;
                    long lPerCount = Math.Min(150, lHitCount);
                    DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                    // װ�������ʽ
                    for (; ; )
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null && stop.State != 0)
                        {
                            strError = "�û��ж�";
                        }

                        // stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                        lRet = Channel.GetSearchResult(
                            stop,
                            "default",   // strResultSetName
                            lStart,
                            lPerCount,
                            "id",
                            this.Lang,
                            out searchresults,
                            out strError);
                        if (lRet == -1)
                            return -1;

                        if (lRet == 0)
                        {
                            strError = "GetSearchResult() error";
                            return -1;
                        }

                        // ����������
                        foreach (DigitalPlatform.CirculationClient.localhost.Record record in searchresults)
                        {
                            sw.WriteLine(record.Path);
                        }

                        lStart += searchresults.Length;
                        // lCount -= searchresults.Length;
                        if (lStart >= lHitCount || lPerCount <= 0)
                            break;

                        stop.SetProgressValue(lStart);
                    }
                }


                return (int)lHitCount;
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

        }
#endif

        // �������κż���װ��
        private void button_load_loadFromBatchNo_Click(object sender, EventArgs e)
        {
            SearchByBatchnoForm dlg = new SearchByBatchnoForm();
            MainForm.SetControlFont(dlg, this.Font, false);

            // 2008/11/30 new add
            dlg.BatchNo = this.BatchNo;
            dlg.ItemLocation = this.LocationString;

            dlg.CfgSectionName = "AccountBookForm_SearchByBatchnoForm";
            dlg.GetLocationValueTable -= new GetValueTableEventHandler(dlg_GetLocationValueTable);
            dlg.GetLocationValueTable += new GetValueTableEventHandler(dlg_GetLocationValueTable);

            dlg.GetBatchNoTable -= new GetKeyCountListEventHandler(dlg_GetBatchNoTable);
            dlg.GetBatchNoTable += new GetKeyCountListEventHandler(dlg_GetBatchNoTable);
            dlg.RefDbName = "";
            /*
                dlg.RefDbName = EntityForm.GetDbName(this.entityEditControl1.RecPath);
             * */
            dlg.MainForm = this.MainForm;

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.SourceStyle = "batchno";

            // 2008/11/22 new add
            this.BatchNo = dlg.BatchNo;
            this.LocationString = dlg.ItemLocation;

            string strMatchLocation = dlg.ItemLocation;
            if (strMatchLocation == "<��ָ��>")
                strMatchLocation = null;    // null��""������ܴ�

            string strBatchNo = dlg.BatchNo;
            if (strBatchNo == "<��ָ��>")
                strBatchNo = null;    // null��""������ܴ�

            string strError = "";

            bool bClearBefore = true;
            if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                bClearBefore = false;

            if (bClearBefore == true)
                ClearBefore();

            string strRecPathFilename = Path.GetTempFileName();

            try
            {
                // ���� ���κ� �� �ݲصص� �����еļ�¼·��д���ļ�
                int nRet = SearchBatchNoAndLocation(
                    this.comboBox_load_type.Text,
                    strBatchNo,
                    strMatchLocation,
                    strRecPathFilename,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                nRet = LoadFromRecPathFile(strRecPathFilename,
                    this.comboBox_load_type.Text,
                    this.checkBox_load_fillBiblioSummary.Checked,
                    new string[] { "summary", "@isbnissn" },
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

            }
            finally
            {
                if (string.IsNullOrEmpty(strRecPathFilename) == false)
                {
                    File.Delete(strRecPathFilename);
                    strRecPathFilename = "";
                }
            }
            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }

#if NO
        // �������κż���װ��
        private void button_load_loadFromBatchNo_Click(object sender, EventArgs e)
        {
            SearchByBatchnoForm dlg = new SearchByBatchnoForm();
            MainForm.SetControlFont(dlg, this.Font, false);

            // 2008/11/30 new add
            dlg.BatchNo = this.BatchNo;
            dlg.ItemLocation = this.LocationString;

            dlg.CfgSectionName = "AccountBookForm_SearchByBatchnoForm";
            dlg.GetLocationValueTable -= new GetValueTableEventHandler(dlg_GetLocationValueTable);
            dlg.GetLocationValueTable += new GetValueTableEventHandler(dlg_GetLocationValueTable);

            dlg.GetBatchNoTable -= new GetKeyCountListEventHandler(dlg_GetBatchNoTable);
            dlg.GetBatchNoTable += new GetKeyCountListEventHandler(dlg_GetBatchNoTable);
            dlg.RefDbName = "";
            /*
                dlg.RefDbName = EntityForm.GetDbName(this.entityEditControl1.RecPath);
             * */
            dlg.MainForm = this.MainForm;

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.SourceStyle = "batchno";

            // 2008/11/22 new add
            this.BatchNo = dlg.BatchNo;
            this.LocationString = dlg.ItemLocation;

            string strMatchLocation = dlg.ItemLocation;

            if (strMatchLocation == "<��ָ��>")
                strMatchLocation = null;    // null��""������ܴ�

            string strError = "";

            if (Control.ModifierKeys == Keys.Control)
            {
            }
            else
            {
                this.listView_in.Items.Clear();
                this.SortColumns_in.Clear();
                SortColumns.ClearColumnSortDisplay(this.listView_in.Columns);

                this.refid_table.Clear();
                this.orderxml_table.Clear();
            }

            EnableControls(false);
            //MainForm.ShowProgress(true);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            try
            {
                stop.SetProgressRange(0, 100);
                // stop.SetProgressValue(0);

                long lRet = Channel.SearchItem(
                    stop,
                    // 2010/2/25 changed
                     this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
                    dlg.BatchNo,
                    -1,
                    "���κ�",
                    string.IsNullOrEmpty(dlg.BatchNo) == false ? "exact" : "left",  // ����������κ�Ϊ�ռ���
                    this.Lang,
                    "batchno",   // strResultSetName
                    "",    // strSearchStyle
                    "", // strOutputStyle
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (lRet == 0)
                {
                    strError = "���κ� '"+dlg.BatchNo+"' û�����м�¼��";
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
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                MessageBox.Show(this, "�û��ж�");
                                return;
                            }
                        }

                        DigitalPlatform.CirculationClient.localhost.Record result_item = searchresults[i];

                        string strBarcode = result_item.Cols[0];
                        string strRecPath = result_item.Path;

                        /*
                        // ����������Ϊ�գ������·��װ��
                        // 2009/8/6 new add
                        if (String.IsNullOrEmpty(strBarcode) == true)
                        {
                            strBarcode = "@path:" + strRecPath;
                        }
                         * */

                        // ����
                        strBarcode = "@path:" + strRecPath;

                        string strOutputItemRecPath = "";
                        ListViewItem item = null;
                        // ���ݲ�����Ż��߼�¼·����װ����¼
                        // return: 
                        //      -2  ��������Ѿ���list�д�����
                        //      -1  ����
                        //      0   ��Ϊ�ݲصص㲻ƥ�䣬û�м���list��
                        //      1   �ɹ�
                        int nRet = LoadOneItem(
                            this.comboBox_load_type.Text,
                            strBarcode,
                            null,
                            this.listView_in,
                            strMatchLocation,
                            out strOutputItemRecPath,
                            out item,
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

                        // TODO: �Ƿ�Ҫ����¼·��������ͼ������ڿ����Ƿ���ȷ?

                    }

                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    stop.SetMessage("������ " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " ��");

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                }

                if (this.listView_in.Items.Count == 0
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

            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif


        void dlg_GetBatchNoTable(object sender, GetKeyCountListEventArgs e)
        {
            Global.GetBatchNoTable(e,
                this,
                this.comboBox_load_type.Text,
                "item",
                this.stop,
                this.Channel);

#if NOOOOOOOOOOOOOOOOOOO
            string strError = "";

            if (e.KeyCounts == null)
                e.KeyCounts = new List<KeyCount>();

            EnableControls(false);
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����г�ȫ�������κ� ...");
            stop.BeginLoop();

            try
            {
                MainForm.SetProgressRange(100);
                MainForm.SetProgressValue(0);

                long lRet = Channel.SearchItem(
                    stop,
                    "<all>",
                    "", // strBatchNo
                    2000,  // -1,
                    "���κ�",
                    "left",
                    this.Lang,
                    "batchno",   // strResultSetName
                    "keycount", // strOutputStyle
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (lRet == 0)
                {
                    strError = "û���ҵ��κβ����κż�����";
                    return;
                }

                long lHitCount = lRet;

                long lStart = 0;
                long lCount = lHitCount;
                SearchResult[] searchresults = null;

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
                        "keycount",
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "GetSearchResult() error: " + strError;
                        goto ERROR1;
                    }

                    if (lRet == 0)
                    {
                        // MessageBox.Show(this, "δ����");
                        return;
                    }

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        if (searchresults[i].Cols == null)
                        {
                            strError = "�����Ӧ�÷����������ݿ��ں˵����°汾";
                            goto ERROR1;
                        }

                        KeyCount keycount = new KeyCount();
                        keycount.Key = searchresults[i].Path;
                        keycount.Count = Convert.ToInt32(searchresults[i].Cols[0]);
                        e.KeyCounts.Add(keycount);
                    }

                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    stop.SetMessage("������ " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " ��");

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
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

            if (strSortStyle == "�������")
            {
                // ע��������������ֵ�һ���Ѿ����úã��򲻸ı��䷽�򡣲������Ⲣ����ζ���䷽��һ��������
                this.SortColumns_in.SetFirstColumn(COLUMN_BARCODE,
                    this.listView_in.Columns,
                    false);
            }
            else if (strSortStyle == "��¼��")
            {
                this.SortColumns_in.SetFirstColumn(COLUMN_REGISTERNO,
                    this.listView_in.Columns,
                    false);
            }
            else if (strSortStyle == "����")
            {
                this.SortColumns_in.SetFirstColumn(COLUMN_BARCODE,
                    this.listView_in.Columns,
                    false);
                this.SortColumns_in.SetFirstColumn(EXTEND_COLUMN_SELLER,
                    this.listView_in.Columns,
                    false);
            }
            else if (strSortStyle == "������Դ")
            {
                this.SortColumns_in.SetFirstColumn(COLUMN_BARCODE,
                    this.listView_in.Columns,
                    false);
                this.SortColumns_in.SetFirstColumn(EXTEND_COLUMN_SOURCE,
                    this.listView_in.Columns,
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
                this.listView_in.ListViewItemSorter = new SortColumnsComparer(this.SortColumns_in);
                this.listView_in.ListViewItemSorter = null;
            }
            finally
            {
                this.Cursor = oldCursor;
            }

            SetGroupBackcolor(
                this.listView_in,
                this.SortColumns_in[0].No);

            return 1;
        }

        void ForceSortColumnsIn(int nClickColumn)
        {
            // ע��������������ֵ�һ���Ѿ����úã��򲻸ı��䷽�򡣲������Ⲣ����ζ���䷽��һ��������
            this.SortColumns_in.SetFirstColumn(nClickColumn,
                this.listView_in.Columns,
                false);

            // ����
            this.listView_in.ListViewItemSorter = new SortColumnsComparer(this.SortColumns_in);
            this.listView_in.ListViewItemSorter = null;

            SetGroupBackcolor(
                this.listView_in,
                nClickColumn);
        }

        /*
        // �޸��������飬���õ�һ�У���ԭ�����к��ƺ�
        public static void ChangeSortColumns(ref List<int> SortColumns,
            int nFirstColumn)
        {
            // �������������Ѿ����ڵ�ֵ
            SortColumns.Remove(nFirstColumn);
            // �ŵ��ײ�
            SortColumns.Insert(0, nFirstColumn);
        }
         * */

        // ���������ֵ�ı仯����������ɫ
        static void SetGroupBackcolor(
            ListView list,
            int nSortColumn)
        {
            string strPrevText = "";
            bool bDark = false;
            for(int i=0;i<list.Items.Count;i++)
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
                    item.BackColor = System.Drawing.Color.LightGray;

                    if (item.ForeColor == item.BackColor)
                        item.ForeColor = System.Drawing.Color.Black;
                }
                else
                {
                    item.BackColor = System.Drawing.Color.White;

                    if (item.ForeColor == item.BackColor)
                        item.ForeColor = System.Drawing.Color.Black;
                }
            }
        }

        // �Լ������б��������
        private void listView_in_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;
            this.SortColumns_in.SetFirstColumn(nClickColumn,
                this.listView_in.Columns);

            // ����
            this.listView_in.ListViewItemSorter = new SortColumnsComparer(this.SortColumns_in);

            this.listView_in.ListViewItemSorter = null;

            SetGroupBackcolor(
                this.listView_in,
                nClickColumn);

        }

        private void listView_in_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_selectAll_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ˢ�� [" + this.listView_in.SelectedItems.Count.ToString() + "] (&S)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_refreshSelected_Click);
            if (this.listView_in.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            /*
            menuItem = new MenuItem("ˢ��ȫ����(&R)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_refreshAll_Click);
            contextMenu.MenuItems.Add(menuItem);
             * */

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("ɾ�� [" + this.listView_in.SelectedItems.Count.ToString() + "] (&D)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_deleteSelected_Click);
            if (this.listView_in.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.listView_in, new Point(e.X, e.Y));		
        }

        void menu_refreshSelected_Click(object sender, EventArgs e)
        {
            ListView list = (ListView)((MenuItem)sender).Tag;
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in list.SelectedItems)
            {
                items.Add(item);

            }
            RefreshLines(
                COLUMN_RECPATH,
                items, 
                this.checkBox_load_fillBiblioSummary.Checked,
                new string[] { "summary", "@isbnissn" });
        }

        void menu_selectAll_Click(object sender, EventArgs e)
        {
            ListView list = (ListView)((MenuItem)sender).Tag;

            ListViewUtil.SelectAllLines(list);
        }

#if NO
        void menu_refreshAll_Click(object sender, EventArgs e)
        {
            ListView list = (ListView)((MenuItem)sender).Tag;

            List<ListViewItem> items = new List<ListViewItem>();
            for (int i = 0; i < list.Items.Count; i++)
            {
                items.Add(list.Items[i]);
            }
            RefreshLines(items, this.checkBox_load_fillBiblioSummary.Checked);
        }
#endif

        // ɾ���������б����Ѿ�ѡ������
        void menu_deleteSelected_Click(object sender, EventArgs e)
        {
            ListView list = (ListView)((MenuItem)sender).Tag;


            if (list.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫɾ�������");
                return;
            }

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in list.SelectedItems)
            {
                items.Add(item);
            }

            DialogResult result = MessageBox.Show(this,
"ȷʵҪɾ��ѡ���� "+items.Count.ToString()+" ������?",
"dp2Circulation",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            DeleteLines(items);

        }

        static void DeleteLines(List<ListViewItem> items)
        {
            if (items.Count == 0)
                return;
            ListView list = items[0].ListView;

            for (int i = 0; i < items.Count; i++)
            {
                list.Items.Remove(items[i]);
            }
        }

#if NO
        void RefreshLines(List<ListViewItem> items,
    bool bFillBiblioSummary)
        {
            string strError = "";
            string strTimeMessage = "";
            int nRet = 0;

            EnableControls(false);
            // MainForm.ShowProgress(true);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ˢ�� ...");
            stop.BeginLoop();

            try
            {
                stop.SetProgressRange(0, items.Count);
                ProgressEstimate estimate = new ProgressEstimate();
                estimate.SetRange(0, items.Count);
                estimate.Start();

                int nLineCount = 0;
                List<string> lines = new List<string>();
                List<ListViewItem> part_items = new List<ListViewItem>();
                for (int i = 0; i < items.Count; i++)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�1";
                        goto ERROR1;
                    }

                    ListViewItem item = items[i];

                    stop.SetMessage("����ˢ�� " + item.Text + " ...");
                    stop.SetProgressValue(i);

                    string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

                    lines.Add(strRecPath);
                    part_items.Add(item);
                    if (lines.Count >= 100)
                    {
                        if (lines.Count > 0)
                            stop.SetMessage("(" + i.ToString() + " / " + nLineCount.ToString() + ") ����װ��·�� " + lines[0] + " �ȼ�¼��"
                                + "ʣ��ʱ�� " + ProgressEstimate.Format(estimate.Estimate(i)) + " �Ѿ���ʱ�� " + ProgressEstimate.Format(estimate.delta_passed));

                        // ����һС����¼��װ��
                        nRet = DoLoadRecords(lines,
                            part_items,
                            bFillBiblioSummary,
                            new string [] {"summary","@isbnissn"},
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        lines.Clear();
                        part_items.Clear();
                    }
                }

                // ���ʣ�µ�һ��
                if (lines.Count > 0)
                {
                    if (lines.Count > 0)
                        stop.SetMessage("(" + nLineCount.ToString() + " / " + nLineCount.ToString() + ") ����װ��·�� " + lines[0] + " �ȼ�¼...");

                    // ����һС����¼��װ��
                    nRet = DoLoadRecords(lines,
                        part_items,
                        bFillBiblioSummary,
                        new string[] { "summary", "@isbnissn" },
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    lines.Clear();
                    part_items.Clear();
                }

                strTimeMessage = "��ˢ�²���Ϣ " + nLineCount.ToString() + " �����ķ�ʱ��: " + estimate.GetTotalTime().ToString();

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("ˢ����ɡ�");
                stop.HideProgress();

                EnableControls(true);
                // MainForm.ShowProgress(false);
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif

#if NO
        void RefreshLines(List<ListViewItem> items,
            bool bFillBiblioSummary)
        {
            string strError = "";

            EnableControls(false);
            // MainForm.ShowProgress(true);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ˢ�� ...");
            stop.BeginLoop();

            try
            {
                stop.SetProgressRange(0, items.Count);
                // stop.SetProgressValue(0);


                for (int i = 0; i < items.Count; i++)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�1";
                        goto ERROR1;
                    }


                    ListViewItem item = items[i];

                    stop.SetMessage("����ˢ�� " + item.Text + " ...");

                    int nRet = RefreshOneItem(item, bFillBiblioSummary, out strError);
                    /*
                    if (nRet == -1)
                        MessageBox.Show(this, strError);
                     * */

                    stop.SetProgressValue(i);
                }

                stop.SetProgressValue(items.Count);

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("ˢ����ɡ�");
                stop.HideProgress();

                EnableControls(true);
                // MainForm.ShowProgress(false);
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif

#if NO
        public int RefreshOneItem(ListViewItem item,
            bool bFillBiblioSummary,
            out string strError)
        {
            strError = "";

            string strItemText = "";
            string strBiblioText = "";

            string strItemRecPath = "";
            string strBiblioRecPath = "";

            byte[] item_timestamp = null;

            string strBarcode = "";

            string strBarcodeOrRecPath = item.Text;
            if (StringUtil.HasHead(strBarcodeOrRecPath, "@path:") == true)
                strBarcode = strBarcodeOrRecPath;
            else
                strBarcode = "@path:" + item.SubItems[COLUMN_RECPATH].Text;
            REDO_GETITEMINFO:
            long lRet = Channel.GetItemInfo(
                stop,
                strBarcode,
                "xml",
                out strItemText,
                out strItemRecPath,
                out item_timestamp,
                "recpath",
                out strBiblioText,
                out strBiblioRecPath,
                out strError);
            if (lRet == -1)
            {
                DialogResult temp_result = MessageBox.Show(this,
strError + "\r\n\r\n�Ƿ�����?",
"AccountBookForm",
MessageBoxButtons.RetryCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (temp_result == DialogResult.Retry)
                    goto REDO_GETITEMINFO;
            } 
            if (lRet == -1 || lRet == 0)
            {
                ListViewUtil.ChangeItemText(item, 1, strError);

                SetItemColor(item, TYPE_ERROR);
                goto ERROR1;
            }

            string strBiblioSummary = "";
            string strISBnISSN = "";

            SummaryInfo info = (SummaryInfo)this.m_summaryTable[strBiblioRecPath];
            if (info != null)
            {
                strBiblioSummary = info.Summary;
                strISBnISSN = info.ISBnISSn;
            }

            if (strBiblioSummary == ""
                && (this.checkBox_load_fillBiblioSummary.Checked == true || bFillBiblioSummary == true ) )
            {
                string[] formats = new string[2];
                formats[0] = "summary";
                formats[1] = "@isbnissn";
                string[] results = null;
                byte[] timestamp = null;

                Debug.Assert( String.IsNullOrEmpty(strBiblioRecPath) == false, "strBiblioRecPathֵ����Ϊ��");
            REDO_GETBIBLIOINFO:
                lRet = Channel.GetBiblioInfos(
                    stop,
                    strBiblioRecPath,
                    formats,
                    out results,
                    out timestamp,
                    out strError);
                if (lRet == -1)
                {
                    DialogResult temp_result = MessageBox.Show(this,
    strError + "\r\n\r\n�Ƿ�����?",
    "AccountBookForm",
    MessageBoxButtons.RetryCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                    if (temp_result == DialogResult.Retry)
                        goto REDO_GETBIBLIOINFO;
                } 
                if (lRet == -1 || lRet == 0)
                {
                    if (lRet == 0 && String.IsNullOrEmpty(strError) == true)
                        strError = "��Ŀ��¼ '" + strBiblioRecPath + "' ������";

                    strBiblioSummary = "�����ĿժҪʱ��������: " + strError;

                    // TODO: ���results.Length������������ʵ�����Լ�������
                }
                else
                {
                    Debug.Assert(results != null && results.Length == 2, "results�������2��Ԫ��");
                    strBiblioSummary = results[0];
                    strISBnISSN = results[1];

                    // ����cacheռ�ݵ��ڴ�̫��
                    if (this.m_summaryTable.Count > 1000)
                        this.m_summaryTable.Clear();

                    if (info == null)
                    {
                        info = new SummaryInfo();
                        info.Summary = strBiblioSummary;
                        info.ISBnISSn = strISBnISSN;
                        this.m_summaryTable[strBiblioRecPath] = info;
                    }
                }
            }

            // ����һ�����xml��¼��ȡ���й���Ϣ����listview��
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strItemText);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                goto ERROR1;
            }


            {
                SetListViewItemText(dom,
                    true,
                    strItemRecPath,
                    strBiblioSummary,
                    strISBnISSN,
                    strBiblioRecPath,
                    item);
            }

            // ͼ��
            // item.ImageIndex = TYPE_NORMAL;
            SetItemColor(item, TYPE_NORMAL);

            // 2009/7/25 new add
            // �����Ҫ�Ӷ������õ���Ŀ��Ϣ
            if (this.checkBox_load_fillOrderInfo.Checked == true)
                FillOrderColumns(item, this.comboBox_load_type.Text);

            return 1;
        ERROR1:
            return -1;
        }
#endif

        // ������ı��ļ�
        private void button_print_outputTextFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.listView_in.Items.Count == 0)
            {
                strError = "û�п��������";
                goto ERROR1;
            }

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

            OutputAcountBookTextFileDialog option_dialog = new OutputAcountBookTextFileDialog();
            MainForm.SetControlFont(option_dialog, this.Font, false);

            option_dialog.Truncate = this.TextTruncate;
            option_dialog.OutputStatisPart = this.TextOutputStatisPart;
            option_dialog.StartPosition = FormStartPosition.CenterScreen;
            option_dialog.ShowDialog(this);

            if (option_dialog.DialogResult != DialogResult.OK)
                return;

            this.TextTruncate = option_dialog.Truncate;
            this.TextOutputStatisPart = option_dialog.OutputStatisPart;

            bool bAppend = true;

            if (File.Exists(this.ExportTextFilename) == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ı��ļ� '" + this.ExportTextFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel �������)",
                    "AccountBookForm",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Cancel)
                    return;
                if (result == DialogResult.No)
                    bAppend = false;
            }

            EnableControls(false);
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڹ���Ʋ��� ...");
            stop.BeginLoop();

            // �����ļ�
            StreamWriter sw = new StreamWriter(this.ExportTextFilename,
                bAppend,	// append
                System.Text.Encoding.UTF8);
            try
            {
                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                int nCount = 0;
                List<ListViewItem> items = new List<ListViewItem>();
                for (int i = 0; i < this.listView_in.Items.Count; i++)
                {
                    ListViewItem item = this.listView_in.Items[i];

                    items.Add(item);
                    nCount++;
                }

                XLWorkbook doc = null;

                // ������ı��ļ�
                int nRet = OutputToTextFile(
                    items,
                    sw,
                    ref doc,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.Cursor = oldCursor;

                string strExportStyle = "����";
                if (bAppend == true)
                    strExportStyle = "׷��";

                this.MainForm.StatusBarMessage = "�Ʋ��ʲ����� " + nCount.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportTextFilename;

            }
            finally
            {
                if (sw != null)
                    sw.Close();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                EnableControls(true);
            }

            return;
            ERROR1:
                MessageBox.Show(this, strError);
        }

        // ���ô�ӡѡ�� WordXML
        private void button_print_optionWordXml_Click(object sender, EventArgs e)
        {
            // ���ñ���ͷ��
            string strNamePath = "accountbook_printoption_wordxml";

            PrintOption option = new AccountBookPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);

            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.Text = this.comboBox_load_type.Text + " WordML ��ӡ����";
            dlg.DataDir = this.MainForm.DataDir;    // ��������ģ��ҳ
            dlg.PrintOption = option;
            dlg.ColumnItems = new string[] {
                "no -- ���",
                "barcode -- �������",
                "summary -- ժҪ",

                // 2009/7/24 new add
                "isbnIssn -- ISBN/ISSN",
                "publishTime -- ����ʱ��",
                "volume -- ����",
                "orderClass -- �������",
                "catalogNo -- ��Ŀ��",
                "orderTime -- ����ʱ��",
                "orderID -- ������",
                "seller -- ����",
                "source -- ������Դ",
                "orderPrice -- ������",
                "acceptPrice -- ���ռ�",

                "accessNo -- ��ȡ��",
                "state -- ״̬",
                "location -- �ݲصص�",
                "price -- ��۸�",
                "bookType -- ������",
                "registerNo -- ��¼��",
                "comment -- ע��",
                "mergeComment -- �ϲ�ע��",
                "batchNo -- ���κ�",
                /*
                "borrower -- ������",
                "borrowDate -- ��������",
                "borrowPeriod -- ��������",
                 * */
                "recpath -- ���¼·��",
                "biblioRecpath -- �ּ�¼·��",
                "biblioPrice -- �ּ۸�",
                "refID -- �ο�ID"
            };


            this.MainForm.AppInfo.LinkFormState(dlg, "accountbook_printoption_wordxml_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            option.SaveData(this.MainForm.AppInfo,
                strNamePath);
        }

        // ���ô�ӡѡ�� HTML
        private void button_print_optionHTML_Click(object sender, EventArgs e)
        {
            // ���ñ���ͷ��
            string strNamePath = "accountbook_printoption_html";

            PrintOption option = new AccountBookPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);

            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.Text = this.comboBox_load_type.Text + " HTML ��ӡ����";
            dlg.DataDir = this.MainForm.DataDir;    // ��������ģ��ҳ
            dlg.PrintOption = option;
            dlg.ColumnItems = new string[] {
                "no -- ���",
                "barcode -- �������",
                "summary -- ժҪ",

                // 2009/7/24 new add
                "isbnIssn -- ISBN/ISSN",
                "publishTime -- ����ʱ��",
                "volume -- ����",
                "orderClass -- �������",
                "catalogNo -- ��Ŀ��",
                "orderTime -- ����ʱ��",
                "orderID -- ������",
                "seller -- ����",
                "source -- ������Դ",
                "orderPrice -- ������",
                "acceptPrice -- ���ռ�",

                "accessNo -- ��ȡ��",
                "state -- ״̬",
                "location -- �ݲصص�",
                "price -- ��۸�",
                "bookType -- ������",
                "registerNo -- ��¼��",
                "comment -- ע��",
                "mergeComment -- �ϲ�ע��",
                "batchNo -- ���κ�",
                /*
                "borrower -- ������",
                "borrowDate -- ��������",
                "borrowPeriod -- ��������",
                 * */
                "recpath -- ���¼·��",
                "biblioRecpath -- �ּ�¼·��",
                "biblioPrice -- �ּ۸�",
                "refID -- �ο�ID"
            };


            this.MainForm.AppInfo.LinkFormState(dlg, "accountbook_printoption_html_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            option.SaveData(this.MainForm.AppInfo,
                strNamePath);
        }

        // ���ô�ӡѡ�� ���ı��ļ�
        private void button_print_optionText_Click(object sender, EventArgs e)
        {
            // ���ñ���ͷ��
            string strNamePath = "accountbook_printoption_text";

            PrintOption option = new AccountBookPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);

            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.Text = this.comboBox_load_type.Text + " ���ı� �������";
            dlg.DataDir = this.MainForm.DataDir;    // ��������ģ��ҳ
            dlg.PrintOption = option;
            dlg.ColumnItems = new string[] {
                "no -- ���",
                "barcode -- �������",
                "summary -- ժҪ",

                // 2009/7/24 new add
                "isbnIssn -- ISBN/ISSN",
                "publishTime -- ����ʱ��",
                "volume -- ����",
                "orderClass -- �������",
                "catalogNo -- ��Ŀ��",
                "orderTime -- ����ʱ��",
                "orderID -- ������",
                "seller -- ����",
                "source -- ������Դ",
                "orderPrice -- ������",
                "acceptPrice -- ���ռ�",

                "accessNo -- ��ȡ��",
                "state -- ״̬",
                "location -- �ݲصص�",
                "price -- ��۸�",
                "bookType -- ������",
                "registerNo -- ��¼��",
                "comment -- ע��",
                "mergeComment -- �ϲ�ע��",
                "batchNo -- ���κ�",
                /*
                "borrower -- ������",
                "borrowDate -- ��������",
                "borrowPeriod -- ��������",
                 * */
                "recpath -- ���¼·��",
                "biblioRecpath -- �ּ�¼·��",
                "biblioPrice -- �ּ۸�",
                "refID -- �ο�ID"
            };


            this.MainForm.AppInfo.LinkFormState(dlg, "accountbook_printoption_text_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            option.SaveData(this.MainForm.AppInfo,
                strNamePath);
        }

        private void listView_in_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView_in.SelectedIndices.Count == 0)
                this.MainForm.StatusBarMessage = "δѡ����";
            else if (this.listView_in.SelectedIndices.Count == 1)
                this.MainForm.StatusBarMessage = "�к� " + (this.listView_in.SelectedIndices[0] + 1).ToString();
            else
            {
                this.MainForm.StatusBarMessage = "���к� " + (this.listView_in.SelectedIndices[0] + 1).ToString() + " ��ѡ���� " + this.listView_in.SelectedIndices.Count.ToString() + " ������";
            }
        }


        // �����WordXML�ļ�
        private void button_print_outputWordXmlFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ�����WordML�ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = true;
            dlg.FileName = this.ExportWordXmlFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "WordML�ļ� (*.xml)|*.xml|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.ExportWordXmlFilename = dlg.FileName;

            OutputAcountBookTextFileDialog option_dialog = new OutputAcountBookTextFileDialog();
            MainForm.SetControlFont(option_dialog, this.Font, false);

            option_dialog.Text = "�Ʋ��ʲ������WordML�ļ�";
            option_dialog.MessageText = "��ָ���������";
            option_dialog.Truncate = this.WordXmlTruncate;
            option_dialog.OutputStatisPart = this.WordXmlOutputStatisPart;
            option_dialog.StartPosition = FormStartPosition.CenterScreen;
            option_dialog.ShowDialog(this);

            if (option_dialog.DialogResult != DialogResult.OK)
                return;

            EnableControls(false);

            try
            {

                this.WordXmlTruncate = option_dialog.Truncate;
                this.WordXmlOutputStatisPart = option_dialog.OutputStatisPart;


                XmlTextWriter writer = null;

                try
                {
                    writer = new XmlTextWriter(this.ExportWordXmlFilename, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    strError = "�����ļ� '" + ExportWordXmlFilename + "' ʱ��������: " + ex.Message;
                    goto ERROR1;
                }

                writer.Formatting = Formatting.Indented;
                writer.Indentation = 4;

                try
                {
                    Cursor oldCursor = this.Cursor;
                    this.Cursor = Cursors.WaitCursor;

                    int nCount = 0;
                    List<ListViewItem> items = new List<ListViewItem>();
                    for (int i = 0; i < this.listView_in.Items.Count; i++)
                    {
                        ListViewItem item = this.listView_in.Items[i];

                        items.Add(item);
                        nCount++;
                    }


                    // �����Word XML�ļ�
                    int nRet = OutputToWordXmlFile(
                        items,
                        writer,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    this.Cursor = oldCursor;

                    this.MainForm.StatusBarMessage = "�Ʋ��ʲ����� " + nCount.ToString() + "�� �ѳɹ��������ļ� " + this.ExportWordXmlFilename;
                }
                finally
                {
                    if (writer != null)
                    {
                        writer.Close();
                        writer = null;
                    }
                }
            }
            finally
            {
                EnableControls(true);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void AccountBookForm_Activated(object sender, EventArgs e)
        {
            // 2009/8/13 new add
            this.MainForm.stopManager.Active(this.stop);
        }

        string m_strUsedScriptFilename = "";
        private void button_print_runScript_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ�� C# �ű��ļ�";
            dlg.FileName = this.m_strUsedScriptFilename;
            dlg.Filter = "C# �ű��ļ� (*.cs)|*.cs|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.m_strUsedScriptFilename = dlg.FileName;

            AccountBookHost host = null;
            Assembly assembly = null;

            nRet = PrepareScript(this.m_strUsedScriptFilename,
                out assembly,
                out host,
                out strError);
            if (nRet == -1)
                goto ERROR1;



            this.MainForm.OperHistory.AppendHtml("<div class='debug begin'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ��ʼִ�нű� " + dlg.FileName + "</div>");

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("������������ C# �ű� ...");
            stop.BeginLoop();

            this.EnableControls(false);

            this.listView_in.Enabled = false;
            try
            {
                {
                    host.AccountBookForm = this;

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
                    stop.SetProgressRange(0, this.listView_in.Items.Count);

                {
                    host.AccountBookForm = this;

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

                int i = 0; 
                foreach (ListViewItem item in this.listView_in.Items)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null
                        && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    stop.SetProgressValue(i);


                    this.MainForm.OperHistory.AppendHtml("<div class='debug recpath'>" + HttpUtility.HtmlEncode((i + 1).ToString()) + "</div>");

                    host.AccountBookForm = this;
                    host.ListViewItem = item;

                    StatisEventArgs args = new StatisEventArgs();
                    host.OnRecord(this, args);
                    if (args.Continue == ContinueType.SkipAll)
                        break;
                    if (args.Continue == ContinueType.Error)
                    {
                        strError = args.ParamString;
                        goto ERROR1;
                    }

                    i++;
                }

                {
                    host.AccountBookForm = this;
                    host.ListViewItem = null;

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

                this.listView_in.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControls(true);

                this.MainForm.OperHistory.AppendHtml("<div class='debug end'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ����ִ�нű� " + dlg.FileName + "</div>");
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// �������ؼ��� ListView ���Ͷ���
        /// </summary>
        public ListView ListView
        {
            get
            {
                return this.listView_in;
            }
        }

        // ׼���ű�����
        int PrepareScript(string strCsFileName,
            out Assembly assembly,
            out AccountBookHost host,
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
                "dp2Circulation.AccountBookHost");
            if (entryClassType == null)
            {
                strError = strCsFileName + " ��û���ҵ� dp2Circulation.MarcQueryHost ������";
                goto ERROR1;
            }

            // newһ��Host��������
            host = (AccountBookHost)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            return 0;
        ERROR1:
            return -1;
        }

        private void button_print_createNewScriptFile_Click(object sender, EventArgs e)
        {
            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ������ C# �ű��ļ���";
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
                AccountBookHost.CreateStartCsFile(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return;
            }

            System.Diagnostics.Process.Start("notepad.exe", dlg.FileName);

            this.m_strUsedScriptFilename = dlg.FileName;
        }

        private void button_print_outputExcelFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.listView_in.Items.Count == 0)
            {
                strError = "û�п��������";
                goto ERROR1;
            }

            XLWorkbook doc = null;

            // ѯ���ļ���
            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ����� Excel �ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = true;
            dlg.FileName = this.ExportExcelFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "Excel �ļ� (*.xlsx)|*.xlsx|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.ExportExcelFilename = dlg.FileName;

#if NO
            try
            {
                doc = ExcelDocument.Create(this.ExportExcelFilename);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                goto ERROR1;
            }

            doc.Stylesheet = GenerateStyleSheet();
#endif
            try
            {
                doc = new XLWorkbook(XLEventTracking.Disabled);
                File.Delete(this.ExportExcelFilename);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                goto ERROR1;
            }
            OutputAcountBookTextFileDialog option_dialog = new OutputAcountBookTextFileDialog();
            MainForm.SetControlFont(option_dialog, this.Font, false);

            option_dialog.Truncate = this.TextTruncate;
            option_dialog.OutputStatisPart = this.TextOutputStatisPart;
            option_dialog.StartPosition = FormStartPosition.CenterScreen;
            option_dialog.ShowDialog(this);

            if (option_dialog.DialogResult != DialogResult.OK)
                return;

            this.TextTruncate = option_dialog.Truncate;
            this.TextOutputStatisPart = option_dialog.OutputStatisPart;

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڹ���Ʋ��� ...");
            stop.BeginLoop();

            try
            {
                int nCount = 0;
                List<ListViewItem> items = new List<ListViewItem>();
                for (int i = 0; i < this.listView_in.Items.Count; i++)
                {
                    ListViewItem item = this.listView_in.Items[i];

                    items.Add(item);
                    nCount++;
                }

                // ������ı��ļ�
                int nRet = OutputToTextFile(
                    items,
                    null,
                    ref doc,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (doc != null)
                {
                    // Close the document.
                    // doc.Close();
                    doc.SaveAs(this.ExportExcelFilename);
                }

                this.MainForm.StatusBarMessage = "�Ʋ��ʲ����� " + nCount.ToString() + "�� �ѳɹ�������ļ� " + this.ExportExcelFilename;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                EnableControls(true);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

#if NO
        // parameters:
        //      nIndex  ������±ꡣҲ���Բ�ʹ�ã��� 0 ����
        public int DoMarcFilter(ListViewItem item,
            int nIndex,
            out string strError)
        {
            strError = "";

            if (this.MarcFilter == null)
            {
                strError = "��δ��ʼ�� MARC ����������ʹ�� PrepareMarcFilter() ������ʼ��";
                return -1;
            }


            string strMARC = "";
            string strOutMarcSyntax = "";
            int nRet = 0;

            // TODO: �д���Ҫ���Ա����������������ڴ�ӡ������ŷ��֣�������

            // ���MARC��ʽ��Ŀ��¼
            string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);
            nRet = GetMarc(strBiblioRecPath,
                out strMARC,
                out strOutMarcSyntax,
                out strError);
            if (nRet == -1)
                return -1;

            this.ColumnTable.Clear();   // �����һ��¼����ʱ���������

            // ����filter�е�Record��ض���
            nRet = this.MarcFilter.DoRecord(
                null,
                strMARC,
                strOutMarcSyntax,
                nIndex,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

#endif
    }

    // �������ض�ȱʡֵ��PrintOption������
    class AccountBookPrintOption : PrintOption
    {
        string PublicationType = "ͼ��"; // ͼ�� ����������

        public AccountBookPrintOption(string strDataDir,
            string strPublicationType)
        {
            this.DataDir = strDataDir;
            this.PublicationType = strPublicationType;

            this.PageHeaderDefault = "%date% �Ʋ��ʲ� -- ��Դ %sourcedescription% -- (�� %pagecount% ҳ)";
            this.PageFooterDefault = "%pageno%/%pagecount%";

            this.TableTitleDefault = "%date% �Ʋ��ʲ�";

            this.LinesPerPageDefault = 20;

            // 2008/9/5 new add
            // Columnsȱʡֵ
            Columns.Clear();

            // "no -- ���",
            Column column = new Column();
            column.Name = "no -- ���";
            column.Caption = "���";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "barcode -- �������"
            column = new Column();
            column.Name = "barcode -- �������";
            column.Caption = "�������";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "accessNo -- ��ȡ��"
            column = new Column();
            column.Name = "accessNo -- ��ȡ��";
            column.Caption = "��ȡ��";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "summary -- ժҪ"
            column = new Column();
            column.Name = "summary -- ժҪ";
            column.Caption = "ժҪ";
            column.MaxChars = 15;
            this.Columns.Add(column);

            // "location -- �ݲصص�"
            column = new Column();
            column.Name = "location -- �ݲصص�";
            column.Caption = "�ݲصص�";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "price -- ��۸�"
            column = new Column();
            column.Name = "price -- ��۸�";
            column.Caption = "��۸�";
            column.MaxChars = -1;
            this.Columns.Add(column);

            /* ȱʡʱ��Ҫ���������Ŀ��
            // "biblioPrice -- �ּ۸�"
            column = new Column();
            column.Name = "biblioPrice -- �ּ۸�";
            column.Caption = "�ּ۸�";
            column.MaxChars = -1;
            this.Columns.Add(column);
             * */

            // "biblioRecpath -- �ּ�¼·��"
            column = new Column();
            column.Name = "biblioRecpath -- �ּ�¼·��";
            column.Caption = "�ּ�¼·��";
            column.MaxChars = -1;
            this.Columns.Add(column);
        }

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
    }
}