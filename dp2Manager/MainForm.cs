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

using System.Deployment.Application;

using DigitalPlatform;
using DigitalPlatform.Text;
using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.GUI;
using DigitalPlatform.CommonDialog;

namespace dp2Manager
{
	/// <summary>
	/// Summary description for MainForm.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
	{
        public string DataDir = "";

		public DigitalPlatform.StopManager	stopManager = new DigitalPlatform.StopManager();
		DigitalPlatform.Stop stop = null;

		public ServerCollection Servers = null;

		public LinkInfoCollection LinkInfos = null;

		public string Lang = "zh";

		//���������Ϣ
		public ApplicationInfo	AppInfo = new ApplicationInfo("dp2managers.xml");

        RmsChannel channel = null;	// ��ʱʹ�õ�channel����

	//	public AutoResetEvent eventClose = new AutoResetEvent(false);

		public RmsChannelCollection	Channels = new RmsChannelCollection();	// ӵ��

		private ResTree treeView_res;


		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem menuItem_accountManagement;
		private System.Windows.Forms.MenuItem menuItem_databaseManagement;
		private System.Windows.Forms.MenuItem menuItem_newDatabase;
		private System.Windows.Forms.MenuItem menuItem_deleteDatabase;
        private System.Windows.Forms.MenuItem menuItem_refresh;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MenuItem menuItem_serversCfg;
		private System.Windows.Forms.MenuItem menuItem_exit;
		private System.Windows.Forms.ToolBar toolBar1;
		private System.Windows.Forms.ToolBarButton toolBarButton_stop;
		private System.Windows.Forms.ImageList imageList_toolbar;
		private System.Windows.Forms.MenuItem menuItem_cfgLinkInfo;
		private System.Windows.Forms.MenuItem menuItem3;
        private MenuItem menuItem_test;
        private StatusStrip statusStrip_main;
        private ToolStripStatusLabel toolStripStatusLabel_main;
        private ToolStripProgressBar toolStripProgressBar_main;
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
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItem_serversCfg = new System.Windows.Forms.MenuItem();
            this.menuItem_cfgLinkInfo = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuItem_exit = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem_accountManagement = new System.Windows.Forms.MenuItem();
            this.menuItem_databaseManagement = new System.Windows.Forms.MenuItem();
            this.menuItem_newDatabase = new System.Windows.Forms.MenuItem();
            this.menuItem_deleteDatabase = new System.Windows.Forms.MenuItem();
            this.menuItem_refresh = new System.Windows.Forms.MenuItem();
            this.menuItem_test = new System.Windows.Forms.MenuItem();
            this.toolBar1 = new System.Windows.Forms.ToolBar();
            this.toolBarButton_stop = new System.Windows.Forms.ToolBarButton();
            this.imageList_toolbar = new System.Windows.Forms.ImageList(this.components);
            this.treeView_res = new DigitalPlatform.rms.Client.ResTree();
            this.statusStrip_main = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel_main = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar_main = new System.Windows.Forms.ToolStripProgressBar();
            this.statusStrip_main.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem2,
            this.menuItem1});
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 0;
            this.menuItem2.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem_serversCfg,
            this.menuItem_cfgLinkInfo,
            this.menuItem3,
            this.menuItem_exit});
            this.menuItem2.Text = "�ļ�(&F)";
            // 
            // menuItem_serversCfg
            // 
            this.menuItem_serversCfg.Index = 0;
            this.menuItem_serversCfg.Text = "ȱʡ�ʻ�����(&A)...";
            this.menuItem_serversCfg.Click += new System.EventHandler(this.menuItem_serversCfg_Click);
            // 
            // menuItem_cfgLinkInfo
            // 
            this.menuItem_cfgLinkInfo.Index = 1;
            this.menuItem_cfgLinkInfo.Text = "���ù���Ŀ¼(&L)...";
            this.menuItem_cfgLinkInfo.Visible = false;
            this.menuItem_cfgLinkInfo.Click += new System.EventHandler(this.menuItem_cfgLinkInfo_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 2;
            this.menuItem3.Text = "-";
            // 
            // menuItem_exit
            // 
            this.menuItem_exit.Index = 3;
            this.menuItem_exit.Text = "�˳�(&X)";
            this.menuItem_exit.Click += new System.EventHandler(this.menuItem_exit_Click);
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 1;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem_accountManagement,
            this.menuItem_databaseManagement,
            this.menuItem_newDatabase,
            this.menuItem_deleteDatabase,
            this.menuItem_refresh,
            this.menuItem_test});
            this.menuItem1.Text = "����(&U)";
            // 
            // menuItem_accountManagement
            // 
            this.menuItem_accountManagement.Index = 0;
            this.menuItem_accountManagement.Text = "�ʻ�(&A)...";
            this.menuItem_accountManagement.Click += new System.EventHandler(this.menuItem_accountManagement_Click);
            // 
            // menuItem_databaseManagement
            // 
            this.menuItem_databaseManagement.Index = 1;
            this.menuItem_databaseManagement.Text = "���ݿ�(&M)...";
            this.menuItem_databaseManagement.Click += new System.EventHandler(this.menuItem_databaseManagement_Click);
            // 
            // menuItem_newDatabase
            // 
            this.menuItem_newDatabase.Index = 2;
            this.menuItem_newDatabase.Text = "�½����ݿ�(&N)...";
            this.menuItem_newDatabase.Click += new System.EventHandler(this.menuItem_newDatabase_Click);
            // 
            // menuItem_deleteDatabase
            // 
            this.menuItem_deleteDatabase.Index = 3;
            this.menuItem_deleteDatabase.Text = "ɾ�����ݿ�(&D)";
            this.menuItem_deleteDatabase.Click += new System.EventHandler(this.menuItem_deleteObject_Click);
            // 
            // menuItem_refresh
            // 
            this.menuItem_refresh.Index = 4;
            this.menuItem_refresh.Text = "ˢ��(&R)";
            this.menuItem_refresh.Click += new System.EventHandler(this.menuItem_refresh_Click);
            // 
            // menuItem_test
            // 
            this.menuItem_test.Index = 5;
            this.menuItem_test.Text = "test";
            this.menuItem_test.Visible = false;
            this.menuItem_test.Click += new System.EventHandler(this.menuItem_test_Click);
            // 
            // toolBar1
            // 
            this.toolBar1.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
            this.toolBar1.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
            this.toolBarButton_stop});
            this.toolBar1.DropDownArrows = true;
            this.toolBar1.ImageList = this.imageList_toolbar;
            this.toolBar1.Location = new System.Drawing.Point(0, 0);
            this.toolBar1.Name = "toolBar1";
            this.toolBar1.ShowToolTips = true;
            this.toolBar1.Size = new System.Drawing.Size(481, 34);
            this.toolBar1.TabIndex = 2;
            this.toolBar1.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.toolBar1_ButtonClick);
            // 
            // toolBarButton_stop
            // 
            this.toolBarButton_stop.Enabled = false;
            this.toolBarButton_stop.ImageIndex = 0;
            this.toolBarButton_stop.Name = "toolBarButton_stop";
            this.toolBarButton_stop.ToolTipText = "ֹͣ";
            // 
            // imageList_toolbar
            // 
            this.imageList_toolbar.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList_toolbar.ImageStream")));
            this.imageList_toolbar.TransparentColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(193)))));
            this.imageList_toolbar.Images.SetKeyName(0, "");
            this.imageList_toolbar.Images.SetKeyName(1, "");
            // 
            // treeView_res
            // 
            this.treeView_res.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.treeView_res.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView_res.HideSelection = false;
            this.treeView_res.ImageIndex = 0;
            this.treeView_res.Location = new System.Drawing.Point(0, 34);
            this.treeView_res.Name = "treeView_res";
            this.treeView_res.SelectedImageIndex = 0;
            this.treeView_res.Size = new System.Drawing.Size(481, 340);
            this.treeView_res.TabIndex = 0;
            this.treeView_res.OnSetMenu += new DigitalPlatform.GUI.GuiAppendMenuEventHandle(this.treeView_res_OnSetMenu);
            this.treeView_res.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_res_AfterSelect);
            // 
            // statusStrip_main
            // 
            this.statusStrip_main.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel_main,
            this.toolStripProgressBar_main});
            this.statusStrip_main.Location = new System.Drawing.Point(0, 352);
            this.statusStrip_main.Name = "statusStrip_main";
            this.statusStrip_main.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.statusStrip_main.Size = new System.Drawing.Size(481, 22);
            this.statusStrip_main.TabIndex = 5;
            this.statusStrip_main.Text = "statusStrip1";
            // 
            // toolStripStatusLabel_main
            // 
            this.toolStripStatusLabel_main.Name = "toolStripStatusLabel_main";
            this.toolStripStatusLabel_main.Size = new System.Drawing.Size(292, 17);
            this.toolStripStatusLabel_main.Spring = true;
            // 
            // toolStripProgressBar_main
            // 
            this.toolStripProgressBar_main.Name = "toolStripProgressBar_main";
            this.toolStripProgressBar_main.Size = new System.Drawing.Size(172, 16);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(481, 374);
            this.Controls.Add(this.statusStrip_main);
            this.Controls.Add(this.treeView_res);
            this.Controls.Add(this.toolBar1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Menu = this.mainMenu1;
            this.Name = "MainForm";
            this.Text = "dp2manager V2 -- �ں˹���";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
            this.Closed += new System.EventHandler(this.Form1_Closed);
            this.Load += new System.EventHandler(this.Form1_Load);
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

		private void Form1_Load(object sender, System.EventArgs e)
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
					+ "\\manager_servers.bin",
					true);
				Servers.ownerForm = this;
			}
			catch (SerializationException ex)
			{
				MessageBox.Show(this, ex.Message);
				Servers = new ServerCollection();
				// �����ļ������Ա㱾�����н���ʱ���Ǿ��ļ�
                Servers.FileName = this.DataDir
					+ "\\manager_servers.bin";

			}

            this.Servers.ServerChanged += new ServerChangedEventHandle(Servers_ServerChanged);

			// ���ļ���װ�ش���һ��LinkInfoCollection����
			// parameters:
			//		bIgnorFileNotFound	�Ƿ��׳�FileNotFoundException�쳣��
			//							���==true������ֱ�ӷ���һ���µĿ�ServerCollection����
			// Exception:
			//			FileNotFoundException	�ļ�û�ҵ�
			//			SerializationException	�汾Ǩ��ʱ���׳���
			try 
			{
                LinkInfos = LinkInfoCollection.Load(this.DataDir
					+ "\\manager_linkinfos.bin",
					true);
			}
			catch (SerializationException ex)
			{
				MessageBox.Show(this, ex.Message);
				LinkInfos = new LinkInfoCollection();
				// �����ļ������Ա㱾�����н���ʱ���Ǿ��ļ�
                LinkInfos.FileName = this.DataDir
					+ "\\manager_linkinfos.bin";

			}




			// ���ô��ڳߴ�״̬
			if (AppInfo != null) 
			{
                SetFirstDefaultFont();

                MainForm.SetControlFont(this, this.DefaultFont);

				AppInfo.LoadFormStates(this,
					"mainformstate");
			}

			stopManager.Initial(toolBarButton_stop,
                this.toolStripStatusLabel_main,
                this.toolStripProgressBar_main);
			stop = new DigitalPlatform.Stop();
			stop.Register(this.stopManager, true);	// ����������

            /*
			this.Channels.procAskAccountInfo = 
				new Delegate_AskAccountInfo(this.Servers.AskAccountInfo);
             */
            this.Channels.AskAccountInfo += new AskAccountInfoEventHandle(this.Servers.OnAskAccountInfo);



			// �򵥼�������׼������
			treeView_res.AppInfo = this.AppInfo;	// ����treeview��popup�˵��޸������ļ�ʱ����dialog�ߴ�λ��

			treeView_res.stopManager = this.stopManager;

			treeView_res.Servers = this.Servers;	// ����

			treeView_res.Channels = this.Channels;	// ����
		
			treeView_res.Fill(null);

			//
			LinkInfos.Channels = this.Channels;

			int nRet = 0;
			string strError = "";
			nRet = this.LinkInfos.Link(out strError);
			if (nRet == -1)
				MessageBox.Show(this, strError);
		
		}

        void Servers_ServerChanged(object sender, ServerChangedEventArgs e)
        {
            this.treeView_res.Refresh(ResTree.RefreshStyle.All);   // ˢ�µ�һ��
        }


		private void Form1_Closed(object sender, System.EventArgs e)
		{
            this.Channels.AskAccountInfo -= new AskAccountInfoEventHandle(this.Servers.OnAskAccountInfo);

            // ���ȱ�˴˾䣬��Servers.Save���������
            this.Servers.ServerChanged -= new ServerChangedEventHandle(Servers_ServerChanged);

			// ���浽�ļ�
			// parameters:
			//		strFileName	�ļ��������==null,��ʾʹ��װ��ʱ������Ǹ��ļ���
			Servers.Save(null);
			Servers = null;

			LinkInfos.Save(null);
			LinkInfos = null;

			// ���洰�ڳߴ�״̬
			if (AppInfo != null) 
			{

				AppInfo.SaveFormStates(this,
					"mainformstate");
			}

			//��סsave,������ϢXML�ļ�
			AppInfo.Save();
			AppInfo = null;	// ������������������	
		}

		private void menuItem_accountManagement_Click(object sender, System.EventArgs e)
		{

			if (treeView_res.SelectedNode == null)
			{
				MessageBox.Show("��ѡ��һ���ڵ�");
				return;
			}

			ResPath respath = new ResPath(treeView_res.SelectedNode);

			GetUserNameDlg namedlg = new GetUserNameDlg();
            MainForm.SetControlFont(namedlg, this.DefaultFont);

			string strError = "";
            this.Cursor = Cursors.WaitCursor;
            int nRet = namedlg.Initial(this.Servers,
				this.Channels,
				this.stopManager,
				respath.Url,
				out strError);
            this.Cursor = Cursors.Arrow;
            if (nRet == -1)
			{
				MessageBox.Show(strError);
				return;
			}

			namedlg.StartPosition = FormStartPosition.CenterScreen;
			namedlg.ShowDialog(this);
			if (namedlg.DialogResult != DialogResult.OK)
				return;

			UserRightsDlg dlg = new UserRightsDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);
            dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.UserName = namedlg.SelectedUserName;
            dlg.UserRecPath = namedlg.SelectedUserRecPath;
			dlg.ServerUrl = respath.Url;
			dlg.MainForm = this;

			this.AppInfo.LinkFormState(dlg, "userrightsdlg_state");
			dlg.ShowDialog(this);
			this.AppInfo.UnlinkFormState(dlg);
		}

		private void menuItem_databaseManagement_Click(object sender, System.EventArgs e)
		{
			if (treeView_res.SelectedNode == null)
			{
				MessageBox.Show("��ѡ��һ�����ݿ�ڵ�");
				return;
			}

			ResPath respath = new ResPath(treeView_res.SelectedNode);
			if (respath.Path == "")
			{
				MessageBox.Show("��ѡ��һ�����ݿ����͵Ľڵ�");
				return;
			}
			string strPath = respath.Path;
			string strDbName = StringUtil.GetFirstPartPath(ref strPath);
			if (strDbName == "")
			{
				MessageBox.Show("����: ���ݿ���Ϊ��");
				return;
			}

			DatabaseDlg dlg = new DatabaseDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);
            dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.MainForm = this;
			dlg.Initial(respath.Url, 
                strDbName);

			this.AppInfo.LinkFormState(dlg, "databasedlg_state");
			dlg.ShowDialog(this);
			this.AppInfo.UnlinkFormState(dlg);
		}

		// ���������ݿ�
		private void menuItem_newDatabase_Click(object sender, System.EventArgs e)
		{
			if (treeView_res.SelectedNode == null)
			{
				MessageBox.Show("��ѡ��һ�������������ݿ�ڵ�");
				return;
			}

			ResPath respath = new ResPath(treeView_res.SelectedNode);

			string strRefDbName = "";
			if (treeView_res.SelectedNode != null
                && treeView_res.SelectedNode.ImageIndex == ResTree.RESTYPE_DB)
			{
				if (respath.Path != "")
				{
					string strPath = respath.Path;
					strRefDbName = StringUtil.GetFirstPartPath(ref strPath);
				}
			}


			DatabaseDlg dlg = new DatabaseDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);
            dlg.Text = "���������ݿ�";
			dlg.IsCreate = true;
			dlg.RefDbName = strRefDbName;
			dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.MainForm = this;
			dlg.Initial(respath.Url, 
				"");

			this.AppInfo.LinkFormState(dlg, "databasedlg_state");
			dlg.ShowDialog(this);
			this.AppInfo.UnlinkFormState(dlg);
		}

		// ����û���¼
        // return:
        //      -1  error
        //      0   not found
        //      >=1   �������е�����
		public int GetUserRecord(
			string strServerUrl,
			string strUserName,
			out string strRecPath,
			out string strXml,
			out byte[] baTimeStamp,
			out string strError)
		{
			strError = "";

			strXml = "";
            strRecPath = "";
			baTimeStamp = null;

            if (strUserName == "")
            {
                strError = "�û���Ϊ��";
                return -1;
            }

            string strQueryXml = "<target list='" + Defs.DefaultUserDb.Name
                + ":" + Defs.DefaultUserDb.SearchPath.UserName + "'><item><word>"
				+ strUserName + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>10</maxCount></item><lang>chi</lang></target>";

            RmsChannel channel = this.Channels.GetChannel(strServerUrl);
			if (channel == null)
			{
				strError = "Channels.GetChannel �쳣";
				return -1;
			}

			long nRet = channel.DoSearch(strQueryXml,
                "default",
                out strError);
			if (nRet == -1) 
			{
				strError = "�����ʻ���ʱ����: " + strError;
				return -1;
			}

			if (nRet == 0)
				return 0;	// not found

            long nSearchCount = nRet;

			List<string> aPath = null;
			nRet = channel.DoGetSearchResult(
                "default",
				1,
				this.Lang,
				null,	// stop,
				out aPath,
				out strError);
			if (nRet == -1) 
			{
				strError = "����ע���û����ȡ�������ʱ����: " + strError;
				return -1;
			}
			if (aPath.Count == 0)
			{
				strError = "����ע���û����ȡ�ļ������Ϊ��";
				return -1;
			}

			// strRecID = ResPath.GetRecordId((string)aPath[0]);
            strRecPath = (string)aPath[0];

			string strStyle = "content,data,timestamp,withresmetadata";
			string strMetaData;
			string strOutputPath;

			nRet = channel.GetRes((string)aPath[0],
				strStyle,
				out strXml,
				out strMetaData,
				out baTimeStamp,
				out strOutputPath,
				out strError);
			if (nRet == -1) 
			{
				strError = "��ȡע���û����¼��ʱ����: " + strError;
				return -1;
			}


			return (int)nSearchCount;
		}

        // ����·������û���¼
        // return:
        //      -1  error
        //      0   not found
        //      >=1   �������е�����
        public int GetUserRecord(
            string strServerUrl,
            string strRecPath,
            out string strXml,
            out byte[] baTimeStamp,
            out string strError)
        {

            strError = "";

            strXml = "";
            baTimeStamp = null;

            if (strRecPath == "")
            {
                strError = "·��Ϊ��";
                return -1;
            }

            RmsChannel channel = this.Channels.GetChannel(strServerUrl);
            if (channel == null)
            {
                strError = "Channels.GetChannel �쳣";
                return -1;
            }

 
            string strStyle = "content,data,timestamp,withresmetadata";
            string strMetaData;
            string strOutputPath;

            long nRet = channel.GetRes(strRecPath,
                strStyle,
                out strXml,
                out strMetaData,
                out baTimeStamp,
                out strOutputPath,
                out strError);
            if (nRet == -1)
            {
                strError = "��ȡע���û����¼��ʱ����: " + strError;
                if (channel.ErrorCode == ChannelErrorCode.NotFound)
                    return 0;
                return -1;
            }

            return 1;
        }



        private void menuItem_deleteObject_Click(object sender, System.EventArgs e)
		{
            try
            {
                string strError = "";

                if (treeView_res.SelectedNode == null)
                {
                    MessageBox.Show("��ѡ��һ�����ݿ⡢Ŀ¼���ļ��ڵ�");
                    return;
                }

                ResPath respath = new ResPath(treeView_res.SelectedNode);

                string strPath = "";
                if (respath.Path != "")
                {
                    strPath = respath.Path;
                    // strPath = StringUtil.GetFirstPartPath(ref strPath);
                }
                else
                {
                    // Debug.Assert(false, "");
                    MessageBox.Show("��ѡ��һ�����ݿ⡢Ŀ¼���ļ��ڵ�");
                    return;
                }

                string strText = "";

                if (treeView_res.SelectedNode.ImageIndex == ResTree.RESTYPE_DB)
                    strText = "ȷʵҪɾ��λ�� " + respath.Url + "\r\n�����ݿ� '" + strPath + "' ?\r\n\r\n***���棺���ݿ�һ��ɾ�������޷��ָ���";
                else
                    strText = "ȷʵҪɾ��λ�� " + respath.Url + "\r\n�Ķ��� '" + strPath + "' ?\r\n\r\n***���棺����һ��ɾ�������޷��ָ���";

                //
                DialogResult result = MessageBox.Show(this,
                    strText,
                    "dp2manager",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                    return;

                RmsChannel channel = Channels.GetChannel(respath.Url);
                if (channel == null)
                {
                    strError = "Channels.GetChannel �쳣";
                    goto ERROR1;
                }

                long lRet = 0;

                if (treeView_res.SelectedNode.ImageIndex == ResTree.RESTYPE_DB)
                {
                    // ɾ�����ݿ�
                    lRet = channel.DoDeleteDB(strPath, out strError);
                }
                else
                {
                    byte[] timestamp = null;
                    byte[] output_timestamp = null;

                REDODELETE:
                    // ɾ��������Դ
                    lRet = channel.DoDeleteRes(strPath,
                        timestamp,
                        out output_timestamp,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                    {
                        timestamp = output_timestamp;
                        goto REDODELETE;
                    }
                }

                if (lRet == -1)
                    goto ERROR1;

                if (treeView_res.SelectedNode.ImageIndex == ResTree.RESTYPE_DB)
                    MessageBox.Show(this, "���ݿ� '" + strPath + "' �ѱ��ɹ�ɾ��");
                else
                    MessageBox.Show(this, "���� '" + strPath + "' �ѱ��ɹ�ɾ��");



                this.treeView_res.Refresh(ResTree.RefreshStyle.All);

                return;
            ERROR1:
                MessageBox.Show(this, strError);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "menuItem_deleteObject_Click��) �׳��쳣: " + ExceptionUtil.GetDebugText(ex));
            }
		}

		public void menuItem_refresh_Click(object sender, System.EventArgs e)
		{
			treeView_res.menu_refresh(null, null);
		}

		private void treeView_res_OnSetMenu(object sender, DigitalPlatform.GUI.GuiAppendMenuEventArgs e)
		{
			Debug.Assert(e.ContextMenu != null, "e����Ϊnull");

            int nNodeType = -1;
            TreeNode node = this.treeView_res.SelectedNode;
            if (node != null)
                nNodeType = node.ImageIndex;



			MenuItem menuItem = new MenuItem("-");
			e.ContextMenu.MenuItems.Add(menuItem);


			// �ʻ�����
			menuItem = new MenuItem("�ʻ�(&A)...");
			menuItem.Click += new System.EventHandler(this.menuItem_accountManagement_Click);
			e.ContextMenu.MenuItems.Add(menuItem);

            // �½��ʻ�
            menuItem = new MenuItem("���ʻ�(&N)...");
            menuItem.Click += new System.EventHandler(this.menuItem_newAccount_Click);
            e.ContextMenu.MenuItems.Add(menuItem);


			menuItem = new MenuItem("-");
			e.ContextMenu.MenuItems.Add(menuItem);


			// �������ݿ�
			menuItem = new MenuItem("���ݿ�(&M)...");
			menuItem.Click += new System.EventHandler(this.menuItem_databaseManagement_Click);
            if (nNodeType != ResTree.RESTYPE_DB)
                menuItem.Enabled = false;
			e.ContextMenu.MenuItems.Add(menuItem);



			// �½����ݿ�
			menuItem = new MenuItem("�½����ݿ�(&N)...");
			menuItem.Click += new System.EventHandler(this.menuItem_newDatabase_Click);
			e.ContextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("-");
			e.ContextMenu.MenuItems.Add(menuItem);

			// ɾ�����ݿ�
			menuItem = new MenuItem("ɾ�����ݿ�(&D)");
			menuItem.Click += new System.EventHandler(this.menuItem_deleteObject_Click);
            if (nNodeType != ResTree.RESTYPE_DB 
                && nNodeType != ResTree.RESTYPE_FILE
                && nNodeType != ResTree.RESTYPE_FOLDER)
                menuItem.Enabled = false;
            if (nNodeType != ResTree.RESTYPE_DB)
                menuItem.Text = "ɾ������(&D)";
			e.ContextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("-");
			e.ContextMenu.MenuItems.Add(menuItem);

#if NO
			// ��������Ŀ¼
			menuItem = new MenuItem("��������Ŀ¼(&L)...");
			menuItem.Click += new System.EventHandler(this.menuItem_linkLocalDir_Click);
			e.ContextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("-");
            e.ContextMenu.MenuItems.Add(menuItem);
#endif

            // ���ģ��
            menuItem = new MenuItem("����ģ��(&E)...");
            menuItem.Click += new System.EventHandler(this.menuItem_exportTemplate_Click);
            e.ContextMenu.MenuItems.Add(menuItem);

            // ����ģ��
            menuItem = new MenuItem("����ģ��(&I)...");
            menuItem.Click += new System.EventHandler(this.menuItem_importTemplate_Click);
            e.ContextMenu.MenuItems.Add(menuItem);


		}

        // ����ģ��
        void menuItem_exportTemplate_Click(object sender, System.EventArgs e)
        {
            if (treeView_res.SelectedNode == null)
            {
                MessageBox.Show("��ѡ��һ���ڵ�");
                return;
            }

            if (treeView_res.SelectedNode.ImageIndex != ResTree.RESTYPE_DB
                && treeView_res.SelectedNode.ImageIndex != ResTree.RESTYPE_SERVER)
            {
                MessageBox.Show("��ѡ��һ�������������ݿ����ͽڵ�");
                return;
            }

            treeView_res.Refresh(ResTree.RefreshStyle.Selected);

            ExportTemplateDlg dlg = new ExportTemplateDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

            dlg.Objects = new List<ObjectInfo>();

            if (treeView_res.SelectedNode.ImageIndex == ResTree.RESTYPE_SERVER)
            {
                for (int i = 0; i < treeView_res.SelectedNode.Nodes.Count; i++)
                {
                    ObjectInfo objectinfo = new ObjectInfo();

                    ResPath respath = new ResPath(treeView_res.SelectedNode.Nodes[i]);

                    objectinfo.Path = respath.Path;
                    objectinfo.Url = respath.Url;
                    objectinfo.ImageIndex = treeView_res.SelectedNode.Nodes[i].ImageIndex;
                    dlg.Objects.Add(objectinfo);
                }
            }
            else
            {
                ObjectInfo objectinfo = new ObjectInfo();

                ResPath respath = new ResPath(treeView_res.SelectedNode);

                objectinfo.Path = respath.Path;
                objectinfo.Url = respath.Url;
                objectinfo.ImageIndex = treeView_res.SelectedNode.ImageIndex;
                dlg.Objects.Add(objectinfo);
            }

            dlg.MainForm = this;
            dlg.ShowDialog(this);
        }

        // ����ģ��
        void menuItem_importTemplate_Click(object sender, System.EventArgs e)
        {
            if (treeView_res.SelectedNode == null)
            {
                MessageBox.Show("��ѡ��һ���ڵ�");
                return;
            }

            if (treeView_res.SelectedNode.ImageIndex != ResTree.RESTYPE_SERVER)
            {
                MessageBox.Show("��ѡ��һ�����������ͽڵ�");
                return;
            }

            ResPath respath = new ResPath(treeView_res.SelectedNode);

            /*
            string strRefDbName = "";
            if (treeView_res.SelectedNode != null)
            {
                if (respath.Path != "")
                {
                    string strPath = respath.Path;
                    strRefDbName = StringUtil.GetFirstPartPath(ref strPath);
                }
            }
             */


            OpenFileDialog filedlg = new OpenFileDialog();

            filedlg.FileName = "*.template";
			// filedlg.InitialDirectory = Environment.CurrentDirectory;
			filedlg.Filter = "ģ���ļ� (*.template)|*.template|All files (*.*)|*.*" ;
			filedlg.RestoreDirectory = true ;

			if (filedlg.ShowDialog() != DialogResult.OK)
			{
				return;
			}


            ImportTemplateDlg dlg = new ImportTemplateDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

            dlg.Url = respath.Url;
            dlg.FileName = filedlg.FileName;
            dlg.MainForm = this;
            dlg.ShowDialog(this);
        }

        // �½��ʻ�
        void menuItem_newAccount_Click(object sender, System.EventArgs e)
        {

            if (treeView_res.SelectedNode == null)
            {
                MessageBox.Show("��ѡ��һ���ڵ�");
                return;
            }

            ResPath respath = new ResPath(treeView_res.SelectedNode);

            UserRightsDlg dlg = new UserRightsDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

            dlg.MainForm = this;
            dlg.ServerUrl = respath.Url;
            dlg.ShowDialog(this);
        }

		// ��������Ŀ¼
		private void menuItem_linkLocalDir_Click(object sender, System.EventArgs e)
		{
			string strDefault = "";
			if (treeView_res.SelectedNode != null)
			{
				ResPath respath = new ResPath(treeView_res.SelectedNode);


				if (treeView_res.SelectedNode.ImageIndex == ResTree.RESTYPE_FOLDER)
					strDefault = respath.FullPath;
				else
					strDefault = respath.Url;
			}


			LinkInfoDlg dlg = new LinkInfoDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

			dlg.LinkInfos = this.LinkInfos;
			dlg.CreateNewServerPath = strDefault;
			dlg.ShowDialog(this);
		}
		

		private void treeView_res_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			if (treeView_res.SelectedNode == null)
			{
                this.toolStripStatusLabel_main.Text = "��δѡ��һ���ڵ�";
				return;
			}

			ResPath respath = new ResPath(treeView_res.SelectedNode);

            this.toolStripStatusLabel_main.Text = "��ǰ�ڵ�: " + respath.FullPath;
		
		}

		private void menuItem_serversCfg_Click(object sender, System.EventArgs e)
		{
			ServersDlg dlg = new ServersDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

			ServerCollection newServers = Servers.Dup();

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

            dlg.Servers = newServers;

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

			// this.treeView_res.Servers = this.Servers;
			treeView_res.Fill(null);
		}

		private void menuItem_exit_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void toolBar1_ButtonClick(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e)
		{
			if (e.Button == toolBarButton_stop) 
			{
				stopManager.DoStopActive();
			}
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

		void DoStop()
		{
			if (this.channel != null)
				this.channel.Abort();
		}

		private void menuItem_cfgLinkInfo_Click(object sender, System.EventArgs e)
		{
			LinkInfoDlg dlg = new LinkInfoDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

			dlg.LinkInfos = this.LinkInfos;
			dlg.ShowDialog(this);
		}

        // ��������ֵ�Ի���
        private void menuItem_test_Click(object sender, EventArgs e)
        {
            CategoryPropertyDlg dlg = new CategoryPropertyDlg();
            MainForm.SetControlFont(dlg, this.DefaultFont);

            dlg.CfgFileName = Environment.CurrentDirectory + "\\userrightsdef.xml";
            dlg.ShowDialog(this);
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
                    return null;

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
}
