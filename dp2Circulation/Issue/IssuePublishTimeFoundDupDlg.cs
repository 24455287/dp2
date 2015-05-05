using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace dp2Circulation
{
    // �ڱ༭�ڼ�¼���ڼǵ��Ĺ����У��������ظ��������ڵļ�¼��
    // ���Ի���������ʾ��Щ�ڼ�¼
    internal partial class IssuePublishTimeFoundDupDlg : Form
    {
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;
        public string IssueText = "";   // �ڵ�HTML��Ϣ
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

        public IssuePublishTimeFoundDupDlg()
        {
            InitializeComponent();
        }

        private void IssuePublishTimeFoundDupDlg_Load(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.IssueText) == false)
                Global.SetHtmlString(this.webBrowser_issue,
                    this.IssueText,
                    this.MainForm.DataDir,
                    "ossuepublishtimedup_item");

            if (String.IsNullOrEmpty(this.BiblioText) == false)
                Global.SetHtmlString(this.webBrowser_biblio,
                    this.BiblioText,
                    this.MainForm.DataDir,
                    "ossuepublishtimedup_item");

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}