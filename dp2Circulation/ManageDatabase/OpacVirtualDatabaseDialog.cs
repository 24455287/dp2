using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.CommonControl;
using DigitalPlatform.Xml;

namespace dp2Circulation
{
    /*
        <virtualDatabase>
            <caption lang="zh-CN">�����鿯</caption>
            <caption lang="en">Chinese Books and Series</caption>
            <from style="title">
                <caption lang="zh-CN">����</caption>
                <caption lang="en">Title</caption>
            </from>
            <from style="author">
                <caption lang="zh-CN">����</caption>
                <caption lang="en">Author</caption>
            </from>
            <database name="����ͼ��" />
            <database name="�����ڿ�" />
        </virtualDatabase>
     * * */
    /// <summary>
    /// �������� OPAC ����������� virtualDatabase Ԫ�صĶԻ���
    /// </summary>
    internal partial class OpacVirtualDatabaseDialog : Form
    {
        /// <summary>
        /// �Ƿ�Ϊ����ģʽ?
        /// true: ����ģʽ; false: �޸�ģʽ
        /// </summary>
        public bool CreateMode = false;

        /// <summary>
        /// ϵͳ����
        /// </summary>
        public ManagerForm ManagerForm = null;

        public string Xml = ""; // ���ڴ�ǰ����װ�س�ʼ�����壬���ڹرպ��÷����޸ĺ�Ķ���

        public List<string> ExistingOpacNormalDbNames = new List<string>(); // �Ѿ����ڵ���ͨ��������ӳ�Ա���ʱ��Ӧ���������Χ����ѡ

        public OpacVirtualDatabaseDialog()
        {
            InitializeComponent();
        }

        public int Initial(ManagerForm managerform,
            bool bCreateMode,
            string strXml,
            out string strError)
        {
            strError = "";

            this.ManagerForm = managerform;
            this.CreateMode = bCreateMode;

            this.Xml = strXml;

            // ��䴰������
            if (String.IsNullOrEmpty(strXml) == false)
            {
                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "XMLװ�ص�DOMʱ����: " + ex.Message;
                    return -1;
                }

                // �������captions
                XmlNodeList nodes = dom.DocumentElement.SelectNodes("caption");
                string strCaptionsXml = "";
                for (int i = 0; i < nodes.Count; i++)
                {
                    strCaptionsXml += nodes[i].OuterXml;
                }

                if (String.IsNullOrEmpty(strCaptionsXml) == false)
                    this.captionEditControl_virtualDatabaseName.Xml = strCaptionsXml;

                // ��Ա��
                nodes = dom.DocumentElement.SelectNodes("database");
                string strMemberDatabaseNames = "";
                for (int i = 0; i < nodes.Count; i++)
                {
                    strMemberDatabaseNames += DomUtil.GetAttr(nodes[i], "name") + "\r\n";
                }
                this.textBox_memberDatabases.Text = strMemberDatabaseNames;

                // froms����
                nodes = dom.DocumentElement.SelectNodes("from");
                string strFromsXml = "";
                for (int i = 0; i < nodes.Count; i++)
                {
                    strFromsXml += nodes[i].OuterXml;
                }
                if (String.IsNullOrEmpty(strFromsXml) == false)
                    this.fromEditControl1.Xml = strFromsXml;
            }

            return 0;
        }

        public List<string> MemberDatabaseNames
        {
            get
            {
                return this.GetMemberDatabaseNames();
            }
            set
            {
                string strText = "";
                for (int i = 0; i < value.Count; i++)
                {
                    string strOne = value[i];
                    if (String.IsNullOrEmpty(strOne) == true)
                        continue;
                    strText += strOne + "\r\n";
                }

                this.textBox_memberDatabases.Text = strText;
            }
        }

        private void OpacVirtualDatabaseDialog_Load(object sender, EventArgs e)
        {
            if (this.CreateMode == true)
            {
                if (String.IsNullOrEmpty(this.captionEditControl_virtualDatabaseName.Xml) == true)
                    this.captionEditControl_virtualDatabaseName.Xml = "<caption lang='zh'></caption><caption lang='en'></caption>";
            }
            else
            {

            }
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            // ���м��
            string strError = "";

            // ����������

            // ��鵱ǰ������ʽ���Ƿ�Ϸ�
            // return:
            //      -1  �����̱������
            //      0   ��ʽ�д���
            //      1   ��ʽû�д���
            int nRet = this.captionEditControl_virtualDatabaseName.Verify(out strError);
            if (nRet <= 0)
            {
                strError = "�������������: " + strError;
                this.tabControl_main.SelectedTab = this.tabPage_virtualDatabaseName;
                this.captionEditControl_virtualDatabaseName.Focus();
                goto ERROR1;
            }

            // ����������������������е����ݿ����Ƿ��ظ�
            if (this.CreateMode == true)
            {
                // �����������������Ƿ�͵�ǰ�Ѿ����ڵ���������ظ�
                // return:
                //      -1  ���Ĺ��̷�������
                //      0   û���ظ�
                //      1   ���ظ�
                nRet = this.ManagerForm.DetectVirtualDatabaseNameDup(this.captionEditControl_virtualDatabaseName.Xml,
                    out strError);
                if (nRet == -1 || nRet == 1)
                    goto ERROR1;
            }

            // ����Ա����
            List<string> dbnames = GetMemberDatabaseNames();
            if (dbnames.Count == 0)
            {
                strError = "��δָ����Ա����: " + strError;
                this.tabControl_main.SelectedTab = this.tabPage_memberDatabases;
                this.textBox_memberDatabases.Focus();
                goto ERROR1;

            }

            // ������;������
            nRet = this.fromEditControl1.Verify(out strError);
            if (nRet <= 0)
            {
                strError = "����;������������: " + strError;
                this.tabControl_main.SelectedTab = this.tabPage_froms;
                this.fromEditControl1.Focus();
                goto ERROR1;
            }

            // ������Է��͸���������XML����
            string strXml = "";
            nRet = BuildXml(out strXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.Xml = strXml;

            this.DialogResult = DialogResult.OK;
            this.Close();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ������Է��͸���������XML����
        int BuildXml(out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<virtualDatabase />");

            // �����ʾ���������captions
            XmlDocumentFragment fragment = dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = this.captionEditControl_virtualDatabaseName.Xml;
            }
            catch (Exception ex)
            {
                strError = "virtual database name captions fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                return -1;
            }

            dom.DocumentElement.AppendChild(fragment);

            // froms
            fragment = dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = this.fromEditControl1.Xml;
            }
            catch (Exception ex)
            {
                strError = "froms fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                return -1;
            }

            dom.DocumentElement.AppendChild(fragment);

            // member databases
            List<string> dbnames = GetMemberDatabaseNames();
            for (int i = 0; i < dbnames.Count; i++)
            {
                XmlNode node = dom.CreateElement("database");
                dom.DocumentElement.AppendChild(node);
                DomUtil.SetAttr(node, "name", dbnames[i]);
            }

            strXml = dom.DocumentElement.OuterXml;

            return 0;
        }

        // ����һ����Ա����
        private void button_insertMemberDatabaseName_Click(object sender, EventArgs e)
        {
            int nRet = 0;
            string strError = "";
            /*
            int x = 0;
            int y = 0;
            API.GetEditCurrentCaretPos(
                this.textBox_memberDatabases,
                out x,
                out y);

            string strLine = "";

            if (this.textBox_memberDatabases.Lines.Length > 0)
                strLine = this.textBox_memberDatabases.Lines[y];
             * */

            // Ҫ�ų������ݿ���
            // ���������һ��Ϊ�Ѿ���Ϊ��Ա����ʹ���˵ģ�һ��Ϊ��δ����ΪOPAC��ͨ���
            List<string> exclude_dbnames = new List<string>();
            for (int i = 0; i < this.textBox_memberDatabases.Lines.Length; i++)
            {
                string strLine = this.textBox_memberDatabases.Lines[i].Trim();
                if (String.IsNullOrEmpty(strLine) == true)
                    continue;

                exclude_dbnames.Add(strLine);
            }

                // ����OPAC�Ѿ��������ͨ����֮�У�Ҫ�ų�
            List<string> exclude1 = null;
            nRet = GetExcludeDbNames(this.ManagerForm.AllDatabaseInfoXml,
                this.ExistingOpacNormalDbNames,
                out exclude1,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            exclude_dbnames.AddRange(exclude1);

            GetOpacMemberDatabaseNameDialog dlg = new GetOpacMemberDatabaseNameDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.SelectedDatabaseName = this.textBox_memberDatabases.SelectedText;
            dlg.ManagerForm = this.ManagerForm;
            dlg.AllDatabaseInfoXml = this.ManagerForm.AllDatabaseInfoXml;
            dlg.ExcludingDbNames = exclude_dbnames;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            string strNewText = dlg.SelectedDatabaseName.Replace(",", "\r\n");

            // �����ǰû��ѡ�����ַ�Χ�Ļ�����Ҫ���²��������ĩβ���ӻس����з���
            if (String.IsNullOrEmpty(this.textBox_memberDatabases.SelectedText) == true)
                strNewText += "\r\n";

            this.textBox_memberDatabases.Paste(strNewText);
            this.textBox_memberDatabases.Focus();

            // SetLineText(this.textBox_memberDatabases, y, dlg.SelectedDatabaseName);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ���Ҫ�ų��ġ���δ����ΪOPAC��ͨ������ݿ���
        int GetExcludeDbNames(string strAllDatbaseInfo,
            List<string> opac_normal_dbnames,
            out List<string> results,
            out string strError)
        {
            strError = "";
            results = new List<string>();

            if (String.IsNullOrEmpty(strAllDatbaseInfo) == true)
                return 0;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strAllDatbaseInfo);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strName = DomUtil.GetAttr(node, "name");

                if (opac_normal_dbnames.IndexOf(strName) == -1)
                    results.Add(strName);
            }

            return 0;
        }

#if NO
        public static void SetLineText(TextBox textbox,
    int nLine,
    string strValue)
        {
            string strText = textbox.Text.Replace("\r\n", "\r");
            string[] lines = strText.Split(new char[] { '\r' });

            strText = "";
            for (int i = 0; i < Math.Max(nLine, lines.Length); i++)
            {
                if (i != 0)
                    strText += "\r\n";

                if (i == nLine)
                    strText += strValue;
                else
                {
                    if (i < lines.Length)
                        strText += lines[i];
                    else
                        strText += "";
                }

            }

            textbox.Text = strText;
        }
#endif

        // Ϊ�������captions�༭��ĩβ����һ��
        private void button_virtualDatabaseName_newLine_Click(object sender, EventArgs e)
        {
            this.captionEditControl_virtualDatabaseName.NewElement();
        }

        // �ڼ���;�������б��У�����һ�����У����뵽���
        private void button_froms_newBlankLine_Click(object sender, EventArgs e)
        {
            this.fromEditControl1.NewElement(true);
        }

        // ��ó�Ա���ݿ����б�
        // ���Զ�ȥ������
        List<string> GetMemberDatabaseNames()
        {
            List<string> results = new List<string>();
            for (int i = 0; i < this.textBox_memberDatabases.Lines.Length; i++)
            {
                string strLine = this.textBox_memberDatabases.Lines[i].Trim();
                if (String.IsNullOrEmpty(strLine) == true)
                    continue;
                results.Add(strLine);
            }

            return results;
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public void EnableControls(bool bEnable)
        {
            this.tabControl_main.Enabled = bEnable;

            this.button_OK.Enabled = bEnable;
            this.button_Cancel.Enabled = bEnable;
        }

        // �����Ա���ȫ������;��(��ʾʱ��ȥ�غϲ�)����ǰ����;�������С�
        // ���뵱ǰ���崰ʱ�������ظ���styleҪ���档�����ظ�style��<from>�����û�ѡ������ǰ�Ļ������µĳ���
        private void button_from_import_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            List<string> dbnames = GetMemberDatabaseNames();
            if (dbnames.Count == 0)
            {
                strError = "��δ�����Ա����������޷������Ա��ļ���;������";
                goto ERROR1;
            }

            ImportFromsDialog dlg = new ImportFromsDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            this.EnableControls(false);
            nRet = dlg.Initial(this.ManagerForm,
                dbnames,
                out strError);
            this.EnableControls(true);
            if (nRet == -1)
                goto ERROR1;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            if (this.fromEditControl1.Elements.Count == 0)
                this.fromEditControl1.Xml = dlg.SelectedFromsXml;
            else
            {
                // �����ǰ�Ѿ������ݣ����Ѻϲ��������
                DialogResult result = MessageBox.Show(this,
"��ǰ�Ѵ��ڼ���;��������Ϣ���Ƿ�Ҫ���������Ҫ�ϲ�����ǰ����?\r\n\r\n(Yes: �ϲ�; No: ����; Cancel: ����)",
"OpacVirtuslDatabaseDialog",
MessageBoxButtons.YesNoCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                {
                    // �ϲ�Ҫ����ļ���;������
                    nRet = MergeImportFroms(dlg.SelectedFromsXml,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                }
                if (result == DialogResult.No)
                {
                    // ����
                    this.fromEditControl1.Xml = dlg.SelectedFromsXml;
                }
                if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �ϲ�Ҫ����ļ���;������
        int MergeImportFroms(string strXml,
            out string strError)
        {
            strError = "";
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            try
            {
                dom.DocumentElement.InnerXml = strXml;
            }
            catch (Exception ex)
            {
                strError = "Set InnerXml error: " + ex.Message;
                return -1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("from");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strFromStyle = DomUtil.GetAttr(node, "style");

                bool bFound = false;
                for (int j = 0; j < this.fromEditControl1.Elements.Count; j++)
                {
                    FromElement element = this.fromEditControl1.Elements[j];

                    if (element.Style == strFromStyle)
                    {
                        bFound = true;

                        // TODO: �����ҵ��������û�б�Ҫ��captions���кϲ�?
                        break;
                    }
                }

                if (bFound == true)
                {
                    continue;
                }

                FromElement new_element = this.fromEditControl1.AppendNewElement();
                new_element.Style = strFromStyle;
                new_element.CaptionsXml = node.InnerXml;
            }

            return 0;
        }
    }
}