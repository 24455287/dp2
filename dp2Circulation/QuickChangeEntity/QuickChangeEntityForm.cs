using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using System.IO;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// �����޸Ĳᴰ
    /// </summary>
    internal partial class QuickChangeEntityForm : MyForm
    {
        WebExternalHost m_webExternalHost_biblio = new WebExternalHost();

        string m_strRefID_1 = "";
        // public string m_strRefID_2 = "";
#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;
#endif

        /// <summary>
        /// ��ǰ���ڴ����һ�����¼����������Ŀ��¼·��
        /// </summary>
        public string BiblioRecPath = "";

        LoadActionType m_loadActionType = LoadActionType.LoadAndAutoChange;

        const int WM_SWITCH_FOCUS = API.WM_USER + 200;

        // ��ϢWM_SWITCH_FOCUS��wparam����ֵ
        const int ITEM_BARCODE = 0;
        const int CONTROL_STATE = 1;

        /// <summary>
        /// ���캯��
        /// </summary>
        public QuickChangeEntityForm()
        {
            InitializeComponent();
        }

        string RefID_1
        {
            get
            {
                if (String.IsNullOrEmpty(this.m_strRefID_1) == true)
                    this.m_strRefID_1 = Guid.NewGuid().ToString();

                return this.m_strRefID_1;
            }
        }

        /*
        public string RefID_2
        {
            get
            {
                if (String.IsNullOrEmpty(this.m_strRefID_2) == true)
                    this.m_strRefID_2 = Guid.NewGuid().ToString();

                return this.m_strRefID_2;
            }
        }*/

        private void QuickChangeEntityForm_Load(object sender, EventArgs e)
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

            this.m_webExternalHost_biblio.Initial(this.MainForm, this.webBrowser_biblio);
            this.webBrowser_biblio.ObjectForScripting = this.m_webExternalHost_biblio;

            this.AcceptButton = this.button_loadBarcode;

            this.entityEditControl1.GetValueTable += new GetValueTableEventHandler(entityEditControl1_GetValueTable);
        }

        void entityEditControl1_GetValueTable(object sender, GetValueTableEventArgs e)
        {
            string strError = "";
            string[] values = null;
            int nRet = MainForm.GetValueTable(e.TableName,
                e.DbName,
                out values,
                out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
            e.values = values;
        }

        private void QuickChangeEntityForm_FormClosing(object sender, FormClosingEventArgs e)
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

            if (this.entityEditControl1.Changed == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ����Ϣ���޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
    "QuickChangeEntityForm",
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

        private void QuickChangeEntityForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.m_webExternalHost_biblio != null)
                this.m_webExternalHost_biblio.Destroy();

#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            this.entityEditControl1.GetValueTable -= new GetValueTableEventHandler(entityEditControl1_GetValueTable);
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

        // 
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ���ݲ�����ţ�װ����¼����Ŀ��¼
        /// </summary>
        /// <param name="bEnableControls">�Ƿ��ڴ�������н�ֹ����Ԫ��</param>
        /// <param name="strBarcode">�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        ///      -1  ����
        ///      0   û���ҵ�
        ///      1   �ҵ�
        /// </returns>
        public int LoadRecord(
            bool bEnableControls,
            string strBarcode,
            out string strError)
        {
            strError = "";

            if (bEnableControls == true)
            {
                EnableControls(false);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڳ�ʼ���������� ...");
                stop.BeginLoop();


                this.Update();
                this.MainForm.Update();
            }

            this.entityEditControl1.Clear();

            this.m_webExternalHost_biblio.StopPrevious();
            this.webBrowser_biblio.Stop();

            Global.ClearHtmlPage(this.webBrowser_biblio,
                this.MainForm.DataDir);

            this.textBox_message.Text = "";

            stop.SetMessage("����װ����¼ " + strBarcode + " ...");


            try
            {
                string strItemText = "";
                string strBiblioText = "";

                string strItemRecPath = "";
                string strBiblioRecPath = "";

                byte[] item_timestamp = null;

                long lRet = Channel.GetItemInfo(
                    stop,
                    strBarcode,
                    "xml",
                    out strItemText,
                    out strItemRecPath,
                    out item_timestamp,
                    "html",
                    out strBiblioText,
                    out strBiblioRecPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                if (lRet == 0)
                    return 0;

                if (lRet > 1)
                {
                    strError = "������� " + strBarcode + " ���ֱ����ж�����¼��ʹ��: \r\n" + strItemRecPath + "\r\n\r\n����һ�����ش�����������ϵͳ����Ա�����ų���";
                    goto ERROR1;
                }

                this.BiblioRecPath = strBiblioRecPath;

                int nRet = this.entityEditControl1.SetData(strItemText,
                    strItemRecPath,
                    item_timestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                Debug.Assert(this.entityEditControl1.Changed == false, "");

                this.entityEditControl1.SetReadOnly("librarian");

#if NO
                Global.SetHtmlString(this.webBrowser_biblio,
                    strBiblioText,
                    this.MainForm.DataDir,
                    "quickchangeentityform_biblio");
#endif
                this.m_webExternalHost_biblio.SetHtmlString(strBiblioText,
                    "quickchangeentityform_biblio");

                this.textBox_message.Text = "���¼·��: " + strItemRecPath + " �����������(��Ŀ)��¼·��: " + strBiblioRecPath;

            }
            finally
            {
                if (bEnableControls == true)
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");

                    EnableControls(true);
                }
            }

            return 1;
        ERROR1:
            strError = "װ�ز������Ϊ " + strBarcode + "�ļ�¼��������: " + strError;
            // MessageBox.Show(this, strError);
            return -1;
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.textBox_barcode.Enabled = bEnable;
            this.textBox_message.Enabled = bEnable;

            this.textBox_barcodeFile.Enabled = bEnable;
            this.textBox_outputBarcodes.Enabled = bEnable;

            this.button_loadBarcode.Enabled = bEnable;
            this.entityEditControl1.Enabled = bEnable;

            this.button_beginByBarcodeFile.Enabled = bEnable;
            this.button_changeParam.Enabled = bEnable;
            this.button_file_getBarcodeFilename.Enabled = bEnable;
            this.button_saveCurrentRecord.Enabled = bEnable;
            this.button_saveToBarcodeFile.Enabled = bEnable;

            this.button_getRecPathFileName.Enabled = bEnable;
            this.textBox_recPathFile.Enabled = bEnable;
            this.button_beginByRecPathFile.Enabled = bEnable;
        }

        private void button_loadBarcode_Click(object sender, EventArgs e)
        {
            int nRet = 0;
            string strError = "";

            // Debug.Assert(false, "");

            // �ȱ���ǰһ��
            if (this.entityEditControl1.Changed == true)
            {
                if (this.LoadActionType == LoadActionType.LoadOnly)
                {
                    // ������δ����
                    DialogResult result = MessageBox.Show(this,
        "��ǰ����Ϣ���޸ĺ���δ���档����ʱװ���µ���Ϣ������δ������Ϣ����ʧ��\r\n\r\n�Ƿ񱣴����װ���µ���Ϣ? (Yes �����װ��; No �����浫װ��; Cancel �����棬Ҳ����װ��)",
        "QuickChangeEntityForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button3);
                    if (result == DialogResult.Cancel)
                        return;
                    if (result == DialogResult.No)
                        goto DOLOAD;
                }
                else
                {
                    // ��װ�벢�Զ��޸ĵ�״̬�£�����ѯ�ʣ�ֱ�ӱ����װ��
                }


                nRet = DoSave(true, out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return;
                }
            }
            DOLOAD:

            nRet = LoadRecord(true, this.textBox_barcode.Text,
                out strError);
            if (nRet != 1)
            {
                MessageBox.Show(this, strError);
                goto SETFOCUS;
            }

            if (this.LoadActionType == LoadActionType.LoadAndAutoChange)
            {
                // �Զ��޸�
                AutoChangeData();
            }

            this.textBox_outputBarcodes.Text += this.textBox_barcode.Text + "\r\n";

            SETFOCUS:
            // ���㶨λ
            string strFocusAction = this.MainForm.AppInfo.GetString(
"change_param",
"focusAction",
"������ţ���ȫѡ");


            if (strFocusAction == "������ţ���ȫѡ")
            {
                SwitchFocus(0);
            }
            else if (strFocusAction == "����Ϣ�༭��")
            {
                SwitchFocus(1);
            }
        }

        // return:
        //      0   û��ʵ���Ըı�
        //      1   ��ʵ���Ըı�
        int AutoChangeData()
        {
            bool bChanged = false;
            // װ��ֵ
            /*
            string strState = this.MainForm.AppInfo.GetString(
                "change_param",
                "state",
                "<���ı�>");

            if (strState != "<���ı�>")
                this.entityEditControl1.State = strState;
            */


            // state
            string strStateAction = this.MainForm.AppInfo.GetString(
                "change_param",
                "state",
                "<���ı�>");
            if (strStateAction != "<���ı�>")
            {
                string strState = this.entityEditControl1.State;

                if (strStateAction == "<������>")
                {
                    string strAdd = this.MainForm.AppInfo.GetString(
                        "change_param",
                        "state_add",
                        "");
                    string strRemove = this.MainForm.AppInfo.GetString(
                        "change_param",
                        "state_remove",
                        "");

                    string strOldState = strState;

                    if (String.IsNullOrEmpty(strAdd) == false)
                        StringUtil.SetInList(ref strState, strAdd, true);
                    if (String.IsNullOrEmpty(strRemove) == false)
                        StringUtil.SetInList(ref strState, strRemove, false);

                    if (strOldState != strState)
                    {
                        this.entityEditControl1.State = strState;
                        bChanged = true;
                    }
                }
                else
                {
                    if (strStateAction != strState)
                    {
                        this.entityEditControl1.State = strStateAction;
                        bChanged = true;
                    }
                }
            }

            string strLocation = this.MainForm.AppInfo.GetString(
                "change_param",
                "location",
                "<���ı�>");

            if (strLocation != "<���ı�>")
            {
                if (this.entityEditControl1.LocationString != strLocation)
                {
                    this.entityEditControl1.LocationString = strLocation;
                    bChanged = true;
                }
            }


            string strBookType = this.MainForm.AppInfo.GetString(
                "change_param",
                "bookType",
                "<���ı�>");

            if (strBookType != "<���ı�>")
            {
                if (this.entityEditControl1.BookType != strBookType)
                {
                    this.entityEditControl1.BookType = strBookType;
                    bChanged = true;
                }
            }

            string strBatchNo = this.MainForm.AppInfo.GetString(
                "change_param",
                "batchNo",
                "<���ı�>");
            if (strBatchNo != "<���ı�>")
            {
                if (this.entityEditControl1.BatchNo != strBatchNo)
                {
                    this.entityEditControl1.BatchNo = strBatchNo;
                    bChanged = true;
                }
            }

            if (bChanged == true)
                return 1;

            return 0;
        }

        /// <summary>
        /// ���ֶ��������Ի����ռ�������Ϣ
        /// </summary>
        /// <returns>true: ȷ��; false: ����</returns>
        public bool SetChangeParameters()
        {
            ChangeEntityActionDialog dlg = new ChangeEntityActionDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.GetValueTable += new GetValueTableEventHandler(entityEditControl1_GetValueTable);
            if (String.IsNullOrEmpty(this.entityEditControl1.RecPath) == true)
            {
                dlg.RefDbName = "";
            }
            else
            {
                dlg.RefDbName = Global.GetDbName(this.entityEditControl1.RecPath);
            }
            dlg.MainForm = this.MainForm;
            // dlg.StartPosition = FormStartPosition.CenterScreen;

            this.MainForm.AppInfo.LinkFormState(dlg, "quickchangeentityform_changeparamdialog_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.OK)
                return true;
            return false;
        }

        // �޸Ĳ���
        private void button_changeParam_Click(object sender, EventArgs e)
        {
            SetChangeParameters();
        }


        // ���浱ǰʵ���¼
        // �����Ժ󣬺�ɫ���ַ�����Ϊ��ɫ
        private void button_saveCurrentRecord_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = DoSave(true, out strError);
            if (nRet != 1)
                MessageBox.Show(this, strError);
        }

        // return:
        //      -1  ����
        //      0   û�б�Ҫ����
        //      1   ����ɹ�
        int DoSave(bool bEnableControls,
            out string strError)
        {
            strError = "";

            if (bEnableControls == true)
                EnableControls(false);

            try
            {
                if (this.entityEditControl1.Changed == false)
                {
                    strError = "û���޸Ĺ�����Ϣ��Ҫ����";
                    goto ERROR1;
                }

                EntityInfo[] entities = null;
                EntityInfo[] errorinfos = null;

                // ������Ҫ�ύ��ʵ����Ϣ����
                int nRet = BuildSaveEntities(
                    out entities,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (entities == null || entities.Length == 0)
                    return 0; // û�б�Ҫ����


                nRet = SaveEntityRecords(
                    bEnableControls,
                    this.BiblioRecPath,
                    entities,
                    out errorinfos,
                    out strError);

                this.entityEditControl1.Changed = false;    // 2007/4/4

                // �ѳ�����������Ҫ����״̬��������ֵ���ʾ���ڴ�
                if (RefreshOperResult(errorinfos) == true)
                {
                    if (nRet != -1)
                        return -1;
                }

                if (nRet == -1)
                {
                    goto ERROR1;
                }

                return 1;
            ERROR1:
                strError = "��������Ϊ "+this.entityEditControl1.Barcode+" �Ĳ��¼ʱ����: " + strError;
                return -1;
            }
            finally
            {
                if (bEnableControls == true)
                    EnableControls(true);
            }
        }

        // �������ڱ����ʵ����Ϣ����
        int BuildSaveEntities(
            out EntityInfo[] entities,
            out string strError)
        {
            strError = "";
            entities = null;
            int nRet = 0;

            entities = new EntityInfo[1];

            EntityInfo info = new EntityInfo();

            string strXml = "";
            nRet = this.entityEditControl1.GetData(
                true,
                out strXml,
                out strError);
            if (nRet == -1)
                return -1;


            info.Action = "change";
            info.OldRecPath = this.entityEditControl1.RecPath;  //  2007/6/2
            info.NewRecPath = this.entityEditControl1.RecPath;

            info.NewRecord = strXml;
            info.NewTimestamp = null;

            info.OldRecord = this.entityEditControl1.OldRecord;
            info.OldTimestamp = this.entityEditControl1.Timestamp;

            // info.RefID = this.RefID_1; // 2008/3/3 // ��һ�������������˼���ѵ��Ƿ���ʹ��ͬһ�� refid?

            // 2013/6/23
            if (string.IsNullOrEmpty(info.RefID) == true)
                info.RefID = Guid.NewGuid().ToString();

            entities[0] = info;

            return 0;
        }

        // ����ʵ���¼
        // ������ˢ�½���ͱ���
        int SaveEntityRecords(
            bool bEnableControls,
            string strBiblioRecPath,
            EntityInfo[] entities,
            out EntityInfo[] errorinfos,
            out string strError)
        {
            if (bEnableControls == true)
            {
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڱ������Ϣ ...");
                stop.BeginLoop();

                this.Update();
                this.MainForm.Update();
            }


            try
            {
                long lRet = Channel.SetEntities(
                    stop,
                    strBiblioRecPath,
                    entities,
                    out errorinfos,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                if (bEnableControls == true)
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }
            }

            return 1;
        ERROR1:
            return -1;
        }

        // �ѱ�����Ϣ�еĳɹ������״̬�޸Ķ���
        // ���ҳ���ȥ��û�б���ġ�ɾ����BookItem����ڴ���Ӿ��ϣ�
        // return:
        //      false   û�о���
        //      true    ���־���
        bool RefreshOperResult(EntityInfo[] errorinfos)
        {
            int nRet = 0;

            string strWarning = ""; // ������Ϣ

            if (errorinfos == null)
                return false;

            for (int i = 0; i < errorinfos.Length; i++)
            {
                XmlDocument dom = new XmlDocument();

                string strNewXml = errorinfos[i].NewRecord;
                string strOldXml = errorinfos[i].OldRecord;


                // ������Ϣ����
                if (errorinfos[i].ErrorCode == ErrorCodeValue.NoError)
                {

                    if (errorinfos[i].Action == "change")
                    {
                        string strError = "";
                        nRet = this.entityEditControl1.SetData(
                            errorinfos[i].NewRecord,
                            errorinfos[i].NewRecPath,
                            errorinfos[i].NewTimestamp,
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(this, strError);

                        this.entityEditControl1.SetReadOnly("librarian");
                    }

                    continue;
                }

                // ������

                // TimeStampMismatch�����ʱ��, ʵ����OldRecord�з����˵�ǰ���и�λ�õļ�¼, OldTimeStamp���Ƕ�Ӧ��ʱ���
                // ��Ҫ���ֲο���, ���ڲ����߶Աȴ���
                if (errorinfos[i].ErrorCode == ErrorCodeValue.TimestampMismatch)
                {
                    this.entityEditControl1.OldRecord = errorinfos[i].OldRecord;

                    // �Ƿ���Ҫ�ý���������ȷˢ��һ��, ����?
                    // ��Ϊ��ˢ��, ������ֹ³ç�������ύ����
                    this.entityEditControl1.Timestamp = errorinfos[i].OldTimestamp;    // ������ʹ���ٴα���, û�����ϰ�
                }

                strWarning += "���ύ��������з������� -- " + errorinfos[i].ErrorInfo + "\r\n";
            }

            // 
            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strWarning += "\r\n��ע���޸ĺ������ύ����";
                MessageBox.Show(this, strWarning);
                return true;
            }

            return false;
        }

        private void ToolStripMenuItem_loadOnly_Click(object sender, EventArgs e)
        {
            this.LoadActionType = LoadActionType.LoadOnly;
        }

        private void ToolStripMenuItem_loadAndAutoChange_Click(object sender, EventArgs e)
        {
            this.LoadActionType = LoadActionType.LoadAndAutoChange;
        }

        /// <summary>
        /// װ������
        /// </summary>
        public LoadActionType LoadActionType
        {
            get
            {
                return this.m_loadActionType;
            }
            set
            {
                this.m_loadActionType = value;

                this.ToolStripMenuItem_loadOnly.Checked = false;
                this.ToolStripMenuItem_loadAndAutoChange.Checked = false;

                if (m_loadActionType == LoadActionType.LoadOnly)
                {
                    this.button_loadBarcode.Text = "ֻװ��(���޸�)";
                    this.ToolStripMenuItem_loadOnly.Checked = true;
                }
                if (m_loadActionType == LoadActionType.LoadAndAutoChange)
                {
                    this.button_loadBarcode.Text = "װ�벢�Զ��޸�";
                    this.ToolStripMenuItem_loadAndAutoChange.Checked = true;
                }

            }
        }

        private void QuickChangeEntityForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        // ̽���ı��ļ�������
        // parameters:
        //      bIncludeBlankLine   �ǰ�������
        // return:
        //      -1  ����
        //      >=0 ����
        static long GetTextLineCount(string strFilename,
            bool bIncludeBlankLine)
        {
            try
            {
                long lCount = 0;
                StreamReader sr = null;
                sr = new StreamReader(strFilename);

                try
                {

                    // ���ж����ļ�����
                    for (; ; )
                    {
                        string strLine = "";
                        strLine = sr.ReadLine();
                        if (strLine == null)
                            break;

                        strLine = strLine.Trim();
                        if (String.IsNullOrEmpty(strLine) == true
                            && bIncludeBlankLine == false)
                            continue;

                        lCount++;
                    }
                    return lCount;

                }
                finally
                {
                    sr.Close();
                }
            }
            catch
            {
                return -1;
            }
        }

        // return:
        //      -1  ����
        //      0   ��������
        //      >=1 ���������
        /// <summary>
        /// ���ݲ�������ļ����д���
        /// </summary>
        /// <param name="strFilename">��������ļ���</param>
        /// <returns>
        ///      -1  ����
        ///      0   ��������
        ///      >=1 ���������
        /// </returns>
        public int DoBarcodeFile(string strFilename)
        {
            string strError = "";

            this.tabControl_input.SelectedTab = this.tabPage_barcodeFile;

            this.textBox_barcodeFile.Text = strFilename;

            // parameters:
            //      strFileType barcode/recpath
            // return:
            //      -1  ����
            //      0   ����
            //      >=1 ���������
            int nRet = ProcessFile(
                    "barcode",
                    out strError);
            if (nRet == -1)
                goto ERROR1;

            return nRet;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // return:
        //      -1  ����
        //      0   ��������
        //      >=1 ���������
        /// <summary>
        /// ���ݼ�¼·���ļ����д���
        /// </summary>
        /// <param name="strFilename">��¼·���ļ���</param>
        /// <returns>
        ///      -1  ����
        ///      0   ��������
        ///      >=1 ���������
        /// </returns>
        public int DoRecPathFile(string strFilename)
        {
            string strError = "";

            this.tabControl_input.SelectedTab = this.tabPage_recPathFile;

            this.textBox_recPathFile.Text = strFilename;

            // parameters:
            //      strFileType barcode/recpath
            // return:
            //      -1  ����
            //      0   ����
            //      >=1 ���������
            int nRet = ProcessFile(
                    "recpath",
                    out strError);
            if (nRet == -1)
                goto ERROR1;

            return nRet;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // parameters:
        //      strFileType barcode/recpath
        // return:
        //      -1  ����
        //      0   ����
        //      >=1 ���������
        int ProcessFile(
            string strFileType,
            out string strError)
        {
            strError = "";

            string strFilename = "";
            if (strFileType == "barcode")
            {
                if (string.IsNullOrEmpty(this.textBox_barcodeFile.Text) == true)
                {
                    OpenFileDialog dlg = new OpenFileDialog();

                    dlg.FileName = this.textBox_barcodeFile.Text;
                    dlg.Title = "��ָ��Ҫ�򿪵�������ļ���";
                    dlg.Filter = "������ļ� (*.txt)|*.txt|All files (*.*)|*.*";
                    dlg.RestoreDirectory = true;

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return 0;

                    this.textBox_barcodeFile.Text = dlg.FileName;
                }

                strFilename = this.textBox_barcodeFile.Text;
            }
            else if (strFileType == "recpath")
            {
                if (string.IsNullOrEmpty(this.textBox_recPathFile.Text) == true)
                {
                    OpenFileDialog dlg = new OpenFileDialog();

                    dlg.FileName = this.textBox_recPathFile.Text;
                    dlg.Title = "��ָ��Ҫ�򿪵ļ�¼·���ļ���";
                    dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
                    dlg.RestoreDirectory = true;

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return 0;

                    this.textBox_recPathFile.Text = dlg.FileName;
                }

                strFilename = this.textBox_recPathFile.Text;
            }
            else
            {
                strError = "δ֪�� strFileType '" + strFilename + "'";
                return -1;
            }

            // ̽���ı��ļ�������
            // parameters:
            //      bIncludeBlankLine   �ǰ�������
            // return:
            //      -1  ����
            //      >=0 ����
            long lLineCount = GetTextLineCount(strFilename,
                false);

            if (lLineCount != -1)
                stop.SetProgressRange(0, lLineCount);

            int nCurrentLine = 0;
            StreamReader sr = null;
            try
            {
                this.textBox_outputBarcodes.Text = "";

                // ���ļ�
                /*
                try
                {
                 * */
                sr = new StreamReader(strFilename);
                /*
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message);
                    return;
                }*/

                EnableControls(false);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڳ�ʼ���������� ...");
                stop.BeginLoop();
                this.Update();
                this.MainForm.Update();

                try
                {
                    int nRet = 0;

                    // ���ж����ļ�����
                    for (; ; )
                    {
                        Application.DoEvents();
                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�1";
                                return -1;
                            }
                        }


                        string strLine = "";
                        strLine = sr.ReadLine();
                        if (strLine == null)
                            break;

                        strLine = strLine.Trim();
                        if (String.IsNullOrEmpty(strLine) == true)
                            continue;

                        if (strFileType == "barcode")
                            stop.SetMessage("���ڴ��������� " + strLine + " ��Ӧ�ļ�¼...");
                        else
                            stop.SetMessage("���ڴ����¼·�� " + strLine + " ��Ӧ�ļ�¼...");
                        stop.SetProgressValue(nCurrentLine);

                        nCurrentLine++;

                        // �ȱ���ǰһ��
                        if (this.entityEditControl1.Changed == true)
                        {
                            nRet = DoSave(false,
                                out strError);
                            if (nRet == -1)
                                MessageBox.Show(this, strError);
                        }
                        // DOLOAD:

                        nRet = LoadRecord(false,
                            strFileType == "barcode" ? strLine : "@path:" + strLine,
                            out strError);
                        if (nRet != 1)
                        {
                            this.textBox_outputBarcodes.Text += "# " + strLine + " " + strError + "\r\n";
                            continue;
                        }

                        // �Զ��޸�
                        // return:
                        //      0   û��ʵ���Ըı�
                        //      1   ��ʵ���Ըı�
                        AutoChangeData();

                        if (this.entityEditControl1.Changed == true)
                        {
                            nRet = DoSave(false,
                                out strError);
                            if (nRet == -1)
                            {
                                this.textBox_outputBarcodes.Text += "# " + strLine + " " + strError + "\r\n";
                                continue;
                            }

                            if (nRet != -1)
                            {
                                this.textBox_outputBarcodes.Text += strLine + "\r\n";
                            }
                        }
                        else
                        {
                            this.textBox_outputBarcodes.Text += "# " + strLine + "\r\n";
                        }
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

                return nCurrentLine;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }
            finally
            {
                if (sr != null)
                    sr.Close();
            }
        }

        // �����ļ��Զ������޸�
        private void button_beginByBarcodeFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            // parameters:
            //      strFileType barcode/recpath
            // return:
            //      -1  ����
            //      0   ����
            //      >=1 ���������
            int nRet = ProcessFile(
                    "barcode",
                    out strError);
            if (nRet == -1)
                goto ERROR1;

            MessageBox.Show(this, "������ɡ��������¼ " + nRet.ToString() + " ��");

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �����Ѵ�������ŵ�������ļ�
        private void button_saveToBarcodeFile_Click(object sender, EventArgs e)
        {
            // ѯ���ļ�ȫ·��
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ�����(����Ż�·��)����ļ���";
            dlg.OverwritePrompt = true;
            dlg.CreatePrompt = false;
            // dlg.FileName = this.LocalPath;
            // dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "����ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            StreamWriter sw = new StreamWriter(dlg.FileName);
            sw.Write(this.textBox_outputBarcodes.Text);
            sw.Close();
        }

        void SwitchFocus(int target)
        {
            API.PostMessage(this.Handle, WM_SWITCH_FOCUS,
                target, 0);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_SWITCH_FOCUS:
                    {
                        if ((int)m.WParam == ITEM_BARCODE)
                        {
                            this.textBox_barcode.SelectAll();
                            this.textBox_barcode.Focus();
                        }

                        if ((int)m.WParam == CONTROL_STATE)
                        {
                            this.entityEditControl1.FocusState(false);
                        }

                        return;
                    }
                // break;
            }
            base.DefWndProc(ref m);
        }

        private void button_file_getBarcodeFilename_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.FileName = this.textBox_barcodeFile.Text;
            dlg.Title = "��ָ��Ҫ�򿪵�������ļ���";
            dlg.Filter = "������ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_barcodeFile.Text = dlg.FileName;
        }

        private void button_getRecPathFileName_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.FileName = this.textBox_recPathFile.Text;
            dlg.Title = "��ָ��Ҫ�򿪵ļ�¼·���ļ���";
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_recPathFile.Text = dlg.FileName;
        }

        private void button_beginByRecPathFile_Click(object sender, EventArgs e)
        {
            string strError = "";

            // parameters:
            //      strFileType barcode/recpath
            // return:
            //      -1  ����
            //      0   ����
            //      >=1 ���������
            int nRet = ProcessFile(
                    "recpath",
                    out strError);
            if (nRet == -1)
                goto ERROR1;

            MessageBox.Show(this, "������ɡ��������¼ " + nRet.ToString() + " ��");

            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }
    }

    /// <summary>
    /// װ������
    /// </summary>
    public enum LoadActionType
    {
        /// <summary>
        /// ֻװ��(���޸�)
        /// </summary>
        LoadOnly = 0,   // ֻװ��(���޸�)
        /// <summary>
        /// װ�ز����Զ��޸�
        /// </summary>
        LoadAndAutoChange = 1, // װ�ز����Զ��޸�
    }
}