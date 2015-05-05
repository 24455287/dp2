using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using DigitalPlatform.GUI;
using DigitalPlatform.rms.Client.rmsws_localhost;
using DigitalPlatform.Text;
using DigitalPlatform.Xml;


namespace DigitalPlatform.rms.Client
{
	/// <summary>
	/// Summary description for ResRightTree.
	/// </summary>
	public class ResRightTree : System.Windows.Forms.TreeView
	{
		public ServerCollection Servers = null;	// ����
		public RmsChannelCollection Channels = null;

		bool m_bChanged = false;

		public DigitalPlatform.StopManager stopManager = null;

		RmsChannel channel = null;

		public string ServerUrl = "";

		public string Lang = "zh";

		public XmlDocument UserRightsDom = null;	// �û��ʻ���¼

		public string PropertyCfgFileName = "";

		TreeNode m_oldHoverNode = null;

		public int[] EnabledIndices = null;

        public event GuiAppendMenuEventHandle OnSetMenu;

        public event NodeRightsChangedEventHandle OnNodeRightsChanged;

		private System.Windows.Forms.ImageList imageList_resIcon;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.ComponentModel.IContainer components;

		public ResRightTree()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ResRightTree));
			this.imageList_resIcon = new System.Windows.Forms.ImageList(this.components);
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			// 
			// imageList_resIcon
			// 
			this.imageList_resIcon.ImageSize = new System.Drawing.Size(16, 16);
			this.imageList_resIcon.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList_resIcon.ImageStream")));
			this.imageList_resIcon.TransparentColor = System.Drawing.Color.Fuchsia;
			// 
			// ResRightTree
			// 
			this.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.ResRightTree_AfterExpand);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ResRightTree_MouseUp);
			this.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ResRightTree_AfterSelect);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ResRightTree_MouseMove);

		}
		#endregion

		public bool Changed 
		{
			get 
			{
				return m_bChanged;
			}
			set 
			{
				m_bChanged = value;
			}
		}

        // ��ʼ��
        // parameters:
        //      userRightsDom   �û���¼��dom���󡣽�ֱ�������������
		public void Initial(ServerCollection servers,
			RmsChannelCollection channels,
			DigitalPlatform.StopManager stopManager,
			string serverUrl,
			XmlDocument UserRightsDom)
		{
			this.Servers = servers;
			this.Channels = channels;
			this.stopManager = stopManager;
			this.ServerUrl = serverUrl;

			this.UserRightsDom = UserRightsDom; // ֱ����������dom����

			// �÷������˻�õ���Ϣ�����
			Cursor save = this.Cursor;
			this.Cursor = Cursors.WaitCursor;
			FillAll(null);
			InitialRightsParam();
			this.Cursor = save;

			this.m_bChanged = false;
		}

		string GetDefElementString(int nType)
		{
			if (nType == ResTree.RESTYPE_DB)
				return "database";
			if (nType == ResTree.RESTYPE_FILE)
				return "file";
			if (nType == ResTree.RESTYPE_FOLDER)
				return "dir";
			if (nType == ResTree.RESTYPE_FROM)
				return "from";
			if (nType == ResTree.RESTYPE_SERVER)
				return "server";

			return "object";
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

				TreeNode nodeNew = new TreeNode(this.ServerUrl, ResTree.RESTYPE_SERVER, ResTree.RESTYPE_SERVER);
				ResTree.SetLoading(nodeNew);

				NodeInfo nodeinfo = new NodeInfo();
				nodeinfo.TreeNode = nodeNew;
				nodeinfo.Expandable = true;
				nodeinfo.DefElement = GetDefElementString(nodeNew.ImageIndex);
				nodeinfo.NodeState |= NodeState.Object;

				nodeNew.Tag = nodeinfo;


				if (EnabledIndices != null
					&& StringUtil.IsInList(nodeNew.ImageIndex, EnabledIndices) == false)
					nodeNew.ForeColor = ControlPaint.LightLight(nodeNew.ForeColor);

				children.Add(nodeNew);
				return 0;
			}


			// �����µĽڵ�����
			ResPath respath = new ResPath(node);

			string strPath = respath.Path;

			//if (node != null)
			//	strPath = TreeViewUtil.GetPath(node);

			this.channel = Channels.GetChannel(this.ServerUrl);

			Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

			ResInfoItem [] items = null;

			string strError = "";

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
			{
				try 
				{
					MessageBox.Show(this, "Channel::DoDir() Error: " + strError);
				}
				catch
				{
					// this�����Ѿ�������
					return -1;
				}

				if (node != null) 
				{
					ResTree.SetLoading(node);	// ������ƺ������³���+��
					node.Collapse();
				}
				return -1;
			}


			if (items != null) 
			{
				children.Clear();

				for(i=0;i<items.Length;i++) 
				{
					// ����from���ͽڵ�
					if (items[i].Type == ResTree.RESTYPE_FROM)
						continue;

					TreeNode nodeNew = new TreeNode(items[i].Name, items[i].Type, items[i].Type);


					NodeInfo nodeinfo = new NodeInfo();
					nodeinfo.TreeNode = nodeNew;
					nodeinfo.Expandable = items[i].HasChildren;
					nodeinfo.DefElement = GetDefElementString(nodeNew.ImageIndex);
					nodeinfo.NodeState |= NodeState.Object;
                    nodeinfo.Style = items[i].Style;
					nodeNew.Tag = nodeinfo;

					if (items[i].HasChildren)
						ResTree.SetLoading(nodeNew);

					if (EnabledIndices != null
						&& StringUtil.IsInList(nodeNew.ImageIndex, EnabledIndices) == false)
						nodeNew.ForeColor = ControlPaint.LightLight(nodeNew.ForeColor);

					children.Add(nodeNew);
				}
			}

			return 0;
		}

		/*
		// ��һ���ڵ��¼�����"loading..."���Ա����+��
		static void SetLoading(TreeNode node)
		{
			// ��node
			TreeNode nodeNew = new TreeNode("loading...", ResTree.RESTYPE_LOADING, ResTree.RESTYPE_LOADING);

			node.Nodes.Clear();
			node.Nodes.Add(nodeNew);
		}

		// �¼��Ƿ����loading...?
		static bool IsLoading(TreeNode node)
		{
			if (node.Nodes.Count == 0)
				return false;

			if (node.Nodes[0].Text == "loading...")
				return true;

			return false;
		}
		*/

		// �ص�����
		void DoStop(object sender, StopEventArgs e)
		{
			if (this.channel != null)
				this.channel.Abort();
		}

		private void ResRightTree_AfterExpand(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			TreeNode node = e.Node;

			if (node == null)
				return;

			// ��Ҫչ��
			if (ResTree.IsLoading(node) == true) 
			{
				Fill(node);
			}		
		}

		private void ResRightTree_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			TreeNode node = e.Node;

			if (node == null)
				return;

			// ��Ҫչ��
			if (ResTree.IsLoading(node) == true) 
			{
				Fill(node);
			}		
		
		}

		void FillAll(TreeNode node)
		{
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

		// �����ʻ���¼�е���Ϣ, ���ȫ�������Ȩ����Ϣ
		int InitialRightsParam()
		{
			// �õ�<server>�ڵ�

			XmlNode nodeRoot = this.UserRightsDom.SelectSingleNode("//server");   // rightsItem

			if (nodeRoot == null)
				return 0;	// �����ڵ�û���ҵ�

			// ������Ȼ�ṹ���г�ʼ��
			InitialRights(nodeRoot,
				this.Nodes[0]);


			return 0;
		}


		// ��ʼ��: ��xml�е���Ϣ����treeview
		int InitialRights(XmlNode parentXmlNode,
			TreeNode parentTreeNode)
		{
            // �������ڵ�����
            if (parentTreeNode.ImageIndex == ResTree.RESTYPE_SERVER)
            {
                string strName = DomUtil.GetAttr(parentXmlNode, "name");

                NodeInfo nodeinfo = null;
                nodeinfo = (NodeInfo)parentTreeNode.Tag;
                if (nodeinfo == null)
                {
                    nodeinfo = new NodeInfo();
                    nodeinfo.TreeNode = parentTreeNode;
                    parentTreeNode.Tag = nodeinfo;
                }
                nodeinfo.NodeState |= NodeState.Account | NodeState.Object;

                nodeinfo.DefElement = parentXmlNode.Name;
                nodeinfo.DefName = strName;
                nodeinfo.Rights = DomUtil.GetAttrDiff(parentXmlNode, "rights");

            }



			for(int i=0;i<parentXmlNode.ChildNodes.Count;i++)
			{
				XmlNode childXmlNode = parentXmlNode.ChildNodes[i];
				if (childXmlNode.NodeType != XmlNodeType.Element)
					continue;

				string strName = DomUtil.GetAttr(childXmlNode, "name");

				int nType = 0;
				bool bExpandable = false;
				// ���ݿ�
				if (childXmlNode.Name == "database")
				{
					nType = ResTree.RESTYPE_DB;
					bExpandable = true;
				}
				// Ŀ¼
				if (childXmlNode.Name == "dir")
				{
					nType = ResTree.RESTYPE_FOLDER;
					bExpandable = true;
				}
				// �ļ�
				if (childXmlNode.Name == "file")
				{
					nType = ResTree.RESTYPE_FILE;
					bExpandable = true;
				}

				TreeNode childTreeNode = FindTreeNode(parentTreeNode, strName);

				NodeInfo nodeinfo = null;

				// û���ҵ�
				if (childTreeNode == null)
				{
					// �´���һ��,���Ǳ�עΪδʹ�õ�
					childTreeNode = new TreeNode(strName, nType, nType);

					nodeinfo = new NodeInfo();
					nodeinfo.TreeNode = childTreeNode;
					nodeinfo.Expandable = bExpandable;
					nodeinfo.NodeState |= NodeState.Account;
					childTreeNode.Tag = nodeinfo;

					childTreeNode.ForeColor = ControlPaint.LightLight(childTreeNode.ForeColor);	// ��ɫ

					parentTreeNode.Nodes.Add(childTreeNode);
				}
				else // �ҵ�
				{
					nodeinfo = (NodeInfo)childTreeNode.Tag;
					if (nodeinfo == null)
					{
						nodeinfo = new NodeInfo();
						nodeinfo.TreeNode = childTreeNode;
						childTreeNode.Tag = nodeinfo;
					}
					nodeinfo.NodeState |= NodeState.Account | NodeState.Object;
				}


				nodeinfo.DefElement = childXmlNode.Name;
				nodeinfo.DefName = strName;
				nodeinfo.Rights = DomUtil.GetAttrDiff(childXmlNode, "rights");

                if (nodeinfo.Rights == "" || nodeinfo.Rights == null)
					childTreeNode.ForeColor = SystemColors.GrayText;	// ControlPaint.LightLight(nodeNew.ForeColor);
				else
					childTreeNode.ForeColor = SystemColors.WindowText;


				// �ݹ�
				InitialRights(childXmlNode,
					childTreeNode);

			}

			return 0;
		}

		TreeNode FindTreeNode(TreeNode parent,
			string strName)
		{
			for(int i=0;i<parent.Nodes.Count;i++)
			{
				TreeNode node = parent.Nodes[i];
				if (node.Text == strName)
					return node;
			}

			return null;
		}

        // �ҵ����ӽڵ��а���ָ��name����ֵ��
        XmlNode FindXmlNode(XmlNode parent,
			string strName)
		{
			for(int i=0;i<parent.ChildNodes.Count;i++)
			{
				XmlNode node = parent.ChildNodes[i];
				if (node.NodeType != XmlNodeType.Element)
					continue;
				if (DomUtil.GetAttr(node, "name") == strName)
					return node;
			}

			return null;
		}


        // ��treeview�е���Ϣ�ռ�������xml��
        public int FinishRightsParam()
		{
			// �õ�<server>�ڵ�

			XmlNode nodeRoot = this.UserRightsDom.SelectSingleNode("//server");   // rightsItem

            if (nodeRoot == null)
            {
                // �����ڵ�û���ҵ�
                DomUtil.SetElementText(this.UserRightsDom.DocumentElement, "server", "");   // rightsItem
                nodeRoot = this.UserRightsDom.SelectSingleNode("//server"); // rightsItems
                Debug.Assert(nodeRoot != null, "������Ϊ���Ҳ�����?");

            }

			// ������Ȼ�ṹ���г�ʼ��
			FinishRights(nodeRoot,
				this.Nodes[0]);


			return 0;
		}

		// ����: ��treeview�е���Ϣ�ռ�������xml��
		int FinishRights(XmlNode parentXmlNode,
			TreeNode parentTreeNode)
		{
			ArrayList aFound = new ArrayList();

            // ��Ҫ���⴦��
            if (parentTreeNode.ImageIndex == ResTree.RESTYPE_SERVER)
            {
                NodeInfo nodeinfo = (NodeInfo)parentTreeNode.Tag;

                // ����Ȩ������
                DomUtil.SetAttr(parentXmlNode, "rights", nodeinfo.Rights);
            }

			for(int i=0;i<parentTreeNode.Nodes.Count;i++)
			{
				TreeNode childTreeNode = parentTreeNode.Nodes[i];

				string strName = childTreeNode.Text;

				NodeInfo nodeinfo = (NodeInfo)childTreeNode.Tag;

                // �ҵ����ӽڵ��а���ָ��name����ֵ��
				XmlNode childXmlNode = FindXmlNode(parentXmlNode,
					strName);

				if (childXmlNode == null)
				{

					Debug.Assert(nodeinfo.DefElement != "", "nodeinfo��DefElement��δ����");

					childXmlNode = parentXmlNode.OwnerDocument.CreateElement(nodeinfo.DefElement);
					childXmlNode = parentXmlNode.AppendChild(childXmlNode);
					DomUtil.SetAttr(childXmlNode, "name", strName);
				}
				else 
				{
					// �ҵ�
				}

				aFound.Add(childXmlNode);


				// ����Ȩ������
				DomUtil.SetAttr(childXmlNode, "rights", nodeinfo.Rights);

				// �ݹ�
				FinishRights(childXmlNode,
					childTreeNode);

			}

			// ��treenode�������,Ҫɾ��
			for(int i=0;i<parentXmlNode.ChildNodes.Count;i++)
			{

				XmlNode node = parentXmlNode.ChildNodes[i];
				if (node.NodeType != XmlNodeType.Element)
					continue;

				if (aFound.IndexOf(node) == -1)
				{
					node.ParentNode.RemoveChild(node);
					i --;
				}
			}

			return 0;
		}

		private void ResRightTree_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if(e.Button != MouseButtons.Right)
				return;

			ContextMenu contextMenu = new ContextMenu();
			MenuItem menuItem = null;

			menuItem = new MenuItem("Ȩ��(&R)");
			menuItem.Click += new System.EventHandler(this.menu_editRights_Click);
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("ɾ��(&D)");
			menuItem.Click += new System.EventHandler(this.menu_deleteNode_Click);
            if (this.SelectedNode != null && this.SelectedNode.ImageIndex == ResTree.RESTYPE_SERVER)
                menuItem.Enabled = false;
			contextMenu.MenuItems.Add(menuItem);

            if (OnSetMenu != null)
            {
                GuiAppendMenuEventArgs newargs = new GuiAppendMenuEventArgs();
                newargs.ContextMenu = contextMenu;
                OnSetMenu(this, newargs);
                if (newargs.ContextMenu != contextMenu)
                    contextMenu = newargs.ContextMenu;
            }

            if (contextMenu != null)
                contextMenu.Show(this, new Point(e.X, e.Y));

		}

        // �༭Ȩ��
        // return:
        //      false   û�з����޸�
        //      true    �������޸�
        public DialogResult NodeRightsDlg(TreeNode node,
            out string strRights)
        {
            strRights = "";

            DigitalPlatform.CommonDialog.CategoryPropertyDlg dlg = new DigitalPlatform.CommonDialog.CategoryPropertyDlg();
            dlg.Font = GuiUtil.GetDefaultFont();

            if (node == null)
                node = this.SelectedNode;

            /*
			NodeInfo nodeinfo = (NodeInfo)this.SelectedNode.Tag;

			dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.Text = "���� '"+ this.SelectedNode.Text +"' ��Ȩ��";
			dlg.PropertyString = nodeinfo.Rights;
			dlg.CfgFileName = this.PropertyCfgFileName;
			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;


			nodeinfo.Rights = dlg.PropertyString;

			if (nodeinfo.Rights == "")
				this.SelectedNode.ForeColor = SystemColors.GrayText;	// ControlPaint.LightLight(nodeNew.ForeColor);
			else
				this.SelectedNode.ForeColor = SystemColors.WindowText;

			this.m_bChanged = true;
             */

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.Text = "���� '" + node.Text + "' ��Ȩ��";
            dlg.PropertyString = GetNodeRights(node);
            dlg.CfgFileName = this.PropertyCfgFileName;
            dlg.ShowDialog(this);

            strRights = dlg.PropertyString;

            return dlg.DialogResult;
        }

		// �༭Ȩ��
		private void menu_editRights_Click(object sender, System.EventArgs e)
		{
            if (this.SelectedNode == null)
            {
                MessageBox.Show("��δѡ��Ҫ�༭������...");
                return;
            }

            string strRights = "";

            if (NodeRightsDlg(this.SelectedNode,
                out strRights) != DialogResult.OK)
                return;

            SetNodeRights(this.SelectedNode, strRights);
		}

        // ���һ���ڵ���������Ȩ���ַ���
        public static string GetNodeRights(TreeNode node)
        {
            NodeInfo nodeinfo = (NodeInfo)node.Tag;

            if (nodeinfo == null)
                return null;

            return nodeinfo.Rights;
        }

        public static int GetNodeStyle(TreeNode node)
        {
            NodeInfo nodeinfo = (NodeInfo)node.Tag;

            if (nodeinfo == null)
                return 0;

            return nodeinfo.Style;
        }

        // ���һ���ڵ��Ƿ����չ����״̬
        public static bool GetNodeExpandable(TreeNode node)
        {
            NodeInfo nodeinfo = (NodeInfo)node.Tag;

            if (nodeinfo == null)
                return false;

            return nodeinfo.Expandable;
        }

        // ����һ���ڵ���������Ȩ���ַ���
        public void SetNodeRights(TreeNode node,
            string strRights)
        {
            NodeInfo nodeinfo = (NodeInfo)node.Tag;

            if (nodeinfo.Rights == strRights)
                return;

            nodeinfo.Rights = strRights;

            if (nodeinfo.Rights == "" || nodeinfo.Rights == null)
                node.ForeColor = SystemColors.GrayText;	// ControlPaint.LightLight(nodeNew.ForeColor);
            else
                node.ForeColor = SystemColors.WindowText;

            this.m_bChanged = true;

            if (OnNodeRightsChanged != null)
            {
                NodeRightsChangedEventArgs e = new NodeRightsChangedEventArgs();
                e.Node = node;
                e.Rights = strRights;
                OnNodeRightsChanged(this, e);
            }
        }

		// ɾ���ڵ�
		private void menu_deleteNode_Click(object sender, System.EventArgs e)
		{
			if (this.SelectedNode == null)
			{
				MessageBox.Show("��δѡ��Ҫɾ���Ľڵ�...");
				return;
			}

            if (this.SelectedNode.ImageIndex == ResTree.RESTYPE_SERVER)
            {
                MessageBox.Show("����ɾ���������ڵ�...");
                return;
            }
                


			DialogResult result = MessageBox.Show(this,
				"ȷʵҪɾ���ڵ� " +this.SelectedNode.Text + "?",
				"ResRightTree",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question, 
				MessageBoxDefaultButton.Button2);
			if (result != DialogResult.Yes)
				return;

			this.SelectedNode.Remove();

			this.m_bChanged = true;
		}

		private void ResRightTree_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			string strText = "";

			// Point p = this.PointToClient(new Point(e.X, e.Y));
			//TreeNode selection = this.GetNodeAt(p);
			TreeNode selection = this.GetNodeAt(e.X, e.Y);

			if (m_oldHoverNode == selection)
				return;


			if (selection != null) 
			{
				selection.BackColor = SystemColors.Info;
				NodeInfo nodeinfo = (NodeInfo)selection.Tag;
				if (nodeinfo != null) 
				{
					string strState = "";
					strState = NodeInfo.GetNodeStateString(nodeinfo);

                    if (nodeinfo.Rights == null)
                        strText = "���� '" + selection.Text + "' Ȩ�� -- (δ����);  ״̬--" + strState;
					else if (nodeinfo.Rights == "")
						strText = "���� '" + selection.Text + "' Ȩ�� -- (��);  ״̬--" + strState;
					else
						strText = "���� '" + selection.Text + "' Ȩ�� -- " + nodeinfo.Rights + ";  ״̬ -- "+ strState;
				}

			}
			toolTip1.SetToolTip(this, strText);

			if (m_oldHoverNode != selection)
			{
				if (m_oldHoverNode != null)
					m_oldHoverNode.BackColor = SystemColors.Window;

				m_oldHoverNode = selection;
			}
		}

	}

	public enum NodeState
	{
		None = 0,
		Object = 0x01,	// ����ʵ�ʶ���
		Account = 0x02,	// �����ʻ���¼����

	}

	// �ڵ���ϸ��Ϣ
	public class NodeInfo
	{
		public bool Expandable = false;	// �Ƿ����չ���¼�����
		public string Rights = "";	// Ȩ���ַ���

		public string DefElement = "";	// �����õ�Ԫ����
		public string DefName = "";	// ����Ԫ���е�name����ֵ

		public TreeNode TreeNode = null;

		public NodeState NodeState = NodeState.None;

        public int Style = 0;

		public static string GetNodeStateString(NodeInfo nodeinfo)
		{
			string strState = "";
			if ((nodeinfo.NodeState & NodeState.Account) == NodeState.Account)
				strState = "�ʻ�����";
			if ((nodeinfo.NodeState & NodeState.Object) == NodeState.Object)
			{
				if (strState != "")
					strState += ",";
				strState = "����";
			}

			return strState;
		}

	}

    public delegate void NodeRightsChangedEventHandle(object sender,
    NodeRightsChangedEventArgs e);

    public class NodeRightsChangedEventArgs : EventArgs
    {
        public TreeNode Node = null;
        public string Rights = "";
    }

}
