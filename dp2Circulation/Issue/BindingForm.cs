using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

using DigitalPlatform;
using DigitalPlatform.Xml;

namespace dp2Circulation
{
    // C# �ű���Ҫ�õ�������� is BindingForm
    /// <summary>
    /// ��ʾ�ڿ�װ��ͼ�ν���ĶԻ���
    /// </summary>
    public partial class BindingForm : Form
    {
        // Ctrl+A�Զ���������
        /// <summary>
        /// �Զ���������
        /// </summary>
        public event GenerateDataEventHandler GenerateData = null;

        const int WM_ENSURE_VISIBLE = API.WM_USER + 200;

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// �ڿ��ؼ��������� ApplicationInfo ����
        /// </summary>
        public ApplicationInfo AppInfo 
        {
            get
            {
                return this.bindingControl1.AppInfo;
            }
            set
            {
                this.bindingControl1.AppInfo = value;
            }
        }

        /// <summary>
        /// ��ö�����Ϣ
        /// </summary>
        public event GetOrderInfoEventHandler GetOrderInfo = null;

        // public event GetItemInfoEventHandler GetItemInfo = null;

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        public BindingForm()
        {
            InitializeComponent();
        }

        private void BindingForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
            // this.bindingControl1.AppInfo = this.AppInfo;

            this.bindingControl1.GetOrderInfo -= new GetOrderInfoEventHandler(bindingControl1_GetOrderInfo);
            this.bindingControl1.GetOrderInfo += new GetOrderInfoEventHandler(bindingControl1_GetOrderInfo);

#if OLD_INITIAL
            this.bindingControl1.GetItemInfo -= new GetItemInfoEventHandler(bindingControl1_GetItemInfo);
            this.bindingControl1.GetItemInfo += new GetItemInfoEventHandler(bindingControl1_GetItemInfo);
#endif

            this.entityEditControl1.GetValueTable -= new GetValueTableEventHandler(orderDesignControl1_GetValueTable);
            this.entityEditControl1.GetValueTable += new GetValueTableEventHandler(orderDesignControl1_GetValueTable);

            this.entityEditControl1.SetReadOnly("binding");

            this.entityEditControl1.GetAccessNoButton.Click -= new EventHandler(button_getAccessNo_Click);
            this.entityEditControl1.GetAccessNoButton.Click += new EventHandler(button_getAccessNo_Click);

            this.orderDesignControl1.ArriveMode = true;
            this.orderDesignControl1.SeriesMode = true;
            this.orderDesignControl1.Changed = false;

            this.orderDesignControl1.GetValueTable -= new GetValueTableEventHandler(orderDesignControl1_GetValueTable);
            this.orderDesignControl1.GetValueTable += new GetValueTableEventHandler(orderDesignControl1_GetValueTable);

            LoadState();

            this.MainForm.LoadSplitterPos(
this.splitContainer_main,
"bindingform",
"main_splitter_pos");

            API.PostMessage(this.Handle, WM_ENSURE_VISIBLE, 0, 0);
        }

        // �����ȡ��
        void button_getAccessNo_Click(object sender, EventArgs e)
        {

            if (this.GenerateData != null)
            {
                GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                if (Control.ModifierKeys == Keys.Control)
                    e1.ScriptEntry = "ManageCallNumber";
                else
                    e1.ScriptEntry = "CreateCallNumber";
                e1.FocusedControl = sender; // senderΪ��ԭʼ���ӿؼ�
                this.GenerateData(this, e1);
            }
        }

#if NO
        public List<CallNumberItem> GetCallNumberItems()
        {
            List<CallNumberItem> callnumber_items = this.BookItems.GetCallNumberItems();

            CallNumberItem item = null;

            int index = this.BookItems.IndexOf(this.BookItem);
            if (index == -1)
            {
                // ����һ������
                item = new CallNumberItem();
                callnumber_items.Add(item);

                item.CallNumber = "";   // ��Ҫ������ǰ�ģ�����Ӱ�쵽ȡ�Ž��
            }
            else
            {
                // ˢ���Լ���λ��
                item = callnumber_items[index];
                item.CallNumber = entityEditControl_editing.AccessNo;
            }

            item.RecPath = this.entityEditControl_editing.RecPath;
            item.Location = entityEditControl_editing.LocationString;
            item.Barcode = entityEditControl_editing.Barcode;

            return callnumber_items;
        }
#endif

        /// <summary>
        /// �����ȡ����������
        /// </summary>
        /// <returns>CallNumberItem ����</returns>
        public List<CallNumberItem> GetCallNumberItems()
        {
            ItemBindingItem cur_item = null;
            if (this.m_item is ItemBindingItem)
            {
                cur_item = (ItemBindingItem)this.m_item;
            }

            // ����ͬһ���ڿ��ڵ�ȫ����������Ϣ
            List<CallNumberItem> callnumber_items = this.bindingControl1.GetCallNumberItems(cur_item);

            {
                CallNumberItem item = null;
                // ����һ������
                item = new CallNumberItem();
                callnumber_items.Add(item);

                item.CallNumber = "";   // ��Ҫ������ǰ�ģ�����Ӱ�쵽ȡ�Ž��

                item.RecPath = this.entityEditControl1.RecPath;
                item.Location = entityEditControl1.LocationString;
                item.Barcode = entityEditControl1.Barcode;
            }

        // FOUND:
            return callnumber_items;
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_ENSURE_VISIBLE:
                    this.bindingControl1.EnsureCurrentIssueVisible();
                    return;
            }
            base.DefWndProc(ref m);
        }

        /// <summary>
        /// װ����ǰ�洢��״̬
        /// </summary>
        public void LoadState()
        {
            bool bEditAreaVisible = false;
            bool bNeedInvalidate = false;
            bool bNeedRelayout = false;

            if (this.AppInfo != null)
            {
                bEditAreaVisible = this.AppInfo.GetBoolean("bindingform",
                    "edit_area_visible", false);
            }
            // һ��ʼ�༭���������صģ����߱����ϴε�״̬
            VisibleEditArea(bEditAreaVisible);

            string strSplitterDirection = this.AppInfo.GetString(
                "binding_form",
                "splitter_direction",
                "ˮƽ");

            if (strSplitterDirection == "��ֱ")
                this.splitContainer_main.Orientation = Orientation.Horizontal;
            else
                this.splitContainer_main.Orientation = Orientation.Vertical;

            // ��ʾ������Ϣ����ֵ
            bool bValue = this.AppInfo.GetBoolean(
                "binding_form",
                "display_orderinfoxy",
                false);
            if (this.bindingControl1.DisplayOrderInfoXY != bValue)
            {
                this.bindingControl1.DisplayOrderInfoXY = bValue;
                bNeedInvalidate = true;
            }

            // ��ʾ�ֹ��ⶩ����
             bValue = this.AppInfo.GetBoolean(
                "binding_form",
                "display_lockedOrderGroup",
                true);
            if (this.bindingControl1.HideLockedOrderGroup != !bValue)
            {
                this.bindingControl1.HideLockedOrderGroup = !bValue;
                bNeedRelayout = true;
            }

            // �������κ�
            this.AcceptBatchNo = this.AppInfo.GetString(
                "binding_form",
                "accept_batchno",
                "");

            // �����������
            {
                string strLinesCfg = this.AppInfo.GetString(
        "binding_form",
        "cell_lines_cfg",
        "");
                if (String.IsNullOrEmpty(strLinesCfg) == false)
                {
                    string[] parts = strLinesCfg.Split(new char[] { ',' });
                    this.bindingControl1.TextLineNames = parts;
                    bNeedInvalidate = true;
                }
                else
                {
                    if (this.bindingControl1.TextLineNames != this.bindingControl1.DefaultTextLineNames)
                    {
                        this.bindingControl1.TextLineNames = this.bindingControl1.DefaultTextLineNames;
                        bNeedInvalidate = true;
                    }
                }
            }

            // �����������
            {
                string strLinesCfg = this.AppInfo.GetString(
        "binding_form",
        "group_lines_cfg",
        "");
                if (String.IsNullOrEmpty(strLinesCfg) == false)
                {
                    string[] parts = strLinesCfg.Split(new char[] { ',' });
                    this.bindingControl1.GroupTextLineNames = parts;
                    bNeedInvalidate = true;
                }
                else
                {
                    if (this.bindingControl1.GroupTextLineNames != this.bindingControl1.DefaultGroupTextLineNames)
                    {
                        this.bindingControl1.GroupTextLineNames = this.bindingControl1.DefaultGroupTextLineNames;
                        bNeedInvalidate = true;
                    }
                }
            }

            if (bNeedInvalidate == true)
            {
                this.bindingControl1.Invalidate();
            }

            if (bNeedRelayout == true)
            {
                // if (this.bindingControl1.HideLockedOrderGroup == false)
                {
                    string strError = "";
                    // ����Щ��ǰ���صĺ϶���ͳ�Ա����ͼ���°���һ��
                    int nRet = this.bindingControl1.RelayoutHiddenBindingCell(out strError);
                    if (nRet == -1)
                        MessageBox.Show(this, strError);
                }
                this.bindingControl1.RefreshLayout();
            }
        }

        private void BindingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.bindingControl1.Changed == true
                && this.DialogResult == DialogResult.Cancel)
            {
                DialogResult dialog_result = MessageBox.Show(this,
"�������ݷ������޸ģ�����ʱ�رմ��ڽ�������Щ�޸Ķ�ʧ��\r\n\r\nȷʵҪ�رմ��ڣ�",
"BindingControls",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                if (dialog_result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            /*
            FormWindowState old_state = this.WindowState;
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.Visible = false;
                this.WindowState = FormWindowState.Normal;
            }
             * */
            // �ָ���λ��
            this.MainForm.SaveSplitterPos(
                this.splitContainer_main,
                "bindingform",
                "main_splitter_pos");
            /*
            if (this.WindowState != old_state)
            {
                this.WindowState = old_state;
            }
             * */
            // ��ʾ������Ϣ����ֵ
            this.MainForm.AppInfo.SetBoolean(
                "binding_form",
                "display_orderinfoxy",
                this.bindingControl1.DisplayOrderInfoXY);

            // ��ʾ�ֹ��ⶩ����
            this.MainForm.AppInfo.SetBoolean(
                "binding_form",
                "display_lockedOrderGroup",
                !this.bindingControl1.HideLockedOrderGroup);

        }

        private void BindingForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.AppInfo != null)
            {
                this.AppInfo.SetBoolean("bindingform",
                    "edit_area_visible",
                    this.m_bEditAreaVisible);
            }

            this.AppInfo.SetString(
                "binding_form",
                "accept_batchno",
                this.AcceptBatchNo);
        }

        void orderDesignControl1_GetValueTable(object sender, GetValueTableEventArgs e)
        {
            if (this.GetValueTable != null)
                this.GetValueTable(sender, e);
        }

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                return this.bindingControl1.Changed;
            }
            set
            {
                this.bindingControl1.Changed = value;
            }
        }

        internal List<IssueBindingItem> Items
        {
            get
            {
                return this.bindingControl1.Issues;
            }
        }

        internal List<ItemBindingItem> BindItems
        {
            get
            {
                return this.bindingControl1.ParentItems;
            }
        }

        internal List<ItemBindingItem> AllItems
        {
            get
            {
                return this.bindingControl1.AllItems;
            }
        }

        internal List<IssueBindingItem> Issues
        {
            get
            {
                return this.bindingControl1.Issues;
            }
        }

        /// <summary>
        /// ���¼�༭�ؼ�����ǰѡ���Ĳ��¼
        /// </summary>
        public EntityEditControl EntityEditControl
        {
            get
            {
                return this.entityEditControl1;
            }
        }

#if OLD_INITIAL
        // ��ʼ���ڼ䣬׷��һ���ڶ���
        public IssueBindingItem AppendIssue(string strXml,
            out string strError)
        {
            if (this.bindingControl1.HasGetItemInfo() == false)
            {
                this.bindingControl1.GetItemInfo += new GetItemInfoEventHandler(bindingControl1_GetItemInfo);
            }

            return this.bindingControl1.AppendIssue(strXml, out strError);
        }

        // ��ʼ���ڼ䣬׷��һ���϶������
        public ItemBindingItem AppendBindItem(string strXml,
            out string strError)
        {
            if (this.bindingControl1.HasGetItemInfo() == false)
            {
                this.bindingControl1.GetItemInfo += new GetItemInfoEventHandler(bindingControl1_GetItemInfo);
            }

            return this.bindingControl1.AppendBindItem(strXml, 
                out strError);
        }

        public List<string> AllIssueMembersRefIds
        {
            get
            {
                return this.bindingControl1.AllIssueMembersRefIds;
            }
        }

        public int AppendNoneIssueSingleItems(List<string> XmlRecords,
    out string strError)
        {
            return this.bindingControl1.AppendNoneIssueSingleItems(XmlRecords,
                out strError);
        }


        // ��ʼ��
        public int Initial(out string strError)
        {
            if (this.bindingControl1.HasGetItemInfo() == false)
            {
                this.bindingControl1.GetItemInfo += new GetItemInfoEventHandler(bindingControl1_GetItemInfo);
            }

            return this.bindingControl1.Initial(out strError);
        }
#endif

        // ��ʼ��
        // parameters:
        //      strLayoutMode   "auto" "accepting" "binding"��autoΪ�Զ�ģʽ��acceptingΪȫ����Ϊ�ǵ���bindingΪȫ����Ϊװ��
        // return:
        //      -1  ����
        //      0   �ɹ�
        //      1   �ɹ������о��档������Ϣ��strError��
        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="strLayoutMode">����ģʽ��"auto" "accepting" "binding" ֮һ��auto Ϊ�Զ�ģʽ��accepting Ϊȫ����Ϊ�ǵ���binding Ϊȫ����Ϊװ��</param>
        /// <param name="ItemXmls">���¼ XML ����</param>
        /// <param name="IssueXmls">�ڼ�¼ XML ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1: ����</para>>
        /// <para>0: �ɹ�</para>>
        /// <para>1: �ɹ������о��档������Ϣ�� strError ��</para>>
        /// </returns>
        public int NewInitial(
            string strLayoutMode,
            List<string> ItemXmls,
            List<string> IssueXmls,
            out string strError)
        {
            if (this.bindingControl1.HasGetOrderInfo() == false)
            {
                this.bindingControl1.GetOrderInfo += new GetOrderInfoEventHandler(bindingControl1_GetOrderInfo);
            }

            return this.bindingControl1.NewInitial(
                strLayoutMode,
                ItemXmls,
                IssueXmls,
                out strError);
        }

        /// <summary>
        /// �Ƿ�Ϊ�´����Ĳ��¼���á��ӹ��С�״̬
        /// </summary>
        public bool SetProcessingState
        {
            get
            {
                return this.bindingControl1.SetProcessingState;
            }
            set
            {
                this.bindingControl1.SetProcessingState = value;
            }
        }

        /// <summary>
        /// �������κ�
        /// </summary>
        public string AcceptBatchNo
        {
            get
            {
                return this.bindingControl1.AcceptBatchNo;
            }
            set
            {
                this.bindingControl1.AcceptBatchNo = value;
            }
        }

        /// <summary>
        /// �������κ��Ƿ��Ѿ��ڽ��汻������
        /// </summary>
        public bool AcceptBatchNoInputed
        {
            get
            {
                return this.bindingControl1.AcceptBatchNoInputed;
            }
            set
            {
                this.bindingControl1.AcceptBatchNoInputed = value;
            }
        }

        // ��ȡֵ�б�ʱ��Ϊ���������ݿ���
        /// <summary>
        /// ��Ŀ��������ȡֵ�б�ʱ��Ϊ���������ݿ���
        /// </summary>
        public string BiblioDbName
        {
            get
            {
                return this.bindingControl1.BiblioDbName;
            }
            set
            {
                this.bindingControl1.BiblioDbName = value;
            }
        }

        /// <summary>
        /// ��ǰ�������ʻ���
        /// </summary>
        public string Operator
        {
            get
            {
                return this.bindingControl1.Operator;
            }
            set
            {
                this.bindingControl1.Operator = value;
            }
        }

        /// <summary>
        /// ��ǰ�û���Ͻ�Ĺݴ����б�
        /// </summary>
        public string LibraryCodeList
        {
            get
            {
                return this.bindingControl1.LibraryCodeList;
            }
            set
            {
                this.bindingControl1.LibraryCodeList = value;
            }
        }

        /*
        void bindingControl1_GetItemInfo(object sender, GetItemInfoEventArgs e)
        {
            if (this.GetItemInfo != null)
                this.GetItemInfo(sender, e);
        }
         * */

        void bindingControl1_GetOrderInfo(object sender, GetOrderInfoEventArgs e)
        {
            if (this.GetOrderInfo != null)
                this.GetOrderInfo(sender, e);
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";
            // ��β���һ���ڱ༭������޸�
            BackItem();

            // �������һЩ״̬
            int nRet = this.bindingControl1.Finish(out strError);
            if (nRet == -1)
                goto ERROR1;

            // ���
            nRet = this.bindingControl1.Check(out strError);
            if (nRet == -1)
                goto ERROR1;

            this.DialogResult = DialogResult.OK;
            this.Close();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

#if ORDERDESIGN_CONTROL
        void BackIssue()
        {
            if (!(this.m_item is IssueBindingItem))
            {
                return;
            }

            IssueBindingItem issue = (IssueBindingItem)this.m_item;

            string strError = "";
            int nRet = 0;

            // ��β�ϴεĶ��󣬴ӱ༭��������
            if (this.orderDesignControl1.Changed == true
    && issue != null)
            {
                // ��order�ؼ��е���Ϣ�޸Ķ��ֵ�IssueBindingItem������
                nRet = this.bindingControl1.GetFromOrderControl(
                    this.orderDesignControl1,
                    issue,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nRet == 2)  // ����Ϣ�н�һ���仯����Ҫ���õ��༭��
                {
                    string strOrderInfoMessage = "";
                    // ��������Ϣ��ʼ���ɹ��ؼ�
                    // return:
                    //      -1  ����
                    //      0   û���ҵ���Ӧ�Ĳɹ���Ϣ
                    //      1   �ҵ��ɹ���Ϣ
                    nRet = this.bindingControl1.InitialOrderControl(
                        issue,
                        this.orderDesignControl1,
                        out strOrderInfoMessage,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == 0)
                    {
                        this.orderDesignControl1.Visible = false;
                        this.orderDesignControl1.Clear();
                        issue = null;
                        return;
                    }

                }

                // this.m_issue.Changed = true;
                this.orderDesignControl1.Changed = false;
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif

        object m_item = null;

        void BackItem()
        {
            if (!(this.m_item is ItemBindingItem))
            {
                return;
            }

            ItemBindingItem item = (ItemBindingItem)this.m_item;
            string strError = "";
            int nRet = 0;

            // ��β�ϴεĶ��󣬴ӱ༭��������
            if (this.entityEditControl1.Changed == true
    && item != null)
            {
                string strXml = "";
                nRet = this.entityEditControl1.GetData(
                    false,  // �����this.Parent
                    out strXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                nRet = item.ChangeItemXml(strXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                item.Changed = true;
                // this.m_item = null;

                this.entityEditControl1.Changed = false;
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void bindingControl1_CellFocusChanged(object sender, FocusChangedEventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ��β�ϴεĶ��󣬴ӱ༭��������
            BackItem();

#if ORDERDESIGN_CONTROL
            BackIssue();
#endif


            // �Ӳ���󵽱༭��
            if (e.NewFocusObject is Cell)
            {
                Cell cell = null;

                cell = (Cell)e.NewFocusObject;
                if (/*cell.item != this.m_item
                    &&*/ cell.item != null
                    )
                {
                    nRet = this.entityEditControl1.SetData(cell.item.Xml,
                        cell.item.RecPath,
                        null,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (cell.item.Calculated == true
                        || cell.item.Deleted == true
                        || cell.item.Locked == true
                        || (cell.item.ParentItem != null && cell.item.ParentItem.Locked == true) )
                        this.entityEditControl1.SetReadOnly("all");
                    else
                        this.entityEditControl1.SetReadOnly("binding");
                    this.entityEditControl1.Changed = false;
                    this.m_item = cell.item;
                    this.entityEditControl1.ContentControl.Invalidate(); // �����п��ܸı�
                    this.entityEditControl1.Visible = true;
                    this.orderDesignControl1.Visible = false;
                    return;
                }
                
            }

            #if ORDERDESIGN_CONTROL

            // ���ڶ��󵽱༭��
            if (e.NewFocusObject is IssueBindingItem)
            {
                IssueBindingItem issue = null;

                issue = (IssueBindingItem)e.NewFocusObject;
                if (issue != this.m_item
                    && String.IsNullOrEmpty(issue.PublishTime) == false
                    && issue.Virtual == false)
                {
                    string strOrderInfoMessage = "";

                    // ��������Ϣ��ʼ���ɹ��ؼ�
                    // return:
                    //      -1  ����
                    //      0   û���ҵ���Ӧ�Ĳɹ���Ϣ
                    //      1   �ҵ��ɹ���Ϣ
                    nRet = this.bindingControl1.InitialOrderControl(
                        issue,
                        this.orderDesignControl1,
                        out strOrderInfoMessage,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 0)
                        goto END1;
                    
                    this.m_item = issue;
                    this.orderDesignControl1.Visible = true;
                    this.entityEditControl1.Visible = false;
                    return;
                }

            }
#endif

        // END1:
            this.orderDesignControl1.Visible = false;
            this.orderDesignControl1.Clear();
            this.m_item = null;

            this.entityEditControl1.Visible = false;
            this.entityEditControl1.Clear();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        bool m_bEditAreaVisible = true;

        /// <summary>
        /// �༭�����Ƿ�ɼ�
        /// </summary>
        public bool EditAreaVisible
        {
            get
            {
                return this.checkBox_displayEditArea.Checked;
            }
            set
            {
                this.checkBox_displayEditArea.Checked = value;
            }
        }

        void VisibleEditArea(bool bVisible)
        {
            this.checkBox_displayEditArea.Checked = bVisible;

            if (m_bEditAreaVisible == bVisible)
                return;
            if (bVisible == false)
            {
                // ���ر༭�����൱�ڰ�װ���ؼ�ֱ�ӷŵ�����

                // �Ӽ������Ƴ�װ���ؼ�
                this.splitContainer_main.Panel2.Controls.Remove(this.bindingControl1);

                // �޸�װ���ؼ���λ�úͳߴ�
                this.bindingControl1.Dock = DockStyle.None;
                this.bindingControl1.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                this.bindingControl1.Location = this.splitContainer_main.Location;
                this.bindingControl1.Size = this.splitContainer_main.Size;
                this.Controls.Add(this.bindingControl1);

                this.Controls.Remove(this.splitContainer_main);
            }
            else
            {
                // ��ʾ�༭�����൱�ڰѷָ�ؼ�ֱ�ӷŵ�����
                this.splitContainer_main.Dock = DockStyle.None;
                this.splitContainer_main.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                this.splitContainer_main.Location = this.bindingControl1.Location;
                this.splitContainer_main.Size = this.bindingControl1.Size;

                this.Controls.Remove(this.bindingControl1);
                this.bindingControl1.Dock = DockStyle.Fill;
                this.splitContainer_main.Panel1.Controls.Add(this.bindingControl1);

                this.Controls.Add(this.splitContainer_main);
            }

            this.m_bEditAreaVisible = bVisible;
        }

        private void bindingControl1_EditArea(object sender, EditAreaEventArgs e)
        {
            if (e.Action == "get_state")
            {
                if (this.m_bEditAreaVisible == true)
                    e.Result = "visible";
                else
                    e.Result = "hide";
                return;
            }

            if (e.Action == "open")
                this.VisibleEditArea(true);
            else if (e.Action == "close")
                this.VisibleEditArea(false);
            else if (e.Action == "focus")
                this.entityEditControl1.Focus();
        }

        private void entityEditControl1_Leave(object sender, EventArgs e)
        {
            BackItem();
        }

        private void orderDesignControl1_Leave(object sender, EventArgs e)
        {
#if ORDERDESIGN_CONTROL
            BackIssue();
#endif
        }

        private void checkBox_displayEditArea_CheckedChanged(object sender, EventArgs e)
        {
            this.VisibleEditArea(this.checkBox_displayEditArea.Checked);

            // ��LoadState()��������
            if (this.AppInfo != null)
            {
                this.AppInfo.SetBoolean("bindingform",
        "edit_area_visible",
        this.m_bEditAreaVisible);
            }

        }

        // ѡ��
        private void button_option1_Click(object sender, EventArgs e)
        {
            // ͬ���洢ֵ
            this.MainForm.AppInfo.SetBoolean(
    "binding_form",
    "display_orderinfoxy",
    this.bindingControl1.DisplayOrderInfoXY);

            this.MainForm.AppInfo.SetBoolean(
"binding_form",
"display_lockedOrderGroup",
!this.bindingControl1.HideLockedOrderGroup);

            BindingOptionDialog dlg = new BindingOptionDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.DefaultTextLineNames = this.bindingControl1.DefaultTextLineNames;
            dlg.DefaultGroupTextLineNames = this.bindingControl1.DefaultGroupTextLineNames;
            dlg.AppInfo = this.AppInfo;
            dlg.StartPosition = FormStartPosition.CenterScreen;

            dlg.ShowDialog(this);
            if (dlg.DialogResult == DialogResult.OK)
            {
                this.LoadState();

                // TODO: ��ʾ������״̬�ĸı䣬��Ҫ����һ�����³�ʼ������
                // ��Ҫ���ҳ�����Щ��Ҫ��ʾ�ĺ϶��������Ҫ�ۺ��ж����Ա�����Ķ�����
            }
        }

        private void entityEditControl1_VisibleChanged(object sender, EventArgs e)
        {
            if (this.entityEditControl1.Visible == true)
            {
                this.tableLayoutPanel_editArea.RowStyles[1].SizeType = SizeType.Percent;
                this.tableLayoutPanel_editArea.RowStyles[1].Height = 100;

                this.tableLayoutPanel_editArea.RowStyles[2].SizeType = SizeType.Percent;
                this.tableLayoutPanel_editArea.RowStyles[2].Height = 0;
            }
        }

        private void orderDesignControl1_VisibleChanged(object sender, EventArgs e)
        {
            if (this.orderDesignControl1.Visible == true)
            {
                this.tableLayoutPanel_editArea.RowStyles[2].SizeType = SizeType.Percent;
                this.tableLayoutPanel_editArea.RowStyles[2].Height = 100;

                this.tableLayoutPanel_editArea.RowStyles[1].SizeType = SizeType.Percent;
                this.tableLayoutPanel_editArea.RowStyles[1].Height = 0;
            }
        }

        private void toolStripButton_closeTextArea_Click(object sender, EventArgs e)
        {
            this.VisibleEditArea(false);
        }

        private void entityEditControl1_PaintContent(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            /*
            int nDelta = 8;
            RectangleF rect = this.entityEditControl1.ContentControl.DisplayRectangle;
            rect.Inflate(nDelta, nDelta);
            Pen pen = new Pen(Color.Gray);
            BindingControl.RoundRectangle(e.Graphics,
                pen, null, rect, 10);
             * */

            if (!(this.m_item is ItemBindingItem))
                return;

            ItemBindingItem item = (ItemBindingItem)this.m_item;
            Cell cell = item.ContainerCell;
            if (cell == null)
                return;

            PaintInfo info = cell.GetPaintInfo();
            this.entityEditControl1.MemberBackColor = info.BackColor;
            this.entityEditControl1.MemberForeColor = info.ForeColor;

            int nDelta = 8;
            RectangleF rect = this.entityEditControl1.ContentControl.DisplayRectangle;
            rect.Inflate(nDelta, nDelta);

            cell.PaintBorder((long)rect.X,
            (long)rect.Y,
            (int)rect.Width,
            (int)rect.Height,
            e);

        }

        private void entityEditControl1_ControlKeyDown(object sender, ControlKeyEventArgs e)
        {
            if (e.e.KeyCode == Keys.A && e.e.Control == true)
            {
                if (this.GenerateData != null)
                {
                    GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                    e1.FocusedControl = sender; // senderΪ��ԭʼ���ӿؼ�
                    this.GenerateData(this, e1);
                }
                return;
            }
        }

    }
}