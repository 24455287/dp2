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
using System.IO;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.GUI;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// �ִκŴ�
    /// </summary>
    public partial class ZhongcihaoForm : MyForm
    {
        /// <summary>
        /// ����������ļ�¼·���ļ�ȫ·��
        /// </summary>
        public string ExportRecPathFilename = "";   // ʹ�ù��ĵ���·���ļ�

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

        const int TYPE_NORMAL = 0;
        const int TYPE_ERROR = 1;
        const int TYPE_CURRENT = 2;

        /// <summary>
        /// ���캯��
        /// </summary>
        public ZhongcihaoForm()
        {
            InitializeComponent();

            ListViewProperty prop = new ListViewProperty();
            this.listView_number.Tag = prop;
            // ��һ�����⣬��¼·��
            prop.SetSortStyle(0, ColumnSortStyle.RecPath);
            prop.GetColumnTitles -= new GetColumnTitlesEventHandler(prop_GetColumnTitles);
            prop.GetColumnTitles += new GetColumnTitlesEventHandler(prop_GetColumnTitles);
        }

        void prop_GetColumnTitles(object sender, GetColumnTitlesEventArgs e)
        {
            e.ColumnTitles = new ColumnPropertyCollection();
            ColumnPropertyCollection temp = this.MainForm.GetBrowseColumnProperties(e.DbName);
            if (temp != null)
                e.ColumnTitles.AddRange(temp);  // Ҫ���ƣ���Ҫֱ��ʹ�ã���Ϊ������ܻ��޸ġ���Ӱ�쵽ԭ��

            /*
            if (this.m_bFirstColumnIsKey == true)
                e.ColumnTitles.Insert(0, "���еļ�����");
             * */
        }

        private void ZhongcihaoForm_Load(object sender, EventArgs e)
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

            // ���
            if (String.IsNullOrEmpty(this.textBox_classNumber.Text) == true)
            {
                this.textBox_classNumber.Text = this.MainForm.AppInfo.GetString(
                    "zhongcihao_form",
                    "classnumber",
                    "");
            }

            // ������Ŀ����
            if (String.IsNullOrEmpty(this.comboBox_biblioDbName.Text) == true)
            {
                this.comboBox_biblioDbName.Text = this.MainForm.AppInfo.GetString(
                    "zhongcihao_form",
                    "biblio_dbname",
                    "");
            }

            // �Ƿ�Ҫ���������
            this.checkBox_returnBrowseCols.Checked = this.MainForm.AppInfo.GetBoolean(
                    "zhongcihao_form",
                    "return_browse_cols",
                    true);

            string strWidths = this.MainForm.AppInfo.GetString(
"zhongcihao_form",
"record_list_column_width",
"");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_number,
                    strWidths,
                    true);
            }


            if (this.AutoBeginSearch == true)
            {
                API.PostMessage(this.Handle, WM_INITIAL, 0, 0);
            }

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
                        this.button_searchDouble_Click(null, null);
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }


        private void ZhongcihaoForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void ZhongcihaoForm_FormClosed(object sender, FormClosedEventArgs e)
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
                "zhongcihao_form",
                "classnumber",
                this.textBox_classNumber.Text);

            // ������Ŀ����
            this.MainForm.AppInfo.SetString(
                "zhongcihao_form",
                "biblio_dbname",
                this.comboBox_biblioDbName.Text);

            // �Ƿ�Ҫ���������
            this.MainForm.AppInfo.SetBoolean(
        "zhongcihao_form",
        "return_browse_cols",
        this.checkBox_returnBrowseCols.Checked);

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_number);
            this.MainForm.AppInfo.SetString(
                "zhongcihao_form",
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

        /// <summary>
        /// ��Ŀ����
        /// </summary>
        public string BiblioDbName
        {
            get
            {
                return this.comboBox_biblioDbName.Text;
            }
            set
            {
                this.comboBox_biblioDbName.Text = value;
            }
        }

        string _biblioRecPath = "";

        // ����ȡ�ŵ���Ŀ��¼��·��������У��ͳ�ƹ��̣��ų��Լ���
        /// <summary>
        /// ����ȡ�ŵ���Ŀ��¼��·��
        /// </summary>
        public string MyselfBiblioRecPath
        {
            get
            {
                return this._biblioRecPath;
            }
            set
            {
                this._biblioRecPath = value;

                // 2014/4/9
                string strBiblioDbName = Global.GetDbName(value);
                if (string.IsNullOrEmpty(strBiblioDbName) == false)
                    this.BiblioDbName = strBiblioDbName;
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

                    int nRet = FillList(true, out strError);
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
 

        // ����
        private void button_search_Click(object sender, EventArgs e)
        {
            string strError = "";

            EventFinish.Reset();
            try
            {
                int nRet = FillList(true, out strError);
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
            this.comboBox_biblioDbName.Enabled = bEnable;
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

        int FillList(bool bSort,
            out string strError)
        {
            strError = "";
            // int nRet = 0;

            this.listView_number.Items.Clear();
            this.listView_number.ListViewItemSorter = null;
            this.MaxNumber = "";

            /*
            if (dom == null)
            {
                strError = "���ȵ���GetGlobalCfgFile()����";
                return -1;
            }
             * */

            if (this.ClassNumber == "")
            {
                strError = "��δָ�������";
                return -1;
            }

            if (this.BiblioDbName == "")
            {
                strError = "��δָ����Ŀ����";
                return -1;
            }

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ���ͬ�����¼ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                string strQueryXml = "";

                long lRet = Channel.SearchUsedZhongcihao(
                    stop,
                    GetZhongcihaoDbGroupName(this.BiblioDbName),
                    // "!" + this.BiblioDbName,
                    this.ClassNumber,
                    "zhongcihao",
                    out strQueryXml,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                if (lRet == 0)
                {
                    strError = "û�����еļ�¼��";
                    return 0;   // not found
                }


                long lHitCount = lRet;

                long lStart = 0;
                long lPerCount = Math.Min(50, lHitCount);

                ZhongcihaoSearchResult[] searchresults = null;

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
                    if (bShift == true || this.checkBox_returnBrowseCols.Checked == false)
                    {
                        strBrowseStyle = "";
                        lCurrentPerCount = lPerCount * 10;
                    }

                    stop.SetMessage("����װ�������Ϣ " + (lStart + 1).ToString() + " - " + (lStart + lPerCount).ToString() + " (���� " + lHitCount.ToString() + " ����¼) ...");

                    lRet = Channel.GetZhongcihaoSearchResult(
                        stop,
                        GetZhongcihaoDbGroupName(this.BiblioDbName),
                        // "!" + this.BiblioDbName,
                        "zhongcihao",   // strResultSetName
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
                        ZhongcihaoSearchResult result_item = searchresults[i];
                        ListViewItem item = new ListViewItem();
                        item.Text = result_item.Path;

                        item.SubItems.Add(result_item.Zhongcihao);

#if NO
                        if (CheckNumber(result_item.Zhongcihao) == true)
                            item.ImageIndex = TYPE_NORMAL;
                        else
                            item.ImageIndex = TYPE_ERROR;
#endif
                        item.ImageIndex = TYPE_NORMAL;

                        if (result_item.Cols != null)
                        {
                            ListViewUtil.EnsureColumns(this.listView_number, result_item.Cols.Length + 1);
                            for (int j = 0; j < result_item.Cols.Length; j++)
                            {
                                ListViewUtil.ChangeItemText(item, j + 2, result_item.Cols[j]);
                            }
                        }

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

            if (bSort == true)
            {
                // ����
                this.listView_number.ListViewItemSorter = new ZhongcihaoListViewItemComparer();
                this.listView_number.ListViewItemSorter = null;

                // ���ظ��ִκŵ�������������ɫ�����
                ColorDup();

                EnsureStartRecordVisible();

                this.MaxNumber = GetTopNumber(this.listView_number);    // this.listView_number.Items[0].SubItems[1].Text;
            }

            return 0;
        ERROR1:
            return -1;
        }

        // �÷����¼�������ɼ���Χ
        void EnsureStartRecordVisible()
        {
            if (string.IsNullOrEmpty(this.MyselfBiblioRecPath) == true)
                return;
            ListViewItem item = ListViewUtil.FindItem(this.listView_number, this.MyselfBiblioRecPath, 0);
            if (item != null)
            {
                item.ImageIndex = TYPE_CURRENT;
                item.Font = new Font(item.Font, FontStyle.Bold);
                item.BackColor = Color.Yellow;
                item.EnsureVisible();
            }
        }

        // ����ִκŸ�ʽ�Ƿ���ȷ
        // �ִκű���Ϊ������
        // return:
        //      true    ��ȷ
        //      false   ����
        static bool CheckNumber(string strText)
        {
            if (StringUtil.IsPureNumber(strText) == true)
                return true;

            return false;
        }

        // ���Ѿ�����������У�ȡ��λ�����������ִκš�
        // ���������Զ��ų�MyselfBiblioRecPath������¼
        string GetTopNumber(ListView list)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];
                string strRecPath = item.Text;
                if (strRecPath != this.MyselfBiblioRecPath)
                    return item.SubItems[1].Text;
            }

            // TODO: ��������Լ����⣬��û������������Ч�ִκŵ������ˣ���Ҳֻ�����Լ����ִκ�-1���䵱��

            return "";  // û���ҵ�
        }

        // ʹ�����ظ��б�ɫ
        void ColorDup()
        {
            string strPrevNumber = "";
            Color color1 = Color.FromArgb(220, 220, 220);
            Color color2 = Color.FromArgb(230, 230, 230);
            Color color = color1;
            int nDupCount = 0;
            for (int i = 0; i < this.listView_number.Items.Count; i++)
            {
                string strNumber = this.listView_number.Items[i].SubItems[1].Text;

                // 2014/4/9
                // �س���һ���ֽ��бȽ�
                int index = strNumber.IndexOfAny(new char[] { '/', '.', ',', '=', '-', '#' });
                if (index != -1)
                    strNumber = strNumber.Substring(0, index);

                if (strNumber == strPrevNumber)
                {
                    if (i >= 1 && nDupCount == 0)
                        this.listView_number.Items[i - 1].BackColor = color;

                    this.listView_number.Items[i].BackColor = color;
                    nDupCount++;
                }
                else
                {
                    if (nDupCount >= 1)
                    {
                        // ��һ����ɫ
                        if (color == color1)
                            color = color2;
                        else
                            color = color1;
                    }

                    nDupCount = 0;

                    this.listView_number.Items[i].BackColor = SystemColors.Window;

                }


                strPrevNumber = strNumber;
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
            // this.label_tailNumberTitle.Text = "��'" + this.ZhongcihaoDbName + "'�е�β��(&T):";
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
                long lRet = Channel.GetZhongcihaoTailNumber(
                    stop,
                    GetZhongcihaoDbGroupName(this.BiblioDbName),
                    // "!" + this.BiblioDbName,
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

        // �ƶ�β�š�����Ѿ����ڵ�β�ű�strTestNumber��Ҫ�����ƶ�
        /// <summary>
        /// �ƶ�β�š�����Ѿ����ڵ�β�ű�strTestNumber��Ҫ�����ƶ�
        /// </summary>
        /// <param name="strTestNumber">���ڱȶԵ�β��</param>
        /// <param name="strOutputNumber">�����ƶ����β��</param>
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
                long lRet = Channel.SetZhongcihaoTailNumber(
                    stop,
                    "conditionalpush",
                    GetZhongcihaoDbGroupName(this.BiblioDbName),
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

        /// <summary>
        /// ����β��
        /// </summary>
        /// <param name="strTailNumber">Ҫ���õ�β��</param>
        /// <param name="strOutputNumber">ʵ�����õ�β��</param>
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
                long lRet = Channel.SetZhongcihaoTailNumber(
                    stop,
                    "save",
                    GetZhongcihaoDbGroupName(this.BiblioDbName),
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

        // ���β��
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

        // �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
        // return:
        //      -1  error
        //      0   not found
        //      1   succeed
        /// <summary>
        /// �õ���ǰ��Ŀ��ͳ�Ƴ��������ŵļ�1�Ժ�ĺ�
        /// </summary>
        /// <param name="strResult">���ؽ��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û���ҵ�; 1: �ɹ�</returns>
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

        // �������֣�ͬ���顢β��
        private void button_searchDouble_Click(object sender, EventArgs e)
        {
            string strError = "";

            EventFinish.Reset();
            try
            {
                this.textBox_tailNumber.Text = "";   // Ԥ��filllist ��ǰ�˳�, ���Ǵ���

                int nRet = FillList(true, out strError);
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

        private void comboBox_biblioDbName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_biblioDbName.Items.Count > 0)
                return;

            // this.comboBox_biblioDbName.Items.Add("<ȫ��>");

            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty property = this.MainForm.BiblioDbProperties[i];
                    this.comboBox_biblioDbName.Items.Add(property.DbName);
                }
            }
        }

        // �������������������ݿ��������ִκŷ������任ΪAPIʹ�õ���̬
        static string GetZhongcihaoDbGroupName(string strText)
        {
            if (String.IsNullOrEmpty(strText) == true)
                return "";

            // �����һ���ַ���!���ţ������Ƿ�����
            if (strText[0] == '!')
                return strText.Substring(1);

            // û�У����ţ��������������ݿ���
            return "!" + strText;
        }

        // ����β��
        /// <summary>
        /// ����β��
        /// </summary>
        /// <param name="strDefaultNumber">ȱʡʱ��β�š������ǰ��û��β�ţ���ʹ����</param>
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
                long lRet = Channel.SetZhongcihaoTailNumber(
                    stop,
                    "increase",
                    GetZhongcihaoDbGroupName(this.BiblioDbName),
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

        #region Э���ⲿ���õĺ���

        /// <summary>
        /// �ȴ���������
        /// </summary>
        public void WaitSearchFinish()
        {
            for (; ; )
            {
                Application.DoEvents();
                bool bRet = this.EventFinish.WaitOne(10, true);
                if (bRet == true)
                    break;
            }
        }

        #endregion

        // ����һ���Ĳ��ԣ�����ִκ�
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        /// <summary>
        /// ����һ���Ĳ��ԣ�����ִκ�
        /// </summary>
        /// <param name="style">�ִκ�ȡ�ŵķ��</param>
        /// <param name="strClass">���</param>
        /// <param name="strBiblioDbName">��Ŀ����</param>
        /// <param name="strNumber">�����ִκ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public int GetNumber(
            ZhongcihaoStyle style,
            string strClass,
            string strBiblioDbName,
            out string strNumber,
            out string strError)
        {
            strNumber = "";
            strError = "";
            int nRet = 0;

            this.ClassNumber = strClass;
            this.BiblioDbName = strBiblioDbName;

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
                    "1",
            this.MainForm.DefaultFont);
                if (strNumber == null)
                    return 0;	// ������������

                return 1;
            }

            // ÿ�ζ�������Ŀͳ�����������顢У��β��
            if (style == ZhongcihaoStyle.BiblioAndSeed
                || style == ZhongcihaoStyle.SeedAndBiblio)
            {

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
                        strTestNumber = "1";

                    // �������û�й���¼����ǰ�ǵ�һ��
                    strNumber = InputDlg.GetInput(
                        this,
                        null,
                        "�������� '" + strClass + "' �ĵ�ǰ�ִκ�����:",
                        strTestNumber,
            this.MainForm.DefaultFont);
                    if (strNumber == null)
                        return 0;	// ������������

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
                        strTestNumber = "1";
                    // �������û�й���¼����ǰ�ǵ�һ��
                    strNumber = InputDlg.GetInput(
                        this,
                        null,
                        "�������� '" + strClass + "' �ĵ�ǰ�ִκ�����:",
                        strTestNumber,
            this.MainForm.DefaultFont);
                    if (strNumber == null)
                        return 0;	// ������������

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

        // ˫��������Ŀ��¼װ����ϸ��
        private void listView_number_DoubleClick(object sender, EventArgs e)
        {
            if (this.listView_number.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫװ����ϸ��������");
                return;
            }
            string strPath = this.listView_number.SelectedItems[0].SubItems[0].Text;

            EntityForm form = new EntityForm();

            form.MdiParent = this.MainForm;

            form.MainForm = this.MainForm;
            form.Show();
            form.LoadRecordOld(strPath, "", true);

        }

        private void ZhongcihaoForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);

        }

        private void listView_number_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;



            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Click += new System.EventHandler(this.menu_selectAllLines_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("������ѡ��� " + this.listView_number.SelectedItems.Count.ToString() + " �������¼·���ļ�(&S)...");
            menuItem.Click += new System.EventHandler(this.menu_saveToRecordPathFile_Click);
            if (this.listView_number.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView_number, new Point(e.X, e.Y));		
        }

        void menu_selectAllLines_Click(object sender, EventArgs e)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                ListViewUtil.SelectAllLines(this.listView_number);
            }
            finally
            {
                this.Cursor = oldCursor;
            }
        }

        // ���浽��¼·���ļ�
        void menu_saveToRecordPathFile_Click(object sender, EventArgs e)
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
                    "BiblioSearchForm",
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

                foreach (ListViewItem item in this.listView_number.SelectedItems)
                {
                    // ListViewItem item = this.listView_number.SelectedItems[i];
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

            this.MainForm.StatusBarMessage = "��Ŀ��¼·�� " + this.listView_number.SelectedItems.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportRecPathFilename;
        }

        private void listView_number_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListViewUtil.OnSeletedIndexChanged(this.listView_number,
                0,
                new List<int> { 1 });
        }

    }

    // ����
    // Implements the manual sorting of items by columns.
    class ZhongcihaoListViewItemComparer : IComparer
    {
        public ZhongcihaoListViewItemComparer()
        {
        }

        public int Compare(object x, object y)
        {
            // �ִκ��ַ�����Ҫ�Ҷ��� 2007/10/12
            string s1 = ((ListViewItem)x).SubItems[1].Text;
            string s2 = ((ListViewItem)y).SubItems[1].Text;

#if NO
            CanonicalString(ref s1, ref s2);
            return -1 * String.Compare(s1, s2);
#endif
            return -1 * CompareString(s1, s2);
        }

        // �Ƚ������ַ���
        // �Ȱ��� / �и�Ϊ������֡�Ȼ��ÿ�����ֽ��л���Ƚ�
        static int CompareString(string s1, string s2)
        {
            string [] parts1 = s1.Split(new char[] { '/' });
            string [] parts2 = s2.Split(new char[] { '/' });

            int nCount = Math.Max(parts1.Length, parts2.Length);
            for (int i = 0; i < nCount; i++)
            {
                if (i >= parts1.Length)
                    return -1;
                if (i >= parts2.Length)
                    return 1;

                string p1 = parts1[i];
                string p2 = parts2[i];

                CanonicalString(ref p1, ref p2);
                int nRet = String.Compare(s1, s2);
                if (nRet != 0)
                    return nRet;

            }

            return 0;
        }

        // 2008/9/19 new add
        // ���滯�����Ƚϵ��ַ���
        // ����'.'���и���ţ���������ι淶��Ϊ�˴˵ȳ�
        static void CanonicalString(ref string s1, ref string s2)
        {
            string[] a1 = s1.Split(new char[] {'.', ',', '=','-', '#' });
            string[] a2 = s2.Split(new char[] { '.', ',', '=', '-', '#' });

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

    }

    // 
    /// <summary>
    /// �ִκ�ȡ�ŵķ��
    /// </summary>
    public enum ZhongcihaoStyle
    {
        /// <summary>
        /// ��������Ŀͳ������
        /// </summary>
        Biblio = 1, // ��������Ŀͳ������
        /// <summary>
        /// ÿ�ζ�������Ŀͳ�����������顢У��β�š�ƫ����Ŀͳ��ֵ����äĿ����β��
        /// </summary>
        BiblioAndSeed = 2,  // ÿ�ζ�������Ŀͳ�����������顢У��β�š�ƫ����Ŀͳ��ֵ����äĿ����β�š�
        /// <summary>
        /// ÿ�ζ�������Ŀͳ�����������顢У��β�š�ƫ��(β�ſ��)β�ţ�ÿ�ζ�����β��
        /// </summary>
        SeedAndBiblio = 3, // ÿ�ζ�������Ŀͳ�����������顢У��β�š�ƫ��β�ţ�ÿ�ζ�����β��
        /// <summary>
        /// ������(�ִκſ�)β��
        /// </summary>
        Seed = 4, // ������(�ִκſ�)β��
    }

}