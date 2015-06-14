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
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using System.Globalization;

namespace dp2Circulation
{
    internal partial class OrderEditControl : ItemEditControlBase
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

        public OrderEditControl()
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
                return this.comboBox_state.Text;
            }
            set
            {
                this.comboBox_state.Text = value;
            }
        }

        public string CatalogNo
        {
            get
            {
                return this.textBox_catalogNo.Text;
            }
            set
            {
                this.textBox_catalogNo.Text = value;
            }
        }

        public string Seller
        {
            get
            {
                return this.comboBox_seller.Text;
            }
            set
            {
                this.comboBox_seller.Text = value;
            }
        }

        public string Source
        {
            get
            {
                return this.comboBox_source.Text;
            }
            set
            {
                this.comboBox_source.Text = value;
            }
        }

        public string Range
        {
            get
            {
                return this.textBox_range.Text;
            }
            set
            {
                this.textBox_range.Text = value;
            }
        }

        public string IssueCount
        {
            get
            {
                return this.textBox_issueCount.Text;
            }
            set
            {
                this.textBox_issueCount.Text = value;
            }
        }

        public string Copy
        {
            get
            {
                return this.textBox_copy.Text;
            }
            set
            {
                this.textBox_copy.Text = value;
            }
        }

        public string Price
        {
            get
            {
                return this.textBox_price.Text;
            }
            set
            {
                this.textBox_price.Text = value;
            }
        }

        public string TotalPrice
        {
            get
            {
                return this.textBox_totalPrice.Text;
            }
            set
            {
                this.textBox_totalPrice.Text = value;
            }
        }

        public string OrderTime
        {
            get
            {
                DateTime value = this.dateTimePicker_orderTime.Value;
                if (value == this.dateTimePicker_orderTime.MinDate)
                    return "";

                return DateTimeUtil.Rfc1123DateTimeStringEx(value);
            }
            set
            {
                if (string.IsNullOrEmpty(value) == true)
                    this.dateTimePicker_orderTime.Value = this.dateTimePicker_orderTime.MinDate;
                else
                {
                    try
                    {
                        // �����׳��쳣
                        this.dateTimePicker_orderTime.Value = DateTimeUtil.FromRfc1123DateTimeString(value).ToLocalTime();
                    }
                    catch // (Exception ex)
                    {
                        this.dateTimePicker_orderTime.Value = TryParseTimeString(value);
                    }
                }
            }
        }

        DateTime TryParseTimeString(string strText)
        {
            DateTime time;
            if (DateTime.TryParse(strText, out time) == true)
                return time;

            string[] formats = new string[] {"yyyy","yyyyMM", "yyyyMMdd" };

            foreach (string format in formats)
            {
                if (DateTime.TryParseExact(strText,
                    format,
                    CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out time) == true)
                    return time;
            }

            return this.dateTimePicker_orderTime.MinDate;
        }

        public string OrderID
        {
            get
            {
                return this.textBox_orderID.Text;
            }
            set
            {
                this.textBox_orderID.Text = value;
            }
        }

        public string Distribute
        {
            get
            {
                return this.textBox_distribute.Text;
            }
            set
            {
                this.textBox_distribute.Text = value;
            }
        }

        public string Class
        {
            get
            {
                return this.comboBox_class.Text;
            }
            set
            {
                this.comboBox_class.Text = value;
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

        public string SellerAddress
        {
            get
            {
                return this.textBox_sellerAddress.Text;
            }
            set
            {
                this.textBox_sellerAddress.Text = value;
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

        // 2010/4/8
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

        private void OrderEditControl_SizeChanged(object sender, EventArgs e)
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

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="strXml">������¼ XML</param>
        /// <param name="strRecPath">������¼·��</param>
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
            this.CatalogNo = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "catalogNo");
            this.Seller = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "seller");
            this.Source = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "source");
            this.Range = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "range");
            this.IssueCount = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "issueCount");
            this.Copy = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "copy");
            this.Price = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "price");
            this.TotalPrice = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "totalPrice");
            this.OrderTime = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "orderTime");
            this.OrderID = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "orderID");
            this.Distribute = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "distribute");
            this.Class = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "class");
            this.SellerAddress = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement,
                "sellerAddress");

            this.Comment = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "comment");
            this.BatchNo = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "batchNo");

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
            this.CatalogNo = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "catalogNo");
            this.Seller = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "seller");
            this.Source = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "source");
            this.Range = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "range");
            this.IssueCount = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "issueCount");
            this.Copy = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "copy");
            this.Price = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "price");
            this.TotalPrice = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "totalPrice");
            this.OrderTime = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "orderTime");
            this.OrderID = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "orderID");
            this.Distribute = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "distribute");
            this.Class = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "class");
            this.SellerAddress = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement,
                "sellerAddress");

            this.Comment = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "comment");
            this.BatchNo = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "batchNo");

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
            this.CatalogNo = "";
            this.Seller = "";
            this.Source = "";
            this.Range = "";
            this.IssueCount = "";
            this.Copy = "";
            this.Price = "";
            this.TotalPrice = "";
            this.OrderTime = "";
            this.OrderID = "";
            this.Distribute = "";
            this.Class = "";
            this.SellerAddress = "";

            this.Comment = "";
            this.BatchNo = "";

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
                "catalogNo", this.CatalogNo);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "seller", this.Seller);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "source", this.Source);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, 
                "range", this.Range);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "issueCount", this.IssueCount);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "copy", this.Copy);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "price", this.Price);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "totalPrice", this.TotalPrice);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "orderTime", this.OrderTime);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "orderID", this.OrderID);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "distribute", this.Distribute);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "class", this.Class);
            try
            {
                DomUtil.SetElementInnerXml(this.RecordDom.DocumentElement,
                    "sellerAddress",
                    this.SellerAddress);
            }
            catch (Exception ex)
            {
                string strError = "������ַ��ǶXMLƬ�� '" + this.SellerAddress + "' ��ʽ����: " + ex.Message;
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

            DomUtil.SetElementText(this.RecordDom.DocumentElement, 
                "comment", this.Comment);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "batchNo", this.BatchNo);
        }

#if NO
        /// <summary>
        /// �������ʺ��ڱ���ļ�¼��Ϣ
        /// </summary>
        /// <param name="strXml">���ع���õĶ�����¼ XML</param>
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
                this.comboBox_state.SelectAll();

            this.comboBox_state.Focus();
        }

        public void FocusCatalogNo(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.textBox_catalogNo.SelectAll();

            this.textBox_catalogNo.Focus();
        }

        public void FocusSeller(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.comboBox_seller.SelectAll();

            this.comboBox_seller.Focus();
        }

        public void FocusSource(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.comboBox_source.SelectAll();

            this.comboBox_source.Focus();
        }

#if NO
        internal override void ResetColor()
        {
            Color color = this.tableLayoutPanel_main.BackColor;
            this.label_index_color.BackColor = color;    // �ͱ���һ��
            this.label_state_color.BackColor = color;
            this.label_catalogNo_color.BackColor = color;
            this.label_seller_color.BackColor = color;
            this.label_source_color.BackColor = color;
            this.label_range_color.BackColor = color;
            this.label_issueCount_color.BackColor = color;
            this.label_copy_color.BackColor = color;
            this.label_price_color.BackColor = color;
            this.label_totalPrice_color.BackColor = color;
            this.label_orderTime_color.BackColor = color;
            this.label_orderID_color.BackColor = color;
            this.label_distribute_color.BackColor = color;
            this.label_class_color.BackColor = color;

            this.label_comment_color.BackColor = color;
            this.label_batchNo_color.BackColor = color;
            this.label_sellerAddress_color.BackColor = color;
            this.label_recPath_color.BackColor = color;
            this.label_refID_color.BackColor = color;
            this.label_operations_color.BackColor = color;
        }
#endif
        /*
        private void textBox_index_TextChanged(object sender, EventArgs e)
        {
            valueTextChanged(sender, e);
        }*/

#if NO
        private void valueTextChanged(object sender, EventArgs e)
        {
            this.Changed = true;

            string strControlName = "";

            if (sender is Control)
            {
                Control control = (Control)sender;
                strControlName = control.Name;
            }
            if (sender is TextBox)
            {
                TextBox textbox = (TextBox)sender;
                strControlName = textbox.Name;
            }
            else if (sender is ComboBox)
            {
                ComboBox combobox = (ComboBox)sender;
                strControlName = combobox.Name;
            }
            else if (sender is DateTimePicker)
            {
                DateTimePicker picker = (DateTimePicker)sender;
                strControlName = picker.Name;
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
        /// "librarian" ��ʾֻ�м�¼·�����ο�IDΪֻ��������Ϊ�ɸ�д
        /// </param>
        public override void SetReadOnly(string strStyle)
        {
            if (strStyle == "all")
            {
                this.comboBox_state.Enabled = false;
                this.comboBox_seller.Enabled = false;
                this.comboBox_source.Enabled = false;
                this.comboBox_class.Enabled = false;

                this.textBox_index.ReadOnly = true;
                this.textBox_catalogNo.ReadOnly = true;
                this.textBox_range.ReadOnly = true;
                this.textBox_issueCount.ReadOnly = true;
                this.textBox_copy.ReadOnly = true;
                this.textBox_price.ReadOnly = true;
                this.textBox_totalPrice.ReadOnly = true;
                this.dateTimePicker_orderTime.Enabled = !true;
                this.textBox_orderID.ReadOnly = true;
                this.textBox_distribute.ReadOnly = true;

                this.textBox_comment.ReadOnly = true;
                this.textBox_batchNo.ReadOnly = true;
                this.textBox_recPath.ReadOnly = true;

                this.textBox_sellerAddress.ReadOnly = true;
                this.textBox_refID.ReadOnly = true;
                this.textBox_operations.ReadOnly = true;

                return;
            }

            // �����ReadOnly
            this.textBox_index.ReadOnly = false;
            this.textBox_catalogNo.ReadOnly = false;

            this.comboBox_state.Enabled = true;
            this.comboBox_seller.Enabled = true;
            this.comboBox_source.Enabled = true;
            this.comboBox_class.Enabled = true;

            this.textBox_range.ReadOnly = false;
            this.textBox_issueCount.ReadOnly = false;
            this.textBox_copy.ReadOnly = false;
            this.textBox_price.ReadOnly = false;
            this.textBox_totalPrice.ReadOnly = false;
            this.dateTimePicker_orderTime.Enabled = !false;
            this.textBox_orderID.ReadOnly = false;
            this.textBox_distribute.ReadOnly = false;

            this.textBox_comment.ReadOnly = false;
            this.textBox_batchNo.ReadOnly = false;
            this.textBox_recPath.ReadOnly = false;

            this.textBox_sellerAddress.ReadOnly = false;
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
            this.textBox_index.ReadOnly = false;
            this.textBox_refID.ReadOnly = false;
        }

        // ��ֹ���� 2009/7/19
        int m_nInDropDown = 0;


        private void comboBox_state_DropDown(object sender, EventArgs e)
        {
            // ��ֹ���� 2009/7/19
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
                        e1.TableName = "orderState";
                    else if (combobox == this.comboBox_seller)
                        e1.TableName = "orderSeller";
                    else if (combobox == this.comboBox_source)
                        e1.TableName = "orderSource";
                    else if (combobox == this.comboBox_class)
                        e1.TableName = "orderClass";
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
            var refControl = r as OrderEditControl;

            if (this.Index != refControl.Index)
                this.label_index_color.BackColor = this.ColorDifference;

            if (this.State != refControl.State)
                this.label_state_color.BackColor = this.ColorDifference;

            if (this.CatalogNo != refControl.CatalogNo)
                this.label_catalogNo_color.BackColor = this.ColorDifference;


            if (this.Seller != refControl.Seller)
                this.label_seller_color.BackColor = this.ColorDifference;

            if (this.Source != refControl.Source)
                this.label_source_color.BackColor = this.ColorDifference;

            if (this.Range != refControl.Range)
                this.label_range_color.BackColor = this.ColorDifference;

            if (this.IssueCount != refControl.IssueCount)
                this.label_issueCount_color.BackColor = this.ColorDifference;

            if (this.Copy != refControl.Copy)
                this.label_copy_color.BackColor = this.ColorDifference;

            if (this.Price != refControl.Price)
                this.label_price_color.BackColor = this.ColorDifference;

            if (this.TotalPrice != refControl.TotalPrice)
                this.label_totalPrice_color.BackColor = this.ColorDifference;

            if (this.OrderTime != refControl.OrderTime)
                this.label_orderTime_color.BackColor = this.ColorDifference;

            if (this.OrderID != refControl.OrderID)
                this.label_orderID_color.BackColor = this.ColorDifference;

            if (this.Distribute != refControl.Distribute)
                this.label_distribute_color.BackColor = this.ColorDifference;

            if (this.Class != refControl.Class)
                this.label_class_color.BackColor = this.ColorDifference;

            if (this.Comment != refControl.Comment)
                this.label_comment_color.BackColor = this.ColorDifference;

            if (this.BatchNo != refControl.BatchNo)
                this.label_batchNo_color.BackColor = this.ColorDifference;

            if (this.SellerAddress != refControl.SellerAddress)
                this.label_sellerAddress_color.BackColor = this.ColorDifference;

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
                if (sender == (object)this.textBox_index)
                    e1.Name = "Index";
                else if (sender == (object)this.comboBox_state)
                    e1.Name = "State";
                else if (sender == (object)this.textBox_catalogNo)
                    e1.Name = "CatalogNo";
                else if (sender == (object)this.comboBox_seller)
                    e1.Name = "Seller";
                else if (sender == (object)this.comboBox_source)
                    e1.Name = "Source";
                else if (sender == (object)this.textBox_range)
                    e1.Name = "Range";
                else if (sender == (object)this.textBox_issueCount)
                    e1.Name = "IssueCount";
                else if (sender == (object)this.textBox_copy)
                    e1.Name = "Copy";
                else if (sender == (object)this.textBox_price)
                    e1.Name = "Price";
                else if (sender == (object)this.textBox_totalPrice)
                    e1.Name = "TotalPrice";
                else if (sender == (object)this.dateTimePicker_orderTime)
                    e1.Name = "OrderTime";
                else if (sender == (object)this.textBox_orderID)
                    e1.Name = "OrderID";
                else if (sender == (object)this.textBox_distribute)
                    e1.Name = "Distribute";
                else if (sender == (object)this.comboBox_class)
                    e1.Name = "Class";
                else if (sender == (object)this.textBox_comment)
                    e1.Name = "Comment";
                else if (sender == (object)this.textBox_batchNo)
                    e1.Name = "BatchNo";
                else if (sender == (object)this.textBox_sellerAddress)
                    e1.Name = "SellerAddress";
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
            // if (this.ControlKeyDown != null)
            {
                ControlKeyEventArgs e1 = new ControlKeyEventArgs();
                e1.e = e;
                if (sender == (object)this.textBox_index)
                    e1.Name = "Index";
                else if (sender == (object)this.comboBox_state)
                    e1.Name = "State";
                else if (sender == (object)this.textBox_catalogNo)
                    e1.Name = "CatalogNo";
                else if (sender == (object)this.comboBox_seller)
                    e1.Name = "Seller";
                else if (sender == (object)this.comboBox_source)
                    e1.Name = "Source";
                else if (sender == (object)this.textBox_range)
                    e1.Name = "Range";
                else if (sender == (object)this.textBox_issueCount)
                    e1.Name = "IssueCount";
                else if (sender == (object)this.textBox_copy)
                    e1.Name = "Copy";
                else if (sender == (object)this.textBox_price)
                    e1.Name = "Price";
                else if (sender == (object)this.textBox_totalPrice)
                    e1.Name = "TotalPrice";
                else if (sender == (object)this.dateTimePicker_orderTime)
                    e1.Name = "OrderTime";
                else if (sender == (object)this.textBox_orderID)
                    e1.Name = "OrderID";
                else if (sender == (object)this.textBox_distribute)
                    e1.Name = "Distribute";
                else if (sender == (object)this.comboBox_class)
                    e1.Name = "Class";
                else if (sender == (object)this.textBox_comment)
                    e1.Name = "Comment";
                else if (sender == (object)this.textBox_batchNo)
                    e1.Name = "BatchNo";
                else if (sender == (object)this.textBox_sellerAddress)
                    e1.Name = "SellerAddress";
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

        private void comboBox_seller_DropDown(object sender, EventArgs e)
        {
            comboBox_state_DropDown(sender, e);
        }

        private void comboBox_source_DropDown(object sender, EventArgs e)
        {
            comboBox_state_DropDown(sender, e);
        }

        private void comboBox_class_DropDown(object sender, EventArgs e)
        {
            comboBox_state_DropDown(sender, e);
        }

#if NO
        private void textBox_refID_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_refID_color.BackColor = this.ColorChanged;
                this.Changed = true;
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
#endif

        private void comboBox_state_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValue);
            this.BeginInvoke(d, new object[] { sender });
#endif
        }

        private void comboBox_seller_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValue);
            this.BeginInvoke(d, new object[] { sender });
#endif

        }

        private void comboBox_source_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValue);
            this.BeginInvoke(d, new object[] { sender });
#endif
        }

        private void comboBox_class_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValue);
            this.BeginInvoke(d, new object[] { sender });
#endif
        }

        private void dateTimePicker_orderTime_ValueChanged(object sender, EventArgs e)
        {
            if (this.dateTimePicker_orderTime.Value == this.dateTimePicker_orderTime.MinDate)
                this.dateTimePicker_orderTime.CustomFormat = " ";
            else
                this.dateTimePicker_orderTime.CustomFormat = "yyyy-MM-dd HH:mm:ss";

            // valueTextChanged(sender, e);
        }

    }
}
