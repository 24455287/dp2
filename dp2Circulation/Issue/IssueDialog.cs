using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DigitalPlatform.LibraryServer;

namespace dp2Circulation
{
    internal partial class IssueDialog : Form
    {
        // public object Tag = null;   // Я���κζ���
        /// <summary>
        /// �����¼�
        /// </summary>
        public event CheckDupEventHandler CheckDup = null;

        public IssueDialog()
        {
            InitializeComponent();
        }

        private void IssueDialog_Load(object sender, EventArgs e)
        {

        }

        private void IssueDialog_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void IssueDialog_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.textBox_publishTime.Text == "")
            {
                strError = "��δ�������ʱ��";
                goto ERROR1;
            }

            // ������ʱ���ʽ�Ƿ���ȷ
            int nRet = LibraryServerUtil.CheckSinglePublishTime(this.textBox_publishTime.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (this.textBox_issue.Text == "")
            {
                strError = "��δ�����ں�";
                goto ERROR1;
            }

            // ����ںŸ�ʽ�Ƿ���ȷ
            nRet = VolumeInfo.CheckIssueNo(
                "�ں�",
                this.textBox_issue.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // ������ںŸ�ʽ�Ƿ���ȷ
            if (String.IsNullOrEmpty(this.textBox_zong.Text) == false)
            {
                nRet = VolumeInfo.CheckIssueNo(
                    "���ں�",
                    this.textBox_zong.Text,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            // ����Ÿ�ʽ�Ƿ���ȷ
            if (String.IsNullOrEmpty(this.textBox_volume.Text) == false)
            {
                nRet = VolumeInfo.CheckIssueNo(
                    "���",
                    this.textBox_volume.Text,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            if (this.CheckDup != null)
            {
                CheckDupEventArgs e1 = new CheckDupEventArgs();
                e1.PublishTime = this.PublishTime;
                e1.Issue = this.Issue;
                e1.Zong = this.Zong;
                e1.Volume = this.Volume;
                e1.EnsureVisible = true;
                this.CheckDup(this, e1);

                if (e1.DupIssues.Count > 0)
                {
                    // ���ظ����ڹ�����Ұ

                    MessageBox.Show(this, e1.DupInfo);
                    return;
                }

                if (e1.WarningIssues.Count > 0)
                {
                    // ������ĵ��ڹ�����Ұ

                    DialogResult dialog_result = MessageBox.Show(this,
            "����: " + e1.WarningInfo + "\r\n\r\n�Ƿ����?\r\n\r\n(OK: ����ᾯ�棬�������к�������; Cancel: ���ضԻ�������޸�",
            "BindingControls",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
                    if (dialog_result == DialogResult.Cancel)
                        return;
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

        public string PublishTime
        {
            get
            {
                return this.textBox_publishTime.Text;
            }
            set
            {
                this.textBox_publishTime.Text = value;
            }
        }

        public string Issue
        {
            get
            {
                return this.textBox_issue.Text;
            }
            set
            {
                this.textBox_issue.Text = value;
            }
        }

        public string Zong
        {
            get
            {
                return this.textBox_zong.Text;
            }
            set
            {
                this.textBox_zong.Text = value;
            }
        }

        public string Volume
        {
            get
            {
                return this.textBox_volume.Text;
            }
            set
            {
                this.textBox_volume.Text = value;
            }
        }

        public string EditComment
        {
            get
            {
                return this.textBox_editComment.Text;
            }
            set
            {
                this.textBox_editComment.Text = value;
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

    /// <summary>
    /// �����¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    internal delegate void CheckDupEventHandler(object sender,
        CheckDupEventArgs e);

    /// <summary>
    /// �����¼��Ĳ���
    /// </summary>
    internal class CheckDupEventArgs : EventArgs
    {
        // [in]
        /// <summary>
        /// [in] ����ʱ��
        /// </summary>
        public string PublishTime = "";
        /// <summary>
        /// [in] �����ں�
        /// </summary>
        public string Issue = "";
        /// <summary>
        /// [in] ���ں�
        /// </summary>
        public string Zong = "";
        /// <summary>
        /// [in] ���
        /// </summary>
        public string Volume = "";

        /// <summary>
        /// [in] �Ƿ�Ҫȷ��ѡ��������ɼ�
        /// </summary>
        public bool EnsureVisible = false;

        // [out]
        /// <summary>
        /// [out] �����ظ���Ϣ
        /// </summary>
        public string DupInfo = "";
        /// <summary>
        /// [out] ���ط����ظ����ڶ��󼯺�
        /// </summary>
        public List<IssueBindingItem> DupIssues = new List<IssueBindingItem>();
        /// <summary>
        /// [out] ���ؾ�����Ϣ
        /// </summary>
        public string WarningInfo = "";
        /// <summary>
        /// [out] ���ط���������ڶ��󼯺�
        /// </summary>
        public List<IssueBindingItem> WarningIssues = new List<IssueBindingItem>();
    }
}