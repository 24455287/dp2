using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Xml;
using DigitalPlatform.Xml;
using DigitalPlatform.rms.Client;

namespace DigitalPlatform.Library
{
    /// <summary>
    /// ��ʾ��ѡ�������ŵ����ݿ�
    /// һ����Ŀ���һ��ʵ��⣬������������ͬ�������֡�����Ϣ
    /// </summary>
    public partial class GetLinkDbDlg : Form
    {
        /// <summary>
        /// 
        /// </summary>
        public SearchPanel SearchPanel = null;

        string m_strItemDbName = "";
        string m_strBiblioDbName = "";

        XmlDocument dom = null; // global�����ļ�dom

        /// <summary>
        /// ���캯��
        /// </summary>
        public GetLinkDbDlg()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ������URL
        /// </summary>
        public string ServerUrl
        {
            get
            {
                return this.textBox_serverUrl.Text;
            }
            set
            {
                this.textBox_serverUrl.Text = value;
                dom = null; // �������������ļ�dom
            }
        }

        /// <summary>
        /// ��Ŀ����
        /// </summary>
        public string BiblioDbName
        {
            get
            {
                return this.m_strBiblioDbName;
            }
            set
            {
                this.m_strBiblioDbName = value;
            }
        }

        /// <summary>
        /// ʵ�����
        /// </summary>
        public string ItemDbName
        {
            get
            {
                return this.m_strItemDbName;
            }
            set
            {
                this.m_strItemDbName = value;
            }
        }

        private void GetLinkDbDlg_Load(object sender, EventArgs e)
        {

            if (this.textBox_serverUrl.Text != "")
            {
                string strError = "";
                int nRet = this.GetGlobalCfgFile(out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return;
                }

                FillList();
            }

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            if (this.listView_dbs.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ����Ŀ���ʵ���");
                return;
            }

            this.BiblioDbName = this.listView_dbs.SelectedItems[0].Text;
            this.ItemDbName = this.listView_dbs.SelectedItems[0].SubItems[1].Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void button_findServer_Click(object sender, EventArgs e)
        {
            // ѡ��Ŀ�������
            OpenResDlg dlg = new OpenResDlg();

            dlg.Text = "��ѡ�������";
            dlg.EnabledIndices = new int[] { ResTree.RESTYPE_SERVER };
            dlg.ap = this.SearchPanel.ap;
            dlg.ApCfgTitle = "getlinkdbdlg_findserver";
            dlg.Path = this.textBox_serverUrl.Text;
            dlg.Initial(this.SearchPanel.Servers,
                this.SearchPanel.Channels);
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.ServerUrl = dlg.Path;

            //

            string strError = "";
            int nRet = this.GetGlobalCfgFile(out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }

            FillList();

        }

        // ���cfgs/global�����ļ�
        int GetGlobalCfgFile(out string strError)
        {
            strError = "";

            if (this.dom != null)
                return 0;	// �Ż�

            if (this.textBox_serverUrl.Text == "")
            {
                strError = "��δָ��������URL";
                return -1;
            }

            string strCfgFilePath = "cfgs/global";
            XmlDocument tempdom = null;
            // ��������ļ�
            // return:
            //		-1	error
            //		0	not found
            //		1	found
            int nRet = this.SearchPanel.GetCfgFile(
                this.textBox_serverUrl.Text,
                strCfgFilePath,
                out tempdom,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "�����ļ� '" + strCfgFilePath + "' û���ҵ�...";
                return -1;
            }

            this.dom = tempdom;

            return 0;
        }

        void FillList()
        {
            this.listView_dbs.Items.Clear();

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//dblink");

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strBiblioDbName = DomUtil.GetAttr(node, "bibliodb");
                string strItemDbName = DomUtil.GetAttr(node, "itemdb");
                string strComment = DomUtil.GetAttr(node, "comment");

                ListViewItem item = new ListViewItem(strBiblioDbName);
                item.SubItems.Add(strItemDbName);
                item.SubItems.Add(strComment);

                this.listView_dbs.Items.Add(item);

                if (this.BiblioDbName == strBiblioDbName)
                    item.Selected = true;
            }
        }

        private void listView_dbs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView_dbs.SelectedItems.Count == 0)
                this.button_OK.Enabled = false;
            else
                this.button_OK.Enabled = true;
        }

        private void listView_dbs_DoubleClick(object sender, EventArgs e)
        {
            button_OK_Click(null, null);
        }

    }
}