using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using DigitalPlatform.IO;
using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;

namespace dp2Circulation
{
    /// <summary>
    /// ʱ�Ӵ�
    /// </summary>
    public partial class ClockForm : Form
    {
        /// <summary>
        /// ͨѶͨ��
        /// </summary>
        public LibraryChannel Channel = new LibraryChannel();
        // public ApplicationInfo ap = null;
        /// <summary>
        /// ��ǰ��������
        /// </summary>
        public string Lang = "zh";

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;

        int m_nIn = 0;  // ���ںͷ������򽻵��Ĳ���

        const int WM_PREPARE = API.WM_USER + 200;

        /// <summary>
        /// ���캯��
        /// </summary>
        public ClockForm()
        {
            InitializeComponent();
        }

        private void ClockForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

            this.dateTimePicker1.Value = DateTime.Now;

            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            this.Channel.AfterLogin -= new AfterLoginEventHandle(Channel_AfterLogin);
            this.Channel.AfterLogin += new AfterLoginEventHandle(Channel_AfterLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������

            API.PostMessage(this.Handle, WM_PREPARE, 0, 0);
            this.timer1.Start();
        }

        void Channel_AfterLogin(object sender, AfterLoginEventArgs e)
        {
            this.MainForm.Channel_AfterLogin(this, e);
        }

        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }

        private void button_set_Click(object sender, EventArgs e)
        {
            string strError = "";

            DialogResult result = MessageBox.Show(this,
"ȷʵҪ�ѷ�����ʱ������Ϊ '" + this.TimeStringForDisplay + "' ?\r\n\r\n���棺���������ʱ�����õò���ȷ����Ժܶ���ͨ������������Ӱ��",
"ClockForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�������÷�������ǰʱ��Ϊ "+this.RFC1123TimeString+" ...");
            stop.BeginLoop();

            this.EnableControls(false);

            int value = Interlocked.Increment(ref this.m_nIn);

            try
            {
                if (value > 1)
                {
                    strError = "ͨ�����ڱ���һ����ʹ�ã���ǰ����������";
                    goto ERROR1;   // ��ֹ����
                }

                long lRet = Channel.SetClock(
                    stop,
                    this.RFC1123TimeString,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                Interlocked.Decrement(ref this.m_nIn);

                this.EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            MessageBox.Show(this, "ʱ�����óɹ�");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        int GetServerTime(bool bChangeEnableState,
            out string strError)
        {
            strError = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�÷�������ǰʱ�� ...");
            stop.BeginLoop();

            if (bChangeEnableState == true)
                this.EnableControls(false);

            int value = Interlocked.Increment(ref this.m_nIn);
            try
            {
                if (value > 1)
                    return 0;   // ��ֹ����

                string strTime = "";
                long lRet = Channel.GetClock(
                    stop,
                    out strTime,
                    out strError);
                if (lRet == -1)
                    return -1;

                this.RFC1123TimeString = strTime;
            }
            finally
            {
                Interlocked.Decrement(ref this.m_nIn);

                if (bChangeEnableState == true)
                    this.EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 0;
        }

        private void button_get_Click(object sender, EventArgs e)
        {
            string strError = "";

            int nRet = GetServerTime(true,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        private void button_reset_Click(object sender, EventArgs e)
        {
            string strError = "";

            DialogResult result = MessageBox.Show(this,
"ȷʵҪ�ѷ�����ʱ�Ӹ�ԭΪ�ͷ�����Ӳ��ʱ��һ��?\r\n\r\n���棺���������ʱ�����õò���ȷ����Ժܶ���ͨ������������Ӱ��",
"ClockForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;


            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ�������ʱ�Ӹ�ԭΪӲ��ʱ�� ...");
            stop.BeginLoop();

            this.EnableControls(false);

            int value = Interlocked.Increment(ref this.m_nIn);

            try
            {
                if (value > 1)
                {
                    strError = "ͨ�����ڱ���һ����ʹ�ã���ǰ����������";
                    goto ERROR1;   // ��ֹ����
                }

                long lRet = Channel.SetClock(
                    stop,
                    null,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                Interlocked.Decrement(ref this.m_nIn);

                this.EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            MessageBox.Show(this, "������ʱ�Ӹ�ԭ�ɹ�");

            // TODO: ��ԭ�����»��һ�£��Ա��ò����߿���Ч��
            button_get_Click(sender, e);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        // ע�⣬��GMTʱ��
        /// <summary>
        /// ��ʾ��ǰʱ��� RFC1123 �¼��ַ���
        /// </summary>
        public string RFC1123TimeString
        {
            get
            {
                if (String.IsNullOrEmpty(this.textBox_time.Text) == true)
                {
                    DateTime time = this.dateTimePicker1.Value.ToUniversalTime();

                    return DateTimeUtil.Rfc1123DateTimeString(time);
                }
                return this.textBox_time.Text;
            }
            set
            {
                DateTime time;
                try
                {
                    time = DateTimeUtil.FromRfc1123DateTimeString(value);
                }
                catch
                {
                    MessageBox.Show(this, "ʱ���ַ��� " +value+ "��ʽ���Ϸ�" );
                    return;
                }

                this.textBox_time.Text = value;
                this.dateTimePicker1.Value = time.ToLocalTime();
            }
        }

        string TimeStringForDisplay
        {
            get
            {
                return DateTimeUtil.LocalTime(this.RFC1123TimeString);
            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            DateTime time = this.dateTimePicker1.Value.ToUniversalTime();

            this.textBox_time.Text = DateTimeUtil.Rfc1123DateTimeString(time);
        }

        private void ClockForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

        private void ClockForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        private void checkBox_autoGet_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_autoGetServerTime.Checked == true)
            {
                // ����ʱ��ˢ�µ�����£��޸�ʱ���ַ����Ĳ���������ѣ�ֻ�ý�ֹ
                this.dateTimePicker1.Enabled = false;
                this.button_set.Enabled = false;
                this.button_reset.Enabled = false;

                // �ı�Ϊtrue״̬���������һ��
                string strError = "";

                GetServerTime(false,
                    out strError);
            }
            else
            {
                this.dateTimePicker1.Enabled = true;
                this.button_set.Enabled = true;
                this.button_reset.Enabled = true;
            }
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public void EnableControls(bool bEnable)
        {
            if (this.checkBox_autoGetServerTime.Checked == false)
                this.dateTimePicker1.Enabled = bEnable;
            else
                this.dateTimePicker1.Enabled = false;

            this.button_get.Enabled = bEnable;

            if (this.checkBox_autoGetServerTime.Checked == false)
            {
                this.button_set.Enabled = bEnable;
                this.button_reset.Enabled = bEnable;
            }
            else
            {
                this.button_set.Enabled = false;
                this.button_reset.Enabled = false;
            }

            this.textBox_time.Enabled = bEnable;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string strError = "";

            // ˢ�·�����ʱ����ʾ
            if (this.checkBox_autoGetServerTime.Checked == true)
            {
                GetServerTime(false,
                    out strError);
            }

            // ˢ�±���ʱ����ʾ
            DateTime now = DateTime.Now;
            this.textBox_localTime.Text = now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void ClockForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (stop != null)
            {
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }

            }

            this.timer1.Stop();
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_PREPARE:
                    {
                        string strError = "";

                        // ���ڴ򿪺󣬵�һ�λ�÷�����ʱ����ʾ
                        GetServerTime(true, // changed
                            out strError);

                        // ��һ��ˢ�±���ʱ����ʾ
                        DateTime now = DateTime.Now;
                        this.textBox_localTime.Text = now.ToString("yyyy-MM-dd HH:mm:ss");

                        return;
                    }
                // break;

            }
            base.DefWndProc(ref m);
        }
    }
}