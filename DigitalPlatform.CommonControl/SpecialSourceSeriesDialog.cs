using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace DigitalPlatform.CommonControl
{
    /// <summary>
    /// �ڿ����ⶩ����������� �Ի���
    /// 
    /// </summary>
    public partial class SpecialSourceSeriesDialog : Form
    {
        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        public string DbName = "";  // ���ڻ��ֵ�б�

        // ��ַXMLƬ��
        public string AddressXml = "";

        public string Seller = "";
        public string Source = "";

        public SpecialSourceSeriesDialog()
        {
            InitializeComponent();
        }

        private void SpecialSourceSeriesDialog_Load(object sender, EventArgs e)
        {
            string strError = "";
            // �ۺϸ�����Ϣ������״̬
            int nRet = SetType(out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);

        }

        private void SpecialSourceSeriesDialog_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void SpecialSourceSeriesDialog_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";
            // ����û�ѡ���״̬
            int nRet = GetType(out strError);
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

        // ����û�ѡ���״̬
        int GetType(out string strError)
        {
            strError = "";

            if (this.personAddressControl.Changed == true)
            {
                // ��ñ༭�������
                try
                {
                    this.AddressXml = this.personAddressControl.DataDom.DocumentElement.OuterXml;
                }
                catch (Exception ex)
                {
                    strError = "���AddressXml����ʱ����: " + ex.Message;
                    return -1;
                }
            }

            // ��ͨ����
            if (this.comboBox_specialSource.Text == "��ͨ")
            {
                if (this.comboBox_seller.Text == "")
                {
                    strError = "��ͨ��������ʱ������������Ϊ��";
                    return -1;
                }

                if (this.comboBox_source.Text == "")
                {
                    strError = "��ͨ��������ʱ��������Դ����Ϊ��";
                    return -1;
                }

                ///

                if (this.comboBox_seller.Text == "ֱ��")
                {
                    strError = "��ͨ��������ʱ������������Ϊ '" + this.comboBox_seller.Text + "'";
                    return -1;
                }

                if (this.comboBox_seller.Text == "����")
                {
                    strError = "��ͨ��������ʱ������������Ϊ '" + this.comboBox_seller.Text + "'";
                    return -1;
                }

                if (this.comboBox_seller.Text == "��")
                {
                    strError = "��ͨ��������ʱ������������Ϊ '" + this.comboBox_seller.Text + "'";
                    return -1;
                }

                this.Source = this.comboBox_source.Text;
                this.Seller = this.comboBox_seller.Text;
                // ��ַ����
                return 0;
            }

            // ֱ��
            if (this.comboBox_specialSource.Text == "ֱ��")
            {
                if (this.comboBox_source.Text == "")
                {
                    strError = "ֱ��ʱ��������Դ����Ϊ��";
                    return -1;
                }

                if (this.comboBox_seller.Text == "����")
                {
                    strError = "ֱ��ʱ������������Ϊ '" + this.comboBox_seller.Text + "'";
                    return -1;
                }

                if (this.comboBox_seller.Text == "��")
                {
                    strError = "ֱ��ʱ������������Ϊ '" + this.comboBox_seller.Text + "'";
                    return -1;
                }

                this.Source = this.comboBox_source.Text;

                this.Seller = "ֱ��";
                // TODO: �ϳɵ�ַ
                return 0;
            }

            // ����
            if (this.comboBox_specialSource.Text == "����")
            {
                this.Seller = "����";
                this.Source = "";
                return 0;
            }

            // ��
            if (this.comboBox_specialSource.Text == "��")
            {
                this.Seller = "��";
                this.Source = "";
                return 0;
            }

            strError = "���Ϸ����������� '" + this.comboBox_specialSource.Text + "'";
            return -1;
        }

        // �ۺϸ�����Ϣ������״̬
        int SetType(out string strError)
        {
            strError = "";
            int nRet = 0;

            this.comboBox_seller.Text = this.Seller;
            this.comboBox_source.Text = this.Source;

            // װ���ַ��Ϣ
            if (String.IsNullOrEmpty(this.AddressXml) == false)
            {
                nRet = this.personAddressControl.SetData(this.AddressXml,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            if (this.Seller == "ֱ��")
            {
                this.comboBox_specialSource.Text = "ֱ��";
                return 0;
            }

            if (this.Seller == "����")
            {
                this.comboBox_specialSource.Text = "����";
                return 0;
            }

            if (this.Seller == "��")
            {
                this.comboBox_specialSource.Text = "��";
                return 0;
            }

            this.comboBox_specialSource.Text = "��ͨ";
            return 0;
        }

        // ��ֹ���� 2009/7/19 new add
        int m_nInDropDown = 0;

        private void comboBox_DropDown(object sender, EventArgs e)
        {
            // ��ֹ���� 2009/7/19 new add
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

                    if (combobox == this.comboBox_source)
                        e1.TableName = "orderSource";
                    else if (combobox == this.comboBox_seller)
                        e1.TableName = "orderSeller";
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

        private void comboBox_seller_DropDown(object sender, EventArgs e)
        {
            comboBox_DropDown(sender, e);
        }

        private void comboBox_source_DropDown(object sender, EventArgs e)
        {
            comboBox_DropDown(sender, e);
        }

        private void comboBox_specialSource_TextChanged(object sender, EventArgs e)
        {
            if (this.comboBox_specialSource.Text == "ֱ��")
            {
                this.comboBox_seller.Enabled = false;
                this.comboBox_seller.Visible = false;
                this.label_seller.Visible = false;

                this.comboBox_source.Enabled = true;
                this.comboBox_source.Visible = true;
                this.label_source.Visible = true;
            }
            else if (this.comboBox_specialSource.Text == "����")
            {
                this.comboBox_seller.Enabled = false;
                this.comboBox_seller.Visible = false;
                this.label_seller.Visible = false;

                this.comboBox_source.Enabled = false;
                this.comboBox_source.Visible = false;
                this.label_source.Visible = false;
            }
            else if (this.comboBox_specialSource.Text == "��")
            {
                this.comboBox_seller.Enabled = false;
                this.comboBox_seller.Visible = false;
                this.label_seller.Visible = false;

                this.comboBox_source.Enabled = false;
                this.comboBox_source.Visible = false;
                this.label_source.Visible = false;
            }
            else if (this.comboBox_specialSource.Text == "��ͨ")
            {
                this.comboBox_seller.Enabled = true;
                this.comboBox_seller.Visible = true;
                this.label_seller.Visible = true;

                this.comboBox_source.Enabled = true;
                this.comboBox_source.Visible = true;
                this.label_source.Visible = true;

                if (this.comboBox_seller.Text == "ֱ��")
                    this.comboBox_seller.Text = "";

                if (this.comboBox_seller.Text == "����"
                    || this.comboBox_seller.Text == "��")
                    this.comboBox_seller.Text = "";
            }
            else
            {
                // �������Ϸ���������
                this.comboBox_seller.Enabled = false;
                this.comboBox_seller.Visible = false;
                this.label_seller.Visible = false;

                this.comboBox_source.Enabled = false;
                this.comboBox_source.Visible = false;
                this.label_source.Visible = false;
            }

        }

    }
}