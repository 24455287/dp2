using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using System.Threading;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.GUI;
using DigitalPlatform.Text;

namespace DigitalPlatform.Library
{
	/// <summary>
	/// ���ضԻ���
	/// </summary>
	public class DupDlg : System.Windows.Forms.Form
	{
        OneHit m_hit = null;

        string m_strWeightList = "";  // ԭʼ��weight���壬���ŷָ���б�

        /*
        string m_strSearchStyle = "";
		int m_nCurWeight = 0;
		int m_nThreshold = 0;
		string m_strSearchReason = "";	// ����ϸ����Ϣ
         * */

		Hashtable m_tableItem = new Hashtable();

		SearchPanel SearchPanel = null;

        /// <summary>
        /// ��������
        /// </summary>
		public AutoResetEvent EventFinish = new AutoResetEvent(false);

		bool m_bAutoBeginSearch = false;

        /// <summary>
        /// ��Щ��¼��Ҫװ�������Ϣ��
        /// </summary>
		public LoadBrowse LoadBrowse = LoadBrowse.All;

        /// <summary>
        /// ����ϸ��
        /// </summary>
		public event OpenDetailEventHandler OpenDetail = null;

		XmlDocument domDupCfg = null;

		string m_strRecord = "";

		private System.Windows.Forms.Button button_findServerUrl;
		private System.Windows.Forms.TextBox textBox_serverUrl;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label_message;
		private System.Windows.Forms.Button button_stop;
		private System.Windows.Forms.Button button_search;
		private System.Windows.Forms.ColumnHeader columnHeader_path;
		private System.Windows.Forms.ColumnHeader columnHeader_sum;

        /// <summary>
        /// ��������������м�¼��ListView
        /// </summary>
		public ListView listView_browse;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBox_recordPath;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBox_projectName;
		private System.Windows.Forms.Button button_findProjectName;
		private System.Windows.Forms.ColumnHeader columnHeader_searchComment;
		private System.Windows.Forms.ToolTip toolTip_searchComment;
		private System.Windows.Forms.Label label_dupMessage;
		private System.ComponentModel.IContainer components;

        /// <summary>
        /// ���캯��
        /// </summary>
		public DupDlg()
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DupDlg));
            this.button_findServerUrl = new System.Windows.Forms.Button();
            this.textBox_serverUrl = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label_message = new System.Windows.Forms.Label();
            this.button_stop = new System.Windows.Forms.Button();
            this.button_search = new System.Windows.Forms.Button();
            this.listView_browse = new System.Windows.Forms.ListView();
            this.columnHeader_path = new System.Windows.Forms.ColumnHeader();
            this.columnHeader_sum = new System.Windows.Forms.ColumnHeader();
            this.columnHeader_searchComment = new System.Windows.Forms.ColumnHeader();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_recordPath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_projectName = new System.Windows.Forms.TextBox();
            this.button_findProjectName = new System.Windows.Forms.Button();
            this.toolTip_searchComment = new System.Windows.Forms.ToolTip(this.components);
            this.label_dupMessage = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button_findServerUrl
            // 
            this.button_findServerUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_findServerUrl.Location = new System.Drawing.Point(495, 13);
            this.button_findServerUrl.Name = "button_findServerUrl";
            this.button_findServerUrl.Size = new System.Drawing.Size(42, 29);
            this.button_findServerUrl.TabIndex = 14;
            this.button_findServerUrl.Text = "...";
            this.button_findServerUrl.Click += new System.EventHandler(this.button_findServerUrl_Click);
            // 
            // textBox_serverUrl
            // 
            this.textBox_serverUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_serverUrl.Location = new System.Drawing.Point(152, 15);
            this.textBox_serverUrl.Name = "textBox_serverUrl";
            this.textBox_serverUrl.Size = new System.Drawing.Size(337, 25);
            this.textBox_serverUrl.TabIndex = 13;
            this.textBox_serverUrl.TextChanged += new System.EventHandler(this.textBox_serverUrl_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 19);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(99, 15);
            this.label3.TabIndex = 12;
            this.label3.Text = "��������(&S):";
            // 
            // label_message
            // 
            this.label_message.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label_message.Location = new System.Drawing.Point(16, 338);
            this.label_message.Name = "label_message";
            this.label_message.Size = new System.Drawing.Size(521, 30);
            this.label_message.TabIndex = 17;
            // 
            // button_stop
            // 
            this.button_stop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_stop.Enabled = false;
            this.button_stop.Location = new System.Drawing.Point(437, 120);
            this.button_stop.Name = "button_stop";
            this.button_stop.Size = new System.Drawing.Size(100, 29);
            this.button_stop.TabIndex = 16;
            this.button_stop.Text = "ֹͣ(&S)";
            this.button_stop.Click += new System.EventHandler(this.button_stop_Click);
            // 
            // button_search
            // 
            this.button_search.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_search.Location = new System.Drawing.Point(329, 120);
            this.button_search.Name = "button_search";
            this.button_search.Size = new System.Drawing.Size(100, 29);
            this.button_search.TabIndex = 15;
            this.button_search.Text = "����(&S)";
            this.button_search.Click += new System.EventHandler(this.button_search_Click);
            // 
            // listView_browse
            // 
            this.listView_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listView_browse.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader_path,
            this.columnHeader_sum,
            this.columnHeader_searchComment});
            this.listView_browse.FullRowSelect = true;
            this.listView_browse.HideSelection = false;
            this.listView_browse.Location = new System.Drawing.Point(16, 157);
            this.listView_browse.Name = "listView_browse";
            this.listView_browse.Size = new System.Drawing.Size(521, 130);
            this.listView_browse.TabIndex = 18;
            this.listView_browse.UseCompatibleStateImageBehavior = false;
            this.listView_browse.View = System.Windows.Forms.View.Details;
            this.listView_browse.DoubleClick += new System.EventHandler(this.listView_browse_DoubleClick);
            this.listView_browse.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_browse_MouseMove);
            // 
            // columnHeader_path
            // 
            this.columnHeader_path.Text = "��¼·��";
            this.columnHeader_path.Width = 93;
            // 
            // columnHeader_sum
            // 
            this.columnHeader_sum.Text = "Ȩֵ��";
            this.columnHeader_sum.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader_sum.Width = 70;
            // 
            // columnHeader_searchComment
            // 
            this.columnHeader_searchComment.Text = "��������";
            this.columnHeader_searchComment.Width = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 89);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(114, 15);
            this.label1.TabIndex = 19;
            this.label1.Text = "Դ��¼·��(&P):";
            // 
            // textBox_recordPath
            // 
            this.textBox_recordPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_recordPath.Location = new System.Drawing.Point(152, 85);
            this.textBox_recordPath.Name = "textBox_recordPath";
            this.textBox_recordPath.Size = new System.Drawing.Size(337, 25);
            this.textBox_recordPath.TabIndex = 20;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 15);
            this.label2.TabIndex = 21;
            this.label2.Text = "���ط���(&P):";
            // 
            // textBox_projectName
            // 
            this.textBox_projectName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_projectName.Location = new System.Drawing.Point(152, 50);
            this.textBox_projectName.Name = "textBox_projectName";
            this.textBox_projectName.Size = new System.Drawing.Size(337, 25);
            this.textBox_projectName.TabIndex = 22;
            // 
            // button_findProjectName
            // 
            this.button_findProjectName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_findProjectName.Location = new System.Drawing.Point(495, 48);
            this.button_findProjectName.Name = "button_findProjectName";
            this.button_findProjectName.Size = new System.Drawing.Size(42, 29);
            this.button_findProjectName.TabIndex = 23;
            this.button_findProjectName.Text = "...";
            this.button_findProjectName.Click += new System.EventHandler(this.button_findProjectName_Click);
            // 
            // toolTip_searchComment
            // 
            this.toolTip_searchComment.AutomaticDelay = 5000;
            // 
            // label_dupMessage
            // 
            this.label_dupMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label_dupMessage.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_dupMessage.Location = new System.Drawing.Point(16, 297);
            this.label_dupMessage.Name = "label_dupMessage";
            this.label_dupMessage.Size = new System.Drawing.Size(527, 30);
            this.label_dupMessage.TabIndex = 24;
            this.label_dupMessage.Text = "��δ����...";
            // 
            // DupDlg
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(8, 18);
            this.ClientSize = new System.Drawing.Size(553, 379);
            this.Controls.Add(this.label_dupMessage);
            this.Controls.Add(this.button_findProjectName);
            this.Controls.Add(this.textBox_projectName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox_recordPath);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listView_browse);
            this.Controls.Add(this.label_message);
            this.Controls.Add(this.button_stop);
            this.Controls.Add(this.button_search);
            this.Controls.Add(this.button_findServerUrl);
            this.Controls.Add(this.textBox_serverUrl);
            this.Controls.Add(this.label3);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DupDlg";
            this.ShowInTaskbar = false;
            this.Text = "DupDlg";
            this.Load += new System.EventHandler(this.DupDlg_Load);
            this.Closed += new System.EventHandler(this.DupDlg_Closed);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion


        /// <summary>
        /// ��������URL
        /// </summary>
        /// <remarks>���ڻ�ȡcfgs/dup�����ļ��ķ�����URL</remarks>
		public string ServerUrl
		{
			get 
			{
				return textBox_serverUrl.Text;
			}
			set
			{
				domDupCfg = null;
				textBox_serverUrl.Text = value;
			}
		}

        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="searchpanel">�������</param>
        /// <param name="strServerUrl">��������URL</param>
        /// <param name="bAutoBeginSearch">���Ի���򿪺��Ƿ��Զ���ʼ����</param>
		public void Initial(
			SearchPanel searchpanel,
			string strServerUrl,
			bool bAutoBeginSearch)
		{
			this.SearchPanel = searchpanel;

			this.SearchPanel.InitialStopManager(this.button_stop,
				this.label_message);

			this.ServerUrl = strServerUrl;

			this.m_bAutoBeginSearch = bAutoBeginSearch;
		}

        /// <summary>
        /// ������صļ�¼
        /// </summary>
		public string Record
		{
			get 
			{
				return m_strRecord;
			}
			set 
			{
				m_strRecord = value;
			}
		}

        /// <summary>
        /// ������صļ�¼·����id����Ϊ?����Ҫ����ģ���keys
        /// </summary>
		public string RecordFullPath
		{
			get 
			{
				return this.textBox_recordPath.Text;
			}
			set 
			{
				this.textBox_recordPath.Text = value;
				this.Text = "����: " + ResPath.GetReverseRecordPath(value);
			}
		}


        /// <summary>
        /// ������صļ�¼·�������ݿⲿ��
        /// </summary>
		public string OriginDbFullPath
		{
			get 
			{
				ResPath respath = new ResPath(this.textBox_recordPath.Text);

				return respath.Url + "?" + ResPath.GetDbName(respath.Path);
			}

		}

        /// <summary>
        /// ���ط�����
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
        /// �����������ϻ�ȡcfgs/dup�����ļ�
        /// </summary>
        /// <param name="strError">���صĳ�����Ϣ</param>
        /// <returns>
        /// <value>-1����</value>
        /// <value>0����</value>
        /// </returns>
		int GetDupCfgFile(out string strError)
		{
			strError = "";

			if (this.domDupCfg != null)
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

			this.domDupCfg = tempdom;

			return 0;
		}

		private void DupDlg_Load(object sender, System.EventArgs e)
		{
			object[] pList = new object []  { null, null };

			if (m_bAutoBeginSearch == true) 
			{
				this.BeginInvoke(new Delegate_Search(this.button_search_Click), pList);
			}

			// this.BackgroundImage = new Bitmap("f:\\cs\\dp1batch\\project_icon.bmp" );
			// this.BackgroundImage = GetBackImage();

			// this.listView_browse.BackgroundImage = "f:\\cs\\dp1batch\\project_icon.bmp";

			// this.listView_browse.BackImage = GetBackImage();

		}

		/*
		Bitmap GetBackImage()
		{
			// ��ʽ��ͼ��
			Bitmap bitmapDest = new Bitmap(200,
				200);

			Graphics objGraphics = Graphics.FromImage(bitmapDest);
			objGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
			objGraphics.SmoothingMode = SmoothingMode.AntiAlias;

			objGraphics.Clear(Color.White);// Color.Teal

			DrawText(objGraphics,
				"text",
				200,
				200);

			return bitmapDest;
		}

		static void DrawText(Graphics g,
			string strText,
			int nWidth,
			int nHeight)
		{

			Font font = new Font("Arial", 18, FontStyle.Bold);
			Brush brushText = null;

			brushText = new SolidBrush(Color.Red);
 
			StringFormat stringFormat = new StringFormat();

			stringFormat.Alignment = StringAlignment.Center;
			stringFormat.LineAlignment = StringAlignment.Center;
 
			RectangleF rect = new RectangleF(
				0,
				0,
				nWidth,
				nHeight);
			g.DrawString(strText,
				font, 
				brushText,
				rect,
				stringFormat);
		}
		*/
		

		delegate void Delegate_Search(object sender, EventArgs e);

        /// <summary>
        /// �ȴ���������
        /// </summary>
		public void WaitSearchFinish()
		{
			for(;;)
			{
				Application.DoEvents();
				bool bRet = this.EventFinish.WaitOne(10, true);
				if (bRet == true)
					break;
			}
		}

		private void DupDlg_Closed(object sender, System.EventArgs e)
		{
			EventFinish.Set();
		}

		private void button_search_Click(object sender, System.EventArgs e)
		{
			string strError = "";

			int nRet = DoSearch(out strError);
			if (nRet == -1)
			{
				MessageBox.Show(this, strError);
			}

			/*
			EventFinish.Reset();
			try 
			{

				this.listView_browse.Items.Clear();
				this.m_tableItem.Clear();

				if (this.ServerUrl == "")
				{
					strError = "��������URL��δָ��";
					goto ERROR1;
				}
				if (this.ProjectName == "")
				{
					strError = "���ط�������δָ��";
					goto ERROR1;
				}
				if (this.RecordFullPath == "")
				{
					strError = "Դ��¼·����δָ��";
					goto ERROR1;
				}
				if (this.Record == "")
				{
					strError = "Դ��¼������δָ��";
					goto ERROR1;
				}

				// �ӷ������ϻ�ȡdup�����ļ�
				int nRet = GetDupCfgFile(out strError);
				if (nRet == -1)
					goto ERROR1;

				// ���project name�Ƿ����
				XmlNode nodeProject = GetProjectNode(this.ProjectName,
					out strError);
				if (nodeProject == null)
					goto ERROR1;

				// ����Դ��¼·��
				ResPath respath = new ResPath(this.RecordFullPath);

				ArrayList aLine = null;	// AccessKeyInfo��������
				// ���keys
				// ģ�ⴴ��������
				// return:
				//		-1	һ�����
				//		0	����
				nRet = this.SearchPanel.GetKeys(
					respath.Url,
					respath.Path,
					this.Record,
					out aLine,
					out strError);
				if (nRet == -1)
					goto ERROR1;

				nRet = 	LoopSearch(
					nodeProject,
					aLine,
					out strError);
				if (nRet == -1)
					goto ERROR1;

				// ����
				this.SearchPanel.BeginLoop("��������");
				try 
				{
					this.listView_browse.ListViewItemSorter = new ListViewItemComparer();
				}
				finally 
				{
					this.SearchPanel.EndLoop();
				}


				// ��������Ϣ
				this.SearchPanel.BeginLoop("���ڻ�ȡ�������Ϣ ...");
				try 
				{
					nRet = GetBrowseColumns(out strError);
					if (nRet == -1)
						goto ERROR1;
				}
				finally 
				{
					this.SearchPanel.EndLoop();
				}


				// MessageBox.Show(this, "OK");	// �㱨�������

				return;
			}
			finally 
			{
				EventFinish.Set();
			}
			
		
			ERROR1:
				MessageBox.Show(this, strError);
			*/
		}


        /// <summary>
        /// ����
        /// </summary>
        /// <param name="strError">���صĴ�����Ϣ</param>
        /// <returns>-1����;0����</returns>
		public int DoSearch(out string strError)
		{
			strError = "";

			EventFinish.Reset();
			try 
			{

				this.listView_browse.Items.Clear();
				this.m_tableItem.Clear();

				if (this.ServerUrl == "")
				{
					strError = "��������URL��δָ��";
					goto ERROR1;
				}
				if (this.ProjectName == "")
				{
					strError = "���ط�������δָ��";
					goto ERROR1;
				}
				if (this.RecordFullPath == "")
				{
					strError = "Դ��¼·����δָ��";
					goto ERROR1;
				}
				if (this.Record == "")
				{
					strError = "Դ��¼������δָ��";
					goto ERROR1;
				}

				// �ӷ������ϻ�ȡdup�����ļ�
				int nRet = GetDupCfgFile(out strError);
				if (nRet == -1)
					goto ERROR1;

				if (this.ProjectName == "{default}")
				{
					ResPath respathtemp = new ResPath(this.RecordFullPath);

					string strOriginDbFullPath = respathtemp.Url + "?" + ResPath.GetDbName(respathtemp.Path);
					string strDefaultProjectName = "";
					nRet = GetDefaultProjectName(strOriginDbFullPath,
						out strDefaultProjectName,
						out strError);
					if (nRet == -1)
						goto ERROR1;
					if (nRet == 0)
					{
						strError = "���ط���� '" + strOriginDbFullPath + "' ��δ����ȱʡ���ط�������(����dup�����ļ�����<default>Ԫ�ض���)��\r\n�����'���ط���'textbox�ұߵ�'...'��ťָ����һ��ʵ�ڵĲ��ط����������в��ء�";
						goto ERROR1;
					}
					Debug.Assert(nRet == 1, "");
					this.ProjectName = strDefaultProjectName;
				}

				// ���project name�Ƿ����
				XmlNode nodeProject = GetProjectNode(this.ProjectName,
					out strError);
				if (nodeProject == null)
					goto ERROR1;

				// ����Դ��¼·��
				ResPath respath = new ResPath(this.RecordFullPath);

                List<AccessKeyInfo> aLine = null;	// AccessKeyInfo��������
				// ���keys
				// ģ�ⴴ��������
				// return:
				//		-1	һ�����
				//		0	����
				nRet = this.SearchPanel.GetKeys(
					respath.Url,
					respath.Path,
					this.Record,
					out aLine,
					out strError);
				if (nRet == -1)
					goto ERROR1;

				nRet = 	LoopSearch(
					nodeProject,
					aLine,
					out strError);
				if (nRet == -1)
					goto ERROR1;

				// ����
				this.SearchPanel.BeginLoop("��������");
				try 
				{
					this.listView_browse.ListViewItemSorter = new ListViewItemComparer();
				}
				finally 
				{
					this.SearchPanel.EndLoop();
				}

				SetDupState();

				// ��������Ϣ
				this.SearchPanel.BeginLoop("���ڻ�ȡ�������Ϣ ...");
				try 
				{
					nRet = GetBrowseColumns(out strError);
					if (nRet == -1)
						goto ERROR1;
				}
				finally 
				{
					this.SearchPanel.EndLoop();
				}
				return 0;
			}
			finally 
			{
				EventFinish.Set();
			}
		
			ERROR1:
				return -1;
		}


        /// <summary>
        /// ��ò��ؽ������¼ȫ·���ļ���
        /// </summary>
		public string[] DupPaths
		{
			get 
			{
				int i;
				ArrayList aPath = new ArrayList();
				for(i=0;i<this.listView_browse.Items.Count;i++)
				{
					string strText = this.listView_browse.Items[i].SubItems[1].Text;

					if (strText.Length > 0 && strText[0] == '*')
					{
						aPath.Add(ResPath.GetRegularRecordPath(this.listView_browse.Items[i].Text));
					}
					else
						break;
				}

				if (aPath.Count == 0)
					return new string[0];

				string [] result = new string[aPath.Count];
				for(i=0;i<aPath.Count;i++)
				{
					result[i] = (string)aPath[i];
				}

				return result;
			}
		}

		// ���ò���״̬
		void SetDupState()
		{
			int nCount = 0;
			for(int i=0;i<this.listView_browse.Items.Count;i++)
			{
				string strText = this.listView_browse.Items[i].SubItems[1].Text;

				if (strText.Length > 0 && strText[0] == '*')
					nCount ++;
				else
					break;
			}

			if (nCount > 0)
				this.label_dupMessage.Text = "�� " +Convert.ToString(nCount)+ " ���ظ���¼��";
			else
				this.label_dupMessage.Text = "û���ظ���¼��";

		}

		// ���һ��������Ӧ��ȱʡ���ط�����
		int GetDefaultProjectName(string strFromDbFullPath,
			out string strDefaultProjectName,
			out string strError)
		{
			strDefaultProjectName = "";
			strError = "";

			if (this.domDupCfg == null)
			{
				strError = "�����ļ�dom��δ��ʼ��";
				return -1;
			}

			ResPath respath = new ResPath(strFromDbFullPath);


			XmlNode node = this.domDupCfg.SelectSingleNode("//default[@origin='"+strFromDbFullPath+"']");
			if (node == null)
			{
				node = this.domDupCfg.SelectSingleNode("//default[@origin='"+respath.Path+"']");
			}

			if (node == null)
				return 0;	// not found

			strDefaultProjectName = DomUtil.GetAttr(node, "project");

			return 1;
		}

		// ѭ������
		int LoopSearch(
			XmlNode nodeProject,
            List<AccessKeyInfo> aLine,
			out string strError)
		{
			strError = "";
			int nRet = 0;

			if (nodeProject == null)
			{
				strError = "nodeProject��������Ϊnull";
				return -1;
			}

            Hashtable threshold_table = new Hashtable();    // ���ݿ�������ֵ�Ķ��ձ�
            Hashtable keyscount_table = new Hashtable();    // �����¼��ÿ��from��������key����Ŀ ���ձ�hashtable key����̬ΪstrDbName + "|" + strFrom

			XmlNodeList databases = nodeProject.SelectNodes("database");

			// <database>ѭ��
			for(int i=0;i<databases.Count;i++)
			{
				XmlNode database = databases[i];

				string strName = DomUtil.GetAttr(database, "name");
				if (strName == "")
					continue;

				string strThreshold = DomUtil.GetAttr(database, "threshold");

				int nThreshold = 0;
				try 
				{
					nThreshold = Convert.ToInt32(strThreshold);
				}
				catch
				{
                    strError = "nameΪ '"+strName+"' ��<database>Ԫ����threshold����ֵ '" + strThreshold + "' ��ʽ����ȷ��ӦΪ������";
                    return -1;
				}

                threshold_table[strName] = nThreshold;

				string strUrl = "";
				string strDbName = "";
				// �����URL�Ϳ���
				nRet = strName.IndexOf("?");
				if (nRet == -1)
				{
					strUrl = this.ServerUrl;	// ��ǰ��������
					strDbName = strName;
				}
				else 
				{
					strUrl = strName.Substring(0, nRet);
					strDbName = strName.Substring(nRet + 1);
				}

				XmlNodeList accesspoints = database.SelectNodes("accessPoint");
				// <accessPoint>ѭ��
				for(int j = 0;j<accesspoints.Count;j++)
				{
					XmlNode accesspoint = accesspoints[j];

					string strFrom = DomUtil.GetAttr(accesspoint, "name");

					// ���from����Ӧ��key
                    List<string> keys = GetKeysByFrom(aLine,
						strFrom);
					if (keys.Count == 0)
						continue;

                    keyscount_table[strDbName + "|" + strFrom] = keys.Count;

					string strWeight = DomUtil.GetAttr(accesspoint, "weight");
					string strSearchStyle = DomUtil.GetAttr(accesspoint, "searchStyle");

                    /*
					int nWeight = 0;
					try 
					{
						nWeight = Convert.ToInt32(strWeight);
					}
					catch
					{
						// ���涨������?
					}*/

					for(int k=0;k<keys.Count;k++)
					{
						string strKey = (string)keys[k];
						if (strKey == "")
							continue;

						// ����һ��from
						nRet = SearchOneFrom(
							strUrl,
							strDbName,
							strFrom,
							strKey,
							strSearchStyle,
							strWeight,
							// nThreshold,
							5000,
							out strError);
						if (nRet == -1)
						{
							// ??? �����������?
						}
					}

				}

                // ������һ�����ݿ���
			}

            // ��listview��ÿ����ʾ����
  			Color color = Color.FromArgb(255,255,200);

            for (int i = 0; i < this.listView_browse.Items.Count; i++)
            {
                ListViewItem item = this.listView_browse.Items[i];
                ItemInfo info = (ItemInfo)item.Tag;
                Debug.Assert(info != null, "");

                // ��ÿ���
                ResPath respath = new ResPath(ResPath.GetRegularRecordPath(item.Text));
                string strDbName = respath.GetDbName();


                int nWeight = AddWeight(
                    keyscount_table,
                    strDbName,
                    info.Hits);

                // ��õ�ǰ���threshold
                int nThreshold = (int)threshold_table[strDbName];

                string strNumber = nWeight.ToString();
                if (nWeight >= nThreshold)
                {
                    strNumber = "*" + strNumber;
                    item.BackColor = color;
                }

                ListViewUtil.ChangeItemText(item, 1, strNumber);
                ListViewUtil.ChangeItemText(item, 2, BuildComment(info.Hits));
            }

			return 0;
		}

        // �Ȱ��ո��������ۼӸ��Ե�weight��Ȼ�������weight
        static int AddWeight(
            Hashtable keyscount_table,
            string strDbName,
            List<OneHit> hits)
        {
            Hashtable weight_table = new Hashtable();

            int nWeight = 0;    // û�������������Ȩֵ�ۼ�
            // �ۼӷ���
            for (int i = 0; i < hits.Count; i++)
            {
                OneHit hit = hits[i];

                if (StringUtil.IsInList("average", hit.SearchStyle) == true)
                {
                    OneFromWeights one = (OneFromWeights)weight_table[hit.From];
                    if (one == null)
                    {
                        one = new OneFromWeights();
                        one.From = hit.From;

                        weight_table[hit.From] = one;
                    }

                    one.Weights += hit.Weight;
                    one.Hits++;
                }
                else
                    nWeight += hit.Weight;
            }

            // �ۼ��������weights
            foreach (string key in weight_table.Keys)
            {
                OneFromWeights one = (OneFromWeights)weight_table[key];
                Debug.Assert(one != null, "");

                // ��ø�from��keyscount
                int nKeysCount = (int)keyscount_table[strDbName + "|" + one.From];

                Debug.Assert(nKeysCount != 0, "");    // ��ֹ��0��
                nWeight += one.Weights / nKeysCount;
            }

            return nWeight;
        }

        static string BuildComment(List<OneHit> hits)
        {
            string strResult = "";
            for (int i = 0; i < hits.Count; i++)
            {
                OneHit hit = hits[i];

                if (String.IsNullOrEmpty(strResult) == false)
                    strResult += ";";
                strResult += "key='" + hit.Key + "', from='" + hit.From + "', weight=" + hit.Weight.ToString();
                if (String.IsNullOrEmpty(hit.SearchStyle) == false)
                    strResult += ", searchStyle=" + hit.SearchStyle;
            }

            return strResult;
        }

        // һ��������Ȩֵ�ܺͣ��Լ�������
        class OneFromWeights
        {
            public string From = "";    // ����;����
            public int Weights = 0; // �ܵ�weight
            public int Hits = 0;    // ����������(����)
        }

		// ��ģ��keys�и���from��ö�Ӧ��key
		List<string> GetKeysByFrom(List<AccessKeyInfo> aLine,
			string strFromName)
		{
			List<string> aResult = new List<string>();
			for(int i=0;i<aLine.Count;i++)
			{
				AccessKeyInfo info = aLine[i];
				if (info.FromName == strFromName)
					aResult.Add(info.Key);
			}

			return aResult;
		}

        // ���ַ�������ѡ��XML����ʽר�õ�search style
        // Ҳ���� exact left middle right�����ȱʡ����Ϊ����exact
        // ����ж�����õ�ֵ�����һ��������
        static string GetFirstQuerySearchStyle(string strText)
        {
            string[] parts = strText.Split(new char[] {','});
            for (int i = 0; i < parts.Length; i++)
            {
                string strStyle = parts[i].Trim().ToLower();
                if (strStyle == "exact"
                    || strStyle == "left"
                    || strStyle == "middle"
                    || strStyle == "right")
                    return strStyle;
            }

            return "exact";
        }

        // ���һ��from���м���
        int SearchOneFrom(
            string strServerUrl,
            string strDbName,
			string strFrom,
			string strKey,
			string strSearchStyle,
            string strWeight,
			// int nThreshold,
			long nMax,
			out string strError)
        {

            this.SearchPanel.BrowseRecord -= new BrowseRecordEventHandler(BrowseRecordNoColsCallBack);
            this.SearchPanel.BrowseRecord += new BrowseRecordEventHandler(BrowseRecordNoColsCallBack);

            try
            {

                /*
                if (strSearchStyle == "")
                    strSearchStyle = "exact";
                 * */

                /*
                this.m_strSearchStyle = strSearchStyle; // 2009/3/2 new add
				this.m_nCurWeight = nWeight;	// Ϊ�¼���������Ԥ��
				this.m_nThreshold = nThreshold;
                 * */


                // 2007/4/5 ���� ������ GetXmlStringSimple()
                string strQueryXml = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 new add
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strKey)
                    + "</word><match>" + GetFirstQuerySearchStyle(strSearchStyle) + "</match><relation>=</relation><dataType>string</dataType><maxCount>" + Convert.ToString(nMax) + "</maxCount></item><lang>zh</lang></target>";

                this.SearchPanel.BeginLoop("������Կ� '" + strDbName + "' ���� '" + strKey + "'");

                this.m_hit = new OneHit();
                this.m_hit.Key = strKey;
                this.m_hit.From = strFrom;
                this.m_hit.SearchStyle = strSearchStyle;

                this.m_strWeightList = strWeight;
                // this.m_strSearchReason = "key='" + strKey + "', from='" +strFrom + "', weight=" + Convert.ToString(nWeight);

                long lRet = 0;

                try
                {
                    // return:
                    //		-2	�û��ж�
                    //		-1	һ�����
                    //		0	δ����
                    //		>=1	����������������������
                    lRet = this.SearchPanel.SearchAndBrowse(
                        strServerUrl,
                        strQueryXml,
                        false,
                        out strError);

                    return (int)lRet;
                }
                finally
                {
                    this.SearchPanel.EndLoop();
                }
            }
            finally
            {
                this.SearchPanel.BrowseRecord -= new BrowseRecordEventHandler(BrowseRecordNoColsCallBack);
            }
        }


		void BrowseRecordNoColsCallBack(object sender, BrowseRecordEventArgs e)
		{
			string strError = "";

			if (e.FullPath == this.RecordFullPath)
				return;	// ��ǰ��¼�Լ�����Ҫװ�������

			int nRet = FillList(
				e.FullPath,
                this.m_hit,
                this.m_strWeightList,
                /*
                this.m_strSearchStyle,
				this.m_nCurWeight,
				this.m_nThreshold,
				this.m_strSearchReason,
                 * */
				out strError);
			if (nRet == -1) 
			{
				e.Cancel = true;
				e.ErrorInfo = strError;
			}
		}

        // ����б�
        // parameters:
        //      strReason   ��������ע��
        //      hit_param   Я���˲���������Ҫ�����ƺ󣬽��¶���������
		int FillList(
			string strFullPath,
            OneHit hit_param,
            string strWeightList,
            /*
            string strSearchStyle,
			int nCurWeight,
			int nThreshold,
			string strReason,
             * */
			out string strError)
		{
			strError = "";

			// Color color = Color.FromArgb(255,255,200);

			// string strNumber = "";

            OneHit hit= new OneHit(hit_param);
            /*
            hit.From = hit_param.From;
            hit.Key = hit_param.Key;
            hit.SearchStyle = hit_param.SearchStyle;
            hit.Weight = hit_param.Weight;
             * */

            ItemInfo info = null;

			string strPath = ResPath.GetReverseRecordPath(strFullPath);

			// ����pathѰ���Ѿ����ڵ�item
			ListViewItem item = (ListViewItem)m_tableItem[strPath];
			if (item == null)
			{
				item = new ListViewItem(strPath, 0);

                /*
				strNumber = Convert.ToString(nCurWeight);

				if (nCurWeight >= nThreshold)
				{
					strNumber = "*" + strNumber;
					item.BackColor = color;
				}

				item.SubItems.Add(strNumber);
				item.SubItems.Add(strReason);
                 * */

				this.listView_browse.Items.Add(item);
				m_tableItem[strPath] = item;

                info = new ItemInfo();
                item.Tag = info;
                info.Hits.Add(hit);
			}
			else 
			{
                /*
				// ���Ѿ����ڵ�weightֵ���ϱ�����ֵ
				if (nCurWeight != 0)
				{
					string strExistWeight = item.SubItems[1].Text;

					// ȥ�����ܴ��ڵ�����'*'�ַ�
					if (strExistWeight.Length > 0 && strExistWeight[0] == '*')
						strExistWeight = strExistWeight.Substring(1);

					int nOldValue = 0;
					try 
					{
						nOldValue = Convert.ToInt32(strExistWeight);
					}
					catch 
					{
					}


					int nValue = nOldValue + nCurWeight;

					strNumber = Convert.ToString(nValue);

					if (nValue >= nThreshold)
					{
						strNumber = "*" + strNumber;
						if (nOldValue < nThreshold)
							item.BackColor = color;
					}

					item.SubItems[1].Text = strNumber;
				}

				string strOldReason = item.SubItems[2].Text;

				if (strOldReason != "")
					item.SubItems[2].Text += ";";

				item.SubItems[2].Text += strReason;
                */


                info = (ItemInfo)item.Tag;
                Debug.Assert(info != null, "");

                info.Hits.Add(hit);
			}

            int nHitIndex = GetHitIndex(info.Hits, hit.From);

            Debug.Assert(nHitIndex >= 0, "");

            // ��þ���һ�����е�weightֵ
            // ����б��е����ֲ���nHitIndex��ô�������ȡ���һ����ֵ
            // parameters:
            //      strWeightList   ԭʼ��weight���Զ��壬��̬Ϊ"100,50,20"����"50"
            //      nHitIndex   ��ǰ���е���һ��Ϊ�ܹ����еĶ��ٴ�
            hit.Weight = GetWeight(strWeightList,
                nHitIndex);


			return 0;
		}

        // ����ض�from�£����һ�����е�index
        // return:
        //      -1  not found
        //      ����    found
        static int GetHitIndex(List<OneHit> hits,
            string strFrom)
        {
            int j = -1;
            for (int i = 0; i < hits.Count; i++)
            {
                OneHit hit = hits[i];
                if (hit.From == strFrom)
                    j++;
            }

            return j;
        }

        // ��þ���һ�����е�weightֵ
        // ����б��е����ֲ���nHitIndex��ô�������ȡ���һ����ֵ
        // parameters:
        //      strWeightList   ԭʼ��weight���Զ��壬��̬Ϊ"100,50,20"����"50"
        //      nHitIndex   ��ǰ���е���һ��Ϊ�ܹ����еĶ��ٴ�
        static int GetWeight(string strWeightList,
            int nHitIndex)
        {
            Debug.Assert(nHitIndex >= 0, "");

            string[] parts = strWeightList.Split(new char[] { ',' });
            Debug.Assert(parts.Length >= 1, "");

            string strWeight = "";

            if (parts.Length - 1 < nHitIndex)
                strWeight = parts[parts.Length - 1].Trim();
            else
                strWeight = parts[nHitIndex].Trim();

            try
            {
                return Convert.ToInt32(strWeight);
            }
            catch
            {
                return 0;
            }
        }

        /*
        // parameters:
        //      strReason   ��������ע��
		int FillList(
			string strFullPath,
            string strSearchStyle,
			int nCurWeight,
			int nThreshold,
			string strReason,
			out string strError)
		{
			strError = "";

			Color color = Color.FromArgb(255,255,200);

			string strNumber = "";

			string strPath = ResPath.GetReverseRecordPath(strFullPath);

			// ����pathѰ���Ѿ����ڵ�item
			ListViewItem item = (ListViewItem)m_tableItem[strPath];
			if (item == null)
			{
				item = new ListViewItem(strPath, 0);

				strNumber = Convert.ToString(nCurWeight);

				if (nCurWeight >= nThreshold)
				{
					strNumber = "*" + strNumber;
					item.BackColor = color;
				}

				item.SubItems.Add(strNumber);
				item.SubItems.Add(strReason);

				this.listView_browse.Items.Add(item);

				m_tableItem[strPath] = item;
			}
			else 
			{
				// ���Ѿ����ڵ�weightֵ���ϱ�����ֵ
				if (nCurWeight != 0)
				{
					string strExistWeight = item.SubItems[1].Text;

					// ȥ�����ܴ��ڵ�����'*'�ַ�
					if (strExistWeight.Length > 0 && strExistWeight[0] == '*')
						strExistWeight = strExistWeight.Substring(1);

					int nOldValue = 0;
					try 
					{
						nOldValue = Convert.ToInt32(strExistWeight);
					}
					catch 
					{
					}


					int nValue = nOldValue + nCurWeight;

					strNumber = Convert.ToString(nValue);

					if (nValue >= nThreshold)
					{
						strNumber = "*" + strNumber;
						if (nOldValue < nThreshold)
							item.BackColor = color;
					}

					item.SubItems[1].Text = strNumber;
				}

				string strOldReason = item.SubItems[2].Text;

				if (strOldReason != "")
					item.SubItems[2].Text += ";";

				item.SubItems[2].Text += strReason;

			}

			return 0;
		}
         * */

		private void button_stop_Click(object sender, System.EventArgs e)
		{
			if (this.SearchPanel != null)
				this.SearchPanel.DoStopClick();
		
		}

		// ���<project>����Ԫ��
		XmlNode GetProjectNode(string strProjectName,
			out string strError)
		{
			strError = "";

			if (this.domDupCfg == null)
			{
				strError = "���ȵ���GetDupCfgFile()��ȡ�����ļ�";
				return null;	
			}

			XmlNode node = this.domDupCfg.DocumentElement.SelectSingleNode("//project[@name='"+strProjectName+"']");
			if (node == null)
				strError = "���ط��� '" +strProjectName + "' ������";
			return node;
		}

		private void textBox_serverUrl_TextChanged(object sender, System.EventArgs e)
		{
			if (this.SearchPanel != null)
				this.SearchPanel.ServerUrl = this.textBox_serverUrl.Text;

		}

		private void listView_browse_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ListViewItem selection = this.listView_browse.GetItemAt(e.X, e.Y);

			if (selection != null)
			{
				string strText = "";
				int nRet = 	ListViewUtil.ColumnHitTest(this.listView_browse,
					e.X);
				if (nRet == 0)
					strText = selection.SubItems[0].Text;
				else if (nRet == 1 || nRet == 2)
					strText = selection.SubItems[0].Text + "\r\n------\r\n" + 
					selection.SubItems[2].Text.Replace(";",";\r\n");

				this.toolTip_searchComment.SetToolTip(this.listView_browse,
					strText);
			}
			else
				this.toolTip_searchComment.SetToolTip(this.listView_browse, null);

		}

		int GetBrowseColumns(out string strError)
		{
			strError = "";

			if (this.LoadBrowse == LoadBrowse.None)
				return 0;


			ArrayList aFullPath = new ArrayList();
			int i=0;
			for(i=0;i<this.listView_browse.Items.Count;i++)
			{
				string strFullPath = this.listView_browse.Items[i].Text;

				string strNumber = this.listView_browse.Items[i].SubItems[1].Text;

				if (strNumber.Length > 0 && strNumber[0] == '*')
				{
				}
				else 
				{
					if (this.LoadBrowse == LoadBrowse.Dup)
						continue;
				}

				aFullPath.Add(ResPath.GetRegularRecordPath(strFullPath));
			}

			string [] fullpaths = new string [aFullPath.Count];
			for(i=0;i<fullpaths.Length;i++)
			{
				fullpaths[i] = (string)aFullPath[i];
			}


			this.SearchPanel.BrowseRecord -= new BrowseRecordEventHandler(BrowseRecordColsCallBack);
			this.SearchPanel.BrowseRecord += new BrowseRecordEventHandler(BrowseRecordColsCallBack);


			try 
			{
				// ��ȡ�����¼
				// return:
				//		-1	error
				//		0	not found
				//		1	found
				int nRet = this.SearchPanel.GetBrowseRecord(fullpaths,
					false,
					"cols",
					out strError);
				if (nRet == -1)
					return -1;
			}
			finally 
			{
				this.SearchPanel.BrowseRecord -= new BrowseRecordEventHandler(BrowseRecordColsCallBack);
			}

			return 0;
		}


		void BrowseRecordColsCallBack(object sender, BrowseRecordEventArgs e)
		{
			ListViewUtil.EnsureColumns(this.listView_browse,
				3 + e.Cols.Length,
				200);

			ListViewItem item = (ListViewItem)this.m_tableItem[ResPath.GetReverseRecordPath(e.FullPath)];
			if (item == null)
			{
				e.Cancel = true;
				e.ErrorInfo = "·��Ϊ '" + e.FullPath + "' ��������listview�в�����...";
				return;
			}


			for(int j=0;j<e.Cols.Length;j++)
			{
				ListViewUtil.ChangeItemText(item,
					j+3,
					e.Cols[j]);
			}
		}

		private void listView_browse_DoubleClick(object sender, System.EventArgs e)
		{
			if (this.OpenDetail == null)
				return;

			string[] paths = BrowseList.GetSelectedRecordPaths(this.listView_browse, true);

			if (paths.Length == 0)
				return;
			/*
			string [] paths = new string [this.listView_browse.SelectedItems.Count];
			for(int i=0;i<this.listView_browse.SelectedItems.Count;i++)
			{
				string strPath = this.listView_browse.SelectedItems[i].Text;

				// paths[i] = this.textBox_serverUrl.Text + "?" + strPath;
				paths[i] = ResPath.GetRegularRecordPath(strPath);

			}
			*/

			OpenDetailEventArgs args = new OpenDetailEventArgs();
			args.Paths = paths;
			args.OpenNew = true;

			this.listView_browse.Enabled = false;
			this.OpenDetail(this, args);		
			this.listView_browse.Enabled = true;
		}

		private void button_findServerUrl_Click(object sender, System.EventArgs e)
		{
			OpenResDlg dlg = new OpenResDlg();

			dlg.Text = "��ѡ����������";
			dlg.EnabledIndices = new int[] { ResTree.RESTYPE_SERVER };
			dlg.ap = this.SearchPanel.ap;
			dlg.ApCfgTitle = "findServerUrl_openresdlg";
			dlg.MultiSelect = false;
			dlg.Path = this.textBox_serverUrl.Text;
			dlg.Initial( this.SearchPanel.Servers,
				this.SearchPanel.Channels);	
			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;

			textBox_serverUrl.Text = dlg.Path;		
		}

		private void button_findProjectName_Click(object sender, System.EventArgs e)
		{
            FindProjectName();
		}

        /// <summary>
        /// ��"��ò��ط�����"�Ի���,��ò��ط���������������URL
        /// </summary>
        /// <returns>DialogResult.OK�Ի�����OK��ť�ر�;DialogResult.Cancel�Ի�����Cancel��ť�ر�</returns>
        public DialogResult FindProjectName()
        {
			GetDupProjectNameDlg dlg = new GetDupProjectNameDlg();

            dlg.ServerUrl = this.ServerUrl;
            dlg.SearchPanel = this.SearchPanel;
			dlg.DomDupCfg = this.domDupCfg;
			dlg.ProjectName = this.textBox_projectName.Text;
			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
                return dlg.DialogResult;

            this.ServerUrl = dlg.ServerUrl;
			this.textBox_projectName.Text = dlg.ProjectName;
            return dlg.DialogResult;
        }


		// Implements the manual sorting of items by columns.
		class ListViewItemComparer : IComparer
		{
			public ListViewItemComparer()
			{
			}

			public int Compare(object x, object y)
			{
                /*
				string strNumber1 = ((ListViewItem)x).SubItems[1].Text;
				string strNumber2 = ((ListViewItem)y).SubItems[1].Text;
                 * */

                // 2009/6/30 changed
                // sorter�߱��󣬿���ListView.Items.Add()����ֻ��һ�е����У��ͻ�����������������ʱ����е�[1]�в����߱���
                string strNumber1 = ListViewUtil.GetItemText(((ListViewItem)x), 1);
                string strNumber2 = ListViewUtil.GetItemText(((ListViewItem)y), 1);


				// ����һ��
				if (strNumber1.Length > 0)
				{
					if (strNumber1[0] == '*')
					{
						strNumber1 = strNumber1.Remove(0, 1);
					}
				}

				if (strNumber2.Length > 0)
				{
					if (strNumber2[0] == '*')
					{
						strNumber2 = strNumber2.Remove(0, 1);
					}
				}

				int nNumber1 = 0;
				int nNumber2 = 0;

				try 
				{
					nNumber1 = Convert.ToInt32(strNumber1);
				}
				catch
				{
				}

				try 
				{
					nNumber2 = Convert.ToInt32(strNumber2);
				}
				catch
				{
				}

				return -1*(nNumber1 - nNumber2);
			}
		}

	}

    /// <summary>
    /// ���������Щ����Ҫװ�������Ϣ��
    /// </summary>
	public enum LoadBrowse
	{
        /// <summary>
        /// ȫ��
        /// </summary>
		All = 0,

        /// <summary>
        /// ������ֵ����
        /// </summary>
		Dup = 1,

        /// <summary>
        /// ȫ������
        /// </summary>
		None = 2,
	}

    // һ�����е���Ϣ
    class OneHit
    {
        public string Key = ""; // ������
        public string From = "";    // ����;��
        public int Weight = 0;  // ���еķ���ֵ
        public string SearchStyle = ""; // �������

        public OneHit()
        {
        }

        public OneHit(OneHit hit_param)
        {
            this.From = hit_param.From;
            this.Key = hit_param.Key;
            this.SearchStyle = hit_param.SearchStyle;
            this.Weight = hit_param.Weight;
        }
    }

    class ItemInfo
    {
        public List<OneHit> Hits = new List<OneHit>();
    }
}
