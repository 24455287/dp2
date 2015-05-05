using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Diagnostics;

using System.Runtime.Serialization;

using System.Runtime.Serialization.Formatters.Binary;


using DigitalPlatform.GUI;
using DigitalPlatform.IO;
using DigitalPlatform.Xml;
using DigitalPlatform.CommonControl;

namespace DigitalPlatform.Script
{
	/// <summary>
	/// Summary description for ProjectManageDlg.
	/// </summary>
	public class ProjectManageDlg : System.Windows.Forms.Form
	{
        // projects.xml�ļ�URL
        // "http://dp2003.com/dp2circulation/projects/projects.xml"
        // "http://dp2003.com/dp2batch/projects/projects.xml"
        public string ProjectsUrl = "";

        public string HostName = "";

        public event AutoCreateProjectXmlFileEventHandle CreateProjectXmlFile = null;

		public ScriptManager scriptManager = null;

		public ApplicationInfo	AppInfo = null;

        public string DataDir = ""; // ����Ŀ¼


		string strRecentPackageFilePath = "";

		private System.Windows.Forms.TreeView treeView1;
		private System.Windows.Forms.Button button_OK;
		private System.Windows.Forms.ImageList imageList_projectNodeType;
		private System.Windows.Forms.Button button_modify;
		private System.Windows.Forms.Button button_new;
		private System.Windows.Forms.Button button_delete;
		private System.Windows.Forms.Button button_down;
		private System.Windows.Forms.Button button_up;
		private System.Windows.Forms.Button button_import;
		private System.Windows.Forms.Button button_export;
        private Button button_updateProjects;
		private System.ComponentModel.IContainer components;

		public ProjectManageDlg()
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectManageDlg));
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.button_OK = new System.Windows.Forms.Button();
            this.imageList_projectNodeType = new System.Windows.Forms.ImageList(this.components);
            this.button_modify = new System.Windows.Forms.Button();
            this.button_new = new System.Windows.Forms.Button();
            this.button_delete = new System.Windows.Forms.Button();
            this.button_down = new System.Windows.Forms.Button();
            this.button_up = new System.Windows.Forms.Button();
            this.button_import = new System.Windows.Forms.Button();
            this.button_export = new System.Windows.Forms.Button();
            this.button_updateProjects = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.HideSelection = false;
            this.treeView1.Location = new System.Drawing.Point(9, 9);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(358, 268);
            this.treeView1.TabIndex = 0;
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            this.treeView1.DoubleClick += new System.EventHandler(this.treeView1_DoubleClick);
            this.treeView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.treeView1_MouseDown);
            this.treeView1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.treeView1_MouseUp);
            // 
            // button_OK
            // 
            this.button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button_OK.Location = new System.Drawing.Point(375, 287);
            this.button_OK.Name = "button_OK";
            this.button_OK.Size = new System.Drawing.Size(72, 23);
            this.button_OK.TabIndex = 9;
            this.button_OK.Text = "�ر�";
            this.button_OK.Click += new System.EventHandler(this.button_OK_Click);
            // 
            // imageList_projectNodeType
            // 
            this.imageList_projectNodeType.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList_projectNodeType.ImageStream")));
            this.imageList_projectNodeType.TransparentColor = System.Drawing.Color.Fuchsia;
            this.imageList_projectNodeType.Images.SetKeyName(0, "");
            this.imageList_projectNodeType.Images.SetKeyName(1, "");
            // 
            // button_modify
            // 
            this.button_modify.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_modify.Location = new System.Drawing.Point(372, 12);
            this.button_modify.Name = "button_modify";
            this.button_modify.Size = new System.Drawing.Size(75, 21);
            this.button_modify.TabIndex = 1;
            this.button_modify.Text = "�޸�(&M)";
            this.button_modify.Click += new System.EventHandler(this.button_modify_Click);
            // 
            // button_new
            // 
            this.button_new.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_new.Location = new System.Drawing.Point(372, 38);
            this.button_new.Name = "button_new";
            this.button_new.Size = new System.Drawing.Size(75, 22);
            this.button_new.TabIndex = 2;
            this.button_new.Text = "����(&N)";
            this.button_new.Click += new System.EventHandler(this.button_newProject_Click);
            // 
            // button_delete
            // 
            this.button_delete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_delete.Location = new System.Drawing.Point(372, 149);
            this.button_delete.Name = "button_delete";
            this.button_delete.Size = new System.Drawing.Size(75, 22);
            this.button_delete.TabIndex = 5;
            this.button_delete.Text = "ɾ��(&E)";
            this.button_delete.Click += new System.EventHandler(this.button_delete_Click);
            // 
            // button_down
            // 
            this.button_down.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_down.Location = new System.Drawing.Point(372, 109);
            this.button_down.Name = "button_down";
            this.button_down.Size = new System.Drawing.Size(75, 22);
            this.button_down.TabIndex = 4;
            this.button_down.Text = "����(&D)";
            this.button_down.Click += new System.EventHandler(this.button_down_Click);
            // 
            // button_up
            // 
            this.button_up.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_up.Location = new System.Drawing.Point(372, 82);
            this.button_up.Name = "button_up";
            this.button_up.Size = new System.Drawing.Size(75, 22);
            this.button_up.TabIndex = 3;
            this.button_up.Text = "����(&U)";
            this.button_up.Click += new System.EventHandler(this.button_up_Click);
            // 
            // button_import
            // 
            this.button_import.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_import.Location = new System.Drawing.Point(9, 287);
            this.button_import.Name = "button_import";
            this.button_import.Size = new System.Drawing.Size(132, 23);
            this.button_import.TabIndex = 6;
            this.button_import.Text = "����[��ǰĿ¼](&I)...";
            this.button_import.Click += new System.EventHandler(this.button_import_Click);
            // 
            // button_export
            // 
            this.button_export.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_export.Location = new System.Drawing.Point(148, 287);
            this.button_export.Name = "button_export";
            this.button_export.Size = new System.Drawing.Size(99, 23);
            this.button_export.TabIndex = 7;
            this.button_export.Text = "����(&E)...";
            this.button_export.Click += new System.EventHandler(this.button_export_Click);
            // 
            // button_updateProjects
            // 
            this.button_updateProjects.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_updateProjects.Location = new System.Drawing.Point(253, 287);
            this.button_updateProjects.Name = "button_updateProjects";
            this.button_updateProjects.Size = new System.Drawing.Size(99, 23);
            this.button_updateProjects.TabIndex = 8;
            this.button_updateProjects.Text = "������(&U)";
            this.button_updateProjects.UseVisualStyleBackColor = true;
            this.button_updateProjects.Visible = false;
            this.button_updateProjects.Click += new System.EventHandler(this.button_updateProjects_Click);
            // 
            // ProjectManageDlg
            // 
            this.AcceptButton = this.button_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(456, 318);
            this.Controls.Add(this.button_updateProjects);
            this.Controls.Add(this.button_export);
            this.Controls.Add(this.button_import);
            this.Controls.Add(this.button_down);
            this.Controls.Add(this.button_up);
            this.Controls.Add(this.button_delete);
            this.Controls.Add(this.button_new);
            this.Controls.Add(this.button_modify);
            this.Controls.Add(this.button_OK);
            this.Controls.Add(this.treeView1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ProjectManageDlg";
            this.ShowInTaskbar = false;
            this.Text = "��������";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.ProjectManageDlg_Closing);
            this.Closed += new System.EventHandler(this.ProjectManageDlg_Closed);
            this.Load += new System.EventHandler(this.ProjectManageDlg_Load);
            this.ResumeLayout(false);

		}
		#endregion

		private void ProjectManageDlg_Load(object sender, System.EventArgs e)
		{
            Debug.Assert(string.IsNullOrEmpty(this.HostName) == false, "");

			if (AppInfo != null) 
			{
				AppInfo.LoadFormStates(this,
					"projectman");
			}
			/*
			if (applicationInfo != null) 
			{

				this.Width = applicationInfo.GetInt(
					"projectman", "width", 640);
				this.Height = applicationInfo.GetInt(
					"projectman", "height", 500);

				this.Location = new Point(
					applicationInfo.GetInt("projectman", "x", 0),
					applicationInfo.GetInt("projectman", "y", 0));

				this.WindowState = (FormWindowState)Enum.Parse(typeof(FormWindowState), applicationInfo.GetString(
					"projectman", "window_state", "Normal"));
			}
			*/


			treeView1.ImageList = imageList_projectNodeType;
			treeView1.PathSeparator = "/";

			if (scriptManager != null)
			{
				bool bDone = false;
			REDO:
				try 
				{
					scriptManager.FillTree(this.treeView1);
				}
				catch(System.IO.FileNotFoundException ex) 
				{
					/*
					MessageBox.Show("װ��" + scriptManager.CfgFilePath + "�ļ�ʧ�ܣ�ԭ��:"
						+ ex.Message);
					*/
					// MessageBox.Show(ex.Message);
					//return;
					if (bDone == false) 
					{
						MessageBox.Show(this, "�Զ��������ļ� " + scriptManager.CfgFilePath);

                        // �����¼�
                        if (this.CreateProjectXmlFile != null)
                        {
                            AutoCreateProjectXmlFileEventArgs e1 = new AutoCreateProjectXmlFileEventArgs();
                            e1.Filename = scriptManager.CfgFilePath;
                            this.CreateProjectXmlFile(this, e1);
                        }

						ScriptManager.CreateDefaultProjectsXmlFile(scriptManager.CfgFilePath,
							"clientcfgs");
						bDone = true;
						goto REDO;
					}
					else 
					{
						MessageBox.Show(this, ex.Message);
						return;
					}
				}
				catch(System.Xml.XmlException ex)
				{
					MessageBox.Show("װ��" + scriptManager.CfgFilePath + "�ļ�ʧ�ܣ�ԭ��:"
						+ ex.Message);
					return;
				}
			}
			treeView1_AfterSelect(null,null);

			TreeViewUtil.SelectTreeNode(treeView1, 
				AppInfo.GetString(
				"projectman",
				"lastUsedProject",
				""),
                '/');

		}

		private void ProjectManageDlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			/*
			if (scriptManager.Changed == true)
			{
				DialogResult msgResult = MessageBox.Show(this,
					"�Ƿ������ǰ�������޸Ķ������˳�?",
					"script",
					MessageBoxButtons.OKCancel,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button2);
				if (msgResult == DialogResult.Cancel) 
				{
					e.Cancel = true;
					return;
				}
			}
			*/



		}

		private void ProjectManageDlg_Closed(object sender, System.EventArgs e)
		{

			if (AppInfo != null) 
			{
				AppInfo.SetString(
					"projectman",
					"lastUsedProject",
					TreeViewUtil.GetSelectedTreeNodePath(treeView1, '/')); // 2007/8/2 changed
			}

			if (AppInfo != null) 
			{
				AppInfo.SaveFormStates(this,
					"projectman");
			}

			/*
			if (applicationInfo != null) 
			{
				applicationInfo.SetString(
					"projectman", "window_state", 
					Enum.GetName(typeof(FormWindowState), this.WindowState));
			}

			if (applicationInfo != null) 
			{
				WindowState = FormWindowState.Normal;	// �Ƿ������ش���?
				applicationInfo.SetInt(
					"projectman", "width", this.Width);
				applicationInfo.SetInt(
					"projectman", "height", this.Height);

				applicationInfo.SetInt("projectman", "x", this.Location.X);
				applicationInfo.SetInt("projectman", "y", this.Location.Y);
			}
			*/


		}

		// �޸ķ���
		private void button_modify_Click(object sender, System.EventArgs e)
		{
			int nRet;
			// ��ǰ��ѡ���node
			if (treeView1.SelectedNode == null) 
			{
				MessageBox.Show("��δѡ�񷽰�����Ŀ¼");
				return;
			}

			TreeNode node = treeView1.SelectedNode;
			if (node.ImageIndex == 0) 
			{
				// �޸�Ŀ¼��
				DirNameDlg namedlg = new DirNameDlg();
                GuiUtil.AutoSetDefaultFont(namedlg);

				namedlg.textBox_dirName.Text = node.Text;
				namedlg.StartPosition = FormStartPosition.CenterScreen;
				namedlg.ShowDialog(this);

				if (namedlg.DialogResult == DialogResult.OK) 
				{
					// return:
					//	0	not found
					//	1	found and changed
					nRet = scriptManager.RenameDir(node.FullPath,
						namedlg.textBox_dirName.Text);
					if (nRet == 1) 
					{
						node.Text = namedlg.textBox_dirName.Text;	// �����Ӿ�
						scriptManager.Save();
					}
				}

				return ;
			}

			string strProjectNamePath = node.FullPath;

			string strLocate = "";

			// ��÷�������
			// strProjectNamePath	������������·��
			// return:
			//		-1	error
			//		0	not found project
			//		1	found
			nRet = scriptManager.GetProjectData(
				strProjectNamePath,
				out strLocate);
			if (nRet != 1) 
			{
				MessageBox.Show("���� "+ strProjectNamePath + " ��ScriptManager��û���ҵ�");
				return ;
			}


			ScriptDlg dlg = new ScriptDlg();
            GuiUtil.AutoSetDefaultFont(dlg);

            dlg.HostName = this.HostName;
			dlg.scriptManager = scriptManager;
			dlg.Initial(strProjectNamePath,
				strLocate);

			dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.ShowDialog(this);


			if (dlg.DialogResult == DialogResult.OK) 
			{
				if (dlg.ResultProjectNamePath != strProjectNamePath) 
				{
					/*
					// �޸���ʾ��Project����
					string strPath;
					string strName;
					ScriptManager.SplitProjectPathName(dlg.ResultProjectNamePath,
						out strPath,
						out strName);

					string strError;

					nRet = scriptManager.ChangeProjectData(strProjectNamePath,
						strName,
						null,
						out strError);
					if (nRet == -1) 
					{
						MessageBox.Show(this, strError);
					}
					else 
					{
						// ������ʾ?
					}
					*/
					// XML DOM�Ѿ���ScriptDlg���޸ģ�����ֻ�Ƕ�����ʾ
					string strPath;
					string strName;
					ScriptManager.SplitProjectPathName(dlg.ResultProjectNamePath,
						out strPath,
						out strName);

					node.Text = strName;

				}

				scriptManager.Save();
			}

		
		}

		// �ڶ������ҵ�һ�����ظ�������
		string GetTempProjectName(TreeView treeView,
			TreeNode parent,
			string strPrefix,
			ref int nPrefixNumber)
		{
			TreeNodeCollection nodes = null;

			if (parent != null) 
				nodes = parent.Nodes;
			else
				nodes = treeView.Nodes;

			string strName = strPrefix;

			for(;;nPrefixNumber ++) 
			{
				if (nPrefixNumber == -1)
					strName = strPrefix;
				else
					strName = strPrefix + " " + Convert.ToString(nPrefixNumber);

				bool bFound = false;
				for(int i=0;i<nodes.Count; i++) 
				{
					string strText = nodes[i].Text;

					if (String.Compare(strText, strName, true) == 0) 
					{
						bFound = true;
						break;
					}
				}

				if (bFound == false)
					break;
			}

			return strName;

		}

		// �ڶ������ҵ�һ�����ظ�������
		string GetTempDirName(TreeView treeView,
			TreeNode parent,
			string strPrefix,
			ref int nPrefixNumber)
		{
			TreeNodeCollection nodes = null;

			if (parent != null) 
				nodes = parent.Nodes;
			else
				nodes = treeView.Nodes;

			string strName = strPrefix;

			for(;;nPrefixNumber ++) 
			{
				strName = strPrefix + " " +Convert.ToString(nPrefixNumber);

				bool bFound = false;
				for(int i=0;i<nodes.Count; i++) 
				{
					string strText = nodes[i].Text;

					if (String.Compare(strText, strName, true) == 0) 
					{
						bFound = true;
						break;
					}
				}

				if (bFound == false)
					break;
			}

			return strName;

		}

		// �·���
		private void button_newProject_Click(object sender, System.EventArgs e)
		{
			// ��ǰ����·��
			string strProjectPath = "";
			string strTempName = "new project 1";

			TreeNode parent = null;

			int nPrefixNumber = -1;	// 1

			// ��ǰ��ѡ���node
			if (treeView1.SelectedNode == null) 
			{
				strProjectPath = "";
			}
			else 
			{
				// �����ǰѡ�����dir���ͽڵ㣬�������´�����project
				// ���򣬾���ͬһ��������project
				if (treeView1.SelectedNode.ImageIndex == 0)
					parent = treeView1.SelectedNode;
				else
					parent = treeView1.SelectedNode.Parent;

				strProjectPath = parent != null ? parent.FullPath : "";

				strTempName = GetTempProjectName(treeView1,
					parent,
					"new project",
					ref nPrefixNumber);

				scriptManager.Save();

			}

			/*
			StreamReader sr = new StreamReader(strTempName, true);
			string strCode =sr.ReadToEnd();
			sr.Close();
			*/

			string strNewLocate = scriptManager.NewProjectLocate(
				"new project",
				ref nPrefixNumber);

			ScriptDlg dlg = new ScriptDlg();
            GuiUtil.AutoSetDefaultFont(dlg);

            dlg.HostName = this.HostName;
            dlg.scriptManager = scriptManager;
			dlg.New(strProjectPath,
				strTempName,
				strNewLocate);

			dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;

			// ʵ�ʲ���project����
			XmlNode projNode = scriptManager.NewProjectNode(
				dlg.ResultProjectNamePath,
				dlg.ResultLocate,
				false);	// false��ʾ����Ҫ����Ŀ¼��ȱʡ�ļ�

			// ������ʾ?
			scriptManager.FillOneLevel(treeView1, 
				parent, 
				projNode.ParentNode);
			TreeViewUtil.SelectTreeNode(treeView1, 
				scriptManager.GetNodePathName(projNode),
                '/');

			/*
			if (parent != null) 
			{
				parent.Expand();
			}
			*/

			scriptManager.Save();
		}

		// ��Ŀ¼
		private void button_newDir_Click(object sender, System.EventArgs e)
		{
			// ��ǰ����·��
			string strDirPath = "";

			TreeNode parent = null;

			int nPrefixNumber = 1;

			// ��ǰ��ѡ���node
			if (treeView1.SelectedNode == null) 
			{
				strDirPath = "";
			}
			else 
			{
				// project�²��ܴ����ӽڵ㣬���ǿ��Դ����ֵ�?

				if (treeView1.SelectedNode.ImageIndex == 0)
					parent = treeView1.SelectedNode;
				else
					parent = treeView1.SelectedNode.Parent;

				strDirPath = parent != null ? parent.FullPath : "";
			}

			string strTempName = GetTempDirName(treeView1,
				parent,
				"new dir",
				ref nPrefixNumber);

			DirNameDlg namedlg = new DirNameDlg();
            GuiUtil.AutoSetDefaultFont(namedlg);

			namedlg.textBox_dirName.Text = strTempName;
			namedlg.StartPosition = FormStartPosition.CenterScreen;
			namedlg.ShowDialog(this);

			if (namedlg.DialogResult == DialogResult.OK) 
			{
				string strDirNamePath = (strDirPath!="" ? strDirPath + "/" : "")
					+ namedlg.textBox_dirName.Text;

				XmlNode dirNode = scriptManager.NewDirNode(strDirNamePath);

				if (dirNode != null) 
				{
					scriptManager.Save();

					// ������ʾ?
					scriptManager.FillOneLevel(treeView1, 
						parent, 
						dirNode.ParentNode);

					TreeViewUtil.SelectTreeNode(treeView1, 
						scriptManager.GetNodePathName(dirNode),
                        '/');

					/*
					if (parent != null) 
						parent.Expand();
						*/

				}
			}

		}

		// ɾ������
		private void button_delete_Click(object sender, System.EventArgs e)
		{
			string strError;

			// ��ǰ��ѡ���node
			if (treeView1.SelectedNode == null) 
			{
				MessageBox.Show("��δѡ�񷽰���Ŀ¼");
				return ;
			}

			TreeNode parent = treeView1.SelectedNode.Parent;

			int nRet;
			XmlNode parentXmlNode = null;
			DialogResult msgResult;

			TreeNode node = treeView1.SelectedNode;
			if (node.ImageIndex == 0) 
			{
				node.ExpandAll();
				msgResult = MessageBox.Show(this,
					"ȷʵҪɾ��Ŀ¼ " + node.FullPath + "���¼�������ȫ��Ŀ¼�ͷ���ô?",
					"script",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button2);
				if (msgResult == DialogResult.No)
					return;

				// return:
				//	-1	error
				//	0	not found
				//	1	found and changed
				nRet = scriptManager.DeleteDir(node.FullPath,
					out parentXmlNode,
					out strError);
				if (nRet == -1) 
				{
					MessageBox.Show(strError);
					// return ;
				}

				if (nRet == 1) 
				{
					if (parentXmlNode != null)
					{
						// ������ʾ?
						scriptManager.FillOneLevel(treeView1, 
							parent, 
							parentXmlNode);
					}
					scriptManager.Save();
				}
				return ;
			}


			string strProjectNamePath = node.FullPath;

			msgResult = MessageBox.Show(this,
				"ȷʵҪɾ������ '" + node.FullPath + "' ?",
				"script",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button2);
			if (msgResult == DialogResult.No)
				return;

			// ׼��ɾ����selectionͣ���Ľڵ�·��
			TreeNode nodeNear = null;
			if (node.PrevNode != null)
				nodeNear = node.PrevNode;
			else if (node.NextNode != null)
				nodeNear = node.NextNode;
			else 
				nodeNear = parent;
			string strPath = "";
			if (nodeNear != null)
				strPath = nodeNear.FullPath;

			// ɾ��һ������
			// return:
			// -1	error
			//	0	not found
			//	1	found and deleted
			//	2	canceld	���projectû�б�ɾ��
			nRet = scriptManager.DeleteProject(
				strProjectNamePath,
				true,
				out parentXmlNode,
				out strError);
			if (nRet == -1)
				goto ERROR1;

			if (nRet == 0) 
			{
				strError = "���� "+ strProjectNamePath + " ��ScriptManager��û���ҵ�";
				goto CANCEL1;
			}

			if (nRet == 2)
			{
				strError = "���� "+ strProjectNamePath + " ����ɾ��";
				goto CANCEL1;
			}

			if (parentXmlNode != null)
			{
				// ������ʾ?
				scriptManager.FillOneLevel(treeView1, 
					parent, 
					parentXmlNode);

				TreeViewUtil.SelectTreeNode(treeView1, 
					strPath,
                    '/');

			}


			scriptManager.Save();
			return;
			CANCEL1:
				if (strError != "")
					MessageBox.Show(strError);
			return;

			ERROR1:
				MessageBox.Show(strError);
			return;
		}


		private void treeView1_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			// ��ǰ��ѡ���node
			if (treeView1.SelectedNode == null) 
			{
				button_modify.Enabled = false;
				button_new.Enabled = true;
				button_delete.Enabled = false;
				button_up.Enabled = false;
				button_down.Enabled = false;
				button_export.Enabled = false;
				return ;
			}
			

			if (treeView1.SelectedNode.ImageIndex == 0) // Ŀ¼
			{
				button_modify.Enabled = true;
				button_new.Enabled = true;
				button_delete.Enabled = true;

				if (treeView1.SelectedNode.PrevNode == null)
					button_up.Enabled = false;
				else
					button_up.Enabled = true;
				if (treeView1.SelectedNode.NextNode == null)
					button_down.Enabled = false;
				else
					button_down.Enabled = true;

				button_export.Enabled = false;
				return;
			}

			if (treeView1.SelectedNode.ImageIndex == 1) // project
			{
				button_modify.Enabled = true;
				button_new.Enabled = true;
				button_delete.Enabled = true;
				if (treeView1.SelectedNode.PrevNode == null)
					button_up.Enabled = false;
				else
					button_up.Enabled = true;
				if (treeView1.SelectedNode.NextNode == null)
					button_down.Enabled = false;
				else
					button_down.Enabled = true;

				button_export.Enabled = true;
			}
		}

		private void button_OK_Click(object sender, System.EventArgs e)
		{

			this.Close();
			this.DialogResult = DialogResult.OK;
		}

		private void button_Cancel_Click(object sender, System.EventArgs e)
		{

			this.Close();
			this.DialogResult = DialogResult.Cancel;
		}



		private void treeView1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if(e.Button != MouseButtons.Right)
				return;

			ContextMenu contextMenu = new ContextMenu();
			MenuItem menuItem = null;

			TreeNode node = treeView1.SelectedNode;

			//
			menuItem = new MenuItem("�޸�(&M)");
			menuItem.Click += new System.EventHandler(this.button_modify_Click);
			if (node == null) 
			{
				menuItem.Enabled = false;
			}
			contextMenu.MenuItems.Add(menuItem);

			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			string strText;

		{
			TreeNode parent = null;

			if (treeView1.SelectedNode != null) 
			{
				if (treeView1.SelectedNode.ImageIndex == 0)
					parent = treeView1.SelectedNode;
				else
					parent = treeView1.SelectedNode.Parent;
			}

			if (parent == null)
				strText = "�ڸ���";
			else
				strText = "��Ŀ¼ " + parent.Text + "��";
		}

			//
			menuItem = new MenuItem("��������(" + strText + ") (&N)");
			menuItem.Click += new System.EventHandler(this.button_newProject_Click);
			contextMenu.MenuItems.Add(menuItem);


		{
			TreeNode parent = null;

			if (treeView1.SelectedNode != null) 
			{
				if (treeView1.SelectedNode.ImageIndex == 0)
					parent = treeView1.SelectedNode;
				else
					parent = treeView1.SelectedNode.Parent;
			}

			if (parent == null)
				strText = "�ڸ���";
			else
				strText = "��Ŀ¼ " + parent.Text + "��";
		}


			//
			menuItem = new MenuItem("����Ŀ¼(" + strText + ") (&A)");
			menuItem.Click += new System.EventHandler(this.button_newDir_Click);
			contextMenu.MenuItems.Add(menuItem);

			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);


			// 
			menuItem = new MenuItem("����(&U)");
			menuItem.Click += new System.EventHandler(this.button_up_Click);
			if (treeView1.SelectedNode == null
				|| treeView1.SelectedNode.PrevNode == null)
				menuItem.Enabled = false;
			else
				menuItem.Enabled = true;
			contextMenu.MenuItems.Add(menuItem);



			// 
			menuItem = new MenuItem("����(&D)");
			menuItem.Click += new System.EventHandler(this.button_down_Click);
			if (treeView1.SelectedNode == null
				|| treeView1.SelectedNode.NextNode == null)
				menuItem.Enabled = false;
			else
				menuItem.Enabled = true;
			contextMenu.MenuItems.Add(menuItem);

			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			//
			menuItem = new MenuItem("ɾ��(&E)");
			menuItem.Click += new System.EventHandler(this.button_delete_Click);
			if (node == null) 
			{
				menuItem.Enabled = false;
			}
			contextMenu.MenuItems.Add(menuItem);


			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("����(&C)");
			menuItem.Click += new System.EventHandler(this.button_CopyToClipboard_Click);
			if (node == null || node.ImageIndex == 0) 
			{
				menuItem.Enabled = false;
			}
			contextMenu.MenuItems.Add(menuItem);

			bool bHasClipboardObject = false;
			IDataObject iData = Clipboard.GetDataObject();
			if (iData == null
				|| iData.GetDataPresent(typeof(Project)) == false)
				bHasClipboardObject = false;
			else
				bHasClipboardObject = true;



			menuItem = new MenuItem("ճ������ǰĿ¼ '" + GetCurTreeDir() + "' (&P)");
			menuItem.Click += new System.EventHandler(this.button_PasteFromClipboard_Click);
			if (bHasClipboardObject== false)
			{
				menuItem.Enabled = false;
			}
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("ճ����ԭĿ¼ '" + GetClipboardProjectDir() + "' (&O)");
			menuItem.Click += new System.EventHandler(this.button_PasteFromClipboardToOriginDir_Click);

			if (bHasClipboardObject== false)
			{
				menuItem.Enabled = false;
			}
			contextMenu.MenuItems.Add(menuItem);


			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("����(&E)");
			menuItem.Click += new System.EventHandler(this.button_CopyToFile_Click);
			if (node == null || node.ImageIndex == 0) 
			{
				menuItem.Enabled = false;
			}
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("����(&I)");
			menuItem.Click += new System.EventHandler(this.button_PasteFromFile_Click);
			/*
			if (node == null || node.ImageIndex == 0) 
			{
				menuItem.Enabled = false;
			}
			*/
			contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�� dp2003.com ��װ����(&I)");
            menuItem.Click += new System.EventHandler(this.menu_installProjects_Click);
            if (string.IsNullOrEmpty(this.ProjectsUrl) == true)
            {
                menuItem.Enabled = false;
            }
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�� dp2003.com ������(&U)");
            menuItem.Click += new System.EventHandler(this.button_updateProjects_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�Ӵ���Ŀ¼��װ����(&D)");
            menuItem.Click += new System.EventHandler(this.menu_installProjectsFromDisk_Click);
            if (string.IsNullOrEmpty(this.ProjectsUrl) == true)
            {
                menuItem.Enabled = false;
            }
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�Ӵ���Ŀ¼������(&P)");
            menuItem.Click += new System.EventHandler(this.button_updateProjectsFromDisk_Click);
            contextMenu.MenuItems.Add(menuItem);

			contextMenu.Show(treeView1, new Point(e.X, e.Y) );		
		}

        // �Ӵ���Ŀ¼��װ����
        void menu_installProjectsFromDisk_Click(object sender, System.EventArgs e)
        {
            string strError = "";
            int nRet = 0;
            int nInstallCount = 0;

            bool bDebugger = false;
            if (Control.ModifierKeys == Keys.Control)
                bDebugger = true;

            FolderBrowserDialog dir_dlg = new FolderBrowserDialog();

            dir_dlg.Description = "��ָ����������Ŀ¼:";
            dir_dlg.RootFolder = Environment.SpecialFolder.MyComputer;
            dir_dlg.ShowNewFolderButton = false;

            if (dir_dlg.ShowDialog() != DialogResult.OK)
                return;

            // Ѱ�� projects.xml �ļ�
            string strProjectsFileName = PathUtil.MergePath(dir_dlg.SelectedPath, "projects.xml");
            if (File.Exists(strProjectsFileName) == false)
            {
                // strError = "����ָ����Ŀ¼ '" + dir_dlg.SelectedPath + "' �в�û�а��� projects.xml �ļ����޷����а�װ";
                // goto ERROR1;

                // ���û�� projects.xml �ļ���������ȫ�� *.projpack �ļ�����������һ����ʱ�� ~projects.xml�ļ�
                Debug.Assert(string.IsNullOrEmpty(this.DataDir) == false, "");
                strProjectsFileName = PathUtil.MergePath(this.DataDir, "~projects.xml");
                nRet = ScriptManager.BuildProjectsFile(dir_dlg.SelectedPath,
                    strProjectsFileName,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }


            this.EnableControls(false);
            try
            {
                // �г��Ѿ���װ�ķ�����URL
                List<string> installed_urls = new List<string>();

                nRet = this.scriptManager.GetInstalledUrls(out installed_urls,
                out strError);
                if (nRet == -1)
                    goto ERROR1;

                SelectInstallProjectsDialog dlg = new SelectInstallProjectsDialog();
                GuiUtil.SetControlFont(dlg, this.Font);
                dlg.FilterHosts.Clear();
                Debug.Assert(string.IsNullOrEmpty(this.HostName) == false, "");
                dlg.FilterHosts.Add(this.HostName);
                dlg.XmlFilename = strProjectsFileName;
                dlg.InstalledUrls = installed_urls;
                if (bDebugger == true)
                    dlg.Category = "debugger";
                dlg.StartPosition = FormStartPosition.CenterScreen;

                if (this.AppInfo != null)
                    this.AppInfo.LinkFormState(dlg,
                        "SelectInstallProjectsDialog_state");
                dlg.ShowDialog(this);
                if (this.AppInfo != null)
                    this.AppInfo.UnlinkFormState(dlg);
                if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return;

                foreach (ProjectItem item in dlg.SelectedProjects)
                {
                    string strLastModified = "";
                    string strLocalFileName1 = "";

                    if (string.IsNullOrEmpty(item.FilePath) == false)
                    {
                        strLocalFileName1 = item.FilePath;
                    }
                    else
                    {
                        string strPureFileName = ScriptManager.GetFileNameFromUrl(item.Url);

                        strLocalFileName1 = PathUtil.MergePath(dir_dlg.SelectedPath, strPureFileName);
                    }

                    FileInfo fi = new FileInfo(strLocalFileName1);
                    if (fi.Exists == false)
                    {
                        strError = "û���ҵ��ļ� '" + strLocalFileName1 + "'";
                        //    strError = "Ŀ¼ '" + dir_dlg.SelectedPath + "' ��û���ҵ��ļ� '" + strPureFileName + "'";
                        goto ERROR1;
                    }


                    strLastModified = DateTimeUtil.Rfc1123DateTimeString(fi.LastWriteTimeUtc);

                    // ��װProject
                    // return:
                    //      -1  ����
                    //      0   û�а�װ����
                    //      >0  ��װ�ķ�����
                    nRet = this.scriptManager.InstallProject(
                        this,
                        "��ǰͳ�ƴ�",
                        strLocalFileName1,
                        strLastModified,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    nInstallCount += nRet;
                }

                // ˢ��������ʾ
                if (nInstallCount > 0)
                {
                    this.treeView1.Nodes.Clear();
                    scriptManager.FillTree(this.treeView1);
                    treeView1_AfterSelect(null, null);
                }
            }
            finally
            {
                this.EnableControls(true);
            }

            MessageBox.Show(this, "����װ���� " + nInstallCount.ToString() + " ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �� dp2003.com ��װ����
        void menu_installProjects_Click(object sender, System.EventArgs e)
        {
            string strError = "";
            int nRet = 0;
            int nInstallCount = 0;

            bool bDebugger = false;
            if (Control.ModifierKeys == Keys.Control)
                bDebugger = true;


            this.EnableControls(false);
            try
            {
                Debug.Assert(string.IsNullOrEmpty(this.DataDir) == false, "");

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
                    this.ProjectsUrl,   // "http://dp2003.com/dp2batch/projects/projects.xml"
                    strLocalFileName,
                    strTempFileName,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // �г��Ѿ���װ�ķ�����URL
                List<string> installed_urls = new List<string>();

                nRet = this.scriptManager.GetInstalledUrls(out installed_urls,
                out strError);
                if (nRet == -1)
                    goto ERROR1;

                SelectInstallProjectsDialog dlg = new SelectInstallProjectsDialog();
                GuiUtil.SetControlFont(dlg, this.Font);
                dlg.FilterHosts.Clear();
                Debug.Assert(string.IsNullOrEmpty(this.HostName) == false, "");
                dlg.FilterHosts.Add(this.HostName);
                dlg.XmlFilename = strLocalFileName;
                dlg.InstalledUrls = installed_urls;
                if (bDebugger == true)
                    dlg.Category = "debugger";
                dlg.StartPosition = FormStartPosition.CenterScreen;

                if (this.AppInfo != null)
                    this.AppInfo.LinkFormState(dlg,
                        "SelectInstallProjectsDialog_state");
                dlg.ShowDialog(this);
                if (this.AppInfo != null)
                    this.AppInfo.UnlinkFormState(dlg);
                if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return;

                foreach (ProjectItem item in dlg.SelectedProjects)
                {
                    string strLocalFileName1 = this.DataDir + "\\~install_project.projpack";
                    string strTempFileName1 = this.DataDir + "\\~temp_download_webfile";
                    string strLastModified = "";

                    nRet = WebFileDownloadDialog.DownloadWebFile(
                        this,
                        item.Url,
                        strLocalFileName1,
                        strTempFileName1,
                        "",
                        out strLastModified,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // ��װProject
                    // return:
                    //      -1  ����
                    //      0   û�а�װ����
                    //      >0  ��װ�ķ�����
                    nRet = this.scriptManager.InstallProject(
                        this,
                        "��ǰͳ�ƴ�",
                        strLocalFileName1,
                        strLastModified,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    nInstallCount += nRet;
                }

                // ˢ��������ʾ
                if (nInstallCount > 0)
                {
                    this.treeView1.Nodes.Clear();
                    scriptManager.FillTree(this.treeView1);
                    treeView1_AfterSelect(null, null);
                }
            }
            finally
            {
                this.EnableControls(true);
            }


            MessageBox.Show(this, "����װ���� " + nInstallCount.ToString() + " ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

		private void treeView1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			TreeNode curSelectedNode = treeView1.GetNodeAt(e.X, e.Y);

			if (treeView1.SelectedNode != curSelectedNode) 
			{
				treeView1.SelectedNode = curSelectedNode;

				if (treeView1.SelectedNode == null)
					treeView1_AfterSelect(null, null);	// ����
			}

		}

		void MoveUpDown(bool bUp)
		{
			// ��ǰ��ѡ���node
			if (treeView1.SelectedNode == null) 
			{
				MessageBox.Show("��δѡ�񷽰���Ŀ¼");
				return ;
			}

			TreeNode parent = treeView1.SelectedNode.Parent;

			string strPath = treeView1.SelectedNode.FullPath;

			int nRet;
			XmlNode parentXmlNode = null;

			TreeNode node = treeView1.SelectedNode;

			// �����ƶ��ڵ�
			// return:
			//	0	not found
			//	1	found and moved
			//	2	cant move
			nRet =  scriptManager.MoveNode(node.FullPath,
				bUp,
				out parentXmlNode);

			if (nRet == 1) 
			{
				if (parentXmlNode != null)
				{
					// ������ʾ?
					scriptManager.FillOneLevel(treeView1, 
						parent, 
						parentXmlNode);

					TreeViewUtil.SelectTreeNode(treeView1, 
						strPath,
                        '/');
				}
				scriptManager.Save();
			}

			if (nRet == 2) 
			{
				MessageBox.Show("�Ѿ���ͷ�ˣ������ƶ���...");
			}
			return ;
		}

		private void button_up_Click(object sender, System.EventArgs e)
		{
			MoveUpDown(true);
		}

		private void button_down_Click(object sender, System.EventArgs e)
		{
			MoveUpDown(false);
		}

		/*
		static void SelectTreeNode(TreeView treeView, 
			string strPath)
		{
			string[] aName = strPath.Split(new Char [] {'/'});

			TreeNode node = null;
			TreeNode nodeThis = null;
			for(int i=0;i<aName.Length;i++)
			{
				TreeNodeCollection nodes = null;

				if (node == null)
					nodes = treeView.Nodes;
				else 
					nodes = node.Nodes;

				bool bFound = false;
				for(int j=0;j<nodes.Count;j++)
				{
					if (aName[i] == nodes[j].Text) 
					{
						bFound = true;
						nodeThis = nodes[j];
						break;
					}
				}
				if (bFound == false)
					break;

				node = nodeThis;

			}

			if (nodeThis!= null && nodeThis.Parent != null)
				nodeThis.Parent.Expand();

			treeView.SelectedNode = nodeThis;
		}
		*/

		private void treeView1_DoubleClick(object sender, System.EventArgs e)
		{
			// ��ǰ��ѡ���node
			if (treeView1.SelectedNode == null) 
				return ;

			TreeNode node = treeView1.SelectedNode;
			if (node.ImageIndex == 0) // Ŀ¼
				return;

			button_modify_Click(null, null);
		}

		// ����
		private void button_import_Click(object sender, System.EventArgs e)
		{
			// δ����Control��, һ�㵼�빦�� -- ���뵱ǰĿ¼
			if (!(Control.ModifierKeys == Keys.Control))
			{
				button_PasteFromFile_Click(null, null);
				return;
			}

			int nRet ;
			string strError = "";

			// ѯ��project*.xml�ļ�ȫ·��
			OpenFileDialog projectDefFileDlg = new OpenFileDialog();

			projectDefFileDlg.FileName = "outer_projects.xml";
			projectDefFileDlg.InitialDirectory = Environment.CurrentDirectory;
			projectDefFileDlg.Filter = "projects files (outer*.xml)|outer*.xml|All files (*.*)|*.*" ;
			//dlg.FilterIndex = 2 ;
			projectDefFileDlg.RestoreDirectory = true ;

			if(projectDefFileDlg.ShowDialog() != DialogResult.OK)
			{
				return;
			}

			ScriptManager newScriptManager = new ScriptManager();
			newScriptManager.applicationInfo = null;	//applicationInfo;
			newScriptManager.CfgFilePath = projectDefFileDlg.FileName;
			newScriptManager.Load();

			// ѡȡҪImport��Project��

			GetProjectNameDlg nameDlg = new GetProjectNameDlg();
            GuiUtil.AutoSetDefaultFont(nameDlg);

			nameDlg.Text = "��ѡ��Ҫ������ⲿ������";
			nameDlg.scriptManager = newScriptManager;
			/*
			nameDlg.textBox_projectName.Text = applicationInfo.GetString(
				"projectmanagerdlg_import",
				"lastUsedProject",
				"");
			*/

			nameDlg.StartPosition = FormStartPosition.CenterScreen;
			nameDlg.ShowDialog(this);

			if (nameDlg.DialogResult != DialogResult.OK)
				return;

			string strSourceProjectName = nameDlg.ProjectName;

			string strSourceLocate = "";

			// ���Դ��������
			// strProjectNamePath	������������·��
			// return:
			//		-1	error
			//		0	not found project
			//		1	found
			nRet = newScriptManager.GetProjectData(
				strSourceProjectName,
				out strSourceLocate);
			if (nRet == -1) 
			{
				MessageBox.Show(this, "source GetProjectData() "+ strSourceProjectName + " error...");
				return ;
			}
			if (nRet == 0)
			{
				MessageBox.Show(this, "source project "+ strSourceProjectName + " not found error...");
				return ;
			}


			/*
			applicationInfo.SetString(
				"projectmanagerdlg_import",
				"lastUsedProject",
				nameDlg.textBox_projectName.Text);
			*/

			REDOEXPORT:

				string strTargetLocate = "";
			// ���Ŀ�귽������
			// strProjectNamePath	������������·��
			// return:
			//		-1	error
			//		0	not found project
			//		1	found
			nRet = this.scriptManager.GetProjectData(
				strSourceProjectName,
				out strTargetLocate);
			if (nRet == -1) 
			{
				MessageBox.Show(this, "target GetProjectData() "+ strSourceProjectName + " error...");
				return ;
			}

			// ����������ѯ���Ƿ񸲸�
			if (nRet == 1) 
			{
				string strText = "��ǰ�Ѿ�������Դ '"
					+ strSourceProjectName + "' ͬ����Ŀ�귽��(����Ŀ¼λ��'"
					+ strTargetLocate + "')��\r\n\r\n" 
					+ "�����Ƿ񸲸����е�Ŀ�귽��?\r\n(Yes=����; No=�������룻Cancel=��������)";


				DialogResult msgResult = MessageBox.Show(this,
					strText,
					"script",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button2);

				
				if (msgResult == DialogResult.Cancel) 
					return;


				if (msgResult == DialogResult.Yes) 
				{	// ����
					// ����Ŀ¼
					nRet = PathUtil.CopyDirectory(strSourceLocate,
						strTargetLocate,
						true,
						out strError);
					if (nRet == -1)
						goto ERROR1;
					goto END1;
				}
				else 
				{	// ����


					// ѯ��������
					nameDlg = new GetProjectNameDlg();
                    GuiUtil.AutoSetDefaultFont(nameDlg);

					nameDlg.Text = "���ƶ�Ŀ��(ϵͳ��)�·�����";
					nameDlg.scriptManager = this.scriptManager;
					nameDlg.ProjectName = strSourceProjectName;

					nameDlg.StartPosition = FormStartPosition.CenterScreen;
					nameDlg.ShowDialog(this);

					if (nameDlg.DialogResult != DialogResult.OK)
						goto END2;

					strSourceProjectName = nameDlg.ProjectName;
					goto REDOEXPORT;

				}



			}
			else // ��������ֱ�Ӹ���
			{
				// ����һ���µ�project�����strTargetLocate
				int nPrefixNumber = -1;	// 0
				strTargetLocate = this.scriptManager.NewProjectLocate(
					PathUtil.PureName(strSourceLocate),	// ����ȡ��Դ��ͬ��ĩ��Ŀ¼��
					ref nPrefixNumber);

				// ����Ŀ¼
				nRet = PathUtil.CopyDirectory(strSourceLocate,
					strTargetLocate,
					true,
					out strError);
				if (nRet == -1)
					goto ERROR1;

				// ʵ�ʲ���project����
				XmlNode projNode = this.scriptManager.NewProjectNode(
					strSourceProjectName,	// ����ԭ��������
					strTargetLocate,
					false);	// false��ʾ����Ҫ����Ŀ¼��ȱʡ�ļ�

				// ������ʾ?
				scriptManager.RefreshTree(treeView1);
			}

			END1:

				this.scriptManager.Save();



			TreeViewUtil.SelectTreeNode(treeView1, 
				strSourceProjectName,
                '/');

			MessageBox.Show(this, "�ⲿ���� '" + strSourceProjectName + "' �Ѿ��ɹ����뱾ϵͳ��");

			return;
			END2:
				return;
			ERROR1:
				MessageBox.Show(this, strError);
			return ;
		}

		// ����
		private void button_export_Click(object sender, System.EventArgs e)
		{
			// δ����Control��, һ�㵼������
			if (!(Control.ModifierKeys == Keys.Control))
			{
				button_CopyToFile_Click(null, null);
				return;
			}

			// ���⵼������
			int nRet ;
			string strError = "";

			TreeNode node = treeView1.SelectedNode;

			if (node == null) 
			{
				MessageBox.Show(this,"����ѡ��Ҫ�����ķ���...");
				return;
			}

			string strSourceProjectName = node.FullPath;

			string strSourceLocate = "";

			// ���Դ��������
			// strProjectNamePath	������������·��
			// return:
			//		-1	error
			//		0	not found project
			//		1	found
			nRet = scriptManager.GetProjectData(
				strSourceProjectName,
				out strSourceLocate);
			if (nRet == -1) 
			{
				MessageBox.Show(this, "source GetProjectData() "+ strSourceProjectName + " error...");
				return ;
			}
			if (nRet == 0)
			{
				MessageBox.Show(this, "source project "+ strSourceProjectName + " not found error...");
				return ;
			}



			// ѯ��project*.xml�ļ�ȫ·��
			SaveFileDialog projectDefFileDlg = new SaveFileDialog();

			projectDefFileDlg.CreatePrompt = false;
			projectDefFileDlg.FileName = "outer_projects.xml";
			projectDefFileDlg.InitialDirectory = Environment.CurrentDirectory;
			projectDefFileDlg.Filter = "projects files (outer*.xml)|outer*.xml|All files (*.*)|*.*" ;
			//dlg.FilterIndex = 2 ;
			projectDefFileDlg.RestoreDirectory = true ;

			if(projectDefFileDlg.ShowDialog() != DialogResult.OK)
			{
				return;
			}

			// ����ļ������ڣ��򴴽�֮
			if (File.Exists(projectDefFileDlg.FileName) == false)
				ScriptManager.CreateDefaultProjectsXmlFile(projectDefFileDlg.FileName,
					"outercfgs");

			// ����ScriptManager����
			ScriptManager newScriptManager = new ScriptManager();
			newScriptManager.applicationInfo = null;	//applicationInfo;
			newScriptManager.CfgFilePath = projectDefFileDlg.FileName;
			newScriptManager.Load();

			// ��ѯProject·��+���Ƿ��Ѿ��������projects.xml�Ѿ�����

			REDOEXPORT:

				string strTargetLocate = "";
			// ��÷�������
			// strProjectNamePath	������������·��
			// return:
			//		-1	error
			//		0	not found project
			//		1	found
			nRet = newScriptManager.GetProjectData(
				strSourceProjectName,
				out strTargetLocate);
			if (nRet == -1) 
			{
				MessageBox.Show(this, "target GetProjectData() "+ strSourceProjectName + " error...");
				return ;
			}

			// ����������ѯ���Ƿ񸲸�
			if (nRet == 1) 
			{
				string strText = "�ⲿ������\r\n  (���ļ� '" + projectDefFileDlg.FileName + "' ����)\r\n�Ѿ�����һ����Դ \r\n'"
					+ strSourceProjectName + "'\r\n ͬ���ķ���\r\n  (�����Ŀ¼λ�� '"
					+ strTargetLocate + "')��\r\n\r\n" 
					+ "�����Ƿ񸲸Ǵ˷���?\r\n(Yes=����; No=�����󵼳���Cancel=��������)\r\n\r\nע�⣺���Ǻ��޷���ԭ��";


				DialogResult msgResult = MessageBox.Show(this,
					strText,
					"script",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button2);

				
				if (msgResult == DialogResult.Cancel) 
					return;


				if (msgResult == DialogResult.Yes) 
				{	// ����
					// ����Ŀ¼
					nRet = PathUtil.CopyDirectory(strSourceLocate,
						strTargetLocate,
						true,
						out strError);
					if (nRet == -1)
						goto ERROR1;
					goto END1;
				}
				else 
				{	// ����


					// ѯ��������
					GetProjectNameDlg nameDlg = new GetProjectNameDlg();
                    GuiUtil.AutoSetDefaultFont(nameDlg);

					nameDlg.Text = "��ѡ��Ŀ��(�ⲿ)�·�����";
					nameDlg.scriptManager = newScriptManager;
					nameDlg.ProjectName = strSourceProjectName;

					nameDlg.StartPosition = FormStartPosition.CenterScreen;
					nameDlg.ShowDialog(this);

					if (nameDlg.DialogResult != DialogResult.OK)
						goto END2;

					strSourceProjectName = nameDlg.ProjectName;
					goto REDOEXPORT;

				}



			}
			else // ��������ֱ�Ӹ���
			{
				// ����һ���µ�project�����strTargetLocate
				int nPrefixNumber = -1;	// 0
				strTargetLocate = newScriptManager.NewProjectLocate(
					PathUtil.PureName(strSourceLocate),	// ����ȡ��Դ��ͬ��ĩ��Ŀ¼��
					ref nPrefixNumber);

				// ����Ŀ¼
				nRet = PathUtil.CopyDirectory(strSourceLocate,
					strTargetLocate,
					true,
					out strError);
				if (nRet == -1)
					goto ERROR1;

				// ʵ�ʲ���project����
				XmlNode projNode = newScriptManager.NewProjectNode(
					strSourceProjectName,	// ����ԭ��������
					strTargetLocate,
					false);	// false��ʾ����Ҫ����Ŀ¼��ȱʡ�ļ�

			}

			END1:

				newScriptManager.Save();
			MessageBox.Show(this, "���� '" + strSourceProjectName
				+ "' \r\n�Ѿ��ɹ��������ļ� \r\n'" 
				+ newScriptManager.CfgFilePath + "' \r\n��������ⲿ�������ڡ�");

			return;
			END2:
				return;
			ERROR1:
				MessageBox.Show(this, strError);
			return ;
		}

		// ����
		private void button_CopyToClipboard_Click(object sender, System.EventArgs e)
		{

			int nRet;
			// ��ǰ��ѡ���node
			if (treeView1.SelectedNode == null) 
			{
				MessageBox.Show("��δѡ�񷽰�����Ŀ¼");
				return ;
			}

			TreeNode node = treeView1.SelectedNode;
			if (node.ImageIndex == 0) // Ŀ¼
			{

			}
			else 
			{
				string strProjectNamePath = node.FullPath;

				string strLocate = "";

				// ��÷�������
				// strProjectNamePath	������������·��
				// return:
				//		-1	error
				//		0	not found project
				//		1	found
				nRet = scriptManager.GetProjectData(
					strProjectNamePath,
					out strLocate);
				if (nRet != 1) 
				{
					MessageBox.Show("���� "+ strProjectNamePath + " ��ScriptManager��û���ҵ�");
					return ;
				}

                Project project = null;
                try
                {

                    project = Project.MakeProject(
                        strProjectNamePath,
                        strLocate);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "MakeProject error : " + ex.Message);
                    return;
                }

				Clipboard.SetDataObject(project);

			}


		}

		// ���, �����Ƶ��ļ�
		private void button_CopyToFile_Click(object sender, System.EventArgs e)
		{
			int nRet;
			// ��ǰ��ѡ���node
			if (treeView1.SelectedNode == null) 
			{
				MessageBox.Show("��δѡ�񷽰�����Ŀ¼");
				return ;
			}

			TreeNode node = treeView1.SelectedNode;
			if (node.ImageIndex == 0) // Ŀ¼
			{

			}
			else 
			{
				string strProjectNamePath = node.FullPath;

				string strLocate = "";

				// ��÷�������
				// strProjectNamePath	������������·��
				// return:
				//		-1	error
				//		0	not found project
				//		1	found
				nRet = scriptManager.GetProjectData(
					strProjectNamePath,
					out strLocate);
				if (nRet != 1) 
				{
					MessageBox.Show("���� "+ strProjectNamePath + " ��ScriptManager��û���ҵ�");
					return ;
				}

				string strPath;
				string strName;

				// �������ķ���"����·��"��,����·������
				ScriptManager.SplitProjectPathName(strProjectNamePath,
					out strPath,
					out strName);

                Project project = null;

                try
                {
                    project = Project.MakeProject(
                         strProjectNamePath,
                         strLocate);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "MakeProject error : " + ex.Message);
                    return;
                }

                // Ŀǰ������host����Ϊ�գ��������������Լ��
                string strHostName = project.GetHostName();
                if (string.IsNullOrEmpty(strHostName) == false
                    && strHostName != this.HostName)
                {
                    string strError = "�⵼���ķ�����(��metadata.xml�����)������Ϊ '" + strHostName + "', �����ϵ�ǰ���ڵ������� '" + this.HostName + "'���ܾ�����";
                    MessageBox.Show(strError);
                    return;
                }

				// ѯ�ʰ��ļ�ȫ·��
				SaveFileDialog dlg = new SaveFileDialog();

				dlg.Title = "�������� -- ��ָ��Ҫ������ļ���";
				dlg.CreatePrompt = true;
				dlg.FileName = strName + ".projpack";
				dlg.InitialDirectory = strRecentPackageFilePath == "" ?
					Environment.GetFolderPath(Environment.SpecialFolder.Personal)
					: strRecentPackageFilePath; //Environment.CurrentDirectory;
				dlg.Filter = "��������ļ� (*.projpack)|*.projpack|All files (*.*)|*.*" ;
				dlg.RestoreDirectory = true ;

				if(dlg.ShowDialog() != DialogResult.OK)
				{
					return;
				}

				strRecentPackageFilePath = dlg.FileName;


				// Clipboard.SetDataObject(project);
				ProjectCollection array = new ProjectCollection();

				array.Add(project);

				///
				//Opens a file and serializes the object into it in binary format.
				Stream stream = File.Open(dlg.FileName, FileMode.Create);
				BinaryFormatter formatter = new BinaryFormatter();

				formatter.Serialize(stream, array);
				stream.Close();
			}


		}


		// ���, �����ļ����Ƶ���ǰ���� ���뵱ǰѡ����Ŀ¼
		private void button_PasteFromFile_Click(object sender, System.EventArgs e)
		{
            string strError = "";
            // ѯ�ʰ��ļ�ȫ·��
			OpenFileDialog dlg = new OpenFileDialog();

			dlg.Title = "���뷽�� -- ��ָ��Ҫ�򿪵��ļ���";
			dlg.FileName = "";	// strName + ".projpack";
			dlg.InitialDirectory = strRecentPackageFilePath == "" ?
				Environment.GetFolderPath(Environment.SpecialFolder.Personal)
				: strRecentPackageFilePath; //Environment.CurrentDirectory;
			dlg.Filter = "��������ļ� (*.projpack)|*.projpack|All files (*.*)|*.*";	// projects package files
			dlg.RestoreDirectory = true ;

			if(dlg.ShowDialog() != DialogResult.OK)
			{
				return;
			}

			strRecentPackageFilePath = dlg.FileName;

			Stream stream = null;
			try 
			{
				stream = File.Open(dlg.FileName, FileMode.Open);
			}
			catch (FileNotFoundException)
			{
				MessageBox.Show(this, "�ļ� " + dlg.FileName + "������...");
				return;
			}

			BinaryFormatter formatter = new BinaryFormatter();

			ProjectCollection projects = null;
			try 
			{
				projects = (ProjectCollection)formatter.Deserialize(stream);
			}
			catch (SerializationException ex) 
			{
				MessageBox.Show("װ�ش���ļ�����" + ex.Message);
				return;
			}
			finally  
			{
				stream.Close();
			}

            FileInfo fi = new FileInfo(dlg.FileName);
            string strLastModified = DateTimeUtil.Rfc1123DateTimeString(fi.LastWriteTimeUtc);

			for(int i=0;i<projects.Count;i++)
			{
				Project project = (Project)projects[i];

                string strHostName = project.GetHostName();
                if (string.IsNullOrEmpty(strHostName) == false
                && strHostName != this.HostName)
                {
                    strError = "�⵼�뷽�� '"+project.NamePath+"' ������Ϊ '" + strHostName + "', �����ϵ�ǰ���ڵ������� '" + this.HostName + "'�����ܾ����롣";
                    MessageBox.Show(strError);
                    continue;
                }

				int nRet = PasteProject(project,
                    strLastModified,
                    false,
                    out strError);
				if (nRet == -1) 
				{
					MessageBox.Show(strError);
					return;
				}
			}
		}



		// ��Project����Paste�����������
		// bRestoreOriginNamePath	�Ƿ�ָ���ԭʼ����·����==false����ʾ�ָ���treeview��ǰĿ¼
		private int PasteProject(Project project,
            string strLastModified,
			bool bRestoreOriginNamePath,
			out string strError)
		{
			strError = "";

			string strPath;
			string strName;

			// ��Project��
			ScriptManager.SplitProjectPathName(project.NamePath,
				out strPath,
				out strName);

			string strCurPath = "";

			// �����Ŀ¼

			int nRet;
			TreeNode node = null;
			TreeNode parent = null;


			// �ָ�ԭʼ����·��
			if (bRestoreOriginNamePath == true)
			{
				if (strPath == "")
					parent = null;
				XmlNode xmlNode = scriptManager.LocateDirNode(
					strPath);
				if (xmlNode == null) 
				{
					xmlNode = scriptManager.NewDirNode(
						strPath);
					// ������ʾ?
					scriptManager.RefreshTree(treeView1);
					TreeViewUtil.SelectTreeNode(treeView1, 
						strPath,
                        '/');
				}
				else 
				{
					TreeViewUtil.SelectTreeNode(treeView1, 
						strPath,
                        '/');
				}

			}

			// �ָ�����ǰĿ¼
		{
			node = treeView1.SelectedNode;
			// ��ǰ��ѡ���node
			if (node == null) 
			{
				// ��
			}
			else 
			{

				if (node.ImageIndex == 0) // Ŀ¼
				{
					parent = node;
					strCurPath = node.FullPath;
				}
				else 
				{
					parent = node.Parent;
					if (parent != null)
						strCurPath = parent.FullPath;
				}
			}
		}

			// ������ǰĿ¼���Ƿ��Ѿ�����ͬ��Project

			string strLocate;
			// ��÷�������
			// strProjectNamePath	������������·��
			// return:
			//		-1	error
			//		0	not found project
			//		1	found
			nRet = scriptManager.GetProjectData(
				ScriptManager.MakeProjectPathName(strCurPath, strName),
				out strLocate);
			if (nRet == -1) 
			{
				strError = "GetProjectData "+ ScriptManager.MakeProjectPathName(strCurPath, strName) + " error";
				return -1;
			}

			int nPrefixNumber = 0;

			if (nRet == 0) 
			{
				nPrefixNumber = -1;
			}
			else 
			{
				// ����paste����

				// �ڶ������ҵ�һ�����ظ�������
				strName = GetTempProjectName(treeView1,
					parent,
					strName,
					ref nPrefixNumber);
			}

			string strLocatePrefix = "";
			if (project.Locate == "") 
			{
				strLocatePrefix = strName;
			}
			else 
			{
				strLocatePrefix = PathUtil.PureName(project.Locate);
			}

			strLocate = scriptManager.NewProjectLocate(
				strLocatePrefix,
				ref nPrefixNumber);

			string strNamePath = ScriptManager.MakeProjectPathName(strCurPath, strName);

			// ֱ��paste
			project.WriteToLocate(strLocate,
                true);

			// ʵ�ʲ���project����
			XmlNode projNode = scriptManager.NewProjectNode(
				strNamePath,
				strLocate,
				false);	// false��ʾ����Ҫ����Ŀ¼��ȱʡ�ļ�

            DomUtil.SetAttr(projNode, "lastModified",
strLastModified);

			// ������ʾ?
			scriptManager.FillOneLevel(treeView1, 
				parent, 
				projNode.ParentNode);
			TreeViewUtil.SelectTreeNode(treeView1, 
				scriptManager.GetNodePathName(projNode),
                '/');

			scriptManager.Save();

			TreeViewUtil.SelectTreeNode(treeView1, 
				strNamePath,
                '/');

			return 0;
		}

		// ճ��������ǰĿ¼
		private void button_PasteFromClipboard_Click(object sender, System.EventArgs e)
		{
            string strError = "";

			IDataObject iData = Clipboard.GetDataObject();
			if (iData == null
				|| iData.GetDataPresent(typeof(Project)) == false)
			{
				strError = "���������в�����Project��������";
                goto ERROR1;
			}

			Project project = (Project)iData.GetData(typeof(Project));

			if (project == null) 
			{
				strError = "GetData error";
				goto ERROR1;
			}

            string strHostName = project.GetHostName();
            if (string.IsNullOrEmpty(strHostName) == false 
                && strHostName != this.HostName)
            {
                strError = "���棺��ճ���ķ���������Ϊ '" + strHostName + "', �����ϵ�ǰ���ڵ������� '" + this.HostName + "'����ע����ճ����ɺ��޸���������(λ��metadata.xml��)";
                MessageBox.Show(strError);
            }

			int nRet = PasteProject(project,
                "",
                false,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(strError);
            return;
        }

        // ճ������ԭʼĿ¼
        private void button_PasteFromClipboardToOriginDir_Click(object sender, System.EventArgs e)
        {
            string strError = "";
            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null
                || iData.GetDataPresent(typeof(Project)) == false)
            {
                MessageBox.Show(this, "���������в�����Project��������");
                return;
            }

            Project project = (Project)iData.GetData(typeof(Project));
            if (project == null)
            {
                strError = "GetData error";
                goto ERROR1;
            }

            // TODO: �Ƿ��������ճ�������Ǿ���һ�¼��ɣ�
            string strHostName = project.GetHostName();
            if (string.IsNullOrEmpty(strHostName) == false
                && strHostName != this.HostName)
            {
                strError = "���棺��ճ���ķ���������Ϊ '" + strHostName + "', �����ϵ�ǰ���ڵ������� '"+this.HostName+"'����ע����ճ����ɺ��޸���������(λ��metadata.xml��)";
                MessageBox.Show(strError);
            }

            int nRet = PasteProject(project, 
                "",
                true,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(strError);
            return;
        }

		// �õ���������Project�����ԭʼ����Ŀ¼
		string GetClipboardProjectDir()
		{
			IDataObject iData = Clipboard.GetDataObject();
			if (iData == null
				|| iData.GetDataPresent(typeof(Project)) == false)
			{
				// "���������в�����Project��������"
				return "";
			}

			Project project = (Project)iData.GetData(typeof(Project));

			if (project == null) 
			{
				// GetData error;
				return "";
			}

			string strPath;
			string strName;
			ScriptManager.SplitProjectPathName(project.NamePath, out strPath, out strName);
			return strPath;
		}

		string GetCurTreeDir()
		{
			TreeNode node = treeView1.SelectedNode;
			// ��ǰ��ѡ���node
			if (node == null) 
			{
				// ��
				return "";
			}
			else 
			{

				if (node.ImageIndex == 0) // Ŀ¼
				{
					return node.FullPath;
				}
				else 
				{
					TreeNode parent = node.Parent;
					if (parent != null)
						return parent.FullPath;
					return "";
				}
			}
		}

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public void EnableControls(bool bEnable)
        {
            this.treeView1.Enabled = bEnable;

            if (bEnable == false)
            {
                this.button_modify.Enabled = bEnable;
                this.button_new.Enabled = bEnable;
                this.button_delete.Enabled = bEnable;
                this.button_up.Enabled = bEnable;
                this.button_down.Enabled = bEnable;
                this.button_export.Enabled = bEnable;
            }
            if (bEnable == true)
            {
                treeView1_AfterSelect(null, null);
            }

            this.button_import.Enabled = bEnable;
            this.button_updateProjects.Enabled = bEnable;
            this.button_OK.Enabled = bEnable;
        }

        // �Ӵ���Ŀ¼������
        private void button_updateProjectsFromDisk_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strWarning = "";
            string strUpdateInfo = "";
            int nUpdateCount = 0;

            FolderBrowserDialog dir_dlg = new FolderBrowserDialog();

            dir_dlg.Description = "��ָ����������Ŀ¼:";
            dir_dlg.RootFolder = Environment.SpecialFolder.MyComputer;
            dir_dlg.ShowNewFolderButton = false;

            if (dir_dlg.ShowDialog() != DialogResult.OK)
                return;


            this.EnableControls(false);
            try
            {
                bool bHideMessageBox = false;
                bool bDontUpdate = false;
                // ������һ�������ڵ��µ�ȫ������
                // parameters:
                //      dir_node    �����ڵ㡣��� == null ������ȫ������
                //      strSource   "!url"���ߴ���Ŀ¼���ֱ��ʾ����������£����ߴӴ��̼�����
                // return:
                //      -1  ����
                //      0   �ɹ�
                int nRet = this.scriptManager.CheckUpdate(
                    this,
                    null,
                    dir_dlg.SelectedPath,
                    ref bHideMessageBox,
                    ref bDontUpdate,
                    ref nUpdateCount,
                    ref strUpdateInfo,
                    ref strWarning,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            finally
            {
                this.EnableControls(true);
            }

            if (string.IsNullOrEmpty(strWarning) == false)
                MessageBox.Show(this, strWarning);

            if (string.IsNullOrEmpty(strUpdateInfo) == false)
                MessageBox.Show(this, "���з����Ѿ�����:\r\n" + strUpdateInfo);
            else
                MessageBox.Show(this, "û�з��ָ���");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �� dp2003.com ������
        private void button_updateProjects_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strWarning = "";
            string strUpdateInfo = "";
            int nUpdateCount = 0;

            this.EnableControls(false);
            try
            {
                bool bHideMessageBox = false;
                bool bDontUpdate = false;
                // ������һ�������ڵ��µ�ȫ������
                // parameters:
                //      dir_node    �����ڵ㡣��� == null ������ȫ������
                //      strSource   "!url"���ߴ���Ŀ¼���ֱ��ʾ����������£����ߴӴ��̼�����
                // return:
                //      -1  ����
                //      0   �ɹ�
                int nRet = this.scriptManager.CheckUpdate(
                    this,
                    null,
                    "!url",
                    ref bHideMessageBox,
                    ref bDontUpdate,
                    ref nUpdateCount,
                    ref strUpdateInfo,
                    ref strWarning,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            finally
            {
                this.EnableControls(true);
            }

            if (string.IsNullOrEmpty(strWarning) == false)
                MessageBox.Show(this, strWarning);

            if (string.IsNullOrEmpty(strUpdateInfo) == false)
                MessageBox.Show(this, "���з����Ѿ�����:\r\n" + strUpdateInfo);
            else
                MessageBox.Show(this, "û�з��ָ���");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

#if NO
        // ������һ�������ڵ��µ�ȫ������
        // parameters:
        //      dir_node    �����ڵ㡣��� == null ������ȫ������
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int CheckUpdate(
            TreeNode dir_node,
            ref int nUpdateCount,
            ref string strUpdateInfo,
            ref string strWarning,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            TreeNodeCollection nodes = null;
            if (dir_node == null)
                nodes = this.treeView1.Nodes;
            else
                nodes = dir_node.Nodes;

            foreach (TreeNode node in nodes)
            {
                if (node.ImageIndex == 0)
                {
                    // Ŀ¼�ڵ�
                    nRet = CheckUpdate(node, 
                        ref nUpdateCount,
                        ref strUpdateInfo,
                        ref strWarning,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }
                else
                {
                    // �����ڵ�
                    // return:
                    //      -1  ����
                    //      0   û�и���
                    //      1   �Ѿ�����
                    //      2   ��ΪĳЩԭ���޷�������
                    nRet = CheckUpdateOneProject(node,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 2)
                        strWarning += "���� " + node.FullPath + " "+strError+";\r\n";

                    if (nRet == 1)
                    {
                        nUpdateCount++;
                        strUpdateInfo += node.FullPath + "\r\n";
                    }
                }
            }

            return 0;
        }

        // ������һ������
        // return:
        //      -1  ����
        //      0   û�и���
        //      1   �Ѿ�����
        //      2   ��ΪĳЩԭ���޷�������
        public int CheckUpdateOneProject(TreeNode node,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strProjectNamePath = node.FullPath;
            string strLocate = "";
            string strIfModifySince = "";

            // ��÷�������
            // strProjectNamePath	������������·��
            // return:
            //		-1	error
            //		0	not found project
            //		1	found
            nRet = scriptManager.GetProjectData(
                strProjectNamePath,
                out strLocate,
                out strIfModifySince);
            if (nRet != 1)
            {
                strError = "���� " + strProjectNamePath + " ��ScriptManager��û���ҵ�";
                return -1;
            }

            // �������URL

            XmlDocument metadata_dom = null;
            // ���(һ���Ѿ���װ��)����Ԫ����
            // parameters:
            //      dom ����Ԫ����XMLDOM
            // return:
            //      -1  ����
            //      0   û���ҵ�Ԫ�����ļ�
            //      1   �ɹ�
            nRet = ScriptManager.GetProjectMetadata(strLocate,
            out metadata_dom,
            out strError);

            if (nRet == -1)
                return -1;

            if (nRet == 0)
            {
                strError = "Ԫ�����ļ������ڣ�����޷�������";
                return 2;   // û��Ԫ�����ļ����޷�����
            }

            if (metadata_dom.DocumentElement == null)
            {
                strError = "Ԫ����DOM�ĸ�Ԫ�ز����ڣ�����޷�������";
                return 2;
            }

            string strUpdateUrl = DomUtil.GetAttr(metadata_dom.DocumentElement,
                "updateUrl");
            if (string.IsNullOrEmpty(strUpdateUrl) == true)
            {
                strError = "Ԫ����D��û�ж���updateUrl���ԣ�����޷�������";
                return 2;
            }

            // ��������ָ�����ں���¹����ļ�

            Debug.Assert(string.IsNullOrEmpty(this.DataDir) == false, "");

            string strLocalFileName = PathUtil.MergePath(this.DataDir, "~temp_project.projpack");
            string strTempFileName = PathUtil.MergePath(this.DataDir, "~temp_download_webfile");

            string strLastModified = "";
            nRet = WebFileDownloadDialog.DownloadWebFile(
                this,
                strUpdateUrl,
                strLocalFileName,
                strTempFileName,
                strIfModifySince,
                out strLastModified,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                return 0;
            }

            if (string.IsNullOrEmpty(strIfModifySince) == false
                && string.IsNullOrEmpty(strLastModified) == false)
            {
                DateTime ifmodifiedsince = DateTimeUtil.FromRfc1123DateTimeString(strIfModifySince);
                DateTime lastmodified = DateTimeUtil.FromRfc1123DateTimeString(strLastModified);
                if (ifmodifiedsince == lastmodified)
                    return 0;
            }

            nRet = UpdateProject(
            strLocalFileName,
            strLocate,
            out strError);
            if (nRet == -1)
                return -1;

            nRet = scriptManager.SetProjectData(
    strProjectNamePath,
    strLastModified);
            scriptManager.Save();

            return 1;
        }

        // ����Project
        private int UpdateProject(
            string strFilename,
            string strExistLocate,
            out string strError)
        {
            strError = "";

            Project project = null;
            Stream stream = null;
            try
            {
                stream = File.Open(strFilename, FileMode.Open);
            }
            catch (FileNotFoundException)
            {
                strError = "�ļ� " + strFilename + "������...";
                return -1;
            }

            BinaryFormatter formatter = new BinaryFormatter();

            ProjectCollection projects = null;
            try
            {
                projects = (ProjectCollection)formatter.Deserialize(stream);
            }
            catch (SerializationException ex)
            {
                strError = "װ�ش���ļ�����" + ex.Message;
                return -1;
            }
            finally
            {
                stream.Close();
            }

            if (projects.Count == 0)
            {
                strError = ".projpack�ļ���û�а����κ�Project";
                return -1;
            }
            if (projects.Count > 1)
            {
                strError = ".projpack�ļ��а����˶��������Ŀǰ�ݲ�֧�ִ����л�ȡ������";
                return -1;
            }

            project = (Project)projects[0];

            // ɾ������Ŀ¼�е�ȫ���ļ�
            try
            {
                Directory.Delete(strExistLocate, true);
            }
            catch (Exception ex)
            {
                strError = "ɾ��Ŀ¼ʱ����: " + ex.Message;
                return -1;
            }
            PathUtil.CreateDirIfNeed(strExistLocate);

            // ֱ��paste
            project.WriteToLocate(strExistLocate);
            return 0;
        }

#endif
	}

    public delegate void AutoCreateProjectXmlFileEventHandle(object sender,
AutoCreateProjectXmlFileEventArgs e);

    public class AutoCreateProjectXmlFileEventArgs : EventArgs
    {
        public string Filename = "";
    }
}
