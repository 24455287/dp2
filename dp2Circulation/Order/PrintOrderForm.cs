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
using System.Reflection;
using System.Web;   // HttpUtility

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.CirculationClient;

using DigitalPlatform.IO;
using DigitalPlatform.CommonControl;

using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.Marc;

using DigitalPlatform.CirculationClient.localhost;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DigitalPlatform.dp2.Statis;
using DocumentFormat.OpenXml;

namespace dp2Circulation
{
    /// <summary>
    /// ��ӡ����
    /// </summary>
    public partial class PrintOrderForm : BatchPrintFormBase
    {
        List<string> UsedAssemblyFilenames = new List<string>();

        /// <summary>
        /// �ű�������
        /// </summary>
        public ScriptManager ScriptManager = new ScriptManager();

        string BatchNo = "";    // ����ڼ����������������κ�

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        public MainForm MainForm
        {
            get
            {
                return (MainForm)this.MdiParent;
            }
        }
        
        DigitalPlatform.Stop stop = null;
#endif

        /// <summary>
        /// ����ͼ���±�: ����
        /// </summary>
        public const int TYPE_ERROR = 0;
        /// <summary>
        /// ����ͼ���±�: ��ͨ
        /// </summary>
        public const int TYPE_NORMAL = 1;   // �鱾��������
        /// <summary>
        /// ����ͼ���±�: �������޸�
        /// </summary>
        public const int TYPE_CHANGED = 2;  // �鱾������������ʾԭʼ��¼���ڴ����з������޸ģ�����δ����

        /// <summary>
        /// ���ʹ�ù��ļ�¼·���ļ�ȫ·��
        /// </summary>
        public string RecPathFilePath = "";

        // ����������к�����
        SortColumns SortColumns_origin = new SortColumns();
        SortColumns SortColumns_merged = new SortColumns();

        #region ԭʼ���� ListView �к�

        /// <summary>
        /// ԭʼ�����к�: ��¼·��
        /// </summary>
        public static int ORIGIN_COLUMN_RECPATH = 0;    // ��¼·��
        /// <summary>
        /// ԭʼ�����к�: ժҪ
        /// </summary>
        public static int ORIGIN_COLUMN_SUMMARY = 1;    // ժҪ
        /// <summary>
        /// ԭʼ�����к�: ������Ϣ
        /// </summary>
        public static int ORIGIN_COLUMN_ERRORINFO = 1;  // ������Ϣ
        /// <summary>
        /// ԭʼ�����к�: ISBN/ISSN
        /// </summary>
        public static int ORIGIN_COLUMN_ISBNISSN = 2;           // ISBN/ISSN
        /// <summary>
        /// ԭʼ�����к�: ״̬
        /// </summary>
        public static int ORIGIN_COLUMN_STATE = 3;      // ״̬
        /// <summary>
        /// ԭʼ�����к�: ��Ŀ��
        /// </summary>
        public static int ORIGIN_COLUMN_CATALOGNO = 4;          // ��Ŀ��
        /// <summary>
        /// ԭʼ�����к�: ����
        /// </summary>
        public static int ORIGIN_COLUMN_SELLER = 5;        // ����
        /// <summary>
        /// ԭʼ�����к�: ������Դ
        /// </summary>
        public static int ORIGIN_COLUMN_SOURCE = 6;        // ������Դ
        /// <summary>
        /// ԭʼ�����к�: ʱ�䷶Χ
        /// </summary>
        public static int ORIGIN_COLUMN_RANGE = 7;        // ʱ�䷶Χ
        /// <summary>
        /// ԭʼ�����к�: ��������
        /// </summary>
        public static int ORIGIN_COLUMN_ISSUECOUNT = 8;        // ��������
        /// <summary>
        /// ԭʼ�����к�: ������
        /// </summary>
        public static int ORIGIN_COLUMN_COPY = 9;              // ������
        /// <summary>
        /// ԭʼ�����к�: ����
        /// </summary>
        public static int ORIGIN_COLUMN_PRICE = 10;             // ����
        /// <summary>
        /// ԭʼ�����к�: �ܼ۸�
        /// </summary>
        public static int ORIGIN_COLUMN_TOTALPRICE = 11;        // �ܼ۸�
        /// <summary>
        /// ԭʼ�����к�: ����ʱ��
        /// </summary>
        public static int ORIGIN_COLUMN_ORDERTIME = 12;        // ����ʱ��
        /// <summary>
        /// ԭʼ�����к�: ������
        /// </summary>
        public static int ORIGIN_COLUMN_ORDERID = 13;          // ������
        /// <summary>
        /// ԭʼ�����к�: �ݲط���
        /// </summary>
        public static int ORIGIN_COLUMN_DISTRIBUTE = 14;       // �ݲط���
        /// <summary>
        /// ԭʼ�����к�: ���
        /// </summary>
        public static int ORIGIN_COLUMN_CLASS = 15;             // ���
        /// <summary>
        /// ԭʼ�����к�: ��ע
        /// </summary>
        public static int ORIGIN_COLUMN_COMMENT = 16;          // ��ע
        /// <summary>
        /// ԭʼ�����к�: ���κ�
        /// </summary>
        public static int ORIGIN_COLUMN_BATCHNO = 17;          // ���κ�
        /// <summary>
        /// ԭʼ�����к�: ������ַ
        /// </summary>
        public static int ORIGIN_COLUMN_SELLERADDRESS = 18;    // ������ַ
        /// <summary>
        /// ԭʼ�����к�: �ּ�¼·��
        /// </summary>
        public static int ORIGIN_COLUMN_BIBLIORECPATH = 19;    // �ּ�¼·��

        #endregion

        #region �ϲ������� ListView �к�

        /// <summary>
        /// �ϲ������ݵ��к�: ����
        /// </summary>
        public static int MERGED_COLUMN_SELLER = 0;             // ����
        /// <summary>
        /// �ϲ������ݵ��к�: ��Ŀ��
        /// </summary>
        public static int MERGED_COLUMN_CATALOGNO = 1;          // ��Ŀ��
        /// <summary>
        /// �ϲ������ݵ��к�: ժҪ
        /// </summary>
        public static int MERGED_COLUMN_SUMMARY = 2;    // ժҪ
        /// <summary>
        /// �ϲ������ݵ��к�: ������Ϣ
        /// </summary>
        public static int MERGED_COLUMN_ERRORINFO = 2;  // ������Ϣ
        /// <summary>
        /// �ϲ������ݵ��к�: ISBN/ISSN
        /// </summary>
        public static int MERGED_COLUMN_ISBNISSN = 3;           // ISBN/ISSN
        /// <summary>
        /// �ϲ������ݵ��к�: �ϲ�ע��
        /// </summary>
        public static int MERGED_COLUMN_MERGECOMMENT = 4;      // �ϲ�ע��
        /// <summary>
        /// �ϲ������ݵ��к�: ʱ�䷶Χ
        /// </summary>
        public static int MERGED_COLUMN_RANGE = 5;        // ʱ�䷶Χ
        /// <summary>
        /// �ϲ������ݵ��к�: ��������
        /// </summary>
        public static int MERGED_COLUMN_ISSUECOUNT = 6;        // ��������
        /// <summary>
        /// �ϲ������ݵ��к�: ������
        /// </summary>
        public static int MERGED_COLUMN_COPY = 7;              // ������
        /// <summary>
        /// �ϲ������ݵ��к�: ÿ�ײ���
        /// </summary>
        public static int MERGED_COLUMN_SUBCOPY = 8;              // ÿ�ײ���
        /// <summary>
        /// �ϲ������ݵ��к�: ����
        /// </summary>
        public static int MERGED_COLUMN_PRICE = 9;             // ����
        /// <summary>
        /// �ϲ������ݵ��к�: �ܼ۸�
        /// </summary>
        public static int MERGED_COLUMN_TOTALPRICE = 10;        // �ܼ۸�
        /// <summary>
        /// �ϲ������ݵ��к�: ����ʱ��
        /// </summary>
        public static int MERGED_COLUMN_ORDERTIME = 11;        // ����ʱ��
        /// <summary>
        /// �ϲ������ݵ��к�: ������
        /// </summary>
        public static int MERGED_COLUMN_ORDERID = 12;          // ������
        /// <summary>
        /// �ϲ������ݵ��к�: �ݲط���
        /// </summary>
        public static int MERGED_COLUMN_DISTRIBUTE = 13;       // �ݲط���
        /// <summary>
        /// �ϲ������ݵ��к�: �ѵ�������
        /// </summary>
        public static int MERGED_COLUMN_ACCEPTCOPY = 14;       // �ѵ�������

        /// <summary>
        /// �ϲ������ݵ��к�: �ѵ���ÿ�ײ���
        /// </summary>
        public static int MERGED_COLUMN_ACCEPTSUBCOPY = 15;       // �ѵ���ÿ�ײ���

        /// <summary>
        /// �ϲ������ݵ��к�: ���鵥��
        /// </summary>
        public static int MERGED_COLUMN_ACCEPTPRICE = 16;       // ���鵥��

        /// <summary>
        /// �ϲ������ݵ��к�: ���
        /// </summary>
        public static int MERGED_COLUMN_CLASS = 17;             // ���
        /// <summary>
        /// �ϲ������ݵ��к�: ��ע
        /// </summary>
        public static int MERGED_COLUMN_COMMENT = 18;          // ��ע
        /// <summary>
        /// �ϲ������ݵ��к�: ������ַ
        /// </summary>
        public static int MERGED_COLUMN_SELLERADDRESS = 19;    // ������ַ
        /// <summary>
        /// �ϲ������ݵ��к�: �ּ�¼·��
        /// </summary>
        public static int MERGED_COLUMN_BIBLIORECPATH = 20;    // �ּ�¼·��

        #endregion

        const int WM_LOADSIZE = API.WM_USER + 201;

        /// <summary>
        /// ���캯��
        /// </summary>
        public PrintOrderForm()
        {
            InitializeComponent();
        }

        private void PrintOrderForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
            CreateOriginColumnHeader(this.listView_origin);
            CreateMergedColumnHeader(this.listView_merged);

#if NO
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            this.comboBox_load_type.Text = this.MainForm.AppInfo.GetString(
                "printorder_form",
                "publication_type",
                "ͼ��");

            // �������
            this.checkBox_print_accepted.Checked = this.MainForm.AppInfo.GetBoolean(
                "printorder_form",
                "print_accepted",
                false);

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);

            ScriptManager.applicationInfo = this.MainForm.AppInfo;
            ScriptManager.CfgFilePath =
                this.MainForm.DataDir + "\\output_order_projects.xml";  // ����ķ������ǲ��ֳ��������͵�
            ScriptManager.DataDir = this.MainForm.DataDir;

            ScriptManager.CreateDefaultContent -= new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);
            ScriptManager.CreateDefaultContent += new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);

            try
            {
                ScriptManager.Load();
            }
            catch (FileNotFoundException)
            {
                // ���ر���
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void PrintOrderForm_FormClosing(object sender, FormClosingEventArgs e)
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

            if (this.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ��������ԭʼ��Ϣ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
                    "PrintOrderForm",
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

        private void PrintOrderForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            this.MainForm.AppInfo.SetString(
                "printorder_form",
                "publication_type",
                this.comboBox_load_type.Text);

            this.MainForm.AppInfo.SetBoolean(
    "printorder_form",
    "print_accepted",
    this.checkBox_print_accepted.Checked);

            SaveSize();
        }

        /*public*/
        void LoadSize()
        {
#if NO
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state");
#endif

            string strWidths = this.MainForm.AppInfo.GetString(
                "printorder_form",
                "list_origin_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_origin,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
    "printorder_form",
    "list_merged_width",
    "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_merged,
                    strWidths,
                    true);
            }
        }

        /*public*/
        void SaveSize()
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

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_origin);
            this.MainForm.AppInfo.SetString(
                "printorder_form",
                "list_origin_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.listView_merged);
            this.MainForm.AppInfo.SetString(
                "printorder_form",
                "list_merged_width",
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

        bool m_bEnabled = true;

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.m_bEnabled = bEnable;

            // load page
            this.button_load_loadFromBatchNo.Enabled = bEnable;
            this.button_load_loadFromFile.Enabled = bEnable;

            this.comboBox_load_type.Enabled = bEnable;

            // next button
            if (bEnable == true)
                SetNextButtonEnable();
            else
            {
                this.button_next.Enabled = false;

                //
                this.button_saveChange_saveChange.Enabled = false;
            }

            // print page
            this.button_print_mergedOption.Enabled = bEnable;
            this.button_print_printOrderList.Enabled = bEnable;
            this.button_print_originOption.Enabled = bEnable;
            this.button_print_printOriginList.Enabled = bEnable;
            this.button_print_outputOrderOption.Enabled = bEnable;
            this.button_print_outputOrder.Enabled = bEnable;
            this.button_print_arriveRatioStatis.Enabled = bEnable;

            if (this.checkBox_print_accepted.Checked == true)
                this.checkBox_print_accepted.Enabled = bEnable;
            else
                this.checkBox_print_accepted.Enabled = false;
        }

        /// <summary>
        /// �Ƿ�Ϊ�������Ρ��� ����������� checkbox �Ƿ�ѡ
        /// </summary>
        public bool AcceptCondition
        {
            get
            {
                return this.checkBox_print_accepted.Checked;
            }
            set
            {
                this.checkBox_print_accepted.Checked = value;
            }
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
            else if (this.tabControl_main.SelectedTab == this.tabPage_saveChange)
            {

                //      0   û�з������޸�
                //      1   �������޸ģ�������δ����
                //      2   �������޸ģ��Ѿ�������ˣ���û���µ��޸�
                int nState = ReportSaveChangeState(out strError);
                int nErrorCount = GetErrorLineCount();
                if (nState == 1)
                {
                    this.button_saveChange_saveChange.Enabled = true;
                }
                else
                {
                    this.button_saveChange_saveChange.Enabled = false;
                }

                // û�д������û���޸Ļ����޸��Ѿ�����
                if (nErrorCount == 0 && nState != 1)
                    this.button_next.Enabled = true;
                else
                    this.button_next.Enabled = false;

                if (nErrorCount > 0)
                    strError += "\r\n\r\nԭʼ�����б����д�������(��ɫ����) " + nErrorCount.ToString() + "��";

                this.textBox_saveChange_info.Text = strError;
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

            for (int i = 0; i < this.listView_origin.Items.Count; i++)
            {
                ListViewItem item = this.listView_origin.Items[i];

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

        int m_nSavedCount = 0;  // ��ǰ������Ĵ���

        // �㱨������������
        // return:
        //      true    �����Ѿ����
        //      false   ������δ���
        //      0   û�з������޸�
        //      1   �������޸ģ�������δ����
        //      2   �������޸ģ��Ѿ�������ˣ���û���µ��޸�
        int ReportSaveChangeState(out string strError)
        {
            strError = "";

            // ȫ��listview�����TYPE_NORMAL״̬���ű��������Ѿ����
            int nYellowCount = 0;   // �������޸ĵ�����
            int nRedCount = 0;  // �д�����Ϣ������
            int nWhiteCount = 0;    // ��ͨ����

            for (int i = 0; i < this.listView_origin.Items.Count; i++)
            {
                ListViewItem item = this.listView_origin.Items[i];

                if (item.ImageIndex == TYPE_CHANGED)
                    nYellowCount++;
                else if (item.ImageIndex == TYPE_ERROR)
                    nRedCount++;
                else if (item.ImageIndex == TYPE_NORMAL)
                    nWhiteCount++;
            }

            if (nYellowCount == 0)
            {
                if (this.m_nSavedCount == 0)
                {
                    strError = "û�з������޸�";
                    // return true;
                    return 0;
                }

                strError = "�Ѿ�����";
                return 2;
            }

            // strError = "ԭʼ�����б����з������޸ĺ���δ�����������д������\r\n\r\n�б�����:\r\n�������޸ĵ�����(����ɫ����) " + nYellowCount.ToString() + " ��\r\n��������(��ɫ����) " + nRedCount.ToString() + "��\r\n\r\n(ֻ��ȫ�����Ϊ��ͨ״̬(��ɫ����)���ű�����������Ѿ����)";
            // return false;
            strError = "ԭʼ�����б����з������޸ĺ���δ���������(����ɫ����) " + nYellowCount.ToString() + " ��\r\n\r\n(ֻ�е��б���ȫ�����Ϊ��ͨ״̬(��ɫ����)���ű�����������Ѿ����)";
            return 1;
        }

        // ����״̬����Ŀ
        int GetErrorLineCount()
        {
            int nRedCount = 0;  // �д�����Ϣ������

            for (int i = 0; i < this.listView_origin.Items.Count; i++)
            {
                ListViewItem item = this.listView_origin.Items[i];

                if (item.ImageIndex == TYPE_ERROR)
                    nRedCount++;
            }

            return nRedCount;
        }

        // parameters:
        //      bAutoSetSeriesType  �Ƿ�����ļ���һ���е�·���е����ݿ������Զ�����Combobox_type
        // return:
        //      -1  ����
        //      0   ��������
        //      1   �ɹ�
        /// <summary>
        /// �Ӷ�����¼·���ļ���װ������
        /// </summary>
        /// <param name="bAutoSetSeriesType">�Ƿ��Զ������ļ��������ó���������</param>
        /// <param name="strFilename">������¼·���ļ���ȫ·��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:  ����</para>
        /// <para>0:   ��������</para>
        /// <para>1:   �ɹ�</para>
        /// </returns>
        public int LoadFromOrderRecPathFile(
            bool bAutoSetSeriesType,
            string strFilename,
            out string strError)
        {
            strError = "";

            int nDupCount = 0;
            int nRet = 0;

            StreamReader sr = null;
            try
            {
                // ���ļ�
                sr = new StreamReader(strFilename);

                EnableControls(false);
                // MainForm.ShowProgress(true);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڳ�ʼ���������� ...");
                stop.BeginLoop();
                this.Update();
                this.MainForm.Update();

                try
                {
                    if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                    {
                    }
                    else
                    {
                        if (this.Changed == true)
                        {
                            // ������δ����
                            DialogResult result = MessageBox.Show(this,
                                "��ǰ��������ԭʼ��Ϣ���޸ĺ���δ���档����ʱΪװ�������ݶ����ԭ����Ϣ����δ������Ϣ����ʧ��\r\n\r\nȷʵҪװ��������? ",
                                "PrintOrderForm",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button2);
                            if (result != DialogResult.Yes)
                            {
                                return 0; // ����
                            }
                        }

                        this.listView_origin.Items.Clear();
                        // 2008/11/22 new add
                        this.SortColumns_origin.Clear();
                        SortColumns.ClearColumnSortDisplay(this.listView_origin.Columns);

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


                        string strOrderRecPath = "";
                        strOrderRecPath = sr.ReadLine();

                        if (strOrderRecPath == null)
                            break;

                        strOrderRecPath = strOrderRecPath.Trim();
                        if (String.IsNullOrEmpty(strOrderRecPath) == true)
                            continue;

                        if (strOrderRecPath[0] == '#')
                            continue;   // ע����

                        // ��鶩����·��
                        {
                            string strDbName = Global.GetDbName(strOrderRecPath);
                            string strBiblioDbName = this.MainForm.GetBiblioDbNameFromOrderDbName(strDbName);
                            if (string.IsNullOrEmpty(strBiblioDbName) == true)
                            {
                                strError = "��¼·�� '" + strOrderRecPath + "' �е����ݿ��� '" + strDbName + "' ���Ƕ�������";
                                goto ERROR1;
                            }
                            BiblioDbProperty prop = this.MainForm.GetBiblioDbProperty(strBiblioDbName);
                            if (prop == null)
                            {
                                strError = "���ݿ��� '" + strBiblioDbName + "' ������Ŀ����";
                                goto ERROR1;
                            }

                            // �Զ����� ͼ��/�ڿ� ����
                            if (bAutoSetSeriesType == true && nLineCount == 0)
                            {
                                if (string.IsNullOrEmpty(prop.IssueDbName) == true)
                                    this.comboBox_load_type.Text = "ͼ��";
                                else
                                    this.comboBox_load_type.Text = "����������";
                            }

                            if (string.IsNullOrEmpty(prop.IssueDbName) == false)
                            {
                                // �ڿ���
                                if (this.comboBox_load_type.Text != "����������")
                                {
                                    strError = "��¼·�� '" + strOrderRecPath + "' �еĶ������� '" + strDbName + "' ����ͼ������";
                                    goto ERROR1;
                                }
                            }
                            else
                            {
                                // ͼ���
                                if (this.comboBox_load_type.Text != "ͼ��")
                                {
                                    strError = "��¼·�� '" + strOrderRecPath + "' �еĶ������� '" + strDbName + "' �����ڿ�����";
                                    goto ERROR1;
                                }
                            }
                        }

                        nLineCount++;
                    }

                    // ���ý��ȷ�Χ
                    stop.SetProgressRange(0, nLineCount);
                    // stop.SetProgressValue(0);

                    // ���д���
                    // �ļ���ͷ?
                    // sr.BaseStream.Seek(0, SeekOrigin.Begin);

                    sr.Close();

                    sr = new StreamReader(strFilename);


                    for (int i = 0; ; i++)
                    {
                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�2";
                                goto ERROR1;
                            }
                        }

                        string strOrderRecPath = "";
                        strOrderRecPath = sr.ReadLine();

                        stop.SetProgressValue(i);

                        if (strOrderRecPath == null)
                            break;

                        strOrderRecPath = strOrderRecPath.Trim();
                        if (String.IsNullOrEmpty(strOrderRecPath) == true)
                            continue;

                        if (strOrderRecPath[0] == '#')
                            continue;   // ע����

                        stop.SetMessage("����װ��·�� " + strOrderRecPath + " ��Ӧ�ļ�¼...");


                        // ���ݼ�¼·����װ�붩����¼
                        // return: 
                        //      -2  ·���Ѿ���list�д�����
                        //      -1  ����
                        //      1   �ɹ�
                        nRet = LoadOneItem(strOrderRecPath,
                            this.listView_origin,
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
            this.RecPathFilePath = strFilename;
            this.Text = "��ӡ���� " + Path.GetFileName(this.RecPathFilePath);

            if (nDupCount != 0)
            {
                MessageBox.Show(this, "װ��������� " + nDupCount.ToString() + "���ظ���¼·����������ԡ�");
            }

            // ���ϲ��������б�
            stop.SetMessage("���ںϲ�����...");
            nRet = FillMergedList(out strError);
            stop.SetMessage("");
            if (nRet == -1)
                goto ERROR1;


            // �㱨����װ�������
            // return:
            //      0   ��δװ���κ�����    
            //      1   װ���Ѿ����
            //      2   ��Ȼװ�������ݣ����������д�������
            int nState = ReportLoadState(out strError);
            if (nState != 1)
                goto ERROR1;

            return 1;
        ERROR1:
            return -1;
        }

        // �Ӷ������¼·���ļ�װ��
        private void button_load_loadFromFile_Click(object sender, EventArgs e)
        {
            this.BatchNo = "";  // ��ʾ���Ǹ������κŻ�õ�����

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵Ķ������¼·���ļ���";
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string strError = "";

            // return:
            //      -1  ����
            //      0   ����
            //      1   װ�سɹ�
            int nRet = LoadFromOrderRecPathFile(
                true,
                dlg.FileName,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            this.Text = "��ӡ����";
            MessageBox.Show(this, strError);
        }

        // 
        /// <summary>
        /// ����Ŀ��¼ XML ��ʽת��Ϊ MARC ��ʽ��
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strXml">��Ŀ��¼ XML</param>
        /// <param name="strMarc">���� MARC ��¼(���ڸ�ʽ)</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����: 0: �ɹ�</returns>
        public int ConvertXmlToMarc(
            string strBiblioRecPath,
            string strXml,
            out string strMarc,
            out string strError)
        {
            strError = "";
            strMarc = "";
            int nRet = 0;

            // strXml��Ϊ��Ŀ��¼
            string strBiblioDbName = Global.GetDbName(strBiblioRecPath);

            string strSyntax = this.MainForm.GetBiblioSyntax(strBiblioDbName);
            if (String.IsNullOrEmpty(strSyntax) == true)
                strSyntax = "unimarc";

            if (strSyntax == "usmarc" || strSyntax == "unimarc")
            {
                // ��XML��Ŀ��¼ת��ΪMARC��ʽ
                string strOutMarcSyntax = "";

                // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
                // parameters:
                //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
                //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
                //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
                nRet = MarcUtil.Xml2Marc(strXml,
                    true,   // 2013/1/12 �޸�Ϊtrue
                    "", // strMarcSyntax
                    out strOutMarcSyntax,
                    out strMarc,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (String.IsNullOrEmpty(strOutMarcSyntax) == false)
                {
                    if (strOutMarcSyntax != strSyntax)
                    {
                        strError = "��Ŀ��¼ " + strBiblioRecPath + " ��syntax '" + strOutMarcSyntax + "' �����������ݿ� '" + strBiblioDbName + "' �Ķ���syntax '" + strSyntax + "' ��һ��";
                        return -1;
                    }
                }

                return 0;
            }

            strError = "��Ŀ�� '" + strBiblioDbName + "' �ĸ�ʽ����MARC��ʽ��(���� '" + strSyntax + "')";
            return -1;
        }

        // �����Ŀ����(XML��ʽ)
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// �� dpLibrary ���������һ����Ŀ��¼��
        /// ��ο� dp2Library API GetBiblioInfos() ����ϸ����
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strXmlRecord">��Ŀ��¼ XML</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:  ����</para>
        /// <para>0:   û���ҵ�</para>
        /// <para>1:   �ҵ�</para>
        /// </returns>
        public int GetBiblioRecord(string strBiblioRecPath,
            out string strXmlRecord,
            out string strError)
        {
            strError = "";
            strXmlRecord = "";

            string[] formats = new string[1];
            formats[0] = "xml";
            string[] results = null;
            byte[] timestamp = null;

            if (String.IsNullOrEmpty(strBiblioRecPath) == true)
            {
                strError = "strBiblioRecPath����ֵ����Ϊ��";
                return -1;
            }

            long lRet = Channel.GetBiblioInfos(
                stop,
                strBiblioRecPath,
                "",
                formats,
                out results,
                out timestamp,
                out strError);
            if (lRet == 0)
            {
                if (String.IsNullOrEmpty(strError) == true)
                    strError = "��¼ " + strBiblioRecPath + " û���ҵ�";

                return 0;   // not found
            }
            if (lRet == -1)
            {
                strError = "�����Ŀ��¼ʱ��������: " + strError;
                return -1;
            }
            else
            {
                Debug.Assert(results != null && results.Length == 1, "results�������1��Ԫ��");
                strXmlRecord = results[0];
            }

            return (int)lRet;
        }

        // ���ݼ�¼·����װ�붩����¼
        // return: 
        //      -2  ·���Ѿ���list�д�����
        //      -1  ����
        //      1   �ɹ�
        int LoadOneItem(string strRecPath,
            ListView list,
            out string strError)
        {
            strError = "";

            string strItemXml = "";
            string strBiblioText = "";

            string strOutputOrderRecPath = "";
            string strOutputBiblioRecPath = "";

            byte[] item_timestamp = null;

            long lRet = Channel.GetOrderInfo(
                stop,
                "@path:" + strRecPath,
                // "",
                "xml",
                out strItemXml,
                out strOutputOrderRecPath,
                out item_timestamp,
                "recpath",
                out strBiblioText,
                out strOutputBiblioRecPath,
                out strError);
            if (lRet == -1 || lRet == 0)
            {
                ListViewItem item = new ListViewItem(strRecPath, 0);

                OriginItemData data = new OriginItemData();
                item.Tag = data;
                data.Timestamp = item_timestamp;
                data.Xml = strItemXml;

                item.SubItems.Add(strError);

                SetItemColor(item, TYPE_ERROR);
                list.Items.Add(item);

                // ���¼�������������Ұ
                list.EnsureVisible(list.Items.Count - 1);

                goto ERROR1;
            }

            string strBiblioSummary = "";
            string strISBnISSN = "";

            // ������¼·���Ƿ����ظ�?
            // ˳����ͬ�ֵ�����
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem curitem = list.Items[i];

                if (strRecPath == curitem.Text)
                {
                    strError = "��¼·�� " + strRecPath + " �����ظ�";
                    return -2;
                }

                if (strBiblioSummary == "" && curitem.ImageIndex != TYPE_ERROR)
                {
                    if (curitem.SubItems[ORIGIN_COLUMN_BIBLIORECPATH].Text == strOutputBiblioRecPath)
                    {
                        strBiblioSummary = ListViewUtil.GetItemText(curitem, ORIGIN_COLUMN_SUMMARY);
                        strISBnISSN = ListViewUtil.GetItemText(curitem, ORIGIN_COLUMN_ISBNISSN);
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

                Debug.Assert(String.IsNullOrEmpty(strOutputBiblioRecPath) == false, "strBiblioRecPathֵ����Ϊ��");

                lRet = Channel.GetBiblioInfos(
                    stop,
                    strOutputBiblioRecPath,
                    "",
                    formats,
                    out results,
                    out timestamp,
                    out strError);
                if (lRet == -1 || lRet == 0)
                {
                    if (lRet == 0 && String.IsNullOrEmpty(strError) == true)
                        strError = "��Ŀ��¼ '" + strOutputBiblioRecPath + "' ������";

                    strBiblioSummary = "�����ĿժҪʱ��������: " + strError;
                }
                else
                {
                    Debug.Assert(results != null && results.Length == 2, "results�������2��Ԫ��");
                    strBiblioSummary = results[0];
                    strISBnISSN = results[1];
                }
            }

            // ����һ������xml��¼��ȡ���й���Ϣ����listview��

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
                ListViewItem item = AddToListView(
                    this.comboBox_load_type.Text,
                    this.checkBox_print_accepted.Checked,
                    list,
                    dom,
                    strOutputOrderRecPath,
                    strBiblioSummary,
                    strISBnISSN,
                    strOutputBiblioRecPath);

                // ����timestamp/xml
                OriginItemData data = (OriginItemData)item.Tag;
                Debug.Assert(data != null, "");
                data.Timestamp = item_timestamp;
                data.Xml = strItemXml;

                /*
                // ͼ��
                SetItemColor(item, TYPE_NORMAL);
                 * */

                // ���¼�������������Ұ
                list.EnsureVisible(list.Items.Count - 1);
            }

            return 1;
        ERROR1:
            return -1;
        }

        static System.Drawing.Color GetItemForeColor(int nType)
        {
            if (nType == TYPE_ERROR)
            {
                return System.Drawing.Color.White;
            }
            else if (nType == TYPE_CHANGED)
            {
                return System.Drawing.SystemColors.WindowText;
            }
            else if (nType == TYPE_NORMAL)
            {
                return System.Drawing.SystemColors.WindowText;
            }
            else
            {
                throw new Exception("δ֪��image type");
            }
        }

        static System.Drawing.Color GetItemBackColor(int nType)
        {
            if (nType == TYPE_ERROR)
            {
                return System.Drawing.Color.Red;
            }
            else if (nType == TYPE_CHANGED)
            {
                return System.Drawing.Color.LightYellow;
            }
            else if (nType == TYPE_NORMAL)
            {
                return System.Drawing.SystemColors.Window;
            }
            else
            {
                throw new Exception("δ֪��image type");
            }

        }

        // ��������ı�����ǰ����ɫ����ͼ��
        static void SetItemColor(ListViewItem item,
            int nType)
        {
            item.BackColor = GetItemBackColor(nType);
            item.ForeColor = GetItemForeColor(nType);
            item.ImageIndex = nType;

            // ˳������ñ�����ǰ��data.Changedֵ�Ƿ���ȷ
#if DEBUG
            {
                if (item.Tag is OriginItemData
                    && nType != TYPE_ERROR)
                {
                    OriginItemData data = (OriginItemData)item.Tag;

                    Debug.Assert(data != null, "");

                    if (data != null)
                    {
                        if (nType == TYPE_CHANGED)
                        {
                            Debug.Assert(data.Changed == true, "");
                        }
                        if (nType == TYPE_NORMAL)
                        {
                            Debug.Assert(data.Changed == false, "");
                        }
                    }
                }
            }
#endif

            /*
            if (nType == TYPE_ERROR)
            {
                item.ForeColor = Color.White;
                item.ImageIndex = TYPE_ERROR;
            }
            else if (nType == TYPE_CHANGED)
            {
                item.BackColor = Color.LightYellow;
                item.ForeColor = SystemColors.WindowText;
                item.ImageIndex = TYPE_CHANGED;
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
             * */

        }

        /*public*/ static ListViewItem AddToListView(
            string strPubType,
            bool bAccepted,
            ListView list,
            XmlDocument dom,
            string strRecPath,
            string strBiblioSummary,
            string strISBnISSN,
            string strBiblioRecPath)
        {
            ListViewItem item = new ListViewItem(strRecPath, TYPE_NORMAL);

            SetListViewItemText(
                strPubType,
                bAccepted,
                dom,
                false,
                strRecPath,
                strBiblioSummary,
                strISBnISSN,
                strBiblioRecPath,
                item);
            list.Items.Add(item);

            return item;
        }

        // ���ݶ�����¼DOM����ListViewItem����һ�����������
        // parameters:
        //      bSetBarcodeColumn   �Ƿ�Ҫ���õ�һ�м�¼·��������
        /*public*/ static void SetListViewItemText(
            string strPubType,
            bool bAccepted,
            XmlDocument dom,
            bool bSetRecPathColumn,
            string strRecPath,
            string strBiblioSummary,
            string strISBnISSN,
            string strBiblioRecPath,
            ListViewItem item)
        {
            int nRet = 0;
            OriginItemData data = null;
            data = (OriginItemData)item.Tag;
            if (data == null)
            {
                data = new OriginItemData();
                item.Tag = data;
            }
            else
            {
                data.Changed = false;   // 2008/9/5 new add
            }

            string strState = DomUtil.GetElementText(dom.DocumentElement,
                "state");
            string strCatalogNo = DomUtil.GetElementText(dom.DocumentElement,
                "catalogNo");
            string strSeller = DomUtil.GetElementText(dom.DocumentElement,
                "seller");
            string strSource = DomUtil.GetElementText(dom.DocumentElement,
                "source");
            string strRange = DomUtil.GetElementText(dom.DocumentElement,
                "range");
            string strIssueCount = DomUtil.GetElementText(dom.DocumentElement,
                "issueCount");

            // TODO: �Ƿ�ֻ�����������ַ������븴����?
            string strCopy = DomUtil.GetElementText(dom.DocumentElement,
                "copy");

            // TODO: �Ƿ�ֻ�������۷���۸���?
            string strPrice = DomUtil.GetElementText(dom.DocumentElement,
                "price");
            string strTotalPrice = DomUtil.GetElementText(dom.DocumentElement,
                "totalPrice");

            // 2010/12/8
            if (strState == "������" && bAccepted == false)
            {
                strBiblioSummary = "����¼״̬Ϊ '������'�������ٲ��붩����ӡ";
                SetItemColor(item,
                        TYPE_ERROR);
            }

            List<int> textchanged_columns = new List<int>();


            int nIssueCount = 1;
            if (strPubType == "����������")
            {
                try
                {
                    nIssueCount = Convert.ToInt32(strIssueCount);
                }
                catch (Exception ex)
                {
                    strBiblioSummary = "�������� '" + strIssueCount + "' ��ʽ����ȷ: " + ex.Message;
                    SetItemColor(item,
                            TYPE_ERROR);
                }
            }
            else
            {
                Debug.Assert(strPubType == "ͼ��", "");
            }

            {
                string strOldCopy = "";
                string strNewCopy = "";
                // ���� "old[new]" �ڵ�����ֵ
                OrderDesignControl.ParseOldNewValue(strCopy,
                    out strOldCopy,
                    out strNewCopy);
                // strCopy = strOldCopy;

                int nCopy = 0;
                try
                {
                    nCopy = Convert.ToInt32(OrderDesignControl.GetCopyFromCopyString(strOldCopy));
                }
                catch (Exception ex)
                {
                    strBiblioSummary = "������������ '" + strOldCopy + "' ��ʽ����ȷ: " + ex.Message;
                    SetItemColor(item,
                            TYPE_ERROR);
                }

                string strCurrentOldPrice = "";
                string strCurrentNewPrice = "";

                // ���� "old[new]" �ڵ�����ֵ
                OrderDesignControl.ParseOldNewValue(strPrice,
                    out strCurrentOldPrice,
                    out strCurrentNewPrice);

                string strCurrentPrice = strCurrentOldPrice;


                // ���ܼ۸�
                string strCurTotalPrice = "";
                string strError = "";

                // 2009/11/9 changed
                // ֻ��ԭʼ�������ܼ۸�Ϊ��ʱ�����б�Ҫ���ܼ۸�
                if (String.IsNullOrEmpty(strTotalPrice) == true)
                {
                    nRet = PriceUtil.MultiPrice(strCurrentPrice,
                        nCopy,
                        out strCurTotalPrice,
                        out strError);
                    if (nRet == -1)
                    {
                        strBiblioSummary = "�۸��ַ��� '" + strCurrentPrice + "' ��ʽ����ȷ: " + strError;
                        SetItemColor(item,
                                TYPE_ERROR);
                    }

                    // ����������
                    if (nIssueCount != 1)
                    {
                        Debug.Assert(strPubType == "����������", "");

                        string strTempPrice = strCurTotalPrice;
                        nRet = PriceUtil.MultiPrice(strTempPrice,
                            nIssueCount,
                            out strCurTotalPrice,
                            out strError);
                        if (nRet == -1)
                        {
                            strBiblioSummary = "�۸��ַ��� '" + strTempPrice + "' ��ʽ����ȷ: " + strError;
                            SetItemColor(item,
                                    TYPE_ERROR);
                        }
                    }

                    if (item.ImageIndex != TYPE_ERROR)
                    {
                        if (strTotalPrice != strCurTotalPrice)
                        {
                            strTotalPrice = "*" + strCurTotalPrice; // 2014/11/5
                            data.Changed = true;
                            SetItemColor(item,
                                TYPE_CHANGED); // ��ʾ�ܼ۸񱻸ı���

                            /*
                            // �Ӵ�����
                            item.SubItems[ORIGIN_COLUMN_TOTALPRICE].Font = 
                                new Font(item.SubItems[ORIGIN_COLUMN_TOTALPRICE].Font, FontStyle.Bold);
                             * */
                            textchanged_columns.Add(ORIGIN_COLUMN_TOTALPRICE);
                        }
                    }

                }


            }

            // �����޸� ״̬
            if (item.ImageIndex != TYPE_ERROR)  // 2009/11/23 new add
            {
                if (strState == "������")
                {
                    /*
                    strBiblioSummary = "����¼״̬Ϊ '������'�������ٲ��붩����ӡ";
                    SetItemColor(item,
                            TYPE_ERROR);
                     * */
                }
                else if (bAccepted == false)
                {
                    string strNewState = "�Ѷ���";

                    if (strState != strNewState)
                    {
                        strState = "*" + strNewState;   // 2014/11/5
                        data.Changed = true;
                        SetItemColor(item,
                            TYPE_CHANGED); // ��ʾ״̬���ı���

                        textchanged_columns.Add(ORIGIN_COLUMN_STATE);
                    }
                }
            }

            string strOrderTime = DomUtil.GetElementText(dom.DocumentElement,
                "orderTime");

            if (string.IsNullOrEmpty(strOrderTime) == false)
            {
                // ת��Ϊ����ʱ���ʽ 2009/1/5 new add
                try
                {
                    DateTime order_time = DateTimeUtil.FromRfc1123DateTimeString(strOrderTime);
                    strOrderTime = order_time.ToLocalTime().ToShortDateString();
                }
                catch (Exception /*ex*/)
                {
                    strOrderTime = "ʱ���ַ��� '" + strOrderTime + "' ��ʽ���󣬲���RFC1123��ʽ";
                }
            }

            string strOrderID = DomUtil.GetElementText(dom.DocumentElement,
                "orderID");
            string strDistribute = DomUtil.GetElementText(dom.DocumentElement,
                "distribute");
            string strClass = DomUtil.GetElementText(dom.DocumentElement,
                "class");
            string strComment = DomUtil.GetElementText(dom.DocumentElement,
                "comment");
            string strBatchNo = DomUtil.GetElementText(dom.DocumentElement,
                "batchNo");
            string strSellerAddress = DomUtil.GetElementInnerXml(dom.DocumentElement,
                "sellerAddress");

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_SUMMARY, strBiblioSummary);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ISBNISSN, strISBnISSN);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_STATE, strState);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_CATALOGNO, strCatalogNo);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_SELLER, strSeller);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_SOURCE, strSource);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_RANGE, strRange);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ISSUECOUNT, strIssueCount);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_COPY, strCopy);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_PRICE, strPrice);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_TOTALPRICE, strTotalPrice);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ORDERTIME, strOrderTime);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ORDERID, strOrderID);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_DISTRIBUTE, strDistribute);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_CLASS, strClass);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_COMMENT, strComment);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_BATCHNO, strBatchNo);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_SELLERADDRESS, strSellerAddress);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_BIBLIORECPATH, strBiblioRecPath);

            if (bSetRecPathColumn == true)
            {
                ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_RECPATH, strRecPath);
            }

            // �Ӵ�����
            for (int i = 0; i < textchanged_columns.Count; i++)
            {
                int index = textchanged_columns[i];
                item.SubItems[index].Font =
                    new System.Drawing.Font(item.SubItems[index].Font, FontStyle.Bold);
            }



            if (item.ImageIndex == TYPE_NORMAL)
                SetItemColor(item, TYPE_NORMAL);

        }

        // ���� ԭʼ����listview ����Ŀ����
        void CreateOriginColumnHeader(ListView list)
        {
            ColumnHeader columnHeader_recpath = new ColumnHeader();
            ColumnHeader columnHeader_errorInfo = new ColumnHeader();
            ColumnHeader columnHeader_isbnIssn = new ColumnHeader();
            ColumnHeader columnHeader_state = new ColumnHeader();
            ColumnHeader columnHeader_catalogNo = new ColumnHeader();
            ColumnHeader columnHeader_seller = new ColumnHeader();
            ColumnHeader columnHeader_source = new ColumnHeader();
            ColumnHeader columnHeader_range = new ColumnHeader();
            ColumnHeader columnHeader_issueCount = new ColumnHeader();
            ColumnHeader columnHeader_copy = new ColumnHeader();
            ColumnHeader columnHeader_price = new ColumnHeader();

            ColumnHeader columnHeader_totalPrice = new ColumnHeader();
            ColumnHeader columnHeader_orderTime = new ColumnHeader();
            ColumnHeader columnHeader_orderID = new ColumnHeader();
            ColumnHeader columnHeader_distribute = new ColumnHeader();

            ColumnHeader columnHeader_class = new ColumnHeader();
            ColumnHeader columnHeader_comment = new ColumnHeader();
            ColumnHeader columnHeader_batchNo = new ColumnHeader();
            ColumnHeader columnHeader_sellerAddress = new ColumnHeader();
            ColumnHeader columnHeader_biblioRecpath = new ColumnHeader();

            list.Columns.Clear();

            list.Columns.AddRange(new ColumnHeader[] {
            columnHeader_recpath,
            columnHeader_errorInfo,
            columnHeader_isbnIssn,
            columnHeader_state,
            columnHeader_catalogNo,
            columnHeader_seller,
            columnHeader_source,
            columnHeader_range,
            columnHeader_issueCount,
            columnHeader_copy,
            columnHeader_price,
            columnHeader_totalPrice,
            columnHeader_orderTime,
            columnHeader_orderID,
            columnHeader_distribute,
            columnHeader_class,
            columnHeader_comment,
            columnHeader_batchNo,
            columnHeader_sellerAddress,
            columnHeader_biblioRecpath});


            // 
            // columnHeader_recpath
            // 
            columnHeader_recpath.Text = "������¼·��";
            columnHeader_recpath.Width = 200;
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
            // columnHeader_catalogNo
            // 
            columnHeader_catalogNo.Text = "��Ŀ��";
            columnHeader_catalogNo.Width = 100;
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
            // columnHeader_range
            // 
            columnHeader_range.Text = "ʱ�䷶Χ";
            columnHeader_range.Width = 150;

            // 
            // columnHeader_issueCount
            // 
            columnHeader_issueCount.Text = "��������";
            columnHeader_issueCount.Width = 150;
            columnHeader_issueCount.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_copy
            // 
            columnHeader_copy.Text = "������";
            columnHeader_copy.Width = 150;
            columnHeader_copy.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_price
            // 
            columnHeader_price.Text = "����";
            columnHeader_price.Width = 150;
            columnHeader_price.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_totalPrice
            // 
            columnHeader_totalPrice.Text = "�ܼ�";
            columnHeader_totalPrice.Width = 150;
            columnHeader_totalPrice.TextAlign = HorizontalAlignment.Right;
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
            // columnHeader_distribute
            // 
            columnHeader_distribute.Text = "�ݲط���";
            columnHeader_distribute.Width = 150;

            // 
            // columnHeader_class
            // 
            columnHeader_class.Text = "���";
            columnHeader_class.Width = 100;

            // 
            // columnHeader_comment
            // 
            columnHeader_comment.Text = "��ע";
            columnHeader_comment.Width = 150;
            // 
            // columnHeader_batchNo
            // 
            columnHeader_batchNo.Text = "���κ�";
            columnHeader_batchNo.Width = 100;
            // 
            // columnHeader_sellerAddress
            // 
            columnHeader_sellerAddress.Text = "������ַ";
            columnHeader_sellerAddress.Width = 200;
            // 
            // columnHeader_biblioRecpath
            // 
            columnHeader_biblioRecpath.Text = "�ּ�¼·��";
            columnHeader_biblioRecpath.Width = 200;
        }

        // ���� �ϲ�������listview ����Ŀ����
        void CreateMergedColumnHeader(ListView list)
        {
            ColumnHeader columnHeader_seller = new ColumnHeader();
            ColumnHeader columnHeader_catalogNo = new ColumnHeader();
            ColumnHeader columnHeader_errorInfo = new ColumnHeader();
            ColumnHeader columnHeader_isbnIssn = new ColumnHeader();
            ColumnHeader columnHeader_mergeComment = new ColumnHeader();
            ColumnHeader columnHeader_range = new ColumnHeader();
            ColumnHeader columnHeader_issueCount = new ColumnHeader();
            ColumnHeader columnHeader_copy = new ColumnHeader();
            ColumnHeader columnHeader_subcopy = new ColumnHeader();
            ColumnHeader columnHeader_price = new ColumnHeader();
            ColumnHeader columnHeader_totalPrice = new ColumnHeader();
            ColumnHeader columnHeader_orderTime = new ColumnHeader();
            ColumnHeader columnHeader_orderID = new ColumnHeader();
            ColumnHeader columnHeader_distribute = new ColumnHeader();
            ColumnHeader columnHeader_acceptcopy = new ColumnHeader();
            ColumnHeader columnHeader_acceptsubcopy = new ColumnHeader();
            ColumnHeader columnHeader_acceptprice = new ColumnHeader();
            ColumnHeader columnHeader_class = new ColumnHeader();
            ColumnHeader columnHeader_comment = new ColumnHeader();
            ColumnHeader columnHeader_sellerAddress = new ColumnHeader();
            ColumnHeader columnHeader_biblioRecpath = new ColumnHeader();

            list.Columns.Clear();

            list.Columns.AddRange(new ColumnHeader[] {
            columnHeader_seller,
            columnHeader_catalogNo,
            columnHeader_errorInfo,
            columnHeader_isbnIssn,
            columnHeader_mergeComment,
            columnHeader_range,
            columnHeader_issueCount,
            columnHeader_copy,
            columnHeader_subcopy,
            columnHeader_price,
            columnHeader_totalPrice,
            columnHeader_orderTime,
            columnHeader_orderID,
            columnHeader_distribute,
            columnHeader_acceptcopy,
            columnHeader_acceptsubcopy,
            columnHeader_acceptprice,
            columnHeader_class,
            columnHeader_comment,
            columnHeader_sellerAddress,
            columnHeader_biblioRecpath});


            // 
            // columnHeader_seller
            // 
            columnHeader_seller.Text = "����";
            columnHeader_seller.Width = 150;
            // 
            // columnHeader_catalogNo
            // 
            columnHeader_catalogNo.Text = "��Ŀ��";
            columnHeader_catalogNo.Width = 100;
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
            // columnHeader_source
            // 
            columnHeader_mergeComment.Text = "�ϲ�ע��";
            columnHeader_mergeComment.Width = 150;
            // 
            // columnHeader_range
            // 
            columnHeader_range.Text = "ʱ�䷶Χ";
            columnHeader_range.Width = 150;
            // 
            // columnHeader_issueCount
            // 
            columnHeader_issueCount.Text = "��������";
            columnHeader_issueCount.Width = 150;
            columnHeader_issueCount.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_copy
            // 
            columnHeader_copy.Text = "������";
            columnHeader_copy.Width = 100;
            columnHeader_copy.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_subcopy
            // 
            columnHeader_subcopy.Text = "ÿ�ײ���";
            columnHeader_subcopy.Width = 100;
            columnHeader_subcopy.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_price
            // 
            columnHeader_price.Text = "����";
            columnHeader_price.Width = 150;
            columnHeader_price.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_totalPrice
            // 
            columnHeader_totalPrice.Text = "�ܼ�";
            columnHeader_totalPrice.Width = 150;
            columnHeader_totalPrice.TextAlign = HorizontalAlignment.Right;
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
            // columnHeader_distribute
            // 
            columnHeader_distribute.Text = "�ݲط���";
            columnHeader_distribute.Width = 150;
            // 
            // columnHeader_acceptcopy
            // 
            columnHeader_acceptcopy.Text = "�ѵ�����";
            columnHeader_acceptcopy.Width = 100;
            // 
            // columnHeader_acceptsubcopy
            // 
            columnHeader_acceptsubcopy.Text = "�ѵ�ÿ�ײ���";
            columnHeader_acceptsubcopy.Width = 100;
            // 
            // columnHeader_acceptprice
            // 
            columnHeader_acceptprice.Text = "���鵥��";
            columnHeader_acceptprice.Width = 100;
            // 
            // columnHeader_class
            // 
            columnHeader_class.Text = "���";
            columnHeader_class.Width = 100;
            // 
            // columnHeader_comment
            // 
            columnHeader_comment.Text = "��ע";
            columnHeader_comment.Width = 150;
            // 
            // columnHeader_sellerAddress
            // 
            columnHeader_sellerAddress.Text = "������ַ";
            columnHeader_sellerAddress.Width = 200;
            // 
            // columnHeader_biblioRecpath
            // 
            columnHeader_biblioRecpath.Text = "�ּ�¼·��";
            columnHeader_biblioRecpath.Width = 200;
        }


        private void button_next_Click(object sender, EventArgs e)
        {
            if (this.tabControl_main.SelectedTab == this.tabPage_load)
            {
                this.tabControl_main.SelectedTab = this.tabPage_saveChange;
                // this.textBox_verify_itemBarcode.Focus();
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_saveChange)
            {
                this.tabControl_main.SelectedTab = this.tabPage_print;
                this.button_print_printOrderList.Focus();
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
                MessageBox.Show(this, "�Ѿ������һ��page");
            }
            else
            {
                Debug.Assert(false, "δ֪��tabpage״̬");
            }

            // this.SetNextButtonEnable();
        }

        private void tabControl_main_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.tabControl_main.SelectedTab == this.tabPage_load)
            {
                this.SetNextButtonEnable();
                // this.button_next.Enabled = true;
                // ǿ����ʾ��ԭʼ�����б��Ա��û���ȷ�ع�������
                this.tabControl_items.SelectedTab = this.tabPage_originItems;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_saveChange)
            {
                if (this.m_bEnabled == true)
                {
                    this.SetNextButtonEnable();
                    // this.button_next.Enabled = true;

                    // ǿ����ʾ��ԭʼ�����б��Ա��û���ȷ�ع�������
                    this.tabControl_items.SelectedTab = this.tabPage_originItems;
                }
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
                this.SetNextButtonEnable();
                // this.button_next.Enabled = false;
            }
            else
            {
                Debug.Assert(false, "δ֪��tabpage״̬");
            }
        }

        /*public*/ class NamedListViewItems : List<ListViewItem>
        {
            public string Seller = "";
            // List<ListViewItem> Items = new List<ListViewItem>();
        }

        // ��������(����)����������ͬ��List<ListViewItem>
        /*public*/ class NamedListViewItemsCollection : List<NamedListViewItems>
        {
            public void AddItem(string strSeller,
                ListViewItem item)
            {
                NamedListViewItems list = null;
                bool bFound = false;

                // ��λ
                for(int i=0;i<this.Count;i++)
                {
                    list = this[i];
                    if (list.Seller == strSeller)
                    {
                        bFound = true;
                        break;
                    }
                }

                // ��������ڣ����´���һ��list
                if (bFound == false)
                {
                    list = new NamedListViewItems();
                    list.Seller = strSeller;
                    this.Add(list);
                }

                list.Add(item);
            }
        }

        // ��ӡ����
        private void button_print_printOrderList_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = PrintOrder("html", out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
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
                    new CellFormat(                                                                   // Index 5 - Alignment
                        new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center }
                    ) { /*FontId = 1, FillId = 0, BorderId = 0, */ApplyAlignment = true },
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 1, ApplyBorder = true }      // Index 6 - Border
                )
            ); // return
        }

        string ExportExcelFilename = "";

        // ��ӡ����
        // parameters:
        //      strStyle    excel / html ֮һ���߶���������ϡ� excel: ��� Excel �ļ�
        int PrintOrder(
            string strStyle,
            out string strError)
        {
            strError = "";

            int nErrorCount = 0;

            ExcelDocument doc = null;

            if (StringUtil.IsInList("excel", strStyle) == true)
            {
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
                    return 0;

                this.ExportExcelFilename = dlg.FileName;

#if NO
                // string filepath = Path.Combine(this.MainForm.UserDir, "test.xlsx");
                SpreadsheetDocument spreadsheetDocument = null;
                spreadsheetDocument = SpreadsheetDocument.Create(this.ExportExcelFilename, SpreadsheetDocumentType.Workbook);

                doc = new ExcelDocument(spreadsheetDocument);
#endif
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
                // doc.Initial();
            }

            this.tabControl_items.SelectedTab = this.tabPage_mergedItems;

            EnableControls(false);
            // MainForm.ShowProgress(true);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڹ��충�� ...");
            stop.BeginLoop();

            try
            {

                NamedListViewItemsCollection lists = new NamedListViewItemsCollection();

                // �ȼ���Ƿ��д������˳�㹹��item�б�
                // List<ListViewItem> items = new List<ListViewItem>();
                for (int i = 0; i < this.listView_merged.Items.Count; i++)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    ListViewItem item = this.listView_merged.Items[i];

                    if (item.ImageIndex == TYPE_ERROR)
                        nErrorCount++;

                    lists.AddItem(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                        item);
                }

                if (nErrorCount != 0)
                {
                    MessageBox.Show(this, "���棺�����ӡ�����嵥������ " + nErrorCount.ToString() + " ������������Ϣ�����");
                }

                List<string> filenames = new List<string>();
                try
                {
                    // ��������ӡ����
                    for (int i = 0; i < lists.Count; i++)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null && stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }

                        List<string> temp_filenames = null;
                        int nRet = PrintMergedList(lists[i],
                            ref doc,
                            out temp_filenames,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        filenames.AddRange(temp_filenames);
                    }

                    for (int i = 0; i < lists.Count; i++)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null && stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }

                        // ��������ӡ����ͳ��ҳ
                        List<string> temp_filenames = null;
                        int nRet = PrintClassStatisList(
                            "class",
                            lists[i],
                            ref doc,
                            out temp_filenames,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (temp_filenames == null || temp_filenames.Count == 0)
                            continue;

                        Debug.Assert(temp_filenames != null);
                        filenames.AddRange(temp_filenames);

                        // ��������ӡ������ͳ��ҳ
                        temp_filenames = null;
                        nRet = PrintClassStatisList(
                            "publisher",
                            lists[i],
                            ref doc,
                            out temp_filenames,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (temp_filenames == null || temp_filenames.Count == 0)
                            continue;

                        Debug.Assert(temp_filenames != null);
                        filenames.AddRange(temp_filenames);
                    }

                    if (doc == null)
                    {
                        HtmlPrintForm printform = new HtmlPrintForm();

                        printform.Text = "��ӡ����";
                        printform.MainForm = this.MainForm;
                        printform.Filenames = filenames;
                        this.MainForm.AppInfo.LinkFormState(printform, "printorder_htmlprint_formstate");
                        printform.ShowDialog(this);
                        this.MainForm.AppInfo.UnlinkFormState(printform);
                    }
                }
                finally
                {
                    if (filenames != null)
                    {
                        Global.DeleteFiles(filenames);
                        filenames.Clear();
                    }
                }
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

            END1:
            if (doc != null)
            {
                // Close the document.
                doc.Close();
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ��ӡһ�������ķ���ͳ�Ʊ�
        int PrintClassStatisList(
            string strStatisType,
            NamedListViewItems items,
            ref ExcelDocument doc,
            out List<string> filenames,
            out string strError)
        {
            strError = "";
            filenames = null;
            bool bError = true;


            // ����һ��html�ļ����Ա㺯�����غ���ʾ��HtmlPrintForm�С�

            try
            {
                // Debug.Assert(false, "");

                // ����htmlҳ��
                int nRet = BuildStatisHtml(
                    strStatisType,
                    items,
                    ref doc,
                    out filenames,
                    out strError);
                if (nRet == -1)
                    return -1;
                bError = false;
            }
            finally
            {
                // ������
                if (filenames != null && bError == true)
                {
                    Global.DeleteFiles(filenames);
                    filenames.Clear();
                }
            }

            return 0;
        }

        // ��ӡһ�������Ķ���
        int PrintMergedList(NamedListViewItems items,
            ref ExcelDocument doc,
            out List<string> html_filenames,
            out string strError)
        {
            strError = "";
            html_filenames = null;
            bool bError = true;

            // ����һ��html�ļ����Ա㺯�����غ���ʾ��HtmlPrintForm�С�

            try
            {
                // Debug.Assert(false, "");

                // ����htmlҳ��
                int nRet = BuildMergedHtml(
                    items,
                    ref doc,
                    out html_filenames,
                    out strError);
                if (nRet == -1)
                    return -1;
                bError = false;
            }
            finally
            {
                // ������
                if (html_filenames != null && bError == true)
                {
                    Global.DeleteFiles(html_filenames);
                    html_filenames.Clear();
                }
            }

            return 0;
        }

        private void button_merged_print_option_Click(object sender, EventArgs e)
        {
            // ���ñ���ͷ��
            string strNamePath = "printorder_printoption";

            PrintOrderPrintOption option = new PrintOrderPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);


            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.Text = this.comboBox_load_type.Text + " ���� ��ӡ����";
            dlg.PrintOption = option;
            dlg.DataDir = this.MainForm.DataDir;
            dlg.ColumnItems = new string[] {
                "no -- ���",
                "seller -- ����",
                "catalogNo -- ��Ŀ��",
                "summary -- ժҪ",
                "isbnIssn -- ISBN/ISSN",
                "mergeComment -- �ϲ�ע��",
                "range -- ʱ�䷶Χ",
                "issueCount -- ��������",
                "copy -- ������",
                                "subcopy -- ÿ�ײ���",
                                "series -- ����",

                "price -- ����",
                "totalPrice -- �ܼ۸�",
                "orderTime -- ����ʱ��",
                "orderID -- ������",
                "distribute -- �ݲط���",
                "acceptCopy -- �ѵ�����",  // 2012/8/29
                "class -- ���",

                "comment -- ע��",

                "sellerAddress -- ������ַ",
                "sellerAddress:zipcode -- ������ַ:��������",
                "sellerAddress:address -- ������ַ:��ַ",
                "sellerAddress:department -- ������ַ:��λ",
                "sellerAddress:name -- ������ַ:��ϵ��",
                "sellerAddress:tel -- ������ַ:�绰",
                "sellerAddress:email -- ������ַ:Email��ַ",
                "sellerAddress:bank -- ������ַ:������",
                "sellerAddress:accounts -- ������ַ:�����˺�",
                "sellerAddress:payStyle -- ������ַ:��ʽ",
                "sellerAddress:comment -- ������ַ:��ע",

                "biblioRecpath -- �ּ�¼·��"
            };


            this.MainForm.AppInfo.LinkFormState(dlg, "printorder_printoption_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            option.SaveData(this.MainForm.AppInfo,
                strNamePath);
        }

        // ������ű��ļ�װ�ص��ڴ�
        int LoadClassTable(string strFilename,
            out List<StatisLine> lines,
            out string strError)
        {
            lines = new List<StatisLine>();
            strError = "";

            try
            {
                using (StreamReader sr = new StreamReader(strFilename))
                {
                    for (int i=0; ; i++)
                    {
                        string strLine = "";
                        strLine = sr.ReadLine();

                        if (strLine == null)
                            break;

                        strLine = strLine.Trim();

                        if (String.IsNullOrEmpty(strLine) == true)
                            continue;

                        StatisLine line = new StatisLine();
                        if (strLine[0] == '!')
                        {
                            line.Class = strLine.Substring(1);
                            line.AllowSum = false;
                        }
                        else
                        {
                            line.Class = strLine;
                            line.AllowSum = true;
                        }
                        lines.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                strError = "��ȡ�ļ� '" + strFilename + "' ����" + ex.Message;
                    return -1;
            }

            return 0;
        }

#if NO
        public static void CreateDefaultClassFilterFile(string strFileName)
        {
            StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8);
            sw.WriteLine("using System;");
            sw.WriteLine("using System.Windows.Forms;");
            sw.WriteLine("using System.IO;");
            sw.WriteLine("using System.Text;");
            sw.WriteLine("using System.Xml;");
            sw.WriteLine("");

            //sw.WriteLine("using DigitalPlatform.MarcDom;");
            //sw.WriteLine("using DigitalPlatform.Statis;");
            sw.WriteLine("using dp2Circulation;");

            sw.WriteLine("using DigitalPlatform.Xml;");


            sw.WriteLine("public class MyOutputOrder : OutputOrder");

            sw.WriteLine("{");

            sw.WriteLine("	public override void Output()");
            sw.WriteLine("	{");
            sw.WriteLine("	}");


            sw.WriteLine("}");
            sw.Close();
        }
#endif

        static bool ComparePublisher(string strText1, string strText2)
        {
            if (strText1 != null)
                strText1 = strText1.ToLower().Replace(" ", "");

            if (strText2 != null)
                strText2 = strText2.ToLower().Replace(" ", "");

            return string.Compare(strText1, strText2) == 0;
        }

        // �÷����ƥ��ͳ�ƽ���С�
        // parameters:
        //      lines   ƥ��ģʽ��
        //      bExactMatch �Ƿ�ȷƥ�䡣��� == false����ʾǰ��һ��
        //      bPublisherMatch �Ƿ�Ϊ��������ƥ�䷽ʽ����ν��������ƥ�䷽ʽ���Ǻ��Կո񣬺��Դ�Сд
        // return:
        //      -1  ����
        //      >=0 ƥ���ϵ�����
        int MatchStatisLine(string strClass,
            List<StatisLine> lines,
            bool bExactMatch,
            bool bPublisherMatch,
            out List<StatisLine> results,
            out string strError)
        {
            strError = "";
            results = new List<StatisLine>();
            foreach (StatisLine line in lines)
            {
                if (line.Class == "*")
                {
                    results.Add(line);
                    continue;
                }

                if (bExactMatch == true)
                {
                    if (bPublisherMatch == true)
                    {
                        if (ComparePublisher(strClass, line.Class) == true)
                            results.Add(line);
                    }
                    else if (strClass == line.Class)
                    {
                        results.Add(line);
                    }
                }
                else
                {
                    if (bPublisherMatch == true)
                    {
                        if (StringUtil.HasHead(strClass.ToLower().Replace(" ", ""), line.Class.ToLower().Replace(" ", "")) == true)
                            results.Add(line);
                    } 
                    else if (StringUtil.HasHead(strClass, line.Class) == true)
                    {
                        results.Add(line);
                    }
                }
            }

            return results.Count;
        }

        // ����ƥ���� �������� ��һ��
        int MatchOtherLine(string strClass,
    List<StatisLine> lines,
    out List<StatisLine> results,
    out string strError)
        {
            strError = "";
            results = new List<StatisLine>();
            foreach (StatisLine line in lines)
            {
                if (strClass == line.Class)
                {
                    results.Add(line);
                }
            }

            return results.Count;
        }

        // �������ͳ��htmlҳ��
        // parameters:
        //      strStatisType   ͳ�Ʊ����� "class" "publisher"
        int BuildStatisHtml(
            string strStatisType,
            NamedListViewItems items,
            ref ExcelDocument doc,
            out List<string> filenames,
            out string strError)
        {
            strError = "";
            filenames = null;

            int nRet = 0;

            // ��ô�ӡ����
            PrintOrderPrintOption option = new PrintOrderPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                "printorder_printoption");

#if NO
            // ׼��һ��� MARC ������
            {
                string strMarcFilterFilePath = option.GetTemplatePageFilePath("MARC������");
                if (String.IsNullOrEmpty(strMarcFilterFilePath) == false)
                {
                    Debug.Assert(String.IsNullOrEmpty(strMarcFilterFilePath) == false, "");
                    nRet = PrepareMarcFilter(strMarcFilterFilePath, out strError);
                    if (nRet == -1)
                        return -1;
                }
            }
#endif

            List<StatisLine> main_lines = null; // ��Ҫģʽ������
            List<StatisLine> secondary_lines = null; // ��Ҫģʽ������

            string strStatisTypeName = "";
            if (strStatisType == "class")
            {
                // �۲�ģ���ļ��Ƿ����
                string strTemplateFilePath = option.GetTemplatePageFilePath("����ű�");
                if (String.IsNullOrEmpty(strTemplateFilePath) == true)
                    return 0;   // û�б�Ҫ��ӡ

                // ������ű��ļ�װ�ص��ڴ�
                nRet = LoadClassTable(strTemplateFilePath,
                    out main_lines,
                    out strError);
                if (nRet == -1)
                    return -1;
                strStatisTypeName = "�����";
            }
            else if (strStatisType == "publisher")
            {
                // �۲�ģ���ļ��Ƿ����
                string strTemplateFilePath = option.GetTemplatePageFilePath("�������");
                if (String.IsNullOrEmpty(strTemplateFilePath) == true)
                    return 0;   // û�б�Ҫ��ӡ

                // ����������ļ�װ�ص��ڴ�
                nRet = LoadClassTable(strTemplateFilePath,
                    out main_lines,
                    out strError);
                if (nRet == -1)
                    return -1;
                strStatisTypeName = "������";

                //
                // �۲��Ҫģ���ļ��Ƿ����
                string strSecondaryTemplateFilePath = option.GetTemplatePageFilePath("����ű�");
                if (String.IsNullOrEmpty(strSecondaryTemplateFilePath) == false)
                {
                    // ������ű��ļ�װ�ص��ڴ�
                    nRet = LoadClassTable(strSecondaryTemplateFilePath,
                        out secondary_lines,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

            }
            else
            {
                strError = "δ֪�� strStatisType ����  '" + strStatisType + "'";
                return -1;
            }

            // ׼�� publisher ��ʽ���ض��� MARC ������
            // ע�������Ϊ������;������һ�� MARC ����������Ҫ���� publisher �����Ҫ�󣬿ɲμ� default_getclass.fltx
            if (this.MarcFilter == null)
            {
                // 
                string strMarcFilterFilePath = option.GetTemplatePageFilePath("MARC������");
                if (String.IsNullOrEmpty(strMarcFilterFilePath) == true)
                {
                    /*
                    // ���û������MARC����������ʹ��һ��ȱʡ����֧��UNIMARC��USMARC����ȡ��ͼ����ŵĹ�����
                    strMarcFilterFilePath = PathUtil.MergePath(this.MainForm.DataDir, "~printorder_default_class_filter.fltx");
                    CreateDefaultClassFilterFile(strMarcFilterFilePath);
                     * */
                    strMarcFilterFilePath = PathUtil.MergePath(this.MainForm.DataDir, "default_getclass.fltx");
                }
                if (File.Exists(strMarcFilterFilePath) == false)
                {
                    strError = "MARC�������ļ� '" + strMarcFilterFilePath + "' �����ڣ�����" + strStatisTypeName + "ͳ��ҳʧ��";
                    return -1;
                }

                Debug.Assert(String.IsNullOrEmpty(strMarcFilterFilePath) == false, "");

                {
                    nRet = PrepareMarcFilter(strMarcFilterFilePath, out strError);
                    if (nRet == -1)
                        return -1;
                }
            }

            Hashtable macro_table = new Hashtable();
            macro_table["%batchno%"] = this.BatchNo; // ���κ�
            macro_table["%seller%"] = items.Seller; // ������
            macro_table["%date%"] = DateTime.Now.ToLongDateString();
            macro_table["%pageno%"] = "1";
            macro_table["%pagecount%"] = "1";

            macro_table["%recpathfilepath%"] = this.RecPathFilePath;
            string strRecPathFilename = Path.GetFileName(this.RecPathFilePath);
            macro_table["%recpathfilename%"] = strRecPathFilename;

            // ���κŻ��ļ��� ����겻�����жϺ�ѡ�ã�ֻ�úϲ�Ϊһ����
            macro_table["%batchno_or_recpathfilename%"] = String.IsNullOrEmpty(this.BatchNo) == true ? strRecPathFilename : this.BatchNo;

            string strCssUrl = GetAutoCssUrl(option, "printorder.css");
            string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";
            macro_table["%link%"] = strLink;

            string strResult = "";

            // ׼��ģ��ҳ
            string strStatisTemplateFilePath = "";
            string strSheetName = "";
            string strTableTitle = "";

            if (strStatisType == "class")
            {
                strTableTitle = "%date% %seller% ����ͳ�Ʊ�";
                if (this.checkBox_print_accepted.Checked == false)
                {
                    strSheetName = "����ͳ��ҳ";
                    strStatisTemplateFilePath = option.GetTemplatePageFilePath("����ͳ��ҳ");
                }
                else
                {
                    strSheetName = "����ͳ��ҳ(������)";    // ������ʹ�÷�����
                    strStatisTemplateFilePath = option.GetTemplatePageFilePath("����ͳ��ҳ[������]");
                }

                if (String.IsNullOrEmpty(strStatisTemplateFilePath) == true)
                {
                    if (this.checkBox_print_accepted.Checked == false)
                        strStatisTemplateFilePath = PathUtil.MergePath(this.MainForm.DataDir, "default_printorder_classstatis.template");
                    else
                        strStatisTemplateFilePath = PathUtil.MergePath(this.MainForm.DataDir, "default_printorder_classstatis_accept.template");
                }

            }
            else if (strStatisType == "publisher")
            {
                strTableTitle = "%date% %seller% ������ͳ�Ʊ�";
                if (this.checkBox_print_accepted.Checked == false)
                {
                    strSheetName = "������ͳ��ҳ";
                    strStatisTemplateFilePath = option.GetTemplatePageFilePath("������ͳ��ҳ");
                }
                else
                {
                    strSheetName = "������ͳ��ҳ(������)"; // ����ʹ�÷�����
                    strStatisTemplateFilePath = option.GetTemplatePageFilePath("������ͳ��ҳ[������]");
                }

                if (String.IsNullOrEmpty(strStatisTemplateFilePath) == true)
                {
                    if (this.checkBox_print_accepted.Checked == false)
                        strStatisTemplateFilePath = PathUtil.MergePath(this.MainForm.DataDir, "default_printorder_publisherstatis.template");
                    else
                        strStatisTemplateFilePath = PathUtil.MergePath(this.MainForm.DataDir, "default_printorder_publisherstatis_accept.template");
                }
            }

            strTableTitle = StringUtil.MacroString(macro_table,
    strTableTitle); 

            Debug.Assert(String.IsNullOrEmpty(strStatisTemplateFilePath) == false, "");

            if (File.Exists(strStatisTemplateFilePath) == false)
            {
                strError = strStatisTypeName + "ͳ��ģ���ļ� '" + strStatisTemplateFilePath + "' �����ڣ�����" + strStatisTypeName + "ͳ��ҳʧ��";
                return -1;
            }
            {
                // ����ģ���ӡ
                string strContent = "";
                // ���Զ�ʶ���ļ����ݵı��뷽ʽ�Ķ����ı��ļ�����ģ��
                // return:
                //      -1  ����
                //      0   �ļ�������
                //      1   �ļ�����
                nRet = Global.ReadTextFileContent(strStatisTemplateFilePath,
                    out strContent,
                    out strError);
                if (nRet == -1)
                    return -1;

                strResult = StringUtil.MacroString(macro_table,
                    strContent);
            }

            filenames = new List<string>();    // ÿҳһ���ļ�������������������ļ���

            // ��Ҫ�����ڲ�ͬ�������ļ���ǰ׺������
            string strFileName = this.MainForm.DataDir + "\\~printorder_" + items.GetHashCode().ToString() + "_" + strStatisType + "statis";

            filenames.Add(strFileName);

            Sheet sheet = null;
            if (doc != null)
                sheet = doc.NewSheet(strSheetName);
            
            bool bWiledMatched = false; // �Ƿ�������ͨ���

            stop.SetProgressValue(0);
            stop.SetProgressRange(0, items.Count);

            stop.SetMessage("���ڱ����ϲ��� ...");
            for (int i = 0; i < items.Count; i++)
            {
                Application.DoEvents();	// ���ý������Ȩ

                if (stop != null && stop.State != 0)
                {
                    strError = "�û��ж�";
                    return -1;
                }

                ListViewItem item = items[i];

                // ����ּ�¼�е���Ҫ�������ݡ�����Ż��߳�������
                string strKey = ""; // ��Ҫ��������
                string strSecondaryKey = "";    // ��Ҫ��������
                if (this.MarcFilter != null)
                {
                    string strMARC = "";
                    string strOutMarcSyntax = "";

                    // ���MARC��ʽ��Ŀ��¼
                    string strBiblioRecPath = ListViewUtil.GetItemText(item, MERGED_COLUMN_BIBLIORECPATH);
                    // ���MARC��ʽ��Ŀ��¼
                    // return:
                    //      -1  ����
                    //      0   �ռ�¼
                    //      1   �ɹ�
                    nRet = GetMarc(strBiblioRecPath,
                        out strMARC,
                        out strOutMarcSyntax,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    this.ColumnTable.Clear();   // �����һ��¼����ʱ���������

                    if (nRet != 0)
                    {
                        // ����filter�е�Record��ض���
                        nRet = this.MarcFilter.DoRecord(
                            null,
                            strMARC,
                            strOutMarcSyntax,
                            i,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        if (strStatisType == "class")
                            strKey = (string)this.ColumnTable["biblioclass"];
                        else if (strStatisType == "publisher")
                        {
                            strKey = (string)this.ColumnTable["bibliopublisher"];
                            strSecondaryKey = (string)this.ColumnTable["biblioclass"];
                        }
                    }

                    stop.SetProgressValue(i + 1);
                }

                // ƥ����
                List<StatisLine> results = null;
                // �÷����ƥ��ͳ�ƽ���С�
                // return:
                //      -1  ����
                //      >=0 ƥ���ϵ�����
                nRet = MatchStatisLine(strKey,
                    main_lines,
                    strStatisType == "class" ? false : true,
                    strStatisType == "class" ? false : true,
                    out results,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    // ���û�������κ���

                    // ��ͼ�ҵ� Class Ϊ ������������ 2013/1/8
                    nRet = MatchOtherLine("����",
                        main_lines,
                        out results,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                    {
                        StatisLine line = new StatisLine();
                        line.Class = "����";
                        main_lines.Add(line);

                        results.Add(line);
                    }
                }
                else
                {
                    if (results.Count == 1 && results[0].Class == "*")
                    {
                        StatisLine line = new StatisLine();
                        line.Class = strKey;
                        main_lines.Add(line);

                        results[0] = line;
                        bWiledMatched = true;
                    }
                }

                // ������Ҫ��
                List<StatisLine> new_results = new List<StatisLine>();
                if (secondary_lines != null)
                {

                    foreach (StatisLine line in results)
                    {
                        if (line.InnerLines == null)
                        {
                            line.InnerLines = new List<StatisLine>();
                            // �´���һ������
                            foreach (StatisLine l in secondary_lines)
                            {
                                StatisLine n = new StatisLine();
                                n.Class = l.Class;
                                line.InnerLines.Add(n);
                            }
                        }

                        List<StatisLine> temp_results = null;
                        // ƥ����
                        // �÷����ƥ��ͳ�ƽ���С�
                        // return:
                        //      -1  ����
                        //      >=0 ƥ���ϵ�����
                        nRet = MatchStatisLine(strSecondaryKey,
                            line.InnerLines,
                            false,
                            false,
                            out temp_results,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 0)
                        {
                            // ���û�������κ���

                            // ��ͼ�ҵ� Class Ϊ ������������ 2013/1/8
                            nRet = MatchOtherLine("����",
                                line.InnerLines,
                                out temp_results,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            if (nRet == 0)
                            {
                                StatisLine new_line = new StatisLine();
                                new_line.Class = "����";
                                line.InnerLines.Add(new_line);

                                temp_results.Add(new_line);
                            }
                        }
                        else
                        {
                            if (temp_results.Count == 1 && temp_results[0].Class == "*")
                            {
                                StatisLine new_line = new StatisLine();
                                new_line.Class = strKey;
                                line.InnerLines.Add(new_line);

                                temp_results[0] = new_line;
                                bWiledMatched = true;
                            }
                        }

                        new_results.AddRange(temp_results);
                    }


                }

                string strTotalPrice = ListViewUtil.GetItemText(item, MERGED_COLUMN_TOTALPRICE);
                string strCopy = ListViewUtil.GetItemText(item, MERGED_COLUMN_COPY);
                string strSubCopy = ListViewUtil.GetItemText(item, MERGED_COLUMN_SUBCOPY);

                string strAcceptCopy = ListViewUtil.GetItemText(item, MERGED_COLUMN_ACCEPTCOPY);
                string strAcceptSubCopy = ListViewUtil.GetItemText(item, MERGED_COLUMN_ACCEPTSUBCOPY);
                string strPrice = ListViewUtil.GetItemText(item, MERGED_COLUMN_PRICE);
                // string strAcceptPrice = ListViewUtil.GetItemText(item, MERGED_COLUMN_ACCEPTPRICE);

                // ����
                int nSeries = 0;
                Int32.TryParse(strCopy, out nSeries);
                int nAcceptSeries = 0;
                Int32.TryParse(strAcceptCopy, out nAcceptSeries);
                int nSubCopy = 1;
                Int32.TryParse(strSubCopy, out nSubCopy);
                int nAcceptSubCopy = 1;
                Int32.TryParse(strAcceptSubCopy, out nAcceptSubCopy);

                ////
                nRet = AddValue(
                    1,
                    nSeries,
                    nSeries * nSubCopy,
                    strTotalPrice,
                    1,
                    nAcceptSeries,
                    nAcceptSeries * nAcceptSubCopy,
                    strPrice,
                    results,
                    out strError);

                if (new_results.Count > 0)
                {
                    nRet = AddValue(
    1,
    nSeries,
    nSeries * nSubCopy,
    strTotalPrice,
    1,
    nAcceptSeries,
    nAcceptSeries * nAcceptSubCopy,
    strPrice,
    new_results,
    out strError);
                }
            }

            string strTableContent = "<table class='" + strStatisType + "statis'>";

            // ��Ŀ������
            {
                #region ��� HTML

                strTableContent += "<tr class='column'>";
                strTableContent += "<td class='class'>" + strStatisTypeName + "</td>";
                strTableContent += "<td class='bibliocount'>����</td>";
                strTableContent += "<td class='seriescount'>����</td>";
                strTableContent += "<td class='itemcount'>����</td>";
                strTableContent += "<td class='orderprice'>������</td>";
                if (this.checkBox_print_accepted.Checked == true)
                {
                    strTableContent += "<td class='accept_bibliocount'>�ѵ�����</td>";
                    strTableContent += "<td class='accept_seriescount'>�ѵ�����</td>";
                    strTableContent += "<td class='accept_itemcount'>�ѵ�����</td>";
                    strTableContent += "<td class='accept_orderprice'>�ѵ�������</td>";
                }

                #endregion

                #region ��� Excel

                if (doc != null)
                {
                    int nColIndex = 0;

                    List<string> cols = new List<string>();
                    cols.Add(strStatisTypeName);
                    cols.Add("����");
                    cols.Add("����");
                    cols.Add("����");
                    cols.Add("������");

                    if (this.checkBox_print_accepted.Checked == true)
                    {
                        cols.Add("�ѵ�����");
                        cols.Add("�ѵ�����");
                        cols.Add("�ѵ�����");
                        cols.Add("�ѵ�������");
                    }

                    // �������
                    doc.WriteExcelTitle(0,
    cols.Count,
    strTableTitle,
    5);

                    foreach (string s in cols)
                    {
                        doc.WriteExcelCell(
            2,
            nColIndex++,
            s,
            true);
                    }
                }

                #endregion
            }

            string strSumPrice = "";
            long lBiblioCount = 0;
            long lSeriesCount = 0;
            long lItemCount = 0;

            string strAcceptSumPrice = "";
            long lAcceptBiblioCount = 0;
            long lAcceptSeriesCount = 0;
            long lAcceptItemCount = 0;

            if (bWiledMatched == true)
            {
                main_lines.Sort(new CellStatisLineComparer());
            }

            // Ƕ�׵��ӱ�
            List<InnerTableLine> inner_tables = new List<InnerTableLine>();

            int nExcelLineIndex = 3;
            stop.SetMessage("�������ͳ��ҳ HTML ...");
            foreach (StatisLine line in main_lines)
            {
                if (line.Class == "*")
                    continue;

                string strCurrentPrices = "";
                // 2012/3/7
                // ������"-123.4+10.55-20.3"�ļ۸��ַ����鲢����
                nRet = PriceUtil.SumPrices(line.Price,
        out strCurrentPrices,
        out strError);
                if (nRet == -1)
                    strCurrentPrices = strError;

                string strAcceptCurrentPrices = "";

                if (this.checkBox_print_accepted.Checked == true)
                {
                    // ������"-123.4+10.55-20.3"�ļ۸��ַ����鲢����
                    nRet = PriceUtil.SumPrices(line.AcceptPrice,
            out strAcceptCurrentPrices,
            out strError);
                    if (nRet == -1)
                        strAcceptCurrentPrices = strError;
                }

                string strNoSumClass = "";
                if (line.AllowSum == false)
                    strNoSumClass = " nosum";
                else
                    strNoSumClass = " sum";

                #region ��� HTML

                strTableContent += "<tr class='content" + HttpUtility.HtmlEncode(strNoSumClass) + "'>";
                strTableContent += "<td class='class'>" + HttpUtility.HtmlEncode(line.Class) + "</td>";
                strTableContent += "<td class='bibliocount'>" + GetTdValueString(line.BiblioCount) + "</td>";
                strTableContent += "<td class='seriescount'>" + GetTdValueString(line.SeriesCount) + "</td>";
                strTableContent += "<td class='itemcount'>" + GetTdValueString(line.ItemCount) + "</td>";
                strTableContent += "<td class='orderprice'>" + HttpUtility.HtmlEncode(strCurrentPrices) + "</td>";
                if (this.checkBox_print_accepted.Checked == true)
                {
                    strTableContent += "<td class='accept_bibliocount'>" + GetTdValueString(line.AcceptBiblioCount) + "</td>";
                    strTableContent += "<td class='accept_seriescount'>" + GetTdValueString(line.AcceptSeriesCount) + "</td>";
                    strTableContent += "<td class='accept_itemcount'>" + GetTdValueString(line.AcceptItemCount) + "</td>";
                    strTableContent += "<td class='accept_orderprice'>" + HttpUtility.HtmlEncode(strAcceptCurrentPrices) + "</td>";
                }

                #endregion

                #region ��� Excel

                if (doc != null)
                {
                    int nColIndex = 0;
                    doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
line.Class,
true);
                    doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
line.BiblioCount.ToString());
                    doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
line.SeriesCount.ToString());
                    doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
line.ItemCount.ToString());
                    doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
strCurrentPrices);

                    if (this.checkBox_print_accepted.Checked == true)
                    {
                        doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
line.AcceptBiblioCount.ToString());
                        doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
line.AcceptSeriesCount.ToString());
                        doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
line.AcceptItemCount.ToString());
                        doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
strAcceptCurrentPrices);
                    }

                    nExcelLineIndex++;
                }

                #endregion

                if (secondary_lines != null
                    && line.InnerLines != null) // 2013/9/11
                {
                    InnerTableLine inner_line = new InnerTableLine();
                    inner_line.Key = line.Class;
                    inner_line.lines = line.InnerLines;
                    inner_line.AllowSum = line.AllowSum;
                    inner_tables.Add(inner_line);
                }

                strTableContent += "</tr>";

                if (line.AllowSum == true)
                {
                    strSumPrice = PriceUtil.JoinPriceString(strSumPrice,
    strCurrentPrices);
                    lBiblioCount += line.BiblioCount;
                    lSeriesCount += line.SeriesCount;
                    lItemCount += line.ItemCount;

                    if (this.checkBox_print_accepted.Checked == true)
                    {
                        strAcceptSumPrice = PriceUtil.JoinPriceString(strAcceptSumPrice,
                            strAcceptCurrentPrices);
                        lAcceptBiblioCount += line.AcceptBiblioCount;
                        lAcceptSeriesCount += line.AcceptSeriesCount;
                        lAcceptItemCount += line.AcceptItemCount;
                    }

                }
            }

            // ������
            #region ��� HTML
            {
                string strOutputPrice = "";
                nRet = PriceUtil.SumPrices(strSumPrice,
        out strOutputPrice,
        out strError);
                if (nRet == -1)
                    strOutputPrice = strError;

                strTableContent += "<tr class='totalize'>";
                strTableContent += "<td class='class'>�ϼ�</td>";
                strTableContent += "<td class='bibliocount'>" + GetTdValueString(lBiblioCount) + "</td>";
                strTableContent += "<td class='seriescount'>" + GetTdValueString(lSeriesCount) + "</td>";
                strTableContent += "<td class='itemcount'>" + GetTdValueString(lItemCount) + "</td>";
                strTableContent += "<td class='orderprice'>" + HttpUtility.HtmlEncode(strOutputPrice) + "</td>";

                if (this.checkBox_print_accepted.Checked == true)
                {
                    string strAcceptOutputPrice = "";
                    nRet = PriceUtil.SumPrices(strAcceptSumPrice,
            out strAcceptOutputPrice,
            out strError);
                    if (nRet == -1)
                        strAcceptOutputPrice = strError;

                    strTableContent += "<td class='accept_bibliocount'>" + GetTdValueString(lAcceptBiblioCount) + "</td>";
                    strTableContent += "<td class='accept_seriescount'>" + GetTdValueString(lAcceptSeriesCount) + "</td>";
                    strTableContent += "<td class='accept_itemcount'>" + GetTdValueString(lAcceptItemCount) + "</td>";
                    strTableContent += "<td class='accept_orderprice'>" + HttpUtility.HtmlEncode(strAcceptOutputPrice) + "</td>";
                }
            }
            #endregion

            #region ��� Excel
            if (doc != null)
            {
                string strOutputPrice = "";
                nRet = PriceUtil.SumPrices(strSumPrice,
        out strOutputPrice,
        out strError);
                if (nRet == -1)
                    strOutputPrice = strError;

                int nColIndex = 0;
                doc.WriteExcelCell(
    nExcelLineIndex,
    nColIndex++,
    "�ϼ�",
    true);
                doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
lBiblioCount.ToString());

                doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
lSeriesCount.ToString());

                doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
lItemCount.ToString());

                doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
strOutputPrice); 

                if (this.checkBox_print_accepted.Checked == true)
                {
                    string strAcceptOutputPrice = "";
                    nRet = PriceUtil.SumPrices(strAcceptSumPrice,
            out strAcceptOutputPrice,
            out strError);
                    if (nRet == -1)
                        strAcceptOutputPrice = strError;

                    doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
lAcceptBiblioCount.ToString());

                    doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
lAcceptSeriesCount.ToString());
                    doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
lAcceptItemCount.ToString());
                    doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
strAcceptOutputPrice); 
                }
            }
            #endregion

            strTableContent += "</tr>";
            strTableContent += "</table>";

            strResult = strResult.Replace("{table}", strTableContent);

            // ������
            StreamUtil.WriteText(strFileName,
                strResult);

            if (secondary_lines != null)
            {
                strResult = "";

                // ׼��ģ��ҳ
                strStatisTemplateFilePath = "";
                if (strStatisType == "publisher")
                {
                    if (this.checkBox_print_accepted.Checked == false)
                    {
                        strSheetName = "���������ͳ��ҳ";
                        strStatisTemplateFilePath = option.GetTemplatePageFilePath("���������ͳ��ҳ");
                    }
                    else
                    {
                        strSheetName = "���������ͳ��ҳ(������)"; // ����ʹ�÷�����
                        strStatisTemplateFilePath = option.GetTemplatePageFilePath("���������ͳ��ҳ[������]");
                    }

                    if (String.IsNullOrEmpty(strStatisTemplateFilePath) == true)
                    {
                        if (this.checkBox_print_accepted.Checked == false)
                            strStatisTemplateFilePath = PathUtil.MergePath(this.MainForm.DataDir, "default_printorder_publisherclassstatis.template");
                        else
                            strStatisTemplateFilePath = PathUtil.MergePath(this.MainForm.DataDir, "default_printorder_publisherclassstatis_accept.template");
                    }
                }


                Debug.Assert(String.IsNullOrEmpty(strStatisTemplateFilePath) == false, "");

                if (File.Exists(strStatisTemplateFilePath) == false)
                {
                    strError = strStatisTypeName + "ͳ��ģ���ļ� '" + strStatisTemplateFilePath + "' �����ڣ�����" + strStatisTypeName + "��Ƕ��ͳ��ҳʧ��";
                    return -1;
                }
                {
                    // ����ģ���ӡ
                    string strContent = "";
                    // ���Զ�ʶ���ļ����ݵı��뷽ʽ�Ķ����ı��ļ�����ģ��
                    // return:
                    //      -1  ����
                    //      0   �ļ�������
                    //      1   �ļ�����
                    nRet = Global.ReadTextFileContent(strStatisTemplateFilePath,
                        out strContent,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    strResult = StringUtil.MacroString(macro_table,
                        strContent);
                }

                if (doc != null)
                    sheet = doc.NewSheet(strSheetName);

                strTableTitle = "%date% %seller% ����������ͳ�Ʊ�";
                strTableTitle = StringUtil.MacroString(macro_table,
                    strTableTitle);

                // ����Ƕ�ױ��
                strTableContent = "";
                nRet = BuildSecondaryPage(
                    strStatisType,
                    strStatisTypeName,
                    inner_tables,
                    strTableTitle,
                    ref doc,
                    out strTableContent,
                    out strError);
                if (nRet == -1)
                    return -1;

                strResult = strResult.Replace("{table}", strTableContent);
                string strInnerFileName = this.MainForm.DataDir + "\\~printorder_" + items.GetHashCode().ToString() + "_" + strStatisType + "statis_inner";
                filenames.Add(strInnerFileName);
                StreamUtil.WriteText(strInnerFileName,
                    strResult);
            }
            return 0;
        }

        // ����Ƕ�ױ��
        // parameters:
        //      strTableTitle   ֻ�е���� Excel ʱ��Ҫ
        // return:
        //      -1  ����
        //      0   �ɹ�
        int BuildSecondaryPage(
            string strStatisType,
            string strStatisTypeName,
            List<InnerTableLine> table,
            string strTableTitle,
            ref ExcelDocument doc,
            out string strResult,
            out string strError)
        {
            strError = "";
            strResult = "";

            // int nTitleColCount = 0; // �б����е�����

            List<StatisLine> first_lines = null;    // ��һ�������������
            foreach(InnerTableLine line in table)
            {
                if (line.lines != null && line.lines.Count > 0)
                {
                    first_lines = line.lines;
                    break;
                }
            }

            if (first_lines == null)
                return 0;   // û���κ�����

            StringBuilder strTableContent = new StringBuilder(4096);
            strTableContent.Append("<table class='" + strStatisType + "extendstatis'>");

            // ��Ŀ������
            {
                strTableContent.Append("<tr class='column'>");
                strTableContent.Append("<td class='class'>" + strStatisTypeName + "</td>");

                // ��ű�����
                foreach(StatisLine line in first_lines)
                {
                    strTableContent.Append("<td class='columntitle' colspan='2'>" + HttpUtility.HtmlEncode(line.Class) + "</td>");
                }
            }


            #region ��� Excel

            if (doc != null)
            {
                int nColIndex = 0;

                // ������
                doc.WriteExcelTitle(0,
first_lines.Count * 2 + 1,
strTableTitle,
5);

                doc.WriteExcelCell(
    2,
    nColIndex++,
    strStatisTypeName,
    true);
                // ��ű�����
                foreach (StatisLine line in first_lines)
                {
                    int nFirstCol = nColIndex;
                    doc.WriteExcelCell(
    2,
    nColIndex++,
    line.Class,
    true,
    5); // ����
                    // �հ׵�Ԫ
                    doc.WriteExcelCell(
    2,
    nColIndex++,
    "",
    true);
                    // ��������Ԫ��������
                    doc.InsertMergeCell(
             2,
             nFirstCol,
             2);
                }

                // nTitleColCount = nColIndex;
            }
            #endregion

            List<StatisLine> sum_items = new List<StatisLine>();

            int nExcelLineIndex = 3;
            foreach (InnerTableLine inner_line in table)
            {
                if (inner_line.Key == "*")
                    continue;

                string strNoSumClass = "";
                if (inner_line.AllowSum == false)
                    strNoSumClass = " nosum";
                else
                    strNoSumClass = " sum";

                strTableContent.Append("<tr class='content" + HttpUtility.HtmlAttributeEncode(strNoSumClass) + "'>");
                strTableContent.Append("<td class='class'>" + HttpUtility.HtmlEncode(inner_line.Key) + "</td>");

                #region ��� Excel

                int nColIndex = 0;
                if (doc != null)
                {
                    doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
inner_line.Key,
true);
                }

                #endregion

                int i = 0;
                foreach (StatisLine line in inner_line.lines)
                {
                    strTableContent.Append("<td class='bibliocount'>" + GetTdValueString(line.BiblioCount) + "</td>");
                    strTableContent.Append("<td class='itemcount'>" + GetTdValueString(line.ItemCount) + "</td>");

                    #region ��� Excel

                    if (doc != null)
                    {
                        doc.WriteExcelCell(
    nExcelLineIndex,
    nColIndex++,
    line.BiblioCount.ToString());
                        doc.WriteExcelCell(
    nExcelLineIndex,
    nColIndex++,
    line.ItemCount.ToString());
                    }

                    #endregion

                    // ����
                    if (inner_line.AllowSum == true)
                    {
                        while (sum_items.Count < i + 1)
                        {
                            sum_items.Add(new StatisLine());
                        }
                        StatisLine sum_item = sum_items[i];
                        sum_item.BiblioCount += line.BiblioCount;
                        sum_item.ItemCount += line.ItemCount;
                    }

                    i++;
                }

                strTableContent.Append("</tr>");

                if (doc != null)
                    nExcelLineIndex++;
            }

            // ������
            {
                strTableContent.Append("<tr class='totalize'>");
                strTableContent.Append("<td class='class'>�ϼ�</td>");

                #region ��� Excel

                int nColIndex = 0;
                if (doc != null)
                {
                    doc.WriteExcelCell(
nExcelLineIndex,
nColIndex++,
"�ϼ�",
true);
                }

                #endregion
                foreach (StatisLine line in sum_items)
                {
                    strTableContent.Append("<td class='bibliocount'>" + GetTdValueString(line.BiblioCount) + "</td>");
                    strTableContent.Append("<td class='itemcount'>" + GetTdValueString(line.ItemCount) + "</td>");
                    #region ��� Excel
                    if (doc != null)
                    {
                        doc.WriteExcelCell(
    nExcelLineIndex,
    nColIndex++,
    line.BiblioCount.ToString());
                        doc.WriteExcelCell(
    nExcelLineIndex,
    nColIndex++,
    line.ItemCount.ToString());
                    }
                    #endregion
                }
                strTableContent.Append("</tr>");
            }

            strTableContent.Append("</tr>");
            strTableContent.Append("</table>");

            strResult = strTableContent.ToString();
            return 0;   //  nTitleColCount;
        }

        class InnerTableLine
        {
            public bool AllowSum = false;
            public string Key = ""; // �󷽱���
            public List<StatisLine> lines = null;   // �ӱ�
        }

        string BuilInnerLine(List<StatisLine> lines)
        {
            StringBuilder result = new StringBuilder(4096);
            foreach (StatisLine line in lines)
            {
                if (line.BiblioCount == 0)
                    continue;   // ѹ��û��ֵ����
                result.Append(line.Class + ":" + line.BiblioCount + ","
                    + line.SeriesCount + ","
                    + line.ItemCount + ","
                    + line.Price + ";");
            }

            return result.ToString();
        }

        int AddValue(
            int nBiblioCount,
            int nSeriesCount,
            int nItemCount,
            string strPrice,
            int nAcceptBiblioCount,
            int nAcceptSeriesCount,
            int nAcceptItemCount,
            string strAcceptPrice,
            List<StatisLine> results,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            foreach (StatisLine line in results)
            {
                if (line.Class == "*")
                    continue;

                line.BiblioCount += nBiblioCount;
                line.SeriesCount += nSeriesCount;
                line.ItemCount += nItemCount;
                line.Price = PriceUtil.JoinPriceString(line.Price,
                    strPrice);
                if (this.checkBox_print_accepted.Checked == true)
                {
                    if (nAcceptSeriesCount > 0)
                    {
                        line.AcceptBiblioCount += nAcceptBiblioCount;
                        line.AcceptSeriesCount += nAcceptSeriesCount;
                        line.AcceptItemCount += nAcceptItemCount;

                        string strTemp = "";
                        nRet = PriceUtil.MultiPrice(strAcceptPrice,
        nAcceptSeriesCount,
        out strTemp,
        out strError);
                        if (nRet != -1)
                        {
                            line.AcceptPrice = PriceUtil.JoinPriceString(line.AcceptPrice,
                                strTemp);
                        }
                        // TODO: �Ƿ񱨴�?
                    }
                }
            }

            return 0;
        }

        static string GetTdValueString(long v)
        {
            if (v == 0)
                return "&nbsp;";
            return v.ToString();
        }

        static string GetRatioString(double v1, double v2)
        {
            double ratio = v1 / v2;

            return String.Format("{0,3:N}", ratio * (double)100) + "%";
        }

        // ���������۸�ı���
        static string GetRatioString(string strPrice1, string strPrice2)
        {
            if (strPrice1.IndexOfAny(new char[] { '+', '-', '*' }) != -1)
                return "�޷��������";
            if (strPrice2.IndexOfAny(new char[] { '+', '-', '*' }) != -1)
                return "�޷��������";

            if (string.IsNullOrEmpty(strPrice1) == true)
            {
                if (string.IsNullOrEmpty(strPrice2) == false)
                    return "0.00%";

                strPrice1 = "0.00";
            }

            if (string.IsNullOrEmpty(strPrice2) == true)
                strPrice2 = "0.00";

            string strError = "";
            string strPrefix1 = "";
            string strValue1 = "";
            string strPostfix1 = "";
            int nRet = PriceUtil.ParsePriceUnit(strPrice1,
                out strPrefix1,
                out strValue1,
                out strPostfix1,
                out strError);
            if (nRet == -1)
                return "strPrice1 '"+strPrice1+"' ��ʽ����: " + strError;

            decimal value1 = 0;
            try
            {
                value1 = Convert.ToDecimal(strValue1);
            }
            catch
            {
                strError = "���� '" + strValue1 + "' ��ʽ����ȷ";
                return strError;
            }

            string strPrefix2 = "";
            string strValue2 = "";
            string strPostfix2 = "";
            nRet = PriceUtil.ParsePriceUnit(strPrice2,
                out strPrefix2,
                out strValue2,
                out strPostfix2,
                out strError);
            if (nRet == -1)
                return "strPrice2 '" + strPrice2 + "' ��ʽ����: " + strError;

            decimal value2 = 0;
            try
            {
                value2 = Convert.ToDecimal(strValue2);
            }
            catch
            {
                strError = "���� '" + strValue2 + "' ��ʽ����ȷ";
                return strError;
            }

            if (strPrefix1 != strPrefix2)
            {
                return "strPrice1 '"+strPrice1+"' �� strPrice2 '"+strPrice2+"' ��ǰ׺��һ�£��޷��������";
            }

            if (strPostfix1 != strPostfix2)
            {
                return "strPrice1 '" + strPrice1 + "' �� strPrice2 '" + strPrice2 + "' �ĺ�׺��һ�£��޷��������";
            }

            return String.Format("{0,3:N}", ((double)value1 / (double)value2) * (double)100) + "%";
        }

        static void WriteExcelLine(WorkbookPart wp,
            Worksheet ws,
            int nLineIndex,
            string strName,
            string strValue,
            bool bString)
        {
            ExcelUtil.UpdateValue(
                wp,
                ws,
                "A" + (nLineIndex + 1).ToString(),
                strName,
                0,
                true);
            ExcelUtil.UpdateValue(
                wp,
                ws,
                "B" + (nLineIndex + 1).ToString(),
                strValue,
                0,
                bString);
        }

        // ����htmlҳ��
        int BuildMergedHtml(
            NamedListViewItems items,
            ref ExcelDocument doc,
            out List<string> filenames,
            out string strError)
        {
            strError = "";
            filenames = new List<string>();    // ÿҳһ���ļ�������������������ļ���
            int nRet = 0;

            stop.SetMessage("���ڹ��충�� ...");

            Hashtable macro_table = new Hashtable();

            // ��ô�ӡ����
            PrintOrderPrintOption option = new PrintOrderPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                "printorder_printoption");

            // ׼��һ��� MARC ������
            {
                string strMarcFilterFilePath = option.GetTemplatePageFilePath("MARC������");
                if (String.IsNullOrEmpty(strMarcFilterFilePath) == false)
                {
                    Debug.Assert(String.IsNullOrEmpty(strMarcFilterFilePath) == false, "");
                    nRet = PrepareMarcFilter(strMarcFilterFilePath, out strError);
                    if (nRet == -1)
                        return -1;
                }
            }

            /*
            // ��鵱ǰ����״̬�Ͱ����ּ۸���֮���Ƿ����ì��
            if (bHasBiblioPriceColumn(option) == true)
            {

                if (this.SortColumns_in.Count != 0
                    && this.SortColumns_in[0].No == COLUMN_BIBLIORECPATH)
                {
                }
                else
                {
                    MessageBox.Show(this, "���棺��ӡ����Ҫ���ˡ��ּ۸��У�����ӡǰ���������δ�����ּ�¼·��������������ӡ���ġ��ּ۸������ݽ��᲻׼ȷ��\r\n\r\nҪ����������������ڴ�ӡǰ���������㡮�ּ�¼·���������⣬ȷ����������");
                }
            }*/


            // �����ҳ����
            int nTablePageCount = items.Count / option.LinesPerPage;
            if ((items.Count % option.LinesPerPage) != 0)
                nTablePageCount++;

            int nPageCount = nTablePageCount + 1;

            macro_table["%batchno%"] = this.BatchNo; // ���κ�
            macro_table["%seller%"] = items.Seller; // ������
            macro_table["%pagecount%"] = nPageCount.ToString();
            macro_table["%linesperpage%"] = option.LinesPerPage.ToString();
            macro_table["%date%"] = DateTime.Now.ToLongDateString();

            macro_table["%recpathfilepath%"] = this.RecPathFilePath;
            string strRecPathFilename = Path.GetFileName(this.RecPathFilePath);
            macro_table["%recpathfilename%"] = strRecPathFilename;

            // ���κŻ��ļ��� ����겻�����жϺ�ѡ�ã�ֻ�úϲ�Ϊһ����
            macro_table["%batchno_or_recpathfilename%"] = String.IsNullOrEmpty(this.BatchNo) == true ? strRecPathFilename : this.BatchNo;


            // filenames = new List<string>();    // ÿҳһ���ļ�������������������ļ���

            // ��Ҫ�����ڲ�ͬ�������ļ���ǰ׺������
            string strFileNamePrefix = this.MainForm.DataDir + "\\~printorder_" + items.GetHashCode().ToString() + "_";

            string strFileName = "";

            // ��ʼ�� Excel �ļ�
#if NO
            Sheets sheets = null;
            WorksheetPart worksheetPart = null;
            {
                // Add a WorkbookPart to the document.
                WorkbookPart workbookpart = spreadsheetDocument.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();

                // Add a WorksheetPart to the WorkbookPart.
                worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                // Add Sheets to the Workbook.
                sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
            }

            // Append a new worksheet and associate it with the workbook.
            Sheet sheet = new Sheet() { 
                Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = (UInt32)filenames.Count + 1,
                Name = "ͳ��ҳ" };
            sheets.Append(sheet);
#endif

            Sheet sheet = null;

            if (doc != null)
                sheet = doc.NewSheet("ͳ��ҳ");

            // �����Ϣҳ
            // TODO: Ҫ���ӡ�ͳ��ҳ��ģ�幦��
            {
                int nItemCount = items.Count;
                int nTotalSeries = GetMergedTotalSeries(items);
                int nTotalCopies = GetMergedTotalCopies(items);
                int nBiblioCount = GetMergedBiblioCount(items);
                string strTotalPrice = GetMergedTotalPrice(items);

                macro_table["%itemcount%"] = nItemCount.ToString(); // ������
                macro_table["%totalcopies%"] = nTotalCopies.ToString(); // �ܲ���
                macro_table["%totalseries%"] = nTotalSeries.ToString(); // ������
                macro_table["%bibliocount%"] = nBiblioCount.ToString(); // ����
                macro_table["%totalprice%"] = strTotalPrice;    // �ܼ۸� ����Ϊ������ֵļ۸�����̬

                // �����յ�
                int nAcceptItemCount = items.Count;
                int nAcceptTotalSeries = GetMergedAcceptTotalSeries(items);
                int nAcceptTotalCopies = GetMergedAcceptTotalCopies(items);
                int nAcceptBiblioCount = GetMergedAcceptBiblioCount(items);
                string strAcceptTotalPrice = GetMergedAcceptTotalPrice(items);

                macro_table["%accept_itemcount%"] = nAcceptItemCount.ToString(); // ������
                macro_table["%accept_totalcopies%"] = nAcceptTotalCopies.ToString(); // �ܲ���
                macro_table["%accept_totalseries%"] = nAcceptTotalSeries.ToString(); // ������
                macro_table["%accept_bibliocount%"] = nAcceptBiblioCount.ToString(); // ����
                macro_table["%accept_totalprice%"] = strAcceptTotalPrice;    // �ܼ۸� ����Ϊ������ֵļ۸�����̬

                // ������
                macro_table["%ratio_itemcount%"] = GetRatioString(nAcceptItemCount, nItemCount); // ������
                macro_table["%ratio_totalcopies%"] = GetRatioString(nAcceptTotalCopies, nTotalCopies); // �ܲ���
                macro_table["%ratio_totalseries%"] = GetRatioString(nAcceptTotalSeries, nTotalSeries); // ������
                macro_table["%ratio_bibliocount%"] = GetRatioString(nAcceptBiblioCount, nBiblioCount); // ����
                macro_table["%ratio_totalprice%"] = GetRatioString(strAcceptTotalPrice, strTotalPrice);    // �ܼ۸� ����Ϊ������ֵļ۸�����̬


                macro_table["%pageno%"] = "1";

                strFileName = strFileNamePrefix + "0" + ".html";

                filenames.Add(strFileName);

                BuildMergedPageTop(option,
                    macro_table,
                    strFileName,
                    false);

                // ������
                StreamUtil.WriteText(strFileName,
                    "<div class='seller'>����: " + HttpUtility.HtmlEncode(items.Seller) + "</div>");
                StreamUtil.WriteText(strFileName,
                    "<div class='bibliocount'>����: " + nBiblioCount.ToString() + "</div>");
                StreamUtil.WriteText(strFileName,
                   "<div class='seriescount'>����: " + nTotalSeries.ToString() + "</div>");  // 2009/1/5 changed
                StreamUtil.WriteText(strFileName,
                    "<div class='itemcount'>����: " + nTotalCopies.ToString() + "</div>");  // 2009/1/5 changed
                StreamUtil.WriteText(strFileName,
                    "<div class='totalprice'>�ܼ�: " + HttpUtility.HtmlEncode(strTotalPrice) + "</div>");

                int nLineIndex = 2;
                
                if (doc != null)
                {
                    BuildMergedExcelPageTop(option,
                        macro_table,
                        ref doc,
                        4,
                        false);

                    doc.WriteExcelLine(
                    nLineIndex++,
                    "����",
                    items.Seller);

                    doc.WriteExcelLine(
    nLineIndex++,
        "����",
        nBiblioCount.ToString());

                    doc.WriteExcelLine(
    nLineIndex++,
    "����",
    nTotalSeries.ToString());

                    doc.WriteExcelLine(
    nLineIndex++,
    "����",
    nTotalCopies.ToString());

                    doc.WriteExcelLine(
    nLineIndex++,
    "�ܼ�",
    strTotalPrice);
                }

                if (this.checkBox_print_accepted.Checked == true)
                {
                    StreamUtil.WriteText(strFileName,
    "<div class='accept_bibliocount'>����������: " + nAcceptBiblioCount.ToString() + "</div>");
                    StreamUtil.WriteText(strFileName,
                       "<div class='accept_seriescount'>����������: " + nAcceptTotalSeries.ToString() + "</div>");  // 2009/1/5 changed
                    StreamUtil.WriteText(strFileName,
                        "<div class='accept_itemcount'>�����ղ���: " + nAcceptTotalCopies.ToString() + "</div>");  // 2009/1/5 changed
                    StreamUtil.WriteText(strFileName,
                        "<div class='accept_totalprice'>�������ܼ�: " + HttpUtility.HtmlEncode(strAcceptTotalPrice) + "</div>");

                    // ������
                    StreamUtil.WriteText(strFileName,
"<div class='ratio_bibliocount'>����������: " + HttpUtility.HtmlEncode((string)macro_table["%ratio_bibliocount%"]) + "</div>");
                    StreamUtil.WriteText(strFileName,
                       "<div class='ratio_seriescount'>����������: " + HttpUtility.HtmlEncode((string)macro_table["%ratio_totalseries%"]) + "</div>");  // 2009/1/5 changed
                    StreamUtil.WriteText(strFileName,
                        "<div class='ratio_itemcount'>����������: " + HttpUtility.HtmlEncode((string)macro_table["%ratio_totalcopies%"]) + "</div>");  // 2009/1/5 changed
                    StreamUtil.WriteText(strFileName,
                        "<div class='ratio_totalprice'>�ܼ۵�����: " + HttpUtility.HtmlEncode((string)macro_table["%ratio_totalprice%"]) + "</div>");

                    if (doc != null)
                    {
                        doc.WriteExcelLine(
nLineIndex++,
"����������",
nAcceptBiblioCount.ToString());
                        doc.WriteExcelLine(
nLineIndex++,
"����������",
nAcceptTotalSeries.ToString());
                        doc.WriteExcelLine(
nLineIndex++,
"�����ղ���",
nAcceptTotalCopies.ToString());
                        doc.WriteExcelLine(
nLineIndex++,
"�������ܼ�",
strAcceptTotalPrice);

                        doc.WriteExcelLine(
nLineIndex++,
"����������",
(string)macro_table["%ratio_bibliocount%"]);

                        doc.WriteExcelLine(
nLineIndex++,
"����������",
(string)macro_table["%ratio_totalseries%"]);

                        doc.WriteExcelLine(
nLineIndex++,
"����������",
(string)macro_table["%ratio_totalcopies%"]);
                        doc.WriteExcelLine(
nLineIndex++,
"�ܼ۵�����",
(string)macro_table["%ratio_totalprice%"]);
                    }
                }

                BuildMergedPageBottom(option,
                    macro_table,
                    strFileName,
                    false);
            }

            if (doc != null)
            {
                sheet = doc.NewSheet("����"); // "��1"
                BuildMergedExcelPageTop(option,
    macro_table,
    ref doc,
    option.Columns.Count,
    true);
            }

            // ���ҳѭ��
            for (int i = 0; i < nTablePageCount; i++)
            {
                Application.DoEvents();	// ���ý������Ȩ

                if (stop != null && stop.State != 0)
                {
                    strError = "�û��ж�";
                    return -1;
                }

#if NO
                // Append a new worksheet and associate it with the workbook.
                sheet = new Sheet()
                {
                    Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = (UInt32)filenames.Count + 1,
                    Name = "��" + (i+1).ToString()
                };
                sheets.Append(sheet);
#endif


#if NO
                SheetInfo sheetinfo = new SheetInfo();
                sheetinfo.wp = spreadsheetDocument.WorkbookPart;
                sheetinfo.ws = worksheetPart.Worksheet;
#endif

                macro_table["%pageno%"] = (i + 1 + 1).ToString();

                strFileName = strFileNamePrefix + (i + 1).ToString() + ".html";

                filenames.Add(strFileName);

                BuildMergedPageTop(option,
                    macro_table,
                    strFileName,
                    true);

                // ��ѭ��
                for (int j = 0; j < option.LinesPerPage; j++)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }

                    BuildMergedTableLine(option,
                        items,
                        strFileName,
                        doc,    // sheetinfo,
                        i, j, 3);
                }

                BuildMergedPageBottom(option,
                    macro_table,
                    strFileName,
                    true);
            }

            return 0;
        }

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

        // �������ֻ��class��
        // ��� "llll -- rrrrr"����߲��֣����Ұ���߲��ֿ����еĲ������ֶ���
        /// <summary>
        /// �������ֻ�� class ����
        /// �㷨Ϊ��� "llll -- rrrrr" ����߲��֣����Ұ���߲��ֿ����еĲ������ֶ���
        /// �����������㷨�� "name(param1, param2)" --&gt; "name"
        /// </summary>
        /// <param name="strText">Ҫ������ַ���</param>
        /// <returns>class ������</returns>
        public static string GetClass(string strText)
        {
            string strLeft = StringUtil.GetLeft(strText);
            string strName = "";
            string strParameters = "";
            ParseNameParam(strLeft,
                out strName,
                out strParameters);
            return strName;
        }

        // ���� name(param1, param2) �������ַ���
        /// <summary>
        /// ���� name(param1, param2) �������ַ���
        /// </summary>
        /// <param name="strText">Ҫ�������ַ���</param>
        /// <param name="strName">���� name ����</param>
        /// <param name="strParameters">���� (param1, param2) ����</param>
        public static void ParseNameParam(string strText,
            out string strName,
            out string strParameters)
        {
            strName = "";
            strParameters = "";

            int nRet = strText.IndexOf("(");
            if (nRet == -1)
            {
                strName = strText;
                return;
            }
            strName = strText.Substring(0, nRet).Trim();
            strParameters = strText.Substring(nRet + 1).Trim();
            nRet = strParameters.LastIndexOf(")");
            if (nRet == -1)
            {
                strName = strText;
                return;
            }
            strParameters = strParameters.Substring(0, nRet).Trim();
        }

        // ��� Excel ҳ��ͷ����Ϣ
        int BuildMergedExcelPageTop(PrintOption option,
            Hashtable macro_table,
            ref ExcelDocument doc,
            int nTitleCols,
            bool bOutputTable)
        {

            // ҳü
            string strPageHeaderText = option.PageHeader;

            if (String.IsNullOrEmpty(strPageHeaderText) == false)
            {
                strPageHeaderText = StringUtil.MacroString(macro_table,
                    strPageHeaderText);

                /*
                StreamUtil.WriteText(strFileName,
                    "<div class='pageheader'>" + HttpUtility.HtmlEncode(strPageHeaderText) + "</div>");
                */
            }

            // ������
            string strTableTitleText = option.TableTitle;

            // ��һ�У�������
            if (String.IsNullOrEmpty(strTableTitleText) == false)
            {

                strTableTitleText = StringUtil.MacroString(macro_table,
                    strTableTitleText);

                doc.WriteExcelTitle(0,
                    nTitleCols,
                    strTableTitleText,
                    5);

            }

            if (bOutputTable == true)
            {
                for (int i = 0; i < option.Columns.Count; i++)
                {
                    Column column = option.Columns[i];

                    string strCaption = column.Caption;

                    // ���û��caption���壬��Ų��name����
                    if (String.IsNullOrEmpty(strCaption) == true)
                        strCaption = column.Name;

                    string strClass = GetClass(column.Name);

                    if (this.checkBox_print_accepted.Checked == false)
                    {
                        // ����� �ѵ����� ��
                        if (strClass == "acceptCopy")
                            continue;
                    }

                    doc.WriteExcelCell(
            2,
            i,
            strCaption,
            true);
                }
            }

            return 0;
        }

        int BuildMergedPageTop(PrintOption option,
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

            // string strCssUrl = this.MainForm.LibraryServerDir + "/printorder.css";
            // 2009/10/10 changed
            string strCssUrl = GetAutoCssUrl(option, "printorder.css");

            /*
            // 2009/10/9 new add
            string strCssFilePath = option.GetTemplatePageFilePath("css");  // ��Сд������
            if (String.IsNullOrEmpty(strCssFilePath) == false)
                strCssUrl = strCssFilePath;
            else
                strCssUrl = this.MainForm.LibraryServerDir + "/printorder.css";    // ȱʡ��
             * */

            string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";

            StreamUtil.WriteText(strFileName,
                "<!DOCTYPE html PUBLIC \" -//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">"
                + "<html><head>" + strLink + "</head><body>");


            // ҳü
            string strPageHeaderText = option.PageHeader;

            if (String.IsNullOrEmpty(strPageHeaderText) == false)
            {
                strPageHeaderText = StringUtil.MacroString(macro_table,
                    strPageHeaderText);

                StreamUtil.WriteText(strFileName,
                    "<div class='pageheader'>" + HttpUtility.HtmlEncode(strPageHeaderText) + "</div>");

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
                    "<div class='tabletitle'>" + HttpUtility.HtmlEncode(strTableTitleText) + "</div>");
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

                    string strClass = GetClass(column.Name);

                    if (this.checkBox_print_accepted.Checked == false)
                    {
                        // ����� �ѵ����� ��
                        if (strClass == "acceptCopy")
                            continue;
                    }

                    StreamUtil.WriteText(strFileName,
                        "<td class='" + strClass + "'>" + HttpUtility.HtmlEncode(strCaption) + "</td>");
                }

                StreamUtil.WriteText(strFileName,
                    "</tr>");

            }

            return 0;
        }

#if NO
        class SheetInfo
        {
            public WorkbookPart wp = null;
            public Worksheet ws = null;
        }
#endif

        // parameters:
        //      nTopBlankLines  Excel ҳ���Ϸ������Ŀհ��������հ������ڱ������������
        int BuildMergedTableLine(PrintOption option,
            List<ListViewItem> items,
            string strFileName,
            // SheetInfo sheetinfo,
            ExcelDocument doc,
            int nPage,
            int nLine,
            int nTopBlankLines)
        {
            string strHtmlLineContent = "";
            int nRet = 0;

            int nIndex = nPage * option.LinesPerPage + nLine;

            if (nIndex >= items.Count)
                goto END1;

            ListViewItem item = items[nIndex];

            if (this.MarcFilter != null)
            {
                string strError = "";
                string strMARC = "";
                string strOutMarcSyntax = "";

                // TODO: �д���Ҫ���Ա����������������ڴ�ӡ������ŷ��֣�������

                // ���MARC��ʽ��Ŀ��¼
                string strBiblioRecPath = ListViewUtil.GetItemText(item, MERGED_COLUMN_BIBLIORECPATH);

                nRet = GetMarc(strBiblioRecPath,
                    out strMARC,
                    out strOutMarcSyntax,
                    out strError);
                if (nRet == -1)
                {
                    strHtmlLineContent = strError;
                    goto END1;
                }

                this.ColumnTable.Clear();   // �����һ��¼����ʱ���������

                // ����filter�е�Record��ض���
                nRet = this.MarcFilter.DoRecord(
                    null,
                    strMARC,
                    strOutMarcSyntax,
                    nIndex,
                    out strError);
                if (nRet == -1)
                {
                    strHtmlLineContent = strError;
                    goto END1;
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

                string strContent = GetMergedColumnContent(item,
                    column.Name);

                if (strContent == "!!!#")
                    strContent = ((nPage * option.LinesPerPage) + nLine + 1).ToString();

                /*
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
                 * */

                // �ض��ַ���
                if (column.MaxChars != -1)
                {
                    if (strContent.Length > column.MaxChars)
                    {
                        strContent = strContent.Substring(0, column.MaxChars);
                        strContent += "...";
                    }
                }

                string strClass = GetClass(column.Name);

                if (this.checkBox_print_accepted.Checked == false)
                {
                    // ����� �ѵ����� ��
                    if (strClass == "acceptCopy")
                        continue;
                }

                if (doc != null)
                {
                    int nLineIndex = (nPage * option.LinesPerPage) + nLine;
                    ExcelUtil.UpdateValue(
                        doc.workbookpart,
                        doc.worksheetPart.Worksheet,
                        ExcelDocument.GetCellName(i, nLineIndex + nTopBlankLines),
                        strContent,
                        0,
                        StringUtil.IsNumber(strContent) ? false : true);
                }

                if (String.IsNullOrEmpty(strContent) == true)
                    strContent = "&nbsp;";
                else
                    strContent = HttpUtility.HtmlEncode(strContent).Replace("\r\n", "<br/>");

                strHtmlLineContent +=
                    "<td class='" + strClass + "'>" + strContent + "</td>";
            }

            END1:

            StreamUtil.WriteText(strFileName,
    "<tr class='content'>");

            /* ���Ҫ��ӡ����
            if (string.IsNullOrEmpty(strLineContent) == true)
            {
                strLineContent += "<td class='blank' colspan='" + option.Columns.Count.ToString() + "'>&nbsp;</td>";
            }
             * */
            StreamUtil.WriteText(strFileName,
                strHtmlLineContent);

            StreamUtil.WriteText(strFileName,
                "</tr>");

            return 0;
        }

        // �����Ŀ����(�ϲ���)
        string GetMergedColumnContent(ListViewItem item,
            string strColumnName)
        {
            // ȥ��"-- ?????"����
            /*
            string strText = strColumnName;
            int nRet = strText.IndexOf("--", 0);
            if (nRet != -1)
                strText = strText.Substring(0, nRet).Trim();
             * */

            string strName = "";
            string strParameters = "";
            ParseNameParam(StringUtil.GetLeft(strColumnName),
                out strName,
                out strParameters);

            // 2013/3/29
            // Ҫ��ColumnTableֵ
            if (strName.Length > 0 && strName[0] == '@')
            {
                strName = strName.Substring(1);
                return (string)this.ColumnTable[strName];
            }

            try
            {
                // TODO: ��Ҫ�޸�
                // Ҫ��Ӣ�Ķ�����
                switch (strName)
                {
                    case "no":
                    case "���":
                        return "!!!#";  // ����ֵ����ʾ���

                    case "seller":
                    case "����":
                    case "����":
                        return item.SubItems[MERGED_COLUMN_SELLER].Text;

                    case "catalogNo":
                    case "��Ŀ��":
                        return item.SubItems[MERGED_COLUMN_CATALOGNO].Text;

                    case "errorInfo":
                    case "summary":
                    case "ժҪ":
                        return item.SubItems[MERGED_COLUMN_SUMMARY].Text;

                    case "isbnIssn":
                    case "ISBN/ISSN":
                        return item.SubItems[MERGED_COLUMN_ISBNISSN].Text;

                    case "mergeComment":
                    case "�ϲ�ע��":
                        return item.SubItems[MERGED_COLUMN_MERGECOMMENT].Text;



                        // û��recpath��¼·������Ϊrecpath�Ѿ����롰�ϲ�ע�͡���
                        // û��state״̬����Ϊstate����ȫ������Ϊ���Ѷ�����
                        // û��source������Դ����Ϊ�Ѿ����롰�ϲ�ע�͡���
                        // û��batchNo���κţ���Ϊԭʼ�����Ѿ��ϲ������ԭʼ���һ��������ͬ�����κ�

                    case "range":
                    case "ʱ�䷶Χ":
                        return item.SubItems[MERGED_COLUMN_RANGE].Text;

                    case "issueCount":
                    case "��������":
                        return item.SubItems[MERGED_COLUMN_ISSUECOUNT].Text;

                    case "series":
                    case "����":
                        {
                            string strCopy = item.SubItems[MERGED_COLUMN_COPY].Text;
                            string strSubCopy = item.SubItems[MERGED_COLUMN_SUBCOPY].Text;
                            if (String.IsNullOrEmpty(strSubCopy) == true)
                                return strCopy;

                            return strCopy + "(ÿ�׺� " + strSubCopy + " ��)";
                        }

                    case "copy":
                    case "������":
                        return item.SubItems[MERGED_COLUMN_COPY].Text;

                    case "subcopy":
                    case "ÿ�ײ���":
                        return item.SubItems[MERGED_COLUMN_SUBCOPY].Text;

                    case "price":
                    case "����":
                        return item.SubItems[MERGED_COLUMN_PRICE].Text;

                    case "totalPrice":
                    case "�ܼ۸�":
                        return item.SubItems[MERGED_COLUMN_TOTALPRICE].Text;

                    case "orderTime":
                    case "����ʱ��":
                        return item.SubItems[MERGED_COLUMN_ORDERTIME].Text;

                    case "orderID":
                    case "������":
                        return item.SubItems[MERGED_COLUMN_ORDERID].Text;

                    case "distribute":
                    case "�ݲط���":
                        return item.SubItems[MERGED_COLUMN_DISTRIBUTE].Text;

                    case "acceptCopy":
                    case "�ѵ�����":
                    case "�ѵ�������":
                        return item.SubItems[MERGED_COLUMN_ACCEPTCOPY].Text;

                    case "acceptSubCopy":
                    case "�ѵ�ÿ�ײ���":
                        return item.SubItems[MERGED_COLUMN_ACCEPTSUBCOPY].Text;

                    case "acceptPrice":
                    case "���鵥��":
                        return item.SubItems[MERGED_COLUMN_ACCEPTPRICE].Text;

                    case "class":
                    case "���":
                        return item.SubItems[MERGED_COLUMN_CLASS].Text;


                    case "comment":
                    case "ע��":
                    case "��ע":
                        return item.SubItems[MERGED_COLUMN_COMMENT].Text;

                    case "biblioRecpath":
                    case "�ּ�¼·��":
                        return item.SubItems[MERGED_COLUMN_BIBLIORECPATH].Text;

                    // ��ʽ���Ժ��������ַ
                    case "sellerAddress":
                    case "������ַ":
                        return GetPrintableSellerAddress(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            strParameters);

                    case "sellerAddress:zipcode":
                    case "������ַ:��������":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "zipcode");

                    case "sellerAddress:address":
                    case "������ַ:��ַ":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "address");

                    case "sellerAddress:department":
                    case "������ַ:��λ":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "department");

                    case "sellerAddress:name":
                    case "������ַ:��ϵ��":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "name");

                    case "sellerAddress:email":
                    case "������ַ:Email��ַ":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "email");

                    case "sellerAddress:bank":
                    case "������ַ:������":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "bank");

                    case "sellerAddress:accounts":
                    case "������ַ:�����˺�":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "accounts");

                    case "sellerAddress:payStyle":
                    case "������ַ:��ʽ":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "payStyle");

                    case "sellerAddress:comment":
                    case "������ַ:��ע":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "comment");
                    default:
                        {
                            // 2013/3/29
                            if (this.ColumnTable.Contains(strName) == false)
                                return "δ֪��Ŀ '" + strName + "'";

                            return (string)this.ColumnTable[strName];
                        }
                }
            }

            catch
            {
                return null;    // ��ʾû�����subitem�±�
            }

        }

        /// <summary>
        /// ���������ַ XML Ƭ���е���ǶԪ��ֵ
        /// </summary>
        /// <param name="strXmlFragment">������ַ XML Ƭ��</param>
        /// <param name="strSeller">������</param>
        /// <param name="strElementName">��ǶԪ����</param>
        /// <returns>��ǶԪ�ص�����ֵ</returns>
        public static string GetSellerAddressInnerValue(string strXmlFragment,
    string strSeller,
    string strElementName)
        {
            if (string.IsNullOrEmpty(strXmlFragment) == true)
                return "";

            XmlDocument dom1 = new XmlDocument();
            try
            {
                dom1.LoadXml("<root>" + strXmlFragment + "</root>");
            }
            catch (Exception ex)
            {
                return "������ַXML�ַ��� '" + strXmlFragment + "' ��ʽ����ȷ: " + ex.Message;
            }


            return DomUtil.GetElementText(dom1.DocumentElement, strElementName);
        }

        /// <summary>
        /// ���������ʾ��ӡ��������ַ�ַ���
        /// </summary>
        /// <param name="strXmlFragment">������ַ XML Ƭ��</param>
        /// <param name="strSeller">������</param>
        /// <param name="strParameters">Ҫɸѡ��Ԫ�����б����ż�����ַ���</param>
        /// <returns>������ʾ�ʹ�ӡ���ı����ݡ��лس����з���</returns>
        public static string GetPrintableSellerAddress(string strXmlFragment,
            string strSeller,
            string strParameters)
        {
            if (string.IsNullOrEmpty(strXmlFragment) == true)
                return "";

            XmlDocument dom1 = new XmlDocument();
            try
            {
                dom1.LoadXml("<root>" + strXmlFragment + "</root>");
            }
            catch (Exception ex)
            {
                return "������ַXML�ַ��� '" + strXmlFragment + "' ��ʽ����ȷ: " + ex.Message;
            }

            string[] elements = new string[] {
            "zipcode", "��������",
            "address", "��ַ",
            "department", "��λ",
            "name", "��ϵ��",
            "tel", "�绰",
            "email", "Email��ַ",
            "bank", "������",
            "accounts", "�����˺�",
            "payStyle", "��ʽ",
            "comment", "��ע"};

            StringBuilder result = new StringBuilder(4096);

            List<string> selectors = StringUtil.FromListString(strParameters);

            for (int i = 0; i < elements.Length / 2; i++)
            {
                string strElementName = elements[i * 2];
                string strCaption = elements[i * 2 + 1];

                // ����caption
                if (strSeller == "��")
                {
                    if (strElementName == "name")
                        strCaption = "������";
                }

                // ��Ԫ��������ɸѡ
                if (string.IsNullOrEmpty(strParameters) == false)
                {
                    if (selectors.IndexOf(strElementName) == -1)
                        continue;
                }

                string text = DomUtil.GetElementText(dom1.DocumentElement, strElementName);
                if (string.IsNullOrEmpty(text) == false)
                {
                    if (result.Length > 0)
                        result.Append("\r\n");
                    result.Append(strCaption + ": " + text);
                }
            }

            return result.ToString();
        }

        int BuildMergedPageBottom(PrintOption option,
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
        "<div class='pagefooter'>" + HttpUtility.HtmlEncode(strPageFooterText) + "</div>");
            }


            StreamUtil.WriteText(strFileName, "</body></html>");

            return 0;
        }

        // ����Ѿ�������������
        static int GetMergedBiblioCount(NamedListViewItems items)
        {
            List<string> paths = new List<string>();

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strText = "";

                try
                {
                    strText = item.SubItems[MERGED_COLUMN_BIBLIORECPATH].Text;
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

        // ����Ѿ���������һ�����ϵ�������
        static int GetMergedAcceptBiblioCount(NamedListViewItems items)
        {
            List<string> paths = new List<string>();

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                int nAcceptSeries = 0;

                try
                {
                    string strAcceptCopy = "";
                    strAcceptCopy = item.SubItems[MERGED_COLUMN_ACCEPTCOPY].Text;

                    // TODO: ע�����Ƿ���[]����?
                    nAcceptSeries = Convert.ToInt32(strAcceptCopy);
                }
                catch
                {
                    continue;
                }

                if (nAcceptSeries == 0)
                    continue;


                string strText = "";

                try
                {
                    strText = item.SubItems[MERGED_COLUMN_BIBLIORECPATH].Text;
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


        // ����Ѷ����ĸ�������
        static int GetMergedTotalCopies(NamedListViewItems items)
        {
            int total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                int nCopy = 0;

                try
                {
                    string strCopy = "";
                    strCopy = item.SubItems[MERGED_COLUMN_COPY].Text;

                    // TODO: ע�����Ƿ���[]����?
                    nCopy = Convert.ToInt32(strCopy);
                }
                catch
                {
                    continue;
                }

                int nSubCopy = 1;
                string strSubCopy = "";
                strSubCopy = item.SubItems[MERGED_COLUMN_SUBCOPY].Text;

                if (String.IsNullOrEmpty(strSubCopy) == false)
                {
                    try
                    {
                        nSubCopy = Convert.ToInt32(strSubCopy);
                    }
                    catch
                    {
                    }
                }

                total += nCopy*nSubCopy;
            }

            return total;
        }

        // ��������յĲ�������
        static int GetMergedAcceptTotalCopies(NamedListViewItems items)
        {
            int total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                int nAcceptSeries = 0;

                try
                {
                    string strAcceptCopy = "";
                    strAcceptCopy = item.SubItems[MERGED_COLUMN_ACCEPTCOPY].Text;

                    // TODO: ע�����Ƿ���[]����?
                    nAcceptSeries = Convert.ToInt32(strAcceptCopy);
                }
                catch
                {
                    continue;
                }

                int nSubCopy = 1;
                string strSubCopy = "";
                // strSubCopy = item.SubItems[MERGED_COLUMN_SUBCOPY].Text;
                strSubCopy = item.SubItems[MERGED_COLUMN_ACCEPTSUBCOPY].Text;

                if (String.IsNullOrEmpty(strSubCopy) == false)
                {
                    try
                    {
                        nSubCopy = Convert.ToInt32(strSubCopy);
                    }
                    catch
                    {
                    }
                }

                total += nAcceptSeries * nSubCopy;  // ����ȷ
            }

            return total;
        }

        // ����Ѷ�����������
        static int GetMergedTotalSeries(NamedListViewItems items)
        {
            int total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                int nCopy = 0;

                try
                {
                    string strCopy = "";
                    strCopy = item.SubItems[MERGED_COLUMN_COPY].Text;

                    // TODO: ע�����Ƿ���[]����?
                    nCopy = Convert.ToInt32(strCopy);
                }
                catch
                {
                    continue;
                }

                total += nCopy;
            }

            return total;
        }

        // ��������յ�������
        static int GetMergedAcceptTotalSeries(NamedListViewItems items)
        {
            int total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                int nCopy = 0;

                try
                {
                    string strAcceptCopy = "";
                    strAcceptCopy = item.SubItems[MERGED_COLUMN_ACCEPTCOPY].Text;

                    // TODO: ע�����Ƿ���[]����?
                    nCopy = Convert.ToInt32(strAcceptCopy);
                }
                catch
                {
                    continue;
                }

                total += nCopy;
            }

            return total;
        }


        /*
        static double GetMergedTotalPrice(NamedListViewItems items)
        {
            double total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strPrice = "";

                try
                {
                    strPrice = item.SubItems[MERGED_COLUMN_TOTALPRICE].Text;
                }
                catch
                {
                    continue;
                }

                if (String.IsNullOrEmpty(strPrice) == true)
                    continue;

                // ��ȡ��������
                string strPurePrice = Global.GetPurePrice(strPrice);

                if (String.IsNullOrEmpty(strPurePrice) == true)
                    continue;

                total += Convert.ToDouble(strPurePrice);
            }

            return total;
        }
         * */

        // ����Ѿ��������ܼ۸�
        static string GetMergedTotalPrice(NamedListViewItems items)
        {
            List<string> prices = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strPrice = "";

                try
                {
                    strPrice = item.SubItems[MERGED_COLUMN_TOTALPRICE].Text;
                }
                catch
                {
                    continue;
                }

                if (String.IsNullOrEmpty(strPrice) == true)
                    continue;

                prices.Add(strPrice);
            }

            List<string> results = null;
            string strError = "";
            // ���ܼ۸�
            // ���ҵ�λ��ͬ�ģ��������
            int nRet = PriceUtil.TotalPrice(prices,
                out results,
                out strError);
            if (nRet == -1)
                return strError;

            string strResult = "";
            for (int i = 0; i < results.Count; i++)
            {
                string strPrice = results[i];
                if (String.IsNullOrEmpty(strResult) == false)
                    strResult += "+";
                strResult += strPrice;
            }

            return strResult;
        }

        // ����Ѿ����յ��ܼ۸�
        static string GetMergedAcceptTotalPrice(NamedListViewItems items)
        {
            string strError = "";
            int nRet = 0;

            List<string> prices = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                int nAcceptSeries = 0;

                try
                {
                    string strAcceptCopy = "";
                    strAcceptCopy = item.SubItems[MERGED_COLUMN_ACCEPTCOPY].Text;

                    // TODO: ע�����Ƿ���[]����?
                    nAcceptSeries = Convert.ToInt32(strAcceptCopy);
                }
                catch
                {
                    continue;
                }

                if (nAcceptSeries == 0)
                    continue;

                string strPrice = "";

                try
                {
                    strPrice = item.SubItems[MERGED_COLUMN_ACCEPTPRICE].Text;
                }
                catch
                {
                    continue;
                }

                if (String.IsNullOrEmpty(strPrice) == true)
                    continue;

                {
                    string strTemp = "";
                    nRet = PriceUtil.MultiPrice(strPrice,
                        nAcceptSeries,
                        out strTemp,
                        out strError);
                    if (nRet == -1)
                        return strError;

                    prices.Add(strTemp);
                }
            }

            List<string> results = null;
            // ���ܼ۸�
            // ���ҵ�λ��ͬ�ģ��������
            nRet = PriceUtil.TotalPrice(prices,
                out results,
                out strError);
            if (nRet == -1)
                return strError;

                string strResult = "";
                for (int i = 0; i < results.Count; i++)
                {
                    string strPrice = results[i];
                    if (String.IsNullOrEmpty(strResult) == false)
                        strResult += "+";
                    strResult += strPrice;
                }

            return strResult;
        }


        private void button_load_loadFromBatchNo_Click(object sender, EventArgs e)
        {
            SearchByBatchnoForm dlg = new SearchByBatchnoForm();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.CfgSectionName = "PrintOrderForm_SearchByBatchnoForm";
            this.BatchNo = "";

            dlg.Text = "���ݶ������κż�����������¼";
            dlg.DisplayLocationList = false;

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

            this.BatchNo = dlg.BatchNo;
            /*
            string strMatchLocation = dlg.ItemLocation;

            if (strMatchLocation == "<��ָ��>")
                strMatchLocation = null;    // null��""������ܴ�
             * */

            string strError = "";
            int nRet = 0;

            if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
            {
            }
            else
            {
                if (this.Changed == true)
                {
                    // ������δ����
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ��������ԭʼ��Ϣ���޸ĺ���δ���档����ʱΪװ�������ݶ����ԭ����Ϣ����δ������Ϣ����ʧ��\r\n\r\nȷʵҪװ��������? ",
                        "PrintOrderForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result != DialogResult.Yes)
                    {
                        return; // ����
                    }
                }

                this.listView_origin.Items.Clear();
                // 2008/11/22 new add
                this.SortColumns_origin.Clear();
                SortColumns.ClearColumnSortDisplay(this.listView_origin.Columns);
            }

            EnableControls(false);
            // MainForm.ShowProgress(true);

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
                    lRet = Channel.SearchOrder(
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
                    lRet = Channel.SearchOrder(
                        stop,
                        this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
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
                    strError = "���κ� '" + dlg.BatchNo + "' û�����м�¼��";
                    goto ERROR1;
                }

                int nDupCount = 0;

                long lHitCount = lRet;

                stop.SetProgressRange(0, lHitCount);
                // stop.SetProgressValue(0);


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
                        string strRecPath = searchresults[i].Path;
                        // ���ݼ�¼·����װ�붩����¼
                        // return: 
                        //      -2  ·���Ѿ���list�д�����
                        //      -1  ����
                        //      1   �ɹ�
                        nRet = LoadOneItem(strRecPath,
                            this.listView_origin,
                            out strError);
                        if (nRet == -2)
                            nDupCount++;

                        stop.SetProgressValue(lStart + i + 1);
                    }

                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    stop.SetMessage("������ " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " ��");

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                }

                /*
                if (this.listView_in.Items.Count == 0
                    && strMatchLocation != null)
                {
                    strError = "��Ȼ���κ� '" + dlg.BatchNo + "' �����˼�¼ " + lHitCount.ToString() + " ��, �����Ǿ�δ��ƥ��ݲصص� '" + strMatchLocation + "' ��";
                    goto ERROR1;
                }*/


                // ���ϲ��������б�
                stop.SetMessage("���ںϲ�����...");
                nRet = FillMergedList(out strError);
                if (nRet == -1)
                    goto ERROR1;

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

        void dlg_GetBatchNoTable(object sender, GetKeyCountListEventArgs e)
        {
            Global.GetBatchNoTable(e,
                this,
                this.comboBox_load_type.Text,
                "order",
                this.stop,
                this.Channel);

#if NOOOOOOOOOOOOOOOOOOOOOOOOO
            string strError = "";

            if (e.KeyCounts == null)
                e.KeyCounts = new List<KeyCount>();

            EnableControls(false);
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����г�ȫ���������κ� ...");
            stop.BeginLoop();

            try
            {
                MainForm.SetProgressRange(100);
                MainForm.SetProgressValue(0);

                long lRet = Channel.SearchOrder(
                    stop,
                    "<all>",
                    "", // strBatchNo
                    2000,   // -1,
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
                    strError = "û���ҵ��κζ������κż�����";
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

        // ���������ֵ�ı仯����������ɫ
        // �㷨�ǽ�ԭ���ı�����ɫ�������ǳһ��
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
                    item.BackColor = Global.Dark(GetItemBackColor(item.ImageIndex), 0.05F);

                    /*
                    item.BackColor = Color.LightGray;

                    if (item.ForeColor == item.BackColor)
                        item.ForeColor = Color.Black;
                     * */
                }
                else
                {
                    item.BackColor = Global.Light(GetItemBackColor(item.ImageIndex), 0.05F);

                    /*
                    item.BackColor = Color.White;

                    if (item.ForeColor == item.BackColor)
                        item.ForeColor = Color.Black;
                     * */
                }
            }
        }


        private void listView_origin_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;
            this.SortColumns_origin.SetFirstColumn(nClickColumn,
                this.listView_origin.Columns);

            // ����
            this.listView_origin.ListViewItemSorter = new SortColumnsComparer(this.SortColumns_origin);

            this.listView_origin.ListViewItemSorter = null;

            SetGroupBackcolor(
                this.listView_origin,
                nClickColumn);
        }

        private void listView_origin_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            string strOrderRecPath = "";
            if (this.listView_origin.SelectedItems.Count > 0)
            {
                strOrderRecPath = ListViewUtil.GetItemText(this.listView_origin.SelectedItems[0], ORIGIN_COLUMN_RECPATH);
            }
            menuItem = new MenuItem("���ֲᴰ���۲충����¼ '" + strOrderRecPath + "' (&O)");
            menuItem.Tag = this.listView_origin;
            menuItem.Click += new System.EventHandler(this.menu_loadOrderRecord_Click);
            if (this.listView_origin.SelectedItems.Count == 0
                || String.IsNullOrEmpty(strOrderRecPath) == true)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("ˢ��ѡ������(&S)");
            menuItem.Tag = this.listView_origin;
            menuItem.Click += new System.EventHandler(this.menu_refreshSelected_Click);
            if (this.listView_origin.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ˢ��ȫ����(&R)");
            menuItem.Tag = this.listView_origin;
            menuItem.Click += new System.EventHandler(this.menu_refreshAll_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("���½��кϲ�(&M)");
            menuItem.Tag = this.listView_origin;
            menuItem.Click += new System.EventHandler(this.menu_merge_Click);
            if (this.listView_origin.Items.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("���ϲ��������(&S)");
            menuItem.Tag = this.listView_origin;
            menuItem.Click += new System.EventHandler(this.menu_sort_for_merge_Click);
            if (this.listView_origin.Items.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("�Ƴ�(&D)");
            menuItem.Tag = this.listView_origin;
            menuItem.Click += new System.EventHandler(this.menu_deleteSelected_Click);
            if (this.listView_origin.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.listView_origin, new Point(e.X, e.Y));		
        }

        // ���ֲᴰ���۲충����¼
        void menu_loadOrderRecord_Click(object sender, EventArgs e)
        {
            LoadOrderToEntityForm(this.listView_origin);
        }

        void LoadOrderToEntityForm(ListView list)
        {
            if (list.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ�ص��ֲᴰ������");
                return;
            }

            string strRecPath = ListViewUtil.GetItemText(list.SelectedItems[0], ORIGIN_COLUMN_RECPATH);

            EntityForm form = new EntityForm();

            form.MainForm = this.MainForm;
            form.MdiParent = this.MainForm;
            form.Show();

            if (this.comboBox_load_type.Text == "ͼ��")
                form.LoadOrderByRecPath(strRecPath, false);
            else
                form.LoadIssueByRecPath(strRecPath, false);
        }

        // ���½��кϲ�
        void menu_merge_Click(object sender, EventArgs e)
        {
            string strError = "";
            stop.SetMessage("���ںϲ�����...");
            int nRet = FillMergedList(out strError);
            stop.SetMessage("");
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
            }
            else
            {
                // �л��� �Ѻϲ� �б�
                this.tabControl_items.SelectedTab = this.tabPage_mergedItems;
            }
        }

        // ���պϲ��ķ��(Ҫ��)��ԭʼ������������
        void menu_sort_for_merge_Click(object sender, EventArgs e)
        {
            this.SortOriginListForMerge();
        }

        void menu_refreshSelected_Click(object sender, EventArgs e)
        {
            ListView list = (ListView)((MenuItem)sender).Tag;
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in list.SelectedItems)
            {
                items.Add(item);

            }
            RefreshLines(items);

            // ���ϲ��������б�
            string strError = "";
            stop.SetMessage("���ںϲ�����...");
            int nRet = FillMergedList(out strError);
            stop.SetMessage("");
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
            }

            /*
            if (this.tabControl_main.SelectedTab == this.tabPage_verify)
            {
                SetVerifyPageNextButtonEnabled();
            }
             * */
        }

        void menu_refreshAll_Click(object sender, EventArgs e)
        {
            ListView list = (ListView)((MenuItem)sender).Tag;

            List<ListViewItem> items = new List<ListViewItem>();
            for (int i = 0; i < list.Items.Count; i++)
            {
                items.Add(list.Items[i]);
            }
            RefreshLines(items);

            // ���ϲ��������б�
            string strError = "";
            stop.SetMessage("���ںϲ�����...");
            int nRet = FillMergedList(out strError);
            stop.SetMessage("");
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
            }

            /*
            if (this.tabControl_main.SelectedTab == this.tabPage_verify)
            {
                SetVerifyPageNextButtonEnabled();
            }*/
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
"ȷʵҪ��ԭʼ�����б������Ƴ�ѡ���� " + items.Count.ToString() + " ������?",
"dp2Circulation",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            DeleteLines(items);

            // ���ϲ��������б�
            string strError = "";
            stop.SetMessage("���ںϲ�����...");
            int nRet = FillMergedList(out strError);
            stop.SetMessage("");
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
            }

            SetNextButtonEnable();  // 2008/12/22 new add

            /*
            if (this.tabControl_main.SelectedTab == this.tabPage_verify)
            {
                SetVerifyPageNextButtonEnabled();
            }*/
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

        /*public*/ int RefreshOneItem(ListViewItem item,
            out string strError)
        {
            strError = "";

            string strItemText = "";
            string strBiblioText = "";

            string strItemRecPath = "";
            string strBiblioRecPath = "";

            byte[] item_timestamp = null;

            string strIndex = "@path:" + item.SubItems[ORIGIN_COLUMN_RECPATH].Text;

            long lRet = Channel.GetOrderInfo(
                stop,
                strIndex,
                // "",
                "xml",
                out strItemText,
                out strItemRecPath,
                out item_timestamp,
                "recpath",
                out strBiblioText,
                out strBiblioRecPath,
                out strError);
            if (lRet == -1 || lRet == 0)
            {
                ListViewUtil.ChangeItemText(item, 1, strError);

                SetItemColor(item, TYPE_ERROR);
                goto ERROR1;
            }

            string strBiblioSummary = "";
            string strISBnISSN = "";

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
                    Debug.Assert(results != null && results.Length == 2, "results�������2��Ԫ��");
                    strBiblioSummary = results[0];
                    strISBnISSN = results[1];
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
                SetListViewItemText(
                    this.comboBox_load_type.Text,
                    this.checkBox_print_accepted.Checked,
                    dom,
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
            return 1;
        ERROR1:
            return -1;
        }

        // 
        /// <summary>
        /// �Ƚ�����������ַ�Ƿ���ȫһ��
        /// </summary>
        /// <param name="strXml1">������ַ XML Ƭ��1</param>
        /// <param name="strXml2">������ַ XML Ƭ��2</param>
        /// <returns>0: ��ȫһ��; 1: ����ȫһ��</returns>
        public static int CompareAddress(string strXml1, string strXml2)
        {
            if (string.IsNullOrEmpty(strXml1) == true && string.IsNullOrEmpty(strXml2) == true)
                return 0;
            if (string.IsNullOrEmpty(strXml1) == true && string.IsNullOrEmpty(strXml2) == false)
                return 1;
            if (string.IsNullOrEmpty(strXml1) == false && string.IsNullOrEmpty(strXml2) == true)
                return 1;
            XmlDocument dom1 = new XmlDocument();
            XmlDocument dom2 = new XmlDocument();

            try
            {
                dom1.LoadXml("<root>" + strXml1 + "</root>");
            }
            catch (Exception ex)
            {
                throw new Exception("������ַXML�ַ��� '"+strXml1+"' ��ʽ����ȷ: " + ex.Message);
            }

            try
            {
                dom2.LoadXml("<root>" + strXml2 + "</root>");
            }
            catch (Exception ex)
            {
                throw new Exception("������ַXML�ַ��� '" + strXml2 + "' ��ʽ����ȷ: " + ex.Message);
            }

            string[] elements = new string[] {
            "zipcode",
            "address",
            "department",
            "name",
            "tel",
            "email",
            "bank",
            "accounts",
            "payStyle",
            "comment"};

            foreach (string element in elements)
            {
                string v1 = DomUtil.GetElementText(dom1.DocumentElement, element);
                string v2 = DomUtil.GetElementText(dom2.DocumentElement, element);
                if (string.IsNullOrEmpty(v1) == true && string.IsNullOrEmpty(v2) == true)
                    continue;
                if (v1 != v2)
                    return 1;

            }

            return 0;
        }

        // ���ϲ��������б�
        int FillMergedList(out string strError)
        {
            strError = "";
            int nRet = 0;

            DateTime now = DateTime.Now;
            int nOrderIdSeed = 1;

            this.listView_merged.Items.Clear();
            // 2008/11/22 new add
            this.SortColumns_merged.Clear();
            SortColumns.ClearColumnSortDisplay(this.listView_merged.Columns);


            // �Ƚ�ԭʼ�����б��� seller/price ������
            SortOriginListForMerge();


            // ѭ��
            for (int i = 0; i < this.listView_origin.Items.Count; i++)
            {
                int nCopy = 0;

                ListViewItem source = this.listView_origin.Items[i];

                if (source.ImageIndex == TYPE_ERROR)
                {
                    strError = "���� " + (i + 1).ToString() + " ��״̬Ϊ���������ų�����...";
                    return -1;
                }

                // ����
                string strSeller = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_SELLER);

                // ������ַ
                string strSellerAddress = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_SELLERADDRESS);


                // ����
                string strPrice = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_PRICE);

                string strAcceptPrice = "";

                // priceȡ���еĶ����۲���
                {
                    string strOldPrice = "";
                    string strNewPrice = "";

                    // ���� "old[new]" �ڵ�����ֵ
                    OrderDesignControl.ParseOldNewValue(strPrice,
                        out strOldPrice,
                        out strNewPrice);

                    strPrice = strOldPrice;
                    strAcceptPrice = strNewPrice;
                }

                string strIssueCount = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_ISSUECOUNT);
                string strRange = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_RANGE);

                // ��Ŀ��¼·��
                string strBiblioRecPath = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_BIBLIORECPATH);

                // ��Ŀ��
                string strCatalogNo = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_CATALOGNO);

                // 2012/8/30
                string strOrderTime = ListViewUtil.GetItemText(source,
    ORIGIN_COLUMN_ORDERTIME);   // �Ѿ��Ǳ���ʱ���ʽ


                string strTempCopy = ListViewUtil.GetItemText(source,
ORIGIN_COLUMN_COPY);
                string strTempAcceptCopy = "";
                {
                    string strOldCopy = "";
                    string strNewCopy = "";
                    // ���� "old[new]" �ڵ�����ֵ
                    OrderDesignControl.ParseOldNewValue(strTempCopy,
                        out strOldCopy,
                        out strNewCopy);
                    strTempCopy = strOldCopy;
                    strTempAcceptCopy = strNewCopy;
                }

                int nSubCopy = 1;
                {
                    string strRightCopy = OrderDesignControl.GetRightFromCopyString(strTempCopy);
                    if (String.IsNullOrEmpty(strRightCopy) == false)
                    {
                        try
                        {
                            nSubCopy = Convert.ToInt32(strRightCopy);
                        }
                        catch (Exception ex)
                        {
                            strError = "ԭʼ�������� " + (i + 1).ToString() + " ��ÿ�ײ��� '" + strRightCopy + "' ��ʽ����ȷ: " + ex.Message;
                            return -1;
                        }
                    }
                }

                // 2014/2/19
                int nAcceptSubCopy = 1;
                {
                    string strRightCopy = OrderDesignControl.GetRightFromCopyString(strTempAcceptCopy);
                    if (String.IsNullOrEmpty(strRightCopy) == false)
                    {
                        try
                        {
                            nAcceptSubCopy = Convert.ToInt32(strRightCopy);
                        }
                        catch (Exception ex)
                        {
                            strError = "ԭʼ�������� " + (i + 1).ToString() + " ���ѵ�ÿ�ײ��� '" + strRightCopy + "' ��ʽ����ȷ: " + ex.Message;
                            return -1;
                        }
                    }
                }

                string strMergeComment = "";    // �ϲ�ע��
                List<string> totalprices = new List<string>();  // �ۻ��ļ۸��ַ���
                List<ListViewItem> origin_items = new List<ListViewItem>();

                string strComments = "";    // ԭʼע��(����)
                string strDistributes = ""; // �ϲ��Ĺݲط����ַ���

                // ����biblioRecPath��price��seller��catalogno����ͬ������
                // ����������������Ҫissuecount��range��ͬ
                int nStart = i; // ���ο�ʼλ��
                int nLength = 0;    // �������������

                for (int j = i; j < this.listView_origin.Items.Count; j++)
                {
                    ListViewItem current_source = this.listView_origin.Items[j];

                    if (current_source.ImageIndex == TYPE_ERROR)
                    {
                        strError = "���� " + (i + 1).ToString() + " ��״̬Ϊ���������ų�����...";
                        return -1;
                    }


                    // ����
                    string strCurrentSeller = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_SELLER);

                    // ������ַ
                    string strCurrentSellerAddress = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_SELLERADDRESS);

                    // ����
                    string strCurrentPrice = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_PRICE);

                    string strCurrentAcceptPrice = "";
                    // priceȡ���еĶ����۲���
                    {
                        string strCurrentOldPrice = "";
                        string strCurrentNewPrice = "";

                        // ���� "old[new]" �ڵ�����ֵ
                        OrderDesignControl.ParseOldNewValue(strCurrentPrice,
                            out strCurrentOldPrice,
                            out strCurrentNewPrice);

                        strCurrentPrice = strCurrentOldPrice;
                        strCurrentAcceptPrice = strCurrentNewPrice;
                    }

                    string strCurrentIssueCount = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_ISSUECOUNT);
                    string strCurrentRange = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_RANGE);

                    // ��Ŀ��¼·��
                    string strCurrentBiblioRecPath = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_BIBLIORECPATH);

                    // ��Ŀ��
                    string strCurrentCatalogNo = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_CATALOGNO);

                    string strTempCurCopy = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_COPY);

                    {
                        string strOldCopy = "";
                        string strNewCopy = "";
                        // ���� "old[new]" �ڵ�����ֵ
                        OrderDesignControl.ParseOldNewValue(strTempCurCopy,
                            out strOldCopy,
                            out strNewCopy);
                        strTempCurCopy = strOldCopy;
                    }

                    int nCurCopy = 0;
                    string strLeftCopy = OrderDesignControl.GetCopyFromCopyString(strTempCurCopy);
                    try
                    {
                        nCurCopy = Convert.ToInt32(strLeftCopy);
                    }
                    catch (Exception ex)
                    {
                        strError = "ԭʼ�������� " + (i + 1).ToString() + " �ڸ������� '" + strLeftCopy + "' ��ʽ����ȷ: " + ex.Message;
                        return -1;
                    }

                    int nCurSubCopy = 1;
                    string strRightCopy = OrderDesignControl.GetRightFromCopyString(strTempCurCopy);
                    if (String.IsNullOrEmpty(strRightCopy) == false)
                    {
                        try
                        {
                            nCurSubCopy = Convert.ToInt32(strRightCopy);
                        }
                        catch (Exception ex)
                        {
                            strError = "ԭʼ�������� " + (i + 1).ToString() + " ��ÿ�ײ��� '" + strRightCopy + "' ��ʽ����ȷ: " + ex.Message;
                            return -1;
                        }
                    }

                    if (this.comboBox_load_type.Text == "ͼ��")
                    {
                        // ��Ԫ���ж� // ��Ԫ���ж� // ��Ԫ���ж�
                        if (strBiblioRecPath != strCurrentBiblioRecPath
                            || strSeller != strCurrentSeller
                            || strPrice != strCurrentPrice
                            || strAcceptPrice != strCurrentAcceptPrice
                            || strCatalogNo != strCurrentCatalogNo
                            || nSubCopy != nCurSubCopy
                            || CompareAddress(strSellerAddress, strCurrentSellerAddress) != 0)
                            break;

                    }
                    else
                    {
                        // ��Ԫ���ж� // ��Ԫ���ж� // ��Ԫ���ж�
                        if (strBiblioRecPath != strCurrentBiblioRecPath
                            || strSeller != strCurrentSeller
                            || strPrice != strCurrentPrice
                            || strAcceptPrice != strCurrentAcceptPrice
                            || strCatalogNo != strCurrentCatalogNo
                            || strIssueCount != strCurrentIssueCount
                            || strRange != strCurrentRange
                            || nSubCopy != nCurSubCopy
                            || CompareAddress(strSellerAddress, strCurrentSellerAddress) != 0)
                            break;
                    }



                    int nIssueCount = 1;
                    if (this.comboBox_load_type.Text != "ͼ��")
                    {
                        try
                        {
                            nIssueCount = Convert.ToInt32(strIssueCount);
                        }
                        catch (Exception ex)
                        {
                            strError = "ԭʼ�������� " + (i + 1).ToString() + " ������ '" + strIssueCount + "' ��ʽ����ȷ: " + ex.Message;
                            return -1;
                        }
                    }

                    // ���ܸ�����
                    nCopy += nCurCopy;

                    // ���ܺϲ�ע��
                    string strSource = ListViewUtil.GetItemText(current_source, ORIGIN_COLUMN_SOURCE);
                    string strRecPath = ListViewUtil.GetItemText(current_source, ORIGIN_COLUMN_RECPATH);
                    if (String.IsNullOrEmpty(strMergeComment) == false)
                        strMergeComment += "; ";
                    strMergeComment += strSource + ", " + nCurCopy.ToString() + "�� (" + strRecPath + ")";

                    // ���ܼ۸�
                    string strTotalPrice = "";
                    // 2009/11/9 changed
                    if (String.IsNullOrEmpty(strCurrentPrice) == false)
                    {
                        nRet = PriceUtil.MultiPrice(strCurrentPrice,
                            nCurCopy * nIssueCount,
                            out strTotalPrice,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ԭʼ�������� " + (i + 1).ToString() + " �ڼ۸��ַ��� '" + strCurrentPrice + "' ��ʽ����ȷ: " + strError;
                            return -1;
                        }
                    }
                    else
                    {
                        // 2009/11/9 new add
                        // ԭʼ�����е��ܼ�
                        strTotalPrice = ListViewUtil.GetItemText(current_source,
                            ORIGIN_COLUMN_TOTALPRICE);
                        if (String.IsNullOrEmpty(strTotalPrice) == true)
                        {
                            strError = "ԭʼ�������� " + (i + 1).ToString() + " �ڣ����۸��ַ���Ϊ��ʱ���ܼ۸��ַ�����ӦΪ��";
                            return -1;
                        }
                    }

                    totalprices.Add(strTotalPrice);

                    // ����ע��
                    string strComment = ListViewUtil.GetItemText(current_source, ORIGIN_COLUMN_COMMENT);
                    if (String.IsNullOrEmpty(strComment) == false)
                    {
                        if (String.IsNullOrEmpty(strComments) == false)
                            strComments += "; ";
                        strComments += strComment + " @" + strRecPath;
                    }

                    // ���ܹݲط����ַ���
                    string strCurDistribute = ListViewUtil.GetItemText(current_source, ORIGIN_COLUMN_DISTRIBUTE);
                    if (String.IsNullOrEmpty(strCurDistribute) == false)
                    {
                        if (String.IsNullOrEmpty(strDistributes) == true)
                            strDistributes = strCurDistribute;
                        else
                        {
                            string strLocationString = "";
                            nRet = LocationCollection.MergeTwoLocationString(strDistributes,
                                strCurDistribute,
                                false,
                                out strLocationString,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            strDistributes = strLocationString;
                        }
                    }

                    // ����ԭʼ����
                    origin_items.Add(current_source);

                    nLength++;
                }

                ListViewItem target = new ListViewItem();

                if (source.ImageIndex == TYPE_ERROR)
                    target.ImageIndex = TYPE_ERROR;
                else
                    target.ImageIndex = TYPE_NORMAL;  // 

                // seller
                target.Text = strSeller;

                // catalog no 
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_CATALOGNO,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_CATALOGNO));

                // summary
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_SUMMARY,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_SUMMARY));

                // isbn issn
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ISBNISSN,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_ISBNISSN));


                // merge comment
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_MERGECOMMENT,
                    strMergeComment);

                // range
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_RANGE,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_RANGE));

                // issue count
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ISSUECOUNT,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_ISSUECOUNT));

                // copy
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_COPY,
                    nCopy.ToString());

                // subcopy
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_SUBCOPY,
                    nSubCopy.ToString());

                // price
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_PRICE,
                    strPrice);

                List<string> sum_prices = null;
                nRet = PriceUtil.TotalPrice(totalprices,
                    out sum_prices,
                    out strError);
                if (nRet == -1)
                {
                    return -1;
                }

                Debug.Assert(sum_prices.Count == 1, "");
                // total price
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_TOTALPRICE,
                    sum_prices[0]);

                // order time
                if (this.checkBox_print_accepted.Checked == false)
                {
                    ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ORDERTIME,
                        now.ToShortDateString());   // TODO: ע�����ʱ��Ҫ���ص�ԭʼ������
                }
                else
                {
                    ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ORDERTIME,
                        strOrderTime);
                }

                // order id
                string strOrderID = nOrderIdSeed.ToString();
                nOrderIdSeed++;
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ORDERID,
                    strOrderID);    // TODO: ע��������Ҫ���ص�ԭʼ������

                // distribute
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_DISTRIBUTE,
                    strDistributes);

                string strAcceptSeries = "";
                
                if (string.IsNullOrEmpty(strDistributes) == false)
                {
                    LocationCollection locations = new LocationCollection();
                    nRet = locations.Build(strDistributes,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "�ݲط����ַ��� '"+strDistributes+"' ��ʽ����: " + strError;
                        return -1;
                    }

                    // �������ڶ������������refid�����߸������γ�һ�顣���������ص�Ӧ�����Ϊ���������ǲ����������ڿ������ղ���
                    strAcceptSeries = locations.GetArrivedCopy().ToString();
                }

                // acceptcopy
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ACCEPTCOPY,
                    strAcceptSeries);

                // acceptsubcopy
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ACCEPTSUBCOPY,
                    nAcceptSubCopy.ToString());

                // acceptprice
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ACCEPTPRICE,
                    strAcceptPrice);

                // class
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_CLASS,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_CLASS));

                // comment
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_COMMENT,
                    strComments);

                // sellerAddress
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_SELLERADDRESS,
                    strSellerAddress);


                // biblio record path
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_BIBLIORECPATH,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_BIBLIORECPATH));

                // ÿ���ϲ��������Tag������������ԴListViewItem���б�
                target.Tag = origin_items;

                // �޸�ԭʼ�����orderTime orderID
                if (this.checkBox_print_accepted.Checked == false)
                {
                    for (int k = 0; k < origin_items.Count; k++)
                    {
                        ListViewItem origin_item = origin_items[k];

                        bool bChanged = false;
                        string strOldOrderTime = ListViewUtil.GetItemText(origin_item, ORIGIN_COLUMN_ORDERTIME);
                        if (strOldOrderTime != now.ToShortDateString())
                        {
                            ListViewUtil.ChangeItemText(origin_item, ORIGIN_COLUMN_ORDERTIME,
                                now.ToShortDateString());
                            bChanged = true;

                            origin_item.SubItems[ORIGIN_COLUMN_ORDERTIME].BackColor = System.Drawing.Color.Red;

                            // �Ӵ�����
                            origin_item.SubItems[ORIGIN_COLUMN_ORDERTIME].Font =
                                new System.Drawing.Font(origin_item.SubItems[ORIGIN_COLUMN_ORDERTIME].Font, FontStyle.Bold);
                        }

                        string strOldOrderID = ListViewUtil.GetItemText(origin_item, ORIGIN_COLUMN_ORDERID);
                        if (strOrderID != strOldOrderID)
                        {
                            ListViewUtil.ChangeItemText(origin_item, ORIGIN_COLUMN_ORDERID,
                                strOrderID);
                            bChanged = true;

                            // �Ӵ�����
                            origin_item.SubItems[ORIGIN_COLUMN_ORDERID].Font =
                                new System.Drawing.Font(origin_item.SubItems[ORIGIN_COLUMN_ORDERID].Font, FontStyle.Bold);
                        }

                        if (bChanged == true)
                            SetItemChanged(origin_item, true);
                    }
                }

                this.listView_merged.Items.Add(target);

                i = nStart + nLength - 1;
            }

            // ˢ��Origin����ǳ���ɫ
            if (this.SortColumns_origin.Count > 0)
            {
                SetGroupBackcolor(
                    this.listView_origin,
                    this.SortColumns_origin[0].No);
            }

            return 0;
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
        }

        void SortOriginListForMerge()
        {
            SortColumns sort_columns = new SortColumns();

            DigitalPlatform.Column column = new DigitalPlatform.Column();
            column.Asc = true;
            column.No = ORIGIN_COLUMN_BIBLIORECPATH;
            column.SortStyle = ColumnSortStyle.RecPath;
            sort_columns.Add(column);

            column = new DigitalPlatform.Column();
            column.Asc = true;
            column.No = ORIGIN_COLUMN_SELLER;
            column.SortStyle = ColumnSortStyle.LeftAlign;
            sort_columns.Add(column);

            column = new DigitalPlatform.Column();
            column.Asc = true;
            column.No = ORIGIN_COLUMN_PRICE;
            column.SortStyle = ColumnSortStyle.LeftAlign;
            sort_columns.Add(column);

            // ����
            this.listView_origin.ListViewItemSorter = new SortColumnsComparer(sort_columns);

            this.listView_origin.ListViewItemSorter = null;

            SetGroupBackcolor(
                this.listView_origin,
                ORIGIN_COLUMN_BIBLIORECPATH);

            this.SortColumns_origin = sort_columns;
        }

        // ���� �ϲ����б�
        private void listView_merged_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;
            this.SortColumns_merged.SetFirstColumn(nClickColumn,
                this.listView_merged.Columns);

            // ����
            this.listView_merged.ListViewItemSorter = new SortColumnsComparer(this.SortColumns_merged);

            this.listView_merged.ListViewItemSorter = null;

            SetGroupBackcolor(
                this.listView_merged,
                nClickColumn);
        }

        // �ϲ����б��ϵ�popupmemu
        private void listView_merged_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("����ӡ�������(&S)");
            menuItem.Tag = this.listView_merged;
            menuItem.Click += new System.EventHandler(this.menu_sort_merged_for_print_Click);
            if (this.listView_merged.Items.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("�Ƴ�(&D)");
            menuItem.Tag = this.listView_merged;
            menuItem.Click += new System.EventHandler(this.menu_deleteSelected_merged_Click);
            if (this.listView_merged.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.listView_merged, new Point(e.X, e.Y));		
        }

        // �Ƴ� �ϲ����б���ѡ��������
        void menu_deleteSelected_merged_Click(object sender, EventArgs e)
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
"ȷʵҪ�ںϲ����б������Ƴ�ѡ���� " + items.Count.ToString() + " ������?",
"dp2Circulation",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            DeleteLines(items);
        }

        // ���մ�ӡ����ķ��(Ҫ��)�Ժϲ���������������
        void menu_sort_merged_for_print_Click(object sender, EventArgs e)
        {
            this.SortMergedListForOutput();
        }

        // ���մ�ӡ����ķ��(Ҫ��)�Ժϲ���������������
        void SortMergedListForOutput()
        {
            SortColumns sort_columns = new SortColumns();

            DigitalPlatform.Column column = new DigitalPlatform.Column();
            column.Asc = true;
            column.No = MERGED_COLUMN_SELLER;
            column.SortStyle = ColumnSortStyle.LeftAlign;
            sort_columns.Add(column);

            column = new DigitalPlatform.Column();
            column.Asc = true;
            column.No = MERGED_COLUMN_BIBLIORECPATH;
            column.SortStyle = ColumnSortStyle.RecPath;
            sort_columns.Add(column);


            column = new DigitalPlatform.Column();
            column.Asc = true;
            column.No = MERGED_COLUMN_PRICE;
            column.SortStyle = ColumnSortStyle.LeftAlign;
            sort_columns.Add(column);

            // ����
            this.listView_merged.ListViewItemSorter = new SortColumnsComparer(sort_columns);

            this.listView_merged.ListViewItemSorter = null;

            SetGroupBackcolor(
                this.listView_merged,
                MERGED_COLUMN_SELLER);
        }

        #region ԭʼ����

        // ��ӡԭʼ����
        private void button_print_printOriginList_Click(object sender, EventArgs e)
        {
            int nErrorCount = 0;

            this.tabControl_items.SelectedTab = this.tabPage_originItems;

            List<ListViewItem> items = new List<ListViewItem>();
            for (int i = 0; i < this.listView_origin.Items.Count; i++)
            {
                ListViewItem item = this.listView_origin.Items[i];

                items.Add(item);

                if (item.ImageIndex == TYPE_ERROR)
                    nErrorCount++;
            }

            if (nErrorCount != 0)
            {
                MessageBox.Show(this, "���棺�����ӡ�����嵥���а���������Ϣ�����");
            }

            PrintOriginList(items);
            return;
        }

        // ԭʼ����ѡ��
        private void button_print_originOption_Click(object sender, EventArgs e)
        {
            // ���ñ���ͷ��
            string strNamePath = "orderorigin_printoption";

            OrderOriginPrintOption option = new OrderOriginPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);

            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.Text = this.comboBox_load_type.Text + " ԭʼ���� ��ӡ����";
            dlg.PrintOption = option;
            dlg.DataDir = this.MainForm.DataDir;
            dlg.ColumnItems = new string[] {
                "no -- ���",
                "recpath -- ��¼·��",
                "summary -- ժҪ",
                "isbnIssn -- ISBN/ISSN",
                "state -- ״̬",
                "catalogNo -- ��Ŀ��",
                "seller -- ����",
                "source -- ������Դ",
                "range -- ʱ�䷶Χ",
                "issueCount -- ��������",
                "copy -- ������",

                "price -- ����",
                "totalPrice -- �ܼ۸�",
                "orderTime -- ����ʱ��",
                "orderID -- ������",
                "distribute -- �ݲط���",
                "class -- ���",

                "comment -- ע��",
                "batchNo -- ���κ�",

                "sellerAddress -- ������ַ",
                "sellerAddress:zipcode -- ������ַ:��������",
                "sellerAddress:address -- ������ַ:��ַ",
                "sellerAddress:department -- ������ַ:��λ",
                "sellerAddress:name -- ������ַ:��ϵ��",
                "sellerAddress:tel -- ������ַ:�绰",
                "sellerAddress:email -- ������ַ:Email��ַ",
                "sellerAddress:bank -- ������ַ:������",
                "sellerAddress:accounts -- ������ַ:�����˺�",
                "sellerAddress:payStyle -- ������ַ:��ʽ",
                "sellerAddress:comment -- ������ַ:��ע",

                "biblioRecpath -- �ּ�¼·��"
            };


            this.MainForm.AppInfo.LinkFormState(dlg, "orderorigin_printoption_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            option.SaveData(this.MainForm.AppInfo,
                strNamePath);
        }

        void PrintOriginList(List<ListViewItem> items)
        {
            string strError = "";

            // ����һ��html�ļ�������ʾ��HtmlPrintForm�С�
            List<string> filenames = null;
            try
            {
                // Debug.Assert(false, "");

                // ����htmlҳ��
                int nRet = BuildOriginHtml(
                    items,
                    out filenames,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                HtmlPrintForm printform = new HtmlPrintForm();

                printform.Text = "��ӡԭʼ��������";
                printform.MainForm = this.MainForm;
                printform.Filenames = filenames;
                this.MainForm.AppInfo.LinkFormState(printform, "printorder_htmlprint_formstate");
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

        // ����htmlҳ��
        int BuildOriginHtml(
            List<ListViewItem> items,
            out List<string> filenames,
            out string strError)
        {
            strError = "";
            filenames = new List<string>();    // ÿҳһ���ļ�������������������ļ���
            int nRet = 0;

            Hashtable macro_table = new Hashtable();

            // ��ô�ӡ����
            OrderOriginPrintOption option = new OrderOriginPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                "orderorigin_printoption");

            // ׼��һ��� MARC ������
            {
                string strMarcFilterFilePath = option.GetTemplatePageFilePath("MARC������");
                if (String.IsNullOrEmpty(strMarcFilterFilePath) == false)
                {
                    Debug.Assert(String.IsNullOrEmpty(strMarcFilterFilePath) == false, "");
                    nRet = PrepareMarcFilter(strMarcFilterFilePath, out strError);
                    if (nRet == -1)
                        return -1;
                }
            }

            /*
            // ��鵱ǰ����״̬�Ͱ����ּ۸���֮���Ƿ����ì��
            if (bHasBiblioPriceColumn(option) == true)
            {

                if (this.SortColumns_in.Count != 0
                    && this.SortColumns_in[0].No == COLUMN_BIBLIORECPATH)
                {
                }
                else
                {
                    MessageBox.Show(this, "���棺��ӡ����Ҫ���ˡ��ּ۸��У�����ӡǰ���������δ�����ּ�¼·��������������ӡ���ġ��ּ۸������ݽ��᲻׼ȷ��\r\n\r\nҪ����������������ڴ�ӡǰ���������㡮�ּ�¼·���������⣬ȷ����������");
                }
            }*/


            // �����ҳ����
            int nTablePageCount = items.Count / option.LinesPerPage;
            if ((items.Count % option.LinesPerPage) != 0)
                nTablePageCount++;

            int nPageCount = nTablePageCount + 1;

            macro_table["%batchno%"] = this.BatchNo; // ���κ�
            macro_table["%pagecount%"] = nPageCount.ToString();
            macro_table["%linesperpage%"] = option.LinesPerPage.ToString();
            macro_table["%date%"] = DateTime.Now.ToLongDateString();

            macro_table["%recpathfilepath%"] = this.RecPathFilePath;
            string strRecPathFilename = Path.GetFileName(this.RecPathFilePath);
            macro_table["%recpathfilename%"] = strRecPathFilename;
            // ���κŻ��ļ��� ����겻�����жϺ�ѡ�ã�ֻ�úϲ�Ϊһ����
            macro_table["%batchno_or_recpathfilename%"] = String.IsNullOrEmpty(this.BatchNo) == true ? strRecPathFilename : this.BatchNo;


            string strFileNamePrefix = this.MainForm.DataDir + "\\~printorder";

            string strFileName = "";

            // �����Ϣҳ
            {
                int nItemCount = items.Count;
                int nTotalCopies = GetOriginTotalCopies(items);
                int nTotalSeries = GetOriginTotalSeries(items);
                int nBiblioCount = GetOriginBiblioCount(items);
                string strTotalPrice = GetOriginTotalPrice(items).ToString();

                macro_table["%itemcount%"] = nItemCount.ToString(); // ������
                macro_table["%totalcopies%"] = nTotalCopies.ToString(); // �ܲ���(ע��ÿ������ж��)
                macro_table["%totalseries%"] = nTotalSeries.ToString(); // ����
                macro_table["%bibliocount%"] = nBiblioCount.ToString(); // ����
                macro_table["%totalprice%"] = strTotalPrice;    // �ܼ۸�


                macro_table["%pageno%"] = "1";

                strFileName = strFileNamePrefix + "0" + ".html";

                filenames.Add(strFileName);

                BuildOriginPageTop(option,
                    macro_table,
                    strFileName,
                    false);

                // ������

                StreamUtil.WriteText(strFileName,
                    "<div class='bibliocount'>����: " + nBiblioCount.ToString() + "</div>");
                StreamUtil.WriteText(strFileName,
                    "<div class='seriescount'>����: " + nTotalSeries.ToString() + "</div>");
                StreamUtil.WriteText(strFileName,
                    "<div class='itemcount'>����: " + nTotalCopies.ToString() + "</div>");
                StreamUtil.WriteText(strFileName,
                    "<div class='totalprice'>�ܼ�: " + HttpUtility.HtmlEncode(strTotalPrice) + "</div>");

                BuildOriginPageBottom(option,
                    macro_table,
                    strFileName,
                    false);

            }

            // ���ҳѭ��
            for (int i = 0; i < nTablePageCount; i++)
            {
                macro_table["%pageno%"] = (i + 1 + 1).ToString();

                strFileName = strFileNamePrefix + (i + 1).ToString() + ".html";

                filenames.Add(strFileName);

                BuildOriginPageTop(option,
                    macro_table,
                    strFileName,
                    true);
                // ��ѭ��
                for (int j = 0; j < option.LinesPerPage; j++)
                {
                    BuildOriginTableLine(option,
                        items,
                        strFileName, i, j);
                }

                BuildOriginPageBottom(option,
                    macro_table,
                    strFileName,
                    true);
            }

            return 0;
        }

        int BuildOriginPageTop(PrintOption option,
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

            // string strCssUrl = this.MainForm.LibraryServerDir + "/orderorigin.css";
            // 2009/10/10 changed
            string strCssUrl = GetAutoCssUrl(option, "orderorigin.css");

            /*
            // 2009/10/9 new add
            string strCssFilePath = option.GetTemplatePageFilePath("css");  // ��Сд������
            if (String.IsNullOrEmpty(strCssFilePath) == false)
                strCssUrl = strCssFilePath;
            else
                strCssUrl = this.MainForm.LibraryServerDir + "/orderorigin.css";    // ȱʡ��
             * */

            string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";

            StreamUtil.WriteText(strFileName,
                "<!DOCTYPE html PUBLIC \" -//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">"
                + "<html><head>" + strLink + "</head><body>");


            // ҳü
            string strPageHeaderText = option.PageHeader;

            if (String.IsNullOrEmpty(strPageHeaderText) == false)
            {
                strPageHeaderText = StringUtil.MacroString(macro_table,
                    strPageHeaderText);

                StreamUtil.WriteText(strFileName,
                    "<div class='pageheader'>" + HttpUtility.HtmlEncode(strPageHeaderText) + "</div>");

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
                    "<div class='tabletitle'>" + HttpUtility.HtmlEncode(strTableTitleText) + "</div>");
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
                        "<td class='" + strClass + "'>" + HttpUtility.HtmlEncode(strCaption) + "</td>");
                }

                StreamUtil.WriteText(strFileName,
                    "</tr>");

            }

            return 0;
        }

        int BuildOriginTableLine(PrintOption option,
            List<ListViewItem> items,
            string strFileName,
            int nPage,
            int nLine)
        {
            // ��Ŀ����
            string strLineContent = "";
            int nRet = 0;

            int nIndex = nPage * option.LinesPerPage + nLine;

            if (nIndex >= items.Count)
                goto END1;

            ListViewItem item = items[nIndex];

            if (this.MarcFilter != null)
            {
                string strError = "";
                string strMARC = "";
                string strOutMarcSyntax = "";

                // TODO: �д���Ҫ���Ա����������������ڴ�ӡ������ŷ��֣�������

                // ���MARC��ʽ��Ŀ��¼
                string strBiblioRecPath = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_BIBLIORECPATH);

                nRet = GetMarc(strBiblioRecPath,
                    out strMARC,
                    out strOutMarcSyntax,
                    out strError);
                if (nRet == -1)
                {
                    strLineContent = strError;
                    goto END1;
                }

                this.ColumnTable.Clear();   // �����һ��¼����ʱ���������

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


            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];

                /*
                int nIndex = nPage * option.LinesPerPage + nLine;

                if (nIndex >= items.Count)
                    break;

                ListViewItem item = items[nIndex];
                 * */

                string strContent = GetOriginColumnContent(item,
                    column.Name);

                if (strContent == "!!!#")
                    strContent = ((nPage * option.LinesPerPage) + nLine + 1).ToString();

                /*
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
                 * */

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
                else
                    strContent = HttpUtility.HtmlEncode(strContent).Replace("\r\n", "<br/>");

                string strClass = StringUtil.GetLeft(column.Name);

                strLineContent +=
                    "<td class='" + strClass + "'>" + strContent + "</td>";
            }

            END1:

            StreamUtil.WriteText(strFileName,
    "<tr class='content'>");

            /* ���Ҫ��ӡ����
            if (string.IsNullOrEmpty(strLineContent) == true)
            {
                strLineContent += "<td class='blank' colspan='" + option.Columns.Count.ToString() + "'>&nbsp;</td>";
            }
             * */
            StreamUtil.WriteText(strFileName,
    strLineContent);

            StreamUtil.WriteText(strFileName,
                "</tr>");

            return 0;
        }

        // �����Ŀ����
        string GetOriginColumnContent(ListViewItem item,
            string strColumnName)
        {
            // ȥ��"-- ?????"����
            /*
            string strText = strColumnName;
            int nRet = strText.IndexOf("--", 0);
            if (nRet != -1)
                strText = strText.Substring(0, nRet).Trim();
             * */

            string strName = "";
            string strParameters = "";
            ParseNameParam(StringUtil.GetLeft(strColumnName),
                out strName,
                out strParameters);

            // 2013/3/29
            // Ҫ��ColumnTableֵ
            if (strName.Length > 0 && strName[0] == '@')
            {
                strName = strName.Substring(1);
                return (string)this.ColumnTable[strName];
            }

            try
            {

                // Ҫ��Ӣ�Ķ�����
                switch (strName)
                {
                    case "no":
                    case "���":
                        return "!!!#";  // ����ֵ����ʾ���

                    case "recpath":
                    case "��¼·��":
                        return item.SubItems[ORIGIN_COLUMN_RECPATH].Text;

                    case "errorInfo":
                    case "summary":
                    case "ժҪ":
                        return item.SubItems[ORIGIN_COLUMN_SUMMARY].Text;

                    case "isbnIssn":
                    case "ISBN/ISSN":
                        return item.SubItems[ORIGIN_COLUMN_ISBNISSN].Text;


                    case "state":
                    case "״̬":
                        return item.SubItems[ORIGIN_COLUMN_STATE].Text;

                    case "catalogNo":
                    case "��Ŀ��":
                        return item.SubItems[ORIGIN_COLUMN_CATALOGNO].Text;

                    case "seller":
                    case "����":
                    case "����":
                        return item.SubItems[ORIGIN_COLUMN_SELLER].Text;

                    case "source":
                    case "������Դ":
                        return item.SubItems[ORIGIN_COLUMN_SOURCE].Text;

                    case "range":
                    case "ʱ�䷶Χ":
                        return item.SubItems[ORIGIN_COLUMN_RANGE].Text;

                    case "issueCount":
                    case "��������":
                        return item.SubItems[ORIGIN_COLUMN_ISSUECOUNT].Text;

                    case "copy":
                    case "������":
                        return item.SubItems[ORIGIN_COLUMN_COPY].Text;

                    case "price":
                    case "����":
                        return item.SubItems[ORIGIN_COLUMN_PRICE].Text;

                    case "totalPrice":
                    case "�ܼ۸�":
                        return item.SubItems[ORIGIN_COLUMN_TOTALPRICE].Text;

                    case "orderTime":
                    case "����ʱ��":
                        return item.SubItems[ORIGIN_COLUMN_ORDERTIME].Text;

                    case "orderID":
                    case "������":
                        return item.SubItems[ORIGIN_COLUMN_ORDERID].Text;

                    case "distribute":
                    case "�ݲط���":
                        return item.SubItems[ORIGIN_COLUMN_DISTRIBUTE].Text;

                    case "class":
                    case "���":
                        return item.SubItems[ORIGIN_COLUMN_CLASS].Text;

                    case "comment":
                    case "ע��":
                    case "��ע":
                        return item.SubItems[ORIGIN_COLUMN_COMMENT].Text;

                    case "batchNo":
                    case "���κ�":
                    case "�����κ�":
                        return item.SubItems[ORIGIN_COLUMN_BATCHNO].Text;

                    case "biblioRecpath":
                    case "�ּ�¼·��":
                        return item.SubItems[ORIGIN_COLUMN_BIBLIORECPATH].Text;

                    // ��ʽ���Ժ��������ַ
                    case "sellerAddress":
                    case "������ַ":
                        return GetPrintableSellerAddress(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            strParameters);

                    case "sellerAddress:zipcode":
                    case "������ַ:��������":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "zipcode");

                    case "sellerAddress:address":
                    case "������ַ:��ַ":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "address");

                    case "sellerAddress:department":
                    case "������ַ:��λ":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "department");

                    case "sellerAddress:name":
                    case "������ַ:��ϵ��":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "name");

                    case "sellerAddress:email":
                    case "������ַ:Email��ַ":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "email");

                    case "sellerAddress:bank":
                    case "������ַ:������":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "bank");

                    case "sellerAddress:accounts":
                    case "������ַ:�����˺�":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "accounts");

                    case "sellerAddress:payStyle":
                    case "������ַ:��ʽ":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "payStyle");

                    case "sellerAddress:comment":
                    case "������ַ:��ע":
                        return GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "comment");
                    default:
                        {
                            // 2013/3/29
                            if (this.ColumnTable.Contains(strName) == false)
                                return "δ֪��Ŀ '" + strName + "'";

                            return (string)this.ColumnTable[strName];
                        }
                }
            }

            catch
            {
                return null;    // ��ʾû�����subitem�±�
            }

        }

        int BuildOriginPageBottom(PrintOption option,
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
        "<div class='pagefooter'>" + HttpUtility.HtmlEncode(strPageFooterText) + "</div>");
            }


            StreamUtil.WriteText(strFileName, "</body></html>");

            return 0;
        }

        static int GetOriginBiblioCount(List<ListViewItem> items)
        {
            List<string> paths = new List<string>();

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strText = "";

                try
                {
                    strText = item.SubItems[ORIGIN_COLUMN_BIBLIORECPATH].Text;
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

        // ���ԭʼ�б��е������ܺ�
        static int GetOriginTotalSeries(List<ListViewItem> items)
        {
            int total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strCopy = "";
                strCopy = item.SubItems[ORIGIN_COLUMN_COPY].Text;
                    // TODO: ע�����Ƿ���[]����?

                string strLeftCopy = OrderDesignControl.GetCopyFromCopyString(strCopy);
                int nLeftCopy = 0;
                try
                {
                    nLeftCopy = Convert.ToInt32(strLeftCopy);
                }
                catch
                {
                    continue;
                }

                total += nLeftCopy;
            }

            return total;
        }

        // ���ԭʼ�б��еĲ����ܺ�
        static int GetOriginTotalCopies(List<ListViewItem> items)
        {
            int total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strCopy = "";
                strCopy = item.SubItems[ORIGIN_COLUMN_COPY].Text;
                // TODO: ע�����Ƿ���[]����?

                string strLeftCopy = OrderDesignControl.GetCopyFromCopyString(strCopy);
                string strRightCopy = OrderDesignControl.GetRightFromCopyString(strCopy);
                int nLeftCopy = 0;
                try
                {
                    nLeftCopy = Convert.ToInt32(strLeftCopy);
                }
                catch
                {
                    continue;
                }

                int nRightCopy = 1;
                if (String.IsNullOrEmpty(strRightCopy) == false)
                {
                    try
                    {
                        nRightCopy = Convert.ToInt32(strRightCopy);
                    }
                    catch
                    {
                    }
                }

                total += nLeftCopy * nRightCopy;
            }

            return total;
        }

        static decimal GetOriginTotalPrice(List<ListViewItem> items)
        {
            decimal total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strPrice = "";

                try
                {
                    strPrice = item.SubItems[ORIGIN_COLUMN_TOTALPRICE].Text;
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


        #endregion

        // �����ԭʼ���ݵ��޸�
        private void button_saveChange_saveChange_Click(object sender, EventArgs e)
        {
            // ��֯�������� SetOrders
            string strError = "";
            int nRet = SaveOrders(out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
            else
            {
                this.m_nSavedCount++;

                // ���� ������� ״̬
                if (this.checkBox_print_accepted.Checked == true)
                {
                    MessageBox.Show(this, "ע�⣬��ǰ״̬Ϊ ����ӡ�������������������Ķ����ݵ��޸ģ�δ�����Զ�����¼״̬�Ͷ���ʱ���ֶε��޸ġ�Ҳ����˵�����������޸ĺ�����¶������ݲ�δ��ɡ��Ѷ�����״̬��Ҳ�Ͳ�����������(ԭ���Ѿ���ӡ�������Ķ������ݲ���Ӱ�죬��������)��\r\n\r\n���Ҫ������ͨ������ӡ����Ҫ���ڱ����ڡ�װ�ء�����ҳ������ԡ�����������Ĺ�ѡ��Ȼ������װ������");
                }

                // ˢ��Origin����ǳ���ɫ
                if (this.SortColumns_origin.Count > 0)
                {
                    SetGroupBackcolor(
                        this.listView_origin,
                        this.SortColumns_origin[0].No);
                }

                this.SetNextButtonEnable();
            }
        }

        // ȥ����һ���ַ�������ַ��Ǳ�ʾ�����Ƿ��޸Ĺ�
        static string RemoveChangedChar(string strText)
        {
            if (string.IsNullOrEmpty(strText) == true)
                return strText;
            if (strText[0] == '*')
                return strText.Substring(1);
            return strText;
        }

        static void RemoveChangedChar(ListViewItem item, int nCol)
        {
            ListViewUtil.ChangeItemText(item, nCol,
                RemoveChangedChar(ListViewUtil.GetItemText(item, nCol)));
        }

        // �����ԭʼ������¼���޸�
        int SaveOrders(out string strError)
        {
            strError = "";
            int nRet = 0;

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ���ԭʼ��¼ ...");
            stop.BeginLoop();

            try
            {
                string strPrevBiblioRecPath = "";
                List<EntityInfo> entity_list = new List<EntityInfo>();
                for (int i = 0; i < this.listView_origin.Items.Count; i++)
                {
                    ListViewItem item = this.listView_origin.Items[i];

                    if (item.ImageIndex == TYPE_ERROR)
                    {
                        strError = "ԭʼ�����б��У��� " + (i+1).ToString() + " ������Ϊ����״̬����Ҫ���ų�������ܽ��б��档";
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
                    Debug.Assert(item.ImageIndex != TYPE_NORMAL, "data.Changed״̬Ϊtrue�����ImageIndex��ӦΪTYPE_NORMAL");

                    string strBiblioRecPath = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_BIBLIORECPATH);

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
                        strError = "order record XMLװ�ص�DOMʱ��������: " + ex.Message;
                        return -1;
                    }

                    DomUtil.SetElementText(dom.DocumentElement,
                        "state",
                        RemoveChangedChar(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_STATE)));
                    RemoveChangedChar(item, ORIGIN_COLUMN_STATE);

                    DomUtil.SetElementText(dom.DocumentElement,
                        "totalPrice",
                        RemoveChangedChar(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_TOTALPRICE)));
                    RemoveChangedChar(item, ORIGIN_COLUMN_TOTALPRICE);

                    string strOrderTime = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ORDERTIME);
                    if (string.IsNullOrEmpty(strOrderTime) == false)
                    {
                        DateTime order_time;
                        try
                        {
                            order_time = DateTime.Parse(strOrderTime);
                        }
                        catch (Exception ex)
                        {
                            strError = "�����ַ��� '" + strOrderTime + "' ��ʽ����" + ex.Message;
                            return -1;
                        }

                        DomUtil.SetElementText(dom.DocumentElement,
                            "orderTime",
                            // DateTimeUtil.Rfc1123DateTimeString(order_time.ToUniversalTime()));
                            DateTimeUtil.Rfc1123DateTimeStringEx(order_time));
                    }

                    DomUtil.SetElementText(dom.DocumentElement,
                        "orderID", 
                        ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ORDERID));

                    EntityInfo info = new EntityInfo();

                    if (String.IsNullOrEmpty(data.RefID) == true)
                    {
                        data.RefID = Guid.NewGuid().ToString();
                    }

                    info.RefID = data.RefID;
                    info.Action = "change";
                    info.OldRecPath = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_RECPATH);
                    info.NewRecPath = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_RECPATH);

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
            long lRet = Channel.SetOrders(
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
                    SetItemColor(item, TYPE_NORMAL);
                    continue;
                }

                if (errorinfos[0].ErrorCode == ErrorCodeValue.TimestampMismatch)
                {
                    // ʱ�����ͻ
                    if (String.IsNullOrEmpty(strErrorText) == false)
                        strErrorText += ";\r\n";
                    strErrorText += "���� '" + ListViewUtil.GetItemText(item, ORIGIN_COLUMN_RECPATH) + "' �ڱ�������г���ʱ�����ͻ��������װ��ԭʼ���ݣ�Ȼ������޸ĺͱ��档";
                }
                else
                {
                    // ��������
                    if (String.IsNullOrEmpty(strErrorText) == false)
                        strErrorText += ";\r\n";
                    strErrorText += "���� '" + ListViewUtil.GetItemText(item, ORIGIN_COLUMN_RECPATH) + "' �ڱ�������з�������: " + errorinfo.ErrorInfo;
                }

                ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ERRORINFO, errorinfo.ErrorInfo);
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
            for (int i = 0; i < this.listView_origin.Items.Count; i++)
            {
                item = this.listView_origin.Items[i];
                OriginItemData data = (OriginItemData)item.Tag;
                if (data.RefID == strRefID)
                    return data;
            }

            item = null;
            return null;
        }

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                for (int i = 0; i < this.listView_origin.Items.Count; i++)
                {
                    OriginItemData data = (OriginItemData)this.listView_origin.Items[i].Tag;
                    if (data.Changed == true)
                        return true;
                }

                return false;
            }
        }

        List<OutputProjectData> formats = new List<OutputProjectData>();


        int PrepareFormats(List<OutputItem> OutputItems,
            out string strError)
        {
            strError = "";

            // OutputProjectData���飬�ȸ��ݶԻ��򴴽�һ����������������᲻ȫ��Ҳ�п���ĳЩ������Ϊ����������û�г��ֶ��ò���
            // ��������������У��ٸ�����Ҫ����OutputProjectData������������������������϶����Ǿ���c#������������һЩ���ø�ʽ����(�����Ǹ�ʽ����<>���Ű���)

            this.formats.Clear();
            for (int i = 0; i < OutputItems.Count; i++)
            {
                OutputItem item = OutputItems[i];
                OutputProjectData format = new OutputProjectData();
                format.Seller = item.Seller;
                format.ProjectName = item.OutputFormat;

                if (String.IsNullOrEmpty(format.ProjectName) == true)
                    format.ProjectName = "<default>";

                if (format.ProjectName[0] != '<')
                {
                    // 
                    string strProjectLocate = "";
                    // ��÷�������
                    // strProjectNamePath	������������·��
                    // return:
                    //		-1	error
                    //		0	not found project
                    //		1	found
                    int nRet = this.ScriptManager.GetProjectData(
                        format.ProjectName,
                        out strProjectLocate);
                    if (nRet == 0)
                    {
                        strError = "���� " + format.ProjectName + " û���ҵ�...";
                        return -1;
                    }
                    if (nRet == -1)
                    {
                        strError = "scriptManager.GetProjectData() error ...";
                        return -1;
                    }

                    format.ProjectLocate = strProjectLocate;

                    OutputOrder objOutputOrder = null;
                    Assembly AssemblyMain = null;

                            // ׼���ű�����
                    nRet = PrepareScript(format.ProjectName,
                        format.ProjectLocate,
                        out objOutputOrder,
                        out AssemblyMain,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    format.Assembly = AssemblyMain;
                    format.OutputOrder = objOutputOrder;

                    // ����Assembly��ʼ������
                    format.OutputOrder.PrintOrderForm = this;
                    format.OutputOrder.PubType = this.comboBox_load_type.Text;
                    format.OutputOrder.DataDir = this.MainForm.DataDir;
                    format.OutputOrder.XmlFilename = "";    // ��δ�������
                    format.OutputOrder.OutputDir = "";  // ��δ�������
                    bool bRet = format.OutputOrder.Initial(out strError);
                    if (bRet == false)
                    {
                        strError = "��ʼ�������ʽ '" + format.ProjectName + "' ʧ��: " + strError;
                        return -1;
                    }

                }

                this.formats.Add(format);
            }

            return 0;
        }

        // �������
        private void button_print_outputOrder_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // �趨���������ʽ
            OrderFileDialog dlg = new OrderFileDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.ScriptManager = this.ScriptManager;
            dlg.AppInfo = this.MainForm.AppInfo;
            dlg.DataDir = this.MainForm.DataDir;

            string strPrefix = "";
            if (this.comboBox_load_type.Text == "ͼ��")
                strPrefix = "book";
            else
                strPrefix = "series";

            dlg.GetValueTable -= new GetValueTableEventHandler(dlg_GetValueTable);
            dlg.GetValueTable += new GetValueTableEventHandler(dlg_GetValueTable);

            dlg.CfgFileName = PathUtil.MergePath(this.MainForm.DataDir, strPrefix + "_order_output_def.xml");   // ��ʽ������Ϣ��Ҫ�ֳ��������͵�
            dlg.Text = this.comboBox_load_type.Text + " ���������ʽ";
            dlg.RunMode = true;
            // dlg.StartPosition = FormStartPosition.CenterScreen;
            this.MainForm.AppInfo.LinkFormState(dlg, "printorder_outputorder_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            // OutputProjectData���飬�ȸ��ݶԻ��򴴽�һ����������������᲻ȫ��Ҳ�п���ĳЩ������Ϊ����������û�г��ֶ��ò���
            // ��������������У��ٸ�����Ҫ����OutputProjectData������������������������϶����Ǿ���c#������������һЩ���ø�ʽ����(�����Ǹ�ʽ����<>���Ű���)
            nRet = PrepareFormats(dlg.OutputItems,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // �������
            nRet = OutputOrder(dlg.OutputFolder,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // �򿪶������Ŀ¼�ļ���
            DialogResult result = MessageBox.Show(this,
                "���������ɡ���������� "+nRet.ToString()+" ����\r\n\r\n�Ƿ������򿪶������Ŀ¼�ļ���? ",
                "PrintOrderForm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(dlg.OutputFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message);
                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ���ѡ��
        private void button_print_outputOrderOption_Click(object sender, EventArgs e)
        {
            // string strError = "";
            // int nRet = 0;

            // �趨���������ʽ
            OrderFileDialog dlg = new OrderFileDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.ScriptManager = this.ScriptManager;
            dlg.AppInfo = this.MainForm.AppInfo;
            dlg.DataDir = this.MainForm.DataDir;

            string strPrefix = "";
            if (this.comboBox_load_type.Text == "ͼ��")
                strPrefix = "book";
            else
                strPrefix = "series";

            dlg.GetValueTable -= new GetValueTableEventHandler(dlg_GetValueTable);
            dlg.GetValueTable += new GetValueTableEventHandler(dlg_GetValueTable);

            dlg.CfgFileName = PathUtil.MergePath(this.MainForm.DataDir, strPrefix + "_order_output_def.xml");   // ��ʽ������Ϣ��Ҫ�ֳ��������͵�
            dlg.Text = "���� " + this.comboBox_load_type.Text + " ���������ʽ";
            dlg.RunMode = false;
            // dlg.StartPosition = FormStartPosition.CenterScreen;
            this.MainForm.AppInfo.LinkFormState(dlg, "printorder_outputorder_potion_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult != DialogResult.OK)
                return;

            return;
            /*
        ERROR1:
            MessageBox.Show(this, strError);
             * */
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

        private void scriptManager_CreateDefaultContent(object sender, CreateDefaultContentEventArgs e)
        {
            string strPureFileName = Path.GetFileName(e.FileName);

            if (String.Compare(strPureFileName, "main.cs", true) == 0)
            {
                CreateDefaultMainCsFile(e.FileName);
                e.Created = true;
            }
                /*
            else if (String.Compare(strPureFileName, "marcfilter.fltx", true) == 0)
            {
                CreateDefaultMarcFilterFile(e.FileName);
                e.Created = true;
            }*/
            else
            {
                e.Created = false;
            }

        }

        // ����ȱʡ��main.cs�ļ�
        /// <summary>
        /// ����ȱʡ�� main.cs �ļ�
        /// </summary>
        /// <param name="strFileName">�ļ���ȫ·��</param>
        public static void CreateDefaultMainCsFile(string strFileName)
        {
            StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8);
            sw.WriteLine("using System;");
            sw.WriteLine("using System.Windows.Forms;");
            sw.WriteLine("using System.IO;");
            sw.WriteLine("using System.Text;");
            sw.WriteLine("using System.Xml;");
            sw.WriteLine("");

            //sw.WriteLine("using DigitalPlatform.MarcDom;");
            //sw.WriteLine("using DigitalPlatform.Statis;");
            sw.WriteLine("using dp2Circulation;");

            sw.WriteLine("using DigitalPlatform.Xml;");


            sw.WriteLine("public class MyOutputOrder : OutputOrder");

            sw.WriteLine("{");

            sw.WriteLine("	public override void Output()");
            sw.WriteLine("	{");
            sw.WriteLine("	}");


            sw.WriteLine("}");
            sw.Close();
        }

        // ɾ�����Ŀ¼�е�ȫ���ļ�
        void DeleteAllFiles(string strDir)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(strDir);

                FileInfo[] fis = di.GetFiles();

                if (fis.Length == 0)
                    return;

                // ����Ҫɾ��
                DialogResult result = MessageBox.Show(this,
                    "�������ǰ��ȷʵҪɾ�����Ŀ¼ "+strDir+" �����е�ȫ�� "+fis.Length.ToString()+" ���ļ�?",
                    "PrintOrderForm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return;
  
                for (int i = 0; i < fis.Length; i++)
                {
                    try
                    {
                        File.Delete(fis[i].FullName);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        // ������Ӷ���
        int OutputOrder(
            string strOutputDir,
            out string strError)
        {
            strError = "";

            if (this.listView_merged.Items.Count == 0)
            {
                strError = "���ϲ����б���û���κ�����޶������ݿ����";
                return -1;
            }

            // ȷ��Ŀ¼����
            PathUtil.CreateDirIfNeed(strOutputDir);

            // ��ʾ�Ƿ�ɾ�����Ŀ¼�е������ļ�
            // �������Ϊ��ǰ���ݼ�¼����ɾ������������ɾ���ܶ����õ��ļ�
            if (this.MainForm.DataDir.ToLower() != strOutputDir.ToLower())
                DeleteAllFiles(strOutputDir);

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����������� ...");
            stop.BeginLoop();

            NamedListViewItemsCollection lists = new NamedListViewItemsCollection();

            try
            {

                int nErrorCount = 0;

                this.tabControl_items.SelectedTab = this.tabPage_mergedItems;

                // �ȼ���Ƿ��д������˳�㹹��item�б�
                List<ListViewItem> items = new List<ListViewItem>();

                stop.SetMessage("���ڹ���Item�б�...");
                for (int i = 0; i < this.listView_merged.Items.Count; i++)
                {
                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�1";
                            return -1;
                        }
                    }

                    ListViewItem item = this.listView_merged.Items[i];

                    if (item.ImageIndex == TYPE_ERROR)
                        nErrorCount++;

                    lists.AddItem(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                        item);
                }

                if (nErrorCount != 0)
                {
                    MessageBox.Show(this, "���棺��������Ķ��������� " + nErrorCount.ToString() + " ���д�����Ϣ�����");
                }

                for (int i = 0; i < lists.Count; i++)
                {
                    string strOutputFilename = PathUtil.MergePath(strOutputDir, lists[i].Seller + ".xml");

                    int nRet = OutputOneOrder(lists[i],
                        strOutputDir,
                        strOutputFilename,
                        out strError);
                    if (nRet == -1)
                        return -1;

                }

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("���������ɡ���������� " + lists.Count.ToString() + " ��");
                stop.HideProgress();

                EnableControls(true);
            }

            return lists.Count;
        }

        // ����й�һ�������Ķ���
        int OutputOneOrder(NamedListViewItems items,
            string strOutputDir,
            string strOutputFilename,
            out string strError)
        {
            strError = "";

            XmlTextWriter writer = new XmlTextWriter(strOutputFilename, Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.Indentation = 4;

            writer.WriteStartDocument();
            writer.WriteStartElement("order");

            string strSeller = items.Seller;

            writer.WriteStartElement("generalInfo");

            // ������
            writer.WriteElementString("seller", strSeller);
            // ��������
            writer.WriteElementString("createDate", DateTime.Now.ToLongDateString());
            // ���κ�
            writer.WriteElementString("batchNo", this.BatchNo);
            // ��¼·���ļ��� ȫ·��
            writer.WriteElementString("recPathFilename", this.RecPathFilePath);

            writer.WriteEndElement();

            // ͳ����Ϣ
            {
                writer.WriteStartElement("statisInfo");

                int nItemCount = items.Count;
                int nTotalSeries = GetMergedTotalSeries(items);
                int nTotalCopies = GetMergedTotalCopies(items);
                int nBiblioCount = GetMergedBiblioCount(items);
                string strTotalPrice = GetMergedTotalPrice(items);

                // ������
                writer.WriteElementString("itemCount", nItemCount.ToString());
                // ������(ע��ÿ������ж��ס�һ���ڿ����ж��)
                writer.WriteElementString("totalseries", nTotalSeries.ToString());
                // �ܲ���(ע��һ���ڿ����ж��)
                writer.WriteElementString("totalcopies", nTotalCopies.ToString());
                // ����
                writer.WriteElementString("titleCount", nBiblioCount.ToString());
                // �ܼ۸� ����Ϊ������ֵļ۸�����̬
                writer.WriteElementString("totalPrice", strTotalPrice);

                writer.WriteEndElement();
            }

            stop.SetProgressRange(0, items.Count);

            // ������
            for (int i = 0; i < items.Count; i++)
            {
                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�2";
                        return -1;
                    }
                }

                ListViewItem item = items[i];

                writer.WriteStartElement("item");

                // ��ţ���0��ʼ
                writer.WriteAttributeString("index", i.ToString());

                // catalogNo
                writer.WriteElementString("catalogNo",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_CATALOGNO));

                // summary
                writer.WriteElementString("summary",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_SUMMARY));

                // isbn/issn
                writer.WriteElementString("isbnIssn",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_ISBNISSN));

                // merge comment
                writer.WriteElementString("mergeComment",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_MERGECOMMENT));

                // range
                writer.WriteElementString("range",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_RANGE));

                // issue count
                writer.WriteElementString("issueCount",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_ISSUECOUNT));

                // copy
                writer.WriteElementString("copy",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_COPY));

                // price
                writer.WriteElementString("price",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_PRICE));

                // total price
                writer.WriteElementString("totalPrice",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_TOTALPRICE));

                // order time
                writer.WriteElementString("orderTime",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_ORDERTIME));

                // order ID
                writer.WriteElementString("orderID",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_ORDERID));

                // distribute
                writer.WriteElementString("distribute",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_DISTRIBUTE));

                // class
                writer.WriteElementString("class",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_CLASS));

                // comment
                writer.WriteElementString("comment",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_COMMENT));

                // biblio recpath
                writer.WriteElementString("biblioRecpath",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_BIBLIORECPATH));

                writer.WriteEndElement();   // of item

                stop.SetMessage("����������� " + strSeller + " " + (i + 1).ToString());

                stop.SetProgressValue(i);
            }

            writer.WriteEndElement();   // of order
            writer.WriteEndDocument();
            writer.Close();
            writer = null;


            // ִ�нű�
            OutputProjectData format = GetFormat(strSeller);
            if (format == null)
                return 0;   // ȱʡ��ʽ���Ƿ���Ҫ�����ݽ�������أ�

            // ���õĸ�ʽ
            if (format.ProjectName[0] == '<')
            {
                if (format.ProjectName == "<default>"
                    || format.ProjectName == "<ȱʡ>")
                    return 0;

                strError = "δ֪�����ø�ʽ '" + format.ProjectName + "'";
                return -1;
            }

            // ����Script

            try
            {
                Debug.Assert(format.OutputOrder != null, "");
                Debug.Assert(format.Assembly != null, "");

                format.OutputOrder.XmlFilename = strOutputFilename;
                format.OutputOrder.Seller = format.Seller;
                format.OutputOrder.DataDir = this.MainForm.DataDir;
                format.OutputOrder.OutputDir = strOutputDir;
                format.OutputOrder.PubType = this.comboBox_load_type.Text;

                // ִ�нű���Output()
                format.OutputOrder.Output();
            }
            catch (Exception ex)
            {
                strError = "�ű�ִ�й����׳��쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                return -1;
            }

            return 0;
        }

        /*
        // ����й�һ�������Ķ���
        int OutputOneOrder(NamedListViewItems items,
            string strOutputFilename,
            out string strError)
        {
            strError = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<order />");

            string strSeller = items.Seller;

            // ������
            DomUtil.SetElementText(dom.DocumentElement,
                "seller", strSeller);
            // ��������
            DomUtil.SetElementText(dom.DocumentElement,
                "createDate", DateTime.Now.ToLongDateString());
            // ���κ�
            DomUtil.SetElementText(dom.DocumentElement, 
                "batchNo", this.BatchNo);
            // ��¼·���ļ��� ȫ·��
            DomUtil.SetElementText(dom.DocumentElement,
                "recPathFilename", this.RecPathFilePath);

            // ͳ����Ϣ
            {
                int nItemCount = items.Count;
                int nTotalCopies = GetMergedTotalCopies(items);
                int nBiblioCount = GetMergedBiblioCount(items);
                string strTotalPrice = GetMergedTotalPrice(items);

                // ������
                DomUtil.SetElementText(dom.DocumentElement,
                    "itemCount", nItemCount.ToString());
                // �ܲ���(ע��ÿ������ж��)
                DomUtil.SetElementText(dom.DocumentElement,
                    "totalcopies", nTotalCopies.ToString());
                // ����
                DomUtil.SetElementText(dom.DocumentElement,
                   "titleCount", nBiblioCount.ToString());
                // �ܼ۸� ����Ϊ������ֵļ۸�����̬
                DomUtil.SetElementText(dom.DocumentElement,
                   "totalPrice", strTotalPrice);
            }

            stop.SetProgressRange(0, items.Count);

            // ������
            for (int i = 0; i < items.Count; i++)
            {
                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�2";
                        return -1;
                    }
                }

                ListViewItem item = items[i];

                XmlNode node = dom.CreateElement("item");
                dom.DocumentElement.AppendChild(node);

                // ��ţ���0��ʼ
                DomUtil.SetAttr(node, "index", i.ToString());

                // catalogNo
                DomUtil.SetElementText(node,
                    "catalogNo",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_CATALOGNO));

                // summary
                DomUtil.SetElementText(node,
                    "summary",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_SUMMARY));

                // isbn/issn
                DomUtil.SetElementText(node,
                    "isbnIssn",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_ISBNISSN));

                // merge comment
                DomUtil.SetElementText(node,
                    "mergeComment",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_MERGECOMMENT));

                // range
                DomUtil.SetElementText(node,
                    "range",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_RANGE));

                // issue count
                DomUtil.SetElementText(node,
                    "issueCount",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_ISSUECOUNT));

                // copy
                DomUtil.SetElementText(node,
                    "copy",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_COPY));

                // price
                DomUtil.SetElementText(node,
                    "price",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_PRICE));
                    
                // total price
                DomUtil.SetElementText(node,
                    "totalPrice",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_TOTALPRICE));

                // order time
                DomUtil.SetElementText(node,
                    "orderTime",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_ORDERTIME));

                // order ID
                DomUtil.SetElementText(node,
                    "orderID",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_ORDERID));

                // distribute
                DomUtil.SetElementText(node,
                    "distribute",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_DISTRIBUTE));

                // class
                DomUtil.SetElementText(node,
                    "class",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_CLASS));

                // comment
                DomUtil.SetElementText(node,
                    "comment",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_COMMENT));

                // biblio recpath
                DomUtil.SetElementText(node,
                    "biblioRecpath",
                    ListViewUtil.GetItemText(item, MERGED_COLUMN_BIBLIORECPATH));

                stop.SetMessage("����������� " + strSeller + " " + (i + 1).ToString());

                stop.SetProgressValue(i);
            }

            dom.Save(strOutputFilename);

            // ִ�нű�
            OutputProjectData format = GetFormat(strSeller);
            if (format == null)
                return 0;   // ȱʡ��ʽ���Ƿ���Ҫ�����ݽ�������أ�

            // ���õĸ�ʽ
            if (format.ProjectName[0] == '<')
            {
                if (format.ProjectName == "<default>"
                    || format.ProjectName == "<ȱʡ>")
                    return 0;

                strError = "δ֪�����ø�ʽ '" + format.ProjectName + "'";
                return -1;
            }

            // ����Script

            try
            {
                Debug.Assert(format.OutputOrder != null, "");
                Debug.Assert(format.Assembly != null, "");

                format.OutputOrder.XmlFilename = strOutputFilename;
                format.OutputOrder.Seller = format.Seller;
                format.OutputOrder.DataDir = this.MainForm.DataDir;

                // ִ�нű���Output()
                format.OutputOrder.Output();
            }
            catch (Exception ex)
            {
                strError = "�ű�ִ�й����׳��쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                return -1;
            }

            return 0;
        }
         * */

        OutputProjectData GetFormat(string strSeller)
        {
            for (int i = 0; i < this.formats.Count; i++)
            {
                if (strSeller == formats[i].Seller)
                    return formats[i];
            }

            return null;
        }

        // ׼���ű�����
        int PrepareScript(string strProjectName,
            string strProjectLocate,
            out OutputOrder objOutputOrder,
            out Assembly AssemblyMain,
            out string strError)
        {
            AssemblyMain = null;
            objOutputOrder = null;
            strError = "";

            string strWarning = "";
            string strMainCsDllName = "";

            for (int AssemblyVersion = 0; ; AssemblyVersion++)
            {
                strMainCsDllName = strProjectLocate + "\\~output_order_main_" + this.GetHashCode().ToString() + "_"+ Convert.ToString(AssemblyVersion++) + ".dll";    // ++
                bool bFound = false;
                for (int i = 0; i < UsedAssemblyFilenames.Count; i++)
                {
                    string strName = this.UsedAssemblyFilenames[i];
                    if (strMainCsDllName == strName)
                    {
                        bFound = true;
                        break;
                    }
                }
                if (bFound == false)
                {
                    this.UsedAssemblyFilenames.Add(strMainCsDllName);
                    break;
                }
            }


            string strLibPaths = "\"" + this.MainForm.DataDir + "\""
                + ","
                + "\"" + strProjectLocate + "\"";

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
									Environment.CurrentDirectory + "\\dp2circulation.exe",
            };


            // ����Project��Script main.cs��Assembly
            // return:
            //		-2	���������Ѿ���ʾ��������Ϣ�ˡ�
            //		-1	����
            int nRet = ScriptManager.BuildAssembly(
                "PrintOrderForm",
                strProjectName,
                "main.cs",
                saAddRef,
                strLibPaths,
                strMainCsDllName,
                out strError,
                out strWarning);
            if (nRet == -2)
                goto ERROR1;
            if (nRet == -1)
            {
                if (strWarning == "")
                    goto ERROR1;
                MessageBox.Show(this, strWarning);
            }

            AssemblyMain = Assembly.LoadFrom(strMainCsDllName);
            if (AssemblyMain == null)
            {
                strError = "LoadFrom " + strMainCsDllName + " fail";
                goto ERROR1;
            }

            // �õ�Assembly��XmlStatis������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                AssemblyMain,
                "dp2Circulation.OutputOrder");
            if (entryClassType == null)
            {
                strError = strMainCsDllName + "��û���ҵ� dp2Circulation.OutputOrder �������ࡣ";
                goto ERROR1;
            }
            // newһ��OutputOrder��������
            objOutputOrder = (OutputOrder)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            // ΪOutputOrder���������ò���
            objOutputOrder.PrintOrderForm = this;
            objOutputOrder.ProjectDir = strProjectLocate;

            return 0;
        ERROR1:
            return -1;
        }

        // �����������͸ı��Ҫ����������б������
        private void comboBox_load_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.listView_origin.Items.Clear();
            this.SortColumns_origin.Clear();
            SortColumns.ClearColumnSortDisplay(this.listView_origin.Columns);


            this.listView_merged.Items.Clear();
            this.SortColumns_merged.Clear();
            SortColumns.ClearColumnSortDisplay(this.listView_merged.Columns);
        }

        private void PrintOrderForm_Activated(object sender, EventArgs e)
        {
            // 2009/8/13 new add
            this.MainForm.stopManager.Active(this.stop);

        }

        private void checkBox_print_accepted_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_print_accepted.Checked == true)
            {
                // TODO���Ƿ���������װ�ػ���ˢ�£� ��Ϊ��Щ�п���û��װ�������
                this.button_print_printOrderList.Text = "��ӡ��������嵥(&P)...";
                this.button_print_arriveRatioStatis.Enabled = true;
            }
            else
            {
                this.button_print_printOrderList.Text = "��ӡ����(&P)...";
                this.button_print_arriveRatioStatis.Enabled = false;
            }
        }

        private void listView_origin_DoubleClick(object sender, EventArgs e)
        {
            LoadOrderToEntityForm(this.listView_origin);
        }

        #region �����ʷ�ʱ��Ƭͳ��

        // ������ͳ��
        private void button_print_arriveRatioStatis_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = PrintArriveRatio("html", out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        // ������ͳ��
        // parameters:
        //      strStyle    excel / html ֮һ���߶���������ϡ� excel: ��� Excel �ļ�
        int PrintArriveRatio(
            string strStyle,
            out string strError)
        {
            strError = "";
            int nErrorCount = 0;

            ExcelDocument doc = null;

            if (StringUtil.IsInList("excel", strStyle) == true)
            {
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
                    return 0;

                this.ExportExcelFilename = dlg.FileName;

#if NO
                SpreadsheetDocument spreadsheetDocument = null;
                spreadsheetDocument = SpreadsheetDocument.Create(this.ExportExcelFilename, SpreadsheetDocumentType.Workbook);

                doc = new ExcelDocument(spreadsheetDocument);
#endif
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
            }

            this.tabControl_items.SelectedTab = this.tabPage_mergedItems;

            EnableControls(false);
            // MainForm.ShowProgress(true);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڹ鲢����ʱ�� ...");
            stop.BeginLoop();

            try
            {
                NamedListViewItemsCollection lists = new NamedListViewItemsCollection();

                DateTime last_ordertime = new DateTime(0);
                // �ȼ���Ƿ��д������˳�㹹��item�б�
                // List<ListViewItem> items = new List<ListViewItem>();
                for (int i = 0; i < this.listView_merged.Items.Count; i++)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    ListViewItem item = this.listView_merged.Items[i];

                    if (item.ImageIndex == TYPE_ERROR)
                        nErrorCount++;

                    // ��������Ķ���ʱ��
                    string strOrderTime = ListViewUtil.GetItemText(item, MERGED_COLUMN_ORDERTIME);
                    if (string.IsNullOrEmpty(strOrderTime) == false)
                    {
                        DateTime order_time;
                        try
                        {
                            order_time = DateTime.Parse(strOrderTime);
                        }
                        catch (Exception ex)
                        {
                            strError = "�� "+(i+1).ToString()+" �����ַ��� '" + strOrderTime + "' ��ʽ����" + ex.Message;
                            goto ERROR1;
                        }

                        if (last_ordertime == new DateTime(0))
                            last_ordertime = order_time;
                        else
                        {
                            if (order_time < last_ordertime)
                                last_ordertime = order_time;
                        }
                    }

                    lists.AddItem(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                        item);
                }

                if (nErrorCount != 0)
                {
                    MessageBox.Show(this, "���棺�����ӡ�����嵥������ " + nErrorCount.ToString() + " ������������Ϣ�����");
                }

                DateSliceDialog dlg = new DateSliceDialog();
                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.OrderDate = last_ordertime;
                dlg.StartTime = last_ordertime;
                dlg.EndTime = DateTime.Now;
                dlg.QuickSet = "������������";
                dlg.Slice = this.MainForm.AppInfo.GetString(
                    "printorder_form",
                    "slice",
                    "��");
                dlg.ShowDialog(this);

                if (dlg.DialogResult != System.Windows.Forms.DialogResult.OK)
                    return 0;

                this.MainForm.AppInfo.SetString(
                    "printorder_form",
                    "slice",
                    dlg.Slice);

                List<TimeSlice> time_ranges = dlg.TimeSlices;

                List<string> filenames = new List<string>();
                try
                {
                    // ������ͳ�Ƶ�����
                    for (int i = 0; i < lists.Count; i++)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null && stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }

                        List<string> temp_filenames = null;
                        int nRet = BuildArriveHtml(lists[i],
                            ref doc,
                            time_ranges,
                            out temp_filenames,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        filenames.AddRange(temp_filenames);
                    }

                    if (doc == null)
                    {
                        HtmlPrintForm printform = new HtmlPrintForm();

                        printform.Text = "������ͳ��";
                        printform.MainForm = this.MainForm;
                        printform.Filenames = filenames;
                        this.MainForm.AppInfo.LinkFormState(printform, "printorder_htmlprint_formstate");
                        printform.ShowDialog(this);
                        this.MainForm.AppInfo.UnlinkFormState(printform);
                    }
                }
                finally
                {
                    if (filenames != null)
                    {
                        Global.DeleteFiles(filenames);
                        filenames.Clear();
                    }
                }
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

            if (doc != null)
            {
                // Close the document.
                doc.Close();
            }

            return 1;
        ERROR1:
            // MessageBox.Show(this, strError);
            return -1;
        }

        // ÿһ���ϲ����ڵ�����ͳ�ƵĹؼ���Ϣ
        class OneLine
        {
            public ListViewItem Item = null;
            public List<OneBook> Books = null;
            public DateTime OrderTime = new DateTime(0);
        }

        // ÿһ������Ϣ
        class OneBook
        {
            public string RefID = "";
            public DateTime CreateTime = new DateTime(0);
        }


        // ���ȫ�����¼�� refid --> ����ʱ�� ���ձ���ν����ʱ����Ǽ�¼������ʱ�䡣���û�м�¼����ʱ�䣬������޸�ʱ�����
        // ��refid --> ����ʱ��
        int GetArriveTimes(NamedListViewItems items,
            out List<OneLine> infos,
            out string strError)
        {
            strError = "";
            infos = new List<OneLine>();
            int nRet = 0;

            foreach( ListViewItem item in items)
            {
                string strDistributes = item.SubItems[MERGED_COLUMN_DISTRIBUTE].Text;
                if (string.IsNullOrEmpty(strDistributes) == true)
                    continue;

                LocationCollection locations = new LocationCollection();
                nRet = locations.Build(strDistributes,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�ݲط����ַ��� '" + strDistributes + "' ��ʽ����: " + strError;
                    return -1;
                }

                OneLine info = new OneLine();
                info.Item = item;
                info.Books = new List<OneBook>();

                infos.Add(info);
                List<string> refids = locations.GetRefIDs();
                foreach (string s in refids)
                {
                    OneBook book = new OneBook();
                    book.RefID = s;
                    info.Books.Add(book);
                }
            }

            Hashtable table = new Hashtable();
            List<string> temp_refids = new List<string>();
            foreach (OneLine line in infos)
            {
                if (line.Books == null)
                    continue;
                foreach(OneBook book in line.Books)
                {
                    if (string.IsNullOrEmpty(book.RefID) == true)
                        continue;
                    temp_refids.Add(book.RefID);
                }

                if (temp_refids.Count >= 100)
                {
                    nRet = GetRecordTimes(temp_refids,
            ref table,
            out strError);
                    if (nRet == -1)
                        return -1;
                    temp_refids.Clear();
                }
            }

            // ���һ��
            if (temp_refids.Count > 0)
            {
                nRet = GetRecordTimes(temp_refids,
        ref table,
        out strError);
                if (nRet == -1)
                    return -1;
                temp_refids.Clear();
            }

            // װ��ṹ
            foreach (OneLine line in infos)
            {
                if (line.Books == null)
                    continue;
                foreach (OneBook book in line.Books)
                {
                    if (string.IsNullOrEmpty(book.RefID) == true)
                        continue;
                    object obj = table[book.RefID];
                    if (obj == null)
                        continue;   // ��ǰ�������ֹ�refidû�ж�Ӧ���¼�������book.CreateTime�о���ȱʡֵ
                    DateTime time = (DateTime)obj;
                    book.CreateTime = time;
                }
            }

            return 0;
        }

        // ���� refid ���һ����¼�Ĵ���ʱ�䣬׷�ӵ� Hashtable ��
        int GetRecordTimes(List<string> refids,
            ref Hashtable result_table,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (result_table == null)
                result_table = new Hashtable();

            Hashtable table = null;
            // ��ò��¼��Ϣ
            nRet = LoadItemRecord(
                refids,
                ref table,
                out strError);
            if (nRet == -1)
                return -1;
            foreach (string key in table.Keys)
            {
                string strXml = (string)table[key];
                if (string.IsNullOrEmpty(strXml) == true)
                {
                    Debug.Assert(false, "");
                    continue;
                }
                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "item xml load to dom error: " + ex.Message;
                    return -1;
                }

                XmlNode node = dom.DocumentElement.SelectSingleNode("operations/operation[@name='create']");
                if (node == null)
                    node = dom.DocumentElement.SelectSingleNode("operations/operation");

                if (node == null)
                    continue;
                string strTime = DomUtil.GetAttr(node, "time");
                try
                {
                    DateTime time = DateTimeUtil.FromRfc1123DateTimeString(strTime);
                    result_table[key] = time;
                }
                catch(Exception /*ex*/)
                {
                    strError = "refidΪ '"+key+"' �ļ�¼��RFC1123�ַ��� '"+strTime+"' ��ʽ����ȷ";
                    return -1;
                }

            }

            return 0;
        }

        class OneItemRecord
        {
            public string RefID = "";
            public string RecPath = "";
            public string Xml = "";
        }

        // ���ݲ��¼refid��ת��Ϊ���¼��recpath��Ȼ���ò��¼XML
        int LoadItemRecord(
            List<string> refids,
            ref Hashtable table,
            out string strError)
        {
            strError = "";
            if (table == null)
                table = new Hashtable();

        REDO_GETITEMINFO:
            string strBiblio = "";
            string strResult = "";
            long lRet = this.Channel.GetItemInfo(stop,
                "@refid-list:" + StringUtil.MakePathList(refids),
                "get-path-list",
                out strResult,
                "", // strBiblioType,
                out strBiblio,
                out strError);
            if (lRet == -1)
                return -1;

            List<string> recpaths = StringUtil.SplitList(strResult);
            Debug.Assert(refids.Count == recpaths.Count, "");

            List<OneItemRecord> records = new List<OneItemRecord>(); 
            List<string> notfound_refids = new List<string>();
            List<string> errors = new List<string>();
            {
                int i = 0;
                foreach (string recpath in recpaths)
                {
                    if (string.IsNullOrEmpty(recpath) == true)
                        notfound_refids.Add(refids[i]);
                    else if (recpath[0] == '!')
                        errors.Add(recpath.Substring(1));
                    else
                    {
                        OneItemRecord record = new OneItemRecord();
                        record.RefID = refids[i];
                        record.RecPath = recpath;
                        records.Add(record);
                    }
                    i++;
                }
            }

            if (errors.Count > 0)
            {
                strError = "ת���ο�ID�Ĺ��̷�������: " + StringUtil.MakePathList(errors);

                DialogResult temp_result = MessageBox.Show(this,
strError + "\r\n\r\n�Ƿ�����?",
"PrintOrderForm",
MessageBoxButtons.RetryCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (temp_result == DialogResult.Retry)
                    goto REDO_GETITEMINFO;
                return -1;
            }

            if (notfound_refids.Count > 0)
            {
                if (string.IsNullOrEmpty(strError) == false)
                    strError += ";\r\n";

                strError += "���в��¼�ο�IDû���ҵ�: " + StringUtil.MakePathList(notfound_refids);
                DialogResult temp_result = MessageBox.Show(this,
strError + "\r\n\r\n�Ƿ��������?",
"PrintOrderForm",
MessageBoxButtons.OKCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (temp_result == DialogResult.Cancel)
                    return -1;
            }

            // ������ò��¼
            List<string> item_recpaths = new List<string>();
            foreach (OneItemRecord record in records)
            {
                if (String.IsNullOrEmpty(record.RecPath) == false)
                    item_recpaths.Add(record.RecPath);
            }

            if (item_recpaths.Count > 0)
            {
                // ��¼·�� --> XML��¼��
                Hashtable result_table = new Hashtable();
                List<DigitalPlatform.CirculationClient.localhost.Record> results = new List<DigitalPlatform.CirculationClient.localhost.Record>();

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

                    string[] paths = new string[item_recpaths.Count];
                    item_recpaths.CopyTo(paths);
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

                    foreach (DigitalPlatform.CirculationClient.localhost.Record record in searchresults)
                    {
                        if (record.RecordBody == null || string.IsNullOrEmpty(record.Path) == true)
                            continue;

                        result_table[record.Path] = record.RecordBody.Xml;
                    }

                    item_recpaths.RemoveRange(0, searchresults.Length);

                    if (item_recpaths.Count == 0)
                        break;
                }

                // ���ú�XML�ַ���
                foreach (OneItemRecord record in records)
                {
                    if (String.IsNullOrEmpty(record.RecPath) == true)
                        continue;

                    string strXml = (string)result_table[record.RecPath];
                    record.Xml = strXml;
                }

                // ������table��
                foreach (OneItemRecord record in records)
                {
                    if (String.IsNullOrEmpty(record.RecPath) == true
                        || string.IsNullOrEmpty(record.Xml) == true)
                        continue;

                    table[record.RefID] = record.Xml;
                }
            }

            return 0;
        }

        // �����ض�ʱ�䷶Χ�ڵ�����ֲ���
        static int GetValues(DateTime start,
            DateTime end,
            List<OneLine> infos,
            out long lBiblioCount,
            out long lItemCount,
            out string strError)
        {
            strError = "";
            lBiblioCount = 0;
            lItemCount = 0;

            foreach (OneLine line in infos)
            {
                if (line.Books == null)
                    continue;
                long lCurItemCount = 0;
                foreach (OneBook book in line.Books)
                {
                    if (book.CreateTime >= start && book.CreateTime < end)
                    {
                        lCurItemCount++;
                    }
                }
                if (lCurItemCount > 0)
                {
                    lBiblioCount++;
                    lItemCount += lCurItemCount;
                }
            }

            return 0;
        }

        // ������ͳ��
        int BuildArriveHtml(
            NamedListViewItems items,
            ref ExcelDocument doc,
            List<TimeSlice> time_ranges,
            out List<string> filenames,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            filenames = new List<string>();    // ÿҳһ���ļ�������������������ļ���

            stop.SetMessage("���ڽ��е�����ͳ�� ...");

            // ׼�����¼
            List<OneLine> infos = null;
            nRet = GetArriveTimes(items,
                out infos,
                out strError);
            if (nRet == -1)
                return -1;

            Hashtable macro_table = new Hashtable();

            // ��ô�ӡ����
            PrintOrderPrintOption option = new PrintOrderPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                "printorder_printoption");


            macro_table["%batchno%"] = this.BatchNo; // ���κ�
            macro_table["%seller%"] = items.Seller; // ������
            macro_table["%date%"] = DateTime.Now.ToLongDateString();

            macro_table["%recpathfilepath%"] = this.RecPathFilePath;
            string strRecPathFilename = Path.GetFileName(this.RecPathFilePath);
            macro_table["%recpathfilename%"] = strRecPathFilename;

            // ���κŻ��ļ��� ����겻�����жϺ�ѡ�ã�ֻ�úϲ�Ϊһ����
            macro_table["%batchno_or_recpathfilename%"] = String.IsNullOrEmpty(this.BatchNo) == true ? strRecPathFilename : this.BatchNo;



            // ��Ҫ�����ڲ�ͬ�������ļ���ǰ׺������
            string strFileNamePrefix = this.MainForm.DataDir + "\\~printorder_" + items.GetHashCode().ToString() + "_";

            string strFileName = "";

            Sheet sheet = null;
            if (doc != null)
                sheet = doc.NewSheet("������ͳ�Ʊ�");


            // �����Ϣҳ
            // TODO: Ҫ���ӡ�ͳ��ҳ��ģ�幦��
            {
                int nItemCount = items.Count;
                int nTotalSeries = GetMergedTotalSeries(items);
                int nTotalCopies = GetMergedTotalCopies(items);
                int nBiblioCount = GetMergedBiblioCount(items);

                macro_table["%itemcount%"] = nItemCount.ToString(); // ������
                macro_table["%totalcopies%"] = nTotalCopies.ToString(); // �ܲ���
                macro_table["%totalseries%"] = nTotalSeries.ToString(); // ������
                macro_table["%bibliocount%"] = nBiblioCount.ToString(); // ����

#if NO
                // �����յ�
                int nAcceptItemCount = items.Count;
                int nAcceptTotalSeries = GetMergedAcceptTotalSeries(items);
                int nAcceptTotalCopies = GetMergedAcceptTotalCopies(items);
                int nAcceptBiblioCount = GetMergedAcceptBiblioCount(items);

                macro_table["%accept_itemcount%"] = nAcceptItemCount.ToString(); // ������
                macro_table["%accept_totalcopies%"] = nAcceptTotalCopies.ToString(); // �ܲ���
                macro_table["%accept_totalseries%"] = nAcceptTotalSeries.ToString(); // ������
                macro_table["%accept_bibliocount%"] = nAcceptBiblioCount.ToString(); // ����

                // ������
                macro_table["%ratio_itemcount%"] = GetRatioString(nAcceptItemCount, nItemCount); // ������
                macro_table["%ratio_totalcopies%"] = GetRatioString(nAcceptTotalCopies, nTotalCopies); // �ܲ���
                macro_table["%ratio_totalseries%"] = GetRatioString(nAcceptTotalSeries, nTotalSeries); // ������
                macro_table["%ratio_bibliocount%"] = GetRatioString(nAcceptBiblioCount, nBiblioCount); // ����
#endif

                macro_table["%pageno%"] = "1";

                strFileName = strFileNamePrefix + "0" + ".html";

                filenames.Add(strFileName);

                BuildSlicePageTop(option,
                    macro_table,
                    strFileName);

                int nLineIndex = 2;
                if (doc != null)
                {
                    BuildSliceExcelPageTop(option,
        macro_table,
        ref doc,
        5);

                    {
                        List<string> captions = new List<string>();
                        captions.Add("ʱ��");
                        captions.Add("����");
                        captions.Add("�ֵ�����");
                        captions.Add("����");
                        captions.Add("�ᵽ����");
                        int i = 0;
                        foreach (string strCaption in captions)
                        {
                            doc.WriteExcelCell(
                    nLineIndex,
                    i++,
                    strCaption,
                    true);
                        }
                    }
                }

                StringBuilder table_content = new StringBuilder(4096);

                table_content.Append("<table class='slice'>");

                // ��������
                {
                    table_content.Append("<tr class='column'>");

                    table_content.Append("<td class='slice'>ʱ��</td>");
                    table_content.Append("<td class='bibliocount'>����</td>");
                    table_content.Append("<td class='biblioratio'>�ֵ�����</td>");
                    table_content.Append("<td class='itemcount'>����</td>");
                    table_content.Append("<td class='itemratio'>�ᵽ����</td>");
                    table_content.Append("</tr>");
                }

                // ��������Ҳ���ǵ����ʵķ�ĸ����
                {
                    table_content.Append("<tr class='order'>");

                    table_content.Append("<td class='slice'>������</td>");
                    table_content.Append("<td class='bibliocount'>" + nBiblioCount.ToString() + "</td>");
                    table_content.Append("<td class='biblioratio'>&nbsp;</td>");
                    table_content.Append("<td class='itemcount'>" + nTotalCopies.ToString() + "</td>");
                    table_content.Append("<td class='itemratio'>&nbsp;</td>");
                    table_content.Append("</tr>");
                }
                if (doc != null)
                {
                    nLineIndex++;
                    int i = 0;
                    doc.WriteExcelCell(
            nLineIndex,
            i++,
            "������",
            true);
                    doc.WriteExcelCell(
nLineIndex,
i++,
nBiblioCount.ToString(),
false);
                    doc.WriteExcelCell(
nLineIndex,
i++,
"",
true);
                    doc.WriteExcelCell(
nLineIndex,
i++,
nTotalCopies.ToString(),
false);
                }

                Debug.Assert(time_ranges.Count > 0, "");
                DateTime start = time_ranges[0].Start;
                foreach (TimeSlice slice in time_ranges)
                {
                    DateTime end = slice.Start + slice.Length;

                    long lBiblioCount = 0;
                    long lItemCount = 0;
                    // �����ض�ʱ�䷶Χ�ڵ�����ֲ���
                    nRet = GetValues(start,
                        end,
                        infos,
            out lBiblioCount,
            out lItemCount,
            out strError);
                    if (nRet == -1)
                        return -1;

                    string strTrClass = "";
                    if (string.IsNullOrEmpty(slice.Style) == false)
                        strTrClass = " class='" + slice.Style + "' ";

                    table_content.Append("<tr"+strTrClass+">");

                    table_content.Append("<td class='slice'>" + HttpUtility.HtmlEncode(slice.Caption) + "</td>");

                    string strRatioItem = GetRatioString(lItemCount, nTotalCopies); // ������
                    string strRatioBiblio = GetRatioString(lBiblioCount, nBiblioCount); // ����

                    table_content.Append("<td class='bibliocount'>" + lBiblioCount.ToString() + "</td>");
                    table_content.Append("<td class='biblioratio'>" + HttpUtility.HtmlEncode(strRatioBiblio) + "</td>");
                    table_content.Append("<td class='itemcount'>" + lItemCount.ToString() + "</td>");
                    table_content.Append("<td class='itemratio'>" + HttpUtility.HtmlEncode(strRatioItem) + "</td>");

                    table_content.Append("</tr>");

                    if (doc != null)
                    {
                        nLineIndex++;
                        int i = 0;
                        doc.WriteExcelCell(
                nLineIndex,
                i++,
                slice.Caption,
                true);
                        doc.WriteExcelCell(
    nLineIndex,
    i++,
    lBiblioCount.ToString(),
    false);
                        doc.WriteExcelCell(
    nLineIndex,
    i++,
    strRatioBiblio,
    true);
                        doc.WriteExcelCell(
    nLineIndex,
    i++,
    lItemCount.ToString(),
    false);
                        doc.WriteExcelCell(
    nLineIndex,
    i++,
    strRatioItem,
    true);

                    }
                }

                table_content.Append("</table>");

                StreamUtil.WriteText(strFileName,
                    table_content.ToString());

                BuildSlicePageBottom(option,
                    macro_table,
                    strFileName);
            }

            return 0;
        }

        // ��� Excel ҳ��ͷ����Ϣ
        int BuildSliceExcelPageTop(PrintOption option,
            Hashtable macro_table,
            ref ExcelDocument doc,
            int nTitleCols)
        {

            // ҳü
            string strPageHeaderText = "%date% %seller% ������ͳ�Ʊ� - ���κŻ��ļ���: %batchno_or_recpathfilename%";

            if (String.IsNullOrEmpty(strPageHeaderText) == false)
            {
                strPageHeaderText = StringUtil.MacroString(macro_table,
                    strPageHeaderText);
            }

            // ������
            string strTableTitleText = "%date% %seller% ������ͳ�Ʊ�";

            // ��һ�У�������
            if (String.IsNullOrEmpty(strTableTitleText) == false)
            {

                strTableTitleText = StringUtil.MacroString(macro_table,
                    strTableTitleText);

                doc.WriteExcelTitle(0,
                    nTitleCols,
                    strTableTitleText,
                    5);

            }




            return 0;
        }

        int BuildSlicePageTop(PrintOption option,
    Hashtable macro_table,
    string strFileName)
        {

            string strCssUrl = GetAutoCssUrl(option, "printslice.css");

            string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";

            StreamUtil.WriteText(strFileName,
                "<!DOCTYPE html PUBLIC \" -//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">"
                + "<html><head>" + strLink + "</head><body>");

            // ҳü
            string strPageHeaderText = "%date% %seller% ������ͳ�Ʊ� - ���κŻ��ļ���: %batchno_or_recpathfilename%";

            if (String.IsNullOrEmpty(strPageHeaderText) == false)
            {
                strPageHeaderText = StringUtil.MacroString(macro_table,
                    strPageHeaderText);

                StreamUtil.WriteText(strFileName,
                    "<div class='pageheader'>" + HttpUtility.HtmlEncode(strPageHeaderText) + "</div>");
            }

            // ������
            string strTableTitleText = "%date% %seller% ������ͳ�Ʊ�";

            if (String.IsNullOrEmpty(strTableTitleText) == false)
            {

                strTableTitleText = StringUtil.MacroString(macro_table,
                    strTableTitleText);

                StreamUtil.WriteText(strFileName,
                    "<div class='tabletitle'>" + HttpUtility.HtmlEncode(strTableTitleText) + "</div>");
            }

            return 0;
        }

        int BuildSlicePageBottom(PrintOption option,
Hashtable macro_table,
string strFileName)
        {
            // ҳ��
            string strPageFooterText = "";

            if (String.IsNullOrEmpty(strPageFooterText) == false)
            {
                strPageFooterText = StringUtil.MacroString(macro_table,
                    strPageFooterText);

                StreamUtil.WriteText(strFileName,
        "<div class='pagefooter'>" + HttpUtility.HtmlEncode(strPageFooterText) + "</div>");
            }

            StreamUtil.WriteText(strFileName, "</body></html>");
            return 0;
        }

#endregion

        // ��ӡ���� -- ��� Excel �ļ�
        private void toolStripMenuItem_outputExcel_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = PrintOrder("excel", out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ������ͳ�� -- ��� Excel �ļ�
        private void toolStripMenuItem_arriveRatio_outputExcel_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = PrintArriveRatio("excel", out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
    }

    internal class OutputProjectData
    {
        public string Seller = "";  // ������

        public string ProjectName = ""; // ���ο�����������Ҳ�������ʽ�������Ϊ"<default>"��ʽ����ʾΪ���õ������ʽ�����Ƕ��ο�������
        public string ProjectLocate = "";   // ��������Ŀ¼

        public OutputOrder OutputOrder = null; // ��������
        public Assembly Assembly = null;   // Assembly����

        // public int AssemblyVersion = 0; // Assembly����İ汾�š�����Ϊ�޷�������ǰ��Assembly�ļ������õĲ�����ʩ
    }

    // �ϲ������ݴ�ӡ �������ض�ȱʡֵ��PrintOption������
    internal class PrintOrderPrintOption : PrintOption
    {
        string PublicationType = "ͼ��"; // ͼ�� ����������

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

        public PrintOrderPrintOption(string strDataDir,
            string strPublicationType)
        {
            this.DataDir = strDataDir;
            this.PublicationType = strPublicationType;

            this.PageHeaderDefault = "%date% %seller% ���� - ���κŻ��ļ���: %batchno_or_recpathfilename% - (�� %pagecount% ҳ)";
            this.PageFooterDefault = "%pageno%/%pagecount%";

            this.TableTitleDefault = "%date% %seller% ����";

            this.LinesPerPageDefault = 20;

            // Columnsȱʡֵ
            Columns.Clear();

            // "no -- ���",
            Column column = new Column();
            column.Name = "no -- ���";
            column.Caption = "���";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "catalogNo -- ��Ŀ��"
            column = new Column();
            column.Name = "catalogNo -- ��Ŀ��";
            column.Caption = "��Ŀ��";
            column.MaxChars = -1;
            this.Columns.Add(column);


            // "summary -- ժҪ"
            column = new Column();
            column.Name = "summary -- ժҪ";
            column.Caption = "ժҪ";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "isbnIssn -- ISBN/ISSN"
            column = new Column();
            column.Name = "isbnIssn -- ISBN/ISSN";
            column.Caption = "ISBN/ISSN";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "price -- ����"
            column = new Column();
            column.Name = "price -- ����";
            column.Caption = "����";
            column.MaxChars = -1;
            this.Columns.Add(column);


            if (this.PublicationType == "����������")
            {
                // "range -- ʱ�䷶Χ"
                column = new Column();
                column.Name = "range -- ʱ�䷶Χ";
                column.Caption = "ʱ�䷶Χ";
                column.MaxChars = -1;
                this.Columns.Add(column);

                // "issueCount -- ��������"
                column = new Column();
                column.Name = "issueCount -- ��������";
                column.Caption = "��������";
                column.MaxChars = -1;
                this.Columns.Add(column);
            }

            /*
            // "copy -- ������"
            column = new Column();
            column.Name = "copy -- ������";
            column.Caption = "������";
            column.MaxChars = -1;
            this.Columns.Add(column);
             * */

            // "series -- ����"
            column = new Column();
            column.Name = "series -- ����";
            column.Caption = "����";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "totalPrice -- �ܼ۸�"
            column = new Column();
            column.Name = "totalPrice -- �ܼ۸�";
            column.Caption = "�ܼ۸�";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "acceptCopy -- �ѵ�����"
            column = new Column();
            column.Name = "acceptCopy -- �ѵ�����";
            column.Caption = "�ѵ�����";
            column.MaxChars = -1;
            this.Columns.Add(column);
        }
    }

    // ԭʼ���ݴ�ӡ �������ض�ȱʡֵ��PrintOption������
    internal class OrderOriginPrintOption : PrintOption
    {
        string PublicationType = "ͼ��"; // ͼ�� ����������

        public OrderOriginPrintOption(string strDataDir,
            string strPublicationType)
        {
            this.DataDir = strDataDir;
            this.PublicationType = strPublicationType;

            this.PageHeaderDefault = "%date% ԭʼ�������� - %recpathfilename% - (�� %pagecount% ҳ)";
            this.PageFooterDefault = "%pageno%/%pagecount%";

            this.TableTitleDefault = "%date% ԭʼ��������";

            this.LinesPerPageDefault = 20;

            // Columnsȱʡֵ
            Columns.Clear();

            // "no -- ���",
            Column column = new Column();
            column.Name = "no -- ���";
            column.Caption = "���";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "catalogNo -- ��Ŀ��"
            column = new Column();
            column.Name = "catalogNo -- ��Ŀ��";
            column.Caption = "��Ŀ��";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "summary -- ժҪ"
            column = new Column();
            column.Name = "summary -- ժҪ";
            column.Caption = "ժҪ";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "isbnIssn -- ISBN/ISSN"
            column = new Column();
            column.Name = "isbnIssn -- ISBN/ISSN";
            column.Caption = "ISBN/ISSN";
            column.MaxChars = -1;
            this.Columns.Add(column);


            // "price -- ����"
            column = new Column();
            column.Name = "price -- ����";
            column.Caption = "����";
            column.MaxChars = -1;
            this.Columns.Add(column);

            if (this.PublicationType == "����������")
            {
                // "range -- ʱ�䷶Χ"
                column = new Column();
                column.Name = "range -- ʱ�䷶Χ";
                column.Caption = "ʱ�䷶Χ";
                column.MaxChars = -1;
                this.Columns.Add(column);

                // "issueCount -- ��������"
                column = new Column();
                column.Name = "issueCount -- ��������";
                column.Caption = "��������";
                column.MaxChars = -1;
                this.Columns.Add(column);
            }

            // "copy -- ������"
            column = new Column();
            column.Name = "copy -- ������";
            column.Caption = "������";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "totalPrice -- �ܼ۸�"
            column = new Column();
            column.Name = "totalPrice -- �ܼ۸�";
            column.Caption = "�ܼ۸�";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "class -- ���"
            column = new Column();
            column.Name = "class -- ���";
            column.Caption = "���";
            column.MaxChars = -1;
            this.Columns.Add(column);
        }
    }

    // ԭʼ����listviewitem��Tag��Я�������ݽṹ
    internal class OriginItemData
    {
        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed = false;
        public byte[] Timestamp = null;
        public string Xml = ""; // ������¼��XML��¼��
        public string RefID = "";   // �����¼ʱ���õ�refid
    }

    internal class StatisLine
    {
        public string Class = "";   // �����
        public long BiblioCount = 0;    // ����
        public long SeriesCount = 0;      // ����
        public long ItemCount = 0;      // ����
        public bool AllowSum = true;    // �Ƿ�������
        public string Price = "";       // �۸��ַ���

        public long AcceptBiblioCount = 0;    // �ѵ�����
        public long AcceptSeriesCount = 0;      // �ѵ�����
        public long AcceptItemCount = 0;      // �ѵ�����
        public string AcceptPrice = "";       // �ѵ��۸��ַ���

        public List<StatisLine> InnerLines = null;  // Ƕ�׵��ӱ�
    }

    // ����������С����ǰ
    internal class CellStatisLineComparer : IComparer<StatisLine>
    {
        int IComparer<StatisLine>.Compare(StatisLine x, StatisLine y)
        {
            string s1 = x.Class;
            string s2 = y.Class;

            return String.Compare(s1, s2);
        }
    }
}