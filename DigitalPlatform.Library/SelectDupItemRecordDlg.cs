using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DigitalPlatform.Library
{
    /// <summary>
    /// �Ի��򣺴������ظ��Ĳ��¼��ѡ��һ��
    /// </summary>
    public partial class SelectDupItemRecordDlg : Form
    {
        /// <summary>
        /// ·���ļ���
        /// </summary>
        public List<DoublePath> Paths = null;

        /// <summary>
        /// ѡ���·��(˫·��)
        /// </summary>
        public DoublePath SelectedDoublePath = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        public SelectDupItemRecordDlg()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ��Ϣ����
        /// </summary>
        public string MessageText
        {
            get
            {
                return this.label_message.Text;
            }
            set
            {
                this.label_message.Text = value;
            }
        }

        private void SelectDupItemRecord_Load(object sender, EventArgs e)
        {

            this.FillList();

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            if (this.listView_paths.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ���κ�����");
                return;
            }

            this.SelectedDoublePath = this.Paths[this.listView_paths.SelectedIndices[0]];

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        void FillList()
        {
            this.listView_paths.Items.Clear();

            if (this.Paths == null)
                return;

            for (int i = 0; i < this.Paths.Count; i++)
            {
                DoublePath dpath = this.Paths[i];

                ListViewItem item = new ListViewItem(dpath.ItemRecPath);

                item.SubItems.Add(dpath.BiblioRecPath);

                this.listView_paths.Items.Add(item);
            }
        }

        private void listView_paths_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView_paths.SelectedItems.Count > 0)
                this.button_OK.Enabled = true;
            else
                this.button_OK.Enabled = false;
        }

        private void listView_paths_DoubleClick(object sender, EventArgs e)
        {
            this.button_OK_Click(null, null);
        }
    }


    /// <summary>
    /// ר���ڴ洢�֡�����ؼ�¼·����˫·���ṹ
    /// </summary>
    public class DoublePath
    {
        /// <summary>
        /// ��Ŀ��·��
        /// </summary>
        public string BiblioRecPath = "";

        /// <summary>
        /// ʵ���·��
        /// </summary>
        public string ItemRecPath = "";
    }
}