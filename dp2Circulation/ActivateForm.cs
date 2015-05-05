using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.IO;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.Text;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// ���
    /// </summary>
    public partial class ActivateForm : MyForm
    {
        Commander commander = null;

        const int WM_LOAD_OLD_USERINFO = API.WM_USER + 200;
        const int WM_LOAD_NEW_USERINFO = API.WM_USER + 201;
        const int WM_SAVE_OLD_RECORD = API.WM_USER + 202;
        const int WM_SAVE_NEW_RECORD = API.WM_USER + 203;
        const int WM_DEVOLVE = API.WM_USER + 204;
        const int WM_ACTIVATE_TARGET = API.WM_USER + 205;



        WebExternalHost m_webExternalHost_new = new WebExternalHost();
        WebExternalHost m_webExternalHost_old = new WebExternalHost();

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;
#endif

        /// <summary>
        /// ���캯��
        /// </summary>
        public ActivateForm()
        {
            InitializeComponent();
        }

        private void ActivateForm_Load(object sender, EventArgs e)
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

            this.readerEditControl_old.SetReadOnly("librarian");
            this.readerEditControl_old.GetValueTable += new GetValueTableEventHandler(readerEditControl1_GetValueTable);
            this.readerEditControl_old.Initializing = false;   // ���û�д˾䣬һ��ʼ�ڿ�ģ�����޸ľͲ����ɫ

            this.readerEditControl_new.SetReadOnly("librarian");
            this.readerEditControl_new.GetValueTable += new GetValueTableEventHandler(readerEditControl1_GetValueTable);
            this.readerEditControl_new.Initializing = false;   // ���û�д˾䣬һ��ʼ�ڿ�ģ�����޸ľͲ����ɫ


            // webbrowser
            this.m_webExternalHost_new.Initial(this.MainForm, this.webBrowser_newReaderInfo);
            this.webBrowser_newReaderInfo.ObjectForScripting = this.m_webExternalHost_new;

            this.m_webExternalHost_old.Initial(this.MainForm, this.webBrowser_oldReaderInfo);
            this.webBrowser_oldReaderInfo.ObjectForScripting = this.m_webExternalHost_old;

            // commander
            this.commander = new Commander(this);
            this.commander.IsBusy -= new IsBusyEventHandler(commander_IsBusy);
            this.commander.IsBusy += new IsBusyEventHandler(commander_IsBusy);
        }

        void commander_IsBusy(object sender, IsBusyEventArgs e)
        {
            if (this.m_webExternalHost_old.ChannelInUse ||
                this.m_webExternalHost_new.ChannelInUse == true)
            {
                e.IsBusy = true;
            }
        }

        private void ActivateForm_FormClosing(object sender, FormClosingEventArgs e)
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

            if (this.readerEditControl_old.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ��֤����Ϣ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
    "ActivateForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (this.readerEditControl_new.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ��֤����Ϣ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
    "ActivateForm",
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

        private void ActivateForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.commander.Destroy();

            if (this.m_webExternalHost_new != null)
                this.m_webExternalHost_new.Destroy();

            if (this.m_webExternalHost_old != null)
                this.m_webExternalHost_old.Destroy();

            this.readerEditControl_old.GetValueTable -= new GetValueTableEventHandler(readerEditControl1_GetValueTable);
            this.readerEditControl_new.GetValueTable -= new GetValueTableEventHandler(readerEditControl1_GetValueTable);
        }

        void readerEditControl1_GetValueTable(object sender, GetValueTableEventArgs e)
        {
            string strError = "";
            string[] values = null;
            int nRet = MainForm.GetValueTable(e.TableName,
                e.DbName,
                out values,
                out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
            e.values = values;
        }

#if NO
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }
#endif

        private void textBox_oldBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_loadOldUserInfo;
            this.MainForm.EnterPatronIdEdit(InputType.PQR);
        }

        private void textBox_oldBarcode_Leave(object sender, EventArgs e)
        {
            this.MainForm.LeavePatronIdEdit();

        }

        private void textBox_newBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_loadNewUserInfo;

            this.MainForm.EnterPatronIdEdit(InputType.PQR);
        }

        private void textBox_newBarcode_Leave(object sender, EventArgs e)
        {
            this.MainForm.LeavePatronIdEdit();
        }

        /// <summary>
        /// װ�ؾɼ�¼
        /// </summary>
        /// <param name="strReaderBarcode">����֤�����</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int LoadOldRecord(string strReaderBarcode)
        {
            this.textBox_oldBarcode.Text = strReaderBarcode;

            int nRet = this.LoadRecord(ref strReaderBarcode,
                this.readerEditControl_old,
                this.m_webExternalHost_old,
                // this.webBrowser_oldReaderInfo,
                this.webBrowser_oldXml);
            if (this.textBox_oldBarcode.Text != strReaderBarcode)
                this.textBox_oldBarcode.Text = strReaderBarcode;
            return nRet;
        }

        /// <summary>
        /// װ���¼�¼
        /// </summary>
        /// <param name="strReaderBarcode">����֤�����</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int LoadNewRecord(string strReaderBarcode)
        {
            this.textBox_newBarcode.Text = strReaderBarcode;

            int nRet = this.LoadRecord(ref strReaderBarcode,
                this.readerEditControl_new,
                this.m_webExternalHost_new,
                // this.webBrowser_newReaderInfo,
                this.webBrowser_newXml);
            if (this.textBox_newBarcode.Text != strReaderBarcode)
                this.textBox_newBarcode.Text = strReaderBarcode;

            return nRet;
        }

        private void button_loadOldUserInfo_Click(object sender, EventArgs e)
        {
            if (this.textBox_oldBarcode.Text == "")
            {
                MessageBox.Show(this, "��δָ���ɶ���֤��֤�����");
                return;
            }

            this.button_loadOldUserInfo.Enabled = false;

            this.m_webExternalHost_old.StopPrevious();
            this.webBrowser_oldReaderInfo.Stop();

            this.commander.AddMessage(WM_LOAD_OLD_USERINFO);
        }

        private void button_loadNewUserInfo_Click(object sender, EventArgs e)
        {
            if (this.textBox_newBarcode.Text == "")
            {
                MessageBox.Show(this, "��δָ���¶���֤��֤�����");
                return;
            }

            this.button_loadNewUserInfo.Enabled = false;

            this.m_webExternalHost_new.StopPrevious();
            this.webBrowser_newReaderInfo.Stop();

            this.commander.AddMessage(WM_LOAD_NEW_USERINFO);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_LOAD_OLD_USERINFO:
                        if (this.m_webExternalHost_old.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            string strReaderBarcode = this.textBox_oldBarcode.Text;
                            this.LoadRecord(ref strReaderBarcode,
                                this.readerEditControl_old,
                                this.m_webExternalHost_old,
                                // this.webBrowser_oldReaderInfo,
                                this.webBrowser_oldXml);
                            if (this.textBox_oldBarcode.Text != strReaderBarcode)
                                this.textBox_oldBarcode.Text = strReaderBarcode;
                        }
                    return;
                case WM_LOAD_NEW_USERINFO:
                    if (this.m_webExternalHost_new.CanCallNew(
                        this.commander,
                        m.Msg) == true)
                    {
                        string strReaderBarcode = this.textBox_newBarcode.Text;
                        this.LoadRecord(ref strReaderBarcode,
                            this.readerEditControl_new,
                            this.m_webExternalHost_new,
                            // this.webBrowser_newReaderInfo,
                            this.webBrowser_newXml);
                        if (this.textBox_newBarcode.Text != strReaderBarcode)
                            this.textBox_newBarcode.Text = strReaderBarcode;
                    }
                    return;
                case WM_SAVE_OLD_RECORD:
                    if (this.m_webExternalHost_old.CanCallNew(
                        this.commander,
                        m.Msg) == true)
                    {

                        this.SaveOldRecord();
                    }
                    return;
                case WM_SAVE_NEW_RECORD:
                    if (this.m_webExternalHost_new.CanCallNew(
                        this.commander,
                        m.Msg) == true)
                    {

                        this.SaveNewRecord();
                    }
                    return;
                case WM_DEVOLVE:
                    if (this.m_webExternalHost_new.CanCallNew(
                        this.commander,
                        m.Msg) == true)
                    {

                        this.Devolve();
                    }
                    return;
                case WM_ACTIVATE_TARGET:
                    if (this.m_webExternalHost_new.CanCallNew(
                        this.commander,
                        m.Msg) == true)
                    {

                        this.ActivateTarget();
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }


        // ���ݶ���֤����ţ�װ����߼�¼
        // parameters:
        //      edit    ���߱༭�ؼ�������==null
        //      webbHtml    ������ʾHTML��WebBrowser�ؼ�������==null
        //      webbXml   ������ʾXML��WebBrowser�ؼ�������==null
        // return:
        //      0   cancelled
        internal int LoadRecord(ref string strBarcode,
            ReaderEditControl edit,
            WebExternalHost external_html,
            // WebBrowser webbHtml,
            WebBrowser webbXml)
        {
            string strError = "";
            int nRet = 0;

            if (edit != null
                && edit.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
"��ǰ����Ϣ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ����֤���������װ������? ",
"ActivateForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                    return 0;   // cancelled

            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڳ�ʼ���������� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            EnableControls(false);

            if (edit != null)
                edit.Clear();
#if NO
            if (webbHtml != null)
            {
                Global.ClearHtmlPage(webbHtml,
                    this.MainForm.DataDir);
            }
#endif
            if (external_html != null)
            {
                external_html.ClearHtmlPage();
            }

            if (webbXml != null)
            {
                Global.ClearHtmlPage(webbXml,
                    this.MainForm.DataDir);
            }

            try
            {
                byte[] baTimestamp = null;
                string strRecPath = "";

                int nRedoCount = 0;
            REDO:
                stop.SetMessage("����װ����߼�¼ " + strBarcode + " ...");

                string[] results = null;

                long lRet = Channel.GetReaderInfo(
                    stop,
                    strBarcode,
                    "xml,html",
                    out results,
                    out strRecPath,
                    out baTimestamp,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (lRet == 0)
                    goto ERROR1;

                if (lRet > 1)
                {
                    // ������Ժ���Ȼ�����ظ�
                    if (nRedoCount > 0)
                    {
                        strError = "���� " + strBarcode + " ���м�¼ " + lRet.ToString() + " ��������װ����߼�¼��\r\n\r\nע������һ�����ش�����ϵͳ����Ա�����ų���";
                        goto ERROR1;    // ��������
                    }
                    SelectPatronDialog dlg = new SelectPatronDialog();

                    dlg.Overflow = StringUtil.SplitList(strRecPath).Count < lRet;
                    nRet = dlg.Initial(
                        this.MainForm,
                        this.Channel,
                        this.stop,
                        StringUtil.SplitList(strRecPath),
                        "��ѡ��һ�����߼�¼",
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    // TODO: ���洰���ڵĳߴ�״̬
                    this.MainForm.AppInfo.LinkFormState(dlg, "ActivateForm_SelectPatronDialog_state");
                    dlg.ShowDialog(this);
                    this.MainForm.AppInfo.UnlinkFormState(dlg);

                    if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    {
                        strError = "����ѡ��";
                        return 0;
                    }

                    strBarcode = dlg.SelectedBarcode;
                    nRedoCount++;
                    goto REDO;
                }



                // this.ReaderBarcode = strBarcode;

                if (results == null || results.Length < 2)
                {
                    strError = "���ص�results��������";
                    goto ERROR1;
                }

                string strXml = "";
                strXml = results[0];
                string strHtml = results[1];

                if (edit != null)
                {
                    nRet = edit.SetData(
                        strXml,
                        strRecPath,
                        baTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }

                if (webbXml != null)
                {
                    /*
                    SetXmlToWebbrowser(webbXml,
                        strXml);
                     * */
                    Global.SetXmlToWebbrowser(webbXml,
                        this.MainForm.DataDir,
                        "xml",
                        strXml);
                }

                // this.m_strSetAction = "change";

#if NO
                if (webbHtml != null)
                {
                    Global.SetHtmlString(webbHtml,
                            strHtml,
                            this.MainForm.DataDir,
                            "activateform_html");
                }
#endif

                if (external_html != null)
                    external_html.SetHtmlString(strHtml, "activateform_html");
            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 1;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.textBox_oldBarcode.Enabled = bEnable;
            this.textBox_newBarcode.Enabled = bEnable;

            this.tabControl_old.Enabled = bEnable;
            this.tabControl_new.Enabled = bEnable;

            this.button_loadOldUserInfo.Enabled = bEnable;
            this.button_loadNewUserInfo.Enabled = bEnable;

            this.button_devolve.Enabled = bEnable;
            this.button_activate.Enabled = bEnable;

            this.toolStrip_new.Enabled = bEnable;
            this.toolStrip_old.Enabled = bEnable;
        }

        // ת�Ʋ�����Ŀ��֤
        private void button_activate_Click(object sender, EventArgs e)
        {
            this.button_activate.Enabled = false;

            this.m_webExternalHost_new.StopPrevious();
            this.webBrowser_newReaderInfo.Stop();

            this.m_webExternalHost_old.StopPrevious();
            this.webBrowser_oldReaderInfo.Stop();

            this.commander.AddMessage(WM_ACTIVATE_TARGET);
        }

        // ת�Ʋ�����Ŀ��֤
        void ActivateTarget()
        {
            string strError = "";
            int nRet = 0;

            // �Ѿ�֤�Ľ�����Ϣת����֤
            nRet = DevolveReaderInfo(this.textBox_oldBarcode.Text,
                this.textBox_newBarcode.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // ˢ��
            string strReaderBarcode = this.textBox_oldBarcode.Text;
            this.LoadRecord(ref strReaderBarcode,
                this.readerEditControl_old,
                this.m_webExternalHost_old,
                // this.webBrowser_oldReaderInfo,
                this.webBrowser_oldXml);
            strReaderBarcode = this.textBox_newBarcode.Text;
            this.LoadRecord(ref strReaderBarcode,
                this.readerEditControl_new,
                this.m_webExternalHost_new,
                // this.webBrowser_newReaderInfo,
                this.webBrowser_newXml);

            bool bZhuxiao = false;

            // �Ѿ�֤��״̬�޸�Ϊ��ע����
            if (this.readerEditControl_old.State != "ע��")
            {
                this.readerEditControl_old.State = "ע��";

                // return:
                //      -1  error
                //      0   �ɹ�
                //      1   �������˼�¼�����ı䣬δ���档ע�����±����¼
                nRet = SaveReaderInfo(this.readerEditControl_old,
                    out strError);
                if (nRet == -1)
                {
                    strError = strError + "\r\n\r\nĿ��֤û�����ü����";
                    goto ERROR1;
                }
                if (nRet == 1)
                {
                    // ���ַ������˼�¼�����ı䣬��¼���δ���档
                    // ����Ϊ����������¼������һ�����水ť���������Ա��档
                    MessageBox.Show(this, "Դ֤��Ϣ�޸ĺ���δ���棬�밴���水ť����֮��");
                }
                else
                {
                    bZhuxiao = true;
                    // ˢ��
                    string strTempReaderBarcode = this.readerEditControl_old.Barcode;
                    this.LoadRecord(ref strTempReaderBarcode,
                        null,
                        this.m_webExternalHost_old,
                        // this.webBrowser_oldReaderInfo,
                        this.webBrowser_oldXml);
                }

            }

            // ����֤��״̬�޸�Ϊ����
            if (this.readerEditControl_new.State != "")
            {
                this.readerEditControl_new.State = "";
                // return:
                //      -1  error
                //      0   �ɹ�
                //      1   �������˼�¼�����ı䣬δ���档ע�����±����¼
                nRet = SaveReaderInfo(this.readerEditControl_new,
                    out strError);
                if (nRet == -1)
                {
                    if (bZhuxiao == true)
                        strError = strError + "\r\n\r\nԴ֤�Ѿ�ע����";
                    goto ERROR1;
                }
                if (nRet == 1)
                {
                    MessageBox.Show(this, "Ŀ��֤��Ϣ�޸ĺ���δ���棬�밴���水ť����֮��");
                }
                else
                {
                    string strTempReaderBarcode = this.readerEditControl_new.Barcode;
                    this.LoadRecord(ref strTempReaderBarcode,
                        null,
                        this.m_webExternalHost_new,
                        // this.webBrowser_newReaderInfo,
                        this.webBrowser_newXml);
                }

            }


            MessageBox.Show(this, "ת�Ʋ�����Ŀ��֤�������");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ����
        // return:
        //      -1  error
        //      0   �ɹ�
        //      1   �������˼�¼�����ı䣬δ���档ע�����±����¼
        int SaveReaderInfo(ReaderEditControl edit,
            out string strError)
        {
            strError = "";

            if (edit.Barcode == "")
            {
                strError = "��δ����֤�����";
                goto ERROR1;
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ�����߼�¼ " + edit.Barcode + " ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                string strNewXml = "";
                int nRet = edit.GetData(
                    out strNewXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                ErrorCodeValue kernel_errorcode;

                byte[] baNewTimestamp = null;
                string strExistingXml = "";
                string strSavedXml = "";
                string strSavedPath = "";

                long lRet = Channel.SetReaderInfo(
                    stop,
                    "change",
                    edit.RecPath,
                    strNewXml,
                    edit.OldRecord,
                    edit.Timestamp,
                    out strExistingXml,
                    out strSavedXml,
                    out strSavedPath,
                    out baNewTimestamp,
                    out kernel_errorcode,
                    out strError);
                if (lRet == -1)
                {
                    // Debug.Assert(false, "");

                    if (kernel_errorcode == ErrorCodeValue.TimestampMismatch)
                    {
                        CompareReaderForm dlg = new CompareReaderForm();
                        dlg.Initial(
                            this.MainForm,
                            edit.RecPath,
                            strExistingXml,
                            baNewTimestamp,
                            strNewXml,
                            edit.Timestamp,
                            "���ݿ��еļ�¼�ڱ༭�ڼ䷢���˸ı䡣����ϸ�˶ԣ��������޸Ĵ����е�δ�����¼����ȷ����ť������Ա��档");

                        dlg.StartPosition = FormStartPosition.CenterScreen;
                        dlg.ShowDialog(this);
                        if (dlg.DialogResult == DialogResult.OK)
                        {
                            nRet = edit.SetData(dlg.UnsavedXml,
                                dlg.RecPath,
                                dlg.UnsavedTimestamp,
                                out strError);
                            if (nRet == -1)
                            {
                                return -1;
                            }
                            strError = "��ע�����±����¼";
                            return 1;
                        }
                    }

                    goto ERROR1;
                }

                /*
                this.Timestamp = baNewTimestamp;
                this.OldRecord = strSavedXml;
                this.RecPath = strSavedPath;
                 */

                if (lRet == 1)
                {
                    // �����ֶα��ܾ�
                    MessageBox.Show(this, strError);

                    if (Channel.ErrorCode == ErrorCode.PartialDenied)
                    {
                        // ��������װ��?
                        MessageBox.Show(this, "������װ�ؼ�¼, �����Щ�ֶ������޸ı��ܾ���");
                    }
                }
                else
                {
                    // ����װ�ؼ�¼���༭��
                    nRet = edit.SetData(strSavedXml,
                        strSavedPath,
                        baNewTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }

            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            strError = "����ɹ�";
            return 0;
        ERROR1:
            return -1;
        }


        int DevolveReaderInfo(string strSourceReaderBarcode,
            string strTargetReaderBarcode,
            out string strError)
        {
            strError = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ת�ƶ��߽�����Ϣ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            this.EnableControls(false);

            try
            {
                long lRet = Channel.DevolveReaderInfo(
                    stop,
                    strSourceReaderBarcode,
                    strTargetReaderBarcode,
                    out strError);
                if (lRet == -1)
                    return -1;

                return (int)lRet;

            }
            finally
            {
                this.EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }
        }

        // ת��
        private void button_devolve_Click(object sender, EventArgs e)
        {
            this.button_devolve.Enabled = false;

            this.m_webExternalHost_new.StopPrevious();
            this.webBrowser_newReaderInfo.Stop();

            this.m_webExternalHost_old.StopPrevious();
            this.webBrowser_oldReaderInfo.Stop();

            this.commander.AddMessage(WM_DEVOLVE);
        }

        void Devolve()
        {
            string strError = "";
            int nRet = 0;

            // ��Դ֤�Ľ�����Ϣת��Ŀ��֤
            nRet = DevolveReaderInfo(this.textBox_oldBarcode.Text,
                this.textBox_newBarcode.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // ˢ��
            string strReaderBarcode = this.textBox_oldBarcode.Text;
            this.LoadRecord(ref strReaderBarcode,
                this.readerEditControl_old,
                this.m_webExternalHost_old,
                // this.webBrowser_oldReaderInfo,
                this.webBrowser_oldXml);
            strReaderBarcode = this.textBox_newBarcode.Text;
            this.LoadRecord(ref strReaderBarcode,
                this.readerEditControl_new,
                this.m_webExternalHost_new,
                // this.webBrowser_newReaderInfo,
                this.webBrowser_newXml);

            MessageBox.Show(this, "ת�����");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

#if NO
        void SetXmlToWebbrowser(WebBrowser webbrowser,
            string strXml)
        {
            string strTargetFileName = MainForm.DataDir + "\\xml.xml";

            StreamWriter sw = new StreamWriter(strTargetFileName,
                false,	// append
                System.Text.Encoding.UTF8);
            sw.Write(strXml);
            sw.Close();

            webbrowser.Navigate(strTargetFileName);
        }
#endif

        private void button_saveOld_Click(object sender, EventArgs e)
        {
        }

        void SaveOldRecord()
        {
            string strError = "";
            int nRet = 0;

            nRet = SaveReaderInfo(this.readerEditControl_old,
    out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 1)
            {
                // ���ַ������˼�¼�����ı䣬��¼���δ���档
                goto ERROR1;
            }

            MessageBox.Show(this, "����ɹ�");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        private void button_saveNew_Click(object sender, EventArgs e)
        {
        }

        void SaveNewRecord()
        {
            string strError = "";
            int nRet = 0;

            nRet = SaveReaderInfo(this.readerEditControl_new,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 1)
            {
                // ���ַ������˼�¼�����ı䣬��¼���δ���档
                goto ERROR1;
            }

            MessageBox.Show(this, "����ɹ�");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        private void readerEditControl_old_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            this.toolStripButton_old_save.Enabled = e.CurrentChanged;
        }

        private void readerEditControl_new_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            this.toolStripButton_new_save.Enabled = e.CurrentChanged;
        }

        private void ActivateForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        // ��Դ��¼�и��Ƴ�������źͼ�¼·�����������ȫ�����ݡ�
        private void button_copyFromOld_Click(object sender, EventArgs e)
        {

        }

        private void panel_old_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;

        }

        private void panel_old_DragDrop(object sender, DragEventArgs e)
        {
            string strError = "";

            string strWhole = (String)e.Data.GetData("Text");

            string[] lines = strWhole.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 1)
            {
                strError = "��һ��Ҳ������";
                goto ERROR1;
            }

            if (lines.Length > 1)
            {
                strError = "���һ��ֻ��������һ����¼";
                goto ERROR1;
            }

            string strFirstLine = lines[0].Trim();

            // ȡ��recpath
            string strRecPath = "";
            int nRet = strFirstLine.IndexOf("\t");
            if (nRet == -1)
                strRecPath = strFirstLine;
            else
                strRecPath = strFirstLine.Substring(0, nRet).Trim();

            // �ж����ǲ��Ƕ��߼�¼·��
            string strDbName = Global.GetDbName(strRecPath);

            if (this.MainForm.IsReaderDbName(strDbName) == true)
            {
                string[] parts = strFirstLine.Split(new char[] { '\t' });
                string strReaderBarcode = "";
                if (parts.Length >= 2)
                    strReaderBarcode = parts[1].Trim();

                if (String.IsNullOrEmpty(strReaderBarcode) == false)
                {
                    this.textBox_oldBarcode.Text = strReaderBarcode;
                    this.button_loadOldUserInfo_Click(this, null);
                }
            }
            else
            {
                strError = "��¼·�� '" + strRecPath + "' �е����ݿ������Ƕ��߿���...";
                goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void panel_new_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;

        }

        private void panel_new_DragDrop(object sender, DragEventArgs e)
        {
            string strError = "";

            string strWhole = (String)e.Data.GetData("Text");

            string[] lines = strWhole.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 1)
            {
                strError = "��һ��Ҳ������";
                goto ERROR1;
            }

            if (lines.Length > 1)
            {
                strError = "���һ��ֻ��������һ����¼";
                goto ERROR1;
            }

            string strFirstLine = lines[0].Trim();

            // ȡ��recpath
            string strRecPath = "";
            int nRet = strFirstLine.IndexOf("\t");
            if (nRet == -1)
                strRecPath = strFirstLine;
            else
                strRecPath = strFirstLine.Substring(0, nRet).Trim();

            // �ж����ǲ��Ƕ��߼�¼·��
            string strDbName = Global.GetDbName(strRecPath);

            if (this.MainForm.IsReaderDbName(strDbName) == true)
            {
                string[] parts = strFirstLine.Split(new char[] { '\t' });
                string strReaderBarcode = "";
                if (parts.Length >= 2)
                    strReaderBarcode = parts[1].Trim();

                if (String.IsNullOrEmpty(strReaderBarcode) == false)
                {
                    this.textBox_newBarcode.Text = strReaderBarcode;
                    this.button_loadNewUserInfo_Click(this, null);
                }
            }
            else
            {
                strError = "��¼·�� '" + strRecPath + "' �е����ݿ������Ƕ��߿���...";
                goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void toolStripButton_old_save_Click(object sender, EventArgs e)
        {
            this.toolStrip_old.Enabled = false;

            this.m_webExternalHost_old.StopPrevious();
            this.webBrowser_oldReaderInfo.Stop();

            this.commander.AddMessage(WM_SAVE_OLD_RECORD);
        }

        private void toolStripButton_new_save_Click(object sender, EventArgs e)
        {
            this.toolStrip_new.Enabled = false;

            this.m_webExternalHost_new.StopPrevious();
            this.webBrowser_newReaderInfo.Stop();

            this.commander.AddMessage(WM_SAVE_NEW_RECORD);
        }

        private void toolStripButton_new_copyFromOld_Click(object sender, EventArgs e)
        {

            // TODO: ��Ҫ�����µ���
            this.readerEditControl_new.NameString = this.readerEditControl_old.NameString;
            this.readerEditControl_new.State = this.readerEditControl_old.State;
            this.readerEditControl_new.Comment = this.readerEditControl_old.Comment;
            this.readerEditControl_new.ReaderType = this.readerEditControl_old.ReaderType;
            this.readerEditControl_new.CreateDate = this.readerEditControl_old.CreateDate;
            this.readerEditControl_new.ExpireDate = this.readerEditControl_old.ExpireDate;
            this.readerEditControl_new.DateOfBirth = this.readerEditControl_old.DateOfBirth;
            this.readerEditControl_new.Gender = this.readerEditControl_old.Gender;
            this.readerEditControl_new.IdCardNumber = this.readerEditControl_old.IdCardNumber;
            this.readerEditControl_new.Department = this.readerEditControl_old.Department;
            this.readerEditControl_new.Address = this.readerEditControl_old.Address;
            this.readerEditControl_new.Tel = this.readerEditControl_old.Tel;
            this.readerEditControl_new.Email = this.readerEditControl_old.Email;

        }

        private void readerEditControl_old_GetLibraryCode(object sender, GetLibraryCodeEventArgs e)
        {
            e.LibraryCode = this.MainForm.GetReaderDbLibraryCode(e.DbName);
        }

        private void readerEditControl_new_GetLibraryCode(object sender, GetLibraryCodeEventArgs e)
        {
            e.LibraryCode = this.MainForm.GetReaderDbLibraryCode(e.DbName);
        }

    }
}