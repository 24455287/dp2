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
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.IO;
using DigitalPlatform.Drawing;
using DigitalPlatform.Interfaces;

using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.CommonControl;
using DigitalPlatform.Script;
using DigitalPlatform.dp2.Statis;

namespace dp2Circulation
{
    /// <summary>
    /// ������Ϣ������
    /// </summary>
    public partial class ReaderInfoForm : MyForm
    {
        int m_nChannelInUse = 0; // >0��ʾͨ�����ڱ�ʹ��

        Commander commander = null;

        const int WM_NEXT_RECORD = API.WM_USER + 200;
        const int WM_PREV_RECORD = API.WM_USER + 201;
        const int WM_LOAD_RECORD = API.WM_USER + 202;
        const int WM_DELETE_RECORD = API.WM_USER + 203;
        const int WM_HIRE = API.WM_USER + 204;
        const int WM_SAVETO = API.WM_USER + 205;
        const int WM_SAVE_RECORD = API.WM_USER + 206;
        const int WM_FOREGIFT = API.WM_USER + 207;
        const int WM_RETURN_FOREGIFT = API.WM_USER + 208;
        const int WM_SET_FOCUS = API.WM_USER + 209;

        WebExternalHost m_webExternalHost = new WebExternalHost();

        string m_strSetAction = "new";

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;
#endif

        // byte[] timestamp = null;
        // string m_strPath = "";

        // bool m_bChanged = false;

        // public byte[] Timestamp = null; // ���߼�¼��ʱ���
        // public string RecPath = ""; // ���߼�¼·��

        // public string OldRecord = "";

        SelectedTemplateCollection selected_templates = new SelectedTemplateCollection();

        /// <summary>
        /// ���캯��
        /// </summary>
        public ReaderInfoForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ��ǰ����֤�����
        /// </summary>
        public string ReaderBarcode
        {
            get
            {
                return this.toolStripTextBox_barcode.Text;  //  this.textBox_readerBarcode.Text;
            }
            set
            {
                this.toolStripTextBox_barcode.Text = value; //  this.textBox_readerBarcode.Text = value;
            }
        }

        // �ⲿʹ��
        /// <summary>
        /// ������Ϣ�༭�ؼ�
        /// </summary>
        public ReaderEditControl ReaderEditControl
        {
            get
            {
                return this.readerEditControl1;
            }
        }

        private void ReaderInfoForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);

#if NO
                // ���ڴ�ʱ��ʼ��
                this.m_bSuppressScriptErrors = !this.MainForm.DisplayScriptErrorDialog;
#endif
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

            this.readerEditControl1.SetReadOnly("librarian");

            this.readerEditControl1.GetValueTable += new GetValueTableEventHandler(readerEditControl1_GetValueTable);

            this.readerEditControl1.Initializing = false;   // ���û�д˾䣬һ��ʼ�ڿ�ģ�����޸ľͲ����ɫ

            //
            this.binaryResControl1.ContentChanged -= new ContentChangedEventHandler(binaryResControl1_ContentChanged);
            this.binaryResControl1.ContentChanged += new ContentChangedEventHandler(binaryResControl1_ContentChanged);

            this.binaryResControl1.Channel = this.Channel;
            this.binaryResControl1.Stop = this.stop;



            // webbrowser
            this.m_webExternalHost.Initial(this.MainForm, this.webBrowser_readerInfo);
            this.m_webExternalHost.GetLocalPath -= new GetLocalFilePathEventHandler(m_webExternalHost_GetLocalPath);
            this.m_webExternalHost.GetLocalPath += new GetLocalFilePathEventHandler(m_webExternalHost_GetLocalPath);
            this.webBrowser_readerInfo.ObjectForScripting = this.m_webExternalHost;

            this.commander = new Commander(this);
            this.commander.IsBusy -= new IsBusyEventHandler(commander_IsBusy);
            this.commander.IsBusy += new IsBusyEventHandler(commander_IsBusy);

            string strSelectedTemplates = this.MainForm.AppInfo.GetString(
    "readerinfo_form",
    "selected_templates",
    "");
            if (String.IsNullOrEmpty(strSelectedTemplates) == false)
            {
                selected_templates.Build(strSelectedTemplates);
            }

            API.PostMessage(this.Handle, WM_SET_FOCUS, 0, 0);
        }

        void m_webExternalHost_GetLocalPath(object sender, GetLocalFilePathEventArgs e)
        {
            if (e.Name == "PatronCardPhoto")
            {
                List<ListViewItem> items = this.binaryResControl1.FindItemByUsage("cardphoto");
                if (items.Count > 0)
                {
                    string strError = "";
                    string strLocalPath = "";
                    // return:
                    //      -1  ����
                    //      0   �������޸Ļ��ߴ�������δ���ص����
                    //      1   �ɹ�
                    int nRet = this.binaryResControl1.GetUnuploadFilePath(items[0],
            out strLocalPath,
            out strError);
                    e.LocalFilePath = strLocalPath;
                    // ע������·��""��ʾ�������͵Ķ����У�����û�б����ļ���Ҳ����˵�Ѿ����أ���Ҫ�ӷ�������
                }
                else
                {
                    e.LocalFilePath = null; // null��ʾ����û���������͵Ķ���
                }
            }
        }

        void commander_IsBusy(object sender, IsBusyEventArgs e)
        {
            e.IsBusy = this.m_webExternalHost.ChannelInUse;
        }

        void binaryResControl1_ContentChanged(object sender, ContentChangedEventArgs e)
        {
        }


        // 
        /// <summary>
        /// ������Ϣ�Ƿ񱻸ı�
        /// </summary>
        public bool ObjectChanged
        {
            get
            {
                if (this.binaryResControl1 != null)
                    return this.binaryResControl1.Changed;

                return false;
            }
            set
            {
                if (this.binaryResControl1 != null)
                    this.binaryResControl1.Changed = value;
            }

        }

        /// <summary>
        /// ���߼�¼ XML �Ƿ񱻸ı�
        /// </summary>
        public bool ReaderXmlChanged
        {
            get
            {
                if (this.readerEditControl1 != null)
                {
                    // ���object id�����ı䣬��ô����XML��¼û�иı䣬�����ĺϳ�XMLҲ�����˸ı�
                    if (this.binaryResControl1 != null)
                    {
                        if (this.binaryResControl1.IsIdUsageChanged() == true)
                            return true;
                    }

                    return this.readerEditControl1.Changed;
                }

                return false;
            }
            set
            {
                if (this.readerEditControl1 != null)
                    this.readerEditControl1.Changed = value;
            }
        }

        /*
        // 2008/10/28 new add
        void NewExternal()
        {
            if (this.m_webExternalHost != null)
            {
                this.m_webExternalHost.Close();
                this.webBrowser_readerInfo.ObjectForScripting = null;
            }

            this.m_webExternalHost = new WebExternalHost();
            this.m_webExternalHost.Initial(this.MainForm);
            this.webBrowser_readerInfo.ObjectForScripting = this.m_webExternalHost;
        }*/

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

        private void ReaderInfoForm_FormClosing(object sender, FormClosingEventArgs e)
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

            if (this.ReaderXmlChanged == true
                || this.ObjectChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ����Ϣ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
    "ReaderInfoForm",
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

        private void ReaderInfoForm_FormClosed(object sender, FormClosedEventArgs e)
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

            string strSelectedTemplates = selected_templates.Export();
            this.MainForm.AppInfo.SetString(
                "readerinfo_form",
                "selected_templates",
                strSelectedTemplates);

            this.readerEditControl1.GetValueTable -= new GetValueTableEventHandler(readerEditControl1_GetValueTable);

#if NO
            MainForm.AppInfo.SaveMdiChildFormStates(this,
"mdi_form_state");
#endif

        }

        /*
        public string Path
        {
            get
            {
                return m_strPath;
            }
            set
            {
                m_strPath = value;
            }
        }
         */

#if NO
        void SetXmlToWebbrowser(WebBrowser webbrowser,
            string strXml)
        {
            string strTargetFileName = MainForm.DataDir + "\\xml.xml";

            /*
            StreamWriter sw = new StreamWriter(strTargetFileName,
                false,	// append
                System.Text.Encoding.UTF8);
            sw.Write(strXml);
            sw.Close();
             * */
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strTargetFileName = MainForm.DataDir + "\\xml.txt";
                StreamWriter sw = new StreamWriter(strTargetFileName,
    false,	// append
    System.Text.Encoding.UTF8);
                sw.Write("XML����װ��DOMʱ����: " + ex.Message + "\r\n\r\n" + strXml);
                sw.Close();
                webbrowser.Navigate(strTargetFileName);

                return;
            }

            dom.Save(strTargetFileName);
            webbrowser.Navigate(strTargetFileName);
        }
#endif

        public void AsyncLoadRecord(string strBarcode)
        {
            this.toolStripTextBox_barcode.Text = strBarcode;

            EnableToolStrip(false);

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_LOAD_RECORD);
        }

        // ���ݶ���֤����ţ�װ����߼�¼
        // parameters:
        //      bForceLoad  �ڷ����������������Ƿ�ǿ��װ���һ��
        /// <summary>
        /// ���ݶ���֤����ţ�װ����߼�¼
        /// </summary>
        /// <param name="strBarcode">����֤�����</param>
        /// <param name="bForceLoad">�ڷ����������������Ƿ�ǿ��װ���һ��</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public int LoadRecord(string strBarcode,
            bool bForceLoad)
        {
            string strError = "";
            int nRet = 0;

#if NO
            // 2013/12/4
            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();
#endif

            if (this.ReaderXmlChanged == true
                || this.ObjectChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
"��ǰ����Ϣ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ����֤���������װ������? ",
"ReaderInfoForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                    return 0;   // cancelled

            }

            this.m_nChannelInUse++;
            if (this.m_nChannelInUse > 1)
            {
                this.m_nChannelInUse--;
                strError = "ͨ���Ѿ���ռ�á����Ժ�����";
                goto ERROR1;
            }
            try
            {

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڳ�ʼ���������� ...");
                stop.BeginLoop();

                this.Update();
                this.MainForm.Update();

                EnableControls(false);

                this.readerEditControl1.Clear();
#if NO
                Global.ClearHtmlPage(this.webBrowser_readerInfo,
                    this.MainForm.DataDir);
#endif
                ClearReaderHtmlPage();
                this.binaryResControl1.Clear();

                try
                {
                    byte[] baTimestamp = null;
                    string strOutputRecPath = "";
                    int nRedoCount = 0;

                    REDO:
                    stop.SetMessage("����װ����߼�¼ " + strBarcode + " ...");

                    string[] results = null;
                    long lRet = Channel.GetReaderInfo(
                        stop,
                        strBarcode,
                        "xml,html",
                        out results,
                        out strOutputRecPath,
                        out baTimestamp,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (lRet == 0)
                        goto ERROR1;

                    if (lRet > 1)
                    {
#if NO
                        if (bForceLoad == true)
                        {
                            strError = "���� " + strBarcode + " ���м�¼ " + lRet.ToString() + " ��������װ�����е�һ�����߼�¼��\r\n\r\n����һ�����ش�����ϵͳ����Ա�����ų���";
                            MessageBox.Show(this, strError);    // ��������װ���һ�� 
                        }
                        else
                        {
                            strError = "���� " + strBarcode + " ���м�¼ " + lRet.ToString() + " ��������װ����߼�¼��\r\n\r\nע������һ�����ش�����ϵͳ����Ա�����ų���";
                            goto ERROR1;    // ��������
                        }
#endif
                        // ������Ժ���Ȼ�����ظ�
                        if (nRedoCount > 0)
                        {
                            if (bForceLoad == true)
                            {
                                strError = "���� " + strBarcode + " ���м�¼ " + lRet.ToString() + " ��������װ�����е�һ�����߼�¼��\r\n\r\n����һ�����ش�����ϵͳ����Ա�����ų���";
                                MessageBox.Show(this, strError);    // ��������װ���һ�� 
                            }
                            else
                            {
                                strError = "���� " + strBarcode + " ���м�¼ " + lRet.ToString() + " ��������װ����߼�¼��\r\n\r\nע������һ�����ش�����ϵͳ����Ա�����ų���";
                                goto ERROR1;    // ��������
                            }
                        }

                        SelectPatronDialog dlg = new SelectPatronDialog();

                        dlg.Overflow = StringUtil.SplitList(strOutputRecPath).Count < lRet;
                        nRet = dlg.Initial(
                            this.MainForm,
                            this.Channel,
                            this.stop,
                            StringUtil.SplitList(strOutputRecPath),
                            "��ѡ��һ�����߼�¼",
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        this.MainForm.AppInfo.LinkFormState(dlg, "ReaderInfoForm_SelectPatronDialog_state");
                        dlg.ShowDialog(this);
                        this.MainForm.AppInfo.UnlinkFormState(dlg);

                        if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                            return 0;

                        strBarcode = dlg.SelectedBarcode;
                        nRedoCount++;
                        goto REDO;
                    }

                    this.ReaderBarcode = strBarcode;

                    /*
                    this.RecPath = strRecPath;

                    this.Timestamp = baTimestamp;

                    // ����ջ�õļ�¼
                    this.OldRecord = strXml;
                     */


                    if (results == null || results.Length < 2)
                    {
                        strError = "���ص�results��������";
                        goto ERROR1;
                    }

                    string strXml = "";
                    string strHtml = "";
                    strXml = results[0];
                    strHtml = results[1];

                    nRet = this.readerEditControl1.SetData(
                        strXml,
                        strOutputRecPath,
                        baTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;


                    // ����װ�������Դ
                    {
                        nRet = this.binaryResControl1.LoadObject(strOutputRecPath,    // 2008/11/2 changed
                            strXml,
                            out strError);
                        if (nRet == -1)
                        {
                            MessageBox.Show(this, strError);
                            // return -1;
                        }
                    }

                    /*
                    this.SetXmlToWebbrowser(this.webBrowser_xml,
                        strXml);
                     * */
                    Global.SetXmlToWebbrowser(this.webBrowser_xml,
                        this.MainForm.DataDir,
                        "xml",
                        strXml);

                    this.m_strSetAction = "change";

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
                        ChargingForm.SetHtmlString(this.webBrowser_readerInfo,
    "װ�ض��߼�¼��������: " + strError);

                    }
                    else
                    {
                        ChargingForm.SetHtmlString(this.webBrowser_readerInfo,
                            strHtml,
        this.MainForm.DataDir,
        "readerinfoform_reader");
                    }
                     * */

#if NO
                    // 2013/12/21
                    this.m_webExternalHost.StopPrevious();
                    this.webBrowser_readerInfo.Stop();

                    Global.SetHtmlString(this.webBrowser_readerInfo,
        strHtml,
        this.MainForm.DataDir,
        "readerinfoform_reader");
#endif
                    this.SetReaderHtmlString(strHtml);

                }
                finally
                {
                    EnableControls(true);

                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }
            }
            finally
            {
                this.m_nChannelInUse--;
            }

            return 1;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        void ClearReaderHtmlPage()
        {
            // 2013/12/21
            this.m_webExternalHost.StopPrevious();
            // this.webBrowser_readerInfo.Stop();

            Global.ClearHtmlPage(this.webBrowser_readerInfo,
    this.MainForm.DataDir);
        }

        void SetReaderHtmlString(string strHtml)
        {
#if NO
            // 2013/12/21
            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            Global.SetHtmlString(this.webBrowser_readerInfo,
strHtml,
this.MainForm.DataDir,
"readerinfoform_reader");
#endif
            this.m_webExternalHost.SetHtmlString(strHtml,
                "readerinfoform_reader");
        }

        // ���ݶ��߼�¼·����װ����߼�¼
        // parameters:
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        /// <summary>
        /// ���ݶ��߼�¼·����װ����߼�¼
        /// </summary>
        /// <param name="strRecPath">���߼�¼·��</param>
        /// <param name="strPrevNextStyle">����</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public int LoadRecordByRecPath(string strRecPath,
            string strPrevNextStyle = "")
        {
            string strError = "";

#if NO
            // 2013/12/4
            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();
#endif

            if (this.ReaderXmlChanged == true
                || this.ObjectChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
"��ǰ����Ϣ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ���ݼ�¼·������װ������? ",
"ReaderInfoForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                    return 0;   // cancelled

            }

            this.m_nChannelInUse++;
            if (this.m_nChannelInUse > 1)
            {
                this.m_nChannelInUse--;
                strError = "ͨ���Ѿ���ռ�á����Ժ�����";
                goto ERROR1;
            }
            try
            {
                bool bPrevNext = false;

                if (String.IsNullOrEmpty(strPrevNextStyle) == false)
                {
                    strRecPath += "$" + strPrevNextStyle.ToLower();
                    bPrevNext = true;
                }

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڳ�ʼ���������� ...");
                stop.BeginLoop();

                this.Update();
                this.MainForm.Update();

                EnableControls(false);

                // NewExternal();

                if (bPrevNext == false)
                {
                    this.readerEditControl1.Clear();
#if NO
                    Global.ClearHtmlPage(this.webBrowser_readerInfo,
                        this.MainForm.DataDir);
#endif
                    ClearReaderHtmlPage();

                    this.binaryResControl1.Clear();
                }

                try
                {
                    byte[] baTimestamp = null;
                    string strOutputRecPath = "";

                    stop.SetMessage("����װ����߼�¼ " + strRecPath + " ...");

                    string[] results = null;
                    long lRet = Channel.GetReaderInfo(
                        stop,
                        "@path:" + strRecPath,
                        "xml,html",
                        out results,
                        out strOutputRecPath,
                        out baTimestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        if (bPrevNext == true)
                        {
                            strError += "\r\n\r\n�¼�¼û��װ�أ������л�������װ��ǰ�ļ�¼";
                        }
                        goto ERROR1;
                    }

                    if (lRet == 0)
                    {
                        if (bPrevNext == true)
                        {
                            strError += "\r\n\r\n�¼�¼û��װ�أ������л�������װ��ǰ�ļ�¼";
                        }
                        goto ERROR1;
                    }

                    if (lRet > 1)   // �����ܷ�����?
                    {
                        strError = "��¼·�� " + strRecPath + " ���м�¼ " + lRet.ToString() + " ��������װ����߼�¼��\r\n\r\nע������һ�����ش�����ϵͳ����Ա�����ų���";
                        goto ERROR1;
                    }


                    /*
                    this.RecPath = strRecPath;

                    this.Timestamp = baTimestamp;

                    // ����ջ�õļ�¼
                    this.OldRecord = strXml;
                     */


                    if (results == null || results.Length < 2)
                    {
                        strError = "���ص�results��������";
                        goto ERROR1;
                    }

                    string strXml = "";
                    string strHtml = "";
                    strXml = results[0];
                    strHtml = results[1];

                    int nRet = this.readerEditControl1.SetData(
                        strXml,
                        strOutputRecPath,   // strRecPath,
                        baTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // ����װ�������Դ
                    {
                        this.binaryResControl1.Clear();
                        nRet = this.binaryResControl1.LoadObject(strOutputRecPath,    // 2008/11/2 changed
                            strXml,
                            out strError);
                        if (nRet == -1)
                        {
                            MessageBox.Show(this, strError);
                            // return -1;
                        }
                    }

                    this.ReaderBarcode = this.readerEditControl1.Barcode;

                    /*
                    this.SetXmlToWebbrowser(this.webBrowser_xml,
                        strXml);
                     * */
                    Global.SetXmlToWebbrowser(this.webBrowser_xml,
    this.MainForm.DataDir,
    "xml",
    strXml);

                    this.m_strSetAction = "change";

#if NO
                    // 2013/12/21
                    this.m_webExternalHost.StopPrevious();
                    this.webBrowser_readerInfo.Stop();

                    Global.SetHtmlString(this.webBrowser_readerInfo,
        strHtml,
        this.MainForm.DataDir,
        "readerinfoform_reader");
#endif
                    this.SetReaderHtmlString(strHtml);
                }
                finally
                {
                    EnableControls(true);

                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }
            }
            finally
            {
                this.m_nChannelInUse--;
            }

            return 1;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

#if NO
        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }
#endif

        /*public*/ void SetMenuItemState()
        {
            // �˵�

            // ��������ť

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        void EnableSendKey(bool bEnable)
        {
            // 2014/10/12
            if (this.MainForm == null)
                return;

            if (string.IsNullOrEmpty(this.MainForm.IdcardReaderUrl) == true)
                return;

            int nRet = 0;
            string strError = "";
            try
            {
                nRet = StartIdcardChannel(
                    this.MainForm.IdcardReaderUrl,
                    out strError);
                if (nRet == -1)
                    return;

                if (m_idcardObj.SendKeyEnabled != bEnable)
                    m_idcardObj.SendKeyEnabled = bEnable;
            }
            catch
            {
                return;
            }
            finally
            {
                try
                {
                    EndIdcardChannel();
                }
                catch
                {
                }
            }
        }

        private void ReaderInfoForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            SetMenuItemState();

            if (this.DisableIdcardReaderSendkey == true)
            {
                EnableSendKey(false);
            }
            // Debug.WriteLine("Activated");
        }

        private void ReaderInfoForm_Deactivate(object sender, EventArgs e)
        {
            if (this.DisableIdcardReaderSendkey == true)
            {
                EnableSendKey(true);
            }

            // Debug.WriteLine("DeActivated");
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            // this.textBox_readerBarcode.Enabled = bEnable;
            // this.button_load.Enabled = bEnable;
            this.toolStrip_load.Enabled = bEnable;


            this.readerEditControl1.Enabled = bEnable;

            if (bEnable == false)
                this.toolStripButton_delete.Enabled = bEnable;
            else
            {
                if (this.readerEditControl1.RecPath != "")
                    this.toolStripButton_delete.Enabled = true;  // ֻ�о߱���ȷ��·���ļ�¼�����ܱ�ɾ��
                else
                    this.toolStripButton_delete.Enabled = false;
            }

            this.toolStripButton_loadFromIdcard.Enabled = bEnable;

            this.toolStripDropDownButton_loadBlank.Enabled = bEnable;
            this.toolStripButton_loadBlank.Enabled = bEnable;

            this.toolStripButton_webCamera.Enabled = bEnable;
            this.toolStripButton_pasteCardPhoto.Enabled = bEnable;

            this.toolStripButton_registerFingerprint.Enabled = bEnable;
            this.toolStripButton_createMoneyRecord.Enabled = bEnable;

            this.toolStripButton_saveTo.Enabled = bEnable;
            this.toolStripButton_save.Enabled = bEnable;

            this.toolStripButton_clearOutofReservationCount.Enabled = bEnable;

            this.toolStripButton_option.Enabled = bEnable;

            this.toolStripDropDownButton_otherFunc.Enabled = bEnable;

            // 2008/10/28 new add
            this.toolStripButton_next.Enabled = bEnable;
            this.toolStripButton_prev.Enabled = bEnable;
        }

        private void toolStripTextBox_barcode_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                // �س�
                case Keys.Enter:
                    // toolStripTextBox_barcode.Enabled = false;
                    // toolStripTextBox_barcode.SelectAll();   //
                    toolStripButton_load_Click(sender, new EventArgs());
                    //e.Handled = true;
                    //e.SuppressKeyPress = true;
                    break;
            }
        }

        private void toolStripButton_load_Click(object sender, EventArgs e)
        {
            if (this.toolStripTextBox_barcode.Text == "")
            {
                MessageBox.Show(this, "��δָ������֤�����");
                return;
            }

            EnableToolStrip(false);

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_LOAD_RECORD);

        }

#if NO
        private void button_load_Click(object sender, EventArgs e)
        {
            if (this.textBox_readerBarcode.Text == "")
            {
                MessageBox.Show(this, "��δָ������֤�����");
                return;
            }

            this.toolStrip1.Enabled = false;

            this.m_webExternalHost.StopPrevious();
                    this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_LOAD_RECORD);
        }

        private void textBox_readerBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_load;
        }

                private void textBox_readerBarcode_TextChanged(object sender, EventArgs e)
        {
            this.UpdateWindowTitle();
        }
#endif

        private void toolStripTextBox_barcode_TextChanged(object sender, EventArgs e)
        {
            this.UpdateWindowTitle();
        }

        void UpdateWindowTitle()
        {
            this.Text = "���� " + this.toolStripTextBox_barcode.Text; // this.textBox_readerBarcode.Text;
        }

        // ���������ļ�
        int SaveCfgFile(string strCfgFilePath,
            string strContent,
            byte[] baTimestamp,
            out string strError)
        {
            strError = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ��������ļ� ...");
            stop.BeginLoop();

            try
            {
                stop.SetMessage("���ڱ��������ļ� " + strCfgFilePath + " ...");

                byte[] output_timestamp = null;
                string strOutputPath = "";

                long lRet = Channel.WriteRes(
                    stop,
                    strCfgFilePath,
                    strContent,
                    true,
                    "",	// style
                    baTimestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

        int m_nInGetCfgFile = 0;    // ��ֹGetCfgFile()��������


        // ��������ļ�
        // parameters:
        //      
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetCfgFileContent(string strCfgFilePath,
            out string strContent,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";
            strContent = "";

            if (m_nInGetCfgFile > 0)
            {
                strError = "GetCfgFile() ������";
                return -1;
            }


            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�������������ļ� ...");
            stop.BeginLoop();

            m_nInGetCfgFile++;

            try
            {
                stop.SetMessage("�������������ļ� " + strCfgFilePath + " ...");
                string strMetaData = "";
                string strOutputPath = "";

                string strStyle = "content,data,metadata,timestamp,outputpath";

                long lRet = Channel.GetRes(stop,
                    MainForm.cfgCache,
                    strCfgFilePath,
                    strStyle,
                    null,
                    out strContent,
                    out strMetaData,
                    out baOutputTimestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    if (Channel.ErrorCode == ErrorCode.NotFound)
                        return 0;

                    goto ERROR1;
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                m_nInGetCfgFile--;
            }

            return 1;
        ERROR1:
            return -1;
        }

        // 
        /// <summary>
        /// ���浱ǰ�����ڼ�¼��ģ�������ļ�
        /// </summary>
        public void SaveReaderToTemplate()
        {
            this.EnableControls(false);

            try
            {

                // ���·�������Ѿ��еĶ��߿���
                string strReaderDbName = Global.GetDbName(this.readerEditControl1.RecPath);

                GetDbNameDlg dlg = new GetDbNameDlg();
                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.DbType = "reader";
                dlg.DbName = strReaderDbName;
                dlg.MainForm = this.MainForm;
                dlg.Text = "��ѡ��Ŀ����߿���";
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.ShowDialog(this);

                if (dlg.DialogResult != DialogResult.OK)
                    return;

                strReaderDbName = dlg.DbName;


                // ����ģ�������ļ�
                string strContent = "";
                string strError = "";

                byte[] baTimestamp = null;

                // return:
                //      -1  error
                //      0   not found
                //      1   found
                int nRet = GetCfgFileContent(strReaderDbName + "/cfgs/template",
                    out strContent,
                    out baTimestamp,
                    out strError);
                if (nRet == -1 || nRet == 0)
                {
                    goto ERROR1;
                }

                SelectRecordTemplateDlg tempdlg = new SelectRecordTemplateDlg();
                MainForm.SetControlFont(tempdlg, this.Font, false);
                nRet = tempdlg.Initial(
                    true,
                    strContent, out strError);
                if (nRet == -1)
                    goto ERROR1;


                tempdlg.Text = "��ѡ��Ҫ�޸ĵ�ģ���¼";
                tempdlg.CheckNameExist = false;	// ��OK��ťʱ������"���ֲ�����",���������½�һ��ģ��
                //tempdlg.ap = this.MainForm.applicationInfo;
                //tempdlg.ApCfgTitle = "detailform_selecttemplatedlg";
                tempdlg.ShowDialog(this);

                if (tempdlg.DialogResult != DialogResult.OK)
                    return;

                string strNewXml = "";
                nRet = this.readerEditControl1.GetData(
                    out strNewXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ��Ҫ����password/displayNameԪ������
                {
                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strNewXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "װ��XML��DOM����: " + ex.Message;
                        goto ERROR1;
                    }
                    DomUtil.SetElementText(dom.DocumentElement,
                        "password", "");
                    DomUtil.SetElementText(dom.DocumentElement,
                        "displayName", "");

                    strNewXml = dom.OuterXml;
                }

                // �޸������ļ�����
                if (tempdlg.textBox_name.Text != "")
                {
                    // �滻����׷��һ����¼
                    nRet = tempdlg.ReplaceRecord(tempdlg.textBox_name.Text,
                        strNewXml,
                        out strError);
                    if (nRet == -1)
                    {
                        goto ERROR1;
                    }
                }

                if (tempdlg.Changed == false)	// û�б�Ҫ�����ȥ
                    return;

                string strOutputXml = tempdlg.OutputXml;

                // Debug.Assert(false, "");
                nRet = SaveCfgFile(strReaderDbName + "/cfgs/template",
                    strOutputXml,
                    baTimestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.MainForm.StatusBarMessage = "�޸�ģ��ɹ���";
                return;

            ERROR1:
                MessageBox.Show(this, strError);
            }
            finally
            {
                this.EnableControls(true);
            }
        }

        // װ�ض��߼�¼ģ��
        // return:
        //      -1  error
        //      0   ����
        //      1   �ɹ�װ��
        /// <summary>
        /// װ�ض��߼�¼ģ��
        /// </summary>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public int LoadReaderTemplateFromServer()
        {
            this.EnableControls(false);

            try
            {

                int nRet = 0;
                string strError = "";

                bool bShift = (Control.ModifierKeys == Keys.Shift);

                if (this.ReaderXmlChanged == true
                    || this.ObjectChanged == true)
                {
                    // ������δ����
                    DialogResult result = MessageBox.Show(this,
        "��ǰ����Ϣ���޸ĺ���δ���档����ʱ�������¶�����Ϣ������δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�����¶�����Ϣ? ",
        "ReaderInfoForm",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button2);
                    if (result != DialogResult.Yes)
                        return 0;
                }

                this.binaryResControl1.Clear();
                this.ObjectChanged = false; // 2013/10/17

                nRet = this.readerEditControl1.SetData("<root />",
         "",
         null,
         out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.readerEditControl1.Changed = false;

                string strSelectedDbName = this.MainForm.AppInfo.GetString(
                    "readerinfo_form",
                    "selected_dbname_for_loadtemplate",
                    "");

                SelectedTemplate selected = this.selected_templates.Find(strSelectedDbName);

                GetDbNameDlg dbname_dlg = new GetDbNameDlg();
                MainForm.SetControlFont(dbname_dlg, this.Font, false);
                dbname_dlg.DbType = "reader";
                if (selected != null)
                {
                    dbname_dlg.NotAsk = selected.NotAskDbName;
                    dbname_dlg.AutoClose = (bShift == true ? false : selected.NotAskDbName);
                }

                dbname_dlg.EnableNotAsk = true;
                dbname_dlg.DbName = strSelectedDbName;
                dbname_dlg.MainForm = this.MainForm;

                dbname_dlg.Text = "װ�ض��߼�¼ģ�� -- ��ѡ��Ŀ����߿���";
                //  dbname_dlg.StartPosition = FormStartPosition.CenterScreen;

                this.MainForm.AppInfo.LinkFormState(dbname_dlg, "readerinfoformm_load_template_GetBiblioDbNameDlg_state");
                dbname_dlg.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(dbname_dlg);

                if (dbname_dlg.DialogResult != DialogResult.OK)
                    return 0;

                string strReaderDbName = dbname_dlg.DbName;
                // ����
                this.MainForm.AppInfo.SetString(
                    "readerinfo_form",
                    "selected_dbname_for_loadtemplate",
                    strReaderDbName);

                selected = this.selected_templates.Find(strReaderDbName);

                this.readerEditControl1.RecPath = dbname_dlg.DbName + "/?";	// Ϊ��׷�ӱ���
                this.readerEditControl1.Changed = false;

                // ���������ļ�
                string strContent = "";

                // string strCfgFilePath = respath.Path + "/cfgs/template";
                byte[] baCfgOutputTimestamp = null;
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = GetCfgFileContent(strReaderDbName + "/cfgs/template",
                    out strContent,
                    out baCfgOutputTimestamp,
                    out strError);
                if (nRet == 0)
                {
                    MessageBox.Show(this, strError + "\r\n\r\n������λ�ڱ��ص� ��ѡ��/������Ϣȱʡֵ�� ��ˢ�¼�¼");

                    // ���template�ļ������ڣ����ұ������õ�ģ��
                    string strNewDefault = this.MainForm.AppInfo.GetString(
    "readerinfoform_optiondlg",
    "newreader_default",
    "<root />");
                    nRet = this.readerEditControl1.SetData(strNewDefault,
                         "",
                         null,
                         out strError);
                    if (nRet == -1)
                        MessageBox.Show(this, strError);

                    // this.ClearCardPhoto();
                    this.binaryResControl1.Clear();
                    this.ObjectChanged = false; // 2013/10/17

#if NO
                    Global.ClearHtmlPage(this.webBrowser_readerInfo,
                        this.MainForm.DataDir);
#endif
                    ClearReaderHtmlPage();

                    
                    /*
                    this.SetXmlToWebbrowser(this.webBrowser_xml,
                        strNewDefault);
                     * */
                    Global.SetXmlToWebbrowser(this.webBrowser_xml,
    this.MainForm.DataDir,
    "xml",
    strNewDefault);

                    this.m_strSetAction = "new";
                    this.m_strLoadSource = "local";
                    return -1;
                }
                if (nRet == -1 || nRet == 0)
                {
                    this.readerEditControl1.Timestamp = null;
                    goto ERROR1;
                }

                // MessageBox.Show(this, strContent);

                SelectRecordTemplateDlg select_temp_dlg = new SelectRecordTemplateDlg();
                MainForm.SetControlFont(select_temp_dlg, this.Font, false);

                select_temp_dlg.Text = "��ѡ���¶��߼�¼ģ�� -- ���Կ� '" + strReaderDbName + "'";
                string strSelectedTemplateName = "";
                bool bNotAskTemplateName = false;
                if (selected != null)
                {
                    strSelectedTemplateName = selected.TemplateName;
                    bNotAskTemplateName = selected.NotAskTemplateName;
                }

                select_temp_dlg.SelectedName = strSelectedTemplateName;
                select_temp_dlg.AutoClose = (bShift == true ? false : bNotAskTemplateName);
                select_temp_dlg.NotAsk = bNotAskTemplateName;

                nRet = select_temp_dlg.Initial(
                    false,
                    strContent,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�������ļ� '" + "template" + "' ��������: " + strError;
                    goto ERROR1;
                }

                this.MainForm.AppInfo.LinkFormState(select_temp_dlg, "readerinfoform_load_template_SelectTemplateDlg_state");
                select_temp_dlg.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(select_temp_dlg);

                if (select_temp_dlg.DialogResult != DialogResult.OK)
                    return 0;

                // ���䱾�ε�ѡ���´ξͲ����ٽ��뱾�Ի�����
                this.selected_templates.Set(strReaderDbName,
                    dbname_dlg.NotAsk,
                    select_temp_dlg.SelectedName,
                    select_temp_dlg.NotAsk);

                this.readerEditControl1.Timestamp = null;

                // this.BiblioOriginPath = ""; // ��������ݿ�������ԭʼpath


                nRet = this.readerEditControl1.SetData(
        select_temp_dlg.SelectedRecordXml,
        dbname_dlg.DbName + "/?",
        null,
        out strError);
                if (nRet == -1)
                    goto ERROR1;

                /*
                this.SetXmlToWebbrowser(this.webBrowser_xml,
                    select_temp_dlg.SelectedRecordXml);
                 * */
                Global.SetXmlToWebbrowser(this.webBrowser_xml,
this.MainForm.DataDir,
"xml",
select_temp_dlg.SelectedRecordXml);

                this.m_strSetAction = "new";
                this.m_strLoadSource = "server";

#if NO
                Global.ClearHtmlPage(this.webBrowser_readerInfo,
                    this.MainForm.DataDir);
#endif
                ClearReaderHtmlPage();

                this.readerEditControl1.Changed = false;
                return 1;
            ERROR1:
                MessageBox.Show(this, strError);
                return -1;
            }
            finally
            {
                this.EnableControls(true);
            }
        }


        // װ��һ���հ׼�¼[�ӱ���]
        // return:
        //      -1  error
        //      0   ����
        //      1   �ɹ�װ��
        /// <summary>
        /// �ӱ���װ��һ���հ׼�¼
        /// </summary>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public int LoadReaderTemplateFromLocal()
        {
            if (this.ReaderXmlChanged == true
                || this.ObjectChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ����Ϣ���޸ĺ���δ���档����ʱ�������¶�����Ϣ������δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�����¶�����Ϣ? ",
    "ReaderInfoForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                    return 0;
            }

            this.EnableControls(false);

            try
            {
                string strError = "";

                string strNewDefault = this.MainForm.AppInfo.GetString(
        "readerinfoform_optiondlg",
        "newreader_default",
        "<root />");
                int nRet = this.readerEditControl1.SetData(strNewDefault,
                     "",
                     null,
                     out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return -1;
                }

                // this.ClearCardPhoto();
                this.binaryResControl1.Clear();

#if NO
                Global.ClearHtmlPage(this.webBrowser_readerInfo,
                    this.MainForm.DataDir);
#endif
                ClearReaderHtmlPage();

                /*
                this.SetXmlToWebbrowser(this.webBrowser_xml,
                    strNewDefault);
                 * */
                Global.SetXmlToWebbrowser(this.webBrowser_xml,
this.MainForm.DataDir,
"xml",
strNewDefault);

                this.m_strSetAction = "new";
                this.m_strLoadSource = "local";

                this.readerEditControl1.Changed = false; // 2013/10/17
                this.ObjectChanged = false; // 2013/10/17
                return 1;
            }
            finally
            {
                this.EnableControls(true);
            }
        }

        void EnableToolStrip(bool bEnable)
        {
            toolStripTextBox_barcode.Enabled = bEnable;
            this.toolStrip1.Enabled = bEnable;
        }

        // ����
        private void toolStripButton_save_Click(object sender, EventArgs e)
        {
            EnableToolStrip(false);

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_SAVE_RECORD);
        }

#if NO
        // ��ʽУ�������
        // return:
        //      -2  ������û������У�鷽�����޷�У��
        //      -1  error
        //      0   ���ǺϷ��������
        //      1   �ǺϷ��Ķ���֤�����
        //      2   �ǺϷ��Ĳ������
        int VerifyBarcode(
            string strBarcode,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����У������ ...");
            stop.BeginLoop();

            /*
            this.Update();
            this.MainForm.Update();
             * */

            try
            {
                long lRet = Channel.VerifyBarcode(
                    stop,
                    strBarcode,
                    out strError);
                if (lRet == -1)
                {
                    if (Channel.ErrorCode == DigitalPlatform.CirculationClient.localhost.ErrorCode.NotFound)
                        return -2;
                    goto ERROR1;
                }
                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }
        ERROR1:
            return -1;
        }
#endif

        // 
        /// <summary>
        /// �Ƿ�У������������
        /// </summary>
        public bool NeedVerifyBarcode
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "reader_info_form",
                    "verify_barcode",
                    false);
            }
        }

        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        /// <summary>
        /// �����¼
        /// </summary>
        /// <param name="strStyle">���Ϊ displaysuccess/verifybarcode ֮һ�������</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public int SaveRecord(string strStyle = "displaysuccess,verifybarcode")
        {
            string strError = "";
            int nRet = 0;

            if (this.readerEditControl1.Barcode == "")
            {
                strError = "��δ����֤�����";
                goto ERROR1;
            }

            // У��֤�����
            if (this.NeedVerifyBarcode == true
                && StringUtil.IsIdcardNumber(this.readerEditControl1.Barcode) == false)
            {
                // ��ʽУ�������
                // return:
                //      -2  ������û������У�鷽�����޷�У��
                //      -1  error
                //      0   ���ǺϷ��������
                //      1   �ǺϷ��Ķ���֤�����
                //      2   �ǺϷ��Ĳ������
                nRet = VerifyBarcode(
                    this.Channel.LibraryCodeList,
                    this.readerEditControl1.Barcode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ����������ʽ���Ϸ�
                if (nRet == 0)
                {
                    strError = "�������֤���� " + this.readerEditControl1.Barcode + " ��ʽ����ȷ("+strError+")��";
                    goto ERROR1;
                }

                // ʵ��������ǲ������
                if (nRet == 2)
                {
                    strError = "������������ " + this.readerEditControl1.Barcode + " �ǲ�����š����������֤����š�";
                    goto ERROR1;
                }

                /*
                // ���ڷ�����û������У�鹦�ܣ�����ǰ�˷�����У��Ҫ������������һ��
                if (nRet == -2)
                    MessageBox.Show(this, "���棺ǰ�˿�����У�����빦�ܣ����Ƿ�������ȱ����Ӧ�Ľű��������޷�У�����롣\r\n\r\n��Ҫ������ִ˾���Ի�����ر�ǰ��У�鹦��");
                 * */
            }

            // TODO: ����ʱ���ѡ��


            // �� this.readerEditControl1.RecPath Ϊ�յ�ʱ����Ҫ���ֶԻ������û�����ѡ��Ŀ���
            string strTargetRecPath = this.readerEditControl1.RecPath;
            if (string.IsNullOrEmpty(this.readerEditControl1.RecPath) == true)
            {
                // ���ֶԻ������û�����ѡ��Ŀ���
                ReaderSaveToDialog saveto_dlg = new ReaderSaveToDialog();
                MainForm.SetControlFont(saveto_dlg, this.Font, false);
                saveto_dlg.MessageText = "��ѡ���¼λ��";
                saveto_dlg.MainForm = this.MainForm;
                saveto_dlg.RecPath = this.readerEditControl1.RecPath;
                saveto_dlg.RecID = "?";

                this.MainForm.AppInfo.LinkFormState(saveto_dlg, "readerinfoform_savetodialog_state");
                saveto_dlg.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(saveto_dlg);

                if (saveto_dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return 0;

                strTargetRecPath = saveto_dlg.RecPath;
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ�����߼�¼ " + this.readerEditControl1.Barcode + " ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                string strNewXml = "";
                nRet = GetReaderXml(
            true,
            false,
            out strNewXml,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                ErrorCodeValue kernel_errorcode;

                byte[] baNewTimestamp = null;
                string strExistingXml = "";
                string strSavedXml = "";
                string strSavedPath = "";

                // ����
                // MessageBox.Show(this, "1 this.m_strSetAction='"+this.m_strSetAction+"'");

                long lRet = Channel.SetReaderInfo(
                    stop,
                    this.m_strSetAction,
                    strTargetRecPath,
                    strNewXml,
                    // 2007/11/5 changed
                    this.m_strSetAction != "new" ? this.readerEditControl1.OldRecord : null,
                    this.m_strSetAction != "new" ? this.readerEditControl1.Timestamp : null,

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
                            this.readerEditControl1.RecPath,
                            strExistingXml,
                            baNewTimestamp,
                            strNewXml,
                            this.readerEditControl1.Timestamp,
                            "���ݿ��еļ�¼�ڱ༭�ڼ䷢���˸ı䡣����ϸ�˶ԣ��������޸Ĵ����е�δ�����¼����ȷ����ť������Ա��档");

                        dlg.StartPosition = FormStartPosition.CenterScreen;
                        dlg.ShowDialog(this);
                        if (dlg.DialogResult == DialogResult.OK)
                        {
                            nRet = this.readerEditControl1.SetData(dlg.UnsavedXml,
                                dlg.RecPath,
                                dlg.UnsavedTimestamp,
                                out strError);
                            if (nRet == -1)
                            {
                                MessageBox.Show(this, strError);
                            }
                            MessageBox.Show(this, "��ע�����±����¼");
                            return -1;
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
                    this.binaryResControl1.BiblioRecPath = strSavedPath;
                    // �ύ���󱣴�����
                    // return:
                    //		-1	error
                    //		>=0 ʵ�����ص���Դ������
                    nRet = this.binaryResControl1.Save(out strError);
                    if (nRet == -1)
                    {
                        MessageBox.Show(this, strError);
                    }
                    if (nRet >= 1)
                    {
                        // ���»��ʱ���
                        string[] results = null;
                        string strOutputPath = "";
                        lRet = Channel.GetReaderInfo(
                            stop,
                            "@path:" + strSavedPath,
                            "", // "xml,html",
                            out results,
                            out strOutputPath,
                            out baNewTimestamp,
                            out strError);
                        if (lRet == -1 || lRet == 0)
                        {
                            MessageBox.Show(this, strError);
                        }
                    }

                    // ����װ�ؼ�¼���༭��
                    nRet = this.readerEditControl1.SetData(strSavedXml,
                        strSavedPath,
                        baNewTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // ˢ��XML��ʾ
                    /*
                    this.SetXmlToWebbrowser(this.webBrowser_xml,
                        strSavedXml);
                     * */
                    Global.SetXmlToWebbrowser(this.webBrowser_xml,
this.MainForm.DataDir,
"xml",
strSavedXml);
                    // 2007/11/12 new add
                    this.m_strSetAction = "change";

                    // װ�ؼ�¼��HTML
                    {
                        byte[] baTimestamp = null;
                        string strOutputRecPath = "";

                        string strBarcode = this.readerEditControl1.Barcode;

                        stop.SetMessage("����װ����߼�¼ " + strBarcode + " ...");

                        string[] results = null;
                        lRet = Channel.GetReaderInfo(
                            stop,
                            strBarcode,
                            "html",
                            out results,
                            out strOutputRecPath,
                            out baTimestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "�����¼�Ѿ��ɹ�������ˢ��HTML��ʾ��ʱ��������: " + strError;
                            // Global.SetHtmlString(this.webBrowser_readerInfo, strError);
                            this.m_webExternalHost.SetTextString(strError);
                            goto ERROR1;
                        }

                        if (lRet == 0)
                        {
                            strError = "�����¼�Ѿ��ɹ�������ˢ��HTML��ʾ��ʱ��������: " + strError;
                            // Global.SetHtmlString(this.webBrowser_readerInfo, strError);
                            this.m_webExternalHost.SetTextString(strError);
                            goto ERROR1;
                        }

                        if (lRet > 1)
                        {
                            strError = "���� " + strBarcode + " ���м�¼ " + lRet.ToString() + " ����ע������һ�����ش�����ϵͳ����Ա�����ų���";
                            strError = "�����¼�Ѿ��ɹ�������ˢ��HTML��ʾ��ʱ��������: " + strError;
                            // Global.SetHtmlString(this.webBrowser_readerInfo, strError);
                            this.m_webExternalHost.SetTextString(strError);
                            goto ERROR1;    // ��������
                        }

                        string strHtml = results[0];

#if NO
                        Global.SetHtmlString(this.webBrowser_readerInfo,
                            strHtml,
        this.MainForm.DataDir,
        "readerinfoform_reader");
#endif
                        this.SetReaderHtmlString(strHtml);
                    }

                }

                // ����ָ�Ƹ��ٻ���
                if (string.IsNullOrEmpty(this.MainForm.FingerprintReaderUrl) == false
                    && string.IsNullOrEmpty(this.readerEditControl1.Barcode) == false)
                {
                    // return:
                    //      -2  remoting����������ʧ�ܡ�����������δ����
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = UpdateFingerprintCache(
                         this.readerEditControl1.Barcode,
                         this.readerEditControl1.Fingerprint,
                         out strError);
                    if (nRet == -1)
                    {
                        strError = "��Ȼ���߼�¼�Ѿ�����ɹ���������ָ�ƻ���ʱ�����˴���: " + strError;
                        goto ERROR1;
                    }
                    // -2 ���ⲻ������Ϊ�û�����������URL�����ǵ�ǰ��������û������
                }

            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            if (StringUtil.IsInList("displaysuccess", strStyle) == true)
                this.MainForm.StatusBarMessage = "���߼�¼����ɹ�";
            // MessageBox.Show(this, "����ɹ�");
            return 1;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // ���
        private void toolStripButton_saveTo_Click(object sender, EventArgs e)
        {
            EnableToolStrip(false);

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_SAVETO);
        }

        // �����Ŀ��¼��XML��ʽ
        // parameters:
        //      bIncludeFileID  �Ƿ�Ҫ���ݵ�ǰrescontrol���ݺϳ�<dprms:file>Ԫ��?
        //      bClearFileID    �Ƿ�Ҫ�����ǰ��<dprms:file>Ԫ��
        int GetReaderXml(
            bool bIncludeFileID,
            bool bClearFileID,
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            string strNewXml = "";
            int nRet = this.readerEditControl1.GetData(
                out strNewXml,
                out strError);
            if (nRet == -1)
                return -1;


            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strNewXml);
            }
            catch (Exception ex)
            {
                strError = "XML����װ��DOMʱ����: " + ex.Message;
                return -1;
            }


            Debug.Assert(dom != null, "");

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);

            if (bClearFileID == true
                || (this.binaryResControl1 != null && bIncludeFileID == true)
                )
            {
                // 2011/10/13
                // �����ǰ��<dprms:file>Ԫ��
                XmlNodeList nodes = dom.DocumentElement.SelectNodes("//dprms:file", nsmgr);
                foreach (XmlNode node in nodes)
                {
                    node.ParentNode.RemoveChild(node);
                }
            }


            // �ϳ�<dprms:file>Ԫ��
            if (this.binaryResControl1 != null
                && bIncludeFileID == true)  // 2008/12/3 new add
            {
                List<string> ids = this.binaryResControl1.GetIds();
                List<string> usages = this.binaryResControl1.GetUsages();

                Debug.Assert(ids.Count == usages.Count, "");

                for (int i = 0; i < ids.Count; i++)
                {
                    string strID = ids[i];
                    if (String.IsNullOrEmpty(strID) == true)
                        continue;

                    string strUsage = usages[i];

                    XmlNode node = null;

                    node = dom.DocumentElement.SelectSingleNode("//dprms:file[@id='" + strID + "']", nsmgr);
                    if (node == null)
                    {
                        node = dom.CreateElement("dprms",
                             "file",
                             DpNs.dprms);
                        dom.DocumentElement.AppendChild(node);
                    }
                    else
                    {
                        DomUtil.SetAttr(node, "usage", null);
                    }

                    DomUtil.SetAttr(node, "id", strID);
                    if (string.IsNullOrEmpty(strUsage) == false)
                        DomUtil.SetAttr(node, "usage", strUsage);

                }
            }

            strXml = dom.OuterXml;
            return 0;
        }

        // ���һЩ�����ֶε�����
        static int ClearReserveFields(
            ref string strNewXml,
            out string strError)
        {
            strError = "";

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strNewXml);
            }
            catch (Exception ex)
            {
                strError = "װ��XML��DOM����: " + ex.Message;
                return -1;
            }
            DomUtil.DeleteElement(dom.DocumentElement,
                "password");
            DomUtil.DeleteElement(dom.DocumentElement,
                "displayName");
            // 2014/11/14
            DomUtil.DeleteElement(dom.DocumentElement,
                "fingerprint");
            DomUtil.DeleteElement(dom.DocumentElement,
                "hire");
            DomUtil.DeleteElement(dom.DocumentElement,
                "foregift");
            DomUtil.DeleteElement(dom.DocumentElement,
                "personalLibrary");
            DomUtil.DeleteElement(dom.DocumentElement,
                "friends");
                
#if NO
            // ���<dprms:file>Ԫ��
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//dprms:file", nsmgr);
            foreach (XmlNode node in nodes)
            {
                node.ParentNode.RemoveChild(node);
            }
#endif

            strNewXml = dom.OuterXml;
            return 0;
        }

        void SaveTo()
        {
            string strError = "";
            int nRet = 0;
            bool bReserveFieldsCleared = false;

            if (this.readerEditControl1.Barcode == "")
            {
                strError = "��δ����֤�����";
                goto ERROR1;
            }

            // У��֤�����
            if (this.NeedVerifyBarcode == true
                && StringUtil.IsIdcardNumber(this.readerEditControl1.Barcode) == false)
            {
                // ��ʽУ�������
                // return:
                //      -2  ������û������У�鷽�����޷�У��
                //      -1  error
                //      0   ���ǺϷ��������
                //      1   �ǺϷ��Ķ���֤�����
                //      2   �ǺϷ��Ĳ������
                nRet = VerifyBarcode(
                    this.Channel.LibraryCodeList,
                    this.readerEditControl1.Barcode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ����������ʽ���Ϸ�
                if (nRet == 0)
                {
                    strError = "�������֤����� " + this.readerEditControl1.Barcode + " ��ʽ����ȷ(" + strError + ")��";
                    goto ERROR1;
                }

                // ʵ��������ǲ������
                if (nRet == 2)
                {
                    strError = "������������ " + this.readerEditControl1.Barcode + " �ǲ�����š����������֤����š�";
                    goto ERROR1;
                }

                /*
                // ���ڷ�����û������У�鹦�ܣ�����ǰ�˷�����У��Ҫ������������һ��
                if (nRet == -2)
                    MessageBox.Show(this, "���棺ǰ�˿�����У�����빦�ܣ����Ƿ�������ȱ����Ӧ�Ľű��������޷�У�����롣\r\n\r\n��Ҫ������ִ˾���Ի�����ر�ǰ��У�鹦��");
                 * */
            }

            // ���ֶԻ������û�����ѡ��Ŀ���
            ReaderSaveToDialog saveto_dlg = new ReaderSaveToDialog();
            MainForm.SetControlFont(saveto_dlg, this.Font, false);
            saveto_dlg.Text = "����һ�����߼�¼";
            saveto_dlg.MessageText = "��ѡ��Ҫ�����Ŀ���¼λ��\r\n(��¼IDΪ ? ��ʾ׷�ӱ��浽���ݿ�ĩβ)";
            saveto_dlg.MainForm = this.MainForm;
            saveto_dlg.RecPath = this.readerEditControl1.RecPath;
            saveto_dlg.RecID = "?";

            this.MainForm.AppInfo.LinkFormState(saveto_dlg, "readerinfoform_savetodialog_state");
            saveto_dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(saveto_dlg);

            if (saveto_dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            if (saveto_dlg.RecID == "?")
                this.m_strSetAction = "new";
            else
                this.m_strSetAction = "change";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ�����߼�¼ " + this.readerEditControl1.Barcode + " ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                string strNewXml = "";

                if (this.m_strSetAction == "new")
                    nRet = GetReaderXml(
                        false,  // ������<dprms:file>Ԫ��
                        true,   // ���<dprms:file>Ԫ��
                        out strNewXml,
                        out strError);
                else
                    nRet = GetReaderXml(
                        true,  // ����<dprms:file>Ԫ��
                        false,
                        out strNewXml,
                        out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ��Ҫ����password/displayNameԪ������
                if (this.m_strSetAction == "new")
                {
                            // ���һЩ�����ֶε�����
                    nRet = ClearReserveFields(
            ref strNewXml,
            out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    bReserveFieldsCleared = true;
                }

                ErrorCodeValue kernel_errorcode;

                byte[] baNewTimestamp = null;
                string strExistingXml = "";
                string strSavedXml = "";
                string strSavedPath = "";

                // ����
                // MessageBox.Show(this, "2 this.m_strSetAction='" + this.m_strSetAction + "'");

                long lRet = Channel.SetReaderInfo(
                    stop,
                    this.m_strSetAction,
                    saveto_dlg.RecPath, // this.readerEditControl1.RecPath,
                    strNewXml,
                    this.m_strSetAction != "new" ? this.readerEditControl1.OldRecord : null,
                    this.m_strSetAction != "new" ? this.readerEditControl1.Timestamp : null,
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
                            this.readerEditControl1.RecPath,
                            strExistingXml,
                            baNewTimestamp,
                            strNewXml,
                            this.readerEditControl1.Timestamp,
                            "���ݿ��еļ�¼�ڱ༭�ڼ䷢���˸ı䡣����ϸ�˶ԣ��������޸Ĵ����е�δ�����¼����ȷ����ť������Ա��档");

                        dlg.StartPosition = FormStartPosition.CenterScreen;
                        dlg.ShowDialog(this);
                        if (dlg.DialogResult == DialogResult.OK)
                        {
                            nRet = this.readerEditControl1.SetData(dlg.UnsavedXml,
                                dlg.RecPath,
                                dlg.UnsavedTimestamp,
                                out strError);
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
                    this.binaryResControl1.BiblioRecPath = strSavedPath;
                    // �ύ���󱣴�����
                    // return:
                    //		-1	error
                    //		>=0 ʵ�����ص���Դ������
                    nRet = this.binaryResControl1.Save(out strError);
                    if (nRet == -1)
                    {
                        MessageBox.Show(this, strError);
                    }
                    if (nRet >= 1)
                    {
                        // ���»��ʱ���
                        string[] results = null;
                        string strOutputPath = "";
                        lRet = Channel.GetReaderInfo(
                            stop,
                            "@path:" + strSavedPath,
                            "", // "xml,html",
                            out results,
                            out strOutputPath,
                            out baNewTimestamp,
                            out strError);
                        if (lRet == -1 || lRet == 0)
                        {
                            MessageBox.Show(this, strError);
                        }
                    }

                    // ����װ�ؼ�¼���༭��
                    nRet = this.readerEditControl1.SetData(strSavedXml,
                        strSavedPath,
                        baNewTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    /*
                    this.SetXmlToWebbrowser(this.webBrowser_xml,
                        strSavedXml);
                     * */
                    Global.SetXmlToWebbrowser(this.webBrowser_xml,
this.MainForm.DataDir,
"xml",
strSavedXml);
                    // 2007/11/12 new add
                    this.m_strSetAction = "change";

                    // ����װ�������Դ
                    {
                        nRet = this.binaryResControl1.LoadObject(strSavedPath,    // 2008/11/2 changed
                            strSavedXml,
                            out strError);
                        if (nRet == -1)
                        {
                            MessageBox.Show(this, strError);
                            // return -1;
                        }
                    }

                    // 2011/11/23
                    // װ�ؼ�¼��HTML
                    {
                        byte[] baTimestamp = null;
                        string strOutputRecPath = "";

                        string strBarcode = this.readerEditControl1.Barcode;

                        stop.SetMessage("����װ����߼�¼ " + strBarcode + " ...");

                        string[] results = null;
                        lRet = Channel.GetReaderInfo(
                            stop,
                            strBarcode,
                            "html",
                            out results,
                            out strOutputRecPath,
                            out baTimestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "�����¼�Ѿ��ɹ�������ˢ��HTML��ʾ��ʱ��������: " + strError;
                            // Global.SetHtmlString(this.webBrowser_readerInfo, strError);
                            this.m_webExternalHost.SetTextString(strError);
                            goto ERROR1;
                        }

                        if (lRet == 0)
                        {
                            strError = "�����¼�Ѿ��ɹ�������ˢ��HTML��ʾ��ʱ��������: " + strError;
                            Global.SetHtmlString(this.webBrowser_readerInfo, strError);
                            goto ERROR1;
                        }

                        if (lRet > 1)
                        {
                            strError = "���� " + strBarcode + " ���м�¼ " + lRet.ToString() + " ����ע������һ�����ش�����ϵͳ����Ա�����ų���";
                            strError = "�����¼�Ѿ��ɹ�������ˢ��HTML��ʾ��ʱ��������: " + strError;
                            // Global.SetHtmlString(this.webBrowser_readerInfo, strError);
                            this.m_webExternalHost.SetTextString(strError);
                            goto ERROR1;    // ��������
                        }

                        string strHtml = results[0];

#if NO
                        Global.SetHtmlString(this.webBrowser_readerInfo,
                            strHtml,
        this.MainForm.DataDir,
        "readerinfoform_reader");
#endif
                        this.SetReaderHtmlString(strHtml);
                    }

                }
            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            if (bReserveFieldsCleared == true)
                MessageBox.Show(this, "���ɹ����¼�¼������Ϊ��ʼ״̬����ʾ����δ���á�");
            else
                MessageBox.Show(this, "���ɹ���");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        // ɾ����¼
        private void toolStripButton_delete_Click(object sender, EventArgs e)
        {
            EnableToolStrip(false);

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_DELETE_RECORD);

        }

        // ɾ����¼
        void DeleteRecord()
        {
            string strError = "";

            if (this.readerEditControl1.Barcode == "")
            {
                strError = "��δ����֤����ţ��޷�ɾ��";
                goto ERROR1;
            }

            // bool bForceDelete = false;
            string strRecPath = null;
            string strText = "ȷʵҪɾ��֤�����Ϊ '" + this.readerEditControl1.Barcode + "' �Ķ��߼�¼ ? ";

            // ���ͬʱ����control������ʾǿ�ư��ռ�¼·��ɾ��
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                // bForceDelete = true;
                strRecPath = this.readerEditControl1.RecPath;
                strText = "ȷʵҪɾ��֤�����Ϊ '" + this.readerEditControl1.Barcode + "' ���Ҽ�¼·��Ϊ '" + strRecPath + "' �Ķ��߼�¼ ? ";
            }

            DialogResult result = MessageBox.Show(this,
                strText,
                "ReaderInfoForm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ɾ�����߼�¼ " + this.readerEditControl1.Barcode + " ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {

                string strNewXml = "";
                int nRet = this.readerEditControl1.GetData(
                    out strNewXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                string strOldBarcode = this.readerEditControl1.Barcode;

                ErrorCodeValue kernel_errorcode;

                byte[] baNewTimestamp = null;
                string strExistingXml = "";
                string strSavedXml = "";
                string strSavedPath = "";

                long lRet = Channel.SetReaderInfo(
                    stop,
                    "delete",
                    strRecPath,   // this.readerEditControl1.RecPath,
                    "", // strNewXml,
                    this.readerEditControl1.OldRecord,
                    this.readerEditControl1.Timestamp,
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
                            this.readerEditControl1.RecPath,
                            strExistingXml,
                            baNewTimestamp,
                            strNewXml,
                            this.readerEditControl1.Timestamp,
                            "���ݿ��еļ�¼�ڱ༭�ڼ䷢���˸ı䡣����ϸ�˶ԣ����������ɾ��������ȷ������ť�������ɾ�����������ɾ���ˣ��밴��ȡ������ť");

                        dlg.StartPosition = FormStartPosition.CenterScreen;
                        dlg.ShowDialog(this);
                        if (dlg.DialogResult == DialogResult.OK)
                        {
                            nRet = this.readerEditControl1.SetData(dlg.UnsavedXml,
                                dlg.RecPath,
                                dlg.UnsavedTimestamp,
                                out strError);
                            if (nRet == -1)
                            {
                                MessageBox.Show(this, strError);
                            }
                            MessageBox.Show(this, "��ע����߼�¼��ʱ***��δ***ɾ����\r\n\r\n��Ҫɾ����¼���밴��ɾ������ť�����ύɾ������");
                            return;
                        }
                    }

                    goto ERROR1;
                }

                // ����ɾ�����Ĵ��ڣ�һ����Ҫ�����������±����ȥ
                this.m_strSetAction = "new";

                nRet = this.readerEditControl1.SetData(strExistingXml,
                    null,
                    null,
                    out strError);
                if (nRet == -1)
                {
                    strError = "ɾ���������SetData()����ʧ��: " + strError;
                    MessageBox.Show(this, strError);
                }

                this.readerEditControl1.Changed = false;

                // ����ָ�Ƹ��ٻ���
                if (string.IsNullOrEmpty(this.MainForm.FingerprintReaderUrl) == false
                    && string.IsNullOrEmpty(this.readerEditControl1.Barcode) == false)
                {
                    // return:
                    //      -2  remoting����������ʧ�ܡ�����������δ����
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = UpdateFingerprintCache(
                         strOldBarcode,
                         "",
                         out strError);
                    if (nRet == -1)
                    {
                        strError = "��Ȼ���߼�¼�Ѿ�ɾ���ɹ���������ָ�ƻ���ʱ�����˴���: " + strError;
                        goto ERROR1;
                    }
                    // -2 ���ⲻ������Ϊ�û�����������URL�����ǵ�ǰ��������û������
                }
            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            MessageBox.Show(this, "ɾ���ɹ���\r\n\r\n���ᷢ�ֱ༭�����л����Ŷ��߼�¼���ݣ����벻Ҫ���ģ����ݿ���Ķ��߼�¼�Ѿ���ɾ���ˡ�\r\n\r\n�������ʱ����ˣ������԰������水ť���Ѷ��߼�¼ԭ�������ȥ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;

        }

#if NO
        #region delete

        // ɾ��
        private void button_delete_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.readerEditControl1.Barcode == "")
            {
                strError = "��δ����֤����ţ��޷�ɾ��";
                goto ERROR1;
            }

            DialogResult result = MessageBox.Show(this,
"ȷʵҪɾ��֤�����Ϊ '" + this.readerEditControl1.Barcode + "' �Ķ��߼�¼ ? ",
"ReaderInfoForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ɾ�����߼�¼ " + this.readerEditControl1.Barcode + " ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {

                string strNewXml = "";
                int nRet = this.readerEditControl1.GetData(
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
                    "delete",
                    null,   // this.readerEditControl1.RecPath,
                    "", // strNewXml,
                    this.readerEditControl1.OldRecord,
                    this.readerEditControl1.Timestamp,
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
                            this.readerEditControl1.RecPath,
                            strExistingXml,
                            baNewTimestamp,
                            strNewXml,
                            this.readerEditControl1.Timestamp,
                            "���ݿ��еļ�¼�ڱ༭�ڼ䷢���˸ı䡣����ϸ�˶ԣ����������ɾ��������ȷ������ť�������ɾ�����������ɾ���ˣ��밴��ȡ������ť");

                        dlg.StartPosition = FormStartPosition.CenterScreen;
                        dlg.ShowDialog(this);
                        if (dlg.DialogResult == DialogResult.OK)
                        {
                            nRet = this.readerEditControl1.SetData(dlg.UnsavedXml,
                                dlg.RecPath,
                                dlg.UnsavedTimestamp,
                                out strError);
                            if (nRet == -1)
                            {
                                MessageBox.Show(this, strError);
                            }
                            MessageBox.Show(this, "��ע����߼�¼��ʱ***��δ***ɾ����\r\n\r\n��Ҫɾ����¼���밴��ɾ������ť�����ύɾ������");
                            return;
                        }
                    }

                    goto ERROR1;
                }

                // ����ɾ�����Ĵ��ڣ�һ����Ҫ�����������±����ȥ
                this.m_strSetAction = "new";

                nRet = this.readerEditControl1.SetData(strExistingXml,
                    null,
                    null,
                    out strError);
                if (nRet == -1)
                {
                    strError = "ɾ���������SetDate()����ʧ��: " + strError;
                    MessageBox.Show(this, strError);
                }

                this.readerEditControl1.Changed = false;

            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            MessageBox.Show(this, "ɾ���ɹ���\r\n\r\n���ᷢ�ֱ༭�����л����Ŷ��߼�¼���ݣ����벻Ҫ���ģ����ݿ���Ķ��߼�¼�Ѿ���ɾ���ˡ�\r\n\r\n�������ʱ����ˣ������԰������水ť���Ѷ��߼�¼ԭ�������ȥ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }


        // ���
        private void button_saveTo_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.readerEditControl1.Barcode == "")
            {
                strError = "��δ����֤�����";
                goto ERROR1;
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ�����߼�¼ " + this.readerEditControl1.Barcode + " ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                string strNewXml = "";
                int nRet = this.readerEditControl1.GetData(
                    out strNewXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ��Ҫ����passwordԪ�����ݡ�
                {
                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strNewXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "װ��XML��DOM����: " + ex.Message;
                        goto ERROR1;
                    }
                    DomUtil.SetElementText(dom.DocumentElement,
                        "password", "");
                    strNewXml = dom.OuterXml;
                }

                ErrorCodeValue kernel_errorcode;

                byte[] baNewTimestamp = null;
                string strExistingXml = "";
                string strSavedXml = "";
                string strSavedPath = "";

                long lRet = Channel.SetReaderInfo(
                    stop,
                    "new",  // this.m_strSetAction,
                    "", // this.readerEditControl1.RecPath,
                    strNewXml,
                    "", // this.readerEditControl1.OldRecord,
                    null,   // this.readerEditControl1.Timestamp,
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
                            this.readerEditControl1.RecPath,
                            strExistingXml,
                            baNewTimestamp,
                            strNewXml,
                            this.readerEditControl1.Timestamp,
                            "���ݿ��еļ�¼�ڱ༭�ڼ䷢���˸ı䡣����ϸ�˶ԣ��������޸Ĵ����е�δ�����¼����ȷ����ť������Ա��档");

                        dlg.StartPosition = FormStartPosition.CenterScreen;
                        dlg.ShowDialog(this);
                        if (dlg.DialogResult == DialogResult.OK)
                        {
                            nRet = this.readerEditControl1.SetData(dlg.UnsavedXml,
                                dlg.RecPath,
                                dlg.UnsavedTimestamp,
                                out strError);
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

                /*
                this.Timestamp = baNewTimestamp;
                this.OldRecord = strSavedXml;
                this.RecPath = strSavedPath;
                 */

                if (lRet == 1)
                {
                    // �����ֶα��ܾ�
                    MessageBox.Show(this, strError);

                    if (Channel.ErrorCode == ErrorCode.ChangePartDenied)
                    {
                        // ��������װ��?
                        MessageBox.Show(this, "������װ�ؼ�¼, �����Щ�ֶ������޸ı��ܾ���");
                    }
                }
                else
                {
                    // ����װ�ؼ�¼���༭��
                    nRet = this.readerEditControl1.SetData(strSavedXml,
                        strSavedPath,
                        baNewTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    // 
                    this.SetXmlToWebbrowser(this.webBrowser_xml,
                        strSavedXml);

                    // 2007/11/12 new add
                    this.m_strSetAction = "change";
                }

            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            MessageBox.Show(this, "���ɹ����¼�¼��������δ���á�");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        // ����
        private void button_save_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.readerEditControl1.Barcode == "")
            {
                strError = "��δ����֤�����";
                goto ERROR1;
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ�����߼�¼ " + this.readerEditControl1.Barcode + " ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                string strNewXml = "";
                int nRet = this.readerEditControl1.GetData(
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
                    this.m_strSetAction,
                    this.readerEditControl1.RecPath,
                    strNewXml,
                    this.m_strSetAction != "new" ? this.readerEditControl1.OldRecord : null,
                    this.m_strSetAction != "new" ? this.readerEditControl1.Timestamp : null,
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
                            this.readerEditControl1.RecPath,
                            strExistingXml,
                            baNewTimestamp,
                            strNewXml,
                            this.readerEditControl1.Timestamp,
                            "���ݿ��еļ�¼�ڱ༭�ڼ䷢���˸ı䡣����ϸ�˶ԣ��������޸Ĵ����е�δ�����¼����ȷ����ť������Ա��档");

                        dlg.StartPosition = FormStartPosition.CenterScreen;
                        dlg.ShowDialog(this);
                        if (dlg.DialogResult == DialogResult.OK)
                        {
                            nRet = this.readerEditControl1.SetData(dlg.UnsavedXml,
                                dlg.RecPath,
                                dlg.UnsavedTimestamp,
                                out strError);
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

                /*
                this.Timestamp = baNewTimestamp;
                this.OldRecord = strSavedXml;
                this.RecPath = strSavedPath;
                 */

                if (lRet == 1)
                {
                    // �����ֶα��ܾ�
                    MessageBox.Show(this, strError);

                    if (Channel.ErrorCode == ErrorCode.ChangePartDenied)
                    {
                        // ��������װ��?
                        MessageBox.Show(this, "������װ�ؼ�¼, �����Щ�ֶ������޸ı��ܾ���");
                    }
                }
                else
                {
                    // ����װ�ؼ�¼���༭��
                    nRet = this.readerEditControl1.SetData(strSavedXml,
                        strSavedPath,
                        baNewTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    // 
                    this.SetXmlToWebbrowser(this.webBrowser_xml,
                        strSavedXml);

                    // 2007/11/12 new add
                    this.m_strSetAction = "change";

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
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        // װ��һ���հ׼�¼
        private void button_loadBlank_Click(object sender, EventArgs e)
        {
            if (this.readerEditControl1.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ����Ϣ���޸ĺ���δ���档����ʱ�������¶�����Ϣ������δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�����¶�����Ϣ? ",
    "ReaderInfoForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                    return;
            }

            this.EnableControls(false);

            try
            {
                string strError = "";

                string strNewDefault = this.MainForm.AppInfo.GetString(
        "readerinfoform_optiondlg",
        "newreader_default",
        "<root />");
                int nRet = this.readerEditControl1.SetData(strNewDefault,
                     "",
                     null,
                     out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);

                Global.ClearHtmlPage(this.webBrowser_readerInfo,
                    this.MainForm.DataDir);
                // 
                this.SetXmlToWebbrowser(this.webBrowser_xml,
                    strNewDefault);


                this.m_strSetAction = "new";
            }
            finally
            {
                this.EnableControls(true);
            }
        }

        // ѡ��
        private void button_option_Click(object sender, EventArgs e)
        {
            ReaderInfoFormOptionDlg dlg = new ReaderInfoFormOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.MainForm = this.MainForm;
            dlg.ShowDialog(this);
        }

        #endregion

#endif

        // ѡ��
        private void toolStripButton_option_Click(object sender, EventArgs e)
        {
            ReaderInfoFormOptionDlg dlg = new ReaderInfoFormOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.MainForm = this.MainForm;
            dlg.ShowDialog(this);
        }


        void Hire()
        {
            if (this.ReaderXmlChanged == true
                || this.ObjectChanged == true)
            {
                MessageBox.Show(this, "��ǰ����Ϣ���޸ĺ���δ���档�����ȱ���󣬲��ܽ��д������Ĳ�����");
                return;
            }

            string strError = "";
            int nRet = 0;

            if (this.readerEditControl1.Barcode == "")
            {
                strError = "��δ����֤�����";
                goto ERROR1;
            }

            // У��֤�����
            if (this.NeedVerifyBarcode == true
                && StringUtil.IsIdcardNumber(this.readerEditControl1.Barcode) == false)
            {
                // ��ʽУ�������
                // return:
                //      -2  ������û������У�鷽�����޷�У��
                //      -1  error
                //      0   ���ǺϷ��������
                //      1   �ǺϷ��Ķ���֤�����
                //      2   �ǺϷ��Ĳ������
                nRet = VerifyBarcode(
                    this.Channel.LibraryCodeList,
                    this.readerEditControl1.Barcode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ����������ʽ���Ϸ�
                if (nRet == 0)
                {
                    strError = "�������֤����� " + this.readerEditControl1.Barcode + " ��ʽ����ȷ(" + strError + ")��";
                    goto ERROR1;
                }

                // ʵ��������ǲ������
                if (nRet == 2)
                {
                    strError = "������������ " + this.readerEditControl1.Barcode + " �ǲ�����š����������֤����š�";
                    goto ERROR1;
                }

                /*
                // ���ڷ�����û������У�鹦�ܣ�����ǰ�˷�����У��Ҫ������������һ��
                if (nRet == -2)
                    MessageBox.Show(this, "���棺ǰ�˿�����У�����빦�ܣ����Ƿ�������ȱ����Ӧ�Ľű��������޷�У�����롣\r\n\r\n��Ҫ������ִ˾���Ի�����ر�ǰ��У�鹦��");
                 * */
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڴ������߼�¼ " + this.readerEditControl1.Barcode + " �� ��𽻷����� ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                string strReaderBarcode = this.readerEditControl1.Barcode;
                string strAction = "hire";

                string strOutputrReaderXml = "";
                string strOutputID = "";

                long lRet = Channel.Hire(
                    stop,
                    strAction,
                    strReaderBarcode,
                    out strOutputrReaderXml,
                    out strOutputID,
                    out strError);
                if (lRet == -1)
                {
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

            // ����װ�ش�������
            LoadRecord(this.readerEditControl1.Barcode,
                false);

            MessageBox.Show(this, "������𽻷����� �ɹ�");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;

        }

        // ǰһ�����߼�¼
        private void toolStripButton_prev_Click(object sender, EventArgs e)
        {
            EnableToolStrip(false);

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_PREV_RECORD);
        }

        // ��һ�����߼�¼
        private void toolStripButton_next_Click(object sender, EventArgs e)
        {
            EnableToolStrip(false);

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_NEXT_RECORD);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_SET_FOCUS:
                    this.toolStripTextBox_barcode.Focus();
                    return;
                case WM_LOAD_RECORD:
                    EnableToolStrip(false);
                    try
                    {
                        if (this.m_webExternalHost.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            this.LoadRecord(
                                this.toolStripTextBox_barcode.Text,
                                // this.textBox_readerBarcode.Text,
                                false);
                        }
                    }
                    finally
                    {
                        EnableToolStrip(true);
                    }
                    return;
                case WM_DELETE_RECORD:
                    EnableToolStrip(false);
                    try
                    {
                        if (this.m_webExternalHost.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            this.DeleteRecord();
                        }
                    }
                    finally
                    {
                        EnableToolStrip(true);
                    }
                    return;
                case WM_NEXT_RECORD:
                    EnableToolStrip(false);
                    try
                    {
                        /*
                        Debug.Assert(this.m_webExternalHost.IsInLoop == false, "����ǰ������һ��ѭ����δֹͣ");

                        if (this.m_webExternalHost.ChannelInUse == true)
                        {
                            // ����֮��
                            this.m_webExternalHost.Stop();
                            // Thread.Sleep(100);
                            this.commander.AddMessage(WM_NEXT_RECORD);
                            return;
                        }


                        Debug.Assert(this.m_webExternalHost.ChannelInUse == false, "����ǰ����ͨ����δ�ͷ�");
                         * */

                        if (this.m_webExternalHost.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            LoadRecordByRecPath(this.readerEditControl1.RecPath, "next");
                        }
                    }
                    finally
                    {
                        EnableToolStrip(true);
                    } 
                    return;

                case WM_PREV_RECORD:
                    EnableToolStrip(false);
                    try
                    {
                        if (this.m_webExternalHost.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            LoadRecordByRecPath(this.readerEditControl1.RecPath, "prev");
                        }
                    }
                    finally
                    {
                        EnableToolStrip(true);
                    }
                    return;
                case WM_HIRE:
                    EnableToolStrip(false);
                    try
                    {
                        if (this.m_webExternalHost.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            this.Hire();
                        }
                    }
                    finally
                    {
                        EnableToolStrip(true);
                    }
                    return;
                case WM_FOREGIFT:
                    EnableToolStrip(false);
                    try
                    {
                        if (this.m_webExternalHost.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            this.Foregift("foregift");
                        }
                    }
                    finally
                    {
                        EnableToolStrip(true);
                    }
                    return;
                case WM_RETURN_FOREGIFT:
                    EnableToolStrip(false);
                    try
                    {
                        if (this.m_webExternalHost.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            this.Foregift("return");
                        }
                    }
                    finally
                    {
                        EnableToolStrip(true);
                    }
                    return;
                case WM_SAVETO:
                    EnableToolStrip(false);
                    try
                    {
                        if (this.m_webExternalHost.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            this.SaveTo();
                        }
                    }
                    finally
                    {
                        EnableToolStrip(true);
                    }
                    return;
                case WM_SAVE_RECORD:
                    EnableToolStrip(false);
                    try
                    {
                        if (this.m_webExternalHost.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            this.SaveRecord();
                        }
                    }
                    finally
                    {
                        EnableToolStrip(true);
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }

        private void toolStripButton_stopSummaryLoop_Click(object sender, EventArgs e)
        {
            // this.m_webExternalHost.IsInLoop = false;
            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

        }


        // parameters:
        //      strAction   Ϊforegift��return֮һ
        void Foregift(string strAction)
        {
            if (this.ReaderXmlChanged == true
                || this.ObjectChanged == true)
            {
                MessageBox.Show(this, "��ǰ����Ϣ���޸ĺ���δ���档�����ȱ���󣬲��ܽ��д���Ѻ��Ĳ�����");
                return;
            }

            string strError = "";
            int nRet = 0;


            if (this.readerEditControl1.Barcode == "")
            {
                strError = "��δ����֤�����";
                goto ERROR1;
            }

            // У��֤�����
            if (this.NeedVerifyBarcode == true
                && StringUtil.IsIdcardNumber(this.readerEditControl1.Barcode) == false)
            {
                // ��ʽУ�������
                // return:
                //      -2  ������û������У�鷽�����޷�У��
                //      -1  error
                //      0   ���ǺϷ��������
                //      1   �ǺϷ��Ķ���֤�����
                //      2   �ǺϷ��Ĳ������
                nRet = VerifyBarcode(
                    this.Channel.LibraryCodeList,
                    this.readerEditControl1.Barcode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ����������ʽ���Ϸ�
                if (nRet == 0)
                {
                    strError = "�������֤����� " + this.readerEditControl1.Barcode + " ��ʽ����ȷ(" + strError + ")��";
                    goto ERROR1;
                }

                // ʵ��������ǲ������
                if (nRet == 2)
                {
                    strError = "������������ " + this.readerEditControl1.Barcode + " �ǲ�����š����������֤����š�";
                    goto ERROR1;
                }

                /*
                // ���ڷ�����û������У�鹦�ܣ�����ǰ�˷�����У��Ҫ������������һ��
                if (nRet == -2)
                    MessageBox.Show(this, "���棺ǰ�˿�����У�����빦�ܣ����Ƿ�������ȱ����Ӧ�Ľű��������޷�У�����롣\r\n\r\n��Ҫ������ִ˾���Ի�����ر�ǰ��У�鹦��");
                 * */
            }

            string strActionName = "Ѻ�𽻷�";

            if (strAction == "return")
                strActionName = "Ѻ���˷�";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڴ������߼�¼ " + this.readerEditControl1.Barcode + " ��"+strActionName+"��¼ ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                string strReaderBarcode = this.readerEditControl1.Barcode;

                string strOutputrReaderXml = "";
                string strOutputID = "";

                Debug.Assert(strAction == "foregift" || strAction == "return", "");

                long lRet = Channel.Foregift(
                    stop,
                    strAction,
                    strReaderBarcode,
                    out strOutputrReaderXml,
                    out strOutputID,
                    out strError);
                if (lRet == -1)
                {
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

            // ����װ�ش�������
            LoadRecord(this.readerEditControl1.Barcode,
                false);

            MessageBox.Show(this, "����"+strActionName+"��¼�ɹ�");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;

        }

        // ������𽻷�����
        private void ToolStripMenuItem_hire_Click(object sender, EventArgs e)
        {
            EnableToolStrip(false);

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_HIRE);
        }

        /*
        // old
        private void toolStripButton_hire_Click(object sender, EventArgs e)
        {
        }*/

        // ����Ѻ�𽻷�����
        private void ToolStripMenuItem_foregift_Click(object sender, EventArgs e)
        {
            EnableToolStrip(false);

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_FOREGIFT);
        }

        /*
        // old
        private void toolStripButton_foregift_Click(object sender, EventArgs e)
        {
        }*/

        // ����Ѻ���˷�����
        private void ToolStripMenuItem_returnForegift_Click(object sender, EventArgs e)
        {
            EnableToolStrip(false);

            this.m_webExternalHost.StopPrevious();
            this.webBrowser_readerInfo.Stop();

            this.commander.AddMessage(WM_RETURN_FOREGIFT);
        }

        private void ReaderInfoForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;

        }

        private void ReaderInfoForm_DragDrop(object sender, DragEventArgs e)
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
                strError = "���ߴ�ֻ��������һ����¼";
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
                this.LoadRecordByRecPath(strRecPath,
                    "");
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

        private void toolStripButton_clearOutofReservationCount_Click(object sender, EventArgs e)
        {
            bool bRet = this.readerEditControl1.ClearOutofReservationCount();
            if (bRet == true)
            {
                MessageBox.Show(this, "��ǰ��¼�� ԤԼ����δȡ���� �Ѿ������Ϊ0��ע�Ᵽ�浱ǰ��¼��");
            }
        }

        private void toolStripButton_saveTemplate_Click(object sender, EventArgs e)
        {
            SaveReaderToTemplate();
        }

        private void toolStripButton_pasteCardPhoto_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            Image image = null;
            IDataObject obj1 = Clipboard.GetDataObject();
            if (obj1.GetDataPresent(typeof(Bitmap)))
            {
                image = (Image)obj1.GetData(typeof(Bitmap));
            }
            else 
            {
                strError = "��ǰWindows��������û��ͼ�ζ���";
                goto ERROR1;
            }

                string strShrinkComment = "";
            using (image)
            {

                // �Զ���Сͼ��

#if NO
            string strMaxWidth = this.MainForm.AppInfo.GetString(
    "readerinfoform_optiondlg",
    "cardphoto_maxwidth",
    "120");
            int nMaxWidth = -1;
            Int32.TryParse(strMaxWidth,
                out nMaxWidth);
            if (nMaxWidth != -1)
            {
                int nOldWidth = image.Width;
                // ��Сͼ��
                // parameters:
                //		nNewWidth0	���(0��ʾ���仯)
                //		nNewHeight0	�߶�
                //      bRatio  �Ƿ񱣳��ݺ����
                // return:
                //      -1  ����
                //      0   û�б�Ҫ����(objBitmapδ����)
                //      1   �Ѿ�����
                nRet = GraphicsUtil.ShrinkPic(ref image,
                    nMaxWidth,
                    0,
                    true,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nOldWidth != image.Width)
                {
                    strShrinkComment = "ͼ���ȱ��� "+nOldWidth.ToString()+" ������С�� "+image.Width.ToString()+" ����";
                }
            }

            string strTempFilePath = FileUtil.NewTempFileName(this.MainForm.DataDir,
                "~temp_make_cardphoto_",
                ".png");

            image.Save(strTempFilePath, System.Drawing.Imaging.ImageFormat.Png);
            image.Dispose();
            image = null;
            
            List<ListViewItem> items = this.binaryResControl1.FindItemByUsage("cardphoto");
            if (items.Count == 0)
            {
                nRet = this.binaryResControl1.AppendNewItem(
    strTempFilePath,
    "cardphoto",
    out strError);

            }
            else
            {
                nRet = this.binaryResControl1.ChangeObjectFile(items[0],
     strTempFilePath,
     "cardphoto",
             out strError);
            }
            if (nRet == -1)
                goto ERROR1;
#endif
                nRet = SetCardPhoto(image,
                out strShrinkComment,
                out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            // �л�����������ҳ���Ա�������ܿ����ոմ����Ķ�����
            this.tabControl_readerInfo.SelectedTab = this.tabPage_objects;

            MessageBox.Show(this, "֤����Ƭ�Ѿ��ɹ�������\r\n"
                +strShrinkComment
                +"\r\n\r\n(����ǰ���߼�¼��δ���棬ͼ��������δ�ύ��������)\r\n\r\nע���Ժ󱣴浱ǰ���߼�¼��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void toolStripButton_webCamera_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strShrinkComment = "";

#if NO
            UtilityForm form = this.MainForm.EnsureUtilityForm();
            form.Activate();
            form.ActivateWebCameraPage();
#endif
            this.MainForm.DisableCamera();
            try
            {
                using (CameraPhotoDialog dlg = new CameraPhotoDialog())
                {
                    // MainForm.SetControlFont(dlg, this.Font, false);
                    dlg.Font = this.Font;

                    dlg.CurrentCamera = this.MainForm.AppInfo.GetString(
                        "readerinfoform",
                        "current_camera",
                        "");

                    this.MainForm.AppInfo.LinkFormState(dlg, "CameraPhotoDialog_state");
                    dlg.ShowDialog(this);
                    this.MainForm.AppInfo.UnlinkFormState(dlg);

                    this.MainForm.AppInfo.SetString(
                        "readerinfoform",
                        "current_camera",
                        dlg.CurrentCamera);

                    if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                        return;

                    int nRet = 0;

                    Image image = dlg.Image;

                    using (image)
                    {
                        // �Զ���Сͼ��
                        nRet = SetCardPhoto(image,
                        out strShrinkComment,
                        out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }


                }

            }
            finally
            {
                Application.DoEvents();

                this.MainForm.EnableCamera();
            }

            // �л�����������ҳ���Ա�������ܿ����ոմ����Ķ�����
            this.tabControl_readerInfo.SelectedTab = this.tabPage_objects;  // �ᵼ�����뽹��仯�����ߴ�ֹͣ��׽����

            MessageBox.Show(this, "֤����Ƭ�Ѿ��ɹ�������\r\n"
                + strShrinkComment
                + "\r\n\r\n(����ǰ���߼�¼��δ���棬ͼ��������δ�ύ��������)\r\n\r\nע���Ժ󱣴浱ǰ���߼�¼��");
            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }


        // װ��һ���հ׼�¼ �ӱ���
        private void toolStripMenuItem_loadBlankFromLocal_Click(object sender, EventArgs e)
        {
            LoadReaderTemplateFromLocal();
            this.toolStripButton_loadBlank.Image = this.toolStripMenuItem_loadBlankFromLocal.Image;
            this.toolStripButton_loadBlank.Text = this.toolStripMenuItem_loadBlankFromLocal.Text;
        }

        // װ��һ���հ׼�¼ �ӷ�����
        private void ToolStripMenuItem_loadBlankFromServer_Click(object sender, EventArgs e)
        {
            LoadReaderTemplateFromServer();
            this.toolStripButton_loadBlank.Image = this.ToolStripMenuItem_loadBlankFromServer.Image;
            this.toolStripButton_loadBlank.Text = this.ToolStripMenuItem_loadBlankFromServer.Text;
        }

        // ��仯������
        private void toolStripButton_loadBlank_Click(object sender, EventArgs e)
        {
            if (this.toolStripButton_loadBlank.Text == this.toolStripMenuItem_loadBlankFromLocal.Text)
            {
                LoadReaderTemplateFromLocal();
            }
            else
            {
                LoadReaderTemplateFromServer();
            }
        }


        IpcClientChannel m_idcardChannel = new IpcClientChannel();
        IIdcard m_idcardObj = null;

        int StartIdcardChannel(
            string strUrl,
            out string strError)
        {
            strError = "";

            //Register the channel with ChannelServices.
            ChannelServices.RegisterChannel(m_idcardChannel, false);

            try
            {
                m_idcardObj = (IIdcard)Activator.GetObject(typeof(IIdcard),
                    strUrl);
                if (m_idcardObj == null)
                {
                    strError = "�޷����ӵ������� " + strUrl;
                    return -1;
                }
            }
            finally
            {
            }

            return 0;
        }

        void EndIdcardChannel()
        {
            ChannelServices.UnregisterChannel(m_idcardChannel);
        }

        // parameters:
        //      strSelection    ���֤�ֶ�ѡ���б�ȱʡֵΪ "name,gender,nation,dateOfBirth,address,idcardnumber,agency,validaterange,photo"
        //      bSetCreateDate  �Ƿ����� ��֤���� �ֶ�����
        static int BuildReaderXml(string strIdcardXml,
            string strSelection,
            bool bSetReaderBarcode,
            bool bSetCreateDate,
            ref string strReaderXml,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(strReaderXml) == true)
                strReaderXml = "<root />";

            XmlDocument domSource = new XmlDocument();
            try
            {
                domSource.LoadXml(strIdcardXml);
            }
            catch (Exception ex)
            {
                strError = "���֤��Ϣ XML װ�� DOM ʧ��: " + ex.Message;
                return -1;
            }

            XmlDocument domTarget = new XmlDocument();
            try
            {
                domTarget.LoadXml(strReaderXml);
            }
            catch (Exception ex)
            {
                strError = "ԭ�ж���XMLװ��DOMʧ��: " + ex.Message;
                return -1;
            }

            // ���֤��
            if (StringUtil.IsInList("idcardnumber", strSelection) == true)
            {
                string strID = DomUtil.GetElementText(domSource.DocumentElement,
                    "id");

                if (bSetReaderBarcode == true)
                {
                    // ����֤��
                    DomUtil.SetElementText(domTarget.DocumentElement,
                        "barcode", strID);
                }

                DomUtil.SetElementText(domTarget.DocumentElement,
                    "idCardNumber", strID);
            }

            // ����
            if (StringUtil.IsInList("name", strSelection) == true)
            {
                string strName = DomUtil.GetElementText(domSource.DocumentElement,
        "name");
                DomUtil.SetElementText(domTarget.DocumentElement,
                    "name", strName);
            }

            // �Ա�
            if (StringUtil.IsInList("gender", strSelection) == true)
            {
                string strGender = DomUtil.GetElementText(domSource.DocumentElement,
        "gender");
                DomUtil.SetElementText(domTarget.DocumentElement,
                    "gender", strGender);
            }

            // ����
            if (StringUtil.IsInList("nation", strSelection) == true)
            {
                string strNation = DomUtil.GetElementText(domSource.DocumentElement,
    "nation");
                DomUtil.SetElementText(domTarget.DocumentElement,
                    "nation", strNation);
            }

            // ��������
            if (StringUtil.IsInList("dateOfBirth", strSelection) == true)
            {
                string strDateOfBirth = DomUtil.GetElementText(domSource.DocumentElement,
    "dateOfBirth");
                DomUtil.SetElementText(domTarget.DocumentElement,
                    "dateOfBirth", strDateOfBirth);
            }

            // ��ͥ��ַ
            if (StringUtil.IsInList("address", strSelection) == true)
            {
                string strAddress = DomUtil.GetElementText(domSource.DocumentElement,
    "address");
                DomUtil.SetElementText(domTarget.DocumentElement,
                    "address", strAddress);
            }

            // ��֤����
            string strCreateDate = DomUtil.GetElementText(domSource.DocumentElement,
"createDate");

            // ʧЧ����
            string strExpireDate = DomUtil.GetElementText(domSource.DocumentElement,
"expireDate");
            if (StringUtil.IsInList("validaterange", strSelection) == true)
            {
                DomUtil.SetElementText(domTarget.DocumentElement,
                    "expireDate", strExpireDate);
            }

            // ��֤����
            string strAgency = DomUtil.GetElementText(domSource.DocumentElement,
"agency");
            string strComment = "";

            if (StringUtil.IsInList("agency", strSelection) == true)
            {
                strComment += "����¼�������֤��Ϣ���������֤ǩ������: " + strAgency + "; ";
            }
            if (StringUtil.IsInList("validaterange", strSelection) == true)
            {
                strComment += "��Ч����: " + DateTimeUtil.LocalDate(strCreateDate)
                    + " - "
                    + DateTimeUtil.LocalDate(strExpireDate);
            }

            if (string.IsNullOrEmpty(strComment) == false)
            {
                DomUtil.SetElementText(domTarget.DocumentElement,
                    "comment", strComment);
            }

            // ���߼�¼�Ĵ���������������
            if (bSetCreateDate == true)
            {
                DomUtil.SetElementText(domTarget.DocumentElement,
                    "createDate", DateTimeUtil.Rfc1123DateTimeStringEx(DateTime.Now));
            }

            // TODO: �Ƿ񾯸��Ѿ�ʧЧ�����֤��?

            strReaderXml = domTarget.DocumentElement.OuterXml;
            return 0;
        }

        // 
        /// <summary>
        /// ���ɾ����ǰ��¼��֤����Ƭ����
        /// </summary>
        public void ClearCardPhoto()
        {
            List<ListViewItem> items = this.binaryResControl1.FindItemByUsage("cardphoto");

            this.binaryResControl1.MaskDelete(items);
        }

        /// <summary>
        /// ��ǰ�������Ƿ��Ѿ�������;Ϊ "cardphoto" �Ķ�����Դ
        /// </summary>
        public bool HasCardPhoto
        {
            get
            {
                List<ListViewItem> items = this.binaryResControl1.FindItemByUsage("cardphoto");
                if (items.Count > 0)
                {
                    // �۲��Ƿ�������һ���ߴ�Ϊ 0 �������
                    foreach (ListViewItem item in items)
                    {
                        string strSizeString = ListViewUtil.GetItemText(item, BinaryResControl.COLUMN_SIZE);
                        if (string.IsNullOrEmpty(strSizeString) == false)
                        {
                            long v = 0;
                            if (long.TryParse(strSizeString, out v) == false)
                                continue;
                            if (v > 0)
                                return true;
                        }
                    }
                    return false;
                }
                return false;
            }
        }

        // 
        /// <summary>
        /// ���õ�ǰ��¼��֤����Ƭ����
        /// </summary>
        /// <param name="image">��Ƭ����</param>
        /// <param name="strShrinkComment">��������ע��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int SetCardPhoto(Image image,
            out string strShrinkComment,
            out string strError)
        {
            strError = "";
            strShrinkComment = "";
            int nRet = 0;

            // �Զ���Сͼ��
            string strMaxWidth = this.MainForm.AppInfo.GetString(
    "readerinfoform_optiondlg",
    "cardphoto_maxwidth",
    "120");
            int nMaxWidth = -1;
            Int32.TryParse(strMaxWidth,
                out nMaxWidth);
            if (nMaxWidth != -1)
            {
                int nOldWidth = image.Width;
                // ��Сͼ��
                // parameters:
                //		nNewWidth0	���(0��ʾ���仯)
                //		nNewHeight0	�߶�
                //      bRatio  �Ƿ񱣳��ݺ����
                // return:
                //      -1  ����
                //      0   û�б�Ҫ����(objBitmapδ����)
                //      1   �Ѿ�����
                nRet = DigitalPlatform.Drawing.GraphicsUtil.ShrinkPic(ref image,
                    nMaxWidth,
                    0,
                    true,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nOldWidth != image.Width)
                {
                    strShrinkComment = "ͼ���ȱ��� " + nOldWidth.ToString() + " ������С�� " + image.Width.ToString() + " ����";
                }
            }

            string strTempFilePath = FileUtil.NewTempFileName(this.MainForm.DataDir,
                "~temp_make_cardphoto_",
                ".png");

            image.Save(strTempFilePath, System.Drawing.Imaging.ImageFormat.Png);
            image.Dispose();
            image = null;

            List<ListViewItem> items = this.binaryResControl1.FindItemByUsage("cardphoto");
            if (items.Count == 0)
            {
                ListViewItem item = null;
                nRet = this.binaryResControl1.AppendNewItem(
    strTempFilePath,
    "cardphoto",
    out item,
    out strError);

            }
            else
            {
                nRet = this.binaryResControl1.ChangeObjectFile(items[0],
     strTempFilePath,
     "cardphoto",
             out strError);
            }
            if (nRet == -1)
                goto ERROR1;

            return 0;
        ERROR1:
            return -1;
        }

        /// <summary>
        /// �Ƿ���ʾ���Ƿ������֤�ŵ���֤����š���ť
        /// </summary>
        public string DisplaySetReaderBarcodeDialogButton
        {
            get
            {
                return this.MainForm.AppInfo.GetString(
    "reader_info_form",
    "display_setreaderbarcode_dialog_button",
    "no");
            }
            set
            {
                this.MainForm.AppInfo.SetString(
    "reader_info_form",
    "display_setreaderbarcode_dialog_button",
    value);
            }
        }

        /// <summary>
        /// �Ƿ���ʾ���Ƿ������֤�ŵ���֤����š��Ի���
        /// </summary>
        public bool DisplaySetReaderBarcodeDialog
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
    "reader_info_form",
    "display_setreaderbarcode_dialog",
    true);
            }
            set
            {
                this.MainForm.AppInfo.SetBoolean(
    "reader_info_form",
    "display_setreaderbarcode_dialog",
    value);
            }
        }

        // 
        /// <summary>
        /// ���֤�ֶ�ѡ���б�
        /// </summary>
        public string IdcardFieldSelection
        {
            get
            {
                return this.MainForm.AppInfo.GetString(
    "readerinfoform_optiondlg",
    "idcardfield_filter_list",
    "name,gender,nation,dateOfBirth,address,idcardnumber,agency,validaterange,photo");

            }
        }

        // 
        /// <summary>
        /// �����ֶ����Ի���ʱ�Ƿ��Զ�����
        /// </summary>
        public bool AutoRetryReaderCard
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                "reader_info_form",
                "autoretry_readcarddialog",
                true);
            }
            set
            {
                this.MainForm.AppInfo.SetBoolean(
                "reader_info_form",
                "autoretry_readcarddialog",
                value);
            }
        }

        // �ڶ��ߴ���Χ���Զ��ر� ���֤������ ���̷���(&S)
        /// <summary>
        /// �Ƿ��ڶ��ߴ���Χ���Զ��ر� ���֤������ ���̷���
        /// </summary>
        public bool DisableIdcardReaderSendkey
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
    "reader_info_form",
    "disable_idcardreader_sendkey",
    true);
            }
        }

        string m_strLoadSource = "";   // ��ʲô����װ�صĿհ׼�¼��Ϣ? local server idcard

        string m_strIdcardXml = "";
        byte[] m_baPhoto = null;

        // parameters:
        //      bClear  ����ǰ�Ƿ�����༭��ԭ�е�ȫ������
        // return:
        //      -1  ����
        //      0   ����װ��
        //      1   �ɹ�
        int LoadFromIdcard(bool bClear,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(this.MainForm.IdcardReaderUrl) == true)
            {
                strError = "��δ���� ���֤������URL ϵͳ�������޷���ȡ���֤��";
                return -1;
            }

            if (string.IsNullOrEmpty(this.IdcardFieldSelection) == true)
            {
                MessageBox.Show(this, "��ʾ�������õ����֤�ֶ�ѡ�ò����в������κ��ֶΣ����Ե������û��ʵ�����塣(�����ڶ��ߴ��ġ�ѡ��Ի������޸����֤�ֶ�ѡ�ò���)");
            }

            this.EnableControls(false);

            bool bOldSendKeyEnabled = true;
            try
            {
                int nRet = StartIdcardChannel(
                    this.MainForm.IdcardReaderUrl,
                    out strError);
                if (nRet == -1)
                    return -1;

                Image image = null;

                try
                {
                    try
                    {
                        bOldSendKeyEnabled = m_idcardObj.SendKeyEnabled;
                        m_idcardObj.SendKeyEnabled = false;
                    }
                    catch (Exception ex)
                    {
                        strError = "��� " + this.MainForm.IdcardReaderUrl + " ����ʧ��: " + ex.Message;
                        return -1;
                    }

                    // ������δ����
                    // �ڽ�ֹפ������ SendKey �Ժ�ų��ֶԻ���Ϻ�
                    if (this.ReaderXmlChanged == true
    || this.ObjectChanged == true)
                    {
                        DialogResult result = MessageBox.Show(this,
            "��ǰ����Ϣ���޸ĺ���δ���档����ʱ�������¶�����Ϣ������δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�����¶�����Ϣ? ",
            "ReaderInfoForm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
                        if (result != DialogResult.Yes)
                            return 0;
                    }

                    m_strIdcardXml = "";
                    m_baPhoto = null;

                REDO:
                    try
                    {
                        // prameters:
                        //      strStyle ��λ�ȡ���ݡ�all/xml/photo ��һ�����߶�������
                        // return:
                        //      -1  ����
                        //      0   �ɹ�
                        //      1   �ظ�����δ���ߵĿ���
                        nRet = m_idcardObj.ReadCard("all",
                            out m_strIdcardXml,
                            out m_baPhoto,
                            out strError);
                    }
                    catch (Exception ex)
                    {
                        strError = "��� " + this.MainForm.IdcardReaderUrl + " ����ʧ��: " + ex.Message;
                        return -1;
                    }

                    if (nRet == -1)
                    {
                        /*
                        // �̶��������̽��һ��
                        DialogResult result = MessageBox.Show(this,
"������֤�ŵ��������ϣ������ֵ��������...",
"ReaderInfoForm",
MessageBoxButtons.RetryCancel,
MessageBoxIcon.Asterisk,
MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.Retry)
                            goto REDO;
                        strError = "��������";
                         * */

                        PlaceIdcardDialog dlg = new PlaceIdcardDialog();
                        MainForm.SetControlFont(dlg, this.Font, false);
                        dlg.AutoRetry = this.AutoRetryReaderCard;
                        dlg.ReadCard -= new ReadCardEventHandler(dlg_ReadCard);
                        dlg.ReadCard += new ReadCardEventHandler(dlg_ReadCard);
                        this.MainForm.AppInfo.LinkFormState(dlg, "PlaceIdcardDialog_state");
                        dlg.ShowDialog(this);
                        this.MainForm.AppInfo.UnlinkFormState(dlg);

                        this.AutoRetryReaderCard = dlg.AutoRetry;

                        if (dlg.DialogResult == System.Windows.Forms.DialogResult.Retry)
                            goto REDO;
                        if (dlg.DialogResult == System.Windows.Forms.DialogResult.OK)
                        {
                            Debug.Assert(string.IsNullOrEmpty(m_strIdcardXml) == false, "");
                        }
                        else
                        {
                            Debug.Assert(dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel, "");
                            strError = "��������";
                            return 0;
                        }
                    }

                    Console.Beep(); // ��ʾ��ȡ�ɹ�

                    // string strLocalTempPhotoFilename = this.MainForm.DataDir + "/~current_unsaved_patron_photo.png";
                    if (m_baPhoto != null
                    && StringUtil.IsInList("photo", this.IdcardFieldSelection) == true)
                    {
                        using (MemoryStream s = new MemoryStream(m_baPhoto))
                        {
                            image = new Bitmap(s);
                        }

                        // image.Save(strLocalTempPhotoFilename, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    else
                    {
                        // File.Delete(strLocalTempPhotoFilename);
                    }
                    m_baPhoto = null;   // �ͷſռ�

                }
                finally
                {
                    try
                    {
                        m_idcardObj.SendKeyEnabled = bOldSendKeyEnabled;
                    }
                    catch
                    {
                    }

                    EndIdcardChannel();
                }

                bool bSetReaderBarcode = false;
                if (StringUtil.IsInList("idcardnumber", this.IdcardFieldSelection) == true)
                {
                    if (this.DisplaySetReaderBarcodeDialog == true)
                    {
                        SetReaderBarcodeNumberDialog dlg = new SetReaderBarcodeNumberDialog();
                        MainForm.SetControlFont(dlg, this.Font, false);

                        dlg.DontAsk = !this.DisplaySetReaderBarcodeDialog;
                        dlg.InitialSelect = this.DisplaySetReaderBarcodeDialogButton;
                        this.MainForm.AppInfo.LinkFormState(dlg, "readerinfoformm_SetReaderBarcodeNumberDialog_state");
                        dlg.ShowDialog(this);
                        this.MainForm.AppInfo.UnlinkFormState(dlg);

                        this.DisplaySetReaderBarcodeDialog = !dlg.DontAsk;
                        this.DisplaySetReaderBarcodeDialogButton = (dlg.DialogResult == System.Windows.Forms.DialogResult.Yes ? "yes" : "no");

                        bSetReaderBarcode = (dlg.DialogResult == System.Windows.Forms.DialogResult.Yes);
                    }
                    else
                    {
                        bSetReaderBarcode = (this.DisplaySetReaderBarcodeDialogButton == "yes");
                    }
                }

                string strReaderXml = "";
                if (bClear == false)
                {
                    nRet = this.readerEditControl1.GetData(
                        out strReaderXml,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "��ȡ�༭��������XMLʱ����" + strError;
                        return -1;
                    }
                }

                nRet = BuildReaderXml(m_strIdcardXml,
                    this.IdcardFieldSelection,
                    bSetReaderBarcode,
                    bClear,
                    ref strReaderXml,
                    out strError);
                if (nRet == -1)
                    return -1;

                nRet = this.readerEditControl1.SetData(strReaderXml,
                    bClear == true ? "" : this.readerEditControl1.RecPath,    // 2013/6/17 ����������ǰ�����ݣ���Ҳ������ǰ��·��
                    bClear == true ? null : this.readerEditControl1.Timestamp,  // 2013/6/27
                    out strError);
                if (nRet == -1)
                {
                    return -1;
                }

                if (StringUtil.IsInList("photo", this.IdcardFieldSelection) == true)
                {
                    // this.binaryResControl1.Clear();

                    if (image != null)
                    {
                        string strShrinkComment = "";
                        nRet = SetCardPhoto(image,
        out strShrinkComment,
        out strError);
                        if (nRet == -1)
                            return -1;
                        image.Dispose();
                        image = null;
                    }
                }

#if NO
                Global.ClearHtmlPage(this.webBrowser_readerInfo,
                    this.MainForm.DataDir);
#endif
                ClearReaderHtmlPage();

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("�������� HTML Ԥ�� ...");
                stop.BeginLoop();

                EnableControls(false);

                try
                {
                    byte[] baTimestamp = null;
                    string strOutputRecPath = "";

                    string strBarcode = strReaderXml;

                    string[] results = null;
                    long lRet = Channel.GetReaderInfo(
                        stop,
                        strBarcode,
                        "html",
                        out results,
                        out strOutputRecPath,
                        out baTimestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "�������߼�¼ HTML Ԥ��ʱ��������: " + strError;
                        // Global.SetHtmlString(this.webBrowser_readerInfo, strError);
                        this.m_webExternalHost.SetTextString(strError);
                    }
                    else
                    {
                        string strHtml = results[0];

#if NO
                        // 2013/12/21
                        this.m_webExternalHost.StopPrevious();
                        this.webBrowser_readerInfo.Stop();

                        Global.SetHtmlString(this.webBrowser_readerInfo,
                            strHtml,
                            this.MainForm.DataDir,
                            "readerinfoform_reader");
#endif
                        this.SetReaderHtmlString(strHtml);
                    }
                }
                finally
                {
                    EnableControls(true);

                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }
                
                /*
                this.SetXmlToWebbrowser(this.webBrowser_xml,
                    strReaderXml);
                 * */
                Global.SetXmlToWebbrowser(this.webBrowser_xml,
this.MainForm.DataDir,
"xml",
strReaderXml);
                if (bClear == false) // 2013/6/19
                {
                    if (Global.IsAppendRecPath(this.readerEditControl1.RecPath) == true)
                        this.m_strSetAction = "new";
                    else
                        this.m_strSetAction = "change";
                }
                else
                    this.m_strSetAction = "new";

                this.m_strLoadSource = "idcard";
                return 1;
            }
            finally
            {
                this.EnableControls(true);
            }
        }

        void dlg_ReadCard(object sender, ReadCardEventArgs e)
        {
            try
            {
                string strError = "";

                string strTempXml = ""; // 2013/10/17
                // prameters:
                //      strStyle ��λ�ȡ���ݡ�all/xml/photo ��һ�����߶�������
                // return:
                //      -1  ����
                //      0   �ɹ�
                //      1   �ظ�����δ���ߵĿ���
                int nRet = m_idcardObj.ReadCard("all",
                    out strTempXml,
                    out m_baPhoto,
                    out strError);
                if (nRet != -1)
                {
                    e.Done = true;
                    Debug.Assert(string.IsNullOrEmpty(strTempXml) == false, "");
                    m_strIdcardXml = strTempXml;
                }
            }
            catch (Exception /*ex*/)
            {
            }
        }

        // �����֤װ��
        private void toolStripButton_loadFromIdcard_Click(object sender, EventArgs e)
        {
            string strError = "";

            // �����סControl��ʹ��������ܣ��ͱ�ʾ�������ǰ������
            bool bControl = Control.ModifierKeys == Keys.Control;

            int nRet = LoadFromIdcard(!bControl, out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
        }

        private void ReaderInfoForm_Enter(object sender, EventArgs e)
        {

        }

        private void ReaderInfoForm_Leave(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem_moveRecord_Click(object sender, EventArgs e)
        {
            string strError = "";
            // int nRet = 0;
            string strTargetRecPath = "";

            if (string.IsNullOrEmpty(this.readerEditControl1.RecPath) == true)
            {
                strError = "��ǰ��¼��·��Ϊ�գ��޷������ƶ�����";
                goto ERROR1;
            }

            if (this.ReaderXmlChanged == true
    || this.ObjectChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
"��ǰ����Ϣ���޸ĺ���δ���档����ʱ�����ƶ�����������δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�����ƶ�����? ",
"ReaderInfoForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                    return;   // cancelled

            }

            // ���ֶԻ������û�����ѡ��Ŀ���
            ReaderSaveToDialog saveto_dlg = new ReaderSaveToDialog();
            MainForm.SetControlFont(saveto_dlg, this.Font, false);
            saveto_dlg.Text = "�ƶ����߼�¼";
            saveto_dlg.MessageText = "��ѡ��Ҫ�ƶ�ȥ��Ŀ���¼λ��";
            saveto_dlg.MainForm = this.MainForm;
            saveto_dlg.RecPath = this.readerEditControl1.RecPath;
            saveto_dlg.RecID = "?";

            this.MainForm.AppInfo.LinkFormState(saveto_dlg, "readerinfoform_movetodialog_state");
            saveto_dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(saveto_dlg);

            if (saveto_dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����ƶ����߼�¼ " + this.readerEditControl1.RecPath + " �� "+saveto_dlg.RecPath+"...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                strTargetRecPath = saveto_dlg.RecPath;

                byte[] target_timestamp = null;
                long lRet = Channel.MoveReaderInfo(
    stop,
    this.readerEditControl1.RecPath,
    ref strTargetRecPath,
    out target_timestamp,
    out strError);
                if (lRet == -1)
                    goto ERROR1;

                Debug.Assert(string.IsNullOrEmpty(strTargetRecPath) == false, "");
            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            // ����װ�ش�������
            Debug.Assert(string.IsNullOrEmpty(strTargetRecPath) == false, "");
            LoadRecordByRecPath(strTargetRecPath,
                "");

            MessageBox.Show(this, "�ƶ��ɹ���");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

#if NO
        bool m_bSuppressScriptErrors = true;
        public bool SuppressScriptErrors
        {
            get
            {
                return this.m_bSuppressScriptErrors;
            }
            set
            {
                this.m_bSuppressScriptErrors = value;
            }
        }
#endif

        private void webBrowser_readerInfo_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            ((WebBrowser)sender).Document.Window.Error -= new HtmlElementErrorEventHandler(Window_Error);
            ((WebBrowser)sender).Document.Window.Error += new HtmlElementErrorEventHandler(Window_Error);
        }

        void Window_Error(object sender, HtmlElementErrorEventArgs e)
        {
            if (this.MainForm.SuppressScriptErrors == true)
                e.Handled = true;
        }

        private void readerEditControl1_GetLibraryCode(object sender, GetLibraryCodeEventArgs e)
        {
            e.LibraryCode = this.MainForm.GetReaderDbLibraryCode(e.DbName);
        }

        #region ָ�ƵǼǹ���

        IpcClientChannel m_fingerPrintChannel = new IpcClientChannel();
        IFingerprint m_fingerPrintObj = null;

        int StartFingerprintChannel(
            string strUrl,
            out string strError)
        {
            strError = "";

            //Register the channel with ChannelServices.
            ChannelServices.RegisterChannel(m_fingerPrintChannel, true);

            try
            {
                m_fingerPrintObj = (IFingerprint)Activator.GetObject(typeof(IFingerprint),
                    strUrl);
                if (m_fingerPrintObj == null)
                {
                    strError = "�޷����ӵ������� " + strUrl;
                    return -1;
                }
            }
            finally
            {
            }

            return 0;
        }

        void EndFingerprintChannel()
        {
            ChannelServices.UnregisterChannel(m_fingerPrintChannel);
        }

        // �ֲ�����ָ����Ϣ���ٻ���
        // return:
        //      -2  remoting����������ʧ�ܡ�����������δ����
        //      -1  ����
        //      0   �ɹ�
        int UpdateFingerprintCache(
            string strBarcode,
            string strFingerprint,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(this.MainForm.FingerprintReaderUrl) == true)
            {
                strError = "��δ���� ָ���Ķ���URL ϵͳ�������޷�����ָ�Ƹ��ٻ���";
                return -1;
            }

            int nRet = StartFingerprintChannel(
                this.MainForm.FingerprintReaderUrl,
                out strError);
            if (nRet == -1)
                return -1;

            try
            {
                List<FingerprintItem> items = new List<FingerprintItem>();

                FingerprintItem item = new FingerprintItem();
                item.ReaderBarcode = strBarcode;
                item.FingerprintString = strFingerprint;
                items.Add(item);

                // return:
                //      -2  remoting����������ʧ�ܡ�����������δ����
                //      -1  ����
                //      0   �ɹ�
                nRet = AddItems(items,
    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == -2)
                    return -2;
            }
            finally
            {
                EndFingerprintChannel();
            }

            return 0;
        }

        // return:
        //      -2  remoting����������ʧ�ܡ�����������δ����
        //      -1  ����
        //      0   �ɹ�
        int AddItems(List<FingerprintItem> items,
    out string strError)
        {
            strError = "";

            try
            {
                int nRet = m_fingerPrintObj.AddItems(items,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            // [System.Runtime.Remoting.RemotingException] = {"���ӵ� IPC �˿�ʧ��: ϵͳ�Ҳ���ָ�����ļ���\r\n "}
            catch (System.Runtime.Remoting.RemotingException ex)
            {
                strError = "��� " + this.MainForm.FingerprintReaderUrl + " �� AddItems() ����ʧ��: " + ex.Message;
                return -2;
            }
            catch (Exception ex)
            {
                strError = "��� " + this.MainForm.FingerprintReaderUrl + " �� AddItems() ����ʧ��: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // return:
        //      -1  error
        //      0   ��������
        //      1   �ɹ�����
        int ReadFingerprintString(
            out string strFingerprint,
            out string strVersion,
            out string strError)
        {
            strError = "";
            strFingerprint = "";
            strVersion = "";

            if (string.IsNullOrEmpty(this.MainForm.FingerprintReaderUrl) == true)
            {
                strError = "��δ���� ָ���Ķ���URL ϵͳ�������޷���ȡָ����Ϣ";
                return -1;
            }

            int nRet = StartFingerprintChannel(
                this.MainForm.FingerprintReaderUrl,
                out strError);
            if (nRet == -1)
                return -1;

            try
            {
                try
                {
                    // ���һ��ָ�������ַ���
                    // return:
                    //      -1  error
                    //      0   ��������
                    //      1   �ɹ�����
                    nRet = m_fingerPrintObj.GetFingerprintString(out strFingerprint,
                        out strVersion,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    return nRet;
                    // this.MainForm.StatusBarMessage = "";
                }
                catch (Exception ex)
                {
                    strError = "��� " + this.MainForm.FingerprintReaderUrl + " �� GetFingerprintString() ����ʧ��: " + ex.Message;
                    return -1;
                }
                // Console.Beep(); // ��ʾ��ȡ�ɹ�
            }
            finally
            {
                EndFingerprintChannel();
            }
        }

        #endregion

        private void toolStripButton_registerFingerprint_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strFingerprint = "";
            string strVersion = "";

            this.EnableControls(false);
            this.MainForm.StatusBarMessage = "�ȴ�ɨ��ָ��...";
            this.Update();
            Application.DoEvents();
            try
            {
                REDO:
                // return:
                //      -1  error
                //      0   ��������
                //      1   �ɹ�����
                int nRet = ReadFingerprintString(
                    out strFingerprint,
                    out strVersion,
                    out strError);
                if (nRet == -1)
                {
                    DialogResult temp_result = MessageBox.Show(this,
strError + "\r\n\r\n�Ƿ�����?",
"ReaderInfoForm",
MessageBoxButtons.RetryCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                    if (temp_result == DialogResult.Retry)
                        goto REDO;

                }

                if (nRet == -1 || nRet == 0)
                    goto ERROR1;

                this.readerEditControl1.Fingerprint = strFingerprint;
                this.readerEditControl1.FingerprintVersion = strVersion;
                this.readerEditControl1.Changed = true;
            }
            finally
            {
                this.EnableControls(true);
            }

            // MessageBox.Show(this, strFingerprint);
            this.MainForm.StatusBarMessage = "ָ����Ϣ��ȡ�ɹ�";
            return;
        ERROR1:
            this.MainForm.StatusBarMessage = strError;
            MessageBox.Show(this, strError);
        }

        private void toolStripMenuItem_clearFingerprint_Click(object sender, EventArgs e)
        {
            /*
            if (string.IsNullOrEmpty(this.readerEditControl1.Fingerprint) == false
                || string.IsNullOrEmpty(this.readerEditControl1.FingerprintVersion) == false)
            {
            }
             * */
                this.readerEditControl1.FingerprintVersion = "";
                this.readerEditControl1.Fingerprint = "";
                this.readerEditControl1.Changed = true;
        }

        // �����ڽ������ŵ��ı��ļ�
        private void ToolStripMenuItem_exportBorrowingBarcode_Click(object sender, EventArgs e)
        {
            string strError = "";

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ�����������ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            // dlg.FileName = this.ExportTextFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "�ı��ļ� (*.txt)|*.txt|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            bool bAppend = true;

            if (File.Exists(dlg.FileName) == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ı��ļ� '" + dlg.FileName + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel �������)",
                    "ReaderInfoForm",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Cancel)
                    return;
                if (result == DialogResult.No)
                    bAppend = false;
            }

            string strNewXml = "";
            int nRet = this.readerEditControl1.GetData(
                out strNewXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;


            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strNewXml);
            }
            catch (Exception ex)
            {
                strError = "װ��XML��DOM����: " + ex.Message;
                goto ERROR1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("borrows/borrow");

            using (StreamWriter sw = new StreamWriter(dlg.FileName, bAppend, Encoding.UTF8))
            {
                
                foreach (XmlElement node in nodes)
                {
                    string strBarcode = node.GetAttribute("barcode");
                    if (string.IsNullOrEmpty(strBarcode) == false)
                        sw.WriteLine(strBarcode);
                }
            }

            this.MainForm.StatusBarMessage = "�����ɹ���";
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void toolStripTextBox_barcode_Enter(object sender, EventArgs e)
        {
            if (m_nChannelInUse > 0)
                return;
            this.MainForm.EnterPatronIdEdit(InputType.PQR);

            // 2013/5/25
            // ��ֹ���֤���������̷����ʱ��֤���������������
            if (this.DisableIdcardReaderSendkey == true)
            {
                EnableSendKey(true);
            }

            // Debug.WriteLine("Barcode textbox focued");

        }

        private void toolStripTextBox_barcode_Leave(object sender, EventArgs e)
        {
            // 2014/10/12
            if (this.MainForm == null)
                return;

            this.MainForm.LeavePatronIdEdit();

            // 2013/5/25
            // ��ֹ���֤���������̷����ʱ��֤���������������
            if (this.DisableIdcardReaderSendkey == true)
            {
                EnableSendKey(false);
            }
            // Debug.WriteLine("Barcode textbox leave");
        }

        private void readerEditControl1_CreatePinyin(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            string strHanzi = this.readerEditControl1.NameString;
            if (string.IsNullOrEmpty(strHanzi) == true)
            {
                strError = "��δ�����������������޷���������ƴ��";
                goto ERROR1;
            }

            this.EnableControls(false);
            try
            {
                string strPinyin = "";
                // return:
                //      -1  ����
                //      0   �û��ж�ѡ��
                //      1   �ɹ�
                nRet = this.MainForm.GetPinyin(
                    this,
                    strHanzi,
                    PinyinStyle.None,
                    false,
                    out strPinyin,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                this.readerEditControl1.NamePinyin = strPinyin;
            }
            finally
            {
                this.EnableControls(true);
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ������ Excel �ļ�
        private void toolStripMenuItem_exportExcel_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            string strNewXml = "";
            nRet = this.readerEditControl1.GetData(
                out strNewXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strNewXml);
            }
            catch (Exception ex)
            {
                strError = "װ��XML��DOM����: " + ex.Message;
                goto ERROR1;
            }


            // ����һ���ض����ļ���
            string strName = DomUtil.GetElementText(dom.DocumentElement, "name");
            string strBarcode = DomUtil.GetElementText(dom.DocumentElement, "barcode");

            string strFileName = strName + "_" + strBarcode + ".xlsx";

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ����� Excel �ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = true;
            dlg.FileName = strFileName;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "Excel �ļ� (*.xlsx)|*.xlsx|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.EnableControls(false);
            try
            {
                XmlNodeList nodes = dom.DocumentElement.SelectNodes("borrows/borrow");

                ExcelDocument doc = ExcelDocument.Create(dlg.FileName);
                try
                {
                    doc.NewSheet("Sheet1");

                    int nColIndex = 0;
                    int _lineIndex = 0;

                    // ����
                    List<CellData> cells = new List<CellData>();
                    cells.Add(new CellData(nColIndex++, "����"));
                    cells.Add(new CellData(nColIndex++, strName));
                    doc.WriteExcelLine(_lineIndex, cells, WriteExcelLineStyle.None);
                    _lineIndex++;

                    // ֤�����
                    nColIndex = 0;
                    cells = new List<CellData>();
                    cells.Add(new CellData(nColIndex++, "֤�����"));
                    cells.Add(new CellData(nColIndex++, strBarcode));
                    doc.WriteExcelLine(_lineIndex, cells, WriteExcelLineStyle.None);
                    _lineIndex++;

                    // ����
                    _lineIndex++;

                    // ���� �ڽ��
                    // TODO: ��ÿ�Խ����
                    nColIndex = 0;
                    cells = new List<CellData>();
                    cells.Add(new CellData(nColIndex++, "�ڽ��(" + nodes.Count.ToString() + ")"));
                    doc.WriteExcelLine(_lineIndex, cells, WriteExcelLineStyle.None);
                    _lineIndex++;

                    // �����Ŀ������
                    nColIndex = 0;
                    cells = new List<CellData>();
                    cells.Add(new CellData(nColIndex++, "�������"));
                    cells.Add(new CellData(nColIndex++, "��ĿժҪ"));
                    cells.Add(new CellData(nColIndex++, "����ʱ��"));
                    cells.Add(new CellData(nColIndex++, "��������"));

                    doc.WriteExcelLine(_lineIndex, cells, WriteExcelLineStyle.None);
                    _lineIndex++;


                    foreach (XmlElement node in nodes)
                    {
                        nColIndex = 0;
                        cells = new List<CellData>();

                        string strItemBarcode = node.GetAttribute("barcode");
                        string strConfirmItemRecPath = node.GetAttribute("recPath");
                        string strBorrowDate = node.GetAttribute("borrowDate");
                        string strBorrowPeriod = node.GetAttribute("borrowPeriod");
                        string strSummary = "";
                        nRet = this.MainForm.GetBiblioSummary(strItemBarcode,
                            strConfirmItemRecPath,
                            true,
                            out strSummary,
                            out strError);
                        if (nRet == -1)
                            strSummary = strError;

                        cells.Add(new CellData(nColIndex++, strItemBarcode));
                        cells.Add(new CellData(nColIndex++, strSummary));
                        cells.Add(new CellData(nColIndex++, DateTimeUtil.LocalTime(strBorrowDate)));
                        cells.Add(new CellData(nColIndex++, strBorrowPeriod));

                        doc.WriteExcelLine(_lineIndex, cells, WriteExcelLineStyle.None);
                        _lineIndex++;
                    }

                    // ����
                    _lineIndex++;
                    // create time
                    {
                        _lineIndex++;
                        cells = new List<CellData>();
                        cells.Add(new CellData(0, "���ļ�����ʱ��"));
                        cells.Add(new CellData(1, DateTime.Now.ToString()));
                        doc.WriteExcelLine(_lineIndex, cells);

                        _lineIndex++;
                    }

                }
                finally
                {
                    doc.SaveWorksheet();
                    doc.Close();
                }
            }
            finally
            {
                this.EnableControls(true);
            }

            this.MainForm.StatusBarMessage = "�����ɹ���";
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// MDI�Ӵ��ڱ�֪ͨ�¼�����
        /// </summary>
        /// <param name="e">�¼�����</param>
        public override void OnNotify(ParamChangedEventArgs e)
        {
            if (e.Section == "valueTableCacheCleared")
            {
                this.readerEditControl1.OnValueTableCacheCleared();
            }
        }

        private void readerEditControl1_EditRights(object sender, EventArgs e)
        {
            DigitalPlatform.CommonDialog.PropertyDlg dlg = new DigitalPlatform.CommonDialog.PropertyDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.Text = "��ǰ���ߵ�Ȩ��";
            dlg.PropertyString = this.readerEditControl1.Rights;
            dlg.CfgFileName = this.MainForm.DataDir + "\\userrightsdef.xml";
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.readerEditControl1.Rights = dlg.PropertyString;
        }

        // �Ӻ���
        private void toolStripButton_addFriends_Click(object sender, EventArgs e)
        {
            // ��Ϊ����Ҫˢ�´��ڣ����Ҫ�����ǰ�޸��Ѿ�����
            if (this.ReaderXmlChanged == true
    || this.ObjectChanged == true)
            {
                MessageBox.Show(this, "��ǰ����Ϣ���޸ĺ���δ���档�����ȱ���󣬲��ܽ��мӺ��ѵĲ�����");
                return;
            }

            AddFriendsDialog dlg = new AddFriendsDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            this.MainForm.AppInfo.LinkFormState(dlg, "ReaderInfoForm_AddFriendsDialog_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            string strError = "";
            long lRet = 0;


            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼӺ��� ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                // Result.Value -1���� 0����ɹ�(ע�⣬��������Է�ͬ��) 1:����ǰ�Ѿ��Ǻ��ѹ�ϵ�ˣ�û�б�Ҫ�ظ����� 2:�Ѿ��ɹ����
                lRet = Channel.SetFriends(
    stop,
    "request",
    dlg.ReaderBarcode,
    dlg.Comment,
    "",
    out strError);
                if (lRet == -1 || lRet == 1)
                {
                    goto ERROR1;
                }

                if (lRet == 0)
                    strError = "�����Ѿ����������ȴ��Է�ͬ��";
            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            if (lRet == 2)
            {
                // TODO: ��Ҫ����ˢ�´��ڣ����� firends �ֶεĸ�����ʾ
                MessageBox.Show(this, "�����ֶ��Ѿ����޸ģ���ע������װ�ض��߼�¼");
            }
            this.MainForm.StatusBarMessage = strError;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
    }
}