using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Xml;
using System.Text;

using DigitalPlatform.GUI;
using DigitalPlatform.Text;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Range;   // Backup

using DigitalPlatform.rms.Client.rmsws_localhost;

namespace DigitalPlatform.rms.Client
{
	/// <summary>
	/// Summary description for ResTree.
	/// </summary>
	public class ResTree : System.Windows.Forms.TreeView
	{
		public ApplicationInfo AppInfo = null;

		public DigitalPlatform.StopManager stopManager = null;

		RmsChannel channel = null;

		#region	��Դ���͡�����Icon�±���

		public const int RESTYPE_SERVER = 2;
		public const int RESTYPE_DB = 0;
		public const int RESTYPE_FROM = 1;
		public const int RESTYPE_LOADING = 3;
		public const int RESTYPE_FOLDER = 4;
		public const int RESTYPE_FILE = 5;

		#endregion

        #region ��Դ���
        public const int RESSTYLE_USERDATABASE = 0x01;
        #endregion


        public string Lang = "zh";

		public int[] EnabledIndices = null;	// null��ʾȫ�����ڡ����������ڣ�����Ԫ�ظ���Ϊ0����ʾȫ������

		public ServerCollection Servers = null;	// ����
		public RmsChannelCollection Channels = null;

		public event GuiAppendMenuEventHandle OnSetMenu;

		private System.Windows.Forms.ImageList imageList_resIcon;
		private System.ComponentModel.IContainer components;

		public ResTree()
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

				DoStop(null, null); 

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ResTree));
            this.imageList_resIcon = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // imageList_resIcon
            // 
            this.imageList_resIcon.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList_resIcon.ImageStream")));
            this.imageList_resIcon.TransparentColor = System.Drawing.Color.Fuchsia;
            this.imageList_resIcon.Images.SetKeyName(0, "database.bmp");
            this.imageList_resIcon.Images.SetKeyName(1, "searchfrom.bmp");
            this.imageList_resIcon.Images.SetKeyName(2, "");
            this.imageList_resIcon.Images.SetKeyName(3, "");
            this.imageList_resIcon.Images.SetKeyName(4, "");
            this.imageList_resIcon.Images.SetKeyName(5, "");
            // 
            // ResTree
            // 
            this.LineColor = System.Drawing.Color.Black;
            this.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.ResTree_AfterCheck);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ResTree_MouseUp);
            this.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.ResTree_AfterExpand);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ResTree_MouseDown);
            this.ResumeLayout(false);

		}
		#endregion

        public int Fill(TreeNode node)
        {
            string strError = "";
            int nRet = Fill(node, out strError);
            if (nRet == -1)
            {
                try
                {
                    MessageBox.Show(this,
                        strError);
                }
                catch
                {
                    // this�����Ѿ�������
                }
                return -1;
            }

            return nRet;
        }

		// �ݹ�
		public int Fill(TreeNode node,
            out string strError)
		{
            strError = "";
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

				for(i=0;i<Servers.Count;i++) 
				{
					Server server = (Server)Servers[i];
					TreeNode nodeNew = new TreeNode(server.Url, RESTYPE_SERVER, RESTYPE_SERVER);
					SetLoading(nodeNew);

					if (EnabledIndices != null
						&& StringUtil.IsInList(nodeNew.ImageIndex, EnabledIndices) == false)
						nodeNew.ForeColor = ControlPaint.LightLight(nodeNew.ForeColor);

					children.Add(nodeNew);
				}

				return 0;
			}


			// �����µĽڵ�����
			ResPath respath = new ResPath(node);

			this.channel = Channels.GetChannel(respath.Url);

			Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

			/*
			int nStart = 0;
			int nPerCount = -1;
			int nCount = 0;
			*/

			ResInfoItem [] items = null;

#if NO
			DigitalPlatform.Stop stop = null;

			if (stopManager != null) 
			{
				stop = new DigitalPlatform.Stop();

                stop.Register(this.stopManager, true);	// ����������

                stop.OnStop += new StopEventHandler(this.DoStop);
				stop.Initial("������Ŀ¼: " + respath.FullPath);
				stop.BeginLoop();
			}
#endif
            DigitalPlatform.Stop stop = PrepareStop("������Ŀ¼: " + respath.FullPath);

            long lRet = 0;
            try
            {
                lRet = channel.DoDir(respath.Path,
                    this.Lang,
                    null,   // ����Ҫ�г�ȫ�����Ե�����
                    out items,
                    out strError);
            }
            finally
            {
                EndStop(stop);
            }

#if NO
			if (stopManager != null) 
			{
				stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
				stop.Initial("");

				stop.Unregister();	// ����������
			}
#endif

			this.channel = null;

			if (lRet == -1) 
			{
#if NO
				try 
				{
					MessageBox.Show(this, "Channel::DoDir() Error: " + strError);
				}
				catch
				{
					// this�����Ѿ�������
					return -1;
				}
#endif
				if (node != null) 
				{
					SetLoading(node);	// ������ƺ������³���+��
					node.Collapse();
				}
				return -1;
			}


			if (items != null) 
			{
				children.Clear();

				//for(i=0;i<items.Length;i++) 
                foreach(ResInfoItem res_item in items)
				{
                    // ResInfoItem res_item = items[i];

                    TreeNode nodeNew = new TreeNode(res_item.Name, res_item.Type, res_item.Type);

                    if (res_item.Type == RESTYPE_DB)
                    {
                        DbProperty prop = new DbProperty();
                        prop.TypeString = res_item.TypeString;  // �����ַ���
                        nodeNew.Tag = prop;
                        List<string> column_names = null;
                        int nRet = GetBrowseColumns(
                            node.Text,
                            res_item.Name,
                            out column_names,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        prop.ColumnNames = column_names;
                    }
                    else
                    {
                        ItemProperty prop = new ItemProperty();
                        prop.TypeString = res_item.TypeString;  // �����ַ���
                        nodeNew.Tag = prop;
                    }

                    if (res_item.HasChildren)
						SetLoading(nodeNew);

					if (EnabledIndices != null
						&& StringUtil.IsInList(nodeNew.ImageIndex, EnabledIndices) == false)
						nodeNew.ForeColor = ControlPaint.LightLight(nodeNew.ForeColor);

					children.Add(nodeNew);
				}
			}

			return 0;
		}

        // return:
        //      -1  ������ϣ�������Ժ�Ĳ���
        //      0   �ɹ�
        public int GetBrowseColumns(
            string strServerUrl,
            string strDbName,
            out List<string> column_names,
            out string strError)
        {
            strError = "";
            column_names = new List<string>();

            this.channel = Channels.GetChannel(strServerUrl);
            Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

            try
            {
                string strCfgFilePath = strDbName + "/cfgs/browse";
                string strStyle = "content,data";   // ,metadata,timestamp,outputpath";

                string strResult = "";
                string strMetaData = "";
                byte[] baOutputTimeStamp = null;
                string strOutputResPath = "";
                long lRet = this.channel.GetRes(strCfgFilePath,
                    strStyle,
                    out strResult,
                    out strMetaData,
                    out baOutputTimeStamp,
                    out strOutputResPath,
                    out strError);
                if (lRet == -1)
                    return -1;

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strResult);
                }
                catch (Exception ex)
                {
                    strError = "�����ļ� " + strCfgFilePath + " ����װ��XMLDOMʱ����: " + ex.Message;
                    return -1;
                }

                XmlNodeList nodes = dom.DocumentElement.SelectNodes("//col");
                for (int j = 0; j < nodes.Count; j++)
                {
                    string strColumnTitle = DomUtil.GetAttr(nodes[j], "title");
                    column_names.Add(strColumnTitle);
                }

                return 0;
            }
            finally
            {
                this.channel = null;
            }
        }

		// �ص�����
		void DoStop(object sender, StopEventArgs e)
		{
			if (this.channel != null)
				this.channel.Abort();
		}

		// ��һ���ڵ��¼�����"loading..."���Ա����+��
		public static void SetLoading(TreeNode node)
		{
            if (node == null)
                return;

			// ��node
			TreeNode nodeNew = new TreeNode("loading...", RESTYPE_LOADING, RESTYPE_LOADING);

			node.Nodes.Clear();
			node.Nodes.Add(nodeNew);
		}

		// �¼��Ƿ����loading...?
		public static bool IsLoading(TreeNode node)
		{
			if (node.Nodes.Count == 0)
				return false;

			if (node.Nodes[0].Text == "loading...")
				return true;

			return false;
		}

		private void ResTree_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            // TreeNode node = this.SelectedNode;

            //
            menuItem = new MenuItem("����������(&A)");
            menuItem.Click += new System.EventHandler(this.menu_newServer);
            //	menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            // 
            menuItem = new MenuItem("��¼(&L)");
            menuItem.Click += new System.EventHandler(this.menu_login);
            if (this.SelectedNode == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�ǳ�(&O)");
            menuItem.Click += new System.EventHandler(this.menu_logout);
            if (this.SelectedNode == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ˢ��(&R)");
            menuItem.Click += new System.EventHandler(this.menu_refresh);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("��ʼ�����ݿ�(&I)");
            menuItem.Click += new System.EventHandler(this.menu_initialDB);
            if (this.SelectedNode == null || this.SelectedNode.ImageIndex != RESTYPE_DB)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ˢ�����ݿⶨ��(&R)");
            menuItem.Click += new System.EventHandler(this.menu_refreshDB);
            if (this.SelectedNode == null || this.SelectedNode.ImageIndex != RESTYPE_DB)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�༭�����ļ�(&E)");
            menuItem.Click += new System.EventHandler(this.menu_editCfgFile);
            if (this.SelectedNode == null || this.SelectedNode.ImageIndex != RESTYPE_FILE)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�޸�����(&P)");
            menuItem.Click += new System.EventHandler(this.menu_changePassword);
            if (this.SelectedNode == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("��̨����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_batchTask);
            if (this.SelectedNode == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("��������(&E)");
            menuItem.Click += new System.EventHandler(this.menu_export);
            if (this.SelectedNode == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("��������(&I)");
            menuItem.Click += new System.EventHandler(this.menu_import);
            if (this.SelectedNode == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("���ٵ�������(&S)");
            menuItem.Click += new System.EventHandler(this.menu_quickImport);
            if (this.SelectedNode == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("�������");
            contextMenu.MenuItems.Add(menuItem);

            MenuItem subMenuItem = new MenuItem("delete keys index");
            subMenuItem.Click += new System.EventHandler(this.menu_deleteKeysIndex);
            if (this.SelectedNode == null)
                subMenuItem.Enabled = false;
            menuItem.MenuItems.Add(subMenuItem);


            subMenuItem = new MenuItem("create keys index");
            subMenuItem.Click += new System.EventHandler(this.menu_createKeysIndex);
            if (this.SelectedNode == null)
                subMenuItem.Enabled = false;
            menuItem.MenuItems.Add(subMenuItem);

            subMenuItem = new MenuItem("disable keys index");
            subMenuItem.Click += new System.EventHandler(this.menu_disableKeysIndex);
            if (this.SelectedNode == null)
                subMenuItem.Enabled = false;
            menuItem.MenuItems.Add(subMenuItem);

            subMenuItem = new MenuItem("rebuild keys index");
            subMenuItem.Click += new System.EventHandler(this.menu_rebuildKeysIndex);
            if (this.SelectedNode == null)
                subMenuItem.Enabled = false;
            menuItem.MenuItems.Add(subMenuItem);

            subMenuItem = new MenuItem("flush pending keys");
            subMenuItem.Click += new System.EventHandler(this.menu_flushKeysCache);
            if (this.SelectedNode == null)
                subMenuItem.Enabled = false;
            menuItem.MenuItems.Add(subMenuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // 
            menuItem = new MenuItem("����ѡ(&M)");
            menuItem.Click += new System.EventHandler(this.menu_toggleCheckBoxes);
            if (this.CheckBoxes == true)
                menuItem.Checked = true;
            else
                menuItem.Checked = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("���ȫ����ѡ(&C)");
            menuItem.Click += new System.EventHandler(this.menu_clearCheckBoxes);
            if (this.CheckBoxes == true)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // 
            menuItem = new MenuItem("�ڵ���Ϣ(&N)");
            menuItem.Click += new System.EventHandler(this.menu_nodeInfo);
            contextMenu.MenuItems.Add(menuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // 
            menuItem = new MenuItem("���¼�������Ŀ¼���ļ�(&N)");
            menuItem.Click += new System.EventHandler(this.menu_newDirectoryOrFile);
            if (this.SelectedNode != null
                && (this.SelectedNode.ImageIndex == ResTree.RESTYPE_SERVER
                || this.SelectedNode.ImageIndex == ResTree.RESTYPE_DB
                || this.SelectedNode.ImageIndex == ResTree.RESTYPE_FOLDER))
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;

            contextMenu.MenuItems.Add(menuItem);

            // ����
            {
                menuItem = new MenuItem("���Գ�ʱ");
                menuItem.Click += menuItem_testTimeout_Click;
                contextMenu.MenuItems.Add(menuItem);
            }


            ////



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
                contextMenu.Show(this, new Point(e.X, e.Y));
        }

        // ���Գ�ʱ
        void menuItem_testTimeout_Click(object sender, EventArgs e)
        {

            ResPath respath = new ResPath(this.SelectedNode);

            this.channel = Channels.GetChannel(respath.Url);

            Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

            this.channel.DoTest("test");

            this.channel = null;
        }

        // �´���һ��Ŀ¼���ļ�
        void menu_newDirectoryOrFile(object sender, System.EventArgs e)
        {
            string strError = "";

            int nRet = NewServerSideObject(ResTree.RESTYPE_FOLDER, out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 1)
            {
                strError = "�����Ѿ����� : " + strError;
                goto ERROR1;
            }

            menu_refresh(null, null);
            if (this.SelectedNode != null)
                this.SelectedNode.Expand();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        int NewServerSideObject(int nType,
            out string strError)
        {
            strError = "";
            if (this.SelectedNode == null)
            {
                strError = "��δѡ���׼�ڵ�";
                goto ERROR1;
            }

            if (this.SelectedNode.ImageIndex != ResTree.RESTYPE_SERVER
                && this.SelectedNode.ImageIndex != ResTree.RESTYPE_DB
                && this.SelectedNode.ImageIndex != ResTree.RESTYPE_FOLDER)
            {
                strError = "ֻ���ڷ����������ݿ⡢Ŀ¼����֮�´�����Ŀ¼";
                goto ERROR1;
            }

            NewObjectDlg dlg = new NewObjectDlg();
            dlg.Font = GuiUtil.GetDefaultFont();
            dlg.Type = nType;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return 0;

            ResPath respath = new ResPath(this.SelectedNode);

            string strPath = "";
            if (respath.Path == "")
                strPath = dlg.textBox_objectName.Text;
            else
                strPath = respath.Path + "/" + dlg.textBox_objectName.Text;

            int nRet = NewServerSideObject(
                respath.Url,
                strPath,
                dlg.Type,
                null,
                null,   // byte[] baTimeStamp,
                out strError);

            return nRet;
        ERROR1:
            return -1;
        }

        // �ڷ������˴�������
        // return:
        //		-1	����
        //		1	�Լ�����ͬ������
        //		0	��������
        int NewServerSideObject(
            string strServerUrl,
            string strPath,
            int nType,
            Stream stream,
            byte[] baTimeStamp,
            out string strError)
        {
            strError = "";

            this.channel = Channels.GetChannel(strServerUrl);
            Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

        REDO:

#if NO
            DigitalPlatform.Stop stop = null;

            if (stopManager != null)
            {
                stop = new DigitalPlatform.Stop();
                stop.Register(this.stopManager, true);	// ����������

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڴ����¶���: " + strServerUrl + "?" + strPath);
                stop.BeginLoop();

            }
#endif
            DigitalPlatform.Stop stop = PrepareStop("���ڴ����¶���: " + strServerUrl + "?" + strPath);

            byte[] baOutputTimestamp = null;
            string strOutputPath = "";
            string strStyle = "";

            if (nType == ResTree.RESTYPE_FOLDER)
                strStyle = "createdir";

            string strRange = "";
            if (stream != null && stream.Length != 0)
            {
                Debug.Assert(stream.Length != 0, "test");
                strRange = "0-" + Convert.ToString(stream.Length - 1);
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

            EndStop(stop);
#if NO
            if (stopManager != null)
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.Unregister();	// ����������
            }
#endif

            if (lRet == -1)
            {
                if (this.channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                {
                    baTimeStamp = baOutputTimestamp;
                    goto REDO;
                }

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

		// ˢ��
		public void menu_refresh(object sender, System.EventArgs e)
		{
            /*
			if (this.SelectedNode == null) 
			{
				this.Fill(null);
				return;
			}

			ResPath respath = new ResPath(this.SelectedNode);

			// ˢ��
			ResPath OldPath = new ResPath(this.SelectedNode);

			respath.Path = "";
			ExpandPath(respath);	// ѡ�з����������½ڵ����
			SetLoading(this.SelectedNode);

			ExpandPath(OldPath);
             */
            this.Refresh(RefreshStyle.All);
		}

        // ˢ�·��
        public enum RefreshStyle
        {
            All = 0xffff,
            Servers = 0x01,
            Selected = 0x02,
        }

        public void Refresh(RefreshStyle style)
        {
            ResPath OldPath = null;
            bool bExpanded = false;

            // ����
            if (this.SelectedNode != null)
            {
                OldPath = new ResPath(this.SelectedNode);
                bExpanded = this.SelectedNode.IsExpanded;
            }

            // ˢ�·�������
            if ((style & RefreshStyle.Servers) == RefreshStyle.Servers) 
			{
				this.Fill(null);
			}


            // ˢ�µ�ǰѡ��Ľڵ�
            if (OldPath != null
                && (style & RefreshStyle.Selected) == RefreshStyle.Selected)
            {
                ResPath respath = OldPath.Clone();

                // ˢ��

                respath.Path = "";
                ExpandPath(respath);	// ѡ�з����������½ڵ����
                SetLoading(this.SelectedNode);

                ExpandPath(OldPath);
                if (bExpanded == true && this.SelectedNode != null)
                    this.SelectedNode.Expand();
            }
        }

		// ����һ���������ڵ�
		void menu_newServer(object sender, System.EventArgs e)
		{
			ServerNameDlg dlg = new ServerNameDlg();
            dlg.Font = GuiUtil.GetDefaultFont();
			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;

			int nRet = Servers.NewServer(dlg.textBox_url.Text, -1);
			if (nRet == 1) 
			{
				MessageBox.Show(this, "������ " + dlg.textBox_url.Text + " �Ѿ�����...");
				return;
			}

			// ˢ��
			this.Fill(null);

			// ˢ�º�ָ�ԭ��ѡ���node
		}

		// ��¼
		void menu_login(object sender, System.EventArgs e)
		{
			if (this.SelectedNode == null) 
			{
				MessageBox.Show(this, "��δѡ��ڵ�");
				return;
			}

			ResPath respath = new ResPath(this.SelectedNode);

			this.channel = Channels.GetChannel(respath.Url);

			Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

			string strError;
			// return:
			//		-1	error
			//		0	login failed
			//		1	login succeed
			int nRet = channel.UiLogin(
				null,
				respath.Path,
				LoginStyle.FillDefaultInfo,
				out strError);


			this.channel = null;

			if (nRet == -1 || nRet == 0) 
			{
				MessageBox.Show(this, strError);
				return;
			}

			// ˢ��
			ResPath OldPath = new ResPath(this.SelectedNode);

			respath.Path = "";
			ExpandPath(respath);	// ѡ�з����������½ڵ����
			SetLoading(this.SelectedNode);

			ExpandPath(OldPath);

		}

		// �ǳ�
		void menu_logout(object sender, System.EventArgs e)
		{
			if (this.SelectedNode == null) 
			{
				MessageBox.Show(this, "��δѡ��ڵ�");
				return;
			}

			ResPath respath = new ResPath(this.SelectedNode);

			this.channel = Channels.GetChannel(respath.Url);

			Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

#if NO
			DigitalPlatform.Stop stop = null;

			if (stopManager != null) 
			{
				stop = new DigitalPlatform.Stop();

                stop.Register(this.stopManager, true);	// ����������

                stop.OnStop += new StopEventHandler(this.DoStop);
				stop.Initial("���ڵǳ�: " + respath.FullPath);
				stop.BeginLoop();

			}
#endif
            DigitalPlatform.Stop stop = PrepareStop("���ڵǳ�: " + respath.FullPath);

			string strError;
			// return:
			//		-1	error
			//		0	login failed
			//		1	login succeed
			long nRet = channel.DoLogout(
				out strError);

            EndStop(stop);
#if NO
			if (stopManager != null) 
			{
				stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
				stop.Initial("");

				stop.Unregister();	// ����������
			}
#endif

			this.channel = null;

			if (nRet == -1) 
			{
				MessageBox.Show(this, strError);
				return;
			}

			// ˢ��
			//ResPath OldPath = new ResPath(this.SelectedNode);

			respath.Path = "";
			ExpandPath(respath);	// ѡ�з����������½ڵ����
			SetLoading(this.SelectedNode);
			if (this.SelectedNode != null)
				this.SelectedNode.Collapse();

			//ExpandPath(OldPath);

		}

        void menu_deleteKeysIndex(object sender, System.EventArgs e)
        {
            ManageKeysIndex("deletekeysindex");
        }

        void menu_createKeysIndex(object sender, System.EventArgs e)
        {
            ManageKeysIndex("createkeysindex");
        }

        void menu_disableKeysIndex(object sender, System.EventArgs e)
        {
            ManageKeysIndex("disablekeysindex");
        }

        void menu_rebuildKeysIndex(object sender, System.EventArgs e)
        {
            ManageKeysIndex("rebuildkeysindex");
        }

        void menu_flushKeysCache(object sender, System.EventArgs e)
        {
            ManageKeysIndex("flushpendingkeys");
        }

        void ManageKeysIndex(string strAction)
        {
            string strError = "";
            int nRet = ManageKeysIndex(null, strAction, null, out strError);
            if (nRet == -1)
                goto ERROR1;
            MessageBox.Show(this, "�����ɹ�");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        int ManageKeysIndex(
            string strDbUrl,
            string strAction,
            string strMessage,
            out string strError)
        {
            strError = "";

            ResPath respath = null;
            if (strDbUrl == null)
            {
                if (this.SelectedNode == null)
                {
                    strError = "��δѡ��ҪҪ���������ݿ�ڵ�";
                    goto ERROR1;
                }

                if (this.SelectedNode.ImageIndex != RESTYPE_DB)
                {
                    strError = "��ѡ��Ľڵ㲻�����ݿ����͡���ѡ��Ҫ���������ݿ�ڵ㡣";
                    goto ERROR1;
                }
                respath = new ResPath(this.SelectedNode);
            }
            else
                respath = new ResPath(strDbUrl);

            this.channel = Channels.GetChannel(respath.Url);
            Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

#if NO
            DigitalPlatform.Stop stop = null;

            if (stopManager != null)
            {
                stop = new DigitalPlatform.Stop();

                stop.Register(this.stopManager, true);	// ����������

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڵ������� " + respath.FullPath);
                stop.BeginLoop();
            }
#endif
            DigitalPlatform.Stop stop = PrepareStop(
                strMessage == null ?
                "���ڶ� " + respath.FullPath + " ���й������ " + strAction + " ..."
                : strMessage);

            TimeSpan old_timeout = channel.Timeout;
            if (strAction == "endfastappend")
            {
                // ��β�׶ο���Ҫ�ķѺܳ���ʱ��
                channel.Timeout = new TimeSpan(3, 0, 0);
            }

            try
            {
                long lRet = channel.DoRefreshDB(
                    strAction,
                    respath.Path,
                    false,
                    out strError);
                if (lRet == -1)
                {
                    strError = "�������ݿ� '" + respath.Path + "' ʱ����: " + strError;
                    goto ERROR1;
                }

            }
            finally
            {
                EndStop(stop);
#if NO
                if (stopManager != null)
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");

                    stop.Unregister();	// �������������
                }
#endif
                if (strAction == "endfastappend")
                {
                    channel.Timeout = old_timeout;
                }

                this.channel = null;
            }
            return 0;
        ERROR1:
            return -1;
        }

        // ���ٵ���
        void menu_quickImport(object sender, System.EventArgs e)
        {
            ImportData(true);
        }

        // ���ٵ�������
        void menu_import(object sender, System.EventArgs e)
        {
            ImportData(false);
        }

        // ��������
        int ImportData(bool bFastMode = false)
        {
            string strError = "";
            string strTimeMessage = "";
            int CHUNK_SIZE = 150 * 1024;    // 70

            if (this.SelectedNode == null)
            {
                strError = "��δѡ��ҪҪ�������ݵ����ݿ�ڵ�";
                goto ERROR0;
            }

            if (this.SelectedNode.ImageIndex != RESTYPE_DB)
            {
                strError = "��ѡ��Ľڵ㲻�����ݿ����͡���ѡ��Ҫ�������ݵ����ݿ�ڵ㡣";
                goto ERROR0;
            }

            if (bFastMode == true)
            {
                DialogResult result = MessageBox.Show(this,
        "���棺\r\n�ڿ��ٵ����ڼ䣬������ݿ�����һ������״̬�������ݿ�������������޸Ĳ�����ʱ�ᱻ��ֹ��ֱ��������ɡ�\r\n\r\n����ȷʵҪ���п��ٵ���ô?",
        "��������",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return 0;
            }

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ����������ļ�";
            dlg.FileName = "";
            dlg.Filter = "�����ļ� (*.dp2bak)|*.dp2bak|XML�ļ� (*.xml)|*.xml|ISO2709�ļ� (*.iso;*.mrc)|*.iso;*.mrc|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
            {
                return 0;
            }

            long lTotalCount = 0;

            ImportUtil import_util = new ImportUtil();
            int nRet = import_util.Begin(this,
                this.AppInfo,
                dlg.FileName,
                out strError);
            if (nRet == -1 || nRet == 1)
                goto ERROR0;

#if NO
            ResPath respath = new ResPath(this.SelectedNode);
            this.channel = Channels.GetChannel(respath.Url);
            Debug.Assert(channel != null, "Channels.GetChannel() �쳣");
#endif
            // ȱʡ��Ŀ�����ݿ�·��
            ResPath default_target_respath = new ResPath(this.SelectedNode);
            RmsChannel cur_channel = Channels.CreateTempChannel(default_target_respath.Url);
            Debug.Assert(cur_channel != null, "Channels.GetChannel() �쳣");

            List<string> target_dburls = new List<string>();
#if NO
            DigitalPlatform.Stop stop = null;

            if (stopManager != null)
            {
                stop = new DigitalPlatform.Stop();

                stop.Register(this.stopManager, true);	// ����������

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڵ������� " + respath.FullPath);
                stop.BeginLoop();
            }
#endif
            DigitalPlatform.Stop stop = PrepareStop("���ڵ������� ...");  // + default_target_respath.FullPath);
            stop.OnStop -= new StopEventHandler(this.DoStop);   // ȥ��ȱʡ�Ļص�����
            stop.OnStop += (sender1, e1) =>
            {
                if (cur_channel != null)
                    cur_channel.Abort();
            };
            stop.Style = StopStyle.EnableHalfStop;  // API�ļ�϶�����жϡ������ȡ���������;����Ϊ�ж϶����� Session ʧЧ���������ʧ�������޷� Retry ��ȡ
            ProgressEstimate estimate = new ProgressEstimate();
            try // open import util
            {
                    bool bDontPromptTimestampMismatchWhenOverwrite = false;
                    DbNameMap map = new DbNameMap();
                    long lSaveOffs = -1;

                    estimate.SetRange(0, import_util.Stream.Length);
                    estimate.StartEstimate();

                    stop.SetProgressRange(0, import_util.Stream.Length);

                    List<UploadRecord> records = new List<UploadRecord>();
                    int nBatchSize = 0;
                    for (int index = 0; ; index++)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop.State != 0)
                        {
                            DialogResult result = MessageBox.Show(this,
                                "ȷʵҪ�жϵ�ǰ���������?",
                                "��������",
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

                        //string strXml = "";
                        //string strResPath = "";
                        //string strTimeStamp = "";
                        UploadRecord record = null;

                        if (import_util.FileType == ExportFileType.BackupFile)
                        {
                            if (lSaveOffs != -1)
                                import_util.Stream.Seek(lSaveOffs, SeekOrigin.Begin);
                        }

                        nRet = import_util.ReadOneRecord(out record,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 1)
                            break;

                        if (import_util.FileType == ExportFileType.BackupFile)
                        {
                            // ����ÿ�ζ�ȡ����ļ�ָ��λ��
                            lSaveOffs = import_util.Stream.Position;
                        }

                        Debug.Assert(record != null, "");
#if NO
                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "XMLװ��DOMʱ����: " + ex.Message;
                        goto ERROR1;
                    }
                    string strResPath = DomUtil.GetAttr(DpNs.dprms, dom.DocumentElement, "path");
                    string strTimeStamp = DomUtil.GetAttr(DpNs.dprms, dom.DocumentElement, "timestamp");
#endif

                        // ׼��Ŀ��·��
                        {
                            string strLongPath = record.Url + "?" + record.RecordBody.Path;
#if NO
                        // ����ԭʼ·��׼������д���·��
                        // return:
                        //      -1  ����
                        //      0   �û�����
                        //      1   �ɹ�
                        nRet = ImportUtil.PrepareOverwritePath(
                this.Servers,
                this.Channels,
                this,
                ref map,
                ref strLongPath,
                out strError);
                        if (nRet == 0 || nRet == -1)
                            goto ERROR1;
#endif
                            // ����ԭʼ·��׼������д���·��
                            // return:
                            //      -1  ����
                            //      0   �û�����
                            //      1   �ɹ�
                            //      2   Ҫ��������
                            nRet = ImportUtil.PrepareOverwritePath(
                                this,
                                this.Servers,
                                this.Channels,
                                this.AppInfo,
                                index,
                                default_target_respath.FullPath,
                                ref map,
                                ref strLongPath,
                                out strError);
                            if (nRet == 0 || nRet == -1)
                                goto ERROR1;
                            if (nRet == 2)
                                continue;

                            ResPath respath = new ResPath(strLongPath);
                            record.Url = respath.Url;
                            record.RecordBody.Path = respath.Path;

                            // ����ÿ�����ݿ�� URL
                            string strDbUrl = GetDbUrl(strLongPath);
                            if (target_dburls.IndexOf(strDbUrl) == -1)
                            {
                                // ÿ�����ݿ�Ҫ����һ�ο���ģʽ��׼������
                                if (bFastMode == true)
                                {
                                    nRet = ManageKeysIndex(strDbUrl,
                                        "beginfastappend",
                                        "���ڶ����ݿ� "+strDbUrl+" ���п��ٵ���ģʽ��׼������ ...",
                                        out strError);
                                    if (nRet == -1)
                                        goto ERROR1;
                                }
                                target_dburls.Add(strDbUrl);
                            }
                        }

                        bool bNeedPush = false;
                        // �Ƿ�Ҫ�ѻ��۵ļ�¼���ͳ�ȥ����д��?
                        // Ҫ�������¼�飺
                        // 1) ��ǰ��¼��ǰһ����¼֮�䣬�����˷�����
                        // 2) �ۻ��ļ�¼�ߴ糬��Ҫ��
                        // 3) ��ǰ��¼��һ������ļ�¼ (������ΪҪ���ִ��ļ��ж�����˳����д��(����׷��ʱ��ĺ�������˳��)���ͱ����ڵ���д�뱾��ǰ����д����۵���Щ��¼)
                        if (records.Count > 0)
                        {
                            if (record.TooLarge() == true)
                                bNeedPush = true;
                            else if (nBatchSize + record.RecordBody.Xml.Length > CHUNK_SIZE)
                                bNeedPush = true;
                            else
                            {
                                if (LastUrl(records) != record.Url)
                                    bNeedPush = true;
                            }
                        }


                        if (bNeedPush == true)
                        {
                            // ׼�� Channel
                            Debug.Assert(records.Count > 0, "");
                            cur_channel = ImportUtil.GetChannel(this.Channels,
                                stop,
                                LastUrl(records),
                                cur_channel);

                            List<UploadRecord> save_records = new List<UploadRecord>();
                            save_records.AddRange(records);

                            while (records.Count > 0)
                            {
                                // �� XML ��¼����д�����ݿ�
                                // return:
                                //      -1  ����
                                //      >=0 �����Ѿ�д��ļ�¼����������������ʱ records ���ϵ�Ԫ����û�б仯(��Ԫ�ص�Path��Timestamp���б仯)�������Ҫ�����ɽ�ȡrecords�����к���δ����Ĳ����ٴε��ñ�����
                                nRet = ImportUtil.WriteRecords(
                                    this,
                                    stop,
                                    cur_channel,
                                    bFastMode,
                                    records,
                                    ref bDontPromptTimestampMismatchWhenOverwrite,
                                    out strError);
                                if (nRet == -1)
                                    goto ERROR1;
                                if (nRet == 0)
                                {
                                    // TODO: ����Ը�Ϊ����д��
                                    strError = "WriteRecords() error :" + strError;
                                    goto ERROR1;
                                }
                                Debug.Assert(nRet <= records.Count, "");
                                records.RemoveRange(0, nRet);
                                lTotalCount += nRet;
                            }

                            // ���ض���
                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = import_util.UploadObjects(
                                stop,
                                cur_channel,
                                save_records,
                                ref bDontPromptTimestampMismatchWhenOverwrite,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            nBatchSize = 0;
                            stop.SetProgressValue(import_util.Stream.Position);

                            stop.SetMessage("�Ѿ�д���¼ " + lTotalCount.ToString() + " ����"
    + "ʣ��ʱ�� " + ProgressEstimate.Format(estimate.Estimate(import_util.Stream.Position)) + " �Ѿ���ʱ�� " + ProgressEstimate.Format(estimate.delta_passed));

                        }

                        // ��� ��¼�� XML �ߴ�̫�󲻱��ڳ������أ���Ҫ�ڵ���ֱ������
                        if (record.TooLarge() == true)
                        {
                            // ׼�� Channel
                            // ResPath respath = new ResPath(record.RecordBody.Path);
                            cur_channel = ImportUtil.GetChannel(this.Channels,
                                stop,
                                record.Url,
                                cur_channel);

                            // д��һ�� XML ��¼
                            // return:
                            //      -1  ����
                            //      0   �����ж���������
                            //      1   �ɹ�
                            //      2   ����������������������
                            nRet = ImportUtil.WriteOneXmlRecord(
                                this,
                                stop,
                                cur_channel,
                                record,
                                ref bDontPromptTimestampMismatchWhenOverwrite,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                            if (nRet == 0)
                                goto ERROR1;

                            List<UploadRecord> temp = new List<UploadRecord>();
                            temp.Add(record);
                            // ���ض���
                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = import_util.UploadObjects(
                                stop,
                                cur_channel,
                                temp,
                                ref bDontPromptTimestampMismatchWhenOverwrite,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            lTotalCount += 1;
                            continue;
                        }

                        records.Add(record);
                        if (record.RecordBody != null && record.RecordBody.Xml != null)
                            nBatchSize += record.RecordBody.Xml.Length;
                    }

                    // ����ύһ��
                    if (records.Count > 0)
                    {
                        // ׼�� Channel
                        Debug.Assert(records.Count > 0, "");
                        cur_channel = ImportUtil.GetChannel(this.Channels,
                            stop,
                            LastUrl(records),
                            cur_channel);

                        List<UploadRecord> save_records = new List<UploadRecord>();
                        save_records.AddRange(records);

                        while (records.Count > 0)
                        {
                            // �� XML ��¼����д�����ݿ�
                            // return:
                            //      -1  ����
                            //      >=0 �����Ѿ�д��ļ�¼����������������ʱ records ���ϵ�Ԫ����û�б仯(��Ԫ�ص�Path��Timestamp���б仯)�������Ҫ�����ɽ�ȡrecords�����к���δ����Ĳ����ٴε��ñ�����
                            nRet = ImportUtil.WriteRecords(
                                this,
                                stop,
                                cur_channel,
                                bFastMode,
                                records,
                                ref bDontPromptTimestampMismatchWhenOverwrite,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                            if (nRet == 0)
                            {
                                strError = "WriteRecords() error :" + strError;
                                goto ERROR1;
                            }
                            Debug.Assert(nRet <= records.Count, "");
                            records.RemoveRange(0, nRet);
                            lTotalCount += nRet;
                        }

                        // ���ض���
                        // return:
                        //      -1  ����
                        //      0   �ɹ�
                        nRet = import_util.UploadObjects(
                            stop,
                            cur_channel,
                            save_records,
                            ref bDontPromptTimestampMismatchWhenOverwrite,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        nBatchSize = 0;
                        stop.SetProgressValue(import_util.Stream.Position);

                        stop.SetMessage("�Ѿ�д���¼ " + lTotalCount.ToString() + " ����"
    + "ʣ��ʱ�� " + ProgressEstimate.Format(estimate.Estimate(import_util.Stream.Position)) + " �Ѿ���ʱ�� " + ProgressEstimate.Format(estimate.delta_passed));

                        records.Clear();
                        nBatchSize = 0;
                    }
            }// close import util
            finally
            {
                if (bFastMode == true)
                {
                    foreach (string url in target_dburls)
                    {
                        string strQuickModeError = "";
                        nRet = ManageKeysIndex(url,
                            "endfastappend",
                            "���ڶ����ݿ� " + url + " ���п��ٵ���ģʽ����β�����������ĵȴ� ...",
                            out strQuickModeError);
                        if (nRet == -1)
                            MessageBox.Show(this, strQuickModeError);
                    }
                }

                EndStop(stop);
#if NO
                if (stopManager != null)
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");

                    stop.Unregister();	// �������������
                }
#endif
                cur_channel.Close();
                cur_channel = null;

                import_util.End();
            }

            strTimeMessage = "�ܹ��ķ�ʱ��: " + estimate.GetTotalTime().ToString();
            MessageBox.Show(this, "�ļ� " + dlg.FileName + " �ڵ������Ѿ��ɹ������������ݿ�:\r\n\r\n" + StringUtil.MakePathList(target_dburls, "\r\n") + "\r\n\r\n�������¼ " + lTotalCount.ToString() + " ����\r\n\r\n" + strTimeMessage);
            return 0;
        ERROR0:
            MessageBox.Show(this, strError);
            return -1;
        ERROR1:
            MessageBox.Show(this, strError);
            // ʹ���� lTotalCount �� estimate �Ժ�ı���
            if (lTotalCount > 0)
            {
                strTimeMessage = "�ܹ��ķ�ʱ��: " + estimate.GetTotalTime().ToString();
                MessageBox.Show(this, "�ļ� " + dlg.FileName + " �ڵĲ��������Ѿ��ɹ������������ݿ�:\r\n\r\n" + StringUtil.MakePathList(target_dburls, "\r\n") + "\r\n\r\n�������¼ " + lTotalCount.ToString() + " ����\r\n\r\n" + strTimeMessage);
            }
            return -1;
        }

#if NO
        // ���������һ��Ԫ�ص� LongPath
        static string LastLongPath(List<UploadRecord> records)
        {
            Debug.Assert(records.Count > 0, "");
            UploadRecord last_record = records[records.Count - 1];
            if (last_record.RecordBody == null)
                return null;
            return last_record.RecordBody.Path;
        }
#endif
        // ���������һ��Ԫ�ص� Url
        static string LastUrl(List<UploadRecord> records)
        {
            Debug.Assert(records.Count > 0, "");
            UploadRecord last_record = records[records.Count - 1];
            return last_record.Url;
        }

        static string GetDbUrl(string strLongPath)
        {
            ResPath respath = new ResPath(strLongPath);
            respath.MakeDbName();
            return respath.FullPath;
        }

        // ��̨�������
        void menu_batchTask(object sender, System.EventArgs e)
        {
            string strError = "";

            if (this.SelectedNode == null)
            {
                strError = "��δѡ��ҪҪ�۲����̨����ķ������ڵ�";
                goto ERROR1;
            }
            /*
            if (this.SelectedNode.ImageIndex != RESTYPE_DB)
            {
                strError = "��ѡ��Ľڵ㲻�����ݿ����͡���ѡ��Ҫ�������ݵ����ݿ�ڵ㡣";
                goto ERROR1;
            }
             * */

            ResPath respath = new ResPath(this.SelectedNode);
            RmsChannel cur_channel = Channels.CreateTempChannel(respath.Url);
            Debug.Assert(cur_channel != null, "Channels.GetChannel() �쳣");
            DigitalPlatform.Stop stop = PrepareStop("���ڵ������� " + respath.FullPath);

            stop.OnStop -= new StopEventHandler(this.DoStop);   // ȥ��ȱʡ�Ļص�����
            stop.OnStop += (sender1, e1) =>
            {
                if (cur_channel != null)
                    cur_channel.Abort();
            };

            try
            {
                BatchTaskForm dlg = new BatchTaskForm();

                dlg.Channel = cur_channel;
                dlg.Stop = stop;
                dlg.ShowDialog(this);

            }
            finally
            {
                EndStop(stop);

                cur_channel.Close();
                cur_channel = null;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��������
        void menu_export(object sender, System.EventArgs e)
        {
            string strError = "";

            string strTimeMessage = "";
            long lTotalCount = 0;	// ��������
            long lExportCount = 0;	// �Ѿ�����������

            if (this.SelectedNode == null)
            {
                strError = "��δѡ��ҪҪ�������ݵ����ݿ�ڵ�";
                goto ERROR1;
            }

            if (this.SelectedNode.ImageIndex != RESTYPE_DB)
            {
                strError = "��ѡ��Ľڵ㲻�����ݿ����͡���ѡ��Ҫ�������ݵ����ݿ�ڵ㡣";
                goto ERROR1;
            }

            ResPath respath = new ResPath(this.SelectedNode);

            // ѯ�ʵ������ݵķ�Χ
            ExportDataDialog data_range_dlg = new ExportDataDialog();
            data_range_dlg.DbPath = respath.Path;
            data_range_dlg.AllRecords = true;
            data_range_dlg.StartPosition = FormStartPosition.CenterScreen;
            data_range_dlg.ShowDialog(this);

            if (data_range_dlg.DialogResult != DialogResult.OK)
                return;

            string strRange = "0-9999999999";
            if (data_range_dlg.AllRecords == false)
                strRange = data_range_dlg.StartID + "-" + data_range_dlg.EndID;

            // �������ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ����������ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = "";
            dlg.FilterIndex = 1;

            dlg.Filter = "�����ļ� (*.dp2bak)|*.dp2bak|XML�ļ� (*.xml)|*.xml|ISO2709�ļ� (*.iso;*.mrc)|*.iso;*.mrc|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            ExportUtil export_util = new ExportUtil();


            string strQueryXml = "<target list='" + respath.Path
    + ":" + "__id'><item><word>"+strRange+"</word><match>exact</match><relation>range</relation><dataType>number</dataType><maxCount>-1</maxCount></item><lang>chi</lang></target>";


            RmsChannel cur_channel = Channels.CreateTempChannel(respath.Url);
            Debug.Assert(cur_channel != null, "Channels.GetChannel() �쳣");

#if NO
            DigitalPlatform.Stop stop = null;

            if (stopManager != null)
            {
                stop = new DigitalPlatform.Stop();

                stop.Register(this.stopManager, true);	// ����������

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڵ������� " + respath.FullPath);
                stop.BeginLoop();
            }
#endif
            DigitalPlatform.Stop stop = PrepareStop("���ڵ������� " + respath.FullPath);

            stop.OnStop -= new StopEventHandler(this.DoStop);   // ȥ��ȱʡ�Ļص�����
            stop.OnStop += (sender1, e1) =>
            {
                    if (cur_channel != null)
                        cur_channel.Abort();
                };

            try
            {
                long lRet = cur_channel.DoSearch(strQueryXml,
    "default",
    out strError);
                if (lRet == -1)
                {
                    strError = "�������ݿ� '"+respath.Path+"' ʱ����: " + strError;
                    goto ERROR1;
                }

                if (lRet == 0)
                {
                    strError = "���ݿ� '" + respath.Path + "' ��û���κ����ݼ�¼";
                    goto ERROR1;	// not found
                }

                stop.Style = StopStyle.EnableHalfStop;  // API�ļ�϶�����жϡ������ȡ���������;����Ϊ�ж϶����� Session ʧЧ���������ʧ�������޷� Retry ��ȡ

                lTotalCount = lRet;	// ��������
                long lThisCount = lTotalCount;
                long lStart = 0;

                ProgressEstimate estimate = new ProgressEstimate();

                estimate.SetRange(0, lTotalCount);
                estimate.StartEstimate();

                stop.SetProgressRange(0, lTotalCount);

                int nRet = export_util.Begin(this,
    dlg.FileName,
    out strError);
                if (nRet == -1)
                    goto ERROR1;

                DialogResult last_one_result = DialogResult.Yes;    // ǰһ�ζԻ���ѡ��ķ�ʽ
                bool bDontAskOne = false;
                int nRedoOneCount = 0;
                DialogResult last_get_result = DialogResult.Retry;    // ǰһ�ζԻ���ѡ��ķ�ʽ
                bool bDontAskGet = false;
                int nRedoGetCount = 0;

                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop.State != 0)
                    {
                        DialogResult result = MessageBox.Show(this,
                            "ȷʵҪ�жϵ�ǰ���������?",
                            "��������",
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

                    string strStyle = "id,xml,timestamp";
                    if (export_util.FileType == ExportFileType.BackupFile)
                        strStyle = "id,xml,timestamp,metadata";

                    nRedoGetCount = 0;
                REDO_GET:
                    Record[] searchresults = null;
                    lRet = cur_channel.DoGetSearchResult(
                        "default",
                        lStart,
                        lThisCount,
                        strStyle,
                        this.Lang,
                        stop,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                    {
                        if (stop.State != 0)    // �Ѿ��ж�
                            goto ERROR1;

                        // �Զ������д������ƣ����������ѭ��
                        if (bDontAskGet == true && last_get_result == DialogResult.Retry
                            && nRedoGetCount < 3)
                        {
                            nRedoGetCount++;
                            goto REDO_GET;
                        }

                        DialogResult result = MessageDlg.Show(this,
    "��ȡ�������ʱ (ƫ���� " + lStart + ") ����\r\n---\r\n"
    + strError + "\r\n---\r\n\r\n�Ƿ����Ի�ȡ����?\r\n\r\nע��\r\n[����] ���»�ȡ\r\n[�ж�] �ж�����������",
    "��������",
    MessageBoxButtons.RetryCancel,
    MessageBoxDefaultButton.Button1,
    ref bDontAskOne,
    new string[] { "����", "�ж�" });
                        last_get_result = result;

                        if (result == DialogResult.Retry)
                        {
                            nRedoGetCount = 0;
                            goto REDO_GET;
                        }

                        Debug.Assert(result == DialogResult.Cancel, ""); 

                        strError = "��ȡ�������ʱ����: " + strError;
                        goto ERROR1;
                    }

                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        Record record = searchresults[i];

                        if (i == 0)
                        {
                            stop.SetMessage("���������¼ " + record.Path + "������� " + lExportCount.ToString() + " ����"
        + "ʣ��ʱ�� " + ProgressEstimate.Format(estimate.Estimate(lExportCount)) + " �Ѿ���ʱ�� " + ProgressEstimate.Format(estimate.delta_passed));
                        }
                        nRedoOneCount = 0;

                    REDO_ONE:
                        nRet = export_util.ExportOneRecord(
                            cur_channel,
                            stop,
                            respath.Url,
                            record.Path,
                            record.RecordBody.Xml,
                            record.RecordBody.Metadata,
                            record.RecordBody.Timestamp,
                            out strError);
                        if (nRet == -1)
                        {
                            if (stop.State != 0)    // �Ѿ��ж�
                                goto ERROR1;

                            // ���ԡ��������ж�?
                            // ���Ե�ʱ��ע�Ᵽ���ļ����λ�ã���Ҫ���²����β��
                            // MessageBoxButtons.AbortRetryIgnore  YesNoCancel
                            if (bDontAskOne == true && last_one_result == DialogResult.No)
                                continue;   // TODO: �������־�ļ��м��������ļ�¼�������������������ʾ����

                            // �Զ������д������ƣ����������ѭ��
                            if (bDontAskOne == true && last_one_result == DialogResult.Yes
                                && nRedoOneCount < 3)
                            {
                                nRedoOneCount++;
                                goto REDO_ONE;
                            }

                            DialogResult result = MessageDlg.Show(this,
                                "������¼ '" + record.Path + "' ʱ����\r\n---\r\n"
                                + strError + "\r\n---\r\n\r\n�Ƿ����Ե�������?\r\n\r\nע��\r\n[����] ���µ���������¼\r\n[����] ���Ե���������¼������������Ĵ���\r\n[�ж�] �ж�����������",
                                "��������",
                                MessageBoxButtons.YesNoCancel,
                                MessageBoxDefaultButton.Button1,
                                ref bDontAskOne,
                                new string [] {"����","����","�ж�"});
                            last_one_result = result;

                            if (result == DialogResult.Yes)
                            {
                                nRedoOneCount = 0;
                                goto REDO_ONE;
                            }

                            if (result == DialogResult.No)
                                continue;

                            Debug.Assert(result == DialogResult.Cancel, ""); 

                            goto ERROR1;
                        }

                        stop.SetProgressValue(lExportCount + 1);
                        lExportCount++;
                    }

                    if (lStart + searchresults.Length >= lTotalCount)
                        break;

                    lStart += searchresults.Length;
                    lThisCount -= searchresults.Length;
                }

                strTimeMessage = "�ܹ��ķ�ʱ��: " + estimate.GetTotalTime().ToString();
            }
            finally
            {
                EndStop(stop);
#if NO
                if (stopManager != null)
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");

                    stop.Unregister();	// �������������
                }
#endif
                cur_channel.Close();
                cur_channel = null;

                export_util.End();
            }

            MessageBox.Show(this, "λ�ڷ����� '" + respath.Url + "' �ϵ����ݿ� '" + respath.Path + "' �ڹ��м�¼ " + lTotalCount.ToString() + " �������ε��� " + lExportCount.ToString() + " ����" + strTimeMessage);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            if (lExportCount > 0)
                MessageBox.Show(this, "���ݿ��ڹ��м�¼ " + lTotalCount.ToString() + " �������ε��� " + lExportCount.ToString() + " ��");
        }



        DigitalPlatform.Stop PrepareStop(string strText)
        {
            if (stopManager == null)
                return null;

            DigitalPlatform.Stop stop = new DigitalPlatform.Stop();

            stop.Register(this.stopManager, true);	// ����������

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial(strText);
            stop.BeginLoop();

            return stop;
        }

        void EndStop(DigitalPlatform.Stop stop)
        {
            if (stopManager == null || stop == null)
                return;

            stop.EndLoop();
            stop.OnStop -= new StopEventHandler(this.DoStop);
            stop.Initial("");

            stop.Unregister();	// ����������
        }


        void menu_changePassword(object sender, System.EventArgs e)
		{
			if (this.SelectedNode == null)
			{
				MessageBox.Show(this, "��δѡ������� ...");
				return;
			}
			ChangePasswordDlg dlg = new ChangePasswordDlg();
            dlg.Font = GuiUtil.GetDefaultFont();
            ResPath respath = new ResPath(this.SelectedNode);

			dlg.Channels = this.Channels;
			dlg.Url = respath.Url;
			if (Servers != null)
			{
				Server server = Servers[respath.Url];
				if (server != null)
					dlg.UserName = server.DefaultUserName;
			}
			dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.ShowDialog(this);
		}
		
		// �༭�����ļ�
		void menu_editCfgFile(object sender, System.EventArgs e)
		{
			if (this.SelectedNode == null)
			{
				MessageBox.Show(this, "��δѡ��Ҫ�༭�������ļ��ڵ�");
				return;
			}
			
			if (this.SelectedNode.ImageIndex != RESTYPE_FILE)
			{
				MessageBox.Show(this, "��ѡ��Ľڵ㲻�������ļ����͡���ѡ��Ҫ�༭�������ļ��ڵ㡣");
				return;
			}

			ResPath respath = new ResPath(this.SelectedNode);

			// �༭�����ļ�
			CfgFileEditDlg dlg = new CfgFileEditDlg();
            dlg.Font = GuiUtil.GetDefaultFont();

			dlg.Initial(this.Servers,
				this.Channels,
				this.stopManager,
				respath.Url,
				respath.Path);

			if (this.AppInfo != null)
				this.AppInfo.LinkFormState(dlg, "CfgFileEditDlg_state");
			dlg.ShowDialog(this);
			if (this.AppInfo != null)
				this.AppInfo.UnlinkFormState(dlg);

			/*
			if (dlg.DialogResult != DialogResult.OK)
				goto FINISH;
			*/




		}

		// ��ʼ�����ݿ�
		void menu_initialDB(object sender, System.EventArgs e)
		{
			if (this.SelectedNode == null)
			{
				MessageBox.Show(this, "��δѡ��Ҫ��ʼ�������ݿ�ڵ�");
				return;
			}
			
			if (this.SelectedNode.ImageIndex != RESTYPE_DB)
			{
				MessageBox.Show(this, "��ѡ��Ľڵ㲻�����ݿ����͡���ѡ��Ҫ��ʼ�������ݿ�ڵ㡣");
				return;
			}

			ResPath respath = new ResPath(this.SelectedNode);

			string strText = "��ȷʵҪ��ʼ��λ�ڷ����� '"+respath.Url+"' �ϵ����ݿ� '"+respath.Path + "' ��?\r\n\r\n���棺���ݿ�һ������ʼ�������а�����ԭ�����ݽ�ȫ�����ݻ٣������޷��ָ���";

			DialogResult msgResult = MessageBox.Show(this,
				strText,
				"��ʼ�����ݿ�",
				MessageBoxButtons.OKCancel,
				MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button2);
				
			if (msgResult != DialogResult.OK) 
			{
				MessageBox.Show(this, "��ʼ�����ݿ����������...");
				return;
			}


			this.channel = Channels.GetChannel(respath.Url);

			Debug.Assert(channel != null, "Channels.GetChannel() �쳣");


			string strError = "";

#if NO
			DigitalPlatform.Stop stop = null;

			if (stopManager != null) 
			{
				stop = new DigitalPlatform.Stop();

                stop.Register(this.stopManager, true);	// ����������

                stop.OnStop += new StopEventHandler(this.DoStop);
				stop.Initial("���ڳ�ʼ�����ݿ�: " + respath.FullPath);
				stop.BeginLoop();

			}
#endif
            DigitalPlatform.Stop stop = PrepareStop("���ڳ�ʼ�����ݿ�: " + respath.FullPath);

			long lRet = channel.DoInitialDB(respath.Path, out strError);

            EndStop(stop);
#if NO
			if (stopManager != null) 
			{
				stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
				stop.Initial("");

				stop.Unregister();	// �������������
			}
#endif

			this.channel = null;

			if (lRet == -1) 
			{
				MessageBox.Show(this, strError);
			}
			else 
			{
				MessageBox.Show(this, "λ�ڷ�����'"+respath.Url+"'�ϵ����ݿ� '"+respath.Path+"' ���ɹ���ʼ����");
			}
		}

        // ˢ�����ݿⶨ��(�����ԭ��keys)
        void menu_refreshDB(object sender, System.EventArgs e)
        {
            RefreshDB(false);
        }

        void RefreshDB(bool bClearAllKeyTables)
        {
            if (this.SelectedNode == null)
            {
                MessageBox.Show(this, "��δѡ��Ҫˢ�¶�������ݿ�ڵ�");
                return;
            }

            if (this.SelectedNode.ImageIndex != RESTYPE_DB)
            {
                MessageBox.Show(this, "��ѡ��Ľڵ㲻�����ݿ����͡���ѡ��Ҫˢ�¶�������ݿ�ڵ㡣");
                return;
            }

            ResPath respath = new ResPath(this.SelectedNode);

            string strText = "ȷʵҪˢ��λ�ڷ����� '" + respath.Url + "' �ϵ����ݿ� '" + respath.Path + "' �Ķ�����?\r\n\r\nע��ˢ�����ݿⶨ�壬��Ϊ���ݿ�������keys�����ļ���������SQL�����������ݿ������е����ݡ�";

            DialogResult msgResult = MessageBox.Show(this,
                strText,
                "ˢ�����ݿⶨ��",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (msgResult != DialogResult.OK)
            {
                MessageBox.Show(this, "ˢ�����ݿⶨ��Ĳ���������...");
                return;
            }


            this.channel = Channels.GetChannel(respath.Url);

            Debug.Assert(channel != null, "Channels.GetChannel() �쳣");


            string strError = "";

#if NO
            DigitalPlatform.Stop stop = null;

            if (stopManager != null)
            {
                stop = new DigitalPlatform.Stop();

                stop.Register(this.stopManager, true);	// ����������

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("����ˢ�����ݿⶨ��: " + respath.FullPath );
                stop.BeginLoop();

            }
#endif
            DigitalPlatform.Stop stop = PrepareStop("����ˢ�����ݿⶨ��: " + respath.FullPath);


            long lRet = channel.DoRefreshDB(
                "begin",
                respath.Path,
                bClearAllKeyTables,
                out strError);

            EndStop(stop);
#if NO
            if (stopManager != null)
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                stop.Unregister();	// �������������
            }
#endif

            this.channel = null;

            if (lRet == -1)
            {
                MessageBox.Show(this, strError);
            }
            else
            {
                MessageBox.Show(this, "λ�ڷ�����'" + respath.Url + "'�ϵ����ݿ� '" + respath.Path + "' ���ɹ�ˢ���˶��塣");
            }
        }


        void menu_nodeInfo(object sender, System.EventArgs e)
        {
            if (this.SelectedNode == null)
            {
                MessageBox.Show(this, "��δѡ��ڵ�");
                return;
            }

            string strText = "";

            ItemProperty prop = (ItemProperty)this.SelectedNode.Tag;
            if (prop != null)
            {
                string strTypeString = prop.TypeString;
                strText = "�ڵ���: " + this.SelectedNode.Text + "\r\n";
                if (String.IsNullOrEmpty(strTypeString) == false)
                    strText += "����: " + strTypeString;
                else
                    strText += "����: " + "(��)";
            }
            else
                strText = "ItemProperty == null";

            MessageBox.Show(this, strText);
        }

        void menu_clearCheckBoxes(object sender, System.EventArgs e)
        {
            this.ClearChildrenCheck(null);
        }

		void menu_toggleCheckBoxes(object sender, System.EventArgs e)
		{
			//ResPath OldPath = new ResPath(this.SelectedNode);


			if (this.CheckBoxes == true)
				this.CheckBoxes = false;
			else
				this.CheckBoxes = true;

			//ExpandPath(OldPath);
		}

		delegate int Delegate_Fill(TreeNode node);

		private void ResTree_AfterExpand(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			
			TreeNode node = e.Node;

			if (node == null)
				return;

			// ��Ҫչ��
			if (IsLoading(node) == true) 
			{
				//Fill(node);

				object[] pList = new object []  { node };

				this.BeginInvoke(new Delegate_Fill(this.Fill), pList);

			}
			
		}




		// ����·��,����һ��node��checked״̬
		public bool CheckNode(ResPath respath,
			bool bChecked)
		{
			TreeNode node = this.GetTreeNode(respath);
			if (node == null)
				return false;

			node.Checked = bChecked;
			return true;
		}

		// ����·���õ�node�ڵ����
		public TreeNode GetTreeNode(ResPath respath)
		{

			string[] aName = respath.Path.Split(new Char [] {'/'});

			TreeNode node = null;
			TreeNode nodeThis = null;


			string[] temp = new string[aName.Length + 1];
			Array.Copy(aName,0, temp, 1, aName.Length);
			temp[0] = respath.Url;

			aName = temp;

			for(int i=0;i<aName.Length;i++)
			{
				TreeNodeCollection nodes = null;

				if (node == null)
					nodes = this.Nodes;
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
					return null;

				node = nodeThis;

			}

			return nodeThis;
		}

        // ���滯����URI (ָ������query���ֵ���߲���)
        static string CanonicalizeAbsoluteUri(string s)
        {
            if (string.IsNullOrEmpty(s) == true)
                return s;

            s = s.ToLower();

            // ȷ�������һ��'/'�ַ�
            if (s[s.Length - 1] != '/')
                s += "/";

            return s;
        }

        // 2012/3/31
        // �ж�����URL�Ƿ��ͬ
        static bool IsUrlEqual(string s1, string s2)
        {
            try
            {
                Uri uri1 = new Uri(s1);
                Uri uri2 = new Uri(s2);

                if (CanonicalizeAbsoluteUri(uri1.AbsoluteUri) != CanonicalizeAbsoluteUri(uri2.AbsoluteUri))
                    return false;
                if (uri1.Query != uri2.Query)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

		// ����·����չ��
        // return:
        //      true    ��ĩһ���ҵ�
        //      false   û���ҵ�(������һ��û���ҵ�)
		public bool ExpandPath(ResPath respath)
		{
			string[] aName = respath.Path.Split(new Char [] {'/'});

			TreeNode node = null;
			TreeNode nodeThis = null;

			string[] temp = new string[aName.Length + 1];
			Array.Copy(aName,0, temp, 1, aName.Length);
			temp[0] = respath.Url;

			aName = temp;

            bool bBreak = false;    // �Ƿ���ĳ��û���ҵ����м��ж���?
			for(int i=0;i<aName.Length;i++)
			{
				TreeNodeCollection nodes = null;

				if (node == null)
					nodes = this.Nodes;
				else 
					nodes = node.Nodes;

				bool bFound = false;
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (aName[i] == nodes[j].Text
                        || (i == 0 && IsUrlEqual(aName[i], nodes[j].Text) == true)) // URL������Ҫ����ȽϷ��� 2012/3/31
                    {
                        bFound = true;
                        nodeThis = nodes[j];
                        break;
                    }
                }
                if (bFound == false)
                {
                    bBreak = true;
                    break;
                }

				node = nodeThis;

				// ��Ҫչ��
				if (IsLoading(node) == true) 
				{
					Fill(node);
				}
                node.Expand();  // 2006/1/20     �������ղ��û���ҵ���ҲҪչ���м���
			}

			if (nodeThis!= null && nodeThis.Parent != null)
				nodeThis.Parent.Expand();

			this.SelectedNode = nodeThis;

            // 2009/3/3 new add
            if (bBreak == true)
                return false;

            return true;
		}


        // ����·����check��ȥ��check���Ǽ�����˼�����ǹ�ѡ����˼
        // return:
        //      true    �ҵ�
        //      false   û���ҵ�(������һ��û���ҵ�)
        public bool CheckPath(ResPath respath)
        {
            Debug.Assert(this.CheckBoxes == true, "ֻ����CheckBoxes==true������µ���");

            string[] aName = respath.Path.Split(new Char[] { '/' });
            TreeNode node = null;
            TreeNode nodeThis = null;

            string[] temp = new string[aName.Length + 1];
            Array.Copy(aName, 0, temp, 1, aName.Length);
            // ��һ����Url
            temp[0] = respath.Url;

            aName = temp;

            bool bBreak = false;    // �Ƿ���ĳ��û���ҵ����м��ж���?
            for (int i = 0; i < aName.Length; i++)
            {
                TreeNodeCollection nodes = null;

                if (node == null)
                    nodes = this.Nodes;
                else
                    nodes = node.Nodes;

                bool bFound = false;
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (aName[i] == nodes[j].Text
                        || (i == 0 && IsUrlEqual(aName[i], nodes[j].Text) == true)) // URL������Ҫ����ȽϷ��� 2012/4/1
                    {
                        bFound = true;
                        nodeThis = nodes[j];
                        break;
                    }
                }
                if (bFound == false)
                {
                    bBreak = true;
                    break;
                }

                node = nodeThis;

                // ��Ҫչ��
                if (IsLoading(node) == true)
                {
                    Fill(node);
                }
                node.Expand();  // 2006/1/20     �������ղ��û���ҵ���ҲҪչ���м���
                node.Checked = true;
            }

            if (nodeThis != null && nodeThis.Parent != null)
                nodeThis.Parent.Expand();

            if (bBreak == true)
                return false;

            return true;
        }

		// ��һ�׶Ρ��������TargetItem��Url��Target����
		// �������ϵ�ѡ��״̬���ɼ���Ŀ���ַ���
		// ��ͬ�ķ������е��ַ����ֿ���
		public TargetItemCollection GetSearchTarget()
		{

			string strDb = "";

			TargetItemCollection aText = new TargetItemCollection();

			if (this.CheckBoxes == false) 
			{
				ArrayList aNode = new ArrayList();
				TreeNode node = this.SelectedNode;
				if (node == null)
					return aText;

				for(;node!=null;) 
				{
					aNode.Insert(0, node);
					node = node.Parent;
				}

				if (aNode.Count == 0)
					goto END1;


				TargetItem item = new TargetItem();
				item.Lang = this.Lang;

				aText.Add(item);

				item.Url = ((TreeNode)aNode[0]).Text;

				if (aNode.Count == 1)
					goto END1;

				item.Target = ((TreeNode)aNode[1]).Text;

				
				if (aNode.Count == 2)
					goto END1;

				item.Target += ":" + ((TreeNode)aNode[2]).Text;


				END1:
				return aText;
			}

			// ��ѡ�еķ�����
			foreach(TreeNode nodeServer in this.Nodes )
			{
				if (nodeServer.Checked == false)
					continue;

				// ��ѡ�е����ݿ�
				foreach(TreeNode nodeDb in nodeServer.Nodes )
				{
					if (nodeDb.Checked == false)
						continue;

                    if (nodeDb.ImageIndex != RESTYPE_DB)
                        continue;   // 2006/6/16 new add ��Ϊ�����������ļ�Ŀ¼�����ļ�������Ҫ����

					if (strDb != "")
						strDb += ";";
					strDb += nodeDb.Text + ":";

					//��һ��strFrom�±��������Ժܺõش�����
					string strFrom = "";
					//��ѡ�е�from
					foreach(TreeNode nodeFrom in nodeDb.Nodes )
					{
						if (nodeFrom.Checked == true)
						{
							if (strFrom != "")
								strFrom += ",";
							strFrom += nodeFrom.Text ;
						}
					}
					strDb += strFrom;
				}

				TargetItem item = new TargetItem();
				item.Url = nodeServer.Text;
				item.Target = strDb;
				item.Lang = this.Lang;

				aText.Add(item);

				strDb = "";
			}

			return aText;
		}

        // ��õ�ǰѡ�е�һ�������ɸ����ݿ�����ȫ·��
        public List<string> GetCheckedDatabaseList()
        {
            List<string> result = new List<string>();

            if (this.CheckBoxes == false)
            {
                TreeNode node = this.SelectedNode;
                if (node == null)
                    return result;

                if (node.ImageIndex != RESTYPE_DB)
                    return result;

                ResPath respath = new ResPath(node);

                result.Add(respath.FullPath);
                return result;
            }

            // ��ѡ�еķ�����
            foreach (TreeNode nodeServer in this.Nodes)
            {
                if (nodeServer.Checked == false)
                    continue;

                // ��ѡ�е����ݿ�
                foreach (TreeNode nodeDb in nodeServer.Nodes)
                {
                    if (nodeDb.Checked == false)
                        continue;

                    if (nodeDb.ImageIndex != RESTYPE_DB)
                        continue;

                    ResPath respath = new ResPath(nodeDb);

                    result.Add(respath.FullPath);
                }


            }

            return result;
        }

        // 2008/11/17 new add
        // ����·���б���ѡ�������ݿ�
        // parameters:
        //      paths   ·�������顣ÿ��·������̬��: http://localhost/dp2kernel?���ݿ���
        // return:
        //      false   Ҫѡ���Ŀ�겻����
        //      true    �Ѿ�ѡ��
        public bool SelectDatabases(List<string> paths,
            out string strError)
        {
            strError = "";

            if (paths.Count == 0)
            {
                if (this.CheckBoxes == true)
                    ClearChildrenCheck(null);
                this.SelectedNode = null;
                strError = "paths������û���κ�Ԫ��";
                return false;
            }

            if (paths.Count == 1)
            {
                this.CheckBoxes = false;
                this.SelectedNode = null;
                ResPath respath = new ResPath(paths[0]);

                // return:
                //      true    ��ĩһ���ҵ�
                //      false   û���ҵ�(������һ��û���ҵ�)
                bool bRet = ExpandPath(respath);
                if (this.SelectedNode == null)
                {
                    strError = "'" + paths[0] + "' �ķ������ڵ�����Դ����û���ҵ�";
                    return false;
                }

                if (bRet == false)
                {
                    strError = "'" + respath.FullPath + "' �����ݿ�ڵ�����Դ����û���ҵ�";
                    return false;
                }

                return true;
            }

            this.CheckBoxes = true;
            ClearChildrenCheck(null);
            this.SelectedNode = null;

            for (int i = 0; i < paths.Count; i++)
            {
                ResPath respath = new ResPath(paths[i]);

                bool bFound = CheckPath(respath);
                if (bFound == false)
                {
                    if (String.IsNullOrEmpty(strError) == false)
                        strError += "\r\n";
                    strError += "'" + respath.FullPath + "' ��ĳ������Դ����û���ҵ�";
                }
            }

            if (String.IsNullOrEmpty(strError) == true)
                return true;

            return false;
        }

        private void ResTree_AfterCheck(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			TreeNode node = e.Node;
			if (node == null)
				return;

            // 2008/11/17
            if (node.Checked == true)
            {
                node.ForeColor = SystemColors.InfoText;
                node.BackColor = SystemColors.Info;
            }
            else
            {
                node.ForeColor = SystemColors.WindowText;
                node.BackColor = SystemColors.Window;
            }

			if (node.Checked == false) 
			{
				ClearOneLevelChildrenCheck(node);
			}
			else 
			{
				if (node.Parent != null)
					node.Parent.Checked = true;
			}

			// ע���¼��Լ���ݹ�
		
		}

		// ����¼����е�ѡ�е���(�������Լ�)
		public void ClearOneLevelChildrenCheck(TreeNode nodeStart)
		{
			if (nodeStart == null)
				return;
			foreach(TreeNode node in nodeStart.Nodes )
			{
				node.Checked = false;
				// ClearChildrenCheck(node);	// ��ʱ���ݹ�
			}
		}

        // 2008/11/17 new add
        // ����¼����е�ѡ�е���(�������Լ�)
        // parameters:
        //      nodeStart   ���node�����Ϊnull, ��ʾ�Ӹ��㿪ʼ�����ȫ��
        public void ClearChildrenCheck(TreeNode nodeStart)
        {
            TreeNodeCollection nodes = null;
            if (nodeStart == null)
            {
                nodes = this.Nodes;
            }
            else
                nodes = nodeStart.Nodes;

            foreach (TreeNode node in nodes)
            {
                node.Checked = false;
                ClearChildrenCheck(node);	// �ݹ�
            }
        }

		private void ResTree_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			
			TreeNode curSelectedNode = this.GetNodeAt(e.X, e.Y);

			if (this.SelectedNode != curSelectedNode)
				this.SelectedNode = curSelectedNode;
			
		}

		/*
		// ѭ�������ΰѸ��׽ڵ�ѡ��(�������Լ�)
		public void CheckParent(TreeNode node)
		{
			node = node.Parent;
			while(true)
			{
				if (node == null)
					return;
				node.Checked = true;  //check�¼����������������Ա����ø���
				break;
				// node = node.Parent;
			}
		}
		*/

        public List<string> GetBrowseColumnNames(string strServerUrlOrName,
    string strDbName)
        {
            // �ҵ��������ڵ�
            TreeNode node_server = this.FindServer(strServerUrlOrName);
            if (node_server == null)
                return null;    // not found server

            string strError = "";
            // ��Ҫչ��
            if (IsLoading(node_server) == true)
            {
                int nRet = Fill(node_server, out strError);
                if (nRet == -1)
                {
                    throw new Exception(strError);
                }
            }

            // �ҵ����ݿ�ڵ�
            TreeNode node_db = FindDb(node_server, strDbName);
            if (node_db == null)
                return null;    // not found db

            DbProperty prop = (DbProperty)node_db.Tag;

            return prop.ColumnNames;
        }

        // �ҵ��������ڵ�
        TreeNode FindServer(string strServerUrlOrName)
        {
            for (int i = 0; i < this.Nodes.Count; i++)
            {
                TreeNode currrent_node = this.Nodes[i];

#if NO
                Debug.Assert(currrent_node.Tag is dp2ServerNodeInfo, "");
                dp2ServerNodeInfo info = (dp2ServerNodeInfo)currrent_node.Tag;

                if (info.Url == strServerUrlOrName
                    || info.Name == strServerUrlOrName)
                    return currrent_node;
#endif
                if (currrent_node.Text == strServerUrlOrName)
                    return currrent_node;
            }

            return null;
        }

        static TreeNode FindDb(TreeNode server_node, string strDbName)
        {
            Debug.Assert(server_node != null, "");
            for (int i = 0; i < server_node.Nodes.Count; i++)
            {
                TreeNode node = server_node.Nodes[i];
                if (node.Text == strDbName)
                    return node;
            }

            return null;
        }
	}

    public class ItemProperty
    {
        public string TypeString = "";
    }

    public class DbProperty : ItemProperty
    {
        public string DbName = "";
        public List<string> ColumnNames = new List<string>();
    }

	// ����Ŀ������
	public class TargetItem
	{
		public string Lang = "";
		public string Url = "";
		public string Target = "";	// ����Ŀ���ַ���������"��1:from1,from2;��2:from1,from2"

		public string Words = "";	// ԭʼ̬�ļ�����,��δ�и�
		public string[] aWord = null;	// MakeWordPhrases()�ӹ�����ַ���
		public string Xml = "";
		public int MaxCount = -1;	// �������������
	}

	// ����Ŀ������
	public class TargetItemCollection : ArrayList
	{

		// �ڶ��׶�: ����ÿ��TargetItem��Words��ԭʼ��̬�ļ����ʣ��и�Ϊstring[] aWord
		// ���ñ�����ǰ��Ӧ��Ϊÿ��TargetItem�������ú�Words��Աֵ
		// �ڶ��׶κ͵�һ�׶��Ⱥ�˳����Ҫ��
		public int MakeWordPhrases(
            bool bSplitWords,
            bool bAutoDetectRange,
			bool bAutoDetectRelation)
		{
			for(int i=0;i<this.Count;i++)
			{
				TargetItem item = (TargetItem)this[i];
				item.aWord = MakeWordPhrases(item.Words,
                    bSplitWords,
					bAutoDetectRange,
                    bAutoDetectRelation);
			}

			return 0;
		}


		// �����׶Σ�����ÿ��TargetItem�е�Target��aWord�������Xml����
		public int MakeXml()
		{
			string strText = "";
			for(int i=0;i<this.Count;i++)
			{
				TargetItem item = (TargetItem)this[i];

				strText = "";

				string strCount = "";

				if (item.MaxCount != -1)
					strCount = "<maxCount>" + Convert.ToString(item.MaxCount) + "</maxCount>";

				for(int j=0;j<item.aWord.Length;j++) 
				{
					if (j != 0) 
					{
						strText += "<operator value='OR' />";
					}

                    strText += "<item>" + item.aWord[j] + strCount + "</item>";
				}

				strText = "<target list='"
                    + StringUtil.GetXmlStringSimple(item.Target)       // 2007/9/14 new add
                    + "'>" + strText 
					+ "<lang>"+ item.Lang +"</lang></target>";

				item.Xml = strText;
			}

			return 0;
		}

        // ƥ����������
        static bool MatchTailQuote(char left, char right)
        {
            if (left == '��' && right == '��')
                return true;
            if (left == '��' && right == '��')
                return true;

            if (left == '\'' && right == '\'')
                return true;

            if (left == '"' && right == '"')
                return true;

            return false;
        }

        // ���տո��и��������
        static List<string> SplitWords(string strWords)
        {
            List<string> results = new List<string>();
            string strWord = "";
            bool bInQuote = false;
            char chQuote = '\'';
            for (int i = 0; i < strWords.Length; i++)
            {
                if ("\'\"��������".IndexOf(strWords[i]) != -1)
                {
                    if (bInQuote == true
                        && MatchTailQuote(chQuote, strWords[i]) == true)
                    {
                        bInQuote = false;
                        continue;   // �ڽ���к����������
                    }
                    else if (bInQuote == false)
                    {
                        bInQuote = true;
                        chQuote = strWords[i];
                        continue;   // �ڽ���к����������
                    }
                }

                if ( ( strWords[i] == ' ' || strWords[i] == '��')
                    && bInQuote == false
                    && String.IsNullOrEmpty(strWord) == false)
                {
                    results.Add(strWord);
                    strWord = "";
                }
                else
                {
                    strWord += strWords[i];
                }
            }

            if (String.IsNullOrEmpty(strWord) == false)
            {
                results.Add(strWord);
                strWord = "";
            }


            return results;
        }

        // ����һ���������ַ��������տհ��и�ɵ��������ʣ�
        // ���Ҹ��ݼ������Ƿ�Ϊ���ֵȵȲ²���������������������
        // ��<item>�ڲ���Ԫ�ص��ַ�����������Ȼ�������<target>��Ԫ�أ�
        // ���չ���������<item>�ַ���
        public static string[] MakeWordPhrases(string strWords,
            bool bSplitWords,
			bool bAutoDetectRange,
			bool bAutoDetectRelation)
		{
            /*
			string[] aWord;
			aWord = strWords.Split(new Char [] {' '});
             */
            List<string> aWord = null;
            
            if (bSplitWords == true)
                aWord = SplitWords(strWords);
	
			if (aWord == null || aWord.Count == 0) 
			{
				aWord = new List<string>();
				aWord.Add(strWords);
			}
	
			string strXml = "";
			string strWord = "";
			string strMatch = "";
			string strRelation = "";
			string strDataType = "";	

			ArrayList aResult = new ArrayList();

			foreach(string strOneWord in aWord)
			{
				/*
				strRelation = "";
				strDataType = "";	
				strWord = "";
				strMatch = "";
				*/


				if (bAutoDetectRange == true) 
				{
					string strID1;
					string strID2;

					QueryClient.SplitRangeID(strOneWord,out strID1, out strID2);
					if (StringUtil.IsNum(strID1)==true 
						&& StringUtil.IsNum(strID2) && strOneWord!="")
					{
						strWord = strOneWord;
						strMatch = "exact";
						strRelation = "range";  // 2012/3/29
						strDataType = "number";
						goto CONTINUE;
					}
				}


				if (bAutoDetectRelation == true)
				{
					string strOperatorTemp;
					string strRealText;
				
					int ret;
					ret = QueryClient.GetPartCondition(strOneWord,
						out strOperatorTemp,
						out strRealText);
				
					if (ret == 0 && strOneWord!="")
					{
						strWord = strRealText;
						strMatch = "exact";
						strRelation = strOperatorTemp;
						if(StringUtil.IsNum(strRealText) == true)
							strDataType = "number";					
						else
							strDataType = "string";
						goto CONTINUE;
					}
				}

					strWord = strOneWord;
					strMatch = "left";
					strRelation = "=";
					strDataType = "string";					


			
			CONTINUE:

                // 2007/4/5 ���� ������ GetXmlStringSimple()
				strXml += "<word>"
                    + StringUtil.GetXmlStringSimple(strWord)
                    + "</word>"
					+ "<match>"+ strMatch +	"</match>"
					+ "<relation>" + strRelation + "</relation>"
					+ "<dataType>" + strDataType + "</dataType>";

				aResult.Add(strXml);

				strXml = "";
			}

			return ConvertUtil.GetStringArray(0, aResult);
		}


	}

}
