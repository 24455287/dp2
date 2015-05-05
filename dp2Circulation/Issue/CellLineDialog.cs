using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace dp2Circulation
{
    internal partial class CellLineDialog : Form
    {
        public CellLineDialog()
        {
            InitializeComponent();
        }

        private void CellLineDialog_Load(object sender, EventArgs e)
        {

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.comboBox_fieldName.Text) == true)
            {
                MessageBox.Show(this, "��δָ���ֶ���");
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public string FieldName
        {
            get
            {
                string strValue = this.comboBox_fieldName.Text;
                return GetLeftPart(strValue);
            }
            set
            {
                this.comboBox_fieldName.Text = value;
            }
        }

        public string Caption
        {
            get
            {
                string strValue = this.textBox_caption.Text;
                if (String.IsNullOrEmpty(strValue) == false)
                    return strValue;

                // ʵ�ڲ��У���combobox�Ҳ�
                strValue = this.comboBox_fieldName.Text;
                return GetRightPart(strValue);
            }
            set
            {
                this.textBox_caption.Text = value;
            }
        }

        private void comboBox_fieldName_SelectedIndexChanged(object sender, EventArgs e)
        {
            int nRet = this.comboBox_fieldName.Text.IndexOf("--");
            if (nRet == -1)
                return;

            string strRight = this.comboBox_fieldName.Text.Substring(nRet + 2).Trim();
            this.textBox_caption.Text = strRight;
        }

        static string GetRightPart(string strText)
        {
            int nRet = strText.IndexOf("--");
            if (nRet == -1)
                return "";

            return strText.Substring(nRet+2).Trim();
        }

        static string GetLeftPart(string strText)
        {
            int nRet = strText.IndexOf("--");
            if (nRet == -1)
                return strText;

            return strText.Substring(0, nRet).Trim();
        }

        public string[] GroupFieldNames = new string[] {
"seller -- ��������",
"source -- ������Դ",
"price -- ����۸�",
"range -- ʱ�䷶Χ",
"batchNo -- ���κ�",

"state -- ����״̬",
"range -- ʱ�䷶Χ",
"issueCount -- ��������",
"orderTime -- ����ʱ��",
"orderID -- ����ID",
"comment -- ע��",
"catalogNo -- ��Ŀ��",
"copy -- ������",
"distribute -- �ݲط���",
"class -- ������Ŀ",
"totalPrice -- �ܼ۸�",
"sellerAddres -- ���̵�ַ",
        };

        public void FillGroupFieldNameTable()
        {
            this.comboBox_fieldName.Items.Clear();
            this.comboBox_fieldName.Items.AddRange(this.GroupFieldNames);
        }
    }
}