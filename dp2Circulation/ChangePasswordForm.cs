using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// �޸����봰
    /// </summary>
    public partial class ChangePasswordForm : MyForm
    {
#if NO
        public LibraryChannel Channel = new LibraryChannel();
        // public ApplicationInfo ap = null;
        public string Lang = "zh";

        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;
#endif

        const int WM_FIRST_SETFOCUS = API.WM_USER + 200;

        /// <summary>
        /// ���캯��
        /// </summary>
        public ChangePasswordForm()
        {
            InitializeComponent();
        }

        private void ChangePasswordForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
#if NO
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);


            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            bool bReader = this.MainForm.AppInfo.GetBoolean(
                "default_account",
                "isreader",
                false);
            if (bReader == false)
            {
                this.textBox_reader_oldPassword.Enabled = false;
            }
            else
            {
                this.textBox_reader_comment.Text = "���Ƕ���Ϊ�Լ��޸����롣";
                this.tabControl_main.Controls.Remove(this.tabPage_worker);
            }

            checkBox_worker_force_CheckedChanged(this, null);

            API.PostMessage(this.Handle, WM_FIRST_SETFOCUS, 0, 0);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_FIRST_SETFOCUS:
                    this.textBox_reader_barcode.Focus();
                    return;
            }
            base.DefWndProc(ref m);
        }

#if NO
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            MainForm.Channel_BeforeLogin(this, e);
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

#endif

        private void ChangePasswordForm_FormClosed(object sender, FormClosedEventArgs e)
        {

        }



        private void button_reader_changePassword_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.textBox_reader_barcode.Text == "")
            {
                MessageBox.Show(this, "��δ�������֤����š�");
                this.textBox_reader_barcode.Focus();
                return;
            }

            if (this.textBox_reader_newPassword.Text != this.textBox_reader_confirmNewPassword.Text)
            {
                MessageBox.Show(this, "������ �� ȷ�������벻һ�¡����������롣");
                this.textBox_reader_newPassword.Focus();
                return;
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����޸Ķ������� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            this.EnableControls(false);

            try
            {
                long lRet = Channel.ChangeReaderPassword(
                    stop,
                    this.textBox_reader_barcode.Text,
                    this.textBox_reader_oldPassword.Text,
                    this.textBox_reader_newPassword.Text,
                    out strError);
                if (lRet == 0)
                {
                    goto ERROR1;
                }
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                this.EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            MessageBox.Show(this, "���������Ѿ����ɹ��޸ġ�");

            this.textBox_reader_barcode.SelectAll();
            this.textBox_reader_barcode.Focus();
            return;
        ERROR1:
            MessageBox.Show(this, strError);

            // �������¶�λ������������
            this.textBox_reader_oldPassword.Focus();
            this.textBox_reader_oldPassword.SelectAll();
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.button_reader_changePassword.Enabled = bEnable;
            this.textBox_reader_barcode.Enabled = bEnable;
            this.textBox_reader_newPassword.Enabled = bEnable;
            this.textBox_reader_confirmNewPassword.Enabled = bEnable;

            bool bReader = this.MainForm.AppInfo.GetBoolean(
    "default_account",
    "isreader",
    false);
            if (bReader == false)
                this.textBox_reader_oldPassword.Enabled = false;
            else
                this.textBox_reader_oldPassword.Enabled = bEnable;

            this.button_worker_changePassword.Enabled = bEnable;
            this.textBox_worker_userName.Enabled = bEnable;
            if (this.checkBox_worker_force.Checked == true)
                this.textBox_worker_oldPassword.Enabled = false;
            else
                this.textBox_worker_oldPassword.Enabled = bEnable;
            this.textBox_worker_newPassword.Enabled = bEnable;
            this.textBox_worker_confirmNewPassword.Enabled = bEnable;
            this.checkBox_worker_force.Enabled = bEnable;

        }

        private void button_worker_changePassword_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.textBox_worker_userName.Text == "")
            {
                MessageBox.Show(this, "��δ�����û�����");
                this.textBox_worker_userName.Focus();
                return;
            }

            if (this.textBox_worker_newPassword.Text != this.textBox_worker_confirmNewPassword.Text)
            {
                MessageBox.Show(this, "������ �� ȷ�������벻һ�¡����������롣");
                this.textBox_worker_newPassword.Focus();
                return;
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����޸Ĺ�����Ա���� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            this.EnableControls(false);

            try
            {
                long lRet = 0;

                // ��ǿ���޸����룬�������޸�
                if (this.checkBox_worker_force.Checked == false)
                {

                    if (this.textBox_worker_userName.Text != "!changeKernelPassword")
                    {
                        // return:
                        //      -1  error
                        //      0   ��¼δ�ɹ�
                        //      1   ��¼�ɹ�
                        lRet = Channel.Login(this.textBox_worker_userName.Text,
                            this.textBox_worker_oldPassword.Text,
                            "type=worker",
                            out strError);
                        if (lRet == -1)
                        {
                            goto ERROR1;
                        }

                        if (lRet == 0)
                        {
                            strError = "�����벻��ȷ";
                            goto ERROR1;
                        }
                    }

                    try
                    {

                        lRet = Channel.ChangeUserPassword(
                            stop,
                            this.textBox_worker_userName.Text,
                            this.textBox_worker_oldPassword.Text,
                            this.textBox_worker_newPassword.Text,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;
                    }
                    finally
                    {
                        string strError_1 = "";
                        Channel.Logout(out strError_1);
                    }
                }

                // ǿ���޸�����
                if (this.checkBox_worker_force.Checked == true)
                {
                    UserInfo info = new UserInfo();
                    info.UserName = this.textBox_worker_userName.Text;
                    info.Password = this.textBox_worker_newPassword.Text;
                    // ��actionΪ"resetpassword"ʱ����info.ResetPassword״̬�������ã�����������Ҫ�޸����롣resetpassword�����޸�������Ϣ��Ҳ����˵info�г���Password/UserName����������Ա��ֵ��Ч��
                    lRet = Channel.SetUser(
                        stop,
                        "resetpassword",
                        info,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                }


            }
            finally
            {
                this.EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            MessageBox.Show(this, "������Ա '" + this.textBox_worker_userName.Text + "' �����Ѿ����ɹ��޸ġ�");

            this.textBox_worker_userName.SelectAll();
            this.textBox_worker_userName.Focus();
            return;
        ERROR1:
            MessageBox.Show(this, strError);

            // �������¶�λ������������
            this.textBox_worker_oldPassword.Focus();
            this.textBox_worker_oldPassword.SelectAll();
        }

        private void checkBox_worker_force_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_worker_force.Checked == true)
                this.textBox_worker_oldPassword.Enabled = false;
            else
                this.textBox_worker_oldPassword.Enabled = true;
        }

        private void ChangePasswordForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        private void tabControl_main_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.tabControl_main.SelectedTab == this.tabPage_reader)
            {
                this.AcceptButton = this.button_reader_changePassword;
            }
            else
            {
                Debug.Assert(this.tabControl_main.SelectedTab == this.tabPage_worker, "");
                this.AcceptButton = this.button_worker_changePassword;
            }
        }
    }
}