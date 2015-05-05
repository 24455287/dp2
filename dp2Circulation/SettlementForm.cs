using System;
using System.Collections;
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
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ���㴰
    /// </summary>
    public partial class SettlementForm : MyForm
    {
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

        // ͼ���±�
        const int ITEMTYPE_AMERCED = 0;
        const int ITEMTYPE_NEWLY_SETTLEMENTED = 1;
        const int ITEMTYPE_OLD_SETTLEMENTED = 2;
        const int ITEMTYPE_UNKNOWN = 3;

        // �е�λ��
        const int COLUMN_ID =               0;
        const int COLUMN_STATE =            1;
        const int COLUMN_READERBARCODE =    2;
        const int COLUMN_LIBRARYCODE =      3;
        const int COLUMN_PRICE =            4;
        const int COLUMN_COMMENT =          5;
        const int COLUMN_REASON =           6;
        const int COLUMN_BORROWDATE =       7;
        const int COLUMN_BORROWPERIOD =     8;
        const int COLUMN_RETURNDATE =       9;
        const int COLUMN_RETURNOPERATOR =   10;
        const int COLUMN_BARCODE =          11;
        const int COLUMN_SUMMARY =          12;
        const int COLUMN_AMERCEOPERATOR =   13;
        const int COLUMN_AMERCETIME =       14;
        const int COLUMN_SETTLEMENTOPERATOR = 15;
        const int COLUMN_SETTLEMENTTIME =   16;

        const int COLUMN_RECPATH =          17;

        // ����������к�����
        SortColumns SortColumns = new SortColumns();

        /// <summary>
        /// ���캯��
        /// </summary>
        public SettlementForm()
        {
            InitializeComponent();
        }

        private void SettlementForm_Load(object sender, EventArgs e)
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

            // ��ʼ����
            this.dateControl_start.Text = this.MainForm.AppInfo.GetString(
                 "settlementform",
                 "start_date",
                 "");

            // ��������
            this.dateControl_end.Text = this.MainForm.AppInfo.GetString(
                "settlementform",
                "end_date",
                "");

            // ��ʼ������
            this.textBox_range_startCtlno.Text = this.MainForm.AppInfo.GetString(
                "settlementform",
                "start_ctlno",
                "");

            // ����������
            this.textBox_range_endCtlno.Text = this.MainForm.AppInfo.GetString(
                "settlementform",
                "end_ctlno",
                "");

            // �շѲ���ʱ�䷶Χ
            this.radioButton_range_amerceOperTime.Checked = this.MainForm.AppInfo.GetBoolean(
                "settlementform",
                "range_amerceopertime",
                true);

            // �����ŷ�Χ
            this.radioButton_range_ctlno.Checked = this.MainForm.AppInfo.GetBoolean(
                "settlementform",
                "range_ctlno",
                false);

            // ״̬
            this.comboBox_range_state.Text = this.MainForm.AppInfo.GetString(
                "settlementform",
                "range_state",
                "<ȫ��>");

            // �����շ���С��
            this.checkBox_sumByAmerceOperator.Checked = this.MainForm.AppInfo.GetBoolean(
                "settlementform",
                "sumby_amerceoperator",
                true);

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);
        }

        private void SettlementForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void SettlementForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            // ��ʼ����
            this.MainForm.AppInfo.SetString(
                "settlementform",
                "start_date",
                this.dateControl_start.Text);
            // ��������
            this.MainForm.AppInfo.SetString(
                "settlementform",
                "end_date",
                this.dateControl_end.Text);

            // ��ʼ������
            this.MainForm.AppInfo.SetString(
                "settlementform",
                "start_ctlno",
                this.textBox_range_startCtlno.Text);

            // ����������
            this.MainForm.AppInfo.SetString(
                "settlementform",
                "end_ctlno",
                this.textBox_range_endCtlno.Text);

            this.MainForm.AppInfo.SetBoolean(
                "settlementform",
                "range_amerceopertime",
                this.radioButton_range_amerceOperTime.Checked);

            this.MainForm.AppInfo.SetBoolean(
                "settlementform",
                "range_ctlno",
                this.radioButton_range_ctlno.Checked);

            // ״̬
            this.MainForm.AppInfo.SetString(
                "settlementform",
                "range_state",
                this.comboBox_range_state.Text);

            // �����շ���С��
            this.MainForm.AppInfo.SetBoolean(
                "settlementform",
                "sumby_amerceoperator",
                this.checkBox_sumByAmerceOperator.Checked);

            SaveSize();
        }

        /*public*/ void LoadSize()
        {
#if NO
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state");
#endif

            string strWidths = this.MainForm.AppInfo.GetString(
                "settlement_form",
                "amerced_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_amerced,
                    strWidths,
                    true);
            }
        }

        /*public*/ void SaveSize()
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

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_amerced);
            this.MainForm.AppInfo.SetString(
                "settlement_form",
                "amerced_list_column_width",
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

        // ���滯����ʱ�䡣
        static void CanonicalizeDate(ref DateTime time,
            string strStyle)
        {
            if (strStyle == "smallbound")   // ��С�߽�
            {
                time = new DateTime(time.Year, time.Month, time.Day,
                    0, 0, 0, 0);
            }
            else if (strStyle == "largebound") // ���߽�
            {
                time = new DateTime(time.Year, time.Month, time.Day,
                    23, 59, 59, 999);
            }
            else
            {
                throw new Exception("δ֪�� strStyleֵ '" + strStyle + "'");
            }
        }


        // ����XML����ʽ
        // parameters:
        //      start_time  ��ʼʱ�䡣����ʱ�䡣
        //      end_time    ����ʱ�䡣����ʱ�䡣
        // return:
        //      -1  ����
        //      0   ΥԼ�����û������
        //      1   �ɹ�
        int BuildQueryXml(DateTime start_time,
            DateTime end_time,
            string strState,
            out string strQueryXml,
            out string strError)
        {
            strQueryXml = "";
            strError = "";

            string strDbName = "ΥԼ��";
            string strFrom = "�ɿ�ʱ��";

            // ��start_time��end_time���й淶
            CanonicalizeDate(ref start_time,
                "smallbound");
            CanonicalizeDate(ref end_time,
                "largebound");


            string strStartTime = DateTimeUtil.Rfc1123DateTimeString(start_time.ToUniversalTime());
            string strEndTime = DateTimeUtil.Rfc1123DateTimeString(end_time.ToUniversalTime());


            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.SetMessage("���ڻ�ȡΥԼ����� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.GetSystemParameter(
                    stop,
                    "amerce",
                    "dbname",
                    out strDbName,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                if (lRet == 0
                    || string.IsNullOrEmpty(strDbName) == true)
                {
                    strError = "ΥԼ�����û�����á�";
                    return 0;   // not found
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            Debug.Assert(strDbName != "", "");

            strQueryXml = "<target list='" + strDbName + ":" + strFrom + "'>"

                // start
            + "<item><word>"
            + StringUtil.GetXmlStringSimple(strStartTime)
            + "</word><match>left</match><relation>" + StringUtil.GetXmlStringSimple(">=") + "</relation><dataType>number</dataType><maxCount>-1</maxCount></item>"

            + "<operator value='AND' />"
                // end
            + "<item><word>"
            + StringUtil.GetXmlStringSimple(strEndTime)
            + "</word><match>left</match><relation>" + StringUtil.GetXmlStringSimple("<=") + "</relation><dataType>number</dataType><maxCount>-1</maxCount></item>"
            + "<lang>" + this.Lang + "</lang></target>";


            if (String.IsNullOrEmpty(strState) == false
                && strState != "<ȫ��>")
            {
                if (strState == "���շ�")
                    strState = "amerced";
                else if (strState == "�ɽ���"
                    || strState == "�ѽ���")  // TODO: Ӧ���޸�Ϊ���ɽ��㡱
                    strState = "settlemented";

                string strStateXml = "";
                strStateXml = "<target list='" + strDbName + ":" + "״̬" + "'>"
                            + "<item><word>"
                            + StringUtil.GetXmlStringSimple(strState)
                            + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item>"
                            + "<lang>" + this.Lang + "</lang></target>";

                strQueryXml = "<group>" + strQueryXml + "<operator value='AND'/>" + strStateXml + "</group>";
            }


            return 1;
        ERROR1:
            return -1;
        }

        // ����XML����ʽ
        // parameters:
        //      strStartCtlno  ��ʼ�����š�
        //      strEndCtlno ���������š�
        // return:
        //      -1  ����
        //      0   ΥԼ�����û������
        //      1   �ɹ�
        int BuildQueryXml(string strStartCtlno,
            string strEndCtlno,
            string strState,
            out string strQueryXml,
            out string strError)
        {
            strQueryXml = "";
            strError = "";

            if (String.IsNullOrEmpty(strStartCtlno) == true)
                strStartCtlno = "1";
            if (String.IsNullOrEmpty(strEndCtlno) == true)
                strEndCtlno = "9999999999";

            string strDbName = "ΥԼ��";
            string strFrom = "__id";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.SetMessage("���ڻ�ȡΥԼ����� ...");
            stop.BeginLoop();

            try
            {

                long lRet = Channel.GetSystemParameter(
                    stop,
                    "amerce",
                    "dbname",
                    out strDbName,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                if (lRet == 0
                    || string.IsNullOrEmpty(strDbName) == true)
                {
                    strError = "ΥԼ�����û�����á�";
                    return 0;   // not found
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            Debug.Assert(strDbName != "", "");

            strQueryXml = "<target list='" + strDbName + ":" + strFrom + "'>"

            + "<item><word>"
            + strStartCtlno + "-" + strEndCtlno
            + "</word><match>exact</match><relation>" + "draw" + "</relation><dataType>number</dataType><maxCount>-1</maxCount></item>"

            + "<lang>" + this.Lang + "</lang></target>";

            if (String.IsNullOrEmpty(strState) == false
    && strState != "<ȫ��>")
            {
                if (strState == "���շ�")
                    strState = "amerced";
                else if (strState == "�ɽ���"
                    || strState == "�ѽ���")  // Ӧ���޸�Ϊ���ɽ��㡱
                    strState = "settlemented";

                string strStateXml = "";
                strStateXml = "<target list='" + strDbName + ":" + "״̬" + "'>"
                            + "<item><word>"
                            + StringUtil.GetXmlStringSimple(strState)
                            + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item>"
                            + "<lang>" + this.Lang + "</lang></target>";

                strQueryXml = "<group>" + strQueryXml + "<operator value='AND'/>" + strStateXml + "</group>";
            }

            return 1;
        ERROR1:
            return -1;
        }

        int m_nInSearching = 0;

        // �ӡ�ΥԼ�𡱿������ΥԼ��ļ�¼������ʾ��listiview��
        int LoadAmercedRecords(
            bool bQuick,
            string strQueryXml,
            out string strError)
        {
            strError = "";

            this.listView_amerced.Items.Clear();
            // 2008/11/22 new add
            this.SortColumns.Clear();
            SortColumns.ClearColumnSortDisplay(this.listView_amerced.Columns);

            this.toolStripStatusLabel_items_message1.Text = "";
            this.toolStripStatusLabel_items_message2.Text = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.SetMessage("���ڼ���ΥԼ���¼ ...");
            stop.BeginLoop();

            this.EnableControls(false);
            this.Update();
            this.MainForm.Update();

            this.m_nInSearching++;

            try
            {

                /*
                string strDbName = "ΥԼ��";
                string strFrom = "�ɿ�ʱ��";
                string strLang = "zh";
                string strQueryXml = "";
                string strStartTime = DateTimeUtil.Rfc1123DateTimeString(start_time.ToUniversalTime());
                string strEndTime = DateTimeUtil.Rfc1123DateTimeString(end_time.ToUniversalTime());


                long lRet = Channel.GetSystemParameter(
                    stop,
                    "amerce",
                    "dbname",
                    out strDbName,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                if (lRet == 0)
                {
                    strError = "ΥԼ�����û�����á�";
                    return 0;   // not found
                }

                Debug.Assert(strDbName != "", "");

                strQueryXml = "<target list='" + strDbName + ":" + strFrom + "'>"

                    // start
                + "<item><word>"
                + StringUtil.GetXmlStringSimple(strStartTime)
                + "</word><match>left</match><relation>" + StringUtil.GetXmlStringSimple(">=") + "</relation><dataType>number</dataType><maxCount>-1</maxCount></item>"

                + "<operator value='AND' />"
                    // end
                + "<item><word>"
                + StringUtil.GetXmlStringSimple(strEndTime)
                + "</word><match>left</match><relation>" + StringUtil.GetXmlStringSimple("<=") + "</relation><dataType>number</dataType><maxCount>-1</maxCount></item>"
                + "<lang>" + strLang + "</lang></target>";
                 * */

                long lRet = Channel.Search(
                    stop,
                    strQueryXml,
                    "amerced",
                    "", // strOutputStyle
                    out strError);
                if (lRet == 0)
                {
                    strError = "����δ����";
                    return 0;   // not found
                }
                if (lRet == -1)
                    goto ERROR1;

                int nLoadCount = 0;

                long lHitCount = lRet;

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                Record[] searchresults = null;

                stop.SetProgressRange(0, lHitCount);

                // ��ý������װ��listview
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    lRet = Channel.GetSearchResult(
                        stop,
                        "amerced",   // strResultSetName
                        lStart,
                        lPerCount,
                        "id",   // "id,cols"
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (lRet == 0)
                    {
                        strError = "δ����";
                        return 0;
                    }

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null && stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }

                        bool bTempQuick = Control.ModifierKeys == Keys.Control;

                        string strPath = searchresults[i].Path;

                        byte[] timestamp = null;
                        string strXml = "";

                        stop.SetMessage("����װ��ΥԼ���¼ " + strPath + " " + (lStart + i + 1).ToString() + " / " + lHitCount.ToString() + " ...");

                        lRet = Channel.GetRecord(stop,
                            strPath,
                            out timestamp,
                            out strXml,
                            out strError);
                        if (lRet == -1)
                        {
                            if (Channel.ErrorCode == ErrorCode.AccessDenied)
                                goto CONTINUE;

                            goto ERROR1;
                        }

                        int nRet = FillAmercedLine(
                            null,
                            stop,
                            strXml,
                            strPath,
                            (bQuick == true || bTempQuick == true) ? false : true,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        nLoadCount++;
                    CONTINUE:
                        stop.SetProgressValue(lStart + i + 1);
                    }

                    lStart += searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;
                }

                if (nLoadCount != lHitCount)
                {
                    MessageBox.Show(this, "�������� "+lHitCount.ToString()+" ����ʵ��װ�� "+nLoadCount.ToString()+" ��");
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.m_nInSearching--;
                this.EnableControls(true);
            }

            OnItemTypeChanged();
            return 1;
        ERROR1:
            return -1;
        }

        // ˢ��ָ��id��������
        // parameters:
        //      bPrepareStop    �Ƿ�׼��stopѭ��״̬������ⲿ����ǰ�Ѿ�׼�����ˣ�����Ҫ��false����
        int RefreshAmercedRecords(
            bool bPrepareStop,
            string[] ids,
            out string strError)
        {
            strError = "";

            if (bPrepareStop == true)
            {
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.SetMessage("����ˢ��ΥԼ���¼ ...");
                stop.BeginLoop();

                this.EnableControls(false);
            }

            try
            {
                for (int i = 0; i < ids.Length; i++)
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

                    string strID = ids[i];

                    // ����id�õ���¼·��
                    ListViewItem item = GetItemByID(strID);
                    if (item == null)
                    {
                        strError = "id '" + strID + "' ��listview��û���ҵ���Ӧ��item";
                        goto ERROR1;
                    }

                    string strPath = item.SubItems[COLUMN_RECPATH].Text;


                    stop.SetMessage("����װ���¼��Ϣ " + strPath);
                    byte[] timestamp = null;
                    string strXml = "";

                    long lRet = Channel.GetRecord(stop,
                        strPath,
                        out timestamp,
                        out strXml,
                        out strError);
                    if (lRet == -1)
                    {
                        goto ERROR1;
                    }

                    int nRet = FillAmercedLine(
                        item,
                        stop,
                        strXml,
                        strPath,
                        true,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                }
            }
            finally
            {
                if (bPrepareStop == true)
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");

                    this.EnableControls(true);
                }
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ����ID��listview�в����¼·��
        ListViewItem GetItemByID(string strID)
        {
            for (int i = 0; i < this.listView_amerced.Items.Count; i++)
            {
                ListViewItem item = this.listView_amerced.Items[i];

                if (item.Text == strID)
                    return item;
            }

            return null;    // not found
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.button_next.Enabled = bEnable;

            /*
            this.button_settlement.Enabled = bEnable;
            this.button_undoSettlement.Enabled = bEnable;
             * */
            this.toolStrip_items.Enabled = bEnable;

            /*
            this.dateControl_start.Enabled = bEnable;
            this.dateControl_end.Enabled = bEnable;
             * 
            this.textBox_range_startCtlno.Enabled = bEnable;
            this.textBox_range_endCtlno.Enabled = bEnable;
             * */
            this.radioButton_range_amerceOperTime.Enabled = bEnable;
            this.radioButton_range_ctlno.Enabled = bEnable;

            SetRangeControlsEnabled(bEnable);

            this.comboBox_range_state.Enabled = bEnable;
        }

        // ���������ʾ��;��״̬�ַ���
        static string GetDisplayStateText(string strState)
        {
            if (strState == "amerced")
                return "���շ�";

            if (strState == "settlemented")
                return "�½���";

            return strState;
        }

        // ������ڴ洢��;��״̬�ַ���
        static string GetOriginStateText(string strState)
        {
            if (strState == "���շ�")
                return "amerced";

            if (strState == "�½���")
                return "settlemented";

            if (strState == "�ɽ���")   // 2009/1/30 new add
                return "settlemented";


            return strState;
        }

        // ���һ���µ�amerced��
        // stop�Ѿ������BeginLoop()��
        // TODO: Summary���ʱ���������Ϊ��������Ǵ���
        // parameters:
        //      item    ListView������Ϊnull����ʾ��������Ҫ�����µ�����
        int FillAmercedLine(
            ListViewItem item,
            Stop stop,
            string strXml,
            string strRecPath,
            bool bFillSummary,
            out string strError)
        {
            strError = "";

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ�ص�DOMʱ��������: " + ex.Message;
                return -1;
            }

            string strItemBarcode = DomUtil.GetElementText(dom.DocumentElement, "itemBarcode");
            string strItemRecPath = DomUtil.GetElementText(dom.DocumentElement, "itemRecPath");
            string strSummary = "";
            string strReaderBarcode = DomUtil.GetElementText(dom.DocumentElement, "readerBarcode");
            string strLibraryCode = DomUtil.GetElementText(dom.DocumentElement, "libraryCode");
            string strPrice = DomUtil.GetElementText(dom.DocumentElement, "price");
            string strComment = DomUtil.GetElementText(dom.DocumentElement, "comment");
            string strReason = DomUtil.GetElementText(dom.DocumentElement, "reason");
            string strBorrowDate = DomUtil.GetElementText(dom.DocumentElement, "borrowDate");

            strBorrowDate = DateTimeUtil.LocalTime(strBorrowDate, "u");

            string strBorrowPeriod = DomUtil.GetElementText(dom.DocumentElement, "borrowPeriod");
            string strReturnDate = DomUtil.GetElementText(dom.DocumentElement, "returnDate");

            strReturnDate = DateTimeUtil.LocalTime(strReturnDate, "u");

            string strID = DomUtil.GetElementText(dom.DocumentElement, "id");
            string strReturnOperator = DomUtil.GetElementText(dom.DocumentElement, "returnOperator");
            string strState = DomUtil.GetElementText(dom.DocumentElement, "state");

            strState = GetDisplayStateText(strState);   // 2009/1/29 new add

            string strAmerceOperator = DomUtil.GetElementText(dom.DocumentElement, "operator");
            string strAmerceTime = DomUtil.GetElementText(dom.DocumentElement, "operTime");

            strAmerceTime = DateTimeUtil.LocalTime(strAmerceTime, "u");

            string strSettlementOperator = DomUtil.GetElementText(dom.DocumentElement, "settlementOperator");
            string strSettlementTime = DomUtil.GetElementText(dom.DocumentElement, "settlementOperTime");

            strSettlementTime = DateTimeUtil.LocalTime(strSettlementTime, "u");

            if (bFillSummary == true)
            {
                // stop.OnStop += new StopEventHandler(this.DoStop);
                stop.SetMessage("���ڻ�ȡժҪ " + strItemBarcode + " ...");
                // stop.BeginLoop();


                try
                {

                    string strBiblioRecPath = "";
                    long lRet = Channel.GetBiblioSummary(
                        stop,
                        strItemBarcode,
                        strItemRecPath,
                        null,
                        out strBiblioRecPath,
                        out strSummary,
                        out strError);
                    if (lRet == -1)
                    {
                        strSummary = strError;
                        // return -1;
                    }

                }
                finally
                {
                    // stop.EndLoop();
                    // stop.OnStop -= new StopEventHandler(this.DoStop);
                    // stop.Initial("");
                }
            }

            string strOldState = null;

            if (item == null)
            {
                item = new ListViewItem(strID, 0);
                this.listView_amerced.Items.Add(item);
                strOldState = null;
            }
            else
            {
                strOldState = item.SubItems[COLUMN_STATE].Text;
                item.SubItems.Clear();
                item.Text = strID;
            }

            item.SubItems.Add(strState);
            item.SubItems.Add(strReaderBarcode);
            item.SubItems.Add(strLibraryCode);
            item.SubItems.Add(strPrice);
            item.SubItems.Add(strComment);
            item.SubItems.Add(strReason);
            item.SubItems.Add(strBorrowDate);
            item.SubItems.Add(strBorrowPeriod);
            item.SubItems.Add(strReturnDate);
            item.SubItems.Add(strReturnOperator);
            item.SubItems.Add(strItemBarcode);
            item.SubItems.Add(strSummary);

            item.SubItems.Add(strAmerceOperator);
            item.SubItems.Add(strAmerceTime);
            item.SubItems.Add(strSettlementOperator);
            item.SubItems.Add(strSettlementTime);

            item.SubItems.Add(strRecPath);

            SetItemIconAndColor(strOldState,
                item);

            return 0;
        }

        static void SetItemIconAndColor(string strOldState,
            ListViewItem item)
        {
            string strState = item.SubItems[COLUMN_STATE].Text;

            if (strState == "amerced" 
                || strState == "���շ�")
            {
                item.ImageIndex = ITEMTYPE_AMERCED;
                item.BackColor = Color.LightYellow;
                item.ForeColor = SystemColors.WindowText;
            }
            else if (strState == "settlemented"
                || strState == "�½���"
                || strState == "�ɽ���")
            {
                if (strOldState == null)
                {
                    item.ImageIndex = ITEMTYPE_OLD_SETTLEMENTED;
                    item.BackColor = SystemColors.Window;
                    item.ForeColor = Color.Gray;

                    // 2009/1/30 new add
                    ListViewUtil.ChangeItemText(item, COLUMN_STATE, "�ɽ���");
                }
                else if (strOldState == "settlemented"
                    || strOldState == "�½���")
                {
                    // ״̬����
                    Debug.Assert(item.ImageIndex == ITEMTYPE_NEWLY_SETTLEMENTED, "");
                }
                else if (strOldState == "�ɽ���")
                {
                    // ״̬����
                    Debug.Assert(item.ImageIndex == ITEMTYPE_OLD_SETTLEMENTED, "");
                }
                else
                {
                    Debug.Assert(strOldState == "amerced" || strOldState == "���շ�", "");
                    item.ImageIndex = ITEMTYPE_NEWLY_SETTLEMENTED;
                    item.BackColor = Color.LightGreen;
                    item.ForeColor = SystemColors.WindowText;
                }
            }
            else
            {
                item.ImageIndex = ITEMTYPE_UNKNOWN;    // δ֪������
                item.BackColor = SystemColors.Window;
                item.ForeColor = SystemColors.WindowText;
            }

        }

        void SetNextButtonEnable()
        {
            // string strError = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_range)
            {
                this.button_next.Enabled = true;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_items)
            {
                this.button_next.Enabled = true;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
                this.button_next.Enabled = false;
            }
            else
            {
                Debug.Assert(false, "δ֪��tabpage״̬");
            }

        }

        // ��һ�� ��ť
        private void button_next_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_range)
            {
                bool bQuick = Control.ModifierKeys == Keys.Control;

                if (this.radioButton_range_amerceOperTime.Checked == true)  // 2009/1/29 new add
                {

                    // ������������Ƿ�Ϊ�գ��ʹ�С��ϵ
                    if (this.dateControl_start.Value == new DateTime((long)0))
                    {
                        strError = "��δָ����ʼ����";
                        this.dateControl_start.Focus();
                        goto ERROR1;
                    }

                    if (this.dateControl_end.Value == new DateTime((long)0))
                    {
                        strError = "��δָ����������";
                        this.dateControl_end.Focus();
                        goto ERROR1;
                    }

                    if (this.dateControl_start.Value.Ticks > this.dateControl_end.Value.Ticks)
                    {
                        strError = "��ʼ���ڲ��ܴ��ڽ�������";
                        goto ERROR1;
                    }
                }

                string strQueryXml = "";
                int nRet = 0;

                if (this.radioButton_range_amerceOperTime.Checked == true)
                {
                    // return:
                    //      -1  ����
                    //      0   ΥԼ�����û������
                    //      1   �ɹ�
                    nRet = BuildQueryXml(this.dateControl_start.Value,
                        this.dateControl_end.Value,
                        this.comboBox_range_state.Text,
                        out strQueryXml,
                        out strError);
                }
                else
                {

                    // ����XML����ʽ
                    // parameters:
                    //      strStartCtlno  ��ʼ�����š�
                    //      strEndCtlno ���������š�
                    // return:
                    //      -1  ����
                    //      0   ΥԼ�����û������
                    //      1   �ɹ�
                    nRet = BuildQueryXml(this.textBox_range_startCtlno.Text,
                        this.textBox_range_endCtlno.Text,
                        this.comboBox_range_state.Text,
                        out strQueryXml,
                        out strError);
                }


                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                    goto ERROR1;    // ΥԼ���û������

                this.tabControl_main.SelectedTab = this.tabPage_items;

                // װ�ؼ�¼
                nRet = LoadAmercedRecords(
                    bQuick,
                    strQueryXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nRet == 0)
                {
                    MessageBox.Show(this, strError);
                    return;
                }

                return;
            }

            else if (this.tabControl_main.SelectedTab == this.tabPage_items)
            {

                int nAmercedCount = 0;
                int nOldSettlementedCount = 0;
                int nNewlySettlementedCount = 0;
                int nOtherCount = 0;
                GetItemTypesCount(out nAmercedCount,
                    out nOldSettlementedCount,
                    out nNewlySettlementedCount,
                    out nOtherCount);

                if (nAmercedCount == 0)
                {
                    if (nNewlySettlementedCount == 0
                        && nOldSettlementedCount == 0)
                    {
                        MessageBox.Show(this, "��ǰ�б���û���κ��½���;ɽ��������޷���ӡ�������ݵĽ����嵥");
                        goto END1;
                    }
                }

                if (nAmercedCount > 0)
                {
                    // ��ʾ��������δ���������
                    DialogResult result = MessageBox.Show(
                        this,
                        "�Ե�ǰ����������δ����������н��㣿\r\n\r\n(Yes ���㣬������һ��; No �����㣬����һ��; Cancel �����㣬Ҳ������һ��)",
                        "SettlementForm",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.Cancel)
                    {
                        // Ҳ���л�ҳ
                        goto END1;
                    }
                    if (result == DialogResult.No)
                    {
                        // �����㣬�����л�ҳ
                        this.tabControl_main.SelectedTab = this.tabPage_print;
                        this.button_next.Enabled = false;
                        goto END1;
                    }

                    // ���㣬���л�ҳ
                    menu_selectAmerced_Click(this, null);

                    if (
                        (this.toolStripButton_items_useCheck.Checked == true
                        && this.listView_amerced.CheckedItems.Count != 0)
                        ||
                        (this.toolStripButton_items_useCheck.Checked == false
                        && this.listView_amerced.SelectedItems.Count != 0)
                        )
                    {
                        button_settlement_Click(this, null);
                    }
                }

                this.tabControl_main.SelectedTab = this.tabPage_print;
                this.button_next.Enabled = false;
            }
            else if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
            }
            else
            {
                Debug.Assert(false, "δ֪��tabpage״̬");
            }

        END1:
            this.SetNextButtonEnable();


            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void radioButton_range_amerceOperTime_CheckedChanged(object sender, EventArgs e)
        {
            SetRangeControlsEnabled(true);
        }

        private void radioButton_range_ctlno_CheckedChanged(object sender, EventArgs e)
        {
            SetRangeControlsEnabled(true);
        }

        void SetRangeControlsEnabled(bool bEnable)
        {
            // 2009/1/29 new add
            if (bEnable == false)
            {
                this.dateControl_start.Enabled = false;
                this.dateControl_end.Enabled = false;

                this.textBox_range_startCtlno.Enabled = false;
                this.textBox_range_endCtlno.Enabled = false;
                return;
            }

            if (this.radioButton_range_amerceOperTime.Checked == true)
            {
                this.dateControl_start.Enabled = true;
                this.dateControl_end.Enabled = true;

                this.textBox_range_startCtlno.Enabled = false;
                this.textBox_range_endCtlno.Enabled = false;
            }
            else
            {
                this.dateControl_start.Enabled = false;
                this.dateControl_end.Enabled = false;

                this.textBox_range_startCtlno.Enabled = true;
                this.textBox_range_endCtlno.Enabled = true;
            }

        }

        // ����
        private void button_settlement_Click(object sender, EventArgs e)
        {
            SettlementAction("settlement");
        }

        // ��������
        private void button_undoSettlement_Click(object sender, EventArgs e)
        {
            SettlementAction("undosettlement");
        }

        // ���㡢��������ɾ��
        // TODO: �Ծɽ�������ĳ�����Ҫ���أ�����Ҫ����һ��
        // parameters:
        //      bSettlement ���Ϊtrue����ʾ���㣻���Ϊfalse����ʾ��������
        void SettlementAction(string strAction)
        {
            string strError = "";

            string strOperName = "";

            if (strAction == "settlement")
                strOperName = "����";
            else if (strAction == "undosettlement")
                strOperName = "��������";
            else if (strAction == "delete")
            {
                strOperName = "ɾ���ѽ�������";
            }
            else
            {
                strError = "δ��ʶ��� strAction ����ֵ '" + strAction + "'";
                goto ERROR1;
            }

            // ����id�б�
            List<string> total_ids = new List<string>();

            List<ListViewItem> items = new List<ListViewItem>();

            if (this.toolStripButton_items_useCheck.Checked == true)
            {
                if (this.listView_amerced.CheckedItems.Count == 0)
                {
                    strError = "��δ��ѡҪ" + strOperName + "������";
                    goto ERROR1;
                }

                for (int i = 0; i < this.listView_amerced.CheckedItems.Count; i++)
                {
                    ListViewItem item = this.listView_amerced.CheckedItems[i];
                    items.Add(item);
                }
            }
            else
            {
                if (this.listView_amerced.SelectedItems.Count == 0)
                {
                    strError = "��δѡ��Ҫ" + strOperName + "������";
                    goto ERROR1;
                }

                foreach (ListViewItem item in this.listView_amerced.SelectedItems)
                {
                    // ListViewItem item = this.listView_amerced.SelectedItems[i];
                    items.Add(item);
                }
            }

            int nAmercedCount = 0;
            int nOldSettlementedCount = 0;
            int nNewlySettlementedCount = 0;
            int nOtherCount = 0;

            // �Ƚ��м��;���
            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                if (item.ImageIndex == ITEMTYPE_AMERCED)
                    nAmercedCount++;
                else if (item.ImageIndex == ITEMTYPE_OLD_SETTLEMENTED)
                    nOldSettlementedCount++;
                else if (item.ImageIndex == ITEMTYPE_NEWLY_SETTLEMENTED)
                    nNewlySettlementedCount++;
                else
                    nOtherCount++;
            }

            string strWarning = "";

            if (strAction == "settlement")
            {
                if (nAmercedCount == 0)
                {
                    strError = "��ǰѡ���������У���û�а���״̬Ϊ�����շѡ��������������޷�����";
                    goto ERROR1;
                }

                if (nOldSettlementedCount + nNewlySettlementedCount > 0)
                    strWarning = "��ǰѡ������������ "
                        + (nOldSettlementedCount + nNewlySettlementedCount).ToString()
                        + " ���Ѿ����������ڽ�������н���������";
            }
            else if (strAction == "undosettlement")
            {
                if (nOldSettlementedCount + nNewlySettlementedCount == 0)
                {
                    strError = "��ǰѡ���������У���û�а���״̬Ϊ���½��㡱�͡��ɽ��㡱�����������������޷�����";
                    goto ERROR1;
                }

                if (nOldSettlementedCount > 0)
                {
                    strWarning = "��ǰѡ������������ "
                        + nOldSettlementedCount.ToString()
                        + " ���ɽ�������(���Ǳ��ν��������)�����Ҫ����(��)���г�������Ĳ�������Ӱ�쵽��ǰ�Ѿ���ӡ���Ľ����嵥��׼ȷ�Ժ�Ȩ���ԡ�\r\n\r\nȷʵҪ�����ǽ��г�������Ĳ���?";
                    DialogResult result = MessageBox.Show(
                        this,
                        strWarning,
                        "SettlementForm",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Cancel)
                    {
                        strError = "���γ�������Ĳ����Ѿ���ȫ������(�����¡��ɽ�������)��";
                        goto ERROR1;
                    }

                    strWarning = "";
                }

                if (nAmercedCount > 0)
                    strWarning = "��ǰѡ������������ "
    + nAmercedCount.ToString()
    + " ��δ����(�����շ�״̬)������ڳ�������Ĳ����н���������";

            }
            else if (strAction == "delete")
            {
                if (nOldSettlementedCount + nNewlySettlementedCount == 0)
                {
                    strError = "��ǰѡ���������У���û�а���״̬Ϊ���½��㡱�͡��ɽ��㡱�����ɾ���ѽ�������Ĳ����޷�����";
                    goto ERROR1;
                }

                if (nOldSettlementedCount > 0)
                {
                    strWarning = "ȷʵҪ�����ݿ���ɾ����ǰѡ���� "
    + (nOldSettlementedCount + nNewlySettlementedCount).ToString()
    + " ���ѽ�������?\r\n\r\n(���棺ɾ�������ǲ������))";
                    DialogResult result = MessageBox.Show(
                        this,
                        strWarning,
                        "SettlementForm",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Cancel)
                    {
                        strError = "ȡ������";
                        goto ERROR1;
                    }

                    strWarning = "";
                }

                if (nAmercedCount > 0)
                    strWarning = "��ǰѡ������������ "
+ nAmercedCount.ToString()
+ " ��δ����(�����շ�״̬)�������ɾ���ѽ�������Ĳ����н���������";

            }

            if (String.IsNullOrEmpty(strWarning) == false)
            {
                MessageBox.Show(this, strWarning);
            }

            // �Ƿ�ǰ�˾Ϳɽ���ì���Լ�飿
            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                string strID = item.Text;
                string strState = item.SubItems[COLUMN_STATE].Text;

                if (strAction == "settlement")
                {
                    if (strState == "settlemented"
                        || strState == "�½���"
                        || strState == "�ɽ���")
                    {
                        /*
                        strError = "IDΪ " + strID + " ������״̬Ϊ"+strState+"���޷��ٽ���"+strAction+"��������ȥ��������Ĺ�ѡ״̬���������ύ����";
                        goto ERROR1;
                         * */
                        continue;
                    }
                }

                if (strAction == "undosettlement")
                {
                    if (strState == "amerced" 
                        || strState == "���շ�")
                    {
                        /*
                        strError = "IDΪ " + strID + " ������״̬Ϊ"+strState+"���޷�����"+strAction+"��������ȥ��������Ĺ�ѡ״̬���������ύ����";
                        goto ERROR1;
                         * */
                        continue;
                    }
                }

                if (strAction == "delete")
                {
                    if (strState == "amerced" 
                        || strState == "���շ�")
                    {
                        /*
                        strError = "IDΪ " + strID + " ������״̬Ϊ"+strState+"���޷�����"+strAction+"��������ȥ��������Ĺ�ѡ״̬���������ύ����"; 
                        goto ERROR1;
                         * */
                        continue;
                    }
                }

                total_ids.Add(strID);
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.SetMessage("���ڽ���"+strOperName+" ...");
            stop.BeginLoop();

            stop.SetProgressRange(0, total_ids.Count);

            this.EnableControls(false);

            try
            {
                int nDone = 0;

                int nPerCount = 10;
                int nBatchCount = (total_ids.Count / nPerCount) + ((total_ids.Count % nPerCount) != 0 ? 1 : 0);
                for (int j = 0; j < nBatchCount; j++)
                {
                    // ÿ�ִ���10��id
                    int nThisCount = Math.Min(total_ids.Count - j * nPerCount, nPerCount);
                    string[] ids = new string[nThisCount];
                    total_ids.CopyTo(j * nPerCount, ids, 0, nThisCount);

                    long lRet = Channel.Settlement(
                        stop,
                        strAction,
                        ids,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;


                    // ˢ��
                    int nRet = RefreshAmercedRecords(
                        false,
                        ids,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    nDone += nThisCount;
                    stop.SetProgressValue(nDone);
                }

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControls(true);
            }

            // ����ɹ�
            MessageBox.Show(this, strOperName + "�ɹ���(�����¼�� " + total_ids.Count.ToString() + " ��)");
            this.OnItemTypeChanged();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ������popup�˵�
        private void listView_amerced_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            int nAmercedCount = 0;
            int nOldSettlementedCount = 0;
            int nNewlySettlementedCount = 0;
            int nOtherCount = 0;
            GetItemTypesCount(out nAmercedCount,
                out nOldSettlementedCount,
                out nNewlySettlementedCount,
                out nOtherCount);

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            string strSelectedName = "ѡ����";
            if (this.toolStripButton_items_useCheck.Checked == true)
                strSelectedName = "��ѡ��";

            int nSelectedCount = 0;
            if (this.toolStripButton_items_useCheck.Checked == true)
                nSelectedCount = this.listView_amerced.CheckedItems.Count;
            else
                nSelectedCount = this.listView_amerced.SelectedItems.Count;

            // ����
            menuItem = new MenuItem("����" + strSelectedName + " " + nSelectedCount.ToString() + " ������(&S)");
            menuItem.Click += new System.EventHandler(this.toolStripButton_items_settlement_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ��������
            menuItem = new MenuItem("��������" + strSelectedName + " " + nSelectedCount.ToString() + " ������(&S)");
            menuItem.Click += new System.EventHandler(this.toolStripButton_items_undoSettlement_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("�Ƴ�"+strSelectedName+" " + nSelectedCount.ToString() + " ������(&R)");
            menuItem.Click += new System.EventHandler(this.menu_removeSelectedItems_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            string strText = "ȫѡ(&A)";
            if (this.toolStripButton_items_useCheck.Checked == true)
                strText = "ȫ��ѡ(&H)";

            menuItem = new MenuItem(strText);
            menuItem.Click += new System.EventHandler(this.menu_selectAll_Click);
            contextMenu.MenuItems.Add(menuItem);

            if (this.toolStripButton_items_useCheck.Checked == true)
                strText = "ȫ�����ѡ(&U)";
            else
                strText = "ȫ��ѡ(&U)";

            menuItem = new MenuItem(strText);
            menuItem.Click += new System.EventHandler(this.menu_unSelectAll_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            if (this.toolStripButton_items_useCheck.Checked == true)
                strText = "��ѡ";
            else 
                strText = "ѡ��";

            menuItem = new MenuItem(strText+"ȫ��("+nAmercedCount.ToString()+"��) ���շ� ����(&A)");
            menuItem.Click += new System.EventHandler(this.menu_selectAmerced_Click);
            if (nAmercedCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem(strText+"ȫ��("+(nNewlySettlementedCount + nOldSettlementedCount).ToString()+"��) �ѽ���(�����½���;ɽ���) ����(&S)");
            menuItem.Click += new System.EventHandler(this.menu_selectSettlemented_Click);
            if (nNewlySettlementedCount + nOldSettlementedCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ʹ�ù�ѡ(&C)");
            menuItem.Click += new System.EventHandler(this.menu_toggleUseCheck_Click);
            if (this.toolStripButton_items_useCheck.Checked == true)
                menuItem.Checked = true;
            else
                menuItem.Checked = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // ע�����ѽ��㡱�����½���;ɽ���
            menuItem = new MenuItem("�����ݿ���ɾ����ǰ" + strText + "�� �ѽ���(�����½���;ɽ���) ����(&D)");
            menuItem.Click += new System.EventHandler(this.menu_deleteSettlementedItemsFromDb_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // 
            menuItem = new MenuItem("������ǰ" + strText + "�����XML�ļ�(&E)");
            menuItem.Click += new System.EventHandler(this.menu_exportToXmlFile_Click);
            if (nSelectedCount == 0 || this.m_nInSearching > 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView_amerced, new Point(e.X, e.Y));		

        }

        void menu_exportToXmlFile_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.m_nInSearching > 0)
            {
                strError = "��ǰ���ڽ�����һ������������Ҫ��ֹͣ�������ܽ��е���ΥԼ����¼�Ĳ���...";
                goto ERROR1;
            }

            nRet = ExportXmlFile(out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        string m_strOutputXmlFilename = "";

        int ExportXmlFile(out string strError)
        {
            strError = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.SetMessage("���ڵ���ΥԼ���¼ ...");
            stop.BeginLoop();

            this.EnableControls(false);

            try
            {
                List<ListViewItem> items = new List<ListViewItem>();

                if (this.toolStripButton_items_useCheck.Checked == true)
                {
                    if (this.listView_amerced.CheckedItems.Count == 0)
                    {
                        strError = "��δ��ѡҪ����������";
                        goto ERROR1;
                    }

                    for (int i = 0; i < this.listView_amerced.CheckedItems.Count; i++)
                    {
                        ListViewItem item = this.listView_amerced.CheckedItems[i];
                        items.Add(item);
                    }
                }
                else
                {
                    if (this.listView_amerced.SelectedItems.Count == 0)
                    {
                        strError = "��δѡ��Ҫ����������";
                        goto ERROR1;
                    }

                    foreach (ListViewItem item in this.listView_amerced.SelectedItems)
                    {
                        // ListViewItem item = this.listView_amerced.SelectedItems[i];
                        items.Add(item);
                    }
                }

                // ׼��XML����ļ�
                SaveFileDialog dlg = new SaveFileDialog(); 
                dlg.Title = "��ָ�������XML�ļ�";
                dlg.OverwritePrompt = true;
                dlg.CreatePrompt = false;
                dlg.FileName = m_strOutputXmlFilename;
                dlg.Filter = "XML�ļ� (*.xml)|*.xml|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() != DialogResult.OK)
                    return 0;

                m_strOutputXmlFilename = dlg.FileName;

                // ������ļ���

                using (FileStream outputfile = File.Create(m_strOutputXmlFilename))
                {
                    XmlTextWriter writer = null;   // Xml��ʽ���
                    writer = new XmlTextWriter(outputfile, Encoding.UTF8);
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 4;

                    // д����Ԫ��
                    writer.WriteStartDocument();
                    writer.WriteStartElement("dprms", "collection", DpNs.dprms);

                    stop.SetProgressRange(0, items.Count);
                    for (int i = 0; i < items.Count; i++)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null
                            && stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }

                        stop.SetMessage("���ڵ���ΥԼ���¼ " + (i + 1).ToString() + " / " + items.Count.ToString() + " ...");

                        ListViewItem item = items[i];
                        string strRecPath = ListViewUtil.GetItemText(item, COLUMN_RECPATH);

                        string strXml = "";
                        byte[] timestamp = null;
                        long lRet = this.Channel.GetRecord(
                            stop,
                            strRecPath,
                            out timestamp,
                            out strXml,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;    // TODO: ��ʾ����

                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(strXml);
                        }
                        catch (Exception ex)
                        {
                            strError = "XMLװ��DOMʱ����: " + ex.Message;
                            goto ERROR1;
                        }
                        dom.DocumentElement.WriteTo(writer);

                        stop.SetProgressValue(i+1);
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Close();
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControls(true);
            }
            return 1;
        ERROR1:
            return -1;
        }

        // �����ݿ���ɾ�� ��ѡ�� �ѽ���(settlemented) ����
        void menu_deleteSettlementedItemsFromDb_Click(object sender, EventArgs e)
        {
            SettlementAction("delete");
        }

        // �Ƴ�ѡ��(��ѡ)������
        void menu_removeSelectedItems_Click(object sender, EventArgs e)
        {
            if (this.toolStripButton_items_useCheck.Checked == false)
            {

                if (this.listView_amerced.SelectedItems.Count == 0)
                {
                    MessageBox.Show(this, "��δѡ���κ���Ҫ�Ƴ�������");
                    return;
                }

                DialogResult result = MessageBox.Show(
                    this,
                    "ȷʵҪ���б����Ƴ���ѡ���� "+this.listView_amerced.SelectedItems.Count.ToString()+" ������?\r\n\r\n(ע����������������ݿ���ɾ����¼)",
                    "SettlementForm",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Cancel)
                    return;

                List<ListViewItem> items = new List<ListViewItem>();

                foreach (ListViewItem item in this.listView_amerced.SelectedItems)
                {
                    items.Add(item);
                }

                for (int i = 0; i < items.Count; i++)
                {
                    this.listView_amerced.Items.Remove(items[i]);
                }
            }
            else
            {
                if (this.listView_amerced.CheckedIndices.Count == 0)
                {
                    MessageBox.Show(this, "��δ��ѡ�κ���Ҫ�Ƴ�������");
                    return;
                }

                DialogResult result = MessageBox.Show(
                    this,
                    "ȷʵҪ���б����Ƴ�����ѡ�� " + this.listView_amerced.CheckedItems.Count.ToString() + " ������?\r\n\r\n(ע����������������ݿ���ɾ����¼)",
                    "SettlementForm",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Cancel)
                    return;

                for (int i = this.listView_amerced.CheckedIndices.Count - 1; i >= 0; i--)
                {
                    this.listView_amerced.Items.RemoveAt(this.listView_amerced.CheckedIndices[i]);
                }
            }

            this.OnItemTypeChanged();
        }

        // ȫ(��)ѡ
        void menu_selectAll_Click(object sender, EventArgs e)
        {
            if (this.toolStripButton_items_useCheck.Checked == false)
            {
                for (int i = 0; i < this.listView_amerced.Items.Count; i++)
                {
                    this.listView_amerced.Items[i].Selected = true;
                }
            }
            else
            {
                for (int i = 0; i < this.listView_amerced.Items.Count; i++)
                {
                    this.listView_amerced.Items[i].Checked = true;
                }
            }
        }

        // ȫ����(��)ѡ
        void menu_unSelectAll_Click(object sender, EventArgs e)
        {
            if (this.toolStripButton_items_useCheck.Checked == false)
            {
                for (int i = 0; i < this.listView_amerced.Items.Count; i++)
                {
                    this.listView_amerced.Items[i].Selected = false;
                }
            }
            else
            {
                for (int i = 0; i < this.listView_amerced.Items.Count; i++)
                {
                    this.listView_amerced.Items[i].Checked = false;
                }
            }
        }

        // ֻ(��)ѡ amerced ״̬����
        void menu_selectAmerced_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.listView_amerced.Items.Count; i++)
            {
                ListViewItem item = this.listView_amerced.Items[i];
                string strState = item.SubItems[COLUMN_STATE].Text;

                if (this.toolStripButton_items_useCheck.Checked == true)
                {
                    if (strState == "amerced"
                        || strState == "���շ�")
                        item.Checked = true;
                    else
                        item.Checked = false;
                }
                else
                {
                    if (strState == "amerced"
                        || strState == "���շ�")
                        item.Selected = true;
                    else
                        item.Selected = false;
                }
            }
        }

        // ѡ��ȫ�� settelmented ״̬����
        // ע�������¾ɽ��������ѡ��
        // TODO: �Ծɽ�������ĳ�����Ҫ���أ�����Ҫ����һ��
        void menu_selectSettlemented_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.listView_amerced.Items.Count; i++)
            {
                ListViewItem item = this.listView_amerced.Items[i];
                string strState = item.SubItems[COLUMN_STATE].Text;

                if (this.toolStripButton_items_useCheck.Checked == true)
                {
                    if (strState == "settlemented"
                        || strState == "�½���"
                        || strState == "�ɽ���")
                        item.Checked = true;
                    else
                        item.Checked = false;
                }
                else
                {
                    if (strState == "settlemented"
                        || strState == "�½���"
                        || strState == "�ɽ���")
                        item.Selected = true;
                    else
                        item.Selected = false;
                }
            }
        }

        // �Ƿ�����۸���?
        static bool HasPriceColumn(PrintOption option)
        {
            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];

                string strText = StringUtil.GetLeft(column.Name);


                if (strText == "price"
                    || strText == "���")
                    return true;
            }

            return false;
        }



        #region ��ӡ��ع���

        // �Լ�����ӡ��������м�飬�����ǲ��Ƿ��Ͻ�������
        // return:
        //      -1  ����
        //      0   ����
        //      1   ��Υ�����̵�������֣���strError������
        int CheckBeforeSettlementPrint(List<ListViewItem> items,
            out string strError)
        {
            strError = "";
            int nAmercedStateCount = 0;
            int nOldSettlementStateCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                if (item == null)
                    continue;

                string strState = item.SubItems[COLUMN_STATE].Text;

                if (strState == "amerced"
                    || strState == "���շ�")
                {
                    nAmercedStateCount++;
                }

                if (item.ImageIndex == ITEMTYPE_OLD_SETTLEMENTED)
                    nOldSettlementStateCount++;
            }

            if (nAmercedStateCount > 0)
            {
                strError = "��ǰ���ӡ��������У��� " +nAmercedStateCount.ToString()+ " �����շѵ�δ���������(�Ƶ�ɫ������)�������Щ��������ӡ��ͳ�ƣ���ô�ܼƺ�С�ƽ����ܴ�������";
            }

            if (nOldSettlementStateCount > 0)
            {
                strError += "��ǰ���ӡ��������У��� " + nOldSettlementStateCount.ToString() + " ������(�Ǳ���)���������(��ɫ���ֵ�����)�������Щ��������ӡ��ͳ�ƣ���ô�ܼƺ�С�ƽ����ܴ����ν����";
            }

            if (String.IsNullOrEmpty(strError) == false)
                return 1;

            return 0;
        }

        void PrintList(List<ListViewItem> items)
        {
            string strError = "";

            // ����һ��html�ļ�������ʾ��HtmlPrintForm�С�

            List<string> filenames = null;
            try
            {
                // Debug.Assert(false, "");

                // ����htmlҳ��
                int nRet = BuildHtml(
                    items,
                    out filenames,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                HtmlPrintForm printform = new HtmlPrintForm();

                printform.Text = "��ӡ�����嵥";
                printform.MainForm = this.MainForm;
                printform.Filenames = filenames;
                this.MainForm.AppInfo.LinkFormState(printform, "printform_state");
                printform.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(printform);
            }

            finally
            {
                if (filenames != null)
                    Global.DeleteFiles(filenames);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ������������ų��˿ն���Ҳ����С����
        static int GetItemCount(List<ListViewItem> items)
        {
            int nResult = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                    nResult++;
            }

            return nResult;
        }

        // ����htmlҳ��
        int BuildHtml(
            List<ListViewItem> items,
            out List<string> filenames,
            out string strError)
        {
            strError = "";

            Hashtable macro_table = new Hashtable();

            // ��ô�ӡ����
            PrintOption option = new SettlementPrintOption(this.MainForm.DataDir);
            option.LoadData(this.MainForm.AppInfo,
                "settlement_printoption");

            // ��鰴�շ���С��ʱ���Ƿ���м۸���
            if (this.checkBox_sumByAmerceOperator.Checked == true)
            {
                // ����Ƿ���м۸��У�
                if (HasPriceColumn(option) == false)
                {
                    MessageBox.Show(this, "���棺���ӡҪ�󡮰��շ���С�ƽ��������۸��в�δ�����ڴ�ӡ���С����С�ƵĽ���޷���ӡ������");
                }
            }



            // �����ҳ����
            int nTablePageCount = items.Count / option.LinesPerPage;
            if ((items.Count % option.LinesPerPage) != 0)
                nTablePageCount++;

            int nPageCount = nTablePageCount + 1;

            macro_table["%pagecount%"] = nPageCount.ToString();
            macro_table["%linesperpage%"] = option.LinesPerPage.ToString();
            macro_table["%date%"] = DateTime.Now.ToLongDateString();


            filenames = new List<string>();    // ÿҳһ���ļ�������������������ļ���

            string strFileNamePrefix = this.MainForm.DataDir + "\\~settlement";

            string strFileName = "";

            // �����Ϣҳ
            // TODO: Ҫ���ӡ�ͳ��ҳ��ģ�幦�ܡ������ģ��������ѭ�����У���һ���Ѷ�
            {
                int nItemCount = GetItemCount(items);
                string strTotalPrice = GetTotalPrice(items).ToString();

                macro_table["%itemcount%"] = nItemCount.ToString();
                macro_table["%totalprice%"] = strTotalPrice;


                macro_table["%pageno%"] = "1";

                strFileName = strFileNamePrefix + "0" + ".html";

                filenames.Add(strFileName);

                BuildPageTop(option,
                    macro_table,
                    strFileName,
                    false);

                // ������
                StreamUtil.WriteText(strFileName,
                    "<table class='totalsum'>");

                // �б�����
                StreamUtil.WriteText(strFileName,
                    "<tr class='totalsum_columtitle'>");
                StreamUtil.WriteText(strFileName,
                    "<td class='person'>�շ���</td>");
                StreamUtil.WriteText(strFileName,
                    "<td class='itemcount'>������</td>");
                StreamUtil.WriteText(strFileName,
                    "<td class='price'>���</td>");
                StreamUtil.WriteText(strFileName,
                    "</tr>");

                // �ܼ���

                StreamUtil.WriteText(strFileName,
                    "<tr class='totalsum_line'>");
                StreamUtil.WriteText(strFileName,
                    "<td class='person'>�ܼ�</td>");
                StreamUtil.WriteText(strFileName,
                    "<td class='itemcount'>" + nItemCount.ToString() + "</td>");
                StreamUtil.WriteText(strFileName,
                    "<td class='price'>" + strTotalPrice + "</td>");
                StreamUtil.WriteText(strFileName,
                    "</tr>");


                if (this.checkBox_sumByAmerceOperator.Checked == true)
                {
                    // �շ���С��

                    // С����ʾ��
                    StreamUtil.WriteText(strFileName,
                        "<tr class='amerceoperatorsum_titleline'>");
                    StreamUtil.WriteText(strFileName,
                        "<td class='amerceoperatorsum_titleline' colspan='3'>(����Ϊ���շ��߷����С�ƽ��)</td>");
                    StreamUtil.WriteText(strFileName,
                        "</tr>");

                    for (int i = 0; i < items.Count; i++)
                    {
                        ListViewItem item = items[i];
                        if (item == null)
                        {
                            string strAmerceOperator = "";
                            int nSumItemCount = 0;
                            decimal sum = ComputeSameAmerceOperatorSumPrice(items,
                                i,
                                out strAmerceOperator,
                                out nSumItemCount);
                            StreamUtil.WriteText(strFileName,
                                "<tr class='amerceoperatorsum_line'>");
                            StreamUtil.WriteText(strFileName,
                                "<td class='person'>" + strAmerceOperator + "</td>");
                            StreamUtil.WriteText(strFileName,
                                "<td class='itemcount'>" + nSumItemCount.ToString() + "</td>");
                            StreamUtil.WriteText(strFileName,
                                "<td class='price'>" + sum.ToString() + "</td>");

                            StreamUtil.WriteText(strFileName,
                                "</tr>");
                        }
                    }

                    StreamUtil.WriteText(strFileName,
                        "</table>");

                }

                BuildPageBottom(option,
                    macro_table,
                    strFileName,
                    false);
            }

            // ���ҳѭ��
            for (int i = 0; i < nTablePageCount; i++)
            {
                macro_table["%pageno%"] = (i + 1 + 1).ToString();

                strFileName = strFileNamePrefix + (i + 1).ToString() + ".html";

                filenames.Add(strFileName);

                BuildPageTop(option,
                    macro_table,
                    strFileName,
                    true);
                // ��ѭ��
                for (int j = 0; j < option.LinesPerPage; j++)
                {
                    BuildTableLine(option,
                        items,
                        strFileName, i, j);
                }

                BuildPageBottom(option,
                    macro_table,
                    strFileName,
                    true);
            }


            return 0;
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
            bool bOutputTable)
        {
            /*
            string strLibraryServerUrl = this.MainForm.AppInfo.GetString(
    "config",
    "circulation_server_url",
    "");
            int pos = strLibraryServerUrl.LastIndexOf("/");
            if (pos != -1)
                strLibraryServerUrl = strLibraryServerUrl.Substring(0, pos);
             * */

            // string strCssUrl = this.MainForm.LibraryServerDir + "/settlement.css";
            // 2009/10/10 changed
            string strCssUrl = GetAutoCssUrl(option, "settlement.css");

            /*
            // 2009/10/9 new add
            string strCssFilePath = option.GetTemplatePageFilePath("css");  // ��Сд������
            if (String.IsNullOrEmpty(strCssFilePath) == false)
                strCssUrl = strCssFilePath;
            else
                strCssUrl = this.MainForm.LibraryServerDir + "/settlement.css";    // ȱʡ��
             * */

            string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";

            StreamUtil.WriteText(strFileName,
                "<html><head>" + strLink + "</head><body>");


            // ҳü
            string strPageHeaderText = option.PageHeader;

            if (String.IsNullOrEmpty(strPageHeaderText) == false)
            {
                strPageHeaderText = Global.MacroString(macro_table,
                    strPageHeaderText);

                StreamUtil.WriteText(strFileName,
                    "<div class='pageheader'>" + strPageHeaderText + "</div>");

                /*
                StreamUtil.WriteText(strFileName,
                    "<hr class='pageheader' />");
                 * */
            }

            // ������
            string strTableTitleText = option.TableTitle;

            if (String.IsNullOrEmpty(strTableTitleText) == false)
            {

                strTableTitleText = Global.MacroString(macro_table,
                    strTableTitleText);

                StreamUtil.WriteText(strFileName,
                    "<div class='tabletitle'>" + strTableTitleText + "</div>");
            }

            if (bOutputTable == true)
            {

                // ���ʼ
                StreamUtil.WriteText(strFileName,
                    "<table class='table'>");   //   border='1'

                // ��Ŀ����
                StreamUtil.WriteText(strFileName,
                    "<tr class='column'>");

                for (int i = 0; i < option.Columns.Count; i++)
                {
                    Column column = option.Columns[i];

                    string strCaption = column.Caption;

                    // ���û��caption���壬��Ų��name����
                    if (String.IsNullOrEmpty(strCaption) == true)
                        strCaption = column.Name;

                    string strClass = StringUtil.GetLeft(column.Name);

                    StreamUtil.WriteText(strFileName,
                        "<td class='" + strClass + "'>" + strCaption + "</td>");
                }

                StreamUtil.WriteText(strFileName,
                    "</tr>");

            }

            return 0;
        }

        // ���ܼ۸񡣼ٶ�nIndex�����л���(ͬһamerceOperator��������һ��)
        static decimal ComputeSameAmerceOperatorSumPrice(List<ListViewItem> items,
            int nIndex,
            out string strAmerceOperator,
            out int nCount)
        {
            strAmerceOperator = "";
            nCount = 0;

            if (nIndex - 1 < 0)
                return 0;

            Debug.Assert(items[nIndex] == null, "�������ӦΪnull");

            decimal total = 0;

            for (int i = nIndex - 1; i >= 0; i--)
            {
                ListViewItem item = items[i];

                if (item == null)
                    break;

                // ˳�����շ���
                if (String.IsNullOrEmpty(strAmerceOperator) == true)
                    strAmerceOperator = GetColumnContent(item, "amerceOperator");

                string strPrice = GetColumnContent(item, "price");

                // ��ȡ��������
                string strPurePrice = PriceUtil.GetPurePrice(strPrice);

                nCount++;   // ������û�м۸��ַ�������Щ����

                if (String.IsNullOrEmpty(strPurePrice) == true)
                    continue;

                total += Convert.ToDecimal(strPurePrice);
            }

            return total;
        }

        int BuildTableLine(PrintOption option,
    List<ListViewItem> items,
    string strFileName,
    int nPage,
    int nLine)
        {
            int nIndex = nPage * option.LinesPerPage + nLine;

            if (nIndex >= items.Count)
                return 0;

            ListViewItem item = items[nIndex];

            string strAmerceOperator = "";
            string strSumContent = "";
            int nItemCount = 0;
            if (item == null)
            {
                // ����ǰ��ļ۸�
                strSumContent = ComputeSameAmerceOperatorSumPrice(items, nIndex, out strAmerceOperator, out nItemCount).ToString();
            }

            // ��Ŀ����
            string strLineContent = "";

            for (int i = 0; i < option.Columns.Count; i++)
            {
                Column column = option.Columns[i];

                string strContent = "";

                // ��ʾ��Ҫ��ӡС����
                if (item == null)
                {
                    string strColumnName = StringUtil.GetLeft(column.Name);
                    if ( strColumnName == "price"
                        || strColumnName == "���")
                    {
                        strContent = strAmerceOperator + " �� " + nItemCount.ToString() + "�� С�ƣ�" + strSumContent;
                    }
                    else if (strColumnName == "amerceOperator"
                        || strColumnName == "�շ���")
                    {
                        strContent = strAmerceOperator;
                    }
                }
                else
                {
                    strContent = GetColumnContent(item,
                        column.Name);
                }

                if (strContent == "!!!#")
                    strContent = ((nPage * option.LinesPerPage) + nLine + 1).ToString();

                // �ض��ַ���
                if (column.MaxChars != -1)
                {
                    if (strContent.Length > column.MaxChars)
                    {
                        strContent = strContent.Substring(0, column.MaxChars);
                        strContent += "...";
                    }
                }

                if (String.IsNullOrEmpty(strContent) == true)
                    strContent = "&nbsp;";

                string strClass = StringUtil.GetLeft(column.Name);

                strLineContent +=
                    "<td class='" + strClass + "'>" + strContent + "</td>";
            }

            if (item != null)
            {
                StreamUtil.WriteText(strFileName,
                    "<tr class='content'>");
            }
            else
            {
                StreamUtil.WriteText(strFileName,
                    "<tr class='content_amerceoperator_sum'>");
            }

            StreamUtil.WriteText(strFileName,
                strLineContent);

            StreamUtil.WriteText(strFileName,
                "</tr>");

            return 0;
        }

        int BuildPageBottom(PrintOption option,
    Hashtable macro_table,
    string strFileName,
    bool bOutputTable)
        {


            if (bOutputTable == true)
            {
                // ������
                StreamUtil.WriteText(strFileName,
                    "</table>");
            }

            // ҳ��
            string strPageFooterText = option.PageFooter;

            if (String.IsNullOrEmpty(strPageFooterText) == false)
            {
                /*
                StreamUtil.WriteText(strFileName,
                    "<hr class='pagefooter' />");
                 * */


                strPageFooterText = StringUtil.MacroString(macro_table,
                    strPageFooterText);

                StreamUtil.WriteText(strFileName,
        "<div class='pagefooter'>" + strPageFooterText + "</div>");
            }


            StreamUtil.WriteText(strFileName, "</body></html>");

            return 0;
        }

        static decimal GetTotalPrice(List<ListViewItem> items)
        {
            decimal total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                if (item == null)
                    continue;

                string strPrice = "";

                try
                {
                    strPrice = item.SubItems[COLUMN_PRICE].Text;
                }
                catch
                {
                    continue;
                }

                if (String.IsNullOrEmpty(strPrice) == true)
                    continue;

                // ��ȡ��������
                string strPurePrice = PriceUtil.GetPurePrice(strPrice);

                if (String.IsNullOrEmpty(strPurePrice) == true)
                    continue;

                total += Convert.ToDecimal(strPurePrice);
            }

            return total;
        }

#endregion

        // ��ӡѡ��
        private void button_print_option_Click(object sender, EventArgs e)
        {
            // ���ñ���ͷ��
            PrintOption option = new SettlementPrintOption(this.MainForm.DataDir);
            option.LoadData(this.MainForm.AppInfo,
                "settlement_printoption");


            PrintOptionDlg dlg = new PrintOptionDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.MainForm = this.MainForm;
            dlg.DataDir = this.MainForm.DataDir;
            dlg.PrintOption = option;
            dlg.ColumnItems = new string[] {
                "no -- ���",
                "id -- ��¼ID",
                "state -- ״̬",
                "readerBarcode -- ����֤�����",
                "summary -- ժҪ",
                "price -- ���",
                "comment -- ע��",
                "reason -- ԭ��",
                "borrowDate -- ��������",
                "borrowPeriod -- ����ʱ��",
                "returnDate -- ��������",
                "returnOperator -- ���������",
                "barcode -- �������",
                "amerceOperator -- �շ���",
                "amerceTime -- �շ�����",
                "settlementOperator -- ������",
                "settlementTime -- ��������",
                "recpath -- ��¼·��"
            };

            this.MainForm.AppInfo.LinkFormState(dlg, "settlement_printoption_formstate");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            option.SaveData(this.MainForm.AppInfo,
                "settlement_printoption");
        }

        // �����Ŀ����
        static string GetColumnContent(ListViewItem item,
            string strColumnName)
        {
            // ȥ��"-- ?????"����
            string strText = StringUtil.GetLeft(strColumnName);

            try
            {

                // Ҫ��Ӣ�Ķ�����
                switch (strText)
                {
                    case "no":
                    case "���":
                        return "!!!#";  // ����ֵ����ʾ���
                    case "id":
                    case "��¼ID":
                        return item.SubItems[COLUMN_ID].Text;
                    case "state":
                    case "״̬":
                        return item.SubItems[COLUMN_STATE].Text;
                    case "readerBarcode":
                    case "����֤�����":
                        return item.SubItems[COLUMN_READERBARCODE].Text;
                    case "summary":
                    case "ժҪ":
                        return item.SubItems[COLUMN_SUMMARY].Text;
                    case "price":
                    case "���":
                        return item.SubItems[COLUMN_PRICE].Text;
                    case "comment":
                    case "ע��":
                        return item.SubItems[COLUMN_COMMENT].Text;
                    case "reason":
                    case "ԭ��":
                        return item.SubItems[COLUMN_REASON].Text;
                    case "borrowDate":
                    case "��������":
                        return item.SubItems[COLUMN_BORROWDATE].Text;
                    case "borrowPeriod":
                    case "����ʱ��":
                        return item.SubItems[COLUMN_BORROWPERIOD].Text;
                    case "returnDate":
                    case "��������":
                        return item.SubItems[COLUMN_RETURNDATE].Text;
                    case "returnOperator":
                    case "���������":
                        return item.SubItems[COLUMN_RETURNOPERATOR].Text;
                    case "barcode":
                    case "�������":
                        return item.SubItems[COLUMN_BARCODE].Text;
                    case "amerceOperator":
                    case "�շ���":
                        return item.SubItems[COLUMN_AMERCEOPERATOR].Text;
                    case "amerceTime":
                    case "�շ�����":
                        return item.SubItems[COLUMN_AMERCETIME].Text;


                    case "settlementOperator":
                    case "������":
                        return item.SubItems[COLUMN_SETTLEMENTOPERATOR].Text;
                    case "settlementTime":
                    case "��������":
                        return item.SubItems[COLUMN_SETTLEMENTTIME].Text;
                    case "recpath":
                    case "��¼·��":
                        return item.SubItems[COLUMN_RECPATH].Text;
                    default:
                        return "undefined column";
                }
            }

            catch
            {
                return null;    // ��ʾû�����subitem�±�
            }

        }

        private void listView_amerced_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;
            this.SortColumns.SetFirstColumn(nClickColumn,
                this.listView_amerced.Columns);

            // ����
            this.listView_amerced.ListViewItemSorter = new SortColumnsComparer(this.SortColumns);

            this.listView_amerced.ListViewItemSorter = null;
        }

        private void tabControl_main_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetNextButtonEnable();
        }

        // ��ӡ���ν��㲿��
        private void button_print_printSettlemented_Click(object sender, EventArgs e)
        {
            string strError = "";

            // ��鵱ǰ����״̬�Ͱ��շ���С��֮���Ƿ����ì��
            if (this.checkBox_sumByAmerceOperator.Checked == true)
            {

                if (this.SortColumns.Count != 0
                    && this.SortColumns[0].No == COLUMN_AMERCEOPERATOR)
                {
                }
                else
                {
                    ColumnClickEventArgs e1 = new ColumnClickEventArgs(COLUMN_AMERCEOPERATOR);
                    listView_amerced_ColumnClick(this, e1);
                    MessageBox.Show(this, "���ӡҪ�󡮰��շ���С�ƽ�����ӡǰ������Զ���������������շ��ߡ�����");
                }

            }

            // ���Ҫ��ӡС���У���Ҫ��items�����в���null���󣬷����ӡʱ��Դ���
            string strPrevAmerceOperator = "";

            List<ListViewItem> items = new List<ListViewItem>();
            for (int i = 0; i < this.listView_amerced.Items.Count; i++)
            {
                ListViewItem item = this.listView_amerced.Items[i];

                // �ų��������ô�ӡ�Ķ���
                if (item.ImageIndex != ITEMTYPE_NEWLY_SETTLEMENTED)
                    continue;

                string strAmerceOperator = item.SubItems[COLUMN_AMERCEOPERATOR].Text;

                if (this.checkBox_sumByAmerceOperator.Checked == true)
                {
                    if (strAmerceOperator != strPrevAmerceOperator
                        && items.Count != 0)
                    {
                        items.Add(null);    // ����һ���ն��󣬱�ʾ����Ҫ��ӡС����
                    }
                }

                items.Add(item);

                if (this.checkBox_sumByAmerceOperator.Checked == true)
                {
                    strPrevAmerceOperator = strAmerceOperator;
                }
            }

            // ��Ҫ���� ���һ��С����
            if (this.checkBox_sumByAmerceOperator.Checked == true)
            {
                if (items.Count > 0
                    && items[items.Count - 1] != null)
                    items.Add(null);
            }

            if (items.Count == 0)
            {
                strError = "��ǰ������û��״̬Ϊ �½��� ���������޷���ӡ";
                goto ERROR1;
            }

            /*

            // �Լ�����ӡ��������м�飬�����ǲ��Ƿ��Ͻ�������
            // return:
            //      -1  ����
            //      0   ����
            //      1   ��Υ�����̵�������֣���strError������
            int nRet = CheckBeforeSettlementPrint(items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 1)
                MessageBox.Show(this, "����: " + strError);
            */
            PrintList(items);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��ӡȫ��
        private void button_print_printAll_Click(object sender, EventArgs e)
        {
            // ��鵱ǰ����״̬�Ͱ��շ���С��֮���Ƿ����ì��
            if (this.checkBox_sumByAmerceOperator.Checked == true)
            {

                if (this.SortColumns.Count != 0
                    && this.SortColumns[0].No == COLUMN_AMERCEOPERATOR)
                {
                }
                else
                {
                    ColumnClickEventArgs e1 = new ColumnClickEventArgs(COLUMN_AMERCEOPERATOR);
                    listView_amerced_ColumnClick(this, e1);
                    MessageBox.Show(this, "���ӡҪ�󡮰��շ���С�ƽ�����ӡǰ������Զ���������������շ��ߡ�����");


                    // MessageBox.Show(this, "���棺��ӡҪ�󡮰��շ���С�ƽ�������ӡǰ���������δ�����շ��ߡ�����������ӡ����С�ƽ��᲻׼ȷ��\r\n\r\nҪ����������������ڴ�ӡǰ���������㡮�շ��ߡ������⣬ȷ����������");
                }

            }

            // ���Ҫ��ӡС���У���Ҫ��items�����в���null���󣬷����ӡʱ��Դ���
            string strPrevAmerceOperator = "";

            List<ListViewItem> items = new List<ListViewItem>();
            for (int i = 0; i < this.listView_amerced.Items.Count; i++)
            {
                ListViewItem item = this.listView_amerced.Items[i];

                string strAmerceOperator = item.SubItems[COLUMN_AMERCEOPERATOR].Text;

                if (this.checkBox_sumByAmerceOperator.Checked == true)
                {
                    if (strAmerceOperator != strPrevAmerceOperator
                        && items.Count != 0)
                    {
                        items.Add(null);    // ����һ���ն��󣬱�ʾ����Ҫ��ӡС����
                    }
                }

                items.Add(item);

                if (this.checkBox_sumByAmerceOperator.Checked == true)
                {
                    strPrevAmerceOperator = strAmerceOperator;
                }
            }

            // ��Ҫ���� ���һ��С����
            if (this.checkBox_sumByAmerceOperator.Checked == true)
            {
                if (items.Count > 0
                    && items[items.Count - 1] != null)
                    items.Add(null);
            }

            string strError = "";

            // �Լ�����ӡ��������м�飬�����ǲ��Ƿ��Ͻ�������
            // return:
            //      -1  ����
            //      0   ����
            //      1   ��Υ�����̵�������֣���strError������
            int nRet = CheckBeforeSettlementPrint(items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 1)
                MessageBox.Show(this, "����: " + strError);

            PrintList(items);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void SettlementForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);
        }

        // �����б��и�������ĸ���
        void GetItemTypesCount(out int nAmercedCount,
            out int nOldSettlementedCount,
            out int nNewlySettlementedCount,
            out int nOtherCount)
        {
            nAmercedCount = 0;
            nOldSettlementedCount = 0;
            nNewlySettlementedCount = 0;
            nOtherCount = 0;

            for(int i=0;i<this.listView_amerced.Items.Count;i++)
            {
                ListViewItem item = this.listView_amerced.Items[i];
                if (item.ImageIndex == ITEMTYPE_AMERCED)
                    nAmercedCount++;
                else if (item.ImageIndex == ITEMTYPE_OLD_SETTLEMENTED)
                    nOldSettlementedCount++;
                else if (item.ImageIndex == ITEMTYPE_NEWLY_SETTLEMENTED)
                    nNewlySettlementedCount++;
                else
                    nOtherCount++;
            }
        }

        // ��������������ͷ����仯
        void OnItemTypeChanged()
        {
            int nAmercedCount = 0;
            int nOldSettlementedCount = 0;
            int nNewlySettlementedCount = 0;
            int nOtherCount = 0;
            GetItemTypesCount(out nAmercedCount,
                out nOldSettlementedCount,
                out nNewlySettlementedCount,
                out nOtherCount);

            if (nAmercedCount == 0)
                this.toolStripButton_items_selectAmercedItems.Enabled = false;
            else
                this.toolStripButton_items_selectAmercedItems.Enabled = true;

            if (nNewlySettlementedCount + nOldSettlementedCount == 0)
                this.toolStripButton_items_selectSettlementedItems.Enabled = false;
            else
                this.toolStripButton_items_selectSettlementedItems.Enabled = true;

            /*
            string strText = "";
            int nSelectedCount = 0;
            if (this.toolStripButton_items_useCheck.Checked == true)
            {
                strText = "��ѡ";
                nSelectedCount = this.listView_amerced.CheckedItems.Count;
            }
            else
            {
                strText = "ѡ��";
                nSelectedCount = this.listView_amerced.SelectedItems.Count;
            }*/

            this.toolStripStatusLabel_items_message1.Text = "���շ� " + nAmercedCount.ToString() + ", �½��� " + nNewlySettlementedCount.ToString() + ", �ɽ��� " + nOldSettlementedCount.ToString();
            // this.label_items_message.Text = "���շ� " + nAmercedCount.ToString() + ", �½��� " + nNewlySettlementedCount.ToString() + ", �ɽ��� " + nOldSettlementedCount.ToString() + "    " + strText + " " + nSelectedCount.ToString();
        }

        // ѡ�����ı�
        private void listView_amerced_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.toolStripButton_items_useCheck.Checked == true)
                return;

            if (this.listView_amerced.SelectedItems.Count == 0)
            {
                this.toolStripButton_items_remove.Enabled = false;

                this.toolStripButton_items_settlement.Enabled = false;
                this.toolStripButton_items_undoSettlement.Enabled = false;

            }
            else
            {
                this.toolStripButton_items_remove.Enabled = true;

                this.toolStripButton_items_settlement.Enabled = true;
                this.toolStripButton_items_undoSettlement.Enabled = true;
            }

            this.toolStripStatusLabel_items_message2.Text = "ѡ�� " + this.listView_amerced.SelectedItems.Count.ToString();
        }

        // ��ѡ�����ı�
        private void listView_amerced_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (this.toolStripButton_items_useCheck.Checked == false)
                return;

            if (this.listView_amerced.CheckedItems.Count == 0)
            {
                this.toolStripButton_items_remove.Enabled = false;

                this.toolStripButton_items_settlement.Enabled = false;
                this.toolStripButton_items_undoSettlement.Enabled = false;
            }
            else
            {
                this.toolStripButton_items_remove.Enabled = true;

                this.toolStripButton_items_settlement.Enabled = true;
                this.toolStripButton_items_undoSettlement.Enabled = true;
            }

            if (e != null)
            {
                // ��checked״̬����������Ӵ֣����߷�֮
                if (e.Item.Checked == true)
                    e.Item.Font = new Font(e.Item.Font, FontStyle.Bold);
                else
                    e.Item.Font = new Font(e.Item.Font, FontStyle.Regular);
            }

            this.toolStripStatusLabel_items_message2.Text = "��ѡ " + this.listView_amerced.CheckedItems.Count.ToString();
        }

        // �л���ʹ�ù�ѡ��״̬
        void menu_toggleUseCheck_Click(object sender, EventArgs e)
        {
            if (this.toolStripButton_items_useCheck.Checked == true)
                this.toolStripButton_items_useCheck.Checked = false;
            else
                this.toolStripButton_items_useCheck.Checked = true;

            toolStripButton_items_useCheck_Click(sender, e);
        }

        private void toolStripButton_items_useCheck_Click(object sender, EventArgs e)
        {
            // �ú��ַ�ʽ��ѡ��?

            if (this.toolStripButton_items_useCheck.Checked == true)
            {
                this.listView_amerced.CheckBoxes = true;

                // ��ԭ����selected��Ϊchecked
                for (int i = 0; i < this.listView_amerced.Items.Count; i++)
                {
                    ListViewItem item = this.listView_amerced.Items[i];
                    if (item.Selected == true)
                    {
                        item.Checked = true;
                        item.Selected = false;
                    }
                    else
                        item.Checked = false;
                }

                listView_amerced_ItemChecked(sender, null);
            }
            else
            {
                // ��ԭ����checked��Ϊselected
                for (int i = 0; i < this.listView_amerced.Items.Count; i++)
                {
                    ListViewItem item = this.listView_amerced.Items[i];
                    if (item.Checked == true)
                    {
                        item.Selected = true;
                        item.Checked = false;

                        item.Font = new Font(item.Font, FontStyle.Regular);
                    }
                    else
                        item.Selected = false;
                }

                this.listView_amerced.CheckBoxes = false;

                listView_amerced_SelectedIndexChanged(sender, null);
            }
        }

        private void toolStripButton_items_remove_Click(object sender, EventArgs e)
        {
            menu_removeSelectedItems_Click(sender, e);
        }

        private void toolStripButton_items_selectAll_Click(object sender, EventArgs e)
        {
            menu_selectAll_Click(sender, e);
        }

        private void toolStripButton_items_unSelectAll_Click(object sender, EventArgs e)
        {
            menu_unSelectAll_Click(sender, e);
        }

        private void toolStripButton_items_selectAmercedItems_Click(object sender, EventArgs e)
        {
            menu_selectAmerced_Click(sender, e);
        }

        private void toolStripButton_items_selectSettlementedItems_Click(object sender, EventArgs e)
        {
            menu_selectSettlemented_Click(sender, e);
        }

        private void toolStripButton_items_undoSettlement_Click(object sender, EventArgs e)
        {
            SettlementAction("undosettlement");
        }

        private void toolStripButton_items_settlement_Click(object sender, EventArgs e)
        {
            SettlementAction("settlement");
        }


    }

    // �������ض�ȱʡֵ��PrintOption������
    internal class SettlementPrintOption : PrintOption
    {
        public SettlementPrintOption(string strDataDir)
        {
            this.DataDir = strDataDir;

            this.PageHeaderDefault = "%date% �շѽ����嵥 - (�� %pagecount% ҳ)";
            this.PageFooterDefault = "%pageno%/%pagecount%";

            this.TableTitleDefault = "%date% �շѽ����嵥";

            this.LinesPerPageDefault = 20;

            // Columnsȱʡֵ
            Columns.Clear();

            // "id -- ��¼ID",
            Column column = new Column();
            column.Name = "id -- ��¼ID";
            column.Caption = "��¼ID";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "readerBarcode -- ����֤�����",
            column = new Column();
            column.Name = "readerBarcode -- ����֤�����";
            column.Caption = "����֤�����";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "reason -- ԭ��",
            column = new Column();
            column.Name = "reason -- ԭ��";
            column.Caption = "ԭ��";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "price -- ���",
            column = new Column();
            column.Name = "price -- ���";
            column.Caption = "���";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "amerceOperator -- �շ���",
            column = new Column();
            column.Name = "amerceOperator -- �շ���";
            column.Caption = "�շ���";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "amerceTime -- �շ�����",
            column = new Column();
            column.Name = "amerceTime -- �շ�����";
            column.Caption = "�շ�����";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "settlementOperator -- ������",
            column = new Column();
            column.Name = "settlementOperator -- ������";
            column.Caption = "������";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "settlementTime -- ��������",
            column = new Column();
            column.Name = "settlementTime -- ��������";
            column.Caption = "��������";
            column.MaxChars = -1;
            this.Columns.Add(column);

            // "recpath -- ��¼·��"
            column = new Column();
            column.Name = "recpath -- ��¼·��";
            column.Caption = "��¼·��";
            column.MaxChars = -1;
            this.Columns.Add(column);

        }
    }
}