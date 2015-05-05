using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.IO;

using System.Diagnostics;

using DigitalPlatform.GUI;
using DigitalPlatform.rms.Client.rmsws_localhost;
using DigitalPlatform.Text;
using DigitalPlatform.Xml;


namespace DigitalPlatform.rms.Client
{
	/*
	// ����洢ģʽ
	public enum StorageMode
	{
		None = 0,	// ��δ����
		Real = 1,	// ��ʵ����
		Memory = 2,	// �ڴ����
	}
	*/

	/// <summary>
	/// ���ݿ������������ �� �� �ؼ�
	/// </summary>
	public class DatabaseObjectTree : System.Windows.Forms.TreeView
	{
        public bool EnableDefaultFileEditing = false;

        public int DbStyle = 0;   // ���ݿ�Style
		public ApplicationInfo applicationInfo = null;

		public ObjEventCollection Log = new ObjEventCollection();

		// public StorageMode StorageMode = StorageMode.None;	// �洢ģʽ��ʱû�ж�

		public DigitalPlatform.StopManager stopManager = null;

		RmsChannel channel = null;

		public string Lang = "zh";

		public ServerCollection Servers = null;	// ����
		public RmsChannelCollection Channels = null;

		public string ServerUrl = "";
		// public string DbName = "";
		string m_strDbName = "";

		public bool DisplayRoot = true;

		//
		public DatabaseObject Root = new DatabaseObject();	// �ڴ�������ĸ����������

		public event GuiAppendMenuEventHandle OnSetMenu;

		public event OnObjectDeletedEventHandle OnObjectDeleted;

		private System.Windows.Forms.ImageList imageList_resIcon;
		private System.ComponentModel.IContainer components;

		public DatabaseObjectTree()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			this.ImageList = imageList_resIcon;

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

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(DatabaseObjectTree));
			this.imageList_resIcon = new System.Windows.Forms.ImageList(this.components);
			// 
			// imageList_resIcon
			// 
			this.imageList_resIcon.ImageSize = new System.Drawing.Size(16, 16);
			this.imageList_resIcon.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList_resIcon.ImageStream")));
			this.imageList_resIcon.TransparentColor = System.Drawing.Color.Fuchsia;
			// 
			// DatabaseObjectTree
			// 
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.DatabaseObjectTree_MouseUp);

		}
		#endregion

		public string DbName
		{
			get 
			{
				return m_strDbName;
			}
			set 
			{
				m_strDbName = value;
				if (this.DisplayRoot == true)
				{
					if (this.Nodes.Count != 0)
					{
						this.Nodes[0].Text = value;
                        this.Root.Name = value; // �ڴ��������ҲҪ�ı�

					}
				}
			}
		}

		public void Initial(ServerCollection servers,
			RmsChannelCollection channels,
			DigitalPlatform.StopManager stopManager,
			string serverUrl,
			string strDbName)
		{
			this.Servers = servers;
			this.Channels = channels;
			this.stopManager = stopManager;
			this.ServerUrl = serverUrl;
			this.DbName = strDbName;

            if (this.DbName != "")
            {

                // ������ݿ�Style
                string strError = "";
                this.DbStyle = this.GetDbStyle(
                    this.DbName,
                    out strError);
                if (this.DbStyle == -1)
                    throw new Exception(strError);

                // �÷������˻�õ���Ϣ�����
                Cursor save = this.Cursor;
                this.Cursor = Cursors.WaitCursor;
                FillAll(null);
                this.Cursor = save;
            }

		}

		public int CreateMemoryObject(out string strError)
		{
			strError = "";

            if (this.DbName == "" || this.DbName == "?")  // 2006/1/20
                return 0;

			DatabaseObject root = new DatabaseObject();
			root.Type = ResTree.RESTYPE_DB;
			root.Name = this.DbName;
            root.Style = this.DbStyle;
			int nRet = CreateObject(root,
				this.DbName,
				out strError);
			if (nRet == -1)
				return -1;

			this.Root = root;
			return 0;
		}

		// ���ȫ���ڵ�
		public void FillAll(TreeNode node)
		{
			string strError = "";


			int nRet = CreateMemoryObject(out strError);
			if (nRet == -1)
				MessageBox.Show(this, strError);

			Fill(node);

			TreeNodeCollection children = null;

			if (node == null) 
			{
				children = this.Nodes;
			}
			else 
			{
				node.Expand();
				children = node.Nodes;
			}

			for(int i=0;i<children.Count;i++)
			{
				TreeNode child = children[i];
				// ��Ҫչ��
				if (ResTree.IsLoading(child) == true) 
				{
					FillAll(child);
				}		
			}

		}

		string GetPath(TreeNode treenode)
		{
			if (this.DisplayRoot == false)
			{
				if (treenode == null)
					return this.DbName;
				return this.DbName + "/" + TreeViewUtil.GetPath(treenode, '/');
			}
			else
			{
				Debug.Assert(treenode != null, "����ʾ��ģʽ��, ������null���ñ�����");
				return TreeViewUtil.GetPath(treenode, '/');
			}

		}

        // ����TreeNode�ڵ���Style
        public int GetNodeStyle(TreeNode node)
        {
            if (this.Root == null)
                return 0;

            string strPath = "";
            if (node != null)
                strPath = TreeViewUtil.GetPath(node, '/');

            DatabaseObject obj = this.Root.LocateObject(strPath);
            if (obj == null)
                return 0;

            return obj.Style;
        }

		// �ݹ�
		public int Fill(TreeNode node)
		{
			TreeNodeCollection children = null;

			if (node == null) 
			{
				children = this.Nodes;
			}
			else 
			{
				children = node.Nodes;
			}

			int i;


			// ����
			if (node == null) 
			{
				children.Clear();

				if (this.DisplayRoot == true)
				{
					if (true)
					{
						TreeNode nodeNew = new TreeNode(this.Root.Name, 
							this.Root.Type,
							this.Root.Type);
						ResTree.SetLoading(nodeNew);
						children.Add(nodeNew);
					}
					return 0;
				}
			}


			string strPath = "";



				children.Clear();

				if (node != null)
                    strPath = TreeViewUtil.GetPath(node, '/');
				else
				{
					strPath = "";
				}

				DatabaseObject parent = this.Root.LocateObject(strPath);
				if (parent == null)
				{
					Debug.Assert(false, "path not found" );
					return -1;	// ·��û���ҵ�
				}

				for(i=0;i<parent.Children.Count;i++) 
				{
					DatabaseObject child = (DatabaseObject)parent.Children[i];

					// ����from���ͽڵ�
					if (child.Type == ResTree.RESTYPE_FROM)
						continue;

					TreeNode nodeNew = new TreeNode(child.Name, child.Type, child.Type);


					Debug.Assert(child.Type != -1, "����ֵ��δ��ʼ��");

					if (child.Type == ResTree.RESTYPE_FOLDER)
						ResTree.SetLoading(nodeNew);

					children.Add(nodeNew);
				}

				return 0;

		}

		// ���ڴ�������TreeView��
		// (�ݲ��ı�������˵���������)
		// parameters:
		//		nodeInsertPos	����ο��ڵ㣬�ڴ�ǰ���롣���==null����ʾ��parent�¼�ĩβ׷��
		public int InsertObject(TreeNode nodeParent,
			TreeNode nodeInsertPos,
			DatabaseObject obj,
			out string strError)
		{
			strError = "";
			int nRet = 0;

			TreeNodeCollection children = null;

			if (nodeParent == null) 
			{
				children = this.Nodes;
			}
			else 
			{

				children = nodeParent.Nodes;
			}

			ArrayList aObject = new ArrayList();
			if (obj.Type == -1 || obj.Type == ResTree.RESTYPE_DB)
			{
				aObject.AddRange(obj.Children);
			}
			else 
			{
				aObject.Add(obj);
			}

			for(int i=0;i<aObject.Count;i++)
			{
				DatabaseObject perObj = (DatabaseObject)aObject[i];

				TreeNode nodeNew = new TreeNode(perObj.Name,
					perObj.Type, perObj.Type);

				Debug.Assert(perObj.Type != -1, "����ֵ��δ��ʼ��");
				Debug.Assert(perObj.Type != ResTree.RESTYPE_DB, "�������Ͳ���ΪDB");

				if (nodeInsertPos == null)
					children.Add(nodeNew);
				else 
				{
					int index = children.IndexOf(nodeInsertPos);
					if (index == -1)
						children.Add(nodeNew);
					else
						children.Insert(index, nodeNew);
				}

				string strPath = this.GetPath(nodeNew);

				// ���޸ļ�¼����־
				ObjEvent objevent = new ObjEvent();
				objevent.Obj = perObj;
				objevent.Oper = ObjEventOper.New;
				objevent.Path = strPath;
				this.Log.Add(objevent);
				/*
				// �ڷ������˶���
				if (this.StorageMode == StorageMode.Real)
				{
					MemoryStream stream = null;
					
					if (perObj.Type == ResTree.RESTYPE_FILE)
						stream = new MemoryStream(perObj.Content);

					string strPath = this.GetPath(nodeNew);
					nRet = NewServerSideObject(strPath,
						nodeNew.ImageIndex,
						stream,
						perObj.TimeStamp,
						out strError);
					if (nRet == -1)
						return -1;
				}
				*/


				if (perObj.Type == ResTree.RESTYPE_FOLDER)
				{
					// �ݹ�
					// ResTree.SetLoading(nodeNew);
					for(int j=0;j<perObj.Children.Count;j++)
					{

						nRet = InsertObject(nodeNew,
							null,
							(DatabaseObject)perObj.Children[j],
							out strError);
						if (nRet == -1)
							return -1;
					}

				}

				if (nodeNew.Parent != null)
					nodeNew.Parent.Expand();

			}

			return 0;
		}
		
		// �ص�����
		void DoStop(object sender, StopEventArgs e)
		{
			if (this.channel != null)
				this.channel.Abort();
		}

		private void DatabaseObjectTree_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if(e.Button != MouseButtons.Right)
				return;

			int nImageIndex = -1;
			if (this.SelectedNode != null)
				nImageIndex = this.SelectedNode.ImageIndex;

			ContextMenu contextMenu = new ContextMenu();
			MenuItem menuItem = null;


			menuItem = new MenuItem("�༭�����ļ�(&E)");
			menuItem.Click += new System.EventHandler(this.menu_editCfgFile);
			if (nImageIndex != ResTree.RESTYPE_FILE)
				menuItem.Enabled = false;
			contextMenu.MenuItems.Add(menuItem);

			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);


			menuItem = new MenuItem("�¶��� [ͬ��](&S)");
			menuItem.Click += new System.EventHandler(this.menu_newObjectSibling_Click);
			if (nImageIndex == ResTree.RESTYPE_DB || this.Nodes.Count == 0)
				menuItem.Enabled = false;
			else
				menuItem.Enabled = true;
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("�¶��� [�¼�](&H)");
			menuItem.Click += new System.EventHandler(this.menu_newObjectChild_Click);
			if (nImageIndex == ResTree.RESTYPE_FOLDER || nImageIndex == ResTree.RESTYPE_DB)
				menuItem.Enabled = true;
			else
				menuItem.Enabled = false;
			contextMenu.MenuItems.Add(menuItem);

			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			bool bHasClipboardObject = false;
			IDataObject iData = Clipboard.GetDataObject();
			if (iData == null
				|| iData.GetDataPresent(typeof(DatabaseObject)) == false)
				bHasClipboardObject = false;
			else
				bHasClipboardObject = true;


			menuItem = new MenuItem("������(&T)");
			menuItem.Click += new System.EventHandler(this.menu_copyTreeToClipboard_Click);
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("ճ����(&A)");
			menuItem.Click += new System.EventHandler(this.menu_pasteTreeFromClipboard_Click);
			if (bHasClipboardObject== false)
			{
				menuItem.Enabled = false;
			}
			contextMenu.MenuItems.Add(menuItem);

			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("����(&C)");
			menuItem.Click += new System.EventHandler(this.menu_copyObjectToClipboard_Click);
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("ճ��[ǰ��](&P)");
			menuItem.Click += new System.EventHandler(this.menu_pasteObjectFromClipboard_InsertBefore_Click);
			if (bHasClipboardObject== false)
			{
				menuItem.Enabled = false;
			}
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("ճ��[׷�ӵ��¼�ĩβ](&S)");
			menuItem.Click += new System.EventHandler(this.menu_pasteObjectFromClipboard_AppendChild_Click);
			if (bHasClipboardObject== false)
			{
				menuItem.Enabled = false;
			}
			contextMenu.MenuItems.Add(menuItem);

			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);


			menuItem = new MenuItem("ɾ������(&D)");
			menuItem.Click += new System.EventHandler(this.menu_deleteObject_Click);
			if (this.SelectedNode == null || nImageIndex == ResTree.RESTYPE_DB)
				menuItem.Enabled = false;
			contextMenu.MenuItems.Add(menuItem);

			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("ģʽ(&M)");
			menuItem.Click += new System.EventHandler(this.menu_displayMode_Click);
			contextMenu.MenuItems.Add(menuItem);

			////

			if (OnSetMenu != null)
			{
                GuiAppendMenuEventArgs newargs = new GuiAppendMenuEventArgs();
				newargs.ContextMenu = contextMenu;
				OnSetMenu(this, newargs);
				if (newargs.ContextMenu != contextMenu)
					contextMenu = newargs.ContextMenu;
			}		


			if (contextMenu != null)
				contextMenu.Show(this, new Point(e.X, e.Y) );		
	
		}


		// �����¶���
		void DoNewObject(bool bInsertAsChild)
		{
			NewObjectDlg dlg = new NewObjectDlg();
            dlg.Font = GuiUtil.GetDefaultFont();

            dlg.Type = ResTree.RESTYPE_FILE;
			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;

			string strPath = "";
			string strError = "";

			DatabaseObject obj = new DatabaseObject();
			obj.Name = dlg.textBox_objectName.Text;
			obj.Type = dlg.Type;
			obj.Changed = true;

			TreeNode node = new TreeNode(obj.Name, obj.Type, obj.Type);

			TreeNode nodeParent = null;

			if (bInsertAsChild == false)
			{
				// ����ͬ��


				// ����
				TreeNode nodeDup = TreeViewUtil.FindNodeByText((TreeView)this,
					this.SelectedNode != null ? this.SelectedNode.Parent : null,
                    dlg.textBox_objectName.Text);
				if (nodeDup != null)
				{
					strError = "ͬ�������Ѿ����ڡ�����������";
					goto ERROR1;
				}

                if (this.SelectedNode == null)
                {
                    strError = "��δѡ���׼����";
                    goto ERROR1;
                }

				nodeParent = this.SelectedNode.Parent;
				if (nodeParent == null)
					nodeParent = this.Nodes[0];

			}
			else
			{
				// �����¼�

				// ����
				TreeNode nodeDup = TreeViewUtil.FindNodeByText((TreeView)this,
					this.SelectedNode,
					dlg.textBox_objectName.Text);
				if (nodeDup != null)
				{
					strError = "ͬ�������Ѿ����ڡ�����������";
					goto ERROR1;
				}
			
				nodeParent = this.SelectedNode;
				if (nodeParent == null)
					nodeParent = this.Nodes[0];

			}

			nodeParent.Nodes.Add(node);

            strPath = TreeViewUtil.GetPath(nodeParent, '/');
			DatabaseObject objParent = this.Root.LocateObject(strPath);
			if (objParent == null)
			{
				strError = "·��Ϊ '" +strPath+ "' ���ڴ����û���ҵ�...";
				goto ERROR1;
			}

			obj.Parent = objParent;
			objParent.Children.Add(obj);

            strPath = TreeViewUtil.GetPath(node, '/');

			// ���޸ļ�¼����־
			ObjEvent objevent = new ObjEvent();
			objevent.Obj = obj;
			objevent.Oper = ObjEventOper.New;
			objevent.Path = strPath;
			this.Log.Add(objevent);

			/*
			int nRet = NewServerSideObject(strPath,
				dlg.Type,
				null,
				null,
				out strError);
			if (nRet == -1)
				goto ERROR1;

			// ˢ��?
			FillAll(null);
			*/

			return;
			ERROR1:
				MessageBox.Show(this, strError);
			return;
		}

		// �ڷ������˴�������
		// return:
		//		-1	����
		//		1	�Լ�����ͬ������
		//		0	��������
		int NewServerSideObject(string strPath,
			int nType,
			Stream stream,
			byte [] baTimeStamp,
            out byte [] baOutputTimestamp,
			out string strError)
		{
			strError = "";
            baOutputTimestamp = null;

            Debug.Assert(this.Channels != null, "");

			this.channel = Channels.GetChannel(this.ServerUrl);

			Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

			DigitalPlatform.Stop stop = null;

			if (stopManager != null) 
			{
				stop = new DigitalPlatform.Stop();

                stop.Register(this.stopManager, true);	// ����������

                stop.OnStop += new StopEventHandler(this.DoStop);
				stop.Initial("���ڴ����¶���: " + this.ServerUrl + "?" + strPath);

				stop.BeginLoop();

			}

			string strOutputPath = "";
			string strStyle = "";

			if (nType == ResTree.RESTYPE_FOLDER)
				strStyle = "createdir";
			/*
			long lRet = channel.DoSaveTextRes(strPath,
				"",	// content ��ʱΪ��
				true,	// bInlucdePreamble
				strStyle,	// style
				null,	// baTimeStamp,
				out baOutputTimestamp,
				out strOutputPath,
				out strError);
			*/

			string strRange = "";
			if (stream != null && stream.Length != 0) 
			{
				Debug.Assert(stream.Length != 0, "test");
				strRange = "0-" + Convert.ToString(stream.Length-1);
			}
			long lRet = channel.DoSaveResObject(strPath,
				stream,
				(stream != null && stream.Length != 0) ? stream.Length : 0,
				strStyle,
				"",	// strMetadata,
				strRange,
				true,
				baTimeStamp,	// timestamp,
				out baOutputTimestamp,
				out strOutputPath,
				out strError);

			if (stopManager != null) 
			{
				stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
				stop.Initial("");

				stop.Unregister();	// ����������
			}


			if (lRet == -1)
			{
				if (this.channel.ErrorCode == ChannelErrorCode.AlreadyExist) 
				{
					this.channel = null;
					return 1;	// �Ѿ�����ͬ��ͬ���Ͷ���
				}
				this.channel = null;
				strError = "д�� '" + strPath + "' ��������: " + strError;
				return -1;
			}

			this.channel = null;
			return 0;
		}

		// �༭�����ļ�
		void menu_editCfgFile(object sender, System.EventArgs e)
		{
			if (this.SelectedNode == null)
			{
				MessageBox.Show(this, "��δѡ��Ҫ�༭�������ļ��ڵ�");
				return;
			}
			
			if (this.SelectedNode.ImageIndex != ResTree.RESTYPE_FILE)
			{
				MessageBox.Show(this, "��ѡ��Ľڵ㲻�������ļ����͡���ѡ��Ҫ�༭�������ļ��ڵ㡣");
				return;
			}

			string strPath = this.GetPath(this.SelectedNode);

            if (DatabaseObject.IsDefaultFile(strPath) == true
                && EnableDefaultFileEditing == false)
            {
                MessageBox.Show(this, "���ݿ�ȱʡ�������ļ������ڴ˽����޸ġ�");
                return;
            }

			DatabaseObject obj = this.Root.LocateObject(strPath);
			if (obj == null)
			{
				MessageBox.Show(this, "·��Ϊ '" +strPath+ "' ���ڴ����û���ҵ�...");
				return;
			}

			// �༭�����ļ�
			CfgFileEditDlg dlg = new CfgFileEditDlg();
            dlg.Font = GuiUtil.GetDefaultFont();

			dlg.Initial(obj, strPath);
			/*
			dlg.Initial(this.Servers,
				this.Channels,
				this.stopManager,
				this.ServerUrl,
				strPath);
			*/

			if (this.applicationInfo != null)
				this.applicationInfo.LinkFormState(dlg, "CfgFileEditDlg_state");
			dlg.ShowDialog(this);
			if (this.applicationInfo != null)
				this.applicationInfo.UnlinkFormState(dlg);

			if (dlg.DialogResult == DialogResult.OK)
			{
				// ���޸ļ�¼����־
				ObjEvent objevent = new ObjEvent();
				objevent.Obj = obj;
				objevent.Oper = ObjEventOper.Change;
				objevent.Path = strPath;
				this.Log.Add(objevent);
			}
		}

		// ��ʾģʽ
		private void menu_displayMode_Click(object sender, System.EventArgs e)
		{
			/*
			string strMode = "";
			if (this.StorageMode == StorageMode.Real)
			{
				strMode = "��ʵ";
			}
			if (this.StorageMode == StorageMode.Memory)
			{
				strMode = "�ڴ����";
			}

			string strText = "��ǰ����洢ģʽΪ '" + strMode + "' , ���ݿ���Ϊ '" + this.DbName + "'��";
			MessageBox.Show(this, strText);
			*/

		}

		
		// ���Ƶ�ǰѡ��Ķ��󵽼�����
		private void menu_copyObjectToClipboard_Click(object sender, System.EventArgs e)
		{
			DatabaseObject root = null;
			string strError = "";
			// int nRet = 0;
			/*
			if (this.StorageMode == StorageMode.Real)
			{
				nRet = BuildMemoryTree(
					this.SelectedNode,
					out root,
					out strError);
				if (nRet == -1)
				{
					MessageBox.Show(this, strError);
					return;
				}
			}
			*/
			if (true)
			{
				string strPath = "";
				if (this.SelectedNode != null)
                    strPath = TreeViewUtil.GetPath(this.SelectedNode, '/');

				DatabaseObject parent = this.Root.LocateObject(strPath);
				if (parent == null)
				{
					strError = "·�� '" +strPath+ "'û���ҵ���Ӧ���ڴ���� ...";
					MessageBox.Show(this, strError);
					return ;
				}

				root = parent.Clone();

			}

			Clipboard.SetDataObject(root);

			if(Control.ModifierKeys == Keys.Control)
				MessageBox.Show(this, root.Dump());
		}

		// �Ӽ�����ճ��һ������(ǰ��)
		private void menu_pasteObjectFromClipboard_InsertBefore_Click(object sender, System.EventArgs e)
		{
			DoPasteObject(true);
		}

		// �Ӽ�����ճ��һ������(׷�ӵ��¼�ĩβ)
		private void menu_pasteObjectFromClipboard_AppendChild_Click(object sender, System.EventArgs e)
		{
			DoPasteObject(false);
		}

		// parameters:
		//		bInsert	�Ƿ�ǰ�� true:ǰ�� false:׷�ӵ��¼�ĩβ
		void DoPasteObject(bool bInsert)
		{
			IDataObject iData = Clipboard.GetDataObject();
			if (iData == null
				|| iData.GetDataPresent(typeof(DatabaseObject)) == false)
			{
				MessageBox.Show(this, "���������в�����DatabaseObject��������");
				return;
			}

			DatabaseObject root = (DatabaseObject)iData.GetData(typeof(DatabaseObject));

			if (root == null) 
			{
				MessageBox.Show(this, "GetData error");
				return;
			}

			/*
			public void InsertObject(TreeNode nodeParent,
				TreeNode nodeInsertPos,
				DatabaseObject obj)
			*/

			string strError = "";
			int nRet = 0;
			if (this.SelectedNode == null)
			{
				TreeNode nodeInsertPos = null;

				if (this.Nodes.Count != 0)
					nodeInsertPos = this.Nodes[0];

				// ���뵽��һ������ǰ��
				nRet = InsertObject(null,
					nodeInsertPos,
					root,
					out strError);
			
			}
			else 
			{
				if (bInsert == true)
				{
					// ���뵱ǰ�ڵ��ǰ��
					nRet = InsertObject(this.SelectedNode.Parent,
						this.SelectedNode,
						root,
						out strError);
				}
				else 
				{
					// ���뵱ǰ�ڵ���¼�ĩβ
					nRet = InsertObject(this.SelectedNode,
						null,
						root,
						out strError);
				}

			}

			if (nRet == -1)
				MessageBox.Show(this, strError);
		}

		// �Ӽ�����ճ��������
		private void menu_pasteTreeFromClipboard_Click(object sender, System.EventArgs e)
		{
			IDataObject iData = Clipboard.GetDataObject();
			if (iData == null
				|| iData.GetDataPresent(typeof(DatabaseObject)) == false)
			{
				MessageBox.Show(this, "���������в�����DatabaseObject��������");
				return;
			}

			DatabaseObject root = (DatabaseObject)iData.GetData(typeof(DatabaseObject));

			if (root == null) 
			{
				MessageBox.Show(this, "GetData error");
				return;
			}

			this.Nodes.Clear();

			// ����ǰ���ϵ�ȫ���ڵ����Ϊɾ������������־

			PutAllObjToLog(ObjEventOper.Delete,
				this.Root);

			//StorageMode oldmode = this.StorageMode;
			//this.StorageMode = StorageMode.Memory;

			//DatabaseObject oldroot = this.Root;

			this.Root = root;
			this.FillAll(null);

			//this.Root = oldroot;
			//this.StorageMode = oldmode;

			// ����������ȫ���ڵ���Ϊ����������־
			PutAllObjToLog(ObjEventOper.New,
				this.Root);
		}

        public void SetRootObject(DatabaseObject root)
        {
            this.Nodes.Clear();

            // ����ǰ���ϵ�ȫ���ڵ����Ϊɾ������������־

            PutAllObjToLog(ObjEventOper.Delete,
                this.Root);


            this.Root = root;
            this.FillAll(null);


            // ����������ȫ���ڵ���Ϊ����������־
            PutAllObjToLog(ObjEventOper.New,
                this.Root);
        }

		

		// �������帴�Ƶ�clipboard
		private void menu_copyTreeToClipboard_Click(object sender, System.EventArgs e)
		{
			/*
			DatabaseObject root = null;
			string strError = "";
			int nRet = BuildMemoryTree(
				(TreeNode)null,
				out root,
				out strError);
			if (nRet == -1)
			{
				MessageBox.Show(this, strError);
				return;
			}
			*/
			DatabaseObject root = this.Root.Clone();

			Clipboard.SetDataObject(root);
		}

		/*
		// �л�Ϊ�ڴ����ģʽ
		public int SwitchToMemoryMode(out string strError)
		{
			Debug.Assert(false, "��ֹ");
			strError = "";

			if (this.DisplayRoot == true
				&& this.Nodes.Count == 0)
			{
				strError = "������ʾ��������£���һ�������ж���ֻ��һ���ڵ����...";
				return -1;
			}

			DatabaseObject root = null;
			int nRet = BuildMemoryTree(
				this.DisplayRoot == false ?
				(TreeNode)null : this.Nodes[0],
				out root,
				out strError);
			if (nRet == -1)
				return -1;

			this.Nodes.Clear();

			// this.StorageMode = StorageMode.Memory;

			this.Root = root;
			this.FillAll(null);

			return 0;
		}
		*/
		

		// �¶���(������ͬ��ĩβ)
		private void menu_newObjectSibling_Click(object sender, System.EventArgs e)
		{
			DoNewObject(false);
		}


		// �¶���(�������¼�ĩβ)
		private void menu_newObjectChild_Click(object sender, System.EventArgs e)
		{
			DoNewObject(true);
		}
		
		// ɾ������
		private void menu_deleteObject_Click(object sender, System.EventArgs e)
		{
			if (this.SelectedNode == null)
			{
				MessageBox.Show("��δѡ��Ҫɾ���Ķ���...");
				return;
			}

			if (this.SelectedNode.ImageIndex == ResTree.RESTYPE_DB)
			{
				MessageBox.Show("���ﲻ��ɾ�����ݿ����...");
				return;
			}

			string strPath = this.GetPath(this.SelectedNode);
			DatabaseObject obj = this.Root.LocateObject(strPath);
			if (obj == null)
			{
				MessageBox.Show(this, "·��Ϊ '" +strPath+ "' ���ڴ����û���ҵ�...");
				return;
			}

			/*
			this.channel = Channels.GetChannel(this.ServerUrl);

			Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

			DigitalPlatform.GUI.Stop stop = null;

			if (stopManager != null) 
			{
				stop = new DigitalPlatform.GUI.Stop();
			
				stop.Register(this.stopManager);	// ����������

				stop.Initial(new Delegate_doStop(this.DoStop),
					"����ɾ������: " + this.ServerUrl + "?" + strPath);
				stop.BeginLoop();

			}

			byte [] baTimestamp = new byte [1];
			byte [] baOutputTimestamp = null;
			// string strOutputPath = "";
			string strError = "";


			REDO:
		// ɾ�����ݿ����

			long lRet = channel.DoDeleteRecord(strPath,
				baTimestamp,
				out baOutputTimestamp,
				out strError);
			if (lRet == -1)
			{
				// ʱ�����ƥ��
				if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
				{
					baTimestamp = baOutputTimestamp;
					goto REDO;
				}
			}
			

			if (stopManager != null) 
			{
				stop.EndLoop();
				stop.Initial(null, "");

				stop.Unregister();	// ����������
			}

			this.channel = null;

			if (lRet == -1)
			{
				MessageBox.Show(this, strError);
				return;
			}
			*/

			// ���޸ļ�¼����־
			ObjEvent objevent = new ObjEvent();
			objevent.Obj = obj;
			objevent.Oper = ObjEventOper.Delete;
			objevent.Path = strPath;
			this.Log.Add(objevent);

			// ˢ��?
			// FillAll(null);
			this.SelectedNode.Remove();

			if (OnObjectDeleted != null)
			{
				OnObjectDeletedEventArgs newargs = new OnObjectDeletedEventArgs();
				newargs.ObjectPath = strPath;
				OnObjectDeleted(this, newargs);
			}	
	
		}


		/*
		// �ѵ�ǰRealģʽ�Ķ���ȫ������Ϊ�ڴ���
		public int BuildMemoryTree(
			TreeNode treenode,
			out DatabaseObject root,
			out string strError)
		{
			Debug.Assert(false, "��ֹ");
			root = null;
			strError = "";

			
			//if (this.StorageMode != StorageMode.Real)
			//{
			//	strError = "������Real�洢ģʽ�µ��ú���BuildMemoryTree()";
			//	return -1;
			//}
			

			root = new DatabaseObject();

			return BuildMemoryTree(
				treenode,
				root,
                out strError);
		}
		*/

		int BuildMemoryTree(
			TreeNode parentTreeNode,
			DatabaseObject parentDatabaseObject,
			out string strError)
		{
			strError = "";

			TreeNodeCollection children = null;

			if (parentTreeNode == null) 
			{
				children = this.Nodes;
			}
			else 
			{
				children = parentTreeNode.Nodes;
			}

			//DatabaseObject newObj = null;

			if (parentTreeNode != null)	// ʵ��
			{
				TreeNode treenode = parentTreeNode;

				parentDatabaseObject.Type = treenode.ImageIndex;
				parentDatabaseObject.Name = treenode.Text;

				if (treenode.ImageIndex == ResTree.RESTYPE_DB)
				{

					//newObj = parentDatabaseObject;
				}
				else if (treenode.ImageIndex == ResTree.RESTYPE_FOLDER)
				{

					/*
					newObj = DatabaseObject.BuildDirObject(treenode.Text);
					newObj.Type = treenode.ImageIndex;
					newObj.Parent = parentDatabaseObject;
					parentDatabaseObject.Children.Add(newObj);
					*/
					//newObj = parentDatabaseObject;
				}
				else if (treenode.ImageIndex == ResTree.RESTYPE_FILE)
				{
					this.channel = Channels.GetChannel(this.ServerUrl);

					Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

					string strPath = "";
					byte[] baTimeStamp = null;
					string strMetaData;
					string strOutputPath;

					strPath = this.GetPath(treenode);

                    // string strStyle = "attachment,data,timestamp,outputpath";
					string strStyle = "content,data,timestamp,outputpath";
					MemoryStream stream = new MemoryStream();

					long lRet = channel.GetRes(strPath,
						stream,
						null,	// stop,
						strStyle,
						null,	// byte [] input_timestamp,
						out strMetaData,
						out baTimeStamp,
						out strOutputPath,
						out strError);
					if (lRet == -1)
						return -1;

					parentDatabaseObject.SetData(stream);
					parentDatabaseObject.TimeStamp = baTimeStamp;
					stream.Close();
					stream = null;

					return 0;
				}
				else 
				{
					Debug.Assert(false, "����Ľڵ�����");
				}

			}

			for(int i=0;i<children.Count;i++)
			{
				TreeNode treenode = children[i];

				DatabaseObject child = new DatabaseObject();
				child.Parent = parentDatabaseObject;
				parentDatabaseObject.Children.Add(child);

				int nRet = BuildMemoryTree(
                    treenode,
					child,
					out strError);
				if (nRet == -1)
					return -1;

				
			}

			return 0;
		}


		/*
		// ���ڴ���󴴽�Ϊ�����ķ������˶���
		public int BuildRealObjects(
			string strDbName,
			DatabaseObject root,
			out string strError)
		{
			strError = "";

			int nRet = 0;

			// ������
			if (root.Type != -1)
			{
				if (root.Type == ResTree.RESTYPE_DB)
					goto DOCHILD;	// ���Ա��ڵ㣬���Ǽ������¼��ڵ�


				// ȱʡ�����ļ������Ա���
				if (root.IsDefaultFile() == true)
					return 0;

				MemoryStream stream = null;
					
				if (root.Type == ResTree.RESTYPE_FILE)
					stream = new MemoryStream(root.Content);

				string strPath = root.MakePath(strDbName);
// �ڷ������˴�������
				nRet = NewServerSideObject(strPath,
					root.Type,
					stream,
					root.TimeStamp,
                    out strError);
				if (nRet == -1)
					return -1;
			}

			DOCHILD:
			// �ݹ�
			for(int i=0;i<root.Children.Count;i++)
			{
				DatabaseObject obj = (DatabaseObject)root.Children[i];

				nRet = BuildRealObjects(
                    strDbName,
					obj,
                    out strError);
				if (nRet == -1)
					return -1;
			}


			return 0;
		}
		*/


		// ����·�������ڴ����
		public int CreateObject(DatabaseObject obj,
			string strPath,
			out string strError)
		{
			strError = "";
			obj.Children.Clear();


			if (obj.Type == ResTree.RESTYPE_FILE)
			{
				byte[] baTimeStamp = null;
				string strMetaData;
				string strOutputPath;


				this.channel = Channels.GetChannel(this.ServerUrl);

				Debug.Assert(channel != null, "Channels.GetChannel() �쳣");


                // string strStyle = "attachment,data,timestamp,outputpath";
                string strStyle = "content,data,timestamp,outputpath";
				MemoryStream stream = new MemoryStream();

				long lRet = channel.GetRes(strPath,
					stream,
					null,	// stop,
					strStyle,
					null,	// byte [] input_timestamp,
					out strMetaData,
					out baTimeStamp,
					out strOutputPath,
					out strError);
				if (lRet == -1) 
				{
					// obj.SetData(null);
					obj.TimeStamp = null;
					stream.Close();
					stream = null;

					return 0;	// ��������

					//return -1;
				}

				obj.SetData(stream);
				obj.TimeStamp = baTimeStamp;
				stream.Close();
				stream = null;
			}

			if (obj.Type == ResTree.RESTYPE_DB 
				|| obj.Type == ResTree.RESTYPE_FOLDER)
			{

				this.channel = Channels.GetChannel(this.ServerUrl);

				Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

				ResInfoItem [] items = null;

				DigitalPlatform.Stop stop = null;

				if (stopManager != null) 
				{
					stop = new DigitalPlatform.Stop();
                    stop.Register(this.stopManager, true);	// ����������

                    stop.OnStop += new StopEventHandler(this.DoStop);
					stop.Initial("������Ŀ¼: " + this.ServerUrl + "?" + strPath);

					stop.BeginLoop();
				}

				long lRet = channel.DoDir(strPath,
					this.Lang,
                    null,   // ����Ҫ����ȫ�����Ե�����
					out items,
					out strError);

				if (stopManager != null) 
				{
					stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
					stop.Initial("");

					stop.Unregister();	// ����������
				}

				this.channel = null;

				if (lRet == -1) 
					return -1;



				if (items == null)
					return 0;

				for(int i=0;i<items.Length;i++) 
				{
					// ����from���ͽڵ�
					if (items[i].Type == ResTree.RESTYPE_FROM)
						continue;

					DatabaseObject child = new DatabaseObject();
					child.Name = items[i].Name;
					child.Type = items[i].Type;
                    child.Style = items[i].Style;

					child.Parent = obj;
					obj.Children.Add(child);

					int nRet = CreateObject(child,
						strPath + "/" + items[i].Name,
						out strError);
					if (nRet == -1)
						return -1;

				}
			}




			return 0;
		}

        int GetDbStyle(string strDbName,
            out string strError)
        {
            strError = "";

            this.channel = Channels.GetChannel(this.ServerUrl);

            Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

            ResInfoItem[] items = null;

            DigitalPlatform.Stop stop = null;

            if (stopManager != null)
            {
                stop = new DigitalPlatform.Stop();
                stop.Register(this.stopManager, true);	// ����������

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڻ�����ݿ� " + strDbName + "�ķ�����");

                stop.BeginLoop();
            }

            long lRet = channel.DoDir("",   // �г�ȫ�����ݿ�
                this.Lang,
                null,   // ����Ҫ�г�ȫ�����Ե�����
                out items,
                out strError);

            if (stopManager != null)
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                stop.Unregister();	// ����������
            }

            this.channel = null;

            if (lRet == -1)
                return -1;

            if (items == null)
                return 0;   // ���ݿⲻ����

            for (int i = 0; i < items.Length; i++)
            {
                // ���Է����ݿ����ͽڵ�
                if (items[i].Type != ResTree.RESTYPE_DB)
                    continue;

                if (items[i].Name == strDbName)
                    return items[i].Style;
            }

            return 0;   // ���ݿⲻ����
        }

		// ������ȫ���������ض����ͼ�����־��
		public void PutAllObjToLog(ObjEventOper oper,
			DatabaseObject root)
		{
			ObjEvent objevent = new ObjEvent();

			objevent.Obj = root;
			objevent.Oper = oper;
			objevent.Path = root.MakePath(this.DbName);

			this.Log.Add(objevent);

			if (oper == ObjEventOper.Delete
				&& root.Type == ResTree.RESTYPE_FOLDER)
				return;	// ɾ��Ŀ¼����, ֻҪ��������������־, ���¼��������ٽ���

			// �ݹ�
			for(int i=0;i<root.Children.Count;i++)
			{
				PutAllObjToLog(oper,
					(DatabaseObject)root.Children[i]);
			}
		}

        // 2012/4/18
        // ˢ�¶������ƶ�λ���Ժ�ġ����ĳ�������ȫ��������ʱ���
        void RefreshTimestamp(
            int nStartIndex,
            string strPath,
            byte [] baTimestamp)
        {
            ObjEventCollection log = this.Log;
            for (int i = nStartIndex; i < log.Count; i++)
            {
                ObjEvent objevent = (ObjEvent)log[i];

                if (objevent.Obj.Type == -1)    // �������Ķ���
                    continue;

                if (objevent.Obj.Type == ResTree.RESTYPE_DB)
                    continue;

                // ȱʡ�����ļ������Բ���
                if (objevent.Oper == ObjEventOper.New
                    || objevent.Oper == ObjEventOper.Change)
                {
                    if (DatabaseObject.IsDefaultFile(objevent.Path) == true)
                        continue;
                }

                if (objevent.Path != strPath)
                    continue;

                if (objevent.Obj != null)
                    objevent.Obj.TimeStamp = baTimestamp;
            }

        }

		public int SubmitLog(out string strErrorText)
		{
			strErrorText = "";
			int nRet;

			ObjEventCollection log = this.Log;
			string strError = "";

			for(int i=0;i<log.Count;i++)
			{
				ObjEvent objevent = (ObjEvent)log[i];

                if (objevent.Obj.Type == -1)    // �������Ķ���
                    continue;

				if (objevent.Obj.Type == ResTree.RESTYPE_DB)
					continue;

				// ȱʡ�����ļ������Բ���
				if (objevent.Oper == ObjEventOper.New
					|| objevent.Oper == ObjEventOper.Change)
				{
					if (DatabaseObject.IsDefaultFile(objevent.Path) == true)
						continue;
				}

				if (objevent.Oper == ObjEventOper.New)
				{
					MemoryStream stream = null;
					
					if (objevent.Obj.Type == ResTree.RESTYPE_FILE
						&& objevent.Obj.Content != null)
						stream = new MemoryStream(objevent.Obj.Content);

					string strPath = objevent.Path;
                    byte[] baOutputTimestamp = null;
					nRet = NewServerSideObject(strPath,
						objevent.Obj.Type,
						stream,
						objevent.Obj.TimeStamp,
                        out baOutputTimestamp,
						out strError);
                    if (nRet == -1)
                    {
                        strError = "�½����� '" + strPath + "' ʱ��������: " + strError;
                        MessageBox.Show(this, strError);
                        strErrorText += strError + "\r\n";
                        // return -1;
                    }
                    else
                    {
                        // ��������ɹ�����Ҫ�Ѷ����к�������м���������ͬ����Ķ����޸�ʱ���
        // ˢ�¶������ƶ�λ���Ժ�ġ����ĳ�������ȫ��������ʱ���
                        RefreshTimestamp(
                            i + 1,
                            strPath,
                            baOutputTimestamp);
                    }
				}
				if (objevent.Oper == ObjEventOper.Change)
				{
					MemoryStream stream = null;
					
					if (objevent.Obj.Type == ResTree.RESTYPE_FILE)
						stream = new MemoryStream(objevent.Obj.Content);

					string strPath = objevent.Path;
                    byte[] baOutputTimestamp = null;
                    nRet = NewServerSideObject(strPath,
						objevent.Obj.Type,
						stream,
						objevent.Obj.TimeStamp,
                        out baOutputTimestamp,
						out strError);
					if (nRet == -1)
					{
						strError = "�޸Ķ��� '" +strPath + "' ʱ��������: " + strError;
						MessageBox.Show(this, strError);
						strErrorText += strError + "\r\n";
					}
                    else
                    {
                        // ��������ɹ�����Ҫ�Ѷ����к�������м���������ͬ����Ķ����޸�ʱ���
                        // ˢ�¶������ƶ�λ���Ժ�ġ����ĳ�������ȫ��������ʱ���
                        RefreshTimestamp(
                            i + 1,
                            strPath,
                            baOutputTimestamp);
                    }

				}
				else if (objevent.Oper == ObjEventOper.Delete)
				{
                    // TODO: �����Ѿ���ʱ����ˣ����Բ�������

					this.channel = Channels.GetChannel(this.ServerUrl);

					Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

					byte [] baTimestamp = new byte [1];
					byte [] baOutputTimestamp = null;
					string strPath = objevent.Path;
					// string strOutputPath = "";
				REDO:
					// ɾ�����ݿ����
					long lRet = channel.DoDeleteRes(strPath,
						baTimestamp,
                        "",
						out baOutputTimestamp,
						out strError);
					if (lRet == -1)
					{
						// ʱ�����ƥ��
						if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
						{
							baTimestamp = baOutputTimestamp;
							goto REDO;
						}
						strError = "ɾ������ '" +strPath + "' ʱ��������: " + strError;
						MessageBox.Show(this, strError);
						strErrorText += strError + "\r\n";
					}
				}

			}

			log.Clear();

			if (strErrorText == "")
				return 0;
			return -1;
		}

		public void ClearLog()
		{
			this.Log.Clear();
		}

	}


	// ����ɾ��
	public delegate void OnObjectDeletedEventHandle(object sender,
	OnObjectDeletedEventArgs e);

	public class OnObjectDeletedEventArgs: EventArgs
	{
		public string ObjectPath = "";
	}

}
