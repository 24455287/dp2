using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Web;   // HttpUtility
using System.Reflection;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.Script;

using DigitalPlatform.CirculationClient;
using DigitalPlatform.GUI;

namespace dp2Catalog
{
    public partial class DcForm : Form
    {
        public LoginInfo LoginInfo = new LoginInfo();

        int m_nTimerCount = 0;

        const int WM_LOADSIZE = API.WM_USER + 201;
        const int WM_ENABLE_UPDATE = API.WM_USER + 202;

        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;

        public ISearchForm LinkedSearchForm = null;

        /*
        ISearchForm m_linkedSearchForm = null;

        public ISearchForm LinkedSearchForm
        {
            get
            {
                return this.m_linkedSearchForm;
            }
            set
            {
                this.m_linkedSearchForm = value;

                // �޸���صİ�ť״̬
                if (this.m_linkedSearchForm != null)
                {
                    MainForm.toolButton_prev.Enabled = true;
                    MainForm.toolButton_next.Enabled = true;
                }
                else
                {
                    MainForm.toolButton_prev.Enabled = false;
                    MainForm.toolButton_next.Enabled = false;
                }
            }
        }
         * */

        DigitalPlatform.Z3950.Record CurrentRecord = null;

        Encoding CurrentEncoding = Encoding.GetEncoding(936);

        public string AutoDetectedMarcSyntaxOID = "";

        byte[] CurrentTimestamp = null;

        string DcCfgFilename = "";

        // public LibraryChannel Channel = new LibraryChannel();


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
            }

        }

        public DcForm()
        {
            InitializeComponent();
        }

        private void DcForm_Load(object sender, EventArgs e)
        {
            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������

            /*
            Global.FillEncodingList(this.comboBox_originDataEncoding,
                false);
             * */



            // ��ʼ��DC�༭��
            string strCfgFilename = this.MainForm.DataDir + "\\dc_define.xml";

            string strError = "";
            int nRet = this.DcEditor.LoadCfg(strCfgFilename,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
            }
            else
            {
                DcCfgFilename = strCfgFilename;
            }

            this.LoadFontToDcEditor();

            this.binaryResControl1.ContentChanged -= new ContentChangedEventHandler(binaryResControl1_ContentChanged);
            this.binaryResControl1.ContentChanged += new ContentChangedEventHandler(binaryResControl1_ContentChanged);

            this.binaryResControl1.Channel = null;
            this.binaryResControl1.Stop = this.stop;

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);
        }

        private void DcForm_FormClosing(object sender, FormClosingEventArgs e)
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

            if (this.BiblioChanged == true
                || this.ObjectChanged == true)
            {

                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
                    "DcForm",
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

        private void DcForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }

            SaveSize();
        }

        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_LOADSIZE:
                    LoadSize();
                    return;
                    /*
                case WM_ENABLE_UPDATE:
                    MessageBox.Show(this, "end");
                    this.DcEditor.EnableUpdate();
                    return;
                     * */
            }
            base.DefWndProc(ref m);
        }

        public void LoadSize()
        {
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state",
                MainForm.DefaultMdiWindowWidth,
                MainForm.DefaultMdiWindowHeight);


            // ���splitContainer_main��״̬
            int nValue = MainForm.AppInfo.GetInt(
            "dcform",
            "splitContainer_main",
            -1);
            if (nValue != -1)
                this.splitContainer_main.SplitterDistance = nValue;

        }

        public void SaveSize()
        {
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                MainForm.AppInfo.SaveMdiChildFormStates(this,
                    "mdi_form_state");

                // ����splitContainer_main��״̬
                MainForm.AppInfo.SetInt(
                "dcform",
                "splitContainer_main",
                    this.splitContainer_main.SplitterDistance);
            }
        }

        void binaryResControl1_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            // SetSaveAllButtonState(true);
        }

        // ������Ϣ�Ƿ񱻸ı�
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

        // ��Ŀ��Ϣ�Ƿ񱻸ı�
        public bool BiblioChanged
        {
            get
            {
                if (this.DcEditor != null)
                {
                    // ���object id�����ı䣬��ô����MARCû�иı䣬�����ĺϳ�XMLҲ�����˸ı�
                    if (this.binaryResControl1 != null)
                    {
                        if (this.binaryResControl1.IsIdUsageChanged() == true)
                            return true;
                    }

                    return this.DcEditor.Changed;
                }

                return false;
            }
            set
            {
                if (this.DcEditor != null)
                    this.DcEditor.Changed = value;
            }
        }

        // ��õ�ǰ���޸ı�־�Ĳ��ֵ�����
        string GetCurrentChangedPartName()
        {
            string strPart = "";

            if (this.BiblioChanged == true)
                strPart += "��Ŀ��Ϣ";

            if (this.ObjectChanged == true)
            {
                if (strPart != "")
                    strPart += "��";
                strPart += "������Ϣ";
            }

            return strPart;
        }

        public void Reload()
        {
            LoadRecordByPath("current");
        }

        // �������ݿ�������λ��װ�ؼ�¼
        public int LoadRecordByPath(string strDirection)
        {
            string strError = "";
            int nRet = 0;

            if (this.BiblioChanged == true
                || this.ObjectChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪװ��������? ",
                    "DcForm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return 0;
            }

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

            if (strProtocol != "dp2library")
            {
                strError = "���ܴ���Э��" + strProtocol;
                goto ERROR1;
            }

            return LoadDp2Record(dp2_searchform,
                strPath,
                strDirection,
                true);
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // �ӽ������װ�ؼ�¼
        public int LoadRecord(string strDirection)
        {
            string strError = "";

            if (this.BiblioChanged == true
                || this.ObjectChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪװ��������? ",
                    "DcForm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return 0;
            }

            ISearchForm searchform = null;

            if (this.LinkedSearchForm == null)
            {
                strError = "û�й����ļ��������޷��Ӽ����������װ�ؼ�¼";
                goto ERROR1;

                /*
                searchform = this.GetDp2SearchForm();

                if (searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷�����LoadRecord()";
                    goto ERROR1;
                }*/
            }
            else
            {
                searchform = this.LinkedSearchForm;
            }

            string strPath = this.textBox_tempRecPath.Text;
            if (String.IsNullOrEmpty(strPath) == true)
            {
                strError = "textBox_tempRecPath��·��Ϊ��";
                goto ERROR1;
            }

            // �������������
            string strProtocol = "";
            string strResultsetName = "";
            string strIndex = "";

            int nRet = MarcDetailForm.ParsePath(strPath,
                out strProtocol,
                out strResultsetName,
                out strIndex,
                out strError);
            if (nRet == -1)
            {
                strError = "����·�� '" + strPath + "' �ַ��������з�������: " + strError;
                goto ERROR1;
            }


            if (strProtocol != searchform.CurrentProtocol)
            {
                strError = "��������Э���Ѿ������ı�";
                goto ERROR1;
            }

            if (strResultsetName != searchform.CurrentResultsetPath)
            {
                strError = "������Ѿ������ı�";
                goto ERROR1;
            }

            int index = 0;

            index = Convert.ToInt32(strIndex) - 1;


            if (strDirection == "prev")
            {
                index--;
                if (index < 0)
                {
                    strError = "��ͷ";
                    goto ERROR1;
                }
            }
            else if (strDirection == "next")
            {
                index++;
            }
            else if (strDirection == "current")
            {
                // index����
            }
            else
            {
                strError = "����ʶ���strDirection����ֵ '" + strDirection + "'";
                goto ERROR1;
            }

            return LoadRecord(searchform, index);
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // װ��XML��¼�����ݽ������λ��
        public int LoadRecord(ISearchForm searchform,
            int index)
        {
            string strError = "";
            string strRecordXml = "";

            this.LinkedSearchForm = searchform;
            this.SavePath = "";

            DigitalPlatform.Z3950.Record record = null;
            Encoding currentEncoding = null;

            this.CurrentRecord = null;


            byte[] baTimestamp = null;
            string strSavePath = "";
            string strOutStyle = "";
            LoginInfo logininfo = null;
            long lVersion = 0;
            string strXmlFragment = "";

            int nRet = searchform.GetOneRecord(
                "xml",
                index,  // ������ֹ
                "index:" + index.ToString(),
                "hilight_browse_line", // true,
                out strSavePath,
                out strRecordXml,
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

            this.LoginInfo = logininfo;

            this.CurrentTimestamp = baTimestamp;
            this.SavePath = strSavePath;
            this.CurrentEncoding = currentEncoding;

            // dp2libraryЭ��
            if (searchform.CurrentProtocol == "dp2library")
            {
                dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷��������ݴ���";
                    goto ERROR1;
                }

                string strProtocol = "";
                string strPath = "";
                nRet = Global.ParsePath(strSavePath,
                    out strProtocol,
                    out strPath,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                string strServerName = "";
                string strLocalPath = "";
                // ������¼·����
                // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                dp2SearchForm.ParseRecPath(strPath,
                    out strServerName,
                    out strLocalPath);

                string strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);

                // ���cfgs\dcdef
                string strCfgFileName = "dcdef";

                string strCfgPath = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;

                // ����ǰ�Ĳ�ͬ�����б�Ҫ��������
                if (this.DcCfgFilename != strCfgPath)
                {
                    string strCode = "";
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

                    nRet = this.DcEditor.LoadCfgCode(strCode,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    this.DcCfgFilename = strCfgPath;
                }

                // ����װ�������Դ
                {
                    EnableStateCollection save = this.MainForm.DisableToolButtons();
                    try
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
                    }
                    finally
                    {
                        save.RestoreAll();
                    }
                }
            }

            /*
            // �滻����0x0a
            strMARC = strMARC.Replace("\r", "");
            strMARC = strMARC.Replace("\n", "\r\n");
             * */

            // TODO: �ٴ�װ���ʱ��������
            // װ��DC�༭��
            this.DcEditor.Xml = strRecordXml;


            /*
            // װ��XMLֻ��Web�ؼ�
            {
                string strTempFileName = MainForm.DataDir + "\\xml.xml";

                // SUTRS
                if (record != null)
                {
                    if (record.m_strSyntaxOID == "1.2.840.10003.5.101")
                        strTempFileName = MainForm.DataDir + "\\xml.txt";
                }

                Stream stream = File.Create(strTempFileName);

                // д��xml����
                byte[] buffer = Encoding.UTF8.GetBytes(strRecordXml);

                stream.Write(buffer, 0, buffer.Length);

                stream.Close();

                this.webBrowser_xml.Navigate(strTempFileName);
            }
             * */


            this.CurrentRecord = record;

            /*
            if (this.CurrentRecord != null)
            {
                // װ������Ʊ༭��
                this.binaryEditor_originData.SetData(
                    this.CurrentRecord.m_baRecord);

                // װ��ԭʼ�ı�
                nRet = this.SetOriginText(this.CurrentRecord.m_baRecord,
                    this.CurrentEncoding,
                    out strError);
                if (nRet == -1)
                {
                    this.textBox_originData.Text = strError;
                }

                // ���ݿ���
                this.textBox_originDatabaseName.Text = this.CurrentRecord.m_strDBName;

                // record syntax OID
                this.textBox_originMarcSyntaxOID.Text = this.CurrentRecord.m_strSyntaxOID;
            }*/


            // ��������·��
            string strFullPath = searchform.CurrentProtocol + ":"
                + searchform.CurrentResultsetPath
                + "/" + (index + 1).ToString();

            this.textBox_tempRecPath.Text = strFullPath;


            this.BiblioChanged = false;

            this.DcEditor.Focus();
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // װ��XML��¼�����ݼ�¼·��
        // parameters:
        //      strPath ·�������� "ͼ���ܿ�/1@���ط�����"
        public int LoadDp2Record(dp2SearchForm dp2_searchform,
            string strPath,
            string strDirection,
            bool bLoadResObject)
        {
            string strError = "";
            string strRecordXml = "";

            if (dp2_searchform == null)
            {
                strError = "dp2_searchform��������Ϊ��";
                goto ERROR1;
            }

            if (dp2_searchform.CurrentProtocol != "dp2library")
            {
                strError = "���ṩ�ļ���������dp2libraryЭ��";
                goto ERROR1;
            }

            DigitalPlatform.Z3950.Record record = null;
            Encoding currentEncoding = null;

            this.CurrentRecord = null;

            byte[] baTimestamp = null;
            string strOutStyle = "";

            string strSavePath = "";

            long lVersion = 0;
            LoginInfo logininfo = null;
            string strXmlFragment = "";

            int nRet = dp2_searchform.GetOneRecord(
                // true,
                "xml",
                // strPath,
                // strDirection,
                0,   // test
                "path:" + strPath + ",direction:" + strDirection,
                "",
                out strSavePath,
                out strRecordXml,
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

            this.CurrentTimestamp = baTimestamp;
            // this.SavePath = dp2_searchform.CurrentProtocol + ":" + strOutputPath;
            this.SavePath = strSavePath;
            this.CurrentEncoding = currentEncoding;

            string strServerName = "";
            string strLocalPath = "";

            strPath = strSavePath;

            // ������¼·����
            // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
            dp2SearchForm.ParseRecPath(strPath,
                out strServerName,
                out strLocalPath);

            string strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);

            // ���cfgs\dcdef
            string strCfgFileName = "dcdef";

            string strCfgPath = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;

            // ����ǰ�Ĳ�ͬ�����б�Ҫ��������
            if (this.DcCfgFilename != strCfgPath)
            {
                string strCode = "";
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

                nRet = this.DcEditor.LoadCfgCode(strCode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.DcCfgFilename = strCfgPath;
            }

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
            }




            // TODO: �ٴ�װ���ʱ��������
            // װ��DC�༭��
            this.DcEditor.Xml = strRecordXml;


            this.CurrentRecord = record;

            this.BiblioChanged = false;

            this.DcEditor.Focus();
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // װ��XMLֻ��Web�ؼ�
        void SetXmlCodeDisplay(string strRecordXml)
        {
            string strTempFileName = MainForm.DataDir + "\\xml.xml";

            Stream stream = File.Create(strTempFileName);

            // д��xml����
            byte[] buffer = Encoding.UTF8.GetBytes(strRecordXml);

            stream.Write(buffer, 0, buffer.Length);

            stream.Close();

            this.webBrowser_xml.Navigate(strTempFileName);
        }

        /*
        int SetOriginText(byte[] baOrigin,
    Encoding encoding,
    out string strError)
        {
            strError = "";

            if (encoding == null)
            {
                int nRet = this.MainForm.GetEncoding(this.comboBox_originDataEncoding.Text,
                    out encoding,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            else
            {
                this.comboBox_originDataEncoding.Text = GetEncodingForm.GetEncodingName(this.CurrentEncoding);

            }

            this.textBox_originData.Text = encoding.GetString(baOrigin);

            return 0;
        }*/

        private void toolStripButton_dispXmlText_Click(object sender, EventArgs e)
        {
            string strXmlBody = "";

            try
            {
                strXmlBody = this.DcEditor.Xml;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return;
            }

            XmlViewerForm dlg = new XmlViewerForm();
            GuiUtil.SetControlFont(dlg, this.Font);

            dlg.Text = "XML����ĵ�ǰDC��¼";
            dlg.MainForm = this.MainForm;
            dlg.XmlString = strXmlBody;
            // dlg.StartPosition = FormStartPosition.CenterScreen;

            this.MainForm.AppInfo.LinkFormState(dlg,
                "dc_xml_dialog_state");

            dlg.ShowDialog(this);

            this.MainForm.AppInfo.UnlinkFormState(dlg);



            // dlg.ShowDialog();
        }

        void EnableControls(bool bEnable)
        {
            this.textBox_tempRecPath.Enabled = bEnable;
            this.DcEditor.Enabled = bEnable;
        }

        public int MergeResourceIds(ref string strXml,
            out string strError)
        {
            strError = "";

            if (this.binaryResControl1 == null)
                return 0;

            XmlDocument domDc = new XmlDocument();
            try
            {
                domDc.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XML����װ��DOMʱ����: " + ex.Message;
                goto ERROR1;
            }

            // ��ɾ���Ѿ��е�<dprms:file>Ԫ��
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);

            XmlNodeList nodes = domDc.DocumentElement.SelectNodes("//dprms:file", nsmgr);
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                if (node.ParentNode != null)
                    node.ParentNode.RemoveChild(node);
            }

#if NO
            // Ȼ�����ӱ��ε�id��
            List<string> ids = this.binaryResControl1.GetIds();

            for (int i = 0; i < ids.Count; i++)
            {
                string strID = ids[i];
                if (String.IsNullOrEmpty(strID) == true)
                    continue;

                XmlNode node = domDc.CreateElement("dprms",
                    "file",
                    DpNs.dprms);
                domDc.DocumentElement.AppendChild(node);
                DomUtil.SetAttr(node, "id", strID);
            }
#endif
            int nRet = this.binaryResControl1.AddFileFragments(ref domDc, out strError);
            if (nRet == -1)
                return -1;

            strXml = domDc.OuterXml;
            return 1;
        ERROR1:
            return -1;

        }

        // ����
        // parameters:
        //      strSender   �����������Դ "toolbar" "ctrl_d"
        public int SearchDup(string strSender)
        {
            string strError = "";
            int nRet = 0;

            Debug.Assert(strSender == "toolbar" || strSender == "ctrl_d", "");

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

                if (strProtocol != "dp2library")
                    strStartPath = "";  // ��ʹ����ѡ�����·��
            }

            if (String.IsNullOrEmpty(strStartPath) == true)
            {
                /*
                strError = "��ǰ��¼·��Ϊ�գ��޷����в���";
                goto ERROR1;
                 * */
                dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                if (dp2_searchform == null)
                {
                    strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷����в���";
                    goto ERROR1;
                }

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

                    dlg.Text = "��ָ��һ��dp2library���ݿ⣬����Ϊģ��Ĳ������";
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

                strProtocol = "dp2library";
                strPath = strDefaultStartPath;
            }



            this.EnableControls(false);
            try
            {

                // dtlpЭ��ļ�¼����
                if (strProtocol.ToLower() == "dtlp")
                {
                    strError = "Ŀǰ�ݲ�֧��DTLPЭ��Ĳ��ز���";
                    goto ERROR1;
                }
                else if (strProtocol.ToLower() == "dp2library")
                {
                    dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                    if (dp2_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷����в���";
                        goto ERROR1;
                    }

                    // ��strPath����Ϊserver url��local path��������
                    string strServerName = "";
                    string strPurePath = "";
                    dp2SearchForm.ParseRecPath(strPath,
                        out strServerName,
                        out strPurePath);

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
                    nRet = dp2_searchform.GetDbSyntax(null, // this.stop, BUG!!!
                        strServerName,
                        strDbName,
                        out strSyntax,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (String.IsNullOrEmpty(strSyntax) == true)
                        strSyntax = "unimarc";

                    // �����Ŀ��¼��XML��ʽ
                    string strXml = "";

                    try
                    {
                        strXml = this.DcEditor.Xml;
                    }
                    catch (Exception ex)
                    {
                        strError = ex.Message;
                        goto ERROR1;
                    }


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
                    strError = "Ŀǰ�ݲ�֧��Z39.50Э��ı������";
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

        // �����¼
        public int SaveRecord()
        {
            string strError = "";
            int nRet = 0;

            string strLastSavePath = MainForm.LastSavePath;
            if (String.IsNullOrEmpty(strLastSavePath) == false)
            {
                string strOutputPath = "";
                nRet = MarcDetailForm.ChangePathToAppendStyle(strLastSavePath,
                    out strOutputPath,
                    out strError);
                if (nRet == -1)
                {
                    MainForm.LastSavePath = ""; // �����´μ������� 2011/3/4
                    goto ERROR1;
                }
                strLastSavePath = strOutputPath;
            }

            SaveRecordDlg dlg = new SaveRecordDlg();
            GuiUtil.SetControlFont(dlg, this.Font);

            dlg.MainForm = this.MainForm;
            dlg.GetDtlpSearchParam += new GetDtlpSearchParamEventHandle(dlg_GetDtlpSearchParam);
            dlg.GetDp2SearchParam += new GetDp2SearchParamEventHandle(dlg_GetDp2SearchParam);
            // dlg.RecPath = this.SavePath == "" ? MainForm.LastSavePath : this.SavePath;
            dlg.RecPath = this.SavePath == "" ? strLastSavePath : this.SavePath;
            dlg.StartPosition = FormStartPosition.CenterScreen;

            dlg.ActiveProtocol = "dp2library";

            this.MainForm.AppInfo.LinkFormState(dlg, "SaveRecordDlg_state");
            dlg.UiState = this.MainForm.AppInfo.GetString("DcForm", "SaveRecordDlg_uiState", "");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.SetString("DcForm", "SaveRecordDlg_uiState", dlg.UiState);
            if (dlg.DialogResult != DialogResult.OK)
                return 0;

            MainForm.LastSavePath = dlg.RecPath;


            string strProtocol = "";
            string strPath = "";
            nRet = Global.ParsePath(dlg.RecPath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.EnableControls(false);
            try
            {

                // dp2libraryЭ��ļ�¼����
                if (strProtocol.ToLower() == "dp2library")
                {
                    dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                    if (dp2_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷������¼";
                        goto ERROR1;
                    }

                    string strXml = "";

                    try
                    {
                        strXml = this.DcEditor.Xml;
                    }
                    catch (Exception ex)
                    {
                        strError = ex.Message;
                        goto ERROR1;
                    }

                    // �ϳ�<dprms:file>Ԫ��
                    nRet = MergeResourceIds(ref strXml,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    /*
                    if (this.binaryResControl1 != null)
                    {
                        XmlDocument domDc = new XmlDocument();
                        try
                        {
                            domDc.LoadXml(strXml);
                        }
                        catch (Exception ex)
                        {
                            strError = "XML����װ��DOMʱ����: " + ex.Message;
                            goto ERROR1;
                        }

                        // ��ɾ���Ѿ��е�<dprms:file>Ԫ��

                        List<string> ids = this.binaryResControl1.GetIds();

                        for (int i = 0; i < ids.Count; i++)
                        {
                            string strID = ids[i];
                            if (String.IsNullOrEmpty(strID) == true)
                                continue;

                            XmlNode node = domDc.CreateElement("dprms",
                                "file",
                                DpNs.dprms);
                            domDc.DocumentElement.AppendChild(node);
                            DomUtil.SetAttr(node, "id", strID);
                        }

                        strXml = domDc.OuterXml;
                    }
                     * */

                    string strOutputPath = "";
                    byte[] baOutputTimestamp = null;
                    nRet = dp2_searchform.SaveXmlRecord(
                        strPath,
                        strXml,
                        this.CurrentTimestamp,
                        out strOutputPath,
                        out baOutputTimestamp,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    this.SavePath = strProtocol + ":" + strOutputPath;
                    this.CurrentTimestamp = baOutputTimestamp;

                    /*
                    // �����·��Ϊ��
                    this.textBox_tempRecPath.Text = "";
                     * */


                    // �����Դ�ؼ���û������path������Ϊ׷���ͣ�����
                    // TODO: �ǲ��Ǹɴ඼��һ�Σ�
                    if (String.IsNullOrEmpty(this.binaryResControl1.BiblioRecPath) == true
                        || dp2SearchForm.IsAppendRecPath(this.binaryResControl1.BiblioRecPath) == true)
                    {
                        string strServerName = "";
                        string strLocalPath = "";
                        // ������¼·����
                        // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                        dp2SearchForm.ParseRecPath(strOutputPath,
                            out strServerName,
                            out strLocalPath);
                        this.binaryResControl1.BiblioRecPath = strLocalPath;
                    }

                    // �ύ���󱣴�����
                    // return:
                    //		-1	error
                    //		>=0 ʵ�����ص���Դ������
                    nRet = this.binaryResControl1.Save(out strError);
                    if (nRet == -1)
                        MessageBox.Show(this, strError);
                    else
                    {
                        MessageBox.Show(this, "����ɹ�");
                    }

                    if (nRet >= 1)
                    {
                        /*
                        bObjectSaved = true;
                        if (strText != "")
                            strText += " ";
                        strText += "������Ϣ";
                         * */

                        // ˢ����Ŀ��¼��ʱ���
                        // LoadRecord("current");
                        LoadDp2Record(dp2_searchform,
                            strOutputPath,
                            "current",
                            false);
                    }

                    this.BiblioChanged = false;
                    return 0;
                }
                else if (strProtocol.ToLower() == "dtlp")
                {
                    strError = "ĿǰDC���ݲ�֧��DTLPЭ��ı������";
                    goto ERROR1;
                }
                else if (strProtocol.ToLower() == "z3950")
                {
                    strError = "ĿǰDC���ݲ�֧��Z39.50Э��ı������";
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
                    dtlp_searchform = new DtlpSearchForm();
                    dtlp_searchform.MainForm = this.MainForm;
                    dtlp_searchform.MdiParent = this.MainForm;
                    dtlp_searchform.WindowState = FormWindowState.Minimized;
                    dtlp_searchform.Show();
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

        private void DcForm_Activated(object sender, EventArgs e)
        {
            if (stop != null)
                MainForm.stopManager.Active(this.stop);

            MainForm.SetMenuItemState();

            // �˵�
            MainForm.MenuItem_saveOriginRecordToIso2709.Enabled = false;
            MainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = false;
            MainForm.MenuItem_font.Enabled = true;
            MainForm.MenuItem_saveToTemplate.Enabled = true;

            // ��������ť
            MainForm.toolButton_search.Enabled = false;

            MainForm.toolButton_prev.Enabled = true;
            MainForm.toolButton_next.Enabled = true;
            /*
            if (this.LinkedSearchForm != null)
            {
                MainForm.toolButton_prev.Enabled = true;
                MainForm.toolButton_next.Enabled = true;
            }
            else
            {
                MainForm.toolButton_prev.Enabled = false;
                MainForm.toolButton_next.Enabled = false;
            }*/

            MainForm.toolButton_nextBatch.Enabled = false;

            MainForm.toolButton_getAllRecords.Enabled = false;
            MainForm.toolButton_saveTo.Enabled = false;
            MainForm.toolButton_save.Enabled = true;
            MainForm.toolButton_delete.Enabled = true;

            MainForm.toolButton_loadTemplate.Enabled = true;

            MainForm.toolButton_dup.Enabled = true;
            MainForm.toolButton_verify.Enabled = true;

            MainForm.toolButton_refresh.Enabled = true;
            MainForm.toolButton_loadFullRecord.Enabled = false;
        }


        // ɾ����¼
        public int DeleteRecord()
        {
            string strError = "";

            if (String.IsNullOrEmpty(this.SavePath) == true)
            {
                strError = "ȱ������·�����޷�����ɾ��";
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

            strText += "ȷʵҪɾ����Ŀ��¼ " + strPath + " ";

            int nObjectCount = this.binaryResControl1.ObjectCount;
            if (nObjectCount != 0)
                strText += "�ʹ����� " + nObjectCount.ToString() + " ������";

            strText += " ?";

            // ����ɾ��
            DialogResult result = MessageBox.Show(this,
                strText,
                "DcForm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return 0;
            }



            this.EnableControls(false);
            try
            {

                // dp2libraryЭ��ļ�¼����
                if (strProtocol.ToLower() == "dp2library")
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

                    this.binaryResControl1.Clear(); // 2008/3/18 �����������ݣ����Ᵽ���ȥ��ʱ���γɿն�����Դ

                    this.ObjectChanged = false;
                    this.BiblioChanged = false;

                    MessageBox.Show(this, "ɾ���ɹ�");
                    return 1;
                }
                else if (strProtocol.ToLower() == "z3950")
                {
                    strError = "Ŀǰ�ݲ�֧��Z39.50Э���ɾ������";
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

        // װ��ģ��
        public int LoadTemplate()
        {
            string strError = "";
            int nRet = 0;

            if (this.BiblioChanged == true
                || this.ObjectChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�� " + GetCurrentChangedPartName() + " ���޸ĺ���δ���档����ʱװ�������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪװ��������? ",
                    "DcForm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return 0;
            }


            dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

            if (dp2_searchform == null)
            {
                strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷�װ��ģ��";
                goto ERROR1;
            }

            string strProtocol = "";
            string strPath = "";

            string strServerName = "";
            string strLocalPath = "";

            string strBiblioDbName = "";

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

                if (strProtocol == "dp2library")
                {
                    // ������¼·����
                    // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                    dp2SearchForm.ParseRecPath(strPath,
                        out strServerName,
                        out strLocalPath);

                    strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);
                }
                else
                {
                    strProtocol = "dp2library";
                    strPath = "";
                }
            }
            else
            {
                strProtocol = "dp2library";
            }

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

            // ��Ŀ��·�����Ϊ��������
            nRet = dlg.Path.IndexOf("/");
            if (nRet == -1)
            {
                Debug.Assert(false, "");
                strServerName = dlg.Path;
                strBiblioDbName = "";
                strError = "��ѡ��Ŀ��(���ݿ�)·�� '" + dlg.Path + "' ��ʽ����ȷ";
                goto ERROR1;
            }
            else
            {
                strServerName = dlg.Path.Substring(0, nRet);
                strBiblioDbName = dlg.Path.Substring(nRet + 1);

                // �����ѡ���ݿ��syntax������Ϊdc

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
                    strError = "��ȡ��Ŀ�� '" +strBiblioDbName+ "�����ݸ�ʽʱ��������: " + strError;
                    goto ERROR1;
                }

                if (strSyntax != "dc")
                {
                    strError = "��ѡ��Ŀ�� '" + strBiblioDbName + "' ����DC��ʽ�����ݿ�";
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
            nRet = tempdlg.Initial(false,
                strCode, 
                out strError);
            if (nRet == -1)
            {
                strError = "װ�������ļ� '" + strCfgFilePath + "' ��������: " + strError;
                goto ERROR1;
            }

            tempdlg.ap = this.MainForm.AppInfo;
            tempdlg.ApCfgTitle = "dcform_selecttemplatedlg";
            tempdlg.ShowDialog(this);

            if (tempdlg.DialogResult != DialogResult.OK)
                return 0;

            // ���cfgs\dcdef
            string strCfgFileName = "dcdef";

            string strCfgPath = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;

            // ����ǰ�Ĳ�ͬ�����б�Ҫ��������
            if (this.DcCfgFilename != strCfgPath)
            {
                strCode = "";
                // byte[] baCfgOutputTimestamp = null;
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

                nRet = this.DcEditor.LoadCfgCode(strCode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.DcCfgFilename = strCfgPath;
            }

            // ����װ�������Դ
            {
                this.binaryResControl1.Clear();
                this.binaryResControl1.Channel = dp2_searchform.GetChannel(dp2_searchform.GetServerUrl(strServerName));
                this.binaryResControl1.BiblioRecPath = strBiblioDbName + "/?";
            }



            this.DcEditor.Xml = tempdlg.SelectedRecordXml;
            this.CurrentTimestamp = null;   // baCfgOutputTimestamp;

            this.SavePath = strProtocol + ":" + strBiblioDbName + "/?" + "@" + strServerName;

            this.ObjectChanged = false;
            this.BiblioChanged = false;

            this.LinkedSearchForm = null;  // �жϺ�ԭ�������ļ���������ϵ��������û��ǰ��ҳ��
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }


        // ���浽ģ��
        public int SaveToTemplate()
        {
            string strError = "";
            int nRet = 0;

            dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

            if (dp2_searchform == null)
            {
                strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷����浱ǰ���ݵ�ģ��";
                goto ERROR1;
            }

            string strProtocol = "";
            string strPath = "";

            string strServerName = "";
            string strLocalPath = "";

            string strBiblioDbName = "";

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

                if (strProtocol == "dp2library")
                {
                    // ������¼·����
                    // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                    dp2SearchForm.ParseRecPath(strPath,
                        out strServerName,
                        out strLocalPath);

                    strBiblioDbName = dp2SearchForm.GetDbName(strLocalPath);
                }
                else
                {
                    strProtocol = "dp2library";
                    strPath = "";
                }
            }


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

            nRet = dlg.Path.IndexOf("/");
            if (nRet == -1)
                strServerName = dlg.Path;
            else
            {
                strServerName = dlg.Path.Substring(0, nRet);
                strBiblioDbName = dlg.Path.Substring(nRet + 1);

                // �����ѡ���ݿ��syntax������Ϊdc

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

                if (strSyntax != "dc")
                {
                    strError = "��ѡ��Ŀ�� '" + strBiblioDbName + "' ����DC��ʽ�����ݿ�";
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
			tempdlg.ApCfgTitle = "dcform_selecttemplatedlg";
			tempdlg.ShowDialog(this);

            if (tempdlg.DialogResult != DialogResult.OK)
                return 0;

			// �޸������ļ�����
			if (tempdlg.textBox_name.Text != "")
			{
                string strXml = "";

                try {
                strXml = this.DcEditor.Xml;
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    goto ERROR1;
                }

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

            MessageBox.Show(this, "�޸�ģ��ɹ�");
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        private void tabControl_main_Selected(object sender, TabControlEventArgs e)
        {
            if (this.tabControl_main.SelectedTab == this.tabPage_xmlDisplay)
            {
                string strXml = "";
                try
                {
                    strXml = this.DcEditor.Xml;
                }
                catch (Exception ex)
                {
                    strXml = "<error>" + HttpUtility.HtmlEncode(ex.Message) + "</error>";
                }

                SetXmlCodeDisplay(strXml);
            }
        }

        // �ӹ�Ctrl+���ּ�
        protected override bool ProcessDialogKey(
            Keys keyData)
        {

            // Ctrl + A �Զ�¼�빦��
            if (keyData == (Keys.A | Keys.Control))
            {
                // MessageBox.Show(this, "CTRL+A");
                this.AutoGenerate();
                return true;
            }

            // Ctrl + D ����
            if (keyData == (Keys.D | Keys.Control))
            {
                this.SearchDup("ctrl_d");
                return true;
            }

            return false;
        }

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

                string strCfgFileName = "dp2catalog_dc_autogen.cs";

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

                strCfgFileName = "dp2catalog_dc_autogen.cs.ref";

                strCfgPath = strBiblioDbName + "/cfgs/" + strCfgFileName + "@" + strServerName;

                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = dp2_searchform.GetCfgFile(strCfgPath,
                    out strRef,
                    out baCfgOutputTimestamp,
                    out strError);
                if (nRet == -1 || nRet == 0)
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
									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.commoncontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.script.dll",

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
                "dp2Catalog.DcDetailHost");
            if (entryClassType == null)
            {
                strError = "dp2Catalog.DcDetailHost������û���ҵ�";
                return -1;
            }

            // newһ��DcDetailHost��������
            DcDetailHost hostObj = (DcDetailHost)entryClassType.InvokeMember(null,
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

        // Ϊ�˼�����ǰ�� API
        public int HanziTextToPinyin(
    bool bLocal,
    string strText,
    PinyinStyle style,
    out string strPinyin,
    out string strError)
        {
            return this.MainForm.HanziTextToPinyin(
                this,
                bLocal,
                strText,
                style,
                "",
                out strPinyin,
                out strError);
        }

#if NO
        // ���ݺ��ֵõ�ƴ���ַ���
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

        void LoadFontToDcEditor()
        {
            string strFontString = MainForm.AppInfo.GetString("dceditor",
                "fontstring",
                "");  // "Arial Unicode MS, 9pt"

            if (String.IsNullOrEmpty(strFontString) == false)
            {
                // Create the FontConverter.
                System.ComponentModel.TypeConverter converter =
                    System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));

                this.DcEditor.Font = (Font)converter.ConvertFromString(strFontString);
            }

            string strFontColor = MainForm.AppInfo.GetString("dceditor",
                "fontcolor",
                "");

            if (String.IsNullOrEmpty(strFontColor) == false)
            {
                // Create the ColorConverter.
                System.ComponentModel.TypeConverter converter =
                    System.ComponentModel.TypeDescriptor.GetConverter(typeof(Color));

                this.DcEditor.ForeColor = (Color)converter.ConvertFromString(strFontColor);
            }
        }

        void SaveFontForDcEditor()
        {
            {
                // Create the FontConverter.
                System.ComponentModel.TypeConverter converter =
                    System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));

                string strFontString = converter.ConvertToString(this.DcEditor.Font);

                MainForm.AppInfo.SetString("dceditor",
                    "fontstring",
                    strFontString);
            }

            {
                // Create the ColorConverter.
                System.ComponentModel.TypeConverter converter =
                    System.ComponentModel.TypeDescriptor.GetConverter(typeof(Color));

                string strFontColor = converter.ConvertToString(this.DcEditor.ForeColor);

                MainForm.AppInfo.SetString("dceditor",
                    "fontcolor",
                    strFontColor);
            }

        }

        // ��������
        public void SetFont()
        {
            FontDialog dlg = new FontDialog();

            dlg.ShowColor = true;
            dlg.Color = this.DcEditor.ForeColor;
            dlg.Font = this.DcEditor.Font;
            dlg.ShowApply = true;
            dlg.ShowHelp = true;
            dlg.AllowVerticalFonts = false;

            dlg.Apply -= new EventHandler(dlgFont_Apply);
            dlg.Apply += new EventHandler(dlgFont_Apply);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            this.DcEditor.DisableUpdate();

            this.DcEditor.Font = dlg.Font;
            this.DcEditor.ForeColor = dlg.Color;

            this.DcEditor.EnableUpdate();

            // ���浽�����ļ�
            SaveFontForDcEditor();
        }

        void dlgFont_Apply(object sender, EventArgs e)
        {
            FontDialog dlg = (FontDialog)sender;

            this.DcEditor.DisableUpdate();

            this.DcEditor.Font = dlg.Font;
            this.DcEditor.ForeColor = dlg.Color;

            this.DcEditor.EnableUpdate();

            // ���浽�����ļ�
            SaveFontForDcEditor();
        }

        // ����Ԫ��
        private void toolStripButton_newElement_Click(object sender, EventArgs e)
        {
            this.DcEditor.NewElement();
        }

        // ɾ��Ԫ��
        private void toolStripButton_deleteEelement_Click(object sender, EventArgs e)
        {
            this.DcEditor.DeleteSelectedElements();
        }

        private void DcEditor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.DcEditor.SelectedIndices.Count == 0)
                this.toolStripButton_deleteEelement.Enabled = false;
            else
                this.toolStripButton_deleteEelement.Enabled = true;
        }

#if NOOOOOOOOOOOO
        protected override void OnSizeChanged(EventArgs e)
        {
            // MessageBox.Show(this, "begin");
            this.DcEditor.DisableDrawCell();
            try
            {
                base.OnSizeChanged(e);
            }
            finally
            {
                this.DcEditor.EnableDrawCell();
                // MessageBox.Show(this, "end");
            }
        }
#endif

    }
}