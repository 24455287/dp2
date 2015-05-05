using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;

using DigitalPlatform.Xml;
using DigitalPlatform.rms.Client;

namespace DigitalPlatform.Library
{
	/// <summary>
	/// ��dup�����ļ��л�ȡ�������ĶԻ���
	/// </summary>
	public class GetDupProjectNameDlg : System.Windows.Forms.Form
	{
        /// <summary>
        /// �������
        /// </summary>
        public SearchPanel SearchPanel = null;

        /// <summary>
        /// cfgs/dup�����ļ�����
        /// </summary>
		public XmlDocument DomDupCfg = null;

		private System.Windows.Forms.ListView listView_projectNames;
		private System.Windows.Forms.ColumnHeader columnHeader_name;
		private System.Windows.Forms.ColumnHeader columnHeader_comment;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBox_projectName;
		private System.Windows.Forms.Button button_OK;
		private System.Windows.Forms.Button button_Cancel;
        private Label label2;
        private TextBox textBox_serverUrl;
        private Button button_findServerUrl;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        /// <summary>
        /// ���캯��
        /// </summary>
		public GetDupProjectNameDlg()
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GetDupProjectNameDlg));
            this.listView_projectNames = new System.Windows.Forms.ListView();
            this.columnHeader_name = new System.Windows.Forms.ColumnHeader();
            this.columnHeader_comment = new System.Windows.Forms.ColumnHeader();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_projectName = new System.Windows.Forms.TextBox();
            this.button_OK = new System.Windows.Forms.Button();
            this.button_Cancel = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_serverUrl = new System.Windows.Forms.TextBox();
            this.button_findServerUrl = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listView_projectNames
            // 
            this.listView_projectNames.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listView_projectNames.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader_name,
            this.columnHeader_comment});
            this.listView_projectNames.FullRowSelect = true;
            this.listView_projectNames.HideSelection = false;
            this.listView_projectNames.Location = new System.Drawing.Point(19, 75);
            this.listView_projectNames.MultiSelect = false;
            this.listView_projectNames.Name = "listView_projectNames";
            this.listView_projectNames.Size = new System.Drawing.Size(424, 225);
            this.listView_projectNames.TabIndex = 0;
            this.listView_projectNames.UseCompatibleStateImageBehavior = false;
            this.listView_projectNames.View = System.Windows.Forms.View.Details;
            this.listView_projectNames.SelectedIndexChanged += new System.EventHandler(this.listView_projectNames_SelectedIndexChanged);
            this.listView_projectNames.DoubleClick += new System.EventHandler(this.listView_projectNames_DoubleClick);
            // 
            // columnHeader_name
            // 
            this.columnHeader_name.Text = "������";
            this.columnHeader_name.Width = 109;
            // 
            // columnHeader_comment
            // 
            this.columnHeader_comment.Text = "ע��";
            this.columnHeader_comment.Width = 335;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 316);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "������(&N):";
            // 
            // textBox_projectName
            // 
            this.textBox_projectName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_projectName.Location = new System.Drawing.Point(123, 312);
            this.textBox_projectName.Name = "textBox_projectName";
            this.textBox_projectName.Size = new System.Drawing.Size(320, 25);
            this.textBox_projectName.TabIndex = 2;
            // 
            // button_OK
            // 
            this.button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_OK.Location = new System.Drawing.Point(235, 347);
            this.button_OK.Name = "button_OK";
            this.button_OK.Size = new System.Drawing.Size(100, 30);
            this.button_OK.TabIndex = 3;
            this.button_OK.Text = "ȷ��";
            this.button_OK.Click += new System.EventHandler(this.button_OK_Click);
            // 
            // button_Cancel
            // 
            this.button_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Cancel.Location = new System.Drawing.Point(343, 347);
            this.button_Cancel.Name = "button_Cancel";
            this.button_Cancel.Size = new System.Drawing.Size(100, 30);
            this.button_Cancel.TabIndex = 4;
            this.button_Cancel.Text = "����";
            this.button_Cancel.Click += new System.EventHandler(this.button_Cancel_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 15);
            this.label2.TabIndex = 5;
            this.label2.Text = "��������(&S):";
            // 
            // textBox_serverUrl
            // 
            this.textBox_serverUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_serverUrl.Location = new System.Drawing.Point(19, 37);
            this.textBox_serverUrl.Name = "textBox_serverUrl";
            this.textBox_serverUrl.Size = new System.Drawing.Size(372, 25);
            this.textBox_serverUrl.TabIndex = 6;
            // 
            // button_findServerUrl
            // 
            this.button_findServerUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_findServerUrl.Location = new System.Drawing.Point(399, 37);
            this.button_findServerUrl.Name = "button_findServerUrl";
            this.button_findServerUrl.Size = new System.Drawing.Size(44, 30);
            this.button_findServerUrl.TabIndex = 7;
            this.button_findServerUrl.Text = "...";
            this.button_findServerUrl.UseVisualStyleBackColor = true;
            this.button_findServerUrl.Click += new System.EventHandler(this.button_findServerUrl_Click);
            // 
            // GetDupProjectNameDlg
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(8, 18);
            this.ClientSize = new System.Drawing.Size(459, 387);
            this.Controls.Add(this.button_findServerUrl);
            this.Controls.Add(this.textBox_serverUrl);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button_Cancel);
            this.Controls.Add(this.button_OK);
            this.Controls.Add(this.textBox_projectName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listView_projectNames);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GetDupProjectNameDlg";
            this.Text = "��ָ�����ط�����";
            this.Load += new System.EventHandler(this.GetProjectNameDlg_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		private void GetProjectNameDlg_Load(object sender, System.EventArgs e)
		{

			FillList();
		
		}

		void FillList()
		{
			this.listView_projectNames.Items.Clear();

			if (this.DomDupCfg == null)
				return;

			XmlNodeList nodes = this.DomDupCfg.SelectNodes("//project");
			for(int i=0;i<nodes.Count;i++)
			{
				XmlNode node = nodes[i];

				string strName = DomUtil.GetAttr(node, "name");
				string strComment = DomUtil.GetAttr(node, "comment");

				ListViewItem item = new ListViewItem(strName, 0);

				item.SubItems.Add(strComment);

				this.listView_projectNames.Items.Add(item);

				if (strName == this.ProjectName)
					item.Selected = true;
			}

		}

		private void button_OK_Click(object sender, System.EventArgs e)
		{
			if (this.textBox_projectName.Text == "")
			{
				MessageBox.Show(this, "��δָ��������");
				return;
			}

			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void button_Cancel_Click(object sender, System.EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void listView_projectNames_DoubleClick(object sender, System.EventArgs e)
		{
			button_OK_Click(this, null);
		
		}

		private void listView_projectNames_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (this.listView_projectNames.SelectedItems.Count > 0)
				this.textBox_projectName.Text = this.listView_projectNames.SelectedItems[0].Text;
			else
				this.textBox_projectName.Text = "";
		}

        /// <summary>
        /// ������
        /// </summary>
		public string ProjectName
		{
			get
			{
				return this.textBox_projectName.Text;
			}
			set
			{
				this.textBox_projectName.Text = value;
			}
		}

        /// <summary>
        /// ��������URL
        /// </summary>
        public string ServerUrl
        {
            get
            {
                return this.textBox_serverUrl.Text;
            }
            set
            {
                this.textBox_serverUrl.Text = value;
            }
        

        }

        private void button_findServerUrl_Click(object sender, EventArgs e)
        {
            OpenResDlg dlg = new OpenResDlg();

            dlg.Text = "��ѡ����������";
            dlg.EnabledIndices = new int[] { ResTree.RESTYPE_SERVER };
            dlg.ap = this.SearchPanel.ap;
            dlg.ApCfgTitle = "findServerUrl_openresdlg";
            dlg.MultiSelect = false;
            dlg.Path = this.textBox_serverUrl.Text;
            dlg.Initial(this.SearchPanel.Servers,
                this.SearchPanel.Channels);
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            textBox_serverUrl.Text = dlg.Path;

            // ���list����
            if (this.ServerUrl != "")
            {
                int nRet = 0;
                string strError = "";

                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                try
                {
                    nRet = GetDupCfgFile(out strError);
                }
                finally
                {
                    this.Cursor = oldCursor;
                }

                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return;
                }

                FillList();

                this.textBox_projectName.Text = "";
            }
        }

        // �ӷ������ϻ�ȡdup�����ļ�
        int GetDupCfgFile(out string strError)
        {
            strError = "";

            if (this.DomDupCfg != null)
                return 0;	// �Ż�

            if (this.textBox_serverUrl.Text == "")
            {
                strError = "��δָ��������URL";
                return -1;
            }

            string strCfgFilePath = "cfgs/dup";
            XmlDocument tempdom = null;
            // ��������ļ�
            // return:
            //		-1	error
            //		0	not found
            //		1	found
            int nRet = this.SearchPanel.GetCfgFile(
                this.textBox_serverUrl.Text,
                strCfgFilePath,
                out tempdom,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "�����ļ� '" + strCfgFilePath + "' û���ҵ�...";
                return -1;
            }

            this.DomDupCfg = tempdom;

            return 0;
        }


	}
}
