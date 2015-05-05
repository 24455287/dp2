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
using DigitalPlatform.Marc;
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// �����޸���Ŀ��
    /// </summary>
    internal partial class QuickChangeBiblioForm : MyForm
    {
#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;
#endif


        public QuickChangeBiblioForm()
        {
            InitializeComponent();
        }

        private void QuickChangeBiblioForm_Load(object sender, EventArgs e)
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

        private void QuickChangeBiblioForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void QuickChangeBiblioForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif


            // �������ڽ����У�
        }

        // return:
        //      -1  ����
        //      0   ��������
        //      1   ��������
        public int DoRecPathLines()
        {
            this.tabControl_input.SelectedTab = this.tabPage_paths;

            this.EnableControls(false);
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("");
            stop.BeginLoop();
            this.Update();
            this.MainForm.Update();

            try
            {
                return DoTextLines();
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControls(true);
            }

        }

        public void DoRecPathFile(string strFileName)
        {
            this.tabControl_input.SelectedTab = this.tabPage_recpathFile;
            this.textBox_recpathFile.Text = strFileName;

            this.EnableControls(false);
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("");
            stop.BeginLoop();
            this.Update();
            this.MainForm.Update();
            try
            {
                DoFileName();
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControls(true);
            }
        }

        private void button_begin_Click(object sender, EventArgs e)
        {
            this.EnableControls(false);
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("");
            stop.BeginLoop();
            this.Update();
            this.MainForm.Update();

            try
            {
                if (this.tabControl_input.SelectedTab == this.tabPage_paths)
                {
                    DoTextLines();
                }
                else if (this.tabControl_input.SelectedTab == this.tabPage_recpathFile)
                {
                    DoFileName();
                }

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.EnableControls(true);
            }
        }

        public string RecPathLines
        {
            get
            {
                return this.textBox_paths.Text;
            }
            set
            {
                this.textBox_paths.Text = value;
            }
        }

        public string RecPathFileName
        {
            get
            {
                return this.textBox_recpathFile.Text;
            }
            set
            {
                this.textBox_recpathFile.Text = value;
            }
        }

        // return:
        //      -1  ����
        //      0   ��������
        //      1   ��������
        int DoTextLines()
        {
            string strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(this.textBox_paths.Text) == true)
            {
                strError = "��δָ���κ�·��";
                goto ERROR1;
            }

            // TODO: MessageBox��ʾ��Ҫ���е��޸Ķ����������治�ܸ�ԭ

            // TODO: ����޸Ķ�������������ʲô�����޸ĵ����
            string strInfo = GetSummary();
            if (String.IsNullOrEmpty(strInfo) == true)
            {
                DialogResult result = MessageBox.Show(this,
    "��ǰû���κ��޸Ķ�����ȷʵҪ��������?",
    "dp2Circulation",
    MessageBoxButtons.OKCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Cancel)
                    return 0;
            }
            else
            {
                DialogResult result = MessageBox.Show(this,
"�������������޸Ķ�����\r\n---"+strInfo+"\r\n\r\n��ʼ����?",
"dp2Circulation",
MessageBoxButtons.OKCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Cancel)
                    return 0;
            }

            int nCount = 0; // �ܹ����������
            int nChangedCount = 0;  // �����޸ĵ��ж�����

            DateTime now = DateTime.Now;

            stop.SetProgressRange(0, this.textBox_paths.Lines.Length);

            for (int i = 0; i < this.textBox_paths.Lines.Length; i++)
            {
                Application.DoEvents();
                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�1";
                        goto ERROR1;
                    }
                }

                string strLine = this.textBox_paths.Lines[i].Trim();
                nRet = strLine.IndexOfAny(new char[] {' ','\t'});
                if (nRet != -1)
                {
                    strLine = strLine.Substring(0, nRet).Trim();
                }

                if (String.IsNullOrEmpty(strLine) == true)
                    continue;
                nRet = ChangeOneRecord(strLine,
                    now,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                nCount++;
                if (nRet == 1)
                    nChangedCount++;
                stop.SetProgressValue(i + 1);
            }

            MessageBox.Show(this, "������ϡ��������¼ " + nCount.ToString() + " ����ʵ�ʷ����޸� " + nChangedCount.ToString() + " ��");
            return 1;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        void DoFileName()
        {
            string strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(this.textBox_recpathFile.Text) == true)
            {
                strError = "��δָ����¼·���ļ���";
                goto ERROR1;
            }

            // TODO: MessageBox��ʾ��Ҫ���е��޸Ķ����������治�ܸ�ԭ

            // TODO: ����޸Ķ�������������ʲô�����޸ĵ����
            string strInfo = GetSummary();
            if (String.IsNullOrEmpty(strInfo) == true)
            {
                DialogResult result = MessageBox.Show(this,
    "��ǰû���κ��޸Ķ�����ȷʵҪ��������?",
    "dp2Circulation",
    MessageBoxButtons.OKCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Cancel)
                    return;
            }
            else
            {
                DialogResult result = MessageBox.Show(this,
"�������������޸Ķ�����\r\n---" + strInfo + "\r\n\r\n��ʼ����?",
"dp2Circulation",
MessageBoxButtons.OKCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Cancel)
                    return;
            }

            int nCount = 0; // �ܹ����������
            int nChangedCount = 0;  // �����޸ĵ��ж�����

            using (StreamReader sr = new StreamReader(this.textBox_recpathFile.Text))
            {

                DateTime now = DateTime.Now;

                stop.SetProgressRange(0, sr.BaseStream.Length);

                for (; ; )
                {
                    Application.DoEvents();
                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�2";
                            goto ERROR1;
                        }
                    }

                    string strLine = "";
                    strLine = sr.ReadLine();
                    if (strLine == null)
                        break;

                    strLine = strLine.Trim();
                    if (String.IsNullOrEmpty(strLine) == true)
                        continue;

                    nRet = strLine.IndexOfAny(new char[] { ' ', '\t' });
                    if (nRet != -1)
                    {
                        strLine = strLine.Substring(0, nRet).Trim();
                    }

                    if (String.IsNullOrEmpty(strLine) == true)
                        continue;
                    // return:
                    //      -1  ����
                    //      0   δ�����ı�
                    //      1   �����˸ı�
                    nRet = ChangeOneRecord(strLine,
                        now,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    nCount++;
                    if (nRet == 1)
                        nChangedCount++;
                    stop.SetProgressValue(sr.BaseStream.Position);
                }
            }

            MessageBox.Show(this, "������ϡ��������¼ "+nCount.ToString()+" ����ʵ�ʷ����޸� "+nChangedCount.ToString()+" ��");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.textBox_paths.Enabled = bEnable;
            this.textBox_recpathFile.Enabled = bEnable;

            this.button_begin.Enabled = bEnable;
            this.button_changeParam.Enabled = bEnable;
            this.button_file_getRecpathFilename.Enabled = bEnable;
        }

        // return:
        //      -1  ����
        //      0   δ�����ı�
        //      1   �����˸ı�
        int ChangeOneRecord(string strBiblioRecPath,
            DateTime now,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            stop.SetMessage("���ڴ��� " + strBiblioRecPath + " ...");

            string[] formats = new string[1];
            formats[0] = "xml";

            string[] results = null;
            byte[] timestamp = null;
            long lRet = Channel.GetBiblioInfos(
                stop,
                strBiblioRecPath,
                    "",
                formats,
                out results,
                out timestamp,
                out strError);
            if (lRet == 0)
            {
                return 0;   // not found
            }
            if (lRet == -1)
                return -1;
            if (results.Length == 0)
            {
                strError = "results length error";
                return -1;
            }
            string strXml = results[0];

            XmlDocument domOrigin = new XmlDocument();

            try
            {
                domOrigin.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "װ��XML��DOMʱ��������: " + ex.Message;
                return -1;
            }


            string strMARC = "";
            string strMarcSyntax = "";
            string strOutMarcSyntax = "";
            // ��XML��ʽת��ΪMARC��ʽ
            // �Զ������ݼ�¼�л��MARC�﷨
            nRet = MarcUtil.Xml2Marc(strXml,
                true,
                strMarcSyntax,
                out strOutMarcSyntax,
                out strMARC,
                out strError);
            if (nRet == -1)
            {
                strError = "XMLת����MARC��¼ʱ����: " + strError;
                return -1;
            }

            // �޸�
            // return:
            //      -1  ����
            //      0   δ�����ı�
            //      1   �����˸ı�
            nRet = ModifyField998(ref strMARC,
                now,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;

            // ת����xml��ʽ
            XmlDocument domMarc = null;
            nRet = MarcUtil.Marc2Xml(strMARC,
                strOutMarcSyntax,
                out domMarc,
                out strError);
            if (nRet == -1)
                return -1;

            // �ϲ�<dprms:file>Ԫ��
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);

            XmlNodeList nodes = domOrigin.DocumentElement.SelectNodes("//dprms:file", nsmgr);

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode new_node = domMarc.CreateElement("dprms",
                    "file",
                    DpNs.dprms);
                domMarc.DocumentElement.AppendChild(new_node);
                DomUtil.SetElementOuterXml(new_node, nodes[i].OuterXml);
            }

            // ����
            byte[] baNewTimestamp = null;
            string strOutputPath = "";
            lRet = Channel.SetBiblioInfo(
    stop,
    "change",
    strBiblioRecPath,
    "xml",
    domMarc.DocumentElement.OuterXml,
    timestamp,
    "",
    out strOutputPath,
    out baNewTimestamp,
    out strError);
            if (lRet == -1)
            {
                strError = "������Ŀ��¼ '" + strBiblioRecPath + "' ʱ����: " + strError;
                return -1;
            }

            return 1;
        }

        // ��������޸����������
        string GetSummary()
        {
            string strResult = "";
            int nCount = 0;

            // state
            string strStateAction = this.MainForm.AppInfo.GetString(
                "change_biblio_param",
                "state",
                "<���ı�>");
            if (strStateAction != "<���ı�>")
            {
                if (strStateAction == "<������>")
                {
                    string strAdd = this.MainForm.AppInfo.GetString(
                "change_biblio_param",
                "state_add",
                "");
                    string strRemove = this.MainForm.AppInfo.GetString(
            "change_biblio_param",
            "state_remove",
            "");
                    if (String.IsNullOrEmpty(strAdd) == false)
                    {
                        strResult += "\r\n��״ֵ̬(998$s)����� '"+strAdd+"'";
                        nCount++;
                    }
                    if (String.IsNullOrEmpty(strRemove) == false)
                    {
                        strResult += "\r\n��״ֵ̬(998$s)��ȥ�� '" + strAdd + "'";
                        nCount++;
                    }

                }
                else
                {
                    strResult += "\r\n��״ֵ̬(998$s)��Ϊ '" + strStateAction + "'";
                    nCount++;
                }

            }

            // time
            string strTimeAction = this.MainForm.AppInfo.GetString(
    "change_biblio_param",
    "opertime",
    "<���ı�>");
            if (strTimeAction != "<���ı�>")
            {
                if (strTimeAction == "<��ǰʱ��>")
                {
                    strResult += "\r\n��ʱ��ֵ(998$u)����Ϊ��ǰʱ��";
                    nCount++;
                }
                else if (strTimeAction == "<���>")
                {
                    strResult += "\r\n��ʱ��ֵ(998$u)���";
                    nCount++;
                }
                else if (strTimeAction == "<ָ��ʱ��>")
                {
                    string strValue = this.MainForm.AppInfo.GetString(
                        "change_biblio_param",
                        "opertime_value",
                        "");
                    strResult += "\r\n��ʱ��ֵ(998$u)�޸�Ϊ '" + strValue + "'";
                    nCount++;
                }
                else
                {
                }

            }

            // batchno
            string strBatchNoAction = this.MainForm.AppInfo.GetString(
"change_biblio_param",
"batchNo",
"<���ı�>");
            if (strBatchNoAction != "<���ı�>")
            {
                strResult += "\r\n�����κ�ֵ(998$a)�޸�Ϊ '" + strBatchNoAction + "'";
                nCount++;
            }

            return strResult;
        }

        // TODO: Ԥ�Ƚ�AppInfo��ֵȡ�����ӿ��ٶ�
        // return:
        //      -1  ����
        //      0   δ�����ı�
        //      1   �����˸ı�
        int ModifyField998(ref string strMARC,
            DateTime now,
            out string strError)
        {
            strError = "";
            // int nRet = 0;
            bool bChanged = false;

            string strField998 = MarcUtil.GetField(strMARC,
                "998");
            if (strField998 == null)
            {
                strError = "GetField() 998 error";
                return -1;
            }

            if (String.IsNullOrEmpty(strField998) == true
                || strField998.Length < 5)
                strField998 = "998  ";


            // state
            string strStateAction = this.MainForm.AppInfo.GetString(
                "change_biblio_param",
                "state",
                "<���ı�>");
            if (strStateAction != "<���ı�>")
            {
                string strState = MarcUtil.GetSubfieldContent(strField998,
    "s");

                if (strStateAction == "<������>")
                {
                    string strAdd = this.MainForm.AppInfo.GetString(
                "change_biblio_param",
                "state_add",
                "");
                    string strRemove = this.MainForm.AppInfo.GetString(
            "change_biblio_param",
            "state_remove",
            "");

                    string strOldState = strState;

                    if (String.IsNullOrEmpty(strAdd) == false)
                        StringUtil.SetInList(ref strState, strAdd, true);
                    if (String.IsNullOrEmpty(strRemove) == false)
                        StringUtil.SetInList(ref strState, strRemove, false);

                    if (strOldState != strState)
                    {
                        MarcUtil.ReplaceSubfieldContent(ref strField998,
                            "s", strState);
                        bChanged = true;
                    }
                }
                else
                {
                    if (strStateAction != strState)
                    {
                        MarcUtil.ReplaceSubfieldContent(ref strField998,
                            "s", strStateAction);
                        bChanged = true;
                    }
                }

            }


            // time
            string strTimeAction = this.MainForm.AppInfo.GetString(
    "change_biblio_param",
    "opertime",
    "<���ı�>");
            if (strTimeAction != "<���ı�>")
            {
                string strTime = MarcUtil.GetSubfieldContent(strField998,
    "u");
                DateTime time = new DateTime(0);
                if (strTimeAction == "<��ǰʱ��>")
                {
                    time = now;
                }
                else if (strTimeAction == "<���>")
                {

                }
                else if (strTimeAction == "<ָ��ʱ��>")
                {
                    string strValue = this.MainForm.AppInfo.GetString(
                        "change_biblio_param",
                        "opertime_value",
                        "");
                    if (String.IsNullOrEmpty(strValue) == true)
                    {
                        strError = "������ <ָ��ʱ��> ��ʽ���޸�ʱ����ָ����ʱ��ֵ����Ϊ��";
                        return -1;
                    }
                    try
                    {
                        time = DateTime.Parse(strValue);
                    }
                    catch (Exception ex)
                    {
                        strError = "�޷�����ʱ���ַ��� '" + strValue + "' :" + ex.Message;
                        return -1;
                    }
                }
                else
                {
                    // ��֧��
                    strError = "��֧�ֵ�ʱ�䶯�� '" + strTimeAction + "'";
                    return -1;
                }

                string strOldTime = strTime;

                if (strTimeAction == "<���>")
                    strTime = "";
                else
                    strTime = time.ToString("u");

                if (strOldTime != strTime)
                {
                    MarcUtil.ReplaceSubfieldContent(ref strField998,
    "u", strTime);
                    bChanged = true;
                }
            }


            // batchno
            string strBatchNoAction = this.MainForm.AppInfo.GetString(
"change_biblio_param",
"batchNo",
"<���ı�>");
            if (strBatchNoAction != "<���ı�>")
            {
                string strBatchNo = MarcUtil.GetSubfieldContent(strField998,
                    "a");

                if (strBatchNo != strBatchNoAction)
                {
                    MarcUtil.ReplaceSubfieldContent(ref strField998,
                        "a", strBatchNoAction);
                    bChanged = true;
                }
            }

            if (bChanged == false)
                return 0;

            // 
            MarcUtil.ReplaceField(ref strMARC,
                "998",
                0,
                strField998);

            return 1;
        }

        // ���ö�������
        // return:
        //      false   ����
        //      true    ȷ��
        public bool SetChangeParameters()
        {
            ChangeBiblioActionDialog dlg = new ChangeBiblioActionDialog();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.GetValueTable -= new GetValueTableEventHandler(dlg_GetValueTable);
            dlg.GetValueTable += new GetValueTableEventHandler(dlg_GetValueTable);
            dlg.MainForm = this.MainForm;

            this.MainForm.AppInfo.LinkFormState(dlg, "ChangeBiblioActionDialog_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.OK)
                return true;

            return false;
        }

        private void button_changeParam_Click(object sender, EventArgs e)
        {
            SetChangeParameters();
        }

        void dlg_GetValueTable(object sender, GetValueTableEventArgs e)
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

        private void QuickChangeBiblioForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;

        }

        private void button_file_getRecpathFilename_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵�(��Ŀ��)��¼·���ļ���";
            dlg.FileName = this.textBox_recpathFile.Text;
            // dlg.InitialDirectory = 
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_recpathFile.Text = dlg.FileName;
        }




    }
}