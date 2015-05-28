using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Xml;

using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using System.Deployment.Application;

using System.Diagnostics;
using System.Net;   // for WebClient class
using System.IO;
using System.Web;

using System.Reflection;

using System.Drawing.Text;
using System.Speech.Synthesis;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.IO;   // DateTimeUtil
using DigitalPlatform.CommonControl;

using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.GcatClient.gcat_new_ws;
using DigitalPlatform.GcatClient;
using DigitalPlatform.Marc;
using DigitalPlatform.LibraryServer;
using DigitalPlatform.MarcDom;
using System.Security.Permissions;

namespace dp2Circulation
{
    /// <summary>
    /// ��ܴ���
    /// </summary>
    public partial class MainForm : Form
    {
        // 2014/10/3
        // MarcFilter���󻺳��
        public FilterCollection Filters = new FilterCollection();

        SpeechSynthesizer m_speech = new SpeechSynthesizer();

        private DigitalPlatform.Drawing.QrRecognitionControl qrRecognitionControl1;

        internal event EventHandler FixedSelectedPageChanged = null;

        #region �ű�֧��

        /// <summary>
        /// �ű�������
        /// </summary>
        public ScriptManager ScriptManager = new ScriptManager();

        MainFormHost objStatis = null;
        Assembly AssemblyMain = null;

        // int AssemblyVersion = 0;
        string m_strInstanceDir = "";

        /// <summary>
        /// C# �ű�ִ�е�ʵ��Ŀ¼
        /// </summary>
        public string InstanceDir
        {
            get
            {
                if (string.IsNullOrEmpty(this.m_strInstanceDir) == false)
                    return this.m_strInstanceDir;

                this.m_strInstanceDir = PathUtil.MergePath(this.DataDir, "~bin_" + Guid.NewGuid().ToString());
                PathUtil.CreateDirIfNeed(this.m_strInstanceDir);

                return this.m_strInstanceDir;
            }
        }

        #endregion


        CommentViewerForm m_propertyViewer = null;

        internal ObjectCache<Assembly> AssemblyCache = new ObjectCache<Assembly>();
        internal ObjectCache<XmlDocument> DomCache = new ObjectCache<XmlDocument>();

        // internal FormWindowState MdiWindowState = FormWindowState.Normal;

        // ���������õ�ǰ�˽�����Ϣ�ӿڲ���
        /*
<clientFineInterface name="�Ͽ�Զ��"/>
         * * */
        /// <summary>
        /// ���������õ�ǰ�˽�����Ϣ�ӿڲ���
        /// </summary>
        public string ClientFineInterfaceName = "";

        /// <summary>
        /// �ӷ������˻�ȡ�� CallNumber ������Ϣ
        /// </summary>
        public XmlDocument CallNumberCfgDom = null;
        string CallNumberInfo = "";  // <callNumber>Ԫ�ص�InnerXml

        // public string LibraryServerDiretory = "";   // dp2libraryws��library.xml�����õ�<libraryserver url='???'>����

        /// <summary>
        /// MDI Client ����
        /// </summary>
        public MdiClient MdiClient = null;

        BackgroundForm m_backgroundForm = null; // MDI Client ����������ʾ���ֵĴ���

        /// <summary>
        /// ��ĿժҪ���ػ���
        /// </summary>
        public StringCache SummaryCache = new StringCache();

        /// <summary>
        /// ���� XML ����
        /// ֻ���ڻ�ȡ����֤��Ƭʱ����Ϊ��ʱ�����ݱ䶯������
        /// </summary>
        public StringCache ReaderXmlCache = new StringCache();  // ֻ���ڻ�ȡ����֤��Ƭʱ����Ϊ��ʱ�����ݱ䶯������ 2012/1/5

        // ΪC#�ű���׼��
        /// <summary>
        /// �����洢
        /// Ϊ C# �ű���׼��
        /// </summary>
        public Hashtable ParamTable = new Hashtable();

        /// <summary>
        /// ���ټ�ƴ������
        /// </summary>
        public QuickPinyin QuickPinyin = null;

        /// <summary>
        /// ISBN �и����
        /// </summary>
        public IsbnSplitter IsbnSplitter = null;

        /// <summary>
        /// �ĽǺ������
        /// </summary>
        public QuickSjhm QuickSjhm = null;

        /// <summary>
        /// ���ر����
        /// </summary>
        public QuickCutter QuickCutter = null;

        /// <summary>
        /// �����������ļ�����
        /// </summary>
        public CfgCache cfgCache = new CfgCache();

        // ͳ�ƴ���assembly�İ汾����
        /*
        public int OperLogStatisAssemblyVersion = 0;
        public int ReaderStatisAssemblyVersion = 0;
        public int ItemStatisAssemblyVersion = 0;
        public int BiblioStatisAssemblyVersion = 0;
        public int XmlStatisAssemblyVersion = 0;
        public int Iso2709StatisAssemblyVersion = 0;
         * */
        internal int StatisAssemblyVersion = 0;

        bool m_bUrgent = false;
        string EncryptKey = "dp2circulation_client_password_key";

        /// <summary>
        /// ����Ŀ¼
        /// </summary>
        public string DataDir = "";

        /// <summary>
        /// �û�Ŀ¼
        /// </summary>
        public string UserDir = ""; // 2013/6/16


        public string UserTempDir = ""; // 2015/1/4

        // ���������Ϣ
        /// <summary>
        /// ���ô洢
        /// </summary>
        public ApplicationInfo AppInfo = new ApplicationInfo("dp2circulation.xml");

        /// <summary>
        /// Stop ������
        /// </summary>
        public DigitalPlatform.StopManager stopManager = new DigitalPlatform.StopManager();

        #region ���ݿ���Ϣ����

        /// <summary>
        /// ��Ŀ�����·����Ϣ����
        /// </summary>
        public BiblioDbFromInfo[] BiblioDbFromInfos = null;   // ��Ŀ�����·����Ϣ

        /// <summary>
        /// ���߿����·����Ϣ����
        /// </summary>
        public BiblioDbFromInfo[] ReaderDbFromInfos = null;   // ���߿����·����Ϣ 2012/2/8

        /// <summary>
        /// ʵ������·����Ϣ����
        /// </summary>
        public BiblioDbFromInfo[] ItemDbFromInfos = null;   // ʵ������·����Ϣ 2012/5/5

        /// <summary>
        /// ���������·����Ϣ����
        /// </summary>
        public BiblioDbFromInfo[] OrderDbFromInfos = null;   // ���������·����Ϣ 2012/5/5

        /// <summary>
        /// �ڿ����·����Ϣ����
        /// </summary>
        public BiblioDbFromInfo[] IssueDbFromInfos = null;   // �ڿ����·����Ϣ 2012/5/5

        /// <summary>
        /// ��ע�����·����Ϣ����
        /// </summary>
        public BiblioDbFromInfo[] CommentDbFromInfos = null;   // ��ע�����·����Ϣ 2012/5/5

        /// <summary>
        /// ��Ʊ�����·����Ϣ����
        /// </summary>
        public BiblioDbFromInfo[] InvoiceDbFromInfos = null;   // ��Ʊ�����·����Ϣ 2012/11/8

        /// <summary>
        /// ΥԼ������·����Ϣ����
        /// </summary>
        public BiblioDbFromInfo[] AmerceDbFromInfos = null;   // ΥԼ������·����Ϣ 2012/11/8

        /// <summary>
        /// ��Ŀ�����Լ���
        /// </summary>
        public List<BiblioDbProperty> BiblioDbProperties = null;

        /// <summary>
        /// ��ͨ�����Լ���
        /// </summary>
        public List<NormalDbProperty> NormalDbProperties = null;

        // public string[] ReaderDbNames = null;
        /// <summary>
        /// ���߿����Լ���
        /// </summary>
        public List<ReaderDbProperty> ReaderDbProperties = null;

        /// <summary>
        /// ʵ�ÿ����Լ���
        /// </summary>
        public List<UtilDbProperty> UtilDbProperties = null;

        #endregion

        /// <summary>
        /// ��ǰ���ӵķ�������ͼ�����
        /// </summary>
        public string LibraryName = "";

        //
        internal ReaderWriterLock m_lockChannel = new ReaderWriterLock();
        internal static int m_nLockTimeout = 5000;	// 5000=5��

        /// <summary>
        /// ͨѶͨ����MainForm �Լ�ʹ��
        /// </summary>
        public LibraryChannel Channel = new LibraryChannel();

        /// <summary>
        /// ֹͣ����
        /// </summary>
        public DigitalPlatform.Stop Stop = null;

        /// <summary>
        /// �������Դ���
        /// </summary>
        public string Lang = "zh";

        // const int WM_PREPARE = API.WM_USER + 200;

        Hashtable valueTableCache = new Hashtable();

        /// <summary>
        /// ������ʷ����
        /// </summary>
        public OperHistory OperHistory = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            try
            {
                this.qrRecognitionControl1 = new DigitalPlatform.Drawing.QrRecognitionControl();
            }
            catch
            {
            }
            // 
            // tabPage_camera
            // 
            this.tabPage_camera.Controls.Add(this.qrRecognitionControl1);
#if NO
            this.tabPage_camera.Location = new System.Drawing.Point(4, 25);
            this.tabPage_camera.Name = "tabPage_camera";
            this.tabPage_camera.Size = new System.Drawing.Size(98, 202);
            this.tabPage_camera.TabIndex = 4;
            this.tabPage_camera.Text = "QR ʶ��";
            this.tabPage_camera.UseVisualStyleBackColor = true;
#endif
            // 
            // qrRecognitionControl1
            // 
            this.qrRecognitionControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.qrRecognitionControl1.Location = new System.Drawing.Point(0, 0);
            this.qrRecognitionControl1.Name = "qrRecognitionControl1";
            this.qrRecognitionControl1.Size = new System.Drawing.Size(98, 202);
            this.qrRecognitionControl1.MinimumSize = new Size(200, 200);
            this.qrRecognitionControl1.TabIndex = 0;
            this.qrRecognitionControl1.BackColor = Color.DarkGray;   //  System.Drawing.SystemColors.Window;
        }

        /// <summary>
        /// �Ƿ�Ϊ��װ���һ������
        /// </summary>
        public bool IsFirstRun
        {
            get
            {
                try
                {
                    if (ApplicationDeployment.CurrentDeployment.IsFirstRun == true)
                        return true;

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

#if NO
        // defines how far we are extending the Glass margins
        private API.MARGINS margins;

        /// <summary>
        /// Override the OnPaintBackground method, to draw the desired
        /// Glass regions black and display as Glass
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (API.DwmIsCompositionEnabled())
            {
                e.Graphics.Clear(Color.Black);
                // put back the original form background for non-glass area
                Rectangle clientArea = new Rectangle(
                margins.Left,
                margins.Top,
                this.ClientRectangle.Width - margins.Left - margins.Right,
                this.ClientRectangle.Height - margins.Top - margins.Bottom);
                Brush b = new SolidBrush(this.BackColor);
                e.Graphics.FillRectangle(b, clientArea);
            }
        }


        /// <summary>
        /// Use the form padding values to define a Glass margin
        /// </summary>
        private void SetGlassRegion()
        {
            // Set up the glass effect using padding as the defining glass region
            if (API.DwmIsCompositionEnabled())
            {
                Padding padding = new System.Windows.Forms.Padding(50);
                margins = new API.MARGINS();
                margins.Top = padding.Top;
                margins.Left = padding.Left;
                margins.Bottom = padding.Bottom;
                margins.Right = padding.Right;
                API.DwmExtendFrameIntoClientArea(this.Handle, ref margins);
            }
        }
#endif

        private void MainForm_Load(object sender, EventArgs e)
        {
            /*
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            this.BackColor = Color.Transparent;
            this.toolStrip_main.BackColor = Color.Transparent;
             * */

            this.SetBevel(false);
#if NO
            if (!API.DwmIsCompositionEnabled())
            {
                //MessageBox.Show("This demo requires Vista, with Aero enabled.");
                //Application.Exit();
            }
            else
            {
                SetGlassRegion();
            }
#endif

            // ���MdiClient����
            {
                Type t = typeof(Form);
                PropertyInfo pi = t.GetProperty("MdiClient", BindingFlags.Instance | BindingFlags.NonPublic);
                this.MdiClient = (MdiClient)pi.GetValue(this, null);
                this.MdiClient.SizeChanged += new EventHandler(MdiClient_SizeChanged);

                m_backgroundForm = new BackgroundForm();
                m_backgroundForm.MdiParent = this;
                m_backgroundForm.Show();
            }


            if (ApplicationDeployment.IsNetworkDeployed == true)
            {
                // MessageBox.Show(this, "network");
                DataDir = Application.LocalUserAppDataPath;
            }
            else
            {
                // MessageBox.Show(this, "no network");
                DataDir = Environment.CurrentDirectory;
            }

            string strError = "";
            int nRet = 0;

            {
                // 2013/6/16
                this.UserDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "dp2Circulation_v2");
                PathUtil.CreateDirIfNeed(this.UserDir);

                this.UserTempDir = Path.Combine(this.UserDir, "temp");
                PathUtil.CreateDirIfNeed(this.UserTempDir);

                // ɾ��һЩ��ǰ��Ŀ¼
                string strDir = PathUtil.MergePath(this.DataDir, "operlogcache");
                if (Directory.Exists(strDir) == true)
                {
                    nRet = Global.DeleteDataDir(
                        this,
                        strDir,
                        out strError);
                    if (nRet == -1)
                    {
                        MessageBox.Show(this, "ɾ����ǰ�������ļ�Ŀ¼ʱ��������: " + strError);
                    }
                }
                strDir = PathUtil.MergePath(this.DataDir, "fingerprintcache");
                if (Directory.Exists(strDir) == true)
                {
                    nRet = Global.DeleteDataDir(
                    this,
                    strDir,
                    out strError);
                    if (nRet == -1)
                    {
                        MessageBox.Show(this, "ɾ����ǰ�������ļ�Ŀ¼ʱ��������: " + strError);
                    }
                }
            }

            {
                string strCssUrl = PathUtil.MergePath(this.DataDir, "/background.css");
                string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";

                Global.WriteHtml(m_backgroundForm.WebBrowser,
                    "<html><head>" + strLink + "</head><body>");
            }

            // ���ô��ڳߴ�״̬
            if (AppInfo != null)
            {
                // �״����У��������á�΢���źڡ�����
                if (this.IsFirstRun == true)
                {
                    SetFirstDefaultFont();
                }

                MainForm.SetControlFont(this, this.DefaultFont);

                AppInfo.LoadFormStates(this,
                    "mainformstate",
                    FormWindowState.Maximized);

                // ����һ�����Ͱ���Щ��������Ϊ��ʼ״̬
                this.DisplayScriptErrorDialog = false;
            }

            InitialFixedPanel();

            // this.Update();   // �Ż�


            stopManager.Initial(this.toolButton_stop,
                (object)this.toolStripStatusLabel_main,
                (object)this.toolStripProgressBar_main);
            stopManager.OnDisplayMessage += new DisplayMessageEventHandler(stopManager_OnDisplayMessage);
            this.SetMenuItemState();


            // cfgcache
            nRet = cfgCache.Load(this.DataDir
                + "\\cfgcache.xml",
                out strError);
            if (nRet == -1)
            {
                if (IsFirstRun == false)
                    MessageBox.Show(strError);
            }


            cfgCache.TempDir = this.DataDir
                + "\\cfgcache";
            cfgCache.InstantSave = true;

            // 2013/4/12
            // �����ǰ������ļ�
            cfgCache.Upgrade();

            // �����ϴγ���������ֹʱ�����Ķ��ڱ�������
            bool bSavePasswordLong =
    AppInfo.GetBoolean(
    "default_account",
    "savepassword_long",
    false);

            if (bSavePasswordLong == false)
            {
                AppInfo.SetString(
                    "default_account",
                    "password",
                    "");
            }

            StartPrepareNames(true, true);

            this.MdiClient.ClientSizeChanged += new EventHandler(MdiClient_ClientSizeChanged);

            // GuiUtil.RegisterIE9DocMode();

            #region �ű�֧��
            ScriptManager.applicationInfo = this.AppInfo;
            ScriptManager.CfgFilePath =
                this.DataDir + "\\mainform_statis_projects.xml";
            ScriptManager.DataDir = this.DataDir;

            ScriptManager.CreateDefaultContent -= new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);
            ScriptManager.CreateDefaultContent += new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);

            try
            {
                ScriptManager.Load();
            }
            catch (FileNotFoundException)
            {
                // ���ر��� 2009/2/4 new add
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            #endregion


            this.qrRecognitionControl1.Catched += new DigitalPlatform.Drawing.CatchedEventHandler(qrRecognitionControl1_Catched);
            this.qrRecognitionControl1.CurrentCamera = AppInfo.GetString(
                "mainform",
                "current_camera",
                "");
            this.qrRecognitionControl1.EndCatch();  // һ��ʼ��ʱ�򲢲�������ͷ 2013/5/25

            this.m_strPinyinGcatID = this.AppInfo.GetString("entity_form", "gcat_pinyin_api_id", "");
            this.m_bSavePinyinGcatID = this.AppInfo.GetBoolean("entity_form", "gcat_pinyin_api_saveid", false);

#if NO
            // 2015/5/24
            MouseLButtonMessageFilter filter = new MouseLButtonMessageFilter();
            filter.MainForm = this;
            Application.AddMessageFilter(filter);
#endif

        }

        string m_strPrevMessageText = "";

        void stopManager_OnDisplayMessage(object sender, DisplayMessageEventArgs e)
        {
            if (m_backgroundForm != null)
            {
                if (e.Message != m_strPrevMessageText)
                {
                    m_backgroundForm.AppendHtml(HttpUtility.HtmlEncode(e.Message) + "<br/>");
                    m_strPrevMessageText = e.Message;
                }
            }
        }

        void MdiClient_SizeChanged(object sender, EventArgs e)
        {
            m_backgroundForm.Size = new System.Drawing.Size(this.MdiClient.ClientSize.Width, this.MdiClient.ClientSize.Height);
        }

        void SetFirstDefaultFont()
        {
            if (this.DefaultFont != null)
                return;

            try
            {
                FontFamily family = new FontFamily("΢���ź�");
            }
            catch
            {
                return;
            }
            this.DefaultFontString = "΢���ź�, 9pt";
        }


        void InstallBarcodeFont()
        {
            bool bInstalled = true;
            try
            {
                FontFamily family = new FontFamily("C39HrP24DhTt");
            }
            catch
            {
                bInstalled = false;
            }

            if (bInstalled == true)
            {
                // �Ѿ���װ
                return;
            }

            // 
            string strFontFilePath = PathUtil.MergePath(this.DataDir, "b3901.ttf");
            int nRet = API.AddFontResourceA(strFontFilePath);
            if (nRet == 0)
            {
                // ʧ��
                MessageBox.Show(this, "��װ�����ļ� " + strFontFilePath + " ʧ��");
                return;
            }


            {
                // �ɹ�

                // Ϊ�˽�� GDI+ ��һ�� BUG
                // PrivateFontCollection m_pfc = new PrivateFontCollection();
                GlobalVars.PrivateFonts.AddFontFile(strFontFilePath);
#if NO
                API.SendMessage((IntPtr)0xffff,0x001d, IntPtr.Zero, IntPtr.Zero);
                API.SendMessage(this.Handle, 0x001d, IntPtr.Zero, IntPtr.Zero);
#endif
            }

#if NO
            /*
            try
            {
                FontFamily family = new FontFamily("C39HrP24DhTt");
            }
            catch (Exception ex)
            {
                bInstalled = false;
            }
             * */
            InstalledFontCollection enumFonts = new InstalledFontCollection();
            FontFamily[] fonts = enumFonts.Families;

            string strResult = "";
            foreach (FontFamily m in fonts)
            {
                strResult += m.Name + "\r\n";
            }

            int i = 0;
            i++;
#endif
        }

        void MdiClient_ClientSizeChanged(object sender, EventArgs e)
        {
            AcceptForm top = this.GetTopChildWindow<AcceptForm>();
            if (top != null)
            {
                top.OnMdiClientSizeChanged();
            }
        }

        /// <summary>
        /// ��ʼ��ʼ������ø��ֲ���
        /// </summary>
        /// <param name="bFullInitial">�Ƿ�ִ��ȫ����ʼ��</param>
        /// <param name="bRestoreLastOpenedWindow">�Ƿ�Ҫ�ָ���ǰ�򿪵Ĵ���</param>
        public void StartPrepareNames(bool bFullInitial, bool bRestoreLastOpenedWindow)
        {
#if NO
            if (bFullInitial == true)
                API.PostMessage(this.Handle, WM_PREPARE, 1, 0);
            else
                API.PostMessage(this.Handle, WM_PREPARE, 0, 0);
#endif
            this.BeginInvoke(new Func<bool, bool, bool>(InitialProperties), bFullInitial, bRestoreLastOpenedWindow);
        }

        void InitialFixedPanel()
        {
            string strDock = this.AppInfo.GetString(
                "MainForm",
                "fixedpanel_dock",
                "right");
            int nFixedHeight = this.AppInfo.GetInt(
                "MainForm",
                "fixedpanel_height",
                100);
            int nFixedWidth = this.AppInfo.GetInt(
                "MainForm",
                "fixedpanel_width",
                -1);
            // �״δ򿪴���
            if (nFixedWidth == -1)
                nFixedWidth = this.Width / 3;

            if (strDock == "bottom")
            {
                this.panel_fixed.Dock = DockStyle.Bottom;
                this.panel_fixed.Size = new Size(this.panel_fixed.Width,
                    nFixedHeight);
            }
            else if (strDock == "top")
            {
                this.panel_fixed.Dock = DockStyle.Top;
                this.panel_fixed.Size = new Size(this.panel_fixed.Width,
                    nFixedHeight);
            }
            else if (strDock == "left")
            {
                this.panel_fixed.Dock = DockStyle.Left;
                this.panel_fixed.Size = new Size(nFixedWidth,
                    this.panel_fixed.Size.Height);
            }
            else if (strDock == "right")
            {
                this.panel_fixed.Dock = DockStyle.Right;
                this.panel_fixed.Size = new Size(nFixedWidth,
                    this.panel_fixed.Size.Height);
            }

            this.splitter_fixed.Dock = this.panel_fixed.Dock;

            bool bHide = this.AppInfo.GetBoolean(
                "MainForm",
                "hide_fixed_panel",
                false);
            if (bHide == true)
            {
                /*
                this.panel_fixed.Visible = false;
                this.splitter_fixed.Visible = false;
                 * */
                this.PanelFixedVisible = false;
            }

            try
            {
                this.tabControl_panelFixed.SelectedIndex = this.AppInfo.GetInt(
                    "MainForm",
                    "active_fixed_panel_page",
                    0);
            }
            catch
            {
            }
        }

        void FinishFixedPanel()
        {
            string strDock = "right";
            if (this.panel_fixed.Dock == DockStyle.Bottom)
                strDock = "bottom";
            else if (this.panel_fixed.Dock == DockStyle.Left)
                strDock = "left";
            else if (this.panel_fixed.Dock == DockStyle.Right)
                strDock = "right";
            else if (this.panel_fixed.Dock == DockStyle.Top)
                strDock = "top";

            this.AppInfo.SetString(
                "MainForm",
                "fixedpanel_dock",
                strDock);
            this.AppInfo.SetInt(
                "MainForm",
                "fixedpanel_height",
                this.panel_fixed.Size.Height);
            this.AppInfo.SetInt(
                "MainForm",
                "fixedpanel_width",
                this.panel_fixed.Size.Width);

            this.AppInfo.SetInt(
    "MainForm",
    "active_fixed_panel_page",
    this.tabControl_panelFixed.SelectedIndex);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // DisableChildTopMost();

            // ��ǰ��ر�MDI�Ӵ��ڵ�ʱ���Ѿ���������ֹ�رյ����������Ͳ����ٴ�ѯ����
            if (e.CloseReason == CloseReason.UserClosing && e.Cancel == true)
                return;



            // if (e.CloseReason != CloseReason.ApplicationExitCall)
            if (e.CloseReason == CloseReason.UserClosing)   // 2014/8/13
            {
                if (this.Stop != null)
                {
                    if (this.Stop.State == 0)    // 0 ��ʾ���ڴ���
                    {
                        MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                        e.Cancel = true;
                        return;
                    }
                }

                // ����ر�
                DialogResult result = MessageBox.Show(this,
                    "ȷʵҪ�˳� dp2Circulation -- ����/��ͨ ? ",
                    "dp2Circulation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

#if NO
            // ����ر�ʱMDI���ڵ�Maximized״̬
            if (this.ActiveMdiChild != null)
                this.MdiWindowState = this.ActiveMdiChild.WindowState;
#endif
        }

#if NO
        void DisableChildTopMost()
        {
            foreach (Control form in this.Controls)
            {
                if (form.TopMost == true)
                {
                    form.TopMost = false;
                    form.WindowState = FormWindowState.Minimized;
                }
            }
        }
#endif

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (this._acceptForm != null)
            {
                try
                {
                    this._acceptForm.Close();
                    this._acceptForm = null;
                }
                catch
                {
                }
            }
#endif

            if (m_propertyViewer != null)
                m_propertyViewer.Close();

             AppInfo.SetString(
                "mainform",
                "current_camera",
                this.qrRecognitionControl1.CurrentCamera); 
            this.qrRecognitionControl1.Catched -= new DigitalPlatform.Drawing.CatchedEventHandler(qrRecognitionControl1_Catched);

            this.MdiClient.ClientSizeChanged -= new EventHandler(MdiClient_ClientSizeChanged);

            // this.timer_operHistory.Stop();

            if (this.OperHistory != null)
                this.OperHistory.Close();

            // ���洰�ڳߴ�״̬
            if (AppInfo != null)
            {

                string strOpenedMdiWindow = GuiUtil.GetOpenedMdiWindowString(this);
                this.AppInfo.SetString(
                    "main_form",
                    "last_opened_mdi_window",
                    strOpenedMdiWindow);

                FinishFixedPanel();

                AppInfo.SaveFormStates(this,
                    "mainformstate");
            }

            // cfgcache
            string strError;
            int nRet = cfgCache.Save(null, out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);


            // �������ڱ��������
            bool bSavePasswordLong =
    AppInfo.GetBoolean(
    "default_account",
    "savepassword_long",
    false);

            if (bSavePasswordLong == false)
            {
                AppInfo.SetString(
                    "default_account",
                    "password",
                    "");
            }

            if (this.m_bSavePinyinGcatID == false)
                this.m_strPinyinGcatID = "";
            this.AppInfo.SetString("entity_form", "gcat_pinyin_api_id", this.m_strPinyinGcatID);
            this.AppInfo.GetBoolean("entity_form", "gcat_pinyin_api_saveid", this.m_bSavePinyinGcatID);

            //��סsave,������ϢXML�ļ�
            AppInfo.Save();
            AppInfo = null;	// ������������������

            if (this.Channel != null)
                this.Channel.Close();   // TODO: �������һ��ʱ�䣬�������ʱ����Abort()

        }

#if NO
        // ����������library.xml��<libraryserver url="???">�����Ƿ�����
        // return:
        //      -1  error
        //      0   ����
        //      1   ������
        int CheckServerUrl(out string strError)
        {
            strError = "";

            // 2009/2/11 new add
            if (String.IsNullOrEmpty(this.LibraryServerUrl) == true)
                return 0;

            int nRet = this.LibraryServerUrl.ToLower().IndexOf("/dp2library");
            if (nRet == -1)
            {
                strError = "ǰ�������õ�ͼ���Ӧ�÷�����WebServiceUrl '" + this.LibraryServerUrl + "' ��ʽ���󣺽�βӦ��Ϊ/dp2library";
                return -1;
            }

            string strFirstDirectory = this.LibraryServerUrl.Substring(0, nRet).ToLower();

            if (String.IsNullOrEmpty(this.LibraryServerDiretory) == true)
            {
                // ���������Ǿɰ汾(û�д���<libraryserver url="???">������)���������˸���û����<libraryserver url="???">�������ͨ��ǰ�˻�ȡ��һ������ʱ��û�гɹ�
                return 0;
            }

            string strSecondDirectory = this.LibraryServerDiretory.ToLower();

            if (strFirstDirectory == strSecondDirectory)
                return 0;   // ���

            string strFirstUrl = strFirstDirectory + "/install_stamp.txt";
            string strSecondUrl = strSecondDirectory + "/install_stamp.txt";

            byte[] first_data = null;
            byte[] second_data = null;
            WebClient webClient = new WebClient();
            try
            {
                first_data = webClient.DownloadData(strFirstUrl);
            }
            catch (Exception ex)
            {
                strError = "����" + strFirstUrl + "�ļ��������� :" + ex.Message;
                return 0;   // �޷��жϣ�Ȩ�ҵ�������
            }

            string strSuggestion = "";

            try
            {
                Uri uri = new Uri(strFirstUrl);
                string strFirstHost = uri.Host.ToLower();
                if (strFirstHost != "localhost"
                    && strFirstHost != "127.0.0.1")
                {
                    strSuggestion = "\r\n\r\n���飺�޸�Ӧ�÷�����library.xml�����ļ���<libraryserver url='???'>���ã�???���ֿɲ���ֵ '" + strFirstDirectory + "'";
                }
            }
            catch
            {
            }

            try
            {
                second_data = webClient.DownloadData(strSecondUrl);
            }
            catch (Exception ex)
            {
                strError = "���棺����ͼ���Ӧ�÷���������Ŀ¼��library.xml�����ļ��У�<libraryserver url='???'>�����õ�URL '" + this.LibraryServerDiretory + "' �������µ��ļ�install_stamp.txt���м���Է��ʵ�ʱ����: " + ex.Message + "��������ò������ò�������" + strSuggestion;
                return 1;   // ������
            }

            if (ByteArray.Compare(first_data, second_data) != 0)
            {
                strError = "���棺����ͼ���Ӧ�÷���������Ŀ¼��library.xml�����ļ��У�<libraryserver url='???'>�����õ�URL '" + this.LibraryServerDiretory + "' ���м���Է��ʵ�ʱ�򣬷�������ǰ�������õķ����� URL '' ��ָ��Ĳ���ͬһ������Ŀ¼��" + strSuggestion;
                return 1;
            }

            return 0;
        }
#endif

        delegate void _RefreshCameraDevList();

        /// <summary>
        /// ˢ������ͷ�豸�б�
        /// </summary>
        void RefreshCameraDevList()
        {
            this.qrRecognitionControl1.RefreshDevList();
        }

        /// <summary>
        /// ����ȱʡ���̺���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == API.WM_DEVICECHANGE)
            {
                if (m.WParam.ToInt32() == API.DBT_DEVNODES_CHANGED)
                {
                    _RefreshCameraDevList d = new _RefreshCameraDevList(RefreshCameraDevList);
                    this.BeginInvoke(d);
                }
            }
            base.WndProc(ref m);
        }

#if SN
        void WriteSerialNumberStatusFile()
        {
            try
            {
                string strFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dp2circulation_status");
                if (File.Exists(strFileName) == true)
                    return;
                using (StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8))
                {
                    sw.Write(DateTimeUtil.DateTimeToString8(DateTime.Now));
                }

                File.SetAttributes(strFileName, FileAttributes.Hidden);
            }
            catch
            {
            }
        }

        bool IsExistsSerialNumberStatusFile()
        {
            try
            {
                string strFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dp2circulation_status");
                return File.Exists(strFileName);
            }
            catch
            {
            }
            return true;    // ��������쳣�������д��ļ�
        }

#endif

        // ��ʼ�����ֲ���
        bool InitialProperties(bool bFullInitial, bool bRestoreLastOpenedWindow)
        {
            int nRet = 0;

            // �Ƚ�ֹ����
            if (bFullInitial == true)
            {
                EnableControls(false);
                this.MdiClient.Enabled = false;
            }

            try
            {
                string strError = "";

                if (bFullInitial == true)
                {
                    // this.Logout(); 

#if NO
                                {
                                    FirstRunDialog first_dialog = new FirstRunDialog();
                                    MainForm.SetControlFont(first_dialog, this.DefaultFont);
                                    first_dialog.MainForm = this;
                                    first_dialog.StartPosition = FormStartPosition.CenterScreen;
                                    if (first_dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                                    {
                                        Application.Exit();
                                        return;
                                    }
                                }
#endif

                    bool bFirstDialog = false;

                    // �����Ҫ�����ȳ������û��棬��������dp2libraryws��URL
                    string strLibraryServerUrl = this.AppInfo.GetString(
                        "config",
                        "circulation_server_url",
                        "");
                    if (String.IsNullOrEmpty(strLibraryServerUrl) == true)
                    {
                        FirstRunDialog first_dialog = new FirstRunDialog();
                        MainForm.SetControlFont(first_dialog, this.DefaultFont);
                        first_dialog.MainForm = this;
                        first_dialog.StartPosition = FormStartPosition.CenterScreen;
                        if (first_dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                        {
                            Application.Exit();
                            return false;
                        }
                        bFirstDialog = true;

                        // �״�д�� ����ģʽ ��Ϣ
                        this.AppInfo.SetString("main_form", "last_mode", first_dialog.Mode);
                        if (first_dialog.Mode == "test")
                        {
                            this.AppInfo.SetString("sn", "sn", "test");
                            this.AppInfo.Save();
                        }
                    }

#if NO
                    // ������кš��������ʱ��Ҫ����ֲ�Ʒ����
                    // DateTime start_day = new DateTime(2014, 10, 15);    // 2014/10/15 �Ժ�ǿ���������кŹ���
                    // if (DateTime.Now >= start_day || IsExistsSerialNumberStatusFile() == true)
                    {
                        // ���û�Ŀ¼��д��һ�������ļ�����ʾ���кŹ����Ѿ�����
                        // WriteSerialNumberStatusFile();

                        nRet = this.VerifySerialCode("", true, out strError);
                        if (nRet == -1)
                        {
                            MessageBox.Show(this, "dp2Circulation ��Ҫ���������кŲ���ʹ��");
                            Application.Exit();
                            return false;
                        }
                    }
#endif

#if SN
                    {
                        _verified = false;
                        nRet = this.VerifySerialCode("", false, out strError);
                        if (nRet == 0)
                            _verified = true;

                    }
#else
                    this.MenuItem_resetSerialCode.Visible = false;
#endif

                    bool bLogin = this.AppInfo.GetBoolean(
                        "default_account",
                        "occur_per_start",
                        true);
                    if (bLogin == true
                        && bFirstDialog == false)   // �״����еĶԻ�����ֺ󣬵�¼�Ի���Ͳ��س�����
                    {
                        SetDefaultAccount(
                            null,
                            "��¼", // "ָ��ȱʡ�ʻ�",
                            "�״ε�¼", // "��ָ����������м����õ���ȱʡ�ʻ���Ϣ��",
                            LoginFailCondition.None,
                            this,
                            false);
                    }
                    else
                    {
                        // 2015/5/15
                        string strServerUrl =
AppInfo.GetString("config",
"circulation_server_url",
"http://localhost:8001/dp2library");

                        if (string.Compare(strServerUrl, CirculationLoginDlg.dp2LibraryXEServerUrl, true) == 0)
                            AutoStartDp2libraryXE();
                    }
                }

                nRet = PrepareSearch();
                if (nRet == 1)
                {
                    try
                    {

                        // 2013/6/18
                        nRet = TouchServer(false);
                        if (nRet == -1)
                            goto END1;

                        // ֻ����ǰһ��û�д��������²�̽��汾��
                        if (nRet == 0)
                        {
                            // ���dp2Library�汾��
                            // return:
                            //      -1  error
                            //      0   dp2Library�İ汾�Ź��͡�������Ϣ��strError��
                            //      1   dp2Library�汾�ŷ���Ҫ��
                            nRet = CheckVersion(false, out strError);
                            if (nRet == -1)
                            {
                                MessageBox.Show(this, strError);
                                goto END1;
                            }
                            if (nRet == 0)
                                MessageBox.Show(this, strError);
                        }

                        // �����Ŀ���ݿ�From��Ϣ
                        nRet = GetDbFromInfos(false);
                        if (nRet == -1)
                            goto END1;

                        // ���ȫ�����ݿ�Ķ���
                        nRet = GetAllDatabaseInfo(false);
                        if (nRet == -1)
                            goto END1;

                        // �����Ŀ�������б�
                        nRet = InitialBiblioDbProperties(false);
                        if (nRet == -1)
                            goto END1;

                        // ��ö��߿����б�
                        /*
                        nRet = GetReaderDbNames();
                        if (nRet == -1)
                            goto END1;
                         * */
                        nRet = InitialReaderDbProperties(false);
                        if (nRet == -1)
                            goto END1;

                        // ���ʵ�ÿ������б�
                        nRet = GetUtilDbProperties(false);
                        if (nRet == -1)
                            goto END1;

                        // 2008/11/29 new add
                        nRet = InitialNormalDbProperties(false);
                        if (nRet == -1)
                            goto END1;

                        // ���ͼ���һ����Ϣ
                        nRet = GetLibraryInfo(false);
                        if (nRet == -1)
                            goto END1;

                        // �����ȡ��������Ϣ
                        // 2009/2/24 new add
                        nRet = GetCallNumberInfo(false);
                        if (nRet == -1)
                            goto END1;

                        // ���ǰ�˽��ѽӿ�������Ϣ
                        // 2009/7/20 new add
                        nRet = GetClientFineInterfaceInfo(false);
                        if (nRet == -1)
                            goto END1;

                        // ��÷�����ӳ�䵽���ص������ļ�
                        nRet = GetServerMappedFile(false);
                        if (nRet == -1)
                            goto END1;


                        /*
                        // ����������library.xml��<libraryserver url="???">�����Ƿ�����
                        // return:
                        //      -1  error
                        //      0   ����
                        //      1   ������
                        nRet = CheckServerUrl(out strError);
                        if (nRet != 0)
                            MessageBox.Show(this, strError);
                         * */


                        // �˶Ա��غͷ�����ʱ��
                        // return:
                        //      -1  error
                        //      0   û������
                        //      1   ����ʱ�Ӻͷ�����ʱ��ƫ����󣬳���10���� strError���б�����Ϣ
                        nRet = CheckServerClock(false, out strError);
                        if (nRet != 0)
                            MessageBox.Show(this, strError);
                    }
                    finally
                    {
                        EndSearch();
                    }
                }

                // ��װ��������
                InstallBarcodeFont();

            END1:

                Stop = new DigitalPlatform.Stop();
                Stop.Register(stopManager, true);	// ����������
                Stop.SetMessage("����ɾ����ǰ��������ʱ�ļ�...");

                DeleteAllTempFiles(this.DataDir);
                DeleteAllTempFiles(this.UserTempDir);

                Stop.SetMessage("���ڸ��Ʊ��������ļ�...");
                // ����Ŀ¼
                nRet = PathUtil.CopyDirectory(Path.Combine(this.DataDir, "report_def"),
                    Path.Combine(this.UserDir, "report_def"),
                    false,
                    out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);

                Stop.SetMessage("");
                if (Stop != null) // �������
                {
                    Stop.Unregister();	// ����������
                    Stop = null;
                }

                // 2013/12/4
                if (InitialClientScript(out strError) == -1)
                    MessageBox.Show(this, strError);


                // ��ʼ����ʷ���󣬰���C#�ű�
                if (this.OperHistory == null)
                {
                    this.OperHistory = new OperHistory();
                    nRet = this.OperHistory.Initial(this,
                        this.webBrowser_history,
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(this, strError);

                    // this.timer_operHistory.Start();

                }

            }
            finally
            {
                // Ȼ����ɽ���
                if (bFullInitial == true)
                {
                    this.MdiClient.Enabled = true;
                    EnableControls(true);
                }

                if (this.m_backgroundForm != null)
                {
                    // TODO: ����и������Ĺ���
                    this.stopManager.OnDisplayMessage += new DisplayMessageEventHandler(stopManager_OnDisplayMessage);
                    this.MdiClient.SizeChanged -= new EventHandler(MdiClient_SizeChanged);
                    this.m_backgroundForm.Close();
                    this.m_backgroundForm = null;
                }
            }

            if (bRestoreLastOpenedWindow == true)
                RestoreLastOpenedMdiWindow();

            if (bFullInitial == true)
            {
#if NO
                // �ָ��ϴ������Ĵ���
                string strOpenedMdiWindow = this.AppInfo.GetString(
                    "main_form",
                    "last_opened_mdi_window",
                    "");

                RestoreLastOpenedMdiWindow(strOpenedMdiWindow);
#endif

                // ��ʼ��ָ�Ƹ��ٻ���
                FirstInitialFingerprintCache();

            }
            return true;
        }

        void RestoreLastOpenedMdiWindow()
        {
            // �ָ��ϴ������Ĵ���
            string strOpenedMdiWindow = this.AppInfo.GetString(
                "main_form",
                "last_opened_mdi_window",
                "");

            RestoreLastOpenedMdiWindow(strOpenedMdiWindow);
        }

        void RestoreLastOpenedMdiWindow(string strOpenedMdiWindow)
        {
            // ȱʡ��һ��Z search form
            if (String.IsNullOrEmpty(strOpenedMdiWindow) == true)
                strOpenedMdiWindow = "dp2Circulation.ChargingForm";

            string[] types = strOpenedMdiWindow.Split(new char[] { ',' });
            for (int i = 0; i < types.Length; i++)
            {
                string strType = types[i];
                if (String.IsNullOrEmpty(strType) == true)
                    continue;

                if (strType == "dp2Circulation.ChargingForm")
                    this.MenuItem_openChargingForm_Click(this, null);
                else if (strType == "dp2Circulation.QuickChargingForm")
                    this.MenuItem_openQuickChargingForm_Click(this, null);
                else if (strType == "dp2Circulation.BiblioSearchForm")
                    this.MenuItem_openBiblioSearchForm_Click(this, null);
                else if (strType == "dp2Circulation.ReaderSearchForm")
                    this.MenuItem_openReaderSearchForm_Click(this, null);
                else if (strType == "dp2Circulation.ItemSearchForm")
                    this.MenuItem_openItemSearchForm_Click(this, null);
                else if (strType == "dp2Circulation.IssueSearchForm")
                    this.MenuItem_openIssueSearchForm_Click(this, null);
                else if (strType == "dp2Circulation.OrderSearchForm")
                    this.MenuItem_openOrderSearchForm_Click(this, null);
                else if (strType == "dp2Circulation.CommentSearchForm")
                    this.MenuItem_openCommentSearchForm_Click(this, null);
                else
                    continue;
            }

            // װ��MDI�Ӵ���״̬
            this.AppInfo.LoadFormMdiChildStates(this,
                "mainformstate");
        }

        #region �˵�����

        // �¿���־ͳ�ƴ�
        private void MenuItem_openOperLogStatisForm_Click(object sender, EventArgs e)
        {
#if NO
            OperLogStatisForm form = new OperLogStatisForm();

            // form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<OperLogStatisForm>();
        }


        private void ToolStripMenuItem_openReportForm_Click(object sender, EventArgs e)
        {
#if NO
            ReportForm form = new ReportForm();
            form.MdiParent = this;
            form.Show();
#endif

#if NO
            string strError = "";
            int nRet = this.VerifySerialCode("report", out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, "������Ҫ���������кŲ���ʹ��");
                return;
            }
#endif

            OpenWindow<ReportForm>();

        }

        // �¿�����ͳ�ƴ�
        private void MenuItem_openReaderStatisForm_Click(object sender, EventArgs e)
        {
#if NO
            ReaderStatisForm form = new ReaderStatisForm();

            // form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<ReaderStatisForm>();

        }

        // �¿���ͳ�ƴ�
        private void MenuItem_openItemStatisForm_Click(object sender, EventArgs e)
        {
#if NO
            ItemStatisForm form = new ItemStatisForm();

            // form.MainForm = this;
            // form.DbType = "item";
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<ItemStatisForm>();

        }

        // �¿���Ŀͳ�ƴ�
        private void MenuItem_openBiblioStatisForm_Click(object sender, EventArgs e)
        {
#if NO
            BiblioStatisForm form = new BiblioStatisForm();

            // form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<BiblioStatisForm>();

        }

        private void MenuItem_openXmlStatisForm_Click(object sender, EventArgs e)
        {
#if NO
            XmlStatisForm form = new XmlStatisForm();

            // form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<XmlStatisForm>();

        }

        private void MenuItem_openIso2709StatisForm_Click(object sender, EventArgs e)
        {
#if NO
            Iso2709StatisForm form = new Iso2709StatisForm();

            // form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<Iso2709StatisForm>();

        }

        // �¿���ݵǼǴ�
        private void MenuItem_openPassGateForm_Click(object sender, EventArgs e)
        {
#if NO
            PassGateForm form = new PassGateForm();
            // form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<PassGateForm>();

        }

        // �¿����㴰
        private void MenuItem_openSettlementForm_Click(object sender, EventArgs e)
        {
#if NO
            SettlementForm form = new SettlementForm();

            form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<SettlementForm>();

        }

        // �¿����ߴ�
        private void MenuItem_openReaderInfoForm_Click(object sender, EventArgs e)
        {
#if NO
            ReaderInfoForm form = new ReaderInfoForm();

            form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<ReaderInfoForm>();
        }

        // �¿����ɴ�ӡ����
        private void MenuItem_openChargingPrintManageForm_Click(object sender, EventArgs e)
        {
#if NO
            ChargingPrintManageForm form = new ChargingPrintManageForm();

            form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<ChargingPrintManageForm>();
        }

        // �й�MDI�Ӵ������еĲ˵�����
        private void MenuItem_mdi_arrange_Click(object sender, System.EventArgs e)
        {
            // ƽ�� ˮƽ��ʽ
            if (sender == MenuItem_tileHorizontal)
                this.LayoutMdi(MdiLayout.TileHorizontal);

            if (sender == MenuItem_tileVertical)
                this.LayoutMdi(MdiLayout.TileVertical);

            if (sender == MenuItem_cascade)
                this.LayoutMdi(MdiLayout.Cascade);

            if (sender == MenuItem_arrangeIcons)
                this.LayoutMdi(MdiLayout.ArrangeIcons);

        }

        // �¿����ɴ�
        private void MenuItem_openChargingForm_Click(object sender, EventArgs e)
        {
#if NO
            ChargingForm form = new ChargingForm();

            form.MdiParent = this;

            form.MainForm = this;

            form.Show();
#endif
            OpenWindow<ChargingForm>();

        }

        private void MenuItem_openQuickChargingForm_Click(object sender, EventArgs e)
        {
#if NO
            QuickChargingForm form = new QuickChargingForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif

            OpenWindow<QuickChargingForm>();
        }

        // �¿����߲�ѯ��
        private void MenuItem_openReaderSearchForm_Click(object sender, EventArgs e)
        {
#if NO
            ReaderSearchForm form = new ReaderSearchForm();

            form.MdiParent = this;

            // form.MainForm = this;

            form.Show();
#endif
            OpenWindow<ReaderSearchForm>();
        }

        void OpenWindow<T>()
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                T form = Activator.CreateInstance<T>();
                dynamic o = form;
                o.MdiParent = this;

                if (o.MainForm == null)
                {
                    try
                    {
                        o.MainForm = this;
                    }
                    catch
                    {
                        // �Ƚ������д������͵� MainForm ����ֻ�����Ժ����޸�����
                    }
                }
                o.Show();
            }
            else
                EnsureChildForm<T>(true);
        }

        // �¿�ʵ���ѯ��
        private void MenuItem_openItemSearchForm_Click(object sender, EventArgs e)
        {
#if NO
            ItemSearchForm form = new ItemSearchForm();
            form.DbType = "item";

            form.MdiParent = this;

            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<ItemSearchForm>();
        }

        private void MenuItem_openOrderSearchForm_Click(object sender, EventArgs e)
        {
#if NO
            OrderSearchForm form = new OrderSearchForm();
            // form.DbType = "order";

            form.MdiParent = this;

            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<OrderSearchForm>();
        }

        private void MenuItem_openIssueSearchForm_Click(object sender, EventArgs e)
        {
#if NO
            IssueSearchForm form = new IssueSearchForm();
            // form.DbType = "issue";

            form.MdiParent = this;

            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<IssueSearchForm>();

        }

        private void MenuItem_openCommentSearchForm_Click(object sender, EventArgs e)
        {
#if NO
            CommentSearchForm form = new CommentSearchForm();
            // form.DbType = "comment";

            form.MdiParent = this;

            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<CommentSearchForm>();
        }

        /// <summary>
        /// ��һ��ʵ��/����/��/��ע��ѯ��
        /// </summary>
        /// <param name="strDbType">���ݿ����͡�Ϊ item/order/issue/comment ֮һ</param>
        /// <returns>�´򿪵Ĵ���</returns>
        public ItemSearchForm OpenItemSearchForm(string strDbType)
        {
            ItemSearchForm form = null;

            if (strDbType == "item")
                form = new ItemSearchForm();
            else if (strDbType == "order")
                form = new OrderSearchForm();
            else if (strDbType == "issue")
                form = new IssueSearchForm();
            else if (strDbType == "comment")
                form = new CommentSearchForm();
            else
                form = new ItemSearchForm();

            form.DbType = strDbType;
            form.MdiParent = this;
            form.Show();

            return form;
        }

        private void MenuItem_openInvoiceSearchForm_Click(object sender, EventArgs e)
        {
#if NO
            InvoiceSearchForm form = new InvoiceSearchForm();
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<InvoiceSearchForm>();

        }

        // �¿�ʵ�崰
        private void MenuItem_openItemInfoForm_Click(object sender, EventArgs e)
        {
#if NO
            ItemInfoForm form = new ItemInfoForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<ItemInfoForm>();

        }

        // �¿�ʱ�Ӵ�
        private void MenuItem_openClockForm_Click(object sender, EventArgs e)
        {
#if NO
            ClockForm form = new ClockForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<ClockForm>();

        }

        // ϵͳ��������
        private void MenuItem_configuration_Click(object sender, EventArgs e)
        {
            _expireVersionChecked = false;

            string strOldDefaultFontString = this.DefaultFontString;

            CfgDlg dlg = new CfgDlg();

            dlg.ParamChanged += new ParamChangedEventHandler(CfgDlg_ParamChanged);
            dlg.ap = this.AppInfo;
            dlg.MainForm = this;

            dlg.UiState = this.AppInfo.GetString(
                    "main_form",
                    "cfgdlg_uiState",
                    ""); 
            this.AppInfo.LinkFormState(dlg,
                "cfgdlg_state");
            dlg.ShowDialog(this);
            this.AppInfo.UnlinkFormState(dlg);
            this.AppInfo.SetString(
                    "main_form",
                    "cfgdlg_uiState",
                    dlg.UiState);

            dlg.ParamChanged -= new ParamChangedEventHandler(CfgDlg_ParamChanged);

            // ȱʡ���巢���˱仯
            if (strOldDefaultFontString != this.DefaultFontString)
            {
                Size oldsize = this.Size;

                MainForm.SetControlFont(this, this.DefaultFont, true);

                /*
                if (this.WindowState == FormWindowState.Normal)
                    this.Size = oldsize;
                 * */

                foreach (Form child in this.MdiChildren)
                {
                    oldsize = child.Size;

                    MainForm.SetControlFont(child, this.DefaultFont, true);

                    // child.Size = oldsize;
                }
            }

        }

        void CfgDlg_ParamChanged(object sender, ParamChangedEventArgs e)
        {
            if (e.Section == "charging_form"
                && e.Entry == "no_biblio_and_item_info")
            {
                // ������ǰ�򿪵�����chargingform
                List<Form> forms = GetChildWindows(typeof(ChargingForm));
                foreach (Form child in forms)
                {
                    ChargingForm chargingform = (ChargingForm)child;

                    chargingform.ClearItemAndBiblioControl();
                    chargingform.ChangeLayout((bool)e.Value);
                }
            }

        }

        // �˳�
        private void MenuItem_exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // �¿��޸����봰
        private void MenuItem_openChangePasswordForm_Click(object sender, EventArgs e)
        {
#if NO
            ChangePasswordForm form = new ChangePasswordForm();
            form.MdiParent = this;
            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<ChangePasswordForm>();

        }

        private void MenuItem_openAmerceForm_Click(object sender, EventArgs e)
        {
#if NO
            AmerceForm form = new AmerceForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<AmerceForm>();

        }

        private void MenuItem_openReaderManageForm_Click(object sender, EventArgs e)
        {
#if NO
            ReaderManageForm form = new ReaderManageForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<ReaderManageForm>();
        }

        private void MenuItem_openBiblioSearchForm_Click(object sender, EventArgs e)
        {
#if NO
            BiblioSearchForm form = new BiblioSearchForm();
            form.MdiParent = this;
            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<BiblioSearchForm>();
        }

        private void MenuItem_openEntityForm_Click(object sender, EventArgs e)
        {
#if NO
            EntityForm form = new EntityForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<EntityForm>();

        }

        // ���޸Ĳ�
        private void MenuItem_openQuickChangeEntityForm_Click(object sender, EventArgs e)
        {
#if NO
            QuickChangeEntityForm form = new QuickChangeEntityForm();
            form.MdiParent = this;
            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<QuickChangeEntityForm>();

        }

        // ���޸���Ŀ
        private void MenuItem_openQuickChangeBiblioForm_Click(object sender, EventArgs e)
        {
#if NO
            QuickChangeBiblioForm form = new QuickChangeBiblioForm();
            form.MdiParent = this;
            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<QuickChangeBiblioForm>();

        }

        private void MenuItem_openOperLogForm_Click(object sender, EventArgs e)
        {
#if NO
            OperLogForm form = new OperLogForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<OperLogForm>();

        }

        private void MenuItem_openCalendarForm_Click(object sender, EventArgs e)
        {
#if NO
            CalendarForm form = new CalendarForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<CalendarForm>();
        }

        private void MenuItem_openBatchTaskForm_Click(object sender, EventArgs e)
        {
#if NO
            BatchTaskForm form = new BatchTaskForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<BatchTaskForm>();

        }

        private void MenuItem_openManagerForm_Click(object sender, EventArgs e)
        {
#if NO
            ManagerForm form = new ManagerForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<ManagerForm>();

        }

        private void MenuItem_openUserForm_Click(object sender, EventArgs e)
        {
#if NO
            UserForm form = new UserForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<UserForm>();

        }

        private void MenuItem_channelForm_Click(object sender, EventArgs e)
        {
#if NO
            ChannelForm form = new ChannelForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<ChannelForm>();

        }

        private void MenuItem_openActivateForm_Click(object sender, EventArgs e)
        {
#if NO
            ActivateForm form = new ActivateForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<ActivateForm>();

        }

        private void MenuItem_openTestForm_Click(object sender, EventArgs e)
        {
#if NO
            TestForm form = new TestForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<TestForm>();

        }

        // ���ִκŴ�
        private void MenuItem_openZhongcihaoForm_Click(object sender, EventArgs e)
        {
#if NO
            ZhongcihaoForm form = new ZhongcihaoForm();
            form.MdiParent = this;
            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<ZhongcihaoForm>();

        }

        // ����ȡ�Ŵ�
        private void MenuItem_openCallNumberForm_Click(object sender, EventArgs e)
        {
#if NO
            CallNumberForm form = new CallNumberForm();
            form.MdiParent = this;
            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<CallNumberForm>();

        }

        private void MenuItem_openUrgentChargingForm_Click(object sender, EventArgs e)
        {
#if NO
            UrgentChargingForm form = new UrgentChargingForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<UrgentChargingForm>();

        }

        // Ӧ���ָ�
        private void MenuItem_recoverUrgentLog_Click(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild is UrgentChargingForm)
            {
                ((UrgentChargingForm)this.ActiveMdiChild).Recover();
            }
        }

        // �ǳ�
        private void MenuItem_logout_Click(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild is EntityForm)
            {
                ((EntityForm)this.ActiveMdiChild).Logout();
            }
        }

        // ������Ŀ¼�ļ���
        private void MenuItem_openDataFolder_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(this.DataDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        // �򿪳���Ŀ¼�ļ���
        private void MenuItem_openProgramFolder_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Environment.CurrentDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void MenuItem_operCheckBorrowInfoForm_Click(object sender, EventArgs e)
        {
#if NO
            CheckBorrowInfoForm form = new CheckBorrowInfoForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<CheckBorrowInfoForm>();

        }

        // ��ؽ��� hand over and take over
        private void MenuItem_handover_Click(object sender, EventArgs e)
        {
#if NO
            ItemHandoverForm form = new ItemHandoverForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();
#endif
            OpenWindow<ItemHandoverForm>();

        }

        // �ɹ� ��ӡ����
        private void MenuItem_printOrder_Click(object sender, EventArgs e)
        {
#if NO
            PrintOrderForm form = new PrintOrderForm();
            form.MdiParent = this;
            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<PrintOrderForm>();

        }

        // �˵� ��ӡ���յ�
        private void MenuItem_printAccept_Click(object sender, EventArgs e)
        {
#if NO
            PrintAcceptForm form = new PrintAcceptForm();
            form.MdiParent = this;
            // form.MainForm = this;
            form.Show();
#endif
            OpenWindow<PrintAcceptForm>();


        }

        // ��ӡ��ѯ��
        private void MenuItem_printClaim_Click(object sender, EventArgs e)
        {
#if NO
            PrintClaimForm form = new PrintClaimForm();
            form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<PrintClaimForm>();

        }

        // ��ӡ�Ʋ���
        private void MenuItem_printAccountBook_Click(object sender, EventArgs e)
        {
#if NO
            AccountBookForm form = new AccountBookForm();
            // form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<AccountBookForm>();

        }

        // ��ӡװ����
        private void MenuItem_printBindingList_Click(object sender, EventArgs e)
        {
#if NO
            PrintBindingForm form = new PrintBindingForm();
            form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<PrintBindingForm>();

        }

#if NO
        MdiClient GetMdiClient()
        {
            Type t = typeof(Form);
            PropertyInfo pi = t.GetProperty("MdiClient", BindingFlags.Instance | BindingFlags.NonPublic);
            return (MdiClient)pi.GetValue(this, null);
        }
#endif

#if !ACCEPT_MODE
        AcceptForm _acceptForm = null;
#endif

        private void MenuItem_accept_Click(object sender, EventArgs e)
        {
            #if ACCEPT_MODE

            AcceptForm top = this.GetTopChildWindow<AcceptForm>();
            if (top != null)
            {
                top.Activate();
                return;
            }

            AcceptForm form = new AcceptForm();

            form.MdiParent = this;

            form.MainForm = this;
            form.Show();

            // form.WaitLoadFinish();

            // API.PostMessage(this.Handle, WM_REFRESH_MDICLIENT, 0, 0);

            {
                this.MdiClient.Invalidate();
                this.MdiClient.Update();

                // TODO: Invalidate ȫ���򿪵�MDI�Ӵ���
                for (int i = 0; i < this.MdiChildren.Length; i++)
                {
                    Global.InvalidateAllControls(this.MdiChildren[i]);
                }
            }
#else
            if (_acceptForm == null || _acceptForm.IsDisposed == true)
            {
                _acceptForm = new AcceptForm();
                _acceptForm.MainForm = this;
                _acceptForm.FormClosed -= new FormClosedEventHandler(accept_FormClosed);
                _acceptForm.FormClosed += new FormClosedEventHandler(accept_FormClosed);

                this.AppInfo.LinkFormState(_acceptForm, "acceptform_state");

                _acceptForm.Show(this);
            }
            else
            {
                _acceptForm.ActivateFirstPage();
            }

            if (Control.ModifierKeys == Keys.Control)
            {
                if (_acceptForm.Visible == false)
                {
                    _acceptForm.DoFloating();
                    // _acceptForm.Show(this);
                }
            }
            else
            {
                if (this.CurrentPropertyControl != _acceptForm.MainControl)
                    _acceptForm.DoDock(true); // �Զ���ʾFixedPanel
            }

#endif
        }

        void accept_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_acceptForm != null)
            {
                this.AppInfo.UnlinkFormState(_acceptForm);
                this._acceptForm = null;
            }
        }

        // ��������ļ����ػ���
        private void MenuItem_clearCfgCache_Click(object sender, EventArgs e)
        {
            cfgCache.ClearCfgCache();

            this.AssemblyCache.Clear(); // ˳��Ҳ���Assembly����
            this.DomCache.Clear();
        }

        // �����ĿժҪ���ػ���
        private void MenuItem_clearSummaryCache_Click(object sender, EventArgs e)
        {
            this.SummaryCache.RemoveAll();
        }

        // ��������
        private void MenuItem_font_Click(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild is EntityForm)
            {
                ((EntityForm)this.ActiveMdiChild).SetFont();
            }
            else if (this.ActiveMdiChild is MyForm)
            {
                ((MyForm)this.ActiveMdiChild).SetBaseFont();
            }
        }

        // �ָ�Ϊȱʡ����
        private void MenuItem_restoreDefaultFont_Click(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild is EntityForm)
            {
                ((EntityForm)this.ActiveMdiChild).RestoreDefaultFont();
            }
            else if (this.ActiveMdiChild is MyForm)
            {
                ((MyForm)this.ActiveMdiChild).RestoreDefaultFont();
            }
        }

        #endregion

        private void toolButton_stop_Click(object sender, EventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
                stopManager.DoStopAll(null);    // 2012/3/25
            else
                stopManager.DoStopActive();
        }

        private void MainForm_MdiChildActivate(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild == null)
            {
                SetMenuItemState();
            }
        }

        internal void SetMenuItemState()
        {
            // �˵�
 
            // ��������ť
            this.ToolStripMenuItem_loadReaderInfo.Enabled = true;
            this.ToolStripMenuItem_loadItemInfo.Enabled = true;
            this.ToolStripMenuItem_autoLoadItemOrReader.Enabled = true;

            this.MenuItem_recoverUrgentLog.Enabled = false;
            this.MenuItem_font.Enabled = false;
            this.MenuItem_restoreDefaultFont.Enabled = false;
            this.MenuItem_logout.Enabled = false;
        }

#if NO

        // ��ǰ�����ReaderInfoForm
        public ReaderInfoForm TopReaderInfoForm
        {
            get
            {
                return (ReaderInfoForm)GetTopChildWindow(typeof(ReaderInfoForm));
            }

        }

        // ��ǰ�����AcceptForm
        public AcceptForm TopAcceptForm
        {
            get
            {
                return GetTopChildWindow<AcceptForm>();
            }
        }


        public ActivateForm TopActivateForm
        {
            get
            {
                return (ActivateForm)GetTopChildWindow(typeof(ActivateForm));
            }
        }

        public EntityForm TopEntityForm
        {
            get
            {
                return (EntityForm)GetTopChildWindow(typeof(EntityForm));
            }
        }

        public DupForm TopDupForm
        {
            get
            {
                return (DupForm)GetTopChildWindow(typeof(DupForm));
            }
        }

        // ��ǰ�����ItemInfoForm
        public ItemInfoForm TopItemInfoForm
        {
            get
            {
                return (ItemInfoForm)GetTopChildWindow(typeof(ItemInfoForm));
            }
        }

        public UtilityForm TopUtilityForm
        {
            get
            {
                return (UtilityForm)GetTopChildWindow(typeof(UtilityForm));
            }
        }

        // ��ǰ�����ChargingForm
        public ChargingForm TopChargingForm
        {
            get
            {
                return (ChargingForm)GetTopChildWindow(typeof(ChargingForm));
            }
        }

        // ��ǰ�����ChargingForm
        public UrgentChargingForm TopUrgentChargingForm
        {
            get
            {
                return (UrgentChargingForm)GetTopChildWindow(typeof(UrgentChargingForm));
            }
        }

        // ��ǰ�����HtmlPrintForm
        public HtmlPrintForm TopHtmlPrintForm
        {
            get
            {
                return (HtmlPrintForm)GetTopChildWindow(typeof(HtmlPrintForm));
            }
        }


        // ��ǰ�����ChargingPrintManageForm
        public ChargingPrintManageForm TopChargingPrintManageForm
        {
            get
            {
                return (ChargingPrintManageForm)GetTopChildWindow(typeof(ChargingPrintManageForm));
            }
        }

        // ��ǰ�����AmerceForm
        public AmerceForm TopAmerceForm
        {
            get
            {
                return (AmerceForm)GetTopChildWindow(typeof(AmerceForm));
            }
        }

        // ��ǰ�����ReaderManageForm
        public ReaderManageForm TopReaderManageForm
        {
            get
            {
                return (ReaderManageForm)GetTopChildWindow(typeof(ReaderManageForm));
            }
        }

        // ��ǰ�����PrintAcceptForm
        public PrintAcceptForm TopPrintAcceptForm
        {
            get
            {
                return (PrintAcceptForm)GetTopChildWindow(typeof(PrintAcceptForm));
            }
        }

        // ��ǰ�����LabelPrintForm
        public LabelPrintForm TopLabelPrintForm
        {
            get
            {
                return (LabelPrintForm)GetTopChildWindow(typeof(LabelPrintForm));
            }
        }

        // ��ǰ�����CardPrintForm
        public CardPrintForm TopCardPrintForm
        {
            get
            {
                return (CardPrintForm)GetTopChildWindow(typeof(CardPrintForm));
            }
        }

        // ��ǰ�����BiblioStatisForm
        public BiblioStatisForm TopBiblioStatisForm
        {
            get
            {
                return (BiblioStatisForm)GetTopChildWindow(typeof(BiblioStatisForm));
            }
        }

#endif

        // �õ��ض����͵�MDI����
        List<Form> GetChildWindows(Type type)
        {
            List<Form> results = new List<Form>();

            foreach(Form child in this.MdiChildren)
            {
                if (child.GetType().Equals(type) == true)
                    results.Add(child);
            }

            return results;
        }

        /// <summary>
        /// ���һ���Ѿ��򿪵� MDI �Ӵ��ڣ����û�У����´�һ��
        /// </summary>
        /// <typeparam name="T">�Ӵ�������</typeparam>
        /// <returns>�Ӵ��ڶ���</returns>
        public T EnsureChildForm<T>(bool bActivate = false)
        {
            T form = GetTopChildWindow<T>();
            if (form == null)
            {
                form = Activator.CreateInstance<T>();
                dynamic o = form;
                o.MdiParent = this;

                // 2013/3/26
                if (o.MainForm == null)
                {
                    try
                    {
                        o.MainForm = this;
                    }
                    catch
                    {
                        // �Ƚ������д������͵� MainForm ����ֻ�����Ժ����޸�����
                    }
                }
                o.Show();
            }
            else
            {
                if (bActivate == true)
                {
                    try
                    {
                        dynamic o = form;
                        o.Activate();

                        if (o.WindowState == FormWindowState.Minimized)
                            o.WindowState = FormWindowState.Normal;
                    }
                    catch
                    {
                    }
                }
            }
            return form;
        }

        // 
        /// <summary>
        /// �õ��ض����͵Ķ��� MDI �Ӵ���
        /// </summary>
        /// <typeparam name="T">�Ӵ�������</typeparam>
        /// <returns>�Ӵ��ڶ���</returns>
        public T GetTopChildWindow<T>()
        {
            if (ActiveMdiChild == null)
                return default(T);

            // �õ������MDI Child
            IntPtr hwnd = this.ActiveMdiChild.Handle;

            if (hwnd == IntPtr.Zero)
                return default(T);

            while (hwnd != IntPtr.Zero)
            {
                Form child = null;
                // �ж�һ�����ھ�����Ƿ�Ϊ MDI �Ӵ��ڣ�
                // return:
                //      null    ���� MDI �Ӵ���o
                //      ����      ������������Ӧ�� Form ����
                child = IsChildHwnd(hwnd);
                if (child != null)
                {
                    // if (child is T)
                    if (child.GetType().Equals(typeof(T)) == true)
                    {
                        try
                        {
                            return (T)Convert.ChangeType(child, typeof(T));
                        }
                        catch (InvalidCastException ex)
                        {
                            throw new InvalidCastException("�ڽ����� '" + child.GetType().ToString() + "' ת��Ϊ���� '" + typeof(T).ToString() + "' �Ĺ����г����쳣: " + ex.Message, ex);
                        }
                    }
                }

                hwnd = API.GetWindow(hwnd, API.GW_HWNDNEXT);
            }

            return default(T);
        }

        // �ж�һ�����ھ�����Ƿ�Ϊ MDI �Ӵ��ڣ�
        // return:
        //      null    ���� MDI �Ӵ���o
        //      ����      ������������Ӧ�� Form ����
        Form IsChildHwnd(IntPtr hwnd)
        {
            foreach (Form child in this.MdiChildren)
            {
                if (hwnd == child.Handle)
                    return child;
            }

            return null;
        }

        // �õ��ض����͵Ķ���MDI����
        Form GetTopChildWindow(Type type)
        {
            if (ActiveMdiChild == null)
                return null;

            // �õ������MDI Child
            IntPtr hwnd = this.ActiveMdiChild.Handle;

            if (hwnd == IntPtr.Zero)
                return null;

            for (; ; )
            {
                if (hwnd == IntPtr.Zero)
                    break;

                Form child = null;
                for (int j = 0; j < this.MdiChildren.Length; j++)
                {
                    if (hwnd == this.MdiChildren[j].Handle)
                    {
                        child = this.MdiChildren[j];
                        goto FOUND;
                    }
                }

                goto CONTINUE;
            FOUND:

                if (child.GetType().Equals(type) == true)
                    return child;

            CONTINUE:
                hwnd = API.GetWindow(hwnd, API.GW_HWNDNEXT);
            }

            return null;
        }

        // �Ƿ�ΪMDI�Ӵ���������2��֮һ?
        // 2008/9/8
        internal bool IsTopTwoChildWindow(Form form)
        {
            if (ActiveMdiChild == null)
                return false;

            // �õ������MDI Child
            IntPtr hwnd = this.ActiveMdiChild.Handle;

            if (hwnd == IntPtr.Zero)
                return false;

            for (int i = 0; i < 2; i++)
            {
                if (hwnd == IntPtr.Zero)
                    break;

                if (hwnd == form.Handle)
                {
                    return true;
                }

                hwnd = API.GetWindow(hwnd, API.GW_HWNDNEXT);
            }

            return false;
        }

        // ��������װ�ض��߼�¼
        private void ToolStripMenuItem_loadReaderInfo_Click(object sender, EventArgs e)
        {
            this.ToolStripMenuItem_loadReaderInfo.Checked = true;
            this.ToolStripMenuItem_loadItemInfo.Checked = false;
            this.ToolStripMenuItem_autoLoadItemOrReader.Checked = false;
            this.toolStripButton_loadBarcode.Text = "����";
        }

        // ��������װ��ʵ���¼
        private void ToolStripMenuItem_loadItemInfo_Click(object sender, EventArgs e)
        {
            this.ToolStripMenuItem_loadReaderInfo.Checked = false;
            this.ToolStripMenuItem_loadItemInfo.Checked = true;
            this.ToolStripMenuItem_autoLoadItemOrReader.Checked = false;
            this.toolStripButton_loadBarcode.Text = "��";
        }

        private void ToolStripMenuItem_autoLoadItemOrReader_Click(object sender, EventArgs e)
        {
            this.ToolStripMenuItem_loadReaderInfo.Checked = false;
            this.ToolStripMenuItem_loadItemInfo.Checked = false;
            this.ToolStripMenuItem_autoLoadItemOrReader.Checked = true;
            this.toolStripButton_loadBarcode.Text = "�Զ�";
        }

        bool _expireVersionChecked = false;

        internal void Channel_BeforeLogin(object sender,
            DigitalPlatform.CirculationClient.BeforeLoginEventArgs e)
        {
#if SN
            if (_expireVersionChecked == false)
            {
                double base_version = 2.36;
                string strExpire = GetExpireParam();
                if (string.IsNullOrEmpty(strExpire) == false
                    && this.Version < base_version
                    && this.Version != 0)
                {
                    string strError = "����ʧЧ���кŲ����� dp2Circulation ��Ҫ�� dp2Library " + base_version + " �����ϰ汾����ʹ�� (����ǰ dp2Library �汾��Ϊ " + this.Version.ToString() + " )��\r\n\r\n������ dp2Library �����°汾��Ȼ���������� dp2Circulation��\r\n\r\n�㡰ȷ������ť�˳�";
                    MessageBox.Show(strError);
                    Application.Exit();
                    return;
                }
                _expireVersionChecked = true;
            }
#endif

            if (e.FirstTry == true)
            {
                e.UserName = AppInfo.GetString(
                    "default_account",
                    "username",
                    "");
                e.Password = AppInfo.GetString(
                    "default_account",
                    "password",
                    "");
                e.Password = this.DecryptPasssword(e.Password);

                bool bIsReader =
                    AppInfo.GetBoolean(
                    "default_account",
                    "isreader",
                    false);

                string strLocation = AppInfo.GetString(
                "default_account",
                "location",
                "");
                e.Parameters = "location=" + strLocation;
                if (bIsReader == true)
                    e.Parameters += ",type=reader";

                // 2014/9/13
                e.Parameters += ",mac=" + StringUtil.MakePathList(SerialCodeForm.GetMacAddress(), "|");

#if SN
                // �����к��л�� expire= ����ֵ
                string strExpire = GetExpireParam();
                if (string.IsNullOrEmpty(strExpire) == false)
                    e.Parameters += ",expire=" + strExpire;
#endif

                // 2014/10/23
                if (this.TestMode == true)
                    e.Parameters += ",testmode=true";

                if (String.IsNullOrEmpty(e.UserName) == false)
                    return; // ��������, �Ա�����һ�� ������ �Ի�����Զ���¼
            }

            // 
            IWin32Window owner = null;

            if (sender is IWin32Window)
                owner = (IWin32Window)sender;
            else
                owner = this;

            CirculationLoginDlg dlg = SetDefaultAccount(
                e.LibraryServerUrl,
                null,
                e.ErrorInfo,
                e.LoginFailCondition,
                owner);
            if (dlg == null)
            {
                e.Cancel = true;
                return;
            }


            e.UserName = dlg.UserName;
            e.Password = dlg.Password;
            e.SavePasswordShort = dlg.SavePasswordShort;
            e.Parameters = "location=" + dlg.OperLocation;
            if (dlg.IsReader == true)
                e.Parameters += ",type=reader";

            // 2014/9/13
            e.Parameters += ",mac=" + StringUtil.MakePathList(SerialCodeForm.GetMacAddress(), "|");

#if SN
            // �����к��л�� expire= ����ֵ
            {
                string strExpire = GetExpireParam();
                if (string.IsNullOrEmpty(strExpire) == false)
                    e.Parameters += ",expire=" + strExpire;
            }
#endif

            // 2014/10/23
            if (this.TestMode == true)
                e.Parameters += ",testmode=true";

            e.SavePasswordLong = dlg.SavePasswordLong;
            if (e.LibraryServerUrl != dlg.ServerUrl)
            {
                e.LibraryServerUrl = dlg.ServerUrl;
                _expireVersionChecked = false;
            }
        }


        internal void Channel_AfterLogin(object sender, AfterLoginEventArgs e)
        {
#if SN
            LibraryChannel channel = sender as LibraryChannel;
            if (_verified == false && StringUtil.IsInList("serverlicensed", channel.Rights) == false)
            {
                string strError = "";
                int nRet = this.VerifySerialCode("", true, out strError);
                if (nRet == -1)
                {
                    channel.Close();
                    MessageBox.Show(this, "dp2Circulation ��Ҫ���������кŲ���ʹ��");
                    Application.Exit();
                    return;
                }
            }
            _verified = true;
#endif
        }

#if SN
        bool _verified = false;

        // �����к��л�� expire= ����ֵ
        // ����ֵΪ MAC ��ַ���б��м�ָ��� '|'
        internal string GetExpireParam()
        {
            string strSerialCode = this.AppInfo.GetString("sn", "sn", "");
            if (string.IsNullOrEmpty(strSerialCode) == false)
            {
                string strExtParam = GetExtParams(strSerialCode);
                if (string.IsNullOrEmpty(strExtParam) == false)
                {
                    Hashtable ext_table = StringUtil.ParseParameters(strExtParam);
                    return (string)ext_table["expire"];
                }
            }

            return "";
        }

#endif

        /// <summary>
        /// ���ȱʡ�û���
        /// </summary>
        public string DefaultUserName
        {
            get
            {
                return AppInfo.GetString(
                "default_account",
                "username",
                "");
            }
        }


        // return:
        //      0   û��׼���ɹ�
        //      1   ׼���ɹ�
        /// <summary>
        /// ׼�����м���
        /// </summary>
        /// <param name="bActivateStop">�Ƿ񼤻� Stop</param>
        /// <returns>0: û�гɹ�; 1: �ɹ�</returns>
        public int PrepareSearch(bool bActivateStop = true)
        {
            if (String.IsNullOrEmpty(this.LibraryServerUrl) == true)
                return 0;

            this.Channel.Url = this.LibraryServerUrl;

            this.Channel.BeforeLogin -= new DigitalPlatform.CirculationClient.BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new DigitalPlatform.CirculationClient.BeforeLoginEventHandle(Channel_BeforeLogin);

            this.Channel.AfterLogin -= new AfterLoginEventHandle(Channel_AfterLogin);
            this.Channel.AfterLogin += new AfterLoginEventHandle(Channel_AfterLogin);

            Stop = new DigitalPlatform.Stop();
            Stop.Register(stopManager, bActivateStop);	// ����������

            return 1;
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <returns>���� 0</returns>
        public int EndSearch()
        {
            if (Stop != null) // �������
            {
                Stop.Unregister();	// ����������
                Stop = null;
            }

            return 0;
        }

        // �ǳ�
        /// <summary>
        /// MainForm �ǳ�
        /// </summary>
        public void Logout()
        {
            int nRet = PrepareSearch();
            if (nRet == 0)
            {
                return;  // 2009/2/11
            }

            // this.Update();

            string strError = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڵǳ� ...");
            Stop.BeginLoop();

            try
            {
                // string strValue = "";
                long lRet = Channel.Logout(out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " �ǳ�ʱ��������" + strError;
                    goto ERROR1;
                }
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                EndSearch();
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// ��̽�Ӵ�һ�·�����
        /// </summary>
        /// <param name="bPrepareSearch">�Ƿ�Ҫ׼��ͨ��</param>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int TouchServer(bool bPrepareSearch = true)
        {
        REDO:
            if (bPrepareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return -1;
            }

            string strError = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("�������ӷ����� "+Channel.Url+" ...");
            Stop.BeginLoop();

            try
            {
                string strTime = "";
                long lRet = Channel.GetClock(Stop,
                    out strTime,
                    out strError);
                if (lRet == -1)
                {
                    if (this.Channel.WcfException is System.ServiceModel.Security.MessageSecurityException)
                    {
                        // ͨѶ��ȫ�����⣬ʱ������
                        strError = strError + "\r\n\r\n�п�����ǰ�˻���ʱ�Ӻͷ�����ʱ�Ӳ��������ɵ�";
                        goto ERROR1;
                    }
                }
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 0;
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
        }

        // 0 ��ʾ2.1���¡�2.1������ʱ�ž��еĻ�ȡ�汾�Ź���
        /// <summary>
        /// ��ǰ���ӵ� dp2Library �汾��
        /// </summary>
        [System.ComponentModel.DefaultValue(0)]
        public double Version {get;set;}

        // return:
        //      -1  error
        //      0   dp2Library�İ汾�Ź��͡�������Ϣ��strError��
        //      1   dp2Library�汾�ŷ���Ҫ��
        /// <summary>
        /// ��� dp2Library �汾��
        /// </summary>
        /// <param name="bPrepareSearch">�Ƿ�Ҫ׼��ͨ��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: dp2Library�İ汾�Ź��͡�������Ϣ��strError��; 1: dp2Library�汾�ŷ���Ҫ��</returns>
        public int CheckVersion(
            bool bPrepareSearch,
            out string strError)
        {
            strError = "";

            if (bPrepareSearch == true)
            {
                int nRet = PrepareSearch();
                if (nRet == 0)
                {
                    strError = "PrepareSearch() error";
                    return -1;
                }
            }

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڼ��汾��, ���Ժ� ...");
            Stop.BeginLoop();

            try
            {
                string strVersion = "";
                long lRet = Channel.GetVersion(Stop,
    out strVersion,
    out strError);
                if (lRet == -1)
                {
                    if (this.Channel.WcfException is System.ServiceModel.Security.MessageSecurityException)
                    {
                        // ԭ����dp2Library���߱�GetVersion() API�����ߵ�����
                        this.Version = 0;
                        strError = "��ǰ dp2Circulation �汾��Ҫ�� dp2Library 2.1 �����ϰ汾����ʹ�� (����ǰ dp2Library �汾��Ϊ '2.0������' )�������� dp2Library �����°汾��";
                        return 0;
                    }

                    strError = "��Է����� " + Channel.Url + " ��ð汾�ŵĹ��̷�������" + strError;
                    return -1;
                }


                double value = 0;

                if (string.IsNullOrEmpty(strVersion) == true)
                {
                    strVersion = "2.0����";
                    value = 2.0;
                }
                else
                {
                    // �����Ͱ汾��
                    if (double.TryParse(strVersion, out value) == false)
                    {
                        strError = "dp2Library �汾�� '" + strVersion + "' ��ʽ����ȷ";
                        return -1;
                    }
                }

                this.Version = value;

                double base_version = 2.33;
                if (value < base_version)   // 2.12
                {
                    // strError = "��ǰ dp2Circulation �汾��Ҫ�� dp2Library " + base_version + " �����ϰ汾����ʹ�� (����ǰ dp2Library �汾��Ϊ " + strVersion + " )��\r\n\r\n�뾡������ dp2Library �����°汾��";
                    // return 0;
                    strError = "��ǰ dp2Circulation �汾����� dp2Library " + base_version + " �����ϰ汾����ʹ�� (����ǰ dp2Library �汾��Ϊ " + strVersion + " )��\r\n\r\n���������� dp2Library �����°汾��";
                    this.AppInfo.Save();
                    MessageBox.Show(this, strError);
                    Application.Exit();
                    return -1;
                }

#if SN
                if (this.TestMode == true && this.Version < 2.34)
                {
                    strError = "dp2Circulation ������ģʽֻ���������ӵ� dp2library �汾Ϊ 2.34 ����ʱ����ʹ�� (��ǰ dp2library �汾Ϊ "+this.Version.ToString()+")";
                    this.AppInfo.Save();
                    MessageBox.Show(this, strError);
                    DialogResult result = MessageBox.Show(this,
    "�������кſ�����������ģʽ��\r\n\r\n�Ƿ�Ҫ���˳�ǰ�������к�?",
    "dp2Circulation",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                    if (result == System.Windows.Forms.DialogResult.No)
                    {
                        Application.Exit();
                        return -1;
                    }
                    else
                    {
                        MenuItem_resetSerialCode_Click(this, new EventArgs());
                        Application.Exit();
                        return -1;
                    }
                }
#endif
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 1;
        }

        // 
        // return:
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        /// <summary>
        /// ��� ��Ŀ��/���߿� ��(����)����;��
        /// </summary>
        /// <param name="bPrepareSearch">�Ƿ�Ҫ׼��ͨ��</param>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int GetDbFromInfos(bool bPrepareSearch = true)
        {
            REDO:
            if (bPrepareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return -1;
            }

            // TODO: �ں�����Ϊ�޷����Channel������ǰ���Ƿ�Ҫ�����صļ���;�����ݽṹ?
            // this.Update();
            string strError = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("�����м���;�� ...");
            Stop.BeginLoop();

            try
            {
                // �����Ŀ��ļ���;��
                BiblioDbFromInfo[] infos = null;

                long lRet = Channel.ListDbFroms(Stop,
                    "biblio",
                    this.Lang,
                    out infos,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " �г���Ŀ�����;�����̷�������" + strError;
                    goto ERROR1;
                }

                this.BiblioDbFromInfos = infos;

                // ��ö��߿�ļ���;��
                infos = null;
                lRet = Channel.ListDbFroms(Stop,
    "reader",
    this.Lang,
    out infos,
    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " �г����߿����;�����̷�������" + strError;
                    goto ERROR1;
                }

                if (infos != null && this.BiblioDbFromInfos != null
                    && infos.Length > 0 && this.BiblioDbFromInfos.Length > 0
                    && infos[0].Caption == this.BiblioDbFromInfos[0].Caption)
                {
                    // �����һ��Ԫ�ص�captionһ������˵��GetDbFroms API�Ǿɰ汾�ģ���֧�ֻ�ȡ���߿�ļ���;������
                    this.ReaderDbFromInfos = null;
                }
                else
                {
                    this.ReaderDbFromInfos = infos;
                }

                if (this.Version >= 2.11)
                {
                    // ���ʵ���ļ���;��
                    infos = null;
                    lRet = Channel.ListDbFroms(Stop,
        "item",
        this.Lang,
        out infos,
        out strError);
                    if (lRet == -1)
                    {
                        strError = "��Է����� " + Channel.Url + " �г�ʵ������;�����̷�������" + strError;
                        goto ERROR1;
                    }
                    this.ItemDbFromInfos = infos;

                    // ����ڿ�ļ���;��
                    infos = null;
                    lRet = Channel.ListDbFroms(Stop,
        "issue",
        this.Lang,
        out infos,
        out strError);
                    if (lRet == -1)
                    {
                        strError = "��Է����� " + Channel.Url + " �г��ڿ����;�����̷�������" + strError;
                        goto ERROR1;
                    }
                    this.IssueDbFromInfos = infos;

                    // ��ö�����ļ���;��
                    infos = null;
                    lRet = Channel.ListDbFroms(Stop,
        "order",
        this.Lang,
        out infos,
        out strError);
                    if (lRet == -1)
                    {
                        strError = "��Է����� " + Channel.Url + " �г����������;�����̷�������" + strError;
                        goto ERROR1;
                    }
                    this.OrderDbFromInfos = infos;

                    // �����ע��ļ���;��
                    infos = null;
                    lRet = Channel.ListDbFroms(Stop,
        "comment",
        this.Lang,
        out infos,
        out strError);
                    if (lRet == -1)
                    {
                        strError = "��Է����� " + Channel.Url + " �г���ע�����;�����̷�������" + strError;
                        goto ERROR1;
                    }
                    this.CommentDbFromInfos = infos;
                }

                if (this.Version >= 2.17)
                {
                    // ��÷�Ʊ��ļ���;��
                    infos = null;
                    lRet = Channel.ListDbFroms(Stop,
        "invoice",
        this.Lang,
        out infos,
        out strError);
                    if (lRet == -1)
                    {
                        strError = "��Է����� " + Channel.Url + " �г���Ʊ�����;�����̷�������" + strError;
                        goto ERROR1;
                    }

                    this.InvoiceDbFromInfos = infos;

                    // ���ΥԼ���ļ���;��
                    infos = null;
                    lRet = Channel.ListDbFroms(Stop,
        "amerce",
        this.Lang,
        out infos,
        out strError);
                    if (lRet == -1)
                    {
                        strError = "��Է����� " + Channel.Url + " �г�ΥԼ������;�����̷�������" + strError;
                        goto ERROR1;
                    }

                    this.AmerceDbFromInfos = infos;

                }

                // ��Ҫ���һ��Caption�Ƿ����ظ�(����style��ͬ)�ģ�����У���Ҫ�޸�Caption��
                this.CanonicalizeBiblioFromValues();

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 0;
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
        }

        // ÿ���ļ���һ������ֻ�����ļ�ʱ��, ����������޸ĺ��ȫ������ļ�����
        // return:
        //      -1  ����
        //      0   �����Ѿ����ļ�����������޸�ʱ��ͷ�������һ�£���˲������»����
        //      1   ������ļ�����
        int GetSystemFile(string strFileNameParam,
            out string strError)
        {
            strError = "";

            string strFileName = "";
            string strLastTime = "";
            StringUtil.ParseTwoPart(strFileNameParam,
                "|",
                out strFileName,
                out strLastTime);

            Stream stream = null;
            try
            {
                string strServerMappedPath = PathUtil.MergePath(this.DataDir, "servermapped");
                string strLocalFilePath = PathUtil.MergePath(strServerMappedPath, strFileName);
                PathUtil.CreateDirIfNeed(PathUtil.PathPart(strLocalFilePath));

                // �۲챾���Ƿ�������ļ�������޸�ʱ���Ƿ�ͷ������Ǻ�
                if (File.Exists(strLocalFilePath) == true)
                {
                    FileInfo fi = new FileInfo(strLocalFilePath);
                    DateTime local_file_time = fi.LastWriteTimeUtc;

                    if (string.IsNullOrEmpty(strLastTime) == true)
                    {
                        Stop.SetMessage("���ڻ�ȡϵͳ�ļ� " + strFileName + " ������޸�ʱ�� ...");

                        byte[] baContent = null;
                        long lRet = Channel.GetFile(
        Stop,
        "cfgs",
        strFileName,
        -1, // lStart,
        0,  // lLength,
        out baContent,
        out strLastTime,
        out strError);
                        if (lRet == -1)
                            return -1;
                    }

                    if (string.IsNullOrEmpty(strLastTime) == true)
                    {
                        strError = "strLastTime ��Ӧ��Ϊ��";
                        return -1;
                    }
                    Debug.Assert(string.IsNullOrEmpty(strLastTime) == false, "");

                    DateTime remote_file_time = DateTimeUtil.FromRfc1123DateTimeString(strLastTime);
                    if (local_file_time == remote_file_time)
                        return 0;   // �����ٴλ��������
                }

            REDO:
                Stop.SetMessage("��������ϵͳ�ļ� " + strFileName + " ...");

                string strPrevFileTime = "";
                long lStart = 0;
                long lLength = -1;
                for (; ; )
                {
                    byte[] baContent = null;
                    string strFileTime = "";
                    // ���ϵͳ�����ļ�
                    // parameters:
                    //      strCategory �ļ����ࡣĿǰֻ��ʹ�� cfgs
                    //      lStart  ��Ҫ����ļ����ݵ���㡣���Ϊ-1����ʾ(baContent��)�������ļ�����
                    //      lLength ��Ҫ��õĴ�lStart��ʼ�����byte�������Ϊ-1����ʾϣ�������ܶ��ȡ��(���ǲ��ܱ�֤һ����β)
                    // rights:
                    //      ��Ҫ getsystemparameter Ȩ��
                    // return:
                    //      result.Value    -1 �������� �ļ����ܳ���
                    long lRet = Channel.GetFile(
                        Stop,
                        "cfgs",
                        strFileName,
                        lStart,
                        lLength,
                        out baContent,
                        out strFileTime,
                        out strError);
                    if (lRet == -1)
                        return -1;

                    if (stream == null)
                    {
                        stream = File.Open(
    strLocalFilePath,
    FileMode.Create,
    FileAccess.ReadWrite,
    FileShare.ReadWrite);
                    }

                    // ��;�ļ�ʱ�䱻�޸���
                    if (string.IsNullOrEmpty(strPrevFileTime) == false
                        && strFileTime != strPrevFileTime)
                    {
                        goto REDO;  // ��������
                    }

                    if (lRet == 0)
                        return 0;   // �ļ�����Ϊ0

                    stream.Write(baContent, 0, baContent.Length);
                    lStart += baContent.Length;

                    strPrevFileTime = strFileTime;

                    if (lStart >= lRet)
                        break;  // �����ļ��Ѿ��������
                }

                stream.Close();
                stream = null;

                // �޸ı����ļ�ʱ��
                {
                    FileInfo fi = new FileInfo(strLocalFilePath);
                    fi.LastWriteTimeUtc = DateTimeUtil.FromRfc1123DateTimeString(strPrevFileTime);
                }
                return 1;   // �ӷ��������������
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
        }

        // ɾ��ָ��Ŀ¼�£���֪�ļ�����������ļ�
        /// <summary>
        /// ɾ��ָ��Ŀ¼�£���֪�ļ�����������ļ�
        /// </summary>
        /// <param name="strSourceDir">Ŀ¼·��</param>
        /// <param name="exclude_filenames">Ҫ�ų����ļ����б�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public static int RemoveFiles(string strSourceDir,
            List<string> exclude_filenames,
            out string strError)
        {
            strError = "";

            try
            {

                DirectoryInfo di = new DirectoryInfo(strSourceDir);

                if (di.Exists == false)
                {
                    strError = "ԴĿ¼ '" + strSourceDir + "' ������...";
                    return -1;
                }

                FileSystemInfo[] subs = di.GetFileSystemInfos();

                for (int i = 0; i < subs.Length; i++)
                {
                    if ((subs[i].Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        int nRet = RemoveFiles(subs[i].FullName,
                            exclude_filenames,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        continue;
                    }

                    string strFileName = subs[i].FullName.ToLower();

                    if (exclude_filenames.IndexOf(strFileName) == -1)
                    {
                        try
                        {
                            File.Delete(strFileName);
                        }
                        catch
                        {
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            return 0;
        }

        // 
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        /// <summary>
        /// ���ͼ���һ����Ϣ
        /// </summary>
        /// <param name="bPrepareSearch">�Ƿ�Ҫ׼��ͨ��</param>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int GetServerMappedFile(bool bPrepareSearch = true)
        {
        REDO:
            if (bPrepareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return -1;
            }

            // this.Update();

            string strError = "";
            int nRet = 0;

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ�ôӷ�����ӳ�䵽���ص������ļ� ...");
            Stop.BeginLoop();

            try
            {
                string strServerMappedPath = PathUtil.MergePath(this.DataDir, "servermapped");
                List<string> fullnames = new List<string>();

                string strValue = "";
                long lRet = Channel.GetSystemParameter(Stop,
                    "cfgs",
                    this.Version >= 2.23 ? "listFileNamesEx" : "listFileNames",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " ���ӳ�������ļ������̷�������" + strError;
                    goto ERROR1;
                }
                if (lRet == 0)
                    goto DELETE_FILES;

                string[] filenames = null;
                
                if (this.Version >= 2.23)
                    filenames = strValue.Replace("||", "?").Split(new char[] { '?' });
                else
                    filenames = strValue.Split(new char[] { ',' });
                foreach (string filename in filenames)
                {
                    if (string.IsNullOrEmpty(filename) == true)
                        continue;

                    nRet = GetSystemFile(filename,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    string strFileName = "";
                    string strLastTime = "";
                    StringUtil.ParseTwoPart(filename,
                        "|",
                        out strFileName,
                        out strLastTime);
                    fullnames.Add(Path.Combine(strServerMappedPath, strFileName).ToLower());
                }

                DELETE_FILES:
                // ɾ��û���õ����ļ�
                nRet = RemoveFiles(strServerMappedPath,
                    fullnames,
                    out strError);
                if (nRet == -1)
                    return -1;

            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 0;
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
        }

        // 
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        /// <summary>
        /// ���ͼ���һ����Ϣ
        /// </summary>
        /// <param name="bPrepareSearch">�Ƿ�Ҫ׼��ͨ��</param>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int GetLibraryInfo(bool bPrepareSearch = true)
        {
            REDO:
            if (bPrepareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return -1;
            }

            // this.Update();

            string strError = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ��ͼ���һ����Ϣ ...");
            Stop.BeginLoop();

            try
            {
                string strValue = "";
                long lRet = Channel.GetSystemParameter(Stop,
                    "library",
                    "name",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " ���ͼ���һ����Ϣlibrary/name���̷�������" + strError;
                    goto ERROR1;
                }

                this.LibraryName = strValue;

                /*
                lRet = Channel.GetSystemParameter(Stop,
                    "library",
                    "serverDirectory",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " ���ͼ���һ����Ϣlibrary/serverDirectory���̷�������" + strError;
                    goto ERROR1;
                }

                this.LibraryServerDiretory = strValue;
                 * */
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 0;
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
        }

        // ��������ļ�
        // ���õ� cfgCache
        // return:
        //      -1  �������û���ҵ�
        //      1   �ҵ�
        /// <summary>
        /// ��������ļ����õ��������ļ�����
        /// </summary>
        /// <param name="Channel">ͨѶͨ��</param>
        /// <param name="stop">ֹͣ����</param>
        /// <param name="strDbName">���ݿ���</param>
        /// <param name="strCfgFileName">�����ļ���</param>
        /// <param name="remote_timestamp">Զ��ʱ��������Ϊ null����ʾҪ�ӷ�����ʵ�ʻ�ȡʱ���</param>
        /// <param name="strContent">���������ļ�����</param>
        /// <param name="baOutputTimestamp">���������ļ�ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: �����û���ҵ�; 1: �ҵ�</returns>
        public int GetCfgFile(
            LibraryChannel Channel,
            Stop stop,
            string strDbName,
            string strCfgFileName,
            byte [] remote_timestamp,
            out string strContent,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";
            strContent = "";

            /*
            if (m_nInGetCfgFile > 0)
            {
                strError = "GetCfgFile() ������";
                return -1;
            }*/

            /*
            if (stop != null)
            {
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("�������������ļ� ...");
                stop.BeginLoop();
            }*/

            // m_nInGetCfgFile++;

            try
            {
                string strPath = strDbName + "/cfgs/" + strCfgFileName;

                stop.SetMessage("�������������ļ� " + strPath + " ...");
                string strMetaData = "";
                string strOutputPath = "";

                string strStyle = "content,data,metadata,timestamp,outputpath";

                long lRet = Channel.GetRes(stop,
                    this.cfgCache,
                    strPath,
                    strStyle,
                    remote_timestamp,
                    out strContent,
                    out strMetaData,
                    out baOutputTimestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                /*
                if (stop != null)
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }*/

                // m_nInGetCfgFile--;
            }

            return 1;
        ERROR1:
            return -1;
        }

        // 
        // 
        // return:
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        /// <summary>
        /// �����ͨ�������б���Ҫ���������Ŀ����
        /// ������InitialBiblioDbProperties()��InitialReaderDbProperties()�Ժ����
        /// </summary>
        /// <param name="bPrepareSearch">�Ƿ�Ҫ׼��ͨ��</param>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int InitialNormalDbProperties(bool bPrepareSearch)
        {
        REDO:
            if (bPrepareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return -1;
            }

            // this.Update();

            string strError = "";
            int nRet = 0;

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ����ͨ�������б� ...");
            Stop.BeginLoop();

            try
            {
                this.NormalDbProperties = new List<NormalDbProperty>();

                // ����NormalDbProperties����
                if (this.BiblioDbProperties != null)
                {
                    for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                    {
                        BiblioDbProperty biblio = this.BiblioDbProperties[i];

                        NormalDbProperty normal = null;

                        if (String.IsNullOrEmpty(biblio.DbName) == false)
                        {
                            normal = new NormalDbProperty();
                            normal.DbName = biblio.DbName;
                            this.NormalDbProperties.Add(normal);
                        }

                        if (String.IsNullOrEmpty(biblio.ItemDbName) == false)
                        {
                            normal = new NormalDbProperty();
                            normal.DbName = biblio.ItemDbName;
                            this.NormalDbProperties.Add(normal);
                        }

                        // Ϊʲô��ǰҪע�͵�?
                        if (String.IsNullOrEmpty(biblio.OrderDbName) == false)
                        {
                            normal = new NormalDbProperty();
                            normal.DbName = biblio.OrderDbName;
                            this.NormalDbProperties.Add(normal);
                        }

                        if (String.IsNullOrEmpty(biblio.IssueDbName) == false)
                        {
                            normal = new NormalDbProperty();
                            normal.DbName = biblio.IssueDbName;
                            this.NormalDbProperties.Add(normal);
                        }

                        if (String.IsNullOrEmpty(biblio.CommentDbName) == false)
                        {
                            normal = new NormalDbProperty();
                            normal.DbName = biblio.CommentDbName;
                            this.NormalDbProperties.Add(normal);
                        }


                    }
                }

                if (this.ReaderDbNames != null)
                {
                    for (int i = 0; i < this.ReaderDbNames.Length; i++)
                    {
                        string strDbName = this.ReaderDbNames[i];

                        if (String.IsNullOrEmpty(strDbName) == true)
                            continue;

                        NormalDbProperty normal = null;

                        normal = new NormalDbProperty();
                        normal.DbName = strDbName;
                        this.NormalDbProperties.Add(normal);
                    }
                }

                if (this.Version >= 2.23)
                {
                    // �����ļ����б�
                    List<string> filenames = new List<string>();
                    for (int i = 0; i < this.NormalDbProperties.Count; i++)
                    {
                        NormalDbProperty normal = this.NormalDbProperties[i];
                        filenames.Add(normal.DbName + "/cfgs/browse");
                    }

                    // �Ȼ��ʱ���
                    // TODO: ����ļ�̫����Է�����ȡ
                    string strValue = "";
                    long lRet = Channel.GetSystemParameter(Stop,
                        "cfgs/get_res_timestamps",
                        StringUtil.MakePathList(filenames),
                        out strValue,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "��Է����� " + Channel.Url + " ��� browse �����ļ�ʱ����Ĺ��̷�������" + strError;
                        goto ERROR1;
                    }

                    // ����ʱ����б�
                    Hashtable table = new Hashtable();
                    List<string> results = StringUtil.SplitList(strValue, ',');
                    foreach (string s in results)
                    {
                        string strFileName = "";
                        string strTimestamp = "";

                        StringUtil.ParseTwoPart(s, "|", out strFileName, out strTimestamp);
                        if (string.IsNullOrEmpty(strTimestamp) == true)
                            continue;
                        table[strFileName] = strTimestamp;
                    }

                    // ��������ļ�������
                    for (int i = 0; i < this.NormalDbProperties.Count; i++)
                    {
                        NormalDbProperty normal = this.NormalDbProperties[i];

                        normal.ColumnProperties = new ColumnPropertyCollection();

                        string strFileName = normal.DbName + "/cfgs/browse";
                        string strTimestamp = (string)table[strFileName];

                        string strContent = "";
                        byte[] baCfgOutputTimestamp = null;
                        nRet = GetCfgFile(
                            Channel,
                            Stop,
                            normal.DbName,
                            "browse",
                            ByteArray.GetTimeStampByteArray(strTimestamp),
                            out strContent,
                            out baCfgOutputTimestamp,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(strContent);
                        }
                        catch (Exception ex)
                        {
                            strError = "���ݿ� " + normal.DbName + " �� browse �����ļ�����װ��XMLDOMʱ����: " + ex.Message;
                            goto ERROR1;
                        }

                        XmlNodeList nodes = dom.DocumentElement.SelectNodes("//col");
                        foreach (XmlNode node in nodes)
                        {
                            string strColumnType = DomUtil.GetAttr(node, "type");

                            // 2013/10/23
                            string strColumnTitle = dp2ResTree.GetColumnTitle(node,
                                this.Lang);

                            normal.ColumnProperties.Add(strColumnTitle, strColumnType);
                        }
                    }

                }
                else
                {
                    // TODO: �Ƿ񻺴���Щ�����ļ�? 
                    // ��� browse �����ļ�
                    for (int i = 0; i < this.NormalDbProperties.Count; i++)
                    {
                        NormalDbProperty normal = this.NormalDbProperties[i];

                        normal.ColumnProperties = new ColumnPropertyCollection();

                        string strContent = "";
                        byte[] baCfgOutputTimestamp = null;
                        nRet = GetCfgFile(
                            Channel,
                            Stop,
                            normal.DbName,
                            "browse",
                            null,
                            out strContent,
                            out baCfgOutputTimestamp,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(strContent);
                        }
                        catch (Exception ex)
                        {
                            strError = "���ݿ� " + normal.DbName + " �� browse �����ļ�����װ��XMLDOMʱ����: " + ex.Message;
                            goto ERROR1;
                        }

                        XmlNodeList nodes = dom.DocumentElement.SelectNodes("//col");
                        foreach (XmlNode node in nodes)
                        {
                            string strColumnType = DomUtil.GetAttr(node, "type");

                            // 2013/10/23
                            string strColumnTitle = dp2ResTree.GetColumnTitle(node,
                                this.Lang);

                            normal.ColumnProperties.Add(strColumnTitle, strColumnType);
                        }
                    }
                }
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 0;
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
        }

#if NO
        // ��� col Ԫ�ص� title ����ֵ������������������ص� title Ԫ��ֵ
        /*
<col>
	<title>
		<caption lang='zh-CN'>����</caption>
		<caption lang='en'>Title</caption>
	</title>
         * */
        static string GetColumnTitle(XmlNode nodeCol, 
            string strLang = "zh")
        {
            string strColumnTitle = DomUtil.GetAttr(nodeCol, "title");
            if (string.IsNullOrEmpty(strColumnTitle) == false)
                return strColumnTitle;
            XmlNode nodeTitle = nodeCol.SelectSingleNode("title");
            if (nodeTitle == null)
                return "";
            return DomUtil.GetCaption(strLang, nodeTitle);
        }
#endif

        /// <summary>
        /// ���»��ȫ�����ݿⶨ��
        /// </summary>
        public void ReloadDatabasesInfo()
        {
            GetAllDatabaseInfo();
            InitialReaderDbProperties();
            GetUtilDbProperties();
        }

        /// <summary>
        /// ��ʾ��ǰȫ�����ݿ���Ϣ�� XmlDocument ����
        /// </summary>
        public XmlDocument AllDatabaseDom = null;

        /// <summary>
        /// ��ȡȫ�����ݿⶨ��
        /// </summary>
        /// <param name="bPrepareSearch">�Ƿ�Ҫ׼��ͨ��</param>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int GetAllDatabaseInfo(bool bPrepareSearch = true)
        {
        REDO:
            if (bPrepareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return -1;
            }

            string strError = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ��ȫ�����ݿⶨ�� ...");
            Stop.BeginLoop();

            try
            {
                string strValue = "";
                long lRet = 0;

                this.AllDatabaseDom = null;

                lRet = Channel.ManageDatabase(
    Stop,
    "getinfo",
    "",
    "",
    out strValue,
    out strError);
                if (lRet == -1)
                {
                    if (this.Channel.ErrorCode == ErrorCode.AccessDenied)
                    {
                    }

                    strError = "��Է����� " + Channel.Url + " ���ȫ�����ݿⶨ����̷�������" + strError;
                    goto ERROR1;
                }

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strValue);
                }
                catch (Exception ex)
                {
                    strError = "XMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                this.AllDatabaseDom = dom;

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
            }
            finally
            {
                if (Stop != null)
                {
                    Stop.EndLoop();
                    Stop.OnStop -= new StopEventHandler(this.DoStop);
                    Stop.Initial("");
                }

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 0;
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
        }

        // 
        // return:
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        /// <summary>
        /// ��ñ�Ŀ�������б�
        /// </summary>
        /// <param name="bPrepareSearch">�Ƿ�Ҫ׼��ͨ��</param>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int InitialBiblioDbProperties(bool bPrepareSearch = true)
        {
        REDO:
            if (bPrepareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return -1;
            }

            // this.Update();

            string strError = "";
            int nRet = 0;

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڳ�ʼ����Ŀ�������б� ...");
            Stop.BeginLoop();

            try
            {
                this.BiblioDbProperties = new List<BiblioDbProperty>();
                if (this.AllDatabaseDom == null)
                    return 0;

                XmlNodeList nodes = this.AllDatabaseDom.DocumentElement.SelectNodes("database[@type='biblio']");
                foreach (XmlNode node in nodes)
                {

                    string strName = DomUtil.GetAttr(node, "name");
                    string strType = DomUtil.GetAttr(node, "type");
                    // string strRole = DomUtil.GetAttr(node, "role");
                    // string strLibraryCode = DomUtil.GetAttr(node, "libraryCode");

                    BiblioDbProperty property = new BiblioDbProperty();
                    this.BiblioDbProperties.Add(property);
                    property.DbName = DomUtil.GetAttr(node, "name");
                    property.ItemDbName = DomUtil.GetAttr(node, "entityDbName");
                    property.Syntax = DomUtil.GetAttr(node, "syntax");
                    property.IssueDbName = DomUtil.GetAttr(node, "issueDbName");
                    property.OrderDbName = DomUtil.GetAttr(node, "orderDbName");
                    property.CommentDbName = DomUtil.GetAttr(node, "commentDbName");
                    property.Role = DomUtil.GetAttr(node, "role");

                    bool bValue = true;
                    nRet = DomUtil.GetBooleanParam(node,
                        "inCirculation",
                        true,
                        out bValue,
                        out strError);
                    property.InCirculation = bValue;
                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 0;
#if NO
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
#endif
        }


#if NO
        // 
        // return:
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        /// <summary>
        /// ��ñ�Ŀ�������б�
        /// </summary>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int InitialBiblioDbProperties()
        {
        REDO:
            int nRet = PrepareSearch();
            if (nRet == 0)
                return -1;

            // this.Update();

            string strError = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ����Ŀ�������б� ...");
            Stop.BeginLoop();

            try
            {
                string strValue = "";
                long lRet = 0;

                this.BiblioDbProperties = new List<BiblioDbProperty>();


                // ���÷���һ���Ի��ȫ������
                lRet = Channel.GetSystemParameter(Stop,
                    "system",
                    "biblioDbGroup",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " �����Ŀ����Ϣ���̷�������" + strError;
                    goto ERROR1;
                }

                if (String.IsNullOrEmpty(strValue) == true)
                {
                    // �����þɷ���

                    lRet = Channel.GetSystemParameter(Stop,
                        "biblio",
                        "dbnames",
                        out strValue,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "��Է����� " + Channel.Url + " ��ñ�Ŀ�����б���̷�������" + strError;
                        goto ERROR1;
                    }

                    string[] biblioDbNames = strValue.Split(new char[] { ',' });

                    for (int i = 0; i < biblioDbNames.Length; i++)
                    {
                        BiblioDbProperty property = new BiblioDbProperty();
                        property.DbName = biblioDbNames[i];
                        this.BiblioDbProperties.Add(property);
                    }


                    // ����﷨��ʽ
                    lRet = Channel.GetSystemParameter(Stop,
                        "biblio",
                        "syntaxs",
                        out strValue,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "��Է����� " + Channel.Url + " ��ñ�Ŀ�����ݸ�ʽ�б���̷�������" + strError;
                        goto ERROR1;
                    }

                    string[] syntaxs = strValue.Split(new char[] { ',' });

                    if (syntaxs.Length != this.BiblioDbProperties.Count)
                    {
                        strError = "��Է����� " + Channel.Url + " ��ñ�Ŀ����Ϊ " + this.BiblioDbProperties.Count.ToString() + " ���������ݸ�ʽΪ " + syntaxs.Length.ToString() + " ����������һ��";
                        goto ERROR1;
                    }

                    // �������ݸ�ʽ
                    for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                    {
                        this.BiblioDbProperties[i].Syntax = syntaxs[i];
                    }

                    {

                        // ��ö�Ӧ��ʵ�����
                        lRet = Channel.GetSystemParameter(Stop,
                            "item",
                            "dbnames",
                            out strValue,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "��Է����� " + Channel.Url + " ���ʵ������б���̷�������" + strError;
                            goto ERROR1;
                        }

                        string[] itemdbnames = strValue.Split(new char[] { ',' });

                        if (itemdbnames.Length != this.BiblioDbProperties.Count)
                        {
                            strError = "��Է����� " + Channel.Url + " ��ñ�Ŀ����Ϊ " + this.BiblioDbProperties.Count.ToString() + " ������ʵ�����Ϊ " + itemdbnames.Length.ToString() + " ����������һ��";
                            goto ERROR1;
                        }

                        // �������ݸ�ʽ
                        for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                        {
                            this.BiblioDbProperties[i].ItemDbName = itemdbnames[i];
                        }

                    }

                    {

                        // ��ö�Ӧ���ڿ���
                        lRet = Channel.GetSystemParameter(Stop,
                            "issue",
                            "dbnames",
                            out strValue,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "��Է����� " + Channel.Url + " ����ڿ����б���̷�������" + strError;
                            goto ERROR1;
                        }

                        string[] issuedbnames = strValue.Split(new char[] { ',' });

                        if (issuedbnames.Length != this.BiblioDbProperties.Count)
                        {
                            return 0; // TODO: ��ʱ�����档�Ƚ��������û���������dp2libraryws 2007/10/19�Ժ�İ汾�������پ���
                            /*
                            strError = "��Է����� " + Channel.Url + " ��ñ�Ŀ����Ϊ " + this.BiblioDbProperties.Count.ToString() + " �������ڿ���Ϊ " + issuedbnames.Length.ToString() + " ����������һ��";
                            goto ERROR1;
                             * */
                        }

                        // �������ݸ�ʽ
                        for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                        {
                            this.BiblioDbProperties[i].IssueDbName = issuedbnames[i];
                        }
                    }

                    ///////

                    {

                        // ��ö�Ӧ�Ķ�������
                        lRet = Channel.GetSystemParameter(Stop,
                            "order",
                            "dbnames",
                            out strValue,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "��Է����� " + Channel.Url + " ��ö��������б���̷�������" + strError;
                            goto ERROR1;
                        }

                        string[] orderdbnames = strValue.Split(new char[] { ',' });

                        if (orderdbnames.Length != this.BiblioDbProperties.Count)
                        {
                            return 0; // TODO: ��ʱ�����档�Ƚ��������û���������dp2libraryws 2007/11/30�Ժ�İ汾�������پ���
                            /*
                            strError = "��Է����� " + Channel.Url + " ��ñ�Ŀ����Ϊ " + this.BiblioDbProperties.Count.ToString() + " ��������������Ϊ " + orderdbnames.Length.ToString() + " ����������һ��";
                            goto ERROR1;
                             * */
                        }

                        // �������ݸ�ʽ
                        for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                        {
                            this.BiblioDbProperties[i].OrderDbName = orderdbnames[i];
                        }
                    }

                }
                else
                {
                    // �·���
                    XmlDocument dom = new XmlDocument();
                    dom.LoadXml("<root />");

                    try
                    {
                        dom.DocumentElement.InnerXml = strValue;
                    }
                    catch (Exception ex)
                    {
                        strError = "category=system,name=biblioDbGroup�����ص�XMLƬ����װ��InnerXmlʱ����: " + ex.Message;
                        goto ERROR1;
                    }

                    XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];

                        BiblioDbProperty property = new BiblioDbProperty();
                        this.BiblioDbProperties.Add(property);
                        property.DbName = DomUtil.GetAttr(node, "biblioDbName");
                        property.ItemDbName = DomUtil.GetAttr(node, "itemDbName");
                        property.Syntax = DomUtil.GetAttr(node, "syntax");
                        property.IssueDbName = DomUtil.GetAttr(node, "issueDbName");
                        property.OrderDbName = DomUtil.GetAttr(node, "orderDbName");
                        property.CommentDbName = DomUtil.GetAttr(node, "commentDbName");
                        property.Role = DomUtil.GetAttr(node, "role");

                        bool bValue = true;
                        nRet = DomUtil.GetBooleanParam(node,
                            "inCirculation",
                            true,
                            out bValue,
                            out strError);
                        property.InCirculation = bValue;
                    }
                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                EndSearch();
            }

            return 0;
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
        }

#endif

        string[] m_readerDbNames = null;

        /// <summary>
        /// ���ȫ�����߿���
        /// </summary>
        public string[] ReaderDbNames 
        {
            get
            {
                if (this.m_readerDbNames == null)
                {
                    this.m_readerDbNames = new string[this.ReaderDbProperties.Count];
                    int i = 0;
                    foreach (ReaderDbProperty prop in this.ReaderDbProperties)
                    {
                        this.m_readerDbNames[i++] = prop.DbName;
                    }
                }

                return this.m_readerDbNames;
            }
        }

        // 
        // return:
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        /// <summary>
        /// ��ö��߿������б�
        /// </summary>
        /// <param name="bPrepareSearch">�Ƿ�Ҫ׼��ͨ��</param>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int InitialReaderDbProperties(bool bPrepareSearch = true)
        {
        REDO:
            if (bPrepareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return -1;
            }

            string strError = "";
            int nRet = 0;

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڳ�ʼ�����߿������б� ...");
            Stop.BeginLoop();

            try
            {
                this.ReaderDbProperties = new List<ReaderDbProperty>();
                this.m_readerDbNames = null;

                if (this.AllDatabaseDom == null)
                    return 0;

                XmlNodeList nodes = this.AllDatabaseDom.DocumentElement.SelectNodes("database[@type='reader']");
                foreach (XmlNode node in nodes)
                {

                    ReaderDbProperty property = new ReaderDbProperty();
                    this.ReaderDbProperties.Add(property);
                    property.DbName = DomUtil.GetAttr(node, "name");
                    property.LibraryCode = DomUtil.GetAttr(node, "libraryCode");

                    bool bValue = true;
                    nRet = DomUtil.GetBooleanParam(node,
                        "inCirculation",
                        true,
                        out bValue,
                        out strError);
                    property.InCirculation = bValue;
                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 0;
#if NO
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
#endif
        }


#if NO
        // 
        // return:
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        /// <summary>
        /// ��ö��߿������б�
        /// </summary>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int InitialReaderDbProperties()
        {
        REDO:
            int nRet = PrepareSearch();
            if (nRet == 0)
                return -1;

            string strError = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ�ö��߿������б� ...");
            Stop.BeginLoop();

            try
            {
                string strValue = "";
                long lRet = 0;

                this.ReaderDbProperties = new List<ReaderDbProperty>();
                this.m_readerDbNames = null;

                // ���÷���һ���Ի��ȫ������
                lRet = Channel.GetSystemParameter(Stop,
                    "system",
                    "readerDbGroup",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " ��ö��߿���Ϣ���̷�������" + strError;
                    goto ERROR1;
                }

                if (String.IsNullOrEmpty(strValue) == true)
                {
                    // �����þɷ���

                    lRet = Channel.GetSystemParameter(Stop,
                        "reader",
                        "dbnames",
                        out strValue,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "��Է����� " + Channel.Url + " ��ö��߿����б���̷�������" + strError;
                        goto ERROR1;
                    }

                    string[] readerDbNames = strValue.Split(new char[] { ',' });

                    for (int i = 0; i < readerDbNames.Length; i++)
                    {
                        ReaderDbProperty property = new ReaderDbProperty();
                        property.DbName = readerDbNames[i];
                        this.ReaderDbProperties.Add(property);
                    }
                }
                else
                {
                    // �·���
                    XmlDocument dom = new XmlDocument();
                    dom.LoadXml("<root />");

                    try
                    {
                        dom.DocumentElement.InnerXml = strValue;
                    }
                    catch (Exception ex)
                    {
                        strError = "category=system,name=readerDbGroup�����ص�XMLƬ����װ��InnerXmlʱ����: " + ex.Message;
                        goto ERROR1;
                    }

                    XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];

                        ReaderDbProperty property = new ReaderDbProperty();
                        this.ReaderDbProperties.Add(property);
                        property.DbName = DomUtil.GetAttr(node, "name");
                        property.LibraryCode = DomUtil.GetAttr(node, "libraryCode");

                        bool bValue = true;
                        nRet = DomUtil.GetBooleanParam(node,
                            "inCirculation",
                            true,
                            out bValue,
                            out strError);
                        property.InCirculation = bValue;
                    }
                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                EndSearch();
            }

            return 0;
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
        }
#endif

        // 
        // return:
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        /// <summary>
        /// ���ǰ�˽��ѽӿ�������Ϣ
        /// </summary>
        /// <param name="bPrepareSearch">�Ƿ�Ҫ׼��ͨ��</param>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int GetClientFineInterfaceInfo(bool bPrepareSearch = true)
        {
        REDO:
            if (bPrepareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return -1;
            }

            // this.Update();   // �Ż�

            string strError = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ��ǰ�˽��ѽӿ�������Ϣ ...");
            Stop.BeginLoop();

            try
            {
                string strValue = "";
                long lRet = Channel.GetSystemParameter(Stop,
                    "circulation",
                    "clientFineInterface",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " ���ǰ�˽��ѽӿ�������Ϣ���̷�������" + strError;
                    goto ERROR1;
                }

                this.ClientFineInterfaceName = "";

                if (String.IsNullOrEmpty(strValue) == false)
                {
                    XmlDocument cfg_dom = new XmlDocument();
                    try
                    {
                        cfg_dom.LoadXml(strValue);
                    }
                    catch (Exception ex)
                    {
                        strError = "���������õ�ǰ�˽��ѽӿ�XMLװ��DOMʱ����: " + ex.Message;
                        goto ERROR1;
                    }

                    this.ClientFineInterfaceName = DomUtil.GetAttr(cfg_dom.DocumentElement,
                        "name");
                }

            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 0;
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
        }

        // 
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        /// <summary>
        /// �����ȡ��������Ϣ
        /// </summary>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int GetCallNumberInfo(bool bPreareSearch = true)
        {
            this.CallNumberInfo = "";
            this.CallNumberCfgDom = null;

            if (bPreareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return -1;
            }

            // this.Update();   // �Ż�

            string strError = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ����ȡ��������Ϣ ...");
            Stop.BeginLoop();

            try
            {
                string strValue = "";
                long lRet = Channel.GetSystemParameter(Stop,
                    "circulation",
                    "callNumber",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " �����ȡ��������Ϣ���̷�������" + strError;
                    goto ERROR1;
                }

                this.CallNumberInfo = strValue;

                this.CallNumberCfgDom = new XmlDocument();
                this.CallNumberCfgDom.LoadXml("<callNumber/>");

                try
                {
                    this.CallNumberCfgDom.DocumentElement.InnerXml = this.CallNumberInfo;
                }
                catch (Exception ex)
                {
                    strError = "Set callnumber_cfg_dom InnerXml error: " + ex.Message;
                    goto ERROR1;
                }
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPreareSearch == true)
                    EndSearch();
            }

            return 0;
        ERROR1:
            /*
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�Ҫ����?",
                "dp2Circulation",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == DialogResult.OK)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
             * */
            return 1;
        }

#if NO
        // �˺���ϣ���𽥷�ֹ
        // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ��ù���һ���ض��ݲصص����ȡ��������Ϣ��
        /// ����������ֹ
        /// </summary>
        /// <param name="strLocation">�ݲصص�</param>
        /// <param name="strArrangeGroupName">�����ż���ϵ��</param>
        /// <param name="strZhongcihaoDbname">�����ִκſ���</param>
        /// <param name="strClassType">���ط���������</param>
        /// <param name="strQufenhaoType">�������ֺ�����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:  ����</para>
        /// <para>0:   û���ҵ�</para>
        /// <para>1:   �ҵ�</para>
        /// </returns>
        public int GetCallNumberInfo(string strLocation,
            out string strArrangeGroupName,
            out string strZhongcihaoDbname,
            out string strClassType,
            out string strQufenhaoType,
            out string strError)
        {
            strError = "";
            strArrangeGroupName = "";
            strZhongcihaoDbname = "";
            strClassType = "";
            strQufenhaoType = "";

            if (this.CallNumberCfgDom == null)
                return 0;

            if (this.CallNumberCfgDom.DocumentElement == null)
                return 0;

            XmlNode node = this.CallNumberCfgDom.DocumentElement.SelectSingleNode("group/location[@name='" + strLocation + "']");
            if (node == null)
            {
                // 2014/2/13
                XmlNodeList nodes = this.CallNumberCfgDom.DocumentElement.SelectNodes("group/location");
                if (nodes.Count == 0)
                    return 0;
                foreach (XmlNode current in nodes)
                {
                    string strPattern = DomUtil.GetAttr(current, "name");
                    if (LibraryServerUtil.MatchLocationName(strLocation, strPattern) == true)
                    {
                        node = current;
                        goto END1;
                    }
                }
                return 0;
            }

            END1:
            XmlNode nodeGroup = node.ParentNode;
            strArrangeGroupName = DomUtil.GetAttr(nodeGroup, "name");
            strZhongcihaoDbname = DomUtil.GetAttr(nodeGroup, "zhongcihaodb");
            strClassType = DomUtil.GetAttr(nodeGroup, "classType");
            strQufenhaoType = DomUtil.GetAttr(nodeGroup, "qufenhaoType");

            return 1;
        }

#endif

        // �˺���ϣ���𽥷�ֹ
        // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ��ù���һ���ض��ݲصص����ȡ��������Ϣ��
        /// ����������ֹ
        /// </summary>
        /// <param name="strLocation">�ݲصص�</param>
        /// <param name="strArrangeGroupName">�����ż���ϵ��</param>
        /// <param name="strZhongcihaoDbname">�����ִκſ���</param>
        /// <param name="strClassType">���ط���������</param>
        /// <param name="strQufenhaoType">�������ֺ�����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:  ����</para>
        /// <para>0:   û���ҵ�</para>
        /// <para>1:   �ҵ�</para>
        /// </returns>
        public int GetCallNumberInfo(string strLocation,
            out string strArrangeGroupName,
            out string strZhongcihaoDbname,
            out string strClassType,
            out string strQufenhaoType,
            out string strError)
        {
            strError = "";
            strArrangeGroupName = "";
            strZhongcihaoDbname = "";
            strClassType = "";
            strQufenhaoType = "";

            ArrangementInfo info = null;
            int nRet = GetArrangementInfo(strLocation,
                out info,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;

            strArrangeGroupName = info.ArrangeGroupName;
            strZhongcihaoDbname = info.ZhongcihaoDbname;
            strClassType = info.ClassType;
            strQufenhaoType = info.QufenhaoType;
            return nRet;
        }

        // ע���ż���ϵ�����е� location Ԫ�� name ֵ�����ܺ���ͨ���
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ��ù���һ���ض��ݲصص����ȡ��������Ϣ
        /// </summary>
        /// <param name="strLocation">�ݲصص��ַ���</param>
        /// <param name="info">������ȡ��������Ϣ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û���ҵ�; 1: �ҵ�</returns>
        public int GetArrangementInfo(string strLocation,
            out ArrangementInfo info,
            out string strError)
        {
            strError = "";
            info = null;

            if (this.CallNumberCfgDom == null)
                return 0;

            if (this.CallNumberCfgDom.DocumentElement == null)
                return 0;

            XmlNode node = this.CallNumberCfgDom.DocumentElement.SelectSingleNode("group/location[@name='" + strLocation + "']");
            if (node == null)
            {
                XmlNodeList nodes = this.CallNumberCfgDom.DocumentElement.SelectNodes("group/location");
                if (nodes.Count == 0)
                    return 0;
                foreach (XmlNode current in nodes)
                {
                    string strPattern = DomUtil.GetAttr(current, "name");
                    if (LibraryServerUtil.MatchLocationName(strLocation, strPattern) == true)
                    {
                        info = new ArrangementInfo();
                        info.Fill(current.ParentNode);
                        return 1;
                    }
                }

                return 0;
            }

            info = new ArrangementInfo();
            XmlNode nodeGroup = node.ParentNode;
            info.Fill(nodeGroup);
            return 1;
        }

#if NO
        // ��ö��߿����б�
        // return:
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        public int GetReaderDbNames()
        {
            int nRet = PrepareSearch();
            if (nRet == 0)
                return -1;

            // this.Update();

            string strError = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ�ö��߿����б� ...");
            Stop.BeginLoop();

            try
            {
                string strValue = "";
                long lRet = Channel.GetSystemParameter(Stop,
                    "reader",
                    "dbnames",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " ��ö��߿����б���̷�������" + strError;
                    goto ERROR1;
                }

                this.ReaderDbNames = strValue.Split(new char[] { ',' });
                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                EndSearch();
            }

            return 0;
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�Ҫ����?",
                "dp2Circulation",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
        if (result == DialogResult.OK)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
        }
#endif

        // 
        // return:
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        //      1   ������ϣ����������Ĳ���
        /// <summary>
        /// ���ʵ�ÿ������б�
        /// </summary>
        /// <param name="bPrepareSearch">�Ƿ�Ҫ׼��ͨ��</param>
        /// <returns>-1: ������ϣ�������Ժ�Ĳ���; 0: �ɹ�; 1: ������ϣ����������Ĳ���</returns>
        public int GetUtilDbProperties(bool bPrepareSearch = true)
        {
        REDO:
            if (bPrepareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return -1;
            }

            // this.Update();

            string strError = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڳ�ʼ��ʵ�ÿ������б� ...");
            Stop.BeginLoop();

            try
            {
                this.UtilDbProperties = new List<UtilDbProperty>();

                if (this.AllDatabaseDom == null)
                    return 0;
#if NO
                string strValue = "";
                long lRet = Channel.GetSystemParameter(Stop,
                    "utilDb",
                    "dbnames",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " ���ʵ�ÿ����б���̷�������" + strError;
                    goto ERROR1;
                }

                string[] utilDbNames = strValue.Split(new char[] { ',' });

                for (int i = 0; i < utilDbNames.Length; i++)
                {
                    UtilDbProperty property = new UtilDbProperty();
                    property.DbName = utilDbNames[i];
                    this.UtilDbProperties.Add(property);
                }

                // �������
                lRet = Channel.GetSystemParameter(Stop,
                    "utilDb",
                    "types",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " ���ʵ�ÿ����ݸ�ʽ�б���̷�������" + strError;
                    goto ERROR1;
                }

                string[] types = strValue.Split(new char[] { ',' });

                if (types.Length != this.UtilDbProperties.Count)
                {
                    strError = "��Է����� " + Channel.Url + " ���ʵ�ÿ���Ϊ " + this.UtilDbProperties.Count.ToString() + " ����������Ϊ " + types.Length.ToString() + " ����������һ��";
                    goto ERROR1;
                }

                // �������ݸ�ʽ
                for (int i = 0; i < this.UtilDbProperties.Count; i++)
                {
                    this.UtilDbProperties[i].Type = types[i];
                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
#endif

                XmlNodeList nodes = this.AllDatabaseDom.DocumentElement.SelectNodes("database");
                foreach (XmlNode node in nodes)
                {
                    string strName = DomUtil.GetAttr(node, "name");
                    string strType = DomUtil.GetAttr(node, "type");
                    // string strRole = DomUtil.GetAttr(node, "role");
                    // string strLibraryCode = DomUtil.GetAttr(node, "libraryCode");

                    // �յ����ֽ�������
                    if (String.IsNullOrEmpty(strName) == true)
                        continue;

                    if (strType == "zhongcihao"
                        || strType == "publisher"
                        || strType == "dictionary")
                    {
                        UtilDbProperty property = new UtilDbProperty();
                        property.DbName = strName;
                        property.Type = strType;
                        this.UtilDbProperties.Add(property);
                    }

                }
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 0;
#if NO
        ERROR1:
            DialogResult result = MessageBox.Show(this,
                strError + "\r\n\r\n�Ƿ�����?",
                "dp2Circulation",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (result == System.Windows.Forms.DialogResult.Yes)
                goto REDO;
            if (result == DialogResult.No)
                return 1;   // ������ϣ����������Ĳ���

            return -1;  // ������ϣ�������Ժ�Ĳ���
#endif
        }

        /// <summary>
        /// ���һ�����ݿ����Ŀ���Լ���
        /// </summary>
        /// <param name="strDbName">���ݿ���</param>
        /// <returns>���Լ���</returns>
        public ColumnPropertyCollection GetBrowseColumnProperties(string strDbName)
        {
            // ColumnPropertyCollection results = new ColumnPropertyCollection();
            Debug.Assert(this.NormalDbProperties != null, "this.NormalDbProperties == null");
            if (this.NormalDbProperties == null)
                return null;    // 2014/12/22
            for (int i = 0; i < this.NormalDbProperties.Count; i++)
            {
                NormalDbProperty prop = this.NormalDbProperties[i];
                Debug.Assert(prop != null, "prop == null");
                if (prop == null)
                    continue;    // 2014/12/22
                if (prop.DbName == strDbName)
                    return prop.ColumnProperties;
            }

            return null;    // not found
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public void EnableControls(bool bEnable)
        {
            this.menuStrip_main.Enabled = bEnable;

            this.toolStripDropDownButton_barcodeLoadStyle.Enabled = bEnable;
            this.toolStripTextBox_barcode.Enabled = bEnable;

            this.toolButton_amerce.Enabled = bEnable;
            this.toolButton_borrow.Enabled = bEnable;
            this.toolButton_lost.Enabled = bEnable;
            this.toolButton_readerManage.Enabled = bEnable;
            this.toolButton_renew.Enabled = bEnable;
            this.toolButton_return.Enabled = bEnable;
            this.toolButton_verifyReturn.Enabled = bEnable;
            this.toolButton_print.Enabled = bEnable;
            this.toolStripButton_loadBarcode.Enabled = bEnable;

            
        }

        // ��caption�����滯��
        // Ҳ�����ж��Ƿ����ظ���caption��������У���������
        void CanonicalizeBiblioFromValues()
        {
            if (this.BiblioDbFromInfos == null)
                return;

            for (int i = 0; i < this.BiblioDbFromInfos.Length; i++)
            {
                BiblioDbFromInfo info = this.BiblioDbFromInfos[i];

                int nSeed = 1;
                for (int j = i + 1; j < this.BiblioDbFromInfos.Length; j++)
                {
                    BiblioDbFromInfo info1 = this.BiblioDbFromInfos[j];

                    // ���caption���أ����һ�����ӱ��
                    if (info.Caption == info1.Caption
                        && info.Style != info1.Style)
                    {
                        info1.Caption += nSeed.ToString();
                        nSeed++;
                    }
                }
            }
        }

        // 2009/11/8 new add
        // 
        // Exception:
        //     ���ܻ��׳�Exception�쳣
        /// <summary>
        /// ����from style�ַ����õ�style caption
        /// </summary>
        /// <param name="strStyle">from style�ַ���</param>
        /// <returns>style caption�ַ���</returns>
        public string GetBiblioFromCaption(string strStyle)
        {
            if (this.BiblioDbFromInfos == null)
            {
                throw new Exception("this.DbFromInfos��δ��ʼ��");
            }

            Debug.Assert(this.BiblioDbFromInfos != null, "this.DbFromInfos��δ��ʼ��");

            for (int i = 0; i < this.BiblioDbFromInfos.Length; i++)
            {
                BiblioDbFromInfo info = this.BiblioDbFromInfos[i];

                if (strStyle == info.Style)
                    return info.Caption;
            }

            return null;
        }

        // 
        // Exception:
        //     ���ܻ��׳�Exception�쳣
        /// <summary>
        /// ����from���б��ַ����õ�from style�б��ַ���
        /// </summary>
        /// <param name="strCaptions">����;����</param>
        /// <returns>style�б��ַ���</returns>
        public string GetBiblioFromStyle(string strCaptions)
        {
            if (this.BiblioDbFromInfos == null)
            {
                throw new Exception("this.DbFromInfos��δ��ʼ��");
                // return null;    // 2009/3/29 new add
            }

            Debug.Assert(this.BiblioDbFromInfos != null, "this.DbFromInfos��δ��ʼ��");

            string strResult = "";

            string[] parts = strCaptions.Split(new char[] { ',' });
            for (int k = 0; k < parts.Length; k++)
            {
                string strCaption = parts[k].Trim();

                // 2009/9/23 new add
                // TODO: �Ƿ����ֱ��ʹ��\t����Ĳ����أ�
                // ����һ��caption�ַ������г���������е�\t����
                int nRet = strCaption.IndexOf("\t");
                if (nRet != -1)
                    strCaption = strCaption.Substring(0, nRet).Trim();

                if (strCaption.ToLower() == "<all>"
                    || strCaption == "<ȫ��>"
                    || String.IsNullOrEmpty(strCaption) == true)
                    return "<all>";

                for (int i = 0; i < this.BiblioDbFromInfos.Length; i++)
                {
                    BiblioDbFromInfo info = this.BiblioDbFromInfos[i];

                    if (strCaption == info.Caption)
                    {
                        if (string.IsNullOrEmpty(strResult) == false)
                            strResult += ",";
                        // strResult += GetDisplayStyle(info.Style, true);   // ע�⣬ȥ�� _ �� __ ��ͷ����Щ��Ӧ�û�ʣ������һ�� style
                        strResult += GetDisplayStyle(info.Style, true, false);   // ע�⣬ȥ�� __ ��ͷ����Щ��Ӧ�û�ʣ������һ�� style��_ ��ͷ�Ĳ�Ҫ�˳�
                    }
                }
            }

            return strResult;

            // return null;
        }



        // ComboBox�汾
        /// <summary>
        /// �����Ŀ�����;�� ComboBox �б�
        /// </summary>
        /// <param name="comboBox_from">ComboBox ����</param>
        public void FillBiblioFromList(ComboBox comboBox_from)
        {
            // ���浱ǰ�� Text ֵ
            string strOldText = comboBox_from.Text;

            comboBox_from.Items.Clear();

            comboBox_from.Items.Add("<ȫ��>");

            if (this.BiblioDbFromInfos == null)
                return;

            Debug.Assert(this.BiblioDbFromInfos != null);

            string strFirstItem = "";
            // װ�����;��
            for (int i = 0; i < this.BiblioDbFromInfos.Length; i++)
            {
                BiblioDbFromInfo info = this.BiblioDbFromInfos[i];

                comboBox_from.Items.Add(info.Caption/* + "(" + infos[i].Style+ ")"*/);

                if (i == 0)
                    strFirstItem = info.Caption;
            }

            comboBox_from.Text = strFirstItem;

            // 2014/5/20
            if (string.IsNullOrEmpty(strOldText) == false)
                comboBox_from.Text = strOldText;
        }

        // TabComboBox�汾
        // �ұ��г�style��
        /// <summary>
        /// �����Ŀ�����;�� TabComboBox �б�
        /// ÿһ������Ǽ���;�������ұ��� style ��
        /// </summary>
        /// <param name="comboBox_from">TabComboBox����</param>
        public void FillBiblioFromList(DigitalPlatform.CommonControl.TabComboBox comboBox_from)
        {
            comboBox_from.Items.Clear();

            comboBox_from.Items.Add("<ȫ��>");

            if (this.BiblioDbFromInfos == null)
                return;

            Debug.Assert(this.BiblioDbFromInfos != null);

            string strFirstItem = "";
            // װ�����;��
            for (int i = 0; i < this.BiblioDbFromInfos.Length; i++)
            {
                BiblioDbFromInfo info = this.BiblioDbFromInfos[i];

                comboBox_from.Items.Add(info.Caption + "\t" + GetDisplayStyle(info.Style));

                if (i == 0)
                    strFirstItem = info.Caption;
            }

            comboBox_from.Text = strFirstItem;
        }

        // ���˵� _ ��ͷ����Щstyle�Ӵ�
        // parameters:
        //      bRemove2    �Ƿ�ҲҪ�˳� __ ǰ׺��
        //                  �������ڼ���;���б������ʱ��Ϊ�˱�����ᣬҪ���� __ ǰ׺�ģ������ͼ������� dp2library ��ʱ��Ϊ�˱�������Ҳ����ƥ����������;����Ҫ�� __ ǰ׺�� style �˳�
        static string GetDisplayStyle(string strStyles,
            bool bRemove2 = false)
        {
            string[] parts = strStyles.Split(new char[] {','});
            List<string> results = new List<string>();
            foreach (string part in parts)
            {
                string strText = part.Trim();
                if (String.IsNullOrEmpty(strText) == true)
                    continue;

                if (bRemove2 == false)
                {
                    // ֻ�˳� _ ��ͷ��
                    if (StringUtil.HasHead(strText, "_") == true
                        && StringUtil.HasHead(strText, "__") == false)
                        continue;
                }
                else
                {
                    // 2013/12/30 _ �� __ ��ͷ�Ķ����˳�
                    if (StringUtil.HasHead(strText, "_") == true)
                        continue;
                }

                results.Add(strText);
            }

            return StringUtil.MakePathList(results, ",");
        }

        // ���˵� _ ��ͷ����Щstyle�Ӵ�
        // parameters:
        //      bRemove2    �Ƿ��˳� __ ǰ׺��
        //      bRemove1    �Ƿ��˳� _ ǰ׺��
        static string GetDisplayStyle(string strStyles,
            bool bRemove2,
            bool bRemove1)
        {
            string[] parts = strStyles.Split(new char[] { ',' });
            List<string> results = new List<string>();
            foreach (string part in parts)
            {
                string strText = part.Trim();
                if (String.IsNullOrEmpty(strText) == true)
                    continue;

                if (strText[0] == '_')
                {
                    if (bRemove1 == true)
                    {
                        if (strText.Length >= 2 && /*strText[0] == '_' &&*/ strText[1] != '_')
                            continue;
#if NO
                        if (strText[0] == '_')
                            continue;
#endif
                        if (strText.Length == 1)
                            continue;
                    }

                    if (bRemove2 == true && strText.Length >= 2)
                    {
                        if (/*strText[0] == '_' && */ strText[1] == '_')
                            continue;
                    }
                }


                results.Add(strText);
            }

            return StringUtil.MakePathList(results, ",");
        }

        // 
        /// <summary>
        /// dp2Library ������ URL
        /// </summary>
        public string LibraryServerUrl
        {
            get
            {
                if (this.AppInfo == null)
                    return "";

                return this.AppInfo.GetString(
                    "config",
                    "circulation_server_url",
                    "http://localhost:8001/dp2library");
            }
            set
            {
                if (this.AppInfo != null)
                {
                    this.AppInfo.SetString(
                        "config",
                        "circulation_server_url",
                        value);
                }
            }
        }

        /// <summary>
        /// ���ֵ�б���
        /// </summary>
        public void ClearValueTableCache()
        {
            this.valueTableCache = new Hashtable();

            // ֪ͨ���� MDI �Ӵ���
            ParamChangedEventArgs e = new ParamChangedEventArgs();
            e.Section = "valueTableCacheCleared";
            NotifyAllMdiChildren(e);
        }

        void NotifyAllMdiChildren(ParamChangedEventArgs e)
        {
            foreach (Form form in this.MdiChildren)
            {
                if (form is MyForm)
                {
                    MyForm myForm = form as MyForm;
                    myForm.OnNotify(e);
                }
            }
        }

        // ���ֵ�б�
        // ����cache����
        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        /// <param name="strTableName">�����</param>
        /// <param name="strDbName">���ݿ���</param>
        /// <param name="values">����ֵ�ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int GetValueTable(string strTableName,
            string strDbName,
            out string [] values,
            out string strError)
        {
            values = null;
            strError = "";

            // �ȿ������������Ƿ��Ѿ�����
            string strName = strTableName + "~~~" + strDbName;

            values = (string [])this.valueTableCache[strName];

            if (values != null)
                return 0;

            int nRet = PrepareSearch();
            if (nRet == 0)
            {
                strError = "PrepareSearch() error";
                return -1;  // 2009/2/11
            }

            // this.Update();

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ�ȡֵ�б� ...");
            Stop.BeginLoop();

            try
            {
                long lRet = Channel.GetValueTable(
                    Stop,
                    strTableName,
                    strDbName,
                    out values,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (values == null)
                    values = new string[0];

                this.valueTableCache[strName] = values;
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                EndSearch();
            }

            return 0;
        ERROR1:
            return -1;
        }

        #region EnsureXXXForm ...
        /// <summary>
        /// ������� UtilityForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public UtilityForm EnsureUtilityForm()
        {
#if NO
            UtilityForm form = TopUtilityForm;
            if (form == null)
            {
                form = new UtilityForm();
                form.MdiParent = this;
                // form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<UtilityForm>();
        }

        /// <summary>
        /// ������� HtmlPrintForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <param name="bMinimized">�Ƿ���С������</param>
        /// <returns>����</returns>
        public HtmlPrintForm EnsureHtmlPrintForm(bool bMinimized)
        {
            HtmlPrintForm form = GetTopChildWindow<HtmlPrintForm>();
            if (form == null)
            {
                Form top = this.ActiveMdiChild;
                FormWindowState top_state = FormWindowState.Normal;
                if (top != null)
                {
                    top_state = top.WindowState;
                }

                form = new HtmlPrintForm();
                form.MdiParent = this;
                form.MainForm = this;
                if (bMinimized == true)
                    form.WindowState = FormWindowState.Minimized;
                form.Show();

                if (top != null && bMinimized == true)
                {
                    if (top.WindowState != top_state)
                        top.WindowState = top_state;
                }
            }

            return form;
        }

        /// <summary>
        /// ������� AmerceForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public AmerceForm EnsureAmerceForm()
        {
#if NO
            AmerceForm form = TopAmerceForm;
            if (form == null)
            {
                form = new AmerceForm();
                form.MdiParent = this;
                form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<AmerceForm>();
        }

        /// <summary>
        /// ������� ReaderManageForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public ReaderManageForm EnsureReaderManageForm()
        {
#if NO
            ReaderManageForm form = TopReaderManageForm;
            if (form == null)
            {
                form = new ReaderManageForm();
                form.MdiParent = this;
                form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<ReaderManageForm>();
        }

        /// <summary>
        /// ������� ReaderInfoForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public ReaderInfoForm EnsureReaderInfoForm()
        {
#if NO
            ReaderInfoForm form = TopReaderInfoForm;
            if (form == null)
            {
                form = new ReaderInfoForm();
                form.MdiParent = this;
                form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<ReaderInfoForm>();
        }

        /// <summary>
        /// ������� ActivateForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public ActivateForm EnsureActivateForm()
        {
#if NO
            ActivateForm form = TopActivateForm;
            if (form == null)
            {
                form = new ActivateForm();
                form.MdiParent = this;
                form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<ActivateForm>();
        }

        /// <summary>
        /// ������� EntityForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public EntityForm EnsureEntityForm()
        {
#if NO
            EntityForm form = TopEntityForm;
            if (form == null)
            {
                form = new EntityForm();
                form.MdiParent = this;
                form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<EntityForm>();
        }

        /// <summary>
        /// ������� DupForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public DupForm EnsureDupForm()
        {
#if NO
            DupForm form = TopDupForm;
            if (form == null)
            {
                form = new DupForm();
                form.MdiParent = this;
                form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<DupForm>();
        }

        /// <summary>
        /// ������� ItemInfoForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public ItemInfoForm EnsureItemInfoForm()
        {
#if NO
            ItemInfoForm form = TopItemInfoForm;
            if (form == null)
            {
                form = new ItemInfoForm();
                form.MdiParent = this;
                form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<ItemInfoForm>();
        }

        /// <summary>
        /// ������� PrintAcceptForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public PrintAcceptForm EnsurePrintAcceptForm()
        {
#if NO
            PrintAcceptForm form = TopPrintAcceptForm;
            if (form == null)
            {
                form = new PrintAcceptForm();
                form.MdiParent = this;
                form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<PrintAcceptForm>();
        }

        /// <summary>
        /// ������� LabelPrintForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public LabelPrintForm EnsureLabelPrintForm()
        {
#if NO
            LabelPrintForm form = TopLabelPrintForm;
            if (form == null)
            {
                form = new LabelPrintForm();
                form.MdiParent = this;
                // form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<LabelPrintForm>();
        }

        /// <summary>
        /// ������� CardPrintForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public CardPrintForm EnsureCardPrintForm()
        {
#if NO
            CardPrintForm form = TopCardPrintForm;
            if (form == null)
            {
                form = new CardPrintForm();
                form.MdiParent = this;
                // form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<CardPrintForm>();
        }

        /// <summary>
        /// ������� BiblioStatisForm ���ڣ����û�У����´���һ��
        /// </summary>
        /// <returns>����</returns>
        public BiblioStatisForm EnsureBiblioStatisForm()
        {
#if NO
            BiblioStatisForm form = TopBiblioStatisForm;
            if (form == null)
            {
                form = new BiblioStatisForm();
                form.MdiParent = this;
                // form.MainForm = this;
                form.Show();
            }

            return form;
#endif
            return EnsureChildForm<BiblioStatisForm>();
        }

#endregion

        private void toolButton_borrow_Click(object sender, EventArgs e)
        {
            if (this.Urgent == false)
            {
                if (this.ActiveMdiChild != null
                    && this.ActiveMdiChild is ChargingForm)
                {
                    EnsureChildForm<ChargingForm>().Activate();
                    EnsureChildForm<ChargingForm>().SmartFuncState = FuncState.Borrow;
                }
                else
                {
                    EnsureChildForm<QuickChargingForm>().Activate();
                    EnsureChildForm<QuickChargingForm>().SmartFuncState = FuncState.Borrow;
                }
            }
            else
            {
                EnsureChildForm<UrgentChargingForm>().Activate();
                EnsureChildForm<UrgentChargingForm>().SmartFuncState = FuncState.Borrow;
            }
        }

        private void toolButton_return_Click(object sender, EventArgs e)
        {
            if (this.Urgent == false)
            {
                if (this.ActiveMdiChild != null
                    && this.ActiveMdiChild is ChargingForm)
                {
                    EnsureChildForm<ChargingForm>().Activate();
                    EnsureChildForm<ChargingForm>().SmartFuncState = FuncState.Return;
                }
                else
                {
                    EnsureChildForm<QuickChargingForm>().Activate();
                    EnsureChildForm<QuickChargingForm>().SmartFuncState = FuncState.Return;
                }
            }
            else
            {
                EnsureChildForm<UrgentChargingForm>().Activate();
                EnsureChildForm<UrgentChargingForm>().SmartFuncState = FuncState.Return;
            }
        }

        private void toolButton_verifyReturn_Click(object sender, EventArgs e)
        {
            if (this.Urgent == false)
            {
                if (this.ActiveMdiChild != null
                    && this.ActiveMdiChild is ChargingForm)
                {
                    EnsureChildForm<ChargingForm>().Activate();
                    EnsureChildForm<ChargingForm>().SmartFuncState = FuncState.VerifyReturn;
                }
                else
                {
                    EnsureChildForm<QuickChargingForm>().Activate();
                    EnsureChildForm<QuickChargingForm>().SmartFuncState = FuncState.VerifyReturn;
                }
            }
            else
            {
                EnsureChildForm<UrgentChargingForm>().Activate();
                EnsureChildForm<UrgentChargingForm>().SmartFuncState = FuncState.VerifyReturn;
            }
        }

        private void toolButton_renew_Click(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild != null
                && this.ActiveMdiChild is ChargingForm)
            {
                EnsureChildForm<ChargingForm>().Activate();
                EnsureChildForm<ChargingForm>().SmartFuncState = FuncState.VerifyRenew;
            }
            else
            {
                EnsureChildForm<QuickChargingForm>().Activate();
                EnsureChildForm<QuickChargingForm>().SmartFuncState = FuncState.VerifyRenew;
            }
        }

        private void toolButton_lost_Click(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild != null
                && this.ActiveMdiChild is ChargingForm)
            {
                EnsureChildForm<ChargingForm>().Activate();
                EnsureChildForm<ChargingForm>().SmartFuncState = FuncState.Lost;
            }
            else
            {
                EnsureChildForm<QuickChargingForm>().Activate();
                EnsureChildForm<QuickChargingForm>().SmartFuncState = FuncState.Lost;
            }
        }

        private void toolButton_amerce_Click(object sender, EventArgs e)
        {
            EnsureAmerceForm().Activate();
        }

        private void toolButton_readerManage_Click(object sender, EventArgs e)
        {
            EnsureReaderManageForm().Activate();
        }

        // ������ӡ��ť
        private void toolButton_print_Click(object sender, EventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                EnsureChildForm<ChargingPrintManageForm>().Activate();
            }
            else
            {
                Form active = this.ActiveMdiChild;

                if (active is ChargingForm)
                {
                    ChargingForm form = (ChargingForm)active;
                    form.Print();
                }
                else if (active is QuickChargingForm)
                {
                    QuickChargingForm form = (QuickChargingForm)active;
                    form.Print();
                }
                else if (active is AmerceForm)
                {
                    AmerceForm form = (AmerceForm)active;
                    form.Print();
                }
            }
        }

        // װ������
        private void toolStripButton_loadBarcode_Click(object sender, EventArgs e)
        {
            if (this.ToolStripMenuItem_loadReaderInfo.Checked == true)
                LoadReaderBarcode();
            else if (this.ToolStripMenuItem_loadItemInfo.Checked == true)
                LoadItemBarcode();
            else
            {
                Debug.Assert(this.ToolStripMenuItem_autoLoadItemOrReader.Checked == true, "");
                LoadItemOrReaderBarcode();
            }
            this.toolStripTextBox_barcode.SelectAll();
        }

        private void toolStripTextBox_barcode_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                // �س�
                case Keys.Enter:
                    toolStripButton_loadBarcode_Click(sender, e);
                    break;
            }

        }

        // װ����������ؼ�¼
        // ����ռ�õ�ǰ�Ѿ��򿪵��ֲᴰ
        void LoadItemBarcode()
        {
            if (this.toolStripTextBox_barcode.Text == "")
            {
                MessageBox.Show(this, "��δ��������");
                return;
            }

            /*
            ItemInfoForm form = this.TopItemInfoForm;

            if (form == null)
            {
                // �¿�һ��ʵ�崰
                form = new ItemInfoForm();
                form.MdiParent = this;
                form.MainForm = this;
                form.Show();
            }

            form.LoadRecord(this.toolStripTextBox_barcode.Text);
             * */

            EntityForm form = this.GetTopChildWindow<EntityForm>();

            if (form == null)
            {
                // �¿�һ���ֲᴰ
                form = new EntityForm();
                form.MdiParent = this;
                form.MainForm = this;
                form.Show();
            }
            else
                Global.Activate(form);

            // װ��һ���ᣬ����װ����
            // parameters:
            //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            form.LoadItemByBarcode(this.toolStripTextBox_barcode.Text, false);
        }

        // װ�����֤�������صļ�¼
        // ����ռ�õ�ǰ�Ѿ��򿪵Ķ��ߴ�
        void LoadReaderBarcode()
        {
            if (this.toolStripTextBox_barcode.Text == "")
            {
                MessageBox.Show(this, "��δ���������");
                return;
            }

            ReaderInfoForm form = this.GetTopChildWindow<ReaderInfoForm>();

            if (form == null)
            {
                // �¿�һ�����ߴ�
                form = new ReaderInfoForm();
                form.MdiParent = this;
                form.MainForm = this;
                form.Show();
            }
            else
                Global.Activate(form);


            // ���ݶ���֤����ţ�װ����߼�¼
            // parameters:
            //      bForceLoad  �ڷ����������������Ƿ�ǿ��װ���һ��
            form.LoadRecord(this.toolStripTextBox_barcode.Text,
                false);
        }

        // �Զ��ж��������Ͳ�װ����Ӧ�ļ�¼
        void LoadItemOrReaderBarcode()
        {
            string strError = "";

            if (this.toolStripTextBox_barcode.Text == "")
            {
                strError = "��δ��������";
                goto ERROR1;
            }

            // ��ʽУ�������
            // return:
            //      -2  ������û������У�鷽�����޷�У��
            //      -1  error
            //      0   ���ǺϷ��������
            //      1   �ǺϷ��Ķ���֤�����
            //      2   �ǺϷ��Ĳ������
            int nRet = VerifyBarcode(
                this.Channel.LibraryCodeList,
                this.toolStripTextBox_barcode.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == -2)
            {
                strError = "����������δ�ṩ�����У�鷽��������޷��ֱ����֤����źͲ������";
                goto ERROR1;
            }
                
            if (nRet == 0)
            {
                if (String.IsNullOrEmpty(strError) == true)
                    strError = "���벻�Ϸ�";
                goto ERROR1;
            }

            if (nRet == 1)
            {
                /*
                ReaderInfoForm form = this.TopReaderInfoForm;

                if (form == null)
                {
                    // �¿�һ�����ߴ�
                    form = new ReaderInfoForm();
                    form.MdiParent = this;
                    form.MainForm = this;
                    form.Show();
                }
                else
                    Global.Activate(form);


                form.LoadRecord(this.toolStripTextBox_barcode.Text,
                    false);
                 * */
                LoadReaderBarcode();
            }

            if (nRet == 2)
            {
                LoadItemBarcode();
            }

            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �˶Ա��غͷ�����ʱ��
        // return:
        //      -1  error
        //      0   û������
        //      1   ����ʱ�Ӻͷ�����ʱ��ƫ����󣬳���10���� strError���б�����Ϣ
        int CheckServerClock(bool bPrepareSearch,
            out string strError)
        {
            strError = "";

            if (bPrepareSearch == true)
            {
                if (PrepareSearch() == 0)
                    return 0;
            }

            // this.Update();

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ�÷�������ǰʱ�� ...");
            Stop.BeginLoop();

            try
            {
                string strTime = "";
                long lRet = Channel.GetClock(
                    Stop,
                    out strTime,
                    out strError);
                if (lRet == -1)
                    return -1;

                DateTime server_time = DateTimeUtil.FromRfc1123DateTimeString(strTime);
                server_time = server_time.ToLocalTime();

                DateTime now = DateTime.Now;

                TimeSpan delta = server_time - now;
                if (delta.TotalMinutes > 10 || delta.TotalMinutes < -10)
                {
                    strError = "����ʱ�Ӻͷ�����ʱ�Ӳ������Ϊ " 
                        + delta.ToString()
                        + "��\r\n\r\n"
                        + "����ʱ�ķ�����ʱ��Ϊ: " + server_time.ToString() + "  ����ʱ��Ϊ: " + now.ToString()
                        +"\r\n\r\n����ʱ�Ӵ���ϸ�˶Է�����ʱ�ӣ����б�Ҫ�����趨������ʱ��Ϊ��ȷֵ��\r\n\r\nע����ͨ���ܾ����÷�����ʱ�ӣ����������ʱ����ȷ������ʱ�Ӳ���ȷ��һ�㲻��Ӱ����ͨ�����������С�";
                    return 1;
                }
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                if (bPrepareSearch == true)
                    EndSearch();
            }

            return 0;
        }

        // д��ʵ���¼
        public int WriteDictionary(
            string strDbName,
            string strKey,
            string strValue,
            out string strError)
        {
            strError = "";

            string strLang = "zh";
            string strQueryXml = "<target list='" + strDbName + ":" + "��" + "'><item><word>"
+ StringUtil.GetXmlStringSimple(strKey)
+ "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>" + strLang + "</lang></target>";

            int nRet = PrepareSearch();
            if (nRet == 0)
            {
                strError = "PrepareSearch() error";
                return -1;
            }

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڼ������� '" + strKey + "' ...");
            Stop.BeginLoop();

            EnableControls(false);
            try
            {
                long lRet = Channel.Search(
                    Stop,
                    strQueryXml,
                    "default",
                    "",
                    out strError);

                if (lRet == -1)
                    goto ERROR1;

                string strXml = "";
                string strRecPath = "";
                byte[] baTimestamp = null;
                if (lRet >= 1)
                {
                    DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;
                    lRet = Channel.GetSearchResult(
                        Stop,
                        "default",
                        0,
                        1,
                        "id,xml,timestamp",   // "id,cols"
                        strLang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    DigitalPlatform.CirculationClient.localhost.Record record = searchresults[0];

                    strXml = (record.RecordBody.Xml);
                    strRecPath = record.Path;
                    baTimestamp = record.RecordBody.Timestamp;
                }

                XmlDocument dom = new XmlDocument();
                if (string.IsNullOrEmpty(strXml) == false)
                {
                    try
                    {
                        dom.LoadXml(strXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "XML װ�� DOM ʱ����: " + ex.Message;
                        goto ERROR1;
                    }
                }
                else
                {
                    dom.LoadXml("<root />");
                }

                // ���� key Ԫ��
                XmlNode nodeKey = dom.DocumentElement.SelectSingleNode("key");
                if (nodeKey == null)
                {
                    nodeKey = dom.CreateElement("key");
                    dom.DocumentElement.AppendChild(nodeKey);
                }

                DomUtil.SetAttr(nodeKey, "name", strKey);

                // Ѱ��ƥ��� rel Ԫ��
                XmlNode nodeRel = dom.DocumentElement.SelectSingleNode("rel[@name=" + StringUtil.XPathLiteral(strValue) + "]");
                if (nodeRel == null)
                {
                    nodeRel = dom.CreateElement("rel");
                    dom.DocumentElement.AppendChild(nodeRel);

                    DomUtil.SetAttr(nodeRel, "name", strValue); 
                }

                // weight �� 1
                string strWeight = DomUtil.GetAttr(nodeRel, "weight");
                if (string.IsNullOrEmpty(strWeight) == true)
                    strWeight = "1";
                else
                {
                    long v = 0;
                    long.TryParse(strWeight, out v);
                    v++;
                    strWeight = v.ToString();
                }

                DomUtil.SetAttr(nodeRel, "weight", strWeight);

                // д�ؼ�¼
                if (string.IsNullOrEmpty(strRecPath) == true)
                    strRecPath = strDbName + "/?";

                byte[] output_timestamp = null;
                string strOutputPath = "";
                // ����Xml��¼����װ�汾�����ڱ����ı����͵���Դ��
                lRet = Channel.WriteRes(
                    Stop,
                    strRecPath,
                    dom.DocumentElement.OuterXml,
                    true,
                    "",
                    baTimestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                return 0;
            }
            finally
            {
                EnableControls(true);

                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                EndSearch();
            }
        ERROR1:
            return -1;
        }

        // �����ʵ��
        // parameters:
        //      results [in,out] �����Ҫ���ؽ������Ҫ�ڵ���ǰ new List<string>()���������Ҫ���ؽ�������� null ����
        public int SearchDictionary(
            Stop stop,
            string strDbName,
            string strKey,
            string strMatchStyle,
            int nMaxCount,
            ref List<string> results,
            out string strError)
        {
            strError = "";

            string strLang = "zh";
            string strQueryXml = "<target list='" + strDbName + ":" + "��" + "'><item><word>"
+ StringUtil.GetXmlStringSimple(strKey)
+ "</word><match>" + strMatchStyle + "</match><relation>=</relation><dataType>string</dataType><maxCount>"+nMaxCount.ToString()+"</maxCount></item><lang>" + strLang + "</lang></target>";

            int nRet = PrepareSearch();
            if (nRet == 0)
            {
                strError = "PrepareSearch() error";
                return -1;
            }

            if (stop == null)
            {
                stop = Stop;

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڼ������� '" + strKey + "' ...");
                stop.BeginLoop();

                EnableControls(false);
            }

            try
            {
                long lRet = Channel.Search(
                    stop,
                    strQueryXml,
                    "default",
                    "",
                    out strError);
                if (lRet == 0)
                    return 0;
                if (lRet == -1)
                    goto ERROR1;
                if (results == null)
                    return (int)lRet;

                long lHitCount = lRet;

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                for (; ; )
                {
                    lRet = Channel.GetSearchResult(
                        Stop,
                        "default",
                        lStart,
                        lPerCount,
                        "id,xml",   // "id,cols"
                        strLang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        DigitalPlatform.CirculationClient.localhost.Record record = searchresults[i];

                        results.Add(record.RecordBody.Xml);
                    }

                    lStart += searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;
                }

                return (int)lHitCount;
            }
            finally
            {

                if (stop == Stop)
                {
                    EnableControls(true);

                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }

                EndSearch();
            }
        ERROR1:
            return -1;
        }

#if NO
        // ��ʽУ�������
        // return:
        //      -2  ������û������У�鷽�����޷�У��
        //      -1  error
        //      0   ���ǺϷ��������
        //      1   �ǺϷ��Ķ���֤�����
        //      2   �ǺϷ��Ĳ������
        int VerifyBarcode(
            string strBarcode,
            out string strError)
        {
            strError = "";

            int nRet = PrepareSearch();
            if (nRet == 0)
            {
                strError = "PrepareSearch() error";
                return -1;  // 2009/2/11
            }

            // this.Update();

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("����У������ ...");
            Stop.BeginLoop();

            /*
            this.Update();
            this.MainForm.Update();
             * */
            EnableControls(false);

            try
            {
                long lRet = Channel.VerifyBarcode(
                    Stop,
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
                EnableControls(true);

                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                EndSearch();
            }
        ERROR1:
            return -1;
        }
#endif

        // ��װ��İ汾
        // ��ʽУ�������
        // return:
        //      -2  ������û������У�鷽�����޷�У��
        //      -1  error
        //      0   ���ǺϷ��������
        //      1   �ǺϷ��Ķ���֤�����
        //      2   �ǺϷ��Ĳ������
        int VerifyBarcode(
            string strLibraryCodeList,
            string strBarcode,
            out string strError)
        {
            strError = "";

            int nRet = PrepareSearch();
            if (nRet == 0)
            {
                strError = "PrepareSearch() error";
                return -1;  // 2009/2/11
            }

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("������֤����� "+strBarcode+"...");
            Stop.BeginLoop();

            // EnableControls(false);

            try
            {
                return VerifyBarcode(
                    Stop,
                    Channel,
                    strLibraryCodeList,
                    strBarcode,
                    EnableControls,
                    out strError);
            }
            finally
            {
                // EnableControls(true);

                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");

                EndSearch();
            }
        }

        public delegate void Delegate_enableControls(bool bEnable);

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
        /// <param name="stop">ֹͣ����</param>
        /// <param name="Channel">ͨѶͨ��</param>
        /// <param name="strLibraryCode">�ݴ���</param>
        /// <param name="strBarcode">ҪУ��������</param>
        /// <param name="procEnableControls">EnableControl()������ַ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-2  ������û������У�鷽�����޷�У��</para>
        /// <para>-1  ����</para>
        /// <para>0   ���ǺϷ��������</para>
        /// <para>1   �ǺϷ��Ķ���֤�����</para>
        /// <para>2   �ǺϷ��Ĳ������</para>
        /// </returns>
        public int VerifyBarcode(
            Stop stop,
            LibraryChannel Channel,
            string strLibraryCode,
            string strBarcode,
            Delegate_enableControls procEnableControls,
            out string strError)
        {
            strError = "";

            // 2014/5/4
            if (StringUtil.HasHead(strBarcode, "PQR:") == true)
            {
                strError = "���Ƕ���֤�Ŷ�ά��";
                return 1;
            }

            // ���Ƚ���ǰ��У��
            if (this.ClientHost != null)
            {
                bool bOldStyle = false;
                dynamic o = this.ClientHost;
                try
                {
                    return o.VerifyBarcode(
                        strLibraryCode, // 2014/9/27 ����
                        strBarcode,
                        out strError);
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
                {
                    // ���������з�����������У��
                    bOldStyle = true;
                }
                catch (Exception ex)
                {
                    strError = "ǰ��ִ��У��ű��׳��쳣: " + ExceptionUtil.GetDebugText(ex);
                    return -1;
                }

                if (bOldStyle == true)
                {
                    // ������ǰ�Ĳ�����ʽ
                    try
                    {
                        return o.VerifyBarcode(
                            strBarcode,
                            out strError);
                    }
                    catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
                    {
                        // ���������з�����������У��
                    }
                    catch (Exception ex)
                    {
                        strError = "ǰ��ִ��У��ű��׳��쳣: " + ExceptionUtil.GetDebugText(ex);
                        return -1;
                    }
                }
            }

            if (procEnableControls != null)
                procEnableControls(false);
            // EnableControls(false);

#if NO
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����У������ ...");
            stop.BeginLoop();
#endif
            try
            {
                long lRet = Channel.VerifyBarcode(
                    stop,
                    strLibraryCode,
                    strBarcode,
                    out strError);
                if (lRet == -1)
                {
                    if (Channel.ErrorCode == DigitalPlatform.CirculationClient.localhost.ErrorCode.NotFound)
                        return -2;
                    return -1;
                }
                return (int)lRet;
            }
            finally
            {
#if NO
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                // EnableControls(true);
#endif
                if (procEnableControls != null)
                    procEnableControls(true);
            }
        }

        // parameters:
        //      bLogin  �Ƿ��ڶԻ����������¼�����Ϊfalse����ʾֻ������ȱʡ�ʻ�������ֱ�ӵ�¼
        CirculationLoginDlg SetDefaultAccount(
            string strServerUrl,
            string strTitle,
            string strComment,
            LoginFailCondition fail_contidion,
            IWin32Window owner,
            bool bLogin = true)
        {
            CirculationLoginDlg dlg = new CirculationLoginDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

            if (String.IsNullOrEmpty(strServerUrl) == true)
            {
                dlg.ServerUrl =
        AppInfo.GetString("config",
        "circulation_server_url",
        "http://localhost:8001/dp2library");
            }
            else
            {
                dlg.ServerUrl = strServerUrl;
            }

            if (owner == null)
                owner = this;

            if (String.IsNullOrEmpty(strTitle) == false)
                dlg.Text = strTitle;

            if (bLogin == false)
                dlg.SetDefaultMode = true;
            dlg.Comment = strComment;
            dlg.UserName = AppInfo.GetString(
                "default_account",
                "username",
                "");

            dlg.SavePasswordShort =
    AppInfo.GetBoolean(
    "default_account",
    "savepassword_short",
    false);

            dlg.SavePasswordLong =
                AppInfo.GetBoolean(
                "default_account",
                "savepassword_long",
                false);

            if (dlg.SavePasswordShort == true || dlg.SavePasswordLong == true)
            {
                dlg.Password = AppInfo.GetString(
        "default_account",
        "password",
        "");
                dlg.Password = this.DecryptPasssword(dlg.Password);
            }
            else
            {
                dlg.Password = "";
            }

            dlg.IsReader =
                AppInfo.GetBoolean(
                "default_account",
                "isreader",
                false);
            dlg.OperLocation = AppInfo.GetString(
                "default_account",
                "location",
                "");

            this.AppInfo.LinkFormState(dlg,
                "logindlg_state");

            if (fail_contidion == LoginFailCondition.PasswordError
                && dlg.SavePasswordShort == false
                && dlg.SavePasswordLong == false)
                dlg.AutoShowShortSavePasswordTip = true;

            dlg.ShowDialog(owner);

            this.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == DialogResult.Cancel)
            {
                return null;
            }

            // ����Ǽ������� dp2libraryXE �����棬����Ҫ������
            if (string.Compare(dlg.ServerUrl, CirculationLoginDlg.dp2LibraryXEServerUrl, true) == 0)
                AutoStartDp2libraryXE();

            AppInfo.SetString(
                "default_account",
                "username",
                dlg.UserName);
            AppInfo.SetString(
                "default_account",
                "password",
                (dlg.SavePasswordShort == true || dlg.SavePasswordLong == true) ?
                this.EncryptPassword(dlg.Password) : "");

            AppInfo.SetBoolean(
    "default_account",
    "savepassword_short",
    dlg.SavePasswordShort);

            AppInfo.SetBoolean(
                "default_account",
                "savepassword_long",
                dlg.SavePasswordLong);

            AppInfo.SetBoolean(
                "default_account",
                "isreader",
                dlg.IsReader);
            AppInfo.SetString(
                "default_account",
                "location",
                dlg.OperLocation);


            // 2006/12/30 new add
            AppInfo.SetString(
                "config",
                "circulation_server_url",
                dlg.ServerUrl);


            return dlg;
        }

        void AutoStartDp2libraryXE()
        {
            string strShortcutFilePath = PathUtil.GetShortcutFilePath("DigitalPlatform/dp2 V2/dp2Library XE");
            if (File.Exists(strShortcutFilePath) == false)
            {
                // ��װ������
                DialogResult result = MessageBox.Show(this,
"dp2libraryXE �ڱ�����δ��װ��\r\ndp2Circulation (����)�������� dp2LibraryXE ���������������Ҫ��װ����������ʹ�á�\r\n\r\n�Ƿ������� dp2003.com ���ذ�װ?",
"dp2Circulation",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (result == System.Windows.Forms.DialogResult.Yes)
                    FirstRunDialog.StartDp2libraryXe(
                        this,
                        "dp2Circulation",
                        this.Font,
                        false);
            }
            else
            {
                if (FirstRunDialog.HasDp2libraryXeStarted() == false)
                {
                    FirstRunDialog.StartDp2libraryXe(
                        this,
                        "dp2Circulation",
                        this.Font,
                        true);
                }
            }

            // �����ǰ����û������ǰ��
            {
                if (this.WindowState == FormWindowState.Minimized)
                    this.WindowState = FormWindowState.Normal;
                this.Activate();
                API.SetForegroundWindow(this.Handle);
            }
        }



        internal string DecryptPasssword(string strEncryptedText)
        {
            if (String.IsNullOrEmpty(strEncryptedText) == false)
            {
                try
                {
                    string strPassword = Cryptography.Decrypt(
        strEncryptedText,
        EncryptKey);
                return strPassword;
                }
                catch
                {
                    return "errorpassword";
                }
            }

            return "";
        }

        internal string EncryptPassword(string strPlainText)
        {
            return Cryptography.Encrypt(strPlainText, this.EncryptKey);
        }

        /// <summary>
        /// ��ǰ�Ƿ�ΪӦ���軹״̬
        /// </summary>
        public bool Urgent
        {
            get
            {
                return this.m_bUrgent;
            }
            set
            {
                this.m_bUrgent = value;
            }
        }

        // ��û����еĶ��߼�¼XML
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        internal int GetCachedReaderXml(string strReaderBarcode,
            string strConfirmReaderRecPath,
            out string strXml,
            out string strOutputPath,
            out string strError)
        {
            strXml = "";
            strOutputPath = "";
            strError = "";

            string strReaderBarcodeUnionPath = strReaderBarcode + "|" + strConfirmReaderRecPath;
            // ����cache���Ƿ��Ѿ�����
            StringCacheItem item = this.ReaderXmlCache.SearchItem(strReaderBarcodeUnionPath);
            if (item != null)
            {
                int nRet = item.Content.IndexOf("|");
                if (nRet == -1)
                    strXml = item.Content;
                else
                {
                    strOutputPath = item.Content.Substring(0, nRet);
                    strXml = item.Content.Substring(nRet + 1);
                }
                return 1;
            }

            return 0;
        }

        // ������߼�¼XML����
        internal void SetReaderXmlCache(string strReaderBarcode,
    string strConfirmReaderRecPath,
    string strXml,
            string strPath)
        {
            string strReaderBarcodeUnionPath = strReaderBarcode + "|" + strConfirmReaderRecPath;
            StringCacheItem item = this.SummaryCache.EnsureItem(strReaderBarcodeUnionPath);
            item.Content = strPath + "|" + strXml;
        }

        // 2014/9/20
        /// <summary>
        /// ��ö���ժҪ
        /// </summary>
        /// <param name="strPatronBarcode">����֤�����</param>
        /// <param name="bDisplayProgress">�Ƿ��ڽ���������ʾ</param>
        /// <returns></returns>
        public string GetReaderSummary(string strPatronBarcode,
            bool bDisplayProgress)
        {
            if (string.IsNullOrEmpty(strPatronBarcode) == true)
                return "";

            string strError = "";
            string strXml = "";
            string strOutputPath = "";

            // ��û����еĶ��߼�¼XML
            // return:
            //      -1  ����
            //      0   û���ҵ�
            //      1   �ҵ�
            int nRet = this.GetCachedReaderXml(strPatronBarcode,
                "",
out strXml,
out strOutputPath,
out strError);
            if (nRet == -1)
                return strError;

            if (nRet == 0)
            {
                nRet = PrepareSearch(bDisplayProgress);
                if (nRet == 0)
                    return "PrepareSearch() error";

                Stop.OnStop += new StopEventHandler(this.DoStop);
                if (bDisplayProgress == true)
                {
                    Stop.Initial("���ڻ�ö�����Ϣ '" + strPatronBarcode + "'...");
                    Stop.BeginLoop();
                }

                try
                {
                    string[] results = null;
                    byte[] baTimestamp = null;

                    long lRet = Channel.GetReaderInfo(Stop,
                        strPatronBarcode,
                        "xml",
                        out results,
                        out strOutputPath,
                        out baTimestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        return "!" + strError;
                    }
                    else if (lRet > 1)
                    {
                        strError = "!����֤����� " + strPatronBarcode + " ���ظ���¼ " + lRet.ToString() + "��";
                        return strError;
                    }
                    if (lRet == 0)
                    {
                        strError = "!֤�����Ϊ " + strPatronBarcode + " �Ķ��߼�¼û���ҵ�";
                        return strError;
                    }


                        Debug.Assert(results.Length > 0, "");
                        strXml = results[0];


                    // ���뵽����
                    this.SetReaderXmlCache(strPatronBarcode,
                        "",
                        strXml,
                        strOutputPath);

                }
                finally
                {
                    if (bDisplayProgress == true)
                    {
                        Stop.EndLoop();
                        Stop.Initial("");
                    }
                    Stop.OnStop -= new StopEventHandler(this.DoStop);

                    this.EndSearch();
                }
            }

            return Global.GetReaderSummary(strXml);
        }


        // ��û����е�bibliosummary
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        internal int GetCachedBiblioSummary(string strItemBarcode,
    string strConfirmItemRecPath,
    out string strSummary,
    out string strError)
        {
            strSummary = "";
            strError = "";

            string strItemBarcodeUnionPath = strItemBarcode + "|" + strConfirmItemRecPath;
            // ����cache���Ƿ��Ѿ�����
            StringCacheItem item = this.SummaryCache.SearchItem(strItemBarcodeUnionPath);
            if (item != null)
            {
                strSummary = item.Content;
                return 1;
            }

            return 0;
        }

        internal void SetBiblioSummaryCache(string strItemBarcode,
    string strConfirmItemRecPath,
    string strSummary)
        {
            string strItemBarcodeUnionPath = strItemBarcode + "|" + strConfirmItemRecPath;
            // ���cache��û�У������cache
            StringCacheItem item = this.SummaryCache.EnsureItem(strItemBarcodeUnionPath);
            item.Content = strSummary;
        }

        /// <summary>
        /// �����ĿժҪ
        /// </summary>
        /// <param name="strItemBarcode">�������</param>
        /// <param name="strConfirmItemRecPath">����ȷ�ϵĲ��¼·��</param>
        /// <param name="bDisplayProgress">�Ƿ��ڽ���������ʾ</param>
        /// <param name="strSummary">������ĿժҪ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int GetBiblioSummary(string strItemBarcode,
            string strConfirmItemRecPath,
            bool bDisplayProgress,
            out string strSummary,
            out string strError)
        {
            /*
            // ����
            strSummary = "...";
            strError = "";
            return 0;
             * */

            string strItemBarcodeUnionPath = strItemBarcode + "|" + strConfirmItemRecPath;
            // ����cache���Ƿ��Ѿ�����
            StringCacheItem item = this.SummaryCache.SearchItem(strItemBarcodeUnionPath);
            if (item != null)
            {
                strError = "";
                strSummary = item.Content;
                return 0;
            }

            int nRet = PrepareSearch(bDisplayProgress);
            if (nRet == 0)
            {
                strError = "PrepareSearch() error";
                strSummary = strError;
                return -1;  // 2009/2/11
            }


            Stop.OnStop += new StopEventHandler(this.DoStop);
            if (bDisplayProgress == true)
            {
                Stop.Initial("MainForm���ڻ����ĿժҪ '" + strItemBarcodeUnionPath + "'...");
                Stop.BeginLoop();
            }

            try
            {

                string strBiblioRecPath = "";

                // ��Ϊ������ֻ��һ��Channelͨ��������Ҫ����ʹ��
                this.m_lockChannel.AcquireWriterLock(m_nLockTimeout);
                try
                {

                    long lRet = Channel.GetBiblioSummary(
                        Stop,
                        strItemBarcode,
                        strConfirmItemRecPath,
                        null,
                        out strBiblioRecPath,
                        out strSummary,
                        out strError);
                    if (lRet == -1)
                    {
                        return -1;
                    }

                }
                catch
                {
                    strSummary = "��Channel��������ʧ��...";
                    strError = "��Channel��������ʧ��...";
                    return -1;
                }
                finally
                {
                    this.m_lockChannel.ReleaseWriterLock();
                }

                // ���cache��û�У������cache
                item = this.SummaryCache.EnsureItem(strItemBarcodeUnionPath);
                item.Content = strSummary;

            }
            finally
            {
                if (bDisplayProgress == true)
                {
                    Stop.EndLoop();
                    Stop.Initial("");
                }
                Stop.OnStop -= new StopEventHandler(this.DoStop);

                this.EndSearch();   // BUG !!! 2012/3/28ǰ����һ��
            }

            return 0;
        }

#if NOOOOOOOOOOOOOOO
        // �Ѿ���stopŪ������ʾ��������ë��
        public void ShowProgress(bool bVisible)
        {
            this.toolStripProgressBar_main.Visible = bVisible;
        }

        public void SetProgressValue(int nValue)
        {
            this.toolStripProgressBar_main.Value = nValue;
        }

        public void SetProgressRange(int nMax)
        {
            this.toolStripProgressBar_main.Minimum = 0;
            this.toolStripProgressBar_main.Maximum = nMax;
        }
#endif

        private void MenuItem_copyright_Click(object sender, EventArgs e)
        {
            CopyrightDlg dlg = new CopyrightDlg();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.MainForm = this;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);
        }

        /*
* ���������¼�Ƿ��ʺϽ���Ŀ���ϵ
* 1) Դ�⣬Ӧ���ǲɹ�������
* 2) Դ���Ŀ��⣬����һ����
* 3) Դ���syntax��Ŀ����syntax����һ����
* 4) Ŀ��ⲻӦ������Դ��ɫ
* */
        // parameters:
        //      strSourceRecPath    ��¼ID����Ϊ�ʺ�
        //      strTargetRecPath    ��¼ID����Ϊ�ʺţ�����bCheckTargetWenhao==falseʱ
        // return:
        //      -1  ����
        //      0   ���ʺϽ���Ŀ���ϵ (���������û��ʲô�����ǲ��ʺϽ���)
        //      1   �ʺϽ���Ŀ���ϵ
        internal int CheckBuildLinkCondition(string strSourceBiblioRecPath,
            string strTargetBiblioRecPath,
            bool bCheckTargetWenhao,
            out string strError)
        {
            strError = "";

            // TODO: ��ü��һ�����·���ĸ�ʽ���Ϸ�����Ŀ����������MainForm���ҵ�

            // ����ǲ�����Ŀ������MARC��ʽ�Ƿ�͵�ǰ���ݿ�һ�¡������ǵ�ǰ��¼�Լ�
            string strTargetDbName = Global.GetDbName(strTargetBiblioRecPath);
            string strTargetRecordID = Global.GetRecordID(strTargetBiblioRecPath);

            if (String.IsNullOrEmpty(strTargetDbName) == true
                || String.IsNullOrEmpty(strTargetRecordID) == true)
            {
                strError = "Ŀ���¼·�� '" + strTargetBiblioRecPath + "' ���ǺϷ��ļ�¼·��";
                goto ERROR1;
            }

            // 2009/11/25 new add
            if (this.IsBiblioSourceDb(strTargetDbName) == true)
            {
                strError = "�� '" + strTargetDbName + "' �� ��Դ��Ŀ�� ��ɫ��������ΪĿ���";
                return 0;
            }

            // 2011/11/29
            if (this.IsOrderWorkDb(strTargetDbName) == true)
            {
                strError = "�� '" + strTargetDbName + "' �� �ɹ������� ��ɫ��������ΪĿ���";
                return 0;
            }

            string strSourceDbName = Global.GetDbName(strSourceBiblioRecPath);
            string strSourceRecordID = Global.GetRecordID(strSourceBiblioRecPath);

            if (String.IsNullOrEmpty(strSourceDbName) == true
                || String.IsNullOrEmpty(strSourceRecordID) == true)
            {
                strError = "Դ��¼·�� '" + strSourceBiblioRecPath + "' ���ǺϷ��ļ�¼·��";
                goto ERROR1;
            }

            /*
            if (this.IsOrderWorkDb(strSourceDbName) == false)
            {
                strError = "Դ�� '" + strSourceDbName + "' ���߱� �ɹ������� ��ɫ";
                return 0;
            }*/

            // ������Ŀ�������MARC��ʽ�﷨��
            // return:
            //      null    û���ҵ�ָ������Ŀ����
            string strSourceSyntax = this.GetBiblioSyntax(strSourceDbName);
            if (String.IsNullOrEmpty(strSourceSyntax) == true)
                strSourceSyntax = "unimarc";
            string strSourceIssueDbName = this.GetIssueDbName(strSourceDbName);


            bool bFound = false;
            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty prop = this.BiblioDbProperties[i];

                    if (prop.DbName == strTargetDbName)
                    {
                        bFound = true;

                        string strTargetSyntax = prop.Syntax;
                        if (String.IsNullOrEmpty(strTargetSyntax) == true)
                            strTargetSyntax = "unimarc";

                        if (strTargetSyntax != strSourceSyntax)
                        {
                            strError = "�����õ�Ŀ���¼������Ŀ���ݸ�ʽΪ '" + strTargetSyntax + "'����Դ��¼����Ŀ���ݸ�ʽ '" + strSourceSyntax + "' ��һ�£���˲������ܾ�";
                            return 0;
                        }

                        if (String.IsNullOrEmpty(prop.IssueDbName)
                            != String.IsNullOrEmpty(strSourceIssueDbName))
                        {
                            strError = "�����õ�Ŀ���¼������Ŀ�� '" + strTargetDbName + "' ��������(�ڿ�����ͼ��)��Դ��¼����Ŀ�� '" + strSourceDbName + "' ��һ�£���˲������ܾ�";
                            return 0;
                        }
                    }
                }
            }

            if (bFound == false)
            {
                strError = "'" + strTargetDbName + "' ���ǺϷ�����Ŀ����";
                goto ERROR1;
            }

            // source

            if (this.IsBiblioDbName(strSourceDbName) == false)
            {
                strError = "'" + strSourceDbName + "' ���ǺϷ�����Ŀ����";
                goto ERROR1;
            }

            if (strSourceRecordID == "?")
            {
                /* Դ��¼ID����Ϊ�ʺţ���Ϊ�ⲻ�����������ӹ�ϵ
                strError = "Դ��¼ '"+strSourceBiblioRecPath+"' ·����ID����Ϊ�ʺ�";
                return 0;
                 * */
            }
            else
            {
                // ��������ʺţ���Ҫ���һ���ˣ�û����
                if (Global.IsPureNumber(strSourceRecordID) == false)
                {
                    strError = "Դ��¼  '" + strSourceBiblioRecPath + "' ·����ID���ֱ���Ϊ������";
                    goto ERROR1;
                }
            }

            // target
            if (strTargetRecordID == "?")
            {
                if (bCheckTargetWenhao == true)
                {
                    strError = "Ŀ���¼ '"+strTargetBiblioRecPath+"' ·����ID����Ϊ�ʺ�";
                    return 0;
                }
            }
            else
            {
                if (Global.IsPureNumber(strTargetRecordID) == false)
                {
                    strError = "Ŀ���¼ '" + strTargetBiblioRecPath + "' ·����ID���ֱ���Ϊ������";
                    goto ERROR1;
                }
            }

            if (strTargetDbName == strSourceDbName)
            {
                strError = "Ŀ���¼��Դ��¼��������ͬһ����Ŀ�� '"+strTargetBiblioRecPath+"'";
                return 0;
                // ע�������Ͳ��ü��Ŀ���Ƿ�Դ��¼��
            }

            return 1;
        ERROR1:
            return -1;
        }


        // 
        // return:
        //      null    û���ҵ�ָ������Ŀ����
        /// <summary>
        /// ������Ŀ������� MARC ��ʽ�﷨��
        /// </summary>
        /// <param name="strBiblioDbName">��Ŀ����</param>
        /// <returns>�﷨�������Ϊ null ��ʾû���ҵ�</returns>
        public string GetBiblioSyntax(string strBiblioDbName)
        {
            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].DbName == strBiblioDbName)
                        return this.BiblioDbProperties[i].Syntax;
                }
            }

            return null;
        }

        // 
        // return:
        //      null    û���ҵ�ָ������Ŀ����
        /// <summary>
        /// ���ݶ��߿�����ùݴ���
        /// </summary>
        /// <param name="strReaderDbName">���߿���</param>
        /// <returns>�ܴ���</returns>
        public string GetReaderDbLibraryCode(string strReaderDbName)
        {
            if (this.ReaderDbProperties != null)
            {
                foreach(ReaderDbProperty prop in this.ReaderDbProperties)
                {
                    if (prop.DbName == strReaderDbName)
                        return prop.LibraryCode;
                }
            }

            return null;
        }

        // 2013/6/15
        // 
        /// <summary>
        /// ���ȫ�����õĹݴ���
        /// </summary>
        /// <returns>�ַ�������</returns>
        public List<string> GetAllLibraryCode()
        {
            List<string> results = new List<string>();
            if (this.ReaderDbProperties != null)
            {
                foreach (ReaderDbProperty prop in this.ReaderDbProperties)
                {
                    results.Add(prop.LibraryCode);
                }

                results.Sort();
                StringUtil.RemoveDup(ref results);
            }

            return results;
        }

        // 
        /// <summary>
        /// �ж�һ�������Ƿ�Ϊ�Ϸ�����Ŀ����
        /// </summary>
        /// <param name="strDbName">���ݿ���</param>
        /// <returns>�Ƿ�Ϊ�Ϸ�����Ŀ����</returns>
        public bool IsValidBiblioDbName(string strDbName)
        {
            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].DbName == strDbName)
                        return true;
                }
            }

            return false;
        }

        // 
        // �������""����ʾ����Ŀ���������û�ж���
        /// <summary>
        /// ������Ŀ������ö�Ӧ����������
        /// </summary>
        /// <param name="strBiblioDbName">��Ŀ����</param>
        /// <param name="strDbType">�����������</param>
        /// <returns>�������������Ϊ null����ʾ��Ŀ��û���ҵ������Ϊ ""����ʾ����Ŀ�ⲻ�߱������͵�������</returns>
        public string GetItemDbName(string strBiblioDbName,
            string strDbType)
        {
            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].DbName == strBiblioDbName)
                    {
                        if (strDbType == "item")
                            return this.BiblioDbProperties[i].ItemDbName;
                        else if (strDbType == "order")
                            return this.BiblioDbProperties[i].OrderDbName;
                        else if (strDbType == "issue")
                            return this.BiblioDbProperties[i].IssueDbName;
                        else if (strDbType == "comment")
                            return this.BiblioDbProperties[i].CommentDbName;
                        else
                            return "";
                    }
                }
            }

            return null;
        }

        // 
        // �������""����ʾ����Ŀ���ʵ���û�ж���
        /// <summary>
        /// ������Ŀ������ö�Ӧ��ʵ�����
        /// </summary>
        /// <param name="strBiblioDbName">��Ŀ����</param>
        /// <returns>ʵ����������Ϊ null����ʾ��Ŀ��û���ҵ������Ϊ ""����ʾ����Ŀ�ⲻ�߱�ʵ���</returns>
        public string GetItemDbName(string strBiblioDbName)
        {
            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].DbName == strBiblioDbName)
                        return this.BiblioDbProperties[i].ItemDbName;
                }
            }

            return null;
        }

        // 
        // �������""����ʾ����Ŀ����ڿ�û�ж���
        /// <summary>
        /// ������Ŀ������ö�Ӧ���ڿ���
        /// </summary>
        /// <param name="strBiblioDbName">��Ŀ����</param>
        /// <returns>�ڿ��������Ϊ null����ʾ��Ŀ��û���ҵ������Ϊ ""����ʾ����Ŀ�ⲻ�߱��ڿ�</returns>
        public string GetIssueDbName(string strBiblioDbName)
        {
            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].DbName == strBiblioDbName)
                        return this.BiblioDbProperties[i].IssueDbName;
                }
            }

            return null;
        }

        // 
        // �������""����ʾ����Ŀ��Ķ�����û�ж���
        /// <summary>
        /// ������Ŀ������ö�Ӧ�Ķ�������
        /// </summary>
        /// <param name="strBiblioDbName">��Ŀ����</param>
        /// <returns>�������������Ϊ null����ʾ��Ŀ��û���ҵ������Ϊ ""����ʾ����Ŀ�ⲻ�߱�������</returns>
        public string GetOrderDbName(string strBiblioDbName)
        {
            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].DbName == strBiblioDbName)
                        return this.BiblioDbProperties[i].OrderDbName;
                }
            }

            return null;
        }

        // 
        // �������""����ʾ����Ŀ�����ע��û�ж���
        /// <summary>
        /// ������Ŀ������ö�Ӧ����ע����
        /// </summary>
        /// <param name="strBiblioDbName">��Ŀ����</param>
        /// <returns>��ע���������Ϊ null����ʾ��Ŀ��û���ҵ������Ϊ ""����ʾ����Ŀ�ⲻ�߱���ע��</returns>
        public string GetCommentDbName(string strBiblioDbName)
        {
            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].DbName == strBiblioDbName)
                        return this.BiblioDbProperties[i].CommentDbName;
                }
            }

            return null;
        }

        // ʵ����� --> �ڿ�/ͼ������ ���ձ�
        Hashtable itemdb_type_table = new Hashtable();

        // 
        // return:
        //      -1  ����ʵ���
        //      0   ͼ������
        //      1   �ڿ�����
        /// <summary>
        /// �۲�ʵ����ǲ����ڿ�������
        /// </summary>
        /// <param name="strItemDbName">ʵ�����</param>
        /// <returns>-1: ����ʵ���; 0: ͼ������; 1: �ڿ�����</returns>
        public int IsSeriesTypeFromItemDbName(string strItemDbName)
        {
            int nRet = 0;
            object o = itemdb_type_table[strItemDbName];
            if (o != null)
            {
                nRet = (int)o;
                return nRet;
            }

            string strBiblioDbName = GetBiblioDbNameFromItemDbName(strItemDbName);
            if (strBiblioDbName == null)
                return -1;
            string strIssueDbName = GetIssueDbName(strBiblioDbName);
            if (string.IsNullOrEmpty(strIssueDbName) == true)
                nRet = 0;
            else
                nRet = 1;

            itemdb_type_table[strItemDbName] = nRet;
            return nRet;
        }

        // 
        /// <summary>
        /// ����ʵ�������ö�Ӧ����Ŀ����
        /// </summary>
        /// <param name="strItemDbName">ʵ�����</param>
        /// <returns>��Ŀ����</returns>
        public string GetBiblioDbNameFromItemDbName(string strItemDbName)
        {
            // 2008/11/28 new add
            // ʵ�����Ϊ�գ��޷�����Ŀ������
            // ��ʵҲ�����ң������ҳ����ľͲ���Ψһ���ˣ����Ըɴ಻��
            if (String.IsNullOrEmpty(strItemDbName) == true)
                return null;

            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].ItemDbName == strItemDbName)
                        return this.BiblioDbProperties[i].DbName;
                }
            }

            return null;
        }

        // ���� ��/����/��/��ע ��¼·���� parentid ��������������Ŀ��¼·��
        public string BuildBiblioRecPath(string strDbType,
            string strItemRecPath,
            string strParentID)
        {
            if (string.IsNullOrEmpty(strParentID) == true)
                return null;

            string strItemDbName = Global.GetDbName(strItemRecPath);
            if (string.IsNullOrEmpty(strItemDbName) == true)
                return null;

            string strBiblioDbName = this.GetBiblioDbNameFromItemDbName(strDbType, strItemDbName);
            if (string.IsNullOrEmpty(strBiblioDbName) == true)
                return null;

            return  strBiblioDbName + "/" + strParentID;
        }

        /// <summary>
        /// ����ʵ��(��/����/��ע)������ö�Ӧ����Ŀ����
        /// </summary>
        /// <param name="strDbType">���ݿ�����</param>
        /// <param name="strItemDbName">���ݿ���</param>
        /// <returns>��Ŀ����</returns>
        public string GetBiblioDbNameFromItemDbName(string strDbType,
            string strItemDbName)
        {
            if (String.IsNullOrEmpty(strItemDbName) == true)
                return null;

            if (this.BiblioDbProperties != null)
            {
                if (strDbType == "item")
                {
                    foreach (BiblioDbProperty prop in this.BiblioDbProperties)
                    {
                        if (prop.ItemDbName == strItemDbName)
                            return prop.DbName;
                    }
                }
                else if (strDbType == "order")
                {
                    foreach (BiblioDbProperty prop in this.BiblioDbProperties)
                    {
                        if (prop.OrderDbName == strItemDbName)
                            return prop.DbName;
                    }
                }
                else if (strDbType == "issue")
                {
                    foreach (BiblioDbProperty prop in this.BiblioDbProperties)
                    {
                        if (prop.IssueDbName == strItemDbName)
                            return prop.DbName;
                    }
                }
                else if (strDbType == "comment")
                {
                    foreach (BiblioDbProperty prop in this.BiblioDbProperties)
                    {
                        if (prop.CommentDbName == strItemDbName)
                            return prop.DbName;
                    }
                }
                else
                    throw new Exception("�޷��������ݿ����� '"+strDbType+"'");
            }

            return null;
        }

        // ������ --> �ڿ�/ͼ������ ���ձ�
        Hashtable orderdb_type_table = new Hashtable();

        // 
        // return:
        //      -1  ���Ƕ�����
        //      0   ͼ������
        //      1   �ڿ�����
        /// <summary>
        /// �۲충�����ǲ����ڿ�������
        /// </summary>
        /// <param name="strOrderDbName">��������</param>
        /// <returns>-1: ���Ƕ�����; 0: ͼ������; 1: �ڿ�����</returns>
        public int IsSeriesTypeFromOrderDbName(string strOrderDbName)
        {
            int nRet = 0;
            object o = orderdb_type_table[strOrderDbName];
            if (o != null)
            {
                nRet = (int)o;
                return nRet;
            }

            string strBiblioDbName = GetBiblioDbNameFromOrderDbName(strOrderDbName);
            if (strBiblioDbName == null)
                return -1;
            string strIssueDbName = GetIssueDbName(strBiblioDbName);
            if (string.IsNullOrEmpty(strIssueDbName) == true)
                nRet = 0;
            else
                nRet = 1;

            orderdb_type_table[strOrderDbName] = nRet;
            return nRet;
        }

        // 
        /// <summary>
        /// �����ڿ�����ö�Ӧ����Ŀ����
        /// </summary>
        /// <param name="strIssueDbName">�ڿ���</param>
        /// <returns>��Ŀ����</returns>
        public string GetBiblioDbNameFromIssueDbName(string strIssueDbName)
        {
            // 2008/11/28 new add
            // �ڿ���Ϊ�գ��޷�����Ŀ������
            // ��ʵҲ�����ң������ҳ����ľͲ���Ψһ���ˣ����Ըɴ಻��
            if (String.IsNullOrEmpty(strIssueDbName) == true)
                return null;


            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].IssueDbName == strIssueDbName)
                        return this.BiblioDbProperties[i].DbName;
                }
            }

            return null;
        }

        // 
        /// <summary>
        /// ���ݶ���������ö�Ӧ����Ŀ����
        /// </summary>
        /// <param name="strOrderDbName">��������</param>
        /// <returns>��Ŀ����</returns>
        public string GetBiblioDbNameFromOrderDbName(string strOrderDbName)
        {
            // 2008/11/28 new add
            // ��������Ϊ�գ��޷�����Ŀ������
            // ��ʵҲ�����ң������ҳ����ľͲ���Ψһ���ˣ����Ըɴ಻��
            if (String.IsNullOrEmpty(strOrderDbName) == true)
                return null;

            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].OrderDbName == strOrderDbName)
                        return this.BiblioDbProperties[i].DbName;
                }
            }

            return null;
        }

        // 
        /// <summary>
        /// ������ע������ö�Ӧ����Ŀ����
        /// </summary>
        /// <param name="strCommentDbName">��ע����</param>
        /// <returns>��Ŀ����</returns>
        public string GetBiblioDbNameFromCommentDbName(string strCommentDbName)
        {
            if (String.IsNullOrEmpty(strCommentDbName) == true)
                return null;

            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].CommentDbName == strCommentDbName)
                        return this.BiblioDbProperties[i].DbName;
                }
            }

            return null;
        }


        // 2009/11/25
        // 
        /// <summary>
        /// �Ƿ�Ϊ��Դ��Ŀ��?
        /// </summary>
        /// <param name="strBiblioDbName">��Ŀ����</param>
        /// <returns>�Ƿ�Ϊ��Դ��Ŀ��</returns>
        public bool IsBiblioSourceDb(string strBiblioDbName)
        {
            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty prop = this.BiblioDbProperties[i];

                    if (prop.DbName == strBiblioDbName)
                    {
                        if (StringUtil.IsInList("biblioSource", prop.Role) == true)
                            return true;
                    }
                }
            }

            return false;
        }

        // 2009/10/24
        // 
        /// <summary>
        /// �Ƿ�Ϊ�ɹ�������?
        /// </summary>
        /// <param name="strBiblioDbName">��Ŀ����</param>
        /// <returns>�Ƿ�Ϊ�ɹ�������</returns>
        public bool IsOrderWorkDb(string strBiblioDbName)
        {
            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty prop = this.BiblioDbProperties[i];

                    if (prop.DbName == strBiblioDbName)
                    {
                        if (StringUtil.IsInList("orderWork", prop.Role) == true)
                            return true;
                    }
                }
            }

            return false;
        }

        // 2008/12/15
        // 
        /// <summary>
        /// �Ƿ�Ϊ��Ŀ����?
        /// </summary>
        /// <param name="strDbName">���ݿ���</param>
        /// <returns>�Ƿ�Ϊ��Ŀ����</returns>
        public bool IsBiblioDbName(string strDbName)
        {
            if (String.IsNullOrEmpty(strDbName) == true)
                return false;

            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].DbName == strDbName)
                        return true;
                }
            }

            return false;
        }

        // 2012/9/1
        /// <summary>
        /// ���һ����Ŀ���������Ϣ
        /// </summary>
        /// <param name="strDbName">��Ŀ����</param>
        /// <returns>������Ϣ����</returns>
        public BiblioDbProperty GetBiblioDbProperty(string strDbName)
        {
            if (String.IsNullOrEmpty(strDbName) == true)
                return null;

            if (this.BiblioDbProperties != null)
            {
                foreach (BiblioDbProperty prop in this.BiblioDbProperties)
                {
                    if (prop.DbName == strDbName)
                        return prop;
                }
            }

            return null;
        }

        // 2008/12/15
        // 
        /// <summary>
        /// �Ƿ�Ϊʵ�����?
        /// </summary>
        /// <param name="strDbName">���ݿ���</param>
        /// <returns>�Ƿ�Ϊʵ�����</returns>
        public bool IsItemDbName(string strDbName)
        {
            if (String.IsNullOrEmpty(strDbName) == true)
                return false;

            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].ItemDbName == strDbName)
                        return true;
                }
            }

            return false;
        }

        // 2008/12/15
        // 
        /// <summary>
        /// �Ƿ�Ϊ���߿���?
        /// </summary>
        /// <param name="strDbName">���ݿ���</param>
        /// <returns>�Ƿ�Ϊ���߿���</returns>
        public bool IsReaderDbName(string strDbName)
        {
            if (String.IsNullOrEmpty(strDbName) == true)
                return false;

            if (this.ReaderDbNames != null)    // 2009/3/29 new add
            {
                for (int i = 0; i < this.ReaderDbNames.Length; i++)
                {
                    if (this.ReaderDbNames[i] == strDbName)
                        return true;
                }
            }

            return false;
        }

        // 
        /// <summary>
        /// �Ƿ�Ϊ��������?
        /// </summary>
        /// <param name="strDbName">���ݿ���</param>
        /// <returns>�Ƿ�Ϊ��������</returns>
        public bool IsOrderDbName(string strDbName)
        {
            if (String.IsNullOrEmpty(strDbName) == true)
                return false;

            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].OrderDbName == strDbName)
                        return true;
                }
            }

            return false;
        }

        // 
        /// <summary>
        /// �Ƿ�Ϊ�ڿ���?
        /// </summary>
        /// <param name="strDbName">���ݿ���</param>
        /// <returns>�Ƿ�Ϊ�ڿ���</returns>
        public bool IsIssueDbName(string strDbName)
        {
            if (String.IsNullOrEmpty(strDbName) == true)
                return false;

            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].IssueDbName == strDbName)
                        return true;
                }
            }

            return false;
        }

        // 
        /// <summary>
        /// �Ƿ�Ϊ��ע����?
        /// </summary>
        /// <param name="strDbName">���ݿ���</param>
        /// <returns>�Ƿ�Ϊ��ע����</returns>
        public bool IsCommentDbName(string strDbName)
        {
            if (String.IsNullOrEmpty(strDbName) == true)
                return false;

            if (this.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    if (this.BiblioDbProperties[i].CommentDbName == strDbName)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ��ȡ�����ÿ�ܴ���״̬�е�������Ϣ
        /// </summary>
        public string StatusBarMessage
        {
            get
            {
                return toolStripStatusLabel_main.Text;
            }
            set
            {
                toolStripStatusLabel_main.Text = value;
            }
        }


#if NO
        // ���������ļ�
        public int DownloadDataFile(string strFileName,
            out string strError)
        {
            strError = "";

            WebClient webClient = new WebClient();

            // TODO: �Ƿ�����dp2003.cn����?
            string strUrl = "http://dp2003.com/dp2Circulation/" + strFileName;
            string strLocalFileName = this.DataDir + "\\" + strFileName;
            try
            {
                webClient.DownloadFile(strUrl,
                    strLocalFileName);
            }
            catch (Exception ex)
            {
                strError = "����" + strFileName + "�ļ��������� :" + ex.Message;
                return -1;
            }

            strError = "����" + strFileName + "�ļ��ɹ� :\r\n" + strUrl + " --> " + strLocalFileName;
            return 0;
        }
#endif

        // 
        /// <summary>
        /// ���������ļ�
        /// �� http://dp2003.com/dp2Circulation/ λ�����ص�����Ŀ¼���ļ������ֲ���
        /// </summary>
        /// <param name="strFileName">���ļ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int DownloadDataFile(string strFileName,
            out string strError)
        {
            strError = "";

            string strUrl = "http://dp2003.com/dp2Circulation/" + strFileName;
            string strLocalFileName = this.DataDir + "\\" + strFileName;
            string strTempFileName = this.DataDir + "\\~temp_download_webfile";

            int nRet = WebFileDownloadDialog.DownloadWebFile(
                this,
                strUrl,
                strLocalFileName,
                strTempFileName,
                out strError);
            if (nRet == -1)
                return -1;
            strError = "����" + strFileName + "�ļ��ɹ� :\r\n" + strUrl + " --> " + strLocalFileName;
            return 0;
        }

        /// <summary>
        /// װ�ؿ��ټ�ƴ����Ҫ�ĸ�����Ϣ
        /// </summary>
        /// <param name="bAutoDownload">�Ƿ��Զ��� dp2003.com �����������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����ǰ�Ѿ�װ��; 1�����ļ�װ��</returns>
        public int LoadQuickPinyin(bool bAutoDownload,
            out string strError)
        {
            strError = "";

            // �Ż�
            if (this.QuickPinyin != null)
                return 0;

        REDO:

            try
            {
                this.QuickPinyin = new QuickPinyin(this.DataDir + "\\pinyin.xml");
            }
            catch (FileNotFoundException ex)
            {
                strError = "װ�ر���ƴ���ļ��������� :" + ex.Message;

                if (bAutoDownload == true)
                {
                    string strError1 = "";
                    int nRet = this.DownloadDataFile("pinyin.xml",
                        out strError1);
                    if (nRet == -1)
                    {
                        strError = strError + "\r\n�Զ������ļ���\r\n" + strError1;
                        return -1;
                    }
                    goto REDO;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                strError = "װ�ر���ƴ���ļ��������� :" + ex.Message;
                return -1;
            }

            return 1;
        }

        /// <summary>
        /// װ�ؿ��ر���Ϣ
        /// </summary>
        /// <param name="bAutoDownload">�Ƿ��Զ��� dp2003.com �����������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����ǰ�Ѿ�װ��; 1�����ļ�װ��</returns>
        public int LoadQuickCutter(bool bAutoDownload,
            out string strError)
        {
            strError = "";

            // �Ż�
            if (this.QuickCutter != null)
                return 0;

        REDO:

            try
            {
                this.QuickCutter = new QuickCutter(this.DataDir + "\\cutter.xml");
            }
            catch (FileNotFoundException ex)
            {
                strError = "װ�ر��ؿ��ر��ļ��������� :" + ex.Message;

                if (bAutoDownload == true)
                {
                    string strError1 = "";
                    int nRet = this.DownloadDataFile("cutter.xml",
                        out strError1);
                    if (nRet == -1)
                    {
                        strError = strError + "\r\n�Զ������ļ���\r\n" + strError1;
                        return -1;
                    }
                    goto REDO;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                strError = "װ�ر��ؿ��ر��ļ��������� :" + ex.Message;
                return -1;
            }

            return 1;
        }

        /// <summary>
        /// װ���ĽǺ�����Ϣ
        /// </summary>
        /// <param name="bAutoDownload">�Ƿ��Զ��� dp2003.com �����������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����ǰ�Ѿ�װ��; 1�����ļ�װ��</returns>
        public int LoadQuickSjhm(bool bAutoDownload,
            out string strError)
        {
            strError = "";

            // �Ż�
            if (this.QuickSjhm != null)
                return 0;

        REDO:

            try
            {
                this.QuickSjhm = new QuickSjhm(this.DataDir + "\\sjhm.xml");
            }
            catch (FileNotFoundException ex)
            {
                strError = "װ�ر����ĽǺ����ļ��������� :" + ex.Message;

                if (bAutoDownload == true)
                {
                    string strError1 = "";
                    int nRet = this.DownloadDataFile("sjhm.xml",
                        out strError1);
                    if (nRet == -1)
                    {
                        strError = strError + "\r\n�Զ������ļ���\r\n" + strError1;
                        return -1;
                    }
                    goto REDO;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                strError = "װ�ر����ĽǺ����ļ��������� :" + ex.Message;
                return -1;
            }

            return 1;
        }

        /// <summary>
        /// װ�� ISBN �и���Ϣ
        /// </summary>
        /// <param name="bAutoDownload">�Ƿ��Զ��� dp2003.com �����������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����ǰ�Ѿ�װ��; 1�����ļ�װ��</returns>
        public int LoadIsbnSplitter(bool bAutoDownload,
            out string strError)
        {
            strError = "";

            // �Ż�
            if (this.IsbnSplitter != null)
                return 0;

        REDO:

            try
            {
                this.IsbnSplitter = new IsbnSplitter(this.DataDir + "\\rangemessage.xml");  // "\\isbn.xml"
            }
            catch (FileNotFoundException ex)
            {
                strError = "װ�ر��� isbn �����ļ� rangemessage.xml �������� :" + ex.Message;

                if (bAutoDownload == true)
                {
                    string strError1 = "";
                    int nRet = this.DownloadDataFile("rangemessage.xml",    // "isbn.xml"
                        out strError1);
                    if (nRet == -1)
                    {
                        strError = strError + "\r\n�Զ������ļ���\r\n" + strError1;
                        return -1;
                    }
                    goto REDO;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                strError = "װ�ر���isbn�����ļ��������� :" + ex.Message;
                return -1;
            }

            return 1;
        }

        // 
        /// <summary>
        /// ���ISBNʵ�ÿ�Ŀ���
        /// </summary>
        /// <returns>ISBNʵ�ÿ�Ŀ���</returns>
        public string GetPublisherUtilDbName()
        {
            if (this.UtilDbProperties == null)
                return null;    // not found

            for (int i = 0; i < this.UtilDbProperties.Count; i++)
            {
                UtilDbProperty property = this.UtilDbProperties[i];

                if (property.Type == "publisher")
                    return property.DbName;
            }

            return null;    // not found
        }

        // ��ISBN����ȡ�ó�����Ų���
        // �����������Զ���Ӧ��978ǰ׺������ISBN��
        // ISBN�����޺��ʱ���������Զ��ȼӺ��Ȼ����ȡ�ó������
        // parameters:
        //      strPublisherNumber  ��������롣������978-����
        /// <summary>
        /// �� ISBN ����ȡ�ó�����Ų���
        /// �����������Զ���Ӧ�� 978 ǰ׺������ ISBN ��
        /// ISBN �����޺��ʱ���������Զ��ȼӺ��Ȼ����ȡ�ó������
        /// </summary>
        /// <param name="strISBN">ISBN ���ַ���</param>
        /// <param name="strPublisherNumber">���س�������벿�֡������� 978- ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int GetPublisherNumber(string strISBN,
            out string strPublisherNumber,
            out string strError)
        {
            strPublisherNumber = "";
            strError = "";

            int nRet = strISBN.IndexOf("-");
            if (nRet == -1)
            {

                nRet = this.LoadIsbnSplitter(true, out strError);
                if (nRet == -1)
                {
                    strError = "��ȡ�������ǰ������ISBN����û�к�ܣ��ڼ����ܵĹ����У����ִ���: " + strError;
                    return -1;
                }

                string strResult = "";

                nRet = this.IsbnSplitter.IsbnInsertHyphen(strISBN,
                    "force10",  // ���ڳ���������ISBN��������978ǰ׺
                    out strResult,
                    out strError);
                if (nRet == -1)
                {
                    strError = "��ȡ�������ǰ������ISBN����û�к�ܣ��ڼ����ܵĹ����У����ִ���: " + strError;
                    return -1;
                }

                strISBN = strResult;
            }

            return Global.GetPublisherNumber(strISBN,
                out strPublisherNumber,
                out strError);
        }

        // ����ָ���λ��
        internal void SaveSplitterPos(SplitContainer container,
            string strSection,
            string strEntry)
        {
            if (container.ParentForm != null
                && container.ParentForm.WindowState == FormWindowState.Minimized)
            {
                container.ParentForm.WindowState = FormWindowState.Normal;  // 2012/3/16
                // TODO: ֱ�ӷ��أ������棿
                // Debug.Assert(false, "SaveSplitterPos()Ӧ���ڴ���Ϊ��Minimized״̬�µ���");
            }

            float fValue = (float)container.SplitterDistance / 
                (
                container.Orientation == Orientation.Horizontal ?
                (float)container.Height
                :
                (float)container.Width
                )
                ;
            this.AppInfo.SetFloat(
                strSection,
                strEntry,
                fValue);

        }

        // ��ò����÷ָ���λ��
        internal void LoadSplitterPos(SplitContainer container,
            string strSection,
            string strEntry)
        {
            float fValue = this.AppInfo.GetFloat(
                strSection,
                strEntry,
                (float)0);
            if (fValue == 0)
                return;

            try
            {
                container.SplitterDistance = (int)Math.Ceiling(
                (
                container.Orientation == Orientation.Horizontal ?
                (float)container.Height
                :
                (float)container.Width
                ) 
                * fValue);
            }
            catch
            {
            }
        }

        // ���ɽ軹������API�Ƿ���Ҫ����item xml���ݣ�
        // ����Ϊ��PrintHost�и���item xmlʵ�ֶ��ֹ��ܵ���Ҫ�����򿪡��������Ч�ʿ��ǣ��ر�
        // Ŀǰ��û�б�Ҫ���������������
        /// <summary>
        /// ���ɽ軹������API�Ƿ���Ҫ����item xml���ݣ�
        /// ����Ϊ��PrintHost�и���item xmlʵ�ֶ��ֹ��ܵ���Ҫ�����򿪡��������Ч�ʿ��ǣ��ر�
        /// </summary>
        public bool ChargingNeedReturnItemXml
        {
            get
            {
                // return true;
                return this.AppInfo.GetBoolean("charging",
                    "need_return_item_xml",
                    false);
            }
            set
            {
                this.AppInfo.SetBoolean("charging",
                    "need_return_item_xml",
                    value);
            }
        }

#if NO
        private void timer_operHistory_Tick(object sender, EventArgs e)
        {
            this.OperHistory.OnTimer();
        }
#endif

        // 
        // return:
        //      null    װ��ʧ��
        //      ����    װ��(����)�ɹ�
        /// <summary>
        /// �۲쵱ǰ�Ƿ��з���ָ��·���� EntityForm �Ѿ��򿪣����û�����´�һ��
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <returns>EntityForm �������Ϊ null ���ʾ��ʧ��</returns>
        public EntityForm GetEntityForm(string strBiblioRecPath)
        {
            for (int i = 0; i < this.MdiChildren.Length; i++)
            {
                Form child = this.MdiChildren[i];

                if (child is EntityForm)
                {
                    EntityForm entity_form = (EntityForm)child;
                    if (entity_form.BiblioRecPath == strBiblioRecPath)
                        return entity_form;
                }
            }

            EntityForm form = new EntityForm();
            form.MdiParent = this;
            form.MainForm = this;
            form.Show();

            // return:
            //      -1  �����Ѿ���MessageBox����
            //      0   û��װ��
            //      1   �ɹ�װ��
            //      2   ͨ����ռ��
            int nRet = form.LoadRecordOld(strBiblioRecPath,
                "",
                true);
            if (nRet != 1)
            {
                form.Close();
                return null;
            }

            return form;
        }
#if NO
        // ����ͼ���Ӧ�÷���������Ŀ¼URL
        // ע: ���Ǹ���ǰ�˱����������������
        // ���硰http://test111/dp2libraryws��
        public string LibraryServerDir
        {
            get
            {
                string strLibraryServerUrl = this.AppInfo.GetString(
                    "config",
                    "circulation_server_url",
                    "");
                int pos = strLibraryServerUrl.LastIndexOf("/");
                if (pos != -1)
                    return strLibraryServerUrl.Substring(0, pos);

                return strLibraryServerUrl;
            }
        }
#endif
        // ����ͼ���Ӧ�÷���������Ŀ¼URL
        // ע: ���Ǹ���ǰ�˱����������������
        // ���硰http://test111/dp2library��
        internal string LibraryServerDir1
        {
            get
            {
                string strLibraryServerUrl = this.AppInfo.GetString(
                    "config",
                    "circulation_server_url",
                    "");

                return strLibraryServerUrl;
            }
        }

        // 
        /// <summary>
        /// GCATͨ�ú������ߺ���� WebService URL
        /// ȱʡΪ http://dp2003.com/gcatserver/
        /// </summary>
        public string GcatServerUrl
        {
            get
            {
                return this.AppInfo.GetString("config",
                    "gcat_server_url",
                    "http://dp2003.com/gcatserver/");
            }
        }

        /// <summary>
        /// ƴ�������� URL��
        /// ȱʡΪ http://dp2003.com/gcatserver/
        /// </summary>
        public string PinyinServerUrl
        {
            get
            {
                return this.AppInfo.GetString("config",
                    "pinyin_server_url",
                    "http://dp2003.com/gcatserver/");
            }
        }

        /// <summary>
        /// �Ƿ�Ҫ��ʱ���ñ���ƴ������״̬�˳�ʱ���ᱻ����
        /// </summary>
        public bool ForceUseLocalPinyinFunc = false;    // �Ƿ�Ҫ��ʱ���ñ���ƴ������״̬�˳�ʱ���ᱻ����

        private void MenuItem_clearDatabaseInfoCatch_Click(object sender, EventArgs e)
        {
            bool bEnabled = this.MenuItem_clearDatabaseInfoCatch.Enabled;
            this.MenuItem_clearDatabaseInfoCatch.Enabled = false;
            try
            {
                this.Channel.Close();   // ��ʹͨ��ͨ�����µ�¼

                // ���»�ø��ֿ������б�
                this.StartPrepareNames(false, false);
            }
            finally
            {
                this.MenuItem_clearDatabaseInfoCatch.Enabled = bEnabled;
            }
        }

        // ��ǩ��ӡ��
        private void MenuItem_openLabelPrintForm_Click(object sender, EventArgs e)
        {
#if NO
            LabelPrintForm form = new LabelPrintForm();

            // form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<LabelPrintForm>();

        }

        // ��Ƭ��ӡ��
        private void MenuItem_openCardPrintForm_Click(object sender, EventArgs e)
        {
#if NO
            CardPrintForm form = new CardPrintForm();

            // form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<CardPrintForm>();

        }

        // 
        /// <summary>
        /// ������Ŀ¼�»����ʱ�ļ���
        /// </summary>
        /// <param name="strDirName">��ʱ�ļ�Ŀ¼��</param>
        /// <param name="strFilenamePrefix">��ʱ�ļ���ǰ׺�ַ���</param>
        /// <returns>��ʱ�ļ���</returns>
        public string NewTempFilename(string strDirName,
            string strFilenamePrefix)
        {
            string strFilePath = "";
            int nRedoCount = 0;
            string strDir = PathUtil.MergePath(this.DataDir, strDirName);
            PathUtil.CreateDirIfNeed(strDir);
            for (int i = 0; ; i++)
            {
                strFilePath = PathUtil.MergePath(strDir, strFilenamePrefix + (i + 1).ToString());
                if (File.Exists(strFilePath) == false)
                {
                    // ����һ��0�ֽڵ��ļ�
                    try
                    {
                        File.Create(strFilePath).Close();
                    }
                    catch (Exception/* ex*/)
                    {
                        if (nRedoCount > 10)
                        {
                            string strError = "�����ļ� '" + strFilePath + "' ʧ��...";
                            throw new Exception(strError);
                        }
                        nRedoCount++;
                        continue;
                    }
                    break;
                }
            }

            return strFilePath;
        }

        private void toolStrip_main_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;

        }

        private void toolStrip_main_DragDrop(object sender, DragEventArgs e)
        {
            string strError = "";

            string strWhole = (String)e.Data.GetData("Text");

            string[] lines = strWhole.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 1)
            {
                strError = "��һ��Ҳ������";
                goto ERROR1;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string strFirstLine = lines[i].Trim();

                if (String.IsNullOrEmpty(strFirstLine) == true)
                    continue;

                // ȡ��recpath
                string strRecPath = "";
                int nRet = strFirstLine.IndexOf("\t");
                if (nRet == -1)
                    strRecPath = strFirstLine;
                else
                    strRecPath = strFirstLine.Substring(0, nRet).Trim();

                // �ж�������Ŀ��¼·��������ʵ���¼·����
                string strDbName = Global.GetDbName(strRecPath);

                if (this.IsBiblioDbName(strDbName) == true)
                {
                    EntityForm form = new EntityForm();
                    form.MdiParent = this;
                    form.MainForm = this;
                    form.Show();

                    form.LoadRecordOld(strRecPath,
                        "",
                        true);
                }
                else if (this.IsItemDbName(strDbName) == true)
                {
                    // TODO: ��Ҫ�Ľ�Ϊ������ڵ�ǰ�Ѿ��򿪵�EntityForm���ҵ����¼·�����Ͳ����д򿪴����ˡ�����ѡ����Ӧ��listview�м���

                    EntityForm form = new EntityForm();
                    form.MdiParent = this;
                    form.MainForm = this;
                    form.Show();

                    form.LoadItemByRecPath(strRecPath,
                        false);
                }
                else if (this.IsReaderDbName(strDbName) == true)
                {
                    ReaderInfoForm form = new ReaderInfoForm();
                    form.MdiParent = this;
                    form.MainForm = this;
                    form.Show();

                    form.LoadRecordByRecPath(strRecPath,
                        "");
                }
                else
                {
                    strError = "��¼·�� '" + strRecPath + "' �е����ݿ����Ȳ�����Ŀ������Ҳ����ʵ�����...";
                    goto ERROR1;
                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        // ɾ������Ŀ¼��ȫ����ʱ�ļ�
        // �����������ʱ�����
        void DeleteAllTempFiles(string strDataDir)
        {
            // ���ÿ���Ȩ
            Application.DoEvents();

            DirectoryInfo di = new DirectoryInfo(strDataDir);

            if (string.IsNullOrEmpty(di.Name) == false
                && di.Name[0] == '~')
            {
                try
                {
                    di.Delete(true);
                }
                catch
                {
                    // goto DELETE_FILES;
                }

                return;
            }

        // DELETE_FILES:
            FileInfo[] fis = di.GetFiles();
            for (int i = 0; i < fis.Length; i++)
            {
                string strFileName = fis[i].Name;
                if (strFileName.Length > 0
                    && strFileName[0] == '~')
                {
                    Stop.SetMessage("����ɾ�� " + fis[i].FullName);
                    try
                    {
                        File.Delete(fis[i].FullName);
                    }
                    catch
                    {
                    }
                }
            }

            // �����¼�Ŀ¼���ݹ�
            DirectoryInfo[] dis = di.GetDirectories();
            for (int i = 0; i < dis.Length; i++)
            {
                DeleteAllTempFiles(dis[i].FullName);
            }
        }

        private void MenuItem_openTestSearch_Click(object sender, EventArgs e)
        {
#if NO
            TestSearchForm form = new TestSearchForm();

            // form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<TestSearchForm>();

        }

        /// <summary>
        /// ȱʡ��������
        /// </summary>
        public string DefaultFontString
        {
            get
            {
                return this.AppInfo.GetString(
                    "Global",
                    "default_font",
                    "");
            }
            set
            {
                this.AppInfo.SetString(
                    "Global",
                    "default_font",
                    value);
            }
        }

        /// <summary>
        /// ȱʡ����
        /// </summary>
        new public Font DefaultFont
        {
            get
            {
                string strDefaultFontString = this.DefaultFontString;
                if (String.IsNullOrEmpty(strDefaultFontString) == true)
                {
                    return GuiUtil.GetDefaultFont();    // 2015/5/8
                    // return null;
                }

                // Create the FontConverter.
                System.ComponentModel.TypeConverter converter =
                    System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));

                return (Font)converter.ConvertFromString(strDefaultFontString);
            }
        }

        // parameters:
        //      bForce  �Ƿ�ǿ�����á�ǿ��������ָDefaultFont == null ��ʱ��ҲҪ����Control.DefaultFont������
        /// <summary>
        /// ���ÿؼ�����
        /// </summary>
        /// <param name="control">�ؼ�</param>
        /// <param name="font">����</param>
        /// <param name="bForce">�Ƿ�ǿ�����á�ǿ��������ָDefaultFont == null ��ʱ��ҲҪ����Control.DefaultFont������</param>
        public static void SetControlFont(Control control,
            Font font,
            bool bForce = false)
        {
            if (font == null)
            {
                if (bForce == false)
                    return;
                font = Control.DefaultFont;
            }
            if (font.Name == control.Font.Name
                && font.Style == control.Font.Style
                && font.SizeInPoints == control.Font.SizeInPoints)
            { }
            else
            control.Font = font;

            ChangeDifferentFaceFont(control, font);
        }

        static void ChangeDifferentFaceFont(Control parent,
            Font font)
        {
            // �޸������¼��ؼ������壬�����������һ���Ļ�
            foreach (Control sub in parent.Controls)
            {
                Font subfont = sub.Font;

#if NO
                float ratio = subfont.SizeInPoints / font.SizeInPoints;
                if (subfont.Name != font.Name
                    || subfont.SizeInPoints != font.SizeInPoints)
                {
                    sub.Font = new Font(font.FontFamily, ratio * font.SizeInPoints, subfont.Style, GraphicsUnit.Point);


                    // sub.Font = new Font(font, subfont.Style);
                }
#endif
                ChangeFont(font, sub);

                if (sub is ToolStrip)
                {
                    ChangeDifferentFaceFont((ToolStrip)sub, font);
                }

                // �ݹ�
                ChangeDifferentFaceFont(sub, font);
            }
        }

        static void ChangeDifferentFaceFont(ToolStrip tool,
    Font font)
        {
            // �޸�������������壬�����������һ���Ļ�
            for(int i=0;i<tool.Items.Count;i++)
            {
                ToolStripItem item = tool.Items[i];

                Font subfont = item.Font;
                float ratio = subfont.SizeInPoints / font.SizeInPoints;
                if (subfont.Name != font.Name
                    || subfont.SizeInPoints != font.SizeInPoints)
                {
                    // item.Font = new Font(font, subfont.Style);
                    item.Font = new Font(font.FontFamily, ratio * font.SizeInPoints, subfont.Style, GraphicsUnit.Point);
                }
            }
        }

        // �޸�һ���ؼ�������
        static void ChangeFont(Font font,
            Control item)
        {
            Font subfont = item.Font;
            float ratio = subfont.SizeInPoints / font.SizeInPoints;
            if (subfont.Name != font.Name
                || subfont.SizeInPoints != font.SizeInPoints)
            {
                // item.Font = new Font(font, subfont.Style);
                item.Font = new Font(font.FontFamily, ratio * font.SizeInPoints, subfont.Style, GraphicsUnit.Point);
            }
        }

        static void ChangeDifferentFaceFont(SplitContainer tool,
Font font)
        {
            ChangeFont(font, tool.Panel1);
            // �ݹ�
            ChangeDifferentFaceFont(tool.Panel1, font);

            ChangeFont(font, tool.Panel2);

            // �ݹ�
            ChangeDifferentFaceFont(tool.Panel2, font);
        }

        /// <summary>
        /// ����̶��������ġ����ԡ�����ҳ
        /// </summary>
        public void ActivatePropertyPage()
        {
            this.tabControl_panelFixed.SelectedTab = this.tabPage_property;
        }

        /// <summary>
        /// �̶������������Կؼ�
        /// </summary>
        public Control CurrentPropertyControl
        {
            get
            {
                if (this.tabPage_property.Controls.Count == 0)
                    return null;
                return this.tabPage_property.Controls[0];
            }
            set
            {
                // ���ԭ�пؼ�
                while (this.tabPage_property.Controls.Count > 0)
                    this.tabPage_property.Controls.RemoveAt(0);

                if (value != null)
                {
                    this.tabPage_property.Controls.Add(value);
                    // this.tabControl_panelFixed.SelectedTab = this.tabPage_property;
                }
            }
        }

        /// <summary>
        /// ����̶��������ġ����ա�����ҳ
        /// </summary>
        public void ActivateAcceptPage()
        {
            this.tabControl_panelFixed.SelectedTab = this.tabPage_accept;
        }

        /// <summary>
        /// �̶������������տؼ�
        /// </summary>
        public Control CurrentAcceptControl
        {
            get
            {
                if (this.tabPage_accept.Controls.Count == 0)
                    return null;
                return this.tabPage_accept.Controls[0];
            }
            set
            {
                // ���ԭ�пؼ�
                while (this.tabPage_accept.Controls.Count > 0)
                    this.tabPage_accept.Controls.RemoveAt(0);

                if (value != null)
                {
                    this.tabPage_accept.Controls.Add(value);
                }
            }
        }

        /// <summary>
        /// ����̶��������ġ�У����������ҳ
        /// </summary>
        public void ActivateVerifyResultPage()
        {
            this.tabControl_panelFixed.SelectedTab = this.tabPage_verifyResult;
        }

        /// <summary>
        /// �̶���������У�����ؼ�
        /// </summary>
        public Control CurrentVerifyResultControl
        {
            get
            {
                if (this.tabPage_verifyResult.Controls.Count == 0)
                    return null;
                return this.tabPage_verifyResult.Controls[0];
            }
            set
            {
                // ���ԭ�пؼ�
                while (this.tabPage_verifyResult.Controls.Count > 0)
                    this.tabPage_verifyResult.Controls.RemoveAt(0);

                if (value != null)
                {
                    this.tabPage_verifyResult.Controls.Add(value);
                    // this.tabControl_panelFixed.SelectedTab = this.tabPage_verifyResult;
                }

            }
        }

        /// <summary>
        /// ����̶��������ġ��������ݡ�����ҳ
        /// </summary>
        public void ActivateGenerateDataPage()
        {
            this.tabControl_panelFixed.SelectedTab = this.tabPage_generateData;
        }

        /// <summary>
        /// �̶��������Ĵ������ݿؼ�
        /// </summary>
        public Control CurrentGenerateDataControl
        {
            get
            {
                if (this.tabPage_generateData.Controls.Count == 0)
                    return null;
                return this.tabPage_generateData.Controls[0];
            }
            set
            {
                // ���ԭ�пؼ�
                while (this.tabPage_generateData.Controls.Count > 0)
                    this.tabPage_generateData.Controls.RemoveAt(0);

                if (value != null)
                {
                    this.tabPage_generateData.Controls.Add(value);
                    // this.tabControl_panelFixed.SelectedTab = this.tabPage_generateData;

                    // ������ְ�ش���ͼ�������
                    if (this.tabControl_panelFixed.Visible
                        && this.tabControl_panelFixed.SelectedTab == this.tabPage_generateData)
                        this.tabPage_generateData.Update();
                }
            }
        }

        /// <summary>
        /// �̶���������Ƿ�ɼ�
        /// </summary>
        public bool PanelFixedVisible
        {
            get
            {
                return this.panel_fixed.Visible;
            }
            set
            {
                this.panel_fixed.Visible = value;
                this.splitter_fixed.Visible = value;

                this.MenuItem_displayFixPanel.Checked = value;
            }
        }

        private void toolStripButton_close_Click(object sender, EventArgs e)
        {
            this.PanelFixedVisible = false;
        }

        private void toolButton_refresh_Click(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild is EntityForm)
            {
                ((EntityForm)this.ActiveMdiChild).Reload();
            }
            if (this.ActiveMdiChild is ChargingForm)
            {
                ((ChargingForm)this.ActiveMdiChild).Reload();
            }
            if (this.ActiveMdiChild is ItemInfoForm)
            {
                ((ItemInfoForm)this.ActiveMdiChild).Reload();
            }
        }

        private void MenuItem_utility_Click(object sender, EventArgs e)
        {
#if NO
            UtilityForm form = new UtilityForm();

            // form.MainForm = this;
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<UtilityForm>();

        }

        // 
        /// <summary>
        /// ��ǰ�̶�����Ƿ�߱���ʾ�����ԡ�������
        /// </summary>
        /// <returns>�Ƿ�</returns>
        public bool CanDisplayItemProperty()
        {
            if (this.PanelFixedVisible == false)
                return false;
            if (this.tabControl_panelFixed.SelectedTab != this.tabPage_property)
                return false;

            return true;
        }

        /// <summary>
        /// ��ù̶�����������Եı���
        /// </summary>
        /// <returns>��������</returns>
        public string GetItemPropertyTitle()
        {
            if (this.m_propertyViewer == null)
                return null;

            return this.m_propertyViewer.Text;
        }

        /// <summary>
        /// �ڹ̶��������ġ����ԡ�����ҳ��ʾ��Ϣ
        /// </summary>
        /// <param name="strTitle">��������</param>
        /// <param name="strHtml">HTML �ַ���</param>
        /// <param name="strXml">XML �ַ���</param>
        public void DisplayItemProperty(string strTitle,
    string strHtml,
    string strXml)
        {
            if (this.CanDisplayItemProperty() == false)
                return;

            bool bNew = false;
            if (this.m_propertyViewer == null)
            {
                m_propertyViewer = new CommentViewerForm();
                MainForm.SetControlFont(m_propertyViewer, this.Font, false);
                bNew = true;
            }

            // �Ż�
            if (m_propertyViewer.Text == strTitle)
                return;

            m_propertyViewer.MainForm = this;  // �����ǵ�һ��

            if (string.IsNullOrEmpty(strTitle) == true
                && string.IsNullOrEmpty(strHtml) == true
                && string.IsNullOrEmpty(strXml) == true)
            {
                this.m_propertyViewer.Clear();
                this.m_propertyViewer.Text = "";
            }
            else
            {
                if (bNew == true)
                    m_propertyViewer.InitialWebBrowser();

                m_propertyViewer.SuppressScriptErrors = true;   // ���Ե�ʱ������Ϊ false

                m_propertyViewer.Text = strTitle;
                m_propertyViewer.HtmlString = strHtml;
                m_propertyViewer.XmlString = strXml;
            }


            // 
            if (this.CurrentPropertyControl != m_propertyViewer.MainControl)
                m_propertyViewer.DoDock(false); // �����Զ���ʾFixedPanel
        }

        // ���ͳ�ƴ���Ӣ��������
        static string GetTypeName(object form)
        {
            string strTypeName = form.GetType().ToString();
            int nRet = strTypeName.LastIndexOf(".");
            if (nRet != -1)
                strTypeName = strTypeName.Substring(nRet + 1);

            return strTypeName;
        }

        // ���ͳ�ƴ��ĺ���������
        static string GetWindowName(object form)
        {
            return SelectInstallProjectsDialog.GetHanziHostName(GetTypeName(form));
        }

        // �Ӵ��̸���ȫ������
        private void MenuItem_updateStatisProjectsFromDisk_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nUpdateCount = 0;
            int nRet = 0;

            FolderBrowserDialog dir_dlg = new FolderBrowserDialog();

            dir_dlg.Description = "��ָ����������Ŀ¼:";
            dir_dlg.RootFolder = Environment.SpecialFolder.MyComputer;
            dir_dlg.ShowNewFolderButton = false;
            // dir_dlg.SelectedPath = this.textBox_outputFolder.Text;

            if (dir_dlg.ShowDialog() != DialogResult.OK)
                return;

            bool bHideMessageBox = false;
            bool bDontUpdate = false;

            List<Type> types = new List<Type>();
            types.Add(typeof(Iso2709StatisForm));
            types.Add(typeof(OperLogStatisForm));
            types.Add(typeof(ReaderStatisForm));
            types.Add(typeof(ItemStatisForm));
            types.Add(typeof(OrderStatisForm));
            types.Add(typeof(BiblioStatisForm));
            types.Add(typeof(XmlStatisForm));
            types.Add(typeof(PrintOrderForm));

            foreach (Type type in types)
            {
                bool bNewOpened = false;
                var form = GetTopChildWindow(type);
                if (form == null)
                {
                    form = (Form)Activator.CreateInstance(type);
                    form.MdiParent = this;
                    form.WindowState = FormWindowState.Minimized;
                    form.Show();
                    bNewOpened = true;
                    Application.DoEvents();
                }

                try
                {
                    // return:
                    //      -2  ȫ������
                    //      -1  ����
                    //      >=0 ������
                    nRet = UpdateProjects(form,
                        dir_dlg.SelectedPath,
                        ref bHideMessageBox,
                        ref bDontUpdate,
                        out strError);
                    if (nRet == -1 || nRet == -2)
                        goto ERROR1;
                    nUpdateCount += nRet;
                }
                finally
                {
                    if (bNewOpened == true)
                        form.Close();
                }
            }

            // ƾ����ӡ
            {
                // return:
                //      -2  ȫ������
                //      -1  ����
                //      >=0 ������
                nRet = UpdateProjects(this.OperHistory,
                    dir_dlg.SelectedPath,
                        ref bHideMessageBox,
                        ref bDontUpdate,
                        out strError);
                if (nRet == -1 || nRet == -2)
                    goto ERROR1;
                nUpdateCount += nRet;
            }

            // MainForm
            {
                // return:
                //      -2  ȫ������
                //      -1  ����
                //      >=0 ������
                nRet = UpdateProjects(this,
                    dir_dlg.SelectedPath,
                    ref bHideMessageBox,
                    ref bDontUpdate,
                    out strError);
                if (nRet == -1 || nRet == -2)
                    goto ERROR1;
                nUpdateCount += nRet;
            }

            if (nUpdateCount > 0)
                MessageBox.Show(this, "������ " + nUpdateCount.ToString() + " ������");
            else
                MessageBox.Show(this, "û�з��ָ���");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �Ӵ��̰�װȫ������
        private void MenuItem_installStatisProjectsFromDisk_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = -1;
            int nInstallCount = 0;

            bool bDebugger = false;
            if (Control.ModifierKeys == Keys.Control)
                bDebugger = true;

            FolderBrowserDialog dir_dlg = new FolderBrowserDialog();

            dir_dlg.Description = "��ָ����������Ŀ¼:";
            dir_dlg.RootFolder = Environment.SpecialFolder.MyComputer;
            dir_dlg.ShowNewFolderButton = false;
            // dir_dlg.SelectedPath = this.textBox_outputFolder.Text;

            if (dir_dlg.ShowDialog() != DialogResult.OK)
                return;

            // this.textBox_outputFolder.Text = dir_dlg.SelectedPath;


            // Ѱ�� projects.xml �ļ�
            string strLocalFileName = PathUtil.MergePath(dir_dlg.SelectedPath, "projects.xml");
            if (File.Exists(strLocalFileName) == false)
            {
                // strError = "����ָ����Ŀ¼ '" + dir_dlg.SelectedPath + "' �в�û�а��� projects.xml �ļ����޷����а�װ";
                // goto ERROR1;

                // ���û�� projects.xml �ļ���������ȫ�� *.projpack �ļ�����������һ����ʱ�� ~projects.xml�ļ�
                strLocalFileName = PathUtil.MergePath(this.DataDir, "~projects.xml");
                nRet = ScriptManager.BuildProjectsFile(dir_dlg.SelectedPath,
                    strLocalFileName,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            // �г��Ѿ���װ�ķ�����URL
            List<string> installed_urls = new List<string>();
            List<Form> newly_opened_forms = new List<Form>();
            List<Form> forms = new List<Form>();

            List<Type> types = new List<Type>();
            types.Add(typeof(Iso2709StatisForm));
            types.Add(typeof(OperLogStatisForm));
            types.Add(typeof(ReaderStatisForm));
            types.Add(typeof(ItemStatisForm));
            types.Add(typeof(OrderStatisForm));
            types.Add(typeof(BiblioStatisForm));
            types.Add(typeof(XmlStatisForm));
            types.Add(typeof(PrintOrderForm));

            foreach (Type type in types)
            {
                // bool bNewOpened = false;
                var form = GetTopChildWindow(type);
                if (form == null)
                {
                    form = (Form)Activator.CreateInstance(type);
                    form.MdiParent = this;
                    form.WindowState = FormWindowState.Minimized;
                    form.Show();
                    newly_opened_forms.Add(form);
                    Application.DoEvents();
                }

                forms.Add(form);

                dynamic o = form;
                List<string> urls = new List<string>();
                nRet = o.ScriptManager.GetInstalledUrls(out urls,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                installed_urls.AddRange(urls);
            }

            // ƾ����ӡ
            {
                List<string> urls = new List<string>();
                nRet = this.OperHistory.ScriptManager.GetInstalledUrls(out urls,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                installed_urls.AddRange(urls);
            }

            // ��ܴ���
            {
                List<string> urls = new List<string>();
                nRet = this.ScriptManager.GetInstalledUrls(out urls,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                installed_urls.AddRange(urls);
            }

            try
            {
                SelectInstallProjectsDialog dlg = new SelectInstallProjectsDialog();
                MainForm.SetControlFont(dlg, this.DefaultFont);
                dlg.XmlFilename = strLocalFileName;
                dlg.InstalledUrls = installed_urls;
                if (bDebugger == true)
                    dlg.Category = "debugger";
                dlg.StartPosition = FormStartPosition.CenterScreen;

                this.AppInfo.LinkFormState(dlg,
                    "SelectInstallProjectsDialog_state");
                dlg.ShowDialog(this);
                this.AppInfo.UnlinkFormState(dlg);
                if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return;

                // ���������а�װ
                foreach (Form form in forms)
                {
                    // Ϊһ��ͳ�ƴ���װ���ɷ���
                    // parameters:
                    //      projects    ����װ�ķ�����ע���п��ܰ������ʺϰ�װ�������ڵķ���
                    // return:
                    //      -1  ����
                    //      >=0 ��װ�ķ�����
                    nRet = InstallProjects(
                        form,
                        GetWindowName(form),
                        dlg.SelectedProjects,
                        dir_dlg.SelectedPath,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    nInstallCount += nRet;
                }

                // ƾ����ӡ
                {
                    nRet = InstallProjects(
    this.OperHistory,
    "ƾ����ӡ",
    dlg.SelectedProjects,
    dir_dlg.SelectedPath,
    out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    nInstallCount += nRet;
                }

                // MainForm
                {
                    nRet = InstallProjects(
    this,
    "��ܴ���",
    dlg.SelectedProjects,
    dir_dlg.SelectedPath,
    out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    nInstallCount += nRet;
                }
            }
            finally
            {
                // �رձ����´򿪵Ĵ���
                foreach (Form form in newly_opened_forms)
                {
                    form.Close();
                }
            }

            MessageBox.Show(this, "����װ���� " + nInstallCount.ToString() + " ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �� dp2003.com ��װȫ������
        private void MenuItem_installStatisProjects_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = -1;
            int nInstallCount = 0;

            bool bDebugger = false;
            if (Control.ModifierKeys == Keys.Control)
                bDebugger = true;

            // ����projects.xml�ļ�
            string strLocalFileName = PathUtil.MergePath(this.DataDir, "~temp_projects.xml");
            string strTempFileName = PathUtil.MergePath(this.DataDir, "~temp_download_projects.xml");

            try
            {
                File.Delete(strLocalFileName);
            }
            catch
            {
            }
            try
            {
                File.Delete(strTempFileName);
            }
            catch
            {
            }

            nRet = WebFileDownloadDialog.DownloadWebFile(
                this,
                "http://dp2003.com/dp2circulation/projects/projects.xml",
                strLocalFileName,
                strTempFileName,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // �г��Ѿ���װ�ķ�����URL
            List<string> installed_urls = new List<string>();
            List<Form> newly_opened_forms = new List<Form>();
            List<Form> forms = new List<Form>();

            List<Type> types = new List<Type>();
            types.Add(typeof(Iso2709StatisForm));
            types.Add(typeof(OperLogStatisForm));
            types.Add(typeof(ReaderStatisForm));
            types.Add(typeof(ItemStatisForm));
            types.Add(typeof(OrderStatisForm));
            types.Add(typeof(BiblioStatisForm));
            types.Add(typeof(XmlStatisForm));
            types.Add(typeof(PrintOrderForm));

            foreach (Type type in types)
            {
                // bool bNewOpened = false;
                var form = GetTopChildWindow(type);
                if (form == null)
                {
                    form = (Form)Activator.CreateInstance(type);
                    form.MdiParent = this;
                    form.WindowState = FormWindowState.Minimized;
                    form.Show();
                    newly_opened_forms.Add(form);
                    Application.DoEvents();
                }

                forms.Add(form);

                dynamic o = form;
                List<string> urls = new List<string>();
                nRet = o.ScriptManager.GetInstalledUrls(out urls,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                installed_urls.AddRange(urls);
            }

            // ƾ����ӡ
            {
                List<string> urls = new List<string>();
                nRet = this.OperHistory.ScriptManager.GetInstalledUrls(out urls,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                installed_urls.AddRange(urls);
            }

            // ��ܴ���
            {
                List<string> urls = new List<string>();
                nRet = this.ScriptManager.GetInstalledUrls(out urls,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                installed_urls.AddRange(urls);
            }

            try
            {
                SelectInstallProjectsDialog dlg = new SelectInstallProjectsDialog();
                MainForm.SetControlFont(dlg, this.DefaultFont);
                dlg.XmlFilename = strLocalFileName;
                dlg.InstalledUrls = installed_urls;
                if (bDebugger == true)
                    dlg.Category = "debugger";
                dlg.StartPosition = FormStartPosition.CenterScreen;

                this.AppInfo.LinkFormState(dlg,
                    "SelectInstallProjectsDialog_state");
                dlg.ShowDialog(this);
                this.AppInfo.UnlinkFormState(dlg);
                if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return;

                // ���������а�װ
                foreach (Form form in forms)
                {
                    // Ϊһ��ͳ�ƴ���װ���ɷ���
                    // parameters:
                    //      projects    ����װ�ķ�����ע���п��ܰ������ʺϰ�װ�������ڵķ���
                    // return:
                    //      -1  ����
                    //      >=0 ��װ�ķ�����
                    nRet = InstallProjects(
                        form,
                        GetWindowName(form),
                        dlg.SelectedProjects,
                        "!url",
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    nInstallCount += nRet;
                }

                // ƾ����ӡ
                {
                    nRet = InstallProjects(
    this.OperHistory,
    "ƾ����ӡ",
    dlg.SelectedProjects,
                        "!url",
    out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    nInstallCount += nRet;
                }

                // MainForm
                {
                    nRet = InstallProjects(
    this,
    "��ܴ���",
    dlg.SelectedProjects,
                        "!url",
    out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    nInstallCount += nRet;
                }
            }
            finally
            {
                // �رձ����´򿪵Ĵ���
                foreach (Form form in newly_opened_forms)
                {
                    form.Close();
                }
            }

            MessageBox.Show(this, "����װ���� " + nInstallCount.ToString() + " ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // Ϊһ��ͳ�ƴ���װ���ɷ���
        // parameters:
        //      projects    ����װ�ķ�����ע���п��ܰ������ʺϰ�װ�������ڵķ���
        // return:
        //      -1  ����
        //      >=0 ��װ�ķ�����
        int InstallProjects(
            object form,
            string strWindowName,
            List<ProjectItem> projects,
            string strSource,
            out string strError)
        {
            strError = "";
            int nInstallCount = 0;
            int nRet = 0;

            dynamic o = form;

            o.EnableControls(false);
            try
            {
                /*
                    string strTypeName = form.GetType().ToString();
                    nRet = strTypeName.LastIndexOf(".");
                    if (nRet != -1)
                        strTypeName = strTypeName.Substring(nRet + 1);
                */
                string strTypeName = GetTypeName(form);

                foreach (ProjectItem item in projects)
                {
                    if (strTypeName != item.Host)
                        continue;

                    string strLocalFileName = "";
                    string strLastModified = "";

                    if (strSource == "!url")
                    {
                        strLocalFileName = this.DataDir + "\\~install_project.projpack";
                        string strTempFileName = this.DataDir + "\\~temp_download_webfile";

                        nRet = WebFileDownloadDialog.DownloadWebFile(
                            this,
                            item.Url,
                            strLocalFileName,
                            strTempFileName,
                            "",
                            out strLastModified,
                            out strError);
                        if (nRet == -1)
                            return -1;
                    }
                    else
                    {
                        string strLocalDir = strSource;

                        // Uri uri = new Uri(item.Url);
                        /*
                        string strPath = item.Url;  // uri.LocalPath;
                        nRet = strPath.LastIndexOf("/");
                        if (nRet != -1)
                            strPath = strPath.Substring(nRet);
                         * */
                        string strPureFileName = ScriptManager.GetFileNameFromUrl(item.Url);

                        strLocalFileName = PathUtil.MergePath(strLocalDir, strPureFileName);

                        FileInfo fi = new FileInfo(strLocalFileName);
                        if (fi.Exists == false)
                        {
                            strError = "Ŀ¼ '" + strLocalDir + "' ��û���ҵ��ļ� '" + strPureFileName + "'";
                            return -1;
                        }
                        strLastModified = DateTimeUtil.Rfc1123DateTimeString(fi.LastWriteTimeUtc);
                    }

                    // ��װProject
                    // return:
                    //      -1  ����
                    //      0   û�а�װ����
                    //      >0  ��װ�ķ�����
                    nRet = o.ScriptManager.InstallProject(
                        o is Form ? o : this,
                        strWindowName,
                        strLocalFileName,
                        strLastModified,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    nInstallCount += nRet;
                }
            }
            finally
            {
                o.EnableControls(true);
            }

            return nInstallCount;
        }

        // ����һ������ӵ�е�ȫ������
        // parameters:
        //      strSource   "!url"���ߴ���Ŀ¼���ֱ��ʾ����������£����ߴӴ��̼�����
        // return:
        //      -2  ȫ������
        //      -1  ����
        //      >=0 ������
        int UpdateProjects(
            object form,
            string strSource,
            ref bool bHideMessageBox,
            ref bool bDontUpdate,
            out string strError)
        {
            strError = "";
            string strWarning = "";
            string strUpdateInfo = "";
            int nUpdateCount = 0;

            dynamic o = form;

            o.EnableControls(false);
            try
            {
                // ������һ�������ڵ��µ�ȫ������
                // parameters:
                //      dir_node    �����ڵ㡣��� == null ������ȫ������
                //      strSource   "!url"���ߴ���Ŀ¼���ֱ��ʾ����������£����ߴӴ��̼�����
                // return:
                //      -2  ȫ������
                //      -1  ����
                //      0   �ɹ�
                int nRet = o.ScriptManager.CheckUpdate(
                    o is Form ? o : this,
                    null,
                    strSource,
                    ref bHideMessageBox,
                    ref bDontUpdate,
                    ref nUpdateCount,
                    ref strUpdateInfo,
                    ref strWarning,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == -2)
                    return -2;
            }
            finally
            {
                o.EnableControls(true);
            }
            return nUpdateCount;
        }

        // �� dp2003.com ������ȫ������
        private void MenuItem_updateStatisProjects_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nUpdateCount = 0;
            int nRet = 0;

            bool bHideMessageBox = false;
            bool bDontUpdate = false;

            List<Type> types = new List<Type>();
            types.Add(typeof(Iso2709StatisForm));
            types.Add(typeof(OperLogStatisForm));
            types.Add(typeof(ReaderStatisForm));
            types.Add(typeof(ItemStatisForm));
            types.Add(typeof(OrderStatisForm));
            types.Add(typeof(BiblioStatisForm));
            types.Add(typeof(XmlStatisForm));
            types.Add(typeof(PrintOrderForm));

            foreach (Type type in types)
            {
                bool bNewOpened = false;
                var form = GetTopChildWindow(type);
                if (form == null)
                {
                    form = (Form)Activator.CreateInstance(type);
                    form.MdiParent = this;
                    form.WindowState = FormWindowState.Minimized;
                    form.Show();
                    bNewOpened = true;
                    Application.DoEvents();
                }

                try
                {
                    // return:
                    //      -2  ȫ������
                    //      -1  ����
                    //      >=0 ������
                    nRet = UpdateProjects(form,
                        "!url",
                        ref bHideMessageBox,
                        ref bDontUpdate,
                        out strError);
                    if (nRet == -1 || nRet == -2)
                        goto ERROR1;
                    nUpdateCount += nRet;
                }
                finally
                {
                    if (bNewOpened == true)
                        form.Close();
                }
            }

            // ƾ����ӡ
            {
                // return:
                //      -2  ȫ������
                //      -1  ����
                //      >=0 ������
                nRet = UpdateProjects(this.OperHistory,
                        "!url",
                        ref bHideMessageBox,
                        ref bDontUpdate,
                        out strError);
                if (nRet == -1 || nRet == -2)
                    goto ERROR1;
                nUpdateCount += nRet;
            }

            // MainForm
            {
                // return:
                //      -2  ȫ������
                //      -1  ����
                //      >=0 ������
                nRet = UpdateProjects(this,
                        "!url",
                        ref bHideMessageBox,
                        ref bDontUpdate,
                        out strError);
                if (nRet == -1 || nRet == -2)
                    goto ERROR1;
                nUpdateCount += nRet;
            }

            if (nUpdateCount > 0)
                MessageBox.Show(this, "������ " + nUpdateCount.ToString() + " ������");
            else
                MessageBox.Show(this, "û�з��ָ���");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// �Ƿ�����������ؼ��Ľű�������ʾ
        /// </summary>
        public bool SuppressScriptErrors
        {
            get
            {
                return !DisplayScriptErrorDialog;
            }
        }

        // ������ؼ�����ű�����Ի���(&S)
        /// <summary>
        /// ������ؼ��Ƿ�����ű�����Ի���
        /// </summary>
        public bool DisplayScriptErrorDialog
        {
            get
            {

                return this.AppInfo.GetBoolean(
                    "global",
                    "display_webbrowsecontrol_scripterror_dialog",
                    false);
            }
            set
            {
                this.AppInfo.SetBoolean(
                    "global",
                    "display_webbrowsecontrol_scripterror_dialog",
                    value);
            }
        }
#if NO
        private void button_test_Click(object sender, EventArgs e)
        {
            ScrollToEnd(this.OperHistory.WebBrowser);
        }

        public static void ScrollToEnd(WebBrowser webBrowser)
        {
            /*
            HtmlDocument doc = webBrowser.Document;
            doc.Body.ScrollIntoView(false);
             * */
            /*
            webBrowser.Focus();
            System.Windows.Forms.SendKeys.Send("{PGDN}");
             * */
            webBrowser.Document.Window.ScrollTo(0,
                webBrowser.Document.Body.ScrollRectangle.Height);
        }
#endif

        #region Client.cs �ű�֧��

        ClientHost _clientHost = null;
        public ClientHost ClientHost
        {
            get
            {
                return _clientHost;
            }
        }

        int InitialClientScript(out string strError)
        {
            strError = "";

            Assembly assembly = null;

            string strServerMappedPath = Path.Combine(this.DataDir, "servermapped");
            string strFileName = Path.Combine(strServerMappedPath, "client/client.cs");

            if (File.Exists(strFileName) == false)
                return 0;   // �ű��ļ�û���ҵ�

            int nRet = PrepareClientScript(strFileName,
                out assembly,
                out _clientHost,
                out strError);
            if (nRet == -1)
            {
                strError = "��ʼ��ǰ�˽ű� '" + Path.GetFileName(strFileName) + "' ʱ����: " + strError;
                return -1;
            }

            _clientHost.MainForm = this;

            return 0;
        }

        // ׼���ű�����
        int PrepareClientScript(string strCsFileName,
            out Assembly assembly,
            out ClientHost host,
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
                "dp2Circulation.ClientHost");
            if (entryClassType == null)
            {
                strError = strCsFileName + " ��û���ҵ� dp2Circulation.ClientHost ������";
                goto ERROR1;
            }

            // newһ��Host��������
            host = (ClientHost)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            return 0;
        ERROR1:
            return -1;
        }

        #endregion

        #region MainForm ͳ�Ʒ���

        private void scriptManager_CreateDefaultContent(object sender, CreateDefaultContentEventArgs e)
        {
            string strPureFileName = Path.GetFileName(e.FileName);

            if (String.Compare(strPureFileName, "main.cs", true) == 0)
            {
                CreateDefaultMainCsFile(e.FileName);
                e.Created = true;
            }
            else
            {
                e.Created = false;
            }

        }

        // 
        /// <summary>
        /// ����ȱʡ�ġ�����Ϊ MainFormHost �� main.cs �ļ�
        /// </summary>
        /// <param name="strFileName">�ļ�ȫ·��</param>
        /// <returns>0: �ɹ�</returns>
        public static int CreateDefaultMainCsFile(string strFileName)
        {

            StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8);
            sw.WriteLine("using System;");
            sw.WriteLine("using System.Collections.Generic;");
            sw.WriteLine("using System.Windows.Forms;");
            sw.WriteLine("using System.IO;");
            sw.WriteLine("using System.Text;");
            sw.WriteLine("using System.Xml;");
            sw.WriteLine("");

            //sw.WriteLine("using DigitalPlatform.MarcDom;");
            //sw.WriteLine("using DigitalPlatform.Statis;");
            sw.WriteLine("using dp2Circulation;");
            sw.WriteLine("");
            sw.WriteLine("using DigitalPlatform.Xml;");
            sw.WriteLine("");

            sw.WriteLine("public class MyStatis : MainFormHost");

            sw.WriteLine("{");

            sw.WriteLine("	public override void Main(object sender, EventArgs e)");
            sw.WriteLine("	{");
            sw.WriteLine("	}");


            sw.WriteLine("}");
            sw.Close();

            return 0;
        }

        private void ToolStripMenuItem_projectManage_Click(object sender, EventArgs e)
        {
            ProjectManageDlg dlg = new ProjectManageDlg();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.ProjectsUrl = "http://dp2003.com/dp2circulation/projects/projects.xml";
            dlg.HostName = "MainForm";
            dlg.scriptManager = this.ScriptManager;
            dlg.AppInfo = this.AppInfo;
            dlg.DataDir = this.DataDir;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);
        }

        string m_strProjectName = "";

        // ִ��ͳ�Ʒ���
        private void toolStripMenuItem_runProject_Click(object sender, EventArgs e)
        {
            string strError = "";

            // ���ֶԻ���ѯ��Project����
            GetProjectNameDlg dlg = new GetProjectNameDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.scriptManager = this.ScriptManager;
            dlg.ProjectName = this.m_strProjectName;
            dlg.NoneProject = false;

            this.AppInfo.LinkFormState(dlg, "GetProjectNameDlg_state");
            dlg.ShowDialog(this);
            this.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.m_strProjectName = dlg.ProjectName;

            //
            string strProjectLocate = "";
            // ��÷�������
            // strProjectNamePath	������������·��
            // return:
            //		-1	error
            //		0	not found project
            //		1	found
            int nRet = this.ScriptManager.GetProjectData(
                dlg.ProjectName,
                out strProjectLocate);
            if (nRet == 0)
            {
                strError = "���� " + dlg.ProjectName + " û���ҵ�...";
                goto ERROR1;
            }
            if (nRet == -1)
            {
                strError = "scriptManager.GetProjectData() error ...";
                goto ERROR1;
            }

            // 
            nRet = RunScript(dlg.ProjectName,
                strProjectLocate,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        int RunScript(string strProjectName,
            string strProjectLocate,
            out string strError)
        {
            EnableControls(false);

            Stop = new DigitalPlatform.Stop();
            Stop.Register(stopManager, true);	// ����������

            this.Stop.OnStop += new StopEventHandler(this.DoStop);
            this.Stop.Initial("����ִ�нű� ...");
            this.Stop.BeginLoop();

            try
            {

                int nRet = 0;
                strError = "";

                this.objStatis = null;
                this.AssemblyMain = null;

                // 2009/11/5 new add
                // ��ֹ��ǰ�����Ĵ򿪵��ļ���Ȼû�йر�
                Global.ForceGarbageCollection();

                nRet = PrepareScript(strProjectName,
                    strProjectLocate,
                    out objStatis,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                objStatis.ProjectDir = strProjectLocate;
                // objStatis.Console = this.Console;

                // ִ�нű���Main()

                if (objStatis != null)
                {
                    EventArgs args = new EventArgs();
                    objStatis.Main(this, args);
                }

                return 0;
            ERROR1:
                return -1;

            }
            catch (Exception ex)
            {
                strError = "�ű� '" + strProjectName + "' ִ�й����׳��쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                return -1;
            }
            finally
            {
                this.Stop.EndLoop();
                this.Stop.OnStop -= new StopEventHandler(this.DoStop);
                this.Stop.Initial("");

                this.AssemblyMain = null;

                if (Stop != null) // �������
                {
                    Stop.Unregister();	// ����������
                    Stop = null;
                }
                EnableControls(true);
            }
        }

        // ׼���ű�����
        int PrepareScript(string strProjectName,
            string strProjectLocate,
            out MainFormHost objStatis,
            out string strError)
        {
            this.AssemblyMain = null;

            objStatis = null;

            string strWarning = "";
            string strMainCsDllName = PathUtil.MergePath(this.InstanceDir, "\\~mainform_statis_main_" + Convert.ToString(this.StatisAssemblyVersion++) + ".dll");    // ++

            string strLibPaths = "\"" + this.DataDir + "\""
                + ","
                + "\"" + strProjectLocate + "\"";

            string[] saAddRef = {
                                    // 2011/4/20 ����
                                    "system.dll",
                                    "system.drawing.dll",
                                    "system.windows.forms.dll",
                                    "system.xml.dll",
                                    "System.Runtime.Serialization.dll",

									Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.Client.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.library.dll",
									// Environment.CurrentDirectory + "\\digitalplatform.statis.dll",
									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
   									Environment.CurrentDirectory + "\\digitalplatform.Script.dll",
									Environment.CurrentDirectory + "\\digitalplatform.dp2.statis.dll",
									// Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
									Environment.CurrentDirectory + "\\dp2circulation.exe",
            };


            // ����Project��Script main.cs��Assembly
            // return:
            //		-2	���������Ѿ���ʾ��������Ϣ�ˡ�
            //		-1	����
            int nRet = ScriptManager.BuildAssembly(
                "MainForm",
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


            this.AssemblyMain = Assembly.LoadFrom(strMainCsDllName);
            if (this.AssemblyMain == null)
            {
                strError = "LoadFrom " + strMainCsDllName + " fail";
                goto ERROR1;
            }

            // �õ�Assembly��MainFormHost������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                this.AssemblyMain,
                "dp2Circulation.MainFormHost");
            if (entryClassType == null)
            {
                strError = strMainCsDllName + "��û���ҵ� dp2Circulation.MainFormHost �����ࡣ";
                goto ERROR1;
            }
            // newһ��Statis��������
            objStatis = (MainFormHost)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            // ΪStatis���������ò���
            objStatis.MainForm = this;
            objStatis.ProjectDir = strProjectLocate;
            objStatis.InstanceDir = this.InstanceDir;

            return 0;
        ERROR1:
            return -1;
        }

        #endregion

        // 2012/3/25
        private void ToolStripMenuItem_stopAll_Click(object sender, EventArgs e)
        {
            stopManager.DoStopAll(null);
        }

        // 
        /// <summary>
        /// ׼����ӡ�����ö��������ϵͳ������û���ҵ���Ϣ�����´���һ��PrinterInfo����
        /// </summary>
        /// <param name="strType">����</param>
        /// <returns>PrinterInfo���󣬴�ӡ��������Ϣ</returns>
        public PrinterInfo PreparePrinterInfo(string strType)
        {
            PrinterInfo info = this.GetPrinterInfo(strType);
            if (info != null)
                return info;
            info = new PrinterInfo();
            info.Type = strType;
            return info;
        }

        // 
        /// <summary>
        /// ���һ���ض����͵Ĵ�ӡ������
        /// </summary>
        /// <param name="strType">����</param>
        /// <returns>PrinterInfo����</returns>
        internal PrinterInfo GetPrinterInfo(string strType)
        {
            string strText = this.AppInfo.GetString("printerInfo",
                strType,
                "");
            if (string.IsNullOrEmpty(strText) == true)
                return null;    // not found
            return new PrinterInfo(strType, strText);
        }

        // 
        /// <summary>
        /// ����һ���ض����͵Ĵ�ӡ������
        /// </summary>
        /// <param name="strType">����</param>
        /// <param name="info">��ӡ��������Ϣ</param>
        public void SavePrinterInfo(string strType,
            PrinterInfo info)
        {
            if (info == null)
            {
                this.AppInfo.SetString("printerInfo",
                    strType,
                    null);
                return;
            }

            this.AppInfo.SetString("printerInfo",
                strType,
                info.GetText());
        }

        private void MenuItem_displayFixPanel_Click(object sender, EventArgs e)
        {
            this.PanelFixedVisible = !this.PanelFixedVisible;
        }

        // ��һ������ͳ�ƴ�
        private void MenuItem_openOrderStatisForm_Click(object sender, EventArgs e)
        {
#if NO
            OrderStatisForm form = new OrderStatisForm();

            // form.MainForm = this;
            // form.DbType = "order";
            form.MdiParent = this;
            form.Show();
#endif
            OpenWindow<OrderStatisForm>();

        }

        /// <summary>
        /// ָ�Ʊ��ػ���Ŀ¼
        /// </summary>
        public string FingerPrintCacheDir
        {
            get
            {
                // string strDir = PathUtil.MergePath(this.MainForm.DataDir, "fingerprintcache");
                return PathUtil.MergePath(this.UserDir, "fingerprintcache");   // 2013/6/16
            }
        }

        /// <summary>
        /// ������־���ػ���Ŀ¼
        /// </summary>
        public string OperLogCacheDir
        {
            get
            {
                // return PathUtil.MergePath(this.DataDir, "operlogcache");
                return PathUtil.MergePath(this.UserDir, "operlogcache");    // 2013/6/16
            }
        }

        /// <summary>
        /// �Ƿ��Զ����������־
        /// </summary>
        public bool AutoCacheOperlogFile
        {
            get
            {
                // �Զ�������־�ļ�
                return
                    this.AppInfo.GetBoolean(
                    "global",
                    "auto_cache_operlogfile",
                    true);
            }
        }

        /// <summary>
        /// ��ƴ��ʱ�Զ�ѡ�������
        /// </summary>
        public bool AutoSelPinyin
        {
            get
            {
                return this.AppInfo.GetBoolean(
                    "global",
                    "auto_select_pinyin",
                    false);
            }
        }

        // ��ʼ��ָ�ƻ���
        private void MenuItem_initFingerprintCache_Click(object sender, EventArgs e)
        {
            ReaderSearchForm form = new ReaderSearchForm();
            form.FingerPrintMode = true;
            form.MdiParent = this;
            form.Show();

            string strError = "";
            // return:
            //      -2  remoting����������ʧ�ܡ�����������δ����
            //      -1  ����
            //      0   �ɹ�
            int nRet = form.InitFingerprintCache(false, out strError);
            if (nRet == -1 || nRet == -2)
                goto ERROR1;
            form.Close();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            form.Close();
        }

        // �״γ�ʼ��ָ�ƻ��档����ӿڳ�����δ�������򲻱���������Ϊȱʡ����remoting server��URL���ã��ܿ����û���������Ҫ���ýӿڳ������˼
        void FirstInitialFingerprintCache()
        {
            string strError = "";

            // û������ ָ���Ķ����ӿ�URL ��������û�б�Ҫ���г�ʼ��
            if (string.IsNullOrEmpty(this.FingerprintReaderUrl) == true)
                return;

            ReaderSearchForm form = new ReaderSearchForm();
            form.FingerPrintMode = true;
            form.MdiParent = this;
            form.Opacity = 0;
            form.Show();
            form.Update();

            // TODO: ��ʾ���ڳ�ʼ������Ҫ�رմ���
            // return:
            //      -2  remoting����������ʧ�ܡ�����������δ����
            //      -1  ����
            //      0   �ɹ�
            int nRet = form.InitFingerprintCache(true, out strError);
            if (nRet == -1 || nRet == -2)
            {
                strError = "��ʼ��ָ�ƻ���ʧ��: " + strError;
                goto ERROR1;
            }
            form.Close();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            form.Close();
        }

        // 
        /// <summary>
        /// ���֤������ URL
        /// </summary>
        public string IdcardReaderUrl
        {
            get
            {
                return this.AppInfo.GetString("cardreader",
                    "idcardReaderUrl",
                    "");  // ����ֵ "ipc://IdcardChannel/IdcardServer"
            }
        }

        // 
        /// <summary>
        /// ָ���Ķ��� URL
        /// </summary>
        public string FingerprintReaderUrl
        {
            get
            {
                return this.AppInfo.GetString("fingerprint",
                    "fingerPrintReaderUrl",
                    "");  // ����ֵ "ipc://FingerprintChannel/FingerprintServer"
            }
        }

        // 
        /// <summary>
        /// ָ�ƴ����ʻ� �û���
        /// </summary>
        public string FingerprintUserName
        {
            get
            {
                return this.AppInfo.GetString("fingerprint",
                    "userName",
                    "");
            }
            set
            {
                this.AppInfo.SetString("fingerprint",
                    "userName",
                    value);
            }
        }

        // 
        /// <summary>
        /// ָ�ƴ����ʻ� ����
        /// </summary>
        internal string FingerprintPassword
        {
            get
            {
                string strPassword = this.AppInfo.GetString("fingerprint",
                    "password",
                    "");
                return this.DecryptPasssword(strPassword);
            }
            set
            {
                string strPassword = this.EncryptPassword(value);
                this.AppInfo.SetString(
                    "fingerprint",
                    "password",
                    strPassword);
            }
        }

        // �̶������ Page ѡ��ҳ
        private void tabControl_panelFixed_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.FixedSelectedPageChanged != null)
                this.FixedSelectedPageChanged(this, e);
        }

        #region Ϊ���ּ�ƴ����ع���

        // ���ַ����еĺ���ת��Ϊ�ĽǺ���
        // parameters:
        //      bLocal  �Ƿ�ӱ��ػ�ȡ�ĽǺ���
        // return:
        //      -1  ����
        //      0   �û�ϣ���ж�
        //      1   ����
        /// <summary>
        /// ���ַ����еĺ���ת��Ϊ�ĽǺ���
        /// </summary>
        /// <param name="bLocal">�Ƿ�ӱ��ػ�ȡ�ĽǺ���</param>
        /// <param name="strText">�����ַ���</param>
        /// <param name="sjhms">�����ĽǺ����ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �û�ϣ���ж�; 1: ����</returns>
        public int HanziTextToSjhm(
            bool bLocal,
            string strText,
            out List<string> sjhms,
            out string strError)
        {
            strError = "";
            sjhms = new List<string>();

            // string strSpecialChars = "���������������������������������ۣݡ����������������ܣ�������������";

            for (int i = 0; i < strText.Length; i++)
            {
                char ch = strText[i];

                if (StringUtil.IsHanzi(ch) == false)
                    continue;

                // �����Ƿ��������
                if (StringUtil.SpecialChars.IndexOf(ch) != -1)
                    continue;

                // ����
                string strHanzi = "";
                strHanzi += ch;


                string strResultSjhm = "";

                int nRet = 0;

                if (bLocal == true)
                {
                    nRet = this.LoadQuickSjhm(true, out strError);
                    if (nRet == -1)
                        return -1;
                    nRet = this.QuickSjhm.GetSjhm(
                        strHanzi,
                        out strResultSjhm,
                        out strError);
                }
                else
                {
                    throw new Exception("�ݲ�֧�ִ�ƴ�����л�ȡ�ĽǺ���");
                    /*
                    nRet = GetOnePinyin(strHanzi,
                         out strResultPinyin,
                         out strError);
                     * */
                }
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {	// canceled
                    return 0;
                }

                Debug.Assert(strResultSjhm != "", "");

                strResultSjhm = strResultSjhm.Trim();
                sjhms.Add(strResultSjhm);
            }

            return 1;   // ��������
        }

        GcatServiceClient m_gcatClient = null;
        string m_strPinyinGcatID = "";
        bool m_bSavePinyinGcatID = false;

        // �����ַ���ת��Ϊƴ����������ǰ�汾
        // ����������Ѿ�MessageBox������strError��һ�ַ���Ϊ�ո�
        /// <summary>
        /// �����ַ���ת��Ϊƴ�������ܷ�ʽ
        /// </summary>
        /// <param name="owner">���ں����� MessageBox �ͶԻ��� ����������</param>
        /// <param name="strText">�����ַ���</param>
        /// <param name="style">ת��Ϊƴ���ķ��</param>
        /// <param name="strPinyin">����ƴ���ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �û�ϣ���ж�; 1: ����; 2: ����ַ�������û���ҵ�ƴ���ĺ���</returns>
        public int SmartHanziTextToPinyin(
            IWin32Window owner,
            string strText,
            PinyinStyle style,
            out string strPinyin,
            out string strError)
        {
            return SmartHanziTextToPinyin(
                owner,
                strText,
                style,
                false,
                out strPinyin,
                out strError);
        }

        // �����ַ���ת��Ϊƴ�����°汾
        // ����������Ѿ�MessageBox������strError��һ�ַ���Ϊ�ո�
        /// <summary>
        /// �����ַ���ת��Ϊƴ�������ܷ�ʽ
        /// </summary>
        /// <param name="owner">���ں����� MessageBox �ͶԻ��� ����������</param>
        /// <param name="strText">�����ַ���</param>
        /// <param name="style">ת��Ϊƴ���ķ��</param>
        /// <param name="bAutoSel">�Ƿ��Զ�ѡ�������</param>
        /// <param name="strPinyin">����ƴ���ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �û�ϣ���ж�; 1: ����; 2: ����ַ�������û���ҵ�ƴ���ĺ���</returns>
        public int SmartHanziTextToPinyin(
            IWin32Window owner,
            string strText,
            PinyinStyle style,
            bool bAutoSel,
            out string strPinyin,
            out string strError)
        {
            strPinyin = "";
            strError = "";

            bool bNotFoundPinyin = false;   // �Ƿ���ֹ�û���ҵ�ƴ����ֻ�ܰѺ��ַ������ַ��������

            Stop new_stop = new DigitalPlatform.Stop();
            new_stop.Register(this.stopManager, true);	// ����������
            new_stop.OnStop += new StopEventHandler(new_stop_OnStop);
            new_stop.Initial("���ڻ�� '" + strText + "' ��ƴ����Ϣ (�ӷ����� " + this.PinyinServerUrl + ")...");
            new_stop.BeginLoop();

            m_gcatClient = null;
            try
            {

                m_gcatClient = GcatNew.CreateChannel(this.PinyinServerUrl);

            REDO_GETPINYIN:
                int nStatus = -1;	// ǰ��һ���ַ������� -1:ǰ��û���ַ� 0:��ͨӢ����ĸ 1:�ո� 2:����
                string strPinyinXml = "";
                // return:
                //      -2  strID��֤ʧ��
                //      -1  ����
                //      0   �ɹ�
                int nRet = GcatNew.GetPinyin(
                    new_stop,
                    m_gcatClient,
                    m_strPinyinGcatID,
                    strText,
                    out strPinyinXml,
                    out strError);
                if (nRet == -1)
                {
                    if (new_stop != null && new_stop.State != 0)
                        return 0;

                    DialogResult result = MessageBox.Show(owner,
    "�ӷ����� '" + this.PinyinServerUrl + "' ��ȡƴ���Ĺ��̳���:\r\n" + strError + "\r\n\r\n�Ƿ�Ҫ��ʱ��Ϊʹ�ñ�����ƴ������? \r\n\r\n(ע����ʱ���ñ���ƴ����״̬�ڳ����˳�ʱ���ᱣ�������Ҫ���ø��ñ���ƴ����ʽ����ʹ�����˵��ġ��������á��������������������ҳ�ġ�ƴ��������URL���������)",
    "EntityForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        this.ForceUseLocalPinyinFunc = true;
                        strError = "�����ñ���ƴ���������²���һ�Ρ�(���β�������: " + strError + ")";
                        return -1;
                    }
                    strError = " " + strError;
                    return -1;
                }

                if (nRet == -2)
                {
                    IdLoginDialog login_dlg = new IdLoginDialog();
                    login_dlg.Text = "���ƴ�� -- "
                        + ((string.IsNullOrEmpty(this.m_strPinyinGcatID) == true) ? "������ID" : strError);
                    login_dlg.ID = this.m_strPinyinGcatID;
                    login_dlg.SaveID = this.m_bSavePinyinGcatID;
                    login_dlg.StartPosition = FormStartPosition.CenterScreen;
                    if (login_dlg.ShowDialog(owner) == DialogResult.Cancel)
                    {
                        return 0;
                    }

                    this.m_strPinyinGcatID = login_dlg.ID;
                    this.m_bSavePinyinGcatID = login_dlg.SaveID;
                    goto REDO_GETPINYIN;
                }

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strPinyinXml);
                }
                catch (Exception ex)
                {
                    strError = "strPinyinXmlװ�ص�XMLDOMʱ����: " + ex.Message;
                    return -1;
                }

                foreach (XmlNode nodeWord in dom.DocumentElement.ChildNodes)
                {
                    if (nodeWord.NodeType == XmlNodeType.Text)
                    {
                        SelPinyinDlg.AppendText(ref strPinyin, nodeWord.InnerText);
                        nStatus = 0;
                        continue;
                    }

                    if (nodeWord.NodeType != XmlNodeType.Element)
                        continue;

                    string strWordPinyin = DomUtil.GetAttr(nodeWord, "p");
                    if (string.IsNullOrEmpty(strWordPinyin) == false)
                        strWordPinyin = strWordPinyin.Trim();

                    // Ŀǰֻȡ���׶����ĵ�һ��
                    nRet = strWordPinyin.IndexOf(";");
                    if (nRet != -1)
                        strWordPinyin = strWordPinyin.Substring(0, nRet).Trim();

                    string[] pinyin_parts = strWordPinyin.Split(new char[] { ' ' });
                    int index = 0;
                    // ��ѡ�������
                    foreach (XmlNode nodeChar in nodeWord.ChildNodes)
                    {
                        if (nodeChar.NodeType == XmlNodeType.Text)
                        {
                            SelPinyinDlg.AppendText(ref strPinyin, nodeChar.InnerText);
                            nStatus = 0;
                            continue;
                        }

                        string strHanzi = nodeChar.InnerText;
                        string strCharPinyins = DomUtil.GetAttr(nodeChar, "p");

                        if (String.IsNullOrEmpty(strCharPinyins) == true)
                        {
                            strPinyin += strHanzi;
                            nStatus = 0;
                            index++;
                            continue;
                        }

                        if (strCharPinyins.IndexOf(";") == -1)
                        {
                            DomUtil.SetAttr(nodeChar, "sel", strCharPinyins);
                            SelPinyinDlg.AppendPinyin(ref strPinyin,
                                SelPinyinDlg.ConvertSinglePinyinByStyle(
                                    strCharPinyins,
                                    style)
                                    );
                            nStatus = 2;
                            index++;
                            continue;
                        }

#if _TEST_PINYIN
                        // ���ԣ�
                        string[] parts = strCharPinyins.Split(new char[] {';'});
                        {
                            DomUtil.SetAttr(nodeChar, "sel", parts[0]);
                            AppendPinyin(ref strPinyin, parts[0]);
                            nStatus = 2;
                            index++;
                            continue;
                        }
#endif


                        string strSampleText = "";
                        int nOffs = -1;
                        SelPinyinDlg.GetOffs(dom.DocumentElement,
                            nodeChar,
                            out strSampleText,
                            out nOffs);

                        {	// ����Ƕ��ƴ��
                            SelPinyinDlg dlg = new SelPinyinDlg();
                            //float ratio_single = dlg.listBox_multiPinyin.Font.SizeInPoints / dlg.Font.SizeInPoints;
                            //float ratio_sample = dlg.textBox_sampleText.Font.SizeInPoints / dlg.Font.SizeInPoints;
                            MainForm.SetControlFont(dlg, this.Font, false);
                            // ά�������ԭ�д�С������ϵ
                            //dlg.listBox_multiPinyin.Font = new Font(dlg.Font.FontFamily, ratio_single * dlg.Font.SizeInPoints, GraphicsUnit.Point);
                            //dlg.textBox_sampleText.Font = new Font(dlg.Font.FontFamily, ratio_sample * dlg.Font.SizeInPoints, GraphicsUnit.Point);
                            // ����Ի���Ƚ����� MainForm.SetControlFont(dlg, this.Font, false);

                            dlg.Text = "��ѡ���� '" + strHanzi + "' ��ƴ�� (���Է����� " + this.PinyinServerUrl + ")";
                            dlg.SampleText = strSampleText;
                            dlg.Offset = nOffs;
                            dlg.Pinyins = strCharPinyins;
                            if (index < pinyin_parts.Length)
                                dlg.ActivePinyin = pinyin_parts[index];
                            dlg.Hanzi = strHanzi;

                            if (bAutoSel == true
                                && string.IsNullOrEmpty(dlg.ActivePinyin) == false)
                            {
                                dlg.ResultPinyin = dlg.ActivePinyin;
                                dlg.DialogResult = DialogResult.OK;
                            }
                            else
                            {
                                this.AppInfo.LinkFormState(dlg, "SelPinyinDlg_state");

                                dlg.ShowDialog(owner);

                                this.AppInfo.UnlinkFormState(dlg);
                            }

                            Debug.Assert(DialogResult.Cancel != DialogResult.Abort, "�ƶ�");

                            if (dlg.DialogResult == DialogResult.Abort)
                            {
                                return 0;   // �û�ϣ�������ж�
                            }

                            DomUtil.SetAttr(nodeChar, "sel", dlg.ResultPinyin);

                            if (dlg.DialogResult == DialogResult.Cancel)
                            {
                                SelPinyinDlg.AppendText(ref strPinyin, strHanzi);
                                nStatus = 2;
                                bNotFoundPinyin = true;
                            }
                            else if (dlg.DialogResult == DialogResult.OK)
                            {
                                SelPinyinDlg.AppendPinyin(ref strPinyin,
                                    SelPinyinDlg.ConvertSinglePinyinByStyle(
                                    dlg.ResultPinyin,
                                    style)
                                    );
                                nStatus = 2;
                            }
                            else
                            {
                                Debug.Assert(false, "SelPinyinDlg����ʱ���������DialogResultֵ");
                            }

                            index++;
                        }
                    }
                }

#if _TEST_PINYIN
#else
                // 2014/10/22
                // ɾ�� word �µ� Text �ڵ�
                XmlNodeList text_nodes = dom.DocumentElement.SelectNodes("word/text()");
                foreach (XmlNode node in text_nodes)
                {
                    Debug.Assert(node.NodeType == XmlNodeType.Text, "");
                    node.ParentNode.RemoveChild(node);
                }


                // ��û��p���Ե�<char>Ԫ��ȥ�����Ա��ϴ�
                XmlNodeList nodes = dom.DocumentElement.SelectNodes("//char");
                foreach (XmlNode node in nodes)
                {
                    string strP = DomUtil.GetAttr(node, "p");
                    string strSelValue = DomUtil.GetAttr(node, "sel");  // 2013/9/13

                    if (string.IsNullOrEmpty(strP) == true
                        || string.IsNullOrEmpty(strSelValue) == true)
                    {
                        XmlNode parent = node.ParentNode;
                        parent.RemoveChild(node);

                        // �ѿյ�<word>Ԫ��ɾ��
                        if (parent.Name == "word"
                            && parent.ChildNodes.Count == 0
                            && parent.ParentNode != null)
                        {
                            parent.ParentNode.RemoveChild(parent);
                        }
                    }

                    // TODO: һ��ƴ����û������ѡ��ģ��Ƿ�Ͳ������ˣ�
                    // ע�⣬ǰ�˸����´�����ƴ���������أ�ֻ�ǵ���ԭ���ӷ����������ģ�����������
                }

                if (dom.DocumentElement.ChildNodes.Count > 0)
                {
                    // return:
                    //      -2  strID��֤ʧ��
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = GcatNew.SetPinyin(
                        new_stop,
                        m_gcatClient,
                        "",
                        dom.DocumentElement.OuterXml,
                        out strError);
                    if (nRet == -1)
                    {
                        if (new_stop != null && new_stop.State != 0)
                            return 0;
                        return -1;
                    }
                }
#endif

                if (bNotFoundPinyin == false)
                    return 1;   // ��������

                return 2;   // ����ַ�������û���ҵ�ƴ���ĺ���
            }
            finally
            {
                new_stop.EndLoop();
                new_stop.OnStop -= new StopEventHandler(new_stop_OnStop);
                new_stop.Initial("");
                new_stop.Unregister();
                if (m_gcatClient != null)
                {
                    m_gcatClient.Close();
                    m_gcatClient = null;
                }
            }
        }

        void new_stop_OnStop(object sender, StopEventArgs e)
        {
            if (this.m_gcatClient != null)
            {
                this.m_gcatClient.Abort();
            }
        }

        // ���ַ����еĺ��ֺ�ƴ������
        // parameters:
        //      bLocal  �Ƿ�ӱ�����ȡƴ��
        // return:
        //      -1  ����
        //      0   �û�ϣ���ж�
        //      1   ����
        /// <summary>
        /// �����ַ���ת��Ϊƴ������ͨ��ʽ
        /// </summary>
        /// <param name="owner">���ں����� MessageBox �ͶԻ��� ����������</param>
        /// <param name="bLocal">�Ƿ�ӱ��ػ�ȡƴ����Ϣ</param>
        /// <param name="strText">�����ַ���</param>
        /// <param name="style">ת��Ϊƴ���ķ��</param>
        /// <param name="strPinyin">����ƴ���ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �û�ϣ���ж�; 1: ����; 2: ����ַ�������û���ҵ�ƴ���ĺ���</returns>
        public int HanziTextToPinyin(
            IWin32Window owner,
            bool bLocal,
            string strText,
            PinyinStyle style,
            out string strPinyin,
            out string strError)
        {
            strError = "";
            strPinyin = "";

            // string strSpecialChars = "���������������������������������ۣݡ����������������ܣ�������������";
            bool bNotFoundPinyin = false;   // �Ƿ���ֹ�û���ҵ�ƴ����ֻ�ܰѺ��ַ������ַ��������
            string strHanzi;
            int nStatus = -1;	// ǰ��һ���ַ������� -1:ǰ��û���ַ� 0:��ͨӢ����ĸ 1:�ո� 2:����

            for (int i = 0; i < strText.Length; i++)
            {
                char ch = strText[i];

                strHanzi = "";

                if (ch >= 0 && ch <= 128)
                {
                    if (nStatus == 2)
                        strPinyin += " ";

                    strPinyin += ch;

                    if (ch == ' ')
                        nStatus = 1;
                    else
                        nStatus = 0;

                    continue;
                }
                else
                {	// ����
                    strHanzi += ch;
                }

                // ����ǰ�������Ӣ�Ļ��ߺ��֣��м����ո�
                if (nStatus == 2 || nStatus == 0)
                    strPinyin += " ";

                // �����Ƿ��������
                if (StringUtil.SpecialChars.IndexOf(strHanzi) != -1)
                {
                    strPinyin += strHanzi;	// ���ڱ�Ӧ��ƴ����λ��
                    nStatus = 2;
                    continue;
                }

                // ���ƴ��
                string strResultPinyin = "";

                int nRet = 0;

                if (bLocal == true)
                {
                    nRet = this.LoadQuickPinyin(true, out strError);
                    if (nRet == -1)
                        return -1;
                    nRet = this.QuickPinyin.GetPinyin(
                        strHanzi,
                        out strResultPinyin,
                        out strError);
                }
                else
                {
                    throw new Exception("�ݲ�֧�ִ�ƴ�����л�ȡƴ��");
                    /*
                    nRet = GetOnePinyin(strHanzi,
                         out strResultPinyin,
                         out strError);
                     * */
                }
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {	
                    // canceled
                    strPinyin += strHanzi;	// ֻ�ý����ַ��ڱ�Ӧ��ƴ����λ��
                    nStatus = 2;
                    continue;
                }

                Debug.Assert(strResultPinyin != "", "");

                strResultPinyin = strResultPinyin.Trim();
                if (strResultPinyin.IndexOf(";", 0) != -1)
                {	// ����Ƕ��ƴ��
                    SelPinyinDlg dlg = new SelPinyinDlg();
                    //float ratio_single = dlg.listBox_multiPinyin.Font.SizeInPoints / dlg.Font.SizeInPoints;
                    //float ratio_sample = dlg.textBox_sampleText.Font.SizeInPoints / dlg.Font.SizeInPoints;
                    MainForm.SetControlFont(dlg, this.Font, false);
                    // ά�������ԭ�д�С������ϵ
                    //dlg.listBox_multiPinyin.Font = new Font(dlg.Font.FontFamily, ratio_single * dlg.Font.SizeInPoints, GraphicsUnit.Point);
                    //dlg.textBox_sampleText.Font = new Font(dlg.Font.FontFamily, ratio_sample * dlg.Font.SizeInPoints, GraphicsUnit.Point);
                    // ����Ի���Ƚ����� MainForm.SetControlFont(dlg, this.Font, false);

                    dlg.Text = "��ѡ���� '" + strHanzi + "' ��ƴ�� (���Ա���)";
                    dlg.SampleText = strText;
                    dlg.Offset = i;
                    dlg.Pinyins = strResultPinyin;
                    dlg.Hanzi = strHanzi;

                    this.AppInfo.LinkFormState(dlg, "SelPinyinDlg_state");

                    dlg.ShowDialog(owner);

                    this.AppInfo.UnlinkFormState(dlg);

                    Debug.Assert(DialogResult.Cancel != DialogResult.Abort, "�ƶ�");

                    if (dlg.DialogResult == DialogResult.Cancel)
                    {
                        strPinyin += strHanzi;
                        bNotFoundPinyin = true;
                    }
                    else if (dlg.DialogResult == DialogResult.OK)
                    {
                        strPinyin += SelPinyinDlg.ConvertSinglePinyinByStyle(
                            dlg.ResultPinyin,
                            style);
                    }
                    else if (dlg.DialogResult == DialogResult.Abort)
                    {
                        return 0;   // �û�ϣ�������ж�
                    }
                    else
                    {
                        Debug.Assert(false, "SelPinyinDlg����ʱ���������DialogResultֵ");
                    }
                }
                else
                {
                    // ����ƴ��

                    strPinyin += SelPinyinDlg.ConvertSinglePinyinByStyle(
                        strResultPinyin,
                        style);
                }
                nStatus = 2;
            }

            if (bNotFoundPinyin == false)
                return 1;   // ��������

            return 2;   // ����ַ�������û���ҵ�ƴ���ĺ���
        }

        // parameters:
        //      strIndicator    �ֶ�ָʾ���������null���ã����ʾ����ָʾ������ɸѡ
        // return:
        //      0   û���ҵ�ƥ�����������
        //      >=1 �ҵ��������ҵ��������������
        /// <summary>
        /// ��ú�һ���ֶ���ص�ƴ�����������
        /// </summary>
        /// <param name="cfg_dom">�洢��������Ϣ�� XmlDocument ����</param>
        /// <param name="strFieldName">�ֶ���</param>
        /// <param name="strIndicator">�ֶ�ָʾ��</param>
        /// <param name="cfg_items">����ƥ������������</param>
        /// <returns>0: û���ҵ�ƥ�����������; >=1: �ҵ���ֵΪ�����������</returns>
        public static int GetPinyinCfgLine(XmlDocument cfg_dom,
            string strFieldName,
            string strIndicator,
            out List<PinyinCfgItem> cfg_items)
        {
            cfg_items = new List<PinyinCfgItem>();

            XmlNodeList nodes = cfg_dom.DocumentElement.SelectNodes("item");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                PinyinCfgItem item = new PinyinCfgItem(node);

                if (item.FieldName != strFieldName)
                    continue;

                if (string.IsNullOrEmpty(item.IndicatorMatchCase) == false
                    && string.IsNullOrEmpty(strIndicator) == false)
                {
                    if (MarcUtil.MatchIndicator(item.IndicatorMatchCase, strIndicator) == false)
                        continue;
                }

                cfg_items.Add(item);
            }

            return cfg_items.Count;
        }

        // ��װ��� ���ֵ�ƴ�� ����
        // parameters:
        // return:
        //      -1  ����
        //      0   �û��ж�ѡ��
        //      1   �ɹ�
        /// <summary>
        /// �����ַ���ת��Ϊƴ��
        /// </summary>
        /// <param name="owner">���ں����� MessageBox �ͶԻ��� ����������</param>
        /// <param name="strHanzi">�����ַ���</param>
        /// <param name="style">ת��Ϊƴ���ķ��</param>
        /// <param name="bAutoSel">�Ƿ��Զ�ѡ�������</param>
        /// <param name="strPinyin">����ƴ���ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �û�ϣ���ж�; 1: ����; 2: ����ַ�������û���ҵ�ƴ���ĺ���</returns>
        public int GetPinyin(
            IWin32Window owner,
            string strHanzi,
            PinyinStyle style,  // PinyinStyle.None,
            bool bAutoSel,
            out string strPinyin,
            out string strError)
        {
            strError = "";
            strPinyin = "";
            int nRet = 0;

            // ���ַ����еĺ��ֺ�ƴ������
            // return:
            //      -1  ����
            //      0   �û�ϣ���ж�
            //      1   ����
            if (string.IsNullOrEmpty(this.PinyinServerUrl) == true
               || this.ForceUseLocalPinyinFunc == true)
            {
                nRet = this.HanziTextToPinyin(
                    owner,
                    true,	// ���أ�����
                    strHanzi,
                    style,
                    out strPinyin,
                    out strError);
            }
            else
            {
                // �����ַ���ת��Ϊƴ��
                // ����������Ѿ�MessageBox������strError��һ�ַ���Ϊ�ո�
                // return:
                //      -1  ����
                //      0   �û�ϣ���ж�
                //      1   ����
                nRet = this.SmartHanziTextToPinyin(
                    owner,
                    strHanzi,
                    style,
                    bAutoSel,
                    out strPinyin,
                    out strError);
            }
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "�û��жϡ�ƴ�����ֶ����ݿ��ܲ�������";
                return 0;
            }

            return 1;
        }
#if NO
        // ��װ��� ���ֵ�ƴ�� ����
        // parameters:
        // return:
        //      -1  ����
        //      0   �û��ж�ѡ��
        //      1   �ɹ�
        public int HanziTextToPinyin(string strHanzi,
            bool bAutoSel,
            PinyinStyle style,  // PinyinStyle.None,
            out string strPinyin,
            out string strError)
        {
            strError = "";
            strPinyin = "";
            int nRet = 0;

            // ���ַ����еĺ��ֺ�ƴ������
            // return:
            //      -1  ����
            //      0   �û�ϣ���ж�
            //      1   ����
            if (string.IsNullOrEmpty(this.PinyinServerUrl) == true
               || this.ForceUseLocalPinyinFunc == true)
            {
                nRet = this.HanziTextToPinyin(
                    this,
                    true,	// ���أ�����
                    strHanzi,
                    style,
                    out strPinyin,
                    out strError);
            }
            else
            {
                // �����ַ���ת��Ϊƴ��
                // ����������Ѿ�MessageBox������strError��һ�ַ���Ϊ�ո�
                // return:
                //      -1  ����
                //      0   �û�ϣ���ж�
                //      1   ����
                nRet = this.SmartHanziTextToPinyin(
                    this,
                    strHanzi,
                    style,
                    bAutoSel,
                    out strPinyin,
                    out strError);
            }
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "�û��жϡ�ƴ�����ֶ����ݿ��ܲ�������";
                return 0;
            }

            return 1;
        }
#endif

        // parameters:
        //      strPrefix   Ҫ����ƴ�����ֶ�����ǰ����ǰ׺�ַ��������� {cr:NLC} �� {cr:CALIS}
        // return:
        //      -1  ���������жϵ����
        //      0   ����
        /// <summary>
        /// Ϊ MarcRecord �����ڵļ�¼��ƴ��
        /// </summary>
        /// <param name="record">MARC ��¼����</param>
        /// <param name="strCfgXml">ƴ������ XML</param>
        /// <param name="style">���</param>
        /// <param name="strPrefix">ǰ׺�ַ�����ȱʡΪ��</param>
        /// <param name="bAutoSel">�Ƿ��Զ�ѡ�������</param>
        /// <returns>-1: ���������жϵ����; 0: ����</returns>
        public int AddPinyin(
            MarcRecord record,
            string strCfgXml,
            PinyinStyle style = PinyinStyle.None,
            string strPrefix = "",
            bool bAutoSel = false)
        {
            string strError = "";
            XmlDocument cfg_dom = new XmlDocument();
            try
            {
                cfg_dom.LoadXml(strCfgXml);
            }
            catch (Exception ex)
            {
                strError = "strCfgXmlװ�ص�XMLDOMʱ����: " + ex.Message;
                goto ERROR1;
            }

            string strRuleParam = "";
            if (string.IsNullOrEmpty(strPrefix) == false)
            {
                string strCmd = StringUtil.GetLeadingCommand(strPrefix);
                if (string.IsNullOrEmpty(strCmd) == false
&& StringUtil.HasHead(strCmd, "cr:") == true)
                {
                    strRuleParam = strCmd.Substring(3);
                }
            }

            MarcNodeList fields = record.select("field");

            foreach (MarcField field in fields)
            {

                List<PinyinCfgItem> cfg_items = null;
                int nRet = GetPinyinCfgLine(
                    cfg_dom,
                    field.Name,
                    field.Indicator,
                    out cfg_items);
                if (nRet <= 0)
                    continue;

                string strHanzi = "";

                string strFieldPrefix = "";

                // 2012/11/5
                // �۲��ֶ�����ǰ��� {} ����
                {
                    string strCmd = StringUtil.GetLeadingCommand(field.Content);
                    if (string.IsNullOrEmpty(strRuleParam) == false
                        && string.IsNullOrEmpty(strCmd) == false
                        && StringUtil.HasHead(strCmd, "cr:") == true)
                    {
                        string strCurRule = strCmd.Substring(3);
                        if (strCurRule != strRuleParam)
                            continue;
                    }
                    else if (string.IsNullOrEmpty(strCmd) == false)
                    {
                        strFieldPrefix = "{" + strCmd + "}";
                    }
                }

                // 2012/11/5
                // �۲� $* ���ֶ�
                {
                    MarcNodeList subfields = field.select("subfield[@name='*']");
                    //

                    if (subfields.count > 0)
                    {
                        string strCurStyle = subfields[0].Content;
                        if (string.IsNullOrEmpty(strRuleParam) == false
                            && strCurStyle != strRuleParam)
                            continue;
                        else if (string.IsNullOrEmpty(strCurStyle) == false)
                        {
                            strFieldPrefix = "{cr:" + strCurStyle + "}";
                        }
                    }
                }

                foreach (PinyinCfgItem item in cfg_items)
                {
                    for (int k = 0; k < item.From.Length; k++)
                    {
                        if (item.From.Length != item.To.Length)
                        {
                            strError = "�������� fieldname='" + item.FieldName + "' from='" + item.From + "' to='" + item.To + "' ����from��to����ֵ���ַ�������";
                            goto ERROR1;
                        }

                        string from = new string(item.From[k], 1);
                        string to = new string(item.To[k], 1);

                        // ɾ���Ѿ����ڵ�Ŀ�����ֶ�
                        field.select("subfield[@name='" + to + "']").detach();

                        MarcNodeList subfields = field.select("subfield[@name='" + from + "']");

                        foreach (MarcSubfield subfield in subfields)
                        {
                            strHanzi = subfield.Content;

                            if (DetailHost.ContainHanzi(strHanzi) == false)
                                continue;

                            string strSubfieldPrefix = "";  // ��ǰ���ֶ����ݱ������е�ǰ׺

                            // �������ǰ�����ܳ��ֵ� {} ����
                            string strCmd = StringUtil.GetLeadingCommand(strHanzi);
                            if (string.IsNullOrEmpty(strRuleParam) == false
                                && string.IsNullOrEmpty(strCmd) == false
                                && StringUtil.HasHead(strCmd, "cr:") == true)
                            {
                                string strCurRule = strCmd.Substring(3);
                                if (strCurRule != strRuleParam)
                                    continue;   // ��ǰ���ֶ����ں�strPrefix��ʾ�Ĳ�ͬ�ı�Ŀ������Ҫ������������ƴ��
                                strHanzi = strHanzi.Substring(strPrefix.Length); // ȥ�� {} ����
                            }
                            else if (string.IsNullOrEmpty(strCmd) == false)
                            {
                                strHanzi = strHanzi.Substring(strCmd.Length + 2); // ȥ�� {} ����
                                strSubfieldPrefix = "{" + strCmd + "}";
                            }

                            string strPinyin;

#if NO
                            // ���ַ����еĺ��ֺ�ƴ������
                            // return:
                            //      -1  ����
                            //      0   �û�ϣ���ж�
                            //      1   ����
                            if (string.IsNullOrEmpty(this.PinyinServerUrl) == true
                               || this.ForceUseLocalPinyinFunc == true)
                            {
                                nRet = this.HanziTextToPinyin(
                                    this,
                                    true,	// ���أ�����
                                    strHanzi,
                                    style,
                                    out strPinyin,
                                    out strError);
                            }
                            else
                            {
                                // �����ַ���ת��Ϊƴ��
                                // ����������Ѿ�MessageBox������strError��һ�ַ���Ϊ�ո�
                                // return:
                                //      -1  ����
                                //      0   �û�ϣ���ж�
                                //      1   ����
                                nRet = this.SmartHanziTextToPinyin(
                                    this,
                                    strHanzi,
                                    style,
                                    bAutoSel,
                                    out strPinyin,
                                    out strError);
                            }
#endif
                            nRet = this.GetPinyin(
                                this,
                                strHanzi,
                                style,
                                bAutoSel,
                                out strPinyin,
                                out strError);
                            if (nRet == -1)
                            {
                                goto ERROR1;
                            }
                            if (nRet == 0)
                            {
                                strError = "�û��жϡ�ƴ�����ֶ����ݿ��ܲ�������";
                                goto ERROR1;
                            }

                            string strContent = strPinyin;

                            if (string.IsNullOrEmpty(strPrefix) == false)
                                strContent = strPrefix + strPinyin;
                            else if (string.IsNullOrEmpty(strSubfieldPrefix) == false)
                                strContent = strSubfieldPrefix + strPinyin;

                            subfield.after(MarcQuery.SUBFLD + to + strPinyin);
                        }
                    }
                }
            }

            return 0;
        ERROR1:
            if (string.IsNullOrEmpty(strError) == false)
            {
                if (strError[0] != ' ')
                    MessageBox.Show(this, strError);
            }
            return -1;
        }

        /// <summary>
        /// Ϊ MarcRecord �����ڵļ�¼ɾ��ƴ��
        /// </summary>
        /// <param name="record">MARC ��¼����</param>
        /// <param name="strCfgXml">ƴ������ XML</param>
        /// <param name="strPrefix">ǰ׺�ַ�����ȱʡΪ��</param>
        public void RemovePinyin(
            MarcRecord record,
            string strCfgXml,
            string strPrefix = "")
        {
            string strError = "";
            XmlDocument cfg_dom = new XmlDocument();
            try
            {
                cfg_dom.LoadXml(strCfgXml);
            }
            catch (Exception ex)
            {
                strError = "strCfgXmlװ�ص�XMLDOMʱ����: " + ex.Message;
                goto ERROR1;
            }

            string strRuleParam = "";
            if (string.IsNullOrEmpty(strPrefix) == false)
            {
                string strCmd = StringUtil.GetLeadingCommand(strPrefix);
                if (string.IsNullOrEmpty(strCmd) == false
&& StringUtil.HasHead(strCmd, "cr:") == true)
                {
                    strRuleParam = strCmd.Substring(3);
                }
            }

            MarcNodeList fields = record.select("field");

            foreach (MarcField field in fields)
            {

                List<PinyinCfgItem> cfg_items = null;
                int nRet = GetPinyinCfgLine(
                    cfg_dom,
                    field.Name,
                    field.Indicator,    // TODO: ���Բ�����ָʾ�������������ɾ������Ѱ��Χ
                    out cfg_items);
                if (nRet <= 0)
                    continue;

                string strField = field.Text;

                // �۲��ֶ�����ǰ��� {} ����
                if (string.IsNullOrEmpty(strRuleParam) == false)
                {
                    string strCmd = StringUtil.GetLeadingCommand(field.Content);
                    if (string.IsNullOrEmpty(strRuleParam) == false
                        && string.IsNullOrEmpty(strCmd) == false
                        && StringUtil.HasHead(strCmd, "cr:") == true)
                    {
                        string strCurRule = strCmd.Substring(3);
                        if (strCurRule != strRuleParam)
                            continue;
                    }
                }

                // 2012/11/6
                // �۲� $* ���ֶ�
                if (string.IsNullOrEmpty(strRuleParam) == false)
                {
                    MarcNodeList subfields = field.select("subfield[@name='*']");
                    if (subfields.count > 0)
                    {
                        string strCurStyle = subfields[0].Content;
                        if (string.IsNullOrEmpty(strRuleParam) == false
                            && strCurStyle != strRuleParam)
                            continue;
                    }
                }

                foreach (PinyinCfgItem item in cfg_items)
                {
                    for (int k = 0; k < item.To.Length; k++)
                    {
                        string to = new string(item.To[k], 1);
                        if (string.IsNullOrEmpty(strPrefix) == true)
                        {
                            // ɾ���Ѿ����ڵ�Ŀ�����ֶ�
                            field.select("subfield[@name='" + to + "']").detach();
                        }
                        else
                        {
                            MarcNodeList subfields = field.select("subfield[@name='" + to + "']");

                            // ֻɾ�������ض�ǰ׺�����ݵ����ֶ�
                            foreach (MarcSubfield subfield in subfields)
                            {
                                string strContent = subfield.Content;
                                if (subfield.Content.Length == 0)
                                    subfields.detach(); // �����ݵ����ֶ�Ҫɾ��
                                else
                                {
                                    if (StringUtil.HasHead(subfield.Content, strPrefix) == true)
                                        subfields.detach();
                                }
                            }
                        }
                    }
                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        #endregion

        #region QR ʶ��

        // ��ǰ���������������
        InputType m_inputType = InputType.None;

        // �����뽹����� ���߱�ʶ �༭�� ��ʱ�򴥷�
        internal void EnterPatronIdEdit(InputType inputtype)
        {
            m_inputType = inputtype;
            this.qrRecognitionControl1.StartCatch();
        }

        // �����뽹���뿪 ���߱�ʶ �༭�� ��ʱ�򴥷�
        internal void LeavePatronIdEdit()
        {
            this.qrRecognitionControl1.EndCatch();
            // m_bDisableCamera = false;
        }

        // �����ֹ�ظ��Ļ��������
        public void ClearQrLastText()
        {
            this.qrRecognitionControl1.LastText = "";
        }

        bool m_bDisableCamera = false;
        // string _cameraName = "";

        /// <summary>
        /// ����ͷ��ֹ����
        /// </summary>
        public void DisableCamera()
        {
            //    _cameraName = this.qrRecognitionControl1.CurrentCamera;
            if (this.qrRecognitionControl1.InCatch == true)
            {
                this.qrRecognitionControl1.EndCatch();
                this.m_bDisableCamera = true;

                // this.qrRecognitionControl1.CurrentCamera = "";
            }
        }

        /// <summary>
        /// ����ͷ�ָ�����
        /// </summary>
        public void EnableCamera()
        {
            //    this.qrRecognitionControl1.CurrentCamera = _cameraName;
            if (m_bDisableCamera == true)
            {
                this.qrRecognitionControl1.StartCatch();
                this.m_bDisableCamera = false;
            }
        }

        void qrRecognitionControl1_Catched(object sender, DigitalPlatform.Drawing.CatchedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text) == true)
                return;

            int nHitCount = 0;  // ƥ��Ĵ���
            if ((this.m_inputType & InputType.QR) == InputType.QR)
            {
                if ((e.BarcodeFormat & ZXing.BarcodeFormat.QR_CODE) != 0)
                    nHitCount++;
            }
            // ����Ƿ����� PQR ��ά��
            if ((this.m_inputType & InputType.PQR) == InputType.PQR)
            {
                if ((e.BarcodeFormat & ZXing.BarcodeFormat.QR_CODE) != 0
                    && StringUtil.HasHead(e.Text, "PQR:") == true)
                    nHitCount++;
            }
            // ����Ƿ����� ISBN һά��
            if ((this.m_inputType & InputType.EAN_BARCODE) == InputType.EAN_BARCODE)
            {
                if ((e.BarcodeFormat & ZXing.BarcodeFormat.EAN_13) != 0
                    /* && IsbnSplitter.IsIsbn13(e.Text) == true */)
                    nHitCount++;
            }
            // ����Ƿ�������ͨһά��
            if ((this.m_inputType & InputType.NORMAL_BARCODE) == InputType.NORMAL_BARCODE)
            {
                if ((e.BarcodeFormat & ZXing.BarcodeFormat.All_1D) > 0)
                    nHitCount++;
            }

            if (nHitCount > 0)
            {
                // SendKeys.Send(e.Text + "\r");
                Invoke(new Action<string>(SendKey), e.Text + "\r");
            }
            else
            {
                // TODO: ����
            }
        }

        private void SendKey(string strText)
        {
            SendKeys.Send(strText);
        }

        #endregion

        private void MenuItem_openUserFolder_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(this.UserDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        // 
        /// <summary>
        /// ��־��ȡ��ϸ����0 ����ϸ 1 ���� 2 �����
        /// </summary>
        public int OperLogLevel
        {
            get
            {
                string strText = this.AppInfo.GetString(
                    "operlog_form",
                    "level",
                    "1 -- ����");
                string strNumber = StringUtil.GetLeft(strText);
                int v = 0;
                Int32.TryParse(strNumber, out v);
                return v;
            }
        }

        private void MenuItem_closeAllMdiWindows_Click(object sender, EventArgs e)
        {
            foreach (Form form in this.MdiChildren)
            {
                form.Close();
            }
        }

        /// <summary>
        /// �ʶ�
        /// </summary>
        /// <param name="strText">Ҫ�ʶ����ı�</param>
        public void Speak(string strText)
        {
            this.m_speech.SpeakAsyncCancelAll();
            this.m_speech.SpeakAsync(strText);
            // MessageBox.Show(this, strText);
        }

        private void toolStripMenuItem_fixedPanel_clear_Click(object sender, EventArgs e)
        {
            if (this.tabControl_panelFixed.SelectedTab == this.tabPage_history)
            {
                this.OperHistory.ClearHtml();
            }
            else if (this.tabControl_panelFixed.SelectedTab == this.tabPage_camera)
            {
                this.qrRecognitionControl1.CurrentCamera = "";
            }
        }

        delegate object Delegate_InvokeScript(WebBrowser webBrowser,
            string strFuncName, object[] args);

        public object InvokeScript(
            WebBrowser webBrowser,
            string strFuncName, 
            object[] args)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    Delegate_InvokeScript d = new Delegate_InvokeScript(InvokeScript);
                    return this.Invoke(d, new object[] { webBrowser, strFuncName, args });
                }

                return webBrowser.Document.InvokeScript(strFuncName, args);
            }
            catch
            {
                return null;
            }
        }

        public void BeginInvokeScript(
    WebBrowser webBrowser,
    string strFuncName,
    object[] args)
        {

            try
            {
                Delegate_InvokeScript d = new Delegate_InvokeScript(InvokeScript);
                this.BeginInvoke(d, new object[] { webBrowser, strFuncName, args });
            }
            catch
            {
            }
        }

        private void MenuItem_inventory_Click(object sender, EventArgs e)
        {
#if NO
            NewInventoryForm form = new NewInventoryForm();
            form.MdiParent = this;
            form.Show();
#endif
            // OpenWindow<NewInventoryForm>();
            OpenWindow<InventoryForm>();

        }

        private void tabControl_panelFixed_SizeChanged(object sender, EventArgs e)
        {
            if (this.qrRecognitionControl1 != null)
            {
                this.qrRecognitionControl1.PerformAutoScale();
                this.qrRecognitionControl1.PerformLayout();
            }
        }

        private void contextMenuStrip_fixedPanel_Opening(object sender, CancelEventArgs e)
        {
            // �̶���� ��������ҳ �����Ĳ˵������֡���Ϊ��Ƕ�Ĺ����Լ�������
            if (this.tabControl_panelFixed.SelectedTab == this.tabPage_accept)
                e.Cancel = true;
        }

        private void tabPage_accept_Enter(object sender, EventArgs e)
        {
            if (this._acceptForm != null)
                this._acceptForm.EnableProgress();
        }

        private void tabPage_accept_Leave(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// ����Ի����
        /// </summary>
        /// <param name="keyData">System.Windows.Forms.Keys ֵ֮һ������ʾҪ����ļ���</param>
        /// <returns>����ؼ�����ʹ�û�������Ϊ true������Ϊ false���������һ������</returns>
        protected override bool ProcessDialogKey(
Keys keyData)
        {
#if NO
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
#endif

            if (keyData == Keys.Enter)
            {
                if (this.tabControl_panelFixed.SelectedTab == this.tabPage_accept
                    && this._acceptForm != null)
                    this._acceptForm.DoEnterKey();
                // return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        private void MenuItem_messageForm_Click(object sender, EventArgs e)
        {
            OpenWindow<MessageForm>();
        }

        #region ���кŻ���

        bool _testMode = false;

        public bool TestMode
        {
            get
            {
                return this._testMode;
            }
            set
            {
                this._testMode = value;
                SetTitle();
            }
        }

        void SetTitle()
        {
            if (this.TestMode == true)
                this.Text = "dp2Circulation V2 -- ���� [����ģʽ]";
            else
                this.Text = "dp2Circulation V2 -- ����";

        }

#if SN
        // �������ַ���ƥ�����к�
        bool MatchLocalString(string strSerialNumber)
        {
            List<string> macs = SerialCodeForm.GetMacAddress();
            foreach (string mac in macs)
            {
                string strLocalString = GetEnvironmentString(mac);
                string strSha1 = Cryptography.GetSHA1(StringUtil.SortParams(strLocalString) + "_reply");
                if (strSha1 == SerialCodeForm.GetCheckCode(strSerialNumber))
                    return true;
            }

            // 2014/12/19
            if (DateTime.Now.Month == 12)
            {
                foreach (string mac in macs)
                {
                    string strLocalString = GetEnvironmentString(mac, true);
                    string strSha1 = Cryptography.GetSHA1(StringUtil.SortParams(strLocalString) + "_reply");
                    if (strSha1 == SerialCodeForm.GetCheckCode(strSerialNumber))
                        return true;
                }
            }

            return false;
        }


        // parameters:
        //      strRequirFuncList   Ҫ�����߱��Ĺ����б����ż�����ַ���
        //      bReinput    ������кŲ�����Ҫ���Ƿ�ֱ�ӳ��ֶԻ������û������������к�
        // return:
        //      -1  ����
        //      0   ��ȷ
        internal int VerifySerialCode(string strRequirFuncList,
            bool bReinput,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // 2014/11/15
            string strFirstMac = "";
            List<string> macs = SerialCodeForm.GetMacAddress();
            if (macs.Count != 0)
            {
                strFirstMac = macs[0];
            }

            string strSerialCode = this.AppInfo.GetString("sn", "sn", "");

            // �״�����
            if (string.IsNullOrEmpty(strSerialCode) == true)
            {
            }

        REDO_VERIFY:

            if (strSerialCode == "test")
            {
                if (string.IsNullOrEmpty(strRequirFuncList) == true)
                {
                    this.TestMode = true;
                    // ����д�� ����ģʽ ��Ϣ����ֹ�û�����
                    // С�Ͱ�û�ж�Ӧ������ģʽ
                    this.AppInfo.SetString("main_form", "last_mode", "test");
                    return 0;
                }
            }
            else
            {
                this.TestMode = false;
                this.AppInfo.SetString("main_form", "last_mode", "standard");
            }

            //string strLocalString = GetEnvironmentString();

            //string strSha1 = Cryptography.GetSHA1(StringUtil.SortParams(strLocalString) + "_reply");

        if (CheckFunction(GetEnvironmentString(""), strRequirFuncList) == false ||
                // strSha1 != GetCheckCode(strSerialCode)
                MatchLocalString(strSerialCode) == false
                || String.IsNullOrEmpty(strSerialCode) == true)
            {
                if (bReinput == false)
                {
                    strError = "���к���Ч";
                    return -1;
                }

                if (String.IsNullOrEmpty(strSerialCode) == false)
                    MessageBox.Show(this, "���к���Ч������������");
                else if (CheckFunction(GetEnvironmentString(""), strRequirFuncList) == false)
                    MessageBox.Show(this, "���к��� function ������Ч������������");

                // �����������кŶԻ���
                nRet = ResetSerialCode(
                    false,
                    strSerialCode,
                    GetEnvironmentString(strFirstMac));
                if (nRet == 0)
                {
                    strError = "����";
                    return -1;
                }
                strSerialCode = this.AppInfo.GetString("sn", "sn", "");
                goto REDO_VERIFY;
            }
            return 0;
        }

        // return:
        //      false   ������
        //      true    ����
        bool CheckFunction(string strEnvString,
            string strFuncList)
        {
            Hashtable table = StringUtil.ParseParameters(strEnvString);
            string strFuncValue = (string)table["function"];
            string[] parts = strFuncList.Split(new char[] {','});
            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part) == true)
                    continue;
                if (StringUtil.IsInList(part, strFuncValue) == false)
                    return false;
            }

            return true;
        }

        // parameters:
        string GetEnvironmentString(string strMAC,
            bool bNextYear = false)
        {
            Hashtable table = new Hashtable();
            table["mac"] = strMAC;  // SerialCodeForm.GetMacAddress();
            // table["time"] = GetTimeRange();
            if (bNextYear == false)
                table["time"] = SerialCodeForm.GetTimeRange();
            else
                table["time"] = SerialCodeForm.GetNextYearTimeRange();

            table["product"] = "dp2circulation";

            string strSerialCode = this.AppInfo.GetString("sn", "sn", "");
            // �� strSerialCode �е���չ�����趨�� table ��
            SerialCodeForm.SetExtParams(ref table, strSerialCode);
#if NO
            if (string.IsNullOrEmpty(strSerialCode) == false)
            {
                string strExtParam = GetExtParams(strSerialCode);
                if (string.IsNullOrEmpty(strExtParam) == false)
                {
                    Hashtable ext_table = StringUtil.ParseParameters(strExtParam);
                    string function = (string)ext_table["function"];
                    if (string.IsNullOrEmpty(function) == false)
                        table["function"] = function;
                }
            }
#endif

            return StringUtil.BuildParameterString(table);
        }



        // ��� xxx|||xxxx ����߲���
        static string GetCheckCode(string strSerialCode)
        {
            string strSN = "";
            string strExtParam = "";
            StringUtil.ParseTwoPart(strSerialCode,
                "|||",
                out strSN,
                out strExtParam);

            return strSN;
        }

        // ��� xxx|||xxxx ���ұ߲���
        static string GetExtParams(string strSerialCode)
        {
            string strSN = "";
            string strExtParam = "";
            StringUtil.ParseTwoPart(strSerialCode,
                "|||",
                out strSN,
                out strExtParam);

            return strExtParam;
        }

#if NO
        static string GetTimeRange()
        {
            DateTime now = DateTime.Now;
            return now.Year.ToString().PadLeft(4, '0');
        }
#endif

        string CopyrightKey = "dp2circulation_sn_key";

        // return:
        //      0   Cancel
        //      1   OK
        int ResetSerialCode(
            bool bAllowSetBlank,
            string strOldSerialCode,
            string strOriginCode)
        {
            _expireVersionChecked = false;

            Hashtable ext_table = StringUtil.ParseParameters(strOriginCode);
            string strMAC = (string)ext_table["mac"];
            if (string.IsNullOrEmpty(strMAC) == true)
                strOriginCode = "!error";
            else
                strOriginCode = Cryptography.Encrypt(strOriginCode,
                    this.CopyrightKey);
            SerialCodeForm dlg = new SerialCodeForm();
            dlg.Font = this.Font;
            dlg.SerialCode = strOldSerialCode;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.OriginCode = strOriginCode;

        REDO:
            dlg.ShowDialog(this);
            if (dlg.DialogResult != DialogResult.OK)
                return 0;

            if (string.IsNullOrEmpty(dlg.SerialCode) == true)
            {
                if (bAllowSetBlank == true)
                {
                    DialogResult result = MessageBox.Show(this,
        "ȷʵҪ�����к�����Ϊ��?\r\n\r\n(һ�������к�����Ϊ�գ�dp2Circulation ���Զ��˳����´�������Ҫ�����������к�)",
        "dp2Circulation",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button2);
                    if (result == System.Windows.Forms.DialogResult.No)
                    {
                        return 0;
                    }
                }
                else
                {
                    MessageBox.Show(this, "���кŲ�����Ϊ�ա�����������");
                    goto REDO;
                }
            }

            this.AppInfo.SetString("sn", "sn", dlg.SerialCode);
            this.AppInfo.Save();

            return 1;
        }

#endif

        #endregion

        private void MenuItem_resetSerialCode_Click(object sender, EventArgs e)
        {
#if SN
            string strError = "";
            int nRet = 0;

            // 2014/11/15
            string strFirstMac = "";
            List<string> macs = SerialCodeForm.GetMacAddress();
            if (macs.Count != 0)
            {
                strFirstMac = macs[0];
            }

            string strRequirFuncList = "";  // ��Ϊ����������ͨ�õ����кţ�����������ĸ����ܣ����Զ����ú����кŵĹ��ܲ�����顣ֻ�еȵ��õ����幦�ܵ�ʱ�򣬲��ܷ������к��Ƿ�������幦�ܵ� function = ... ����

            string strSerialCode = "";
        REDO_VERIFY:

            if (strSerialCode == "test")
            {
                this.TestMode = true;
                // ����д�� ����ģʽ ��Ϣ����ֹ�û�����
                // С�Ͱ�û�ж�Ӧ������ģʽ
                this.AppInfo.SetString("main_form", "last_mode", "test");
                return;
            }
            else
            {
                this.TestMode = false;
                this.AppInfo.SetString("main_form", "last_mode", "standard");
            }

            //string strLocalString = GetEnvironmentString();

            //string strSha1 = Cryptography.GetSHA1(StringUtil.SortParams(strLocalString) + "_reply");

        if (CheckFunction(GetEnvironmentString(""), strRequirFuncList) == false ||
                // strSha1 != GetCheckCode(strSerialCode) 
                MatchLocalString(strSerialCode) == false
                || String.IsNullOrEmpty(strSerialCode) == true)
            {
                if (String.IsNullOrEmpty(strSerialCode) == false)
                    MessageBox.Show(this, "���к���Ч������������");
                else if (CheckFunction(GetEnvironmentString(""), strRequirFuncList) == false)
                    MessageBox.Show(this, "���к��� function ������Ч������������");


                // �����������кŶԻ���
                nRet = ResetSerialCode(
                    true,
                    strSerialCode,
                    GetEnvironmentString(strFirstMac));
                if (nRet == 0)
                {
                    strError = "����";
                    goto ERROR1;
                }
                strSerialCode = this.AppInfo.GetString("sn", "sn", "");
                if (string.IsNullOrEmpty(strSerialCode) == true)
                {
                    Application.Exit();
                    return;
                }

                this.AppInfo.Save();
                goto REDO_VERIFY;
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
        }

        private void MenuItem_openEntityRegisterForm_Click(object sender, EventArgs e)
        {
            OpenWindow<EntityRegisterForm>();
        }

        private void MenuItem_openEntityRegisterWizard_Click(object sender, EventArgs e)
        {
            OpenWindow<EntityRegisterWizard>();
        }

        private void MenuItem_reLogin_Click(object sender, EventArgs e)
        {
            StartPrepareNames(true, false);
        }

        // ���һ����ʱ�ļ������ļ���δ����
        public string GetTempFileName(string strPrefix)
        {
            return Path.Combine(this.UserTempDir, "~" + strPrefix + Guid.NewGuid().ToString());
        }

        #region servers.xml

        static string _baseCfg = @"
<root>
  <server name='�����.����ƽ̨����' type='dp2library' url='http://123.103.13.236/dp2library' userName='public'/>
  <server name='����ѷ�й�' type='amazon' url='webservices.amazon.cn'/>
</root>";

        // ���� servers.xml �����ļ�
        public int BuildServersCfgFile(string strCfgFileName,
            out string strError)
        {
            strError = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml(_baseCfg);

            // �汾��
            // 0.01 2014/12/10
            dom.DocumentElement.SetAttribute("version", "0.01");

            // ��ӵ�ǰ������
            {
                XmlElement server = dom.CreateElement("server");
                dom.DocumentElement.AppendChild(server);
                server.SetAttribute("name", "��ǰ������");
                server.SetAttribute("type", "dp2library");
                server.SetAttribute("url", ".");
                server.SetAttribute("userName", ".");

                int nCount = 0;
                // ��� database Ԫ��
                foreach (BiblioDbProperty prop in this.BiblioDbProperties)
                {
                    // USMARC ��ʽ�ģ����ڿ��⣬������
                    if (prop.Syntax != "unimarc"
                        || string.IsNullOrEmpty(prop.IssueDbName) == false)
                        continue;

                    // ��ʱ��
                    if (StringUtil.IsInList("catalogWork", prop.Role) == true)
                    {
                        XmlElement database = dom.CreateElement("database");
                        server.AppendChild(database);

                        database.SetAttribute("name", prop.DbName);
                        database.SetAttribute("isTarget", "yes");
                        database.SetAttribute("access", "append,overwrite");

                        database.SetAttribute("entityAccess", "append,overwrite");
                        nCount++;
                    }

                    // �����
                    if (StringUtil.IsInList("catalogTarget", prop.Role) == true)
                    {
                        XmlElement database = dom.CreateElement("database");
                        server.AppendChild(database);

                        database.SetAttribute("name", prop.DbName);
                        database.SetAttribute("isTarget", "yes");
                        database.SetAttribute("access", "append,overwrite");

                        database.SetAttribute("entityAccess", "append,overwrite");
                        nCount++;
                    }
                }

                if (nCount == 0)
                {
                    strError = "��ǰ��δ�����ɫΪ catalogWork �� catalogTarget ��ͼ����Ŀ�⡣���������������ļ�ʧ��";
                    return -1;
                }
            }

            string strHnbUrl = "";
            {
                XmlElement server = dom.DocumentElement.SelectSingleNode("server[@name='�����.����ƽ̨����']") as XmlElement;
                if (server != null)
                    strHnbUrl = server.GetAttribute("url");
                // ��ǰ�������Ǻ���ͷ�������Ҫɾ�����������
                if (string.Compare(this.LibraryServerUrl, strHnbUrl, true) == 0)
                {
                    server.ParentNode.RemoveChild(server);
                }
            }

            dom.Save(strCfgFileName);
            return 0;
        }

        // ��������ļ��İ汾��
        public static double GetServersCfgFileVersion(string strCfgFileName)
        {
            if (File.Exists(strCfgFileName) == false)
                return 0;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.Load(strCfgFileName);
            }
            catch (Exception ex)
            {
                return 0;
            }

            if (dom.DocumentElement == null)
                return 0;

            double version = 0;
            string strVersion = dom.DocumentElement.GetAttribute("version");
            if (double.TryParse(strVersion, out version) == false)
                return 0;

            return version;
        }

        #endregion // servers.xml

        #region ��Ϣ����

#if NO
        public event MessageFilterEventHandler MessageFilter = null;

        // Creates a  message filter.
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public class MouseLButtonMessageFilter : IMessageFilter
        {
            public MainForm MainForm = null;
            public bool PreFilterMessage(ref Message m)
            {
                // Blocks all the messages relating to the left mouse button. 
                if (m.Msg >= 513 && m.Msg <= 515)
                {
                    if (this.MainForm.MessageFilter != null)
                    {
                        MessageFilterEventArgs e = new MessageFilterEventArgs();
                        e.Message = m;
                        this.MainForm.MessageFilter(this, e);
                        m = e.Message;
                        return e.ReturnValue;
                    }
                }
                return false;
            }
        }

#endif

        #endregion

    }

    /// <summary>
    /// ��Ϣ�����¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void MessageFilterEventHandler(object sender,
    MessageFilterEventArgs e);

    /// <summary>
    /// ��Ϣ�����¼��Ĳ���
    /// </summary>
    public class MessageFilterEventArgs : EventArgs
    {
        public Message Message;  // [in][out]
        public bool ReturnValue = false;    // true ��ʾҪ�̵������Ϣ�� false ��ʾ�����������Ϣ
    }

    [Flags]
    internal enum InputType
    {
        None = 0,
        QR = 0x01,             // QR ��
        PQR = 0x02,             // PRQ: ������ QR ��
        EAN_BARCODE = 0x04,    // EAN (���� ISBN) ���롣ע�⣬���� QR ��
        NORMAL_BARCODE = 0x08,    // ��ͨ 1D ����
        ALL = (QR | PQR | EAN_BARCODE | NORMAL_BARCODE)  // �������͵� Mask
    }

    // 
    /// <summary>
    /// ʵ�ÿ�����
    /// </summary>
    public class UtilDbProperty
    {
        /// <summary>
        /// ���ݿ���
        /// </summary>
        public string DbName = "";  // ����

        /// <summary>
        /// ����
        /// </summary>
        public string Type = "";  // ���ͣ���;
    }

    // 
    /// <summary>
    /// ��Ŀ������
    /// </summary>
    public class BiblioDbProperty
    {
        /// <summary>
        /// ��Ŀ����
        /// </summary>
        public string DbName = "";  // ��Ŀ����
        /// <summary>
        /// ��ʽ�﷨
        /// </summary>
        public string Syntax = "";  // ��ʽ�﷨

        /// <summary>
        /// ʵ�����
        /// </summary>
        public string ItemDbName = "";  // ��Ӧ��ʵ�����

        /// <summary>
        /// �ڿ���
        /// </summary>
        public string IssueDbName = ""; // ��Ӧ���ڿ��� 2007/10/19 new add

        /// <summary>
        /// ��������
        /// </summary>
        public string OrderDbName = ""; // ��Ӧ�Ķ������� 2007/11/30 new add

        /// <summary>
        /// ��ע����
        /// </summary>
        public string CommentDbName = "";   // ��Ӧ����ע���� 2009/10/23 new add

        /// <summary>
        /// ��ɫ
        /// </summary>
        public string Role = "";    // ��ɫ 2009/10/23 new add

        /// <summary>
        /// �Ƿ������ͨ
        /// </summary>
        public bool InCirculation = true;  // �Ƿ������ͨ 2009/10/23 new add
    }

    // 
    /// <summary>
    /// ���߿�����
    /// </summary>
    public class ReaderDbProperty
    {
        /// <summary>
        /// ���߿���
        /// </summary>
        public string DbName = "";  // ���߿���
        /// <summary>
        /// �Ƿ������ͨ
        /// </summary>
        public bool InCirculation = true;  // �Ƿ������ͨ
        /// <summary>
        /// �ݴ���
        /// </summary>
        public string LibraryCode = ""; // �ݴ���
    }

    // 
    /// <summary>
    /// ��ͨ�������
    /// </summary>
    public class NormalDbProperty
    {
        /// <summary>
        /// ���ݿ���
        /// </summary>
        public string DbName = "";

        /// <summary>
        /// �����Ŀ���Լ���
        /// </summary>
        public ColumnPropertyCollection ColumnProperties = new ColumnPropertyCollection();
    }

    /// <summary>
    /// �ż���ϵ��Ϣ
    /// </summary>
    public class ArrangementInfo
    {
        /// <summary>
        /// �ż���ϵ��
        /// </summary>
        public string ArrangeGroupName = "";
        /// <summary>
        /// �ִκ����ݿ���
        /// </summary>
        public string ZhongcihaoDbname = "";
        /// <summary>
        /// �������
        /// </summary>
        public string ClassType = "";
        /// <summary>
        /// ���ֺ�����
        /// </summary>
        public string QufenhaoType = "";
        /// <summary>
        /// ��ȡ������
        /// </summary>
        public string CallNumberStyle = "";

        /// <summary>
        /// ���� XmlNode ���챾����
        /// </summary>
        /// <param name="nodeArrangementGroup">����ڵ����</param>
        public void Fill(XmlNode nodeArrangementGroup)
        {
            this.ArrangeGroupName = DomUtil.GetAttr(nodeArrangementGroup, "name");
            this.ZhongcihaoDbname = DomUtil.GetAttr(nodeArrangementGroup, "zhongcihaodb");
            this.ClassType = DomUtil.GetAttr(nodeArrangementGroup, "classType");
            this.QufenhaoType = DomUtil.GetAttr(nodeArrangementGroup, "qufenhaoType");
            this.CallNumberStyle = DomUtil.GetAttr(nodeArrangementGroup, "callNumberStyle");
        }
    }

    // 
    /// <summary>
    /// ��ӡ����ѡ������Ϣ
    /// </summary>
    public class PrinterInfo
    {
        /// <summary>
        /// ����
        /// </summary>
        public string Type = "";
        /// <summary>
        /// Ԥ��ȱʡ��ӡ�����֣�����ѡ����Ĵ�ӡ������
        /// </summary>
        public string PrinterName = "";  // Ԥ��ȱʡ��ӡ�����֣�����ѡ����Ĵ�ӡ������
        /// <summary>
        /// Ԥ��ȱʡ��ֽ�ųߴ�����
        /// </summary>
        public string PaperName = "";   // Ԥ��ȱʡ��ֽ�ųߴ�����

        /// <summary>
        /// ��ӡֽ����
        /// </summary>
        public bool Landscape = false;  // ��ӡֽ����

        // 
        /// <summary>
        /// �����ı�������ʽ����
        /// </summary>
        /// <param name="strType">����</param>
        /// <param name="strText">���ġ���ʽΪ printerName=/??;paperName=???</param>
        public PrinterInfo(string strType,
            string strText)
        {
            this.Type = strType;

            Hashtable table = StringUtil.ParseParameters(strText,
                ';',
                '=');
            this.PrinterName = (string)table["printerName"];
            this.PaperName = (string)table["paperName"];
            string strLandscape = (string)table["landscape"];
            if (string.IsNullOrEmpty(strLandscape) == true)
                this.Landscape = false;
            else
                this.Landscape = DomUtil.IsBooleanTrue(strLandscape);
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public PrinterInfo()
        {
        }

        // 
        /// <summary>
        /// ����ı�������ʽ
        /// </summary>
        /// <returns>�����ı�������ʽ����ʽΪ printerName=/??;paperName=???</returns>
        public string GetText()
        {
            return "printerName=" + this.PrinterName
                + ";paperName=" + this.PaperName
                + (this.Landscape == true ? ";landscape=yes" : "");
        }
    }

    /// <summary>
    /// ȫ�ֱ�������
    /// </summary>
    public static class GlobalVars
    {
        /// <summary>
        /// ˽�����弯��
        /// </summary>
        public static PrivateFontCollection PrivateFonts = new PrivateFontCollection();
    }

    /// <summary>
    /// ��ͨ / ����ǰ�˳��� dp2circulation.exe
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NamespaceDoc
    {
    }
}