using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using System.Collections;

namespace dp2Circulation
{
    /// <summary>
    /// ������Ŀ��(��)�ĶԻ���
    /// ��Ŀ�������������ݿ���ɣ�����Ƚϸ���
    /// </summary>
    internal partial class BiblioDatabaseDialog : Form
    {
        int m_nInInitial = 0;

        /// <summary>
        /// ϵͳ����
        /// </summary>
        public ManagerForm ManagerForm = null;

        public bool CreateMode = false; // �Ƿ�Ϊ����ģʽ��==trueΪ����ģʽ��==falseΪ�޸�ģʽ
        public bool Recreate = false;   // �Ƿ�Ϊ���´���ģʽ����CreateMode == true ʱ������
        XmlDocument dom = null;

        public BiblioDatabaseDialog()
        {
            InitializeComponent();
        }

        // ��� ���Ʋ������������
        void SetReplicationParam(string strText)
        {
            Hashtable table = StringUtil.ParseParameters(strText);
            this.comboBox_replication_centerServer.Text = (string)table["server"];
            this.comboBox_replication_dbName.Text = (string)table["dbname"];
        }

        // �ӽ����Ѽ����Ʋ���
        string GetReplicationParam()
        {
            if (string.IsNullOrEmpty(this.comboBox_replication_centerServer.Text) == true
                && string.IsNullOrEmpty(this.comboBox_replication_dbName.Text) == true)
                return "";

            Hashtable table = new Hashtable();
            table["server"] = this.comboBox_replication_centerServer.Text;
            table["dbname"] = this.comboBox_replication_dbName.Text;
            return StringUtil.BuildParameterString(table);
        }

        public int Initial(string strXml,
            out string strError)
        {
            strError = "";

            this.dom = new XmlDocument();
            try
            {
                this.dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            string strType = DomUtil.GetAttr(dom.DocumentElement,
                "type");
            if (strType != "biblio")
            {
                strError = "<database>Ԫ�ص�type����ֵ('" + strType + "')Ӧ��Ϊbiblio";
                return -1;
            }

            this.m_nInInitial++;    // ����xxxchanged��Ӧ

            try
            {
                this.textBox_biblioDbName.Text = DomUtil.GetAttr(dom.DocumentElement,
                    "name");
                this.comboBox_syntax.Text = DomUtil.GetAttr(dom.DocumentElement,
                    "syntax");
                // 2009/10/23 new add
                this.checkedComboBox_role.Text = DomUtil.GetAttr(dom.DocumentElement,
                    "role");

                SetReplicationParam(DomUtil.GetAttr(dom.DocumentElement, "replication"));

                this.textBox_entityDbName.Text = DomUtil.GetAttr(dom.DocumentElement,
                    "entityDbName");
                this.textBox_issueDbName.Text = DomUtil.GetAttr(dom.DocumentElement,
                    "issueDbName");
                this.textBox_orderDbName.Text = DomUtil.GetAttr(dom.DocumentElement,
                    "orderDbName");
                this.textBox_commentDbName.Text = DomUtil.GetAttr(dom.DocumentElement,
                    "commentDbName");

                string strInCirculation = DomUtil.GetAttr(dom.DocumentElement,
                    "inCirculation");
                if (String.IsNullOrEmpty(strInCirculation) == true)
                    strInCirculation = "true";
                this.checkBox_inCirculation.Checked = DomUtil.IsBooleanTrue(strInCirculation);


                // usage����һ���ڴӷ�����������XMLƬ������û�еġ������ڴ�����ʱ��client����server��xmlƬ���в���
                string strUsage = DomUtil.GetAttr(dom.DocumentElement,
                    "usage");
                if (String.IsNullOrEmpty(strUsage) == true)
                {
                    // ���usage����Ϊ�գ�����Ҫ�ۺ��ж�
                    if (this.textBox_issueDbName.Text == "")
                    {
                        strUsage = "book -- ͼ��";
                    }
                    else
                    {
                        strUsage = "series -- �ڿ�";
                    }
                }

                this.comboBox_documentType.Text = strUsage;
            }
            finally
            {
                this.m_nInInitial--;
            }


            return 0;
        }

        private void BiblioDatabaseDialog_Load(object sender, EventArgs e)
        {
            if (this.CreateMode == false)
            {
                this.comboBox_syntax.Enabled = false;
                this.comboBox_documentType.Enabled = false;
            }

        }

        private void textBox_biblioDbName_TextChanged(object sender, EventArgs e)
        {
            if (this.m_nInInitial > 0)
                return;

            if (this.CreateMode == true)
            {
                /*
                string strSyntax = GetPureValue(this.comboBox_syntax.Text);
                if (String.IsNullOrEmpty(strSyntax) == true)
                    strSyntax = "unimarc";
                 * */

                string strUsage = GetPureValue(this.comboBox_documentType.Text);
                if (String.IsNullOrEmpty(strUsage) == true)
                    strUsage = "book";

                if (strUsage == "book")
                {
                    if (this.textBox_biblioDbName.Text == "")
                    {
                        this.textBox_entityDbName.Text = "";
                        this.textBox_orderDbName.Text = "";
                        this.textBox_issueDbName.Text = "";
                        this.textBox_commentDbName.Text = "";
                    }
                    else
                    {
                        this.textBox_entityDbName.Text = this.textBox_biblioDbName.Text + "ʵ��";
                        this.textBox_orderDbName.Text = this.textBox_biblioDbName.Text + "����";
                        this.textBox_issueDbName.Text = "";
                        this.textBox_commentDbName.Text = this.textBox_biblioDbName.Text + "��ע";
                    }
                }
                else if (strUsage == "series")
                {
                    if (this.textBox_biblioDbName.Text == "")
                    {
                        this.textBox_entityDbName.Text = "";
                        this.textBox_orderDbName.Text = "";
                        this.textBox_issueDbName.Text = "";
                        this.textBox_commentDbName.Text = "";
                    }
                    else
                    {
                        this.textBox_entityDbName.Text = this.textBox_biblioDbName.Text + "ʵ��";
                        this.textBox_orderDbName.Text = this.textBox_biblioDbName.Text + "����";
                        this.textBox_issueDbName.Text = this.textBox_biblioDbName.Text + "��";
                        this.textBox_commentDbName.Text = this.textBox_biblioDbName.Text + "��ע";
                    }
                }
            }
        }

        static string GetPureValue(string strText)
        {
            int nRet = strText.IndexOf("--");
            if (nRet == -1)
                return strText;

            return strText.Substring(0, nRet).Trim();
        }

        private void comboBox_usage_TextChanged(object sender, EventArgs e)
        {
            if (this.m_nInInitial > 0)
                return;

            string strUsage = GetPureValue(this.comboBox_documentType.Text);
            if (String.IsNullOrEmpty(strUsage) == true)
                strUsage = "book";

            if (strUsage == "book")
            {
                this.textBox_issueDbName.Text = "";
            }
            else if (strUsage == "series")
            {
                if (this.textBox_biblioDbName.Text == "")
                    this.textBox_issueDbName.Text = "";
                else
                    this.textBox_issueDbName.Text = this.textBox_biblioDbName.Text + "��";
            }
        }

        static string MakeListString(List<string> names)
        {
            string strResult = "";
            for (int i = 0; i < names.Count; i++)
            {
                strResult += names[i] + "\r\n";
            }

            return strResult;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ���

            // syntax
            if (this.comboBox_syntax.Text == "")
            {
                strError = "��δָ�����ݸ�ʽ";
                goto ERROR1;
            }

            if (this.comboBox_documentType.Text == "")
            {
                strError = "��δָ����������";
                goto ERROR1;
            }

            if (this.checkBox_inCirculation.Checked == true)
            {
                if (String.IsNullOrEmpty(this.textBox_entityDbName.Text) == true)
                {
                    strError = "Ҫ������ͨ���ͱ���ָ��ʵ�����";
                    goto ERROR1;
                }
            }

            if ((string.IsNullOrEmpty(this.comboBox_replication_dbName.Text) == false
                && string.IsNullOrEmpty(this.comboBox_replication_centerServer.Text) == true)
                ||
            (string.IsNullOrEmpty(this.comboBox_replication_dbName.Text) == true
                && string.IsNullOrEmpty(this.comboBox_replication_centerServer.Text) == false))
            {
                strError = "��ͬ��������ҳ �� ���ķ����� �� ��Ŀ����������ͬʱ�߱�";
                goto ERROR1;
            }

            string strUsage = GetPureValue(this.comboBox_documentType.Text);
            string strRole = this.checkedComboBox_role.Text;
            string strReplication = GetReplicationParam();

            {
            REDO:
                if (strUsage == "book")
                {
                    if (String.IsNullOrEmpty(this.textBox_issueDbName.Text) == false)
                    {
                        // 2009/2/6 new add
                        if (this.CreateMode == false)
                        {
                            // �Ի��򾯸�
                            DialogResult result = MessageBox.Show(this,
                                "ȷʵҪ����Ŀ�� '"
                                + this.textBox_biblioDbName.Text
                                + "' �����������޸�Ϊ �ڿ�?",
                                "BiblioDatabaseDialog",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button2);
                            if (result != DialogResult.Yes)
                            {
                                strError = "����������Ϊ ͼ�� ʱ������ָ���ڿ���";
                                goto ERROR1;
                            }

                            strUsage = "series";  // ���ⲻ�ı�combobox�����ݣ��Ա�Cancel���ܹ��ָ�ԭ״
                            goto REDO;
                        }
                        else
                        {

                            strError = "����������Ϊ ͼ�� ʱ������ָ���ڿ���";
                            goto ERROR1;
                        }
                    }
                }
                else if (strUsage == "series")
                {
                    if (StringUtil.IsInList("orderWork", strRole) == true)
                    {
                        strError = "����������Ϊ �ڿ� ʱ����ɫ����Ϊ orderWork (�ɹ�������)";
                        goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(this.textBox_issueDbName.Text) == true)
                    {
                        // 2009/2/6 new add
                        if (this.CreateMode == false)
                        {
                            // �Ի��򾯸�
                            DialogResult result = MessageBox.Show(this,
                                "ȷʵҪ����Ŀ�� '"
                                + this.textBox_biblioDbName.Text
                                + "' �����������޸�Ϊ ͼ��?",
                                "BiblioDatabaseDialog",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button2);
                            if (result != DialogResult.Yes)
                            {
                                strError = "����������Ϊ �ڿ� ʱ������ָ���ڿ���";
                                goto ERROR1;
                            }

                            strUsage = "book";  // ���ⲻ�ı�combobox�����ݣ��Ա�Cancel���ܹ��ָ�ԭ״
                            goto REDO;
                        }
                        else
                        {
                            strError = "����������Ϊ �ڿ� ʱ������ָ���ڿ���";
                            goto ERROR1;
                        }
                    }
                }
            }

            // ��� �ɹ������� �ļ��
            if (StringUtil.IsInList("orderWork", strRole) == true)
            {
                if (String.IsNullOrEmpty(this.textBox_orderDbName.Text) == true)
                {
                    strError = "����ɫΪ orderWork (�ɹ�������)ʱ���������������";
                    goto ERROR1;
                }

                // 2009/11/5 new add
                if (String.IsNullOrEmpty(this.textBox_entityDbName.Text) == true)
                {
                    strError = "����ɫΪ orderWork (�ɹ�������)ʱ���������ʵ���";
                    goto ERROR1;
                }

                if (this.checkBox_inCirculation.Checked == true)
                {
                    strError = "����ɫΪ orderWork (�ɹ�������)ʱ�����ܲ�����ͨ";
                    goto ERROR1;
                }
            }

            // ��� ��Դ��Ŀ�� �ļ��
            if (StringUtil.IsInList("biblioSource", strRole) == true)
            {
                if (String.IsNullOrEmpty(this.textBox_biblioDbName.Text) == true)
                {
                    strError = "����ɫΪ biblioSource (��Դ��Ŀ��)ʱ�����������Ŀ��";
                    goto ERROR1;
                }

                if (this.checkBox_inCirculation.Checked == true)
                {
                    strError = "����ɫΪ biblioSource (��Դ��Ŀ��)ʱ�����ܲ�����ͨ";
                    goto ERROR1;
                }
            }

            if (this.CreateMode == true)
            {
                // ����ģʽ
                EnableControls(false);

                try
                {
                    string strDatabaseInfo = "";
                    string strOutputInfo = "";

                    XmlDocument dom = new XmlDocument();
                    dom.LoadXml("<root />");
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    // type
                    DomUtil.SetAttr(nodeDatabase, "type", "biblio");

                    // syntax
                    if (this.comboBox_syntax.Text == "")
                    {
                        strError = "��δָ�����ݸ�ʽ";
                        goto ERROR1;
                    }
                    string strSyntax = GetPureValue(this.comboBox_syntax.Text);
                    DomUtil.SetAttr(nodeDatabase, "syntax", strSyntax);

                    // usage
                    /*
                    if (this.comboBox_documentType.Text == "")
                    {
                        strError = "��δָ����������";
                        goto ERROR1;
                    }
                    string strUsage = GetPureValue(this.comboBox_documentType.Text);
                     * */

                    DomUtil.SetAttr(nodeDatabase, "usage", strUsage);

                    // role
                    DomUtil.SetAttr(nodeDatabase, "role", strRole);

                    if (string.IsNullOrEmpty(strReplication) == false)
                        DomUtil.SetAttr(nodeDatabase, "replication", strReplication);

                    // inCirculation
                    string strInCirculation = "true";
                    if (this.checkBox_inCirculation.Checked == true)
                        strInCirculation = "true";
                    else
                        strInCirculation = "false";

                    DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);


                    // ��Ŀ����
                    if (this.textBox_biblioDbName.Text == "")
                    {
                        strError = "��δָ����Ŀ����";
                        goto ERROR1;
                    }
                    nRet = Global.CheckDbName(this.textBox_biblioDbName.Text,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    DomUtil.SetAttr(nodeDatabase, "name", this.textBox_biblioDbName.Text);

                    // ʵ�����
                    if (this.textBox_entityDbName.Text != "")
                    {
                        /*
                        if (this.textBox_entityDbName.Text == "")
                        {
                            strError = "��δָ��ʵ�����";
                            goto ERROR1;
                        }
                         * */

                        nRet = Global.CheckDbName(this.textBox_entityDbName.Text,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        DomUtil.SetAttr(nodeDatabase, "entityDbName", this.textBox_entityDbName.Text);
                    }

                    // ��������
                    if (this.textBox_orderDbName.Text != "")
                    {
                        nRet = Global.CheckDbName(this.textBox_orderDbName.Text,
        out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        DomUtil.SetAttr(nodeDatabase, "orderDbName", this.textBox_orderDbName.Text);
                    }

                    // ����ڿ����ľ߱���usage�Ƿ�ì��
                    if (String.IsNullOrEmpty(strUsage) == true)
                        strUsage = "book";

                    if (strUsage == "book")
                    {
                        if (this.textBox_issueDbName.Text != "")
                        {
                            strError = "��;Ϊbookʱ���ڿ�������Ϊ��";
                            goto ERROR1;
                        }
                    }
                    else if (strUsage == "series")
                    {
                        if (this.textBox_issueDbName.Text == "")
                        {
                            strError = "��;Ϊseriesʱ���ڿ�������߱�";
                            goto ERROR1;
                        }
                    }

                    // �ڿ���
                    if (this.textBox_issueDbName.Text != "")
                    {
                        nRet = Global.CheckDbName(this.textBox_issueDbName.Text,
    out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        DomUtil.SetAttr(nodeDatabase, "issueDbName", this.textBox_issueDbName.Text);
                    }

                    // ��ע����
                    if (this.textBox_commentDbName.Text != "")
                    {
                        nRet = Global.CheckDbName(this.textBox_commentDbName.Text,
    out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        DomUtil.SetAttr(nodeDatabase, "commentDbName", this.textBox_commentDbName.Text);
                    }


                    // Ϊȷ����ݶ���¼
                    // return:
                    //      -1  ����
                    //      0   ������¼
                    //      1   ��¼�ɹ�
                    nRet = this.ManagerForm.ConfirmLogin(out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == 0)
                    {
                        strError = "�������ݿ�Ĳ���������";
                        MessageBox.Show(this, strError);

                    }
                    else
                    {
                        strDatabaseInfo = dom.OuterXml;

                        // �������ݿ�
                        nRet = this.ManagerForm.CreateDatabase(
                            strDatabaseInfo,
                            this.Recreate,
                            out strOutputInfo,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                }
                finally
                {
                    EnableControls(true);
                }
            }
            else
            {
                // �޸�ģʽ
                EnableControls(false);

                try
                {
                    string strDatabaseInfo = "";
                    string strOutputInfo = "";

                    // �޸ĵ����ݿ���
                    List<string> change_dbnames = new List<string>();
                    // ɾ�������ݿ���
                    List<string> delete_dbnames = new List<string>();
                    // ���������ݿ���
                    List<string> create_dbnames = new List<string>();

                    // �����޸������DOM
                    XmlDocument change_dom = new XmlDocument();
                    change_dom.LoadXml("<root />");
                    XmlNode nodeChangeDatabase = change_dom.CreateElement("database");
                    change_dom.DocumentElement.AppendChild(nodeChangeDatabase);

                    /*
                    // ����ɾ�������DOM
                    XmlDocument delete_dom = new XmlDocument();
                    delete_dom.LoadXml("<root />");
                     * */

                    // ���ڴ��������DOM
                    XmlDocument create_dom = new XmlDocument();
                    create_dom.LoadXml("<root />");

                    // type
                    DomUtil.SetAttr(nodeChangeDatabase, "type", "biblio");

                    // syntax
                    if (this.comboBox_syntax.Text == "")
                    {
                        strError = "��δָ�����ݸ�ʽ";
                        goto ERROR1;
                    }
                    string strSyntax = GetPureValue(this.comboBox_syntax.Text);
                    DomUtil.SetAttr(nodeChangeDatabase, "syntax", strSyntax);

                    // usage

                    /*
                    if (this.comboBox_documentType.Text == "")
                    {
                        strError = "��δָ����������";
                        goto ERROR1;
                    }
                    string strUsage = GetPureValue(this.comboBox_documentType.Text);
                     * */
                    DomUtil.SetAttr(nodeChangeDatabase, "usage", strUsage);


                    // ����ڿ����ľ߱���usage�Ƿ�ì��
                    if (String.IsNullOrEmpty(strUsage) == true)
                        strUsage = "book";

                    if (strUsage == "book")
                    {
                        if (this.textBox_issueDbName.Text != "")
                        {
                            strError = "��;Ϊbookʱ���ڿ�������Ϊ��";
                            goto ERROR1;
                        }
                    }
                    else if (strUsage == "series")
                    {
                        if (this.textBox_issueDbName.Text == "")
                        {
                            strError = "��;Ϊseriesʱ���ڿ�������߱�";
                            goto ERROR1;
                        }
                    }

                    // ��Ŀ����
                    string strOldBiblioDbName = DomUtil.GetAttr(this.dom.DocumentElement,
                        "name");

                    if (String.IsNullOrEmpty(strOldBiblioDbName) == false
                        && this.textBox_biblioDbName.Text == "")
                    {
                        strError = "��Ŀ���������޸�Ϊ��";
                        goto ERROR1;
                    }

                    bool bChanged = false;  // �Ƿ���ʵ�����޸�����������ڱ�ʾ�Ƿ���Ҫ������ɾ�����ݿ�

                    if (strOldBiblioDbName != this.textBox_biblioDbName.Text)
                    {
                        nRet = Global.CheckDbName(this.textBox_biblioDbName.Text,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        DomUtil.SetAttr(nodeChangeDatabase, "name", this.textBox_biblioDbName.Text);
                        bChanged = true;
                        change_dbnames.Add(strOldBiblioDbName + " --> " + this.textBox_biblioDbName.Text);
                    }

                    bool bInCirculationChanged = false;

                    // �Ƿ������ͨ
                    string strOldInCirculation = DomUtil.GetAttr(this.dom.DocumentElement,
                        "inCirculation");
                    if (String.IsNullOrEmpty(strOldInCirculation) == true)
                        strOldInCirculation = "true";

                    bool bOldInCirculation = DomUtil.IsBooleanTrue(strOldInCirculation);
                    if (bOldInCirculation != this.checkBox_inCirculation.Checked)
                    {
                        // ��XML�о���inCirculation���Ե�ʱ�򣬲ű�ʾҪ�޸��������
                        DomUtil.SetAttr(nodeChangeDatabase, "name", this.textBox_biblioDbName.Text);
                        DomUtil.SetAttr(nodeChangeDatabase, "inCirculation",
                            this.checkBox_inCirculation.Checked == true ? "true" : "false");
                        bChanged = true;
                        bInCirculationChanged = true;
                    }

                    bool bRoleChanged = false;

                    // ��ɫ
                    string strOldRole = DomUtil.GetAttr(this.dom.DocumentElement,
                        "role");
                    if (strOldRole != strRole)
                    {
                        // ��XML�о���role���Ե�ʱ�򣬲ű�ʾҪ�޸��������
                        DomUtil.SetAttr(nodeChangeDatabase, "name", this.textBox_biblioDbName.Text);
                        DomUtil.SetAttr(nodeChangeDatabase, "role",
                            strRole);
                        bChanged = true;
                        bRoleChanged = true;
                    }

                    bool bReplicationChanged = false;

                    // ��ɫ
                    string strOldReplication = DomUtil.GetAttr(this.dom.DocumentElement,
                        "replication");
                    if (strOldReplication != strReplication)
                    {
                        // �� XML �о��� replication ���Ե�ʱ�򣬲ű�ʾҪ�޸��������
                        DomUtil.SetAttr(nodeChangeDatabase, "name", this.textBox_biblioDbName.Text);
                        DomUtil.SetAttr(nodeChangeDatabase, "replication",
                            strReplication);
                        bChanged = true;
                        bReplicationChanged = true;
                    }

                    // ʵ�����
                    string strOldEntityDbName = DomUtil.GetAttr(this.dom.DocumentElement,
                        "entityDbName");
                    if (this.textBox_entityDbName.Text == "")
                    {
                        if (String.IsNullOrEmpty(strOldEntityDbName) == false)
                        {
                            // ʵ��������������޸�Ϊ�գ���ʾҪɾ��ʵ���
                            /*
                            XmlNode nodeDeleteDatabase = delete_dom.CreateElement("database");
                            delete_dom.DocumentElement.AppendChild(nodeDeleteDatabase);

                            DomUtil.SetAttr(nodeDeleteDatabase, "name", strOldEntityDbName);
                             * */
                            delete_dbnames.Add(strOldEntityDbName);
                        }
                    }
                    else if (strOldEntityDbName != this.textBox_entityDbName.Text)
                    {

                        if (String.IsNullOrEmpty(strOldEntityDbName) == true)
                        {
                            // ʵ������ӿձ�Ϊ��ֵ����ʾҪ����ʵ���
                            XmlNode nodeCreateDatabase = create_dom.CreateElement("database");
                            create_dom.DocumentElement.AppendChild(nodeCreateDatabase);

                            DomUtil.SetAttr(nodeCreateDatabase, "name", this.textBox_entityDbName.Text);
                            DomUtil.SetAttr(nodeCreateDatabase, "type", "entity");
                            DomUtil.SetAttr(nodeCreateDatabase, "biblioDbName", this.textBox_biblioDbName.Text);
                            create_dbnames.Add(this.textBox_entityDbName.Text);
                        }
                        else
                        {
                            nRet = Global.CheckDbName(this.textBox_entityDbName.Text,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            DomUtil.SetAttr(nodeChangeDatabase, "entityDbName", this.textBox_entityDbName.Text);
                            bChanged = true;
                            change_dbnames.Add(strOldEntityDbName + " --> " + this.textBox_entityDbName.Text);
                        }
                    }

                    // ��������
                    string strOldOrderDbName = DomUtil.GetAttr(this.dom.DocumentElement,
                        "orderDbName");
                    if (this.textBox_orderDbName.Text == "")
                    {
                        if (String.IsNullOrEmpty(strOldOrderDbName) == false)
                        {
                            // ���������������޸�Ϊ�գ���ʾҪɾ��������
                            /*
                            XmlNode nodeDeleteDatabase = delete_dom.CreateElement("database");
                            delete_dom.DocumentElement.AppendChild(nodeDeleteDatabase);

                            DomUtil.SetAttr(nodeDeleteDatabase, "name", strOldOrderDbName);
                             * */
                            delete_dbnames.Add(strOldOrderDbName);
                        }
                    }
                    else if (strOldOrderDbName != this.textBox_orderDbName.Text)
                    {
                        if (String.IsNullOrEmpty(strOldOrderDbName) == true)
                        {
                            // ���������ӿձ�Ϊ��ֵ����ʾҪ����������
                            XmlNode nodeCreateDatabase = create_dom.CreateElement("database");
                            create_dom.DocumentElement.AppendChild(nodeCreateDatabase);

                            DomUtil.SetAttr(nodeCreateDatabase, "name", this.textBox_orderDbName.Text);
                            DomUtil.SetAttr(nodeCreateDatabase, "type", "order");
                            DomUtil.SetAttr(nodeCreateDatabase, "biblioDbName", this.textBox_biblioDbName.Text);
                            create_dbnames.Add(this.textBox_orderDbName.Text);
                        }
                        else
                        {
                            nRet = Global.CheckDbName(this.textBox_orderDbName.Text,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            DomUtil.SetAttr(nodeChangeDatabase, "orderDbName", this.textBox_orderDbName.Text);
                            bChanged = true;
                            change_dbnames.Add(strOldOrderDbName + " --> " + this.textBox_orderDbName.Text);
                        }
                    }

                    // �ڿ���
                    string strOldIssueDbName = DomUtil.GetAttr(this.dom.DocumentElement,
                        "issueDbName");
                    if (this.textBox_issueDbName.Text == "")
                    {
                        if (String.IsNullOrEmpty(strOldIssueDbName) == false)
                        {
                            // �ڿ������������޸�Ϊ�գ���ʾҪɾ���ڿ�
                            /*
                            XmlNode nodeDeleteDatabase = delete_dom.CreateElement("database");
                            delete_dom.DocumentElement.AppendChild(nodeDeleteDatabase);

                            DomUtil.SetAttr(nodeDeleteDatabase, "name", strOldIssueDbName);
                             * */
                            delete_dbnames.Add(strOldIssueDbName);
                        }
                    }
                    else if (strOldIssueDbName != this.textBox_issueDbName.Text)
                    {
                        if (String.IsNullOrEmpty(strOldIssueDbName) == true)
                        {
                            // �ڿ����ӿձ�Ϊ��ֵ����ʾҪ�����ڿ�
                            XmlNode nodeCreateDatabase = create_dom.CreateElement("database");
                            create_dom.DocumentElement.AppendChild(nodeCreateDatabase);

                            DomUtil.SetAttr(nodeCreateDatabase, "name", this.textBox_issueDbName.Text);
                            DomUtil.SetAttr(nodeCreateDatabase, "type", "issue");
                            DomUtil.SetAttr(nodeCreateDatabase, "biblioDbName", this.textBox_biblioDbName.Text);
                            create_dbnames.Add(this.textBox_issueDbName.Text);
                        }
                        else
                        {
                            nRet = Global.CheckDbName(this.textBox_issueDbName.Text,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            DomUtil.SetAttr(nodeChangeDatabase, "issueDbName", this.textBox_issueDbName.Text);
                            bChanged = true;
                            change_dbnames.Add(strOldIssueDbName + " --> " + this.textBox_issueDbName.Text);
                        }
                    }


                    // ��ע����
                    string strOldCommentDbName = DomUtil.GetAttr(this.dom.DocumentElement,
                        "commentDbName");
                    if (this.textBox_commentDbName.Text == "")
                    {
                        if (String.IsNullOrEmpty(strOldCommentDbName) == false)
                        {
                            delete_dbnames.Add(strOldCommentDbName);
                        }
                    }
                    else if (strOldCommentDbName != this.textBox_commentDbName.Text)
                    {
                        if (String.IsNullOrEmpty(strOldCommentDbName) == true)
                        {
                            // ��ע�����ӿձ�Ϊ��ֵ����ʾҪ�����ڿ�
                            XmlNode nodeCreateDatabase = create_dom.CreateElement("database");
                            create_dom.DocumentElement.AppendChild(nodeCreateDatabase);

                            DomUtil.SetAttr(nodeCreateDatabase, "name", this.textBox_commentDbName.Text);
                            DomUtil.SetAttr(nodeCreateDatabase, "type", "comment");
                            DomUtil.SetAttr(nodeCreateDatabase, "biblioDbName", this.textBox_biblioDbName.Text);
                            create_dbnames.Add(this.textBox_commentDbName.Text);
                        }
                        else
                        {
                            nRet = Global.CheckDbName(this.textBox_commentDbName.Text,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            DomUtil.SetAttr(nodeChangeDatabase, "commentDbName", this.textBox_commentDbName.Text);
                            bChanged = true;
                            change_dbnames.Add(strOldCommentDbName + " --> " + this.textBox_commentDbName.Text);
                        }
                    }

                    // ��ʾ�޸ĵ����ݿ�����Ҫɾ�������ݿ⣬Ҫ���������ݿ�
                    string strText = "";
                    if (change_dbnames.Count > 0)
                    {
                        strText += "Ҫ�����������ݿ����޸�:\r\n---\r\n";
                        strText += MakeListString(change_dbnames);
                        strText += "\r\n";
                    }

                    if (delete_dbnames.Count > 0)
                    {
                        strText += "Ҫɾ���������ݿ�:\r\n---\r\n";
                        strText += MakeListString(delete_dbnames);
                        strText += "����: ���ݿⱻɾ�������е�������Ҳ�޷���ԭ��\r\n";
                        strText += "\r\n";
                    }

                    if (create_dbnames.Count > 0)
                    {
                        strText += "Ҫ�����������ݿ�:\r\n---\r\n";
                        strText += MakeListString(create_dbnames);
                        strText += "\r\n";
                    }

                    if (bInCirculationChanged == true)
                    {
                        strText += "\r\n��Ŀ�� '�Ƿ������ͨ' ״̬�������޸ģ���Ϊ:\r\n---\r\n";
                        strText += this.checkBox_inCirculation.Checked == true ? "Ҫ������ͨ" : "��������ͨ";
                        strText += "\r\n";
                    }

                    if (bRoleChanged == true)
                    {
                        strText += "\r\n��Ŀ�� '��ɫ' �������޸ģ���Ϊ:\r\n---\r\n";
                        strText += strRole;
                        strText += "\r\n";
                    }

                    if (bReplicationChanged == true)
                    {
                        strText += "\r\n��Ŀ�� '���Ʋ���' �������޸ģ���Ϊ:\r\n---\r\n";
                        strText += strReplication;
                        strText += "\r\n";
                    }

                    if (bChanged == false && string.IsNullOrEmpty(strText) == true)
                    {
                        Debug.Assert(string.IsNullOrEmpty(strText) == true, "");

#if DEBUG
                        XmlNodeList nodes = create_dom.DocumentElement.SelectNodes("database");
                        Debug.Assert(nodes.Count == 0, "");
#endif

                        // 2013/1/27
                        // Ҫ���ԣ������޸ĵ������(�����´������ݿ�������)��OK��ť���º󣬲�Ӧ�������˳� 
                        this.DialogResult = DialogResult.Cancel;
                        this.Close();
                        return;
                    }

                    // �Ի��򾯸�
                    DialogResult result = MessageBox.Show(this,
                        strText + "\r\nȷʵҪ����?",
                        "BiblioDatabaseDialog",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result != DialogResult.Yes)
                        return;

                    if (bChanged == true)
                    {
                        strDatabaseInfo = change_dom.OuterXml;

                        // �޸����ݿ�
                        nRet = this.ManagerForm.ChangeDatabase(
                            strOldBiblioDbName,
                            strDatabaseInfo,
                            out strOutputInfo,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }

                    // ɾ�����ݿ�
                    /*
                    XmlNodeList nodes = delete_dom.DocumentElement.SelectNodes("database");
                    if (nodes.Count > 0)
                    {
                        strDatabaseInfo = delete_dom.OuterXml;
                     * */
                    bool bConfirmed = false;
                    if (delete_dbnames.Count > 0)
                    {
                        // Ϊȷ����ݶ���¼
                        // return:
                        //      -1  ����
                        //      0   ������¼
                        //      1   ��¼�ɹ�
                        nRet = this.ManagerForm.ConfirmLogin(out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 0)
                        {
                            strError = "ˢ�����ݿ�Ĳ���������";
                            MessageBox.Show(this, strError);
                        }
                        else
                        {
                            bConfirmed = true;
                            // ɾ�����ݿ�
                            nRet = this.ManagerForm.DeleteDatabase(
                                MakeListString(delete_dbnames),
                                out strOutputInfo,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                        }
                    }

                    // �������ݿ�
                    {
                        XmlNodeList nodes = create_dom.DocumentElement.SelectNodes("database");
                        if (nodes.Count > 0)
                        {
                            if (bConfirmed == false)
                            {
                                // Ϊȷ����ݶ���¼
                                // return:
                                //      -1  ����
                                //      0   ������¼
                                //      1   ��¼�ɹ�
                                nRet = this.ManagerForm.ConfirmLogin(out strError);
                                if (nRet == -1)
                                    goto ERROR1;
                                if (nRet == 0)
                                {
                                    strError = "�������ݿ�Ĳ���������";
                                    MessageBox.Show(this, strError);
                                    goto END1;
                                }
                            }

                            strDatabaseInfo = create_dom.OuterXml;

                            // �������ݿ�
                            nRet = this.ManagerForm.CreateDatabase(
                                strDatabaseInfo,
                                false,
                                out strOutputInfo,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                        }
                    }
                }
                finally
                {
                    EnableControls(true);
                }

            }

        END1:
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

        // ��Ŀ����
        public string BiblioDatabaseName
        {
            get
            {
                return this.textBox_biblioDbName.Text;
            }
            set
            {
                this.textBox_biblioDbName.Text = value;
            }
        }

        public bool InCirculation
        {
            get
            {
                return this.checkBox_inCirculation.Checked;
            }
            set
            {
                this.checkBox_inCirculation.Checked = value;
            }
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public void EnableControls(bool bEnable)
        {
            this.textBox_biblioDbName.Enabled = bEnable;
            this.textBox_entityDbName.Enabled = bEnable;
            this.textBox_issueDbName.Enabled = bEnable;
            this.textBox_orderDbName.Enabled = bEnable;
            this.textBox_commentDbName.Enabled = bEnable;

            this.checkBox_inCirculation.Enabled = bEnable;

            if (this.CreateMode == true)
            {
                this.comboBox_syntax.Enabled = bEnable;
                this.comboBox_documentType.Enabled = bEnable;
            }
            else
            {
                this.comboBox_syntax.Enabled = false;
                this.comboBox_documentType.Enabled = false;
            }

            this.checkedComboBox_role.Enabled = bEnable;

            this.comboBox_replication_centerServer.Enabled = bEnable;
            this.comboBox_replication_dbName.Enabled = bEnable;

            this.button_OK.Enabled = bEnable;
            this.button_Cancel.Enabled = bEnable;
        }

        private void checkedComboBox_role_DropDown(object sender, EventArgs e)
        {
            if (this.checkedComboBox_role.Items.Count != 0)
                return;

            this.checkedComboBox_role.Items.Add("orderWork\t�ɹ�������");
            this.checkedComboBox_role.Items.Add("orderRecommendStore\t�����洢��");
            this.checkedComboBox_role.Items.Add("biblioSource\t��Դ��Ŀ��");
            this.checkedComboBox_role.Items.Add("catalogWork\t��Ŀ������");
            this.checkedComboBox_role.Items.Add("catalogTarget\t��Ŀ�����");
        }

        private void comboBox_replication_centerServer_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_replication_centerServer.Items.Count > 0)
                return;

            List<string> server_names = this.ManagerForm.GetCenterServerNames();
            foreach (string s in server_names)
            {
                this.comboBox_replication_centerServer.Items.Add(s);
            }

        }

        int m_nInDropDown = 0;

        private void comboBox_replication_dbName_DropDown(object sender, EventArgs e)
        {
            if (m_nInDropDown > 0 || this.comboBox_replication_dbName.Items.Count > 0)
                return;

            string strError = "";
            m_nInDropDown++;
            try
            {
                if (string.IsNullOrEmpty(this.comboBox_replication_centerServer.Text) == true)
                {
                    strError = "����ѡ�����ķ�����";
                    goto ERROR1;
                }

                List<string> dbnames = null;
                int nRet = this.ManagerForm.GetRemoteBiblioDbNames(
                    this.comboBox_replication_centerServer.Text,
                    out dbnames,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                foreach (string s in dbnames)
                {
                    this.comboBox_replication_dbName.Items.Add(s);
                }
            }
            finally
            {
                m_nInDropDown--;
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
    }
}