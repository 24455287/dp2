using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Drawing.Drawing2D;

using DigitalPlatform;
using DigitalPlatform.Text;
using DigitalPlatform.Xml;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// ���¼�༭�ؼ�
    /// </summary>
    public partial class EntityEditControl : ItemEditControlBase
    {
        public event ApendMenuEventHandler AppendMenu = null;

        // ����״̬
        public override ItemDisplayState CreateState
        {
            get
            {
                return base.CreateState;
            }
            set
            {
                base.CreateState = value;

                this.Invalidate();
            }
        }

        /// <summary>
        /// �ݲصص�ı��¼�
        /// </summary>
        public event TextChangeEventHandler LocationStringChanged = null;

        #region �ⲿʹ�õĶ���

        /// <summary>
        /// ������ȡ�� ��ť
        /// </summary>
        public System.Windows.Forms.Button GetAccessNoButton
        {
            get
            {
                return this.button_getAccessNo;
            }
        }


        #endregion

        #region ���ݳ�Ա



        /// <summary>
        /// �������
        /// </summary>
        public string Barcode
        {
            get
            {
                return this.textBox_barcode.Text;
            }
            set
            {
                this.textBox_barcode.Text = value;
            }
        }

        /// <summary>
        /// ״̬
        /// </summary>
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

        /// <summary>
        /// ����ʱ��
        /// </summary>
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

        /// <summary>
        /// �ݲص�
        /// </summary>
        public string LocationString
        {
            get
            {
                return this.comboBox_location.Text;
            }
            set
            {
                this.comboBox_location.Text = value;
            }
        }

        /// <summary>
        /// ������������
        /// </summary>
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

        // 2008/2/15 new add
        /// <summary>
        /// ������Դ
        /// </summary>
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

        /// <summary>
        /// �۸�
        /// </summary>
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

        /// <summary>
        /// װ������
        /// </summary>
        public string BindingCost
        {
            get
            {
                return this.textBox_bindingCost.Text;
            }
            set
            {
                this.textBox_bindingCost.Text = value;
            }
        }

        /// <summary>
        /// ע��
        /// </summary>
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

        /// <summary>
        /// ������
        /// </summary>
        public string Borrower
        {
            get
            {
                return this.textBox_borrower.Text;
            }
            set
            {
                this.textBox_borrower.Text = value;
            }
        }

        /// <summary>
        /// ��������
        /// </summary>
        public string BorrowDate
        {
            get
            {
                return this.textBox_borrowDate.Text;
            }
            set
            {
                this.textBox_borrowDate.Text = value;
            }
        }

        /// <summary>
        /// ��������
        /// </summary>
        public string BorrowPeriod
        {
            get
            {
                return this.textBox_borrowPeriod.Text;
            }
            set
            {
                this.textBox_borrowPeriod.Text = value;
            }
        }

        /// <summary>
        /// ��¼·��
        /// </summary>
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

        /// <summary>
        /// ͼ������
        /// </summary>
        public string BookType
        {
            get
            {
                return this.comboBox_bookType.Text;
            }
            set
            {
                this.comboBox_bookType.Text = value;
            }
        }

        /// <summary>
        /// ��¼��
        /// </summary>
        public string RegisterNo
        {
            get
            {
                return this.textBox_registerNo.Text;
            }
            set
            {
                this.textBox_registerNo.Text = value;
            }
        }

        /// <summary>
        /// �ϲ�ע��
        /// </summary>
        public string MergeComment
        {
            get
            {
                return this.textBox_mergeComment.Text;
            }
            set
            {
                this.textBox_mergeComment.Text = value;
            }
        }

        /// <summary>
        /// ���κ�
        /// </summary>
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

        /// <summary>
        /// ���
        /// </summary>
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

        // 2008/12/12 new add
        /// <summary>
        /// ��ȡ��
        /// </summary>
        public string AccessNo
        {
            get
            {
                return this.textBox_accessNo.Text;
            }
            set
            {
                this.textBox_accessNo.Text = value;
            }
        }



        /// <summary>
        /// �ο� ID
        /// </summary>
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

        // 2009/10/11 new add
        /// <summary>
        /// �����
        /// </summary>
        public string Intact
        {
            get
            {
                return this.textBox_intact.Text;
            }
            set
            {
                this.textBox_intact.Text = value;
            }
        }

        // 2009/10/11 new add
        /// <summary>
        /// װ����Ϣ
        /// </summary>
        public string Binding
        {
            get
            {
                return this.textBox_binding.Text;
            }
            set
            {
                this.textBox_binding.Text = value;
            }
        }

        // 2009/10/24 new add
        /// <summary>
        /// ������Ϣ
        /// </summary>
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

        /// <summary>
        /// ���캯��
        /// </summary>
        public EntityEditControl()
        {
            InitializeComponent();

            base._tableLayoutPanel_main = this.tableLayoutPanel_main;
            AddEvents(true);

            SetMouseWheelSimulateEvent();

        }

        /*public*/ void SetMouseWheelSimulateEvent()
        {
            foreach (Control child in this.tableLayoutPanel_main.Controls)
            {
                if (child is TextBox)
                {
                    TextBox textbox = (TextBox)child;
                    if (textbox.Multiline == true)
                        child.MouseWheel += new MouseEventHandler(textBox_comment_MouseWheel);
                }

                if (child is ComboBox)
                {
                    child.MouseWheel += new MouseEventHandler(textBox_comment_MouseWheel);
                }

                if (child is CheckedComboBox)
                {
                    child.MouseWheel += new MouseEventHandler(textBox_comment_MouseWheel);
                }
            }
        }

        void textBox_comment_MouseWheel(object sender, MouseEventArgs e)
        {
            int nValue = this.tableLayoutPanel_main.VerticalScroll.Value;
            nValue  -= e.Delta;
            if (nValue > this.tableLayoutPanel_main.VerticalScroll.Maximum)
                nValue = this.tableLayoutPanel_main.VerticalScroll.Maximum;
            if (nValue < this.tableLayoutPanel_main.VerticalScroll.Minimum)
                nValue = this.tableLayoutPanel_main.VerticalScroll.Minimum;

            if (this.tableLayoutPanel_main.VerticalScroll.Value != nValue)
            {
                this.tableLayoutPanel_main.VerticalScroll.Value = nValue;
                this.tableLayoutPanel_main.PerformLayout();
            }
        }

        private void EntityEditControl_SizeChanged(object sender, EventArgs e)
        {
            tableLayoutPanel_main.Size = this.Size;
        }

#if NO
        bool _simpleMode = false;

        /// <summary>
        /// �Ƿ�Ϊ��ģʽ
        /// </summary>
        public bool SimpleMode
        {
            get
            {
                return this._simpleMode;
            }
            set
            {
                this._simpleMode = value;

                SetSimpleMode(value);
            }
        }
#endif

        string _displayMode = "full";

        /// <summary>
        ///  �༭����ʾģʽ
        /// </summary>
        public string DisplayMode
        {
            get
            {
                return this._displayMode;
            }
            set
            {
                this._displayMode = value;

                SetDisplayMode(value);
            }
        }

        void SetLineVisible(Control control, bool bVisible)
        {
            TableLayoutPanelCellPosition position = this.tableLayoutPanel_main.GetPositionFromControl(control);
            Control label = this.tableLayoutPanel_main.GetControlFromPosition(0, position.Row);
            Control color = this.tableLayoutPanel_main.GetControlFromPosition(1, position.Row);
            Control button = this.tableLayoutPanel_main.GetControlFromPosition(3, position.Row);

            control.Visible = bVisible;
            label.Visible = bVisible;
            color.Visible = bVisible;
            if (button != null)
                button.Visible = bVisible;
        }

#if NO
        void SetSimpleMode(bool bSimple)
        {
            SetLineVisible(this.checkedComboBox_state, !bSimple);
            SetLineVisible(this.textBox_publishTime, !bSimple);
            SetLineVisible(this.comboBox_source, !bSimple);
            SetLineVisible(this.comboBox_seller, !bSimple);
            SetLineVisible(this.textBox_registerNo, !bSimple);
            SetLineVisible(this.textBox_comment, !bSimple);
            SetLineVisible(this.textBox_mergeComment, !bSimple);
            SetLineVisible(this.textBox_borrower, !bSimple);
            SetLineVisible(this.textBox_borrowDate, !bSimple);
            SetLineVisible(this.textBox_borrowPeriod, !bSimple);
            SetLineVisible(this.textBox_intact, !bSimple);
            SetLineVisible(this.textBox_bindingCost, !bSimple);
            SetLineVisible(this.textBox_binding, !bSimple);
            SetLineVisible(this.textBox_operations, !bSimple);
            SetLineVisible(this.textBox_recPath, !bSimple);
            SetLineVisible(this.textBox_refID, !bSimple);
        }
#endif

        Label label_errorInfo = null;

        // ������Ϣ����
        public string ErrorInfo
        {
            get
            {
                if (this.label_errorInfo == null)
                    return "";
                return this.label_errorInfo.Text;
            }
            set
            {
                if (this.label_errorInfo == null)
                    CreateErrorInfoLabel(value);
                else
                    this.label_errorInfo.Text = value;

                if (string.IsNullOrEmpty(value) == true)
                    this.label_errorInfo.Visible = false;
                else
                    this.label_errorInfo.Visible = true;
            }
        }

#if NO
        // ������Ϣ��ͼ��
        public Image ErrorInfoIcon
        {
            get
            {
                if (this.label_errorInfo == null)
                    return null;
                return this.label_errorInfo.Image;
            }
            set
            {
                if (this.label_errorInfo == null)
                    CreateErrorInfoLabel();
                this.label_errorInfo.Image = value;
            }
        }
#endif
        public Label ErrorInfoLabel
        {
            get
            {
                if (this.label_errorInfo == null)
                    CreateErrorInfoLabel(null); 
                return this.label_errorInfo;
            }
        }

#if NO
        void SetLabelText(Label label, 
            int nWidth,
            string strText)
        {
            label.Text = strText;

            Graphics g = Graphics.FromHwnd(this.Handle);

            StringFormat sf = new StringFormat();
            sf.Trimming = StringTrimming.None;
            SizeF size = g.MeasureString(strText + "\r\n",   //������һ��'\r\n'�Ա�֤��������еĸ߶�
                label.Font,
                nWidth,
                sf);

            int nTempHeight = (int)size.Height;
            if (nTempHeight <= 0)
                nTempHeight = 20;

            label.ClientSize = new Size(nWidth, nTempHeight);
        }
#endif

        // ���� ErrorInfo ��ǩ
        // parameters:
        //      strText ���֡����Ϊ null����ʾ�����ó�ʼ�ı�
        void CreateErrorInfoLabel(string strText)
        {
            if (this.label_errorInfo != null)
                return;

            int nWidth = this.GetLineContentPixelWidth();

            this.label_errorInfo = new GrowLabel();
            this.label_errorInfo.Font = this.tableLayoutPanel_main.Font;
            // this.label_errorInfo.AutoSize = true;
            //this.label_errorInfo.MaximumSize = new Size(this.tableLayoutPanel_main.ClientSize.Width, 2000);
            //this.label_errorInfo.Dock = DockStyle.Fill;
            this.label_errorInfo.Size = new Size(nWidth, 20);
            //this.label_errorInfo.ForeColor = Color.DarkRed;
            this.label_errorInfo.BackColor = Color.DarkRed;
            this.label_errorInfo.ForeColor = Color.White;
            this.label_errorInfo.Padding = new Padding(8);

            this.DisableUpdate();
            try
            {
                InsertRowStyle(this.tableLayoutPanel_main, 0);
                if (strText != null)
                    this.label_errorInfo.Text = strText;
            }
            finally
            {
                this.EnableUpdate();
            }

            this.tableLayoutPanel_main.RowStyles[0] = new RowStyle(SizeType.AutoSize);

            this.tableLayoutPanel_main.Controls.Add(this.label_errorInfo, 0, 0);
            this.tableLayoutPanel_main.SetColumnSpan(this.label_errorInfo, this.tableLayoutPanel_main.ColumnStyles.Count);
        }

        // �� nRow λ�ò���һ���µ� RowStyle��ͬʱ������Ŀؼ�����ƶ�һ��
        public static void InsertRowStyle(TableLayoutPanel table, int nRow)
        {
            int nEnd = table.RowStyles.Count - 1;

            table.RowCount++;

            // 2015/5/30
                List<int> column_indices = new List<int>();
            {
                for (int j = 0; j < table.ColumnStyles.Count; j++)
                {
                    column_indices.Add(j);
                }
                // ���ƶ� textbox �С�������� Focued �� Control���ᴥ�� Leave �¼����ᵼ���޸� Label ��ɫ��������Ҫ���ҵ�ͬ�е� Label
                if (column_indices.IndexOf(2) != -1)
                {
                    column_indices.Remove(2);
                    column_indices.Insert(0, 2);
                }
            }

            // ���ƶ��󷽵Ŀؼ�
            for (int i = nEnd; i >= nRow; i--)
            {
                foreach (int j in column_indices)
                {
                    Control control = table.GetControlFromPosition(j, i);
                    if (control != null)
                    {
                        table.Controls.Remove(control);
                        table.Controls.Add(control, j, i + 1);
                    }
                }
            }

            table.RowStyles.Insert(nRow, new RowStyle());

            // table.RowStyles[nRow] = new RowStyle(); // ��ʱ��
        }

        public Padding TablePadding
        {
            get
            {
                return this.tableLayoutPanel_main.Padding;
            }
            set
            {
                this.tableLayoutPanel_main.Padding = value;
            }
        }

        public Padding TableMargin
        {
            get
            {
                return this.tableLayoutPanel_main.Margin;
            }
            set
            {
                this.tableLayoutPanel_main.Margin = value;
            }
        }

        void SetDisplayMode(string strMode)
        {
            this.DisableUpdate();
            try
            {
                if (strMode == "simple_register")
                {
                    this.tableLayoutPanel_main.AutoScroll = false;
                    this.tableLayoutPanel_main.AutoSize = true;
                    this.tableLayoutPanel_main.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;

                    // this.label_barcode.Font = new Font(this.Font.Name, this.Font.Size * 2, FontStyle.Bold);

                    this.textBox_barcode.Font = new Font(/*this.Font.Name*/"Courier New", this.Font.Size * 2, FontStyle.Bold);
                    this.textBox_barcode.Dock = DockStyle.Fill;

                    this.textBox_refID.ReadOnly = true;

                    this.tableLayoutPanel_main.Margin = new Padding(0, 0, 0, 0);
                    this.tableLayoutPanel_main.Padding = new Padding(4);

                    this.tableLayoutPanel_main.BackColor = SystemColors.Window;

                    this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                }

                if (strMode == "simple")
                {
                    this.tableLayoutPanel_main.AutoScroll = true;
                    this.tableLayoutPanel_main.AutoSize = true;
                    this.tableLayoutPanel_main.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                }

                List<Control> controls = new List<Control>();

                if (strMode == "simple" || strMode == "simple_register")
                {
                    controls.Add(this.textBox_barcode);
                    controls.Add(this.comboBox_location);
                    controls.Add(this.textBox_price);
                    controls.Add(this.textBox_accessNo);
                    controls.Add(this.textBox_volume);
                    controls.Add(this.comboBox_bookType);
                    controls.Add(this.textBox_batchNo);
                    controls.Add(this.textBox_refID);
                }

                // ������ label �޸�Ϊ�Ҷ���
                if (strMode == "simple_register")
                {
                    for (int i = 0; i < this.tableLayoutPanel_main.RowStyles.Count; i++)
                    {
                        Label label = this.tableLayoutPanel_main.GetControlFromPosition(0, i) as Label;
                        if (label != null)
                        {
                            label.TextAlign = ContentAlignment.MiddleRight;
                            label.ForeColor = SystemColors.GrayText;
                        }
                    }
                }

                for (int i = 0; i < this.tableLayoutPanel_main.RowStyles.Count; i++)
                {
                    Control control = this.tableLayoutPanel_main.GetControlFromPosition(2, i);
                    if (control == null)
                        continue;

                    {
                        Control label = this.tableLayoutPanel_main.GetControlFromPosition(0, i) as Label;
                        if (label != null)
                        {
                            if (label != this.label_barcode)
                            {
                                label.MouseUp -= new System.Windows.Forms.MouseEventHandler(this.tableLayoutPanel_main_MouseUp);
                                label.MouseUp += new System.Windows.Forms.MouseEventHandler(this.tableLayoutPanel_main_MouseUp);
                            }
                        }
                    }

                    if (strMode == "full")
                    {
                        // ��ʾ
                        if (control.Visible == false)
                            SetLineVisible(control, true);
                        continue;
                    }

                    if (controls.IndexOf(control) == -1)
                    {
                        // ����
                        if (this.Visible == false || control.Visible == true)
                        {
                            SetLineVisible(control, false);
                            this.tableLayoutPanel_main.RowStyles[i] = new RowStyle(SizeType.Absolute, 0);
                        }
                    }
                    else
                    {
                        // ��ʾ
                        if (this.Visible == false || control.Visible == false)
                        {
                            SetLineVisible(control, true);
                            this.tableLayoutPanel_main.RowStyles[i] = new RowStyle(SizeType.AutoSize);
                        }
                    }
                }

            }
            finally
            {
                this.EnableUpdate();
            }

                // ���������Χ����Ȼ�Ļ�������ѽ������ ConboBox �Ϲ������֣�����������ܵ�����ȥ�²���
                if (strMode == "simple_register")
                {
                    this.button_getAccessNo.Visible = false;

                    this.tableLayoutPanel_main.PerformLayout();

                    int nHeight = this.textBox_refID.Location.Y + this.textBox_refID.Height;
                    this.tableLayoutPanel_main.AutoScrollMinSize = new Size(this.tableLayoutPanel_main.AutoScrollMinSize.Width, nHeight);
                }


        }

        /// <summary>
        /// ��Ա�ؼ��ı���ɫ
        /// </summary>
        public Color MemberBackColor
        {
            get
            {
                return this.label_barcode.BackColor;
            }
            set
            {
                this.label_barcode.BackColor = value;
                this.label2.BackColor = value;
                this.label3.BackColor = value;
                this.label4.BackColor = value;
                this.label5.BackColor = value;
                this.label6.BackColor = value;
                this.label7.BackColor = value;
                this.label8.BackColor = value;
                this.label9.BackColor = value;
                this.label10.BackColor = value;
                this.label11.BackColor = value;
                this.label12.BackColor = value;
                this.label13.BackColor = value;
                this.label14.BackColor = value;
                this.label15.BackColor = value;
                this.label16.BackColor = value;
                this.label17.BackColor = value;
                this.label18.BackColor = value;
                this.label19.BackColor = value;
                this.label20.BackColor = value;
                this.label21.BackColor = value;
                this.label22.BackColor = value;
                this.label23.BackColor = value;
            }
        }

        /// <summary>
        /// ��Ա�ؼ���ǰ��ɫ
        /// </summary>
        public Color MemberForeColor
        {
            get
            {
                return this.label_barcode.ForeColor;
            }
            set
            {
                this.label_barcode.ForeColor = value;
                this.label2.ForeColor = value;
                this.label3.ForeColor = value;
                this.label4.ForeColor = value;
                this.label5.ForeColor = value;
                this.label6.ForeColor = value;
                this.label7.ForeColor = value;
                this.label8.ForeColor = value;
                this.label9.ForeColor = value;
                this.label10.ForeColor = value;
                this.label11.ForeColor = value;
                this.label12.ForeColor = value;
                this.label13.ForeColor = value;
                this.label14.ForeColor = value;
                this.label15.ForeColor = value;
                this.label16.ForeColor = value;
                this.label17.ForeColor = value;
                this.label18.ForeColor = value;
                this.label19.ForeColor = value;
                this.label20.ForeColor = value;
                this.label21.ForeColor = value;
                this.label22.ForeColor = value;
                this.label23.ForeColor = value;
            }
        }

        /// <summary>
        /// ����ɫ
        /// </summary>
        [Category("Appearance")]
        [DescriptionAttribute("Background Color")]
        [DefaultValue(typeof(Color), "WhiteSmoke")]
        public new Color BackColor
        {
            get
            {
                return base.BackColor;
                // return this.tableLayoutPanel_main.BackColor;
            }
            set
            {
                this.tableLayoutPanel_main.BackColor = Color.Transparent;
                base.BackColor = value;
            }
        }

        /// <summary>
        /// ǰ��ɫ
        /// </summary>
        [Category("Appearance")]
        [DescriptionAttribute("Foreground Color")]
        [DefaultValue(typeof(Color), "SystemColors.ControlText")]
        public new Color ForeColor
        {
            get
            {
                return this.tableLayoutPanel_main.ForeColor;
            }
            set
            {
                this.tableLayoutPanel_main.ForeColor = value;

                this.MemberForeColor = value;
                this.SetAllEditColor(this.BackColor, this.ForeColor);
            }
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
                /*
                this.m_bChanged = value;
                if (this.m_bChanged == false)
                    this.ResetColor();
                 * */

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
        /// <param name="strXml">ʵ���¼ XML</param>
        /// <param name="strRecPath">ʵ���¼·��</param>
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
                if (String.IsNullOrEmpty(strXml) == true)
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

            this.Barcode = DomUtil.GetElementText(this.RecordDom.DocumentElement, "barcode");
            this.State = DomUtil.GetElementText(this.RecordDom.DocumentElement, "state");
            this.PublishTime = DomUtil.GetElementText(this.RecordDom.DocumentElement, "publishTime");
            this.LocationString = DomUtil.GetElementText(this.RecordDom.DocumentElement, "location");
            this.Seller = DomUtil.GetElementText(this.RecordDom.DocumentElement, "seller");

            this.Source = DomUtil.GetElementText(this.RecordDom.DocumentElement, "source");

            this.Price = DomUtil.GetElementText(this.RecordDom.DocumentElement, "price");
            this.BindingCost = DomUtil.GetElementText(this.RecordDom.DocumentElement, "bindingCost");
            this.BookType = DomUtil.GetElementText(this.RecordDom.DocumentElement, "bookType");

            this.RegisterNo = DomUtil.GetElementText(this.RecordDom.DocumentElement, "registerNo");

            this.Comment = DomUtil.GetElementText(this.RecordDom.DocumentElement, "comment");
            this.MergeComment = DomUtil.GetElementText(this.RecordDom.DocumentElement, "mergeComment");
            this.BatchNo = DomUtil.GetElementText(this.RecordDom.DocumentElement, "batchNo");
            this.Volume = DomUtil.GetElementText(this.RecordDom.DocumentElement, "volume");
            this.AccessNo = DomUtil.GetElementText(this.RecordDom.DocumentElement, "accessNo");


            this.Borrower = DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrower");
            this.BorrowDate = DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrowDate");
            this.BorrowPeriod = DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrowPeriod");

            this.Intact = DomUtil.GetElementText(this.RecordDom.DocumentElement, "intact");

            //this.Binding = DomUtil.GetElementText(this.RecordDom.DocumentElement, "binding");
            this.Binding = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement, "binding");

            // this.Operations = DomUtil.GetElementText(this.RecordDom.DocumentElement, "operations");
            this.Operations = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement, "operations");

            this.ParentId = DomUtil.GetElementText(this.RecordDom.DocumentElement, "parent");

            this.RefID = DomUtil.GetElementText(this.RecordDom.DocumentElement, "refID");

            this.RecPath = strRecPath;

            this.Initializing = false;

            this.Changed = false;

            return 0;
        }
#endif

        internal override void DomToMember(string strRecPath)
        {
            this.Barcode = DomUtil.GetElementText(this.RecordDom.DocumentElement, "barcode");
            this.State = DomUtil.GetElementText(this.RecordDom.DocumentElement, "state");
            this.PublishTime = DomUtil.GetElementText(this.RecordDom.DocumentElement, "publishTime");
            this.LocationString = DomUtil.GetElementText(this.RecordDom.DocumentElement, "location");
            this.Seller = DomUtil.GetElementText(this.RecordDom.DocumentElement, "seller");

            this.Source = DomUtil.GetElementText(this.RecordDom.DocumentElement, "source");

            this.Price = DomUtil.GetElementText(this.RecordDom.DocumentElement, "price");
            this.BindingCost = DomUtil.GetElementText(this.RecordDom.DocumentElement, "bindingCost");
            this.BookType = DomUtil.GetElementText(this.RecordDom.DocumentElement, "bookType");

            this.RegisterNo = DomUtil.GetElementText(this.RecordDom.DocumentElement, "registerNo");

            this.Comment = DomUtil.GetElementText(this.RecordDom.DocumentElement, "comment");
            this.MergeComment = DomUtil.GetElementText(this.RecordDom.DocumentElement, "mergeComment");
            this.BatchNo = DomUtil.GetElementText(this.RecordDom.DocumentElement, "batchNo");
            this.Volume = DomUtil.GetElementText(this.RecordDom.DocumentElement, "volume");
            this.AccessNo = DomUtil.GetElementText(this.RecordDom.DocumentElement, "accessNo");


            this.Borrower = DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrower");
            this.BorrowDate = DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrowDate");
            this.BorrowPeriod = DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrowPeriod");

            this.Intact = DomUtil.GetElementText(this.RecordDom.DocumentElement, "intact");

            //this.Binding = DomUtil.GetElementText(this.RecordDom.DocumentElement, "binding");
            this.Binding = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement, "binding");

            // this.Operations = DomUtil.GetElementText(this.RecordDom.DocumentElement, "operations");
            this.Operations = DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement, "operations");

            this.ParentId = DomUtil.GetElementText(this.RecordDom.DocumentElement, "parent");

            this.RefID = DomUtil.GetElementText(this.RecordDom.DocumentElement, "refID");

            this.RecPath = strRecPath;
        }

        /// <summary>
        /// ���ȫ������
        /// </summary>
        public override void Clear()
        {
            this.Barcode = "";
            this.State = "";
            this.PublishTime = "";
            this.LocationString = "";
            this.Seller = "";
            this.Source = "";
            this.Price = "";
            this.BindingCost = "";
            this.BookType = "";

            this.RegisterNo = "";

            this.Comment = "";
            this.MergeComment = "";
            this.BatchNo = "";
            this.Volume = "";
            this.AccessNo = "";

            this.Borrower = "";
            this.BorrowDate = "";
            this.BorrowPeriod = "";

            this.Intact = "";
            this.Binding = "";
            this.Operations = "";

            this.ParentId = "";

            this.RefID = "";

            this.ResetColor();

            this.Changed = false;
        }



        /// <summary>
        /// ˢ�� ��¼ XMLDOM�����ѿؼ��е����ݸ��µ� XMLDOM ��
        /// </summary>
        /*public*/ internal override void RefreshDom()
        {
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "parent", this.ParentId);

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "refID", this.RefID);

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "barcode", this.Barcode);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "state", this.State);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "publishTime", this.PublishTime);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "location", this.LocationString);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "seller", this.Seller);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "source", this.Source);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "price", this.Price);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "bindingCost", this.BindingCost);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "bookType", this.BookType);

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "registerNo", this.RegisterNo);

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "comment", this.Comment);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "mergeComment", this.MergeComment);

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "batchNo", this.BatchNo);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "volume", this.Volume);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "accessNo", this.AccessNo);

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "borrower", this.Borrower);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "borrowDate", this.BorrowDate);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "borrowPeriod", this.BorrowPeriod);

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "intact", this.Intact);

            // DomUtil.SetElementText(this.RecordDom.DocumentElement, "binding", this.Binding);
            try
            {
                DomUtil.SetElementInnerXml(this.RecordDom.DocumentElement,
                    "binding",
                    this.Binding);
            }
            catch (Exception ex)
            {
                string strError = "������Ϣ(<binding>Ԫ��)��ǶXMLƬ�� '" + this.Binding + "' ��ʽ����: " + ex.Message;
                throw new Exception(strError);
            }

            // DomUtil.SetElementText(this.RecordDom.DocumentElement, "operations", this.Operations);
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



        /// <summary>
        /// �����뽹�����õ��������������
        /// </summary>
        /// <param name="bSelectAll">�Ƿ�ͬʱȫѡ����</param>
        public void FocusBarcode(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.textBox_barcode.SelectAll();

            this.textBox_barcode.Focus();
        }

        /// <summary>
        /// �����뽹�����õ�״̬��������
        /// </summary>
        /// <param name="bSelectAll">�Ƿ�ͬʱȫѡ����</param>
        public void FocusState(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.checkedComboBox_state.SelectAll();

            this.checkedComboBox_state.Focus();
        }

        /// <summary>
        /// �����뽹�����õ�����ʱ����������
        /// </summary>
        /// <param name="bSelectAll">�Ƿ�ͬʱȫѡ����</param>
        public void FocusPublishTime(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.textBox_publishTime.SelectAll();

            this.textBox_publishTime.Focus();
        }

        /// <summary>
        /// �����뽹�����õ��ݲص���������
        /// </summary>
        /// <param name="bSelectAll">�Ƿ�ͬʱȫѡ����</param>
        public void FocusLocationString(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.comboBox_location.SelectAll();

            this.comboBox_location.Focus();
        }

        /// <summary>
        /// �����뽹�����õ�������������
        /// </summary>
        /// <param name="bSelectAll">�Ƿ�ͬʱȫѡ����</param>
        public void FocusSeller(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.comboBox_seller.SelectAll();

            this.comboBox_seller.Focus();
        }

#if NO
        internal override void ResetColor()
        {
            Color color = this.tableLayoutPanel_main.BackColor;
            this.label_barcode_color.BackColor = color;    // �ͱ���һ��
            this.label_state_color.BackColor = color;
            this.label_publishTime_color.BackColor = color;
            this.label_location_color.BackColor = color;
            this.label_seller_color.BackColor = color;
            this.label_source_color.BackColor = color;
            this.label_price_color.BackColor = color;
            this.label_bindingCost_color.BackColor = color;

            this.label_bookType_color.BackColor = color;
            this.label_registerNo_color.BackColor = color;
            this.label_comment_color.BackColor = color;
            this.label_mergeComment_color.BackColor = color;
            this.label_batchNo_color.BackColor = color;
            this.label_volume_color.BackColor = color;
            this.label_accessNo_color.BackColor = color;
            this.label_borrower_color.BackColor = color;
            this.label_borrowDate_color.BackColor = color;
            this.label_borrowPeriod_color.BackColor = color;

            this.label_intact_color.BackColor = color;
            this.label_binding_color.BackColor = color;
            this.label_operations_color.BackColor = color;

            this.label_recPath_color.BackColor = color;
            this.label_refID_color.BackColor = color;
        }
#endif

#if NO
        private void textBox_barcode_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                // this.label_barcode_color.BackColor = this.ColorChanged;
                Control control = sender as Control;
                EditLineState state = GetLineState(control);

                if (state == null)
                    state = new EditLineState();

                if (state.Changed == false)
                {
                    state.Changed = true;
                    SetLineDisplayState(control, state);
                }
                this.Changed = true;
            }
        }
#endif

        private void checkedComboBox_state_TextChanged(object sender, EventArgs e)
        {
#if NO
            if (m_bInInitial == false)
            {
                this.label_state_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
#endif

            Global.FilterValueList(this, (Control)sender);
        }

#if NO
        private void textBox_publishTime_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_publishTime_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }
#endif


        string _previousLocation = "";

        private void comboBox_location_TextChanged(object sender, EventArgs e)
        {
            this.comboBox_seller.Items.Clear();
            this.comboBox_source.Items.Clear();
            this.checkedComboBox_state.Items.Clear();
            this.comboBox_bookType.Items.Clear();

#if NO
            if (m_bInInitial == false)
            {
                this.label_location_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
#endif

            // �Ƚ��¾�����ֵ
            if (this.LocationStringChanged != null)
            {
                TextChangeEventArgs e1 = new TextChangeEventArgs();
                e1.OldText = this._previousLocation;
                e1.NewText = this.comboBox_location.Text;
                this.LocationStringChanged(this, e1);
            }

            _previousLocation = this.comboBox_location.Text;
        }

#if NO
        private void comboBox_seller_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_seller_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }

        }

        private void comboBox_source_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_source_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_price_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_price_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_bindingCost_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_bindingCost_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void comboBox_bookType_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_bookType_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_registerNo_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_registerNo_color.BackColor = this.ColorChanged;
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

        private void textBox_mergeComment_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_mergeComment_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_borrower_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_borrower_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_borrowDate_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_borrowDate_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_borrowPeriod_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_borrowPeriod_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_intact_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_intact_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_binding_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_binding_color.BackColor = this.ColorChanged;
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

        private void textBox_recPath_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_recPath_color.BackColor = this.ColorChanged;
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

        private void textBox_volume_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_volume_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_accessNo_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_accessNo_color.BackColor = this.ColorChanged;
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
            /// <summary>
            /// װ�������ߡ�����ͼ���һ�㹤����Ա�����޸ĵ��ֶ��⣬�������޸ľ�װ����Ϣ�������ߵ�
            /// </summary>
            Binding = 3,    // װ�������ߡ�����ͼ���һ�㹤����Ա�����޸ĵ��ֶ��⣬�������޸ľ�binding�������ߵ�
        }
#endif

        // �������ݵ�ʵ�����, ����ز������Ҫ�޸ĵ�ĳЩ������ΪReadOnly״̬
        /// <summary>
        /// ����ֻ��״̬
        /// </summary>
        /// <param name="strStyle">�������ֻ��״̬��
        /// "all" ��ʾȫ��Ϊֻ����
        /// "librarian" ��ʾֻ�м�¼·�����ο�ID�������ߡ��������ޡ���������Ϊֻ��(��������߲�Ϊ�գ���������ҲҪ����Ϊֻ��)������Ϊ�ɸ�д;
        /// "binding" ��ʾ���˳���ʱ�䡢�����ߡ��������ޡ��������ڡ���¼·�����ο�ID��װ����Ϣ��������Ϣ�������Ϣ����Ϊֻ������(��������߲�Ϊ�գ���������ҲҪ����Ϊֻ��)����������Ϊ�ɸ�д;
        /// </param>
        public override void SetReadOnly(string strStyle)
        {
            if (strStyle == "all")
            {
                this.checkedComboBox_state.Enabled = false;
                this.comboBox_location.Enabled = false;
                this.comboBox_bookType.Enabled = false;
                this.comboBox_seller.Enabled = false;
                this.comboBox_source.Enabled = false;

                this.textBox_publishTime.ReadOnly = true;
                this.textBox_barcode.ReadOnly = true;
                this.textBox_price.ReadOnly = true;
                this.textBox_bindingCost.ReadOnly = true;
                this.textBox_registerNo.ReadOnly = true;
                this.textBox_comment.ReadOnly = true;
                this.textBox_mergeComment.ReadOnly = true;
                this.textBox_batchNo.ReadOnly = true;
                this.textBox_volume.ReadOnly = true;
                this.textBox_accessNo.ReadOnly = true;
                this.textBox_borrower.ReadOnly = true;
                this.textBox_borrowDate.ReadOnly = true;
                this.textBox_borrowPeriod.ReadOnly = true;
                this.textBox_recPath.ReadOnly = true;
                this.textBox_refID.ReadOnly = true;
                this.textBox_intact.ReadOnly = true;
                this.textBox_binding.ReadOnly = true;
                this.textBox_operations.ReadOnly = true;

                this.button_getAccessNo.Enabled = false;
                return;
            }

            // �����ReadOnly
            this.textBox_barcode.ReadOnly = false;
            this.checkedComboBox_state.Enabled = true;
            this.textBox_publishTime.ReadOnly = false;
            this.comboBox_location.Enabled = true;
            this.comboBox_seller.Enabled = true;
            this.comboBox_source.Enabled = true;
            this.textBox_price.ReadOnly = false;
            this.textBox_bindingCost.ReadOnly = false;
            this.comboBox_bookType.Enabled = true;
            this.textBox_registerNo.ReadOnly = false;
            this.textBox_comment.ReadOnly = false;
            this.textBox_mergeComment.ReadOnly = false;
            this.textBox_batchNo.ReadOnly = false;
            this.textBox_volume.ReadOnly = false;
            this.textBox_accessNo.ReadOnly = false;

            this.textBox_borrower.ReadOnly = false;
            this.textBox_borrowDate.ReadOnly = false;
            this.textBox_borrowPeriod.ReadOnly = false;
            this.textBox_recPath.ReadOnly = false;

            this.textBox_refID.ReadOnly = false;
            this.textBox_intact.ReadOnly = false;
            this.textBox_binding.ReadOnly = false;
            this.textBox_operations.ReadOnly = false;

            this.button_getAccessNo.Enabled = true;

            if (strStyle == "librarian")
            {
                this.textBox_borrower.ReadOnly = true;
                this.textBox_borrowPeriod.ReadOnly = true;
                this.textBox_borrowDate.ReadOnly = true;
                this.textBox_recPath.ReadOnly = true;
                this.textBox_refID.ReadOnly = true; // 2009/6/2 new add

                if (this.textBox_borrower.Text != "")
                    this.textBox_barcode.ReadOnly = true;  // Ϊ�˲��Է�������API�������޸�Ϊfalse
                else
                    this.textBox_barcode.ReadOnly = false;
            }

            if (strStyle == "binding")
            {
                this.checkedComboBox_state.Enabled = false;

                this.textBox_publishTime.ReadOnly = true;
                this.textBox_borrower.ReadOnly = true;
                this.textBox_borrowPeriod.ReadOnly = true;
                this.textBox_borrowDate.ReadOnly = true;
                this.textBox_recPath.ReadOnly = true;
                this.textBox_refID.ReadOnly = true; // 2009/6/2 new add

                if (this.textBox_borrower.Text != "")
                    this.textBox_barcode.ReadOnly = true;  // Ϊ�˲��Է�������API�������޸�Ϊfalse
                else
                    this.textBox_barcode.ReadOnly = false;

                this.textBox_binding.ReadOnly = true;
                this.textBox_operations.ReadOnly = true;
                this.textBox_volume.ReadOnly = true;
            }
        }

        // �������Ѿ�����ΪReadOnly״̬��ĳЩ����Ϊ�ɸ�д״̬
        /// <summary>
        /// ����Ϊ���޸�״̬
        /// </summary>
        public override void SetChangeable()
        {
            this.textBox_borrower.ReadOnly = false;
            this.textBox_borrowPeriod.ReadOnly = false;
            this.textBox_borrowDate.ReadOnly = false;
            this.textBox_recPath.ReadOnly = false;
            this.textBox_refID.ReadOnly = false; // 2009/6/2 new add

            this.textBox_barcode.ReadOnly = false;
        }

        // ���һ���е������б�ֵ
        public List<string> GetLineValueList(EditLine line)
        {
            List<string> results = new List<string>();
            if (line.EditControl == this.comboBox_location)
            {
                comboBox_location_DropDown(this.comboBox_location, new EventArgs());
                foreach (string s in comboBox_location.Items)
                {
                    results.Add(s);
                }
                return results;
            }
            if (line.EditControl == this.checkedComboBox_state)
            {
                comboBox_state_DropDown(this.checkedComboBox_state, new EventArgs());
                foreach (string s in checkedComboBox_state.Items)
                {
                    results.Add(s);
                }
                return results;
            }
            if (line.EditControl == this.comboBox_bookType)
            {
                comboBox_bookType_DropDown(this.comboBox_bookType, new EventArgs());
                foreach (string s in comboBox_bookType.Items)
                {
                    results.Add(s);
                }
                return results;
            }
            return null;    // ��ʾ����û�������б�
        }

        int m_nInDropDown = 0;

        private void comboBox_location_DropDown(object sender, EventArgs e)
        {
            // ��ֹ���� 2009/2/23 new add
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

                    if (combobox == this.comboBox_location)
                        e1.TableName = "location";
                    else if (checked_combobox == this.checkedComboBox_state)
                        e1.TableName = "state";
                    else if (combobox == this.comboBox_bookType)
                        e1.TableName = "bookType";
                    else if (combobox == this.comboBox_seller)
                        e1.TableName = "orderSeller";
                    else if (combobox == this.comboBox_source)
                        e1.TableName = "orderSource";
                    else
                    {
                        Debug.Assert(false, "��֧�ֵ�sender");
                        return;
                    }

                    // this.GetValueTable(this, e1);
                    OnGetValueTable(sender, e1);

                    if (e1.values != null)
                    {
                        List<string> results = null;

                        string strLibraryCode = "";
                        string strPureName = "";

                        Global.ParseCalendarName(this.comboBox_location.Text,
                    out strLibraryCode,
                    out strPureName);

                        if (combobox != this.comboBox_location  // �ݲصص��б�Ҫ������
                            && String.IsNullOrEmpty(this.comboBox_location.Text) == false)
                        {
                            // ���˳����Ϲݴ������Щֵ�ַ���
                            results = Global.FilterValuesWithLibraryCode(strLibraryCode,
                                StringUtil.FromStringArray(e1.values));
                        }
                        else
                        {
                            results = StringUtil.FromStringArray(e1.values);
                        }

                        foreach (string s in results)
                        {
                            if (combobox != null)
                                combobox.Items.Add(s);
                            else
                                checked_combobox.Items.Add(s);
                        }
#if NO
                        for (int i = 0; i < e1.values.Length; i++)
                        {
                            if (combobox != null)
                                combobox.Items.Add(e1.values[i]);
                            else
                                checked_combobox.Items.Add(e1.values[i]);
                        }
#endif
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

        private void comboBox_state_DropDown(object sender, EventArgs e)
        {
            comboBox_location_DropDown(sender, e);

            // ����ȱʡֵ 2014/9/7
            if (this.checkedComboBox_state.Items.Count == 0)
            {
                List<string> values = StringUtil.SplitList("ע��,��ʧ,�ӹ���");
                foreach (string s in values)
                {
                    this.checkedComboBox_state.Items.Add(s);
                }
            }
        }

        private void comboBox_seller_DropDown(object sender, EventArgs e)
        {
            comboBox_location_DropDown(sender, e);
        }

        private void comboBox_source_DropDown(object sender, EventArgs e)
        {
            comboBox_location_DropDown(sender, e);
        }

        private void comboBox_bookType_DropDown(object sender, EventArgs e)
        {
            comboBox_location_DropDown(sender, e);
        }

        // �Ƚ��Լ���refControl�����ݲ��죬��������ɫ��ʾ�����ֶ�
        /// <summary>
        /// �Ƚ��Լ���refControl�����ݲ��죬��������ɫ��ʾ�����ֶ�
        /// </summary>
        /// <param name="r">Ҫ���Լ����бȽϵĿؼ�����</param>
        public override void HighlightDifferences(ItemEditControlBase r)
        {
            var refControl = r as EntityEditControl;

            if (this.Barcode != refControl.Barcode)
                this.label_barcode_color.BackColor = this.ColorDifference;

            if (this.State != refControl.State)
                this.label_state_color.BackColor = this.ColorDifference;

            if (this.PublishTime != refControl.PublishTime)
                this.label_publishTime_color.BackColor = this.ColorDifference;

            if (this.LocationString != refControl.LocationString)
                this.label_location_color.BackColor = this.ColorDifference;

            if (this.Seller != refControl.Seller)
                this.label_seller_color.BackColor = this.ColorDifference;

            if (this.Source != refControl.Source)
                this.label_source_color.BackColor = this.ColorDifference;

            if (this.Price != refControl.Price)
                this.label_price_color.BackColor = this.ColorDifference;

            if (this.BindingCost != refControl.BindingCost)
                this.label_bindingCost_color.BackColor = this.ColorDifference;

            if (this.BookType != refControl.BookType)
                this.label_bookType_color.BackColor = this.ColorDifference;

            if (this.RegisterNo != refControl.RegisterNo)
                this.label_registerNo_color.BackColor = this.ColorDifference;

            if (this.Comment != refControl.Comment)
                this.label_comment_color.BackColor = this.ColorDifference;

            if (this.MergeComment != refControl.MergeComment)
                this.label_mergeComment_color.BackColor = this.ColorDifference;

            if (this.BatchNo != refControl.BatchNo)
                this.label_batchNo_color.BackColor = this.ColorDifference;

            if (this.Volume != refControl.Volume)
                this.label_volume_color.BackColor = this.ColorDifference;

            if (this.AccessNo != refControl.AccessNo)
                this.label_accessNo_color.BackColor = this.ColorDifference;

            if (this.Borrower != refControl.Borrower)
                this.label_borrower_color.BackColor = this.ColorDifference;

            if (this.BorrowDate != refControl.BorrowDate)
                this.label_borrowDate_color.BackColor = this.ColorDifference;

            if (this.BorrowPeriod != refControl.BorrowPeriod)
                this.label_borrowPeriod_color.BackColor = this.ColorDifference;

            if (this.RecPath != refControl.RecPath)
                this.label_recPath_color.BackColor = this.ColorDifference;

            if (this.RefID != refControl.RefID)
                this.label_refID_color.BackColor = this.ColorDifference;

            if (this.Intact != refControl.Intact)
                this.label_intact_color.BackColor = this.ColorDifference;

            if (this.Binding != refControl.Binding)
                this.label_binding_color.BackColor = this.ColorDifference;

            if (this.Operations != refControl.Operations)
                this.label_operations_color.BackColor = this.ColorDifference;
        }


        private void DoKeyPress(object sender, KeyPressEventArgs e)
        {
            if (/*this.ControlKeyPress != null*/true)
            {
                ControlKeyPressEventArgs e1 = new ControlKeyPressEventArgs();
                e1.e = e;

                if (sender == (object)this.textBox_barcode)
                    e1.Name = "Barcode";
                else if (sender == (object)this.checkedComboBox_state)
                    e1.Name = "State";
                else if (sender == (object)this.textBox_publishTime)
                    e1.Name = "PublishTime";
                else if (sender == (object)this.comboBox_location)
                    e1.Name = "Location";
                else if (sender == (object)this.comboBox_seller)
                    e1.Name = "Seller";
                else if (sender == (object)this.comboBox_source)
                    e1.Name = "Source";
                else if (sender == (object)this.textBox_price)
                    e1.Name = "Price";
                else if (sender == (object)this.textBox_bindingCost)
                    e1.Name = "BindingCost";
                else if (sender == (object)this.textBox_comment)
                    e1.Name = "Comment";
                else if (sender == (object)this.textBox_borrower)
                    e1.Name = "Borrower";
                else if (sender == (object)this.textBox_borrowDate)
                    e1.Name = "BorrowDate";
                else if (sender == (object)this.textBox_borrowPeriod)
                    e1.Name = "BorrowPeriod";
                else if (sender == (object)this.textBox_recPath)
                    e1.Name = "RecPath";
                else if (sender == (object)this.comboBox_bookType)
                    e1.Name = "BookType";
                else if (sender == (object)this.textBox_registerNo)
                    e1.Name = "RegisterNo";
                else if (sender == (object)this.textBox_mergeComment)
                    e1.Name = "MergeComment";
                else if (sender == (object)this.textBox_batchNo)
                    e1.Name = "BatchNo";
                else if (sender == (object)this.textBox_refID)
                    e1.Name = "RefID";
                else if (sender == (object)this.textBox_volume)
                    e1.Name = "Volume";
                else if (sender == (object)this.textBox_accessNo)
                    e1.Name = "AccessNo";
                else if (sender == (object)this.textBox_intact)
                    e1.Name = "Intact";
                else if (sender == (object)this.textBox_binding)
                    e1.Name = "Binding";
                else if (sender == (object)this.textBox_operations)
                    e1.Name = "Operations";
                else 
                {
                    Debug.Assert(false, "δ֪�Ĳ���");
                    return;
                }

                // this.ControlKeyPress(sender, e1);
                OnControlKeyPress(sender, e1);
            }

        }

        private void DoKeyDown(object sender, KeyEventArgs e)
        {
            /*
            if (e.KeyCode == Keys.P)
                MessageBox.Show(this, "pppppppppp");
             * */

            if (/*this.ControlKeyDown != null*/true)
            {
                ControlKeyEventArgs e1 = new ControlKeyEventArgs();
                e1.e = e;

                if (sender == (object)this.textBox_barcode)
                    e1.Name = "Barcode";
                else if (sender == (object)this.checkedComboBox_state)
                    e1.Name = "State";
                else if (sender == (object)this.textBox_publishTime)
                    e1.Name = "PublishTime";
                else if (sender == (object)this.comboBox_location)
                    e1.Name = "Location";
                else if (sender == (object)this.comboBox_seller)
                    e1.Name = "Seller";
                else if (sender == (object)this.comboBox_source)
                    e1.Name = "Source";
                else if (sender == (object)this.textBox_price)
                    e1.Name = "Price";
                else if (sender == (object)this.textBox_bindingCost)
                    e1.Name = "BindingCost";
                else if (sender == (object)this.textBox_comment)
                    e1.Name = "Comment";
                else if (sender == (object)this.textBox_borrower)
                    e1.Name = "Borrower";
                else if (sender == (object)this.textBox_borrowDate)
                    e1.Name = "BorrowDate";
                else if (sender == (object)this.textBox_borrowPeriod)
                    e1.Name = "BorrowPeriod";
                else if (sender == (object)this.textBox_recPath)
                    e1.Name = "RecPath";
                else if (sender == (object)this.comboBox_bookType)
                    e1.Name = "BookType";
                else if (sender == (object)this.textBox_registerNo)
                    e1.Name = "RegisterNo";
                else if (sender == (object)this.textBox_mergeComment)
                    e1.Name = "MergeComment";
                else if (sender == (object)this.textBox_batchNo)
                    e1.Name = "BatchNo";
                else if (sender == (object)this.textBox_refID)
                    e1.Name = "RefID";
                else if (sender == (object)this.textBox_volume)
                    e1.Name = "Volume";
                else if (sender == (object)this.textBox_accessNo)
                    e1.Name = "AccessNo";
                else if (sender == (object)this.textBox_intact)
                    e1.Name = "Intact";
                else if (sender == (object)this.textBox_binding)
                    e1.Name = "Binding";
                else if (sender == (object)this.textBox_operations)
                    e1.Name = "Operations";
                else
                {
                    Debug.Assert(false, "δ֪�Ĳ���");
                    return;
                }



                // this.ControlKeyDown(sender, e1);
                OnControlKeyDown(sender, e1);
            }

        }

        private void ToolStripMenuItem_barcode_allowEdit_Click(object sender, EventArgs e)
        {
            if (this.textBox_barcode.ReadOnly == true)
            {
                this.textBox_barcode.ReadOnly = false;
            }
            else
            {
                this.textBox_barcode.ReadOnly = true;
            }
        }

        private void textBox_barcode_ReadOnlyChanged(object sender, EventArgs e)
        {
            if (this.textBox_barcode.ReadOnly == true)
            {
                this.ToolStripMenuItem_barcode_allowEdit.Checked = false;
            }
            else
            {
                this.ToolStripMenuItem_barcode_allowEdit.Checked = true;
            }
        }

#if NO
        private void comboBoxSizeChanged(object sender, EventArgs e)
        {
            if (!(sender is ComboBox))
                return;
            ((ComboBox)sender).Invalidate();
        }
#endif

        /// <summary>
        /// ���س�Ա�ؼ��� TableLayoutPanel ����
        /// </summary>
        public TableLayoutPanel ContentControl
        {
            get
            {
                return this.tableLayoutPanel_main;
            }
        }

        private void tableLayoutPanel_main_Paint(object sender, PaintEventArgs e)
        {
#if NO
            if (this.PaintContent != null)
                this.PaintContent(sender, e);
#endif
            OnPaintContent(this, e);    // sender 2014/10/4 �޸�
        }

        private void comboBox_bookType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
        }

        private void comboBox_seller_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
        }

        private void comboBox_source_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
        }

        private void comboBox_location_TextUpdate(object sender, EventArgs e)
        {
            string strText = this.comboBox_location.Text;

        }

        void OnLocationChanged()
        {
        }

        private void EntityEditControl_Paint(object sender, PaintEventArgs e)
        {

        }

        // int m_nIn = 0;

        private void tableLayoutPanel_main_SizeChanged(object sender, EventArgs e)
        {
            if (this.label_errorInfo != null)
            {
                // this.label_errorInfo.MaximumSize = new Size(this.tableLayoutPanel_main.ClientSize.Width, 2000);
            }
        }

        private void tableLayoutPanel_main_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;


#if NO
            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("ɾ��(&D)");
            contextMenu.MenuItems.Add(menuItem);
#endif
            if (this.AppendMenu != null)
            {
                ContextMenu contextMenu = new ContextMenu();
                // MenuItem menuItem = null;

                AppendMenuEventArgs e1 = new AppendMenuEventArgs();
                e1.ContextMenu = contextMenu;
                this.AppendMenu(this, e1);

                contextMenu.Show(sender as Control, new Point(e.X, e.Y));		
            }
        }


#if NO
        class MyTextBox : TextBox
        {
            public MyTextBox(): base ()
            {
                this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            }
        }
#endif
    }

    /// <summary>
    /// �ı������仯
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void TextChangeEventHandler(object sender,
    TextChangeEventArgs e);

    /// <summary>
    /// �ı������仯�¼��Ĳ���
    /// </summary>
    public class TextChangeEventArgs : EventArgs
    {
        /// <summary>
        /// �ɵ��ı�
        /// </summary>
        public string OldText = "";

        /// <summary>
        /// �µ��ı�
        /// </summary>
        public string NewText = "";
    }
}
