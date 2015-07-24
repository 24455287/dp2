using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.IO;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ��������Ϣ��
    /// </summary>
    public partial class CheckBorrowInfoForm : MyForm
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

        /// <summary>
        /// ���캯��
        /// </summary>
        public CheckBorrowInfoForm()
        {
            InitializeComponent();
        }

        private void CheckBorrowInfoForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
#if NO
            MainForm.AppInfo.LoadMdiChildFormStates(this,
    "mdi_form_state");
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            Global.ClearForPureTextOutputing(this.webBrowser_resultInfo);

            this.checkBox_displayPriceString.Checked = this.MainForm.AppInfo.GetBoolean(
                "check_borrowinfo_form",
                "display_price_string",
                true);

            this.checkBox_forceCNY.Checked = this.MainForm.AppInfo.GetBoolean(
                "check_borrowinfo_form",
                "force_cny",
                false);

            this.checkBox_overwriteExistPrice.Checked = this.MainForm.AppInfo.GetBoolean(
                "check_borrowinfo_form",
                "overwrite_exist_price",
                false);

        }

#if NO
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }
#endif

        private void CheckBorrowInfoForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void CheckBorrowInfoForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {

                this.MainForm.AppInfo.SetBoolean(
                    "check_borrowinfo_form",
                    "display_price_string",
                    this.checkBox_displayPriceString.Checked);

                this.MainForm.AppInfo.SetBoolean(
                    "check_borrowinfo_form",
                    "force_cny",
                    this.checkBox_forceCNY.Checked);

                this.MainForm.AppInfo.SetBoolean(
                    "check_borrowinfo_form",
                    "overwrite_exist_price",
                    this.checkBox_overwriteExistPrice.Checked);

            }
        }

        private void button_beginCheckFromReader_Click(object sender, EventArgs e)
        {
            string strError = "";
            List<string> barcodes = null;
            int nRet = SearchAllReaderBarcode(out barcodes,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            nRet = CheckAllReaderRecord(barcodes,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            MessageBox.Show(this, "OK");

            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        // ����������ж���֤�����
        int SearchAllReaderBarcode(out List<string> barcodes,
            out string strError)
        {
            strError = "";

            barcodes = new List<string>();

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                long lRet = Channel.SearchReader(stop,
                    "<ȫ��>",
                    "",
                    -1,
                    "֤����",
                    "left",
                    this.Lang,
                    null,   // strResultSetName
                    "", // strOutputStyle
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                long lHitCount = lRet;

                long lStart = 0;
                long lCount = lHitCount;

                stop.SetProgressRange(0, lCount);

                Global.WriteHtml(this.webBrowser_resultInfo,
    "���� "+lHitCount.ToString()+" �����߼�¼��\r\n");


                Record[] searchresults = null;

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
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
                        goto ERROR1;

                    if (lRet == 0)
                    {
                        strError =  "δ����";
                        goto ERROR1;
                    }

                    Debug.Assert(searchresults != null, "");

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        /*
                        NewLine(this.listView_records,
                            searchresults[i].Path,
                            searchresults[i].Cols);
                         * */
                        barcodes.Add(searchresults[i].Cols[0]);
                    }


                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    stop.SetMessage("�������� " + lHitCount .ToString()+ " �����ѻ������ " + lStart.ToString() + " ��");
                    stop.SetProgressValue(lStart);

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);

                // ����ȥ��
                stop.SetMessage("���������ȥ��");

                // ����
                barcodes.Sort();

                // ȥ��
                int nRemoved = 0;
                for (int i = 0; i < barcodes.Count; i++)
                {
                    string strBarcode = barcodes[i];

                    for (int j = i + 1; j < barcodes.Count; j++)
                    {
                        if (strBarcode == barcodes[j])
                        {
                            barcodes.RemoveAt(j);
                            nRemoved++;
                            j--;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
            }

            return 0;

        ERROR1:
            return -1;
        }

        int CheckAllReaderRecord(List<string> barcodes,
            out string strError)
        {
            strError = "";
            long lRet = 0;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ��м�� ...");
            stop.BeginLoop();

            EnableControls(false);

            string[] aDupPath = null;
            try
            {
                Global.WriteHtml(this.webBrowser_resultInfo,
                    "���ڽ��м��...\r\n");

                stop.SetProgressRange(0, barcodes.Count);
                int nCount = 0;
                for (int i = 0; i < barcodes.Count; i++)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    string strReaderBarcode = barcodes[i];
                    string strOutputReaderBarcode = "";

                    stop.SetMessage("���ڼ��� "+(i+1).ToString()+" �����߼�¼������Ϊ " + strReaderBarcode );
                    stop.SetProgressValue(i);

                    int nStart = 0;
                    int nPerCount = -1;
                    int nProcessedBorrowItems = 0;
                    int nTotalBorrowItems = 0;

                    for (; ; )
                    {

                        lRet = Channel.RepairBorrowInfo(
                            stop,
                            "checkfromreader",
                            strReaderBarcode,
                            "",
                            "",
                            nStart,   // 2008/10/27 
                            nPerCount,   // 2008/10/27 
                            out nProcessedBorrowItems,   // 2008/10/27 
                            out nTotalBorrowItems,   // 2008/10/27 
                            out strOutputReaderBarcode,
                            out aDupPath,
                            out strError);

                        string strOffsComment = "";
                        if (nStart > 0)
                        {
                            strOffsComment = "(ƫ����" + nStart.ToString() + "��ʼ��" + nProcessedBorrowItems + "�����Ĳ�)";
                        }


                        if (lRet == -1)
                        {
                            Global.WriteHtml(this.webBrowser_resultInfo,
                                "�����߼�¼ " + strReaderBarcode + " ʱ" + strOffsComment + "����: " + strError + "\r\n");
                        }
                        if (lRet == 1)
                        {
                            Global.WriteHtml(this.webBrowser_resultInfo,
                                "�����߼�¼ " + strReaderBarcode + " ʱ" + strOffsComment + "��������: " + strError + "\r\n");
                        }

                        if (nTotalBorrowItems > 0 && nProcessedBorrowItems == 0)
                        {
                            Debug.Assert(false, "��nTotalBorrowItemsֵ����0��ʱ��(Ϊ" + nTotalBorrowItems.ToString() + ")��nProcessedBorrowItemsֵ����Ϊ0");
                            break;
                        }

                        nStart += nProcessedBorrowItems;

                        if (nStart >= nTotalBorrowItems)
                            break;
                    }

                    // ����֤����Ų���
                    if (this.checkBox_checkReaderBarcodeDup.Checked == true)
                    {
                        string[] results = null;
                        lRet = Channel.GetReaderInfo(stop,
                            strReaderBarcode,
                            "xml",
                            out results,
                            out strError);
                        if (lRet == -1)
                        {
                            Global.WriteHtml(this.webBrowser_resultInfo,
                                "�Զ���֤����� " + strReaderBarcode + " ����ʱ����: " + strError + "\r\n");
                        }
                        if (lRet > 1)
                        {
                            Global.WriteHtml(this.webBrowser_resultInfo,
                                "����֤����� " + strReaderBarcode + " ���ظ���¼ " + lRet.ToString() + "��\r\n");
                        }
                    }


                    nCount++;
                }

                Global.WriteHtml(this.webBrowser_resultInfo,
                    "���������������߼�¼ " + nCount.ToString() + " ����\r\n");


            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
            }

            return 0;

        ERROR1:
            return -1;
        }

#if NO
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
            this.button_beginCheckFromReader.Enabled = bEnable;
            this.button_beginCheckFromItem.Enabled = bEnable;
            this.button_clearInfo.Enabled = bEnable;

            this.button_repairReaderSide.Enabled = bEnable;
            this.button_repairItemSide.Enabled = bEnable;
            this.textBox_itemBarcode.Enabled = bEnable;
            this.textBox_readerBarcode.Enabled = bEnable;

            this.button_batchAddItemPrice.Enabled = bEnable;

            this.checkBox_checkItemBarcodeDup.Enabled = bEnable;
            this.checkBox_checkReaderBarcodeDup.Enabled = bEnable;

            this.checkBox_displayPriceString.Enabled = bEnable;
            this.checkBox_forceCNY.Enabled = bEnable;
            this.checkBox_overwriteExistPrice.Enabled = bEnable;

            this.textBox_single_readerBarcode.Enabled = bEnable;
            this.textBox_single_itemBarcode.Enabled = bEnable;
            this.button_single_checkFromItem.Enabled = bEnable;
            this.button_single_checkFromReader.Enabled = bEnable;
        }

        private void button_beginCheckFromItem_Click(object sender, EventArgs e)
        {
            string strError = "";
            List<string> barcodes = null;
            int nRet = SearchAllItemBarcode(out barcodes,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            nRet = CheckAllItemRecord(barcodes,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            MessageBox.Show(this, "OK");

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ����������в������
        int SearchAllItemBarcode(out List<string> barcodes,
            out string strError)
        {
            strError = "";

            barcodes = new List<string>();

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڼ��� ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                long lRet = Channel.SearchItem(
                    stop,
                    "<all>",
                    "",
                    -1,
                    "������",
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

                stop.SetProgressRange(0, lCount);

                Global.WriteHtml(this.webBrowser_resultInfo,
    "���� " + lHitCount.ToString() + " �����¼��\r\n");


                Record[] searchresults = null;

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
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
                        /*
                        NewLine(this.listView_records,
                            searchresults[i].Path,
                            searchresults[i].Cols);
                         * */
                        barcodes.Add(searchresults[i].Cols[0]);
                    }


                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    stop.SetMessage("�������� " + lHitCount.ToString() + " �����ѻ������ " + lStart.ToString() + " ��");
                    stop.SetProgressValue(lStart);

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                }

                // ����ȥ��
                stop.SetMessage("���������ȥ��");

                // ����
                barcodes.Sort();

                // ȥ��
                int nRemoved = 0;
                for (int i = 0; i < barcodes.Count; i++)
                {
                    string strBarcode = barcodes[i];

                    for (int j = i+1; j < barcodes.Count; j++)
                    {
                        if (strBarcode == barcodes[j])
                        {
                            barcodes.RemoveAt(j);
                            nRemoved++;
                            j--;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
            }

            return 0;

        ERROR1:
            return -1;
        }

        // ����������в������(��һ�汾��������ļ�)
        int SearchAllItemBarcode(string strBarcodeFilename,
            out string strError)
        {
            strError = "";

            // �����ļ�
            StreamWriter sw = new StreamWriter(strBarcodeFilename,
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
                    long lRet = Channel.SearchItem(
                        stop,
                        "<all>",
                        "",
                        -1,
                        "������",
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

                    Global.WriteHtml(this.webBrowser_resultInfo,
        "���� " + lHitCount.ToString() + " �����¼��\r\n");


                    Record[] searchresults = null;

                    // װ�������ʽ
                    for (; ; )
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null && stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
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
                            // barcodes.Add(searchresults[i].Cols[0]);
                            sw.Write(searchresults[i].Cols[0] + "\r\n");
                        }


                        lStart += searchresults.Length;
                        lCount -= searchresults.Length;

                        stop.SetMessage("�������� " + lHitCount.ToString() + " �����ѻ������ " + lStart.ToString() + " ��");

                        if (lStart >= lHitCount || lCount <= 0)
                            break;
                    }

                    // MessageBox.Show(this, Convert.ToString(lRet) + " : " + strError);
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

        
        int CheckAllItemRecord(List<string> barcodes,
            out string strError)
        {
            strError = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ��м�� ...");
            stop.BeginLoop();

            EnableControls(false);

            string[] aDupPath = null;
            try
            {
                Global.WriteHtml(this.webBrowser_resultInfo,
                    "���ڽ��м��...\r\n");

                stop.SetProgressRange(0, barcodes.Count);

                int nCount = 0;
                for (int i = 0; i < barcodes.Count; i++)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    string strItemBarcode = barcodes[i];
                    string strOutputReaderBarcode = "";

                    stop.SetMessage("���ڼ��� " + (i + 1).ToString() + " �����¼������Ϊ " + strItemBarcode);
                    stop.SetProgressValue(i);

                    int nProcessedBorrowItems = 0;
                    int nTotalBorrowItems = 0;

                    long lRet = Channel.RepairBorrowInfo(
                        stop,
                        "checkfromitem",
                        "",
                        strItemBarcode,
                        "",
                        0,
                        -1,
                        out nProcessedBorrowItems,   // 2008/10/27 
                        out nTotalBorrowItems,   // 2008/10/27 
                        out strOutputReaderBarcode,
                        out aDupPath,
                        out strError);
                    if (lRet == -1 || lRet == 1)
                    {
                        if (Channel.ErrorCode == ErrorCode.ItemBarcodeDup)
                        {
                            List<string> linkedPath = new List<string>();

                            Global.WriteHtml(this.webBrowser_resultInfo,
                                "�����¼ " + strItemBarcode + " ʱ���ֲ�����������ظ���¼ " + aDupPath.Length.ToString() + "�� -- " + StringUtil.MakePathList(aDupPath) + "��\r\n");


                            for (int j = 0; j < aDupPath.Length; j++)
                            {
                                string strText = " ������е� "+(j+1).ToString()+" ����·��Ϊ " + aDupPath[j] + ": ";

                                string[] aDupPathTemp = null;
                                // string strOutputReaderBarcode = "";
                                long lRet_2 = Channel.RepairBorrowInfo(
                                    stop,
                                    "checkfromitem",
                                    "",
                                    strItemBarcode,
                                    aDupPath[j],
                        0,
                        -1,
                        out nProcessedBorrowItems,   // 2008/10/27 
                        out nTotalBorrowItems,   // 2008/10/27 
                                    out strOutputReaderBarcode,
                                    out aDupPathTemp,
                                    out strError);
                                if (lRet_2 == -1)
                                {
                                    goto ERROR1;
                                }
                                if (lRet_2 == 1)
                                {
                                    strText += "��������: " + strError + "\r\n";

                                    Global.WriteHtml(this.webBrowser_resultInfo,
                                        strText);
                                }

                            } // end of for

                            continue;
                        }

                        /*
                        Global.WriteHtml(this.webBrowser_resultInfo,
                            "�����¼ " + strItemBarcode + " ʱ����: " + strError + "\r\n");
                         * */
                        if (lRet == -1)
                        {
                            Global.WriteHtml(this.webBrowser_resultInfo,
                                "�����¼ " + strItemBarcode + " ʱ����: " + strError + "\r\n");
                        }
                        if (lRet == 1)
                        {
                            Global.WriteHtml(this.webBrowser_resultInfo,
                                "�����¼ " + strItemBarcode + " ʱ��������: " + strError + "\r\n");
                        }
                        continue;
                    } // end of return -1

                    if (lRet == -1)
                    {
                        Global.WriteHtml(this.webBrowser_resultInfo,
                            "�����¼ " + strItemBarcode + " ʱ����: " + strError + "\r\n");
                    }
                    if (lRet == 1)
                    {
                        Global.WriteHtml(this.webBrowser_resultInfo,
                            "�����¼ " + strItemBarcode + " ʱ��������: " + strError + "\r\n");
                    }


                    /*
                    if (this.checkBox_checkItemBarcodeDup.Checked == true)
                    {
                        string[] paths = null;
                        lRet = Channel.SearchItemDup(stop,
                            strItemBarcode,
                            100,
                            out paths,
                            out strError);
                        if (lRet == -1)
                        {
                            Global.WriteHtml(this.webBrowser_resultInfo,
                                "�Բ������ " + strItemBarcode + " ����ʱ����: " + strError + "\r\n");
                        }
                        if (lRet > 1)
                        {
                            Global.WriteHtml(this.webBrowser_resultInfo,
                                "������� " + strItemBarcode + " ���ظ���¼ " + paths.Length.ToString() + "��\r\n");
                        }
                    }
                     * */

                    nCount++;
                }

                Global.WriteHtml(this.webBrowser_resultInfo,
                    "���������������¼ " + nCount.ToString() + " ����\r\n");


            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();
            }

            return 0;

        ERROR1:
            return -1;
        }

        private void button_repairReaderSide_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = RepairError(
                "repairreaderside",
                this.textBox_readerBarcode.Text,
                this.textBox_itemBarcode.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            MessageBox.Show(this, "�޸��ɹ���");
            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        int RepairError(
            string strAction,
            string strReaderBarcode,
            string strItemBarcode,
            out string strError)
        {
            strError = "";
            int nProcessedBorrowItems = 0;
            int nTotalBorrowItems = 0;

            Debug.Assert(strAction == "repairreaderside" || strAction == "repairitemside", "");

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ����޸� ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                    string strConfirmItemRecPath = "";
                REDO:
                    string[] aDupPath = null;

                    string strOutputReaderBarcode = "";

                    long lRet = Channel.RepairBorrowInfo(
                        stop,
                        strAction,  // "repairreaderside",
                        strReaderBarcode,
                        strItemBarcode,
                        strConfirmItemRecPath,
                        0,
                        -1,
                        out nProcessedBorrowItems,   // 2008/10/27 
                        out nTotalBorrowItems,   // 2008/10/27 
                        out strOutputReaderBarcode,
                        out aDupPath,
                        out strError);
                    if (lRet == -1)
                    {
                        if (Channel.ErrorCode == ErrorCode.ItemBarcodeDup)
                        {
                            this.MainForm.PrepareSearch();
                            try
                            {
                                ItemBarcodeDupDlg dupdlg = new ItemBarcodeDupDlg();
                                MainForm.SetControlFont(dupdlg, this.Font, false);
                                string strErrorNew = "";
                                int nRet = dupdlg.Initial(
                                    this.MainForm,
                                    aDupPath,
                                    "�������ŷ����ظ����޸��������ܾ���\r\n\r\n�ɸ��������г�����ϸ��Ϣ��ѡ���ʵ��Ĳ��¼�����Բ�����\r\n\r\nԭʼ������Ϣ:\r\n" + strError,
                                    this.MainForm.Channel,
                                    this.MainForm.Stop,
                                    out strErrorNew);
                                if (nRet == -1)
                                {
                                    // ��ʼ���Ի���ʧ��
                                    MessageBox.Show(this, strErrorNew);
                                    goto ERROR1;
                                }

                                this.MainForm.AppInfo.LinkFormState(dupdlg, "CheckBorrowInfoForm_dupdlg_state");
                                dupdlg.ShowDialog(this);
                                this.MainForm.AppInfo.UnlinkFormState(dupdlg);

                                if (dupdlg.DialogResult == DialogResult.Cancel)
                                    goto ERROR1;

                                strConfirmItemRecPath = dupdlg.SelectedRecPath;

                                goto REDO;
                            }
                            finally
                            {
                                this.MainForm.EndSearch();
                            }
                        }

                        goto ERROR1;
                    } // end of return -1

            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 0;
        ERROR1:
            return -1;
        }

        private void button_repairItemSide_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = RepairError(
                "repairitemside",
                this.textBox_readerBarcode.Text,
                this.textBox_itemBarcode.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            MessageBox.Show(this, "�޸��ɹ���");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �����Ӳ�۸�
        private void button_batchAddItemPrice_Click(object sender, EventArgs e)
        {
            string strError = "";


            if (this.checkBox_overwriteExistPrice.Checked == true)
            {
                DialogResult result = MessageBox.Show(this,
    "ȷʵҪ�����Ѿ����ڵļ۸��ַ���? ����һ���ܲ�ƽ���Ĳ�����",
    "CheckBorrowInfoForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    MessageBox.Show(this, "��������");
                    return;
                }
            }

            // List<string> barcodes = null;
            string strBarcodeFilename = Path.GetTempFileName();

            try
            {
                int nRet = SearchAllItemBarcode(strBarcodeFilename,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                nRet = BatchAddItemPrice(strBarcodeFilename,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            finally
            {
                File.Delete(strBarcodeFilename);
            }

            MessageBox.Show(this, "OK");

            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        int BatchAddItemPrice(string strBarcodeFilename,
            out string strError)
        {
            strError = "";

            StreamReader sr = null;

            try
            {
                sr = new StreamReader(strBarcodeFilename, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                strError = "���ļ� " + strBarcodeFilename + " ʧ��: " + ex.Message;
                return -1;
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���������Ӳ�۸� ...");
            stop.BeginLoop();

            EnableControls(false);

            try
            {
                int nCount = 0;

                Global.ClearForPureTextOutputing(this.webBrowser_resultInfo);

                for (int i = 0; ; i++)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null && stop.State != 0)
                    {
                        strError = "�û��ж�";
                        goto ERROR1;
                    }

                    // string strItemBarcode = barcodes[i];
                    string strItemBarcode = sr.ReadLine();

                    if (strItemBarcode == null)
                        break;

                    if (String.IsNullOrEmpty(strItemBarcode) == true)
                        continue;

                    stop.SetMessage("���ڼ��� " + (i + 1).ToString() + " �����¼������Ϊ " + strItemBarcode);

                    int nRedoCount = 0;
                REDO:

                    // �����Ŀ��¼·��
                    string strBiblioRecPath = "";
                    string strItemRecPath = "";
                    byte[] item_timestamp = null;

                    string strItemXml = "";
                    string strBiblioText = "";

                    // Result.Value -1���� 0û���ҵ� 1�ҵ� >1���ж���1��
                    long lRet = Channel.GetItemInfo(
                        stop,
                        strItemBarcode,
                        "xml",   // strResultType
                        out strItemXml,
                        out strItemRecPath,
                        out item_timestamp,
                        "recpath",  // strBiblioType
                        out strBiblioText,
                        out strBiblioRecPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "�����Ŀ��¼��·����������: " + strError;

                        Global.WriteHtml(this.webBrowser_resultInfo,
                            "�����¼ " + strItemBarcode + " ʱ����(1): " + strError + "\r\n");
                        continue;
                    }

                    if (lRet == 0)
                    {
                        strError = "������� " + strItemBarcode + " ��Ӧ��XML����û���ҵ���";
                        Global.WriteHtml(this.webBrowser_resultInfo,
                           "�����¼ " + strItemBarcode + " ʱ����(2): " + strError + "\r\n");
                        continue;
                    }

                    if (lRet > 1)
                    {
                        strError = "������� " + strItemBarcode + " ��Ӧ���ݶ���һ����";
                        Global.WriteHtml(this.webBrowser_resultInfo,
                           "�����¼ " + strItemBarcode + " ʱ����(3): " + strError + "\r\n");
                        continue;
                    }

                    XmlDocument itemdom = new XmlDocument();
                    try
                    {
                        itemdom.LoadXml(strItemXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "���¼װ��DOMʧ��: " + ex.Message;
                        Global.WriteHtml(this.webBrowser_resultInfo,
                            "�����¼ " + strItemBarcode + " ʱ����(4): " + strError + "\r\n");
                        continue;
                    }

                    // ���Ϊ׷��
                    if (this.checkBox_overwriteExistPrice.Checked == false)
                    {
                        // �������¼���Ƿ��Ѿ����˼۸���Ϣ?
                        if (HasPrice(itemdom) == true)
                            continue;
                    }

                    // ���biblio part price
                    string strPartName = "@price";
                    string strResultValue = "";

                    // Result.Value -1���� 0û���ҵ� 1�ҵ�
                    lRet = Channel.GetBiblioInfo(
                        stop,
                        strBiblioRecPath,
                        "", // strBiblioXml
                        strPartName,    // ����'@'����
                        out strResultValue,
                        out strError);
                    if (lRet == -1)
                    {
                        Global.WriteHtml(this.webBrowser_resultInfo,
                            "�����¼ " + strItemBarcode + " ʱ����(5): " + strError + "\r\n");
                        continue;
                    }

                    if (lRet == 0)
                    {
                        strError = "��Ŀ���� '"+strBiblioRecPath+"' ��û�м۸���Ϣ";
                        Global.WriteHtml(this.webBrowser_resultInfo,
                            "�����¼ " + strItemBarcode + " ʱ����(5): " + strError + "\r\n");
                        continue;
                    }

                    // �淶���۸��ַ���
                    string strPrice = CanonicalizePrice(strResultValue,
                        this.checkBox_forceCNY.Checked);

                    // ����۸���Ϣ
                    int nRet = AddPrice(ref itemdom,
                        strPrice,
                        out strError);
                    if (nRet == -1)
                    {
                        Global.WriteHtml(this.webBrowser_resultInfo,
                            "�����¼ " + strItemBarcode + " ʱ����(6): " + strError + "\r\n");
                        continue;
                    }

                    // ������¼

                    EntityInfo[] entities = new EntityInfo[1];
                    EntityInfo info = new EntityInfo();

                    info.RefID = Guid.NewGuid().ToString(); // 2008/4/14 
                    info.Action = "change";
                    info.OldRecPath = strItemRecPath;    // 2007/6/2 
                    info.NewRecPath = strItemRecPath;

                    info.NewRecord = itemdom.OuterXml;
                    info.NewTimestamp = null;

                    info.OldRecord = strItemXml;
                    info.OldTimestamp = item_timestamp;
                    entities[0] = info;

                    EntityInfo[] errorinfos = null;

                    lRet = Channel.SetEntities(
                        stop,
                        strBiblioRecPath,
                        entities,
                        out errorinfos,
                        out strError);
                    if (lRet == -1)
                    {
                        Global.WriteHtml(this.webBrowser_resultInfo,
    "�����¼ " + strItemBarcode + " ʱ����(7): " + strError + "\r\n");
                        continue;
                    }

                    {
                        // ���ʱ�����ƥ�䣿����
                        if (errorinfos != null
                            && errorinfos.Length == 1)
                        {
                            // ������Ϣ����
                            if (errorinfos[0].ErrorCode == ErrorCodeValue.NoError)
                            {
                            }
                            else if (errorinfos[0].ErrorCode == ErrorCodeValue.TimestampMismatch
                                && nRedoCount < 10)
                            {
                                nRedoCount++;
                                goto REDO;
                            }
                            else
                            {
                                Global.WriteHtml(this.webBrowser_resultInfo,
                                    "�����¼ " + strItemBarcode + " ʱ����(8): " + errorinfos[0].ErrorInfo + "\r\n");
                                continue;
                            }
                        }

                    }

                    if (this.checkBox_displayPriceString.Checked == true)
                    {
                        if (strResultValue != strPrice)
                        {
                            Global.WriteHtml(this.webBrowser_resultInfo,
                                strItemBarcode + ": " + strResultValue + " --> " + strPrice + " \r\n");
                        }
                        else
                        {
                            Global.WriteHtml(this.webBrowser_resultInfo,
                                strItemBarcode + ": " + strPrice + " \r\n");
                        }
                    }

                    nCount++;
                }


                Global.WriteHtml(this.webBrowser_resultInfo,
                    "����������������۸��ַ��� " + nCount.ToString() + " ����\r\n");


            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                if (sr != null)
                    sr.Close();
            }


            return 0;

        ERROR1:
            return -1;
        }

        static bool HasPrice(XmlDocument itemdom)
        {
            string strPrice = DomUtil.GetElementText(itemdom.DocumentElement,
                "price");

            if (String.IsNullOrEmpty(strPrice) == true)
                return false;

            return true;
        }

        static int AddPrice(ref XmlDocument itemdom,
            string strPrice,
            out string strError)
        {
            strError = "";

            DomUtil.SetElementText(itemdom.DocumentElement,
                "price",
                strPrice);

            return 0;
        }

        /*
~~~~~~~
    ��ɽʦԺ������Դ�࣬��ǰ���ּ۸��ֶθ�ʽ��¼��ʽ�������С�CNY25.00Ԫ����
��25.00��������25.00Ԫ��������25.00������CNY25.00������cny25.00������25.00
Ԫ���ȵȣ���������ȷ���Ժ�ȫ���á�CNY25.00����ʽ��¼��
    CALIS�У�����ظ�010$d�����۸�ʵ¼�ͻ������������ּ۸����ԣ�������ɽ
ʦԺҲ�������Ĵ����ظ��۸����ֶε����ݡ�
    Ϊʡ�ɱ�������������Ϣ�༭���У�����ֻ��һ���۸��ֶΣ���Ķ����ܣ����
û�м۸��ֶΣ���ת��Ϊ�ն����㣩��
    ת��ʱ���Ƿ���Լ�˵�������ȫ������������硰����.��������С����������
ȫ�⵫���ѡ�����Ӣ�ı���硰������

~~~~
�����裺
1) ȫ���ַ�ת��Ϊ���
2) ��������ֲ���
3) �۲�ǰ׺���ߺ�׺�������CNY cny �� Ԫ������������ȷ��Ϊ����ҡ�
ǰ׺�ͺ�׺��ȫΪ�գ�Ҳ��ȷ��Ϊ����ҡ�
���򣬱���ԭ����ǰ׺��         * */
        // ���滯�۸��ַ���
        static string CanonicalizePrice(string strPrice,
            bool bForceCNY)
        {
            // ȫ���ַ��任Ϊ���
            strPrice = Global.ConvertQuanjiaoToBanjiao(strPrice);

            if (bForceCNY == true)
            {
                // ��ȡ��������
                string strPurePrice = PriceUtil.GetPurePrice(strPrice);

                return "CNY" + strPurePrice;
            }

            string strPrefix = "";
            string strValue = "";
            string strPostfix = "";
            string strError = "";

            int nRet = PriceUtil.ParsePriceUnit(strPrice,
                out strPrefix,
                out strValue,
                out strPostfix,
                out strError);
            if (nRet == -1)
                return strPrice;    // �޷�parse

            bool bCNY = false;
            strPrefix = strPrefix.Trim();
            strPostfix = strPostfix.Trim();

            if (String.IsNullOrEmpty(strPrefix) == true
                && String.IsNullOrEmpty(strPostfix) == true)
            {
                bCNY = true;
                goto DONE;
            }


            if (strPrefix.IndexOf("CNY") != -1 
                || strPrefix.IndexOf("cny") != -1
                || strPrefix.IndexOf("�ãΣ�") != -1
                || strPrefix.IndexOf("����") != -1
                || strPrefix.IndexOf('��') != -1)
            {
                bCNY = true;
                goto DONE;
            }

            if (strPostfix.IndexOf("Ԫ") != -1)
            {
                bCNY = true;
                goto DONE;
            }

            DONE:
                // �����
                if (bCNY == true)
                    return "CNY" + strValue;

            // ��������
                return strPrefix + strValue + strPostfix;

        }

        private void button_clearInfo_Click(object sender, EventArgs e)
        {
            Global.ClearForPureTextOutputing(this.webBrowser_resultInfo);
        }

        private void button_single_checkFromItem_Click(object sender, EventArgs e)
        {
            string strError = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڽ��м�� ...");
            stop.BeginLoop();

            EnableControls(false);

            string[] aDupPath = null;
            string strText = "";
            try
            {
                    string strItemBarcode = this.textBox_single_itemBarcode.Text;
                    string strOutputReaderBarcode = "";

                    int nProcessedBorrowItems = 0;
                    int nTotalBorrowItems = 0;

                    long lRet = Channel.RepairBorrowInfo(
                        stop,
                        "checkfromitem",
                        "",
                        strItemBarcode,
                        "",
                        0,
                        -1,
                        out nProcessedBorrowItems,   // 2008/10/27 
                        out nTotalBorrowItems,   // 2008/10/27 
                        out strOutputReaderBarcode,
                        out aDupPath,
                        out strError);
                    if (lRet == -1 || lRet == 1)
                    {
                        if (Channel.ErrorCode == ErrorCode.ItemBarcodeDup)
                        {
                            List<string> linkedPath = new List<string>();

                            strText += "�����¼ " + strItemBarcode + " ʱ���ֲ�����������ظ���¼ " + aDupPath.Length.ToString() + "�����������\r\n";

                            for (int j = 0; j < aDupPath.Length; j++)
                            {
                                strText += " �� " + (j + 1).ToString() + " ����·��Ϊ " + aDupPath[j] + " \r\n";

                                string[] aDupPathTemp = null;
                                long lRet_2 = Channel.RepairBorrowInfo(
                                    stop,
                                    "checkfromitem",
                                    "",
                                    strItemBarcode,
                                    aDupPath[j],
                        0,
                        -1,
                        out nProcessedBorrowItems,   // 2008/10/27 
                        out nTotalBorrowItems,   // 2008/10/27 
                                    out strOutputReaderBarcode,
                                    out aDupPathTemp,
                                    out strError);
                                if (lRet_2 == -1)
                                {
                                    goto ERROR1;
                                }
                                if (lRet_2 == 1)
                                {
                                    strText += "  ��������: " + strError + "\r\n";
                                }

                            } // end of for

                            goto END1;
                        }

                        if (lRet == -1)
                        {
                            strText += "�����¼ " + strItemBarcode + " ʱ����: " + strError + "\r\n";
                        }
                        if (lRet == 1)
                        {
                            strText += "�����¼ " + strItemBarcode + " ʱ��������: " + strError + "\r\n";
                        }
                        goto END1;
                    } // end of return -1 or 1

                    if (lRet == -1)
                    {
                        strText += "�����¼ " + strItemBarcode + " ʱ����: " + strError + "\r\n";
                    }
                    if (lRet == 1)
                    {
                        strText += "�����¼ " + strItemBarcode + " ʱ��������: " + strError + "\r\n";
                    }


                    if (string.IsNullOrEmpty(strItemBarcode) == false)
                    {
                        string[] paths = null;
                        /*
                        lRet = Channel.SearchItemDup(stop,
                            strItemBarcode,
                            100,
                            out paths,
                            out strError);
                         * */
                        lRet = SearchEntityBarcode(stop,
                            strItemBarcode,
                            out paths,
                            out strError);
                        if (lRet == -1)
                        {
                            strText += "�Բ������ " + strItemBarcode + " ����ʱ����: " + strError + "\r\n";
                        }
                        if (lRet > 1)
                        {
                            strText += "������� " + strItemBarcode + " ���ظ���¼ " + paths.Length.ToString() + "��\r\n";
                        }
                    }


            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            END1:
            if (strText == "")
                MessageBox.Show(this, "û�з������⡣");
            else
                MessageBox.Show(this, strText);
            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }

        int SearchEntityBarcode(
            Stop stop,
            string strBarcode,
            out string[] paths,
            out string strError)
        {
            strError = "";
            paths = null;

            if (string.IsNullOrEmpty(strBarcode) == true)
            {
                strError = "��Ӧ�ò������Ϊ��������";
                return -1;
            }

            long lRet = Channel.SearchItem(
stop,
"<ȫ��>",
strBarcode,
100,
"�������",
"exact",
"zh",
"dup",
"", // strSearchStyle
"", // strOutputStyle
out strError);
            if (lRet == -1)
                return -1;  // error

            if (lRet == 0)
                return 0;   // not found

            long lHitCount = lRet;

            List<string> aPath = null;
            lRet = Channel.GetSearchResult(stop,
                "dup",
                0,
                Math.Min(lHitCount, 100),
                "zh",
                out aPath,
                out strError);
            if (lRet == -1)
                return -1;

            paths = new string[aPath.Count];
            aPath.CopyTo(paths);

            return (int)lHitCount;
        }


        private void CheckBorrowInfoForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);
        }

        private void button_single_checkFromReader_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.textBox_single_readerBarcode.Text == "")
            {
                strError = "��δָ��Ҫ���Ķ���֤�����";
                goto ERROR1;
            }

            List<string> barcodes = new List<string>();
            barcodes.Add(this.textBox_single_readerBarcode.Text);

            nRet = CheckAllReaderRecord(barcodes,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
    }
}