using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using DigitalPlatform.GUI;

namespace DigitalPlatform.CirculationClient
{
	/// <summary>
	/// Summary description for ServersDlg.
	/// </summary>
	public class ServersDlg : System.Windows.Forms.Form
	{
        public bool FirstRun = false;

		public dp2ServerCollection Servers = null;	// ����

		bool m_bChanged = false;

        private DigitalPlatform.GUI.ListViewNF listView1;
		private System.Windows.Forms.ColumnHeader columnHeader_url;
		private System.Windows.Forms.ColumnHeader columnHeader_userName;
		private System.Windows.Forms.ColumnHeader columnHeader_savePassword;
		private System.Windows.Forms.Button button_OK;
		private System.Windows.Forms.Button button_Cancel;
        private Button button_newServer;
        private ColumnHeader columnHeader_name;
        private IContainer components;

        private MessageBalloon m_firstUseBalloon = null;


		public ServersDlg()
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
				if(components != null)
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
            this.listView1 = new DigitalPlatform.GUI.ListViewNF();
            this.columnHeader_name = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_url = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_userName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_savePassword = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.button_OK = new System.Windows.Forms.Button();
            this.button_Cancel = new System.Windows.Forms.Button();
            this.button_newServer = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader_name,
            this.columnHeader_url,
            this.columnHeader_userName,
            this.columnHeader_savePassword});
            this.listView1.FullRowSelect = true;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(9, 9);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(446, 276);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.DoubleClick += new System.EventHandler(this.listView1_DoubleClick);
            this.listView1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView1_MouseUp);
            // 
            // columnHeader_name
            // 
            this.columnHeader_name.Text = "��������";
            this.columnHeader_name.Width = 200;
            // 
            // columnHeader_url
            // 
            this.columnHeader_url.Text = "������URL";
            this.columnHeader_url.Width = 300;
            // 
            // columnHeader_userName
            // 
            this.columnHeader_userName.Text = "�û���";
            this.columnHeader_userName.Width = 150;
            // 
            // columnHeader_savePassword
            // 
            this.columnHeader_savePassword.Text = "�Ƿ񱣴�����";
            this.columnHeader_savePassword.Width = 150;
            // 
            // button_OK
            // 
            this.button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_OK.Location = new System.Drawing.Point(380, 290);
            this.button_OK.Name = "button_OK";
            this.button_OK.Size = new System.Drawing.Size(75, 22);
            this.button_OK.TabIndex = 1;
            this.button_OK.Text = "ȷ��";
            this.button_OK.Click += new System.EventHandler(this.button_OK_Click);
            // 
            // button_Cancel
            // 
            this.button_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Cancel.Location = new System.Drawing.Point(380, 317);
            this.button_Cancel.Name = "button_Cancel";
            this.button_Cancel.Size = new System.Drawing.Size(75, 21);
            this.button_Cancel.TabIndex = 2;
            this.button_Cancel.Text = "ȡ��";
            this.button_Cancel.Click += new System.EventHandler(this.button_Cancel_Click);
            // 
            // button_newServer
            // 
            this.button_newServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_newServer.Location = new System.Drawing.Point(9, 290);
            this.button_newServer.Name = "button_newServer";
            this.button_newServer.Size = new System.Drawing.Size(113, 22);
            this.button_newServer.TabIndex = 3;
            this.button_newServer.Text = "����������(&N)";
            this.button_newServer.UseVisualStyleBackColor = true;
            this.button_newServer.Click += new System.EventHandler(this.button_newServer_Click);
            // 
            // ServersDlg
            // 
            this.AcceptButton = this.button_OK;
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(464, 348);
            this.Controls.Add(this.button_newServer);
            this.Controls.Add(this.button_Cancel);
            this.Controls.Add(this.button_OK);
            this.Controls.Add(this.listView1);
            this.Name = "ServersDlg";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "dp2library ��������ȱʡ�ʻ�����";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.ServersDlg_Closing);
            this.Load += new System.EventHandler(this.ServersDlg_Load);
            this.ResumeLayout(false);

		}
		#endregion

		private void ServersDlg_Load(object sender, System.EventArgs e)
		{
			FillList();

            if (this.FirstRun == true)
            {
                // this.toolTip_firstUse.Show("�밴�˰�ť����һ���µķ�����Ŀ��", this.button_newServer);
                ShowMessageTip();
            }
		}

		private void button_OK_Click(object sender, System.EventArgs e)
		{
			// OK��Cancel�˳����Ի���,��ʵ Servers�е������Ѿ��޸ġ�
			// Ϊ����Cancel�˳��з��������޸ĵ�Ч����������ڳ�ʼ��Servers
			// ���Ե�ʱ����һ����¡��ServerCollection����
		
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void button_Cancel_Click(object sender, System.EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		void FillList()
		{
			listView1.Items.Clear();

			if (Servers == null)
				return;

			for(int i = 0;i<Servers.Count; i++)
			{
                dp2Server server = (dp2Server)Servers[i];

				ListViewItem item = new ListViewItem(server.Name, 0);

				listView1.Items.Add(item);

                item.SubItems.Add(server.Url);
				item.SubItems.Add(server.DefaultUserName);
				item.SubItems.Add(server.SavePassword == true ? "��" : "��");

			}


		}

		private void listView1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if(e.Button != MouseButtons.Right)
				return;

			ContextMenu contextMenu = new ContextMenu();
			MenuItem menuItem = null;

			bool bSelected = listView1.SelectedItems.Count > 0;

			//
			menuItem = new MenuItem("�޸�(&M)");
			menuItem.Click += new System.EventHandler(this.menu_modifyServer);
			if (bSelected == false) 
			{
				menuItem.Enabled = false;
			}
			contextMenu.MenuItems.Add(menuItem);


			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);


			menuItem = new MenuItem("ɾ��(&D)");
			menuItem.Click += new System.EventHandler(this.menu_deleteServer);
			if (bSelected == false)
				menuItem.Enabled = false;
			contextMenu.MenuItems.Add(menuItem);

			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);


			//
			menuItem = new MenuItem("����(&N)");
			menuItem.Click += new System.EventHandler(this.menu_newServer);
			contextMenu.MenuItems.Add(menuItem);

	
			contextMenu.Show(listView1, new Point(e.X, e.Y) );		
		}

		void menu_deleteServer(object sender, System.EventArgs e)
		{
			if (listView1.SelectedIndices.Count == 0)
			{
				MessageBox.Show(this, "��δѡ��Ҫɾ�������� ...");
				return;
			}

			DialogResult msgResult = MessageBox.Show(this,
				"ȷʵҪɾ����ѡ�������",
				"ServersDlg",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button2);

			if (msgResult != DialogResult.Yes) 
			{
				return;
			}

			for(int i=listView1.SelectedIndices.Count-1;i>=0;i--)
			{
				Servers.RemoveAt(listView1.SelectedIndices[i]);
			}

			Servers.Changed = true;

			FillList();

			m_bChanged = true;
		}
		

		void menu_modifyServer(object sender, System.EventArgs e)
		{
			if (listView1.SelectedIndices.Count == 0)
			{
				MessageBox.Show(this, "��δѡ��Ҫ�޸ĵ����� ...");
				return;
			}


			int nActiveLine = listView1.SelectedIndices[0];
			// ListViewItem item = listView1.Items[nActiveLine];

            ServerDlg dlg = new ServerDlg();
            // GuiUtil.AutoSetDefaultFont(dlg); 
            GuiUtil.SetControlFont(dlg, this.Font);

			dlg.Text = "�޸�ȱʡ�ʻ�����";

            dlg.ServerName = ((dp2Server)Servers[nActiveLine]).Name;
            dlg.Password = ((dp2Server)Servers[nActiveLine]).DefaultPassword;
            dlg.ServerUrl = ((dp2Server)Servers[nActiveLine]).Url;
            dlg.UserName = ((dp2Server)Servers[nActiveLine]).DefaultUserName;
            dlg.SavePassword = ((dp2Server)Servers[nActiveLine]).SavePassword;

			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;

            ((dp2Server)Servers[nActiveLine]).Name = dlg.ServerName;
            ((dp2Server)Servers[nActiveLine]).DefaultPassword = dlg.Password;
            ((dp2Server)Servers[nActiveLine]).Url = dlg.ServerUrl;
            ((dp2Server)Servers[nActiveLine]).DefaultUserName = dlg.UserName;
            ((dp2Server)Servers[nActiveLine]).SavePassword = dlg.SavePassword;

			Servers.Changed = true;

			FillList();

		// ѡ��һ��
		// parameters:
		//		nIndex	Ҫ����ѡ���ǵ��С����==-1����ʾ���ȫ��ѡ���ǵ���ѡ��
		//		bMoveFocus	�Ƿ�ͬʱ�ƶ�focus��־����ѡ����
			ListViewUtil.SelectLine(listView1, 
				nActiveLine,
				true);

			m_bChanged = true;

		}


		void menu_newServer(object sender, System.EventArgs e)
		{
			int nActiveLine = -1;
			if (listView1.SelectedIndices.Count != 0)
			{
				nActiveLine = listView1.SelectedIndices[0];
			}

			ServerDlg dlg = new ServerDlg();
            // GuiUtil.AutoSetDefaultFont(dlg); 
            GuiUtil.SetControlFont(dlg, this.Font);

			dlg.Text = "������������ַ��ȱʡ�ʻ�";

            if (nActiveLine == -1)
            {   
                // �޲ο��������ε�����
                dlg.ServerName = "���Ժ���ϱ�Ŀ����";
                dlg.ServerUrl = "http://ssucs.org/dp2library";
                dlg.UserName = "test";
            }
            else
			{
                dlg.ServerName = ((dp2Server)Servers[nActiveLine]).Name;
                dlg.Password = ((dp2Server)Servers[nActiveLine]).DefaultPassword;
                dlg.ServerUrl = ((dp2Server)Servers[nActiveLine]).Url;
                dlg.UserName = ((dp2Server)Servers[nActiveLine]).DefaultUserName;
                dlg.SavePassword = ((dp2Server)Servers[nActiveLine]).SavePassword;
			}

			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;

            dp2Server server = Servers.NewServer(nActiveLine);
            server.Name = dlg.ServerName;
			server.DefaultPassword = dlg.Password;
			server.Url = dlg.ServerUrl;
			server.DefaultUserName = dlg.UserName;
			server.SavePassword = dlg.SavePassword;

			Servers.Changed = true;

			FillList();

			// ѡ��һ��
			// parameters:
			//		nIndex	Ҫ����ѡ���ǵ��С����==-1����ʾ���ȫ��ѡ���ǵ���ѡ��
			//		bMoveFocus	�Ƿ�ͬʱ�ƶ�focus��־����ѡ����
			ListViewUtil.SelectLine(listView1, 
				Servers.Count - 1,
				true);

			m_bChanged = true;

		}


		private void listView1_DoubleClick(object sender, System.EventArgs e)
		{
			menu_modifyServer(null, null);
		}

		private void ServersDlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (this.DialogResult != DialogResult.OK)
			{
				if (m_bChanged == true)
				{
					DialogResult msgResult = MessageBox.Show(this,
						"Ҫ�����ڶԻ�����������ȫ���޸�ô?",
						"ServersDlg",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question,
						MessageBoxDefaultButton.Button2);
					if (msgResult == DialogResult.No) 
					{
						e.Cancel = true;
						return;
					}
				}
			}

		}

        private void button_newServer_Click(object sender, EventArgs e)
        {
            HideMessageTip();

            menu_newServer(null, null);
        }

        void ShowMessageTip()
        {
            m_firstUseBalloon = new MessageBalloon();
            m_firstUseBalloon.Parent = this.button_newServer;
            m_firstUseBalloon.Title = "��ӭʹ��dp2��Ŀǰ��";
            m_firstUseBalloon.TitleIcon = TooltipIcon.Info;
            m_firstUseBalloon.Text = "�밴�˰�ť������һ�� dp2library ������Ŀ��";

            m_firstUseBalloon.Align = BalloonAlignment.BottomRight;
            m_firstUseBalloon.CenterStem = false;
            m_firstUseBalloon.UseAbsolutePositioning = false;
            m_firstUseBalloon.Show();
        }

        void HideMessageTip()
        {
            if (m_firstUseBalloon == null)
                return;

            m_firstUseBalloon.Dispose();
            m_firstUseBalloon = null;
        }
	}
}
