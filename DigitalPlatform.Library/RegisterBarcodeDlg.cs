using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.Web;

// using SHDocVw;
// using mshtml;

using System.Reflection;

using DigitalPlatform.GUI;
using DigitalPlatform.rms.Client;

using DigitalPlatform.Script;
using DigitalPlatform.Marc;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;

namespace DigitalPlatform.Library
{
    /// <summary>
    /// ������ŵ�¼�Ի���
    /// </summary>
    public partial class RegisterBarcodeDlg : Form
    {
        // Assembly AssemblyFilter = null;
        MyFilterDocument filter = null;
        ScriptManager scriptManager = new ScriptManager();

        /// <summary>
        /// ����ַ���
        /// </summary>
        public string ResultString = "";


        BrowseSearchResultDlg browseWindow = null; 

        /// <summary>
        /// 
        /// </summary>
        public SearchPanel SearchPanel = null;

        string m_strServerUrl = "";
        string m_strBiblioDbName = "";
        string m_strItemDbName = "";

        string m_strBiblioRecPath = "";

        /// <summary>
        /// ���Դ���
        /// </summary>
        public string Lang = "zh";

        /// <summary>
        /// �������
        /// </summary>
        public BookItemCollection Items = null;

        bool m_bSearchOnly = false; // �Ƿ�ֻΪ��������񡢲�֧�ֲ��¼����

        XmlDocument dom = null; // cfgs/global�����ļ�

        /// <summary>
        /// ����ϸ��
        /// </summary>
        public event OpenDetailEventHandler OpenDetail = null;


        /// <summary>
        /// �Ƿ�ֻ��������¼
        /// </summary>
        public bool SearchOnly
        {
            get
            {
                return m_bSearchOnly;
            }
            set
            {
                m_bSearchOnly = value;
                if (m_bSearchOnly == true)
                {
                    this.button_register.Text = "����(&S)";
                    this.button_save.Enabled = false;
                    this.Text = "��ز����";
                }
                else
                {
                    this.button_register.Text = "�Ǽ�(&R)";
                    this.button_save.Enabled = true;
                    this.Text = "��ز��¼";
                }
            }
        }

        /// <summary>
        /// ������URL
        /// </summary>
        public string ServerUrl
        {
            get
            {
                return this.m_strServerUrl;
            }
            set
            {
                this.m_strServerUrl = value;
                UpdateTargetInfo();
            }
        }

        /// <summary>
        /// ��Ŀ����
        /// </summary>
        public string BiblioDbName
        {
            get
            {
                return this.m_strBiblioDbName;
            }
            set
            {
                this.m_strBiblioDbName = value;
                UpdateTargetInfo();
            }
        }

        /// <summary>
        /// ʵ�����
        /// </summary>
        public string ItemDbName
        {
            get
            {
                return this.m_strItemDbName;
            }
            set
            {
                this.m_strItemDbName = value;
                UpdateTargetInfo();
            }
        }

        /// <summary>
        /// ��Ŀ��¼·��
        /// </summary>
        public string BiblioRecPath
        {
            get
            {
                return this.m_strBiblioRecPath;
            }
            set
            {
                this.m_strBiblioRecPath = value;
                this.label_biblioRecPath.Text = "�ּ�¼·��: " + value;
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public RegisterBarcodeDlg()
        {
            InitializeComponent();
        }

        private void RegisterBarcodeDlg_Load(object sender, EventArgs e)
        {
            UpdateTargetInfo();

            FillFromList();

        }

        private void button_target_Click(object sender, EventArgs e)
        {
            GetLinkDbDlg dlg = new GetLinkDbDlg();

            dlg.SearchPanel = this.SearchPanel;
            dlg.ServerUrl = this.ServerUrl;
            dlg.BiblioDbName = this.BiblioDbName;
            dlg.ItemDbName = this.ItemDbName;

            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.ServerUrl = dlg.ServerUrl;
            this.BiblioDbName = dlg.BiblioDbName;
            this.ItemDbName = dlg.ItemDbName;

            FillFromList();
        }

        // ����
        private void button_save_Click(object sender, EventArgs e)
        {
            string strError = "";

            EnableControls(false);
            int nRet = this.SaveItems(out strError);
            EnableControls(true);
            if (nRet == -1)
                goto ERROR1;

            MessageBox.Show(this, "����Ϣ����ɹ���");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;

        }

        // ����Ŀ���йص���ʾ��Ϣ
        void UpdateTargetInfo()
        {
            this.label_target.Text = "������: " + this.ServerUrl + "\r\n��Ŀ��: " + this.BiblioDbName + ";    ʵ���: " + this.ItemDbName;
        }

        // ���from�б�
        void FillFromList()
        {
            this.comboBox_from.Items.Clear();


            if (this.ServerUrl == "")
                return;

            if (this.BiblioDbName == "")
                return;


            string strOldSelectedItem = this.comboBox_from.Text;
            this.comboBox_from.Text = "";
            RmsChannel channel = this.SearchPanel.Channels.GetChannel(this.ServerUrl);

            Debug.Assert(channel != null, "Channels.GetChannel() �쳣");

            string [] items = null;

            string strError = "";

            long lRet = channel.DoDir(this.BiblioDbName,
                this.Lang,
                null,   // ����Ҫ�г�ȫ�����Ե�����
                ResTree.RESTYPE_FROM,
                out items,
                out strError);

            if (lRet == -1)
            {
                strError = "�� '" +this.BiblioDbName + "' ��ļ���;��ʱ��������: " + strError;
                goto ERROR1;
            }

            bool bFoundOldItem = false;
            for (int i = 0; i < items.Length; i++)
            {
                if (strOldSelectedItem == items[i])
                    bFoundOldItem = true;
                this.comboBox_from.Items.Add(items[i]);
            }

            if (bFoundOldItem == true)
                this.comboBox_from.Text = strOldSelectedItem;
            else
            {
                if (this.comboBox_from.Items.Count > 0)
                    this.comboBox_from.Text = (string)this.comboBox_from.Items[0];
            }

            return;

        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        static bool IsISBnBarcode(string strText)
        {
            if (strText.Length == 13)
            {
                string strHead = strText.Substring(0, 3);
                if (strHead == "978")
                    return true;
            }

            return false;
        }

        string GetQueryString()
        {
            string strText = this.textBox_queryWord.Text;
            int nRet = strText.IndexOf(';');
            if (nRet != -1)
            {
                strText = strText.Substring(0, nRet).Trim();
                this.textBox_queryWord.Text = strText;
            }

            if (this.checkBox_autoDetectQueryBarcode.Checked == true)
            {
                if (strText.Length == 13)
                {
                    string strHead = strText.Substring(0, 3);
                    if (strHead == "978")
                    {
                        this.textBox_queryWord.Text = strText + " ;�Զ���" + strText.Substring(3, 9)+"������";
                        return strText.Substring(3, 9);
                    }
                }
            }

            return strText;
        }

        /// <summary>
        /// ��������Ŀ����
        /// </summary>
        /// <param name="strError"></param>
        /// <returns></returns>
        public long SearchBiblio(out string strError)
        {
            this.SearchPanel.BeginLoop("���ڼ��� " + this.textBox_queryWord.Text + " ...");
            try
            {
                // 2007/4/5 ���� ������ GetXmlStringSimple()
                string strQueryXml = "<target list='"
                    + StringUtil.GetXmlStringSimple(this.BiblioDbName + ":" + this.comboBox_from.Text)        // 2007/9/14 new add
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(GetQueryString())
                    + "</word><match>left</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>" + this.Lang + "</lang></target>";


                ActivateBrowseWindow();

                long lRet = 0;

                this.SearchPanel.BrowseRecord += new BrowseRecordEventHandler(SearchPanel_BrowseRecord);

                try
                {
                    // ����
                    lRet = this.SearchPanel.SearchAndBrowse(
                        this.ServerUrl,
                        strQueryXml,
                        true,
                        out strError);
                }
                finally
                {
                    this.SearchPanel.BrowseRecord -= new BrowseRecordEventHandler(SearchPanel_BrowseRecord);
                }


                if (lRet == 1 && this.browseWindow != null)
                {
                    this.browseWindow.LoadFirstDetail(true);
                }

                if (lRet == 0)
                {
                    strError = "û�����С�";
                }


                if (lRet == 0 || lRet == -1)
                {
                    this.browseWindow.Close();
                    this.browseWindow = null;

                }


                return lRet;

            }
            finally
            {
                this.SearchPanel.EndLoop();
            }

        }

        void ActivateBrowseWindow()
        {
            if (this.browseWindow == null
                || (this.browseWindow != null && this.browseWindow.IsDisposed == true))
            {
                this.browseWindow = new BrowseSearchResultDlg();
                this.browseWindow.Text = "���ж����ּ�¼�������ѡ��һ��";
                this.browseWindow.Show();

                this.browseWindow.OpenDetail -= new OpenDetailEventHandler(browseWindow_OpenDetail);
                this.browseWindow.OpenDetail += new OpenDetailEventHandler(browseWindow_OpenDetail);
            }
            else
            {
                this.browseWindow.BringToFront();
                this.browseWindow.RecordsList.Items.Clear();
            }
        }

        void SearchPanel_BrowseRecord(object sender, BrowseRecordEventArgs e)
        {
            this.browseWindow.NewLine(e.FullPath,
                e.Cols);
        }

        // װ����ϸ��¼
        void browseWindow_OpenDetail(object sender, OpenDetailEventArgs e)
        {
            if (e.Paths.Length == 0)
                return;

            ResPath respath = new ResPath(e.Paths[0]);

            string strError = "";
            int nRet = LoadBiblioRecord(
                respath.Path,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        int LoadBiblioRecord(
            string strBiblioRecPath,
            out string strError)
        {
            strError = "";
            this.BiblioRecPath = strBiblioRecPath;

            string strMarcXml = "";
            byte[] baTimeStamp = null;

            int nRet = this.SearchPanel.GetRecord(
                this.ServerUrl,
                this.BiblioRecPath,
                out strMarcXml,
                out baTimeStamp,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // ת��ΪMARC��ʽ

            string strOutMarcSyntax = "";
            string strMarc = "";
            // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
            // parameters:
            //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
            //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
            //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
            nRet = MarcUtil.Xml2Marc(strMarcXml,
                true,
                "", // this.CurMarcSyntax,
                out strOutMarcSyntax,
                out strMarc,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (this.filter == null)
            {
                string strCfgFilePath = this.BiblioDbName + "/cfgs/html.fltx";
                string strContent = "";
                // ��������ļ�
                // return:
                //		-1	error
                //		0	not found
                //		1	found
                nRet = this.SearchPanel.GetCfgFile(
                    this.ServerUrl,
                    strCfgFilePath,
                    out strContent,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    strError = "�ڷ����� " + this.ServerUrl + " ��û���ҵ������ļ� '" + strCfgFilePath + "' �����ֻ����MARC��������ʽ��ʾ��Ŀ��Ϣ...";
                    string strText = strMarc.Replace((char)31, '^');
                    strText = strText.Replace(new string((char)30, 1), "<br/>");
                    this.HtmlString = strText;
                    goto ERROR1;
                }


                MyFilterDocument tempfilter = null;

                nRet = PrepareMarcFilter(
                    strContent,
                    //Environment.CurrentDirectory + "\\marc.fltx",
                    out tempfilter,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.filter = tempfilter;
            }

            this.ResultString = "";

            // ����filter�е�Record��ض���
            nRet = this.filter.DoRecord(
                null,
                strMarc,
                0,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.HtmlString = this.ResultString;

            // װ�ز���Ϣ
            nRet = LoadItems(this.BiblioRecPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return 0;

        ERROR1:
            return -1;
        }

        // �����ǰ�������Ϣ
        void Clear()
        {
            this.HtmlString = "(blank)";
            this.listView_items.Items.Clear();
            this.BiblioRecPath = "";
        }

        private void button_search_Click(object sender, EventArgs e)
        {
            string strError = "";

            // �����ǰ�������Ϣ
            /*
            this.HtmlString = "(blank)";
            this.listView_items.Items.Clear();
            this.BiblioRecPath = "";
             */
            this.Clear();

            EnableControls(false);
            long nRet = SearchBiblio(out strError);
            EnableControls(true);
            if (nRet == 0 || nRet == -1)
                goto ERROR1;

            this.textBox_queryWord.SelectAll();

            this.textBox_itemBarcode.Focus();   // �����л��������textbox
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            this.textBox_queryWord.Focus();
            this.textBox_queryWord.SelectAll();
            return;
        }

        /// <summary>
        /// HTML�ַ���
        /// </summary>
        public string HtmlString
        {
            get
            {
                HtmlDocument doc = this.webBrowser_record.Document;

                if (doc == null)
                    return "";

                HtmlElement item = doc.All["html"];
                if (item == null)
                    return "";

                return item.OuterHtml;

            }
            set
            {
                // this.webBrowser_record.Navigate("about:blank");


                HtmlDocument doc = this.webBrowser_record.Document;

                if (doc == null)
                {
                    this.webBrowser_record.Navigate("about:blank");
                    doc = this.webBrowser_record.Document;
                }

                doc = doc.OpenNew(true);
                doc.Write(value);
            }
        }


        int PrepareMarcFilter(
            string strFilterFileContent,
            out MyFilterDocument filter,
            out string strError)
        {
            filter = new MyFilterDocument();

            filter.HostForm = this;
            filter.strOtherDef = "RegisterBarcodeDlg HostForm = null;";

            filter.strPreInitial = " MyFilterDocument doc = (MyFilterDocument)this.Document;\r\n";
            filter.strPreInitial += " HostForm = ("
                + "RegisterBarcodeDlg" + ")doc.HostForm;\r\n";

            // filter.Load(strFilterFileName);
            filter.LoadContent(strFilterFileContent);

            string strCode = "";    // c#����

            int nRet = filter.BuildScriptFile(out strCode,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string[] saAddRef1 = {
										 Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.rms.client.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.library.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
										 Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
										 Environment.CurrentDirectory + "\\dp2rms.exe",
										 /*strMainCsDllName*/ };

            Assembly assembly = null;
            string strWarning = "";
            string strLibPaths = "";

            string[] saRef2 = filter.GetRefs();

            string[] saRef = new string[saRef2.Length + saAddRef1.Length];
            Array.Copy(saRef2, saRef, saRef2.Length);
            Array.Copy(saAddRef1, 0, saRef, saRef2.Length, saAddRef1.Length);

            // ����Script��Assembly
            // �������ڶ�saRef���ٽ��к��滻
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saRef,
                strLibPaths,
                out assembly,
                out strError,
                out strWarning);

            if (nRet == -2)
                goto ERROR1;
            if (nRet == -1)
            {
                if (strWarning == "")
                {
                    goto ERROR1;
                }
                MessageBox.Show(this, strWarning);
            }

            filter.Assembly = assembly;

            return 0;
        ERROR1:
            return -1;
        }

        private void textBox_queryWord_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_search;
        }

        private void textBox_itemBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_register;
        }

        private void button_register_Click(object sender, EventArgs e)
        {
            if (m_bSearchOnly == true)
            {
                this.DoSearchFromBarcode(true);
            }
            else
            {
                this.DoRegisterBarcode();
            }
        }

        // ͨ��������ż����쿴����ֲ���Ϣ
        // �Ƿ�����Ż�Ϊ���ȿ�����ǰ�������Ƿ�����Ҫ����������ţ�������Ҳ���ڷ����˲��ع��ܡ�
        void DoSearchFromBarcode(bool bDetectDup)
        {
            string strError = "";
            // string strBiblioRecPath = "";
            // string strItemRecPath = "";


            EnableControls(false);

            try
            {

                this.Clear();

                List<DoublePath> paths = null;

                int nRet = GetLinkInfoFromBarcode(
                    this.textBox_itemBarcode.Text,
                    true,
                    out paths,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nRet == 0)
                {
                    goto NOTFOUND;
                }

                DoublePath dpath = null;

                if (nRet > 1)
                {
                    // MessageBox.Show(this, "������� " + this.textBox_itemBarcode.Text + "�������ظ������뼰ʱ�����");
                    SelectDupItemRecordDlg dlg = new SelectDupItemRecordDlg();
                    dlg.MessageText = "������� " + this.textBox_itemBarcode.Text + "�������ظ������⽫�ᵼ��ҵ���ܳ��ֹ��ϡ�\r\n\r\n��ѡ��ǰϣ���۲��һ�����¼��";
                    dlg.Paths = paths;
                    dlg.ShowDialog(this);

                    if (dlg.DialogResult != DialogResult.OK)
                        return; // ��������

                    dpath = dlg.SelectedDoublePath;
                }
                else
                {
                    dpath = paths[0];
                }

                this.BiblioDbName = ResPath.GetDbName(dpath.BiblioRecPath);
                this.ItemDbName = ResPath.GetDbName(dpath.ItemRecPath);

                nRet = LoadBiblioRecord(
                    dpath.BiblioRecPath,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ͻ����ʾ�����еĲ���Ϣ��
                ListViewItem listitem = this.GetListViewItem(
                    this.textBox_itemBarcode.Text,
                    dpath.ItemRecPath);

                if (listitem == null)
                {
                    strError = "�������Ϊ '" + this.textBox_itemBarcode.Text 
                        + "' ���¼·��Ϊ '"+ dpath.ItemRecPath +"' ��ListViewItem��Ȼ������ ...";
                    goto ERROR1;
                }

                listitem.Selected = true;
                listitem.Focused = true;
                this.listView_items.EnsureVisible(this.listView_items.Items.IndexOf(listitem));

            }
            finally
            {
                EnableControls(true);
            }



            this.textBox_itemBarcode.SelectAll();
            this.textBox_itemBarcode.Focus();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        NOTFOUND:
            MessageBox.Show(this, "������� " + this.textBox_itemBarcode.Text + " û���ҵ���Ӧ�ļ�¼��");
            this.textBox_itemBarcode.SelectAll();
            this.textBox_itemBarcode.Focus();
            return;
        }

        // ���cfgs/global�����ļ�
        int GetGlobalCfgFile(out string strError)
        {
            strError = "";

            if (this.dom != null)
                return 0;	// �Ż�

            if (this.ServerUrl == "")
            {
                strError = "��δָ��������URL";
                return -1;
            }

            string strCfgFilePath = "cfgs/global";
            XmlDocument tempdom = null;
            // ��������ļ�
            // return:
            //		-1	error
            //		0	not found
            //		1	found
            int nRet = this.SearchPanel.GetCfgFile(
                this.ServerUrl,
                strCfgFilePath,
                out tempdom,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "�����ļ� '" + strCfgFilePath + "' û���ҵ�...";
                return -1;
            }

            this.dom = tempdom;

            return 0;
        }


        // ���ݲ��������һϵ�п��ܵ�ʵ����м���������Ϣ��
        // Ȼ����ȡ���й��ֵ���Ϣ
        int GetLinkInfoFromBarcode(string strBarcode,
            bool bDetectDup,
            out List<DoublePath> paths,
            out string strError)
        {
            strError = "";
            // strBiblioRecPath = "";
            // strItemRecPath = "";

            paths = new List<DoublePath>();

            string strBiblioRecPath = "";
            string strItemRecPath = "";

            // ���cfgs/global�����ļ�
            int nRet = GetGlobalCfgFile(out strError);
            if (nRet == -1)
                return -1;

            // �г�����<dblink>��������
            XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("//dblink");
            if (nodes.Count == 0)
            {
                strError = "cfgs/global�����ļ��У���δ�����κ�<dblink>Ԫ�ء�";
                return -1;
            }


            this.SearchPanel.BeginLoop("���ڼ��� " + strBarcode + " ����Ӧ�Ĳ���Ϣ...");
            try
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];
                    string strBiblioDbName = DomUtil.GetAttr(node, "bibliodb");
                    string strItemDbName = DomUtil.GetAttr(node, "itemdb");

                    // 2007/4/5 ���� ������ GetXmlStringSimple()
                    string strQueryXml = "<target list='"
                        + StringUtil.GetXmlStringSimple(strItemDbName + ":" + "������")       // 2007/9/14 new add
                        + "'><item><word>"
                        + StringUtil.GetXmlStringSimple(strBarcode)
                        + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>" + this.Lang + "</lang></target>";

                    // strItemRecPath = "";
                    List<string> aPath = null;

                    nRet = this.SearchPanel.SearchMultiPath(
                        this.ServerUrl,
                        strQueryXml,
                        1000,
                        out aPath,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                        continue;

                    for (int j = 0; j < aPath.Count; j++)
                    {
                        strItemRecPath = aPath[j];

                        XmlDocument tempdom = null;
                        byte[] baTimestamp = null;
                        // ��ȡ���¼
                        nRet = this.SearchPanel.GetRecord(
                            this.ServerUrl,
                            strItemRecPath,
                            out tempdom,
                            out baTimestamp,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "��ȡ���¼ " + strItemRecPath + " ʱ��������" + strError;
                            return -1;
                        }

                        strBiblioRecPath = strBiblioDbName + "/" + DomUtil.GetElementText(tempdom.DocumentElement, "parent");

                        DoublePath dpath = new DoublePath();
                        dpath.ItemRecPath = strItemRecPath;
                        dpath.BiblioRecPath = strBiblioRecPath;

                        paths.Add(dpath);
                    }

                    // �������Ҫ���أ����������к󾡿��˳�ѭ��
                    if (bDetectDup == false && paths.Count >= 1)
                        return paths.Count;
                }

                return paths.Count;
            }
            finally
            {
                this.SearchPanel.EndLoop();
            }
        }

        // ���¼
        // ��������Ĳ����������һ����Ϣ�����߶�λ���Ѿ����ڵ���
        void DoRegisterBarcode()
        {
            string strError = "";
            int nRet;

            if (this.textBox_itemBarcode.Text == "")
            {
                strError = "��δ����������";
                goto ERROR1;
            }

            // ���������������Ƿ�ΪISBN�����
            if (IsISBnBarcode(this.textBox_itemBarcode.Text) == true)
            {
                // ���浱ǰ����Ϣ
                EnableControls(false);
                nRet = this.SaveItems(out strError);
                EnableControls(true);
                if (nRet == -1)
                    goto ERROR1;

                // ת���������ּ�������
                this.textBox_queryWord.Text = this.textBox_itemBarcode.Text;
                this.textBox_itemBarcode.Text = "";

                this.button_search_Click(null, null);
                return;
            }


            if (this.Items == null)
                this.Items = new BookItemCollection();

            // ���ڲ���
            BookItem item = this.Items.GetItem(this.textBox_itemBarcode.Text);

            ListViewItem listitem = null;

            // ����ò�����ŵ������Ѿ�����
            if (item != null)
            {
                listitem = this.GetListViewItem(this.textBox_itemBarcode.Text,
                    null);

                if (listitem == null)
                {
                    strError = "�������Ϊ '" + this.textBox_itemBarcode.Text + "'��BookItem�ڴ�������ڣ�����û�ж�Ӧ��ListViewItem ...";
                    goto ERROR1;
                }

                UnselectListViewItems();
                listitem.Selected = true;
                listitem.Focused = true;
                this.listView_items.EnsureVisible(this.listView_items.Items.IndexOf(listitem));
                goto END1;
            }

            List<DoublePath> paths = null;

            // ����ȫ�����
            nRet = GetLinkInfoFromBarcode(
                this.textBox_itemBarcode.Text,
                true,
                out paths,
                out strError);
            if (nRet == -1)
            {
                strError = "������Ų��ز������ִ���" + strError;
                goto ERROR1;
            }

            if (nRet > 0)
            {
                // MessageBox.Show(this, "������� " + this.textBox_itemBarcode.Text + "�������ظ������뼰ʱ�����");
                SelectDupItemRecordDlg dlg = new SelectDupItemRecordDlg();
                dlg.MessageText = "������� " + this.textBox_itemBarcode.Text + " �Ѿ�����¼���ˣ�������¡�\r\n\r\n�������ϸ�۲죬��ѡ��ǰϣ���۲��һ�����¼�������밴��ȡ������ť��";
                dlg.Paths = paths;
                dlg.ShowDialog(this);

                if (dlg.DialogResult != DialogResult.OK)
                    return; // ��������

                if (this.BiblioRecPath != dlg.SelectedDoublePath.BiblioRecPath)
                {
                    MessageBox.Show(this, "��ע����������Զ�װ������ " + dlg.SelectedDoublePath.BiblioRecPath + " �������У����Ժ����������ԭ�� " + this.BiblioRecPath + " ���в��¼�����м�����װ��ԭ�ֺ����в��¼ ...");
                }

                // �ȱ��汾��
                nRet = this.SaveItems(out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.textBox_queryWord.Text = "";   // qingchu yuanlai de jiansuoci , bimian wuhui 

                DoublePath dpath = dlg.SelectedDoublePath;

                this.BiblioDbName = ResPath.GetDbName(dpath.BiblioRecPath);
                this.ItemDbName = ResPath.GetDbName(dpath.ItemRecPath);

                nRet = LoadBiblioRecord(
                    dpath.BiblioRecPath,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ͻ����ʾ�����еĲ���Ϣ��
                listitem = this.GetListViewItem(
                    this.textBox_itemBarcode.Text,
                    dpath.ItemRecPath);

                if (listitem == null)
                {
                    strError = "�������Ϊ '" + this.textBox_itemBarcode.Text
                        + "' ���¼·��Ϊ '" + dpath.ItemRecPath + "' ��ListViewItem��Ȼ������ ...";
                    goto ERROR1;
                }

                listitem.Selected = true;
                listitem.Focused = true;
                this.listView_items.EnsureVisible(this.listView_items.Items.IndexOf(listitem));

                this.textBox_itemBarcode.SelectAll();
                this.textBox_itemBarcode.Focus();
                return;
            }


            item = new BookItem();

            item.Barcode = this.textBox_itemBarcode.Text;
            item.Changed = true;    // ��ʾ����������

            this.Items.Add(item);

            listitem = item.AddToListView(this.listView_items);

            // ����ѡ����
            UnselectListViewItems();
            listitem.Focused = true;
            this.listView_items.EnsureVisible(this.listView_items.Items.IndexOf(listitem));

        END1:
            this.textBox_itemBarcode.SelectAll();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��listview�и��ݲ�����Ŷ�λһ��item����
        // parameters:
        //      strItemRecPath  ���¼·�������==null�����ʾ�������ڶ�λ�в�������
        ListViewItem GetListViewItem(string strBarcode,
            string strItemRecPath)
        {
            int nColumnIndex = this.listView_items.Columns.IndexOf(columnHeader_recpath);

            for (int i = 0; i < this.listView_items.Items.Count; i++)
            {
                if (strBarcode == this.listView_items.Items[i].Text)
                {
                    if (String.IsNullOrEmpty(strItemRecPath) == true)
                        return this.listView_items.Items[i];

                    if (strItemRecPath == this.listView_items.Items[i].SubItems[nColumnIndex].Text)
                        return this.listView_items.Items[i];
                }
            }

            return null;
        }

        void UnselectListViewItems()
        {
            for (int i = 0; i < this.listView_items.Items.Count; i++)
            {
                this.listView_items.Items[i].Selected = false;
            }

        }

        int LoadItems(string strBiblioRecPath,
            out string strError)
        {
            strError = "";

            this.listView_items.Items.Clear();

            if (String.IsNullOrEmpty(strBiblioRecPath) == true)
            {
                /*
                strError = "strBiblioRecPath��������Ϊ��";
                return -1;
                 */
                return 0;
            }

            if (this.Items == null)
                this.Items = new BookItemCollection();
            else
                this.Items.Clear();

            // ���������й������ּ�¼id�Ĳ��¼
            long lRet = SearchItems(ResPath.GetRecordId(strBiblioRecPath),
                out strError);
            if (lRet == -1)
                return -1;

            return 0;
        }

        /// <summary>
        /// ������������
        /// </summary>
        /// <param name="strBiblioRecId"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public long SearchItems(string strBiblioRecId,
            out string strError)
        {
            this.SearchPanel.BeginLoop("���ڼ������д����� " + strBiblioRecId + " �Ĳ��¼ ...");
            try
            {
                string strQueryXml = "<target list='"
                    + StringUtil.GetXmlStringSimple(this.ItemDbName + ":" + "����¼")       // 2007/9/14 new add
                    + "'><item><word>"
                    + strBiblioRecId
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>" + this.Lang + "</lang></target>";

                // ����
                long lRet = 0;

                this.SearchPanel.BrowseRecord += new BrowseRecordEventHandler(SearchItems_BrowseRecord);

                try
                {
                    lRet = this.SearchPanel.SearchAndBrowse(
                         this.ServerUrl,
                         strQueryXml,
                         false,
                         out strError);
                }
                finally
                {
                    this.SearchPanel.BrowseRecord -= new BrowseRecordEventHandler(SearchItems_BrowseRecord);
                }

                return lRet;

            }
            finally
            {
                this.SearchPanel.EndLoop();
            }

        }

        // ��������Ϣ�����У�����������ÿ����¼�Ļص�����
        void SearchItems_BrowseRecord(object sender, BrowseRecordEventArgs e)
        {
            ResPath respath = new ResPath(e.FullPath);

            XmlDocument tempdom = null;
            byte[] baTimeStamp = null;
            string strError = "";

            int nRet = this.SearchPanel.GetRecord(
                respath.Url,
                respath.Path,
                out tempdom,
                out baTimeStamp,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            BookItem item = new BookItem(respath.Path, tempdom);

            item.Timestamp = baTimeStamp;
            this.Items.Add(item);

            item.AddToListView(this.listView_items);

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �������Ϣ
        // (���ɾ������Ϣ����һ�����ֵ�����)
        int SaveItems(out string strError)
        {
            strError = "";

            if (this.Items == null)
            {
                strError = "Items��δ��ʼ��";
                return -1;
            }

            for (int i = 0; i < this.Items.Count; i++)
            {
                BookItem item = this.Items[i];

                // ����û���޸Ĺ�������
                if (item.Changed == false)
                    continue;

                // ������
                if (item.RecPath == "")
                    item.RecPath = this.ItemDbName + "/?";

                if (item.Parent == "")
                {
                    if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
                    {
                        strError = "��BiblioRecPath��ԱΪ�գ��޷��������Ϣ��";
                        return -1;
                    }
                    item.Parent = ResPath.GetRecordId(this.BiblioRecPath);
                }

                string strXml = "";

                int nRet = item.BuildRecord(
                    out strXml,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�� " +Convert.ToString(i+1)+" �й�����¼ʱ����: " + strError;
                    return -1;
                }

                byte[] baOutputTimestamp = null;

                nRet = this.SearchPanel.SaveRecord(
                    this.ServerUrl,
                    item.RecPath,
                    strXml,
                    item.Timestamp,
                    true,
                    out baOutputTimestamp,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�� " + Convert.ToString(i + 1) + " �б�����¼ʱ����: " + strError;
                    return -1;
                }
                item.Timestamp = baOutputTimestamp;
                item.Changed = false;
                // ������ɫ�ᷢ���仯
                item.RefreshItemColor();

            }

            return 0;
        }

        void EnableControls(bool bEnable)
        {
            this.textBox_itemBarcode.Enabled = bEnable;
            this.textBox_queryWord.Enabled = bEnable;
            this.comboBox_from.Enabled = bEnable;
            this.checkBox_autoDetectQueryBarcode.Enabled = bEnable;
            this.button_register.Enabled = bEnable;
            this.button_save.Enabled = bEnable;
            this.button_search.Enabled = bEnable;
            this.listView_items.Enabled = bEnable;
        }

        private void RegisterBarcodeDlg_Activated(object sender, EventArgs e)
        {

            if (this.browseWindow == null
                || (this.browseWindow != null && this.browseWindow.IsDisposed == true))
            {
            }
            else
            {
                this.browseWindow.BringToFront();
            }
        }

        private void RegisterBarcodeDlg_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.Items != null)
            {
                if (this.Items.Changed == true)
                {
                    DialogResult result = MessageBox.Show(this,
    "��ǰ�в���Ϣ���޸ĺ���δ���档\r\n\r\nȷʵҪ�رմ���? ",
    "RegitsterBarcodeDlg",
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
        }

        private void RegisterBarcodeDlg_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        // ��Ŀ��¼·���ı�ǩ˫��
        private void label_biblioRecPath_DoubleClick(object sender, EventArgs e)
        {
            if (this.OpenDetail == null)
                return;

            string[] paths = new string[1];
            paths[0] = ServerUrl + "?" + BiblioRecPath;

            OpenDetailEventArgs args = new OpenDetailEventArgs();
            args.Paths = paths;
            args.OpenNew = true;

            this.label_biblioRecPath.Enabled = false;
            this.OpenDetail(this, args);
            this.label_biblioRecPath.Enabled = true;
        }
    }

    /// <summary>
    /// MARC�������ض��汾
    /// </summary>
    public class MyFilterDocument : FilterDocument
    {
        /// <summary>
        /// �����Ի���
        /// </summary>
        public RegisterBarcodeDlg HostForm = null;
    }

 
}