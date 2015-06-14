using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// ���ڽ��й���Ŀؼ�
    /// ��ʾ�����ڣ�ÿ�ڵĶ��ض�����Ϣ(Ҳ���ǰ�����������Ϣ�Ķ�����Ϣ)
    /// </summary>
    internal partial class IssueManageControl : UserControl
    {
        public List<string> DeletingIds = new List<string>();

        public const int TYPE_RECIEVE_ZERO = 0; // һ��Ҳδ�յ�
        public const int TYPE_RECIEVE_NOT_COMPLETE = 1; // ��δ��ȫ 
        public const int TYPE_RECIEVE_COMPLETED = 2;    // �Ѿ���ȫ

        // ��ö�����Ϣ
        public event GetOrderInfoEventHandler GetOrderInfo = null;

        // ��ò���Ϣ
        // public event GetItemInfoEventHandler GetItemInfo = null;

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        /*
        // ����/ɾ��ʵ������
        public event GenerateEntityEventHandler GenerateEntity = null;
         * */

        TreeNode m_currentTreeNode = null;

        public IssueManageControl()
        {
            InitializeComponent();

            this.TreeView.ImageList = this.imageList_treeIcon;

            this.orderDesignControl1.ArriveMode = true;
            this.orderDesignControl1.SeriesMode = true;
            this.orderDesignControl1.Changed = false;

            this.orderDesignControl1.GetValueTable -= new GetValueTableEventHandler(orderDesignControl1_GetValueTable);
            this.orderDesignControl1.GetValueTable += new GetValueTableEventHandler(orderDesignControl1_GetValueTable);

            EanbleOrderDesignControl(false);
        }

        void orderDesignControl1_GetValueTable(object sender, GetValueTableEventArgs e)
        {
            if (this.GetValueTable != null)
                this.GetValueTable(sender, e);
        }

        void EanbleOrderDesignControl(bool bEnable)
        {
            if (bEnable == true)
            {
                this.orderDesignControl1.Enabled = true;
                this.orderDesignControl1.Visible = true;
                this.label_orderInfo_message.Visible = false;
            }
            else
            {
                this.orderDesignControl1.Clear();
                this.orderDesignControl1.Enabled = false;
                this.orderDesignControl1.Visible = false;
                this.label_orderInfo_message.Visible = true;
            }
        }

        public string OrderInfoMessage
        {
            get
            {
                return this.label_orderInfo_message.Text;
            }
            set
            {
                this.label_orderInfo_message.Text = value;
            }
        }

        // ��ȡֵ�б�ʱ��Ϊ���������ݿ���
        public string BiblioDbName
        {
            get
            {
                return this.orderDesignControl1.BiblioDbName;
            }
            set
            {
                this.orderDesignControl1.BiblioDbName = value;
            }
        }

        public void Clear()
        {
            this.TreeView.Nodes.Clear();
        }

        bool m_bChanged = false;

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                return this.m_bChanged;
            }
            set
            {
                this.m_bChanged = value;
            }
        }

        public List<IssueManageItem> Items
        {
            get
            {
                List<IssueManageItem> results = new List<IssueManageItem>();
                for (int i = 0; i < this.TreeView.Nodes.Count; i++)
                {
                    results.Add((IssueManageItem)this.TreeView.Nodes[i].Tag);
                }

                return results;
            }
        }

        public IssueManageItem AppendNewItem(string strXml,
            out string strError)
        {
            strError = "";

            IssueManageItem item = new IssueManageItem();

            /*
            item.Xml = strXml;
            item.dom = new XmlDocument();
            try
            {
                item.dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����: " + ex.Message;
                return null;
            }
             * */
            int nRet = item.Initial(strXml, out strError);
            if (nRet == -1)
                return null;


            // �������ڵ����
            TreeNode tree_node = new TreeNode();
            tree_node.ImageIndex = TYPE_RECIEVE_ZERO;
            tree_node.Tag = item;
            // ��ʾ����λ��Ϣ�����ڲ��
            item.SetNodeCaption(tree_node);

            this.TreeView.Nodes.Add(tree_node);

            return item;
        }

        // �����ܱ��һ�ڵ��ꡢ�����ںš����ںš���ŵ��ַ�������������ʾ���������ڴ洢
        public static string BuildVolumeDisplayString(
            string strPublishTime,
            string strIssue,
            string strZong,
            string strVolume)
        {
            string strResult = "";

            string strYear = "";
            // ȡ�����
            if (String.IsNullOrEmpty(strPublishTime) == true
                || strPublishTime.Length < 4)
                strYear = "????";
            else
            {
                strYear = strPublishTime.Substring(0,4);
            }

            strResult += strYear + ", no."
                + (String.IsNullOrEmpty(strIssue) == false ? strIssue : "?")
                + "";

            if (String.IsNullOrEmpty(strZong) == false)
            {
                if (strResult != "")
                    strResult += ", ";
                strResult += "��." + strZong;
            }


            if (String.IsNullOrEmpty(strVolume) == false)
            {
                if (strResult != "")
                    strResult += ", ";
                strResult += "v." + strVolume;
            }

            return strResult;
        }

        // �����ܱ��һ�ڵ��ꡢ�����ںš����ںš���ŵ��ַ�������������ʾ���������ڴ洢
        // ��װ��İ汾
        public static string BuildVolumeDisplayString(
            string strPublishTime,
            string strItemVolumeString)
        {
            if (String.IsNullOrEmpty(strPublishTime) == true
                && String.IsNullOrEmpty(strItemVolumeString) == true)
                return "";

            if (strPublishTime.IndexOf("-") != -1)
                return "[�϶�] " + strItemVolumeString; // �϶����volumestring�Ѿ���������ݣ�����Ҫ������֯

            string strIssue = "";
            string strZong = "";
            string strVolume = "";
            VolumeInfo.ParseItemVolumeString(strItemVolumeString,
                out strIssue,
                out strZong,
                out strVolume);

            return BuildVolumeDisplayString(
                strPublishTime,
                strIssue,
                strZong,
                strVolume);
        }


        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e == null)
            {
                // û��ѡ���κνڵ�
                EanbleOrderDesignControl(false);

                // �޸Ĺ�������ť״̬
                {
                    this.toolStripButton_delete.Enabled = false;
                    this.toolStripButton_modify.Enabled = false;
                    this.toolStripButton_moveUp.Enabled = false;
                    this.toolStripButton_moveDown.Enabled = false;
                }

                return;
            }


            string strError = "";
            // װ�����ݵ��ұ�

            // �����ǰTreeNode�ڵ����滹û�ж�����Ϣ������ⲿ��ȡ������ڻ�ȡһ�κ��и��Ѿ���ȡ�ı�־

            TreeNode tree_node = e.Node;

            // �޸Ĺ�������ť״̬
            {
                this.toolStripButton_delete.Enabled = true;
                this.toolStripButton_modify.Enabled = true;

                int index = this.TreeView.Nodes.IndexOf(tree_node);
                if (index == 0)
                    this.toolStripButton_moveUp.Enabled = false;
                else
                    this.toolStripButton_moveUp.Enabled = true;

                if (index >= this.TreeView.Nodes.Count - 1)
                    this.toolStripButton_moveDown.Enabled = false;
                else
                    this.toolStripButton_moveDown.Enabled = true;
            }



            IssueManageItem item = (IssueManageItem)tree_node.Tag;
            Debug.Assert(item != null, "");

            List<string> XmlRecords = new List<string>();
            XmlNodeList nodes = item.dom.DocumentElement.SelectNodes("orderInfo/*");

            if (nodes.Count > 0)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlRecords.Add(nodes[i].OuterXml);
                }
            }
            else if (this.GetOrderInfo != null)
            {
                GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
                e1.BiblioRecPath = "";
                e1.PublishTime = item.PublishTime;
                this.GetOrderInfo(this, e1);
                if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
                {
                    strError = "�ڻ�ȡ�����ڳ�������Ϊ '" + item.PublishTime + "' �Ķ�����Ϣ�Ĺ����з�������: " + e1.ErrorInfo;
                    goto ERROR1;
                }

                XmlRecords = e1.OrderXmls;

                if (XmlRecords.Count == 0)
                {
                    this.OrderInfoMessage = "�������� '" + item.PublishTime + "' û�ж�Ӧ�ĵĶ�����Ϣ";
                    EanbleOrderDesignControl(false);

                    item.OrderedCount = -1;
                    this.m_currentTreeNode = e.Node;
                    return;
                }
            }

            this.OrderInfoMessage = "";
            EanbleOrderDesignControl(true);

            // return:
            //      -1  error
            //      >=0 �������ܷ���
            int nRet = LoadOrderDesignItems(XmlRecords,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            item.OrderedCount = nRet;

            this.m_currentTreeNode = e.Node;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void TreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            UpdateTreeNodeInfo();
        }

        // ��ÿ��õ���󶩹�ʱ�䷶Χ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetMaxOrderRange(out string strStartDate,
            out string strEndDate,
            out string strError)
        {
            strStartDate = "";
            strEndDate = "";
            strError = "";

            if (this.GetOrderInfo == null)
                return 0;

            GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
            e1.BiblioRecPath = "";
            e1.PublishTime = "*";
            this.GetOrderInfo(this, e1);
            if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
            {
                strError = e1.ErrorInfo;
                return -1;
            }

            if (e1.OrderXmls.Count == 0)
                return 0;

            for (int i = 0; i < e1.OrderXmls.Count; i++)
            {
                string strXml = e1.OrderXmls[i];

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "����XMLװ��DOMʱ��������: " + ex.Message;
                    return -1;
                }

                string strRange = DomUtil.GetElementText(dom.DocumentElement,
                    "range");

                string strIssueCount = DomUtil.GetElementText(dom.DocumentElement,
                    "issueCount");

                int nIssueCount = 0;
                try
                {
                    nIssueCount = Convert.ToInt32(strIssueCount);
                }
                catch
                {
                    continue;
                }

                int nRet = strRange.IndexOf("-");
                if (nRet == -1)
                {
                    strError = "ʱ�䷶Χ '" + strRange + "' ��ʽ����ȱ��-";
                    return -1;
                }

                string strStart = strRange.Substring(0, nRet).Trim();
                string strEnd = strRange.Substring(nRet + 1).Trim();

                if (strStart.Length != 8)
                {
                    strError = "ʱ�䷶Χ '" + strRange + "' ��ʽ������߲����ַ�����Ϊ8";
                    return -1;
                }
                if (strEnd.Length != 8)
                {
                    strError = "ʱ�䷶Χ '" + strRange + "' ��ʽ�����ұ߲����ַ�����Ϊ8";
                    return -1;
                }

                if (strStartDate == "")
                    strStartDate = strStart;
                else
                {
                    if (String.Compare(strStartDate, strStart) > 0)
                        strStartDate = strStart;
                }

                if (strEndDate == "")
                    strEndDate = strEnd;
                else
                {
                    if (String.Compare(strEndDate, strEnd) < 0)
                        strEndDate = strEnd;
                }
            }

            if (strStartDate == "")
            {
                Debug.Assert(strEndDate == "", "");
                return 0;
            }

            return 1;
        }

        // ���һ������ʱ���Ƿ����Ѿ������ķ�Χ��
        bool InOrderRange(string strPublishTime)
        {
            if (this.GetOrderInfo == null)
                return false;

            GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
            e1.BiblioRecPath = "";
            e1.PublishTime = strPublishTime;
            this.GetOrderInfo(this, e1);
            if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
                return false;

            if (e1.OrderXmls.Count == 0)
                return false;

            return true;
        }

        // ���һ���ڵ�������
        // return:
        //      -1  ����
        //      0   �޷����
        //      1   ���
        int GetOneYearIssueCount(string strPublishYear,
            out int nValue,
            out string strError)
        {
            strError = "";
            nValue = 0;

            if (this.GetOrderInfo == null)
                return 0;   // �޷����

            GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
            e1.BiblioRecPath = "";
            e1.PublishTime = strPublishYear;
            this.GetOrderInfo(this, e1);
            if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
            {
                strError = "�ڻ�ȡ�����ڳ�������Ϊ '" + strPublishYear + "' �Ķ�����Ϣ�Ĺ����з�������: " + e1.ErrorInfo;
                return -1;
            }

            if (e1.OrderXmls.Count == 0)
                return 0;

            for (int i = 0; i < e1.OrderXmls.Count; i++)
            {
                string strXml = e1.OrderXmls[i];

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "XMLװ��DOMʱ��������: " + ex.Message;
                    return -1;
                }

                string strRange = DomUtil.GetElementText(dom.DocumentElement,
                    "range");

                string strIssueCount = DomUtil.GetElementText(dom.DocumentElement,
                    "issueCount");

                int nIssueCount = 0;
                try
                {
                    nIssueCount = Convert.ToInt32(strIssueCount);
                }
                catch
                {
                    continue;
                }

                float years = Global.Years(strRange);
                if (years != 0)
                {
                    nValue = Convert.ToInt32((float)nIssueCount * (1/years));
                }
            }

            return 1;
        }

        public void UpdateTreeNodeInfo()
        {
            if (this.orderDesignControl1.Changed == false)
                return;

            if (this.m_currentTreeNode == null)
                return;


            // �������뿪������޸Ĺ����ұ������
            TreeNode tree_node = this.m_currentTreeNode;
            IssueManageItem item = (IssueManageItem)tree_node.Tag;
            XmlNodeList nodes = item.dom.DocumentElement.SelectNodes("orderInfo/*");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                node.ParentNode.RemoveChild(node);
            }

            string strError = "";
            List<string> XmlRecords = null;
            // �����ұߵ�OrderDesignControl���ݹ���XML��¼
            int nRet = BuildOrderXmlRecords(
                out XmlRecords,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            XmlNode root = item.dom.DocumentElement.SelectSingleNode("orderInfo");
            if (root == null)
            {
                root = item.dom.CreateElement("orderInfo");
                item.dom.DocumentElement.AppendChild(root);
            }
            for (int i = 0; i < XmlRecords.Count; i++)
            {
                XmlDocumentFragment fragment = item.dom.CreateDocumentFragment();
                try
                {
                    fragment.InnerXml = XmlRecords[i];
                }
                catch (Exception ex)
                {
                    strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                    goto ERROR1;
                }

                root.AppendChild(fragment);
                this.Changed = true;
            }

            item.Changed = true;

            item.SetNodeCaption(tree_node); // ˢ�½ڵ���ʾ

            this.m_currentTreeNode = null;

            this.orderDesignControl1.Changed = false;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��������¼װ�ص��ұߵ�OrderDesignControl��
        // return:
        //      -1  error
        //      >=0 �������ܷ���
        int LoadOrderDesignItems(List<string> XmlRecords,
            out string strError)
        {
            strError = "";

            this.orderDesignControl1.DisableUpdate();

            try
            {

                this.orderDesignControl1.Clear();

                int nOrderedCount = 0;  // ˳�������������ܷ���
                for (int i = 0; i < XmlRecords.Count; i++)
                {
                    DigitalPlatform.CommonControl.Item item =
                        this.orderDesignControl1.AppendNewItem(XmlRecords[i],
                        out strError);
                    if (item == null)
                        return -1;

                    nOrderedCount += item.OldCopyValue;
                }

                this.orderDesignControl1.Changed = false;
                return nOrderedCount;

            }
            finally
            {
                this.orderDesignControl1.EnableUpdate();
            }

        }

        // �����ұߵ�OrderDesignControl���ݹ���XML��¼
        int BuildOrderXmlRecords(
            out List<string> XmlRecords,
            out string strError)
        {
            strError = "";
            XmlRecords = new List<string>();

            for (int i = 0; i < this.orderDesignControl1.Items.Count; i++)
            {
                DigitalPlatform.CommonControl.Item design_item = this.orderDesignControl1.Items[i];

                string strXml = "";
                int nRet = design_item.BuildXml(out strXml, out strError);
                if (nRet == -1)
                    return -1;

                XmlDocument dom = new XmlDocument();
                dom.LoadXml(strXml);

                /*
                DomUtil.SetElementText(dom.DocumentElement,
                    "parent", Global.GetID(this.BiblioRecPath));
                 * */

                /*
                if (design_item.NewlyAcceptedCount > 0)
                {
                    DomUtil.SetElementText(dom.DocumentElement,
                        "state", "������");
                }*/

                XmlRecords.Add(dom.DocumentElement.OuterXml);   // ��Ҫ����prolog
            }

            return 0;
        }

        private void TreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            TreeNode node = this.TreeView.SelectedNode;

            //
            menuItem = new MenuItem("�޸�(&M)");
            menuItem.DefaultItem = true;
            menuItem.Click += new System.EventHandler(this.button_modify_Click);
            if (node == null)
            {
                menuItem.Enabled = false;
            }
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("������(&N)");
            menuItem.Click += new System.EventHandler(this.button_newIssue_Click);
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("��ȫ����(&A)");
            menuItem.Click += new System.EventHandler(this.button_newAllIssue_Click);
            contextMenu.MenuItems.Add(menuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            /*
            // 
            menuItem = new MenuItem("����(&U)");
            menuItem.Click += new System.EventHandler(this.button_up_Click);
            if (this.TreeView.SelectedNode == null
                || this.TreeView.SelectedNode.PrevNode == null)
                menuItem.Enabled = false;
            else
                menuItem.Enabled = true;
            contextMenu.MenuItems.Add(menuItem);



            // 
            menuItem = new MenuItem("����(&D)");
            menuItem.Click += new System.EventHandler(this.button_down_Click);
            if (this.TreeView.SelectedNode == null
                || this.TreeView.SelectedNode.NextNode == null)
                menuItem.Enabled = false;
            else
                menuItem.Enabled = true;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);
             * */

            //
            menuItem = new MenuItem("ȫ��ɾ��");
            menuItem.Click += new System.EventHandler(this.button_deleteAll_Click);
            if (this.TreeView.Nodes.Count == 0)
            {
                menuItem.Enabled = false;
            }
            contextMenu.MenuItems.Add(menuItem);


            //
            menuItem = new MenuItem("ɾ��(&E)");
            menuItem.Click += new System.EventHandler(this.button_delete_Click);
            if (node == null)
            {
                menuItem.Enabled = false;
            }
            contextMenu.MenuItems.Add(menuItem);

            /*
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
            if (bHasClipboardObject == false)
            {
                menuItem.Enabled = false;
            }
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ճ����ԭĿ¼ '" + GetClipboardProjectDir() + "' (&O)");
            menuItem.Click += new System.EventHandler(this.button_PasteFromClipboardToOriginDir_Click);

            if (bHasClipboardObject == false)
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


            */

            contextMenu.Show(TreeView, new Point(e.X, e.Y));		
        }

        // ɾ��ȫ���ڽڵ�
        void button_deleteAll_Click(object sender, System.EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.TreeView.Nodes.Count == 0)
            {
                strError = "û���κ��ڽڵ�ɹ�ɾ��";
                goto ERROR1;
            }

            string strText = "ȷʵҪɾ��ȫ�� "+this.TreeView.Nodes.Count.ToString()+" ���ڽڵ� ?";
            DialogResult result = MessageBox.Show(this,
                strText,
                "IssueManageControl",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result == DialogResult.No)
                return;


            for (int i = this.TreeView.Nodes.Count - 1; i >= 0; i--)
            {
                TreeNode tree_node = this.TreeView.Nodes[i];

                IssueManageItem item = (IssueManageItem)tree_node.Tag;

                List<string> ids = null;
                // ��ò�ο�ID�б�
                nRet = item.GetItemRefIDs(out ids,
                    out strError);
                if (nRet == -1)
                {
                    strError = "���������������refidʱ����: " + strError;
                    goto ERROR1;
                }

                this.TreeView.Nodes.Remove(tree_node);

                // ɾ���ڽڵ�󣬴������ڱ��ڵ����в�����refid
                if (ids.Count > 0)
                {
                    // Debug.Assert(this.GenerateEntity != null, "");

                    this.DeletingIds.AddRange(ids);
                }
            }

            this.m_currentTreeNode = null;
            this.orderDesignControl1.Clear();

            this.Changed = true;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }


        // ɾ���ڽڵ�
        // TODO: �Ѿ���ȫ���յ��ڽڵ㣬ɾ����ʱ��Ҫ���ء��ȷ�˵�����˽��ģ�
        void button_delete_Click(object sender, System.EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ��ǰ��ѡ���node
            if (this.TreeView.SelectedNode == null)
            {
                strError = "��δѡ��Ҫɾ�����ڽڵ�";
                goto ERROR1;
            }

            IssueManageItem item = (IssueManageItem)this.TreeView.SelectedNode.Tag;

            List<string> ids = null;
            // ��ò�ο�ID�б�
            nRet = item.GetItemRefIDs(out ids,
                out strError);
            if (nRet == -1)
            {
                strError = "���������������refidʱ����: " + strError;
                goto ERROR1;
            }

            string strText = "ȷʵҪɾ���ڽڵ� '"+this.TreeView.SelectedNode.Text+"' ";

            if (ids.Count > 0)
                strText += "�������� " + ids.Count.ToString() + " ���Ѽǵ��Ĳ�����";
            
            strText += "?";

            DialogResult result = MessageBox.Show(this,
                strText,
                "IssueManageControl",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result == DialogResult.No)
                return;

            if (this.m_currentTreeNode == this.TreeView.SelectedNode)
            {
                this.m_currentTreeNode = null;
            }

            this.TreeView.Nodes.Remove(this.TreeView.SelectedNode);

            if (this.m_currentTreeNode == null)
            {
                this.orderDesignControl1.Clear();
            }

            this.Changed = true;

#if NOOOOOOOOOOOO
            // TODO: ɾ���ڽڵ��Ҫע��ɾ�����ڱ��ڵ����в�����(���ɾ��)
            if (ids.Count > 0)
            {
                Debug.Assert(this.GenerateEntity != null, "");

                List<string> deleted_ids = null;
                nRet = DeleteItemRecords(ids, 
                    out deleted_ids,
                    out strError);
                if (nRet == -1)
                {
                    // TODO: ���ڽڵ����Ѿ��ɹ�ɾ����refidȥ����Ȼ���ٱ���
                    goto ERROR1;
                }

                // ע����Ȼɾ�����̷����˴��󣬵�������ɾ����������maskdelete�����Բ����߿��Ե����ᡱҳȥ�ֶ�undo maskdelete�������Ҫ
            }
#endif
            // ɾ���ڽڵ�󣬴������ڱ��ڵ����в�����refid
            if (ids.Count > 0)
            {
                // Debug.Assert(this.GenerateEntity != null, "");

                this.DeletingIds.AddRange(ids);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

#if NOOOOOOOOOOOOOOOOOOOOOOOO
        // parameters:
        //      deleted_ids �Ѿ��ɹ�ɾ����id
        int DeleteItemRecords(List<string> ids,
            out List<string> deleted_ids,
            out string strError)
        {
            strError = "";
            deleted_ids = new List<string>();

            Debug.Assert(this.GenerateEntity != null, "");

            GenerateEntityEventArgs data_container = new GenerateEntityEventArgs();
            // data_container.InputItemBarcode = this.InputItemsBarcode;
            data_container.SeriesMode = true;

            for (int i = 0; i < ids.Count; i++)
            {
                GenerateEntityData e = new GenerateEntityData();

                e.Action = "delete";
                e.RefID = ids[i];
                e.Xml = "";

                data_container.DataList.Add(e);
            }

            if (data_container.DataList != null
    && data_container.DataList.Count > 0)
            {
                // �����ⲿ�ҽӵ��¼�
                this.GenerateEntity(this, data_container);
                string strErrorText = "";

                if (String.IsNullOrEmpty(data_container.ErrorInfo) == false)
                {
                    strError = data_container.ErrorInfo;
                    return -1;
                }

                for (int i = 0; i < data_container.DataList.Count; i++)
                {
                    GenerateEntityData data = data_container.DataList[i];
                    if (String.IsNullOrEmpty(data.ErrorInfo) == false)
                    {
                        strErrorText += data.ErrorInfo;
                    }
                    else
                        deleted_ids.Add(data.RefID);
                }

                if (String.IsNullOrEmpty(strErrorText) == false)
                {
                    strError = strErrorText;
                    return -1;
                }
            }

            return 0;
        }
#endif

        private void button_up_Click(object sender, System.EventArgs e)
        {
            MoveUpDown(true);
        }

        private void button_down_Click(object sender, System.EventArgs e)
        {
            MoveUpDown(false);
        }

        bool MoveUpDown(bool bUp)
        {
            string strError = "";

            // ��ǰ��ѡ���node
            if (this.TreeView.SelectedNode == null)
            {
                strError = "��δѡ��Ҫ�ƶ������ڵ�";
                goto ERROR1;
            }

            TreeNode tree_node = this.TreeView.SelectedNode;

            int index = this.TreeView.Nodes.IndexOf(tree_node);

            if (bUp == true)
            {
                if (index == 0)
                {
                    strError = "�Ѿ���ͷ";
                    goto ERROR1;
                }
            }
            else
            {
                if (index >= this.TreeView.Nodes.Count - 1)
                {
                    strError = "�Ѿ���β";
                    goto ERROR1;
                }
            }

            // �Ƴ�
            this.TreeView.Nodes.Remove(tree_node);

            // �����ȥ
            if (bUp == true)
            {
                this.TreeView.Nodes.Insert(index - 1, tree_node);
                this.Changed = true;
            }
            else
            {
                this.TreeView.Nodes.Insert(index + 1, tree_node);
                this.Changed = true;
            }

            // ѡ���������ƶ��Ľڵ�
            this.TreeView.SelectedNode = tree_node;

            return true;    // �������ƶ�
        ERROR1:
            MessageBox.Show(this, strError);
            return false;   // û���ƶ�
        }

        private void TreeView_MouseDown(object sender, MouseEventArgs e)
        {
            TreeNode curSelectedNode = this.TreeView.GetNodeAt(e.X, e.Y);

            if (TreeView.SelectedNode != curSelectedNode)
            {
                UpdateTreeNodeInfo();   // 2009/1/6

                TreeView.SelectedNode = curSelectedNode;

                if (TreeView.SelectedNode == null)
                    TreeView_AfterSelect(null, null);	// ����
            }

        }

        // �޸�����Ϣ
        void button_modify_Click(object sender, EventArgs e)
        {
            string strError = "";
            if (this.TreeView.SelectedNode == null)
            {
                strError = "��δѡ��Ҫ�޸ĵ��ڽڵ�";
                goto ERROR1;
            }

            IssueManageItem item = (IssueManageItem)this.TreeView.SelectedNode.Tag;

            IssueDialog dlg = new IssueDialog();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.PublishTime = item.PublishTime;
            dlg.Issue = item.Issue;
            dlg.Zong = item.Zong;
            dlg.Volume = item.Volume;
            dlg.Comment = item.Comment;

            dlg.StartPosition = FormStartPosition.CenterScreen;

            REDO_INPUT:
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            TreeNode dup_tree_node = null;
            // �Գ���ʱ����в���
            // parameters:
            //      exclude �����Ҫ�ų���TreeNode����
            // return:
            //      -1  error
            //      0   û����
            //      1   ��
            int nRet = CheckPublishTimeDup(dlg.PublishTime,
                this.TreeView.SelectedNode,
                out dup_tree_node,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 1)
            {
                // ѡ�����ظ���TreeNode�ڵ㣬���ڲ����߹۲��ظ������
                Debug.Assert(dup_tree_node != null, "");
                if (dup_tree_node != null)
                    this.TreeView.SelectedNode = dup_tree_node;

                MessageBox.Show(this, "�޸ĺ���ڽڵ� " + strError + "\r\n���޸ġ�");

                goto REDO_INPUT;
            }


            item.PublishTime = dlg.PublishTime;
            item.Issue = dlg.Issue;
            item.Zong = dlg.Zong;
            item.Volume = dlg.Volume;
            item.Comment = dlg.Comment;

            item.SetNodeCaption(this.TreeView.SelectedNode);

            item.Changed = true;

            this.Changed = true;

            // TODO: �޸ĳ���ʱ���Ҫע���޸����ڱ��ڵ����в�ĳ���ʱ���ֶ�����
            // Ϊ�˱���ͱ��ɾ���Ĳ��������ͻ�������޸�ǰ��Ҫ������δ�ύ�Ĳ��޸��ȱ����ύ?

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        static string IncreaseNumber(string strNumber)
        {
            int v = 0;
            try
            {
                v = Convert.ToInt32(strNumber);
            }
            catch
            {
                return strNumber;   // ����ʧ��
            }
            return (v+1).ToString();
        }

        // Ԥ����һ�ڵĳ���ʱ��
        // exception:
        //      ������strPublishTimeΪ�����ܵ����ڶ��׳��쳣
        // parameters:
        //      strPublishTime  ��ǰ��һ�ڳ���ʱ��
        //      nIssueCount һ���ڳ�������
        static string NextPublishTime(string strPublishTime,
            int nIssueCount)
        {
            DateTime now = DateTimeUtil.Long8ToDateTime(strPublishTime);

            // һ��һ��
            if (nIssueCount == 1)
            {
                return DateTimeUtil.DateTimeToString8(DateTimeUtil.NextYear(now));
            }

            // һ������
            if (nIssueCount == 2)
            {
                // 6�����Ժ��ͬ��
                for (int i = 0; i < 6; i++)
                {
                    now = DateTimeUtil.NextMonth(now);
                }

                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ������
            if (nIssueCount == 3)
            {
                // 4�����Ժ��ͬ��
                for (int i = 0; i < 4; i++)
                {
                    now = DateTimeUtil.NextMonth(now);
                }

                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��4��
            if (nIssueCount == 4)
            {
                // 3�����Ժ��ͬ��
                for (int i = 0; i < 3; i++)
                {
                    now = DateTimeUtil.NextMonth(now);
                }

                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��5�� ��һ��6�ڴ���취һ��
            // һ��6��
            if (nIssueCount == 5 || nIssueCount == 6)
            {
                // 
                // 2�����Ժ��ͬ��
                for (int i = 0; i < 2; i++)
                {
                    now = DateTimeUtil.NextMonth(now);
                }

                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��7/8/9/10/11�� ��һ��12�ڴ���취һ��
            // һ��12��
            if (nIssueCount >= 7 && nIssueCount <= 12)
            {
                // 1�����Ժ��ͬ��
                now = DateTimeUtil.NextMonth(now);

                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��24��
            if (nIssueCount == 24)
            {
                // 15���Ժ�
                now += new TimeSpan(15,0,0,0);
                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��36��
            if (nIssueCount == 36)
            {
                // 10���Ժ�
                now += new TimeSpan(10, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��48��
            if (nIssueCount == 48)
            {
                // 7���Ժ�
                now += new TimeSpan(7, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��52��
            if (nIssueCount == 52)
            {
                // 7���Ժ�
                now += new TimeSpan(7, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��365��
            if (nIssueCount == 365)
            {
                // 1���Ժ�
                now += new TimeSpan(1, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(now);
            }

            return "????????";  // �޷����������
        }

        /*
        // ���ݳ������ڲ��ҵ�һ��ƥ���TreeNode�ڵ�
        TreeNode FindTreeNode(string strPublishTime)
        {
            for (int i = 0; i < this.TreeView.Nodes.Count; i++)
            {
                TreeNode tree_node = this.TreeView.Nodes[i];

                IssueManageItem item = (IssueManageItem)tree_node.Tag;
                Debug.Assert(item != null, "");

                if (item.PublishTime == strPublishTime)
                    return tree_node;
            }

            return null;
        }
        */

        // ��ȫ����(���)���ӵ�ǰ��ĩβ��һ���ڿ�ʼ������������ֱ����������ʱ�䷶Χ
        void button_newAllIssue_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            int nCreateCount = 0;

            // �ҵ����һ�ڡ�����Ҳ��������ȳ��ֶԻ���ѯ�ʵ�һ��
            if (this.TreeView.Nodes.Count == 0)
            {
                string strStartDate = "";
                string strEndDate = "";
                // ��ÿ��õ���󶩹�ʱ�䷶Χ
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = GetMaxOrderRange(out strStartDate,
                    out strEndDate,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    strError = "��ǰû�ж�����Ϣ���޷�������ȫ����";
                    goto ERROR1;
                }


                // ���ֶԻ����������һ�ڵĲ���������ʱ��������Զ�̽����Ƽ�
                // ����Ҫ���ճ���������Ϣ���Ѿ���ȫ�Ķ�����¼����ա����������ְ�ԭ��������չ��ĵ�һ�ڳ���ʱ���Ƽ����������
                // ��ν���(������Ϣ��)�����������ɹ���װ������������

                IssueDialog dlg = new IssueDialog();
                MainForm.SetControlFont(dlg, this.Font, false);

                dlg.Text = "��ָ�����ڵ�����";
                dlg.PublishTime = strStartDate + "?";   // ��ö�����Χ���������
                dlg.EditComment = "��ǰ����ʱ�䷶ΧΪ " + strStartDate + "-" + strEndDate;   // ��ʾ���õĶ���ʱ�䷶Χ
                dlg.StartPosition = FormStartPosition.CenterScreen;

            REDO_INPUT:
                dlg.ShowDialog(this);

                if (dlg.DialogResult != DialogResult.OK)
                    return; // ������������

                // ���һ���������ʱ���Ƿ񳬹�����ʱ�䷶Χ?
                if (InOrderRange(dlg.PublishTime) == false)
                {
                    MessageBox.Show(this, "��ָ�������ڳ���ʱ�� '" + dlg.PublishTime + "' ���ڵ�ǰ����ʱ�䷶Χ�ڣ����������롣");
                    goto REDO_INPUT;
                }



                IssueManageItem new_item = new IssueManageItem();
                nRet = new_item.Initial("<root />", out strError);
                if (nRet == -1)
                    goto ERROR1;

                new_item.PublishTime = dlg.PublishTime;
                new_item.Issue = dlg.Issue;
                new_item.Zong = dlg.Zong;
                new_item.Volume = dlg.Volume;
                new_item.Comment = dlg.Comment;

                TreeNode tree_node = new TreeNode();
                tree_node.ImageIndex = TYPE_RECIEVE_ZERO;
                tree_node.Tag = new_item;
                // ��ʾ����λ��Ϣ�����ڲ��
                new_item.SetNodeCaption(tree_node);

                int index = 0;
                if (this.TreeView.SelectedNode != null)
                    index = this.TreeView.Nodes.IndexOf(this.TreeView.SelectedNode) + 1;

                this.TreeView.Nodes.Insert(index, tree_node);
                nCreateCount++;

                new_item.Changed = true;
                this.Changed = true;

                // ѡ���²���Ľڵ�
                this.TreeView.SelectedNode = tree_node;

            }
            else
            {
                // ѡ�����һ��TreeNode
                Debug.Assert(this.TreeView.Nodes.Count != 0, "");
                TreeNode last_tree_node = this.TreeView.Nodes[this.TreeView.Nodes.Count - 1];

                if (this.TreeView.SelectedNode != last_tree_node)
                    this.TreeView.SelectedNode = last_tree_node;
            }

            Debug.Assert(this.TreeView.SelectedNode != null, "");
            TreeNode tail_node = this.TreeView.SelectedNode;
            // int nWarningCount = 0;

            // ����ѭ��������ȫ���ڵ�
            for (int i=0;  ;i++ )
            {
                Debug.Assert(this.TreeView.SelectedNode != null, "");

                IssueManageItem ref_item = (IssueManageItem)tail_node.Tag;

                string strNextPublishTime = "";
                string strNextIssue = "";
                string strNextZong = "";
                string strNextVolume = "";

                {
                    int nIssueCount = 0;
                    // ���һ���ڵ�������
                    // return:
                    //      -1  ����
                    //      0   �޷����
                    //      1   ���
                    nRet = GetOneYearIssueCount(ref_item.PublishTime,
                        out nIssueCount,
                        out strError);

                    int nRefIssue = 0;
                    try
                    {
                        nRefIssue = Convert.ToInt32(ref_item.Issue);
                    }
                    catch
                    {
                        nRefIssue = 0;
                    }


                    try
                    {
                        // Ԥ����һ�ڵĳ���ʱ��
                        // parameters:
                        //      strPublishTime  ��ǰ��һ�ڳ���ʱ��
                        //      nIssueCount һ���ڳ�������
                        strNextPublishTime = NextPublishTime(ref_item.PublishTime,
                             nIssueCount);
                    }
                    catch (Exception ex)
                    {
                        // 2009/2/8
                        strError = "�ڻ������ '" + ref_item.PublishTime + "' �ĺ�һ�ڳ�������ʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    if (strNextPublishTime == "????????")
                        break;

                    // ���һ���������ʱ���Ƿ񳬹�����ʱ�䷶Χ?
                    if (InOrderRange(strNextPublishTime) == false)
                        break;  // �����������һ��


                    // �����Զ�������Ҫ֪��һ�����Ƿ���꣬����ͨ����ѯ�ɹ���Ϣ�õ�һ�������ĵ�����
                    if (nRefIssue >= nIssueCount
                        && nIssueCount > 0) // 2010/3/3
                    {
                        // ������
                        strNextIssue = "1";
                    }
                    else
                    {
                        strNextIssue = (nRefIssue + 1).ToString();
                    }

                    strNextZong = IncreaseNumber(ref_item.Zong);
                    if (nRefIssue >= nIssueCount && nIssueCount > 0)
                        strNextVolume = IncreaseNumber(ref_item.Volume);
                    else
                        strNextVolume = ref_item.Volume;

                }

                // ��publishTimeҪ���أ��Ժ�����ϵҪ���м����������
                TreeNode dup_tree_node = null;
                // �Գ���ʱ����в���
                // parameters:
                //      exclude �����Ҫ�ų���TreeNode����
                // return:
                //      -1  error
                //      0   û����
                //      1   ��
                nRet = CheckPublishTimeDup(strNextPublishTime,
                    null,
                    out dup_tree_node,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 1)
                {
                    //this.TreeView.SelectedNode = dup_tree_node;
                    tail_node = dup_tree_node;

                    MessageBox.Show(this, "����ʱ��Ϊ '" + strNextPublishTime + "' ���ڽڵ��Ѿ������ˡ���λ�ý���������ĩβ");

                    // ���ظ��ڵ��ƶ������λ��
                    this.TreeView.Nodes.Remove(dup_tree_node);
                    this.TreeView.Nodes.Add(dup_tree_node);

                    // this.TreeView.SelectedNode = dup_tree_node; // ��û����һ���������ѭ��
                    tail_node = dup_tree_node;
                    continue;
                }

                IssueManageItem new_item = new IssueManageItem();
                nRet = new_item.Initial("<root />", out strError);
                if (nRet == -1)
                    goto ERROR1;

                new_item.PublishTime = strNextPublishTime;
                new_item.Issue = strNextIssue;
                new_item.Zong = strNextZong;
                new_item.Volume = strNextVolume;

                TreeNode tree_node = new TreeNode();
                tree_node.ImageIndex = TYPE_RECIEVE_ZERO;
                tree_node.Tag = new_item;
                // ��ʾ����λ��Ϣ�����ڲ��
                new_item.SetNodeCaption(tree_node);

                int index = 0;
                /*
                if (this.TreeView.SelectedNode != null)
                    index = this.TreeView.Nodes.IndexOf(this.TreeView.SelectedNode) + 1;
                 * */
                if (tail_node != null)
                    index = this.TreeView.Nodes.IndexOf(tail_node) + 1;

                this.TreeView.Nodes.Insert(index, tree_node);
                nCreateCount++;

                new_item.Changed = true;
                this.Changed = true;

                /*
                // ѡ���²���Ľڵ�
                this.TreeView.SelectedNode = tree_node;
                 * */
                tail_node = tree_node;
            }

          
            if (tail_node != null)
            {
                // ѡ���²���Ľڵ�
                this.TreeView.SelectedNode = tail_node;
            }

            string strMessage = "";
            if (nCreateCount == 0)
                strMessage = "û�������µ��ڽڵ�";
            else
                strMessage = "�������� " + nCreateCount.ToString() + " ���ڽڵ�";

            MessageBox.Show(this, strMessage);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ������(���)
        void button_newIssue_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            IssueManageItem ref_item = null;
            
            if (this.TreeView.SelectedNode != null)
                ref_item = (IssueManageItem)this.TreeView.SelectedNode.Tag;

            IssueDialog dlg = new IssueDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            if (ref_item != null)
            {
                // TODO: ������Զ�����

                int nIssueCount = 0;
                // ���һ���ڵ�������
                // return:
                //      -1  ����
                //      0   �޷����
                //      1   ���
                nRet = GetOneYearIssueCount(ref_item.PublishTime,
                    out nIssueCount,
                    out strError);

                int nRefIssue = 0;
                try
                {
                    nRefIssue = Convert.ToInt32(ref_item.Issue);
                }
                catch
                {
                    nRefIssue = 0;
                }


                string strNextPublishTime = "";

                try
                {
                    // Ԥ����һ�ڵĳ���ʱ��
                    // parameters:
                    //      strPublishTime  ��ǰ��һ�ڳ���ʱ��
                    //      nIssueCount һ���ڳ�������
                    strNextPublishTime = NextPublishTime(ref_item.PublishTime,
                         nIssueCount);
                }
                catch (Exception ex)
                {
                    // 2009/2/8
                    strError = "�ڻ������ '" + ref_item.PublishTime + "' �ĺ�һ�ڳ�������ʱ��������: " + ex.Message;
                    goto ERROR1;
                }

                dlg.PublishTime = strNextPublishTime;

                // �����Զ�������Ҫ֪��һ�����Ƿ���꣬����ͨ����ѯ�ɹ���Ϣ�õ�һ�������ĵ�����
                if (nRefIssue >= nIssueCount
                    && nIssueCount > 0) // 2010/3/3
                {
                    // ������
                    dlg.Issue = "1";
                }
                else
                {
                    dlg.Issue = (nRefIssue+1).ToString();
                }

                dlg.Zong = IncreaseNumber(ref_item.Zong);
                if (nRefIssue >= nIssueCount && nIssueCount > 0)
                    dlg.Volume = IncreaseNumber(ref_item.Volume);
                else
                    dlg.Volume = ref_item.Volume;

                if (nIssueCount > 0)
                    dlg.EditComment = "һ����� " + nIssueCount.ToString() + " ��";
            }

            dlg.StartPosition = FormStartPosition.CenterScreen;

            REDO_INPUT:
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            // ��publishTimeҪ���أ��Ժ�����ϵҪ���м����������
            TreeNode dup_tree_node = null;
            // �Գ���ʱ����в���
            // parameters:
            //      exclude �����Ҫ�ų���TreeNode����
            // return:
            //      -1  error
            //      0   û����
            //      1   ��
            nRet = CheckPublishTimeDup(dlg.PublishTime,
                null,
                out dup_tree_node,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 1)
            {
                // ѡ�����ظ���TreeNode�ڵ㣬���ڲ����߹۲��ظ������
                Debug.Assert(dup_tree_node != null, "");
                if (dup_tree_node != null)
                    this.TreeView.SelectedNode = dup_tree_node;

                MessageBox.Show(this, "���������ڽڵ� " + strError + "\r\n���޸ġ�");

                goto REDO_INPUT;
            }

            IssueManageItem new_item = new IssueManageItem();
            nRet = new_item.Initial("<root />", out strError);
            if (nRet == -1)
                goto ERROR1;

            new_item.PublishTime = dlg.PublishTime;
            new_item.Issue = dlg.Issue;
            new_item.Zong = dlg.Zong;
            new_item.Volume = dlg.Volume;
            new_item.Comment = dlg.Comment;

            TreeNode tree_node = new TreeNode();
            tree_node.ImageIndex = TYPE_RECIEVE_ZERO;
            tree_node.Tag = new_item;
            // ��ʾ����λ��Ϣ�����ڲ��
            new_item.SetNodeCaption(tree_node);

            int index = 0;
            if (this.TreeView.SelectedNode != null)
                index = this.TreeView.Nodes.IndexOf(this.TreeView.SelectedNode) + 1;

            this.TreeView.Nodes.Insert(index, tree_node);

            new_item.Changed = true;
            this.Changed = true;

            // ѡ���²���Ľڵ�
            this.TreeView.SelectedNode = tree_node;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �Գ���ʱ����в���
        // parameters:
        //      exclude �����Ҫ�ų���TreeNode����
        // return:
        //      -1  error
        //      0   û����
        //      1   ��
        int CheckPublishTimeDup(string strPublishTime,
            TreeNode exclude,
            out TreeNode dup_tree_node,
            out string strError)
        {
            strError = "";
            dup_tree_node = null;

            for (int i = 0; i < this.TreeView.Nodes.Count; i++)
            {
                TreeNode tree_node = this.TreeView.Nodes[i];

                if (tree_node == exclude)
                    continue;

                IssueManageItem item = (IssueManageItem)tree_node.Tag;

                if (item.PublishTime == strPublishTime)
                {
                    strError = "����ʱ�� '" + strPublishTime + "' ��λ�� " + (i+1).ToString() + " �ڵ��ظ���";
                    dup_tree_node = tree_node;
                    return 1;
                }
            }

            return 0;
        }

        private void TreeView_DoubleClick(object sender, EventArgs e)
        {
            button_modify_Click(sender, e);
        }

        private void TreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Insert)
            {
                button_newIssue_Click(this, null);
                e.Handled = true;
            }
            else if (e.KeyData == Keys.Delete)
            {
                button_delete_Click(this, null);
                e.Handled = true;
            }
        }

        private void toolStripButton_newIssue_Click(object sender, EventArgs e)
        {
            button_newIssue_Click(sender, e);
        }

        private void toolStripButton_delete_Click(object sender, EventArgs e)
        {
            button_delete_Click(sender, e);
        }

        // �޸�һ���ڽڵ�
        private void toolStripButton_modify_Click(object sender, EventArgs e)
        {
            button_modify_Click(sender, e);
        }

        // ����(һ�������ڵ�)ȫ���ڽڵ�
        private void toolStripButton_newAll_Click(object sender, EventArgs e)
        {
            button_newAllIssue_Click(sender, e);
        }

        private void toolStripButton_moveUp_Click(object sender, EventArgs e)
        {
            button_up_Click(sender, e);
        }

        private void toolStripButton_moveDown_Click(object sender, EventArgs e)
        {
            button_down_Click(sender, e);
        }

        public void Sort()
        {
            TreeView.TreeViewNodeSorter = new NodeSorter();
            this.TreeView.Sort();
            // һ��������к�û�����TreeViewNodeSorter��������¶����ʱ����Զ�����
        }
    }


    // ��һ��Σ��ڶ���
    internal class IssueManageItem
    {
        public IssueManageControl Container = null;

        public object Tag = null;   // ���ڴ����Ҫ���ӵ��������Ͷ���

        public string Xml = ""; // һ���ڼ�¼��XML

        internal XmlDocument dom = null;

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed = false;

        public int OrderedCount = -1;    // �����ķ������Ӷ���XML�л�õġ�-1��ʾδ֪

        public int Initial(string strXml,
            out string strError)
        {
            strError = "";

            this.Xml = strXml;
            this.dom = new XmlDocument();
            try
            {
                this.dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            return 0;
        }

        public string PublishTime
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "publishTime");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "publishTime", value);
            }
        }

        public string Issue
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "issue");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "issue", value);
            }
        }

        public string Volume
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "volume");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "volume", value);
            }
        }

        // 2010/3/28
        public string Comment
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "comment");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "comment", value);
            }
        }

        public string Zong
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "zong");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "zong", value);
            }
        }

        public string RefID
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "refID");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "refID", value);
            }
        }

        public string OrderInfo
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementInnerXml(this.dom.DocumentElement,
                    "orderInfo");
            }
        }

        // ��ò�ο�ID�б�
        public int GetItemRefIDs(out List<string> ids,
            out string strError)
        {
            strError = "";
            ids = new List<string>();

            XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*/distribute");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strDistribute = node.InnerText.Trim();
                if (String.IsNullOrEmpty(strDistribute) == true)
                    continue;

                LocationCollection locations = new LocationCollection();
                int nRet = locations.Build(strDistribute,
                    out strError);
                if (nRet == -1)
                    return -1;

                for (int j = 0; j < locations.Count; j++)
                {
                    Location location = locations[j];

                    // ��δ���������������
                    if (location.RefID == "*"
                        || String.IsNullOrEmpty(location.RefID) == true)
                        continue;

                    ids.Add(location.RefID);
                }
            }

            return 0;
        }

        // �������ڵ�����ֺ�ͼ��Icon
        public void SetNodeCaption(TreeNode tree_node)
        {
            Debug.Assert(this.dom != null, "");

            string strPublishTime = DomUtil.GetElementText(this.dom.DocumentElement,
                "publishTime");
            string strIssue = DomUtil.GetElementText(this.dom.DocumentElement,
                "issue");
            string strVolume = DomUtil.GetElementText(this.dom.DocumentElement,
                "volume");
            string strZong = DomUtil.GetElementText(this.dom.DocumentElement,
                "zong");

            int nOrderdCount = 0;
            int nRecievedCount = 0;
            // �����յĲ���
            // string strOrderInfoXml = "";

            if (this.dom == null)
                goto SKIP_COUNT;

            {

                XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*/copy");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    string strCopy = node.InnerText.Trim();
                    if (String.IsNullOrEmpty(strCopy) == true)
                        continue;

                    string strNewCopy = "";
                    string strOldCopy = "";
                    OrderDesignControl.ParseOldNewValue(strCopy,
                        out strOldCopy,
                        out strNewCopy);

                    int nNewCopy = 0;
                    int nOldCopy = 0;

                    try
                    {
                        if (String.IsNullOrEmpty(strNewCopy) == false)
                        {
                            nNewCopy = Convert.ToInt32(strNewCopy);
                        }
                        if (String.IsNullOrEmpty(strOldCopy) == false)
                        {
                            nOldCopy = Convert.ToInt32(strOldCopy);
                        }
                    }
                    catch
                    {
                    }

                    nOrderdCount += nOldCopy;
                    nRecievedCount += nNewCopy;
                }
            }

        SKIP_COUNT:

            if (this.OrderedCount == -1 && nOrderdCount > 0)
                this.OrderedCount = nOrderdCount;

            tree_node.Text = strPublishTime + " no." + strIssue + " ��." + strZong + " v." + strVolume + " (" + nRecievedCount.ToString() + ")";

            if (this.OrderedCount == -1)
            {
                if (nRecievedCount == 0)
                    tree_node.ImageIndex = IssueManageControl.TYPE_RECIEVE_ZERO;
                else
                    tree_node.ImageIndex = IssueManageControl.TYPE_RECIEVE_NOT_COMPLETE;
            }
            else
            {
                if (nRecievedCount >= this.OrderedCount)
                    tree_node.ImageIndex = IssueManageControl.TYPE_RECIEVE_COMPLETED;
                else if (nRecievedCount > 0)
                    tree_node.ImageIndex = IssueManageControl.TYPE_RECIEVE_NOT_COMPLETE;
                else
                    tree_node.ImageIndex = IssueManageControl.TYPE_RECIEVE_ZERO;
            }

            tree_node.SelectedImageIndex = tree_node.ImageIndex;
        }

    }

    /*
    // �ڶ���Σ��ɹ���Ϣ����
    public class OrderItem
    {


    }*/




    // Create a node sorter that implements the IComparer interface.
    internal class NodeSorter : IComparer
    {
        // Compare the length of the strings, or the strings
        // themselves, if they are the same length.
        public int Compare(object x, object y)
        {
            IssueManageItem item_x = (IssueManageItem)((TreeNode)x).Tag;
            IssueManageItem item_y = (IssueManageItem)((TreeNode)y).Tag;

            return string.Compare(item_x.PublishTime, item_y.PublishTime);
        }
    }

}
