using System;
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

namespace dp2Circulation
{
    internal partial class IssueEditControl : ItemEditControlBase
    {
#if NO
        // ��ȡֵ�б�ʱ��Ϊ���������ݿ���
        public string BiblioDbName = "";

        XmlDocument RecordDom = null;

        bool m_bChanged = false;

        bool m_bInInitial = true;   // �Ƿ����ڳ�ʼ������֮��

        Color ColorChanged = Color.Yellow; // ��ʾ���ݸı������ɫ
        Color ColorDifference = Color.Blue; // ��ʾ�����в������ɫ

        string m_strParentId = "";

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        /// <summary>
        /// ���ݷ����ı�
        /// </summary>
        public event ContentChangedEventHandler ContentChanged = null;

        public event ControlKeyPressEventHandler ControlKeyPress = null;

        public event ControlKeyEventHandler ControlKeyDown = null;

        public bool Initializing
        {
            get
            {
                return this.m_bInInitial;
            }
            set
            {
                this.m_bInInitial = value;
            }
        }
#endif

        #region ���ݳ�Ա

#if NO
        public string OldRecord = "";
        public byte[] Timestamp = null;
#endif

        public string PublishTime
        {
            get
            {
                return this.textBox_publishTime.Text;
            }
            set
            {
                this.textBox_publishTime.Text = value;
            }
        }

        public string State
        {
            get
            {
                return this.comboBox_state.Text;
            }
            set
            {
                this.comboBox_state.Text = value;
            }
        }

        public string Issue
        {
            get
            {
                return this.textBox_issue.Text;
            }
            set
            {
                this.textBox_issue.Text = value;
            }
        }

        public string Zong
        {
            get
            {
                return this.textBox_zong.Text;
            }
            set
            {
                this.textBox_zong.Text = value;
            }
        }

        public string Volume
        {
            get
            {
                return this.textBox_volume.Text;
            }
            set
            {
                this.textBox_volume.Text = value;
            }
        }

        public string OrderInfo
        {
            get
            {
                return this.textBox_orderInfo.Text;
            }
            set
            {
                this.textBox_orderInfo.Text = value;
            }
        }

        public string Comment
        {
            get
            {
                return this.textBox_comment.Text;
            }
            set
            {
                this.textBox_comment.Text = value;
            }
        }

        public string BatchNo
        {
            get
            {
                return this.textBox_batchNo.Text;
            }
            set
            {
                this.textBox_batchNo.Text = value;
            }
        }


        public string RecPath
        {
            get
            {
                return this.textBox_recPath.Text;
            }
            set
            {
                this.textBox_recPath.Text = value;
            }
        }

        public string RefID
        {
            get
            {
                return this.textBox_refID.Text;
            }
            set
            {
                this.textBox_refID.Text = value;
            }
        }

        // 2010/4/7
        public string Operations
        {
            get
            {
                return this.textBox_operations.Text;
            }
            set
            {
                this.textBox_operations.Text = value;
            }
        }

#if NO
        public string ParentId
        {
            get
            {
                return this.m_strParentId;
            }
            set
            {
                this.m_strParentId = value;
            }
        }
#endif

        #endregion

        public IssueEditControl()
        {
            InitializeComponent();
        }

        private void tableLayoutPanel_main_SizeChanged(object sender, EventArgs e)
        {
            tableLayoutPanel_main.Size = this.Size;

        }

#if NO
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
                bool bOldValue = this.m_bChanged;

                this.m_bChanged = value;
                if (this.m_bChanged == false)
                    this.ResetColor();

                // �����¼�
                if (bOldValue != value && this.ContentChanged != null)
                {
                    ContentChangedEventArgs e = new ContentChangedEventArgs();
                    e.OldChanged = bOldValue;
                    e.CurrentChanged = value;
                    ContentChanged(this, e);
                }
            }
        }
#endif

#if NO
        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="strXml">�ڼ�¼ XML</param>
        /// <param name="strRecPath">�ڼ�¼·��</param>
        /// <param name="timestamp">ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int SetData(string strXml,
            string strRecPath,
            byte[] timestamp,
            out string strError)
        {
            strError = "";

            this.OldRecord = strXml;
            this.Timestamp = timestamp;

            this.RecordDom = new XmlDocument();

            try
            {
                if (string.IsNullOrEmpty(strXml) == true)
                    this.RecordDom.LoadXml("<root />");
                else
                    this.RecordDom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XML����װ�ص�DOMʱ����" + ex.Message;
                return -1;
            }

            this.Initializing = true;

            this.PublishTime = DomUtil.GetElementText(this.RecordDom.DocumentElement, "publishTime");
            this.State = DomUtil.GetElementText(this.RecordDom.DocumentElement, "state");
            this.Issue = DomUtil.GetElementText(this.RecordDom.DocumentElement, "issue");
            this.Zong = DomUtil.GetElementText(this.RecordDom.DocumentElement, "zong");
            this.Volume = DomUtil.GetElementText(this.RecordDom.DocumentElement, "volume");

            // 2008/12/23 changed
            /*
            XmlNode nodeOrderInfo = this.RecordDom.DocumentElement.SelectSingleNode("orderInfo");
            if (nodeOrderInfo != null)
                this.OrderInfo = nodeOrderInfo.InnerXml;
            else
                this.OrderInfo = "";
             * */
            this.OrderInfo = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement, "orderInfo");


            this.Comment = DomUtil.GetElementText(this.RecordDom.DocumentElement, "comment");
            this.BatchNo = DomUtil.GetElementText(this.RecordDom.DocumentElement, "batchNo");

            this.ParentId = DomUtil.GetElementText(this.RecordDom.DocumentElement, "parent");
            this.RefID = DomUtil.GetElementText(this.RecordDom.DocumentElement, "refID");
            this.Operations = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement, "operations");

            this.RecPath = strRecPath;

            this.Initializing = false;

            this.Changed = false;

            return 0;
        }
#endif

        internal override void DomToMember(string strRecPath)
        {
            this.PublishTime = DomUtil.GetElementText(this.RecordDom.DocumentElement, "publishTime");
            this.State = DomUtil.GetElementText(this.RecordDom.DocumentElement, "state");
            this.Issue = DomUtil.GetElementText(this.RecordDom.DocumentElement, "issue");
            this.Zong = DomUtil.GetElementText(this.RecordDom.DocumentElement, "zong");
            this.Volume = DomUtil.GetElementText(this.RecordDom.DocumentElement, "volume");

            // 2008/12/23 changed
            /*
            XmlNode nodeOrderInfo = this.RecordDom.DocumentElement.SelectSingleNode("orderInfo");
            if (nodeOrderInfo != null)
                this.OrderInfo = nodeOrderInfo.InnerXml;
            else
                this.OrderInfo = "";
             * */
            this.OrderInfo = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement, "orderInfo");


            this.Comment = DomUtil.GetElementText(this.RecordDom.DocumentElement, "comment");
            this.BatchNo = DomUtil.GetElementText(this.RecordDom.DocumentElement, "batchNo");

            this.ParentId = DomUtil.GetElementText(this.RecordDom.DocumentElement, "parent");
            this.RefID = DomUtil.GetElementText(this.RecordDom.DocumentElement, "refID");
            this.Operations = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement, "operations");

            this.RecPath = strRecPath;
        }

        /// <summary>
        /// ���ȫ������
        /// </summary>
        public override void Clear()
        {
            this.PublishTime = "";
            this.State = "";
            this.Issue = "";
            this.Zong = "";
            this.Volume = "";
            this.OrderInfo = "";

            this.Comment = "";
            this.BatchNo = "";

            this.ParentId = "";
            this.RefID = "";
            this.Operations = "";
            this.ResetColor();

            this.Changed = false;
        }

#if NO
        // ���ܻ��׳��쳣
        public XmlDocument DataDom
        {
            get
            {
                // 2012/12/28
                if (this.RecordDom == null)
                {
                    this.RecordDom = new XmlDocument();
                    this.RecordDom.LoadXml("<root />");
                } 
                this.RefreshDom();
                return this.RecordDom;
            }
        }
#endif

        internal override void RefreshDom()
        {
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "parent", this.ParentId);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "refID", this.RefID);

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "publishTime", this.PublishTime);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "state", this.State);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "issue", this.Issue);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "zong", this.Zong);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "volume", this.Volume);

            // 2008/12/23 changed
            /*
            XmlNode nodeOrderInfo = this.RecordDom.DocumentElement.SelectSingleNode("orderInfo");
            if (nodeOrderInfo == null)
            {
                nodeOrderInfo = this.RecordDom.CreateElement("orderInfo");
                this.RecordDom.DocumentElement.AppendChild(nodeOrderInfo);
            }

            // TODO: ���ܻ��׳��쳣��
            try
            {
                nodeOrderInfo.InnerXml = this.OrderInfo;
            }
            catch (Exception ex)
            {
                string strError = "������Ϣ��ǶXMLƬ�� '"+this.OrderInfo+"' ��ʽ����: " + ex.Message;
                throw new Exception(strError);
            }
             * */
            try
            {
                DomUtil.SetElementInnerXml(this.RecordDom.DocumentElement,
                    "orderInfo", 
                    this.OrderInfo);
            }
            catch (Exception ex)
            {
                string strError = "������Ϣ(<orderInfo>Ԫ��)��ǶXMLƬ�� '" + this.OrderInfo + "' ��ʽ����: " + ex.Message;
                throw new Exception(strError);
            }

            try
            {
                DomUtil.SetElementInnerXml(this.RecordDom.DocumentElement,
                    "operations",
                    this.Operations);
            }
            catch (Exception ex)
            {
                string strError = "������Ϣ(<operations>Ԫ��)��ǶXMLƬ�� '" + this.Operations + "' ��ʽ����: " + ex.Message;
                throw new Exception(strError);
            }

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "comment", this.Comment);

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "batchNo", this.BatchNo);
        }

#if NO
        /// <summary>
        /// �������ʺ��ڱ���ļ�¼��Ϣ
        /// </summary>
        /// <param name="strXml">���ع���õ��ڼ�¼ XML</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int GetData(
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            if (this.RecordDom == null)
            {
                this.RecordDom = new XmlDocument();
                this.RecordDom.LoadXml("<root />");
            }

            if (this.ParentId == "")
            {
                strError = "GetData()����Parent��Ա��δ���塣";
                return -1;
            }

            try
            {
                this.RefreshDom();
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            strXml = this.RecordDom.OuterXml;

            return 0;
        }
#endif

        public void FocusPublishTime(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.textBox_publishTime.SelectAll();

            this.textBox_publishTime.Focus();
        }

        public void FocusState(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.comboBox_state.SelectAll();

            this.comboBox_state.Focus();
        }

        public void FocusIssue(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.textBox_issue.SelectAll();

            this.textBox_issue.Focus();
        }

        internal override void ResetColor()
        {
            Color color = this.tableLayoutPanel_main.BackColor;
            this.label_publishTime_color.BackColor = color;    // �ͱ���һ��
            this.label_state_color.BackColor = color;
            this.label_issue_color.BackColor = color;
            this.label_zong_color.BackColor = color;
            this.label_volume_color.BackColor = color;
            this.label_orderInfo_color.BackColor = color;
            this.label_comment_color.BackColor = color;
            this.label_batchNo_color.BackColor = color;
            this.label_recPath_color.BackColor = color;
            this.label_refID_color.BackColor = color;
            this.label_operations_color.BackColor = color;
        }

        private void textBox_publishTime_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_publishTime_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }

        }

        private void comboBox_state_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_state_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }

        }

        private void textBox_issue_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_issue_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }

        }

        private void textBox_zong_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_zong_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }

        }

        private void textBox_volume_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_volume_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }

        }

        private void textBox_orderInfo_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_orderInfo_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }

        }

        private void textBox_comment_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_comment_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }

        }

        private void textBox_batchNo_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_batchNo_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_recPath_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_recPath_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_refID_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_refID_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

#if NO
        /// <summary>
        /// ֻ��״̬���
        /// </summary>
        public enum ReadOnlyStyle
        {
            /// <summary>
            /// ���ȫ��ֻ��״̬���ָ��ɱ༭״̬
            /// </summary>
            Clear = 0,  // ���ȫ��ReadOnly״̬���ָ��ɱ༭״̬
            /// <summary>
            /// ȫ��ֻ��
            /// </summary>
            All = 1,    // ȫ����ֹ�޸�
            /// <summary>
            /// ͼ���һ�㹤����Ա�������޸�·��
            /// </summary>
            Librarian = 2,  // ͼ���һ�㹤����Ա�������޸�·��
        }
#endif

        // �������ݵ�ʵ�����, ���ڹ�����Ҫ�޸ĵ�ĳЩ������ΪReadOnly״̬
        /// <summary>
        /// ����ֻ��״̬
        /// </summary>
        /// <param name="strStyle">�������ֻ��״̬��
        /// "all" ��ʾȫ��Ϊֻ����
        /// "librarian" ��ʾֻ�м�¼·�����ο�IDΪֻ��������Ϊ�ɸ�д
        /// </param>
        public override void SetReadOnly(string strStyle)
        {
            if (strStyle == "all")
            {
                this.comboBox_state.Enabled = false;

                this.textBox_publishTime.ReadOnly = true;
                this.textBox_issue.ReadOnly = true;
                this.textBox_zong.ReadOnly = true;
                this.textBox_volume.ReadOnly = true;
                this.textBox_orderInfo.ReadOnly = true;
                this.textBox_comment.ReadOnly = true;
                this.textBox_batchNo.ReadOnly = true;
                this.textBox_recPath.ReadOnly = true;
                this.textBox_refID.ReadOnly = true;
                this.textBox_operations.ReadOnly = true;

                return;
            }

            // �����ReadOnly
            this.textBox_publishTime.ReadOnly = false;
            this.comboBox_state.Enabled = true;
            this.textBox_issue.ReadOnly = false;
            this.textBox_zong.ReadOnly = false;
            this.textBox_volume.ReadOnly = false;
            this.textBox_orderInfo.ReadOnly = false;
            this.textBox_comment.ReadOnly = false;
            this.textBox_batchNo.ReadOnly = false;

            this.textBox_recPath.ReadOnly = false;
            this.textBox_refID.ReadOnly = false;
            this.textBox_operations.ReadOnly = false;

            if (strStyle == "librarian")
            {
                this.textBox_recPath.ReadOnly = true;
                this.textBox_refID.ReadOnly = true;
            }
        }

        // �������Ѿ�����ΪReadOnly״̬��ĳЩ����Ϊ�ɸ�д״̬
        /// <summary>
        /// ����Ϊ���޸�״̬
        /// </summary>
        public override void SetChangeable()
        {
            this.textBox_recPath.ReadOnly = false;
            this.textBox_publishTime.ReadOnly = false;
            this.textBox_refID.ReadOnly = false;
        }

        // ��ֹ���� 2009/7/19 new add
        int m_nInDropDown = 0;

        private void comboBox_state_DropDown(object sender, EventArgs e)
        {
            // ��ֹ���� 2009/7/19 new add
            if (this.m_nInDropDown > 0)
                return;

            ComboBox combobox = (ComboBox)sender;

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {

                if (combobox.Items.Count == 0
                    /*&& this.GetValueTable != null*/)
                {
                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    e1.DbName = this.BiblioDbName;  // 2009/2/15 change

                    if (combobox == this.comboBox_state)
                        e1.TableName = "issueState";
                    else
                    {
                        Debug.Assert(false, "��֧�ֵ�sender");
                        return;
                    }

                    // this.GetValueTable(this, e1);
                    OnGetValueTable(this, e1);

                    if (e1.values != null)
                    {
                        for (int i = 0; i < e1.values.Length; i++)
                        {
                            combobox.Items.Add(e1.values[i]);
                        }
                    }
                    else
                    {
                        combobox.Items.Add("<not found>");
                    }
                }
            }
            finally
            {
                this.Cursor = oldCursor;
                this.m_nInDropDown--;
            }
        }

        // �Ƚ��Լ���refControl�����ݲ��죬��������ɫ��ʾ�����ֶ�
        /// <summary>
        /// �Ƚ��Լ���refControl�����ݲ��죬��������ɫ��ʾ�����ֶ�
        /// </summary>
        /// <param name="r">Ҫ���Լ����бȽϵĿؼ�����</param>
        public override void HighlightDifferences(ItemEditControlBase r)
        {
            var refControl = r as IssueEditControl;

            if (this.PublishTime != refControl.PublishTime)
                this.label_publishTime_color.BackColor = this.ColorDifference;

            if (this.State != refControl.State)
                this.label_state_color.BackColor = this.ColorDifference;

            if (this.Issue != refControl.Issue)
                this.label_issue_color.BackColor = this.ColorDifference;

            if (this.Zong != refControl.Zong)
                this.label_zong_color.BackColor = this.ColorDifference;

            if (this.Volume != refControl.Volume)
                this.label_volume_color.BackColor = this.ColorDifference;

            if (this.OrderInfo != refControl.OrderInfo)
                this.label_orderInfo_color.BackColor = this.ColorDifference;

            if (this.Comment != refControl.Comment)
                this.label_comment_color.BackColor = this.ColorDifference;

            if (this.BatchNo != refControl.BatchNo)
                this.label_batchNo_color.BackColor = this.ColorDifference;

            if (this.RecPath != refControl.RecPath)
                this.label_recPath_color.BackColor = this.ColorDifference;

            if (this.RefID != refControl.RefID)
                this.label_refID_color.BackColor = this.ColorDifference;

            if (this.Operations != refControl.Operations)
                this.label_operations_color.BackColor = this.ColorDifference;
        }


        private void DoKeyPress(object sender, KeyPressEventArgs e)
        {
            // if (this.ControlKeyPress != null)
            {
                ControlKeyPressEventArgs e1 = new ControlKeyPressEventArgs();
                e1.e = e;
                if (sender == (object)this.textBox_publishTime)
                    e1.Name = "PublishTime";
                else if (sender == (object)this.comboBox_state)
                    e1.Name = "State";
                else if (sender == (object)this.textBox_issue)
                    e1.Name = "Issue";
                else if (sender == (object)this.textBox_zong)
                    e1.Name = "Zong";
                else if (sender == (object)this.textBox_volume)
                    e1.Name = "Volume";
                else if (sender == (object)this.textBox_orderInfo)
                    e1.Name = "OrderInfo";
                else if (sender == (object)this.textBox_comment)
                    e1.Name = "Comment";
                else if (sender == (object)this.textBox_batchNo)
                    e1.Name = "BatchNo";
                else if (sender == (object)this.textBox_recPath)
                    e1.Name = "RecPath";
                else if (sender == (object)this.textBox_refID)
                    e1.Name = "RefID";
                else if (sender == (object)this.textBox_operations)
                    e1.Name = "Operations";
                else
                {
                    Debug.Assert(false, "δ֪�Ĳ���");
                    return;
                }

                // this.ControlKeyPress(this, e1);
                this.OnControlKeyPress(this, e1);
            }

        }

        private void DoKeyDown(object sender, KeyEventArgs e)
        {
            /*
            if (e.KeyCode == Keys.P)
                MessageBox.Show(this, "pppppppppp");
             * */

            // if (this.ControlKeyDown != null)
            {
                ControlKeyEventArgs e1 = new ControlKeyEventArgs();
                e1.e = e;
                if (sender == (object)this.textBox_publishTime)
                    e1.Name = "PublishTime";
                else if (sender == (object)this.comboBox_state)
                    e1.Name = "State";
                else if (sender == (object)this.textBox_issue)
                    e1.Name = "Issue";
                else if (sender == (object)this.textBox_zong)
                    e1.Name = "Zong";
                else if (sender == (object)this.textBox_volume)
                    e1.Name = "Volume";
                else if (sender == (object)this.textBox_orderInfo)
                    e1.Name = "OrderInfo";
                else if (sender == (object)this.textBox_comment)
                    e1.Name = "Comment";
                else if (sender == (object)this.textBox_batchNo)
                    e1.Name = "BatchNo";
                else if (sender == (object)this.textBox_recPath)
                    e1.Name = "RecPath";
                else if (sender == (object)this.textBox_refID)
                    e1.Name = "RefID";
                else if (sender == (object)this.textBox_operations)
                    e1.Name = "Operations";
                else
                {
                    Debug.Assert(false, "δ֪�Ĳ���");
                    return;
                }

                // this.ControlKeyDown(this, e1);
                this.OnControlKeyDown(this, e1);
            }

        }

        private void textBox_operations_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_operations_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

#if NO
        delegate void Delegate_filterValue(Control control);

        // ���˵� {} ��Χ�Ĳ���
        void FileterValue(Control control)
        {
            string strText = Global.GetPureSeletedValue(control.Text);
            if (control.Text != strText)
                control.Text = strText;
        }
#endif

        private void comboBox_state_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValue);
            this.BeginInvoke(d, new object[] { sender });
#endif
        }
    }
}
