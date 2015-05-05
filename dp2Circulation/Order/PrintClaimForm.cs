using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Web;   // HttpUtility

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Script;
using DigitalPlatform.IO;
using DigitalPlatform.Xml;
using DigitalPlatform.Marc;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Text;

using DigitalPlatform.dp2.Statis;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ��ӡ��ѯ����
    /// </summary>
    public partial class PrintClaimForm : MyForm
    {
        // װ������ʱ�ķ�ʽ
        string SourceStyle = "";    // "bibliodatabase" "bibliorecpathfile" "orderdatabase" "orderrecpathfile"

        // �������� OneSeller ����Ķ��ձ�
        Hashtable seller_table = new Hashtable();

        /// <summary>
        /// ���ڽ��д�ѯ����
        /// </summary>
        bool Running = false;

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;
#endif

        const int WM_LOADSIZE = API.WM_USER + 201;

        /// <summary>
        /// ���캯��
        /// </summary>
        public PrintClaimForm()
        {
            InitializeComponent();
        }

        private void PrintClaimForm_Load(object sender, EventArgs e)
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

            this.comboBox_source_type.Text = this.MainForm.AppInfo.GetString(
                "printclaimform",
                "publication_type",
                "����������");

            this.checkBox_source_guess.Checked = this.MainForm.AppInfo.GetBoolean(
    "printclaimform",
    "guess",
    true);

            this.radioButton_inputStyle_biblioRecPathFile.Checked = this.MainForm.AppInfo.GetBoolean(
    "printclaimform",
    "inputstyle_bibliorecpathfile",
    false);


            this.radioButton_inputStyle_biblioDatabase.Checked = this.MainForm.AppInfo.GetBoolean(
                "printclaimform",
                "inputstyle_bibliodatabase",
                true);


            // ����ļ�¼·���ļ���
            this.textBox_inputBiblioRecPathFilename.Text = this.MainForm.AppInfo.GetString(
                "printclaimform",
                "input_recpath_filename",
                "");


            // �������Ŀ����
            this.comboBox_inputBiblioDbName.Text = this.MainForm.AppInfo.GetString(
                "printclaimform",
                "input_bibliodbname",
                "<ȫ��>");

            // 
            this.radioButton_inputStyle_orderRecPathFile.Checked = this.MainForm.AppInfo.GetBoolean(
"printclaimform",
"inputstyle_orderrecpathfile",
false);


            this.radioButton_inputStyle_orderDatabase.Checked = this.MainForm.AppInfo.GetBoolean(
                "printclaimform",
                "inputstyle_orderdatabase",
                false);

            // ����Ķ������¼·���ļ���
            this.textBox_inputOrderRecPathFilename.Text = this.MainForm.AppInfo.GetString(
                "printclaimform",
                "input_orderrecpath_filename",
                "");


            // ����Ķ�������
            this.comboBox_inputOrderDbName.Text = this.MainForm.AppInfo.GetString(
                "printclaimform",
                "input_orderdbname",
                "");

            // *** ʱ�䷶Χҳ

            this.checkBox_timeRange_usePublishTime.Checked = this.MainForm.AppInfo.GetBoolean(
                "printclaimform",
                "time_range_userPublishTime",
                true);

            this.checkBox_timeRange_useOrderTime.Checked = this.MainForm.AppInfo.GetBoolean(
    "printclaimform",
    "time_range_userOrderTime",
    true);

            this.comboBox_timeRange_afterOrder.Text = this.MainForm.AppInfo.GetString(
    "printclaimform",
    "time_range_afterOrder",
    "");

            this.checkBox_timeRange_none.Checked = this.MainForm.AppInfo.GetBoolean(
    "printclaimform",
    "time_range_none",
    false);

            // ʱ�䷶Χ
            this.textBox_timeRange.Text = this.MainForm.AppInfo.GetString(
                "printclaimform",
                "time_range",
                "");

            SetInputPanelEnabled(true);
            SetTimeRangeState(true);

            comboBox_source_type_SelectedIndexChanged(this, null);

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);
        }

        private void PrintClaimForm_FormClosing(object sender, FormClosingEventArgs e)
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
        }

        private void PrintClaimForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            this.MainForm.AppInfo.SetString(
                "printclaimform",
                "publication_type",
                this.comboBox_source_type.Text);

            this.MainForm.AppInfo.SetBoolean(
                "printclaimform",
                "guess",
                this.checkBox_source_guess.Checked);

            this.MainForm.AppInfo.SetBoolean(
                "printclaimform",
                "inputstyle_bibliorecpathfile",
                this.radioButton_inputStyle_biblioRecPathFile.Checked);


            this.MainForm.AppInfo.SetBoolean(
                "printclaimform",
                "inputstyle_bibliodatabase",
                this.radioButton_inputStyle_biblioDatabase.Checked);



            // ����ļ�¼·���ļ���
            this.MainForm.AppInfo.SetString(
                "printclaimform",
                "input_recpath_filename",
                this.textBox_inputBiblioRecPathFilename.Text);

            // �������Ŀ����
            this.MainForm.AppInfo.SetString(
                "printclaimform",
                "input_bibliodbname",
                this.comboBox_inputBiblioDbName.Text);

            // 
            this.MainForm.AppInfo.SetBoolean(
"printclaimform",
"inputstyle_orderrecpathfile",
this.radioButton_inputStyle_orderRecPathFile.Checked);


            this.MainForm.AppInfo.SetBoolean(
                "printclaimform",
                "inputstyle_orderdatabase",
                this.radioButton_inputStyle_orderDatabase.Checked);

            // ����Ķ������¼·���ļ���
            this.MainForm.AppInfo.SetString(
                "printclaimform",
                "input_orderrecpath_filename",
                this.textBox_inputOrderRecPathFilename.Text);

            // ����Ķ�������
            this.MainForm.AppInfo.SetString(
                "printclaimform",
                "input_orderdbname",
                this.comboBox_inputOrderDbName.Text);

            // *** ʱ�䷶Χҳ

            this.MainForm.AppInfo.SetBoolean(
                "printclaimform",
                "time_range_userPublishTime",
                this.checkBox_timeRange_usePublishTime.Checked);

            this.MainForm.AppInfo.SetBoolean(
    "printclaimform",
    "time_range_userOrderTime",
    this.checkBox_timeRange_useOrderTime.Checked);

            this.MainForm.AppInfo.SetString(
    "printclaimform",
    "time_range_afterOrder",
    this.comboBox_timeRange_afterOrder.Text);

            this.MainForm.AppInfo.SetBoolean(
    "printclaimform",
    "time_range_none",
    this.checkBox_timeRange_none.Checked);

            // ʱ�䷶Χ
            this.MainForm.AppInfo.SetString(
                "printclaimform",
                "time_range",
                this.textBox_timeRange.Text);

            SaveSize();
        }

        void LoadSize()
        {
#if NO
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state");
#endif

            string strWidths = this.MainForm.AppInfo.GetString(
                "printclaimform",
                "list_origin_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_origin,
                    strWidths,
                    true);
            }

            strWidths = this.MainForm.AppInfo.GetString(
    "printclaimform",
    "list_merged_width",
    "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_merged,
                    strWidths,
                    true);
            }
        }

        void SaveSize()
        {
#if NO
            MainForm.AppInfo.SaveMdiChildFormStates(this,
                "mdi_form_state");
#endif

            /*
            // ���MDI�Ӵ��ڲ���MainForm�ո�׼���˳�ʱ��״̬���ָ�����Ϊ�˼���ߴ���׼��
            if (this.WindowState != this.MainForm.MdiWindowState)
                this.WindowState = this.MainForm.MdiWindowState;
             * */

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_origin);
            this.MainForm.AppInfo.SetString(
                "printclaimform",
                "list_origin_width",
                strWidths);

            strWidths = ListViewUtil.GetColumnWidthListString(this.listView_merged);
            this.MainForm.AppInfo.SetString(
                "printclaimform",
                "list_merged_width",
                strWidths);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_LOADSIZE:
                    LoadSize();
                    return;
            }
            base.DefWndProc(ref m);
        }

#if NO
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }

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
            this.comboBox_source_type.Enabled = bEnable;

            SetInputPanelEnabled(bEnable);

            /*
            this.checkBox_timeRange_usePublishTime.Enabled = bEnable;
            this.checkBox_timeRange_useOrderTime.Enabled = bEnable;
            this.checkBox_timeRange_none.Enabled = bEnable;
            this.comboBox_timeRange_afterOrder.Enabled = bEnable;
            this.textBox_timeRange.Enabled = bEnable;
            this.comboBox_timeRange_quickSet.Enabled = bEnable;
             * */
            SetInputPanelEnabled(bEnable);

            this.button_next.Enabled = bEnable;

            this.button_print.Enabled = bEnable;

            this.button_printOption.Enabled = bEnable;
        }

        public void SetBiblioRecPaths(List<string> recpaths)
        {
            this.textBox_inputBiblioRecPathFilename.Text = StringUtil.MakePathList(recpaths) + ","; // ȷ��������һ������
            this.radioButton_inputStyle_biblioRecPathFile.Checked = true;
        }

        public void SetOrderRecPaths(List<string> recpaths)
        {
            this.textBox_inputOrderRecPathFilename.Text = StringUtil.MakePathList(recpaths) + ","; // ȷ��������һ������
            this.radioButton_inputStyle_orderRecPathFile.Checked = true;
        }

        public PublicationType PublicationType
        {
            get
            {
                if (this.comboBox_source_type.Text == "ͼ��")
                    return PublicationType.Book;
                return PublicationType.Series;
            }
            set
            {
                if (value == PublicationType.Book)
                    this.comboBox_source_type.Text = "ͼ��";
                else
                    this.comboBox_source_type.Text = "����������";
            }
        }

        // ������
        /// <summary>
        /// ���뷽ʽ
        /// </summary>
        public PrintClaimInputStyle InputStyle
        {
            get
            {
                if (this.radioButton_inputStyle_biblioRecPathFile.Checked == true)
                    return PrintClaimInputStyle.BiblioRecPathFile;
                else if (this.radioButton_inputStyle_biblioDatabase.Checked == true)
                    return PrintClaimInputStyle.BiblioDatabase;
                else if (this.radioButton_inputStyle_orderRecPathFile.Checked == true)
                    return PrintClaimInputStyle.OrderRecPathFile;
                else
                    return PrintClaimInputStyle.OrderDatabase;
            }
        }

        // ������ҳ�������ʾһ���ı���
        void WriteTextLines(string strHtml)
        {
            /*
            string[] parts = strHtml.Replace("\r\n", "\n").Split(new char[] {'\n'});
            StringBuilder s = new StringBuilder(4096);
            foreach (string p in parts)
            {
                s.Append(HttpUtility.HtmlEncode(p) + "\r\n");
            }
            Global.WriteHtml(this.webBrowser_errorInfo,
                s.ToString());
             * */
            Global.WriteHtml(this.webBrowser_errorInfo,
                HttpUtility.HtmlEncode(strHtml));
        }

        // ������ҳ�������ʾһ�� HTML �ַ���
        void WriteHtml(string strHtml)
        {
            Global.WriteHtml(this.webBrowser_errorInfo,
                strHtml);
        }

        static void OldSetHtmlString(WebBrowser webBrowser,
            string strHtml)
        {
            // ���� �������ã������Զ�<body onload='...'>�¼�
            HtmlDocument doc = webBrowser.Document;

            if (doc == null)
            {
                webBrowser.Navigate("about:blank");
                doc = webBrowser.Document;
                Debug.Assert(doc != null, "doc��Ӧ��Ϊnull");
            }

            doc = doc.OpenNew(true);
            doc.Write(strHtml);
        }

        // 2012//30
        // 
        /// <summary>
        /// ���һ����Ŀ��¼������ȫ��������¼·��
        /// </summary>
        /// <param name="sw">Ҧд��� StreamWriter ����</param>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����>=0: ����ʵ��д���·������</returns>
        int GetChildOrderRedPath(StreamWriter sw,
            string strBiblioRecPath,
            out string strError)
        {
            strError = "";

            long lStart = 0;
            long lResultCount = 0;
            long lCount = -1;

            for (; ; )
            {
                EntityInfo[] orders = null;

                long lRet = Channel.GetOrders(
                    stop,
                    strBiblioRecPath,
                    lStart,
                    lCount,
                    "onlygetpath",
                    this.Lang,
                    out orders,
                    out strError);
                if (lRet == -1)
                    return -1;

                if (lRet == 0)
                    return 0;

                lResultCount = lRet;

                Debug.Assert(orders != null, "");

                for (int i = 0; i < orders.Length; i++)
                {
                    if (orders[i].ErrorCode != ErrorCodeValue.NoError)
                    {
                        strError = "·��Ϊ '" + orders[i].OldRecPath + "' �Ķ�����¼װ���з�������: " + orders[i].ErrorInfo;  // NewRecPath
                        return -1;
                    }

                    sw.Write(orders[i].OldRecPath + "\r\n");
                }

                lStart += orders.Length;
                if (lStart >= lResultCount)
                    break;
            }

            return (int)lStart;
        }

        // 2012/8/30
        // ���������⣬��������¼·��������ļ�
        int SearchOrderRecPath(
            string strRecPathFilename,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(this.comboBox_inputOrderDbName.Text) == true)
            {
                strError = "��δָ����������";
                return -1;
            }

            // �����ļ�
            StreamWriter sw = new StreamWriter(strRecPathFilename,
                false,	// append
                System.Text.Encoding.UTF8);

            try
            {

                long lRet = 0;

                lRet = Channel.SearchOrder(stop,
                     this.comboBox_inputOrderDbName.Text,
                     "",
                     -1,    // nPerMax
                     "__id",
                     "left",
                     this.Lang,
                     null,   // strResultSetName
                     "",    // strSearchStyle
                     "", // strOutputStyle
                     out strError);
                if (lRet == -1)
                    goto ERROR1;
                long lHitCount = lRet;

                long lStart = 0;
                long lCount = lHitCount;

                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }
                    }

                    lRet = Channel.GetSearchResult(
                        stop,
                        null,   // strResultSetName
                        lStart,
                        lCount,
                        "id",   // "id,cols",
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (lRet == 0)
                    {
                        strError = "δ����";
                        goto ERROR1;
                    }

                    Debug.Assert(searchresults != null, "");

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        string strOrderRecPath = searchresults[i].Path;

                        sw.WriteLine(strOrderRecPath);
                    }

                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    stop.SetMessage("���м�¼ " + lHitCount.ToString() + " �����ѻ�ü�¼ " + lStart.ToString() + " ��");

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                }
            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }

            return 0;
        ERROR1:
            return -1;
        }

        // 2012/8/30
        // ������Ŀ���¼·���ļ����õ������Ķ������¼·��
        // parameters:
        //      strBiblioRecPathFilename    ��Ŀ���¼·���ļ�����Ҳ�����Ƕ��ż������Ŀ���¼·���б�
        int GetOrderRecPath(string strBiblioRecPathFilename,
            string strOutputFilename,
            out string strError)
        {
            strError = "";

            // 2015/1/28
            string strTempFileName = "";
            if (strBiblioRecPathFilename.IndexOf(",") != -1)
            {
                // ��������д��һ����ʱ�ļ�
                strTempFileName = this.MainForm.GetTempFileName("pcf_");

                using (StreamWriter sw = new StreamWriter(strTempFileName, false, Encoding.UTF8))
                {
                    sw.Write(strBiblioRecPathFilename.Replace(",", "\r\n"));
                }
                strBiblioRecPathFilename = strTempFileName;
            }

            // �����ļ�
            using (StreamWriter sw = new StreamWriter(strOutputFilename,
                false,	// append
                System.Text.Encoding.UTF8))
            {

                try
                {
                    using (StreamReader sr = new StreamReader(strBiblioRecPathFilename, Encoding.UTF8))
                    {
                        for (; ; )
                        {
                            string strBiblioRecPath = sr.ReadLine();

                            if (strBiblioRecPath == null)
                                break;
                            strBiblioRecPath = strBiblioRecPath.Trim();
                            if (String.IsNullOrEmpty(strBiblioRecPath) == true)
                                continue;

                            // �����Ŀ��·��
                            {
                                string strDbName = Global.GetDbName(strBiblioRecPath);
                                BiblioDbProperty prop = this.MainForm.GetBiblioDbProperty(strDbName);

                                if (prop == null)
                                {
                                    strError = "��¼·�� '" + strBiblioRecPath + "' �е����ݿ��� '" + strDbName + "' ������Ŀ����";
                                    return -1;
                                }

                                if (string.IsNullOrEmpty(prop.IssueDbName) == false)
                                {
                                    // �ڿ���
                                    if (this.comboBox_source_type.Text != "����������")
                                    {
                                        strError = "��¼·�� '" + strBiblioRecPath + "' �е���Ŀ���� '" + strDbName + "' ����ͼ������";
                                        return -1;
                                    }
                                }
                                else
                                {
                                    // ͼ���
                                    if (this.comboBox_source_type.Text != "ͼ��")
                                    {
                                        strError = "��¼·�� '" + strBiblioRecPath + "' �е���Ŀ���� '" + strDbName + "' �����ڿ�����";
                                        return -1;
                                    }
                                }
                            }

                            // ���һ����Ŀ��¼������ȫ��������¼·��
                            int nRet = GetChildOrderRedPath(sw,
                                strBiblioRecPath,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }

                    }
                }
                catch (Exception ex)
                {
                    strError = "�����ļ� " + strBiblioRecPathFilename + " ʱ����: " + ex.Message;
                    return -1;
                }
            }

            if (string.IsNullOrEmpty(strTempFileName) == false)
                File.Delete(strTempFileName);

            return 0;
        }

        // 2012/8/3-
        // ������Ŀ�⣬ ����ض����κţ�����������Ŀ��¼��Ȼ�������Ķ�����¼·��������ļ�
        int SearchBiblioOrderRecPath(
            string strBatchNo,
            string strRecPathFilename,
            out string strError)
        {
            strError = "";

#if NO
            string strDbNameList = GetBiblioDbNameList();

            if (String.IsNullOrEmpty(strDbNameList) == true)
            {
                if (this.comboBox_inputBiblioDbName.Text == "<ȫ��>"
                    || this.comboBox_inputBiblioDbName.Text.ToLower() == "<all>"
                    || this.comboBox_inputBiblioDbName.Text == "")
                {
                    strError = "���������� '" + this.comboBox_source_type.Text + "' ������ƥ�����Ŀ��";
                    return -1;
                }

                strError = "��δָ����Ŀ����";
                return -1;
            }
#endif
            if (String.IsNullOrEmpty(this.comboBox_inputBiblioDbName.Text) == true)
            {
                strError = "��δָ����Ŀ����";
                return -1;
            }

            if (this.comboBox_inputBiblioDbName.Text == "<ȫ��>")
            {
                if (this.comboBox_source_type.Text == "ͼ��")
                    this.comboBox_inputBiblioDbName.Text = "<ȫ��ͼ��>";
                else
                    this.comboBox_inputBiblioDbName.Text = "<ȫ���ڿ�>";
            }

            // �����ļ�
            StreamWriter sw = new StreamWriter(strRecPathFilename,
                false,	// append
                System.Text.Encoding.UTF8);

            try
            {

                long lRet = 0;
                string strQueryXml = "";

                // ��ָ�����κţ���ζ���ض���ȫ������
                if (String.IsNullOrEmpty(strBatchNo) == true)
                {
                    lRet = Channel.SearchBiblio(stop,
                         this.comboBox_inputBiblioDbName.Text,
                         "",
                         -1,    // nPerMax
                         "recid",
                         "left",
                         this.Lang,
                         null,   // strResultSetName
                         "",    // strSearchStyle
                         "", // strOutputStyle
                         out strQueryXml,
                         out strError);
                    if (lRet == -1)
                        goto ERROR1;
                }
                else
                {
                    // ָ�����κš��ض��⡣
                    lRet = Channel.SearchBiblio(stop,
                         this.comboBox_inputBiblioDbName.Text,
                         strBatchNo,
                         -1,    // nPerMax
                         "batchno",
                         "exact",
                         this.Lang,
                         null,   // strResultSetName
                         "",    // strSearchStyle
                         "", // strOutputStyle
                         out strQueryXml,
                         out strError);
                    if (lRet == -1)
                        goto ERROR1;
                }

                long lHitCount = lRet;

                long lStart = 0;
                long lCount = lHitCount;


                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }
                    }


                    lRet = Channel.GetSearchResult(
                        stop,
                        null,   // strResultSetName
                        lStart,
                        lCount,
                        "id",   // "id,cols",
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (lRet == 0)
                    {
                        strError = "δ����";
                        goto ERROR1;
                    }

                    Debug.Assert(searchresults != null, "");

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        string strBiblioRecPath = searchresults[i].Path;
                        int nRet = GetChildOrderRedPath(sw,
                            strBiblioRecPath,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }

                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    stop.SetMessage("���м�¼ " + lHitCount.ToString() + " �����ѻ�ü�¼ " + lStart.ToString() + " ��");

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                }
            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }

            return 0;
        ERROR1:
            return -1;
        }

        // 2012/8/30
        // ���충�����¼·���ļ�
        // return:
        //      0   ��ͨ����
        //      1   Ҫȫ���ж�
        int MakeOrderRecPathFile(
            out string strTempRecPathFilename,
            out string strError)
        {
            strError = "";
            strTempRecPathFilename = "";

            int nRet = 0;

            // TODO: ��ʱ�ļ��Ƿ�ɾ��?
            // ��¼·����ʱ�ļ�
            strTempRecPathFilename = this.MainForm.GetTempFileName("pcf_");

            // ������Ŀ��
            if (this.InputStyle == PrintClaimInputStyle.BiblioDatabase)
            {
                this.SourceStyle = "bibliodatabase";

                nRet = SearchBiblioOrderRecPath(
                    "", // this.tabComboBox_inputBatchNo.Text,
                    strTempRecPathFilename,
                    out strError);
                if (nRet == -1)
                    return -1;
                // strAccessPointName = "��¼·��";
            }
            else if (this.InputStyle == PrintClaimInputStyle.BiblioRecPathFile)
            {
                this.SourceStyle = "bibliorecpathfile";

                // ������Ŀ���¼·���ļ����õ������Ķ������¼·��
                nRet = GetOrderRecPath(this.textBox_inputBiblioRecPathFilename.Text,
                    strTempRecPathFilename,
                    out strError);
                if (nRet == -1)
                    return -1;

                // strAccessPointName = "��¼·��";
            }
            else if (this.InputStyle == PrintClaimInputStyle.OrderDatabase)
            {
                this.SourceStyle = "orderdatabase";

                // ���������⣬��������¼·��������ļ�
                nRet = SearchOrderRecPath(
                    strTempRecPathFilename,
                    out strError);
                if (nRet == -1)
                    return -1;

            }
            else if (this.InputStyle == PrintClaimInputStyle.OrderRecPathFile)
            {
                strError = "�������������ڴ�������";
                return -1;
            }
            else
            {
                Debug.Assert(false, "");
            }

            return 0;
        }

        // ׼��ʱ����˲���
        int PrepareTimeFilter(out TimeFilter filter,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            filter = new TimeFilter();

            try
            {
                filter.OrderTimeDelta = GetOrderTimeDelta(this.comboBox_timeRange_afterOrder.Text);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            if (this.checkBox_timeRange_none.Checked == true)
            {
                filter.Style = "none";
                return 0;
            }

            if (this.checkBox_timeRange_useOrderTime.Checked == false
                && this.checkBox_timeRange_usePublishTime.Checked == false)
            {
                strError = "Ҫ�����ʱ�� �� Ҫ�󶩹�ʱ������ָ����Χ����Ҫ����ѡ��һ��(����ѡ���ˡ������ˡ�)";
                return -1;
            }

            if (string.IsNullOrEmpty(this.textBox_timeRange.Text) == true)
            {
                strError = "��δ�趨ʱ�䷶Χֵ";
                return -1;
            }

            if (checkBox_timeRange_usePublishTime.Checked == true
                && this.checkBox_timeRange_useOrderTime.Checked == true)
                filter.Style = "both";
            else if (checkBox_timeRange_usePublishTime.Checked == true
                && this.checkBox_timeRange_useOrderTime.Checked == false)
                filter.Style = "publishtime";
            else if (checkBox_timeRange_usePublishTime.Checked == false
    && this.checkBox_timeRange_useOrderTime.Checked == true)
                filter.Style = "ordertime";
            else
            {
                Debug.Assert(false, "");
            }

            // ȱʡЧ������Զ�Ĺ�ȥ-��������
            DateTime startTime = new DateTime(0);
            DateTime endTime = DateTime.Now;

            nRet = Global.ParseTimeRangeString(this.textBox_timeRange.Text,
                true,
                out startTime,
                out endTime,
                out strError);
            if (nRet == -1)
                return -1;

            filter.StartTime = startTime;
            filter.EndTime = endTime;

            return 0;
        }

        // �������ֵ
        static double GetValue(string strValue)
        {
            if (strValue == "һ")
                return 1;
            if (strValue == "��")
                return 2;
            if (strValue == "��")
                return 2;
            if (strValue == "��")
                return 3;
            if (strValue == "��")
                return 4;
            if (strValue == "��")
                return 5;
            if (strValue == "��")
                return 6;
            if (strValue == "��")
                return 7;
            if (strValue == "��")
                return 8;
            if (strValue == "��")
                return 9;
            if (strValue == "ʮ")
                return 10;
            if (strValue == "��")
                return 0;
            if (strValue == "��")
                return 0.5;

            double v = 0;
            if (double.TryParse(strValue, out v) == false)
                throw new Exception("���� '" + strValue + "' ��ʽ����");

            return v;
        }

        static TimeSpan GetOrderTimeDelta(string strNameParam)
        {
            if (string.IsNullOrEmpty(strNameParam) == true)
                return new TimeSpan();

            if (strNameParam == "����")
                return new TimeSpan();

            string strName = strNameParam.Replace("��", "").Trim();
            strName = strName.Replace("��", "").Trim();

            if (strName.IndexOf("��") != -1)
            {
                string strNumber = strName.Replace("��", "").Trim();

                double v = GetValue(strNumber);

                return new TimeSpan((int)((double)365 * v), 0, 0, 0);
            }

            if (strName.IndexOf("��") != -1)
            {
                string strNumber = strName.Replace("��", "").Trim();

                double v = GetValue(strNumber);

                if (v >= 12)
                {
                    v = ((v / (double)12) * (double)365) + (v % (double)12) * (double)30.5;
                }
                else
                {
                    v = v * (double)30.5;
                }

                return new TimeSpan((int)v, 0, 0, 0);

            }
            if (strName.IndexOf("��") != -1)
            {
                string strNumber = strName.Replace("��", "").Trim();

                double v = GetValue(strNumber);

                return new TimeSpan((int)(v * 24), 0, 0);

            }
            if (strName.IndexOf("��") != -1)
            {
                string strNumber = strName.Replace("��", "").Trim();

                double v = GetValue(strNumber);

                return new TimeSpan((int)((double)7 * v), 0, 0, 0);
            }

            throw new Exception("�޷�ʶ���ʱ�䳤�� '" + strNameParam + "'");
        }

#if NO
        static TimeSpan GetOrderTimeDelta(string strName)
        {
            if (string.IsNullOrEmpty(strName) == true)
                return new TimeSpan();

            // ����
            if (strName == "����")
                return new TimeSpan();

            // һ�ܺ�
            if (strName == "һ�ܺ�")
                return new TimeSpan(7, 0, 0, 0);

            if (strName == "�����")
                return new TimeSpan(182, 0, 0, 0);

            if (strName == "һ���")
                return new TimeSpan(365, 0, 0, 0);

            if (strName == "�����")
                return new TimeSpan(2 * 365, 0, 0, 0);
            if (strName == "�����")
                return new TimeSpan(3 * 365, 0, 0, 0);

            if (strName == "�����")
                return new TimeSpan(4 * 365, 0, 0, 0);

            throw new Exception("����ʶ���ʱ�䳤�� '" + strName + "'");

        }
#endif

        // ��ÿ����Ŀ��¼����ѭ��
        // return:
        //      0   ��ͨ����
        //      1   Ҫȫ���ж�
        int DoLoop(out string strError,
            out string strWarning)
        {
            strError = "";
            strWarning = "";

            int nRet = 0;
            // long lRet = 0;

            // bool bSyntaxWarned = false;
            // bool bFilterWarned = false;

            this.seller_table.Clear();

#if NO
            // ȱʡЧ������Զ�Ĺ�ȥ-��������
            DateTime startTime = new DateTime(0);
            DateTime endTime = DateTime.Now;

            if (this.textBox_timeRange.Text != "")
            {
                nRet = Global.ParseTimeRangeString(this.textBox_timeRange.Text,
                    out startTime,
                    out endTime,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
#endif
            TimeFilter filter = null;
            // ׼��ʱ����˲���
            nRet = PrepareTimeFilter(out filter,
                out strError);
            if (nRet == -1)
            {
                strError = "ʱ�䷶Χ���ò���ȷ: " + strError;
                return -1;
            }

            // ���������Ϣ�����в��������
            OldSetHtmlString(this.webBrowser_errorInfo, "<pre>");

            // ��¼·����ʱ�ļ�
            string strTempRecPathFilename = "";
            string strInputFilename = "";

            // string strInputFileName = "";   // �ⲿָ���������ļ���Ϊ������ļ����߼�¼·���ļ���ʽ
            string strAccessPointName = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                if (this.InputStyle == PrintClaimInputStyle.OrderRecPathFile)
                {
                    this.SourceStyle = "orderrecpathfile";
                    if (this.textBox_inputOrderRecPathFilename.Text.IndexOf(",") == -1)
                        strInputFilename = this.textBox_inputOrderRecPathFilename.Text;
                    else
                    {
                        // ��������д��һ����ʱ�ļ�
                        strTempRecPathFilename = this.MainForm.GetTempFileName("pcf_");

                        using (StreamWriter sw = new StreamWriter(strTempRecPathFilename, false, Encoding.UTF8))
                        {
                            sw.Write(this.textBox_inputOrderRecPathFilename.Text.Replace(",", "\r\n"));
                        }
                        strInputFilename = strTempRecPathFilename;
                    }
                }
                else
                {
                    nRet = MakeOrderRecPathFile(
            out strTempRecPathFilename,
            out strError);
                    if (nRet == -1)
                        return -1;
                    strInputFilename = strTempRecPathFilename;
                }

                StreamReader sr = null;

                try
                {
                    sr = new StreamReader(strInputFilename, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    strError = "���ļ� " + strInputFilename + " ʧ��: " + ex.Message;
                    return -1;
                }

                IssueHost issue_host = null;
                BookHost order_host = null;

                if (this.comboBox_source_type.Text == "����������"
                    || this.comboBox_source_type.Text == "�ڿ�")
                {
                    issue_host = new IssueHost();
                    issue_host.Channel = this.Channel;
                    issue_host.Stop = this.stop;
                }
                else
                {
                    order_host = new BookHost();
                    order_host.Channel = this.Channel;
                    order_host.Stop = this.stop;
                }

                /*
                this.progressBar_records.Minimum = 0;
                this.progressBar_records.Maximum = (int)sr.BaseStream.Length;
                this.progressBar_records.Value = 0;
                 * */

                stop.SetProgressRange(0, sr.BaseStream.Length);

                try
                {
                    // int nCount = 0;

                    for (int nRecord=0; ;nRecord++)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                DialogResult result = MessageBox.Show(this,
                                    "׼���жϡ�\r\n\r\nȷʵҪ�ж�ȫ������? (Yes ȫ���жϣ�No �ж�ѭ�������Ǽ�����β����Cancel �����жϣ���������)",
                                    "bibliostatisform",
                                    MessageBoxButtons.YesNoCancel,
                                    MessageBoxIcon.Question,
                                    MessageBoxDefaultButton.Button3);

                                if (result == DialogResult.Yes)
                                {
                                    strError = "�û��ж�";
                                    return -1;
                                }
                                if (result == DialogResult.No)
                                    return 0;   // ��װloop��������

                                stop.Continue(); // ����ѭ��
                            }
                        }

                        string strOrderRecPath = sr.ReadLine();

                        if (strOrderRecPath == null)
                            break;

                        strOrderRecPath = strOrderRecPath.Trim();

                        if (String.IsNullOrEmpty(strOrderRecPath) == true)
                            continue;

                        if (strOrderRecPath[0] == '#')
                            continue;   // ע����

                        // ��鶩����·��
                        {
                            string strDbName = Global.GetDbName(strOrderRecPath);
                            string strBiblioDbName = this.MainForm.GetBiblioDbNameFromOrderDbName(strDbName);
                            if (string.IsNullOrEmpty(strBiblioDbName) == true)
                            {
                                strError = "��¼·�� '" + strOrderRecPath + "' �е����ݿ��� '" + strDbName + "' ���Ƕ�������";
                                return -1;
                            }
                            BiblioDbProperty prop = this.MainForm.GetBiblioDbProperty(strBiblioDbName);
                            if (prop == null)
                            {
                                strError = "���ݿ��� '" + strBiblioDbName + "' ������Ŀ����";
                                return -1;
                            }

                            if (string.IsNullOrEmpty(prop.IssueDbName) == false)
                            {
                                // �ڿ���
                                if (this.comboBox_source_type.Text != "����������")
                                {
                                    strError = "��¼·�� '" + strOrderRecPath + "' �еĶ������� '" + strDbName + "' ����ͼ������";
                                    return -1;
                                }
                            }
                            else
                            {
                                // ͼ���
                                if (this.comboBox_source_type.Text != "ͼ��")
                                {
                                    strError = "��¼·�� '" + strOrderRecPath + "' �еĶ������� '" + strDbName + "' �����ڿ�����";
                                    return -1;
                                }
                            }
                        }

                        stop.SetMessage("���ڻ�ȡ�� " + (nRecord + 1).ToString() + " ��������¼��" + strAccessPointName + "Ϊ " + strOrderRecPath);

                        stop.SetProgressValue(sr.BaseStream.Position);
                        // this.progressBar_records.Value = (int)sr.BaseStream.Position;

                        // �����Ŀ��¼
                        // string strOutputRecPath = "";
                        // byte[] baTimestamp = null;

#if NO
                        string strAccessPoint = "";
                        if (this.InputStyle == PrintClaimInputStyle.BiblioDatabase)
                            strAccessPoint = strOrderRecPath;
                        else if (this.InputStyle == PrintClaimInputStyle.BiblioRecPathFile)
                            strAccessPoint = strOrderRecPath;
                        else
                        {
                            Debug.Assert(false, "");
                        }
#endif

#if NO
                        string strBiblio = "";
                        // string strBiblioRecPath = "";

                        // Result.Value -1���� 0û���ҵ� 1�ҵ� >1���ж���1��
                        lRet = Channel.GetBiblioInfo(
                            stop,
                            strAccessPoint,
                            "", // strBiblioXml
                            "xml",   // strResultType
                            out strBiblio,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "�����Ŀ��¼ " + strAccessPoint + " ʱ��������: " + strError;
                            this.WriteHtml(strError + "\r\n");
                            continue;
                        }

                        if (lRet == 0)
                        {
                            strError = "��Ŀ��¼" + strAccessPointName + " " + strOrderRecPath + " ��Ӧ��XML����û���ҵ���";
                            this.WriteHtml(strError + "\r\n");
                            continue;
                        }

                        if (lRet > 1)
                        {
                            strError = "��Ŀ��¼" + strAccessPointName + " " + strOrderRecPath + " ��Ӧ���ݶ���һ����";
                            this.WriteHtml(strError + "\r\n");
                            continue;
                        }

                        string strXml = "";

                        strXml = strBiblio;


                        // �����Ƿ���ϣ��ͳ�Ƶķ�Χ��
                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(strXml);
                        }
                        catch (Exception ex)
                        {
                            strError = "��Ŀ��¼װ��DOM��������: " + ex.Message;
                            continue;
                        }

                        // strXml��Ϊ��Ŀ��¼
                        string strBiblioDbName = Global.GetDbName(strOrderRecPath);

                        string strSyntax = this.MainForm.GetBiblioSyntax(strBiblioDbName);
                        if (String.IsNullOrEmpty(strSyntax) == true)
                            strSyntax = "unimarc";

                        if (strSyntax == "usmarc" || strSyntax == "unimarc")
                        {
                            // ��XML��Ŀ��¼ת��ΪMARC��ʽ
                            string strOutMarcSyntax = "";
                            string strMarc = "";

                            // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
                            // parameters:
                            //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
                            //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
                            //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
                            nRet = MarcUtil.Xml2Marc(strXml,
                                false,
                                "", // strMarcSyntax
                                out strOutMarcSyntax,
                                out strMarc,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            if (String.IsNullOrEmpty(strOutMarcSyntax) == false)
                            {
                                if (strOutMarcSyntax != strSyntax
                                    && bSyntaxWarned == false)
                                {
                                    strWarning += "��Ŀ��¼ " + strOrderRecPath + " ��syntax '" + strOutMarcSyntax + "' �����������ݿ� '" + strBiblioDbName + "' �Ķ���syntax '" + strSyntax + "' ��һ��\r\n";
                                    bSyntaxWarned = true;
                                }
                            }


                        }
                        else
                        {
                            // ����MARC��ʽ
                        }
#endif

                        if (this.comboBox_source_type.Text == "����������"
                            || this.comboBox_source_type.Text == "�ڿ�")
                        {
                            // �����ڿ�
                            // return:
                            //      0   δ����
                            //      1   �Ѵ���
                            nRet = ProcessIssues(
                                issue_host,
                                filter,
                                strOrderRecPath);
                        }
                        else
                        {
                            // ����ͼ��
                            // return:
                            //      0   δ����
                            //      1   �Ѵ���
                            nRet = ProcessBooks(
                                order_host,
                                filter,
                                strOrderRecPath);
                        }

                        if (nRet == 0)
                            continue;

                        /*
                        // ����һ����Ŀ��¼�Լ����µĶ������ڼ�¼
                        nRet = host.LoadIssueRecords(strRecPath,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "��IssueHost��װ���¼ " + strRecPath + " �������ڼ�¼ʱ��������:" + strError;
                            this.WriteHtml(strError + "\r\n");
                            continue;
                        }

                        nRet = host.LoadOrderRecords(strRecPath,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "��IssueHost��װ���¼ " + strRecPath + " ������������¼ʱ��������: " + strError;
                            this.WriteHtml(strError + "\r\n");
                            continue;
                        }

                        nRet = host.CreateIssues(out strError);
                        if (nRet == -1)
                        {
                            strError = "��IssueHost��CreateIssues() " + strRecPath + " error: " + strError;
                            this.WriteHtml(strError + "\r\n");
                            continue;
                        }

                        if (nRet != 0)
                        {
                            this.WriteHtml(host.BiblioRecPath + "\r\n" + host.DumpIssue() + "\r\n");
                        }
                         * */
                        /*
                        CONTINUE:
                        nCount++;
                         * */
                    }

                }
                finally
                {

                    if (sr != null)
                        sr.Close();
                }
            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                if (string.IsNullOrEmpty(strTempRecPathFilename) == false)
                    File.Delete(strTempRecPathFilename);
            }

            return 0;
        }

        // return:
        //      0   δ����
        //      1   �Ѵ���
        int ProcessIssues(
            IssueHost issue_host,
            TimeFilter filter,
            string strOrderRecPath)
        {
            string strError = "";

            this.WriteTextLines("*** " + strOrderRecPath + " ");

            long lRet = 0;

            StringBuilder debugInfo = null;
            if (this.checkBox_debug.Checked == true)
                debugInfo = new StringBuilder();

            // ��ʼ���ؼ�
            // return:
            //      -1  error
            //      0   û�ж�����Ϣ
            //      1   ��ʼ���ɹ�
            int nRet = issue_host.Initial(strOrderRecPath,
                this.checkBox_source_guess.Checked,
                ref debugInfo,
                out strError);
            if (nRet == -1)
            {
                this.WriteTextLines(strError + "\r\n");
                return 0;
            }

            if (debugInfo != null)
                this.WriteTextLines(debugInfo.ToString());

            if (nRet == 0)
            {
                if (string.IsNullOrEmpty(strError) == false)
                    this.WriteTextLines(strError + "\r\n");
                return 0;
            }

            List<IssueInfo> issue_infos = null;
            // ����ڸ�����Ϣ
            // ÿ��һ�У����������������˻���
            // return:
            //      -1  error
            //      0   û���κ���Ϣ
            //      >0  ��Ϣ����
            nRet = issue_host.GetIssueInfo(
                out issue_infos,
                out strError);
            if (nRet == -1)
            {
                this.WriteTextLines(strError + "\r\n");
                return 0;
            }

            if (nRet != 0)
            {
                this.WriteTextLines("������ " + issue_host.BiblioRecPath + "\r\n" + IssueHost.DumpIssueInfos(issue_infos) + "\r\n");

                // ��IssueInfo�����д���ָ��ʱ�䷶Χ��������Ƴ�
                string strDebugInfo = "";
                IssueHost.RemoveOutofTimeRangeIssueInfos(ref issue_infos,
                    filter,
                    out strDebugInfo);
                this.WriteTextLines(strDebugInfo + "\r\n");

                // ȥ��issue_infos���Ѿ��������
                IssueHost.RemoveArrivedIssueInfos(ref issue_infos);
                if (issue_infos.Count > 0)
                {
                    string strSummary = "";
                    string strISBnISSN = "";
                    string strTitle = "";

                    {
                        string[] formats = new string[3];
                        formats[0] = "summary";
                        formats[1] = "@isbnissn";
                        formats[2] = "@title";

                        string[] results = null;
                        byte[] timestamp = null;

                        Debug.Assert(String.IsNullOrEmpty(strOrderRecPath) == false, "strRecPathֵ����Ϊ��");

                        lRet = Channel.GetBiblioInfos(
                            stop,
                            issue_host.BiblioRecPath, // strOrderRecPath,
                            "",
                            formats,
                            out results,
                            out timestamp,
                            out strError);
                        if (lRet == -1 || lRet == 0)
                        {
                            if (lRet == 0 && String.IsNullOrEmpty(strError) == true)
                                strError = "��Ŀ��¼ '" + issue_host.BiblioRecPath + "' ������";

                            strSummary = "�����ĿժҪʱ��������: " + strError;
                        }
                        else
                        {
                            Debug.Assert(results != null && results.Length == 3, "results�������3��Ԫ��");
                            strSummary = results[0];
                            strISBnISSN = results[1];
                            strTitle = results[2];
                        }
                    }

                    List<NamedIssueInfoCollection> series_results = null;
                    // ��IssueInfo��������������������Ϊ����������
                    series_results = IssueHost.SortIssueInfo(issue_infos);

                    for (int i = 0; i < series_results.Count; i++)
                    {
                        NamedIssueInfoCollection list = series_results[i];

                        string strAddressXml = "";

                        // ������̵�ַ
                        if (list.Seller == "ֱ��"
                            || list.Seller == "����"
                            || list.Seller == "��")
                        {
                            strAddressXml = issue_host.GetAddressXml(list.Seller);

                            if (String.IsNullOrEmpty(strAddressXml) == true)
                            {
                                this.WriteTextLines("*** �������� " + list.Seller + " δ���ڶ�����Ϣ�з������̵�ַ\r\n");
                                return 1;
                            }

                            string strSellerName = "";

                            nRet = BuildSellerName(
                                list.Seller,
                                strTitle,
                                strAddressXml,
                                out strSellerName,
                                out strError);

                            list.Seller = strSellerName;
                        }


                        OneSeries series = new OneSeries();
                        series.BiblioRecPath = issue_host.BiblioRecPath;    //  strOrderRecPath;
                        series.BiblioSummary = strSummary;
                        series.ISSN = strISBnISSN;
                        series.Title = strTitle;
                        series.IssueInfos.AddRange(list);

                        OneSeller seller = GetOneSeller(list.Seller);

                        OneSeries exist_series = seller.FindOneSeries(series.BiblioRecPath);
                        if (exist_series == null)
                            seller.Add(series);
                        else
                            exist_series.MergeIssueInfos(series);

                        // �״λ�����̵�ַ
                        if (String.IsNullOrEmpty(seller.AddressXml) == true)
                            seller.AddressXml = strAddressXml;
                    }
                }
            }

            return 1;
        }

        // return:
        //      0   δ����
        //      1   �Ѵ���
        int ProcessBooks(
            BookHost book_host,
            TimeFilter filter,
            string strOrderRecPath)
        {
            string strError = "";

            this.WriteTextLines("*** " + strOrderRecPath + " ");

            long lRet = 0;

            StringBuilder debugInfo = null;
            if (this.checkBox_debug.Checked == true)
                debugInfo = new StringBuilder();

            // ��ʼ���ؼ�
            // return:
            //      -1  error
            //      0   û�ж�����Ϣ
            //      1   ��ʼ���ɹ�
            int nRet = book_host.Initial(strOrderRecPath,
                out strError);
            if (nRet == -1)
            {
                this.WriteTextLines(strError + "\r\n");
                return 0;
            }

            if (nRet == 0)
            {
                if (string.IsNullOrEmpty(strError) == false)
                    this.WriteTextLines(strError + "\r\n");
                return 0;
            }

            List<IssueInfo> issue_infos = null;
            string strWarning = "";
            // ����ڸ�����Ϣ
            // ÿ��һ�У����������������˻���
            // return:
            //      -1  error
            //      0   û���κ���Ϣ
            //      >0  ��Ϣ����
            nRet = book_host.GetOrderInfo(
                filter,
                out issue_infos,
                out strError,
                out strWarning);
            if (nRet == -1)
            {
                this.WriteTextLines(strError + "\r\n");
                return 0;
            }

            if (string.IsNullOrEmpty(strWarning) == false)
            {
                this.WriteTextLines("����: " + strWarning + "\r\n");
            }

            if (nRet != 0)
            {
                this.WriteTextLines("������ " + book_host.BiblioRecPath + "\r\n" + BookHost.DumpOrderInfos(issue_infos) + "\r\n");

                // ��IssueInfo�����д���ָ��ʱ�䷶Χ��������Ƴ�
                string strDebugInfo = "";
                BookHost.RemoveOutofTimeRangeOrderInfos(ref issue_infos,
                    filter,
                    out strDebugInfo);
                this.WriteTextLines(strDebugInfo + "\r\n");

                // ȥ��issue_infos���Ѿ��������
                BookHost.RemoveArrivedOrderInfos(ref issue_infos);
                if (issue_infos.Count > 0)
                {

                    string strSummary = "";
                    string strISBnISSN = "";
                    string strTitle = "";

                    {
                        string[] formats = new string[3];
                        formats[0] = "summary";
                        formats[1] = "@isbnissn";
                        formats[2] = "@title";

                        string[] results = null;
                        byte[] timestamp = null;

                        Debug.Assert(String.IsNullOrEmpty(book_host.BiblioRecPath) == false, "book_host.BiblioRecPathֵ����Ϊ��");

                        lRet = Channel.GetBiblioInfos(
                            stop,
                            book_host.BiblioRecPath,    // strOrderRecPath,
                            "",
                            formats,
                            out results,
                            out timestamp,
                            out strError);
                        if (lRet == -1 || lRet == 0)
                        {
                            if (lRet == 0 && String.IsNullOrEmpty(strError) == true)
                                strError = "��Ŀ��¼ '" + book_host.BiblioRecPath + "' ������";

                            strSummary = "�����ĿժҪʱ��������: " + strError;
                        }
                        else
                        {
                            Debug.Assert(results != null && results.Length == 3, "results�������3��Ԫ��");
                            strSummary = results[0];
                            strISBnISSN = results[1];
                            strTitle = results[2];
                        }
                    }

                    List<NamedIssueInfoCollection> series_results = null;
                    // ��IssueInfo��������������������Ϊ����������
                    series_results = BookHost.SortOrderInfo(issue_infos);

                    for (int i = 0; i < series_results.Count; i++)
                    {
                        NamedIssueInfoCollection list = series_results[i];

                        string strAddressXml = "";

                        // ������̵�ַ
                        if (list.Seller == "ֱ��"
                            || list.Seller == "����"
                            || list.Seller == "��")
                        {
                            strAddressXml = book_host.GetAddressXml(list.Seller);

                            if (String.IsNullOrEmpty(strAddressXml) == true)
                            {
                                this.WriteTextLines("*** �������� " + list.Seller + " δ���ڶ�����Ϣ�з������̵�ַ\r\n");
                                return 1;
                            }

                            string strSellerName = "";

                            nRet = BuildSellerName(
                                list.Seller,
                                strTitle,
                                strAddressXml,
                                out strSellerName,
                                out strError);

                            list.Seller = strSellerName;
                        }


                        OneSeries series = new OneSeries();
                        series.BiblioRecPath = book_host.BiblioRecPath; // strOrderRecPath;
                        series.BiblioSummary = strSummary;
                        series.ISSN = strISBnISSN;
                        series.Title = strTitle;
                        series.IssueInfos.AddRange(list);

                        OneSeller seller = GetOneSeller(list.Seller);
                        OneSeries exist_series = seller.FindOneSeries(series.BiblioRecPath);
                        if (exist_series == null)
                            seller.Add(series);
                        else
                            exist_series.MergeIssueInfos(series);

                        // �״λ�����̵�ַ
                        if (String.IsNullOrEmpty(seller.AddressXml) == true)
                            seller.AddressXml = strAddressXml;
                    }
                }
            }

            return 1;
        }

        // parameters:
        //      strStyle    ������ Ϊ ֱ��/����/��
        //      strTitle    �ڿ���
        static int BuildSellerName(
            string strStyle,
            string strTitle,
            string strAddressXml,
            out string strSellerName,
            out string strError)
        {
            strError = "";
            strSellerName = "";

            if (String.IsNullOrEmpty(strAddressXml) == true)
                return 0;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root/>");
            try
            {
                dom.DocumentElement.InnerXml = strAddressXml;
            }
            catch (Exception ex)
            {
                strError = "��ַXML��ʽ����ȷ: " + ex.Message;
                return -1;
            }

            string strZipcode = DomUtil.GetElementText(dom.DocumentElement,
                "zipcode");
            string strAddress = DomUtil.GetElementText(dom.DocumentElement,
                "address");
            string strDepartment = DomUtil.GetElementText(dom.DocumentElement,
                "department");
            string strName = DomUtil.GetElementText(dom.DocumentElement,
                "name");

            if (strStyle == "ֱ��")
            {
                if (String.IsNullOrEmpty(strDepartment) == false)
                    strSellerName = strDepartment
                        + "|" + strZipcode
                        + "|" + strAddress
                        + "|" + strDepartment;
                else
                    strSellerName = "��" + strTitle + "���༭��"
                        + "|" + strZipcode
                        + "|" + strAddress
                        + "|" + strDepartment;

            }
            else if (strStyle == "����")
            {
                if (String.IsNullOrEmpty(strDepartment) == false)
                    strSellerName = strDepartment
                        + "|" + strZipcode
                        + "|" + strAddress
                        + "|" + strDepartment;
                else
                    strSellerName = "��" + strTitle + "���ṩ��"
                        + "|" + strZipcode
                        + "|" + strAddress
                        + "|" + strDepartment;
            }
            else if (strStyle == "��")
            {
                if (String.IsNullOrEmpty(strName) == false)
                {
                    strSellerName = strName
                        + "|" + strZipcode
                        + "|" + strAddress
                        + "|" + strDepartment
                        + "|" + strName;
                }
                else
                {
                    strSellerName = strDepartment
                        + "|" + strZipcode
                        + "|" + strAddress
                        + "|" + strDepartment
                        + "|" + strName;
                }
            }
            else
            {
                strError = "δ֪�������� '" + strStyle + "'";
                return -1;
            }

            return 1;
        }

        OneSeller GetOneSeller(string strSeller)
        {
            object o = this.seller_table[strSeller];
            if (o != null)
                return (OneSeller)o;

            // ����һ���µĶ���
            OneSeller seller = new OneSeller();
            seller.Seller = strSeller;
            this.seller_table[strSeller] = seller;
            return seller;
        }

        // ���ݳ��������ͣ�������ݿ����б�
        string GetBiblioDbNameList()
        {
            // һ���Եĵ�������
            if (this.comboBox_inputBiblioDbName.Text != "<ȫ��>"
                && this.comboBox_inputBiblioDbName.Text.ToLower() != "<all>"
                && this.comboBox_inputBiblioDbName.Text != "")
                return this.comboBox_inputBiblioDbName.Text;

            List<string> dbnames = new List<string>();
            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty prop = this.MainForm.BiblioDbProperties[i];

                    if (String.IsNullOrEmpty(prop.OrderDbName) == true)
                        continue;   // û�ж������ܵ���Ŀ�ⲻ�ڿ���֮��

                    if (this.comboBox_source_type.Text == "ͼ��")
                    {
                        if (String.IsNullOrEmpty(prop.IssueDbName) == false)
                            continue;
                    }
                    else
                    {
                        // �ڿ���Ҫ���ڿ�����Ϊ��

                        if (String.IsNullOrEmpty(prop.IssueDbName) == true)
                            continue;
                    }

                    dbnames.Add(prop.DbName);
                }
            }

            if (dbnames.Count == 0)
                return "";

            return StringUtil.MakePathList(dbnames, ",");
        }

        // ��������ض����κţ�����������Ŀ��¼·��(������ļ�)
        int SearchBiblioRecPath(
            string strBatchNo,
            string strRecPathFilename,
            out string strError)
        {
            strError = "";

            string strDbNameList = GetBiblioDbNameList();

            if (String.IsNullOrEmpty(strDbNameList) == true)
            {
                if (this.comboBox_inputBiblioDbName.Text == "<ȫ��>"
                    || this.comboBox_inputBiblioDbName.Text.ToLower() == "<all>"
                    || this.comboBox_inputBiblioDbName.Text == "")
                {
                    strError = "���������� '" + this.comboBox_source_type.Text + "' ������ƥ�����Ŀ��";
                    return -1;
                }

                strError = "��δָ����Ŀ����";
                return -1;
            }

            // �����ļ�
            StreamWriter sw = new StreamWriter(strRecPathFilename,
                false,	// append
                System.Text.Encoding.UTF8);

            try
            {
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڼ��� ...");
                stop.BeginLoop();

                EnableControls(false);

                try
                {
                    long lRet = 0;
                    string strQueryXml = "";

                    // ��ָ�����κţ���ζ���ض���ȫ������
                    if (String.IsNullOrEmpty(strBatchNo) == true)
                    {
                        lRet = Channel.SearchBiblio(stop,
                             strDbNameList,
                             "",
                             -1,    // nPerMax
                             "recid",
                             "left",
                             this.Lang,
                             null,   // strResultSetName
                             "",    // strSearchStyle
                             "", // strOutputStyle
                             out strQueryXml,
                             out strError);
                        if (lRet == -1)
                            goto ERROR1;
                    }
                    else
                    {
                        // ָ�����κš��ض��⡣
                        lRet = Channel.SearchBiblio(stop,
                             strDbNameList,
                             strBatchNo,
                             -1,    // nPerMax
                             "batchno",
                             "exact",
                             this.Lang,
                             null,   // strResultSetName
                             "",    // strSearchStyle
                             "", // strOutputStyle
                             out strQueryXml,
                             out strError);
                        if (lRet == -1)
                            goto ERROR1;
                    }

                    long lHitCount = lRet;

                    long lStart = 0;
                    long lCount = lHitCount;


                    DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                    // װ�������ʽ
                    for (; ; )
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                goto ERROR1;
                            }
                        }


                        lRet = Channel.GetSearchResult(
                            stop,
                            null,   // strResultSetName
                            lStart,
                            lCount,
                            "id",   // "id,cols",
                            this.Lang,
                            out searchresults,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;

                        if (lRet == 0)
                        {
                            strError = "δ����";
                            goto ERROR1;
                        }

                        Debug.Assert(searchresults != null, "");


                        // ����������
                        for (int i = 0; i < searchresults.Length; i++)
                        {
                            sw.Write(searchresults[i].Path + "\r\n");
                        }


                        lStart += searchresults.Length;
                        lCount -= searchresults.Length;

                        stop.SetMessage("���м�¼ " + lHitCount.ToString() + " �����ѻ�ü�¼ " + lStart.ToString() + " ��");

                        if (lStart >= lHitCount || lCount <= 0)
                            break;
                    }

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
                if (sw != null)
                    sw.Close();
            }

            return 0;
        ERROR1:
            return -1;
        }

        void SetInputPanelEnabled(bool bEnable)
        {
            if (bEnable == true)
            {
                this.radioButton_inputStyle_biblioDatabase.Enabled = true;
                this.radioButton_inputStyle_biblioRecPathFile.Enabled = true;
                this.radioButton_inputStyle_orderDatabase.Enabled = true;
                this.radioButton_inputStyle_orderRecPathFile.Enabled = true;

                if (this.radioButton_inputStyle_biblioRecPathFile.Checked == true)
                {
                    Debug.Assert(this.radioButton_inputStyle_biblioRecPathFile.Checked == true, "");
                    Debug.Assert(this.radioButton_inputStyle_biblioDatabase.Checked == false, "");
                    Debug.Assert(this.radioButton_inputStyle_orderRecPathFile.Checked == false, "");
                    Debug.Assert(this.radioButton_inputStyle_orderDatabase.Checked == false, "");


                    this.textBox_inputBiblioRecPathFilename.Enabled = true;
                    this.button_findInputBiblioRecPathFilename.Enabled = true;

                    this.comboBox_inputBiblioDbName.Enabled = false;

                    this.textBox_inputOrderRecPathFilename.Enabled = false;
                    this.button_findInputOrderRecPathFilename.Enabled = false;

                    this.comboBox_inputOrderDbName.Enabled = false;
                }
                else if (this.radioButton_inputStyle_biblioDatabase.Checked == true)
                {
                    Debug.Assert(this.radioButton_inputStyle_biblioRecPathFile.Checked == false, "");
                    Debug.Assert(this.radioButton_inputStyle_biblioDatabase.Checked == true, "");
                    Debug.Assert(this.radioButton_inputStyle_orderRecPathFile.Checked == false, "");
                    Debug.Assert(this.radioButton_inputStyle_orderDatabase.Checked == false, "");

                    this.textBox_inputBiblioRecPathFilename.Enabled = false;
                    this.button_findInputBiblioRecPathFilename.Enabled = false;

                    this.comboBox_inputBiblioDbName.Enabled = true;

                    this.textBox_inputOrderRecPathFilename.Enabled = false;
                    this.button_findInputOrderRecPathFilename.Enabled = false;

                    this.comboBox_inputOrderDbName.Enabled = false;
                }
                else if (this.radioButton_inputStyle_orderRecPathFile.Checked == true)
                {
                    Debug.Assert(this.radioButton_inputStyle_biblioRecPathFile.Checked == false, "");
                    Debug.Assert(this.radioButton_inputStyle_biblioDatabase.Checked == false, "");
                    Debug.Assert(this.radioButton_inputStyle_orderRecPathFile.Checked == true, "");
                    Debug.Assert(this.radioButton_inputStyle_orderDatabase.Checked == false, "");

                    this.textBox_inputBiblioRecPathFilename.Enabled = false;
                    this.button_findInputBiblioRecPathFilename.Enabled = false;

                    this.comboBox_inputBiblioDbName.Enabled = false;

                    this.textBox_inputOrderRecPathFilename.Enabled = true;
                    this.button_findInputOrderRecPathFilename.Enabled = true;

                    this.comboBox_inputOrderDbName.Enabled = false;
                }
                else if (this.radioButton_inputStyle_orderDatabase.Checked == true)
                {
                    Debug.Assert(this.radioButton_inputStyle_biblioRecPathFile.Checked == false, "");
                    Debug.Assert(this.radioButton_inputStyle_biblioDatabase.Checked == false, "");
                    Debug.Assert(this.radioButton_inputStyle_orderRecPathFile.Checked == false, "");
                    Debug.Assert(this.radioButton_inputStyle_orderDatabase.Checked == true, "");

                    this.textBox_inputBiblioRecPathFilename.Enabled = false;
                    this.button_findInputBiblioRecPathFilename.Enabled = false;

                    this.comboBox_inputBiblioDbName.Enabled = false;

                    this.textBox_inputOrderRecPathFilename.Enabled = false;
                    this.button_findInputOrderRecPathFilename.Enabled = false;

                    this.comboBox_inputOrderDbName.Enabled = true;
                }
                else
                {
                    // Debug.Assert(false, "�������ߵ�����");
                }
            }
            else
            {
                this.radioButton_inputStyle_biblioDatabase.Enabled = false;
                this.radioButton_inputStyle_biblioRecPathFile.Enabled = false;
                this.radioButton_inputStyle_orderDatabase.Enabled = false;
                this.radioButton_inputStyle_orderRecPathFile.Enabled = false;

                this.textBox_inputBiblioRecPathFilename.Enabled = false;
                this.button_findInputBiblioRecPathFilename.Enabled = false;

                this.comboBox_inputBiblioDbName.Enabled = false;

                this.textBox_inputOrderRecPathFilename.Enabled = false;
                this.button_findInputOrderRecPathFilename.Enabled = false;

                this.comboBox_inputOrderDbName.Enabled = false;
            }
        }

        private void radioButton_inputStyle_biblioRecPathFile_CheckedChanged(object sender, EventArgs e)
        {
            SetInputPanelEnabled(true);
        }

        private void radioButton_inputStyle_biblioDatabase_CheckedChanged(object sender, EventArgs e)
        {
            SetInputPanelEnabled(true);
        }

        private void comboBox_inputBiblioDbName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_inputBiblioDbName.Items.Count > 0)
                return;

            // this.comboBox_inputBiblioDbName.Items.Add("<ȫ��>");
            if (this.comboBox_source_type.Text == "ͼ��")
                this.comboBox_inputBiblioDbName.Items.Add("<ȫ��ͼ��>");
            else
                this.comboBox_inputBiblioDbName.Items.Add("<ȫ���ڿ�>");

            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty prop = this.MainForm.BiblioDbProperties[i];

                    if (String.IsNullOrEmpty(prop.OrderDbName) == true)
                        continue;   // û�ж������ܵ���Ŀ�ⲻ�ڿ���֮��

                    if (this.comboBox_source_type.Text == "ͼ��")
                    {
                        if (String.IsNullOrEmpty(prop.IssueDbName) == false)
                            continue;
                    }
                    else
                    {
                        // �ڿ���Ҫ���ڿ�����Ϊ��

                        if (String.IsNullOrEmpty(prop.IssueDbName) == true)
                            continue;
                    }

                    this.comboBox_inputBiblioDbName.Items.Add(prop.DbName);
                }
            }

        }

        private void comboBox_source_type_TextChanged(object sender, EventArgs e)
        {
            if (this.comboBox_inputBiblioDbName.Items.Count > 0)
            {
                this.comboBox_inputBiblioDbName.Items.Clear();
            }


#if NO
            // ���һ�µ�ǰ�Ѿ�ѡ������Ŀ�����ͳ����������Ƿ�ì��
             if (this.MainForm.BiblioDbProperties != null)
             {
                 for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                 {
                     BiblioDbProperty prop = this.MainForm.BiblioDbProperties[i];

                     if (strText == prop.DbName)
                     {
                         if (this.comboBox_source_type.Text == "ͼ��"
                             && String.IsNullOrEmpty(prop.IssueDbName) == false)
                         {
                             this.comboBox_inputBiblioDbName.Text = "";
                             break;
                         }
                         else if (this.comboBox_source_type.Text == "����������"
                             && String.IsNullOrEmpty(prop.IssueDbName) == true)
                         {
                             this.comboBox_inputBiblioDbName.Text = "";
                             break;
                         }
                     }
                 }
            }
#endif

            // ���һ�µ�ǰ�Ѿ�ѡ���Ķ��������ͳ����������Ƿ�ì��
            if (this.comboBox_source_type.Text == "ͼ��"
    && this.comboBox_inputBiblioDbName.Text == "<ȫ���ڿ�>")
            {
                this.comboBox_inputBiblioDbName.Text = "<ȫ��ͼ��>";
                return;
            }
            if (this.comboBox_source_type.Text == "����������"
&& this.comboBox_inputBiblioDbName.Text == "<ȫ��ͼ��>")
            {
                this.comboBox_inputBiblioDbName.Text = "<ȫ���ڿ�>";
                return;
            }
            string strText = this.comboBox_inputBiblioDbName.Text;
            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty prop = this.MainForm.BiblioDbProperties[i];

                    if (strText == prop.DbName)
                    {
                        if (this.comboBox_source_type.Text == "ͼ��"
                            && String.IsNullOrEmpty(prop.IssueDbName) == false)
                        {
                            this.comboBox_inputBiblioDbName.Text = "";
                            break;
                        }
                        else if (this.comboBox_source_type.Text == "����������"
                            && String.IsNullOrEmpty(prop.IssueDbName) == true)
                        {
                            this.comboBox_inputBiblioDbName.Text = "";
                            break;
                        }
                    }
                }
            }

            //

             if (this.comboBox_inputOrderDbName.Items.Count > 0)
             {
                 this.comboBox_inputOrderDbName.Items.Clear();
             }

             strText = this.comboBox_inputOrderDbName.Text;

             // ���һ�µ�ǰ�Ѿ�ѡ���Ķ��������ͳ����������Ƿ�ì��
             if (this.comboBox_source_type.Text == "ͼ��"
     && this.comboBox_inputOrderDbName.Text == "<ȫ���ڿ�>")
             {
                 this.comboBox_inputOrderDbName.Text = "<ȫ��ͼ��>";
                 return;
             }
             if (this.comboBox_source_type.Text == "����������"
&& this.comboBox_inputOrderDbName.Text == "<ȫ��ͼ��>")
             {
                 this.comboBox_inputOrderDbName.Text = "<ȫ���ڿ�>";
                 return;
             }
             if (this.MainForm.BiblioDbProperties != null)
             {
                 for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                 {
                     BiblioDbProperty prop = this.MainForm.BiblioDbProperties[i];

                     if (strText == prop.OrderDbName)
                     {
                         if (this.comboBox_source_type.Text == "ͼ��"
                             && String.IsNullOrEmpty(prop.IssueDbName) == false)
                         {
                             this.comboBox_inputOrderDbName.Text = "";
                             break;
                         }
                         else if (this.comboBox_source_type.Text == "����������"
                             && String.IsNullOrEmpty(prop.IssueDbName) == true)
                         {
                             this.comboBox_inputOrderDbName.Text = "";
                             break;
                         }
                     }
                 }
             }
        }

        private void button_next_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strWarning = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_source)
            {
                if (InputStyle == PrintClaimInputStyle.BiblioRecPathFile)
                {
                    if (this.textBox_inputBiblioRecPathFilename.Text == "")
                    {
                        strError = "��δָ���������Ŀ���¼·���ļ���";
                        goto ERROR1;
                    }
                }
                else if (this.InputStyle == PrintClaimInputStyle.BiblioDatabase)
                {
                    if (this.comboBox_inputBiblioDbName.Text == "")
                    {
                        strError = "��δָ����Ŀ����";
                        goto ERROR1;
                    }
                }
                else if (InputStyle == PrintClaimInputStyle.OrderRecPathFile)
                {
                    if (this.textBox_inputOrderRecPathFilename.Text == "")
                    {
                        strError = "��δָ������Ķ������¼·���ļ���";
                        goto ERROR1;
                    }
                }
                else if (this.InputStyle == PrintClaimInputStyle.OrderDatabase)
                {
                    if (this.comboBox_inputOrderDbName.Text == "")
                    {
                        strError = "��δָ����������";
                        goto ERROR1;
                    }
                }

                // �л������ڷ�Χpage
                this.tabControl_main.SelectedTab = this.tabPage_timeRange;
                return;
            }

            if (this.tabControl_main.SelectedTab == this.tabPage_timeRange)
            {
                if (this.textBox_timeRange.Text == "")
                {
                    strError = "��δָ����ѯ���ڷ�Χ";
                    goto ERROR1;
                }
                this.tabControl_main.SelectedTab = this.tabPage_run;
            }

            if (this.tabControl_main.SelectedTab == this.tabPage_run)
            {
                this.Running = true;
                try
                {
                    // ��ÿ����Ŀ��¼����ѭ��
                    // return:
                    //      0   ��ͨ����
                    //      1   Ҫȫ���ж�
                    int nRet = DoLoop(out strError,
                        out strWarning);
                    if (nRet == -1)
                        goto ERROR1;
                }
                finally
                {
                    this.Running = false;
                }

                MessageBox.Show(this, "ͳ����ɡ�");
                this.tabControl_main.SelectedTab = this.tabPage_print;
                this.button_next.Enabled = false;
                return;
            }

            if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
                this.button_next.Enabled = false;
            }

            if (String.IsNullOrEmpty(strWarning) == false)
                MessageBox.Show(this, "����: \r\n" + strWarning);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void tabControl_main_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.Running == true)
                return;

            if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
                this.button_next.Enabled = false;
                return;
            }

            this.button_next.Enabled = true;
        }

        // ��ӡ��ѯ��
        private void button_print_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.seller_table.Count == 0)
            {
                strError = "Ŀǰû�пɴ�ӡ������";
                goto ERROR1;
            }

            List<string> filenames = new List<string>();

            try
            {
                foreach (string strKey in this.seller_table.Keys)
                {
                    OneSeller seller = (OneSeller)this.seller_table[strKey];
                    string strFilename = "";
                    int nRet = PrintOneSeller(seller,
                        out strFilename,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    filenames.Add(strFilename);
                }

                HtmlPrintForm printform = new HtmlPrintForm();

                printform.Text = "��ӡ��ѯ��";
                printform.MainForm = this.MainForm;
                printform.Filenames = filenames;
                this.MainForm.AppInfo.LinkFormState(printform, "printclaim_htmlprint_formstate");
                printform.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(printform);

            }
            finally
            {
                if (filenames != null)
                {
                    Global.DeleteFiles(filenames);
                    filenames.Clear();
                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��ô����������������'|'����Ĳ���
        static string GetPureSellerName(string strText)
        {
            int nRet = strText.IndexOf("|");
            if (nRet == -1)
                return strText;

            return strText.Substring(0, nRet);
        }

        // ���촿�ı����ʼĵ�ַ�ַ���
        // parameters:
        static int BuildAddressText(
            string strAddressXml,
            out string strText,
            out string strError)
        {
            strError = "";
            strText = "";

            if (String.IsNullOrEmpty(strAddressXml) == true)
                return 0;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root/>");
            try
            {
                dom.DocumentElement.InnerXml = strAddressXml;
            }
            catch (Exception ex)
            {
                strError = "��ַXML��ʽ����ȷ: " + ex.Message;
                return -1;
            }

            string strZipcode = DomUtil.GetElementText(dom.DocumentElement,
                "zipcode");
            string strAddress = DomUtil.GetElementText(dom.DocumentElement,
                "address");
            string strDepartment = DomUtil.GetElementText(dom.DocumentElement,
                "department");
            string strName = DomUtil.GetElementText(dom.DocumentElement,
                "name");

            strText = "(" + strZipcode + ")" + strAddress + " " + strDepartment + " " + strName;

            return 1;
        }

        // 
        // 
        /// <summary>
        /// ������Դ��������
        /// ���Ϊ"batchno"��ʽ����Ϊ���κţ�
        /// ���Ϊ"barcodefile"��ʽ����Ϊ������ļ���(���ļ���); 
        /// ���Ϊ"recpathfile"��ʽ����Ϊ��¼·���ļ���(���ļ���)
        /// </summary>
        public string SourceDescription
        {
            get
            {
                if (this.SourceStyle == "bibliodatabase")
                {
                    string strText = "";

                    if (String.IsNullOrEmpty(this.comboBox_inputBiblioDbName.Text) == false)
                        strText += "��Ŀ�� " + this.comboBox_inputBiblioDbName.Text;

                    return strText;
                }
                else if (this.SourceStyle == "bibliorecpathfile")
                {
                    return "��Ŀ���¼·���ļ� " + Path.GetFileName(this.textBox_inputBiblioRecPathFilename.Text);
                }
                else if (this.SourceStyle == "orderdatabase")
                {
                    string strText = "";

                    if (String.IsNullOrEmpty(this.comboBox_inputOrderDbName.Text) == false)
                        strText += "������ " + this.comboBox_inputOrderDbName.Text;

                    return strText;
                }
                else if (this.SourceStyle == "orderrecpathfile")
                {
                    return "�������¼·���ļ� " + Path.GetFileName(this.textBox_inputOrderRecPathFilename.Text);
                }
                else
                {
                    Debug.Assert(this.SourceStyle == "", "");
                    return "";
                }
            }
        }

        // ��ӡ����һ�����̵�ȫ���ڿ���Ϣ
        int PrintOneSeller(OneSeller seller,
            out string strFilename,
            out string strError)
        {
            strError = "";
            strFilename = "";

            int nRet = 0;

            string strAddressText = "";
            if (String.IsNullOrEmpty(seller.AddressXml) == false)
            {
                nRet = BuildAddressText(
                    seller.AddressXml,
                    out strAddressText,
                    out strError);
                if (nRet == -1)
                    return -1;
            }


            Hashtable macro_table = new Hashtable();

            // ���κŻ��ļ��� ����겻�����жϺ�ѡ�ã�ֻ�úϲ�Ϊһ����
            macro_table["%sourcedescription%"] = this.SourceDescription;


            // ��ô�ӡ����
            PrintClaimPrintOption option = new PrintClaimPrintOption(this.MainForm.DataDir,
                this.comboBox_source_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                "printclaim_printoption");

            macro_table["%seller%"] = GetPureSellerName(seller.Seller); // ������
            macro_table["%selleraddress%"] = strAddressText;    // 2009/9/17 new add
            macro_table["%libraryname%"] = this.MainForm.LibraryName;
            /*
            macro_table["%pagecount%"] = nPageCount.ToString();
            macro_table["%linesperpage%"] = option.LinesPerPage.ToString();
             * */
            macro_table["%date%"] = DateTime.Now.ToLongDateString();


            // ��Ҫ�����ڲ�ͬ���̵��ļ���ǰ׺������
            string strFileNamePrefix = this.MainForm.DataDir + "\\~printclaim_" + seller.GetHashCode().ToString() + "_";

            strFilename = strFileNamePrefix + "0" + ".html";

            BuildPageTop(option,
                macro_table,
                strFilename,
                seller);

            // ����ź�����
            {

                // �ڿ�����
                macro_table["%seriescount%"] = seller.Count.ToString();
                // ��ص�����
                macro_table["%issuecount%"] = GetIssueCount(seller).ToString();
                // ȱ�Ĳ���
                macro_table["%missingitemcount%"] = GetMissingItemCount(seller).ToString();

                macro_table["%datadir%"] = this.MainForm.DataDir;   // ��������datadir��templatesĿ¼�ڵ�ĳЩ�ļ�
                //// macro_table["%libraryserverdir%"] = this.MainForm.LibraryServerDir;  // �������÷������˵�CSS�ļ�

                string strTemplateFilePath = option.GetTemplatePageFilePath("�ż�����");
                if (String.IsNullOrEmpty(strTemplateFilePath) == false)
                {
                    /*
	<div class='pageheader'>%date% �Ʋ��ʲ� -- %sourcedescription% -- (�� %pagecount% ҳ)</div>
	<div class='tabletitle'>%date% �Ʋ��ʲ� -- %barcodefilepath%</div>
	<div class='itemcount'>����: %itemcount%</div>
	<div class='bibliocount'>����: %bibliocount%</div>
	<div class='totalprice'>�ܼ�: %totalprice%</div>
	<div class='sepline'><hr/></div>
	<div class='batchno'>���κ�: %batchno%</div>
	<div class='location'>�ݲصص�: %location%</div>
	<div class='location'>������ļ�: %barcodefilepath%</div>
	<div class='location'>��¼·���ļ�: %recpathfilepath%</div>
	<div class='sepline'><hr/></div>
	<div class='pagefooter'>%pageno%/%pagecount%</div>
                     * * */

                    // ����ģ���ӡ
                    string strContent = "";
                    // ���Զ�ʶ���ļ����ݵı��뷽ʽ�Ķ����ı��ļ�����ģ��
                    // return:
                    //      -1  ����
                    //      0   �ļ�������
                    //      1   �ļ�����
                    nRet = Global.ReadTextFileContent(strTemplateFilePath,
                        out strContent,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    string strResult = Global.MacroString(macro_table,
                        strContent);
                    StreamUtil.WriteText(strFilename,
                        strResult);
                }
                else
                {
                    // ȱʡ�Ĺ̶����ݴ�ӡ
                    StreamUtil.WriteText(strFilename,
                        "<div class='letter'>");

                    string strAddressLine = "";
                    if (String.IsNullOrEmpty((string)macro_table["%selleraddress%"]) == false)
                        strAddressLine = "�£�%selleraddress%<br/><br/>";

                    string strText = strAddressLine + "�𾴵� %seller%:<br/>�ҹ��ڹ󴦶���������"+this.TypeName+" %seriescount% �֣��� "
                        + (this.TypeName == "�ڿ�" ? "%issuecount% �� " : "")
                        +"�� %missingitemcount% ������δ���������첹��Ϊ�Ρ�лл��<br/><br/>%libraryname%<br/>%date%";
                    strText = Global.MacroString(macro_table,
                        strText);

                    StreamUtil.WriteText(strFilename,
                        strText);

                    StreamUtil.WriteText(strFilename,
                        "</div>");
                }

            }

            for (int i = 0; i < seller.Count; i++)
            {
                OneSeries series = seller[i];

                PrintOneSeries(option,
                    macro_table,
                    series,
                    strFilename);
            }


            BuildPageBottom(option,
                macro_table,
                strFilename);

            return 0;
        }

        string TypeName
        {
            get
            {
                if (this.comboBox_source_type.Text == "����������"
                    || this.comboBox_source_type.Text == "�ڿ�")
                    return "�ڿ�";
                return "ͼ��";
            }
        }

        // ���������
        static int GetIssueCount(OneSeller seller)
        {
            int nCount = 0;
            for (int i = 0; i < seller.Count; i++)
            {
                OneSeries series = seller[i];

                nCount += series.IssueInfos.Count;
            }

            return nCount;
        }

        // ȱ�Ĳ���
        static int GetMissingItemCount(OneSeller seller)
        {
            int nCount = 0;
            for (int i = 0; i < seller.Count; i++)
            {
                OneSeries series = seller[i];

                for (int j = 0; j < series.IssueInfos.Count; j++)
                {
                    IssueInfo info = series.IssueInfos[j];

                    int nValue = 0;

                    try
                    {
                        nValue = Convert.ToInt32(info.MissingCount);
                    }
                    catch
                    {
                    }

                    nCount += nValue;
                }
            }

            return nCount;
        }

        // ���һ���ڿ�����Ϣ
        void PrintOneSeries(PrintOption option,
            Hashtable macro_table, 
            OneSeries series,
            string strFilename)
        {

            string strClass = "";
            string strCaption = "";


            // ���ʼ
            StreamUtil.WriteText(strFilename,
                "<br/><table class='table'>");   //   border='1'


            // ��һ����Ŀ����
            StreamUtil.WriteText(strFilename,
                "<tr class='column'>");

            strClass = "series_info";
            strCaption = this.TypeName + "��Ϣ";
            StreamUtil.WriteText(strFilename,
"<td class='" + strClass + "' colspan='5'>" + strCaption + "</td>");


            StreamUtil.WriteText(strFilename,
                "</tr>");

            // �ڿ���Ϣ��
            StreamUtil.WriteText(strFilename,
                "<tr class='series_info'>");

            strClass = "series_info";
            strCaption = series.BiblioSummary;
            StreamUtil.WriteText(strFilename,
"<td class='" + strClass + "' colspan='" + option.Columns.Count.ToString() + "'>" + strCaption + "</td>");


            StreamUtil.WriteText(strFilename,
                "</tr>");

            // �ڶ�����Ŀ����

            StreamUtil.WriteText(strFilename,
    "</tr>");

            // �ڿ���Ϣ��

            // ͨ����ȱ����Ϣ
            StreamUtil.WriteText(strFilename,
                "<tr class='column'>");
            strClass = "missing_info";
            if (this.TypeName == "�ڿ�")
                strCaption = "ȱ����Ϣ";
            else
                strCaption = "ȱ����Ϣ";

            StreamUtil.WriteText(strFilename,
                "<td class='" + strClass + "' colspan='" + option.Columns.Count.ToString() + "'>" + strCaption + "</td>");
            StreamUtil.WriteText(strFilename,
                "</tr>");

            /*
            StreamUtil.WriteText(strFilename,
                "<tr class='column'>");

            strClass = "publishTime";
            strCaption = "��������";
            StreamUtil.WriteText(strFilename,
"<td class='" + strClass + "'>" + strCaption + "</td>");

            strClass = "issue";
            strCaption = "�ں�";
            StreamUtil.WriteText(strFilename,
"<td class='" + strClass + "'>" + strCaption + "</td>");

            strClass = "orderCount";
            strCaption = "��������";
            StreamUtil.WriteText(strFilename,
"<td class='" + strClass + "'>" + strCaption + "</td>");

            strClass = "arrivedCount";
            strCaption = "ʵ������";
            StreamUtil.WriteText(strFilename,
"<td class='" + strClass + "'>" + strCaption + "</td>");

            strClass = "missingCount";
            strCaption = "ȱ����";
            StreamUtil.WriteText(strFilename,
"<td class='" + strClass + "'>" + strCaption + "</td>");

            StreamUtil.WriteText(strFilename,
"</tr>");
             * */
            // ��Ŀ����
            StreamUtil.WriteText(strFilename,
                "<tr class='column'>");

            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];

                strCaption = column.Caption;

                // ���û��caption���壬��Ų��name����
                if (String.IsNullOrEmpty(strCaption) == true)
                    strCaption = column.Name;

                strClass = StringUtil.GetLeft(column.Name);

                StreamUtil.WriteText(strFilename,
                    "<td class='" + strClass + "'>" + strCaption + "</td>");
            }

            StreamUtil.WriteText(strFilename,
                "</tr>");


            // ������
            for (int i = 0; i < series.IssueInfos.Count; i++)
            {
                IssueInfo info = series.IssueInfos[i];

                // ������Ѿ��������
                if (info.MissingCount == "0")
                    continue;

                StreamUtil.WriteText(strFilename,
"<tr class='content'>");

                /*
                strClass = "publishTime";
                strCaption = info.PublishTime;
                StreamUtil.WriteText(strFilename,
    "<td class='" + strClass + "'>" + strCaption + "</td>");

                strClass = "issue";
                strCaption = info.Issue;
                StreamUtil.WriteText(strFilename,
    "<td class='" + strClass + "'>" + strCaption + "</td>");

                strClass = "orderCount";
                strCaption = info.OrderCount;
                StreamUtil.WriteText(strFilename,
    "<td class='" + strClass + "'>" + strCaption + "</td>");

                strClass = "arrivedCount";
                strCaption = info.ArrivedCount;
                StreamUtil.WriteText(strFilename,
    "<td class='" + strClass + "'>" + strCaption + "</td>");

                strClass = "missingCount";
                strCaption = info.MissingCount;
                StreamUtil.WriteText(strFilename,
    "<td class='" + strClass + "'>" + strCaption + "</td>");
                 * */
                for (int j = 0; j < option.Columns.Count; j++)
                {
                    Column column = option.Columns[j];

                    string strContent = GetColumnContent(info,
    column.Name);

                    strClass = StringUtil.GetLeft(column.Name);
                    StreamUtil.WriteText(strFilename,
                        "<td class='" + strClass + "'>" + strContent + "</td>");

                }

                StreamUtil.WriteText(strFilename,
    "</tr>");
            }

            // ������
            StreamUtil.WriteText(strFilename,
                "</table>");
        }

        // �����Ŀ����
        string GetColumnContent(IssueInfo info,
            string strColumnName)
        {
            // ȥ��"-- ?????"����
            /*
            string strText = strColumnName;
            int nRet = strText.IndexOf("--", 0);
            if (nRet != -1)
                strText = strText.Substring(0, nRet).Trim();
             * */

            string strText = StringUtil.GetLeft(strColumnName);

            try
            {
                // TODO: ��Ҫ�޸�
                // Ҫ��Ӣ�Ķ�����
                switch (strText)
                {
                    case "publishTime":
                    case "��������":
                        {
                            string strPublishTime = "";
                            
                            if (string.IsNullOrEmpty(info.PublishTime) == false)
                                strPublishTime = DateTimeUtil.ForcePublishTime8(info.PublishTime);

                            if (string.IsNullOrEmpty(strPublishTime) == true
                                && string.IsNullOrEmpty(info.OrderTime) == false)
                            {
                                DateTime order_time = DateTimeUtil.FromRfc1123DateTimeString(info.OrderTime).ToLocalTime();

                                string strDelay = this.comboBox_timeRange_afterOrder.Text;
                                if (strDelay == "����")
                                    strDelay = "";
                                if (string.IsNullOrEmpty(strDelay) == false)
                                    strDelay = " + " + strDelay;

                                return "? " + order_time.ToString("d") + strDelay;
                            }

                            return DateTimeUtil.Long8ToDateTime(strPublishTime).ToString("d");
                        }

                    case "issue":
                    case "�ں�":
                        return info.Issue;

                    case "orderCount":
                    case "��������":
                        return info.OrderCount;

                    case "arrivedCount":
                    case "ʵ������":
                        return info.ArrivedCount;

                    case "missingCount":
                    case "ȱ����":
                        return info.MissingCount;

                    default:
                        return "undefined column";
                }
            }
            catch
            {
                return null; 
            }
        }

        // 2009/10/10 new add
        // ���css�ļ���·��(����http:// ��ַ)���������Ƿ���С�ͳ��ҳ�����Զ�����
        // parameters:
        //      strDefaultCssFileName   ��css��ģ��ȱʡ����£������õ�����Ŀ¼�е�css�ļ��������ļ���
        string GetAutoCssUrl(PrintOption option,
            string strDefaultCssFileName)
        {
            string strCssFilePath = option.GetTemplatePageFilePath("css");  // ��Сд������
            if (String.IsNullOrEmpty(strCssFilePath) == false)
                return strCssFilePath;
            else
            {
                // return this.MainForm.LibraryServerDir + "/" + strDefaultCssFileName;    // ȱʡ��
                return PathUtil.MergePath(this.MainForm.DataDir, strDefaultCssFileName);    // ȱʡ��
            }
        }

        int BuildPageTop(PrintOption option,
            Hashtable macro_table,
            string strFileName,
            OneSeller seller)
        {
            // string strCssUrl = this.MainForm.LibraryServerDir + "/printclaim.css";
            // 2009/10/10 changed
            string strCssUrl = GetAutoCssUrl(option, "printclaim.css");

            /*
            // 2009/10/9 new add
            string strCssFilePath = option.GetTemplatePageFilePath("css");  // ��Сд������
            if (String.IsNullOrEmpty(strCssFilePath) == false)
                strCssUrl = strCssFilePath;
            else
                strCssUrl = this.MainForm.LibraryServerDir + "/printclaim.css";    // ȱʡ��
             * */

            string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";

            StreamUtil.WriteText(strFileName,
                "<html><head>" + strLink + "</head><body>");

            /*
            // ҳü
            string strPageHeaderText = option.PageHeader;

            if (String.IsNullOrEmpty(strPageHeaderText) == false)
            {
                strPageHeaderText = Global.MacroString(macro_table,
                    strPageHeaderText);

                StreamUtil.WriteText(strFileName,
                    "<div class='pageheader'>" + strPageHeaderText + "</div>");
            }
             * */

            // ��������
            StreamUtil.WriteText(strFileName,
    "<div class='seller'>" + GetPureSellerName(seller.Seller) + "</div>");

            /*
            // ������
            string strTableTitleText = option.TableTitle;

            if (String.IsNullOrEmpty(strTableTitleText) == false)
            {

                strTableTitleText = Global.MacroString(macro_table,
                    strTableTitleText);

                StreamUtil.WriteText(strFileName,
                    "<div class='tabletitle'>" + strTableTitleText + "</div>");
            }
             * */



            return 0;
        }


        int BuildPageBottom(PrintOption option,
            Hashtable macro_table,
            string strFileName)
        {

            /*
            // ҳ��
            string strPageFooterText = option.PageFooter;

            if (String.IsNullOrEmpty(strPageFooterText) == false)
            {
                strPageFooterText = Global.MacroString(macro_table,
                    strPageFooterText);

                StreamUtil.WriteText(strFileName,
        "<div class='pagefooter'>" + strPageFooterText + "</div>");
            }*/

            StreamUtil.WriteText(strFileName, "</body></html>");

            return 0;
        }

        private void button_timeRange_clearTimeRange_Click(object sender, EventArgs e)
        {
            this.textBox_timeRange.Text = "";

        }

        private void button_timeRange_inputTimeRange_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;
            DateTime start;
            DateTime end;

            nRet = Global.ParseTimeRangeString(this.textBox_timeRange.Text,
                false,
                out start,
                out end,
                out strError);
            /*
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }*/


            TimeRangeDlg dlg = new TimeRangeDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.Text = "��ѯ���ڷ�Χ";
            dlg.StartDate = start;
            dlg.EndDate = end;
            dlg.AllowStartDateNull = true;  // �������ʱ��Ϊ��

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult == DialogResult.Cancel)
                return;

            this.textBox_timeRange.Text = Global.MakeTimeRangeString(dlg.StartDate, dlg.EndDate);

            this.comboBox_timeRange_quickSet.Text = ""; // �������
        }

        // ��������
        private void comboBox_timeRange_quickSet_TextChanged(object sender, EventArgs e)
        {
            if (this.comboBox_timeRange_quickSet.Text == "����ǰ")
            {
                this.textBox_timeRange.Text = Global.MakeTimeRangeString(new DateTime(0), DateTime.Now);
            }
            else if (this.comboBox_timeRange_quickSet.Text == "һ��ǰ")
            {
                DateTime now = DateTime.Now;
                DateTime time = new DateTime(now.Year, now.Month, 1);
                time = time - new TimeSpan(24, 0, 0);

                this.textBox_timeRange.Text = Global.MakeTimeRangeString(new DateTime(0), time);
            }
            else if (this.comboBox_timeRange_quickSet.Text == "����ǰ")
            {
                DateTime now = DateTime.Now;
                DateTime time = new DateTime(now.Year, now.Month, 1);

                if (time.Month >= 7)    // 2011/7/11 bug
                {
                    time = new DateTime(time.Year, time.Month - 6, 1);
                }
                else
                {
                    time = new DateTime(time.Year - 1, time.Month + 12 - 6, 1);
                }

                time = time - new TimeSpan(24, 0, 0);

                this.textBox_timeRange.Text = Global.MakeTimeRangeString(new DateTime(0), time);
            }
            else if (this.comboBox_timeRange_quickSet.Text == "һ��ǰ")
            {
                DateTime now = DateTime.Now;
                DateTime time = new DateTime(now.Year - 1, now.Month, 1);
                time = time - new TimeSpan(24, 0, 0);

                this.textBox_timeRange.Text = Global.MakeTimeRangeString(new DateTime(0), time);
            }
            else
            {
                // Console.Beep(); // ��ʾ�޷�����
            }
        }

        private void PrintClaimForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);
        }

        private void button_printOption_Click(object sender, EventArgs e)
        {
            // ���ñ���ͷ��
            string strNamePath = "printclaim_printoption";

            PrintClaimPrintOption option = new PrintClaimPrintOption(this.MainForm.DataDir,
                this.comboBox_source_type.Text);
            option.LoadData(this.MainForm.AppInfo,
                strNamePath);


            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.DataDir = this.MainForm.DataDir;
            dlg.Text = this.comboBox_source_type.Text + " ��ѯ�� ��ӡ����";
            dlg.PrintOption = option;
            dlg.ColumnItems = new string[] {
                "publishTime -- ��������",
                "issue -- �ں�",
                "orderCount -- ��������",
                "arrivedCount -- ʵ������",
                "missingCount -- ȱ����",
            };


            this.MainForm.AppInfo.LinkFormState(dlg, "printclaim_printoption_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            option.SaveData(this.MainForm.AppInfo,
                strNamePath);
        }

        private void button_findInputBiblioRecPathFilename_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵�[��Ŀ��]��¼·���ļ���";
            if (this.textBox_inputBiblioRecPathFilename.Text.IndexOf(",") == -1)
                dlg.FileName = this.textBox_inputBiblioRecPathFilename.Text;
            // dlg.InitialDirectory = 
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_inputBiblioRecPathFilename.Text = dlg.FileName;
        }

        private void button_findInputOrderRecPathFilename_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵�[������]��¼·���ļ���";
            if (this.textBox_inputOrderRecPathFilename.Text.IndexOf(",") == -1)
                dlg.FileName = this.textBox_inputOrderRecPathFilename.Text;
            // dlg.InitialDirectory = 
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_inputOrderRecPathFilename.Text = dlg.FileName;
        }

        private void comboBox_inputOrderDbName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_inputOrderDbName.Items.Count > 0)
                return;

            if (this.comboBox_source_type.Text == "ͼ��")
                this.comboBox_inputOrderDbName.Items.Add("<ȫ��ͼ��>");
            else
                this.comboBox_inputOrderDbName.Items.Add("<ȫ���ڿ�>");

            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty prop = this.MainForm.BiblioDbProperties[i];

                    if (String.IsNullOrEmpty(prop.OrderDbName) == true)
                        continue; 

                    if (this.comboBox_source_type.Text == "ͼ��")
                    {
                        if (String.IsNullOrEmpty(prop.IssueDbName) == false)
                            continue;
                    }
                    else
                    {
                        // �ڿ���Ҫ���ڿ�����Ϊ��

                        if (String.IsNullOrEmpty(prop.IssueDbName) == true)
                            continue;
                    }

                    this.comboBox_inputOrderDbName.Items.Add(prop.OrderDbName);
                }
            }
        }

        private void radioButton_inputStyle_orderRecPathFile_CheckedChanged(object sender, EventArgs e)
        {
            SetInputPanelEnabled(true);
        }

        private void radioButton_inputStyle_orderDatabase_CheckedChanged(object sender, EventArgs e)
        {
            SetInputPanelEnabled(true);
        }

        void SetTimeRangeState(bool bEnabled)
        {
            if (this.checkBox_timeRange_useOrderTime.Checked == true)
            {
                this.comboBox_timeRange_afterOrder.Enabled = bEnabled;
            }
            else
            {
                this.comboBox_timeRange_afterOrder.Enabled = false;
            }

            if (this.checkBox_timeRange_none.Checked == true)
            {
                this.checkBox_timeRange_useOrderTime.Enabled = false;
                this.checkBox_timeRange_usePublishTime.Enabled = false;
                this.comboBox_timeRange_afterOrder.Enabled = false;

                SetTimeRangeValueVisible(false);
            }
            else
            {
                this.checkBox_timeRange_useOrderTime.Enabled = bEnabled;
                this.checkBox_timeRange_usePublishTime.Enabled = bEnabled;
                // checkBox_timeRange_useOrderTime_CheckedChanged(null, null);

                SetTimeRangeValueVisible(bEnabled);
            }
        }

        private void checkBox_timeRange_useOrderTime_CheckedChanged(object sender, EventArgs e)
        {
            SetTimeRangeState(true);
        }

        // ����ʱ�䷶Χֵ��صĽ���Ԫ�ص�Enabled״̬���������������á�����
        void SetTimeRangeValueVisible(bool bVisible)
        {
            this.label_timerange.Visible = bVisible;
            this.textBox_timeRange.Visible = bVisible;
            this.button_timeRange_clearTimeRange.Visible = bVisible;
            this.button_timeRange_inputTimeRange.Visible = bVisible;
            this.groupBox_timeRange_quickSet.Visible = bVisible;
        }

        private void checkBox_timeRange_none_CheckedChanged(object sender, EventArgs e)
        {
            SetTimeRangeState(true);
        }

        private void comboBox_source_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.comboBox_source_type.Text == "ͼ��")
                this.checkBox_source_guess.Enabled = false;
            else
                this.checkBox_source_guess.Enabled = true;
        }
    }

    /// <summary>
    /// ��ѯ��������
    /// </summary>
    public enum PrintClaimInputStyle
    {
        /// <summary>
        /// ��Ŀ���¼·���ļ�
        /// </summary>
        BiblioRecPathFile = 1,    // ��Ŀ���¼·���ļ�
        /// <summary>
        /// ������Ŀ��
        /// </summary>
        BiblioDatabase = 2,     // ������Ŀ��
        /// <summary>
        /// �������¼·���ļ�
        /// </summary>
        OrderRecPathFile = 3,   // �������¼·���ļ�
        /// <summary>
        /// ����������
        /// </summary>
        OrderDatabase = 4,      // ����������
    }

    // 
    /// <summary>
    /// һ������������������ڿ���ȱ����Ϣ����ͬ���̵�ַ
    /// </summary>
    public class OneSeller : List<OneSeries>
    {
        /// <summary>
        /// ������������
        /// </summary>
        public string Seller = "";  // ������

        /// <summary>
        /// ��ַ XML
        /// </summary>
        public string AddressXml = "";  // ��ϵ��ַ��Ϣ

        // 
        /// <summary>
        /// ������Ŀ��¼·���ҵ�һ���Ѿ����ڵ�OneSeries����
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <returns>OneSeries ����</returns>
        public OneSeries FindOneSeries(string strBiblioRecPath)
        {
            foreach (OneSeries series in this)
            {
                if (series.BiblioRecPath == strBiblioRecPath)
                    return series;
            }

            return null;
        }
    }

    // 
    /// <summary>
    /// һ���ڿ���ȱ����Ϣ
    /// </summary>
    public class OneSeries
    {
        /// <summary>
        /// ��Ŀ��¼·��
        /// </summary>
        public string BiblioRecPath = "";   // ��Ŀ��¼·��

        /// <summary>
        /// ��ĿժҪ
        /// </summary>
        public string BiblioSummary = "";   // ��ĿժҪ

        /// <summary>
        /// ISSN
        /// </summary>
        public string ISSN = "";

        /// <summary>
        /// ����
        /// </summary>
        public string Title = "";

        /// <summary>
        /// ����Ϣ����
        /// </summary>
        public List<IssueInfo> IssueInfos = new List<IssueInfo>();   // ȱ����Ϣ

        /// <summary>
        /// ��������������ӵ�ֵ
        /// </summary>
        /// <param name="s1">�����ַ���1</param>
        /// <param name="s2">�����ַ���2</param>
        /// <returns>����ַ���</returns>
        public static string Add(string s1, string s2)
        {
            long v1 = 0;
            Int64.TryParse(s1, out v1);
            long v2 = 0;
            Int64.TryParse(s2, out v2);

            return (v1 + v2).ToString();
        }

        /// <summary>
        /// �� other_series �ϲ����뵱ǰ���󡣺ϲ��Ǹ��ݳ������ں��ںŽ��е�
        /// </summary>
        /// <param name="other_series">��һ���ڿ�����Ϣ</param>
        public void MergeIssueInfos(OneSeries other_series)
        {
            int nAppendCount = 0;
            foreach (IssueInfo other_info in other_series.IssueInfos)
            {
                string strOtherYearPart = IssueUtil.GetYearPart(other_info.PublishTime);
                bool bFound = false;
                foreach (IssueInfo info in this.IssueInfos)
                {
                    if (strOtherYearPart == IssueUtil.GetYearPart(info.PublishTime)
                        && other_info.Issue == info.Issue)
                    {
                        info.OrderCount = Add(info.OrderCount, other_info.OrderCount);
                        info.MissingCount = Add(info.MissingCount, other_info.MissingCount);
                        info.ArrivedCount = Add(info.ArrivedCount, other_info.ArrivedCount);
                        bFound = true;
                        break;
                    }
                }

                if (bFound == false)
                {
                    this.IssueInfos.Add(other_info);
                    nAppendCount++;
                }
            }

            // ��Ҫ��������
            if (nAppendCount > 0)
            {
                this.IssueInfos.Sort(new IssueInfoSorter());
            }
        }
    }

#if NO
    // һ�����������������ͼ���ȱ��Ϣ����ͬ���̵�ַ
    public class OneSellerMono : List<OneMono>
    {
        public string Seller = "";  // ������
        public string AddressXml = "";  // ��ϵ��ַ��Ϣ
    }

    // һ��ͼ���ȱ��Ϣ
    public class OneMono
    {
        public string BiblioRecPath = "";   // ��Ŀ��¼·��
        public string BiblioSummary = "";   // ��ĿժҪ
        public string ISSN = "";
        public string Title = "";

        public List<OrderInfo> OrderInfos = new List<OrderInfo>();   // ȱ����Ϣ
    }
#endif

    // ��ѯ����ӡ �������ض�ȱʡֵ��PrintOption������
    internal class PrintClaimPrintOption : PrintOption
    {
        string PublicationType = "ͼ��"; // ͼ�� ����������

        public override void LoadData(ApplicationInfo ai,
            string strPath)
        {
            string strNamePath = strPath;
            if (this.PublicationType != "ͼ��")
                strNamePath = "series_" + strNamePath;
            base.LoadData(ai, strNamePath);
        }

        public override void SaveData(ApplicationInfo ai,
            string strPath)
        {
            string strNamePath = strPath;
            if (this.PublicationType != "ͼ��")
                strNamePath = "series_" + strNamePath;
            base.SaveData(ai, strNamePath);
        }

        public PrintClaimPrintOption(string strDataDir,
            string strPublicationType)
        {
            this.DataDir = strDataDir;
            this.PublicationType = strPublicationType;

            this.PageHeaderDefault = "%date% %seller% ��ѯ�� - ���κŻ��ļ���: %batchno_or_recpathfilename% - (�� %pagecount% ҳ)"; // TODO: �޸� batchno_or_recpathfilename
            this.PageFooterDefault = "%pageno%/%pagecount%";

            this.TableTitleDefault = "%date% %seller% ��ѯ��";

            this.LinesPerPageDefault = 20;

            // Columnsȱʡֵ
            Columns.Clear();

            // "publishTime -- ��������",
            Column column = new Column();
            column.Name = "publishTime -- ��������";
            column.Caption = "��������";
            column.MaxChars = -1;
            this.Columns.Add(column);

            if (strPublicationType == "����������"
                || strPublicationType == "�ڿ�")
            {
                // "issue -- �ں�"
                column = new Column();
                column.Name = "issue -- �ں�";
                column.Caption = "�ں�";
                column.MaxChars = -1;
                this.Columns.Add(column);
            }


            // "orderCount -- ��������"
            column = new Column();
            column.Name = "orderCount -- ��������";
            column.Caption = "��������";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "arrivedCount -- ʵ������"
            column = new Column();
            column.Name = "arrivedCount -- ʵ������";
            column.Caption = "ʵ������";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "missingCount -- ȱ����"
            column = new Column();
            column.Name = "missingCount -- ȱ����";
            column.Caption = "ȱ����";
            column.MaxChars = -1;
            this.Columns.Add(column);

        }
    }

    // 
    /// <summary>
    /// ʱ�������������ʱ�����Ҫ��
    /// </summary>
    public class TimeFilter
    {
        /// <summary>
        /// ���"both" ���߶���(���ó���ʱ�䣬���û�����ö���ʱ��) / "publishtime" ֻ�ó���ʱ��(û�г���ʱ���򲻴���) / "ordertime" ֻ�ö���ʱ��ƫ��(û�ж���ʱ���򲻴���) / "none" ��ȫ������ 
        /// </summary>
        public string Style = "both";   // "both" ���߶���(���ó���ʱ�䣬���û�����ö���ʱ��) / "publishtime" ֻ�ó���ʱ��(û�г���ʱ���򲻴���) / "ordertime" ֻ�ö���ʱ��ƫ��(û�ж���ʱ���򲻴���) / "none" ��ȫ������ 
        // �������ڷ�Χ
        // ȱʡЧ������Զ�Ĺ�ȥ-��������
        /// <summary>
        /// �������ڷ�Χ����ʼʱ�䡣ȱʡΪ��Զ�Ĺ�ȥ
        /// </summary>
        public DateTime StartTime = new DateTime(0);
        /// <summary>
        /// �������ڷ�Χ������ʱ�䡣ȱʡΪ����
        /// </summary>
        public DateTime EndTime = DateTime.Now;

        // �������� + ƫ���� ����ָ����Χ
        /// <summary>
        /// ����ʱ��ƫ����
        /// </summary>
        public TimeSpan OrderTimeDelta = new TimeSpan();

        // Ѱ��ʵ��1�����ϵ����һ�ڡ�����һ�����ɣ���Ϊ���ĳ����Ȼ������ȱ�ķ�Χ(��ָ����ΧԽ������)������ʵ���ϵ��ˣ�����������ʱ�仹Ҫ�����Ӧ��Ҳ���ˡ�������Ҫ����ʵ�ʵ�����������Ǿ���������趨��ʱ��
        /// <summary>
        /// �Ƿ�У��ʵ���ѵ�����
        /// </summary>
        public bool VerifyArrivedIssue = false;
    }
}