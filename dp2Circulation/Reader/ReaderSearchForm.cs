using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Web;
using System.Reflection;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.CommonControl;
using DigitalPlatform.Text;
using DigitalPlatform.ResultSet;
using DigitalPlatform.Interfaces;

using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.Marc;
using DigitalPlatform.Script;
using ClosedXML.Excel;

namespace dp2Circulation
{
    /// <summary>
    /// ���߲�ѯ��
    /// </summary>
    public partial class ReaderSearchForm : SearchFormBase
    {

        /// <summary>
        /// �Ƿ�Ϊָ��ģʽ�����Ϊָ��ģʽ����Ҫ���ô����ʻ���¼
        /// </summary>
        public bool FingerPrintMode = false;    // �Ƿ�Ϊָ��ģʽ�����Ϊָ��ģʽ����Ҫ���ô����ʻ���¼


        /*
        // ����������к�����
        SortColumns SortColumns = new SortColumns();
         * */

        /// <summary>
        /// ����ù������������ļ���
        /// </summary>
        public string ExportBarcodeFilename = "";

        /// <summary>
        /// ����ù��������¼·���ļ���
        /// </summary>
        public string ExportRecPathFilename = "";

        string m_strUsedRecPathFilename = "";
        string m_strUsedBarcodeFilename = "";

        /// <summary>
        /// ����б� ListView
        /// </summary>
        public ListView ListViewRecords
        {
            get
            {
                return this.listView_records;
            }
        }

        /*
        public LibraryChannel Channel = new LibraryChannel();
        // public ApplicationInfo ap = null;
        public string Lang = "zh";

        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;
         * */

        /// <summary>
        /// ���캯��
        /// </summary>
        public ReaderSearchForm()
        {
            this.DbType = "patron";

            InitializeComponent();

            _listviewRecords = this.listView_records;

            ListViewProperty prop = new ListViewProperty();
            this.listView_records.Tag = prop;
            // ��һ�����⣬��¼·��
            prop.SetSortStyle(0, ColumnSortStyle.RecPath);
            prop.GetColumnTitles -= new GetColumnTitlesEventHandler(prop_GetColumnTitles);
            prop.GetColumnTitles += new GetColumnTitlesEventHandler(prop_GetColumnTitles);
        }

        void prop_GetColumnTitles(object sender, GetColumnTitlesEventArgs e)
        {
            if (e.DbName == "<blank>")
            {
                e.ColumnTitles = new ColumnPropertyCollection();
                e.ColumnTitles.Add("������");
                e.ColumnTitles.Add("����");
                return;
            }

            e.ColumnTitles = this.MainForm.GetBrowseColumnProperties(e.DbName);
        }

        // ��״̬����ʾ������Ϣ
        internal override void SetStatusMessage(string strMessage)
        {
            this.label_message.Text = strMessage;
        }


        private void ReaderSearchForm_Load(object sender, EventArgs e)
        {
            /*
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
             * */

            this.comboBox_readerDbName.Text = this.MainForm.AppInfo.GetString(
                "readersearchform",
                "readerdbname",
                "<ȫ��>");

            this.comboBox_from.Text = this.MainForm.AppInfo.GetString(
                "readersearchform",
                "from",
                "");

            this.comboBox_matchStyle.Text = this.MainForm.AppInfo.GetString(
                "readersearchform",
                "match_style",
                "ǰ��һ��");

            bool bHideMatchStyle = this.MainForm.AppInfo.GetBoolean(
                "reader_search_form",
                "hide_matchstyle",
                false);

            if (bHideMatchStyle == true)
            {
                this.label_matchStyle.Visible = false;
                this.comboBox_matchStyle.Visible = false;
                this.comboBox_matchStyle.Text = "ǰ��һ��"; // ���غ󣬲���ȱʡֵ
            }

            string strWidths = this.MainForm.AppInfo.GetString(
                "readersearchform",
                "record_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_records,
                    strWidths,
                    true);
            }

            comboBox_matchStyle_TextChanged(null, null);

            if (this.MainForm.ReaderDbFromInfos != null)
            {
                FillReaderDbFroms();
            }
        }

        /*
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }*/


        /// <summary>
        /// ���ص� Channel_BeforeLogin()
        /// </summary>
        /// <param name="sender">������</param>
        /// <param name="e">�¼�����</param>
        public override void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            if (this.FingerPrintMode == false)
                base.Channel_BeforeLogin(this, e);
            else
            {
                if (string.IsNullOrEmpty(this.MainForm.FingerprintUserName) == false
                    && this.MainForm.FingerprintUserName != this.MainForm.DefaultUserName)
                    MyBeforeLogin(this, e);
                else
                    base.Channel_BeforeLogin(this, e);
            }
        }

        void MyBeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            // ֻ�е������ʻ��������ʱ�򣬲Ž��е�һ����̽
            if (e.FirstTry == true && string.IsNullOrEmpty(this.MainForm.FingerprintPassword) == false)
            {
                e.UserName = this.MainForm.FingerprintUserName;
                e.Password = this.MainForm.FingerprintPassword;

                bool bIsReader = false; // ������Ա��ʽ

                string strLocation = this.MainForm.AppInfo.GetString(
                    "default_account",
                    "location",
                    "");    // ����̨�ź�ȱʡ�ʻ�һ��
                e.Parameters = "location=" + strLocation;
                if (bIsReader == true)
                    e.Parameters += ",type=reader";

                if (String.IsNullOrEmpty(e.UserName) == false)
                    return; // ��������, �Ա�����һ�� ������ �Ի�����Զ���¼
            }

            // 
            IWin32Window owner = null;

            if (sender is IWin32Window)
                owner = (IWin32Window)sender;
            else
                owner = this;

            string strComment = "Ϊ��ʼ��ָ�ƻ��棬��Ҫ�û� "+this.MainForm.FingerprintUserName+" ���Խ��е�¼";

            CirculationLoginDlg dlg = SetFingerprintAccount(
                e.LibraryServerUrl,
                strComment,
                string.IsNullOrEmpty(e.ErrorInfo) == false ? e.ErrorInfo : strComment,
                e.LoginFailCondition,
                owner);
            if (dlg == null)
            {
                e.Cancel = true;
                return;
            }

            e.UserName = dlg.UserName;
            e.Password = dlg.Password;
            e.SavePasswordShort = dlg.SavePasswordShort;
            e.Parameters = "location=" + dlg.OperLocation;
            if (dlg.IsReader == true)
                e.Parameters += ",type=reader";
            e.SavePasswordLong = dlg.SavePasswordLong;
            e.LibraryServerUrl = dlg.ServerUrl;
        }

        // parameters:
        CirculationLoginDlg SetFingerprintAccount(
            string strServerUrl,
            string strTitle,
            string strComment,
            LoginFailCondition fail_contidion,
            IWin32Window owner)
        {
            CirculationLoginDlg dlg = new CirculationLoginDlg();
            MainForm.SetControlFont(dlg, this.MainForm.DefaultFont);

            if (String.IsNullOrEmpty(strServerUrl) == true)
            {
                dlg.ServerUrl =
        this.MainForm.AppInfo.GetString("config",
        "circulation_server_url",
        "http://localhost:8001/dp2library");
            }
            else
            {
                dlg.ServerUrl = strServerUrl;
            }

            if (owner == null)
                owner = this;

            if (String.IsNullOrEmpty(strTitle) == false)
                dlg.Text = strTitle;

            dlg.Comment = strComment;
            dlg.UserName = this.MainForm.FingerprintUserName;

            dlg.IsReaderEnabled = false;

            dlg.SavePasswordShort = false;
            dlg.SavePasswordShortEnabled = false;

            dlg.SavePasswordLong = false;

            dlg.Password = this.MainForm.FingerprintPassword;

            dlg.IsReader = false;
            dlg.OperLocation = this.MainForm.AppInfo.GetString(
                "default_account",
                "location",
                "");

            this.MainForm.AppInfo.LinkFormState(dlg,
                "logindlg_state");

            dlg.ShowDialog(owner);

            this.MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult == DialogResult.Cancel)
            {
                return null;
            }

            this.MainForm.FingerprintUserName = dlg.UserName;

            if (dlg.SavePasswordLong == true)
                this.MainForm.FingerprintPassword = dlg.Password;

            // server url���޸Ĳ�Ҫ����

            return dlg;
        }

        void FillReaderDbFroms()
        {
            this.comboBox_from.Items.Clear();
            this.comboBox_from.Items.Add("<ȫ��>");   // 2013/5/24
            for (int i = 0; i < this.MainForm.ReaderDbFromInfos.Length; i++)
            {
                string strCaption = this.MainForm.ReaderDbFromInfos[i].Caption;
                this.comboBox_from.Items.Add(strCaption);
            }
        }

        private void ReaderSearchForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            /*
            if (stop != null)
            {
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }

            }*/


        }

        private void ReaderSearchForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            /*
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }*/

            this.MainForm.AppInfo.SetString(
                "readersearchform",
                "readerdbname",
                this.comboBox_readerDbName.Text);

            this.MainForm.AppInfo.SetString(
                "readersearchform",
                "from",
                this.comboBox_from.Text);

            this.MainForm.AppInfo.SetString(
                "readersearchform",
                "match_style",
                this.comboBox_matchStyle.Text);

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_records);
            this.MainForm.AppInfo.SetString(
                "readersearchform",
                "record_list_column_width",
                strWidths);


        }

        /// <summary>
        /// �������е�����¼�����Ʋ�����-1 ��ʾ������
        /// </summary>
        public int MaxSearchResultCount
        {
            get
            {
                return (int)this.MainForm.AppInfo.GetInt(
                    "reader_search_form",
                    "max_result_count",
                    -1);
            }
        }

        // �Ƿ����ƶ��ķ�ʽװ������б�
        // 2008/1/20 
        /// <summary>
        /// �Ƿ����ƶ��ķ�ʽװ������б�
        /// </summary>
        public bool PushFillingBrowse
        {
            get
            {
                return MainForm.AppInfo.GetBoolean(
                    "reader_search_form",
                    "push_filling_browse",
                    false);
            }
        }

        string GetCurrentMatchStyle()
        {
            string strText = this.comboBox_matchStyle.Text;

            // 2009/8/6 
            if (strText == "��ֵ")
                return "null";

            if (String.IsNullOrEmpty(strText) == true)
                return "left"; // ȱʡʱ��Ϊ�� ǰ��һ��

            if (strText == "ǰ��һ��")
                return "left";
            if (strText == "�м�һ��")
                return "middle";
            if (strText == "��һ��")
                return "right";
            if (strText == "��ȷһ��")
                return "exact";

            return strText; // ֱ�ӷ���ԭ��
        }

        // ����
        private void toolStripButton_search_Click(object sender, EventArgs e)
        {
            string strError = "";

            {
                if (this.m_nChangedCount > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ���м�¼�б����� " + this.m_nChangedCount.ToString() + " ���޸���δ���档\r\n\r\n�Ƿ��������?\r\n\r\n(Yes �����Ȼ�����������No ��������)",
                        "ReaderSearchForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;
                }
                this.ClearListViewItems();

                ListViewUtil.ClearSortColumns(this.listView_records);
            }
            /*
            this.listView_records.Items.Clear();
            ListViewUtil.ClearSortColumns(this.listView_records);
             * */
            ClearListViewItems();

            this.label_message.Text = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� '" + this.textBox_queryWord.Text + "'...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                string strMatchStyle = GetCurrentMatchStyle();

                if (this.textBox_queryWord.Text == "")
                {
                    if (strMatchStyle == "null")
                    {
                        this.textBox_queryWord.Text = "";

                        // ר�ż�����ֵ
                        strMatchStyle = "exact";
                    }
                    else
                    {
                        // Ϊ���ڼ�����Ϊ�յ�ʱ�򣬼�����ȫ���ļ�¼
                        strMatchStyle = "left";
                    }
                }
                else
                {
                    // 2012/3/31
                    if (strMatchStyle == "null")
                    {
                        strError = "������ֵ��ʱ���뱣�ּ�����Ϊ��";
                        goto ERROR1;
                    }
                }

                long lRet = Channel.SearchReader(stop,
                    this.comboBox_readerDbName.Text,
                    this.textBox_queryWord.Text,
                    this.MaxSearchResultCount, // -1,
                    this.comboBox_from.Text,
                    strMatchStyle,  // "left",
                    this.Lang,
                    null,   // strResultSetName
                    "", // strOutputStyle
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                long lHitCount = lRet;

                this.label_message.Text = "���������� " + lHitCount.ToString() + " �����߼�¼";

                stop.SetProgressRange(0, lHitCount);

                long lStart = 0;
                long lCount = lHitCount;
                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                bool bPushFillingBrowse = this.PushFillingBrowse;

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            this.label_message.Text = "���������� " + lHitCount.ToString() + " �����߼�¼����װ�� " + lStart.ToString() + " �����û��ж�...";
                            MessageBox.Show(this, "�û��ж�");
                            return;
                        }
                    }


                    lRet = Channel.GetSearchResult(
                        stop,
                        null,   // strResultSetName
                        lStart,
                        lCount,
                        "id,cols",
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                    {
                        this.label_message.Text = "���������� " + lHitCount.ToString() + " �����߼�¼����װ�� " + lStart.ToString() + " ����" + strError;
                        goto ERROR1;
                    }

                    if (lRet == 0)
                    {
                        MessageBox.Show(this, "δ����");
                        return;
                    }

                    Debug.Assert(searchresults != null, "");
                    Debug.Assert(searchresults.Length > 0, "");

                    // ����������
                    this.listView_records.BeginUpdate();
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        if (bPushFillingBrowse == true)
                            Global.InsertNewLine(this.listView_records,
                                searchresults[i].Path,
                                searchresults[i].Cols);
                        else
                            Global.AppendNewLine(this.listView_records,
                                searchresults[i].Path,
                                searchresults[i].Cols);
                    }
                    this.listView_records.EndUpdate();

                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    stop.SetMessage("������ " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " ��");

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                    stop.SetProgressValue(lStart);

                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
                this.label_message.Text = "���������� " + lHitCount.ToString() + " �����߼�¼����ȫ��װ��";
            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        /// <summary>
        /// ��ǰ���ڲ�ѯ�����ݿ����ͣ�������ʾ��������̬
        /// </summary>
        public override string DbTypeCaption
        {
            get
            {
                Debug.Assert(this.DbType == "patron", "");
                return "����";
            }
        }

        // ���һ����¼
        //return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        internal override int GetRecord(
            string strRecPath,
            out string strXml,
            out byte[] baTimestamp,
            out string strError)
        {
            strError = "";
            strXml = "";

            string[] results = null;
            baTimestamp = null;
            string strOutputRecPath = "";
            // ��ö��߼�¼
            long lRet = Channel.GetReaderInfo(
stop,
"@path:" + strRecPath,
"xml",
out results,
out strOutputRecPath,
out baTimestamp,
out strError);

            if (lRet == 0)
                return 0;  // �Ƿ��趨Ϊ����״̬?
            if (lRet == -1)
                return -1;

            if (results == null || results.Length == 0)
            {
                strError = "results error";
                return -1;
            }

            strXml = results[0];
            return 1;
        }

        // return:
        //      -2  ʱ�����ƥ��
        //      -1  ����
        //      0   �ɹ�
        internal override int SaveRecord(string strRecPath,
            BiblioInfo info,
            out byte[] baNewTimestamp,
            out string strError)
        {
            strError = "";

            ErrorCodeValue kernel_errorcode;
            string strOutputPath = "";

            baNewTimestamp = null;
            string strExistingXml = "";
            string strSavedXml = "";
            // string strSavedPath = "";
            long lRet = Channel.SetReaderInfo(
stop,
"change",
strRecPath,
info.NewXml,
info.OldXml,
info.Timestamp,
out strExistingXml,
out strSavedXml,
out strOutputPath,
out baNewTimestamp,
out kernel_errorcode,
out strError);
            if (lRet == -1)
            {
                if (Channel.ErrorCode == ErrorCode.TimestampMismatch)
                    return -2;
                return -1;
            }

            info.Timestamp = baNewTimestamp;    // 2013/10/17

            return 0;
        }

#if NO
                public int SaveChangedRecords(List<ListViewItem> items,
            out string strError)
        {
            strError = "";

            int nReloadCount = 0;
            int nSavedCount = 0;

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ�����߼�¼ ...");
            stop.BeginLoop();

            this.EnableControls(false);
            // this.listView_records.Enabled = false;
            try
            {
                stop.SetProgressRange(0, items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    if (stop != null && stop.State != 0)
                    {
                        strError = "���ж�";
                        return -1;
                    }

                    ListViewItem item = items[i];
                    string strRecPath = item.Text;
                    if (string.IsNullOrEmpty(strRecPath) == true)
                    {
                        stop.SetProgressValue(i);
                        goto CONTINUE;
                    }

                    BiblioInfo info = (BiblioInfo)this.m_biblioTable[strRecPath];
                    if (info == null)
                        goto CONTINUE;

                    if (string.IsNullOrEmpty(info.NewXml) == true)
                        goto CONTINUE;

                    string strOutputPath = "";

                    stop.SetMessage("���ڱ�����߼�¼ " + strRecPath);

                    ErrorCodeValue kernel_errorcode;

                    byte[] baNewTimestamp = null;

                    string strExistingXml = "";
                    string strSavedXml = "";
                    // string strSavedPath = "";
                    long lRet = Channel.SetReaderInfo(
    stop,
    "change",
    strRecPath,
    info.NewXml,
    info.OldXml,
    info.Timestamp,
    out strExistingXml,
    out strSavedXml,
    out strOutputPath,
    out baNewTimestamp,
    out kernel_errorcode,
    out strError);
#if NO
                    byte[] baNewTimestamp = null;

                    long lRet = Channel.SetBiblioInfo(
                        stop,
                        "change",
                        strRecPath,
                        "xml",
                        info.NewXml,
                        info.Timestamp,
                        "",
                        out strOutputPath,
                        out baNewTimestamp,
                        out strError);
#endif
                    if (lRet == -1)
                    {
                        if (Channel.ErrorCode == ErrorCode.TimestampMismatch)
                        {
                            DialogResult result = MessageBox.Show(this,
    "������߼�¼ " + strRecPath + " ʱ����ʱ�����ƥ��: " + strError + "��\r\n\r\n�˼�¼���޷������档\r\n\r\n���������Ƿ�Ҫ˳������װ�ش˼�¼? \r\n\r\n(Yes ����װ�أ�\r\nNo ������װ�ء��������������ļ�¼����; \r\nCancel �ж������������)",
    "ReaderSearchForm",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                            if (result == System.Windows.Forms.DialogResult.Cancel)
                                break;
                            if (result == System.Windows.Forms.DialogResult.No)
                                goto CONTINUE;

                            // ����װ����Ŀ��¼�� OldXml
                            string[] results = null;
                            string strOutputRecPath = "";
                            lRet = Channel.GetReaderInfo(
    stop,
    "@path:" + strRecPath,
    "xml",
    out results,
    out strOutputRecPath,
    out baNewTimestamp,
    out strError);
#if NO
                            // byte[] baTimestamp = null;
                            lRet = Channel.GetBiblioInfos(
                                stop,
                                strRecPath,
                                "",
                                new string[] { "xml" },   // formats
                                out results,
                                out baNewTimestamp,
                                out strError);
#endif
                            if (lRet == 0)
                            {
                                // TODO: ����󣬰� item ���Ƴ���
                                return -1;
                            }
                            if (lRet == -1)
                                return -1;
                            if (results == null || results.Length == 0)
                            {
                                strError = "results error";
                                return -1;
                            }
                            info.OldXml = results[0];
                            info.Timestamp = baNewTimestamp;
                            nReloadCount++;
                            goto CONTINUE;
                        }

                        return -1;
                    }

                    info.Timestamp = baNewTimestamp;
                    info.OldXml = info.NewXml;
                    info.NewXml = "";

                    item.BackColor = SystemColors.Window;
                    item.ForeColor = SystemColors.WindowText;

                    nSavedCount++;

                    this.m_nChangedCount--;
                    Debug.Assert(this.m_nChangedCount >= 0, "");

                CONTINUE:
                    stop.SetProgressValue(i);
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControls(true);
                // this.listView_records.Enabled = true;
            }

            DoViewComment(false);

            strError = "";
            if (nSavedCount > 0)
                strError += "��������߼�¼ " + nSavedCount + " ��";
            if (nReloadCount > 0)
            {
                if (string.IsNullOrEmpty(strError) == false)
                    strError += " ; ";
                strError += "�� " + nReloadCount + " �����߼�¼��Ϊʱ�����ƥ�������װ�ؾɼ�¼����(��۲�����±���)";
            }

            return 0;
        }
#endif

        /*
        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }
         * */

        private void textBox_queryWord_Enter(object sender, EventArgs e)
        {
            // this.AcceptButton = this.button_search;
        }

        private void listView_records_Enter(object sender, EventArgs e)
        {
            // this.AcceptButton = null;
        }

        /// <summary>
        /// �Ƿ�����װ���Ѿ��򿪵���ϸ��
        /// </summary>
        public bool LoadToExistDetailWindow
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "all_search_form",
                    "load_to_exist_detailwindow",
                    true);
            }
        }

        // (���ݶ���֤�����)�Ѷ��߼�¼װ����ߴ�
        private void listView_records_DoubleClick(object sender, EventArgs e)
        {
            string strOpenStyle = "new";
            if (this.LoadToExistDetailWindow == true)
                strOpenStyle = "exist";

            /*
            LoadRecordToReaderInfoForm(strOpenStyle,
                "barcode"); // ˫�����������barcode��ʽ������Ϊ�˾����ظ��Ķ���֤�����
             * */
            LoadRecordToReaderInfoForm(strOpenStyle,
                "auto"); // ˫�����������auto��barcode��ʽ������Ϊ�˾����ظ��Ķ���֤�����
        }

        // ����¼װ�ص����ߴ�
        // parameters:
        //      strIdType   ��ʶ���� "barcode" "recpath" "auto"
        //      strOpenStyle �򿪴��ڵķ�ʽ "new" "exist"
        void LoadRecordToReaderInfoForm(string strOpenStyle,
            string strIdType)
        {

            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ����ߴ�������");
                return;
            }

            string strBarcodeOrRecPath = "";

            if (strIdType == "auto")
            {
                strBarcodeOrRecPath = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);  // this.listView_records.SelectedItems[0].SubItems[1].Text;

                // ��������Ϊ��
                if (String.IsNullOrEmpty(strBarcodeOrRecPath) == true)
                {
                    strBarcodeOrRecPath = this.listView_records.SelectedItems[0].SubItems[0].Text;
                    strIdType = "recpath";
                }
                else
                {
                    strIdType = "barcode";
                }

                Debug.Assert(strIdType != "auto", "auto���ͺ��治������");
            }
            else if (strIdType == "barcode")
                strBarcodeOrRecPath = this.listView_records.SelectedItems[0].SubItems[1].Text;
            else
            {
                Debug.Assert(strIdType == "recpath", "");
                strBarcodeOrRecPath = this.listView_records.SelectedItems[0].SubItems[0].Text;
            }

            ReaderInfoForm form = null;

            if (strOpenStyle == "exist")
            {
                form = MainForm.GetTopChildWindow<ReaderInfoForm>();
                if (form != null)
                    Global.Activate(form);
            }

            if (form == null)
            {
                form = new ReaderInfoForm();

                form.MdiParent = this.MainForm;

                form.MainForm = this.MainForm;
                form.Show();
            }

            if (strIdType == "barcode")
            {
                form.LoadRecord(strBarcodeOrRecPath, false); // �����ظ�����ʱ����ǿ��װ�룬�𵽾��湤����Ա������
                // form.AsyncLoadRecord(strBarcodeOrRecPath);
            }
            else
            {
                Debug.Assert(strIdType == "recpath", "");

                // form.LoadRecord("@path:" + strRecPath, false);   // ����취�����⣬ReaderInfoForm.ReaderBarcode����
                form.LoadRecordByRecPath(strBarcodeOrRecPath, "");
            }
        }

        // ����¼װ�ص����Ѵ�
        // parameters:
        //      strIdType   ��ʶ���� "barcode" "recpath"
        //      strOpenStyle �򿪴��ڵķ�ʽ "new" "exist"
        void LoadRecordToAmerceForm(string strOpenStyle,
            string strIdType)
        {

            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ�뽻�Ѵ�������");
                return;
            }

            string strBarcodeOrRecPath = "";

            if (strIdType == "barcode")
                strBarcodeOrRecPath = this.listView_records.SelectedItems[0].SubItems[1].Text;
            else
            {
                Debug.Assert(strIdType == "recpath", "");
                strBarcodeOrRecPath = this.listView_records.SelectedItems[0].SubItems[0].Text;
            }

            AmerceForm form = null;

            if (strOpenStyle == "exist")
            {
                form = MainForm.GetTopChildWindow<AmerceForm>();
                if (form != null)
                    Global.Activate(form);
            }

            if (form == null)
            {
                form = new AmerceForm();

                form.MdiParent = this.MainForm;

                form.MainForm = this.MainForm;
                form.Show();
            }

            if (strIdType == "barcode")
                form.LoadReader(strBarcodeOrRecPath, false); // �����ظ�����ʱ����ǿ��װ�룬�𵽾��湤����Ա������
            else
            {
                Debug.Assert(strIdType == "recpath", "");

                Debug.Assert(false, "Ŀǰ��δ֧��");
                form.LoadReader("@path:" + strBarcodeOrRecPath, false);   // �պϵģ���δ����
                // form.LoadRecordByRecPath(strBarcodeOrRecPath, "");
            }
        }


        // װ����ߴ� 1
        void menu_recPath_newly_Click(object sender, EventArgs e)
        {
            LoadRecordToReaderInfoForm("new",
                "recpath");
        }

        // 2
        void menu_barcode_newly_Click(object sender, EventArgs e)
        {
            LoadRecordToReaderInfoForm("new",
                "barcode");
        }

        // 3
        void menu_recPath_exist_Click(object sender, EventArgs e)
        {
            LoadRecordToReaderInfoForm("exist",
                "recpath");
        }

        // 4
        void menu_barcode_exist_Click(object sender, EventArgs e)
        {
            LoadRecordToReaderInfoForm("exist",
                "barcode");
        }

        // װ�뽻�Ѵ� 1
        void menu_amerce_by_barcode_newly_Click(object sender, EventArgs e)
        {
            LoadRecordToAmerceForm("new",
                "barcode");
        }

        // װ�뽻�Ѵ� 2
        void menu_amerce_by_barcode_exist_Click(object sender, EventArgs e)
        {
            LoadRecordToAmerceForm("exist",
                "barcode");
        }

        // ע:������listview
        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.toolStrip_search.Enabled = bEnable;
            this.comboBox_readerDbName.Enabled = bEnable;
            this.comboBox_from.Enabled = bEnable;
            this.comboBox_matchStyle.Enabled = bEnable;

            if (this.comboBox_matchStyle.Text == "��ֵ")
                this.textBox_queryWord.Enabled = false;
            else
                this.textBox_queryWord.Enabled = bEnable;
        }

        private void ReaderSearchForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            // this.MainForm.MenuItem_font.Enabled = false;
        }

        private void comboBox_readerDbName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_readerDbName.Items.Count > 0)
                return;

            this.comboBox_readerDbName.Items.Add("<ȫ��>");

            if (this.MainForm.ReaderDbNames != null)    // 2009/3/29 
            {
                for (int i = 0; i < this.MainForm.ReaderDbNames.Length; i++)
                {
                    this.comboBox_readerDbName.Items.Add(this.MainForm.ReaderDbNames[i]);
                }
            }
        }

        private void listView_records_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            ListViewItem selected_item = null;
            
            string strBarcode = "";
            string strRecPath = "";

            if (this.listView_records.SelectedItems.Count > 0)
            {
                selected_item = this.listView_records.SelectedItems[0];
                strBarcode = ListViewUtil.GetItemText(selected_item, 1);
                strRecPath = ListViewUtil.GetItemText(selected_item, 0);
            }

            string strOpenStyle = "�¿���";
            if (this.LoadToExistDetailWindow == true)
                strOpenStyle = "�Ѵ򿪵�";


            menuItem = new MenuItem("�� [����֤����� '" + strBarcode + "' װ��" + strOpenStyle + "���ߴ�] (&O)");
            menuItem.DefaultItem = true;
            menuItem.Click += new System.EventHandler(this.listView_records_DoubleClick);
            if (String.IsNullOrEmpty(strBarcode) == true)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("�򿪷�ʽ(&T)");
            contextMenu.MenuItems.Add(menuItem);

            // �Ӳ˵�

            // *** ���ߴ�
            strOpenStyle = "�¿���";

            // ��¼·��
            MenuItem subMenuItem = new MenuItem("װ��" + strOpenStyle + "���ߴ������ݼ�¼·�� '" + strRecPath + "'");
            subMenuItem.Click += new System.EventHandler(this.menu_recPath_newly_Click);
            if (String.IsNullOrEmpty(strRecPath) == true)
                subMenuItem.Enabled = false;
            menuItem.MenuItems.Add(subMenuItem);

            // ����
            subMenuItem = new MenuItem("װ��" + strOpenStyle + "���ߴ�������֤����� '" + strBarcode + "'");
            subMenuItem.Click += new System.EventHandler(this.menu_barcode_newly_Click);
            if (String.IsNullOrEmpty(strBarcode) == true)
                subMenuItem.Enabled = false;
            menuItem.MenuItems.Add(subMenuItem);

            strOpenStyle = "�Ѵ򿪵�";

            bool bHasOpendReaderInfoForm = (this.MainForm.GetTopChildWindow<ReaderInfoForm>() != null);

            // ��¼·��
            subMenuItem = new MenuItem("װ��" + strOpenStyle + "���ߴ������ݼ�¼·�� '" + strRecPath + "'");
            subMenuItem.Click += new System.EventHandler(this.menu_recPath_exist_Click);
            if (String.IsNullOrEmpty(strRecPath) == true
                || bHasOpendReaderInfoForm == false)
                subMenuItem.Enabled = false;
            menuItem.MenuItems.Add(subMenuItem);

            // ����
            subMenuItem = new MenuItem("װ��" + strOpenStyle + "���ߴ�������֤����� '" + strBarcode + "'");
            subMenuItem.Click += new System.EventHandler(this.menu_barcode_exist_Click);
            if (String.IsNullOrEmpty(strBarcode) == true
                || bHasOpendReaderInfoForm == false)
                subMenuItem.Enabled = false;
            menuItem.MenuItems.Add(subMenuItem);

            /*
            menuItem = new MenuItem("�� [���ݼ�¼·�� '"+strRecPath+"' װ�뵽���ߴ�] (&P)");
            menuItem.Click += new System.EventHandler(this.menu_loadReaderInfoByRecPath_Click);
            if (String.IsNullOrEmpty(strRecPath) == true)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);
             * */

            // ---
            subMenuItem = new MenuItem("-");
            menuItem.MenuItems.Add(subMenuItem);


            // *** ���Ѵ�
            strOpenStyle = "�¿���";

            // ����
            subMenuItem = new MenuItem("װ��" + strOpenStyle + "���Ѵ�������֤����� '" + strBarcode + "'");
            subMenuItem.Click += new System.EventHandler(this.menu_amerce_by_barcode_newly_Click);
            if (String.IsNullOrEmpty(strBarcode) == true)
                subMenuItem.Enabled = false;
            menuItem.MenuItems.Add(subMenuItem);

            strOpenStyle = "�Ѵ򿪵�";

            // ����
            subMenuItem = new MenuItem("װ��" + strOpenStyle + "���Ѵ�������֤����� '" + strBarcode + "'");
            subMenuItem.Click += new System.EventHandler(this.menu_amerce_by_barcode_exist_Click);
            if (String.IsNullOrEmpty(strBarcode) == true
                || this.MainForm.GetTopChildWindow<AmerceForm>() == null)
                subMenuItem.Enabled = false;
            menuItem.MenuItems.Add(subMenuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_cutToClipboard_Click);
            if (this.listView_records.SelectedIndices.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("����(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copyToClipboard_Click);
            if (this.listView_records.SelectedIndices.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("���Ƶ���(&S)");
            // menuItem.Click += new System.EventHandler(this.menu_copySingleColumnToClipboard_Click);
            if (this.listView_records.SelectedIndices.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            for (int i = 0; i < this.listView_records.Columns.Count; i++)
            {
                subMenuItem = new MenuItem("������ '" + this.listView_records.Columns[i].Text + "'");
                subMenuItem.Tag = i;
                subMenuItem.Click += new System.EventHandler(this.menu_copySingleColumnToClipboard_Click);
                menuItem.MenuItems.Add(subMenuItem);
            }

            bool bHasClipboardObject = false;
            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null
                || iData.GetDataPresent(typeof(string)) == false)
                bHasClipboardObject = false;
            else
                bHasClipboardObject = true;

            menuItem = new MenuItem("ճ��[ǰ��](&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteFromClipboard_insertBefore_Click);
            if (bHasClipboardObject == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ճ��[���](&V)");
            menuItem.Click += new System.EventHandler(this.menu_pasteFromClipboard_insertAfter_Click);
            if (bHasClipboardObject == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Click += new System.EventHandler(this.menu_selectAllLines_Click);
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            // ������
            // ���ڼ�����ʱ�򣬲���������������������Ϊstop.BeginLoop()Ƕ�׺��Min Max Value֮��ı���ָ����⻹û�н��
            {
                menuItem = new MenuItem("������(&B)");
                contextMenu.MenuItems.Add(menuItem);

                subMenuItem = new MenuItem("�����޸Ķ��߼�¼ [" + this.listView_records.SelectedItems.Count.ToString() + "] (&Q)");
                subMenuItem.Click += new System.EventHandler(this.menu_quickChangeRecords_Click);
                if (this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("ִ�� C# �ű� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&M)");
                subMenuItem.Click += new System.EventHandler(this.menu_quickMarcQueryRecords_Click);
                if (this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("�����޸� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&C)");
                subMenuItem.Click += new System.EventHandler(this.menu_clearSelectedChangedRecords_Click);
                if (this.m_nChangedCount == 0 || this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("����ȫ���޸� [" + this.m_nChangedCount.ToString() + "] (&L)");
                subMenuItem.Click += new System.EventHandler(this.menu_clearAllChangedRecords_Click);
                if (this.m_nChangedCount == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("����ѡ�����޸� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&S)");
                subMenuItem.Click += new System.EventHandler(this.menu_saveSelectedChangedRecords_Click);
                if (this.m_nChangedCount == 0 || this.listView_records.SelectedItems.Count == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("����ȫ���޸� [" + this.m_nChangedCount.ToString() + "] (&A)");
                subMenuItem.Click += new System.EventHandler(this.menu_saveAllChangedRecords_Click);
                if (this.m_nChangedCount == 0 || this.InSearching == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("�����µ� C# �ű��ļ� (&C)");
                subMenuItem.Click += new System.EventHandler(this.menu_createMarcQueryCsFile_Click);
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                subMenuItem = new MenuItem("�ƶ���ѡ��� " + this.listView_records.SelectedItems.Count.ToString() + " �����߼�¼(&M)");
                subMenuItem.Click += new System.EventHandler(this.menu_moveRecords_Click);
                if (this.listView_records.SelectedItems.Count == 0)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);
            }

#if NO
            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�����޸���ѡ��� " + this.listView_records.SelectedItems.Count.ToString() + " �����߼�¼(&Q)");
            menuItem.Click += new System.EventHandler(this.menu_quickChangeRecords_Click);
            if (this.listView_records.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);
#endif

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("������������ļ� [" + this.listView_records.SelectedItems.Count.ToString() + " ] (&B)");
            menuItem.Click += new System.EventHandler(this.menu_exportBarcodeFile_Click);
            if (this.listView_records.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("��������¼·���ļ� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&P)");
            menuItem.Click += new System.EventHandler(this.menu_exportRecPathFile_Click);
            if (this.listView_records.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�����������鵽 Excel �ļ� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&D)");
            menuItem.Click += new System.EventHandler(this.menu_exportReaderInfoToExcelFile_Click);
            if (this.listView_records.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�Ӽ�¼·���ļ��е���(&I)...");
            menuItem.Click += new System.EventHandler(this.menu_importFromRecPathFile_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("��������ļ��е���(&R)...");
            menuItem.Click += new System.EventHandler(this.menu_importFromBarcodeFile_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ˢ�� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&R)");
            menuItem.Click += new System.EventHandler(this.menu_refreshSelectedItems_Click);
            if (this.listView_records.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView_records, new Point(e.X, e.Y));		
        }

        // ˢ����ѡ����С�Ҳ�������´����ݿ���װ�������
        void menu_refreshSelectedItems_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫˢ�µ������";
                goto ERROR1;
            }

            int nChangedCount = 0;
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                if (string.IsNullOrEmpty(item.Text) == true)
                    continue;
                items.Add(item);

                if (IsItemChanged(item) == true)
                    nChangedCount++;
            }

            // ����δ��������ݻᶪʧ
            if (nChangedCount > 0)
            {
                DialogResult result = MessageBox.Show(this,
    "Ҫˢ�µ� " + this.listView_records.SelectedItems.Count.ToString()+ " ���������� " + nChangedCount.ToString() + " ���޸ĺ���δ���档���ˢ�����ǣ��޸����ݻᶪʧ��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
    "ReaderSearchForm",
    MessageBoxButtons.OKCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return;
            }

            nRet = RefreshListViewLines(items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            DoViewComment(false);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
            RrefreshSelectedItems();
        }



#if NO
        // ˢ����ѡ����С�Ҳ�������´����ݿ���װ�������
        void menu_refreshSelectedItems_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫˢ������е�����";
                goto ERROR1;
            }

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ˢ������� ...");
            stop.BeginLoop();

            this.EnableControls(false);
            try
            {
                if (stop != null)
                    stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);
                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null
                        && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    if (string.IsNullOrEmpty(item.Text) == true)
                        continue;

                    if (stop != null)
                    {
                        stop.SetMessage("����ˢ������� " + item.Text + " ...");
                        stop.SetProgressValue(i++);
                    }
                    nRet = RefreshBrowseLine(item,
    out strError);
                    if (nRet == -1)
                    {
                        DialogResult result = MessageBox.Show(this,
                            "ˢ���������ʱ����: " + strError + "��\r\n\r\n�Ƿ����ˢ��? (OK ˢ�£�Cancel ����ˢ��)",
                            "ReaderSearchForm",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.Cancel)
                            break;
                    }
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControls(true);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

#endif

        void menu_importFromBarcodeFile_Click(object sender, EventArgs e)
        {
            SetStatusMessage("");   // �����ǰ��������ʾ

            // bool bSkipBrowe = false;
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵�������ļ���";
            dlg.FileName = this.m_strUsedBarcodeFilename;
            dlg.Filter = "������ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.m_strUsedBarcodeFilename = dlg.FileName;

            StreamReader sr = null;
            string strError = "";

            try
            {
                // TODO: ����Զ�̽���ļ��ı��뷽ʽ?
                sr = new StreamReader(dlg.FileName, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                strError = "���ļ� " + dlg.FileName + " ʧ��: " + ex.Message;
                goto ERROR1;
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ�������� ...");
            stop.BeginLoop();


            try
            {
                // �����������û����ģ������Ҫ������е������־
                ListViewUtil.ClearSortColumns(this.listView_records);


                if (this.listView_records.Items.Count > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "����ǰ�Ƿ�Ҫ������м�¼�б��е����е� " + this.listView_records.Items.Count.ToString() + " ��?\r\n\r\n(�������������µ�����н�׷���������к���)\r\n(Yes �����No �����(׷��)��Cancel ��������)",
                        "ReaderSearchForm",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.Cancel)
                        return;
                    if (result == DialogResult.Yes)
                    {
                        ClearListViewItems();
                    }
                }

                stop.SetProgressRange(0, sr.BaseStream.Length);

                List<ListViewItem> items = new List<ListViewItem>();

                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            MessageBox.Show(this, "�û��ж�");
                            return;
                        }
                    }

                    string strBarcode = sr.ReadLine();

                    stop.SetProgressValue(sr.BaseStream.Position);


                    if (strBarcode == null)
                        break;

                    // 


                    ListViewItem item = new ListViewItem();
                    item.Text = "";
                    ListViewUtil.ChangeItemText(item, 1, strBarcode);

                    this.listView_records.Items.Add(item);

                    // if (FillLineByBarcode(strBarcode, item, ref bSkipBrowe) == true)
                    //     break;
                    FillLineByBarcode(strBarcode, item);

                    items.Add(item);
                }

                // ˢ�������
                int nRet = RefreshListViewLines(items,
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                if (sr != null)
                    sr.Close();
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// �������ĩβ�¼���һ��
        /// </summary>
        /// <param name="strBarcode">֤�����</param>
        /// <returns>�´����� ListViewItem ����</returns>
        public ListViewItem AddBarcodeToBrowseList(string strBarcode)
        {
            ListViewItem item = new ListViewItem();

            // TODO: ��Ҫ����Ϊ�����ж���֪���к�
            ListViewUtil.ChangeItemText(item, 1, strBarcode);
            FillLineByBarcode(strBarcode, item);

            this._listviewRecords.Items.Add(item);
            return item;
        }

        bool FillLineByBarcode(string strBarcode,
    ListViewItem item)
        {
            string strError = "";
            string strReaderRecPath = "";

            // ����������ţ����������������Ŀ��¼·����
            int nRet = SearchRecPathByBarcode(strBarcode,
            out strReaderRecPath,
            out strError);
            if (nRet == -1)
            {
                ListViewUtil.ChangeItemText(item, 2, strError);
            }
            else if (nRet == 0)
            {
                ListViewUtil.ChangeItemText(item, 2, "����� '" + strBarcode + "' û���ҵ���¼");
            }
            else if (nRet == 1)
            {
                item.Text = strReaderRecPath;
            }
            else if (nRet > 1) // ���з����ظ�
            {
                ListViewUtil.ChangeItemText(item, 2, "����� '" + strBarcode + "' ���� " + nRet.ToString() + " ����¼������һ�����ش���");
                return false;
            }

            return true;
        }

#if NO
        // return:
        //      true    Ҫ�ж�
        //      false   ���ж�
        bool FillLineByBarcode(string strBarcode,
            ListViewItem item,
            ref bool bSkipBrowse)
        {
            string strError = "";
            string strReaderRecPath = "";


            // ����������ţ����������������Ŀ��¼·����
            int nRet = SearchRecPathByBarcode(strBarcode,
            out strReaderRecPath,
            out strError);
            if (nRet == -1)
            {
                ListViewUtil.ChangeItemText(item, 2, strError);
            }
            else if (nRet == 0)
            {
                ListViewUtil.ChangeItemText(item, 2, "����� '" + strBarcode + "' û���ҵ���¼");
            }
            else if (nRet == 1)
            {
                item.Text = strReaderRecPath;

                if (bSkipBrowse == false
    && !(Control.ModifierKeys == Keys.Control))
                {
                    nRet = RefreshBrowseLine(item,
            out strError);
                    if (nRet == -1)
                    {
                        DialogResult result = MessageBox.Show(this,
    "����������ʱ����: " + strError + "��\r\n\r\n�Ƿ������ȡ�������? (Yes ��ȡ��No ����ȡ��Cancel ��������)",
    "ReaderSearchForm",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.No)
                            bSkipBrowse = true;
                        if (result == System.Windows.Forms.DialogResult.Cancel)
                        {
                            return true;
                        }
                    }
                }

            }
            else if (nRet > 1) // ���з����ظ�
            {
                ListViewUtil.ChangeItemText(item, 2, "����� '" + strBarcode + "' ���� " + nRet.ToString() + " ����¼������һ�����ش���");
            }

            return false;
        }

#endif

        // ���ݶ���֤����ţ�����������¼·��
        int SearchRecPathByBarcode(string strBarcode,
            out string strReaderRecPath,
            out string strError)
        {
            strError = "";
            strReaderRecPath = "";

            try
            {
                byte[] baTimestamp = null;

                string[] results = null;
                long lRet = Channel.GetReaderInfo(
                    stop,
                    strBarcode,
                    "",
                    out results,
                    out strReaderRecPath,
                    out baTimestamp,
                    out strError);
                if (lRet == -1)
                    return -1;

                return (int)lRet;   // 
            }
            finally
            {
            }
        }

        // �Ӽ�¼·���ļ��е���
        void menu_importFromRecPathFile_Click(object sender, EventArgs e)
        {
            SetStatusMessage("");   // �����ǰ��������ʾ

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵Ķ��߼�¼·���ļ���";
            dlg.FileName = this.m_strUsedRecPathFilename;
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.m_strUsedRecPathFilename = dlg.FileName;

            StreamReader sr = null;
            string strError = "";
            // bool bSkipBrowse = false;

            try
            {
                // TODO: ����Զ�̽���ļ��ı��뷽ʽ?
                sr = new StreamReader(dlg.FileName, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                strError = "���ļ� " + dlg.FileName + " ʧ��: " + ex.Message;
                goto ERROR1;
            }

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڵ����¼·�� ...");
            stop.BeginLoop();

            this.EnableControls(false);
            try
            {
                // �����������û����ģ������Ҫ������е������־
                ListViewUtil.ClearSortColumns(this.listView_records);
                stop.SetProgressRange(0, sr.BaseStream.Length);


                if (this.listView_records.Items.Count > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "����ǰ�Ƿ�Ҫ������м�¼�б��е����е� " + this.listView_records.Items.Count.ToString() + " ��?\r\n\r\n(�������������µ�����н�׷���������к���)\r\n(Yes �����No �����(׷��)��Cancel ��������)",
                        "ReaderSearchForm",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.Cancel)
                        return;
                    if (result == DialogResult.Yes)
                    {
                        ClearListViewItems();
                    }
                }

                List<ListViewItem> items = new List<ListViewItem>();
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            MessageBox.Show(this, "�û��ж�");
                            return;
                        }
                    }

                    string strRecPath = sr.ReadLine();

                    stop.SetProgressValue(sr.BaseStream.Position);

                    if (strRecPath == null)
                        break;

                    // ���·������ȷ�ԣ�������ݿ��Ƿ�Ϊ���߿�֮һ
                    string strDbName = Global.GetDbName(strRecPath);
                    if (string.IsNullOrEmpty(strDbName) == true)
                    {
                        strError = "'" + strRecPath + "' ���ǺϷ��ļ�¼·��";
                        goto ERROR1;
                    }

                    if (this.MainForm.IsReaderDbName(strDbName) == false)
                    {
                        strError = "·�� '" + strRecPath + "' �е����ݿ��� '" + strDbName + "' ���ǺϷ��Ķ��߿������ܿ�����ָ�����ļ����Ƕ��߿�ļ�¼·���ļ�";
                        goto ERROR1;
                    }

                    ListViewItem item = new ListViewItem();
                    item.Text = strRecPath;

                    this.listView_records.Items.Add(item);

#if NO
                    if (bSkipBrowse == false
                        && !(Control.ModifierKeys == Keys.Control))
                    {
                        int nRet = RefreshBrowseLine(item,
                out strError);
                        if (nRet == -1)
                        {
                            DialogResult result = MessageBox.Show(this,
        "����������ʱ����: " + strError + "��\r\n\r\n�Ƿ������ȡ�������? (Yes ��ȡ��No ����ȡ��Cancel ��������)",
        "ReaderSearchForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                            if (result == System.Windows.Forms.DialogResult.No)
                                bSkipBrowse = true;
                            if (result == System.Windows.Forms.DialogResult.Cancel)
                            {
                                strError = "���ж�";
                                break;
                            }
                        }
                    }
#endif
                    items.Add(item);

                }

                int nRet = RefreshListViewLines(items,
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControls(true);

                if (sr != null)
                    sr.Close();
            }

            DoViewComment(false);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }



#if NO
        // ����ǰ����¼·�����Ѿ���ֵ
        public int RefreshBrowseLine(ListViewItem item,
            out string strError)
        {
            strError = "";

            string strRecPath = ListViewUtil.GetItemText(item, 0);
            string[] paths = new string[1];
            paths[0] = strRecPath;
            DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

            long lRet = this.Channel.GetBrowseRecords(
                this.stop,
                paths,
                "id,cols",
                out searchresults,
                out strError);
            if (lRet == -1)
                return -1;

            if (searchresults == null || searchresults.Length == 0)
            {
                strError = "searchresults == null || searchresults.Length == 0";
                return -1;
            }

            for (int i = 0; i < searchresults[0].Cols.Length; i++)
            {
                ListViewUtil.ChangeItemText(item,
                    i + 1,
                    searchresults[0].Cols[i]);
            }

            return 0;
        }
#endif

        void ClearListViewItems()
        {
            this.listView_records.Items.Clear();

            ListViewUtil.ClearSortColumns(this.listView_records);

            // ���������Ҫȷ����������
            for (int i = 1; i < this.listView_records.Columns.Count; i++)
            {
                this.listView_records.Columns[i].Text = i.ToString();
            }

#if NO
            this.m_biblioTable = new Hashtable();
            this.m_nChangedCount = 0;

            if (this.m_commentViewer != null)
                this.m_commentViewer.Clear();
#endif
            ClearBiblioTable();
            ClearCommentViewer();
        }

        void menu_selectAllLines_Click(object sender, EventArgs e)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                this.listView_records.SelectedIndexChanged -= new System.EventHandler(this.listView_records_SelectedIndexChanged);

                ListViewUtil.SelectAllLines(this.listView_records);

                this.listView_records.SelectedIndexChanged += new System.EventHandler(this.listView_records_SelectedIndexChanged);
                listView_records_SelectedIndexChanged(null, null);
            }
            finally
            {
                this.Cursor = oldCursor;
            }
        }

        void menu_copyToClipboard_Click(object sender, EventArgs e)
        {
            Global.CopyLinesToClipboard(this, this.listView_records, false);
        }

        void menu_copySingleColumnToClipboard_Click(object sender, EventArgs e)
        {
            int nColumn = (int)((MenuItem)sender).Tag;

            Global.CopyLinesToClipboard(this, nColumn, this.listView_records, false);
        }

        void menu_cutToClipboard_Click(object sender, EventArgs e)
        {
            this.listView_records.SelectedIndexChanged -= new System.EventHandler(this.listView_records_SelectedIndexChanged);

            Global.CopyLinesToClipboard(this, this.listView_records, true);

            this.listView_records.SelectedIndexChanged += new System.EventHandler(this.listView_records_SelectedIndexChanged);
            listView_records_SelectedIndexChanged(null, null);
        }

        void menu_pasteFromClipboard_insertBefore_Click(object sender, EventArgs e)
        {
            this.listView_records.SelectedIndexChanged -= new System.EventHandler(this.listView_records_SelectedIndexChanged);

            Global.PasteLinesFromClipboard(this, this.listView_records, true);

            this.listView_records.SelectedIndexChanged += new System.EventHandler(this.listView_records_SelectedIndexChanged);
            listView_records_SelectedIndexChanged(null, null);

        }

        void menu_pasteFromClipboard_insertAfter_Click(object sender, EventArgs e)
        {
            this.listView_records.SelectedIndexChanged -= new System.EventHandler(this.listView_records_SelectedIndexChanged);

            Global.PasteLinesFromClipboard(this, this.listView_records, false);

            this.listView_records.SelectedIndexChanged += new System.EventHandler(this.listView_records_SelectedIndexChanged);
            listView_records_SelectedIndexChanged(null, null);
        }

        // (���ݼ�¼·��)װ�뵽���ߴ�
        void menu_loadReaderInfoByRecPath_Click(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ����ߴ�������");
                return;
            }
            string strRecPath = this.listView_records.SelectedItems[0].SubItems[0].Text;

            ReaderInfoForm form = new ReaderInfoForm();

            form.MdiParent = this.MainForm;

            form.MainForm = this.MainForm;
            form.Show();

            // form.LoadRecord("@path:" + strRecPath, false);   // ����취�����⣬ReaderInfoForm.ReaderBarcode����

            form.LoadRecordByRecPath(strRecPath, "");
        }

#if NO
        // �����޸ļ�¼
        void menu_quickChangeRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;
            bool bSkipUpdateBrowse = false; // �Ƿ�Ҫ�������������

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ�����޸ĵĶ��߼�¼����";
                goto ERROR1;
            }

            ChangeReaderActionDialog dlg = new ChangeReaderActionDialog();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.Text = "�����޸Ķ��߼�¼ -- ��ָ����������";
            dlg.MainForm = this.MainForm;
            dlg.GetValueTable -= new GetValueTableEventHandler(dlg_GetValueTable);
            dlg.GetValueTable += new GetValueTableEventHandler(dlg_GetValueTable);

            this.MainForm.AppInfo.LinkFormState(dlg, "readersearchform_quickchangedialog_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            DateTime now = DateTime.Now;

            // TODO: ���һ�£������Ƿ�һ���޸Ķ�����û��

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����޸Ķ��߼�¼ ...");
            stop.BeginLoop();

            EnableControls(false);
            this.listView_records.Enabled = false;

            int nProcessCount = 0;
            int nChangedCount = 0;
            try
            {
                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            MessageBox.Show(this, "�û��ж�");
                            return;
                        }
                    }

                    string strRecPath = item.Text;

                    if (string.IsNullOrEmpty(strRecPath) == true)
                    {
                        // Debug.Assert(false, "");
                        continue;
                    }

                REDO_CHANGE:
                    // ��ö��߼�¼
                    byte[] baTimestamp = null;
                    string strOutputRecPath = "";

                    stop.SetMessage("����װ����߼�¼ " + strRecPath + " ...");

                    string[] results = null;
                    long lRet = Channel.GetReaderInfo(
                        stop,
                        "@path:" + strRecPath,
                        "xml",
                        out results,
                        out strOutputRecPath,
                        out baTimestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        goto ERROR1;
                    }

                    if (lRet == 0)
                    {
                        goto ERROR1;
                    }

                    if (lRet > 1)   // �����ܷ�����?
                    {
                        strError = "��¼·�� " + strRecPath + " ���м�¼ " + lRet.ToString() + " ��������װ����߼�¼��\r\n\r\nע������һ�����ش�����ϵͳ����Ա�����ų���";
                        goto ERROR1;
                    }
                    if (results == null || results.Length < 1)
                    {
                        strError = "���ص�results��������";
                        goto ERROR1;
                    }
                    string strXml = results[0];

                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "װ��XML��DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    // �޸�һ�����߼�¼XmlDocument
                    // return:
                    //      -1  ����
                    //      0   û��ʵ�����޸�
                    //      1   �������޸�
                    nRet = ModifyRecord(ref dom,
                        now,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    nProcessCount++;

                    if (nRet == 0)
                        continue;

                    Debug.Assert(nRet == 1, "");


                    ErrorCodeValue kernel_errorcode;

                    byte[] baNewTimestamp = null;
                    string strExistingXml = "";
                    string strSavedXml = "";
                    string strSavedPath = "";
                    lRet = Channel.SetReaderInfo(
    stop,
    "change",
    strRecPath,
    dom.OuterXml,
    strXml,
    baTimestamp,
    out strExistingXml,
    out strSavedXml,
    out strSavedPath,
    out baNewTimestamp,
    out kernel_errorcode,
    out strError);
                    if (lRet == -1)
                    {
                        DialogResult result = MessageBox.Show(this,
"������߼�¼ '" + strRecPath + "' ʱ����: " + strError + "��\r\n\r\n�Ƿ����Ա���? (Yes ���ԣ�No ���Դ�����¼�ı��棬���Ǽ��账�����ļ�¼��Cancel �ж����޸Ĳ���)",
"ReaderSearchForm",
MessageBoxButtons.YesNoCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                        if (result == System.Windows.Forms.DialogResult.Yes)
                            goto REDO_CHANGE;

                        if (result == System.Windows.Forms.DialogResult.No)
                            continue;
                        if (result == System.Windows.Forms.DialogResult.Cancel)
                            goto ERROR1;
                    }

                    // ˢ�������
                    if (bSkipUpdateBrowse == false)
                    {
                        nRet = RefreshBrowseLine(item,
    out strError);
                        if (nRet == -1)
                        {
                            DialogResult result = MessageBox.Show(this,
        "ˢ���������ʱ����: " + strError + "��\r\n\r\n�����Ƿ����(���޸Ĳ�����)ˢ���������? (Yes ��ȡ��No ����ȡ��Cancel �ж����޸Ĳ���)",
        "ReaderSearchForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                            if (result == System.Windows.Forms.DialogResult.No)
                                bSkipUpdateBrowse = true;
                            if (result == System.Windows.Forms.DialogResult.Cancel)
                                break;
                        }
                    }

                    stop.SetProgressValue(++i);
                    nChangedCount++;
                }
            }
            finally
            {
                EnableControls(true);
                this.listView_records.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;
            }
            MessageBox.Show(this, "�ɹ��޸Ķ��߼�¼ " + nChangedCount.ToString() + " �� (������ "+nProcessCount.ToString()+" ��)");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif
        // �����޸ļ�¼
        void menu_quickChangeRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            nRet = QuickChangeItemRecords(out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet != 0)
                MessageBox.Show(this, strError);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

#if NO
        // �����޸ļ�¼
        void menu_quickChangeRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;
            // bool bSkipUpdateBrowse = false; // �Ƿ�Ҫ�������������

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ�����޸ĵĶ��߼�¼����";
                goto ERROR1;
            }

            ChangeReaderActionDialog dlg = new ChangeReaderActionDialog();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.Text = "�����޸Ķ��߼�¼ -- ��ָ����������";
            dlg.MainForm = this.MainForm;
            dlg.GetValueTable -= new GetValueTableEventHandler(dlg_GetValueTable);
            dlg.GetValueTable += new GetValueTableEventHandler(dlg_GetValueTable);

            this.MainForm.AppInfo.LinkFormState(dlg, "readersearchform_quickchangedialog_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            DateTime now = DateTime.Now;

            // TODO: ���һ�£������Ƿ�һ���޸Ķ�����û��
            this.MainForm.OperHistory.AppendHtml("<div class='debug begin'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ��ʼִ�п����޸Ķ��߼�¼</div>");

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����޸Ķ��߼�¼ ...");
            stop.BeginLoop();

            EnableControls(false);
            this.listView_records.Enabled = false;

            int nProcessCount = 0;
            int nChangedCount = 0;
            try
            {
                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                List<ListViewItem> items = new List<ListViewItem>();
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    if (string.IsNullOrEmpty(item.Text) == true)
                        continue;

                    items.Add(item);
                }

                ListViewPatronLoader loader = new ListViewPatronLoader(this.Channel,
                    stop,
                    items,
                    this.m_biblioTable);

                int i = 0;
                foreach (LoaderItem item in loader)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null
                        && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    stop.SetProgressValue(i);

                    BiblioInfo info = item.BiblioInfo;

                    this.MainForm.OperHistory.AppendHtml("<div class='debug recpath'>" + HttpUtility.HtmlEncode(info.RecPath) + "</div>");

                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(info.OldXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "װ��XML��DOMʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    string strDebugInfo = "";
                    // �޸�һ�����߼�¼XmlDocument
                    // return:
                    //      -1  ����
                    //      0   û��ʵ�����޸�
                    //      1   �������޸�
                    nRet = ModifyRecord(ref dom,
                        now,
                        out strDebugInfo,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    this.MainForm.OperHistory.AppendHtml("<div class='debug normal'>" + HttpUtility.HtmlEncode(strDebugInfo).Replace("\r\n", "<br/>") + "</div>");

                    nProcessCount++;

                    if (nRet == 1)
                    {
                        string strXml = dom.OuterXml;
                        if (info != null)
                        {
                            if (string.IsNullOrEmpty(info.NewXml) == true)
                                this.m_nChangedCount++;
                            info.NewXml = strXml;
                        }

                        item.ListViewItem.BackColor = SystemColors.Info;
                        item.ListViewItem.ForeColor = SystemColors.InfoText;
                    }

                    i++;
                    nChangedCount++;
                }
            }
            finally
            {
                EnableControls(true);
                this.listView_records.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.MainForm.OperHistory.AppendHtml("<div class='debug end'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ���������޸Ķ��߼�¼</div>");
            }

            DoViewComment(false);
            MessageBox.Show(this, "�޸Ķ��߼�¼ " + nChangedCount.ToString() + " �� (������ " + nProcessCount.ToString() + " ��)\r\n\r\n(ע���޸Ĳ�δ�Զ����档���ڹ۲�ȷ�Ϻ�ʹ�ñ�������޸ı���ض��߿�)");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

#endif


#if NO
        void dlg_GetValueTable(object sender, GetValueTableEventArgs e)
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
#endif

#if NO
        // �޸�һ�����߼�¼XmlDocument
        // return:
        //      -1  ����
        //      0   û��ʵ�����޸�
        //      1   �������޸�
        int ModifyRecord(ref XmlDocument dom,
            DateTime now,
            out string strDebugInfo,
            out string strError)
        {
            strError = "";
            strDebugInfo = "";

            bool bChanged = false;

            StringBuilder debug = new StringBuilder(4096);

            // state
            string strStateAction = this.MainForm.AppInfo.GetString(
                "change_reader_param",
                "state",
                "<���ı�>");
            if (strStateAction != "<���ı�>")
            {
                string strState = DomUtil.GetElementText(dom.DocumentElement,
                    "state");

                if (strStateAction == "<������>")
                {
                    string strAdd = this.MainForm.AppInfo.GetString(
                "change_reader_param",
                "state_add",
                "");
                    string strRemove = this.MainForm.AppInfo.GetString(
            "change_reader_param",
            "state_remove",
            "");

                    string strOldState = strState;

                    if (String.IsNullOrEmpty(strAdd) == false)
                        StringUtil.SetInList(ref strState, strAdd, true);
                    if (String.IsNullOrEmpty(strRemove) == false)
                        StringUtil.SetInList(ref strState, strRemove, false);

                    if (strOldState != strState)
                    {
                        DomUtil.SetElementText(dom.DocumentElement,
                            "state",
                            strState);
                        bChanged = true;

                        debug.Append("<state> '" + strOldState + "' --> '" + strState + "'\r\n");
                    }
                }
                else
                {
                    if (strStateAction != strState)
                    {
                        DomUtil.SetElementText(dom.DocumentElement,
                            "state",
                            strStateAction);
                        bChanged = true;

                        debug.Append("<state> '" + strState + "' --> '" + strStateAction + "'\r\n");
                    }
                }
            }

            // expire date
            string strTimeAction = this.MainForm.AppInfo.GetString(
    "change_reader_param",
    "expire_date",
    "<���ı�>");
            if (strTimeAction != "<���ı�>")
            {
                string strTime = DomUtil.GetElementText(dom.DocumentElement,
                    "expireDate");
                DateTime time = new DateTime(0);
                if (strTimeAction == "<��ǰʱ��>")
                {
                    time = now;
                }
                else if (strTimeAction == "<���>")
                {

                }
                else if (strTimeAction == "<ָ��ʱ��>")
                {
                    string strValue = this.MainForm.AppInfo.GetString(
                        "change_reader_param",
                        "expire_date_value",
                        "");
                    if (String.IsNullOrEmpty(strValue) == true)
                    {
                        strError = "������ <ָ��ʱ��> ��ʽ���޸�ʱ����ָ����ʱ��ֵ����Ϊ��";
                        return -1;
                    }
                    try
                    {
                        time = DateTime.Parse(strValue);
                    }
                    catch (Exception ex)
                    {
                        strError = "�޷�����ʱ���ַ��� '" + strValue + "' :" + ex.Message;
                        return -1;
                    }
                }
                else
                {
                    // ��֧��
                    strError = "��֧�ֵ�ʱ�䶯�� '" + strTimeAction + "'";
                    return -1;
                }

                string strOldTime = strTime;

                if (strTimeAction == "<���>")
                    strTime = "";
                else
                    strTime = DateTimeUtil.Rfc1123DateTimeStringEx(time);

                if (strOldTime != strTime)
                {
                    DomUtil.SetElementText(dom.DocumentElement,
                        "expireDate",
                        strTime);
                    bChanged = true;

                    debug.Append("<expireDate> '" + strOldTime + "' --> '" + strTime + "'\r\n");
                }
            }

            // reader type
            string strReaderTypeAction = this.MainForm.AppInfo.GetString(
"change_reader_param",
"reader_type",
"<���ı�>");
            if (strReaderTypeAction != "<���ı�>")
            {
                string strReaderType = DomUtil.GetElementText(dom.DocumentElement,
                    "readerType");

                if (strReaderType != strReaderTypeAction)
                {
                    DomUtil.SetElementText(dom.DocumentElement,
                        "readerType",
                        strReaderTypeAction);
                    bChanged = true;

                    debug.Append("<readerType> '" + strReaderType + "' --> '" + strReaderTypeAction + "'\r\n");
                }
            }

            // �����ֶ�
            string strFieldName = this.MainForm.AppInfo.GetString(
"change_reader_param",
"field_name",
"<��ʹ��>");
            if (strFieldName != "<��ʹ��>")
            {
                string strFieldValue = this.MainForm.AppInfo.GetString(
    "change_reader_param",
    "field_value",
    "");
                if (strFieldName == "֤�����")
                {
                    ChangeField(ref dom,
            "barcode",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "֤��")
                {
                    ChangeField(ref dom,
            "cardNumber",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "��֤����")
                {
                    ChangeField(ref dom,
            "createDate",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "ʧЧ����")
                {
                    ChangeField(ref dom,
            "expireDate",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "ע��")
                {
                    ChangeField(ref dom,
            "comment",
            strFieldValue,
            ref debug,
            ref bChanged);
                }


                if (strFieldName == "�������")
                {
                    // hire Ԫ�ص� period ����
                    ChangeField(ref dom,
            "hire",
            "period",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "���ʧЧ��")
                {
                    // hire Ԫ�ص� expireDate ����

                    ChangeField(ref dom,
            "hire",
            "expireDate",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "Ѻ�����")
                {
                    ChangeField(ref dom,
            "foregift",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "����")
                {
                    ChangeField(ref dom,
            "name",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "�Ա�")
                {
                    ChangeField(ref dom,
            "gender",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "��������")
                {
                    ChangeField(ref dom,
            "dateOfBirth",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "���֤��")
                {
                    ChangeField(ref dom,
            "idCardNumber",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "��λ")
                {
                    ChangeField(ref dom,
            "department",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "ְ��")
                {
                    ChangeField(ref dom,
            "post",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "��ַ")
                {
                    ChangeField(ref dom,
            "address",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "�绰")
                {
                    ChangeField(ref dom,
            "tel",
            strFieldValue,
            ref debug,
            ref bChanged);
                }

                if (strFieldName == "Email��ַ")
                {
                    ChangeField(ref dom,
            "email",
            strFieldValue,
            ref debug,
            ref bChanged);
                }
            }

            strDebugInfo = debug.ToString();

            if (bChanged == true)
                return 1;

            return 0;
        }
#endif

        // �����ƶ����߼�¼
        void menu_moveRecords_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ�ƶ��Ķ��߼�¼����";
                goto ERROR1;
            }

            if (this.m_nChangedCount > 0)
            {
                // ������δ����
                strError = "��ǰ�������� " + m_nChangedCount + " ���޸���δ���档����ʱ�ƶ����߼�¼������δ������Ϣ���ܻᶪʧ��\r\n\r\n���ȱ����¼�����߷����޸ĺ�������ִ�б�����";
                goto ERROR1;
            }

            // �õ�ѡ����Χ�ĵ�һ����¼��·��
            string strFirstRecPath = this.listView_records.SelectedItems[0].Text;

            // ���ֶԻ������û�����ѡ��Ŀ���
            ReaderSaveToDialog saveto_dlg = new ReaderSaveToDialog();
            MainForm.SetControlFont(saveto_dlg, this.Font, false);
            saveto_dlg.Text = "�ƶ����߼�¼";
            saveto_dlg.MessageText = "��ѡ��Ҫ�ƶ�ȥ��Ŀ���¼λ��";
            saveto_dlg.MainForm = this.MainForm;
            saveto_dlg.RecPath = strFirstRecPath;
            saveto_dlg.RecID = "?";
            if (this.listView_records.SelectedItems.Count > 1)
                saveto_dlg.EnableRecID = false; // �����¼����һ��������£��ʺ�ID�����޸�

            this.MainForm.AppInfo.LinkFormState(saveto_dlg, "readersearchform_movetodialog_state");
            saveto_dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(saveto_dlg);

            if (saveto_dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�ƶ����߼�¼ ...");
            stop.BeginLoop();

            EnableControls(false);
            this.listView_records.Enabled = false;

            int nCount = 0;
            try
            {
                stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                int i = 0;
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            MessageBox.Show(this, "�û��ж�");
                            return;
                        }
                    }

                    string strCurrentRecPath = item.Text;

                    if (string.IsNullOrEmpty(strCurrentRecPath) == true)
                    {
                        // Debug.Assert(false, "");
                        continue;
                    }

                    string strTargetRecPath = saveto_dlg.RecPath;

                    stop.SetMessage("�����ƶ����߼�¼ '"+strCurrentRecPath+"' ...");

                    byte[] target_timestamp = null;
                    long lRet = Channel.MoveReaderInfo(
        stop,
        strCurrentRecPath,
        ref strTargetRecPath,
        out target_timestamp,
        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    Debug.Assert(string.IsNullOrEmpty(strTargetRecPath) == false, "");

                    item.Text = strTargetRecPath;   // ˢ������еļ�¼·������

                    stop.SetProgressValue(++i);
                    nCount++;
                }
            }
            finally
            {
                EnableControls(true);
                this.listView_records.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;
            }
            MessageBox.Show(this, "�ɹ��ƶ����߼�¼ "+nCount.ToString()+" ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �����������鵽 Excel �ļ�
        void menu_exportReaderInfoToExcelFile_Click(object sender, EventArgs e)
        {
            string strError = "";
            List<string> barcodes = new List<string>();
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                // TODO: �� style ��ʶ����
                barcodes.Add(item.SubItems[1].Text);
            }

            // return:
            //      -1  ����
            //      0   �û��ж�
            //      1   �ɹ�
            int nRet = this.CreateReaderDetailExcelFile(barcodes,
                true,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // MessageBox.Show(this, "�������");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ����Ϊ������ļ�
        void menu_exportBarcodeFile_Click(object sender, EventArgs e)
        {
            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ�����������ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = this.ExportBarcodeFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "������ļ� (*.txt)|*.txt|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.ExportBarcodeFilename = dlg.FileName;

            bool bAppend = true;

            if (File.Exists(this.ExportBarcodeFilename) == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "������ļ� '" + this.ExportBarcodeFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel ��������)",
                    "ReaderSearchForm",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Cancel)
                    return;
                if (result == DialogResult.No)
                    bAppend = false;
                else if (result == DialogResult.Yes)
                    bAppend = true;
                else
                {
                    Debug.Assert(false, "");
                }
            }
            else
                bAppend = false;

            // �����ļ�
            StreamWriter sw = new StreamWriter(this.ExportBarcodeFilename,
                bAppend,	// append
                System.Text.Encoding.UTF8);
            try
            {
                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    sw.WriteLine(item.SubItems[1].Text);
                }

                this.Cursor = oldCursor;
            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }

            string strExportStyle = "����";
            if (bAppend == true)
                strExportStyle = "׷��";

            this.MainForm.StatusBarMessage = "����֤����� " + this.listView_records.SelectedItems.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportBarcodeFilename;
        }

        // ����Ϊ��¼·���ļ�
        void menu_exportRecPathFile_Click(object sender, EventArgs e)
        {
            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ����ļ�¼·���ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = this.ExportRecPathFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.ExportRecPathFilename = dlg.FileName;

            bool bAppend = true;

            if (File.Exists(this.ExportRecPathFilename) == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "��¼·���ļ� '" + this.ExportRecPathFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel ��������)",
                    "ReaderSearchForm",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Cancel)
                    return;
                if (result == DialogResult.No)
                    bAppend = false;
                else if (result == DialogResult.Yes)
                    bAppend = true;
                else
                {
                    Debug.Assert(false, "");
                }
            }
            else
                bAppend = false;

            // �����ļ�
            StreamWriter sw = new StreamWriter(this.ExportRecPathFilename,
                bAppend,	// append
                System.Text.Encoding.UTF8);
            try
            {
                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    sw.WriteLine(item.Text);
                }

                this.Cursor = oldCursor;
            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }

            string strExportStyle = "����";
            if (bAppend == true)
                strExportStyle = "׷��";

            this.MainForm.StatusBarMessage = "���߼�¼·�� " + this.listView_records.SelectedItems.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportRecPathFilename;
        }

        private void listView_records_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            /*
            int nClickColumn = e.Column;

            ColumnSortStyle sortStyle = ColumnSortStyle.LeftAlign;

            // ��һ��Ϊ��¼·��������������
            if (nClickColumn == 0)
                sortStyle = ColumnSortStyle.RecPath;

            this.SortColumns.SetFirstColumn(nClickColumn,
                sortStyle,
                this.listView_records.Columns);

            // ����
            this.listView_records.ListViewItemSorter = new SortColumnsComparer(this.SortColumns);

            this.listView_records.ListViewItemSorter = null;
            */
            ListViewUtil.OnColumnClick(this.listView_records, e);

        }



        private void listView_records_SelectedIndexChanged(object sender, EventArgs e)
        {
#if NO
            API.MSG msg = new API.MSG();
            bool bRet = API.PeekMessage(ref msg, 
                this.Handle, 
                (uint)WM_SELECT_INDEX_CHANGED,
                (uint)WM_SELECT_INDEX_CHANGED, 
                0);
            if (bRet == false)
                API.PostMessage(this.Handle, WM_SELECT_INDEX_CHANGED, 0, 0);
#endif
            // this.commander.AddMessage(WM_SELECT_INDEX_CHANGED);

            OnListViewSelectedIndexChanged(sender, e);
        }

        private void textBox_queryWord_TextChanged(object sender, EventArgs e)
        {
            this.Text = "���߲�ѯ " + this.textBox_queryWord.Text;
        }

        private void listView_records_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                string strTotal = "";
                if (this.listView_records.SelectedIndices.Count > 0)
                {
                    for (int i = 0; i < this.listView_records.SelectedIndices.Count; i++)
                    {
                        int index = this.listView_records.SelectedIndices[i];

                        ListViewItem item = this.listView_records.Items[index];
                        string strLine = Global.BuildLine(item);
                        strTotal += strLine + "\r\n";
                    }
                }
                else
                {
                    strTotal = Global.BuildLine((ListViewItem)e.Item);
                }

                this.listView_records.DoDragDrop(
                    strTotal,
                    DragDropEffects.Link);
            }
        }

        private void comboBox_matchStyle_TextChanged(object sender, EventArgs e)
        {
            if (this.comboBox_matchStyle.Text == "��ֵ")
            {
                this.textBox_queryWord.Text = "";
                this.textBox_queryWord.Enabled = false;
            }
            else
            {
                this.textBox_queryWord.Enabled = true;
            }

        }

        // �������ͼ��
        private void comboBox_readerDbName_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_readerDbName.Invalidate();
        }

        // �������ͼ��
        private void comboBox_from_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_from.Invalidate();
        }

        // �������ͼ��
        private void comboBox_matchStyle_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_matchStyle.Invalidate();
        }

        private void textBox_queryWord_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                // �س�
                case (char)Keys.Enter:
                    toolStripButton_search_Click(sender, e);
                    break;
            }
        }

        private void ToolStripMenuItem_rfc1123Single_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            dlg.RangeMode = false;
            try
            {
                dlg.Rfc1123String = this.textBox_queryWord.Text;
            }
            catch
            {
                this.textBox_queryWord.Text = "";
            }
            this.MainForm.AppInfo.LinkFormState(dlg, "searchreaderform_gettimedialog_single");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_queryWord.Text = dlg.Rfc1123String;
        }

        private void ToolStripMenuItem_uSingle_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            dlg.RangeMode = false;
            try
            {
                dlg.uString = this.textBox_queryWord.Text;
            }
            catch
            {
                this.textBox_queryWord.Text = "";
            }

            this.MainForm.AppInfo.LinkFormState(dlg, "searchreaderform_gettimedialog_single");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_queryWord.Text = dlg.uString;
        }

        private void ToolStripMenuItem_rfc1123Range_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            dlg.RangeMode = true;
            // �ָ�Ϊ�����ַ���
            try
            {
                dlg.Rfc1123String = this.textBox_queryWord.Text;
            }
            catch
            {
                this.textBox_queryWord.Text = "";
            }
            this.MainForm.AppInfo.LinkFormState(dlg, "searchreaderform_gettimedialog_range");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_queryWord.Text = dlg.Rfc1123String;
        }

        private void ToolStripMenuItem_uRange_Click(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            dlg.RangeMode = true;
            try
            {
                dlg.uString = this.textBox_queryWord.Text;
            }
            catch
            {
                this.textBox_queryWord.Text = "";
            }

            this.MainForm.AppInfo.LinkFormState(dlg, "searchreaderform_gettimedialog_range");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.textBox_queryWord.Text = dlg.uString;
        }

        #region ָ�ƻ�����ع���

        System.Windows.Forms.Label m_labelPrompt = null;

        // �������ڳ���һ�������ʾ
        // parameters:
        //      strText ��ʾ���ݡ����Ϊnull����ʾ�ָ�����ʾ��״̬
        void Prompt(string strText)
        {
            if (string.IsNullOrEmpty(strText) == true)
            {
                if (m_labelPrompt != null
                    && this.Controls.IndexOf(this.m_labelPrompt) != -1)
                {
                    this.Controls.Remove(this.m_labelPrompt);
                    this.m_labelPrompt = null;
                }
                return;
            }
            if (m_labelPrompt == null)
            {
                m_labelPrompt = new Label();
                m_labelPrompt.BackColor = SystemColors.Highlight;
                m_labelPrompt.ForeColor = SystemColors.HighlightText;
                //m_labelPrompt.BackColor = Color.White;
                //m_labelPrompt.ForeColor = Color.FromArgb(100,100,100);
                m_labelPrompt.Font = new Font(this.Font.FontFamily, (float)12, FontStyle.Bold);
                m_labelPrompt.TextAlign = ContentAlignment.MiddleCenter;
                /*
                string strFilename = PathUtil.MergePath(this.MainForm.DataDir, "fingerprint-cache-loading.gif");
                if (File.Exists(strFilename) == true)
                {
                    m_labelPrompt.ImageAlign = ContentAlignment.TopCenter;
                    m_labelPrompt.Image = Image.FromFile(strFilename, false);
                }
                 * */
            }
            m_labelPrompt.Text = strText;
            if (this.Controls.IndexOf(this.m_labelPrompt) == -1)
            {
                m_labelPrompt.Dock = DockStyle.Fill;

                this.Controls.Add(m_labelPrompt);
                this.m_labelPrompt.PerformLayout();
                this.ResumeLayout(false);
                this.m_labelPrompt.BringToFront();
            }
            Application.DoEvents();
            this.Update();
        }

        private void ToolStripMenuItem_initFingerprintCache_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = InitFingerprintCache(false, out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
        
        // parameters:
        //      bDelayShow  �ӳ���ʾ��ǰ���ڡ����Ϊ false����ʾ��������ʾ���¶�
        // return:
        //      -2  remoting����������ʧ�ܡ�ָ�ƽӿڳ�����δ����
        //      -1  ����
        //      0   �ɹ�
        /// <summary>
        /// ��ʼ��ָ�ƻ���
        /// </summary>
        /// <param name="bDelayShow">�Ƿ��ӳ���ʾ��ǰ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-2:  remoting����������ʧ�ܡ�ָ�ƽӿڳ�����δ����</para>
        /// <para>-1:  ����</para>
        /// <para>0:   �ɹ�</para>
        /// </returns>
        public int InitFingerprintCache(
            bool bDelayShow,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(this.MainForm.FingerprintReaderUrl) == true)
            {
                strError = "��δ���� ָ���Ķ����ӿ�URL ����";
                return -1;
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڴ���ָ�����ݻ��� ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                // �����ǰ��ȫ���������ݣ��Ա����½���
                // return:
                //      -2  remoting����������ʧ�ܡ�����������δ����
                //      -1  ����
                //      >=0 ʵ�ʷ��͸��ӿڳ����������Ŀ
                int nRet = CreateFingerprintCache(null,
                    out strError);
                if (nRet == -1 || nRet == -2)
                    return nRet;

                // TODO: �����ӳ���ʾ
                if (bDelayShow == true)
                {
                    this.Opacity = 1;
                }

                this.Prompt("���ڳ�ʼ��ָ�ƻ��� ...\r\n�벻Ҫ�رձ�����\r\n\r\n(�ڴ˹����У���ָ��ʶ���޹صĴ��ں͹��ܲ���Ӱ�죬��ǰ��ʹ��)\r\n");

                List<string> readerdbnames = null;
                nRet = GetCurrentOwnerReaderNameList(
                    out readerdbnames,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (readerdbnames.Count == 0)
                {
                    strError = "��ǰ�û�û�й�Ͻ�κζ��߿⣬��ʼ��ָ�ƻ���Ĳ����޷����";
                    return -1;
                }

                int nCount = 0;
                // ����Щ���߿�������и��ٻ���ĳ�ʼ��
                // ʹ�� ����� browse ��ʽ���Ա��ö��߼�¼�е� fingerprint timestamp�ַ��������߼��� fingerprint string
                // <fingerprint timestamp='XXXX'></fingerprint>
                foreach (string strReaderDbName in readerdbnames)
                {
                    // ��ʼ��һ�����߿��ָ�ƻ���
                    // return:
                    //      -1  ����
                    //      >=0 ʵ�ʷ��͸��ӿڳ����������Ŀ
                    nRet = BuildOneDbCache(
                        strReaderDbName,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    nCount += nRet;
                }

                if (nCount == 0)
                {
                    strError = "��ǰ�û���Ͻ�Ķ��߿� " + StringUtil.MakePathList(readerdbnames) + " ��û���κζ��߼�¼����ʼ��ָ�ƻ���Ĳ���û�����";
                    return -1;
                }
            }
            finally
            {
                this.Prompt(null);
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
            }

            return 0;
        }

        // ��ʼ��һ�����߿��ָ�ƻ���
        // return:
        //      -1  ����
        //      >=0 ʵ�ʷ��͸��ӿڳ����������Ŀ
        int BuildOneDbCache(
            string strReaderDbName,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            DpResultSet resultset = null;
            bool bCreate = false;

            Hashtable timestamp_table = new Hashtable();    // recpath --> fingerprint timestamp

            string strDir = this.MainForm.FingerPrintCacheDir;  // PathUtil.MergePath(this.MainForm.DataDir, "fingerprintcache");
            PathUtil.CreateDirIfNeed(strDir);

            // ������ļ���
            string strResultsetFilename = PathUtil.MergePath(strDir, strReaderDbName);

            if (File.Exists(strResultsetFilename) == false)
            {
                resultset = new DpResultSet(false, false);
                resultset.Create(strResultsetFilename,
                    strResultsetFilename + ".index");
                bCreate = true;
            }
            else
                bCreate = false;

            // *** ��һ�׶Σ� �����µĽ�����ļ������߻�ȡȫ�����߼�¼�е�ָ��ʱ���

            bool bDone = false;    // ���������� �Ƿ������д�����
            try
            {
                /*
                long lRet = Channel.SearchReader(stop,
        strReaderDbName,
        "1-9999999999",
        -1,
        "__id",
        "left",
        this.Lang,
        null,   // strResultSetName
        "", // strOutputStyle
        out strError);
                */
                long lRet = Channel.SearchReader(stop,
strReaderDbName,
"",
-1,
"ָ��ʱ���",
"left",
this.Lang,
null,   // strResultSetName
"", // strOutputStyle
out strError);
                if (lRet == -1)
                {
                    if (Channel.ErrorCode == ErrorCode.AccessDenied)
                        strError = "�û� "+Channel.UserName+" Ȩ�޲���: " + strError;
                    return -1;
                }

                if (lRet == 0)
                {
                    // TODO: ��ʱ�������ǰ�н�����ļ����������������Ӱ�칦����ȷ�ԣ����ԸĽ�Ϊ�Ѳ����Ľ�����ļ�ɾ��
                    return 0;
                }

                long lHitCount = lRet;
                stop.SetProgressRange(0, lHitCount);

                long lStart = 0;
                long lCount = lHitCount;
                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }


                    lRet = Channel.GetSearchResult(
                        stop,
                        null,   // strResultSetName
                        lStart,
                        lCount,
                        bCreate == true ? "id,cols,format:cfgs/browse_fingerprint" : "id,cols,format:cfgs/browse_fingerprinttimestamp",
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                        return -1;

                    if (lRet == 0)
                    {
                        strError = "GetSearchResult() return 0";
                        return -1;
                    }

                    Debug.Assert(searchresults != null, "");
                    Debug.Assert(searchresults.Length > 0, "");

                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        DigitalPlatform.CirculationClient.localhost.Record record = searchresults[i];
                        if (bCreate == true)
                        {
                            if (record.Cols == null || record.Cols.Length < 3)
                            {
                                strError = "record.Cols error ... �п�������Ϊ���߿�ȱ�������ļ� cfgs/browse_fingerprint";
                                return -1;
                            }
                            if (string.IsNullOrEmpty(record.Cols[0]) == true)
                                continue;   // ���߼�¼��û��ָ����Ϣ
                            DpRecord item = new DpRecord(record.Path);
                            // timestamp | barcode | fingerprint
                            item.BrowseText = record.Cols[0] + "|" + record.Cols[1] + "|" + record.Cols[2];
                            resultset.Add(item);
                        }
                        else
                        {
                            if (record.Cols == null || record.Cols.Length < 1)
                            {
                                strError = "record.Cols error ... �п�������Ϊ���߿�ȱ�������ļ� cfgs/browse_fingerprinttimestamp";
                                return -1;
                            }
                            if (record.Cols.Length < 2)
                            {
                                strError = "record.Cols error ... ��Ҫˢ�������ļ� cfgs/browse_fingerprinttimestamp �����°汾";
                                return -1;
                            }
                            if (string.IsNullOrEmpty(record.Cols[0]) == true)
                                continue;   // ���߼�¼��û��ָ����Ϣ

                            // ����ʱ���
                            // timestamp | barcode 
                            timestamp_table[record.Path] = record.Cols[0] + "|" + record.Cols[1];
                        }
                    }

                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    stop.SetMessage(strReaderDbName + " ������¼ " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " ��");

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                    stop.SetProgressValue(lStart);

                }

                if (bCreate == true)
                    bDone = true;

                if (bCreate == true)
                {
                    // return:
                    //      -2  remoting����������ʧ�ܡ�����������δ����
                    //      -1  ����
                    //      >=0 ʵ�ʷ��͸��ӿڳ����������Ŀ
                    nRet = CreateFingerprintCache(resultset,
    out strError);
                    if (nRet == -1 || nRet == -2)
                        return -1;

                    return nRet;
                }
            }
            finally
            {
                if (bCreate == true)
                {
                    Debug.Assert(resultset != null, "");
                    if (bDone == true)
                    {
                        string strTemp1 = "";
                        string strTemp2 = "";
                        resultset.Detach(out strTemp1,
                            out strTemp2);
                    }
                    else
                    {
                        // �����ļ��ᱻɾ��
                        resultset.Close();
                    }
                }
            }

            // �ȶ�ʱ��������½�����ļ�
            Hashtable update_table = new Hashtable();   // ��Ҫ���µ����recpath --> 1
            resultset = new DpResultSet(false, false);
            resultset.Attach(strResultsetFilename,
    strResultsetFilename + ".index");
            try
            {
                long nCount = resultset.Count;
                for (long i = 0; i < nCount; i++)
                {
                    DpRecord record = resultset[i];

                    string strRecPath = record.ID;
                    // timestamp | barcode 
                    string strNewTimestamp = (string)timestamp_table[strRecPath];
                    if (strNewTimestamp == null)
                    {
                        // ����״̬�£����߼�¼�Ѿ������ڣ���Ҫ�ӽ������ɾ��
                        resultset.RemoveAt((int)i);
                        i--;
                        nCount--;
                        continue;
                    }

                    // ��ֳ�֤����� 2013/1/28
                    string strNewBarcode = "";
                    nRet = strNewTimestamp.IndexOf("|");
                    if (nRet != -1)
                    {
                        strNewBarcode = strNewTimestamp.Substring(nRet + 1);
                        strNewTimestamp = strNewTimestamp.Substring(0, nRet);
                    }

                    // ���¶��߼�¼���Ѿ�û��ָ����Ϣ��������߼�¼�е�ָ��Ԫ�ر�ɾ����
                    if (string.IsNullOrEmpty(strNewTimestamp) == true)
                    {
                        // ɾ����������
                        resultset.RemoveAt((int)i);
                        i--;
                        nCount--;

                        timestamp_table.Remove(strRecPath);
                        continue;
                    }

                    // ȡ�ý�����ļ��е�ԭ��ʱ����ַ���
                    string strText = record.BrowseText; // timestamp | barcode | fingerprint
                    nRet = strText.IndexOf("|");
                    if (nRet == -1)
                    {
                        strError = "browsetext ����û�� '|' �ַ�";
                        return -1;
                    }
                    string strOldTimestamp = strText.Substring(0, nRet);
                    // timestamp | barcode | fingerprint
                    string strOldBarcode = strText.Substring(nRet + 1);
                    nRet = strOldBarcode.IndexOf("|");
                    if (nRet != -1)
                    {
                        strOldBarcode = strOldBarcode.Substring(0, nRet);
                    }

                    // ʱ��������仯����Ҫ��������
                    if (strNewTimestamp != strOldTimestamp
                        || strNewBarcode != strOldBarcode)
                    {
                        // ���֤�����Ϊ�գ��޷��������չ�ϵ��Ҫ����
                        if (string.IsNullOrEmpty(strNewBarcode) == false)
                            update_table[strRecPath] = 1;

                        // ɾ����������
                        resultset.RemoveAt((int)i);
                        i--;
                        nCount--;
                    }
                    timestamp_table.Remove(strRecPath);
                }

                // ѭ��������timestamp_table��ʣ����ǵ�ǰ������ļ���û�а�������Щ���߼�¼·��

                if (update_table.Count > 0)
                {
                    // ��ȡָ����Ϣ��׷�ӵ�������ļ���β��
                    // parameters:
                    //      update_table   keyΪ���߼�¼·��
                    nRet = AppendFingerprintInfo(resultset,
                        update_table,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

                // �����������������ָ����Ϣ,��Ҫ��ȡ��׷�ӵ�������ļ�β��
                if (timestamp_table.Count > 0)
                {
                    // ��ȡָ����Ϣ��׷�ӵ�������ļ���β��
                    // parameters:
                    //      update_table   keyΪ���߼�¼·��
                    nRet = AppendFingerprintInfo(resultset,
                        timestamp_table,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

                // return:
                //      -2  remoting����������ʧ�ܡ�����������δ����
                //      -1  ����
                //      >=0 ʵ�ʷ��͸��ӿڳ����������Ŀ
                nRet = CreateFingerprintCache(resultset,
            out strError);
                if (nRet == -1 || nRet == -2)
                    return -1;

                return nRet;
            }
            finally
            {
                string strTemp1 = "";
                string strTemp2 = "";
                resultset.Detach(out strTemp1, out strTemp2);
            }
        }

        // ��ȡָ����Ϣ��׷�ӵ�������ļ���β��
        // parameters:
        //      update_table   keyΪ���߼�¼·��
        int AppendFingerprintInfo(DpResultSet resultset,
            Hashtable update_table,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // ��Ҫ��ø��µ����Ȼ��׷�ӵ�������ļ���β��
            // ע�⣬��Ҫ���ڳ����ؽ�������ļ����Ա���ն���ռ�
            List<string> lines = new List<string>();
            foreach (string recpath in update_table.Keys)
            {
                lines.Add(recpath);
                if (lines.Count >= 100)
                {
                    List<DigitalPlatform.CirculationClient.localhost.Record> records = null;
                    nRet = GetSomeFingerprintData(lines,
    out records,
    out strError);
                    if (nRet == -1)
                        return -1;
                    foreach (DigitalPlatform.CirculationClient.localhost.Record record in records)
                    {
                        if (record.Cols == null || record.Cols.Length < 3)
                        {
                            strError = "record.Cols error ... �п�������Ϊ���߿�ȱ�������ļ� cfgs/browse_fingerprint";
                            // TODO: ��������������£�������;���ֶ��߼�¼�����ǰ���޸ĵ�����������ƺ�����continue
                            return -1;
                        }

                        // ���֤�����Ϊ�գ��޷��������չ�ϵ��Ҫ����
                        if (string.IsNullOrEmpty(record.Cols[1]) == true)
                            continue;

                        DpRecord item = new DpRecord(record.Path);
                        // timestamp | barcode | fingerprint
                        item.BrowseText = record.Cols[0] + "|" + record.Cols[1] + "|" + record.Cols[2];
                        resultset.Add(item);
                    }
                    lines.Clear();
                }
            }

            if (lines.Count > 0)
            {
                List<DigitalPlatform.CirculationClient.localhost.Record> records = null;
                nRet = GetSomeFingerprintData(lines,
out records,
out strError);
                if (nRet == -1)
                    return -1;
                foreach (DigitalPlatform.CirculationClient.localhost.Record record in records)
                {
                    if (record.Cols == null || record.Cols.Length < 3)
                    {
                        strError = "record.Cols error ... �п�������Ϊ���߿�ȱ�������ļ� cfgs/browse_fingerprint";
                        // TODO: ��������������£�������;���ֶ��߼�¼�����ǰ���޸ĵ�����������ƺ�����continue
                        return -1;
                    }
                    DpRecord item = new DpRecord(record.Path);
                    // timestamp | barcode | fingerprint
                    item.BrowseText = record.Cols[0] + "|" + record.Cols[1] + "|" + record.Cols[2];
                    resultset.Add(item);
                }
            }

            return 0;
        }

        // ����һС��ָ�����ݵ�װ��
        // parameters:
        int GetSomeFingerprintData(List<string> lines,
            out List<DigitalPlatform.CirculationClient.localhost.Record> records,
            out string strError)
        {
            strError = "";

            records = new List<DigitalPlatform.CirculationClient.localhost.Record>();
            // List<DigitalPlatform.CirculationClient.localhost.Record> records = new List<DigitalPlatform.CirculationClient.localhost.Record>();

            for (; ; )
            {
                if (stop != null && stop.State != 0)
                {
                    strError = "�û��ж�1";
                    return -1;
                }

                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                string[] paths = new string[lines.Count];
                lines.CopyTo(paths);
            REDO_GETRECORDS:
                long lRet = this.Channel.GetBrowseRecords(
                    this.stop,
                    paths,
                    "id,cols,format:cfgs/browse_fingerprint",
                    out searchresults,
                    out strError);
                if (lRet == -1)
                {
                    DialogResult temp_result = MessageBox.Show(this,
    strError + "\r\n\r\n�Ƿ�����?",
    "ReaderSearchForm",
    MessageBoxButtons.RetryCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                    if (temp_result == DialogResult.Retry)
                        goto REDO_GETRECORDS;
                    return -1;
                }

                records.AddRange(searchresults);

                // ȥ���Ѿ�������һ����
                lines.RemoveRange(0, searchresults.Length);

                if (lines.Count == 0)
                    break;
            }

            return 0;
        }

        // ��õ�ǰ�ʻ�����Ͻ�Ķ��߿�����
        // Ϊ��ȷ��Channel�Զ���¼�����������һ�η��������readerdbgroup���塣��ʵ��Ӧ�Ķ�����Ϣ��MainForm�����е�
        int GetCurrentOwnerReaderNameList(
            out List<string> readerdbnames,
            out string strError)
        {
            strError = "";
            readerdbnames = new List<string>();
            // int nRet = 0;

            // ȷ����¼һ��
            string strValue = "";
            long lRet = Channel.GetSystemParameter(stop,
    "system",
    "readerDbGroup",
    out strValue,
    out strError);
            if (lRet == -1)
                return -1;

            // �·���
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            try
            {
                dom.DocumentElement.InnerXml = strValue;
            }
            catch (Exception ex)
            {
                strError = "category=system,name=readerDbGroup�����ص�XMLƬ����װ��InnerXmlʱ����: " + ex.Message;
                return -1;
            }

            string strLibraryCodeList = this.Channel.LibraryCodeList;


            XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");

            foreach (XmlNode node in nodes)
            {
                string strLibraryCode = DomUtil.GetAttr(node, "libraryCode");

                if (Global.IsGlobalUser(strLibraryCodeList) == false)
                {
                    if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                        continue;
                }

                string strDbName = DomUtil.GetAttr(node, "name");
                readerdbnames.Add(strDbName);
                /*
                bool bValue = true;
                nRet = DomUtil.GetBooleanParam(node,
                    "inCirculation",
                    true,
                    out bValue,
                    out strError);
                if (bValue == false)
                    continue;   // �������μ���ͨ�Ŀ�
                 * */
            }

            return 0;
        }

        static void ParseResultItemString(string strText,
            out string strTimestamp,
            out string strBarcode,
            out string strFingerprint)
        {
            strTimestamp = "";
            strBarcode = "";
            strFingerprint = "";

            string[] parts = strText.Split(new char[] {'|'});
            if (parts.Length > 0)
                strTimestamp = parts[0];
            if (parts.Length > 1)
                strBarcode = parts[1];
            if (parts.Length > 2)
                strFingerprint = parts[2];
        }

        // return:
        //      -2  remoting����������ʧ�ܡ�����������δ����
        //      -1  ����
        //      >=0 ʵ�ʷ��͸��ӿڳ����������Ŀ
        int CreateFingerprintCache(DpResultSet resultset,
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
                if (resultset == null)
                {
                    // �����ǰ��ȫ���������ݣ��Ա����½���
                    // return:
                    //      -2  remoting����������ʧ�ܡ�����������δ����
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = AddItems(null,
    out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == -2)
                        return -2;

                    return 0;
                }

                int nSendCount = 0;
                long nCount = resultset.Count;
                List<FingerprintItem> items = new List<FingerprintItem>();
                for (long i = 0; i < nCount; i++)
                {
                    DpRecord record = resultset[i];

                    string strTimestamp = "";
                    string strBarcode = "";
                    string strFingerprint = "";
                    ParseResultItemString(record.BrowseText,
out strTimestamp,
out strBarcode,
out strFingerprint);
                    // TODO: ע�����֤�����Ϊ�յģ���Ҫ���ͳ�ȥ


                    FingerprintItem item = new FingerprintItem();
                    item.ReaderBarcode = strBarcode;
                    item.FingerprintString = strFingerprint;

                    items.Add(item);
                    if (items.Count >= 100)
                    {
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
                        nSendCount += items.Count;
                        items.Clear();
                    }
                }

                if (items.Count > 0)
                {
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
                    nSendCount += items.Count;
                }

                // Console.Beep(); // ��ʾ��ȡ�ɹ�
                return nSendCount;
            }
            finally
            {
                EndFingerprintChannel();
            }
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

        IpcClientChannel m_fingerPrintChannel = new IpcClientChannel();
        IFingerprint m_fingerPrintObj = null;

        int StartFingerprintChannel(
            string strUrl,
            out string strError)
        {
            strError = "";

            //Register the channel with ChannelServices.
            ChannelServices.RegisterChannel(m_fingerPrintChannel, false);

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

        #endregion

        #region �������йع���

        internal override bool InSearching
        {
            get
            {
                if (this.comboBox_from.Enabled == true)
                    return false;
                return true;
            }
        }





        static string MergeXml(string strXml1,
    string strXml2)
        {
            if (string.IsNullOrEmpty(strXml1) == true)
                return strXml2;
            if (string.IsNullOrEmpty(strXml2) == true)
                return strXml1;

            return strXml1; // ��ʱ����
        }

        internal override string GetHeadString(bool bAjax = true)
        {
            string strCssFilePath = PathUtil.MergePath(this.MainForm.DataDir, "operloghtml.css");

            if (bAjax == true)
                return
                    "<head>" +
                    "<LINK href='" + strCssFilePath + "' type='text/css' rel='stylesheet'>" +
                    "<link href=\"%mappeddir%/jquery-ui-1.8.7/css/jquery-ui-1.8.7.css\" rel=\"stylesheet\" type=\"text/css\" />" +
                    "<script type=\"text/javascript\" src=\"%mappeddir%/jquery-ui-1.8.7/js/jquery-1.4.4.min.js\"></script>" +
                    "<script type=\"text/javascript\" src=\"%mappeddir%/jquery-ui-1.8.7/js/jquery-ui-1.8.7.min.js\"></script>" +
                    //"<script type='text/javascript' src='%datadir%/jquery.js'></script>" +
                    "<script type='text/javascript' charset='UTF-8' src='%datadir%\\getsummary.js" + "'></script>" +
                    "</head>";
            return
    "<head>" +
    "<LINK href='" + strCssFilePath + "' type='text/css' rel='stylesheet'>" +
    "</head>";
        }


        internal override int GetXmlHtml(BiblioInfo info,
    out string strXml,
    out string strHtml2,
    out string strError)
        {
            strError = "";
            strXml = "";
            strHtml2 = "";

            string strOldXml = "";
            string strNewXml = "";

            int nRet = 0;

            strOldXml = info.OldXml;
            strNewXml = info.NewXml;

            if (string.IsNullOrEmpty(strOldXml) == false
                && string.IsNullOrEmpty(strNewXml) == false)
            {
                // ����չʾ���� MARC ��¼����� HTML �ַ���
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = MarcDiff.DiffXml(
                    strOldXml,
                    strNewXml,
                    out strHtml2,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            else if (string.IsNullOrEmpty(strOldXml) == false
    && string.IsNullOrEmpty(strNewXml) == true)
            {
                strHtml2 = MarcUtil.GetHtmlOfXml(strOldXml,
                    false);
            }
            else if (string.IsNullOrEmpty(strOldXml) == true
                && string.IsNullOrEmpty(strNewXml) == false)
            {
                strHtml2 = MarcUtil.GetHtmlOfXml(strNewXml,
                    false);
            }

            strXml = MergeXml(strOldXml, strNewXml);

            return 0;
        }


        #endregion

        #region C# �ű�����



        // ����һ���µ� C# �ű��ļ�
        void menu_createMarcQueryCsFile_Click(object sender, EventArgs e)
        {
#if NO
            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ�����Ľű��ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = true;
            dlg.FileName = "";
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "C#�ű��ļ� (*.cs)|*.cs|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                PatronHost.CreateStartCsFile(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return;
            }

            System.Diagnostics.Process.Start("notepad.exe", dlg.FileName);

            this.m_strUsedMarcQueryFilename = dlg.FileName;
#endif
            CreateMarcQueryCsFile();
        }

        // ����һ���µ� C# �ű��ļ�
        /// <summary>
        /// ����һ���µ� C# �ű��ļ����ᵯ���Ի���ѯ���ļ�����
        /// �����е���� PatronHost ������
        /// </summary>
        public void CreateMarcQueryCsFile()
        {
            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ�����Ľű��ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = true;
            dlg.FileName = "";
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "C#�ű��ļ� (*.cs)|*.cs|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                PatronHost.CreateStartCsFile(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return;
            }

            System.Diagnostics.Process.Start("notepad.exe", dlg.FileName);

            this.m_strUsedMarcQueryFilename = dlg.FileName;
        }

        void menu_quickMarcQueryRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView_records.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫִ�� C# �ű�������";
                goto ERROR1;
            }

            // ������Ϣ����
            // ����Ѿ���ʼ�����򱣳�
            if (this.m_biblioTable == null)
                this.m_biblioTable = new Hashtable();

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ�� C# �ű��ļ�";
            dlg.FileName = this.m_strUsedMarcQueryFilename;
            dlg.Filter = "C# �ű��ļ� (*.cs)|*.cs|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.m_strUsedMarcQueryFilename = dlg.FileName;

            PatronHost host = null;
            Assembly assembly = null;

            nRet = PrepareMarcQuery(this.m_strUsedMarcQueryFilename,
                out assembly,
                out host,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            {
                host.MainForm = this.MainForm;
                host.UiForm = this;
                host.RecordPath = "";
                host.PatronDom = null;
                host.Changed = false;
                host.UiItem = null;

                StatisEventArgs args = new StatisEventArgs();
                host.OnInitial(this, args);
                if (args.Continue == ContinueType.SkipAll)
                    return;
                if (args.Continue == ContinueType.Error)
                {
                    strError = args.ParamString;
                    goto ERROR1;
                }
            }

            this.MainForm.OperHistory.AppendHtml("<div class='debug begin'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ��ʼִ�нű� " + dlg.FileName + "</div>");

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("������Զ��߼�¼ִ�� C# �ű� ...");
            stop.BeginLoop();

            this.EnableControls(false);

            this.listView_records.Enabled = false;
            try
            {
                if (stop != null)
                    stop.SetProgressRange(0, this.listView_records.SelectedItems.Count);

                host.CodeFileName = this.m_strUsedMarcQueryFilename;
                {
                    host.MainForm = this.MainForm;
                    host.RecordPath = "";
                    host.PatronDom = null;
                    host.Changed = false;
                    host.UiItem = null;

                    StatisEventArgs args = new StatisEventArgs();
                    host.OnBegin(this, args);
                    if (args.Continue == ContinueType.SkipAll)
                        return;
                    if (args.Continue == ContinueType.Error)
                    {
                        strError = args.ParamString;
                        goto ERROR1;
                    }
                }

                List<ListViewItem> items = new List<ListViewItem>();
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    if (string.IsNullOrEmpty(item.Text) == true)
                        continue;

                    items.Add(item);
                }

                ListViewPatronLoader loader = new ListViewPatronLoader(this.Channel,
                    stop,
                    items,
                    this.m_biblioTable);
                loader.DbTypeCaption = this.DbTypeCaption;

                int i = 0;
                foreach (LoaderItem item in loader)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null
                        && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    stop.SetProgressValue(i);

                    BiblioInfo info = item.BiblioInfo;

                    this.MainForm.OperHistory.AppendHtml("<div class='debug recpath'>" + HttpUtility.HtmlEncode(info.RecPath) + "</div>");

                    host.MainForm = this.MainForm;
                    host.RecordPath = info.RecPath;
                    host.PatronDom = new XmlDocument();
                    host.PatronDom.LoadXml(info.OldXml);
                    host.Changed = false;
                    host.UiItem = item.ListViewItem;

                    StatisEventArgs args = new StatisEventArgs();
                    host.OnRecord(this, args);
                    if (args.Continue == ContinueType.SkipAll)
                        break;
                    if (args.Continue == ContinueType.Error)
                    {
                        strError = args.ParamString;
                        goto ERROR1;
                    }

                    if (host.Changed == true)
                    {
                        string strXml = host.PatronDom.OuterXml;
                        if (info != null)
                        {
                            if (string.IsNullOrEmpty(info.NewXml) == true)
                                this.m_nChangedCount++;
                            info.NewXml = strXml;
                        }

                        item.ListViewItem.BackColor = SystemColors.Info;
                        item.ListViewItem.ForeColor = SystemColors.InfoText;
                    }

                    // ��ʾΪ��������ʽ
                    i++;
                }

                {
                    host.MainForm = this.MainForm;
                    host.RecordPath = "";
                    host.PatronDom = null;
                    host.Changed = false;
                    host.UiItem = null;

                    StatisEventArgs args = new StatisEventArgs();
                    host.OnEnd(this, args);
                    if (args.Continue == ContinueType.Error)
                    {
                        strError = args.ParamString;
                        goto ERROR1;
                    }
                }
            }
            finally
            {
                if (host != null)
                    host.FreeResources();

                this.listView_records.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                this.EnableControls(true);

                this.MainForm.OperHistory.AppendHtml("<div class='debug end'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ����ִ�нű� " + dlg.FileName + "</div>");
            }

            DoViewComment(false);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ����ѡ�����޸�
        void menu_clearSelectedChangedRecords_Click(object sender, EventArgs e)
        {
#if NO
            // TODO: ȷʵҪ?

            if (this.m_nChangedCount == 0)
            {
                MessageBox.Show(this, "��ǰû���κ��޸Ĺ�������ɶ���");
                return;
            }
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    ClearOneChange(item);
                }
            }
            finally
            {
                this.Cursor = oldCursor;
            }

            DoViewComment(false);
#endif
            ClearSelectedChangedRecords();
        }





        // ����ȫ���޸�
        void menu_clearAllChangedRecords_Click(object sender, EventArgs e)
        {
#if NO
            // TODO: ȷʵҪ?

            if (this.m_nChangedCount == 0)
            {
                MessageBox.Show(this, "��ǰû���κ��޸Ĺ�������ɶ���");
                return;
            }
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                foreach (ListViewItem item in this.listView_records.Items)
                {
                    ClearOneChange(item);
                }
            }
            finally
            {
                this.Cursor = oldCursor;
            }

            DoViewComment(false);
#endif
            ClearAllChangedRecords();
        }

        // ����ѡ��������޸�
        void menu_saveSelectedChangedRecords_Click(object sender, EventArgs e)
        {
#if NO
            // TODO: ȷʵҪ?

            if (this.m_nChangedCount == 0)
            {
                MessageBox.Show(this, "��ǰû���κ��޸Ĺ���������Ҫ����");
                return;
            }

            string strError = "";

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                items.Add(item);
            }

            int nRet = SaveChangedRecords(items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            strError = "������ɡ�\r\n\r\n" + strError;
            MessageBox.Show(this, strError);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
            SaveSelectedChangedRecords();
        }

        // ����ȫ���޸�����
        void menu_saveAllChangedRecords_Click(object sender, EventArgs e)
        {
#if NO
            // TODO: ȷʵҪ?

            if (this.m_nChangedCount == 0)
            {
                MessageBox.Show(this, "��ǰû���κ��޸Ĺ���������Ҫ����");
                return;
            }

            string strError = "";

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView_records.Items)
            {
                items.Add(item);
            }

            int nRet = SaveChangedRecords(items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            strError = "������ɡ�\r\n\r\n" + strError;
            MessageBox.Show(this, strError);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
            SaveAllChangedRecords();
        }


        // ׼���ű�����
        int PrepareMarcQuery(string strCsFileName,
            out Assembly assembly,
            out PatronHost host,
            out string strError)
        {
            assembly = null;
            strError = "";
            host = null;


            string strContent = "";
            Encoding encoding;
            // ���Զ�ʶ���ļ����ݵı��뷽ʽ�Ķ����ı��ļ�����ģ��
            // parameters:
            //      lMaxLength  װ�����󳤶ȡ�����������򳬹��Ĳ��ֲ�װ�롣���Ϊ-1����ʾ������װ�볤��
            // return:
            //      -1  ���� strError���з���ֵ
            //      0   �ļ������� strError���з���ֵ
            //      1   �ļ�����
            //      2   ��������ݲ���ȫ��
            int nRet = FileUtil.ReadTextFileContent(strCsFileName,
                -1,
                out strContent,
                out encoding,
                out strError);
            if (nRet == -1)
                return -1;

            string strWarningInfo = "";
            string[] saAddRef = {
                                    // 2011/4/20 ����
                                    "system.dll",
                                    "system.drawing.dll",
                                    "system.windows.forms.dll",
                                    "system.xml.dll",
                                    "System.Runtime.Serialization.dll",

									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
   									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Script.dll",  // 2011/8/25 ����
									Environment.CurrentDirectory + "\\digitalplatform.dp2.statis.dll",
                Environment.CurrentDirectory + "\\dp2circulation.exe",
            };

            // 2013/12/16
            nRet = ScriptManager.GetRef(strCsFileName,
    ref saAddRef,
    out strError);
            if (nRet == -1)
                goto ERROR1;

            // ֱ�ӱ��뵽�ڴ�
            // parameters:
            //		refs	���ӵ�refs�ļ�·����·���п��ܰ�����%installdir%
            nRet = ScriptManager.CreateAssembly_1(strContent,
                saAddRef,
                "",
                out assembly,
                out strError,
                out strWarningInfo);
            if (nRet == -1)
                goto ERROR1;

            // �õ�Assembly��Host������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                assembly,
                "dp2Circulation.PatronHost");
            if (entryClassType == null)
            {
                strError = strCsFileName + " ��û���ҵ� dp2Circulation.PatronHost ������";
                goto ERROR1;
            }

            // newһ��Host��������
            host = (PatronHost)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            return 0;
        ERROR1:
            return -1;
        }

        #endregion

        // return:
        //      -1  ����
        //      0   �û��ж�
        //      1   �ɹ�
        public int CreateReaderDetailExcelFile(List<string> reader_barcodes,
            bool bLaunchExcel,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ����� Excel �ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = true;
            // dlg.FileName = this.ExportExcelFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "Excel �ļ� (*.xlsx)|*.xlsx|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return 0;

            XLWorkbook doc = null;

            try
            {
                doc = new XLWorkbook(XLEventTracking.Disabled);
                File.Delete(dlg.FileName);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            IXLWorksheet sheet = null;
            sheet = doc.Worksheets.Add("���");

            // TODO: sheet ���԰��յ�λ�����֡����簴�հ༶

            stop.Style = StopStyle.EnableHalfStop;
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("������������ ...");
            stop.BeginLoop();

            EnableControls(false);
            this.listView_records.Enabled = false;

            try
            {
                if (stop != null)
                    stop.SetProgressRange(0, reader_barcodes.Count);

                // ÿ���е�����ַ���
                List<int> column_max_chars = new List<int>();

                // TODO: ��ı��⣬����ʱ��

                int nRowIndex = 3;  // �ճ�ǰ����
                int nColIndex = 1;

                int nReaderIndex = 0;
                foreach (string strBarcode in reader_barcodes)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                            return 0;
                    }

                    if (string.IsNullOrEmpty(strBarcode) == true)
                        continue;

                    // ��ö��߼�¼
                    byte[] baTimestamp = null;
                    string strOutputRecPath = "";

                    stop.SetMessage("���ڴ�����߼�¼ " + strBarcode + " ...");

                    string[] results = null;
                    long lRet = Channel.GetReaderInfo(
                        stop,
                        strBarcode,
                        "advancexml,advancexml_borrow_bibliosummary,advancexml_overdue_bibliosummary",
                        out results,
                        out strOutputRecPath,
                        out baTimestamp,
                        out strError);
                    if (lRet == -1)
                        return -1;

                    if (lRet == 0)
                        return -1;

                    if (lRet > 1)   // �����ܷ�����?
                    {
                        strError = "����֤����� " + strBarcode + " ���м�¼ " + lRet.ToString() + " ��������װ����߼�¼��\r\n\r\nע������һ�����ش�����ϵͳ����Ա�����ų���";
                        return -1;
                    }
                    if (results == null || results.Length < 1)
                    {
                        strError = "���ص�results��������";
                        return -1;
                    }
                    string strXml = results[0];

                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "װ�ض��߼�¼ XML �� DOM ʱ��������: " + ex.Message;
                        return -1;
                    }

                    // 
                    //
                    OutputReaderInfo(sheet,
            dom,
            nReaderIndex,
            ref nRowIndex,
            ref column_max_chars);

                    // ����ڽ����
                    OutputBorrows(sheet,
            dom,
            ref nRowIndex,
            ref column_max_chars);

                    // ���ΥԼ����
                    OutputOverdues(sheet,
            dom,
            ref nRowIndex,
            ref column_max_chars);

                    nRowIndex++;    // ����֮��Ŀ���

                    nReaderIndex++;
                    if (stop != null)
                        stop.SetProgressValue(nReaderIndex);
                }

                {
                    if (stop != null)
                        stop.SetMessage("���ڵ����п�� ...");
                    Application.DoEvents();

                    //double char_width = GetAverageCharPixelWidth(list);

                    // �ַ���̫����в�Ҫ�� width auto adjust
                    foreach (IXLColumn column in sheet.Columns())
                    {
                        int MAX_CHARS = 50;   // 60

                        int nIndex = column.FirstCell().Address.ColumnNumber - 1;
                        if (nIndex >= column_max_chars.Count)
                            break;
                        int nChars = column_max_chars[nIndex];

                        if (nIndex == 1)
                        {
                            column.Width = 10;
                            continue;
                        }

                        if (nIndex == 3)
                            MAX_CHARS = 50;
                        else
                            MAX_CHARS = 24;

                        if (nChars < MAX_CHARS)
                            column.AdjustToContents();
                        else
                            column.Width = Math.Min(MAX_CHARS, nChars);

                        //else
                        //    column.Width = (double)list.Columns[i].Width / char_width;  // Math.Min(MAX_CHARS, nChars);
                    }
                }

            }
            finally
            {
                EnableControls(true);
                this.listView_records.Enabled = true;

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
                stop.Style = StopStyle.None;

                if (doc != null)
                {
                    doc.SaveAs(dlg.FileName);
                    doc.Dispose();
                }

                if (bLaunchExcel)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(dlg.FileName);
                    }
                    catch
                    {

                    }
                }

            }
            return 0;
        }

        static string GetContactString(XmlDocument dom)
        {
            string strTel = DomUtil.GetElementText(dom.DocumentElement,
"tel");
            string strEmail = DomUtil.GetElementText(dom.DocumentElement,
"email");
            string strAddress = DomUtil.GetElementText(dom.DocumentElement,
"email");
            List<string> list = new List<string>();
            if (string.IsNullOrEmpty(strTel) == false)
                list.Add(strTel);
            if (string.IsNullOrEmpty(strEmail) == false)
                list.Add(strEmail);
            if (string.IsNullOrEmpty(strAddress) == false)
                list.Add(strAddress);
            return StringUtil.MakePathList(list, "; ");
        }

        static void OutputTitleLine(IXLWorksheet sheet,
            ref int nRowIndex,
            string strTitle,
            XLColor text_color,
            int nStartIndex,
            int nColumnCount)
        {
            // �������
            int nColIndex = nStartIndex;
            {
                IXLCell cell = sheet.Cell(nRowIndex, nColIndex).SetValue(strTitle);
                cell.Style.Alignment.WrapText = true;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = text_color;
                //cell.Style.Font.FontName = strFontName;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                // cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                nColIndex++;
            }

            // �ϲ�����
            {
                var rngData = sheet.Range(nRowIndex, nStartIndex, nRowIndex, nStartIndex + nColumnCount - 1);
                rngData.Merge();
                // rngData.LastColumn().Style.Border.RightBorder = XLBorderStyleValues.Hair;
            }

            nRowIndex++;
        }

        void OutputReaderInfo(IXLWorksheet sheet,
            XmlDocument dom,
            int nReaderIndex,
            ref int nRowIndex,
            ref List<int> column_max_chars)
        {
            string strReaderBarcode = DomUtil.GetElementText(dom.DocumentElement,
    "barcode");
            string strName = DomUtil.GetElementText(dom.DocumentElement,
                "name");
            string strDepartment = DomUtil.GetElementText(dom.DocumentElement,
"department");
            string strState = DomUtil.GetElementText(dom.DocumentElement,
    "state");
            string strCreateDate = ToLocalTime(DomUtil.GetElementText(dom.DocumentElement,
                "createDate"), "yyyy/MM/dd");
            string strExpireDate = ToLocalTime(DomUtil.GetElementText(dom.DocumentElement,
                "expireDate"), "yyyy/MM/dd");
            string strReaderType = DomUtil.GetElementText(dom.DocumentElement,
                "readerType");
            string strComment = DomUtil.GetElementText(dom.DocumentElement,
                "comment");

            List<IXLCell> cells = new List<IXLCell>();

            // �������
            // IXLCell cell_no = null;
            int nColIndex = 2;
            {
                IXLCell cell = sheet.Cell(nRowIndex, nColIndex).SetValue(nReaderIndex + 1);
                cell.Style.Alignment.WrapText = true;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 20;
                //cell.Style.Font.FontName = strFontName;
                //cell.Style.Alignment.Horizontal = alignments[nColIndex - 1];
                // cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cells.Add(cell);
                // cell_no = cell;
                nColIndex++;
            }

            // ����ַ���
            SetMaxChars(ref column_max_chars, 1, (nReaderIndex + 1).ToString().Length * 2);

            // ��ŵ��ұ�����
            {
                var rngData = sheet.Range(nRowIndex, 2, nRowIndex + 3, 2);
                rngData.Merge();
                rngData.LastColumn().Style.Border.RightBorder = XLBorderStyleValues.Hair;

                // ��һ������ĺ���
                rngData = sheet.Range(nRowIndex, 2, nRowIndex, 2 + 7 - 1);
                rngData.FirstRow().Style.Border.TopBorder = XLBorderStyleValues.Medium;
            }

#if NO
            // ����״̬ʱ��������ɫ
            if (string.IsNullOrEmpty(strState) == false)
            {
                var rngData = sheet.Range(nRowIndex, 2, nRowIndex + 3, 2 + 7 - 1);
                rngData.Style.Fill.BackgroundColor = XLColor.LightBrown;
            }
#endif

            int nFirstRow = nRowIndex;
            {
                List<string> subtitles = new List<string>();
                subtitles.Add("����");
                subtitles.Add("֤�����");
                subtitles.Add("����");
                subtitles.Add("��ϵ��ʽ");

                List<string> subcols = new List<string>();
                subcols.Add(strName);
                subcols.Add(strReaderBarcode);
                subcols.Add(strDepartment);
                subcols.Add(GetContactString(dom));


                for (int line = 0; line < subtitles.Count; line++)
                {
                    nColIndex = 3;
                    {
                        IXLCell cell = sheet.Cell(nRowIndex, nColIndex).SetValue(subtitles[line]);
                        cell.Style.Alignment.WrapText = true;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.DarkGray;
                        //cell.Style.Font.FontName = strFontName;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        // cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        nColIndex++;
                        cells.Add(cell);
                    }
                    {
                        IXLCell cell = sheet.Cell(nRowIndex, nColIndex).SetValue(subcols[line]);
                        cell.Style.Alignment.WrapText = true;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        //cell.Style.Font.FontName = strFontName;
                        //cell.Style.Alignment.Horizontal = alignments[nColIndex - 1];
                        if (line == 0)
                        {
                            cell.Style.Font.FontName = "΢���ź�";
                            cell.Style.Font.FontSize = 20;
                        }
                        nColIndex++;
                        cells.Add(cell);
                    }
                    nRowIndex++;
                }

                //    

                //var rngData = sheet.Range(cells[0], cells[cells.Count - 1]);
                //rngData.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;


            }

            nRowIndex = nFirstRow;
            {


                List<string> subtitles = new List<string>();
                subtitles.Add("״̬");
                subtitles.Add("��Ч��");
                subtitles.Add("�������");
                subtitles.Add("ע��");

                List<string> subcols = new List<string>();
                subcols.Add(strState);
                subcols.Add(strCreateDate+"~"+strExpireDate);
                subcols.Add(strReaderType);
                subcols.Add(strComment);

                for (int line = 0; line < subtitles.Count; line++)
                {
                    nColIndex = 7;
                    {
                        IXLCell cell = sheet.Cell(nRowIndex, nColIndex).SetValue(subtitles[line]);
                        cell.Style.Alignment.WrapText = true;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.DarkGray;
                        //cell.Style.Font.FontName = strFontName;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        // cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        nColIndex++;
                        cells.Add(cell);
                    }
                    {
                        IXLCell cell = sheet.Cell(nRowIndex, nColIndex).SetValue(subcols[line]);
                        cell.Style.Alignment.WrapText = true;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        if (line == 0)
                        {
                            cell.Style.Font.FontName = "΢���ź�";
                            cell.Style.Font.FontSize = 20;
                            if (string.IsNullOrEmpty(strState) == false)
                            {
                                cell.Style.Font.FontColor = XLColor.White;
                                cell.Style.Fill.BackgroundColor = XLColor.DarkRed;
                            }
                        }
                        nColIndex++;
                        cells.Add(cell);
                    }
                    nRowIndex++;
                }
            }



        }

        void OutputBorrows(IXLWorksheet sheet,
            XmlDocument dom,
            ref int nRowIndex,
            ref List<int> column_max_chars)
        {
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("borrows/borrow");
            if (nodes.Count == 0)
                return;

            int nStartRow = nRowIndex;

            OutputTitleLine(sheet,
ref nRowIndex,
"--- �ڽ� --- " + nodes.Count,
XLColor.DarkGreen,
2,
7);

            List<IXLCell> cells = new List<IXLCell>();

            // ����Ϣ�����еı���
            {
                List<string> titles = new List<string>();
                titles.Add("���");
                titles.Add("�������");
                titles.Add("��ĿժҪ");
                titles.Add("����ʱ��");
                titles.Add("����");
                titles.Add("Ӧ��ʱ��");
                titles.Add("�Ƿ���");

                int nColIndex = 2;
                foreach (string s in titles)
                {
                    IXLCell cell = sheet.Cell(nRowIndex, nColIndex).SetValue(s);
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.DarkGray;
                    //cell.Style.Font.FontName = strFontName;
                    //cell.Style.Alignment.Horizontal = alignments[nColIndex - 1];
                    nColIndex++;
                    cells.Add(cell);
                }
                nRowIndex++;
            }

            int nItemIndex = 0;
            foreach (XmlElement borrow in nodes)
            {
                string strItemBarcode = borrow.GetAttribute("barcode");
                string strBorrowDate = ToLocalTime(borrow.GetAttribute("borrowDate"), "yyyy-MM-dd HH:mm");
                string strBorrowPeriod = GetDisplayTimePeriodString(borrow.GetAttribute("borrowPeriod"));
                string strReturningDate = ToLocalTime(borrow.GetAttribute("returningDate"), "yyyy-MM-dd");
                string strRecPath = borrow.GetAttribute("recPath");
                string strIsOverdue = borrow.GetAttribute("isOverdue");
                bool bIsOverdue = DomUtil.IsBooleanTrue(strIsOverdue, false);
                string strOverdueInfo = borrow.GetAttribute("overdueInfo1");

                string strSummary = borrow.GetAttribute("summary");
#if NO
                            nRet = this.MainForm.GetBiblioSummary(strItemBarcode,
                                strRecPath, // strConfirmItemRecPath,
                                false,
                                out strSummary,
                                out strError);
                            if (nRet == -1)
                                strSummary = strError;
#endif

                List<string> cols = new List<string>();
                cols.Add((nItemIndex + 1).ToString());
                cols.Add(strItemBarcode);
                cols.Add(strSummary);
                cols.Add(strBorrowDate);
                cols.Add(strBorrowPeriod);
                cols.Add(strReturningDate);
                if (bIsOverdue)
                    cols.Add(strOverdueInfo);
                else
                    cols.Add("");

                int nColIndex = 2;
                foreach (string s in cols)
                {
                    // ͳ������ַ���
                    SetMaxChars(ref column_max_chars, nColIndex - 1, GetCharWidth(s));

                    IXLCell cell = null;
                    if (nColIndex == 2)
                    {
                        cell = sheet.Cell(nRowIndex, nColIndex).SetValue(nItemIndex + 1);
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else
                        cell = sheet.Cell(nRowIndex, nColIndex).SetValue(s);
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    //cell.Style.Font.FontName = strFontName;
                    //cell.Style.Alignment.Horizontal = alignments[nColIndex - 1];
                    nColIndex++;
                    cells.Add(cell);

                }

                // ���ڵ���Ϊ��ɫ����
                if (bIsOverdue)
                {
                    var line = sheet.Range(nRowIndex, 2, nRowIndex, 2 + cols.Count - 1);
                    line.Style.Fill.BackgroundColor = XLColor.Yellow;
                }

                nItemIndex++;
                nRowIndex++;
            }

            // ����Ϣ�����µ�����
            var rngData = sheet.Range(cells[0], cells[cells.Count - 1]);
            rngData.FirstRow().Style.Border.TopBorder = XLBorderStyleValues.Dotted;

#if NO
            // ��һ������ĺ���
            rngData = sheet.Range(cell_no, cells[cells.Count - 1]);
            rngData.FirstRow().Style.Border.TopBorder = XLBorderStyleValues.Medium;
#endif
            sheet.Rows(nStartRow + 1, nRowIndex-1).Group();
        }

        void OutputOverdues(IXLWorksheet sheet,
    XmlDocument dom,
    ref int nRowIndex,
    ref List<int> column_max_chars)
        {
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("overdues/overdue");
            if (nodes.Count == 0)
                return;

            int nStartRow = nRowIndex;

            OutputTitleLine(sheet,
                ref nRowIndex,
                "--- ���� --- " + nodes.Count,
                XLColor.DarkRed,
                2,
                6);

            int nRet = 0;

            List<IXLCell> cells = new List<IXLCell>();

            // ��Ŀ����
            {
                List<string> titles = new List<string>();
                titles.Add("���");
                titles.Add("�������");
                titles.Add("��ĿժҪ");
                titles.Add("˵��");
                titles.Add("���");
                titles.Add("ID");

#if NO
                titles.Add("��ͣ�������");
                titles.Add("�������");
                titles.Add("����");
                titles.Add("�յ�����");
#endif

                int nColIndex = 2;
                foreach (string s in titles)
                {
                    IXLCell cell = sheet.Cell(nRowIndex, nColIndex).SetValue(s);
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.DarkGray;
                    nColIndex++;
                    cells.Add(cell);
                }
                nRowIndex++;
            }

            int nItemIndex = 0;
            foreach (XmlElement borrow in nodes)
            {
                string strItemBarcode = borrow.GetAttribute("barcode");
                string strReason = borrow.GetAttribute("reason");
                string strPrice = borrow.GetAttribute("price");
                string strID = borrow.GetAttribute("id");
                string strRecPath = borrow.GetAttribute("recPath");

                string strSummary = borrow.GetAttribute("summary");
                if (string.IsNullOrEmpty(strItemBarcode) == false
                    && string.IsNullOrEmpty(strSummary) == true)
                {
                    string strError = "";
                    nRet = this.MainForm.GetBiblioSummary(strItemBarcode,
                        strRecPath, // strConfirmItemRecPath,
                        false,
                        out strSummary,
                        out strError);
                    if (nRet == -1)
                        strSummary = strError;
                }

                List<string> cols = new List<string>();
                cols.Add((nItemIndex + 1).ToString());
                cols.Add(strItemBarcode);
                cols.Add(strSummary);
                cols.Add(strReason);
                cols.Add(strPrice);
                cols.Add(strID);

                int nColIndex = 2;
                foreach (string s in cols)
                {
                    // ͳ������ַ���
                    SetMaxChars(ref column_max_chars, nColIndex - 1, GetCharWidth(s));

                    IXLCell cell = null;
                    if (nColIndex == 2)
                    {
                        cell = sheet.Cell(nRowIndex, nColIndex).SetValue(nItemIndex + 1);
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else
                        cell = sheet.Cell(nRowIndex, nColIndex).SetValue(s);
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    nColIndex++;
                    cells.Add(cell);
                }

                nItemIndex++;
                nRowIndex++;
            }

            // �������µ�����
            var rngData = sheet.Range(cells[0], cells[cells.Count - 1]);
            rngData.FirstRow().Style.Border.TopBorder = XLBorderStyleValues.Dotted;

            sheet.Rows(nStartRow + 1, nRowIndex - 1).Group();
        }

        static string GetDisplayTimePeriodString(string strText)
        {
            strText = strText.Replace("day", "��");

            return strText.Replace("hour", "Сʱ");
        }

        // ����һ���ַ����ġ������ַ���ȡ��������൱�����������ַ����
        static int GetCharWidth(string strText)
        {
            int result = 0;
            foreach (char c in strText)
            {
                result += StringUtil.IsHanzi(c) == true ? 2 : 1;
            }

            return result;
        }

        static void SetMaxChars(ref List<int> column_max_chars, int index, int chars)
        {
            // ȷ���ռ��㹻
            while (column_max_chars.Count < index + 1)
            {
                column_max_chars.Add(0);
            }

            // ͳ������ַ���
            int nOldChars = column_max_chars[index];
            if (chars > nOldChars)
            {
                column_max_chars[index] = chars;
            }
        }

        static string ToLocalTime(string strRfc1123, string strFormat)
        {
            try
            {
                return DateTimeUtil.Rfc1123DateTimeStringToLocal(strRfc1123, strFormat);
            }
            catch (Exception ex)
            {
                return "ʱ���ַ��� '" + strRfc1123 + "' ��ʽ����ȷ: " + ex.Message;
            }
        }
#if NO
        int GetEntityInfo(
            Stop stop,
            string strItemBarcode,
            out List<string> cols,
            out string strError)
        {
            cols = new List<string>();
            strError = "";

            string strXml = "";
            string strOutputRecPath = "";
            byte[] baTimestamp = null;
            string strBiblio = "";
            string strBiblioRecPath = "";
            long lRet = Channel.GetItemInfo(
stop,
strItemBarcode,
"xml",
out strXml,
out strOutputRecPath,
out baTimestamp,
"",
out strBiblio,
out strBiblioRecPath,
out strError);
            if (lRet == -1)
                return -1;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "װ�ز��¼ XML �� DOM ʱ��������: " + ex.Message;
                return -1;
            }

            // ��

            /*

����֤����� ���� ���� ��������

������� ��ĿժҪ ����ʱ�� ���� Ӧ��ʱ��

��ĿժҪ������չΪ ���� ���� ������ �������� ISBN
             * */
            cols.Add(DomUtil.GetElementText(dom.DocumentElement));

            return 0;
        }
#endif

    }

    /// <summary>
    /// ���� ListViewItem �����ö��߼�¼��Ϣ��ö����
    /// �������û������
    /// </summary>
    public class ListViewPatronLoader : IEnumerable
    {
        /// <summary>
        /// ���ݿ����ͣ�������ʾ�����֡�ȱʡΪ��
        /// </summary>
        public string DbTypeCaption
        {
            get;
            set;
        }
        /// <summary>
        /// ListViewItem ��������
        /// </summary>
        public List<ListViewItem> Items
        {
            get;
            set;
        }

        /// <summary>
        /// �����
        /// </summary>
        public Hashtable CacheTable
        {
            get;
            set;
        }

        BrowseLoader m_loader = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="channel">ͨѶͨ��</param>
        /// <param name="stop">ֹͣ����</param>
        /// <param name="items">ListViewItem ����</param>
        /// <param name="cacheTable">���ڻ���� Hashtable</param>
        public ListViewPatronLoader(LibraryChannel channel,
            Stop stop,
            List<ListViewItem> items,
            Hashtable cacheTable)
        {
            m_loader = new BrowseLoader();
            m_loader.Channel = channel;
            m_loader.Stop = stop;
            m_loader.Format = "id,xml,timestamp";
            // m_loader.GetBiblioInfoStyle = GetBiblioInfoStyle.Timestamp; // ������Ϣֻȡ�� timestamp

            this.Items = items;
            this.CacheTable = cacheTable;
        }

        /// <summary>
        /// ���ö�ٽӿ�
        /// </summary>
        /// <returns>ö�ٽӿ�</returns>
        public IEnumerator GetEnumerator()
        {
            Debug.Assert(m_loader != null, "");

            List<string> recpaths = new List<string>(); // ������ô�а�������Щ��¼
            foreach (ListViewItem item in this.Items)
            {
                string strRecPath = item.Text;
                Debug.Assert(string.IsNullOrEmpty(strRecPath) == false, "");

                BiblioInfo info = (BiblioInfo)this.CacheTable[strRecPath];
                if (info == null || string.IsNullOrEmpty(info.OldXml) == true)
                    recpaths.Add(strRecPath);
            }

            // ע�� Hashtable ����һ��ʱ���ڲ�Ӧ�ñ��޸ġ�������ƻ� m_loader �� items ֮���������Ӧ��ϵ

            m_loader.RecPaths = recpaths;

            var enumerator = m_loader.GetEnumerator();

            // ��ʼѭ��
            foreach (ListViewItem item in this.Items)
            {
                string strRecPath = item.Text;
                Debug.Assert(string.IsNullOrEmpty(strRecPath) == false, "");

                BiblioInfo info = (BiblioInfo)this.CacheTable[strRecPath];
                if (info == null || string.IsNullOrEmpty(info.OldXml) == true)
                {
                    if (m_loader.Stop != null)
                    {
                        m_loader.Stop.SetMessage("���ڻ�ȡ"+this.DbTypeCaption+"��¼ " + strRecPath);
                    }
                    bool bRet = enumerator.MoveNext();
                    if (bRet == false)
                    {
                        Debug.Assert(false, "��û�е���β, MoveNext() ��Ӧ�÷��� false");
                        // TODO: ��ʱ��Ҳ���Բ��÷���һ����û���ҵ��Ĵ������Ԫ��
                        yield break;
                    }

                    DigitalPlatform.CirculationClient.localhost.Record biblio = (DigitalPlatform.CirculationClient.localhost.Record)enumerator.Current;
                    Debug.Assert(biblio.Path == strRecPath, "m_loader �� items ��Ԫ��֮�� ��¼·�������ϸ��������Ӧ��ϵ");

                    // ��Ҫ���뻺��
                    if (info == null)
                    {
                        info = new BiblioInfo();
                        info.RecPath = biblio.Path;
                    }
                    info.OldXml = biblio.RecordBody.Xml;
                    info.Timestamp = biblio.RecordBody.Timestamp;
                    this.CacheTable[strRecPath] = info;
                    yield return new LoaderItem(info, item);
                }
                else
                    yield return new LoaderItem(info, item);
            }
        }
    }
}