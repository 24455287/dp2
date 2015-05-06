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
using DigitalPlatform.CommonControl;

using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.dp2.Statis;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;

namespace dp2Circulation
{
    // ��ӡ(�ɹ�)���յ�
    /// <summary>
    /// ��ӡ���յ���
    /// </summary>
    public partial class PrintAcceptForm : BatchPrintFormBase
    {
        // װ������ʱ�ķ�ʽ
        string SourceStyle = "";    // "batchno" "barcodefile" "recpathfile"

        /// <summary>
        /// ����ù��ļ�¼·���ļ�ȫ·��
        /// </summary>
        public string RecPathFilePath = "";

        // refid -- ������¼path ���ձ�
        Hashtable refid_table = new Hashtable();
        // ������¼path -- ������¼XML���ձ�
        Hashtable orderxml_table = new Hashtable();

        string BatchNo = "";    // ����ڼ����������������κ�

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;
#endif
        /// <summary>
        /// ����ͼ�� ImageIndex : ����
        /// </summary>
        public const int TYPE_ERROR = 0;
        /// <summary>
        /// ����ͼ�� ImageIndex : ��ͨ���鱾��������
        /// </summary>
        public const int TYPE_NORMAL = 1;   // �鱾��������
        /// <summary>
        /// ����ͼ�� ImageIndex : �޸Ĺ����鱾������������ʾԭʼ��¼���ڴ����з������޸ģ�����δ����
        /// </summary>
        public const int TYPE_CHANGED = 2;  // �鱾������������ʾԭʼ��¼���ڴ����з������޸ģ�����δ����


        // ����������к�����
        SortColumns SortColumns_origin = new SortColumns();
        SortColumns SortColumns_merged = new SortColumns();

        #region ԭʼ����listview�к�
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
        /// ԭʼ�����к�: ����ʱ��
        /// </summary>
        public static int ORIGIN_COLUMN_PUBLISHTIME = 4;          // ����ʱ��
        /// <summary>
        /// ԭʼ�����к�: ����
        /// </summary>
        public static int ORIGIN_COLUMN_VOLUME = 5;          // ����
        /// <summary>
        /// ԭʼ�����к�: �ݲصص�
        /// </summary>
        public static int ORIGIN_COLUMN_LOCATION = 6;       // �ݲصص�
        /// <summary>
        /// ԭʼ�����к�: ����
        /// </summary>
        public static int ORIGIN_COLUMN_SELLER = 7;        // ����
        /// <summary>
        /// ԭʼ�����к�: ������Դ
        /// </summary>
        public static int ORIGIN_COLUMN_SOURCE = 8;        // ������Դ
        /// <summary>
        /// ԭʼ�����к�: ����
        /// </summary>
        public static int ORIGIN_COLUMN_ITEMPRICE = 9;             // ����
        /// <summary>
        /// ԭʼ�����к�: ��ע
        /// </summary>
        public static int ORIGIN_COLUMN_COMMENT = 10;          // ��ע
        /// <summary>
        /// ԭʼ�����к�: (����)���κ�
        /// </summary>
        public static int ORIGIN_COLUMN_BATCHNO = 11;          // (����)���κ�
        /// <summary>
        /// ԭʼ�����к�: �ο�ID
        /// </summary>
        public static int ORIGIN_COLUMN_REFID = 12;          // �ο�ID
        /// <summary>
        /// ԭʼ�����к�: �ּ�¼·��
        /// </summary>
        public static int ORIGIN_COLUMN_BIBLIORECPATH = 13;    // �ּ�¼·��

        /// <summary>
        /// ԭʼ�����к�: ��Ŀ��
        /// </summary>
        public static int ORIGIN_COLUMN_CATALOGNO = 14;    // ��Ŀ��
        /// <summary>
        /// ԭʼ�����к�: ������
        /// </summary>
        public static int ORIGIN_COLUMN_ORDERID = 15;    // ������
        /// <summary>
        /// ԭʼ�����к�: �������
        /// </summary>
        public static int ORIGIN_COLUMN_ORDERCLASS = 16;    // �������
        /// <summary>
        /// ԭʼ�����к�: ����ʱ��
        /// </summary>
        public static int ORIGIN_COLUMN_ORDERTIME = 17;    // ����ʱ��
        /// <summary>
        /// ԭʼ�����к�: (������¼�е�)������
        /// </summary>
        public static int ORIGIN_COLUMN_ORDERPRICE = 18;    // (������¼�е�)������
        /// <summary>
        /// ԭʼ�����к�: (������¼�е�)�����
        /// </summary>
        public static int ORIGIN_COLUMN_ACCEPTPRICE = 19;    // (������¼�е�)�����
        /// <summary>
        /// ԭʼ�����к�: �������Ķ�����¼��������ַ
        /// </summary>
        public static int ORIGIN_COLUMN_SELLERADDRESS = 20;    // �������Ķ�����¼��������ַ
        /// <summary>
        /// ԭʼ�����к�: �������Ķ�����¼·��
        /// </summary>
        public static int ORIGIN_COLUMN_ORDERRECPATH = 21;    // �������Ķ�����¼·��
        /// <summary>
        /// ԭʼ�����к�: ��������
        /// </summary>
        public static int ORIGIN_COLUMN_ACCEPTSUBCOPY = 22;    // �������� 1:2 ��һ���ڵĵڶ���
        /// <summary>
        /// ԭʼ�����к�: ����ʱ��ÿ�ײ���
        /// </summary>
        public static int ORIGIN_COLUMN_ORDERSUBCOPY = 23;    // ����ʱ��ÿ�ײ���

        #endregion

        #region �ϲ�������listview�к�

        /// <summary>
        /// �ϲ��������к�: ����
        /// </summary>
        public static int MERGED_COLUMN_SELLER = 0;             // ����
        /// <summary>
        /// �ϲ��������к�: ��Ŀ��
        /// </summary>
        public static int MERGED_COLUMN_CATALOGNO = 1;          // ��Ŀ��
        /// <summary>
        /// �ϲ��������к�: ժҪ
        /// </summary>
        public static int MERGED_COLUMN_SUMMARY = 2;    // ժҪ
        /// <summary>
        /// �ϲ��������к�: ������Ϣ
        /// </summary>
        public static int MERGED_COLUMN_ERRORINFO = 2;  // ������Ϣ
        /// <summary>
        /// �ϲ��������к�: ISBN/ISSN
        /// </summary>
        public static int MERGED_COLUMN_ISBNISSN = 3;           // ISBN/ISSN
        /// <summary>
        /// �ϲ��������к�: ����ʱ��
        /// </summary>
        public static int MERGED_COLUMN_PUBLISHTIME = 4;          // ����ʱ��
        /// <summary>
        /// �ϲ��������к�: ����
        /// </summary>
        public static int MERGED_COLUMN_VOLUME = 5;          // ����

        /// <summary>
        /// �ϲ��������к�: �ϲ�ע��
        /// </summary>
        public static int MERGED_COLUMN_MERGECOMMENT = 6;      // �ϲ�ע��

        /// <summary>
        /// �ϲ��������к�: ʱ�䷶Χ
        /// </summary>
        public static int MERGED_COLUMN_RANGE = 7;      // ʱ�䷶Χ
        /// <summary>
        /// �ϲ��������к�: ��������
        /// </summary>
        public static int MERGED_COLUMN_ISSUECOUNT = 8;      // ��������

        /// <summary>
        /// �ϲ��������к�: ������
        /// </summary>
        public static int MERGED_COLUMN_COPY = 9;              // ������
        /// <summary>
        /// �ϲ��������к�: ÿ�ײ���
        /// </summary>
        public static int MERGED_COLUMN_SUBCOPY = 10;              // ÿ�ײ���
        /// <summary>
        /// �ϲ��������к�: ��������
        /// </summary>
        public static int MERGED_COLUMN_ORDERPRICE = 11;             // ��������
        /// <summary>
        /// �ϲ��������к�: (����)����
        /// </summary>
        public static int MERGED_COLUMN_PRICE = 12;             // (����)����
        /// <summary>
        /// �ϲ��������к�: �ܼ۸�
        /// </summary>
        public static int MERGED_COLUMN_TOTALPRICE = 13;        // �ܼ۸�
        /// <summary>
        /// �ϲ��������к�: ʵ��(��)����
        /// </summary>
        public static int MERGED_COLUMN_ITEMPRICE = 14;             // ʵ��(��)����
        /// <summary>
        /// �ϲ��������к�: ����ʱ��
        /// </summary>
        public static int MERGED_COLUMN_ORDERTIME = 15;        // ����ʱ��
        /// <summary>
        /// �ϲ��������к�: ������
        /// </summary>
        public static int MERGED_COLUMN_ORDERID = 16;          // ������
        /// <summary>
        /// �ϲ��������к�: �ݲط���
        /// </summary>
        public static int MERGED_COLUMN_DISTRIBUTE = 17;       // �ݲط���
        /// <summary>
        /// �ϲ��������к�: ���
        /// </summary>
        public static int MERGED_COLUMN_CLASS = 18;             // ���
        /// <summary>
        /// �ϲ��������к�: ��ע
        /// </summary>
        public static int MERGED_COLUMN_COMMENT = 19;          // ��ע
        /// <summary>
        /// �ϲ��������к�: ������ַ
        /// </summary>
        public static int MERGED_COLUMN_SELLERADDRESS = 20;          // ������ַ
        /// <summary>
        /// �ϲ��������к�: �ּ�¼·��
        /// </summary>
        public static int MERGED_COLUMN_BIBLIORECPATH = 21;    // �ּ�¼·��

        #endregion

        const int WM_LOADSIZE = API.WM_USER + 201;

        /// <summary>
        /// ���캯��
        /// </summary>
        public PrintAcceptForm()
        {
            InitializeComponent();
        }

        private void PrintAcceptForm_Load(object sender, EventArgs e)
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
                "printaccept_form",
                "publication_type",
                "ͼ��");

            comboBox_load_type_SelectedIndexChanged(null, null);

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);
        }

        private void PrintAcceptForm_FormClosing(object sender, FormClosingEventArgs e)
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
                    "PrintAcceptForm",
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

        private void PrintAcceptForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            this.MainForm.AppInfo.SetString(
                "printaccept_form",
                "publication_type",
                this.comboBox_load_type.Text);

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
                "printaccept_form",
                "list_origin_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_origin,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
    "printaccept_form",
    "list_merged_width",
    "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_merged,
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

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_origin);
            this.MainForm.AppInfo.SetString(
                "printaccept_form",
                "list_origin_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.listView_merged);
            this.MainForm.AppInfo.SetString(
                "printaccept_form",
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

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            // load page
            this.button_load_loadFromBatchNo.Enabled = bEnable;
            this.button_load_loadFromRecPathFile.Enabled = bEnable;
            this.button_load_loadFromOrderBatchNo.Enabled = bEnable;
            this.comboBox_load_type.Enabled = bEnable;

            // next button
            if (bEnable == true)
                SetNextButtonEnable();
            else
                this.button_next.Enabled = false;

            // print page
            this.button_print_Option.Enabled = bEnable;
            this.button_print_printAcceptList.Enabled = bEnable;
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

                bool bOK = ReportSaveChangeState(out strError);

                if (bOK == false)
                {
                    this.button_next.Enabled = false;
                    this.button_saveChange_saveChange.Enabled = true;
                }
                else
                {
                    this.button_next.Enabled = true;
                    this.button_saveChange_saveChange.Enabled = false;
                }

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

        // �㱨������������
        // return:
        //      true    �����Ѿ����
        //      false   ������δ���
        bool ReportSaveChangeState(out string strError)
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

            if (nWhiteCount == this.listView_origin.Items.Count)
            {
                strError = "û�з������޸�";
                return true;
            }

            strError = "ԭʼ�����б����з������޸ĺ���δ�����������д������\r\n\r\n�б�����:\r\n�������޸ĵ�����(����ɫ����) " + nYellowCount.ToString() + " ��\r\n��������(��ɫ����) " + nRedCount.ToString() + "��\r\n\r\n(ֻ��ȫ�����Ϊ��ͨ״̬(��ɫ����)���ű�����������Ѿ����)";
            return false;
        }

        // ��(�����յ�)���¼·���ļ�װ��
        // parameters:
        //      bAutoSetSeriesType  �Ƿ�����ļ���һ���е�·���е����ݿ������Զ����ó��������� Combobox_type
        // return:
        //      -1  ����
        //      0   ����
        //      1   װ�سɹ�
        /// <summary>
        /// ��(�����յ�)���¼·���ļ�װ��
        /// </summary>
        /// <param name="bAutoSetSeriesType">�Ƿ�����ļ���һ���е�·���е����ݿ������Զ����ó��������� Combobox_type</param>
        /// <param name="strFilename">�ļ�ȫ·��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:  ����</para>
        /// <para>0:   ����</para>
        /// <para>1:   װ�سɹ�</para>
        /// </returns>
        public int LoadFromItemRecPathFile(
            bool bAutoSetSeriesType,
            string strFilename,
            out string strError)
        {
            strError = "";

            this.SourceStyle = "recpathfile";

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
                                "PrintAcceptForm",
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
                                return -1;
                            }
                        }


                        string strLine = "";
                        strLine = sr.ReadLine();

                        if (strLine == null)
                            break;

                        strLine = strLine.Trim();
                        if (string.IsNullOrEmpty(strLine) == true)
                            continue;

                        if (strLine[0] == '#')
                            continue;   // ע����

                        string strItemDbName = Global.GetDbName(strLine);
                        // ������ݿ�����(ͼ��/�ڿ�)
                        // �۲�ʵ����ǲ����ڿ�������
                        // return:
                        //      -1  ����ʵ���
                        //      0   ͼ������
                        //      1   �ڿ�����
                        nRet = this.MainForm.IsSeriesTypeFromItemDbName(strItemDbName);
                        if (nRet == -1)
                        {
                            strError = "��¼·�� '" + strLine + "' �е����ݿ��� '" + strItemDbName + "' ����ʵ�����";
                            return -1;
                        }

                        // �Զ����� ͼ��/�ڿ� ����
                        if (bAutoSetSeriesType == true && nLineCount == 0)
                        {
                            if (nRet == 0)
                                this.comboBox_load_type.Text = "ͼ��";
                            else
                                this.comboBox_load_type.Text = "����������";
                        }

                        if (this.comboBox_load_type.Text == "ͼ��")
                        {
                            if (nRet != 0)
                            {
                                strError = "��¼·�� '" + strLine + "' �е����ݿ��� '" + strItemDbName + "' ����һ��ͼ�����͵�ʵ�����";
                                return -1;
                            }
                        }
                        else
                        {
                            if (nRet != 1)
                            {
                                strError = "��¼·�� '" + strLine + "' �е����ݿ��� '" + strItemDbName + "' ����һ���������������͵�ʵ�����";
                                return -1;
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

                    for (int i = 0; ;)
                    {
                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�2";
                                return -1;
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

                        stop.SetMessage("����װ��·�� " + strLine + " ��Ӧ�ļ�¼...");


                        // ���ݼ�¼·����װ�붩����¼
                        // return: 
                        //      -2  ·���Ѿ���list�д�����
                        //      -1  ����
                        //      1   �ɹ�
                        nRet = LoadOneItem(
                            this.comboBox_load_type.Text,
                            strLine,
                            this.listView_origin,
                            out strError);
                        if (nRet == -2)
                            nDupCount++;

                        i++;
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
                return -1;
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
                return -1;


            // �㱨����װ�������
            // return:
            //      0   ��δװ���κ�����    
            //      1   װ���Ѿ����
            //      2   ��Ȼװ�������ݣ����������д�������
            int nState = ReportLoadState(out strError);
            if (nState != 1)
                return -1;

            return 1;
        }

        // TODO: ��Ҫ����¼·���Ƿ�����ʵ��⣿
        private void button_load_loadFromRecPathFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            this.BatchNo = "";  // ��ʾ���Ǹ������κŻ�õ�����

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵ļ�¼·���ļ���";
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            // ��(�����յ�)���¼·���ļ�װ��
            // return:
            //      -1  ����
            //      0   ����
            //      1   װ�سɹ�
            int nRet = LoadFromItemRecPathFile(
                true,
                dlg.FileName,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            this.Text = "��ӡ���յ�";
            MessageBox.Show(this, strError);
        }

        // ���ݼ�¼·����װ����¼
        // return: 
        //      -2  ·���Ѿ���list�д�����
        //      -1  ����
        //      1   �ɹ�
        int LoadOneItem(
            string strPubType,
            string strRecPath,
            ListView list,
            out string strError)
        {
            strError = "";

            string strItemXml = "";
            string strBiblioText = "";

            string strOutputOrderRecPath = "";
            string strOutputBiblioRecPath = "";

            byte[] item_timestamp = null;

            long lRet = Channel.GetItemInfo(
                stop,
                
                (strRecPath[0] != '@' ? "@path:" + strRecPath : strRecPath),
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

                OriginAcceptItemData data = new OriginAcceptItemData();
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

            if (strRecPath[0] == '@')
                strRecPath = strOutputOrderRecPath;

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
                    if (ListViewUtil.GetItemText(curitem, ORIGIN_COLUMN_BIBLIORECPATH) == strOutputBiblioRecPath)
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

            // ����һ����xml��¼��ȡ���й���Ϣ����listview��

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
                ListViewItem item = AddToListView(list,
                    dom,
                    strOutputOrderRecPath,
                    strBiblioSummary,
                    strISBnISSN,
                    strOutputBiblioRecPath);

                // ����timestamp/xml
                OriginAcceptItemData data = (OriginAcceptItemData)item.Tag;
                Debug.Assert(data != null, "");
                data.Timestamp = item_timestamp;
                data.Xml = strItemXml;

                /*
                // ͼ��
                SetItemColor(item, TYPE_NORMAL);
                 * */

                // ���¼�������������Ұ
                list.EnsureVisible(list.Items.Count - 1);

                // �����Ҫ�Ӷ������õ���Ŀ��Ϣ
                FillOrderColumns(item, strPubType);
            }

            return 1;
        ERROR1:
            return -1;
        }

        // �����Ҫ�Ӷ������õ���Ŀ��Ϣ
        void FillOrderColumns(ListViewItem item,
            string strPubType)
        {
            string strRefID = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_REFID);
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

                string strPublishTime = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_PUBLISHTIME);

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
            string strSellerAddress = DomUtil.GetElementInnerXml(dom.DocumentElement,
                "sellerAddress");

            string strOrderPrice = "";  // ������¼�еĶ�����
            string strAcceptPrice = "";    // ������¼�еĵ����

            // ���total price�Ƿ���ȷ
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

            // 2010/4/27
            // ����ڿ�ȱ�������
            if (bSeries == true && string.IsNullOrEmpty(strAcceptPrice) == true)
            {
                // ��ȡ��۸�
                strAcceptPrice = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ITEMPRICE);
                // Ȼ��ȡ������
                if (String.IsNullOrEmpty(strAcceptPrice) == true)
                    strAcceptPrice = strOrderPrice;
            }

            try
            {
                strOrderTime = DateTimeUtil.LocalTime(strOrderTime);
            }
            catch (Exception ex)
            {
                strOrderTime = "ʱ���ַ��� '" + strOrderTime + "' ��ʽ����: " + ex.Message;
            }

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_CATALOGNO, strCatalogNo);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ORDERID, strOrderID);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ORDERCLASS, strClass);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ORDERTIME, strOrderTime);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ORDERPRICE, strOrderPrice);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ACCEPTPRICE, strAcceptPrice);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_SELLERADDRESS, strSellerAddress);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ORDERRECPATH, strOrderOrIssueRecPath);

            string strDistribute = DomUtil.GetElementText(dom.DocumentElement,
                "distribute");
            if (String.IsNullOrEmpty(strDistribute) == false)
            {
                // ����refid -- ��¼·�����չ�ϵ
                List<string> refids = GetLocationRefIDs(strDistribute);

                for (int i = 0; i < refids.Count; i++)
                {
                    string strCurrentRefID = refids[i];
                    this.refid_table[strCurrentRefID] = strOrderOrIssueRecPath;
                }
            }

            string strCopyDetail = "";
            // return:
            //      -1  ����
            //      0   һ��ᣬ�������ڵĲᡣstrResult��Ϊ��
            //      1   ���ڲᡣstrResult�з�����ֵ
            nRet = GetCopyDetail(strDistribute,
                strRefID,
                out strCopyDetail,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            {
                string strCopy = DomUtil.GetElementText(dom.DocumentElement,
    "copy");
                string strOldCopy = "";
                string strNewCopy = "";

                // ���� "old[new]" �ڵ�����ֵ
                OrderDesignControl.ParseOldNewValue(strCopy,
                    out strOldCopy,
                    out strNewCopy);

                // ����ʱÿ�װ������� 2012/9/4
                string strOrderRightCopy = OrderDesignControl.GetRightFromCopyString(strOldCopy);
                if (string.IsNullOrEmpty(strOrderRightCopy) == true)
                    strOrderRightCopy = "1";

                ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ORDERSUBCOPY, strOrderRightCopy);

                if (String.IsNullOrEmpty(strCopyDetail) == false)
                {
                    // һ�װ����Ĳ���
                    string strRightCopy = OrderDesignControl.GetRightFromCopyString(strNewCopy);
                    if (String.IsNullOrEmpty(strRightCopy) == false)
                        strCopyDetail += "/" + strRightCopy;
                }
            }

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ACCEPTSUBCOPY, strCopyDetail);
            return;
        ERROR1:
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ERRORINFO, strError);
            SetItemColor(item, TYPE_ERROR);
        }

        // ������������ĸ�������
        // 0:1/7
        static int ParseSubCopy(string strText,
            out string strNo,
            out string strIndex,
            out string strCopy,
            out string strError)
        {
            strNo = "";
            strIndex = "";
            strCopy = "";
            strError = "";

            int nRet = strText.IndexOf(":");
            if (nRet == -1)
            {
                strError = "û��ð��";
                return -1;
            }

            strNo = strText.Substring(0, nRet).Trim();
            strText = strText.Substring(nRet + 1).Trim();

            nRet = strText.IndexOf("/");
            if (nRet == -1)
            {
                strError = "û��/��";
                return -1;
            }
            strIndex = strText.Substring(0, nRet).Trim();
            strCopy = strText.Substring(nRet + 1).Trim();
            return 0;
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

                    if (string.IsNullOrEmpty(location.RefID) == true)
                        continue;

                    // 2012/9/4
                    string[] parts = location.RefID.Split(new char[] { '|' });
                    foreach (string text in parts)
                    {
                        string strCurrentRefID = text.Trim();
                        if (string.IsNullOrEmpty(strCurrentRefID) == true)
                            continue;

                        if (strCurrentRefID == strRefID)
                        {
                            strOrderXml = node.ParentNode.OuterXml;
                            return 1;
                        }
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

            Record[] searchresults = null;

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
                strError = "�ο�ID '" + strRefID + "' ���ж���("+lRet.ToString()+")������¼";
                return -1;
            }

            long lHitCount = lRet;

            Record[] searchresults = null;

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

        // ���ɹ��ݲ��ַ����е�refid��������
        /// <summary>
        /// ���ɹ��ݲ��ַ����еĲο� ID ��������
        /// </summary>
        /// <param name="strText">�ݲ��ַ���</param>
        /// <returns>��ʾ�ο� ID ���ַ�������</returns>
        public static List<string> GetLocationRefIDs(string strText)
        {
            List<string> results = new List<string>();

            if (String.IsNullOrEmpty(strText) == true)
                return results;

            int nStart = 0;
            int nEnd = 0;
            int nPos = 0;
            for (; ; )
            {
                nStart = strText.IndexOf("{", nPos);
                if (nStart == -1)
                    break;
                nPos = nStart + 1;
                nEnd = strText.IndexOf("}", nPos);
                if (nEnd == -1)
                    break;
                nPos = nEnd + 1;
                if (nEnd <= nStart + 1)
                    continue;
                string strPart = strText.Substring(nStart + 1, nEnd - nStart - 1).Trim();

                if (String.IsNullOrEmpty(strPart) == true)
                    continue;

                string[] ids = strPart.Split(new char[] { ',', '|' });
                for (int j = 0; j < ids.Length; j++)
                {
                    string strID = ids[j].Trim();
                    if (String.IsNullOrEmpty(strID) == true)
                        continue;

                    results.Add(strID);
                }
            }

            return results;
        }

        // ���һ��������������ַ���
        // parameters:
        //      strDistribute     �ݲط����ַ���
        //      strRefID    Ҫ��ע�Ĳ��refid
        //      strResult   ���������ַ�������ʽΪ��1:2�� ��ʾ��һ���ڵĵڶ��� ��Ŵ�1��ʼ����
        // return:
        //      -1  ����
        //      0   һ��ᣬ�������ڵĲᡣstrResult��Ϊ��
        //      1   ���ڲᡣstrResult�з�����ֵ
        /*public*/ static int GetCopyDetail(string strDistribute,
            string strRefID,
            out string strResult,
            out string strError)
        {
            strResult = "";
            strError = "";

            if (String.IsNullOrEmpty(strRefID) == true)
            {
                strError = "strRefID������ӦΪ��";
                return -1;
            }

            if (String.IsNullOrEmpty(strDistribute) == true)
            {
                strError = "strText������ӦΪ��";
                return -1;
            }

            List<string> results = new List<string>();

            int nStart = 0;
            int nEnd = 0;
            int nPos = 0;
            for (; ; )
            {
                nStart = strDistribute.IndexOf("{", nPos);
                if (nStart == -1)
                    break;
                nPos = nStart + 1;
                nEnd = strDistribute.IndexOf("}", nPos);
                if (nEnd == -1)
                    break;
                nPos = nEnd + 1;
                if (nEnd <= nStart + 1)
                    continue;
                string strPart = strDistribute.Substring(nStart + 1, nEnd - nStart - 1).Trim();

                if (String.IsNullOrEmpty(strPart) == true)
                    continue;

                string[] ids = strPart.Split(new char[] { ','});
                for (int j = 0; j < ids.Length; j++)
                {
                    string strID = ids[j].Trim();
                    if (String.IsNullOrEmpty(strID) == true)
                        continue;

                    results.Add(strID);
                }
            }

            // Ѱ��λ��
            int iTao = -1;
            for (int i = 0; i < results.Count; i++)
            {
                string strSegment = results[i];

                bool bTao = false;
                int nRet = strSegment.IndexOf("|");
                if (nRet == -1)
                    bTao = false;
                else
                {
                    bTao = true;
                    iTao ++;
                }

                if (bTao == false)
                {
                    if (strRefID == strSegment)
                        return 0;
                    continue;
                }

                string[] ids = strSegment.Split(new char[] { '|' });
                for (int j = 0; j < ids.Length; j++)
                {
                    string strID = ids[j].Trim();
                    if (String.IsNullOrEmpty(strID) == true)
                        continue;

                    if (strID == strRefID)
                    {
                        strResult = (iTao + 1).ToString() + ":" + (j + 1).ToString();
                        return 1;
                    }
                }
            }

            strError = "refid '"+strRefID+"' ���ַ��� '"+strDistribute+"' ��û���ҵ�";
            return -1;    // not found
        }

        static System.Drawing.Color GetItemForeColor(int nType)
        {
            if (nType == TYPE_ERROR)
            {
                return System.Drawing.Color.White;
            }
            else if (nType == TYPE_CHANGED)
            {
                return SystemColors.WindowText;
            }
            else if (nType == TYPE_NORMAL)
            {
                return SystemColors.WindowText;
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
                return SystemColors.Window;
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
                if (item.Tag is OriginAcceptItemData
                    && nType != TYPE_ERROR)
                {
                    OriginAcceptItemData data = (OriginAcceptItemData)item.Tag;

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

        /*public*/ static ListViewItem AddToListView(ListView list,
    XmlDocument dom,
    string strRecPath,
    string strBiblioSummary,
    string strISBnISSN,
    string strBiblioRecPath)
        {
            ListViewItem item = new ListViewItem(strRecPath, TYPE_NORMAL);

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

        // ���ݶ�����¼DOM����ListViewItem����һ�����������
        // parameters:
        //      bSetBarcodeColumn   �Ƿ�Ҫ���õ�һ�м�¼·��������
        /*public*/ static void SetListViewItemText(XmlDocument dom,
            bool bSetRecPathColumn,
            string strRecPath,
            string strBiblioSummary,
            string strISBnISSN,
            string strBiblioRecPath,
            ListViewItem item)
        {
            OriginAcceptItemData data = null;
            data = (OriginAcceptItemData)item.Tag;
            if (data == null)
            {
                data = new OriginAcceptItemData();
                item.Tag = data;
            }
            else
            {
                data.Changed = false;   // 2008/9/5 new add
            }

            string strState = DomUtil.GetElementText(dom.DocumentElement,
                "state");
            string strPublishTime = DomUtil.GetElementText(dom.DocumentElement,
                "publishTime");
            string strVolume = DomUtil.GetElementText(dom.DocumentElement,
                "volume");

            string strLocation = DomUtil.GetElementText(dom.DocumentElement,
                "location");
            string strSeller = DomUtil.GetElementText(dom.DocumentElement,
                "seller");
            string strSource = DomUtil.GetElementText(dom.DocumentElement,
                "source");
            string strPrice = DomUtil.GetElementText(dom.DocumentElement,
                "price");

            List<int> textchanged_columns = new List<int>();

            /*
            // �����޸� ״̬
            {
                if (strState == "������")
                {
                    strBiblioSummary = "����¼״̬Ϊ '������'�������ٲ��붩����ӡ";
                    SetItemColor(item,
                            TYPE_ERROR);
                }
                else
                {
                    string strNewState = "�Ѷ���";

                    if (strState != strNewState)
                    {
                        strState = strNewState;
                        data.Changed = true;
                        SetItemColor(item,
                            TYPE_CHANGED); // ��ʾ״̬���ı���
                        textchanged_columns.Add(ORIGIN_COLUMN_STATE);
                    }
                }
            }*/

            string strComment = DomUtil.GetElementText(dom.DocumentElement,
                "comment");
            string strBatchNo = DomUtil.GetElementText(dom.DocumentElement,
                "batchNo");
            string strRefID = DomUtil.GetElementText(dom.DocumentElement,
                "refID");

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_SUMMARY, strBiblioSummary);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ISBNISSN, strISBnISSN);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_STATE, strState);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_PUBLISHTIME, strPublishTime);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_VOLUME, strVolume);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_LOCATION, strLocation);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_SELLER, strSeller);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_SOURCE, strSource);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_ITEMPRICE, strPrice);

            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_COMMENT, strComment);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_BATCHNO, strBatchNo);
            ListViewUtil.ChangeItemText(item, ORIGIN_COLUMN_REFID, strRefID);

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
            ColumnHeader columnHeader_publishTime = new ColumnHeader();
            ColumnHeader columnHeader_volume = new ColumnHeader();
            ColumnHeader columnHeader_seller = new ColumnHeader();
            ColumnHeader columnHeader_source = new ColumnHeader();
            ColumnHeader columnHeader_price = new ColumnHeader();

            ColumnHeader columnHeader_location = new ColumnHeader();

            ColumnHeader columnHeader_comment = new ColumnHeader();
            ColumnHeader columnHeader_batchNo = new ColumnHeader();
            ColumnHeader columnHeader_refID = new ColumnHeader();
            ColumnHeader columnHeader_biblioRecpath = new ColumnHeader();

            ColumnHeader columnHeader_catalogNo = new ColumnHeader();
            ColumnHeader columnHeader_orderID = new ColumnHeader();
            ColumnHeader columnHeader_orderClass = new ColumnHeader();
            ColumnHeader columnHeader_orderTime = new ColumnHeader();
            ColumnHeader columnHeader_orderPrice = new ColumnHeader();
            ColumnHeader columnHeader_acceptPrice = new ColumnHeader();
            ColumnHeader columnHeader_sellerAddress = new ColumnHeader();
            ColumnHeader columnHeader_orderRecpath = new ColumnHeader();
            ColumnHeader columnHeader_acceptSubCopy = new ColumnHeader();
            ColumnHeader columnHeader_orderSubCopy = new ColumnHeader();


            list.Columns.Clear();

            list.Columns.AddRange(new ColumnHeader[] {
            columnHeader_recpath,
            columnHeader_errorInfo,
            columnHeader_isbnIssn,
            columnHeader_state,
            columnHeader_publishTime,
            columnHeader_volume,
            columnHeader_location,
            columnHeader_seller,
            columnHeader_source,
            columnHeader_price,
            columnHeader_comment,
            columnHeader_batchNo,
            columnHeader_refID,
            columnHeader_biblioRecpath,
            columnHeader_catalogNo,
            columnHeader_orderID,
            columnHeader_orderClass,
            columnHeader_orderTime,
            columnHeader_orderPrice,
            columnHeader_acceptPrice,
            columnHeader_sellerAddress,
            columnHeader_orderRecpath,
            columnHeader_acceptSubCopy,
            columnHeader_orderSubCopy});


            // 
            // columnHeader_recpath
            // 
            columnHeader_recpath.Text = "���¼·��";
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
            // columnHeader_publishTime
            // 
            columnHeader_publishTime.Text = "����ʱ��";
            columnHeader_publishTime.Width = 100;
            // 
            // columnHeader_volume
            // 
            columnHeader_volume.Text = "����";
            columnHeader_volume.Width = 100;
            // 
            // columnHeader_location
            // 
            columnHeader_location.Text = "�ݲصص�";
            columnHeader_location.Width = 150;
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
            // columnHeader_price
            // 
            columnHeader_price.Text = "��۸�";
            columnHeader_price.Width = 150;
            columnHeader_price.TextAlign = HorizontalAlignment.Right;
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
            // columnHeader_refID
            // 
            columnHeader_refID.Text = "�ο�ID";
            columnHeader_refID.Width = 100;
            // 
            // columnHeader_biblioRecpath
            // 
            columnHeader_biblioRecpath.Text = "�ּ�¼·��";
            columnHeader_biblioRecpath.Width = 200;

            // 
            // columnHeader_catalogNo
            // 
            columnHeader_catalogNo.Text = "��Ŀ��";
            columnHeader_catalogNo.Width = 150;
            // 
            // columnHeader_orderID
            // 
            columnHeader_orderID.Text = "������";
            columnHeader_orderID.Width = 150;
            // 
            // columnHeader_orderClass
            // 
            columnHeader_orderClass.Text = "������Ŀ";
            columnHeader_orderClass.Width = 150;
            // 
            // columnHeader_orderTime
            // 
            columnHeader_orderTime.Text = "����ʱ��";
            columnHeader_orderTime.Width = 150;
            // 
            // columnHeader_orderPrice
            // 
            columnHeader_orderPrice.Text = "������";
            columnHeader_orderPrice.Width = 150;
            columnHeader_orderPrice.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_acceptPrice
            // 
            columnHeader_acceptPrice.Text = "�����";
            columnHeader_acceptPrice.Width = 150;
            columnHeader_acceptPrice.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_sellerAddress
            // 
            columnHeader_sellerAddress.Text = "������ַ";
            columnHeader_sellerAddress.Width = 200;
            // 
            // columnHeader_orderRecpath
            // 
            columnHeader_orderRecpath.Text = "������¼·��";
            columnHeader_orderRecpath.Width = 200;
            // 
            // columnHeader_acceptSubCopy
            // 
            columnHeader_acceptSubCopy.Text = "��������";
            columnHeader_acceptSubCopy.Width = 200;
            // 
            // columnHeader_orderSubCopy
            // 
            columnHeader_orderSubCopy.Text = "����ʱÿ���ڲ���";
            columnHeader_orderSubCopy.Width = 200;

        }

        // ���� �ϲ�������listview ����Ŀ����
        void CreateMergedColumnHeader(ListView list)
        {
            ColumnHeader columnHeader_seller = new ColumnHeader();
            ColumnHeader columnHeader_catalogNo = new ColumnHeader();
            ColumnHeader columnHeader_errorInfo = new ColumnHeader();
            ColumnHeader columnHeader_isbnIssn = new ColumnHeader();
            ColumnHeader columnHeader_publishTime = new ColumnHeader();
            ColumnHeader columnHeader_volume = new ColumnHeader();
            ColumnHeader columnHeader_mergeComment = new ColumnHeader();
            ColumnHeader columnHeader_range = new ColumnHeader();
            ColumnHeader columnHeader_issueCount = new ColumnHeader();
            ColumnHeader columnHeader_copy = new ColumnHeader();
            ColumnHeader columnHeader_subcopy = new ColumnHeader();
            ColumnHeader columnHeader_orderPrice = new ColumnHeader();
            ColumnHeader columnHeader_price = new ColumnHeader();
            ColumnHeader columnHeader_totalPrice = new ColumnHeader();
            ColumnHeader columnHeader_itemPrice = new ColumnHeader();
            ColumnHeader columnHeader_orderTime = new ColumnHeader();
            ColumnHeader columnHeader_orderID = new ColumnHeader();
            ColumnHeader columnHeader_distribute = new ColumnHeader();
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
            columnHeader_publishTime,
            columnHeader_volume,
            columnHeader_mergeComment,
            columnHeader_range,
            columnHeader_issueCount,
            columnHeader_copy,
            columnHeader_subcopy,
            columnHeader_orderPrice,
            columnHeader_price,
            columnHeader_totalPrice,
            columnHeader_itemPrice,
            columnHeader_orderTime,
            columnHeader_orderID,
            columnHeader_distribute,
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
            // columnHeader_publishTime
            // 
            columnHeader_publishTime.Text = "����ʱ��";
            columnHeader_publishTime.Width = 100;
            // 
            // columnHeader_volume
            // 
            columnHeader_volume.Text = "����";
            columnHeader_volume.Width = 100;
            // 
            // columnHeader_mergeComment
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
            // columnHeader_orderPrice
            // 
            columnHeader_orderPrice.Text = "��������";
            columnHeader_orderPrice.Width = 150;
            columnHeader_orderPrice.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_price
            // 
            columnHeader_price.Text = "���յ���";
            columnHeader_price.Width = 150;
            columnHeader_price.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_totalPrice
            // 
            columnHeader_totalPrice.Text = "�ܼ�";
            columnHeader_totalPrice.Width = 150;
            columnHeader_totalPrice.TextAlign = HorizontalAlignment.Right;
            // 
            // columnHeader_itemPrice
            // 
            columnHeader_itemPrice.Text = "ʵ�嵥��";
            columnHeader_itemPrice.Width = 150;
            columnHeader_itemPrice.TextAlign = HorizontalAlignment.Right;
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
                this.button_print_printAcceptList.Focus();
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
                MessageBox.Show(this, "�Ѿ������һ��page");
            }
            else
            {
                Debug.Assert(false, "δ֪��tabpage״̬");
            }
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
                this.SetNextButtonEnable();
                // this.button_next.Enabled = true;

                // ǿ����ʾ��ԭʼ�����б��Ա��û���ȷ�ع�������
                this.tabControl_items.SelectedTab = this.tabPage_originItems;
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
            List<ListViewItem> Items = new List<ListViewItem>();
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
                for (int i = 0; i < this.Count; i++)
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

        private void button_print_printAcceptList_Click(object sender, EventArgs e)
        {
            int nErrorCount = 0;

            this.tabControl_items.SelectedTab = this.tabPage_mergedItems;

            NamedListViewItemsCollection lists = new NamedListViewItemsCollection();

            // �ȼ���Ƿ��д������˳�㹹��item�б�
            List<ListViewItem> items = new List<ListViewItem>();
            for (int i = 0; i < this.listView_merged.Items.Count; i++)
            {
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

            string strError = "";
            List<string> filenames = new List<string>();
            try
            {
                for (int i = 0; i < lists.Count; i++)
                {
                    List<string> temp_filenames = null;
                    int nRet = PrintMergedList(lists[i],
                        out temp_filenames,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    filenames.AddRange(temp_filenames);
                }

                HtmlPrintForm printform = new HtmlPrintForm();

                printform.Text = "��ӡ���յ�";
                printform.MainForm = this.MainForm;
                printform.Filenames = filenames;
                this.MainForm.AppInfo.LinkFormState(printform, "printaccept_htmlprint_formstate");
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

        int PrintMergedList(NamedListViewItems items,
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
                int nRet = BuildMergedHtml(
                    items,
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

        // ���յ���ӡѡ��
        private void button_print_Option_Click(object sender, EventArgs e)
        {
            // ���ñ���ͷ��
            string strNamePath = "printaccept_printoption";

            PrintOption option = new PrintAcceptPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);


            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.DataDir = this.MainForm.DataDir;
            dlg.Text = this.comboBox_load_type.Text + " ���յ� ��ӡ����";
            dlg.PrintOption = option;
            dlg.ColumnItems = new string[] {
                "no -- ���",
                "seller -- ����",
                "catalogNo -- ��Ŀ��",
                "summary -- ժҪ",
                "isbnIssn -- ISBN/ISSN",
                "publishTime -- ����ʱ��",
                "volume -- ����",
                "mergeComment -- �ϲ�ע��",
                "copy -- ������",
                "series -- ����",
                "subcopy -- ÿ�ײ���",

                "orderPrice -- ��������",
                "acceptPrice -- ���յ���",
                "totalPrice -- �ܼ۸�",
                "itemPrice -- ʵ�嵥��",
                "orderTime -- ����ʱ��",
                "orderID -- ������",
                "distribute -- �ݲط���",
                "orderClass -- ���",
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

        // ������Դ��������
        // ���Ϊ"batchno"��ʽ����Ϊ���κţ����Ϊ"barcodefile"��ʽ����Ϊ������ļ���(���ļ���); ���Ϊ"recpathfile"��ʽ����Ϊ��¼·���ļ���(���ļ���)
        /// <summary>
        /// ������Դ���������֡�
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

                    /*
                    if (String.IsNullOrEmpty(this.LocationString) == false
                        && this.LocationString != "<��ָ��>")
                    {
                        if (String.IsNullOrEmpty(strText) == false)
                            strText += "; ";
                        strText += "�ݲص� " + this.LocationString;
                    }*/

                    return this.BatchNo;
                }
                /*
            else if (this.SourceStyle == "barcodefile")
            {
                return "������ļ� " + Path.GetFileName(this.BarcodeFilePath);
            }
                 * */
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

        // ����htmlҳ��
        // ��ӡ���յ�
        int BuildMergedHtml(
            NamedListViewItems items,
            out List<string> filenames,
            out string strError)
        {
            strError = "";

            Hashtable macro_table = new Hashtable();

            string strNamePath = "printaccept_printoption";

            // ��ô�ӡ����
            PrintOption option = new PrintAcceptPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);

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

            // 2009/7/30 changed
            if (this.SourceStyle == "batchno")
            {
                macro_table["%batchno%"] = HttpUtility.HtmlEncode(this.BatchNo); // ���κ�
            }
            else
            {
                Debug.Assert(this.SourceStyle == "barcodefile"
                    || this.SourceStyle == "recpathfile"
                    || this.SourceStyle == "",
                    "");

                macro_table["%batchno%"] = "";
            }
            macro_table["%seller%"] = items.Seller; // ������
            macro_table["%pagecount%"] = nPageCount.ToString();
            macro_table["%linesperpage%"] = option.LinesPerPage.ToString();
            macro_table["%date%"] = DateTime.Now.ToLongDateString();

            // 2009/7/30 changed
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

            // ���κŻ��ļ��� ����겻�����жϺ�ѡ�ã�ֻ�úϲ�Ϊһ����
            macro_table["%batchno_or_recpathfilename%"] = String.IsNullOrEmpty(this.BatchNo) == true ? Path.GetFileName(this.RecPathFilePath) : this.BatchNo;
            macro_table["%sourcedescription%"] = this.SourceDescription;

            filenames = new List<string>();    // ÿҳһ���ļ�������������������ļ���

            // ��Ҫ�����ڲ�ͬ�������ļ���ǰ׺������
            string strFileNamePrefix = this.MainForm.DataDir + "\\~printaccept_" + items.GetHashCode().ToString() + "_";

            string strFileName = "";

            // �����Ϣҳ
            {
                int nItemCount = items.Count;
                int nTotalCopies = GetMergedTotalCopies(items);
                int nTotalSeries = GetMergedTotalSeries(items);
                int nBiblioCount = GetMergedBiblioCount(items);
                string strTotalPrice = GetMergedTotalPrice(items);

                macro_table["%itemcount%"] = nItemCount.ToString(); // ������
                macro_table["%totalcopies%"] = nTotalCopies.ToString(); // �ܲ���(ע��ÿ�׿����ж��)
                macro_table["%totalseries%"] = nTotalSeries.ToString(); // ������(ע��ÿ������ж���)
                macro_table["%bibliocount%"] = nBiblioCount.ToString(); // ����
                macro_table["%totalprice%"] = strTotalPrice;    // �ܼ۸� ����Ϊ������ֵļ۸�����̬


                macro_table["%pageno%"] = "1";

                // 2009/7/30 new add
                macro_table["%datadir%"] = this.MainForm.DataDir;   // ��������datadir��templatesĿ¼�ڵ�ĳЩ�ļ�
                ////macro_table["%libraryserverdir%"] = this.MainForm.LibraryServerDir;  // �������÷������˵�CSS�ļ�
                // 2009/10/10 new add
                macro_table["%cssfilepath%"] = this.GetAutoCssUrl(option, "printaccept.css");  // �������÷������˻�css��ģ���CSS�ļ�

                strFileName = strFileNamePrefix + "0" + ".html";

                filenames.Add(strFileName);

                string strTemplateFilePath = option.GetTemplatePageFilePath("ͳ��ҳ");
                if (String.IsNullOrEmpty(strTemplateFilePath) == false)
                {
                    /*
<html>
<head>
    ���÷�<LINK href='%libraryserverdir%/printaccept.css' type='text/css' rel='stylesheet'>
	���÷�<LINK href='%cssfilepath%' type='text/css' rel='stylesheet'>
</head>
<body>
    <div class='pageheader'>%date% %seller% ���յ� - ��Դ: %sourcedescription% - (�� %pagecount% ҳ)</div>
    <div class='tabletitle'>%date% %seller% ���յ�</div>
    <div class='seller'>����: %seller%</div>
    <div class='copies'>����: %totalcopies%</div>
    <div class='bibliocount'>����: %bibliocount%</div>
    <div class='totalprice'>�ܼ�: %totalprice%</div>
    <div class='sepline'><hr/></div>
    <div class='batchno'>���κ�: %batchno%</div>
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

                    BuildMergedPageTop(option,
                        macro_table,
                        strFileName,
                        false);

                    // ������
                    StreamUtil.WriteText(strFileName,
                        "<div class='seller'>����: " + items.Seller + "</div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='bibliocount'>����: " + nBiblioCount.ToString() + "</div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='series'>����: " + nTotalSeries.ToString() + "</div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='copies'>����: " + nTotalCopies.ToString() + "</div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='totalprice'>�ܼ�: " + strTotalPrice + "</div>");

                    BuildMergedPageBottom(option,
                        macro_table,
                        strFileName,
                        false);
                }

            }


            string strMarcFilterFilePath = option.GetTemplatePageFilePath("MARC������");
            if (String.IsNullOrEmpty(strMarcFilterFilePath) == false)
            {
                int nRet = PrepareMarcFilter(strMarcFilterFilePath, out strError);
                if (nRet == -1)
                    return -1;
            }

            // ���ҳѭ��
            for (int i = 0; i < nTablePageCount; i++)
            {
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
                    BuildMergedTableLine(option,
                        items,
                        strFileName, i, j);
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

            // string strCssUrl = this.MainForm.LibraryServerDir + "/printaccept.css";
            // 2009/10/10 changed
            string strCssUrl = GetAutoCssUrl(option, "printaccept.css");

            /*
            // 2009/10/9 new add
            string strCssFilePath = option.GetTemplatePageFilePath("css");  // ��Сд������
            if (String.IsNullOrEmpty(strCssFilePath) == false)
                strCssUrl = strCssFilePath;
            else
                strCssUrl = this.MainForm.LibraryServerDir + "/printaccept.css";    // ȱʡ��
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

                    string strClass = PrintOrderForm.GetClass(column.Name);

                    StreamUtil.WriteText(strFileName,
                        "<td class='" + strClass + "'>" + strCaption + "</td>");
                }

                StreamUtil.WriteText(strFileName,
                    "</tr>");

            }

            return 0;
        }

        int BuildMergedTableLine(PrintOption option,
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
                string strBiblioRecPath = ListViewUtil.GetItemText(item, MERGED_COLUMN_BIBLIORECPATH);

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

                string strContent = GetMergedColumnContent(item,
                    column.Name);

                if (strContent == "!!!#")
                    strContent = ((nPage * option.LinesPerPage) + nLine + 1).ToString();

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

                string strClass = PrintOrderForm.GetClass(column.Name);

                strLineContent +=
                    "<td class='" + strClass + "'>" + strContent + "</td>";
            }

        END1:
            StreamUtil.WriteText(strFileName,
    "<tr class='content'>");

            StreamUtil.WriteText(strFileName,
    strLineContent);

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
            PrintOrderForm.ParseNameParam(StringUtil.GetLeft(strColumnName),
                out strName,
                out strParameters);

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
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER);

                    case "catalogNo":
                    case "��Ŀ��":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_CATALOGNO);

                    case "errorInfo":
                    case "summary":
                    case "ժҪ":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_SUMMARY);

                    case "isbnIssn":
                    case "ISBN/ISSN":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_ISBNISSN);

                    case "publishTime":
                    case "����ʱ��":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_PUBLISHTIME);

                    case "volume":
                    case "����":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_VOLUME);


                    case "mergeComment":
                    case "�ϲ�ע��":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_MERGECOMMENT);

                    case "copy":
                    case "������":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_COPY);

                    case "subcopy":
                    case "ÿ�ײ���":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_SUBCOPY);

                    case "series":
                    case "����":
                        {
                            string strCopy = ListViewUtil.GetItemText(item, MERGED_COLUMN_COPY);
                            string strSubCopy = ListViewUtil.GetItemText(item, MERGED_COLUMN_SUBCOPY);
                            if (String.IsNullOrEmpty(strSubCopy) == true)
                                return strCopy;

                            return strCopy + "(ÿ�׺� " + strSubCopy + " ��)";
                        }

                    case "orderPrice":
                    case "��������":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_ORDERPRICE);


                    case "price":
                    case "acceptPrice":
                    case "����":
                    case "���յ���":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_PRICE);

                    case "totalPrice":
                    case "�ܼ۸�":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_TOTALPRICE);

                        // 2013/5/31
                    case "itemPrice":
                    case "ʵ�嵥��":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_ITEMPRICE);


                    case "orderTime":
                    case "����ʱ��":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_ORDERTIME);

                    case "orderID":
                    case "������":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_ORDERID);


                    // û��recpath��¼·������Ϊrecpath�Ѿ����롰�ϲ�ע�͡���
                    // û��state״̬����Ϊstate����ȫ������Ϊ���Ѷ�����
                    // û��source������Դ����Ϊ�Ѿ����롰�ϲ�ע�͡���
                    // û��batchNo���κţ���Ϊԭʼ�����Ѿ��ϲ������ԭʼ���һ��������ͬ�����κ�

                    case "distribute":
                    case "�ݲط���":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_DISTRIBUTE);

                    case "class":
                    case "orderClass":
                    case "���":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_CLASS);


                    case "comment":
                    case "ע��":
                    case "��ע":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_COMMENT);

                    case "biblioRecpath":
                    case "�ּ�¼·��":
                        return ListViewUtil.GetItemText(item, MERGED_COLUMN_BIBLIORECPATH);

                    // ��ʽ���Ժ��������ַ
                    case "sellerAddress":
                    case "������ַ":
                        return PrintOrderForm.GetPrintableSellerAddress(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            strParameters);

                    case "sellerAddress:zipcode":
                    case "������ַ:��������":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "zipcode");

                    case "sellerAddress:address":
                    case "������ַ:��ַ":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "address");

                    case "sellerAddress:department":
                    case "������ַ:��λ":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "department");

                    case "sellerAddress:name":
                    case "������ַ:��ϵ��":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "name");

                    case "sellerAddress:email":
                    case "������ַ:Email��ַ":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "email");

                    case "sellerAddress:bank":
                    case "������ַ:������":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "bank");

                    case "sellerAddress:accounts":
                    case "������ַ:�����˺�":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "accounts");

                    case "sellerAddress:payStyle":
                    case "������ַ:��ʽ":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "payStyle");

                    case "sellerAddress:comment":
                    case "������ַ:��ע":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, MERGED_COLUMN_SELLER),
                            "comment");
                    default:
                        {
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
        "<div class='pagefooter'>" + strPageFooterText + "</div>");
            }


            StreamUtil.WriteText(strFileName, "</body></html>");

            return 0;
        }

        static int GetMergedBiblioCount(NamedListViewItems items)
        {
            List<string> paths = new List<string>();

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strText = "";

                try
                {
                    strText = ListViewUtil.GetItemText(item, MERGED_COLUMN_BIBLIORECPATH);
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

        // �ϲ����������
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
                    strCopy = ListViewUtil.GetItemText(item, MERGED_COLUMN_COPY);

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

        // �ϲ�����ܲ���
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
                    strCopy = ListViewUtil.GetItemText(item, MERGED_COLUMN_COPY);

                    // TODO: ע�����Ƿ���[]����?
                    nCopy = Convert.ToInt32(strCopy);
                }
                catch
                {
                    continue;
                }

                int nSubCopy = 1;
                string strSubCopy = "";
                strSubCopy = ListViewUtil.GetItemText(item, MERGED_COLUMN_SUBCOPY);
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


        static string GetMergedTotalPrice(NamedListViewItems items)
        {
            List<string> prices = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strPrice = "";

                try
                {
                    strPrice = ListViewUtil.GetItemText(item, MERGED_COLUMN_TOTALPRICE);
                }
                catch
                {
                    continue;
                }

                if (String.IsNullOrEmpty(strPrice) == true)
                    continue;

                prices.Add(strPrice);
            }

            return PriceUtil.TotalPrice(prices);
        }

        // �����������κż���װ��
        private void button_load_loadFromAcceptBatchNo_Click(object sender, EventArgs e)
        {
            LoadFromAcceptBatchNo(null);
        }

        // ���ݶ������κż���װ��
        private void button_load_loadFromOrderBatchNo_Click(object sender, EventArgs e)
        {
            LoadFromOrderBatchNo(null);
        }

        // �����������κż���װ������
        // parameters:
        //      bAutoSetSeriesType  �Ƿ�������еĵ�һ����¼��·���е����ݿ������Զ�����Combobox_type
        //      strDefaultBatchNo   ȱʡ�����κš����Ϊnull�����ʾ��ʹ�����������
        /// <summary>
        /// �����������κż���װ������
        /// </summary>
        /// <param name="strDefaultBatchNo">ȱʡ�����κš����Ϊ null�����ʾ��ʹ���������</param>
        public void LoadFromAcceptBatchNo(
            // bool bAutoSetSeriesType,
            string strDefaultBatchNo)
        {
            SearchByBatchnoForm dlg = new SearchByBatchnoForm();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.CfgSectionName = "PrintAcceptForm_SearchByAcceptBatchnoForm";
            this.BatchNo = "";

            if (strDefaultBatchNo != null)
                dlg.BatchNo = strDefaultBatchNo;

            dlg.Text = "��������(��)���κż����������յĲ��¼";
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

            this.SourceStyle = "batchno";

            this.BatchNo = dlg.BatchNo;

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
                        "PrintAcceptForm",
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


                this.refid_table.Clear();
                this.orderxml_table.Clear();
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
                    lRet = Channel.SearchItem(
                    stop,
                    this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
                        //"<all>",
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
this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
                        //"<all>",
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
                Record[] searchresults = null;

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

#if NO
                        // ��Ϊ������ʱ��������ͣ���������û�б�Ҫ�����ͽ��м����
                        string strItemDbName = Global.GetDbName(strRecPath);
                        // ������ݿ�����(ͼ��/�ڿ�)
                        // �۲�ʵ����ǲ����ڿ�������
                        // return:
                        //      -1  ����ʵ���
                        //      0   ͼ������
                        //      1   �ڿ�����
                        nRet = this.MainForm.IsSeriesTypeFromItemDbName(strItemDbName);
                        if (nRet == -1)
                        {
                            strError = "��¼·�� '" + strRecPath + "' �е����ݿ��� '" + strItemDbName + "' ����ʵ�����";
                            goto ERROR1;
                        }

                        // �Զ����� ͼ��/�ڿ� ����
                        if (bAutoSetSeriesType == true && lStart + i == 0)
                        {
                            if (nRet == 0)
                                this.comboBox_load_type.Text = "ͼ��";
                            else
                                this.comboBox_load_type.Text = "����������";
                        }

                        // ����¼·��
                        if (this.comboBox_load_type.Text == "ͼ��")
                        {
                            if (nRet != 0)
                            {
                                strError = "��¼·�� '" + strRecPath + "' �е����ݿ��� '" + strItemDbName + "' ����һ��ͼ�����͵�ʵ�����";
                                goto ERROR1;
                            }
                        }
                        else
                        {
                            if (nRet != 1)
                            {
                                strError = "��¼·�� '" + strRecPath + "' �е����ݿ��� '" + strItemDbName + "' ����һ���������������͵�ʵ�����";
                                goto ERROR1;
                            }
                        }
#endif

                        // ���ݼ�¼·����װ����¼
                        // return: 
                        //      -2  ·���Ѿ���list�д�����
                        //      -1  ����
                        //      1   �ɹ�
                        nRet = LoadOneItem(
                            this.comboBox_load_type.Text,
                            strRecPath,
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

                // ������ڲ�������
                stop.SetMessage("���� ������ڲ�������...");
                nRet = CheckSubCopy(out strError);
                if (nRet == -1)
                    goto ERROR1;


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
                "item",
                this.stop,
                this.Channel);

#if NOOOOOOOOOOOOOOOOOOOOOOOOOO
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
                    this.comboBox_load_type.Text == "ͼ��" ? "<all book>" : "<all series>",
                    // "<all>",
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

        // ���ݶ������κż���װ������
        // parameters:
        //      strDefaultBatchNo   ȱʡ�����κš����Ϊnull�����ʾ��ʹ�����������
        /// <summary>
        /// ���ݶ������κż���װ������
        /// </summary>
        /// <param name="strDefaultBatchNo">ȱʡ�����κš����Ϊ null�����ʾ��ʹ���������</param>
        public void LoadFromOrderBatchNo(string strDefaultBatchNo)
        {
            SearchByBatchnoForm dlg = new SearchByBatchnoForm();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.CfgSectionName = "PrintAcceptForm_SearchByOrderBatchnoForm";
            this.BatchNo = "";

            dlg.Text = "���ݶ������κż����������յĲ��¼";
            dlg.DisplayLocationList = false;

            dlg.GetLocationValueTable -= new GetValueTableEventHandler(dlg_GetOrderLocationValueTable);
            dlg.GetLocationValueTable += new GetValueTableEventHandler(dlg_GetOrderLocationValueTable);

            dlg.GetBatchNoTable -= new GetKeyCountListEventHandler(dlg_GetOrderBatchNoTable);
            dlg.GetBatchNoTable += new GetKeyCountListEventHandler(dlg_GetOrderBatchNoTable);

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
                        "PrintAcceptForm",
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

                this.refid_table.Clear();
                this.orderxml_table.Clear();
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
                    // 2013/3/27
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
                        strError = "����ȫ�� '" + this.comboBox_load_type.Text + "' ���͵Ķ�����¼û�����м�¼��";
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

                int nOrderRecCount = 0;
                int nItemRecCount = 0;

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
                        "id", // "id,cols",
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

                        // ���ݶ�����¼·����װ�붩����¼�����������ղ��¼
                        // return: 
                        //      -2  ·���Ѿ���list�д�����
                        //      -1  ����
                        //      1   �ɹ�
                        nRet = LoadOrderItem(strRecPath,
                            this.listView_origin,
                            ref nOrderRecCount,
                            ref nItemRecCount,
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

                // ������ڲ�������
                stop.SetMessage("���� ������ڲ�������...");
                nRet = CheckSubCopy(out strError);
                if (nRet == -1)
                    goto ERROR1;


                // ���ϲ��������б�
                stop.SetMessage("���ںϲ�����...");
                nRet = FillMergedList(out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nItemRecCount == 0)
                {
                    MessageBox.Show(this, "δװ���κ������յĲ��¼ (��������¼ "+nOrderRecCount.ToString()+" ��)");
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

            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ���ݼ�¼·����������������¼������װ��������������ղ��¼
        // parameters:
        //      nOrderRecPath   �ܹ������Ķ�����¼����
        //      nItemRecCount   �ܹ�װ��Ĳ��¼����
        // return: 
        //      -2  ·���Ѿ���list�д�����
        //      -1  ����
        //      1   �ɹ�
        int LoadOrderItem(string strRecPath,
            ListView list,
            ref int nOrderRecCount,
            ref int nItemRecCount,
            out string strError)
        {
            strError = "";

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
                ListViewItem item = new ListViewItem(strRecPath, 0);

                OriginAcceptItemData data = new OriginAcceptItemData();
                item.Tag = data;
                data.Timestamp = item_timestamp;
                data.Xml = strOrderXml;

                item.SubItems.Add("��ȡ������¼ "+strRecPath+" ʱ����: " + strError);

                SetItemColor(item, TYPE_ERROR);
                list.Items.Add(item);

                // ���¼�������������Ұ
                list.EnsureVisible(list.Items.Count - 1);

                return -1;
            }

            // ����һ������xml��¼��ȡ���й���Ϣ����listview��
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strOrderXml);
            }
            catch (Exception ex)
            {
                strError = "������¼XMLװ��DOMʱ����: " + ex.Message;
                goto ERROR1;
            }


            List<string> distributes = new List<string>();


            if (this.comboBox_load_type.Text == "����������")
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

            nOrderRecCount++;

#if NO
            string strDistribute = DomUtil.GetElementText(dom.DocumentElement, "distribute");
            if (string.IsNullOrEmpty(strDistribute) == true)
                return 0;
#endif
            foreach (string strDistribute in distributes)
            {

                LocationCollection locations = new LocationCollection();
                int nRet = locations.Build(strDistribute,
                    out strError);
                if (nRet == -1)
                    return -1;

                for (int i = 0; i < locations.Count; i++)
                {
                    DigitalPlatform.Location location = locations[i];

                    if (string.IsNullOrEmpty(location.RefID) == true)
                        continue;

                    string[] parts = location.RefID.Split(new char[] { '|' });
                    foreach (string text in parts)
                    {
                        string strRefID = text.Trim();
                        if (string.IsNullOrEmpty(strRefID) == true)
                            continue;

                        // ���ݲ��¼��refidװ����¼
                        // return: 
                        //      -2  ·���Ѿ���list�д�����
                        //      -1  ����
                        //      1   �ɹ�
                        nRet = LoadOneItem(this.comboBox_load_type.Text,
                            "@refID:" + strRefID,
                            list,
                            out strError);
                        if (nRet == -2)
                            continue;
                        if (nRet == -1)
                            continue;

                        nItemRecCount++;
                    }
                }
            }

            return 1;
        ERROR1:
            {
                ListViewItem item = new ListViewItem(strRecPath, 0);

                OriginAcceptItemData data = new OriginAcceptItemData();
                item.Tag = data;
                data.Timestamp = item_timestamp;
                data.Xml = strOrderXml;

                item.SubItems.Add(strError);

                SetItemColor(item, TYPE_ERROR);
                list.Items.Add(item);

                // ���¼�������������Ұ
                list.EnsureVisible(list.Items.Count - 1);
            }
            return -1;
        }

        void dlg_GetOrderBatchNoTable(object sender, GetKeyCountListEventArgs e)
        {
            Global.GetBatchNoTable(e,
                this,
                this.comboBox_load_type.Text,
                "order",
                this.stop,
                this.Channel);
        }

        void dlg_GetOrderLocationValueTable(object sender, GetValueTableEventArgs e)
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
                    strThisText = ListViewUtil.GetItemText(item, nSortColumn);
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

            string strItemRecPath = "";
            if (this.listView_origin.SelectedItems.Count > 0)
            {
                strItemRecPath = ListViewUtil.GetItemText(this.listView_origin.SelectedItems[0], ORIGIN_COLUMN_RECPATH);
            }
            menuItem = new MenuItem("���ֲᴰ���۲���¼ '"+strItemRecPath+"' (&I)");
            menuItem.Tag = this.listView_origin;
            menuItem.Click += new System.EventHandler(this.menu_loadItemRecord_Click);
            if (this.listView_origin.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            string strOrderRecPath = "";
            if (this.listView_origin.SelectedItems.Count > 0)
            {
                strOrderRecPath = ListViewUtil.GetItemText(this.listView_origin.SelectedItems[0],ORIGIN_COLUMN_ORDERRECPATH);
            }
            menuItem = new MenuItem("���ֲᴰ���۲충����¼ '" + strOrderRecPath + "' (&O)");
            menuItem.Tag = this.listView_origin;
            menuItem.Click += new System.EventHandler(this.menu_loadOrderRecord_Click);
            if (this.listView_origin.SelectedItems.Count == 0
                || String.IsNullOrEmpty(strOrderRecPath) == true)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
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

        // ���ֲᴰ���۲���¼
        void menu_loadItemRecord_Click(object sender, EventArgs e)
        {
            LoadItemToEntityForm(this.listView_origin);
        }

        void LoadItemToEntityForm(ListView list)
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

            form.LoadItemByRecPath(strRecPath, false);
        }

#if NO
        void LoadToEntityForm(ListView list)
        {
            if (list.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ�ص�����");
                return;
            }

            string strBarcode = "";
            string strRecPath = ListViewUtil.GetItemText(list.SelectedItems[0], ORIGIN_COLUMN_RECPATH);
            string strRefID = ListViewUtil.GetItemText(list.SelectedItems[0], ORIGIN_COLUMN_REFID);

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
#endif

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

            string strRecPath = ListViewUtil.GetItemText(list.SelectedItems[0], ORIGIN_COLUMN_ORDERRECPATH);

            EntityForm form = new EntityForm();

            form.MainForm = this.MainForm;
            form.MdiParent = this.MainForm;
            form.Show();

            if (this.comboBox_load_type.Text == "ͼ��")
                form.LoadOrderByRecPath(strRecPath, false);
            else
                form.LoadIssueByRecPath(strRecPath, false);
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
                MessageBox.Show(this, "��δѡ��Ҫ�Ƴ�������");
                return;
            }

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in list.SelectedItems)
            {
                items.Add(item);
            }

            DialogResult result = MessageBox.Show(this,
                "ȷʵҪ��ԭʼ�����б������Ƴ�ѡ���� " + items.Count.ToString() + " ������?",
                "PrintAcceptForm",
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

            string strIndex = "@path:" + ListViewUtil.GetItemText(item, ORIGIN_COLUMN_RECPATH);

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
                ListViewUtil.ChangeItemText(item, 1, strError); // 1 ��������listview������
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
            return 1;
        ERROR1:
            return -1;
        }

        /*
        int FillMergedList(out string strError)
        {
            strError = "";
            int nRet = 0;

            return 0;
        }*/

        int OriginIndex(ListViewItem item)
        {
            return this.listView_origin.Items.IndexOf(item);
        }

        /*public*/ class Tao
        {
            public string OrderRecPath = "";    // ������¼·��
            public string No = "";  // ����� ��1��ʼ����
            public int Count = 0;   // ����
            public List<string> IDs = new List<string>();   // �������Ĳ���š���1��ʼ����
            public string ErrorInfo = "";
        }

        static int AddOneItem(ref List<Tao> taos,
            string strOrderRecPath,
            string strNo,
            string strID,
            string strCount,
            out string strError)
        {
            strError = "";

            Tao tao = null;
            for(int i=0;i<taos.Count;i++)
            {
                Tao current_tao = taos[i];
                if (current_tao.OrderRecPath == strOrderRecPath
                    && current_tao.No == strNo)
                {
                    tao = current_tao;
                    break;  // found
                }
            }

            if (tao == null)
            {
                tao = new Tao();
                tao.OrderRecPath = strOrderRecPath;
                tao.No = strNo;
                taos.Add(tao);
            }

            int nCount = 0;

            try
            {
                nCount = Convert.ToInt32(strCount);
            }
            catch
            {
                strError = "strCount '"+strCount+"' ��ʽ����ȷ��Ӧ��Ϊ����";
                return -1;
            }

            if (tao.Count == 0)
                tao.Count = nCount;
            else
            {
                if (nCount != tao.Count)
                    tao.ErrorInfo = "���ֲ�һ�µĲ��� '"+strCount+"'";
            }

            tao.IDs.Add(strID);
            return 0;
        }

        // ������ڲ��������
        int CheckSubCopy(out string strError)
        {
            strError = "";
            int nRet = 0;

            List<Tao> taos = new List<Tao>();

            for (int i = 0; i < this.listView_origin.Items.Count; i++)
            {
                ListViewItem source = this.listView_origin.Items[i];

                string strSubCopy = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_ACCEPTSUBCOPY);
                if (String.IsNullOrEmpty(strSubCopy) == true)
                    continue;

                string strNo = "";
                string strIndex = "";
                string strCopy = "";
                nRet = ParseSubCopy(strSubCopy,
        out strNo,
        out strIndex,
        out strCopy,
        out strError);
                if (nRet == -1)
                {
                    strError = "���� " + (i + 1).ToString() + " �����������ʽ����: '" + strError + "'�������ų�����...";
                    return -1;
                }

                string strOrderRecPath = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_ORDERRECPATH);

                nRet = AddOneItem(ref taos,
                    strOrderRecPath,
                    strNo,
                    strIndex,
                    strCopy,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            // ���
            for (int i = 0; i < taos.Count; i++)
            {
                Tao tao = taos[i];
                if (String.IsNullOrEmpty(tao.ErrorInfo) == false)
                {
                    strError = "������¼ '" + tao.OrderRecPath + "' �ڵ� " + tao.No + " �� " + tao.ErrorInfo;
                    return -1;
                }

                // ������������
                // 2014/6/21 �޸����򷽷�
                tao.IDs.Sort(
                    delegate(string s1, string s2)
                    {
                        return StringUtil.RightAlignCompare(s1, s2);
                    }
                    );
                for (int j = 0; j < tao.IDs.Count; j++)
                {
                    if (j > 0)
                    {
                        string strID1 = tao.IDs[j - 1];
                        string strID2 = tao.IDs[j];

                        int v1 = 0;
                        int v2 = 0;
                        try
                        {
                            v1 = Convert.ToInt32(strID1);
                        }
                        catch
                        {
                            strError = "��� '"+strID1+"' ��ʽ����";
                            return -1;
                        }
                        try
                        {
                            v2 = Convert.ToInt32(strID2);
                        }
                        catch
                        {
                            strError = "��� '" + strID2 + "' ��ʽ����";
                            return -1;
                        }

                        if (v1 + 1 != v2)
                        {
                            strError = "��� '" + strID1 + "' �� '" + strID2 + "' ֮�䲻����";
                            return -1;
                        }
                    }
                }

                if (tao.Count != tao.IDs.Count)
                {
                    strError = "������¼ '"+tao.OrderRecPath+"' �ڵ� "+tao.No+" ��Ӧ�� "+tao.Count.ToString()+" �ᣬ����ǰֻ�� "+tao.IDs.Count.ToString()+" �ᣬ�����˲��������";
                    return -1;
                }

            }

            return 0;
        }

        // ���ϲ��������б�
        int FillMergedList(out string strError)
        {
            strError = "";
            int nRet = 0;
            List<int> null_acceptprice_lineindexs = new List<int>();

            DateTime now = DateTime.Now;
            // int nOrderIdSeed = 1;

            this.listView_merged.Items.Clear();
            // 2008/11/22 new add
            this.SortColumns_merged.Clear();
            SortColumns.ClearColumnSortDisplay(this.listView_merged.Columns);

            // �Ƚ�ԭʼ�����б��� bibliorecpath/seller/price ������
            SortOriginListForMerge();


            // ֻ��ȡһ��һ��ģ���һ�׵ĵ�һ��
            List<ListViewItem> items = new List<ListViewItem>();
            for (int i = 0; i < this.listView_origin.Items.Count; i++)
            {
                ListViewItem source = this.listView_origin.Items[i];

                string strSubCopy = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_ACCEPTSUBCOPY);
                if (String.IsNullOrEmpty(strSubCopy) == false)
                {
                    string strNo = "";
                    string strIndex = "";
                    string strCopy = "";
                    nRet = ParseSubCopy(strSubCopy,
            out strNo,
            out strIndex,
            out strCopy,
            out strError);
                    if (nRet == -1)
                    {
                        strError = "���� " + (i + 1).ToString() + " �����������ʽ����: '" + strError + "'�������ų�����...";
                        return -1;
                    }

                    // ���Ǹ��׵ĵ�һ�ᣬ������
                    if (strIndex != "1")
                        continue;
                }

                items.Add(source);
            }

            // ѭ��
            for (int i = 0; i < items.Count; i++)
            {
                int nCopy = 0;

                ListViewItem source = items[i];

                if (source.ImageIndex == TYPE_ERROR)
                {
                    strError = "���� " + (OriginIndex(source) + 1).ToString() + " ��״̬Ϊ���������ų�����...";
                    return -1;
                }

                string strSubCopyDetail = ListViewUtil.GetItemText(source,
    ORIGIN_COLUMN_ACCEPTSUBCOPY);
                string strSubCopy = "";
                if (String.IsNullOrEmpty(strSubCopyDetail) == false)
                {
                    string strNo = "";
                    string strIndex = "";
                    nRet = ParseSubCopy(strSubCopyDetail,
            out strNo,
            out strIndex,
            out strSubCopy,
            out strError);
                    if (nRet == -1)
                    {
                        strError = "���� " + (OriginIndex(source) + 1).ToString() + " �����������ʽ����: '" + strError + "'�������ų�����...";
                        return -1;
                    }

                    Debug.Assert(strIndex == "1", "");
                }

                // ����
                string strSeller = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_SELLER);

                // ������ַ
                string strSellerAddress = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_SELLERADDRESS);

                // 2013/5/31
                // ���ۣ�ʵ���
                string strItemPrice = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_ITEMPRICE); 


                // 2013/5/31
                // ���ۣ�������
                string strOrderPrice = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_ORDERPRICE); 


                // ���ۣ������
                string strAcceptPrice = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_ACCEPTPRICE);  // 2009/11/23 changed

                string strPublishTime = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_PUBLISHTIME);

                string strVolume = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_VOLUME);

                /*
                // priceȡ���е����ռ۲���
                {
                    string strOldPrice = "";
                    string strNewPrice = "";

                    // ���� "old[new]" �ڵ�����ֵ
                    OrderDesignControl.ParseOldNewValue(strPrice,
                        out strOldPrice,
                        out strNewPrice);

                    strPrice = strNewPrice;
                }*/

                // ��Ŀ��¼·��
                string strBiblioRecPath = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_BIBLIORECPATH);

                // ��Ŀ��
                string strCatalogNo = ListViewUtil.GetItemText(source,
                    ORIGIN_COLUMN_CATALOGNO);

                string strMergeComment = "";    // �ϲ�ע��
                List<string> totalprices = new List<string>();  // �ۻ��ļ۸��ַ���
                List<ListViewItem> origin_items = new List<ListViewItem>();

                string strComments = "";    // ԭʼע��(����)
                string strDistributes = ""; // �ϲ��Ĺݲط����ַ���

                // ����biblioRecPath��price��seller����ͬ������
                int nStart = i; // ���ο�ʼλ��
                int nLength = 0;    // �������������

                for (int j = i; j < items.Count; j++)
                {
                    ListViewItem current_source = items[j];

                    if (current_source.ImageIndex == TYPE_ERROR)
                    {
                        strError = "���� " + (OriginIndex(current_source) + 1).ToString() + " ��״̬Ϊ���������ų�����...";
                        return -1;
                    }

                    // ����
                    string strCurrentSeller = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_SELLER);

                    // ������ַ
                    string strCurrentSellerAddress = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_SELLERADDRESS);

                    string strCurrentItemPrice = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_ITEMPRICE);

                    string strCurrentOrderPrice = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_ORDERPRICE);

                    // ����(���¼�е�)
                    string strCurrentAcceptPrice = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_ACCEPTPRICE); // 2009/11/23 changed

                    string strCurrentPublishTime = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_PUBLISHTIME);

                    string strCurrentVolume = ListViewUtil.GetItemText(current_source,
                       ORIGIN_COLUMN_VOLUME);
                    /*
                    // priceȡ���е����ռ۲���
                    {
                        string strCurrentOldPrice = "";
                        string strCurrentNewPrice = "";

                        // ���� "old[new]" �ڵ�����ֵ
                        OrderDesignControl.ParseOldNewValue(strCurrentPrice,
                            out strCurrentOldPrice,
                            out strCurrentNewPrice);

                        strCurrentPrice = strCurrentNewPrice;
                    }*/

                    // ��Ŀ��¼·��
                    string strCurrentBiblioRecPath = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_BIBLIORECPATH);

                    // ��Ŀ��
                    string strCurrentCatalogNo = ListViewUtil.GetItemText(current_source,
                        ORIGIN_COLUMN_CATALOGNO);

                    if (this.comboBox_load_type.Text == "ͼ��")
                    {
                        // ��Ԫ���ж�
                        if (strBiblioRecPath != strCurrentBiblioRecPath
                            || strSeller != strCurrentSeller
                            || strItemPrice != strCurrentItemPrice
                            || strOrderPrice != strCurrentOrderPrice
                            || strAcceptPrice != strCurrentAcceptPrice
                            || strCatalogNo != strCurrentCatalogNo
                            || PrintOrderForm.CompareAddress(strSellerAddress, strCurrentSellerAddress) != 0)
                            break;
                    }
                    else
                    {
                        Debug.Assert(this.comboBox_load_type.Text == "����������", "");
                        // ��Ԫ���ж�
                        if (strBiblioRecPath != strCurrentBiblioRecPath
                            || strSeller != strCurrentSeller
                            || strItemPrice != strCurrentItemPrice
                            || strOrderPrice != strCurrentOrderPrice
                            || strAcceptPrice != strCurrentAcceptPrice
                            || strCatalogNo != strCurrentCatalogNo
                            || strPublishTime != strCurrentPublishTime
                            || strVolume != strCurrentVolume
                            || PrintOrderForm.CompareAddress(strSellerAddress, strCurrentSellerAddress) != 0)
                            break;
                    }

                    int nCurCopy = 1;

                    // ���ܸ�����
                    nCopy += nCurCopy;

                    // ���ܺϲ�ע��
                    string strSource = ListViewUtil.GetItemText(current_source, ORIGIN_COLUMN_SOURCE);
                    string strRecPath = ListViewUtil.GetItemText(current_source, ORIGIN_COLUMN_RECPATH);
                    if (String.IsNullOrEmpty(strMergeComment) == false)
                        strMergeComment += "; ";
                    strMergeComment += strSource + ", " + nCurCopy.ToString() + "�� (" + strRecPath + ")";

                    if (String.IsNullOrEmpty(strCurrentAcceptPrice) == true)
                    {
                        // ���ؾ��пյ���۵��к�
                        null_acceptprice_lineindexs.Add(j);
                    }
                    else
                    {
                        // ���ܼ۸�
                        totalprices.Add(strCurrentAcceptPrice);
                    }

                    // ����ע��
                    string strComment = ListViewUtil.GetItemText(current_source, ORIGIN_COLUMN_COMMENT);
                    if (String.IsNullOrEmpty(strComment) == false)
                    {
                        if (String.IsNullOrEmpty(strComments) == false)
                            strComments += "; ";
                        strComments += strComment + " @" + strRecPath;
                    }

                    // ���ܹݲط����ַ���
                    string strCurDistribute = ListViewUtil.GetItemText(current_source, ORIGIN_COLUMN_LOCATION) + ":1";
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

                // publish time
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_PUBLISHTIME,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_PUBLISHTIME));

                // volume
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_VOLUME,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_VOLUME));


                // merge comment
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_MERGECOMMENT,
                    strMergeComment);

                // copy
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_COPY,
                    nCopy.ToString());

                // subcopy
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_SUBCOPY,
                    strSubCopy);

                // item price
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ITEMPRICE,
                    strItemPrice);

                // order price
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ORDERPRICE,
                    strOrderPrice);

                // price
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_PRICE,
                    strAcceptPrice);

                List<string> sum_prices = null;
                nRet = TotalPrice(totalprices,
                    out sum_prices,
                    out strError);
                if (nRet == -1)
                {
                    // TODO: �����ʱ��Ҫ�г������е�ÿ���ַ������������
                    return -1;
                }

                string strSumPrice = "";

                if (sum_prices.Count > 0)
                {
                    Debug.Assert(sum_prices.Count == 1, "");
                    strSumPrice = sum_prices[0];
                }
                // total price
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_TOTALPRICE,
                    strSumPrice);

                /* ��ӡ���յ�ʱ��
                // order time
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ORDERTIME,
                    now.ToShortDateString());   // TODO: ע�����ʱ��Ҫ���ص�ԭʼ������
                 * */

                // order time ��Ҫ�鲢����
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ORDERTIME,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_ORDERTIME));


                // order id ��Ҫ�鲢����
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_ORDERID,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_ORDERID));


                // distribute
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_DISTRIBUTE,
                    strDistributes);

                // class ��Ҫ�鲢����
                ListViewUtil.ChangeItemText(target, MERGED_COLUMN_CLASS,
                    ListViewUtil.GetItemText(source, ORIGIN_COLUMN_ORDERCLASS));

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

            if (null_acceptprice_lineindexs.Count > 0)
            {
                ListViewUtil.ClearSelection(this.listView_origin);
                for (int i = 0; i < null_acceptprice_lineindexs.Count; i++)
                {
                    this.listView_origin.Items[null_acceptprice_lineindexs[i]].Selected = true;
                }
                MessageBox.Show(this, "ԭʼ�����б��й��� " + null_acceptprice_lineindexs.Count.ToString() + "  �еĵ����Ϊ�գ��ѱ�ѡ������ע����");
            }

            return 0;
        }


        // ����listview item��changed״̬
        static void SetItemChanged(ListViewItem item,
            bool bChanged)
        {
            OriginAcceptItemData data = (OriginAcceptItemData)item.Tag;
            if (data == null)
            {
                data = new OriginAcceptItemData();
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

        // ���ܼ۸�
        // ���ҵ�λ��ͬ�ģ��������
        static int TotalPrice(List<string> prices,
            out List<string> results,
            out string strError)
        {
            strError = "";
            results = new List<string>();

            List<PriceItem> items = new List<PriceItem>();

            // �任ΪPriceItem
            for (int i = 0; i < prices.Count; i++)
            {
                string strPrefix = "";
                string strValue = "";
                string strPostfix = "";
                int nRet = PriceUtil.ParsePriceUnit(prices[i],
                    out strPrefix,
                    out strValue,
                    out strPostfix,
                    out strError);
                if (nRet == -1)
                    return -1;
                decimal value = 0;
                try
                {
                    value = Convert.ToDecimal(strValue);
                }
                catch
                {
                    strError = "���� '" + strValue + "' ��ʽ����ȷ";
                    return -1;
                }

                PriceItem item = new PriceItem();
                item.Prefix = strPrefix;
                item.Postfix = strPostfix;
                item.Value = value;

                items.Add(item);
            }

            // ����
            for (int i = 0; i < items.Count; i++)
            {
                PriceItem item = items[i];

                for (int j = i + 1; j < items.Count; j++)
                {
                    PriceItem current_item = items[j];
                    if (current_item.Prefix == item.Prefix
                        && current_item.Postfix == item.Postfix)
                    {
                        item.Value += current_item.Value;
                        items.RemoveAt(j);
                        j--;
                    }
                    else
                        break;
                }
            }

            // ���
            for (int i = 0; i < items.Count; i++)
            {
                PriceItem item = items[i];

                results.Add(item.Prefix + item.Value.ToString() + item.Postfix);
            }

            return 0;
        }

        class PriceItem
        {
            public string Prefix = "";
            public string Postfix = "";
            public decimal Value = 0;
        }
#if NOOOOOOOOOOOOOOO
        // ����۸�˻�
        static int MultiPrice(string strPrice,
            int nCopy,
            out string strResult,
            out string strError)
        {
            strError = "";
            strResult = "";

            string strPrefix = "";
            string strValue = "";
            string strPostfix = "";
            int nRet = PriceUtil.ParsePriceUnit(strPrice,
                out strPrefix,
                out strValue,
                out strPostfix,
                out strError);
            if (nRet == -1)
                return -1;

            decimal value = 0;
            try
            {
                value = Convert.ToDecimal(strValue);
            }
            catch
            {
                strError = "���� '" + strValue + "' ��ʽ����ȷ";
                return -1;
            }

            value *= (decimal)nCopy;

            strResult = strPrefix + value.ToString() + strPostfix;
            return 0;
        }
#endif

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
            column.No = ORIGIN_COLUMN_ACCEPTPRICE;  // 2009/11/23 changed
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
                MessageBox.Show(this, "��δѡ��Ҫ�Ƴ�������");
                return;
            }

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in list.SelectedItems)
            {
                items.Add(item);
            }

            DialogResult result = MessageBox.Show(this,
                "ȷʵҪ�ںϲ����б������Ƴ�ѡ���� " + items.Count.ToString() + " ������?",
                "PrintAcceptForm",
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

        // ԭʼ���� ��ӡѡ��
        private void button_print_originOption_Click(object sender, EventArgs e)
        {
            // ���ñ���ͷ��
            string strNamePath = "printaccept_origin_printoption";

            PrintOption option = new AcceptOriginPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);

            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.DataDir = this.MainForm.DataDir;
            dlg.Text = this.comboBox_load_type.Text + " ԭʼ���� ��ӡ����";
            dlg.PrintOption = option;
            dlg.ColumnItems = new string[] {
                "no -- ���",
                "recpath -- ���¼·��",
                "summary -- ժҪ",
                "state -- ״̬",
                "isbnIssn -- ISBN/ISSN",
                "publishTime -- ����ʱ��",
                "volume -- ����",
                "location -- �ݲصص�",
                "seller -- ����",
                "source -- ������Դ",

                "acceptPrice -- ���յ���",    // ���յ���
                "itemPrice -- ʵ�嵥��",
                "orderPrice -- ��������",

                "comment -- ע��",
                "batchNo -- �������κ�",
                "refID -- �ο�ID",
                "biblioRecpath -- �ּ�¼·��",

                "catalogNo -- ��Ŀ��",
                "orderID -- ������",
                "orderClass -- �������",
                "orderTime -- ����ʱ��",
                "orderPrice -- ������",
                "acceptPrice -- �����",

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

                "orderRecpath -- ������¼·��"

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

                printform.Text = "��ӡԭʼ��������(����Ϣ)";
                printform.MainForm = this.MainForm;
                printform.Filenames = filenames;

                this.MainForm.AppInfo.LinkFormState(printform, "printaccept_htmlprint_formstate");
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
        // ��ӡԭʼ����
        int BuildOriginHtml(
            List<ListViewItem> items,
            out List<string> filenames,
            out string strError)
        {
            strError = "";

            Hashtable macro_table = new Hashtable();

            // ��ô�ӡ����
            PrintOption option = new AcceptOriginPrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                "printaccept_origin_printoption");

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

            // 2009/7/30 changed
            if (this.SourceStyle == "batchno")
            {
                macro_table["%batchno%"] = HttpUtility.HtmlEncode(this.BatchNo); // ���κ�
            }
            else
            {
                Debug.Assert(this.SourceStyle == "barcodefile"
                    || this.SourceStyle == "recpathfile"
                    || this.SourceStyle == "",
                    "");

                macro_table["%batchno%"] = "";
            }

            macro_table["%pagecount%"] = nPageCount.ToString();
            macro_table["%linesperpage%"] = option.LinesPerPage.ToString();
            macro_table["%date%"] = DateTime.Now.ToLongDateString();

            // 2009/7/30 changed
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

            // ���κŻ��ļ��� ����겻�����жϺ�ѡ�ã�ֻ�úϲ�Ϊһ����
            macro_table["%batchno_or_recpathfilename%"] = String.IsNullOrEmpty(this.BatchNo) == true ? Path.GetFileName(this.RecPathFilePath) : this.BatchNo;
            macro_table["%sourcedescription%"] = this.SourceDescription;

            filenames = new List<string>();    // ÿҳһ���ļ�������������������ļ���

            string strFileNamePrefix = this.MainForm.DataDir + "\\~printaccept";

            string strFileName = "";

            // �����Ϣҳ
            {
                int nItemCount = items.Count;
                int nTotalCopies = GetOriginTotalCopies(items);
                int nTotalSeries = GetOriginTotalSeries(items);
                int nBiblioCount = GetOriginBiblioCount(items);
                string strTotalPrice = GetOriginTotalPrice(items);

                macro_table["%itemcount%"] = nItemCount.ToString(); // ������
                macro_table["%totalcopies%"] = nTotalCopies.ToString(); // �ܲ���
                macro_table["%totalseries%"] = nTotalSeries.ToString(); // ������
                macro_table["%bibliocount%"] = nBiblioCount.ToString(); // ����
                macro_table["%totalprice%"] = strTotalPrice;    // �ܼ۸�

                macro_table["%pageno%"] = "1";

                // 2009/7/30 new add
                macro_table["%datadir%"] = this.MainForm.DataDir;   // ��������datadir��templatesĿ¼�ڵ�ĳЩ�ļ�
                ////macro_table["%libraryserverdir%"] = this.MainForm.LibraryServerDir;  // �������÷������˵�CSS�ļ�
                // 2009/10/10 new add
                macro_table["%cssfilepath%"] = this.GetAutoCssUrl(option, "acceptorigin.css");  // �������÷������˻�css��ģ���CSS�ļ�

                strFileName = strFileNamePrefix + "0" + ".html";

                filenames.Add(strFileName);

                string strTemplateFilePath = option.GetTemplatePageFilePath("ͳ��ҳ");
                if (String.IsNullOrEmpty(strTemplateFilePath) == false)
                {
                    /*
<html>
<head>
    ���÷�<LINK href='%libraryserverdir%/printorigin.css' type='text/css' rel='stylesheet'>
	���÷�<LINK href='%cssfilepath%' type='text/css' rel='stylesheet'>
</head>
<body>
    <div class='pageheader'>%date% ԭʼ�������� - ��Դ: %sourcedescription% - (�� %pagecount% ҳ)</div>
    <div class='tabletitle'>%date% ԭʼ��������</div>
    <div class='copies'>����: %totalcopies%</div>
    <div class='bibliocount'>����: %bibliocount%</div>
    <div class='totalprice'>�ܼ�: %totalprice%</div>
    <div class='sepline'><hr/></div>
    <div class='batchno'>���κ�: %batchno%</div>
    <div class='location'>��¼·���ļ�: %recpathfilepath%</div>
    <div class='sepline'><hr/></div>
    <div class='pagefooter'>%pageno%/%pagecount%</div>
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


                    BuildOriginPageTop(option,
                        macro_table,
                        strFileName,
                        false);

                    // ������

                    StreamUtil.WriteText(strFileName,
                        "<div class='itemcount'>������: " + nItemCount.ToString() + "</div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='bibliocount'>����: " + nBiblioCount.ToString() + "</div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='series'>����: " + nTotalSeries.ToString() + "</div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='copies'>����: " + nTotalCopies.ToString() + "</div>");
                    StreamUtil.WriteText(strFileName,
                        "<div class='totalprice'>�ܼ�: " + strTotalPrice + "</div>");

                    BuildOriginPageBottom(option,
                        macro_table,
                        strFileName,
                        false);
                }

            }

            string strMarcFilterFilePath = option.GetTemplatePageFilePath("MARC������");
            if (String.IsNullOrEmpty(strMarcFilterFilePath) == false)
            {
                int nRet = PrepareMarcFilter(strMarcFilterFilePath, out strError);
                if (nRet == -1)
                    return -1;
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
            */

            // string strCssUrl = this.MainForm.LibraryServerDir + "/acceptorigin.css";
            // 2009/10/10 changed
            string strCssUrl = GetAutoCssUrl(option, "acceptorigin.css");

            /*
            // 2009/10/9 new add
            string strCssFilePath = option.GetTemplatePageFilePath("css");  // ��Сд������
            if (String.IsNullOrEmpty(strCssFilePath) == false)
                strCssUrl = strCssFilePath;
            else
                strCssUrl = this.MainForm.LibraryServerDir + "/acceptorigin.css";    // ȱʡ��
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

                    string strClass = PrintOrderForm.GetClass(column.Name);

                    StreamUtil.WriteText(strFileName,
                        "<td class='" + strClass + "'>" + strCaption + "</td>");
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

                string strClass = PrintOrderForm.GetClass(column.Name);

                strLineContent +=
                    "<td class='" + strClass + "'>" + strContent + "</td>";
            }

            END1:

            StreamUtil.WriteText(strFileName,
    "<tr class='content'>");

            StreamUtil.WriteText(strFileName,
    strLineContent);

            StreamUtil.WriteText(strFileName,
                "</tr>");

            return 0;
        }

        // ���ԭʼ���� ��Ŀ����
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
            PrintOrderForm.ParseNameParam(StringUtil.GetLeft(strColumnName),
                out strName,
                out strParameters);

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
                    case "���¼·��":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_RECPATH);

                    case "errorInfo":
                    case "summary":
                    case "ժҪ":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SUMMARY);

                    case "state":
                    case "״̬":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_STATE);

                    case "isbnIssn":
                    case "ISBN/ISSN":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ISBNISSN);

                    case "publishTime":
                    case "����ʱ��":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_PUBLISHTIME);

                    case "volume":
                    case "����":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_VOLUME);

                    case "location":
                    case "�ݲصص�":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_LOCATION);

                    case "seller":
                    case "����":
                    case "����":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER);

                    case "source":
                    case "������Դ":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SOURCE);

                        // ���ռ�
                    case "price":
                    case "acceptprice":
                    case "acceptPrice":
                    case "����":
                    case "���յ���":
                    case "�����":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ACCEPTPRICE);

                        // ʵ���¼�еļ۸�
                    case "itemprice":
                    case "itemPrice":
                    case "ʵ�嵥��":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ITEMPRICE);

                        // ������
                    case "orderprice":
                    case "orderPrice":
                    case "��������":
                    case "������":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ORDERPRICE);


                    case "comment":
                    case "ע��":
                    case "��ע":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_COMMENT);

                    // �������κ�
                    case "batchNo":
                    case "���κ�":
                    case "�������κ�":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_BATCHNO);

                    case "refID":
                    case "�ο�ID":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_REFID);


                    case "biblioRecpath":
                    case "�ּ�¼·��":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_BIBLIORECPATH);

                    case "catalogNo":
                    case "��Ŀ��":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_CATALOGNO);

                    case "orderId":
                    case "orderID":
                    case "������":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ORDERID);

                    case "class":
                    case "orderClass":
                    case "�������":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ORDERCLASS);

                    case "orderTime":
                    case "����ʱ��":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ORDERTIME);

#if NO
                    case "orderPrice":
                    case "������":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ORDERPRICE);

                    case "acceptPrice":
                    case "�����":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ACCEPTPRICE);
#endif

                    case "orderRecpath":
                    case "������¼·��":
                        return ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ORDERRECPATH);

                    // ��ʽ���Ժ��������ַ
                    case "sellerAddress":
                    case "������ַ":
                        return PrintOrderForm.GetPrintableSellerAddress(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            strParameters);

                    case "sellerAddress:zipcode":
                    case "������ַ:��������":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "zipcode");

                    case "sellerAddress:address":
                    case "������ַ:��ַ":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "address");

                    case "sellerAddress:department":
                    case "������ַ:��λ":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "department");

                    case "sellerAddress:name":
                    case "������ַ:��ϵ��":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "name");

                    case "sellerAddress:email":
                    case "������ַ:Email��ַ":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "email");

                    case "sellerAddress:bank":
                    case "������ַ:������":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "bank");

                    case "sellerAddress:accounts":
                    case "������ַ:�����˺�":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "accounts");

                    case "sellerAddress:payStyle":
                    case "������ַ:��ʽ":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "payStyle");

                    case "sellerAddress:comment":
                    case "������ַ:��ע":
                        return PrintOrderForm.GetSellerAddressInnerValue(ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLERADDRESS),
                            ListViewUtil.GetItemText(item, ORIGIN_COLUMN_SELLER),
                            "comment");
                    default:
                        {
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
        "<div class='pagefooter'>" + strPageFooterText + "</div>");
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
                    strText = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_BIBLIORECPATH);
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

        // ���ԭʼ�б��е�������
        static int GetOriginTotalSeries(List<ListViewItem> items)
        {
            int total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strSubCopyDetail = ListViewUtil.GetItemText(item,
ORIGIN_COLUMN_ACCEPTSUBCOPY);
                if (String.IsNullOrEmpty(strSubCopyDetail) == true)
                {
                    total++;
                    continue;
                }

                string strNo = "";
                string strIndex = "";
                string strCopy = "";
                string strError = "";
                int nRet = ParseSubCopy(strSubCopyDetail,
        out strNo,
        out strIndex,
        out strCopy,
        out strError);
                if (nRet == -1)
                    continue;

                // ���Ǹ��׵ĵ�һ�ᣬ������
                if (strIndex == "1")
                    total ++;
            }

            return total;
        }

        // ���ԭʼ�б��е��ܲ���
        static int GetOriginTotalCopies(List<ListViewItem> items)
        {
            return items.Count; // 2009/7/30 changed
            /*
            int total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];


                total++;
            }

            return total;
             * */
        }

        static string GetOriginTotalPrice(List<ListViewItem> items)
        {
            List<String> prices = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strPrice = "";

                try
                {
                    // �õ���۽��м���
                    strPrice = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ACCEPTPRICE);
                }
                catch
                {
                    continue;
                }

                if (String.IsNullOrEmpty(strPrice) == true)
                    continue;

                string strSubCopyDetail = ListViewUtil.GetItemText(item,
ORIGIN_COLUMN_ACCEPTSUBCOPY);
                if (String.IsNullOrEmpty(strSubCopyDetail) == true)
                {
                    prices.Add(strPrice);
                    continue;
                }

                string strNo = "";
                string strIndex = "";
                string strCopy = "";
                string strError = "";
                int nRet = ParseSubCopy(strSubCopyDetail,
        out strNo,
        out strIndex,
        out strCopy,
        out strError);
                if (nRet == -1)
                    continue;

                // ���Ǹ��׵ĵ�һ�ᣬ������
                if (strIndex == "1")
                    prices.Add(strPrice);

            }

            return PriceUtil.TotalPrice(prices);
        }

        private void button_saveChange_saveChange_Click(object sender, EventArgs e)
        {
            // ��֯�������� SetOrders
            string strError = "";
            int nRet = SaveOrders(out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
            else
            {
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
                        strError = "ԭʼ�����б��У��� " + (i + 1).ToString() + " ������Ϊ����״̬����Ҫ���ų�������ܽ��б��档";
                        return -1;
                    }

                    OriginAcceptItemData data = (OriginAcceptItemData)item.Tag;
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
                        "state", ListViewUtil.GetItemText(item, ORIGIN_COLUMN_STATE));

                    EntityInfo info = new EntityInfo();

                    if (String.IsNullOrEmpty(data.RefID) == true)
                    {
                        data.RefID = Guid.NewGuid().ToString();
                    }

                    info.RefID = data.RefID;
                    info.Action = "change";
                    info.OldRecPath = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_RECPATH);
                    info.NewRecPath = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_RECPATH); ;

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
                OriginAcceptItemData data = FindDataByRefID(errorinfo.RefID, out item);
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
        OriginAcceptItemData FindDataByRefID(string strRefID,
            out ListViewItem item)
        {
            item = null;
            for (int i = 0; i < this.listView_origin.Items.Count; i++)
            {
                item = this.listView_origin.Items[i];
                OriginAcceptItemData data = (OriginAcceptItemData)item.Tag;
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
                    OriginAcceptItemData data = (OriginAcceptItemData)this.listView_origin.Items[i].Tag;
                    if (data.Changed == true)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// ��ǰ����������
        /// </summary>
        public string PublicationType
        {
            get
            {
                return this.comboBox_load_type.Text;
            }
            set
            {
                if (value != "ͼ��" && value != "����������")
                {
                    throw new Exception("���Ϸ���PublicationTypeֵ '" + value + "'������Ϊ 'ͼ��' �� '����������'");
                }

                this.comboBox_load_type.Text = value;
            }
        }

        private void PrintAcceptForm_Activated(object sender, EventArgs e)
        {
            // 2009/8/13 new add
            this.MainForm.stopManager.Active(this.stop);
        }

        private void comboBox_load_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*
            if (this.comboBox_load_type.Text == "ͼ��")
            {
                this.button_load_loadFromOrderBatchNo.Enabled = true;
            }
            else
            {
                this.button_load_loadFromOrderBatchNo.Enabled = false;
            }
             * */
        }

        private void button_print_exchangeRateStatis_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = PrintExchangeRate("html", out strError);
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

        // ��ӡ����ͳ�Ʊ�
        // parameters:
        //      strStyle    excel / html ֮һ���߶���������ϡ� excel: ��� Excel �ļ�
        int PrintExchangeRate(
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
                    return -1;
                }

                doc.Stylesheet = GenerateStyleSheet();
                // doc.Initial();
            }


            this.tabControl_items.SelectedTab = this.tabPage_originItems;

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڴ������ʱ� ...");
            stop.BeginLoop();

            try
            {

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

                PrintExchangeRateList(items,
                    ref doc);
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            if (doc != null)
            {
                // Close the document.
                doc.Close();
            }

            return 1;
        }

        void PrintExchangeRateList(List<ListViewItem> items,
            ref ExcelDocument doc)
        {
            string strError = "";

            // ����һ��html�ļ�������ʾ��HtmlPrintForm�С�
            List<string> filenames = null;
            try
            {
                // Debug.Assert(false, "");

                // ����htmlҳ��
                int nRet = BuildExchangeRateHtml(
                    items,
                    ref doc,
                    out filenames,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                    goto ERROR1;

                if (doc == null)
                {
                    HtmlPrintForm printform = new HtmlPrintForm();

                    printform.Text = "��ӡ����ͳ�Ʊ�";
                    printform.MainForm = this.MainForm;
                    printform.Filenames = filenames;

                    this.MainForm.AppInfo.LinkFormState(printform, "printaccept_htmlprint_formstate");
                    printform.ShowDialog(this);
                    this.MainForm.AppInfo.UnlinkFormState(printform);
                }
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

        Hashtable m_exchangeTable = new Hashtable();

        // �ۼ�һ������ַ���
        int AddCurrency(string strCurrentcyString1,
            int nSubCopy1,
            string strCurrentcyString2,
            int nSubCopy2,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(strCurrentcyString1) == true
                || string.IsNullOrEmpty(strCurrentcyString2) == true)
                return 0;

            string strPrefix1 = "";
            string strValue1 = "";
            string strPostfix1 = "";
            int nRet = PriceUtil.ParsePriceUnit(strCurrentcyString1,
                out strPrefix1,
                out strValue1,
                out strPostfix1,
                out strError);
            if (nRet == -1)
                return -1;

            double value1 = 0;
            try
            {
                value1 = Convert.ToDouble(strValue1);
            }
            catch
            {
                strError = "���� '" + strValue1 + "' ��ʽ����ȷ";
                return -1;
            }

            if (value1 == 0)
                return 0;

            value1 = value1 / (double)nSubCopy1;

            //
            string strPrefix2 = "";
            string strValue2 = "";
            string strPostfix2 = "";
            nRet = PriceUtil.ParsePriceUnit(strCurrentcyString2,
                out strPrefix2,
                out strValue2,
                out strPostfix2,
                out strError);
            if (nRet == -1)
                return -1;

            double value2 = 0;
            try
            {
                value2 = Convert.ToDouble(strValue2);
            }
            catch
            {
                strError = "���� '" + strValue2 + "' ��ʽ����ȷ";
                return -1;
            }

            if (value2 == 0)
                return 0;

            value2 = value2 / (double)nSubCopy2;

            if (strPrefix1 == strPrefix2
                && strPostfix1 == strPostfix2)
                return 0;   // Դ��Ŀ����ͬ�ı��ֲ������ۼ�

            string strKey = strPrefix1 + "|" + strPostfix1 + "-->" + strPrefix2 + "|" + strPostfix2;
            ExchangeInfo info = (ExchangeInfo)this.m_exchangeTable[strKey];
            if (info == null)
            {
                info = new ExchangeInfo();
                this.m_exchangeTable[strKey] = info;
                info.OriginCurrency = strPrefix1 + " " + strPostfix1;
                info.TargetCurrency = strPrefix2 + " " + strPostfix2;
            }

            info.OrginValue += value1;
            info.TargetValue += value2;

            return 1;
        }

        // ��� Excel ҳ��ͷ����Ϣ
        int BuildExchangeRateExcelPageTop(PrintOption option,
            Hashtable macro_table,
            ref ExcelDocument doc,
            int nTitleCols)
        {
            // ������
            string strTableTitleText = "%date% ���ʱ�";

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

        // ����htmlҳ��
        // ��ӡ����ͳ�Ʊ�
        // return:
        //      -1  ����
        //      0   û������
        //      1   �ɹ�
        int BuildExchangeRateHtml(
            List<ListViewItem> items,
            ref ExcelDocument doc,
            out List<string> filenames,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            filenames = new List<string>();    // ÿҳһ���ļ�������������������ļ���

            // ��ô�ӡ����
            string strNamePath = "printaccept_exchangerate_printoption";
            ExchangeRatePrintOption option = new ExchangeRatePrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);

            this.m_exchangeTable.Clear();

            Hashtable macro_table = new Hashtable();

            // 2009/7/30 changed
            if (this.SourceStyle == "batchno")
            {
                macro_table["%batchno%"] = HttpUtility.HtmlEncode(this.BatchNo); // ���κ�
            }
            else
            {
                Debug.Assert(this.SourceStyle == "barcodefile"
                    || this.SourceStyle == "recpathfile"
                    || this.SourceStyle == "",
                    "");

                macro_table["%batchno%"] = "";
            }

            macro_table["%date%"] = DateTime.Now.ToLongDateString();

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

            // ���κŻ��ļ��� ����겻�����жϺ�ѡ�ã�ֻ�úϲ�Ϊһ����
            macro_table["%batchno_or_recpathfilename%"] = String.IsNullOrEmpty(this.BatchNo) == true ? Path.GetFileName(this.RecPathFilePath) : this.BatchNo;
            macro_table["%sourcedescription%"] = this.SourceDescription;

            string strCssUrl = GetAutoCssUrl(option, "exchangeratetable.css");
            string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";
            macro_table["%link%"] = strLink;

            string strResult = "";

            // ׼��ģ��ҳ
            string strStatisTemplateFilePath = "";

            strStatisTemplateFilePath = option.GetTemplatePageFilePath("���ʱ�");

            if (String.IsNullOrEmpty(strStatisTemplateFilePath) == true)
            {
                strStatisTemplateFilePath = PathUtil.MergePath(this.MainForm.DataDir, "default_printaccept_exchangeratetable.template");
            }

            Debug.Assert(String.IsNullOrEmpty(strStatisTemplateFilePath) == false, "");

            if (File.Exists(strStatisTemplateFilePath) == false)
            {
                strError = "����ͳ��ģ���ļ� '" + strStatisTemplateFilePath + "' �����ڣ��������ʱ�ʧ��";
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


            stop.SetProgressValue(0);
            stop.SetProgressRange(0, items.Count);

            stop.SetMessage("���ڱ���ԭʼ������ ...");
            for (int i = 0; i < items.Count; i++)
            {
                Application.DoEvents();	// ���ý������Ȩ

                if (stop != null && stop.State != 0)
                {
                    strError = "�û��ж�";
                    return -1;
                }

                ListViewItem item = items[i];

                string strOrderRecPath = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ORDERRECPATH);
                if (string.IsNullOrEmpty(strOrderRecPath) == true)
                    continue;

                string strOrderPrice = ListViewUtil.GetItemText(item,ORIGIN_COLUMN_ORDERPRICE);
                string strAcceptPrice = ListViewUtil.GetItemText(item,ORIGIN_COLUMN_ACCEPTPRICE);

                string strOrderSubCopy = ListViewUtil.GetItemText(item,ORIGIN_COLUMN_ORDERSUBCOPY);
                string strAcceptSubCopy = ListViewUtil.GetItemText(item, ORIGIN_COLUMN_ACCEPTSUBCOPY);

                // ACCEPTSUBCOPY��Ҫ�ӹ�һ�¡�ԭʼ��̬Ϊ 1:1/3 ��Ҫȡ��'/'�ұߵ�����
                {
                    strAcceptSubCopy = GetRightFromAccptSubCopyString(strAcceptSubCopy);
                    if (string.IsNullOrEmpty(strAcceptSubCopy) == true)
                        strAcceptSubCopy = "1";
                }

                int nOrderSubCopy = 1;
                if (Int32.TryParse(strOrderSubCopy, out nOrderSubCopy) == false)
                {
                    strError = "��¼ " + strOrderRecPath + " �ж���ʱ���ڲ����ַ��� '" + strOrderSubCopy + "' ��ʽ����";
                    return -1;
                }

                int nAcceptSubCopy = 1;
                if (Int32.TryParse(strAcceptSubCopy, out nAcceptSubCopy) == false)
                {
                    strError = "��¼ " + strOrderRecPath + " ������ʱ���ڲ����ַ��� '" + strAcceptSubCopy + "' ��ʽ����";
                    return -1;
                }

                // 2014/2/18
                nOrderSubCopy = nAcceptSubCopy;

                // �ۼ�һ������ַ���
                nRet = AddCurrency(strOrderPrice,
                    nOrderSubCopy,
                    strAcceptPrice,
                    nAcceptSubCopy,
                    out strError);
                if (nRet == -1)
                {
                    return -1;
                }
            }

            if (this.m_exchangeTable.Count == 0)
            {
                strError = "û�пɴ�ӡ������";
                return 0;
            }

            string strFileName = this.MainForm.DataDir + "\\~printaccept_exchangerate";
            filenames.Add(strFileName);

            Sheet sheet = null;
            if (doc != null)
                sheet = doc.NewSheet("���ʱ�");

            int nLineIndex = 2;
            if (doc != null)
            {
                BuildExchangeRateExcelPageTop(option,
    macro_table,
    ref doc,
    4);

                {
                    List<string> captions = new List<string>();
                    captions.Add("���չ�ϵ");
                    captions.Add("Դ���");
                    captions.Add("Ŀ����");
                    captions.Add("����");
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


            string strTableContent = "<table class='exchangerate'>";

            // ��Ŀ������
            {
                strTableContent += "<tr class='column'>";
                strTableContent += "<td class='relation'>���չ�ϵ</td>";
                strTableContent += "<td class='origincurrency'>Դ���</td>";
                strTableContent += "<td class='targetcurrency'>Ŀ����</td>";
                strTableContent += "<td class='exchangerate'>����</td>";
            }

            // ��������?
            string [] keys = new string[this.m_exchangeTable.Keys.Count];
            this.m_exchangeTable.Keys.CopyTo(keys, 0);
            Array.Sort(keys);


            stop.SetMessage("�������ͳ��ҳ HTML ...");
            foreach (string key in keys)
            {
                ExchangeInfo info = (ExchangeInfo)this.m_exchangeTable[key];
                strTableContent += "<tr class='content' >";
                strTableContent += "<td class='relation'>" + HttpUtility.HtmlEncode(info.OriginCurrency + " / " + info.TargetCurrency) + "</td>";
                strTableContent += "<td class='origincurrency'>" + HttpUtility.HtmlEncode(info.OrginValue.ToString()) + "</td>";
                strTableContent += "<td class='targetcurrency'>" + HttpUtility.HtmlEncode(info.TargetValue.ToString()) + "</td>";
                strTableContent += "<td class='exchangerate'>" +
                    (info.TargetValue / info.OrginValue).ToString() + "</td>";
                strTableContent += "</tr>";

                if (doc != null)
                {
                    nLineIndex++;
                    int i = 0;
                    doc.WriteExcelCell(
            nLineIndex,
            i++,
            info.OriginCurrency + " / " + info.TargetCurrency,
            true);
                    doc.WriteExcelCell(
nLineIndex,
i++,
info.OrginValue.ToString(),
false);
                    doc.WriteExcelCell(
nLineIndex,
i++,
info.TargetValue.ToString(),
false);
                    doc.WriteExcelCell(
nLineIndex,
i++,
(info.TargetValue / info.OrginValue).ToString(),
false);
                }

            }

            strTableContent += "</table>";

            strResult = strResult.Replace("{table}", strTableContent);

            // ������
            StreamUtil.WriteText(strFileName,
                strResult);

            return 1;
        }

        // �����������ַ����еõ����ڲ�������
        // Ҳ���� "1:1/3"����"3"���֡����û���ҵ�'/'���ͷ���""
        static string GetRightFromAccptSubCopyString(string strText)
        {
            int nRet = strText.IndexOf("/");
            if (nRet == -1)
                return "";

            return strText.Substring(nRet + 1).Trim();
        }

        private void listView_origin_DoubleClick(object sender, EventArgs e)
        {
            LoadItemToEntityForm(this.listView_origin);
        }

        // ���ʱ� ��ӡѡ��
        private void button_print_exchangeRateOption_Click(object sender, EventArgs e)
        {
            // ���ñ���ͷ��
            string strNamePath = "printaccept_exchangerate_printoption";

            PrintOption option = new ExchangeRatePrintOption(this.MainForm.DataDir,
                this.comboBox_load_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);

            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.DataDir = this.MainForm.DataDir;
            dlg.Text = this.comboBox_load_type.Text + " ���ʱ� ��ӡ����";
            dlg.PrintOption = option;
            dlg.ColumnItems = new string[] {
            };

            this.MainForm.AppInfo.LinkFormState(dlg, "order_exchangerate_printoption_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            option.SaveData(this.MainForm.AppInfo,
                strNamePath);
        }

        private void toolStripMenuItem_printExchangeRate_outputExcel_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = PrintExchangeRate("excel", out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

    }

    // �ϲ������ݴ�ӡ �������ض�ȱʡֵ��PrintOption������
    internal class PrintAcceptPrintOption : PrintOption
    {
        string PublicationType = "ͼ��"; // ͼ�� ����������

        public PrintAcceptPrintOption(string strDataDir,
            string strPublicationType)
        {
            this.DataDir = strDataDir;
            this.PublicationType = strPublicationType;

            this.PageHeaderDefault = "%date% %seller% ���յ� - ��Դ: %sourcedescription% - (�� %pagecount% ҳ)";
            this.PageFooterDefault = "%pageno%/%pagecount%";

            this.TableTitleDefault = "%date% %seller% ���յ�";

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

            if (this.PublicationType == "����������")
            {
                // "publishTime -- ����ʱ��"
                column = new Column();
                column.Name = "publishTime -- ����ʱ��";
                column.Caption = "����ʱ��";
                column.MaxChars = -1;
                this.Columns.Add(column);

                // "volume -- ����"
                column = new Column();
                column.Name = "volume -- ����";
                column.Caption = "����";
                column.MaxChars = -1;
                this.Columns.Add(column);
            }

            // "price -- ����"
            // "acceptPrice -- ���յ���"
            column = new Column();
            column.Name = "acceptPrice -- ���յ���";
            column.Caption = "���յ���";
            column.MaxChars = -1;
            this.Columns.Add(column);

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

    // ԭʼ���ݴ�ӡ �������ض�ȱʡֵ��PrintOption������
    internal class AcceptOriginPrintOption : PrintOption
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

        public AcceptOriginPrintOption(string strDataDir,
            string strPublicationType)
        {
            this.DataDir = strDataDir;
            this.PublicationType = strPublicationType;

            this.PageHeaderDefault = "%date% ԭʼ�������� - ��Դ: %sourcedescription% - (�� %pagecount% ҳ)";
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

            if (this.PublicationType == "����������")
            {
                // "publishTime -- ����ʱ��"
                column = new Column();
                column.Name = "publishTime -- ����ʱ��";
                column.Caption = "����ʱ��";
                column.MaxChars = -1;
                this.Columns.Add(column);

                // "volume -- ����"
                column = new Column();
                column.Name = "volume -- ����";
                column.Caption = "����";
                column.MaxChars = -1;
                this.Columns.Add(column);
            }

            // "price -- ����"
            // "acceptPrice -- ���յ���"
            column = new Column();
            column.Name = "acceptPrice -- ���յ���";
            column.Caption = "���յ���";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // ԭʼ���ݵ�ÿ������һ�ᣬ����û�и������ܼ��ֶ�
            /*
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
             * */

            // "orderClass -- ���"
            column = new Column();
            column.Name = "orderClass -- ���";
            column.Caption = "���";
            column.MaxChars = -1;
            this.Columns.Add(column);

        }
    }

    // ԭʼ����listviewitem��Tag��Я�������ݽṹ
    /*public*/ class OriginAcceptItemData
    {
        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed = false;
        public byte[] Timestamp = null;
        public string Xml = ""; // ������¼��XML��¼��
        public string RefID = "";   // �����¼ʱ���õ�refid
    }

    // ���ʱ����ݴ�ӡ �������ض�ȱʡֵ��PrintOption������
    internal class ExchangeRatePrintOption : PrintOption
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

        public ExchangeRatePrintOption(string strDataDir,
            string strPublicationType)
        {
            this.DataDir = strDataDir;
            this.PublicationType = strPublicationType;

            this.PageHeaderDefault = "";
            this.PageFooterDefault = "";

            this.TableTitleDefault = "";
            // this.TableTitleDefault = "%date% ����ͳ�Ʊ�";

            this.LinesPerPageDefault = 0;

            // Columnsȱʡֵ
            Columns.Clear();
        }
    }


    /*public*/ class ExchangeInfo
    {
        // Դ����
        public string OriginCurrency = "";
        public double OrginValue = 0;

        public string TargetCurrency = "";
        public double TargetValue = 0;
    }
}