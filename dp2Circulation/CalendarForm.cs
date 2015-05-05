using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ������������ͼ��ݿ�������
    /// </summary>
    public partial class CalendarForm : MyForm
    {
#if NO
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        DigitalPlatform.Stop stop = null;
#endif

        string m_strCurrentCalendarName = "";

        const int WM_LOAD_CALENDAR = API.WM_USER + 200;
        const int WM_DROPDOWN = API.WM_USER + 201;

        /// <summary>
        /// ��ǰ������
        /// </summary>
        public string CurrentCalendarName
        {
            get
            {
                return this.m_strCurrentCalendarName;
            }
            set
            {
                this.m_strCurrentCalendarName = value;
                // ˢ�´��ڱ���
                this.Text = "����: " + this.m_strCurrentCalendarName;
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public CalendarForm()
        {
            InitializeComponent();
        }

        private void CalendarForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

#if NO
            MainForm.AppInfo.LoadMdiChildFormStates(this,
    "mdi_form_state");
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            this.comboBox_calendarName.Text = MainForm.AppInfo.GetString(
                "CalendarForm",
                "CalendarName",
                "");


        }

        private void CalendarForm_FormClosing(object sender, FormClosingEventArgs e)
        {
#if NO
            if (stop != null)
            {
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }

            }
#endif

            if (this.calenderControl1.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ����Ϣ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
    "CalendarForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }


        private void CalendarForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                MainForm.AppInfo.SetString(
        "CalendarForm",
        "CalendarName",
        this.comboBox_calendarName.Text);
            }

        }

        // װ��ȫ��������
        int FillCalendarNames(out string strError)
        {
            this.comboBox_calendarName.Items.Clear();

            EnableControls(false, true);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ��ȫ�������� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                int nStart = 0;
                int nCount = 100;
                List<string> names = new List<string>();

                while (true)
                {
                    CalenderInfo[] infos = null;

                    long lRet = Channel.GetCalendar(
                        stop,
                        "list",
                        "",
                        nStart,
                        nCount,
                        out infos,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;
                    if (lRet == 0)
                        break;

                    // 
                    for (int i = 0; i < infos.Length; i++)
                    {
                        names.Add(infos[i].Name);
                    }

                    nStart += infos.Length;
                    if (nStart >= lRet)
                        break;
                }

                names.Sort(new CalencarNameComparer());
                foreach (string s in names)
                {
                    this.comboBox_calendarName.Items.Add(s);
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true, true);
            }

            return 1;
        ERROR1:
            return -1;
        }

        // �����������
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        int GetCalendarContent(string strName,
            out string strRange,
            out string strContent,
            out string strComment,
            out string strError)
        {
            strRange = "";
            strContent = "";
            strComment = "";
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ������ '"+strName+"' ������ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                CalenderInfo[] infos = null;

                long lRet = Channel.GetCalendar(
                    stop,
                    "get",
                    strName,
                    0,
                    -1,
                    out infos,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                if (lRet == 0)
                {
                    strError = "���� '" + strName + "' �����ڡ�";
                    return 0;   // not found
                }

                if (lRet > 1)
                {
                    strError = "����Ϊ '" + strName + "' ��������Ȼ�� " + lRet.ToString() + " ������ϵͳ����Ա�޸Ĵ˴���";
                    return -1;
                }

                Debug.Assert(infos != null, "");
                strContent = infos[0].Content;
                strRange = infos[0].Range;
                strComment = infos[0].Comment;

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);

            }

            return 1;
        ERROR1:
            return -1;
        }

        // ���桢������ɾ������
        // return:
        //      -1  ����
        //      0   �ɹ�
        int SetCalendarContent(
            string strAction,
            string strName,
            string strRange,
            string strContent,
            string strComment,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڶ����� '" + strName + "' ���� "+strAction+" ���� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                CalenderInfo��info = new CalenderInfo();
                info.Name = strName;
                info.Range = strRange;
                info.Comment = strComment;
                info.Content = strContent;

                long lRet = Channel.SetCalendar(
                    stop,
                    strAction,
                    info,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);

            }

            return 0;
        ERROR1:
            return -1;
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            LoadCalendar(this.comboBox_calendarName.Text);
        }

        int LoadCalendar(string strName)
        {
            string strError = "";

            if (this.calenderControl1.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ����Ϣ���޸ĺ���δ���档����ʱװ���µ����ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ����װ��? ",
    "CalendarForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    return 0;   // ����
                }
            }


            string strRange = "";
            string strContent = "";
            string strComment = "";

            this.calenderControl1.Clear();

            // return:
            //      -1  ����
            //      0   û���ҵ�
            //      1   �ҵ�
            int nRet = GetCalendarContent(strName,
                out strRange,
                out strContent,
                out strComment,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                goto ERROR1;
            }

            this.textBox_timeRange.Text = strRange;
            this.textBox_comment.Text = strComment;

            nRet = this.calenderControl1.SetData(strRange,
                1,
                strContent,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.button_delete.Enabled = true;
            this.CurrentCalendarName = strName;   // ���浱ǰ��������
            this.calenderControl1.Changed = false;
            return 1;   // ����װ��
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;  // ����
        }

        private void comboBox_calendarName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_calendarName.Items.Count > 0)
                return;
            string strError = "";
            int nRet = FillCalendarNames(out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
            else 
            {

                if (this.comboBox_calendarName.Text == ""
                    && this.comboBox_calendarName.Items.Count > 0)
                {
                    this.comboBox_calendarName.Text = (string)this.comboBox_calendarName.Items[0];
                    API.PostMessage(this.Handle, WM_DROPDOWN, 0, 0);
                }
            }

        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            EnableControls(bEnable, false);
        }

        void EnableControls(bool bEnable, bool bExcludeNameList)
        {
            if (bExcludeNameList == true)
                this.comboBox_calendarName.Enabled = bEnable;

            this.button_load.Enabled = bEnable;
            this.calenderControl1.Enabled = bEnable;

            if (bEnable == false)
                this.button_save.Enabled = bEnable;
            else
                this.button_save.Enabled = this.calenderControl1.Changed;
        }

        private void comboBox_calendarName_DropDownClosed(object sender, EventArgs e)
        {
        }

        private void comboBox_calendarName_SelectionChangeCommitted(object sender, EventArgs e)
        {
            API.PostMessage(this.Handle, WM_LOAD_CALENDAR, 0, 0);

        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DROPDOWN:
                    {
                        this.comboBox_calendarName.Focus();
                        this.comboBox_calendarName.DroppedDown = true;
                    }
                    return;
                case WM_LOAD_CALENDAR:
                    {
                        if (this.comboBox_calendarName.Text != "")
                        {
                            // ��Ҫһ���ط�����Ļ�ǰ�����֣����ڷ���װ���µĺ󣬻ָ������֡�

                            int nRet = LoadCalendar(this.comboBox_calendarName.Text);

                            // �ָ�ԭ��������
                            if (nRet == -1 || nRet == 0)
                                this.comboBox_calendarName.Text = this.CurrentCalendarName;
                        }
                        return;
                    }
                    // break;

            }
            base.DefWndProc(ref m);
        }

        private void comboBox_calendarName_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_load;
        }

        private void calenderControl1_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_save;
        }

        private void calenderControl1_BoxStateChanged(object sender, EventArgs e)
        {
            this.button_save.Enabled = true;
        }

        private void button_create_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strData = "";

            int nRet = calenderControl1.GetDates(1,
                out strData,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.textBox_timeRange.Text = this.calenderControl1.GetRangeString();

            // ������������
            // return:
            //      -1  ����
            //      0   �ɹ�
            nRet = SetCalendarContent(
                "new",
                this.comboBox_calendarName.Text,
                this.textBox_timeRange.Text,
                strData,
                this.textBox_comment.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;


            this.calenderControl1.Changed = false;
            this.button_save.Enabled = false;

            this.comboBox_calendarName.Items.Clear();   // ��ʹ���»�ȡ


            MessageBox.Show(this, "�����ɹ�");
            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strData = "";

            int nRet = calenderControl1.GetDates(1,
                out strData,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.textBox_timeRange.Text = this.calenderControl1.GetRangeString();

            // ���浽������
            // return:
            //      -1  ����
            //      0   �ɹ�
            nRet = SetCalendarContent(
                "change",
                this.comboBox_calendarName.Text,
                this.textBox_timeRange.Text,
                strData,
                this.textBox_comment.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.calenderControl1.Changed = false;
            this.button_save.Enabled = false;
            MessageBox.Show(this, "����ɹ�");
            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void textBox_comment_TextChanged(object sender, EventArgs e)
        {
            this.button_save.Enabled = true;
            this.button_delete.Enabled = true;
        }

        // ɾ��
        private void button_delete_Click(object sender, EventArgs e)
        {
            string strError = "";

            DialogResult result = MessageBox.Show(this,
"ȷʵҪɾ������ '"+this.comboBox_calendarName.Text+"' ? ",
"CalendarForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            // �ӷ�����ɾ��
            // return:
            //      -1  ����
            //      0   �ɹ�
            int nRet = SetCalendarContent(
                "delete",
                this.comboBox_calendarName.Text,
                "", // range
                "", // content
                "", // conmment
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.calenderControl1.Changed = false;
            this.button_save.Enabled = true;
            this.button_delete.Enabled = false;

            this.comboBox_calendarName.Items.Clear();   // ��ʹ���»�ȡ

            MessageBox.Show(this, "ɾ���ɹ�");
            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void CalendarForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }
 
    }

    // ������������
    /*public*/ class CalencarNameComparer : IComparer<string>
    {
        int IComparer<string>.Compare(string x, string y)
        {
            string strLibraryCode1 = "";
            string strPureName1 = "";

            Global.ParseCalendarName(x,
        out strLibraryCode1,
        out strPureName1);

            string strLibraryCode2 = "";
            string strPureName2 = "";

            Global.ParseCalendarName(y,
        out strLibraryCode2,
        out strPureName2);

            // �ݴ��벿�ֶ�Ϊ�գ��ͱȽϴ����ֲ���
            if (string.IsNullOrEmpty(strLibraryCode1) == true
                && string.IsNullOrEmpty(strLibraryCode2) == true)
                return string.Compare(strPureName1, strPureName2);

            // �ݴ��벿����������
            if (string.IsNullOrEmpty(strLibraryCode1) == true
                && string.IsNullOrEmpty(strLibraryCode2) == false)
                return -1;

            if (string.IsNullOrEmpty(strLibraryCode1) == false
                && string.IsNullOrEmpty(strLibraryCode2) == true)
                return 1;

            // ȫ�ַ����Ƚ�
            return string.Compare(x, y);
        }
    }
}