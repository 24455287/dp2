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

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.CirculationClient;

using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Marc;

using DigitalPlatform.CirculationClient.localhost;
using System.Threading;

namespace dp2Circulation
{
    /// <summary>
    /// ����ƽ���
    /// </summary>
    public partial class ItemHandoverForm : BatchPrintFormBase
    {
        FillThread _fillThread = null;

        // Դ��Ŀ����Ŀ��¼�Ķ��ձ��Ѿ������˸������ʺʹ���ģ���������
        Hashtable m_biblioRecPathTable = new Hashtable();

        // Դ��Ŀ���Ŀ����Ŀ��Ķ��ձ�
        Hashtable m_targetDbNameTable = new Hashtable();

        string SourceStyle = "";    // "batchno" "barcodefile"

        string BatchNo = "";    // �����������κ�
        string LocationString = ""; // �������Ĺݲصص�

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

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
        /// <summary>
        /// ����ͼ���±�: ��ͨ��У��
        /// </summary>
        public const int TYPE_VERIFIED = 2;

        /// <summary>
        /// ���ʹ�ù��������ļ�ȫ·��
        /// </summary>
        public string BarcodeFilePath = "";
        /// <summary>
        /// ���ʹ�ù��ļ�¼·���ļ�ȫ·��
        /// </summary>
        public string RecPathFilePath = "";

        // int m_nGreenItemCount = 0;

        // ����������к�����
        SortColumns SortColumns_in = new SortColumns();
        SortColumns SortColumns_outof = new SortColumns();

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
        /// �к�: �ݲصص�
        /// </summary>
        public static int COLUMN_LOCATION = 4;   // �ݲصص�
        /// <summary>
        /// �к�: �۸�
        /// </summary>
        public static int COLUMN_PRICE = 5;      // �۸�
        /// <summary>
        /// �к�: ������
        /// </summary>
        public static int COLUMN_BOOKTYPE = 6;   // ������
        /// <summary>
        /// �к�: ��¼��
        /// </summary>
        public static int COLUMN_REGISTERNO = 7; // ��¼��
        /// <summary>
        /// �к�: ע��
        /// </summary>
        public static int COLUMN_COMMENT = 8;    // ע��
        /// <summary>
        /// �к�: �ϲ�ע��
        /// </summary>
        public static int COLUMN_MERGECOMMENT = 9;   // �ϲ�ע��
        /// <summary>
        /// �к�: ���κ�
        /// </summary>
        public static int COLUMN_BATCHNO = 10;    // ���κ�
        /// <summary>
        /// �к�: ������
        /// </summary>
        public static int COLUMN_BORROWER = 11;  // ������
        /// <summary>
        /// �к�: ��������
        /// </summary>
        public static int COLUMN_BORROWDATE = 12;    // ��������
        /// <summary>
        /// �к�: ��������
        /// </summary>
        public static int COLUMN_BORROWPERIOD = 13;  // ��������
        /// <summary>
        /// �к�: ���¼·��
        /// </summary>
        public static int COLUMN_RECPATH = 14;   // ���¼·��
        /// <summary>
        /// �к�: �ּ�¼·��
        /// </summary>
        public static int COLUMN_BIBLIORECPATH = 15; // �ּ�¼·��
        /// <summary>
        /// �к�: ��ȡ��
        /// </summary>
        public static int COLUMN_ACCESSNO = 16; // ��ȡ��
        /// <summary>
        /// �к�: Ŀ���¼·��
        /// </summary>
        public static int COLUMN_TARGETRECPATH = 17; // Ŀ���¼·��

        #endregion

        const int WM_LOADSIZE = API.WM_USER + 201;

        /*
        // ��ʾtabҳ���뵽�Ѿ����Լ�����һ����״̬
        bool PageLoadOK 
        {
            get
            {
                // ֻҪlist��������ͱ���װ��������
                if (this.listView_in.Items.Count > 0)
                    return true;
                return false;
            }
        }

        bool PageVerifyOK 
        {
            get
            {
                string strError = "";
                return ReportVerifyState(out strError);
            }
        }

        // bool PagePrintOK = false;
         * */
        /// <summary>
        /// ���캯��
        /// </summary>
        public ItemHandoverForm()
        {
            InitializeComponent();
        }


        private void ItemHandoverForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
            CreateColumnHeader(this.listView_in);

            CreateColumnHeader(this.listView_outof);

#if NO
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            // 2009/2/2
            this.comboBox_load_type.Text = this.MainForm.AppInfo.GetString(
                "itemhandoverform",
                "publication_type",
                "ͼ��");


            this.BarcodeFilePath = this.MainForm.AppInfo.GetString(
                "itemhandoverform",
                "barcode_filepath",
                "");

            this.RecPathFilePath = this.MainForm.AppInfo.GetString(
    "itemhandoverform",
    "recpath_filepath",
    "");

            this.BatchNo = this.MainForm.AppInfo.GetString(
                "itemhandoverform",
                "batchno",
                "");

            this.LocationString = this.MainForm.AppInfo.GetString(
                "itemhandoverform",
                "location_string",
                "");

            this.checkBox_verify_autoUppercaseBarcode.Checked = 
                this.MainForm.AppInfo.GetBoolean(
                "itemhandoverform",
                "auto_uppercase_barcode",
                true);

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);
        }

        private void ItemHandoverForm_FormClosing(object sender, 
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

            if (this.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�������в���Ϣ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
                    this.FormCaption,
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

        private void ItemHandoverForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif
            if (this._scanBarcodeForm != null)
                this._scanBarcodeForm.Close();

            // 2009/2/2
            this.MainForm.AppInfo.SetString(
                "itemhandoverform",
                "publication_type",
                this.comboBox_load_type.Text);


            this.MainForm.AppInfo.SetString(
                "itemhandoverform",
                "barcode_filepath",
                this.BarcodeFilePath);

            this.MainForm.AppInfo.SetString(
                "itemhandoverform",
                "recpath_filepath",
                this.RecPathFilePath);

            this.MainForm.AppInfo.SetString(
                "itemhandoverform",
                "batchno",
                this.BatchNo);

            this.MainForm.AppInfo.SetString(
                "itemhandoverform",
                "location_string",
                this.LocationString);

            this.MainForm.AppInfo.SetBoolean(
                "itemhandoverform",
                "auto_uppercase_barcode",
                this.checkBox_verify_autoUppercaseBarcode.Checked);

            SaveSize();
        }

        /*public*/ void LoadSize()
        {
#if NO
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state");
#endif

            string strWidths = this.MainForm.AppInfo.GetString(
                "itemhandoverform",
                "list_in_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_in,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
    "itemhandoverform",
    "list_outof_width",
    "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_outof,
                    strWidths,
                    true);
            }

            this.MainForm.LoadSplitterPos(
this.splitContainer_main,
"itemhandoverform",
"splitContainer_main_ratio");

            this.MainForm.LoadSplitterPos(
this.splitContainer_inAndOutof,
"itemhandoverform",
"splitContainer_inandoutof_ratio");
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

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_in);
            this.MainForm.AppInfo.SetString(
                "itemhandoverform",
                "list_in_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.listView_outof);
            this.MainForm.AppInfo.SetString(
                "itemhandoverform",
                "list_outof_width",
                strWidths);

            this.MainForm.SaveSplitterPos(
this.splitContainer_main,
"itemhandoverform",
"splitContainer_main_ratio");

            this.MainForm.SaveSplitterPos(
this.splitContainer_inAndOutof,
"itemhandoverform",
"splitContainer_inandoutof_ratio");
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
            this.comboBox_load_type.Enabled = this.ScanMode == true ? false : bEnable;
            this.button_load_loadFromBatchNo.Enabled = this.ScanMode == true ? false : bEnable;
            this.button_load_loadFromBarcodeFile.Enabled = this.ScanMode == true ? false : bEnable;
            this.button_load_loadFromRecPathFile.Enabled = this.ScanMode == true ? false : bEnable;
            this.button_load_scanBarcode.Enabled = this.ScanMode == true ? false : bEnable;

            // next button
            if (bEnable == true)
                SetNextButtonEnable();
            else
                this.button_next.Enabled = false;

            // verify page
            this.textBox_verify_itemBarcode.Enabled = bEnable;
            this.button_verify_load.Enabled = bEnable;
            this.checkBox_verify_autoUppercaseBarcode.Enabled = bEnable;


            // print page
            this.button_print_option.Enabled = bEnable;
            this.button_print_printCheckedList.Enabled = bEnable;

            this.button_print_printNormalList.Enabled = bEnable;

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

            this.listView_outof.Items.Clear();
            this.SortColumns_outof.Clear();
            SortColumns.ClearColumnSortDisplay(this.listView_outof.Columns);
        }


        // ����������ļ�װ��
        private void button_load_loadFromBarcodeFile_Click(object sender, EventArgs e)
        {
            int nRet = 0;
            string strError = "";

            bool bClearBefore = true;
            if (Control.ModifierKeys == Keys.Control)
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
                    true,
                    new string[] { "summary", "@isbnissn", "targetrecpath" },
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
            this.Text = "����ƽ� " + Path.GetFileName(this.BarcodeFilePath);

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
            this.Text = "����ƽ�";
            MessageBox.Show(this, strError);
        }


#if NO
        // ����������ļ�װ��
        private void button_load_loadFromFile_Click(object sender, EventArgs e)
        {
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

                        this.listView_in.Items.Clear();
                        this.SortColumns_in.Clear();
                        SortColumns.ClearColumnSortDisplay(this.listView_in.Columns);

                        this.listView_outof.Items.Clear();
                        this.SortColumns_outof.Clear();
                        SortColumns.ClearColumnSortDisplay(this.listView_outof.Columns);
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


                        // ���ݲ�����ţ�װ����¼
                        // return: 
                        //      -2  ��������Ѿ���list�д�����
                        //      -1  ����
                        //      1   �ɹ�
                        int nRet = LoadOneItem(strLine,
                            this.listView_in,
                            null,
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
            this.BarcodeFilePath = dlg.FileName;
            this.Text = "����ƽ� " + Path.GetFileName(this.BarcodeFilePath);

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
            this.Text = "����ƽ�";
            MessageBox.Show(this, strError);
        }
#endif
        public string LoadType
        {
            get
            {
                return (string)Invoke(new Func<string>(GetLoadType));
            }
        }

        string GetLoadType()
        {
            return this.comboBox_load_type.Text;
        }

        // ����һС����¼��װ��
        internal override int DoLoadRecords(List<string> lines,
            List<ListViewItem> items,
            bool bFillSummaryColumn,
            string[] summary_col_names,
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
                    "id,xml,timestamp", // ע�⣬������ timestamp
                    out searchresults,
                    out strError);
                if (lRet == -1)
                {
                    if (this.InvokeRequired == false)
                    {
                        DialogResult temp_result = MessageBox.Show(this,
        strError + "\r\n\r\n�Ƿ�����?",
        this.FormCaption,
        MessageBoxButtons.RetryCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                        if (temp_result == DialogResult.Retry)
                            goto REDO_GETRECORDS;
                    }
                    return -1;
                }

                records.AddRange(searchresults);

                // ȥ���Ѿ�������һ����
                lines.RemoveRange(0, searchresults.Length);

                if (lines.Count == 0)
                    break;
            }

            // ׼�� DOM ����ĿժҪ��
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

            List<dp2Circulation.AccountBookForm.OrderInfo> orderinfos = new List<dp2Circulation.AccountBookForm.OrderInfo>();

            if (this.InvokeRequired == false)
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
                        this.LoadType,  // this.comboBox_load_type.Text,
                        bFillSummaryColumn,
                        summary_col_names,
                        "@path:" + info.Record.Path,
                        info,
                        this.listView_in,
                        null,
                        out strOutputItemRecPath,
                        ref item,
                        out strError);


#if NO
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
#endif

                }
            }
            finally
            {
                if (this.InvokeRequired == false)
                    this.listView_in.EndUpdate();
            }

#if NO
            // �ӷ�������ö�����¼��·��
            if (orderinfos.Count > 0)
            {
                nRet = LoadOrderInfo(
                    orderinfos,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
#endif

            return 0;
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

            // 2009/10/27
            ColumnHeader columnHeader_targetRecpath = new ColumnHeader();


            list.Columns.Clear();

            list.Columns.AddRange(new ColumnHeader[] {
            columnHeader_barcode,
            columnHeader_errorInfo,
            columnHeader_isbnIssn,
            columnHeader_state,
            columnHeader_location,
            columnHeader_price,
            columnHeader_bookType,
            columnHeader_registerNo,
            columnHeader_comment,
            columnHeader_mergeComment,
            columnHeader_batchNo,
            columnHeader_borrower,
            columnHeader_borrowDate,
            columnHeader_borrowPeriod,
            columnHeader_recpath,
            columnHeader_biblioRecpath,
            columnHeader_accessno,
            columnHeader_targetRecpath});


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
        }

        #region ��ǰ�Ĵ���

#if NO

        // ���ݲ�����Ż��߼�¼·����װ����¼
        // parameters:
        //      strBarcodeOrRecPath ������Ż��߼�¼·�����������ǰ׺Ϊ"@path:"���ʾΪ·��
        //      strMatchLocation    ���ӵĹݲصص�ƥ�����������==null����ʾû�������������(ע�⣬""��null���岻ͬ��""��ʾȷʵҪƥ�����ֵ)
        // return: 
        //      -2  ��������Ѿ���list�д�����(��û�м���listview��)
        //      -1  ����(ע���ʾ��������Ѿ�����listview����)
        //      0   ��Ϊ�ݲصص㲻ƥ�䣬û�м���list��
        //      1   �ɹ�
        public int LoadOneItem(
            string strBarcodeOrRecPath,
            ListView list,
            string strMatchLocation,
            out string strError)
        {
            strError = "";

            // �ж��Ƿ��� @path: ǰ׺�����ں����֧����
            bool bIsRecPath = StringUtil.HasHead(strBarcodeOrRecPath, "@path:"); ;

            string strItemXml = "";
            string strBiblioText = "";

            string strItemRecPath = "";
            string strBiblioRecPath = "";

            byte[] item_timestamp = null;

            long lRet = Channel.GetItemInfo(
                stop,
                strBarcodeOrRecPath,
                "xml",
                out strItemXml,
                out strItemRecPath,
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

                // 2009/10/29
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
            string strTargetRecPath = "";

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
                        strTargetRecPath = ListViewUtil.GetItemText(curitem, COLUMN_TARGETRECPATH);
                    }
                }
            }

            if (strBiblioSummary == "")
            {
                string[] formats = new string[3];
                formats[0] = "summary";
                formats[1] = "@isbnissn";
                formats[2] = "targetrecpath";
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
                    Debug.Assert(results != null && results.Length == 3, "results�������3��Ԫ��");
                    strBiblioSummary = results[0];
                    strISBnISSN = results[1];
                    strTargetRecPath = results[2];
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

                // 2013/3/26
                if (strLocation == null)
                    strLocation = "";

                if (strMatchLocation != strLocation)
                    return 0;
            }

            {
                ListViewItem item = AddToListView(list,
                    dom,
                    strItemRecPath,
                    strBiblioSummary,
                    strISBnISSN,
                    strBiblioRecPath,
                    strTargetRecPath);

                // ����timestamp/xml
                OriginItemData data = (OriginItemData)item.Tag;
                Debug.Assert(data != null, "");
                data.Timestamp = item_timestamp;
                data.Xml = strItemXml;

                if (this.comboBox_load_type.Text == "����������")
                {
                    // ����Ƿ�Ϊ�϶����¼���ߵ����¼������Ϊ�϶���Ա
                    // return:
                    //      0   ���ǡ�ͼ���Ѿ�����ΪTYPE_ERROR
                    //      1   �ǡ�ͼ����δ����
                    int nRet = CheckBindingItem(item);
                    if (nRet == 1)
                    {
                        // ͼ��
                        SetItemColor(item, TYPE_NORMAL);
                    }
                }
                else
                {
                    Debug.Assert(this.comboBox_load_type.Text == "ͼ��", "");
                    // ͼ��
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

                // ������ֹ
        // ���ݲ��¼DOM����ListViewItem����һ�����������
        // ���������Զ��������data.Changed����Ϊfalse
        // parameters:
        //      bSetBarcodeColumn   �Ƿ�Ҫ��������������(��һ��)
        static void SetListViewItemText(XmlDocument dom,
            bool bSetBarcodeColumn,
            string strRecPath,
            string strBiblioSummary,
            string strISBnISSN,
            string strBiblioRecPath,
            string strTargetRecPath,
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
            string strBorrower = DomUtil.GetElementText(dom.DocumentElement,
                "borrower");
            string strBorrowDate = DomUtil.GetElementText(dom.DocumentElement,
                "borrowDate");
            string strAccessNo = DomUtil.GetElementText(dom.DocumentElement,
                "accessNo");

            // 2007/6/20
            strBorrowDate = DateTimeUtil.LocalTime(strBorrowDate, "u");

            string strBorrowPeriod = DomUtil.GetElementText(dom.DocumentElement,
                "borrowPeriod");

            ListViewUtil.ChangeItemText(item, COLUMN_SUMMARY, strBiblioSummary);

            ListViewUtil.ChangeItemText(item, COLUMN_ACCESSNO, strAccessNo);
            ListViewUtil.ChangeItemText(item, COLUMN_ISBNISSN, strISBnISSN);

            ListViewUtil.ChangeItemText(item, COLUMN_STATE, strState);
            ListViewUtil.ChangeItemText(item, COLUMN_LOCATION, strLocation);
            ListViewUtil.ChangeItemText(item, COLUMN_PRICE, strPrice);
            ListViewUtil.ChangeItemText(item, COLUMN_BOOKTYPE, strBookType);
            ListViewUtil.ChangeItemText(item, COLUMN_REGISTERNO, strRegisterNo);
            ListViewUtil.ChangeItemText(item, COLUMN_COMMENT, strComment);
            ListViewUtil.ChangeItemText(item, COLUMN_MERGECOMMENT, strMergeComment);
            ListViewUtil.ChangeItemText(item, COLUMN_BATCHNO, strBatchNo);

            ListViewUtil.ChangeItemText(item, COLUMN_BORROWER, strBorrower);
            ListViewUtil.ChangeItemText(item, COLUMN_BORROWDATE, strBorrowDate);
            ListViewUtil.ChangeItemText(item, COLUMN_BORROWPERIOD, strBorrowPeriod);
            ListViewUtil.ChangeItemText(item, COLUMN_RECPATH, strRecPath);

            ListViewUtil.ChangeItemText(item, COLUMN_BIBLIORECPATH, strBiblioRecPath);

            ListViewUtil.ChangeItemText(item, COLUMN_TARGETRECPATH, strTargetRecPath);

            if (bSetBarcodeColumn == true)
            {
                string strBarcode = DomUtil.GetElementText(dom.DocumentElement,
                    "barcode");
                ListViewUtil.ChangeItemText(item, COLUMN_BARCODE, strBarcode);
            }

            SetItemColor(item, TYPE_NORMAL);

        }

        // ������ֹ
        static ListViewItem AddToListView(ListView list,
            XmlDocument dom,
            string strRecPath,
            string strBiblioSummary,
            string strISBnISSN,
            string strBiblioRecPath,
            string strTargetRecPath)
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
                strTargetRecPath,
                item);
            list.Items.Add(item);
            return item;
        }
#endif

        #endregion // ��ǰ�Ĵ���

        // ����Ƿ�Ϊ�϶����¼���ߵ����¼������Ϊ�϶���Ա
        // return:
        //      0   ���ǡ���Ϊ�϶���Ա��ͼ���Ѿ�����ΪTYPE_ERROR
        //      1   �ǡ���ΪΪ�϶����¼���ߵ����¼��ͼ��δ�޸Ĺ�
        int CheckBindingItem(ListViewItem item)
        {
            string strError = "";
            /*
            string strPublishTime = ListViewUtil.GetItemText(item, COLUMN_PUBLISHTIME);
            if (strPublishTime.IndexOf("-") == -1)
            {
                strError = "���Ǻ϶��ᡣ�������� '" + strPublishTime + "' ���Ƿ�Χ��ʽ";
                goto ERROR1;
            }
             * */

            string strState = ListViewUtil.GetItemText(item, COLUMN_STATE);
            if (StringUtil.IsInList("�϶���Ա", strState) == true)
            {
                strError = "���Ǻ϶���򵥲ᡣ״̬ '" + strState + "' �о���'�϶���Ա'ֵ";
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
                    strError = "���Ǻ϶���򵥲ᡣ<binding>Ԫ���о���<bindingParent>Ԫ��";
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
            // ȷ���̰߳�ȫ 2014/9/3
            if (item.ListView != null && item.ListView.InvokeRequired)
            {
                item.ListView.BeginInvoke(new Action<ListViewItem, int>(SetItemColor), item, nType);
                return;
            }

            item.ImageIndex = nType;    // 2009/11/1

            if (nType == TYPE_ERROR)
            {
                item.BackColor = Color.Red;
                item.ForeColor = Color.White;
                item.ImageIndex = TYPE_ERROR;
            }
            else if (nType == TYPE_VERIFIED)
            {
                item.BackColor = Color.Green;
                item.ForeColor = Color.White;
                item.ImageIndex = TYPE_VERIFIED;
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

        internal override void SetListViewItemText(XmlDocument dom,
            byte [] baTimestamp,
            bool bSetBarcodeColumn,
            string strRecPath,
            string strBiblioRecPath,
            string[] summary_col_names,
            SummaryInfo summary,
            ListViewItem item)
        {
            string strBiblioSummary = "";
            string strISBnISSN = "";
            string strTargetRecPath = "";

            if (summary != null && summary.Values != null)
            {
                if (summary.Values.Length > 0)
                    strBiblioSummary = summary.Values[0];
                if (summary.Values.Length > 1)
                    strISBnISSN = summary.Values[1];
                if (summary.Values.Length > 2)
                    strTargetRecPath = summary.Values[2];
            }

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

            data.Xml = dom.OuterXml;    //
            data.Timestamp = baTimestamp;

            string strState = DomUtil.GetElementText(dom.DocumentElement,
                "state");
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
            string strBorrower = DomUtil.GetElementText(dom.DocumentElement,
                "borrower");
            string strBorrowDate = DomUtil.GetElementText(dom.DocumentElement,
                "borrowDate");
            string strAccessNo = DomUtil.GetElementText(dom.DocumentElement,
                "accessNo");

            // 2007/6/20
            strBorrowDate = DateTimeUtil.LocalTime(strBorrowDate, "u");

            string strBorrowPeriod = DomUtil.GetElementText(dom.DocumentElement,
                "borrowPeriod");

            ListViewUtil.ChangeItemText(item, COLUMN_SUMMARY, strBiblioSummary);

            ListViewUtil.ChangeItemText(item, COLUMN_ACCESSNO, strAccessNo);
            ListViewUtil.ChangeItemText(item, COLUMN_ISBNISSN, strISBnISSN);

            ListViewUtil.ChangeItemText(item, COLUMN_STATE, strState);
            ListViewUtil.ChangeItemText(item, COLUMN_LOCATION, strLocation);
            ListViewUtil.ChangeItemText(item, COLUMN_PRICE, strPrice);
            ListViewUtil.ChangeItemText(item, COLUMN_BOOKTYPE, strBookType);
            ListViewUtil.ChangeItemText(item, COLUMN_REGISTERNO, strRegisterNo);
            ListViewUtil.ChangeItemText(item, COLUMN_COMMENT, strComment);
            ListViewUtil.ChangeItemText(item, COLUMN_MERGECOMMENT, strMergeComment);
            ListViewUtil.ChangeItemText(item, COLUMN_BATCHNO, strBatchNo);

            ListViewUtil.ChangeItemText(item, COLUMN_BORROWER, strBorrower);
            ListViewUtil.ChangeItemText(item, COLUMN_BORROWDATE, strBorrowDate);
            ListViewUtil.ChangeItemText(item, COLUMN_BORROWPERIOD, strBorrowPeriod);
            ListViewUtil.ChangeItemText(item, COLUMN_RECPATH, strRecPath);

            ListViewUtil.ChangeItemText(item, COLUMN_BIBLIORECPATH, strBiblioRecPath);

            ListViewUtil.ChangeItemText(item, COLUMN_TARGETRECPATH, strTargetRecPath);

            bool bBarcodeChanged = false;
            if (bSetBarcodeColumn == true)
            {
                string strBarcode = DomUtil.GetElementText(dom.DocumentElement,
                    "barcode");
                string strOldBarcode = ListViewUtil.GetItemText(item, COLUMN_BARCODE);
                if (strBarcode != strOldBarcode)
                {
                    ListViewUtil.ChangeItemText(item, COLUMN_BARCODE, strBarcode);
                    bBarcodeChanged = true;
                }
            }

            if (item.ImageIndex != TYPE_VERIFIED || bBarcodeChanged == true)   // 2012/4/8 ԭ��������֤��״̬������������������޸Ĺ�����������������ɫ������Ӧ��������ɫ --- ��ҪĿ���Ǳ�����ǰ��֤���״̬
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
            else if (this.tabControl_main.SelectedTab == this.tabPage_verify)
            {

                bool bOK = ReportVerifyState(out strError);

                if (bOK == false)
                {
                    this.button_next.Enabled = false;
                }
                else
                    this.button_next.Enabled = true;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_move)
            {
                int nProcessiingCount = -1;
                if (this.comboBox_load_type.Text == "ͼ��")
                {
                    // ������û������һ��Ŀ��·�����������Թ������ʵ���¼
                    int nNeedMoveCount = 0;
                    // �����ж��ٸ����ӹ��С�״̬����
                    nProcessiingCount = 0;
                    foreach (ListViewItem item in this.listView_in.Items)
                    {
                        string strTargetRecPath = ListViewUtil.GetItemText(item, COLUMN_TARGETRECPATH);
                        if (String.IsNullOrEmpty(strTargetRecPath) == false)
                            nNeedMoveCount++;
                        else
                        {
                            string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);
                            string strBiblioDbName = Global.GetDbName(strBiblioRecPath);
                            if (this.MainForm.IsOrderWorkDb(strBiblioDbName) == true)
                                nNeedMoveCount++;
                        }

                        string strState = ListViewUtil.GetItemText(item, COLUMN_STATE);
                        if (String.IsNullOrEmpty(strState) == false
                            && Global.IncludeStateProcessing(strState) == true)
                            nProcessiingCount++;
                    }

                    if (nNeedMoveCount > 0)
                    {
                        this.button_move_moveAll.Enabled = true;
                        this.button_move_moveAll.Text = "ת�Ƶ�Ŀ��� ["+nNeedMoveCount.ToString()+"] (&M)...";
                    }
                    else
                    {
                        this.button_move_moveAll.Enabled = false;
                        this.button_move_moveAll.Text = "ת�Ƶ�Ŀ���(&M)...";
                    }
                }
                else
                {
                    this.button_move_moveAll.Enabled = false;
                    this.button_move_moveAll.Text = "ת�Ƶ�Ŀ���(&M)...";
                }

                {
                    if (nProcessiingCount == -1)
                    {
                        nProcessiingCount = 0;
                        foreach (ListViewItem item in this.listView_in.Items)
                        {
                            string strState = ListViewUtil.GetItemText(item, COLUMN_STATE);
                            if (String.IsNullOrEmpty(strState) == false
                                && Global.IncludeStateProcessing(strState) == true)
                                nProcessiingCount++;
                        }
                    }

                    if (nProcessiingCount > 0)
                    {
                        this.button_move_changeStateAll.Enabled = true;
                        this.button_move_changeStateAll.Text = "������ӹ��С�״̬ ["+nProcessiingCount.ToString()+"] (&C)...";
                    }
                    else
                    {
                        this.button_move_changeStateAll.Enabled = false;
                        this.button_move_changeStateAll.Text = "������ӹ��С�״̬(&C)...";
                    }
                }

                if (this.listView_in.Items.Count > 0)
                {
                    this.button_move_notifyReader.Enabled = true;
                    this.button_move_notifyReader.Text = "֪ͨ�������� [" + this.listView_in.Items.Count .ToString()+ "] (&N)...";
                }
                else
                {
                    this.button_move_notifyReader.Enabled = false;
                    this.button_move_notifyReader.Text = "֪ͨ��������(&N)...";
                }

                if (this.listView_in.Items.Count > 0)
                {
                    this.button_move_changeLocation.Enabled = true;
                    this.button_move_changeLocation.Text = "�޸Ĺݲص� [" + this.listView_in.Items.Count.ToString() + "] (&L)...";
                }
                else
                {
                    this.button_move_changeLocation.Enabled = false;
                    this.button_move_changeLocation.Text = "�޸Ĺݲص�(&L)...";
                }

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
            // string strError = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_load)
            {
                this.tabControl_main.SelectedTab = this.tabPage_verify;
                this.button_next.Enabled = true;
                this.textBox_verify_itemBarcode.Focus();
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_verify)
            {
                this.tabControl_main.SelectedTab = this.tabPage_move;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_move)
            {
                this.tabControl_main.SelectedTab = this.tabPage_print;
                this.button_next.Enabled = false;
                this.button_print_printCheckedList.Focus();
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
            }
            else
            {
                Debug.Assert(false, "δ֪��tabpage״̬");
            }

            this.SetNextButtonEnable();
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


        // �㱨У����������
        // return:
        //      true    У���Ѿ����
        //      false   У����δ���
        bool ReportVerifyState(out string strError)
        {
            strError = "";

            // ȫ��listview�������ɫ״̬������û���κμ���������, �ű���У���Ѿ����
            int nGreenCount = 0;
            int nRedCount = 0;
            int nWhiteCount = 0;

            for (int i = 0; i < this.listView_in.Items.Count; i++)
            {
                ListViewItem item = this.listView_in.Items[i];

                if (item.ImageIndex == TYPE_VERIFIED)
                    nGreenCount++;
                else if (item.ImageIndex == TYPE_ERROR)
                    nRedCount++;
                else if (item.ImageIndex == TYPE_NORMAL)
                    nWhiteCount++;
            }

            if (nGreenCount == this.listView_in.Items.Count
                && this.listView_outof.Items.Count == 0)
                return true;

            strError = "��֤��δ��ɡ�\r\n\r\n�б�����:\r\n����֤����(��ɫ) "+nGreenCount.ToString()+" ��\r\n��������(��ɫ) " +nRedCount.ToString()+ "��\r\nδ��֤����(��ɫ) " +nWhiteCount.ToString()+ "��\r\n����������(λ���·��б���) " +this.listView_outof.Items.Count+ "��\r\n\r\n(ֻ��ȫ�����Ϊ����֤״̬(��ɫ)���ű�����֤�Ѿ����)";
            return false;
        }

        private void tabControl_main_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.tabControl_main.SelectedTab == this.tabPage_load)
            {
                this.button_next.Enabled = true;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_verify)
            {
                this.button_next.Enabled = true;
                if (this.ScanMode == true)
                    this.ScanMode = false;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_move)
            {
                this.SetNextButtonEnable();
                if (this.ScanMode == true)
                    this.ScanMode = false;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
                this.button_next.Enabled = false;
                if (this.ScanMode == true)
                    this.ScanMode = false;
            }
            else
            {
                Debug.Assert(false, "δ֪��tabpage״̬");
            }
        }

        private void textBox_verify_itemBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_verify_load;
        }

        // ɨ��һ���������
        private void button_verify_load_Click(object sender, EventArgs e)
        {
            string strError = "";
            if (this.textBox_verify_itemBarcode.Text == "")
            {
                strError = "������Ų���Ϊ�ա�";
                goto ERROR1;
            }

            // 2009/11/27
            if (this.checkBox_verify_autoUppercaseBarcode.Checked == true)
            {
                string strUpper = this.textBox_verify_itemBarcode.Text.ToUpper();
                if (this.textBox_verify_itemBarcode.Text != strUpper)
                    this.textBox_verify_itemBarcode.Text = strUpper;
            }

            // TODO: ��֤�������

            // ���Ҽ�����
            ListViewItem item = FindItem(this.listView_in,
                this.textBox_verify_itemBarcode.Text);

            if (item == null)
            {

                EnableControls(false);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("����װ�ز� "
                    +this.textBox_verify_itemBarcode.Text
                    +" ...");
                stop.BeginLoop();

                try
                {
                    // Debug.Assert(false, "");

#if NO
                    // û���ҵ�������out of list
                    int nRet = LoadOneItem(
                        this.textBox_verify_itemBarcode.Text,
                        this.listView_outof,
                        null,
                        out strError);
#endif
                    string strOutputItemRecPath = "";
                    // ���ݲ�����ţ�װ����¼
                    // return: 
                    //      -2  ��������Ѿ���list�д�����
                    //      -1  ����
                    //      1   �ɹ�
                    int nRet = LoadOneItem(
                        this.comboBox_load_type.Text,
                        true,
                        new string[] { "summary", "@isbnissn", "targetrecpath" },
                        this.textBox_verify_itemBarcode.Text,
                        null,   // info,
                        this.listView_outof,
                        null,
                        out strOutputItemRecPath,
                        ref item,
                        out strError);
                }
                finally
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");

                    EnableControls(true);
                }

                // ���¼�������������Ұ
                if (this.listView_outof.Items.Count != 0)
                    this.listView_outof.EnsureVisible(this.listView_outof.Items.Count - 1);

                strError = "����Ϊ " 
                    + this.textBox_verify_itemBarcode.Text
                    + " �Ĳ��¼���ڼ����ڡ��Ѽ��뵽�������б�";
                goto ERROR1;
            }
            else
            {
                // �ҵ����ı�icon
                /*
                item.ImageIndex = TYPE_CHECKED;
                item.BackColor = Color.Green;
                 * */
                if (item.ImageIndex == TYPE_ERROR)
                {
                    strError = "���� "
                        + this.textBox_verify_itemBarcode.Text
                        +" ����Ӧ��������Ȼ�Ѿ������ڼ����ڣ������������޷�ͨ����֤��\r\n\r\n��Ը�����������ݽ����޸ģ�Ȼ��ˢ�����������ɨ�����������֤��";
                    // ѡ��������
                    item.Selected = true;
                    // �����������Ұ
                    this.listView_in.EnsureVisible(this.listView_in.Items.IndexOf(item));
                    goto ERROR1;
                }

                SetItemColor(item, TYPE_VERIFIED);

                // ���±�ɫ���������Ұ
                this.listView_in.EnsureVisible(this.listView_in.Items.IndexOf(item));

                // this.m_nGreenItemCount++;

                SetVerifyPageNextButtonEnabled();
            }

            this.textBox_verify_itemBarcode.SelectAll();
            this.textBox_verify_itemBarcode.Focus();
            return;
        ERROR1:
            MessageBox.Show(this, strError);

            this.textBox_verify_itemBarcode.SelectAll();
            this.textBox_verify_itemBarcode.Focus();
        }

        // ������ɫ�������Ŀ
        int GetGreenItemCount()
        {
            int nCount = 0;
            for (int i = 0; i < this.listView_in.Items.Count; i++)
            {
                if (this.listView_in.Items[i].ImageIndex == TYPE_VERIFIED)
                    nCount++;
            }
            return nCount;
        }

        void SetVerifyPageNextButtonEnabled()
        {
            if (GetGreenItemCount() >= this.listView_in.Items.Count
    && this.listView_outof.Items.Count == 0)
                this.button_next.Enabled = true;
            else
                this.button_next.Enabled = false;
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

            if (nErrorCount != 0
                || nUncheckedCount != 0
                || this.listView_outof.Items.Count != 0)
            {
                MessageBox.Show(this, "���棺�����ӡ�����嵥�������δȫ��������֤���衣\r\n\r\nǩ������ǰ������������֤���衣");
            }


            PrintList("ȫ�������嵥", items);
            return;
            /*
        ERROR1:
            MessageBox.Show(this, strError);
             * */
        }

        // ��ӡ����֤�嵥
        private void button_print_printCheckedList_Click(object sender, EventArgs e)
        {
            // ��顢����
            string strError = "";

            bool bOK = ReportVerifyState(out strError);

            if (bOK == false)
            {
                string strWarning = strError + "\r\n\r\n" 
                    + "�Ƿ���Ҫ��ӡ����֤�Ĳ���(��ɫ��)?";
                DialogResult result = MessageBox.Show(this,
strWarning,
this.FormCaption,
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    strError = "������ӡ��";
                    goto ERROR1;
                }
            }

            List<ListViewItem> items = new List<ListViewItem>();
            for (int i = 0; i < this.listView_in.Items.Count; i++)
            {
                ListViewItem item = this.listView_in.Items[i];

                if (item.ImageIndex == TYPE_VERIFIED)
                    items.Add(item);
            }

            if (items.Count == 0)
            {
                MessageBox.Show(this, "���棺��ǰ������������֤������(��ɫ��)��");
            }

            PrintList("����֤�嵥", items);

            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        void PrintList(
            string strTitle,
            List<ListViewItem> items)
        {
            string strError = "";

            // ����һ��html�ļ�������ʾ��HtmlPrintForm�С�
            EnableControls(false);
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
                EnableControls(true);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ���ô�ӡѡ��
        private void button_print_option_Click(object sender, EventArgs e)
        {
            string strNamePath = "handover_printoption";

            // ���ñ���ͷ��
            PrintOption option = new ItemHandoverPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);


            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.Text = this.comboBox_load_type.Text + " ��ӡ����";
            dlg.DataDir = this.MainForm.DataDir;    // ��������ģ��ҳ
            dlg.PrintOption = option;
            dlg.ColumnItems = new string[] {
                "no -- ���",
                "barcode -- �������",
                "summary -- ժҪ",
                "isbnIssn -- ISBN/ISSN",
                "accessNo -- ��ȡ��",
                "state -- ״̬",
                "location -- �ݲصص�",
                "price -- ��۸�",
                "bookType -- ������",
                "registerNo -- ��¼��",
                "comment -- ע��",
                "mergeComment -- �ϲ�ע��",
                "batchNo -- ���κ�",
                "borrower -- ������",
                "borrowDate -- ��������",
                "borrowPeriod -- ��������",
                "recpath -- ���¼·��",
                "biblioRecpath -- �ּ�¼·��",
                "biblioPrice -- �ּ۸�"
            };


            this.MainForm.AppInfo.LinkFormState(dlg, "handover_printoption_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            option.SaveData(this.MainForm.AppInfo,
                strNamePath);
        }

        // ����htmlҳ��
        // �����ǡ���ӡ����֤�嵥�����ǡ���ӡȫ�������嵥�������ñ�����
        int BuildHtml(
            List<ListViewItem> items,
            out List<string> filenames,
            out string strError)
        {
            strError = "";

            Hashtable macro_table = new Hashtable();

            string strNamePath = "handover_printoption";

            // ��ô�ӡ����
            PrintOption option = new ItemHandoverPrintOption(this.MainForm.DataDir,
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
                // 2008/11/22
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
            macro_table["%barcodefilepath%"] = "";
            macro_table["%barcodefilename%"] = "";
            macro_table["%recpathfilepath%"] = "";
            macro_table["%recpathfilename%"] = "";

            if (this.SourceStyle == "barcodefile")
            {
                macro_table["%barcodefilepath%"] = this.BarcodeFilePath;
                macro_table["%barcodefilename%"] = Path.GetFileName(this.BarcodeFilePath);
            }
            else if (this.SourceStyle == "recpathfile")
            {
                macro_table["%recpathfilepath%"] = this.RecPathFilePath;
                macro_table["%recpathfilename%"] = Path.GetFileName(this.RecPathFilePath);
            }


            filenames = new List<string>();    // ÿҳһ���ļ�������������������ļ���

            string strFileNamePrefix = this.MainForm.DataDir + "\\~itemhandover";

            string strFileName = "";

            // ���ͳ����Ϣҳ
            {
                int nItemCount = items.Count;
                int nBiblioCount = GetBiblioCount(items);
                string strTotalPrice = GetTotalPrice(items).ToString();

                macro_table["%itemcount%"] = nItemCount.ToString();
                macro_table["%bibliocount%"] = nBiblioCount.ToString();
                macro_table["%totalprice%"] = strTotalPrice;

                macro_table["%pageno%"] = "1";

                // 2008/11/23
                macro_table["%datadir%"] = this.MainForm.DataDir;   // ��������datadir��templatesĿ¼�ڵ�ĳЩ�ļ�
                ////macro_table["%libraryserverdir%"] = this.MainForm.LibraryServerDir;  // �������÷������˵�CSS�ļ�
                // 2009/10/10
                macro_table["%cssfilepath%"] = this.GetAutoCssUrl(option, "itemhandover.css");  // �������÷������˻�css��ģ���CSS�ļ�

                strFileName = strFileNamePrefix + "0" + ".html";

                filenames.Add(strFileName);

                string strTemplateFilePath = option.GetTemplatePageFilePath("ͳ��ҳ");
                if (String.IsNullOrEmpty(strTemplateFilePath) == false)
                {
                    /*
<html>
<head>
	���÷�<LINK href='%libraryserverdir%/itemhandover.css' type='text/css' rel='stylesheet'>
	���÷�<LINK href='%cssfilepath%' type='text/css' rel='stylesheet'>
</head>
<body>
	<div class='pageheader'>%date% ���ƽ��嵥 -- ��: %batchno% -- ��: %location% -- (�� %pagecount% ҳ)</div>
	<div class='tabletitle'>%date% ���ƽ��嵥 -- %barcodefilepath%</div>
	<div class='itemcount'>����: %itemcount%</div>
	<div class='bibliocount'>����: %bibliocount%</div>
	<div class='totalprice'>�ܼ�: %totalprice%</div>
	<div class='sepline'><hr/></div>
	<div class='batchno'>���κ�: %batchno%</div>
	<div class='location'>�ݲصص�: %location%</div>
	<div class='sepline'><hr/></div>
	<div class='sender'>�ƽ���: </div>
	<div class='recipient'>������: </div>
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
                    int nRet = Global.ReadTextFileContent(strTemplateFilePath,
                        out strContent,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    string strResult = StringUtil.MacroString(macro_table,
                        strContent);
                    StreamUtil.WriteText(strFileName,
                        strResult);
                }
                else
                {
                    // ȱʡ�Ĺ̶����ݴ�ӡ

                    BuildPageTop(option,
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
                        // 2008/11/22
                        if (String.IsNullOrEmpty(this.BatchNo) == false)
                        {
                            StreamUtil.WriteText(strFileName,
                                "<div class='batchno'>���κ�: " + this.BatchNo + "</div>");
                        }
                        if (String.IsNullOrEmpty(this.LocationString) == false)
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

                    StreamUtil.WriteText(strFileName,
                        "<div class='sepline'><hr/></div>");


                    StreamUtil.WriteText(strFileName,
                        "<div class='sender'>�ƽ���: </div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='recipient'>������: </div>");


                    BuildPageBottom(option,
                        macro_table,
                        strFileName,
                        false);
                }

            }

            // ���ҳѭ��
            for (int i = 0; i < nTablePageCount; i++)
            {
                macro_table["%pageno%"] = (i + 1 + 1).ToString();

                strFileName = strFileNamePrefix + (i + 1).ToString() + ".html";

                filenames.Add(strFileName);

                BuildPageTop(option,
                    macro_table,
                    strFileName,
                    true);
                // ��ѭ��
                for (int j = 0; j < option.LinesPerPage; j++)
                {
                    BuildTableLine(option,
                        items,
                        strFileName, i, j);
                }

                BuildPageBottom(option,
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

        // 2009/10/10
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

            // string strCssUrl = this.MainForm.LibraryServerDir + "/itemhandover.css";
            // 2009/10/10 changed
            string strCssUrl = GetAutoCssUrl(option, "itemhandover.css");

            /*
            // 2009/10/9
            string strCssFilePath = option.GetTemplatePageFilePath("css");  // ��Сд������
            if (String.IsNullOrEmpty(strCssFilePath) == false)
                strCssUrl = strCssFilePath;
            else
                strCssUrl = this.MainForm.LibraryServerDir + "/itemhandover.css";    // ȱʡ��
             * */

            string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";

            StreamUtil.WriteText(strFileName,
                "<html><head>" + strLink + "</head><body>");

           
            // ҳü
            string strPageHeaderText = option.PageHeader;

            if (String.IsNullOrEmpty(strPageHeaderText) == false)
            {
                strPageHeaderText = StringUtil.MacroString(macro_table,
                    strPageHeaderText);

                StreamUtil.WriteText(strFileName,
                    "<div class='pageheader'>" + strPageHeaderText + "</div>");

                /*
                StreamUtil.WriteText(strFileName,
                    "<hr class='pageheader' />");
                 * */
            }

            // ������
            string strTableTitleText = option.TableTitle;

            if (String.IsNullOrEmpty(strTableTitleText) == false)
            {

                strTableTitleText = StringUtil.MacroString(macro_table,
                    strTableTitleText);

                StreamUtil.WriteText(strFileName,
                    "<div class='tabletitle'>" + strTableTitleText + "</div>");
            }

            if (bOutputTable == true)
            {

                // ���ʼ
                StreamUtil.WriteText(strFileName,
                    "<table class='table'>");   //   border='1'

                // ��Ŀ����
                StreamUtil.WriteText(strFileName,
                    "<tr class='column'>");

                for (int i = 0; i < option.Columns.Count; i++)
                {
                    Column column = option.Columns[i];

                    string strCaption = column.Caption;

                    // ���û��caption���壬��Ų��name����
                    if (String.IsNullOrEmpty(strCaption) == true)
                        strCaption = column.Name;

                    string strClass = StringUtil.GetLeft(column.Name);

                    StreamUtil.WriteText(strFileName,
                        "<td class='" + strClass + "'>" + strCaption + "</td>");
                }

                StreamUtil.WriteText(strFileName,
                    "</tr>");

            }

            return 0;
        }

        // �����ּ۸񡣼ٶ�nIndex�����л���(ͬһ�������һ��)
        static decimal ComputeBiblioPrice(List<ListViewItem> items,
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

        int BuildTableLine(PrintOption option,
            List<ListViewItem> items,
            string strFileName,
            int nPage,
            int nLine)
        {
            // ��Ŀ����
            string strLineContent = "";

            bool bBiblioSumLine = false;    // �Ƿ�Ϊ�ֵ����һ��(������)

            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];

                int nIndex = nPage * option.LinesPerPage + nLine;

                if (nIndex >= items.Count)
                    break;

                ListViewItem item = items[nIndex];

                string strContent = GetColumnContent(item,
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

                strLineContent +=
                    "<td class='" + strClass + "'>" + strContent + "</td>";
            }

            if (bBiblioSumLine == false)
            {
                StreamUtil.WriteText(strFileName,
        "<tr class='content'>");
            }
            else
            {
                StreamUtil.WriteText(strFileName,
        "<tr class='content_biblio_sum'>");
            }

            StreamUtil.WriteText(strFileName,
    strLineContent);

            StreamUtil.WriteText(strFileName,
                "</tr>");

            return 0;
        }


        // �����Ŀ����
        static string GetColumnContent(ListViewItem item,
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
                    case "state":
                        return item.SubItems[COLUMN_STATE].Text;
                    case "location":
                    case "�ݲصص�":
                        return ListViewUtil.GetItemText(item, COLUMN_LOCATION);
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
                    case "borrower":
                        return item.SubItems[COLUMN_BORROWER].Text;
                    case "borrowDate":
                        return item.SubItems[COLUMN_BORROWDATE].Text;
                    case "borrowPeriod":
                        return item.SubItems[COLUMN_BORROWPERIOD].Text;
                    case "recpath":
                        return item.SubItems[COLUMN_RECPATH].Text;
                    case "biblioRecpath":
                    case "�ּ�¼·��":
                        return item.SubItems[COLUMN_BIBLIORECPATH].Text;
                    case "accessNo":
                    case "�����":
                    case "��ȡ��":
                        // ��ӡǰҪȥ�� {ns} ������� 2014/9/6
                        return StringUtil.GetPlainTextCallNumber(ListViewUtil.GetItemText(item, COLUMN_ACCESSNO));
                    case "biblioPrice":
                    case "�ּ۸�":
                        return "!!!biblioPrice";  // ����ֵ����ʾ�ּ۸�
                    default:
                        return "undefined column";
                }
            }

            catch
            {
                return null;    // ��ʾû�����subitem�±�
            }

        }

        int BuildPageBottom(PrintOption option,
            Hashtable macro_table,
            string strFileName,
            bool bOutputTable)
        {


            if (bOutputTable == true)
            {
                // ������
                StreamUtil.WriteText(strFileName,
                    "</table>");
            }

            // ҳ��
            string strPageFooterText = option.PageFooter;

            if (String.IsNullOrEmpty(strPageFooterText) == false)
            {
                /*
                StreamUtil.WriteText(strFileName,
                    "<hr class='pagefooter' />");
                 * */


                strPageFooterText = StringUtil.MacroString(macro_table,
                    strPageFooterText);

                StreamUtil.WriteText(strFileName,
        "<div class='pagefooter'>" + strPageFooterText + "</div>");
            }


            StreamUtil.WriteText(strFileName, "</body></html>");

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

        private void listView_in_DoubleClick(object sender, EventArgs e)
        {
            LoadToEntityForm(this.listView_in);
        }

        private void listView_outof_DoubleClick(object sender, EventArgs e)
        {
            LoadToEntityForm(this.listView_outof);
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

        // �������κż���װ��
        private void button_load_loadFromBatchNo_Click(object sender, EventArgs e)
        {
            SearchByBatchnoForm dlg = new SearchByBatchnoForm();
            MainForm.SetControlFont(dlg, this.Font, false);

            // 2008/11/30
            dlg.BatchNo = this.BatchNo;
            dlg.ItemLocation = this.LocationString;

            dlg.CfgSectionName = "ItemHandoverForm_SearchByBatchnoForm";
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

            // 2008/11/22
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
            if (Control.ModifierKeys == Keys.Control)
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
                    true,
                    new string[] { "summary", "@isbnissn", "targetrecpath" },
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

            // 2008/11/30
            dlg.BatchNo = this.BatchNo;
            dlg.ItemLocation = this.LocationString;

            dlg.CfgSectionName = "ItemHandoverForm_SearchByBatchnoForm";
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

            // 2008/11/22
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

                this.listView_in.Items.Clear();
                this.SortColumns_in.Clear();
                SortColumns.ClearColumnSortDisplay(this.listView_in.Columns);

                this.listView_outof.Items.Clear();
                this.SortColumns_outof.Clear();
                SortColumns.ClearColumnSortDisplay(this.listView_outof.Columns);
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

                long lRet = 0;
                if (this.BatchNo == "<��ָ��>")
                {
                    // 2013/3/25
                    lRet = Channel.SearchItem(
    stop,
     this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
    "", // dlg.BatchNo,
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
                        strError = "����ȫ�� '" + this.comboBox_load_type.Text + "' ���͵Ĳ��¼û�����м�¼��";
                        goto ERROR1;
                    }
                }
                else
                {

                    lRet = Channel.SearchItem(
                        stop,
                        // 2010/2/25 changed
                         this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
                        // "<all>",
                        dlg.BatchNo,
                        -1,
                        "���κ�",
                        "exact",
                        this.Lang,
                        "batchno",   // strResultSetName
                        "",    // strSearchStyle
                        "", // strOutputStyle
                        out strError);
                }
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
                        DigitalPlatform.CirculationClient.localhost.Record result_item = searchresults[i];

                        string strBarcode = result_item.Cols[0];
                        string strRecPath = result_item.Path;

                        /*
                        // ����������Ϊ�գ������·��װ��
                        // 2009/8/6
                        if (String.IsNullOrEmpty(strBarcode) == true)
                        {
                            strBarcode = "@path:" + strRecPath;
                        }*/

                        // ����
                        strBarcode = "@path:" + strRecPath;


                        // ���ݲ�����Ż��߼�¼·����װ����¼
                        // return: 
                        //      -2  ��������Ѿ���list�д�����
                        //      -1  ����
                        //      0   ��Ϊ�ݲصص㲻ƥ�䣬û�м���list��
                        //      1   �ɹ�
                        int nRet = LoadOneItem(strBarcode,
                            this.listView_in,
                            strMatchLocation,
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

        void ForceSortColumnsIn(int nClickColumn)
        {
            // 2009/7/25 changed
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

        // �Լ������б��������
        private void listView_outof_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;
            this.SortColumns_outof.SetFirstColumn(nClickColumn,
                this.listView_outof.Columns);

            // ����
            this.listView_outof.ListViewItemSorter = new SortColumnsComparer(this.SortColumns_outof);

            this.listView_outof.ListViewItemSorter = null;

            SetGroupBackcolor(
                this.listView_outof,
                nClickColumn);
        }

        // �������б�������Ĳ˵�
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

            menuItem = new MenuItem("ˢ��ѡ������(&S)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_refreshSelected_Click);
            if (this.listView_in.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ˢ��ȫ����(&R)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_refreshAll_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�޸Ĺݲصأ���ѡ���� "
+ this.listView_in.SelectedItems.Count.ToString()
+ " ��(&C)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_changeLocation_Click);
            if (this.listView_in.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�������֤״̬����ѡ���� "
                + this.listView_in.SelectedItems.Count.ToString()
                + " ��(&L)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_clearVerifiedSelected_Click);
            if (this.listView_in.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);



            menuItem = new MenuItem("ת�Ƶ�Ŀ��⣬��ѡ���� "
                + this.listView_in.SelectedItems.Count.ToString()
                + " ��(&M)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_moveToTargetDb_Click);
            if (this.listView_in.SelectedItems.Count == 0
                || this.comboBox_load_type.Text == "����������")
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("������ӹ��С�״̬����ѡ���� "
    + this.listView_in.SelectedItems.Count.ToString()
    + " ��(&C)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_clearProccessingState_Click);
            if (this.listView_in.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("֪ͨ�������ߣ���ѡ���� "
    + this.listView_in.SelectedItems.Count.ToString()
    + " ��(&C)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_notifyReader_Click);
            if (this.listView_in.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�Ƴ�(&D)");
            menuItem.Tag = this.listView_in;
            menuItem.Click += new System.EventHandler(this.menu_deleteSelected_Click);
            if (this.listView_in.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.listView_in, new Point(e.X, e.Y));		
        }

        static List<ListViewItem> GetSelectedItems(ListView list)
        {
            List<ListViewItem> results = new List<ListViewItem>();
            foreach (ListViewItem item in list.SelectedItems)
            {
                results.Add(item);
            }
            return results;
        }

        static List<ListViewItem> GetAllItems(ListView list)
        {
            List<ListViewItem> results = new List<ListViewItem>();
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];
                results.Add(item);
            }
            return results;
        }

        void menu_notifyReader_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_in.SelectedItems.Count == 0)
            {
                strError = "��ǰû��ѡ���κ�����";
                goto ERROR1;
            }

            List<ListViewItem> items = GetSelectedItems(this.listView_in);

            int nNotifiedCount = 0;
            nRet = NotifyReader(
                items,
                out nNotifiedCount,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            MessageBox.Show(this, "�ɹ�֪ͨ��Ŀ��¼ " + nNotifiedCount + " ��");
            return;
        ERROR1:
            this.SetNextButtonEnable();
            MessageBox.Show(this, strError);
        }

        // ��ѡ������ִ�� �޸Ĺݲص�
        void menu_changeLocation_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_in.SelectedItems.Count == 0)
            {
                strError = "��ǰû��ѡ���κ�����";
                goto ERROR1;
            }

            List<ListViewItem> items = GetSelectedItems(this.listView_in);

            // return:
            //      0   �����޸�
            //      1   �������޸�
            nRet = ChangeLocation(items,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
                return;

            this.SetNextButtonEnable();

            if (string.IsNullOrEmpty(strError) == false)
                MessageBox.Show(this, strError);
            return;
        ERROR1:
            this.SetNextButtonEnable();
            MessageBox.Show(this, strError);
        }

        // parameters:
        // return:
        //      0   �����޸�
        //      1   �������޸�
        int ChangeLocation(List<ListViewItem> items,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // �㱨����װ�������
            // return:
            //      0   ��δװ���κ�����    
            //      1   װ���Ѿ����
            //      2   ��Ȼװ�������ݣ����������д�������
            int nState = ReportLoadState(out strError);
            if (nState != 1)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ڲ���ǰ��" + strError + "��\r\n\r\nȷʵҪ����? ",
                    this.FormCaption,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return 0;
            }
            // ���޸Ĺݲصص�ǰ������¼�����
            // return:
            //      0   ��δװ���κ�����    
            //      1   �����з�������
            //      2   �������谭����������
            nState = CheckBeforeChangeLocation(out strError);
            if (nState != 1)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ڲ���ǰ��" + strError + "��\r\n\r\nȷʵҪ����? ",
                    this.FormCaption,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return 0;
                if (nState == 2)
                {
                    result = MessageBox.Show(this,
        "�Ƿ�ҪתΪ����Щ�ڽ�״̬��ͼ��ִ�л������? \r\n\r\n[��] תΪ�������; [��] ���������޸ĹݲصصĲ���\r\n\r\nע�⣺���תΪ���л�����������޸ĹݲصصĲ����ᱻȡ������ȴ����������ɺ�������ִ���޸ĹݲصصĲ���",
        this.FormCaption,
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button2);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        nRet = DoReturn(items,
            out strError);
                        if (nRet == -1)
                            return -1;
                        MessageBox.Show(this.MainForm, "�޸ĹݲصصĲ����ѱ�ȡ������ȴ����������ȫ��ɺ�������ִ���޸ĹݲصصĲ�����\r\n\r\nע: ���������ɺ�����к�ɫ�������Ҫר�Ŵ���");
                        return 0;
                    }
                }
            }

            // �ȼ���ǲ����Ѿ����޸ĵġ����ѣ����������������Щ�޸�Ҳ��һ������
            int nChangedCount = GetChangedCount(items);
            if (nChangedCount > 0)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ڲ���ǰ����ѡ���ķ�Χ���� " + nChangedCount.ToString() + " ������Ϣ���޸ĺ���δ���档��������������Щ�޸Ļᱻһ��������֡�\r\n\r\nȷʵҪ����? ",
                    this.FormCaption,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.No)
                    return 0;
            }

            GetLocationDialog dlg = new GetLocationDialog();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.ItemLocation = ListViewUtil.GetItemText(items[0], COLUMN_LOCATION); // �Ի����г��ֵ�һ�еĹݲص���Ϊ�ο�

            dlg.GetLocationValueTable -= new GetValueTableEventHandler(dlg_GetLocationValueTable);
            dlg.GetLocationValueTable += new GetValueTableEventHandler(dlg_GetLocationValueTable);

            dlg.RefDbName = "";
            /*
                dlg.RefDbName = EntityForm.GetDbName(this.entityEditControl1.RecPath);
             * */
            dlg.MainForm = this.MainForm;

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return 0;

            nChangedCount = ChangeLocation(items, dlg.ItemLocation);

            ListViewUtil.ClearSelection(this.listView_in);  // ���ȫ��ѡ���־

            int nSavedCount = 0;
            // return:
            //      -1  ����
            //      0   �ɹ�
            //      1   �ж�
            nRet = SaveItemRecords(
                items,  // ���淶Χ���ܱȱ���clear���Դ�
                out nSavedCount,
                out strError);
            if (nRet == -1)
                return -1;
            else
            {
                // ˢ��Origin����ǳ���ɫ
                if (this.SortColumns_in.Count > 0)
                {
                    SetGroupBackcolor(
                        this.listView_in,
                        this.SortColumns_in[0].No);
                }

                this.SetNextButtonEnable();

                if (nChangedCount > 0)
                    strError = "���޸� " + nChangedCount.ToString() + " ����� " + nSavedCount.ToString() + " ��";
                else
                    strError = "û�з����޸ĺͱ���";
            }

            return 1;
        }

        // ���޸Ĺݲصص�ǰ������¼�����
        // return:
        //      0   ��δװ���κ�����    
        //      1   �����з�������
        //      2   �������谭����������
        int CheckBeforeChangeLocation(out string strError)
        {
            strError = "";

            int nBorrowCount = 0;
            for (int i = 0; i < this.listView_in.Items.Count; i++)
            {
                ListViewItem item = this.listView_in.Items[i];

                if (item.ImageIndex == TYPE_ERROR)
                    continue;

                string strBorrower = ListViewUtil.GetItemText(item, COLUMN_BORROWER);
                if (string.IsNullOrEmpty(strBorrower) == false)
                    nBorrowCount++;
            }

            if (nBorrowCount != 0)
            {
                strError = "�б����� " + nBorrowCount + " ���ڽ�״̬���С���Щ���޷����޸Ĺݲص��ֶΡ����ڲ���ǰ�ų���Щ�У����߽�����ִ�л�������������װ��";
                return 2;
            }

            strError = "��������װ����ȷ��";
            return 1;
        }

        // ��ѡ������ִ�� ������ӹ��С�״̬
        void menu_clearProccessingState_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_in.SelectedItems.Count == 0)
            {
                strError = "��ǰû��ѡ���κ�����";
                goto ERROR1;
            }

            List<ListViewItem> items = GetSelectedItems(this.listView_in);

            // �ȼ���ǲ����Ѿ����޸ĵġ����ѣ����������������Щ�޸�Ҳ��һ������
            int nChangedCount = GetChangedCount(items);
            if (nChangedCount > 0)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ڲ���ǰ����ѡ���ķ�Χ���� " + nChangedCount.ToString() + " ������Ϣ���޸ĺ���δ���档��������������Щ�޸Ļᱻһ��������֡�\r\n\r\nȷʵҪ����? ",
                    this.FormCaption,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.No)
                    return;
            }

            int nClearCount = ClearProcessingState(items);

            ListViewUtil.ClearSelection(this.listView_in);  // ���ȫ��ѡ���־

            int nSavedCount = 0;
            // return:
            //      -1  ����
            //      0   �ɹ�
            //      1   �ж�
            nRet = SaveItemRecords(
                items,  // ���淶Χ���ܱȱ���clear���Դ�
                out nSavedCount,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }
            else
            {
                // ˢ��Origin����ǳ���ɫ
                if (this.SortColumns_in.Count > 0)
                {
                    SetGroupBackcolor(
                        this.listView_in,
                        this.SortColumns_in[0].No);
                }

                this.SetNextButtonEnable();

                if (nClearCount > 0)
                    MessageBox.Show(this, "���޸� " + nClearCount.ToString() + " ����� " + nSavedCount.ToString() + " ��");
                else
                    MessageBox.Show(this, "û�з����޸ĺͱ���");
            }
            return;
        ERROR1:
            this.SetNextButtonEnable();
            MessageBox.Show(this, strError);
        }

        // ��ѡ������ִ�� �ƶ���Ŀ���
        void menu_moveToTargetDb_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_in.Items.Count == 0)
            {
                strError = "��ǰû���κ�����ɹ�����";
                goto ERROR1;
            }

            if (this.comboBox_load_type.Text == "����������")
            {
                strError = "���ܶ��������������ת�Ƶ�Ŀ���Ĳ���";
                goto ERROR1;
            }

            List<ListViewItem> items = GetSelectedItems(this.listView_in);

            // �ȼ���ǲ����Ѿ����޸ĵġ����ѣ����������������Щ�޸�Ҳ��һ������
            int nChangedCount = GetChangedCount(items);
            if (nChangedCount > 0)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ڲ���ǰ����ѡ���ķ�Χ���� " + nChangedCount.ToString() + " �����Ϣ���޸ĺ���δ���档��������������Щ�޸Ļᱻһ��������֡�\r\n\r\nȷʵҪ����? ",
                    this.FormCaption,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.No)
                    return;
            }

            int nMovedCount = 0;
            // ת��
            // return:
            //      -1  ���������ʱ��nMovedCount���>0����ʾ�Ѿ�ת�Ƶ�������
            //      0   �ɹ�
            //      1   ��;����
            nRet = DoMove(
                items,
                out nMovedCount,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nMovedCount > 0)
                MessageBox.Show(this, "��ת�� " + nMovedCount.ToString() + " ��");
            else
                MessageBox.Show(this, "û�з���ת��");


            // ��������next��������ť��״̬
            this.SetNextButtonEnable();
            return;
        ERROR1:
            this.SetNextButtonEnable();
            MessageBox.Show(this, strError);
        }


        // ��� ����֤ ״̬������ѡ������
        void menu_clearVerifiedSelected_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this.listView_in.SelectedItems)
            {
                if (item.ImageIndex == TYPE_VERIFIED)
                    SetItemColor(item, TYPE_NORMAL);
                // ע���Դ���״̬���У��������״̬
            }
            if (this.tabControl_main.SelectedTab == this.tabPage_verify)
            {
                SetVerifyPageNextButtonEnabled();
            }
        }

        void menu_selectAll_Click(object sender, EventArgs e)
        {
            ListView list = (ListView)((MenuItem)sender).Tag;
            ListViewUtil.SelectAllLines(list);
        }

        void menu_refreshSelected_Click(object sender, EventArgs e)
        {
#if NO
            ListView list = (ListView)((MenuItem)sender).Tag;
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in list.SelectedItems)
            {
                items.Add(item);

            }
            RefreshLines(items);

            if (this.tabControl_main.SelectedTab == this.tabPage_verify)
            {
                SetVerifyPageNextButtonEnabled();
            }
#endif
            ListView list = (ListView)((MenuItem)sender).Tag;
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in list.SelectedItems)
            {
                items.Add(item);
            }
            RefreshLines(COLUMN_RECPATH,
                items,
                true,
                new string[] { "summary", "@isbnissn", "targetrecpath"});
            if (this.tabControl_main.SelectedTab == this.tabPage_verify)
            {
                SetVerifyPageNextButtonEnabled();
            }
        }

        void menu_refreshAll_Click(object sender, EventArgs e)
        {
            ListView list = (ListView)((MenuItem)sender).Tag;

            List<ListViewItem> items = new List<ListViewItem>();
            for (int i = 0; i < list.Items.Count; i++)
            {
                items.Add(list.Items[i]);
            }
            // RefreshLines(items);
            RefreshLines(COLUMN_RECPATH,
                items,
                true,
                new string[] { "summary", "@isbnissn", "targetrecpath" });

            if (this.tabControl_main.SelectedTab == this.tabPage_verify)
            {
                SetVerifyPageNextButtonEnabled();
            }
        }

        // �Ƴ��������б����Ѿ�ѡ������
        void menu_deleteSelected_Click(object sender, EventArgs e)
        {
            ListView list = (ListView)((MenuItem)sender).Tag;


            if (list.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫ�Ƴ������");
                return;
            }

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in list.SelectedItems)
            {
                items.Add(item);
            }

            DialogResult result = MessageBox.Show(this,
"ȷʵҪ�Ƴ�ѡ���� "+items.Count.ToString()+" ������?",
this.FormCaption,
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            DeleteLines(items);

            // if (this.tabControl_main.SelectedTab == this.tabPage_verify)
            {
                SetVerifyPageNextButtonEnabled();
            }
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
        void RefreshLines(List<ListViewItem> items)
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

                    int nRet = RefreshOneItem(item, out strError);
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
            out string strError)
        {
            strError = "";

            string strItemXml = "";
            string strBiblioText = "";

            string strItemRecPath = "";
            string strBiblioRecPath = "";

            byte[] item_timestamp = null;

            // string strBarcode = item.Text;

            // 2007/5/11 new changed
            string strBarcode = "@path:" + item.SubItems[COLUMN_RECPATH].Text;

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
            if (lRet == -1 || lRet == 0)
            {
                ListViewUtil.ChangeItemText(item,
                    COLUMN_ERRORINFO,
                    strError);
                SetItemColor(item, TYPE_ERROR);

                // ����timestamp/xml
                OriginItemData data = (OriginItemData)item.Tag;
                Debug.Assert(data != null, "");
                data.Timestamp = item_timestamp;
                data.Xml = strItemXml;
                data.Changed = false;

                goto ERROR1;
            }

            string strBiblioSummary = "";
            string strISBnISSN = "";
            string strTargetRecPath = "";

            if (strBiblioSummary == "")
            {
                /*
                lRet = Channel.GetBiblioSummary(
                    stop,
                    strBarcode,
                    "",
                    "",
                    out strBiblioRecPath,
                    out strBiblioSummary,
                    out strError);
                if (lRet == -1)
                {
                    strBiblioSummary = "�����ĿժҪʱ��������: " + strError;
                }
                 * */
                /*
                lRet = Channel.GetBiblioSummary(
                    stop,
                    strBarcode,
                    "",
                    "",
                    out strBiblioRecPath,
                    out strBiblioSummary,
                    out strError);
                if (lRet == -1)
                {
                    strBiblioSummary = "�����ĿժҪʱ��������: " + strError;
                }*/
                string[] formats = new string[3];
                formats[0] = "summary";
                formats[1] = "@isbnissn";
                formats[2] = "targetrecpath";
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
                    Debug.Assert(results != null && results.Length == 3, "results�������2��Ԫ��");
                    strBiblioSummary = results[0];
                    strISBnISSN = results[1];
                    strTargetRecPath = results[2];
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


            {
                SetListViewItemText(dom,
                    item_timestamp,
                    true,
                    strItemRecPath,
                    strBiblioSummary,
                    strISBnISSN,
                    strBiblioRecPath,
                    strTargetRecPath,
                    item);
            }

            {

                // ����timestamp/xml
                OriginItemData data = (OriginItemData)item.Tag;
                Debug.Assert(data != null, "");
                data.Timestamp = item_timestamp;
                data.Xml = strItemXml;
                data.Changed = false;

            }

            // ͼ��
            // item.ImageIndex = TYPE_NORMAL;

            // �����TYPE_CHECKED���򱣳ֲ���
            // 2009/11/23 changed
            if (item.ImageIndex == TYPE_ERROR)
                SetItemColor(item, TYPE_NORMAL);

            return 1;
        ERROR1:
            return -1;
        }
#endif

        // out_of �б��ϵ�������popup�˵�
        private void listView_outof_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Tag = this.listView_outof;
            menuItem.Click += new System.EventHandler(this.menu_selectAll_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ˢ��ѡ������(&S)");
            menuItem.Tag = this.listView_outof;
            menuItem.Click += new System.EventHandler(this.menu_refreshSelected_Click);
            if (this.listView_outof.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ˢ��ȫ����(&R)");
            menuItem.Tag = this.listView_outof;
            menuItem.Click += new System.EventHandler(this.menu_refreshAll_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�Ƴ�(&D)");
            menuItem.Tag = this.listView_outof;
            menuItem.Click += new System.EventHandler(this.menu_deleteSelected_Click);
            if (this.listView_outof.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.listView_outof, new Point(e.X, e.Y));		
        }

        private void ItemHandoverForm_Activated(object sender, EventArgs e)
        {
            // 2009/8/13
            this.MainForm.stopManager.Active(this.stop);

        }

        // ת��
        // return:
        //      -1  ���������ʱ��nMovedCount���>0����ʾ�Ѿ�ת�Ƶ�������
        //      0   �ɹ�
        //      1   ��;����
        int DoMove(
            List<ListViewItem> items,
            out int nMovedCount,
            out string strError)
        {
            strError = "";
            int nErrorCount = 0;
            nMovedCount = 0;

            ListViewUtil.ClearSelection(this.listView_in);

            this.m_biblioRecPathTable.Clear();
            this.m_targetDbNameTable.Clear();

            EnableControls(false);
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ���ת�� ...");
            stop.BeginLoop();

            try
            {

                stop.SetProgressRange(0, this.listView_in.Items.Count);
                stop.SetProgressValue(0);

                for (int i = 0; i < items.Count; i++)
                {
                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�1";
                            return 1;
                        }
                    }


                    ListViewItem item = items[i];

                    /*
                    string strTargetRecPath = ListViewUtil.GetItemText(item, COLUMN_TARGETRECPATH);
                    if (String.IsNullOrEmpty(strTargetRecPath) == true)
                        continue;
                     * */

                    string strNewItemRecPath = "";
                    // return:
                    //      -1	����
                    //      0	û�б�Ҫת�ơ�˵��������strError�з���
                    //      1	�ɹ�ת��
                    //      2   canceled
                    int nRet = MoveOneRecord(item,
                        out strNewItemRecPath,
                        out strError);
                    if (nRet == -1)
                    {
                        ListViewUtil.ChangeItemText(item,
                            COLUMN_ERRORINFO,
                            strError);
                        SetItemColor(item, TYPE_ERROR);
                        item.EnsureVisible();
                        nErrorCount++;
                        continue;
                    }

                    if (nRet == 0)
                        continue;

                    if (nRet == 2)
                    {
                        strError = "�û��ж�2";
                        return 1;
                    }

                    if (String.IsNullOrEmpty(strNewItemRecPath) == false)
                        ListViewUtil.ChangeItemText(item,
                            COLUMN_RECPATH,
                            strNewItemRecPath);

                    // ת�ƺ�������������Ŀ��¼Ӧ�ò�������Ŀ��
                    /*
                    ListViewUtil.ChangeItemText(item,
                        COLUMN_TARGETRECPATH,
                        "");
                     * */
                    // ͨ��ˢ�¸������Զ�ʵ�֣����ɿ�
                    // nRet = RefreshOneItem(item, out strError);

                    nMovedCount++;
                    item.Selected = true;

                    stop.SetProgressValue(i+1);
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



            if (nErrorCount > 0)
            {
                strError = "��������з��� " + nErrorCount.ToString() + " ������뿴�б��к�ɫ�������еĴ�����Ϣ��\r\n\r\n���⴦�����Ժ���ע��ʹ�������Ĳ˵�����ˢ������У��Ա�۲��¼������״̬";
                return -1;
            }
            else
            {
                // 2014/8/27
                // �к�ɫ�����е�ʱ�򣬲�ˢ��ȫ���С�
                // TODO: С�����ʱֻˢ�²�������У�
                RefreshLines(COLUMN_RECPATH,
        items,
        true,
        new string[] { "summary", "@isbnissn", "targetrecpath" });
            }

            return 0;
        }

        // ������listview�������ҳ�һ��Դ��Ŀ���Ŀ����Ŀ��Ķ��չ�ϵ
        string SearchExistRelation(string strSourceBiblioDbName)
        {
            for (int i = 0; i < this.listView_in.Items.Count; i++)
            {
                ListViewItem item = this.listView_in.Items[i];
                string strSourceBiblioRecPath = ListViewUtil.GetItemText(item,
                    COLUMN_BIBLIORECPATH);
                if (String.IsNullOrEmpty(strSourceBiblioRecPath) == true)
                    continue;

                string strTempSourceBiblioDbName = Global.GetDbName(strSourceBiblioRecPath);

                if (String.IsNullOrEmpty(strTempSourceBiblioDbName) == true)
                    continue;

                if (strTempSourceBiblioDbName == strSourceBiblioDbName)
                {
                    string strTargetBiblioRecPath = ListViewUtil.GetItemText(item,
                        COLUMN_TARGETRECPATH);
                    if (String.IsNullOrEmpty(strTargetBiblioRecPath) == true)
                        continue;

                    string strTargetBiblioDbName = Global.GetDbName(strTargetBiblioRecPath);

                    if (String.IsNullOrEmpty(strTargetBiblioDbName) == true)
                        continue;

                    return strTargetBiblioDbName;
                }
            }

            return null;    // not found
        }

        string GetTargetBiblioDbName(string strSourceBiblioDbName)
        {
            string strTargetBiblioDbName = (string)this.m_targetDbNameTable[strSourceBiblioDbName];
            if (String.IsNullOrEmpty(strTargetBiblioDbName) == false)
                return strTargetBiblioDbName;

            SelectTargetBiblioDbNameDialog dlg = new SelectTargetBiblioDbNameDialog();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.MessageText = "��ָ��������Ŀ�� '" + strSourceBiblioDbName + "' �еļ�¼Ҫת��ȥ��Ŀ����Ŀ��";
            dlg.SourceBiblioDbName = strSourceBiblioDbName;
            dlg.MainForm = this.MainForm;
            dlg.TargetBiblioDbName = SearchExistRelation(strSourceBiblioDbName);    // �����е����������������չ�ϵ
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return null;    // ��ʾcancel

            this.m_targetDbNameTable[strSourceBiblioDbName] = dlg.TargetBiblioDbName;

            return dlg.TargetBiblioDbName;
        }

        // ������Ŀ��¼����
        // parameters:
        // return:
        //      -1  error
        //      0   canceled
        //      1   �Ѿ�����
        //      2   û�и��ơ���Ϊ������¼������ͬ�������ڶԻ����ϵ��ˡ������ǡ���ť
        //      3   û�и��ơ���Ϊ��ǰ�Ѿ����ƹ��ˣ����ߴ������
        int CopyOneBiblioRecord(string strSourceBiblioRecPath,
            string strTargetBiblioRecPath,
            out string strError)
        {
            strError = "";

            Debug.Assert(String.IsNullOrEmpty(strSourceBiblioRecPath) == false, "strSourceBiblioRecPathֵ����Ϊ��");
            Debug.Assert(String.IsNullOrEmpty(strTargetBiblioRecPath) == false, "");

            object o = m_biblioRecPathTable[strSourceBiblioRecPath + " --> " + strTargetBiblioRecPath];
            if (o != null)
                return 3;

            // �Ȼ��Դ��Ŀ��¼
            string[] formats = new string[1];
            formats[0] = "xml";
            string[] results = null;
            byte[] timestamp = null;

            long lRet = Channel.GetBiblioInfos(
                stop,
                strSourceBiblioRecPath,
                "",
                formats,
                out results,
                out timestamp,
                out strError);
            if (lRet == -1 || lRet == 0)
            {
                if (lRet == 0 && String.IsNullOrEmpty(strError) == true)
                {
                    strError = "��Ŀ��¼ '" + strSourceBiblioRecPath + "' ������";
                    return -1;
                }

                return -1;
            }

            Debug.Assert(results != null && results.Length == 1, "results�������1��Ԫ��");
            string strSourceXml = results[0];   // Դ��Ŀ����

            string strSourceBiblioDbName = Global.GetDbName(strSourceBiblioRecPath);
            if (String.IsNullOrEmpty(strSourceBiblioDbName) == true)
            {
                strError = "��·�� '" + strSourceBiblioRecPath + "' �л�����ݿ���ʱ����";
                return -1;
            }

            // ת��ΪMARC��ʽ
            // TODO: �Ƿ����ֱ����XML�Ͻ����޸�?
            string strSourceSyntax = this.MainForm.GetBiblioSyntax(strSourceBiblioDbName);
            string strOutMarcSyntax = "";
            string strSourceMarc = "";
            // ��XML��ʽת��ΪMARC��ʽ
            // �Զ������ݼ�¼�л��MARC�﷨
            int nRet = MarcUtil.Xml2Marc(strSourceXml,
                true,
                strSourceSyntax,
                out strOutMarcSyntax,
                out strSourceMarc,
                out strError);
            if (nRet == -1)
            {
                strError = "����¼ '"+strTargetBiblioRecPath+"' ��XMLת����MARC��¼ʱ����: " + strError;
                return -1;
            }

            // Ȼ����Ŀ����Ŀ��¼
            formats = new string[1];
            formats[0] = "xml";
            results = null;
            byte[] target_timestamp = null;

            lRet = Channel.GetBiblioInfos(
                stop,
                strTargetBiblioRecPath,
                "",
                formats,
                out results,
                out target_timestamp,
                out strError);
            if (lRet == -1 || lRet == 0)
            {
                if (lRet == 0 && String.IsNullOrEmpty(strError) == true)
                {
                    strError = "��Ŀ��¼ '" + strTargetBiblioRecPath + "' ������";
                    return -1;
                }

                return -1;
            }

            Debug.Assert(results != null && results.Length == 1, "results�������1��Ԫ��");
            string strTargetXml = results[0];   // Ŀ����Ŀ����

            string strTargetBiblioDbName = Global.GetDbName(strTargetBiblioRecPath);
            if (String.IsNullOrEmpty(strTargetBiblioDbName) == true)
            {
                strError = "��·�� '"+strTargetBiblioRecPath+"' �л�����ݿ���ʱ����";
                return -1;
            }


            // ת��ΪMARC��ʽ
            string strTargetSyntax = this.MainForm.GetBiblioSyntax(strTargetBiblioDbName);
            string strTargetMarc = "";

            if (strSourceSyntax.ToLower() != strTargetSyntax.ToLower())
            {
                strError = "Դ��Ŀ��¼ '"+strSourceBiblioRecPath+"' ���ڿ�ĸ�ʽ '"+strSourceSyntax+"' �� Ŀ�����ݼ�¼ '"+strTargetBiblioRecPath+"' ���ڿ�ĸ�ʽ '"+strTargetSyntax+"' ��ͬ";
                return -1;
            }

            // ��XML��ʽת��ΪMARC��ʽ
            // �Զ������ݼ�¼�л��MARC�﷨
            nRet = MarcUtil.Xml2Marc(strTargetXml,
                true,
                strTargetSyntax,
                out strOutMarcSyntax,
                out strTargetMarc,
                out strError);
            if (nRet == -1)
            {
                strError = "����¼ '"+strTargetBiblioRecPath+"' ��XMLת����MARC��¼ʱ����: " + strError;
                return -1;
            }

            // ������MARC��¼ȥ��һЩ��Ҫ�����ֶΣ�����001-005/801/905/998�Ժ���бȽϣ������ǲ���һ��
            // �����һ��������Ҫ�����Ի����ò����߽���������ѡ��
            // return:
            //      -1  ����
            //      0   һ��
            //      1   ��һ��
            nRet = CompareTwoMarc(
                strTargetSyntax,
                strSourceMarc,
                strTargetMarc,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                m_biblioRecPathTable[strSourceBiblioRecPath + " --> " + strTargetBiblioRecPath] = true;
                return 2;
            }

            // ����ԭ����998�ֶ�����
            string strField998 = "";
            string strNextFieldName = "";
            nRet = MarcUtil.GetField(strTargetMarc,
                "998",
                0,
                out strField998,
                out strNextFieldName);


            TwoBiblioDialog dlg = new TwoBiblioDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.Text = "ת����Ŀ��¼";
            dlg.MessageText = "��ĿԴ��¼��Ŀ���¼���ݲ�ͬ��\r\n\r\n�����Ƿ�Ҫ��Դ��¼����Ŀ���¼?";
            dlg.LabelSourceText = "Դ " + strSourceBiblioRecPath;
            dlg.LabelTargetText = "Ŀ�� " + strTargetBiblioRecPath;
            dlg.MarcSource = strSourceMarc;
            dlg.MarcTarget = strTargetMarc;
            dlg.ReadOnlyTarget = true;   // Ŀ��MARC�༭�����ý����޸ġ���ʵҲ�������޸ģ������������߸���һ�㶫��������
            // dlg.StartPosition = FormStartPosition.CenterScreen;

            this.MainForm.AppInfo.LinkFormState(dlg, "TwoBiblioDialog_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult == DialogResult.Cancel)
                return 0;   // ȫ������

            if (dlg.DialogResult == DialogResult.No)
            {
                m_biblioRecPathTable[strSourceBiblioRecPath + " --> " + strTargetBiblioRecPath] = true;
                return 2;
            }

            string strFinalMarc = "";

            if (dlg.EditTarget == false)
                strFinalMarc = dlg.MarcSource;
            else
                strFinalMarc = dlg.MarcTarget;

            // ��ԭԭ��Ŀ���¼�е�998�ֶ����ݣ�
            MarcUtil.ReplaceField(ref strFinalMarc,
                "998",
                0,
                strField998);

            // ת����XML
            XmlDocument domMarc = null;
            nRet = MarcUtil.Marc2Xml(strFinalMarc,
                strSourceSyntax,
                out domMarc,
                out strError);
            if (nRet == -1)
                return -1;

            XmlDocument domTarget = new XmlDocument();
            try
            {
                domTarget.LoadXml(strTargetXml);
            }
            catch (Exception ex)
            {
                strError = "target biblio record XML load to DOM error: " + ex.Message;
                return -1;
            }

            // ��Ҫ��ԭԭ��target��¼�е�<dprms:file>Ԫ�أ�
            // ������ԭ��¼��856�ֶεȣ��ǲ����ߵ�����
            // parameters:
            //      source  �洢��<dprms:file>Ԫ�ص�DOM
            //      marc    �洢��MARC�ṹԪ�ص�DOM
            nRet = MergeTwoXml(
                strSourceSyntax,
                ref domTarget,
                domMarc,
                out strError);
            if (nRet == -1)
                return -1;

            byte[] baNewTimestamp = null;
            string strOutputPath = "";
            lRet = Channel.SetBiblioInfo(
                stop,
                "change",
                strTargetBiblioRecPath,
                "xml",
                domTarget.DocumentElement.OuterXml,
                target_timestamp,
                "",
                out strOutputPath,
                out baNewTimestamp,
                out strError);
            if (lRet == -1)
            {
                strError = "�޸���Ŀ��¼ '" + strTargetBiblioRecPath + "' ʱ����: " + strError;
                return -1;
            }

            m_biblioRecPathTable[strSourceBiblioRecPath + " --> " + strTargetBiblioRecPath] = true;
            return 1;
        }

        // �Ƚ�ǰ��Ҫɾ����UNIMARC�ֶ�
        static string[] deleting_unimarc_fieldnames = new string[] {
                "-01", 
                "001", 
                "005", 
                "801", 
                "905", 
                "998", 
        };

        // �Ƚ�ǰ��Ҫɾ����USMARC�ֶ�
        static string[] deleting_usmarc_fieldnames = new string[] {
                "-01", 
                "001", 
                "005", 
                "801", 
                "905", 
                "998", 
        };

        static void DeleteField(ref string strMARC,
            string strFieldName)
        {
            string strField = "";
            string strNextFieldName = "";

            while (true)
            {
                // return:
                //		-1	����
                //		0	��ָ�����ֶ�û���ҵ�
                //		1	�ҵ����ҵ����ֶη�����strField������
                int nRet = MarcUtil.GetField(strMARC,
                    strFieldName,
                    0,
                    out strField,
                    out strNextFieldName);
                if (nRet == 0 || nRet == -1)
                    break;
                MarcUtil.ReplaceField(ref strMARC,
                    strFieldName,
                    0,
                    null);
            }

        }
        // �Ƚ�����MARC��¼�������Ƿ��б����ԵĲ���
        // ������MARC��¼ȥ��һЩ��Ҫ�����ֶΣ�����001-005/801/905/998�Ժ���бȽϣ������ǲ���һ��
        // return:
        //      -1  ����
        //      0   һ��
        //      1   ��һ��
        static int CompareTwoMarc(
            string strSyntax,
            string strMARC1,
            string strMARC2,
            out string strError)
        {
            strError = "";

            string[] deleting_fieldnames = null;

            if (strSyntax.ToLower() == "unimarc")
            {
                deleting_fieldnames = deleting_unimarc_fieldnames;
            }
            else if (strSyntax.ToLower() == "usmarc")
            {
                deleting_fieldnames = deleting_unimarc_fieldnames;
            }
            else
            {
                strError = "δ֪��MARC��ʽ '" + strSyntax + "'";
                return -1;
            }

            for (int i = 0; i < deleting_fieldnames.Length; i++)
            {
                DeleteField(ref strMARC1,
                    deleting_fieldnames[i]);
                DeleteField(ref strMARC2,
                    deleting_fieldnames[i]);
            }

            string strContent1 = "";
            string strContent2 = "";

            if (strMARC1.Length > 24)
                strContent1 = strMARC1.Substring(24);

            if (strMARC2.Length > 24)
                strContent2 = strMARC2.Substring(24);

            if (String.Compare(strContent1, strContent2) != 0)
                return 1;

            return 0;
        }


        // ת����Ŀ��¼
        // parameters:
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        int MoveOneBiblioRecord(string strSourceBiblioRecPath,
            out string strTargetBiblioRecPath,
            out string strError)
        {
            strError = "";
            strTargetBiblioRecPath = "";

            Debug.Assert(String.IsNullOrEmpty(strSourceBiblioRecPath) == false, "");

            string strSourceBiblioDbName = Global.GetDbName(strSourceBiblioRecPath);

            string strTargetBiblioDbName = GetTargetBiblioDbName(strSourceBiblioDbName);
            if (String.IsNullOrEmpty(strTargetBiblioDbName) == true)
                return 0;   // canceled

            // �Ȼ����Ŀ��¼
            string[] formats = new string[1];
            formats[0] = "xml";
            string[] results = null;
            byte[] timestamp = null;

            Debug.Assert(String.IsNullOrEmpty(strSourceBiblioRecPath) == false, "strSourceBiblioRecPathֵ����Ϊ��");

            long lRet = Channel.GetBiblioInfos(
                stop,
                strSourceBiblioRecPath,
                "",
                formats,
                out results,
                out timestamp,
                out strError);
            if (lRet == -1 || lRet == 0)
            {
                if (lRet == 0 && String.IsNullOrEmpty(strError) == true)
                {
                    strError = "��Ŀ��¼ '" + strSourceBiblioRecPath + "' ������";
                    return -1;
                }

                return -1;
            }

            Debug.Assert(results != null && results.Length == 1, "results�������1��Ԫ��");
            string strSourceXml = results[0];   // Դ��Ŀ����

            // ת��ΪMARC��ʽ
            // TODO: �Ƿ����ֱ����XML�Ͻ����޸�?
            string strSourceSyntax = this.MainForm.GetBiblioSyntax(strSourceBiblioDbName);
            string strOutMarcSyntax = "";
            string strMarc = "";
            // ��XML��ʽת��ΪMARC��ʽ
            // �Զ������ݼ�¼�л��MARC�﷨
            int nRet = MarcUtil.Xml2Marc(strSourceXml,
                true,
                strSourceSyntax,
                out strOutMarcSyntax,
                out strMarc,
                out strError);
            if (nRet == -1)
            {
                strError = "XMLת����MARC��¼ʱ����: " + strError;
                return -1;
            }

            // ����Ŀ����Ŀ��¼
            // ֱ�ӽ�û��998$t��MARC��¼���ڴ���
            // TODO: Ϊ�˱��գ��Ƿ�����ɾ��һ��998$t?
            strTargetBiblioRecPath = strTargetBiblioDbName + "/?";
            string strOutputPath = "";
            byte[] baNewTimestamp = null;
            string strOutputBiblio = "";
            lRet = Channel.CopyBiblioInfo(
                stop,
                "onlycopybiblio",   // ֻ������Ŀ��¼��������������ʵ���¼��
                strSourceBiblioRecPath,
                "xml",
                null,   // strSourceXml,
                timestamp,
                strTargetBiblioRecPath,
                null,   // strSourceXml,
                        "",
                        out strOutputBiblio,
                out strOutputPath,
                out baNewTimestamp,
                out strError);

            /*
            lRet = Channel.SetBiblioInfo(
                stop,
                "new",
                strTargetBiblioRecPath,
                "xml",
                strSourceXml,
                null,
                out strOutputPath,
                out baNewTimestamp,
                out strError);
             * */
            if (lRet == -1)
            {
                strError = "������Ŀ��¼ '" + strTargetBiblioRecPath + "' ʱ����: " + strError;
                return -1;
            }

            strTargetBiblioRecPath = strOutputPath;

            // �޸�Դ��Ŀ��¼
            // �޸�/����998$t
            string strField = "";
            string strNextFieldName = "";
            nRet = MarcUtil.GetField(strMarc,
                "998",
                0,
                out strField,
                out strNextFieldName);
            if (nRet == 0)
                strField = "998  ";

            MarcUtil.ReplaceSubfield(ref strField,
                "t",
                0,
                "t" + strTargetBiblioRecPath);
            MarcUtil.ReplaceField(ref strMarc,
                "998",
                0,
                strField);

            // ת����XML
            XmlDocument domMarc = null;
            nRet = MarcUtil.Marc2Xml(strMarc,
                strSourceSyntax,
                out domMarc,
                out strError);
            if (nRet == -1)
                return -1;

            // �����µ�unimarc:Ԫ��ȫ��ɾ�������Ǳ�������Ԫ��
            XmlDocument domSource = new XmlDocument();
            try
            {
                domSource.LoadXml(strSourceXml);
            }
            catch (Exception ex)
            {
                strError = "source biblio record XML load to DOM error: " + ex.Message;
                return -1;
            }

            nRet = MergeTwoXml(
                strSourceSyntax,
                ref domSource,
                domMarc,
                out strError);
            if (nRet == -1)
                return -1;

            // TODO: �Ժ��������ɾ��Դ��Ŀ��¼?
            lRet = Channel.SetBiblioInfo(
                stop,
                "change",
                strSourceBiblioRecPath,
                "xml",
                domSource.DocumentElement.OuterXml,
                timestamp,
                "",
                out strOutputPath,
                out baNewTimestamp,
                out strError);
            if (lRet == -1)
            {
                strError = "�޸���Ŀ��¼ '" + strSourceBiblioRecPath + "' ʱ����: " + strError;
                return -1;
            }

            return 1;
        }

        // parameters:
        //      source  �洢��<dprms:file>Ԫ�ص�DOM
        //      marc    �洢��MARC�ṹԪ�ص�DOM
        int MergeTwoXml(
            string strMarcSyntax,
            ref XmlDocument source,
            XmlDocument marc,
            out string strError)
        {
            strError = "";

            string strNamespaceURI = "";
            string strPrefix = "";
            if (strMarcSyntax == "unimarc")
            {
                strNamespaceURI = DpNs.unimarcxml;
                strPrefix = "unimarc";
            }
            else if (strMarcSyntax == "usmarc")
            {
                strNamespaceURI = Ns.usmarcxml;
                strPrefix = "usmarc";
            }
            else
            {
                strError = "δ֪��marcsyntax '" + strMarcSyntax + "'";
                return -1;
            }

            // ɾ����Ԫ�����������ֿռ�Ϊunimarc����usmarc�Ľڵ�
            for (int i = 0; i < source.DocumentElement.ChildNodes.Count; i++)
            {
                XmlNode node = source.DocumentElement.ChildNodes[i];
                if (node.NamespaceURI == strNamespaceURI)
                {
                    source.DocumentElement.RemoveChild(node);
                    i--;
                }
            }

            // ����marc�ڵ�
            for (int i = 0; i < marc.DocumentElement.ChildNodes.Count; i++)
            {
                XmlNode source_node = marc.DocumentElement.ChildNodes[i];
                if (source_node.NodeType != XmlNodeType.Element)
                    continue;
                if (source_node.NamespaceURI != strNamespaceURI)
                    continue;

                XmlNode new_node = source.CreateElement(strPrefix, 
                    source_node.LocalName,
                    strNamespaceURI);
                source.DocumentElement.AppendChild(new_node);
                DomUtil.SetElementOuterXml(new_node, source_node.OuterXml);
            }

            

            return 0;
        }

        // �ƶ�һ�����¼
        // ע�⣺���ñ�����ǰ����Ҫ����Χ׼����stop
        // parameters:
        //      strNewItemRecPath   �ƶ����µ�ʵ���¼·��
        // return:
        //      -1	����
        //      0	û�б�Ҫת�ơ�˵��������strError�з���
        //      1	�ɹ�ת��
        //      2   canceled
        int MoveOneRecord(ListViewItem item,
            out string strNewItemRecPath,
            out string strError)
        {
            strError = "";
            strNewItemRecPath = "";
            long lRet = 0;
            int nRet = 0;

            string strItemRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

            string strTargetBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_TARGETRECPATH);

            string strSourceBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);
            string strSourceBiblioDbName = Global.GetDbName(strSourceBiblioRecPath);

            // ���û��Ŀ����Ŀ��¼����ʾ��Ҫ�´���Ŀ����Ŀ��¼
            if (String.IsNullOrEmpty(strTargetBiblioRecPath) == true)
            {
                // ����Դ��Ŀ���ǲ������ڹ�����
                if (this.MainForm.IsOrderWorkDb(strSourceBiblioDbName) == false)
                {
                    strError = "��¼ '" + strItemRecPath + "' �����������Ŀ��¼���߱�Ŀ���¼�����Ҳ��ǲɹ��������ɫ������ת��";
                    return 0;
                }

                // ת��(����)��Ŀ��¼
                // parameters:
                // return:
                //      -1  error
                //      0   canceled
                //      1   succeed
                nRet = MoveOneBiblioRecord(strSourceBiblioRecPath,
                    out strTargetBiblioRecPath,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                    return 2;

                // �޸��б������д����ڴ�Դ��Ŀ��¼���еġ�Ŀ���¼·������
                for (int i = 0; i < this.listView_in.Items.Count; i++)
                {
                    ListViewItem temp_item = this.listView_in.Items[i];
                    string strBiblioRecPath = ListViewUtil.GetItemText(temp_item, COLUMN_BIBLIORECPATH);
                    if (strBiblioRecPath == strSourceBiblioRecPath)
                    {
                        /*
                        ListViewUtil.ChangeItemText(temp_item, 
                            COLUMN_BIBLIORECPATH,
                            strTargetBiblioRecPath);
                         * */
                        ListViewUtil.ChangeItemText(temp_item,
                            COLUMN_TARGETRECPATH,
                            strTargetBiblioRecPath);
                    }
                }

                // ��Ŀ��¼����(ĳ�������ת��)�ɹ���Ŀ����Ŀ��¼·��������
            }
            else
            {
                // �Ѿ���Ŀ����Ŀ��¼��
                // ��Ҫ��Դ��Ŀ��¼��Ŀ����Ŀ��¼�����Ƿ���ͬ��
                // ������ݲ�ͬ������Ҫѯ���Ƿ�Ҫ��Դ��¼���ݸ��Ƶ�Ŀ���¼

                // ������Ŀ��¼����
                // parameters:
                // return:
                //      -1  error
                //      0   canceled
                //      1   �Ѿ�����
                //      2   û�и��ơ���Ϊ������¼������ͬ
                nRet = CopyOneBiblioRecord(strSourceBiblioRecPath,
                    strTargetBiblioRecPath,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    // ���������������
                    return 2;
                }

                if (nRet == 2)
                {
                    // �����Ƿ����˸�����Ŀ��¼�������Ƿ��������������
                }
            }

            /*
            // Ϊת�ƣ����»��һ�β��¼
            string strItemXml = "";
            string strBiblioText = "";

            string strOutputItemRecPath = "";
            string strSourceBiblioRecPath = "";

            byte[] item_timestamp = null;

            lRet = Channel.GetItemInfo(
                stop,
                "@path:" + strItemRecPath,
                "xml",
                out strItemXml,
                out strOutputItemRecPath,
                out item_timestamp,
                "recpath",
                out strBiblioText,
                out strSourceBiblioRecPath,
                out strError);
            if (lRet == -1 || lRet == 0)
            {
                return -1;
            }
             * */

            OriginItemData data = (OriginItemData)item.Tag;

            Debug.Assert(data != null, "");

            string strItemXml = data.Xml;

            Debug.Assert(String.IsNullOrEmpty(strSourceBiblioRecPath) == false, "");

            byte[] item_timestamp = data.Timestamp;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strItemXml);
            }
            catch (Exception ex)
            {
                strError = "���¼XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            string strTargetBiblioDbName = Global.GetDbName(strTargetBiblioRecPath);


            bool bMove = false; // �Ƿ���Ҫ�ƶ����¼
            string strTargetItemDbName = "";
            if (strSourceBiblioDbName != strTargetBiblioDbName)
            {
                // ��Ŀ�ⷢ���˸ı䣬���б�Ҫ�ƶ�����������޸�ʵ���¼��<parent>����
                bMove = true;
                strTargetItemDbName = MainForm.GetItemDbName(strTargetBiblioDbName);

                if (String.IsNullOrEmpty(strTargetItemDbName) == true)
                {
                    strError = "��Ŀ�� '" + strTargetBiblioDbName + "' ��û�д�����ʵ��ⶨ�塣����ʧ��";
                    return -1;
                }
            }

            EntityInfo info = new EntityInfo();
            info.RefID = Guid.NewGuid().ToString();

            string strTargetBiblioRecID = Global.GetRecordID(strTargetBiblioRecPath);

            DomUtil.SetElementText(dom.DocumentElement,
                "parent", strTargetBiblioRecID);

            string strNewItemXml = dom.OuterXml;

            info.OldRecPath = strItemRecPath;
            if (bMove == false)
            {
                info.Action = "change";
                info.NewRecPath = strItemRecPath;
            }
            else
            {
                info.Action = "move";
                Debug.Assert(String.IsNullOrEmpty(strTargetItemDbName) == false, "");
                info.NewRecPath = strTargetItemDbName + "/?";  // ��ʵ���¼�ƶ�����һ��ʵ����У�׷�ӳ�һ���¼�¼�����ɼ�¼�Զ���ɾ��
            }

            info.NewRecord = strNewItemXml;
            info.NewTimestamp = null;

            info.OldRecord = strItemXml;
            info.OldTimestamp = item_timestamp;

            // 
            EntityInfo[] entities = new EntityInfo[1];
            entities[0] = info;

            EntityInfo[] errorinfos = null;

            lRet = Channel.SetEntities(
                stop,
                strTargetBiblioRecPath,
                entities,
                out errorinfos,
                out strError);
            if (lRet == -1)
                return -1;
            
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
                        strNewItemRecPath = error.NewRecPath;
                    }
                }
                if (nErrorCount > 0)
                {
                    return -1;
                }
            }

            return 1;
        }

        private void button_move_moveAll_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_in.Items.Count == 0)
            {
                strError = "��ǰû���κ�����ɹ�����";
                goto ERROR1;
            }
            if (this.comboBox_load_type.Text == "����������")
            {
                strError = "���ܶ��������������ת�Ƶ�Ŀ���Ĳ���";
                goto ERROR1;
            }

            List<ListViewItem> items = GetAllItems(this.listView_in);

            // �ȼ���ǲ����Ѿ����޸ĵġ����ѣ����������������Щ�޸�Ҳ��һ������
            int nChangedCount = GetChangedCount(items);
            if (nChangedCount > 0)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ڲ���ǰ����ǰ�������� " + nChangedCount.ToString() + " �����Ϣ���޸ĺ���δ���档��������������Щ�޸Ļᱻһ��������֡�\r\n\r\nȷʵҪ����? ",
                    this.FormCaption,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.No)
                    return;
            }

            int nMovedCount = 0;
            // ת��
            // return:
            //      -1  ���������ʱ��nMovedCount���>0����ʾ�Ѿ�ת�Ƶ�������
            //      0   �ɹ�
            //      1   ��;����
            nRet = DoMove(
                items,
                out nMovedCount,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nMovedCount > 0)
                MessageBox.Show(this, "��ת�� " + nMovedCount.ToString() + " ��");
            else
                MessageBox.Show(this, "û�з���ת��");

            // ��������next��������ť��״̬
            this.SetNextButtonEnable();
            return;
        ERROR1:
            this.SetNextButtonEnable();
            MessageBox.Show(this, strError);
        }

        // ������ӹ��С�״̬
        private void button_move_changeStateAll_Click(object sender, EventArgs e)
        {
            // ��֯�������� SaveItemRecords
            string strError = "";
            int nRet = 0;

            if (this.listView_in.Items.Count == 0)
            {
                strError = "��ǰû���κ�����ɹ�����";
                goto ERROR1;
            }

            List<ListViewItem> items = GetAllItems(this.listView_in);

            // �ȼ���ǲ����Ѿ����޸ĵġ����ѣ����������������Щ�޸�Ҳ��һ������
            int nChangedCount = GetChangedCount(items);
            if (nChangedCount > 0)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ڲ���ǰ����ǰ�������� " + nChangedCount.ToString() + " �����Ϣ���޸ĺ���δ���档��������������Щ�޸Ļᱻһ��������֡�\r\n\r\nȷʵҪ����? ",
                    this.FormCaption,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.No)
                    return;
            }

            int nClearCount = ClearProcessingState(items);

            ListViewUtil.ClearSelection(this.listView_in);  // ���ȫ��ѡ���־
            
            // TODO: ���������е���Ŀ��¼·����Ȼ��ȥ�ء�����Щ��Ŀ��¼֪ͨ�Ƽ��� 


            int nSavedCount = 0;
            // return:
            //      -1  ����
            //      0   �ɹ�
            //      1   �ж�
            nRet = SaveItemRecords(
                items,  // ���淶Χ���ܱȱ���clear���Դ�
                out nSavedCount,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            else
            {
                // ˢ��Origin����ǳ���ɫ
                if (this.SortColumns_in.Count > 0)
                {
                    SetGroupBackcolor(
                        this.listView_in,
                        this.SortColumns_in[0].No);
                }

                this.SetNextButtonEnable();

                if (nClearCount > 0)
                    MessageBox.Show(this, "���޸� " + nClearCount.ToString() + " ����� " + nSavedCount.ToString() + " ��");
                else
                    MessageBox.Show(this, "û�з����޸ĺͱ���");
            }
            return;
        ERROR1:
            this.SetNextButtonEnable();
            MessageBox.Show(this, strError);
        }

        // ����listview item��changed״̬
        // TODO: �Ƿ���Ҫ����һ��ָʾ��Щ���ֱ��޸ĵ�˵�����ַ���?
        static void SetItemChanged(ListViewItem item,
            bool bChanged)
        {
            OriginItemData data = (OriginItemData)item.Tag;
            if (data == null)
            {
                data = new OriginItemData();
                item.Tag = data;
            }

            data.Changed = bChanged;

            /*
            if (item.ImageIndex == TYPE_ERROR)
            {
                // �����������TYPE_ERROR�����޸���ɫ
            }
            else if (item.ImageIndex == TYPE_NORMAL)
            {
                if (bChanged == true)
                    SetItemColor(item, TYPE_CHANGED);    // TODO: ������޸���ǳ�������ɫ���Ƿ���Ҫ��ˢһ����ɫ?
            }
            if (item.ImageIndex == TYPE_CHANGED)
            {
                if (bChanged == false)
                    SetItemColor(item, TYPE_NORMAL);    // TODO: ������޸���ǳ�������ɫ���Ƿ���Ҫ��ˢһ����ɫ?
            }
             * */
        }

        // return:
        //      �����޸���״̬���������
        int ChangeLocation(List<ListViewItem> items,
            string strNewLocation)
        {
            int nChangedCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strLocation = ListViewUtil.GetItemText(item, COLUMN_LOCATION);
                if (strLocation != strNewLocation)
                {
                    ListViewUtil.ChangeItemText(item, COLUMN_LOCATION, strNewLocation);
                    SetItemChanged(item, true);
                    nChangedCount++;
                }
            }

            return nChangedCount;
        }


        // return:
        //      �����޸���״̬���������
        int ClearProcessingState(List<ListViewItem> items)
        {
            int nChangedCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strState = ListViewUtil.GetItemText(item, COLUMN_STATE);
                string strNewState = Global.RemoveStateProcessing(strState);

                if (strState != strNewState)
                {
                    ListViewUtil.ChangeItemText(item, COLUMN_STATE, strNewState);
                    SetItemChanged(item, true);
                    nChangedCount++;
                }
            }

            return nChangedCount;
        }

        // �����ԭʼʵ���¼���޸�
        // return:
        //      -1  ����
        //      0   �ɹ�
        //      1   �ж�
        int SaveItemRecords(
            List<ListViewItem> items,
            out int nSavedCount,
            out string strError)
        {
            strError = "";
            nSavedCount = 0;
            int nRet = 0;

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ���ʵ���¼ ...");
            stop.BeginLoop();

            try
            {
                string strPrevBiblioRecPath = "";
                List<EntityInfo> entity_list = new List<EntityInfo>();
                for (int i = 0; i < items.Count; i++)
                {
                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�1";
                            return 1;
                        }
                    }

                    ListViewItem item = items[i];

                    if (item.ImageIndex == TYPE_ERROR)
                    {
                        strError = "ԭʼ�����б��У��� " + (i + 1).ToString() + " ������Ϊ����״̬����Ҫ���ų�������ܽ��б��档";
                        return -1;
                    }

                    OriginItemData data = (OriginItemData)item.Tag;
                    if (data == null)
                    {
                        Debug.Assert(false, "");
                        continue;
                    }
                    if (data.Changed == false)
                        continue;

                    item.Selected = true;   // ��Ҫ�������������ѡ���־
                    nSavedCount++;

                    // Debug.Assert(item.ImageIndex != TYPE_NORMAL, "data.Changed״̬Ϊtrue�����ImageIndex��ӦΪTYPE_NORMAL");

                    string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);

                    if (strBiblioRecPath != strPrevBiblioRecPath
                        && entity_list.Count > 0)
                    {
                        // ����һ������
                        nRet = SaveOneBatchOrders(entity_list,
                            strPrevBiblioRecPath,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        entity_list.Clear();
                    }

                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(data.Xml);
                    }
                    catch (Exception ex)
                    {
                        strError = "item record XMLװ�ص�DOMʱ��������: " + ex.Message;
                        return -1;
                    }

                    DomUtil.SetElementText(dom.DocumentElement,
                        "state", ListViewUtil.GetItemText(item, COLUMN_STATE));

                    // �ݲصص� 2014/9/3
                    DomUtil.SetElementText(dom.DocumentElement,
                        "location", ListViewUtil.GetItemText(item, COLUMN_LOCATION));

                    // TODO: ��Ҫ����<operations>Ԫ��

                    EntityInfo info = new EntityInfo();

                    if (String.IsNullOrEmpty(data.RefID) == true)
                    {
                        data.RefID = Guid.NewGuid().ToString();
                    }

                    info.RefID = data.RefID;
                    info.Action = "change";
                    info.OldRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);
                    info.NewRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH); ;

                    info.NewRecord = dom.OuterXml;
                    info.NewTimestamp = null;

                    info.OldRecord = data.Xml;
                    info.OldTimestamp = data.Timestamp;

                    entity_list.Add(info);

                    strPrevBiblioRecPath = strBiblioRecPath;
                }

                // ���һ������
                if (String.IsNullOrEmpty(strPrevBiblioRecPath) == false
                        && entity_list.Count > 0)
                {
                    // ����һ������
                    nRet = SaveOneBatchOrders(entity_list,
                        strPrevBiblioRecPath,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    entity_list.Clear();
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

        int SaveOneBatchOrders(List<EntityInfo> entity_list,
            string strBiblioRecPath,
            out string strError)
        {
            strError = "";

            EntityInfo[] entities = new EntityInfo[entity_list.Count];
            entity_list.CopyTo(entities);

            EntityInfo[] errorinfos = null;
            long lRet = Channel.SetEntities(
                stop,
                strBiblioRecPath,
                entities,
                out errorinfos,
                out strError);
            if (lRet == -1)
            {
                return -1;
            }

            string strErrorText = "";
            for (int i = 0; i < errorinfos.Length; i++)
            {
                EntityInfo errorinfo = errorinfos[i];

                Debug.Assert(String.IsNullOrEmpty(errorinfo.RefID) == false, "");

                ListViewItem item = null;
                OriginItemData data = FindDataByRefID(errorinfo.RefID, out item);
                if (data == null)
                {
                    strError = "RefID '" + errorinfo.RefID + "' ��Ȼ��ԭʼ�����б����Ҳ�����Ӧ������";
                    return -1;
                }

                // ������Ϣ����
                if (errorinfo.ErrorCode == ErrorCodeValue.NoError)
                {
                    data.Timestamp = errorinfo.NewTimestamp;    // ˢ��timestamp���Ա���淢���޸ĺ��������
                    data.Changed = false;
                    Debug.Assert(String.IsNullOrEmpty(errorinfo.NewRecord) == false, "");
                    data.Xml = errorinfo.NewRecord;
                    if (item.ImageIndex != TYPE_VERIFIED)   // 2012/3/19
                        SetItemColor(item, TYPE_NORMAL);
                    continue;
                }

                if (errorinfos[0].ErrorCode == ErrorCodeValue.TimestampMismatch)
                {
                    // ʱ�����ͻ
                    if (String.IsNullOrEmpty(strErrorText) == false)
                        strErrorText += ";\r\n";
                    strErrorText += "���� '" + ListViewUtil.GetItemText(item, COLUMN_RECPATH) + "' �ڱ�������г���ʱ�����ͻ��������װ��ԭʼ���ݣ�Ȼ������޸ĺͱ��档";
                }
                else
                {
                    // ��������
                    if (String.IsNullOrEmpty(strErrorText) == false)
                        strErrorText += ";\r\n";
                    strErrorText += "���� '" + ListViewUtil.GetItemText(item, COLUMN_RECPATH) + "' �ڱ�������з�������: " + errorinfo.ErrorInfo;
                }

                ListViewUtil.ChangeItemText(item, 
                    COLUMN_ERRORINFO,
                    errorinfo.ErrorInfo);
                SetItemColor(item, TYPE_ERROR);
            }

            if (String.IsNullOrEmpty(strErrorText) == false)
            {
                strError = strErrorText;
                return -1;
            }

            return 0;
        }

        // ����refid��λ��ListViewItem��Tag����
        OriginItemData FindDataByRefID(string strRefID,
            out ListViewItem item)
        {
            item = null;
            for (int i = 0; i < this.listView_in.Items.Count; i++)
            {
                item = this.listView_in.Items[i];
                OriginItemData data = (OriginItemData)item.Tag;
                if (data.RefID == strRefID)
                    return data;
            }

            item = null;
            return null;
        }

        // ͳ�Ƴ�ָ����Χ��������Changed==true�ĸ���
        static int GetChangedCount(List<ListViewItem> items)
        {
            int nCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                OriginItemData data = (OriginItemData)items[i].Tag;
                if (data.Changed == true)
                    nCount ++;
            }

            return nCount;
        }

        // �Ƿ�������ı��δ����?
        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                for (int i = 0; i < this.listView_in.Items.Count; i++)
                {
                    OriginItemData data = (OriginItemData)this.listView_in.Items[i].Tag;
                    if (data != null && data.Changed == true)
                        return true;
                }

                return false;
            }
        }

        private void button_move_notifyReader_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_in.Items.Count == 0)
            {
                strError = "��ǰû���κ�����ɹ�����";
                goto ERROR1;
            }

            int nNotifiedCount = 0;
            List<ListViewItem> items = GetAllItems(this.listView_in);
            nRet = NotifyReader(
                items,
                out nNotifiedCount,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            MessageBox.Show(this, "�ɹ�֪ͨ��Ŀ��¼ "+nNotifiedCount+" ��");
            return;
        ERROR1:
            this.SetNextButtonEnable();
            MessageBox.Show(this, strError);
        }

        // һ����Ŀ��¼�µ����ɹݴ���
        // �Ӳ��¼�еĹݲصص��ֶ��Ѽ����ܶ���
        class OneBiblio
        {
            public List<string> LibrayCodeList = new List<string>();
        }

        // TODO: ���Խ���һ��hashtable���������Ѿ�֪ͨ������Ŀ��¼·��������ظ�֪ͨ���ᾯ��
        int NotifyReader(
    List<ListViewItem> items,
    out int nNotifiedCount,
    out string strError)
        {
            strError = "";
            nNotifiedCount = 0;
            // int nRet = 0;

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����֪ͨ�Ƽ������Ķ��� ...");
            stop.BeginLoop();

            try
            {

                Hashtable biblio_table = new Hashtable();
                foreach (ListViewItem item in items)
                {
                    string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);
                    if (string.IsNullOrEmpty(strBiblioRecPath) == true)
                        continue;

                    string strLocation = ListViewUtil.GetItemText(item, COLUMN_LOCATION);

                    strLocation = StringUtil.GetPureLocation(strLocation);
                    string strLibraryCode = Global.GetLibraryCode(strLocation);

                    OneBiblio biblio = (OneBiblio)biblio_table[strBiblioRecPath];
                    if (biblio == null)
                    {
                        biblio = new OneBiblio();
                        biblio_table[strBiblioRecPath] = biblio;
                    }

                    if (biblio.LibrayCodeList.IndexOf(strLibraryCode) == -1)
                        biblio.LibrayCodeList.Add(strLibraryCode);
                }

                if (biblio_table.Count == 0)
                    return 0;   // û���κ���Ҫ֪ͨ������

                foreach (string strBiblioRecPath in biblio_table.Keys)
                {
                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�1";
                        return 1;
                    }

                    OneBiblio biblio = (OneBiblio)biblio_table[strBiblioRecPath];

                    Debug.Assert(biblio != null, "");

                    byte[] baNewTimestamp = null;
                    string strOutputPath = "";
                    long lRet = Channel.SetBiblioInfo(
                        stop,
                        "notifynewbook",
                        strBiblioRecPath,
                        StringUtil.MakePathList(biblio.LibrayCodeList),
                        "",
                        null,
                        "",
                        out strOutputPath,
                        out baNewTimestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "֪ͨ���߹����д�����Ŀ��¼ '" + strBiblioRecPath + "' ʱ����: " + strError;
                        return -1;
                    }

                    nNotifiedCount++;
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

        // �Ӳ��¼·���ļ�װ��
        private void button_load_loadFromRecPathFile_Click(object sender, EventArgs e)
        {
            int nRet = 0;
            string strError = "";

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵Ĳ��¼·���ļ���";
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
                true,
                new string[] { "summary", "@isbnissn", "targetrecpath" },
                (Control.ModifierKeys == Keys.Control ? false : true),
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // �����ļ���
            this.RecPathFilePath = dlg.FileName;
            this.Text = "����ƽ� " + Path.GetFileName(this.RecPathFilePath);
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
            this.Text = "����ƽ�";
            MessageBox.Show(this, strError);
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

        delegate void Delegate_SetError(ListView list,
    ref ListViewItem item,
    string strBarcodeOrRecPath,
    string strError);

        internal override void SetError(ListView list,
    ref ListViewItem item,
    string strBarcodeOrRecPath,
    string strError)
        {
            // ȷ���̰߳�ȫ 2014/9/3
            if (list != null && list.InvokeRequired)
            {
                Delegate_SetError d = new Delegate_SetError(SetError);
                object[] args = new object[4];
                args[0] = list;
                args[1] = item;
                args[2] = strBarcodeOrRecPath;
                args[3] = strError;
                this.Invoke(d, args);

                // ȡ�� ref ����ֵ
                item = (ListViewItem)args[1];
                return;
            }

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

        internal override int VerifyItem(
            string strPubType,
            string strBarcodeOrRecPath,
            ListViewItem item,
            XmlDocument item_dom,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // ����timestamp/xml
            OriginItemData data = (OriginItemData)item.Tag;
            Debug.Assert(data != null, "");

            if (strPubType == "����������")
            {
                // ����Ƿ�Ϊ�϶����¼���ߵ����¼������Ϊ�϶���Ա
                // return:
                //      0   ���ǡ�ͼ���Ѿ�����ΪTYPE_ERROR
                //      1   �ǡ�ͼ����δ����
                nRet = CheckBindingItem(item);
                if (nRet == 1)
                {
                    // ͼ��
                    // SetItemColor(item, TYPE_NORMAL);
                }
            }
            else
            {
                Debug.Assert(strPubType == "ͼ��", "");
                // ͼ��
                // SetItemColor(item, TYPE_NORMAL);
            }

            // ��������
            bool bIsRecPath = StringUtil.HasHead(strBarcodeOrRecPath, "@path:");
            if (bIsRecPath == false)
            {
                string strBarcode = DomUtil.GetElementText(item_dom.DocumentElement,
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
                    return -1;
                }
            }

            return 0;
        }

        ScanBarcodeForm _scanBarcodeForm = null;

        // װ�ط�ʽ ɨ�������
        private void button_load_scanBarcode_Click(object sender, EventArgs e)
        {
            if (this._scanBarcodeForm == null)
            {
                this._scanBarcodeForm = new ScanBarcodeForm();
                MainForm.SetControlFont(this._scanBarcodeForm, this.Font, false);
                this._scanBarcodeForm.BarcodeScaned += new ScanedEventHandler(_scanBarcodeForm_BarcodeScaned);
                this._scanBarcodeForm.FormClosed += new FormClosedEventHandler(_scanBarcodeForm_FormClosed);
                this._scanBarcodeForm.Show(this);
            }
            else
            {
                if (this._scanBarcodeForm.WindowState == FormWindowState.Minimized)
                    this._scanBarcodeForm.WindowState = FormWindowState.Normal;
            }

            this.ScanMode = true;

            if (this._fillThread == null)
            {
                this._fillThread = new FillThread();
                this._fillThread.Container = this;
                this._fillThread.BeginThread();
            }
        }

        void _scanBarcodeForm_BarcodeScaned(object sender, ScanedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Barcode) == true)
            {
                Console.Beep();
                return;
            }

            // �Ѳ������ֱ�Ӽ������У�Ȼ��ȴ�ר�ŵ��߳���װ��ˢ��
            // Ҫ����
            ListViewItem dup = ListViewUtil.FindItem(this.listView_in, e.Barcode, COLUMN_BARCODE);
            if (dup != null)
            {
                Console.Beep();
                ListViewUtil.SelectLine(dup, true);
                MessageBox.Show(this, "��ɨ��Ĳ������ ��"+e.Barcode+"�� ���б����Ѿ������ˣ���ע�ⲻҪ�ظ�ɨ��");
                this._scanBarcodeForm.Activate();
                return;
            }

            ListViewItem item = new ListViewItem();
            ListViewUtil.ChangeItemText(item, COLUMN_BARCODE, e.Barcode);
            this.listView_in.Items.Add(item);
            ListViewUtil.SelectLine(item, true);
            item.EnsureVisible();

            if (this._fillThread != null)
                this._fillThread.Activate();
        }

        void _scanBarcodeForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this._scanBarcodeForm = null;

            this._fillThread.StopThread(true);
            this._fillThread = null;

            this.ScanMode = false;
        }

        delegate void Delegate_GetNewlyLines(out List<ListViewItem> items,
            out List<string> barcodes);

        internal void GetNewlyLines(out List<ListViewItem> items,
            out List<string> barcodes)
        {
            if (this.InvokeRequired)
            {
                Delegate_GetNewlyLines d = new Delegate_GetNewlyLines(GetNewlyLines);
                object[] args = new object[2];
                args[0] = null;
                args[1] = null;
                this.Invoke(d, args);


                // ȡ��out����ֵ
                items = (List<ListViewItem>)args[0];
                barcodes = (List<string>)args[1];
                return;
            }

            items = new List<ListViewItem>();
            barcodes = new List<string>();

            foreach (ListViewItem item in this.listView_in.Items)
            {
                string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);
                if (string.IsNullOrEmpty(strRecPath) == false)
                    continue;

                string strBarcode = ListViewUtil.GetItemText(item, COLUMN_BARCODE);
                if (string.IsNullOrEmpty(strBarcode) == true)
                    continue;

                items.Add(item);
                barcodes.Add(strBarcode);
            }
        }

        delegate void Delegate_SetRecPathColumn(List<ListViewItem> items,
            List<string> recpaths);
        internal void SetRecPathColumn(List<ListViewItem> items,
            List<string> recpaths)
        {
            if (this.InvokeRequired == true)
            {
                Delegate_SetRecPathColumn d = new Delegate_SetRecPathColumn(SetRecPathColumn);
                this.BeginInvoke(d,
                    new object[] { 
                        items,
                        recpaths }
                    );
                return;
            }

            //
            int i = 0;
            foreach (ListViewItem item in items)
            {
                string strRecPath = recpaths[i];
                if (string.IsNullOrEmpty(strRecPath) == false
                    && strRecPath[0] == '!')
                {
                    ListViewUtil.ChangeItemText(item, COLUMN_ERRORINFO, strRecPath);
                    SetItemColor(item, TYPE_ERROR);
                }
                else
                    ListViewUtil.ChangeItemText(item, COLUMN_RECPATH, strRecPath);
                i++;
            }
        }

        #region װ�� listview_in ������߳�

        class FillThread : ThreadBase
        {
            internal ReaderWriterLock m_lock = new ReaderWriterLock();
            internal static int m_nLockTimeout = 5000;	// 5000=5��

            public ItemHandoverForm Container = null;

            // �����߳�ÿһ��ѭ����ʵ���Թ���
            public override void Worker()
            {
                string strError = "";
                int nRet = 0;

                this.m_lock.AcquireWriterLock(m_nLockTimeout);
                try
                {
                    if (this.Stopped == true)
                        return;

                    // �ҵ���Щ��Ҫ�������ݵ��С�Ҳ���� COLUMN_RECPATH Ϊ�յ���
                    List<ListViewItem> items = new List<ListViewItem>();
                    List<string> barcodes = new List<string>();
                    this.Container.GetNewlyLines(out items,
            out barcodes);

                    if (barcodes.Count > 0)
                    {
                        // ת��Ϊ��¼·��
                        List<string> recpaths = new List<string>();
                        nRet = this.Container.ConvertItemBarcodeToRecPath(
                            barcodes,
                            out recpaths,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        Debug.Assert(barcodes.Count == recpaths.Count, "");

                        // �� recpath ����������
                        this.Container.SetRecPathColumn(items, recpaths);

                        // ˢ��ָ������
                        this.Container.RefreshLines(COLUMN_RECPATH,
    items,
    true,
    new string[] { "summary", "@isbnissn", "targetrecpath" });
                    }

                    // m_bStopThread = true;   // ֻ��һ�־�ֹͣ
                }
                finally
                {
                    this.m_lock.ReleaseWriterLock();
                }

            ERROR1:
                // Safe_setError(this.Container.listView_in, strError);
                return;
            }

        }

        bool _scanMode = false;

        /// <summary>
        /// �Ƿ���ɨ�������״̬��
        /// </summary>
        public bool ScanMode
        {
            get
            {
                return this._scanMode;
            }
            set
            {
                if (this._scanMode == value)
                    return;

                this._scanMode = value;

                this.comboBox_load_type.Enabled = !this._scanMode;
                this.button_load_loadFromBarcodeFile.Enabled = !this._scanMode;
                this.button_load_loadFromBatchNo.Enabled = !this._scanMode;
                this.button_load_loadFromRecPathFile.Enabled = !this._scanMode;
                this.button_load_scanBarcode.Enabled = !this._scanMode;

                if (this._scanMode == false)
                {
                    if (this._scanBarcodeForm != null)
                        this._scanBarcodeForm.Close();
                }
                else
                {
                    button_load_scanBarcode_Click(this, new EventArgs());
                }
            }
        }

        // ���л������
        // return:
        //      -1  ����
        //      ����  �����������
        int DoReturn(List<ListViewItem> items,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (items.Count == 0)
            {
                strError = "��δ����Ҫ���������";
                return -1;
            }

            if (stop != null && stop.State == 0)    // 0 ��ʾ���ڴ���
            {
                strError = "Ŀǰ�г��������ڽ��У��޷����л���Ĳ���";
                return -1;
            }

            string strOperName = "����";

            int nCount = 0;
            List<ListViewItem> oper_items = new List<ListViewItem>();

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ���" + strOperName + "���� ...");
            stop.BeginLoop();

            this.EnableControls(false);
            try
            {
                // ��һ���µĿ�ݳ��ɴ�
                QuickChargingForm form = new QuickChargingForm();
                form.MdiParent = this.MainForm;
                form.MainForm = this.MainForm;
                form.BorrowComplete -= new BorrowCompleteEventHandler(form_BorrowComplete);
                form.BorrowComplete += new BorrowCompleteEventHandler(form_BorrowComplete);
                form.Show();

                form.SmartFuncState = FuncState.Return;

                stop.SetProgressRange(0, items.Count);

                int i = 0;
                foreach (ListViewItem item in items)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }

                    string strBorrower = ListViewUtil.GetItemText(item, COLUMN_BORROWER);
                    if (string.IsNullOrEmpty(strBorrower) == true)
                        continue;

                    string strItemBarcode = ListViewUtil.GetItemText(item, COLUMN_BARCODE);

                    if (String.IsNullOrEmpty(strItemBarcode) == true)
                        continue;

                    form.AsyncDoAction(form.SmartFuncState, strItemBarcode);

                    stop.SetProgressValue(++i);

                    nCount++;
                    oper_items.Add(item);
                }

                // form.Close();
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

            // ע�����ڲ�֪�������������ʲôʱ����ɣ����Դ˴��޷�ˢ�� items ����ʾ
            return nCount;
        }

        void form_BorrowComplete(object sender, BorrowCompleteEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ItemBarcode) == true)
                return;

            ListViewItem item = ListViewUtil.FindItem(this.listView_in, e.ItemBarcode, COLUMN_BARCODE);
            if (item == null)
                return;
            List<ListViewItem> items = new List<ListViewItem>();
            items.Add(item);
            RefreshLines(COLUMN_RECPATH,
items,
false,
new string[] { "summary", "@isbnissn", "targetrecpath" });
        }

        #endregion

        // �޸�ȫ������Ĺݲص�
        private void button_move_changeLocation_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_in.Items.Count == 0)
            {
                strError = "��ǰû���κ�����ɹ�����";
                goto ERROR1;
            }

            List<ListViewItem> items = GetAllItems(this.listView_in);

            // return:
            //      0   �����޸�
            //      1   �������޸�
            nRet = ChangeLocation(items,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
                return;

            this.SetNextButtonEnable();

            if (string.IsNullOrEmpty(strError) == false)
                MessageBox.Show(this, strError);
            return;
        ERROR1:
            this.SetNextButtonEnable();
            MessageBox.Show(this, strError);
        }

    }

    // �������ض�ȱʡֵ��PrintOption������
    internal class ItemHandoverPrintOption : PrintOption
    {
        string PublicationType = "ͼ��"; // ͼ�� ����������

        public ItemHandoverPrintOption(string strDataDir,
            string strPublicationType)
        {
            this.DataDir = strDataDir;
            this.PublicationType = strPublicationType;

            this.PageHeaderDefault = "%date% ���ƽ��嵥 -- ���κ�: %batchno% -- �ݲصص�: %location% -- (�� %pagecount% ҳ)";
            this.PageFooterDefault = "%pageno%/%pagecount%";

            this.TableTitleDefault = "%date% ���ƽ��嵥";

            this.LinesPerPageDefault = 20;

            // 2008/9/5
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
            column.MaxChars = 50;
            this.Columns.Add(column);


            // "location -- �ݲصص�"
            column = new Column();
            column.Name = "location -- �ݲصص�";
            column.Caption = "-----�ݲصص�-----";  // ȷ���еĿ�ȵ�һ�ּ򵥰취
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "price -- ��۸�"
            column = new Column();
            column.Name = "price -- ��۸�";
            column.Caption = "��۸�";
            column.MaxChars = -1;
            this.Columns.Add(column);

            /* ȱʡʱ�������ּ۸�
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