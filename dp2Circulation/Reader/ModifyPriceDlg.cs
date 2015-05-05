using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

// using DigitalPlatform;

namespace dp2Circulation
{
    /// <summary>
    /// ����ʱ�޸Ľ��ĶԻ���
    /// </summary>
    internal partial class ModifyPriceDlg : Form
    {
        public string OldPrice = "";    // ����ľɼ۸����ڰ����ж�OK��ť��״̬

        public ModifyPriceDlg()
        {
            InitializeComponent();
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            // 2011/12/2
            if (string.IsNullOrEmpty(this.textBox_price.Text) == true)
            {
                MessageBox.Show(this, "����ַ�������Ϊ��");
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

        public string ID
        {
            get
            {
                return this.textBox_id.Text;
            }
            set
            {
                this.textBox_id.Text = value;
            }
        }

        public string Price
        {
            get
            {
                return this.textBox_price.Text;
            }
            set
            {
                this.textBox_price.Text = value;
            }
        }

        private void textBox_comment_TextChanged(object sender, EventArgs e)
        {
            SetOkButtonState();
        }

        void SetOkButtonState()
        {
            if (this.textBox_appendComment.Text != ""
                || this.textBox_price.Text != this.OldPrice)
                this.button_OK.Enabled = true;
            else
                this.button_OK.Enabled = false;
        }

        // Ҫ׷�ӵ���ע��
        public string AppendComment
        {
            get
            {
                return this.textBox_appendComment.Text;
            }
            set
            {
                this.textBox_appendComment.Text = value;
            }
        }

        // �Ѿ����ڵ�ע�ͣ�����ֱ���޸��Ѿ����ڵ�ע��
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

        private void textBox_price_Validating(object sender, CancelEventArgs e)
        {
            bool bRet = Global.HasQuanjiaoChars(this.textBox_price.Text);

            if (bRet == true)
            {
                MessageBox.Show(this, "�۸��ַ����в�����ȫ�ǵ����ֺ���ĸ");
                e.Cancel = true;
                return;
            }
        }

        private void button_insertDateTime_Click(object sender, EventArgs e)
        {
            /*
            int x=0;
            int y=0;
            API.GetEditCurrentCaretPos(
                    this.textBox_comment,
                    out x,
                    out y);
             * */

            this.textBox_appendComment.Paste(DateTime.Now.ToString());

            this.textBox_appendComment.Focus();

        }

        private void button_insertDateTime_MouseHover(object sender, EventArgs e)
        {
            this.toolTip_usage.Show("���뵱ǰ����ʱ��", this.button_newComment_insertDateTime);
        }

        private void textBox_price_TextChanged(object sender, EventArgs e)
        {
            SetOkButtonState();
        }

        private void textBox_appendComment_Validated(object sender, EventArgs e)
        {
            
        }

        private void textBox_appendComment_Validating(object sender, CancelEventArgs e)
        {
            if (this.textBox_appendComment.Text.IndexOfAny(new char[] { '<', '>' }) != -1)
            {
                MessageBox.Show(this, "ע�������в������������ '<' '>'");
                e.Cancel = true;
            }
        }
    }
}