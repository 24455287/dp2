using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using System.IO;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// ���ɲ�������
    /// </summary>
    public partial class ChargingForm : MyForm, IProtectFocus, IChargingForm
    {
        /// <summary>
        /// IProtectFocus �ӿ�Ҫ��ĺ���
        /// </summary>
        /// <param name="pfAllow">�Ƿ�����</param>
        public void AllowFocusChange(ref bool pfAllow)
        {
            pfAllow = false;
        }

        Commander commander = null;

        const int WM_LOAD_READER = API.WM_USER + 300;
        const int WM_LOAD_ITEM = API.WM_USER + 301;

        WebExternalHost m_webExternalHost_readerInfo = new WebExternalHost();
        WebExternalHost m_webExternalHost_itemInfo = new WebExternalHost();
        WebExternalHost m_webExternalHost_biblioInfo = new WebExternalHost();

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        // public ApplicationInfo ap = null;
        public string Lang = "zh";

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;
#endif

        string m_strCurrentBarcode = "";
        string m_strCurrentReaderBarcode = "";

        FuncState m_funcstate = FuncState.Borrow;

        DisplayState m_displaystate = DisplayState.TEXT;

        // ͬһ�������������ĳɹ������ۻ��Ĳ�����ż���
        List<string> oneReaderItemBarcodes = new List<string>();

        // bool m_bVerifyReaderPassword = false;

        // public AutoResetEvent eventReaderWebComplete = new AutoResetEvent(false);	// true : initial state is signaled 

        const int WM_SWITCH_FOCUS = API.WM_USER + 200;
        // const int WM_LOADSIZE = API.WM_USER + 201;
        const int WM_ENABLE_EDIT = API.WM_USER + 202;

        // ��ϢWM_SWITCH_FOCUS��wparam����ֵ
        /// <summary>
        /// ����λ���±꣺����֤�����������
        /// </summary>
        public const int READER_BARCODE = 0;
        /// <summary>
        /// ����λ���±꣺����֤����������
        /// </summary>
        public const int READER_PASSWORD = 1;
        /// <summary>
        /// ����λ���±꣺�������������
        /// </summary>
        public const int ITEM_BARCODE = 2;

        // string FastInputText = "";  // CharingInfoDlg���صĿ����������

        Hashtable m_textTable = new Hashtable();
        int m_nTextNumber = 0;

        // ������������Ĳ������
        List<BarcodeAndTime> m_itemBarcodes = new List<BarcodeAndTime>();

        /// <summary>
        /// ��ǰ��Ķ���֤�����
        /// </summary>
        public string ActiveReaderBarcode
        {
            get
            {
                return this.toolStripDropDownButton_readerBarcodeNavigate.Text;
            }
            set
            {
                this.toolStripDropDownButton_readerBarcodeNavigate.Text = value;

                if (String.IsNullOrEmpty(value) == true)
                {
                    this.ToolStripMenuItem_naviToAmerceForm.Enabled = false;
                    this.ToolStripMenuItem_naviToReaderInfoForm.Enabled = false;
                    this.ToolStripMenuItem_naviToActivateForm_old.Enabled = false;
                    this.ToolStripMenuItem_openReaderManageForm.Enabled = false;
                    this.ToolStripMenuItem_naviToActivateForm_new.Enabled = false;
                }
                else
                {
                    this.ToolStripMenuItem_naviToAmerceForm.Enabled = true;
                    this.ToolStripMenuItem_naviToReaderInfoForm.Enabled = true;
                    this.ToolStripMenuItem_naviToActivateForm_old.Enabled = true;
                    this.ToolStripMenuItem_openReaderManageForm.Enabled = true;
                    this.ToolStripMenuItem_naviToActivateForm_new.Enabled = true;
                }
            }
        }

        /// <summary>
        /// ��ǰ��Ĳ������
        /// </summary>
        public string ActiveItemBarcode
        {
            get
            {
                return this.toolStripDropDownButton_itemBarcodeNavigate.Text;
            }
            set
            {
                this.toolStripDropDownButton_itemBarcodeNavigate.Text = value;

                if (String.IsNullOrEmpty(value) == true)
                {
                    this.ToolStripMenuItem_openEntityForm.Enabled = false;
                    this.ToolStripMenuItem_openItemInfoForm.Enabled = false;
                }
                else
                {
                    this.ToolStripMenuItem_openEntityForm.Enabled = true;
                    this.ToolStripMenuItem_openItemInfoForm.Enabled = true;
                }
            }
        }

        // 
        /// <summary>
        /// ��ʾ������Ϣ�ĸ�ʽ��Ϊ text html ֮һ
        /// </summary>
        public string PatronRenderFormat
        {
            get
            {
                if (this.DisplayState == DisplayState.TEXT)
                    return "text";

                if (this.NoBorrowHistory == true && this.MainForm.Version >= 2.21)
                    return "html:noborrowhistory";

                return "html";
            }
        }

        /// <summary>
        /// ��ʾ��Ŀ������Ϣ�ĸ�ʽ��Ϊ text html ֮һ
        /// </summary>
        public string RenderFormat
        {
            get
            {
                if (this.DisplayState == DisplayState.TEXT)
                    return "text";
                return "html";
            }
        }

        /// <summary>
        /// ��ʾ״̬
        /// </summary>
        public DisplayState DisplayState
        {
            get
            {
                return this.m_displaystate;
            }
            set
            {
                this.m_displaystate = value;

                if (this.m_displaystate == DisplayState.TEXT)
                {
                    this.webBrowser_reader.Visible = false;
                    this.webBrowser_biblio.Visible = false;
                    this.webBrowser_item.Visible = false;

                    this.textBox_readerInfo.Visible = true;
                    this.textBox_biblioInfo.Visible = true;
                    this.textBox_itemInfo.Visible = true;

                    this.tableLayoutPanel_readerInfo.RowStyles[2].SizeType = SizeType.Percent;
                    this.tableLayoutPanel_readerInfo.RowStyles[2].Height = 1.0F;

                    this.tableLayoutPanel_biblioInfo.RowStyles[2].SizeType = SizeType.Percent;
                    this.tableLayoutPanel_biblioInfo.RowStyles[2].Height = 1.0F;

                    this.tableLayoutPanel_itemInfo.RowStyles[2].SizeType = SizeType.Percent;
                    this.tableLayoutPanel_itemInfo.RowStyles[2].Height = 1.0F;

                }
                if (this.m_displaystate == DisplayState.HTML)
                {
                    this.textBox_readerInfo.Visible = false;
                    this.textBox_biblioInfo.Visible = false;
                    this.textBox_itemInfo.Visible = false;

                    this.webBrowser_reader.Visible = true;
                    this.webBrowser_biblio.Visible = true;
                    this.webBrowser_item.Visible = true;

                    this.tableLayoutPanel_readerInfo.RowStyles[1].SizeType = SizeType.Percent;
                    this.tableLayoutPanel_readerInfo.RowStyles[1].Height = 1.0F;

                    this.tableLayoutPanel_biblioInfo.RowStyles[1].SizeType = SizeType.Percent;
                    this.tableLayoutPanel_biblioInfo.RowStyles[1].Height = 1.0F;

                    this.tableLayoutPanel_itemInfo.RowStyles[1].SizeType = SizeType.Percent;
                    this.tableLayoutPanel_itemInfo.RowStyles[1].Height = 1.0F;
                }
            }
        }

        /// <summary>
        /// �Ƿ�Ҫǿ�Ʋ���[��δʹ��]
        /// </summary>
        public bool Force
        {
            get
            {
                return false;   // 2008/10/29 new add
                /*
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "force",
                    false);
                 * */
            }
            set
            {

                this.MainForm.AppInfo.SetBoolean(
                    "charging_form",
                    "force",
                    value);
            }
        }


        // 
        // 2008/9/26
        /// <summary>
        /// �Ƿ��Զ���������������
        /// </summary>
        public bool AutoClearTextbox
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "autoClearTextbox",
                    true);
            }
            set
            {
                this.MainForm.AppInfo.SetBoolean(
                    "charging_form",
                    "autoClearTextbox",
                    value);
            }
        }

        /// <summary>
        /// �Ƿ� ����ʾ��Ŀ�Ͳ���Ϣ
        /// </summary>
        public bool NoBiblioAndItemInfo
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "no_biblio_and_item_info",
                    false);
            }

        }

        /// <summary>
        /// �Ƿ� ����ʾ��ɫ��ʾ�Ի���
        /// </summary>
        public bool GreenDisable
        {
            get
            {
                return
                this.MainForm.AppInfo.GetBoolean(
                "charging_form",
                "green_infodlg_not_occur",
                false);
            }
        }

        /// <summary>
        /// �Ƿ��Զ�У������������
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

        //
        bool AutoSwitchReaderBarcode
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "auto_switch_reader_barcode",
                    false);
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

        /// <summary>
        /// �ı䲼�ַ�ʽ
        /// </summary>
        /// <param name="bNoBiblioAndItemInfo">�Ƿ� ��Ҫ��ʾ��Ŀ�Ͳ���Ϣ����</param>
        public void ChangeLayout(bool bNoBiblioAndItemInfo)
        {
            if (bNoBiblioAndItemInfo == true)
            {
                // ��operation���ؼ�����biblioanditem�ƶ���readerinfo���ؼ���
                this.tableLayoutPanel_readerInfo.Controls.Add(this.tableLayoutPanel_operation,
                    0, 4);
                this.tableLayoutPanel_biblioAndItem.Controls.Remove(this.tableLayoutPanel_operation);
                // ��readerinfo���ؼ�(��main��panel1)��������߲�
                this.panel_main.Controls.Add(this.tableLayoutPanel_readerInfo); // Dock��������Fill

                this.splitContainer_main.Panel1.Controls.Remove(this.tableLayoutPanel_readerInfo);

                /*
                // ��readerinfo��Dock��ʽ�޸�ΪFill
                this.tableLayoutPanel_readerInfo.Dock = DockStyle.Fill;
                 * */

                // ����splitContainer_main
                this.splitContainer_main.Visible = false;
            }
            else
            {
                // ��operation���ؼ�����readerinfo���ؼ����ƶ���biblioanditem
                this.tableLayoutPanel_biblioAndItem.Controls.Add(this.tableLayoutPanel_operation,
                    0, 3);
                this.tableLayoutPanel_readerInfo.Controls.Remove(this.tableLayoutPanel_operation);

                // ��readerinfo���ؼ�����߲��ƶ���main��panel1
                this.splitContainer_main.Panel1.Controls.Add(this.tableLayoutPanel_readerInfo);
                this.panel_main.Controls.Remove(this.tableLayoutPanel_readerInfo);

                /*
                // ��readerinfo��Dock��ʽ�޸�ΪFill
                this.tableLayoutPanel_readerInfo.Dock = DockStyle.Fill;
                 * */

                // ��ʾsplitContainer_main
                this.splitContainer_main.Visible = true;
            }
        }

        private void ChargingForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

            if (this.NoBiblioAndItemInfo == true)
            {
                /*
                // ��operation���ؼ�����biblioanditem�ƶ���readerinfo���ؼ���
                this.tableLayoutPanel_readerInfo.Controls.Add(this.tableLayoutPanel_operation,
                    0, 4);
                this.tableLayoutPanel_biblioAndItem.Controls.Remove(this.tableLayoutPanel_operation);
                // ��readerinfo���ؼ�(��main��panel1)��������߲�
                this.Controls.Add(this.tableLayoutPanel_readerInfo);
                this.splitContainer_main.Panel1.Controls.Remove(this.tableLayoutPanel_readerInfo);

                // ��readerinfo��Dock��ʽ�޸�ΪFill
                this.tableLayoutPanel_readerInfo.Dock = DockStyle.Fill;

                // ����splitContainer_main
                this.splitContainer_main.Visible = false;
                 * */
                ChangeLayout(true);
            }

            this.MainForm.AppInfo.LoadMdiSize += new EventHandler(AppInfo_LoadMdiSize);
            this.MainForm.AppInfo.SaveMdiSize += new EventHandler(AppInfo_SaveMdiSize);

            // LoadSize();

            this.FuncState = this.FuncState;    // ʹ"����"��ť������ʾ��ȷ

            string strDisplayFormat =
                this.MainForm.AppInfo.GetString(
                "charging_form",
                "display_format",
                "HTML");
            if (strDisplayFormat == "HTML")
                this.DisplayState = DisplayState.HTML;
            else
                this.DisplayState = DisplayState.TEXT;

            // webbrowser
            this.m_webExternalHost_readerInfo.Initial(this.MainForm, this.webBrowser_reader);
            this.webBrowser_reader.ObjectForScripting = this.m_webExternalHost_readerInfo;

            // 2009/10/18 new add
            this.m_webExternalHost_itemInfo.Initial(this.MainForm, this.webBrowser_item);
            this.webBrowser_item.ObjectForScripting = this.m_webExternalHost_itemInfo;

            this.m_webExternalHost_biblioInfo.Initial(this.MainForm, this.webBrowser_biblio);
            this.webBrowser_biblio.ObjectForScripting = this.m_webExternalHost_biblioInfo;

#if NO
            // this.VerifyReaderPassword = this.VerifyReaderPassword;  // ʹ"У���������"��ť����״̬��ʾ��ȷ
            this.VerifyReaderPassword = this.MainForm.AppInfo.GetBoolean(
                "charging_form",
                "verify_reader_password",
                false);
#endif
            if (this.VerifyReaderPassword == true)
            {
                this.label_verifyReaderPassword.Visible = true;
                this.textBox_readerPassword.Visible = true;
                this.button_verifyReaderPassword.Visible = true;
            }
            else
            {
                this.label_verifyReaderPassword.Visible = false;
                this.textBox_readerPassword.Visible = false;
                this.button_verifyReaderPassword.Visible = false;
            }

            if (this.PatronBarcodeAllowHanzi == false)
                this.textBox_readerBarcode.ImeMode = System.Windows.Forms.ImeMode.Disable;

            this.SwitchFocus(READER_BARCODE, null);

#if NO
            // ���ڴ�ʱ��ʼ��
            this.m_bSuppressScriptErrors = !this.MainForm.DisplayScriptErrorDialog;
#endif

            // API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);

            this.commander = new Commander(this);
            this.commander.IsBusy -= new IsBusyEventHandler(commander_IsBusy);
            this.commander.IsBusy += new IsBusyEventHandler(commander_IsBusy);

            SetReaderRenderString("(��)");
            SetBiblioRenderString("(��)");
            SetItemRenderString("(��)");
        }

        void AppInfo_SaveMdiSize(object sender, EventArgs e)
        {
            if (sender != this)
                return;

            /*
            // ����splitContainer_main��״̬
            MainForm.AppInfo.SetInt(
                "chargingform_state",
                "splitContainer_main",
                this.splitContainer_main.SplitterDistance);
            // ����splitContainer_biblioAndItem��״̬
            MainForm.AppInfo.SetInt(
                "chargingform_state",
                "splitContainer_biblioAndItem",
                this.splitContainer_biblioAndItem.SplitterDistance);
             * */

            // �ָ���λ��
            // ����splitContainer_main��״̬
            this.MainForm.SaveSplitterPos(
                this.splitContainer_main,
                "chargingform_state",
                "splitContainer_main");
            // ����splitContainer_biblioAndItem��״̬
            this.MainForm.SaveSplitterPos(
                this.splitContainer_biblioAndItem,
                "chargingform_state",
                "splitContainer_biblioAndItem");

        }

        void AppInfo_LoadMdiSize(object sender, EventArgs e)
        {
            if (sender != this)
                return;

            try
            {
                /*
                // ���splitContainer_main��״̬
                int nValue = MainForm.AppInfo.GetInt(
                "chargingform_state",
                "splitContainer_main",
                -1);
                if (nValue != -1)
                    this.splitContainer_main.SplitterDistance = nValue;

                // ���splitContainer_biblioAndItem��״̬
                nValue = MainForm.AppInfo.GetInt(
                "chargingform_state",
                "splitContainer_biblioAndItem",
                -1);
                if (nValue != -1)
                    this.splitContainer_biblioAndItem.SplitterDistance = nValue;
                 * */

                // ���splitContainer_main��״̬
                this.MainForm.LoadSplitterPos(
                    this.splitContainer_main,
                    "chargingform_state",
                    "splitContainer_main");

                // ���splitContainer_biblioAndItem��״̬
                this.MainForm.LoadSplitterPos(
                    this.splitContainer_biblioAndItem,
                    "chargingform_state",
                    "splitContainer_biblioAndItem");
            }
            catch
            {
            }
        }

        void commander_IsBusy(object sender, IsBusyEventArgs e)
        {
            e.IsBusy = this.m_webExternalHost_readerInfo.ChannelInUse || this.m_webExternalHost_itemInfo.ChannelInUse || this.m_webExternalHost_biblioInfo.ChannelInUse;
        }

        private void ChargingForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void ChargingForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.commander.Destroy();

            if (this.m_webExternalHost_readerInfo != null)
            {
                this.m_webExternalHost_readerInfo.Destroy();
            }
            if (this.m_webExternalHost_itemInfo != null)
            {
                this.m_webExternalHost_itemInfo.Destroy();
            }
            if (this.m_webExternalHost_biblioInfo != null)
            {
                this.m_webExternalHost_biblioInfo.Destroy();
            }

            if (this.Channel != null)
                this.Channel.Close();   // TODO: �������һ��ʱ�䣬�������ʱ����Abort()

            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                // SaveSize();

                this.MainForm.AppInfo.LoadMdiSize -= new EventHandler(AppInfo_LoadMdiSize);
                this.MainForm.AppInfo.SaveMdiSize -= new EventHandler(AppInfo_SaveMdiSize);
            }
        }

        /// <summary>
        /// ͬһ�������������ĳɹ������ۻ��Ĳ�����ż���
        /// </summary>
        public string[] OneReaderItemBarcodes
        {
            get
            {
                if (this.oneReaderItemBarcodes == null)
                    return null;
                string[] result = new string[this.oneReaderItemBarcodes.Count];
                for (int i = 0; i < this.oneReaderItemBarcodes.Count; i++)
                {
                    result[i] = this.oneReaderItemBarcodes[i];
                }
                return result;
            }
        }

        // ���н����л����ܵ�
        /// <summary>
        /// �������͡�����ʱ���н����л�����
        /// </summary>
        public FuncState SmartFuncState
        {
            get
            {
                return m_funcstate;
            }
            set
            {
                /*
                FuncState old_funcstate = this.m_funcstate;

                this.FuncState = value;

                // ���webbrowser
                SetReaderRenderString("(��)");

                SetBiblioRenderString("(��)");
                SetItemRenderString("(��)");

                // �л�Ϊ��ͬ�Ĺ��ܵ�ʱ�򣬶�λ����
                if (old_funcstate != this.m_funcstate)
                {
                    if (this.m_funcstate != FuncState.Return)
                    {
                        this.textBox_readerBarcode.SelectAll();
                        this.textBox_itemBarcode.SelectAll();

                        // this.textBox_readerBarcode.Focus();
                        this.SwitchFocus(READER_BARCODE, null);
                    }
                    else
                    {
                        this.textBox_itemBarcode.SelectAll();

                        // this.textBox_itemBarcode.Focus();
                        this.SwitchFocus(ITEM_BARCODE, null);
                    }
                }
                else // �ظ�����Ϊͬ�����ܣ������������
                {
                        this.textBox_readerBarcode.Text = "";
                        this.textBox_itemBarcode.Text = "";

                    if (this.m_funcstate != FuncState.Return)
                    {
                        // this.textBox_readerBarcode.Focus();
                        this.SwitchFocus(READER_BARCODE, null);
                    }
                    else
                    {
                        // this.textBox_itemBarcode.Focus();
                        this.SwitchFocus(ITEM_BARCODE, null);
                    }
                }

                */

                SmartSetFuncState(value,
                    true,
                    true);
            }
        }

        // �������ù�������
        // parameters:
        //      bClearInfoWindow    �л����Ƿ������Ϣ������
        //      bDupAsClear �Ƿ���ظ������ö�������������������������
        void SmartSetFuncState(FuncState value,
            bool bClearInfoWindow,
            bool bDupAsClear)
        {
            // 2011/12/6
            this.m_webExternalHost_itemInfo.StopPrevious();
            this.webBrowser_reader.Stop();
            this.m_webExternalHost_readerInfo.StopPrevious();
            this.webBrowser_item.Stop();
            this.m_webExternalHost_biblioInfo.StopPrevious();
            this.webBrowser_biblio.Stop();

            FuncState old_funcstate = this.m_funcstate;

            this.FuncState = value;

            // ���webbrowser
            if (bClearInfoWindow == true)
            {
                SetReaderRenderString("(��)");

                SetBiblioRenderString("(��)");
                SetItemRenderString("(��)");
            }

            // �л�Ϊ��ͬ�Ĺ��ܵ�ʱ�򣬶�λ����
            if (old_funcstate != this.m_funcstate)
            {
                // 2008/9/26 new add
                if (this.AutoClearTextbox == true)
                {
                    this.textBox_readerBarcode.Text = "";
                    this.textBox_readerPassword.Text = "";
                    this.textBox_itemBarcode.Text = "";
                }

                if (this.m_funcstate != FuncState.Return)
                {
                    this.textBox_readerBarcode.SelectAll();
                    this.textBox_itemBarcode.SelectAll();

                    // this.textBox_readerBarcode.Focus();
                    this.SwitchFocus(READER_BARCODE, null);
                }
                else
                {
                    this.textBox_itemBarcode.SelectAll();

                    // this.textBox_itemBarcode.Focus();
                    this.SwitchFocus(ITEM_BARCODE, null);
                }
            }
            else // �ظ�����Ϊͬ�����ܣ������������
            {
                // 2008/9/26 new add
                if (this.AutoClearTextbox == true)
                {
                    this.textBox_readerBarcode.Text = "";
                    this.textBox_readerPassword.Text = "";
                    this.textBox_itemBarcode.Text = "";
                }
                else
                {
                    if (bDupAsClear == true)
                    {
                        this.textBox_readerBarcode.Text = "";
                        this.textBox_readerPassword.Text = "";
                        this.textBox_itemBarcode.Text = "";
                    }
                }

                if (this.m_funcstate != FuncState.Return)
                {
                    // this.textBox_readerBarcode.Focus();
                    this.SwitchFocus(READER_BARCODE, null);
                }
                else
                {
                    // this.textBox_itemBarcode.Focus();
                    this.SwitchFocus(ITEM_BARCODE, null);
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

                this.m_funcstate = value;

                this.toolStripMenuItem_borrow.Checked = false;
                this.toolStripMenuItem_return.Checked = false;
                this.toolStripMenuItem_verifyReturn.Checked = false;
                this.toolStripMenuItem_renew.Checked = false;
                this.toolStripMenuItem_lost.Checked = false;

                // 2008/9/26 new add
                if (this.AutoClearTextbox == true)
                {
                    this.textBox_readerBarcode.Text = "";
                    this.textBox_readerPassword.Text = "";
                    this.textBox_itemBarcode.Text = "";
                }

                if (m_funcstate == FuncState.Borrow)
                {
                    this.button_itemAction.Text = "��";
                    this.toolStripMenuItem_borrow.Checked = true;
                    // this.textBox_readerBarcode.Enabled = true;
                    EnableEdit(READER_BARCODE, true);
                }
                if (m_funcstate == FuncState.Return)
                {
                    this.button_itemAction.Text = "��";
                    this.toolStripMenuItem_return.Checked = true;
                    this.textBox_readerBarcode.Text = "";
                    // this.textBox_readerBarcode.Enabled = false;
                    EnableEdit(READER_BARCODE, false);
                }
                if (m_funcstate == FuncState.VerifyReturn)
                {
                    this.button_itemAction.Text = "��֤��";
                    this.toolStripMenuItem_verifyReturn.Checked = true;
                    // this.textBox_readerBarcode.Enabled = true;
                    EnableEdit(READER_BARCODE, true);
                }
                if (m_funcstate == FuncState.VerifyRenew)
                {
                    this.button_itemAction.Text = "����";
                    this.toolStripMenuItem_renew.Checked = true;
                    // this.textBox_readerBarcode.Enabled = true;
                    EnableEdit(READER_BARCODE, true);
                }
                if (m_funcstate == FuncState.Lost)
                {
                    this.button_itemAction.Text = "��ʧ";
                    this.toolStripMenuItem_lost.Checked = true;
                    // this.textBox_readerBarcode.Enabled = true;
                    EnableEdit(READER_BARCODE, true);
                }
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public ChargingForm()
        {
            InitializeComponent();

            // ��������ؼ��߱����Էֱ��״η�ֹͣ������
            Global.PrepareStop(this.webBrowser_biblio);
            Global.PrepareStop(this.webBrowser_item);
            Global.PrepareStop(this.webBrowser_reader);
        }

        /// <summary>
        /// ��ǰ����֤�����
        /// </summary>
        public string CurrentReaderBarcode
        {
            get
            {
                return m_strCurrentReaderBarcode;
            }
            set
            {
                m_strCurrentBarcode = value;

                // ��������¼����ʾ�ڴ�����
                string strError = "";
                // return:
                //      -1  error
                //      0   û���ҵ�
                //      1   �ɹ�
                //      2   ����
                int nRet = LoadReaderRecord(ref m_strCurrentBarcode,
                    out strError);
                if (nRet == -1)
                {
                    SetReaderRenderString(
                        "װ�ض��߼�¼��������: " + strError);
                }
            }
        }

        /// <summary>
        /// ����װ�ض��߼�¼
        /// </summary>
        public void Reload()
        {
            string strBarcode = this.textBox_readerBarcode.Text;

            if (string.IsNullOrEmpty(strBarcode) == true)
                strBarcode = m_strCurrentBarcode;

            if (string.IsNullOrEmpty(strBarcode) == true)
                return;


            // ��������¼����ʾ�ڴ�����
            string strError = "";
            // return:
            //      -1  error
            //      0   û���ҵ�
            //      1   �ɹ�
            //      2   ����
            int nRet = LoadReaderRecord(ref strBarcode,
                out strError);
            if (nRet == -1)
            {
                SetReaderRenderString(
                    "װ�ض��߼�¼��������: " + strError);
            }

            if (strBarcode != this.textBox_readerBarcode.Text)
                this.textBox_readerBarcode.Text = strBarcode; 
        }


        void SetReaderRenderString(string strText)
        {
            // NewExternal();

            if (this.DisplayState == DisplayState.TEXT)
                this.textBox_readerInfo.Text = strText;
            else
            {
                m_webExternalHost_readerInfo.StopPrevious();

                if (strText == "(��)")
                {
                    Global.ClearHtmlPage(this.webBrowser_reader,
                        this.MainForm.DataDir);
                    return;
                }

                // 2012/1/13
                Global.StopWebBrowser(this.webBrowser_reader);

                // PathUtil.CreateDirIfNeed(this.MainForm.DataDir + "\\servermapped");

                string strTempFilename = this.MainForm.DataDir + "\\~charging_temp_reader.html";
                using (StreamWriter sw = new StreamWriter(strTempFilename, false, Encoding.UTF8))
                {
                    sw.Write(strText);
                }
                this.webBrowser_reader.Navigate(strTempFilename);
            }
        }

        void SetItemRenderString(string strText)
        {
            if (this.DisplayState == DisplayState.TEXT)
                this.textBox_itemInfo.Text = strText;
            else
            {
                // 2011/12/6
                this.m_webExternalHost_itemInfo.StopPrevious();

                if (strText == "(��)")
                {
                    Global.ClearHtmlPage(this.webBrowser_item,
                        this.MainForm.DataDir);
                    return;
                }

                // 2012/1/13
                Global.StopWebBrowser(this.webBrowser_item);

                // PathUtil.CreateDirIfNeed(this.MainForm.DataDir + "\\servermapped");
                string strTempFilename = this.MainForm.DataDir + "\\~charging_temp_item.html";
                using (StreamWriter sw = new StreamWriter(strTempFilename, false, Encoding.UTF8))
                {
                    sw.Write(strText);
                }
                this.webBrowser_item.Navigate(strTempFilename);
            }
        }

        void SetBiblioRenderString(string strText)
        {
            if (this.DisplayState == DisplayState.TEXT)
                this.textBox_biblioInfo.Text = strText;
            else
            {
                // 2011/12/6
                this.m_webExternalHost_biblioInfo.StopPrevious();
                this.webBrowser_biblio.Stop();

                if (strText == "(��)")
                {
                    Global.ClearHtmlPage(this.webBrowser_biblio,
                        this.MainForm.DataDir);
                    return;
                }

                // 2012/1/13
                Global.StopWebBrowser(this.webBrowser_biblio);

                // PathUtil.CreateDirIfNeed(this.MainForm.DataDir + "\\servermapped");
                string strTempFilename = this.MainForm.DataDir + "\\~charging_temp_biblio.html";
                using (StreamWriter sw = new StreamWriter(strTempFilename, false, Encoding.UTF8))
                {
                    sw.Write(strText);
                }
                this.webBrowser_biblio.Navigate(strTempFilename);
            }
        }

        // ���ַ����еĺ� %datadir% �滻Ϊʵ�ʵ�ֵ
        string ReplaceMacro(string strText)
        {
            strText = strText.Replace("%mappeddir%", PathUtil.MergePath(this.MainForm.DataDir, "servermapped"));
            return strText.Replace("%datadir%", this.MainForm.DataDir);
        }

        /// <summary>
        /// �Ƿ� �ʶ�������������װ�ض��߼�¼��ʱ��ȱʡΪ false
        /// </summary>
        public bool VoiceName
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "speak_reader_name",
                    false);
            }
        }

        // ��ÿ��Է��͸���������֤������ַ���
        // ȥ��ǰ��� ~
        static string GetRequestPatronBarcode(string strText)
        {
            if (string.IsNullOrEmpty(strText) == true)
                return "";
            if (strText[0] == '~')
                return strText.Substring(1);

            return strText;
        }

        // װ����߼�¼
        // return:
        //      -1  error
        //      0   û���ҵ�
        //      1   �ɹ�
        //      2   ����
        int LoadReaderRecord(ref string strBarcode,
            out string strError)
        {
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڳ�ʼ���������� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            EnableControls(false);

            try
            {

                SetReaderRenderString("(��)");

                SetBiblioRenderString("(��)");
                SetItemRenderString("(��)");

                string strStyle = this.PatronRenderFormat;

                if (this.VoiceName == true)
                    strStyle += ",summary";

                stop.SetMessage("����װ����߼�¼ " + strBarcode + " ...");

                string[] results = null;
                byte [] baTimestamp = null;
                string strRecPath = "";
                long lRet = Channel.GetReaderInfo(
                    stop,
                    GetRequestPatronBarcode(strBarcode),
                    strStyle,   // this.RenderFormat, // "html",
                    out results,
                    out strRecPath,
                    out baTimestamp,
                    out strError);
                if (lRet == 0)
                {
                    if (StringUtil.IsIdcardNumber(strBarcode) == true)
                        SetReaderRenderString("֤�����(�����֤��)Ϊ '" + strBarcode + "' �Ķ��߼�¼û���ҵ� ...");
                    else
                        SetReaderRenderString("֤�����Ϊ '" + strBarcode + "' �Ķ��߼�¼û���ҵ� ...");
                    return 0;   // not found
                }
                if (lRet == -1)
                    goto ERROR1;

                if (results == null || results.Length == 0)
                {
                    strError = "���ص�results��������";
                    goto ERROR1;
                }
                string strResult = "";
                strResult = results[0];


                if (lRet > 1)
                {
                    /*
                    strError = "����֤����� '" + strBarcode + "' ���� " + lRet.ToString() + " �����߼�¼������һ�����ش�����ϵͳ����Ա�����ų���\r\n\r\n(��ǰ��������ʾ�������еĵ�һ����¼)";
                    goto ERROR1;
                     * */
                    SelectPatronDialog dlg = new SelectPatronDialog();

                    MainForm.SetControlFont(dlg, this.Font, false);
                    dlg.NoBorrowHistory = this.NoBorrowHistory;
                    dlg.ColorBarVisible = false;
                    dlg.MessageVisible = false;
                    dlg.Overflow = StringUtil.SplitList(strRecPath).Count < lRet;
                    int nRet = dlg.Initial(
                        this.MainForm,
                        this.Channel,
                        this.stop,
                        StringUtil.SplitList(strRecPath),
                        "��ѡ��һ�����߼�¼",
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    // TODO: ���洰���ڵĳߴ�״̬
                    this.MainForm.AppInfo.LinkFormState(dlg, "ChargingForm_SelectPatronDialog_state");
                    dlg.ShowDialog(this);
                    this.MainForm.AppInfo.UnlinkFormState(dlg);

                    if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                        return 2;

                    strBarcode = dlg.SelectedBarcode;
                    strResult = dlg.SelectedHtml;
                }

                SetReaderRenderString(ReplaceMacro(strResult));

                this.m_strCurrentBarcode = strBarcode;  // 2011/6/24

                if (this.VoiceName == true && results.Length >= 2)
                {
                    string strName = results[1];
                    this.MainForm.Speak(strName);
                }
            }
            finally
            {
                EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

        // װ����¼
        // parameters:
        //      strBarcode  ������š�����Ϊ"@path:"��������ʾʹ�ü�¼·��
        int LoadItemAndBiblioRecord(string strBarcode,
            out string strError)
        {
            SetBiblioRenderString("(��)");
            SetItemRenderString("(��)");

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����װ����¼ " + strBarcode + " ...");
            stop.BeginLoop();

            try
            {
                string strItemText = "";
                string strBiblioText = "";

                long lRet = Channel.GetItemInfo(
                    stop,
                    strBarcode,
                    this.RenderFormat, // "html",
                    out strItemText,
                    this.RenderFormat, // "html",
                    out strBiblioText,
                    out strError);
                if (lRet == 0)
                    return 0;   // not found
                if (lRet == -1)
                    goto ERROR1;

                SetItemRenderString(ReplaceMacro(strItemText));

                SetBiblioRenderString(ReplaceMacro(strBiblioText));

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

        /// <summary>
        /// ��������ؼ��л�� HTML �ַ���
        /// </summary>
        /// <param name="webBrowser">������ؼ�</param>
        /// <returns>HTML �ַ���</returns>
        public static string GetHtmlString(WebBrowser webBrowser)
        {
            HtmlDocument doc = webBrowser.Document;

            if (doc == null)
                return "";

            HtmlElement item = doc.All["html"];
            if (item == null)
                return "";

            return item.OuterHtml;
        }

#if NO
        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }
#endif

        private void textBox_readerBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_loadReader;

            this.MainForm.EnterPatronIdEdit(InputType.ALL);
        }

        private void textBox_readerBarcode_Leave(object sender, EventArgs e)
        {
            this.MainForm.LeavePatronIdEdit();
        }

        private void textBox_readerPassword_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_verifyReaderPassword;
        }

        private void textBox_itemBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_itemAction;

            this.MainForm.EnterPatronIdEdit(InputType.ALL);
        }

        private void textBox_itemBarcode_Leave(object sender, EventArgs e)
        {
            this.MainForm.LeavePatronIdEdit();
        }

        private void button_loadReader_Click(object sender, EventArgs e)
        {
            this.button_loadReader.Enabled = false; // BUG 2009/6/2
            this.textBox_readerBarcode.Enabled = false;   // 2009/10/20 new add

            this.m_webExternalHost_readerInfo.StopPrevious();
            this.webBrowser_reader.Stop();

            // 2009/10/20 new add
            this.m_webExternalHost_itemInfo.StopPrevious();
            this.webBrowser_item.Stop();
            this.m_webExternalHost_biblioInfo.StopPrevious();
            this.webBrowser_biblio.Stop();

            this.commander.AddMessage(WM_LOAD_READER);
        }

        // �Ƿ�Ϊ����
        // ����һ�����Ϻ��֣����� ~ ��ͷ����������
        static bool IsName(string strText)
        {
            if (StringUtil.ContainHanzi(strText) == true)
                return true;
            if (StringUtil.HasHead(strText, "~") == true)
                return true;
            return false;
        }
        /// <summary>
        /// װ�ض��߼�¼
        /// </summary>
        public void DoLoadReader()
        {
            string strError = "";
            int nRet = 0;

            Debug.Assert(this.MainForm != null, "this.MainForm == null");
            Debug.Assert(this.Channel != null, "this.Channel == null");

            // 2008/9/26 new add
            if (this.AutoClearTextbox == true)
            {
                this.textBox_readerPassword.Text = "";
                this.textBox_itemBarcode.Text = "";
            }

            if (this.textBox_readerBarcode.Text == "")
            {
                strError = "��δ�������֤�����";
                goto ERROR1;
            }

            if (this.AutoToUpper == true)
                this.textBox_readerBarcode.Text = this.textBox_readerBarcode.Text.ToUpper();

            this.ActiveReaderBarcode = this.textBox_readerBarcode.Text;

            // �������Ĳ������
            Debug.Assert(this.m_itemBarcodes != null, "this.m_itemBarcodes == null");
            this.m_itemBarcodes.Clear();

            if (this.NeedVerifyBarcode == true
                && StringUtil.IsIdcardNumber(this.textBox_readerBarcode.Text) == false
                && IsName(this.textBox_readerBarcode.Text) == false)
            {
                // ��ʽУ�������
                // return:
                //      -2  ������û������У�鷽�����޷�У��
                //      -1  error
                //      0   ���ǺϷ��������
                //      1   �ǺϷ��Ķ���֤�����
                //      2   �ǺϷ��Ĳ������
                nRet = VerifyBarcode(
                    this.Channel.LibraryCodeList,
                    this.textBox_readerBarcode.Text,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ����������ʽ���Ϸ�
                if (nRet == 0)
                {
                    string strFastInputText = ChargingInfoDlg.Show(
                        this.CharingInfoHost,
                        "����������� " + this.textBox_readerBarcode.Text + " ��ʽ����ȷ(" + strError + ")�����������롣",
                        InfoColor.Red,
                        "װ�ض��߼�¼",
                        this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                    this.SwitchFocus(READER_BARCODE, strFastInputText);
                    return;
                }

                // ʵ��������ǲ������
                if (nRet == 2)
                {
                    string strFastInputText = ChargingInfoDlg.Show(
                        this.CharingInfoHost,
                        "������������ " + this.textBox_readerBarcode.Text + " �ǲ�����š����������֤����š�",
                        InfoColor.Red,
                        "װ�ض��߼�¼",
                        this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                    this.SwitchFocus(READER_BARCODE, strFastInputText);
                    return;
                }

                // ���ڷ�����û������У�鹦�ܣ�����ǰ�˷�����У��Ҫ������������һ��
                if (nRet == -2)
                    MessageBox.Show(this, "���棺ǰ�˿�����У�����빦�ܣ����Ƿ�������ȱ����Ӧ�Ľű��������޷�У�����롣\r\n\r\n��Ҫ������ִ˾���Ի�����ر�ǰ��У�鹦��");
            }

            // this.oneReaderItemBarcodes.Clear();
            this.oneReaderItemBarcodes = new List<string>();    // 2014/12/22

            this.m_strCurrentBarcode = this.textBox_readerBarcode.Text;

            // ��������¼����ʾ�ڴ�����
            // return:
            //      -1  error
            //      0   û���ҵ�
            //      1   �ɹ�
            //      2   ����
            nRet = LoadReaderRecord(ref m_strCurrentBarcode,
                out strError);
            if (this.m_strCurrentBarcode != this.textBox_readerBarcode.Text)
            {
                this.textBox_readerBarcode.Text = this.m_strCurrentBarcode;
                this.ActiveReaderBarcode = this.textBox_readerBarcode.Text;
            }

            if (nRet == -1)
            {
                string strFastInputText = ChargingInfoDlg.Show(
                    this.CharingInfoHost,
                    strError,
                    InfoColor.Red,
                    "װ�ض��߼�¼",
                    this.InfoDlgOpacity,
                    this.MainForm.DefaultFont);
                /*
                SetReaderRenderString(this.webBrowser_reader,
                    "װ�ض��߼�¼��������: " + strError);
                 * */
                // ���뽹����Ȼ�ص�����֤�����������
                /*
                this.textBox_readerBarcode.SelectAll();
                this.textBox_readerBarcode.Focus();
                 * */
                this.SwitchFocus(READER_BARCODE, strFastInputText);
            }
            else if (nRet == 0)
            {
                if (this.Channel.ErrorCode == ErrorCode.IdcardNumberNotFound)
                    strError = "�������֤��(��֤�����) " + this.textBox_readerBarcode.Text + " �����ڡ�\r\n\r\n�����ʹ�����֤��һ�ν��飬���������֤���ٴ������߼�¼";
                else
                    strError = "����֤����� " + this.textBox_readerBarcode.Text + " �����ڡ�";
                string strFastInputText = ChargingInfoDlg.Show(
                        this.CharingInfoHost,
                        strError,
                        InfoColor.Red,
                        "װ�ض��߼�¼",
                        this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);

                // ���뽹����Ȼ�ص�����֤�����������
                /*
                this.textBox_readerBarcode.SelectAll();
                this.textBox_readerBarcode.Focus();
                 * */
                this.SwitchFocus(READER_BARCODE, strFastInputText);
            }
            else if (nRet == 2)
            {
                // ����װ�� 
                return;
            }
            else
            {
                Debug.Assert(nRet == 1, "");
                // ת�����뽹��
                /*
                if (this.textBox_readerPassword.Enabled == true)
                {
                    this.textBox_readerPassword.SelectAll();
                    this.textBox_readerPassword.Focus();
                }
                else
                {
                    this.textBox_itemBarcode.SelectAll();
                    this.textBox_itemBarcode.Focus();
                }
                */
                if (this.textBox_readerPassword.Visible == true // 2011/12/5
                    && this.textBox_readerPassword.Enabled == true)
                {
                    this.SwitchFocus(READER_PASSWORD, null);
                }
                else
                {
                    this.SwitchFocus(ITEM_BARCODE, null);
                }

                Debug.Assert(this.MainForm.OperHistory != null, "this.MainForm.OperHistory == null");

                // ����������ʷ����
                this.MainForm.OperHistory.ReaderBarcodeScaned(
                    this.textBox_readerBarcode.Text);
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
            if (this.FuncState == FuncState.Return)
                this.SwitchFocus(ITEM_BARCODE, "");
            else
                this.SwitchFocus(READER_BARCODE, "");
            return;
        }

        // 
        /// <summary>
        /// ��ӡ�軹ƾ��
        /// </summary>
        public void Print()
        {
            // ������ʷ����
            this.MainForm.OperHistory.Print();
        }

        /*
        // ���Դ�ӡ�軹ƾ��
        public void TestPrint()
        {
            // ������ʷ����
            this.MainForm.OperHistory.TestPrint();
        }*/

        void EnableEdit(int target,
            bool bEnable)
        {
            API.PostMessage(this.Handle, WM_ENABLE_EDIT,
                target, bEnable == true ? 1 : 0);
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
                case WM_LOAD_READER:
                    {
                        if (this.m_webExternalHost_readerInfo.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            DoLoadReader();
                        }
                    }
                    return;
                case WM_LOAD_ITEM:
                    {
                        if (this.m_webExternalHost_itemInfo.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            DoItemAction();
                        }
                    }
                    return;
                /*
            case WM_LOADSIZE:
                LoadSize();
                return;
                 * */
                case WM_ENABLE_EDIT:
                    {
                        int nOn = (int)m.LParam;
                        if ((int)m.WParam == READER_BARCODE)
                        {
                            if (nOn == 1)
                            {
                                if (this.textBox_readerBarcode.Enabled == false)
                                {
                                    this.textBox_readerBarcode.Enabled = true;
                                    this.button_loadReader.Enabled = true;
                                }
                            }
                            else
                            {
                                if (this.textBox_readerBarcode.Enabled == true)
                                {
                                    this.textBox_readerBarcode.Enabled = false;
                                    this.button_loadReader.Enabled = false;
                                }
                            }
                        }
                        if ((int)m.WParam == ITEM_BARCODE)
                        {
                            if (nOn == 1)
                            {
                                if (this.textBox_itemBarcode.Enabled == false)
                                    this.textBox_itemBarcode.Enabled = true;
                            }
                            else
                            {
                                if (this.textBox_itemBarcode.Enabled == true)
                                    this.textBox_itemBarcode.Enabled = false;
                            }
                        }
                    }
                    return;

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

                                // 2009/6/2 new add
                                if (this.button_loadReader.Enabled == false)
                                    this.button_loadReader.Enabled = true;
                                /*
                                // 2009/11/8 new add
                                if (this.textBox_readerBarcode.Enabled == false)
                                    this.textBox_readerBarcode.Enabled = true;
                                 * */

                                this.button_loadReader_Click(this, null);
                            }
                            if ((int)m.WParam == READER_PASSWORD)
                            {
                                this.textBox_readerPassword.Text = strFastInputText;

                                // 2009/6/2 new add
                                if (this.button_verifyReaderPassword.Enabled == false)
                                    this.button_verifyReaderPassword.Enabled = true;
                                /*
                                // 2009/11/8 new add
                                if (this.textBox_readerPassword.Enabled == false)
                                    this.textBox_readerPassword.Enabled = true;
                                 * */

                                this.button_verifyReaderPassword_Click(this, null);
                            }
                            if ((int)m.WParam == ITEM_BARCODE)
                            {
                                this.textBox_itemBarcode.Text = strFastInputText;

                                // 2009/6/2 new add
                                if (this.button_itemAction.Enabled == false)
                                    this.button_itemAction.Enabled = true;
                                /*
                                // 2009/11/8 new add
                                if (this.textBox_itemBarcode.Enabled == false)
                                    this.textBox_itemBarcode.Enabled = true;
                                 * */

                                this.button_itemAction_Click(this, null);
                            }

                            return;
                        }

                        if ((int)m.WParam == READER_BARCODE)
                        {
                            // 2009/6/2 new add
                            if (this.button_loadReader.Enabled == false)
                                this.button_loadReader.Enabled = true;
                            // 2009/11/8 new add
                            if (this.textBox_readerBarcode.Enabled == false)
                                this.textBox_readerBarcode.Enabled = true;

                            /*
                            // ???
                            if (this.FuncState == FuncState.Return)
                                this.FuncState = FuncState.Borrow;
                             * */
                            this.textBox_readerBarcode.SelectAll();
                            this.textBox_readerBarcode.Focus();


                        }

                        if ((int)m.WParam == READER_PASSWORD)
                        {
                            // 2009/6/2 new add
                            if (this.button_verifyReaderPassword.Enabled == false)
                                this.button_verifyReaderPassword.Enabled = true;
                            // 2009/11/8 new add
                            if (this.textBox_readerPassword.Enabled == false)
                                this.textBox_readerPassword.Enabled = true;

                            this.textBox_readerPassword.SelectAll();
                            this.textBox_readerPassword.Focus();
                        }

                        if ((int)m.WParam == ITEM_BARCODE)
                        {
                            // 2009/6/2 new add
                            if (this.button_itemAction.Enabled == false)
                                this.button_itemAction.Enabled = true;
                            // 2009/11/8 new add
                            if (this.textBox_itemBarcode.Enabled == false)
                                this.textBox_itemBarcode.Enabled = true;

                            this.textBox_itemBarcode.SelectAll();
                            this.textBox_itemBarcode.Focus();
                        }

                        return;
                    }
                // break;
            }
            base.DefWndProc(ref m);
        }

        //
        /// <summary>
        ///  �����Ŀ��ʵ������������ؼ��е�����
        /// </summary>
        public void ClearItemAndBiblioControl()
        {
            SetBiblioRenderString("(��)");
            SetItemRenderString("(��)");
        }

#if NOOOOOOOOOOO
        void NewExternal()
        {
            if (this.m_webExternalHost != null)
            {
                /*
                if (this.m_webExternalHost.IsInLoop == false)
                    return; // �������ѭ���У���������������
                 * */

                this.m_webExternalHost.Destroy();
                this.webBrowser_reader.ObjectForScripting = null;
            }

            this.m_webExternalHost = new WebExternalHost();
            this.m_webExternalHost.Initial(this.MainForm);
            this.webBrowser_reader.ObjectForScripting = this.m_webExternalHost;
        }
#endif

        private void button_itemAction_Click(object sender, EventArgs e)
        {
            this.button_itemAction.Enabled = false;
            this.textBox_itemBarcode.Enabled = false;   // 2009/10/20 new add

            this.m_webExternalHost_itemInfo.StopPrevious();
            this.webBrowser_item.Stop();

            // 2009/10/20 new add
            this.m_webExternalHost_readerInfo.StopPrevious();
            this.webBrowser_reader.Stop();
            this.m_webExternalHost_biblioInfo.StopPrevious();
            this.webBrowser_biblio.Stop();

            this.commander.AddMessage(WM_LOAD_ITEM);
        }

        //
        delegate int Delegate_SelectOneItem(
            FuncState func,
            string strText,
    out string strItemBarcode,
    out string strError);

        // return:
        //      -1  error
        //      0   ����
        //      1   �ɹ�
        internal int SelectOneItem(
            FuncState func,
            string strText,
            out string strItemBarcode,
            out string strError)
        {
            strError = "";
            strItemBarcode = "";

            if (this.InvokeRequired)
            {
                Delegate_SelectOneItem d = new Delegate_SelectOneItem(SelectOneItem);
                object[] args = new object[4];
                args[0] = func;
                args[1] = strText;
                args[2] = strItemBarcode;
                args[3] = strError;
                int result = (int)this.Invoke(d, args);

                // ȡ��out����ֵ
                strItemBarcode = (string)args[2];
                strError = (string)args[3];
                return result;
            }

            SelectItemDialog dlg = new SelectItemDialog();

            MainForm.SetControlFont(dlg, this.Font, false);
            if (func == dp2Circulation.FuncState.Borrow
                || func == dp2Circulation.FuncState.ContinueBorrow)
            {
                dlg.FunctionType = "borrow";
                dlg.Text = "��ѡ��Ҫ���ĵĲ�";
            }
            else if (func == dp2Circulation.FuncState.VerifyRenew)
            {
                dlg.FunctionType = "renew";
                dlg.Text = "��ѡ��Ҫ����Ĳ�";
            }
            else if (func == dp2Circulation.FuncState.Return || func == dp2Circulation.FuncState.Lost)
            {
                dlg.FunctionType = "return";
                dlg.Text = "��ѡ��Ҫ���صĲ�";
            }
            else if (func == dp2Circulation.FuncState.VerifyReturn || func == dp2Circulation.FuncState.VerifyLost)
            {
                dlg.FunctionType = "return";
                dlg.VerifyBorrower = this.textBox_readerBarcode.Text;
                dlg.Text = "��ѡ��Ҫ(��֤)���صĲ�";
            }

            dlg.AutoOperSingleItem = this.AutoOperSingleItem;
            dlg.AutoSearch = true;
            dlg.MainForm = this.MainForm;
            dlg.From = "ISBN";
            dlg.QueryWord = strText;

            dlg.UiState = this.MainForm.AppInfo.GetString(
        "ChargingForm",
        "SelectItemDialog_uiState",
        "");

            // TODO: ���洰���ڵĳߴ�״̬
            this.MainForm.AppInfo.LinkFormState(dlg, "ChargingForm_SelectItemDialog_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            this.MainForm.AppInfo.SetString(
"ChargingForm",
"SelectItemDialog_uiState",
dlg.UiState);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return 0;

            Debug.Assert(string.IsNullOrEmpty(dlg.SelectedItemBarcode) == false, "");
            strItemBarcode = dlg.SelectedItemBarcode;
            return 1;
        }


        /// <summary>
        /// ִ�ж�����ť����Ķ���
        /// </summary>
        /// <returns>-1: ����; 0: ����û��ִ��; 1: �����Ѿ�ִ��</returns>
        public int DoItemAction()
        {
            string strError = "";
            int nRet = 0;

            DateTime start_time = DateTime.Now;

#if NO
            // ����������Ϊ�գ�������س����Ա������л�������֤�����������
            if (this.textBox_itemBarcode.Text == "")
            {
                MessageBox.Show(this, "��δ����������");
                this.SwitchFocus(ITEM_BARCODE, null);
                return -1;
            }
#endif

            if (this.AutoToUpper == true)
                this.textBox_itemBarcode.Text = this.textBox_itemBarcode.Text.ToUpper();

            this.ActiveReaderBarcode = this.textBox_readerBarcode.Text;

            BarcodeAndTime barcodetime = null;

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

                        // �����ǰ�ڡ�����״̬����Ҫ�޸�Ϊ����֤�������������֤�����������Ϊdiable���޷��л������ȥ
                        if (this.FuncState == FuncState.Return)
                            this.FuncState = FuncState.VerifyReturn;


                        // �������֤�����������
                        this.textBox_readerBarcode.Text = "��������һ�����ߵ�֤�����...";
                        this.SwitchFocus(READER_BARCODE, null);
                        return 0;
                    }
                }
                barcodetime = new BarcodeAndTime();
                barcodetime.Barcode = this.textBox_itemBarcode.Text;
                barcodetime.Time = DateTime.Now;


                this.m_itemBarcodes.Add(barcodetime);
                // ��������һ������Ϳ�����
                while (this.m_itemBarcodes.Count > 1)
                    this.m_itemBarcodes.RemoveAt(0);
            }

            string strFastInputText = "";

            string strTemp = this.textBox_itemBarcode.Text;
            if ( ( this.UseIsbnBorrow == true && QuickChargingForm.IsISBN(ref strTemp) == true)
                || strTemp.ToLower() == "?b"
                || string.IsNullOrEmpty(strTemp) == true)
            {
                string strItemBarcode = "";
                // return:
                //      -1  error
                //      0   ����
                //      1   �ɹ�
                nRet = SelectOneItem(this.FuncState,
                    strTemp.ToLower() == "?b" ? "" : strTemp,
                    out strItemBarcode,
                    out strError);
                if (nRet == -1)
                {
                    strError = "ѡ����¼�Ĺ����г���: " + strError;
                    goto ERROR1;
                }
                if (nRet == 0)
                {
                    strFastInputText = ChargingInfoDlg.Show(
                        this.CharingInfoHost,
                        "��ȡ��ѡ����¼��ע�������δִ��",
                        InfoColor.Red,
                        "ɨ�������",
                        this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                    this.SwitchFocus(ITEM_BARCODE, strFastInputText);
                    return -1;
                }

                this.textBox_itemBarcode.Text = strItemBarcode;
            }

            this.ActiveItemBarcode = this.textBox_itemBarcode.Text;


            if (this.NeedVerifyBarcode == true)
            {
                // ��ʽУ�������
                // return:
                //      -2  ������û������У�鷽�����޷�У��
                //      -1  error
                //      0   ���ǺϷ��������
                //      1   �ǺϷ��Ķ���֤�����
                //      2   �ǺϷ��Ĳ������
                nRet = VerifyBarcode(
                    this.Channel.LibraryCodeList,
                    this.textBox_itemBarcode.Text,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ����������ʽ���Ϸ�
                if (nRet == 0)
                {
                    strFastInputText = ChargingInfoDlg.Show(
                        this.CharingInfoHost,
                        "����������� " + this.textBox_itemBarcode.Text + " ��ʽ����ȷ(" + strError + ")�����������롣",
                        InfoColor.Red,
                        "ɨ�������",
                        this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                    this.SwitchFocus(ITEM_BARCODE, strFastInputText);
                    return -1;
                }

                // ����ʵ��������Ƕ���֤�����
                if (nRet == 1)
                {
                    // 2008/1/2 new add
                    if (this.AutoSwitchReaderBarcode == true)
                    {
                        string strItemBarcode = this.textBox_itemBarcode.Text;
                        this.textBox_itemBarcode.Text = "";

                        // �����ǰ�ڡ��������ߡ���֤����״̬����Ҫ�޸�Ϊ���衱?
                        if (this.FuncState == FuncState.Return
                            || this.FuncState == FuncState.VerifyReturn)
                            this.FuncState = FuncState.Borrow;
                        else
                            this.FuncState = FuncState.VerifyReturn;

                        // ֱ�ӿ�Խ��ȥִ�н��Ĺ���
                        this.textBox_readerBarcode.Text = strItemBarcode;
                        this.button_loadReader_Click(null, null);

                        // this.SwitchFocus(READER_BARCODE, strItemBarcode);
                        return 0;
                    }

                    // �̰�ʽ��Ҫ���������ƥ��
                    strFastInputText = ChargingInfoDlg.Show(
                        this.CharingInfoHost,
                        "������������ " + this.textBox_itemBarcode.Text + " �Ƕ���֤����š������������š�",
                        InfoColor.Red,
                        "ɨ�������",
                        this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                    this.SwitchFocus(ITEM_BARCODE, strFastInputText);
                    return -1;
                }

                // ���ڷ�����û������У�鹦�ܣ�����ǰ�˷�����У��Ҫ������������һ��
                if (nRet == -2)
                    MessageBox.Show(this, "���棺ǰ�˿�����У�����빦�ܣ����Ƿ�������ȱ����Ӧ�Ľű��������޷�У�����롣\r\n\r\n��Ҫ������ִ˾���Ի�����ر�ǰ��У�鹦��");
            }

            EnableControls(false);

            try
            {

                if (this.NoBiblioAndItemInfo == false)
                {
                    // ���Ĳ���ǰװ����¼
                    nRet = LoadItemAndBiblioRecord(this.textBox_itemBarcode.Text,
                        out strError);
                    if (nRet == 0)
                    {
                        strError = "������� '" + this.textBox_itemBarcode.Text + "' û���ҵ�";
                        goto ERROR1;
                    }

                    if (nRet == -1)
                        goto ERROR1;
                }


                long lRet = 0;

                // ��/����
                if (this.FuncState == FuncState.Borrow
                    || this.FuncState == FuncState.VerifyRenew)
                {
                    string strOperName = "";

                    if (this.FuncState == FuncState.Borrow)
                    {
                        strOperName = "����";
                    }
                    else if (this.FuncState == FuncState.VerifyRenew)
                    {
                        strOperName = "����";
                    }


                    stop.OnStop += new StopEventHandler(this.DoStop);
                    stop.Initial("���ڽ���" + strOperName + "����: " + this.textBox_readerBarcode.Text
                    + " " + strOperName + " " + this.textBox_itemBarcode.Text + " ...");
                    stop.BeginLoop();

                    // ??
                    // SetReaderRenderString("(��)");

                    if (this.NoBiblioAndItemInfo == false)
                    {
                        // �����Ŀ��ʵ����Ϣ
                        SetBiblioRenderString("(��)");
                        SetItemRenderString("(��)");
                    }

                    try
                    {
                        //                   string strResult = "";
                        string strReaderRecord = "";
                        // string strItemRecord = "";
                        string strConfirmItemRecPath = null;

                        bool bRenew = false;
                        if (this.FuncState == FuncState.VerifyRenew)
                            bRenew = true;

                    REDO:
                        string[] aDupPath = null;
                        string[] item_records = null;
                        string[] reader_records = null;
                        string[] biblio_records = null;
                        string strOutputReaderBarcode = "";

                        BorrowInfo borrow_info = null;

                        // item���صĸ�ʽ
                        string strItemReturnFormats = "";
                        // 2008/5/9 �б�Ҫ�ŷ���item��Ϣ
                        if (this.NoBiblioAndItemInfo == false)
                            strItemReturnFormats = this.RenderFormat;
                        if (this.MainForm.ChargingNeedReturnItemXml == true)
                        {
                            if (String.IsNullOrEmpty(strItemReturnFormats) == false)
                                strItemReturnFormats += ",";
                            strItemReturnFormats += "xml";
                        }

                        // biblio���صĸ�ʽ
                        string strBiblioReturnFormats = "";
                        if (this.NoBiblioAndItemInfo == false)
                            strBiblioReturnFormats = this.RenderFormat;

                        string strStyle = "reader";
                        if (this.NoBiblioAndItemInfo == false)
                            strStyle += ",item,biblio";
                        else if (this.MainForm.ChargingNeedReturnItemXml)
                            strStyle += ",item";

                        //if (this.MainForm.TestMode == true)
                        //    strStyle += ",testmode";

                        lRet = Channel.Borrow(
                            stop,
                            bRenew,
                            this.textBox_readerBarcode.Text,
                            this.textBox_itemBarcode.Text,
                            strConfirmItemRecPath,
                            this.Force,
                            this.OneReaderItemBarcodes,
                            strStyle,   //this.NoBiblioAndItemInfo == false ? "reader,item,biblio" : "reader",
                            strItemReturnFormats, // this.RenderFormat, // "html",
                            out item_records,   // strItemRecord,
                            this.PatronRenderFormat + ",xml", // "html",
                            out reader_records, // strReaderRecord,
                            strBiblioReturnFormats,
                            out biblio_records,
                            out aDupPath,
                            out strOutputReaderBarcode,
                            out borrow_info,
                            out strError);

                        if (reader_records != null && reader_records.Length > 0)
                            strReaderRecord = reader_records[0];

                        // ˢ�¶�����Ϣ
                        if (String.IsNullOrEmpty(strReaderRecord) == false)
                            SetReaderRenderString(ReplaceMacro(strReaderRecord));

                        // ��ʾ��Ŀ��ʵ����Ϣ
                        if (this.NoBiblioAndItemInfo == false)
                        {
                            string strItemRecord = "";
                            if (item_records != null && item_records.Length > 0)
                                strItemRecord = item_records[0];
                            if (String.IsNullOrEmpty(strItemRecord) == false)
                                this.SetItemRenderString(ReplaceMacro(strItemRecord));

                            string strBiblioRecord = "";
                            if (biblio_records != null && biblio_records.Length > 0)
                                strBiblioRecord = biblio_records[0];
                            if (String.IsNullOrEmpty(strBiblioRecord) == false)
                                this.SetBiblioRenderString(ReplaceMacro(strBiblioRecord));
                        }

                        string strItemXml = "";
                        if (this.MainForm.ChargingNeedReturnItemXml == true
                            && item_records != null)
                        {
                            Debug.Assert(item_records != null, "");

                            if (item_records.Length > 0)
                            {
                                // xml���������һ��
                                strItemXml = item_records[item_records.Length - 1];
                            }
                        }

                        if (lRet == -1)
                        {
                            // �������Ĳ������
                            this.m_itemBarcodes.Clear();

                            if (Channel.ErrorCode == DigitalPlatform.CirculationClient.localhost.ErrorCode.ItemBarcodeDup)
                            {
                                this.MainForm.PrepareSearch();

                                try
                                {
                                    ItemBarcodeDupDlg dupdlg = new ItemBarcodeDupDlg();
                                    MainForm.SetControlFont(dupdlg, this.Font, false);
                                    string strErrorNew = "";
                                    nRet = dupdlg.Initial(
                                        this.MainForm,
                                        aDupPath,
                                        "�������ŷ����ظ���" + strOperName + "�������ܾ���\r\n\r\n�ɸ��������г�����ϸ��Ϣ��ѡ���ʵ��Ĳ��¼�����Բ�����\r\n\r\nԭʼ������Ϣ:\r\n" + strError,
                                        this.MainForm.Channel,
                                        this.MainForm.Stop,
                                        out strErrorNew);
                                    if (nRet == -1)
                                    {
                                        // ��ʼ���Ի���ʧ��
                                        MessageBox.Show(this, strErrorNew);
                                        goto ERROR1;
                                    }

                                    this.MainForm.AppInfo.LinkFormState(dupdlg, "ChargingForm_dupdlg_state");
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
                        }

                        /*
                         * ���ڶ���Ĳ���? 2008/5/9 ȥ��
                        if (String.IsNullOrEmpty(strConfirmItemRecPath) == false
                            && this.NoBiblioAndItemInfo == false)
                        {
                            // ���Ĳ�����װ��׼ȷ�Ĳ��¼
                            string strError_1 = "";
                            int nRet_1 = LoadItemAndBiblioRecord("@path:" + strConfirmItemRecPath,
                                out strError_1);
                            if (nRet == -1)
                            {
                                strError_1 = "���¼ '" + strConfirmItemRecPath + "' û���ҵ�";
                                MessageBox.Show(this, strError);
                            }
                        }
                        */

                        DateTime end_time = DateTime.Now;


                        string strReaderSummary = "";
                        if (reader_records != null && reader_records.Length > 1)
                        {
                            /*
                            // 2012/1/5
                            // ���뻺��
                            this.MainForm.SetReaderXmlCache(strOutputReaderBarcode,
                                "",
                                reader_records[1]);
                             * */
                            strReaderSummary = Global.GetReaderSummary(reader_records[1]);
                        }

                        this.MainForm.OperHistory.BorrowAsync(
                            this,
                            bRenew,
                            strOutputReaderBarcode,
                            this.textBox_itemBarcode.Text,
                            strConfirmItemRecPath,
                            strReaderSummary,
                            strItemXml,
                            borrow_info,
                            start_time,
                            end_time);

                    }
                    finally
                    {
                        stop.EndLoop();
                        stop.OnStop -= new StopEventHandler(this.DoStop);
                        stop.Initial("");
                    }

                    // �ۻ�ͬһ���߽��ĳɹ��Ĳ������
                    this.oneReaderItemBarcodes.Add(this.textBox_itemBarcode.Text);

                    if (lRet == 1)
                    {
                        // ���ظ��������
                        strFastInputText = ChargingInfoDlg.Show(
                        this.CharingInfoHost,
                            strError.Replace("\r\n", "\r\n\r\n"),
                            InfoColor.Yellow,
                            strOperName,    // "caption",
                        this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                    }
                    else
                    {
                        if (this.GreenDisable == false)
                        {
                            strFastInputText = ChargingInfoDlg.Show(
                        this.CharingInfoHost,
                                strOperName + "�ɹ�",
                                InfoColor.Green,
                                strOperName,    // "caption",
                            this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                        }
                    }
                }
                else if (this.FuncState == FuncState.Return
                    || this.FuncState == FuncState.VerifyReturn
                    || this.FuncState == FuncState.Lost)
                {
                    string strAction = "";
                    string strLocation = "";    // ���صĲ�Ĺݲصص�

                    if (this.FuncState == FuncState.Return)
                        strAction = "return";
                    if (this.FuncState == FuncState.VerifyReturn)
                        strAction = "return";
                    if (this.FuncState == FuncState.Lost)
                        strAction = "lost";

                    Debug.Assert(strAction != "", "");

                    string strOperName = "";

                    if (this.FuncState == FuncState.Return)
                    {
                        strOperName = "����";
                    }
                    else if (this.FuncState == FuncState.VerifyReturn)
                    {
                        strOperName = "��֤����";
                    }
                    else
                    {
                        strOperName = "��ʧ";
                    }

                    // ����
                    stop.OnStop += new StopEventHandler(this.DoStop);
                    stop.Initial("���ڽ��� " + strOperName + " ����: " + this.textBox_readerBarcode.Text
                    + " �� " + this.textBox_itemBarcode.Text + " ...");
                    stop.BeginLoop();


                    // ??
                    // SetReaderRenderString("(��)");

                    if (this.NoBiblioAndItemInfo == false)
                    {
                        // �����Ŀ��ʵ����Ϣ
                        SetBiblioRenderString("(��)");
                        SetItemRenderString("(��)");
                    }


                    try
                    {
                        string strReaderRecord = "";
                        string strConfirmItemRecPath = null;
                        string strOutputReaderBarcode = "";

                    REDO:
                        string[] aDupPath = null;
                        string[] item_records = null;
                        string[] reader_records = null;
                        string[] biblio_records = null;

                        ReturnInfo return_info = null;

                        // item���صĸ�ʽ 2008/5/9
                        string strItemReturnFormats = "";
                        if (this.NoBiblioAndItemInfo == false)
                            strItemReturnFormats = this.RenderFormat;
                        if (this.MainForm.ChargingNeedReturnItemXml == true)
                        {
                            if (String.IsNullOrEmpty(strItemReturnFormats) == false)
                                strItemReturnFormats += ",";
                            strItemReturnFormats += "xml";
                        }


                        // biblio���صĸ�ʽ
                        string strBiblioReturnFormats = "";
                        if (this.NoBiblioAndItemInfo == false)
                            strBiblioReturnFormats = this.RenderFormat;

                        string strStyle = "reader";
                        if (this.NoBiblioAndItemInfo == false)
                            strStyle += ",item,biblio";
                        else if (this.MainForm.ChargingNeedReturnItemXml)
                            strStyle += ",item";

                        //if (this.MainForm.TestMode == true)
                        //    strStyle += ",testmode";

                        lRet = Channel.Return(
                            stop,
                            strAction,
                            this.textBox_readerBarcode.Text,
                            this.textBox_itemBarcode.Text,
                            strConfirmItemRecPath,
                            this.Force,
                            strStyle,   // this.NoBiblioAndItemInfo == false ? "reader,item,biblio" : "reader",
                            strItemReturnFormats,
                            out item_records,
                            this.PatronRenderFormat + ",xml", // "html",
                            out reader_records,
                            strBiblioReturnFormats,
                            out biblio_records,
                            out aDupPath,
                            out strOutputReaderBarcode,
                            out return_info,
                            out strError);
                        if (lRet == -1)
                        {
                            // �������Ĳ������
                            this.m_itemBarcodes.Clear();

                            if (Channel.ErrorCode == DigitalPlatform.CirculationClient.localhost.ErrorCode.ItemBarcodeDup)
                            {
                                this.MainForm.PrepareSearch();

                                try
                                {
                                    ItemBarcodeDupDlg dupdlg = new ItemBarcodeDupDlg();
                                    MainForm.SetControlFont(dupdlg, this.Font, false);
                                    string strErrorNew = "";
                                    nRet = dupdlg.Initial(
                                        this.MainForm,
                                        aDupPath,
                                        "�������ŷ����ظ������ز������ܾ���\r\n\r\n�ɸ��������г�����ϸ��Ϣ��ѡ���ʵ��Ĳ��¼�����Բ�����\r\n\r\nԭʼ������Ϣ:\r\n" + strError,
                                        this.MainForm.Channel,
                                        this.MainForm.Stop,
                                        out strErrorNew);
                                    if (nRet == -1)
                                    {
                                        // ��ʼ���Ի���ʧ��
                                        MessageBox.Show(this, strErrorNew);
                                        goto ERROR1;
                                    }

                                    this.MainForm.AppInfo.LinkFormState(dupdlg, "ChargingForm_dupdlg_state");
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
                        }

                        if (return_info != null)
                        {
                            strLocation = StringUtil.GetPureLocation(return_info.Location);
                        }

                        // ȷ������Ķ���֤�����
                        this.ActiveReaderBarcode = strOutputReaderBarcode;

                        if (reader_records != null && reader_records.Length > 0)
                            strReaderRecord = reader_records[0];

                        string strReaderSummary = "";
                        if (reader_records != null && reader_records.Length > 1)
                        {
                            /*
                            // 2012/1/5
                            // ���뻺��
                            this.MainForm.SetReaderXmlCache(strOutputReaderBarcode,
                                "",
                                reader_records[1]);
                             * */
                            strReaderSummary = Global.GetReaderSummary(reader_records[1]);
                        }

                        // ˢ�¶�����Ϣ
                        SetReaderRenderString(ReplaceMacro(strReaderRecord));

                        // ��ʾ��Ŀ��ʵ����Ϣ
                        if (this.NoBiblioAndItemInfo == false)
                        {
                            string strItemRecord = "";
                            if (item_records != null && item_records.Length > 0)
                                strItemRecord = item_records[0];
                            if (String.IsNullOrEmpty(strItemRecord) == false)
                                SetItemRenderString(ReplaceMacro(strItemRecord));

                            string strBiblioRecord = "";
                            if (biblio_records != null && biblio_records.Length > 0)
                                strBiblioRecord = biblio_records[0];
                            if (String.IsNullOrEmpty(strBiblioRecord) == false)
                                this.SetBiblioRenderString(ReplaceMacro(strBiblioRecord));

                            /*
                            {
                                // ��ǰ�ķ�����Ҫ����һ��API

                                string strError_1 = "";
                                int nRet_1 = 0;

                                if (String.IsNullOrEmpty(strConfirmItemRecPath) == false)
                                {
                                    nRet_1 = LoadItemAndBiblioRecord("@path:" + strConfirmItemRecPath,
                                         out strError_1);
                                }
                                else if (aDupPath != null && aDupPath.Length == 1)
                                {
                                    nRet_1 = LoadItemAndBiblioRecord("@path:" + aDupPath[0],
                                         out strError_1);
                                }
                                else
                                {
                                    nRet_1 = LoadItemAndBiblioRecord(this.textBox_itemBarcode.Text,
                                         out strError_1);
                                }

                                if (nRet_1 == -1)
                                    MessageBox.Show(this, strError_1);
                            }
                             * */
                        }

                        string strItemXml = "";
                        if (this.MainForm.ChargingNeedReturnItemXml == true
                            && item_records != null)
                        {
                            if (item_records.Length > 0)
                            {
                                // xml���������һ��
                                strItemXml = item_records[item_records.Length - 1];
                            }
                        }

                        DateTime end_time = DateTime.Now;

                        this.MainForm.OperHistory.ReturnAsync(
                            this,
                            this.FuncState == FuncState.Lost,
                            strOutputReaderBarcode, // this.textBox_readerBarcode.Text,
                            this.textBox_itemBarcode.Text,
                            strConfirmItemRecPath,
                            strReaderSummary,
                            strItemXml,
                            return_info,
                            start_time,
                            end_time);

                    }
                    finally
                    {
                        stop.EndLoop();
                        stop.OnStop -= new StopEventHandler(this.DoStop);
                        stop.Initial("");
                    }

                    if (lRet == 1)
                    {
                        // �������/����ԤԼ��/���ظ��������
                        strFastInputText = ChargingInfoDlg.Show(
                        this.CharingInfoHost,
                            strError.Replace("\r\n", "\r\n\r\n"),
                            InfoColor.Yellow,
                            strOperName,    // "caption",
                        this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                    }
                    else
                    {
                        if (this.GreenDisable == false)
                        {
                            string strText = "����ɹ�";
                            if (string.IsNullOrEmpty(strLocation) == false)
                                strText += "\r\n\r\n�ݲص�: " + strLocation;
                            strFastInputText = ChargingInfoDlg.Show(
                                this.CharingInfoHost,
                                strText,
                                InfoColor.Green,
                                strOperName,    // "caption",
                            this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                        }
                    }
                } // endif if ����
                else
                {
                    strFastInputText = ChargingInfoDlg.Show(
                        this.CharingInfoHost,
                        "�ݲ�֧��",
                        InfoColor.Red,
                        "caption",  // 
                        this.InfoDlgOpacity,
                        this.MainForm.DefaultFont);
                }

            }
            finally
            {
                EnableControls(true);
            }

            // ����ص��������������
            this.SwitchFocus(ITEM_BARCODE, strFastInputText);
            return 1;
        ERROR1:
            strFastInputText = ChargingInfoDlg.Show(
                this.CharingInfoHost,
                strError,
                InfoColor.Red,
                "caption",
                this.InfoDlgOpacity,
                this.MainForm.DefaultFont);
            EnableControls(true);

            // ����ص��������textbox
            /*
            this.textBox_itemBarcode.SelectAll();
            this.textBox_itemBarcode.Focus();
             * */
            this.SwitchFocus(ITEM_BARCODE, strFastInputText);
            return -1;
        }

        /*
        public const int READER_BARCODE = 0;
        public const int READER_PASSWORD = 1;
        public const int ITEM_BARCODE = 2;
         * */
        /// <summary>
        /// ��ʾ���ٲ����Ի���
        /// </summary>
        /// <param name="color">��Ϣ��ɫ</param>
        /// <param name="strCaption">�Ի����������</param>
        /// <param name="strMessage">��Ϣ��������</param>
        /// <param name="nTarget">�Ի���رպ�Ҫ�л�ȥ��λ�á�Ϊ READER_BARCODE READER_PASSWORD ITEM_BARCODE ֮һ</param>
        public void FastMessageBox(InfoColor color,
            string strCaption,
            string strMessage,
            int nTarget)
        {
            string strFastInputText = ChargingInfoDlg.Show(
                this.CharingInfoHost,
                strMessage,
                color,
                strCaption,
                this.InfoDlgOpacity,
                this.MainForm.DefaultFont);

            this.SwitchFocus(nTarget, strFastInputText);
        }

        // ��ť�ϵ������popupmenu: ��
        private void toolStripMenuItem_borrow_Click(object sender, EventArgs e)
        {
            // this.FuncState = FuncState.Borrow;
            SmartSetFuncState(FuncState.Borrow,
                false,
                false);
        }

        private void toolStripMenuItem_return_Click(object sender, EventArgs e)
        {
            // this.FuncState = FuncState.Return;
            SmartSetFuncState(FuncState.Return,
                false,
                false);

        }

        private void toolStripMenuItem_verifyReturn_Click(object sender, EventArgs e)
        {
            this.FuncState = FuncState.VerifyReturn;
        }

        private void toolStripMenuItem_renew_Click(object sender, EventArgs e)
        {
            this.FuncState = FuncState.VerifyRenew;
        }

        private void ToolStripMenuItem_lost_Click(object sender, EventArgs e)
        {
            this.FuncState = FuncState.Lost;
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.textBox_itemBarcode.Enabled = bEnable;
            this.button_itemAction.Enabled = bEnable;
            // this.checkBox_force.Enabled = bEnable;

#if NO
            if (this.VerifyReaderPassword == true)
            {
                this.textBox_readerPassword.Enabled = bEnable;
                this.button_verifyReaderPassword.Enabled = bEnable;
            }
#endif
            if (this.textBox_readerPassword.Visible == true)
            {
                this.textBox_readerPassword.Enabled = bEnable;
                this.button_verifyReaderPassword.Enabled = bEnable;
            }

            if (this.FuncState == FuncState.Return)
            {
                this.textBox_readerBarcode.Enabled = false;
                this.button_loadReader.Enabled = false;
            }
            else
            {
                this.textBox_readerBarcode.Enabled = bEnable;
                this.button_loadReader.Enabled = bEnable;
            }

        }

        /// <summary>
        /// ����������������
        /// </summary>
        public string ItemBarcode
        {
            get
            {
                return this.textBox_itemBarcode.Text;
            }
            set
            {
                this.textBox_itemBarcode.Text = value;
            }
        }

        /// <summary>
        /// ����֤���������������
        /// </summary>
        public string ReaderBarcode
        {
            get
            {
                return this.textBox_readerBarcode.Text;
            }
            set
            {
                this.textBox_readerBarcode.Text = value;
            }
        }

        private void button_verifyReaderPassword_Click(object sender, EventArgs e)
        {
            string strError = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����У��������� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            this.EnableControls(false);

            try
            {
                long lRet = Channel.VerifyReaderPassword(
                    stop,
                    this.textBox_readerBarcode.Text,
                    this.textBox_readerPassword.Text,
                    out strError);
                if (lRet == 0)
                {
                    goto ERROR1;
                }
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                this.EnableControls(true);

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            /*
            this.textBox_itemBarcode.SelectAll();
            this.textBox_itemBarcode.Focus();
             * */
            // У����ȷ
            this.SwitchFocus(ITEM_BARCODE, null);
            return;
        ERROR1:
            // У�鷢��/��������
            string strFastInputText = ChargingInfoDlg.Show(
                        this.CharingInfoHost,
                strError,
                InfoColor.Red,
                "��֤����֤����",
                this.InfoDlgOpacity,
                true,
                        this.MainForm.DefaultFont);
            // �������¶�λ������������
            /*
            this.textBox_readerPassword.Focus();
            this.textBox_readerPassword.SelectAll();
             * */
            this.SwitchFocus(READER_PASSWORD, strFastInputText);
        }

#if NO
        // ������ �Ƿ���Ҫ У���������
        bool VerifyReaderPassword
        {
            get
            {
                return m_bVerifyReaderPassword;
            }
            set
            {
                this.m_bVerifyReaderPassword = value;
                this.MenuItem_verifyReaderPassword.Checked = value;

                if (m_bVerifyReaderPassword == false)
                {
                    this.textBox_readerPassword.Enabled = false;
                    this.button_verifyReaderPassword.Enabled = false;
                }
                else
                {
                    this.textBox_readerPassword.Enabled = true;
                    this.button_verifyReaderPassword.Enabled = true;
                }
            }
        }
#endif
        bool VerifyReaderPassword
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "verify_reader_password",
                    false);
            }
            set
            {
                this.MainForm.AppInfo.SetBoolean(
                    "charging_form",
                    "verify_reader_password",
                    value);
            }
        }

        /// <summary>
        /// �Զ�����Ψһ����
        /// </summary>
        public bool AutoOperSingleItem
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "auto_oper_single_item",
                    false);
            }
        }

        /// <summary>
        /// �Ƿ����� ISBN ���黹�鹦��
        /// </summary>
        public bool UseIsbnBorrow
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "isbn_borrow",
                    true);
            }
        }


        /// <summary>
        /// ������Ϣ�в���ʾ������ʷ
        /// </summary>
        public bool NoBorrowHistory
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "no_borrow_history",
                    true);
            }
            set
            {
                this.MainForm.AppInfo.SetBoolean(
                    "charging_form",
                    "no_borrow_history",
                    value);
            }
        }

#if NO
        private void MenuItem_verifyReaderPassword_Click(object sender, EventArgs e)
        {
            if (this.VerifyReaderPassword == true)
            {
                this.VerifyReaderPassword = false;
            }
            else
            {
                this.VerifyReaderPassword = true;
            }
        }
#endif

        /*
        private void button_testCookie_Click(object sender, EventArgs e)
        {
            string strURL = "http://localhost";
            Uri UriURL = new Uri(strURL);
            string strCookie = RetrieveIECookiesForUrl(UriURL.AbsoluteUri);

            MessageBox.Show(this, strCookie);
        }

        private static string RetrieveIECookiesForUrl(string url)
        {
            url = "";
            StringBuilder cookieHeader = new StringBuilder(new String(' ',
    256), 256);
            int datasize = cookieHeader.Length;
            if (!API.InternetGetCookie(url, null, cookieHeader, ref datasize))
            {
                if (datasize < 0)
                    return String.Empty;
                cookieHeader = new StringBuilder(datasize); // resize with new datasize 
                API.InternetGetCookie(url, null, cookieHeader, ref datasize);
            }
            return cookieHeader.ToString(); 

        }
         * */



        // �޸Ĵ��ڱ���
        void UpdateWindowTitle()
        {
            this.Text = "���� " + this.textBox_readerBarcode.Text + " " + this.textBox_itemBarcode.Text;
        }

        private void textBox_readerBarcode_TextChanged(object sender, EventArgs e)
        {
            // �������Ĳ������
            this.m_itemBarcodes.Clear();

            this.UpdateWindowTitle();

        }

        private void ChargingForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;

            this.MainForm.toolButton_refresh.Enabled = true;
        }

        // 2008/10/31 new add
        ChargingInfoHost m_chargingInfoHost = null;

        /// <summary>
        /// ��� ChargingInfoHost ����
        /// </summary>
        internal ChargingInfoHost CharingInfoHost
        {
            get
            {
                if (this.m_chargingInfoHost == null)
                {
                    m_chargingInfoHost = new ChargingInfoHost();
                    m_chargingInfoHost.ap = MainForm.AppInfo;
                    m_chargingInfoHost.window = this;
                    if (this.StopFillingWhenCloseInfoDlg == true)
                    {
                        m_chargingInfoHost.StopGettingSummary -= new EventHandler(m_chargingInfoHost_StopGettingSummary);
                        m_chargingInfoHost.StopGettingSummary += new EventHandler(m_chargingInfoHost_StopGettingSummary);
                    }
                }
                else
                {
                    if (this.StopFillingWhenCloseInfoDlg == false)
                    {
                        m_chargingInfoHost.StopGettingSummary -= new EventHandler(m_chargingInfoHost_StopGettingSummary);
                    }
                    else
                    {
                        m_chargingInfoHost.StopGettingSummary -= new EventHandler(m_chargingInfoHost_StopGettingSummary);
                        m_chargingInfoHost.StopGettingSummary += new EventHandler(m_chargingInfoHost_StopGettingSummary);
                    }
                }

                return m_chargingInfoHost;
            }
        }

        void m_chargingInfoHost_StopGettingSummary(object sender, EventArgs e)
        {
            if (this.m_webExternalHost_readerInfo != null)
                this.m_webExternalHost_readerInfo.StopPrevious();
            this.webBrowser_reader.Stop();
            if (this.m_webExternalHost_itemInfo != null)
                this.m_webExternalHost_itemInfo.StopPrevious();
            this.webBrowser_item.Stop();
            if (this.m_webExternalHost_biblioInfo != null)
                this.m_webExternalHost_biblioInfo.StopPrevious();
            this.webBrowser_biblio.Stop();
        }

        /// <summary>
        /// �Ƿ�Ҫ�Զ��������Сд�ַ�ת��Ϊ��д
        /// </summary>
        public bool AutoToUpper
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                "charging_form",
                "auto_toupper_barcode",
                false);
            }
        }

        #region ����֤����ſ��ٵ����˵�����

        private void ToolStripMenuItem_naviToAmerceForm_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.ActiveReaderBarcode) == true)
            {
                MessageBox.Show(this, "��ǰ��û�л�Ķ���֤�����");
                return;
            }

            AmerceForm form = this.MainForm.EnsureAmerceForm();
            Global.Activate(form);

            form.LoadReader(this.ActiveReaderBarcode, true);
        }

        private void ToolStripMenuItem_naviToReaderInfoForm_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.ActiveReaderBarcode) == true)
            {
                MessageBox.Show(this, "��ǰ��û�л�Ķ���֤�����");
                return;
            }

            ReaderInfoForm form = this.MainForm.EnsureReaderInfoForm();
            Global.Activate(form);

            form.LoadRecord(this.ActiveReaderBarcode,
                false);
        }

        private void ToolStripMenuItem_naviToActivateForm_old_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.ActiveReaderBarcode) == true)
            {
                MessageBox.Show(this, "��ǰ��û�л�Ķ���֤�����");
                return;
            }

            ActivateForm form = this.MainForm.EnsureActivateForm();
            Global.Activate(form);

            form.LoadOldRecord(this.ActiveReaderBarcode);
        }

        private void ToolStripMenuItem_naviToActivateForm_new_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.ActiveReaderBarcode) == true)
            {
                MessageBox.Show(this, "��ǰ��û�л�Ķ���֤�����");
                return;
            }

            ActivateForm form = this.MainForm.EnsureActivateForm();
            Global.Activate(form);

            form.LoadNewRecord(this.ActiveReaderBarcode);
        }

        private void ToolStripMenuItem_openReaderManageForm_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.ActiveReaderBarcode) == true)
            {
                MessageBox.Show(this, "��ǰ��û�л�Ķ���֤�����");
                return;
            }

            ReaderManageForm form = this.MainForm.EnsureReaderManageForm();
            Global.Activate(form);

            form.LoadRecord(this.ActiveReaderBarcode);
        }

        #endregion

        #region ������ſ��ٵ����˵�����


        private void ToolStripMenuItem_openEntityForm_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.ActiveItemBarcode) == true)
            {
                MessageBox.Show(this, "��ǰ��û�л�Ĳ������");
                return;
            }

            EntityForm form = this.MainForm.EnsureEntityForm();
            Global.Activate(form);

            form.LoadItemByBarcode(this.ActiveItemBarcode, false);
        }

        private void ToolStripMenuItem_openItemInfoForm_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.ActiveItemBarcode) == true)
            {
                MessageBox.Show(this, "��ǰ��û�л�Ĳ������");
                return;
            }

            ItemInfoForm form = this.MainForm.EnsureItemInfoForm();
            Global.Activate(form);

            form.LoadRecord(this.ActiveItemBarcode);
        }

        #endregion

        private void webBrowser_reader_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            ((WebBrowser)sender).Document.Window.Error -= new HtmlElementErrorEventHandler(Window_Error);
            ((WebBrowser)sender).Document.Window.Error += new HtmlElementErrorEventHandler(Window_Error);
        }

        private void ChargingForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Text"))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;

        }

        private void ChargingForm_DragDrop(object sender, DragEventArgs e)
        {
            string strError = "";

            string strWhole = (String)e.Data.GetData("Text");

            string[] lines = strWhole.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 1)
            {
                strError = "��һ��Ҳ������";
                goto ERROR1;
            }

            if (lines.Length > 1)
            {
                strError = "���ɴ�ֻ��������һ����¼";
                goto ERROR1;
            }

            string strFirstLine = lines[0].Trim();

            // ȡ��recpath
            string strRecPath = "";
            int nRet = strFirstLine.IndexOf("\t");
            if (nRet == -1)
                strRecPath = strFirstLine;
            else
                strRecPath = strFirstLine.Substring(0, nRet).Trim();

            // �ж����ǲ��Ƕ��߼�¼·��
            string strDbName = Global.GetDbName(strRecPath);

            if (this.MainForm.IsReaderDbName(strDbName) == true)
            {
                string[] parts = strFirstLine.Split(new char[] { '\t' });
                string strReaderBarcode = "";
                if (parts.Length >= 2)
                    strReaderBarcode = parts[1].Trim();

                if (String.IsNullOrEmpty(strReaderBarcode) == false)
                {
                    // this.CurrentReaderBarcode = strReaderBarcode;

                    this.textBox_readerBarcode.Text = strReaderBarcode;
                    button_loadReader_Click(this, null);
                }
            }
            else
            {
                strError = "��¼·�� '" + strRecPath + "' �е����ݿ������Ƕ��߿���...";
                goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void textBox_itemBarcode_TextChanged(object sender, EventArgs e)
        {
            // �޸Ĵ��ڱ���
            this.UpdateWindowTitle();
        }

        /// <summary>
        /// ����Ի����
        /// </summary>
        /// <param name="keyData">System.Windows.Forms.Keys ֵ֮һ������ʾҪ����ļ���</param>
        /// <returns>����ؼ�����ʹ�û�������Ϊ true������Ϊ false���������һ������</returns>
        protected override bool ProcessDialogKey(
Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }

            if (keyData == Keys.F5)
            {
                this.Reload();
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        private void webBrowser_biblio_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            ((WebBrowser)sender).Document.Window.Error -= new HtmlElementErrorEventHandler(Window_Error);
            ((WebBrowser)sender).Document.Window.Error += new HtmlElementErrorEventHandler(Window_Error);
        }

        private void webBrowser_item_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            ((WebBrowser)sender).Document.Window.Error -= new HtmlElementErrorEventHandler(Window_Error);
            ((WebBrowser)sender).Document.Window.Error += new HtmlElementErrorEventHandler(Window_Error);
        }

        void Window_Error(object sender, HtmlElementErrorEventArgs e)
        {
            if (this.MainForm.SuppressScriptErrors == true)
                e.Handled = true;
        }

#if NO
        bool m_bSuppressScriptErrors = true;
        public bool SuppressScriptErrors
        {
            get
            {
                return this.m_bSuppressScriptErrors;
            }
            set
            {
                this.m_bSuppressScriptErrors = value;
            }
        }
#endif

        /// <summary>
        /// �Ƿ�Ҫ�ڹر���Ϣ�Ի����ʱ���Զ�ֹͣ���
        /// </summary>
        public bool StopFillingWhenCloseInfoDlg
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
    "charging_form",
    "stop_filling_when_close_infodlg",
    true);
            }
        }

        /// <summary>
        /// ֤�����������Ƿ��������뺺��
        /// </summary>
        public bool PatronBarcodeAllowHanzi
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_form",
                    "patron_barcode_allow_hanzi",
                    false);
            }
        }


    }

    /// <summary>
    /// ��������
    /// </summary>
    public enum FuncState
    {
        /// <summary>
        /// �Զ����ȿ��Խ��飬Ҳ���Ի���
        /// </summary>
        Auto = 0,   // �ȿ��Խ��飬Ҳ���Ի���
        /// <summary>
        /// ����
        /// </summary>
        Borrow = 1, // ����
        /// <summary>
        /// ����(����֤)
        /// </summary>
        Return = 2, // ����(����֤)
        /// <summary>
        /// ����(Ҫ��֤)
        /// </summary>
        VerifyReturn = 3, // ����(Ҫ��֤)
        /// <summary>
        /// ����(����֤)
        /// </summary>
        Renew = 4,  // ����
        /// <summary>
        /// ����(Ҫ��֤)
        /// </summary>
        VerifyRenew = 5,  // ����
        /// <summary>
        /// ��ʧ����
        /// </summary>
        Lost = 6,   // ��ʧ

        /// <summary>
        /// ��֤��ʧ
        /// </summary>
        VerifyLost = 7, // ��֤��ʧ
        /// <summary>
        /// װ�ض�����Ϣ
        /// </summary>
        LoadPatronInfo = 8, // װ�ض�����Ϣ
        /// <summary>
        /// ͬһ���߼�����
        /// </summary>
        ContinueBorrow = 9, // ͬһ���߼�������
    }

    /*public*/ class BarcodeAndTime
    {
        public string Barcode = "";
        public DateTime Time = DateTime.Now;
    }

    /// <summary>
    /// ��ʾ״̬
    /// </summary>
    public enum DisplayState
    {
        /// <summary>
        /// HTML ��ʽ
        /// </summary>
        HTML = 0, // html��ʽ
        /// <summary>
        /// ���ı���ʽ
        /// </summary>
        TEXT = 1,   // ���ı���ʽ
    }

    /// <summary>
    /// ������Ϣ������
    /// </summary>
    internal class ChargingInfoHost
    {
        /// <summary>
        /// ApplicationInfo
        /// </summary>
        public ApplicationInfo ap = null;
        /// <summary>
        /// ��������
        /// </summary>
        public IWin32Window window = null;

        // 
        /// <summary>
        /// ֹͣ��ȡժҪ�Ķ���
        /// </summary>
        public event EventHandler StopGettingSummary = null;

        /// <summary>
        /// ��Ӧֹͣ��ȡժҪ�Ķ���
        /// </summary>
        /// <param name="sender">������</param>
        /// <param name="e">�¼�����</param>
        public void OnStopGettingSummary(object sender, EventArgs e)
        {
            if (this.StopGettingSummary != null)
                this.StopGettingSummary(sender, e);
        }

    }
}