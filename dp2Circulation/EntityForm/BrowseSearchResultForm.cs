using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DigitalPlatform;
using DigitalPlatform.GUI;

namespace dp2Circulation
{
    /// <summary>
    /// �������ж�����¼ʱ�����ѡ��Ի���
    /// </summary>
    internal partial class BrowseSearchResultForm : Form
    {
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        public Stop stop = null;
        /// <summary>
        /// ����ϸ��
        /// </summary>
        public event OpenDetailEventHandler OpenDetail = null;

        /// <summary>
        /// ��ʾ��¼��ListView��
        /// </summary>
        public ListView RecordsList
        {
            get
            {
                return this.listView_records;
            }
        }


        public BrowseSearchResultForm()
        {
            InitializeComponent();

            ListViewProperty prop = new ListViewProperty();
            this.listView_records.Tag = prop;
            // ��һ�����⣬��¼·��
            prop.SetSortStyle(0, ColumnSortStyle.RecPath);
            prop.GetColumnTitles -= new GetColumnTitlesEventHandler(prop_GetColumnTitles);
            prop.GetColumnTitles += new GetColumnTitlesEventHandler(prop_GetColumnTitles);
        }

        void prop_GetColumnTitles(object sender, GetColumnTitlesEventArgs e)
        {
            if (e.DbName == "<blank>")
            {
                e.ColumnTitles = new ColumnPropertyCollection();
                e.ColumnTitles.Add("������");
                e.ColumnTitles.Add("����");
                return;
            }

            e.ColumnTitles = this.MainForm.GetBrowseColumnProperties(e.DbName);
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            this.button_OK.Enabled = false;
            if (this.listView_records.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ������");
                this.button_OK.Enabled = true;
                return;
            }

            OnLoadDetail();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {

            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ȷ���б��������㹻
        void EnsureColumns(int nCount)
        {
            if (this.listView_records.Columns.Count >= nCount)
                return;

            for (int i = this.listView_records.Columns.Count; i < nCount; i++)
            {
                string strText = "";
                if (i == 0)
                {
                    strText = "��¼·��";
                }
                else
                {
                    strText = Convert.ToString(i);
                }

                ColumnHeader col = new ColumnHeader();
                col.Text = strText;
                col.Width = 200;
                this.listView_records.Columns.Add(col);
            }

        }


        /// <summary>
        /// ��listview���׷��һ��
        /// </summary>
        /// <param name="strID">ID</param>
        /// <param name="others">����������</param>
        public void NewLine(string strID,
            string[] others)
        {
            EnsureColumns(others.Length + 1);

            ListViewItem item = new ListViewItem(strID, 0);

            this.listView_records.Items.Add(item);

            for (int i = 0; i < others.Length; i++)
            {
                item.SubItems.Add(others[i]);
            }
        }

        /*
        private void listView_records_DoubleClick(object sender, EventArgs e)
        {
            OnLoadDetail();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
         */

        /// <summary>
        /// װ���һ����¼����ϸ��
        /// </summary>
        /// <param name="bCloseWindow">�Ƿ�˳��رձ�����</param>
        public void LoadFirstDetail(bool bCloseWindow)
        {
            if (this.listView_records.Items.Count == 0)
                return;

            string[] paths = new string[1];
            paths[0] = this.listView_records.Items[0].Text;

            OpenDetailEventArgs args = new OpenDetailEventArgs();
            args.Paths = paths;
            args.OpenNew = false;

            this.listView_records.Enabled = false;
            this.OpenDetail(this, args);
            this.listView_records.Enabled = true;

            if (bCloseWindow == true)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        void OnLoadDetail()
        {
            if (this.OpenDetail == null)
                return;

            if (this.listView_records.SelectedItems.Count == 0)
                return;

            if (this.stop != null)
                stop.DoStop();

            string[] paths = new string[this.listView_records.SelectedItems.Count];
            int i = 0;
            foreach (ListViewItem item in this.listView_records.SelectedItems)
            {
                paths[i++] = item.Text;
            }

            OpenDetailEventArgs args = new OpenDetailEventArgs();
            args.Paths = paths;
            args.OpenNew = true;

            this.listView_records.Enabled = false;
            this.OpenDetail(this, args);
            this.listView_records.Enabled = true;
        }


        private void listView_records_DoubleClick(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();

            OnLoadDetail();

        }

        private void listView_records_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView_records.SelectedItems.Count > 0)
                this.button_OK.Enabled = true;
            else
                this.button_OK.Enabled = false;

            ListViewUtil.OnSeletedIndexChanged(this.listView_records,
                0,
                null);

        }

        private void BrowseSearchResultForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.stop != null)
                this.stop.DoStop();
        }

        private void listView_records_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewUtil.OnColumnClick(this.listView_records, e);
        }

    }

    /// <summary>
    /// ����ϸ���¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void OpenDetailEventHandler(object sender,
    OpenDetailEventArgs e);

    /// <summary>
    /// ����ϸ���¼��Ĳ���
    /// </summary>
    public class OpenDetailEventArgs : EventArgs
    {
        /// <summary>
        /// ��¼ȫ·�����ϡ�
        /// </summary>
        public string[] Paths = null;

        /// <summary>
        /// �Ƿ�Ϊ�´���
        /// </summary>
        public bool OpenNew = false;
    }

}