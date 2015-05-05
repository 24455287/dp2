using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.Xml;

namespace dp2Circulation
{
    // ���ɲ�����ʾ�����Ƿ�ɹ�����Ϣ�Ի���
    // �к��������״̬
    internal partial class ChargingInfoDlg : Form
    {
        public ChargingInfoHost Host = null;

        const int WM_SWITCH_FOCUS = API.WM_USER + 200;

        public double DelayOpacity = 1.0;

        public bool Password
        {
            get
            {
                return this.textBox_fastInputText.PasswordChar == '*';
            }
            set
            {
                if (value == true)
                    this.textBox_fastInputText.PasswordChar = '*';
                else
                    this.textBox_fastInputText.PasswordChar = (char)0;
            }
        }

        public string FastInputText
        {
            get
            {
                return this.textBox_fastInputText.Text;
            }
            set
            {
                this.textBox_fastInputText.Text = value;
            }
        }

        public ChargingInfoDlg()
        {
            InitializeComponent();
        }

        public InfoColor InfoColor
        {
            get
            {
                if (this.label_colorBar.BackColor == Color.Red)
                    return InfoColor.Red;
                if (this.label_colorBar.BackColor == Color.LightCoral)
                    return InfoColor.LightRed;
                if (this.label_colorBar.BackColor == Color.Yellow)
                    return InfoColor.Yellow;
                if (this.label_colorBar.BackColor == Color.Green)
                    return InfoColor.Green;

                return InfoColor.Green;
            }
            set
            {
                if (value == InfoColor.Red)
                    this.label_colorBar.BackColor = Color.Red;
                else if (value == InfoColor.LightRed)
                    this.label_colorBar.BackColor = Color.LightCoral;
                else if (value == InfoColor.Yellow)
                    this.label_colorBar.BackColor = Color.Yellow;
                else if (value == InfoColor.Green)
                    this.label_colorBar.BackColor = Color.Green;
                else
                    this.label_colorBar.BackColor = Color.Green;
            }
        }

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

        private void button_OK_Click(object sender, EventArgs e)
        {
            // ��������˿�����֣�ֹͣλ�ڱ���ȥ�Ŀ���Ȩ���������ֹѭ��
            this.textBox_fastInputText.Enabled = false; // �����ظ�����

            if (this.Host != null)
            {
                this.Host.OnStopGettingSummary(this, e);
            }
            else
            {
                // Debug.Assert(false, "û�е����жϻ�ȡ��ĿժҪ�Ĺ��ܣ��ᵼ�¶Ի���رյ��ӳ�");
            }


            this.DialogResult = DialogResult.OK;
            this.Close();
        }


        // ʡ�Բ����İ汾
        // ȱʡ����ɫΪgreen
        static string Show(IWin32Window owner,
            string strText)
        {
            return Show(owner, strText, InfoColor.Green, null,
                1.0);
        }

        // ʡ�Բ����İ汾
        // ȱʡ����ɫΪgreen
        static string Show(IWin32Window owner,
            string strText,
            double delayOpacity)
        {
            return Show(owner, strText, InfoColor.Green, null,
                delayOpacity);
        }

        static string Show(IWin32Window owner,
            string strText,
            InfoColor infocolor)
        {
            return Show(owner, strText, infocolor, null, 1.0);
        }

        static string Show(IWin32Window owner,
            string strText,
            InfoColor infocolor,
            double delayOpacity)
        {
            return Show(owner, strText, infocolor, null, delayOpacity);
        }

        // ԭʼ�汾
        static string Show(IWin32Window owner,
            string strText,
            InfoColor infocolor,
            string strCaption,
            double delayOpacity,
            Font font = null)
        {
            ChargingInfoDlg dlg = new ChargingInfoDlg();
            if (font != null)
                MainForm.SetControlFont(dlg, font, false);

            dlg.DelayOpacity = delayOpacity;
            dlg.InfoColor = infocolor;
            dlg.MessageText = strText;
            if (strCaption != null)
                dlg.Text = strCaption;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(owner);

            return dlg.FastInputText;
        }

        public static string Show(ChargingInfoHost host,
    string strText,
    InfoColor infocolor,
    string strCaption,
    double delayOpacity,
            Font font = null)
        {
            ChargingInfoDlg dlg = new ChargingInfoDlg();
            if (font != null)
                MainForm.SetControlFont(dlg, font, false);

            dlg.Host = host;
            dlg.DelayOpacity = delayOpacity;
            dlg.InfoColor = infocolor;
            dlg.MessageText = strText;
            if (strCaption != null)
                dlg.Text = strCaption;
            dlg.StartPosition = FormStartPosition.CenterScreen;

            host.ap.LinkFormState(dlg, "ChargingInfoDlg_state");
            dlg.ShowDialog(host.window);
            host.ap.UnlinkFormState(dlg);

            return dlg.FastInputText;
        }

        // 2009/6/2 new add
        public static string Show(ChargingInfoHost host,
            string strText,
            InfoColor infocolor,
            string strCaption,
            double delayOpacity,
            bool bPassword,
            Font font = null)
        {
            ChargingInfoDlg dlg = new ChargingInfoDlg();
            if (font != null)
                MainForm.SetControlFont(dlg, font, false);

            dlg.Host = host;
            dlg.DelayOpacity = delayOpacity;
            dlg.InfoColor = infocolor;
            dlg.MessageText = strText;
            if (strCaption != null)
                dlg.Text = strCaption;
            dlg.Password = bPassword;
            dlg.StartPosition = FormStartPosition.CenterScreen;

            host.ap.LinkFormState(dlg, "ChargingInfoDlg_state");
            dlg.ShowDialog(host.window);
            host.ap.UnlinkFormState(dlg);

            return dlg.FastInputText;
        }

        private void ChargingInfoDlg_Load(object sender, EventArgs e)
        {
            this.textBox_message.Select(0, 0);

            API.PostMessage(this.Handle, WM_SWITCH_FOCUS,
                0, 0);

            // ׼����͸��
            this.timer_transparent.Start();
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_SWITCH_FOCUS:
                    {
                        this.textBox_fastInputText.SelectAll();
                        this.textBox_fastInputText.Focus();

                        return;
                    }
                // break;
            }
            base.DefWndProc(ref m);
        }

        private void timer_transparent_Tick(object sender, EventArgs e)
        {
            this.timer_transparent.Stop();
            this.Opacity = this.DelayOpacity;
        }

        private void label_colorBar_MouseDown(object sender, MouseEventArgs e)
        {
            this.Opacity = 1.0;

        }

        private void label_colorBar_MouseUp(object sender, MouseEventArgs e)
        {
            this.timer_transparent.Start();
        }

        // �ָ���͸��
        private void textBox_fastInputText_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (this.Opacity != 1.0)
                this.Opacity = 1.0;

        }

        /// <summary>
        /// ����Ի����
        /// </summary>
        /// <param name="keyData">System.Windows.Forms.Keys ֵ֮һ������ʾҪ����ļ���</param>
        /// <returns>����ؼ�����ʹ�û�������Ϊ true������Ϊ false���������һ������</returns>
        protected override bool ProcessDialogKey(
    Keys keyData)
        {
            /*
            if (keyData == Keys.Enter)
            {
                this.button_OK_Click(this, null);
                return true;
            }*/

            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }


            // return false;
            return base.ProcessDialogKey(keyData);
        }
    }

    /// <summary>
    /// (���ٲ����Ի���)��Ϣ��ɫ
    /// </summary>
    public enum InfoColor
    {
        /// <summary>
        /// ��ɫ������ʧ�ܣ����߽�ֹ
        /// </summary>
        Red = 0,    // ����ʧ�ܣ����߽�ֹ

        /// <summary>
        /// Ǯ��ɫ����������ʧ�ܣ�Ҳ���ܳɹ�
        /// </summary>
        LightRed = 1,   // ��������ʧ�ܣ�Ҳ���ܳɹ�

        /// <summary>
        /// ��ɫ�������ɹ��������к���������Ҫ����
        /// </summary>
        Yellow = 2, // �����ɹ��������к���������Ҫ����

        /// <summary>
        /// ��ɫ�������ɹ���û�к�������
        /// </summary>
        Green = 3,  // �����ɹ���û�к�������
    }
}