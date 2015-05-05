using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace dp2Circulation
{
    /// <summary>
    /// ��ʾ�ͱ༭���� MARC ��Ŀ��¼�ĶԻ���
    /// </summary>
    public partial class TwoBiblioDialog : Form
    {
        string m_strOldMessage = "";

        /// <summary>
        /// ���º���
        /// </summary>
        public TwoBiblioDialog()
        {
            InitializeComponent();
        }

        private void TwoBiblioDialog_Load(object sender, EventArgs e)
        {
            this.marcEditor1.Changed = false;
            this.marcEditor2.Changed = false;

            this.m_strOldMessage = this.MessageText;
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Դ MARC �ַ���
        /// </summary>
        public string MarcSource
        {
            get
            {
                return this.marcEditor1.Marc;
            }
            set
            {
                this.marcEditor1.Marc = value;
            }
        }

        /// <summary>
        /// Դ�Ƿ�Ϊֻ��
        /// </summary>
        public bool ReadOnlySource
        {
            get
            {
                return this.marcEditor1.ReadOnly;
            }
            set
            {
                this.marcEditor1.ReadOnly = value;
            }
        }

        /// <summary>
        /// Ŀ�� MARC �ַ���
        /// </summary>
        public string MarcTarget
        {
            get
            {
                return this.marcEditor2.Marc;
            }
            set
            {
                this.marcEditor2.Marc = value;
            }
        }

        /// <summary>
        /// Ŀ���Ƿ�Ϊֻ��
        /// </summary>
        public bool ReadOnlyTarget
        {
            get
            {
                return this.marcEditor2.ReadOnly;
            }
            set
            {
                this.marcEditor2.ReadOnly = value;
            }
        }

        /// <summary>
        /// ��Ϣ�ַ���
        /// </summary>
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

        /// <summary>
        /// Դ��ǩ����
        /// </summary>
        public string LabelSourceText
        {
            get
            {
                return this.label_left.Text;
            }
            set
            {
                this.label_left.Text = value;
            }
        }

        /// <summary>
        /// Ŀ���ǩ����
        /// </summary>
        public string LabelTargetText
        {
            get
            {
                return this.label_right.Text;
            }
            set
            {
                this.label_right.Text = value;
            }
        }

        /// <summary>
        /// �Ƿ�����ֱ���޸�Ŀ���¼
        /// </summary>
        public bool EditTarget
        {
            get
            {
                return this.checkBox_editTarget.Checked;
            }
            set
            {
                this.checkBox_editTarget.Checked = value;
            }
        }

        private void button_yes_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Yes;
            this.Close();

        }

        private void button_no_Click(object sender, EventArgs e)
        {
            if (this.checkBox_editTarget.Checked == true)
            {
                if (this.marcEditor2.Changed == true)
                {
                    DialogResult result = MessageBox.Show(this,
                        "�Ƿ�Ҫ�����ղŶԴ�����Ŀ���¼���ݵ��޸�?",
                        "TwoBiblioDialog",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.No)
                        return;
                }
            }

            this.DialogResult = DialogResult.No;
            this.Close();
        }

        private void checkBox_editTarget_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_editTarget.Checked == false)
            {
                this.marcEditor1.ReadOnly = false;
                this.marcEditor2.ReadOnly = true;

                this.button_yes.Text = "����(&O)";
                this.button_no.Text = "������(&N)";
                this.MessageText = this.m_strOldMessage;
            }
            else
            {
                this.marcEditor1.ReadOnly = true;
                this.marcEditor2.ReadOnly = false;

                this.button_yes.Text = "����(&S)";
                this.button_no.Text = "������(&N)";

                this.MessageText = "�����Ƿ�Ҫ�����Ŀ���¼���޸�?";
            }
        }
    }
}