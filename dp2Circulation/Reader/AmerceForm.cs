using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Threading;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// ���Ѵ�
    /// </summary>
    public partial class AmerceForm : MyForm
    {
        CommentViewerForm m_operlogViewer = null;
        const string NOTSUPPORT = "<html><body><p>[�ݲ�֧��]</p></body></html>";

        /*
        bool m_bStopFilling = true;
        internal Thread threadFillSummary = null;
         * */

        bool m_bStopFillAmercing = true;
        internal Thread threadFillAmercing = null;
        FillAmercingParam FillAmercingParam = null;

        bool m_bStopFillAmerced = true;
        internal Thread threadFillAmerced = null;
        FillAmercedParam FillAmercedParam = null;

        // ͼ���±�
        const int ITEMTYPE_AMERCED = 0;
        const int ITEMTYPE_NEWLY_SETTLEMENTED = 1;
        const int ITEMTYPE_OLD_SETTLEMENTED = 2;
        const int ITEMTYPE_UNKNOWN = 3;
        const int ITEMTYPE_ERROR = 3;

        int m_nChannelInUse = 0;

        Commander commander = null;

        const int WM_LOADSIZE = API.WM_USER + 201;

        const int WM_LOAD = API.WM_USER + 300;
        const int WM_UNDO_AMERCE = API.WM_USER + 301;
        const int WM_MODIFY_PRICE_AND_COMMENT = API.WM_USER + 302;
        const int WM_AMERCE = API.WM_USER + 303;


        WebExternalHost m_webExternalHost = new WebExternalHost();

        #region �������б���к�
        /// <summary>
        /// �������б���кţ��������
        /// </summary>
        public const int COLUMN_AMERCING_ITEMBARCODE = 0;
        /// <summary>
        /// �������б���к�: ��ĿժҪ
        /// </summary>
        public const int COLUMN_AMERCING_BIBLIOSUMMARY = 1;
        /// <summary>
        /// �������б���к�: ���
        /// </summary>
        public const int COLUMN_AMERCING_PRICE = 2;
        /// <summary>
        /// �������б���к�: ע��
        /// </summary>
        public const int COLUMN_AMERCING_COMMENT = 3;
        /// <summary>
        /// �������б���к�: ����
        /// </summary>
        public const int COLUMN_AMERCING_REASON = 4;
        /// <summary>
        /// �������б���к�: ��ʼʱ��
        /// </summary>
        public const int COLUMN_AMERCING_BORROWDATE = 5;
        /// <summary>
        /// �������б���к�: ����ʱ��
        /// </summary>
        public const int COLUMN_AMERCING_BORROWPERIOD = 6;
        /// <summary>
        /// �������б���к�: ��ʼ������
        /// </summary>
        public const int COLUMN_AMERCING_BORROWOPERATOR = 7;    //
        /// <summary>
        /// �������б���к�: ����ʱ��
        /// </summary>
        public const int COLUMN_AMERCING_RETURNDATE = 8;
        /// <summary>
        /// �������б���к�: ����������
        /// </summary>
        public const int COLUMN_AMERCING_RETURNOPERATOR = 9;    //
        /// <summary>
        /// �������б���к�: ���� ID
        /// </summary>
        public const int COLUMN_AMERCING_ID = 10;

        #endregion

        #region �ѽ����б���к�
        /// <summary>
        /// �ѽ����б���к�: �������
        /// </summary>
        public const int COLUMN_AMERCED_ITEMBARCODE = 0;
        /// <summary>
        /// �ѽ����б���к�: ��ĿժҪ
        /// </summary>
        public const int COLUMN_AMERCED_BIBLIOSUMMARY = 1;
        /// <summary>
        /// �ѽ����б���к�: ���
        /// </summary>
        public const int COLUMN_AMERCED_PRICE = 2;
        /// <summary>
        /// �ѽ����б���к�: ע��
        /// </summary>
        public const int COLUMN_AMERCED_COMMENT = 3;
        /// <summary>
        /// �ѽ����б���к�: ����
        /// </summary>
        public const int COLUMN_AMERCED_REASON = 4;
        /// <summary>
        /// �ѽ����б���к�: ��ʼʱ��
        /// </summary>
        public const int COLUMN_AMERCED_BORROWDATE = 5;
        /// <summary>
        /// �ѽ����б���к�: ����ʱ��
        /// </summary>
        public const int COLUMN_AMERCED_BORROWPERIOD = 6;
        /// <summary>
        /// �ѽ����б���к�: ����ʱ��
        /// </summary>
        public const int COLUMN_AMERCED_RETURNDATE = 7;
        /// <summary>
        /// �ѽ����б���к�: ���� ID
        /// </summary>
        public const int COLUMN_AMERCED_ID = 8;
        /// <summary>
        /// �ѽ����б���к�: ����������
        /// </summary>
        public const int COLUMN_AMERCED_RETURNOPERATOR = 9;
        /// <summary>
        /// �ѽ����б���к�: ״̬
        /// </summary>
        public const int COLUMN_AMERCED_STATE = 10;
        /// <summary>
        /// �ѽ����б���к�: ���Ѳ�����
        /// </summary>
        public const int COLUMN_AMERCED_AMERCEOPERATOR = 11;
        /// <summary>
        /// �ѽ����б���к�: ����ʱ��
        /// </summary>
        public const int COLUMN_AMERCED_AMERCETIME = 12;
        /// <summary>
        /// �ѽ����б���к�: ���������
        /// </summary>
        public const int COLUMN_AMERCED_SETTLEMENTOPERATOR = 13;
        /// <summary>
        /// �ѽ����б���к�: ����ʱ��
        /// </summary>
        public const int COLUMN_AMERCED_SETTLEMENTTIME = 14;
        /// <summary>
        /// �ѽ����б���к�: ���Ѽ�¼·��
        /// </summary>
        public const int COLUMN_AMERCED_RECPATH = 15;

        #endregion

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;
#endif


        // bool m_bChanged = false;

        /// <summary>
        /// ���캯��
        /// </summary>
        public AmerceForm()
        {
            InitializeComponent();

            ListViewProperty prop = new ListViewProperty();
            this.listView_amerced.Tag = prop;
            prop.SetSortStyle(COLUMN_AMERCED_RECPATH, ColumnSortStyle.RecPath);
            prop.SetSortStyle(COLUMN_AMERCED_PRICE, ColumnSortStyle.RightAlign);
            prop.SetSortStyle(COLUMN_AMERCED_BORROWPERIOD, ColumnSortStyle.RightAlign);

            ListViewProperty prop_1 = new ListViewProperty();
            this.listView_overdues.Tag = prop_1;
            prop_1.SetSortStyle(COLUMN_AMERCING_PRICE, ColumnSortStyle.RightAlign);
            prop_1.SetSortStyle(COLUMN_AMERCING_BORROWPERIOD, ColumnSortStyle.RightAlign);

        }

        private void AmerceForm_Load(object sender, EventArgs e)
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

            // webbrowser
            this.m_webExternalHost.Initial(this.MainForm, this.webBrowser_readerInfo);
            this.webBrowser_readerInfo.ObjectForScripting = this.m_webExternalHost;

            this.commander = new Commander(this);
            this.commander.IsBusy -= new IsBusyEventHandler(commander_IsBusy);
            this.commander.IsBusy += new IsBusyEventHandler(commander_IsBusy);

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);

            /*
            this.listView_amerced.SmallImageList = this.imageList_itemType;
            this.listView_amerced.LargeImageList = this.imageList_itemType;
             * */

            this.checkBox_fillSummary.Checked = this.MainForm.AppInfo.GetBoolean(
                "amerce_form",
                "fill_summary",
                true);

            if (this.LayoutMode == "���ҷֲ�")
                this.splitContainer_main.Orientation = Orientation.Vertical; 
        }

        void commander_IsBusy(object sender, IsBusyEventArgs e)
        {
            e.IsBusy = this.m_webExternalHost.ChannelInUse;
        }

        /*public*/ void LoadSize()
        {
#if NO
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state");
#endif

            try
            {
                // ���splitContainer_main��״̬
                this.MainForm.LoadSplitterPos(
    this.splitContainer_main,
    "amerceform_state",
    "splitContainer_main_ratio");

                // ���splitContainer_upper��״̬
                this.MainForm.LoadSplitterPos(
this.splitContainer_lists,
"amerceform_state",
"splitContainer_lists_ratio");

            }
            catch
            {
            }

            string strWidths = this.MainForm.AppInfo.GetString(
                "amerce_form",
                "amerced_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_amerced,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
                "amerce_form",
                "overdues_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_overdues,
                    strWidths,
                    true);
            }

            //this.panel_amerced_command.PerformLayout();
            //this.panel_amercing_command.PerformLayout();

            //this.tableLayoutPanel_amerced.PerformLayout();
            //this.tableLayoutPanel_amercingOverdue.PerformLayout();

            // this.PerformLayout();
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

            /*
            // ����splitContainer_main��״̬
            MainForm.AppInfo.SetInt(
                "amerceform_state",
                "splitContainer_main",
                this.splitContainer_main.SplitterDistance);
            // ����splitContainer_upper��״̬
            MainForm.AppInfo.SetInt(
                "amerceform_state",
                "splitContainer_upper",
                this.splitContainer_upper.SplitterDistance);
             * */
            // ����splitContainer_main��״̬
            this.MainForm.SaveSplitterPos(
                this.splitContainer_main,
                "amerceform_state",
                "splitContainer_main_ratio");
            // ����splitContainer_upper��״̬
            this.MainForm.SaveSplitterPos(
                this.splitContainer_lists,
                "amerceform_state",
                "splitContainer_lists_ratio");

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_amerced);
            this.MainForm.AppInfo.SetString(
                "amerce_form",
                "amerced_list_column_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.listView_overdues);
            this.MainForm.AppInfo.SetString(
                "amerce_form",
                "overdues_list_column_width",
                strWidths);
        }

        // 
        /// <summary>
        /// ���Ѵ����ַ�ʽ
        /// </summary>
        public string LayoutMode
        {
            get
            {
                return this.MainForm.AppInfo.GetString("amerce_form",
        "layout",
        "���ҷֲ�");
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
                case WM_LOADSIZE:
                    LoadSize();
                    return;
                case WM_LOAD:
                    if (this.m_webExternalHost.CanCallNew(
                        this.commander,
                        m.Msg) == true)
                    {
                        LoadReader(this.textBox_readerBarcode.Text, true);
                    }
                    return;
                case WM_UNDO_AMERCE:
                    if (this.m_webExternalHost.CanCallNew(
                        this.commander,
                        m.Msg) == true)
                    {
                        UndoAmerce();
                    }
                    return;
                case WM_MODIFY_PRICE_AND_COMMENT:
                    if (this.m_webExternalHost.CanCallNew(
                        this.commander,
                        m.Msg) == true)
                    {
                        ModifyPriceAndComment();
                    }
                    return;
                case WM_AMERCE:
                    if (this.m_webExternalHost.CanCallNew(
                        this.commander,
                        m.Msg) == true)
                    {
                        AmerceSubmit();
                    }
                    return;

            }
            base.DefWndProc(ref m);
        }

        private void AmerceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopFillAmercing(true);

            StopFillAmerced(true);

            /*
            this.m_bStopFilling = true;

            if (this.threadFillSummary != null)
                this.threadFillSummary.Abort();
             * */
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

            string strError = "";
            string strInfo = "";
            // ��õ�ǰ�������Ƿ����޸ĺ�δ���ֵ�״̬(�ǡ���)����Ϣ(������Щ�����޸���Ϣ)
            // return:
            //      -1  ִ�й��̷�������
            //      0   û���޸�
            //      >0  �޸Ĺ����������޸ĵ���������ϸ��Ϣ��strInfo��
            int nRet = GetChangedInfo(
                out strInfo,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, "GetChangedInfo() error : " + strError);
                return;
            }

            if (nRet > 0)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ�������������з������޸ģ�����δ�ύ��������:\r\n---\r\n"
    + strInfo
    + "\r\n---"
    + "\r\n\r\n����: ����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
    "AmerceForm",
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

        private void AmerceForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.MainForm.AppInfo.SetBoolean(
    "amerce_form",
    "fill_summary",
    this.checkBox_fillSummary.Checked);

            this.commander.Destroy();

            if (this.m_webExternalHost != null)
                this.m_webExternalHost.Destroy();
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            SaveSize();
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

        // �Ӷ��߼�¼ XML �ַ�����ȡ������֤�����
        // return:
        //      -1  ����
        //      0   �ɹ�
        static int GetReaderBarcode(string strReaderXml,
            out string strReaderBarcode,
            out string strError)
        {
            strError = "";
            strReaderBarcode = "";
            if (string.IsNullOrEmpty(strReaderXml) == true)
                return 0;

            XmlDocument reader_dom = new XmlDocument();
            try
            {
                reader_dom.LoadXml(strReaderXml);
            }
            catch (Exception ex)
            {
                strError = "���߼�¼ XML װ�� DOM ʱ����: " + ex.Message;
                return -1;
            }
            strReaderBarcode = DomUtil.GetElementText(reader_dom.DocumentElement, "barcode");
            return 0;
        }

        // װ����߼�¼
        // parameters:
        //      strBarcode  [in][out] ����֤����ţ��������֤�ŵ��������롣���֤�����
        // return:
        //      -1  error
        //      0   not found
        //      >=1 ���еĶ��߼�¼����
        int LoadReaderHtmlRecord(ref string strBarcode,
            out string strXml,
            out string strError)
        {
            strXml = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(strBarcode) == true)
            {
                strError = "strBarcode��������Ϊ��";
                goto ERROR1;
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڳ�ʼ���������� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            int nRecordCount = 0;

            try
            {

                Global.ClearHtmlPage(this.webBrowser_readerInfo, this.MainForm.DataDir);

                byte[] baTimestamp = null;
                string strOutputRecPath = "";
                int nRedoCount = 0;

            REDO:

                stop.SetMessage("����װ����߼�¼ " + strBarcode + " ...");
                string[] results = null;

                long lRet = Channel.GetReaderInfo(
                    stop,
                    strBarcode,
                    "html,xml",
                    out results,
                    out strOutputRecPath,
                    out baTimestamp,
                    out strError);
                if (lRet == 0)
                {
                    // Global.SetHtmlString(this.webBrowser_readerInfo, "֤�����Ϊ '" + strBarcode + "' �Ķ��߼�¼û���ҵ� ...");
                    this.m_webExternalHost.SetTextString("֤�����Ϊ '" + strBarcode + "' �Ķ��߼�¼û���ҵ� ...");
                    
                    return 0;   // not found
                }

                if (lRet == -1)
                    goto ERROR1;

                nRecordCount = (int)lRet;

                if (lRet > 1 && nRedoCount == 0)
                {
                    SelectPatronDialog dlg = new SelectPatronDialog();

                    dlg.Overflow = StringUtil.SplitList(strOutputRecPath).Count < lRet;
                    nRet = dlg.Initial(
                        this.MainForm,
                        this.Channel,
                        this.stop,
                        StringUtil.SplitList(strOutputRecPath),
                        "��ѡ��һ�����߼�¼",
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    // TODO: ���洰���ڵĳߴ�״̬
                    this.MainForm.AppInfo.LinkFormState(dlg, "AmerceForm_SelectPatronDialog_state");
                    dlg.ShowDialog(this);
                    this.MainForm.AppInfo.UnlinkFormState(dlg);

                    if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    {
                        strError = "����ѡ��";
                        return 0;
                    }

                    strBarcode = dlg.SelectedBarcode;
                    nRedoCount++;
                    goto REDO;
                }


                if (results == null || results.Length < 2)
                {
                    strError = "���ص�results��������";
                    goto ERROR1;
                }

                string strHtml = "";
                strHtml = results[0];
                strXml = results[1];

                // 2013/10/14
                string strOutputBarcode = "";
                // �Ӷ��߼�¼ XML �ַ�����ȡ������֤�����
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = GetReaderBarcode(strXml,
                    out strOutputBarcode,
                    out strError);
                if (nRet == -1)
                    return -1;

                strBarcode = strOutputBarcode;

#if NO
                Global.SetHtmlString(this.webBrowser_readerInfo,
                    strHtml,
                    this.MainForm.DataDir,
                    "amercing_reader");
#endif
                this.m_webExternalHost.SetHtmlString(strHtml,
                    "amercing_reader");
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return nRecordCount;
        ERROR1:
            return -1;
        }

        void ClearAllDisplay()
        {
            //this.listView_overdues.Items.Clear();
            //this.listView_amerced.Items.Clear();
            Global.ClearHtmlPage(this.webBrowser_readerInfo,
                this.MainForm.DataDir);
            /*
            this.button_amerced_undoAmerce.Enabled = false;
            this.button_amercingOverdue_submit.Enabled = false;
             * */
            SetAmercedButtonsEnable();
            SetOverduesButtonsEnable();
        }

        void ClearAllDisplay1()
        {
            Global.ClearHtmlPage(this.webBrowser_readerInfo,
                this.MainForm.DataDir);

            SetAmercedButtonsEnable();
            SetOverduesButtonsEnable();
        }

        void ClearHtmlAndAmercingDisplay()
        {
            //this.listView_overdues.Items.Clear();
            Global.ClearHtmlPage(this.webBrowser_readerInfo,
                this.MainForm.DataDir);
            this.toolStripButton_submit.Enabled = false;
        }

        // װ��һ�����ߵ������Ϣ
        // parameters:
        //      bForceLoad  �����������ж�����¼��ʱ���Ƿ�ǿ��װ��?
        // return:
        //      -1  error
        //      0   not found
        //      1   found and loaded
        /// <summary>
        /// װ��һ�����ߵ������Ϣ
        /// </summary>
        /// <param name="strReaderBarcode">����֤����š��������֤�ŵ���������</param>
        /// <param name="bForceLoad">�����������ж�����¼��ʱ���Ƿ�ǿ��װ��?</param>
        /// <returns>
        /// <para>-1: ����</para>
        /// <para>0: û���ҵ�</para>
        /// <para>1: �ҵ������Ѿ�װ��</para>
        /// </returns>
        public int LoadReader(string strReaderBarcode,
            bool bForceLoad)
        {
            string strError = "";

            // this.StopFillSummary();
            StopFillAmercing(false);
            StopFillAmerced(false);


            this.m_nChannelInUse++;
            if (this.m_nChannelInUse > 1)
            {
                this.m_nChannelInUse--;
                MessageBox.Show(this, "ͨ���Ѿ���ռ�á����Ժ�����");
                return -1;
            }
            try
            {

                if (this.textBox_readerBarcode.Text != strReaderBarcode)
                    this.textBox_readerBarcode.Text = strReaderBarcode;

                EnableControls(false);
                try
                {

                    // ��������¼����ʾ�ڴ�����
                    ClearAllDisplay();

                    string strXml = "";

                    // װ����߼�¼
                    // return:
                    //      -1  error
                    //      0   not found
                    //      >=1 ���еĶ��߼�¼����
                    int nRet = LoadReaderHtmlRecord(ref strReaderBarcode,
                        out strXml,
                        out strError);

                    if (this.textBox_readerBarcode.Text != strReaderBarcode)
                        this.textBox_readerBarcode.Text = strReaderBarcode;

                    if (nRet == -1)
                    {
#if NO
                        Global.SetHtmlString(this.webBrowser_readerInfo,
                            "װ�ض��߼�¼��������: " + strError);
#endif
                        this.m_webExternalHost.SetTextString("װ�ض��߼�¼��������: " + strError);
                    }

                    if (nRet == 0)
                        return 0;

                    if (nRet > 1)
                    {
                        if (bForceLoad == true)
                        {
                            strError = "���� " + strReaderBarcode + " ���м�¼ " + nRet.ToString() + " ��������װ�����е�һ�����߼�¼��\r\n\r\n����һ�����ش�����ϵͳ����Ա�����ų���";
                            MessageBox.Show(this, strError);    // ��������װ���һ�� 
                        }
                        else
                        {
                            strError = "���� " + strReaderBarcode + " ���м�¼ " + nRet.ToString() + " ��������װ����߼�¼��\r\n\r\nע������һ�����ش�����ϵͳ����Ա�����ų���";
                            goto ERROR1;    // ��������
                        }
                    }

                    if (String.IsNullOrEmpty(strXml) == false)
                    {
#if NO
                        nRet = FillAmercingList(strXml,
                            out strError);
                        if (nRet == -1)
                        {
                            // strError = "FillAmercingList()��������: " + strError;
                            // goto ERROR1;
                            SetError(this.listView_overdues, strError);
                        }
#endif
                        BeginFillAmercing(strXml);
                    }

#if NO
                    nRet = LoadAmercedRecords(this.textBox_readerBarcode.Text,
                        out strError);
                    if (nRet == -1)
                    {
                        //strError = "LoadAmercedRecords()��������: " + strError;
                        //goto ERROR1;
                        SetError(this.listView_amerced, strError);
                    }
#endif
                    BeginFillAmerced(this.textBox_readerBarcode.Text, null);

#if NO
                    if (this.checkBox_fillSummary.Checked == true)
                        this.BeginFillSummary();
#endif

                }
                finally
                {
                    EnableControls(true);
                }
            }
            finally
            {
                this.m_nChannelInUse--;
            }

            return 1;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }
 

        private void button_load_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.textBox_readerBarcode.Text) == true)
            {
                MessageBox.Show(this, "��δ�������֤�����");
                return;
            }

            this.button_load.Enabled = false;

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_LOAD);
        }

#if NO
        // �ӡ�ΥԼ�𡱿�������Ѿ�����ΥԼ��ļ�¼������ʾ��listiview��
        // return:
        //      -1  error
        //      0   not found
        //      1   �ҵ�������
        int LoadAmercedRecords(string strReaderBarcode,
            out string strError)
        {
            strError = "";

            this.listView_amerced.Items.Clear();

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.SetMessage("���ڼ����ѽ����ü�¼ " + strReaderBarcode + " ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();


            try
            {
                string strDbName = "ΥԼ��";
                string strFrom = "����֤����";
                string strMatchStyle = "exact";
                string strLang = "zh";
                string strQueryXml = "";

                long lRet = Channel.GetSystemParameter(
                    stop,
                    "amerce",
                    "dbname",
                    out strDbName,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                // 2010/12/16 change
                if (lRet == 0 || String.IsNullOrEmpty(strDbName) == true)
                {
                    if (String.IsNullOrEmpty(strError) == true)
                        strError = "ΥԼ�����û�����á�";
                    goto ERROR1;
                }

                // 2007/4/5 ���� ������ GetXmlStringSimple()
                strQueryXml = "<target list='" + strDbName + ":" + strFrom + "'><item><word>"
    + StringUtil.GetXmlStringSimple(strReaderBarcode)
    + "</word><match>" + strMatchStyle + "</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>" + strLang + "</lang></target>";

                lRet = Channel.Search(
                    stop,
                    strQueryXml,
                    "amerced",
                    "", // strOutputStyle
                    out strError);
                if (lRet == 0)
                {
                    strError = "not found";
                    return 0;   // not found
                }
                if (lRet == -1)
                    goto ERROR1;

                long lHitCount = lRet;

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                Record[] searchresults = null;


                // ��ý������װ��listview
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
                        "amerced",   // strResultSetName
                        lStart,
                        lPerCount,
                        "id",   // "id,cols"
                        strLang,
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
                        string strPath = searchresults[i].Path;

                        byte[] timestamp = null;
                        string strXml = "";

                        lRet = Channel.GetRecord(stop,
                            strPath,
                            out timestamp,
                            out strXml,
                            out strError);
                        if (lRet == -1)
                        {
                            goto ERROR1;
                        }

                        int nRet = FillAmercedLine(
                            stop,
                            strXml,
                            strPath,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                    }

                    lStart += searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;
                }
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

        // �ӡ�ΥԼ�𡱿�����������Ѿ�����ΥԼ��ļ�¼����׷����ʾ��listiview��
        // return:
        //      -1  error
        //      0   not found
        //      1   �ҵ�������
        int LoadAmercedRecords(
            List<string> ids,
            out string strError)
        {
            strError = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.SetMessage("���ڻ�ȡ�ѽ����ü�¼ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();


            try
            {
                string strDbName = "ΥԼ��";
                string strFrom = "ID";
                string strMatchStyle = "exact";
                string strLang = "zh";
                string strQueryXml = "";

                long lRet = Channel.GetSystemParameter(
                    stop,
                    "amerce",
                    "dbname",
                    out strDbName,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (lRet == 0 || String.IsNullOrEmpty(strDbName) == true)
                {
                    if (String.IsNullOrEmpty(strError) == true)
                        strError = "ΥԼ�����û�����á�";
                    goto ERROR1;
                }

                strQueryXml = "<target list='" + strDbName + ":" + strFrom + "'>";
                for (int i = 0; i < ids.Count;i++ )
                {
                    string strID = ids[i];

                    if (i > 0)
                        strQueryXml += "<operator value='OR' />";

                    strQueryXml += "<item><word>"
        + StringUtil.GetXmlStringSimple(strID)
        + "</word><match>" + strMatchStyle + "</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>" + strLang + "</lang>";
                }
                strQueryXml += "</target>";

                lRet = Channel.Search(
                    stop,
                    strQueryXml,
                    "amerced",
                    "", // strOutputStyle
                    out strError);
                if (lRet == 0)
                {
                    strError = "not found";
                    return 0;   // not found
                }
                if (lRet == -1)
                    goto ERROR1;

                long lHitCount = lRet;

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                Record[] searchresults = null;


                // ��ý������װ��listview
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
                        "amerced",   // strResultSetName
                        lStart,
                        lPerCount,
                        "id",   // "id,cols"
                        strLang,
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
                        string strPath = searchresults[i].Path;

                        byte[] timestamp = null;
                        string strXml = "";

                        lRet = Channel.GetRecord(stop,
                            strPath,
                            out timestamp,
                            out strXml,
                            out strError);
                        if (lRet == -1)
                        {
                            goto ERROR1;
                        }

                        int nRet = FillAmercedLine(
                            stop,
                            strXml,
                            strPath,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                    }

                    lStart += searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;
                }
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

#endif

        #region װ���Ѿ�����������߳�

        void StopFillAmerced(bool bForce)
        {
            // �����ǰ����������ֹͣ
            m_bStopFillAmerced = true;


            if (bForce == true)
            {
                if (this.threadFillAmerced != null)
                {
                    if (!this.threadFillAmerced.Join(2000))
                        this.threadFillAmerced.Abort();

                    this.threadFillAmerced = null;
                }
            }
        }

        // �����ids�����ʾ׷�����ǡ�����Ϊ����װ��strReaderBaroodeָ���Ķ��ߵ��ѽ��Ѽ�¼
        void BeginFillAmerced(string strReaderBarcode,
            List<string> ids)
        {
            // �����ǰ����������ֹͣ
            StopFillAmerced(true);

            this.FillAmercedParam = new FillAmercedParam();
            this.FillAmercedParam.ReaderBarcode = strReaderBarcode;
            this.FillAmercedParam.IDs = ids;
            this.FillAmercedParam.FillSummary = this.checkBox_fillSummary.Checked;

            this.threadFillAmerced =
        new Thread(new ThreadStart(this.ThreadFillAmercedMain));
            this.threadFillAmerced.Start();
        }


        /*public*/ void ThreadFillAmercedMain()
        {
            string strError = "";
            m_bStopFillAmerced = false;

            LibraryChannel channel = new LibraryChannel();
            channel.Url = this.MainForm.LibraryServerUrl;

            channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            channel.AfterLogin -= new AfterLoginEventHandle(Channel_AfterLogin);
            channel.AfterLogin += new AfterLoginEventHandle(Channel_AfterLogin);

            try
            {
                string strResultSetName = "";
                // ���һЩϵͳ����
                string strDbName = "ΥԼ��";
                string strQueryXml = "";
                string strLang = "zh";

                long lRet = Channel.GetSystemParameter(
                    stop,
                    "amerce",
                    "dbname",
                    out strDbName,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (m_bStopFillAmerced == true)
                    return;

                // 2010/12/16 change
                if (lRet == 0 || String.IsNullOrEmpty(strDbName) == true)
                {
                    if (String.IsNullOrEmpty(strError) == true)
                        strError = "ΥԼ�����û�����á�";
                    goto ERROR1;
                }

                if (string.IsNullOrEmpty(this.FillAmercedParam.ReaderBarcode) == false)
                {
                    Safe_clearList(this.listView_amerced);

                    string strFrom = "����֤����";
                    string strMatchStyle = "exact";

                    // 2007/4/5 ���� ������ GetXmlStringSimple()
                    strQueryXml = "<target list='" + strDbName + ":" + strFrom + "'><item><word>"
        + StringUtil.GetXmlStringSimple(this.FillAmercedParam.ReaderBarcode)
        + "</word><match>" + strMatchStyle + "</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>" + strLang + "</lang></target>";

                    strResultSetName = "amercing";
                } // end of strReaderBarcode != ""
                else
                {
                    if (this.FillAmercedParam.IDs == null || this.FillAmercedParam.IDs.Count == 0)
                    {
                        strError = "IDs ��������Ϊ��";
                        goto ERROR1;
                    }

                    string strFrom = "ID";
                    string strMatchStyle = "exact";

                    strQueryXml = "<target list='" + strDbName + ":" + strFrom + "'>";
                    for (int i = 0; i < this.FillAmercedParam.IDs.Count; i++)
                    {
                        string strID = this.FillAmercedParam.IDs[i];

                        if (i > 0)
                            strQueryXml += "<operator value='OR' />";

                        strQueryXml += "<item><word>"
            + StringUtil.GetXmlStringSimple(strID)
            + "</word><match>" + strMatchStyle + "</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>" + strLang + "</lang>";
                    }
                    strQueryXml += "</target>";

                    strResultSetName = "amerced";
                }

                // ��ʼ����
                lRet = channel.Search(
    stop,
    strQueryXml,
    strResultSetName,
    "", // strOutputStyle
    out strError);
                if (lRet == 0)
                {
                    strError = "not found";
                    return;   // not found
                }
                if (lRet == -1)
                    goto ERROR1;

                if (m_bStopFillAmerced == true)
                    return;

                long lHitCount = lRet;

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                Record[] searchresults = null;


                // ��ý������װ��listview
                for (; ; )
                {

                    if (m_bStopFillAmerced == true)
                    {
                        strError = "�жϣ��б�����...";
                        goto ERROR1;
                    }
                    // stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    lRet = channel.GetSearchResult(
                        stop,
                        strResultSetName,   // strResultSetName
                        lStart,
                        lPerCount,
                        "id",   // "id,cols"
                        strLang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (lRet == 0)
                    {
                        strError = "δ����";
                        return;
                    }

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        if (m_bStopFillAmerced == true)
                        {
                            strError = "�жϣ��б�����...";
                            goto ERROR1;
                        }

                        string strPath = searchresults[i].Path;

                        byte[] timestamp = null;
                        string strXml = "";

                        lRet = channel.GetRecord(stop,
                            strPath,
                            out timestamp,
                            out strXml,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ErrorCode.AccessDenied)
                                continue;
                            goto ERROR1;
                        }

                        int nRet = Safe_fillAmercedLine(
                            stop,
                            strXml,
                            strPath,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                    }

                    lStart += searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;
                }


                // �ڶ��׶Σ����ժҪ
                if (this.FillAmercedParam.FillSummary == true)
                {
                    List<ListViewItem> items = Safe_getItemList(this.listView_amerced);

                    for (int i = 0; i < items.Count; i++)
                    {
                        if (this.m_bStopFillAmerced == true)
                            return;

                        ListViewItem item = items[i];

                        string strSummary = "";
                        string strItemBarcode = "";

                        Safe_getBarcodeAndSummary(listView_amerced,
        item,
        out strItemBarcode,
        out strSummary);

                        // �Ѿ��������ˣ��Ͳ�ˢ����
                        if (String.IsNullOrEmpty(strSummary) == false)
                            continue;

                        if (String.IsNullOrEmpty(strItemBarcode) == true
                            /*&& String.IsNullOrEmpty(strItemRecPath) == true*/)
                            continue;

                        try
                        {
                            string strBiblioRecPath = "";
                            lRet = channel.GetBiblioSummary(
                                null,
                                strItemBarcode,
                                "", // strItemRecPath,
                                null,
                                out strBiblioRecPath,
                                out strSummary,
                                out strError);
                            if (lRet == -1)
                            {
                                strSummary = strError;  // 2009/3/13 changed
                                // return -1;
                            }

                        }
                        finally
                        {
                        }

                        Safe_changeItemText(item, COLUMN_AMERCING_BIBLIOSUMMARY, strSummary);
                    }
                }
                return;
            }
            finally
            {
                channel.Close();
                m_bStopFillAmerced = true;
            }

        ERROR1:
            Safe_setError(this.listView_amerced, strError);
            // Safe_errorBox(strError);
        }


        // FillAmercedLine
        delegate int Delegate_FillAmercedLine(Stop stop,
            string strXml,
            string strRecPath,
            out string strError);


        int Safe_fillAmercedLine(Stop stop,
            string strXml,
            string strRecPath,
            out string strError)
        {
            //string strItemBarcodeParam = "";
            //string strSummaryParam = "";
            Delegate_FillAmercedLine d = new Delegate_FillAmercedLine(FillAmercedLine);
            object[] args = new object[] { stop, strXml, strRecPath, "" };
            int nRet = (int)this.Invoke(d, args);
            strError = (string)args[3];
            return nRet;
        }


        #endregion

        #region װ��δ����������߳�

        void StopFillAmercing(bool bForce)
        {
            // �����ǰ����������ֹͣ
            m_bStopFillAmercing = true;

            if (bForce == true)
            {
                if (this.threadFillAmercing != null)
                {
                    if (!this.threadFillAmercing.Join(2000))
                        this.threadFillAmercing.Abort();
                    this.threadFillAmercing = null;
                }
            }
        }

        void BeginFillAmercing(string strXml)
        {
            // �����ǰ����������ֹͣ
            StopFillAmercing(true);

            FillAmercingParam = new FillAmercingParam();
            this.FillAmercingParam.Xml = strXml;
            this.FillAmercingParam.FillSummary = this.checkBox_fillSummary.Checked;

            this.threadFillAmercing =
        new Thread(new ThreadStart(this.ThreadFillAmercingMain));
            this.threadFillAmercing.Start();
        }

        // ClearList
        delegate void Delegate_ClearList(ListView list);

        void ClearList(ListView list)
        {
            list.Items.Clear();
        }

        void Safe_clearList(ListView list)
        {
            Delegate_ClearList d = new Delegate_ClearList(ClearList);
            this.Invoke(d, new object[] { list });
        }

        // ErrorBox
        delegate void Delegate_ErrorBox(string strText);

        void ErrorBox(string strText)
        {
            MessageBox.Show(this, strText);
        }

        void Safe_errorBox(string strText)
        {
            Delegate_ErrorBox d = new Delegate_ErrorBox(ErrorBox);
            this.Invoke(d, new object[] { strText });
        }

        // AddListItem
        delegate void Delegate_AddListItem(ListView list, ListViewItem item);

        void AddListItem(ListView list, ListViewItem item)
        {
            list.Items.Add(item);
        }

        void Safe_addListItem(ListView list, ListViewItem item)
        {
            Delegate_AddListItem d = new Delegate_AddListItem(AddListItem);
            this.Invoke(d, new object[] { list, item });
        }

        // GetBarcodeAndSummary
        delegate void Delegate_GetBarcodeAndSummary(ListView list,
            ListViewItem item,
            out string strItemBarcode,
            out string strSummary);

        void GetBarcodeAndSummary(ListView list,
            ListViewItem item,
            out string strItemBarcode,
            out string strSummary)
        {
            if (list == this.listView_overdues)
            {
                strSummary = ListViewUtil.GetItemText(item, COLUMN_AMERCING_BIBLIOSUMMARY);
                strItemBarcode = ListViewUtil.GetItemText(item, COLUMN_AMERCING_ITEMBARCODE);
            }
            else
            {
                strSummary = ListViewUtil.GetItemText(item, COLUMN_AMERCED_BIBLIOSUMMARY);
                strItemBarcode = ListViewUtil.GetItemText(item, COLUMN_AMERCED_ITEMBARCODE);
            }
        }

        void Safe_getBarcodeAndSummary(ListView list,
            ListViewItem item,
            out string strItemBarcode,
            out string strSummary)
        {
            //string strItemBarcodeParam = "";
            //string strSummaryParam = "";
            Delegate_GetBarcodeAndSummary d = new Delegate_GetBarcodeAndSummary(GetBarcodeAndSummary);
            object[] args = new object[] { list, item, "", "" };
            this.Invoke(d, args);
            strItemBarcode = (string)args[2];
            strSummary = (string)args[3];
        }

        // ChangeItemText
        delegate void Delegate_ChangeItemText(ListViewItem item, int nCol, string strText);

        void ChangeItemText(ListViewItem item, int nCol, string strText)
        {
            ListViewUtil.ChangeItemText(item, nCol, strText);
        }

        void Safe_changeItemText(ListViewItem item, int nCol, string strText)
        {
            Delegate_ChangeItemText d = new Delegate_ChangeItemText(ChangeItemText);
            this.Invoke(d, new object[] { item, nCol, strText });
        }

        // GetItemList
        delegate List<ListViewItem> Delegate_GetItemList(ListView list);

        List<ListViewItem> GetItemList(ListView list)
        {
            List<ListViewItem> results = new List<ListViewItem>();
            foreach (ListViewItem item in list.Items)
            {
                results.Add(item);
            }
            return results;
        }

        List<ListViewItem> Safe_getItemList(ListView list)
        {
            Delegate_GetItemList d = new Delegate_GetItemList(GetItemList);
            return (List<ListViewItem>)this.Invoke(d, new object[] { list });
        }

        // SetError
        delegate void Delegate_SetError(ListView list,
            string strError);

        // ���ô����ַ�����ʾ
        static void SetError(ListView list,
            string strError)
        {
            // list.Items.Clear();
            ListViewItem item = new ListViewItem();
            // item.ImageIndex = ITEMTYPE_ERROR;
            item.Text = "";
            item.SubItems.Add("����: " + strError);
            ListViewUtil.ChangeItemText(item, COLUMN_AMERCED_STATE, "error");
            list.Items.Add(item);
        }

        void Safe_setError(ListView list,
            string strError)
        {
            Delegate_SetError d = new Delegate_SetError(SetError);
            this.Invoke(d, new object[] { list, strError });
        }

        /*public*/ void ThreadFillAmercingMain()
        {
            string strError = "";
            m_bStopFillAmercing = false;

            LibraryChannel channel = new LibraryChannel();
            channel.Url = this.MainForm.LibraryServerUrl;

            channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            try
            {

                Safe_clearList(this.listView_overdues);

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(this.FillAmercingParam.Xml);
                }
                catch (Exception ex)
                {
                    strError = "����XML��¼װ��XMLDOMʱ��������: " + ex.Message;
                    goto ERROR1;
                }

                List<string> dup_ids = new List<string>();

                // ѡ������<overdue>Ԫ��
                XmlNodeList nodes = dom.DocumentElement.SelectNodes("overdues/overdue");

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (this.m_bStopFillAmercing == true)
                    {
                        strError = "�жϣ��б�����...";
                        goto ERROR1;
                    }

                    XmlNode node = nodes[i];
                    string strItemBarcode = DomUtil.GetAttr(node, "barcode");
                    string strItemRecPath = DomUtil.GetAttr(node, "recPath");
                    string strReason = DomUtil.GetAttr(node, "reason");
                    string strBorrowDate = DomUtil.GetAttr(node, "borrowDate");

                    strBorrowDate = DateTimeUtil.LocalTime(strBorrowDate, "u");

                    string strBorrowPeriod = DomUtil.GetAttr(node, "borrowPeriod");
                    string strReturnDate = DomUtil.GetAttr(node, "returnDate");

                    strReturnDate = DateTimeUtil.LocalTime(strReturnDate, "u");

                    string strID = DomUtil.GetAttr(node, "id");
                    string strPrice = DomUtil.GetAttr(node, "price");
                    string strComment = DomUtil.GetAttr(node, "comment");

                    string strBorrowOperator = DomUtil.GetAttr(node, "borrowOperator");
                    string strReturnOperator = DomUtil.GetAttr(node, "operator");

                    XmlNodeList dup_nodes = dom.DocumentElement.SelectNodes("overdues/overdue[@id='" + strID + "']");
                    if (dup_nodes.Count > 1)
                    {
                        dup_ids.Add(strID);
                    }


                    // TODO: ժҪ�����첽����������ȫ������װ����ɺ󵥶�ɨ��һ����
                    string strSummary = "";


                    ListViewItem item = new ListViewItem(strItemBarcode);

                    // ժҪ
                    // item.SubItems.Add(strSummary);
                    ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_BIBLIOSUMMARY, strSummary);

                    // ���
                    // item.SubItems.Add(strPrice);
                    ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_PRICE, strPrice);

                    // ע��
                    // item.SubItems.Add(strComment);
                    ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_COMMENT, strComment);

                    // ΥԼԭ��
                    // item.SubItems.Add(strReason);
                    ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_REASON, strReason);

                    // ��������
                    // item.SubItems.Add(strBorrowDate);
                    ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_BORROWDATE, strBorrowDate);

                    // ����ʱ��
                    // item.SubItems.Add(strBorrowPeriod);
                    ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_BORROWPERIOD, strBorrowPeriod);

                    // ��������
                    // item.SubItems.Add(strReturnDate);
                    ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_RETURNDATE, strReturnDate);

                    // id
                    // item.SubItems.Add(strID);
                    ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_ID, strID);

                    ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_BORROWOPERATOR, strBorrowOperator);
                    ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_RETURNOPERATOR, strReturnOperator);

                    // ����ԭʼ�۸��ע�ͱ���
                    AmercingItemInfo info = new AmercingItemInfo();
                    info.Price = strPrice;
                    info.Comment = strComment;
                    info.Xml = node.OuterXml;
                    item.Tag = info;

                    Safe_addListItem(this.listView_overdues, item);
                }

                if (dup_ids.Count > 0)
                {
                    StringUtil.RemoveDupNoSort(ref dup_ids);
                    Debug.Assert(dup_ids.Count >= 1, "");
                    strError = "δ�������б��з�������ID�������ظ�������һ�����ش�����ϵͳ����Ա�����ų���\r\n---\r\n" + StringUtil.MakePathList(dup_ids, "; ");
                    goto ERROR1;
                }

                // �ڶ��׶Σ����ժҪ
                if (this.FillAmercingParam.FillSummary == true)
                {
                    List<ListViewItem> items = Safe_getItemList(listView_overdues);

                    for (int i = 0; i < items.Count; i++)
                    {
                        if (this.m_bStopFillAmercing == true)
                            return;

                        ListViewItem item = items[i];

                        string strSummary = "";
                        string strItemBarcode = "";

                        Safe_getBarcodeAndSummary(listView_overdues,
        item,
        out strItemBarcode,
        out strSummary);

                        // �Ѿ��������ˣ��Ͳ�ˢ����
                        if (String.IsNullOrEmpty(strSummary) == false)
                            continue;

                        if (String.IsNullOrEmpty(strItemBarcode) == true
                            /*&& String.IsNullOrEmpty(strItemRecPath) == true*/)
                            continue;

                        try
                        {
                            string strBiblioRecPath = "";
                            long lRet = channel.GetBiblioSummary(
                                null,
                                strItemBarcode,
                                "", // strItemRecPath,
                                null,
                                out strBiblioRecPath,
                                out strSummary,
                                out strError);
                            if (lRet == -1)
                            {
                                strSummary = strError;  // 2009/3/13 changed
                                // return -1;
                            }

                        }
                        finally
                        {
                        }

                        Safe_changeItemText(item, COLUMN_AMERCING_BIBLIOSUMMARY, strSummary);
                    }
                }


                return;
            }
            finally
            {
                channel.Close();
                m_bStopFillAmercing = true;
            }

        ERROR1:
            Safe_setError(this.listView_overdues, strError);
            // Safe_errorBox(strError);
        }

        #endregion

#if NO
        void StopFillSummary()
        {
            // �����ǰ����������ֹͣ
            m_bStopFilling = true;
        }

        void BeginFillSummary()
        {
            // �����ǰ����������ֹͣ
            m_bStopFilling = true;

            if (this.threadFillSummary != null)
            {
                this.threadFillSummary.Abort();
                this.threadFillSummary = null;
            }


            this.threadFillSummary =
        new Thread(new ThreadStart(this.ThreadFillSummaryMain));
            this.threadFillSummary.Start();
        }


        public void ThreadFillSummaryMain()
        {
            m_bStopFilling = false;

            LibraryChannel channel = new LibraryChannel();
            channel.Url = this.MainForm.LibraryServerUrl;

            channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            try
            {

#if NOOOOOOOOO
                Delegate_FillSummary d = new Delegate_FillSummary(FillSummary);
                this.Invoke(d, new object[] { this.listView_overdues,
                channel,
                COLUMN_AMERCING_ITEMBARCODE,
                COLUMN_AMERCING_BIBLIOSUMMARY });


                if (m_bStopFilling == true)
                    return;
#endif

                /*
                FillSummary(
                this.listView_amerced,
                COLUMN_AMERCED_ITEMBARCODE,
                COLUMN_AMERCED_BIBLIOSUMMARY);
                 * */

                Delegate_FillSummary d = new Delegate_FillSummary(FillSummary);
                this.Invoke(d, new object[] { this.listView_amerced,
                channel,
                COLUMN_AMERCED_ITEMBARCODE,
                COLUMN_AMERCED_BIBLIOSUMMARY });
                m_bStopFilling = true;
            }
            finally
            {
                channel.Close();
            }
        }

        delegate void Delegate_FillSummary(ListView list,
            LibraryChannel channel,
            int iColumnBarcode,
            int iColumnSummary);

        void FillSummary(
            ListView list,
            LibraryChannel channel,
            int iColumnBarcode,
            int iColumnSummary)
        {
            string strError = "";

            for (int i = 0; i < list.Items.Count; i++)
            {
                if (m_bStopFilling == true)
                    return;
                
                ListViewItem item = list.Items[i];

                string strSummary = ListViewUtil.GetItemText(item, iColumnSummary);
                string strItemBarcode = ListViewUtil.GetItemText(item, iColumnBarcode);
                // string strItemRecPath = ListViewUtil.GetItemText(item, iColumnSummary);

                if (String.IsNullOrEmpty(strSummary) == false)
                    continue;

                if (String.IsNullOrEmpty(strItemBarcode) == true
                    /*&& String.IsNullOrEmpty(strItemRecPath) == true*/)
                    continue;

                this.stop.SetMessage("���ں�̨��ȡժҪ " + strItemBarcode + " ...");

                try
                {

                    string strBiblioRecPath = "";
                    long lRet = channel.GetBiblioSummary(
                        null,
                        strItemBarcode,
                        "", // strItemRecPath,
                        null,
                        out strBiblioRecPath,
                        out strSummary,
                        out strError);
                    if (lRet == -1)
                    {
                        strSummary = strError;  // 2009/3/13 changed
                        // return -1;
                    }

                }
                finally
                {
                }

                ListViewUtil.ChangeItemText(item, iColumnSummary, strSummary);
            }

            this.stop.SetMessage("");
        }
#endif

        // ���һ���µ�amerced��
        // stop�Ѿ������BeginLoop()��
        // TODO: Summary���ʱ���������Ϊ��������Ǵ���
        int FillAmercedLine(
            Stop stop,
            string strXml,
            string strRecPath,
            out string strError)
        {
            strError = "";

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ�ص�DOMʱ��������: " + ex.Message;
                return -1;
            }

            string strItemBarcode = DomUtil.GetElementText(dom.DocumentElement, "itemBarcode");
            string strItemRecPath = DomUtil.GetElementText(dom.DocumentElement, "itemRecPath");
            string strSummary = "";
            string strPrice = DomUtil.GetElementText(dom.DocumentElement, "price");
            string strComment = DomUtil.GetElementText(dom.DocumentElement, "comment");
            string strReason = DomUtil.GetElementText(dom.DocumentElement, "reason");
            string strBorrowDate = DomUtil.GetElementText(dom.DocumentElement, "borrowDate");

            strBorrowDate = DateTimeUtil.LocalTime(strBorrowDate, "u");

            string strBorrowPeriod = DomUtil.GetElementText(dom.DocumentElement, "borrowPeriod");
            string strReturnDate = DomUtil.GetElementText(dom.DocumentElement, "returnDate");

            strReturnDate = DateTimeUtil.LocalTime(strReturnDate, "u");

            string strID = DomUtil.GetElementText(dom.DocumentElement, "id");
            string strReturnOperator = DomUtil.GetElementText(dom.DocumentElement, "returnOperator");
            string strState = DomUtil.GetElementText(dom.DocumentElement, "state");

            // 2007/6/18 new add
            string strAmerceOperator = DomUtil.GetElementText(dom.DocumentElement, "operator");
            string strAmerceTime = DomUtil.GetElementText(dom.DocumentElement, "operTime");

            strAmerceTime = DateTimeUtil.LocalTime(strAmerceTime, "u");

            string strSettlementOperator = DomUtil.GetElementText(dom.DocumentElement, "settlementOperator");
            string strSettlementTime = DomUtil.GetElementText(dom.DocumentElement, "settlementOperTime");

            strSettlementTime = DateTimeUtil.LocalTime(strSettlementTime, "u");

#if NO
            if (String.IsNullOrEmpty(strItemBarcode) == false)
            {
                // stop.OnStop += new StopEventHandler(this.DoStop);
                stop.SetMessage("���ڻ�ȡժҪ " + strItemBarcode + " ...");
                // stop.BeginLoop();

                try
                {

                    string strBiblioRecPath = "";
                    long lRet = Channel.GetBiblioSummary(
                        stop,
                        strItemBarcode,
                        strItemRecPath,
                        null,
                        out strBiblioRecPath,
                        out strSummary,
                        out strError);
                    if (lRet == -1)
                    {
                        strSummary = strError;
                        // return -1;
                    }

                }
                finally
                {
                    // stop.EndLoop();
                    // stop.OnStop -= new StopEventHandler(this.DoStop);
                    // stop.Initial("");
                }
            }
#endif

            ListViewItem item = new ListViewItem(strItemBarcode, 0);

            /*
            item.SubItems.Add(strSummary);
            item.SubItems.Add(strPrice);
            item.SubItems.Add(strComment);
            item.SubItems.Add(strReason);
            item.SubItems.Add(strBorrowDate);
            item.SubItems.Add(strBorrowPeriod);
            item.SubItems.Add(strReturnDate);
            item.SubItems.Add(strID);
            item.SubItems.Add(strReturnOperator);
            item.SubItems.Add(strState);

            item.SubItems.Add(strAmerceOperator);
            item.SubItems.Add(strAmerceTime);
            item.SubItems.Add(strSettlementOperator);
            item.SubItems.Add(strSettlementTime);

            item.SubItems.Add(strRecPath);
             * */

            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_BIBLIOSUMMARY,
                strSummary);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_PRICE,
                strPrice);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_COMMENT,
                strComment);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_REASON,
                strReason);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_BORROWDATE,
                strBorrowDate);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_BORROWPERIOD,
                strBorrowPeriod);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_RETURNDATE,
                strReturnDate);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_ID,
                strID);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_RETURNOPERATOR,
                strReturnOperator);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_STATE,
                strState);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_AMERCEOPERATOR,
                strAmerceOperator);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_AMERCETIME,
                strAmerceTime);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_SETTLEMENTOPERATOR,
                strSettlementOperator);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_SETTLEMENTTIME,
                strSettlementTime);
            ListViewUtil.ChangeItemText(item,
                COLUMN_AMERCED_RECPATH,
                strRecPath);

            // 2012/10/8
            AmercedItemInfo info = new AmercedItemInfo();
            info.Xml = strXml;
            item.Tag = info;

            this.listView_amerced.Items.Add(item);

            return 0;
        }

#if NOOOOOOOOOOOOOOOOOO
        // װ�����XML��¼
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int LoadReaderXmlRecord(string strBarcode,
            out string strXml,
            out string strError)
        {
            strXml = "";
            strError = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.SetMessage("����װ�����XML��¼ " + strBarcode + " ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();


            try
            {

                // ChargingForm.SetHtmlString(this.webBrowser_readerInfo, "(��)");

                long lRet = Channel.GetReaderInfo(
                    stop,
                    strBarcode,
                    "xml",
                    out strXml,
                    out strError);
                if (lRet == 0)
                {
                    strError = "not found";
                    return 0;   // not found
                }
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

#endif



#if NO
        // �ɵġ������̰߳汾
        // ��䡰δ�����á��б�
        int FillAmercingList(string strXml,
            out string strError)
        {
            strError = "";

            this.listView_overdues.Items.Clear();

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "����XML��¼װ��XMLDOMʱ��������: " + ex.Message;
                return -1;
            }

            List<string> dup_ids = new List<string>();

            // ѡ������<overdue>Ԫ��
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("overdues/overdue");

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strItemBarcode = DomUtil.GetAttr(node, "barcode");
                string strItemRecPath = DomUtil.GetAttr(node, "recPath");
                string strReason = DomUtil.GetAttr(node, "reason");
                string strBorrowDate = DomUtil.GetAttr(node, "borrowDate");

                strBorrowDate = DateTimeUtil.LocalTime(strBorrowDate, "u");

                string strBorrowPeriod = DomUtil.GetAttr(node, "borrowPeriod");
                string strReturnDate = DomUtil.GetAttr(node, "returnDate");

                strReturnDate = DateTimeUtil.LocalTime(strReturnDate, "u");


                string strID = DomUtil.GetAttr(node, "id");
                string strPrice = DomUtil.GetAttr(node, "price");
                string strComment = DomUtil.GetAttr(node, "comment");

                string strBorrowOperator = DomUtil.GetAttr(node, "borrowOperator");
                string strReturnOperator = DomUtil.GetAttr(node, "operator");

                XmlNodeList dup_nodes = dom.DocumentElement.SelectNodes("overdues/overdue[@id='"+strID+"']");
                if (dup_nodes.Count > 1)
                {
                    dup_ids.Add(strID);
                }


                // TODO: ժҪ�����첽����������ȫ������װ����ɺ󵥶�ɨ��һ����
                string strSummary = "";

#if NOOOOOOOOO
                if (String.IsNullOrEmpty(strItemBarcode) == false)
                {
                    stop.OnStop += new StopEventHandler(this.DoStop);
                    stop.SetMessage("���ڻ�ȡժҪ " + strItemBarcode + " ...");
                    stop.BeginLoop();

                    try
                    {

                        string strBiblioRecPath = "";
                        long lRet = Channel.GetBiblioSummary(
                            stop,
                            strItemBarcode,
                            strItemRecPath,
                            null,
                            out strBiblioRecPath,
                            out strSummary,
                            out strError);
                        if (lRet == -1)
                        {
                            strSummary = strError;  // 2009/3/13 changed
                            // return -1;
                        }

                    }
                    finally
                    {
                        stop.EndLoop();
                        stop.OnStop -= new StopEventHandler(this.DoStop);
                        stop.Initial("");
                    }
                }
#endif

                ListViewItem item = new ListViewItem(strItemBarcode);

                this.listView_overdues.Items.Add(item);

                // ժҪ
                // item.SubItems.Add(strSummary);
                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_BIBLIOSUMMARY, strSummary);

                // ���
                // item.SubItems.Add(strPrice);
                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_PRICE, strPrice);

                // ע��
                // item.SubItems.Add(strComment);
                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_COMMENT, strComment);

                // ΥԼԭ��
                // item.SubItems.Add(strReason);
                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_REASON, strReason);

                // ��������
                // item.SubItems.Add(strBorrowDate);
                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_BORROWDATE, strBorrowDate);

                // ����ʱ��
                // item.SubItems.Add(strBorrowPeriod);
                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_BORROWPERIOD, strBorrowPeriod);

                // ��������
                // item.SubItems.Add(strReturnDate);
                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_RETURNDATE, strReturnDate);

                // id
                // item.SubItems.Add(strID);
                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_ID, strID);

                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_BORROWOPERATOR, strBorrowOperator);
                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_RETURNOPERATOR, strReturnOperator);

                // ����ԭʼ�۸��ע�ͱ���
                AmercingItemInfo info = new AmercingItemInfo();
                info.Price = strPrice;
                info.Comment = strComment;
                item.Tag = info;
            }

            if (dup_ids.Count > 0)
            {
                StringUtil.RemoveDupNoSort(ref dup_ids);
                Debug.Assert(dup_ids.Count >= 1, "");
                strError = "δ�������б��з�������ID�������ظ�������һ�����ش�����ϵͳ����Ա�����ų���\r\n---\r\n" + StringUtil.MakePathList(dup_ids, "; ");
                MessageBox.Show(this, strError);
            }

            return 0;
        }
#endif

        // checkbox��ѡ��
        private void listView_overdues_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            this.toolStripLabel_amercingMessage.Text = GetAmercingPriceMessage();

            /*
            string strError = "";
            List<AmerceItem> amerce_items = null;
            int nRet = GetCheckedIdList(this.listView_overdues,
                out amerce_items,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }

            if (amerce_items == null || amerce_items.Count == 0)
                this.button_amercingOverdue_submit.Enabled = false;
            else
                this.button_amercingOverdue_submit.Enabled = true;
             * */
            SetOverduesButtonsEnable();

            ResetAmercingItemsBackColor(this.listView_overdues);
        }


        // ������۸�ϼ�ֵ
        static string GetTotalPrice(List<OverdueItemInfo> item_infos)
        {
            List<string> prices = new List<string>();
            for (int i = 0; i < item_infos.Count; i++)
            {
                string strPrice = item_infos[i].Price;

                prices.Add(strPrice);
            }

            return PriceUtil.TotalPrice(prices);
        }

        // ����� �ѽ��� �۸�ϼ�ֵ��������ʾ��Ϣ
        string GetAmercedPriceMessage()
        {
            List<string> prices = new List<string>();
            int count = 0;
            for (int i = 0; i < this.listView_amerced.Items.Count; i++)
            {
                ListViewItem item = this.listView_amerced.Items[i];
                if (item.Checked == false)
                    continue;

                count++;
                
                string strPrice = ListViewUtil.GetItemText(item, COLUMN_AMERCED_PRICE);

                // ȥ���ַ�����������е�ע�Ͳ���
                int nRet = strPrice.IndexOf("|");
                if (nRet != -1)
                    strPrice = strPrice.Substring(0, nRet);

                if (String.IsNullOrEmpty(strPrice) == true)
                    continue;

                prices.Add(strPrice);
            }

            if (count == 0)
                return "��δѡ���κ���������������ѡ�󣬰�Ŧ�ſ��� -->";

            return "ѡ�й� " + count.ToString() + " ��, �ϼƽ��: " + PriceUtil.TotalPrice(prices);
        }


        // ����� δ���� �۸�ϼ�ֵ��������ʾ��Ϣ
        string GetAmercingPriceMessage()
        {
            List<string> prices = new List<string>();
            // double total = 0;
            int count = 0;
            for (int i = 0; i < this.listView_overdues.Items.Count; i++)
            {
                ListViewItem item = this.listView_overdues.Items[i];
                if (item.Checked == false)
                    continue;

                count++;

                string strPrice = ListViewUtil.GetItemText(item, COLUMN_AMERCING_PRICE);    //  this.listView_overdues.Items[i].SubItems[2].Text;

#if NO
                // ȥ���ַ�����������е�ע�Ͳ���
                int nRet = strPrice.IndexOf("|");
                if (nRet != -1)
                    strPrice = strPrice.Substring(0, nRet);
#endif

                if (String.IsNullOrEmpty(strPrice) == true)
                    continue;

                // ���Ǻŵ��Ǳ����Ҫȥ���Ǻ�
                /*
                if (strPrice.Length > 0 && strPrice[0] == '*')
                {
                    strPrice = strPrice.Substring(1);
                }
                 * */
                strPrice = RemoveChangedMask(strPrice);

                /*
                // ��ȡ��������
                string strPurePrice = Global.GetPurePrice(strPrice);

                if (String.IsNullOrEmpty(strPurePrice) == true)
                    continue;

                total += Convert.ToDouble(strPurePrice);
                 * */
                prices.Add(strPrice);
            }
            if (count == 0)
                return "��δѡ���κ���������������ѡ�󣬰�Ŧ�ſ��� -->";

            // return "ѡ�й� " + count.ToString() + " ��, �ϼƽ��: " + total.ToString();
            return "ѡ�й� " + count.ToString() + " ��, �ϼƽ��: " + PriceUtil.TotalPrice(prices);
        }

        void SelectAll(ListView listview)
        {
            for (int i = 0; i < listview.Items.Count; i++)
            {
                if (Control.ModifierKeys == Keys.Control)
                {
                    // ���ܷ�ת -- Unselect all
                    if (listview.Items[i].Checked == true)
                        listview.Items[i].Checked = false;
                }
                else
                {
                    // �������� -- Select all
                    if (listview.Items[i].Checked == false)
                        listview.Items[i].Checked = true;
                }

                /*
                // TODO: ��Ҫ��ͳһ��ģ��
                if (listview.Items[i].Checked == false)
                {
                    listview.Items[i].BackColor = SystemColors.Window;
                }
                else
                {
                    listview.Items[i].BackColor = Color.Yellow;
                }
                 * */
            }

            if (listview == this.listView_overdues)
                ResetAmercingItemsBackColor(listview);
            else if (listview == this.listView_amerced)
                ResetAmercedItemsBackColor(listview);
            else
            {
                Debug.Assert(false, "δ֪��listview");
            }
        }


        // �����ʾ�õ���Ϣ
        // parameters:
        int GetCheckedOverdueInfos(ListView listview,
            out List<OverdueItemInfo> overdue_infos,
            out string strError)
        {
            strError = "";
            overdue_infos = new List<OverdueItemInfo>();
            int nCheckedCount = 0;

            // Ŀǰ����listview��id�ж�����8
            for (int i = 0; i < listview.Items.Count; i++)
            {
                ListViewItem item = listview.Items[i];
                if (item.Checked == false)
                    continue;

                string strID = "";
                string strPrice = "";
                string strComment = "";
                if (listview == this.listView_amerced)
                {
                    // strID = listview.Items[i].SubItems[8].Text;
                    // strPriceComment = listview.Items[i].SubItems[2].Text;
                    strID = ListViewUtil.GetItemText(item, COLUMN_AMERCED_ID);
                    strPrice = ListViewUtil.GetItemText(item, COLUMN_AMERCED_PRICE);
                    if (string.IsNullOrEmpty(strPrice) == false)
                    {
                        string strResultPrice = "";
                        // ������"-123.4+10.55-20.3"�ļ۸��ַ�����ת������
                        // parameters:
                        //      bSum    �Ƿ�Ҫ˳�����? true��ʾҪ����
                        int nRet = PriceUtil.NegativePrices(strPrice,
                            false,
                            out strResultPrice,
                            out strError);
                        if (nRet == -1)
                            strPrice = "-(" + strPrice + ")";
                        else
                            strPrice = strResultPrice;
                    }
                    strComment = ListViewUtil.GetItemText(item, COLUMN_AMERCED_COMMENT);
                    if (string.IsNullOrEmpty(strComment) == true)
                        strComment = "���ؽ���";
                    else
                        strComment = "���ؽ��� (" + strComment + ")";
                }
                else
                {
                    Debug.Assert(listview == this.listView_overdues, "");
                    strID = ListViewUtil.GetItemText(item, COLUMN_AMERCING_ID);
                    strPrice = ListViewUtil.GetItemText(item, COLUMN_AMERCING_PRICE);
                    strComment = ListViewUtil.GetItemText(item, COLUMN_AMERCING_COMMENT);
                }

                if (String.IsNullOrEmpty(strID) == true)
                {
                    strError = "������idΪ�յ��С�";
                    return -1;
                }

                // ���Ǻŵ��Ǳ�����
                strPrice = RemoveChangedMask(strPrice);

                OverdueItemInfo info = new OverdueItemInfo();
                info.Price = strPrice;
                info.ItemBarcode = ListViewUtil.GetItemText(item, 
                    COLUMN_AMERCING_ITEMBARCODE);
                info.RecPath = ""; // recPath
                info.Reason = ListViewUtil.GetItemText(item, 
                    COLUMN_AMERCING_REASON);

                info.BorrowDate = ListViewUtil.GetItemText(item, 
                    COLUMN_AMERCING_BORROWDATE);
                info.BorrowPeriod = ListViewUtil.GetItemText(item, 
                    COLUMN_AMERCING_BORROWPERIOD);
                info.ReturnDate = ListViewUtil.GetItemText(item, 
                    COLUMN_AMERCING_RETURNDATE);
                info.BorrowOperator = ListViewUtil.GetItemText(item,
                    COLUMN_AMERCING_BORROWOPERATOR);  // borrowOperator
                info.ReturnOperator = ListViewUtil.GetItemText(item,
                    COLUMN_AMERCING_RETURNOPERATOR);    // operator
                info.ID = ListViewUtil.GetItemText(item,
                    COLUMN_AMERCING_ID);

                // 2008/11/15 new add
                info.Comment = strComment;

                overdue_infos.Add(info);

                nCheckedCount++;
            }

            return nCheckedCount;
        }

        // ��õ�ǰ�������Ƿ����޸ĺ�δ���ֵ�״̬(�ǡ���)����Ϣ(������Щ�����޸���Ϣ)
        // return:
        //      -1  ִ�й��̷�������
        //      0   û���޸�
        //      >0  �޸Ĺ����������޸ĵ���������ϸ��Ϣ��strInfo��
        int GetChangedInfo(
            out string strInfo,
            out string strError)
        {
            strError = "";
            strInfo = "";
            int nChangedCount = 0;

            ListView listview = this.listView_overdues;

            // Ŀǰ����listview��id�ж�����8

            for (int i = 0; i < listview.Items.Count; i++)
            {
                ListViewItem item = listview.Items[i];

                AmercingItemInfo info = (AmercingItemInfo)item.Tag;
                if (info == null)
                    continue;

                Debug.Assert(info != null, "");

                string strID = ListViewUtil.GetItemText(item, COLUMN_AMERCING_ID);  // listview.Items[i].SubItems[8].Text;
                string strPrice = ListViewUtil.GetItemText(item, COLUMN_AMERCING_PRICE);  // listview.Items[i].SubItems[2].Text;
                string strComment = ListViewUtil.GetItemText(item, COLUMN_AMERCING_COMMENT); 

                if (String.IsNullOrEmpty(strID) == true)
                {
                    strError = "������idΪ�յ��С�";
                    return -1;
                }

                bool bChanged = false;

                string strExistComment = "";
                string strAppendComment = "";
                ParseCommentString(strComment,
                    out strExistComment,
                    out strAppendComment);

                bool bAppendComment = false;
                bool bCommentChanged = false;   // ע���Ƿ������޸�?

                if (strExistComment != info.Comment)
                    bCommentChanged = true;

                if (string.IsNullOrEmpty(strAppendComment) == false)
                    bAppendComment = true;

                /*
                if (bCommentChanged == true || bAppendComment == true)
                    bChanged = true;
                */

                // ���ǺŵĲ��Ǳ�����
                string strNewPrice = "";

                // ֻҪ���������߱�֮һ
                if ((strPrice.Length > 0 && strPrice[0] == '*')
                    || (bCommentChanged == true || bAppendComment == true))
                {
                    if (String.IsNullOrEmpty(strInfo) == false)
                        strInfo += ";\r\n";
                    strInfo += "�� " + (i + 1).ToString() + " ��";
                    bChanged = true;
                }

                int nFragmentCount = 0;

                // ��һ������
                if (strPrice.Length > 0 && strPrice[0] == '*')
                {
                    strNewPrice = strPrice.Substring(1);

                    strInfo += "�۸��޸�Ϊ " + strNewPrice + " ";
                    nFragmentCount++;
                    /*
                    if (bCommentChanged == true)
                    {
                        if (bAppendComment == false)
                            strInfo += "������ע�ͱ��޸�Ϊ '" + strExistComment + "'";
                        else
                            strInfo += "������ע��Ҫ׷������ '" + strAppendComment + "'";
                    }
                     * */

                }

                // �ڶ�������
                if (bCommentChanged == true || bAppendComment == true)
                {
                    if (nFragmentCount > 0)
                        strInfo += ", ";

                    if (bCommentChanged == true)
                    {
                        strInfo += "ע�ͱ��޸�Ϊ '" + strExistComment + "'";
                        if (bAppendComment == true)
                            strInfo += ", ";
                    }

                    if (bAppendComment == true)
                        strInfo += "ע��Ҫ׷������ '" + strAppendComment + "'";
                }


                if (bChanged == true)
                    nChangedCount ++;
            }

            return nChangedCount;
        }

        // ���������ύ��dp2library��comment�ַ���
        static string BuildCommitComment(
            bool bExistCommentChanged,
            string strExistComment,
            bool bAppendCommentChanged,
            string strAppendComment)
        {
            string strResult = "";
            if (bExistCommentChanged)
                strResult += "<" + strExistComment;
            if (bAppendCommentChanged)
                strResult += ">" + strAppendComment;

            return strResult;
        }

        // parameters:
        //      strFunction Ϊ"amerce" "modifyprice" "modifycomment" ֮һ
        int GetCheckedIdList(ListView listview,
            string strFunction,
            out List<AmerceItem> amerce_items,
            out string strError)
        {
            strError = "";
            amerce_items = new List<AmerceItem>();
            // Ŀǰ����listview��id�ж�����8

            for (int i = 0; i < listview.Items.Count; i++)
            {
                ListViewItem item = listview.Items[i];
                if (item.Checked == false)
                    continue;

                AmercingItemInfo info = null;

                string strID = "";
                string strPrice = "";
                string strComment = "";
                if (listview == this.listView_amerced)
                {
                    // strID = listview.Items[i].SubItems[8].Text;
                    // strPriceComment = listview.Items[i].SubItems[2].Text;
                    strID = ListViewUtil.GetItemText(item, COLUMN_AMERCED_ID);
                    strPrice = ListViewUtil.GetItemText(item, COLUMN_AMERCED_PRICE);
                    strComment = ListViewUtil.GetItemText(item, COLUMN_AMERCED_COMMENT);
                }
                else
                {
                    Debug.Assert(listview == this.listView_overdues, "");
                    strID = ListViewUtil.GetItemText(item, COLUMN_AMERCING_ID);
                    strPrice = ListViewUtil.GetItemText(item, COLUMN_AMERCING_PRICE);
                    strComment = ListViewUtil.GetItemText(item, COLUMN_AMERCING_COMMENT);

                    info = (AmercingItemInfo)item.Tag;
                    Debug.Assert(info != null, "");
                }

                if (String.IsNullOrEmpty(strID) == true)
                {
                    strError = "������idΪ�յ��С�";
                    return -1;
                }

                // ���ǺŵĲ��Ǳ�����
                string strNewPrice = "";
                if (strPrice.Length > 0 && strPrice[0] == '*')
                    strNewPrice = strPrice.Substring(1);

                if (strFunction == "modifyprice")
                {
                    // ����۸�û�б仯��������modifyprice
                    if (strNewPrice == "")
                        continue;
                }

                string strExistComment = "";
                string strAppendComment = "";
                ParseCommentString(strComment,
                    out strExistComment,
                    out strAppendComment);

                bool bAppendComment = false;
                bool bCommentChanged = false;   // ע���Ƿ������޸�?

                if (info != null)
                {
                    if (strExistComment != info.Comment)
                        bCommentChanged = true;
                }

                if (string.IsNullOrEmpty(strAppendComment) == false)
                    bAppendComment = true;

                AmerceItem amerceItem = new AmerceItem();
                amerceItem.ID = strID;

                if (strFunction == "amerce")
                {
                    amerceItem.NewPrice = strNewPrice;
                    if (bCommentChanged == true || bAppendComment == true)
                        amerceItem.NewComment = BuildCommitComment( 
                            bCommentChanged,
                            strExistComment,
                            bAppendComment,
                            strAppendComment);
                }
                else if (strFunction == "modifyprice")
                {
                    amerceItem.NewPrice = strNewPrice;
                    if (bCommentChanged == true || bAppendComment == true)
                        amerceItem.NewComment = BuildCommitComment(
                            bCommentChanged,
                            strExistComment,
                            bAppendComment,
                            strAppendComment);
                }
                else if (strFunction == "modifycomment")
                {
                    if (bCommentChanged == true || bAppendComment == true)
                    {
                    }
                    else
                        continue;

                    // ����Ѿ������޸ļ۸����޸�ע�͵Ĺ����Ѿ��ʹﵽ�ˣ����ﲻ������
                    if (String.IsNullOrEmpty(strNewPrice) == false)
                        continue;

                    if (bCommentChanged == true || bAppendComment == true)
                        amerceItem.NewComment = BuildCommitComment(
                            bCommentChanged,
                            strExistComment,
                            bAppendComment,
                            strAppendComment);
                }

                amerce_items.Add(amerceItem);
            }

            return amerce_items.Count;
        }

        void AmerceSubmit()
        {
            int nRet = 0;
            string strError = "";

            // �����ǲ��Ƿ��Ϸ�����Ҫ��Ľ��ѽӿ�
            if (String.IsNullOrEmpty(this.MainForm.ClientFineInterfaceName) == false)
            {
                // ע�������������Ҫ����Ҫ��ǰ�˲��ýӿڣ���Ӧ��������Ϊ��<��>��
                string strThisInterface = this.AmerceInterface;
                if (String.IsNullOrEmpty(strThisInterface) == true)
                    strThisInterface = "<��>";

                if (string.Compare(this.MainForm.ClientFineInterfaceName, "cardCenter", true) == 0)
                {
                    if (strThisInterface == "<��>")
                    {
                        strError = "Ӧ�÷�����Ҫ��ǰ�˱������ CardCenter ���ͽ��ѽӿ� '" + this.MainForm.ClientFineInterfaceName + "'��Ȼ����ǰ�˵�ǰ���õĽ��ѽӿ�Ϊ'" + this.AmerceInterface + "'";
                        goto ERROR1;
                    }

                    // TODO: �Ƿ�Ҫ�ų����Ͽ�Զ���� ���� ?
                }
                else if (this.MainForm.ClientFineInterfaceName != strThisInterface)
                {
                    strError = "Ӧ�÷�����Ҫ��ǰ�˱�����ý��ѽӿ� '" + this.MainForm.ClientFineInterfaceName + "'��Ȼ����ǰ�˵�ǰ���õĽ��ѽӿ�Ϊ'" + this.AmerceInterface + "'";
                    goto ERROR1;
                }
            }


            if (this.listView_overdues.CheckedItems.Count == 0)
            {
                strError = "��δ��ѡ�κ�Ҫ���ѵ�����";
                goto ERROR1;
            }

            List<AmerceItem> amerce_items = null;
            nRet = GetCheckedIdList(this.listView_overdues,
                "amerce",
                out amerce_items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0)
            {
                strError = "����ѡ��Ҫ���������У�û�з�������������";
                goto ERROR1;
            }

            AmerceItem[] amerce_items_param = new AmerceItem[amerce_items.Count];
            amerce_items.CopyTo(amerce_items_param);

            // ��ʾ��
            List<OverdueItemInfo> overdue_infos = null;
            nRet = GetCheckedOverdueInfos(this.listView_overdues,
                out overdue_infos,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0
                || amerce_items == null
                || amerce_items.Count == 0)
            {
                strError = "��δѡ��Ҫ���ѵ����������ѡ�������û����Ч��id";
                goto ERROR1;
            }

            // ��IC���ۿ�
            if (this.AmerceInterface == "�Ͽ�Զ��")
            {
                string strPrice = GetTotalPrice(overdue_infos);
                // return:
                //      -1  error
                //      0   canceled
                //      1   writed
                nRet = WriteDkywCard(
                    amerce_items_param,
                    overdue_infos,
                    this.textBox_readerBarcode.Text,
                    strPrice,
                    out strError);
                if (nRet == 0)
                    return; // ����

                if (nRet == -1)
                    goto ERROR1;
            }
            else if (string.IsNullOrEmpty(this.AmerceInterface) == false
                && this.AmerceInterface != "<��>")
            {
                string strPrice = GetTotalPrice(overdue_infos);
                // return:
                //      -1  error
                //      0   canceled
                //      1   writed
                nRet = WriteCardCenter(
                    this.AmerceInterface,
                    amerce_items_param,
                    overdue_infos,
                    this.textBox_readerBarcode.Text,
                    strPrice,
                    out strError);
                if (nRet == 0)
                    return; // ����

                if (nRet == -1)
                    goto ERROR1;
            }
            else
            {
                nRet = Submit(amerce_items_param,
                    overdue_infos,
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }


            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // parameters:
        //      bRefreshAll �Ƿ�Ҫ���»�ȡ�����б�?
        // return:
        //      -1  error
        //      0   succeed
        //      1   partial succeed (strError��û����Ϣ����;�Ѿ�MessageBox()������)
        internal int Submit(AmerceItem [] amerce_items,
            List<OverdueItemInfo> overdue_infos,
            bool bRefreshAll,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            bool bPartialSucceed = false;

            // this.StopFillSummary();
            StopFillAmercing(false);
            StopFillAmerced(false);

            DateTime start_time = DateTime.Now;

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ��� ���� ����: " + this.textBox_readerBarcode.Text + " ...");
            stop.BeginLoop();

            try
            {
                string strReaderXml = "";

                AmerceItem[] failed_items = null;

                long lRet = Channel.Amerce(
                    stop,
                    "amerce",
                    this.textBox_readerBarcode.Text,
                    amerce_items,
                    out failed_items,
                    out strReaderXml,
                    out strError);
                if (lRet == -1)
                {
                    /*
                    if (this.AmerceInterface == "�Ͽ�Զ��")
                    {
                        string strPrice = GetTotalPrice(overdue_infos);

                        strError += "\r\n\r\n���ǵ�ǰ���ߵ�IC���Ѿ����ۿ� " + strPrice + "������ϵ������ȡ���ô�IC���ۿ";
                    }
                     * */
                    goto ERROR1;
                }
                // ���ֳɹ�
                if (lRet == 1)
                {
                    bPartialSucceed = true;
                    MessageBox.Show(this, strError);
                    // ֻ��ӡ�ɹ��Ĳ�����
                    if (failed_items != null)
                    {
                        foreach (AmerceItem item in failed_items)
                        {
                            foreach (OverdueItemInfo info in overdue_infos)
                            {
                                if (info.ID == item.ID)
                                {
                                    overdue_infos.Remove(info);
                                    break;
                                }
                            }
                        }
                    }
                }

                DateTime end_time = DateTime.Now;

                string strReaderSummary = "";
                if (String.IsNullOrEmpty(strReaderXml) == false)
                    strReaderSummary = Global.GetReaderSummary(strReaderXml);

                string strAmerceOperator = "";
                if (this.Channel != null)
                    strAmerceOperator = this.Channel.UserName;

                List<string> ids = new List<string>();
                // Ϊ������ÿ��Ԫ�����AmerceOperator
                if (overdue_infos != null)
                {
                    foreach (OverdueItemInfo item in overdue_infos)
                    {
                        item.AmerceOperator = this.Channel.UserName;
                        ids.Add(item.ID);
                    }
                }

                this.MainForm.OperHistory.AmerceAsync(
                    this.textBox_readerBarcode.Text,
                    strReaderSummary,
                    overdue_infos,
                    strAmerceOperator,
                    start_time,
                    end_time);

                if (bRefreshAll == true)
                    ClearAllDisplay();
                else
                    ClearAllDisplay1();

                if (bRefreshAll == true)
                {
#if NO
                    // ˢ���б�?
                    nRet = FillAmercingList(strReaderXml,
        out strError);
                    if (nRet == -1)
                    {
                        strError = "FillList()��������: " + strError;
                        goto ERROR1;
                    }
#endif
                    BeginFillAmercing(strReaderXml);

                }
                else
                {
                    // ��ȥ����Щ�Ѿ����ѵ���Ŀ
                    foreach (string id in ids)
                    {
                        ListViewItem item = ListViewUtil.FindItem(this.listView_overdues,
                            id,
                            COLUMN_AMERCING_ID);
                        if (item != null)
                            this.listView_overdues.Items.Remove(item);
                    }

                    // ����ѡ����Ϣ��ʾ 2013/10/26
                    this.toolStripLabel_amercingMessage.Text = GetAmercingPriceMessage();
                }

                string strXml = "";
                string strReaderBarcode = this.textBox_readerBarcode.Text;
                // ˢ��html?
                nRet = LoadReaderHtmlRecord(ref strReaderBarcode,
                    out strXml,
                    out strError);
                if (this.textBox_readerBarcode.Text != strReaderBarcode)
                    this.textBox_readerBarcode.Text = strReaderBarcode;

                if (nRet == -1)
                {
#if NO
                    Global.SetHtmlString(this.webBrowser_readerInfo,
                        "װ�ض��߼�¼��������: " + strError);
#endif
                    this.m_webExternalHost.SetTextString("װ�ض��߼�¼��������: " + strError);
                    goto ERROR1;
                }

                if (bRefreshAll == true)
                {
#if NO
                    // ˢ��amerced
                    nRet = LoadAmercedRecords(this.textBox_readerBarcode.Text,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "LoadAmercedRecords()��������: " + strError;
                        goto ERROR1;
                    }
#endif
                    BeginFillAmerced(this.textBox_readerBarcode.Text, null);

                }
                else
                {
#if NO
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   �ҵ�������
                    nRet = LoadAmercedRecords(
                        ids,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "LoadAmercedRecords(ids)��������: " + strError;
                        goto ERROR1;
                    }
#endif
                    BeginFillAmerced("", ids);
                }

#if NO
                if (this.checkBox_fillSummary.Checked == true)
                    this.BeginFillSummary();
#endif

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            if (bPartialSucceed == true)
                return 1;

            return 0;
        ERROR1:
            return -1;
        }

        // �ع�
        // return:
        //      -2  �ع��ɹ�������ˢ����ʾʧ��
        //      -1  �ع�ʧ��
        //      0   �ɹ�
        internal int RollBack(out string strError)
        {
            // this.StopFillSummary();
            StopFillAmercing(false);
            StopFillAmerced(false);

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ��� �ع� ����");
            stop.BeginLoop();

            try
            {
                AmerceItem[] failed_items = null;

                string strReaderXml = "";
                int nRet = (int)Channel.Amerce(
                    stop,
                    "rollback",
                    "", // strReaderBarcode,
                    null,   // amerce_items,
                    out failed_items,
                    out strReaderXml,
                    out strError);
                if (nRet == -1 || nRet == 1)
                    return -1;

                ClearAllDisplay();

#if NO
                // ˢ���б�?
                nRet = FillAmercingList(strReaderXml,
    out strError);
                if (nRet == -1)
                {
                    strError = "FillList()��������: " + strError;
                    return -2;
                }
#endif
                BeginFillAmercing(strReaderXml);


                string strXml = "";
                string strReaderBarcode = this.textBox_readerBarcode.Text;
                // ˢ��html?
                nRet = LoadReaderHtmlRecord(ref strReaderBarcode,
                    out strXml,
                    out strError);
                if (this.textBox_readerBarcode.Text != strReaderBarcode)
                    this.textBox_readerBarcode.Text = strReaderBarcode;
                if (nRet == -1)
                {
#if NO
                    Global.SetHtmlString(this.webBrowser_readerInfo,
                        "װ�ض��߼�¼��������: " + strError);
#endif
                    this.m_webExternalHost.SetTextString("װ�ض��߼�¼��������: " + strError);
                    return -2;
                }

#if NO
                // ˢ��amerced
                nRet = LoadAmercedRecords(this.textBox_readerBarcode.Text,
                    out strError);
                if (nRet == -1)
                {
                    strError = "LoadAmercedRecords()��������: " + strError;
                    return -2;
                }
#endif
                BeginFillAmerced(this.textBox_readerBarcode.Text, null);

#if NO
                if (this.checkBox_fillSummary.Checked == true)
                    this.BeginFillSummary();
#endif
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

        // ���ÿ����Ľӿڽ��пۿ����
        // return:
        //      -1  error
        //      0   canceled
        //      1   writed
        int WriteCardCenter(
            string strUrl,
            AmerceItem[] AmerceItems,
            List<OverdueItemInfo> OverdueInfos,
            string strReaderBarcode,
            string strPrice,
            out string strError)
        {
            strError = "";

            AmerceCardDialog dlg = new AmerceCardDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.InterfaceUrl = strUrl;
            dlg.AmerceForm = this;
            dlg.AmerceItems = AmerceItems;
            dlg.OverdueInfos = OverdueInfos;
            dlg.CardNumber = strReaderBarcode;
            dlg.SubmitPrice = strPrice; //  PriceUtil.GetPurePrice(strPrice); // �Ƿ�Ҫȥ�����ҵ�λ?
            dlg.StartPosition = FormStartPosition.CenterScreen;
            this.MainForm.AppInfo.LinkFormState(dlg, "AmerceCardDialog_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return 0;

            return 1;
        }

        // return:
        //      -1  error
        //      0   canceled
        //      1   writed
        int WriteDkywCard(
            AmerceItem[] AmerceItems,
            List<OverdueItemInfo> OverdueInfos,
            string strReaderBarcode,
            string strPrice,
            out string strError)
        {
            strError = "";

            DkywAmerceCardDialog dlg = new DkywAmerceCardDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.AmerceForm = this;
            dlg.AmerceItems = AmerceItems;
            dlg.OverdueInfos = OverdueInfos;
            dlg.CardNumber = strReaderBarcode;
            dlg.SubmitPrice = PriceUtil.GetPurePrice(strPrice); // �Ƿ�Ҫȥ�����ҵ�λ?
            dlg.StartPosition = FormStartPosition.CenterScreen;
            this.MainForm.AppInfo.LinkFormState(dlg, "AmerceCardDialog_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return 0;

            return 1;
        }


        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.textBox_readerBarcode.Enabled = bEnable;
            this.button_load.Enabled = bEnable;

            this.listView_overdues.Enabled = bEnable;
            this.toolStripButton_amercing_selectAll.Enabled = bEnable;

            /*
            this.button_amercingOverdue_submit.Enabled = bEnable;
            this.button_amercingOverdue_modifyPrice.Enabled = bEnable;
             * */
            if (bEnable == false)
            {
                this.toolStripButton_submit.Enabled = false;
                this.toolStripButton_modifyPriceAndComment.Enabled = false;
            }
            else
            {
                SetOverduesButtonsEnable();
            }

            this.listView_amerced.Enabled = bEnable;
            this.toolStripButton_amerced_selectAll.Enabled = bEnable;

            if (bEnable == false)
            {
                this.toolStripButton_undoAmerce.Enabled = false;
            }
            else
            {
                SetAmercedButtonsEnable();
            }

        }



        // return:
        //      -1  error
        //      0   succeed
        //      1   partial succeed
        int UndoAmerce()
        {
            int nRet = 0;
            string strError = "";
            bool bPartialSucceed = false;

            // this.StopFillSummary();
            StopFillAmercing(false);
            StopFillAmerced(false);


            if (this.listView_amerced.CheckedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ���ؽ��ѵ�����";
                goto ERROR1;
            }

            // 2013/12/20
            // ��ʾ��
            List<OverdueItemInfo> overdue_infos = null;
            nRet = GetCheckedOverdueInfos(this.listView_amerced,
                out overdue_infos,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            List<AmerceItem> amerce_items = null;
            nRet = GetCheckedIdList(this.listView_amerced,
                "amerce",
                out amerce_items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0
                || amerce_items == null
                || amerce_items.Count == 0)
            {
                strError = "��ѡ���Ҫ���ؽ��ѵ����û����Ч��id";
                goto ERROR1;
            }

            EnableControls(false);

            DateTime start_time = DateTime.Now;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ��� ���ؽ��� ����: " + this.textBox_readerBarcode.Text + " ...");
            stop.BeginLoop();

            try
            {
                string strReaderXml = "";

                AmerceItem[] amerce_items_param = new AmerceItem[amerce_items.Count];
                amerce_items.CopyTo(amerce_items_param);

                AmerceItem[] failed_items = null;

                long lRet = Channel.Amerce(
                    stop,
                    "undo",
                    this.textBox_readerBarcode.Text,
                    amerce_items_param,
                    out failed_items,
                    out strReaderXml,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                // ���ֳɹ�
                if (lRet == 1)
                {
                    bPartialSucceed = true;
                    MessageBox.Show(this, strError);
                }

                DateTime end_time = DateTime.Now;

                {
                    // ֻ��ӡ�ɹ��Ĳ�����
                    if (failed_items != null)
                    {
                        foreach (AmerceItem item in failed_items)
                        {
                            foreach (OverdueItemInfo info in overdue_infos)
                            {
                                if (info.ID == item.ID)
                                {
                                    overdue_infos.Remove(info);
                                    break;
                                }
                            }
                        }
                    }

                }

                string strReaderSummary = "";
                if (String.IsNullOrEmpty(strReaderXml) == false)
                    strReaderSummary = Global.GetReaderSummary(strReaderXml);

                string strAmerceOperator = "";
                if (this.Channel != null)
                    strAmerceOperator = this.Channel.UserName;

                List<string> ids = new List<string>();
                // Ϊ������ÿ��Ԫ�����AmerceOperator
                if (overdue_infos != null)
                {
                    foreach (OverdueItemInfo item in overdue_infos)
                    {
                        item.AmerceOperator = this.Channel.UserName;
                        ids.Add(item.ID);
                    }
                }

                this.MainForm.OperHistory.AmerceAsync(
                    this.textBox_readerBarcode.Text,
                    strReaderSummary,
                    overdue_infos,
                    strAmerceOperator,
                    start_time,
                    end_time);

                ClearAllDisplay();

#if NO
                // ˢ���б�?
                nRet = FillAmercingList(strReaderXml,
    out strError);
                if (nRet == -1)
                {
                    strError = "FillList()��������: " + strError;
                    goto ERROR1;
                }
#endif
                BeginFillAmercing(strReaderXml);


                string strXml = "";
                string strReaderBarcode = this.textBox_readerBarcode.Text;
                // ˢ��html?
                nRet = LoadReaderHtmlRecord(ref strReaderBarcode,
                    out strXml,
                    out strError);
                if (this.textBox_readerBarcode.Text != strReaderBarcode)
                    this.textBox_readerBarcode.Text = strReaderBarcode;
                if (nRet == -1)
                {
#if NO
                    Global.SetHtmlString(this.webBrowser_readerInfo,
                        "װ�ض��߼�¼��������: " + strError);
#endif
                    this.m_webExternalHost.SetTextString("װ�ض��߼�¼��������: " + strError);
                    goto ERROR1;
                }

#if NO
                // ˢ��amerced
                nRet = LoadAmercedRecords(this.textBox_readerBarcode.Text,
                    out strError);
                if (nRet == -1)
                {
                    strError = "LoadAmercedRecords()��������: " + strError;
                    goto ERROR1;
                }
#endif
                BeginFillAmerced(this.textBox_readerBarcode.Text, null);

#if NO
                if (this.checkBox_fillSummary.Checked == true)
                    this.BeginFillSummary();
#endif

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            if (bPartialSucceed == true)
                return 1;

            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        private void listView_amerced_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            this.toolStripLabel_amercedMessage.Text = GetAmercedPriceMessage();

            /*
            string strError = "";
            List<AmerceItem> amerce_items = null;
            int nRet = GetCheckedIdList(this.listView_amerced,
                out amerce_items,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }


            if (amerce_items == null || amerce_items.Count == 0)
                this.button_amerced_undoAmerce.Enabled = false;
            else
                this.button_amerced_undoAmerce.Enabled = true;
            */
            this.SetAmercedButtonsEnable();

            ResetAmercedItemsBackColor(this.listView_amerced);
        }

        static void ResetAmercingItemsBackColor(ListView list)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];

                if (item.Checked == false)
                    item.BackColor = SystemColors.Window;
                else
                    item.BackColor = Color.Yellow;
            }
        }

        static void ResetAmercedItemsBackColor(ListView list)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];

                if (item.Checked == false)
                {
                    string strState = ListViewUtil.GetItemText(item,
COLUMN_AMERCED_STATE);
                    if (strState == "settlemented")
                    {
                        item.ForeColor = SystemColors.GrayText;
                        item.ImageIndex = ITEMTYPE_OLD_SETTLEMENTED;    // ȫ��������ǰsettlemented��
                    }
                    else if (strState == "error")
                    {
                        item.ForeColor = SystemColors.GrayText;
                        item.ImageIndex = ITEMTYPE_ERROR;    // �������
                    }
                    else
                    {
                        item.ForeColor = SystemColors.WindowText;
                        item.ImageIndex = ITEMTYPE_AMERCED;
                    }

                    item.BackColor = SystemColors.Window;
                }
                else
                    item.BackColor = Color.Yellow;
            }
        }

        private void textBox_readerBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_load;
            this.MainForm.EnterPatronIdEdit(InputType.PQR);
        }

        private void textBox_readerBarcode_Leave(object sender, EventArgs e)
        {
            this.AcceptButton = null;
            this.MainForm.LeavePatronIdEdit();
        }

        private void AmerceForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        // �������˵�
        private void listView_overdues_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("�쿴��¼����(&V)");
            menuItem.Click += new System.EventHandler(this.menu_viewAmercing_Click);
            if (this.listView_overdues.SelectedItems.Count != 1)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("������(&M)");
            menuItem.Click += new System.EventHandler(this.menu_modifyPrice_Click);
            if (this.listView_overdues.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("���ע��(&C)");
            menuItem.Click += new System.EventHandler(this.menu_modifyComment_Click);
            if (this.listView_overdues.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView_overdues, new Point(e.X, e.Y));		
        }

        void menu_viewAmercing_Click(object sender, EventArgs e)
        {
            DoViewOperlog(true);
        }


        // ���۸��ע�ͺϳ�Ϊһ���ַ���
        // ע��û�и�ǰ������*����
        static string MergePriceCommentString(string strPrice,
            string strNewComment)
        {
            if (String.IsNullOrEmpty(strNewComment) == false)
                return strPrice + "|" + strNewComment;

            return strPrice;
        }

#if NO
        // �Ӻϳɵ�(�۸�+ע��)�ַ�����������������
        // ����ȥ���۸��ַ���ͷ����*����
        // 2007/4/19 new add
        static void ParsePriceCommentString(string strText,
            bool bClearChangedChar,
            out string strPrice,
            out string strComment)
        {
            strPrice = "";
            strComment = "";

            int nRet = strText.IndexOf("|");
            if (nRet != -1)
            {
                strComment = strText.Substring(nRet + 1);
                strPrice = strText.Substring(0, nRet);
            }
            else
            {
                strPrice = strText;
            }

            if (bClearChangedChar == true)
            {
                // ȥ���۸��ַ���ͷ����*����
                if (strPrice.Length > 0 && strPrice[0] == '*')
                    strPrice = strPrice.Substring(1);
                // ȥ��ע���ַ���ͷ����<����>����
                if (strComment.Length > 0 &&
                    (strComment[0] == '<' || strComment[0] == '>'))
                    strComment = strComment.Substring(1);

            }

        }

#endif

        // �޸�һ������Ľ��
        void menu_modifyPrice_Click(object sender, EventArgs e)
        {
            if (this.listView_overdues.SelectedIndices.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫ����������");
                return;
            }

            if (this.listView_overdues.SelectedIndices.Count > 1)
            {
                MessageBox.Show(this, "ÿ��ֻ��ѡ��һ���������");
                return;
            }

            int index = this.listView_overdues.SelectedIndices[0];
            ListViewItem item = listView_overdues.Items[index];

            AmercingItemInfo info = (AmercingItemInfo)item.Tag;
            Debug.Assert(info != null, "");

            string strID = ListViewUtil.GetItemText(item, COLUMN_AMERCING_ID);  // .SubItems[8].Text;


#if NO
            string strPriceComment = ListViewUtil.GetItemText(item, COLUMN_AMERCING_PRICE);  // listView_overdues.Items[index].SubItems[2].Text;


            // ��strPrice�ַ���������ע���ַ���
            // 2007/4/19 changed
            string strNewComment = "";
            string strPrice = "";

            /*
            int nRet = strPrice.IndexOf("|");
            if (nRet != -1)
            {
                strComment = strPrice.Substring(nRet + 1);
                strPrice = strPrice.Substring(0, nRet);
            }

            // ȥ���۸��ַ���ͷ����*����
            if (strPrice.Length > 0 && strPrice[0] == '*')
                strPrice = strPrice.Substring(1);
             * */
            ParsePriceCommentString(strPriceComment,
                true,
                out strPrice,
                out strNewComment);
#endif
            string strComment = ListViewUtil.GetItemText(item, COLUMN_AMERCING_COMMENT); // listView_overdues.Items[index].SubItems[3].Text;

            string strExistComment = "";
            string strAppendComment = "";
            ParseCommentString(strComment,
                out strExistComment,
                out strAppendComment);

            ModifyPriceDlg dlg = new ModifyPriceDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.ID = strID;
            dlg.OldPrice = RemoveChangedMask(ListViewUtil.GetItemText(item, COLUMN_AMERCING_PRICE)); // strPrice;
            dlg.Price = RemoveChangedMask(ListViewUtil.GetItemText(item, COLUMN_AMERCING_PRICE));
            dlg.Comment = strExistComment;
            dlg.AppendComment = strAppendComment;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

#if NO
            // ���۸��ע�ͺϳ�Ϊһ���ַ���
            string strNewPrice = "";

            if (strPrice != dlg.Price)
                strNewPrice = "*" + dlg.Price;
            else
                strNewPrice = dlg.Price;

            string strNewText = MergePriceCommentString(strNewPrice,
                 ">"+dlg.NewComment);

            // listView_overdues.Items[index].SubItems[2].Text = strNewText;
            ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_PRICE, strNewText);
#endif
            if (info.Price != dlg.Price)
                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_PRICE, "*" + dlg.Price);
            else
                ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_PRICE, dlg.Price);

            if (info.Comment != dlg.Comment)
                strComment = "<" + dlg.Comment;
            else
                strComment = dlg.Comment;

            if (string.IsNullOrEmpty(dlg.AppendComment) == false)
                strComment += ">" + dlg.AppendComment;    // TODO: �ƺ�����������޸�Ҳ׷��

            ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_COMMENT, strComment);
            item.Checked = true;    // ˳�㹴ѡ���������
        }

        // ȥ���۸��ַ���ǰ����ʾ�޸ĵ�*�ַ�
        static string RemoveChangedMask(string strPrice)
        {
            // ���Ǻŵ��Ǳ����Ҫȥ���Ǻ�
            if (strPrice.Length > 0 && strPrice[0] == '*')
            {
                return strPrice.Substring(1);
            }

            return strPrice;
        }

        // ����listview��Ŀ�е�ע���ַ���
        // parameters:
        //      strText ���������ַ�����Ϊ <comment>appendcomment ���� <comment ���� comment ���� >appendcomment
        static void ParseCommentString(string strText,
            out string strComment,
            out string strAppendComment)
        {
            strComment = "";
            strAppendComment = "";

            int nRet = strText.IndexOf(">");
            if (nRet == -1)
                strComment = strText;
            else
            {
                strComment = strText.Substring(0, nRet);
                strAppendComment = strText.Substring(nRet + 1);
            }

            if (strComment.Length > 0 && strComment[0] == '<')
                strComment = strComment.Substring(1);
        }

        void menu_modifyComment_Click(object sender, EventArgs e)
        {
            if (this.listView_overdues.SelectedIndices.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫ���ע�͵����");
                return;
            }

            if (this.listView_overdues.SelectedIndices.Count > 1)
            {
                MessageBox.Show(this, "ÿ��ֻ��ѡ��һ�������ע�͡�");
                return;
            }

            int index = this.listView_overdues.SelectedIndices[0];
            ListViewItem item = this.listView_overdues.Items[index];

            AmercingItemInfo info = (AmercingItemInfo)item.Tag;
            Debug.Assert(info != null, "");

            string strID = ListViewUtil.GetItemText(item, COLUMN_AMERCING_ID);  // listView_overdues.Items[index].SubItems[8].Text;

#if NO
            string strPriceComment = ListViewUtil.GetItemText(item, COLUMN_AMERCING_PRICE); // listView_overdues.Items[index].SubItems[2].Text;
            string strOldComment = ListViewUtil.GetItemText(item, COLUMN_AMERCING_COMMENT); // listView_overdues.Items[index].SubItems[3].Text;

            // ��strPrice�ַ���������ע���ַ���
            string strNewComment = "";
            string strPrice = "";

            ParsePriceCommentString(strPriceComment,
                false,
                out strPrice,
                out strNewComment);

            bool bAppend = true;

            // ����û���޸Ĺ���֤���ǣ�strNewComment��û���κη��ţ����޸Ĺ����з��ŵ�
            if (String.IsNullOrEmpty(strNewComment) == true)
            {
                bAppend = true; // 2008/6/24 new changed
                strNewComment = ""; // 2008/6/24 new changed
            }
            else if (String.IsNullOrEmpty(strNewComment) == false
                && strNewComment[0] == '<')
            {
                bAppend = false;
                strNewComment = strNewComment.Substring(1);
            }
            else if (String.IsNullOrEmpty(strNewComment) == false
                && strNewComment[0] == '>')
            {
                bAppend = true;
                strNewComment = strNewComment.Substring(1);
            }
#endif

            bool bAppend = true;
            string strComment = ListViewUtil.GetItemText(item, COLUMN_AMERCING_COMMENT); // listView_overdues.Items[index].SubItems[3].Text;

            if (String.IsNullOrEmpty(strComment) == false
                && strComment[0] == '<')
            {
                bAppend = false;
            }
            else if (String.IsNullOrEmpty(strComment) == false
                && strComment[0] == '>')
            {
                bAppend = true;
            }

            string strExistComment = "";
            string strAppendComment = "";
            ParseCommentString(strComment,
                out strExistComment,
                out strAppendComment);

            ModifyCommentDialog dlg = new ModifyCommentDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.ID = strID;
            dlg.Price = RemoveChangedMask(ListViewUtil.GetItemText(item, COLUMN_AMERCING_PRICE));
            dlg.IsAppend = bAppend;
            dlg.OriginOldComment = info.Comment;

            dlg.Comment = strExistComment;
            dlg.AppendComment = strAppendComment;

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

#if NO
            // ���۸��ע�ͺϳ�Ϊһ���ַ���
            string strNewText = "";

            bAppend = dlg.IsAppend;
            
            if (bAppend == true)
                strNewText = MergePriceCommentString(strPrice,
                    ">" + dlg.AppendComment);
            else
                strNewText = MergePriceCommentString(strPrice,
                    "<" + dlg.Comment);

            // listView_overdues.Items[index].SubItems[2].Text = strNewText;
            ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_PRICE, strNewText);
#endif

            if (info.Comment != dlg.Comment)
                strComment = "<" + dlg.Comment;
            else
                strComment = dlg.Comment;

            if (string.IsNullOrEmpty(dlg.AppendComment) == false)
                strComment += ">" + dlg.AppendComment;    // TODO: �ƺ�����������޸�Ҳ׷��

            ListViewUtil.ChangeItemText(item, COLUMN_AMERCING_COMMENT, strComment);
            item.Checked = true;    // ˳�㹴ѡ���������
        }



        // ֻ�޸Ľ���ע��
        // return:
        //      -1  error
        //      0   succeed
        //      1   partial succeed
        int ModifyPriceAndComment()
        {
            int nRet = 0;
            string strError = "";
            bool bPartialSucceed = false;

            if (this.listView_overdues.CheckedItems.Count == 0)
            {
                strError = "��δ��ѡ�κ�����";
                goto ERROR1;
            }

            // this.StopFillSummary();
            StopFillAmercing(false);
            StopFillAmerced(false);

            List<AmerceItem> modifyprice_items = null;
            nRet = GetCheckedIdList(this.listView_overdues,
                "modifyprice",
                out modifyprice_items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            Debug.Assert(modifyprice_items != null, "");

            List<AmerceItem> modifycomment_items = null;
            nRet = GetCheckedIdList(this.listView_overdues,
                "modifycomment",
                out modifycomment_items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            Debug.Assert(modifycomment_items != null, "");

            /*
            List<AmerceItem> appendcomment_items = null;
            nRet = GetCheckedIdList(this.listView_overdues,
                "appendcomment",
                out appendcomment_items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            Debug.Assert(appendcomment_items != null, "");

             * */
            if (modifyprice_items.Count + modifycomment_items.Count/* + appendcomment_items.Count */ == 0)
            {
                strError = "����ѡ�������У�û���κη������۸��ע��׷��/�޸ĵ�����";
                goto ERROR1;
            }

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ��� �޸Ľ��/ע�� �Ĳ���: " + this.textBox_readerBarcode.Text + " ...");
            stop.BeginLoop();

            try
            {
                string strReaderXml = "";

                if (modifyprice_items.Count > 0)
                {
                    AmerceItem[] amerce_items_param = new AmerceItem[modifyprice_items.Count];
                    modifyprice_items.CopyTo(amerce_items_param);
                    AmerceItem[] failed_items = null;

                    long lRet = Channel.Amerce(
                        stop,
                        "modifyprice",
                        this.textBox_readerBarcode.Text,
                        amerce_items_param,
                        out failed_items,
                        out strReaderXml,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;
                    // ���ֳɹ�
                    if (lRet == 1)
                    {
                        bPartialSucceed = true;
                        MessageBox.Show(this, strError);
                    }
                }

                /*
                if (appendcomment_items.Count > 0)
                {
                    AmerceItem[] amerce_items_param = new AmerceItem[appendcomment_items.Count];
                    appendcomment_items.CopyTo(amerce_items_param);

                    long lRet = Channel.Amerce(
                        stop,
                        "appendcomment",
                        this.textBox_readerBarcode.Text,
                        amerce_items_param,
                        out strReaderXml,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;
                }*/

                // ��Ҫ��Ȩ�޸�һЩ����˷��ں���
                if (modifycomment_items.Count > 0)
                {
                    AmerceItem[] amerce_items_param = new AmerceItem[modifycomment_items.Count];
                    modifycomment_items.CopyTo(amerce_items_param);
                    AmerceItem[] failed_items = null;

                    long lRet = Channel.Amerce(
                        stop,
                        "modifycomment",
                        this.textBox_readerBarcode.Text,
                        amerce_items_param,
                        out failed_items,
                        out strReaderXml,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;
                }

                ClearHtmlAndAmercingDisplay();

#if NO
                // ˢ���б�?
                nRet = FillAmercingList(strReaderXml,
    out strError);
                if (nRet == -1)
                {
                    strError = "FillList()��������: " + strError;
                    goto ERROR1;
                }
#endif
                BeginFillAmercing(strReaderXml);


                string strXml = "";
                string strReaderBarcode = this.textBox_readerBarcode.Text;
                // ˢ��html?
                nRet = LoadReaderHtmlRecord(ref strReaderBarcode,
                    out strXml,
                    out strError);
                if (this.textBox_readerBarcode.Text != strReaderBarcode)
                    this.textBox_readerBarcode.Text = strReaderBarcode;
                if (nRet == -1)
                {
#if NO
                    Global.SetHtmlString(this.webBrowser_readerInfo,
                        "װ�ض��߼�¼��������: " + strError);
#endif
                    this.m_webExternalHost.SetTextString("װ�ض��߼�¼��������: " + strError);
                    goto ERROR1;
                }

                /*
                // ˢ��amerced
                nRet = LoadAmercedRecords(this.textBox_readerBarcode.Text,
                    out strError);
                if (nRet == -1)
                {
                    strError = "LoadAmercedRecords()��������: " + strError;
                    goto ERROR1;
                }
                 * */

#if NO
                if (this.checkBox_fillSummary.Checked == true)
                    this.BeginFillSummary();
#endif

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            if (bPartialSucceed == true)
                return 1;
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        void SetOverduesButtonsEnable()
        {
            if (this.listView_overdues.CheckedItems.Count == 0)
            {
                this.toolStripButton_submit.Enabled = false;
                this.toolStripButton_modifyPriceAndComment.Enabled = false;
            }
            else
            {
                this.toolStripButton_submit.Enabled = true;
                this.toolStripButton_modifyPriceAndComment.Enabled = true;
            }

        }

        void SetAmercedButtonsEnable()
        {
            if (this.listView_amerced.CheckedItems.Count == 0)
                this.toolStripButton_undoAmerce.Enabled = false;
            else
                this.toolStripButton_undoAmerce.Enabled = true;
        }

        private void listView_overdues_SelectedIndexChanged(object sender, EventArgs e)
        {
            DoViewOperlog(false);
        }

        private void listView_amerced_SelectedIndexChanged(object sender, EventArgs e)
        {
            DoViewOperlog(false);
        }


        // 
        /// <summary>
        /// ��ӡ�軹��ΥԼ��ƾ��
        /// </summary>
        public void Print()
        {
            // ������ʷ����
            this.MainForm.OperHistory.Print();
        }

        /// <summary>
        /// ���ѽӿ������ַ���
        /// </summary>
        public string AmerceInterface
        {
            get
            {
                // amerce
                return this.MainForm.AppInfo.GetString("config",
                    "amerce_interface",
                    "<��>");
            }
        }

        private void listView_amerced_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewUtil.OnColumnClick(this.listView_amerced, e);
        }

        private void listView_overdues_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewUtil.OnColumnClick(this.listView_overdues, e);
        }

        private void button_beginFillSummary_Click(object sender, EventArgs e)
        {
            // BeginFillSummary();
        }

        private void checkBox_fillSummary_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_fillSummary.Checked == true)
            {
                // BeginFillSummary();
            }
        }

        private void webBrowser_readerInfo_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            ((WebBrowser)sender).Document.Window.Error -= new HtmlElementErrorEventHandler(Window_Error);
            ((WebBrowser)sender).Document.Window.Error += new HtmlElementErrorEventHandler(Window_Error);
        }

        void Window_Error(object sender, HtmlElementErrorEventArgs e)
        {
            if (this.MainForm.SuppressScriptErrors == true)
                e.Handled = true;
        }

        void DoViewOperlog(bool bOpenWindow)
        {
            string strError = "";
            string strHtml = "";
            string strXml = "";
            int nRet = 0;

            // �Ż���������ν�ؽ��з���������
            if (bOpenWindow == false)
            {
                if (this.MainForm.PanelFixedVisible == false
                    && (m_operlogViewer == null || m_operlogViewer.Visible == false))
                    return;
            }

            ListView list = null;
            if (this.listView_amerced.Focused == true)
                list = this.listView_amerced;
            else if (this.listView_overdues.Focused == true)
                list = this.listView_overdues;
            else
                list = null;

            if (list == null || list.SelectedItems.Count != 1)
            {
                // 2012/10/2
                if (this.m_operlogViewer != null)
                    this.m_operlogViewer.Clear();

                return;
            }

            ListViewItem item = list.SelectedItems[0];
            string strTitle = "";
            {

                // ���������������ݵ� HTML �ַ���
                nRet = GetHtmlString(item,
                    out strTitle,
                    out strHtml,
                    out strXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            bool bNew = false;
            if (this.m_operlogViewer == null
                || (bOpenWindow == true && this.m_operlogViewer.Visible == false))
            {
                m_operlogViewer = new CommentViewerForm();
                MainForm.SetControlFont(m_operlogViewer, this.Font, false);
                m_operlogViewer.SuppressScriptErrors = this.MainForm.SuppressScriptErrors;
                bNew = true;
            }

            m_operlogViewer.MainForm = this.MainForm;  // �����ǵ�һ��

            if (bNew == true)
                m_operlogViewer.InitialWebBrowser();

            m_operlogViewer.Text = strTitle;
            m_operlogViewer.HtmlString = (string.IsNullOrEmpty(strHtml) == true ? NOTSUPPORT : strHtml);
            m_operlogViewer.XmlString = strXml;
            m_operlogViewer.FormClosed -= new FormClosedEventHandler(m_viewer_FormClosed);
            m_operlogViewer.FormClosed += new FormClosedEventHandler(m_viewer_FormClosed);

            if (bOpenWindow == true)
            {
                if (m_operlogViewer.Visible == false)
                {
                    this.MainForm.AppInfo.LinkFormState(m_operlogViewer, "operlog_viewer_state");
                    m_operlogViewer.Show(this);
                    m_operlogViewer.Activate();

                    this.MainForm.CurrentPropertyControl = null;
                }
                else
                {
                    if (m_operlogViewer.WindowState == FormWindowState.Minimized)
                        m_operlogViewer.WindowState = FormWindowState.Normal;
                    m_operlogViewer.Activate();
                }
            }
            else
            {
                if (m_operlogViewer.Visible == true)
                {

                }
                else
                {
                    if (this.MainForm.CurrentPropertyControl != m_operlogViewer.MainControl)
                        m_operlogViewer.DoDock(false); // �����Զ���ʾFixedPanel
                }
            }
            return;
        ERROR1:
            MessageBox.Show(this, "DoViewOperlog() ����: " + strError);
        }

        void m_viewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_operlogViewer != null)
            {
                this.MainForm.AppInfo.UnlinkFormState(m_operlogViewer);
                this.m_operlogViewer = null;
            }
        }

        int GetHtmlString(ListViewItem item,
                    out string strTitle,
                    out string strHtml,
                    out string strXml,
                    out string strError)
        {
            strTitle = "";
            strHtml = "";
            strXml = "";
            strError = "";
            int nRet = 0;

            if (item.ListView == this.listView_amerced)
            {
                strTitle = "�ѽ��Ѽ�¼ " + ListViewUtil.GetItemText(item, COLUMN_AMERCED_ID);

                // �ѽ���
                AmercedItemInfo info = (AmercedItemInfo)item.Tag;
                if (info == null)
                    return 0;
                strXml = info.Xml;

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "ΥԼ���¼XMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                nRet = GetAmerceInfoString(dom, out strHtml, out strError);

                if (nRet == -1)
                    return -1;

                {
                    if (string.IsNullOrEmpty(strHtml) == true)
                        return 0;
                    strHtml = "<html>" +
                        GetHeadString() +
                        "<body>" +
                        strHtml +
                        "</body></html>";
                }
            }
            else
            {
                Debug.Assert(item.ListView == this.listView_overdues, "");

                strTitle = "���������� " + ListViewUtil.GetItemText(item, COLUMN_AMERCING_ID);

                // ��δ����
                AmercingItemInfo info = (AmercingItemInfo)item.Tag;
                if (info == null)
                    return 0;
                strXml = info.Xml;

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "��������ϢXMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                nRet = GetOverdueInfoString(dom.DocumentElement,
                    true,
                    out strHtml, out strError);

                if (nRet == -1)
                    return -1;

                {
                    if (string.IsNullOrEmpty(strHtml) == true)
                        return 0;
                    strHtml = "<html>" +
                        GetHeadString() +
                        "<body>" +
                        strHtml +
                        "</body></html>";
                }

            }

            return 0;
        }

        string GetHeadString(bool bAjax = true)
        {
            string strCssFilePath = PathUtil.MergePath(this.MainForm.DataDir, "amercehtml.css");

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

        // ���<overdue>Ԫ�ض�Ӧ�� HTML �ַ����������������<html><body>
        /// <summary>
        /// ��� overdue Ԫ�ض�Ӧ�� HTML �ַ���
        /// </summary>
        /// <param name="root">XML ���ڵ� XmlNode ����</param>
        /// <param name="bSummary">�Ƿ������ĿժҪ</param>
        /// <param name="strHtml">���� HTML �ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public static int GetOverdueInfoString(XmlNode root,
            bool bSummary,
            out string strHtml,
            out string strError)
        {
            strHtml = "";
            strError = "";

            string strItemBarcode = DomUtil.GetAttr(root, "barcode");
            string strItemRecPath = DomUtil.GetAttr(root, "recPath");
            string strReason = DomUtil.GetAttr(root, "reason");
            string strOverduePeriod = DomUtil.GetAttr(root, "overduePeriod");
            string strPrice = DomUtil.GetAttr(root, "price");
            string strBorrowDate = OperLogForm.GetRfc1123DisplayString(
                DomUtil.GetAttr(root, "borrowDate"));
            string strBorrowPeriod = DomUtil.GetAttr(root, "borrowPeriod");
            string strBorrowOperator = DomUtil.GetAttr(root, "borrowOperator");


            string strReturnDate = OperLogForm.GetRfc1123DisplayString(
    DomUtil.GetAttr(root, "returnDate"));
            string strReturnOperator = DomUtil.GetAttr(root, "operator");

            string strID = DomUtil.GetAttr(root, "id");

            string strComment = DomUtil.GetAttr(root, "comment");

            strHtml =
                "<table class='overdueinfo'>" +
                OperLogForm.BuildHtmlEncodedLine("�������", OperLogForm.BuildItemBarcodeLink(strItemBarcode)) +
                (bSummary == true ? OperLogForm.BuildHtmlPendingLine("(��ĿժҪ)", BuildPendingBiblioSummary(strItemBarcode, "")) : "") +
                OperLogForm.BuildHtmlLine("ID", strID) +
                OperLogForm.BuildHtmlLine("ԭ��", strReason) +
                OperLogForm.BuildHtmlLine("����", strOverduePeriod) +
                OperLogForm.BuildHtmlLine("���", strPrice) +
                OperLogForm.BuildHtmlLine("ע��", strComment) +

                OperLogForm.BuildHtmlLine("��������", strBorrowOperator) +
                OperLogForm.BuildHtmlLine("�������", strBorrowDate) +
                OperLogForm.BuildHtmlLine("����", strBorrowPeriod) +

                OperLogForm.BuildHtmlLine("�յ������", strReturnOperator) +
                OperLogForm.BuildHtmlLine("�յ�����", strReturnDate) +

                "</table>";

            return 0;
        }

        // ���ΥԼ���¼ HTML �ַ����������������<html><body>
        static int GetAmerceInfoString(XmlDocument amerce_dom,
out string strHtml,
out string strError)
        {
            strHtml = "";
            strError = "";

            XmlNode node = null;
            string strLibraryCode = DomUtil.GetElementText(amerce_dom.DocumentElement, "libraryCode", out node);
            if (node != null && string.IsNullOrEmpty(strLibraryCode) == true)
                strLibraryCode = "<��>";
            string strItemBarcode = DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "itemBarcode");
            string strReaderBarcode = DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "readerBarcode");
            string strState = DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "state");
            string strID = DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "id");
            string strReason = DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "reason");
            string strOverduePeriod = DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "overduePeriod");

            string strPrice = DomUtil.GetElementInnerXml(amerce_dom.DocumentElement, "price");
            string strComment = DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "comment");

            string strBorrowDate = OperLogForm.GetRfc1123DisplayString(
                DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "borrowDate"));
            string strBorrowPeriod = DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "borrowPeriod");
            string strBorrowOperator = DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "borrowOperator");
            string strReturnDate = OperLogForm.GetRfc1123DisplayString(
    DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "returnDate"));
            string strReturnOperator = DomUtil.GetElementInnerText(amerce_dom.DocumentElement, "returnOperator");

            string strOperator = DomUtil.GetElementText(amerce_dom.DocumentElement, "operator");
            string strOperTime = OperLogForm.GetRfc1123DisplayString(
                DomUtil.GetElementText(amerce_dom.DocumentElement, "operTime"));

            string strSettlementOperator = DomUtil.GetElementText(amerce_dom.DocumentElement, "settlementOperator");
            string strSettlementOperTime = OperLogForm.GetRfc1123DisplayString(
                DomUtil.GetElementText(amerce_dom.DocumentElement, "settlementOperTime"));

            string strUndoSettlementOperator = DomUtil.GetElementText(amerce_dom.DocumentElement, "undoSettlementOperator");
            string strUndoSettlementOperTime = OperLogForm.GetRfc1123DisplayString(
                DomUtil.GetElementText(amerce_dom.DocumentElement, "undoSettlementOperTime"));

            strHtml =
                "<table class='amerceinfo'>" +
                OperLogForm.BuildHtmlLine("�ݴ���", strLibraryCode) +
                OperLogForm.BuildHtmlEncodedLine("�������", OperLogForm.BuildItemBarcodeLink(strItemBarcode)) +
                OperLogForm.BuildHtmlPendingLine("(��ĿժҪ)", BuildPendingBiblioSummary(strItemBarcode, "")) +
                OperLogForm.BuildHtmlEncodedLine("����֤�����", OperLogForm.BuildReaderBarcodeLink(strReaderBarcode)) +
                OperLogForm.BuildHtmlPendingLine("(����ժҪ)", BuildPendingReaderSummary(strReaderBarcode)) +
                OperLogForm.BuildHtmlLine("״̬", strState) +
                OperLogForm.BuildHtmlLine("ID", strID) +
                OperLogForm.BuildHtmlLine("ԭ��", strReason) +
                OperLogForm.BuildHtmlLine("����", strOverduePeriod) +
                OperLogForm.BuildHtmlLine("���", strPrice) +
                OperLogForm.BuildHtmlLine("ע��", strComment) +

                OperLogForm.BuildHtmlLine("��������", strBorrowOperator) +
                OperLogForm.BuildHtmlLine("�������", strBorrowDate) +
                OperLogForm.BuildHtmlLine("����", strBorrowPeriod) +

                OperLogForm.BuildHtmlLine("�յ������", strReturnOperator) +
                OperLogForm.BuildHtmlLine("�յ�����", strReturnDate) +

                OperLogForm.BuildHtmlLine("��ȡΥԼ�������", strOperator) +
                OperLogForm.BuildHtmlLine("��ȡΥԼ�����ʱ��", strOperTime) +

                OperLogForm.BuildHtmlLine("���������", strSettlementOperator) +
                OperLogForm.BuildHtmlLine("�������ʱ��", strSettlementOperTime) +

                OperLogForm.BuildHtmlLine("�������������", strUndoSettlementOperator) +
                OperLogForm.BuildHtmlLine("�����������ʱ��", strUndoSettlementOperTime) +
                "</table>";

            return 0;
        }

        static string BuildPendingBiblioSummary(string strItemBarcode,
    string strItemRecPath)
        {
            if (string.IsNullOrEmpty(strItemBarcode) == true)
                return "";
            string strCommand = "B:" + strItemBarcode + "|" + strItemRecPath;
            return strCommand;
        }

        static string BuildPendingReaderSummary(string strReaderBarcode)
        {
            if (string.IsNullOrEmpty(strReaderBarcode) == true)
                return "";
            string strCommand = "P:" + strReaderBarcode;
                return strCommand;
        }

        private void listView_amerced_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("�쿴��¼����(&V)");
            menuItem.Click += new System.EventHandler(this.menu_viewAmercing_Click);
            if (this.listView_amerced.SelectedItems.Count != 1)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView_amerced, new Point(e.X, e.Y));	
        }

        private void toolStripButton_amerced_selectAll_Click(object sender, EventArgs e)
        {
            SelectAll(this.listView_amerced);
        }

        private void toolStripButton_undoAmerce_Click(object sender, EventArgs e)
        {
            this.toolStripButton_undoAmerce.Enabled = false;

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_UNDO_AMERCE);
        }

        private void toolStripButton_amercing_selectAll_Click(object sender, EventArgs e)
        {
            SelectAll(this.listView_overdues);

        }

        // ֻ�޸Ľ���ע��
        private void toolStripButton_modifyPriceAndComment_Click(object sender, EventArgs e)
        {
            this.toolStripButton_modifyPriceAndComment.Enabled = false;

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_MODIFY_PRICE_AND_COMMENT);

        }

        private void toolStripButton_submit_Click(object sender, EventArgs e)
        {
            this.toolStripButton_submit.Enabled = false;

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_AMERCE);
        }
    }

    class AmercingItemInfo
    {
        public string Price = "";
        public string Comment = "";
        public string Xml = ""; // ���node��OuterXml��Ҳ����<overdue>Ԫ��XMLƬ��
    }

    class AmercedItemInfo
    {
        public string Xml = ""; // ΥԼ���¼XML
    }

    class FillAmercingParam
    {
        public string Xml = ""; // [in]���߼�¼
        public bool FillSummary = true;
    }

    class FillAmercedParam
    {
        public string ReaderBarcode = "";
        public List<string> IDs = null;
        public bool FillSummary = true;
    }
}