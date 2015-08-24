using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Web;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Text;
using DigitalPlatform.DTLP;
using DigitalPlatform.Drawing;  // ColorUtil
using DigitalPlatform.Marc;
using DigitalPlatform.Script;
using DigitalPlatform.UnionCatalogClient;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.CommonControl;

using DigitalPlatform.CirculationClient;
using DigitalPlatform.GcatClient;
using DigitalPlatform.GcatClient.gcat_new_ws;

namespace dp2Catalog
{
    public partial class MarcDetailForm : Form
    {
        SelectedTemplateCollection selected_templates = new SelectedTemplateCollection();

        MacroUtil m_macroutil = new MacroUtil();   // �괦����

        // �洢��Ŀ��<dprms:file>���������XMLƬ��
        XmlDocument domXmlFragment = null;

        VerifyViewerForm m_verifyViewer = null;

        public LoginInfo LoginInfo = new LoginInfo();

        LinkMarcFile linkMarcFile = null;   // �����Ϊnull����ʾ������״̬

        const int WM_LOADSIZE = API.WM_USER + 201;
        const int WM_VERIFY_DATA = API.WM_USER + 204;
        const int WM_FILL_MARCEDITOR_SCRIPT_MENU = API.WM_USER + 205;

        public MainForm MainForm = null;

        public DigitalPlatform.Stop stop = null;

        public ISearchForm LinkedSearchForm = null;

        public long RecordVersion = 0; // ��ǰ��¼���޸ĺ�汾�š�0 ��ʾ��δ�޸Ĺ�

        // ��Z39.50����������������ԭʼ��¼
        DigitalPlatform.Z3950.Record m_currentRecord = null;
        public DigitalPlatform.Z3950.Record CurrentRecord
        {
            get
            {
                return this.m_currentRecord;
            }
            set
            {
                this.m_currentRecord = value;

                // ��ʾCtrl+A�˵�
                if (this.MainForm.PanelFixedVisible == true
                    && value != null)   // 2013/6/5
                    this.AutoGenerate(this.MarcEditor,
                        new GenerateDataEventArgs(),
                    true);
            }
        }

        Encoding CurrentEncoding = Encoding.GetEncoding(936);

        public bool UseAutoDetectedMarcSyntaxOID = false;

        string m_strAutoDetectedMarcSyntaxOID = "";
        private string AutoDetectedMarcSyntaxOID
        {
            get
            {
                return this.m_strAutoDetectedMarcSyntaxOID;
            }
            set
            {
                this.m_strAutoDetectedMarcSyntaxOID = value;
            }
        }

        byte[] CurrentTimestamp = null;

        // ���ڱ����¼��·��
        public string SavePath
        {
            get
            {
                return this.textBox_savePath.Text;
            }
            set
            {
                this.textBox_savePath.Text = value;

                // ��ʾCtrl+A�˵�
                if (this.MainForm.PanelFixedVisible == true)
                    this.AutoGenerate(this.MarcEditor,
                        new GenerateDataEventArgs(),
                    true);
            }
        }

        // (C#�ű�ʹ��)
        // ��Ŀ��¼·�� ���� "����ͼ��/1"
        public string ServerName
        {
            get
            {
                string strError = "";
                string strProtocol = "";
                string strPath = "";
                int nRet = Global.ParsePath(this.SavePath,
                    out strProtocol,
                    out strPath,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);

                // TODO: Ҫ���ֲ�ͬ��protocol������ȷ����

                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                return strServerName;
            }
        }

        // (C#�ű�ʹ��)
        // ��Ŀ��¼·�� ���� "����ͼ��/1"
        public string BiblioRecPath
        {
            get
            {
                string strError = "";
                string strProtocol = "";
                string strPath = "";
                int nRet = Global.ParsePath(this.SavePath,
                    out strProtocol,
                    out strPath,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);

                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                return strLocalPath;
            }
        }

        // (C#�ű�ʹ��)
        // ��Ŀ����
        public string BiblioDbName
        {
            get
            {
                string strError = "";
                string strProtocol = "";
                string strPath = "";
                int nRet = Global.ParsePath(this.SavePath,
                    out strProtocol,
                    out strPath,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);

                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                string strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);

                return strBiblioDbName;
            }
        }

        public MarcDetailForm()
        {
            InitializeComponent();

            this.MarcEditor.Changed = false;    // ��Ϊ���̬��this.MarcEditor.Marc��������һ�Σ�changed�޸���
            this.RecordVersion = 0;
        }

        private void MarcDetailForm_Load(object sender, EventArgs e)
        {
            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������

            Global.FillEncodingList(this.comboBox_originDataEncoding,
                true);
            /*
            // ����MARC8
            this.comboBox_originDataEncoding.Items.Add("MARC-8");
             * */

            this.MarcEditor.AppInfo = this.MainForm.AppInfo;
            LoadFontToMarcEditor();

            this.m_macroutil.ParseOneMacro -= new ParseOneMacroEventHandler(m_macroutil_ParseOneMacro);
            this.m_macroutil.ParseOneMacro += new ParseOneMacroEventHandler(m_macroutil_ParseOneMacro);

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);

            string strSelectedTemplates = this.MainForm.AppInfo.GetString(
    "marcdetailform",
    "selected_templates",
    "");
            if (String.IsNullOrEmpty(strSelectedTemplates) == false)
            {
                selected_templates.Build(strSelectedTemplates);
            }

#if NO
            this.m_strPinyinGcatID = this.MainForm.AppInfo.GetString("entity_form", "gcat_pinyin_api_id", "");
            this.m_bSavePinyinGcatID = this.MainForm.AppInfo.GetBoolean("entity_form", "gcat_pinyin_api_saveid", false);
#endif
        }

        private void MarcDetailForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (stop != null)
            {
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }
            }

            if (/*this.EntitiesChanged == true
                || this.IssuesChanged == true
                || this.ObjectChanged == true
                || */this.BiblioChanged == true
                )
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ�� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
    "MarcDetailForm",
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

        // ������Ϣ�Ƿ񱻸ı�
        public bool ObjectChanged
        {
            get
            {
                /*
                if (this.binaryResControl1 != null)
                    return this.binaryResControl1.Changed;

                return false;
                 * */
                return false;
            }
            set
            {
                /*
                if (this.binaryResControl1 != null)
                    this.binaryResControl1.Changed = value;
                 * */
            }
        }

        // ��Ŀ��Ϣ�Ƿ񱻸ı�
        public bool BiblioChanged
        {
            get
            {
                if (this.MarcEditor != null)
                {
                    /*
                    // ���object id�����ı䣬��ô����MARCû�иı䣬�����ĺϳ�XMLҲ�����˸ı�
                    if (this.binaryResControl1 != null)
                    {
                        if (this.binaryResControl1.IsIdChanged() == true)
                            return true;
                    }
                    */

                    return this.MarcEditor.Changed;
                }

                return false;
            }
            set
            {
                if (this.MarcEditor != null)
                    this.MarcEditor.Changed = value;

                if (value == false)
                {
                    this.RecordVersion = 0;
                }
            }
        }

        // ��õ�ǰ���޸ı�־�Ĳ��ֵ�����
        string GetCurrentChangedPartName()
        {
            string strPart = "";

            if (this.BiblioChanged == true)
                strPart += "��Ŀ��Ϣ";

            /*
            if (this.EntitiesChanged == true)
            {
                if (strPart != "")
                    strPart += "��";
                strPart += "����Ϣ";
            }

            if (this.IssuesChanged == true)
            {
                if (strPart != "")
                    strPart += "��";
                strPart += "����Ϣ";
            }

            if (this.ObjectChanged == true)
            {
                if (strPart != "")
                    strPart += "��";
                strPart += "������Ϣ";
            }
             * */

            return strPart;
        }

        private void MarcDetailForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }

            SaveSize();

            if (this.m_genDataViewer != null)
                this.m_genDataViewer.Close();

            if (this.m_verifyViewer != null)
                this.m_verifyViewer.Close();

            if (this.linkMarcFile != null)
                this.linkMarcFile.Close();

#if NO
            if (this.m_bSavePinyinGcatID == false)
                this.m_strPinyinGcatID = "";
            this.MainForm.AppInfo.SetString("entity_form", "gcat_pinyin_api_id", this.m_strPinyinGcatID);
            this.MainForm.AppInfo.GetBoolean("entity_form", "gcat_pinyin_api_saveid", this.m_bSavePinyinGcatID);
#endif
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                string strSelectedTemplates = selected_templates.Export();
                this.MainForm.AppInfo.SetString(
                    "marcdetailform",
                    "selected_templates",
                    strSelectedTemplates);
            }
        }

        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_FILL_MARCEDITOR_SCRIPT_MENU:
                    // ��ʾCtrl+A�˵�
                    if (this.MainForm.PanelFixedVisible == true)
                        this.AutoGenerate(this.MarcEditor,
                            new GenerateDataEventArgs(),
                            true);
                    return;
                case WM_LOADSIZE:
                    LoadSize();
                    return;
                case WM_VERIFY_DATA:
                    {
                        GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                        e1.FocusedControl = this.MarcEditor;

                        this.VerifyData(this, e1, null, true);
                        return;
                    }
            }
            base.DefWndProc(ref m);
        }

        public string UiState
        {
            get
            {
                List<object> controls = new List<object>();
                controls.Add(this.tabControl_main);
                controls.Add(this.splitContainer_originDataMain);
                return GuiState.GetUiState(controls);
            }
            set
            {
                List<object> controls = new List<object>();
                controls.Add(this.tabControl_main);
                controls.Add(this.splitContainer_originDataMain);
                GuiState.SetUiState(controls, value);
            }
        }


        public void LoadSize()
        {
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state",
                MainForm.DefaultMdiWindowWidth,
                MainForm.DefaultMdiWindowHeight);

#if NO
            // ���splitContainer_originDataMain��״̬
            int nValue = MainForm.AppInfo.GetInt(
            "marcdetailform",
            "splitContainer_originDataMain",
            -1);
            if (nValue != -1)
                this.splitContainer_originDataMain.SplitterDistance = nValue;
#endif
            this.UiState = MainForm.AppInfo.GetString(
            "marcdetailform",
            "ui_state",
            "");

        }

        public void SaveSize()
        {
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                MainForm.AppInfo.SaveMdiChildFormStates(this,
                    "mdi_form_state");

#if NO
            // ����splitContainer_originDataMain��״̬
            MainForm.AppInfo.SetInt(
                "marcdetailform",
                "splitContainer_originDataMain",
                this.splitContainer_originDataMain.SplitterDistance);
#endif
                MainForm.AppInfo.SetString(
    "marcdetailform",
    "ui_state",
    this.UiState);
            }
        }

        // ����·��
        public static int ParsePath(string strPath,
            out string strProtocol,
            out string strResultsetName,
            out string strIndex,
            out string strError)
        {
            strError = "";
            strProtocol = "";
            strResultsetName = "";
            strIndex = "";

            int nRet = strPath.IndexOf(":");
            if (nRet == -1)
            {
                strError = "ȱ��':'";
                return -1;
            }

            strProtocol = strPath.Substring(0, nRet);
            // ȥ��":"
            strPath = strPath.Substring(nRet + 1);

            nRet = strPath.LastIndexOf("/");
            if (nRet == -1)
            {
                strError = "ȱ��/";
                return -1;
            }

            strResultsetName = strPath.Substring(0, nRet);
            strIndex = strPath.Substring(nRet + 1);

            return 0;
        }

        // ˢ�¹����ļ������п��ܻ���ļ�¼�����ڱ����¼֮��
        // return:
        //      -2  ��֧��
        //      -1  error
        //      0   ��ش����Ѿ����٣�û�б�Ҫˢ��
        //      1   �Ѿ�ˢ��
        //      2   �ڽ������û���ҵ�Ҫˢ�µļ�¼
        public int RefreshCachedRecord(
            string strAction,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (this.LinkedSearchForm == null)
            {
                strError = "û�й����ļ�����";
                goto ERROR1;
            }
            else
            {
                if (this.LinkedSearchForm.IsValid() == false)
                {
                    strError = "��ص�Z39.50�������Ѿ����٣�û�б�Ҫˢ��";
                    return 0;
                }
            }

            string strProtocol = "";
            string strPath = "";
            nRet = Global.ParsePath(this.SavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // ��������Э���� �� ԭ�����ӵļ����� û�з����仯
            if (strProtocol == this.LinkedSearchForm.CurrentProtocol)
            {
                nRet = this.LinkedSearchForm.RefreshOneRecord(
                    "path:" + strPath,
                    strAction,
                    out strError);
                return nRet;
            }

            // ���Э���������˱仯
            strPath = this.textBox_tempRecPath.Text;
            if (String.IsNullOrEmpty(strPath) == true)
            {
                strError = "·��Ϊ��";
                return 0;
            }


            // �������������
            strProtocol = "";
            string strResultsetName = "";
            string strIndex = "";

            nRet = ParsePath(strPath,
                out strProtocol,
                out strResultsetName,
                out strIndex,
                out strError);
            if (nRet == -1)
            {
                strError = "����·�� '" + strPath + "' �ַ��������з�������: " + strError;
                goto ERROR1;
            }

            if (strProtocol != this.LinkedSearchForm.CurrentProtocol)
            {
                strError = "��������Э���Ѿ������ı�";
                goto ERROR1;
            }

            if (strResultsetName != this.LinkedSearchForm.CurrentResultsetPath)
            {
                strError = "������Ѿ������ı�";
                goto ERROR1;
            }

            int index = 0;

            index = Convert.ToInt32(strIndex) - 1;

            nRet = this.LinkedSearchForm.RefreshOneRecord(
                "index:" + index.ToString(),
                strAction,
                out strError);
            return nRet;
        ERROR1:
            return -1;
        }

        public void Reload()
        {
            if (String.IsNullOrEmpty(this.SavePath) == true)
                LoadRecord("current", true);
            else
                LoadRecordByPath("current", true);
        }



        // �������ݿ�������λ��װ�ؼ�¼
        // parameters:
        //      bReload �Ƿ�ȷ�������ݿ�װ��
        public int LoadRecordByPath(string strDirection,
            bool bReload = false)
        {
            string strError = "";
            int nRet = 0;

            if (strDirection == "prev")
            {
            }
            else if (strDirection == "next")
            {
            }
            else if (strDirection == "current")
            {
            }
            else
            {
                strError = "����ʶ���strDirection����ֵ '" + strDirection + "'";
                goto ERROR1;
            }

            if (this.BiblioChanged == true
                || this.ObjectChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪװ��������? ",
                    "MarcDetailForm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return 0;
            }

            if (String.IsNullOrEmpty(this.SavePath) == true)
            {
                strError = "��¼·��Ϊ�գ��޷����ж�λ";
                goto ERROR1;
            }

            string strProtocol = "";
            string strPath = "";
            nRet = Global.ParsePath(this.SavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (strProtocol == "dp2library")
            {
                dp2SearchForm dp2_searchform = null;

                if (this.LinkedSearchForm == null
                    || !(this.LinkedSearchForm is dp2SearchForm))
                {
                    dp2_searchform = this.GetDp2SearchForm();

                    if (dp2_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷�����LoadRecord()";
                        goto ERROR1;
                    }
                }
                else
                {
                    dp2_searchform = (dp2SearchForm)this.LinkedSearchForm;
                }

                return LoadDp2Record(dp2_searchform,
                    strPath,
                    strDirection,
                    true,
                    bReload);
            }
            else if (strProtocol == "dtlp")
            {
                DtlpSearchForm dtlp_searchform = null;

                if (this.LinkedSearchForm == null
                    || !(this.LinkedSearchForm is DtlpSearchForm))
                {
                    dtlp_searchform = this.GetDtlpSearchForm();

                    if (dtlp_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�DTLP���������޷�����LoadRecord()";
                        goto ERROR1;
                    }
                }
                else
                {
                    dtlp_searchform = (DtlpSearchForm)this.LinkedSearchForm;
                }

                return LoadAmazonRecord(dtlp_searchform,
                    strPath,
                    strDirection,
                    true);
            }
            else if (strProtocol == "amazon")
            {
                AmazonSearchForm amazon_searchform = null;

                if (this.LinkedSearchForm == null
                    || !(this.LinkedSearchForm is AmazonSearchForm))
                {
                    amazon_searchform = this.GetAmazonSearchForm();

                    if (amazon_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�DTLP���������޷�����LoadRecord()";
                        goto ERROR1;
                    }
                }
                else
                {
                    amazon_searchform = (AmazonSearchForm)this.LinkedSearchForm;
                }

                return LoadAmazonRecord(amazon_searchform,
                    strPath,
                    strDirection,
                    true);
            }
            else
            {
                strError = "LoadRecordByPath()Ŀǰ��֧�� " + strProtocol + " Э��";
                goto ERROR1;
            }

            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

#if NO
        // װ��MARC��¼�����ݼ�¼·��
        // parameters:
        //      strPath ·�������� "localhost/ͼ���ܿ�/ctlno/1"
        public int LoadDtlpRecord(DtlpSearchForm dtlp_searchform,
            string strPath,
            string strDirection,
            bool bLoadResObject)
        {
            string strError = "";

            if (dtlp_searchform == null)
            {
                strError = "dtlp_searchform��������Ϊ��";
                goto ERROR1;
            }

            Debug.Assert(dtlp_searchform.CurrentProtocol == dtlp_searchform.CurrentProtocol.ToLower(), "Э����Ӧ������Сд");

            if (dtlp_searchform.CurrentProtocol != "dtlp")
            {
                strError = "���ṩ�ļ���������dtlpЭ��";
                goto ERROR1;
            }

            DigitalPlatform.Z3950.Record record = null;
            Encoding currentEncoding = null;

            this.CurrentRecord = null;

            byte[] baTimestamp = null;
            string strOutStyle = "";

            string strOutputPath = "";
            string strMARC;

            long lVersion = 0;
            LoginInfo logininfo = null;
            string strXmlFragment = "";

            int nRet = dtlp_searchform.GetOneRecord(
                "marc",
                // strPath,
                // strDirection,
                0,  // test
                "path:" + strPath + ",direction:" + strDirection,
                "",
                out strOutputPath,
                out strMARC,
                out strXmlFragment,
                out strOutStyle,
                out baTimestamp,
                out lVersion,
                out record,
                out currentEncoding,
                out logininfo,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.domXmlFragment = null;

            this.CurrentTimestamp = baTimestamp;
            this.SavePath = dtlp_searchform.CurrentProtocol + ":" + strOutputPath;
            this.CurrentEncoding = currentEncoding;


            /*
            // ����װ�������Դ
            if (bLoadResObject == true)
            {
                this.binaryResControl1.Channel = dp2_searchform.GetChannel(dp2_searchform.GetServerUrl(strServerName));
                nRet = this.binaryResControl1.LoadObject(strLocalPath,
                    strRecordXml,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return -1;
                }
            }*/
            // װ��MARC�༭��
            this.MarcEditor.Marc = strMARC;

            this.CurrentRecord = record;
            if (this.m_currentRecord != null)
            {
                // װ������Ʊ༭��
                this.binaryEditor_originData.SetData(
                    this.m_currentRecord.m_baRecord);

                // װ��ISO2709�ı�
                nRet = this.Set2709OriginText(this.m_currentRecord.m_baRecord,
                    this.CurrentEncoding,
                    out strError);
                if (nRet == -1)
                {
                    this.textBox_originData.Text = strError;
                }

                // ���ݿ���
                this.textBox_originDatabaseName.Text = this.m_currentRecord.m_strDBName;

                // Marc syntax OID
                this.textBox_originMarcSyntaxOID.Text = this.m_currentRecord.m_strSyntaxOID;

                // ��ȷ����OID������ 2008/3/25
                if (String.IsNullOrEmpty(this.m_currentRecord.m_strSyntaxOID) == false)
                    this.AutoDetectedMarcSyntaxOID = "";
            }
            else
            {
                byte[] baMARC = this.CurrentEncoding.GetBytes(strMARC);
                // װ������Ʊ༭��
                this.binaryEditor_originData.SetData(
                    baMARC);

                // װ��ISO2709�ı�
                nRet = this.Set2709OriginText(baMARC,
                    this.CurrentEncoding,
                    out strError);
                if (nRet == -1)
                {
                    this.textBox_originData.Text = strError;
                }
            }


            // ����·��
            /*
            string strPath = searchform.CurrentProtocol + ":"
                + searchform.CurrentResultsetPath
                + "/" + (index + 1).ToString();

            this.textBox_tempRecPath.Text = strPath;
             * */
            this.textBox_tempRecPath.Text = "";


            this.MarcEditor.MarcDefDom = null; // ǿ��ˢ���ֶ�����ʾ
            this.MarcEditor.RefreshNameCaption();

            this.BiblioChanged = false;

            if (this.MarcEditor.FocusedFieldIndex == -1)
                this.MarcEditor.FocusedFieldIndex = 0;

            this.MarcEditor.Focus();
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

#endif

        // װ��MARC��¼�����ݼ�¼·��
        // parameters:
        //      strPath ·�������� "ͼ���ܿ�/1@���ط�����"
        //      bReload �Ƿ�ȷ�������ݿ�װ��
        public int LoadDp2Record(dp2SearchForm dp2_searchform,
            string strPath,
            string strDirection,
            bool bLoadResObject,
            bool bReload = false)
        {
            string strError = "";

            if (dp2_searchform == null)
            {
                strError = "dp2_searchform��������Ϊ��";
                goto ERROR1;
            }

            if (dp2_searchform.CurrentProtocol != "dp2library")
            {
                strError = "���ṩ�ļ��������� dp2library Э��";
                goto ERROR1;
            }

            DigitalPlatform.Z3950.Record record = null;
            Encoding currentEncoding = null;

            this.CurrentRecord = null;

            byte[] baTimestamp = null;
            string strOutStyle = "";

            string strSavePath = "";
            string strMARC;

            long lVersion = 0;
            LoginInfo logininfo = null; 
            string strXmlFragment = "";

            int nRet = dp2_searchform.GetOneRecord(
                //true,
                "marc",
                //strPath,
                //strDirection,
                0,  // test
                "path:" + strPath + ",direction:" + strDirection,
                bReload == true ? "reload" : "",
                out strSavePath,
                out strMARC,
                out strXmlFragment,
                out strOutStyle,
                out baTimestamp,
                out lVersion,
                out record,
                out currentEncoding,
                out logininfo,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            nRet = this.LoadXmlFragment(strXmlFragment,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.CurrentTimestamp = baTimestamp;
            // this.SavePath = dp2_searchform.CurrentProtocol + ":" + strOutputPath;
            this.SavePath = strSavePath;
            this.CurrentEncoding = currentEncoding;


            /*
            // ����װ�������Դ
            if (bLoadResObject == true)
            {
                this.binaryResControl1.Channel = dp2_searchform.GetChannel(dp2_searchform.GetServerUrl(strServerName));
                nRet = this.binaryResControl1.LoadObject(strLocalPath,
                    strRecordXml,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return -1;
                }
            }*/
            // װ��MARC�༭��
            this.MarcEditor.Marc = strMARC;


            this.m_nDisableInitialAssembly++; 
            this.CurrentRecord = record;
            this.m_nDisableInitialAssembly--; 

            if (this.m_currentRecord != null)
            {
                // װ������Ʊ༭��
                this.binaryEditor_originData.SetData(
                    this.m_currentRecord.m_baRecord);

                // װ��ISO2709�ı�
                nRet = this.Set2709OriginText(this.m_currentRecord.m_baRecord,
                    this.CurrentEncoding,
                    out strError);
                if (nRet == -1)
                {
                    this.textBox_originData.Text = strError;
                }

                // ���ݿ���
                this.textBox_originDatabaseName.Text = this.m_currentRecord.m_strDBName;

                // Marc syntax OID
                this.textBox_originMarcSyntaxOID.Text = this.m_currentRecord.m_strSyntaxOID;

                // 2014/5/18
                if (this.UseAutoDetectedMarcSyntaxOID == true)
                {
                    this.AutoDetectedMarcSyntaxOID = this.m_currentRecord.AutoDetectedSyntaxOID;
                    if (string.IsNullOrEmpty(this.AutoDetectedMarcSyntaxOID) == false)
                        this.textBox_originMarcSyntaxOID.Text = this.AutoDetectedMarcSyntaxOID;
                }

#if NO
                // ��ȷ����OID������ 2008/3/25
                if (String.IsNullOrEmpty(this.m_currentRecord.m_strSyntaxOID) == false)
                    this.AutoDetectedMarcSyntaxOID = "";
#endif
            }
            else
            {
                byte[] baMARC = this.CurrentEncoding.GetBytes(strMARC);
                // װ������Ʊ༭��
                this.binaryEditor_originData.SetData(
                    baMARC);

                // װ��ISO2709�ı�
                nRet = this.Set2709OriginText(baMARC,
                    this.CurrentEncoding,
                    out strError);
                if (nRet == -1)
                {
                    this.textBox_originData.Text = strError;
                }
            }

            DisplayHtml(strMARC, this.textBox_originMarcSyntaxOID.Text);

            // ����·��
            /*
            string strPath = searchform.CurrentProtocol + ":"
                + searchform.CurrentResultsetPath
                + "/" + (index + 1).ToString();

            this.textBox_tempRecPath.Text = strPath;
             * */
            if (strDirection != "current")  // 2013/9/18
                this.textBox_tempRecPath.Text = "";


            this.MarcEditor.MarcDefDom = null; // ǿ��ˢ���ֶ�����ʾ
            this.MarcEditor.RefreshNameCaption();

            this.BiblioChanged = false;

            if (this.MarcEditor.FocusedFieldIndex == -1)
                this.MarcEditor.FocusedFieldIndex = 0;

            this.MarcEditor.Focus();
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // װ��MARC��¼�����ݼ�¼·��
        // parameters:
        //      strPath ·�������� "localhost/ͼ���ܿ�/ctlno/1"
        public int LoadAmazonRecord(ISearchForm searchform,
            string strPath,
            string strDirection,
            bool bLoadResObject)
        {
            string strError = "";

            if (searchform == null)
            {
                strError = "searchform ��������Ϊ��";
                goto ERROR1;
            }

            Debug.Assert(searchform.CurrentProtocol == searchform.CurrentProtocol.ToLower(), "Э����Ӧ������Сд");
#if NO
            if (dtlp_searchform.CurrentProtocol != "amazon")
            {
                strError = "���ṩ�ļ���������dtlpЭ��";
                goto ERROR1;
            }
#endif

            DigitalPlatform.Z3950.Record record = null;
            Encoding currentEncoding = null;

            this.CurrentRecord = null;

            byte[] baTimestamp = null;
            string strOutStyle = "";

            string strSavePath = "";
            string strMARC;

            long lVersion = 0;
            LoginInfo logininfo = null;
            string strXmlFragment = "";

            int nRet = searchform.GetOneRecord(
                "marc",
                //strPath,
                //strDirection,
                0,  // test
                "path:" + strPath + ",direction:" + strDirection,
                "",
                out strSavePath,
                out strMARC,
                                out strXmlFragment,
                out strOutStyle,
                out baTimestamp,
                                out lVersion,
                out record,
                out currentEncoding,
                                out logininfo,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.domXmlFragment = null;

            this.CurrentTimestamp = baTimestamp;
            // this.SavePath = searchform.CurrentProtocol + ":" + strOutputPath;
            this.SavePath = strSavePath;
            this.CurrentEncoding = currentEncoding;

            /*
            // ����װ�������Դ
            if (bLoadResObject == true)
            {
                this.binaryResControl1.Channel = dp2_searchform.GetChannel(dp2_searchform.GetServerUrl(strServerName));
                nRet = this.binaryResControl1.LoadObject(strLocalPath,
                    strRecordXml,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return -1;
                }
            }*/
            // װ��MARC�༭��
            this.MarcEditor.Marc = strMARC;


            this.CurrentRecord = record;
            if (this.m_currentRecord != null)
            {
                // װ������Ʊ༭��
                this.binaryEditor_originData.SetData(
                    this.m_currentRecord.m_baRecord);

                // װ��ISO2709�ı�
                nRet = this.Set2709OriginText(this.m_currentRecord.m_baRecord,
                    this.CurrentEncoding,
                    out strError);
                if (nRet == -1)
                {
                    this.textBox_originData.Text = strError;
                }

                // ���ݿ���
                this.textBox_originDatabaseName.Text = this.m_currentRecord.m_strDBName;

                // Marc syntax OID
                this.textBox_originMarcSyntaxOID.Text = this.m_currentRecord.m_strSyntaxOID;

                // ��ȷ����OID������ 2008/3/25
                if (String.IsNullOrEmpty(this.m_currentRecord.m_strSyntaxOID) == false)
                    this.AutoDetectedMarcSyntaxOID = "";
            }
            else
            {
                byte[] baMARC = this.CurrentEncoding.GetBytes(strMARC);
                // װ������Ʊ༭��
                this.binaryEditor_originData.SetData(
                    baMARC);

                // װ��ISO2709�ı�
                nRet = this.Set2709OriginText(baMARC,
                    this.CurrentEncoding,
                    out strError);
                if (nRet == -1)
                {
                    this.textBox_originData.Text = strError;
                }
            }

            DisplayHtml(strMARC, this.textBox_originMarcSyntaxOID.Text);

            this.textBox_tempRecPath.Text = "";

            this.MarcEditor.MarcDefDom = null; // ǿ��ˢ���ֶ�����ʾ
            this.MarcEditor.RefreshNameCaption();

            this.BiblioChanged = false;

            if (this.MarcEditor.FocusedFieldIndex == -1)
                this.MarcEditor.FocusedFieldIndex = 0;

            this.MarcEditor.Focus();
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        public Hashtable GetSelectedPinyin()
        {
            Hashtable result = new Hashtable();
            if (this.domXmlFragment == null)
                return result;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);
            XmlNodeList nodes = this.domXmlFragment.DocumentElement.SelectNodes("dprms:selectedPinyin/dprms:entry", nsmgr);
            foreach (XmlNode node in nodes)
            {
                result[node.InnerText] = DomUtil.GetAttr(node, "pinyin");
            }

            return result;
        }

        public void SetSelectedPinyin(Hashtable table)
        {
            if (this.domXmlFragment == null)
            {
                this.domXmlFragment = new XmlDocument();
                this.domXmlFragment.LoadXml("<root />");
            }
            bool bChanged = false;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);
            XmlNode root = this.domXmlFragment.DocumentElement.SelectSingleNode("dprms:selectedPinyin", nsmgr);
            if (root == null)
            {
                root = this.domXmlFragment.CreateElement("dprms:selectedPinyin", DpNs.dprms);
                this.domXmlFragment.DocumentElement.AppendChild(root);
                bChanged = true;
            }
            else
            {
                if (String.IsNullOrEmpty(root.InnerXml) == false)
                {
                    root.InnerXml = ""; // ���ԭ����ȫ���¼�Ԫ��
                    bChanged = true;
                }
            }

            if (table == null)
            {
                if (bChanged == true)
                    this.BiblioChanged = true;
                return;
            }

            foreach (string key in table.Keys)
            {
                // keyΪ����
                XmlNode node = this.domXmlFragment.CreateElement("dprms:entry", DpNs.dprms);
                root.AppendChild(node);
                node.InnerText = key;
                DomUtil.SetAttr(node, "pinyin", (string)table[key]);
                bChanged = true;
            }

            if (bChanged == true)
                this.BiblioChanged = true;
        }

        // װ����Ŀ���������XMLƬ��
        int LoadXmlFragment(string strXmlFragment,
            out string strError)
        {
            strError = "";

            this.domXmlFragment = null;

            if (string.IsNullOrEmpty(strXmlFragment) == true)
                return 0;

            this.domXmlFragment = new XmlDocument();
            this.domXmlFragment.LoadXml("<root />");

            try
            {
                this.domXmlFragment.DocumentElement.InnerXml = strXmlFragment;
            }
            catch (Exception ex)
            {
                strError = "װ��XML Fragment��InnerXmlʱ����: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // parameters:
        //      bForceFullElementSet    �Ƿ�ǿ����FullԪ�ؼ������Ϊfalse����ʾ����ν��Ҳ����˵���յ�ǰ��Ԫ�ؼ�(�п�����Full��Ҳ�п�����Brief)
        //      bReload                 �Ƿ�ȷ�������ݿ�װ��
        public int LoadRecord(string strDirection,
            bool bForceFullElementSet = false,
            bool bReload = false)
        {
            string strError = "";
            int nRet = 0;

            string strChangedWarning = "";

            if (this.ObjectChanged == true
                || this.BiblioChanged == true)
            {
                strChangedWarning = "��ǰ�� "
                    + GetCurrentChangedPartName()
                + " ���޸Ĺ���\r\n\r\n";

                string strText = strChangedWarning;

                strText += "ȷʵҪװ���µ���Ŀ��¼ ?";

                // ������װ��
                DialogResult result = MessageBox.Show(this,
                    strText,
                    "MarcDetailForm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    return 0;
                }
            }

            // ����������MARC�ļ�״̬ʱ
            if (this.linkMarcFile != null)
            {
                string strMarc = "";
                byte[] baRecord = null;

                if (strDirection == "next")
                {
                    //	    2	����(��ǰ���صļ�¼��Ч)
                    nRet = this.linkMarcFile.NextRecord(out strMarc,
                        out baRecord,
                        out strError);
                    if (nRet == 2)
                    {
                        strError = "��β";
                        goto ERROR1;
                    }
                }
                else if (strDirection == "prev")
                {
                    nRet = this.linkMarcFile.PrevRecord(out strMarc,
                        out baRecord,
                        out strError);
                    if (nRet == 1)
                    {
                        strError = "��ͷ";
                        goto ERROR1;
                    }
                }
                else if (strDirection == "current")
                {
                    nRet = this.linkMarcFile.CurrentRecord(out strMarc,
                        out baRecord,
                        out strError);
                    if (nRet == 1)
                    {
                        strError = "??";
                        goto ERROR1;
                    }
                }
                else
                {
                    strError = "����ʶ���strDirection����ֵ '" + strDirection + "'";
                    goto ERROR1;
                }
                if (nRet == -1)
                    goto ERROR1;

                LoadLinkedMarcRecord(strMarc, baRecord);
                return 0;
            }

            if (this.LinkedSearchForm == null)
            {
                strError = "û�й����ļ�����";
                goto ERROR1;
            }

            string strPath = this.textBox_tempRecPath.Text;
            if (String.IsNullOrEmpty(strPath) == true)
            {
                strError = "·��Ϊ��";
                goto ERROR1;
            }

            // �������������
            string strProtocol = "";
            string strResultsetName = "";
            string strIndex = "";

            nRet = ParsePath(strPath,
                out strProtocol,
                out strResultsetName,
                out strIndex,
                out strError);
            if (nRet == -1)
            {
                strError = "����·�� '" +strPath+ "' �ַ��������з�������: " + strError;
                goto ERROR1;
            }

            if (strProtocol != this.LinkedSearchForm.CurrentProtocol)
            {
                strError = "��������Э���Ѿ������ı�";
                goto ERROR1;
            }

            if (strResultsetName != this.LinkedSearchForm.CurrentResultsetPath)
            {
                strError = "������Ѿ������ı�";
                goto ERROR1;
            }

            int index = 0;

            index = Convert.ToInt32(strIndex) - 1;

            REDO:
            if (strDirection == "prev")
            {
                index--;
                if (index < 0)
                {
                    strError = "��ͷ";
                    goto ERROR1;
                }
            }
            else if (strDirection == "current")
            {
            }
            else if (strDirection == "next")
            {
                index++;
            }
            else
            {
                strError = "����ʶ���strDirection����ֵ '" + strDirection + "'";
                goto ERROR1;
            }

            if (this.LinkedSearchForm.IsValid() == false)
            {
                strError = "���ӵļ������Ѿ�ʧЧ���޷������¼ " + strPath;
                goto ERROR1;
            }

            // return:
            //      -1  ����
            //      0   �ɹ�
            //      2   ��Ҫ����
            nRet = LoadRecord(this.LinkedSearchForm, index, bForceFullElementSet, bReload);  // 
            if (nRet == 2)
            {
                if (strDirection == "current")
                {
                    strError = "��ǰλ�� " + index.ToString() + " ����Ҫ������λ��";
                    goto ERROR1;
                }
                goto REDO;
            }
            return nRet;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        int Set2709OriginText(byte[] baOrigin,
            Encoding encoding,
            out string strError)
        {
            strError = "";

            this.label_originDataWarning.Text = "";

            if (encoding == null)
            {
                int nRet = this.MainForm.GetEncoding(this.comboBox_originDataEncoding.Text,
                    out encoding,
                    out strError);
                if (nRet == -1)
                    return -1;

                /*
                // 2007/7/24 add
                if (encoding == null)
                    encoding = Encoding.UTF8;
                 * */
            }
            else
            {
                this.comboBox_originDataEncoding.Text = GetEncodingForm.GetEncodingName(this.CurrentEncoding);

            }

            this.textBox_originData.Text = (baOrigin == null? "" : encoding.GetString(baOrigin));

            return 0;
        }

        /*
�������� crashReport -- �쳣���� 
���� dp2catalog 
������ xxxx 
ý������ text 
���� ����δ����Ľ����߳��쳣: 
Type: System.ObjectDisposedException
Message: �޷��������ͷŵĶ���
������:��MarcEditor����
Stack:
�� System.Windows.Forms.Control.CreateHandle()
�� System.Windows.Forms.Control.get_Handle()
�� DigitalPlatform.Marc.Field.CalculateHeight(Graphics g, Boolean bIgnoreEdit)
�� DigitalPlatform.Marc.FieldCollection.AddInternal(String strName, String strIndicator, String strValue, Boolean bFireTextChanged, Boolean bInOrder, Int32& nOutputPosition)
�� DigitalPlatform.Marc.Record.SetMarc(String strMarc, Boolean bCheckMarcDef, String& strError)
�� DigitalPlatform.Marc.MarcEditor.set_Marc(String value)
�� dp2Catalog.MarcDetailForm.LoadRecord(ISearchForm searchform, Int32 index, Boolean bForceFullElementSet, Boolean bReload)
�� dp2Catalog.dp2SearchForm.LoadDetail(Int32 index, Boolean bOpenNew)
�� dp2Catalog.dp2SearchForm.listView_browse_DoubleClick(Object sender, EventArgs e)
�� System.Windows.Forms.ListView.WndProc(Message& m)
�� DigitalPlatform.GUI.ListViewNF.WndProc(Message& m)
�� System.Windows.Forms.NativeWindow.Callback(IntPtr hWnd, Int32 msg, IntPtr wparam, IntPtr lparam)


dp2Catalog �汾: dp2Catalog, Version=2.4.5698.23777, Culture=neutral, PublicKeyToken=null
����ϵͳ��Microsoft Windows NT 6.1.7601 Service Pack 1 
����ʱ�� 2015/8/10 13:48:50 (Mon, 10 Aug 2015 13:48:50 +0800) 
ǰ�˵�ַ xxxx ���� http://dp2003.com/dp2library 
         * */
        // �Ӽ�����װ��MARC��¼
        // parameters:
        //      bForceFullElementSet    �Ƿ�ǿ����FullԪ�ؼ������Ϊfalse����ʾ����ν��Ҳ����˵���յ�ǰ��Ԫ�ؼ�(�п�����Full��Ҳ�п�����Brief)
        //      bReload �Ƿ�ȷ�������ݿ�װ��
        // return:
        //      -1  ����
        //      0   �ɹ�
        //      2   ��Ҫ����
        public int LoadRecord(ISearchForm searchform,
            int index,
            bool bForceFullElementSet = false,
            bool bReload = false)
        {
            string strError = "";

            this.stop.BeginLoop();  // ���������� stop�����Է�ֹ��װ�ص���; Form ���رա���� MarcEditor ���� MARC �ַ��������׳��쳣
            this.EnableControls(false);
            try
            {
                string strMARC = "";

                this.LinkedSearchForm = searchform;
                // this.SavePath = "";  // 2011/5/5 ȥ��

                DigitalPlatform.Z3950.Record record = null;
                Encoding currentEncoding = null;

                this.CurrentRecord = null;
                string strSavePath = "";
                byte[] baTimestamp = null;

                this.m_nDisableInitialAssembly++;   // ��ֹ��γ�ʼ��Assembly
                try
                {
                    string strOutStyle = "";
                    LoginInfo logininfo = null;
                    string strXmlFragment = "";
                    long lVersion = 0;

                    string strParameters = "hilight_browse_line";
                    if (bForceFullElementSet == true)
                        strParameters += ",force_full";
                    if (bReload == true)
                        strParameters += ",reload";

                    // ���һ��MARC/XML��¼
                    // return:
                    //      -1  error
                    //      0   suceed
                    //      1   Ϊ��ϼ�¼
                    //      2   �ָ�������Ҫ����������¼
                    int nRet = searchform.GetOneRecord(
                        "marc",
                        index,  // ������ֹ
                        "index:" + index.ToString(),
                        strParameters,  // true,
                        out strSavePath,
                        out strMARC,
                        out strXmlFragment,
                        out strOutStyle,
                        out baTimestamp,
                        out lVersion,
                        out record,
                        out currentEncoding,
                        out logininfo,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == 2)
                        return 2;

                    nRet = this.LoadXmlFragment(strXmlFragment,
            out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    this.LoginInfo = logininfo;

                    if (strOutStyle != "marc")
                    {
                        strError = "����ȡ�ļ�¼����marc��ʽ";
                        goto ERROR1;
                    }

                    this.RecordVersion = lVersion;

                    this.CurrentRecord = record;
                    if (this.m_currentRecord != null)
                    {
                        // װ������Ʊ༭��
                        this.binaryEditor_originData.SetData(
                            this.m_currentRecord.m_baRecord);

                        // װ��ISO2709�ı�
                        nRet = this.Set2709OriginText(this.m_currentRecord.m_baRecord,
                            this.CurrentEncoding,
                            out strError);
                        if (nRet == -1)
                        {
                            this.textBox_originData.Text = strError;
                        }

                        // ���ݿ���
                        this.textBox_originDatabaseName.Text = this.m_currentRecord.m_strDBName;

                        // Marc syntax OID
                        this.textBox_originMarcSyntaxOID.Text = this.m_currentRecord.m_strSyntaxOID;

                        // 2014/5/18
                        if (this.UseAutoDetectedMarcSyntaxOID == true)
                        {
                            this.AutoDetectedMarcSyntaxOID = this.m_currentRecord.AutoDetectedSyntaxOID;
                            if (string.IsNullOrEmpty(this.AutoDetectedMarcSyntaxOID) == false)
                                this.textBox_originMarcSyntaxOID.Text = this.AutoDetectedMarcSyntaxOID;
                        }

#if NO
                    // ��ȷ����OID������ 2008/3/25
                    if (String.IsNullOrEmpty(this.m_currentRecord.m_strSyntaxOID) == false)
                        this.AutoDetectedMarcSyntaxOID = "";
#endif
                    }
                    else
                    {
                        byte[] baMARC = this.CurrentEncoding.GetBytes(strMARC);
                        // װ������Ʊ༭��
                        this.binaryEditor_originData.SetData(
                            baMARC);

                        // װ��ISO2709�ı�
                        nRet = this.Set2709OriginText(baMARC,
                            this.CurrentEncoding,
                            out strError);
                        if (nRet == -1)
                        {
                            this.textBox_originData.Text = strError;
                        }
                    }
                }
                finally
                {
                    this.m_nDisableInitialAssembly--;
                }

                this.SavePath = strSavePath;
                this.CurrentTimestamp = baTimestamp;
                this.CurrentEncoding = currentEncoding;

                // װ��MARC�༭��
                this.MarcEditor.Marc = strMARC;

                DisplayHtml(strMARC, this.textBox_originMarcSyntaxOID.Text);

                // ����·��

                string strPath = searchform.CurrentProtocol + ":"
                    + searchform.CurrentResultsetPath
                    + "/" + (index + 1).ToString();

                this.textBox_tempRecPath.Text = strPath;

                this.MarcEditor.MarcDefDom = null; // ǿ��ˢ���ֶ�����ʾ
                this.MarcEditor.RefreshNameCaption();

                this.BiblioChanged = false;

                if (this.MarcEditor.FocusedFieldIndex == -1)
                    this.MarcEditor.FocusedFieldIndex = 0;

                this.MarcEditor.Focus();
                return 0;
            }
            finally
            {
                this.stop.EndLoop();
                this.EnableControls(true);
            }
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        private void MarcDetailForm_Activated(object sender, EventArgs e)
        {
            if (stop != null)
                MainForm.stopManager.Active(this.stop);

            MainForm.SetMenuItemState();

            // �˵�
            MainForm.MenuItem_saveOriginRecordToIso2709.Enabled = true;
            MainForm.MenuItem_font.Enabled = true;
            MainForm.MenuItem_saveToTemplate.Enabled = true;
            MainForm.MenuItem_viewAccessPoint.Enabled = true;


            // ��������ť
            MainForm.toolButton_search.Enabled = false;
            MainForm.toolButton_prev.Enabled = true;
            MainForm.toolButton_next.Enabled = true;
            MainForm.toolButton_nextBatch.Enabled = false;

            MainForm.toolButton_getAllRecords.Enabled = false;
            MainForm.toolButton_saveTo.Enabled = true;
            MainForm.toolButton_saveToDB.Enabled = true;
            MainForm.toolButton_save.Enabled = true;
            MainForm.toolButton_delete.Enabled = true;

            MainForm.toolButton_loadTemplate.Enabled = true;

            MainForm.toolButton_dup.Enabled = true;
            MainForm.toolButton_verify.Enabled = true;
            MainForm.toolButton_refresh.Enabled = true;
            MainForm.toolButton_loadFullRecord.Enabled = true;

            if (this.m_verifyViewer != null)
            {
                if (m_verifyViewer.Docked == true
                    && this.MainForm.CurrentVerifyResultControl != m_verifyViewer.ResultControl)
                    this.MainForm.CurrentVerifyResultControl = m_verifyViewer.ResultControl;
            }
            else
            {
                this.MainForm.CurrentVerifyResultControl = null;
            }

            SyncRecord();
        }

        // ͬ�� MARC ��¼
        void SyncRecord()
        {
            string strError = "";
            int nRet = 0;

            string strMarcSyntax = "";
            string strMarcSyntaxOID = this.GetCurrentMarcSyntaxOID(out strError);
            if (String.IsNullOrEmpty(strMarcSyntaxOID) == true)
            {
                /*
                strError = "��ǰMARC syntax OIDΪ�գ��޷��ж�MARC�����ʽ";
                goto ERROR1;
                 * */
                return;
            }

            if (strMarcSyntaxOID == "1.2.840.10003.5.1")
                strMarcSyntax = "unimarc";
            if (strMarcSyntaxOID == "1.2.840.10003.5.10")
                strMarcSyntax = "usmarc";

            if (this.LinkedSearchForm != null
                && this.LinkedSearchForm is dp2SearchForm)
            {
                long lVersion = this.RecordVersion;
                string strMARC = this.MarcEditor.Marc;

                string strProtocol = "";
                string strPath = "";
                nRet = Global.ParsePath(this.SavePath,
                    out strProtocol,
                    out strPath,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // return:
                //      -1  ����
                //      0   û�б�Ҫ����
                //      1   �Ѿ����µ� ������
                //      2   ��Ҫ�� strMARC ��ȡ�����ݸ��µ���¼��
                nRet = this.LinkedSearchForm.SyncOneRecord(
                    strPath,
                    ref lVersion,
                    ref strMarcSyntax,
                    ref strMARC,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 2)
                {
                    this.MarcEditor.Marc = strMARC;
                    this.RecordVersion = lVersion;

                    DisplayHtml(strMARC, strMarcSyntaxOID);
                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void comboBox_originDataEncoding_SelectedIndexChanged(object sender, EventArgs e)
        {
            string strError = "";

            if (this.m_currentRecord == null)
            {
                this.textBox_originData.Text = "ȱ����ǰ��¼";
                this.label_originDataWarning.Text = "";
                return;
            }

            byte[] baOriginData = this.m_currentRecord.m_baRecord;    // this.binaryEditor_originData.GetData(100*1024);

            // װ��ISO2709�ı�
            int nRet = this.Set2709OriginText(baOriginData,
                null,   // ��ǰ���뷽ʽ
                out strError);
            if (nRet == -1)
            {
                this.textBox_originData.Text = strError;
            }
        }

        private void comboBox_originDataEncoding_TextChanged(object sender, EventArgs e)
        {
            comboBox_originDataEncoding_SelectedIndexChanged(this, null);
        }

        public void SaveRecordToWorksheet()
        {
            string strError = "";
            int nRet = 0;

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ����Ĺ������ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = MainForm.LastWorksheetFileName;
            dlg.Filter = "�������ļ� (*.wor)|*.wor|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;


            bool bExist = File.Exists(dlg.FileName);
            bool bAppend = false;

            if (bExist == true)
            {
                DialogResult result = MessageBox.Show(this,
        "�ļ� '" + dlg.FileName + "' �Ѵ��ڣ��Ƿ���׷�ӷ�ʽд���¼?\r\n\r\n--------------------\r\nע��(��)׷��  (��)����  (ȡ��)����",
        "ZSearchForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                    bAppend = true;

                if (result == DialogResult.No)
                    bAppend = false;

                if (result == DialogResult.Cancel)
                {
                    strError = "��������...";
                    goto ERROR1;
                }
            }

            // ���ͬһ���ļ�������ʱ��ı��뷽ʽһ����


            MainForm.LastWorksheetFileName = dlg.FileName;

            StreamWriter sw = null;

            try
            {
                // �����ļ�
                sw = new StreamWriter(MainForm.LastWorksheetFileName,
                    bAppend,	// append
                    System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                strError = "�򿪻򴴽��ļ� " + MainForm.LastWorksheetFileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }

            try
            {
                List<string> lines = null;
                // �����ڸ�ʽ�任Ϊ��������ʽ                    // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = MarcUtil.CvtJineiToWorksheet(
                    this.MarcEditor.Marc,
                    -1,
                    out lines,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                foreach (string line in lines)
                {
                    sw.WriteLine(line);
                }

                if (bAppend == true)
                    MainForm.MessageText =
                        "1����¼�ɹ�׷�ӵ��ļ� " + MainForm.LastWorksheetFileName + " β��";
                else
                    MainForm.MessageText =
                        "1����¼�ɹ����浽���ļ� " + MainForm.LastWorksheetFileName + " β��";

            }
            catch (Exception ex)
            {
                strError = "д���ļ� " + MainForm.LastWorksheetFileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }
            finally
            {
                sw.Close();
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��ԭʼ��¼���浽ISO2709�ļ�
        // TODO: �����¼�������޸ģ����ٱ���ԭʼ��¼����Ҫ����MARC�༭���еļ�¼��
        public void SaveOriginRecordToIso2709()
        {
            string strError = "";
            int nRet = 0;

            byte[] baTarget = null; // ����Ѿ���ISO2709��ʽ����洢������
            string strMarc = "";    // �����MARC���ڸ�ʽ���洢������

            // ������Ǵ�Z39.50Э������ļ�¼�����߼�¼��MARC�༭�����Ѿ��޸Ĺ�
            if (this.m_currentRecord == null
                || (this.m_currentRecord != null && this.m_currentRecord.m_baRecord == null)    // 2008/4/14
                || this.MarcEditor.Changed == true)
            {
                // strError = "û�е�ǰ��¼";
                // goto ERROR1;
                // ��MARC�༭����ȡ��¼
                strMarc = this.MarcEditor.Marc;
                baTarget = null;
            }
            else
            {
                strMarc = "";
                baTarget = this.m_currentRecord.m_baRecord;

            }

            Encoding preferredEncoding = this.CurrentEncoding;

            OpenMarcFileDlg dlg = new OpenMarcFileDlg();
            GuiUtil.SetControlFont(dlg, this.Font);
            dlg.IsOutput = true;
            dlg.GetEncoding -= new GetEncodingEventHandler(dlg_GetEncoding);
            dlg.GetEncoding += new GetEncodingEventHandler(dlg_GetEncoding);
            dlg.FileName = MainForm.LastIso2709FileName;
            dlg.CrLf = MainForm.LastCrLfIso2709;
            dlg.RemoveField998 = MainForm.LastRemoveField998;
            dlg.EncodingListItems = Global.GetEncodingList(true);
            dlg.EncodingName = 
                (String.IsNullOrEmpty(MainForm.LastEncodingName) == true ? GetEncodingForm.GetEncodingName(preferredEncoding) : MainForm.LastEncodingName);
            dlg.EncodingComment = "ע: ԭʼ���뷽ʽΪ " + GetEncodingForm.GetEncodingName(preferredEncoding);
            dlg.MarcSyntax = "<�Զ�>";    // strPreferedMarcSyntax;
            dlg.EnableMarcSyntax = false;
            dlg.ShowDialog(this);
            if (dlg.DialogResult != DialogResult.OK)
                return;

            Encoding targetEncoding = null;

            if (dlg.EncodingName == "MARC-8"
    && preferredEncoding.Equals(this.MainForm.Marc8Encoding) == false)
            {
                strError = "��������޷����С�ֻ���ڼ�¼��ԭʼ���뷽ʽΪ MARC-8 ʱ������ʹ��������뷽ʽ�����¼��";
                goto ERROR1;
            }

            nRet = this.MainForm.GetEncoding(dlg.EncodingName,
                out targetEncoding,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }

            string strLastFileName = MainForm.LastIso2709FileName;
            string strLastEncodingName = MainForm.LastEncodingName;


            bool bExist = File.Exists(dlg.FileName);
            bool bAppend = false;

            if (bExist == true)
            {
                DialogResult result = MessageBox.Show(this,
        "�ļ� '" + dlg.FileName + "' �Ѵ��ڣ��Ƿ���׷�ӷ�ʽд���¼?\r\n\r\n--------------------\r\nע��(��)׷��  (��)����  (ȡ��)����",
        "ZSearchForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                    bAppend = true;

                if (result == DialogResult.No)
                    bAppend = false;

                if (result == DialogResult.Cancel)
                {
                    strError = "��������...";
                    goto ERROR1;
                }
            }

            // ���ͬһ���ļ�������ʱ��ı��뷽ʽһ����
            if (strLastFileName == dlg.FileName
                && bAppend == true)
            {
                if (strLastEncodingName != ""
                    && strLastEncodingName != dlg.EncodingName)
                {
                    DialogResult result = MessageBox.Show(this,
                        "�ļ� '" + dlg.FileName + "' ������ǰ�Ѿ��� " + strLastEncodingName + " ���뷽ʽ�洢�˼�¼���������Բ�ͬ�ı��뷽ʽ " + dlg.EncodingName + " ׷�Ӽ�¼�����������ͬһ�ļ��д��ڲ�ͬ���뷽ʽ�ļ�¼�����ܻ������޷�����ȷ��ȡ��\r\n\r\n�Ƿ����? (��)׷��  (��)��������",
                        "ZSearchForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                    {
                        strError = "��������...";
                        goto ERROR1;
                    }

                }
            }

            MainForm.LastIso2709FileName = dlg.FileName;
            MainForm.LastCrLfIso2709 = dlg.CrLf;
            MainForm.LastEncodingName = dlg.EncodingName;
            MainForm.LastRemoveField998 = dlg.RemoveField998;


            Stream s = null;

            try
            {
                s = File.Open(MainForm.LastIso2709FileName,
                     FileMode.OpenOrCreate);
                if (bAppend == false)
                    s.SetLength(0);
                else
                    s.Seek(0, SeekOrigin.End);
            }
            catch (Exception ex)
            {
                strError = "�򿪻򴴽��ļ� " + MainForm.LastIso2709FileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }

            try
            {
                string strMarcSyntax = "";

                // ��Դ��Ŀ����벻ͬ��ʱ�򣬲���Ҫ���MARC�﷨����
                if (this.CurrentEncoding.Equals(targetEncoding) == false
                    || strMarc != "")
                {
                    string strMarcSyntaxOID = this.GetCurrentMarcSyntaxOID(out strError);
                    if (String.IsNullOrEmpty(strMarcSyntaxOID) == true)
                    {
                        strError = "��ǰMARC syntax OIDΪ�գ��޷��ж�MARC�����ʽ";
                        goto ERROR1;
                    }

                    if (strMarcSyntaxOID == "1.2.840.10003.5.1")
                        strMarcSyntax = "unimarc";
                    if (strMarcSyntaxOID == "1.2.840.10003.5.10")
                        strMarcSyntax = "usmarc";
                }


                if (strMarc != "")
                {
                    Debug.Assert(strMarcSyntax != "", "");

                    if (dlg.RemoveField998 == true)
                    {
                        MarcRecord temp = new MarcRecord(strMarc);
                        temp.select("field[@name='998']").detach();
                        strMarc = temp.Text;
                    }

                    if (dlg.Mode880 == true && strMarcSyntax == "usmarc")
                    {
                        MarcRecord temp = new MarcRecord(strMarc);
                        MarcQuery.To880(temp);
                        strMarc = temp.Text;
                    }

                    // ��MARC���ڸ�ʽת��ΪISO2709��ʽ
                    // parameters:
                    //      strSourceMARC   [in]���ڸ�ʽMARC��¼��
                    //      strMarcSyntax   [in]Ϊ"unimarc"��"usmarc"
                    //      targetEncoding  [in]���ISO2709�ı��뷽ʽ��ΪUTF8��codepage-936�ȵ�
                    //      baResult    [out]�����ISO2709��¼�����뷽ʽ��targetEncoding�������ơ�ע�⣬������ĩβ������0�ַ���
                    // return:
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = MarcUtil.CvtJineiToISO2709(
                        strMarc,
                        strMarcSyntax,
                        targetEncoding,
                        out baTarget,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }
                else if (this.CurrentEncoding.Equals(targetEncoding) == true)
                {
                    // source��target���뷽ʽ��ͬ������ת��
                    // baTarget = this.CurrentRecord.m_baRecord;
                    Debug.Assert(strMarcSyntax == "", "");

                    // �淶�� ISO2709 �����¼
                    // ��Ҫ�Ǽ������ļ�¼�������Ƿ���ȷ��ȥ������ļ�¼������
                    baTarget = MarcUtil.CononicalizeIso2709Bytes(targetEncoding,
                        baTarget);
                }
                else
                {
                    // baTarget = this.CurrentRecord.m_baRecord;

                    Debug.Assert(strMarcSyntax != "", "");

                    nRet = ZSearchForm.ChangeIso2709Encoding(
                        this.CurrentEncoding,
                        baTarget,
                        targetEncoding,
                        strMarcSyntax,
                        out baTarget,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }


                s.Seek(0, SeekOrigin.End);

                s.Write(baTarget, 0,
                    baTarget.Length);

                if (dlg.CrLf == true)
                {
                    byte[] baCrLf = targetEncoding.GetBytes("\r\n");
                    s.Write(baCrLf, 0,
                        baCrLf.Length);
                }

                if (bAppend == true)
                    MainForm.MessageText = 
                        "1����¼�ɹ�׷�ӵ��ļ� " + MainForm.LastIso2709FileName + " β��";
                else
                    MainForm.MessageText =
                        "1����¼�ɹ����浽���ļ� " + MainForm.LastIso2709FileName + " β��";

            }
            catch (Exception ex)
            {
                strError = "д���ļ� " + MainForm.LastIso2709FileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }
            finally
            {
                s.Close();
            }

            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }

        void dlg_GetEncoding(object sender, GetEncodingEventArgs e)
        {
            string strError = "";
            Encoding encoding = null;
            int nRet = this.MainForm.GetEncoding(e.EncodingName,
                out encoding,
                out strError);
            if (nRet == -1)
            {
                e.ErrorInfo = strError;
                return;
            }
            e.Encoding = encoding;
        }

        // ��õ�ǰ��¼��MARC��ʽOID
        string GetCurrentMarcSyntaxOID(out string strError)
        {
            strError = "";

            string strMarcSyntaxOID = "";

            if (String.IsNullOrEmpty(this.AutoDetectedMarcSyntaxOID) == false)
            {
                // �����Զ�ʶ��Ľ��
                strMarcSyntaxOID = this.AutoDetectedMarcSyntaxOID;
            }
            else
            {
                if (this.m_currentRecord == null)
                {
                    strError = "û�е�ǰ��¼��Ϣ(�Ӷ��޷���֪MARC��ʽ)";
                    return null;
                }

                strMarcSyntaxOID = this.m_currentRecord.m_strSyntaxOID;

                // 2008/1/8
                if (strMarcSyntaxOID == "1.2.840.10003.5.109.10")
                    strMarcSyntaxOID = "1.2.840.10003.5.10";    // MARCXML����USMARC����
            }

            return strMarcSyntaxOID;
        }

        private void MarcEditor_GetConfigFile(object sender, DigitalPlatform.Marc.GetConfigFileEventArgs e)
        {
            int nRet = 0;
            string strError = "";

            if (String.IsNullOrEmpty(this.SavePath) == false)
            {
                string strProtocol = "";
                string strPath = "";

                // �������������
                nRet = Global.ParsePath(this.SavePath,
                    out strProtocol,
                    out strPath,
                    out strError);
                if (nRet == -1)
                {
                    strError = "����·�� '" + this.SavePath + "' �ַ��������з�������: " + strError;
                    goto ERROR1;
                }

                if (strProtocol != "dp2library")
                    goto OTHER;

                dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷���ȡ�����ļ�";
                    goto ERROR1;
                }

                string strServerName = "";
                string strLocalPath = "";

                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                if (string.Compare(strServerName, "mem", true) == 0
                || string.Compare(strServerName, "file", true) == 0)
                    goto OTHER;

                string strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);

                string strSyntax = "";
                // ���һ�����ݿ������syntax
                // parameters:
                //      stop    ���!=null����ʾʹ�����stop�����Ѿ�OnStop +=
                //              ���==null����ʾ���Զ�ʹ��this.stop�����Զ�OnStop+=
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = dp2_searchform.GetDbSyntax(
                    null,
                    strServerName,
                    strBiblioDbName,
                    out strSyntax,
                    out strError);
                if (nRet == -1)
                {
                    strError = "��ȡ��Ŀ�� '" + strBiblioDbName + "�����ݸ�ʽʱ��������: " + strError;
                    goto ERROR1;
                }

                /*
                string strDefFilename = "";

                if (strSyntax == "unimarc"
                    || strSyntax == "usmarc")
                    strDefFilename = "marcdef";
                else
                {
                    strError = "��ѡ��Ŀ�� '" + strBiblioDbName + "' ����MARC��ʽ�����ݿ�";
                    goto ERROR1;
                }*/

                // �õ��ɾ����ļ���
                string strCfgFileName = e.Path;
                nRet = strCfgFileName.IndexOf("#");
                if (nRet != -1)
                {
                    strCfgFileName = strCfgFileName.Substring(0, nRet);
                }

                // Ȼ����cfgs/template�����ļ�
                string strCfgFilePath = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;

                string strCode = "";
                byte[] baCfgOutputTimestamp = null;
                nRet = dp2_searchform.GetCfgFile(
                    true,
                    strCfgFilePath,
                    out strCode,
                    out baCfgOutputTimestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                e.Stream = new MemoryStream(Encoding.UTF8.GetBytes(strCode));
                return;
            }

        OTHER:
            {
                string strCfgFileName = e.Path;
                nRet = strCfgFileName.IndexOf("#");
                if (nRet != -1)
                {
                    strCfgFileName = strCfgFileName.Substring(0, nRet);
                }

                string strMarcSyntaxOID = "";

                strMarcSyntaxOID = this.GetCurrentMarcSyntaxOID(out strError);
                if (String.IsNullOrEmpty(strMarcSyntaxOID) == true)
                {
                    e.ErrorInfo = "��Ϊ: " + strError + "���޷���������ļ� '" + strCfgFileName + "'";
                    return;
                }

                string strPath = this.MainForm.DataDir + "\\" +  strMarcSyntaxOID.Replace(".", "_") + "\\" + strCfgFileName;

                try
                {
                    Stream s = File.OpenRead(strPath);

                    e.Stream = s;
                }
                catch (Exception ex)
                {
                    e.ErrorInfo = "�ļ�  " + strPath + " ��ʧ��: " + ex.Message;
                }
            }

            return;
        ERROR1:
            e.ErrorInfo = strError;
        }

        private void MarcEditor_GetConfigDom(object sender, GetConfigDomEventArgs e)
        {
            int nRet = 0;
            string strError = "";

            if (String.IsNullOrEmpty(this.SavePath) == false)
            {
                string strProtocol = "";
                string strPath = "";

                // �������������
                nRet = Global.ParsePath(this.SavePath,
                    out strProtocol,
                    out strPath,
                    out strError);
                if (nRet == -1)
                {
                    strError = "����·�� '" + this.SavePath + "' �ַ��������з�������: " + strError;
                    goto ERROR1;
                }

                if (strProtocol != "dp2library")
                    goto OTHER;

                dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷���ȡ�����ļ�";
                    goto ERROR1;
                }

                string strServerName = "";
                string strLocalPath = "";

                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                if (string.Compare(strServerName, "mem", true) == 0
                || string.Compare(strServerName, "file", true) == 0)
                    goto OTHER;

                string strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);

                string strSyntax = "";
                // ���һ�����ݿ������syntax
                // parameters:
                //      stop    ���!=null����ʾʹ�����stop�����Ѿ�OnStop +=
                //              ���==null����ʾ���Զ�ʹ��this.stop�����Զ�OnStop+=
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = dp2_searchform.GetDbSyntax(
                    null,
                    strServerName,
                    strBiblioDbName,
                    out strSyntax,
                    out strError);
                if (nRet == -1)
                {
                    strError = "��ȡ��Ŀ�� '" + strBiblioDbName + "�����ݸ�ʽʱ��������: " + strError;
                    goto ERROR1;
                }

                /*
                string strDefFilename = "";

                if (strSyntax == "unimarc"
                    || strSyntax == "usmarc")
                    strDefFilename = "marcdef";
                else
                {
                    strError = "��ѡ��Ŀ�� '" + strBiblioDbName + "' ����MARC��ʽ�����ݿ�";
                    goto ERROR1;
                }*/

                // �õ��ɾ����ļ���
                string strCfgFileName = e.Path;
                nRet = strCfgFileName.IndexOf("#");
                if (nRet != -1)
                {
                    strCfgFileName = strCfgFileName.Substring(0, nRet);
                }

                // Ȼ����cfgs/template�����ļ�
                string strCfgFilePath = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;

                e.XmlDocument = this.MainForm.DomCache.FindObject(strCfgFilePath);
                if (e.XmlDocument != null)
                    return;

                string strCode = "";
                byte[] baCfgOutputTimestamp = null;
                nRet = dp2_searchform.GetCfgFile(
                    true,
                    strCfgFilePath,
                    out strCode,
                    out baCfgOutputTimestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strCode);
                }
                catch (Exception ex)
                {
                    e.ErrorInfo = "װ�������ļ� '"+strCfgFilePath+"' �� XMLDOM �Ĺ����г��ִ���: " + ex.Message;
                    return;
                }
                e.XmlDocument = dom;
                this.MainForm.DomCache.SetObject(strCfgFilePath, dom);
                return;
            }

        OTHER:
            {
                string strCfgFileName = e.Path;
                nRet = strCfgFileName.IndexOf("#");
                if (nRet != -1)
                {
                    strCfgFileName = strCfgFileName.Substring(0, nRet);
                }


                string strMarcSyntaxOID = "";

                strMarcSyntaxOID = this.GetCurrentMarcSyntaxOID(out strError);
                if (String.IsNullOrEmpty(strMarcSyntaxOID) == true)
                {
                    e.ErrorInfo = "��Ϊ: " + strError + "���޷���������ļ� '" + strCfgFileName + "'";
                    return;
                }


                string strPath = this.MainForm.DataDir + "\\" + strMarcSyntaxOID.Replace(".", "_") + "\\" + strCfgFileName;

                e.XmlDocument = this.MainForm.DomCache.FindObject(strPath);
                if (e.XmlDocument != null)
                    return;

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.Load(strPath);
                }
                catch (Exception ex)
                {
                    e.ErrorInfo = "װ�������ļ� '" + strPath + "' �� XMLDOM �Ĺ����г��ִ���: " + ex.Message;
                    return;
                }
                e.XmlDocument = dom;
                this.MainForm.DomCache.SetObject(strPath, dom);
            }

            return;
        ERROR1:
            e.ErrorInfo = strError;
        }

        // ��ȫ·���޸�Ϊ׷����̬��·����Ҳ���ǰѼ�¼id�޸�Ϊ?
        public static int ChangePathToAppendStyle(string strOriginPath,
            out string strOutputPath,
            out string strError)
        {
            strOutputPath = "";
            strError = "";

            string strProtocol = "";
            string strPath = "";
            int nRet = Global.ParsePath(strOriginPath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                return -1;

            if (strProtocol == "dtlp")
            {
                nRet = strPath.IndexOf("/");
                if (nRet != -1)
                    strOutputPath = strProtocol + ":" + strPath.Substring(0, nRet) + "/?";
                else
                    strOutputPath = strProtocol + ":" + strPath;

                return 0;
            }
            else if (strProtocol == "dp2library")
            {
                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                string strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);

                strOutputPath = strProtocol + ":" + strBiblioDbName + "/?" + "@" + strServerName;
                return 0;
            }
            else if (strProtocol == "unioncatalog")
            {
                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                string strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);

                strOutputPath = strProtocol + ":" + strBiblioDbName + "/?" + "@" + strServerName;
                return 0;
            }
            else
            {
                strError = "����ʶ���Э���� '" + strProtocol + "'";
                return -1;
            }
        }

        public int LinkMarcFile()
        {
            OpenMarcFileDlg dlg = new OpenMarcFileDlg();
            GuiUtil.SetControlFont(dlg, this.Font);
            dlg.IsOutput = false;
            dlg.GetEncoding -= new GetEncodingEventHandler(dlg_GetEncoding);
            dlg.GetEncoding += new GetEncodingEventHandler(dlg_GetEncoding);
            dlg.FileName = this.MainForm.LinkedMarcFileName;
            // dlg.CrLf = MainForm.LastCrLfIso2709;
            dlg.EncodingListItems = Global.GetEncodingList(true);
            // dlg.EncodingName = ""; GetEncodingForm.GetEncodingName(preferredEncoding);
            // dlg.EncodingComment = "ע: ԭʼ���뷽ʽΪ " + GetEncodingForm.GetEncodingName(preferredEncoding);
            // dlg.MarcSyntax = "<�Զ�>";    // strPreferedMarcSyntax;
            dlg.EnableMarcSyntax = true;

            if (String.IsNullOrEmpty(this.MainForm.LinkedEncodingName) == false)
                dlg.EncodingName = this.MainForm.LinkedEncodingName;
            if (String.IsNullOrEmpty(this.MainForm.LinkedMarcSyntax) == false)
                dlg.MarcSyntax = this.MainForm.LinkedMarcSyntax;

            this.MainForm.AppInfo.LinkFormState(dlg, "OpenMarcFileDlg_state");
            dlg.ShowDialog(this);
            if (dlg.DialogResult != DialogResult.OK)
                return 0;

            // �����ù����ļ���
            // 2009/9/21
            this.MainForm.LinkedMarcFileName = dlg.FileName;
            this.MainForm.LinkedEncodingName = dlg.EncodingName;
            this.MainForm.LinkedMarcSyntax = dlg.MarcSyntax;

            string strError = "";

            this.linkMarcFile = new LinkMarcFile();
            int nRet = this.linkMarcFile.Open(dlg.FileName,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.linkMarcFile.Encoding = dlg.Encoding;
            this.linkMarcFile.MarcSyntax = dlg.MarcSyntax;

            string strMarc = "";
            byte[] baRecord = null;
            //	    2	����(��ǰ���صļ�¼��Ч)
            nRet = this.linkMarcFile.NextRecord(out strMarc,
                out baRecord,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            // 2013/5/26
            if (nRet == 2)
                goto ERROR1;

            if (this.linkMarcFile.MarcSyntax == "<�Զ�>"
            || this.linkMarcFile.MarcSyntax.ToLower() == "<auto>")
            {
                // �Զ�ʶ��MARC��ʽ
                string strOutMarcSyntax = "";
                // ̽���¼��MARC��ʽ unimarc / usmarc / reader
                // return:
                //      0   û��̽�������strMarcSyntaxΪ��
                //      1   ̽�������
                nRet = MarcUtil.DetectMarcSyntax(strMarc,
                    out strOutMarcSyntax);
                this.linkMarcFile.MarcSyntax = strOutMarcSyntax;    // �п���Ϊ�գ���ʾ̽�ⲻ����
                if (String.IsNullOrEmpty(this.linkMarcFile.MarcSyntax) == true)
                {
                    MessageBox.Show(this, "����޷�ȷ����MARC�ļ���MARC��ʽ");
                }
            }

            if (dlg.Mode880 == true && linkMarcFile.MarcSyntax == "usmarc")
            {
                MarcRecord temp = new MarcRecord(strMarc);
                MarcQuery.ToParallel(temp);
                strMarc = temp.Text;
            }
            LoadLinkedMarcRecord(strMarc, baRecord);
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        void LoadLinkedMarcRecord(string strMarc,
            byte [] baRecord)
        {
            int nRet = 0;
            string strError = "";

            this.CurrentTimestamp = null;
            this.textBox_tempRecPath.Text = "marcfile:" + this.linkMarcFile.FileName + ":" + this.linkMarcFile.CurrentIndex.ToString();
            this.SavePath = "";
            this.CurrentEncoding = this.linkMarcFile.Encoding;

            string strMarcSyntax = this.linkMarcFile.MarcSyntax.ToLower();

            // 2009/9/21
            if (strMarcSyntax == "unimarc")
                this.AutoDetectedMarcSyntaxOID = "1.2.840.10003.5.1";   // UNIMARC
            else if (strMarcSyntax == "usmarc")
            {
                this.AutoDetectedMarcSyntaxOID = "1.2.840.10003.5.10";   // USMARC
                // this.MarcEditor.Lang = "en";
            }

            // װ��MARC�༭��
            this.MarcEditor.Marc = strMarc;


            this.CurrentRecord = null;
            
            DigitalPlatform.Z3950.Record record = new DigitalPlatform.Z3950.Record();
            if (strMarcSyntax == "unimarc" || strMarcSyntax == "")
                record.m_strSyntaxOID = "1.2.840.10003.5.1";
            else if (strMarcSyntax == "usmarc")
                record.m_strSyntaxOID = "1.2.840.10003.5.10";
            else if (strMarcSyntax == "dt1000reader")
                record.m_strSyntaxOID = "1.2.840.10003.5.dt1000reader";
            else
            {
                strError = "δ֪��MARC syntax '" + strMarcSyntax + "'";
                goto ERROR1;
            }

            record.m_baRecord = baRecord;

            this.CurrentRecord = record;

            DisplayHtml(strMarc, record.m_strSyntaxOID);

            {
                if (this.CurrentEncoding.Equals(this.MainForm.Marc8Encoding) == true)
                {
                }
                else
                {
                    if (baRecord == null)
                    {
                        baRecord = this.CurrentEncoding.GetBytes(strMarc);
                    }
                }

                if (baRecord != null)
                {
                    // װ������Ʊ༭��
                    this.binaryEditor_originData.SetData(
                        baRecord);
                }
                else
                {
                    // TODO: �Ƿ�Ҫ���ԭ������?
                }

                this.label_originDataWarning.Text = "";
                if (baRecord != null)
                {
                    // װ��ISO2709�ı�
                    nRet = this.Set2709OriginText(baRecord,
                        this.CurrentEncoding,
                        out strError);
                    if (nRet == -1)
                    {
                        this.textBox_originData.Text = strError;
                        this.label_originDataWarning.Text = "";
                    }
                }
            }

            this.MarcEditor.MarcDefDom = null; // ǿ��ˢ���ֶ�����ʾ
            this.MarcEditor.RefreshNameCaption();

            this.BiblioChanged = false;

            if (this.MarcEditor.FocusedFieldIndex == -1)
                this.MarcEditor.FocusedFieldIndex = 0;

            this.MarcEditor.Focus();
            return;
            ERROR1:
            MessageBox.Show(this, strError);
        }




        // ����
        // parameters:
        //      strSender   �����������Դ "toolbar" "ctrl_d"
        public int SearchDup(string strSender)
        {
            string strError = "";
            int nRet = 0;

            Debug.Assert(strSender == "toolbar" || strSender == "ctrl_d","");

            string strStartPath = this.SavePath;

            // ��鵱ǰͨѶЭ��
            string strProtocol = "";
            string strPath = "";

            if (String.IsNullOrEmpty(strStartPath) == false)
            {
                nRet = Global.ParsePath(strStartPath,
                    out strProtocol,
                    out strPath,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (strProtocol != "dp2library"
                    && strProtocol != "dtlp")
                    strStartPath = "";  // ��ʹ����ѡ�����·��
            }
            else
            {
                // ѡ��Э��
                SelectProtocolDialog protocol_dlg = new SelectProtocolDialog();
                GuiUtil.SetControlFont(protocol_dlg, this.Font);

                protocol_dlg.Protocols = new List<string>();
                protocol_dlg.Protocols.Add("dp2library");
                protocol_dlg.Protocols.Add("dtlp");
                protocol_dlg.StartPosition = FormStartPosition.CenterScreen;

                protocol_dlg.ShowDialog(this);

                if (protocol_dlg.DialogResult != DialogResult.OK)
                    return 0;

                strProtocol = protocol_dlg.SelectedProtocol;
            }




            this.EnableControls(false);
            try
            {

                // dtlpЭ��Ĳ���
                if (strProtocol.ToLower() == "dtlp")
                {
                    // TODO: �����ʼ·��Ϊ�գ���Ҫ��ϵͳ�����еõ�һ��ȱʡ����ʼ·��

                    DtlpSearchForm dtlp_searchform = this.GetDtlpSearchForm();

                    if (dtlp_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�DTLP���������޷����в���";
                        goto ERROR1;
                    }

                    // �򿪲��ش���
                    DtlpDupForm form = new DtlpDupForm();

                    // form.MainForm = this.MainForm;
                    form.MdiParent = this.MainForm;
                    form.LoadDetail -= new LoadDetailEventHandler(dtlpdupform_LoadDetail);
                    form.LoadDetail += new LoadDetailEventHandler(dtlpdupform_LoadDetail);

                    string strCfgFilename = this.MainForm.DataDir + "\\dtlp_dup.xml";
                    nRet = form.Initial(strCfgFilename,
                        this.MainForm.stopManager,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == 0)
                    {
                        // �����ļ������ڶ��ڱ༭�����ļ��Ľ����޴󰭣����ǶԲ���ȴӰ����Զ -- �޷�����
                        strError = "�����ļ� " + strCfgFilename + " �����ڡ����������˵�������/ϵͳ�������á�������ֶԻ����ѡ��DTLPЭ�顱����ҳ���������ط������á��������á�";
                        goto ERROR1;
                    }


                    form.RecordPath = strPath;
                    form.ProjectName = "{default}"; // "<Ĭ��>"
                    form.MarcRecord = this.MarcEditor.Marc;
                    form.DtlpChannel = dtlp_searchform.DtlpChannel;

                    form.AutoBeginSearch = true;

                    form.Show();

                    return 0;
                }
                else if (strProtocol.ToLower() == "dp2library")
                {
                        dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                        if (dp2_searchform == null)
                        {
                            strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷����в���";
                            goto ERROR1;
                        } 
                    if (String.IsNullOrEmpty(strStartPath) == true)
                    {
                        /*
                        strError = "��ǰ��¼·��Ϊ�գ��޷����в���";
                        goto ERROR1;
                         * */


                        string strDefaultStartPath = this.MainForm.DefaultSearchDupStartPath;

                        // ���ȱʡ���·������Ϊ�գ����߰���Control��ǿ��Ҫ����ֶԻ���
                        if (String.IsNullOrEmpty(strDefaultStartPath) == true
                            || (Control.ModifierKeys == Keys.Control && strSender == "toolbar"))
                        {
                            // ��Ϊ��װ��̬
                            if (String.IsNullOrEmpty(strDefaultStartPath) == false)
                                strDefaultStartPath = Global.GetForwardStyleDp2Path(strDefaultStartPath);

                            // ��ʱָ��һ��dp2library�����������ݿ�
                            GetDp2ResDlg dlg = new GetDp2ResDlg();
                            GuiUtil.SetControlFont(dlg, this.Font);

                            dlg.Text = "��ָ��һ�� dp2library ���ݿ⣬����Ϊģ��Ĳ������";
                            dlg.dp2Channels = dp2_searchform.Channels;
                            dlg.Servers = this.MainForm.Servers;
                            dlg.EnabledIndices = new int[] { dp2ResTree.RESTYPE_DB };
                            dlg.Path = strDefaultStartPath;  // �����������ϴ��ù���·��

                            this.MainForm.AppInfo.LinkFormState(dlg,
                                "searchdup_selectstartpath_dialog_state");

                            dlg.ShowDialog(this);

                            this.MainForm.AppInfo.UnlinkFormState(dlg);

                            if (dlg.DialogResult != DialogResult.OK)
                                return 0;

                            strDefaultStartPath = Global.GetBackStyleDp2Path(dlg.Path + "/?");

                            // �������õ�ϵͳ������
                            this.MainForm.DefaultSearchDupStartPath = strDefaultStartPath;
                        }

                        // strProtocol = "dp2library";
                        strPath = strDefaultStartPath;
                    }


                        //// 
                        /*
                        dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                        if (dp2_searchform == null)
                        {
                            strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷����в���";
                            goto ERROR1;
                        }*/

                    // ��strPath����Ϊserver url��local path��������
                    string strServerName = "";
                    string strPurePath = "";
                    dp2SearchForm.ParseRecPath(strPath,
                        out strServerName,
                        out strPurePath);

                    /*
                    // ���server url

                    string strServerUrl = dp2_searchform.GetServerUrl(strServerName);
                    if (strServerUrl == null)
                    {
                        strError = "δ���ҵ���Ϊ '" + strServerName + "' �ķ�����";
                        goto ERROR1;
                    }
                    */

                    string strDbName = dp2SearchForm.GetDbName(strPurePath);
                    string strSyntax = "";


                    // ���һ�����ݿ������syntax
                    // parameters:
                    //      stop    ���!=null����ʾʹ�����stop�����Ѿ�OnStop +=
                    //              ���==null����ʾ���Զ�ʹ��this.stop�����Զ�OnStop+=
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   found
                    nRet = dp2_searchform.GetDbSyntax(null, // this.stop, bug!!!
                        strServerName,
                        strDbName,
                        out strSyntax,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (String.IsNullOrEmpty(strSyntax) == true)
                        strSyntax = "unimarc";

                    string strXml = "";
                    // �����Ŀ��¼��XML��ʽ
                    // parameters:
                    //      strMarcSyntax Ҫ������XML��¼��marcsyntax��
                    /*
                    nRet = dp2SearchForm.GetBiblioXml(
                        strSyntax,
                        this.MarcEditor.Marc,
                        out strXml,
                        out strError);
                     * */
                    // 2008/5/16 changed
                    nRet = MarcUtil.Marc2Xml(
    this.MarcEditor.Marc,
    strSyntax,
    out strXml,
    out strError);

                    if (nRet == -1)
                        goto ERROR1;


                    // �򿪲��ش���
                    dp2DupForm form = new dp2DupForm();

                    form.MainForm = this.MainForm;
                    form.MdiParent = this.MainForm;

                    form.LibraryServerName = strServerName;
                    form.ProjectName = "<Ĭ��>";
                    form.XmlRecord = strXml;
                    form.RecordPath = strPurePath;

                    form.AutoBeginSearch = true;

                    form.Show();

                    return 0;
                }
                else if (strProtocol.ToLower() == "z3950")
                {
                    strError = "Ŀǰ�ݲ�֧�� Z39.50 Э��Ĳ���";
                    goto ERROR1;
                }
                else if (strProtocol.ToLower() == "amazon")
                {
                    strError = "Ŀǰ�ݲ�֧�� amazon Э��Ĳ���";
                    goto ERROR1;
                }
                else
                {
                    strError = "�޷�ʶ���Э���� '" + strProtocol + "'";
                    goto ERROR1;
                }

            }
            finally
            {
                this.EnableControls(true);
            }

        ERROR1:
            MessageBox.Show(this, strError);
            return -1;

        }

        // ���󡢴����Ƿ���Ч?
        public bool IsValid()
        {
            if (this.IsDisposed == true)
                return false;

            return true;
        }

        void dtlpdupform_LoadDetail(object sender, LoadDetailEventArgs e)
        {
            string strError = "";
            DtlpSearchForm dtlp_searchform = null;

            /*
            // Ҫ����this�Ϸ����Ϸ���
            if (this.IsValid() == false)
            {
                strError = "�����MarcDetailForm�Ѿ�����";
                MessageBox.Show(ForegroundWindow.Instance, strError);
                return;
            }*/

            if (this.LinkedSearchForm == null
                || this.LinkedSearchForm.IsValid() == false
                || !(this.LinkedSearchForm is DtlpSearchForm))
            {
                dtlp_searchform = this.GetDtlpSearchForm();

                if (dtlp_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�DTLP���������޷�����LoadRecord()";
                    goto ERROR1;
                }
            }
            else
            {
                dtlp_searchform = (DtlpSearchForm)this.LinkedSearchForm;
            }

            MarcDetailForm detail = new MarcDetailForm();

            detail.MdiParent = this.MainForm;   // ���ﲻ����this.MdiParent�����this�Ѿ��رգ�this.MainForm������ʹ��
            detail.MainForm = this.MainForm;
            detail.Show();
            int nRet = detail.LoadAmazonRecord(dtlp_searchform,
                e.RecordPath,
                "current",
                true);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        // �����¼
        // parameters:
        //      strStyle    "saveas"���  ����Ϊ��ͨ����
        public int SaveRecord(string strStyle = "save")
        {
            string strError = "";
            int nRet = 0;

            string strLastSavePath = MainForm.LastSavePath;
            if (String.IsNullOrEmpty(strLastSavePath) == false)
            {
                string strOutputPath = "";
                nRet = ChangePathToAppendStyle(strLastSavePath,
                    out strOutputPath,
                    out strError);
                if (nRet == -1)
                {
                    MainForm.LastSavePath = ""; // �����´μ������� 2011/3/4
                    goto ERROR1;
                }
                strLastSavePath = strOutputPath;
            }

            string strCurrentUserName = "";
            string strSavePath = this.SavePath == "" ? strLastSavePath : this.SavePath;

            if (strStyle == "save"
                && string.IsNullOrEmpty(this.SavePath) == false
                && (Control.ModifierKeys & Keys.Control) == 0)
            {
                // 2011/8/8
                // ����ʱ����Ѿ�����·�����Ͳ��ô򿪶Ի�����
            }
            else
            {
                SaveRecordDlg dlg = new SaveRecordDlg();
                GuiUtil.SetControlFont(dlg, this.Font);

                dlg.MainForm = this.MainForm;
                dlg.GetDtlpSearchParam += new GetDtlpSearchParamEventHandle(dlg_GetDtlpSearchParam);
                dlg.GetDp2SearchParam += new GetDp2SearchParamEventHandle(dlg_GetDp2SearchParam);
                if (strStyle == "save")
                    dlg.RecPath = this.SavePath == "" ? strLastSavePath : this.SavePath;
                else
                {
                    dlg.RecPath = strLastSavePath;  // 2011/6/19
                    dlg.Text = "����¼";
                }

                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.ShowDialog(this);
                if (dlg.DialogResult != DialogResult.OK)
                    return 0;

                MainForm.LastSavePath = dlg.RecPath;

                strSavePath = dlg.RecPath;
                strCurrentUserName = dlg.CurrentUserName;
            }


            /*
            if (String.IsNullOrEmpty(this.SavePath) == true)
            {
                strError = "ȱ������·��";
                goto ERROR1;
            }
             * */

            string strProtocol = "";
            string strPath = "";
            nRet = Global.ParsePath(strSavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.stop.BeginLoop();

            this.EnableControls(false);
            try
            {
                // dtlpЭ��ļ�¼����
                if (strProtocol.ToLower() == "dtlp")
                {
                    DtlpSearchForm dtlp_searchform = this.GetDtlpSearchForm();

                    if (dtlp_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�DTLP���������޷������¼";
                        goto ERROR1;
                    }

                    /*
                    string strOutPath = "";
                    nRet = DtlpChannel.CanonicalizeWritePath(strPath,
                        out strOutPath,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    strPath = strOutPath;
                     * */


                    string strOutputPath = "";
                    byte[] baOutputTimestamp = null;
                    nRet = dtlp_searchform.SaveMarcRecord(
                        strPath,
                        this.MarcEditor.Marc,
                        this.CurrentTimestamp,
                        out strOutputPath,
                        out baOutputTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // TODO: ʱ�����ͻ?

                    this.SavePath = strProtocol + ":" + strOutputPath;
                    this.CurrentTimestamp = baOutputTimestamp;

                    this.BiblioChanged = false;

                    // �Ƿ�ˢ��MARC��¼��
                    //AutoCloseMessageBox.Show(this, "����ɹ�");
                    // MessageBox.Show(this, "����ɹ�");
                    return 0;
                }
                else if (strProtocol.ToLower() == "dp2library")
                {
                    dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                    if (dp2_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷������¼";
                        goto ERROR1;
                    }

#if NO
                    // ��ʹ��¼һ��
                    if (string.IsNullOrEmpty(strCurrentUserName) == true
                        && string.IsNullOrEmpty(this.CurrentUserName) == true)
                    {
                        string strServerName = "";
                        string strLocalPath = "";

                        // ������¼·����
                        // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                        dp2SearchForm.ParseRecPath(strPath,
                            out strServerName,
                            out strLocalPath);

                        string strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);
                        string strSyntax = "";
                        nRet = dp2_searchform.GetDbSyntax(
    null,
    strServerName,
    strBiblioDbName,
    out strSyntax,
    out strError);
                    }
#endif

                    // ����ǰ��׼������
                    {
                        // ��ʼ�� dp2catalog_marc_autogen.cs �� Assembly����new MarcDetailHost����
                        // return:
                        //      -2  �����Assembly
                        //      -1  error
                        //      0   û�����³�ʼ��Assembly������ֱ������ǰCache��Assembly (���ܱ������ǿ�)
                        //      1   ����(�����״�)��ʼ����Assembly
                        nRet = InitialAutogenAssembly(out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (this.m_detailHostObj != null)
                        {
                            // ģ���this.SavePath 2011/11/22
                            string strOldSavePath = this.textBox_savePath.Text;
                            this.textBox_savePath.Text = strSavePath;
                            try
                            {
                                BeforeSaveRecordEventArgs e = new BeforeSaveRecordEventArgs();
                                e.CurrentUserName = strCurrentUserName;
                                this.m_detailHostObj.BeforeSaveRecord(this.MarcEditor, e);
                                if (string.IsNullOrEmpty(e.ErrorInfo) == false)
                                {
                                    MessageBox.Show(this, "����ǰ��׼������ʧ��: " + e.ErrorInfo + "\r\n\r\n����������Խ�����");
                                }
                            }
                            finally
                            {
                                // �ָ�this.SavePath
                                this.textBox_savePath.Text = strOldSavePath;
                            }
                        }
                    }

                    byte[] baTimestamp = this.CurrentTimestamp;
                    string strMARC = this.MarcEditor.Marc;
                    string strFragment = "";
                    if (this.domXmlFragment != null
                        && this.domXmlFragment.DocumentElement != null)
                        strFragment = this.domXmlFragment.DocumentElement.InnerXml;

                    // 2014/5/12
                    string strMarcSyntax = "";
                    if (this.CurrentRecord != null)
                        strMarcSyntax = GetMarcSyntax(this.CurrentRecord.m_strSyntaxOID);

                    // 2014/5/18
                    if (string.IsNullOrEmpty(this.AutoDetectedMarcSyntaxOID) == false)
                        strMarcSyntax = GetMarcSyntax(this.AutoDetectedMarcSyntaxOID);

                    string strComment = "";
                    bool bOverwrite = false;

                    if (string.IsNullOrEmpty(this.SavePath) == false)
                    {
                        string strTempProtocol = "";
                        string strTempPath = "";
                        nRet = Global.ParsePath(this.SavePath,
                            out strTempProtocol,
                            out strTempPath,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        string strServerName = "";
                        string strPurePath = "";

                        dp2SearchForm.ParseRecPath(strTempPath,
out strServerName,
out strPurePath);

                        if (dp2SearchForm.IsAppendRecPath(strPurePath) == false)
                        {
                            string strServerUrl = dp2_searchform.GetServerUrl(strServerName);
                            strComment = "copy from " + strPurePath + "@" + strServerUrl;
                        }
                    }
                    else if (string.IsNullOrEmpty(this.textBox_tempRecPath.Text) == false)
                    {
                        strComment = "copy from " + this.textBox_tempRecPath.Text;
                    }

                    string strRights = "";
                    // �ж��Ƿ�׷��
                    {
                        string strServerName = "";
                        string strPurePath = "";

                        dp2SearchForm.ParseRecPath(strPath,
out strServerName,
out strPurePath);
                        if (dp2SearchForm.IsAppendRecPath(strPurePath) == false)
                            bOverwrite = true;

                        nRet = dp2_searchform.GetChannelRights(
                                strServerName,
                                out strRights,
                                out strError);
                        if (nRet == -1)
                            goto ERROR1;

                    }

                    bool bForceWverifyData = StringUtil.IsInList("client_forceverifydata", strRights);

                    bool bVerifyed = false;
                    if (bForceWverifyData == true)
                    {
                        GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                        e1.FocusedControl = this.MarcEditor;

                        // 0: û�з���У�����; 1: ����У�龯��; 2: ����У�����
                        nRet = this.VerifyData(this, e1, strSavePath, true);
                        if (nRet == 2)
                        {
                            strError = "MARC ��¼��У�鷢���д����ܾ����档���޸� MARC ��¼�����±���";
                            goto ERROR1;
                        }
                        bVerifyed = true;
                    }

                REDO_SAVE_DP2:
                    string strOutputPath = "";
                    byte[] baOutputTimestamp = null;
                    // return:
                    //      -2  timestamp mismatch
                    //      -1  error
                    //      0   succeed
                    nRet = dp2_searchform.SaveMarcRecord(
                        true,
                        strPath,
                        strMARC,
                        strMarcSyntax,
                        baTimestamp,
                        strFragment,
                        strComment,
                        out strOutputPath,
                        out baOutputTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == -2)
                    {
                        // ʱ�����ͻ��

                        // װ��Ŀ���¼
                        DigitalPlatform.Z3950.Record record = null;
                        Encoding currentEncoding = null;
                        byte[] baTargetTimestamp = null;
                        string strOutStyle = "";
                        string strTargetMARC = "";
                        string strError1 = "";

                        string strOutputSavePath = "";
                        long lVersion = 0;
                        LoginInfo logininfo = null;
                        string strXmlFragment = "";

                        nRet = dp2_searchform.GetOneRecord(
                            // true,
                            "marc",
                            //strPath,    // �������ʺ�?
                            //"", // strDirection,
                            0,
                            "path:" + strPath,
                            "",
                            out strOutputSavePath,
                            out strTargetMARC,
                            out strXmlFragment,
                            out strOutStyle,
                            out baTargetTimestamp,
                            out lVersion,
                            out record,
                            out currentEncoding,
                            out logininfo,
                            out strError1);
                        if (nRet == -1)
                        {
                            strError = "�����¼ʱ��������: " + strError + "������װ��Ŀ���¼��ʱ���ַ�������: " + strError1;
                            goto ERROR1;
                        }

                        nRet = this.LoadXmlFragment(strXmlFragment,
out strError1);
                        if (nRet == -1)
                        {
                            strError1 = "�����¼ʱ��������: " + strError + "������װ��Ŀ���¼��ʱ���ַ�������: " + strError1;
                            goto ERROR1;
                        }

                        // TODO: ���Դ��Ŀ���MARC��ʽ�Ƿ�һ�£��Ƿ�ǰ������ˣ�
                        TwoBiblioDialog two_biblio_dlg = new TwoBiblioDialog();
                        GuiUtil.SetControlFont(two_biblio_dlg, this.Font);

                        two_biblio_dlg.Text = "������Ŀ��¼";
                        two_biblio_dlg.MessageText = "���������ǵ�Ŀ���¼��Դ���ݲ�ͬ��\r\n\r\n�����Ƿ�ȷ��Ҫ��Դ���ݸ���Ŀ���¼?";
                        two_biblio_dlg.LabelSourceText = "Դ";
                        two_biblio_dlg.LabelTargetText = "Ŀ�� " + strPath;
                        two_biblio_dlg.MarcSource = strMARC;
                        two_biblio_dlg.MarcTarget = strTargetMARC;
                        two_biblio_dlg.ReadOnlyTarget = true;   // ��ʼʱĿ��MARC�༭�����ý����޸�

                        this.MainForm.AppInfo.LinkFormState(two_biblio_dlg, "TwoBiblioDialog_state");
                        two_biblio_dlg.ShowDialog(this);
                        this.MainForm.AppInfo.UnlinkFormState(two_biblio_dlg);

                        if (two_biblio_dlg.DialogResult == DialogResult.Cancel)
                        {
                            strError = "��������";
                            goto ERROR1;
                            // return 0;   // ȫ������
                        }

                        if (two_biblio_dlg.DialogResult == DialogResult.No)
                        {
                            strError = "��������";
                            goto ERROR1;
                        }

                        if (two_biblio_dlg.EditTarget == false)
                            strMARC = two_biblio_dlg.MarcSource;
                        else
                            strMARC = two_biblio_dlg.MarcTarget;

                        baTimestamp = baTargetTimestamp;
                        goto REDO_SAVE_DP2;
                    }

                    this.SavePath = dp2_searchform.CurrentProtocol + ":" + strOutputPath;
                    this.CurrentTimestamp = baOutputTimestamp;

                    this.BiblioChanged = false;

                    this.MarcEditor.ClearMarcDefDom();
                    this.MarcEditor.RefreshNameCaption();


                    // �Ƿ�ˢ��MARC��¼��
                    // MessageBox.Show(this, "����ɹ�");

                    if (bOverwrite == true
                        && this.LinkedSearchForm != null)
                    {
                        // return:
                        //      -2  ��֧��
                        //      -1  error
                        //      0   ��ش����Ѿ����٣�û�б�Ҫˢ��
                        //      1   �Ѿ�ˢ��
                        //      2   �ڽ������û���ҵ�Ҫˢ�µļ�¼
                        nRet = RefreshCachedRecord("refresh",
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(this, "��¼�����Ѿ��ɹ�����ˢ����ؽ�����ڼ�¼ʱ����: " + strError);
                    }

                    if (this.AutoVerifyData == true
                        && bVerifyed == false)
                    {
                        // API.PostMessage(this.Handle, WM_VERIFY_DATA, 0, 0);

                        GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                        e1.FocusedControl = this.MarcEditor;

                        // 0: û�з���У�����; 1: ����У�龯��; 2: ����У�����
                        nRet = this.VerifyData(this, e1, strSavePath, true);
                        if (nRet == 2)
                        {
                            strError = "MARC ��¼��У�鷢���д���¼�Ѿ����档���޸� MARC ��¼�����±���";
                            MessageBox.Show(this, strError);
                        }
                    }

                    return 0;
                }
                else if (strProtocol.ToLower() == "unioncatalog")
                {
                    string strServerName = "";
                    string strPurePath = "";
                    dp2SearchForm.ParseRecPath(strPath,
                        out strServerName,
                        out strPurePath);
                    if (String.IsNullOrEmpty(strServerName) == true)
                    {
                        strError = "·�����Ϸ�: ȱ��������������";
                        goto ERROR1;
                    }
                    if (String.IsNullOrEmpty(strPurePath) == true)
                    {
                        strError = "·�����Ϸ���ȱ����·������";
                        goto ERROR1;
                    }

                    byte[] baTimestamp = this.CurrentTimestamp;
                    string strMARC = this.MarcEditor.Marc;
                    string strMarcSyntax = "";

                    string strMarcSyntaxOID = this.GetCurrentMarcSyntaxOID(out strError);
                    if (String.IsNullOrEmpty(strMarcSyntaxOID) == true)
                    {
                        strError = "��ǰMARC syntax OIDΪ�գ��޷��ж�MARC�����ʽ";
                        goto ERROR1;
                    }

                    if (strMarcSyntaxOID == "1.2.840.10003.5.1")
                        strMarcSyntax = "unimarc";
                    if (strMarcSyntaxOID == "1.2.840.10003.5.10")
                        strMarcSyntax = "usmarc";

                    string strXml = "";

                    nRet = MarcUtil.Marc2Xml(
                        strMARC,
                        strMarcSyntax,
                        out strXml,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    string strXml1 = "";
                    // ������ʹ�õ�marcxml��ʽת��Ϊmarcxchange��ʽ
                    nRet = MarcUtil.MarcXmlToXChange(strXml,
                        null,
                        out strXml1,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // TODO: �Ƿ����ֱ��ʹ��Z39.50���ԶԻ����е��û���������? ��¼ʧ�ܺ�ų��ֵ�¼�Ի���
                    if (this.LoginInfo == null)
                        this.LoginInfo = new dp2Catalog.LoginInfo();

                    bool bRedo = false;
                    REDO_LOGIN:
                    if (string.IsNullOrEmpty(this.LoginInfo.UserName) == true
                        || bRedo == true)
                    {
                        LoginDlg login_dlg = new LoginDlg();
                        GuiUtil.SetControlFont(login_dlg, this.Font);

                        if (bRedo == true)
                            login_dlg.Comment = strError + "\r\n\r\n�����µ�¼";
                        else
                            login_dlg.Comment = "��ָ���û���������";
                        login_dlg.UserName = this.LoginInfo.UserName;
                        login_dlg.Password = this.LoginInfo.Password;
                        login_dlg.SavePassword = true;
                        login_dlg.ServerUrl = strServerName;
                        login_dlg.StartPosition = FormStartPosition.CenterScreen;
                        login_dlg.ShowDialog(this);

                        if (login_dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                        {
                            strError = "��������";
                            goto ERROR1;
                        }

                        this.LoginInfo.UserName = login_dlg.UserName;
                        this.LoginInfo.Password = login_dlg.Password;
                        strServerName = login_dlg.ServerUrl;
                    }

                    if (this.LoginInfo.UserName.IndexOf("/") != -1)
                    {
                        strError = "�û����в��ܳ����ַ� '/'";
                        goto ERROR1;
                    }

                    string strOutputTimestamp = "";
                    string strOutputRecPath = "";
                    // parameters:
                    //      strAction   ������Ϊ"new" "change" "delete" "onlydeletebiblio"֮һ��"delete"��ɾ����Ŀ��¼��ͬʱ�����Զ�ɾ��������ʵ���¼������Ҫ��ʵ���δ���������ɾ����
                    // return:
                    //      -2  ��¼���ɹ�
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = UnionCatalog.UpdateRecord(
                        null,
                        strServerName,
                        this.LoginInfo.UserName + "/" + this.LoginInfo.Password,
                        dp2SearchForm.IsAppendRecPath(strPurePath) == true ? "new": "change",
                        strPurePath,
                        "marcxchange",
                        strXml1,
                        ByteArray.GetHexTimeStampString(baTimestamp),
                        out strOutputRecPath,
                        out strOutputTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == -2)
                    {
                        bRedo = true;
                        goto REDO_LOGIN;
                    }

                    this.CurrentTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);
                    this.SavePath = strProtocol + ":" + strOutputRecPath + "@" + strServerName;

                    this.BiblioChanged = false;

                    this.MarcEditor.ClearMarcDefDom();
                    this.MarcEditor.RefreshNameCaption();


                    // �Ƿ�ˢ��MARC��¼��
                    // MessageBox.Show(this, "����ɹ�");

                    if (dp2SearchForm.IsAppendRecPath(strPurePath) == false
                        && this.LinkedSearchForm != null
                        && this.LinkedSearchForm is ZSearchForm)
                    {
                        nRet = RefreshCachedRecord("refresh",
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(this, "��¼�����Ѿ��ɹ�����ˢ����ؽ�����ڼ�¼ʱ����: " + strError);
                    }

                    return 0;
                }
                else if (strProtocol.ToLower() == "z3950")
                {
                    strError = "Ŀǰ�ݲ�֧�� Z39.50 Э��ı������";
                    goto ERROR1;
                }
                else if (strProtocol.ToLower() == "amazon")
                {
                    strError = "Ŀǰ�ݲ�֧�� amazon Э��ı������";
                    goto ERROR1;
                }
                else
                {
                    strError = "�޷�ʶ���Э���� '" + strProtocol + "'";
                    goto ERROR1;
                }

            }
            finally
            {
                this.stop.EndLoop();

                this.EnableControls(true);
            }

            // return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        DtlpSearchForm GetDtlpSearchForm()
        {
            DtlpSearchForm dtlp_searchform = null;

            if (this.LinkedSearchForm != null
                && this.LinkedSearchForm is DtlpSearchForm)
            {
                dtlp_searchform = (DtlpSearchForm)this.LinkedSearchForm;
            }
            else
            {
                dtlp_searchform = this.MainForm.TopDtlpSearchForm;

                if (dtlp_searchform == null)
                {
                    // �¿�һ��dtlp������
                    FormWindowState old_state = this.WindowState;

                    dtlp_searchform = new DtlpSearchForm();
                    dtlp_searchform.MainForm = this.MainForm;
                    dtlp_searchform.MdiParent = this.MainForm;
                    dtlp_searchform.WindowState = FormWindowState.Minimized;
                    dtlp_searchform.Show();

                    this.WindowState = old_state;
                    this.Activate();

                    // ��Ҫ�ȴ���ʼ�������������
                    dtlp_searchform.WaitLoadFinish();

                }
            }

            return dtlp_searchform;
        }

        dp2SearchForm GetDp2SearchForm()
        {
            dp2SearchForm dp2_searchform = null;

            if (this.LinkedSearchForm != null
                && this.LinkedSearchForm.IsValid() == true   // 2008/3/17
                && this.LinkedSearchForm is dp2SearchForm)
            {
                dp2_searchform = (dp2SearchForm)this.LinkedSearchForm;
            }
            else
            {
                dp2_searchform = this.MainForm.TopDp2SearchForm;

                if (dp2_searchform == null)
                {
                    // �¿�һ��dp2������
                    FormWindowState old_state = this.WindowState;

                    dp2_searchform = new dp2SearchForm();
                    dp2_searchform.MainForm = this.MainForm;
                    dp2_searchform.MdiParent = this.MainForm;
                    dp2_searchform.WindowState = FormWindowState.Minimized;
                    dp2_searchform.Show();

                    // 2008/3/17
                    this.WindowState = old_state;
                    this.Activate();

                    // ��Ҫ�ȴ���ʼ�������������
                    dp2_searchform.WaitLoadFinish();
                }
            }

            return dp2_searchform;
        }

        AmazonSearchForm GetAmazonSearchForm()
        {
            AmazonSearchForm searchform = null;

            if (this.LinkedSearchForm != null
                && this.LinkedSearchForm.IsValid() == true   // 2008/3/17
                && this.LinkedSearchForm is AmazonSearchForm)
            {
                searchform = (AmazonSearchForm)this.LinkedSearchForm;
            }
            else
            {
                searchform = this.MainForm.TopAmazonSearchForm;

                if (searchform == null)
                {
                    // �¿�һ��dp2������
                    FormWindowState old_state = this.WindowState;

                    searchform = new AmazonSearchForm();
                    searchform.MainForm = this.MainForm;
                    searchform.MdiParent = this.MainForm;
                    searchform.WindowState = FormWindowState.Minimized;
                    searchform.Show();

                    // 2008/3/17
                    this.WindowState = old_state;
                    this.Activate();

                    // ��Ҫ�ȴ���ʼ�������������
                    // dp2_searchform.WaitLoadFinish();
                }
            }

            return searchform;
        }


        void dlg_GetDp2SearchParam(object sender, GetDp2SearchParamEventArgs e)
        {
            dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

            e.dp2Channels = dp2_searchform.Channels;
            e.MainForm = this.MainForm;
        }

        void dlg_GetDtlpSearchParam(object sender, GetDtlpSearchParamEventArgs e)
        {
            DtlpSearchForm dtlp_searchform = this.GetDtlpSearchForm();

            e.DtlpChannels = dtlp_searchform.DtlpChannels;
            e.DtlpChannel = dtlp_searchform.DtlpChannel;
        }

        public int GetAccessPoint()
        {
            string strError = "";

            if (String.IsNullOrEmpty(this.SavePath) == true)
            {
                strError = "ȱ������·��";
                goto ERROR1;
            }

            string strProtocol = "";
            string strPath = "";
            int nRet = Global.ParsePath(this.SavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.EnableControls(false);
            try
            {
                // dtlpЭ��
                if (strProtocol.ToLower() == "dtlp")
                {
                    DtlpSearchForm dtlp_searchform = this.GetDtlpSearchForm();

                    if (dtlp_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�DTLP���������޷��۲������";
                        goto ERROR1;
                    }

                    List<string> results = null;

                    // string strOutputPath = "";
                    // byte[] baOutputTimestamp = null;
                    nRet = dtlp_searchform.GetAccessPoint(
                        strPath,
                        this.MarcEditor.Marc,
                        out results,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    ViewAccessPointForm form = new ViewAccessPointForm();

                    form.MdiParent = this.MdiParent;
                    form.AccessPoints = results;
                    form.Show();

                    /*
                    string strText = "";

                    if (results != null)
                    {
                        for (int i = 0; i < results.Count; i++)
                        {
                            strText += results[i] + "\r\n";
                        }
                    }

                    MessageBox.Show(this, strText);
                     * */
                    return 1;
                }
                // dp2libraryЭ��
                else if (strProtocol.ToLower() == "dp2library")
                {

                    dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                    if (dp2_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷��������ݴ���";
                        goto ERROR1;
                    }


                    return 1;
                }
                else if (strProtocol.ToLower() == "z3950")
                {
                    strError = "Ŀǰ�ݲ�֧�� Z39.50 Э��Ĺ۲������Ĳ���";
                    goto ERROR1;
                }
                else if (strProtocol.ToLower() == "amazon")
                {
                    strError = "Ŀǰ�ݲ�֧�� amazon Э��Ĺ۲������Ĳ���";
                    goto ERROR1;
                }
                else
                {
                    strError = "�޷�ʶ���Э���� '" + strProtocol + "'";
                    goto ERROR1;
                }

            }
            finally
            {
                this.EnableControls(true);
            }

            // return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // ɾ����¼
        // TODO: ��Ҫ���Ӷ�dp2��UnionCatalogЭ���ɾ������
        public int DeleteRecord()
        {
            string strError = "";

            if (String.IsNullOrEmpty(this.SavePath) == true)
            {
                strError = "ȱ������·��";
                goto ERROR1;
            }

            string strProtocol = "";
            string strPath = "";
            int nRet = Global.ParsePath(this.SavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string strChangedWarning = "";

            if (this.ObjectChanged == true
                || this.BiblioChanged == true)
            {
                strChangedWarning = "��ǰ�� "
                    + GetCurrentChangedPartName()
                    // strChangedWarning
                + " ���޸Ĺ���\r\n\r\n";
            }

            string strText = strChangedWarning;

            strText += "ȷʵҪɾ����Ŀ��¼ \r\n" + strPath + " ";

            /*
            int nObjectCount = this.binaryResControl1.ObjectCount;
            if (nObjectCount != 0)
                strText += "�ʹ����� " + nObjectCount.ToString() + " ������";
             * */

            strText += " ?";

            // ����ɾ��
            DialogResult result = MessageBox.Show(this,
                strText,
                "MarcDetailForm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return 0;
            }

            this.stop.BeginLoop();

            this.EnableControls(false);
            try
            {

                // dtlpЭ��ļ�¼ɾ��
                if (strProtocol.ToLower() == "dtlp")
                {
                    DtlpSearchForm dtlp_searchform = this.GetDtlpSearchForm();

                    if (dtlp_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�DTLP���������޷������¼";
                        goto ERROR1;
                    }

                    // string strOutputPath = "";
                    // byte[] baOutputTimestamp = null;
                    nRet = dtlp_searchform.DeleteMarcRecord(
                        strPath,
                        this.CurrentTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    MessageBox.Show(this, "ɾ���ɹ�");
                    return 1;
                }
                // dp2libraryЭ��ļ�¼ɾ��
                else if (strProtocol.ToLower() == "dp2library")
                {

                    dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                    if (dp2_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷��������ݴ���";
                        goto ERROR1;
                    }



                    // string strOutputPath = "";
                    byte[] baOutputTimestamp = null;
                    // ɾ��һ��MARC/XML��¼
                    // parameters:
                    //      strSavePath ����Ϊ"����ͼ��/1@���ط�����"��û��Э�������֡�
                    // return:
                    //      -1  error
                    //      0   suceed
                    nRet = dp2_searchform.DeleteOneRecord(
                        strPath,
                        this.CurrentTimestamp,
                        out baOutputTimestamp,
                        out strError);
                    this.CurrentTimestamp = baOutputTimestamp;  // ���㷢������ҲҪ����ʱ������Ա�������ɾ��
                    if (nRet == -1)
                        goto ERROR1;

                    this.ObjectChanged = false;
                    this.BiblioChanged = false;

                    MessageBox.Show(this, "ɾ���ɹ�");
                    // TODO: ZSearchForm�еļ�¼�Ƿ�ҲҪ���?

                    if (this.LinkedSearchForm != null
    && this.LinkedSearchForm is ZSearchForm)
                    {
                        nRet = RefreshCachedRecord("delete",
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(this, "��¼ɾ���Ѿ��ɹ�����ˢ����ؽ�����ڼ�¼ʱ����: " + strError);
                    }

                    return 1;
                }
                else if (strProtocol.ToLower() == "unioncatalog")
                {
                    string strServerName = "";
                    string strPurePath = "";
                    dp2SearchForm.ParseRecPath(strPath,
                        out strServerName,
                        out strPurePath);
                    if (String.IsNullOrEmpty(strServerName) == true)
                    {
                        strError = "·�����Ϸ�: ȱ��������������";
                        goto ERROR1;
                    }
                    if (String.IsNullOrEmpty(strPurePath) == true)
                    {
                        strError = "·�����Ϸ���ȱ����·������";
                        goto ERROR1;
                    }

                    byte[] baTimestamp = this.CurrentTimestamp;

                    // TODO: �Ƿ����ֱ��ʹ��Z39.50���ԶԻ����е��û���������? ��¼ʧ�ܺ�ų��ֵ�¼�Ի���
                    if (this.LoginInfo == null)
                        this.LoginInfo = new dp2Catalog.LoginInfo();

                    bool bRedo = false;
                REDO_LOGIN:
                    if (string.IsNullOrEmpty(this.LoginInfo.UserName) == true
                        || bRedo == true)
                    {
                        LoginDlg login_dlg = new LoginDlg();
                        GuiUtil.SetControlFont(login_dlg, this.Font);
                        if (bRedo == true)
                            login_dlg.Comment = strError + "\r\n\r\n�����µ�¼";
                        else
                            login_dlg.Comment = "��ָ���û���������";
                        login_dlg.UserName = this.LoginInfo.UserName;
                        login_dlg.Password = this.LoginInfo.Password;
                        login_dlg.SavePassword = true;
                        login_dlg.ServerUrl = strServerName;
                        login_dlg.StartPosition = FormStartPosition.CenterScreen;
                        login_dlg.ShowDialog(this);

                        if (login_dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                        {
                            strError = "��������";
                            goto ERROR1;
                        }

                        this.LoginInfo.UserName = login_dlg.UserName;
                        this.LoginInfo.Password = login_dlg.Password;
                        strServerName = login_dlg.ServerUrl;
                    }

                    if (this.LoginInfo.UserName.IndexOf("/") != -1)
                    {
                        strError = "�û����в��ܳ����ַ� '/'";
                        goto ERROR1;
                    }

                    string strOutputTimestamp = "";
                    string strOutputRecPath = "";
                    // parameters:
                    //      strAction   ������Ϊ"new" "change" "delete" "onlydeletebiblio"֮һ��"delete"��ɾ����Ŀ��¼��ͬʱ�����Զ�ɾ��������ʵ���¼������Ҫ��ʵ���δ���������ɾ����
                    // return:
                    //      -2  ��¼���ɹ�
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = UnionCatalog.UpdateRecord(
                        null,
                        strServerName,
                        this.LoginInfo.UserName + "/" + this.LoginInfo.Password,
                        "delete",
                        strPurePath,
                        "", // format
                        null,
                        ByteArray.GetHexTimeStampString(baTimestamp),
                        out strOutputRecPath,
                        out strOutputTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == -2)
                    {
                        bRedo = true;
                        goto REDO_LOGIN;
                    }

                    this.CurrentTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);

                    this.BiblioChanged = false;
                    this.ObjectChanged = false;

                    MessageBox.Show(this, "ɾ���ɹ�");

                    if (this.LinkedSearchForm != null
    && this.LinkedSearchForm is ZSearchForm)
                    {
                        nRet = RefreshCachedRecord("delete",
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(this, "��¼ɾ���Ѿ��ɹ�����ˢ����ؽ�����ڼ�¼ʱ����: " + strError);
                    }


                    return 0;
                }
                else if (strProtocol.ToLower() == "z3950")
                {
                    strError = "Ŀǰ�ݲ�֧�� Z39.50 Э���ɾ������";
                    goto ERROR1;
                }
                else if (strProtocol.ToLower() == "amazon")
                {
                    strError = "Ŀǰ�ݲ�֧�� amazon Э���ɾ������";
                    goto ERROR1;
                }
                else
                {
                    strError = "�޷�ʶ���Э���� '" + strProtocol + "'";
                    goto ERROR1;
                }

            }
            finally
            {
                this.stop.EndLoop();

                this.EnableControls(true);
            }

            // return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        public void EnableControls(bool bEnable)
        {
            this.textBox_tempRecPath.Enabled = bEnable;
            this.MarcEditor.Enabled = bEnable;
        }

        void LoadFontToMarcEditor()
        {
            string strFaceName = MainForm.AppInfo.GetString("marceditor",
                "fontface",
                GuiUtil.GetDefaultEditorFontName());  // "Arial Unicode MS"
            float fFontSize = (float)Convert.ToDouble(MainForm.AppInfo.GetString("marceditor",
                "fontsize",
                "12.0"));

            string strColor = MainForm.AppInfo.GetString("marceditor",
                "fontcolor",
                "");

            string strStyle = MainForm.AppInfo.GetString("marceditor",
                "fontstyle",
                "");
            FontStyle style = FontStyle.Regular;
            if (String.IsNullOrEmpty(strStyle) == false)
            {
                style = (FontStyle)Enum.Parse(typeof(FontStyle), strStyle, true);
            }

            if (String.IsNullOrEmpty(strColor) == false)
            {
                this.MarcEditor.ContentTextColor = ColorUtil.String2Color(strColor);
            }

            this.MarcEditor.Font = new Font(strFaceName, fFontSize, style);

            this.MarcEditor.EnterAsAutoGenerate = MainForm.AppInfo.GetBoolean(
                "marceditor",
                "EnterAsAutoGenerate",
                false);

        }

        void SaveFontForMarcEditor()
        {
            MainForm.AppInfo.SetString("marceditor",
                "fontface",
                this.MarcEditor.Font.FontFamily.Name);
            MainForm.AppInfo.SetString("marceditor",
                "fontsize",
                Convert.ToString(this.MarcEditor.Font.Size));

            //            string strStyle = Enum.GetName(typeof(FontStyle), this.MarcEditor.Font.Style);
            string strStyle = this.MarcEditor.Font.Style.ToString();


            MainForm.AppInfo.SetString("marceditor",
                "fontstyle",
                strStyle);


            MainForm.AppInfo.SetString("marceditor",
                "fontcolor",
                this.MarcEditor.ContentTextColor != DigitalPlatform.Marc.MarcEditor.DefaultBackColor ? ColorUtil.Color2String(this.MarcEditor.ContentTextColor) : "");
        }

        // ��������
        public void SetFont()
        {
            FontDialog dlg = new FontDialog();

            dlg.ShowColor = true;
            dlg.Color = this.MarcEditor.ContentTextColor;
            dlg.Font = this.MarcEditor.Font;
            dlg.ShowApply = true;
            dlg.ShowHelp = true;
            dlg.AllowVerticalFonts = false;

            dlg.Apply -= new EventHandler(dlgFont_Apply);
            dlg.Apply += new EventHandler(dlgFont_Apply);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            this.MarcEditor.Font = dlg.Font;
            this.MarcEditor.ContentTextColor = dlg.Color;

            // ���浽�����ļ�
            SaveFontForMarcEditor();
        }

        void dlgFont_Apply(object sender, EventArgs e)
        {
            FontDialog dlg = (FontDialog)sender;

            this.MarcEditor.Font = dlg.Font;
            this.MarcEditor.ContentTextColor = dlg.Color;

            // ���浽�����ļ�
            SaveFontForMarcEditor();
        }

        #region Ϊ���ּ�ƴ����ع���



#if NO
                // ���ַ����еĺ���ת��Ϊ�ĽǺ���
        // parameters:
        //      bLocal  �Ƿ�ӱ��ػ�ȡ�ĽǺ���
        // return:
        //      -1  ����
        //      0   �û�ϣ���ж�
        //      1   ����
        public int HanziTextToSjhm(
            bool bLocal,
            string strText,
            out List<string> sjhms,
            out string strError)
        {
            strError = "";
            sjhms = new List<string>();

            // string strSpecialChars = "���������������������������������ۣݡ����������������ܣ�������������";


            for (int i = 0; i < strText.Length; i++)
            {
                char ch = strText[i];

                if (StringUtil.IsHanzi(ch) == false)
                    continue;

                // �����Ƿ��������
                if (StringUtil.SpecialChars.IndexOf(ch) != -1)
                {
                    continue;
                }

                // ����
                string strHanzi = "";
                strHanzi += ch;


                string strResultSjhm = "";

                int nRet = 0;

                if (bLocal == true)
                {
                    nRet = this.MainForm.LoadQuickSjhm(true, out strError);
                    if (nRet == -1)
                        return -1;
                    nRet = this.MainForm.QuickSjhm.GetSjhm(
                        strHanzi,
                        out strResultSjhm,
                        out strError);
                }
                else
                {
                    throw new Exception("�ݲ�֧�ִ�ƴ�����л�ȡ�ĽǺ���");
                    /*
                    nRet = GetOnePinyin(strHanzi,
                         out strResultPinyin,
                         out strError);
                     * */
                }
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {	// canceled
                    return 0;
                }

                Debug.Assert(strResultSjhm != "", "");

                strResultSjhm = strResultSjhm.Trim();
                sjhms.Add(strResultSjhm);
            }

            return 1;   // ��������
        }

        GcatServiceClient m_gcatClient = null;
        string m_strPinyinGcatID = "";
        bool m_bSavePinyinGcatID = false;

        // �����ַ���ת��Ϊƴ��
        // ����������Ѿ�MessageBox������strError��һ�ַ���Ϊ�ո�
        // return:
        //      -1  ����
        //      0   �û�ϣ���ж�
        //      1   ����
        public int SmartHanziTextToPinyin(
            string strText,
            PinyinStyle style,
            out string strPinyin,
            out string strError)
        {
            strPinyin = "";
            strError = "";

            Stop new_stop = new DigitalPlatform.Stop();
            new_stop.Register(MainForm.stopManager, true);	// ����������
            new_stop.OnStop += new StopEventHandler(new_stop_OnStop);
            new_stop.Initial("���ڻ�� '" + strText + "' ��ƴ����Ϣ (�ӷ����� " + this.MainForm.PinyinServerUrl + ")...");
            new_stop.BeginLoop();

            m_gcatClient = null;
            try
            {

                m_gcatClient = GcatNew.CreateChannel(this.MainForm.PinyinServerUrl);

            REDO_GETPINYIN:
                int nStatus = -1;	// ǰ��һ���ַ������� -1:ǰ��û���ַ� 0:��ͨӢ����ĸ 1:�ո� 2:����
                string strPinyinXml = "";
                // return:
                //      -2  strID��֤ʧ��
                //      -1  ����
                //      0   �ɹ�
                int nRet = GcatNew.GetPinyin(
                    new_stop,
                    m_gcatClient,
                    m_strPinyinGcatID,
                    strText,
                    out strPinyinXml,
                    out strError);
                if (nRet == -1)
                {
                    DialogResult result = MessageBox.Show(this,
    "�ӷ����� '" + this.MainForm.PinyinServerUrl + "' ��ȡƴ���Ĺ��̳���:\r\n" + strError + "\r\n\r\n�Ƿ�Ҫ��ʱ��Ϊʹ�ñ�����ƴ������? \r\n\r\n(ע����ʱ���ñ���ƴ����״̬�ڳ����˳�ʱ���ᱣ�������Ҫ���ø��ñ���ƴ����ʽ����ʹ�����˵��ġ��������á��������������������ҳ�ġ�ƴ��������URL���������)",
    "EntityForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        this.MainForm.ForceUseLocalPinyinFunc = true;
                        strError = "�����ñ���ƴ���������²���һ�Ρ�(���β�������: " + strError + ")";
                        return -1;
                    }
                    strError = " " + strError;
                    return -1;
                }

                if (nRet == -2)
                {
                    IdLoginDialog login_dlg = new IdLoginDialog();
                    login_dlg.Text = "���ƴ�� -- "
                        + ((string.IsNullOrEmpty(this.m_strPinyinGcatID) == true) ? "������ID" : strError);
                    login_dlg.ID = this.m_strPinyinGcatID;
                    login_dlg.SaveID = this.m_bSavePinyinGcatID;
                    login_dlg.StartPosition = FormStartPosition.CenterScreen;
                    if (login_dlg.ShowDialog(this) == DialogResult.Cancel)
                    {
                        return 0;
                    }

                    this.m_strPinyinGcatID = login_dlg.ID;
                    this.m_bSavePinyinGcatID = login_dlg.SaveID;
                    goto REDO_GETPINYIN;
                }

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strPinyinXml);
                }
                catch (Exception ex)
                {
                    strError = "strPinyinXmlװ�ص�XMLDOMʱ����: " + ex.Message;
                    return -1;
                }

                foreach (XmlNode nodeWord in dom.DocumentElement.ChildNodes)
                {
                    if (nodeWord.NodeType == XmlNodeType.Text)
                    {
                        SelPinyinDlg.AppendText(ref strPinyin, nodeWord.InnerText);
                        nStatus = 0;
                        continue;
                    }

                    if (nodeWord.NodeType != XmlNodeType.Element)
                        continue;

                    string strWordPinyin = DomUtil.GetAttr(nodeWord, "p");
                    if (string.IsNullOrEmpty(strWordPinyin) == false)
                        strWordPinyin = strWordPinyin.Trim();

                    // Ŀǰֻȡ���׶����ĵ�һ��
                    nRet = strWordPinyin.IndexOf(";");
                    if (nRet != -1)
                        strWordPinyin = strWordPinyin.Substring(0, nRet).Trim();

                    string[] pinyin_parts = strWordPinyin.Split(new char[] { ' ' });
                    int index = 0;
                    // ��ѡ�������
                    foreach (XmlNode nodeChar in nodeWord.ChildNodes)
                    {
                        if (nodeChar.NodeType == XmlNodeType.Text)
                        {
                            SelPinyinDlg.AppendText(ref strPinyin, nodeChar.InnerText);
                            nStatus = 0;
                            continue;
                        }

                        string strHanzi = nodeChar.InnerText;
                        string strCharPinyins = DomUtil.GetAttr(nodeChar, "p");

                        if (String.IsNullOrEmpty(strCharPinyins) == true)
                        {
                            strPinyin += strHanzi;
                            nStatus = 0;
                            index++;
                            continue;
                        }

                        if (strCharPinyins.IndexOf(";") == -1)
                        {
                            DomUtil.SetAttr(nodeChar, "sel", strCharPinyins);
                            SelPinyinDlg.AppendPinyin(ref strPinyin,
                                SelPinyinDlg.ConvertSinglePinyinByStyle(
                                    strCharPinyins,
                                    style)
                                    );
                            nStatus = 2;
                            index++;
                            continue;
                        }

#if _TEST_PINYIN
                        // ���ԣ�
                        string[] parts = strCharPinyins.Split(new char[] {';'});
                        {
                            DomUtil.SetAttr(nodeChar, "sel", parts[0]);
                            AppendPinyin(ref strPinyin, parts[0]);
                            nStatus = 2;
                            index++;
                            continue;
                        }
#endif


                        string strSampleText = "";
                        int nOffs = -1;
                        SelPinyinDlg.GetOffs(dom.DocumentElement,
                            nodeChar,
                out strSampleText,
                out nOffs);

                        {	// ����Ƕ��ƴ��
                            SelPinyinDlg dlg = new SelPinyinDlg();
                            float ratio_single = dlg.listBox_multiPinyin.Font.SizeInPoints / dlg.Font.SizeInPoints;
                            float ratio_sample = dlg.textBox_sampleText.Font.SizeInPoints / dlg.Font.SizeInPoints;
                            GuiUtil.SetControlFont(dlg, this.Font, false);
                            // ά�������ԭ�д�С������ϵ
                            dlg.listBox_multiPinyin.Font = new Font(dlg.Font.FontFamily, ratio_single * dlg.Font.SizeInPoints, GraphicsUnit.Point);
                            dlg.textBox_sampleText.Font = new Font(dlg.Font.FontFamily, ratio_sample * dlg.Font.SizeInPoints, GraphicsUnit.Point);
                            // ����Ի���Ƚ����� GuiUtil.SetControlFont(dlg, this.Font, false);

                            dlg.Text = "��ѡ���� '" + strHanzi + "' ��ƴ�� (���Է����� " + this.MainForm.PinyinServerUrl + ")";
                            dlg.SampleText = strSampleText;
                            dlg.Offset = nOffs;
                            dlg.Pinyins = strCharPinyins;
                            if (index < pinyin_parts.Length)
                                dlg.ActivePinyin = pinyin_parts[index];
                            dlg.Hanzi = strHanzi;

                            MainForm.AppInfo.LinkFormState(dlg, "SelPinyinDlg_state");

                            dlg.ShowDialog(this);

                            MainForm.AppInfo.UnlinkFormState(dlg);

                            Debug.Assert(DialogResult.Cancel != DialogResult.Abort, "�ƶ�");

                            if (dlg.DialogResult == DialogResult.Abort)
                            {
                                return 0;   // �û�ϣ�������ж�
                            }

                            DomUtil.SetAttr(nodeChar, "sel", dlg.ResultPinyin);

                            if (dlg.DialogResult == DialogResult.Cancel)
                            {
                                SelPinyinDlg.AppendText(ref strPinyin, strHanzi);
                                nStatus = 2;
                            }
                            else if (dlg.DialogResult == DialogResult.OK)
                            {
                                SelPinyinDlg.AppendPinyin(ref strPinyin,
                                    SelPinyinDlg.ConvertSinglePinyinByStyle(
                                    dlg.ResultPinyin,
                                    style)
                                    );
                                nStatus = 2;
                            }
                            else
                            {
                                Debug.Assert(false, "SelPinyinDlg����ʱ���������DialogResultֵ");
                            }

                            index++;
                        }

                    }
                }

#if _TEST_PINYIN
#else
                // return:
                //      -2  strID��֤ʧ��
                //      -1  ����
                //      0   �ɹ�
                nRet = GcatNew.SetPinyin(
                    new_stop,
                    m_gcatClient,
                    "",
                    dom.DocumentElement.OuterXml,
                    out strError);
                if (nRet == -1)
                    return -1;
#endif

                return 1;
            }
            finally
            {
                new_stop.EndLoop();
                new_stop.OnStop -= new StopEventHandler(new_stop_OnStop);
                new_stop.Initial("");
                new_stop.Unregister();
                if (m_gcatClient != null)
                {
                    m_gcatClient.Close();
                    m_gcatClient = null;
                }
            }
        }


        void new_stop_OnStop(object sender, StopEventArgs e)
        {
            if (this.m_gcatClient != null)
            {
                this.m_gcatClient.Abort();
            }
        }
#endif

#if NO
        // ���ַ����еĺ��ֺ�ƴ������
        // parameters:
        //      bLocal  �Ƿ�ӱ��ػ�ȡƴ��
        // return:
        //      -1  ����
        //      0   �û�ϣ���ж�
        //      1   ����
        public int HanziTextToPinyin(
            bool bLocal,
            string strText,
            PinyinStyle style,
            out string strPinyin,
            out string strError)
        {
            strError = "";
            strPinyin = "";

            string strSpecialChars = "���������������������������������ۣݡ����������������ܣ�������������";


            string strHanzi;
            int nStatus = -1;	// ǰ��һ���ַ������� -1:ǰ��û���ַ� 0:��ͨӢ����ĸ 1:�ո� 2:����


            for (int i = 0; i < strText.Length; i++)
            {
                char ch = strText[i];

                strHanzi = "";

                if (ch >= 0 && ch <= 128)
                {
                    if (nStatus == 2)
                        strPinyin += " ";

                    strPinyin += ch;

                    if (ch == ' ')
                        nStatus = 1;
                    else
                        nStatus = 0;

                    continue;
                }
                else
                {	// ����
                    strHanzi += ch;
                }

                // ����ǰ�������Ӣ�Ļ��ߺ��֣��м����ո�
                if (nStatus == 2 || nStatus == 0)
                    strPinyin += " ";


                // �����Ƿ��������
                if (strSpecialChars.IndexOf(strHanzi) != -1)
                {
                    strPinyin += strHanzi;	// ���ڱ�Ӧ��ƴ����λ��
                    nStatus = 2;
                    continue;
                }


                // ���ƴ��
                string strResultPinyin = "";

                int nRet = 0;

                if (bLocal == true)
                {
                    nRet = this.MainForm.LoadQuickPinyin(true, out strError);
                    if (nRet == -1)
                        return -1;
                    nRet = this.MainForm.QuickPinyin.GetPinyin(
                        strHanzi,
                        out strResultPinyin,
                        out strError);
                }
                else
                {
                    throw new Exception("�ݲ�֧�ִ�ƴ�����л�ȡƴ��");
                    /*
                    nRet = GetOnePinyin(strHanzi,
                         out strResultPinyin,
                         out strError);
                     * */
                }
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {	// canceld
                    strPinyin += strHanzi;	// ֻ�ý����ַ��ڱ�Ӧ��ƴ����λ��
                    nStatus = 2;
                    continue;
                }

                Debug.Assert(strResultPinyin != "", "");

                strResultPinyin = strResultPinyin.Trim();
                if (strResultPinyin.IndexOf(";", 0) != -1)
                {	// ����Ƕ��ƴ��
                    SelPinyinDlg dlg = new SelPinyinDlg();
                    // GuiUtil.SetControlFont(dlg, this.Font);
                    float ratio_single = dlg.listBox_multiPinyin.Font.SizeInPoints / dlg.Font.SizeInPoints;
                    float ratio_sample = dlg.textBox_sampleText.Font.SizeInPoints / dlg.Font.SizeInPoints;
                    GuiUtil.SetControlFont(dlg, this.Font, false);
                    // ά�������ԭ�д�С������ϵ
                    dlg.listBox_multiPinyin.Font = new Font(dlg.Font.FontFamily, ratio_single * dlg.Font.SizeInPoints, GraphicsUnit.Point);
                    dlg.textBox_sampleText.Font = new Font(dlg.Font.FontFamily, ratio_sample * dlg.Font.SizeInPoints, GraphicsUnit.Point);

                    dlg.SampleText = strText;
                    dlg.Offset = i;
                    dlg.Pinyins = strResultPinyin;
                    dlg.Hanzi = strHanzi;

                    MainForm.AppInfo.LinkFormState(dlg, "SelPinyinDlg_state");

                    dlg.ShowDialog(this);

                    MainForm.AppInfo.UnlinkFormState(dlg);

                    Debug.Assert(DialogResult.Cancel != DialogResult.Abort, "�ƶ�");

                    if (dlg.DialogResult == DialogResult.Cancel)
                    {
                        strPinyin += strHanzi;
                    }
                    else if (dlg.DialogResult == DialogResult.OK)
                    {
                        strPinyin += SelPinyinDlg.ConvertSinglePinyinByStyle(
                            dlg.ResultPinyin,
                            style);
                    }
                    else if (dlg.DialogResult == DialogResult.Abort)
                    {
                        return 0;   // �û�ϣ�������ж�
                    }
                    else
                    {
                        Debug.Assert(false, "SelPinyinDlg����ʱ���������DialogResultֵ");
                    }
                }
                else
                {
                    // ����ƴ��

                    strPinyin += SelPinyinDlg.ConvertSinglePinyinByStyle(
                        strResultPinyin,
                        style);
                }
                nStatus = 2;
            }

            return 1;   // ��������
        }
#endif

        #endregion

        private void MarcEditor_GenerateData(object sender, GenerateDataEventArgs e)
        {
            this.AutoGenerate(sender, e);
        }

        private void MarcEditor_ControlLetterKeyPress(object sender,
            ControlLetterKeyPressEventArgs e)
        {
            // Ctrl + D ����
            if (e.KeyData == (Keys.D | Keys.Control))
            {
                this.SearchDup("ctrl_d");
                e.Handled = true;
                return;
            }
        }

#if NO
        // �Զ��ӹ�����
        public void AutoGenerate()
        {
            string strError = "";
            string strCode = "";
            string strRef = "";


            if (String.IsNullOrEmpty(this.SavePath) == true)
            {
                strError = "ȱ������·��";
                goto ERROR1;
            }

            string strProtocol = "";
            string strPath = "";
            int nRet = Global.ParsePath(this.SavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;


            // dtlpЭ����Զ���������
            if (strProtocol.ToLower() == "dtlp")
            {
                strError = "�ݲ�֧������DTLPЭ��������Զ���������";
                goto ERROR1;
            }

            // dp2libraryЭ����Զ���������
            if (strProtocol.ToLower() == "dp2library")
            {
                dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷��������ݴ���";
                    goto ERROR1;
                }

                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                string strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);

                string strCfgFileName = "dp2catalog_marc_autogen.cs";

                string strCfgPath = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;

                byte[] baCfgOutputTimestamp = null;
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = dp2_searchform.GetCfgFile(strCfgPath,
                    out strCode,
                    out baCfgOutputTimestamp,
                    out strError);
                if (nRet == -1 || nRet == 0)
                    goto ERROR1;

                strCfgFileName = "dp2catalog_marc_autogen.cs.ref";

                strCfgPath = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;

                nRet = dp2_searchform.GetCfgFile(strCfgPath,
                    out strRef,
                    out baCfgOutputTimestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

            }

            try
            {
                // ִ�д���
                nRet = RunScript(strCode,
                    strRef,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            catch (Exception ex)
            {
                strError = "ִ�нű���������з����쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif

#if NO
        int RunScript(string strCode,
            string strRef,
            out string strError)
        {
            strError = "";
            string[] saRef = null;
            int nRet;
            // string strWarning = "";

            nRet = ScriptManager.GetRefsFromXml(strRef,
                out saRef,
                out strError);
            if (nRet == -1)
                return -1;

            // 2007/12/4
            ScriptManager.RemoveRefsBinDirMacro(ref saRef);

            string[] saAddRef = {
                                    // 2011/5/4 ����
                                    "system.dll",
                                    "system.xml.dll",
                                    "system.windows.forms.dll",
                                    "system.drawing.dll",
                                    "System.Runtime.Serialization.dll",

									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.script.dll",

									//Environment.CurrentDirectory + "\\digitalplatform.xmleditor.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.client.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marceditor.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcfixedfieldcontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.gcatclient.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.library.dll",
									// Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
									Environment.CurrentDirectory + "\\dp2catalog.exe"
								};

            if (saAddRef != null)
            {
                string[] saTemp = new string[saRef.Length + saAddRef.Length];
                Array.Copy(saRef, 0, saTemp, 0, saRef.Length);
                Array.Copy(saAddRef, 0, saTemp, saRef.Length, saAddRef.Length);
                saRef = saTemp;
            }

            /*
            Assembly assembly = ScriptManager.CreateAssembly(
                strCode,
                saRef,
                null,	// strLibPaths,
                null,	// strOutputFile,
                out strError,
                out strWarning);
            if (assembly == null)
            {
                strError = "�ű����뷢�ִ���򾯸�:\r\n" + strError;
                return -1;
            }*/
            Assembly assembly = null;
            string strErrorInfo = "";
            string strWarningInfo = "";
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saRef,
                null,   // strLibPaths,
                out assembly,
                out strErrorInfo,
                out strWarningInfo);
            if (nRet == -1)
            {
                strError = "�ű����뷢�ִ���򾯸�:\r\n" + strErrorInfo;
                return -1;
            }

            // �õ�Assembly��Host������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                assembly,
                "dp2Catalog.MarcDetailHost");
            if (entryClassType == null)
            {
                strError = "dp2Catalog.MarcDetailHost������û���ҵ�";
                return -1;
            }

            // newһ��MarcDetailHost��������
            MarcDetailHost hostObj = (MarcDetailHost)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            if (hostObj == null)
            {
                strError = "new Host���������ʧ��";
                return -1;
            }

            // ΪHost���������ò���
            hostObj.DetailForm = this;
            hostObj.Assembly = assembly;

            HostEventArgs e = new HostEventArgs();

            /*
            nRet = this.Flush(out strError);
            if (nRet == -1)
                return -1;
             * */


            hostObj.Main(null, e);

            /*
            nRet = this.Flush(out strError);
            if (nRet == -1)
                return -1;
             * */

            return 0;
        }

#endif


        #region ���µĴ������ݽű�����

        Assembly m_autogenDataAssembly = null;
        string m_strAutogenDataCfgFilename = "";    // �Զ��������ݵ�.cs�ļ�·����ȫ·����������������
        object m_autogenSender = null;
        MarcDetailHost m_detailHostObj = null;
        GenerateDataForm m_genDataViewer = null;

        // �Ƿ�Ϊ�µķ��
        bool AutoGenNewStyle
        {
            get
            {
                if (this.m_detailHostObj == null)
                    return false;

                if (this.m_detailHostObj.GetType().GetMethod("CreateMenu") != null)
                    return true;
                return false;
            }
        }

        int m_nDisableInitialAssembly = 0;

        // ��ʼ�� dp2catalog_marc_autogen.cs �� Assembly����new MarcDetailHost����
        // return:
        //      -2  �����Assembly
        //      -1  error
        //      0   û�����³�ʼ��Assembly������ֱ������ǰCache��Assembly (���ܱ������ǿ�)
        //      1   ����(�����״�)��ʼ����Assembly
        public int InitialAutogenAssembly(out string strError)
        {
            strError = "";
            int nRet = 0;

            if (m_nDisableInitialAssembly > 0)
                return 0;

            bool bAssemblyReloaded = false;

            string strAutogenDataCfgFilename = "";
            string strAutogenDataCfgRefFilename = "";
            string strProtocol = "";
            dp2SearchForm dp2_searchform = null;

            if (String.IsNullOrEmpty(this.SavePath) == true)
            {
                if (this.CurrentRecord == null)
                {
                    this.m_autogenDataAssembly = null;
                    this.m_detailHostObj = null;
                    strError = "SavePath��CurrentRecord��Ϊ�գ�Assembly�����";
                    return -2;
                }

                string strCfgFileName = "dp2catalog_marc_autogen.cs";

                string strMarcSyntaxOID = "";

                strMarcSyntaxOID = this.GetCurrentMarcSyntaxOID(out strError);
                if (String.IsNullOrEmpty(strMarcSyntaxOID) == true)
                {
                    strError = "��Ϊ: " + strError + "���޷���������ļ� '" + strCfgFileName + "'";
                    goto ERROR1;
                }

                strAutogenDataCfgFilename = this.MainForm.DataDir + "\\" + strMarcSyntaxOID.Replace(".", "_") + "\\" + strCfgFileName;
                strAutogenDataCfgRefFilename = this.MainForm.DataDir + "\\" + strMarcSyntaxOID.Replace(".", "_") + "\\" + strCfgFileName + ".ref";

                strProtocol = "localfile";
                goto BEGIN;
            }

            string strPath = "";
            nRet = Global.ParsePath(this.SavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // dtlpЭ����Զ���������
            if (strProtocol.ToLower() == "dtlp")
            {
                // TODO: �������ݿ���ӳ�䵽MarcSyntac�Ļ���

                /*
                strError = "�ݲ�֧������DTLPЭ��������Զ���������";
                goto ERROR1;
                 * */

                if (this.CurrentRecord == null)
                {
                    this.m_autogenDataAssembly = null;
                    this.m_detailHostObj = null;
                    strError = "CurrentRecordΪ�գ�Assembly�����";
                    return -2;
                }

                string strCfgFileName = "dp2catalog_marc_autogen.cs";

                string strMarcSyntaxOID = "";

                strMarcSyntaxOID = this.GetCurrentMarcSyntaxOID(out strError);
                if (String.IsNullOrEmpty(strMarcSyntaxOID) == true)
                {
                    strError = "��Ϊ: " + strError + "���޷���������ļ� '" + strCfgFileName + "'";
                    goto ERROR1;
                }


                strAutogenDataCfgFilename = this.MainForm.DataDir + "\\" + strMarcSyntaxOID.Replace(".", "_") + "\\" + strCfgFileName;
                strAutogenDataCfgRefFilename = this.MainForm.DataDir + "\\" + strMarcSyntaxOID.Replace(".", "_") + "\\" + strCfgFileName + ".ref";

                strProtocol = "localfile";
                goto BEGIN;
            }
            // dp2libraryЭ����Զ���������
            else if (strProtocol.ToLower() == "dp2library")
            {
                dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷��������ݴ���";
                    goto ERROR1;
                }

                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                string strCfgFileName = "dp2catalog_marc_autogen.cs";

                if (string.Compare(strServerName, "mem", true) == 0
                || string.Compare(strServerName, "file", true) == 0)
                {
                    string strMarcSyntaxOID = "";

                    strMarcSyntaxOID = this.GetCurrentMarcSyntaxOID(out strError);
                    if (String.IsNullOrEmpty(strMarcSyntaxOID) == true)
                    {
                        strError = "��Ϊ: " + strError + "���޷���������ļ� '" + strCfgFileName + "'";
                        goto ERROR1;
                    }

                    strAutogenDataCfgFilename = this.MainForm.DataDir + "\\" + strMarcSyntaxOID.Replace(".", "_") + "\\" + strCfgFileName;
                    strAutogenDataCfgRefFilename = this.MainForm.DataDir + "\\" + strMarcSyntaxOID.Replace(".", "_") + "\\" + strCfgFileName + ".ref";

                    strProtocol = "localfile";
                    goto BEGIN;
                }

                string strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);

                strAutogenDataCfgFilename = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;
                strAutogenDataCfgRefFilename = strBiblioDbName + "/cfgs/" + strCfgFileName + ".ref@" + strServerName;
            }
            // amazon Э����Զ���������
            else if (strProtocol.ToLower() == "amazon")
            {
                if (this.CurrentRecord == null)
                {
                    this.m_autogenDataAssembly = null;
                    this.m_detailHostObj = null;
                    strError = "CurrentRecordΪ�գ�Assembly�����";
                    return -2;
                }

                string strCfgFileName = "dp2catalog_marc_autogen.cs";

                string strMarcSyntaxOID = "";

                strMarcSyntaxOID = this.GetCurrentMarcSyntaxOID(out strError);
                if (String.IsNullOrEmpty(strMarcSyntaxOID) == true)
                {
                    strError = "��Ϊ: " + strError + "���޷���������ļ� '" + strCfgFileName + "'";
                    goto ERROR1;
                }


                strAutogenDataCfgFilename = this.MainForm.DataDir + "\\" + strMarcSyntaxOID.Replace(".", "_") + "\\" + strCfgFileName;
                strAutogenDataCfgRefFilename = this.MainForm.DataDir + "\\" + strMarcSyntaxOID.Replace(".", "_") + "\\" + strCfgFileName + ".ref";

                strProtocol = "localfile";
                goto BEGIN;
            }
            else
            {
                strError = "�ݲ�֧������ '"+strProtocol+"'  Э��������Զ���������";
                goto ERROR1;
            }

        BEGIN:
            // �����Ҫ������׼��Assembly
            if (m_autogenDataAssembly == null
                || m_strAutogenDataCfgFilename != strAutogenDataCfgFilename)
            {
                this.m_autogenDataAssembly = this.MainForm.AssemblyCache.FindObject(strAutogenDataCfgFilename);
                this.m_detailHostObj = null;

                // ���Cache��û���ֳɵ�Assembly
                if (this.m_autogenDataAssembly == null)
                {
                    string strCode = "";
                    string strRef = "";

                    byte[] baCfgOutputTimestamp = null;

                    if (strProtocol.ToLower() == "dp2library")
                    {
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   found
                        nRet = dp2_searchform.GetCfgFile(strAutogenDataCfgFilename,
                            out strCode,
                            out baCfgOutputTimestamp,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                            goto ERROR1;
                        nRet = dp2_searchform.GetCfgFile(strAutogenDataCfgRefFilename,
out strRef,
out baCfgOutputTimestamp,
out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else if (strProtocol.ToLower() == "localfile")
                    {
                        if (File.Exists(strAutogenDataCfgFilename) == false)
                        {
                            /*
                            if (bOnlyFillMenu == true)
                                return;
                             * */
                            strError = "�����ļ� '" + strAutogenDataCfgFilename + "' ������...";
                            goto ERROR1;
                        }
                        if (File.Exists(strAutogenDataCfgRefFilename) == false)
                        {

                            strError = "�����ļ� '" + strAutogenDataCfgRefFilename + "' ������(���������׵�.cs�ļ��Ѿ�����)...";
                            goto ERROR1;
                        }
                        try
                        {
                            Encoding encoding = FileUtil.DetectTextFileEncoding(strAutogenDataCfgFilename);
                            using (StreamReader sr = new StreamReader(strAutogenDataCfgFilename, encoding))
                            {
                                strCode = sr.ReadToEnd();
                            }
                            encoding = FileUtil.DetectTextFileEncoding(strAutogenDataCfgRefFilename);
                            using (StreamReader sr = new StreamReader(strAutogenDataCfgRefFilename, encoding))
                            {
                                strRef = sr.ReadToEnd();
                            }
                        }
                        catch (Exception ex)
                        {
                            strError = ex.Message;
                            goto ERROR1;
                        }
                    }

                    try
                    {
                        // ׼��Assembly
                        Assembly assembly = null;
                        nRet = GetCsScriptAssembly(
                            strCode,
                            strRef,
                            out assembly,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "����ű��ļ� '" + strAutogenDataCfgFilename + "' ʱ����" + strError;
                            goto ERROR1;
                        }
                        // ���䵽����
                        this.MainForm.AssemblyCache.SetObject(strAutogenDataCfgFilename, assembly);

                        this.m_autogenDataAssembly = assembly;

                        bAssemblyReloaded = true;
                    }
                    catch (Exception ex)
                    {
                        strError = "׼���ű���������з����쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                        goto ERROR1;
                    }
                }

                bAssemblyReloaded = true;

                m_strAutogenDataCfgFilename = strAutogenDataCfgFilename;

                // ���ˣ�Assembly�Ѿ���������
                Debug.Assert(this.m_autogenDataAssembly != null, "");
            }

            Debug.Assert(this.m_autogenDataAssembly != null, "");

            // ׼�� host ����
            if (this.m_detailHostObj == null
                || bAssemblyReloaded == true)
            {
                try
                {
                    MarcDetailHost host = null;
                    nRet = NewHostObject(
                        out host,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ִ�нű��ļ� '" + m_strAutogenDataCfgFilename + "' ʱ����" + strError;
                        goto ERROR1;
                    }
                    this.m_detailHostObj = host;

                }
                catch (Exception ex)
                {
                    strError = "׼���ű���������з����쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                    goto ERROR1;
                }
            }

            Debug.Assert(this.m_detailHostObj != null, "");

            if (bAssemblyReloaded == true)
                return 1;
            return 0;
        ERROR1:
            return -1;
        }

        // �Զ��ӹ�����
        // parameters:
        //      sender    �Ӻδ�����? MarcEditor EntityEditForm
        public void AutoGenerate(object sender,
            GenerateDataEventArgs e,
            bool bOnlyFillMenu = false)
        {
            int nRet = 0;

            string strError = "";
            bool bAssemblyReloaded = false;

            // ��ʼ�� dp2catalog_marc_autogen.cs �� Assembly����new MarcDetailHost����
            // return:
            //      -2  �����Assembly
            //      -1  error
            //      0   û�����³�ʼ��Assembly������ֱ������ǰCache��Assembly
            //      1   ����(�����״�)��ʼ����Assembly
            nRet = InitialAutogenAssembly(out strError);
            if (nRet == -2)
            {
                if (bOnlyFillMenu == true)
                    return;
            }
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                if (this.m_detailHostObj == null)
                    return; // �������߱����޷���ʼ��
            }
            if (nRet == 1)
                bAssemblyReloaded = true;

            Debug.Assert(this.m_detailHostObj != null, "");




            if (this.AutoGenNewStyle == true)
            {
                DisplayAutoGenMenuWindow(this.MainForm.PanelFixedVisible == false ? true : false);
                if (bOnlyFillMenu == false)
                {
                    if (this.MainForm.PanelFixedVisible == true)
                        MainForm.ActivateGenerateDataPage();
                }

                if (this.m_genDataViewer != null)
                {
                    this.m_genDataViewer.sender = sender;
                    this.m_genDataViewer.e = e;
                }

                // ��������˵�����
                if (m_autogenSender != sender
                    || bAssemblyReloaded == true)
                {
                    if (this.m_genDataViewer != null
                        && this.m_genDataViewer.Count > 0)
                        this.m_genDataViewer.Clear();
                }
            }
            else // �ɵķ��
            {
                if (this.m_genDataViewer != null)
                {
                    this.m_genDataViewer.Close();
                    this.m_genDataViewer = null;
                }

                if (this.Focused == true || this.MarcEditor.Focused)
                    this.MainForm.CurrentGenerateDataControl = null;

                // �����ͼ����Ϊ���˵�
                if (bOnlyFillMenu == true)
                    return;
            }

            try
            {
                // �ɵķ��
                if (this.AutoGenNewStyle == false)
                {
                    this.m_detailHostObj.Invoke(String.IsNullOrEmpty(e.ScriptEntry) == true ? "Main" : e.ScriptEntry,
sender,
e);
                    // this.SetSaveAllButtonState(true);
                    return;
                }

                // ��ʼ���˵�
                try
                {
                    if (this.m_genDataViewer != null)
                    {
                        if (this.m_genDataViewer.Count == 0)
                        {
                            dynamic o = this.m_detailHostObj;
                            o.CreateMenu(sender, e);

                            this.m_genDataViewer.Actions = this.m_detailHostObj.ScriptActions;
                        }

                        // ���ݵ�ǰ�����λ��ˢ�¼�������
                        this.m_genDataViewer.RefreshState();
                    }

                    if (String.IsNullOrEmpty(e.ScriptEntry) == false)
                    {
                        this.m_detailHostObj.Invoke(e.ScriptEntry,
                            sender,
                            e);
                    }
                    else
                    {
                        if (this.MainForm.PanelFixedVisible == true
                            && bOnlyFillMenu == false
                            && this.MainForm.CurrentGenerateDataControl != null)
                        {
                            TableLayoutPanel table = (TableLayoutPanel)this.MainForm.CurrentGenerateDataControl;
                            for (int i = 0; i < table.Controls.Count; i++)
                            {
                                Control control = table.Controls[i];
                                if (control is DpTable)
                                {
                                    control.Focus();
                                    break;
                                }
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    /*
                    // ���ȸ��þɵķ��
                    this.m_detailHostObj.Invoke(String.IsNullOrEmpty(e.ScriptEntry) == true ? "Main" : e.ScriptEntry,
    sender,
    e);
                    this.SetSaveAllButtonState(true);
                    return;
                     * */
                    throw;
                }
            }
            catch (Exception ex)
            {
                strError = "ִ�нű��ļ� '" + m_strAutogenDataCfgFilename + "' �����з����쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                goto ERROR1;
            }

            this.m_autogenSender = sender;  // �������һ�εĵ��÷�����

            if (bOnlyFillMenu == false
                && this.m_genDataViewer != null)
                this.m_genDataViewer.TryAutoRun();

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void DisplayAutoGenMenuWindow(bool bOpenWindow)
        {
            string strError = "";

            // �Ż���������ν�ؽ��з���������
            if (bOpenWindow == false)
            {
                if (this.MainForm.PanelFixedVisible == false
                    && (m_genDataViewer == null || m_genDataViewer.Visible == false))
                    return;
            }


            if (this.m_genDataViewer == null
                || (bOpenWindow == true && this.m_genDataViewer.Visible == false))
            {
                m_genDataViewer = new GenerateDataForm();

                m_genDataViewer.AutoRun = this.MainForm.AppInfo.GetBoolean("detailform", "gen_auto_run", false);
                // GuiUtil.SetControlFont(m_genDataViewer, this.Font, false);

                {	// �ָ��п��
                    string strWidths = this.MainForm.AppInfo.GetString(
                                   "gen_data_dlg",
                                    "column_width",
                                   "");
                    if (String.IsNullOrEmpty(strWidths) == false)
                    {
                        DpTable.SetColumnHeaderWidth(m_genDataViewer.ActionTable,
                            strWidths,
                            true);
                    }
                }

                // m_genDataViewer.MainForm = this.MainForm;  // �����ǵ�һ��
                m_genDataViewer.Text = "��������";

                m_genDataViewer.DoDockEvent -= new DoDockEventHandler(m_genDataViewer_DoDockEvent);
                m_genDataViewer.DoDockEvent += new DoDockEventHandler(m_genDataViewer_DoDockEvent);

                m_genDataViewer.SetMenu -= new RefreshMenuEventHandler(m_genDataViewer_SetMenu);
                m_genDataViewer.SetMenu += new RefreshMenuEventHandler(m_genDataViewer_SetMenu);

                m_genDataViewer.TriggerAction -= new TriggerActionEventHandler(m_genDataViewer_TriggerAction);
                m_genDataViewer.TriggerAction += new TriggerActionEventHandler(m_genDataViewer_TriggerAction);

                m_genDataViewer.MyFormClosed -= new EventHandler(m_genDataViewer_MyFormClosed);
                m_genDataViewer.MyFormClosed += new EventHandler(m_genDataViewer_MyFormClosed);

                m_genDataViewer.FormClosed -= new FormClosedEventHandler(m_genDataViewer_FormClosed);
                m_genDataViewer.FormClosed += new FormClosedEventHandler(m_genDataViewer_FormClosed);

            }


            if (bOpenWindow == true)
            {
                if (m_genDataViewer.Visible == false)
                {
                    this.MainForm.AppInfo.LinkFormState(m_genDataViewer, "autogen_viewer_state");
                    m_genDataViewer.Show(this);
                    m_genDataViewer.Activate();

                    this.MainForm.CurrentGenerateDataControl = null;
                }
                else
                {
                    if (m_genDataViewer.WindowState == FormWindowState.Minimized)
                        m_genDataViewer.WindowState = FormWindowState.Normal;
                    m_genDataViewer.Activate();
                }
            }
            else
            {
                if (m_genDataViewer.Visible == true)
                {

                }
                else
                {
                    if (this.MainForm.CurrentGenerateDataControl != m_genDataViewer.Table)
                        m_genDataViewer.DoDock(false); // �����Զ���ʾFixedPanel
                }
            }

            if (this.m_genDataViewer != null)
                this.m_genDataViewer.CloseWhenComplete = bOpenWindow;

            return;
        ERROR1:
            MessageBox.Show(this, "DisplayAutoGenMenu() ����: " + strError);
        }

        void m_genDataViewer_DoDockEvent(object sender, DoDockEventArgs e)
        {
            if (this.MainForm.CurrentGenerateDataControl != m_genDataViewer.Table)
                this.MainForm.CurrentGenerateDataControl = m_genDataViewer.Table;

            if (e.ShowFixedPanel == true
                && this.MainForm.PanelFixedVisible == false)
                this.MainForm.PanelFixedVisible = true;

            /*
            this.MainForm.AppInfo.SetBoolean("detailform", "gen_auto_run", m_genDataViewer.AutoRun);

            {	// �����п��
                string strWidths = DpTable.GetColumnWidthListString(m_genDataViewer.ActionTable);
                this.MainForm.AppInfo.SetString(
                    "gen_data_dlg",
                    "column_width",
                    strWidths);
            }
             * */

            m_genDataViewer.Docked = true;
            m_genDataViewer.Visible = false;
        }

        void m_genDataViewer_SetMenu(object sender, RefreshMenuEventArgs e)
        {
            if (e.Actions == null || this.m_detailHostObj == null)
                return;

            Type classType = m_detailHostObj.GetType();

            foreach (ScriptAction action in e.Actions)
            {
                string strFuncName = action.ScriptEntry + "_setMenu";
                if (string.IsNullOrEmpty(strFuncName) == true)
                    continue;

                DigitalPlatform.Script.SetMenuEventArgs e1 = new DigitalPlatform.Script.SetMenuEventArgs();
                e1.Action = action;
                e1.sender = e.sender;
                e1.e = e.e;

                classType = m_detailHostObj.GetType();
                while (classType != null)
                {
                    try
                    {
                        // �����������ĳ�Ա����
                        classType.InvokeMember(strFuncName,
                            BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.InvokeMethod
                            ,
                            null,
                            this.m_detailHostObj,
                            new object[] { sender, e1 });
                        break;
                    }
                    catch (System.MissingMethodException/*ex*/)
                    {
                        classType = classType.BaseType;
                        if (classType == null)
                            break;
                    }
                }
            }
        }

        void m_genDataViewer_TriggerAction(object sender, TriggerActionArgs e)
        {
            string strError = "";
            if (this.m_detailHostObj != null)
            {
                if (this.IsDisposed == true)
                {
                    if (this.m_genDataViewer != null)
                    {
                        this.m_genDataViewer.Clear();
                        this.m_genDataViewer.Close();
                        this.m_genDataViewer = null;
                        return;
                    }
                }
                if (String.IsNullOrEmpty(e.EntryName) == false)
                {
                    try
                    {
                        this.m_detailHostObj.Invoke(e.EntryName,
                            e.sender,
                            e.e);
                    }
                    catch(Exception ex)
                    {
                        // 2015/8/24
                        strError = "MARC��¼���ļ�¼ '"+this.SavePath+"' ��ִ�д������ݽű���ʱ������쳣: " + ExceptionUtil.GetDebugText(ex)
                            + "\r\n\r\n���������Ŀ��¼��ص� dp2catalog_marc_autogen.cs �����ļ�������ˢ�������Ŀ�ⶨ�壬����������ƽ̨�Ĺ���ʦȡ����ϵ";
                        goto ERROR1;
                    }
                }

                if (this.m_genDataViewer != null)
                    this.m_genDataViewer.RefreshState();
            }
            return;
        ERROR1:
            // MessageBox.Show(this, strError);
            {
                bool bSendReport = true;
                DialogResult result = MessageDlg.Show(this,
        "dp2Catalog �����쳣:\r\n\r\n" + strError,
        "dp2Catalog �����쳣",
        MessageBoxButtons.OK,
        MessageBoxDefaultButton.Button1,
        ref bSendReport,
        new string[] { "ȷ��" },
        "����Ϣ���͸�������");
                // �����쳣����
                if (bSendReport)
                    Program.CrashReport(strError);
            }
        }

        void m_genDataViewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_genDataViewer != null)
            {
                if (this.MainForm != null && this.MainForm.AppInfo != null)
                {
                    this.MainForm.AppInfo.SetBoolean("detailform", "gen_auto_run", m_genDataViewer.AutoRun);

                    {	// �����п��
                        string strWidths = DpTable.GetColumnWidthListString(m_genDataViewer.ActionTable);
                        this.MainForm.AppInfo.SetString(
                            "gen_data_dlg",
                            "column_width",
                            strWidths);
                    }

                    this.MainForm.AppInfo.UnlinkFormState(m_genDataViewer);
                }
                this.m_genDataViewer = null;
            }
        }

        void m_genDataViewer_MyFormClosed(object sender, EventArgs e)
        {
            if (m_genDataViewer != null)
            {
                this.MainForm.AppInfo.SetBoolean("detailform", "gen_auto_run", m_genDataViewer.AutoRun);

                {	// �����п��
                    string strWidths = DpTable.GetColumnWidthListString(m_genDataViewer.ActionTable);
                    this.MainForm.AppInfo.SetString(
                        "gen_data_dlg",
                        "column_width",
                        strWidths);
                }

                this.MainForm.AppInfo.UnlinkFormState(m_genDataViewer);
                this.m_genDataViewer = null;
            }
        }

        int NewHostObject(
            out MarcDetailHost hostObj,
            out string strError)
        {
            strError = "";
            hostObj = null;

            Type entryClassType = ScriptManager.GetDerivedClassType(
    this.m_autogenDataAssembly,
    "dp2Catalog.MarcDetailHost");
            if (entryClassType == null)
            {
                strError = "dp2Catalog.MarcDetailHost�������඼û���ҵ�";
                return -1;
            }

            // newһ��MarcDetailHost��������
            hostObj = (MarcDetailHost)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            if (hostObj == null)
            {
                strError = "new MarcDetailHost�����������ʱʧ��";
                return -1;
            }

            // ΪDetailHost���������ò���
            hostObj.DetailForm = this;
            hostObj.Assembly = this.m_autogenDataAssembly;

            return 0;
        }

        int GetCsScriptAssembly(
    string strCode,
    string strRef,
            out Assembly assembly,
    out string strError)
        {
            strError = "";
            assembly = null;

            string[] saRef = null;
            int nRet;

            nRet = ScriptManager.GetRefsFromXml(strRef,
                out saRef,
                out strError);
            if (nRet == -1)
                return -1;

            ScriptManager.RemoveRefsBinDirMacro(ref saRef);

            string[] saAddRef = {
                                    // 2011/3/4 ����
                                    "system.dll",
                                    "system.xml.dll",
                                    "system.windows.forms.dll",
                                    "system.drawing.dll",
                                    "System.Runtime.Serialization.dll",

									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.script.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.xmleditor.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.client.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marceditor.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcfixedfieldcontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.gcatclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.commoncontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.library.dll",
									// Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
									Environment.CurrentDirectory + "\\dp2catalog.exe"
								};

            if (saAddRef != null)
            {
                string[] saTemp = new string[saRef.Length + saAddRef.Length];
                Array.Copy(saRef, 0, saTemp, 0, saRef.Length);
                Array.Copy(saAddRef, 0, saTemp, saRef.Length, saAddRef.Length);
                saRef = saTemp;
            }

            string strErrorInfo = "";
            string strWarningInfo = "";
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saRef,
                null,   // strLibPaths,
                out assembly,
                out strErrorInfo,
                out strWarningInfo);
            if (nRet == -1)
            {
                strError = "�ű����뷢�ִ���򾯸�:\r\n" + strErrorInfo;
                return -1;
            }

            return 0;
        }


        #endregion


        // ��ó����������Ϣ
        public int GetPublisherInfo(
            string strPublisherNumber,
            out string str210,
            out string strError)
        {
            strError = "";
            str210 = "";

            string strProtocol = "";
            string strPath = "";
            int nRet = Global.ParsePath(this.SavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                return -1;

            if (strProtocol.ToLower() == "dp2library")
            {
                dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷���������GetPublisherInfo()";
                    return -1;
                }

                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                return dp2_searchform.GetPublisherInfo(
                    strServerName,
                    strPublisherNumber,
                    out str210,
                    out strError);
            }

            strError = "�޷�ʶ���Э���� '" + strProtocol + "'";
            return -1;
        }

        // ���ó����������Ϣ
        public int SetPublisherInfo(
            string strPublisherNumber,
            string str210,
            out string strError)
        {
            strError = "";

            string strProtocol = "";
            string strPath = "";
            int nRet = Global.ParsePath(this.SavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                return -1;

            if (strProtocol.ToLower() == "dp2library")
            {
                dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷���������SetPublisherInfo()";
                    return -1;
                }

                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                return dp2_searchform.SetPublisherInfo(
                    strServerName,
                    strPublisherNumber,
                    str210,
                    out strError);
            }

            strError = "�޷�ʶ���Э���� '" + strProtocol + "'";
            return -1;
        }

        // ���102�����Ϣ
        public int Get102Info(
            string strPublisherNumber,
            out string str102,
            out string strError)
        {
            strError = "";
            str102 = "";

            string strProtocol = "";
            string strPath = "";
            int nRet = Global.ParsePath(this.SavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                return -1;

            if (strProtocol.ToLower() == "dp2library")
            {
                dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷���������Get102Info()";
                    return -1;
                }

                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                return dp2_searchform.Get102Info(
                    strServerName,
                    strPublisherNumber,
                    out str102,
                    out strError);
            }

            strError = "�޷�ʶ���Э���� '" + strProtocol + "'";
            return -1;
        }

        // ����102�����Ϣ
        public int Set102Info(
            string strPublisherNumber,
            string str102,
            out string strError)
        {
            strError = "";

            string strProtocol = "";
            string strPath = "";
            int nRet = Global.ParsePath(this.SavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                return -1;

            if (strProtocol.ToLower() == "dp2library")
            {
                dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷���������Set102Info()";
                    return -1;
                }

                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                return dp2_searchform.Set102Info(
                    strServerName,
                    strPublisherNumber,
                    str102,
                    out strError);
            }

            strError = "�޷�ʶ���Э���� '" + strProtocol + "'";
            return -1;
        }

        public int LoadTemplate()
        {
            int nRet = 0;

            if (this.BiblioChanged == true
                || this.ObjectChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪװ��������? ",
                    "MarcDetailForm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return 0;
            }

            // ���ݲ�ͬ��Э�飬���ò�ͬ��װ��ģ�幦��
            string strProtocol = "";
            string strPath = "";
            string strError = "";

            if (String.IsNullOrEmpty(this.SavePath) == false)
            {
                // �������������
                nRet = Global.ParsePath(this.SavePath,
                    out strProtocol,
                    out strPath,
                    out strError);
                if (nRet == -1)
                {
                    strError = "����·�� '" + this.SavePath + "' �ַ��������з�������: " + strError;
                    goto ERROR1;
                }
            }
            else
            {
                // ѡ��Э��
                SelectProtocolDialog protocol_dlg = new SelectProtocolDialog();
                GuiUtil.SetControlFont(protocol_dlg, this.Font);

                protocol_dlg.Protocols = new List<string>();
                protocol_dlg.Protocols.Add("dp2library");
                protocol_dlg.Protocols.Add("dtlp");
                protocol_dlg.StartPosition = FormStartPosition.CenterScreen;

                protocol_dlg.ShowDialog(this);

                if (protocol_dlg.DialogResult != DialogResult.OK)
                    return 0;

                strProtocol = protocol_dlg.SelectedProtocol;
            }


            if (strProtocol == "dp2library")
            {
                return LoadDp2libraryTemplate(strPath);
            }
            else if (strProtocol == "dtlp")
            {
                return LoadDtlpTemplate(strPath);
            }
            else
            {
                return LoadDp2libraryTemplate("");
            }

            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // DTLPЭ���� װ��ģ��
        // parameters:
        //      strPath DTLPЭ����·�������� localhost/����ͼ��/ctlno/0000001
        public int LoadDtlpTemplate(string strPath)
        {
            int nRet = 0;
            string strError = "";

            DtlpSearchForm dtlp_searchform = this.GetDtlpSearchForm();

            if (dtlp_searchform == null)
            {
                strError = "û�����ӵĻ��ߴ򿪵�DTLP���������޷�װ��ģ��";
                goto ERROR1;
            }


            string strServerAddr = "";
            string strDbName = "";
            string strNumber = "";

            // ��������·��
            // return:
            //      -1  ����
            //      0   �ɹ�
            nRet = DtlpChannel.ParseWritePath(strPath,
                out strServerAddr,
                out strDbName,
                out strNumber,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string strStartPath = "";

            if (String.IsNullOrEmpty(strServerAddr) == false
                && String.IsNullOrEmpty(strDbName) == false)
                strStartPath = strServerAddr + "/" + strDbName;
            else if (String.IsNullOrEmpty(strServerAddr) == false)
                strStartPath = strServerAddr;

            GetDtlpResDialog dlg = new GetDtlpResDialog();
            GuiUtil.SetControlFont(dlg, this.Font);


            dlg.Text = "��ѡ��Ŀ�����ݿ�";
            dlg.Initial(dtlp_searchform.DtlpChannels,
                dtlp_searchform.DtlpChannel);
            dlg.EnabledIndices = new int[] { DtlpChannel.TypeStdbase };
            dlg.Path = strStartPath;
            dlg.StartPosition = FormStartPosition.CenterScreen;

            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return 0;

            // ���default.cfg�����ļ�
            string strCfgPath = dlg.Path + "/cfgs/default.cfg";
            string strContent = "";

            Cursor.Current = Cursors.WaitCursor;
            nRet = dtlp_searchform.DtlpChannel.GetCfgFile(strCfgPath,
                out strContent,
                out strError);
            Cursor.Current = Cursors.Default;

            if (nRet == -1)
                goto ERROR1;

            // ѡ��ģ��
            SelectRecordTemplateDialog tempdlg = new SelectRecordTemplateDialog();
            GuiUtil.SetControlFont(tempdlg, this.Font);

            tempdlg.Content = strContent;
            tempdlg.StartPosition = FormStartPosition.CenterScreen;

            tempdlg.ShowDialog(this);

            if (tempdlg.DialogResult != DialogResult.OK)
                return 0;

            this.SavePath = "DTLP" + ":" + dlg.Path + "/ctlno/?";

            // �Զ�ʶ��MARC��ʽ
            string strOutMarcSyntax = "";
            // ̽���¼��MARC��ʽ unimarc / usmarc / reader
            nRet = MarcUtil.DetectMarcSyntax(tempdlg.SelectedRecordMarc,
                out strOutMarcSyntax);
            if (strOutMarcSyntax == "")
                strOutMarcSyntax = "unimarc";

            if (strOutMarcSyntax == "unimarc" || strOutMarcSyntax == "")
                this.AutoDetectedMarcSyntaxOID = "1.2.840.10003.5.1";
            else if (strOutMarcSyntax == "usmarc")
                this.AutoDetectedMarcSyntaxOID = "1.2.840.10003.5.10";
            else if (strOutMarcSyntax == "dt1000reader")
                this.AutoDetectedMarcSyntaxOID = "1.2.840.10003.5.dt1000reader";
            else
            {
                /*
                strError = "δ֪��MARC syntax '" + strOutMarcSyntax + "'";
                goto ERROR1;
                 * */
                // TODO: ���Գ��ֲ˵�ѡ��
            }

            this.MarcEditor.ClearMarcDefDom();
            this.MarcEditor.Marc = tempdlg.SelectedRecordMarc;
            this.CurrentTimestamp = null;

            this.ObjectChanged = false;
            this.BiblioChanged = false;

            DisplayHtml(tempdlg.SelectedRecordMarc, this.AutoDetectedMarcSyntaxOID);

            this.LinkedSearchForm = null;  // �жϺ�ԭ�������ļ���������ϵ��������û��ǰ��ҳ��
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // ����̬Ϊ �����ط�����/����ͼ�顱������·��ת��Ϊ������ͼ��@���ط�������
        static string CanonicalizePath(string strPath)
        {
            string[] parts = strPath.Split(new char[] {'/'});
            if (parts.Length < 2)
                return "";

            return parts[1] + "@" + parts[0];
        }

        // dp2libraryЭ���� װ��ģ��
        // parameters:
        //      strPath dp2libraryЭ����·�������� ����ͼ��/1@���ط�����
        public int LoadDp2libraryTemplate(string strPath)
        {
            try
            {
                string strError = "";
                int nRet = 0;

                // ��ס Shift ʹ�ñ����ܣ������³��ֶԻ���
                bool bShift = (Control.ModifierKeys == Keys.Shift);

                /*
                if (this.BiblioChanged == true
                    || this.ObjectChanged == true)
                {
                    // ������δ����
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ�� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪװ��������? ",
                        "MarcDetailForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return 0;
                }*/


                dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷�װ��ģ��";
                    goto ERROR1;
                }

                string strSelectedDbName = this.MainForm.AppInfo.GetString(
         "entity_form",
         "selected_dbname_for_loadtemplate",
         "");
                SelectedTemplate selected = this.selected_templates.Find(strSelectedDbName);


                string strServerName = "";
                string strLocalPath = "";

                string strBiblioDbName = "";

                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(string.IsNullOrEmpty(strSelectedDbName) == false ? strSelectedDbName : strPath,
                    out strServerName,
                    out strLocalPath);

                strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);


                /*
                if (this.LinkedSearchForm != null
                    && strProtocol != this.LinkedSearchForm.CurrentProtocol)
                {
                    strError = "��������Э���Ѿ������ı�";
                    goto ERROR1;
                }*/

                string strStartPath = "";

                if (String.IsNullOrEmpty(strServerName) == false
                    && String.IsNullOrEmpty(strBiblioDbName) == false)
                    strStartPath = strServerName + "/" + strBiblioDbName;
                else if (String.IsNullOrEmpty(strServerName) == false)
                    strStartPath = strServerName;

                GetDp2ResDlg dbname_dlg = new GetDp2ResDlg();
                GuiUtil.SetControlFont(dbname_dlg, this.Font);
                if (selected != null)
                {
                    dbname_dlg.NotAsk = selected.NotAskDbName;
                    dbname_dlg.AutoClose = (bShift == true ? false : selected.NotAskDbName);
                }
                dbname_dlg.EnableNotAsk = true;

                dbname_dlg.Text = "װ����Ŀģ�� -- ��ѡ��Ŀ�����ݿ�";
                dbname_dlg.dp2Channels = dp2_searchform.Channels;
                dbname_dlg.Servers = this.MainForm.Servers;
                dbname_dlg.EnabledIndices = new int[] { dp2ResTree.RESTYPE_DB };
                dbname_dlg.Path = strStartPath;

                if (this.IsValid() == false)
                    return -1;
                    dbname_dlg.ShowDialog(this);    ////


                if (dbname_dlg.DialogResult != DialogResult.OK)
                    return 0;

                // ����
                this.MainForm.AppInfo.SetString(
                    "entity_form",
                    "selected_dbname_for_loadtemplate",
                    CanonicalizePath(dbname_dlg.Path));

                selected = this.selected_templates.Find(CanonicalizePath(dbname_dlg.Path));   // 

                // ��Ŀ��·�����Ϊ��������
                nRet = dbname_dlg.Path.IndexOf("/");
                if (nRet == -1)
                {
                    Debug.Assert(false, "");
                    strServerName = dbname_dlg.Path;
                    strBiblioDbName = "";
                    strError = "��ѡ��Ŀ��(���ݿ�)·�� '" + dbname_dlg.Path + "' ��ʽ����ȷ";
                    goto ERROR1;
                }
                else
                {
                    strServerName = dbname_dlg.Path.Substring(0, nRet);
                    strBiblioDbName = dbname_dlg.Path.Substring(nRet + 1);

                    // �����ѡ���ݿ��syntax������Ϊmarc

                    string strSyntax = "";
                    // ���һ�����ݿ������syntax
                    // parameters:
                    //      stop    ���!=null����ʾʹ�����stop�����Ѿ�OnStop +=
                    //              ���==null����ʾ���Զ�ʹ��this.stop�����Զ�OnStop+=
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   found
                    nRet = dp2_searchform.GetDbSyntax(
                        null,
                        strServerName,
                        strBiblioDbName,
                        out strSyntax,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "��ȡ��Ŀ�� '" + strBiblioDbName + "�����ݸ�ʽʱ��������: " + strError;
                        goto ERROR1;
                    }

                    if (strSyntax != "unimarc"
                        && strSyntax != "usmarc")
                    {
                        strError = "��ѡ��Ŀ�� '" + strBiblioDbName + "' ����MARC��ʽ�����ݿ�";
                        goto ERROR1;
                    }
                }


                // Ȼ����cfgs/template�����ļ�
                string strCfgFilePath = strBiblioDbName + "/cfgs/template" + "@" + strServerName;

                string strCode = "";
                byte[] baCfgOutputTimestamp = null;
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = dp2_searchform.GetCfgFile(strCfgFilePath,
                    out strCode,
                    out baCfgOutputTimestamp,
                    out strError);
                if (nRet == -1 || nRet == 0)
                    goto ERROR1;

                SelectRecordTemplateDlg temp_dlg = new SelectRecordTemplateDlg();
                GuiUtil.SetControlFont(temp_dlg, this.Font);

                temp_dlg.Text = "��ѡ���¼�¼ģ�� -- " + dbname_dlg.Path;

                string strSelectedTemplateName = "";
                bool bNotAskTemplateName = false;
                if (selected != null)
                {
                    strSelectedTemplateName = selected.TemplateName;
                    bNotAskTemplateName = selected.NotAskTemplateName;
                }

                temp_dlg.SelectedName = strSelectedTemplateName;
                temp_dlg.AutoClose = (bShift == true ? false : bNotAskTemplateName);
                temp_dlg.NotAsk = bNotAskTemplateName;
                temp_dlg.EnableNotAsk = true;    // 2015/5/11

                nRet = temp_dlg.Initial(
                    false,  // true ��ʾҲ����ɾ��
                    strCode,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�������ļ� '" + strCfgFilePath + "' ��������: " + strError;
                    goto ERROR1;
                }

                temp_dlg.ap = this.MainForm.AppInfo;
                temp_dlg.ApCfgTitle = "marcdetailform_selecttemplatedlg";
                if (this.IsValid() == false)
                    return -1;
                    temp_dlg.ShowDialog(this);  ////


                if (temp_dlg.DialogResult != DialogResult.OK)
                    return 0;

                // ���䱾�ε�ѡ���´ξͲ����ٽ��뱾�Ի�����
                this.selected_templates.Set(CanonicalizePath(dbname_dlg.Path),
                    dbname_dlg.NotAsk,
                    temp_dlg.SelectedName,
                    temp_dlg.NotAsk);

                string strMarcSyntax = "";
                string strOutMarcSyntax = "";
                string strRecord = "";

                // �����ݼ�¼�л��MARC��ʽ
                nRet = MarcUtil.Xml2Marc(temp_dlg.SelectedRecordXml,
                    true,
                    strMarcSyntax,
                    out strOutMarcSyntax,
                    out strRecord,
                    out strError);
                if (nRet == -1)
                {
                    strError = "XMLת����MARC��¼ʱ����: " + strError;
                    goto ERROR1;
                }
                this.SavePath = "dp2library" + ":" + strBiblioDbName + "/?" + "@" + strServerName;

                if (this.IsValid() == false)
                    return -1;

                this.MarcEditor.ClearMarcDefDom();
                this.MarcEditor.Marc = strRecord;   ////
                this.CurrentTimestamp = baCfgOutputTimestamp;

                this.ObjectChanged = false;
                this.BiblioChanged = false;

                DisplayHtml(strRecord, GetSyntaxOID(strOutMarcSyntax));

                this.LinkedSearchForm = null;  // �жϺ�ԭ�������ļ���������ϵ��������û��ǰ��ҳ��
                return 0;
            ERROR1:
                MessageBox.Show(this, strError);
                return -1;
            }
            catch (System.ObjectDisposedException)
            {
                return -1;
            }
        }

        // ���浽ģ��
        public int SaveToTemplate()
        {
            string strError = "";
            int nRet = 0;


            // ���ݲ�ͬ��Э�飬���ò�ͬ��װ��ģ�幦��
            string strProtocol = "";
            string strPath = "";

            if (String.IsNullOrEmpty(this.SavePath) == false)
            {
                // �������������
                nRet = Global.ParsePath(this.SavePath,
                    out strProtocol,
                    out strPath,
                    out strError);
                if (nRet == -1)
                {
                    strError = "����·�� '" + this.SavePath + "' �ַ��������з�������: " + strError;
                    goto ERROR1;
                }
            }
            else
            {
                strProtocol = "dp2library";
            }


            if (strProtocol == "dp2library")
            {
                return SaveToDp2libraryTemplate(strPath);
            }
            else if (strProtocol == "dtlp")
            {
                return SaveToDtlpTemplate(strPath);
            }
            else
            {
                return SaveToDp2libraryTemplate("");
            }

            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // ���浽 DTLPЭ�� ģ��
        // parameters:
        //      strPath DTLPЭ����·�������� localhost/����ͼ��/ctlno/0000001
        public int SaveToDtlpTemplate(string strPath)
        {
            int nRet = 0;
            string strError = "";

            DtlpSearchForm dtlp_searchform = this.GetDtlpSearchForm();

            if (dtlp_searchform == null)
            {
                strError = "û�����ӵĻ��ߴ򿪵�DTLP���������޷�����ģ��";
                goto ERROR1;
            }


            string strServerAddr = "";
            string strDbName = "";
            string strNumber = "";

            // ��������·��
            // return:
            //      -1  ����
            //      0   �ɹ�
            nRet = DtlpChannel.ParseWritePath(strPath,
                out strServerAddr,
                out strDbName,
                out strNumber,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string strStartPath = strServerAddr + "/" + strDbName;

            GetDtlpResDialog dlg = new GetDtlpResDialog();
            GuiUtil.SetControlFont(dlg, this.Font);

            dlg.Text = "��ѡ��Ŀ�����ݿ�";
            dlg.Initial(dtlp_searchform.DtlpChannels,
                dtlp_searchform.DtlpChannel);
            dlg.EnabledIndices = new int[] { DtlpChannel.TypeStdbase };
            dlg.Path = strStartPath;
            dlg.StartPosition = FormStartPosition.CenterScreen;

            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return 0;

            // ���default.cfg�����ļ�
            string strCfgPath = dlg.Path + "/cfgs/default.cfg";
            string strContent = "";

            Cursor.Current = Cursors.WaitCursor;
            nRet = dtlp_searchform.DtlpChannel.GetCfgFile(strCfgPath,
                out strContent,
                out strError);
            Cursor.Current = Cursors.Default;

            if (nRet == -1)
                goto ERROR1;

            // ѡ��ģ��
            SelectRecordTemplateDialog tempdlg = new SelectRecordTemplateDialog();
            GuiUtil.SetControlFont(tempdlg, this.Font);

            tempdlg.LoadMode = false;
            tempdlg.Content = strContent;
            tempdlg.SelectedRecordMarc = this.MarcEditor.Marc;
            tempdlg.StartPosition = FormStartPosition.CenterScreen;

            tempdlg.ShowDialog(this);

            if (tempdlg.DialogResult != DialogResult.OK)
                return 0;

            if (tempdlg.Changed == false)
                return 0;

            Cursor.Current = Cursors.WaitCursor;
            nRet = dtlp_searchform.DtlpChannel.WriteCfgFile(strCfgPath,
                tempdlg.Content,
                out strError);
            Cursor.Current = Cursors.Default;

            if (nRet == -1)
                goto ERROR1;


            MessageBox.Show(this, "�޸�ģ�� '" + strCfgPath + "' �ɹ�");
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // ���浽dp2libraryģ��
        public int SaveToDp2libraryTemplate(string strPath)
        {
            string strError = "";
            int nRet = 0;

            dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

            if (dp2_searchform == null)
            {
                strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷����浱ǰ���ݵ�ģ��";
                goto ERROR1;
            }

            string strServerName = "";
            string strLocalPath = "";

            string strBiblioDbName = "";

            // ������¼·����
            // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
            dp2SearchForm.ParseRecPath(strPath,
                out strServerName,
                out strLocalPath);
            strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);

            string strStartPath = "";

            if (String.IsNullOrEmpty(strServerName) == false
                && String.IsNullOrEmpty(strBiblioDbName) == false)
                strStartPath = strServerName + "/" + strBiblioDbName;
            else if (String.IsNullOrEmpty(strServerName) == false)
                strStartPath = strServerName;

            GetDp2ResDlg dlg = new GetDp2ResDlg();
            GuiUtil.SetControlFont(dlg, this.Font);

            dlg.Text = "��ѡ��Ŀ�����ݿ�";
            dlg.dp2Channels = dp2_searchform.Channels;
            dlg.Servers = this.MainForm.Servers;
            dlg.EnabledIndices = new int[] { dp2ResTree.RESTYPE_DB };
            dlg.Path = strStartPath;

            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return 0;

            string strSyntax = "";

            nRet = dlg.Path.IndexOf("/");
            if (nRet == -1)
            {
                strServerName = dlg.Path;
                strBiblioDbName = "";
                strError = "δѡ��Ŀ�����ݿ�";
                goto ERROR1;
            }
            else
            {
                strServerName = dlg.Path.Substring(0, nRet);
                strBiblioDbName = dlg.Path.Substring(nRet + 1);

                // �����ѡ���ݿ��syntax������Ϊdc

                // ���һ�����ݿ������syntax
                // parameters:
                //      stop    ���!=null����ʾʹ�����stop�����Ѿ�OnStop +=
                //              ���==null����ʾ���Զ�ʹ��this.stop�����Զ�OnStop+=
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = dp2_searchform.GetDbSyntax(
                    null,
                    strServerName,
                    strBiblioDbName,
                    out strSyntax,
                    out strError);
                if (nRet == -1)
                {
                    strError = "��ȡ��Ŀ�� '" + strBiblioDbName + "�����ݸ�ʽʱ��������: " + strError;
                    goto ERROR1;
                }

                if (strSyntax != "unimarc"
                    && strSyntax != "usmarc")
                {
                    strError = "��ѡ��Ŀ�� '" + strBiblioDbName + "' ����MARC��ʽ�����ݿ�";
                    goto ERROR1;
                }
            }


            // Ȼ����cfgs/template�����ļ�
            string strCfgFilePath = strBiblioDbName + "/cfgs/template" + "@" + strServerName;

            string strCode = "";
            byte[] baCfgOutputTimestamp = null;
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = dp2_searchform.GetCfgFile(strCfgFilePath,
                out strCode,
                out baCfgOutputTimestamp,
                out strError);
            if (nRet == -1 || nRet == 0)
                goto ERROR1;

            SelectRecordTemplateDlg tempdlg = new SelectRecordTemplateDlg();
            GuiUtil.SetControlFont(tempdlg, this.Font);
            nRet = tempdlg.Initial(true,
                strCode,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            tempdlg.Text = "��ѡ��Ҫ�޸ĵ�ģ���¼";
            tempdlg.CheckNameExist = false;	// ��OK��ťʱ������"���ֲ�����",���������½�һ��ģ��
            tempdlg.ap = this.MainForm.AppInfo;
            tempdlg.ApCfgTitle = "marcdetailform_selecttemplatedlg";
            tempdlg.ShowDialog(this);

            if (tempdlg.DialogResult != DialogResult.OK)
                return 0;

            // �޸������ļ�����
            if (tempdlg.textBox_name.Text != "")
            {
                string strXml = "";
                /*
                nRet = dp2SearchForm.GetBiblioXml(
                    strSyntax,
                    this.MarcEditor.Marc,
                    out strXml,
                    out strError);
                 * */
                // 2008/5/16 changed
                nRet = MarcUtil.Marc2Xml(
    this.MarcEditor.Marc,
    strSyntax,
    out strXml,
    out strError);

                if (nRet == -1)
                    goto ERROR1;

                // �滻����׷��һ����¼
                nRet = tempdlg.ReplaceRecord(tempdlg.textBox_name.Text,
                    strXml,
                    out strError);
                if (nRet == -1)
                {
                    goto ERROR1;
                }
            }

            if (tempdlg.Changed == false)	// û�б�Ҫ�����ȥ
                return 0;

            string strOutputXml = tempdlg.OutputXml;

            nRet = dp2_searchform.SaveCfgFile(
                strCfgFilePath,
                strOutputXml,
                baCfgOutputTimestamp,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            MessageBox.Show(this, "�޸�ģ�� '"+strCfgFilePath+"' �ɹ�");
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        private void tabControl_main_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.tabControl_main.SelectedTab == this.tabPage_originData)
            {
                // һ�������ı䣬��ʾ��Ϣ�Ͳ����档��������װ��ԭʼ����
                if (this.MarcEditor.Changed == true)
                    this.label_originDataWarning.Text = "���棺MARC�༭���еļ�¼�ѷ����ı䣬�������ԭʼ���ݲ�ͬ��...";
            }
        }

        protected override bool ProcessDialogKey(
Keys keyData)
        {
            /*
            if (keyData == Keys.Enter)
            {
                this.button_OK_Click(this, null);
                return true;
            }*/

            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }

            if (keyData == Keys.F2)
            {
                this.SaveRecord();
                return true;
            }

            if (keyData == Keys.F3)
            {
                this.SaveRecord("saveas");
                return true;
            }

            if (keyData == Keys.F4)
            {
                this.LoadTemplate();
                return true;
            }

            if (keyData == Keys.F5)
            {
                this.Reload();
                return true;
            }

            // return false;
            return base.ProcessDialogKey(keyData);
        }

        private void MarcEditor_VerifyData(object sender, GenerateDataEventArgs e)
        {
            this.VerifyData(sender, e);
        }

        public void VerifyData()
        {
            GenerateDataEventArgs e1 = new GenerateDataEventArgs();
            e1.FocusedControl = this.MarcEditor;
            this.VerifyData(this, e1, null, false);
        }

        // MARC��ʽУ��
        // parameters:
        //      sender    �Ӻδ�����? MarcEditor EntityEditForm
        public void VerifyData(object sender,
            GenerateDataEventArgs e)
        {
            VerifyData(sender, e, null, false);
        }

        // MARC��ʽУ��
        // parameters:
        //      sender    �Ӻδ�����? MarcEditor EntityEditForm
        /// <summary>
        /// MARC��ʽУ��
        /// </summary>
        /// <param name="sender">�Ӻδ�����?</param>
        /// <param name="e">GenerateDataEventArgs���󣬱�ʾ��������</param>
        /// <param name="bAutoVerify">�Ƿ��Զ�У�顣�Զ�У���ʱ�����û�з��ִ����򲻳������ĶԻ���</param>
        /// <returns>0: û�з���У�����; 1: ����У�龯��; 2: ����У�����</returns>
        public int VerifyData(object sender,
            GenerateDataEventArgs e,
            string strSavePath,
            bool bAutoVerify)
        {
            string strError = "";
            string strCode = "";
            string strRef = "";


            if (string.IsNullOrEmpty(strSavePath) == true)
                strSavePath = this.SavePath;

            if (String.IsNullOrEmpty(strSavePath) == true)
            {
                strError = "ȱ������·��";
                goto ERROR1;
            }

            string strProtocol = "";
            string strPath = "";
            int nRet = Global.ParsePath(strSavePath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;


            // dtlpЭ����Զ���������
            if (strProtocol.ToLower() == "dtlp")
            {
                strError = "�ݲ�֧������DTLPЭ��������Զ���������";
                goto ERROR1;
            }

            // Debug.Assert(false, "");
            this.m_strVerifyResult = "����У��...";
            // �Զ�У���ʱ�����û�з��ִ����򲻳������ĶԻ���
            if (bAutoVerify == false)
            {
                // ����̶�������أ��ʹ򿪴���
                DoViewVerifyResult(this.MainForm.PanelFixedVisible == false ? true : false);
            }

            VerifyHost host = new VerifyHost();
            host.DetailForm = this;

            // dp2libraryЭ����Զ���������
            if (strProtocol.ToLower() == "dp2library")
            {
                dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷��������ݴ���";
                    goto ERROR1;
                }

                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                string strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);

                string strCfgFileName = "dp2catalog_marc_verify.fltx";

                string strCfgPath = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;

                byte[] baCfgOutputTimestamp = null;
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = dp2_searchform.GetCfgFile(strCfgPath,
                    out strCode,
                    out baCfgOutputTimestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    strCfgFileName = "dp2catalog_marc_verify.cs";

                    strCfgPath = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;

                    baCfgOutputTimestamp = null;
                    nRet = dp2_searchform.GetCfgFile(strCfgPath,
                        out strCode,
                        out baCfgOutputTimestamp,
                        out strError);
                    if (nRet == 0)
                    {
                        strError = "��������û�ж���·��Ϊ '" + strCfgPath + "' �������ļ�(��.fltx�����ļ�)������У���޷�����";
                        goto ERROR1;
                    } 
                    if (nRet == -1)
                        goto ERROR1;

                    strCfgFileName = "dp2catalog_marc_verify.cs.ref";

                    strCfgPath = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;

                    nRet = dp2_searchform.GetCfgFile(strCfgPath,
                        out strRef,
                        out baCfgOutputTimestamp,
                        out strError);
                    if (nRet == 0)
                    {
                        strError = "��������û�ж���·��Ϊ '" + strCfgPath + "' �������ļ�����Ȼ������.cs�����ļ�������У���޷�����";
                        goto ERROR1;
                    }
                    if (nRet == -1)
                        goto ERROR1;

                    try
                    {
                        // ִ�д���
                        nRet = RunVerifyCsScript(
                            sender,
                            e,
                            strCode,
                            strRef,
                            out host,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    catch (Exception ex)
                    {
                        strError = "ִ�нű���������з����쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                        goto ERROR1;
                    }
                }
                else
                {
                    VerifyFilterDocument filter = null;

                    nRet = this.PrepareVerifyMarcFilter(
                        host,
                        strCode,
                        out filter,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "�����ļ� '" + strCfgFileName + "' �Ĺ����г���:\r\n" + strError;
                        goto ERROR1;
                    }

                    try
                    {

                        nRet = filter.DoRecord(null,
                            this.MarcEditor.Marc,
                            0,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                    }
                    catch (Exception ex)
                    {
                        strError = "filter.DoRecord error: " + ExceptionUtil.GetDebugText(ex);
                        goto ERROR1;
                    }
                }
            }

            bool bVerifyFail = false;
            if (string.IsNullOrEmpty(host.ResultString) == true)
            {
                if (this.m_verifyViewer != null)
                    this.m_verifyViewer.ResultString = "����У��û�з����κδ���";
            }
            else
            {
                if (bAutoVerify == true)
                {
                    // �ӳٴ򿪴���
                    DoViewVerifyResult(this.MainForm.PanelFixedVisible == false ? true : false);
                }
                this.m_verifyViewer.ResultString = host.ResultString;
                this.MainForm.ActivateVerifyResultPage();   // 2014/7/13
                bVerifyFail = true;
            }

            return bVerifyFail == true ? 2 : 0;
        ERROR1:
            MessageBox.Show(this, strError);
            if (this.m_verifyViewer != null)
                this.m_verifyViewer.ResultString = strError;
            return 0;
        }

        int RunVerifyCsScript(
    object sender,
    GenerateDataEventArgs e,
    string strCode,
    string strRef,
            out VerifyHost hostObj,
    out string strError)
        {
            strError = "";
            string[] saRef = null;
            int nRet;
            hostObj = null;

            nRet = ScriptManager.GetRefsFromXml(strRef,
                out saRef,
                out strError);
            if (nRet == -1)
                return -1;

            // 2007/12/4
            ScriptManager.RemoveRefsBinDirMacro(ref saRef);

            string[] saAddRef = {
                                    "system.dll",
                                    "system.xml.dll",
                                    "system.windows.forms.dll",
                                    "system.drawing.dll",
                                    "System.Runtime.Serialization.dll",

									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marceditor.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcfixedfieldcontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.gcatclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.script.dll",
									Environment.CurrentDirectory + "\\digitalplatform.commoncontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									Environment.CurrentDirectory + "\\dp2catalog.exe"
								};

            if (saAddRef != null)
            {
                string[] saTemp = new string[saRef.Length + saAddRef.Length];
                Array.Copy(saRef, 0, saTemp, 0, saRef.Length);
                Array.Copy(saAddRef, 0, saTemp, saRef.Length, saAddRef.Length);
                saRef = saTemp;
            }

            Assembly assembly = null;
            string strErrorInfo = "";
            string strWarningInfo = "";
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saRef,
                null,   // strLibPaths,
                out assembly,
                out strErrorInfo,
                out strWarningInfo);
            if (nRet == -1)
            {
                strError = "�ű����뷢�ִ���򾯸�:\r\n" + strErrorInfo;
                return -1;
            }

            // �õ�Assembly��VerifyHost������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                assembly,
                "dp2Catalog.VerifyHost");
            if (entryClassType == null)
            {

                strError = "dp2Catalog.VerifyHost������û���ҵ�";
                return -1;
            }

            {
                // newһ��VerifyHost��������
                hostObj = (VerifyHost)entryClassType.InvokeMember(null,
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                    null);

                if (hostObj == null)
                {
                    strError = "new VerifyHost���������ʧ��";
                    return -1;
                }

                // ΪHost���������ò���
                hostObj.DetailForm = this;
                hostObj.Assembly = assembly;

                HostEventArgs e1 = new HostEventArgs();
                e1.e = e;   // 2009/2/24

                hostObj.Main(sender, e1);
            }

            return 0;
        }

        public int PrepareVerifyMarcFilter(
    VerifyHost host,
    string strContent,
    out VerifyFilterDocument filter,
    out string strError)
        {
            strError = "";

            // �´���
            // string strFilterFileContent = "";

            filter = new VerifyFilterDocument();

            filter.FilterHost = host;
            filter.strOtherDef = "VerifyHost Host = null;";

            filter.strPreInitial = " VerifyFilterDocument doc = (VerifyFilterDocument)this.Document;\r\n";
            filter.strPreInitial += " Host = ("
                + "VerifyHost" + ")doc.FilterHost;\r\n";

            try
            {
                filter.LoadContent(strContent);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            string strCode = "";    // c#����

            int nRet = filter.BuildScriptFile(out strCode,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string[] saAddRef1 = {
                                    "system.dll",
                                    "system.xml.dll",
                                    "system.windows.forms.dll",
                                    "system.drawing.dll",
                                    "System.Runtime.Serialization.dll",

									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marceditor.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcfixedfieldcontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.gcatclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.script.dll",
									Environment.CurrentDirectory + "\\digitalplatform.commoncontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									Environment.CurrentDirectory + "\\dp2catalog.exe"
								};

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
                // MessageBox.Show(this, strWarning);
            }

            filter.Assembly = assembly;

            return 0;
        ERROR1:
            return -1;
        }

        string m_strVerifyResult = "";

        void DoViewVerifyResult(bool bOpenWindow)
        {
            string strError = "";

            // �Ż���������ν�ؽ��з���������
            if (bOpenWindow == false)
            {
                if (this.MainForm.PanelFixedVisible == false
                    && (m_verifyViewer == null || m_verifyViewer.Visible == false))
                    return;
            }


            if (this.m_verifyViewer == null
                || (bOpenWindow == true && this.m_verifyViewer.Visible == false))
            {
                m_verifyViewer = new VerifyViewerForm();
                // GuiUtil.SetControlFont(m_viewer, this.Font, false);
            }

            // m_viewer.MainForm = this.MainForm;  // �����ǵ�һ��
            m_verifyViewer.Text = "У����";
            m_verifyViewer.ResultString = this.m_strVerifyResult;

            m_verifyViewer.DoDockEvent -= new DoDockEventHandler(m_viewer_DoDockEvent);
            m_verifyViewer.DoDockEvent += new DoDockEventHandler(m_viewer_DoDockEvent);

            m_verifyViewer.FormClosed -= new FormClosedEventHandler(m_viewer_FormClosed);
            m_verifyViewer.FormClosed += new FormClosedEventHandler(m_viewer_FormClosed);
            
            m_verifyViewer.Locate -= new LocateEventHandler(m_viewer_Locate);
            m_verifyViewer.Locate += new LocateEventHandler(m_viewer_Locate);

            if (bOpenWindow == true)
            {
                if (m_verifyViewer.Visible == false)
                {
                    this.MainForm.AppInfo.LinkFormState(m_verifyViewer, "verify_viewer_state");
                    m_verifyViewer.Show(this);
                    m_verifyViewer.Activate();

                    this.MainForm.CurrentVerifyResultControl = null;
                }
                else
                {
                    if (m_verifyViewer.WindowState == FormWindowState.Minimized)
                        m_verifyViewer.WindowState = FormWindowState.Normal;
                    m_verifyViewer.Activate();
                }
            }
            else
            {
                if (m_verifyViewer.Visible == true)
                {

                }
                else
                {
                    if (this.MainForm.CurrentVerifyResultControl != m_verifyViewer.ResultControl)
                        m_verifyViewer.DoDock(false); // �����Զ���ʾFixedPanel
                }
            }
            return;
        ERROR1:
            MessageBox.Show(this, "DoViewVerifyResult() ����: " + strError);
        }

        void m_viewer_DoDockEvent(object sender, DoDockEventArgs e)
        {
            if (this.MainForm.CurrentVerifyResultControl != m_verifyViewer.ResultControl)
                this.MainForm.CurrentVerifyResultControl = m_verifyViewer.ResultControl;

            if (e.ShowFixedPanel == true
                && this.MainForm.PanelFixedVisible == false)
                this.MainForm.PanelFixedVisible = true;

            m_verifyViewer.Docked = true;
            m_verifyViewer.Visible = false;
        }

        void m_viewer_Locate(object sender, LocateEventArgs e)
        {
            string strError = "";

            string[] parts = e.Location.Split(new char[] { ',' });
            string strFieldName = "";
            int nFieldIndex = 0;
            string strSubfieldName = "";
            int nSubfieldIndex = 0;

            int nCharPos = 0;
            int nRet = 0;

            if (parts.Length == 0)
                return;
            if (parts.Length >= 1)
            {
                string strValue = parts[0].Trim();
                nRet = strValue.IndexOf("#");
                if (nRet == -1)
                    strFieldName = strValue;
                else
                {
                    strFieldName = strValue.Substring(0, nRet);
                    string strNumber = strValue.Substring(nRet + 1);
                    if (string.IsNullOrEmpty(strNumber) == false)
                    {
                        try
                        {
                            nFieldIndex = Convert.ToInt32(strNumber);
                        }
                        catch
                        {
                            strError = "�ֶ�λ�� '" + strNumber + "' ��ʽ����ȷ...";
                            goto ERROR1;
                        }
                        nFieldIndex--;
                    }
                }
            }

            if (parts.Length >= 2)
            {
                string strValue = parts[1].Trim();
                nRet = strValue.IndexOf("#");
                if (nRet == -1)
                    strSubfieldName = strValue;
                else
                {
                    strSubfieldName = strValue.Substring(0, nRet);
                    string strNumber = strValue.Substring(nRet + 1);
                    if (string.IsNullOrEmpty(strNumber) == false)
                    {
                        try
                        {
                            nSubfieldIndex = Convert.ToInt32(strNumber);
                        }
                        catch
                        {
                            strError = "���ֶ�λ�� '" + strNumber + "' ��ʽ����ȷ...";
                            goto ERROR1;
                        }
                        nSubfieldIndex--;
                    }
                }
            }


            if (parts.Length >= 3)
            {
                string strValue = parts[2].Trim();
                if (string.IsNullOrEmpty(strValue) == false)
                {
                    try
                    {
                        nCharPos = Convert.ToInt32(strValue);
                    }
                    catch
                    {
                        strError = "�ַ�λ�� '" + strValue + "' ��ʽ����ȷ...";
                        goto ERROR1;
                    }
                    if (nCharPos > 0)
                        nCharPos--;
                }
            }

            Field field = this.MarcEditor.Record.Fields[strFieldName, nFieldIndex];
            if (field == null)
            {
                strError = "��ǰMARC�༭���в����� ��Ϊ '" + strFieldName + "' λ��Ϊ " + nFieldIndex.ToString() + " ���ֶ�";
                goto ERROR1;
            }

            if (string.IsNullOrEmpty(strSubfieldName) == true)
            {
                // �ֶ���
                if (nCharPos == -1)
                {
                    this.MarcEditor.SetActiveField(field, 2);
                }
                // �ֶ�ָʾ��
                else if (nCharPos == -2)
                {
                    this.MarcEditor.SetActiveField(field, 1);
                }
                else
                {
                    this.MarcEditor.FocusedField = field;
                    this.MarcEditor.SelectCurEdit(nCharPos, 0);
                }
                this.MarcEditor.EnsureVisible();
                return;
            }

            this.MarcEditor.FocusedField = field;
            this.MarcEditor.EnsureVisible();

            Subfield subfield = field.Subfields[strSubfieldName, nSubfieldIndex];
            if (subfield == null)
            {
                strError = "��ǰMARC�༭���в����� ��Ϊ '" + strSubfieldName + "' λ��Ϊ " + nSubfieldIndex.ToString() + " �����ֶ�";
                goto ERROR1;
            }

            this.MarcEditor.SelectCurEdit(subfield.Offset + 2, 0);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void m_viewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_verifyViewer != null)
            {
                if (this.MainForm != null && this.MainForm.AppInfo != null)
                    this.MainForm.AppInfo.UnlinkFormState(m_verifyViewer);

                this.m_verifyViewer = null;
            }
        }

        // ����ʱ�Զ�����
        public bool AutoVerifyData
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "entity_form",
                    "verify_data_when_saving",
                    false);
            }
        }

        private void MarcEditor_ParseMacro(object sender, ParseMacroEventArgs e)
        {
            string strResult = "";
            string strError = "";

            // ������MacroUtil���д���
            int nRet = m_macroutil.Parse(
                e.Simulate,
                e.Macro,
                out strResult,
                out strError);
            if (nRet == -1)
            {
                e.ErrorInfo = strError;
                return;
            }

            e.Value = strResult;
        }


        void m_macroutil_ParseOneMacro(object sender, ParseOneMacroEventArgs e)
        {
            string strError = "";
            string strName = Unquote(e.Macro);  // ȥ���ٷֺ�

            // ��������
            string strFuncName = "";
            string strParams = "";

            int nRet = strName.IndexOf(":");
            if (nRet == -1)
            {
                strFuncName = strName.Trim();
            }
            else
            {
                strFuncName = strName.Substring(0, nRet).Trim();
                strParams = strName.Substring(nRet + 1).Trim();
            }

            if (strName == "username"
                && String.IsNullOrEmpty(this.SavePath) == false)
            {
                e.Value = this.CurrentUserName;
                return;
            }

            string strValue = "";
            // ��marceditor_macrotable.xml�ļ��н�����
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = MacroUtil.GetFromLocalMacroTable(PathUtil.MergePath(this.MainForm.DataDir, "marceditor_macrotable.xml"),
                strName,
                e.Simulate,
                out strValue,
                out strError);
            if (nRet == -1)
            {
                e.Canceled = true;
                e.ErrorInfo = strError;
                return;
            }

            if (nRet == 1)
            {
                e.Value = strValue;
                return;
            }

            ERROR1:
            e.Canceled = true;  // ���ܽ��ʹ���
            return;
        }

        static string Unquote(string strValue)
        {
            if (strValue.Length == 0)
                return "";
            if (strValue[0] == '%')
                strValue = strValue.Substring(1);
            if (strValue.Length == 0)
                return "";
            if (strValue[strValue.Length - 1] == '%')
                return strValue.Substring(0, strValue.Length - 1);

            return strValue;
        }

        public string CurrentUserName
        {
            get
            {
                string strError = "";
                int nRet = 0;
                string strProtocol = "";
                string strPath = "";
                nRet = Global.ParsePath(this.SavePath,
                    out strProtocol,
                    out strPath,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (strProtocol == "dp2library")
                {
                    dp2SearchForm dp2_searchform = null;

                    if (this.LinkedSearchForm is dp2SearchForm)
                        dp2_searchform = (dp2SearchForm)this.LinkedSearchForm;
                    else
                    {
                        dp2_searchform = this.GetDp2SearchForm();
                        if (dp2_searchform == null)
                        {
                            strError = "û�����ӵĻ��ߴ򿪵�dp2������";
                            goto ERROR1;
                        }
                    }

                    if (dp2_searchform.Channel == null)
                    {
                        string strServerName = "";
                        string strLocalPath = "";

                        // ������¼·����
                        // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                        dp2SearchForm.ParseRecPath(strPath,
                            out strServerName,
                            out strLocalPath);

                        nRet = dp2_searchform.ForceLogin(
    null,
    strServerName,
    out strError);
                    }

                    if (dp2_searchform.Channel != null)
                        return dp2_searchform.Channel.UserName;
                    return "";
                }

                return "";
            ERROR1:
                // throw new Exception(strError);
                return null;
            }
        }

        private void MarcEditor_Enter(object sender, EventArgs e)
        {
            API.PostMessage(this.Handle, WM_FILL_MARCEDITOR_SCRIPT_MENU, 0, 0);
        }

        private void MarcEditor_GetTemplateDef(object sender, GetTemplateDefEventArgs e)
        {
            if (this.m_detailHostObj == null)
            {
                int nRet = 0;
                string strError = "";

                // ��ʼ�� dp2catalog_marc_autogen.cs �� Assembly����new MarcDetailHost����
                // return:
                //      -2  �����Assembly
                //      -1  error
                //      0   û�����³�ʼ��Assembly������ֱ������ǰCache��Assembly
                //      1   ����(�����״�)��ʼ����Assembly
                nRet = InitialAutogenAssembly(out strError);
                if (nRet == -1 || nRet == -2)
                {
                    e.ErrorInfo = strError;
                    return;
                }
                if (nRet == 0)
                {
                    if (this.m_detailHostObj == null)
                    {
                        e.Canceled = true;
                        return; // �������߱����޷���ʼ��
                    }
                }
                Debug.Assert(this.m_detailHostObj != null, "");
            }

            // ����ű�����û����Ӧ�Ļص�����
            if (this.m_detailHostObj.GetType().GetMethod("GetTemplateDef",
                BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.InvokeMethod
                ) == null)
            {
                e.Canceled = true;
                return;
            }

            // �����������ĳ�Ա����
            Type classType = m_detailHostObj.GetType();
            try
            {
                classType.InvokeMember("GetTemplateDef",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod
                    ,
                    null,
                    this.m_detailHostObj,
                    new object[] { sender, e });
            }
            catch (Exception ex)
            {
                e.ErrorInfo = GetExceptionMessage(ex) + "\r\n\r\n" + ExceptionUtil.GetDebugText(ex);  // GetExceptionMessage(ex);
                return;
            }
        }

        static string GetExceptionMessage(Exception ex)
        {
            string strResult = ex.GetType().ToString() + ":" + ex.Message;
            while (ex != null)
            {
                if (ex.InnerException != null)
                    strResult += "\r\n" + ex.InnerException.GetType().ToString() + ": " + ex.InnerException.Message;

                ex = ex.InnerException;
            }

            return strResult;
        }

        private void MarcEditor_TextChanged(object sender, EventArgs e)
        {
            this.RecordVersion = DateTime.Now.Ticks;
        }

        private void MarcDetailForm_Deactivate(object sender, EventArgs e)
        {
            SyncRecord();
        }

        // ���� MARC ��ʽ�ַ������ OID �ַ���
        public static string GetSyntaxOID(string strMarcSyntax)
        {
            if (strMarcSyntax == "unimarc" || strMarcSyntax == "")
                return "1.2.840.10003.5.1";
            else if (strMarcSyntax == "usmarc")
                return "1.2.840.10003.5.10";
            else if (strMarcSyntax == "dt1000reader")
                return "1.2.840.10003.5.dt1000reader";
            else
                return "";
        }

        // ���� OID ��� MARC ��ʽ�ַ���
        public static string GetMarcSyntax(string strOID)
        {
            if (strOID == "1.2.840.10003.5.1")
                return "unimarc";
            if (strOID == "1.2.840.10003.5.10")
                return "usmarc";

            return null;
        }

        void DisplayHtml(string strMARC, string strSytaxOID)
        {
            string strError = "";
            string strHtmlString = "";
                    // return:
        //      -1  ����
        //      0   .fltx �ļ�û���ҵ�
        //      1   �ɹ�
            int nRet = this.MainForm.BuildMarcHtmlText(
                strSytaxOID,
                strMARC,
                out strHtmlString,
                out strError);
            if (nRet == -1)
                strHtmlString = strError.Replace("\r\n", "<br/>");
            if (nRet == 0)
            {
                // TODO: ���
                return;
            }

            Global.SetHtmlString(this.webBrowser_html,
    strHtmlString,
    this.MainForm.DataDir,
    "marcdetailform_biblio");
        }

        #region MARC21 --> HTML

        static string GetMaterialType(MarcRecord record)
        {
            if ("at".IndexOf(record.Header[6]) != -1
                && "acdm".IndexOf(record.Header[7]) != -1)
                return "Book";  // Books

            if (record.Header[6] == "m")
                return "Computer Files";

            if ("df".IndexOf(record.Header[6]) != -1)
                return "Map";  // Maps

            if ("cdij".IndexOf(record.Header[6]) != -1)
                return "Music";  // Music

            if ("a".IndexOf(record.Header[6]) != -1
    && "bis".IndexOf(record.Header[7]) != -1)
                return "Periodical or Newspaper";  // Continuing Resources

            if ("gkor".IndexOf(record.Header[6]) != -1)
                return "Visual Material";  // Visual Materials

            if (record.Header[6] == "p")
                return "Mixed Material";    // Mixed Materials

            return "";
        }

        // ֱ�Ӵ���ÿ�����ֶε�����
        static string ConcatSubfields(MarcNodeList nodes)
        {
            StringBuilder text = new StringBuilder(4096);
            foreach (MarcNode node in nodes)
            {
                text.Append(node.Content + " ");
            }

            return text.ToString().Trim();
        }
        // ��Ϲ������ɸ���ͨ�ֶ�����
        // parameters:
        //      strSubfieldNameList ɸѡ�����ֶ����б����Ϊ null����ʾ��ɸѡ
        static string BuildFields(MarcNodeList fields,
            string strSubfieldNameList = null)
        {
            StringBuilder text = new StringBuilder(4096);
            int i = 0;
            foreach (MarcNode field in fields)
            {
                MarcNodeList nodes = field.select("subfield");
                if (nodes.count > 0)
                {

                    StringBuilder temp = new StringBuilder(4096);
                    foreach (MarcNode subfield in nodes)
                    {
                        if (strSubfieldNameList != null)
                        {
                            if (strSubfieldNameList.IndexOf(subfield.Name) == -1)
                                continue;
                        }
                        temp.Append(subfield.Content + " ");
                    }

                    if (temp.Length > 0)
                    {
                        if (i > 0)
                            text.Append("|");
                        text.Append(temp.ToString().Trim());
                        i++;
                    }
                }
            }

            return text.ToString().Trim();
        }

        // ��Ϲ������ɸ������ֶ�����
        static string BuildSubjects(MarcNodeList fields)
        {
            StringBuilder text = new StringBuilder(4096);
            int i = 0;
            foreach (MarcNode field in fields)
            {
                MarcNodeList nodes = field.select("subfield");
                if (nodes.count > 0)
                {
                    if (i > 0)
                        text.Append("|");

                    bool bPrevContent = false;  // ǰһ�����ֶ��ǳ��� x y z ��������ֶ�
                    StringBuilder temp = new StringBuilder(4096);
                    foreach (MarcNode subfield in nodes)
                    {
                        if (subfield.Name == "2")
                            continue;   // ��ʹ�� $2

                        if (subfield.Name == "x"
                            || subfield.Name == "y"
                            || subfield.Name == "z"
                            || subfield.Name == "v")
                        {
                            temp.Append("--");
                            temp.Append(subfield.Content);
                            bPrevContent = false;
                        }
                        else
                        {
                            if (bPrevContent == true)
                                temp.Append(" ");
                            temp.Append(subfield.Content);
                            bPrevContent = true;
                        }
                    }

                    text.Append(temp.ToString().Trim());
                    i++;
                }
            }

            return text.ToString().Trim();
        }

        // ��Ϲ������ɸ�856�ֶ�����
        static string BuildLinks(MarcNodeList fields)
        {
            StringBuilder text = new StringBuilder(4096);
            int i = 0;
            foreach (MarcNode field in fields)
            {
                MarcNodeList nodes = field.select("subfield");
                if (nodes.count > 0)
                {
                    string u = "";
                    MarcNodeList single = nodes.select("subfield[@name='u']");
                    if (single.count > 0)
                    {
                        u = single[0].Content;
                    }

                    string z = "";
                    single = nodes.select("subfield[@name='z']");
                    if (single.count > 0)
                    {
                        z = single[0].Content;
                    }

                    string t3 = "";
                    single = nodes.select("subfield[@name='3']");
                    if (single.count > 0)
                    {
                        t3 = single[0].Content;
                    }

                    if (i > 0)
                        text.Append("|");

                    StringBuilder temp = new StringBuilder(4096);

                    if (string.IsNullOrEmpty(t3) == false)
                        temp.Append(t3 + ": <|");

                    temp.Append("url:" + u);
                    temp.Append(" text:" + u);
                    if (string.IsNullOrEmpty(z) == false)
                        temp.Append("|>  " + z);

                    text.Append(temp.ToString().Trim());
                    i++;
                }
            }

            return text.ToString().Trim();
        }

        string GetHeadString(bool bAjax = true)
        {
            string strCssFilePath = PathUtil.MergePath(this.MainForm.DataDir, "bibliohtml.css");

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

        int BuildMarc21Html(string strMARC,
            out string strHtmlString,
            out string strError)
        {
            strError = "";
            strHtmlString = "";

            StringBuilder text = new StringBuilder();

            List<NameValueLine> results = null;
            int nRet = ScriptMarc21(strMARC,
            out results,
            out strError);
            if (nRet == -1)
                return -1;

            text.Append("<html>" + GetHeadString (false)+ "<body><table class='biblio'>");
            foreach (NameValueLine line in results)
            {
                text.Append("<tr class='content'>");

                text.Append("<td class='name'>" + HttpUtility.HtmlEncode(line.Name) + "</td>");
                text.Append("<td class='value'>" + HttpUtility.HtmlEncode(line.Value) + "</td>");

                text.Append("</tr>");
            }
            text.Append("</table></body></html>");
            strHtmlString = text.ToString();
            return 0;
        }

        static int ScriptMarc21(string strMARC,
            out List<NameValueLine> results,
            out string strError)
        {
            strError = "";
            results = new List<NameValueLine>();

            MarcRecord record = new MarcRecord(strMARC);

            if (record.ChildNodes.count == 0)
                return 0;

            // LC control no.
            MarcNodeList nodes = record.select("field[@name='010']/subfield[@name='a']");
            if (nodes.count > 0)
            {
                results.Add(new NameValueLine("LC control no.", nodes[0].Content.Trim()));
            }

            // Type of material
            results.Add(new NameValueLine("Type of material", GetMaterialType(record)));

            // Personal name
            MarcNodeList fields = record.select("field[@name='100']");
            foreach (MarcNode field in fields)
            {
                nodes = field.select("subfield");
                if (nodes.count > 0)
                {
                    results.Add(new NameValueLine("Personal name", ConcatSubfields(nodes)));
                }
            }

            // Corporate name
            fields = record.select("field[@name='110']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Corporate name", BuildFields(fields)));
            }

            // Uniform title
            fields = record.select("field[@name='240']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Uniform title", BuildFields(fields)));
            }

            // Main title
            fields = record.select("field[@name='245']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Main title", BuildFields(fields)));
            }
#if NO
            foreach (MarcNode field in fields)
            {
                nodes = field.select("subfield");
                if (nodes.count > 0)
                {
                    results.Add(new OneLine("Main title", ConcatSubfields(nodes)));
                }
            }
#endif

            // Portion of title
            fields = record.select("field[@name='246' and @indicator2='0']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Portion of title", BuildFields(fields)));
            }

            // Spine title
            fields = record.select("field[@name='246' and @indicator2='8']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Spine title", BuildFields(fields)));
            }

            // Edition
            fields = record.select("field[@name='250']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Edition", BuildFields(fields)));
            }

            // Published/Created
            fields = record.select("field[@name='260']");
            foreach (MarcNode field in fields)
            {
                nodes = field.select("subfield");
                if (nodes.count > 0)
                {
                    results.Add(new NameValueLine("Published/Created", ConcatSubfields(nodes)));
                }
            }

            // Related names
            fields = record.select("field[@name='700' or @name='710' or @name='711']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Related names", BuildFields(fields)));
            }

            // Related titles
            fields = record.select("field[@name='730' or @name='740']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Related titles", BuildFields(fields)));
            }

            // Description
            fields = record.select("field[@name='300' or @name='362']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Description", BuildFields(fields)));
            }

            // ISBN
            fields = record.select("field[@name='020']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("ISBN", BuildFields(fields)));
            }

            // Current frequency
            fields = record.select("field[@name='310']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Current frequency", BuildFields(fields)));
            }

            // Former title
            fields = record.select("field[@name='247']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Former title", BuildFields(fields)));
            }

            // Former frequency
            fields = record.select("field[@name='321']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Former frequency", BuildFields(fields)));
            }

            // Continues
            fields = record.select("field[@name='780']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Continues", BuildFields(fields)));
            }

            // ISSN
            MarcNodeList subfields = record.select("field[@name='022']/subfield[@name='a']");
            if (subfields.count > 0)
            {
                results.Add(new NameValueLine("ISSN", ConcatSubfields(subfields)));
            }

            // Linking ISSN
            subfields = record.select("field[@name='022']/subfield[@name='l']");
            if (subfields.count > 0)
            {
                results.Add(new NameValueLine("Linking ISSN", ConcatSubfields(subfields)));
            }

            // Invalid LCCN
            subfields = record.select("field[@name='010']/subfield[@name='z']");
            if (subfields.count > 0)
            {
                results.Add(new NameValueLine("Invalid LCCN", ConcatSubfields(subfields)));
            }

            // Contents
            fields = record.select("field[@name='505' and @indicator1='0']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Contents", BuildFields(fields)));
            }

            // Partial contents
            fields = record.select("field[@name='505' and @indicator1='2']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Partial contents", BuildFields(fields)));
            }

            // Computer file info
            fields = record.select("field[@name='538']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Computer file info", BuildFields(fields)));
            }

            // Notes
            fields = record.select("field[@name='500'  or @name='501' or @name='504' or @name='561' or @name='583' or @name='588' or @name='590']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Notes", BuildFields(fields)));
            }

            // References
            fields = record.select("field[@name='510']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("References", BuildFields(fields)));
            }

            // Additional formats
            fields = record.select("field[@name='530' or @name='533' or @name='776']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Additional formats", BuildFields(fields)));
            }



            // Subjects
            fields = record.select("field[@name='600' or @name='610' or @name='630' or @name='650' or @name='651']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Subjects", BuildSubjects(fields)));
            }

            // Form/Genre
            fields = record.select("field[@name='655']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Form/Genre", BuildSubjects(fields)));
            }

            // Series
            fields = record.select("field[@name='440' or @name='490' or @name='830']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Series", BuildFields(fields)));
            }


            // LC classification
            fields = record.select("field[@name='050']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("LC classification", BuildFields(fields)));
            }
#if NO
            foreach (MarcNode field in fields)
            {
                nodes = field.select("subfield");
                if (nodes.count > 0)
                {
                    results.Add(new OneLine("LC classification", ConcatSubfields(nodes)));
                }
            }
#endif

            // NLM class no.
            fields = record.select("field[@name='060']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("NLM class no.", BuildFields(fields)));
            }


            // Dewey class no.
            // ��Ҫ $2
            fields = record.select("field[@name='082']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Dewey class no.", BuildFields(fields, "a")));
            }


            // NAL class no.
            fields = record.select("field[@name='070']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("NAL class no.", BuildFields(fields)));
            }

            // National bib no.
            fields = record.select("field[@name='015']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("National bib no.", BuildFields(fields, "a")));
            }

            // National bib agency no.
            fields = record.select("field[@name='016']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("National bib agency no.", BuildFields(fields, "a")));
            }

            // LC copy
            fields = record.select("field[@name='051']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("LC copy", BuildFields(fields)));
            }

            // Other system no.
            fields = record.select("field[@name='035'][subfield[@name='a']]");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Other system no.", BuildFields(fields, "a")));
            }
#if NO
            fields = record.select("field[@name='035']");
            foreach (MarcNode field in fields)
            {
                nodes = field.select("subfield[@name='a']");
                if (nodes.count > 0)
                {
                    results.Add(new OneLine("Other system no.", ConcatSubfields(nodes)));
                }
            }
#endif

            // Reproduction no./Source
            fields = record.select("field[@name='037']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Reproduction no./Source", BuildFields(fields)));
            }

            // Geographic area code
            fields = record.select("field[@name='043']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Geographic area code", BuildFields(fields)));
            }

            // Quality code
            fields = record.select("field[@name='042']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Quality code", BuildFields(fields)));
            }

            // Links
            fields = record.select("field[@name='856'or @name='859']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Links", BuildLinks(fields)));
            }

            // Content type
            fields = record.select("field[@name='336']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Content type", BuildFields(fields, "a")));
            }

            // Media type
            fields = record.select("field[@name='337']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Media type", BuildFields(fields, "a")));
            }

            // Carrier type
            fields = record.select("field[@name='338']");
            if (fields.count > 0)
            {
                results.Add(new NameValueLine("Carrier type", BuildFields(fields, "a")));
            }

            return 0;
        }



        #endregion


    }

    public class VerifyHost
    {
        public MarcDetailForm DetailForm = null;
        public string ResultString = "";
        public Assembly Assembly = null;

        public void Invoke(string strFuncName)
        {
            Type classType = this.GetType();

            // ���ó�Ա����
            classType.InvokeMember(strFuncName,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.InvokeMethod
                ,
                null,
                this,
                null);
        }

        public virtual void Main(object sender, HostEventArgs e)
        {

        }
    }

    public class VerifyFilterDocument : FilterDocument
    {
        public VerifyHost FilterHost = null;
    }

    public class NameValueLine
    {
        public string Name = "";
        public string Value = "";

        public NameValueLine()
        {
        }

        public NameValueLine(string strName, string strValue)
        {
            Name = strName;
            Value = strValue;
        }
    }

}