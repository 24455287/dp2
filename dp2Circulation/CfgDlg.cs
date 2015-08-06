using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

using DigitalPlatform.Xml;
using DigitalPlatform.Script;
using DigitalPlatform.IO;
using DigitalPlatform.CommonControl;
using DigitalPlatform.CirculationClient;

namespace dp2Circulation
{
    /// <summary>
    /// ϵͳ�������� �Ի���
    /// </summary>
    internal partial class CfgDlg : Form
    {
        /// <summary>
        /// ���ò����仯���¼�
        /// </summary>
        public event ParamChangedEventHandler ParamChanged = null;

        public ApplicationInfo ap = null;

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        bool m_bServerCfgChanged = false; // ������������Ϣ�޸Ĺ�

        public CfgDlg()
        {
            InitializeComponent();
        }

        public string UiState
        {
            get
            {
                List<object> controls = new List<object>();
                controls.Add(this.tabControl_main);
                return GuiState.GetUiState(controls);
            }
            set
            {
                List<object> controls = new List<object>();
                controls.Add(this.tabControl_main);
                GuiState.SetUiState(controls, value);
            }
        }

        private void CfgDlg_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null
                && !(Control.ModifierKeys == Keys.Control))
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

            // serverurl
            this.textBox_server_dp2LibraryServerUrl.Text =
                ap.GetString("config",
                "circulation_server_url",
                "http://localhost:8001/dp2library");

            // author number GCAT serverurl
            this.textBox_server_authorNumber_gcatUrl.Text =
                ap.GetString("config",
                "gcat_server_url",
                "http://dp2003.com/gcatserver/");  // "http://dp2003.com/dp2libraryws/gcat.asmx"

            // pinyin serverurl
            this.textBox_server_pinyin_gcatUrl.Text =
                ap.GetString("config",
                "pinyin_server_url",
                "http://dp2003.com/gcatserver/");    // 

            // dp2MServer URL
            this.textBox_server_dp2MServerUrl.Text =
                ap.GetString("config",
                "im_server_url",
                "http://dp2003.com/dp2MServer");


            // default account
            this.textBox_defaultAccount_userName.Text =
                ap.GetString(
                "default_account",
                "username",
                "");



            this.checkBox_defaulAccount_savePasswordShort.Checked =
                ap.GetBoolean(
                "default_account",
                "savepassword_short",
                false);
            this.checkBox_defaulAccount_savePasswordLong.Checked =
    ap.GetBoolean(
    "default_account",
    "savepassword_long",
    false);

            if (this.checkBox_defaulAccount_savePasswordShort.Checked == true
                || this.checkBox_defaulAccount_savePasswordLong.Checked == true)
            {
                string strPassword = ap.GetString(
        "default_account",
        "password",
        "");
                strPassword = this.MainForm.DecryptPasssword(strPassword);
                this.textBox_defaultAccount_password.Text = strPassword;
            }


            this.checkBox_defaultAccount_isReader.Checked =
                ap.GetBoolean(
                "default_account",
                "isreader",
                false);
            this.textBox_defaultAccount_location.Text =
                ap.GetString(
                "default_account",
                "location",
                "");
            this.checkBox_defaultAccount_occurPerStart.Checked = ap.GetBoolean(
                "default_account",
                "occur_per_start",
                true);


            // *** charging
            this.checkBox_charging_force.Checked = ap.GetBoolean(
                    "charging_form",
                    "force",
                    false);
            this.numericUpDown_charging_infoDlgOpacity.Value = ap.GetInt(
                "charging_form",
                "info_dlg_opacity",
                100);
            this.checkBox_charging_verifyBarcode.Checked = ap.GetBoolean(
                "charging_form",
                "verify_barcode",
                false);

            this.checkBox_charging_doubleItemInputAsEnd.Checked = ap.GetBoolean(
                "charging_form",
                "doubleItemInputAsEnd",
                false);

            this.comboBox_charging_displayFormat.Text =
    ap.GetString("charging_form",
    "display_format",
    "HTML");

            this.checkBox_charging_autoUppercaseBarcode.Checked =
                ap.GetBoolean(
                "charging_form",
                "auto_toupper_barcode",
                false);

            this.checkBox_charging_greenInfoDlgNotOccur.Checked =
                ap.GetBoolean(
                "charging_form",
                "green_infodlg_not_occur",
                false);


            this.checkBox_charging_stopFillingWhenCloseInfoDlg.Checked =
    ap.GetBoolean(
    "charging_form",
    "stop_filling_when_close_infodlg",
    true);

            this.checkBox_charging_noBiblioAndItem.Checked =
                ap.GetBoolean(
                "charging_form",
                "no_biblio_and_item_info",
                false);

            this.checkBox_charging_autoSwitchReaderBarcode.Checked =
                ap.GetBoolean(
                "charging_form",
                "auto_switch_reader_barcode",
                false);

            // �Զ���������������
            // 2008/9/26
            this.checkBox_charging_autoClearTextbox.Checked = ap.GetBoolean(
                "charging_form",
                "autoClearTextbox",
                true);

            // ���ö���������֤
            this.checkBox_charging_veifyReaderPassword.Checked = ap.GetBoolean(
                "charging_form",
                "verify_reader_password",
                false);

            // �ʶ���������
            this.checkBox_charging_speakNameWhenLoadReaderRecord.Checked = ap.GetBoolean(
                "charging_form",
                "speak_reader_name",
                false);

            // ֤�����������������뺺��
            this.checkBox_charging_patronBarcodeAllowHanzi.Checked = ap.GetBoolean(
                "charging_form",
                "patron_barcode_allow_hanzi",
                false);

            // ������Ϣ�в���ʾ������ʷ
            this.checkBox_charging_noBorrowHistory.Checked = ap.GetBoolean(
                "charging_form",
                "no_borrow_history",
                true);
            if (this.MainForm.ServerVersion < 2.20)
                this.checkBox_charging_noBorrowHistory.Enabled = false;

            // ���� ISBN ���黹�鹦��
            this.checkBox_charging_isbnBorrow.Checked = ap.GetBoolean(
                "charging_form",
                "isbn_borrow",
                true);

            // �Զ�����Ψһ����
            this.checkBox_charging_autoOperItemDialogSingleItem.Checked = ap.GetBoolean(
                "charging_form",
                "auto_oper_single_item",
                false);

            // *** ��ݳ���

            this.comboBox_quickCharging_displayFormat.Text =
ap.GetString("quickcharging_form",
"display_format",
"HTML");

            // ��֤�����
            this.checkBox_quickCharging_verifyBarcode.Checked = ap.GetBoolean(
    "quickcharging_form",
    "verify_barcode",
    false);
            // ������Ϣ�в���ʾ������ʷ
            this.checkBox_quickCharging_noBorrowHistory.Checked = ap.GetBoolean(
                "quickcharging_form",
                "no_borrow_history",
                true);
            if (this.MainForm.ServerVersion < 2.20)
                this.checkBox_quickCharging_noBorrowHistory.Enabled = false;

            // �ʶ���������
            this.checkBox_quickCharging_speakNameWhenLoadReaderRecord.Checked = ap.GetBoolean(
                "quickcharging_form",
                "speak_reader_name",
                false);

            // �ʶ�����
            this.checkBox_quickCharging_speakBookTitle.Checked = ap.GetBoolean(
                "quickcharging_form",
                "speak_book_title",
                false);

            // ���� ISBN ���黹�鹦��
            this.checkBox_quickCharging_isbnBorrow.Checked = ap.GetBoolean(
                "quickcharging_form",
                "isbn_borrow",
                true);

            // �Զ�����Ψһ����
            this.checkBox_quickCharging_autoOperItemDialogSingleItem.Checked = ap.GetBoolean(
                "quickcharging_form",
                "auto_oper_single_item",
                false);

            // *** �ֲᴰ
            this.checkBox_itemManagement_verifyItemBarcode.Checked = ap.GetBoolean(
                "entity_form",
                "verify_item_barcode",
                false);

            this.checkBox_itemManagement_cataloging.Checked = ap.GetBoolean(
                "entity_form",
                "cataloging",
                true);  // 2007/12/2 �޸�Ϊ true

            this.checkBox_itemManagement_searchDupWhenSaving.Checked = ap.GetBoolean(
                "entity_form",
                "search_dup_when_saving",
                false);

            this.checkBox_itemManagement_verifyDataWhenSaving.Checked = ap.GetBoolean(
    "entity_form",
    "verify_data_when_saving",
    false);

            this.checkBox_itemManagement_showQueryPanel.Checked = ap.GetBoolean(
"entityform",
"queryPanel_visibie",
true);
            this.checkBox_itemManagement_showItemQuickInputPanel.Checked = ap.GetBoolean(
"entityform",
"itemQuickInputPanel_visibie",
true);

            // ������Ŀ��¼��ʾΪֻ��״̬
            this.checkBox_itemManagement_linkedRecordReadonly.Checked = ap.GetBoolean(
"entityform",
"linkedRecordReadonly",
true);

            // ��ʾ�����ֹݵĲ��¼
            this.checkBox_itemManagement_displayOtherLibraryItem.Checked = ap.GetBoolean(
"entityform",
"displayOtherLibraryItem",
false);

            // �Զ��޶�paste�����ͼ����
            this.textBox_itemManagement_maxPicWidth.Text = this.MainForm.AppInfo.GetString(
    "entityform",
    "paste_pic_maxwidth",
    "-1");

            // ui
            // ͣ��
            this.comboBox_ui_fixedPanelDock.Text = this.MainForm.panel_fixed.Dock.ToString();

            this.checkBox_ui_hideFixedPanel.Checked = ap.GetBoolean(
                "MainForm",
                "hide_fixed_panel",
                false);

            this.textBox_ui_defaultFont.Text = ap.GetString(
    "Global",
    "default_font",
    "");

            // passgate
            // ��ݵǼ�
            this.numericUpDown_passgate_maxListItemsCount.Value = ap.GetInt(
                "passgate_form",
                "max_list_items_count",
                1000);

            // search
            this.checkBox_search_useExistDetailWindow.Checked = ap.GetBoolean(
                "all_search_form",
                "load_to_exist_detailwindow",
                true);

            this.numericUpDown_search_maxBiblioResultCount.Value = ap.GetInt(
                "biblio_search_form",
                "max_result_count",
                -1);

            this.checkBox_search_hideBiblioMatchStyle.Checked = ap.GetBoolean(
                "biblio_search_form",
                "hide_matchstyle",
                false);

            // 2008/1/20 
            this.checkBox_search_biblioPushFilling.Checked = ap.GetBoolean(
                "biblio_search_form",
                "push_filling_browse",
                false);

            this.numericUpDown_search_maxReaderResultCount.Value = ap.GetInt(
                "reader_search_form",
                "max_result_count",
                -1);

            this.checkBox_search_hideReaderMatchStyle.Checked = ap.GetBoolean(
                "reader_search_form",
                "hide_matchstyle",
                false);

            // 2008/1/20 
            this.checkBox_search_readerPushFilling.Checked = ap.GetBoolean(
                "reader_search_form",
                "push_filling_browse",
                false);

            // ---
            this.numericUpDown_search_maxItemResultCount.Value = ap.GetInt(
                "item_search_form",
                "max_result_count",
                -1);


            // 2008/11/21 
            this.checkBox_search_hideItemMatchStyleAndDbName.Checked = ap.GetBoolean(
                "item_search_form",
                "hide_matchstyle_and_dbname",
                true);

            // 2008/1/20 
            this.checkBox_search_itemPushFilling.Checked = ap.GetBoolean(
                "item_search_form",
                "push_filling_browse",
                false);

            // --- order
            this.numericUpDown_search_maxOrderResultCount.Value = ap.GetInt(
    "order_search_form",
    "max_result_count",
    -1);

            this.checkBox_search_hideOrderMatchStyleAndDbName.Checked = ap.GetBoolean(
                "order_search_form",
                "hide_matchstyle_and_dbname",
                true);

            this.checkBox_search_orderPushFilling.Checked = ap.GetBoolean(
                "order_search_form",
                "push_filling_browse",
                false);

            // --- issue
            this.numericUpDown_search_maxIssueResultCount.Value = ap.GetInt(
    "issue_search_form",
    "max_result_count",
    -1);

            this.checkBox_search_hideIssueMatchStyleAndDbName.Checked = ap.GetBoolean(
                "issue_search_form",
                "hide_matchstyle_and_dbname",
                true);

            this.checkBox_search_issuePushFilling.Checked = ap.GetBoolean(
                "issue_search_form",
                "push_filling_browse",
                false);

            // --- comment
            this.numericUpDown_search_maxCommentResultCount.Value = ap.GetInt(
    "comment_search_form",
    "max_result_count",
    -1);

            this.checkBox_search_hideCommentMatchStyleAndDbName.Checked = ap.GetBoolean(
                "comment_search_form",
                "hide_matchstyle_and_dbname",
                true);

            this.checkBox_search_commentPushFilling.Checked = ap.GetBoolean(
                "comment_search_form",
                "push_filling_browse",
                false);

            // ƾ����ӡ
            this.comboBox_print_prnPort.Text =
                ap.GetString("charging_print",
                "prnPort",
                "LPT1");

            this.checkBox_print_pausePrint.Checked = ap.GetBoolean(
                "charging_print",
                "pausePrint",
                false);

            this.textBox_print_projectName.Text = ap.GetString(
                "charging_print",
                "projectName",
                "");

            //
            this.label_print_projectNameMessage.Text = "";

            // amerce
            this.comboBox_amerce_interface.Text =
                ap.GetString("config",
                "amerce_interface",
                "<��>");

            // ���Ѵ�����
            this.comboBox_amerce_layout.Text =
    ap.GetString("amerce_form",
    "layout",
    "���ҷֲ�");


            // accept
            this.checkBox_accept_singleClickLoadDetail.Checked =
                ap.GetBoolean(
                "accept_form",
                "single_click_load_detail",
                false);

            // *** ������

            // ���֤������URL
            this.textBox_cardReader_idcardReaderUrl.Text =
    ap.GetString("cardreader",
    "idcardReaderUrl",
    "");  // ����ֵ "ipc://IdcardChannel/IdcardServer"

            // *** ָ��

            // ָ���Ķ���URL
            this.textBox_fingerprint_readerUrl.Text =
                ap.GetString("fingerprint",
                "fingerPrintReaderUrl",
                "");    // ����ֵ "ipc://FingerprintChannel/FingerprintServer"

            // ָ�ƴ����ʻ� �û���
            this.textBox_fingerprint_userName.Text =
    ap.GetString("fingerprint",
    "userName",
    ""); 
            // ָ�ƴ����ʻ� ����
            {
                string strPassword = ap.GetString("fingerprint",
                "password",
                "");
                strPassword = this.MainForm.DecryptPasssword(strPassword);
                this.textBox_fingerprint_password.Text = strPassword;
            }

            // *** ����

            // �Զ����� �����ֶ����Ի���ʱ
            this.checkBox_patron_autoRetryReaderCard.Checked =
                ap.GetBoolean(
                "reader_info_form",
                "autoretry_readcarddialog",
                true);

            // ���� �����֤����������� �Ի���
            this.checkBox_patron_displaySetReaderBarcodeDialog.Checked =
                ap.GetBoolean(
                "reader_info_form",
                "display_setreaderbarcode_dialog",
                true);

            // У������������
            this.checkBox_patron_verifyBarcode.Checked = ap.GetBoolean(
    "reader_info_form",
    "verify_barcode",
    false);

            // �ڶ��ߴ���Χ���Զ��ر� ���֤������ ���̷���(&S)
            this.checkBox_patron_disableIdcardReaderKeyboardSimulation.Checked = ap.GetBoolean(
    "reader_info_form",
    "disable_idcardreader_sendkey",
    true);

            // ��־
            // ��ʾ���߽�����ʷ
            this.checkBox_operLog_displayReaderBorrowHistory.Checked =
                ap.GetBoolean(
                "operlog_form",
                "display_reader_borrow_history",
                true);
            // ��ʾ�������ʷ
            this.checkBox_operLog_displayItemBorrowHistory.Checked =
                ap.GetBoolean(
                "operlog_form",
                "display_item_borrow_history",
                true);
            // �Զ�������־�ļ�
            this.checkBox_operLog_autoCache.Checked =
                ap.GetBoolean(
                "global",
                "auto_cache_operlogfile",
                true);

            // ��־��ϸ����
            this.comboBox_operLog_level.Text =
                ap.GetString(
                "operlog_form",
                "level",
                "1 -- ����");

            // ȫ��
            // ������ؼ�����ű�����Ի���(&S)
            this.checkBox_global_displayScriptErrorDialog.Checked =
                ap.GetBoolean(
                "global",
                "display_webbrowsecontrol_scripterror_dialog",
                false);

            // ��ƴ��ʱ�Զ�ѡ�������
            this.checkBox_global_autoSelPinyin.Checked =
                ap.GetBoolean(
                "global",
                "auto_select_pinyin",
                false);

            // *** ��ǩ��ӡ
            // �Ӻδ���ȡ��ȡ��
            this.comboBox_labelPrint_accessNoSource.Text = ap.GetString(
                "labelprint",
                "accessNo_source",
                "�Ӳ��¼");

            // *** ��Ϣ

            // ������Ŀ����
            this.checkBox_message_shareBiblio.Checked = ap.GetBoolean(
                "message",
                "share_biblio",
                false);

            checkBox_charging_isbnBorrow_CheckedChanged(this, null);
            checkBox_quickCharging_isbnBorrow_CheckedChanged(this, null);

            this.m_bServerCfgChanged = false;
        }

        private void CfgDlg_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void CfgDlg_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        void FireParamChanged(string strSection,
            string strEntry,
            object value)
        {
            if (this.ParamChanged != null)
            {
                ParamChangedEventArgs e = new ParamChangedEventArgs();
                e.Section = strSection;
                e.Entry = strEntry;
                e.Value = value;
                this.ParamChanged(this, e);
            }
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            // serverurl
            ap.SetString("config",
                "circulation_server_url",
                this.textBox_server_dp2LibraryServerUrl.Text);

            // author number GCAT serverurl
            ap.SetString("config",
                "gcat_server_url",
                this.textBox_server_authorNumber_gcatUrl.Text);

            // pinyin serverurl
            ap.SetString("config",
                "pinyin_server_url",
                this.textBox_server_pinyin_gcatUrl.Text);

            // dp2MServer URL
            ap.SetString("config",
                "im_server_url",
                this.textBox_server_dp2MServerUrl.Text);

            // default account

            ap.SetString(
                "default_account",
                "username",
                this.textBox_defaultAccount_userName.Text);


            ap.SetBoolean(
                "default_account",
                "savepassword_short",
                this.checkBox_defaulAccount_savePasswordShort.Checked);

            ap.SetBoolean(
    "default_account",
    "savepassword_long",
    this.checkBox_defaulAccount_savePasswordLong.Checked);

            if (this.checkBox_defaulAccount_savePasswordShort.Checked == true
                || this.checkBox_defaulAccount_savePasswordLong.Checked == true)
            {
                string strPassword = this.MainForm.EncryptPassword(this.textBox_defaultAccount_password.Text);
                ap.SetString(
                    "default_account",
                    "password",
                    strPassword);
            }
            else
            {
                ap.SetString(
    "default_account",
    "password",
    "");
            }


            ap.SetBoolean(
                "default_account",
                "isreader",
                this.checkBox_defaultAccount_isReader.Checked);
            ap.SetString(
                "default_account",
                "location",
                this.textBox_defaultAccount_location.Text);
            ap.SetBoolean(
                "default_account",
                "occur_per_start",
                this.checkBox_defaultAccount_occurPerStart.Checked);

            // charging
            ap.SetBoolean(
                "charging_form",
                "force",
                this.checkBox_charging_force.Checked);
            ap.SetInt(
                "charging_form",
                "info_dlg_opacity",
                (int)this.numericUpDown_charging_infoDlgOpacity.Value);
            ap.SetBoolean(
                "charging_form",
                "verify_barcode",
                this.checkBox_charging_verifyBarcode.Checked);

            ap.SetBoolean(
               "charging_form",
                "doubleItemInputAsEnd",
                this.checkBox_charging_doubleItemInputAsEnd.Checked);

            ap.SetString("charging_form",
                "display_format",
                this.comboBox_charging_displayFormat.Text);

            ap.SetBoolean(
                "charging_form",
                "auto_toupper_barcode",
                this.checkBox_charging_autoUppercaseBarcode.Checked);

            ap.SetBoolean(
    "charging_form",
    "green_infodlg_not_occur",
    this.checkBox_charging_greenInfoDlgNotOccur.Checked);

            
            ap.SetBoolean(
"charging_form",
"stop_filling_when_close_infodlg",
this.checkBox_charging_stopFillingWhenCloseInfoDlg.Checked);
          
            ap.SetBoolean(
                "charging_form",
                "no_biblio_and_item_info",
                this.checkBox_charging_noBiblioAndItem.Checked);

            
            ap.SetBoolean(
                "charging_form",
                "auto_switch_reader_barcode",
                this.checkBox_charging_autoSwitchReaderBarcode.Checked);

            // �Զ���������������
            // 2008/9/26
            ap.SetBoolean(
                "charging_form",
                "autoClearTextbox",
                this.checkBox_charging_autoClearTextbox.Checked);


            // ���ö���������֤
            ap.SetBoolean(
                "charging_form",
                "verify_reader_password",
                this.checkBox_charging_veifyReaderPassword.Checked);

            // �ʶ���������
            ap.SetBoolean(
    "charging_form",
    "speak_reader_name",
    this.checkBox_charging_speakNameWhenLoadReaderRecord.Checked);

            // ֤�����������������뺺��
            ap.SetBoolean(
                "charging_form",
                "patron_barcode_allow_hanzi",
                this.checkBox_charging_patronBarcodeAllowHanzi.Checked);

            // ������Ϣ�в���ʾ������ʷ
            ap.SetBoolean(
                "charging_form",
                "no_borrow_history",
                this.checkBox_charging_noBorrowHistory.Checked);

            // ���� ISBN ���黹�鹦��
             ap.SetBoolean(
                "charging_form",
                "isbn_borrow",
                this.checkBox_charging_isbnBorrow.Checked);

            // �Զ�����Ψһ����
            ap.SetBoolean(
                "charging_form",
                "auto_oper_single_item",
                this.checkBox_charging_autoOperItemDialogSingleItem.Checked);

            // *** ��ݳ���

            ap.SetString("quickcharging_form",
                "display_format",
                this.comboBox_quickCharging_displayFormat.Text);

            // ��֤�����
            ap.SetBoolean(
    "quickcharging_form",
    "verify_barcode",
    this.checkBox_quickCharging_verifyBarcode.Checked);

            // ������Ϣ�в���ʾ������ʷ
            ap.SetBoolean(
                "quickcharging_form",
                "no_borrow_history",
                this.checkBox_quickCharging_noBorrowHistory.Checked);

            // �ʶ���������
            ap.SetBoolean(
                "quickcharging_form",
                "speak_reader_name",
                this.checkBox_quickCharging_speakNameWhenLoadReaderRecord.Checked);

            // �ʶ�����
            ap.SetBoolean(
                "quickcharging_form",
                "speak_book_title",
                this.checkBox_quickCharging_speakBookTitle.Checked);

            // ���� ISBN ���黹�鹦��
            ap.SetBoolean(
                "quickcharging_form",
                "isbn_borrow",
                this.checkBox_quickCharging_isbnBorrow.Checked);

            // �Զ�����Ψһ����
            ap.SetBoolean(
                "quickcharging_form",
                "auto_oper_single_item",
                this.checkBox_quickCharging_autoOperItemDialogSingleItem.Checked);

            // *** �ֲᴰ
            ap.SetBoolean(
                "entity_form",
                "verify_item_barcode",
                this.checkBox_itemManagement_verifyItemBarcode.Checked);

            ap.SetBoolean(
                "entity_form",
                "cataloging",
                this.checkBox_itemManagement_cataloging.Checked);

            ap.SetBoolean(
                "entity_form",
                "search_dup_when_saving",
                this.checkBox_itemManagement_searchDupWhenSaving.Checked);

            ap.SetBoolean(
"entity_form",
"verify_data_when_saving",
this.checkBox_itemManagement_verifyDataWhenSaving.Checked);

            ap.SetBoolean(
"entityform",
"queryPanel_visibie",
this.checkBox_itemManagement_showQueryPanel.Checked);
            ap.SetBoolean(
"entityform",
"itemQuickInputPanel_visibie",
this.checkBox_itemManagement_showItemQuickInputPanel.Checked);

            // ������Ŀ��¼��ʾΪֻ��״̬
            ap.SetBoolean(
"entityform",
"linkedRecordReadonly",
this.checkBox_itemManagement_linkedRecordReadonly.Checked);

            // �Զ��޶�paste�����ͼ����
            ap.SetString(
    "entityform",
    "paste_pic_maxwidth",
    this.textBox_itemManagement_maxPicWidth.Text);

            // ��ʾ�����ֹݵĲ��¼
            ap.SetBoolean(
"entityform",
"displayOtherLibraryItem",
this.checkBox_itemManagement_displayOtherLibraryItem.Checked);

            // ui
            ap.SetBoolean(
                "MainForm",
                "hide_fixed_panel",
                this.checkBox_ui_hideFixedPanel.Checked);

            ap.SetString(
                "Global",
                "default_font",
                this.textBox_ui_defaultFont.Text);

            // passgate
            // ��ݵǼ�
            ap.SetInt(
                "passgate_form",
                "max_list_items_count",
                (int)this.numericUpDown_passgate_maxListItemsCount.Value);

            // search
            ap.SetBoolean(
                "all_search_form",
                "load_to_exist_detailwindow",
                this.checkBox_search_useExistDetailWindow.Checked);

            ap.SetInt(
                "biblio_search_form",
                "max_result_count",
                (int)this.numericUpDown_search_maxBiblioResultCount.Value);

            ap.SetBoolean(
                "biblio_search_form",
                "hide_matchstyle",
                this.checkBox_search_hideBiblioMatchStyle.Checked);

            // 2008/1/20 
            ap.SetBoolean(
                "biblio_search_form",
                "push_filling_browse",
                this.checkBox_search_biblioPushFilling.Checked);


            ap.SetInt(
                "reader_search_form",
                "max_result_count",
                (int)this.numericUpDown_search_maxReaderResultCount.Value);

            ap.SetBoolean(
                "reader_search_form",
                "hide_matchstyle",
                this.checkBox_search_hideReaderMatchStyle.Checked);

            // 2008/1/20 
            ap.SetBoolean(
                "reader_search_form",
                "push_filling_browse",
                this.checkBox_search_readerPushFilling.Checked);

            // --- search
            ap.SetInt(
                "item_search_form",
                "max_result_count",
                (int)this.numericUpDown_search_maxItemResultCount.Value);

            // 2008/11/21 
            ap.SetBoolean(
                "item_search_form",
                "hide_matchstyle_and_dbname",
                this.checkBox_search_hideItemMatchStyleAndDbName.Checked);


            // 2008/1/20 
            ap.SetBoolean(
                "item_search_form",
                "push_filling_browse",
                this.checkBox_search_itemPushFilling.Checked);


            // --- order
             ap.SetInt(
    "order_search_form",
    "max_result_count",
    (int)this.numericUpDown_search_maxOrderResultCount.Value);

            ap.SetBoolean(
                "order_search_form",
                "hide_matchstyle_and_dbname",
                this.checkBox_search_hideOrderMatchStyleAndDbName.Checked);

            ap.SetBoolean(
                "order_search_form",
                "push_filling_browse",
                this.checkBox_search_orderPushFilling.Checked);

            // --- issue
            ap.SetInt(
    "issue_search_form",
    "max_result_count",
    (int)this.numericUpDown_search_maxIssueResultCount.Value);

            ap.SetBoolean(
                "issue_search_form",
                "hide_matchstyle_and_dbname",
                this.checkBox_search_hideIssueMatchStyleAndDbName.Checked);

            ap.SetBoolean(
                "issue_search_form",
                "push_filling_browse",
                this.checkBox_search_issuePushFilling.Checked);

            // --- comment
            ap.SetInt(
    "comment_search_form",
    "max_result_count",
    (int)this.numericUpDown_search_maxCommentResultCount.Value);

            ap.SetBoolean(
                "comment_search_form",
                "hide_matchstyle_and_dbname",
                this.checkBox_search_hideCommentMatchStyleAndDbName.Checked);

            ap.SetBoolean(
                "comment_search_form",
                "push_filling_browse",
                this.checkBox_search_commentPushFilling.Checked);


            // ƾ����ӡ
            ap.SetString("charging_print",
                "prnPort",
                this.comboBox_print_prnPort.Text);

            ap.SetBoolean(
                "charging_print",
                "pausePrint",
                this.checkBox_print_pausePrint.Checked);

            ap.SetString(
                "charging_print",
                "projectName",
                this.textBox_print_projectName.Text);

            // amerce
            ap.SetString("config",
                "amerce_interface",
                this.comboBox_amerce_interface.Text);

            // ���Ѵ�����
            ap.SetString("amerce_form",
    "layout",
    this.comboBox_amerce_layout.Text);

            // accept
            ap.SetBoolean(
                "accept_form",
                "single_click_load_detail",
                this.checkBox_accept_singleClickLoadDetail.Checked);

            // *** ������

            // ���֤������URL
            ap.SetString("cardreader",
                "idcardReaderUrl",
                this.textBox_cardReader_idcardReaderUrl.Text);

            // ** ָ��
            // ָ���Ķ���URL
            ap.SetString("fingerprint",
                "fingerPrintReaderUrl",
                this.textBox_fingerprint_readerUrl.Text);

            // ָ�ƴ����ʻ� �û���
            ap.SetString("fingerprint",
                "userName",
                this.textBox_fingerprint_userName.Text);
            // ָ�ƴ����ʻ� ����
            {
                string strPassword = this.MainForm.EncryptPassword(this.textBox_fingerprint_password.Text);
                ap.SetString(
                    "fingerprint",
                "password",
                    strPassword);
            }

            // *** ����
            // �Զ����� �����ֶ����Ի���ʱ
            ap.SetBoolean(
                "reader_info_form",
                "autoretry_readcarddialog",
                this.checkBox_patron_autoRetryReaderCard.Checked);

            // ���� �����֤����������� �Ի���
            ap.SetBoolean(
                "reader_info_form",
                "display_setreaderbarcode_dialog",
                this.checkBox_patron_displaySetReaderBarcodeDialog.Checked);

            // У������������
            ap.SetBoolean(
    "reader_info_form",
    "verify_barcode",
    this.checkBox_patron_verifyBarcode.Checked);

            // �ڶ��ߴ���Χ���Զ��ر� ���֤������ ���̷���(&S)
            ap.GetBoolean(
    "reader_info_form",
    "disable_idcardreader_sendkey",
    this.checkBox_patron_disableIdcardReaderKeyboardSimulation.Checked);

            // ��־
            // ��ʾ���߽�����ʷ
            ap.SetBoolean(
                "operlog_form",
                "display_reader_borrow_history",
                this.checkBox_operLog_displayReaderBorrowHistory.Checked);
            // ��ʾ�������ʷ
            ap.SetBoolean(
                "operlog_form",
                "display_item_borrow_history",
                this.checkBox_operLog_displayItemBorrowHistory.Checked);
            // �Զ�������־�ļ�
            ap.SetBoolean(
                "global",
                "auto_cache_operlogfile",
                this.checkBox_operLog_autoCache.Checked);
            // ��־��ϸ����
            ap.SetString(
                "operlog_form",
                "level",
                this.comboBox_operLog_level.Text);

            // ȫ��
            // ������ؼ�����ű�����Ի���(&S)
            ap.SetBoolean(
                "global",
                "display_webbrowsecontrol_scripterror_dialog",
                this.checkBox_global_displayScriptErrorDialog.Checked);

            // ��ƴ��ʱ�Զ�ѡ�������
            ap.SetBoolean(
                "global",
                "auto_select_pinyin",
                this.checkBox_global_autoSelPinyin.Checked);

            // *** ��ǩ��ӡ
            // �Ӻδ���ȡ��ȡ��
            ap.SetString(
                "labelprint",
                "accessNo_source",
                this.comboBox_labelPrint_accessNoSource.Text);

            // *** ��Ϣ

            // ������Ŀ����
            ap.SetBoolean(
                "message",
                "share_biblio",
                this.checkBox_message_shareBiblio.Checked);

            if (m_bServerCfgChanged == true
                && this.MainForm != null)
            {
                // ���»�ø��ֿ������б�
                this.MainForm.StartPrepareNames(false, false);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {

            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void button_clearValueTableCache_Click(object sender, EventArgs e)
        {
            this.MainForm.ClearValueTableCache();
            MessageBox.Show(this, "OK");
        }

        // ���»����Ŀ��(����)����;���б�
        private void button_reloadBiblioDbFromInfos_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            this.MainForm.GetDbFromInfos();

            MessageBox.Show(this, "OK");

            this.Enabled = true;

        }

        private void comboBox_ui_fixedPanelDock_SelectedIndexChanged(object sender, EventArgs e)
        {
            string strDock = this.comboBox_ui_fixedPanelDock.Text;

            if (strDock == "Top")
            {
                this.MainForm.panel_fixed.Dock = DockStyle.Top;
                this.MainForm.panel_fixed.Size = new Size(this.MainForm.panel_fixed.Width,
                    this.MainForm.Size.Height / 3);
                this.MainForm.splitter_fixed.Dock = DockStyle.Top;
            }
            else if (strDock == "Bottom")
            {
                this.MainForm.panel_fixed.Dock = DockStyle.Bottom;
                this.MainForm.panel_fixed.Size = new Size(this.MainForm.panel_fixed.Width,
                    this.MainForm.Size.Height / 3);
                this.MainForm.splitter_fixed.Dock = DockStyle.Bottom;
            }
            else if (strDock == "Left")
            {
                this.MainForm.panel_fixed.Dock = DockStyle.Left;
                this.MainForm.panel_fixed.Size = new Size(this.MainForm.Size.Width / 3,
                    this.MainForm.panel_fixed.Size.Height);
                this.MainForm.splitter_fixed.Dock = DockStyle.Left;
            }
            else if (strDock == "Right")
            {
                this.MainForm.panel_fixed.Dock = DockStyle.Right;
                this.MainForm.panel_fixed.Size = new Size(this.MainForm.Size.Width / 3,
                    this.MainForm.panel_fixed.Size.Height);
                this.MainForm.splitter_fixed.Dock = DockStyle.Right;
            }
            else
            {
                // ȱʡΪ��
                this.MainForm.panel_fixed.Dock = DockStyle.Right;
                this.MainForm.panel_fixed.Size = new Size(this.MainForm.Size.Width / 3,
                    this.MainForm.panel_fixed.Size.Height);
                this.MainForm.splitter_fixed.Dock = DockStyle.Right;
            }
        }

        private void checkBox_ui_hideFixedPanel_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_ui_hideFixedPanel.Checked == true)
            {
                /*
                this.MainForm.panel_fixed.Visible = false;
                this.MainForm.splitter_fixed.Visible = false;
                 * */
                this.MainForm.PanelFixedVisible = false;
            }
            else
            {
                /*
                this.MainForm.panel_fixed.Visible = true;
                this.MainForm.splitter_fixed.Visible = true;
                 * */
                this.MainForm.PanelFixedVisible = true;
            }
        }

        private void checkBox_defaulAccount_savePasswordLong_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_defaulAccount_savePasswordLong.Checked == true)
                this.checkBox_defaulAccount_savePasswordShort.Checked = true;
        }

        // ���»����Ŀ�����б�
        // 2007/5/27 
        private void button_reloadBiblioDbNames_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            this.MainForm.InitialBiblioDbProperties();
            MessageBox.Show(this, "OK");

            this.Enabled = true;

        }

        private void button_reloadReaderDbProperties_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            // this.MainForm.GetReaderDbNames();
            this.MainForm.InitialReaderDbProperties();
            MessageBox.Show(this, "OK");

            this.Enabled = true;
        }

        // ���»��ʵ����б�
        private void button_reloadUtilDbProperties_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            this.MainForm.GetUtilDbProperties();
            MessageBox.Show(this, "OK");

            this.Enabled = true;
        }

        private void button_downloadPinyinXmlFile_Click(object sender, EventArgs e)
        {
            string strError = "";
            this.MainForm.DownloadDataFile("pinyin.xml", out strError);
            MessageBox.Show(this, strError);
        }

        private void buttondownloadIsbnXmlFile_Click(object sender, EventArgs e)
        {
            string strError = "";
            this.MainForm.DownloadDataFile("rangemessage.xml", out strError);   // 
            MessageBox.Show(this, strError);
        }

        private void MenuItem_print_editCharingPrintCs_Click(object sender, EventArgs e)
        {
            string strFileName = this.MainForm.DataDir + "\\charging_print.cs";
            System.Diagnostics.Process.Start("notepad.exe", strFileName);

        }

        private void MenuItem_print_editCharingPrintCsRef_Click(object sender, EventArgs e)
        {
            string strFileName = this.MainForm.DataDir + "\\charging_print.cs.ref";
            System.Diagnostics.Process.Start("notepad.exe", strFileName);

        }

        // ��ӡ��������
        private void button_print_projectManage_Click(object sender, EventArgs e)
        {
            this.MainForm.OperHistory.OnProjectManager(this);
        }

        private void textBox_print_projectName_TextChanged(object sender, EventArgs e)
        {
            label_print_projectNameMessage.Text = "�������趨����Ҫ��������dp2circulation���򣬲��ܷ������á�";
        }

        private void button_print_findProject_Click(object sender, EventArgs e)
        {
            // ���ֶԻ���ѯ��Project����
            GetProjectNameDlg dlg = new GetProjectNameDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.scriptManager = this.MainForm.OperHistory.ScriptManager;
            dlg.ProjectName = this.textBox_print_projectName.Text;
            dlg.NoneProject = false;

            this.MainForm.AppInfo.LinkFormState(dlg, "GetProjectNameDlg_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.textBox_print_projectName.Text = dlg.ProjectName;
        }

        private void checkBox_charging_noBiblioAndItem_CheckedChanged(object sender, EventArgs e)
        {
            this.FireParamChanged(
                "charging_form",
                "no_biblio_and_item_info",
                (object)this.checkBox_charging_noBiblioAndItem.Checked);
        }

        private void textBox_server_circulationServerUrl_TextChanged(object sender, EventArgs e)
        {
            this.m_bServerCfgChanged = true;
        }

        private void button_ui_getDefaultFont_Click(object sender, EventArgs e)
        {
            Font font = null;
            if (String.IsNullOrEmpty(this.textBox_ui_defaultFont.Text) == false)
            {
                // Create the FontConverter.
                System.ComponentModel.TypeConverter converter =
                    System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));

                font = (Font)converter.ConvertFromString(this.textBox_ui_defaultFont.Text);
            }
            else
            {
                font = Control.DefaultFont;
            }

            FontDialog dlg = new FontDialog();
            dlg.ShowColor = false;
            dlg.Font = font;
            dlg.ShowApply = false;
            dlg.ShowHelp = true;
            dlg.AllowVerticalFonts = false;

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;
            {
                // Create the FontConverter.
                System.ComponentModel.TypeConverter converter =
                    System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));

                this.textBox_ui_defaultFont.Text = converter.ConvertToString(dlg.Font);
            }

        }

        private void comboBox_ui_fixedPanelDock_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_ui_fixedPanelDock.Invalidate();
        }

        private void button_operLog_clearCacheDirectory_Click(object sender, EventArgs e)
        {
            string strError = "";

            string strCacheDir = this.MainForm.OperLogCacheDir; //  PathUtil.MergePath(this.MainForm.DataDir, "operlogcache");
            int nRet = Global.DeleteDataDir(
                this,
                strCacheDir,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            PathUtil.CreateDirIfNeed(strCacheDir);  // ���´���Ŀ¼
            MessageBox.Show(this, "��־�ļ����ػ���Ŀ¼ "+strCacheDir+" �Ѿ������");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void button_fingerprint_defaultValue_Click(object sender, EventArgs e)
        {
            string strDefaultValue = "ipc://FingerprintChannel/FingerprintServer";

            DialogResult result = MessageBox.Show(this,
    "ȷʵҪ�� ָ���Ķ����ӿ�URL ��ֵ����Ϊ����ֵ\r\n \""+strDefaultValue+"\" ? ",
    "CfgDlg",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            this.textBox_fingerprint_readerUrl.Text = strDefaultValue;
        }

        private void button_fingerprint_clearLocalCacheFiles_Click(object sender, EventArgs e)
        {
            string strDir = this.MainForm.FingerPrintCacheDir;  // PathUtil.MergePath(this.MainForm.DataDir, "fingerprintcache");
            DialogResult result = MessageBox.Show(this,
"ȷʵҪɾ���ļ��� " + strDir + " (�������еĵ�ȫ���ļ�) ? ",
"CfgDlg",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            string strError = "";
            try
            {
                Directory.Delete(strDir, true);
            }
            catch (DirectoryNotFoundException)
            {
                strError = "���β���ǰ���ļ��� '" + strDir + "' �Ѿ���ɾ��";
                goto ERROR1;
            }
            catch (Exception ex)
            {
                strError = "ɾ���ļ��� '" + strDir + "' ʱ����: " + ex.Message;
                goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void button_cardReader_setIdcardUrlDefaultValue_Click(object sender, EventArgs e)
        {
            string strDefaultValue = "ipc://IdcardChannel/IdcardServer";

            DialogResult result = MessageBox.Show(this,
    "ȷʵҪ�� ���֤�������ӿ�URL ��ֵ����Ϊ����ֵ\r\n \""+strDefaultValue+"\" ? ",
    "CfgDlg",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            this.textBox_cardReader_idcardReaderUrl.Text = strDefaultValue;
        }

        private void textBox_fingerprint_userName_TextChanged(object sender, EventArgs e)
        {
            // ����û���Ϊ�գ�������ҲҪΪ�ա���Ϊ�����������ַ������ã�����������²�
            if (this.textBox_fingerprint_userName.Text == "")
                this.textBox_fingerprint_password.Text = "";
        }

        private void toolStripButton_server_setHongnibaServer_Click(object sender, EventArgs e)
        {
            if (this.textBox_server_dp2LibraryServerUrl.Text != ServerDlg.HnbUrl)
            {
                this.textBox_server_dp2LibraryServerUrl.Text = ServerDlg.HnbUrl;

                this.textBox_defaultAccount_userName.Text = "";
                this.textBox_defaultAccount_password.Text = "";
            }
        }

        private void toolStripButton_server_setXeServer_Click(object sender, EventArgs e)
        {
            if (this.textBox_server_dp2LibraryServerUrl.Text != "net.pipe://localhost/dp2library/xe")
            {

                this.textBox_server_dp2LibraryServerUrl.Text = "net.pipe://localhost/dp2library/xe";

                this.textBox_defaultAccount_userName.Text = "supervisor";
                this.textBox_defaultAccount_password.Text = "";
            }
        }

        private void checkBox_charging_isbnBorrow_CheckedChanged(object sender, EventArgs e)
        {
                this.groupBox_charging_selectItemDialog.Enabled = this.checkBox_charging_isbnBorrow.Checked;
        }

        private void checkBox_quickCharging_isbnBorrow_CheckedChanged(object sender, EventArgs e)
        {
            this.groupBox_quickCharging_selectItemDialog.Enabled = this.checkBox_quickCharging_isbnBorrow.Checked;

        }


    }

    // �������ݼӹ�ģ��
    /// <summary>
    /// ���ò����仯���¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void ParamChangedEventHandler(object sender,
        ParamChangedEventArgs e);

    /// <summary>
    /// ���ò����仯�¼��Ĳ���
    /// </summary>
    public class ParamChangedEventArgs : EventArgs
    {
        /// <summary>
        /// С�ڱ���
        /// </summary>
        public string Section = "";
        /// <summary>
        /// �������
        /// </summary>
        public string Entry = "";
        /// <summary>
        /// ����ֵ
        /// </summary>
        public object Value = null;
    }

}