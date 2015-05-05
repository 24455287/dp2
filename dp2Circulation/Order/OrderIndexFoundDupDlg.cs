using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace dp2Circulation
{
    internal partial class OrderIndexFoundDupDlg : Form
    {
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;
        public string OrderText = "";   // �ڵ�HTML��Ϣ
        public string BiblioText = "";  // �ֵ�HTML��Ϣ

        public string MessageText
        {
            get
            {
                return this.textBox_message.Text;
            }
            set
            {
                this.textBox_message.Text = value;
            }
        }

        public OrderIndexFoundDupDlg()
        {
            InitializeComponent();
        }

        private void OrderIndexFoundDupDlg_Load(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.OrderText) == false)
                Global.SetHtmlString(this.webBrowser_order,
                    this.OrderText,
                    this.MainForm.DataDir,
                    "orderindexdup_order");

            if (String.IsNullOrEmpty(this.BiblioText) == false)
                Global.SetHtmlString(this.webBrowser_biblio,
                    this.BiblioText,
                    this.MainForm.DataDir,
                    "orderindexdup_biblio");

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();

        }
    }
}