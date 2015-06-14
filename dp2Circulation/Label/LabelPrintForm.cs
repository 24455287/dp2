using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;

using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.IO;
using System.Web;
using DigitalPlatform.Drawing;
using DigitalPlatform.Text;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// ��ǩ��ӡ��
    /// </summary>
    public partial class LabelPrintForm : ItemSearchFormBase    // MyForm
    {
        // bool m_bBiblioSummaryColumn = true; // �Ƿ�������б��� ������ĿժҪ��

        // bool m_bFirstColumnIsKey = false; // ��ǰlistview����еĵ�һ���Ƿ�ӦΪkey


        PrinterInfo m_printerInfo = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        public PrinterInfo PrinterInfo
        {
            get
            {
                return this.m_printerInfo;
            }
            set
            {
                this.m_printerInfo = value;
                SetTitle();
            }
        }

#if NO
        // ���ʹ�ù��ļ�¼·���ļ���
        string m_strUsedRecPathFilename = "";
#endif

        /// <summary>
        /// ��������Ĳ�������ļ���
        /// </summary>
        public string ExportBarcodeFilename = "";
        /// <summary>
        /// ��������ļ�¼·���ļ���
        /// </summary>
        public string ExportRecPathFilename = "";
        /// <summary>
        /// ����������ı��ļ���
        /// </summary>
        public string ExportTextFilename = "";

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;
#endif

        LabelParam label_param = new LabelParam();

        PrintLabelDocument document = null;

        string m_strPrintStyle = "";    // ��ӡ���

        /// <summary>
        /// ���캯��
        /// </summary>
        public LabelPrintForm()
        {
            InitializeComponent();

            _listviewRecords = this.listView_records;

            ListViewProperty prop = new ListViewProperty();
            this.listView_records.Tag = prop;
            // ��һ�����⣬��¼·��
            prop.SetSortStyle(0, ColumnSortStyle.RecPath);
            prop.GetColumnTitles -= new GetColumnTitlesEventHandler(prop_GetColumnTitles);
            prop.GetColumnTitles += new GetColumnTitlesEventHandler(prop_GetColumnTitles);

            prop.CompareColumn -= new CompareEventHandler(prop_CompareColumn);
            prop.CompareColumn += new CompareEventHandler(prop_CompareColumn);
        }

        void prop_CompareColumn(object sender, CompareEventArgs e)
        {
            if (e.Column.SortStyle.Name == "call_number")
            {
                // �Ƚ�������ȡ�ŵĴ�С
                // return:
                //      <0  s1 < s2
                //      ==0 s1 == s2
                //      >0  s1 > s2
                e.Result = StringUtil.CompareAccessNo(e.String1, e.String2, true);
            }
            else if (e.Column.SortStyle.Name == "parent_id")
            {
                // �Ҷ���Ƚ��ַ���
                // parameters:
                //      chFill  ����õ��ַ�
                e.Result = StringUtil.CompareRecPath(e.String1, e.String2);
            }
            else
                e.Result = string.Compare(e.String1, e.String2);
        }

#if NO
        void prop_GetColumnTitles(object sender, GetColumnTitlesEventArgs e)
        {
            e.ColumnTitles = this.MainForm.GetBrowseColumnProperties(e.DbName);
            e.ListViewProperty.SetSortStyle(2, ColumnSortStyle.LeftAlign);
        }
#endif
        void prop_GetColumnTitles(object sender, GetColumnTitlesEventArgs e)
        {
            if (e.DbName == "<blank>")
            {
                e.ColumnTitles = new ColumnPropertyCollection();
                e.ColumnTitles.Add("������");
                e.ColumnTitles.Add("����");
                // �����е�����
                e.ListViewProperty.SetSortStyle(2, ColumnSortStyle.RightAlign);
                return;
            }

            e.ColumnTitles = new ColumnPropertyCollection();
            ColumnPropertyCollection temp = this.MainForm.GetBrowseColumnProperties(e.DbName);
            if (temp != null)
            {
                if (m_bBiblioSummaryColumn == true)
                    e.ColumnTitles.Insert(0, "��ĿժҪ");
                e.ColumnTitles.AddRange(temp);  // Ҫ���ƣ���Ҫֱ��ʹ�ã���Ϊ������ܻ��޸ġ���Ӱ�쵽ԭ��
            }

            if (this.m_bFirstColumnIsKey == true)
                e.ColumnTitles.Insert(0, "���еļ�����");

            // e.ListViewProperty.SetSortStyle(2, ColumnSortStyle.LeftAlign);   // Ӧ�ø��� typeΪitem_barcode ����������ʽ
        }

        void ClearListViewPropertyCache()
        {
            ListViewProperty prop = (ListViewProperty)this.listView_records.Tag;
            prop.ClearCache();
        }

        private void LabelPrintForm_Load(object sender, EventArgs e)
        {
#if NO
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
#endif
#if NO
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            if (string.IsNullOrEmpty(this.textBox_labelFile_labelFilename.Text) == true)
            {
                this.textBox_labelFile_labelFilename.Text = this.MainForm.AppInfo.GetString(
                    "label_print_form",
                    "label_file_name",
                    "");
            }

            if (string.IsNullOrEmpty(this.textBox_labelDefFilename.Text) == true)
            {
                this.textBox_labelDefFilename.Text = this.MainForm.AppInfo.GetString(
                    "label_print_form",
                    "label_def_file_name",
                    "");
            }

            if (this.m_bTestingGridSetted == false)
            {
                this.checkBox_testingGrid.Checked = this.MainForm.AppInfo.GetBoolean(
                    "label_print_form",
                    "print_testing_grid",
                    false);
            }

            string strWidths = this.MainForm.AppInfo.GetString(
                "label_print_form",
                "record_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_records,
                    strWidths,
                    true);
            }

            // ��ǰ���property page
            string strActivePage = this.MainForm.AppInfo.GetString(
                "label_print_form",
                "active_page",
                "");

            if (String.IsNullOrEmpty(strActivePage) == false)
            {
                if (strActivePage == "itemrecords")
                    this.tabControl_main.SelectedTab = this.tabPage_itemRecords;
                else if (strActivePage == "labelfile")
                    this.tabControl_main.SelectedTab = this.tabPage_labelFile;
            }

            if (this.PrinterInfo == null)
            {
                this.PrinterInfo = this.MainForm.PreparePrinterInfo("ȱʡ��ǩ");
            }
            SetTitle();
        }

        private void LabelPrintForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void LabelPrintForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            this.MainForm.AppInfo.SetString(
                "label_print_form",
                "label_file_name",
                this.textBox_labelFile_labelFilename.Text);

            this.MainForm.AppInfo.SetString(
                "label_print_form",
                "label_def_file_name",
                this.textBox_labelDefFilename.Text);

            this.MainForm.AppInfo.SetBoolean(
                "label_print_form",
                "print_testing_grid",
                this.checkBox_testingGrid.Checked);

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_records);
            this.MainForm.AppInfo.SetString(
                "label_print_form",
                "record_list_column_width",
                strWidths);

            // ��ǰ���property page
            string strActivePage = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_labelFile)
                strActivePage = "labelfile";
            else if (this.tabControl_main.SelectedTab == this.tabPage_itemRecords)
                strActivePage = "itemrecords";

            this.MainForm.AppInfo.SetString(
                "label_print_form",
                "active_page",
                strActivePage);

            if (this.PrinterInfo != null)
            {
                string strType = this.PrinterInfo.Type;
                if (string.IsNullOrEmpty(strType) == true)
                    strType = "ȱʡ��ǩ";

                this.MainForm.SavePrinterInfo(strType,
                    this.PrinterInfo);
            }
        }

        /// <summary>
        ///�������ô��ڱ���
        /// </summary>
        public void SetTitle()
        {
            if (this.PrinterInfo == null)
                this.Text = "��ǩ��ӡ";
            else
                this.Text = "��ӡ -- " + this.PrinterInfo.Type + " -- " + this.PrinterInfo.PaperName + " -- " + this.PrinterInfo.PrinterName + (this.PrinterInfo.Landscape == true ? " --- ����" : "");
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

        private void button_labelFile_findLabelFilename_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵ı�ǩ�ļ���";
            dlg.FileName = this.textBox_labelFile_labelFilename.Text;
            dlg.Filter = "��ǩ�ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_labelFile_labelFilename.Text = dlg.FileName;
        }

        private void button_labelFile_findLabelDefFilename_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵ı�ǩ�����ļ���";
            dlg.FileName = this.textBox_labelDefFilename.Text;
            dlg.Filter = "��ǩ�����ļ� (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_labelDefFilename.Text = dlg.FileName;
        }

        private void textBox_labelFile_labelFilename_TextChanged(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.textBox_labelFile_labelFilename.Text) == true)
            {
                this.textBox_labelFile_content.Text = "";
                return;
            }

            string strError = "";
            string strContent = "";
            // ���Զ�ʶ���ļ����ݵı��뷽ʽ�Ķ����ı��ļ�����ģ��
            // return:
            //      -1  ����
            //      0   �ļ�������
            //      1   �ļ�����
            int nRet = Global.ReadTextFileContent(this.textBox_labelFile_labelFilename.Text,
                out strContent,
                out strError);
            if (nRet == 1)
                this.textBox_labelFile_content.Text = strContent;
            else
                this.textBox_labelFile_content.Text = "";
        }

        private void button_printPreview_Click(object sender, EventArgs e)
        {
            if (this.tabControl_main.SelectedTab == this.tabPage_labelFile)
                PrintPreviewFromLabelFile(Control.ModifierKeys == Keys.Control ? true : false);
            else if (this.tabControl_main.SelectedTab == this.tabPage_itemRecords)
                PrintPreviewFromItemRecords(Control.ModifierKeys == Keys.Control ? true : false);
        }

        int GetBiblioInfo(string strBiblioRecPath,
    string strBiblioType,
    out string strBiblio,
    out string strError)
        {
            strError = "";
            strBiblio = "";

            /*
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ִ�нű� ...");
            stop.BeginLoop();

            try
            {*/

            string strBiblioXml = "";   // ��������ṩ��XML��¼
            long lRet = this.Channel.GetBiblioInfo(
                null,   // this.stop,
                strBiblioRecPath,
                strBiblioXml,
                strBiblioType,
                out strBiblio,
                out strError);
            return (int)lRet;
            /*
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }*/
        }

        static void WriteErrorText(StreamWriter sw_error, string strText)
        {
            sw_error.WriteLine(strText);
        }

        // ���ݲ��¼·��������ǩ�ı��ļ�
        int BuildLabelFile(
            string strOutputFilename,
            string strOutputErrorFilename,
            out string strError)
        {
            strError = "";

            int nLabelCount = 0;

            int nErrorCount = 0;    // ��������Ĵ���������������������ֵΪ 0����ʾû������κδ�����Ϣ�����ļ�������Ϊ UTF-8 �� Preamable �� 3 byte ���ȡ�������Ҫ��������������ж�

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(strOutputFilename,
                     false,	// append
                     Encoding.UTF8);
            }
            catch (Exception ex)
            {
                strError = "������ǩ�ļ� " + strOutputFilename + " ʱ����: " + ex.Message;
                return -1;
            }

            StreamWriter sw_error = null;

            if (String.IsNullOrEmpty(strOutputErrorFilename) == false)
            {
                try
                {
                    sw_error = new StreamWriter(strOutputErrorFilename,
                         false,	// append
                         Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    strError = "����������Ϣ�ļ� " + strOutputErrorFilename + " ʱ����: " + ex.Message;
                    if (sw != null)
                        sw.Close();
                    return -1;
                }
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ȡ���¼�ʹ�����ǩ�ļ� ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                string strAccessNoSource = this.AccessNoSource;

#if NO
                bool bHideMessageBox = false;
                DialogResult result = System.Windows.Forms.DialogResult.No;
#endif

                stop.SetProgressRange(0, this.listView_records.Items.Count);

                for (int i = 0; i < this.listView_records.Items.Count; i++)
                {
                    ListViewItem item = this.listView_records.Items[i];
                    string strRecPath = item.Text;
                    if (String.IsNullOrEmpty(strRecPath) == true)
                        continue;

                    string strAccessPoint = "@path:" + strRecPath;

                    string strOutputRecPath = "";
                    string strResult = "";
                    string strBiblio = "";
                    string strBiblioRecPath = "";
                    byte[] baTimestamp = null;

                    // Result.Value -1���� 0û���ҵ� 1�ҵ� >1���ж���1��
                    long lRet = Channel.GetItemInfo(
                        stop,
                        strAccessPoint,
                        "xml",   // strResultType
                        out strResult,
                        out strOutputRecPath,
                        out baTimestamp,
                        "recpath", // strBiblioType
                        out strBiblio,
                        out strBiblioRecPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "��ò��¼ {" + strRecPath + "} ʱ��������: " + strError;
                        WriteErrorText(sw_error, strError);
                        nErrorCount++;
                        continue;
                    }

                    if (lRet == 0)
                    {
                        strError = "{" + strRecPath + "} ��XML����û���ҵ���";
                        WriteErrorText(sw_error, strError);
                        nErrorCount++;
                        continue;
                    }

                    if (lRet > 1)
                    {
                        strError = "{" + strRecPath + "} ��Ӧ���ݼ�¼����һ��(Ϊ "+lRet.ToString()+" ��)��";
                        WriteErrorText(sw_error, strError);
                        nErrorCount++;
                        continue;
                    }

                    string strXml = "";

                    strXml = strResult;

                    // �����Ƿ���ϣ��ͳ�Ƶķ�Χ��
                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "���¼ {"+strRecPath+"} XMLװ��DOM��������: " + ex.Message;
                        WriteErrorText(sw_error, strError);
                        nErrorCount++;
                        continue;
                    }

                    string strAccessNo = DomUtil.GetElementText(dom.DocumentElement,
                        "accessNo");
                    if (string.IsNullOrEmpty(strAccessNo) == false)
                        strAccessNo = StringUtil.GetPlainTextCallNumber(strAccessNo);

                    if (string.IsNullOrEmpty(strAccessNo) == true
    && strAccessNoSource == "�Ӳ��¼")
                    {
                        strError = "���¼ {" + strRecPath + "} ��û����ȡ���ֶ�����";
                        WriteErrorText(sw_error, strError);
                        nErrorCount++;
                        continue;
                    }

                    if (strAccessNoSource == "����Ŀ��¼"
                        || (strAccessNoSource == "˳�δӲ��¼����Ŀ��¼" && String.IsNullOrEmpty(strAccessNo) == true)
                        )
                    {
                        // ������ȡ��ȡ��
                        // TODO: ������ϵͳ���������ü��ִ���ʽ
                        // 1) �����Ӳ��¼�л��
                        // 2) ��������Ŀ��¼�л��
                        // 3) �ȴӲ��¼�л�ã����û���ٴ���Ŀ��¼�л��
                        // ��ô����ͽ����������״γ������������ʱ����ʾһ�¼��ɡ����ߴ������Ժ����ܽ�һ�³����� MessageBox ��

#if NO
                        if (bHideMessageBox == false)
                        {
                            // TODO: ��ť���ֽϳ���ʱ��Ӧ�����Զ���Ӧ
                            result = MessageDialog.Show(this,
        "���¼ " + strRecPath + " ����ȡ���ֶ�����Ϊ�ա������Ƿ�Ҫ�������ļ�¼����̽����Ŀ��¼�� (905�ֶ�) ��ȡ��ȡ��?\r\n\r\n(��ȡ) ����Ŀ��¼�л�ȡ��(����) ����������¼; (�ж�) �жϴ���",
            MessageBoxButtons.YesNoCancel,
            MessageBoxDefaultButton.Button2,
            null,
            ref bHideMessageBox,
            new string[] { "��ȡ", "����", "�ж�" });
                        }
                        if (result == System.Windows.Forms.DialogResult.No)
                            goto CONTINUE;
                        if (result == System.Windows.Forms.DialogResult.Cancel)
                            break; 
                        if (result == System.Windows.Forms.DialogResult.Yes)
                        {
                            string strContent = "";
                            int nRet = this.GetBiblioInfo(strBiblioRecPath,
                                "@accessno",
                                out strContent,
                                out strError);
                            if (nRet == -1)
                            {
                                strError = "�Ӳ��¼ {" + strRecPath + "} �������ּ�¼ " + strBiblioRecPath + " ��ȡ��ȡ�ŵ�ʱ��������: " + strError;
                                WriteErrorText(sw_error, strError);
                                nErrorCount++;
                                continue;
                            }

                            if (String.IsNullOrEmpty(strContent.Replace("/", "")) == true)
                            {
                                strBiblio = "";
                                // ��Ҳû����ȡ��
                                strError = "���¼ {" + strRecPath + "} ��û����ȡ�ţ�������������ּ�¼ " + strBiblioRecPath + " ��Ҳû�������";
                                WriteErrorText(sw_error, strError);
                                nErrorCount++;
                                continue;
                            }

                            strAccessNo = strContent;
                        }
#endif

                        {
                            string strContent = "";
                            int nRet = this.GetBiblioInfo(strBiblioRecPath,
                                "@accessno",
                                out strContent,
                                out strError);
                            if (nRet == -1)
                            {
                                strError = "�Ӳ��¼ {" + strRecPath + "} ����������Ŀ��¼ " + strBiblioRecPath + " ��ȡ��ȡ�ŵ�ʱ��������: " + strError;
                                WriteErrorText(sw_error, strError);
                                nErrorCount++;
                                continue;
                            }

                            if (String.IsNullOrEmpty(strContent.Replace("/", "")) == true)
                            {
                                strBiblio = "";
                                // ��Ҳû����ȡ��

                                if (strAccessNo == "˳�δӲ��¼����Ŀ��¼")
                                    strError = "���¼ {" + strRecPath + "} ��û����ȡ�ţ��������������Ŀ��¼ " + strBiblioRecPath + " ��Ҳû���ҵ���ȡ��";
                                else
                                    strError = "�Ӳ��¼ {" + strRecPath + "} ����������Ŀ��¼ " + strBiblioRecPath + " ��û���ҵ���ȡ��";
                                WriteErrorText(sw_error, strError);
                                nErrorCount++;
                                continue;
                            }

                            strAccessNo = strContent;
                        }

#if NO
                        // ���?
                        string strVolume = DomUtil.GetElementText(dom.DocumentElement, "volume");
                        if (String.IsNullOrEmpty(strVolume) == false)
                            strAccessNo += "/" + strVolume;
#endif
                    }



                    string strText = strAccessNo.Replace("/", "\r\n");

                    try
                    {
                        sw.Write(strText + "\r\n***\r\n");
                    }
                    catch (Exception ex)
                    {
                        strError = "д���ǩ�ļ� " + strOutputFilename + " ʱ��������: " + ex.Message;
                        return -1;
                    }

                    nLabelCount++;

                    CONTINUE:
                    stop.SetProgressValue(i);
                } // end of for
            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                stop.HideProgress();

                if (sw != null)
                    sw.Close();

                if (sw_error != null)
                    sw_error.Close();

            }

            if (FileUtil.GetFileLength(strOutputErrorFilename) == 0
                || nErrorCount == 0)
                File.Delete(strOutputErrorFilename);

            return 0;
        }

        /// <summary>
        /// ��ӡԤ��(���ݱ�ǩ�ļ�)
        /// </summary>
        /// <param name="bDisplayPrinterDialog">�Ƿ���ʾ��ӡ�����öԻ���</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int PrintPreviewFromLabelFile(bool bDisplayPrinterDialog = false)
        {
            string strError = "";

            int nRet = Print(
                this.textBox_labelDefFilename.Text,
                this.textBox_labelFile_labelFilename.Text,
                bDisplayPrinterDialog,
                true,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }
#if NO
        // 
        /// <summary>
        /// ��ӡԤ��(���ݱ�ǩ�ļ�)
        /// </summary>
        /// <param name="bDisplayPrinterDialog">�Ƿ���ʾ��ӡ�����öԻ���</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int PrintPreviewFromLabelFile(bool bDisplayPrinterDialog = false)
        {
            string strError = "";

            int nRet = this.BeginPrint(
                this.textBox_labelFile_labelFilename.Text,
                this.textBox_labelDefFilename.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.document.PreviewMode = true;

            this.MainForm.OperHistory.AppendHtml("<div class='debug begin'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ��ʼִ�д�ӡԤ��</div>");
            this.EnableControls(false);
            try
            {
                printDialog1.Document = this.document;

                if (this.PrinterInfo != null)
                {
                    string strPrinterName = document.PrinterSettings.PrinterName;
                    if (string.IsNullOrEmpty(this.PrinterInfo.PrinterName) == false
                        && this.PrinterInfo.PrinterName != strPrinterName)
                    {
                        this.document.PrinterSettings.PrinterName = this.PrinterInfo.PrinterName;
                        if (this.document.PrinterSettings.IsValid == false)
                        {
                            MessageBox.Show(this, "��ӡ�� " + this.PrinterInfo.PrinterName + " ��ǰ�����ã�������ѡ����ӡ��");
                            this.document.PrinterSettings.PrinterName = strPrinterName;
                            this.PrinterInfo.PrinterName = "";
                            bDisplayPrinterDialog = true;
                        }
                    }

                    PaperSize old_papersize = document.DefaultPageSettings.PaperSize;
                    if (string.IsNullOrEmpty(this.PrinterInfo.PaperName) == false
                        && this.PrinterInfo.PaperName != document.DefaultPageSettings.PaperSize.PaperName)
                    {
                        PaperSize found = null;
                        foreach (PaperSize ps in this.document.PrinterSettings.PaperSizes)
                        {
                            if (ps.PaperName.Equals(this.PrinterInfo.PaperName))
                            {
                                found = ps;
                                break;
                            }
                        }

                        if (found != null)
                            this.document.DefaultPageSettings.PaperSize = found;
                        else
                        {
                            MessageBox.Show(this, "��ӡ�� " + this.PrinterInfo.PrinterName + " ��ֽ������ " + this.PrinterInfo.PaperName + " ��ǰ�����ã�������ѡ��ֽ��");
                            document.DefaultPageSettings.PaperSize = old_papersize;
                            this.PrinterInfo.PaperName = "";
                            bDisplayPrinterDialog = true;
                        }
                    }

                    // ֻҪ��һ����ӡ������û��ȷ������Ҫ���ִ�ӡ���Ի���
                    if (string.IsNullOrEmpty(this.PrinterInfo.PrinterName) == true
                        || string.IsNullOrEmpty(this.PrinterInfo.PaperName) == true)
                        bDisplayPrinterDialog = true;
                }
                else
                {
                    // û����ѡ���õ������Ҫ���ִ�ӡ�Ի���
                    bDisplayPrinterDialog = true;
                }

                DialogResult result = DialogResult.OK;
                if (bDisplayPrinterDialog == true)
                {
                    result = printDialog1.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        // �����ӡ����
                        if (this.PrinterInfo == null)
                            this.PrinterInfo = new PrinterInfo();
                        this.PrinterInfo.PrinterName = document.PrinterSettings.PrinterName;
                        this.PrinterInfo.PaperName = document.DefaultPageSettings.PaperSize.PaperName;

                        // 2014/3/27
                        this.document.DefaultPageSettings = document.PrinterSettings.DefaultPageSettings;

                        SetTitle();
                    }
                }

                TracePrinterInfo();

                printPreviewDialog1.Document = this.document;

                this.MainForm.AppInfo.LinkFormState(printPreviewDialog1, "labelprintform_printpreviewdialog_state");
                printPreviewDialog1.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(printPreviewDialog1);
            }
            finally
            {
                this.EnableControls(true);

                this.EndPrint();

                this.MainForm.OperHistory.AppendHtml("<div class='debug end'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ����ִ�д�ӡԤ��</div>");
            }

            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }
#endif

        void TracePrinterInfo()
        {
            this.OutputText("��ӡ������: " + this.document.PrinterSettings.PrinterName);
            this.OutputText("ֽ������: " + this.document.DefaultPageSettings.PaperSize.ToString());
            this.OutputText("ֽ�ŷ���: " +
                    (this.document.DefaultPageSettings.Landscape == true ? "����" : "����"));
            this.OutputText("�ɴ�ӡ����: " + this.document.DefaultPageSettings.PrintableArea.ToString());
        }

        /// <summary>
        /// ѡ����ӡ��������ӡ����
        /// </summary>
        /// <param name="document">PrintDocument ����</param>
        /// <param name="strPrinterName">��ӡ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>0: �ɹ�ѡ��: 1: û��ѡ������Ϊ���ֲ����á����������ִ�ӡ���Ի���ѡ��</returns>
        public static int SelectPrinterByName(PrintDocument document,
            string strPrinterName,
            out string strError)
        {
            strError = "";

            string strCurrentPrinterName = document.PrinterSettings.PrinterName;
            if (string.IsNullOrEmpty(strPrinterName) == false
                && strPrinterName != strCurrentPrinterName)
            {
                document.PrinterSettings.PrinterName = strPrinterName;
                if (document.PrinterSettings.IsValid == false)
                {
                    strError = "��ӡ�� " + strPrinterName + " ��ǰ�����ã�������ѡ����ӡ��";
                    document.PrinterSettings.PrinterName = strCurrentPrinterName;
                    return 1;
                }
            }

            return 0;
        }

#if NO
        /// <summary>
        /// ѡ��ֽ�ţ���ֽ������
        /// </summary>
        /// <param name="document">PrintDocument ����</param>
        /// <param name="strPaperName">ֽ������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>0: �ɹ�ѡ��: 1: û��ѡ������Ϊ���ֲ����á����������ִ�ӡ���Ի���ѡ��</returns>
        public static int SelectPaperByName(PrintDocument document,
            string strPaperName,
            bool bCheck,
            out string strError)
        {
            strError = "";

            PaperSize old_papersize = document.DefaultPageSettings.PaperSize;
            if ((string.IsNullOrEmpty(strPaperName) == false
                && strPaperName != document.DefaultPageSettings.PaperSize.PaperName)
                || 
                (bCheck == true && string.IsNullOrEmpty(strPaperName) == false))
            {
                PaperSize found = null;
                foreach (PaperSize ps in document.PrinterSettings.PaperSizes)
                {
                    if (ps.PaperName.Equals(strPaperName))
                    {
                        found = ps;
                        break;
                    }
                }

                if (found != null)
                    document.DefaultPageSettings.PaperSize = found;
                else
                {
                    strError = "��ӡ�� " + document.PrinterSettings.PrinterName + " ��ֽ������ " + strPaperName + " ��ǰ�����ã�������ѡ��ֽ��";
                    // document.DefaultPageSettings.PaperSize = old_papersize;
                    return 1;
                }
            }

            return 0;
        }
#endif
        /// <summary>
        /// ѡ��ֽ�ţ���ֽ������
        /// </summary>
        /// <param name="document">PrintDocument ����</param>
        /// <param name="strPaperName">ֽ������</param>
        /// <param name="bLandscape">�Ƿ�Ϊ����</param>
        /// <param name="bCheck">�Ƿ���ֽ�Ű����ڴ�ӡ����ֽ���б���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>0: �ɹ�ѡ��: 1: û��ѡ������Ϊ���ֲ����á����������ִ�ӡ���Ի���ѡ��</returns>
        public static int SelectPaperByName(PrintDocument document,
            string strPaperName,
            bool bLandscape,
            bool bCheck,
            out string strError)
        {
            strError = "";

            PaperSize old_papersize = document.DefaultPageSettings.PaperSize;

            PaperSize paper_size = PrintUtil.BuildPaperSize(strPaperName);
            if (paper_size != null)
            {
                // ���м��
                if (bCheck == true)
                {
                    PaperSize found = null;
                    foreach (PaperSize ps in document.PrinterSettings.PaperSizes)
                    {
                        if (ps.PaperName.Equals(paper_size.PaperName)
                            && ps.Width == paper_size.Width
                            && ps.Height == paper_size.Height)
                        {
                            found = ps;
                            break;
                        }
                    }

                    if (found != null)
                    {
                        paper_size = found;
                        goto END1;
                    }

                    // ���û��ƥ��ģ����˶�����Σ��óߴ�ƥ��һ��
                    found = null;
                    foreach (PaperSize ps in document.PrinterSettings.PaperSizes)
                    {
                        if (ps.Width == paper_size.Width
                            && ps.Height == paper_size.Height)
                        {
                            found = ps;
                            break;
                        }
                    }

                    if (found != null)
                    {
                        paper_size = found;
                        goto END1;
                    }

                    strError = "��ӡ�� " + document.PrinterSettings.PrinterName + " ��ֽ������ " + strPaperName + " ��ǰ�����ã�������ѡ��ֽ��";
                    return 1;
                }

            END1:
                document.DefaultPageSettings.PaperSize = paper_size;    // ע��ֱ�� new PaperSize ������ֵ���ᵼ�´�ӡ���Ի�����ֽ������Ϊ�ա�Ҳ����԰� PrinterSetting �����Ҳ�޸��˾Ϳ�����?
                document.DefaultPageSettings.Landscape = bLandscape;
            }
            else
            {
                // ��̽������������
                PaperSize found = null;
                foreach (PaperSize ps in document.PrinterSettings.PaperSizes)
                {
                    if (ps.PaperName.Equals(strPaperName) == true)
                    {
                        found = ps;
                        break;
                    }
                }

                if (found != null)
                {
                    paper_size = found;
                    document.DefaultPageSettings.PaperSize = paper_size;
                    document.DefaultPageSettings.Landscape = bLandscape;
                    return 0;
                }

                strError = "��ӡ�� " + document.PrinterSettings.PrinterName + " ��ֽ������ " + strPaperName + " ��ǰ�����ã�������ѡ��ֽ��";
                return 1;
            }
#if NO
            if ((string.IsNullOrEmpty(strPaperName) == false
                && strPaperName != document.DefaultPageSettings.PaperSize.PaperName)
                ||
                (bCheck == true && string.IsNullOrEmpty(strPaperName) == false))
            {
                PaperSize found = null;
                foreach (PaperSize ps in document.PrinterSettings.PaperSizes)
                {
                    if (ps.PaperName.Equals(strPaperName))
                    {
                        found = ps;
                        break;
                    }
                }

                if (found != null)
                    document.DefaultPageSettings.PaperSize = found;
                else
                {
                    strError = "��ӡ�� " + document.PrinterSettings.PrinterName + " ��ֽ������ " + strPaperName + " ��ǰ�����ã�������ѡ��ֽ��";
                    // document.DefaultPageSettings.PaperSize = old_papersize;
                    return 1;
                }
            }
#endif

            return 0;
        }

        /// <summary>
        /// ��ӡ���ӡԤ��
        /// </summary>
        /// <param name="strLabelDefFileName">��ǩ�����ļ�</param>
        /// <param name="strLabelFileName">��ǩ�����ļ�</param>
        /// <param name="bDisplayPrinterDialog">�Ƿ���ʾ��ӡ�����öԻ���</param>
        /// <param name="bPrintPreview">�Ƿ���д�ӡԤ����false ��ʾ���д�ӡ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int Print(
            string strLabelDefFileName,
            string strLabelFileName,
            bool bDisplayPrinterDialog,
            bool bPrintPreview,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            this.MainForm.OperHistory.AppendHtml("<div class='debug begin'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ��ʼִ�д�ӡ"
                + (bPrintPreview == true ? "Ԥ��" : "")
                +"</div>");

            try
            {
                nRet = this.BeginPrint(
                    strLabelFileName,
                    strLabelDefFileName,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.document.PreviewMode = bPrintPreview;

                this.EnableControls(false);
                Cursor oldCursor = this.Cursor;
                if (bPrintPreview == false)
                    this.Cursor = Cursors.WaitCursor;
                try
                {
                    bool bCustomPaper = false;

                    if (bPrintPreview == false)
                    {
                        // Allow the user to choose the page range he or she would
                        // like to print.
                        printDialog1.AllowSomePages = true;

                        // Show the help button.
                        printDialog1.ShowHelp = true;
                    }

                    printDialog1.Document = this.document;

                    if (this.PrinterInfo != null)
                    {

                        // this.OutputText("�ָ���ǰ�Ĵ�ӡ����: " + this.PrinterInfo.PrinterName + ", ֽ����: " + this.PrinterInfo.PaperName);

#if NO
                        string strPrinterName = document.PrinterSettings.PrinterName;
                        if (string.IsNullOrEmpty(this.PrinterInfo.PrinterName) == false
                            && this.PrinterInfo.PrinterName != strPrinterName)
                        {
                            this.document.PrinterSettings.PrinterName = this.PrinterInfo.PrinterName;
                            if (this.document.PrinterSettings.IsValid == false)
                            {
                                this.document.PrinterSettings.PrinterName = strPrinterName;
                                MessageBox.Show(this, "��ӡ�� " + this.PrinterInfo.PrinterName + " ��ǰ�����ã�������ѡ����ӡ��");
                                this.PrinterInfo.PrinterName = "";
                                bDisplayPrinterDialog = true;
                            }
                        }
#endif
                        // ���մ洢�Ĵ�ӡ����ѡ����ӡ��
                        nRet = SelectPrinterByName(this.document,
                            this.PrinterInfo.PrinterName,
                            out  strError);
                        if (nRet == 1)
                        {
                            MessageBox.Show(this, "��ӡ�� " + this.PrinterInfo.PrinterName + " ��ǰ�����ã�������ѡ����ӡ��");
                            this.PrinterInfo.PrinterName = "";
                            bDisplayPrinterDialog = true;
                        }

#if NO
                        PaperSize old_papersize = document.DefaultPageSettings.PaperSize;
                        if (string.IsNullOrEmpty(this.PrinterInfo.PaperName) == false
                            && this.PrinterInfo.PaperName != document.DefaultPageSettings.PaperSize.PaperName)
                        {
                            PaperSize found = null;
                            foreach (PaperSize ps in this.document.PrinterSettings.PaperSizes)
                            {
                                if (ps.PaperName.Equals(this.PrinterInfo.PaperName))
                                {
                                    found = ps;
                                    break;
                                }
                            }

                            if (found != null)
                                this.document.DefaultPageSettings.PaperSize = found;
                            else
                            {
                                MessageBox.Show(this, "��ӡ�� " + this.PrinterInfo.PrinterName + " ��ֽ������ " + this.PrinterInfo.PaperName + " ��ǰ�����ã�������ѡ��ֽ��");
                                document.DefaultPageSettings.PaperSize = old_papersize;
                                this.PrinterInfo.PaperName = "";
                                bDisplayPrinterDialog = true;
                            }
                        }
#endif

                        // ��Ҫ�Զ���ֽ��
                        if (string.IsNullOrEmpty(this.label_param.DefaultPrinter) == true
                            && this.label_param.PageWidth > 0
                            && this.label_param.PageHeight > 0)
                        {
                            bCustomPaper = true;

                            PaperSize paper_size = new PaperSize("Custom Label", 
                                (int)label_param.PageWidth,
                                (int)label_param.PageHeight);
                            this.document.DefaultPageSettings.PaperSize = paper_size;
                        }


                        if (// bDisplayPrinterDialog == false && 
                            bCustomPaper == false
                            && string.IsNullOrEmpty(this.PrinterInfo.PaperName) == false)
                        {
                            nRet = SelectPaperByName(this.document,
                                this.PrinterInfo.PaperName,
                                this.PrinterInfo.Landscape,
                                true,   // false,
                                out strError);
                            if (nRet == 1)
                            {
                                MessageBox.Show(this, "��ӡ�� " + this.PrinterInfo.PrinterName + " ��ֽ������ " + this.PrinterInfo.PaperName + " ��ǰ�����ã�������ѡ��ֽ��");
                                this.PrinterInfo.PaperName = "";
                                bDisplayPrinterDialog = true;
                            }
                        }

                        // ֻҪ��һ����ӡ������û��ȷ������Ҫ���ִ�ӡ���Ի���
                        if (bCustomPaper == false)
                        {
                            if (string.IsNullOrEmpty(this.PrinterInfo.PrinterName) == true
                                || string.IsNullOrEmpty(this.PrinterInfo.PaperName) == true)
                                bDisplayPrinterDialog = true;
                        }
                    }
                    else
                    {
                        // û����ѡ���õ������Ҫ���ִ�ӡ�Ի���
                        bDisplayPrinterDialog = true;
                    }

                    // this.document.DefaultPageSettings.Landscape = label_param.Landscape;

                    DialogResult result = DialogResult.OK;
                    if (bDisplayPrinterDialog == true)
                    {
                        result = printDialog1.ShowDialog();

                        if (result == DialogResult.OK)
                        {
                            if (bCustomPaper == true)
                            {
                                PaperSize paper_size = new PaperSize("Custom Label",
                                    (int)label_param.PageWidth,
                                    (int)label_param.PageHeight);
                                this.document.DefaultPageSettings.PaperSize = paper_size;
                            }

                            // �����ӡ����
                            if (this.PrinterInfo == null)
                                this.PrinterInfo = new PrinterInfo();

                            // this.OutputText("��ӡ���Ի��򷵻غ���ѡ���Ĵ�ӡ����: " + document.PrinterSettings.PrinterName + ", ֽ����: " + document.DefaultPageSettings.PaperSize.PaperName);

                            this.PrinterInfo.PrinterName = document.PrinterSettings.PrinterName;
                            // this.PrinterInfo.PaperName = document.PrinterSettings.DefaultPageSettings.PaperSize.PaperName;  // document.DefaultPageSettings.PaperSize.PaperName
                            this.PrinterInfo.PaperName = PrintUtil.GetPaperSizeString(document.DefaultPageSettings.PaperSize);
                            this.PrinterInfo.Landscape = document.DefaultPageSettings.Landscape;

                            if (bCustomPaper == false)
                            {
                                // 2014/3/27
                                // this.document.DefaultPageSettings = document.PrinterSettings.DefaultPageSettings;
                                nRet = SelectPaperByName(this.document,
                                    this.PrinterInfo.PaperName,
                                    this.PrinterInfo.Landscape,
                                    true,
                                    out strError);
                                if (nRet == 1)
                                {
                                    // MessageBox.Show(this, "��ӡ�� " + this.PrinterInfo.PrinterName + " ��ֽ������ " + this.PrinterInfo.PaperName + " ��ǰ�����ã�������ѡ��ֽ��");
                                    //this.PrinterInfo.PaperName = "";
                                    //bDisplayPrinterDialog = true;

                                    this.OutputText("��ӡ���Ի��򷵻غ󣬾�����飬ֽ�� " + this.PrinterInfo.PaperName + " ���ڴ�ӡ�� " + this.PrinterInfo.PrinterName + " �Ŀ���ֽ���б��С����ֶԻ������û�����ѡ��ֽ��");


                                    SelectPaperDialog paper_dialog = new SelectPaperDialog();
                                    MainForm.SetControlFont(paper_dialog, this.Font, false);
                                    paper_dialog.Comment = "ֽ�� " + this.PrinterInfo.PaperName + " ���ڴ�ӡ�� " + this.PrinterInfo.PrinterName + " �Ŀ���ֽ���б��С�\r\n������ѡ��ֽ��";
                                    paper_dialog.Document = this.document;
                                    this.MainForm.AppInfo.LinkFormState(paper_dialog, "paper_dialog_state");
                                    paper_dialog.ShowDialog(this);
                                    this.MainForm.AppInfo.UnlinkFormState(paper_dialog);

                                    if (paper_dialog.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                                        return 0;

                                    this.OutputText("�Ի�������ѡ����ֽ����: " + document.DefaultPageSettings.PaperSize.PaperName);
                                }
                            }

                            this.PrinterInfo.PaperName = PrintUtil.GetPaperSizeString(document.DefaultPageSettings.PaperSize);
                            this.PrinterInfo.Landscape = document.DefaultPageSettings.Landscape;

                            SetTitle();
                        }
                        else
                            return 0;
                    }

                    TracePrinterInfo();

                    if (bPrintPreview == true)
                    {
                        printPreviewDialog1.Document = this.document;

                        this.MainForm.AppInfo.LinkFormState(printPreviewDialog1, "labelprintform_printpreviewdialog_state");
                        printPreviewDialog1.ShowDialog(this);
                        this.MainForm.AppInfo.UnlinkFormState(printPreviewDialog1);
                    }
                    else
                    {
                        this.document.Print();
                    }
                }
                finally
                {
                    if (bPrintPreview == false)
                        this.Cursor = oldCursor;
                    this.EnableControls(true);

                    this.EndPrint();    // �رձ�ǩ�ļ����������ɾ��
                }
            }
            finally
            {
                this.MainForm.OperHistory.AppendHtml("<div class='debug end'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ����ִ�д�ӡ"
                + (bPrintPreview == true ? "Ԥ��" : "")
                + "</div>");
            }

            return 0;
        ERROR1:
            return -1;
        }


        // 
        /// <summary>
        /// ��ӡԤ��(���ݲ��¼)
        /// </summary>
        /// <param name="bDisplayPrinterDialog">�Ƿ���ʾ��ӡ�����öԻ���</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int PrintPreviewFromItemRecords(bool bDisplayPrinterDialog = false)
        {
            string strError = "";

            // ��Ҫ�ȴ�����ǩ�ļ�
            string strLabelFilename = this.MainForm.NewTempFilename(
                "temp_labelfiles",
                "~label_");
            string strErrorFilename = this.MainForm.NewTempFilename(
                "temp_labelfiles",
                "~error_");

            this.MainForm.OperHistory.AppendHtml("<div class='debug begin'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ��ʼִ�д�ӡԤ��</div>");

            try
            {
                this.textBox_errorInfo.Text = "��ǰ��ȡ����Դ: " + this.AccessNoSource + "\r\n\r\n";

                int nRet = BuildLabelFile(
                    strLabelFilename,
                    strErrorFilename,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                FileInfo fi = new FileInfo(strErrorFilename);
                if (fi.Exists && fi.Length > 0)
                {
                    string strContent = "";
                    nRet = Global.ReadTextFileContent(strErrorFilename,
                        out strContent,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    this.textBox_errorInfo.Text += strContent;

                    DialogResult result = MessageBox.Show(this,
                        "������ǩ�ļ��Ĺ������б�����Ϣ�������Ƿ�������д�ӡԤ��?\r\n\r\n(Yes ������ӡԤ����No �����д�ӡԤ��)",
                        "LabelPrintForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                    {
                        this.textBox_errorInfo.Focus();
                        return 0;
                    }

                }

                nRet = Print(this.textBox_labelDefFilename.Text,
                    strLabelFilename,
                    bDisplayPrinterDialog,
                    true,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            finally
            {
                if (String.IsNullOrEmpty(strLabelFilename) == false)
                {
                    try
                    {
                        File.Delete(strLabelFilename);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "ɾ����ʱ��ǩ�ļ� '"+strLabelFilename+"' ʱ����: " + ex.Message);
                    }
                }
                if (String.IsNullOrEmpty(strErrorFilename) == false)
                {
                    try
                    {
                        File.Delete(strErrorFilename);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "ɾ����ʱ�����ļ� '" + strErrorFilename + "' ʱ����: " + ex.Message);
                    }
                }

                // this.MainForm.OperHistory.AppendHtml("<div class='debug end'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ����ִ�д�ӡԤ��</div>");
            }

            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }


        // parameters:
        //      strLabelFilename    ��ǩ�ļ���
        //      strDefFilename  �����ļ���
        int BeginPrint(
            string strLabelFilename,
            string strDefFilename,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strDefFilename) == true)
            {
                strError = "��δָ����ǩ�����ļ���";
                return -1;
            }


            if (String.IsNullOrEmpty(strLabelFilename) == true)
            {
                strError = "��δָ����ǩ�ļ���";
                return -1;
            }

            LabelParam label_param = null;

            int nRet = LabelParam.Build(strDefFilename,
                out label_param,
                out strError);
            if (nRet == -1)
                return -1;

            this.label_param = label_param;

            if (this.document != null)
            {
                this.document.Close();
                this.document = null;
            }

            this.document = new PrintLabelDocument();
            nRet = this.document.Open(strLabelFilename,
                out strError);
            if (nRet == -1)
                return -1;

            this.document.PrintPage -= new System.Drawing.Printing.PrintPageEventHandler(document_PrintPage);
            this.document.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(document_PrintPage);

            if (this.checkBox_testingGrid.Checked == true)
                this.m_strPrintStyle = "TestingGrid";
            else
                this.m_strPrintStyle = "";

            return 0;
        }

        void document_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            this.document.DoPrintPage(this,
                this.label_param,
                this.m_strPrintStyle,
                e);
        }

        void EndPrint()
        {
            if (this.document != null)
            {
                this.document.Close();
                this.document = null;
            }
        }

        private void button_print_Click(object sender, EventArgs e)
        {
            if (this.tabControl_main.SelectedTab == this.tabPage_labelFile)
                PrintFromLabelFile();
            else if (this.tabControl_main.SelectedTab == this.tabPage_itemRecords)
                PrintFromItemRecords();
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.textBox_labelFile_labelFilename.Enabled = bEnable;
            this.button_labelFile_findLabelFilename.Enabled = bEnable;

            this.textBox_labelDefFilename.Enabled = bEnable;
            this.button_findLabelDefFilename.Enabled = bEnable;

            this.button_print.Enabled = bEnable;
            this.button_printPreview.Enabled = bEnable;

            this.checkBox_testingGrid.Enabled = bEnable;

            this.Update();
        }

        // 
        /// <summary>
        /// ��ӡ(���ݱ�ǩ�ļ�)
        /// </summary>
        /// <param name="bDisplayPrinterDialog">�Ƿ���ʾ��ӡ�����öԻ���</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int PrintFromLabelFile(bool bDisplayPrinterDialog = true)
        {
            string strError = "";
            int nRet = this.Print(
                this.textBox_labelDefFilename.Text,
                this.textBox_labelFile_labelFilename.Text,
                bDisplayPrinterDialog,
                false,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // 
        /// <summary>
        /// ��ӡ(���ݲ��¼)
        /// </summary>
        /// <param name="bDisplayPrinterDialog">�Ƿ���ʾ��ӡ�����öԻ���</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int PrintFromItemRecords(bool bDisplayPrinterDialog = true)
        {
            string strError = "";

            // ��Ҫ�ȴ�����ǩ�ļ�
            string strLabelFilename = this.MainForm.NewTempFilename(
                "temp_labelfiles",
                "~label_");
            string strErrorFilename = this.MainForm.NewTempFilename(
    "temp_labelfiles",
    "~error_");

            try
            {
                this.textBox_errorInfo.Text = "";

                int nRet = BuildLabelFile(
                    strLabelFilename,
                    strErrorFilename,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                FileInfo fi = new FileInfo(strErrorFilename);
                if (fi.Exists && fi.Length > 0)
                {
                    string strContent = "";
                    nRet = Global.ReadTextFileContent(strErrorFilename,
                        out strContent,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    this.textBox_errorInfo.Text = strContent;

                    DialogResult result = MessageBox.Show(this,
                        "������ǩ�ļ��Ĺ������б�����Ϣ�������Ƿ�������д�ӡ?\r\n\r\n(Yes ������ӡ��No �����д�ӡ)",
                        "LabelPrintForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                    {
                        this.textBox_errorInfo.Focus();
                        return 0;
                    }

                }

                nRet = Print(this.textBox_labelDefFilename.Text,
                    strLabelFilename,
                    bDisplayPrinterDialog,
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

            }
            finally
            {
                if (String.IsNullOrEmpty(strLabelFilename) == false)
                {
                    try
                    {
                        File.Delete(strLabelFilename);
                    }
                    catch
                    {
                    }
                }
                if (String.IsNullOrEmpty(strErrorFilename) == false)
                {
                    try
                    {
                        File.Delete(strErrorFilename);
                    }
                    catch
                    {
                    }
                }

                // this.MainForm.OperHistory.AppendHtml("<div class='debug end'>" + HttpUtility.HtmlEncode(DateTime.Now.ToLongTimeString()) + " ����ִ�д�ӡ</div>");
            }

            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }


        // 
        /// <summary>
        /// ��ǩ(����)�ļ���
        /// </summary>
        public string LabelFilename
        {
            get
            {
                return this.textBox_labelFile_labelFilename.Text;
            }
            set
            {
                this.textBox_labelFile_labelFilename.Text = value;
            }
        }

        // 
        /// <summary>
        /// ��ǩ�����ļ���
        /// </summary>
        public string LabelDefFilename
        {
            get
            {
                return this.textBox_labelDefFilename.Text;
            }
            set
            {
                this.textBox_labelDefFilename.Text = value;
            }
        }

        private void listView_records_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            bool bSelected = false;
            string strFirstColumn = "";
            if (this.listView_records.SelectedItems.Count > 0)
            {
                bSelected = true;
                strFirstColumn = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 0);
            }

            if (String.IsNullOrEmpty(strFirstColumn) == false)
            {

                string strRecPath = "";
                if (bSelected == true)
                    strRecPath = this.listView_records.SelectedItems[0].Text;

                string strOpenStyle = "�¿���";
                if (this.LoadToExistDetailWindow == true)
                    strOpenStyle = "�Ѵ򿪵�";


                menuItem = new MenuItem("�� [���ݲ��¼·�� '" + strRecPath + "' װ�뵽" + strOpenStyle + "�ֲᴰ](&O)");
                menuItem.DefaultItem = true;
                menuItem.Click += new System.EventHandler(this.listView_records_DoubleClick);
                if (String.IsNullOrEmpty(strRecPath) == true)
                    menuItem.Enabled = false;
                contextMenu.MenuItems.Add(menuItem);

                string strBarcode = "";
                if (bSelected == true)
                {
                    string strError = "";
                    int nRet = GetItemBarcode(
    this.listView_records.SelectedItems[0],
    false,
    out strBarcode,
    out strError);
                    // strBarcode = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);
                }

                bool bExistEntityForm = (this.MainForm.GetTopChildWindow<EntityForm>() != null);
                bool bExistItemInfoForm = (this.MainForm.GetTopChildWindow<ItemInfoForm>() != null);

                //
                menuItem = new MenuItem("�򿪷�ʽ(&T)");
                contextMenu.MenuItems.Add(menuItem);

                // ��һ���Ӳ˵�

                strOpenStyle = "�¿���";

                // ��ʵ�崰����¼·��
                MenuItem subMenuItem = new MenuItem("װ��" + strOpenStyle + "ʵ�崰�����ݼ�¼·�� '" + strRecPath + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_itemInfoForm_recPath_newly_Click);
                if (String.IsNullOrEmpty(strRecPath) == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ��ʵ�崰�������
                subMenuItem = new MenuItem("װ��" + strOpenStyle + "ʵ�崰�����ݲ������ '" + strBarcode + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_itemInfoForm_barcode_newly_Click);
                if (String.IsNullOrEmpty(strBarcode) == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ���ֲᴰ����¼·��
                subMenuItem = new MenuItem("װ��" + strOpenStyle + "�ֲᴰ�����ݼ�¼·�� '" + strRecPath + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_entityForm_recPath_newly_Click);
                if (String.IsNullOrEmpty(strRecPath) == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ���ֲᴰ������
                subMenuItem = new MenuItem("װ��" + strOpenStyle + "�ֲᴰ�����ݲ����� '" + strBarcode + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_entityForm_barcode_newly_Click);
                if (String.IsNullOrEmpty(strBarcode) == true)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ---
                subMenuItem = new MenuItem("-");
                menuItem.MenuItems.Add(subMenuItem);

                strOpenStyle = "�Ѵ򿪵�";

                // ��ʵ�崰����¼·��
                subMenuItem = new MenuItem("װ��" + strOpenStyle + "ʵ�崰�����ݼ�¼·�� '" + strRecPath + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_itemInfoForm_recPath_exist_Click);
                if (String.IsNullOrEmpty(strRecPath) == true
                    || bExistItemInfoForm == false)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ��ʵ�崰������
                subMenuItem = new MenuItem("װ��" + strOpenStyle + "ʵ�崰�����ݲ������ '" + strBarcode + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_itemInfoForm_barcode_exist_Click);
                if (String.IsNullOrEmpty(strBarcode) == true
                    || bExistItemInfoForm == false)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ���ֲᴰ����¼·��
                subMenuItem = new MenuItem("װ��" + strOpenStyle + "�ֲᴰ�����ݼ�¼·�� '" + strRecPath + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_entityForm_recPath_exist_Click);
                if (String.IsNullOrEmpty(strRecPath) == true
                    || bExistEntityForm == false)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

                // ���ֲᴰ������
                subMenuItem = new MenuItem("װ��" + strOpenStyle + "�ֲᴰ�����ݲ������ '" + strBarcode + "'");
                subMenuItem.Click += new System.EventHandler(this.menu_entityForm_barcode_exist_Click);
                if (String.IsNullOrEmpty(strBarcode) == true
                    || bExistEntityForm == false)
                    subMenuItem.Enabled = false;
                menuItem.MenuItems.Add(subMenuItem);

            }


            // // //

            int nPathItemCount = 0;
            int nKeyItemCount = 0;
            GetSelectedItemCount(out nPathItemCount,
                out nKeyItemCount);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_cutToClipboard_Click);
            if (nPathItemCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("����(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copyToClipboard_Click);
            if (nPathItemCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            bool bHasClipboardObject = false;
            IDataObject iData = Clipboard.GetDataObject();
            if (iData != null
                && (iData.GetDataPresent(typeof(string)) == true
                || iData.GetDataPresent(typeof(ClipboardBookItemCollection)) == true)
                )
                bHasClipboardObject = true;
            else
                bHasClipboardObject = false;

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

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("������������ļ� [" + nPathItemCount.ToString() + "] (&B)");
            menuItem.Click += new System.EventHandler(this.menu_exportBarcodeFile_Click);
            if (nPathItemCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("��������¼·���ļ� [" + nPathItemCount.ToString() + "] (&S)");
            menuItem.Click += new System.EventHandler(this.menu_saveToRecordPathFile_Click);
            if (nPathItemCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�������ı��ļ� [" + this.listView_records.SelectedItems.Count.ToString() + "] (&T)");
            menuItem.Click += new System.EventHandler(this.menu_exportTextFile_Click);
            if (this.listView_records.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�Ӽ�¼·���ļ��е���(&I)");
            menuItem.Click += new System.EventHandler(this.menu_importFromRecPathFile_Click);
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("��������ļ��е���(&R)...");
            menuItem.Click += new System.EventHandler(this.menu_importFromBarcodeFile_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�Ƴ���ѡ������ [" + this.listView_records.SelectedItems.Count.ToString() + "] (&D)");
            menuItem.Click += new System.EventHandler(this.menu_removeSelectedItems_Click);
            if (this.listView_records.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("����б�(&C)");
            menuItem.Click += new System.EventHandler(this.menu_clearList_Click);
            if (this.listView_records.Items.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView_records, new Point(e.X, e.Y));	
        }

        void menu_selectAllLines_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.listView_records.Items.Count; i++)
            {
                this.listView_records.Items[i].Selected = true;
            }
        }

        void menu_copyToClipboard_Click(object sender, EventArgs e)
        {
            CopyLinesToClipboard(false);
        }

        void menu_cutToClipboard_Click(object sender, EventArgs e)
        {
            CopyLinesToClipboard(true);
        }

        // parameters:
        //      bCut    �Ƿ�Ϊ����
        void CopyLinesToClipboard(bool bCut)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            List<int> indices = new List<int>();
            string strTotal = "";
            for (int i = 0; i < this.listView_records.SelectedIndices.Count; i++)
            {
                int index = this.listView_records.SelectedIndices[i];

                ListViewItem item = this.listView_records.Items[index];
                string strLine = Global.BuildLine(item);
                strTotal += strLine + "\r\n";

                if (bCut == true)
                    indices.Add(index);
            }

            Clipboard.SetDataObject(strTotal, true);

            if (bCut == true)
            {
                for (int i = indices.Count - 1; i >= 0; i--)
                {
                    int index = indices[i];
                    this.listView_records.Items.RemoveAt(index);
                }
            }

            this.Cursor = oldCursor;
        }

        void menu_pasteFromClipboard_insertBefore_Click(object sender, EventArgs e)
        {
            PasteLines(true);
        }

        void menu_pasteFromClipboard_insertAfter_Click(object sender, EventArgs e)
        {
            PasteLines(false);
        }

        // parameters:
        //      bInsertBefore   �Ƿ�ǰ��? ���==trueǰ�壬������
        void PasteLines(bool bInsertBefore)
        {
            string strError = "";

            IDataObject ido = Clipboard.GetDataObject();

            if (ido == null)
            {
                strError = "��������û������";
                goto ERROR1;
            }

            if (ido.GetDataPresent(typeof(ClipboardBookItemCollection)) == true)
            {
                ClipboardBookItemCollection clipbookitems = (ClipboardBookItemCollection)ido.GetData(typeof(ClipboardBookItemCollection));
                if (clipbookitems == null)
                {
                    strError = "iData.GetData() return null";
                    goto ERROR1;
                }

                clipbookitems.RestoreNonSerialized();

                int index = -1;

                if (this.listView_records.SelectedIndices.Count > 0)
                    index = this.listView_records.SelectedIndices[0];

                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                this.listView_records.SelectedItems.Clear();
                for (int i = 0; i < clipbookitems.Count; i++)
                {
                    BookItem bookitem = clipbookitems[i];

                    string strBarcode = bookitem.Barcode;
                    string strRecPath = bookitem.RecPath;

                    ListViewItem item = new ListViewItem();
                    item.Text = strRecPath;
                    item.SubItems.Add(strBarcode);

                    if (index == -1)
                        this.listView_records.Items.Add(item);
                    else
                    {
                        if (bInsertBefore == true)
                            this.listView_records.Items.Insert(index, item);
                        else
                            this.listView_records.Items.Insert(index + 1, item);

                        index++;
                    }

                    item.Selected = true;

                }

                this.Cursor = oldCursor;
                return;
            }
            else if (ido.GetDataPresent(typeof(string)) == true)
            {
                string strWhole = (string)ido.GetData(DataFormats.UnicodeText);

                /*
                int index = -1;

                if (this.listView_records.SelectedIndices.Count > 0)
                    index = this.listView_records.SelectedIndices[0];

                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                this.listView_records.SelectedItems.Clear();

                string[] lines = strWhole.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    ListViewItem item = Global.BuildListViewItem(
                        this.listView_records,
                        lines[i]);

                    if (index == -1)
                        this.listView_records.Items.Add(item);
                    else
                    {
                        if (bInsertBefore == true)
                            this.listView_records.Items.Insert(index, item);
                        else
                            this.listView_records.Items.Insert(index + 1, item);

                        index++;
                    }

                    item.Selected = true;
                }

                this.Cursor = oldCursor;
                 * */
                DoPasteTabbedText(strWhole,
                    bInsertBefore);


                return;
            }
            else
            {
                strError = "�������мȲ�����ClipboardBookItemCollection���͵����ݣ�Ҳ������string��������";
                goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void DoPasteTabbedText(string strWhole,
            bool bInsertBefore)
        {
            int index = -1;

            if (this.listView_records.SelectedIndices.Count > 0)
                index = this.listView_records.SelectedIndices[0];

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            this.listView_records.SelectedItems.Clear();

            string[] lines = strWhole.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                ListViewItem item = Global.BuildListViewItem(
                    this.listView_records,
                    lines[i]);

                if (index == -1)
                    this.listView_records.Items.Add(item);
                else
                {
                    if (bInsertBefore == true)
                        this.listView_records.Items.Insert(index, item);
                    else
                        this.listView_records.Items.Insert(index + 1, item);

                    index++;
                }

                item.Selected = true;
            }

            this.Cursor = oldCursor;
        }

        private void listView_records_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewUtil.OnColumnClick(this.listView_records, e);
        }

        private void listView_records_SelectedIndexChanged(object sender, EventArgs e)
        {
#if NO
            if (this.listView_records.SelectedIndices.Count == 0)
                this.label_message.Text = "";
            else
            {
                if (this.listView_records.SelectedIndices.Count == 1)
                {
                    this.label_message.Text = "�� " + (this.listView_records.SelectedIndices[0] + 1).ToString() + " ��";
                }
                else
                {
                    this.label_message.Text = "�� " + (this.listView_records.SelectedIndices[0] + 1).ToString() + " �п�ʼ����ѡ�� " + this.listView_records.SelectedIndices.Count.ToString() + " ������";
                }
            }

            ListViewUtil.OnSeletedIndexChanged(this.listView_records,
                0,
                null);
#endif
            OnListViewSelectedIndexChanged(sender, e);
        }


        private void listView_records_DoubleClick(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫ����������");
                return;
            }

            string strFirstColumn = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 0);

            if (String.IsNullOrEmpty(strFirstColumn) == false)
            {
                string strOpenStyle = "new";
                if (this.LoadToExistDetailWindow == true)
                    strOpenStyle = "exist";

                // װ���ֲᴰ/ʵ�崰���ò������/��¼·��
                // parameters:
                //      strTargetFormType   Ŀ�괰������ "EntityForm" "ItemInfoForm"
                //      strIdType   ��ʶ���� "barcode" "recpath"
                //      strOpenType �򿪴��ڵķ�ʽ "new" "exist"
                LoadRecord("EntityForm",
                    "recpath",
                    strOpenStyle);
            }
            else
            {
                MessageBox.Show(this, "��һ�в���Ϊ��");
            }
        }

        // 
        /// <summary>
        /// �Ƿ�����װ���Ѿ��򿪵���ϸ��?
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

            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δ���б���ѡ��Ҫװ��" + strTargetFormName + "����");
                return;
            }

            string strBarcodeOrRecPath = "";

            if (strIdType == "barcode")
            {
                // barcode
                // strBarcodeOrRecPath = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 1);

                string strError = "";
                // ���� ListViewItem ���󣬻�ò�������е�����
                int nRet = GetItemBarcode(
                    this.listView_records.SelectedItems[0],
                    true,
                    out strBarcodeOrRecPath,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return;
                }
            }
            else
            {
                Debug.Assert(strIdType == "recpath", "");
                // recpath
                strBarcodeOrRecPath = ListViewUtil.GetItemText(this.listView_records.SelectedItems[0], 0);
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

        void menu_itemInfoForm_recPath_newly_Click(object sender, EventArgs e)
        {
            LoadRecord("ItemInfoForm",
                "recpath",
                "new");
        }

        void menu_itemInfoForm_barcode_newly_Click(object sender, EventArgs e)
        {
            LoadRecord("ItemInfoForm",
                "barcode",
                "new");
        }

        void menu_entityForm_recPath_newly_Click(object sender, EventArgs e)
        {
            LoadRecord("EntityForm",
                "recpath",
                "new");
        }

        void menu_entityForm_barcode_newly_Click(object sender, EventArgs e)
        {
            LoadRecord("EntityForm",
                "barcode",
                "new");
        }

        void menu_itemInfoForm_recPath_exist_Click(object sender, EventArgs e)
        {
            LoadRecord("ItemInfoForm",
                "recpath",
                "exist");
        }

        void menu_itemInfoForm_barcode_exist_Click(object sender, EventArgs e)
        {
            LoadRecord("ItemInfoForm",
                "barcode",
                "exist");
        }

        void menu_entityForm_recPath_exist_Click(object sender, EventArgs e)
        {
            LoadRecord("EntityForm",
                "recpath",
                "exist");
        }

        void menu_entityForm_barcode_exist_Click(object sender, EventArgs e)
        {
            LoadRecord("EntityForm",
                "barcode",
                "exist");
        }

        void GetSelectedItemCount(out int nPathItemCount,
    out int nKeyItemCount)
        {
            nPathItemCount = 0;
            nKeyItemCount = 0;
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                if (String.IsNullOrEmpty(item.Text) == false)
                    nPathItemCount++;
                else
                    nKeyItemCount++;
            }
        }

        void menu_clearList_Click(object sender, EventArgs e)
        {
            ClearListViewItems();
        }

        // TODO: �Ż��ٶ�
        void menu_importFromBarcodeFile_Click(object sender, EventArgs e)
        {
            SetStatusMessage("");   // �����ǰ��������ʾ

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
                        this.DbType + "SearchForm",
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
                    // ListViewUtil.ChangeItemText(item, 1, strBarcode);

                    this.listView_records.Items.Add(item);

                    FillLineByBarcode(
                        strBarcode, item);

                    items.Add(item);
                }

                // ˢ�������
                int nRet = RefreshListViewLines(items,
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // 2014/1/15
                // ˢ����ĿժҪ
                nRet = FillBiblioSummaryColumn(items,
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

        // �Ӽ�¼·���ļ��е���
        void menu_importFromRecPathFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            int nRet = ImportFromRecPathFile(null,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

#if NO
        // �Ӽ�¼·���ļ��е���
        void menu_importFromRecPathFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵Ĳ��¼·���ļ���";
            dlg.FileName = this.m_strUsedRecPathFilename;
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.m_strUsedRecPathFilename = dlg.FileName;

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
            stop.Initial("���ڵ����¼·�� ...");
            stop.BeginLoop();


            try
            {
                // �����������û����ģ������Ҫ������е������־
                ListViewUtil.ClearSortColumns(this.listView_records);


                if (this.listView_records.Items.Count > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "����ǰ�Ƿ�Ҫ������м�¼�б��е����е� " + this.listView_records.Items.Count.ToString() + " ��?\r\n\r\n(�������������µ�����н�׷���������к���)\r\n(Yes �����No �����(׷��)��Cancel ��������)",
                        "LabelPrintForm",
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

                    if (strRecPath == null)
                        break;

                    // TODO: ���·������ȷ�ԣ�������ݿ��Ƿ�Ϊʵ���֮һ

                    ListViewItem item = new ListViewItem();
                    item.Text = strRecPath;

                    this.listView_records.Items.Add(item);
                }

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                // stop.HideProgress();

                if (sr != null)
                    sr.Close();
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
#endif

#if NO
        // ����ѡ���������·���Ĳ����� ������������ Ϊ������ļ�
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
                    "LabelPrintForm",
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
                    if (String.IsNullOrEmpty(item.Text) == true)
                        continue;
                    string strBarcode = ListViewUtil.GetItemText(item, 1);
                    if (String.IsNullOrEmpty(strBarcode) == true)
                        continue;
                    sw.WriteLine(strBarcode);   // BUG!!!
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

            this.MainForm.StatusBarMessage = "������� " + this.listView_records.SelectedItems.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportBarcodeFilename;
        }
#endif

        // ����ѡ���������·���Ĳ����� ������������ Ϊ������ļ�
        void menu_exportBarcodeFile_Click(object sender, EventArgs e)
        {
            Debug.Assert(this.DbType == "item", "");

            string strError = "";

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
                    "�ļ� '" + this.ExportBarcodeFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel ��������)",
                    this.DbType + "SearchForm",
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

            // m_tableBarcodeColIndex.Clear();
            ClearColumnIndexCache();

            // �����ļ�
            StreamWriter sw = new StreamWriter(this.ExportBarcodeFilename,
                bAppend,	// append
                System.Text.Encoding.UTF8);
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {

                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    if (String.IsNullOrEmpty(item.Text) == true)
                        continue;

#if NO
                    string strRecPath = ListViewUtil.GetItemText(item, 0);
                    // ���ݼ�¼·��������ݿ���
                    string strItemDbName = Global.GetDbName(strRecPath);
                    // �������ݿ������ ������� �к�

                    int nCol = -1;
                    object o = m_tableBarcodeColIndex[strItemDbName];
                    if (o == null)
                    {
                        ColumnPropertyCollection temp = this.MainForm.GetBrowseColumnProperties(strItemDbName);
                        nCol = temp.FindColumnByType("item_barcode");
                        if (nCol == -1)
                        {
                            // ���ʵ���û���� browse �ļ��� ������� ��
                            strError = "���棺ʵ��� '"+strItemDbName+"' �� browse �����ļ���û�ж��� type Ϊ item_barcode ���С���ע��ˢ�»��޸Ĵ������ļ�";
                            MessageBox.Show(this, strError);

                            nCol = 0;   // ����󲿷��������Ч
                        }
                        if (m_bBiblioSummaryColumn == false)
                            nCol += 1;
                        else 
                            nCol += 2;

                        m_tableBarcodeColIndex[strItemDbName] = nCol;   // ��������
                    }
                    else
                        nCol = (int)o;

                    Debug.Assert(nCol > 0, "");

                    string strBarcode = ListViewUtil.GetItemText(item, nCol);
#endif

                    string strBarcode = "";
                    // ���� ListViewItem ���󣬻�ò�������е�����
                    int nRet = GetItemBarcode(
                        item,
                        true,
                        out strBarcode,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (String.IsNullOrEmpty(strBarcode) == true)
                        continue;
                    sw.WriteLine(strBarcode);   // BUG!!!
                }

            }
            finally
            {
                this.Cursor = oldCursor;
                if (sw != null)
                    sw.Close();
            }

            string strExportStyle = "����";
            if (bAppend == true)
                strExportStyle = "׷��";

            this.MainForm.StatusBarMessage = "������� " + this.listView_records.SelectedItems.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportBarcodeFilename;
            return;
        ERROR1:
            MessageBox.Show(strError);
        }

        // ����ѡ������е���·���Ĳ����� ����¼·���ļ�
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
                    "LabelPrintForm",
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
                    if (String.IsNullOrEmpty(item.Text) == true)
                        continue;
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

            this.MainForm.StatusBarMessage = "���¼·�� " + this.listView_records.SelectedItems.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ļ� " + this.ExportRecPathFilename;


        }


        // ����ѡ����е��ı��ļ�
        void menu_exportTextFile_Click(object sender, EventArgs e)
        {
            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ������ı��ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = this.ExportTextFilename;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "�ı��ļ� (*.txt)|*.txt|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.ExportTextFilename = dlg.FileName;

            bool bAppend = true;

            if (File.Exists(this.ExportTextFilename) == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "�ı��ļ� '" + this.ExportTextFilename + "' �Ѿ����ڡ�\r\n\r\n������������Ƿ�Ҫ׷�ӵ����ļ�β��? (Yes ׷�ӣ�No ���ǣ�Cancel ��������)",
                    "LabelPrintForm",
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
            StreamWriter sw = new StreamWriter(this.ExportTextFilename,
                bAppend,	// append
                System.Text.Encoding.UTF8);
            try
            {
                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                foreach (ListViewItem item in this.listView_records.SelectedItems)
                {
                    string strLine = Global.BuildLine(item);
                    sw.WriteLine(strLine);
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

            this.MainForm.StatusBarMessage = "������ " + this.listView_records.SelectedItems.Count.ToString() + "�� �ѳɹ�" + strExportStyle + "���ı��ļ� " + this.ExportTextFilename;
        }

        // �Ӵ�����������ѡ�������
        void menu_removeSelectedItems_Click(object sender, EventArgs e)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            for (int i = this.listView_records.SelectedIndices.Count - 1; i >= 0; i--)
            {
                this.listView_records.Items.RemoveAt(this.listView_records.SelectedIndices[i]);
            }

            this.Cursor = oldCursor;
        }

#if NO
        void ClearListViewItems()
        {
            this.listView_records.Items.Clear();
            ListViewUtil.ClearSortColumns(this.listView_records);
        }
#endif

        private void LabelPrintForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);
        }

        private void textBox_errorInfo_DoubleClick(object sender, EventArgs e)
        {
            if (textBox_errorInfo.Lines.Length == 0)
                return;

            int x = 0;
            int y = 0;
            API.GetEditCurrentCaretPos(
                textBox_errorInfo,
                out x,
                out y);

            string strLine = textBox_errorInfo.Lines[y];

            // �������¼·������ {} ��
            int nRet = strLine.IndexOf("{");
            if (nRet == -1)
                goto ERROR1;

            string strRecPath = strLine.Substring(nRet+1).Trim();
            nRet = strRecPath.IndexOf("}");
            if (nRet != -1)
                strRecPath = strRecPath.Substring(0, nRet).Trim();

            // ѡ��listview����һ��
            ListViewItem item = ListViewUtil.FindItem(this.listView_records,
                strRecPath,
                0);
            if (item == null)
                goto ERROR1;

            ListViewUtil.SelectLine(item, true);
            item.EnsureVisible();

            return;
        ERROR1:
            Console.Beep();
        }

        /// <summary>
        /// �����ǩ�ļ�������ҳ
        /// </summary>
        public void ActivateLabelFilePage()
        {
            this.tabControl_main.SelectedTab = this.tabPage_labelFile;
        }

        /// <summary>
        /// ������¼������ҳ
        /// </summary>
        public void ActivateItemRecordsPage()
        {
            this.tabControl_main.SelectedTab = this.tabPage_itemRecords;
        }

        // �ڴ��ڴ�ǰTestingGrid�Ƿ����ù�
        bool m_bTestingGridSetted = false;

        //  
        /// <summary>
        /// �Ƿ��ӡ������
        /// </summary>
        public bool TestingGrid
        {
            get
            {
                return this.checkBox_testingGrid.Checked;
            }
            set
            {
                this.checkBox_testingGrid.Checked = value;
                this.m_bTestingGridSetted = true;
            }
        }

        private void listView_records_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private void listView_records_DragDrop(object sender, DragEventArgs e)
        {
            // string strError = "";

            string strWhole = (String)e.Data.GetData("Text");

            DoPasteTabbedText(strWhole,
                false);
            return;
            /*
        ERROR1:
            MessageBox.Show(this, strError);
             * */
        }

        private void button_editDefFile_Click(object sender, EventArgs e)
        {
            string strError = "";

#if NO
            if (string.IsNullOrEmpty(this.textBox_labelDefFilename.Text) == true)
            {
                strError = "����ָ����ǩ�����ļ���";
                goto ERROR1;
            }
#endif
            string strOldFileName = this.textBox_labelDefFilename.Text;

            LabelDesignForm dlg = new LabelDesignForm();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.DefFileName = this.textBox_labelDefFilename.Text;
            if (string.IsNullOrEmpty(this.textBox_labelFile_content.Text) == false)
                dlg.SampleLabelText = this.textBox_labelFile_content.Text;

            dlg.UiState = this.MainForm.AppInfo.GetString(
                    "label_print_form",
                    "LabelDesignForm_uiState",
                    "");

            this.MainForm.AppInfo.LinkFormState(dlg, "LabelDesignForm_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            this.MainForm.AppInfo.SetString(
        "label_print_form",
        "LabelDesignForm_uiState",
        dlg.UiState);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            // �ļ�������ƶԻ����з����˱仯
            if (string.IsNullOrEmpty(strOldFileName) == false
                && strOldFileName != dlg.DefFileName)
            {
                DialogResult result = MessageBox.Show(this,
"���ڱ�ǩ��ƶԻ�����װ�����µı�ǩ�����ļ��� '" + dlg.DefFileName + "'��\r\n\r\n����Ҫ�������ǩ�����ļ�Ӧ�õ���ǰ����ô? \r\n\r\n(Yes: Ӧ���µ��ļ���; No: ������ǰ���ļ�������)",
"LabePrintForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (result == System.Windows.Forms.DialogResult.Yes)
                    this.textBox_labelDefFilename.Text = dlg.DefFileName;
            }
            else
                this.textBox_labelDefFilename.Text = dlg.DefFileName;

            // ����ǰ���ļ���û�б仯��ҲҪ����һ��ˢ��
            if (strOldFileName == this.textBox_labelDefFilename.Text)
            {
                textBox_labelDefFilename_TextChanged(this, new EventArgs());
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // TODO: delay
        private void textBox_labelDefFilename_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.textBox_labelDefFilename.Text) == true)
                this.button_editDefFile.Text = "����";
            else
                this.button_editDefFile.Text = "���";

            // ��ͼ���ļ���ȡ�ô�ӡ����Ϣ������ʾ�ڴ��ڱ�����
            LoadPrinterInfo();
        }

        // ��ͼ�ӱ�ǩ�����ļ���ȡ�ô�ӡ����Ϣ������ʾ�ڴ��ڱ�����
        void LoadPrinterInfo()
        {
            string strError = "";

            if (String.IsNullOrEmpty(this.textBox_labelDefFilename.Text) == true)
            {
                strError = "��δָ����ǩ�����ļ���";
                goto ERROR1;
            }

            LabelParam label_param = null;
            int nRet = LabelParam.Build(this.textBox_labelDefFilename.Text,
                out label_param,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // �����ӡ����
            if (string.IsNullOrEmpty(label_param.DefaultPrinter) == false)
            {
                PrinterInfo temp = new PrinterInfo("ȱʡ��ǩ", label_param.DefaultPrinter);
                // ��� label_param �� Landscape �� DefaultPrinter ��һ��
                // temp.Landscape = label_param.Landscape;
                this.PrinterInfo = temp;
            }

            return;
        ERROR1:
            return;
        }

        // �Ӻδ���ȡ��ȡ��
        /*
�Ӳ��¼
����Ŀ��¼
˳�δӲ��¼����Ŀ��¼
         * */
        string AccessNoSource
        {
            get
            {
                return this.MainForm.AppInfo.GetString(
                "labelprint",
                "accessNo_source",
                "�Ӳ��¼");
            }
        }

        internal override bool InSearching
        {
            get
            {
                return false;
            }
        }
    }
}