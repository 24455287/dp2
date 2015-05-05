using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace dp2Circulation
{
    internal partial class ZhongcihaoGroupDialog : Form
    {
        public string AllZhongcihaoDatabaseInfoXml = "";// �����������ִκſ��XMLƬ��
        public List<string> ExcludingDbNames = new List<string>();   // Ҫ�ų��ġ��Ѿ���ʹ���˵��ִκſ���

        public ZhongcihaoGroupDialog()
        {
            InitializeComponent();
        }

        private void ZhongcihaoGroupDialog_Load(object sender, EventArgs e)
        {

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.textBox_groupName.Text == "")
            {
                strError = "��δ��������";
                goto ERROR1;
            }

            if (this.textBox_zhongcihaoDbName.Text == "")
            {
                strError = "��δ�����ִκſ���";
                goto ERROR1;
            }

            // ���Ի����еõ����ִκſ⣬�ǲ��Ǳ����ù����ִκſ⣿
            if (this.ExcludingDbNames != null)
            {
                if (this.ExcludingDbNames.IndexOf(this.textBox_zhongcihaoDbName.Text) != -1)
                {
                    strError = "����ָ�����ִκſ� '" + this.textBox_zhongcihaoDbName.Text + "' �Ѿ���������ʹ�ù���";
                    goto ERROR1;
                }
            }


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

        private void button_getZhongcihaoDbName_Click(object sender, EventArgs e)
        {
            GetOpacMemberDatabaseNameDialog dlg = new GetOpacMemberDatabaseNameDialog();
            MainForm.SetControlFont(dlg, this.Font, false);
            // dlg.Text = "";
            dlg.AllDatabaseInfoXml = this.AllZhongcihaoDatabaseInfoXml;    // �����������ִκſ��XMLƬ��
            dlg.ExcludingDbNames = this.ExcludingDbNames;   // Ҫ�ų��ġ��Ѿ���ʹ���˵��ִκſ���
            dlg.StartPosition = FormStartPosition.CenterScreen;

            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.textBox_zhongcihaoDbName.Text = dlg.SelectedDatabaseName;
        }

        // ����
        public string GroupName
        {
            get
            {
                return this.textBox_groupName.Text;
            }
            set
            {
                this.textBox_groupName.Text = value;
            }
        }

        // �ִκſ���
        public string ZhongcihaoDbName
        {
            get
            {
                return this.textBox_zhongcihaoDbName.Text;
            }
            set
            {
                this.textBox_zhongcihaoDbName.Text = value;
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
    }
}