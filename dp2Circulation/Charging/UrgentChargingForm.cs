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
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.IO;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;

namespace dp2Circulation
{
    /// <summary>
    /// Ӧ�����ɴ�
    /// </summary>
    public partial class UrgentChargingForm : MyForm
    {
#if NO
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";
        DigitalPlatform.Stop stop = null;

#endif

        FuncState m_funcstate = FuncState.Borrow;

        // ������������Ĳ������
        List<BarcodeAndTime> m_itemBarcodes = new List<BarcodeAndTime>();

        const int WM_PREPARE = API.WM_USER + 200;
        const int WM_SCROLLTOEND = API.WM_USER + 201;
        const int WM_SWITCH_FOCUS = API.WM_USER + 202;

        Hashtable m_textTable = new Hashtable();
        int m_nTextNumber = 0;


        // ��ϢWM_SWITCH_FOCUS��wparam����ֵ
        const int READER_BARCODE = 0;
        // const int READER_PASSWORD = 1;
        const int ITEM_BARCODE = 2;

        /// <summary>
        /// ���캯��
        /// </summary>
        public UrgentChargingForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// �Ƿ�Ҫ�Զ�У�������
        /// </summary>
        public bool NeedVerifyBarcode
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "verify_barcode",
                    false);
            }
        }

        /// <summary>
        /// ��Ϣ�Ի���Ĳ�͸����
        /// </summary>
        public double InfoDlgOpacity
        {
            get
            {
                return (double)this.MainForm.AppInfo.GetInt(
                    "charging_form",
                    "info_dlg_opacity",
                    100) / (double)100;
            }
        }

        bool DoubleItemInputAsEnd
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "doubleItemInputAsEnd",
                    false);
            }

        }

        private void UrgentChargingForm_Load(object sender, EventArgs e)
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

            this.MainForm.Urgent = true;

            this.FuncState = this.FuncState;    // ʹ"����"��ť������ʾ��ȷ
            Global.WriteHtml(this.webBrowser_operationInfo,
                "<pre>");
            EnableControls(false);

            API.PostMessage(this.Handle, WM_PREPARE, 0, 0);

        }

#if NO
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            MainForm.Channel_BeforeLogin(this, e);
        }
#endif

        private void UrgentChargingForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void UrgentChargingForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.MainForm != null) 
                this.MainForm.Urgent = false;
        }

        string LogFileName
        {
            get
            {
                return this.MainForm.DataDir + "\\urgent_charging.txt";
            }
        }

        // д����־�ļ�
        // ��ʽ������ ����֤����� ������� ����ʱ��
        int WriteLogFile(string strFunc,
            string strReaderBarcode,
            string strItemBarcode,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strFunc) == true)
            {
                strError = "strFunc����Ϊ��";
                return -1;
            }

            if (String.IsNullOrEmpty(strReaderBarcode) == true
                && String.IsNullOrEmpty(strItemBarcode) == true)
            {
                strError = "strReaderBarcode��strItemBarcode����ͬʱΪ��";
                return -1;
            }

            string strTime = DateTimeUtil.Rfc1123DateTimeString(DateTime.UtcNow);

            string strLine = strFunc + "\t" + strReaderBarcode + "\t" + strItemBarcode + "\t" + strTime + "\r\n";

            StreamUtil.WriteText(this.LogFileName,
                strLine);

            Global.WriteHtml(this.webBrowser_operationInfo,
                strLine);
            Global.ScrollToEnd(this.webBrowser_operationInfo);


            return 0;
        }

        private void button_loadReader_Click(object sender, EventArgs e)
        {
            if (this.textBox_readerBarcode.Text == "")
            {
                MessageBox.Show(this, "����֤�����Ϊ�ա�");

                this.textBox_readerBarcode.SelectAll();
                this.textBox_readerBarcode.Focus();

                return;
            }

            this.textBox_itemBarcode.SelectAll();
            this.textBox_itemBarcode.Focus();
        }

        private void button_itemAction_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.textBox_itemBarcode.Text == "")
            {
                strError = "������Ų���Ϊ�ա�";
                goto ERROR1;
            }

            if (this.DoubleItemInputAsEnd == true)
            {

                // ȡ���ϴ���������һ�����룬��Ŀǰ���������Ƚϣ����Ƿ�һ����
                if (this.m_itemBarcodes.Count > 0)
                {
                    string strLastItemBarcode = this.m_itemBarcodes[m_itemBarcodes.Count - 1].Barcode;
                    TimeSpan delta = DateTime.Now - this.m_itemBarcodes[m_itemBarcodes.Count - 1].Time;
                    // MessageBox.Show(this, delta.TotalMilliseconds.ToString());
                    if (strLastItemBarcode == this.textBox_itemBarcode.Text
                        && delta.TotalMilliseconds < 5000) // 5������
                    {
                        // ����������������
                        this.textBox_itemBarcode.Text = "";
                        // �������֤�����������
                        this.textBox_readerBarcode.Text = "��������һ�����ߵ�֤�����...";
                        this.SwitchFocus(READER_BARCODE, null);
                        return;
                    }
                }


                BarcodeAndTime barcodetime = new BarcodeAndTime();
                barcodetime.Barcode = this.textBox_itemBarcode.Text;
                barcodetime.Time = DateTime.Now;

                this.m_itemBarcodes.Add(barcodetime);
                // ��������һ������Ϳ�����
                while (this.m_itemBarcodes.Count > 1)
                    this.m_itemBarcodes.RemoveAt(0);
            }

            if (this.FuncState == FuncState.Borrow)
            {
                if (this.textBox_readerBarcode.Text == "")
                {
                    strError = "����֤����Ų���Ϊ�ա�";
                    goto ERROR1;
                }


                nRet = this.WriteLogFile("borrow",
                    this.textBox_readerBarcode.Text,
                    this.textBox_itemBarcode.Text,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                string strFastInputText = ChargingInfoDlg.Show(this.CharingInfoHost,
    "���� " + this.textBox_readerBarcode.Text
    + " ���Ĳ� "
    + this.textBox_itemBarcode.Text + " �ɹ���",
    InfoColor.Green,
    "caption",
    this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                this.SwitchFocus(ITEM_BARCODE, strFastInputText);
            }

            if (this.FuncState == FuncState.Return)
            {
                nRet = this.WriteLogFile("return",
    this.textBox_readerBarcode.Text,
    this.textBox_itemBarcode.Text,
    out strError);
                if (nRet == -1)
                    goto ERROR1;

                string strFastInputText = ChargingInfoDlg.Show(this.CharingInfoHost,
" �� "
+ this.textBox_itemBarcode.Text + " ���سɹ���",
InfoColor.Green,
"caption",
this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                this.SwitchFocus(ITEM_BARCODE, strFastInputText);

            }

            if (this.FuncState == FuncState.VerifyReturn)
            {
                if (this.textBox_readerBarcode.Text == "")
                {
                    strError = "����֤����Ų���Ϊ�ա�";
                    goto ERROR1;
                }

                nRet = this.WriteLogFile("return",
    this.textBox_readerBarcode.Text,
    this.textBox_itemBarcode.Text,
    out strError);
                if (nRet == -1)
                    goto ERROR1;

                string strFastInputText = ChargingInfoDlg.Show(this.CharingInfoHost,
"���� " + this.textBox_readerBarcode.Text
+ " ���ز� "
+ this.textBox_itemBarcode.Text + " �ɹ���",
InfoColor.Green,
"caption",
this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                this.SwitchFocus(ITEM_BARCODE, strFastInputText);

            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);


        }

        // ���н����л����ܵ�
        /// <summary>
        /// ��ǰ����״̬
        /// </summary>
        public FuncState SmartFuncState
        {
            get
            {
                return m_funcstate;
            }
            set
            {
                FuncState old_funcstate = this.m_funcstate;

                this.FuncState = value;

                // �л�Ϊ��ͬ�Ĺ��ܵ�ʱ�򣬶�λ����
                if (old_funcstate != this.m_funcstate)
                {
                    if (this.m_funcstate != FuncState.Return)
                    {
                        this.textBox_readerBarcode.SelectAll();
                        this.textBox_itemBarcode.SelectAll();

                        this.textBox_readerBarcode.Focus();
                    }
                    else
                    {
                        this.textBox_itemBarcode.SelectAll();

                        this.textBox_itemBarcode.Focus();
                    }
                }
                else // �ظ�����Ϊͬ�����ܣ������������
                {
                    this.textBox_readerBarcode.Text = "";
                    this.textBox_itemBarcode.Text = "";

                    if (this.m_funcstate != FuncState.Return)
                    {
                        this.textBox_readerBarcode.Focus();
                    }
                    else
                    {
                        this.textBox_itemBarcode.Focus();
                    }

                }
            }
        }

        FuncState FuncState
        {
            get
            {
                return m_funcstate;
            }
            set
            {
                // �������Ĳ������
                this.m_itemBarcodes.Clear();

                if (this.m_funcstate != value
                    && value == FuncState.Return)
                    MessageBox.Show(this, "���棺ʹ�ò�����֤����֤����ŵĻ��ع��ܣ���Ӱ���պ����ݿ�ָ������ݿ�ʱ���ݴ������������ô˹��ܡ�");


                this.m_funcstate = value;

                this.toolStripMenuItem_borrow.Checked = false;
                this.toolStripMenuItem_return.Checked = false;
                this.toolStripMenuItem_verifyReturn.Checked = false;

                if (m_funcstate == FuncState.Borrow)
                {
                    this.button_itemAction.Text = "��";
                    this.toolStripMenuItem_borrow.Checked = true;
                    this.textBox_readerBarcode.Enabled = true;
                }
                if (m_funcstate == FuncState.Return)
                {
                    this.button_itemAction.Text = "��";
                    this.toolStripMenuItem_return.Checked = true;
                    this.textBox_readerBarcode.Text = "";
                    this.textBox_readerBarcode.Enabled = false;
                }
                if (m_funcstate == FuncState.VerifyReturn)
                {
                    this.button_itemAction.Text = "��֤��";
                    this.toolStripMenuItem_verifyReturn.Checked = true;
                    this.textBox_readerBarcode.Enabled = true;
                }

            }
        }

        private void toolStripMenuItem_borrow_Click(object sender, EventArgs e)
        {
            this.FuncState = FuncState.Borrow;
        }

        private void toolStripMenuItem_return_Click(object sender, EventArgs e)
        {
            this.FuncState = FuncState.Return;
        }

        private void toolStripMenuItem_verifyReturn_Click(object sender, EventArgs e)
        {
            this.FuncState = FuncState.VerifyReturn;
        }


        private void textBox_readerBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_loadReader;
        }

        private void textBox_readerPassword_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_verifyReaderPassword;
        }

        private void textBox_itemBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_itemAction;
        }


        // ���ļ���װ�����ݵ������
        int LoadLogFileContentToBrowser(out string strError)
        {
            strError = "";

            string strLogFileName = this.LogFileName;
            int nLineCount = 0;
            try
            {
                StreamReader sr = new StreamReader(strLogFileName, true);
                for (; ; )
                {
                    string strLine = sr.ReadLine();
                    if (strLine == null)
                        break;
                    Global.WriteHtml(this.webBrowser_operationInfo,
        strLine + "\r\n");
                    nLineCount++;
                }
                sr.Close();

            }
            catch (FileNotFoundException)
            {
                return 0;
            }
            catch (Exception ex)
            {
                strError = "��ȡ�ļ����̳���: " + ex.Message;
                return -1;
            }

            Global.WriteHtml(this.webBrowser_operationInfo,
                "--- ��ǰ " + nLineCount + " ��ΪӦ����־�ļ� " + this.LogFileName + " ���Ѿ��洢������ ---\r\n");

            // Global.ScrollToEnd(this.webBrowser_operationInfo);
            API.PostMessage(this.Handle, WM_SCROLLTOEND, 0, 0);


            return 0;
        }

        void SwitchFocus(int target,
    string strFastInput)
        {
            // ���hashtableԽ��Խ��
            if (this.m_textTable.Count > 5)
            {
                Debug.Assert(false, "");
                this.m_textTable.Clear();
            }

            int nNumber = -1;   // -1��ʾ����Ҫ�����ַ�������

            // �����Ҫ�����ַ�������
            if (String.IsNullOrEmpty(strFastInput) == false)
            {
                string strNumber = this.m_nTextNumber.ToString();
                nNumber = this.m_nTextNumber;
                this.m_nTextNumber++;
                if (this.m_nTextNumber == -1)   // �ܿ�-1
                    this.m_nTextNumber++;

                this.m_textTable[strNumber] = strFastInput;
            }

            API.PostMessage(this.Handle, WM_SWITCH_FOCUS,
                target, nNumber);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_PREPARE:
                    {
                        string strError = "";

                        int nRet = LoadLogFileContentToBrowser(out strError);
                        if (nRet == -1)
                            MessageBox.Show(this, strError);

                        // Ȼ����ɽ���
                        EnableControls(true);
                        return;
                    }
                //break;
                case WM_SCROLLTOEND:
                    Global.ScrollToEnd(this.webBrowser_operationInfo);
                    break;
                case WM_SWITCH_FOCUS:
                    {
                        string strFastInputText = "";
                        int nNumber = (int)m.LParam;

                        if (nNumber != -1)
                        {
                            string strNumber = nNumber.ToString();
                            strFastInputText = (string)this.m_textTable[strNumber];
                            this.m_textTable.Remove(strNumber);
                        }

                        if (String.IsNullOrEmpty(strFastInputText) == false)
                        {
                            if ((int)m.WParam == READER_BARCODE)
                            {
                                if (this.FuncState == FuncState.Return)
                                    this.FuncState = FuncState.Borrow;

                                this.textBox_readerBarcode.Text = strFastInputText;
                                this.button_loadReader_Click(this, null);
                            }
                            if ((int)m.WParam == ITEM_BARCODE)
                            {
                                this.textBox_itemBarcode.Text = strFastInputText;
                                this.button_itemAction_Click(this, null);
                            }

                            return;
                        }

                        if ((int)m.WParam == READER_BARCODE)
                        {
                            if (this.FuncState == FuncState.Return)
                                this.FuncState = FuncState.Borrow;

                            this.textBox_readerBarcode.SelectAll();
                            this.textBox_readerBarcode.Focus();
                        }

                        if ((int)m.WParam == ITEM_BARCODE)
                        {
                            this.textBox_itemBarcode.SelectAll();
                            this.textBox_itemBarcode.Focus();
                        }

                        return;
                    }
                // break;

            }
            base.DefWndProc(ref m);
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.textBox_itemBarcode.Enabled = bEnable;
            this.textBox_readerBarcode.Enabled = bEnable;

            this.button_itemAction.Enabled = bEnable;
            this.button_loadReader.Enabled = bEnable;
        }

        private void UrgentChargingForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            this.MainForm.toolButton_amerce.Enabled = false;
            /*
            this.toolButton_borrow.Enabled = true;
            this.toolButton_return.Enabled = true;
            this.MainForm.toolButton_verifyReturn.Enabled = true;
             * */
            this.MainForm.toolButton_lost.Enabled = false;
            this.MainForm.toolButton_readerManage.Enabled = false;
            this.MainForm.toolButton_renew.Enabled = false;

            this.MainForm.toolStripDropDownButton_barcodeLoadStyle.Enabled = false;
            this.MainForm.toolStripTextBox_barcode.Enabled = false;

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = true;

            this.MainForm.Urgent = true;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

#if NO
        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }
#endif

        // 
        /// <summary>
        /// �ָ�Ӧ����־�ļ���������
        /// </summary>
        public void Recover()
        {
            string strError = "";
            int nRet = 0;

            string strLogFileName = this.LogFileName;
            int nLineCount = 0;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڳ�ʼ���������� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            EnableControls(false);

            Global.WriteHtml(this.webBrowser_operationInfo,
    "��ʼ�ָ���\r\n");


            try
            {
                StreamReader sr = new StreamReader(strLogFileName, true);
                for (; ; )
                {
                    string strLine = sr.ReadLine();
                    if (strLine == null)
                        break;

                    if (String.IsNullOrEmpty(strLine) == true)
                        continue;

                    string strXml = "";
                    nRet = BuildRecoverXml(
                        strLine,
                        out strXml,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    long lRet = this.Channel.UrgentRecover(
                        stop,
                        strXml,
                        out strError);
                    if (lRet == -1)
                    {
                        DialogResult result = MessageBox.Show(this,
"��\r\n" + strLine + "\r\n�ָ������ݿ�ʱ����" + strError + "��\r\n\r\nҪ�жϴ���ô? ",
"UrgentChargingForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                        if (result == DialogResult.Yes)
                            goto ERROR1;

                        Global.WriteHtml(this.webBrowser_operationInfo,
strLine + " *** error: " + strError + "\r\n");
                        goto CONTINUE_1;
                    }

                    Global.WriteHtml(this.webBrowser_operationInfo,
        strLine + "\r\n");
                CONTINUE_1:
                    Global.ScrollToEnd(this.webBrowser_operationInfo);

                    nLineCount++;
                }
                sr.Close();

            }
            catch (FileNotFoundException)
            {
                strError = "�ļ� " + strLogFileName + "�����ڡ�";
                goto ERROR1;
            }
            catch (Exception ex)
            {
                strError = "��ȡ�ļ����̳���: " + ex.Message;
                goto ERROR1;
            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            Global.WriteHtml(this.webBrowser_operationInfo,
                "�ָ���ɡ��������¼ " + nLineCount + " ���� \r\n");
            Global.WriteHtml(this.webBrowser_operationInfo,
    "ע�������Ŀ¼���������� " + this.LogFileName + " �ļ������⽫����С���ظ��ָ���\r\n");

            API.PostMessage(this.Handle, WM_SCROLLTOEND, 0, 0);

            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        /*
<root>
  <operation>borrow</operation> ��������
  <readerBarcode>R0000002</readerBarcode> ����֤�����
  <itemBarcode>0000001</itemBarcode>  �������
  <borrowDate>Fri, 08 Dec 2006 04:17:31 GMT</borrowDate> ��������
  <borrowPeriod>30day</borrowPeriod> ��������
  <no>0</no> ���������0Ϊ�״���ͨ���ģ�1��ʼΪ����
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 04:17:31 GMT</operTime> ����ʱ��
  <confirmItemRecPath>...</confirmItemRecPath> �����ж��õĲ��¼·��
  
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
  <itemRecord recPath='...'>...</itemRecord>	���²��¼
</root>
         * 
         * 
 <root>
  <operation>return</operation> ��������
  <action>...</action> ���嶯�� ��return lost����
  <itemBarcode>0000001</itemBarcode> �������
  <readerBarcode>R0000002</readerBarcode> ����֤�����
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 04:17:45 GMT</operTime> ����ʱ��
  <overdues>...</overdues> ���ڻ�ʧ�����Ϣ ͨ������Ϊһ���ַ�����Ϊһ������<overdue>Ԫ��XML�ı�Ƭ��
  
  <confirmItemRecPath>...</confirmItemRecPath> �����ж��õĲ��¼·��
  
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
  <itemRecord recPath='...'>...</itemRecord>	���²��¼
  <lostComment>...</lostComment> ���ڶ�ʧ����ĸ�ע(׷��д����¼<comment>����Ϣ)
</root>
         * * */
        int BuildRecoverXml(
            string strLine,
            out string strXml,
            out string strError)
        {
            strXml = "";
            strError = "";

            string[] cols = strLine.Split(new char[] { '\t' });


            if (cols.Length < 4)
            {
                strError = "strLine[" + strLine + "]��ʽ����ȷ��ӦΪ4�����ݡ�";
                return -1;
            }

            string strFunction = cols[0];
            string strReaderBarcode = cols[1];
            string strItemBarcode = cols[2];
            string strOperTime = cols[3];

            string strUserName =
this.MainForm.AppInfo.GetString(
"default_account",
"username",
"");

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            if (strFunction == "borrow")
            {
                DomUtil.SetElementText(dom.DocumentElement,
                    "operation",
                    "borrow");

                DomUtil.SetElementText(dom.DocumentElement,
    "readerBarcode",
    strReaderBarcode);

                DomUtil.SetElementText(dom.DocumentElement,
    "itemBarcode",
    strItemBarcode);

                // no
                DomUtil.SetElementText(dom.DocumentElement,
"no",
"0");

                // borrowDate
                DomUtil.SetElementText(dom.DocumentElement,
"borrowDate",
strOperTime);

                // defaultBorrowPeriod
                DomUtil.SetElementText(dom.DocumentElement,
"defaultBorrowPeriod",
"60day");
                // ע������³çд��<borrowPeriod>������д��<defaulBorrowPeriod>
                // ��Ϊ��Ҫ�����������ᣬ����̽�����������Բ����͵Ľ��ڲ�����ʵ�ڲ��вŲ������������ȱʡ������


                // operTime
                DomUtil.SetElementText(dom.DocumentElement,
"operTime",
strOperTime);



                // operator
                DomUtil.SetElementText(dom.DocumentElement,
"operator",
strUserName);

            }
            else if (strFunction == "return")
            {
                DomUtil.SetElementText(dom.DocumentElement,
    "operation",
    "return");
                DomUtil.SetElementText(dom.DocumentElement,
"action",
"return");

                // 2006/12/30 new add
                if (String.IsNullOrEmpty(strReaderBarcode) == false)
                {
                    DomUtil.SetElementText(dom.DocumentElement,
    "readerBarcode",
    strReaderBarcode);
                }


                DomUtil.SetElementText(dom.DocumentElement,
    "itemBarcode",
    strItemBarcode);

                // operTime
                DomUtil.SetElementText(dom.DocumentElement,
"operTime",
strOperTime);

                // operator
                DomUtil.SetElementText(dom.DocumentElement,
"operator",
strUserName);

            }
            else
            {
                strError = "����ʶ���function '" + strFunction + "'";
                return -1;
            }

            strXml = dom.OuterXml;

            return 0;
        }

        /// <summary>
        /// ��ó�����Ϣ������
        /// </summary>
        internal ChargingInfoHost CharingInfoHost
        {
            get
            {
                ChargingInfoHost host = new ChargingInfoHost();
                host.ap = MainForm.AppInfo;
                host.window = this;
                return host;
            }
        }
    }
}