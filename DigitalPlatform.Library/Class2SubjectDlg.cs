using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SHDocVw;

using System.Runtime.InteropServices;
using System.Collections.Specialized;

using System.Text;
using System.Web;
using System.Threading;
using System.Xml;

using mshtml;

using DigitalPlatform;
using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.GUI;
using DigitalPlatform.Text;


namespace DigitalPlatform.Library
{
    /// <summary>
    /// Summary description for Class2SubjectDlg.
    /// </summary>
    public class Class2SubjectDlg : System.Windows.Forms.Form, DWebBrowserEvents
    {
        SearchPanel SearchPanel = null;
        string ActionPrefix = "http://dp2003.com/";

        /// <summary>
        /// ���������¼�
        /// </summary>
        public event CopySubjectEventHandler CopySubject = null;

        /// <summary>
        /// ����ָ����Form������뷽ʽ
        /// </summary>
        public string FormEncoding = "";	// 

        /// <summary>
        /// �����Form���ύ��URL
        /// </summary>
        public string SubmitUrl = "";

        /// <summary>
        /// �����Form�ύ����-ֵ�Լ���
        /// </summary>
        public NameValueCollection SubmitResult = new NameValueCollection();

        /// <summary>
        /// css��ʽ�ļ�URL
        /// </summary>
        public string CssUrl = "";

        /// <summary>
        /// ����������տ���
        /// </summary>
        public string DbName = "�����������";


        XmlDocument dom = new XmlDocument();

        // UCOMIConnectionPoint
        // System.Runtime.InteropServices.ComTypes.IConnectionPoint
        private System.Runtime.InteropServices.ComTypes.IConnectionPoint icp;
        private int cookie = -1;

        /// <summary>
        /// �ĵ�װ�����
        /// </summary>
        public AutoResetEvent eventDocumentComplete = new AutoResetEvent(false);

        /// <summary>
        /// ���ڹر�
        /// </summary>
        public AutoResetEvent eventWindowClose = new AutoResetEvent(false);

        private AxSHDocVw.AxWebBrowser axWebBrowser;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_queryWord;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox_from;
        private System.Windows.Forms.Button button_search;
        private System.Windows.Forms.Button button_stop;
        private System.Windows.Forms.Label label_message;
        private System.Windows.Forms.Button button_findServerUrl;
        private System.Windows.Forms.TextBox textBox_serverUrl;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel panel_html;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        public Class2SubjectDlg()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            // UCOMIConnectionPointContainer
            // System.Runtime.InteropServices.ComTypes.IConnectionPointContainer
            System.Runtime.InteropServices.ComTypes.IConnectionPointContainer icpc = (System.Runtime.InteropServices.ComTypes.IConnectionPointContainer)axWebBrowser.GetOcx(); // ADDed

            Guid g = typeof(DWebBrowserEvents).GUID;
            icpc.FindConnectionPoint(ref g, out icp);
            icp.Advise(this, out cookie);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Property"></param>
        public void PropertyChange(string Property) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="URL"></param>
        public void NavigateComplete(string URL) { }

        /// <summary>
        /// 
        /// </summary>
        public void WindowActivate() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="Flags"></param>
        /// <param name="TargetFrameName"></param>
        /// <param name="PostData"></param>
        /// <param name="Headers"></param>
        /// <param name="Cancel"></param>
        public void FrameBeforeNavigate(string URL, int Flags, string TargetFrameName, ref object PostData, string Headers, ref bool Cancel) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="Flags"></param>
        /// <param name="TargetFrameName"></param>
        /// <param name="PostData"></param>
        /// <param name="Headers"></param>
        /// <param name="Processed"></param>
        public void NewWindow(string URL, int Flags, string TargetFrameName, ref object PostData, string Headers, ref bool Processed) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="Flags"></param>
        /// <param name="TargetFrameName"></param>
        /// <param name="PostData"></param>
        /// <param name="Headers"></param>
        /// <param name="Processed"></param>
        public void FrameNewWindow(string URL, int Flags, string TargetFrameName, ref object PostData, string Headers, ref bool Processed) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Text"></param>
        public void TitleChange(string Text) { }

        /// <summary>
        /// 
        /// </summary>
        public void DownloadBegin() { }

        /// <summary>
        /// 
        /// </summary>
        public void DownloadComplete()
        {
            eventDocumentComplete.Set();
        }

        /// <summary>
        /// 
        /// </summary>
        public void WindowMove() { }

        /// <summary>
        /// 
        /// </summary>
        public void WindowResize() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Cancel"></param>
        public void Quit(ref bool Cancel) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Progress"></param>
        /// <param name="ProgressMax"></param>
        public void ProgressChange(int Progress, int ProgressMax) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Text"></param>
        public void StatusTextChange(string Text) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Command"></param>
        /// <param name="Enable"></param>
        public void CommandStateChange(int Command, bool Enable) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="URL"></param>
        public void FrameNavigateComplete(string URL) { }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release event sink
                if (-1 != cookie) icp.Unadvise(cookie);
                cookie = -1;
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Class2SubjectDlg));
            this.axWebBrowser = new AxSHDocVw.AxWebBrowser();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_queryWord = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_from = new System.Windows.Forms.ComboBox();
            this.button_search = new System.Windows.Forms.Button();
            this.button_stop = new System.Windows.Forms.Button();
            this.label_message = new System.Windows.Forms.Label();
            this.button_findServerUrl = new System.Windows.Forms.Button();
            this.textBox_serverUrl = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.panel_html = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.axWebBrowser)).BeginInit();
            this.panel_html.SuspendLayout();
            this.SuspendLayout();
            // 
            // axWebBrowser
            // 
            this.axWebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axWebBrowser.Enabled = true;
            this.axWebBrowser.Location = new System.Drawing.Point(0, 0);
            this.axWebBrowser.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axWebBrowser.OcxState")));
            this.axWebBrowser.Size = new System.Drawing.Size(584, 189);
            this.axWebBrowser.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 54);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "������(&Q):";
            // 
            // textBox_queryWord
            // 
            this.textBox_queryWord.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_queryWord.Location = new System.Drawing.Point(149, 50);
            this.textBox_queryWord.Name = "textBox_queryWord";
            this.textBox_queryWord.Size = new System.Drawing.Size(192, 25);
            this.textBox_queryWord.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 89);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = ";��(&F):";
            // 
            // comboBox_from
            // 
            this.comboBox_from.Items.AddRange(new object[] {
            "���",
            "����",
            "�����"});
            this.comboBox_from.Location = new System.Drawing.Point(149, 85);
            this.comboBox_from.Name = "comboBox_from";
            this.comboBox_from.Size = new System.Drawing.Size(192, 23);
            this.comboBox_from.TabIndex = 4;
            this.comboBox_from.Text = "���";
            // 
            // button_search
            // 
            this.button_search.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_search.Location = new System.Drawing.Point(351, 48);
            this.button_search.Name = "button_search";
            this.button_search.Size = new System.Drawing.Size(100, 29);
            this.button_search.TabIndex = 5;
            this.button_search.Text = "����(&S)";
            this.button_search.Click += new System.EventHandler(this.button_search_Click);
            // 
            // button_stop
            // 
            this.button_stop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_stop.Location = new System.Drawing.Point(459, 48);
            this.button_stop.Name = "button_stop";
            this.button_stop.Size = new System.Drawing.Size(100, 29);
            this.button_stop.TabIndex = 6;
            this.button_stop.Text = "ֹͣ(&S)";
            this.button_stop.Click += new System.EventHandler(this.button_stop_Click);
            // 
            // label_message
            // 
            this.label_message.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label_message.Location = new System.Drawing.Point(16, 344);
            this.label_message.Name = "label_message";
            this.label_message.Size = new System.Drawing.Size(592, 29);
            this.label_message.TabIndex = 7;
            // 
            // button_findServerUrl
            // 
            this.button_findServerUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_findServerUrl.Location = new System.Drawing.Point(560, 13);
            this.button_findServerUrl.Name = "button_findServerUrl";
            this.button_findServerUrl.Size = new System.Drawing.Size(43, 29);
            this.button_findServerUrl.TabIndex = 11;
            this.button_findServerUrl.Text = "...";
            this.button_findServerUrl.Click += new System.EventHandler(this.button_findServerUrl_Click);
            // 
            // textBox_serverUrl
            // 
            this.textBox_serverUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_serverUrl.Location = new System.Drawing.Point(149, 15);
            this.textBox_serverUrl.Name = "textBox_serverUrl";
            this.textBox_serverUrl.Size = new System.Drawing.Size(406, 25);
            this.textBox_serverUrl.TabIndex = 10;
            this.textBox_serverUrl.TextChanged += new System.EventHandler(this.textBox_serverUrl_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 19);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(84, 15);
            this.label3.TabIndex = 9;
            this.label3.Text = "������(&S):";
            // 
            // panel_html
            // 
            this.panel_html.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_html.Controls.Add(this.axWebBrowser);
            this.panel_html.Location = new System.Drawing.Point(19, 134);
            this.panel_html.Name = "panel_html";
            this.panel_html.Size = new System.Drawing.Size(584, 189);
            this.panel_html.TabIndex = 12;
            // 
            // Class2SubjectDlg
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(8, 18);
            this.ClientSize = new System.Drawing.Size(619, 385);
            this.Controls.Add(this.panel_html);
            this.Controls.Add(this.button_findServerUrl);
            this.Controls.Add(this.textBox_serverUrl);
            this.Controls.Add(this.textBox_queryWord);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label_message);
            this.Controls.Add(this.button_stop);
            this.Controls.Add(this.button_search);
            this.Controls.Add(this.comboBox_from);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Class2SubjectDlg";
            this.Text = "�����������";
            this.Load += new System.EventHandler(this.Class2SubjectDlg_Load);
            this.Closed += new System.EventHandler(this.Class2SubjectDlg_Closed);
            ((System.ComponentModel.ISupportInitialize)(this.axWebBrowser)).EndInit();
            this.panel_html.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion


        /// <summary>
        /// HTML�ַ���
        /// </summary>
        public string HtmlString
        {
            get
            {
                IHTMLDocument2 doc = (IHTMLDocument2)this.axWebBrowser.Document;

                if (doc == null)
                    return "";

                IHTMLElementCollection htmls = (IHTMLElementCollection)doc.all.tags("html");
                IHTMLElement item = (IHTMLElement)htmls.item(null, 0);
                if (item == null)
                    return "";

                return item.outerHTML;

                /*

                if (doc.body == null)
                    return "";

                IHTMLElement html = (IHTMLElement)doc.body.parentElement;
                return html.outerHTML;
                */
            }
            set
            {

                IHTMLDocument2 doc = this.DocStream;

                Debug.Assert(doc != null, "DocStreamӦ���ط�null");

                doc.clear();
                doc.write(new object[] { value });
                doc.close();

            }
        }

        /// <summary>
        /// �õ�Document���󣬿�����write()д��
        /// </summary>
        public IHTMLDocument2 DocStream
        {
            get
            {
                // return ((mshtml.HTMLDocumentClass)this.axWebBrowser.Document);
                IHTMLDocument2 doc = (IHTMLDocument2)this.axWebBrowser.Document;

                if (doc == null)
                {
                    object empty = System.Reflection.Missing.Value;
                    axWebBrowser.Navigate("about:blank",
                        ref empty,
                        ref empty,
                        ref empty,
                        ref empty);
                    doc = (IHTMLDocument2)this.axWebBrowser.Document;
                }

                return doc;
            }
        }

        /// <summary>
        /// ������URL
        /// </summary>
        public string ServerUrl
        {
            get
            {
                return this.textBox_serverUrl.Text;
            }
            set
            {
                textBox_serverUrl.Text = value;

                if (this.SearchPanel != null)
                    this.SearchPanel.ServerUrl = value;
            }
        }

        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="searchpanel">�������</param>
        /// <param name="strServerUrl">������URL</param>
        /// <param name="strDbName">����������տ���</param>
        public void Initial(
            SearchPanel searchpanel,
            /*ServerCollection servers,
            CfgCache cfgcache,*/
            string strServerUrl,
            string strDbName)
        {
            /*
            this.Servers = servers;
            this.Channels.procAskAccountInfo = 
                new Delegate_AskAccountInfo(this.Servers.AskAccountInfo);

            this.cfgCache = cfgcache;
            */
            this.SearchPanel = searchpanel;

            this.SearchPanel.InitialStopManager(this.button_stop,
                this.label_message);

            this.ServerUrl = strServerUrl;

            if (strDbName != null)
                this.DbName = strDbName;

            FormEncoding = "utf-8";
        }

        private void button_search_Click(object sender, System.EventArgs e)
        {

            string strError = "";
            // string strXml = "";

            XmlDocument tempdom = null;

            // ����ʵ�ÿ�
            int nRet = 0;

            this.SearchPanel.BeginLoop("���ڼ��� " + this.textBox_queryWord.Text + " ...");

            EnableControls(false);

            this.Update();

            try
            {
                nRet = this.SearchPanel.SearchUtilDb(
                    this.DbName,
                    this.comboBox_from.Text,
                    this.textBox_queryWord.Text,
                    out tempdom,
                    out strError);
            }
            finally
            {
                EnableControls(true);
                this.SearchPanel.EndLoop();
            }
            /*
            int nRet = Util.SearchUtilDb(
                this.SearchPanel.Channels,
                this.SearchPanel.ServerUrl,
                this.DbName,
                this.comboBox_from.Text,
                this.textBox_queryWord.Text,
                out strXml,
                out strError);
            */
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                strError = "δ����";
                goto ERROR1;
            }

            // this.HtmlString = HttpUtility.HtmlEncode(strXml);

            this.dom = tempdom;

            string strHtml = "";

            nRet = MakeHtmlPage(dom,
                out strHtml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.SearchPanel.BeginLoop("����װ�� ...");
            this.HtmlString = strHtml;
            this.SearchPanel.EndLoop();

            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        string RefinementText(string strRefinement,
            string strText)
        {
            if (strRefinement == "disable")
                return "{" + strText + "}";
            if (strRefinement == "seeAlso")
                return "[" + strText + "]";

            return strText;
        }

        int MakeHtmlPage(XmlDocument dom,
            out string strHtml,
            out string strError)
        {
            strError = "";


            strHtml = "";
            strHtml += "<html>";
            strHtml += "<head>";
            // strHtml += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">";
            if (CssUrl != "")
            {
                strHtml += "<LINK href='" + this.CssUrl + "' type='text/css' rel='stylesheet'>";
            }

            strHtml += "</head>";

            strHtml += "<body>";

            strHtml += "<form method='post' action='" + this.ActionPrefix + "copymulti' >";

            XmlNamespaceManager mngr = new XmlNamespaceManager(dom.NameTable);
            mngr.AddNamespace("dprms", "http://dp2003.com/dprms");
            mngr.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            mngr.AddNamespace("rdfs", "http://www.w3.org/2000/01/rdf-schema#");
            // mngr.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
            mngr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            mngr.AddNamespace("c2s", "http://dp2003.com/c2s");


            // �ϼ����
            //    <rdf:Description rdf:about="broaderTerm">
            //        <subject>
            //            <rdf:value>A</rdf:value>
            //        </subject>
            //    </rdf:Description>
            /*
	<rdf:Description rdf:about="broaderTerm">
		<item>
			<subject>��λ�����</subject>
			<rdfs:label>��λ����ű�ǩ����������</rdfs:label>
			<rdfs:label xml:lang="en">��λ����ű�ǩ��Ӣ��</rdfs:label>
		</item>
	</rdf:Description>
             */

            XmlNode node = null;

            node = dom.DocumentElement.SelectSingleNode(
                    "rdf:Description[@rdf:about='broaderTerm']/c2s:item",
                    mngr);
            if (node != null)
            {
                XmlNode child = node.SelectSingleNode("rdfs:label", mngr);

                string strTitle = DomUtil.GetNodeText(child);

                child = node.SelectSingleNode("c2s:subject", mngr);

                string strName = DomUtil.GetNodeText(child);

                strHtml += "<p class='parentlink'>";
                string strUrl = this.ActionPrefix + "navi/" + strName;
                strHtml += "�ϼ���: <a class='parentlink' href='" + strUrl + "' >" + strName + "</a>" + " <span class='parentclassname'>" + strTitle + "</span></div>";

                // strHtml += "<div class='childrenclassline'><a class='childrenclasslink' href='" + strUrl + "' >" + strName + "</a>" + " <span class='childrenclassname'>" + strTitle + "</span></div>";


                strHtml += "</p>";

                strHtml += "<table width='100%' cellspacing='1' cellpadding='8' border='0'>";

            }

            /*
			string strParent = DomUtil.GetElementText(dom.DocumentElement,
                "rdf:Description[@rdf:about='broaderTerm']/c2s:item/c2s:subject",
				mngr);
             */



            // ���
            //    <rdf:Description rdf:about="entry">
            //        <subject>
            //            <rdf:value>A1</rdf:value>
            //            <rdfs:label>���˼������˹����</rdfs:label>
            //        </subject>
            //        <description>ȫ����ˡ�</description>
            //    </rdf:Description>
            /*
	<rdf:Description rdf:about="entry">
		<item>
			<subject>��ͼ�������</subject>
			<rdfs:label>��ͼ�������ǩ����������</rdfs:label>
			<rdfs:label xml:lang="en">��ͼ�������ǩ��Ӣ��</rdfs:label>
		</item>
		<description>��ͼ�����฽ע˵��</description>
	</rdf:Description>
             */
            node = dom.DocumentElement.SelectSingleNode(
                "rdf:Description[@rdf:about='entry']/c2s:item/c2s:subject",
                mngr);
            strHtml += "<tr class='classnumber'>";
            strHtml += "<td class='classnumbertitle' valign='bottom' nowrap align='right' width='20%'>���</td>";

            strHtml += "<td class='classnumbertext' width='80%'>";

            if (node != null)
            {
                string strRefinement = DomUtil.GetAttr(node, "refinement");
                string strText = RefinementText(strRefinement, DomUtil.GetNodeText(node));
                strHtml += strText;
            }

            strHtml += "</td>";
            strHtml += "</tr>";

            // ����
            node = dom.DocumentElement.SelectSingleNode(
                "rdf:Description[@rdf:about='entry']/c2s:subject/rdfs:label", mngr);
            strHtml += "<tr class='classname'>";
            strHtml += "<td class='classnametitle'  nowrap align='right' width='20%'>����</td>";

            strHtml += "<td class='classnametext' width='80%'>";

            if (node != null)
                strHtml += DomUtil.GetNodeText(node);

            strHtml += "</td>";
            strHtml += "</tr>";


            // --
            strHtml += "<tr class='seperator'>";
            strHtml += "<td class='seperator' colspan='2'></td>";
            strHtml += "</tr>";

            int i = 0;

            // ���������
            //    <rdf:Description rdf:about="CTofLabel">
            //        <subject>
            //            <rdf:value>�������</rdf:value>
            //        </subject>
            //	  </rdf:Description>
            /*
	<rdf:Description rdf:about="entryDescriptor">
		<item>
			<subject>������Ӧ���������(�ú�����ʾ)</subject>
			<subject refinement="subdivide">ѧ�Ƹ��������</subject>
			<subject refinement="geographic">�������������</subject>
			<subject refinement="chronological">������������</subject>
			<subject refinement="ethnic" rdf:resource="ethnic" />
			<!--
			ǰ��Ĵ��޶��ʵ�subjectֵ���á�-�����ַ������������ж�refinement����ֵΪ��
			��subdivide��geographic��chronological��chronological��ethnic��
			���á�-�����ڲ���refinement���Ե�subjectֵ֮��
			-->
			<subject refinement="modifier">��������δ�</subject>
			<!--
			ǰ��Ĵ��޶��ʵ�subjectֵ���á�,���ַ������������ж�refinement����ֵΪ��
			��modifier��ʱ���á�,�����ڲ���refinement���Ե�subjectֵ֮��
			-->
			<subject refinement="bond">���������</subject>
			<!--
			ǰ��Ĵ��޶��ʵ�subjectֵ���á�:���ַ������������ж�refinement����ֵΪ��
			��bond��ʱ���á�:�����ڲ���refinement���Ե�subjectֵ֮��
			-->
		</item>
	</rdf:Description>
             */
            XmlNodeList nodes = dom.DocumentElement.SelectNodes(
                "rdf:Description[@rdf:about='entryDescriptor']/c2s:item",
                mngr);
            strHtml += "<tr class='subject'>";
            strHtml += "<td class='subjecttitle'  nowrap align='right'>�����</td>";

            strHtml += "<td class='subjecttext'>";
            for (i = 0; i < nodes.Count; i++)
            {
                node = nodes[i];


                string strText = "";
                string strTempHtml = "";
                // string strText = DomUtil.GetNodeText(node);
                GetSubjectText(node,
                    mngr,
                    out strText,
                    out strTempHtml);

                string strSelectUrl = this.ActionPrefix + "copyone/" + HttpUtility.UrlEncode(strText);

                string strImg = "<img src='" + Environment.CurrentDirectory + "/copy.gif" + "' border='0'>";

                // strHtml += "<div class='relatesubjectline'><input class='checkbox' name='subject' type='checkbox' value='" + DomUtil.GetNodeText(node) + "'/>" + " <a href='" + strSelectUrl + "' alt='ѡ�������" + strText + "'>" + strImg + "</a>" + strTempHtml + "</div>";

                strHtml += "<div class='relatesubjectline'><input class='checkbox' name='subject' type='checkbox' value='" +node.InnerText + "'/>" + " <a href='" + strSelectUrl + "' alt='ѡ�������" + strText + "'>" + strImg + "</a>" + strTempHtml + "</div>";


            }
            strHtml += "</td>";
            strHtml += "</tr>";

            // ������������

            nodes = dom.DocumentElement.SelectNodes(
                "rdf:Description[@rdf:about='noteDescriptor']/c2s:item",
                mngr);
            strHtml += "<tr class='relatesubject'>";
            strHtml += "<td class='relatesubjecttitle'  nowrap align='right'>���������</td>";

            strHtml += "<td class='relatesubjecttext'>";
            for (i = 0; i < nodes.Count; i++)
            {
                node = nodes[i];

                string strText = "";
                string strTempHtml = "";
                // string strText = DomUtil.GetNodeText(node);
                GetSubjectText(node,
                    mngr,
                    out strText,
                    out strTempHtml);

                string strSelectUrl = this.ActionPrefix + "copyone/" + HttpUtility.UrlEncode(strText);

                string strImg = "<img src='" + Environment.CurrentDirectory + "/copy.gif" + "' border='0'>";


                // strHtml += "<div class='relatesubjectline'><input class='checkbox' name='subject' type='checkbox' value='" + DomUtil.GetNodeText(node) + "'/>" + " <a href='" + strSelectUrl + "' alt='ѡ�������" + strText + "'>" + strImg + "</a>" + strTempHtml + "</div>";

                strHtml += "<div class='relatesubjectline'><input class='checkbox' name='subject' type='checkbox' value='" + node.InnerText + "'/>" + " <a href='" + strSelectUrl + "' alt='ѡ�������" + strText + "'>" + strImg + "</a>" + strTempHtml + "</div>";   // ?

            }
            strHtml += "</td>";
            strHtml += "</tr>";

            // ���ť
            strHtml += "<tr class='command'>";
            strHtml += "<td class='command' colspan='2'>";
            strHtml += "<input type='submit' value='ѡ�ô򹴵������' />";
            strHtml += "</td></tr>";


            // --
            strHtml += "<tr class='seperator'>";
            strHtml += "<td class='seperator' colspan='2'></td>";
            strHtml += "</tr>";


            // �¼���
            /*
                <rdf:Description rdf:about="narrowerTerm">
                    <subject>
                        <rdf:value>A11</rdf:value>
                        <rdfs:label>ѡ�����ļ�</rdfs:label>
                    </subject>
                    ...
            */
            /*
    <rdf:Description rdf:about="narrowerTerm">
		<item>
			<subject>��λ�����</subject>
			<rdfs:label>��λ����ű�ǩ</rdfs:label>
			<rdfs:label xml:lang="en">��λ����ű�ǩ��Ӣ��</rdfs:label>
		</item>
		<item>
			<subject refinement="disable">��λ�����(ͣ��)</subject>
			<rdfs:label>��λ����ű�ǩ</rdfs:label>
			<rdfs:label xml:lang="en">��λ����ű�ǩ��Ӣ��</rdfs:label>
		</item>
	</rdf:Description>
             */

            nodes = dom.DocumentElement.SelectNodes(
                "rdf:Description[@rdf:about='narrowerTerm']/c2s:item",
                mngr);
            strHtml += "<tr class='childrenclass'>";
            strHtml += "<td class='childrenclasstitle'  nowrap align='right'>�¼���</td>";

            strHtml += "<td class='childrenclasstext'>";
            for (i = 0; i < nodes.Count; i++)
            {
                node = nodes[i];

                XmlNode child = node.SelectSingleNode("rdfs:label", mngr);

                string strTitle = DomUtil.GetNodeText(child);

                child = node.SelectSingleNode("c2s:subject", mngr);

                string strName = DomUtil.GetNodeText(child);


                string strUrl = this.ActionPrefix + "navi/" + strName;

                strHtml += "<div class='childrenclassline'><a class='childrenclasslink' href='" + strUrl + "' >" + strName + "</a>" + " <span class='childrenclassname'>" + strTitle + "</span></div>";

            }
            strHtml += "</td>";
            strHtml += "</tr>";

            strHtml += "</table>";

            strHtml += "</form>";
            strHtml += "</body></html>";


            return 0;
        }


        /*
        <subject>
            <rdf:value>���˼����</rdf:value>
            <c2s:subdivide c2s:refinement="">
                <rdf:value>�ļ�</rdf:value>
            </c2s:subdivide>
        </subject>
        */
        /*
		<item>
			<subject>������Ӧ���������(�ú�����ʾ)</subject>
			<subject refinement="subdivide">ѧ�Ƹ��������</subject>
			<subject refinement="geographic">�������������</subject>
			<subject refinement="chronological">������������</subject>
			<subject refinement="ethnic" rdf:resource="ethnic" />
			<!--
			ǰ��Ĵ��޶��ʵ�subjectֵ���á�-�����ַ������������ж�refinement����ֵΪ��
			��subdivide��geographic��chronological��chronological��ethnic��
			���á�-�����ڲ���refinement���Ե�subjectֵ֮��
			-->
			<subject refinement="modifier">��������δ�</subject>
			<!--
			ǰ��Ĵ��޶��ʵ�subjectֵ���á�,���ַ������������ж�refinement����ֵΪ��
			��modifier��ʱ���á�,�����ڲ���refinement���Ե�subjectֵ֮��
			-->
			<subject refinement="bond">���������</subject>
			<!--
			ǰ��Ĵ��޶��ʵ�subjectֵ���á�:���ַ������������ж�refinement����ֵΪ��
			��bond��ʱ���á�:�����ڲ���refinement���Ե�subjectֵ֮��
			-->
		</item>
         */
        void GetSubjectText(XmlNode node,
            XmlNamespaceManager mngr,
            out string strPureText,
            out string strHtml)
        {
            strHtml = "";
            strPureText = "";
            XmlNodeList nodes = node.SelectNodes("./c2s:subject", mngr);

            string strImg = "<img src='" + Environment.CurrentDirectory + "/copyfirst.gif" + "' border='0'>";

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode cur = nodes[i];

                XmlNode parent = cur.ParentNode;

                string strRefinement = DomUtil.GetAttr(cur, "refinement");

                if (String.IsNullOrEmpty(strRefinement) == true)
                {
                    string strText = RefinementText(strRefinement,
                        DomUtil.GetNodeText(cur));

                    string strSelectUrl = this.ActionPrefix + "copyone/" + HttpUtility.UrlEncode(strText);




                    // ������
                    strHtml += strText;

                    if (nodes.Count > 1)
                        strHtml += "<a href='" + strSelectUrl + "' alt='ѡ�������" + strText + "'>" + strImg + "</a>";
                    else
                    {
                    }


                    strPureText += strText;
                }
                else
                {
                    // ��������
                    strHtml += "-" + DomUtil.GetNodeText(cur);

                    strPureText += "-" + DomUtil.GetNodeText(cur);
                }
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="flags"></param>
        /// <param name="targetFrame"></param>
        /// <param name="postData"></param>
        /// <param name="headers"></param>
        /// <param name="Cancel"></param>
        public void BeforeNavigate(string url,
            int flags,
            string targetFrame,
            ref object postData,
            string headers,
            ref bool Cancel)
        {

            string strText = "url=" + url + ", headers=" + headers;

            // MessageBox.Show(this, strText);

            // ׼��SubmitResult��Ϣ
            string strEncoding = "";    //  ((mshtml.HTMLDocumentClass)this.axWebBrowser.Document).charset; // ���������XP��IE�ؼ���ת�쳣���������

            // MessageBox.Show(this, "1");


            SubmitUrl = url;
            GetFormData((byte[])postData, strEncoding);


            // MessageBox.Show(this, "2");


            if (url.Length < this.ActionPrefix.Length)
            {
                // MessageBox.Show(this, "return");
                return;
            }

            // MessageBox.Show(this, "3");


            string strLead = url.Substring(0, this.ActionPrefix.Length);

            // MessageBox.Show(this, "strLead='" + strLead + "' ActionPrefix='" + this.ActionPrefix + "'");


            // Ԥ���������
            if (String.Compare(strLead, this.ActionPrefix, true) == 0)
            {
                string strVerb = "";
                string strParam = "";
                GetCommand(
                    url,
                    out strVerb,
                    out strParam);

                if (strVerb == "navi")
                    DoNavi(strParam);
                else if (strVerb == "copymulti")
                    DoCopyMulti();
                else if (strVerb == "copyone")
                    DoCopyOne(strParam);


                Cancel = true;
                // MessageBox.Show(this, "cancel = true");
                return;
            }

        }

        // ���Ʋ�����
        void DoCopyMulti()
        {
            /*
            MessageBox.Show(this, this.SubmitResult["subject"]);
            if (ExitAfterCopy == true)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            */
            if (this.CopySubject != null)
            {
                CopySubjectEventArgs e = new CopySubjectEventArgs();
                e.Subject = this.SubmitResult["subject"];
                e.Single = false;
                this.CopySubject(this, e);
            }

        }

        // ���Ʋ�����
        void DoCopyOne(string strParam)
        {
            strParam = HttpUtility.UrlDecode(strParam);
            SubmitResult.Add("subject", strParam);

            /*
            MessageBox.Show(this, this.SubmitResult["subject"]);
            if (ExitAfterCopy == true) {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            */
            if (this.CopySubject != null)
            {
                CopySubjectEventArgs e = new CopySubjectEventArgs();
                e.Subject = strParam;
                e.Single = true;
                this.CopySubject(this, e);
            }

        }

        void GetCommand(
            string strUrlCmd,
            out string strVerb,
            out string strParam)
        {
            strVerb = "";
            strParam = "";

            string strText = strUrlCmd.Substring(this.ActionPrefix.Length);

            if (strText.Length == 0)
                return;

            if (strText[strText.Length - 1] == '/')
                strText = strText.Remove(strText.Length - 1, 1);

            int nRet = strText.IndexOf("/");
            if (nRet == -1)
            {
                strVerb = strText;
            }
            else
            {
                strVerb = strText.Substring(0, nRet);
                strParam = strText.Substring(nRet + 1);
            }


        }

        /// <summary>
        /// ��ת��ĳ�����
        /// </summary>
        /// <param name="strKey">���</param>
        public void DoNavi(string strKey)
        {
            string strError = "";
            XmlDocument tempdom = null;

            // ����ʵ�ÿ�
            int nRet = 0;

            this.SearchPanel.BeginLoop("���ڼ��� " + strKey + " ...");
            try
            {
                nRet = this.SearchPanel.SearchUtilDb(
                    this.DbName,
                    "���",
                    strKey,
                    out tempdom,
                    out strError);
            }
            finally
            {
                this.SearchPanel.EndLoop();
            }

            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                strError = "δ����";
                goto ERROR1;
            }

            this.dom = tempdom;

            string strHtml = "";

            nRet = MakeHtmlPage(dom,
                out strHtml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.HtmlString = strHtml;


            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void Class2SubjectDlg_Load(object sender, System.EventArgs e)
        {
            /*
            if (ap != null) 
            {
                if (ApCfgTitle != "" && ApCfgTitle != null) 
                {
                    ap.LoadFormStates(this,
                        ApCfgTitle);
                }
                else 
                {
                    Debug.Assert(true, "��Ҫ��ap����ͻָ��������״̬������������ApCfgTitle��Ա");
                }

            }
            */
            if (this.SearchPanel != null)
                this.SearchPanel.SaveFormStates(this);

        }

        private void Class2SubjectDlg_Closed(object sender, System.EventArgs e)
        {
            /*
            if (ap != null) 
            {
                if (ApCfgTitle != "" && ApCfgTitle != null) 
                {
                    ap.SaveFormStates(this,
                        ApCfgTitle);
                }
                else 
                {
                    Debug.Assert(true, "��Ҫ��ap����ͻָ��������״̬������������ApCfgTitle��Ա");
                }

            }
            */
            if (this.SearchPanel != null)
                this.SearchPanel.LoadFormStates(this);
        }


        void GetFormData(byte[] data,
            string strEncoding)
        {
            SubmitResult.Clear();

            if (data == null)
                return;

            Encoding encoding = null;

            try
            {
                if (this.FormEncoding == "")
                    encoding = Encoding.GetEncoding(strEncoding);
                else
                    encoding = Encoding.GetEncoding(FormEncoding);
            }
            catch (NotSupportedException)
            {
                encoding = Encoding.ASCII;
            }

            byte[] data1 = null;

            // ȥ��ĩβ��0
            if (data.Length > 1 && data[data.Length - 1] == 0)
            {
                data1 = new byte[data.Length - 1];
                Array.Copy(data, 0, data1, 0, data.Length - 1);
            }
            else
                data1 = data;

            string strData = encoding.GetString(data1);

            // �и� &
            string[] saItem = strData.Split(new Char[] { '&' });

            for (int i = 0; i < saItem.Length; i++)
            {
                // �и�'='
                int nRet = saItem[i].IndexOf('=', 0);
                if (nRet == -1)
                    continue;	// invalid item
                string strName = saItem[i].Substring(0, nRet);
                string strValue = saItem[i].Substring(nRet + 1);

                // ����
                strName = HttpUtility.UrlDecode(strName,
                    encoding);
                strValue = HttpUtility.UrlDecode(strValue,
                    encoding);

                SubmitResult.Add(strName, strValue);
            }

        }

        private void textBox_serverUrl_TextChanged(object sender, System.EventArgs e)
        {
            if (this.SearchPanel != null)
                this.SearchPanel.ServerUrl = this.textBox_serverUrl.Text;
        }

        private void button_stop_Click(object sender, System.EventArgs e)
        {
            if (this.SearchPanel != null)
                this.SearchPanel.DoStopClick();
        }

        private void button_findServerUrl_Click(object sender, EventArgs e)
        {
            OpenResDlg dlg = new OpenResDlg();

            dlg.Text = "��ѡ�������";
            dlg.EnabledIndices = new int[] { ResTree.RESTYPE_SERVER };
            dlg.Path = textBox_serverUrl.Text;
            dlg.Initial(this.SearchPanel.Servers,
                this.SearchPanel.Channels);
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.textBox_serverUrl.Text = dlg.Path;
        }

        void EnableControls(bool bEnable)
        {
            this.button_search.Enabled = bEnable;
            if (bEnable == true)
                this.button_stop.Enabled = false;
            else
                this.button_stop.Enabled = true;

            this.button_findServerUrl.Enabled = bEnable;

            this.textBox_queryWord.Enabled = bEnable;
            this.textBox_serverUrl.Enabled = bEnable;
            this.comboBox_from.Enabled = bEnable;
        }
    }


    /// <summary>
    /// ���������
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void CopySubjectEventHandler(object sender,
    CopySubjectEventArgs e);

    /// <summary>
    /// ����������¼�����
    /// </summary>
    public class CopySubjectEventArgs : EventArgs
    {
        /// <summary>
        /// �����
        /// </summary>
        public string Subject = "";

        /// <summary>
        /// �Ƿ�Ϊ����һ����
        /// </summary>
        public bool Single = false;
    }
}
