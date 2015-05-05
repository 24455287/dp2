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

namespace DigitalPlatform.CommonControl
{
    public partial class PersonAddressControl : UserControl
    {
        public string DbName = "";  // ���ݿ��������ڻ�ȡvalueTableֵʱ��Ϊ����

        XmlDocument RecordDom = null;

        bool m_bChanged = false;

        bool m_bInInitial = true;   // �Ƿ����ڳ�ʼ������֮��

        Color ColorChanged = Color.Yellow; // ��ʾ���ݸı������ɫ
        Color ColorDifference = Color.Blue; // ��ʾ�����в������ɫ

        /// <summary>
        /// ���ݷ����ı�
        /// </summary>
        public event ContentChangedEventHandler ContentChanged = null;

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        public event ControlKeyPressEventHandler ControlKeyPress = null;

        public event ControlKeyEventHandler ControlKeyDown = null;


        public PersonAddressControl()
        {
            InitializeComponent();
        }

        #region ���ݳ�Ա

        public string OldRecord = "";
        // public byte[] Timestamp = null;

        public string Zipcode
        {
            get
            {
                return this.textBox_zipcode.Text;
            }
            set
            {
                this.textBox_zipcode.Text = value;
            }
        }

        public string Address
        {
            get
            {
                return this.textBox_address.Text;
            }
            set
            {
                this.textBox_address.Text = value;
            }
        }

        public string PersonName
        {
            get
            {
                return this.textBox_name.Text;
            }
            set
            {
                this.textBox_name.Text = value;
            }
        }

        public string Department
        {
            get
            {
                return this.textBox_department.Text;
            }
            set
            {
                this.textBox_department.Text = value;
            }
        }

        public string Tel
        {
            get
            {
                return this.textBox_tel.Text;
            }
            set
            {
                this.textBox_tel.Text = value;
            }
        }

        public string Email
        {
            get
            {
                return this.textBox_email.Text;
            }
            set
            {
                this.textBox_email.Text = value;
            }
        }

        public string Bank
        {
            get
            {
                return this.textBox_bank.Text;
            }
            set
            {
                this.textBox_bank.Text = value;
            }
        }

        public string Accounts
        {
            get
            {
                return this.textBox_accounts.Text;
            }
            set
            {
                this.textBox_accounts.Text = value;
            }
        }

        public string PayStyle
        {
            get
            {
                return this.comboBox_payStyle.Text;
            }
            set
            {
                this.comboBox_payStyle.Text = value;
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

        #endregion  // ���ݳ�Ա

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
        /// <param name="strXml">��ַ XML</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int SetData(string strXml,
            out string strError)
        {
            strError = "";

            this.OldRecord = strXml;
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

            this.Zipcode = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "zipcode");
            this.Address = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "address");
            this.Department = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "department");
            this.PersonName = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "name");
            this.Tel = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "tel");
            this.Email = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "email");
            this.Bank = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "bank");
            this.Accounts = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "accounts");
            this.PayStyle = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "payStyle");
            this.Comment = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "comment");

            this.Initializing = false;

            this.Changed = false;

            return 0;
        }

        public void Clear()
        {
            this.Zipcode = "";
            this.Address = "";
            this.PersonName = "";
            this.Department = "";
            this.Tel = "";
            this.Email = "";
            this.Bank = "";
            this.Accounts = "";
            this.PayStyle = "";
            this.Comment = "";

            this.ResetColor();

            this.Changed = false;
        }

        public XmlDocument DataDom
        {
            get
            {
                // 2009/2/13 new add
                if (this.RecordDom == null)
                {
                    this.RecordDom = new XmlDocument();
                    this.RecordDom.LoadXml("<root />");
                }
                this.RefreshDom();
                return this.RecordDom;
            }
        }

        public void RefreshDom()
        {
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "zipcode", this.Zipcode);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "address", this.Address);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "department", this.Department);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "name", this.PersonName);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "tel", this.Tel);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "email", this.Email);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "bank", this.Bank);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "accounts", this.Accounts);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "payStyle", this.PayStyle);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "comment", this.Comment);
        }

        /// <summary>
        /// �������ʺ��ڱ���ļ�¼��Ϣ
        /// </summary>
        /// <param name="strXml">���ع���õĵ�ַ XML</param>
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

            this.RefreshDom();

            strXml = this.RecordDom.OuterXml;

            return 0;
        }

        public void FocusZipcode(bool bSelectAll)
        {
            if (bSelectAll == true)
                this.textBox_zipcode.SelectAll();

            this.textBox_zipcode.Focus();
        }

        public void ResetColor()
        {
            Color color = this.tableLayoutPanel_main.BackColor;
            this.label_zipcode_color.BackColor = color;    // �ͱ���һ��
            this.label_address_color.BackColor = color;
            this.label_name_color.BackColor = color;
            this.label_department_color.BackColor = color;
            this.label_tel_color.BackColor = color;
            this.label_email_color.BackColor = color;
            this.label_bank_color.BackColor = color;
            this.label_accounts_color.BackColor = color;
            this.label_payStyle_color.BackColor = color;
            this.label_comment_color.BackColor = color;
        }

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
                ComboBox combobox = (ComboBox)sender;
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

        /*
        // ��·����ȡ����������
        // parammeters:
        //      strPath ·��������"����ͼ��/3"
        public static string GetDbName(string strPath)
        {
            int nRet = strPath.LastIndexOf("/");
            if (nRet == -1)
                return strPath;

            return strPath.Substring(0, nRet).Trim();
        }
        */

        // ��ֹ���� 2009/7/19 new add
        int m_nInDropDown = 0;


        private void comboBox_payStyle_DropDown(object sender, EventArgs e)
        {
            // ��ֹ���� 2009/1/15 new add
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {

                ComboBox combobox = (ComboBox)sender;

                if (combobox.Items.Count == 0
                    && this.GetValueTable != null)
                {
                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    e1.DbName = this.DbName;

                    if (combobox == this.comboBox_payStyle)
                        e1.TableName = "payStyle";
                    else
                    {
                        Debug.Assert(false, "��֧�ֵ�sender");
                        return;
                    }

                    this.GetValueTable(this, e1);

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
        /// <param name="refControl">Ҫ���Լ����бȽϵĿؼ�����</param>
        public void HighlightDifferences(PersonAddressControl refControl)
        {
            if (this.Zipcode != refControl.Zipcode)
                this.label_zipcode_color.BackColor = this.ColorDifference;

            if (this.Address != refControl.Address)
                this.label_address_color.BackColor = this.ColorDifference;

            if (this.PersonName != refControl.PersonName)
                this.label_name_color.BackColor = this.ColorDifference;

            if (this.Department != refControl.Department)
                this.label_department_color.BackColor = this.ColorDifference;

            if (this.Tel != refControl.Tel)
                this.label_tel_color.BackColor = this.ColorDifference;

            if (this.Email != refControl.Email)
                this.label_email_color.BackColor = this.ColorDifference;

            if (this.Bank != refControl.Bank)
                this.label_bank_color.BackColor = this.ColorDifference;

            if (this.Accounts != refControl.Accounts)
                this.label_accounts_color.BackColor = this.ColorDifference;

            if (this.PayStyle != refControl.PayStyle)
                this.label_payStyle_color.BackColor = this.ColorDifference;

            if (this.Comment != refControl.Comment)
                this.label_comment_color.BackColor = this.ColorDifference;
        }

        private void DoKeyPress(object sender, KeyPressEventArgs e)
        {
            if (this.ControlKeyPress != null)
            {
                ControlKeyPressEventArgs e1 = new ControlKeyPressEventArgs();
                e1.e = e;
                if (sender == (object)this.textBox_zipcode)
                    e1.Name = "Zipcode";
                else if (sender == (object)this.textBox_address)
                    e1.Name = "Address";
                else if (sender == (object)this.textBox_name)
                    e1.Name = "Name";
                else if (sender == (object)this.textBox_department)
                    e1.Name = "Department";
                else if (sender == (object)this.textBox_tel)
                    e1.Name = "Tel";
                else if (sender == (object)this.textBox_email)
                    e1.Name = "Email";
                else if (sender == (object)this.textBox_bank)
                    e1.Name = "Bank";
                else if (sender == (object)this.textBox_accounts)
                    e1.Name = "Accounts";
                else if (sender == (object)this.comboBox_payStyle)
                    e1.Name = "PayStyle";
                else if (sender == (object)this.textBox_comment)
                    e1.Name = "Comment";
                else
                {
                    Debug.Assert(false, "δ֪�Ĳ���");
                    return;
                }

                this.ControlKeyPress(this, e1);
            }

        }

        private void DoKeyDown(object sender, KeyEventArgs e)
        {
            if (this.ControlKeyDown != null)
            {
                ControlKeyEventArgs e1 = new ControlKeyEventArgs();
                e1.e = e;
                if (sender == (object)this.textBox_zipcode)
                    e1.Name = "Zipcode";
                else if (sender == (object)this.textBox_address)
                    e1.Name = "Address";
                else if (sender == (object)this.textBox_name)
                    e1.Name = "Name";
                else if (sender == (object)this.textBox_department)
                    e1.Name = "Department";
                else if (sender == (object)this.textBox_tel)
                    e1.Name = "Tel";
                else if (sender == (object)this.textBox_email)
                    e1.Name = "Email";
                else if (sender == (object)this.textBox_bank)
                    e1.Name = "Bank";
                else if (sender == (object)this.textBox_accounts)
                    e1.Name = "Accounts";
                else if (sender == (object)this.comboBox_payStyle)
                    e1.Name = "PayStyle";
                else if (sender == (object)this.textBox_comment)
                    e1.Name = "Comment";
                else
                {
                    Debug.Assert(false, "δ֪�Ĳ���");
                    return;
                }

                this.ControlKeyDown(this, e1);
            }

        }
    }
}
