using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    // ��ȡ�Ŵ�
    // λ��UtilĿ¼
    /// <summary>
    /// ��ȡ�Ŵ�
    /// </summary>
    public partial class CallNumberForm : MyForm
    {
        // XmlDocument cfg_dom = null;

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;
#endif

        /// <summary>
        /// ���������ź�
        /// </summary>
        public AutoResetEvent EventFinish = new AutoResetEvent(false);

        string m_strMaxNumber = null;
        string m_strTailNumber = null;

        /// <summary>
        /// �Ƿ�Ҫ(�ڴ��ڴ򿪺�)�Զ���������
        /// </summary>
        public bool AutoBeginSearch = false;

        const int WM_INITIAL = API.WM_USER + 201;

        /// <summary>
        /// ����ȡ�ŵ�ʵ���¼��·��������У��ͳ�ƹ��̣��ų��Լ�
        /// </summary>
        public string MyselfItemRecPath = "";    // ����ȡ�ŵ�ʵ���¼��·��������У��ͳ�ƹ��̣��ų��Լ���
        
        /// <summary>
        /// ����ȡ�ŵ�ʵ���¼����������Ŀ��¼��·��������У��ͳ�ƹ��̣��ų����Լ�ͬ����һ����Ŀ��¼������ʵ���¼
        /// </summary>
        public string MyselfParentRecPath = "";    // ����ȡ�ŵ�ʵ���¼����������Ŀ��¼��·��������У��ͳ�ƹ��̣��ų����Լ�ͬ����һ����Ŀ��¼������ʵ���¼��

        /// <summary>
        /// ����ȡ�ŵ���Ŀ��¼������ʵ���¼�е�������ȡ��
        /// </summary>
        public List<CallNumberItem> MyselfCallNumberItems = null;   // ����ȡ�ŵ���Ŀ��¼������ʵ���¼�е�������ȡ��

        const int TYPE_NORMAL = 0;
        const int TYPE_ERROR = 1;
        const int TYPE_CURRENT = 2;

        #region ����к�

        /// <summary>
        /// ����к�: ���¼·��
        /// </summary>
        public const int COLUMN_ITEMRECPATH = 0;
        /// <summary>
        /// ����к�: ��ȡ��
        /// </summary>
        public const int COLUMN_CALLNUMBER = 1;
        /// <summary>
        /// ����к�: ժҪ
        /// </summary>
        public const int COLUMN_SUMMARY = 2;
        /// <summary>
        /// ����к�: �ݲص�
        /// </summary>
        public const int COLUMN_LOCATION = 3;
        /// <summary>
        /// ����к�: �������
        /// </summary>
        public const int COLUMN_BARCODE = 4;
        /// <summary>
        /// ����к�: ��Ŀ��¼·��
        /// </summary>
        public const int COLUMN_BIBLIORECPATH = 5;

        #endregion

        /// <summary>
        /// ���캯��
        /// </summary>
        public CallNumberForm()
        {
            InitializeComponent();

            ListViewProperty prop = new ListViewProperty();
            this.listView_number.Tag = prop;
            // ��һ�����⣬��¼·��
            prop.SetSortStyle(0, ColumnSortStyle.RecPath);
        }

        private void CallNumberForm_Load(object sender, EventArgs e)
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

            this.GetValueTable -= new GetValueTableEventHandler(CallNumberForm_GetValueTable);
            this.GetValueTable += new GetValueTableEventHandler(CallNumberForm_GetValueTable);


            // ���
            if (String.IsNullOrEmpty(this.textBox_classNumber.Text) == true)
            {
                this.textBox_classNumber.Text = this.MainForm.AppInfo.GetString(
                    "callnumberform",
                    "classnumber",
                    "");
            }

            // �����ݲصص�
            if (m_bLocationSetted == false)
            {
                this.comboBox_location.Text = this.MainForm.AppInfo.GetString(
                    "callnumberform",
                    "location",
                    "");
                m_bLocationSetted = true;
            }

            // �Ƿ�Ҫ���������
            this.checkBox_returnBrowseCols.Checked = this.MainForm.AppInfo.GetBoolean(
                    "callnumberform",
                    "return_browse_cols",
                    true);

            string strWidths = this.MainForm.AppInfo.GetString(
    "callnumberform",
    "record_list_column_width",
    "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_number,
                    strWidths,
                    true);
            }

            /*
            if (this.cfg_dom == null)
            {
                string strError = "";
                // ��ʼ����ȡ��������Ϣ
                int nRet = InitialCallNumberCfgInfo(out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);
            }*/

            if (this.AutoBeginSearch == true)
            {
                API.PostMessage(this.Handle, WM_INITIAL, 0, 0);
            }
        }

        void CallNumberForm_GetValueTable(object sender, GetValueTableEventArgs e)
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

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_INITIAL:
                    {
                        string strError = "";
                        int nRet = 0;

                        ArrangementInfo info = null;
        // return:
        //      -1  error
        //      0   not found
        //      1   found
                        nRet = this.MainForm.GetArrangementInfo(this.LocationString,
                            out info, 
                            out strError);

#if NO
                        string strArrangeGroupName = "";
                        string strZhongcihaoDbname = "";
                        string strClassType = "";
                        string strQufenhaoType = "";

                        // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                        // return:
                        //      -1  error
                        //      0   notd found
                        //      1   found
                        nRet = this.MainForm.GetCallNumberInfo(this.LocationString,
                            out strArrangeGroupName,
                            out strZhongcihaoDbname,
                            out strClassType,
                            out strQufenhaoType,
                            out strError);
#endif
                        if (nRet == 0)
                        {
                            this.button_searchClass_Click(null, null);
                            return;
                        }
                        if (nRet == -1)
                        {
                            this.button_searchClass_Click(null, null);
                            return;
                        }

                        if (String.IsNullOrEmpty(info.ZhongcihaoDbname) == true) // strZhongcihaoDbname
                        {
                            this.button_searchClass_Click(null, null);
                        }
                        else
                        {
                            this.button_searchDouble_Click(null, null);
                        }
                        return;
                        /*
                    ERROR1:
                        MessageBox.Show(this, strError);
                        return;
                         * */
                    }
//                    return;
            }
            base.DefWndProc(ref m);
        }

        private void CallNumberForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void CallNumberForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            // ���
            this.MainForm.AppInfo.SetString(
                "callnumberform",
                "classnumber",
                this.textBox_classNumber.Text);

            // �����ݲصص�
            this.MainForm.AppInfo.SetString(
                "callnumberform",
                "location",
                this.comboBox_location.Text);

            // �Ƿ�Ҫ���������
            this.MainForm.AppInfo.SetBoolean(
                    "callnumberform",
                    "return_browse_cols",
                    this.checkBox_returnBrowseCols.Checked);

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_number);
            this.MainForm.AppInfo.SetString(
                "callnumberform",
                "record_list_column_width",
                strWidths);


            EventFinish.Set();
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

        bool m_bLocationSetted = false;
        /// <summary>
        /// �����ݲصص�
        /// </summary>
        public string LocationString
        {
            get
            {
                if (this.comboBox_location.Text == "<��>")
                    return "";

                return this.comboBox_location.Text;
            }
            set
            {
                this.comboBox_location.Text = value;
                m_bLocationSetted = true;
            }
        }

        /// <summary>
        /// ���
        /// </summary>
        public string ClassNumber
        {
            get
            {
                return this.textBox_classNumber.Text;
            }
            set
            {
                this.textBox_classNumber.Text = value;
            }
        }

        /// <summary>
        /// ����
        /// </summary>
        public string MaxNumber
        {
            get
            {
                if (String.IsNullOrEmpty(m_strMaxNumber) == true)
                {
                    string strError = "";

                    int nRet = FillList(true, 
                        "",
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    return m_strMaxNumber;
                ERROR1:
                    throw (new Exception(strError));
                }
                return m_strMaxNumber;
            }
            set
            {
                this.textBox_maxNumber.Text = value;
                m_strMaxNumber = value;
            }
        }

        /// <summary>
        /// β��
        /// </summary>
        public string TailNumber
        {
            get
            {
                if (String.IsNullOrEmpty(m_strTailNumber) == true)
                {
                    string strError = "";

                    string strTailNumber = "";
                    int nRet = SearchTailNumber(out strTailNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    m_strTailNumber = strTailNumber;
                    return m_strTailNumber;
                ERROR1:
                    throw (new Exception(strError));

                }
                return m_strTailNumber;

            }
            set
            {
                string strError = "";
                string strOutputNumber = "";
                int nRet = SaveTailNumber(value,
                    out strOutputNumber,
                    out strError);
                if (nRet == -1)
                    throw (new Exception(strError));
                else
                    m_strTailNumber = strOutputNumber;	// ˢ�¼���
            }
        }

        private void button_searchClass_Click(object sender, EventArgs e)
        {
            string strError = "";

            EventFinish.Reset();
            try
            {
                int nRet = FillList(true, 
                    "",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ����ñ���β��
                return;
            }
            finally
            {
                EventFinish.Set();
            }

        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.comboBox_location.Enabled = bEnable;
            this.textBox_classNumber.Enabled = bEnable;
            this.textBox_maxNumber.Enabled = bEnable;
            this.textBox_tailNumber.Enabled = bEnable;

            this.button_copyMaxNumber.Enabled = bEnable;
            this.button_getTailNumber.Enabled = bEnable;
            this.button_pushTailNumber.Enabled = bEnable;
            this.button_saveTailNumber.Enabled = bEnable;
            this.button_searchClass.Enabled = bEnable;
            this.button_searchDouble.Enabled = bEnable;
        }

        // �������������������ݿ��������ִκŷ������任ΪAPIʹ�õ���̬
        static string GetArrangeGroupName(string strLocation)
        {
            if (strLocation == "<��>")
                strLocation = "";

            // �ݲصص���Ϊ������ɵ�
            if (String.IsNullOrEmpty(strLocation) == true)
                return "!";

            // �����һ���ַ���!���ţ������Ƿ�����
            if (strLocation[0] == '!')
                return strLocation.Substring(1);

            // û�У����ţ������������ݲصص���
            return "!" + strLocation;
        }

        int FillList(bool bSort,
            string strStyle,
            out string strError)
        {
            strError = "";
            // int nRet = 0;

            Hashtable dbname_table = new Hashtable();   // ʵ����� --> ��Ŀ���� ���ձ�

            this.listView_number.Items.Clear();
            this.MaxNumber = "";

            if (this.ClassNumber == "")
            {
                strError = "��δָ�������";
                return -1;
            }

            /*
            if (this.LocationString == "")
            {
                strError = "��δָ�������ݲصص�";
                return -1;
            }
             * */

            bool bFast = StringUtil.IsInList("fast", strStyle);

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ���ͬ����ʵ���¼ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                string strQueryXml = "";

                long lRet = Channel.SearchOneClassCallNumber(
                    stop,
                    GetArrangeGroupName(this.LocationString),
                    // "!" + this.BiblioDbName,
                    this.ClassNumber,
                    "callnumber",
                    out strQueryXml,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                if (lRet == 0)
                {
                    strError = "û�����еļ�¼��";
                    // return 0;   // not found
                    goto END1;
                }


                long lHitCount = lRet;

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);
                CallNumberSearchResult[] searchresults = null;

                if (stop != null)
                    stop.SetProgressRange(0, lHitCount);

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

                    long lCurrentPerCount = lPerCount;

                    bool bShift = Control.ModifierKeys == Keys.Shift;
                    string strBrowseStyle = "cols";
                    if (bShift == true || this.checkBox_returnBrowseCols.Checked == false
                        || bFast == true)
                    {
                        strBrowseStyle = "";
                        lCurrentPerCount = lPerCount * 10;
                    }

                    stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    lRet = Channel.GetCallNumberSearchResult(
                        stop,
                        GetArrangeGroupName(this.LocationString),
                        // "!" + this.BiblioDbName,
                        "callnumber",   // strResultSetName
                        lStart,
                        lCurrentPerCount,
                        strBrowseStyle, // style
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

                    // ����������
                    this.listView_number.BeginUpdate();
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        CallNumberSearchResult result_item = searchresults[i];
                        ListViewItem item = new ListViewItem();
                        item.ImageIndex = TYPE_NORMAL;
                        item.Text = result_item.ItemRecPath;

                        if (String.IsNullOrEmpty(result_item.ErrorInfo) == false)
                        {
                            ListViewUtil.ChangeItemText(item, COLUMN_CALLNUMBER, result_item.ErrorInfo);
                            item.ImageIndex = TYPE_ERROR;
                        }
                        else
                            ListViewUtil.ChangeItemText(item, COLUMN_CALLNUMBER, result_item.CallNumber);

                        ListViewUtil.ChangeItemText(item, COLUMN_LOCATION, result_item.Location);
                        ListViewUtil.ChangeItemText(item, COLUMN_BARCODE, result_item.Barcode);

                        string strItemDbName = Global.GetDbName(result_item.ItemRecPath);

                        Debug.Assert(String.IsNullOrEmpty(strItemDbName) == false, "");

                        string strBiblioDbName = (string)dbname_table[strItemDbName];

                        if (String.IsNullOrEmpty(strBiblioDbName) == true)
                        {
                            strBiblioDbName = this.MainForm.GetBiblioDbNameFromItemDbName(strItemDbName);
                            dbname_table[strItemDbName] = strBiblioDbName;
                        }

                        if (string.IsNullOrEmpty(result_item.ParentID) == false)
                            ListViewUtil.ChangeItemText(item, COLUMN_BIBLIORECPATH, strBiblioDbName + "/" + result_item.ParentID);


                        /*
                        if (CheckNumber(result_item.Zhongcihao) == true)
                            item.ImageIndex = TYPE_NORMAL;
                        else
                            item.ImageIndex = TYPE_ERROR;
                         * */

                        /*
                        if (result_item.Cols != null)
                        {
                            if (result_item.Cols.Length > 0)
                                item.SubItems.Add(result_item.Cols[0]);
                            if (result_item.Cols.Length > 1)
                                item.SubItems.Add(result_item.Cols[1]);
                        }*/


                        this.listView_number.Items.Add(item);
                        if (stop != null)
                            stop.SetProgressValue(lStart + i + 1);
                    }
                    this.listView_number.EndUpdate();

                    lStart += searchresults.Length;
                    if (lStart >= lHitCount || lPerCount <= 0)
                        break;
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                EnableControls(true);
            }

            END1:
            // ���ڴ������µ���ȡ����ˢ��
            RefreshByNewlyCallNumberItems();

            if (bSort == true)
            {
                // ����
                this.listView_number.ListViewItemSorter = new CallNumberListViewItemComparer();
                this.listView_number.ListViewItemSorter = null; // 2011/10/19

                SetGroupBackcolor(this.listView_number,
                    COLUMN_BIBLIORECPATH);

                this.MaxNumber = GetZhongcihaoPart(GetTopNumber(this.listView_number));

                /*
                // ���ظ��ִκŵ�������������ɫ�����
                ColorDup();

                this.MaxNumber = GetTopNumber(this.listView_number);    // this.listView_number.Items[0].SubItems[1].Text;
                 * */

            }

            EnsureCurrentItemsVisible();

            if (bFast == false)
            {
                int nRet = GetAllBiblioSummary(out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            return 0;
        ERROR1:
            return -1;
        }

        /*
        // ���ڴ������µ���ȡ��ˢ��list
        void RefreshByNewlyCallNumberItems()
        {
            if (this.MyselfCallNumberItems == null)
                return;
            if (this.MyselfCallNumberItems.Count == 0)
                return;

            for (int i = 0; i < this.MyselfCallNumberItems.Count; i++)
            {
                CallNumberItem item = this.MyselfCallNumberItems[i];

                ListViewItem list_item = ListViewUtil.FindItem(this.listView_number, item.RecPath, COLUMN_ITEMRECPATH);
                if (list_item == null)
                    continue;

                ListViewUtil.ChangeItemText(list_item, COLUMN_CALLNUMBER, item.CallNumber);
            }
        }*/

        void EnsureCurrentItemsVisible()
        {
            if (this.m_currentItems.Count == 0)
                return;

            foreach (ListViewItem item in this.m_currentItems)
            {
                item.EnsureVisible();
            }
        }

        List<ListViewItem> m_currentItems = new List<ListViewItem>();

        // ���ڴ������µ���ȡ��ˢ��list
        void RefreshByNewlyCallNumberItems()
        {
            this.m_currentItems.Clear();

            if (this.MyselfCallNumberItems == null)
                return;
            if (this.MyselfCallNumberItems.Count == 0)
                return;


            string strError = "";

#if NO
            string strArrangeGroupName = "";
            string strZhongcihaoDbname = "";
            string strClassType = "";
            string strQufenhaoType = "";
            // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
            int nRet = this.MainForm.GetCallNumberInfo(this.LocationString,
                out strArrangeGroupName,
                out strZhongcihaoDbname,
                out strClassType,
                out strQufenhaoType,
                out strError);
#endif
            ArrangementInfo info = null;
            int nRet = this.MainForm.GetArrangementInfo(this.LocationString,
                out info,
                out strError);
            if (nRet == 0)
                return;
            if (nRet == -1)
                return;

            // ����һ��hash�����ڲ���ʵ���¼��·��
            Hashtable item_recpaths = new Hashtable();
            for (int i = 0; i < this.MyselfCallNumberItems.Count; i++)
            {
                CallNumberItem item = this.MyselfCallNumberItems[i];
                item_recpaths[item.RecPath] = 1;
            }

            // ɾ����ǰ��Ŀ��¼�µ�ȫ��ʵ���¼
            for (int i = 0; i < this.listView_number.Items.Count; i++)
            {
                ListViewItem list_item = this.listView_number.Items[i];

                string strBiblioRecPath = ListViewUtil.GetItemText(list_item, COLUMN_BIBLIORECPATH);
                if (string.IsNullOrEmpty(strBiblioRecPath) == false)
                {
                    if (strBiblioRecPath == this.MyselfParentRecPath)
                    {
                        this.listView_number.Items.RemoveAt(i);
                        i--;
                    }
                }
                else
                {
                    // 2012/3/22
                    // ���ּ�¼·��Ϊ�յ�ʱ�������취
                    string stItemRecPath = ListViewUtil.GetItemText(list_item, COLUMN_ITEMRECPATH);
                    Debug.Assert(string.IsNullOrEmpty(stItemRecPath) == false, "");
                    if (item_recpaths[stItemRecPath] != null)
                    {
                        this.listView_number.Items.RemoveAt(i);
                        i--;
                    }
                }
            }

            // location --> arrangement name
            Hashtable info_table = new Hashtable();

            // �����ڴ��е�ʵ���¼
            for (int i = 0; i < this.MyselfCallNumberItems.Count; i++)
            {
                CallNumberItem item = this.MyselfCallNumberItems[i];

                // ֻ�йݲصص���ϵĲ��ܽ���
                string strLocation = item.Location;

#if NO
                string strCurrentArrangeGroupName = "";

                // TODO: ��������hashtable����
                nRet = this.MainForm.GetCallNumberInfo(strLocation,
                    out strCurrentArrangeGroupName,
                    out strZhongcihaoDbname,
                    out strClassType,
                    out strQufenhaoType,
                    out strError);
                if (nRet == 0)
                    continue;

                if (strCurrentArrangeGroupName != strArrangeGroupName)
                    continue;
#endif
                // ����hashtable����
                // 2014/2/13
                string strCurrentArrangeGroupName = (string)info_table[strLocation];

                if (strCurrentArrangeGroupName == null)
                {
                    ArrangementInfo current_info = null;
                    nRet = this.MainForm.GetArrangementInfo(strLocation,
                        out current_info,
                        out strError);
                    if (nRet == 0)
                        continue;
                    strCurrentArrangeGroupName = current_info.ArrangeGroupName;
                    info_table[strLocation] = strCurrentArrangeGroupName;
                }

                if (strCurrentArrangeGroupName != info.ArrangeGroupName)
                    continue;

                string strCallNumber = StringUtil.BuildLocationClassEntry(item.CallNumber);

                // 2010/3/9
                // ֻ�з���Ų���һ�µĲ��ý���
                string strClass = GetClassPart(strCallNumber);
                if (strClass != this.ClassNumber)
                    continue;

                ListViewItem list_item = new ListViewItem();
                list_item.ImageIndex = TYPE_CURRENT;
                list_item.Text = item.RecPath;

                ListViewUtil.ChangeItemText(list_item, COLUMN_CALLNUMBER, strCallNumber);

                ListViewUtil.ChangeItemText(list_item, COLUMN_LOCATION, item.Location);
                ListViewUtil.ChangeItemText(list_item, COLUMN_BARCODE, item.Barcode);
                ListViewUtil.ChangeItemText(list_item, COLUMN_BIBLIORECPATH, this.MyselfParentRecPath);

                this.listView_number.Items.Add(list_item);

                // ���뵽���ڵ�ǰ�ֵ�ListViewItem�����У����������EnsureVisible
                this.m_currentItems.Add(list_item);
            }
        }

        // 
        /// <summary>
        /// ����ȡ���з��������Ų���
        /// </summary>
        /// <param name="strCallNumber">��ȡ��</param>
        /// <returns>����Ų���</returns>
        public static string GetClassPart(string strCallNumber)
        {
            string[] lines = strCallNumber.Split(new char[] { '/' });
            if (lines.Length < 1)
                return "";

            string strClass = lines[0].Trim();
            return strClass;
        }

        // ����ȡ���з�����ִκ�(�������ߺ�)����
        // ���ų�β���ĸ��Ӻ�
        /// <summary>
        /// ����ȡ���з����ͬ�������ֺ�(�ִκŻ����ߺ�)���֡�
        /// ����ǰ���ų�β���ĸ��Ӻ�
        /// </summary>
        /// <param name="strCallNumber">��ȡ���ַ���</param>
        /// <returns>ͬ�������ֺŲ���</returns>
        public static string GetZhongcihaoPart(string strCallNumber)
        {
            string [] lines = strCallNumber.Split(new char [] {'/'});
            if (lines.Length < 2)
                return "";

            string strZhongcihao = lines[1].Trim();
            /*
            int nRet = strZhongcihao.IndexOfAny(new char[] { '.', ',', '=', '-' });
            if (nRet != -1)
                strZhongcihao = strZhongcihao.Substring(0, nRet);

            return strZhongcihao;
             * */
            return GetLeftPureNumberPart(strZhongcihao);
        }

        // 2009/11/24 new add
        /// <summary>
        /// ���ͬ�������ֺ��г��˸��Ӻ�����Ĳ���
        /// </summary>
        /// <param name="strText">Ҫ������ַ���</param>
        /// <returns>���س��˸��Ӻ�����Ĳ���</returns>
        public static string GetLeftPureNumberPart(string strText)
        {
            if (String.IsNullOrEmpty(strText) == true)
                return "";

            strText = strText.TrimStart();

            StringBuilder s = new StringBuilder();
            for (int i = 0; i < strText.Length; i++)
            {
                char ch = strText[i];
                if (ch < '0' || ch > '9')
                    break;
                s.Append(ch);
            }

            return s.ToString();
        }

        /// <summary>
        /// �Ƚ�ͬ�������ֺŵĴ�С��
        /// �ܴ����ӺŲ���
        /// </summary>
        /// <param name="s1">����Ƚϵ�ͬ�������ֺ�֮һ</param>
        /// <param name="s2">����Ƚϵ�ͬ�������ֺ�֮��</param>
        /// <returns>�������0����ʾ����������ȣ��������0����ʾ֮һ����֮�������С��0����ʾ֮һС��֮��</returns>
        public static int CompareZhongcihao(string s1, string s2)
        {
            CanonicalZhongcihaoString(ref s1, ref s2);
            return String.Compare(s1, s2);
        }

        // 2008/9/19 new add
        // ���滯�����Ƚϵ��ַ���
        // ����'.'���и���ţ���������ι淶��Ϊ�˴˵ȳ�
        static void CanonicalZhongcihaoString(ref string s1, ref string s2)
        {
            string[] a1 = s1.Split(new char[] { '.', ',', '=', '-' });
            string[] a2 = s2.Split(new char[] { '.', ',', '=', '-' });

            string result1 = "";
            string result2 = "";
            int i = 0;
            for (; ; i++)
            {
                if (i >= a1.Length)
                    break;
                if (i >= a2.Length)
                    break;
                string c1 = a1[i];
                string c2 = a2[i];
                int nMaxLength = Math.Max(c1.Length, c2.Length);
                result1 += c1.PadLeft(nMaxLength, '0') + ".";
                result2 += c2.PadLeft(nMaxLength, '0') + ".";
            }

            for (int j = i + 1; j < a1.Length; j++)
            {
                result1 += a1[j] + ".";
            }

            for (int j = i + 1; j < a2.Length; j++)
            {
                result2 += a2[j] + ".";
            }

            s1 = result1;
            s2 = result2;
        }

        /*
        // ���Ѿ�����������У�ȡ��λ�����������ִκš�
        // ���������Զ��ų�MyselfItemRecPath������¼
        string GetTopNumber(ListView list)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];
                string strRecPath = item.Text;
                string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);

                if (strRecPath != this.MyselfItemRecPath
                    && strBiblioRecPath != this.MyselfParentRecPath)
                    return ListViewUtil.GetItemText(item, COLUMN_CALLNUMBER);
            }

            // TODO: ��������Լ����⣬��û������������Ч�ִκŵ������ˣ���Ҳֻ�����Լ����ִκ�-1���䵱��

            return "";  // û���ҵ�
        }*/

        // ���Ѿ�����������У�ȡ��λ�����������ִκš�
        // ���������Զ��ų�MyselfItemRecPath������¼
        string GetTopNumber(ListView list)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];
                string strRecPath = item.Text;
                string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);

                if (strRecPath != this.MyselfItemRecPath
                    && strBiblioRecPath != this.MyselfParentRecPath)
                    return ListViewUtil.GetItemText(item, COLUMN_CALLNUMBER);
            }

            // TODO: ��������Լ����⣬��û������������Ч�ִκŵ������ˣ���Ҳֻ�����Լ����ִκ�-1���䵱��

            return "";  // û���ҵ�
        }

        // ������ɺ�
        int GetMultiNumber(
            string strStyle,
            out string strOtherMaxNumber,
            out string strMyselfNumber,
            out string strSiblingNumber,
            out string strError)
        {
            strError = "";
            strOtherMaxNumber = "";
            strMyselfNumber = "";
            strSiblingNumber = "";

            int nRet = FillList(true,
                strStyle,
                out strError);
            if (nRet == -1)
                return -1;

            for (int i = 0; i < this.listView_number.Items.Count; i++)
            {
                ListViewItem item = this.listView_number.Items[i];
                string strRecPath = item.Text;
                string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);

                if (String.IsNullOrEmpty(strOtherMaxNumber) == true)
                {
                    if (strRecPath != this.MyselfItemRecPath
                        && strBiblioRecPath != this.MyselfParentRecPath)
                        strOtherMaxNumber = GetZhongcihaoPart(ListViewUtil.GetItemText(item, COLUMN_CALLNUMBER));
                }

                if (strRecPath == this.MyselfItemRecPath
                    && string.IsNullOrEmpty(strRecPath) == false)   // 2013/11/14
                    strMyselfNumber = GetZhongcihaoPart(ListViewUtil.GetItemText(item, COLUMN_CALLNUMBER));

                if (String.IsNullOrEmpty(strSiblingNumber) == true)
                {
                    if (strBiblioRecPath == this.MyselfParentRecPath)
                        strSiblingNumber = GetZhongcihaoPart(ListViewUtil.GetItemText(item, COLUMN_CALLNUMBER));
                }

                // �Ż�������ֵ�Ѿ���ã�û�б�Ҫ����ѭ����
                if (String.IsNullOrEmpty(strOtherMaxNumber) == false
                    && String.IsNullOrEmpty(strMyselfNumber) == false
                    && String.IsNullOrEmpty(strSiblingNumber) == false)
                    break;
            }

            return 0;
        }

        // ���������ֵ�ı仯����������ɫ
        // �㷨�ǽ�ԭ���ı�����ɫ�������ǳһ��
        static void SetGroupBackcolor(
            ListView list,
            int nSortColumn)
        {
            string strPrevText = "";
            bool bDark = false;
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];

                if (item.ImageIndex == TYPE_ERROR)
                    continue;

                string strThisText = "";
                try
                {
                    strThisText = item.SubItems[nSortColumn].Text;
                }
                catch
                {
                }

                if (strThisText != strPrevText)
                {
                    // �仯��ɫ
                    if (bDark == true)
                        bDark = false;
                    else
                        bDark = true;

                    strPrevText = strThisText;
                }

                if (bDark == true)
                {
                    item.BackColor = Global.Dark(GetItemBackColor(item.ImageIndex), 0.05F);
                }
                else
                {
                    item.BackColor = Global.Light(GetItemBackColor(item.ImageIndex), 0.05F);
                }
            }
        }

        static Color GetItemBackColor(int nType)
        {
            if (nType == TYPE_ERROR)
            {
                return Color.Red;
            }
            else if (nType == TYPE_NORMAL || nType == TYPE_CURRENT)
            {
                return SystemColors.Window;
            }
            else
            {
                throw new Exception("δ֪��image type");
            }
        }

        // ����ʵ���β��
        private void button_searchDouble_Click(object sender, EventArgs e)
        {
            string strError = "";

            EventFinish.Reset();
            try
            {
                this.textBox_tailNumber.Text = "";   // Ԥ��filllist ��ǰ�˳�, ���Ǵ���

                int nRet = FillList(true,
                    "",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // һ����ñ���β��
                nRet = PanelGetTailNumber(out strError);
                if (nRet == -1)
                    goto ERROR1;

                return;
            }
            finally
            {
                EventFinish.Set();
            }

        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void listView_number_DoubleClick(object sender, EventArgs e)
        {
            if (this.listView_number.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫ����������");
                return;
            }

            string strItemRecPath = ListViewUtil.GetItemText(this.listView_number.SelectedItems[0], 0);

            if (String.IsNullOrEmpty(strItemRecPath) == true)
            {
                MessageBox.Show(this, "ʵ���¼·��Ϊ��");
                return;
            }

            string strOpenStyle = "new";
            /*
                if (this.LoadToExistDetailWindow == true)
                    strOpenStyle = "exist";
             * */

            // װ���ֲᴰ/ʵ�崰���ò������/��¼·��
            // parameters:
            //      strTargetFormType   Ŀ�괰������ "EntityForm" "ItemInfoForm"
            //      strIdType   ��ʶ���� "barcode" "recpath"
            //      strOpenType �򿪴��ڵķ�ʽ "new" "exist"
            LoadRecord("EntityForm",
                "recpath",
                strOpenStyle);
        }


        // װ���ֲᴰ/ʵ�崰���ò������/��¼·��
        // parameters:
        //      strTargetFormType   Ŀ�괰������ "EntityForm" "ItemInfoForm"
        //      strIdType   ��ʶ���� "barcode" "recpath"
        //      strOpenType �򿪴��ڵķ�ʽ "new" "exist"
        void LoadRecord(string strTargetFormType,
            string strIdType,
            string strOpenType)
        {
            string strTargetFormName = "�ֲᴰ";
            if (strTargetFormType == "ItemInfoForm")
                strTargetFormName = "ʵ�崰";

            if (this.listView_number.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫװ��" + strTargetFormName + "����");
                return;
            }

            string strBarcodeOrRecPath = "";

            if (strIdType == "barcode")
            {
                // barcode
                strBarcodeOrRecPath = ListViewUtil.GetItemText(this.listView_number.SelectedItems[0], 1);
            }
            else
            {
                Debug.Assert(strIdType == "recpath", "");
                // recpath
                strBarcodeOrRecPath = ListViewUtil.GetItemText(this.listView_number.SelectedItems[0], 0);
            }

            if (strTargetFormType == "EntityForm")
            {
                EntityForm form = null;

                if (strOpenType == "exist")
                {
                    form = MainForm.GetTopChildWindow<EntityForm>();
                    if (form != null)
                        Global.Activate(form);
                }
                else
                {
                    Debug.Assert(strOpenType == "new", "");
                }

                if (form == null)
                {
                    form = new EntityForm();

                    form.MdiParent = this.MainForm;

                    form.MainForm = this.MainForm;
                    form.Show();
                }

                if (strIdType == "barcode")
                {
                    // װ��һ���ᣬ����װ����
                    // parameters:
                    //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   found
                    form.LoadItemByBarcode(strBarcodeOrRecPath, false);
                }
                else
                {
                    Debug.Assert(strIdType == "recpath", "");

                    // parameters:
                    //      bAutoSavePrev   �Ƿ��Զ��ύ������ǰ���������޸ģ����==true���ǣ����==false����Ҫ����MessageBox��ʾ
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   found
                    form.LoadItemByRecPath(strBarcodeOrRecPath, false);
                }
            }
            else
            {
                Debug.Assert(strTargetFormType == "ItemInfoForm", "");

                ItemInfoForm form = null;

                if (strOpenType == "exist")
                {
                    form = MainForm.GetTopChildWindow<ItemInfoForm>();
                    if (form != null)
                        Global.Activate(form);
                }
                else
                {
                    Debug.Assert(strOpenType == "new", "");
                }

                if (form == null)
                {
                    form = new ItemInfoForm();

                    form.MdiParent = this.MainForm;

                    form.MainForm = this.MainForm;
                    form.Show();
                }

                if (strIdType == "barcode")
                {
                    form.LoadRecord(strBarcodeOrRecPath);
                }
                else
                {
                    Debug.Assert(strIdType == "recpath", "");

                    form.LoadRecordByRecPath(strBarcodeOrRecPath, "");
                }
            }
        }

        int m_nInDropDown = 0;

        private void comboBox_location_DropDown(object sender, EventArgs e)
        {
            // ��ֹ����
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
                    // e1.DbName = this.BiblioDbName;

                    if (combobox == this.comboBox_location)
                        e1.TableName = "location";
                    else
                    {
                        Debug.Assert(false, "��֧�ֵ�sender");
                        return;
                    }

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

        // ����β�ţ���������н���Ԫ��
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int PanelGetTailNumber(out string strError)
        {
            strError = "";
            this.textBox_tailNumber.Text = "";

            string strTailNumber = "";
            int nRet = SearchTailNumber(out strTailNumber,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;

            this.textBox_tailNumber.Text = strTailNumber;
            return 1;
        }

        /// <summary>
        ///  ��������ִκſ��ж�Ӧ��Ŀ��β�š��˹��ܱȽϵ���������õĽ����������������Ԫ��
        /// </summary>
        /// <param name="strTailNumber">����β��</param>
        /// <param name="strError">���ش�����Ϣ</param>
        /// <returns>-1����;0û���ҵ�;1�ҵ�</returns>
        public int SearchTailNumber(
            out string strTailNumber,
            out string strError)
        {
            strTailNumber = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ��β�� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.GetOneClassTailNumber(
                    stop,
                    GetArrangeGroupName(this.LocationString),
                    this.ClassNumber,
                    out strTailNumber,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            // return 0;
        ERROR1:
            return -1;
        }

        // ��ȡβ��
        private void button_getTailNumber_Click(object sender, EventArgs e)
        {
            string strError = "";

            EventFinish.Reset();
            try
            {
                this.textBox_tailNumber.Text = "";   // Ԥ����գ��Է����

                // ��ñ���β��
                int nRet = PanelGetTailNumber(out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    strError = "�� '" + this.ClassNumber + "' ��β���в�����";
                    goto ERROR1;
                }

                return;
            }
            finally
            {
                EventFinish.Set();
            }

        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ����β��
        private void button_saveTailNumber_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.textBox_tailNumber.Text == "")
            {
                strError = "��δ����Ҫ�����β��";
                goto ERROR1;
            }

            EventFinish.Reset();
            try
            {
                string strOutputNumber = "";

                // ���汾��β��
                int nRet = SaveTailNumber(this.textBox_tailNumber.Text,
                    out strOutputNumber,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                return;
            }
            finally
            {
                EventFinish.Set();
            }

        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// ���浱ǰ���E���е�β��
        /// </summary>
        /// <param name="strTailNumber">Ҫ�����β��</param>
        /// <param name="strOutputNumber">����ʵ�ʱ����β��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int SaveTailNumber(
            string strTailNumber,
            out string strOutputNumber,
            out string strError)
        {
            strOutputNumber = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ���β�� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.SetOneClassTailNumber(
                    stop,
                    "save",
                    GetArrangeGroupName(this.LocationString),
                    // "!" + this.BiblioDbName,
                    this.ClassNumber,
                    strTailNumber,
                    out strOutputNumber,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            // return 0;
        ERROR1:
            return -1;
        }

        // �ü����õ���ͬ������ʵ���õ������ţ���̽���ƶ��ִκſ��е�β��
        private void button_pushTailNumber_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strOutputNumber = "";
            // �ƶ�β��
            int nRet = PushTailNumber(this.textBox_maxNumber.Text,
                out strOutputNumber,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.textBox_tailNumber.Text = strOutputNumber;
            // MessageBox.Show(this, "�ƶ�β�ųɹ�");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �ƶ�β�š�����Ѿ����ڵ�β�ű�strTestNumber��Ҫ�����ƶ�
        /// <summary>
        /// �ƶ���ǰ�����е�β�š�
        /// ����Ѿ����ڵ�β�ű� strTestNumber ��Ҫ�����ƶ�
        /// </summary>
        /// <param name="strTestNumber">ϣ���ƶ�����β��</param>
        /// <param name="strOutputNumber">����ʵ�ʱ��ƶ����β��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int PushTailNumber(string strTestNumber,
            out string strOutputNumber,
            out string strError)
        {
            strOutputNumber = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����ƶ�β�� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.SetOneClassTailNumber(
                    stop,
                    "conditionalpush",
                    GetArrangeGroupName(this.LocationString),
                    // "!" + this.BiblioDbName,
                    this.ClassNumber,
                    strTestNumber,
                    out strOutputNumber,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            // return 0;
        ERROR1:
            return -1;
        }

        // ����β��
        /// <summary>
        /// ������ǰ�����е�β��
        /// </summary>
        /// <param name="strDefaultNumber">ȱʡβ�š������ǰβ�ſ�����δ������ǰ���β�ţ����ձ�����������</param>
        /// <param name="strOutputNumber">�����������β��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int IncreaseTailNumber(string strDefaultNumber,
            out string strOutputNumber,
            out string strError)
        {
            strOutputNumber = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("��������β�� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.SetOneClassTailNumber(
                    stop,
                    "increase",
                    GetArrangeGroupName(this.LocationString),
                    // "!" + this.BiblioDbName,
                    this.ClassNumber,
                    strDefaultNumber,
                    out strOutputNumber,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            // return 0;
        ERROR1:
            return -1;
        }

        // ���Ʊȵ�ǰ��Ŀ��ͳ�Ƴ��������Ż���1�ĺ�
        private void button_copyMaxNumber_Click(object sender, EventArgs e)
        {
            string strResult = "";
            string strError = "";

            // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
            // return:
            //      -1  error
            //      1   succeed
            int nRet = GetMaxNumberPlusOne(out strResult,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }

            if (nRet == 0)
                strResult = "1";    // �����ǰ����Ŀ���޷�ͳ�Ƴ����ţ�����Ϊ�õ�"0"������1�Ժ�����Ϊ"1"

            Clipboard.SetDataObject(strResult);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
        // return:
        //      -1  error
        //      0   not found
        //      1   succeed
        /// <summary>
        /// �õ� ���ݵ�ǰ��������Ŀ��Ϣͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
        /// </summary>
        /// <param name="strResult">���ؽ��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1: ����</para>
        /// <para>0: û���ҵ�</para>
        /// <para>1: �ɹ�</para>
        /// </returns>
        public int GetMaxNumberPlusOne(out string strResult,
            out string strError)
        {
            strResult = "";
            strError = "";
            string strMaxNumber = "";

            try
            {
                strMaxNumber = this.MaxNumber;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                goto ERROR1;
            }

            if (String.IsNullOrEmpty(strMaxNumber) == true)
                return 0;

            int nRet = StringUtil.IncreaseLeadNumber(strMaxNumber,
                1,
                out strResult,
                out strError);
            if (nRet == -1)
            {
                strError = "Ϊ���� '" + strMaxNumber + "' ����ʱ��������: " + strError;
                goto ERROR1;

            }
            return 1;
        ERROR1:
            return -1;
        }

        int SetDisplayState(out string strError)
        {
            strError = "";
            int nRet = 0;

#if NO
            string strArrangeGroupName = "";
            string strZhongcihaoDbname = "";
            string strClassType = "";
            string strQufenhaoType = "";

            // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
            // return:
            //      -1  error
            //      0   notd found
            //      1   found
            nRet = this.MainForm.GetCallNumberInfo(this.LocationString,
                out strArrangeGroupName,
                out strZhongcihaoDbname,
                out strClassType,
                out strQufenhaoType,
                out strError);
#endif
            ArrangementInfo info = null;
            nRet = this.MainForm.GetArrangementInfo(this.LocationString,
                out info,
                out strError);
            if (nRet == 0)
            {
                this.MaxNumberVisible = false;

                this.groupBox_tailNumber.Visible = false;
                this.button_searchDouble.Visible = false;
                this.button_pushTailNumber.Visible = false;

                return 0;
            }

            if (nRet == -1)
                return -1;


            if (info.QufenhaoType.ToLower() == "zhongcihao"
                || info.QufenhaoType == "�ִκ�")
            {
                this.MaxNumberVisible = true;
            }
            else
            {
                this.MaxNumberVisible = false;

                info.ZhongcihaoDbname = "";   // ��������ִκ����ͣ�Ҳ�Ͳ�����ִκſ�����
            }

            if (String.IsNullOrEmpty(info.ZhongcihaoDbname) == true)
            {
                this.groupBox_tailNumber.Visible = false;
                this.button_searchDouble.Visible = false;
                this.button_pushTailNumber.Visible = false;
            }
            else
            {
                this.groupBox_tailNumber.Visible = true;
                this.button_searchDouble.Visible = true;
                this.button_pushTailNumber.Visible = true;
            }

            return 0;
        }

        bool MaxNumberVisible
        {
            get
            {
                return this.textBox_maxNumber.Visible;
            }
            set
            {
                this.textBox_maxNumber.Visible = value;
                this.button_copyMaxNumber.Visible = value;
                this.label_maxNumber.Visible = value;
            }
        }

        /*
        // ���MyselfItemRecPath������¼��CallNumber�����ֺŲ��֡���������ڣ�����ͬһ��Ŀ��¼�����ĵ�һ��CallNumber���ֺŲ���
        string GetMyselfOrSiblingQufenNumber(ListView list)
        {
            string strSiblingNumber = "";

            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];
                string strRecPath = item.Text;
                string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);

                if (strRecPath == this.MyselfItemRecPath)
                {
                    string strNumber = ListViewUtil.GetItemText(item, COLUMN_CALLNUMBER);

                    strNumber = GetZhongcihaoPart(strNumber);

                    if (String.IsNullOrEmpty(strNumber) == false)
                        return strNumber;
                    continue;
                }


                if (strBiblioRecPath == this.MyselfParentRecPath
                    && String.IsNullOrEmpty(strSiblingNumber) == true)
                {
                    strSiblingNumber = ListViewUtil.GetItemText(item, COLUMN_CALLNUMBER);

                    strSiblingNumber = GetZhongcihaoPart(strSiblingNumber);
                }
            }

            if (String.IsNullOrEmpty(strSiblingNumber) == false)
                return strSiblingNumber;

            return null;  // û���ҵ�
        }
         * */

        // (�ⲿ���ýӿ�)
        // ����һ���Ĳ��ԣ�����ִκ�
        // TODO: ���Է���һ������ʾ��Ϣ�������Ƿ�������ã����Ǵ�������¼�������������
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        /// <summary>
        /// ����һ���Ĳ��ԣ�����ִκ�
        /// </summary>
        /// <param name="style">�ִκ�ȡ�ŵķ��</param>
        /// <param name="strClass">���</param>
        /// <param name="strLocationString">�ݲصص�</param>
        /// <param name="strNumber">�����ִκ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public int GetZhongcihao(
            ZhongcihaoStyle style,
            string strClass,
            string strLocationString,
            out string strNumber,
            out string strError)
        {
            strNumber = "";
            strError = "";
            int nRet = 0;

            this.ClassNumber = strClass;
            this.LocationString = strLocationString;

            // ��������Ŀͳ������
            if (style == ZhongcihaoStyle.Biblio)
            {
                string strOtherMaxNumber = "";
                string strMyselfNumber = "";
                string strSiblingNumber = "";

                // ������ɺ�
                nRet = GetMultiNumber(
                    "fast",
                    out strOtherMaxNumber,
                    out strMyselfNumber,
                    out strSiblingNumber,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (String.IsNullOrEmpty(strMyselfNumber) == false)
                {
                    strNumber = strMyselfNumber;
                    return 1;
                }

                if (String.IsNullOrEmpty(strSiblingNumber) == false)
                {
                    strNumber = strSiblingNumber;
                    return 1;
                }

                if (String.IsNullOrEmpty(strOtherMaxNumber) == false)
                {
                    nRet = StringUtil.IncreaseLeadNumber(strOtherMaxNumber,
                        1,
                        out strNumber,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "Ϊ���� '" + strOtherMaxNumber + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    return 1;
                }

                // 2009/2/25 new add
                Debug.Assert(nRet == 0, "");

                string strDefaultValue = "";    // "1"

            REDO_INPUT:
                // �������û�й���¼����ǰ�ǵ�һ��
                strNumber = InputDlg.GetInput(
                    this,
                    null,
                    "�������� '" + strClass + "' �ĵ�ǰ�ִκ�����:",
                    strDefaultValue,
            this.MainForm.DefaultFont);
                if (strNumber == null)
                    return 0;	// ������������
                if (String.IsNullOrEmpty(strNumber) == true)
                    goto REDO_INPUT;

                return 1;
            }

            /*
            // ��������Ŀͳ������
            if (style == ZhongcihaoStyle.Biblio)
            {
                // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
                // return:
                //      -1  error
                //      1   succeed
                nRet = GetMaxNumberPlusOne(out strNumber,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nRet == 1)
                    return 1;

                // 2009/2/25 new add
                Debug.Assert(nRet == 0, "");

                // �������û�й���¼����ǰ�ǵ�һ��
                strNumber = InputDlg.GetInput(
                    this,
                    null,
                    "�������� '" + strClass + "' �ĵ�ǰ�ִκ�����:",
                    "1");
                if (strNumber == null)
                    return 0;	// ������������

                return 1;
            }
            */


            // ÿ�ζ�������Ŀͳ�����������顢У��β��
            if (style == ZhongcihaoStyle.BiblioAndSeed
                || style == ZhongcihaoStyle.SeedAndBiblio)
            {
                // TODO: �����ǰ��¼���ڴ��д��ڣ���Ӧ�����������������Ա�����ν������
                if (style == ZhongcihaoStyle.BiblioAndSeed)
                {
                    /*
                    // TODO: ��α����ظ�filllist
                    nRet = FillList(true, out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    strNumber = GetMyselfOrSiblingQufenNumber(this.listView_number);
                    if (String.IsNullOrEmpty(strNumber) == false)
                    {
                        return 1;
                    }
                     * */
                    string strOtherMaxNumber = "";
                    string strMyselfNumber = "";
                    string strSiblingNumber = "";

                    // ������ɺ�
                    nRet = GetMultiNumber(
                        "fast",
                        out strOtherMaxNumber,
                        out strMyselfNumber,
                        out strSiblingNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (String.IsNullOrEmpty(strMyselfNumber) == false)
                    {
                        strNumber = strMyselfNumber;
                        return 1;
                    }

                    if (String.IsNullOrEmpty(strSiblingNumber) == false)
                    {
                        strNumber = strSiblingNumber;
                        return 1;
                    }
                }

                string strTailNumber = this.TailNumber;

                // ���������δ�����ִκ���Ŀ
                if (String.IsNullOrEmpty(strTailNumber) == true)
                {
                    // �Ͼ���ʼֵ����������ͳ�ƽ��
                    string strTestNumber = "";
                    // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   succeed
                    nRet = GetMaxNumberPlusOne(out strTestNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 0)
                        strTestNumber = ""; // "1"

                REDO_INPUT:
                    // �������û�й���¼����ǰ�ǵ�һ��
                    strNumber = InputDlg.GetInput(
                        this,
                        null,
                        "�������� '" + strClass + "' �ĵ�ǰ�ִκ�����:",
                        strTestNumber,
            this.MainForm.DefaultFont);
                    if (strNumber == null)
                        return 0;	// ������������
                    if (String.IsNullOrEmpty(strNumber) == true)
                        goto REDO_INPUT;

                    // dlg.TailNumber = strNumber;	

                    nRet = PushTailNumber(strNumber,
                        out strNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    return 1;
                }
                else // �����Ѿ����ִκ���Ŀ
                {
                    // ����ͳ��ֵ�Ĺ�ϵ
                    string strTestNumber = "";
                    // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   succeed
                    nRet = GetMaxNumberPlusOne(out strTestNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 0)
                    {
                        // ��������β����������
                        nRet = this.IncreaseTailNumber("1",
                            out strNumber,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        return 1;
                    }

                    // ��ͳ�Ƴ����ĺ��ƶ���ǰβ�ţ������˼��������
                    nRet = PushTailNumber(strTestNumber,
                        out strNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // ���������ͷ��أ�Ч��Ϊ�������������������ǰ��¼����ȡ�Ŷ������棬��β�Ų�äĿ��������Ȼȱ��Ҳ�Ǻ����Ե� -- �п��ܶ������ȡ���غ���
                    if (style == ZhongcihaoStyle.BiblioAndSeed)
                        return 1;

                    if (strTailNumber != strNumber)  // ���ʵ�ʷ������ƶ�����Ҫ����ţ�����������
                        return 1;

                    // ��������β������
                    nRet = this.IncreaseTailNumber("1",
                        out strNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    return 1;
                }

                // return 1;
            }

            // ������(�ִκſ�)β��
            if (style == ZhongcihaoStyle.Seed)
            {
                string strTailNumber = "";

                try
                {
                    strTailNumber = this.TailNumber;
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    goto ERROR1;
                }

                // ���������δ�����ִκ���Ŀ
                if (String.IsNullOrEmpty(strTailNumber) == true)
                {
                    // �Ͼ���ʼֵ����������ͳ�ƽ��
                    string strTestNumber = "";
                    // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   succeed
                    nRet = GetMaxNumberPlusOne(out strTestNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 0)
                        strTestNumber = ""; // "1"

                REDO_INPUT:
                    // �������û�й���¼����ǰ�ǵ�һ��
                    strNumber = InputDlg.GetInput(
                        this,
                        null,
                        "�������� '" + strClass + "' �ĵ�ǰ�ִκ�����:",
                        strTestNumber,
            this.MainForm.DefaultFont);
                    if (strNumber == null)
                        return 0;	// ������������
                    if (String.IsNullOrEmpty(strNumber) == true)
                        goto REDO_INPUT;

                    // dlg.TailNumber = strNumber;	

                    nRet = PushTailNumber(strNumber,
                        out strNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    return 1;
                }
                else // �����Ѿ����ִκ���Ŀ����������
                {
                    nRet = this.IncreaseTailNumber("1",
                        out strNumber,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }

                return 1;
            }
            return 1;
        ERROR1:
            return -1;
        }

        int GetAllBiblioSummary(out string strError)
        {
            strError = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.SetMessage("���ڻ�ȡ��ĿժҪ ...");
            stop.BeginLoop();

            try
            {
                string strPrevBiblioRecPath = "";
                string strPrevSummary = "";
                for (int i = 0; i < this.listView_number.Items.Count; i++)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                            return 0;
                    }

                    ListViewItem item = this.listView_number.Items[i];
                    string strSummary = "";
                    string strBiblioRecPath = ListViewUtil.GetItemText(item, COLUMN_BIBLIORECPATH);

                    if (strPrevBiblioRecPath == strBiblioRecPath)
                    {
                        strSummary = strPrevSummary;
                        goto SETTEXT;
                    }

                    string strOutputBiblioRecPath = "";

                    long lRet = Channel.GetBiblioSummary(
                        stop,
                        "@bibliorecpath:" + strBiblioRecPath,
                        "", // strItemRecPath,
                        null,
                        out strOutputBiblioRecPath,
                        out strSummary,
                        out strError);
                    if (lRet == -1)
                    {
                        strSummary = strError;
                    }

                SETTEXT:
                    ListViewUtil.ChangeItemText(item, COLUMN_SUMMARY, strSummary);

                    strPrevBiblioRecPath = strBiblioRecPath;
                    strPrevSummary = strSummary;
                }

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 0;
        }

        private void checkBox_topmost_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_topmost.Checked == true)
            {
                Debug.Assert(this.MainForm != null || this.MdiParent != null, "");
                if (this.MdiParent != null)
                    this.MainForm = (MainForm)this.MdiParent;
                this.MdiParent = null;
                Debug.Assert(this.MainForm != null, "");
                this.Owner = this.MainForm;
                // this.TopMost = true;
            }
            else
            {
                Debug.Assert(this.MainForm != null, "");
                this.MdiParent = this.MainForm;
                // this.TopMost = false;
            }
        }

        /// <summary>
        /// �����Ƿ�Ϊ����״̬
        /// </summary>
        public override bool Floating
        {
            get
            {
                return this.checkBox_topmost.Checked;
            }
            set
            {
                this.checkBox_topmost.Checked = value;
            }
        }

        private void CallNumberForm_Activated(object sender, EventArgs e)
        {
            // 2009/8/13 new add
            // this.MainForm.stopManager.Active(this.stop);
        }

        private void comboBox_location_TextChanged(object sender, EventArgs e)
        {
            // ����������ʾ��Ϣ
            string strError = "";
            int nRet = SetDisplayState(out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
        }

        private void listView_number_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewUtil.OnColumnClick(this.listView_number, e);
        }


#if NOOOOOOOOOOOOOOOOOOOOOOOOO

        // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
        int GetCallNumberInfo(string strLocation,
            out string strArrangeGroupName,
            out string strZhongcihaoDbname,
            out string strClassType,
            out string strQufenhaoType,
            out string strError)
        {
            strError = "";
            strArrangeGroupName = "";
            strZhongcihaoDbname = "";
            strClassType = "";
            strQufenhaoType = "";

            if (this.cfg_dom == null)
            {
                // ��ʼ����ȡ��������Ϣ
                int nRet = InitialCallNumberCfgInfo(out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                    return 0;
                Debug.Assert(this.cfg_dom != null, "");
            }

            if (this.cfg_dom.DocumentElement == null)
                return 0;

            XmlNode node = this.cfg_dom.DocumentElement.SelectSingleNode("group/location[@name='"+strLocation+"']");
            if (node == null)
                return 0;

            XmlNode nodeGroup = node.ParentNode;
            strArrangeGroupName = DomUtil.GetAttr(nodeGroup, "name");
            strZhongcihaoDbname = DomUtil.GetAttr(nodeGroup, "zhongcihaodb");
            strClassType = DomUtil.GetAttr(nodeGroup, "classType");
            strQufenhaoType = DomUtil.GetAttr(nodeGroup, "qufenhaoType");

            return 1;
        }

        // ��ʼ����ȡ��������Ϣ
        // return:
        //      -1  error
        //      0   not initial
        //      1   initialized
        int InitialCallNumberCfgInfo(out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(this.MainForm.CallNumberInfo) == true)
                return 0;

            this.cfg_dom = new XmlDocument();
            this.cfg_dom.LoadXml("<callNumber/>");

            try
            {
                this.cfg_dom.DocumentElement.InnerXml = this.MainForm.CallNumberInfo;
            }
            catch (Exception ex)
            {
                strError = "Set InnerXml error: " + ex.Message;
                return -1;
            }

            return 1;
        }

#endif
    }

    // 
    /// <summary>
    /// ��ǰ�ڴ��е���ȡ������
    /// </summary>
    public class CallNumberItem
    {
        /// <summary>
        /// ���¼��·��
        /// </summary>
        public string RecPath = ""; // ���¼��·��
        /// <summary>
        /// ��ȡ��
        /// </summary>
        public string CallNumber = "";  // ��ȡ��

        /// <summary>
        /// �ݲصص�
        /// </summary>
        public string Location = "";
        /// <summary>
        /// �������
        /// </summary>
        public string Barcode = "";
    }

    // ����
    // Implements the manual sorting of items by columns.
    class CallNumberListViewItemComparer : IComparer
    {
        public CallNumberListViewItemComparer()
        {
        }

        public int Compare(object x, object y)
        {
            string s1 = ((ListViewItem)x).SubItems[CallNumberForm.COLUMN_CALLNUMBER].Text;
            string s2 = ((ListViewItem)y).SubItems[CallNumberForm.COLUMN_CALLNUMBER].Text;

            // ȡ�����ֺ���
            s1 = CallNumberForm.GetZhongcihaoPart(s1);
            s2 = CallNumberForm.GetZhongcihaoPart(s2);

            /*
            CanonicalString(ref s1, ref s2);
            return -1 * String.Compare(s1, s2);
             * */
            // 2009/11/4 changed
            return -1 * CallNumberForm.CompareZhongcihao(s1, s2);
        }

    }
}