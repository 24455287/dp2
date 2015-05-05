using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;

namespace dp2Circulation
{
    internal partial class GetOpacMemberDatabaseNameDialog : Form
    {
        // ��ʾ��ǰȫ�����ݿ���Ϣ��XML�ַ���
        public string AllDatabaseInfoXml = "";

        /// <summary>
        /// ϵͳ����
        /// </summary>
        public ManagerForm ManagerForm = null;

        // ��Ҫ�ų����������ݿ���
        public List<string> ExcludingDbNames = null;

        public GetOpacMemberDatabaseNameDialog()
        {
            InitializeComponent();

            this.listView_databases.LargeImageList = this.imageList_databaseType;
            this.listView_databases.SmallImageList = this.imageList_databaseType;
        }

        private void GetOpacMemberDatabaseNameDialog_Load(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = ListAllDatabases(this.AllDatabaseInfoXml,
                out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);

            // ѡ�е�һ��
            if (this.listView_databases.Items.Count > 0)
            {
                if (this.SelectedDatabaseName != "")
                {
                    ListViewItem item = ListViewUtil.FindItem(this.listView_databases,
                        this.SelectedDatabaseName,
                        0);
                    if (item != null)
                        item.Selected = true;
                }
                else
                {
                    // ѡ�е�һ�����ǻ�ɫ��item
                    for (int i = 0; i < this.listView_databases.Items.Count; i++)
                    {
                        ListViewItem item = this.listView_databases.Items[i];
                        if (item.ImageIndex == 0)
                        {
                            item.Selected = true;
                            break;
                        }
                    }
                }
            }
        }

        // ��listview���г��������ݿ�
        int ListAllDatabases(string strAllDatbaseInfo,
            out string strError)
        {
            strError = "";

            this.listView_databases.Items.Clear();

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
                string strType = DomUtil.GetAttr(node, "type");



                string strTypeName = "";
                if (this.ManagerForm != null)
                {
                    strTypeName = this.ManagerForm.GetTypeName(strType);
                    if (strTypeName == null)
                        strTypeName = strType;
                }
                else
                    strTypeName = strType;

                ListViewItem item = new ListViewItem(strName, 0);
                item.SubItems.Add(strTypeName);
                item.Tag = node.OuterXml;   // ����XML����Ƭ��

                this.listView_databases.Items.Add(item);

                if (this.ExcludingDbNames != null
    && this.ExcludingDbNames.Count > 0)
                {
                    // ���ΪҪ�ų������ݿ���������ɫ����
                    if (this.ExcludingDbNames.IndexOf(strName) != -1)
                    {
                        item.ForeColor = SystemColors.GrayText; // ��ɫ
                        item.ImageIndex = 1;    // ��ʾҪ�ų�
                    }
                }
            }

            return 0;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            if (this.textBox_databaseName.Text == "")
            {
                MessageBox.Show(this, "��δָ�����ݿ���");
                return;
            }


            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void listView_databases_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView_databases.SelectedItems.Count > 0)
            {
                List<string> dbnames = new List<string>();
                foreach (ListViewItem item in this.listView_databases.SelectedItems)
                {
                    if (item.ImageIndex == 0)
                        dbnames.Add(item.Text);
                }
                this.textBox_databaseName.Text = StringUtil.MakePathList(dbnames);
            }
            else
                this.textBox_databaseName.Text = "";
        }

        public string SelectedDatabaseName
        {
            get
            {
                return this.textBox_databaseName.Text;
            }
            set
            {
                this.textBox_databaseName.Text = value;
            }
        }

        private void listView_databases_DoubleClick(object sender, EventArgs e)
        {
            button_OK_Click(this, null);
        }
    }
}