using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Xml;

namespace dp2Circulation
{
    /// <summary>
    /// ָ����֪���ݿ� �� ȱʡ���ط�ʽ�� �Ի���
    /// </summary>
    internal partial class DefaultProjectDialog : Form
    {
        // public DupCfgDialog DupCfgDialog = null;
        public List<string> BiblioDbNames = null;

        public XmlDocument dom = null;

        public DefaultProjectDialog()
        {
            InitializeComponent();
        }

        private void DefaultProjectDialog_Load(object sender, EventArgs e)
        {
            // ���û�и������ݿ�����������Ӧ�����Ա༭
            if (String.IsNullOrEmpty(this.DatabaseName) == true)
            {
                this.comboBox_databaseName.Enabled = true;
            }
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void button_findProjectName_Click(object sender, EventArgs e)
        {
            GetProjectNameDialog dlg = new GetProjectNameDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.Dom = this.dom;
            dlg.ProjectName = this.textBox_defaultProjectName.Text;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.textBox_defaultProjectName.Text = dlg.ProjectName;
        }

        public string DatabaseName
        {
            get
            {
                return this.comboBox_databaseName.Text;
            }
            set
            {
                this.comboBox_databaseName.Text = value;
            }
        }

        public string DefaultProjectName
        {
            get
            {
                return this.textBox_defaultProjectName.Text;
            }
            set
            {
                this.textBox_defaultProjectName.Text = value;
            }
        }

        /*
        private void button_findDatabaseName_Click(object sender, EventArgs e)
        {
            // ��Ҫ��DTLP��Դ�Ի�����Ҫ��DtlpChannels��֧��
            if (this.DupCfgDialog == null)
            {
                MessageBox.Show(this, "DupCfgDialog��ԱΪ�գ��޷���ѡ��Ŀ�����ݿ�ĶԻ���");
                return;
            }

            GetDtlpResDialog dlg = new GetDtlpResDialog();

            dlg.Text = "��ѡ�����ݿ�";
            dlg.Initial(this.DupCfgDialog.DtlpChannels,
                this.DupCfgDialog.DtlpChannel);
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.Path = this.textBox_databaseName.Text;
            dlg.EnabledIndices = new int[] { DtlpChannel.TypeStdbase };
            dlg.ShowDialog(this);

            this.textBox_databaseName.Text = dlg.Path;
        }
         * */

        private void comboBox_databaseName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_databaseName.Items.Count == 0
    && this.BiblioDbNames != null)
            {
                for (int i = 0; i < this.BiblioDbNames.Count; i++)
                {
                    string strDbName = this.BiblioDbNames[i];
                    this.comboBox_databaseName.Items.Add(strDbName);
                }
            }

        }


    }
}