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
    /// ͣ�贰
    /// </summary>
    public partial class ReaderManageForm : MyForm
    {
        Commander commander = null;

        const int WM_LOAD_RECORD = API.WM_USER + 200;
        const int WM_SAVE_RECORD = API.WM_USER + 201;

        WebExternalHost m_webExternalHost = new WebExternalHost();

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;
#endif

        bool m_bChanged = false;

        string RecPath = "";    // ���߼�¼·��
        // string ReaderBarcode = "";  // ����֤�����
        byte [] Timestamp = null;
        string OldRecord = "";

        /// <summary>
        /// ���캯��
        /// </summary>
        public ReaderManageForm()
        {
            InitializeComponent();
        }

        private void ReaderManageForm_Load(object sender, EventArgs e)
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

            this.GetValueTable += new GetValueTableEventHandler(ReaderManageForm_GetValueTable);

            // webbrowser
            this.m_webExternalHost.Initial(this.MainForm, this.webBrowser_normalInfo);
            this.webBrowser_normalInfo.ObjectForScripting = this.m_webExternalHost;

            this.commander = new Commander(this);
            this.commander.IsBusy -= new IsBusyEventHandler(commander_IsBusy);
            this.commander.IsBusy += new IsBusyEventHandler(commander_IsBusy);

        }

        void commander_IsBusy(object sender, IsBusyEventArgs e)
        {
            e.IsBusy = this.m_webExternalHost.ChannelInUse;
        }

#if NO
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }
#endif

        void ReaderManageForm_GetValueTable(object sender, GetValueTableEventArgs e)
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

        private void ReaderManageForm_FormClosing(object sender, FormClosingEventArgs e)
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

            if (this.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ����Ϣ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
    "ReaderManageForm",
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

        private void ReaderManageForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.commander.Destroy();

            if (this.m_webExternalHost != null)
                this.m_webExternalHost.Destroy();

#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            this.GetValueTable -= new GetValueTableEventHandler(ReaderManageForm_GetValueTable);

#if NO
            MainForm.AppInfo.SaveMdiChildFormStates(this,
   "mdi_form_state");
#endif
        }

        /// <summary>
        /// ��ǰ����֤�����
        /// </summary>
        public string ReaderBarcode
        {
            get
            {
                return this.textBox_readerBarcode.Text;
            }
            set
            {
                this.textBox_readerBarcode.Text = value;
            }
        }

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                return this.m_bChanged;
            }
            set
            {
                this.m_bChanged = value;
            }
        }

        // ��ֹ���� 2009/7/19 new add
        int m_nInDropDown = 0;

        private void comboBox_operation_DropDown(object sender, EventArgs e)
        {
            // ��ֹ���� 2009/7/19 new add
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {

                ComboBox combobox = (ComboBox)sender;

                if (combobox.Items.Count == 0
                    && this.GetValueTable != null)
                {
                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    if (String.IsNullOrEmpty(this.RecPath) == false)
                        e1.DbName = Global.GetDbName(this.RecPath);

                    e1.TableName = "readerState";

                    this.GetValueTable(this, e1);

                    if (e1.values != null)
                    {
                        for (int i = 0; i < e1.values.Length; i++)
                        {
                            combobox.Items.Add(e1.values[i]);
                        }
                    }
                    else
                    {
                        combobox.Items.Add("<not found>");
                    }
                }
            }
            finally
            {
                this.Cursor = oldCursor;
                this.m_nInDropDown--;
            }
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            if (this.textBox_readerBarcode.Text == "")
            {
                MessageBox.Show(this, "��δָ������֤�����");
                return;
            }

            this.button_load.Enabled = false;

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_normalInfo.Stop();

            this.commander.AddMessage(WM_LOAD_RECORD);
        }

        /// <summary>
        /// ���ݶ���֤����ţ�װ����߼�¼
        /// </summary>
        /// <param name="strBarcode">����֤�����</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public int LoadRecord(string strBarcode)
        {
            int nRet = this.LoadRecord(ref strBarcode);
            if (this.ReaderBarcode != strBarcode)
                this.ReaderBarcode = strBarcode;
            return nRet;
        }

        // ���ݶ���֤����ţ�װ����߼�¼
        // return:
        //      0   cancelled
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strBarcode">����֤�����</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public int LoadRecord(ref string strBarcode)
        {
            string strError = "";
            int nRet = 0;

            if (this.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
"��ǰ����Ϣ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ����֤���������װ������? ",
"ReaderManageForm",
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

            this.ClearOperationInfo();

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
                    this.MainForm.AppInfo.LinkFormState(dlg, "ReaderManageForm_SelectPatronDialog_state");
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

                this.ReaderBarcode = strBarcode;

                this.RecPath = strRecPath;

                this.Timestamp = baTimestamp;

                if (results == null || results.Length < 2)
                {
                    strError = "���ص�results��������";
                    goto ERROR1;
                }
                string strXml = "";
                string strHtml = "";

                strXml = results[0];
                strHtml = results[1];

                // ����ջ�õļ�¼
                this.OldRecord = strXml;

                /*
                int nRet = this.readerEditControl1.SetData(
                    strXml,
                    strRecPath,
                    baTimestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                 * */

                /*
                this.SetXmlToWebbrowser(this.webBrowser_xml,
                    strXml);
                 * */
                Global.SetXmlToWebbrowser(this.webBrowser_xml,
                    this.MainForm.DataDir,
                    "xml",
                    strXml);

                // this.m_strSetAction = "change";

                /*
                lRet = Channel.GetReaderInfo(
                    stop,
                    strBarcode,
                    "html",
                    out strHtml,
                    out strRecPath,
                    out baTimestamp,
                    out strError);
                if (lRet == -1)
                {
                    ChargingForm.SetHtmlString(this.webBrowser_normalInfo,
"װ�ض��߼�¼��������: " + strError);

                }
                else
                {
                    ChargingForm.SetHtmlString(this.webBrowser_normalInfo,
                        strHtml);
                }
                 * */
#if NO
                Global.SetHtmlString(this.webBrowser_normalInfo,
                    strHtml,
                    this.MainForm.DataDir,
                    "readermanageform_reader");
#endif
                this.m_webExternalHost.SetHtmlString(strHtml,
                    "readermanageform_reader");

                nRet = LoadOperationInfo(out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.Changed = false;
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

#if NO
        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }
#endif

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.textBox_readerBarcode.Enabled = bEnable;
            this.textBox_operator.Enabled = bEnable;
            this.textBox_comment.Enabled = bEnable;

            this.comboBox_operation.Enabled = bEnable;
            this.tabControl_readerInfo.Enabled = bEnable;

            // 2008/10/28 new add
            this.button_save.Enabled = bEnable;
            this.button_load.Enabled = bEnable;
        }

        private void textBox_readerBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_load;
            this.MainForm.EnterPatronIdEdit(InputType.PQR);
        }

        private void textBox_readerBarcode_Leave(object sender, EventArgs e)
        {
            this.MainForm.LeavePatronIdEdit();
        }

        private void textBox_comment_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_save;
        }

        // ��XML��¼�ж���������Ϣ
        int LoadOperationInfo(out string strError)
        {
            strError = "";
            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(this.OldRecord);
            }
            catch (Exception ex)
            {
                strError = "װ��XML����DOMʱ��������: " + ex.Message;
                return -1;
            }

            this.comboBox_operation.Text = DomUtil.GetElementText(dom.DocumentElement,
                "state");

            this.textBox_comment.Text = DomUtil.GetElementText(dom.DocumentElement,
                "comment");

            this.textBox_operator.Text = this.MainForm.AppInfo.GetString(
                    "default_account",
                    "username",
                    "");

            return 0;
        }

        int BuildXml(out string strXml,
            out string strError)
        {
            strXml = "";
            strError = "";

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(this.OldRecord);
            }
            catch (Exception ex)
            {
                strError = "װ��XML����DOMʱ��������: " + ex.Message;
                return -1;
            }

            XmlNode node = DomUtil.SetElementText(dom.DocumentElement,
                "state",
                this.comboBox_operation.Text);

            DomUtil.SetAttr(node, "operator", this.textBox_operator.Text);
            DomUtil.SetAttr(node, "time", DateTimeUtil.Rfc1123DateTimeString(DateTime.UtcNow));

            DomUtil.SetElementText(dom.DocumentElement,
                "comment",
                this.textBox_comment.Text);

            strXml = dom.OuterXml;

            return 0;
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            this.button_save.Enabled = false;

            this.m_webExternalHost.StopPrevious();

            this.commander.AddMessage(WM_SAVE_RECORD);
        }

        void SaveRecord()
        {
            string strError = "";

            if (this.ReaderBarcode == "")
            {
                strError = "��δװ�ض��߼�¼��ȱ��֤�����";
                goto ERROR1;
            }

            string strXml = "";
            int nRet = BuildXml(out strXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;


            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ�����߼�¼ " + this.ReaderBarcode + " ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                ErrorCodeValue kernel_errorcode;

                byte[] baNewTimestamp = null;
                string strExistingXml = "";
                string strSavedXml = "";
                string strSavedPath = "";

                // changestate������Ҫ"setreaderinfo"��"changereaderstate"֮һȨ�ޡ�
                long lRet = Channel.SetReaderInfo(
                    stop,
                    "changestate",   // "change",
                    this.RecPath,
                    strXml,
                    this.OldRecord,
                    this.Timestamp,
                    out strExistingXml,
                    out strSavedXml,
                    out strSavedPath,
                    out baNewTimestamp,
                    out kernel_errorcode,
                    out strError);
                if (lRet == -1)
                {
                    if (kernel_errorcode == ErrorCodeValue.TimestampMismatch)
                    {
                        CompareReaderForm dlg = new CompareReaderForm();
                        dlg.Initial(
                            this.MainForm,
                            this.RecPath,
                            strExistingXml,
                            baNewTimestamp,
                            strXml,
                            this.Timestamp,
                            "���ݿ��еļ�¼�ڱ༭�ڼ䷢���˸ı䡣����ϸ�˶ԣ��������޸Ĵ����е�δ�����¼����ȷ����ť������Ա��档");

                        dlg.StartPosition = FormStartPosition.CenterScreen;
                        dlg.ShowDialog(this);
                        if (dlg.DialogResult == DialogResult.OK)
                        {
                            this.OldRecord = dlg.UnsavedXml;
                            this.RecPath = dlg.RecPath;
                            this.Timestamp = dlg.UnsavedTimestamp;

                            nRet = LoadOperationInfo(out strError);
                            if (nRet == -1)
                            {
                                MessageBox.Show(this, strError);
                            }
                            MessageBox.Show(this, "��ע�����±����¼");
                            return;
                        }
                    }

                    goto ERROR1;
                }

                this.Timestamp = baNewTimestamp;
                this.OldRecord = strSavedXml;
                this.RecPath = strSavedPath;

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
                    /*
                    this.OldRecord = strSavedXml;
                    this.RecPath = strSavedPath;
                    this.Timestamp = baNewTimestamp;

                    int nRet = LoadOperationInfo(out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // 
                    this.SetXmlToWebbrowser(this.webBrowser_xml,
                        strSavedXml);
                     * */

                }

            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            MessageBox.Show(this, "����ɹ�");
            this.Changed = false;

            // ����װ�ؼ�¼���༭��
            string strReaderBarcode = this.ReaderBarcode;
            this.LoadRecord(ref strReaderBarcode);
            if (this.ReaderBarcode != strReaderBarcode)
                this.ReaderBarcode = strReaderBarcode;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void ClearOperationInfo()
        {
            this.textBox_readerBarcode.Text = "";
            this.comboBox_operation.Text = "";
            this.textBox_comment.Text = "";

            Global.ClearHtmlPage(this.webBrowser_normalInfo,
    this.MainForm.DataDir);
            Global.ClearHtmlPage(this.webBrowser_xml,
    this.MainForm.DataDir);
            // this.webBrowser_xml.Navigate("about:blank");    // ??

        }

        private void ReaderManageForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_LOAD_RECORD:
                        if (this.m_webExternalHost.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            string strReaderBarcode = this.textBox_readerBarcode.Text;
                            this.LoadRecord(ref strReaderBarcode);
                            if (this.textBox_readerBarcode.Text != strReaderBarcode)
                                this.textBox_readerBarcode.Text = strReaderBarcode;
                        }
                    return;
                case WM_SAVE_RECORD:
                        if (this.m_webExternalHost.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            SaveRecord();
                        }
                    return;
            }
            base.DefWndProc(ref m);
        }

        private void ReaderManageForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;

        }

        private void ReaderManageForm_DragDrop(object sender, DragEventArgs e)
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
                strError = "ͣ�贰ֻ��������һ����¼";
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
                    this.textBox_readerBarcode.Text = strReaderBarcode;
                    this.button_load_Click(this, null);
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

        private void comboBox_operation_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_operation.Invalidate();
        }


    }
}