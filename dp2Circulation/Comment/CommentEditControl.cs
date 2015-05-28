using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Reflection;

using DigitalPlatform;
using DigitalPlatform.Text;
using DigitalPlatform.Xml;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    internal partial class CommentEditControl : ItemEditControlBase
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
#endif

        public CommentEditControl()
        {
            InitializeComponent();

            base._tableLayoutPanel_main = this.tableLayoutPanel_main;

            AddEvents(true);
        }

        #region ���ݳ�Ա

#if NO
        public string OldRecord = "";
        public byte[] Timestamp = null;
#endif

        public string Index
        {
            get
            {
                return this.textBox_index.Text;
            }
            set
            {
                this.textBox_index.Text = value;
            }
        }

        public string State
        {
            get
            {
                return this.checkedComboBox_state.Text;
            }
            set
            {
                this.checkedComboBox_state.Text = value;
            }
        }

        public string TypeString
        {
            get
            {
                return this.comboBox_type.Text;
            }
            set
            {
                this.comboBox_type.Text = value;
            }
        }

        public string OrderSuggestion
        {
            get
            {
                return this.comboBox_orderSuggestion.Text;
            }
            set
            {
                this.comboBox_orderSuggestion.Text = value;
            }
        }

        public string Title
        {
            get
            {
                return this.textBox_title.Text;
            }
            set
            {
                this.textBox_title.Text = value;
            }
        }

        public string Creator
        {
            get
            {
                return this.textBox_creator.Text;
            }
            set
            {
                this.textBox_creator.Text = value;
            }
        }

        public string Subject
        {
            get
            {
                return this.textBox_subject.Text;
            }
            set
            {
                this.textBox_subject.Text = value;
            }
        }

        public string Summary
        {
            get
            {
                return this.textBox_summary.Text;
            }
            set
            {
                this.textBox_summary.Text = value;
            }
        }

        public string Content
        {
            get
            {
                return this.textBox_content.Text;
            }
            set
            {
                this.textBox_content.Text = value;
            }
        }


        public string CreateTime
        {
            get
            {
                return this.textBox_createTime.Text;
            }
            set
            {
                this.textBox_createTime.Text = value;
            }
        }

        public string LastModified
        {
            get
            {
                return this.textBox_lastModified.Text;
            }
            set
            {
                this.textBox_lastModified.Text = value;
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

        #endregion

        private void CommentEditControl_SizeChanged(object sender, EventArgs e)
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

        // ���洢ֵת��Ϊ�ɶ�����
        static string GetOrderSuggestionCaption(string strText)
        {
            if (string.IsNullOrEmpty(strText) == true)
                return "";

            if (strText == "yes")
                return "[����]";
            if (strText == "no")
                return "[������]";
            return strText;
        }

        // ���ɶ����ֱ仯Ϊ�洢ֵ
        static string GetOrderSuggestionValue(string strText)
        {
            if (string.IsNullOrEmpty(strText) == true)
                return "";
            if (strText == "[��]")
                return "";

            if (strText == "[����]")
                return "yes";
            if (strText == "[������]")
                return "no";
            return strText;
        }

#if NO
        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="strXml">��ע��¼ XML</param>
        /// <param name="strRecPath">��ע��¼·��</param>
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
                this.RecordDom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XML����װ�ص�DOMʱ����" + ex.Message;
                return -1;
            }

            this.Initializing = true;

            this.Index = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "index");
            this.State = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "state");
            this.TypeString = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "type");
            this.OrderSuggestion = GetOrderSuggestionCaption( DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "orderSuggestion")); 
            this.Title = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "title");
            this.Creator = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "creator");
            this.Subject = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "subject");
            this.Summary = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "summary");
            this.Content = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "content").Replace("\\r", "\r\n");
            this.CreateTime = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "createTime");
            this.LastModified = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "lastModified");
            this.ParentId = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "parent");
            this.RefID = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "refID");
            this.Operations = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement,
                "operations");

            this.RecPath = strRecPath;

            this.Initializing = false;

            this.Changed = false;

            return 0;
        }
#endif
        internal override void DomToMember(string strRecPath)
        {
            this.Index = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "index");
            this.State = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "state");
            this.TypeString = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "type");
            this.OrderSuggestion = GetOrderSuggestionCaption(DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "orderSuggestion"));
            this.Title = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "title");
            this.Creator = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "creator");
            this.Subject = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "subject");
            this.Summary = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "summary");
            this.Content = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "content").Replace("\\r", "\r\n");
            this.CreateTime = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "createTime");
            this.LastModified = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "lastModified");
            this.ParentId = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "parent");
            this.RefID = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "refID");
            this.Operations = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement,
                "operations");

            this.RecPath = strRecPath;
        }

        /// <summary>
        /// ���ȫ������
        /// </summary>
        public override void Clear()
        {
            this.Index = "";
            this.State = "";
            this.TypeString = "";
            this.OrderSuggestion = "";
            this.Title = "";
            this.Creator = "";
            this.Subject = "";
            this.Summary = "";
            this.Content = "";
            this.CreateTime = "";
            this.LastModified = "";
            this.ParentId = "";
            this.RefID = "";
            this.Operations = "";
            this.ResetColor();

            this.Changed = false;
        }

#if NO
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
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "parent", this.ParentId);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "refID", this.RefID);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "index", this.Index);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "state", this.State);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "type", this.TypeString);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "orderSuggestion", GetOrderSuggestionValue(this.OrderSuggestion));
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "title", this.Title);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "creator", this.Creator);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "subject", this.Subject);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "summary", this.Summary);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "content", this.Content.Replace("\r\n", "\\r"));

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "createTime", this.CreateTime);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "lastModified", this.LastModified);
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

        }

#if NO
        /// <summary>
        /// �������ʺ��ڱ���ļ�¼��Ϣ
        /// </summary>
        /// <param name="strXml">���ع���õ���ע��¼ XML</param>
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

            this.RefreshDom();

            strXml = this.RecordDom.OuterXml;

            return 0;
        }
#endif

        public void FocusIndex(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.textBox_index.SelectAll();

            this.textBox_index.Focus();
        }

        public void FocusState(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.checkedComboBox_state.SelectAll();

            this.checkedComboBox_state.Focus();
        }

        public void FocusType(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.comboBox_type.SelectAll();

            this.comboBox_type.Focus();
        }

        public void FocusOrderSuggestion(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.comboBox_orderSuggestion.SelectAll();

            this.comboBox_orderSuggestion.Focus();
        }

        public void FocusTitle(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.textBox_title.SelectAll();

            this.textBox_title.Focus();
        }

        public void FocusCreator(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.textBox_creator.SelectAll();

            this.textBox_creator.Focus();
        }

#if NO
        internal override void ResetColor()
        {
            Color color = this.tableLayoutPanel_main.BackColor;
            this.label_index_color.BackColor = color;    // �ͱ���һ��
            this.label_state_color.BackColor = color;
            this.label_type_color.BackColor = color;
            this.label_orderSuggestion_color.BackColor = color;
            this.label_title_color.BackColor = color;
            this.label_creator_color.BackColor = color;
            this.label_subject_color.BackColor = color;
            this.label_summary_color.BackColor = color;
            this.label_content_color.BackColor = color;
            this.label_createTime_color.BackColor = color;
            this.label_lastModified_color.BackColor = color;
            this.label_recPath_color.BackColor = color;
            this.label_refID_color.BackColor = color;
            this.label_operations_color.BackColor = color;
        }
#endif

#if NO
        delegate void Delegate_filterValue(Control control);

        // ���˵� {} ��Χ�Ĳ���
        void FileterValue(Control control)
        {
            string strText = Global.GetPureSeletedValue(control.Text);
            if (control.Text != strText)
                control.Text = strText;
        }

        // ���˵� {} ��Χ�Ĳ���
        // �����б�ֵȥ�صĹ���
        void FileterValueList(Control control)
        {
            List<string> results = StringUtil.FromListString(Global.GetPureSeletedValue(control.Text));
            StringUtil.RemoveDupNoSort(ref results);
            string strText = StringUtil.MakePathList(results);
            if (control.Text != strText)
                control.Text = strText;
        }
#endif

        // ��ȫ�汾
        public static void FilterValueEx(Control owner,
    Control control)
        {
            Delegate_filterValue d = new Delegate_filterValue(__FilterValueEx);

            if (owner.Created == false)
                __FilterValueEx((Control)control);
            else
                owner.BeginInvoke(d, new object[] { control });

        }

        delegate void Delegate_filterValue(Control control);

        // ����ȫ�汾
        // ���˵� {} ��Χ�Ĳ���
        static void __FilterValueEx(Control control)
        {
            string strText = Global.GetPureSeletedValue(control.Text).Replace("[��]", "");
            if (control.Text != strText)
                control.Text = strText;
        }

#if NO
        private void valueTextChanged(object sender, EventArgs e)
        {
            this.Changed = true;

            string strControlName = "";

            if (sender is TextBox)
            {
                TextBox textbox = (TextBox)sender;
                strControlName = textbox.Name;
            }
            else if (sender is ComboBox)
            {
                if (sender == this.comboBox_type)
                {
#if NO
                    Delegate_filterValue d = new Delegate_filterValue(FileterValue);
                    this.BeginInvoke(d, new object[] { sender });
#endif
                    Global.FilterValue(this, (Control)sender);
                }
                else if (sender == this.comboBox_orderSuggestion)
                {
                    FilterValueEx(this, (Control)sender);
                }
                ComboBox combobox = (ComboBox)sender;
                strControlName = combobox.Name;
            }
            else if (sender is CheckedComboBox)
            {
                if (sender == this.checkedComboBox_state)
                {
#if NO
                    Delegate_filterValue d = new Delegate_filterValue(FileterValueList);
                    this.BeginInvoke(d, new object[] { sender });
#endif
                    Global.FilterValueList(this, (Control)sender);
                }

                CheckedComboBox combobox = (CheckedComboBox)sender;
                strControlName = combobox.Name;
            }
            else
            {
                Debug.Assert(false, "δ��������� " + sender.GetType().ToString());
                return;
            }

            int nRet = strControlName.IndexOf("_");
            if (nRet == -1)
            {
                Debug.Assert(false, "textbox������û���»���");
                return;
            }

            string strLabelName = "label_" + strControlName.Substring(nRet + 1) + "_color";

            Label label = (Label)this.tableLayoutPanel_main.Controls[strLabelName];
            if (label == null)
            {
                Debug.Assert(false, "û���ҵ�����Ϊ '" + strLabelName + "' ��Label�ؼ�");
                return;
            }

            label.BackColor = this.ColorChanged;
        }
#endif

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
        /// "librarian" ��ʾֻ�м�¼·�����ο�ID������ʱ�䡢�޸�ʱ��Ϊֻ��������Ϊ�ɸ�д
        /// </param>
        public override void SetReadOnly(string strStyle)
        {
            if (strStyle == "all")
            {
                this.checkedComboBox_state.Enabled = false;
                this.comboBox_type.Enabled = false;
                this.comboBox_orderSuggestion.Enabled = false;

                this.textBox_index.ReadOnly = true;
                this.textBox_title.ReadOnly = true;
                this.textBox_creator.ReadOnly = true;
                this.textBox_subject.ReadOnly = true;
                this.textBox_summary.ReadOnly = true;
                this.textBox_content.ReadOnly = true;

                this.textBox_createTime.ReadOnly = true;
                this.textBox_lastModified.ReadOnly = true;

                this.textBox_recPath.ReadOnly = true;
                this.textBox_refID.ReadOnly = true;
                this.textBox_operations.ReadOnly = true;
                return;
            }

            // �����ReadOnly
            this.textBox_index.ReadOnly = false;
            this.textBox_title.ReadOnly = false;

            this.checkedComboBox_state.Enabled = true;
            this.comboBox_type.Enabled = true;
            this.comboBox_orderSuggestion.Enabled = true;

            this.textBox_creator.ReadOnly = false;
            this.textBox_subject.ReadOnly = false;
            this.textBox_summary.ReadOnly = false;
            this.textBox_content.ReadOnly = false;

            this.textBox_createTime.ReadOnly = false;
            this.textBox_lastModified.ReadOnly = false;

            this.textBox_recPath.ReadOnly = false;

            this.textBox_refID.ReadOnly = false;
            this.textBox_operations.ReadOnly = false;

            if (strStyle == "librarian")
            {
                this.textBox_recPath.ReadOnly = true;
                this.textBox_refID.ReadOnly = true;

                this.textBox_createTime.ReadOnly = true;
                this.textBox_lastModified.ReadOnly = true;
            }
        }

        // �������Ѿ�����ΪReadOnly״̬��ĳЩ����Ϊ�ɸ�д״̬
        /// <summary>
        /// ����Ϊ���޸�״̬
        /// </summary>
        public override void SetChangeable()
        {
            this.textBox_recPath.ReadOnly = false;
            this.textBox_index.ReadOnly = false;
            this.textBox_refID.ReadOnly = false;
        }

        // ��ֹ����
        int m_nInDropDown = 0;

        private void checkedComboBox_state_DropDown(object sender, EventArgs e)
        {
            comboBox_DropDown(sender, e);
        }

        private void comboBox_type_DropDown(object sender, EventArgs e)
        {
            comboBox_DropDown(sender, e);
        }

        private void comboBox_DropDown(object sender, EventArgs e)
        {
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {
                CheckedComboBox checked_combobox = null;
                ComboBox combobox = null;
                int nCount = 0;

                if (sender is CheckedComboBox)
                {
                    checked_combobox = (CheckedComboBox)sender;
                    nCount = checked_combobox.Items.Count;
                }
                else if (sender is ComboBox)
                {
                    combobox = (ComboBox)sender;
                    nCount = combobox.Items.Count;
                }
                else
                    throw new Exception("invalid sender type. must by ComboBox or CheckedComboBox");

                if (nCount == 0
                    /*&& this.GetValueTable != null*/)
                {
                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    e1.DbName = this.BiblioDbName;  // 2009/2/15 changed

                    if (combobox == this.comboBox_type)
                        e1.TableName = "commentType";   // ��ע����
                    else if (combobox == this.comboBox_orderSuggestion)
                        e1.TableName = "orderSuggestion";   // ��������
                    else if (checked_combobox == this.checkedComboBox_state)
                        e1.TableName = "commentState";  // ��ע״̬
                    else
                    {
                        Debug.Assert(false, "��֧�ֵ�sender");
                        return;
                    }

                    // this.GetValueTable(this, e1);
                    this.OnGetValueTable(this, e1);

                    if (e1.values != null)
                    {
                        for (int i = 0; i < e1.values.Length; i++)
                        {
                            if (combobox != null)
                                combobox.Items.Add(e1.values[i]);
                            else
                                checked_combobox.Items.Add(e1.values[i]);
                        }
                    }
                    else
                    {
                        if (combobox != null)
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
            var refControl = r as CommentEditControl;

            if (this.Index != refControl.Index)
                this.label_index_color.BackColor = this.ColorDifference;

            if (this.State != refControl.State)
                this.label_state_color.BackColor = this.ColorDifference;

            if (this.TypeString != refControl.TypeString)
                this.label_type_color.BackColor = this.ColorDifference;

            if (this.OrderSuggestion != refControl.OrderSuggestion)
                this.label_orderSuggestion_color.BackColor = this.ColorDifference;

            if (this.Title != refControl.Title)
                this.label_title_color.BackColor = this.ColorDifference;

            if (this.Creator != refControl.Creator)
                this.label_creator_color.BackColor = this.ColorDifference;

            if (this.Subject != refControl.Subject)
                this.label_subject_color.BackColor = this.ColorDifference;

            if (this.Summary != refControl.Summary)
                this.label_summary_color.BackColor = this.ColorDifference;

            if (this.Content != refControl.Content)
                this.label_content_color.BackColor = this.ColorDifference;

            if (this.CreateTime != refControl.CreateTime)
                this.label_createTime_color.BackColor = this.ColorDifference;

            if (this.LastModified != refControl.LastModified)
                this.label_lastModified_color.BackColor = this.ColorDifference;

            if (this.RecPath != refControl.RecPath)
                this.label_recPath_color.BackColor = this.ColorDifference;

            if (this.RefID != refControl.RefID)
                this.label_refID_color.BackColor = this.ColorDifference;

            if (this.Operations != refControl.Operations)
                this.label_operations_color.BackColor = this.ColorDifference;
        }

        private void DoKeyPress(object sender, KeyPressEventArgs e)
        {
            if (/*this.ControlKeyPress != null*/true)
            {
                ControlKeyPressEventArgs e1 = new ControlKeyPressEventArgs();
                e1.e = e;
                if (sender == (object)this.textBox_index)
                    e1.Name = "Index";
                else if (sender == (object)this.checkedComboBox_state)
                    e1.Name = "State";
                else if (sender == (object)this.comboBox_type)
                    e1.Name = "Type";
                else if (sender == (object)this.comboBox_orderSuggestion)
                    e1.Name = "OrderSuggestion";
                else if (sender == (object)this.textBox_title)
                    e1.Name = "Title";
                else if (sender == (object)this.textBox_creator)
                    e1.Name = "Creator";
                else if (sender == (object)this.textBox_subject)
                    e1.Name = "Subject";
                else if (sender == (object)this.textBox_summary)
                    e1.Name = "Summary";
                else if (sender == (object)this.textBox_content)
                    e1.Name = "Content";
                else if (sender == (object)this.textBox_createTime)
                    e1.Name = "CreateTime";
                else if (sender == (object)this.textBox_lastModified)
                    e1.Name = "LastModified";
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
            if (/*this.ControlKeyDown != null*/true)
            {
                ControlKeyEventArgs e1 = new ControlKeyEventArgs();
                e1.e = e;
                if (sender == (object)this.textBox_index)
                    e1.Name = "Index";
                else if (sender == (object)this.checkedComboBox_state)
                    e1.Name = "State";
                else if (sender == (object)this.comboBox_type)
                    e1.Name = "Type";
                else if (sender == (object)this.comboBox_orderSuggestion)
                    e1.Name = "OrderSuggestion";
                else if (sender == (object)this.textBox_title)
                    e1.Name = "Title";
                else if (sender == (object)this.textBox_creator)
                    e1.Name = "Creator";
                else if (sender == (object)this.textBox_subject)
                    e1.Name = "Subject";
                else if (sender == (object)this.textBox_summary)
                    e1.Name = "Summary";
                else if (sender == (object)this.textBox_content)
                    e1.Name = "Content";
                else if (sender == (object)this.textBox_createTime)
                    e1.Name = "CreateTime";
                else if (sender == (object)this.textBox_lastModified)
                    e1.Name = "LastModified";
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

        private void comboBox_orderSuggestion_DropDown(object sender, EventArgs e)
        {
            comboBox_DropDown(sender, e);
        }
    }
}
