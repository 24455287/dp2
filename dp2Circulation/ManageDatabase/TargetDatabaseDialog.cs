using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DigitalPlatform.Text;

namespace dp2Circulation
{
    /// <summary>
    /// Ŀ������� �Ի���
    /// </summary>
    internal partial class TargetDatabaseDialog : Form
    {
        // public DupCfgDialog DupCfgDialog = null;
        // public string AllBiblioDbInfoXml = "";
        public List<string> BiblioDbNames = null;

        public List<string> UsedDbNames = null;

        public TargetDatabaseDialog()
        {
            InitializeComponent();
        }

        private void TargetDatabaseDialog_Load(object sender, EventArgs e)
        {

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            if (this.comboBox_databaseName.Text == "")
            {
                MessageBox.Show(this, "��δ�������ݿ���");
                return;
            }

            if (this.textBox_threshold.Text == "")
            {
                MessageBox.Show(this, "��δ������ֵ");
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

        public string Threshold
        {
            get
            {
                return this.textBox_threshold.Text;
            }
            set
            {
                this.textBox_threshold.Text = value;
            }
        }

#if NOOOOOOOOOOOOOOOOOO
        private void button_findDatabaseName_Click(object sender, EventArgs e)
        {
            /*
            // ��Ҫ��DTLP��Դ�Ի�����Ҫ��DtlpChannels��֧��
            if (this.DupCfgDialog == null)
            {
                MessageBox.Show(this, "DupCfgDialog��ԱΪ�գ��޷���ѡ��Ŀ�����ݿ�ĶԻ���");
                return;
            }

            GetDtlpResDialog dlg = new GetDtlpResDialog();

            dlg.Text = "��ѡ��Ŀ�����ݿ�";
            dlg.Initial(this.DupCfgDialog.DtlpChannels,
                this.DupCfgDialog.DtlpChannel);
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.Path = this.textBox_databaseName.Text;
            dlg.EnabledIndices = new int[] { DtlpChannel.TypeStdbase };
            dlg.ShowDialog(this);

            this.textBox_databaseName.Text = dlg.Path;
             * */

            GetOpacMemberDatabaseNameDialog dlg = new GetOpacMemberDatabaseNameDialog();
                    MainForm.SetControlFont(dlg, this.Font, false);

            dlg.Text = "��ѡ��Ŀ�����ݿ�";
            dlg.SelectedDatabaseName = this.textBox_databaseName.Text;
            dlg.AllDatabaseInfoXml = AllBiblioDbInfoXml;
            dlg.ExcludingDbNames = used_dbnames;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.textBox_databaseName.Text = dlg.SelectedDatabaseName;
        }
#endif

        private void textBox_threshold_Validating(object sender,
            CancelEventArgs e)
        {
            if (StringUtil.IsPureNumber(this.textBox_threshold.Text) == false)
            {
                MessageBox.Show(this, "��ֵ����Ϊ������");
                e.Cancel = true;
            }
        }

        private void comboBox_databaseName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_databaseName.Items.Count == 0
                && this.BiblioDbNames != null)
            {
                for (int i = 0; i < this.BiblioDbNames.Count; i++)
                {
                    string strDbName = this.BiblioDbNames[i];

                    if (this.UsedDbNames != null)
                    {
                        // �ù������ݿ�����Ҫ��������
                        if (this.UsedDbNames.IndexOf(strDbName) == -1)
                            this.comboBox_databaseName.Items.Add(strDbName);
                    }
                    else
                        this.comboBox_databaseName.Items.Add(strDbName);
                }
            }
        }
    }
}