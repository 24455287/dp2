using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using DigitalPlatform.Xml;
using DigitalPlatform.Text;

namespace dp2Circulation
{
    internal partial class ReaderDatabaseDialog : Form
    {
        int m_nInInitial = 0;

        /// <summary>
        /// ϵͳ����
        /// </summary>
        public ManagerForm ManagerForm = null;

        public bool CreateMode = false; // �Ƿ�Ϊ����ģʽ��==trueΪ����ģʽ��==falseΪ�޸�ģʽ
        public bool Recreate = false;   // �Ƿ�Ϊ���´���ģʽ����CreateMode == true ʱ������
        XmlDocument dom = null;

        /// <summary>
        /// ͼ��ݴ����б��ַ������ṩ�� combobox ʹ��
        /// </summary>
        public string LibraryCodeList
        {
            get
            {
                StringBuilder text = new StringBuilder();
                foreach (string s in this.comboBox_libraryCode.Items)
                {
                    if (text.Length > 0)
                        text.Append(",");
                    text.Append(s);
                }
                return text.ToString();
            }
            set
            {
                List<string> values = StringUtil.SplitList(value);
                this.comboBox_libraryCode.Items.Clear();
                foreach (string s in values)
                {
                    this.comboBox_libraryCode.Items.Add(s);
                }
            }
        }


        public ReaderDatabaseDialog()
        {
            InitializeComponent();
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
            if (strType != "reader")
            {
                strError = "<database>Ԫ�ص�type����ֵ('" + strType + "')Ӧ��Ϊreader";
                return -1;
            }

            this.m_nInInitial++;    // ����xxxchanged��Ӧ

            try
            {

                this.textBox_readerDbName.Text = DomUtil.GetAttr(dom.DocumentElement,
                    "name");

                string strInCirculation = DomUtil.GetAttr(dom.DocumentElement,
                    "inCirculation");
                if (String.IsNullOrEmpty(strInCirculation) == true)
                    strInCirculation = "true";

                this.comboBox_libraryCode.Text = DomUtil.GetAttr(dom.DocumentElement,
                    "libraryCode");

                this.checkBox_inCirculation.Checked = DomUtil.IsBooleanTrue(strInCirculation);

            }
            finally
            {
                this.m_nInInitial--;
            }

            return 0;
        }


        private void ReaderDatabaseDialog_Load(object sender, EventArgs e)
        {
            // ���ֻ��һ���б��������ǰΪ�հף����Զ����ú���һ��
            if (this.CreateMode == true
                && string.IsNullOrEmpty(this.comboBox_libraryCode.Text) == true
                && this.comboBox_libraryCode.Items.Count > 0)
                this.comboBox_libraryCode.Text = (string)this.comboBox_libraryCode.Items[0];
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

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
                    DomUtil.SetAttr(nodeDatabase, "type", "reader");

                    // �Ƿ������ͨ��
                    string strInCirculation = "true";
                    if (this.checkBox_inCirculation.Checked == true)
                        strInCirculation = "true";
                    else
                        strInCirculation = "false";

                    DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);

                    // ��Ŀ����
                    if (this.textBox_readerDbName.Text == "")
                    {
                        strError = "��δָ�����߿���";
                        goto ERROR1;
                    }
                    nRet = Global.CheckDbName(this.textBox_readerDbName.Text,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    DomUtil.SetAttr(nodeDatabase, "name",
                        this.textBox_readerDbName.Text);

                    // 2012/9/7
                    DomUtil.SetAttr(nodeDatabase, "libraryCode",
    this.comboBox_libraryCode.Text);


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

                    // �޸ĵ����ݿ���
                    List<string> change_dbnames = new List<string>();

                    // �����޸������DOM
                    XmlDocument change_dom = new XmlDocument();
                    change_dom.LoadXml("<root />");
                    XmlNode nodeChangeDatabase = change_dom.CreateElement("database");
                    change_dom.DocumentElement.AppendChild(nodeChangeDatabase);

                    // type
                    DomUtil.SetAttr(nodeChangeDatabase, "type", "reader");


                    // ��Ŀ����
                    string strOldReaderDbName = DomUtil.GetAttr(this.dom.DocumentElement,
                        "name");

                    if (String.IsNullOrEmpty(strOldReaderDbName) == false
                        && this.textBox_readerDbName.Text == "")
                    {
                        strError = "���߿��������޸�Ϊ��";
                        goto ERROR1;
                    }

                    bool bChanged = false;  // �Ƿ���ʵ�����޸�����

                    if (strOldReaderDbName != this.textBox_readerDbName.Text)
                    {
                        nRet = Global.CheckDbName(this.textBox_readerDbName.Text,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        DomUtil.SetAttr(nodeChangeDatabase, "name", this.textBox_readerDbName.Text);
                        bChanged = true;
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
                        DomUtil.SetAttr(nodeChangeDatabase, "name", this.textBox_readerDbName.Text);
                        DomUtil.SetAttr(nodeChangeDatabase, "inCirculation",
                            this.checkBox_inCirculation.Checked == true ? "true" : "false");
                        bInCirculationChanged = true;
                    }

                    bool bLibraryCodeChanged = false;

                    // �Ƿ������ͨ
                    string strOldLibraryCode = DomUtil.GetAttr(this.dom.DocumentElement,
                        "libraryCode");
                    if (strOldLibraryCode != this.comboBox_libraryCode.Text)
                    {
                        DomUtil.SetAttr(nodeChangeDatabase, "name", this.textBox_readerDbName.Text);
                        DomUtil.SetAttr(nodeChangeDatabase, "libraryCode",
                            this.comboBox_libraryCode.Text);
                        bLibraryCodeChanged = true;
                    }

                    if (bChanged == false && bInCirculationChanged == false && bLibraryCodeChanged == false)
                        goto END1;

                    // ��ʾ�޸ĵ����ݿ�����Ҫɾ�������ݿ⣬Ҫ���������ݿ�
                    string strText = "";

                    if (bChanged == true)
                        strText += "Ҫ�����ݿ��� '" + strOldReaderDbName + "' �޸�Ϊ '" + this.textBox_readerDbName.Text + "'";

                    if (bInCirculationChanged == true)
                    {
                        if (strText != "")
                            strText += "����";
                        else
                            strText += "Ҫ";
                        strText += "�� '�Ƿ������ͨ' ״̬ �޸�Ϊ"
                            + (this.checkBox_inCirculation.Checked == true ? "'Ҫ����'" : "'������'");
                    }

                    if (bLibraryCodeChanged == true)
                    {
                        if (strText != "")
                            strText += "����";
                        else
                            strText += "Ҫ";
                        strText += "�� ͼ��ݴ��� �޸�Ϊ '"
                            + this.comboBox_libraryCode.Text + "'";
                    }
                        
                    strText += "��\r\n\r\nȷʵҪ����?";

                    // �Ի��򾯸�
                    DialogResult result = MessageBox.Show(this,
                        strText,
                        "ReaderDatabaseDialog",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result != DialogResult.Yes)
                        return;

                    string strDatabaseInfo = "";
                    string strOutputInfo = "";

                    strDatabaseInfo = change_dom.OuterXml;

                    // �޸����ݿ�
                    nRet = this.ManagerForm.ChangeDatabase(
                        strOldReaderDbName,
                        strDatabaseInfo,
                        out strOutputInfo,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

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

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public void EnableControls(bool bEnable)
        {
            this.textBox_readerDbName.Enabled = bEnable;
            this.comboBox_libraryCode.Enabled = bEnable;
            this.checkBox_inCirculation.Enabled = bEnable;

            this.button_OK.Enabled = bEnable;
            this.button_Cancel.Enabled = bEnable;
        }

        public string ReaderDatabaseName
        {
            get
            {
                return this.textBox_readerDbName.Text;
            }
            set
            {
                this.textBox_readerDbName.Text = value;
            }
        }

        public string LibraryCode
        {
            get
            {
                return this.comboBox_libraryCode.Text;
            }
            set
            {
                this.comboBox_libraryCode.Text = value;
            }
        }
    }
}