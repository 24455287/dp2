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
    /// ��Ŀ��¼���Ϊ �Ի���
    /// </summary>
    internal partial class BiblioSaveToDlg : Form
    {
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        public string CurrentBiblioRecPath = "";    // ��ǰ��Ŀ��¼��·��

        bool m_bSavedBuildLink = false; // �����ʾ��BuildLinkֵ

        int m_nManual = 0;  // ���Ϊ0����ʾ�����ֶ���ѡ��������ǳ����ڲ�ȥ�ı�checkedֵ

        public BiblioSaveToDlg()
        {
            InitializeComponent();
        }

        private void BiblioSaveToDlg_Load(object sender, EventArgs e)
        {
            this.m_bSavedBuildLink = this.BuildLink;

            comboBox_biblioDbName_TextChanged(null, null);

            TrySetRecPathFromClipboard();
        }

        private void BiblioSaveToDlg_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.comboBox_biblioDbName.Text) == true)
            {
                MessageBox.Show(this, "��δָ����Ŀ����");
                return;
            }

            if (String.IsNullOrEmpty(this.textBox_recordID.Text) == true)
            {
                MessageBox.Show(this, "��δָ����¼ID");
            }


            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public string MessageText
        {
            get
            {
                return this.label_message.Text;
            }
            set
            {
                this.label_message.Text = value;
            }
        }

        public string RecPath
        {
            get
            {
                return this.comboBox_biblioDbName.Text + "/" + this.textBox_recordID.Text;
            }
            set
            {
                int nRet = value.IndexOf("/");
                if (nRet == -1)
                {
                    this.comboBox_biblioDbName.Text = value;
                }
                else
                {
                    this.comboBox_biblioDbName.Text = value.Substring(0, nRet);
                    this.textBox_recordID.Text = value.Substring(nRet+1);
                }
            }
        }

        public string RecID
        {
            get
            {
                return this.textBox_recordID.Text;
            }
            set
            {
                this.textBox_recordID.Text = value;
            }
        }

        public bool BuildLink
        {
            get
            {
                return this.checkBox_buildLink.Checked;
            }
            set
            {
                this.checkBox_buildLink.Checked = value;
            }
        }

        public bool CopyChildRecords
        {
            get
            {
                return this.checkBox_copyChildRecords.Checked;
            }
            set
            {
                this.checkBox_copyChildRecords.Checked = value;
            }
        }

        public bool EnableCopyChildRecords
        {
            get
            {
                return this.checkBox_copyChildRecords.Enabled;
            }
            set
            {
                this.checkBox_copyChildRecords.Enabled = value;
            }
        }

        private void comboBox_biblioDbName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_biblioDbName.Items.Count > 0)
                return;

            if (this.MainForm.BiblioDbProperties == null)
                return;

            for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
            {
                BiblioDbProperty property = this.MainForm.BiblioDbProperties[i];
                this.comboBox_biblioDbName.Items.Add(property.DbName);
            }
        }

        // combobox�ڵĿ���ѡ��仯�󣬼�¼ID�仯Ϊ"?"
        private void comboBox_biblioDbName_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.textBox_recordID.Text = "?";
        }

        private void textBox_recordID_Validating(object sender, CancelEventArgs e)
        {
            if (this.textBox_recordID.Text != "?"
                && StringUtil.IsPureNumber(this.textBox_recordID.Text) == false)
            {
                MessageBox.Show(this, "��¼ID '"+this.textBox_recordID.Text+"' ���Ϸ�������Ϊ'?'������(ע:����Ϊ���)");
                e.Cancel = true;
                return;
            }
        }

        private void comboBox_biblioDbName_TextChanged(object sender, EventArgs e)
        {
            string strError = "";

            // ������ǰ�ļ�¼·����IDΪ�ʺţ���ζ�Ų�֪��Ŀ��·��������޷�����Ŀ���ϵ
            // ����Ҫ���Ŀ��·����IDӦ��Ϊ�ʺ�
            int nRet = this.MainForm.CheckBuildLinkCondition(
                    this.RecPath,   // Ҫ���ȥ������
                    this.CurrentBiblioRecPath,  // ���ǰ������
                    true,
                    out strError);
            // �����Ƿ���ҪEnable/Diable����checkbox
            if (nRet == 1)
            {
                // �����ָ�����Ĺ�ѡ��������ܵĻ�
                if (this.m_bSavedBuildLink == true)
                {
                    this.m_nManual++;
                    this.checkBox_buildLink.Checked = true;
                    this.m_nManual--;
                }

                this.checkBox_buildLink.Enabled = true;

                this.label_buildLinkMessage.Text = "";
            }
            else
            {

                this.m_nManual++;
                this.checkBox_buildLink.Checked = false;
                this.m_nManual--;

                this.checkBox_buildLink.Enabled = false;

                this.label_buildLinkMessage.Text = strError;
            }

            return;
            /*
        ERROR1:
            MessageBox.Show(this, strError);
             * */
        }

        private void checkBox_buildLink_CheckedChanged(object sender, EventArgs e)
        {
            if (this.m_nManual == 0)
            {
                // ����ֶ��ڽ�����ȷ������off
                if (this.checkBox_buildLink.Checked == false
                    && this.m_bSavedBuildLink == true)
                    this.m_bSavedBuildLink = false; // ���ټ�ָֻ�
            }
        }

        void TrySetRecPathFromClipboard()
        {
            IDataObject ido = Clipboard.GetDataObject();
            if (ido.GetDataPresent(DataFormats.UnicodeText) == false)
                return;

            string strText = (string)ido.GetData(DataFormats.UnicodeText);
            if (string.IsNullOrEmpty(strText) == true)
                return;

            if (IsRecPath(strText) == false)
                return;

            this.RecPath = strText;
        }

        // �ж�һ���ַ����Ƿ�Ϊ��¼·��
        static bool IsRecPath(string strText)
        {
            if (strText.IndexOf("/") == -1)
                return false;
            string strLeft = "";
            string strRight = "";
            StringUtil.ParseTwoPart(strText, "/", out strLeft, out strRight);
            if (string.IsNullOrEmpty(strLeft) == true)
                return false;
            if (strRight == "?")
                return true;
            if (StringUtil.IsPureNumber(strRight) == false)
                return false;
            return true;
        }
    }
}