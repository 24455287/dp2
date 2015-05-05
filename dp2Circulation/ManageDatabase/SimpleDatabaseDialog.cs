using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using DigitalPlatform.Xml;

namespace dp2Circulation
{
    internal partial class SimpleDatabaseDialog : Form
    {
        public string DatabaseType = "";

        int m_nInInitial = 0;
        /// <summary>
        /// ϵͳ����
        /// </summary>
        public ManagerForm ManagerForm = null;

        public bool CreateMode = false; // �Ƿ�Ϊ����ģʽ��==trueΪ����ģʽ��==falseΪ�޸�ģʽ
        public bool Recreate = false;   // �Ƿ�Ϊ���´���ģʽ����CreateMode == true ʱ������

        XmlDocument dom = null;

        public SimpleDatabaseDialog()
        {
            InitializeComponent();
        }

        public int Initial(
            string strDatabaseType,
            string strXml,
out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strDatabaseType) == true)
            {
                strError = "strDatabaseType����ֵ����Ϊ��";
                return -1;
            }

            this.DatabaseType = strDatabaseType;

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
            if (strType != this.DatabaseType)
            {
                strError = "<database>Ԫ�ص�type����ֵ('" + strType + "')Ӧ��Ϊ '"+this.DatabaseType+"'";
                return -1;
            }

            this.m_nInInitial++;    // ����xxxchanged��Ӧ

            try
            {

                this.textBox_dbName.Text = DomUtil.GetAttr(dom.DocumentElement,
                    "name");
            }
            finally
            {
                this.m_nInInitial--;
            }

            return 0;
        }

        private void SimpleDatabaseDialog_Load(object sender, EventArgs e)
        {
            string strError = "";
            if (this.CreateMode == true)
            {
                if (String.IsNullOrEmpty(this.DatabaseType) == true)
                {
                    strError = "��δָ��DatabaseType����";
                    goto ERROR1;
                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
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
                    DomUtil.SetAttr(nodeDatabase, "type", this.DatabaseType);

                    // ����
                    if (this.textBox_dbName.Text == "")
                    {
                        strError = "��δָ������";
                        goto ERROR1;
                    }
                    nRet = Global.CheckDbName(this.textBox_dbName.Text,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    DomUtil.SetAttr(nodeDatabase, "name",
                        this.textBox_dbName.Text);

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
                    string strDatabaseInfo = "";
                    string strOutputInfo = "";

                    // �޸ĵ����ݿ���
                    List<string> change_dbnames = new List<string>();

                    // �����޸������DOM
                    XmlDocument change_dom = new XmlDocument();
                    change_dom.LoadXml("<root />");
                    XmlNode nodeChangeDatabase = change_dom.CreateElement("database");
                    change_dom.DocumentElement.AppendChild(nodeChangeDatabase);

                    // type
                    DomUtil.SetAttr(nodeChangeDatabase, "type", this.DatabaseType);


                    // ����
                    string strOldReaderDbName = DomUtil.GetAttr(this.dom.DocumentElement,
                        "name");

                    if (String.IsNullOrEmpty(strOldReaderDbName) == false
                        && this.textBox_dbName.Text == "")
                    {
                        strError = "���������޸�Ϊ��";
                        goto ERROR1;
                    }

                    bool bChanged = false;  // �Ƿ���ʵ�����޸�����

                    if (strOldReaderDbName != this.textBox_dbName.Text)
                    {
                        nRet = Global.CheckDbName(this.textBox_dbName.Text,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        DomUtil.SetAttr(nodeChangeDatabase, "name", this.textBox_dbName.Text);
                        bChanged = true;
                    }

                    // ��ʾ�޸ĵ����ݿ�����Ҫɾ�������ݿ⣬Ҫ���������ݿ�
                    string strText = "Ҫ�����ݿ��� " + strOldReaderDbName + " �޸�Ϊ " + this.textBox_dbName.Text + ", ȷʵҪ����?";

                    // �Ի��򾯸�
                    DialogResult result = MessageBox.Show(this,
                        strText,
                        "SimpleDatabaseDialog",
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
                            strOldReaderDbName,
                            strDatabaseInfo,
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
            this.textBox_dbName.Enabled = bEnable;

            this.button_OK.Enabled = bEnable;
            this.button_Cancel.Enabled = bEnable;
        }

        public string DatabaseName
        {
            get
            {
                return this.textBox_dbName.Text;
            }
            set
            {
                this.textBox_dbName.Text = value;
            }
        }

        public bool DatabaseNameReadOnly
        {
            get
            {
                return this.textBox_dbName.ReadOnly;
            }
            set
            {
                this.textBox_dbName.ReadOnly = value;
            }
        }

        public string Comment
        {
            get
            {
                return this.textBox_comment.Text;
            }
            set
            {
                this.textBox_comment.Text = value;
                if (String.IsNullOrEmpty(value) == false)
                    this.textBox_comment.Visible = true;
                else
                    this.textBox_comment.Visible = false;
            }
        }
    }
}