// #define _TEST_PINYIN

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Reflection;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Marc;
using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.CommonControl;
using DigitalPlatform.MarcDom;
using DigitalPlatform.GcatClient;

using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.GcatClient.gcat_new_ws;
using DigitalPlatform.CommonDialog;

namespace dp2Circulation
{
    /// <summary>
    /// �ֲᴰ
    /// </summary>
    public partial class EntityForm : MyForm
    {
        // ģ���������ݰ汾��
        int _templateVersion = 0;

        // MARC ��������ݰ汾��
        int _marcEditorVersion = 0;

        CommentViewerForm m_commentViewer = null;

        WebExternalHost m_webExternalHost_biblio = new WebExternalHost();
        // WebExternalHost m_webExternalHost_comment = new WebExternalHost();

        // �洢��Ŀ��<dprms:file>���������XMLƬ��
        XmlDocument domXmlFragment = null;

        VerifyViewerForm m_verifyViewer = null;

        GenerateDataForm m_genDataViewer = null;

        List<PendingLoadRequest> m_listPendingLoadRequest = new List<PendingLoadRequest>();

        SelectedTemplateCollection selected_templates = new SelectedTemplateCollection();

        int m_nChannelInUse = 0; // >0��ʾͨ�����ڱ�ʹ��

        /// <summary>
        /// �Ƿ�����װ�ؼ�¼����;
        /// </summary>
        public bool IsLoading
        {
            get
            {
                if (this.m_nChannelInUse > 0)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Ŀ���¼·��
        /// </summary>
        public string TargetRecPath
        {
            get
            {
                return this.orderControl1.TargetRecPath;
            }
            set
            {
                this.orderControl1.TargetRecPath = value;
                this.issueControl1.TargetRecPath = value;
            }
        }

        /// <summary>
        /// �Ƿ�Ϊ����ģʽ
        /// </summary>
        public bool AcceptMode
        {
            get
            {
                return this._bAcceptMode;
            }
            set
            {
                this._bAcceptMode = value;
#if ACCEPT_MODE
                this.SupressSizeSetting = value;
#endif
            }
        }

        bool _bAcceptMode = false; // �Ƿ�Ϊ����ģʽ ����ǣ���һ����ʩ������Ϊ�������������յ�״̬�������Ϊ��ͨ״̬

        MacroUtil m_macroutil = new MacroUtil();   // �괦����

        bool m_bDeletedMode = false;    // �Ƿ��ڸ�ɾ�����������Ŀ��ʵ����Ϣ�����ǲ��ñ༭������״̬

        string m_strOriginBiblioXml = ""; // ��������ݿ��ģ���е����XML��Ŀ����

        string BiblioOriginPath = "";   // ��Ŀ��¼�����ݿ��е�ԭʼ·��

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// ֹͣ����
        /// </summary>
        public DigitalPlatform.Stop Stop = null;
#endif

        // BookItemCollection bookitems = null;

        // string m_strBiblioRecPath = ""; // �������е��ּ�¼·��

        // BiblioDbFromInfo[] DbFromInfos = null;

        BrowseSearchResultForm browseWindow = null; 

        int m_nInSearching = 0;
        // string m_strTempBiblioRecPath = "";

        RegisterType m_registerType = RegisterType.Register;

        // const int WM_PREPARE = API.WM_USER + 200;
        const int WM_SWITCH_FOCUS = API.WM_USER + 201;
        // const int WM_LOADLAYOUT = API.WM_USER + 202;
        const int WM_SEARCH_DUP = API.WM_USER + 203;
        const int WM_VERIFY_DATA = API.WM_USER + 204;
        const int WM_FILL_MARCEDITOR_SCRIPT_MENU = API.WM_USER + 205;

        // ��ϢWM_SWITCH_FOCUS��wparam����ֵ
        const int BIBLIO_SEARCHTEXT = 0;
        const int ITEM_BARCODE = 1;
        const int MARC_EDITOR = 2;
        const int ITEM_LIST = 3;    // ���б�
        const int ORDER_LIST = 4;   // �����б�
        const int ISSUE_LIST = 5;   // ���б�
        const int COMMENT_LIST = 6;   // ��ע�б�

        /// <summary>
        /// ��Ŀ��¼ʱ���
        /// </summary>
        public byte[] BiblioTimestamp = null;

        // 
        /// <summary>
        /// ��ǰ��¼����Ŀ������
        /// ��Ҫ��C#���ο����ű���
        /// </summary>
        public string BiblioDbName
        {
            get
            {
                return Global.GetDbName(this.BiblioRecPath);
            }
        }

        /// <summary>
        /// ��Ŀ��¼·��
        /// </summary>
        public string BiblioRecPath
        {
            get
            {
                return this.textBox_biblioRecPath.Text;
            }
            set
            {
                string strOldDbName = Global.GetDbName(this.BiblioRecPath);
                string strNewDbName = Global.GetDbName(value);


                this.textBox_biblioRecPath.Text = value;

                if (this.entityControl1 != null)
                {
                    this.entityControl1.BiblioRecPath = value;
                }

                if (this.issueControl1 != null)
                {
                    this.issueControl1.BiblioRecPath = value;
                }

                if (this.orderControl1 != null)
                {
                    this.orderControl1.BiblioRecPath = value;
                }

                if (this.binaryResControl1 != null)
                {
                    this.binaryResControl1.BiblioRecPath = value;
                }

                if (this.commentControl1 != null)
                {
                    this.commentControl1.BiblioRecPath = value;
                }

                // ˢ�´��ڱ���
                this.Text = "�ֲ� " + value;

                // ��ʹ����µ������ļ�
                if (strOldDbName != strNewDbName)
                {
                    this.m_marcEditor.MarcDefDom = null;  
                    this.m_marcEditor.Invalidate();   // TODO: ??
                }

                // ��ʾCtrl+A�˵�
                if (this.MainForm.PanelFixedVisible == true)
                    this.AutoGenerate(this.m_marcEditor,
                        new GenerateDataEventArgs(),
                    true);
            }
        }

        // 2009/2/3 new add
        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                if (this.EntitiesChanged == true
                    || this.IssuesChanged == true
                    || this.BiblioChanged == true
                    || this.ObjectChanged == true
                    || this.OrdersChanged == true
                    || this.CommentsChanged == true)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// ���¼�б�ؼ�
        /// </summary>
        public EntityControl EntityControl
        {
            get
            {
                return this.entityControl1;
            }
        }

        /// <summary>
        /// �ڼ�¼�б�ؼ�
        /// </summary>
        public IssueControl IssueControl
        {
            get
            {
                return this.issueControl1;
            }
        }

        /// <summary>
        /// ������¼�б�ؼ�
        /// </summary>
        public OrderControl OrderControl
        {
            get
            {
                return this.orderControl1;
            }
        }

        /// <summary>
        /// ��ע��¼�б�ؼ�
        /// </summary>
        public CommentControl CommentControl
        {
            get
            {
                return this.commentControl1;
            }
        }

        /// <summary>
        /// ������Դ�б�ؼ�
        /// </summary>
        public BinaryResControl BinaryResControl
        {
            get
            {
                return this.binaryResControl1;
            }
        }

        /// <summary>
        /// MARC �༭��
        /// </summary>
        public DigitalPlatform.Marc.MarcEditor MarcEditor
        {
            get
            {
                return m_marcEditor;
            }
        }

        // ��õ�ǰ���ڵ� MARC �ַ���
        // �������ڻ��ʹ�����༭��ͬ����Ȼ���ٻ�������ַ���
        public string GetMarc()
        {
            SynchronizeMarc();
            return this.m_marcEditor.Marc;
        }

        // ���õ�ǰ���ڵ� MARC �ַ���
        // �����ܻ����������༭���� MARC �ַ���
        public void SetMarc(string strMarc)
        {
            this.m_marcEditor.Marc = strMarc;
            this.easyMarcControl1.SetMarc(strMarc);
            this._marcEditorVersion = 0;
            this._templateVersion = 0;
        }

        public void SynchronizeMarc()
        {
            if (this._marcEditorVersion < this._templateVersion)
            {
                this.m_marcEditor.Marc = this.easyMarcControl1.GetMarc();
            }
            if (this._marcEditorVersion > this._templateVersion)
            {
                this.easyMarcControl1.SetMarc(this.m_marcEditor.Marc);
            }
            this._marcEditorVersion = 0;
            this._templateVersion = 0;
        }

        public void SetMarcChanged(bool bChanged)
        {
            this.m_marcEditor.Changed = bChanged;
            this.easyMarcControl1.Changed = bChanged;
        }

        public bool GetMarcChanged()
        {
            // SynchronizeMarc();
            return this.m_marcEditor.Changed || this.easyMarcControl1.Changed;
        }

        // public bool m_bRemoveDeletedItem = false;   // ��ɾ��������ʱ, �Ƿ���Ӿ���Ĩ����Щ����(ʵ�����ڴ����滹�����м����ύ������)?

        /// <summary>
        /// ���캯��
        /// </summary>
        public EntityForm()
        {
            InitializeComponent();
        }

        void EnableItemsPage(bool bEnable)
        {
            EnablePage(bEnable,
                this.tabControl_itemAndIssue,
                this.tabPage_item);

            // 2014/9/5
            // ��ֹ��������������������
            this.textBox_itemBarcode.Enabled = bEnable;
            this.button_register.Enabled = bEnable;
        }

        void EnableObjectsPage(bool bEnable)
        {
            EnablePage(bEnable,
                this.tabControl_itemAndIssue,
                this.tabPage_object);
        }

        void EnableIssuesPage(bool bEnable)
        {
            EnablePage(bEnable,
                this.tabControl_itemAndIssue,
                this.tabPage_issue);
        }

        void EnableOrdersPage(bool bEnable)
        {
            EnablePage(bEnable,
                this.tabControl_itemAndIssue,
                this.tabPage_order);
        }

        void EnableCommentsPage(bool bEnable)
        {
            EnablePage(bEnable,
                this.tabControl_itemAndIssue,
                this.tabPage_comment);
        }

        bool ItemsPageVisible
        {
            get
            {
                return this.tabControl_itemAndIssue.TabPages.IndexOf(this.tabPage_item) != -1;
            }
        }

        static void EnablePage(bool bEnable,
            TabControl container,
            TabPage page)
        {
            if (bEnable == true)
            {
                if (container.TabPages.IndexOf(page) == -1)
                {
                    container.TabPages.Add(page);
                }
            }
            else
            {
                if (container.TabPages.IndexOf(page) != -1)
                {
                    container.TabPages.Remove(page);
                }
            }
        }

        // AppDomain m_scriptDomain = null;

        private void EntityForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

            // m_scriptDomain = AppDomain.CreateDomain("script");
#if NO
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new DigitalPlatform.CirculationClient.BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new DigitalPlatform.CirculationClient.BeforeLoginEventHandle(Channel_BeforeLogin);

                        // 2012/10/3
            this.Channel.AfterLogin -= new AfterLoginEventHandle(Channel_AfterLogin);
            this.Channel.AfterLogin += new AfterLoginEventHandle(Channel_AfterLogin);

            Stop = new DigitalPlatform.Stop();
            Stop.Register(MainForm.stopManager, true);	// ����������
#endif

            this.m_webExternalHost_biblio.Initial(this.MainForm, this.webBrowser_biblioRecord);
            this.webBrowser_biblioRecord.ObjectForScripting = this.m_webExternalHost_biblio;

            // this.m_webExternalHost_comment.Initial(this.MainForm);

            this.MainForm.AppInfo.LoadMdiSize += new EventHandler(AppInfo_LoadMdiSize);
            this.MainForm.AppInfo.SaveMdiSize += new EventHandler(AppInfo_SaveMdiSize);

            // LoadLayout0();
            if (this.AcceptMode == false)
            {
#if NO
                // ���ô��ڳߴ�״̬
                MainForm.AppInfo.LoadMdiChildFormStates(this,
                    "mdi_form_state");
#endif
            }
            else
            {
                Form form = this;
                FormWindowState savestate = form.WindowState;
                bool bStateChanged = false;
                if (form.WindowState != FormWindowState.Normal)
                {
                    form.WindowState = FormWindowState.Normal;
                    bStateChanged = true;
                }

                AppInfo_LoadMdiSize(this, null);

                if (bStateChanged == true)
                    form.WindowState = savestate;
            }

            if (this.AcceptMode == true)
            {
#if ACCEPT_MODE
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
#endif
            }


            // ���Ϊ��ֹ��Ŀ����
            if (this.Cataloging == false)
            {
                this.tabControl_biblioInfo.TabPages.Remove(this.tabPage_marc);
                this.toolStrip_marcEditor.Enabled = false;

                // ���������Ҳ����ֹ
                this.tabControl_itemAndIssue.TabPages.Remove(this.tabPage_object);
                this.binaryResControl1.Enabled = false;
            }


            this.MainForm.FillBiblioFromList(this.comboBox_from);

            // �ָ��ϴ��˳�ʱ�����ļ���;��
            string strFrom = this.MainForm.AppInfo.GetString(
            "entityform",
            "search_from",
            "");
            if (String.IsNullOrEmpty(strFrom) == false)
                this.comboBox_from.Text = strFrom;

            this.checkedComboBox_biblioDbNames.Text = this.MainForm.AppInfo.GetString(
                "entityform",
                "search_dbnames",
                "<ȫ��>");

            this.comboBox_matchStyle.Text = this.MainForm.AppInfo.GetString(
                "entityform",
                "search_matchstyle",
                "ǰ��һ��");


            /*
            // 2008/6/25 new add
            this.checkBox_autoDetectQueryBarcode.Checked = this.MainForm.AppInfo.GetBoolean(
                "entityform",
                "auto_detect_query_barcode",
                true);
             * */

            this.checkBox_autoSavePrev.Checked = this.MainForm.AppInfo.GetBoolean(
                "entityform",
                "auto_save_prev",
                true);

            this.BiblioChanged = false;

            // ���浱ǰ�������ҳ���֣���Ϊ�������Ҫ����й�page
            this.m_strUsedActiveItemPage = GetActiveItemPageName();

            // ��ʼ����ؼ�
            this.entityControl1.GetMacroValue -= new GetMacroValueHandler(issueControl1_GetMacroValue);
            this.entityControl1.GetMacroValue += new GetMacroValueHandler(issueControl1_GetMacroValue);

            this.entityControl1.ContentChanged -= new ContentChangedEventHandler(issueControl1_ContentChanged);
            this.entityControl1.ContentChanged += new ContentChangedEventHandler(issueControl1_ContentChanged);

            this.entityControl1.GetParameterValue -= new GetParameterValueHandler(entityControl1_GetParameterValue);
            this.entityControl1.GetParameterValue += new GetParameterValueHandler(entityControl1_GetParameterValue);

            this.entityControl1.VerifyBarcode -= new VerifyBarcodeHandler(entityControl1_VerifyBarcode);
            this.entityControl1.VerifyBarcode += new VerifyBarcodeHandler(entityControl1_VerifyBarcode);

            this.entityControl1.EnableControlsEvent -= new EnableControlsHandler(entityControl1_EnableControls);
            this.entityControl1.EnableControlsEvent += new EnableControlsHandler(entityControl1_EnableControls);

            this.entityControl1.LoadRecord -= new LoadRecordHandler(entityControl1_LoadRecord111);
            this.entityControl1.LoadRecord += new LoadRecordHandler(entityControl1_LoadRecord111);

            // 2009/2/24 new add
            this.entityControl1.GenerateData -= new GenerateDataEventHandler(entityControl1_GenerateData);
            this.entityControl1.GenerateData += new GenerateDataEventHandler(entityControl1_GenerateData);

            /*
            // 2009/2/24 new add
            this.entityControl1.GenerateAccessNo -= new GenerateDataEventHandler(entityControl1_GenerateAccessNo);
            this.entityControl1.GenerateAccessNo += new GenerateDataEventHandler(entityControl1_GenerateAccessNo); 
             * */

            this.entityControl1.Channel = this.Channel;
            this.entityControl1.Stop = this.Progress;
            this.entityControl1.MainForm = this.MainForm;

            this.EnableItemsPage(false);


            // ��ʼ���ڿؼ�

            // 2008/12/27 new add
            this.issueControl1.GenerateEntity -= new GenerateEntityEventHandler(issueControl1_GenerateEntity);
            this.issueControl1.GenerateEntity += new GenerateEntityEventHandler(issueControl1_GenerateEntity);

            // 2008/12/24 new add
            this.issueControl1.GetOrderInfo -= new GetOrderInfoEventHandler(issueControl1_GetOrderInfo);
            this.issueControl1.GetOrderInfo += new GetOrderInfoEventHandler(issueControl1_GetOrderInfo);

            this.issueControl1.GetItemInfo -= new GetItemInfoEventHandler(issueControl1_GetItemInfo);
            this.issueControl1.GetItemInfo += new GetItemInfoEventHandler(issueControl1_GetItemInfo);

            this.issueControl1.GetMacroValue -= new GetMacroValueHandler(issueControl1_GetMacroValue);
            this.issueControl1.GetMacroValue += new GetMacroValueHandler(issueControl1_GetMacroValue);

            this.issueControl1.ContentChanged -= new ContentChangedEventHandler(issueControl1_ContentChanged);
            this.issueControl1.ContentChanged += new ContentChangedEventHandler(issueControl1_ContentChanged);

            this.issueControl1.EnableControlsEvent -= new EnableControlsHandler(entityControl1_EnableControls);
            this.issueControl1.EnableControlsEvent += new EnableControlsHandler(entityControl1_EnableControls);

            this.issueControl1.ChangeItem -= new ChangeItemEventHandler(issueControl1_ChangeItem);
            this.issueControl1.ChangeItem += new ChangeItemEventHandler(issueControl1_ChangeItem);

            // 2010/4/27
            this.issueControl1.LoadRecord -= new LoadRecordHandler(entityControl1_LoadRecord111);
            this.issueControl1.LoadRecord += new LoadRecordHandler(entityControl1_LoadRecord111);

            // 2012/9/22
            this.issueControl1.GenerateData -= new GenerateDataEventHandler(entityControl1_GenerateData);
            this.issueControl1.GenerateData += new GenerateDataEventHandler(entityControl1_GenerateData);

            this.issueControl1.Channel = this.Channel;
            this.issueControl1.Stop = this.Progress;
            this.issueControl1.MainForm = this.MainForm;

            this.EnableIssuesPage(false);

            // 2010/4/27
            this.issueControl1.InputItemsBarcode = this.MainForm.AppInfo.GetBoolean(
                "entity_form",
                "issueControl_input_item_barcode",
                true);
            // 2011/9/8
            this.issueControl1.SetProcessingState = this.MainForm.AppInfo.GetBoolean(
                "entity_form",
                "issueControl_set_processing_state",
                true);
            // 2012/5/7
            this.issueControl1.CreateCallNumber = this.MainForm.AppInfo.GetBoolean(
                "entity_form",
                "create_callnumber",
                false);

            // ��ʼ���ɹ��ؼ�
            this.orderControl1.GetMacroValue -= new GetMacroValueHandler(issueControl1_GetMacroValue);
            this.orderControl1.GetMacroValue += new GetMacroValueHandler(issueControl1_GetMacroValue);

            this.orderControl1.ContentChanged -= new ContentChangedEventHandler(issueControl1_ContentChanged);
            this.orderControl1.ContentChanged += new ContentChangedEventHandler(issueControl1_ContentChanged);

            this.orderControl1.EnableControlsEvent -= new EnableControlsHandler(entityControl1_EnableControls);
            this.orderControl1.EnableControlsEvent += new EnableControlsHandler(entityControl1_EnableControls);

            this.orderControl1.GenerateEntity -= new GenerateEntityEventHandler(orderControl1_GenerateEntity);
            this.orderControl1.GenerateEntity += new GenerateEntityEventHandler(orderControl1_GenerateEntity);

            // 2008/11/4 new add
            this.orderControl1.OpenTargetRecord -= new OpenTargetRecordEventHandler(orderControl1_OpenTargetRecord);
            this.orderControl1.OpenTargetRecord += new OpenTargetRecordEventHandler(orderControl1_OpenTargetRecord);

            this.orderControl1.HilightTargetItem -= new HilightTargetItemsEventHandler(orderControl1_HilightTargetItem);
            this.orderControl1.HilightTargetItem += new HilightTargetItemsEventHandler(orderControl1_HilightTargetItem);

            // 2009/11/8 new add
            this.orderControl1.SetTargetRecPath -= new SetTargetRecPathEventHandler(orderControl1_SetTargetRecPath);
            this.orderControl1.SetTargetRecPath += new SetTargetRecPathEventHandler(orderControl1_SetTargetRecPath);

            // 2009/11/23 new add
            this.orderControl1.LoadRecord -= new LoadRecordHandler(entityControl1_LoadRecord111);
            this.orderControl1.LoadRecord += new LoadRecordHandler(entityControl1_LoadRecord111);

            this.orderControl1.VerifyLibraryCode -= new VerifyLibraryCodeEventHandler(orderControl1_VerifyLibraryCode);
            this.orderControl1.VerifyLibraryCode += new VerifyLibraryCodeEventHandler(orderControl1_VerifyLibraryCode);

            this.orderControl1.Channel = this.Channel;
            this.orderControl1.Stop = this.Progress;
            this.orderControl1.MainForm = this.MainForm;

            this.EnableOrdersPage(false);

            // ��ʼ����ע�ؼ�
            this.commentControl1.GetMacroValue -= new GetMacroValueHandler(issueControl1_GetMacroValue);
            this.commentControl1.GetMacroValue += new GetMacroValueHandler(issueControl1_GetMacroValue);

            this.commentControl1.ContentChanged -= new ContentChangedEventHandler(issueControl1_ContentChanged);
            this.commentControl1.ContentChanged += new ContentChangedEventHandler(issueControl1_ContentChanged);

            this.commentControl1.EnableControlsEvent -= new EnableControlsHandler(entityControl1_EnableControls);
            this.commentControl1.EnableControlsEvent += new EnableControlsHandler(entityControl1_EnableControls);

            /*
            this.commentControl1.GenerateEntity -= new GenerateEntityEventHandler(orderControl1_GenerateEntity);
            this.commentControl1.GenerateEntity += new GenerateEntityEventHandler(orderControl1_GenerateEntity);

            this.commentControl1.OpenTargetRecord -= new OpenTargetRecordEventHandler(orderControl1_OpenTargetRecord);
            this.commentControl1.OpenTargetRecord += new OpenTargetRecordEventHandler(orderControl1_OpenTargetRecord);

            this.commentControl1.HilightTargetItem -= new HilightTargetItemsEventHandler(orderControl1_HilightTargetItem);
            this.commentControl1.HilightTargetItem += new HilightTargetItemsEventHandler(orderControl1_HilightTargetItem);

            this.commentControl1.SetTargetRecPath -= new SetTargetRecPathEventHandler(orderControl1_SetTargetRecPath);
            this.commentControl1.SetTargetRecPath += new SetTargetRecPathEventHandler(orderControl1_SetTargetRecPath);

            */
            this.commentControl1.LoadRecord -= new LoadRecordHandler(entityControl1_LoadRecord111);
            this.commentControl1.LoadRecord += new LoadRecordHandler(entityControl1_LoadRecord111);

            this.CommentControl.AddSubject -= new AddSubjectEventHandler(CommentControl_AddSubject);
            this.CommentControl.AddSubject += new AddSubjectEventHandler(CommentControl_AddSubject);

            this.commentControl1.Channel = this.Channel;
            this.commentControl1.Stop = this.Progress;
            this.commentControl1.MainForm = this.MainForm;
            // this.commentControl1.WebExternalHost = this.m_webExternalHost_comment;

            this.EnableCommentsPage(false);

            // ��ʼ������ؼ�
            if (this.Cataloging == true)
            {

                this.binaryResControl1.ContentChanged -= new ContentChangedEventHandler(binaryResControl1_ContentChanged);
                this.binaryResControl1.ContentChanged += new ContentChangedEventHandler(binaryResControl1_ContentChanged);

                this.binaryResControl1.Channel = this.Channel;
                this.binaryResControl1.Stop = this.Progress;

                this.m_macroutil.ParseOneMacro -= new ParseOneMacroEventHandler(m_macroutil_ParseOneMacro);
                this.m_macroutil.ParseOneMacro += new ParseOneMacroEventHandler(m_macroutil_ParseOneMacro);

                // 2009/2/24 new add
                this.binaryResControl1.GenerateData -= new GenerateDataEventHandler(entityControl1_GenerateData);
                this.binaryResControl1.GenerateData += new GenerateDataEventHandler(entityControl1_GenerateData);


                LoadFontToMarcEditor();

                this.m_marcEditor.AppInfo = this.MainForm.AppInfo;    // 2009/9/18 new add
            }



            if (this.AcceptMode == true)
            {
                this.flowLayoutPanel_query.Visible = false;
            }
            else
            {
                this.flowLayoutPanel_query.Visible = this.MainForm.AppInfo.GetBoolean(
"entityform",
"queryPanel_visibie",
true);
            }

            this.panel_itemQuickInput.Visible = this.MainForm.AppInfo.GetBoolean(
"entityform",
"itemQuickInputPanel_visibie",
true);

            // 
            this.EnableControls(true);  // ��ʹ�����桱��ť״̬������

            // API.PostMessage(this.Handle, WM_LOADLAYOUT, 0, 0);


            // 2008/11/2 new add
            // RegisterType
            {
                string strRegisterType = this.MainForm.AppInfo.GetString("entity_form",
                    "register_type",
                    "");
                if (String.IsNullOrEmpty(strRegisterType) == false)
                {
                    try
                    {
                        this.RegisterType = (RegisterType)Enum.Parse(typeof(RegisterType), strRegisterType, true);
                    }
                    catch
                    {
                    }
                }
            }

            string strSelectedTemplates = this.MainForm.AppInfo.GetString(
                "entity_form",
                "selected_templates",
                "");
            if (String.IsNullOrEmpty(strSelectedTemplates) == false)
            {
                selected_templates.Build(strSelectedTemplates);
            }

        }

        /// <summary>
        /// ���� MyForm ���͵� OnMyFormLoad() ����
        /// </summary>
        public override void OnMyFormLoad()
        {
            base.OnMyFormLoad();

            // 2013/6/23 �ƶ�������
            this.Channel.AfterLogin -= new AfterLoginEventHandle(Channel_AfterLogin);
            this.Channel.AfterLogin += new AfterLoginEventHandle(Channel_AfterLogin);
        }

        // ������ɴʣ�ǰ�벿�ֹ���
        void CommentControl_AddSubject(object sender, AddSubjectEventArgs e)
        {
            string strError = "";

            // ������Ŀ�������MARC��ʽ�﷨��
            // return:
            //      null    û���ҵ�ָ������Ŀ����
            string strMarcSyntax = MainForm.GetBiblioSyntax(this.BiblioDbName);
            if (strMarcSyntax == null)
            {
                strError = "��Ŀ���� '" + this.BiblioDbName + "' ��Ȼû���ҵ�";
                goto ERROR1;
            }

            List<string> reserve_subjects = null;
            List<string> exist_subjects = null;

            int nRet = ItemInfoForm.GetSubjectInfo(this.GetMarc(),  // this.m_marcEditor.Marc,
                strMarcSyntax,
                out reserve_subjects,
                out exist_subjects,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            AddSubjectDialog dlg = new AddSubjectDialog();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.ReserveSubjects = reserve_subjects;
            dlg.ExistSubjects = exist_subjects;
            dlg.HiddenNewSubjects = e.HiddenSubjects;
            dlg.NewSubjects = e.NewSubjects;

            this.MainForm.AppInfo.LinkFormState(dlg, "entityform_addsubjectdialog_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
            {
                e.Canceled = true;
                return;
            }

            List<string> subjects = new List<string>();
            subjects.AddRange(dlg.ExistSubjects);
            subjects.AddRange(dlg.NewSubjects);

            StringUtil.RemoveDupNoSort(ref subjects);   // ȥ��
            StringUtil.RemoveBlank(ref subjects);   // ȥ����Ԫ��

            string strMARC = this.GetMarc();    //  this.m_marcEditor.Marc;
            // �޸�ָʾ��1Ϊ�յ���Щ 610 �ֶ�
            // parameters:
            //      strSubject  �����޸ĵ����ɴʵ��ܺ͡�������ǰ���ڵĺͱ�����ӵ�
            nRet = ItemInfoForm.ChangeSubject(ref strMARC,
                strMarcSyntax,
                subjects,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            // this.m_marcEditor.Marc = strMARC;
            // this.m_marcEditor.Changed = true;
            this.SetMarc(strMARC);
            this.SetMarcChanged(true);
            return;
        ERROR1:
            e.ErrorInfo = strError;
            if (e.ShowErrorBox == true)
                MessageBox.Show(this, strError);
            e.Canceled = true;
        }

        void orderControl1_VerifyLibraryCode(object sender, VerifyLibraryCodeEventArgs e)
        {
            if (this.Channel == null)
                return;
            if (Global.IsGlobalUser(this.Channel.LibraryCodeList) == true)
                return; // ȫ���û��������

            List<string> librarycodes = Global.FromLibraryCodeList(e.LibraryCode);

            List<string> outof_librarycodes = new List<string>();
            foreach (string strLibraryCode in librarycodes)
            {
                if (StringUtil.IsInList(strLibraryCode, this.Channel.LibraryCodeList) == false)
                    outof_librarycodes.Add(strLibraryCode);
            }

            if (outof_librarycodes.Count > 0)
            {
                StringUtil.RemoveDupNoSort(ref outof_librarycodes);

                e.ErrorInfo = "�ݴ��� '"+StringUtil.MakePathList(outof_librarycodes)+"' ���ڵ�ǰ�û��Ĺ�Ͻ��Χ '"+this.Channel.LibraryCodeList+"' ��";
                return;
            }
        }

        void AppInfo_SaveMdiSize(object sender, EventArgs e)
        {
            if (sender != this)
                return;

            // �ָ���λ��
            this.MainForm.SaveSplitterPos(
                this.splitContainer_recordAndItems,
                "entity_form",
                "main_splitter_pos");


            // ��ǰ���HTML/MARC page
            string strActivePage = "";

            if (this.tabControl_biblioInfo.SelectedTab == this.tabPage_marc)
                strActivePage = "marc";
            else if (this.tabControl_biblioInfo.SelectedTab == this.tabPage_html)
                strActivePage = "html";
            else if (this.tabControl_biblioInfo.SelectedTab == this.tabPage_template)
                strActivePage = "template";

            this.MainForm.AppInfo.SetString(
                "entity_form",
                "active_page",
                strActivePage);

            // ��ǰ��Ĳ�/��/�ɹ�/���� page
            string strActiveItemIssuePage = GetActiveItemPageName();

            // 

            this.MainForm.AppInfo.SetString(
                "entity_form",
                "active_item_issue_page",
                strActiveItemIssuePage);

            string strWidths = ListViewUtil.GetColumnWidthListString(this.entityControl1.ListView);
            this.MainForm.AppInfo.SetString(
                "entity_form",
                "item_list_column_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.orderControl1.ListView);
            this.MainForm.AppInfo.SetString(
                "entity_form",
                "order_list_column_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.commentControl1.ListView);
            this.MainForm.AppInfo.SetString(
                "entity_form",
                "comment_list_column_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.issueControl1.ListView);
            this.MainForm.AppInfo.SetString(
                "entity_form",
                "issue_list_column_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.binaryResControl1.ListView);
            this.MainForm.AppInfo.SetString(
                "entity_form",
                "object_list_column_width",
                strWidths);
        }

        string GetActiveItemPageName()
        {
            string strActiveItemIssuePage = "";

            if (this.tabControl_itemAndIssue.SelectedTab == this.tabPage_item)
                strActiveItemIssuePage = "item";
            else if (this.tabControl_itemAndIssue.SelectedTab == this.tabPage_issue)
                strActiveItemIssuePage = "issue";
            else if (this.tabControl_itemAndIssue.SelectedTab == this.tabPage_object)
                strActiveItemIssuePage = "object";
            else if (this.tabControl_itemAndIssue.SelectedTab == this.tabPage_order)
                strActiveItemIssuePage = "order";

            return strActiveItemIssuePage;
        }

        void AppInfo_LoadMdiSize(object sender, EventArgs e)
        {
            if (sender != this)
                return;

            // *********** ԭ��LoadLayout0()�Ĳ���

            // ��ǰ���HTML/MARC page
            string strActivePage = this.MainForm.AppInfo.GetString(
                "entity_form",
                "active_page",
                "");

            if (String.IsNullOrEmpty(strActivePage) == false)
            {
                if (strActivePage == "marc")
                    this.tabControl_biblioInfo.SelectedTab = this.tabPage_marc;
                else if (strActivePage == "html")
                    this.tabControl_biblioInfo.SelectedTab = this.tabPage_html;
                else if (strActivePage == "template")
                    this.tabControl_biblioInfo.SelectedTab = this.tabPage_template;
            }

            string strActiveItemIssuePage = this.MainForm.AppInfo.GetString(
"entity_form",
"active_item_issue_page",
"");
            if (LoadActiveItemIssuePage(strActiveItemIssuePage) == false)
                this.m_strUsedActiveItemPage = strActiveItemIssuePage;

            // *********** ԭ��LoadLayout()�Ĳ���

            this.MainForm.LoadSplitterPos(
    this.splitContainer_recordAndItems,
    "entity_form",
    "main_splitter_pos");

            string strWidths = this.MainForm.AppInfo.GetString(
                "entity_form",
                "item_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.entityControl1.ListView,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
                "entity_form",
                "order_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.orderControl1.ListView,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
    "entity_form",
    "comment_list_column_width",
    "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.commentControl1.ListView,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
                "entity_form",
                "issue_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.issueControl1.ListView,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
                "entity_form",
                "object_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.binaryResControl1.ListView,
                    strWidths,
                    true);
            }
        }

        void orderControl1_SetTargetRecPath(object sender, SetTargetRecPathEventArgs e)
        {
            if (e.TargetRecPath == this.BiblioRecPath)
                return;

            // ��������������ֵ
            string strOldTargetRecPath = this.m_marcEditor.Record.Fields.GetFirstSubfield("998", "t");
            if (strOldTargetRecPath == e.TargetRecPath)
                return;

            if (this.LinkedRecordReadonly == true)
            {
                this.m_marcEditor.Record.Fields.SetFirstSubfield("998", "t", e.TargetRecPath);
                if (String.IsNullOrEmpty(e.TargetRecPath) == false)
                    this.m_marcEditor.ReadOnly = true;
                else
                    this.m_marcEditor.ReadOnly = false;
            }
        }

        /// <summary>
        /// �Ƿ�Ҫ�� ������Ŀ��¼��ʾΪֻ��״̬
        /// </summary>
        public bool LinkedRecordReadonly
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
"entityform",
"linkedRecordReadonly",
true);
            }
        }

        void entityControl1_GenerateData(object sender, GenerateDataEventArgs e)
        {
            this.AutoGenerate(sender, e);
        }

        /*
        void entityControl1_GenerateAccessNo(object sender, GenerateDataEventArgs e)
        {
            CreateCallNumber(sender, e);
        }*/

        void issueControl1_GenerateEntity(object sender, GenerateEntityEventArgs e)
        {
            orderControl1_GenerateEntity(sender, e);
        }

        // �ڿؼ�Ҫ��ò���Ϣ
        void issueControl1_GetItemInfo(object sender, GetItemInfoEventArgs e)
        {
            string strError = "";

            // 2010/3/26
            this.entityControl1.Items.SetRefID();

            List<string> XmlRecords = null;
            // ���ݳ���ʱ�䣬ƥ�䡰ʱ�䷶Χ�����ϵĲ��¼
            int nRet = this.entityControl1.GetItemInfoByPublishTime(e.PublishTime,
                out XmlRecords,
                out strError);
            if (nRet == -1)
            {
                e.ErrorInfo = strError;
                return;
            }

            e.ItemXmls = XmlRecords;
        }

        // �ڿؼ�Ҫ��ö�����Ϣ
        void issueControl1_GetOrderInfo(object sender,
            GetOrderInfoEventArgs e)
        {
            string strError = "";
            List<string> XmlRecords = null;
            // ���ݳ���ʱ�䣬ƥ�䡰ʱ�䷶Χ�����ϵĶ�����¼
            int nRet = this.orderControl1.GetOrderInfoByPublishTime(e.PublishTime,
                e.LibraryCodeList,
                out XmlRecords,
                out strError);
            if (nRet == -1)
            {
                e.ErrorInfo = strError;
                return;
            }

            e.OrderXmls = XmlRecords;
        }

        void orderControl1_HilightTargetItem(object sender, HilightTargetItemsEventArgs e)
        {
            this.ActivateItemsPage();
            this.SelectItemsByBatchNo(e.BatchNo, true);
        }

        void orderControl1_OpenTargetRecord(object sender, OpenTargetRecordEventArgs e)
        {
            // �´�һ��EntityForm
            EntityForm form = null;
            int nRet = 0;

            if (e.TargetRecPath == this.BiblioRecPath)
            {
                // ����漰��ǰ��¼�������¿�EntityForm�����ˣ���������items page�Ϳ�����
                form = this;
                Global.Activate(form);
            }
            else
            {
                form = new EntityForm();
                form.MdiParent = this.MdiParent;
                form.MainForm = this.MainForm;
                form.Show();

                nRet = form.LoadRecordOld(e.TargetRecPath, 
                    "",
                    false);
                if (nRet != 1)
                {
                    e.ErrorInfo = "Ŀ����Ŀ��¼ " + e.TargetRecPath + " װ��ʧ��";
                    return;
                }

            }

            form.ActivateItemsPage();
            form.SelectItemsByBatchNo(e.BatchNo, true);
        }

        // ѡ��(����)items�����з���ָ�����κŵ���Щ��
        void SelectItemsByBatchNo(string strAcceptBatchNo,
            bool bClearOthersHilight)
        {
            this.entityControl1.SelectItemsByBatchNo(strAcceptBatchNo,
                bClearOthersHilight);
        }

        // ����items page
        /// <summary>
        /// ���������ҳ
        /// </summary>
        /// <returns>��ǰ�Ƿ���ڴ�����ҳ</returns>
        public bool ActivateItemsPage()
        {
            if (this.tabControl_itemAndIssue.Contains(this.tabPage_item) == true)
            {
                this.tabControl_itemAndIssue.SelectedTab = this.tabPage_item;
                return true;
            }

            return false;   // not found
        }

        // ����orders page
        /// <summary>
        /// ���������ҳ
        /// </summary>
        /// <returns>��ǰ�Ƿ���ڴ�����ҳ</returns>
        public bool ActivateOrdersPage()
        {
            if (this.tabControl_itemAndIssue.Contains(this.tabPage_order) == true)
            {
                this.tabControl_itemAndIssue.SelectedTab = this.tabPage_order;
                return true;
            }

            return false;   // not found
        }

        // ����comments page
        /// <summary>
        /// ������ע����ҳ
        /// </summary>
        /// <returns>��ǰ�Ƿ���ڴ�����ҳ</returns>
        public bool ActivateCommentsPage()
        {
            if (this.tabControl_itemAndIssue.Contains(this.tabPage_comment) == true)
            {
                this.tabControl_itemAndIssue.SelectedTab = this.tabPage_comment;
                return true;
            }

            return false;   // not found
        }

        // ����issues page
        /// <summary>
        /// ����������ҳ
        /// </summary>
        /// <returns>��ǰ�Ƿ���ڴ�����ҳ</returns>
        public bool ActivateIssuesPage()
        {
            if (this.tabControl_itemAndIssue.Contains(this.tabPage_issue) == true)
            {
                this.tabControl_itemAndIssue.SelectedTab = this.tabPage_issue;
                return true;
            }

            return false;   // not found
        }

        // 2009/12/16 new add
        // �޸Ĳ����
        void issueControl1_ChangeItem(object sender,
            ChangeItemEventArgs e)
        {
            string strError = "";

            List<InputBookItem> bookitems = new List<InputBookItem>();

            // ����ʵ���¼
            for (int i = 0; i < e.DataList.Count; i++)
            {
                ChangeItemData data = e.DataList[i];

                BookItem bookitem = null;
                // �ⲿ���ã�����һ��ʵ���¼��
                // ���嶯���У�new change delete neworchange
                int nRet = this.entityControl1.DoSetEntity(
                    true,
                    data.Action,
                    data.RefID,
                    data.Xml,
                    out bookitem,
                    out strError);
                if (nRet == -1)
                {
                    data.ErrorInfo = strError;
                }
                else if (nRet == 1)
                {
                    data.WarningInfo = strError;
                    data.WarningInfo += "\r\n\r\n�������������ظ�������ŵļ�¼�Ѿ����ɹ��������޸ġ��������Ժ�ȥ������������ظ�";
                } 

                if (data.Action == "new"
                    || (bookitem != null && bookitem.ItemDisplayState == ItemDisplayState.New))
                {
                    if (String.IsNullOrEmpty(bookitem.Barcode) == true)
                    {
                        InputBookItem input_bookitem = new InputBookItem();
                        input_bookitem.Sequence = data.Sequence;
                        input_bookitem.BookItem = bookitem;
                        bookitems.Add(input_bookitem);
                    }
                }
            }

            if (bookitems.Count > 0
    && e.InputItemBarcode == true)  // 2009/1/15 new add
            {
                // Ҫ��������������
                InputItemBarcodeDialog item_barcode_dlg = new InputItemBarcodeDialog();
                MainForm.SetControlFont(item_barcode_dlg, this.Font, false);

                item_barcode_dlg.AppInfo = this.MainForm.AppInfo;
                item_barcode_dlg.SeriesMode = e.SeriesMode; // 2008/12/27 new add

                item_barcode_dlg.DetectBarcodeDup -= new DetectBarcodeDupHandler(item_barcode_dlg_DetectBarcodeDup);
                item_barcode_dlg.DetectBarcodeDup += new DetectBarcodeDupHandler(item_barcode_dlg_DetectBarcodeDup);

                item_barcode_dlg.VerifyBarcode -= new VerifyBarcodeHandler(item_barcode_dlg_VerifyBarcode);
                item_barcode_dlg.VerifyBarcode += new VerifyBarcodeHandler(item_barcode_dlg_VerifyBarcode);

                item_barcode_dlg.EntityControl = this.entityControl1;
                item_barcode_dlg.BookItems = bookitems;

                this.MainForm.AppInfo.LinkFormState(item_barcode_dlg, "entityform_inputitembarcodedlg_state");
                item_barcode_dlg.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(item_barcode_dlg);


                if (item_barcode_dlg.DialogResult != DialogResult.OK)
                {
                }
            }

            // TODO: �Ƿ�Ҫ������ȡ��?
            // Ϊ�µĲ��¼������ȡ��
            if (e.CreateCallNumber == true && bookitems.Count > 0)
            {
                // ѡ���µĲ��¼����
                List<BookItem> items = new List<BookItem>();
                foreach (InputBookItem input_item in bookitems)
                {
                    items.Add(input_item.BookItem);
                }
                // ��listview��ѡ��ָ��������
                int nRet = this.EntityControl.SelectItems(
                   true,
                   items);
                if (nRet < items.Count)
                {
                    e.ErrorInfo = "SetlectItems()δ��ѡ��Ҫ���ȫ������";
                    this.ActivateItemsPage();
                    return;
                }

                // Ϊ��ǰѡ�����������ȡ��
                // return:
                //      -1  ����
                //      0   ��������
                //      1   �Ѿ�����
                nRet = this.EntityControl.CreateCallNumber(
                    false,
                    out strError);
                if (nRet == -1)
                {
                    /*
                    e.ErrorInfo = "������ȡ��ʱ��������: " + strError;
                    this.ActivateItemsPage();
                    return;
                     * */
                    // ��������
                    // 2012/9/1
                    this.ActivateItemsPage();
                    MessageBox.Show(this, "���棺������ȡ��ʱ��������: " + strError);
                }
            }

        }

        // ���յ�ʱ���Զ�����ʵ���¼
        void orderControl1_GenerateEntity(object sender, 
            GenerateEntityEventArgs e)
        {
            string strError = "";
            int nRet = 0;

            string strTargetRecPath = "";
            if (sender is OrderControl)
            {
                Debug.Assert(e.SeriesMode == false, "");
                strTargetRecPath = this.orderControl1.TargetRecPath;
            }
            else if (sender is IssueControl)
            {
                Debug.Assert(e.SeriesMode == true, "");
                strTargetRecPath = this.issueControl1.TargetRecPath;
            }
            else if (sender is IssueManageControl)
            {
                Debug.Assert(e.SeriesMode == true, "");
                strTargetRecPath = this.issueControl1.TargetRecPath;
            }
            else
            {
                Debug.Assert(false, "");
                strTargetRecPath = this.orderControl1.TargetRecPath;
            }


            EntityForm form = null;

            // 4) �����·��Ϊ�գ���ʾ��Ҫͨ���˵�ѡ��Ŀ��⣬Ȼ������ͬ3)
            if (String.IsNullOrEmpty(strTargetRecPath) == true)
            {
                string strBiblioRecPath = "";

                if (e.SeriesMode == false)
                {
                    // ͼ�顣
                    
                    // TODO: ���Ϊ�����⣬���Ի���򿪺�ȱʡѡ��Դ����? �����᷽�����������մ���ʵ�崰�������ղ���

                    // ������Ŀ�������MARC��ʽ�﷨��
                    // return:
                    //      null    û���ҵ�ָ������Ŀ����
                    string strCurSyntax = MainForm.GetBiblioSyntax(this.BiblioDbName);
                    if (strCurSyntax == null)
                    {
                        e.ErrorInfo = "��Ŀ���� '" + this.BiblioDbName + "' ��Ȼû���ҵ�";
                        return;
                    }

                    // TODO: �����ѡ�б�Ϊһ���������Ǿ���ò������û�ѡ��?

                    // ���һ��Ŀ�����
                    GetAcceptTargetDbNameDlg dlg = new GetAcceptTargetDbNameDlg();
                    MainForm.SetControlFont(dlg, this.Font, false);
                    dlg.AutoFinish = true;
                    dlg.SeriesMode = e.SeriesMode;
                    dlg.MainForm = this.MainForm;
                    dlg.DbName = this.BiblioDbName;
                    // ���ݵ�ǰ���ڵĿ��marc syntax����һ��Ŀ���ķ�Χ
                    dlg.MarcSyntax = strCurSyntax;

                    dlg.ShowDialog(this);

                    if (dlg.DialogResult != DialogResult.OK)
                    {
                        e.ErrorInfo = "��������ʵ���¼";
                        return;
                    }

                    // ���Ŀ������͵�ǰ���ڵ���Ŀ��¼·���еĿ�����ͬ������ζ��Ŀ���¼���ǵ�ǰ��¼���������½���¼
                    if (dlg.DbName == this.BiblioDbName)
                    {
                        strBiblioRecPath = this.BiblioRecPath;
                    }
                    else
                    {
                        strBiblioRecPath = dlg.DbName + "/?";
                    }
                }
                else
                {
                    // 2009/11/9 new add
                    // �ڿ�����ֹ���յ�������¼��������ֱ�����յ�Դ��¼��
                    strBiblioRecPath = this.BiblioRecPath;
                }

                // �´�һ��EntityForm
                if (strBiblioRecPath == this.BiblioRecPath)
                {
                    // ����漰��ǰ��¼�������¿�EntityForm������
                    form = this;
                }
                else
                {
                    form = new EntityForm();
                    form.MdiParent = this.MdiParent;
                    form.MainForm = this.MainForm;
                    form.Show();

                    // ����MARC��¼
                    // ??? e.BiblioRecord 
                    form.m_marcEditor.Marc = this.GetMarc();    //  this.m_marcEditor.Marc;

                    form.BiblioRecPath = strBiblioRecPath;
                }

                form.EnableItemsPage(true);

                // TODO: �ڴ���ʵ���¼�����У��Ƿ�������������������?
                // ���������ŵ�ͬʱ��Ҫ��Ŀ��ʾ��������Ӧ�Ĺݲصص㣬�Ա㹤����Ա����ڷ�ͼ��
                // Ҳ����dp2Circulation�ṩһ��ͨ��ɨ��������ٹ۲�ݲصص�Ĺ��ܴ���

                goto CREATE_ENTITY;
            }

            // 3) �����·�����п������֣���ʾ�ּ�¼�����ڣ���Ҫ���ݵ�ǰ��¼��MARC��������
            /*
            string strID = Global.GetID(this.TargetRecPath);
            if (String.IsNullOrEmpty(strID) == true
                || strID == "?")
             * */
            if (Global.IsAppendRecPath(this.TargetRecPath) == true)   // 2008/12/3 new add
            {
                string strDbName = Global.GetDbName(strTargetRecPath);

                // ·��ȫΪ�յ�����Ѿ��ߵ�ǰ��ķ�֧���ˣ������ߵ�����
                Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "·��ȫΪ�յ�����Ѿ��ߵ�ǰ��ķ�֧���ˣ������ߵ�����");

                // TODO: ��Ҫ���һ��strDbName�е����ݿ����Ƿ�ȷʵΪĿ���

                string strBiblioRecPath = "";

                // ���Ŀ������͵�ǰ���ڵ���Ŀ��¼·���еĿ�����ͬ������ζ��Ŀ���¼���ǵ�ǰ��¼���������½���¼
                if (strDbName == this.BiblioDbName)
                {
                    strBiblioRecPath = this.BiblioRecPath;
                }
                else
                {
                    strBiblioRecPath = strDbName + "/?";
                }

                // �´�һ��EntityForm
                if (strBiblioRecPath == this.BiblioRecPath)
                {
                    // ����漰��ǰ��¼�������¿�EntityForm������
                    form = this;
                }
                else
                {
                    form = new EntityForm();
                    form.MdiParent = this.MdiParent;
                    form.MainForm = this.MainForm;
                    form.Show();
                }

                // ����MARC��¼
                form.m_marcEditor.Marc = this.GetMarc();    //  this.m_marcEditor.Marc;
                form.BiblioRecPath = strBiblioRecPath;
                form.EnableItemsPage(true);


                goto CREATE_ENTITY;
            }

            // 1)�����·���͵�ǰ��¼·��һ�£�����ʵ���¼�ʹ����ڵ�ǰ��¼�£�
            if (this.entityControl1.BiblioRecPath == strTargetRecPath)
            {

                // ��Ҫ�󱣴�
                form = this;
                goto CREATE_ENTITY;
            }


            // 2) Ŀ���¼·���͵�ǰ��¼·����һ�£�����Ŀ���ּ�¼�Ѿ����ڣ���Ҫ�������洴��ʵ���¼��

            {
                Debug.Assert(strTargetRecPath != this.BiblioRecPath, "�¿����ڣ����벻�漰��ǰ��Ŀ��¼");

                Debug.Assert(form == null, "");

                // �´�һ��EntityForm
                form = new EntityForm();
                form.MdiParent = this.MdiParent;
                form.MainForm = this.MainForm;
                form.Show();

                nRet = form.LoadRecordOld(strTargetRecPath,
                    "",
                    false);
                if (nRet != 1)
                {
                    e.ErrorInfo = "Ŀ����Ŀ��¼ " +strTargetRecPath+ " װ��ʧ��";
                    return;
                }

                // items page��Ȼ�ᱻ��ʾ����

                goto CREATE_ENTITY;
            }

        CREATE_ENTITY:

            Debug.Assert(form != null, "");

            List<InputBookItem> bookitems = new List<InputBookItem>();

            // ����ʵ���¼
            for (int i = 0; i < e.DataList.Count; i++)
            {
                GenerateEntityData data = e.DataList[i];

                BookItem bookitem = null;
                // �ⲿ���ã�����һ��ʵ���¼��
                // ���嶯���У�new change delete
                nRet = form.entityControl1.DoSetEntity(
                    false,
                    data.Action,
                    data.RefID,
                    data.Xml,
                    out bookitem,
                    out strError);
                if (nRet == -1 || nRet == 1)
                {
                    Debug.Assert(nRet != 1, "");
                    data.ErrorInfo = strError;
                }

                if (data.Action == "new")
                {
                    InputBookItem input_bookitem = new InputBookItem();
                    input_bookitem.Sequence = data.Sequence;
                    input_bookitem.OtherPrices = data.OtherPrices;
                    input_bookitem.BookItem = bookitem;
                    bookitems.Add(input_bookitem);
                }
            }

            if (bookitems.Count > 0
                && e.InputItemBarcode == true)  // 2009/1/15 new add
            {
                // Ҫ��������������
                InputItemBarcodeDialog item_barcode_dlg = new InputItemBarcodeDialog();
                MainForm.SetControlFont(item_barcode_dlg, this.Font, false);

                item_barcode_dlg.AppInfo = this.MainForm.AppInfo;
                item_barcode_dlg.SeriesMode = e.SeriesMode; // 2008/12/27 new add

                item_barcode_dlg.DetectBarcodeDup -= new DetectBarcodeDupHandler(item_barcode_dlg_DetectBarcodeDup);
                item_barcode_dlg.DetectBarcodeDup += new DetectBarcodeDupHandler(item_barcode_dlg_DetectBarcodeDup);

                item_barcode_dlg.VerifyBarcode -= new VerifyBarcodeHandler(item_barcode_dlg_VerifyBarcode);
                item_barcode_dlg.VerifyBarcode += new VerifyBarcodeHandler(item_barcode_dlg_VerifyBarcode);

                item_barcode_dlg.EntityControl = form.entityControl1;
                item_barcode_dlg.BookItems = bookitems;

                this.MainForm.AppInfo.LinkFormState(item_barcode_dlg, "entityform_inputitembarcodedlg_state");
                item_barcode_dlg.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(item_barcode_dlg);


                if (item_barcode_dlg.DialogResult != DialogResult.OK)
                {
                }
            }

            // ??
            // �����ձ�����õ���Ŀ��¼·�����ص�TargetRecPath��
            strTargetRecPath = form.BiblioRecPath;
            if (sender is OrderControl)
                this.orderControl1.TargetRecPath = strTargetRecPath;
            else if (sender is IssueControl)
                this.issueControl1.TargetRecPath = strTargetRecPath;
            else if (sender is IssueManageControl)
                this.issueControl1.TargetRecPath = strTargetRecPath;

            // ����MARC��¼
            if (String.IsNullOrEmpty(e.BiblioRecord) == false)
            {
                Debug.Assert(e.BiblioSyntax == "unimarc" 
                    || e.BiblioSyntax == "usmarc"
                    || e.BiblioSyntax == "marc"
                    || e.BiblioSyntax == "xml",
                    "");
                nRet = form.ImportRecordString(e.BiblioSyntax,
                    e.BiblioRecord,
                    out strError);
                if (nRet == -1)
                {
                    e.ErrorInfo = strError;
                    return;
                }
            }

            // Ϊ�µĲ��¼������ȡ��
            if (e.CreateCallNumber == true && bookitems.Count > 0)
            {
                // ѡ���µĲ��¼����
                List<BookItem> items = new List<BookItem>();
                foreach (InputBookItem input_item in bookitems)
                {
                    items.Add(input_item.BookItem);
                }
                // ��listview��ѡ��ָ��������
                nRet = form.EntityControl.SelectItems(
                   true,
                   items);
                if (nRet < items.Count)
                {
                    e.ErrorInfo = "SetlectItems()δ��ѡ��Ҫ���ȫ������";
                    form.ActivateItemsPage();
                    return;
                }

                // Ϊ��ǰѡ�����������ȡ��
                // return:
                //      -1  ����
                //      0   ��������
                //      1   �Ѿ�����
                nRet = form.EntityControl.CreateCallNumber(
                    false,
                    out strError);
                if (nRet == -1)
                {
                    /*
                    e.ErrorInfo = "������ȡ��ʱ��������: " + strError;
                    form.ActivateItemsPage();
                    return;
                     * */
                    // ��������
                    // 2012/9/1
                    this.ActivateItemsPage();
                    MessageBox.Show(this, "���棺������ȡ��ʱ��������: " + strError);
                }
            }


            // ����������¼?
            if (this != form)
            {
                // �ύ���б�������
                // return:
                //      -1  �д���ʱ���ų���Щ��Ϣ����ɹ���
                //      0   �ɹ���
                nRet = form.DoSaveAll();
                e.TargetRecPath = form.BiblioRecPath;

                if (form.HasCommentPage == true && form.CommentControl != null
                    && this.HasCommentPage == true && this.CommentControl != null
                    && this.CommentControl.Items.Count > 0)
                {
                    // �ƶ����鲢��ע��¼
                    // �ı����
                    // ���޸�ʵ����Ϣ��<parent>Ԫ�����ݣ�ʹָ������һ����Ŀ��¼
                    // parameters:
                    //      items   Ҫ�ı����������ϡ����Ϊ null����ʾȫ���ı����
                    nRet = this.CommentControl.ChangeParent(null,
                        form.BiblioRecPath,
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(this, "���棺�ƶ���ע��¼(" + this.BiblioRecPath + " --> " + form.BiblioRecPath + ")ʱ��������: " + strError);

                    // ����װ����ע����ҳ
                    nRet = form.CommentControl.LoadItemRecords(form.BiblioRecPath,
                        "",
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(this, "���棺����װ����Ŀ��¼ " + form.BiblioRecPath + " ��������ע��¼ʱ��������: " + strError);

                }
            }
            else
            {
                e.TargetRecPath = form.BiblioRecPath;
            }

            // ������ʾ֪ͨ�Ƽ����Ķ���
            if (form.HostObject != null)
            {
                AfterCreateItemsArgs e1 = new AfterCreateItemsArgs();
                e1.Case = "accept";
                form.HostObject.AfterCreateItems(this, e1);
                if (string.IsNullOrEmpty(e.ErrorInfo) == false)
                {
                    MessageBox.Show(this, "�����д������¼����������(AfterCreateItems)ʧ��: " + strError + "\r\n\r\n����������Ѿ��ɹ�");
                }
            }

            // 2013/12/2 �ƶ�������
            if (this != form)
            {
                form.Close();
            }

            return;
        }

        /*
        // �����µ�MARC�ַ��������Ǳ���ԭ����998�ֶ�
        void ImportMarcString(string strMarc)
        {
            Field old_998 = null;

            // ���浱ǰ��¼��998�ֶ�
            old_998 = this.MarcEditor.Record.Fields.GetOneField("998", 0);

            this.MarcEditor.Marc = strMarc;

            if (old_998 != null)
            {
                // �ָ���ǰ��998�ֶ�����
                for (int i = 0; i < this.MarcEditor.Record.Fields.Count; i++)
                {
                    Field temp = this.MarcEditor.Record.Fields[i];
                    if (temp.Name == "998")
                    {
                        this.MarcEditor.Record.Fields.RemoveAt(i);
                        i--;
                    }
                }
                this.MarcEditor.Record.Fields.Insert(this.MarcEditor.Record.Fields.Count,
                    old_998.Name,
                    old_998.Indicator,
                    old_998.Value);
            }
        }
         * */

        // �����µ�MARC/XML�ַ��������Ǳ���ԭ����998�ֶ�
        int ImportRecordString(
            string strSyntax,
            string strRecord,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            Field old_998 = null;

            // ���浱ǰ��¼��998�ֶ�
            old_998 = this.m_marcEditor.Record.Fields.GetOneField("998", 0);

            if (strSyntax == "xml")
            {
                nRet = this.SetBiblioRecordToMarcEditor(strRecord, out strError);
            }
            else if (strSyntax == "marc" || strSyntax == "unimarc" || strSyntax == "usmarc")
            {
                // this.m_marcEditor.Marc = strRecord;
                this.SetMarc(strRecord);
            }

            if (nRet == -1)
                return -1;

            if (old_998 != null)
            {
                // �ָ���ǰ��998�ֶ�����
                for (int i = 0; i < this.m_marcEditor.Record.Fields.Count; i++)
                {
                    Field temp = this.m_marcEditor.Record.Fields[i];
                    if (temp.Name == "998")
                    {
                        this.m_marcEditor.Record.Fields.RemoveAt(i);
                        i--;
                    }
                }
                this.m_marcEditor.Record.Fields.Insert(this.m_marcEditor.Record.Fields.Count,
                    old_998.Name,
                    old_998.Indicator,
                    old_998.Value);
            }

            return 0;
        }

        // ��������Ի�������У������
        void item_barcode_dlg_VerifyBarcode(object sender, VerifyBarcodeEventArgs e)
        {
            string strError = "";
            e.Result = this.VerifyBarcode(
                this.Channel.LibraryCodeList,
                e.Barcode,
                out strError);
            e.ErrorInfo = strError;
        }

        void item_barcode_dlg_DetectBarcodeDup(object sender, DetectBarcodeDupEventArgs e)
        {
            int nRet = 0;
            string strError = "";

            // ��һ��������������
            // return:
            //      -1  ����
            //      0   ���ظ�
            //      1   �ظ�
            nRet = e.EntityControl.CheckBarcodeDup(
                e.BookItems,
                out strError);
            e.Result = nRet;
            e.ErrorInfo = strError;
        }

        void m_macroutil_ParseOneMacro(object sender, ParseOneMacroEventArgs e)
        {
            // string strError = "";
            string strName = Unquote(e.Macro);  // ȥ���ٷֺ�

            // ��������
            string strFuncName = "";
            string strParams = "";

            int nRet = strName.IndexOf(":");
            if (nRet == -1)
            {
                strFuncName = strName.Trim();
            }
            else
            {
                strFuncName = strName.Substring(0, nRet).Trim();
                strParams = strName.Substring(nRet + 1).Trim();
            }

            if (strName == "username")
            {
                e.Value = this.Channel.UserName;
                return;
            }

            string strValue = "";
            string strError = "";
            // ��marceditor_macrotable.xml�ļ��н�����
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = MacroUtil.GetFromLocalMacroTable(
                PathUtil.MergePath(this.MainForm.DataDir, "marceditor_macrotable.xml"),
                strName,
                e.Simulate,
                out strValue,
                out strError);
            if (nRet == -1)
            {
                e.Canceled = true;
                e.ErrorInfo = strError;
                return;
            }

            if (nRet == 1)
            {
                e.Value = strValue;
                return;
            }

            if (String.Compare(strFuncName, "IncSeed", true) == 0
                || String.Compare(strFuncName, "IncSeed+", true) == 0
                || String.Compare(strFuncName, "+IncSeed", true) == 0)
            {
                // �ִκſ���, ָ����, Ҫ��䵽��λ��
                string[] aParam = strParams.Split(new char[] { ',' });
                if (aParam.Length != 3 && aParam.Length != 2)
                {
                    strError = "IncSeed��Ҫ2��3��������";
                    goto ERROR1;
                }

                bool IncAfter = false;  // �Ƿ�Ϊ��ȡ���
                if (strFuncName[strFuncName.Length - 1] == '+')
                    IncAfter = true;

                string strZhongcihaoDbName = aParam[0].Trim();
                string strEntryName = aParam[1].Trim();
                strValue = "";

                long lRet = 0;
                if (e.Simulate == true)
                {
                    // parameters:
                    //      strZhongcihaoGroupName  @�����ִκſ��� !����������Ŀ���� ������� �ִκ�����
                    lRet = Channel.GetZhongcihaoTailNumber(
    null,
    strZhongcihaoDbName,
    strEntryName,
    out strValue,
    out strError);
                    if (lRet == -1)
                        goto ERROR1; 
                    if (string.IsNullOrEmpty(strValue) == true)
                    {
                        strValue = "1";
                    }
                }
                else
                {
                    // parameters:
                    //      strZhongcihaoGroupName  @�����ִκſ��� !����������Ŀ���� ������� �ִκ�����
                    lRet = this.Channel.SetZhongcihaoTailNumber(
    null,
    IncAfter == true ? "increase+" : "increase",
    strZhongcihaoDbName,
    strEntryName,
    "1",
    out strValue,
    out strError);
                    if (lRet == -1)
                        goto ERROR1;
                }

                // ������'0'
                if (aParam.Length == 3)
                {
                    int nWidth = 0;
                    try
                    {
                        nWidth = Convert.ToInt32(aParam[2]);
                    }
                    catch
                    {
                        strError = "��������Ӧ��Ϊ�����֣���ʾ����Ŀ�ȣ�";
                        goto ERROR1;
                    }
                    e.Value = strValue.PadLeft(nWidth, '0');
                }
                else
                    e.Value = strValue;
                return;
            }

            e.Canceled = true;  // ���ܽ��ʹ���
            return;
        ERROR1:
            e.Canceled = true;
            e.ErrorInfo = strError;
        }

        static string Unquote(string strValue)
        {
            if (string.IsNullOrEmpty(strValue) == true)
                return "";

            if (strValue[0] == '%')
                strValue = strValue.Substring(1);
            if (strValue.Length == 0)
                return "";
            if (strValue[strValue.Length - 1] == '%')
                return strValue.Substring(0, strValue.Length - 1);

            return strValue;
        }

        void binaryResControl1_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            SetSaveAllButtonState(true);
        }

        void entityControl1_LoadRecord111(object sender, LoadRecordEventArgs e)
        {
            e.Result = this.LoadRecordOld(e.BiblioRecPath,
                "",
                false);
        }

        void entityControl1_EnableControls(object sender, EnableControlsEventArgs e)
        {
            if (this.m_nInDisable == 0)
                this.EnableControls(e.bEnable);
        }

        // ��ؼ�����У������
        void entityControl1_VerifyBarcode(object sender, VerifyBarcodeEventArgs e)
        {
            string strError = "";
            e.Result = this.VerifyBarcode(
                this.Channel.LibraryCodeList,
                e.Barcode,
                out strError);
            e.ErrorInfo = strError;
        }

        // ��ؼ�ѯ�ʲ���ֵ
        void entityControl1_GetParameterValue(object sender, GetParameterValueEventArgs e)
        {
            if (e.Name == "NeedVerifyItemBarcode")
            {
                e.Value = this.NeedVerifyItemBarcode == true ? "true" : "false";
            }
        }

        void issueControl1_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            SetSaveAllButtonState(true);
        }

        void issueControl1_GetMacroValue(object sender, GetMacroValueEventArgs e)
        {
            e.MacroValue = this.GetMacroValue(e.MacroName);
        }

#if NOOOOOOOOOOOOO
        // װ�ز��֡�����Ҫ�첽�Ĳ���
        void LoadLayout0()
        {
            if (this.AcceptMode == false)
            {
                // ���ô��ڳߴ�״̬
                MainForm.AppInfo.LoadMdiChildFormStates(this,
                    "mdi_form_state");
            }



            // ��ǰ���HTML/MARC page
            string strActivePage = this.MainForm.AppInfo.GetString(
                "entity_form",
                "active_page",
                "");

            if (String.IsNullOrEmpty(strActivePage) == false)
            {
                if (strActivePage == "marc")
                    this.tabControl_biblioInfo.SelectedTab = this.tabPage_marc;
                else if (strActivePage == "html")
                    this.tabControl_biblioInfo.SelectedTab = this.tabPage_html;
            }

            LoadActiveItemIssuePage();
        }
#endif

        string m_strUsedActiveItemPage = ""; // �Ƿ�������������ҳ? ����ǣ���װ�ؼ�¼��ʱ����Ҫ�ٴζ���

        bool LoadActiveItemIssuePage(string strActiveItemIssuePage)
        {

            if (this.AcceptMode == true)
            {
                // ��������˳�򣬼���order page / item page
                if (this.tabControl_itemAndIssue.TabPages.IndexOf(this.tabPage_issue) != -1)
                    this.tabControl_itemAndIssue.SelectedTab = this.tabPage_issue;
                else if (this.tabControl_itemAndIssue.TabPages.IndexOf(this.tabPage_order) != -1)
                    this.tabControl_itemAndIssue.SelectedTab = this.tabPage_order;
                else if (this.tabControl_itemAndIssue.TabPages.IndexOf(this.tabPage_item) != -1)
                    this.tabControl_itemAndIssue.SelectedTab = this.tabPage_item;
                else if (this.tabControl_itemAndIssue.TabPages.Count > 0)
                    this.tabControl_itemAndIssue.SelectedTab = this.tabControl_itemAndIssue.TabPages[this.tabControl_itemAndIssue.TabPages.Count - 1];    // ����һ��page
                return true;
            }

            // ��ǰ��Ĳ�/�� page
            if (strActiveItemIssuePage == null)
            {
                strActiveItemIssuePage = this.MainForm.AppInfo.GetString(
        "entity_form",
        "active_item_issue_page",
        "");
            }

            if (String.IsNullOrEmpty(strActiveItemIssuePage) == false)
            {
                if (strActiveItemIssuePage == "item")
                {
                    if (this.tabControl_itemAndIssue.TabPages.IndexOf(this.tabPage_item) != -1)
                    {
                        this.tabControl_itemAndIssue.SelectedTab = this.tabPage_item;
                        return true;
                    }
                }
                else if (strActiveItemIssuePage == "order")
                {
                    if (this.tabControl_itemAndIssue.TabPages.IndexOf(this.tabPage_order) != -1)
                    {
                        this.tabControl_itemAndIssue.SelectedTab = this.tabPage_order;
                        return true;
                    }
                }
                else if (strActiveItemIssuePage == "issue")
                {
                    if (this.tabControl_itemAndIssue.TabPages.IndexOf(this.tabPage_issue) != -1)
                    {
                        this.tabControl_itemAndIssue.SelectedTab = this.tabPage_issue;
                        return true;
                    }
                }
                else if (strActiveItemIssuePage == "object")
                {
                    if (this.tabControl_itemAndIssue.TabPages.IndexOf(this.tabPage_object) != -1)
                    {
                        this.tabControl_itemAndIssue.SelectedTab = this.tabPage_object;
                        return true;
                    }
                }

            }

            if (this.tabControl_itemAndIssue.TabPages.Count > 0)
                this.tabControl_itemAndIssue.SelectedTab = this.tabControl_itemAndIssue.TabPages[this.tabControl_itemAndIssue.TabPages.Count-1];    // ����һ��page

            return false;
        }

#if NOOOOOOOOOOOOOOOOOO
        // װ�ز��֡���Ҫ�첽�Ĳ���
        void LoadLayout()
        {
            // 2009/1/15 new add
            if (this.AcceptMode == true)
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
            }

            this.MainForm.LoadSplitterPos(
                this.splitContainer_recordAndItems,
                "entity_form",
                "main_splitter_pos");

            string strWidths = this.MainForm.AppInfo.GetString(
                "entity_form",
                "item_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.entityControl1.ListView,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
                "entity_form",
                "order_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.orderControl1.ListView,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
                "entity_form",
                "issue_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.issueControl1.ListView,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
                "entity_form",
                "object_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.binaryResControl1.ListView,
                    strWidths,
                    true);
            }

        }

        // ���沼��
        void SaveLayout()
        {
            if (this.AcceptMode == false)
            {
                MainForm.AppInfo.SaveMdiChildFormStates(this,
                    "mdi_form_state");

                /*
                // ���MDI�Ӵ��ڲ���MainForm�ո�׼���˳�ʱ��״̬���ָ�����Ϊ�˼���ߴ���׼��
                if (this.WindowState != this.MainForm.MdiWindowState)
                    this.WindowState = this.MainForm.MdiWindowState;
                 * */
            }


            // �ָ���λ��
            this.MainForm.SaveSplitterPos(
                this.splitContainer_recordAndItems,
                "entity_form",
                "main_splitter_pos");


            // ��ǰ���HTML/MARC page
            string strActivePage = "";

            if (this.tabControl_biblioInfo.SelectedTab == this.tabPage_marc)
                strActivePage = "marc";
            else if (this.tabControl_biblioInfo.SelectedTab == this.tabPage_html)
                strActivePage = "html";

            this.MainForm.AppInfo.SetString(
                "entity_form",
                "active_page",
                strActivePage);

            // ��ǰ��Ĳ�/��/�ɹ�/���� page
            string strActiveItemIssuePage = "";

            if (this.tabControl_itemAndIssue.SelectedTab == this.tabPage_item)
                strActiveItemIssuePage = "item";
            else if (this.tabControl_itemAndIssue.SelectedTab == this.tabPage_issue)
                strActiveItemIssuePage = "issue";
            else if (this.tabControl_itemAndIssue.SelectedTab == this.tabPage_object)
                strActiveItemIssuePage = "object";
            else if (this.tabControl_itemAndIssue.SelectedTab == this.tabPage_order)
                strActiveItemIssuePage = "order";

            this.MainForm.AppInfo.SetString(
                "entity_form",
                "active_item_issue_page",
                strActiveItemIssuePage);

            string strWidths = ListViewUtil.GetColumnWidthListString(this.entityControl1.ListView);
            this.MainForm.AppInfo.SetString(
                "entity_form",
                "item_list_column_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.orderControl1.ListView);
            this.MainForm.AppInfo.SetString(
                "entity_form",
                "order_list_column_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.issueControl1.ListView);
            this.MainForm.AppInfo.SetString(
                "entity_form",
                "issue_list_column_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.binaryResControl1.ListView);
            this.MainForm.AppInfo.SetString(
                "entity_form",
                "object_list_column_width",
                strWidths);
        }
#endif

        // ��õ�ǰ���޸ı�־�Ĳ��ֵ�����
        string GetCurrentChangedPartName()
        {
            string strPart = "";

            if (this.BiblioChanged == true)
                strPart += "��Ŀ��Ϣ";

            if (this.EntitiesChanged == true)
            {
                if (strPart != "")
                    strPart += "��";
                strPart += "����Ϣ";
            }

            if (this.IssuesChanged == true)
            {
                if (strPart != "")
                    strPart += "��";
                strPart += "����Ϣ";
            }

            if (this.ObjectChanged == true)
            {
                if (strPart != "")
                    strPart += "��";
                strPart += "������Ϣ";
            }

            if (this.OrdersChanged == true)
            {
                if (strPart != "")
                    strPart += "��";
                strPart += "�ɹ���Ϣ";
            }

            if (this.CommentsChanged == true)
            {
                if (strPart != "")
                    strPart += "��";
                strPart += "��ע��Ϣ";
            } 
            
            return strPart;
        }

        private void EntityForm_FormClosing(object sender, FormClosingEventArgs e)
        {
#if NO
            if (Stop != null)
            {
                if (Stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }
            }
#endif

            if (this.EntitiesChanged == true
                || this.IssuesChanged == true
                || this.BiblioChanged == true
                || this.ObjectChanged == true
                || this.OrdersChanged == true
                || this.CommentsChanged == true)
            {

                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�������� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
                    "EntityForm",
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

        private void EntityForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (Stop != null) // �������
            {
                Stop.Unregister();	// ����������
                Stop = null;
            }
#endif

            /*
            if (m_scriptDomain != null)
            {
                AppDomain.Unload(m_scriptDomain);
                m_scriptDomain = null;
            }
             * */

            if (this.m_webExternalHost_biblio != null)
                this.m_webExternalHost_biblio.Destroy();
            /*
            if (this.m_webExternalHost_comment != null)
                this.m_webExternalHost_comment.Destroy();
             * */

            if (this.m_verifyViewer != null)
                this.m_verifyViewer.Close();

            if (this.m_commentViewer != null)
                this.m_commentViewer.Close();

            if (this.m_genDataViewer != null)
            {
                m_genDataViewer.TriggerAction -= new TriggerActionEventHandler(m_genDataViewer_TriggerAction);
                m_genDataViewer.SetMenu -= new RefreshMenuEventHandler(m_genDataViewer_SetMenu);
                this.m_genDataViewer.Close();
                this.m_genDataViewer = null;
            }

            if (this.browseWindow != null)
                this.browseWindow.Close();


            // �������;��
            this.MainForm.AppInfo.SetString(
                "entityform",
                "search_from",
                this.comboBox_from.Text);

            this.MainForm.AppInfo.SetString(
                "entityform",
                "search_dbnames",
                this.checkedComboBox_biblioDbNames.Text);

            this.MainForm.AppInfo.SetString(
                "entityform",
                "search_matchstyle",
                this.comboBox_matchStyle.Text);

            /*
            // 2008/6/25 new add
            this.MainForm.AppInfo.SetBoolean(
                "entityform",
                "auto_detect_query_barcode",
                this.checkBox_autoDetectQueryBarcode.Checked);
             * */

            this.MainForm.AppInfo.SetBoolean(
                "entityform",
                "auto_save_prev",
                this.checkBox_autoSavePrev.Checked);

            // 2008/11/2 new add
            // RegisterType
            this.MainForm.AppInfo.SetString("entity_form",
                "register_type",
                this.RegisterType.ToString());

            string strSelectedTemplates = selected_templates.Export();
            this.MainForm.AppInfo.SetString(
                "entity_form",
                "selected_templates",
                strSelectedTemplates);

            // 2010/4/27
            this.MainForm.AppInfo.SetBoolean("entity_form",
                "issueControl_input_item_barcode",
                this.issueControl1.InputItemsBarcode);
            this.MainForm.AppInfo.SetBoolean(
    "entity_form",
    "issueControl_set_processing_state",
    this.issueControl1.SetProcessingState);
            // 2012/5/7
            this.MainForm.AppInfo.SetBoolean(
                "entity_form",
                "create_callnumber",
                this.issueControl1.CreateCallNumber);

            // SaveLayout();
            if (this.AcceptMode == false)
            {
#if NO
                MainForm.AppInfo.SaveMdiChildFormStates(this,
                    "mdi_form_state");
#endif
            }
            else
            {
                Form form = this;
                FormWindowState savestate = form.WindowState;
                bool bStateChanged = false;
                if (form.WindowState != FormWindowState.Normal)
                {
                    form.WindowState = FormWindowState.Normal;
                    bStateChanged = true;
                }

                AppInfo_SaveMdiSize(this, null);

                if (bStateChanged == true)
                    form.WindowState = savestate;
            }

            this.MainForm.AppInfo.LoadMdiSize -= new EventHandler(AppInfo_LoadMdiSize);
            this.MainForm.AppInfo.SaveMdiSize -= new EventHandler(AppInfo_SaveMdiSize);
        }


        void Channel_AfterLogin(object sender, AfterLoginEventArgs e)
        {
            this.commentControl1.SetLibraryCodeFilter(this.Channel.LibraryCodeList);
        }

#if NO
        void Channel_BeforeLogin(object sender, DigitalPlatform.CirculationClient.BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }

        public void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }
#endif

        // 
        /// <summary>
        /// ������Ϣ�Ƿ񱻸ı�
        /// </summary>
        public bool ObjectChanged
        {
            get
            {
                if (this.binaryResControl1 != null)
                    return this.binaryResControl1.Changed;

                return false;
            }
            set
            {
                if (this.binaryResControl1 != null)
                    this.binaryResControl1.Changed = value;
            }

        }

        // 
        /// <summary>
        /// ʵ����Ϣ�Ƿ񱻸ı�
        /// </summary>
        public bool EntitiesChanged
        {
            get
            {
                if (this.entityControl1 != null)
                    return this.entityControl1.Changed;

                return false;
            }
            set
            {
                if (this.entityControl1 != null)
                    this.entityControl1.Changed = value;
            }

        }

        // 
        /// <summary>
        /// ����Ϣ�Ƿ񱻸ı�
        /// </summary>
        public bool IssuesChanged
        {
            get
            {
                if (this.issueControl1 != null)
                    return this.issueControl1.Changed;

                return false;
            }
            set
            {
                if (this.issueControl1 != null)
                    this.issueControl1.Changed = value;
            }
        }

        // 
        /// <summary>
        /// �ɹ���Ϣ�Ƿ񱻸ı�
        /// </summary>
        public bool OrdersChanged
        {
            get
            {
                if (this.orderControl1 != null)
                    return this.orderControl1.Changed;

                return false;
            }
            set
            {
                if (this.orderControl1 != null)
                    this.orderControl1.Changed = value;
            }
        }

        // 
        /// <summary>
        /// ��ע��Ϣ�Ƿ񱻸ı�
        /// </summary>
        public bool CommentsChanged
        {
            get
            {
                if (this.commentControl1 != null)
                    return this.commentControl1.Changed;

                return false;
            }
            set
            {
                if (this.commentControl1 != null)
                    this.commentControl1.Changed = value;
            }
        }

        // 
        /// <summary>
        /// ��Ŀ��Ϣ�Ƿ񱻸ı�
        /// </summary>
        public bool BiblioChanged
        {
            get
            {
                if (this.m_marcEditor != null)
                {
                    // ���object id�����ı䣬��ô����MARCû�иı䣬�����ĺϳ�XMLҲ�����˸ı�
                    if (this.binaryResControl1 != null)
                    {
                        if (this.binaryResControl1.IsIdUsageChanged() == true)
                            return true;
                    }

                    // return this.m_marcEditor.Changed;
                    return this.GetMarcChanged();
                }

                return false;
            }
            set
            {
                if (this.Cataloging == false)
                {
                    if (value == true)
                    {
                        throw new Exception("��ǰ�������Ŀ���ܣ���˲��ܶ�BiblioChanged����trueֵ");
                    }
                }

                if (this.m_marcEditor != null)
                {
                    // this.m_marcEditor.Changed = value;
                    this.SetMarcChanged(value);
                }

                // ****
                toolStripButton_marcEditor_save.Enabled = value;
            }
        }

        // return:
        //      -1  ����
        //      0   û��װ��(���緢�ִ����ڵļ�¼û�б��棬���־���Ի���󣬲�����ѡ����Cancel�����ߡ���ͷ������β��)
        //      1   �ɹ�װ��
        //      2   ͨ����ռ��
        /// <summary>
        /// �ɿ�װ�ؼ�¼
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strPrevNextStyle">ǰ�󷭶����</param>
        /// <returns>
        ///      -1  ����
        ///      0   û��װ��(���緢�ִ����ڵļ�¼û�б��棬���־���Ի���󣬲�����ѡ����Cancel�����ߡ���ͷ������β��)
        ///      1   �ɹ�װ��
        ///      2   ͨ����ռ��
        /// </returns>
        public int SafeLoadRecord(string strBiblioRecPath,
            string strPrevNextStyle)
        {
            string strError = "";
            int nRet = LoadRecord(strBiblioRecPath,
                strPrevNextStyle,
                true,
                false,
                out strError);
            if (nRet == 2)
            {
                this.AddToPendingList(strBiblioRecPath, strPrevNextStyle);
            }

            return nRet;
        }

        /// <summary>
        /// ����װ�ص�ǰ��¼
        /// </summary>
        public void Reload()
        {
            string strError = "";
            int nRet = this.LoadRecord(this.BiblioRecPath,
    "",
    true,
    true,
    out strError,
    true);
            if (nRet == -1 /*|| string.IsNullOrEmpty(strError) == false*/)
                MessageBox.Show(this, strError);
        }

        // ������ǰϰ�ߵİ汾
        // return:
        //      -1  �����Ѿ���MessageBox����
        //      0   û��װ��(���緢�ִ����ڵļ�¼û�б��棬���־���Ի���󣬲�����ѡ����Cancel)
        //      1   �ɹ�װ��
        //      2   ͨ����ռ��
        /// <summary>
        /// װ�ؼ�¼���ɰ汾
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strPrevNextStyle">ǰ�󷭶����</param>
        /// <param name="bCheckInUse">�Ƿ���ͨ��ռ�����</param>
        /// <returns>
        ///      -1  �����Ѿ���MessageBox����
        ///      0   û��װ��(���緢�ִ����ڵļ�¼û�б��棬���־���Ի���󣬲�����ѡ����Cancel)
        ///      1   �ɹ�װ��
        ///      2   ͨ����ռ��
        /// </returns>
        public int LoadRecordOld(string strBiblioRecPath,
            string strPrevNextStyle,
            bool bCheckInUse)
        {
            string strError = "";
            int nRet = LoadRecord(strBiblioRecPath,
                strPrevNextStyle,
                bCheckInUse,
                true,
                out strError);
            if (nRet == -1 || nRet == 2)
            {
                if (String.IsNullOrEmpty(strError) == false)
                    MessageBox.Show(this, strError);
            }
            else
            {
                if (String.IsNullOrEmpty(strError) == false)
                    MessageBox.Show(this, strError);
            }

            return nRet;
        }

        // TODO: ������һ���������Ȼ��Ŀ��¼�������ļ�¼�������ڣ����Ǵ������ݱ��ı��ˣ���Ȼ������ǰ�����ݡ������ʱ�����¼���������ⷢ�������籾�����еĲ���Ϣ������ˡ�
        // Ҫ��취����������±��ִ�����ȫ����Ϣ���䣻���ߣ���Ȼ�Ѿ��ı䣬���԰�MARC���ڵļ�¼ȫ���������Ŀ��¼·��Ҳ������������
        // parameters:
        //      bWarningNotSave �Ƿ񾯸���δ���棿���==false�����ҡ��Զ����桱checkboxΪtrue�����Զ����棬������
        //      bSetFocus   װ����ɺ��Ƿ�ѽ����л���MarcEditor��
        // return:
        //      -1  ����
        //      0   û��װ��(���緢�ִ����ڵļ�¼û�б��棬���־���Ի���󣬲�����ѡ����Cancel�����ߡ���ͷ������β��)
        //      1   �ɹ�װ��
        //      2   ͨ����ռ��
        /// <summary>
        /// װ�ؼ�¼
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strPrevNextStyle">ǰ�󷭶����</param>
        /// <param name="bCheckInUse">�Ƿ���ͨ��ռ�����</param>
        /// <param name="bSetFocus">װ����ɺ��Ƿ�ѽ����л���MarcEditor��</param>
        /// <param name="strTotalError">�����ܵĳ������</param>
        /// <param name="bWarningNotSave">�Ƿ񾯸���δ���棿���==false�����ҡ��Զ����桱checkboxΪtrue�����Զ����棬������</param>
        /// <returns>
        ///      -1  ����
        ///      0   û��װ��(���緢�ִ����ڵļ�¼û�б��棬���־���Ի���󣬲�����ѡ����Cancel�����ߡ���ͷ������β��)
        ///      1   �ɹ�װ��
        ///      2   ͨ����ռ��
        /// </returns>
        public int LoadRecord(string strBiblioRecPath,
            string strPrevNextStyle,
            bool bCheckInUse,
            bool bSetFocus,
            out string strTotalError,
            bool bWarningNotSave = false)
        {
            string strError = "";
            strTotalError = "";

            this.m_nChannelInUse++;
            try
            {
                if (bCheckInUse == true)
                {
                    if (this.m_nChannelInUse > 1 || this.m_nInSearching > 0)
                    {
                        /*
                        this.m_nChannelInUse--;
                        MessageBox.Show(this, "ͨ���Ѿ���ռ�á����Ժ�����");
                        return -1;
                         * */
                        strError = "ͨ���Ѿ���ռ�á����Ժ�����";
                        return 2;   // ͨ����ռ��
                    }
                }

                bool bMarcEditorContentChanged = false; // MARC�༭���ڵ����ݿ����޸�?
                bool bBiblioRecordExist = false;    // ��Ŀ��¼�Ƿ����?
                bool bSubrecordExist = false;   // ������һ�������ļ�¼����
                bool bSubrecordListCleared = false; // �Ӽ�¼��list�Ƿ������?

                string strOutputBiblioRecPath = "";

                lock (this.Channel)
                {
                    if (this.EntitiesChanged == true
                        || this.IssuesChanged == true
                        || this.BiblioChanged == true
                        || this.ObjectChanged == true
                        || this.OrdersChanged == true
                        || this.CommentsChanged == true)
                    {
                        // 2008/6/25 new add
                        if (this.checkBox_autoSavePrev.Checked == true
                            && bWarningNotSave == false)
                        {
                            int nRet = this.DoSaveAll();
                            if (nRet == -1)
                            {
                                // strTotalError = "��ǰ��¼��δ����";  // 2014/7/8
                                return -1;
                            }
                        }
                        else
                        {
                            // ������δ����
                            DialogResult result = MessageBox.Show(this,
                                "��ǰ�������� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪװ��������? ",
                                "EntityForm",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button2);
                            if (result == DialogResult.No)
                                return 0;
                        }
                    }

                    EnableControls(false);
                    try
                    {
                        // 2012/7/25 �ƶ�������
                        // ��Ϊ LoadBiblioRecord() �ᵼ�����AutoGen�˵�
                        if (this.m_genDataViewer != null)
                            this.m_genDataViewer.Clear();

                        if (this.m_commentViewer != null)
                            this.m_commentViewer.Clear();

                        string strXml = "";
                        int nRet = this.LoadBiblioRecord(strBiblioRecPath,
                            strPrevNextStyle,
                            false,
                            out strOutputBiblioRecPath,
                            out strXml,
                            out strError);
                        if (nRet == -1)
                        {
                            string strErrorText = "װ����Ŀ��¼ '" + strBiblioRecPath + "' (style='" + strPrevNextStyle + "')ʱ��������: " + strError;
#if NO
                            Global.SetHtmlString(this.webBrowser_biblioRecord,
                                strErrorText);
#endif
                            this.m_webExternalHost_biblio.SetHtmlString(strErrorText, "entityform_error");

                            // MessageBox.Show(this, strErrorText);
                            if (String.IsNullOrEmpty(strTotalError) == false)
                                strTotalError += "\r\n";
                            strTotalError += strErrorText;
                        }
                        else if (nRet == 0)
                        {
                            bBiblioRecordExist = false;
                            // ��Ȼ�ּ�¼�����ڣ�����Ҳ����װ�ز��¼
                            // return 0;

                            string strText = "";

                            // �ڲ���ǰ�󷭿���¼������£�Ҫ���MARC�����������
                            if (String.IsNullOrEmpty(strPrevNextStyle) == true)
                            {
                                strText = "��Ŀ��¼ '" + strBiblioRecPath + "' û���ҵ�...";

                                // ���MARC�����������
                                // this.m_marcEditor.Marc = "012345678901234567890123";
                                this.SetMarc("012345678901234567890123");
                                bMarcEditorContentChanged = true;

                                // �����Ŀ��¼�����ڣ�������strBiblioRecPath��·��
                                if (String.IsNullOrEmpty(strOutputBiblioRecPath) == true)
                                {
                                    strOutputBiblioRecPath = strBiblioRecPath;
                                }
                            }
                            else
                            {
                                if (strPrevNextStyle == "prev")
                                    strText = "��ͷ";
                                else if (strPrevNextStyle == "next")
                                    strText = "��β";

                                strText += "\r\n\r\n(�����ڵ�ԭ��¼û�б�ˢ��)";

                                strOutputBiblioRecPath = "";    // ��ʱ�����װ��������¼Ҳ�޷������ˣ���Ϊ��֪����Ŀ��¼��·����TODO: �������Բ��ò²ⷨ������Ŀ��¼·��+1����-1,ֱ��������һ����¼
                                // MessageBox.Show(this, strText);

                                if (String.IsNullOrEmpty(strTotalError) == false)
                                    strTotalError += "\r\n";
                                strTotalError += strText;

                                return 0;   // 2008/11/2 new add
                            }

                            // MessageBox.Show(this, strText);
                            if (String.IsNullOrEmpty(strTotalError) == false)
                                strTotalError += "\r\n";
                            strTotalError += strText;
                        }
                        else
                        {
                            bBiblioRecordExist = true;
                        }

                        bool bError = false;

                        // ע����bBiblioRecordExist==trueʱ��LoadBiblioRecord()�������Ѿ��������Ŀ��¼·��

                        strBiblioRecPath = null;    // ��ֹ�������ʹ�á���Ϊprev/next���ʱ��strBiblioRecPath��·������������õļ�¼��·��

                        // ���4��������¼�Ŀؼ�
                        this.entityControl1.ClearItems();
                        this.textBox_itemBarcode.Text = ""; // 2009/1/5 new add

                        this.issueControl1.ClearItems();
                        this.orderControl1.ClearItems();   // 2008/11/2 new add
                        this.commentControl1.ClearItems();
                        this.binaryResControl1.Clear(); // 2008/11/2 new add
                        if (this.m_verifyViewer != null)
                            this.m_verifyViewer.Clear();
                        /*
                        if (this.m_genDataViewer != null)
                            this.m_genDataViewer.Clear();
                         * */

                        bSubrecordListCleared = true;

                        if (String.IsNullOrEmpty(strOutputBiblioRecPath) == false)
                        {
                            string strBiblioDbName = "";

                            /*
                            if (String.IsNullOrEmpty(strOutputBiblioRecPath) == false)
                                strBiblioDbName = Global.GetDbName(strOutputBiblioRecPath);
                            else
                            {
                                Debug.Assert(String.IsNullOrEmpty(strBiblioRecPath) == false, "");
                                strBiblioDbName = Global.GetDbName(strBiblioRecPath);
                            }
                             * */
                            strBiblioDbName = Global.GetDbName(strOutputBiblioRecPath);

                            // ����װ����ص����в�
                            string strItemDbName = this.MainForm.GetItemDbName(strBiblioDbName);
                            if (String.IsNullOrEmpty(strItemDbName) == false) // ���ڵ�ǰ��Ŀ���ж�Ӧ��ʵ���ʱ����װ����¼
                            {
                                this.EnableItemsPage(true);

                                nRet = this.entityControl1.LoadItemRecords(strOutputBiblioRecPath,    // 2008/11/2 new changed
                                    // this.DisplayOtherLibraryItem,
                                    this.DisplayOtherLibraryItem == true ? "getotherlibraryitem" : "",
                                    out strError);
                                if (nRet == -1)
                                {
                                    // MessageBox.Show(this, strError);
                                    if (String.IsNullOrEmpty(strTotalError) == false)
                                        strTotalError += "\r\n";
                                    strTotalError += strError;


                                    bError = true;
                                    // return -1;
                                }

                                if (nRet == 1)
                                    bSubrecordExist = true;
                            }
                            else
                            {
                                this.EnableItemsPage(false);
                            }


                            // ����װ����ص�������
                            string strIssueDbName = this.MainForm.GetIssueDbName(strBiblioDbName);
                            if (String.IsNullOrEmpty(strIssueDbName) == false) // ���ڵ�ǰ��Ŀ���ж�Ӧ���ڿ�ʱ����װ���ڼ�¼
                            {
                                this.EnableIssuesPage(true);

                                nRet = this.issueControl1.LoadItemRecords(strOutputBiblioRecPath,  // 2008/11/2 changed
                                    "",
                                    out strError);
                                if (nRet == -1)
                                {
                                    // MessageBox.Show(this, strError);
                                    if (String.IsNullOrEmpty(strTotalError) == false)
                                        strTotalError += "\r\n";
                                    strTotalError += strError;

                                    bError = true;
                                    // return -1;
                                }

                                if (nRet == 1)
                                    bSubrecordExist = true;
                            }
                            else
                            {
                                this.EnableIssuesPage(false);
                            }

                            // ����װ����ص����ж�����Ϣ
                            string strOrderDbName = this.MainForm.GetOrderDbName(strBiblioDbName);
                            if (String.IsNullOrEmpty(strOrderDbName) == false) // ���ڵ�ǰ��Ŀ���ж�Ӧ�Ĳɹ���ʱ����װ��ɹ���¼
                            {
                                if (String.IsNullOrEmpty(strIssueDbName) == false)
                                    this.orderControl1.SeriesMode = true;
                                else
                                    this.orderControl1.SeriesMode = false;

                                this.EnableOrdersPage(true);
                                nRet = this.orderControl1.LoadItemRecords(strOutputBiblioRecPath,  // 2008/11/2 changed
                                    "",
                                    out strError);
                                if (nRet == -1)
                                {
                                    // MessageBox.Show(this, strError);
                                    if (String.IsNullOrEmpty(strTotalError) == false)
                                        strTotalError += "\r\n";
                                    strTotalError += strError;

                                    bError = true;
                                    // return -1;
                                }


                                if (nRet == 1)
                                    bSubrecordExist = true;
                            }
                            else
                            {
                                this.EnableOrdersPage(false);
                            }

                            // ����װ����ص�������ע��Ϣ
                            string strCommentDbName = this.MainForm.GetCommentDbName(strBiblioDbName);
                            if (String.IsNullOrEmpty(strCommentDbName) == false) // ���ڵ�ǰ��Ŀ���ж�Ӧ�Ĳɹ���ʱ����װ��ɹ���¼
                            {
                                this.EnableCommentsPage(true);
                                nRet = this.commentControl1.LoadItemRecords(strOutputBiblioRecPath,
                                    "",
                                    out strError);
                                if (nRet == -1)
                                {
                                    if (String.IsNullOrEmpty(strTotalError) == false)
                                        strTotalError += "\r\n";
                                    strTotalError += strError;

                                    bError = true;
                                }


                                if (nRet == 1)
                                    bSubrecordExist = true;
                            }
                            else
                            {
                                this.EnableCommentsPage(false);
                            }

                            // ����װ�������Դ
                            {
                                nRet = this.binaryResControl1.LoadObject(strOutputBiblioRecPath,    // 2008/11/2 changed
                                    strXml,
                                    out strError);
                                if (nRet == -1)
                                {
                                    // MessageBox.Show(this, strError);
                                    if (String.IsNullOrEmpty(strTotalError) == false)
                                        strTotalError += "\r\n";
                                    strTotalError += strError;

                                    bError = true;
                                    // return -1;
                                }

                                if (nRet == 1)
                                    bSubrecordExist = true;

                            }

                            // װ����Ŀ��<dprms:file>���������XMLƬ��
                            if (string.IsNullOrEmpty(strXml) == false)
                            {
                                nRet = LoadXmlFragment(strXml,
                                    out strError);
                                if (nRet == -1)
                                {
                                    if (String.IsNullOrEmpty(strTotalError) == false)
                                        strTotalError += "\r\n";
                                    strTotalError += strError;

                                    bError = true;
                                }
                            }


                        } // end of if (String.IsNullOrEmpty(strOutputBiblioRecPath) == false)

                        if (string.IsNullOrEmpty(this.m_strUsedActiveItemPage) == false)
                        {
                            // ֻҪ��ʵ��⣬���㵱ǰ��Ŀ��¼û��������ʵ���¼��ҲҪ��ʾ��listview page
                            if (LoadActiveItemIssuePage(m_strUsedActiveItemPage) == true)
                                this.m_strUsedActiveItemPage = "";
                        }

                        if (bBiblioRecordExist == false && bSubrecordExist == true)
                            this.BiblioRecPath = strOutputBiblioRecPath;

                        if (bBiblioRecordExist == false
                            && bSubrecordExist == false
                            && bSubrecordListCleared == true)
                        {
                            if (bMarcEditorContentChanged == false)
                            {
                                this.m_marcEditor.Marc = "012345678901234567890123";
                                this.SetMarc("012345678901234567890123");
                                bMarcEditorContentChanged = true;
                            }

                            if (this.DeletedMode == false)
                                this.BiblioRecPath = "";    // ��������¼�����˲��ø��ǵļ�¼

                        }

                        // 2008/11/2 new add
                        if (bMarcEditorContentChanged == true)
                            this.BiblioChanged = false; // ��������Զ�����ʱ���󸲸��˲��ø��ǵļ�¼

                        // 2008/9/16 new add
                        this.DeletedMode = false;

                        if (bError == true)
                            return -1;

                        // 2013/11/13
                        if (bBiblioRecordExist == false
    && bSubrecordExist == false
    && bSubrecordListCleared == true)
                            return -1;

                        // 2008/11/26 new add
                        if (m_strFocusedPart == "marceditor"
                            && bSetFocus == true)
                        {
                            SwitchFocus(MARC_EDITOR);
                        }

                        DoViewComment(false);

                        return 1;
                    }
                    finally
                    {
                        EnableControls(true);
                    }
                }
            }
            finally
            {
                this.m_nChannelInUse--;
            }
        }

        /// <summary>
        /// ��ǰ��¼�Ƿ������ע����ҳ
        /// </summary>
        public bool HasCommentPage
        {
            get
            {
                if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
                    return false;

                string strBiblioDbName = Global.GetDbName(this.BiblioRecPath);
                string strCommentDbName = this.MainForm.GetCommentDbName(strBiblioDbName);
                if (String.IsNullOrEmpty(strCommentDbName) == false) // ���ڵ�ǰ��Ŀ���ж�Ӧ�Ĳɹ���ʱ����װ��ɹ���¼
                    return true;
                return false;
            }
        }

        // TODO: ƴ��������仯����ǰ������ƴ���ַ����ǲ��ǻ���������?
        /// <summary>
        /// ��õ�ǰ��¼�Ѿ�ѡ����Ķ��������
        /// </summary>
        /// <returns>�����ַ�����ƴ���ַ����Ķ��ձ�</returns>
        public Hashtable GetSelectedPinyin()
        {
            Hashtable result = new Hashtable();
            if (this.domXmlFragment == null)
                return result;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);
            XmlNodeList nodes = this.domXmlFragment.DocumentElement.SelectNodes("dprms:selectedPinyin/dprms:entry", nsmgr);
            foreach (XmlNode node in nodes)
            {
                result[node.InnerText] = DomUtil.GetAttr(node, "pinyin");
            }

            return result;
        }

        /// <summary>
        /// ����ʹ�ù���ƴ����Ϣ
        /// �洢�����ṩ�Ժ�ʹ��
        /// </summary>
        /// <param name="table">�����ַ�����ƴ���ַ����Ķ��ձ�</param>
        public void SetSelectedPinyin(Hashtable table)
        {
            if (this.domXmlFragment == null)
            {
                this.domXmlFragment = new XmlDocument();
                this.domXmlFragment.LoadXml("<root />");
            }
            bool bChanged = false;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);
            XmlNode root = this.domXmlFragment.DocumentElement.SelectSingleNode("dprms:selectedPinyin", nsmgr);
            if (root == null)
            {
                root = this.domXmlFragment.CreateElement("dprms:selectedPinyin", DpNs.dprms);
                this.domXmlFragment.DocumentElement.AppendChild(root);
                bChanged = true;
            }
            else
            {
                if (String.IsNullOrEmpty(root.InnerXml) == false)
                {
                    root.InnerXml = ""; // ���ԭ����ȫ���¼�Ԫ��
                    bChanged = true;
                }
            }

            if (table == null)
            {
                if (bChanged == true)
                    this.BiblioChanged = true;
                return;
            }

            foreach (string key in table.Keys)
            {
                // keyΪ����
                XmlNode node = this.domXmlFragment.CreateElement("dprms:entry", DpNs.dprms);
                root.AppendChild(node);
                node.InnerText = key;
                DomUtil.SetAttr(node, "pinyin", (string)table[key]);
                bChanged = true;
            }

            if (bChanged == true)
                this.BiblioChanged = true;
        }

        // װ����Ŀ��<dprms:file>���������XMLƬ��
        int LoadXmlFragment(string strXml,
            out string strError)
        {
            strError = "";

            this.domXmlFragment = null;

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

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//unimarc:leader | //unimarc:controlfield | //unimarc:datafield | //usmarc:leader | //usmarc:controlfield | //usmarc:datafield | //dprms:file", nsmgr);
            foreach (XmlNode node in nodes)
            {
                node.ParentNode.RemoveChild(node);
            }

            this.domXmlFragment = new XmlDocument();
            this.domXmlFragment.LoadXml("<root />");
            this.domXmlFragment.DocumentElement.InnerXml = dom.DocumentElement.InnerXml;

            return 0;
        }

        // ����ض�λ����Ŀ��¼�Ƿ��Ѿ�����
        // parameters:
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int DetectBiblioRecord(string strBiblioRecPath,
            out byte [] timestamp,
            out string strError)
        {
            strError = "";
            timestamp = null;

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("���ڼ����Ŀ��¼ " + strBiblioRecPath + " ...");
            Progress.BeginLoop();

            try
            {

                string[] formats = new string[1];
                formats[0] = "xml";

                string[] results = null;

                long lRet = Channel.GetBiblioInfos(
                    Progress,
                    strBiblioRecPath,
                    "",
                    formats,
                    out results,
                    out timestamp,
                    out strError);
                if (lRet == 0)
                {
                    return 0;   // not found
                }
                if (lRet == -1)
                    return -1;
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }

            return 1;
            /*
        ERROR1:
            return -1;
             * */
        }



        // װ����Ŀ��¼
        // �������ں����޸�this.BiblioRecPath������м�������޸�
        // parameters:
        //      strDirectionStyle   prev/next/��
        //      bWarningNotSaved    �Ƿ񾯸�װ��ǰ��Ŀ��Ϣ�޸ĺ���δ���棿
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int LoadBiblioRecord(string strBiblioRecPath,
            string strDirectionStyle,
            bool bWarningNotSaved,
            out string strOutputBiblioRecPath,
            out string strXml,
            out string strError)
        {
            strXml = "";
            strOutputBiblioRecPath = "";

            // 2008/6/24 new add
            if (String.IsNullOrEmpty(strDirectionStyle) == false)
            {
                if (strDirectionStyle != "prev"
                    && strDirectionStyle != "next")
                {
                    strError = "δ֪��strDirectionStyle����ֵ '" + strDirectionStyle + "'";
                    return -1;
                }
            }

            if (bWarningNotSaved == true
                && this.Cataloging == true
                && this.BiblioChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ�б�Ŀ��Ϣ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪװ��������? ",
    "EntityForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    strError = "����װ����Ŀ��¼";
                    return -1;
                }
            }

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("���ڳ�ʼ���������� ...");
            Progress.BeginLoop();

            // this.Update();   // �Ż�
            // this.MainForm.Update();

            try
            {

                if (String.IsNullOrEmpty(strDirectionStyle) == false)
                {
                    strBiblioRecPath += "$" + strDirectionStyle;
                }

#if NO
                Global.SetHtmlString(this.webBrowser_biblioRecord, "(�հ�)");
#endif
                this.m_webExternalHost_biblio.SetHtmlString("(�հ�)", "entityform_error");

                Progress.SetMessage("����װ����Ŀ��¼ " + strBiblioRecPath + " ...");

                bool bCataloging = this.Cataloging;

                /*
                long lRet = Channel.GetBiblioInfo(
                    stop,
                    strBiblioRecPath,
                    "html",
                    out strHtml,
                    out strError);
                 * */
                string[] formats = null;

                if (bCataloging == true)
                {
                    formats = new string[3];
                    formats[0] = "outputpath";
                    formats[1] = "html";
                    formats[2] = "xml";
                }
                else
                {
                    formats = new string[2];
                    formats[0] = "outputpath";
                    formats[1] = "html";
                }

                string[] results = null;
                byte[] baTimestamp = null;

                long lRet = Channel.GetBiblioInfos(
                    Progress,
                    strBiblioRecPath,
                    "",
                    formats,
                    out results,
                    out baTimestamp,
                    out strError);
                if (lRet == 0)
                {
                    // Global.SetHtmlString(this.webBrowser_biblioRecord, "·��Ϊ '" + strBiblioRecPath + "' ����Ŀ��¼û���ҵ� ...");
                    this.m_webExternalHost_biblio.SetHtmlString("·��Ϊ '" + strBiblioRecPath + "' ����Ŀ��¼û���ҵ� ...",
                        "entityform_error");
                    return 0;   // not found
                }

                string strHtml = "";

                bool bError = false;
                string strErrorText = "";

                if (results != null && results.Length >= 1)
                    strOutputBiblioRecPath = results[0];

                if (results != null && results.Length >= 2)
                    strHtml = results[1];

                if (lRet == -1)
                {
                    // ��ʱ������
                    bError = true;
                    strErrorText = strError;

                    // �б��������²�ˢ��ʱ��� 2008/11/28 changed
                }
                else
                {
                    // 2014/11/5
                    if (string.IsNullOrEmpty(strError) == false)
                    {
                        bError = true;
                        strErrorText = strError;
                    }

                    // û�б���ʱ��Ҫ��results�����ϸ���
                    if (results == null)
                    {
                        strError = "results == null";
                        goto ERROR1;
                    }
                    if (results.Length != formats.Length)
                    {
                        strError = "result.Length != formats.Length";
                        goto ERROR1;
                    }

                    // û�б��������²�ˢ��ʱ��� 2008/11/28 changed
                    this.BiblioTimestamp = baTimestamp;
                }


#if NO
                Global.SetHtmlString(this.webBrowser_biblioRecord,
                    strHtml,
                    this.MainForm.DataDir,
                    "entityform_biblio");
#endif
                this.m_webExternalHost_biblio.SetHtmlString(strHtml,
                    "entityform_biblio");

                // ���û���޸�BiblioRecPath���Ͳ��ܰ�MARC�༭���е���Ŀ��¼�޸ģ�������BiblioChanged�Ѿ�Ϊtrue�����ܻᵼ�º�����ԭ����Ŀ��¼����������Զ�����ĸ�����
                this.BiblioRecPath = strOutputBiblioRecPath; // 2008/6/24 new add

                if (bCataloging == true)
                {
                    if (results != null && results.Length >= 3)
                        strXml = results[2];

                    if (bError == false)    // 2008/6/24 new add
                    {
                        // return:
                        //      -1  error
                        //      0   �յļ�¼
                        //      1   �ɹ�
                        int nRet = SetBiblioRecordToMarcEditor(strXml,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        // 2008/11/13 new add
                        if (nRet == 0)
                            MessageBox.Show(this, "���棺��ǰ��Ŀ��¼ '" + strOutputBiblioRecPath + "' ��һ���ռ�¼");

                        this.BiblioChanged = false;

                        // 2009/10/24 new add
                        // ����998$t ����ReadOnly״̬
                        string strTargetBiblioRecPath = this.m_marcEditor.Record.Fields.GetFirstSubfield("998", "t");
                        if (String.IsNullOrEmpty(strTargetBiblioRecPath) == false)
                        {
                            if (this.LinkedRecordReadonly == true)
                            {
                                // TODO: װ��Ŀ���¼�������ǰȫ������(����998)
                                this.m_marcEditor.ReadOnly = true;
                            }
                        }
                        else
                        {
                            // �����Ҫ���ָ��ɱ༭״̬
                            if (this.m_marcEditor.ReadOnly != false)
                                this.m_marcEditor.ReadOnly = false;
                        }

                        // ע���ǲɹ������⣬Ҳ�����趨Ŀ���¼·��
                        // TODO: δ���������ӡ��յ�⡱��ɫ�������Ŀ���ǲ����趨Ŀ���¼·����
                        /*
                        // ���ݵ�ǰ���ǲ��ǲɹ������⣬����������Ŀ���¼����ť�Ƿ�ΪEnabled
                        if (this.MainForm.IsOrderWorkDb(this.BiblioDbName) == true)
                            this.toolStripButton_setTargetRecord.Enabled = true;
                        else
                            this.toolStripButton_setTargetRecord.Enabled = false;
                         * */

                    }
                }

                if (bError == true)
                {
                    strError = strErrorText;
                    goto ERROR1;
                }
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ��XML��ʽ����Ŀ��¼װ��MARC����
        // return:
        //      -1  error
        //      0   �յļ�¼
        //      1   �ɹ�
        int SetBiblioRecordToMarcEditor(string strXml,
            out string strError)
        {
            strError = "";

            string strMarcSyntax = "";
            string strOutMarcSyntax = "";
            string strMarc = "";

            // ����XML����
            this.m_strOriginBiblioXml = strXml;

            // 2008/11/13 new add
            if (String.IsNullOrEmpty(strXml) == true)
            {
                strMarc = "012345678901234567890123";
                // this.m_marcEditor.Marc = strMarc;
                this.SetMarc(strMarc);
                return 0;
            }
            else
            {

                // ��XML��ʽת��ΪMARC��ʽ
                // �Զ������ݼ�¼�л��MARC�﷨
                int nRet = MarcUtil.Xml2Marc(strXml,
                    true,
                    strMarcSyntax,
                    out strOutMarcSyntax,
                    out strMarc,
                    out strError);
                if (nRet == -1)
                {
                    strError = "XMLת����MARC��¼ʱ����: " + strError;
                    return -1;
                }
                // this.m_marcEditor.Marc = strMarc;
                this.SetMarc(strMarc);
                return 1;
            }
        }

        // �����Ŀ�й���Ϣ
        void ClearBiblio()
        {
            // this.m_marcEditor.Marc = "012345678901234567890123";
            this.SetMarc("012345678901234567890123");
            this.BiblioChanged = false;

            // Global.SetHtmlString(this.webBrowser_biblioRecord, "(�հ�)");
            this.m_webExternalHost_biblio.SetHtmlString("(�հ�)",
                "entityform_error");
        }

        #region ԭ����entity������ش���

        /*
        // ���ʵ���й���Ϣ
        void ClearItems()
        {
            this.listView_items.Items.Clear();
            this.bookitems = new BookItemCollection();
        }
         
        // װ��ʵ���¼
        int LoadEntityRecords(string strBiblioRecPath,
            out string strError)
        {
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����װ�����Ϣ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();


            try
            {
                // string strHtml = "";

                EntityInfo[] entities = null;

                long lRet = Channel.GetEntities(
                    stop,
                    strBiblioRecPath,
                    out entities,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                this.ClearItems();

                // Debug.Assert(false, "");

                if (lRet == 0)
                    return 0;

                Debug.Assert(entities != null, "");


                for (int i = 0; i < entities.Length; i++)
                {
                    if (entities[i].ErrorCode != ErrorCodeValue.NoError)
                    {
                        strError = "·��Ϊ '" + entities[i].OldRecPath + "' �Ĳ��¼װ���з�������: " + entities[i].ErrorInfo;  // NewRecPath
                        return -1;
                    }

                    // ����һ�����xml��¼��ȡ���й���Ϣ����listview��
                    BookItem bookitem = new BookItem();

                    int nRet = bookitem.SetData(entities[i].OldRecPath, // NewRecPath
                             entities[i].OldRecord,
                             entities[i].OldTimestamp,
                             out strError);
                    if (nRet == -1)
                        return -1;

                    if (entities[i].ErrorCode == ErrorCodeValue.NoError)
                        bookitem.Error = null;
                    else
                        bookitem.Error = entities[i];

                    this.bookitems.Add(bookitem);


                    bookitem.AddToListView(this.listView_items);
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
         

        // �������˵�
        private void listView_items_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            bool bHasBillioLoaded = false;

            if (String.IsNullOrEmpty(this.BiblioRecPath) == false)
                bHasBillioLoaded = true;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("�޸�(&M)");
            menuItem.Click += new System.EventHandler(this.menu_modifyEntity_Click);
            if (this.listView_items.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("����(&N)");
            menuItem.Click += new System.EventHandler(this.menu_newEntity_Click);
            if (bHasBillioLoaded == false)
                menuItem.Enabled = false;

            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

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


            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // �ı����
            menuItem = new MenuItem("�ı����(&B)");
            menuItem.Click += new System.EventHandler(this.menu_changeParent_Click);
            if (this.listView_items.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("���ɾ��(&D)");
            menuItem.Click += new System.EventHandler(this.menu_deleteEntity_Click);
            if (this.listView_items.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("����ɾ��(&U)");
            menuItem.Click += new System.EventHandler(this.menu_undoDeleteEntity_Click);
            if (this.listView_items.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView_items, new Point(e.X, e.Y));
        }

         *         public int CountOfVisibleBookItems()
        {
            return this.listView_items.Items.Count;
        }

        public int IndexOfVisibleBookItems(BookItem bookitem)
        {
            for (int i = 0; i < this.listView_items.Items.Count; i++)
            {
                BookItem cur = (BookItem)this.listView_items.Items[i].Tag;

                if (cur == bookitem)
                    return i;
            }

            return -1;
        }

        public BookItem GetAtVisibleBookItems(int nIndex)
        {
            return (BookItem)this.listView_items.Items[nIndex].Tag;
        }

                 */

        #endregion


        // �ı� ȫ������ ��ť��״̬
        void SetSaveAllButtonState(bool bEnable)
        {
            // 2011/11/8
            if (this.m_bDeletedMode == true)
            {
                this.button_save.Enabled = true;
                this.toolStripButton_saveAll.Enabled = true;
                return;
            }

            if (this.EntitiesChanged == true
                || this.IssuesChanged == true
                || this.BiblioChanged == true
                || this.ObjectChanged == true
                || this.OrdersChanged == true
                || this.CommentsChanged == true
                )
            {
                this.button_save.Enabled = bEnable;
                this.toolStripButton_saveAll.Enabled = bEnable;
            }
            else
            {
                this.button_save.Enabled = false;
                this.toolStripButton_saveAll.Enabled = false;
            }
        }

        int m_nInDisable = 0;

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {

            this.textBox_queryWord.Enabled = bEnable;

            if (this.ItemsPageVisible == false)
            {
                this.textBox_itemBarcode.Enabled = false;
                this.button_register.Enabled = false;
            }
            else
            {
                this.textBox_itemBarcode.Enabled = bEnable;
                this.button_register.Enabled = bEnable;
            }

            if (bEnable == false)
                this.button_save.Enabled = bEnable;
            else
                SetSaveAllButtonState(bEnable);

            this.button_search.Enabled = bEnable;
            this.toolStripButton_option.Enabled = bEnable;

            this.entityControl1.Enabled = (this.m_bDeletedMode == true) ? false : bEnable;
            this.issueControl1.Enabled = (this.m_bDeletedMode == true) ? false : bEnable;
            this.orderControl1.Enabled = (this.m_bDeletedMode == true) ? false : bEnable;
            this.commentControl1.Enabled = (this.m_bDeletedMode == true) ? false : bEnable;
            this.binaryResControl1.Enabled = (this.m_bDeletedMode == true) ? false : bEnable;

            this.comboBox_from.Enabled = bEnable;
            this.checkedComboBox_biblioDbNames.Enabled = bEnable;
            this.comboBox_matchStyle.Enabled = bEnable;

            // this.checkBox_autoDetectQueryBarcode.Enabled = bEnable;
            this.checkBox_autoSavePrev.Enabled = bEnable;

            this.textBox_biblioRecPath.Enabled = bEnable;

            this.toolStripButton_clear.Enabled = bEnable;

            if (this.toolStrip_marcEditor.Enabled != bEnable)
                this.toolStrip_marcEditor.Enabled = bEnable;

            bool bValue = (this.m_bDeletedMode == true) ? false : bEnable;  // 2012/3/19
            if (this.m_marcEditor.Enabled != bValue)
                this.m_marcEditor.Enabled = bValue;
        }

        // ��ȡ��Ŀ��¼�ľֲ�
        int GetBiblioPart(string strBiblioRecPath,
            string strBiblioXml,
            string strPartName,
            out string strResultValue,
            out string strError)
        {
            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("���ڻ�ȡ��Ŀ��¼�ľֲ� -- '" + strPartName + "'...");
            Progress.BeginLoop();
            try
            {
                // stop.SetMessage("����װ����Ŀ��¼ " + strBiblioRecPath + " ...");

                long lRet = Channel.GetBiblioInfo(
                    Progress,
                    strBiblioRecPath,
                    strBiblioXml,
                    strPartName,    // ����'@'����
                    out strResultValue,
                    out strError);
                return (int)lRet;
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }
        }

        string GetMacroValue(string strMacroName)
        {
            // return strMacroName + "--";
            string strError = "";
            string strResultValue = "";
            int nRet = 0;

            // ��Ŀ��¼XML��ʽ
            string strXmlBody = "";

            if (Global.IsAppendRecPath(this.BiblioRecPath) == true
                || this.BiblioChanged == true)  // 2010/12/5 add
            {
                // �����¼·����������һ��������¼�¼���Ǿ���Ҫ׼����strXmlBody���Ա��ȡ���ʱ��ʹ��
                nRet = this.GetBiblioXml(
                    "", // ��ʹ�Ӽ�¼·���п�marc��ʽ
                    true,   // ������ԴID
                    out strXmlBody,
                    out strError);
                if (nRet == -1)
                    return strError;
            }


            // ��ȡ��Ŀ��¼�ľֲ�
            nRet = GetBiblioPart(this.BiblioRecPath,
                strXmlBody,
                strMacroName,
                out strResultValue,
                out strError);
            if (nRet == -1)
            {
                if (String.IsNullOrEmpty(strResultValue) == true)
                    return strError;

                return strResultValue;
            }

            return strResultValue;
        }


        // ȫ������
        private void button_save_Click(object sender, EventArgs e)
        {
            this.EnableControls(false);
            this.m_nInDisable++;
            try
            {
                if (string.IsNullOrEmpty(this.BiblioRecPath) == true)
                    toolStripButton1_marcEditor_saveTo_Click(null, null);
                else
                    DoSaveAll();
            }
            finally
            {
                this.m_nInDisable--;
                this.EnableControls(true);
            }
        }


        // �ύ���б�������
        // parameters:
        //      strStyle    ���displaysuccess ��ʾ���ĳɹ���Ϣ�ڿ�ܴ��ڵ�״̬�� verifydata ����У���¼����Ϣ(ע���Ƿ�У�黹Ҫȡ��������״̬)
        //                  searchdup ��Ȼ�Ա�����û�����ã����ǿ��Դ��ݵ��¼�����SaveBiblioToDatabase()
        // return:
        //      -1  �д���ʱ���ų���Щ��Ϣ����ɹ���
        //      0   �ɹ���
        /// <summary>
        /// ȫ������
        /// </summary>
        /// <param name="strStyle">���淽ʽ���� displaysuccess / verifydata / searchdup ֮һ���߶��ż����϶��ɡ�displaysuccess ��ʾ���ĳɹ���Ϣ�ڿ�ܴ��ڵ�״̬��; verifydata ����ɹ�����У���¼����Ϣ(ע���Ƿ�У�黹Ҫȡ��������״̬); searchdup ����ɹ����Ͳ�����Ϣ</param>
        /// <returns>-1: �д���ʱ���ų���Щ��Ϣ����ɹ���0: �ɹ���</returns>
        public int DoSaveAll(string strStyle = "displaysuccess,verifydata,searchdup")
        {
            bool bBiblioSaved = false;
            int nRet = 0;
            string strText = "";
            int nErrorCount = 0;

            bool bDisplaySuccess = StringUtil.IsInList("displaysuccess", strStyle);
            bool bVerifyData = StringUtil.IsInList("verifydata", strStyle);
            // bool bForceVerifyData = StringUtil.IsInList("forceverifydata", strStyle);

            bool bVerified = false;

            if (this.m_verifyViewer != null)
                this.m_verifyViewer.Clear();

            string strHtml = "";

            if (this.BiblioChanged == true
                || Global.IsAppendRecPath(this.BiblioRecPath) == true
                || this.m_bDeletedMode == true /* 2011/11/8 */)
            {
                // 2014/7/3
                if (bVerifyData == true
&& this.ForceVerifyData == true)
                {
                    GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                    e1.FocusedControl = this.m_marcEditor;

                    // 0: û�з���У�����; 1: ����У�龯��; 2: ����У�����
                    nRet = this.VerifyData(this, e1, true);
                    if (nRet == 2)
                    {
                        MessageBox.Show(this, "MARC ��¼��У�鷢���д����ܾ����档���޸� MARC ��¼�����±���");
                        return -1;
                    }

                    bVerified = true;
                }

                // ������Ŀ��¼�����ݿ�
                // return:
                //      -1  ����
                //      0   û�б���
                //      1   �Ѿ�����
                nRet = SaveBiblioToDatabase(true,
                    out strHtml,
                    strStyle);
                if (nRet == 1)
                {
                    bBiblioSaved = true;
                    strText += "��Ŀ��Ϣ";
                }
                if (nRet == -1)
                {
                    nErrorCount++;
                }
            }

            bool bOrdersSaved = false;
            // �ύ������������
            // return:
            //      -1  ����
            //      0   û�б�Ҫ����
            //      1   ����ɹ�
            nRet = this.orderControl1.DoSaveItems();
            if (nRet == 1)
            {
                bOrdersSaved = true;
                if (strText != "")
                    strText += " ";
                strText += "�ɹ���Ϣ";
            }
            if (nRet == -1)
            {
                nErrorCount++;

                // 2013/1/18
                // ���������Ϣ���治�ɹ�����Ҫ������������������Ϣ������Ҫ��Ϊ�˶������ջ��ڿ��ǣ������ڶ�����Ϣ����ʧ�ܵ�����¼��������������������µĲ���Ϣ
                return -1;
            }

            bool bIssuesSaved = false;
            bool bIssueError = false;
            // �ύ�ڱ�������
            // return:
            //      -1  ����
            //      0   û�б�Ҫ����
            //      1   ����ɹ�
            nRet = this.issueControl1.DoSaveItems();
            if (nRet == 1)
            {
                bIssuesSaved = true;
                if (strText != "")
                    strText += " ";
                strText += "����Ϣ";
            }
            if (nRet == -1)
            {
                nErrorCount++;
                bIssueError = true;

                // 2013/1/18
                // �������Ϣ���治�ɹ�����Ҫ������������������Ϣ������Ҫ��Ϊ���ڿ����ջ��ڿ��ǣ�����������Ϣ����ʧ�ܵ�����¼��������������������µĲ���Ϣ
                return -1;
            }

            bool bEntitiesSaved = false;

            // ע�����ڿ��ǵ����������Ϣ���治�ɹ����򲻱������Ϣ�����ⷢ����һ��
            if (bIssueError == false)
            {
                // �ύʵ�屣������
                // return:
                //      -1  ����
                //      0   û�б�Ҫ����
                //      1   ����ɹ�
                nRet = this.entityControl1.DoSaveItems();
                if (nRet == 1)
                {
                    bEntitiesSaved = true;
                    if (strText != "")
                        strText += " ";
                    strText += "����Ϣ";
                }
                if (nRet == -1)
                {
                    nErrorCount++;
                }
            }

            bool bCommentsSaved = false;
            // �ύ��ע��������
            // return:
            //      -1  ����
            //      0   û�б�Ҫ����
            //      1   ����ɹ�
            nRet = this.commentControl1.DoSaveItems();
            if (nRet == 1)
            {
                bCommentsSaved = true;
                if (strText != "")
                    strText += " ";
                strText += "��ע��Ϣ";
            }
            if (nRet == -1)
            {
                nErrorCount++;
            }

            bool bObjectSaved = false;
            string strError = "";

            // �������Ŀ���ܵ�ʱ����������������Դ����������Ŀ��¼�ݻ�Ϊ�ռ�¼
            if (this.Cataloging == true)
            {
                // �ύ���󱣴�����
                // return:
                //		-1	error
                //		>=0 ʵ�����ص���Դ������
                nRet = this.binaryResControl1.Save(out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, "���������Ϣʱ����: " + strError);
                    nErrorCount++;
                }

                if (nRet >= 1)
                {
                    bObjectSaved = true;
                    if (strText != "")
                        strText += " ";
                    strText += "������Ϣ";

                    /*
                    string strSavedBiblioRecPath = this.BiblioRecPath;

                    // ˢ����Ŀ��¼��ʱ���
                    string strOutputBiblioRecPath = "";
                    string strXml = "";
                    nRet = LoadBiblioRecord(this.BiblioRecPath,
                        "",
                        false,
                        out strOutputBiblioRecPath,
                        out strXml,
                        out strError);
                    if (nRet == -1)
                    {
                        // �����ȡ��¼ʧ�ܣ�����ԭ����Ŀ��¼·�����ݻ٣���Ҫ�ָ�
                        if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
                        {
                            this.BiblioRecPath = strSavedBiblioRecPath;
                        }

                        MessageBox.Show(this, strError + "\r\n\r\nע�⣺��ǰ�����ڵ���Ŀ��¼ʱ�������û����ȷˢ�£��⽫���º�̵ı�����Ŀ��¼��������ʱ�����ƥ�䱨��");
                        nErrorCount++;
                    }
                    */
                }
            }

            if (string.IsNullOrEmpty(strHtml) == false)
            {
                this.m_webExternalHost_biblio.SetHtmlString(strHtml,
"entityform_biblio");
            }

            if (bDisplaySuccess == true)
            {
                if (bEntitiesSaved == true
                    || bBiblioSaved == true
                    || bIssuesSaved == true
                    || bOrdersSaved == true
                    || bObjectSaved == true
                    || bCommentsSaved == true)
                    this.MainForm.StatusBarMessage = strText + " ���� �ɹ�";
            }

            if (nErrorCount > 0)
            {
                return -1;
            }

            // ����ɹ�����У�� MARC ��¼
            if (bVerifyData == true
                && this.AutoVerifyData == true
                && bVerified == false)
            {
                API.PostMessage(this.Handle, WM_VERIFY_DATA, 0, 0);
            }

            return 0;
        }



        /*
        void DisplayErrorInfo(EntityInfo[] errorinfos)
        {
            if (errorinfos == null)
                return;

            for (int i = 0; i < errorinfos.Length; i++)
            {
                if (errorinfos[i].ErrorCode == ErrorCodeValue.NoError)
                    continue;   // Խ��һ����Ϣ

                XmlDocument dom = new XmlDocument();

                string strNewXml = errorinfos[i].NewRecord;
                string strOldXml = errorinfos[i].OldRecord;

                if (String.IsNullOrEmpty(strNewXml) == false)
                {
                    dom.LoadXml(strNewXml);
                }
                else if (String.IsNullOrEmpty(strOldXml) == false)
                {
                    dom.LoadXml(strOldXml);
                }
                else {
                    // �Ҳ�����������λ
                    Debug.Assert(false, "�Ҳ�����λ������");
                    // �Ƿ񵥶���ʾ����?
                    continue;
                }

                string strBarcode = DomUtil.GetElementText(dom.DocumentElement, "barcode");

                if (String.IsNullOrEmpty(strBarcode) == true)
                {
                    Debug.Assert(false, "DOM��û�зǿյ�<barcode>Ԫ��ֵ");
                    continue;
                }

                BookItem bookitem = this.bookitems.GetItem(strBarcode);

                if (bookitem == null)
                {
                    Debug.Assert(false, "������bookitems��û���ҵ�");
                    continue;
                }

                bookitem.ErrorInfo = errorinfos[i].ErrorInfo;
                bookitem.RefreshListView();
            }
        }
         */

        string GetBiblioQueryString()
        {
            string strText = this.textBox_queryWord.Text;
            int nRet = strText.IndexOf(';');
            if (nRet != -1)
            {
                strText = strText.Substring(0, nRet).Trim();
                this.textBox_queryWord.Text = strText;
            }

            /*
            if (this.checkBox_autoDetectQueryBarcode.Checked == true)
            {
                if (strText.Length == 13)
                {
                    string strHead = strText.Substring(0, 3);
                    if (strHead == "978")
                    {
                        this.textBox_queryWord.Text = strText + " ;�Զ���" + strText.Substring(3, 9) + "������";
                        return strText.Substring(3, 9);
                    }
                }
            }*/

            return strText;
        }

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

        // ���м���
        private void button_search_Click(object sender, EventArgs e)
        {
            string strError = "";

            ActivateBrowseWindow(false);

            this.browseWindow.RecordsList.Items.Clear();

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("���ڼ��� ...");
            Progress.BeginLoop();

            this.browseWindow.stop = Progress;

            //this.button_search.Enabled = false;
            this.EnableControls(false);

            m_nInSearching++;

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

                string strMatchStyle = BiblioSearchForm.GetCurrentMatchStyle(this.comboBox_matchStyle.Text);
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
                    // 2009/11/5 new add
                    if (strMatchStyle == "null")
                    {
                        strError = "������ֵ��ʱ���뱣�ּ�����Ϊ��";
                        goto ERROR1;
                    }
                }

                string strQueryWord = GetBiblioQueryString();


                string strQueryXml = "";
                long lRet = Channel.SearchBiblio(Progress,
                    this.checkedComboBox_biblioDbNames.Text,    // "<ȫ��>",
                    strQueryWord,   // this.textBox_queryWord.Text,
                    this.MaxSearchResultCount,  // 1000
                    strFromStyle,
                    strMatchStyle,
                    this.Lang,
                    null,   // strResultSetName
                    "",    // strSearchStyle
                    "", // strOutputStyle
                    out strQueryXml,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                // TODO: ������1000�������ƣ�������Ϊ�������ã���CfgDlg��

                long lHitCount = lRet;

                if (lHitCount == 0)
                {
                    strError = "��;�� '" + strFromStyle+ "' ���� '" +strQueryWord + "' û������";
                    goto ERROR1;
                }

                if (lHitCount > 1)
                    this.ShowBrowseWindow(lHitCount);

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (Progress != null)
                    {
                        if (Progress.State != 0)
                        {
                            // MessageBox.Show(this, "�û��ж�");
                            break;  // �Ѿ�װ��Ļ���
                        }
                    }

                    Progress.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    lRet = Channel.GetSearchResult(
                        Progress,
                        null,   // strResultSetName
                        lStart,
                        lPerCount,
                        "id,cols",
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                    {
                        if (Progress.State != 0)
                        {
                            // MessageBox.Show(this, "�û��ж�");
                            break;
                        }

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

                        Global.AppendNewLine(
                            this.browseWindow.RecordsList,
                            searchresults[i].Path,
                            searchresults[i].Cols);
                    }

                    lStart += searchresults.Length;
                    // lCount -= searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;
                }

                if (lHitCount == 1)
                    this.browseWindow.LoadFirstDetail(true);

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");

                // this.button_search.Enabled = true;
                this.EnableControls(true);

                m_nInSearching--;

            }

            this.textBox_queryWord.SelectAll();

            // �����л�������textbox
            /*
            this.textBox_itemBarcode.SelectAll();
            this.textBox_itemBarcode.Focus(); 
             * */
            this.SwitchFocus(ITEM_BARCODE);


            return;

        ERROR1:
            CloseBrowseWindow();
            MessageBox.Show(this, strError);
            // �����Իص��ּ�����
            /*
            this.textBox_queryWord.Focus();
            this.textBox_queryWord.SelectAll();
             * */
            this.SwitchFocus(BIBLIO_SEARCHTEXT);
        }

        void CloseBrowseWindow()
        {
            if (this.browseWindow != null)
            {
                this.browseWindow.Close();
                this.browseWindow = null;
            }
        }

        void ShowBrowseWindow(long lHitCount)
        {
            if (this.browseWindow.Visible == false)
                this.MainForm.AppInfo.LinkFormState(this.browseWindow, "browseWindow_state");

            this.browseWindow.Visible = true;

            // 2014/7/8
            if (this.browseWindow.WindowState == FormWindowState.Minimized)
                this.browseWindow.WindowState = FormWindowState.Normal;

            if (lHitCount != -1)
                this.browseWindow.Text = "���� "+lHitCount.ToString()+" ���ּ�¼�������ѡ��һ��";
        }

        void ActivateBrowseWindow(bool bShow)
        {
            if (this.browseWindow == null
                || (this.browseWindow != null && this.browseWindow.IsDisposed == true))
            {
                this.browseWindow = new BrowseSearchResultForm();
                MainForm.SetControlFont(this.browseWindow, this.MainForm.DefaultFont);

                this.browseWindow.MainForm = this.MainForm; // 2009/2/17 new add
                this.browseWindow.Text = "���ж����ּ�¼�������ѡ��һ��";
                this.browseWindow.FormClosed -= new FormClosedEventHandler(browseWindow_FormClosed);
                this.browseWindow.FormClosed += new FormClosedEventHandler(browseWindow_FormClosed);
                // this.browseWindow.MdiParent = this.MainForm;
                if (bShow == true)
                {
                    this.MainForm.AppInfo.LinkFormState(this.browseWindow, "browseWindow_state");
                    this.browseWindow.Show();
                }

                this.browseWindow.OpenDetail -= new OpenDetailEventHandler(browseWindow_OpenDetail);
                this.browseWindow.OpenDetail += new OpenDetailEventHandler(browseWindow_OpenDetail);
            }
            else
            {
                if (this.browseWindow.Visible == false
                    && bShow == true)
                {
                    /*
                    this.MainForm.AppInfo.LinkFormState(this.browseWindow, "browseWindow_state");
                    this.browseWindow.Visible = true;
                     * */
                    ShowBrowseWindow(-1);
                }

                this.browseWindow.BringToFront();
                this.browseWindow.RecordsList.Items.Clear();
            }
        }

        void browseWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (browseWindow != null)
            {
                this.MainForm.AppInfo.UnlinkFormState(browseWindow);
                // this.browseWindow = null;
            }
        }

        /*
        // װ����
        void browseWindow_OpenDetail(object sender, OpenDetailEventArgs e)
        {
            if (e.Paths.Length == 0)
                return;

            string strBiblioRecPath = e.Paths[0];

            // ������һ�����������
            Debug.Assert(m_nInSearching == 0, "");


            this.LoadRecord(strBiblioRecPath);
        }
         */

        void browseWindow_OpenDetail(object sender, OpenDetailEventArgs e)
        {
            if (e.Paths.Length == 0)
                return;

            string strBiblioRecPath = e.Paths[0];

            // ������һ�����������
            // TODO: ��ʵ�������Ѿ�û�б�Ҫ�ˡ���Ϊ�µ�LoadRecord()�����Ѿ������m_nInSearching
            if (m_nInSearching > 0)
            {
                /*
                this.m_strTempBiblioRecPath = strBiblioRecPath;
                API.PostMessage(this.Handle, WM_PREPARE, 0, 0);
                 * */
                this.AddToPendingList(strBiblioRecPath, "");
                return;
            }


            int nRet = this.LoadRecordOld(strBiblioRecPath, "", true);
            // 2009/11/6 new add
            if (nRet == 2)
            {
                this.AddToPendingList(strBiblioRecPath, "");
                return;
            }
        }

        // 
        /// <summary>
        /// ����ʱ�Ƿ��Զ�����
        /// </summary>
        public bool AutoSearchDup
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
    "entity_form",
    "search_dup_when_saving",
    false);
            }
        }

        // 
        /// <summary>
        /// ����ʱ�Ƿ��Զ�У�����ݡ��Զ�У��������������д���Ȼ����
        /// </summary>
        public bool AutoVerifyData
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
    "entity_form",
    "verify_data_when_saving",
    false);
            }
        }

        /// <summary>
        /// ����ʱ�Ƿ�ǿ��У�����ݡ�У��ʱ������������д���ܾ�����
        /// </summary>
        public bool ForceVerifyData
        {
            get
            {
#if NO
                return this.MainForm.AppInfo.GetBoolean(
    "entity_form",
    "verify_data_when_saving",
    false);
#endif
                if (this.Channel == null)
                    return false;
                return StringUtil.IsInList("client_forceverifydata", this.Channel.Rights);
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
                case WM_SEARCH_DUP:
                    this.SearchDup();
                    return;
                case WM_FILL_MARCEDITOR_SCRIPT_MENU:
                    // ��ʾCtrl+A�˵�
                    if (this.MainForm.PanelFixedVisible == true)
                        this.AutoGenerate(this.m_marcEditor,
                            new GenerateDataEventArgs(),
                            true);
                    return;
                case WM_VERIFY_DATA:
                    {
                        GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                        e1.FocusedControl = this.m_marcEditor;

                        this.VerifyData(this, e1, true);
                        return;
                    }
                case WM_SWITCH_FOCUS:
                    {
                        if ((int)m.WParam == BIBLIO_SEARCHTEXT)
                        {
                            this.textBox_queryWord.SelectAll();
                            this.textBox_queryWord.Focus();
                        }

                        else if ((int)m.WParam == ITEM_BARCODE)
                        {
                            this.textBox_itemBarcode.SelectAll();
                            this.textBox_itemBarcode.Focus();
                        }
                        else if ((int)m.WParam == ITEM_LIST)
                        {
                            bool bFound = this.ActivateItemsPage();
                            if (bFound == true)
                                this.entityControl1.Focus();
                        }
                        else if ((int)m.WParam == ORDER_LIST)
                        {
                            bool bFound = this.ActivateOrdersPage();
                            if (bFound == true)
                                this.orderControl1.Focus();
                        }
                        else if ((int)m.WParam == COMMENT_LIST)
                        {
                            bool bFound = this.ActivateCommentsPage();
                            if (bFound == true)
                                this.commentControl1.Focus();
                        }
                        else if ((int)m.WParam == ISSUE_LIST)
                        {
                            bool bFound = this.ActivateIssuesPage();
                            if (bFound == true)
                                this.issueControl1.Focus();
                        }
                        else if ((int)m.WParam == MARC_EDITOR)
                        {
                            if (this.m_marcEditor.FocusedFieldIndex == -1)
                                this.m_marcEditor.FocusedFieldIndex = 0;

                            if (this.m_marcEditor.Focused == false)
                                this.m_marcEditor.Focus();
                        }

                        return;
                    }
                // break;

            }
            base.DefWndProc(ref m);
        }

        // �ּ�����textbox������
        private void textBox_queryWord_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_search;
        }

        private void textBox_queryWord_Leave(object sender, EventArgs e)
        {
            // 2008/12/15 new add
            this.AcceptButton = null;
        }

        private void textBox_itemBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_register;
            m_strFocusedPart = "itembarcode";
        }

        private void textBox_itemBarcode_Leave(object sender, EventArgs e)
        {
            // 2008/12/9 new add
            this.AcceptButton = null;
        }

        /// <summary>
        /// �Ƿ�ҪУ��������
        /// </summary>
        public bool NeedVerifyItemBarcode
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "entity_form",
                    "verify_item_barcode",
                    false);
            }
        }

#if NO
        // ��ʽУ�������
        // return:
        //      -2  ������û������У�鷽�����޷�У��
        //      -1  error
        //      0   ���ǺϷ��������
        //      1   �ǺϷ��Ķ���֤�����
        //      2   �ǺϷ��Ĳ������
        /// <summary>
        /// ��ʽУ�������
        /// </summary>
        /// <param name="strBarcode">ҪУ��������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-2  ������û������У�鷽�����޷�У��</para>
        /// <para>-1  ����</para>
        /// <para>0   ���ǺϷ��������</para>
        /// <para>1   �ǺϷ��Ķ���֤�����</para>
        /// <para>2   �ǺϷ��Ĳ������</para>
        /// </returns>
        public int VerifyBarcode(
            string strBarcode,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("����У������ ...");
            Progress.BeginLoop();

            /*
            this.Update();
            this.MainForm.Update();
             * */

            try
            {
                long lRet = Channel.VerifyBarcode(
                    Progress,
                    strBarcode,
                    out strError);
                if (lRet == -1)
                {
                    if (Channel.ErrorCode == DigitalPlatform.CirculationClient.localhost.ErrorCode.NotFound)
                        return -2;
                    goto ERROR1;
                }
                return (int)lRet;
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");

                EnableControls(true);
            }
        ERROR1:
            return -1;
        }
#endif

        // ��Ǽ�
        private void button_register_Click(object sender, EventArgs e)
        {
            this.DoRegisterEntity();
        }

        // 2006/12/3 new add
        // ���ݲ������ װ��һ���ᣬ����װ����
        // parameters:
        //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ���ݲ�����ţ�װ����Ŀ��¼
        /// </summary>
        /// <param name="strItemBarcode">�������</param>
        /// <param name="bAutoSavePrev">�Ƿ��Զ����洰������ǰ���޸�</param>
        /// <returns>
        /// <para>-1  ����</para>
        /// <para>0   û���ҵ�</para>
        /// <para>1   �ҵ�</para>
        /// </returns>
        public int LoadItemByBarcode(string strItemBarcode,
            bool bAutoSavePrev)
        {
            if (string.IsNullOrEmpty(strItemBarcode) == true)
            {
                MessageBox.Show(this, "��������һ��������Ų��ܽ��м���");
                return -1;
            }
            this.m_nChannelInUse++;
            if (this.m_nChannelInUse > 1)
            {
                this.m_nChannelInUse--;
                MessageBox.Show(this, "ͨ���Ѿ���ռ�á����Ժ�����");
                return -1;
            }
            try
            {

                int nRet = 0;
                // TODO: �ⲿ����ʱ��Ҫ���Զ���items page����

                if (this.EntitiesChanged == true
                    || this.IssuesChanged == true
                    || this.BiblioChanged == true
                    || this.ObjectChanged == true
                    || this.OrdersChanged == true
                    || this.CommentsChanged == true)
                {
                    if (bAutoSavePrev == false)
                    {
                        // ������δ����
                        DialogResult result = MessageBox.Show(this,
                            "��ǰ�������� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档\r\n\r\n��װ���µ�ʵ����Ϣ��ǰ���Ƿ��ȱ�����Щ�޸�? ",
                            "EntityForm",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);
                        if (result == DialogResult.Cancel)
                        {
                            MessageBox.Show(this, "����װ������� (�������Ϊ '" + strItemBarcode + "' )");
                            return -1;
                        }
                        if (result == DialogResult.Yes)
                        {
                            /*
                            // ���浱ǰ����Ϣ
                            nRet = this.entityControl1.DoSaveEntities();
                            if (nRet == -1)
                                return -1; // ������һ������
                             * */
                            nRet = this.DoSaveAll();
                            if (nRet == -1)
                                return -1; // ������һ������

                        }
                    }
                    else
                    {
                        /*
                        // ���浱ǰ����Ϣ
                        nRet = this.entityControl1.DoSaveEntities();
                        if (nRet == -1)
                            return -1; // ������һ������
                         * */
                        nRet = this.DoSaveAll();
                        if (nRet == -1)
                            return -1; // ������һ������
                    }
                }


                // 2006/12/30 new add
                if (strItemBarcode != this.textBox_itemBarcode.Text)
                    this.textBox_itemBarcode.Text = strItemBarcode;

                // ע�������װ���item�����ں͵�ǰ�ֲ�ͬ���֣������ǰ��Ŀ���ݱ��޸Ĺ����ᾯ���Ƿ�(�ƻ���)װ�룬������Ŀ���ݲ��ᱻ���档����һ�����⡣
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = this.entityControl1.DoSearchEntity(this.textBox_itemBarcode.Text);

                // �����л�������������
                // this.SwitchFocus(ITEM_BARCODE);

                this.SwitchFocus(ITEM_LIST);

                return nRet;
            }
            finally
            {
                this.m_nChannelInUse--;
            }
        }

        // 2008/11/2 new add
        // ���ݲ��¼·�� װ��һ���ᣬ����װ����
        // parameters:
        //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ���ݲ��¼·����װ����Ŀ��¼
        /// </summary>
        /// <param name="strItemRecPath">���¼·��</param>
        /// <param name="bAutoSavePrev">�Ƿ��Զ����洰������ǰ���޸�</param>
        /// <returns>
        /// <para>-1  ����</para>
        /// <para>0   û���ҵ�</para>
        /// <para>1   �ҵ�</para>
        /// </returns>
        public int LoadItemByRecPath(string strItemRecPath,
            bool bAutoSavePrev)
        {
            this.m_nChannelInUse++;
            if (this.m_nChannelInUse > 1)
            {
                this.m_nChannelInUse--;
                MessageBox.Show(this, "ͨ���Ѿ���ռ�á����Ժ�����");
                return -1;
            }
            try
            {

                int nRet = 0;

                // TODO: �ⲿ����ʱ��Ҫ���Զ���items page����

                if (this.EntitiesChanged == true
                    || this.IssuesChanged == true
                    || this.BiblioChanged == true
                    || this.ObjectChanged == true
                    || this.OrdersChanged == true
                    || this.CommentsChanged == true)
                {
                    if (bAutoSavePrev == false)
                    {
                        // ������δ����
                        DialogResult result = MessageBox.Show(this,
                            "��ǰ�������� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档\r\n\r\n��װ���µ�ʵ����Ϣ��ǰ���Ƿ��ȱ�����Щ�޸�? ",
                            "EntityForm",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);
                        if (result == DialogResult.Cancel)
                        {
                            MessageBox.Show(this, "����װ������� (���¼·��Ϊ '" + strItemRecPath + "' )");
                            return -1;
                        }
                        if (result == DialogResult.Yes)
                        {
                            nRet = this.DoSaveAll();
                            if (nRet == -1)
                                return -1; // ������һ������

                        }
                    }
                    else
                    {
                        nRet = this.DoSaveAll();
                        if (nRet == -1)
                            return -1; // ������һ������
                    }
                }

                /*
                if (strItemBarcode != this.textBox_itemBarcode.Text)
                    this.textBox_itemBarcode.Text = strItemBarcode;
                 * */

                string strItemBarcode = "";
                BookItem result_item = null;

                // ע�������װ���item�����ں͵�ǰ�ֲ�ͬ���֣������ǰ��Ŀ���ݱ��޸Ĺ����ᾯ���Ƿ�(�ƻ���)װ�룬������Ŀ���ݲ��ᱻ���档����һ�����⡣
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = this.entityControl1.DoSearchItemByRecPath(strItemRecPath,
                    out result_item,
                    false);
                if (result_item != null)
                    strItemBarcode = result_item.Barcode;

                if (strItemBarcode != this.textBox_itemBarcode.Text)
                    this.textBox_itemBarcode.Text = strItemBarcode;

                // �����л�������������
                // this.SwitchFocus(ITEM_BARCODE);
                this.SwitchFocus(ITEM_LIST);

                return nRet;
            }
            finally
            {
                this.m_nChannelInUse--;
            }
        }

        // 2010/2/26 new add
        // ���ݲ��¼�ο�ID װ��һ���ᣬ����װ����
        // parameters:
        //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ���ݲ��¼�ο� ID��ת����Ŀ��¼
        /// </summary>
        /// <param name="strItemRefID">���¼�Ĳο� ID</param>
        /// <param name="bAutoSavePrev">�Ƿ��Զ����洰������ǰ���޸�</param>
        /// <returns>
        /// <para>-1  ����</para>
        /// <para>0   û���ҵ�</para>
        /// <para>1   �ҵ�</para>
        /// </returns>
        public int LoadItemByRefID(string strItemRefID,
            bool bAutoSavePrev)
        {
            this.m_nChannelInUse++;
            if (this.m_nChannelInUse > 1)
            {
                this.m_nChannelInUse--;
                MessageBox.Show(this, "ͨ���Ѿ���ռ�á����Ժ�����");
                return -1;
            }
            try
            {

                int nRet = 0;

                // TODO: �ⲿ����ʱ��Ҫ���Զ���items page����

                if (this.EntitiesChanged == true
                    || this.IssuesChanged == true
                    || this.BiblioChanged == true
                    || this.ObjectChanged == true
                    || this.OrdersChanged == true
                    || this.CommentsChanged == true)
                {
                    if (bAutoSavePrev == false)
                    {
                        // ������δ����
                        DialogResult result = MessageBox.Show(this,
                            "��ǰ�������� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档\r\n\r\n��װ���µ�ʵ����Ϣ��ǰ���Ƿ��ȱ�����Щ�޸�? ",
                            "EntityForm",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);
                        if (result == DialogResult.Cancel)
                        {
                            MessageBox.Show(this, "����װ������� (���¼·��Ϊ '" + strItemRefID + "' )");
                            return -1;
                        }
                        if (result == DialogResult.Yes)
                        {
                            nRet = this.DoSaveAll();
                            if (nRet == -1)
                                return -1; // ������һ������

                        }
                    }
                    else
                    {
                        nRet = this.DoSaveAll();
                        if (nRet == -1)
                            return -1; // ������һ������
                    }
                }

                string strItemBarcode = "";
                BookItem result_item = null;
                // ע�������װ���item�����ں͵�ǰ�ֲ�ͬ���֣������ǰ��Ŀ���ݱ��޸Ĺ����ᾯ���Ƿ�(�ƻ���)װ�룬������Ŀ���ݲ��ᱻ���档����һ�����⡣
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = this.entityControl1.DoSearchItemByRefID(strItemRefID,
                    out result_item);
                if (result_item != null)
                    strItemBarcode = result_item.Barcode;

                if (strItemBarcode != this.textBox_itemBarcode.Text)
                    this.textBox_itemBarcode.Text = strItemBarcode;

                // �����л�������������
                // this.SwitchFocus(ITEM_BARCODE);
                this.SwitchFocus(ITEM_LIST);

                return nRet;
            }
            finally
            {
                this.m_nChannelInUse--;
            }
        }

        // 2011/6/30 new add
        // ������ע��¼·�� װ��һ����ע��¼������װ����
        // parameters:
        //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ������ע��¼·����װ����Ŀ��¼
        /// </summary>
        /// <param name="strCommentRecPath">��ע��¼·��</param>
        /// <param name="bAutoSavePrev">�Ƿ��Զ����洰������ǰ���޸�</param>
        /// <returns>
        /// <para>-1  ����</para>
        /// <para>0   û���ҵ�</para>
        /// <para>1   �ҵ�</para>
        /// </returns>
        public int LoadCommentByRecPath(string strCommentRecPath,
            bool bAutoSavePrev)
        {
            this.m_nChannelInUse++;
            if (this.m_nChannelInUse > 1)
            {
                this.m_nChannelInUse--;
                MessageBox.Show(this, "ͨ���Ѿ���ռ�á����Ժ�����");
                return -1;
            }
            try
            {

                int nRet = 0;

                // TODO: �ⲿ����ʱ��Ҫ���Զ���items page����

                if (this.EntitiesChanged == true
                    || this.IssuesChanged == true
                    || this.BiblioChanged == true
                    || this.ObjectChanged == true
                    || this.OrdersChanged == true
                    || this.CommentsChanged == true)
                {
                    if (bAutoSavePrev == false)
                    {
                        // ������δ����
                        DialogResult result = MessageBox.Show(this,
                            "��ǰ�������� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档\r\n\r\n��װ���µ�ʵ����Ϣ��ǰ���Ƿ��ȱ�����Щ�޸�? ",
                            "EntityForm",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);
                        if (result == DialogResult.Cancel)
                        {
                            MessageBox.Show(this, "����װ����ע���� (��ע��¼·��Ϊ '" + strCommentRecPath + "' )");
                            return -1;
                        }
                        if (result == DialogResult.Yes)
                        {
                            nRet = this.DoSaveAll();
                            if (nRet == -1)
                                return -1; // ������һ������

                        }
                    }
                    else
                    {
                        nRet = this.DoSaveAll();
                        if (nRet == -1)
                            return -1; // ������һ������
                    }
                }

                CommentItem result_item = null;
                // ע�������װ���item�����ں͵�ǰ�ֲ�ͬ���֣������ǰ��Ŀ���ݱ��޸Ĺ����ᾯ���Ƿ�(�ƻ���)װ�룬������Ŀ���ݲ��ᱻ���档����һ�����⡣
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = this.commentControl1.DoSearchItemByRecPath(strCommentRecPath,
                    out result_item);

                this.SwitchFocus(COMMENT_LIST);

                return nRet;
            }
            finally
            {
                this.m_nChannelInUse--;
            }
        }

        // 2009/11/23 new add
        // ���ݶ������¼·�� װ��һ��������¼������װ����
        // parameters:
        //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ���ݶ������¼·����װ����Ŀ��¼
        /// </summary>
        /// <param name="strOrderRecPath">������¼·��</param>
        /// <param name="bAutoSavePrev">�Ƿ��Զ����洰������ǰ���޸�</param>
        /// <returns>
        /// <para>-1  ����</para>
        /// <para>0   û���ҵ�</para>
        /// <para>1   �ҵ�</para>
        /// </returns>
        public int LoadOrderByRecPath(string strOrderRecPath,
            bool bAutoSavePrev)
        {
            this.m_nChannelInUse++;
            if (this.m_nChannelInUse > 1)
            {
                this.m_nChannelInUse--;
                MessageBox.Show(this, "ͨ���Ѿ���ռ�á����Ժ�����");
                return -1;
            }
            try
            {

                int nRet = 0;

                // TODO: �ⲿ����ʱ��Ҫ���Զ���items page����

                if (this.EntitiesChanged == true
                    || this.IssuesChanged == true
                    || this.BiblioChanged == true
                    || this.ObjectChanged == true
                    || this.OrdersChanged == true
                    || this.CommentsChanged == true)
                {
                    if (bAutoSavePrev == false)
                    {
                        // ������δ����
                        DialogResult result = MessageBox.Show(this,
                            "��ǰ�������� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档\r\n\r\n��װ���µ�ʵ����Ϣ��ǰ���Ƿ��ȱ�����Щ�޸�? ",
                            "EntityForm",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);
                        if (result == DialogResult.Cancel)
                        {
                            MessageBox.Show(this, "����װ�붩������ (������¼·��Ϊ '" + strOrderRecPath + "' )");
                            return -1;
                        }
                        if (result == DialogResult.Yes)
                        {
                            nRet = this.DoSaveAll();
                            if (nRet == -1)
                                return -1; // ������һ������

                        }
                    }
                    else
                    {
                        nRet = this.DoSaveAll();
                        if (nRet == -1)
                            return -1; // ������һ������
                    }
                }

                /*
                if (strItemBarcode != this.textBox_itemBarcode.Text)
                    this.textBox_itemBarcode.Text = strItemBarcode;
                 * */

                OrderItem result_item = null;
                // ע�������װ���item�����ں͵�ǰ�ֲ�ͬ���֣������ǰ��Ŀ���ݱ��޸Ĺ����ᾯ���Ƿ�(�ƻ���)װ�룬������Ŀ���ݲ��ᱻ���档����һ�����⡣
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = this.orderControl1.DoSearchItemByRecPath(strOrderRecPath,
                    out result_item);

                this.SwitchFocus(ORDER_LIST);

                return nRet;
            }
            finally
            {
                this.m_nChannelInUse--;
            }
        }

        // 2010/4/27
        // �����ڼ�¼·�� װ��һ���ڼ�¼������װ����
        // parameters:
        //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// �����ڼ�¼·����װ����Ŀ��¼
        /// </summary>
        /// <param name="strIssueRecPath">�ڼ�¼·��</param>
        /// <param name="bAutoSavePrev">�Ƿ��Զ����洰������ǰ���޸�</param>
        /// <returns>
        /// <para>-1  ����</para>
        /// <para>0   û���ҵ�</para>
        /// <para>1   �ҵ�</para>
        /// </returns>
        public int LoadIssueByRecPath(string strIssueRecPath,
            bool bAutoSavePrev)
        {
            this.m_nChannelInUse++;
            if (this.m_nChannelInUse > 1)
            {
                this.m_nChannelInUse--;
                MessageBox.Show(this, "ͨ���Ѿ���ռ�á����Ժ�����");
                return -1;
            }
            try
            {

                int nRet = 0;

                // TODO: �ⲿ����ʱ��Ҫ���Զ���items page����

                if (this.EntitiesChanged == true
                    || this.IssuesChanged == true
                    || this.BiblioChanged == true
                    || this.ObjectChanged == true
                    || this.OrdersChanged == true
                    || this.CommentsChanged == true)
                {
                    if (bAutoSavePrev == false)
                    {
                        // ������δ����
                        DialogResult result = MessageBox.Show(this,
                            "��ǰ�������� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档\r\n\r\n��װ���µ�ʵ����Ϣ��ǰ���Ƿ��ȱ�����Щ�޸�? ",
                            "EntityForm",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);
                        if (result == DialogResult.Cancel)
                        {
                            MessageBox.Show(this, "����װ������� (���¼·��Ϊ '" + strIssueRecPath + "' )");
                            return -1;
                        }
                        if (result == DialogResult.Yes)
                        {
                            nRet = this.DoSaveAll();
                            if (nRet == -1)
                                return -1; // ������һ������

                        }
                    }
                    else
                    {
                        nRet = this.DoSaveAll();
                        if (nRet == -1)
                            return -1; // ������һ������
                    }
                }

                /*
                if (strItemBarcode != this.textBox_itemBarcode.Text)
                    this.textBox_itemBarcode.Text = strItemBarcode;
                 * */

                IssueItem result_item = null;
                // ע�������װ���item�����ں͵�ǰ�ֲ�ͬ���֣������ǰ��Ŀ���ݱ��޸Ĺ����ᾯ���Ƿ�(�ƻ���)װ�룬������Ŀ���ݲ��ᱻ���档����һ�����⡣
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = this.issueControl1.DoSearchItemByRecPath(strIssueRecPath,
                    out result_item);

                this.SwitchFocus(ISSUE_LIST);

                return nRet;
            }
            finally
            {
                this.m_nChannelInUse--;
            }
        }

        private void toolStripMenuItem_SearchOnly_Click(object sender, EventArgs e)
        {
            this.RegisterType = RegisterType.SearchOnly;
        }

        private void toolStripMenuItem_quickRegister_Click(object sender, EventArgs e)
        {
            this.RegisterType = RegisterType.QuickRegister;
        }

        private void toolStripMenuItem_register_Click(object sender, EventArgs e)
        {
            this.RegisterType = RegisterType.Register;
        }

        /// <summary>
        /// �Ǽǰ�ť��������
        /// </summary>
        public RegisterType RegisterType
        {
            get
            {
                return m_registerType;
            }
            set
            {
                m_registerType = value;

                this.toolStripMenuItem_SearchOnly.Checked = false;
                this.toolStripMenuItem_quickRegister.Checked = false;
                this.toolStripMenuItem_register.Checked = false;


                if (m_registerType == RegisterType.SearchOnly)
                {
                    this.button_register.Text = "����";
                    this.toolStripMenuItem_SearchOnly.Checked = true;
                }
                if (m_registerType == RegisterType.QuickRegister)
                {
                    this.button_register.Text = "���ٵǼ�";
                    this.toolStripMenuItem_quickRegister.Checked = true;
                }
                if (m_registerType == RegisterType.Register)
                {
                    this.button_register.Text = "�Ǽ�";
                    this.toolStripMenuItem_register.Checked = true;
                }

            }
        }

        private void button_option_Click(object sender, EventArgs e)
        {
            EntityFormOptionDlg dlg = new EntityFormOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.MainForm = this.MainForm;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);
        }

        // �����Ŀ���ڡ��ᡢ�ɹ���������Ϣ����
        private void button_clear_Click(object sender, EventArgs e)
        {
            Clear(true);
        }

        /// <summary>
        /// �����ǰ��������
        /// </summary>
        /// <param name="bWarningNotSave">�Ƿ񾯸���δ������޸�</param>
        /// <returns>true: �Ѿ����; false: �������</returns>
        public bool Clear(bool bWarningNotSave)
        {
            if (bWarningNotSave == true)
            {
                if (this.EntitiesChanged == true
        || this.IssuesChanged == true
        || this.OrdersChanged == true
        || this.CommentsChanged == true
        || this.BiblioChanged == true
        || this.ObjectChanged == true)
                {
                    // ������δ����
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ�� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档����ʱ���������δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�������? ",
                        "EntityForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return false;   // canceled
                }
            }

            /*
            this.listView_items.Items.Clear();
            this.bookitems = null;
             * */
            this.entityControl1.ClearItems();

            this.issueControl1.ClearItems();

            this.orderControl1.ClearItems();

            this.commentControl1.ClearItems();

            this.TargetRecPath = "";

            this.ClearBiblio();

            // this.m_strTempBiblioRecPath = "";
            lock (this.m_listPendingLoadRequest)
            {
                this.m_listPendingLoadRequest.Clear();  // 2009/11/6 new add
            }

            if (this.m_verifyViewer != null)
                this.m_verifyViewer.Clear();

            if (this.m_genDataViewer != null)
                this.m_genDataViewer.Clear();

            if (this.m_commentViewer != null)
                this.m_commentViewer.Clear();

            return true;    // cleared
        }

        private void EntityForm_Activated(object sender, EventArgs e)
        {
            // 2009/1/15 new add
            if (this.AcceptMode == true)
            {
#if ACCEPT_MODE
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
#endif
            }

            this.MainForm.stopManager.Active(this.Progress);

            this.MainForm.SetMenuItemState();

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = true;
            this.MainForm.MenuItem_logout.Enabled = true;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = true;

            this.MainForm.toolButton_refresh.Enabled = true;

            if (this.m_verifyViewer != null)
            {
                if (m_verifyViewer.Docked == true
                    && this.MainForm.CurrentVerifyResultControl != m_verifyViewer.ResultControl)
                    this.MainForm.CurrentVerifyResultControl = m_verifyViewer.ResultControl;
            }
            else
            {
                this.MainForm.CurrentVerifyResultControl = null;
            }

        }


        void SwitchFocus(int target)
        {
            API.PostMessage(this.Handle,
                WM_SWITCH_FOCUS,
                target,
                0);
        }

        // �Ƿ������Ŀ����
        bool Cataloging
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "entity_form",
                    "cataloging",
                    true);  // 2007/12/2 �޸�Ϊ true
            }
        }

        // װ����Ŀģ��
        // return:
        //      -1  error
        //      0   ����
        //      1   �ɹ�װ��
        /// <summary>
        /// װ����Ŀģ��
        /// </summary>
        /// <param name="bAutoSave">�Ƿ��Զ����洰������ǰ���޸�</param>
        /// <returns>
        /// <para>-1: ����</para>
        /// <para>0: ����</para>
        /// <para>1: �ɹ�װ��</para>
        /// </returns>
        public int LoadBiblioTemplate(bool bAutoSave = true)
        {
            int nRet = 0;

            // ��ס Shift ʹ�ñ����ܣ������³��ֶԻ���
            bool bShift = (Control.ModifierKeys == Keys.Shift);

            if (this.BiblioChanged == true
                || this.EntitiesChanged == true
                || this.IssuesChanged == true
                || this.OrdersChanged == true
                || this.CommentsChanged == true
                || this.ObjectChanged == true)
            {
                                        // 2008/6/25 new add
                if (this.checkBox_autoSavePrev.Checked == true
                    && bAutoSave == true)
                {
                    nRet = this.DoSaveAll();
                    if (nRet == -1)
                        return -1;
                }
                else
                {

                    DialogResult result = MessageBox.Show(this,
                        "װ�ر�Ŀģ��ǰ,���ֵ�ǰ���������� " + GetCurrentChangedPartName() + " �޸ĺ�δ���ü����档�Ƿ�Ҫ����װ�ر�Ŀģ�嵽������(��������ʧ��ǰ�޸ĵ�����)?\r\n\r\n(��)����װ�ر�Ŀģ�� (��)��װ�ر�Ŀģ��",
                        "EntityForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result != DialogResult.Yes)
                    {
                        MessageBox.Show(this, "װ�ر�Ŀģ�����������...");
                        return 0;
                    }
                }
            }

            string strSelectedDbName = this.MainForm.AppInfo.GetString(
                "entity_form",
                "selected_dbname_for_loadtemplate",
                "");

            SelectedTemplate selected = this.selected_templates.Find(strSelectedDbName);

            GetDbNameDlg dbname_dlg = new GetDbNameDlg();
            MainForm.SetControlFont(dbname_dlg, this.Font, false);
            if (selected != null)
            {
                dbname_dlg.NotAsk = selected.NotAskDbName;
                dbname_dlg.AutoClose = (bShift == true ? false : selected.NotAskDbName);
            }

            dbname_dlg.EnableNotAsk = true;
            dbname_dlg.DbName = strSelectedDbName;
            dbname_dlg.MainForm = this.MainForm;

            dbname_dlg.Text = "װ����Ŀģ�� -- ��ѡ��Ŀ���Ŀ����";
            //  dbname_dlg.StartPosition = FormStartPosition.CenterScreen;

            this.MainForm.AppInfo.LinkFormState(dbname_dlg, "entityform_load_template_GetBiblioDbNameDlg_state");
            dbname_dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dbname_dlg);



            if (dbname_dlg.DialogResult != DialogResult.OK)
                return 0;

            string strBiblioDbName = dbname_dlg.DbName;
            // ����
            this.MainForm.AppInfo.SetString(
                "entity_form",
                "selected_dbname_for_loadtemplate",
                strBiblioDbName);

            selected = this.selected_templates.Find(strBiblioDbName);

            this.BiblioRecPath = dbname_dlg.DbName + "/?";	// Ϊ��׷�ӱ���

            // ���������ļ�
            string strContent = "";
            string strError = "";

            // string strCfgFilePath = respath.Path + "/cfgs/template";
            byte[] baCfgOutputTimestamp = null;
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = GetCfgFileContent(strBiblioDbName,
                "template",
                out strContent,
                out baCfgOutputTimestamp,
                out strError);
            if (nRet == -1 || nRet == 0)
            {
                this.BiblioTimestamp = null;
                goto ERROR1;
            }

            // MessageBox.Show(this, strContent);

            SelectTemplateDlg select_temp_dlg = new SelectTemplateDlg();
            MainForm.SetControlFont(select_temp_dlg, this.Font, false);

            select_temp_dlg.Text = "��ѡ������Ŀ��¼ģ�� -- ������Ŀ�� '" + strBiblioDbName + "'";
            string strSelectedTemplateName = "";
            bool bNotAskTemplateName = false;
            if (selected != null)
            {
                strSelectedTemplateName = selected.TemplateName;
                bNotAskTemplateName = selected.NotAskTemplateName;
            }

            select_temp_dlg.SelectedName = strSelectedTemplateName;
            select_temp_dlg.AutoClose = (bShift == true ? false : bNotAskTemplateName);
            select_temp_dlg.NotAsk = bNotAskTemplateName;
            select_temp_dlg.EnableNotAsk = true;

            nRet = select_temp_dlg.Initial(
                true, // true ��ʾҲ����ɾ��  // false,
                strContent,
                out strError);
            if (nRet == -1)
            {
                strError = "װ�������ļ� '" + "template" + "' ��������: " + strError;
                goto ERROR1;
            }

            this.MainForm.AppInfo.LinkFormState(select_temp_dlg, "entityform_load_template_SelectTemplateDlg_state");
            select_temp_dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(select_temp_dlg);

            if (select_temp_dlg.DialogResult != DialogResult.OK)
                return 0;

            if (select_temp_dlg.Changed == true)
            {
                // return:
                //      -1  ����
                //      0   û�б�Ҫ����
                //      1   �ɹ�����
                nRet = SaveTemplateChange(select_temp_dlg,
                    strBiblioDbName,
                    baCfgOutputTimestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                this.MainForm.StatusBarMessage = "�޸�ģ��ɹ���";
                return 1;
            }

            // ���䱾�ε�ѡ���´ξͲ����ٽ��뱾�Ի�����
            this.selected_templates.Set(strBiblioDbName,
                dbname_dlg.NotAsk,
                select_temp_dlg.SelectedName,
                select_temp_dlg.NotAsk);

            this.BiblioTimestamp = null;
            // this.m_strMetaData = "";	// ����XML��¼��Ԫ����

            this.BiblioOriginPath = ""; // ��������ݿ�������ԭʼpath

            // this.TimeStamp = baTimeStamp;

            // this.Text = respath.ReverseFullPath; // ���ڱ���

            // return:
            //      -1  error
            //      0   �յļ�¼
            //      1   �ɹ�
            nRet = SetBiblioRecordToMarcEditor(select_temp_dlg.SelectedRecordXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

#if NO
            Global.SetHtmlString(this.webBrowser_biblioRecord,
                "(�հ�)");
#endif
            this.m_webExternalHost_biblio.SetHtmlString("(�հ�)",
    "entityform_error");

            // ����tabpage��� 2009/1/5 new add
            this.binaryResControl1.Clear();

            // ��tabpage�Ƿ���ʾ
            string strItemDbName = this.MainForm.GetItemDbName(strBiblioDbName);
            if (String.IsNullOrEmpty(strItemDbName) == false)
            {
                this.EnableItemsPage(true);
            }
            else
            {
                this.EnableItemsPage(false);
            }

            this.entityControl1.ClearItems();

            // ��tabpage�Ƿ���ʾ
            string strIssueDbName = this.MainForm.GetIssueDbName(strBiblioDbName);
            if (String.IsNullOrEmpty(strIssueDbName) == false)
            {
                this.EnableIssuesPage(true);
            }
            else
            {
                this.EnableIssuesPage(false);
            }

            this.issueControl1.ClearItems();

            // ����tabpage�Ƿ���ʾ
            string strOrderDbName = this.MainForm.GetOrderDbName(strBiblioDbName);
            if (String.IsNullOrEmpty(strOrderDbName) == false) // ���ڵ�ǰ��Ŀ���ж�Ӧ�Ĳɹ���ʱ����װ��ɹ���¼
            {
                if (String.IsNullOrEmpty(strIssueDbName) == false)
                    this.orderControl1.SeriesMode = true;
                else
                    this.orderControl1.SeriesMode = false;

                this.EnableOrdersPage(true);
            }
            else
            {
                this.EnableOrdersPage(false);
            }

            this.orderControl1.ClearItems();

            // ��עtabpage�Ƿ���ʾ
            string strCommentDbName = this.MainForm.GetCommentDbName(strBiblioDbName);
            if (String.IsNullOrEmpty(strCommentDbName) == false)
            {
                this.EnableCommentsPage(true);
            }
            else
            {
                this.EnableCommentsPage(false);
            }

            this.commentControl1.ClearItems();

            // 2007/11/5 new add
            this.DeletedMode = false;

            this.BiblioChanged = false;

            // ****
            this.toolStripButton_marcEditor_save.Enabled = true;

            // ��ģ���ʱ���������ReadOnly����false
            if (this.m_marcEditor.ReadOnly == true)
                this.m_marcEditor.ReadOnly = false;

            // 2008/11/30 new add
            SwitchFocus(MARC_EDITOR);
            if (dbname_dlg.NotAsk == true || select_temp_dlg.NotAsk == true)
            {
                this.MainForm.StatusBarMessage = "�Զ�����Ŀ�� " + strBiblioDbName + " ��װ����Ϊ " + select_temp_dlg.SelectedName + " ������Ŀ��¼ģ�塣��Ҫ���³���װ�ضԻ����밴סShift���ٵ㡰װ����Ŀģ�塱��ť...";
            }
            return 1;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        /*
        // ��·�����������ݿ���
        static string GetDbName(string strPath)
        {
            int nRet = strPath.IndexOf("/");
            if (nRet == -1)
                return strPath;
            return strPath.Substring(0, nRet);
        }*/

        // ��õ�ǰ��¼��MARC��ʽ
        // 2009/3/4 new add
        // return:
        //      null    ��Ϊ��ǰ��¼·��Ϊ�գ��޷����MARC��ʽ
        //      ����    MARC��ʽ��Ϊ"unimarc" "usnmarc" ֮һ
        /// <summary>
        /// ��õ�ǰ��¼��MARC��ʽ
        /// </summary>
        /// <returns>
        /// <para>null: ��Ϊ��ǰ��¼·��Ϊ�գ��޷����MARC��ʽ</para>
        /// <para>����: MARC��ʽ��Ϊ"unimarc" "usnmarc" ֮һ</para>
        /// </returns>
        public string GetCurrentMarcSyntax()
        {
            string strMarcSyntax = "";

            // ��ÿ��������ݿ����õ�marc syntax
            string strBiblioDbName = Global.GetDbName(this.BiblioRecPath);

            if (String.IsNullOrEmpty(strBiblioDbName) == false)
                strMarcSyntax = MainForm.GetBiblioSyntax(strBiblioDbName);
            else
                return null;    // �޷��õ�����Ϊ��ǰû����Ŀ��·��

            // �ڵ�ǰû�ж���MARC�﷨������£�Ĭ��unimarc
            if (String.IsNullOrEmpty(strMarcSyntax) == true)
                strMarcSyntax = "unimarc";

            return strMarcSyntax;
        }

        // �����Ŀ��¼��XML��ʽ
        // parameters:
        //      strBiblioDbName ��Ŀ������������������Ҫ������XML��¼��marcsyntax������˲���==null����ʾ���this.BiblioRecPath��ȥȡ��Ŀ����
        //      bIncludeFileID  �Ƿ�Ҫ���ݵ�ǰrescontrol���ݺϳ�<dprms:file>Ԫ��?
        int GetBiblioXml(
            string strBiblioDbName,
            bool bIncludeFileID,
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";


            string strMarcSyntax = "";

            // ��ÿ��������ݿ����õ�marc syntax
            if (String.IsNullOrEmpty(strBiblioDbName) == true)
                strBiblioDbName = Global.GetDbName(this.BiblioRecPath);

            if (String.IsNullOrEmpty(strBiblioDbName) == false)
                strMarcSyntax = MainForm.GetBiblioSyntax(strBiblioDbName);
            

            // �ڵ�ǰû�ж���MARC�﷨������£�Ĭ��unimarc
            if (String.IsNullOrEmpty(strMarcSyntax) == true)
                strMarcSyntax = "unimarc";

            // 2008/5/16 changed
            string strMARC = this.GetMarc();    //  this.m_marcEditor.Marc;
            XmlDocument domMarc = null;
            int nRet = MarcUtil.Marc2Xml(strMARC,
                strMarcSyntax,
                out domMarc,
                out strError);
            if (nRet == -1)
                return -1;

            // ��ΪdomMarc�Ǹ���MARC��¼�ϳɵģ���������û�в�����<dprms:file>Ԫ�أ�Ҳ��û��(�����µ�idǰ)�������Ҫ

            Debug.Assert(domMarc != null, "");

            // �ϳ�<dprms:file>Ԫ��
            if (this.binaryResControl1 != null
                && bIncludeFileID == true)  // 2008/12/3 new add
            {
                List<string> ids = this.binaryResControl1.GetIds();
                List<string> usages = this.binaryResControl1.GetUsages();

                Debug.Assert(ids.Count == usages.Count, "");

                for (int i = 0; i < ids.Count; i++)
                {
                    string strID = ids[i];
                    if (String.IsNullOrEmpty(strID) == true)
                        continue;

                    string strUsage = usages[i];

                    XmlNode node = domMarc.CreateElement("dprms",
                        "file",
                        DpNs.dprms);
                    domMarc.DocumentElement.AppendChild(node);
                    DomUtil.SetAttr(node, "id", strID);
                    if (string.IsNullOrEmpty(strUsage) == false)
                        DomUtil.SetAttr(node, "usage", strUsage);
                }
            }


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
                    return -1;
                }

                domMarc.DocumentElement.AppendChild(fragment);
            }


            strXml = domMarc.OuterXml;
            return 0;
        }

        // �ѵ�ǰ��Ŀ��¼���Ƶ�Ŀ��λ��
        // parameters:
        //      strAction   ������Ϊ"onlycopybiblio" "onlymovebiblio"֮һ������ copy / move
        /// <summary>
        /// �ѵ�ǰ��Ŀ��¼���Ƶ�Ŀ��λ��
        /// </summary>
        /// <param name="strAction">������Ϊ copy / move / onlycopybiblio / onlymovebiblio ֮һ</param>
        /// <param name="strTargetBiblioRecPath">Ŀ����Ŀ��¼·��</param>
        /// <param name="strMergeStyle">�ϲ���ʽ</param>
        /// <param name="strXml">������Ŀ��¼ XML</param>
        /// <param name="strOutputBiblioRecPath">ʵ��д�����Ŀ��¼·��</param>
        /// <param name="baOutputTimestamp">Ŀ���¼������ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int CopyBiblio(
            string strAction,
            string strTargetBiblioRecPath,
            string strMergeStyle,
            out string strXml,  // ˳�㷵����Ŀ��¼XML��ʽ
            out string strOutputBiblioRecPath,
            out byte [] baOutputTimestamp,
            out string strError)
        {
            strError = "";
            strXml = "";
            baOutputTimestamp = null;
            strOutputBiblioRecPath = "";
            int nRet = 0;

            string strOldMarc = this.GetMarc();    //  this.m_marcEditor.Marc;
            bool bOldChanged = this.GetMarcChanged();   //  this.m_marcEditor.Changed;

            try
            {

                // 2011/11/28
                // ����ǰ��׼������
                {
                    // ��ʼ�� dp2circulation_marc_autogen.cs �� Assembly����new DetailHost����
                    // return:
                    //      -1  error
                    //      0   û�����³�ʼ��Assembly������ֱ������ǰCache��Assembly
                    //      1   ����(�����״�)��ʼ����Assembly
                    nRet = InitialAutogenAssembly(strTargetBiblioRecPath,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (this.m_detailHostObj != null)
                    {
                        BeforeSaveRecordEventArgs e = new BeforeSaveRecordEventArgs();
                        this.m_detailHostObj.BeforeSaveRecord(this.m_marcEditor, e);
                        if (string.IsNullOrEmpty(e.ErrorInfo) == false)
                        {
                            MessageBox.Show(this, "����ǰ��׼������ʧ��: " + e.ErrorInfo + "\r\n\r\n����������Խ�����");
                        }
                    }
                }

                // �����Ŀ��¼XML��ʽ
                strXml = "";
                nRet = this.GetBiblioXml(
                    "", // ��ʹ�Ӽ�¼·���п�marc��ʽ
                    true,
                    out strXml,
                    out strError);
                if (nRet == -1)
                    return -1;

            }
            finally
            {
                // ��ԭ��ǰ���ڵļ�¼
                if (this.GetMarc() /*this.m_marcEditor.Marc*/ != strOldMarc)
                {
                    // this.m_marcEditor.Marc = strOldMarc;
                    this.SetMarc(strOldMarc);
                }
                if (this.GetMarcChanged() /*this.m_marcEditor.Changed*/ != bOldChanged)
                {
                    // this.m_marcEditor.Changed = bOldChanged;
                    this.SetMarcChanged(bOldChanged);
                }
            }

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("���ڸ�����Ŀ��¼ ...");
            Progress.BeginLoop();

            try
            {
                string strOutputBiblio = "";

                // result.Value:
                //      -1  ����
                //      0   �ɹ���û�о�����Ϣ��
                //      1   �ɹ����о�����Ϣ��������Ϣ�� result.ErrorInfo ��
                long lRet = this.Channel.CopyBiblioInfo(
                    this.Progress,
                    strAction,
                    this.BiblioRecPath,
                    "xml",
                    null,
                    this.BiblioTimestamp,
                    strTargetBiblioRecPath,
                    null,   // strXml,
                    strMergeStyle,
                    out strOutputBiblio,
                    out strOutputBiblioRecPath,
                    out baOutputTimestamp,
                    out strError);
                if (lRet == -1)
                    return -1;
                if (lRet == 1)
                {
                    // �о���
                    MessageBox.Show(this, strError);
                }
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }

            return 0;
        }

        // 2013/12/2
        /// <summary>
        /// ��ýű�����
        /// </summary>
        public DetailHost HostObject
        {
            get
            {
                string strError = "";
                // ��ʼ�� dp2circulation_marc_autogen.cs �� Assembly����new DetailHost����
                // return:
                //      -1  error
                //      0   û�����³�ʼ��Assembly������ֱ������ǰCache��Assembly
                //      1   ����(�����״�)��ʼ����Assembly
                int nRet = InitialAutogenAssembly(null,
                    out strError);
                /*
                if (nRet == -1)
                    throw new Exception(strError);
                 * */
                return this.m_detailHostObj;
            }
        }

        // ������Ŀ��¼�����ݿ�
        // parameters:
        //      bIncludeFileID  (��Ŀ��¼XML)�Ƿ�Ҫ���ݵ�ǰrescontrol���ݺϳ�<dprms:file>Ԫ��?
        // return:
        //      -1  ����
        //      0   û�б���
        //      1   �Ѿ�����
        /// <summary>
        /// ������Ŀ��¼�����ݿ�
        /// </summary>
        /// <param name="bIncludeFileID">(��Ŀ��¼XML)�Ƿ�Ҫ���ݵ�ǰ����ؼ����ݺϳ�&lt;dprms:file&gt;Ԫ��?</param>
        /// <param name="strHtml">�����¼�¼�� OPAC ��ʽ����</param>
        /// <param name="strStyle">����� displaysuccess / searchdup ֮һ���߶��ż����϶��ɡ�displaysuccess ��ʾ���ĳɹ���Ϣ�ڿ�ܴ��ڵ�״̬��; searchdup ����ɹ����Ͳ�����Ϣ</param>
        /// <returns>
        /// <para>-1  ����</para>
        /// <para>0   û�б���</para>
        /// <para>1   �Ѿ�����</para>
        /// </returns>
        public int SaveBiblioToDatabase(bool bIncludeFileID,
            out string strHtml,
            string strStyle = "displaysuccess,searchdup")
        {
            string strError = "";
            strHtml = "";
            int nRet = 0;

            bool bDisplaySuccess = StringUtil.IsInList("displaysuccess", strStyle);
            bool bSearchDup = StringUtil.IsInList("searchdup", strStyle);


            if (this.Cataloging == false)
            {
                strError = "��ǰ�������Ŀ���ܣ����Ҳ����������Ŀ��Ϣ�Ĺ���";
                return -1;
            }


            // ����ղ���ɾ����ģʽ������ȡ�����ģʽ 2007/10/15
            if (this.DeletedMode == true)
            {
                // TODO: ���˲���Ϣ��ҲҪ�����ڡ��ɹ���Ϣ
                int nEntityCount = this.entityControl1.ItemCount;

                if (nEntityCount > 0)
                {
                    DialogResult result = MessageBox.Show(this,
"������ñ����ܽ���ɾ������Ŀ��¼��������ݿ⣬��ô��Ŀ��¼������ "
+ nEntityCount.ToString()
+ " ��ʵ���¼�����ᱻ�����ʵ��⡣\r\n\r\n���Ҫ�ڱ�����Ŀ���ݵ�ͬʱҲ����������Щ��ɾ����ʵ���¼���������ֲᴰ��������ѡ��.../ʹ�ܱ༭���桱���ܣ�Ȼ����ʹ�á�ȫ�����桱��ť"
+ "\r\n\r\n�Ƿ�Ҫ�ڲ�����ʵ���¼������µ���������Ŀ��¼�� (Yes �� / No ��������������Ŀ��¼�Ĳ���)",
"EntityForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                    {
                        strError = "����������Ŀ��¼";
                        goto ERROR1;
                    }
                }
            }

            string strTargetPath = this.BiblioRecPath;
            if (string.IsNullOrEmpty(strTargetPath) == true)
            {
                // ��Ҫѯ�ʱ����·��
                BiblioSaveToDlg dlg = new BiblioSaveToDlg();
                MainForm.SetControlFont(dlg, this.Font, false);

                dlg.MainForm = this.MainForm;
                dlg.Text = "��������Ŀ��¼";
                dlg.MessageText = "��ָ������Ŀ��¼Ҫ���浽��λ��";
                dlg.EnableCopyChildRecords = false;

                dlg.BuildLink = false;

                dlg.CopyChildRecords = false;

                dlg.CurrentBiblioRecPath = this.BiblioRecPath;
                this.MainForm.AppInfo.LinkFormState(dlg, "entityform_BiblioSaveToDlg_state");
                dlg.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(dlg);

                if (dlg.DialogResult != DialogResult.OK)
                    return 0;

                strTargetPath = dlg.RecPath;
            }

            // ����ǰ��׼������
            {
                // ��ʼ�� dp2circulation_marc_autogen.cs �� Assembly����new DetailHost����
                // return:
                //      -1  error
                //      0   û�����³�ʼ��Assembly������ֱ������ǰCache��Assembly
                //      1   ����(�����״�)��ʼ����Assembly
                nRet = InitialAutogenAssembly(strTargetPath,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (this.m_detailHostObj != null)
                {
                    BeforeSaveRecordEventArgs e = new BeforeSaveRecordEventArgs();
                    this.m_detailHostObj.BeforeSaveRecord(this.m_marcEditor, e);
                    if (string.IsNullOrEmpty(e.ErrorInfo) == false)
                    {
                        MessageBox.Show(this, "����ǰ��׼������ʧ��: " + e.ErrorInfo + "\r\n\r\n����������Խ�����");
                    }
                }
            }

            // �����Ŀ��¼XML��ʽ
            string strXmlBody = "";
            nRet = this.GetBiblioXml(
                "", // ��ʹ�Ӽ�¼·���п�marc��ʽ
                bIncludeFileID,
                out strXmlBody,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            bool bPartialDenied = false;
            string strOutputPath = "";
            byte[] baNewTimestamp = null;
            string strWarning = "";
            nRet = SaveXmlBiblioRecordToDatabase(strTargetPath,
                this.DeletedMode == true,
                strXmlBody,
                this.BiblioTimestamp,
                out strOutputPath,
                out baNewTimestamp,
                out strWarning,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (string.IsNullOrEmpty(strWarning) == false)
                MessageBox.Show(this, strWarning);
            if (Channel.ErrorCode == ErrorCode.PartialDenied)
                bPartialDenied = true;


            this.BiblioTimestamp = baNewTimestamp;
            this.BiblioRecPath = strOutputPath;
            this.BiblioOriginPath = strOutputPath;

            this.BiblioChanged = false;

            // ����ղ���ɾ����ģʽ������ȡ�����ģʽ 2007/10/15
            if (this.DeletedMode == true)
            {
                this.DeletedMode = false;

                // ����װ��ʵ���¼���Ա㷴ӳ��listview��յ���ʵ
                // ����װ����ص����в�
                nRet = this.entityControl1.LoadItemRecords(
                    this.BiblioRecPath,
                    // this.DisplayOtherLibraryItem,
                    this.DisplayOtherLibraryItem == true ? "getotherlibraryitem" : "",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }


            // ���ReadOnly״̬�����998$t�Ѿ���ʧ
            if (this.m_marcEditor.ReadOnly == true)
            {
                string strTargetBiblioRecPath = this.m_marcEditor.Record.Fields.GetFirstSubfield("998", "t");
                if (String.IsNullOrEmpty(strTargetBiblioRecPath) == true)
                    this.m_marcEditor.ReadOnly = false;
            }

            if (bDisplaySuccess == true)
            {
                this.MainForm.StatusBarMessage = "��Ŀ��¼ '" + this.BiblioRecPath + "' ����ɹ�";
                // MessageBox.Show(this, "��Ŀ��¼����ɹ���");
            }

            if (bSearchDup == true)
            {
                if (this.AutoSearchDup == true)
                    API.PostMessage(this.Handle, WM_SEARCH_DUP, 0, 0);
            }

            // if (bPartialDenied == true)
            {
                // ���ʵ�ʱ������Ŀ��¼
                string[] results = null;
                string[] formats = null;
                if (bPartialDenied == true)
                {
                    formats = new string[2];
                    formats[0] = "html";
                    formats[1] = "xml";
                }
                else
                {
                    formats = new string[1];
                    formats[0] = "html";
                }
                long lRet = Channel.GetBiblioInfos(
    Progress,
    strOutputPath,
    "",
    formats,
    out results,
    out baNewTimestamp,
    out strError);
                if (lRet == 0)
                {
                    strError = "����װ��ʱ��·��Ϊ '" + strOutputPath + "' ����Ŀ��¼û���ҵ� ...";
                    goto ERROR1;
                }
                if (results == null)
                {
                    strError = "����װ����Ŀ��¼ʱ����: result == null";
                    goto ERROR1;
                }

                {
                    // ������ʾ OPAC ��Ŀ��Ϣ
                    // TODO: ��Ҫ�ڶ��󱣴����Ժ󷢳����ָ��
                    Debug.Assert(results.Length >= 1, "");
                    if (results.Length > 0)
                    {
                        strHtml = results[0];
#if NO
                        this.m_webExternalHost_biblio.SetHtmlString(strHtml,
                            "entityform_biblio");
#endif
                    }
                }

                if (bPartialDenied == true)
                {
                    if (results.Length < 2)
                    {
                        strError = "����װ����Ŀ��¼ʱ����: result.Length["+results.Length.ToString()+"] С�� 2";
                        goto ERROR1;
                    }
                    PartialDeniedDialog dlg = new PartialDeniedDialog();

                    MainForm.SetControlFont(dlg, this.Font, false);
                    dlg.SavingXml = strXmlBody;
                    Debug.Assert(results.Length >= 2, "");
                    dlg.SavedXml = results[1];
                    dlg.MainForm = this.MainForm;

                    this.MainForm.AppInfo.LinkFormState(dlg, "PartialDeniedDialog_state");
                    dlg.ShowDialog(this);
                    this.MainForm.AppInfo.UnlinkFormState(dlg);

                    if (dlg.DialogResult == System.Windows.Forms.DialogResult.OK)
                    {
                        string strOutputBiblioRecPath = "";
                        string strXml = "";
                        // ��ʵ�ʱ���ļ�¼װ�� MARC �༭��
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   found
                        nRet = LoadBiblioRecord(strOutputPath,
                            "",
                            false,
                            out strOutputBiblioRecPath,
                            out strXml,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "����װ����Ŀ��¼ʱ����: " + strError;
                            goto ERROR1;
                        }
                    }
                }
            }
            return 1;

        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        /// <summary>
        /// ��ǰ�Ƿ���ɾ�����״̬
        /// </summary>
        public bool DeletedMode
        {
            get
            {
                return this.m_bDeletedMode;
            }
            set
            {
                this.m_bDeletedMode = value;

                this.SetSaveAllButtonState(true);
                this.EnableControls(true);  // 2009/11/11 new add
            }
        }

        // 
        /// <summary>
        /// ɾ����Ŀ��¼ �� �����Ĳᡢ�ڡ������������¼
        /// </summary>
        public void DeleteBiblioFromDatabase()
        {
            string strError = "";

            List<string> subRecord_warnings = new List<string>();

            int nEntityCount = this.entityControl1.ItemCount;
            if (nEntityCount != 0)
                subRecord_warnings.Add(nEntityCount.ToString() + " �����¼");

            int nIssueCount = this.issueControl1.ItemCount;
            if (nIssueCount != 0)
                subRecord_warnings.Add(nIssueCount.ToString() + " ���ڼ�¼");

            int nOrderCount = this.orderControl1.ItemCount;
            if (nOrderCount != 0)
                subRecord_warnings.Add(nOrderCount.ToString() + " ���ɹ���¼");

            int nCommentCount = this.commentControl1.ItemCount;
            if (nCommentCount != 0)
                subRecord_warnings.Add(nCommentCount.ToString() + " ����ע��¼");

            if (subRecord_warnings.Count > 0)
            {
                // ���ǰ��Ȩ��
                if (StringUtil.IsInList("client_deletebibliosubrecords", this.Channel.Rights) == false)
                {
                    strError = "��Ŀ��¼ " + this.BiblioRecPath + " ���������� "
                        + StringUtil.MakePathList(subRecord_warnings, "��")
                        + "������ǰ�û������߱� client_deletebibliosubrecords Ȩ�ޣ�����޷�����ɾ����Ŀ��¼�Ĳ���";
                    goto ERROR1;
                }
            }

            string strChangedWarning = "";

            // �ȼ��ʵ��listview���Ƿ���new change deleted�������У�����
            if (this.EntitiesChanged == true
                || this.IssuesChanged == true
                || this.OrdersChanged == true
                || this.CommentsChanged == true
                || this.ObjectChanged == true
                || this.BiblioChanged == true)
            {

                strChangedWarning = "��ǰ�� "
                    + GetCurrentChangedPartName()
                + " ���޸Ĺ���\r\n\r\n";
            }

            string strText = strChangedWarning;

            strText += "ȷʵҪɾ����Ŀ��¼ " +this.BiblioRecPath + " ";

            if (subRecord_warnings.Count > 0)
                strText += "�������� " + StringUtil.MakePathList(subRecord_warnings, "��");
#if NO
            int nEntityCount = this.entityControl1.ItemCount;
            if (nEntityCount != 0)
                strText += "�������� " + nEntityCount.ToString() + " �����¼";

            int nIssueCount = this.issueControl1.ItemCount;
            if (nIssueCount != 0)
                strText += "�������� " + nIssueCount.ToString() + " ���ڼ�¼";

            int nOrderCount = this.orderControl1.ItemCount;
            if (nOrderCount != 0)
                strText += "�������� " + nOrderCount.ToString() + " ���ɹ���¼";

            int nCommentCount = this.commentControl1.ItemCount;
            if (nCommentCount != 0)
                strText += "�������� " + nCommentCount.ToString() + " ����ע��¼";
#endif

            int nObjectCount = this.binaryResControl1.ObjectCount;
            if (nObjectCount != 0)
                strText += " �ʹ����� " + nObjectCount.ToString() + " ������";

            strText += " ?";

            // ����ɾ��
            DialogResult result = MessageBox.Show(this,
                strText,
                "EntityForm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            int nRet = DeleteBiblioRecordFromDatabase(this.BiblioRecPath,
                "delete",
                this.BiblioTimestamp,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // this.BiblioTimestamp = null;
            // this.textBox_biblioRecPath.Text = strOutputPath;
            // this.BiblioOriginPath = strOutputPath;


            this.BiblioChanged = false; // ����رմ���ʱ�������޸�����
            this.DeletedMode = true;



            string strMessage = "��Ŀ��¼ '" + this.BiblioRecPath + "' ";

            if (nEntityCount != 0)
            {
                // ����Ϣ������listview�У������Ա����ȥ�����Բ����

                strMessage += "�� ������ʵ���¼ ";
            }

            if (nIssueCount != 0)
            {
                // ����Ϣ������listview�У������Ա����ȥ�����Բ����

                strMessage += "�� �������ڼ�¼ ";
            }

            if (nOrderCount != 0)
            {
                // �ɹ���Ϣ������listview�У������Ա����ȥ�����Բ����

                strMessage += "�� �����Ĳɹ���¼ ";
            }

            if (nCommentCount != 0)
            {
                // ��ע��Ϣ������listview�У������Ա����ȥ�����Բ����

                strMessage += "�� ��������ע��¼ ";
            }

            if (nObjectCount != 0)
            {
                // ������Ϣ�޷������ȥ���������
                this.binaryResControl1.Clear();

                strMessage += "�� �����Ķ��� ";
            }

            strMessage += "ɾ���ɹ�";

            this.MainForm.StatusBarMessage = strMessage;

            this.SetSaveAllButtonState(true);
            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }


        // 
        /// <summary>
        /// ���浱ǰ�����ڼ�¼��ģ�������ļ�
        /// </summary>
        public void SaveBiblioToTemplate()
        {
            // ���·�������Ѿ��е���Ŀ����
            string strBiblioDbName = Global.GetDbName(this.BiblioRecPath);

            GetDbNameDlg dlg = new GetDbNameDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.DbName = strBiblioDbName;
            dlg.MainForm = this.MainForm;
            dlg.Text = "��ѡ��Ŀ���Ŀ����";
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            strBiblioDbName = dlg.DbName;


            // ����ģ�������ļ�
            string strContent = "";
            string strError = "";

            // string strCfgFilePath = respath.Path + "/cfgs/template";
            byte [] baTimestamp = null;

            // return:
            //      -1  error
            //      0   not found
            //      1   found
            int nRet = GetCfgFileContent(strBiblioDbName,
                "template",
                out strContent,
                out baTimestamp,
                out strError);
            if (nRet == -1 || nRet == 0)
            {
                goto ERROR1;
            }

            SelectTemplateDlg tempdlg = new SelectTemplateDlg();
            MainForm.SetControlFont(tempdlg, this.Font, false);
            nRet = tempdlg.Initial(
                true,   // �����޸�
                strContent, 
                out strError);
            if (nRet == -1)
                goto ERROR1;


            tempdlg.Text = "��ѡ��Ҫ�޸ĵ�ģ���¼";
            tempdlg.CheckNameExist = false;	// ��OK��ťʱ������"���ֲ�����",���������½�һ��ģ��
            //tempdlg.ap = this.MainForm.applicationInfo;
            //tempdlg.ApCfgTitle = "detailform_selecttemplatedlg";
            tempdlg.ShowDialog(this);

            if (tempdlg.DialogResult != DialogResult.OK)
                return;

            // return:
            //      -1  ����
            //      0   û�б�Ҫ����
            //      1   �ɹ�����
            nRet = SaveTemplateChange(tempdlg,
                strBiblioDbName,
                baTimestamp,
                out strError);
            if (nRet == -1)
                goto ERROR1;

#if NO
            // �����Ŀ��¼XML��ʽ
            string strXmlBody = "";
            nRet = this.GetBiblioXml(
                strBiblioDbName,
                false,  // ��Ҫ������ԴID
                out strXmlBody,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // �޸������ļ�����
            if (tempdlg.textBox_name.Text != "")
            {
                // �滻����׷��һ����¼
                nRet = tempdlg.ReplaceRecord(tempdlg.textBox_name.Text,
                    strXmlBody,
                    out strError);
                if (nRet == -1)
                {
                    goto ERROR1;
                }
            }

            if (tempdlg.Changed == false)	// û�б�Ҫ�����ȥ
                return;

            string strOutputXml = tempdlg.OutputXml;

            // Debug.Assert(false, "");
            nRet = SaveCfgFile(strBiblioDbName,
                "template",
                strOutputXml,
                baTimestamp,
                out strError);
            if (nRet == -1)
                goto ERROR1;
#endif

            this.MainForm.StatusBarMessage = "�޸�ģ��ɹ���";
            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }

        // return:
        //      -1  ����
        //      0   û�б�Ҫ����
        //      1   �ɹ�����
        int SaveTemplateChange(SelectTemplateDlg tempdlg,
            string strBiblioDbName,
            byte[] baTimestamp,
            out string strError)
        {
            strError = "";

            if (tempdlg.Changed == false    // DOM ����û�б仯
                && tempdlg.textBox_name.Text == "")	// û��ѡ��Ҫ�����ģ����
                return 0;


            // �����Ŀ��¼XML��ʽ
            string strXmlBody = "";
            int nRet = this.GetBiblioXml(
                strBiblioDbName,
                false,  // ��Ҫ������ԴID
                out strXmlBody,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // �޸������ļ�����
            if (tempdlg.textBox_name.Text != "")
            {
                // �滻����׷��һ����¼
                nRet = tempdlg.ReplaceRecord(tempdlg.textBox_name.Text,
                    strXmlBody,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            string strOutputXml = tempdlg.OutputXml;

            // Debug.Assert(false, "");
            nRet = SaveCfgFile(strBiblioDbName,
                "template",
                strOutputXml,
                baTimestamp,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return 1;
        ERROR1:
            return -1;
        }

        // ��װ�汾
                // ��������ļ�
        // parameters:
        //      
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetCfgFileContent(string strBiblioDbName,
            string strCfgFileName,
            out string strContent,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            return GetCfgFileContent(strBiblioDbName + "/cfgs/" + strCfgFileName,
            out strContent,
            out baOutputTimestamp,
            out strError);
        }

        int m_nInGetCfgFile = 0;    // ��ֹGetCfgFile()�������� 2008/3/6 new add

        // ��������ļ�
        // parameters:
        //      
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetCfgFileContent(string strCfgFilePath,
            out string strContent,
            out byte [] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";
            strContent = "";

            if (m_nInGetCfgFile > 0)
            {
                strError = "GetCfgFile() ������";
                return -1;
            }


            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("�������������ļ� ...");
            Progress.BeginLoop();

            m_nInGetCfgFile++;
            
            try
            {
                Progress.SetMessage("�������������ļ� " + strCfgFilePath + " ...");
                string strMetaData = "";
                string strOutputPath = "";

                string strStyle = "content,data,metadata,timestamp,outputpath";

                long lRet = Channel.GetRes(Progress,
                    MainForm.cfgCache,
                    strCfgFilePath,
                    strStyle,
                    null,
                    out strContent,
                    out strMetaData,
                    out baOutputTimestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    if (Channel.ErrorCode == ErrorCode.NotFound)
                        return 0;

                    goto ERROR1;
                }
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");

                m_nInGetCfgFile--;
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ��������ļ�
        // parameters:
        //      
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetCfgFile(string strBiblioDbName,
            string strCfgFileName,
            out string strOutputFilename,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";
            strOutputFilename = "";

            if (m_nInGetCfgFile > 0)
            {
                strError = "GetCfgFile() ������";
                return -1;
            }

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("�������������ļ� ...");
            Progress.BeginLoop();

            m_nInGetCfgFile++;

            try
            {
                string strPath = strBiblioDbName + "/cfgs/" + strCfgFileName;

                Progress.SetMessage("�������������ļ� " + strPath + " ...");
                string strMetaData = "";
                string strOutputPath = "";

                string strStyle = "content,data,metadata,timestamp,outputpath";

                long lRet = Channel.GetResLocalFile(Progress,
                    MainForm.cfgCache,
                    strPath,
                    strStyle,
                    out strOutputFilename,
                    out strMetaData,
                    out baOutputTimestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    if (Channel.ErrorCode == ErrorCode.NotFound)
                        return 0;

                    goto ERROR1;
                }

            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");

                m_nInGetCfgFile--;
            }

            return 1;
        ERROR1:
            return -1;
        }


        // ���������ļ�
        int SaveCfgFile(string strBiblioDbName,
            string strCfgFileName,
            string strContent,
            byte [] baTimestamp,
            out string strError)
        {
            strError = "";

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("���ڱ��������ļ� ...");
            Progress.BeginLoop();

            try
            {
                string strPath = strBiblioDbName + "/cfgs/" + strCfgFileName;

                Progress.SetMessage("���ڱ��������ļ� " + strPath + " ...");

                byte [] output_timestamp = null;
                string strOutputPath = "";

                long lRet = Channel.WriteRes(
                    Progress,
                    strPath,
                    strContent,
                    true,
                    "",	// style
                    baTimestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ����XML��ʽ����Ŀ��¼�����ݿ�
        // parameters:
        //      bResave �Ƿ�Ϊɾ�������±����ģʽ��������ģʽ�£�ʹ�� strAction == "new"
        int SaveXmlBiblioRecordToDatabase(string strPath,
            bool bResave,
            string strXml,
            byte[] baTimestamp,
            out string strOutputPath,
            out byte[] baNewTimestamp,
            out string strWarning,
            out string strError)
        {
            strError = "";
            strWarning = "";
            baNewTimestamp = null;
            strOutputPath = "";

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("���ڱ�����Ŀ��¼ ...");
            Progress.BeginLoop();

            try
            {
                string strAction = "change";

                if (Global.IsAppendRecPath(strPath) == true || bResave == true)
                    strAction = "new";

                /*
                if (String.IsNullOrEmpty(strPath) == true)
                    strAction = "new";
                else
                {
                    string strRecordID = Global.GetRecordID(strPath);
                    if (String.IsNullOrEmpty(strRecordID) == true
                        || strRecordID == "?")
                        strAction = "new";
                }
                */
                REDO:
                long lRet = Channel.SetBiblioInfo(
                    Progress,
                    strAction,
                    strPath,
                    "xml",
                    strXml,
                    baTimestamp,
                    "",
                    out strOutputPath,
                    out baNewTimestamp,
                    out strError);
                if (lRet == -1)
                {
                    strError = "������Ŀ��¼ '" + strPath + "' ʱ����: " + strError;
                    if (strAction == "change" && Channel.ErrorCode == ErrorCode.NotFound)
                    {
                        strError = "������Ŀ��¼ '" + strPath + "' ʱ����: ԭ��¼�Ѿ�������";
                        DialogResult result = MessageBox.Show(this,
strError + "\r\n\r\n�����Ƿ��Ϊ���´����˼�¼?",
"EntityForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.Yes)
                        {
                            strAction = "new";
                            // TODO: ��ʱҲ��Ҫ��һ�����������Ĳ��¼�ȡ����Ҷ�����Դ�����ָܻ���
                            goto REDO;
                        }
                    }

                    goto ERROR1;
                }
                if (Channel.ErrorCode == ErrorCode.PartialDenied)
                {
                    strWarning = "��Ŀ��¼ '" + strPath + "' ����ɹ��������ύ���ֶβ��ֱ��ܾ� ("+strError+")��������ˢ�´��ڣ����ʵ�ʱ����Ч��";
                }
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

        // �����ݿ���ɾ����Ŀ��¼
        int DeleteBiblioRecordFromDatabase(string strPath,
            string strAction,
            byte [] baTimestamp,
            out string strError)
        {
            strError = "";

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("����ɾ����Ŀ��¼ ...");
            Progress.BeginLoop();

            try
            {
                string strOutputPath = "";
                byte[] baNewTimestamp = null;

                long lRet = Channel.SetBiblioInfo(
                    Progress,
                    strAction,  // "delete",
                    strPath,
                    "xml",
                    "", // strXml,
                    baTimestamp,
                    "",
                    out strOutputPath,
                    out baNewTimestamp,
                    out strError);
                if (lRet == -1)
                {
                    // ɾ��ʧ��ʱҲ��Ҫ�����˸���ʱ���
                    // �����������ʱ�����ƥ�䣬�´�����ɾ������?
                    if (baNewTimestamp != null)
                        this.BiblioTimestamp = baNewTimestamp;
                    goto ERROR1;
                }

            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ��������ť��װ��ģ��
        private void toolStripButton_marcEditor_loadTemplate_Click(object sender, EventArgs e)
        {
            this.toolStrip_marcEditor.Enabled = false;
            try
            {
                LoadBiblioTemplate(true);
            }
            finally
            {
                this.toolStrip_marcEditor.Enabled = true;
            }
        }

        // ��������ť�����浽ģ��
        private void toolStripButton_marcEditor_saveTemplate_Click(object sender, EventArgs e)
        {
            this.toolStrip_marcEditor.Enabled = false;
            try
            {
                SaveBiblioToTemplate();
            }
            finally
            {
                this.toolStrip_marcEditor.Enabled = true;
            }

        }

        // ��������ť: �����¼�����ݿ�
        private void toolStripButton_marcEditor_save_Click(object sender, EventArgs e)
        {
            this.toolStrip_marcEditor.Enabled = false;
            try
            {
                // 2014/7/3
                bool bVerifyed = false;

                if (this.m_verifyViewer != null)
                    this.m_verifyViewer.Clear();

                if (this.ForceVerifyData == true)
                {
                    GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                    e1.FocusedControl = this.m_marcEditor;

                    // 0: û�з���У�����; 1: ����У�龯��; 2: ����У�����
                    int nRet = this.VerifyData(this, e1, true);
                    if (nRet == 2)
                    {
                        MessageBox.Show(this, "MARC ��¼��У�鷢���д����ܾ����档���޸� MARC ��¼�����±���");
                        return;
                    }
                    bVerifyed = true;
                }

                string strHtml = "";
                SaveBiblioToDatabase(true, out strHtml);
                this.m_webExternalHost_biblio.SetHtmlString(strHtml,
    "entityform_biblio");

                if (this.AutoVerifyData == true
                    && bVerifyed == false)
                    API.PostMessage(this.Handle, WM_VERIFY_DATA, 0, 0);
            }
            finally
            {
                this.toolStrip_marcEditor.Enabled = true;
            }
        }

        // ��������ť�������ݿ�ɾ����Ŀ��¼
        private void toolStripButton_marcEditor_delete_Click(object sender, EventArgs e)
        {
            this.toolStrip_marcEditor.Enabled = false;
            try
            {
                DeleteBiblioFromDatabase();
            }
            finally
            {
                this.toolStrip_marcEditor.Enabled = true;
            }
        }

        // ��������ť���˵����鿴��ǰXML����
        private void MenuItem_marcEditor_viewXml_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";

            // �����Ŀ��¼XML��ʽ
            string strXmlBody = "";
            int nRet = this.GetBiblioXml(
                "", // ��ʹ�Ӽ�¼·���п�marc��ʽ
                true,   // ������ԴID
                out strXmlBody,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            XmlViewerForm dlg = new XmlViewerForm();

            dlg.Text = "��ǰXML����";
            dlg.MainForm = this.MainForm;
            dlg.XmlString = strXmlBody;

            //dlg.StartPosition = FormStartPosition.CenterScreen;
            this.MainForm.AppInfo.LinkFormState(dlg, "entityform_xmlviewer_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
            if (this.MainForm.CanDisplayItemProperty() == true)
                DoViewComment(false);   // ��ʾ�ڹ̶����
            else
                DoViewComment(true);

        }

        // ��������ť���˵����鿴��������XML����
        private void MenuItem_marcEditor_viewOriginXml_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (String.IsNullOrEmpty(this.m_strOriginBiblioXml) == true)
            {
                strError = "�ݲ��߱�ԭʼXML����";
                goto ERROR1;
            }

            XmlViewerForm dlg = new XmlViewerForm();

            dlg.Text = "��������XML����";
            dlg.MainForm = this.MainForm;
            dlg.XmlString = this.m_strOriginBiblioXml;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog();   // ?? this
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // MARC�༭�������ָı�
        private void MarcEditor_TextChanged(object sender, EventArgs e)
        {
            // ****
            this.toolStripButton_marcEditor_save.Enabled = true;

            this.SetSaveAllButtonState(true);

            this._marcEditorVersion++;
        }

        private void easyMarcControl_TextChanged(object sender, EventArgs e)
        {
            // ****
            this.toolStripButton_marcEditor_save.Enabled = true;

            this.SetSaveAllButtonState(true);

            this._templateVersion++;
        }

        // TODO: �������Ѿ����š�ѡ���ť�ƶ���������������
        // ʹ�ܼ�¼ɾ����ġ�ȫ�����桱��ť
        private void ToolStripMenuItem_enableSaveAllButtonAfterRecordDeleted_Click(object sender, EventArgs e)
        {
            if (this.DeletedMode == false)
            {
                MessageBox.Show(this, "�Ѿ�����ͨģʽ");
                return;
            }

            this.entityControl1.ChangeAllItemToNewState();
            this.issueControl1.ChangeAllItemToNewState();
            this.orderControl1.ChangeAllItemToNewState();
            this.commentControl1.ChangeAllItemToNewState();

            // ��MarcEditor�޸ı�Ǳ�Ϊtrue
            // this.m_marcEditor.Changed = true; // ��һ�������ʹ�ܺ���������ر�EntityForm���ڣ��Ƿ�ᾯ��(��Ŀ)���ݶ�ʧ
            this.SetMarcChanged(true);

            this.DeletedMode = false;
            // this.SetSaveAllButtonState(true);
            // this.EnableControls(true);  // 2009/11/11 new add
        }

        // marc�༭��Ҫ���ⲿ��������ļ�����
        private void MarcEditor_GetConfigFile(object sender, DigitalPlatform.Marc.GetConfigFileEventArgs e)
        {
            Debug.Assert(false, "�����Ҫ������¼��ӿ�");

            if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
            {
                e.ErrorInfo = "��¼·��Ϊ�գ��޷���������ļ�";
                return;
            }

            // ���������ļ�

            // �õ��ɾ����ļ���
            string strCfgFileName = e.Path;
            int nRet = strCfgFileName.IndexOf("#");
            if (nRet != -1)
            {
                strCfgFileName = strCfgFileName.Substring(0, nRet);
            }

            string strBiblioDbName = Global.GetDbName(this.BiblioRecPath);

            string strContent = "";
            string strError = "";

            byte[] baCfgOutputTimestamp = null;
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = GetCfgFileContent(strBiblioDbName,
                strCfgFileName,
                out strContent,
                out baCfgOutputTimestamp,
                out strError);
            if (nRet == -1 || nRet == 0)
            {
                e.ErrorInfo = "��������ļ� '" + strCfgFileName + "' ʱ����" + strError;
            }
            else
            {
                byte[] baContent = StringUtil.GetUtf8Bytes(strContent, true);
                MemoryStream stream = new MemoryStream(baContent);
                e.Stream = stream;
            }
        }

        private void MarcEditor_GetConfigDom(object sender, GetConfigDomEventArgs e)
        {
            // Debug.Assert(false, "");

            if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
            {
                e.ErrorInfo = "��¼·��Ϊ�գ��޷���������ļ� '"+e.Path+"'";
                return;
            }

            // �õ��ɾ����ļ���
            string strCfgFileName = e.Path;
            int nRet = strCfgFileName.IndexOf("#");
            if (nRet != -1)
            {
                strCfgFileName = strCfgFileName.Substring(0, nRet);
            }

            string strBiblioDbName = Global.GetDbName(this.BiblioRecPath);

            string strCfgFilePath = strBiblioDbName + "/cfgs/" + strCfgFileName;

            // ��cache��Ѱ��
            e.XmlDocument = this.MainForm.DomCache.FindObject(strCfgFilePath);
            if (e.XmlDocument != null)
                return;

            // ���������ļ�
            string strContent = "";
            string strError = "";

            byte[] baCfgOutputTimestamp = null;
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = GetCfgFileContent(strCfgFilePath,
                out strContent,
                out baCfgOutputTimestamp,
                out strError);
            if (nRet == -1 || nRet == 0)
            {
                e.ErrorInfo = "��������ļ� '" + strCfgFilePath + "' ʱ����" + strError;
            }
            else
            {
                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strContent);
                }
                catch (Exception ex)
                {
                    e.ErrorInfo = "�����ļ� '" + strCfgFilePath + "' װ��XMLDUMʱ����: " + ex.Message;
                    return;
                }
                e.XmlDocument = dom;
                this.MainForm.DomCache.SetObject(strCfgFilePath, dom);  // ���浽����
            }
        }

        private void MarcEditor_GenerateData(object sender, GenerateDataEventArgs e)
        {
            this.AutoGenerate(sender, e);
        }

        private void MarcEditor_VerifyData(object sender, GenerateDataEventArgs e)
        {
            // this.VerifyData(sender, e);
            this.VerifyData(sender, e, false);
        }

        private void MarcEditor_ParseMacro(object sender, ParseMacroEventArgs e)
        {
            string strResult = "";
            string strError = "";

            // ������MacroUtil���д���
            int nRet = m_macroutil.Parse(
                e.Simulate,
                e.Macro,
                out strResult,
                out strError);
            if (nRet == -1)
            {
                e.ErrorInfo = strError;
                return;
            }

            e.Value = strResult;
        }

#if NO
        // MARC��ʽУ��
        // parameters:
        //      sender    �Ӻδ�����? MarcEditor EntityEditForm BindingForm
        /// <summary>
        /// MARC��ʽУ��
        /// </summary>
        /// <param name="sender">�Ӻδ�����?</param>
        /// <param name="e">GenerateDataEventArgs���󣬱�ʾ��������</param>
        public void VerifyData(object sender, 
            GenerateDataEventArgs e)
        {
            VerifyData(sender, e, false);
        }
#endif

        // MARC��ʽУ��
        // parameters:
        //      sender    �Ӻδ�����? MarcEditor EntityEditForm BindingForm
        /// <summary>
        /// MARC��ʽУ��
        /// </summary>
        /// <param name="sender">�Ӻδ�����?</param>
        /// <param name="e">GenerateDataEventArgs���󣬱�ʾ��������</param>
        /// <param name="bAutoVerify">�Ƿ��Զ�У�顣�Զ�У���ʱ�����û�з��ִ����򲻳������ĶԻ���</param>
        /// <returns>0: û�з���У�����; 1: ����У�龯��; 2: ����У�����</returns>
        public int VerifyData(object sender,
            GenerateDataEventArgs e,
            bool bAutoVerify)
        {
            // ��������·��
            string strBiblioDbName = Global.GetDbName(this.BiblioRecPath);

            string strError = "";
            string strCode = "";
            string strRef = "";
            string strOutputFilename = "";

            // Debug.Assert(false, "");
            this.m_strVerifyResult = "����У��...";
            // �Զ�У���ʱ�����û�з��ִ����򲻳������ĶԻ���
            if (bAutoVerify == false)
            {
                // ����̶�������أ��ʹ򿪴���
                DoViewVerifyResult(this.MainForm.PanelFixedVisible == false ? true : false);

                // 2011/8/17
                if (this.MainForm.PanelFixedVisible == true)
                    MainForm.ActivateVerifyResultPage();
            }

            VerifyHost host = new VerifyHost();
            host.DetailForm = this;

            string strCfgFileName = "dp2circulation_marc_verify.fltx";

            byte[] baCfgOutputTimestamp = null;
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            int nRet = GetCfgFile(strBiblioDbName,
                strCfgFileName,
                out strOutputFilename,
                out baCfgOutputTimestamp,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                // .cs �� .cs.ref
                strCfgFileName = "dp2circulation_marc_verify.cs";
                nRet = GetCfgFileContent(strBiblioDbName,
    strCfgFileName,
    out strCode,
    out baCfgOutputTimestamp,
    out strError);
                if (nRet == 0)
                {
                    strError = "��������û�ж���·��Ϊ '" + strBiblioDbName + "/" + strCfgFileName + "' �������ļ�(��.fltx�����ļ�)������У���޷�����";
                    goto ERROR1;
                } 
                if (nRet == -1 || nRet == 0)
                    goto ERROR1;
                strCfgFileName = "dp2circulation_marc_verify.cs.ref";
                nRet = GetCfgFileContent(strBiblioDbName,
                    strCfgFileName,
                    out strRef,
                    out baCfgOutputTimestamp,
                    out strError);
                if (nRet == 0)
                {
                    strError = "��������û�ж���·��Ϊ '" + strBiblioDbName + "/" + strCfgFileName + "' �������ļ�����Ȼ������.cs�����ļ�������У���޷�����";
                    goto ERROR1;
                } 
                if (nRet == -1 || nRet == 0)
                    goto ERROR1;

                try
                {
                    // ִ�д���
                    nRet = RunVerifyCsScript(
                        sender,
                        e,
                        strCode,
                        strRef,
                        out host,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }
                catch (Exception ex)
                {
                    strError = "ִ�нű���������з����쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                    goto ERROR1;
                }
            }
            else
            {


                VerifyFilterDocument filter = null;

                nRet = this.PrepareMarcFilter(
                    host,
                    strOutputFilename,
                    out filter,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�����ļ� '" + strCfgFileName + "' �Ĺ����г���:\r\n" + strError;
                    goto ERROR1;
                }

                try
                {

                    nRet = filter.DoRecord(null,
                        this.GetMarc(),    //  this.m_marcEditor.Marc,
                        0,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                }
                catch (Exception ex)
                {
                    strError = "filter.DoRecord error: " + ExceptionUtil.GetDebugText(ex);
                    goto ERROR1;
                }
            }

            bool bVerifyFail = false;
            if (string.IsNullOrEmpty(host.ResultString) == true)
            {
                if (this.m_verifyViewer != null)
                {
                    this.m_verifyViewer.ResultString = "����У��û�з����κδ���";
                }
            }
            else
            {
                if (bAutoVerify == true)
                {
                    // �ӳٴ򿪴���
                    DoViewVerifyResult(this.MainForm.PanelFixedVisible == false ? true : false);
                }
                this.m_verifyViewer.ResultString = host.ResultString;
                this.MainForm.ActivateVerifyResultPage();   // 2014/7/3
                bVerifyFail = true;
            }

            this.SetSaveAllButtonState(true);   // 2009/3/29 new add
            return bVerifyFail == true ? 2: 0;
        ERROR1:
            MessageBox.Show(this, strError);
            if (this.m_verifyViewer != null)
                this.m_verifyViewer.ResultString = strError;
            return 0;
        }

        int RunVerifyCsScript(
            object sender,
            GenerateDataEventArgs e,
            string strCode,
            string strRef,
            out VerifyHost hostObj,
            out string strError)
        {
            strError = "";
            string[] saRef = null;
            int nRet;
            hostObj = null;

            nRet = ScriptManager.GetRefsFromXml(strRef,
                out saRef,
                out strError);
            if (nRet == -1)
                return -1;

            // 2007/12/4 new add
            ScriptManager.RemoveRefsBinDirMacro(ref saRef);

            string[] saAddRef = {
                                    "system.dll",
                                    "system.xml.dll",
                                    "system.windows.forms.dll",
                                    "system.drawing.dll",
                                    "System.Runtime.Serialization.dll",

									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marceditor.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcfixedfieldcontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
									Environment.CurrentDirectory + "\\digitalplatform.gcatclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.script.dll",
									Environment.CurrentDirectory + "\\digitalplatform.commoncontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									Environment.CurrentDirectory + "\\dp2circulation.exe"
								};

            if (saAddRef != null)
            {
                string[] saTemp = new string[saRef.Length + saAddRef.Length];
                Array.Copy(saRef, 0, saTemp, 0, saRef.Length);
                Array.Copy(saAddRef, 0, saTemp, saRef.Length, saAddRef.Length);
                saRef = saTemp;
            }

            Assembly assembly = null;
            string strErrorInfo = "";
            string strWarningInfo = "";
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saRef,
                null,   // strLibPaths,
                out assembly,
                out strErrorInfo,
                out strWarningInfo);
            if (nRet == -1)
            {
                strError = "�ű����뷢�ִ���򾯸�:\r\n" + strErrorInfo;
                return -1;
            }

            // �õ�Assembly��VerifyHost������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                assembly,
                "dp2Circulation.VerifyHost");
            if (entryClassType == null)
            {

                strError = "dp2Circulation.VerifyHost������û���ҵ�";
                return -1;
            }

            {
                // newһ��VerifyHost��������
                hostObj = (VerifyHost)entryClassType.InvokeMember(null,
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                    null);

                if (hostObj == null)
                {
                    strError = "new VerifyHost���������ʧ��";
                    return -1;
                }

                // ΪHost���������ò���
                hostObj.DetailForm = this;
                hostObj.Assembly = assembly;

                HostEventArgs e1 = new HostEventArgs();
                e1.e = e;   // 2009/2/24 new add

                hostObj.Main(sender, e1);
            }

            return 0;
        }

        /*public*/ int PrepareMarcFilter(
            VerifyHost host,
            string strFilterFileName,
            out VerifyFilterDocument filter,
            out string strError)
        {
            strError = "";

            // �´���
            // string strFilterFileContent = "";

            filter = new VerifyFilterDocument();

            filter.FilterHost = host;
            filter.strOtherDef = "VerifyHost Host = null;";

            filter.strPreInitial = " VerifyFilterDocument doc = (VerifyFilterDocument)this.Document;\r\n";
            filter.strPreInitial += " Host = ("
                + "VerifyHost" + ")doc.FilterHost;\r\n";

            try
            {
                filter.Load(strFilterFileName);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            string strCode = "";    // c#����

            int nRet = filter.BuildScriptFile(out strCode,
                out strError);
            if (nRet == -1)
                goto ERROR1;

#if NO
            string[] saAddRef1 = {
										 this.BinDir + "\\digitalplatform.marcdom.dll",
										 this.BinDir + "\\digitalplatform.marckernel.dll",
										 this.BinDir + "\\digitalplatform.libraryserver.dll",
										 this.BinDir + "\\digitalplatform.dll",
										 this.BinDir + "\\digitalplatform.Text.dll",
										 this.BinDir + "\\digitalplatform.IO.dll",
										 this.BinDir + "\\digitalplatform.Xml.dll",
										 };
#endif
            string[] saAddRef1 = {
                                    "system.dll",
                                    "system.xml.dll",
                                    "system.windows.forms.dll",
                                    "system.drawing.dll",
                                    "System.Runtime.Serialization.dll",

									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marceditor.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcfixedfieldcontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.gcatclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.script.dll",
									Environment.CurrentDirectory + "\\digitalplatform.commoncontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									Environment.CurrentDirectory + "\\dp2circulation.exe"
								};

            Assembly assembly = null;
            string strWarning = "";
            string strLibPaths = "";

            string[] saRef2 = filter.GetRefs();

            string[] saRef = new string[saRef2.Length + saAddRef1.Length];
            Array.Copy(saRef2, saRef, saRef2.Length);
            Array.Copy(saAddRef1, 0, saRef, saRef2.Length, saAddRef1.Length);

            // ����Script��Assembly
            // �������ڶ�saRef���ٽ��к��滻
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saRef,
                strLibPaths,
                out assembly,
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
                // MessageBox.Show(this, strWarning);
            }

            filter.Assembly = assembly;

            return 0;
        ERROR1:
            return -1;
        }

        #region ���µĴ������ݽű�����

        Assembly m_autogenDataAssembly = null;
        string m_strAutogenDataCfgFilename = "";    // �Զ��������ݵ�.cs�ļ�·����ȫ·����������������
        object m_autogenSender = null;
        DetailHost m_detailHostObj = null;

        // �Ƿ�Ϊ�µķ��
        bool AutoGenNewStyle
        {
            get
            {
                if (this.m_detailHostObj == null)
                    return false;

                if (this.m_detailHostObj.GetType().GetMethod("CreateMenu") != null)
                    return true;
                return false;
            }
        }

        // ��ʼ�� dp2circulation_marc_autogen.cs �� Assembly����new DetailHost����
        // return:
        //      -1  error
        //      0   û�����³�ʼ��Assembly������ֱ������ǰCache��Assembly
        //      1   ����(�����״�)��ʼ����Assembly
        /*public*/ int InitialAutogenAssembly(
            string strBiblioRecPath,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // 2014/7/14
            if (string.IsNullOrEmpty(strBiblioRecPath) == true)
                strBiblioRecPath = this.BiblioRecPath;

            // ��������·��
            string strBiblioDbName = Global.GetDbName(strBiblioRecPath);

            if (string.IsNullOrEmpty(strBiblioDbName) == true)
                return 0;

            string strAutogenDataCfgFilename = strBiblioDbName + "/cfgs/" + "dp2circulation_marc_autogen.cs";

            bool bAssemblyReloaded = false;

            // �����Ҫ������׼��Assembly
            if (m_autogenDataAssembly == null
                || m_strAutogenDataCfgFilename != strAutogenDataCfgFilename)
            {
                this.m_autogenDataAssembly = this.MainForm.AssemblyCache.FindObject(strAutogenDataCfgFilename);
                this.m_detailHostObj = null;

                // ���Cache��û���ֳɵ�Assembly
                if (this.m_autogenDataAssembly == null)
                {
                    string strCode = "";
                    string strRef = "";

                    byte[] baCfgOutputTimestamp = null;
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   found
                    nRet = GetCfgFileContent(strAutogenDataCfgFilename,
                        out strCode,
                        out baCfgOutputTimestamp,
                        out strError);
                    if (nRet == -1 || nRet == 0)
                        goto ERROR1;

                    string strCfgFilePath = strBiblioDbName + "/cfgs/" + "dp2circulation_marc_autogen.cs.ref";
                    nRet = GetCfgFileContent(strCfgFilePath,
                        out strRef,
                        out baCfgOutputTimestamp,
                        out strError);
                    if (nRet == -1 || nRet == 0)
                        goto ERROR1;

                    try
                    {
                        // ׼��Assembly
                        Assembly assembly = null;
                        nRet = GetCsScriptAssembly(
                            strCode,
                            strRef,
                            out assembly,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "����ű��ļ� '" + strAutogenDataCfgFilename + "' ʱ����" + strError;
                            goto ERROR1;
                        }
                        // ���䵽����
                        this.MainForm.AssemblyCache.SetObject(strAutogenDataCfgFilename, assembly);

                        this.m_autogenDataAssembly = assembly;

                        bAssemblyReloaded = true;
                    }
                    catch (Exception ex)
                    {
                        strError = "׼���ű���������з����쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                        goto ERROR1;
                    }
                }

                bAssemblyReloaded = true;

                m_strAutogenDataCfgFilename = strAutogenDataCfgFilename;

                // ���ˣ�Assembly�Ѿ���������
                Debug.Assert(this.m_autogenDataAssembly != null, "");
            }

            Debug.Assert(this.m_autogenDataAssembly != null, "");

            // ׼�� host ����
            if (this.m_detailHostObj == null
                || bAssemblyReloaded == true)
            {
                try
                {
                    DetailHost host = null;
                    nRet = NewHostObject(
                        out host,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ִ�нű��ļ� '" + m_strAutogenDataCfgFilename + "' ʱ����" + strError;
                        goto ERROR1;
                    }
                    this.m_detailHostObj = host;

                }
                catch (Exception ex)
                {
                    strError = "׼���ű���������з����쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                    goto ERROR1;
                }
            }

            Debug.Assert(this.m_detailHostObj != null, "");

            if (bAssemblyReloaded == true)
                return 1;
            return 0;
        ERROR1:
            return -1;
        }

        int m_nInFillMenu = 0;

        // �Զ��ӹ�����
        // parameters:
        //      sender    �Ӻδ�����? MarcEditor EntityEditForm BindingForm
        /*public*/ void AutoGenerate(object sender,
            GenerateDataEventArgs e,
            bool bOnlyFillMenu = false)
        {
            int nRet = 0;
            string strError = "";
            bool bAssemblyReloaded = false;

            // ��ֹ����
            if (bOnlyFillMenu == true && this.m_nInFillMenu > 0)
                return;

            this.m_nInFillMenu++;
            try
            {

                // ��ʼ�� dp2circulation_marc_autogen.cs �� Assembly����new DetailHost����
                // return:
                //      -1  error
                //      0   û�����³�ʼ��Assembly������ֱ������ǰCache��Assembly
                //      1   ����(�����״�)��ʼ����Assembly
                nRet = InitialAutogenAssembly(null,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    if (this.m_detailHostObj == null)
                        return; // �������߱����޷���ʼ��
                }
                if (nRet == 1)
                    bAssemblyReloaded = true;

                Debug.Assert(this.m_detailHostObj != null, "");

                if (this.AutoGenNewStyle == true)
                {
                    bool bDisplayWindow = this.MainForm.PanelFixedVisible == false ? true : false;
                    if (bDisplayWindow == true)
                    {
                        if (String.IsNullOrEmpty(e.ScriptEntry) != true
                            && e.ScriptEntry != "Main")
                            bDisplayWindow = false;
                    }

                    if (sender is EntityEditForm
                        && (String.IsNullOrEmpty(e.ScriptEntry) == true
                            || e.ScriptEntry == "Main"))
                    {
                        bDisplayWindow = true;
                    }
                    else if (sender is BindingForm
    && (String.IsNullOrEmpty(e.ScriptEntry) == true
        || e.ScriptEntry == "Main"))
                    {
                        bDisplayWindow = true;
                    }

                    DisplayAutoGenMenuWindow(bDisplayWindow);   // ���ܻ�ı� .ActionTable�Լ� .Count
                    if (bOnlyFillMenu == false)
                    {
                        if (this.MainForm.PanelFixedVisible == true)
                            MainForm.ActivateGenerateDataPage();
                    }

                    if (this.m_genDataViewer != null)
                    {
                        this.m_genDataViewer.sender = sender;
                        this.m_genDataViewer.e = e;
                    }

                    // ��������˵�����
                    if (m_autogenSender != sender
                        || bAssemblyReloaded == true)
                    {
                        if (this.m_genDataViewer != null
                            && this.m_genDataViewer.Count > 0)
                            this.m_genDataViewer.Clear();
                    }
                }
                else // �ɵķ��
                {
                    if (this.m_genDataViewer != null)
                    {
                        this.m_genDataViewer.Close();
                        this.m_genDataViewer = null;
                    }

                    if (this.Focused == true || this.m_marcEditor.Focused)
                        this.MainForm.CurrentGenerateDataControl = null;

                    // �����ͼ����Ϊ���˵�
                    if (bOnlyFillMenu == true)
                        return;
                }

                try
                {
                    // �ɵķ��
                    if (this.AutoGenNewStyle == false)
                    {
                        this.m_detailHostObj.Invoke(String.IsNullOrEmpty(e.ScriptEntry) == true ? "Main" : e.ScriptEntry,
    sender,
    e);
                        this.SetSaveAllButtonState(true);
                        return;
                    }

                    // ��ʼ���˵�
                    try
                    {
                        if (this.m_genDataViewer != null)
                        {
                            // ���ֲ˵�����
                            if (this.m_genDataViewer.Count == 0)
                            {
                                dynamic o = this.m_detailHostObj;
                                o.CreateMenu(sender, e);

                                this.m_genDataViewer.Actions = this.m_detailHostObj.ScriptActions;
                            }

                            // ���ݵ�ǰ�����λ��ˢ�¼�������
                            this.m_genDataViewer.RefreshState();
                        }

                        if (String.IsNullOrEmpty(e.ScriptEntry) == false)
                        {
                            this.m_detailHostObj.Invoke(e.ScriptEntry,
                                sender,
                                e);
                        }
                        else
                        {
                            if (this.MainForm.PanelFixedVisible == true
                                && bOnlyFillMenu == false
                                && this.MainForm.CurrentGenerateDataControl != null)
                            {
                                TableLayoutPanel table = (TableLayoutPanel)this.MainForm.CurrentGenerateDataControl;
                                for (int i = 0; i < table.Controls.Count; i++)
                                {
                                    Control control = table.Controls[i];
                                    if (control is DpTable)
                                    {
                                        control.Focus();
                                        break;
                                    }
                                }

                            }
                        }
                    }
                    catch (Exception /*ex*/)
                    {
                        /*
                        // ���ȸ��þɵķ��
                        this.m_detailHostObj.Invoke(String.IsNullOrEmpty(e.ScriptEntry) == true ? "Main" : e.ScriptEntry,
        sender,
        e);
                        this.SetSaveAllButtonState(true);
                        return;
                         * */
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    strError = "ִ�нű��ļ� '" + m_strAutogenDataCfgFilename + "' �����з����쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                    goto ERROR1;
                }

                this.m_autogenSender = sender;  // �������һ�εĵ��÷�����

                if (bOnlyFillMenu == false
                    && this.m_genDataViewer != null)
                    this.m_genDataViewer.TryAutoRun();
                return;
            ERROR1:
                MessageBox.Show(this, strError);
            }
            finally
            {
                this.m_nInFillMenu--;
            }
        }

        void DisplayAutoGenMenuWindow(bool bOpenWindow)
        {
            // string strError = "";


            // �Ż���������ν�ؽ��з���������
            if (bOpenWindow == false)
            {
                if (this.MainForm.PanelFixedVisible == false
                    && (m_genDataViewer == null || m_genDataViewer.Visible == false))
                    return;
            }


            if (this.m_genDataViewer == null
                || (bOpenWindow == true && this.m_genDataViewer.Visible == false))
            {
                m_genDataViewer = new GenerateDataForm();

                m_genDataViewer.AutoRun = this.MainForm.AppInfo.GetBoolean("detailform", "gen_auto_run", false);
                // MainForm.SetControlFont(m_genDataViewer, this.Font, false);

                {	// �ָ��п��

                    string strWidths = this.MainForm.AppInfo.GetString(
                                   "gen_data_dlg",
                                    "column_width",
                                   "");
                    if (String.IsNullOrEmpty(strWidths) == false)
                    {
                        DpTable.SetColumnHeaderWidth(m_genDataViewer.ActionTable,
                            strWidths,
                            true);
                    }
                }

                // m_genDataViewer.MainForm = this.MainForm;  // �����ǵ�һ��
                m_genDataViewer.Text = "��������";

                m_genDataViewer.DoDockEvent -= new DoDockEventHandler(m_genDataViewer_DoDockEvent);
                m_genDataViewer.DoDockEvent += new DoDockEventHandler(m_genDataViewer_DoDockEvent);

                m_genDataViewer.SetMenu -= new RefreshMenuEventHandler(m_genDataViewer_SetMenu);
                m_genDataViewer.SetMenu += new RefreshMenuEventHandler(m_genDataViewer_SetMenu);

                m_genDataViewer.TriggerAction -= new TriggerActionEventHandler(m_genDataViewer_TriggerAction);
                m_genDataViewer.TriggerAction += new TriggerActionEventHandler(m_genDataViewer_TriggerAction);

                m_genDataViewer.MyFormClosed -= new EventHandler(m_genDataViewer_MyFormClosed);
                m_genDataViewer.MyFormClosed += new EventHandler(m_genDataViewer_MyFormClosed);

                m_genDataViewer.FormClosed -= new FormClosedEventHandler(m_genDataViewer_FormClosed);
                m_genDataViewer.FormClosed += new FormClosedEventHandler(m_genDataViewer_FormClosed);


            }

            if (bOpenWindow == true)
            {
                if (m_genDataViewer.Visible == false)
                {
                    this.MainForm.AppInfo.LinkFormState(m_genDataViewer, "autogen_viewer_state");
                    m_genDataViewer.Show(this);
                    m_genDataViewer.Activate();

                    this.MainForm.CurrentGenerateDataControl = null;
                }
                else
                {
                    if (m_genDataViewer.WindowState == FormWindowState.Minimized)
                        m_genDataViewer.WindowState = FormWindowState.Normal;
                    m_genDataViewer.Activate();
                }
            }
            else
            {
                if (m_genDataViewer.Visible == true)
                {

                }
                else
                {
                    if (this.MainForm.CurrentGenerateDataControl != m_genDataViewer.Table)
                        m_genDataViewer.DoDock(false); // �����Զ���ʾFixedPanel
                }
            }

            if (this.m_genDataViewer != null)
                this.m_genDataViewer.CloseWhenComplete = bOpenWindow;

            return;
            /*
        ERROR1:
            MessageBox.Show(this, "DisplayAutoGenMenu() ����: " + strError);
             * */
        }

        void m_genDataViewer_DoDockEvent(object sender, DoDockEventArgs e)
        {
            if (this.MainForm.CurrentGenerateDataControl != m_genDataViewer.Table)
                this.MainForm.CurrentGenerateDataControl = m_genDataViewer.Table;

            if (e.ShowFixedPanel == true
                && this.MainForm.PanelFixedVisible == false)
                this.MainForm.PanelFixedVisible = true;

            /*
            this.MainForm.AppInfo.SetBoolean("detailform", "gen_auto_run", m_genDataViewer.AutoRun);

            {	// �����п��
                string strWidths = DpTable.GetColumnWidthListString(m_genDataViewer.ActionTable);
                this.MainForm.AppInfo.SetString(
                    "gen_data_dlg",
                    "column_width",
                    strWidths);
            }
             * */

            m_genDataViewer.Docked = true;
            m_genDataViewer.Visible = false;
        }

        void m_genDataViewer_SetMenu(object sender, RefreshMenuEventArgs e)
        {
            if (e.Actions == null || this.m_detailHostObj == null)
                return;

            Type classType = m_detailHostObj.GetType();
            
            foreach (ScriptAction action in e.Actions)
            {
                string strFuncName = action.ScriptEntry + "_setMenu";
                if (string.IsNullOrEmpty(strFuncName) == true)
                    continue;

                DigitalPlatform.Script.SetMenuEventArgs e1 = new DigitalPlatform.Script.SetMenuEventArgs();
                e1.Action = action;
                e1.sender = e.sender;
                e1.e = e.e;

                classType = m_detailHostObj.GetType();
                while (classType != null)
                {
                    try
                    {
                        // �����������ĳ�Ա����
                        classType.InvokeMember(strFuncName,
                            BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.InvokeMethod
                            ,
                            null,
                            this.m_detailHostObj,
                            new object[] { sender, e1 });
                        break;
                    }
                    catch (System.MissingMethodException/*ex*/)
                    {
                        classType = classType.BaseType;
                        if (classType == null)
                            break;
                    }
                }
            }
        }

        void m_genDataViewer_TriggerAction(object sender, TriggerActionArgs e)
        {
            if (this.m_detailHostObj != null)
            {
                if (this.IsDisposed == true)
                {
                    if (this.m_genDataViewer != null)
                    {
                        this.m_genDataViewer.Clear();
                        this.m_genDataViewer.Close();
                        this.m_genDataViewer = null;
                        return;
                    }
                }
                if (String.IsNullOrEmpty(e.EntryName) == false)
                {
                    this.SynchronizeMarc();

                    this.m_detailHostObj.Invoke(e.EntryName,
                        e.sender,
                        e.e);

                    if (this.tabControl_biblioInfo.SelectedTab == this.tabPage_template)
                        this.SynchronizeMarc();
                }

                if (this.m_genDataViewer != null)
                    this.m_genDataViewer.RefreshState();


            }
        }

        void m_genDataViewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_genDataViewer != null)
            {
                this.MainForm.AppInfo.SetBoolean("detailform", "gen_auto_run", m_genDataViewer.AutoRun);

                {	// �����п��
                    string strWidths = DpTable.GetColumnWidthListString(m_genDataViewer.ActionTable);
                    this.MainForm.AppInfo.SetString(
                        "gen_data_dlg",
                        "column_width",
                        strWidths);
                }

                this.MainForm.AppInfo.UnlinkFormState(m_genDataViewer);
                this.m_genDataViewer = null;
            }
        }

        void m_genDataViewer_MyFormClosed(object sender, EventArgs e)
        {
            if (m_genDataViewer != null)
            {
                this.MainForm.AppInfo.SetBoolean("detailform", "gen_auto_run", m_genDataViewer.AutoRun);

                {	// �����п��
                    string strWidths = DpTable.GetColumnWidthListString(m_genDataViewer.ActionTable);
                    this.MainForm.AppInfo.SetString(
                        "gen_data_dlg",
                        "column_width",
                        strWidths);
                }

                this.MainForm.AppInfo.UnlinkFormState(m_genDataViewer);
                this.m_genDataViewer = null;
            }
        }
        int NewHostObject(
            out DetailHost hostObj,
            out string strError)
        {
            strError = "";
            hostObj = null;

            Type entryClassType = ScriptManager.GetDerivedClassType(
    this.m_autogenDataAssembly,
    "dp2Circulation.DetailHost");
            if (entryClassType == null)
            {
                // Ѱ��Host������Type
                entryClassType = ScriptManager.GetDerivedClassType(
                    this.m_autogenDataAssembly,
                    "dp2Circulation.Host");
                if (entryClassType != null)
                {
                    strError = "���Ľű������Ǵ� dp2Circulation.Host ��̳еģ����ַ�ʽĿǰ�Ѳ���֧�֣���Ҫ�޸�(����)Ϊ�� dp2Circulation.DetailHost �̳�";
                    return -1;
                }

                strError = "dp2Circulation.DetailHost�������඼û���ҵ�";
                return -1;
            }

            // newһ��DetailHost��������
            hostObj = (DetailHost)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            if (hostObj == null)
            {
                strError = "new DetailHost�����������ʱʧ��";
                return -1;
            }

            // ΪDetailHost���������ò���
            hostObj.DetailForm = this;
            hostObj.Assembly = this.m_autogenDataAssembly;

            return 0;
        }

        int GetCsScriptAssembly(
            string strCode,
            string strRef,
            out Assembly assembly,
            out string strError)
        {
            strError = "";
            assembly = null;

            string[] saRef = null;
            int nRet;

            nRet = ScriptManager.GetRefsFromXml(strRef,
                out saRef,
                out strError);
            if (nRet == -1)
                return -1;

            ScriptManager.RemoveRefsBinDirMacro(ref saRef);

            string[] saAddRef = {
                                    // 2011/3/4 ����
                                    "system.dll",
                                    "system.xml.dll",
                                    "system.windows.forms.dll",
                                    "system.drawing.dll",
                                    "System.Runtime.Serialization.dll",

									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.xmleditor.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.client.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marceditor.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcfixedfieldcontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
									Environment.CurrentDirectory + "\\digitalplatform.gcatclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.script.dll",
									Environment.CurrentDirectory + "\\digitalplatform.commoncontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.amazoninterface.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.library.dll",
									// Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
									Environment.CurrentDirectory + "\\dp2circulation.exe"
								};

            if (saAddRef != null)
            {
                string[] saTemp = new string[saRef.Length + saAddRef.Length];
                Array.Copy(saRef, 0, saTemp, 0, saRef.Length);
                Array.Copy(saAddRef, 0, saTemp, saRef.Length, saAddRef.Length);
                saRef = saTemp;
            }

            string strErrorInfo = "";
            string strWarningInfo = "";
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saRef,
                null,   // strLibPaths,
                out assembly,
                out strErrorInfo,
                out strWarningInfo);
            if (nRet == -1)
            {
                strError = "�ű����뷢�ִ���򾯸�:\r\n" + strErrorInfo;
                return -1;
            }
            /*
            if (m_scriptDomain != null)
                m_scriptDomain.Load(assembly.GetName());
             * */

            return 0;
        }


        #endregion

#if NO
        // �Զ��ӹ�����
        // parameters:
        //      sender    �Ӻδ�����? MarcEditor EntityEditForm
        public void AutoGenerate(object sender, GenerateDataEventArgs e)
        {
            // ��������·��
            string strBiblioDbName = Global.GetDbName(this.BiblioRecPath);

            string strError = "";
            string strCode = "";
            string strRef = "";

            // Debug.Assert(false, "");

            string strCfgFileName = "dp2circulation_marc_autogen.cs";   // ԭ����dp2_autogen.cs 2007/12/10�޸�Ϊdp2circulation_marc_autogen.cs

            byte[] baCfgOutputTimestamp = null;
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            int nRet = GetCfgFileContent(strBiblioDbName,
                strCfgFileName,
                out strCode,
                out baCfgOutputTimestamp,
                out strError);
            if (nRet == -1 || nRet == 0)
                goto ERROR1;

            strCfgFileName = "dp2circulation_marc_autogen.cs.ref";
            nRet = GetCfgFileContent(strBiblioDbName,
                strCfgFileName,
                out strRef,
                out baCfgOutputTimestamp,
                out strError);
            if (nRet == -1 || nRet == 0)
                goto ERROR1;

            try
            {
                // ִ�д���
                nRet = RunCsScript(
                    sender,
                    e,
                    strCode,
                    strRef,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            catch (Exception ex)
            {
                strError = "ִ�нű���������з����쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                goto ERROR1;
            }

            this.SetSaveAllButtonState(true);   // 2009/3/29 new add
            return;
       ERROR1:
            MessageBox.Show(this, strError);
        }

#endif

        int RunCsScript(
            object sender,
            GenerateDataEventArgs e,
            string strCode,
            string strRef,
            out string strError)
        {
            strError = "";
            string[] saRef = null;
            int nRet;
            // string strWarning = "";

            nRet = ScriptManager.GetRefsFromXml(strRef,
                out saRef,
                out strError);
            if (nRet == -1)
                return -1;

            // 2007/12/4 new add
            ScriptManager.RemoveRefsBinDirMacro(ref saRef);

            string[] saAddRef = {
                                    // 2011/3/4 ����
                                    "system.dll",
                                    "system.xml.dll",
                                    "system.windows.forms.dll",
                                    "system.drawing.dll",
                                    "System.Runtime.Serialization.dll",

									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.xmleditor.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.client.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marceditor.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcfixedfieldcontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
									Environment.CurrentDirectory + "\\digitalplatform.gcatclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.script.dll",
									Environment.CurrentDirectory + "\\digitalplatform.commoncontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.library.dll",
									// Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
									Environment.CurrentDirectory + "\\dp2circulation.exe"
								};

            if (saAddRef != null)
            {
                string[] saTemp = new string[saRef.Length + saAddRef.Length];
                Array.Copy(saRef, 0, saTemp, 0, saRef.Length);
                Array.Copy(saAddRef, 0, saTemp, saRef.Length, saAddRef.Length);
                saRef = saTemp;
            }

            Assembly assembly = null;
            string strErrorInfo = "";
            string strWarningInfo = "";
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saRef,
                null,   // strLibPaths,
                out assembly,
                out strErrorInfo,
                out strWarningInfo);
            if (nRet == -1)
            {
                strError = "�ű����뷢�ִ���򾯸�:\r\n" + strErrorInfo;
                return -1;
            }

            // �õ�Assembly��Host������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                assembly,
                "dp2Circulation.Host");
            if (entryClassType == null)
            {
                /*
                strError = "dp2Circulation.Host������û���ҵ�";
                return -1;
                 * */

                entryClassType = ScriptManager.GetDerivedClassType(
                    assembly,
                    "dp2Circulation.DetailHost");
                if (entryClassType == null)
                {
                    strError = "dp2Circulation.Host���������dp2Circulation.DetailHost�������඼û���ҵ�";
                    return -1;
                }

                // newһ��DetailHost��������
                DetailHost hostObj = (DetailHost)entryClassType.InvokeMember(null,
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                    null);

                if (hostObj == null)
                {
                    strError = "new DetailHost�����������ʱʧ��";
                    return -1;
                }

                // ΪDetailHost���������ò���
                hostObj.DetailForm = this;
                hostObj.Assembly = assembly;

                // hostObj.Main(sender, e);

                // 2009/2/27 new add
                hostObj.Invoke(String.IsNullOrEmpty(e.ScriptEntry) == true ? "Main" : e.ScriptEntry,
                    sender,
                    e);

                return 0;
            }
            else
            {
                // Ϊ�˼��ݣ����������ĵ��÷�ʽ

                // newһ��Host��������
                Host hostObj = (Host)entryClassType.InvokeMember(null,
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                    null);

                if (hostObj == null)
                {
                    strError = "new Host���������ʧ��";
                    return -1;
                }

                // ΪHost���������ò���
                hostObj.DetailForm = this;
                hostObj.Assembly = assembly;

                HostEventArgs e1 = new HostEventArgs();
                e1.e = e;   // 2009/2/24 new add

                /*
                nRet = this.Flush(out strError);
                if (nRet == -1)
                    return -1;
                 * */


                hostObj.Main(sender, e1);

                /*
                nRet = this.Flush(out strError);
                if (nRet == -1)
                    return -1;
                 * */
            }

            return 0;
        }

#if NO
        /// <summary>
        /// ��ʼѭ��
        /// </summary>
        /// <param name="strMessage">Ҫ��ʾ��״̬�е���Ϣ</param>
        public void BeginLoop(string strMessage)
        {
            EnableControls(false);

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial(strMessage);
            Progress.BeginLoop();

            this.Update();
            this.MainForm.Update();
        }

        /// <summary>
        /// ����ѭ��
        /// </summary>
        public void EndLoop()
        {
            Progress.EndLoop();
            Progress.OnStop -= new StopEventHandler(this.DoStop);
            Progress.Initial("");

            EnableControls(true);
        }
#endif

#if NO
        // ����
        private void toolStripButton_searchDup_Click(object sender, EventArgs e)
        {
            string strError = "";

            // �����Ŀ��¼XML��ʽ
            string strXmlBody = "";
            int nRet = this.GetBiblioXml(
                "", // ��ʹ�Ӽ�¼·���п�marc��ʽ
                true,   // ������ԴID
                out strXmlBody,
                out strError);
            if (nRet == -1)
                goto ERROR1;


            DupForm form = new DupForm();

            form.MainForm = this.MainForm;
            form.MdiParent = this.MainForm;

            form.ProjectName = "<Ĭ��>";
            form.XmlRecord = strXmlBody;
            form.RecordPath = this.BiblioRecPath;

            form.AutoBeginSearch = true;

            form.Show();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif

        // ����
        int SearchDup()
        {
            string strError = "";

            // �����Ŀ��¼XML��ʽ
            string strXmlBody = "";
            int nRet = this.GetBiblioXml(
                "", // ��ʹ�Ӽ�¼·���п�marc��ʽ
                true,   // ������ԴID
                out strXmlBody,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            bool bExistDupForm = this.MainForm.GetTopChildWindow<DupForm>() != null;

            DupForm form = this.MainForm.EnsureDupForm();
            Debug.Assert(form != null, "");

            /*
            form.MainForm = this.MainForm;
            form.MdiParent = this.MainForm;
             * */

            form.ProjectName = "<Ĭ��>";
            form.XmlRecord = strXmlBody;
            form.RecordPath = this.BiblioRecPath;

            /*
            form.AutoBeginSearch = true;
            form.Show();
            form.WaitSearchFinish();
             * */
            Global.Activate(form);
            nRet = form.DoSearch(out strError);
            if (nRet == -1)
            {
                MessageBox.Show(form, strError);
                return -1;
            }

            if (form.GetDupCount() == 0)
            {
                if (bExistDupForm == true)
                {
                    // �Ѳ��ش�ѹ������
                    this.Activate();
                }
                else
                {
                    // �ص����ش�
                    form.Close();
                }
                return 0;
            }

            MessageBox.Show(form, "�����¼ʱ���Զ����أ������ظ���¼");
            return 1;
        ERROR1:
            this.Activate();
            MessageBox.Show(this, strError);
            return -1;
        }

        // 
        /// <summary>
        /// ��ó����������Ϣ
        /// </summary>
        /// <param name="strPublisherNumber">���������</param>
        /// <param name="str210">���� 210 �ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û���ҵ�; 1: �ҵ�</returns>
        public int GetPublisherInfo(string strPublisherNumber,
            out string str210,
            out string strError)
        {
            strError = "";
            str210 = "";

            string strDbName = this.MainForm.GetPublisherUtilDbName();

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "��δ����publisher���͵�ʵ�ÿ���";
                return -1;
            }

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("���ڻ�ó�������Ϣ ...");
            Progress.BeginLoop();

            try
            {
                string strAction = "";

                long lRet = Channel.GetUtilInfo(
                    Progress,
                    strAction,
                    strDbName,
                    "ISBN",
                    strPublisherNumber,
                    "v210",
                    out str210,
                    out strError);
                if (lRet == -1)
                    return -1;

                if (lRet == 0)
                    return 0;

            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }


            return 1;
        }

        // 
        /// <summary>
        /// ���ó����������Ϣ
        /// </summary>
        /// <param name="strPublisherNumber">���������</param>
        /// <param name="str210">210 �ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int SetPublisherInfo(string strPublisherNumber,
            string str210,
            out string strError)
        {
            strError = "";

            string strDbName = this.MainForm.GetPublisherUtilDbName();

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "��δ����publisher���͵�ʵ�ÿ���";
                return -1;
            }

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("�������ó�������Ϣ ...");
            Progress.BeginLoop();

            try
            {
                string strAction = "";

                long lRet = Channel.SetUtilInfo(
                    Progress,
                    strAction,
                    strDbName,
                    "ISBN",
                    "r",
                    "i",
                    "v210",
                    strPublisherNumber,
                    str210,
                    out strError);
                if (lRet == -1)
                    return -1;

                return (int)lRet;
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }

        }

        // 
        /// <summary>
        /// ���102�����Ϣ
        /// </summary>
        /// <param name="strPublisherNumber">���������</param>
        /// <param name="str102">���� 102 �ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û���ҵ�; 1: �ҵ�</returns>
        public int Get102Info(string strPublisherNumber,
            out string str102,
            out string strError)
        {
            strError = "";
            str102 = "";

            string strDbName = this.MainForm.GetPublisherUtilDbName();

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "��δ����publisher���͵�ʵ�ÿ���";
                return -1;
            }

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("���ڻ��102��Ϣ ...");
            Progress.BeginLoop();

            try
            {
                string strAction = "";

                long lRet = Channel.GetUtilInfo(
                    Progress,
                    strAction,
                    strDbName,
                    "ISBN",
                    strPublisherNumber,
                    "v102",
                    out str102,
                    out strError);
                if (lRet == -1)
                    return -1;

                if (lRet == 0)
                    return 0;

            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }


            return 1;
        }

        // 
        /// <summary>
        /// ����102�����Ϣ
        /// </summary>
        /// <param name="strPublisherNumber">���������</param>
        /// <param name="str102">102 �ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int Set102Info(string strPublisherNumber,
            string str102,
            out string strError)
        {
            strError = "";

            string strDbName = this.MainForm.GetPublisherUtilDbName();

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "��δ����publisher���͵�ʵ�ÿ���";
                return -1;
            }

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("��������102��Ϣ ...");
            Progress.BeginLoop();

            try
            {
                string strAction = "";

                long lRet = Channel.SetUtilInfo(
                    Progress,
                    strAction,
                    strDbName,
                    "ISBN",
                    "r",
                    "i",
                    "v102",
                    strPublisherNumber,
                    str102,
                    out strError);
                if (lRet == -1)
                    return -1;

                return (int)lRet;
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }

        }

        // (����)�����Ŀ��¼Ϊ��ע�����������Ĳᡢ�������ڡ���ע��¼�Ͷ�����Դ
        private void toolStripButton1_marcEditor_saveTo_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.MainForm.Version < 2.39)
            {
                strError = "��������Ҫ��� dp2library 2.39 �����ϰ汾����ʹ��";
                goto ERROR1;
            }

            string strTargetRecPath = this.m_marcEditor.Record.Fields.GetFirstSubfield("998", "t");
            if (string.IsNullOrEmpty(strTargetRecPath) == false)
            {
                DialogResult result = MessageBox.Show(this,
    "��ǰ�����ڵļ�¼ԭ���Ǵ� '"+strTargetRecPath+"' ���ƹ����ġ��Ƿ�Ҫ���ƻ�ԭ��λ�ã�\r\n\r\nYes: ��; No: �񣬼���������ͨ���Ʋ���; Cancel: �������β���",
    "EntityForm",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return;
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    // strTargetRecPath�ᷢ������
                }

                if (result == System.Windows.Forms.DialogResult.No)
                {
                    strTargetRecPath = "";
                }
            }


            bool bSaveAs = false;   // Դ��¼ID����'?'��׷�ӷ�ʽ������ζ�����ݿ���û��Դ��¼

            // Դ��¼���� ��
            if (Global.IsAppendRecPath(this.BiblioRecPath) == true)
            {
                bSaveAs = true;
            }

            MergeStyle merge_style = MergeStyle.CombinSubrecord | MergeStyle.ReserveSourceBiblio;

            BiblioSaveToDlg dlg = new BiblioSaveToDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            // dlg.RecPath = this.BiblioRecPath;
            if (string.IsNullOrEmpty(strTargetRecPath) == false)
                dlg.RecPath = strTargetRecPath;
            else
            {
                dlg.RecPath = this.MainForm.AppInfo.GetString(
                    "entity_form",
                    "save_to_used_path",
                    this.BiblioRecPath);
                dlg.RecID = "?";
            }

            if (bSaveAs == false)
                dlg.MessageText = "(ע��������*��ѡ��*�Ƿ�����Ŀ��¼�����Ĳᡢ�ڡ�������ʵ���¼�Ͷ�����Դ)\r\n\r\n����ǰ�����е���Ŀ��¼ " + this.BiblioRecPath + " ���Ƶ�:";
            else
            {
                dlg.Text = "��������Ŀ��¼���ض�λ��";
                dlg.MessageText = "ע��\r\n1) ��ǰִ�е��Ǳ�������Ǹ��Ʋ���(��Ϊ���ݿ����滹û��������¼);\r\n2) ��Ŀ��¼�����Ĳᡢ�ڡ�������ʵ���¼�Ͷ�����Դ�ᱻһ������";
                dlg.EnableCopyChildRecords = false;
            }

            if (string.IsNullOrEmpty(strTargetRecPath) == false)
                dlg.BuildLink = false;
            else
            {
                if (bSaveAs == false)
                    dlg.BuildLink = this.MainForm.AppInfo.GetBoolean(
                        "entity_form",
                        "when_save_to_build_link",
                        true);
                else
                    dlg.BuildLink = false;
            }

            if (bSaveAs == false)
                dlg.CopyChildRecords = this.MainForm.AppInfo.GetBoolean(
                    "entity_form",
                    "when_save_to_copy_child_records",
                    false);
            else
                dlg.CopyChildRecords = true;

            dlg.CurrentBiblioRecPath = this.BiblioRecPath;
            this.MainForm.AppInfo.LinkFormState(dlg, "entityform_BiblioSaveToDlg_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            if (this.BiblioRecPath == dlg.RecPath)
            {
                strError = "Ҫ���浽��λ�� '"+dlg.RecPath+"' �͵�ǰ��¼������λ�� '"+this.BiblioRecPath+"' ��ͬ�����Ʋ������ܾ�����ȷʵҪ���������¼����ֱ��ʹ�ñ��湦�ܡ�";
                goto ERROR1;
            }

            if (bSaveAs == false)
            {
                this.MainForm.AppInfo.SetBoolean(
                    "entity_form",
                    "when_save_to_build_link",
                    dlg.BuildLink);
                this.MainForm.AppInfo.SetBoolean(
                    "entity_form",
                    "when_save_to_copy_child_records",
                    dlg.CopyChildRecords);
            }
            this.MainForm.AppInfo.SetString(
    "entity_form",
    "save_to_used_path",
    dlg.RecPath);

            // Դ��¼���� ��
            if (bSaveAs == true)
            {
                this.BiblioRecPath = dlg.RecPath;

                // �ύ���б�������
                // return:
                //      -1  �д���ʱ���ų���Щ��Ϣ����ɹ���
                //      0   �ɹ���
                nRet = DoSaveAll();
                if (nRet == -1)
                {
                    strError = "�����������";
                    goto ERROR1;
                }

                return;
            }

            // if (dlg.CopyChildRecords == true)
            {
                // �����ǰ��¼û�б��棬���ȱ���
                if (this.EntitiesChanged == true
        || this.IssuesChanged == true
        || this.BiblioChanged == true
        || this.ObjectChanged == true
        || this.OrdersChanged == true
        || this.CommentsChanged == true)
                {
                    // ������δ����
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ�������� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档���Ʋ���ǰ�����ȱ��浱ǰ��¼��\r\n\r\n����Ҫ��������ô��",
                        "EntityForm",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.OK)
                    {
                        // �ύ���б�������
                        // return:
                        //      -1  �д���ʱ���ų���Щ��Ϣ����ɹ���
                        //      0   �ɹ���
                        nRet = DoSaveAll();
                        if (nRet == -1)
                        {
                            strError = "��Ϊ��������������Ժ����ĸ��Ʋ���������";
                            goto ERROR1;
                        }
                    }
                    else
                    {
                        strError = "���Ʋ���������";
                        goto ERROR1;
                    }
                }
            }

            // ����Ҫ����λ�ã���¼�Ƿ��Ѿ�����?
            // TODO������Ҫ����Ϊ�ϲ������߸��ǡ���������ɾ��Ŀ��λ�õļ�¼��
            if (dlg.RecID != "?")
            {
                byte[] timestamp = null;

                // ����ض�λ����Ŀ��¼�Ƿ��Ѿ�����
                // parameters:
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = DetectBiblioRecord(dlg.RecPath,
                    out timestamp,
                    out strError);
                if (nRet == 1)
                {
                    if (dlg.RecPath != strTargetRecPath)
                    {
#if NO
                        // ���Ѹ��ǣ�
                        DialogResult result = MessageBox.Show(this,
                            "��Ŀ��¼ " + dlg.RecPath + " �Ѿ����ڡ�\r\n\r\nҪ�õ�ǰ�����е���Ŀ��¼���Ǵ˼�¼ô? ",
                            "EntityForm",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result != DialogResult.Yes)
                            return;
#endif
                        GetMergeStyleDialog merge_dlg = new GetMergeStyleDialog();
                        MainForm.SetControlFont(merge_dlg, this.Font, false);
                        merge_dlg.SourceRecPath = this.BiblioRecPath;
                        merge_dlg.TargetRecPath = dlg.RecPath;
                        merge_dlg.MessageText = "Ŀ����Ŀ��¼ " + dlg.RecPath + " �Ѿ����ڡ�\r\n\r\n��ָ����ǰ�����е���Ŀ��¼(Դ)�ʹ�Ŀ���¼�ϲ��ķ���";

                        merge_dlg.UiState = this.MainForm.AppInfo.GetString(
        "entity_form",
        "GetMergeStyleDialog_copy_uiState",
        "");
                        merge_dlg.EnableSubRecord = dlg.CopyChildRecords;

                        this.MainForm.AppInfo.LinkFormState(merge_dlg, "entityform_GetMergeStyleDialog_copy_state");
                        merge_dlg.ShowDialog(this);
                        this.MainForm.AppInfo.UnlinkFormState(merge_dlg);
                        this.MainForm.AppInfo.SetString(
"entity_form",
"GetMergeStyleDialog_copy_uiState",
merge_dlg.UiState);

                        if (merge_dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                            return;

                        merge_style = merge_dlg.GetMergeStyle();

                    }

                    // this.BiblioTimestamp = timestamp;   // Ϊ��˳������

                    // TODO: Ԥ�ȼ�������Ȩ�ޣ�ȷ��ɾ����Ŀ��¼���¼���¼���ܳɹ�������;���

#if NO
                    // ɾ��Ŀ��λ�õ���Ŀ��¼����������������ʵ��ȼ�¼
                    nRet = DeleteBiblioRecordFromDatabase(dlg.RecPath,
                        "onlydeletebiblio",
                        timestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
#endif
                    if ((merge_style & MergeStyle.OverwriteSubrecord) != 0)
                    {
                        // ɾ��Ŀ���¼����������ɾ��Ŀ��λ�õ��¼���¼
                        // TODO: ���Ե�ʱ��ע�ⲻ���������ö����Ա���Ŀ����Ŀ��¼�ж���Ŀ�����
                        nRet = DeleteBiblioRecordFromDatabase(dlg.RecPath,
                            (merge_style & MergeStyle.ReserveSourceBiblio) != 0 ? "delete" : "onlydeletesubrecord",
                            timestamp,
                            out strError);
                        if (nRet == -1)
                        {
                            if ((merge_style & MergeStyle.ReserveSourceBiblio) != 0)
                                strError = "ɾ��Ŀ��λ�õ���Ŀ��¼ '" + dlg.RecPath + "' ʱ����: " + strError;
                            else
                                strError = "ɾ��Ŀ��λ�õ���Ŀ��¼ '" + dlg.RecPath + "' ��ȫ���Ӽ�¼ʱ����: " + strError;
                            goto ERROR1;
                        }
                    }

                }
            }

            string strOutputBiblioRecPath = "";
            byte[] baOutputTimestamp = null;
            string strXml = "";

            string strOldBiblioRecPath = this.BiblioRecPath;
            string strOldMarc = this.GetMarc();    //  this.m_marcEditor.Marc;
            bool bOldChanged = this.GetMarcChanged();   //  this.m_marcEditor.Changed;

            try
            {

                // ����ԭ���ļ�¼·��
                bool bOldReadOnly = this.m_marcEditor.ReadOnly;
                Field old_998 = null;
                // bool bOldChanged = this.BiblioChanged;

                if (dlg.BuildLink == true)
                {
                    nRet = this.MainForm.CheckBuildLinkCondition(
                        dlg.RecPath,    // ��������/����ļ�¼
                        strOldBiblioRecPath,    // ����ǰ�ļ�¼
                        false,
                        out strError);
                    if (nRet == -1 || nRet == 0)
                    {
                        // 
                        strError = "�޷�Ϊ��¼ '" + this.BiblioRecPath + "' ����ָ�� '" + strOldBiblioRecPath + "' ��Ŀ���ϵ��" + strError;
                        MessageBox.Show(this, strError);
                    }
                    else
                    {
                        // ���浱ǰ��¼��998�ֶ�
                        old_998 = this.m_marcEditor.Record.Fields.GetOneField("998", 0);

                        this.m_marcEditor.Record.Fields.SetFirstSubfield("998", "t", strOldBiblioRecPath);
                        /*
                        if (bOldReadOnly == false)
                            this.MarcEditor.ReadOnly = true;
                        */
                    }
                }
                else
                {
                    // ���浱ǰ��¼��998�ֶ�
                    old_998 = this.m_marcEditor.Record.Fields.GetOneField("998", 0);

                    // ������ܴ��ڵ�998$t
                    if (old_998 != null)
                    {
                        SubfieldCollection subfields = old_998.Subfields;
                        Subfield old_t = subfields["t"];
                        if (old_t != null)
                        {
                            old_998.Subfields = subfields.Remove(old_t);
                            // ���998��һ�����ֶ�Ҳû���ˣ��Ƿ�����ֶ�Ҫɾ��?
                        }
                        else
                            old_998 = null; // ��ʾ(��Ȼû��ɾ��$t����)���ûָ�
                    }
                }

                string strMergeStyle = "";
                if ((merge_style & MergeStyle.ReserveSourceBiblio) != 0)
                    strMergeStyle = "reserve_source";
                else
                    strMergeStyle = "reserve_target";

                if ((merge_style & MergeStyle.MissingSourceSubrecord) != 0)
                    strMergeStyle += ",missing_source_subrecord";
                else if ((merge_style & MergeStyle.OverwriteSubrecord) != 0)
                {
                    // dp2library ��δʵ��������ܣ�����������ǰ���Ѿ��� SetBiblioInfo() API ����ɾ����Ŀ��λ���������Ӽ�¼��Ч����һ���ġ�(��Ȼ������ʵ������ԭ���Բ�����ô��)
                    // strMergeStyle += ",overwrite_target_subrecord";
                }

                if (dlg.CopyChildRecords == false)
                {
                    nRet = CopyBiblio(
        "onlycopybiblio",
        dlg.RecPath,
        strMergeStyle,
        out strXml,
        out strOutputBiblioRecPath,
        out baOutputTimestamp,
        out strError);
                    if (nRet == -1)
                        MessageBox.Show(this, strError);
                }
                else
                {
                    nRet = CopyBiblio(
                        "copy",
                        dlg.RecPath,
                        strMergeStyle,
                        out strXml,
                        out strOutputBiblioRecPath,
                        out baOutputTimestamp,
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(this, strError);
                }

            }
            finally
            {
#if NO
                // ��ԭ��ǰ���ڵļ�¼
                if (this.m_marcEditor.Marc != strOldMarc)
                    this.m_marcEditor.Marc = strOldMarc;
                if (this.m_marcEditor.Changed != bOldChanged)
                    this.m_marcEditor.Changed = bOldChanged;
#endif
                if (this.GetMarc() /*this.m_marcEditor.Marc*/ != strOldMarc)
                {
                    // this.m_marcEditor.Marc = strOldMarc;
                    this.SetMarc(strOldMarc);
                }
                if (this.GetMarcChanged() /*this.m_marcEditor.Changed*/ != bOldChanged)
                {
                    // this.m_marcEditor.Changed = bOldChanged;
                    this.SetMarcChanged(bOldChanged);
                }
            }

            if (nRet == -1)
            {
                this.BiblioRecPath = strOldBiblioRecPath;

#if NO
                if (old_998 != null)
                {
                    // �ָ���ǰ��998�ֶ�����
                    for (int i = 0; i < this.MarcEditor.Record.Fields.Count; i++)
                    {
                        Field temp = this.MarcEditor.Record.Fields[i];
                        if (temp.Name == "998")
                        {
                            this.MarcEditor.Record.Fields.RemoveAt(i);
                            i--;
                        }
                    }
                    if (old_998 != null)
                    {
                        this.MarcEditor.Record.Fields.Insert(this.MarcEditor.Record.Fields.Count,
                            old_998.Name,
                            old_998.Indicator,
                            old_998.Value);
                    }
                    // �ָ�����ǰ��ReadOnly
                    if (this.MarcEditor.ReadOnly != bOldReadOnly)
                        this.MarcEditor.ReadOnly = bOldReadOnly;

                    if (this.BiblioChanged != bOldChanged)
                        this.BiblioChanged = bOldChanged;
                }
#endif
                return;
            }

            // TODO: ѯ���Ƿ�Ҫ����װ��Ŀ���¼����ǰ���ڣ�����װ���µ�һ���ֲᴰ�����ǲ�װ�룿
            {
                DialogResult result = MessageBox.Show(this,
        "���Ʋ����Ѿ��ɹ���\r\n\r\n�����Ƿ�������Ŀ���¼ '" + strOutputBiblioRecPath + "' װ��һ���µ��ֲᴰ�Ա���й۲�? \r\n\r\n��(Yes): װ��һ���µ��ֲᴰ��\r\n��(No): װ�뵱ǰ���ڣ�\r\nȡ��(Cancel): ��װ��Ŀ���¼���κδ���",
        "EntityForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    EntityForm form = new EntityForm();
                    form.MdiParent = this.MainForm;
                    form.MainForm = this.MainForm;
                    form.Show();
                    Debug.Assert(form != null, "");

                    form.LoadRecordOld(strOutputBiblioRecPath, "", true);
                    return;
                }
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return;
            }

            // ��Ŀ���¼װ�뵱ǰ����
            this.LoadRecordOld(strOutputBiblioRecPath, "", false);
#if NO
            // ��Ŀ���¼װ�뵱ǰ����
            this.BiblioTimestamp = baOutputTimestamp;
            this.BiblioRecPath = strOutputBiblioRecPath;


            string strBiblioDbName = Global.GetDbName(this.BiblioRecPath);

            // bool bError = false;

            // װ���¼�¼��entities����

            // ����װ����ص����в�
            string strItemDbName = this.MainForm.GetItemDbName(strBiblioDbName);
            if (String.IsNullOrEmpty(strItemDbName) == false) // ���ڵ�ǰ��Ŀ���ж�Ӧ��ʵ���ʱ����װ����¼
            {
                this.EnableItemsPage(true);

                nRet = this.entityControl1.LoadEntityRecords(this.BiblioRecPath,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    // bError = true;
                }
            }
            else
            {
                this.EnableItemsPage(false);
                this.entityControl1.ClearEntities();
            }

            // ����װ����ص�������
            string strIssueDbName = this.MainForm.GetIssueDbName(strBiblioDbName);
            if (String.IsNullOrEmpty(strIssueDbName) == false) // ���ڵ�ǰ��Ŀ���ж�Ӧ���ڿ�ʱ����װ���ڼ�¼
            {
                this.EnableIssuesPage(true);
                nRet = this.issueControl1.LoadIssueRecords(this.BiblioRecPath,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    // bError = true;
                }
            }
            else
            {
                this.EnableIssuesPage(false);
                this.issueControl1.ClearIssues();
            }
            // ����װ����ص����ж�����Ϣ
            string strOrderDbName = this.MainForm.GetOrderDbName(strBiblioDbName);
            if (String.IsNullOrEmpty(strOrderDbName) == false) // ���ڵ�ǰ��Ŀ���ж�Ӧ�Ĳɹ���ʱ����װ��ɹ���¼
            {
                if (String.IsNullOrEmpty(strIssueDbName) == false)
                    this.orderControl1.SeriesMode = true;
                else
                    this.orderControl1.SeriesMode = false;

                this.EnableOrdersPage(true);
                nRet = this.orderControl1.LoadOrderRecords(this.BiblioRecPath,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    // bError = true;
                }
            }
            else
            {
                this.EnableOrdersPage(false);
                this.orderControl1.ClearOrders();
            }

            // ����װ����ص�������ע��Ϣ
            string strCommentDbName = this.MainForm.GetCommentDbName(strBiblioDbName);
            if (String.IsNullOrEmpty(strCommentDbName) == false) // ���ڵ�ǰ��Ŀ���ж�Ӧ����ע��ʱ����װ����ע��¼
            {
                this.EnableCommentsPage(true);
                nRet = this.commentControl1.LoadCommentRecords(this.BiblioRecPath,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    // bError = true;
                }
            }
            else
            {
                this.EnableCommentsPage(false);
                this.commentControl1.ClearComments();
            }

            // ����װ�������Դ
            if (dlg.CopyChildRecords == true)
            {
                nRet = this.binaryResControl1.LoadObject(this.BiblioRecPath,    // 2008/11/2 changed
                    strXml,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    // bError = true;
                    // return -1;
                }
            }

            // װ����Ŀ��<dprms:file>���������XMLƬ��
            {
                nRet = LoadXmlFragment(strXml,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                }
            }

            /*
            // û�ж�����Դ
            if (this.binaryResControl1 != null)
            {
                this.binaryResControl1.Clear();
            }
             * */

            // TODO: װ��HTML?
            Global.SetHtmlString(this.webBrowser_biblioRecord, "(�հ�)");   // ��ʱˢ��Ϊ�հ�
#endif

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// ��Ǽ�
        /// </summary>
        public void DoRegisterEntity()
        {
            int nRet = 0;
            string strError = "";

            // TODO: Ҫ���EnableControls���ö��Ƕ�׵����⡣���⽹��ת���Ƿ���ȷ��
            this.EnableControls(false);
            try
            {

                // ��������������Ƿ�ΪISBN����
                if (IsISBnBarcode(this.textBox_itemBarcode.Text) == true)
                {
                    // ���浱ǰ����Ϣ
                        nRet = this.entityControl1.DoSaveItems();
                        if (nRet == -1)
                            return; // ������һ������

                    // ת���������ּ�������
                    this.textBox_queryWord.Text = this.textBox_itemBarcode.Text;
                    this.textBox_itemBarcode.Text = "";

                    this.button_search_Click(null, null);
                    return;
                }

                // �����������ʽ�Ƿ�Ϸ�
                if (NeedVerifyItemBarcode == true
                    && string.IsNullOrEmpty(this.textBox_itemBarcode.Text) == false)    // 2009/11/24 �յ��ַ��������м��
                {
                    // ��ʽУ�������
                    // return:
                    //      -2  ������û������У�鷽�����޷�У��
                    //      -1  error
                    //      0   ���ǺϷ��������
                    //      1   �ǺϷ��Ķ���֤�����
                    //      2   �ǺϷ��Ĳ������
                    nRet = VerifyBarcode(
                        this.Channel.LibraryCodeList,
                        this.textBox_itemBarcode.Text,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // ���������Ÿ�ʽ���Ϸ�
                    if (nRet == 0)
                    {
                        strError = "������������ " + this.textBox_itemBarcode.Text + " ��ʽ����ȷ(" + strError + ")�����������롣";
                        goto ERROR1;
                    }

                    // ʵ��������Ƕ���֤�����
                    if (nRet == 1)
                    {
                        strError = "������������ " + this.textBox_itemBarcode.Text + " �Ƕ���֤����š������������š�";
                        goto ERROR1;
                    }

                    // ���ڷ�����û������У�鹦�ܣ�����ǰ�˷�����У��Ҫ������������һ��
                    if (nRet == -2)
                        MessageBox.Show(this, "���棺ǰ�˿�����У������Ź��ܣ����Ƿ�������ȱ����Ӧ�Ľű��������޷�У������š�\r\n\r\n��Ҫ������ִ˾���Ի�����ر�ǰ��У�鹦��");

                }

                ActivateItemsPage();

                if (this.RegisterType == RegisterType.Register)
                {
                    // �Ǽ�
                    this.entityControl1.DoNewEntity(this.textBox_itemBarcode.Text);

                    this.SwitchFocus(ITEM_BARCODE);
                }
                else if (this.RegisterType == RegisterType.QuickRegister)
                {
                    // ���ٵǼ�
                    nRet = this.entityControl1.DoQuickNewEntity(this.textBox_itemBarcode.Text);
                    if (nRet != -1)
                    {
                        /*
                        this.textBox_itemBarcode.SelectAll();
                        this.textBox_itemBarcode.Focus();
                         * */
                        this.SwitchFocus(ITEM_BARCODE);
                    }

                }
                else if (this.RegisterType == RegisterType.SearchOnly)
                {
                    // ֻ����
                    //this.EnableControls(false);
                    LoadItemByBarcode(this.textBox_itemBarcode.Text,
                        this.checkBox_autoSavePrev.Checked);
                    //this.EnableControls(true);
                }
            }
            finally
            {
                this.EnableControls(true);
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        static bool IsISBnBarcode(string strText)
        {
            if (strText.Length == 13)
            {
                string strHead = strText.Substring(0, 3);
                if (strHead == "978" || strHead == "979")
                    return true;
            }

            return false;
        }

        void LoadFontToMarcEditor()
        {
            string strFontString = MainForm.AppInfo.GetString(
                "marceditor",
                "fontstring",
                "");  // "Arial Unicode MS, 12pt"

            if (String.IsNullOrEmpty(strFontString) == false)
            {
                // Create the FontConverter.
                System.ComponentModel.TypeConverter converter =
                    System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));

                this.m_marcEditor.Font = (Font)converter.ConvertFromString(strFontString);
            }

            string strFontColor = MainForm.AppInfo.GetString(
                "marceditor",
                "fontcolor",
                "");

            if (String.IsNullOrEmpty(strFontColor) == false)
            {
                // Create the ColorConverter.
                System.ComponentModel.TypeConverter converter =
                    System.ComponentModel.TypeDescriptor.GetConverter(typeof(Color));

                this.m_marcEditor.ContentTextColor = (Color)converter.ConvertFromString(strFontColor);
            }
        }

        void SaveFontForMarcEditor()
        {
            {
                // Create the FontConverter.
                System.ComponentModel.TypeConverter converter =
                    System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));

                string strFontString = converter.ConvertToString(this.m_marcEditor.Font);

                MainForm.AppInfo.SetString(
                    "marceditor",
                    "fontstring",
                    strFontString);
            }

            {
                // Create the ColorConverter.
                System.ComponentModel.TypeConverter converter =
                    System.ComponentModel.TypeDescriptor.GetConverter(typeof(Color));

                string strFontColor = converter.ConvertToString(this.m_marcEditor.ContentTextColor);

                MainForm.AppInfo.SetString(
                    "marceditor",
                    "fontcolor",
                    strFontColor);
            }
        }

        /// <summary>
        /// �ָ�ȱʡ����
        /// </summary>
        public new void RestoreDefaultFont()
        {
            if (this.MainForm != null)
            {
                Size oldsize = this.Size;
                if (this.MainForm.DefaultFont == null)
                {
                    MainForm.SetControlFont(this, Control.DefaultFont);
                    this.m_marcEditor.Font = Control.DefaultFont;
                }
                else
                {
                    MainForm.SetControlFont(this, this.MainForm.DefaultFont);
                    this.m_marcEditor.Font = this.MainForm.DefaultFont;
                }
                this.Size = oldsize;

                // ���浽�����ļ�
                SaveFontForMarcEditor();
            }
        }

        // 
        /// <summary>
        /// ��������
        /// </summary>
        public void SetFont()
        {
            if (this.m_marcEditor.Focused)
                SetMarcEditFont();
            else
            {
                MessageBox.Show(this, "���Ҫ���� MARC�༭�� �����壬�뽫���뽹������ MARC�༭�� ����ʹ�ñ����ܡ�\r\n\r\n���Ҫ���ô������������ֵ����壬��ʹ�����˵��ġ��������á�����������ֵĶԻ�����ѡ����ۡ�����ҳ�����á�ȱʡ���塱");
            }
        }

        /// <summary>
        /// ���� MARC �༭��������
        /// </summary>
        public void SetMarcEditFont()
        {
            FontDialog dlg = new FontDialog();
            dlg.ShowColor = true;
            dlg.Color = this.m_marcEditor.ContentTextColor;
            dlg.Font = this.m_marcEditor.Font;
            dlg.ShowApply = true;
            dlg.ShowHelp = true;
            dlg.AllowVerticalFonts = false;

            dlg.Apply -= new EventHandler(dlgMarcEditFont_Apply);
            dlg.Apply += new EventHandler(dlgMarcEditFont_Apply);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            this.m_marcEditor.Font = dlg.Font;
            this.m_marcEditor.ContentTextColor = dlg.Color;

            // ���浽�����ļ�
            SaveFontForMarcEditor();
        }

        void dlgMarcEditFont_Apply(object sender, EventArgs e)
        {
            FontDialog dlg = (FontDialog)sender;

            this.m_marcEditor.Font = dlg.Font;
            this.m_marcEditor.ContentTextColor = dlg.Color;

            // ���浽�����ļ�
            SaveFontForMarcEditor();
        }

        /// <summary>
        /// �ǳ�
        /// </summary>
        public void Logout()
        {
            string strError = "";
            long nRet = this.Channel.Logout(out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }

        }

        private void toolStripButton_prev_Click(object sender, EventArgs e)
        {
            // TODO: ���ԸĽ�Ϊ����Safe...�������Ͳ�������Disable��ť����ֹ������
            this.LoadRecordOld(this.BiblioRecPath, "prev", true);
        }

        private void toolStripButton_next_Click(object sender, EventArgs e)
        {
            this.LoadRecordOld(this.BiblioRecPath, "next", true);
        }

        string m_strFocusedPart = "";

        private void EntityForm_Leave(object sender, EventArgs e)
        {
        }

        private void EntityForm_Enter(object sender, EventArgs e)
        {
            /*
            // 2008/11/26 new add
            if (m_strFocusedPart == "marceditor")
            {
                if (this.MarcEditor.FocusedFieldIndex == -1)
                    this.MarcEditor.FocusedFieldIndex = 0;

                this.MarcEditor.Focus();
            }
            else if (m_strFocusedPart == "itembarcode")
            {
                this.textBox_itemBarcode.Focus();
            }
            */
        }

        private void MarcEditor_Enter(object sender, EventArgs e)
        {
            m_strFocusedPart = "marceditor";

            // API.PostMessage(this.Handle, WM_FILL_MARCEDITOR_SCRIPT_MENU, 0, 0);
            if (this.MainForm.PanelFixedVisible == true)
                this.AutoGenerate(this.m_marcEditor,
                    new GenerateDataEventArgs(),
                    true);

            Debug.WriteLine("MarcEditor Enter");
        }

        private void MarcEditor_Leave(object sender, EventArgs e)
        {
            Debug.WriteLine("MarcEditor Leave");
        }

        private void tabPage_marc_Enter(object sender, EventArgs e)
        {
            /*
            SwitchFocus(MARC_EDITOR);
             * */
        }

        private void MarcEditor_ControlLetterKeyPress(object sender, ControlLetterKeyPressEventArgs e)
        {
            if (e.KeyData == Keys.T)
            {
                e.Handled = true;
                this.LoadBiblioTemplate(true);
                return;
            }
            if (e.KeyData == Keys.D)
            {
                e.Handled = true;
                this.ToolStripMenuItem_searchDupInExistWindow_Click(this, e);
                return;
            }

        }

        private void EntityForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;

        }

        private void EntityForm_DragDrop(object sender, DragEventArgs e)
        {
            string strError = "";

            string strWhole = (String)e.Data.GetData("Text");

            string[] lines = strWhole.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 1)
            {
                strError = "��һ��Ҳ������";
                goto ERROR1;
            }

            if (lines.Length > 1)
            {
                strError = "�ֲᴰֻ��������һ����¼";
                goto ERROR1;
            }

            string strFirstLine = lines[0].Trim();

            // ȡ��recpath
            string strRecPath = "";
            int nRet = strFirstLine.IndexOf("\t");
            if (nRet == -1)
                strRecPath = strFirstLine;
            else
                strRecPath = strFirstLine.Substring(0, nRet).Trim();

            // �ж�������Ŀ��¼·��������ʵ���¼·����
            string strDbName = Global.GetDbName(strRecPath);

            if (this.MainForm.IsBiblioDbName(strDbName) == true)
            {
                this.LoadRecordOld(strRecPath,
                    "",
                    true);
            }
            else if (this.MainForm.IsItemDbName(strDbName) == true)
            {
                this.LoadItemByRecPath(strRecPath,
                    this.checkBox_autoSavePrev.Checked);
            }
            else
            {
                strError = "��¼·�� '" + strRecPath + "' �е����ݿ����Ȳ�����Ŀ������Ҳ����ʵ�����...";
                goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }






        #region �� ��ع���









        #endregion

        /// <summary>
        /// ��ö�����Դ��Ϣ
        /// </summary>
        /// <param name="sender">������</param>
        /// <param name="e">�¼�����</param>
        public void GetResInfo(object sender, GetResInfoEventArgs e)
        {
            List<ResInfo> resinfos = new List<ResInfo>();
            for (int i = 0; i < this.binaryResControl1.ListView.Items.Count; i++)
            {
                ListViewItem item = this.binaryResControl1.ListView.Items[i];

                string strID = ListViewUtil.GetItemText(item, BinaryResControl.COLUMN_ID);
                if (String.IsNullOrEmpty(e.ID) == true
                    || strID == e.ID)
                {
                    ResInfo resinfo = new ResInfo();
                    resinfo.ID = strID;
                    resinfo.LocalPath = ListViewUtil.GetItemText(item, BinaryResControl.COLUMN_LOCALPATH);
                    resinfo.Mime = ListViewUtil.GetItemText(item, BinaryResControl.COLUMN_MIME);
                    try
                    {
                        resinfo.Size = Convert.ToInt64(ListViewUtil.GetItemText(item, BinaryResControl.COLUMN_SIZE));
                    }
                    catch
                    {
                    }

                    resinfos.Add(resinfo);
                }

            }

            e.Results = resinfos;
        }

        // ����Ŀ���¼
        // �ձ�ʾ���
        private void toolStripButton_setTargetRecord_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strTargetBiblioRecPath = this.m_marcEditor.Record.Fields.GetFirstSubfield("998", "t");

        REDO:
            strTargetBiblioRecPath = InputDlg.GetInput(
            this,
            "��ָ��Ŀ���¼·��",
            "Ŀ���¼·��(��ʽ'��Ŀ����/ID'): \r\n\r\n[ע���������Ϊ�գ���ʾ���Ŀ���¼·��]",
            strTargetBiblioRecPath,
            this.MainForm.DefaultFont);
            if (strTargetBiblioRecPath == null)
                return;

            if (strTargetBiblioRecPath == "")
                goto SET;

            // parameters:
            //      strSourceRecPath    ��¼ID����Ϊ�ʺ�
            //      strTargetRecPath    ��¼ID����Ϊ�ʺţ���bCheckTargetWenhao==false
            // return:
            //      -1  ����
            //      0   ���ʺϽ���Ŀ���ϵ (���������û��ʲô�����ǲ��ʺϽ���)
            //      1   �ʺϽ���Ŀ���ϵ
            int nRet = this.MainForm.CheckBuildLinkCondition(this.BiblioRecPath,
                    strTargetBiblioRecPath,
                    true,
                    out strError);
            if (nRet == -1 || nRet == 0)
            {
                MessageBox.Show(this, strError);
                goto REDO;
            }

            /*

            TODO: ����ͳһ�������

            // TODO: ��ü��һ�����·���ĸ�ʽ���Ϸ�����Ŀ����������MainForm���ҵ�

            // ����ǲ�����Ŀ������MARC��ʽ�Ƿ�͵�ǰ���ݿ�һ�¡������ǵ�ǰ��¼�Լ�
            string strDbName = Global.GetDbName(strTargetBiblioRecPath);
            string strRecordID = Global.GetRecordID(strTargetBiblioRecPath);

            if (String.IsNullOrEmpty(strDbName) == true
                || String.IsNullOrEmpty(strRecordID) == true)
            {
                strError = "'"+strTargetBiblioRecPath+"' ���ǺϷ��ļ�¼·��";
                goto ERROR1;
            }

            // ������Ŀ�������MARC��ʽ�﷨��
            // return:
            //      null    û���ҵ�ָ������Ŀ����
            string strCurrentSyntax = this.MainForm.GetBiblioSyntax(this.BiblioDbName);
            if (String.IsNullOrEmpty(strCurrentSyntax) == true)
                strCurrentSyntax = "unimarc";
            string strCurrentIssueDbName = MainForm.GetIssueDbName(this.BiblioDbName);


            bool bFound = false;
            for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
            {
                BiblioDbProperty prop = this.MainForm.BiblioDbProperties[i];

                if (prop.DbName == strDbName)
                {
                    bFound = true;

                    string strTempSyntax = prop.Syntax;
                    if (String.IsNullOrEmpty(strTempSyntax) == true)
                        strTempSyntax = "unimarc";

                    if (strTempSyntax != strCurrentSyntax)
                    {
                        strError = "�����õ�Ŀ���¼������Ŀ���ݸ�ʽΪ '"+strTempSyntax+"'���뵱ǰ��¼����Ŀ���ݸ�ʽ '"+strCurrentSyntax+"' ��һ�£���˲������ܾ�";
                        goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(prop.IssueDbName)
                        != String.IsNullOrEmpty(strCurrentIssueDbName))
                    {
                        strError = "�����õ�Ŀ���¼������Ŀ�� '"+strDbName+"' ��������(�ڿ�����ͼ��)�͵�ǰ��¼����Ŀ�� '"+this.BiblioDbName+"' ��һ�£���˲������ܾ�";
                        goto ERROR1;
                    }
                }
            }

            if (bFound == false)
            {
                strError = "'"+strDbName+"' ���ǺϷ�����Ŀ����";
                goto ERROR1;
            }

            if (strRecordID == "?")
            {
                strError = "��¼ID����Ϊ�ʺ�";
                goto ERROR1;
            }

            if (Global.IsPureNumber(strRecordID) == false)
            {
                strError = "��¼ID���ֱ���Ϊ������";
                goto ERROR1;
            }

            if (strDbName == this.BiblioDbName)
            {
                strError = "Ŀ���¼�͵�ǰ��¼��������ͬһ����Ŀ��";
                goto ERROR1;
                // ע�������Ͳ��ü��Ŀ���Ƿ񱾼�¼��
            }
            */

            bool bReplaceMarc = true;
            // ���棺��ǰ��¼�ᱻĿ���¼��ȫ���
            DialogResult result = MessageBox.Show(this,
                "��ǰMARC�༭���ڵ����ݽ�������Ŀ���¼��������ȫȡ����\r\n\r\nȷʵҪȡ��? \r\n\r\n��(Yes): ȡ����\r\n��(No): ��ȡ�������Ǽ�������Ŀ���¼·���Ĳ�����\r\nȡ��(Cancel): ��ȡ�������ҷ�������Ŀ���¼·���Ĳ���",
                "EntityForm",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == DialogResult.Cancel)
                return;
            if (result == DialogResult.Yes)
                bReplaceMarc = true;
            else
                bReplaceMarc = false;

            if (bReplaceMarc == true)
            {
                // ���浱ǰ��¼��998�ֶ�
                Field old_998 = this.m_marcEditor.Record.Fields.GetOneField("998", 0);

                // װ��Ŀ����Ŀ��¼
                // ���������޸�this.BiblioRecPath����Ϊ���ǵ�ǰ��¼��·��
                // parameters:
                // return:
                //      -1  error
                //      0   not found, strError���г�����Ϣ
                //      1   found
                nRet = LoadTargetBiblioRecord(strTargetBiblioRecPath,
                    out strError);
                if (nRet == 0 || nRet == -1)
                    goto ERROR1;

                // �ָ���ǰ��998�ֶ�����
                for (int i = 0; i < this.m_marcEditor.Record.Fields.Count; i++)
                {
                    Field temp = this.m_marcEditor.Record.Fields[i];
                    if (temp.Name == "998")
                    {
                        this.m_marcEditor.Record.Fields.RemoveAt(i);
                        i--;
                    }
                }
                if (old_998 != null)
                {
                    this.m_marcEditor.Record.Fields.Insert(this.m_marcEditor.Record.Fields.Count,
                        old_998.Name,
                        old_998.Indicator,
                        old_998.Value);
                }
            }

        SET:

            if (String.IsNullOrEmpty(strTargetBiblioRecPath) == false)
                this.m_marcEditor.Record.Fields.SetFirstSubfield("998", "t", strTargetBiblioRecPath);
            else
            {
                this.Remove998t();
            }

        if (this.LinkedRecordReadonly == true)
        {
            if (String.IsNullOrEmpty(strTargetBiblioRecPath) == true)
                this.m_marcEditor.ReadOnly = false;
            else
                this.m_marcEditor.ReadOnly = true;
        }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // return:
        //      shifou fashengle gaibian
        bool Remove998t()
        {
            Field field_998 = this.m_marcEditor.Record.Fields.GetOneField("998", 0);
            if (field_998 == null)
                return false;
            SubfieldCollection subfields = field_998.Subfields;

            bool bChanged = false;
            while (true)
            {
                Subfield subfield = subfields["t"];
                if (subfield != null)
                {
                    subfields.Remove(subfield);
                    bChanged = true;
                }
                else
                    break;
            }

            if (bChanged == true)
                field_998.Subfields = subfields;

            return bChanged;
        }

        // װ��Ŀ����Ŀ��¼
        // ���������޸�this.BiblioRecPath����Ϊ���ǵ�ǰ��¼��·��
        // parameters:
        // return:
        //      -1  error
        //      0   not found, strError���г�����Ϣ
        //      1   found
        int LoadTargetBiblioRecord(string strTargetBiblioRecPath,
            out string strError)
        {
            strError = "";
            string strXml = "";
            string strOutputTargetBiblioRecPath = "";

            if (String.IsNullOrEmpty(strTargetBiblioRecPath) == true)
            {
                strError = "strTargetBiblioRecPath����ֵ����Ϊ��";
                goto ERROR1;
            }

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("����װ��Ŀ���¼ '" + strTargetBiblioRecPath + "' ...");
            Progress.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                // Global.SetHtmlString(this.webBrowser_biblioRecord, "(�հ�)");
                this.m_webExternalHost_biblio.SetHtmlString("(�հ�)",
                    "entityform_error"); 
                
                Progress.SetMessage("����װ��Ŀ���¼ " + strTargetBiblioRecPath + " ...");

                bool bCataloging = this.Cataloging;

                string[] formats = null;

                if (bCataloging == true)
                {
                    formats = new string[3];
                    formats[0] = "outputpath";
                    formats[1] = "html";
                    formats[2] = "xml";
                }
                else
                {
                    formats = new string[2];
                    formats[0] = "outputpath";
                    formats[1] = "html";
                }

                string[] results = null;
                byte[] baTimestamp = null;

                long lRet = Channel.GetBiblioInfos(
                    Progress,
                    strTargetBiblioRecPath,
                    "",
                    formats,
                    out results,
                    out baTimestamp,
                    out strError);
                if (lRet == 0)
                {
                    strError = "·��Ϊ '" + strTargetBiblioRecPath + "' ����Ŀ��¼û���ҵ� ...";
                    return 0;   // not found
                }

                string strHtml = "";

                if (results != null && results.Length >= 1)
                    strOutputTargetBiblioRecPath = results[0];

                if (results != null && results.Length >= 2)
                    strHtml = results[1];

                if (lRet == -1)
                {
                    return -1;
                }
                else
                {
                    // û�б���ʱ��Ҫ��results�����ϸ���
                    if (results == null)
                    {
                        strError = "results == null";
                        goto ERROR1;
                    }
                    if (results.Length != formats.Length)
                    {
                        strError = "result.Length != formats.Length";
                        goto ERROR1;
                    }

                    // û�б��������²�ˢ��ʱ���
                    // this.BiblioTimestamp = baTimestamp;
                }

#if NO
                Global.SetHtmlString(this.webBrowser_biblioRecord,
                    strHtml,
                    this.MainForm.DataDir,
                    "entityform_biblio");
#endif
                this.m_webExternalHost_biblio.SetHtmlString(strHtml,
                    "entityform_biblio");

                if (bCataloging == true)
                {
                    if (results != null && results.Length >= 3)
                        strXml = results[2];

                    {
                        // return:
                        //      -1  error
                        //      0   �յļ�¼
                        //      1   �ɹ�
                        int nRet = SetBiblioRecordToMarcEditor(strXml,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        // 2008/11/13 new add
                        if (nRet == 0)
                            MessageBox.Show(this, "���棺Ŀ���¼ '" + strOutputTargetBiblioRecPath + "' ��һ���ռ�¼");

                        this.BiblioChanged = true;
                    }
                }
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

        // װ��Ŀ���¼
        private void ToolStripMenuItem_loadTargetBiblioRecord_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strTargetBiblioRecPath = this.m_marcEditor.Record.Fields.GetFirstSubfield("998", "t");

            if (String.IsNullOrEmpty(strTargetBiblioRecPath) == true)
            {
                strError = "��ǰ��¼���߱�Ŀ���¼";
                goto ERROR1;
            }

            // return:
            //      -1  �����Ѿ���MessageBox����
            //      0   û��װ��(���緢�ִ����ڵļ�¼û�б��棬���־���Ի���󣬲�����ѡ����Cancel)
            //      1   �ɹ�װ��
            //      2   ͨ����ռ��
            LoadRecordOld(strTargetBiblioRecPath,
                "",
                true);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /*public*/ void AddToPendingList(string strBiblioRecPath,
            string strPrevNextStyle)
        {
            PendingLoadRequest request = new PendingLoadRequest();
            request.RecPath = strBiblioRecPath;
            request.PrevNextStyle = strPrevNextStyle;
            lock (this.m_listPendingLoadRequest)
            {
                this.m_listPendingLoadRequest.Clear();
                this.m_listPendingLoadRequest.Add(request);
            }
            this.timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lock (this.m_listPendingLoadRequest)
            {
                if (this.m_listPendingLoadRequest.Count == 0)
                {
                    this.timer1.Stop();
                    return;
                }
            }
            string strError = "";
            PendingLoadRequest request = null;
            lock (this.m_listPendingLoadRequest)
            {
                request = this.m_listPendingLoadRequest[0];
            }
            int nRet = this.LoadRecord(request.RecPath,
                request.PrevNextStyle,
                true,
                false,
                out strError);
            if (nRet == 2)
            {
            }
            else
            {
                lock (this.m_listPendingLoadRequest)
                {
                    this.m_listPendingLoadRequest.Remove(request);
                }
            }

        }

        // ����������Ϣ��XML�ļ�
        private void ToolStripMenuItem_exportAllInfoToXmlFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ�����XML�ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = true;
            dlg.FileName = "";
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "XML�ļ� (*.xml)|*.xml|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            // �����Ŀ��¼XML��ʽ
            string strXmlBody = "";
            int nRet = this.GetBiblioXml(
                "", // ��ʹ�Ӽ�¼·���п�marc��ʽ
                true,
                out strXmlBody,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            XmlDocument domBiblio = new XmlDocument();
            try
            {
                domBiblio.LoadXml(strXmlBody);
            }
            catch (Exception ex)
            {
                strError = "biblio xml load to dom error: " + ex.Message;
                goto ERROR1;
            }

            XmlNode nodeBiblio = dom.CreateElement("dprms", "biblio", DpNs.dprms);
            dom.DocumentElement.AppendChild(nodeBiblio);
            nodeBiblio.InnerXml = domBiblio.DocumentElement.OuterXml;   // <unimarc:record>����<usmarc:record>��Ϊ<dprms:biblio>���¼�

            // ��
            string strItemXml = "";
            nRet = this.entityControl1.Items.BuildXml(
                out strItemXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            XmlDocument domItems = new XmlDocument();
            try
            {
                domItems.LoadXml(strItemXml);
            }
            catch (Exception ex)
            {
                strError = "items xml load to dom error: " + ex.Message;
                goto ERROR1;
            }

            XmlNode nodeItems = dom.CreateElement("dprms", "itemCollection", DpNs.dprms);
            dom.DocumentElement.AppendChild(nodeItems);
            nodeItems.InnerXml = domItems.DocumentElement.InnerXml;

            // ��
            // TODO: �Ƿ�Ҫ���ݳ��������ͣ������Ƿ񴴽�<dprms:issues>Ԫ��
            string strIssueXml = "";
            nRet = this.issueControl1.Items.BuildXml(
                out strIssueXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            XmlDocument domIssues = new XmlDocument();
            try
            {
                domIssues.LoadXml(strIssueXml);
            }
            catch (Exception ex)
            {
                strError = "issues xml load to dom error: " + ex.Message;
                goto ERROR1;
            }

            XmlNode nodeIssues = dom.CreateElement("dprms", "issueCollection", DpNs.dprms);
            dom.DocumentElement.AppendChild(nodeIssues);
            nodeIssues.InnerXml = domIssues.DocumentElement.InnerXml;

            // ����
            string strOrderXml = "";
            nRet = this.orderControl1.Items.BuildXml(
                out strOrderXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            XmlDocument domOrders = new XmlDocument();
            try
            {
                domOrders.LoadXml(strOrderXml);
            }
            catch (Exception ex)
            {
                strError = "orders xml load to dom error: " + ex.Message;
                goto ERROR1;
            }

            XmlNode nodeOrders = dom.CreateElement("dprms", "orderCollection", DpNs.dprms);
            dom.DocumentElement.AppendChild(nodeOrders);
            nodeOrders.InnerXml = domOrders.DocumentElement.InnerXml;

            // ��ע
            string strCommentXml = "";
            nRet = this.commentControl1.Items.BuildXml(
                out strCommentXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            XmlDocument domComments = new XmlDocument();
            try
            {
                domComments.LoadXml(strCommentXml);
            }
            catch (Exception ex)
            {
                strError = "comments xml load to dom error: " + ex.Message;
                goto ERROR1;
            }

            XmlNode nodeComments = dom.CreateElement("dprms", "commentCollection", DpNs.dprms);
            dom.DocumentElement.AppendChild(nodeComments);
            nodeComments.InnerXml = domComments.DocumentElement.InnerXml;

            try
            {
                dom.Save(dlg.FileName);
            }
            catch (Exception ex)
            {
                strError = "����XML�ļ� '"+dlg.FileName+"' ʱ����: " + ex.Message;
                goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��XML�ļ��е���ȫ����Ϣ
        private void StripMenuItem_importFromXmlFile_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            bool bRefreshRefID = true;
            if (Control.ModifierKeys == Keys.Control)
                bRefreshRefID = false;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "��ָ��Ҫ�򿪵�XML�ļ���";
            dlg.FileName = "";
            // dlg.InitialDirectory = 
            dlg.Filter = "XML�ļ� (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.Load(dlg.FileName);
            }
            catch (Exception ex)
            {
                strError = "XML�ļ� "+dlg.FileName+" װ��ʧ��: " + ex.Message;
                goto ERROR1;
            }

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);

            // ��Ŀ
            XmlNode node = dom.DocumentElement.SelectSingleNode("dprms:biblio", nsmgr);
            if (node != null)
            {
                // return:
                //      -1  error
                //      0   �յļ�¼
                //      1   �ɹ�
                nRet = SetBiblioRecordToMarcEditor(node.OuterXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                Global.ClearHtmlPage(this.webBrowser_biblioRecord, this.MainForm.DataDir);
            }

            // ��
            Hashtable item_refid_change_table = null;
            node = dom.DocumentElement.SelectSingleNode("dprms:itemCollection", nsmgr);
            this.entityControl1.ClearItems();
            if (node != null)
            {
                nRet = this.entityControl1.Items.ImportFromXml(node,
                    this.entityControl1.ListView,
                    bRefreshRefID,
                    out item_refid_change_table,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                this.entityControl1.BiblioRecPath = this.BiblioRecPath;
            }

            Hashtable order_refid_change_table = new Hashtable();

            // ����
            node = dom.DocumentElement.SelectSingleNode("dprms:orderCollection", nsmgr);
            this.orderControl1.ClearItems();
            if (node != null)
            {
                // parameters:
                //       changed_refids  �ۼ��޸Ĺ��� refid ���ձ� ԭ���� --> �µ�
                nRet = this.orderControl1.Items.ImportFromXml(node,
                    this.orderControl1.ListView,
                    bRefreshRefID,
                    ref order_refid_change_table,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                this.orderControl1.BiblioRecPath = this.BiblioRecPath;
            }

            // ��
            node = dom.DocumentElement.SelectSingleNode("dprms:issueCollection", nsmgr);
            this.issueControl1.ClearItems();
            if (node != null)
            {
                nRet = this.issueControl1.Items.ImportFromXml(node,
                    this.issueControl1.ListView,
                    order_refid_change_table,
                    bRefreshRefID,
                    item_refid_change_table,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                this.issueControl1.BiblioRecPath = this.BiblioRecPath;
            }

            // ��ע
            node = dom.DocumentElement.SelectSingleNode("dprms:commentCollection", nsmgr);
            this.commentControl1.ClearItems();
            if (node != null)
            {
                nRet = this.commentControl1.Items.ImportFromXml(node,
                    this.commentControl1.ListView,
                    bRefreshRefID,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                this.commentControl1.BiblioRecPath = this.BiblioRecPath;
            }

            SetSaveAllButtonState(true);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void ToolStripMenuItem_viewMarcJidaoData_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            TestJidaoForm dlg = new TestJidaoForm();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MARC = this.GetMarc();  // this.m_marcEditor.Marc;
            this.MainForm.AppInfo.LinkFormState(dlg, "entityform_testJidaoForm_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            int nCount = dlg.Xmls.Count;

            if (nCount == 0)
            {
                strError = "û�д����κ��ڼ�¼";
                goto ERROR1;
            }

            List<string> xmls = dlg.Xmls;
            // �Ƴ�publishtime�ظ�������
            // return:
            //      -1  ����
            //      0   û���Ƴ���
            //      >0  �Ƴ��ĸ���
            nRet = this.issueControl1.RemoveDupPublishTime(ref xmls,    // 2013/9/21 �޸Ĺ�
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == nCount)
            {
                Debug.Assert(dlg.Xmls.Count == 0, "");
                strError = "�����󴴽��� " + nCount + " ���ڼ�¼��ǰȫ���Ѿ����ڡ�����������";
                goto ERROR1;
            }

            dlg.Xmls = xmls;

            if (nRet > 0)
            {
                DialogResult result = MessageBox.Show(this,
    "�����󴴽��� "+nCount+" ���ڼ�¼���������Ѿ����ڣ�\r\n" + strError + "\r\n\r\n��Щ�ظ����ڲ��ܼ����ڼ�¼�б�\r\n\r\n�����Ƿ������������ "
    +dlg.Xmls.Count.ToString()+" ���ڼ�¼? ",
    "EntityForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return;
            }


            // ������XML���ݣ����������޸��ڶ���
            // TODO: ѭ���г���ʱ��Ҫ��������ȥ������ٱ���
            // return:
            //      -1  error
            //      0   succeed
            nRet = this.issueControl1.ChangeIssues(dlg.Xmls,
            out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void toolStripButton_option_Click(object sender, EventArgs e)
        {
            EntityFormOptionDlg dlg = new EntityFormOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.MainForm = this.MainForm;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);
        }

        private void toolStripButton_clear_Click(object sender, EventArgs e)
        {
            Clear(true);
        }

        private void toolStripButton_saveAll_Click(object sender, EventArgs e)
        {
            button_save_Click(sender, e);
        }

        /// <summary>
        /// ����Ի����
        /// </summary>
        /// <param name="keyData">System.Windows.Forms.Keys ֵ֮һ������ʾҪ����ļ���</param>
        /// <returns>����ؼ�����ʹ�û�������Ϊ true������Ϊ false���������һ������</returns>
        protected override bool ProcessDialogKey(
Keys keyData)
        {
            /*
            if (keyData == Keys.Enter)
            {
                this.button_OK_Click(this, null);
                return true;
            }*/

            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }

            if (keyData == Keys.F2)
            {
                this.DoSaveAll();
                return true;
            }

            if (keyData == Keys.F3)
            {
                this.toolStripButton1_marcEditor_saveTo_Click(this, null);
                return true;
            }

            if (keyData == Keys.F4)
            {
                this.toolStrip_marcEditor.Enabled = false;
                try
                {
                    LoadBiblioTemplate(true);
                }
                finally
                {
                    this.toolStrip_marcEditor.Enabled = true;
                }
                return true;
            }

            if (keyData == Keys.F5)
            {
                this.Reload();
                return true;
            }

            // return false;
            return base.ProcessDialogKey(keyData);
        }

        string m_strVerifyResult = "";

        void DoViewVerifyResult(bool bOpenWindow)
        {
            // string strError = "";

            // �Ż���������ν�ؽ��з���������
            if (bOpenWindow == false)
            {
                if (this.MainForm.PanelFixedVisible == false
                    && (m_verifyViewer == null || m_verifyViewer.Visible == false))
                    return;
            }


            if (this.m_verifyViewer == null
                || (bOpenWindow == true && this.m_verifyViewer.Visible == false))
            {
                m_verifyViewer = new VerifyViewerForm();
                MainForm.SetControlFont(m_verifyViewer, this.Font, false);

                // m_viewer.MainForm = this.MainForm;  // �����ǵ�һ��
                m_verifyViewer.Text = "У����";
                m_verifyViewer.ResultString = this.m_strVerifyResult;

                m_verifyViewer.DoDockEvent -= new DoDockEventHandler(m_viewer_DoDockEvent);
                m_verifyViewer.DoDockEvent += new DoDockEventHandler(m_viewer_DoDockEvent);

                m_verifyViewer.FormClosed -= new FormClosedEventHandler(m_viewer_FormClosed);
                m_verifyViewer.FormClosed += new FormClosedEventHandler(m_viewer_FormClosed);

                m_verifyViewer.Locate -= new LocateEventHandler(m_viewer_Locate);
                m_verifyViewer.Locate += new LocateEventHandler(m_viewer_Locate);

            }

            if (bOpenWindow == true)
            {
                if (m_verifyViewer.Visible == false)
                {
                    this.MainForm.AppInfo.LinkFormState(m_verifyViewer, "verify_viewer_state");
                    m_verifyViewer.Show(this);
                    m_verifyViewer.Activate();

                    this.MainForm.CurrentVerifyResultControl = null;
                }
                else
                {
                    if (m_verifyViewer.WindowState == FormWindowState.Minimized)
                        m_verifyViewer.WindowState = FormWindowState.Normal;
                    m_verifyViewer.Activate();
                }
            }
            else
            {
                if (m_verifyViewer.Visible == true)
                {

                }
                else
                {
                    if (this.MainForm.CurrentVerifyResultControl != m_verifyViewer.ResultControl)
                        m_verifyViewer.DoDock(false); // �����Զ���ʾFixedPanel
                }
            }
            return;
            /*
        ERROR1:
            MessageBox.Show(this, "DoViewVerifyResult() ����: " + strError);
             * */
        }

        void m_viewer_DoDockEvent(object sender, DoDockEventArgs e)
        {
            if (this.MainForm.CurrentVerifyResultControl != m_verifyViewer.ResultControl)
                this.MainForm.CurrentVerifyResultControl = m_verifyViewer.ResultControl;

            if (e.ShowFixedPanel == true
                && this.MainForm.PanelFixedVisible == false)
                this.MainForm.PanelFixedVisible = true;

            m_verifyViewer.Docked = true;
            m_verifyViewer.Visible = false;
        }

        void m_viewer_Locate(object sender, LocateEventArgs e)
        {
            string strError = "";

            string[] parts = e.Location.Split(new char[] { ',' });
            string strFieldName = "";
            int nFieldIndex = 0;
            string strSubfieldName = "";
            int nSubfieldIndex = 0;

            int nCharPos = 0;
            int nRet = 0;

            if (parts.Length == 0)
                return;
            if (parts.Length >= 1)
            {
                string strValue = parts[0].Trim();
                nRet = strValue.IndexOf("#");
                if (nRet == -1)
                    strFieldName = strValue;
                else
                {
                    strFieldName = strValue.Substring(0, nRet);
                    string strNumber = strValue.Substring(nRet + 1);
                    if (string.IsNullOrEmpty(strNumber) == false)
                    {
                        try
                        {
                            nFieldIndex = Convert.ToInt32(strNumber);
                        }
                        catch
                        {
                            strError = "�ֶ�λ�� '" + strNumber + "' ��ʽ����ȷ...";
                            goto ERROR1;
                        }
                        nFieldIndex--;
                    }
                }
            }

            if (parts.Length >= 2)
            {
                string strValue = parts[1].Trim();
                nRet = strValue.IndexOf("#");
                if (nRet == -1)
                    strSubfieldName = strValue;
                else
                {
                    strSubfieldName = strValue.Substring(0, nRet);
                    string strNumber = strValue.Substring(nRet + 1);
                    if (string.IsNullOrEmpty(strNumber) == false)
                    {
                        try
                        {
                            nSubfieldIndex = Convert.ToInt32(strNumber);
                        }
                        catch
                        {
                            strError = "���ֶ�λ�� '" + strNumber + "' ��ʽ����ȷ...";
                            goto ERROR1;
                        }
                        nSubfieldIndex--;
                    }
                }
            }


            if (parts.Length >= 3)
            {
                string strValue = parts[2].Trim();
                if (string.IsNullOrEmpty(strValue) == false)
                {
                    try
                    {
                        nCharPos = Convert.ToInt32(strValue);
                    }
                    catch
                    {
                        strError = "�ַ�λ�� '" + strValue + "' ��ʽ����ȷ...";
                        goto ERROR1;
                    }
                    if (nCharPos > 0)
                        nCharPos--;
                }
            }

            Field field = this.m_marcEditor.Record.Fields[strFieldName, nFieldIndex];
            if (field == null)
            {
                strError = "��ǰMARC�༭���в����� ��Ϊ '"+strFieldName+"' λ��Ϊ "+nFieldIndex.ToString()+" ���ֶ�";
                goto ERROR1;
            }

            if (string.IsNullOrEmpty(strSubfieldName) == true)
            {
                // �ֶ���
                if (nCharPos == -1)
                {
                    this.m_marcEditor.SetActiveField(field, 2);
                }
                // �ֶ�ָʾ��
                else if (nCharPos == -2)
                {
                    this.m_marcEditor.SetActiveField(field, 1);
                }
                else
                {
                    this.m_marcEditor.FocusedField = field;
                    this.m_marcEditor.SelectCurEdit(nCharPos, 0);
                }
                this.m_marcEditor.EnsureVisible();
                return;
            }

            this.m_marcEditor.FocusedField = field;
            this.m_marcEditor.EnsureVisible();
            
            Subfield subfield = field.Subfields[strSubfieldName, nSubfieldIndex];
            if (subfield == null)
            {
                strError = "��ǰMARC�༭���в����� ��Ϊ '" + strSubfieldName + "' λ��Ϊ " + nSubfieldIndex.ToString() + " �����ֶ�";
                goto ERROR1;
            }

            this.m_marcEditor.SelectCurEdit(subfield.Offset + 2, 0);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void m_viewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_verifyViewer != null)
            {
                this.MainForm.AppInfo.UnlinkFormState(m_verifyViewer);
                this.m_verifyViewer = null;
            }
        }

        private void toolStripButton_verifyData_Click(object sender, EventArgs e)
        {
            GenerateDataEventArgs e1 = new GenerateDataEventArgs();
            e1.FocusedControl = this.m_marcEditor;
            this.VerifyData(this, e1, false);
        }

        /// <summary>
        /// ��������Ƿ�ɼ�
        /// </summary>
        public bool QueryPanelVisibie
        {
            get
            {
                return this.flowLayoutPanel_query.Visible;
            }
            set
            {
                this.flowLayoutPanel_query.Visible = value;
                this.MainForm.AppInfo.SetBoolean(
"entityform",
"queryPanel_visibie",
value);
            }
        }

        private void toolStripButton_hideSearchPanel_Click(object sender, EventArgs e)
        {
            this.QueryPanelVisibie = false;
        }

        private void toolStripButton_hideItemQuickImput_Click(object sender, EventArgs e)
        {
            this.ItemQuickInputPanelVisibie = false;
        }

        /// <summary>
        /// �����������岿���Ƿ�ɼ�
        /// </summary>
        public bool ItemQuickInputPanelVisibie
        {
            get
            {
                return this.panel_itemQuickInput.Visible;
            }
            set
            {
                this.panel_itemQuickInput.Visible = value;
                this.MainForm.AppInfo.SetBoolean(
"entityform",
"itemQuickInputPanel_visibie",
value);
            }
        }

        private void entityControl1_Enter(object sender, EventArgs e)
        {
            // ��ʾCtrl+A�˵�
            if (this.MainForm.PanelFixedVisible == true)
            {
                GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                e1.FocusedControl = this.entityControl1.ListView;
                e1.ScriptEntry = "";    // ����Ctrl+A�˵�
                this.AutoGenerate(this.entityControl1, e1,
                    true);
            }
        }

        private void entityControl1_Leave(object sender, EventArgs e)
        {
            /*
            // ����Ctrl+A�˵�
            if (this.MainForm.PanelFixedVisible == true)
            {
                if (this.m_genDataViewer != null)
                    this.m_genDataViewer.Clear();
            }
             * */
        }

        private void MarcEditor_SelectedFieldChanged(object sender, EventArgs e)
        {
            if (this.m_genDataViewer != null)
                this.m_genDataViewer.RefreshState();
        }

        private void binaryResControl1_Enter(object sender, EventArgs e)
        {
            // ��ʾCtrl+A�˵�
            if (this.MainForm.PanelFixedVisible == true)
            {
                GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                e1.FocusedControl = this.binaryResControl1.ListView;
                e1.ScriptEntry = "";    // ����Ctrl+A�˵�
                this.AutoGenerate(this.binaryResControl1, e1,
                    true);
            }
        }

        private void MarcEditor_GetTemplateDef(object sender, GetTemplateDefEventArgs e)
        {
            if (this.m_detailHostObj == null)
            {
                int nRet = 0;
                string strError = "";

                // ��ʼ�� dp2circulation_marc_autogen.cs �� Assembly����new DetailHost����
                // return:
                //      -1  error
                //      0   û�����³�ʼ��Assembly������ֱ������ǰCache��Assembly
                //      1   ����(�����״�)��ʼ����Assembly
                nRet = InitialAutogenAssembly(null,
                    out strError);
                if (nRet == -1)
                {
                    e.ErrorInfo = strError;
                    return;
                }
                if (nRet == 0)
                {
                    if (this.m_detailHostObj == null)
                    {
                        e.Canceled = true;
                        return; // �������߱����޷���ʼ��
                    }
                }
                Debug.Assert(this.m_detailHostObj != null, "");
            }

            Debug.Assert(this.m_detailHostObj != null, "");

            // ����ű�����û����Ӧ�Ļص�����
            if (this.m_detailHostObj.GetType().GetMethod("GetTemplateDef",
                BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.InvokeMethod
                ) == null)
            {
                e.Canceled = true;
                return;
            }

            /*
            dynamic o = this.m_detailHostObj;
            try
            {
                o.GetTemplateDef(sender, e);
            }
            catch (Exception ex)
            {
                e.ErrorInfo = ex.Message;
                return;
            }
             * */
            // �����������ĳ�Ա����
            Type classType = m_detailHostObj.GetType();
            try
            {
                classType.InvokeMember("GetTemplateDef",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod
                    ,
                    null,
                    this.m_detailHostObj,
                    new object[] { sender, e });
            }
            catch (Exception ex)
            {
                e.ErrorInfo = GetExceptionMessage(ex) + "\r\n\r\n" +  ExceptionUtil.GetDebugText(ex);  // GetExceptionMessage(ex);
                return;
            }
        }

        static string GetExceptionMessage(Exception ex)
        {
            string strResult = ex.GetType().ToString() + ":" + ex.Message;
            while (ex != null)
            {
                if (ex.InnerException != null)
                    strResult += "\r\n" + ex.InnerException.GetType().ToString() + ": " + ex.InnerException.Message;

                ex = ex.InnerException;
            }

            return strResult;
        }

        // ʹ�ܼ�¼ɾ����ġ�ȫ�����桱��ť
        private void ToolStripMenuItem_enableSaveAllButton_Click(object sender, EventArgs e)
        {
            if (this.DeletedMode == false)
            {
                MessageBox.Show(this, "�Ѿ�����ͨģʽ");
                return;
            }

            this.entityControl1.ChangeAllItemToNewState();
            this.issueControl1.ChangeAllItemToNewState();
            this.orderControl1.ChangeAllItemToNewState();
            this.commentControl1.ChangeAllItemToNewState();

            // ��MarcEditor�޸ı�Ǳ�Ϊtrue
            // this.m_marcEditor.Changed = true; // ��һ�������ʹ�ܺ���������ر�EntityForm���ڣ��Ƿ�ᾯ��(��Ŀ)���ݶ�ʧ
            this.SetMarcChanged(true);

            this.DeletedMode = false;
        }

        // �ƶ���Ŀ��¼
        private void toolStripButton_marcEditor_moveTo_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.MainForm.Version < 2.39)
            {
                strError = "��������Ҫ��� dp2library 2.39 �����ϰ汾����ʹ��";
                goto ERROR1;
            }

            string strTargetRecPath = this.m_marcEditor.Record.Fields.GetFirstSubfield("998", "t");
            if (string.IsNullOrEmpty(strTargetRecPath) == false)
            {
                DialogResult result = MessageBox.Show(this,
    "��ǰ�����ڵļ�¼ԭ���Ǵ� '" + strTargetRecPath + "' ���ƹ����ġ��Ƿ�Ҫ�ƶ���ԭ��λ�ã�\r\n\r\nYes: ��; No: �񣬼���������ͨ�ƶ�����; Cancel: �������β���",
    "EntityForm",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return;
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    // strTargetRecPath�ᷢ������
                }

                if (result == System.Windows.Forms.DialogResult.No)
                {
                    strTargetRecPath = "";
                }
            }

            // Դ��¼���� ��
            if (Global.IsAppendRecPath(this.BiblioRecPath) == true)
            {
                strError = "Դ��¼��δ�������޷�ִ���ƶ�����";
                goto ERROR1;
            }

            // string strMergeStyle = "";
            MergeStyle merge_style = MergeStyle.CombinSubrecord | MergeStyle.ReserveSourceBiblio;

            BiblioSaveToDlg dlg = new BiblioSaveToDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.Text = "�ƶ���Ŀ��¼�� ...";
            dlg.MainForm = this.MainForm;
            if (string.IsNullOrEmpty(strTargetRecPath) == false)
                dlg.RecPath = strTargetRecPath;
            else
            {
                dlg.RecPath = this.MainForm.AppInfo.GetString(
                    "entity_form",
                    "move_to_used_path",
                    this.BiblioRecPath);
                dlg.RecID = "?";
            }

            dlg.MessageText = "����ǰ�����е���Ŀ��¼ " + this.BiblioRecPath + " (��ͬ�����Ĳᡢ�ڡ�������ʵ���¼�Ͷ�����Դ)�ƶ���:";
            dlg.CopyChildRecords = true;
            dlg.EnableCopyChildRecords = false;

            dlg.BuildLink = false;

            // dlg.CurrentBiblioRecPath = this.BiblioRecPath;
            this.MainForm.AppInfo.LinkFormState(dlg, "entityform_BiblioMoveToDlg_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            if (this.BiblioRecPath == dlg.RecPath)
            {
                strError = "Ҫ�ƶ�����λ�� '" + dlg.RecPath + "' �͵�ǰ��¼������λ�� '" + this.BiblioRecPath + "' ��ͬ���ƶ��������ܾ�����ȷʵҪ���������¼����ֱ��ʹ�ñ��湦�ܡ�";
                goto ERROR1;
            }

            this.MainForm.AppInfo.SetString(
    "entity_form",
    "move_to_used_path",
    dlg.RecPath);

            // if (dlg.CopyChildRecords == true)
            {
                // �����ǰ��¼û�б��棬���ȱ���
                if (this.EntitiesChanged == true
        || this.IssuesChanged == true
        || this.BiblioChanged == true
        || this.ObjectChanged == true
        || this.OrdersChanged == true
        || this.CommentsChanged == true)
                {
                    // ������δ����
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ�������� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档�ƶ�����ǰ�����ȱ��浱ǰ��¼��\r\n\r\n����Ҫ��������ô��",
                        "EntityForm",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.OK)
                    {
                        // �ύ���б�������
                        // return:
                        //      -1  �д���ʱ���ų���Щ��Ϣ����ɹ���
                        //      0   �ɹ���
                        nRet = DoSaveAll();
                        if (nRet == -1)
                        {
                            strError = "��Ϊ��������������Ժ������ƶ�����������";
                            goto ERROR1;
                        }
                    }
                    else
                    {
                        strError = "�ƶ�����������";
                        goto ERROR1;
                    }
                }
            }

            // ����Ҫ����λ�ã���¼�Ƿ��Ѿ�����?
            if (dlg.RecID != "?")
            {
                byte[] timestamp = null;

                // ����ض�λ����Ŀ��¼�Ƿ��Ѿ�����
                // parameters:
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = DetectBiblioRecord(dlg.RecPath,
                    out timestamp,
                    out strError);
                if (nRet == 1)
                {
                    bool bOverwrite = false;
                    if (dlg.RecPath != strTargetRecPath)    // �ƶ���998$t����Ͳ�ѯ���Ƿ񸲸��ˣ�ֱ��ѡ�ù鲢��ʽ
                    {
#if NO
                        // TODO: ��ר�öԻ���ʵ��
                        // ���Ѹ��ǣ�
                        DialogResult result = MessageBox.Show(this,
                            "Ŀ����Ŀ��¼ " + dlg.RecPath + " �Ѿ����ڡ�\r\n\r\nҪ�õ�ǰ�����е���Ŀ��¼(��ͬ���ֶ�����������Ӽ�¼)���Ǵ˼�¼�����ǹ鲢���˼�¼? \r\n\r\nYes: ����; No: �鲢; Cancel: ���������ƶ�����",
                            "EntityForm",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result == DialogResult.Cancel)
                            return;
                        if (result == System.Windows.Forms.DialogResult.Yes)
                            bOverwrite = true;
                        else
                            bOverwrite = false;
#endif
                        GetMergeStyleDialog merge_dlg = new GetMergeStyleDialog();
                        MainForm.SetControlFont(merge_dlg, this.Font, false);
                        merge_dlg.SourceRecPath = this.BiblioRecPath;
                        merge_dlg.TargetRecPath = dlg.RecPath;
                        merge_dlg.MessageText = "Ŀ����Ŀ��¼ " + dlg.RecPath + " �Ѿ����ڡ�\r\n\r\n��ָ����ǰ�����е���Ŀ��¼(Դ)�ʹ�Ŀ���¼�ϲ��ķ���";

                        merge_dlg.UiState = this.MainForm.AppInfo.GetString(
        "entity_form",
        "GetMergeStyleDialog_uiState",
        "");
                        this.MainForm.AppInfo.LinkFormState(merge_dlg, "entityform_GetMergeStyleDialog_state");
                        merge_dlg.ShowDialog(this);
                        this.MainForm.AppInfo.UnlinkFormState(merge_dlg);
                        this.MainForm.AppInfo.SetString(
"entity_form",
"GetMergeStyleDialog_uiState",
merge_dlg.UiState);

                        if (merge_dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                            return;

                        merge_style = merge_dlg.GetMergeStyle();
                    }

                    // this.BiblioTimestamp = timestamp;   // Ϊ��˳������

                    // TODO: Ԥ�ȼ�������Ȩ�ޣ�ȷ��ɾ����Ŀ��¼���¼���¼���ܳɹ�������;���

#if NO
                    if (bOverwrite == true)
                    {
                        // ɾ��Ŀ��λ�õ���Ŀ��¼
                        // ���Ϊ�鲢ģʽ��������������ʵ��ȼ�¼
                        nRet = DeleteBiblioRecordFromDatabase(dlg.RecPath,
                            bOverwrite == true ? "delete" : "onlydeletebiblio",
                            timestamp,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
#endif
                    if ((merge_style & MergeStyle.OverwriteSubrecord)!= 0)
                    {
                        // ɾ��Ŀ���¼����������ɾ��Ŀ��λ�õ��¼���¼
                        // TODO: ���Ե�ʱ��ע�ⲻ���������ö����Ա���Ŀ����Ŀ��¼�ж���Ŀ�����
                        nRet = DeleteBiblioRecordFromDatabase(dlg.RecPath,
                            (merge_style & MergeStyle.ReserveSourceBiblio)!= 0 ? "delete" : "onlydeletesubrecord",
                            timestamp,
                            out strError);
                        if (nRet == -1)
                        {
                            if ((merge_style & MergeStyle.ReserveSourceBiblio) != 0)
                                strError = "ɾ��Ŀ��λ�õ���Ŀ��¼ '"+dlg.RecPath+"' ʱ����: " + strError;
                            else
                                strError = "ɾ��Ŀ��λ�õ���Ŀ��¼ '" + dlg.RecPath + "' ��ȫ���Ӽ�¼ʱ����: " + strError;
                            goto ERROR1;
                        }
                    }
                }
            }

            string strOutputBiblioRecPath = "";
            byte[] baOutputTimestamp = null;
            string strXml = "";

            string strOldBiblioRecPath = this.BiblioRecPath;
            string strOldMarc = this.GetMarc(); //  this.m_marcEditor.Marc;
            bool bOldChanged = this.GetMarcChanged();   // this.m_marcEditor.Changed;

            try
            {
                // ����ԭ���ļ�¼·��
                bool bOldReadOnly = this.m_marcEditor.ReadOnly;
                Field old_998 = null;

                string strDlgTargetDbName = Global.GetDbName(dlg.RecPath);
                string str998TargetDbName = Global.GetDbName(strTargetRecPath);

                // ����ƶ�Ŀ���strTargetRecPathͬ���ݿ⣬��Ҫȥ����¼�п��ܴ��ڵ�998$t
                if (strDlgTargetDbName == str998TargetDbName)
                {
                    // ���浱ǰ��¼��998�ֶ�
                    old_998 = this.m_marcEditor.Record.Fields.GetOneField("998", 0);

                    // ������ܴ��ڵ�998$t
                    if (old_998 != null)
                    {
                        SubfieldCollection subfields = old_998.Subfields;
                        Subfield old_t = subfields["t"];
                        if (old_t != null)
                        {
                            old_998.Subfields = subfields.Remove(old_t);
                            // ���998��һ�����ֶ�Ҳû���ˣ��Ƿ�����ֶ�Ҫɾ��?
                        }
                        else
                            old_998 = null; // ��ʾ(��Ȼû��ɾ��$t����)���ûָ�
                    }
                }

                string strMergeStyle = "";
                if ((merge_style & MergeStyle.ReserveSourceBiblio) != 0)
                    strMergeStyle = "reserve_source";
                else
                    strMergeStyle = "reserve_target";

                if ((merge_style & MergeStyle.MissingSourceSubrecord) != 0)
                    strMergeStyle += ",missing_source_subrecord";
                else if ((merge_style & MergeStyle.OverwriteSubrecord) != 0)
                {
                    // dp2library ��δʵ��������ܣ�����������ǰ���Ѿ��� SetBiblioInfo() API ����ɾ����Ŀ��λ���������Ӽ�¼��Ч����һ���ġ�(��Ȼ������ʵ������ԭ���Բ�����ô��)
                    // strMergeStyle += ",overwrite_target_subrecord";
                }
                // combine ���ʱȱʡ�ģ���������

                nRet = CopyBiblio(
                    "move",
                    dlg.RecPath,
                    strMergeStyle,
                    out strXml,
                    out strOutputBiblioRecPath,
                    out baOutputTimestamp,
                    out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);

            }
            finally
            {
#if NO
                // ��ԭ��ǰ���ڵļ�¼
                if (this.m_marcEditor.Marc != strOldMarc)
                    this.m_marcEditor.Marc = strOldMarc;
                if (this.m_marcEditor.Changed != bOldChanged)
                    this.m_marcEditor.Changed = bOldChanged;
#endif
                if (this.GetMarc() /*this.m_marcEditor.Marc*/ != strOldMarc)
                {
                    // this.m_marcEditor.Marc = strOldMarc;
                    this.SetMarc(strOldMarc);
                }
                if (this.GetMarcChanged() /*this.m_marcEditor.Changed*/ != bOldChanged)
                {
                    // this.m_marcEditor.Changed = bOldChanged;
                    this.SetMarcChanged(bOldChanged);
                }
            }

            if (nRet == -1)
            {
                this.BiblioRecPath = strOldBiblioRecPath;
                return;
            }

            // ��Ŀ���¼װ�뵱ǰ����
            this.LoadRecordOld(strOutputBiblioRecPath, "", false);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void toolStripSplitButton_searchDup_ButtonClick(object sender, EventArgs e)
        {
            ToolStripMenuItem_searchDupInExistWindow_Click(sender, e);
        }

        private void ToolStripMenuItem_searchDupInExistWindow_Click(object sender, EventArgs e)
        {
            string strError = "";

            // �����Ŀ��¼XML��ʽ
            string strXmlBody = "";
            int nRet = this.GetBiblioXml(
                "", // ��ʹ�Ӽ�¼·���п�marc��ʽ
                true,   // ������ԴID
                out strXmlBody,
                out strError);
            if (nRet == -1)
                goto ERROR1;


            DupForm form = this.MainForm.GetTopChildWindow<DupForm>();
            if (form == null)
            {
                form = new DupForm();

                form.MainForm = this.MainForm;
                form.MdiParent = this.MainForm;

                form.ProjectName = "<Ĭ��>";
                form.XmlRecord = strXmlBody;
                form.RecordPath = this.BiblioRecPath;

                form.AutoBeginSearch = true;

                form.Show();
            }
            else
            {
                form.Activate();
                if (form.WindowState == FormWindowState.Minimized)
                    form.WindowState = this.WindowState;

                form.ProjectName = "<Ĭ��>";
                form.XmlRecord = strXmlBody;
                form.RecordPath = this.BiblioRecPath;

                form.BeginSearch();
            }


            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void ToolStripMenuItem_searchDupInNewWindow_Click(object sender, EventArgs e)
        {
            string strError = "";

            // �����Ŀ��¼XML��ʽ
            string strXmlBody = "";
            int nRet = this.GetBiblioXml(
                "", // ��ʹ�Ӽ�¼·���п�marc��ʽ
                true,   // ������ԴID
                out strXmlBody,
                out strError);
            if (nRet == -1)
                goto ERROR1;


            DupForm form = new DupForm();

            form.MainForm = this.MainForm;
            form.MdiParent = this.MainForm;

            form.ProjectName = "<Ĭ��>";
            form.XmlRecord = strXmlBody;
            form.RecordPath = this.BiblioRecPath;

            form.AutoBeginSearch = true;

            form.Show();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
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
                    //"<script type='text/javascript' src='%datadir%/jquery.js'></script>" +
                    "<script type='text/javascript' charset='UTF-8' src='%datadir%\\getsummary.js" + "'></script>" +
                    "</head>";
            return
    "<head>" +
    "<LINK href='" + strCssFilePath + "' type='text/css' rel='stylesheet'>" +
    "</head>";
        }

        void DoViewComment(bool bOpenWindow)
        {
            string strError = "";
            string strHtml = "";
            string strXml = "";

            // �Ż���������ν�ؽ��з���������
            if (bOpenWindow == false)
            {
                if (this.MainForm.PanelFixedVisible == false
                    && (m_commentViewer == null || m_commentViewer.Visible == false))
                    return;
            }

            string strMARC = this.GetMarc();    // this.m_marcEditor.Marc;

            // �����Ŀ��¼XML��ʽ
            int nRet = this.GetBiblioXml(
                "", // ��ʹ�Ӽ�¼·���п�marc��ʽ
                true,   // ������ԴID
                out strXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string strFragmentXml = "";
            nRet = MarcUtil.LoadXmlFragment(strXml,
                out strFragmentXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (this.m_strActiveCatalogingRules != "<������>")
            {
                // ���ձ�Ŀ�������
                // ���һ���ض����� MARC ��¼
                // parameters:
                //      strStyle    Ҫƥ���styleֵ�����Ϊnull����ʾ�κ�44ֵ��ƥ�䣬ʵ����Ч����ȥ��$4������ȫ���ֶ�����
                // return:
                //      0   û��ʵ�����޸�
                //      1   ��ʵ�����޸�
                nRet = MarcUtil.GetMappedRecord(ref strMARC,
                    this.m_strActiveCatalogingRules);
            }

            // 2015/1/3
            string strImageFragment = BiblioSearchForm.GetImageHtmlFragment(
this.BiblioRecPath,
strMARC);

            strHtml = MarcUtil.GetHtmlOfMarc(strMARC,
                strFragmentXml,
                strImageFragment,
                false);
            string strFilterTitle = "";
            if (this.m_strActiveCatalogingRules != "<������>")
            {
                if (string.IsNullOrEmpty(this.m_strActiveCatalogingRules) == true)
                    strFilterTitle = "���˵������ַ�(����ȫ����Ŀ����)";
                else
                    strFilterTitle = "����Ŀ���� '" + this.m_strActiveCatalogingRules + "' ����";

                strFilterTitle = "<div class='cataloging_rule_title'>" + strFilterTitle + "</div>";
            }

            // TODO: ����иı䣬����ʾ�Ⱥ����?
            strHtml = "<html>" +
    GetHeadString() +
    "<body>" +
    strFilterTitle + 
    strHtml +
    GetTimestampHtml(this.BiblioTimestamp) +
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

            m_commentViewer.Text = "MARC���� '" + this.BiblioRecPath + "'";
            m_commentViewer.HtmlString = strHtml;
            m_commentViewer.XmlString = strXml;
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

        void marc_viewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_commentViewer != null)
            {
                this.MainForm.AppInfo.UnlinkFormState(m_commentViewer);
                this.m_commentViewer = null;
            }
        }

        internal static string GetTimestampHtml(byte [] timestamp)
        {
            return "<p>��Ŀ��¼ʱ���: " + ByteArray.GetHexTimeStampString(timestamp) + "</p>";
        }

        List<string> GetExistCatalogingRules()
        {
            return Global.GetExistCatalogingRules(this.GetMarc()/*this.m_marcEditor.Marc*/);

        }

        // ��ǰ��ı�Ŀ����
        //      "" ��ʾÿ����Ŀ������������ʾ��ʱ�����˵� $* �� {cr:...}
        //      "<������>" ��ʾ������
        string m_strActiveCatalogingRules = "<������>";

        private void toolStripDropDownButton_marcEditor_someFunc_DropDownOpening(object sender, EventArgs e)
        {
            this.toolStripMenuItem_marcEditor_setActiveCatalogingRule.DropDownItems.Clear();
            List<string> catalogrules = GetExistCatalogingRules();
            catalogrules.Insert(0, "<������>");
            catalogrules.Insert(1, "");
            bool bFound = false;
            foreach(string s in catalogrules)
            {
                string strName = s;
                if (string.IsNullOrEmpty(s) == true)
                    strName = "<ȫ��>";
                ToolStripMenuItem submenu = new ToolStripMenuItem(); 
                submenu.Text = strName;
                if (s == m_strActiveCatalogingRules)
                {
                    submenu.Checked = true;
                    bFound = true;
                }
                submenu.Click += new EventHandler(catalogingrule_submenu_Click);
                submenu.Tag = s;

                this.toolStripMenuItem_marcEditor_setActiveCatalogingRule.DropDownItems.Add(submenu);
            }

            if (bFound == false)
            {
                ToolStripMenuItem submenu = new ToolStripMenuItem();
                submenu.Text = this.m_strActiveCatalogingRules;
                submenu.Checked = true;
                submenu.Click += new EventHandler(catalogingrule_submenu_Click);
                submenu.Tag = this.m_strActiveCatalogingRules;

                this.toolStripMenuItem_marcEditor_setActiveCatalogingRule.DropDownItems.Add(submenu);
            }

        }

        void catalogingrule_submenu_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuitem = (ToolStripMenuItem)sender;
            this.m_strActiveCatalogingRules = (string)menuitem.Tag;

            DoViewComment(false);
        }

        /// <summary>
        /// �Ƿ���ʾ�����ֹݵĲ��¼
        /// </summary>
        public bool DisplayOtherLibraryItem
        {
            get
            {
                // ��ʾ�����ֹݵĲ��¼
                return this.MainForm.AppInfo.GetBoolean(
    "entityform",
    "displayOtherLibraryItem",
    false);
            }
        }

#if NO
        // 
        /// <summary>
        /// ���õ�ǰ��¼��ͼƬ����
        /// </summary>
        /// <param name="image">Image ����</param>
        /// <param name="strUsage">��;�ַ���</param>
        /// <param name="strShrinkComment">����������ʾ�ַ���</param>
        /// <param name="strID">����ʵ��ʹ�õĶ��� ID</param>
        /// <param name="strError">���ش�����Ϣ</param>
        /// <returns>0: �ɹ�; -1: ����</returns>
        public int SetImageObject(Image image,
            string strUsage,
            out string strShrinkComment,
            out string strID,
            out string strError)
        {
            strError = "";
            strShrinkComment = "";
            strID = "";
            int nRet = 0;

            // �Զ���Сͼ��
            string strMaxWidth = this.MainForm.AppInfo.GetString(
    "entityform",
    "paste_pic_maxwidth",
    "-1");
            int nMaxWidth = -1;
            Int32.TryParse(strMaxWidth,
                out nMaxWidth);
            if (nMaxWidth != -1)
            {
                int nOldWidth = image.Width;
                // ��Сͼ��
                // parameters:
                //		nNewWidth0	���(0��ʾ���仯)
                //		nNewHeight0	�߶�
                //      bRatio  �Ƿ񱣳��ݺ����
                // return:
                //      -1  ����
                //      0   û�б�Ҫ����(objBitmapδ����)
                //      1   �Ѿ�����
                nRet = DigitalPlatform.Drawing.GraphicsUtil.ShrinkPic(ref image,
                    nMaxWidth,
                    0,
                    true,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nOldWidth != image.Width)
                {
                    strShrinkComment = "ͼ���ȱ��� " + nOldWidth.ToString() + " ������С�� " + image.Width.ToString() + " ����";
                }
            }

            string strTempFilePath = FileUtil.NewTempFileName(this.MainForm.DataDir,
                "~temp_make_pic_",
                ".png");

            image.Save(strTempFilePath, System.Drawing.Imaging.ImageFormat.Png);
            image.Dispose();
            image = null;

            ListViewItem item = null;
            List<ListViewItem> items = this.binaryResControl1.FindItemByUsage(strUsage);
            if (items.Count == 0)
            {
                nRet = this.binaryResControl1.AppendNewItem(
                    strTempFilePath,
                    strUsage,
                    out item,
                    out strError);
            }
            else
            {
                item = items[0];
                nRet = this.binaryResControl1.ChangeObjectFile(item,
                    strTempFilePath,
                    strUsage,
                    out strError);
            }
            if (nRet == -1)
                goto ERROR1;

            strID = ListViewUtil.GetItemText(item, BinaryResControl.COLUMN_ID);

            return 0;
        ERROR1:
            return -1;
        }

#endif

        /// <summary>
        /// ���õ�ǰ��¼��ͼƬ����
        /// </summary>
        /// <param name="image">Image ����</param>
        /// <param name="strUsage">��;�ַ���</param>
        /// <param name="strID">����ʵ��ʹ�õĶ��� ID</param>
        /// <param name="strError">���ش�����Ϣ</param>
        /// <returns>0: �ɹ�; -1: ����</returns>
        public int SetImageObject(Image image,
            string strUsage,
            out string strID,
            out string strError)
        {
            strError = "";
            strID = "";
            int nRet = 0;

            string strTempFilePath = FileUtil.NewTempFileName(this.MainForm.DataDir,
                "~temp_make_pic_",
                ".png");

            image.Save(strTempFilePath, System.Drawing.Imaging.ImageFormat.Png);
            //image.Dispose();
            //image = null;

            ListViewItem item = null;
            List<ListViewItem> items = this.binaryResControl1.FindItemByUsage(strUsage);
            if (items.Count == 0)
            {
                nRet = this.binaryResControl1.AppendNewItem(
                    strTempFilePath,
                    strUsage,
                    out item,
                    out strError);
            }
            else
            {
                item = items[0];

                nRet = this.binaryResControl1.ChangeObjectFile(item,
                    strTempFilePath,
                    strUsage,
                    out strError);
            }
            if (nRet == -1)
                goto ERROR1;

            strID = ListViewUtil.GetItemText(item, BinaryResControl.COLUMN_ID);

            return 0;
        ERROR1:
            return -1;
        }

        // return:
        //      -1  ����
        //      ����  ʵ��ɾ���ĸ���
        public int DeleteImageObject(string strID)
        {
            List<ListViewItem> items = this.binaryResControl1.FindItemByID(strID);
            if (items.Count > 0)
                return this.binaryResControl1.MaskDelete(items);
            return 0;
        }
#if NO
        static bool IsAnImage(string filename)
        {
            try
            {
                Image newImage = Image.FromFile(filename);
            }
            catch (OutOfMemoryException ex)
            {
                // Image.FromFile will throw this if file is invalid.
                return false;
            }
            return true;
        }
#endif

        // �Ӽ�����������ͼ��
        private void ToolStripMenuItem_insertCoverImageFromClipboard_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            List<ListViewItem> deleted_items = this.binaryResControl1.FindAllMaskDeleteItem();
            if (deleted_items.Count > 0)
            {
                strError = "��ǰ�б��ɾ���Ķ�����δ�ύ���档�����ύ��Щ������ٽ��в������ͼ��Ĳ���";
                goto ERROR1;
            }

            Image image = null;
            IDataObject obj1 = Clipboard.GetDataObject();
            if (obj1.GetDataPresent(typeof(Bitmap)))
            {
                image = (Image)obj1.GetData(typeof(Bitmap));
            }
            else if (obj1.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])obj1.GetData(DataFormats.FileDrop);

                try
                {
                    image = Image.FromFile(files[0]);
                }
                catch (OutOfMemoryException ex)
                {
                    strError = "��ǰ Windows �������еĵ�һ���ļ�����ͼ���ļ����޷���������ͼ��";
                    goto ERROR1;
                }
            }
            else
            {
                strError = "��ǰ Windows ��������û��ͼ�ζ����޷���������ͼ��";
                goto ERROR1;
            }

            CreateCoverImageDialog dlg = null;
            try
            {
                dlg = new CreateCoverImageDialog();

                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.OriginImage = image;
                this.MainForm.AppInfo.LinkFormState(dlg, "entityform_CreateCoverImageDialog_state");
                dlg.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(dlg);
                if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return;

            }
            finally
            {
                if (image != null)
                {
                    image.Dispose();
                    image = null;
                }
            }

            this.SynchronizeMarc();

            foreach (ImageType type in dlg.ResultImages)
            {
                if (type.Image == null)
                {
                    continue;
                }

                string strType = "FrontCover." + type.TypeName;
                string strSize = type.Image.Width.ToString() + "X" + type.Image.Height.ToString() + "px";

                // string strShrinkComment = "";
                string strID = "";
                nRet = SetImageObject(type.Image,
                    strType,    // "coverimage",
                    // out strShrinkComment,
                    out strID,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                Field field_856 = null;
                List<Field> fields = DetailHost.Find856ByResID(this.m_marcEditor,
                        strID);
                if (fields.Count == 1)
                {
                    field_856 = fields[0];
                    // TODO: ���ֶԻ���
                }
                else if (fields.Count > 1)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ MARC �༭�����Ѿ����� " + fields.Count.ToString() + " �� 856 �ֶ��� $" + DetailHost.LinkSubfieldName + " ���ֶι����˶��� ID '" + strID + "' ���Ƿ�Ҫ�༭���еĵ�һ�� 856 �ֶ�?\r\n\r\n(ע���ɸ��� MARC �༭����ѡ��һ������� 856 �ֶν��б༭)\r\n\r\n(OK: �༭���еĵ�һ�� 856 �ֶ�; Cancel: ȡ������",
                        "EntityForm",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Cancel)
                        return;
                    field_856 = fields[0];
                    // TODO: ���ֶԻ���
                }
                else
                    field_856 = this.m_marcEditor.Record.Fields.Add("856", "  ", "", true);

                field_856.IndicatorAndValue = ("72$3Cover Image$" + DetailHost.LinkSubfieldName + "uri:" + strID + "$xtype:"+strType+";size:"+strSize+"$2dp2res").Replace('$', (char)31);
            }

            if (this.tabControl_biblioInfo.SelectedTab == this.tabPage_template)
                this.SynchronizeMarc();

            MessageBox.Show(this, "����ͼ���856�ֶ��Ѿ��ɹ�������\r\n"
                // + strShrinkComment
                + "\r\n\r\n(����ǰ��¼��δ���棬ͼ��������δ�ύ��������)\r\n\r\nע���Ժ󱣴浱ǰ��¼��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void checkedComboBox_dbName_DropDown(object sender, EventArgs e)
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

        private void checkedComboBox_dbName_ItemChecked(object sender, ItemCheckedEventArgs e)
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

        private void MenuItem_marcEditor_getKeys_Click(object sender, EventArgs e)
        {
            string strError = "";

            // �����Ŀ��¼XML��ʽ
            string strBiblioXml = "";
            int nRet = this.GetBiblioXml(
                "", // ��ʹ�Ӽ�¼·���п�marc��ʽ
                false,
                out strBiblioXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string strResultXml = "";
            nRet = GetKeys(this.BiblioRecPath,
                strBiblioXml,
                out strResultXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            XmlViewerForm dlg = new XmlViewerForm();

            dlg.Text = "��Ŀ��¼�ļ�����";
            dlg.MainForm = this.MainForm;
            dlg.XmlString = strResultXml;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this); 
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        int GetKeys(string strBiblioRecPath,
            string strBiblioXml,
            out string strResultXml,
            out string strError)
        {
            strError = "";
            strResultXml = "";

            Progress.OnStop += new StopEventHandler(this.DoStop);
            Progress.Initial("���ڻ����Ŀ��¼ " + strBiblioRecPath + " �ļ����� ...");
            Progress.BeginLoop();

            try
            {

                string[] formats = new string[1];
                formats[0] = "keys";

                string[] results = null;
                byte[] timestamp = null;
                long lRet = Channel.GetBiblioInfos(
                    Progress,
                    strBiblioRecPath,
                    strBiblioXml,
                    formats,
                    out results,
                    out timestamp,
                    out strError);
                if (lRet == -1)
                    return -1;
                if (results != null && results.Length > 0)
                    strResultXml = results[0];
            }
            finally
            {
                Progress.EndLoop();
                Progress.OnStop -= new StopEventHandler(this.DoStop);
                Progress.Initial("");
            }

            return 1;
        }

        bool _readOnly = false;
        public bool ReadOnly
        {
            get
            {
                return this._readOnly;
            }
            set
            {
                this._readOnly = value;
                this.tableLayoutPanel_main.Enabled = !value;
            }
        }

        private void tabControl_biblioInfo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.tabControl_biblioInfo.SelectedTab == this.tabPage_marc
                || this.tabControl_biblioInfo.SelectedTab == this.tabPage_template)
                SynchronizeMarc();
        }

        private void MenuItem_marcEditor_editMacroTable_Click(object sender, EventArgs e)
        {
            MacroTableDialog dlg = new MacroTableDialog();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.XmlFileName = Path.Combine(this.MainForm.DataDir, "marceditor_macrotable.xml");

            this.MainForm.AppInfo.LinkFormState(dlg, "entityform_MacroTableDialog_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);
            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;
        }

        private void toolStripSplitButton_insertCoverImage_ButtonClick(object sender, EventArgs e)
        {
            ToolStripMenuItem_insertCoverImageFromClipboard_Click(sender, e);
        }

        private void ToolStripMenuItem_removeCoverImage_Click(object sender, EventArgs e)
        {
            bool bChanged = false;
            MarcRecord record = new MarcRecord(this.GetMarc());
            MarcNodeList subfields = record.select("field[@name='856']/subfield[@name='x']");

            foreach (MarcSubfield subfield in subfields)
            {
                string x = subfield.Content;
                if (string.IsNullOrEmpty(x) == true)
                    continue;
                Hashtable table = StringUtil.ParseParameters(x, ';', ':');
                string strType = (string)table["type"];
                if (string.IsNullOrEmpty(strType) == true)
                    continue;
                if (StringUtil.HasHead(strType, "FrontCover") == true)
                {
                    string u = subfield.Parent.select("subfield[@name='u']").FirstContent;

                    subfield.Parent.detach();
                    bChanged = true;

                    DeleteImageObject(GetImageID(u));
                }


            }

            if (bChanged == true)
                this.SetMarc(record.Text);
            else
                MessageBox.Show(this, "û�з��ַ���ͼ��� 856 �ֶ�");
        }

        static string GetImageID(string strUri)
        {
            if (StringUtil.HasHead(strUri, "http:") == true)
                return null;
            if (StringUtil.HasHead(strUri, "uri:") == true)
                return strUri.Substring(4).Trim();
            return strUri;
        }
    }

    /// <summary>
    /// ��Ǽǰ�ť�Ķ�������
    /// </summary>
    public enum RegisterType
    {
        /// <summary>
        /// ֻ����
        /// </summary>
        SearchOnly = 0,   // ֻ����
        /// <summary>
        /// ���ٵǼ�
        /// </summary>
        QuickRegister = 1, // ���ٵǼ�
        /// <summary>
        /// �Ǽ�
        /// </summary>
        Register = 2, // �Ǽ�
    }

#if NO
    // ���ĽǺ���ʱ�Ĵ������
    public enum SjHmStyle
    {
        None = 0,	// �����κθı�
    }
#endif

    class PendingLoadRequest
    {
        public string RecPath = "";
        public string PrevNextStyle = "";
    }

    /// <summary>
    /// У�����ݵ�������
    /// </summary>
    public class VerifyHost
    {
        /// <summary>
        /// �ֲᴰ
        /// </summary>
        public EntityForm DetailForm = null;

        /// <summary>
        /// ����ַ���
        /// </summary>
        public string ResultString = "";

        /// <summary>
        /// �ű������� Assembly
        /// </summary>
        public Assembly Assembly = null;

        /// <summary>
        /// ����һ�����ܺ���
        /// </summary>
        /// <param name="strFuncName">��������</param>
        public void Invoke(string strFuncName)
        {
            Type classType = this.GetType();

            // ���ó�Ա����
            classType.InvokeMember(strFuncName,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.InvokeMethod
                ,
                null,
                this,
                null);
        }

        /// <summary>
        /// ��ں���
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void Main(object sender, HostEventArgs e)
        {

        }
    }

    /// <summary>
    /// ��������У��� FilterDocument ������(MARC �������ĵ���)
    /// </summary>
    public class VerifyFilterDocument : FilterDocument
    {
        /// <summary>
        /// ��������
        /// </summary>
        public VerifyHost FilterHost = null;
    }
}