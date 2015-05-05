using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

// 2013/3/16 ��� XML ע��

namespace dp2Circulation
{
    /// <summary>
    /// ���ڻ��һ��Ŀ����Ŀ�����ĶԻ���
    /// </summary>
    internal partial class GetAcceptTargetDbNameDlg : Form
    {
        /// <summary>
        /// ���Ի����Ƿ�Ҫ�Զ�����?
        /// ������һ����ʵ�����������ֵ���this.DbNameʱ���Զ������Ի���
        /// </summary>
        public bool AutoFinish = false;

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// �Ƿ�Ϊ�ڿ�ģʽ? 
        /// ���Ϊtrue��ֻ�г��������ڿ����Ŀ����������ֻ�г�û�������ڿ����Ŀ���� 
        /// </summary>
        public bool SeriesMode = false; // 2008/12/29 new add

        /// <summary>
        /// MARC �����ʽ��"unimarc"��"usmarc"֮һ
        /// ���Ϊ�գ���ʾ�Ը�ʽ��Ҫ�������Ϊ�գ���Ҫ��Ϊ�ø�ʽ
        /// </summary>
        public string MarcSyntax = "";  // 

        /// <summary>
        /// �û����ѡ�������ݿ���
        /// </summary>
        public string DbName = "";

        /// <summary>
        /// ���캯��
        /// </summary>
        public GetAcceptTargetDbNameDlg()
        {
            InitializeComponent();
        }

        private void GetAcceptTargetDbNameDlg_Load(object sender, EventArgs e)
        {
            FillDbNameList();

            if (this.AutoFinish == true)
            {
                if (this.listView_dbnames.SelectedItems.Count == 1
                    && this.listView_dbnames.Items.Count == 1)
                {
                    button_OK_Click(this, null);
                }
            }
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            if (this.listView_dbnames.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ŀ�����ݿ���");
                return;
            }

            this.DbName = this.listView_dbnames.SelectedItems[0].Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        void FillDbNameList()
        {
            this.listView_dbnames.Items.Clear();

            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty prop = this.MainForm.BiblioDbProperties[i];

                    if (String.IsNullOrEmpty(prop.ItemDbName) == true)
                        continue;

                    if (String.IsNullOrEmpty(this.MarcSyntax) == false)
                    {
                        if (prop.Syntax.ToLower() != this.MarcSyntax.ToLower())
                            continue;
                    }

                    // 2008/12/29 new add
                    if (this.SeriesMode == true)
                    {
                        if (String.IsNullOrEmpty(prop.IssueDbName) == true)
                            continue;
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(prop.IssueDbName) == false)
                            continue;
                    }

                    string strDbName = prop.DbName;

                    ListViewItem item = new ListViewItem();
                    item.Text = strDbName;

                    this.listView_dbnames.Items.Add(item);

                    if (item.Text == this.DbName)
                        item.Selected = true;
                }
            }
        }
    }
}