using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.IO;
using System.Text;
using System.Xml;

using System.Reflection;
using System.Deployment.Application;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.rms.Client;
using DigitalPlatform.IO;
using DigitalPlatform.Range;
using DigitalPlatform.Marc;
using DigitalPlatform.Library;
using DigitalPlatform.Script;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Text;

namespace dp2Batch
{

	/// <summary>
	/// Summary description for MainForm.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
	{
        public string DataDir = "";

		Assembly AssemblyMain = null;
		Assembly AssemblyFilter = null;
		MyFilterDocument MarcFilter = null;
		int		m_nAssemblyVersion = 0;
		Batch batchObj = null;
		int m_nRecordCount = 0;


		ScriptManager scriptManager = new ScriptManager();



		Hashtable m_tableMarcSyntax = new Hashtable();	// ���ݿ�ȫ·����MARC��ʽ�Ķ��ձ�
		string CurMarcSyntax = "";	// ��ǰMARC��ʽ
		bool OutputCrLf = false;	// ISO2709�ļ���¼β���Ƿ����س����з���
        bool AddG01 = false;    // ISO2709�ļ��еļ�¼���Ƿ����-01�ֶΣ�(����ʱ��Ҫȥ��ԭ�еģ��������)
        bool Remove998 = false; // ��� ISO2709 �ļ���ʱ���Ƿ�ɾ�� 998 �ֶ�?

		public CfgCache cfgCache = new CfgCache();


		public event CheckTargetDbEventHandler CheckTargetDb = null;

		public DigitalPlatform.StopManager	stopManager = new DigitalPlatform.StopManager();

		public ServerCollection Servers = null;

		//���������Ϣ
		public ApplicationInfo	AppInfo = new ApplicationInfo("dp2batch.xml");


		//

		RmsChannel channel = null;	// ��ʱʹ�õ�channel����

		public AutoResetEvent eventClose = new AutoResetEvent(false);

		RmsChannelCollection	Channels = new RmsChannelCollection();	// ӵ��

		DigitalPlatform.Stop stop = null;

		string strLastOutputFileName = "";
		int nLastOutputFilterIndex = 1;

		// double ProgressRatio = 1.0;

		bool bNotAskTimestampMismatchWhenOverwrite = false;	// ��ת�����ݵ�ʱ��,�������ʱ�����ƥ��,�Ƿ�ѯ�ʾ�ǿ�и���

		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem_exit;
		private System.Windows.Forms.TabControl tabControl_main;
		private System.Windows.Forms.TabPage tabPage_range;
		private System.Windows.Forms.Panel panel_range;
		private System.Windows.Forms.Panel panel_resdirtree;
		private System.Windows.Forms.Splitter splitter_range;
		private System.Windows.Forms.Panel panel_rangeParams;
		private System.Windows.Forms.CheckBox checkBox_verifyNumber;
		public System.Windows.Forms.TextBox textBox_dbPath;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox groupBox1;
		public System.Windows.Forms.TextBox textBox_endNo;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.TextBox textBox_startNo;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.RadioButton radioButton_startEnd;
		private System.Windows.Forms.RadioButton radioButton_all;
		private System.Windows.Forms.CheckBox checkBox_forceLoop;
		private System.Windows.Forms.TabPage tabPage_resultset;
		private System.Windows.Forms.MenuItem menuItem_file;
		private System.Windows.Forms.MenuItem menuItem_help;
		private System.Windows.Forms.MenuItem menuItem_copyright;
		private System.Windows.Forms.MenuItem menuItem_cfg;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.ToolBar toolBar_main;
		private System.Windows.Forms.ToolBarButton toolBarButton_stop;
        private System.Windows.Forms.ImageList imageList_toolbar;
		private System.Windows.Forms.ToolBarButton toolBarButton_begin;
		private System.Windows.Forms.TabPage tabPage_import;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button button_import_findFileName;
		private System.Windows.Forms.TextBox textBox_import_fileName;
		private System.Windows.Forms.Label label5;
		private ResTree treeView_rangeRes;
		private System.Windows.Forms.CheckBox checkBox_export_delete;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox textBox_import_range;
		private System.Windows.Forms.MenuItem menuItem_serversCfg;
		private System.Windows.Forms.MenuItem menuItem_projectManage;
		private System.Windows.Forms.MenuItem menuItem_run;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.TextBox textBox_import_dbMap;
		private System.Windows.Forms.Button button_import_dbMap;
        private MenuItem menuItem_openDataFolder;
        private MenuItem menuItem3;
        private MenuItem menuItem_rebuildKeys;
        private MenuItem menuItem_rebuildKeysByDbnames;
        private StatusStrip statusStrip_main;
        private ToolStripStatusLabel toolStripStatusLabel_main;
        private ToolStripProgressBar toolStripProgressBar_main;
        private CheckBox checkBox_export_fastMode;
        private CheckBox checkBox_import_fastMode;
		private System.ComponentModel.IContainer components;

		public MainForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuItem_file = new System.Windows.Forms.MenuItem();
            this.menuItem_run = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItem_projectManage = new System.Windows.Forms.MenuItem();
            this.menuItem_cfg = new System.Windows.Forms.MenuItem();
            this.menuItem_serversCfg = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuItem_rebuildKeys = new System.Windows.Forms.MenuItem();
            this.menuItem_rebuildKeysByDbnames = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem_exit = new System.Windows.Forms.MenuItem();
            this.menuItem_help = new System.Windows.Forms.MenuItem();
            this.menuItem_copyright = new System.Windows.Forms.MenuItem();
            this.menuItem_openDataFolder = new System.Windows.Forms.MenuItem();
            this.tabControl_main = new System.Windows.Forms.TabControl();
            this.tabPage_range = new System.Windows.Forms.TabPage();
            this.panel_range = new System.Windows.Forms.Panel();
            this.panel_resdirtree = new System.Windows.Forms.Panel();
            this.splitter_range = new System.Windows.Forms.Splitter();
            this.panel_rangeParams = new System.Windows.Forms.Panel();
            this.checkBox_export_fastMode = new System.Windows.Forms.CheckBox();
            this.checkBox_export_delete = new System.Windows.Forms.CheckBox();
            this.checkBox_verifyNumber = new System.Windows.Forms.CheckBox();
            this.textBox_dbPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBox_endNo = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_startNo = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.radioButton_startEnd = new System.Windows.Forms.RadioButton();
            this.radioButton_all = new System.Windows.Forms.RadioButton();
            this.checkBox_forceLoop = new System.Windows.Forms.CheckBox();
            this.tabPage_resultset = new System.Windows.Forms.TabPage();
            this.tabPage_import = new System.Windows.Forms.TabPage();
            this.checkBox_import_fastMode = new System.Windows.Forms.CheckBox();
            this.textBox_import_range = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.button_import_findFileName = new System.Windows.Forms.Button();
            this.textBox_import_fileName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.button_import_dbMap = new System.Windows.Forms.Button();
            this.textBox_import_dbMap = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.toolBar_main = new System.Windows.Forms.ToolBar();
            this.toolBarButton_stop = new System.Windows.Forms.ToolBarButton();
            this.toolBarButton_begin = new System.Windows.Forms.ToolBarButton();
            this.imageList_toolbar = new System.Windows.Forms.ImageList(this.components);
            this.statusStrip_main = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel_main = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar_main = new System.Windows.Forms.ToolStripProgressBar();
            this.treeView_rangeRes = new DigitalPlatform.rms.Client.ResTree();
            this.tabControl_main.SuspendLayout();
            this.tabPage_range.SuspendLayout();
            this.panel_range.SuspendLayout();
            this.panel_resdirtree.SuspendLayout();
            this.panel_rangeParams.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage_import.SuspendLayout();
            this.statusStrip_main.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem_file,
            this.menuItem_help});
            // 
            // menuItem_file
            // 
            this.menuItem_file.Index = 0;
            this.menuItem_file.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem_run,
            this.menuItem2,
            this.menuItem_projectManage,
            this.menuItem_cfg,
            this.menuItem_serversCfg,
            this.menuItem3,
            this.menuItem_rebuildKeys,
            this.menuItem_rebuildKeysByDbnames,
            this.menuItem1,
            this.menuItem_exit});
            this.menuItem_file.Text = "�ļ�(&F)";
            // 
            // menuItem_run
            // 
            this.menuItem_run.Index = 0;
            this.menuItem_run.Text = "����(&R)...";
            this.menuItem_run.Click += new System.EventHandler(this.menuItem_run_Click);
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 1;
            this.menuItem2.Text = "-";
            // 
            // menuItem_projectManage
            // 
            this.menuItem_projectManage.Index = 2;
            this.menuItem_projectManage.Text = "��������(&M)...";
            this.menuItem_projectManage.Click += new System.EventHandler(this.menuItem_projectManage_Click);
            // 
            // menuItem_cfg
            // 
            this.menuItem_cfg.Enabled = false;
            this.menuItem_cfg.Index = 3;
            this.menuItem_cfg.Text = "����(&C)...";
            this.menuItem_cfg.Click += new System.EventHandler(this.menuItem_cfg_Click);
            // 
            // menuItem_serversCfg
            // 
            this.menuItem_serversCfg.Index = 4;
            this.menuItem_serversCfg.Text = "ȱʡ�ʻ�����(&A)...";
            this.menuItem_serversCfg.Click += new System.EventHandler(this.menuItem_serversCfg_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 5;
            this.menuItem3.Text = "-";
            // 
            // menuItem_rebuildKeys
            // 
            this.menuItem_rebuildKeys.Index = 6;
            this.menuItem_rebuildKeys.Text = "�ؽ�������(&B)";
            this.menuItem_rebuildKeys.Click += new System.EventHandler(this.menuItem_rebuildKeys_Click);
            // 
            // menuItem_rebuildKeysByDbnames
            // 
            this.menuItem_rebuildKeysByDbnames.Index = 7;
            this.menuItem_rebuildKeysByDbnames.Text = "�ؽ�������[���ݼ������ڵ����ݿ�·��](&R)...";
            this.menuItem_rebuildKeysByDbnames.Click += new System.EventHandler(this.menuItem_rebuildKeysByDbnames_Click);
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 8;
            this.menuItem1.Text = "-";
            // 
            // menuItem_exit
            // 
            this.menuItem_exit.Index = 9;
            this.menuItem_exit.Text = "�˳�(&X)";
            this.menuItem_exit.Click += new System.EventHandler(this.menuItem_exit_Click);
            // 
            // menuItem_help
            // 
            this.menuItem_help.Index = 1;
            this.menuItem_help.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem_copyright,
            this.menuItem_openDataFolder});
            this.menuItem_help.Text = "����(&H)";
            // 
            // menuItem_copyright
            // 
            this.menuItem_copyright.Index = 0;
            this.menuItem_copyright.Text = "��Ȩ(&C)...";
            this.menuItem_copyright.Click += new System.EventHandler(this.menuItem_copyright_Click);
            // 
            // menuItem_openDataFolder
            // 
            this.menuItem_openDataFolder.Index = 1;
            this.menuItem_openDataFolder.Text = "�������ļ���(&D)...";
            this.menuItem_openDataFolder.Click += new System.EventHandler(this.menuItem_openDataFolder_Click);
            // 
            // tabControl_main
            // 
            this.tabControl_main.Controls.Add(this.tabPage_range);
            this.tabControl_main.Controls.Add(this.tabPage_resultset);
            this.tabControl_main.Controls.Add(this.tabPage_import);
            this.tabControl_main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl_main.Location = new System.Drawing.Point(0, 32);
            this.tabControl_main.Name = "tabControl_main";
            this.tabControl_main.SelectedIndex = 0;
            this.tabControl_main.Size = new System.Drawing.Size(634, 273);
            this.tabControl_main.TabIndex = 1;
            this.tabControl_main.SelectedIndexChanged += new System.EventHandler(this.tabControl_main_SelectedIndexChanged);
            // 
            // tabPage_range
            // 
            this.tabPage_range.BackColor = System.Drawing.Color.Transparent;
            this.tabPage_range.Controls.Add(this.panel_range);
            this.tabPage_range.Location = new System.Drawing.Point(4, 23);
            this.tabPage_range.Name = "tabPage_range";
            this.tabPage_range.Padding = new System.Windows.Forms.Padding(6);
            this.tabPage_range.Size = new System.Drawing.Size(626, 246);
            this.tabPage_range.TabIndex = 0;
            this.tabPage_range.Text = "����¼ID����";
            this.tabPage_range.UseVisualStyleBackColor = true;
            // 
            // panel_range
            // 
            this.panel_range.Controls.Add(this.panel_resdirtree);
            this.panel_range.Controls.Add(this.splitter_range);
            this.panel_range.Controls.Add(this.panel_rangeParams);
            this.panel_range.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_range.Location = new System.Drawing.Point(6, 6);
            this.panel_range.Name = "panel_range";
            this.panel_range.Size = new System.Drawing.Size(614, 234);
            this.panel_range.TabIndex = 8;
            // 
            // panel_resdirtree
            // 
            this.panel_resdirtree.Controls.Add(this.treeView_rangeRes);
            this.panel_resdirtree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_resdirtree.Location = new System.Drawing.Point(317, 0);
            this.panel_resdirtree.Name = "panel_resdirtree";
            this.panel_resdirtree.Padding = new System.Windows.Forms.Padding(0, 4, 4, 4);
            this.panel_resdirtree.Size = new System.Drawing.Size(297, 234);
            this.panel_resdirtree.TabIndex = 6;
            // 
            // splitter_range
            // 
            this.splitter_range.Location = new System.Drawing.Point(309, 0);
            this.splitter_range.Name = "splitter_range";
            this.splitter_range.Size = new System.Drawing.Size(8, 234);
            this.splitter_range.TabIndex = 8;
            this.splitter_range.TabStop = false;
            // 
            // panel_rangeParams
            // 
            this.panel_rangeParams.AutoScroll = true;
            this.panel_rangeParams.Controls.Add(this.checkBox_export_fastMode);
            this.panel_rangeParams.Controls.Add(this.checkBox_export_delete);
            this.panel_rangeParams.Controls.Add(this.checkBox_verifyNumber);
            this.panel_rangeParams.Controls.Add(this.textBox_dbPath);
            this.panel_rangeParams.Controls.Add(this.label1);
            this.panel_rangeParams.Controls.Add(this.groupBox1);
            this.panel_rangeParams.Controls.Add(this.checkBox_forceLoop);
            this.panel_rangeParams.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel_rangeParams.Location = new System.Drawing.Point(0, 0);
            this.panel_rangeParams.Name = "panel_rangeParams";
            this.panel_rangeParams.Size = new System.Drawing.Size(309, 234);
            this.panel_rangeParams.TabIndex = 7;
            // 
            // checkBox_export_fastMode
            // 
            this.checkBox_export_fastMode.AutoSize = true;
            this.checkBox_export_fastMode.Location = new System.Drawing.Point(144, 198);
            this.checkBox_export_fastMode.Name = "checkBox_export_fastMode";
            this.checkBox_export_fastMode.Size = new System.Drawing.Size(90, 18);
            this.checkBox_export_fastMode.TabIndex = 7;
            this.checkBox_export_fastMode.Text = "����ģʽ(&F)";
            this.checkBox_export_fastMode.UseVisualStyleBackColor = true;
            // 
            // checkBox_export_delete
            // 
            this.checkBox_export_delete.Location = new System.Drawing.Point(9, 195);
            this.checkBox_export_delete.Name = "checkBox_export_delete";
            this.checkBox_export_delete.Size = new System.Drawing.Size(117, 24);
            this.checkBox_export_delete.TabIndex = 6;
            this.checkBox_export_delete.Text = "ɾ����¼(&D)";
            this.checkBox_export_delete.CheckedChanged += new System.EventHandler(this.checkBox_export_delete_CheckedChanged);
            // 
            // checkBox_verifyNumber
            // 
            this.checkBox_verifyNumber.Location = new System.Drawing.Point(9, 171);
            this.checkBox_verifyNumber.Name = "checkBox_verifyNumber";
            this.checkBox_verifyNumber.Size = new System.Drawing.Size(124, 18);
            this.checkBox_verifyNumber.TabIndex = 4;
            this.checkBox_verifyNumber.Text = "У׼��βID(&V)";
            // 
            // textBox_dbPath
            // 
            this.textBox_dbPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_dbPath.Location = new System.Drawing.Point(75, 6);
            this.textBox_dbPath.Name = "textBox_dbPath";
            this.textBox_dbPath.ReadOnly = true;
            this.textBox_dbPath.Size = new System.Drawing.Size(186, 22);
            this.textBox_dbPath.TabIndex = 2;
            this.textBox_dbPath.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(7, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 18);
            this.label1.TabIndex = 1;
            this.label1.Text = "���ݿ�:";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.textBox_endNo);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.textBox_startNo);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.radioButton_startEnd);
            this.groupBox1.Controls.Add(this.radioButton_all);
            this.groupBox1.Location = new System.Drawing.Point(7, 32);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(253, 134);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " �����¼��Χ ";
            // 
            // textBox_endNo
            // 
            this.textBox_endNo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_endNo.Location = new System.Drawing.Point(165, 95);
            this.textBox_endNo.Name = "textBox_endNo";
            this.textBox_endNo.Size = new System.Drawing.Size(74, 22);
            this.textBox_endNo.TabIndex = 5;
            this.textBox_endNo.TextChanged += new System.EventHandler(this.textBox_endNo_TextChanged);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(69, 97);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 19);
            this.label3.TabIndex = 4;
            this.label3.Text = "������¼ID:";
            // 
            // textBox_startNo
            // 
            this.textBox_startNo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_startNo.Location = new System.Drawing.Point(165, 63);
            this.textBox_startNo.Name = "textBox_startNo";
            this.textBox_startNo.Size = new System.Drawing.Size(74, 22);
            this.textBox_startNo.TabIndex = 3;
            this.textBox_startNo.TextChanged += new System.EventHandler(this.textBox_startNo_TextChanged);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(69, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 18);
            this.label2.TabIndex = 2;
            this.label2.Text = "��ʼ��¼ID:";
            // 
            // radioButton_startEnd
            // 
            this.radioButton_startEnd.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButton_startEnd.Checked = true;
            this.radioButton_startEnd.Location = new System.Drawing.Point(21, 38);
            this.radioButton_startEnd.Name = "radioButton_startEnd";
            this.radioButton_startEnd.Size = new System.Drawing.Size(143, 19);
            this.radioButton_startEnd.TabIndex = 1;
            this.radioButton_startEnd.TabStop = true;
            this.radioButton_startEnd.Text = "��ֹID(&S) ";
            // 
            // radioButton_all
            // 
            this.radioButton_all.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButton_all.Location = new System.Drawing.Point(21, 19);
            this.radioButton_all.Name = "radioButton_all";
            this.radioButton_all.Size = new System.Drawing.Size(56, 19);
            this.radioButton_all.TabIndex = 0;
            this.radioButton_all.Text = "ȫ��(&A)";
            this.radioButton_all.CheckedChanged += new System.EventHandler(this.radioButton_all_CheckedChanged);
            // 
            // checkBox_forceLoop
            // 
            this.checkBox_forceLoop.Location = new System.Drawing.Point(144, 171);
            this.checkBox_forceLoop.Name = "checkBox_forceLoop";
            this.checkBox_forceLoop.Size = new System.Drawing.Size(158, 18);
            this.checkBox_forceLoop.TabIndex = 5;
            this.checkBox_forceLoop.Text = "δ����ʱ����ѭ��(&C)";
            // 
            // tabPage_resultset
            // 
            this.tabPage_resultset.BackColor = System.Drawing.Color.Transparent;
            this.tabPage_resultset.Location = new System.Drawing.Point(4, 23);
            this.tabPage_resultset.Name = "tabPage_resultset";
            this.tabPage_resultset.Size = new System.Drawing.Size(626, 98);
            this.tabPage_resultset.TabIndex = 1;
            this.tabPage_resultset.Text = "�����������";
            this.tabPage_resultset.UseVisualStyleBackColor = true;
            this.tabPage_resultset.Visible = false;
            // 
            // tabPage_import
            // 
            this.tabPage_import.AutoScroll = true;
            this.tabPage_import.BackColor = System.Drawing.Color.Transparent;
            this.tabPage_import.Controls.Add(this.checkBox_import_fastMode);
            this.tabPage_import.Controls.Add(this.textBox_import_range);
            this.tabPage_import.Controls.Add(this.label6);
            this.tabPage_import.Controls.Add(this.button_import_findFileName);
            this.tabPage_import.Controls.Add(this.textBox_import_fileName);
            this.tabPage_import.Controls.Add(this.label5);
            this.tabPage_import.Controls.Add(this.button_import_dbMap);
            this.tabPage_import.Controls.Add(this.textBox_import_dbMap);
            this.tabPage_import.Controls.Add(this.label4);
            this.tabPage_import.Location = new System.Drawing.Point(4, 23);
            this.tabPage_import.Name = "tabPage_import";
            this.tabPage_import.Size = new System.Drawing.Size(626, 98);
            this.tabPage_import.TabIndex = 2;
            this.tabPage_import.Text = "����";
            this.tabPage_import.UseVisualStyleBackColor = true;
            // 
            // checkBox_import_fastMode
            // 
            this.checkBox_import_fastMode.AutoSize = true;
            this.checkBox_import_fastMode.Location = new System.Drawing.Point(119, 61);
            this.checkBox_import_fastMode.Name = "checkBox_import_fastMode";
            this.checkBox_import_fastMode.Size = new System.Drawing.Size(90, 18);
            this.checkBox_import_fastMode.TabIndex = 8;
            this.checkBox_import_fastMode.Text = "����ģʽ(&F)";
            this.checkBox_import_fastMode.UseVisualStyleBackColor = true;
            // 
            // textBox_import_range
            // 
            this.textBox_import_range.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_import_range.Location = new System.Drawing.Point(119, 33);
            this.textBox_import_range.Name = "textBox_import_range";
            this.textBox_import_range.Size = new System.Drawing.Size(379, 22);
            this.textBox_import_range.TabIndex = 7;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(9, 36);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(76, 14);
            this.label6.TabIndex = 6;
            this.label6.Text = "���뷶Χ(&R):";
            // 
            // button_import_findFileName
            // 
            this.button_import_findFileName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_import_findFileName.Location = new System.Drawing.Point(504, 8);
            this.button_import_findFileName.Name = "button_import_findFileName";
            this.button_import_findFileName.Size = new System.Drawing.Size(46, 22);
            this.button_import_findFileName.TabIndex = 5;
            this.button_import_findFileName.Text = "...";
            this.button_import_findFileName.Click += new System.EventHandler(this.button_import_findFileName_Click);
            // 
            // textBox_import_fileName
            // 
            this.textBox_import_fileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_import_fileName.Location = new System.Drawing.Point(119, 8);
            this.textBox_import_fileName.Name = "textBox_import_fileName";
            this.textBox_import_fileName.Size = new System.Drawing.Size(379, 22);
            this.textBox_import_fileName.TabIndex = 4;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 10);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(63, 14);
            this.label5.TabIndex = 3;
            this.label5.Text = "�ļ���(&F):";
            // 
            // button_import_dbMap
            // 
            this.button_import_dbMap.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_import_dbMap.Location = new System.Drawing.Point(504, 102);
            this.button_import_dbMap.Name = "button_import_dbMap";
            this.button_import_dbMap.Size = new System.Drawing.Size(46, 23);
            this.button_import_dbMap.TabIndex = 2;
            this.button_import_dbMap.Text = "...";
            this.button_import_dbMap.Click += new System.EventHandler(this.button_import_dbMap_Click);
            // 
            // textBox_import_dbMap
            // 
            this.textBox_import_dbMap.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_import_dbMap.Location = new System.Drawing.Point(12, 104);
            this.textBox_import_dbMap.Multiline = true;
            this.textBox_import_dbMap.Name = "textBox_import_dbMap";
            this.textBox_import_dbMap.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_import_dbMap.Size = new System.Drawing.Size(489, 110);
            this.textBox_import_dbMap.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 87);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(101, 14);
            this.label4.TabIndex = 0;
            this.label4.Text = "����ӳ�����(&T):";
            // 
            // toolBar_main
            // 
            this.toolBar_main.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
            this.toolBar_main.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
            this.toolBarButton_stop,
            this.toolBarButton_begin});
            this.toolBar_main.Divider = false;
            this.toolBar_main.DropDownArrows = true;
            this.toolBar_main.ImageList = this.imageList_toolbar;
            this.toolBar_main.Location = new System.Drawing.Point(0, 0);
            this.toolBar_main.Name = "toolBar_main";
            this.toolBar_main.ShowToolTips = true;
            this.toolBar_main.Size = new System.Drawing.Size(634, 32);
            this.toolBar_main.TabIndex = 2;
            this.toolBar_main.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.toolBar1_ButtonClick);
            // 
            // toolBarButton_stop
            // 
            this.toolBarButton_stop.Enabled = false;
            this.toolBarButton_stop.ImageIndex = 0;
            this.toolBarButton_stop.Name = "toolBarButton_stop";
            this.toolBarButton_stop.ToolTipText = "ֹͣ";
            // 
            // toolBarButton_begin
            // 
            this.toolBarButton_begin.ImageIndex = 1;
            this.toolBarButton_begin.Name = "toolBarButton_begin";
            this.toolBarButton_begin.ToolTipText = "��ʼ";
            // 
            // imageList_toolbar
            // 
            this.imageList_toolbar.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList_toolbar.ImageStream")));
            this.imageList_toolbar.TransparentColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(193)))));
            this.imageList_toolbar.Images.SetKeyName(0, "");
            this.imageList_toolbar.Images.SetKeyName(1, "");
            // 
            // statusStrip_main
            // 
            this.statusStrip_main.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel_main,
            this.toolStripProgressBar_main});
            this.statusStrip_main.Location = new System.Drawing.Point(0, 305);
            this.statusStrip_main.Name = "statusStrip_main";
            this.statusStrip_main.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.statusStrip_main.Size = new System.Drawing.Size(634, 22);
            this.statusStrip_main.TabIndex = 4;
            this.statusStrip_main.Text = "statusStrip1";
            // 
            // toolStripStatusLabel_main
            // 
            this.toolStripStatusLabel_main.Name = "toolStripStatusLabel_main";
            this.toolStripStatusLabel_main.Size = new System.Drawing.Size(445, 17);
            this.toolStripStatusLabel_main.Spring = true;
            // 
            // toolStripProgressBar_main
            // 
            this.toolStripProgressBar_main.Name = "toolStripProgressBar_main";
            this.toolStripProgressBar_main.Size = new System.Drawing.Size(172, 16);
            // 
            // treeView_rangeRes
            // 
            this.treeView_rangeRes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView_rangeRes.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeView_rangeRes.HideSelection = false;
            this.treeView_rangeRes.ImageIndex = 0;
            this.treeView_rangeRes.Location = new System.Drawing.Point(0, 4);
            this.treeView_rangeRes.Name = "treeView_rangeRes";
            this.treeView_rangeRes.SelectedImageIndex = 0;
            this.treeView_rangeRes.Size = new System.Drawing.Size(293, 226);
            this.treeView_rangeRes.TabIndex = 0;
            this.treeView_rangeRes.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeView_rangeRes_AfterCheck);
            this.treeView_rangeRes.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_rangeRes_AfterSelect);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(634, 327);
            this.Controls.Add(this.tabControl_main);
            this.Controls.Add(this.statusStrip_main);
            this.Controls.Add(this.toolBar_main);
            this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Menu = this.mainMenu1;
            this.Name = "MainForm";
            this.Text = "dp2batch V2 -- ������";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
            this.Closed += new System.EventHandler(this.MainForm_Closed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.tabControl_main.ResumeLayout(false);
            this.tabPage_range.ResumeLayout(false);
            this.panel_range.ResumeLayout(false);
            this.panel_resdirtree.ResumeLayout(false);
            this.panel_rangeParams.ResumeLayout(false);
            this.panel_rangeParams.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage_import.ResumeLayout(false);
            this.tabPage_import.PerformLayout();
            this.statusStrip_main.ResumeLayout(false);
            this.statusStrip_main.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
		}

		private void MainForm_Load(object sender, System.EventArgs e)
		{
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



			// ���ļ���װ�ش���һ��ServerCollection����
			// parameters:
			//		bIgnorFileNotFound	�Ƿ��׳�FileNotFoundException�쳣��
			//							���==true������ֱ�ӷ���һ���µĿ�ServerCollection����
			// Exception:
			//			FileNotFoundException	�ļ�û�ҵ�
			//			SerializationException	�汾Ǩ��ʱ���׳���
			try 
			{
                Servers = ServerCollection.Load(this.DataDir
					+ "\\dp2batch_servers.bin",
					true);
				Servers.ownerForm = this;
			}
			catch (SerializationException ex)
			{
				MessageBox.Show(this, ex.Message);
				Servers = new ServerCollection();
				// �����ļ������Ա㱾�����н���ʱ���Ǿ��ļ�
                Servers.FileName = this.DataDir
					+ "\\dp2batch_servers.bin";

			}

            this.Servers.ServerChanged += new ServerChangedEventHandle(Servers_ServerChanged);


			string strError = "";
            int nRet = cfgCache.Load(this.DataDir
				+ "\\cfgcache.xml",
				out strError);
			if (nRet == -1) 
			{
				MessageBox.Show(this, strError);
			}
            cfgCache.TempDir = this.DataDir
				+ "\\cfgcache";
			cfgCache.InstantSave = true;

		
			// ���ô��ڳߴ�״̬
			if (AppInfo != null) 
			{
                /*
                // �״����У��������á�΢���źڡ�����
                if (this.IsFirstRun == true)
                {
                    SetFirstDefaultFont();
                }
                 * */

                SetFirstDefaultFont();

                MainForm.SetControlFont(this, this.DefaultFont);

				AppInfo.LoadFormStates(this,
					"mainformstate");
			}


			stopManager.Initial(this.toolBarButton_stop,
                this.toolStripStatusLabel_main,
                this.toolStripProgressBar_main);
			stopManager.LinkReverseButton(this.toolBarButton_begin);

			// ////////////////

			stop = new DigitalPlatform.Stop();
			stop.Register(this.stopManager, true);	// ����������

            this.Channels.AskAccountInfo +=new AskAccountInfoEventHandle(this.Servers.OnAskAccountInfo);
            /*
			this.Channels.procAskAccountInfo = 
				new Delegate_AskAccountInfo(this.Servers.AskAccountInfo);
             */

			// �򵥼�������׼������
			treeView_rangeRes.stopManager = this.stopManager;

			treeView_rangeRes.Servers = this.Servers;	// ����

			treeView_rangeRes.Channels = this.Channels;	// ����
            treeView_rangeRes.AppInfo = this.AppInfo;   // 2013/2/15
			treeView_rangeRes.Fill(null);

			this.textBox_import_fileName.Text = 
				AppInfo.GetString(
				"page_import",
				"source_file_name",
				"");

			this.textBox_import_range.Text = 
				AppInfo.GetString(
				"page_import",
				"range",
				"");

			this.textBox_import_dbMap.Text = 
				AppInfo.GetString(
				"page_import",
				"dbmap",
				"").Replace(";","\r\n");
            this.checkBox_import_fastMode.Checked = AppInfo.GetBoolean(
                "page_import",
                "fastmode",
                true);

			textBox_startNo.Text = 
				AppInfo.GetString(
				"rangePage",
				"startNumber",
				"");

			textBox_endNo.Text = 
				AppInfo.GetString(
				"rangePage",
				"endNumber",
				"");

			checkBox_verifyNumber.Checked = 
				Convert.ToBoolean(
				AppInfo.GetInt(
				"rangePage",
				"verifyrange",
				0)
				);

			checkBox_forceLoop.Checked = 
				Convert.ToBoolean(
				AppInfo.GetInt(
				"rangePage",
				"forceloop",
				0)
				);

			
			checkBox_export_delete.Checked = 
				Convert.ToBoolean(
				AppInfo.GetInt(
				"rangePage",
				"delete",
				0)
				);

            this.checkBox_export_fastMode.Checked = AppInfo.GetBoolean(
                "rangePage",
                "fastmode",
                true);

			this.radioButton_all.Checked = 
				Convert.ToBoolean(
				AppInfo.GetInt(
				"rangePage",
				"all",
				0)
				);

			strLastOutputFileName = 
				AppInfo.GetString(
				"rangePage",
				"lastoutputfilename",
				"");

			nLastOutputFilterIndex = 
				AppInfo.GetInt(
				"rangePage",
				"lastoutputfilterindex",
				1);

			scriptManager.applicationInfo = AppInfo;
			scriptManager.CfgFilePath =
                this.DataDir + "\\projects.xml";
            scriptManager.DataDir = this.DataDir;

			scriptManager.CreateDefaultContent -=new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);
			scriptManager.CreateDefaultContent +=new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);

			// �����ϴα����·��չ��resdircontrol��
			string strResDirPath = AppInfo.GetString(
				"rangePage",
				"resdirpath",
				"");
			if (strResDirPath != null)
			{
				object[] pList = { strResDirPath };

				this.BeginInvoke(new Delegate_ExpandResDir(ExpandResDir),
					pList);
			}

            checkBox_export_delete_CheckedChanged(null, null);
		}

        void Servers_ServerChanged(object sender, ServerChangedEventArgs e)
        {
            this.treeView_rangeRes.Refresh(ResTree.RefreshStyle.All);
        }

		public delegate void Delegate_ExpandResDir(string strResDirPath);

		void ExpandResDir(string strResDirPath)
		{
			this.toolStripStatusLabel_main.Text = "����չ����ԴĿ¼ " + strResDirPath + ", ���Ժ�...";
			this.Update();

			ResPath respath = new ResPath(strResDirPath);

			EnableControls(false);

			// չ����ָ���Ľڵ�
			treeView_rangeRes.ExpandPath(respath);

			EnableControls(true);

			/*
			//Cursor.Current = Cursors.WaitCursor;
			dtlpResDirControl.ExpandPath(strResDirPath);
			//Cursor.Current = Cursors.Default;
			*/
            toolStripStatusLabel_main.Text = "";

		}

		private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (stop != null) 
			{
				if (stop.State == 0 || stop.State == 1) 
				{
					this.channel.Abort();
					e.Cancel = true;
				}
			}
		}

		private void MainForm_Closed(object sender, System.EventArgs e)
		{

            this.Channels.AskAccountInfo -= new AskAccountInfoEventHandle(this.Servers.OnAskAccountInfo);

            this.Servers.ServerChanged -= new ServerChangedEventHandle(Servers_ServerChanged);

			// ���浽�ļ�
			// parameters:
			//		strFileName	�ļ��������==null,��ʾʹ��װ��ʱ������Ǹ��ļ���
			Servers.Save(null);
			Servers = null;

			string strError;
			int nRet = cfgCache.Save(null, out strError);
			if (nRet == -1)
				MessageBox.Show(this, strError);


			// ���洰�ڳߴ�״̬
			if (AppInfo != null) 
			{
				AppInfo.SaveFormStates(this,
					"mainformstate");
			}

			AppInfo.SetString(
				"page_import",
				"source_file_name",
				this.textBox_import_fileName.Text);
			AppInfo.SetString(
				"page_import",
				"dbmap",
				this.textBox_import_dbMap.Text.Replace("\r\n",";"));
			AppInfo.SetString(
				"page_import",
				"range",
				this.textBox_import_range.Text);
            AppInfo.SetBoolean(
"page_import",
"fastmode",
this.checkBox_import_fastMode.Checked);


			AppInfo.SetString(
				"rangePage",
				"startNumber",
				textBox_startNo.Text);


			AppInfo.SetString(
				"rangePage",
				"endNumber",
				textBox_endNo.Text);


			AppInfo.SetInt(
				"rangePage",
				"verifyrange",
				Convert.ToInt32(checkBox_verifyNumber.Checked));

			AppInfo.SetInt(
				"rangePage",
				"forceloop",
				Convert.ToInt32(checkBox_forceLoop.Checked));

			AppInfo.SetInt(
				"rangePage",
				"delete",
				Convert.ToInt32(checkBox_export_delete.Checked));
            AppInfo.SetBoolean(
    "rangePage",
    "fastmode",
    this.checkBox_export_fastMode.Checked);

			AppInfo.SetInt(
				"rangePage",
				"all",
				Convert.ToInt32(this.radioButton_all.Checked));

			AppInfo.SetString(
				"rangePage",
				"lastoutputfilename",
				strLastOutputFileName);

			AppInfo.SetInt(
				"rangePage",
				"lastoutputfilterindex",
				nLastOutputFilterIndex);

			// ����resdircontrol����ѡ��

			ResPath respath = new ResPath(treeView_rangeRes.SelectedNode);
			AppInfo.SetString(
				"rangePage",
				"resdirpath",
				respath.FullPath);


			//��סsave,������ϢXML�ļ�
			AppInfo.Save();
			AppInfo = null;	// ������������������		

		}


		#region �˵�����

		private void menuItem_cfg_Click(object sender, System.EventArgs e)
		{
		
		}

		private void menuItem_exit_Click(object sender, System.EventArgs e)
		{
            this.Close();
		}

		#endregion

		private void toolBar1_ButtonClick(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e)
		{
			string strError = "";

			if (e.Button == toolBarButton_stop) 
			{
				stopManager.DoStopActive();
			}

			if (e.Button == toolBarButton_begin) 
			{
				// ���ֶԻ���ѯ��Project����
				GetProjectNameDlg dlg = new GetProjectNameDlg();
                MainForm.SetControlFont(dlg, this.DefaultFont);

				dlg.scriptManager = this.scriptManager;
				dlg.ProjectName = AppInfo.GetString(
					"main",
					"lastUsedProject",
					"");
				dlg.NoneProject = Convert.ToBoolean(AppInfo.GetInt(
					"main",
					"lastNoneProjectState",
					0));

				this.AppInfo.LinkFormState(dlg, "GetProjectNameDlg_state");
				dlg.ShowDialog(this);
				this.AppInfo.UnlinkFormState(dlg);


				if (dlg.DialogResult != DialogResult.OK)
					return;

				string strProjectName = "";
				string strLocate = "";	// �����ļ�Ŀ¼


				if (dlg.NoneProject == false)
				{
					// string strWarning = "";

					strProjectName = dlg.ProjectName;

					// ��÷�������
					// strProjectNamePath	������������·��
					// return:
					//		-1	error
					//		0	not found project
					//		1	found
					int nRet = scriptManager.GetProjectData(
						strProjectName,
						out strLocate);

					if (nRet == 0) 
					{
						strError = "���� " + strProjectName + " û���ҵ�...";
						goto ERROR1;
					}
					if (nRet == -1) 
					{
						strError = "scriptManager.GetProjectData() error ...";
						goto ERROR1;
					}
				}

				AppInfo.SetString(
					"main",
					"lastUsedProject",
					strProjectName);
				AppInfo.SetInt(
					"main",
					"lastNoneProjectState",
					Convert.ToInt32(dlg.NoneProject));



				if (tabControl_main.SelectedTab == this.tabPage_range)
				{
					this.DoExport(strProjectName, strLocate);
				}
				else if (tabControl_main.SelectedTab == this.tabPage_import)
				{
					this.DoImport(strProjectName, strLocate);
				}
				
			}

			return;

			ERROR1:
				MessageBox.Show(this, strError);
		}


        void DoImport(string strProjectName,
            string strProjectLocate)
        {
            string strError = "";
            int nRet = 0;

            Assembly assemblyMain = null;
            MyFilterDocument filter = null;
            this.MarcFilter = null;
            batchObj = null;
            m_nRecordCount = -1;

            // ׼���ű�
            if (strProjectName != "" && strProjectName != null)
            {
                nRet = PrepareScript(strProjectName,
                    strProjectLocate,
                    out assemblyMain,
                    out filter,
                    out batchObj,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.AssemblyMain = assemblyMain;
                if (filter != null)
                    this.AssemblyFilter = filter.Assembly;
                else
                    this.AssemblyFilter = null;

                this.MarcFilter = filter;
            }

            // ִ�нű���OnInitial()

            // ����Script��OnInitial()����
            // OnInitial()��OnBegin�ı�������, ����OnInitial()�ʺϼ�������������
            if (batchObj != null)
            {
                BatchEventArgs args = new BatchEventArgs();
                batchObj.OnInitial(this, args);
                /*
                if (args.Continue == ContinueType.SkipBeginMiddle)
                    goto END1;
                if (args.Continue == ContinueType.SkipMiddle) 
                {
                    strError = "OnInitial()��args.Continue����ʹ��ContinueType.SkipMiddle.Ӧʹ��ContinueType.SkipBeginMiddle";
                    goto ERROR1;
                }
                */
                if (args.Continue == ContinueType.SkipAll)
                    goto END1;
            }


            if (this.textBox_import_fileName.Text == "")
            {
                strError = "��δָ�������ļ���...";
                goto ERROR1;
            }
            FileInfo fi = new FileInfo(this.textBox_import_fileName.Text);
            if (fi.Exists == false)
            {
                strError = "�ļ�" + this.textBox_import_fileName.Text + "������...";
                goto ERROR1;
            }

            OpenMarcFileDlg dlg = null;

            // ISO2709�ļ���ҪԤ��׼������
            if (String.Compare(fi.Extension, ".iso", true) == 0
                || String.Compare(fi.Extension, ".mrc", true) == 0)
            {
                // ѯ��encoding��marcsyntax
                dlg = new OpenMarcFileDlg();
                MainForm.SetControlFont(dlg, this.DefaultFont);

                dlg.Text = "��ָ��Ҫ����� ISO2709 �ļ�����";
                dlg.FileName = this.textBox_import_fileName.Text;

                this.AppInfo.LinkFormState(dlg, "OpenMarcFileDlg_input_state");
                dlg.ShowDialog(this);
                this.AppInfo.UnlinkFormState(dlg);

                if (dlg.DialogResult != DialogResult.OK)
                    return;

                this.textBox_import_fileName.Text = dlg.FileName;
                this.CurMarcSyntax = dlg.MarcSyntax;
            }

            // ����Script��OnBegin()����
            // OnBegin()����Ȼ���޸�MainForm��������
            if (batchObj != null)
            {
                BatchEventArgs args = new BatchEventArgs();
                batchObj.OnBegin(this, args);
                /*
                if (args.Continue == ContinueType.SkipMiddle)
                    goto END1;
                if (args.Continue == ContinueType.SkipBeginMiddle)
                    goto END1;
                */
                if (args.Continue == ContinueType.SkipAll)
                    goto END1;
            }

            if (String.Compare(fi.Extension, ".dp2bak", true) == 0)
                nRet = this.DoImportBackup(this.textBox_import_fileName.Text,
                    out strError);

            else if (String.Compare(fi.Extension, ".xml", true) == 0)
                nRet = this.DoImportXml(this.textBox_import_fileName.Text,
                    out strError);

            else if (String.Compare(fi.Extension, ".iso", true) == 0
                || String.Compare(fi.Extension, ".mrc", true) == 0)
            {

                this.CheckTargetDb += new CheckTargetDbEventHandler(CheckTargetDbCallBack);

                try
                {
                    nRet = this.DoImportIso2709(dlg.FileName,
                        dlg.MarcSyntax,
                        dlg.Encoding,
                        out strError);
                }
                finally
                {
                    this.CheckTargetDb -= new CheckTargetDbEventHandler(CheckTargetDbCallBack);
                }

            }
            else
            {
                strError = "δ֪���ļ�����...";
                goto ERROR1;
            }


        END1:
            // ����Script��OnEnd()����
            if (batchObj != null)
            {
                BatchEventArgs args = new BatchEventArgs();
                batchObj.OnEnd(this, args);
            }

            // END2:

            this.AssemblyMain = null;
            this.AssemblyFilter = null;
            if (filter != null)
                filter.Assembly = null;


            if (strError != "")
                MessageBox.Show(this, strError);

            this.MarcFilter = null;
            return;

        ERROR1:
            this.AssemblyMain = null;
            this.AssemblyFilter = null;
            if (filter != null)
                filter.Assembly = null;

            this.MarcFilter = null;

            MessageBox.Show(this, strError);
        }


		// ����XML����
		// parameter: 
		//		strFileName: Ҫ�����ԴXML�ļ�
		// ˵��: ����������һ�������Ĺ���,
		//		ֻҪ����������Ȼ˳����������ÿ����¼�Ϳ����ˡ�
		int DoImportXml(string strFileName,
			out string strError)
		{
			int nRet;
			strError = "";

            bool bFastMode = this.checkBox_import_fastMode.Checked;

			this.bNotAskTimestampMismatchWhenOverwrite = false;	// Ҫѯ��

			// ׼���������ձ�
			DbNameMap map = DbNameMap.Build(this.textBox_import_dbMap.Text.Replace("\r\n", ";"),
                out strError);
            if (map == null)
                return -1;


			Stream file = File.Open(strFileName,
				FileMode.Open,
				FileAccess.Read);

			XmlTextReader reader = new XmlTextReader(file);

			//
			RangeList rl = null;
			long lMax = 0;
			long lMin = 0;
			long lSkipCount = 0;
			int nReadRet = 0;
			string strCount = "";

			//��Χ
			if (textBox_import_range.Text != "") 
			{
				rl = new RangeList(textBox_import_range.Text);
				rl.Sort();
				rl.Merge();
				lMin = rl.min();
				lMax = rl.max();
			}

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ���");
            stop.BeginLoop();

            stop.SetProgressRange(0, file.Length);

            EnableControls(false);

            WriteLog("��ʼ����XML����");

			try
			{
				bool bRet = false;
			
                // �ƶ�����Ԫ��
				while(true) 
				{
					bRet = reader.Read();
					if (bRet == false) 
					{
						strError = "û�и�Ԫ��";
						goto ERROR1;
					}
					if (reader.NodeType == XmlNodeType.Element)
						break;
				}

                // �ƶ������¼���һ��element
                while (true)
                {
                    bRet = reader.Read();
                    if (bRet == false)
                    {
                        strError = "û�е�һ����¼Ԫ��";
                        goto END1;
                    }
                    if (reader.NodeType == XmlNodeType.Element)
                        break;
                }

				this.m_nRecordCount = 0;

				for(long lCount = 0;;lCount ++)
				{
					bool bSkip = false;
					nReadRet = 0;


					Application.DoEvents();	// ���ý������Ȩ

					if (stop.State != 0)
					{
						DialogResult result = MessageBox.Show(this,
							"ȷʵҪ�жϵ�ǰ���������?",
							"dp2batch",
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Question,
							MessageBoxDefaultButton.Button2);
						if (result == DialogResult.Yes)
						{
							strError = "�û��ж�";
							nReadRet = 100;
							goto ERROR1;
						}
						else 
						{
							stop.Continue();
						}
					}


					//������ǰ��¼�Ƿ��ڴ���Χ��
					if (rl != null) 
					{
						if (lMax != -1) // -1:��ȷ��
						{
							if (lCount > lMax)
								nReadRet = 2;	// ���濴�����״̬����break��Ϊʲô��������break������Ϊ�˺�����ʾlabel��Ϣ
						}
						if (rl.IsInRange(lCount, true) == false) 
						{
							bSkip = true;
						}
					}


					// progressBar_main.Value = (int)((file.Position)/ProgressRatio);
                    stop.SetProgressValue(file.Position);

					// ��ʾ��Ϣ
					if (bSkip == true) 
					{
						stop.SetMessage( ((bSkip == true) ? "�������� " : "���ڴ���" )
							+ Convert.ToString(lCount+1) );
					}

					/*
					if (nReadRet == 2)
						goto CONTINUE;

					if (bSkip == true)
						goto CONTINUE;
					*/


					/*
					// ��ֹһ����¼Ҳû�е����,���԰������д��ǰ��
					if (file.Position >= file.Length)
						break;
					*/

					// ����һ��Item
					nRet = DoXmlItemUpload(
                        bFastMode,
                        reader,
						map,
						bSkip == true || nReadRet == 2,
						strCount,
						out strError);
					if (nRet == -1)
						goto ERROR1;
					if (nRet == 1)
						break;

					strCount = "������ "
						+ Convert.ToString(lCount - lSkipCount)
						+ "��/ ������ " 
						+ Convert.ToString(lSkipCount);


					if (bSkip)
						lSkipCount ++;

					if (nReadRet == 1 || nReadRet == 2)  //�жϴ��ļ�����
						break;

				}

    		}
			finally
			{
				file.Close();

                WriteLog("��������XML����");
			}

        END1:

            stop.EndLoop();
            stop.OnStop -= new StopEventHandler(this.DoStop);
            stop.Initial("");

            EnableControls(true);

            strError = "�ָ������ļ� '" + strFileName + "' ��ɡ�";
            return 0;
        ERROR1:
            stop.EndLoop();
            stop.OnStop -= new StopEventHandler(this.DoStop);
            stop.Initial("");
            EnableControls(true);

            if (nReadRet == 100)
                strError = "�ָ������ļ� '" + strFileName + "' ���жϡ�ԭ�� " + strError;
            else
                strError = "�ָ������ļ� '" + strFileName + "' ʧ�ܡ�ԭ��: " + strError;
            return -1;
		}


		// ����ISO2709����
		// parameter: 
		//		strFileName: Ҫ�����ԴISO2709�ļ�
		int DoImportIso2709(string strFileName,
			string strMarcSyntax,
			Encoding encoding,
			out string strError)
		{
			int nRet;
			strError = "";

            bool bFastMode = this.checkBox_import_fastMode.Checked;

			this.CurMarcSyntax = strMarcSyntax;	// ΪC#�ű�����GetMarc()�Ⱥ����ṩ����

			this.bNotAskTimestampMismatchWhenOverwrite = false;	// Ҫѯ��

			// ׼���������ձ�
			DbNameMap map = DbNameMap.Build(this.textBox_import_dbMap.Text.Replace("\r\n",";"),
                out strError);
            if (map == null)
            {
                strError = "���ݿ���ӳ����򴴽��������ձ�ʱ����: " + strError;
                return -1;
            }

			Stream file = File.Open(strFileName,
				FileMode.Open,
				FileAccess.Read);

			//
			RangeList rl = null;
			long lMax = 0;
			long lMin = 0;
			long lSkipCount = 0;
			int nReadRet = 0;
			string strCount = "";

			//��Χ
			if (textBox_import_range.Text != "") 
			{
				rl = new RangeList(textBox_import_range.Text);
				rl.Sort();
				rl.Merge();
				lMin = rl.min();
				lMax = rl.max();
			}

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ���");
            stop.BeginLoop();

            stop.SetProgressRange(0, file.Length);

            EnableControls(false);

            WriteLog("��ʼ����ISO2709��ʽ����");

			try
			{



				// bool bRet = false;

				this.m_nRecordCount = 0;


				for(long lCount = 0;;lCount ++)
				{
					bool bSkip = false;
					nReadRet = 0;


					Application.DoEvents();	// ���ý������Ȩ


					if (stop.State != 0)
					{
						DialogResult result = MessageBox.Show(this,
							"ȷʵҪ�жϵ�ǰ���������?",
							"dp2batch",
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Question,
							MessageBoxDefaultButton.Button2);
						if (result == DialogResult.Yes)
						{
							strError = "�û��ж�";
							nReadRet = 100;
							goto ERROR1;
						}
						else 
						{
							stop.Continue();
						}
					}


					//������ǰ��¼�Ƿ��ڴ���Χ��
					if (rl != null) 
					{
						if (lMax != -1) // -1:��ȷ��
						{
							if (lCount > lMax)
								nReadRet = 2;	// ���濴�����״̬����break��Ϊʲô��������break������Ϊ�˺�����ʾlabel��Ϣ
						}
						if (rl.IsInRange(lCount, true) == false) 
						{
							bSkip = true;
						}
					}


					// progressBar_main.Value = (int)((file.Position)/ProgressRatio);
                    stop.SetProgressValue(file.Position);

					// ��ʾ��Ϣ
					if (bSkip == true) 
					{
						stop.SetMessage( ((bSkip == true) ? "�������� " : "���ڴ���" )
							+ Convert.ToString(lCount+1) );
					}


					/*
					// ��ֹһ����¼Ҳû�е����,���԰������д��ǰ��
					if (file.Position >= file.Length)
						break;
					*/

					string strMARC = "";

					// ��ISO2709�ļ��ж���һ��MARC��¼
					// return:
					//	-2	MARC��ʽ��
					//	-1	����
					//	0	��ȷ
					//	1	����(��ǰ���صļ�¼��Ч)
					//	2	����(��ǰ���صļ�¼��Ч)
					nRet = MarcUtil.ReadMarcRecord(file, 
						encoding,
						true,	// bRemoveEndCrLf,
						true,	// bForce,
						out strMARC,
						out strError);
                    if (nRet == -2 || nRet == -1)
                    {
                        DialogResult result = MessageBox.Show(this,
                            "����MARC��¼(" + lCount .ToString()+ ")����: " + strError + "\r\n\r\nȷʵҪ�жϵ�ǰ���������?",
                            "dp2batch",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result == DialogResult.Yes)
                        {
                            break;
                        }
                        else
                            continue;
                    }

					if (nRet != 0 && nRet != 1)
						break;

					if (this.batchObj != null)
					{
						batchObj.MarcRecord = strMARC;
						batchObj.MarcRecordChanged = false;
						batchObj.MarcSyntax = strMarcSyntax;
					}

					string strXml = "";

					// ��MARC��¼ת��Ϊxml��ʽ
					nRet = MarcUtil.Marc2Xml(strMARC,
						strMarcSyntax,
						out strXml,
						out strError);
					if (nRet == -1)
						goto ERROR1;

                    // TODO: ������MARC��¼�е�-01�ֶΣ����и��ǲ���
                    // �ѵ���������1)ԭ�������ɸ�-01��Ҫ��������(ָ����Ŀ���)ɸѡ��һ�� 2) dt1000��-01��ֻ�ܿ������޷�����web service url��

					// ����һ��Item
					nRet = DoXmlItemUpload(
                        bFastMode,
                        strXml,
						map,
						bSkip == true || nReadRet == 2,
						strCount,
						out strError);
					if (nRet == -1)
						goto ERROR1;
					if (nRet == 1)
						break;

					strCount = "������ "
						+ Convert.ToString(lCount - lSkipCount)
						+ "��/ ������ " 
						+ Convert.ToString(lSkipCount);


					if (bSkip)
						lSkipCount ++;

					if (nReadRet == 1 || nReadRet == 2)  //�жϴ��ļ�����
						break;

				}
			}
			finally
			{
				file.Close();

                WriteLog("��������ISO2709��ʽ����");
			}

            stop.EndLoop();
            stop.OnStop -= new StopEventHandler(this.DoStop);
            stop.Initial("");

            EnableControls(true);

            strError = "�ָ������ļ� '" + strFileName + "' ��ɡ�";
            return 0;
        ERROR1:
            stop.EndLoop();
            stop.OnStop -= new StopEventHandler(this.DoStop);
            stop.Initial("");
            EnableControls(true);

            if (nReadRet == 100)
                strError = "�ָ������ļ� '" + strFileName + "' ���ж�: " + strError;
            else
                strError = "�ָ������ļ� '" + strFileName + "' ����: " + strError;
            return -1;
		}

		// ����һ��item
		// parameter:
		//		strError: error info
		// return:
		//		-1	����
		//		0	����
		//		1	����
		public int DoXmlItemUpload(
            bool bFastMode,
            XmlTextReader reader,
			DbNameMap map,
			bool bSkip,
			string strCount,
			out string strError)
		{
			strError = "";
			bool bRet = false;
			
			while(true) 
			{
                if (reader.NodeType == XmlNodeType.Element)
                    break;
                bRet = reader.Read();
				if (bRet == false)
					return 1;
			}

            /*
			if (bRet == false)
				return 1;	// ����
             * */


			string strXml = reader.ReadOuterXml();

			return DoXmlItemUpload(
                bFastMode,
                strXml,
				map,
				bSkip,
				strCount,
				out strError);
		}

		public SearchPanel SearchPanel
		{
			get 
			{
				SearchPanel searchpanel = new SearchPanel();
				searchpanel.Initial(this.Servers,
					this.cfgCache);

                // ��ʱsearchpanel.ServerUrlδ��

				return searchpanel;
			}
		}

		// ���Ŀ����¼�
		void CheckTargetDbCallBack(object sender,
			CheckTargetDbEventArgs e)
		{
			string strMarcSyntax = (string)m_tableMarcSyntax[e.DbFullPath];

			if (strMarcSyntax == null)
			{
				string strError = "";

				// ��marcdef�����ļ��л��marc��ʽ����
				// return:
				//		-1	����
				//		0	û���ҵ�
				//		1	�ҵ�
				int nRet = this.SearchPanel.GetMarcSyntax(e.DbFullPath,
					out strMarcSyntax,
					out strError);
				if (nRet == 0 || nRet == -1)
				{
					e.Cancel = true;
					e.ErrorInfo = strError;
					return;
				}

				m_tableMarcSyntax[e.DbFullPath] = strMarcSyntax;
			}

            // if (String.Compare(this.CurMarcSyntax, strMarcSyntax, true) != 0)
            if (String.Compare(e.CurrentMarcSyntax, strMarcSyntax, true) != 0)
            {
                e.Cancel = true;
                // e.ErrorInfo = "��ѡ��� MARC ��ʽ '" + this.CurMarcSyntax + "' ��Ŀ��� '" + e.DbFullPath + "' �е� cfgs/marcdef �����ļ��ж���� MARC ��ʽ '" + strMarcSyntax + "' ���Ǻ�, ���������ж�";
                e.ErrorInfo = "��ѡ��� MARC ��ʽ '" + e.CurrentMarcSyntax + "' ��Ŀ��� '" + e.DbFullPath + "' �е� cfgs/marcdef �����ļ��ж���� MARC ��ʽ '" + strMarcSyntax + "' ���Ǻ�, ���������ж�";
                return;
            }
		}

		// ����һ��XML��¼
		int DoOverwriteXmlRecord(
            bool bFastMode,
            string strRecFullPath,
			string strXmlBody,
			byte [] timestamp,
			out string strError)
		{
			strError = "";

			ResPath respath = new ResPath(strRecFullPath);

            RmsChannel channelSave = channel;

			channel = this.Channels.GetChannel(respath.Url);

			try 
			{

				string strWarning = "";
				byte [] output_timestamp = null;
				string strOutputPath = "";

			REDOSAVE:

				// ����Xml��¼
				long lRet = channel.DoSaveTextRes(respath.Path,
					strXmlBody,
					false,	// bIncludePreamble
					bFastMode == true ? "fastmode" : "",//strStyle,
					timestamp,
					out output_timestamp,
					out strOutputPath,
					out strError);

				if (lRet == -1) 
				{
					if (stop != null) 
						stop.Continue();

					if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
					{
                        string strDisplayRecPath = strOutputPath;
                        if (string.IsNullOrEmpty(strDisplayRecPath) == true)
                            strDisplayRecPath = respath.Path;

						if (this.bNotAskTimestampMismatchWhenOverwrite == true) 
						{
							timestamp = new byte[output_timestamp.Length];
							Array.Copy(output_timestamp, 0, timestamp, 0, output_timestamp.Length);
							strWarning = " (ʱ�����ƥ��, �Զ�����)";
							goto REDOSAVE;
						}


						DialogResult result = MessageDlg.Show(this,
                            "���� '" + strDisplayRecPath  
							+" ʱ����ʱ�����ƥ�䡣��ϸ������£�\r\n---\r\n"
							+ strError + "\r\n---\r\n\r\n�Ƿ�����ʱ���ǿ������?\r\nע��(��)ǿ������ (��)���Ե�ǰ��¼����Դ���أ�����������Ĵ��� (ȡ��)�ж�����������",
							"dp2batch",
							MessageBoxButtons.YesNoCancel,
							MessageBoxDefaultButton.Button1,
							ref this.bNotAskTimestampMismatchWhenOverwrite);
						if (result == DialogResult.Yes) 
						{
							timestamp = new byte[output_timestamp.Length];
							Array.Copy(output_timestamp, 0, timestamp, 0, output_timestamp.Length);
							strWarning = " (ʱ�����ƥ��, Ӧ�û�Ҫ������)";
							goto REDOSAVE;
						}

						if (result == DialogResult.No) 
						{
							return 0;	// �������������Դ
						}

						if (result == DialogResult.Cancel) 
						{
							strError = "�û��ж�";
							goto ERROR1;	// �ж���������
						}
					}

					// ѯ���Ƿ�����
					DialogResult result1 = MessageBox.Show(this, 
						"���� '" + respath.Path  
						+" ʱ����������ϸ������£�\r\n---\r\n"
						+ strError + "\r\n---\r\n\r\n�Ƿ�����?\r\nע��(��)���� (��)�����ԣ�����������Ĵ��� (ȡ��)�ж�����������",
						"dp2batch",
						MessageBoxButtons.YesNoCancel,
						MessageBoxIcon.Question,
						MessageBoxDefaultButton.Button1);
					if (result1 == DialogResult.Yes) 
						goto REDOSAVE;
					if (result1 == DialogResult.No) 
						return 0;	// �������������Դ


					goto ERROR1;
				}

				return 0;
			ERROR1:
				return -1;
			}
			finally 
			{
				channel = channelSave;
			}
		}

		// ����һ��item
		// parameter:
		//		strError: error info
		// return:
		//		-1	����
		//		0	����
		//		1	����
		public int DoXmlItemUpload(
            bool bFastMode,
            string strXml,
			DbNameMap map,
			bool bSkip,
			string strCount,
			out string strError)
		{
			strError = "";
            int nRet = 0;
			// bool bRet = false;
			
			// MessageBox.Show(this, strXml);

			if (bSkip == true)
				return 0;

			XmlDocument dataDom = new XmlDocument();
			try
			{
				dataDom.LoadXml(strXml);
			}
			catch(Exception ex)
			{
				strError = "�������ݵ�dom����!\r\n" + ex.Message;
				goto ERROR1;
			}

			XmlNode node = dataDom.DocumentElement;

			string strResPath = DomUtil.GetAttr(DpNs.dprms, node,"path");

			string strTargetPath = "";

            string strSourceDbPath = "";

            if (strResPath != "")
            {
                // ��map�в�ѯ���ǻ���׷�ӣ�
                ResPath respath0 = new ResPath(strResPath);
                respath0.MakeDbName();
                strSourceDbPath = respath0.FullPath;
            }

        REDO:

            DbNameMapItem mapItem = null;


            mapItem = map.MatchItem(strSourceDbPath/*strResPath*/);
            if (mapItem != null)
                goto MAPITEMOK;

            if (mapItem == null)
            {

                if (strSourceDbPath/*strResPath*/ == "")
                {
                    string strText = "Դ�����ļ��м�¼ " + Convert.ToString(this.m_nRecordCount) + " û����Դ���ݿ�,����������������,������δ���?";
                    WriteLog("�򿪶Ի��� '" + strText.Replace("\r\n", "\\n") + "'");
                    nRet = DbNameMapItemDlg.AskNullOriginBox(
                        this,
                        this.AppInfo,
                        strText,
                        this.SearchPanel,
                        map);
                    WriteLog("�رնԻ��� '" + strText.Replace("\r\n", "\\n") + "'"); 

                    if (nRet == 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;	// �ж���������
                    }

                    goto REDO;

                }
                else
                {
                    string strText = "Դ�����ļ��м�¼ " + Convert.ToString(this.m_nRecordCount) + " ����Դ���ݿ� '" + strSourceDbPath/*strResPath*/ + "' û���ҵ���Ӧ��Ŀ���, ����������������,������δ���?";
                    WriteLog("�򿪶Ի��� '" + strText.Replace("\r\n", "\\n") + "'");
                    nRet = DbNameMapItemDlg.AskNotMatchOriginBox(
                        this,
                        this.AppInfo,
                        strText,
                        this.SearchPanel,
                        strSourceDbPath/*strResPath*/,
                        map);
                    WriteLog("�رնԻ��� '" + strText.Replace("\r\n", "\\n") + "'");
                    if (nRet == 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;	// �ж���������
                    }

                    goto REDO;
                }
            }

        MAPITEMOK:

            if (mapItem.Style == "skip")
                return 0;

            // ����Ŀ��·��

            // 1)��Դ·������ȡid��Դ·�����Ա����ļ�����
            ResPath respath = new ResPath(strResPath);
            string strID = respath.GetRecordId();

            if (strID == null || strID == ""
                || (mapItem.Style == "append")
                )
            {
                strID = "?";	// ������һ���Ի���
            }

			// 2)��Ŀ���·�����������ļ�¼·��
			string strTargetFullPath = "";
			if (mapItem.Target == "*") 
			{
				// ��ʱtargetΪ*, ��Ҫ��strResPath�л�ÿ���

				if (strResPath == "")
				{
					Debug.Assert(false, "�����ܳ��ֵ����");
				}

				respath = new ResPath(strResPath);
				respath.MakeDbName();
				strTargetFullPath = respath.FullPath;
			}
			else 
			{
				strTargetFullPath = mapItem.Target;
			}

			respath = new ResPath(strTargetFullPath);


			// ��Ҫ���Ŀ����������MARC��ʽ
			if (CheckTargetDb != null)
			{
				CheckTargetDbEventArgs e = new CheckTargetDbEventArgs();
				e.DbFullPath = strTargetFullPath;
                e.CurrentMarcSyntax = this.CurMarcSyntax;
				this.CheckTargetDb(this, e);
				if (e.Cancel == true)
				{
					if (e.ErrorInfo == "")
						strError = "CheckTargetDb �¼������ж�";
					else
						strError = e.ErrorInfo;
					return -1;
				}

			}


			strTargetPath = respath.Path + "/" + strID;
			// strRecordPath = strTargetPath;

			channel = this.Channels.GetChannel(respath.Url);

			string strTimeStamp = DomUtil.GetAttr(DpNs.dprms, node,"timestamp");

			byte [] timestamp = ByteArray.GetTimeStampByteArray(strTimeStamp);

            // 2012/5/29
            string strOutMarcSyntax = "";
            string strMARC = "";
            // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
            // parameters:
            //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
            //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
            //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
            nRet = MarcUtil.Xml2Marc(strXml,
                false,
                "",
                out strOutMarcSyntax,
                out strMARC,
                out strError);
            /*
            if (nRet == -1)
                return -1;
             * */

            // 2012/5/30
            if (batchObj != null)
            {
                batchObj.MarcSyntax = strOutMarcSyntax;
                batchObj.MarcRecord = strMARC;
                batchObj.MarcRecordChanged = false;	// Ϊ����Script����׼����ʼ״̬
            }


			if (this.MarcFilter != null)
			{
				// ����filter�е�Record��ض���
				nRet = MarcFilter.DoRecord(
					null,
					batchObj.MarcRecord,
					m_nRecordCount,
					out strError);
				if (nRet == -1) 
					goto ERROR1;
			}

			// C#�ű� -- Inputing
			if (this.AssemblyMain != null) 
			{
				// ��Щ����Ҫ�ȳ�ʼ��,��Ϊfilter��������õ���ЩBatch��Ա.
				batchObj.SkipInput = false;
				batchObj.XmlRecord = strXml;

				//batchObj.MarcSyntax = this.CurMarcSyntax;
				//batchObj.MarcRecord = strMarc;	// MARC��¼��
				//batchObj.MarcRecordChanged = false;	// Ϊ����Script����׼����ʼ״̬


				batchObj.SearchPanel.ServerUrl = channel.Url;
				batchObj.ServerUrl = channel.Url;
				batchObj.RecPath = strTargetPath;	// ��¼·��
				batchObj.RecIndex = m_nRecordCount;	// ��ǰ��¼��һ���е����
				batchObj.TimeStamp = timestamp;


				BatchEventArgs args = new BatchEventArgs();

				batchObj.Inputing(this, args);
				if (args.Continue == ContinueType.SkipAll)
				{
					strError = "�ű��ж�SkipAll";
					goto END2;
				}

				if (batchObj.SkipInput == true)
					return 0;	// ������������
			}


			string strWarning = "";
			byte [] output_timestamp = null;
			string strOutputPath = "";

			REDOSAVE:
				if (stop != null) 
				{
					if (strTargetPath.IndexOf("?") == -1)
					{
						stop.SetMessage("�������� " 
							+ strTargetPath + strWarning + " " + strCount);
					}
				}


			// ����Xml��¼
			long lRet = channel.DoSaveTextRes(strTargetPath,
				strXml,
				false,	// bIncludePreamble
                    bFastMode == true ? "fastmode" : "",//strStyle,
                timestamp,
				out output_timestamp,
				out strOutputPath,
				out strError);

			if (lRet == -1)
            {
                if (stop != null)
                    stop.Continue();

                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                {
                    string strDisplayRecPath = strOutputPath;
                    if (string.IsNullOrEmpty(strDisplayRecPath) == true)
                        strDisplayRecPath = strTargetPath;

                    if (this.bNotAskTimestampMismatchWhenOverwrite == true)
                    {
                        timestamp = new byte[output_timestamp.Length];
                        Array.Copy(output_timestamp, 0, timestamp, 0, output_timestamp.Length);
                        strWarning = " (ʱ�����ƥ��, �Զ�����)";
                        goto REDOSAVE;
                    }

                    string strText = "���� '" + strDisplayRecPath
                        + " ʱ����ʱ�����ƥ�䡣��ϸ������£�\r\n---\r\n"
                        + strError + "\r\n---\r\n\r\n�Ƿ�����ʱ���ǿ������?\r\nע��(��)ǿ������ (��)���Ե�ǰ��¼����Դ���أ�����������Ĵ��� (ȡ��)�ж�����������";
                    WriteLog("�򿪶Ի��� '" + strText.Replace("\r\n", "\\n") + "'");
                    DialogResult result = MessageDlg.Show(this,
                        strText,
                        "dp2batch",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxDefaultButton.Button1,
                        ref this.bNotAskTimestampMismatchWhenOverwrite);
                    WriteLog("�رնԻ��� '" + strText.Replace("\r\n", "\\n") + "'");
                    if (result == DialogResult.Yes)
                    {
                        timestamp = new byte[output_timestamp.Length];
                        Array.Copy(output_timestamp, 0, timestamp, 0, output_timestamp.Length);
                        strWarning = " (ʱ�����ƥ��, Ӧ�û�Ҫ������)";
                        goto REDOSAVE;
                    }

                    if (result == DialogResult.No)
                    {
                        return 0;	// �������������Դ
                    }

                    if (result == DialogResult.Cancel)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;	// �ж���������
                    }
                }

                // ѯ���Ƿ�����
                {
                    string strText = "���� '" + strTargetPath
                        + " ʱ����������ϸ������£�\r\n---\r\n"
                        + strError + "\r\n---\r\n\r\n�Ƿ�����?\r\nע��(��)���� (��)�����ԣ�����������Ĵ��� (ȡ��)�ж�����������";
                    WriteLog("�򿪶Ի��� '" + strText.Replace("\r\n", "\\n") + "'");

                    DialogResult result1 = MessageBox.Show(this,
                        strText,
                        "dp2batch",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    WriteLog("�رնԻ��� '" + strText.Replace("\r\n", "\\n") + "'");
                    if (result1 == DialogResult.Yes)
                        goto REDOSAVE;
                    if (result1 == DialogResult.No)
                        return 0;	// �������������Դ
                }

                goto ERROR1;
            }

			// C#�ű� -- Inputed()
			if (this.AssemblyMain != null) 
			{
				// �󲿷ֱ��������ղ�Inputing()ʱ��ԭ����ֻ�޸Ĳ���

				batchObj.RecPath = strOutputPath;	// ��¼·��
				batchObj.TimeStamp = output_timestamp;

				BatchEventArgs args = new BatchEventArgs();

				batchObj.Inputed(this, args);
                /*
                if (args.Continue == ContinueType.SkipMiddle)
                {
                    strError = "�ű��ж�SkipMiddle";
                    goto END1;
                }
                if (args.Continue == ContinueType.SkipBeginMiddle)
                {
                    strError = "�ű��ж�SkipBeginMiddle";
                    goto END1;
                }
                */
                if (args.Continue == ContinueType.SkipAll)
                {
                    strError = "�ű��ж�SkipAll";
                    goto END1;
                }
            }

            this.m_nRecordCount++;

            if (stop != null)
            {
                stop.SetMessage("�����سɹ� '"
                    + strOutputPath + "' " + strCount);
            }


            // strRecordPath = strOutputPath;

            return 0;
        END1:
        END2:

        ERROR1:
            return -1;
        }

		// ��������
		// parameter: 
		//		strFileName: Ҫ�ָ���Դ�����ļ�
		// ˵��: ����������һ�������Ĺ���,
		//		ֻҪ����������Ȼ˳����������ÿ����¼�Ϳ����ˡ�
		int DoImportBackup(string strFileName,
			out string strError)
		{
			int nRet;
			strError = "";

			this.bNotAskTimestampMismatchWhenOverwrite = false;	// Ҫѯ��

			// ׼���������ձ�
			DbNameMap map = DbNameMap.Build(this.textBox_import_dbMap.Text.Replace("\r\n", ";"),
                out strError);
            if (map == null)
                return -1;


			Stream file = File.Open(strFileName,
				FileMode.Open,
				FileAccess.Read);

			//
			RangeList rl = null;
			long lMax = 0;
			long lMin = 0;
			long lSkipCount = 0;
			int nReadRet = 0;
			string strCount = "";

			//��Χ
			if (textBox_import_range.Text != "") 
			{
				rl = new RangeList(textBox_import_range.Text);
				rl.Sort();
				rl.Merge();
				lMin = rl.min();
				lMax = rl.max();
			}

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ���");
            stop.BeginLoop();

            stop.SetProgressRange(0, file.Length);

            EnableControls(false);

            WriteLog("��ʼ����.dp2bak��ʽ����");

			try
			{


				this.m_nRecordCount = 0;


				for(long lCount = 0;;lCount ++)
				{
					bool bSkip = false;
					nReadRet = 0;

					Application.DoEvents();	// ���ý������Ȩ

					if (stop.State != 0)
					{
						DialogResult result = MessageBox.Show(this,
							"ȷʵҪ�жϵ�ǰ���������?",
							"dp2batch",
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Question,
							MessageBoxDefaultButton.Button2);
						if (result == DialogResult.Yes)
						{
							strError = "�û��ж�";
							nReadRet = 100;
							goto ERROR1;
						}
						else 
						{
							stop.Continue();
						}
					}


					//������ǰ��¼�Ƿ��ڴ���Χ��
					if (rl != null) 
					{
						if (lMax != -1) // -1:��ȷ��
						{
							if (lCount > lMax)
								nReadRet = 2;	// ���濴�����״̬����break��Ϊʲô��������break������Ϊ�˺�����ʾlabel��Ϣ
						}
						if (rl.IsInRange(lCount, true) == false) 
						{
							bSkip = true;
						}
					}

					// progressBar_main.Value = (int)((file.Position)/ProgressRatio);
                    stop.SetProgressValue(file.Position);

					// ��ʾ��Ϣ
					if (bSkip == true) 
					{
						stop.SetMessage( ((bSkip == true) ? "�������� " : "���ڴ���" )
							+ Convert.ToString(lCount+1) );
					}

					// ��ֹһ����¼Ҳû�е����,���԰������д��ǰ��
					if (file.Position >= file.Length)
						break;

					// ����һ��Item
					nRet = DoBackupItemUpload(file,
						ref map,
						bSkip == true || nReadRet == 2,
						strCount,
						out strError);
					if (nRet == -1)
						goto ERROR1;

                    Debug.Assert(file.Position <= file.Length,
                        "����DoBackupItemUpload()������, file�ĵ�ǰλ�ô��ڷǷ�λ��");


					if (bSkip)
						lSkipCount ++;

					strCount = "������ "
						+ Convert.ToString(lCount - lSkipCount)
						+ "��/ ������ " 
						+ Convert.ToString(lSkipCount);

					if (nReadRet == 1 || nReadRet == 2)  //�жϴ��ļ�����
						break;

				}
			}
			finally
			{
				file.Close();

                WriteLog("��������.dp2bak��ʽ����");
			}

            stop.EndLoop();
            stop.OnStop -= new StopEventHandler(this.DoStop);
            stop.Initial("");

            EnableControls(true);

            strError = "�ָ������ļ� '" + strFileName + "' ��ɡ�";
            return 0;
        ERROR1:
            stop.EndLoop();
            stop.OnStop -= new StopEventHandler(this.DoStop);
            stop.Initial("");
            EnableControls(true);

            if (nReadRet == 100)
                strError = "�ָ������ļ� '" + strFileName + "' ���ж�: " + strError;
            else
                strError = "�ָ������ļ� '" + strFileName + "' ����: " + strError;
            return -1;



		}


		// ����һ��item
		// parameter:
		//		file:     Դ�����ļ���
		//		strError: error info
		// return:
		//		-1: error
		//		0:  successed
		public int DoBackupItemUpload(Stream file,
			ref DbNameMap map,  // 2007/6/5 new add
			bool bSkip,
			string strCount,
			out string strError)
		{
			strError = "";

			long lStart = file.Position;

			byte [] data = new byte[8];
			int nRet = file.Read(data, 0 , 8);
			if (nRet == 0)
				return 1;	// �Ѿ�����
			if (nRet < 8) 
			{
				strError = "read file error...";
				return -1;
			}

			// ë����
			long lLength = BitConverter.ToInt64(data, 0);   // +8������һ��bug!!!

			if (bSkip == true)
			{
				file.Seek(lLength, SeekOrigin.Current);
				return 0;
			}

			this.channel = null;

			string strRecordPath = "";

			for(int i=0;;i++)
			{
				Application.DoEvents();	// ���ý������Ȩ

				if (stop.State != 0)
				{
					DialogResult result = MessageBox.Show(this,
						"ȷʵҪ�жϵ�ǰ���������?",
						"dp2batch",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question,
						MessageBoxDefaultButton.Button2);
					if (result == DialogResult.Yes)
					{
						strError = "�û��ж�";
						return -1;
					}
					else 
					{
						stop.Continue();
					}
				}


				// progressBar_main.Value = (int)((file.Position)/ProgressRatio);
                stop.SetProgressValue(file.Position);


				if (file.Position - lStart >= lLength+8)    // 2006/8/29 changed
					break;

				// ���ض�����Դ
				nRet = this.DoResUpload(
					ref this.channel,
					ref strRecordPath,
					file,
					ref map,    // 2007/6/5 new add ref
					i==0? true : false,
					strCount,
					out strError);
				if (nRet == -1)
					return -1;
			}

			return 0;
		}


		// ����һ��res
		// parameter: 
		//		inputfile:   Դ��
		//		bIsFirstRes: �Ƿ��ǵ�һ����Դ(xml)
		//		strError:    error info
		// return:
		//		-2	Ƭ���з���ʱ�����ƥ�䡣������������������������Դ
		//		-1	error
		//		0	successed
		public int DoResUpload(
            ref RmsChannel channel,
			ref string strRecordPath,
			Stream inputfile,
			ref DbNameMap map,
			bool bIsFirstRes,
			string strCount,
			out string strError)
		{
			strError = "";
			
			int nRet;
			long lBodyStart = 0;
			long lBodyLength = 0;

			// 1. ���������еõ�strMetadata,��body(body�ŵ�һ����ʱ�ļ���)
			string strMetaDataXml = "";

			nRet = GetResInfo(inputfile,
				bIsFirstRes,
				out strMetaDataXml,
				out lBodyStart,
				out lBodyLength,
				out strError);
			if (nRet == -1)
				goto ERROR1; 

			if (lBodyLength == 0)
				return 0;	// �հ���������
			

			// 2.Ϊ������׼��
			XmlDocument metadataDom = new XmlDocument();
			try
			{
				metadataDom.LoadXml(strMetaDataXml);
			}
			catch(Exception ex)
			{
				strError = "����Ԫ���ݵ�dom����!\r\n" + ex.Message;
				goto ERROR1;
			}

			XmlNode node = metadataDom.DocumentElement;

			string strResPath = DomUtil.GetAttr(node,"path");

			string strTargetPath = "";

			if (bIsFirstRes == true) // ��һ����Դ
			{
				// ��map�в�ѯ���ǻ���׷�ӣ�
				ResPath respath = new ResPath(strResPath);
				respath.MakeDbName();

			REDO:
				DbNameMapItem mapItem = (DbNameMapItem)map["*"];
				if (mapItem != null)
				{
				}
				else 
				{
					mapItem = (DbNameMapItem)map[respath.FullPath.ToUpper()];
				}

				if (mapItem == null) 
				{
					OriginNotFoundDlg dlg = new OriginNotFoundDlg();
                    MainForm.SetControlFont(dlg, this.DefaultFont);

					dlg.Message = "���������������ݿ�·�� '" +respath.FullPath+ "' �ڸ��ǹ�ϵ���ձ���û���ҵ�, ��ѡ�񸲸Ƿ�ʽ: " ;
					dlg.Origin = respath.FullPath.ToUpper();
					dlg.Servers = this.Servers;
					dlg.Channels = this.Channels;
					dlg.Map = map;

                    dlg.StartPosition = FormStartPosition.CenterScreen;
					dlg.ShowDialog(this);

					if (dlg.DialogResult != DialogResult.OK) 
					{
						strError = "�û��ж�...";
						goto ERROR1;
					}

					map = dlg.Map;
					goto REDO;
				}

				if (mapItem.Style == "skip")
					return 0;

				// ����Ŀ��·��

				// 1)��Դ·������ȡid��Դ·�����Ա����ļ�����
				respath = new ResPath(strResPath);
				string strID = respath.GetRecordId();

				if (strID == null || strID == ""
					|| (mapItem.Style == "append")
					)
				{
					strID = "?";	// ������һ���Ի���
				}

				// 2)��Ŀ���·�����������ļ�¼·��
				string strTargetFullPath = "";
				if (mapItem.Target == "*") 
				{
					respath = new ResPath(strResPath);
					respath.MakeDbName();
					strTargetFullPath = respath.FullPath;
				}
				else 
				{
					strTargetFullPath = mapItem.Target;
				}

				respath = new ResPath(strTargetFullPath);
				strTargetPath = respath.Path + "/" + strID;
				strRecordPath = strTargetPath;

				channel = this.Channels.GetChannel(respath.Url);

			}
			else // �ڶ����Ժ����Դ
			{
				if (channel == null)
				{
					strError = "��bIsFirstRes==falseʱ������channel��ӦΪnull...";
					goto ERROR1;
				}


				ResPath respath = new ResPath(strResPath);
				string strObjectId = respath.GetObjectId();
				if (strObjectId == null || strObjectId == "") 
				{
					strError = "object idΪ��...";
					goto ERROR1;
				}
				strTargetPath = strRecordPath + "/object/" + strObjectId;
				if (strRecordPath == "")
				{
					strError = "strRecordPath����ֵΪ��...";
					goto ERROR1;
				}
			}


			// string strLocalPath = DomUtil.GetAttr(node,"localpath");
			// string strMimeType = DomUtil.GetAttr(node,"mimetype");
			string strTimeStamp = DomUtil.GetAttr(node,"timestamp");
			// ע��,strLocalPath������Ҫ���ص�body�ļ�,��ֻ������Ԫ����\
			// body�ļ�ΪstrBodyTempFileName


			// 3.��body�ļ���ֳ�Ƭ�Ͻ�������
			string[] ranges = null;

			if (lBodyLength == 0)	
			{ // ���ļ�
				ranges = new string[1];
				ranges[0] = "";
			}
			else 
			{
				string strRange = "";
				strRange = "0-" + Convert.ToString(lBodyLength-1);

				// ����100K��Ϊһ��chunk
				ranges = RangeList.ChunkRange(strRange,
					100*1024);
			}



			byte [] timestamp = ByteArray.GetTimeStampByteArray(strTimeStamp);
			byte [] output_timestamp = null;

			REDOWHOLESAVE:
				string strOutputPath = "";
			string strWarning = "";

			for(int j=0;j<ranges.Length;j++) 
			{
			REDOSINGLESAVE:

				Application.DoEvents();	// ���ý������Ȩ

				if (stop.State != 0)
				{
					DialogResult result = MessageBox.Show(this,
						"ȷʵҪ�жϵ�ǰ���������?",
						"dp2batch",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question,
						MessageBoxDefaultButton.Button2);
					if (result == DialogResult.Yes)
					{
						strError = "�û��ж�";
						goto ERROR1;
					}
					else 
					{
						stop.Continue();
					}
				}


				string strWaiting = "";
				if (j == ranges.Length - 1)
					strWaiting = " �����ĵȴ�...";

				string strPercent = "";
				RangeList rl = new RangeList(ranges[j]);
				if (rl.Count >= 1) 
				{
					double ratio = (double)((RangeItem)rl[0]).lStart / (double)lBodyLength;
					strPercent = String.Format("{0,3:N}",ratio * (double)100) + "%";
				}

				if (stop != null)
					stop.SetMessage("�������� " + ranges[j] + "/"
						+ Convert.ToString(lBodyLength)
						+ " " + strPercent + " " + strTargetPath + strWarning + strWaiting + " " + strCount);


				inputfile.Seek(lBodyStart, SeekOrigin.Begin);

				long lRet = channel.DoSaveResObject(strTargetPath,
					inputfile,
					lBodyLength,
					"",	// style
					strMetaDataXml,
					ranges[j],
					j == ranges.Length - 1 ? true : false,	// ��βһ�β��������ѵײ�ע�����������WebService API��ʱʱ��
					timestamp,
					out output_timestamp,
					out strOutputPath,
					out strError);

				// progressBar_main.Value = (int)((inputfile.Position)/ProgressRatio);
                stop.SetProgressValue(inputfile.Position);

				strWarning = "";

				if (lRet == -1) 
				{
					if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
					{
                        string strDisplayRecPath = strOutputPath;
                        if (string.IsNullOrEmpty(strDisplayRecPath) == true)
                            strDisplayRecPath = strTargetPath;

						if (this.bNotAskTimestampMismatchWhenOverwrite == true) 
						{
							timestamp = new byte[output_timestamp.Length];
							Array.Copy(output_timestamp, 0, timestamp, 0, output_timestamp.Length);
							strWarning = " (ʱ�����ƥ��, �Զ�����)";
							if (ranges.Length == 1 || j==0) 
								goto REDOSINGLESAVE;
							goto REDOWHOLESAVE;
						}


						DialogResult result = MessageDlg.Show(this,
                            "���� '" + strDisplayRecPath + "' (Ƭ��:" + ranges[j] + "/�ܳߴ�:" + Convert.ToString(lBodyLength)
							+") ʱ����ʱ�����ƥ�䡣��ϸ������£�\r\n---\r\n"
							+ strError + "\r\n---\r\n\r\n�Ƿ�����ʱ���ǿ������?\r\nע��(��)ǿ������ (��)���Ե�ǰ��¼����Դ���أ�����������Ĵ��� (ȡ��)�ж�����������",
							"dp2batch",
							MessageBoxButtons.YesNoCancel,
							MessageBoxDefaultButton.Button1,
							ref this.bNotAskTimestampMismatchWhenOverwrite);
						if (result == DialogResult.Yes) 
						{

							if (output_timestamp != null)
							{
								timestamp = new byte[output_timestamp.Length];
								Array.Copy(output_timestamp, 0, timestamp, 0, output_timestamp.Length);
							}
							else
							{
								timestamp = output_timestamp;
							}
							strWarning = " (ʱ�����ƥ��, Ӧ�û�Ҫ������)";
							if (ranges.Length == 1 || j==0) 
								goto REDOSINGLESAVE;
							goto REDOWHOLESAVE;
						}

						if (result == DialogResult.No) 
						{
							return 0;	// �������������Դ
						}

						if (result == DialogResult.Cancel) 
						{
							strError = "�û��ж�";
							goto ERROR1;	// �ж���������
						}
					}


					goto ERROR1;
				}

				timestamp = output_timestamp;
			}

			// ���ǵ������һ����Դ��ʱ��id����Ϊ��?���������Ҫ�õ�ʵ�ʵ�idֵ
			if (bIsFirstRes)
				strRecordPath = strOutputPath;

			return 0;
			
			ERROR1:
				return -1;
		}


		// ���������еõ�һ��res��metadata��body
		// parameter:
		//		inputfile:       Դ��
		//		bIsFirstRes:     �Ƿ��ǵ�һ����Դ
		//		strMetaDataXml:  ����metadata����
		//		strError:        error info
		// return:
		//		-1: error
		//		0:  successed
		public static int GetResInfo(Stream inputfile,
			bool bIsFirstRes,
			out string strMetaDataXml,
			out long lBodyStart,
			out long lBodyLength,
			out string strError)
		{
			strMetaDataXml = "";
			strError = "";
			lBodyStart = 0;
			lBodyLength = 0;

			byte [] length = new byte[8];

			// �����ܳ���
			int nRet = inputfile.Read(length, 0 , 8);
			if (nRet < 8) 
			{
				strError = "��ȡres�ܳ��Ȳ��ֳ���...";
				return -1;
			}

			long lTotalLength = BitConverter.ToInt64(length, 0);

			// ����metadata����
			nRet = inputfile.Read(length, 0 , 8);
			if (nRet < 8) 
			{
				strError = "��ȡmetadata���Ȳ��ֳ���...";
				return -1;
			}

			long lMetaDataLength = BitConverter.ToInt64(length, 0);

			if (lMetaDataLength >= 100*1024)
			{
				strError = "metadata���ݳ��ȳ���100K���Ʋ�����ȷ��ʽ...";
				return -1;
			}

			byte[] metadata = new byte[(int)lMetaDataLength];
			int nReadLength = inputfile.Read(metadata,
				0,
				(int)lMetaDataLength);
			if (nReadLength < (int)lMetaDataLength)
			{
				strError = "metadata�����ĳ��ȳ����ļ�ĩβ����ʽ����";
				return -1;
			}

			strMetaDataXml = Encoding.UTF8.GetString(metadata);	// ? �Ƿ�����׳��쳣

			// ��body���ֵĳ���
			nRet = inputfile.Read(length, 0 , 8);
			if (nRet < 8) 
			{
				strError = "��ȡbody���Ȳ��ֳ���...";
				return -1;
			}

			lBodyStart = inputfile.Position;

			lBodyLength = BitConverter.ToInt64(length, 0);
			if (bIsFirstRes == true && lBodyLength >= 2000*1024)
			{
				strError = "��һ��res��body��xml���ݳ��ȳ���2000K���Ʋ�����ȷ��ʽ...";
				return -1;
			}

			return 0;
		}

		// ����������ձ�
		private void button_import_dbMap_Click(object sender, System.EventArgs e)
		{
			DbNameMapDlg dlg = new DbNameMapDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

            string strError = "";

			dlg.SearchPanel = this.SearchPanel;
			dlg.DbNameMap = DbNameMap.Build(this.textBox_import_dbMap.Text.Replace("\r\n",";"),
                out strError);
            if (dlg.DbNameMap == null)
            {
                MessageBox.Show(this, strError);
                return;
            }

			this.AppInfo.LinkFormState(dlg, "DbNameMapDlg_state");
			dlg.ShowDialog(this);
			this.AppInfo.UnlinkFormState(dlg);

			if (dlg.DialogResult != DialogResult.OK)
				return;

			this.textBox_import_dbMap.Text = dlg.DbNameMap.ToString(true).Replace(";", "\r\n");
		}


		/*
		void oldfindTargetDB()
		{
			OpenResDlg dlg = new OpenResDlg();

			dlg.Text = "��ѡ��Ŀ�����ݿ�";
			dlg.EnabledIndices = new int[] { ResTree.RESTYPE_DB };
			dlg.ap = this.applicationInfo;
			dlg.ApCfgTitle = "pageimport_openresdlg";
			dlg.MultiSelect = true;
			dlg.Paths = textBox_import_targetDB.Text;
			dlg.Initial( this.Servers,
				this.Channels);	
			// dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;

			textBox_import_targetDB.Text = dlg.Paths;
		}
		*/


		void DoStop(object sender, StopEventArgs e)
		{
			if (this.channel != null)
				this.channel.Abort();
		}


		private void button_import_findFileName_Click(object sender, System.EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();

			dlg.FileName = textBox_import_fileName.Text;
			dlg.Filter = "�����ļ� (*.dp2bak)|*.dp2bak|XML�ļ� (*.xml)|*.xml|ISO2709�ļ� (*.iso;*.mrc)|*.iso;*.mrc|All files (*.*)|*.*" ;
			dlg.RestoreDirectory = true ;

			if(dlg.ShowDialog() != DialogResult.OK)
			{
				return;
			}

			textBox_import_fileName.Text = dlg.FileName;

		}

		// ׼���ű�����
		int PrepareScript(string strProjectName,
			string strProjectLocate,
			out Assembly assemblyMain,
			out MyFilterDocument filter,
			out Batch batchObj,
			out string strError)
		{
			assemblyMain = null;
			Assembly assemblyFilter = null;
			filter = null;
			batchObj = null;

			string strWarning = "";
			string strMainCsDllName = strProjectLocate + "\\~main_" + Convert.ToString(m_nAssemblyVersion++)+ ".dll";

            string strLibPaths = "\"" + this.DataDir + "\""
				+ "," 
				+ "\"" + strProjectLocate + "\"";

			string[] saAddRef = {
									Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.rms.Client.dll",
									Environment.CurrentDirectory + "\\digitalplatform.library.dll",
									// Environment.CurrentDirectory + "\\digitalplatform.statis.dll",
									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
									Environment.CurrentDirectory + "\\dp2batch.exe"};


			// ����Project��Script main.cs��Assembly
			// return:
			//		-2	���������Ѿ���ʾ��������Ϣ�ˡ�
			//		-1	����
			int nRet = scriptManager.BuildAssembly(
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

			assemblyMain = Assembly.LoadFrom(strMainCsDllName);
			if (assemblyMain == null) 
			{
				strError = "LoadFrom " + strMainCsDllName + " fail";
				goto ERROR1;
			}


			// �õ�Assembly��Batch������Type
			Type entryClassType = ScriptManager.GetDerivedClassType(
				assemblyMain,
				"dp2Batch.Batch");

			// newһ��Batch��������
			batchObj = (Batch)entryClassType.InvokeMember(null, 
				BindingFlags.DeclaredOnly | 
				BindingFlags.Public | BindingFlags.NonPublic | 
				BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
				null);

			// ΪBatch���������ò���
			batchObj.MainForm = this;
			batchObj.ap = this.AppInfo;
			batchObj.ProjectDir = strProjectLocate;
			batchObj.DbPath = this.textBox_dbPath.Text;
			batchObj.SearchPanel = this.SearchPanel;
			/*
			batchObj.SearchPanel.InitialStopManager(this.toolBarButton_stop,
				this.statusBar_main);
			*/

			// batchObj.Channel = channel;
			//batchObj.GisIniFilePath = applicationInfo.GetString(
			//	"preference",
			//	"gisinifilepath",
			//	"");

			////////////////////////////
			// װ��marfilter.fltx
			string strFilterFileName = strProjectLocate + "\\marcfilter.fltx";

			if (FileUtil.FileExist(strFilterFileName) == true) 
			{

				filter = new MyFilterDocument();

				filter.Batch = batchObj;
				filter.strOtherDef = entryClassType.FullName + " Batch = null;";

			
				filter.strPreInitial = " MyFilterDocument doc = (MyFilterDocument)this.Document;\r\n";
				filter.strPreInitial += " Batch = ("
					+ entryClassType.FullName + ")doc.Batch;\r\n";

				filter.Load(strFilterFileName);

				nRet = filter.BuildScriptFile(strProjectLocate + "\\marcfilter.fltx.cs",
					out strError);
				if (nRet == -1)
					goto ERROR1;

				string[] saAddRef1 = {
										 Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.rms.client.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.library.dll",
										 // this.DataDir + "\\digitalplatform.statis.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
										 Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
										 Environment.CurrentDirectory + "\\dp2batch.exe",
										 strMainCsDllName};

				string strfilterCsDllName = strProjectLocate + "\\~marcfilter_" + Convert.ToString(m_nAssemblyVersion++)+ ".dll";

				// ����Project��Script��Assembly
				nRet = scriptManager.BuildAssembly(
                    "MainForm",
					strProjectName,
					"marcfilter.fltx.cs",
					saAddRef1,
					strLibPaths,
					strfilterCsDllName,
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


				assemblyFilter = Assembly.LoadFrom(strfilterCsDllName);
				if (assemblyFilter == null) 
				{
					strError = "LoadFrom " + strfilterCsDllName + "fail";
					goto ERROR1;
				}


				filter.Assembly = assemblyFilter;

			}

			return 0;

			ERROR1:
				return -1;
		}

        public void WriteLog(string strText)
        {
            FileUtil.WriteErrorLog(
                this,
                this.DataDir,
                strText);
        }

        // ���
		void DoExport(string strProjectName,
			string strProjectLocate)
		{
			string strError = "";
			int nRet = 0;

			Assembly assemblyMain = null;
			MyFilterDocument filter = null;
			batchObj = null;
			m_nRecordCount = -1;


			// ׼���ű�
			if (strProjectName != "" && strProjectName != null)
			{
				nRet = PrepareScript(strProjectName,
					strProjectLocate,
					out assemblyMain,
					out filter,
					out batchObj,
					out strError);
				if (nRet == -1)
					goto ERROR1;

				this.AssemblyMain = assemblyMain;
				if (filter != null)
					this.AssemblyFilter = filter.Assembly;
				else
					this.AssemblyFilter = null;

			}


			// ִ�нű���OnInitial()

			// ����Script��OnInitial()����
			// OnInitial()��OnBegin�ı�������, ����OnInitial()�ʺϼ�������������
			if (batchObj != null)
			{
				BatchEventArgs args = new BatchEventArgs();
				batchObj.OnInitial(this, args);
				/*
				if (args.Continue == ContinueType.SkipBeginMiddle)
					goto END1;
				if (args.Continue == ContinueType.SkipMiddle) 
				{
					strError = "OnInitial()��args.Continue����ʹ��ContinueType.SkipMiddle.Ӧʹ��ContinueType.SkipBeginMiddle";
					goto ERROR1;
				}
				*/
				if (args.Continue == ContinueType.SkipAll)
					goto END1;
			}

			string strOutputFileName = "";

			if (textBox_dbPath.Text == "")
			{
				MessageBox.Show(this, "��δѡ��Դ��...");
				return;
			}

            string[] dbpaths = this.textBox_dbPath.Text.Split(new char[] {';'});

            Debug.Assert(dbpaths.Length != 0, "");

            // ���Ϊ�������
            if (dbpaths.Length == 1)
            {
                // �����Ƶ�DoExportFile()��������ȥУ��
                ResPath respath = new ResPath(dbpaths[0]);

                channel = this.Channels.GetChannel(respath.Url);

                string strDbName = respath.Path;

                // У����ֹ��
                if (checkBox_verifyNumber.Checked == true)
                {
                    nRet = VerifyRange(channel,
                        strDbName,
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(this, strError);
                }
                else
                {
                    if (this.textBox_startNo.Text == "")
                    {
                        strError = "��δָ����ʼ��";
                        goto ERROR1;
                    }
                    if (this.textBox_endNo.Text == "")
                    {
                        strError = "��δָ��������";
                        goto ERROR1;
                    }
                }
            }
            else
            {
                // ���������޸Ľ���Ҫ�أ���ʾ���ÿ���ⶼ��ȫ�⴦��
                this.radioButton_all.Checked = true;
                this.textBox_startNo.Text = "1";
                this.textBox_endNo.Text = "9999999999";
            }
             

			SaveFileDialog dlg = null;

            if (checkBox_export_delete.Checked == true)
            {
                DialogResult result = MessageBox.Show(this,
                        "ȷʵҪ(�������ͬʱ)ɾ�����ݿ��¼?\r\n\r\n---------\r\n(ȷ��)ɾ�� (����)����������",
                        "dp2batch",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                if (result != DialogResult.OK)
                {
                    strError = "��������...";
                    goto ERROR1;
                }

                result = MessageBox.Show(this,
                    "��ɾ����¼��ͬʱ, �Ƿ񽫼�¼������ļ�?\r\n\r\n--------\r\n(��)һ��ɾ��һ����� (��)ֻɾ�������",
                    "dp2batch",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (result != DialogResult.Yes)
                    goto SKIPASKFILENAME;
            }


			// �������ļ���
			dlg = new SaveFileDialog();

			dlg.Title = "��ָ��Ҫ����ı����ļ���";
			dlg.CreatePrompt = false;
			dlg.OverwritePrompt = false;
			dlg.FileName = strLastOutputFileName;
			dlg.FilterIndex = nLastOutputFilterIndex;

			dlg.Filter = "�����ļ� (*.dp2bak)|*.dp2bak|XML�ļ� (*.xml)|*.xml|ISO2709�ļ� (*.iso;*.mrc)|*.iso;*.mrc|All files (*.*)|*.*" ;

			dlg.RestoreDirectory = true;

			if (dlg.ShowDialog(this) != DialogResult.OK) 
			{
				strError = "��������...";
				goto ERROR1;
			}

			strLastOutputFileName = dlg.FileName;
			nLastOutputFilterIndex = dlg.FilterIndex;
			strOutputFileName = dlg.FileName;

			SKIPASKFILENAME:

				// ����Script��OnBegin()����
				// OnBegin()����Ȼ���޸�MainForm��������
			if (batchObj != null)
			{
				BatchEventArgs args = new BatchEventArgs();
				batchObj.OnBegin(this, args);
				/*
				if (args.Continue == ContinueType.SkipMiddle)
					goto END1;
				if (args.Continue == ContinueType.SkipBeginMiddle)
					goto END1;
				*/
				if (args.Continue == ContinueType.SkipAll)
					goto END1;
			}



            if (dlg == null || dlg.FilterIndex == 1)
                nRet = DoExportFile(
                    dbpaths,
                    strOutputFileName,
                    ExportFileType.BackupFile,
                    null,
                    out strError);
            else if (dlg.FilterIndex == 2)
                nRet = DoExportFile(
                    dbpaths,
                    strOutputFileName,
                    ExportFileType.XmlFile,
                    null,
                    out strError);
            else if (dlg.FilterIndex == 3)
            {
                ResPath respath = new ResPath(dbpaths[0]);

                string strMarcSyntax = "";
                // ��marcdef�����ļ��л��marc��ʽ����
                // return:
                //		-1	����
                //		0	û���ҵ�
                //		1	�ҵ�
                nRet = this.SearchPanel.GetMarcSyntax(respath.FullPath,
                    out strMarcSyntax,
                    out strError);
                if (nRet == 0 || nRet == -1)
                {
                    strError = "��ȡ���ݿ� '" + dbpaths[0] + "' ��MARC��ʽʱ��������: " + strError;
                    goto ERROR1;
                }

                // �������һ�����ݿ������һ���ļ�����Ҫ����ÿ�����ݿ��MARC��ʽ�Ƿ���ͬ�������ʵ��ľ���
                if (dbpaths.Length > 1)
                {
                    string strWarning = "";
                    for (int i = 1; i < dbpaths.Length; i++)
                    {
                        ResPath current_respath = new ResPath(dbpaths[i]);

                        string strPerMarcSyntax = "";
                        // ��marcdef�����ļ��л��marc��ʽ����
                        // return:
                        //		-1	����
                        //		0	û���ҵ�
                        //		1	�ҵ�
                        nRet = this.SearchPanel.GetMarcSyntax(current_respath.FullPath,
                            out strPerMarcSyntax,
                            out strError);
                        if (nRet == 0 || nRet == -1)
                        {
                            strError = "��ȡ���ݿ� '" + dbpaths[i] + "' ��MARC��ʽʱ��������: " + strError;
                            goto ERROR1;
                        }

                        if (strPerMarcSyntax != strMarcSyntax)
                        {
                            if (String.IsNullOrEmpty(strWarning) == false)
                                strWarning += "\r\n";
                            strWarning += "���ݿ� '" + dbpaths[i] + "' (" + strPerMarcSyntax + ")";

                        }
                    }

                    if (String.IsNullOrEmpty(strWarning) == false)
                    {
                        strWarning = "��ѡ������ݿ��У��������ݿ��MARC��ʽ�͵�һ�����ݿ�( '"+dbpaths[0]+"' ("+strMarcSyntax+"))�Ĳ�ͬ: \r\n---\r\n" + strWarning + "\r\n---\r\n\r\n�������Щ��ͬMARC��ʽ�ļ�¼��������һ���ļ��У����ܻ�����������Ժ��ȡ��ʱ�������ѡ�\r\n\r\nȷʵҪ���������ת����һ���ļ���?";
                        DialogResult result = MessageBox.Show(this,
                            strWarning,
                            "dp2batch",
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

                OpenMarcFileDlg marcdlg = new OpenMarcFileDlg();
                MainForm.SetControlFont(marcdlg, this.DefaultFont);
                marcdlg.IsOutput = true;
                marcdlg.Text = "��ָ��Ҫ����� ISO2709 �ļ�����";
                marcdlg.FileName = strOutputFileName;
                marcdlg.MarcSyntax = strMarcSyntax;
                marcdlg.EnableMarcSyntax = false;   // �������û�ѡ��marc syntax����Ϊ�������ݿ����ú��˵����� 2007/8/18

                marcdlg.CrLf = this.OutputCrLf;
                marcdlg.AddG01 = this.AddG01;
                marcdlg.RemoveField998 = this.Remove998;


                this.AppInfo.LinkFormState(marcdlg, "OpenMarcFileDlg_output_state");
                marcdlg.ShowDialog(this);
                this.AppInfo.UnlinkFormState(marcdlg);


                if (marcdlg.DialogResult != DialogResult.OK)
                {
                    strError = "��������...";
                    goto ERROR1;
                }

                if (marcdlg.AddG01 == true)
                {
                    MessageBox.Show(this, "��ѡ�����ڵ�����ISO2709��¼�м���-01�ֶΡ���ע��dp2Batch�ڽ�������������ISO2709�ļ���ʱ�򣬼�¼��-01�ֶ�***�𲻵�***���Ƕ�λ�����á�������-01�ֶΡ�������Ϊ�˽�������ISO2709�ļ�Ӧ�õ�dt1000ϵͳ����Ƶġ�\r\n\r\n�������������Ŀ����Ϊ�˶�dp2ϵͳ��Ŀ���е����ݽ��б��ݣ������.xml��ʽ��.dp2bak��ʽ��");
                }


                strOutputFileName = marcdlg.FileName;
                this.CurMarcSyntax = strMarcSyntax;
                this.OutputCrLf = marcdlg.CrLf;
                this.AddG01 = marcdlg.AddG01;
                this.Remove998 = marcdlg.RemoveField998;

                nRet = DoExportFile(
                    dbpaths,
                    marcdlg.FileName,
                    ExportFileType.ISO2709File,
                    marcdlg.Encoding,
                    out strError);
            }
            else
            {
                strError = "��֧�ֵ��ļ�����...";
                goto ERROR1;
            }

            /*
            if (nRet == 1)
                goto END2;
            */
            if (nRet == -1)
                goto ERROR1;
        END1:
            // ����Script��OnEnd()����
            if (batchObj != null)
            {
                BatchEventArgs args = new BatchEventArgs();
                batchObj.OnEnd(this, args);
            }

            // END2:

            this.AssemblyMain = null;
            this.AssemblyFilter = null;
            if (filter != null)
                filter.Assembly = null;
            this.MarcFilter = null;

            if (String.IsNullOrEmpty(strError) == false)
			    MessageBox.Show(this, strError);
			return;

        ERROR1:
            this.AssemblyMain = null;
            this.AssemblyFilter = null;
            if (filter != null)
                filter.Assembly = null;
            this.MarcFilter = null;


            MessageBox.Show(this, strError);

		}


#if NNNNN
		void DoExportXmlFile(string strOutputFileName)
		{
			string strError = "";

			FileStream outputfile = null;
			XmlTextWriter writer = null;   

			if (textBox_dbPath.Text == "")
			{
				MessageBox.Show(this, "��δѡ��Դ��...");
				return;
			}

			ResPath respath = new ResPath(textBox_dbPath.Text);

			channel = this.Channels.GetChannel(respath.Url);

			string strDbName = respath.Path;

			if (strOutputFileName != null && strOutputFileName != "") 
			{
				// ̽���ļ��Ƿ����
				FileInfo fi = new FileInfo(strOutputFileName);
				if (fi.Exists == true && fi.Length > 0)
				{
					DialogResult result = MessageBox.Show(this,
						"�ļ� '" + strOutputFileName + "' �Ѵ��ڣ��Ƿ񸲸�?\r\n\r\n--------------------\r\nע��(��)����  (��)�жϴ���",
						"dp2batch",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question,
						MessageBoxDefaultButton.Button2);
					if (result != DialogResult.Yes) 
					{
						strError = "��������...";
						goto ERROR1;
					}
				}

				// ���ļ�
				outputfile = File.Create(
					strOutputFileName);

				writer = new XmlTextWriter(outputfile, Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				writer.Indentation = 4;

			}


			try 
			{
				
				Int64 nStart;
				Int64 nEnd;
				Int64 nCur;
				bool bAsc = GetDirection(out nStart,
					out nEnd);

				// ���ý�������Χ
				Int64 nMax = nEnd - nStart;
				if (nMax < 0)
					nMax *= -1;
				nMax ++;

				ProgressRatio =  nMax / 10000;
				if (ProgressRatio < 1.0)
					ProgressRatio = 1.0;

				progressBar_main.Minimum = 0;
				progressBar_main.Maximum = (int)(nMax/ProgressRatio);
				progressBar_main.Value = 0;


				bool bFirst = true;	// �Ƿ�Ϊ��һ��ȡ��¼

				string strID = this.textBox_startNo.Text;


				stop.Initial(new Delegate_doStop(this.DoStop),
					"���ڵ�������");
				stop.BeginLoop();

				EnableControls(false);

				if (writer != null) 
				{
					writer.WriteStartDocument();
					writer.WriteStartElement("dprms","collection",DpNs.dprms);
					//writer.WriteStartElement("collection");
					//writer.WriteAttributeString("xmlns:marc",
					//	"http://www.loc.gov/MARC21/slim");

				}

				// ѭ��
				for(;;) 
				{
					Application.DoEvents();	// ���ý������Ȩ

					if (stop.State != 0)
					{
						strError = "�û��ж�";
						goto ERROR1;
					}

					string strStyle = "";
					if (outputfile != null)
						strStyle = "data,content,timestamp,outputpath";
					else
						strStyle = "timestamp,outputpath";	// �Ż�

					if (bFirst == true)
						strStyle += "";
					else 
					{
						if (bAsc == true)
							strStyle += ",next";
						else
							strStyle += ",prev";
					}


					string strPath = strDbName + "/" + strID;
					string strXmlBody = "";
					string strMetaData = "";
					byte[] baOutputTimeStamp = null;
					string strOutputPath = "";

					bool bFoundRecord = false;

					// �����Դ
					// return:
					//		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
					//		0	�ɹ�
					long lRet = channel.GetRes(strPath,
						strStyle,
						out strXmlBody,
						out strMetaData,
						out baOutputTimeStamp,
						out strOutputPath,
						out strError);


					if (lRet == -1) 
					{
						if (channel.ErrorCode == ChannelErrorCode.NotFound) 
						{
							if (checkBox_forceLoop.Checked == true && bFirst == true)
							{
								AutoCloseMessageBox.Show(this, "��¼ " + strID + " �����ڡ�\r\n\r\n�� ȷ�� ������");

								bFirst = false;
								goto CONTINUE;
							}
							else 
							{
								if (bFirst == true)
								{
									strError = "��¼ " + strID + " �����ڡ����������";
								}
								else 
								{
									if (bAsc == true)
										strError = "��¼ " + strID + " ����ĩһ����¼�����������";
									else
										strError = "��¼ " + strID + " ����ǰһ����¼�����������";
								}
							}

						}
						else if (channel.ErrorCode == ChannelErrorCode.EmptyRecord) 
						{
							bFirst = false;
							bFoundRecord = false;
							// ��id��������
							strID = ResPath.GetRecordId(strOutputPath);
							goto CONTINUE;

						}

						goto ERROR1;
					}

					bFirst = false;

					bFoundRecord = true;

					// ��id��������
					strID = ResPath.GetRecordId(strOutputPath);

				CONTINUE:
					stop.SetMessage(strID);

					// �Ƿ񳬹�ѭ����Χ
					try 
					{
						nCur = Convert.ToInt64(strID);
					}
					catch
					{
						// ???
						nCur = 0;
					}

					if (bAsc == true && nCur > nEnd)
						break;
					if (bAsc == false && nCur < nEnd)
						break;

					if (bFoundRecord == true 
						&& writer != null) 
					{
						// д����
						XmlDocument dom = new XmlDocument();

						try 
						{
							dom.LoadXml(strXmlBody);

							ResPath respathtemp = new ResPath();
							respathtemp.Url = channel.Url;
							respathtemp.Path = strOutputPath;



							// DomUtil.SetAttr(dom.DocumentElement, "xmlns:dprms", DpNs.dprms);
							// ����Ԫ�����ü�������
							DomUtil.SetAttr(dom.DocumentElement, "path", DpNs.dprms, respathtemp.FullPath);
							DomUtil.SetAttr(dom.DocumentElement, "timestamp", DpNs.dprms, ByteArray.GetHexTimeStampString(baOutputTimeStamp));

							// DomUtil.SetAttr(dom.DocumentElement, "xmlns:marc", null);
							dom.DocumentElement.WriteTo(writer);
						}
						catch (Exception ex)
						{
							strError = ex.Message;
							// ѯ���Ƿ����
							goto ERROR1;
						}


						/*
						if (nRet == -1) 
						{
							// ѯ���Ƿ����
							goto ERROR1;
						}
						*/
					}

					// ɾ��
					if (checkBox_export_delete.Checked == true)
					{

						byte [] baOutputTimeStamp1 = null;
						strPath = strOutputPath;	// �õ�ʵ�ʵ�·��

						lRet = channel.DoDeleteRecord(
							strPath,
							baOutputTimeStamp,
							out baOutputTimeStamp1,
							out strError);
						if (lRet == -1) 
						{
							// ѯ���Ƿ����
							goto ERROR1;
						}
					}


					if (bAsc == true) 
					{
						progressBar_main.Value = (int)((nCur-nStart + 1)/ProgressRatio);
					}
					else 
					{
						// ?
						progressBar_main.Value = (int)((nStart-nCur + 1)/ProgressRatio);
					}


					// ���Ѿ������Ľ����ж�
					if (bAsc == true && nCur >= nEnd)
						break;
					if (bAsc == false && nCur <= nEnd)
						break;


				}


				stop.EndLoop();
				stop.Initial(null, "");

				EnableControls(true);

			}

			finally 
			{
				if (writer != null) 
				{
					writer.WriteEndElement();
					writer.WriteEndDocument();
					writer.Close();
					writer = null;
				}

				if (outputfile != null) 
				{
					outputfile.Close();
					outputfile = null;
				}

			}

			END1:
				channel = null;
			if (checkBox_export_delete.Checked == true)
				MessageBox.Show(this, "���ݵ�����ɾ����ɡ�");
			else
				MessageBox.Show(this, "���ݵ�����ɡ�");
			return;

			ERROR1:

				stop.EndLoop();
			stop.Initial(null, "");

			EnableControls(true);


			channel = null;
			MessageBox.Show(this, strError);
			return;
		
		}
#endif


		// return:
		//		-1	error
		//		0	��������
		//		1	ϣ������������OnEnd()
        int DoExportFile(
            string[] dbpaths,
            string strOutputFileName,
            ExportFileType exportType,
            Encoding targetEncoding,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strDeleteStyle = "";
            if (this.checkBox_export_fastMode.Checked == true)
                strDeleteStyle = "fastmode";

            string strInfo = "";    // ������Ϣ������ɺ���ʾ

            FileStream outputfile = null;	// Backup��Xml��ʽ�������Ҫ���
            XmlTextWriter writer = null;   // Xml��ʽ���ʱ��Ҫ���

            bool bAppend = true;

            Debug.Assert(dbpaths != null, "");

            if (dbpaths.Length == 0)
            {
                strError = "��δָ��Դ��...";
                goto ERROR1;
            }


            if (String.IsNullOrEmpty(strOutputFileName) == false)
            {
                // ̽������ļ��Ƿ��Ѿ�����
                FileInfo fi = new FileInfo(strOutputFileName);
                bAppend = true;
                if (fi.Exists == true && fi.Length > 0)
                {
                    if (exportType == ExportFileType.BackupFile
                        || exportType == ExportFileType.ISO2709File)
                    {
                        DialogResult result = MessageBox.Show(this,
                            "�ļ� '" + strOutputFileName + "' �Ѵ��ڣ��Ƿ�׷��?\r\n\r\n--------------------\r\nע��(��)׷��  (��)����  (ȡ��)�жϴ���",
                            "dp2batch",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);
                        if (result == DialogResult.Yes)
                        {
                            bAppend = true;
                        }
                        if (result == DialogResult.No)
                        {
                            bAppend = false;
                        }
                        if (result == DialogResult.Cancel)
                        {
                            strError = "��������...";
                            goto ERROR1;
                        }
                    }
                    else if (exportType == ExportFileType.XmlFile)
                    {
                        DialogResult result = MessageBox.Show(this,
                            "�ļ� '" + strOutputFileName + "' �Ѵ��ڣ��Ƿ񸲸�?\r\n\r\n--------------------\r\nע��(��)����  (��)�жϴ���",
                            "dp2batch",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result != DialogResult.Yes)
                        {
                            strError = "��������...";
                            goto ERROR1;
                        }
                    }


                }

                // ���ļ�
                if (exportType == ExportFileType.BackupFile
                    || exportType == ExportFileType.ISO2709File)
                {
                    outputfile = File.Open(
                        strOutputFileName,
                        FileMode.OpenOrCreate,	// ԭ����Open�������޸�ΪOpenOrCreate����������ʱ�ļ���ϵͳ����Ա�ֶ�����ɾ��(����xml�ļ�����Ȼ����������)������ܹ���Ӧ��������׳�FileNotFoundException�쳣
                        FileAccess.Write,
                        FileShare.ReadWrite);
                }
                else if (exportType == ExportFileType.XmlFile)
                {
                    outputfile = File.Create(
                        strOutputFileName);

                    writer = new XmlTextWriter(outputfile, Encoding.UTF8);
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 4;
                }

            }

            if ((exportType == ExportFileType.BackupFile
                || exportType == ExportFileType.ISO2709File)
                && outputfile != null)
            {
                if (bAppend == true)
                    outputfile.Seek(0, SeekOrigin.End);	// ����׷�ӵ�����
                else
                    outputfile.SetLength(0);
            }

            WriteLog("��ʼ���");

            try
            {

                // string[] dbpaths = textBox_dbPath.Text.Split(new char[] { ';' });

                for (int f = 0; f < dbpaths.Length; f++)
                {
                    string strOneDbPath = dbpaths[f];

                    ResPath respath = new ResPath(strOneDbPath);

                    channel = this.Channels.GetChannel(respath.Url);

                    string strDbName = respath.Path;
                    if (String.IsNullOrEmpty(strInfo) == false)
                        strInfo += "\r\n";
                    strInfo += "" + strDbName;

                    // ʵ�ʴ������β��
                    string strRealStartNo = "";
                    string strRealEndNo = "";

                    /*
                    DialogResult result;
                    if (checkBox_export_delete.Checked == true)
                    {
                        result = MessageBox.Show(this,
                            "ȷʵҪɾ�� '" + respath.Path + "' ��ָ����Χ�ļ�¼?\r\n\r\n---------\r\n(��)ɾ�� (��)����������",
                            "dp2batch",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result != DialogResult.Yes)
                            continue;
                    }
                     * 
                     * */


                    //channel = this.Channels.GetChannel(respath.Url);

                    //string strDbName = respath.Path;

                    // ���Ϊ������
                    if (dbpaths.Length > 0)
                    {
                        // ���Ϊȫѡ
                        if (this.radioButton_all.Checked == true)
                        {
                            // �ָ�Ϊ���Χ
                            this.textBox_startNo.Text = "1";
                            this.textBox_endNo.Text = "9999999999";
                        }

                        // У����ֹ��
                        if (checkBox_verifyNumber.Checked == true)
                        {
                            nRet = VerifyRange(channel,
                                strDbName,
                                out strError);
                            if (nRet == -1)
                                MessageBox.Show(this, strError);

                            if (nRet == 0)
                            {
                                // �����޼�¼
                                AutoCloseMessageBox.Show(this, "���ݿ� " + strDbName + " ���޼�¼��");
                                strInfo += "(�޼�¼)";
                                WriteLog("�������ݿ� " + strDbName + " ���޼�¼");
                                continue;
                            }
                        }
                        else
                        {
                            if (this.textBox_startNo.Text == "")
                            {
                                strError = "��δָ����ʼ��";
                                goto ERROR1;
                            }
                            if (this.textBox_endNo.Text == "")
                            {
                                strError = "��δָ��������";
                                goto ERROR1;
                            }
                        }
                    }

                    string strOutputStartNo = "";
                    string strOutputEndNo = "";
                    // ��Ȼ���治��У����ֹ�ţ�����ҲҪУ�飬Ϊ�����úý�����
                    if (checkBox_verifyNumber.Checked == false)
                    {
                        // У����ֹ��
                        // return:
                        //      0   �����ڼ�¼
                        //      1   ���ڼ�¼
                        nRet = VerifyRange(channel,
                            strDbName,
                            this.textBox_startNo.Text,
                            this.textBox_endNo.Text,
                            out strOutputStartNo,
                            out strOutputEndNo,
                            out strError);
                    }

                    //try
                    //{

                    Int64 nStart = 0;
                    Int64 nEnd = 0;
                    Int64 nCur = 0;
                    bool bAsc = true;

                    bAsc = GetDirection(
                        this.textBox_startNo.Text,
                        this.textBox_endNo.Text,
                        out nStart,
                        out nEnd);

                    // ̽�⵽�ĺ���
                    long nOutputEnd = 0;
                    long nOutputStart = 0;
                    if (checkBox_verifyNumber.Checked == false)
                    {
                        GetDirection(
                            strOutputStartNo,
                            strOutputEndNo,
                            out nOutputStart,
                            out nOutputEnd);
                    }

                    // ���ý�������Χ
                    if (checkBox_verifyNumber.Checked == true)
                    {

                        Int64 nMax = nEnd - nStart;
                        if (nMax < 0)
                            nMax *= -1;
                        nMax++;

                        /*
                        ProgressRatio = nMax / 10000;
                        if (ProgressRatio < 1.0)
                            ProgressRatio = 1.0;

                        progressBar_main.Minimum = 0;
                        progressBar_main.Maximum = (int)(nMax / ProgressRatio);
                        progressBar_main.Value = 0;
                         * */
                        stop.SetProgressRange(0, nMax);
                    }
                    else
                    {
                        Int64 nMax = nOutputEnd - nOutputStart;
                        if (nMax < 0)
                            nMax *= -1;
                        nMax++;
                        stop.SetProgressRange(0, nMax);
                    }


                    bool bFirst = true;	// �Ƿ�Ϊ��һ��ȡ��¼

                    string strID = this.textBox_startNo.Text;

                    stop.OnStop += new StopEventHandler(this.DoStop);
                    stop.Initial("���ڵ�������");
                    stop.BeginLoop();

                    EnableControls(false);

                    if (exportType == ExportFileType.XmlFile
                        && writer != null)
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("dprms", "collection", DpNs.dprms);
                        //writer.WriteStartElement("collection");
                        //writer.WriteAttributeString("xmlns:marc",
                        //	"http://www.loc.gov/MARC21/slim");

                    }

                    WriteLog("��ʼ������ݿ� '"+strDbName+"' �ڵ����ݼ�¼");

                    m_nRecordCount = 0;
                    // ѭ��
                    for (; ; )
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop.State != 0)
                        {
                            WriteLog("�򿪶Ի��� 'ȷʵҪ�жϵ�ǰ���������?'");
                            DialogResult result = MessageBox.Show(this,
                                "ȷʵҪ�жϵ�ǰ���������?",
                                "dp2batch",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button2);
                            WriteLog("�رնԻ��� 'ȷʵҪ�жϵ�ǰ���������?'");
                            if (result == DialogResult.Yes)
                            {
                                strError = "�û��ж�";
                                goto ERROR1;
                            }
                            else
                            {
                                stop.Continue();
                            }
                        }

                        string strDirectionComment = "";
                        string strStyle = "";
                        if (outputfile != null)
                            strStyle = "data,content,timestamp,outputpath";
                        else
                            strStyle = "timestamp,outputpath";	// �Ż�

                        if (bFirst == true)
                        {
                            strStyle += "";
                        }
                        else
                        {
                            if (bAsc == true)
                            {
                                strStyle += ",next";
                                strDirectionComment = "�ĺ�һ����¼";
                            }
                            else
                            {
                                strStyle += ",prev";
                                strDirectionComment = "��ǰһ����¼";
                            }
                        }


                        string strPath = strDbName + "/" + strID;
                        string strXmlBody = "";
                        string strMetaData = "";
                        byte[] baOutputTimeStamp = null;
                        string strOutputPath = "";

                        bool bFoundRecord = false;

                        bool bNeedRetry = true;

                    REDO_GETRES:
                        // �����Դ
                        // return:
                        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
                        //		0	�ɹ�
                        long lRet = channel.GetRes(strPath,
                            strStyle,
                            out strXmlBody,
                            out strMetaData,
                            out baOutputTimeStamp,
                            out strOutputPath,
                            out strError);


                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            {
                                if (bFirst == true)
                                {
                                    if (checkBox_forceLoop.Checked == true)
                                    {
                                        string strText = "��¼ " + strID + strDirectionComment + " �����ڡ�\r\n\r\n�� ȷ�� ������";
                                        WriteLog("�򿪶Ի��� '"+strText.Replace("\r\n", "\\n")+"'");
                                        AutoCloseMessageBox.Show(this, strText);
                                        WriteLog("�رնԻ��� '" + strText.Replace("\r\n", "\\n") + "'");

                                        bFirst = false;
                                        goto CONTINUE;
                                    }
                                    else
                                    {
                                        // �����Ҫǿ��ѭ������ʱҲ���ܽ�������������û���Ϊ���ݿ��������û������
                                        string strText = "��Ϊ���ݿ� " + strDbName + " ָ�����׼�¼ " + strID + strDirectionComment + " �����ڡ�\r\n\r\n(ע��Ϊ������ִ���ʾ�����ڲ���ǰ��ѡ��У׼��βID��)\r\n\r\n�� ȷ�� ���������...";
                                        WriteLog("�򿪶Ի��� '" + strText.Replace("\r\n", "\\n") + "'");
                                        AutoCloseMessageBox.Show(this, strText);
                                        WriteLog("�رնԻ��� '" + strText.Replace("\r\n", "\\n") + "'");

                                        bFirst = false;
                                        goto CONTINUE;
                                    }
                                }
                                else
                                {
                                    Debug.Assert(bFirst == false, "");

                                    if (bFirst == true)
                                    {
                                        strError = "��¼ " + strID + strDirectionComment + " �����ڡ����������";
                                    }
                                    else
                                    {
                                        if (bAsc == true)
                                            strError = "��¼ " + strID + " ����ĩһ����¼�����������";
                                        else
                                            strError = "��¼ " + strID + " ����ǰһ����¼�����������";
                                    }

                                    if (dbpaths.Length > 1)
                                        break;  // ������������������ѭ��
                                    else
                                    {
                                        bNeedRetry = false; // ���������Ҳû�б�Ҫ�������ԶԻ���

                                        WriteLog("�򿪶Ի��� '" + strError.Replace("\r\n", "\\n") + "'");
                                        MessageBox.Show(this, strError);
                                        WriteLog("�رնԻ��� '" + strError.Replace("\r\n", "\\n") + "'");
                                        break;
                                    }
                                }

                            }
                            else if (channel.ErrorCode == ChannelErrorCode.EmptyRecord)
                            {
                                bFirst = false;
                                bFoundRecord = false;
                                // ��id��������
                                strID = ResPath.GetRecordId(strOutputPath);
                                goto CONTINUE;

                            }

                            // ��������
                            if (bNeedRetry == true)
                            {
                                string strText = "��ȡ��¼ '" + strPath + "' (style='" + strStyle + "')ʱ���ִ���: " + strError + "\r\n\r\n���ԣ������жϵ�ǰ���������?\r\n(Retry ���ԣ�Cancel �ж�������)";
                                WriteLog("�򿪶Ի��� '" + strText.Replace("\r\n", "\\n") + "'");
                                DialogResult redo_result = MessageBox.Show(this,
                                    strText,
                                    "dp2batch",
                                    MessageBoxButtons.RetryCancel,
                                    MessageBoxIcon.Question,
                                    MessageBoxDefaultButton.Button1);
                                WriteLog("�رնԻ��� '" + strText.Replace("\r\n", "\\n") + "'");
                                if (redo_result == DialogResult.Cancel)
                                    goto ERROR1;
                                goto
                                    REDO_GETRES;
                            }
                            else
                            {
                                goto ERROR1;
                            }
                        }

                        // 2008/11/9 new add
                        if (String.IsNullOrEmpty(strXmlBody) == true)
                        {
                            bFirst = false;
                            bFoundRecord = false;
                            // ��id��������
                            strID = ResPath.GetRecordId(strOutputPath);
                            goto CONTINUE;
                        }

                        bFirst = false;

                        bFoundRecord = true;

                        // ��id��������
                        strID = ResPath.GetRecordId(strOutputPath);
                        stop.SetMessage("�ѵ�����¼ " + strOutputPath + "  " + m_nRecordCount.ToString());

                        if (String.IsNullOrEmpty(strRealStartNo) == true)
                        {
                            strRealStartNo = strID;
                        }

                        strRealEndNo = strID;

                    CONTINUE:

                        // �Ƿ񳬹�ѭ����Χ
                        try
                        {
                            nCur = Convert.ToInt64(strID);
                        }
                        catch
                        {
                            // ???
                            nCur = 0;
                        }

                        if (checkBox_verifyNumber.Checked == false)
                        {
                            // �����ǰ��¼����ͻ��Ԥ�Ƶ�ͷ����β��
                            if (nCur > nOutputEnd
                                || nCur < nOutputStart)
                            {
                                if (nCur > nOutputEnd)
                                    nOutputEnd = nCur;

                                if (nCur < nOutputStart)
                                    nOutputStart = nCur;

                                // ���¼�������ý�����
                                long nMax = nOutputEnd - nOutputStart;
                                if (nMax < 0)
                                    nMax *= -1;
                                nMax++;

                                stop.SetProgressRange(0, nMax);
                            }
                        }

                        if (bAsc == true && nCur > nEnd)
                            break;
                        if (bAsc == false && nCur < nEnd)
                            break;

                        string strMarc = "";

                        // ��Xmlת��ΪMARC
                        if (exportType == ExportFileType.ISO2709File
                            && bFoundRecord == true)    // 2008/11/13 new add
                        {
                            nRet = GetMarc(strXmlBody,
                                out strMarc,
                                out strError);
                            if (nRet == -1)
                            {
                                strError = "��¼ " + strOutputPath + " �ڽ�XML��ʽת��ΪMARCʱ����: " + strError;
                                goto ERROR1;
                            }
                        }

                        if (this.MarcFilter != null)
                        {
                            // ����filter�е�Record��ض���
                            // TODO: �п���strMarcΪ��Ӵ����Ҫ����һ��
                            nRet = MarcFilter.DoRecord(
                                null,
                                strMarc,
                                m_nRecordCount,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                        }

                        // ����Script��Outputing()����
                        if (bFoundRecord == true && this.AssemblyMain != null)
                        {
                            // ��Щ����Ҫ�ȳ�ʼ��,��Ϊfilter��������õ���ЩBatch��Ա.
                            batchObj.XmlRecord = strXmlBody;

                            batchObj.MarcSyntax = this.CurMarcSyntax;

                            batchObj.MarcRecord = strMarc;	// MARC��¼��
                            batchObj.MarcRecordChanged = false;	// Ϊ����Script����׼����ʼ״̬

                            batchObj.SearchPanel.ServerUrl = channel.Url;
                            batchObj.ServerUrl = channel.Url;
                            batchObj.RecPath = strOutputPath;	// ��¼·��
                            batchObj.RecIndex = m_nRecordCount;	// ��ǰ��¼��һ���е����
                            batchObj.TimeStamp = baOutputTimeStamp;


                            BatchEventArgs args = new BatchEventArgs();

                            batchObj.Outputing(this, args);
                            /*
                            if (args.Continue == ContinueType.SkipMiddle)
                                goto CONTINUEDBS;
                            if (args.Continue == ContinueType.SkipBeginMiddle)
                                goto CONTINUEDBS;
                            */
                            if (args.Continue == ContinueType.SkipAll)
                                goto CONTINUEDBS;

                            // �۲����������MARC��¼�Ƿ񱻸ı�
                            if (batchObj.MarcRecordChanged == true)
                                strMarc = batchObj.MarcRecord;

                            // �۲�XML��¼�Ƿ񱻸ı�
                            if (batchObj.XmlRecordChanged == true)
                                strXmlBody = batchObj.XmlRecord;

                        }


                        if (bFoundRecord == true
                            && outputfile != null)
                        {
                            if (exportType == ExportFileType.BackupFile)
                            {
                                // д����
                                nRet = WriteRecordToBackupFile(
                                    outputfile,
                                    strDbName,
                                    strID,
                                    strMetaData,
                                    strXmlBody,
                                    baOutputTimeStamp,
                                    out strError);
                                if (nRet == -1)
                                {
                                    // ѯ���Ƿ����
                                    goto ERROR1;
                                }
                            }
                            else if (exportType == ExportFileType.ISO2709File)
                            {
                                // д����
                                nRet = WriteRecordToISO2709File(
                                    outputfile,
                                    strDbName,
                                    strID,
                                    strMarc,
                                    baOutputTimeStamp,
                                    targetEncoding,
                                    this.OutputCrLf,
                                    this.AddG01,
                                    this.Remove998,
                                    out strError);
                                if (nRet == -1)
                                {
                                    // ѯ���Ƿ����
                                    goto ERROR1;
                                }
                            }
                            else if (exportType == ExportFileType.XmlFile)
                            {
                                XmlDocument dom = new XmlDocument();

                                try
                                {
                                    dom.LoadXml(strXmlBody);

                                    ResPath respathtemp = new ResPath();
                                    respathtemp.Url = channel.Url;
                                    respathtemp.Path = strOutputPath;


                                    // DomUtil.SetAttr(dom.DocumentElement, "xmlns:dprms", DpNs.dprms);
                                    // ����Ԫ�����ü�������
                                    DomUtil.SetAttr(dom.DocumentElement, "path", DpNs.dprms, respathtemp.FullPath);
                                    DomUtil.SetAttr(dom.DocumentElement, "timestamp", DpNs.dprms, ByteArray.GetHexTimeStampString(baOutputTimeStamp));

                                    // DomUtil.SetAttr(dom.DocumentElement, "xmlns:marc", null);
                                    dom.DocumentElement.WriteTo(writer);
                                }
                                catch (Exception ex)
                                {
                                    strError = ex.Message;
                                    // ѯ���Ƿ����
                                    goto ERROR1;
                                }

                            }
                        }

                        // ɾ��
                        if (checkBox_export_delete.Checked == true)
                        {

                            byte[] baOutputTimeStamp1 = null;
                            strPath = strOutputPath;	// �õ�ʵ�ʵ�·��
                            lRet = channel.DoDeleteRes(
                                strPath,
                                baOutputTimeStamp,
                                strDeleteStyle,
                                out baOutputTimeStamp1,
                                out strError);
                            if (lRet == -1)
                            {
                                // ѯ���Ƿ����
                                goto ERROR1;
                            }
                            stop.SetMessage("��ɾ����¼" + strPath + "  " + m_nRecordCount.ToString());
                        }

                        if (bFoundRecord == true)
                            m_nRecordCount++;


                        if (bAsc == true)
                        {
                            //progressBar_main.Value = (int)((nCur - nStart + 1) / ProgressRatio);
                            stop.SetProgressValue(nCur - nStart + 1);
                        }
                        else
                        {
                            // ?
                            // progressBar_main.Value = (int)((nStart - nCur + 1) / ProgressRatio);
                            stop.SetProgressValue(nStart - nCur + 1);
                        }


                        // ���Ѿ������Ľ����ж�
                        if (bAsc == true && nCur >= nEnd)
                            break;
                        if (bAsc == false && nCur <= nEnd)
                            break;


                    } // end of for one database

                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");

                    EnableControls(true);

                //}

            CONTINUEDBS:
                    strInfo += " : " + m_nRecordCount.ToString() + "�� (ID " + strRealStartNo + "-" + strRealEndNo + ")";

                }   // end of dbpaths loop


            }   // end of try
            finally
            {
                if (writer != null)
                {
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Close();
                    writer = null;
                }

                if (outputfile != null)
                {
                    outputfile.Close();
                    outputfile = null;
                }

            }

            // END1:
            channel = null;

            if (checkBox_export_delete.Checked == true)
                strError = "���ݵ�����ɾ����ɡ�\r\n---\r\n" + strInfo;
            else
                strError = "���ݵ�����ɡ�\r\n---\r\n" + strInfo;

            WriteLog("�������");

            return 0;
        ERROR1:
            stop.EndLoop();
            stop.OnStop -= new StopEventHandler(this.DoStop);
            stop.Initial("");

            EnableControls(true);
            channel = null;
            return -1;
        }

		// ��Xmlת��ΪMARC
		// �ɹ�C#�ű�����
		public int GetMarc(string strXmlBody,
			out string strMarc,
			out string strError)
		{
			string strOutMarcSyntax = "";
			strMarc = "";

			// ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
			// parameters:
			//		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
			//		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
			//		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
			int nRet = MarcUtil.Xml2Marc(strXmlBody,
				true,  // true �� false Ҫ���� // false,
				this.CurMarcSyntax,
				out strOutMarcSyntax,
				out strMarc,
				out strError);
			if (nRet == -1)
				return -1;

			return 0;
		}

        // ��MARC��¼�м���һ��-01�ֶ�
        // ���ǲ����ڵ�һ���ֶ�
        static int AddG01ToMarc(ref string strMARC,
            string strFieldContent,
            out string strError)
        {
            strError = "";

            if (strMARC.Length < 24)
            {
                strMARC = strMARC.PadRight(24, '*');
                strMARC += "-01" + strFieldContent + new string(MarcUtil.FLDEND, 1);
                return 1;
            }

            strMARC = strMARC.Insert(24, "-01" + strFieldContent + new string(MarcUtil.FLDEND, 1));

            /*
        // ���ԭ�м�¼�д���-01�ֶΣ����һ��-01�ֶν�������
            // return:
            //		-1	����
            //		0	û���ҵ�ָ�����ֶΣ���˽�strField���ݲ��뵽�ʵ�λ���ˡ�
            //		1	�ҵ���ָ�����ֶΣ�����Ҳ�ɹ���strField�滻���ˡ�
            int nRet = MarcUtil.ReplaceField(
                ref strMARC,
                "-01",
                0,
                "-01" + strFieldContent);
            if (nRet == -1)
            {
                strError = "ReplaceField() error";
                return -1;
            }*/

            return 1;
        }

        // ȥ��MARC��¼�е�����-01�ֶ�
        // return:
        //      -1  error
        //      0   not changed
        //      1   changed
        static int RemoveG01FromMarc(ref string strMARC,
            out string strError)
        {
            strError = "";

            if (strMARC.Length <=24)
                return 0;

            bool bChanged = false;

            for (; ; )
            {
                string strField = "";
                string strNextFieldName = "";
                // return:
                //		-1	����
                //		0	��ָ�����ֶ�û���ҵ�
                //		1	�ҵ����ҵ����ֶη�����strField������
                int nRet = MarcUtil.GetField(strMARC,
                    "-01",
                    0,
                    out strField,
                    out strNextFieldName);
                if (nRet == -1)
                {
                    strError = "GetField() error";
                    return -1;
                }

                if (nRet == 0)
                    break;

                // return:
                //		-1	����
                //		0	û���ҵ�ָ�����ֶΣ���˽�strField���ݲ��뵽�ʵ�λ���ˡ�
                //		1	�ҵ���ָ�����ֶΣ�����Ҳ�ɹ���strField�滻���ˡ�
                nRet = MarcUtil.ReplaceField(
                    ref strMARC,
                    "-01",
                    0,
                    null);
                if (nRet == -1)
                {
                    strError = "ReplaceField() error";
                    return -1;
                }

                bChanged = true;
            }

            if (bChanged == true)
                return 1;

            return 0;
        }


		// ����¼д��ISO2709�ļ�
		int WriteRecordToISO2709File(
			Stream outputfile,
			string strDbName,
			string strID,
			string strMarc,
			byte [] body_timestamp,
			Encoding targetEncoding,
			bool bOutputCrLf,
            bool bAddG01,
            bool bRemove998,
			out string strError)
		{

			int nRet = 0;

			string strPath = strDbName + "/" + strID;

			long lStart = outputfile.Position;	// ������ʼλ��


			ResPath respath = new ResPath();
			respath.Url = channel.Url;
			respath.Path = strPath;

            
                    // ȥ��MARC��¼�е�����-01�ֶ�
        // return:
        //      -1  error
        //      0   not changed
        //      1   changed
            nRet = RemoveG01FromMarc(ref strMarc,
                out strError);
            if (nRet == -1)
                return -1;

            if (bAddG01 == true)
            {
                string strDt1000Path = "/" + strDbName + "/ctlno/" + strID.PadLeft(10, '0');
                string strTimestamp = ByteArray.GetHexTimeStampString(body_timestamp);

                nRet = AddG01ToMarc(ref strMarc,
                    strDt1000Path + "|" + strTimestamp,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            if (bRemove998 == true)
            {
                MarcRecord record = new MarcRecord(strMarc);
                record.select("field[@name='998']").detach();
                strMarc = record.Text;
            }

			byte [] baResult = null;
            // ��MARC���ڸ�ʽת��ΪISO2709��ʽ
            // parameters:
            //		nMARCType	[in]MARC��ʽ���͡�0ΪUNIMARC 1ΪUSMARC
            //		strSourceMARC		[in]���ڸ�ʽMARC��¼��
            //		targetEncoding	[in]���ISO2709�ı��뷽ʽΪ UTF8 codepage-936�ȵ�
            //		baResult	[out]�����ISO2709��¼���ַ�����nCharset�������ơ�
            //					ע�⣬������ĩβ������0�ַ���
            nRet = MarcUtil.CvtJineiToISO2709(
                strMarc,
                this.CurMarcSyntax,
                targetEncoding,
                out baResult,
                out strError);

			if (nRet == -1)
				return -1;

			outputfile.Write(baResult, 0, baResult.Length);

			if (bOutputCrLf == true)
			{
				baResult = new byte [2];
				baResult[0] = (byte)'\r';
				baResult[1] = (byte)'\n';
				outputfile.Write(baResult, 0, 2);
			}

			
			return 0;

			/*
			ERROR1:
				return -1;
			*/
		}

		// ������¼�������Դд�뱸���ļ�
		int WriteRecordToBackupFile(
			Stream outputfile,
			string strDbName,
			string strID,
			string strMetaData,
			string strXmlBody,
			byte [] body_timestamp,
			out string strError)
		{

            Debug.Assert(String.IsNullOrEmpty(strXmlBody) == false, "strXmlBody����Ϊ��");

			string strPath = strDbName + "/" + strID;

			long lStart = outputfile.Position;	// ������ʼλ��

			byte [] length = new byte[8];

			outputfile.Write(length, 0, 8);	// ��ʱд������,ռ�ݼ�¼�ܳ���λ��

			ResPath respath = new ResPath();
			respath.Url = channel.Url;
			respath.Path = strPath;

			// �ӹ�Ԫ����
            ExportUtil.ChangeMetaData(ref strMetaData,
				null,
				null,
				null,
				null,
				respath.FullPath,
				ByteArray.GetHexTimeStampString(body_timestamp));   // 2005/6/11

			// ��backup�ļ��б����һ�� res
			long lRet = Backup.WriteFirstResToBackupFile(
				outputfile,
				strMetaData,
				strXmlBody);

			// ����

			string [] ids = null;

			// �õ�Xml��¼������<file>Ԫ�ص�id����ֵ
			int nRet = ExportUtil.GetFileIds(strXmlBody,
				out ids,
				out strError);
			if (nRet == -1) 
			{
				outputfile.SetLength(lStart);	// �ѱ���׷��д���ȫ��ȥ��
				strError = "GetFileIds()�����޷���� XML ��¼�е� <dprms:file>Ԫ�ص� id ���ԣ� ��˱����¼ʧ�ܣ�ԭ��: "+ strError;
				goto ERROR1;
			}


			nRet = WriteResToBackupFile(
				this,
				outputfile,
				respath.Path,
				ids,
				channel,
				stop,
				out strError);
			if (nRet == -1) 
			{
				outputfile.SetLength(lStart);	// �ѱ���׷��д���ȫ��ȥ��
				strError = "WriteResToBackupFile()������˱����¼ʧ�ܣ�ԭ��: "+ strError;
				goto ERROR1;
			}

			///


			// д���ܳ���
			long lTotalLength = outputfile.Position - lStart - 8;
			byte[] data = BitConverter.GetBytes(lTotalLength);

			outputfile.Seek(lStart, SeekOrigin.Begin);
			outputfile.Write(data, 0, 8);
			outputfile.Seek(lTotalLength, SeekOrigin.Current);

			return 0;

			ERROR1:
				return -1;
		}

		// ������Դ�����浽�����ļ�
		public static int WriteResToBackupFile(
			IWin32Window owner,
			Stream outputfile,
			string strXmlRecPath,
			string [] res_ids,
            RmsChannel channel,
			DigitalPlatform.Stop stop,
			out string strError)
		{
			strError = "";


			long lRet;

			for(int i=0;i<res_ids.Length;i++)
			{
				Application.DoEvents();	// ���ý������Ȩ

				if (stop.State != 0)
				{
					DialogResult result = MessageBox.Show(owner,
						"ȷʵҪ�жϵ�ǰ���������?",
						"dp2batch",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question,
						MessageBoxDefaultButton.Button2);
					if (result == DialogResult.Yes)
					{
						strError = "�û��ж�";
						return -1;
					}
					else 
					{
						stop.Continue();
					}
				}

				string strID = res_ids[i].Trim();

				if (strID == "")
					continue;

				string strResPath = strXmlRecPath + "/object/" + strID;

				string strMetaData;

				if (stop != null)
					stop.SetMessage("�������� " + strResPath);

				long lResStart = 0;
				// дres��ͷ��
				// �������Ԥ��ȷ֪����res�ĳ��ȣ����������һ��lTotalLengthֵ���ñ�������
				// ������Ҫ�����º��������ص�lStart��������EndWriteResToBackupFile()��
				// �����Ԥ��ȷ֪����res�ĳ��ȣ�����󲻱ص���EndWriteResToBackupFile()
				lRet = Backup.BeginWriteResToBackupFile(
					outputfile,
					0,	// δ֪
					out lResStart);

				byte [] baOutputTimeStamp = null;
				string strOutputPath;

            REDO_GETRES:
				lRet = channel.GetRes(strResPath,
					(Stream)null,	// ���ⲻ��ȡ��Դ��
					stop,
					"metadata,timestamp,outputpath",
					null,
					out strMetaData,	// ����Ҫ���metadata
					out baOutputTimeStamp,
					out strOutputPath,
					out strError);
                if (lRet == -1)
                {
                    // TODO: ��������
                    DialogResult redo_result = MessageBox.Show(owner,
                        "��ȡ��¼ '" + strResPath + "' ʱ���ִ���: " + strError + "\r\n\r\n���ԣ������жϵ�ǰ���������?\r\n(Retry ���ԣ�Cancel �ж�������)",
                        "dp2batch",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    if (redo_result == DialogResult.Cancel)
                        return -1;
                    goto
                        REDO_GETRES;
                }

				byte [] timestamp = baOutputTimeStamp;

				ResPath respath = new ResPath();
				respath.Url = channel.Url;
				respath.Path = strOutputPath;	// strResPath;

				// strMetaData��Ҫ������Դid?
                ExportUtil.ChangeMetaData(ref strMetaData,
					strID,
					null,
					null,
					null,
					respath.FullPath,
					ByteArray.GetHexTimeStampString(baOutputTimeStamp));


				lRet = Backup.WriteResMetadataToBackupFile(outputfile,
					strMetaData);
				if (lRet == -1)
					return -1;

				long lBodyStart = 0;
				// дres body��ͷ��
				// �������Ԥ��ȷ֪body�ĳ��ȣ����������һ��lBodyLengthֵ���ñ�������
				// ������Ҫ�����º��������ص�lBodyStart��������EndWriteResBodyToBackupFile()��
				// �����Ԥ��ȷ֪body�ĳ��ȣ�����󲻱ص���EndWriteResBodyToBackupFile()
				lRet = Backup.BeginWriteResBodyToBackupFile(
					outputfile,
					0, // δ֪
					out lBodyStart);
				if (lRet == -1)
					return -1;

				if (stop != null)
					stop.SetMessage("�������� " + strResPath + " ��������");

            REDO_GETRES_1:
				lRet = channel.GetRes(strResPath,
					outputfile,
					stop,
					"content,data,timestamp", //"content,data,timestamp"
					timestamp,
					out strMetaData,
					out baOutputTimeStamp,
					out strOutputPath,
					out strError);
				if (lRet == -1) 
				{
					if (channel.ErrorCode == ChannelErrorCode.EmptyRecord)
					{
						// �ռ�¼
					}
					else 
					{
                        // TODO: ��������
                        DialogResult redo_result = MessageBox.Show(owner,
                            "��ȡ��¼ '" + strResPath + "' ʱ���ִ���: " + strError + "\r\n\r\n���ԣ������жϵ�ǰ���������?\r\n(Retry ���ԣ�Cancel �ж�������)",
                            "dp2batch",
                            MessageBoxButtons.RetryCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);
                        if (redo_result == DialogResult.Cancel)
                            return -1;
                        goto
                            REDO_GETRES_1;
					}
				}

				long lBodyLength = outputfile.Position - lBodyStart - 8;
				// res body��β
				lRet = Backup.EndWriteResBodyToBackupFile(
					outputfile,
					lBodyLength,
					lBodyStart);
				if (lRet == -1)
					return -1;

				long lTotalLength = outputfile.Position - lResStart - 8;
				lRet = Backup.EndWriteResToBackupFile(
					outputfile,
					lTotalLength,
					lResStart);
				if (lRet == -1)
					return -1;

			}

			/*
			if (stop != null)
				stop.SetMessage("������Դ�������ļ�ȫ�����");
			*/

			return 0;
		}


        // return:
        //		true	��ʼ��ΪС��
        //		false	��ʼ��Ϊ���
        static bool GetDirection(
            string strStartNo,
            string strEndNo,
            out Int64 nStart,
            out Int64 nEnd)
        {
            bool bAsc = true;

            nStart = 0;
            nEnd = 9999999999;

            try
            {
                nStart = Convert.ToInt64(strStartNo);
            }
            catch
            {
            }

            try
            {
                nEnd = Convert.ToInt64(strEndNo);
            }
            catch
            {
            }


            if (nStart > nEnd)
                bAsc = false;
            else
                bAsc = true;

            return bAsc;
        }

#if NOOOOOOOOOOOO
		// return:
		//		true	��ʼ��ΪС��
		//		false	��ʼ��Ϊ���
		bool GetDirection(out Int64 nStart,
			out Int64 nEnd)
		{
			bool bAsc = true;

			nStart = 0;
			nEnd = 9999999999;
			
			try 
			{
				nStart = Convert.ToInt64(textBox_startNo.Text);
			}
			catch 
			{
			}
				
			try 
			{
				nEnd = Convert.ToInt64(textBox_endNo.Text);
			}
			catch 
			{
			}


			if (nStart > nEnd)
				bAsc = false;
			else
				bAsc = true;

			return bAsc;
		}
#endif

        // У����ֹ��
        // return:
        //      0   �����ڼ�¼
        //      1   ���ڼ�¼
        int VerifyRange(RmsChannel channel,
            string strDbName,
            string strInputStartNo,
            string strInputEndNo,
            out string strOutputStartNo,
            out string strOutputEndNo,
            out string strError)
        {
            strError = "";
            strOutputStartNo = "";
            strOutputEndNo = "";

            bool bStartNotFound = false;
            bool bEndNotFound = false;

            // ������������Ϊ�գ���ٶ�Ϊ��ȫ����Χ��
            if (strInputStartNo == "")
                strInputStartNo = "1";

            if (strInputEndNo == "")
                strInputEndNo = "9999999999";


            bool bAsc = true;

            Int64 nStart = 0;
            Int64 nEnd = 9999999999;


            try
            {
                nStart = Convert.ToInt64(strInputStartNo);
            }
            catch
            {
            }


            try
            {
                nEnd = Convert.ToInt64(strInputEndNo);
            }
            catch
            {
            }


            if (nStart > nEnd)
                bAsc = false;
            else
                bAsc = true;

            string strPath = strDbName + "/" + strInputStartNo;
            string strStyle = "outputpath";

            if (bAsc == true)
                strStyle += ",next,myself";
            else
                strStyle += ",prev,myself";

            string strResult;
            string strMetaData;
            byte[] baOutputTimeStamp;
            string strOutputPath;

            string strError0 = "";

            string strStartID = "";
            string strEndID = "";

            // �����Դ
            // return:
            //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
            //		0	�ɹ�
            long lRet = channel.GetRes(strPath,
                strStyle,
                out strResult,
                out strMetaData,
                out baOutputTimeStamp,
                out strOutputPath,
                out strError0);
            if (lRet == -1)
            {
                if (channel.ErrorCode == ChannelErrorCode.NotFound)
                {
                    strStartID = strInputStartNo;
                    bStartNotFound = true;
                }
                else
                    strError += "У��startnoʱ���� " + strError0 + " ";

            }
            else
            {
                // ȡ�÷��ص�id
                strStartID = ResPath.GetRecordId(strOutputPath);
            }

            if (strStartID == "")
            {
                strError = "strStartIDΪ��..." + (string.IsNullOrEmpty(strError) == false? " : " + strError : "");
                return -1;
            }

            strPath = strDbName + "/" + strInputEndNo;

            strStyle = "outputpath";
            if (bAsc == true)
                strStyle += ",prev,myself";
            else
                strStyle += ",next,myself";

            // �����Դ
            // return:
            //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
            //		0	�ɹ�
            lRet = channel.GetRes(strPath,
                strStyle,
                out strResult,
                out strMetaData,
                out baOutputTimeStamp,
                out strOutputPath,
                out strError0);
            if (lRet == -1)
            {

                if (channel.ErrorCode == ChannelErrorCode.NotFound)
                {
                    strEndID = strInputEndNo;
                    bEndNotFound = true;
                }
                else
                {
                    strError += "У��endnoʱ���� " + strError0 + " ";
                }

            }
            else
            {
                // ȡ�÷��ص�id
                strEndID = ResPath.GetRecordId(strOutputPath);
            }

            if (strEndID == "")
            {
                strError = "strEndIDΪ��..." + (string.IsNullOrEmpty(strError) == false ? " : " + strError : ""); ;
                return -1;
            }

            ///
            bool bSkip = false;

            Int64 nTemp = 0;
            try
            {
                nTemp = Convert.ToInt64(strStartID);
            }
            catch
            {
                strError = "strStartIDֵ '" + strStartID + "' ��������...";
                return -1;
            }

            if (bAsc == true)
            {
                if (nTemp > nEnd)
                {
                    bSkip = true;
                }
            }
            else
            {
                if (nTemp < nEnd)
                {
                    bSkip = true;
                }
            }

            if (bSkip == false)
            {
                strOutputStartNo = strStartID;
            }


            ///

            bSkip = false;

            try
            {
                nTemp = Convert.ToInt64(strEndID);
            }
            catch
            {
                strError = "strEndIDֵ '" + strEndID + "' ��������...";
                return -1;
            }
            if (bAsc == true)
            {
                if (nTemp < nStart)
                {
                    bSkip = true;
                }
            }
            else
            {
                if (nTemp > nStart)
                {
                    bSkip = true;
                }
            }

            if (bSkip == false)
            {
                strOutputEndNo = strEndID;
            }

            if (bStartNotFound == true && bEndNotFound == true)
                return 0;

            return 1;
        }

        		// У����ֹ��
        // return:
        //      0   �����ڼ�¼
        //      1   ���ڼ�¼
        int VerifyRange(RmsChannel channel,
            string strDbName,
            out string strError)
        {
            strError = "";

            string strOutputStartNo = "";
            string strOutputEndNo = "";
            int nRet = VerifyRange(channel,
                strDbName,
                this.textBox_startNo.Text,
                this.textBox_endNo.Text,
                out strOutputStartNo,
                out strOutputEndNo,
                out strError);
            if (nRet == -1)
                return -1;

            this.textBox_startNo.Text = strOutputStartNo;
            this.textBox_endNo.Text = strOutputEndNo;

            return nRet;
        }

#if NOOOOOOOOOOOOOOOOOOOOOOOOOO
		// У����ֹ��
        // return:
        //      0   �����ڼ�¼
        //      1   ���ڼ�¼
        int VerifyRange(RmsChannel channel,
			string strDbName,
			out string strError)
		{
            bool bStartNotFound = false;
            bool bEndNotFound = false;

			strError = "";

			// ���edit��Ϊ�գ���ٶ�Ϊ��ȫ����Χ��
			if (textBox_startNo.Text == "")
				textBox_startNo.Text = "1";

			if (textBox_endNo.Text == "")
				textBox_endNo.Text = "9999999999";


			bool bAsc = true;

			Int64 nStart = 0;
			Int64 nEnd = 9999999999;
			
			
			try 
			{
				nStart = Convert.ToInt64(textBox_startNo.Text);
			}
			catch 
			{
			}

				
			try 
			{
				nEnd = Convert.ToInt64(textBox_endNo.Text);
			}
			catch 
			{
			}


			if (nStart > nEnd)
				bAsc = false;
			else
				bAsc = true;

			string strPath = strDbName + "/" + textBox_startNo.Text;
			string strStyle = "outputpath";

			if (bAsc == true)
				strStyle += ",next,myself";
			else 
				strStyle += ",prev,myself";

			string strResult;
			string strMetaData;
			byte [] baOutputTimeStamp;
			string strOutputPath;

			string strError0  = "";

			string strStartID = "";
			string strEndID = "";

			// �����Դ
			// return:
			//		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
			//		0	�ɹ�
			long lRet = channel.GetRes(strPath,
				strStyle,
				out strResult,
				out strMetaData,
				out baOutputTimeStamp,
				out strOutputPath,
				out strError0);
			if (lRet == -1) 
			{
				if (channel.ErrorCode == ChannelErrorCode.NotFound)
				{
					strStartID = textBox_startNo.Text;
                    bStartNotFound = true;
				}
				else 
					strError += "У��startnoʱ���� " + strError0 + " ";
				
			}
			else 
			{
				// ȡ�÷��ص�id
				strStartID = ResPath.GetRecordId(strOutputPath);


			}

			if (strStartID == "")
			{
				strError = "strStartIDΪ��...";
				return -1;
			}

			strPath = strDbName + "/" + textBox_endNo.Text;

			strStyle = "outputpath";
			if (bAsc == true)
				strStyle += ",prev,myself";
			else 
				strStyle += ",next,myself";

			// �����Դ
			// return:
			//		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
			//		0	�ɹ�
			lRet = channel.GetRes(strPath,
				strStyle,
				out strResult,
				out strMetaData,
				out baOutputTimeStamp,
				out strOutputPath,
				out strError0);
			if (lRet == -1) 
			{

				if (channel.ErrorCode == ChannelErrorCode.NotFound)
				{
					strEndID = textBox_endNo.Text;
                    bEndNotFound = true;
				}
				else
					strError += "У��endnoʱ���� " + strError0 + " ";

			}
			else 
			{
				// ȡ�÷��ص�id
				strEndID = ResPath.GetRecordId(strOutputPath);

			}

			if (strEndID == "")
			{
				strError = "strEndIDΪ��...";
				return -1;
			}

			///
			bool bSkip = false;

			Int64 nTemp = 0;
			try 
			{
				nTemp = Convert.ToInt64(strStartID);
			}
			catch
			{
				strError = "strStartIDֵ '" + strStartID + "' ��������...";
				return -1;
			}

			if (bAsc == true) 
			{
				if (nTemp > nEnd)
				{
					bSkip = true;
				}
			}
			else
			{
				if (nTemp < nEnd)
				{
					bSkip = true;
				}
			}

			if (bSkip == false) 
			{
				textBox_startNo.Text = strStartID;
			}


			///

			bSkip = false;

			try 
			{
				nTemp = Convert.ToInt64(strEndID);
			}
			catch
			{
				strError = "strEndIDֵ '" + strEndID + "' ��������...";
				return -1;
			}
			if (bAsc == true) 
			{
				if (nTemp < nStart)
				{
					bSkip = true;
				}
			}
			else
			{
				if (nTemp > nStart)
				{
					bSkip = true;
				}
			}

			if (bSkip == false) 
			{
				textBox_endNo.Text = strEndID;
			}

            if (bStartNotFound == true && bEndNotFound == true)
			    return 0;

            return 1;
		}
#endif

		private void treeView_rangeRes_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
            /*
			if (treeView_rangeRes.CheckBoxes == false
                && treeView_rangeRes.SelectedNode == null)
				return;
             */

            List<string> paths = treeView_rangeRes.GetCheckedDatabaseList();

            if (paths.Count == 0)
            {
                textBox_dbPath.Text = "";
                return;
            }

            // ������ݿ�·��֮����';'����
            string strText = "";
            for (int i = 0; i < paths.Count; i++)
            {
                if (strText != "")
                    strText += ";";
                strText += paths[i];
            }

            textBox_dbPath.Text = strText;

            /*
			if (treeView_rangeRes.SelectedNode.ImageIndex != ResTree.RESTYPE_DB) 
			{
				textBox_dbPath.Text = "";
				return;
			}

			ResPath respath = new ResPath(treeView_rangeRes.SelectedNode);

			textBox_dbPath.Text = respath.FullPath;
             */

            // ��ѡ�����ı�������ǰ�ڡ�ȫ����״̬����Ҫ������ֹ��Χ��������������ǰ��С����������ķ�Χ
            if (this.radioButton_all.Checked == true)
            {
                this.textBox_startNo.Text = "1";
                this.textBox_endNo.Text = "9999999999";
            }

		}

        private void radioButton_all_CheckedChanged(object sender, System.EventArgs e)
		{
			if (radioButton_all.Checked == true
                && m_nPreventNest == 0)
			{
                m_nPreventNest++;
				this.textBox_startNo.Text = "1";
				this.textBox_endNo.Text = "9999999999";
                m_nPreventNest--;
			}
		
		}

		void EnableControls(bool bEnabled)
		{
			textBox_startNo.Enabled = bEnabled;

			textBox_endNo.Enabled = bEnabled;

			checkBox_verifyNumber.Enabled = bEnabled;

			checkBox_forceLoop.Enabled = bEnabled;

			treeView_rangeRes.Enabled = bEnabled;

			radioButton_startEnd.Enabled = bEnabled;

			radioButton_all.Enabled = bEnabled;

			checkBox_export_delete.Enabled = bEnabled;

			///

			this.textBox_import_dbMap.Enabled = bEnabled;

			textBox_import_fileName.Enabled = bEnabled;

			textBox_import_range.Enabled = bEnabled;

			this.button_import_dbMap.Enabled = bEnabled;
			button_import_findFileName.Enabled = bEnabled;

            this.checkBox_import_fastMode.Enabled = bEnabled;
            this.checkBox_export_fastMode.Enabled = bEnabled;
		}

		private void menuItem_serversCfg_Click(object sender, System.EventArgs e)
		{
			ServersDlg dlg = new ServersDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

            string strWidths = this.AppInfo.GetString(
"serversdlg",
"list_column_width",
"");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(dlg.ListView,
                    strWidths,
                    true);
            }

			ServerCollection newServers = Servers.Dup();

			dlg.Servers = newServers;

            //dlg.StartPosition = FormStartPosition.CenterScreen;
			//dlg.ShowDialog(this);
            this.AppInfo.LinkFormState(dlg, "serversdlg_state");
            dlg.ShowDialog(this);
            this.AppInfo.UnlinkFormState(dlg);

            strWidths = ListViewUtil.GetColumnWidthListString(dlg.ListView);
            this.AppInfo.SetString(
                "serversdlg",
                "list_column_width",
                strWidths);


			if (dlg.DialogResult != DialogResult.OK)
				return;

			// this.Servers = newServers;
            this.Servers.Import(newServers);

			// treeView_rangeRes.Servers = this.Servers;
			treeView_rangeRes.Fill(null);
		}

		private void menuItem_copyright_Click(object sender, System.EventArgs e)
		{
			CopyrightDlg dlg = new CopyrightDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

			dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.ShowDialog(this);

		}

		private void menuItem_projectManage_Click(object sender, System.EventArgs e)
		{
			ProjectManageDlg dlg = new ProjectManageDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

            dlg.DataDir = this.DataDir;
            dlg.ProjectsUrl = "http://dp2003.com/dp2batch/projects/projects.xml";
            dlg.HostName = "dp2Batch";
			dlg.scriptManager = this.scriptManager;
			dlg.AppInfo = AppInfo;	
			dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.ShowDialog(this);
		}

        // ��������һ������
		private void menuItem_run_Click(object sender, System.EventArgs e)
		{
		
		}

		private void scriptManager_CreateDefaultContent(object sender, CreateDefaultContentEventArgs e)
		{
			string strPureFileName = Path.GetFileName(e.FileName);

			if (String.Compare(strPureFileName, "main.cs", true) == 0)
			{
				CreateDefaultMainCsFile(e.FileName);
				e.Created = true;
			}
			else if (String.Compare(strPureFileName, "marcfilter.fltx", true) == 0)
			{
				CreateDefaultMarcFilterFile(e.FileName);
				e.Created = true;
			}
			else 
			{
				e.Created = false;
			}

		}

		// ����ȱʡ��main.cs�ļ�
		public static int CreateDefaultMainCsFile(string strFileName)
		{

			StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8);
			sw.WriteLine("using System;");
			sw.WriteLine("using System.Windows.Forms;");
			sw.WriteLine("using System.IO;");
			sw.WriteLine("using System.Text;");
			sw.WriteLine("");

			sw.WriteLine("using DigitalPlatform.MarcDom;");
			sw.WriteLine("using DigitalPlatform.Statis;");
			sw.WriteLine("using dp2Batch;");

			sw.WriteLine("public class MyBatch : Batch");

			sw.WriteLine("{");

			sw.WriteLine("	public override void OnBegin(object sender, BatchEventArgs e)");
			sw.WriteLine("	{");
			sw.WriteLine("	}");


			sw.WriteLine("}");
			sw.Close();

			return 0;
		}

		// ����ȱʡ��marcfilter.fltx�ļ�
		public static int CreateDefaultMarcFilterFile(string strFileName)
		{

			StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8);

			sw.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			sw.WriteLine("<filter>");
			sw.WriteLine("<using>");
			sw.WriteLine("<![CDATA[");
			sw.WriteLine("using System;");
			sw.WriteLine("using System.IO;");
			sw.WriteLine("using System.Text;");
			sw.WriteLine("using System.Windows.Forms;");
			sw.WriteLine("using DigitalPlatform.MarcDom;");
			sw.WriteLine("using DigitalPlatform.Marc;");

			sw.WriteLine("using dp2Batch;");

			sw.WriteLine("]]>");
			sw.WriteLine("</using>");
			sw.WriteLine("	<record>");
			sw.WriteLine("		<def>");
			sw.WriteLine("		<![CDATA[");
			sw.WriteLine("			int i;");
			sw.WriteLine("			int j;");
			sw.WriteLine("		]]>");
			sw.WriteLine("		</def>");
			sw.WriteLine("		<begin>");
			sw.WriteLine("		<![CDATA[");
			sw.WriteLine("			MessageBox.Show(\"record data:\" + this.Data);");
			sw.WriteLine("		]]>");
			sw.WriteLine("		</begin>");
			sw.WriteLine("			 <field name=\"200\">");
			sw.WriteLine("");
			sw.WriteLine("			 </field>");
			sw.WriteLine("		<end>");
			sw.WriteLine("		<![CDATA[");
			sw.WriteLine("");
			sw.WriteLine("			j ++;");
			sw.WriteLine("		]]>");
			sw.WriteLine("		</end>");
			sw.WriteLine("	</record>");
			sw.WriteLine("</filter>");

			sw.Close();

			return 0;
		}

        private void treeView_rangeRes_AfterCheck(object sender, TreeViewEventArgs e)
        {
            treeView_rangeRes_AfterSelect(sender, e);
        }

        private void menuItem_openDataFolder_Click(object sender, EventArgs e)
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

        // �ؽ�������
        private void menuItem_rebuildKeys_Click(object sender, EventArgs e)
        {
            DoRebuildKeys();
        }

        // �ؽ�������
        // TODO: ��Ҫ����Ϊ�ڲ�У׼��λ�ŵ�����½�����ҲҪ��ʾ��ȷ���ɲο�DoExportFile()
        // parameters:
        void DoRebuildKeys()
        {
            string strError = "";
            int nRet = 0;
            long lRet = 0;

            string strInfo = "";    // ������Ϣ������ɺ���ʾ

        //      bClearKeysAtBegin   ������ʼ��ʱ����������е�keys��
        //      bDeleteOldKeysPerRecord ��ÿ����¼��ʱ���Ƿ�Ҫ��ɾ������������¼�ľɵļ����㡣
            bool bClearKeysAtBegin = true;
            bool bDeleteOldKeysPerRecord = false;

            m_nRecordCount = -1;

            if (textBox_dbPath.Text == "")
            {
                MessageBox.Show(this, "��δѡ��Ҫ�ؽ�����������ݿ� ...");
                return;
            }

            DialogResult result = MessageBox.Show(this,
                "ȷʵҪ���������ݿ�\r\n---\r\n"+this.textBox_dbPath.Text.Replace(";","\r\n")+"\r\n---\r\n�����ؽ�������Ĳ���?",
                "dp2batch",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result == DialogResult.No)
                return;

            RebuildKeysDialog option_dlg = new RebuildKeysDialog();
            MainForm.SetControlFont(option_dlg, this.DefaultFont);
            option_dlg.StartPosition = FormStartPosition.CenterScreen;
            option_dlg.ShowDialog(this);
            if (option_dlg.DialogResult == DialogResult.Cancel)
                return;

            if (option_dlg.WholeMode == true)
            {
                bClearKeysAtBegin = true;
                bDeleteOldKeysPerRecord = false;
            }
            else
            {
                bClearKeysAtBegin = false;
                bDeleteOldKeysPerRecord = true;
            }

            string[] dbpaths = textBox_dbPath.Text.Split(new char[] { ';' });

            // ���Ϊ�������
            if (dbpaths.Length == 1)
            {
                // �����Ƶ�DoExportFile()��������ȥУ��
                ResPath respath = new ResPath(dbpaths[0]);

                channel = this.Channels.GetChannel(respath.Url);

                string strDbName = respath.Path;

                // У����ֹ��
                if (checkBox_verifyNumber.Checked == true)
                {
                    nRet = VerifyRange(channel,
                        strDbName,
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(this, strError);
                }
                else
                {
                    if (this.textBox_startNo.Text == "")
                    {
                        strError = "��δָ����ʼ��";
                        goto ERROR1;
                    }
                    if (this.textBox_endNo.Text == "")
                    {
                        strError = "��δָ��������";
                        goto ERROR1;
                    }
                }
            }
            else
            {
                Debug.Assert(dbpaths.Length > 1, "");

                // ���������޸Ľ���Ҫ�أ���ʾ���ÿ���ⶼ��ȫ�⴦��
                this.radioButton_all.Checked = true;
                this.textBox_startNo.Text = "1";
                this.textBox_endNo.Text = "9999999999";
            }


            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����ؽ�������");
            stop.BeginLoop();


            EnableControls(false);
            try
            {

                // TODO: ����Ƕ��������Ƿ�Ҫ�Էǡ�ȫ��������ֹ�ŷ�Χ���о���? ��Ϊ������ǿ�Ȱ���ȫ�������е�

                for (int f = 0; f < dbpaths.Length; f++)
                {
                    string strOneDbPath = dbpaths[f];

                    ResPath respath = new ResPath(strOneDbPath);

                    channel = this.Channels.GetChannel(respath.Url);

                    string strDbName = respath.Path;

                    if (String.IsNullOrEmpty(strInfo) == false)
                        strInfo += "\r\n";

                    strInfo += "" + strDbName;

                    // ʵ�ʴ������β��
                    string strRealStartNo = "";
                    string strRealEndNo = "";
                    /*
                    DialogResult result;
                    if (checkBox_export_delete.Checked == true)
                    {
                        result = MessageBox.Show(this,
                            "ȷʵҪɾ��" + respath.Path + "��ָ����Χ�ļ�¼?\r\n\r\n---------\r\n(��)ɾ�� (��)����������",
                            "dp2batch",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result != DialogResult.Yes)
                            continue;
                    }*/

                    // 

                    // ���Ϊ����ؽ�
                    if (dbpaths.Length > 1)
                    {
                        // ���Ϊȫѡ
                        if (this.radioButton_all.Checked == true
                            || f > 0)
                        {
                            // �ָ�Ϊ���Χ
                            this.textBox_startNo.Text = "1";
                            this.textBox_endNo.Text = "9999999999";
                        }

                        // У����ֹ��
                        if (checkBox_verifyNumber.Checked == true)
                        {
                            nRet = VerifyRange(channel,
                                strDbName,
                                out strError);
                            if (nRet == -1)
                                MessageBox.Show(this, strError);

                            if (nRet == 0)
                            {
                                // �����޼�¼
                                AutoCloseMessageBox.Show(this, "���ݿ� " + strDbName + " ���޼�¼��");
                                strInfo += "(�޼�¼)";

                                /*
                                if (bClearKeysAtBegin == true)
                                {
                                    // ����Refresh���ݿⶨ��
                                    lRet = channel.DoRefreshDB(
                                        "end",
                                        strDbName,
                                        false,  // �˲�����ʱ����
                                        out strError);
                                    if (lRet == -1)
                                        goto ERROR1;
                                }
                                 * */

                                continue;
                            }
                        }
                        else
                        {
                            if (this.textBox_startNo.Text == "")
                            {
                                strError = "��δָ����ʼ��";
                                goto ERROR1;
                            }
                            if (this.textBox_endNo.Text == "")
                            {
                                strError = "��δָ��������";
                                goto ERROR1;
                            }

                        }
                    }


                    Int64 nStart;
                    Int64 nEnd;
                    Int64 nCur;
                    bool bAsc = GetDirection(
                        this.textBox_startNo.Text,
                        this.textBox_endNo.Text,
                        out nStart,
                        out nEnd);

                    // ���ý�������Χ
                    Int64 nMax = nEnd - nStart;
                    if (nMax < 0)
                        nMax *= -1;
                    nMax++;

                    /*
                    ProgressRatio = nMax / 10000;
                    if (ProgressRatio < 1.0)
                        ProgressRatio = 1.0;

                    progressBar_main.Minimum = 0;
                    progressBar_main.Maximum = (int)(nMax / ProgressRatio);
                    progressBar_main.Value = 0;
                     * */
                    stop.SetProgressRange(0, nMax);

                    // Refresh���ݿⶨ��
                    lRet = channel.DoRefreshDB(
                        "begin",
                        strDbName,
                        bClearKeysAtBegin == true ? true : false,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;


                    bool bFirst = true;	// �Ƿ�Ϊ��һ��ȡ��¼

                    string strID = this.textBox_startNo.Text;

                    m_nRecordCount = 0;
                    // ѭ��
                    for (; ; )
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop.State != 0)
                        {
                            result = MessageBox.Show(this,
                                "ȷʵҪ�жϵ�ǰ���������?",
                                "dp2batch",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button2);
                            if (result == DialogResult.Yes)
                            {
                                strError = "�û��ж�";
                                goto ERROR1;
                            }
                            else
                            {
                                stop.Continue();
                            }
                        }

                        string strDirectionComment = "";

                        string strStyle = "";

                        strStyle = "timestamp,outputpath";	// �Ż�

                        if (bDeleteOldKeysPerRecord == true)
                            strStyle += ",forcedeleteoldkeys";


                        if (bFirst == true)
                        {
                            // ע�������У���׺ţ�ֻ��ǿ��ѭ��������£����ܲ���Ҫnext���
                            strStyle += "";
                        }
                        else
                        {
                            if (bAsc == true)
                            {
                                strStyle += ",next";
                                strDirectionComment = "�ĺ�һ����¼";
                            }
                            else
                            {
                                strStyle += ",prev";
                                strDirectionComment = "��ǰһ����¼";
                            }
                        }

                        string strPath = strDbName + "/" + strID;
                        string strOutputPath = "";

                        bool bFoundRecord = false;

                        bool bNeedRetry = true;

                    REDO_REBUILD:
                        // �����Դ
                        // return:
                        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
                        //		0	�ɹ�
                        lRet = channel.DoRebuildResKeys(strPath,
                            strStyle,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            {
                                if (bFirst == true)
                                {
                                    // ���Ҫǿ��ѭ��
                                    if (checkBox_forceLoop.Checked == true)
                                    {
                                        AutoCloseMessageBox.Show(this, "��Ϊ���ݿ� "+strDbName+" ָ�����׼�¼ " + strID + strDirectionComment + " �����ڡ�\r\n\r\n�� ȷ�� ��������ҡ�");
                                        bFirst = false;
                                        goto CONTINUE;
                                    }
                                    else
                                    {
                                        // �����Ҫǿ��ѭ������ʱҲ���ܽ�������������û���Ϊ���ݿ��������û������
                                        AutoCloseMessageBox.Show(this, "��Ϊ���ݿ� " + strDbName + " ָ�����׼�¼ " + strID + strDirectionComment + " �����ڡ�\r\n\r\n(ע��Ϊ������ִ���ʾ�����ڲ���ǰ��ѡ��У׼��βID��)\r\n\r\n�� ȷ�� ���������...");
                                        bFirst = false;
                                        goto CONTINUE;
                                    }
                                }
                                else
                                {
                                    Debug.Assert(bFirst == false, "");

                                    if (bFirst == true)
                                    {
                                        strError = "��¼ " + strID + strDirectionComment + " �����ڡ����������";
                                    }
                                    else
                                    {
                                        if (bAsc == true)
                                            strError = "��¼ " + strID + " ����ĩһ����¼�����������";
                                        else
                                            strError = "��¼ " + strID + " ����ǰһ����¼�����������";
                                    }

                                    if (dbpaths.Length > 1)
                                        break;  // ������������������ѭ��
                                    else
                                    {
                                        bNeedRetry = false; // ���������Ҳû�б�Ҫ�������ԶԻ���
                                        MessageBox.Show(this, strError);
                                        break;
                                    }
                                }

                            }
                            else if (channel.ErrorCode == ChannelErrorCode.EmptyRecord)
                            {
                                bFirst = false;
                                // bFoundRecord = false;
                                // ��id��������
                                strID = ResPath.GetRecordId(strOutputPath);
                                goto CONTINUE;

                            }

                            // ��������
                            if (bNeedRetry == true)
                            {
                                DialogResult redo_result = MessageBox.Show(this,
                                    "�ؽ������� ��¼ '" + strPath + "' (style='" + strStyle + "')ʱ���ִ���: " + strError + "\r\n\r\n���ԣ������жϵ�ǰ���������?\r\n(Retry ���ԣ�Cancel �ж�������)",
                                    "dp2batch",
                                    MessageBoxButtons.RetryCancel,
                                    MessageBoxIcon.Question,
                                    MessageBoxDefaultButton.Button1);
                                if (redo_result == DialogResult.Cancel)
                                    goto ERROR1;
                                goto
                                    REDO_REBUILD;
                            }
                            else
                            {
                                goto ERROR1;
                            }

                        } // end of nRet == -1

                        bFirst = false;

                        bFoundRecord = true;

                        // ��id��������
                        strID = ResPath.GetRecordId(strOutputPath);
                        stop.SetMessage("���ؽ������� ��¼ " + strOutputPath + "  " + m_nRecordCount.ToString());

                        if (String.IsNullOrEmpty(strRealStartNo) == true)
                        {
                            strRealStartNo = strID;
                        }

                        strRealEndNo = strID;

                    CONTINUE:

                        // �Ƿ񳬹�ѭ����Χ
                        try
                        {
                            nCur = Convert.ToInt64(strID);
                        }
                        catch
                        {
                            // ???
                            nCur = 0;
                        }

                        if (bAsc == true && nCur > nEnd)
                            break;
                        if (bAsc == false && nCur < nEnd)
                            break;

                        if (bFoundRecord == true)
                            m_nRecordCount++;

                        //
                        //

                        if (bAsc == true)
                        {
                            // progressBar_main.Value = (int)((nCur - nStart + 1) / ProgressRatio);
                            stop.SetProgressValue(nCur - nStart + 1);
                        }
                        else
                        {
                            // ?
                            // progressBar_main.Value = (int)((nStart - nCur + 1) / ProgressRatio);
                            stop.SetProgressValue(nStart - nCur + 1);
                        }


                        // ���Ѿ������Ľ����ж�
                        if (bAsc == true && nCur >= nEnd)
                            break;
                        if (bAsc == false && nCur <= nEnd)
                            break;
                    }

                    if (bClearKeysAtBegin == true)
                    {
                        // ����Refresh���ݿⶨ��
                        lRet = channel.DoRefreshDB(
                            "end",
                            strDbName,
                            false,  // �˲�����ʱ����
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;
                    }

                    strInfo += " : " + m_nRecordCount.ToString() + "�� (ID " + strRealStartNo + "-" + strRealEndNo + ")";

                }   // end of dbpaths loop


            }   // end of try
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            strError = "�ؽ���������ɡ�\r\n---\r\n" + strInfo;

        // END1:

            MessageBox.Show(this, strError);
            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }


        private void tabControl_main_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.tabControl_main.SelectedTab == this.tabPage_range)
            {
                this.menuItem_rebuildKeys.Enabled = true;
            }
            else
            {
                this.menuItem_rebuildKeys.Enabled = false;
            }
        }

        private void menuItem_rebuildKeysByDbnames_Click(object sender, EventArgs e)
        {
            string strError = "";

            bool bHasClipboardObject = false;
            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null
                || iData.GetDataPresent(typeof(string)) == false)
                bHasClipboardObject = false;
            else
                bHasClipboardObject = true;

            if (bHasClipboardObject == false)
            {
                strError = "��ǰWindows�������в�û�а������ݿ�����Ϣ";
                goto ERROR1;
            }

            string strDbnames = (string)iData.GetData(typeof(string));
            if (String.IsNullOrEmpty(strDbnames) == true)
            {
                strError = "��ǰWindows�������е����ݿ�����ϢΪ��";
                goto ERROR1;
            }

            int nRet = strDbnames.IndexOf("?"); // .asmx?
            if (nRet == -1)
            {
                string strText = strDbnames;
                if (strText.Length > 1000)
                    strText = strText.Substring(0, 1000) + "...";

                strError = "��ǰWindows�����������������ַ��� '" + strText + "' �������ݿ�����ʽ";
                goto ERROR1;
            }

            List<string> paths = new List<string>();
            string[] parts = strDbnames.Split(new char[] {';'});
            for (int i = 0; i < parts.Length; i++)
            {
                string strPart = parts[i].Trim();
                if (String.IsNullOrEmpty(strPart) == true)
                    continue;
                paths.Add(strPart);
            }

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents(); // �ù����״��ʾ����

            bool bRet = this.treeView_rangeRes.SelectDatabases(paths, out strError);

            this.Cursor = oldCursor;

            if (bRet == false)
            {
                strError = "�������ݿ�·������Դ���в�����: \r\n---\r\n" + strError + "\r\n---\r\n\r\n��(�����˵����ļ�/ȱʡ�ʻ���������)����Դ��������µķ������ڵ㣬��ˢ����Դ���������½����ؽ�������Ĳ���";
                goto ERROR1;
            }

            DoRebuildKeys();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        int m_nPreventNest = 0;

        private void textBox_startNo_TextChanged(object sender, EventArgs e)
        {
            if (m_nPreventNest == 0)
            {
                m_nPreventNest++;   // ��ֹradioButton_all_CheckedChanged()�涯
                this.radioButton_startEnd.Checked = true;
                m_nPreventNest--;
            }
        }

        private void textBox_endNo_TextChanged(object sender, EventArgs e)
        {
            if (m_nPreventNest == 0)
            {
                m_nPreventNest++;      // ��ֹradioButton_all_CheckedChanged()�涯
                this.radioButton_startEnd.Checked = true;
                m_nPreventNest--;
            }
        }

        private void checkBox_export_delete_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_export_delete.Checked == true)
                this.checkBox_export_fastMode.Visible = true;
            else
                this.checkBox_export_fastMode.Visible = false;
        }

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
                float ratio = subfont.SizeInPoints / font.SizeInPoints;
                if (subfont.Name != font.Name
                    || subfont.SizeInPoints != font.SizeInPoints)
                {
                    sub.Font = new Font(font.FontFamily, ratio * font.SizeInPoints, subfont.Style, GraphicsUnit.Point);


                    // sub.Font = new Font(font, subfont.Style);
                }

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
            for (int i = 0; i < tool.Items.Count; i++)
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


	}

	public enum ExportFileType
	{
		BackupFile = 0,
		XmlFile = 1,
		ISO2709File = 2,
	}

	public class MyFilterDocument : FilterDocument
	{
		public Batch Batch = null;
	}

	// �����¼����
	public delegate void CheckTargetDbEventHandler(object sender,
	CheckTargetDbEventArgs e);

	public class CheckTargetDbEventArgs: EventArgs
	{
		public string DbFullPath = "";	// Ŀ�����ݿ�ȫ·��
        public string CurrentMarcSyntax = "";   // ��ǰ��¼�� MARC ��ʽ 2014/5/28
		public bool Cancel = false;	// �Ƿ���Ҫ�ж�
		public string ErrorInfo = "";	// �ص��ڼ䷢���Ĵ�����Ϣ
	}

}
