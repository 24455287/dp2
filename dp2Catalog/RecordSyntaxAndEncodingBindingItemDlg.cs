using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace dp2Catalog
{
    public partial class RecordSyntaxAndEncodingBindingItemDlg : Form
    {
        public RecordSyntaxAndEncodingBindingItemDlg()
        {
            InitializeComponent();
        }

        public string Encoding
        {
            get
            {
                return this.comboBox_encoding.Text;
            }
            set
            {
                this.comboBox_encoding.Text = value;
            }
        }

        public string RecordSyntax
        {
            get
            {
                return this.comboBox_recordSyntax.Text;
            }
            set
            {
                this.comboBox_recordSyntax.Text = value;
            }
        }

        private void RecordSyntaxAndEncodingBindingItemDlg_Load(object sender, EventArgs e)
        {
            Global.FillEncodingList(this.comboBox_encoding, true);
            /*
            // ����MARC-8���뷽ʽ
            this.comboBox_encoding.Items.Add("MARC-8");
             * */

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            if (this.comboBox_encoding.Text == "")
            {
                MessageBox.Show(this, "��δѡ�����뷽ʽ");
                return;
            }
            if (this.comboBox_recordSyntax.Text == "")
            {
                MessageBox.Show(this, "��δѡ����¼��ʽ");
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
    }
}