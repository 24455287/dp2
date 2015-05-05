using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.Script;
using DigitalPlatform.Xml;

namespace dp2Circulation
{
    /// <summary>
    /// �����������
    /// </summary>
    internal partial class OrderOutputItemDialog : Form
    {
        const int WM_FINDOUTPUTFORMAT = API.WM_USER + 201;

        public ScriptManager ScriptManager = null;
        public ApplicationInfo AppInfo = null;

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        // �Ѿ��ù�������(����)����
        // ����������б��ų�combobox_seller�еĲ�������
        public List<string> ExcludeSellers = new List<string>();

        public OrderOutputItemDialog()
        {
            InitializeComponent();
        }

        private void OrderOutputItemDialog_Load(object sender, EventArgs e)
        {

        }

        private void OrderOutputItemDialog_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void OrderOutputItemDialog_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";
            if (this.comboBox_seller.Text == "")
            {
                strError = "��δָ��������";
                goto ERROR1;
            }

            // �����ʽ����Ϊ�գ���ʾȱʡ�ĸ�ʽ

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

        private void button_findOutputFormat_Click(object sender, EventArgs e)
        {
            // ���ֶԻ���ѯ��Project����
            GetProjectNameDlg dlg = new GetProjectNameDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.Text = "��ָ�� ������� ������";
            dlg.scriptManager = this.ScriptManager;
            dlg.ProjectName = this.comboBox_outputFormat.Text;
            dlg.NoneProject = false;
            dlg.DisableNoneProject = true;

            this.AppInfo.LinkFormState(dlg, "OrderOutputItemDialog_GetProjectNameDlg_state");
            dlg.ShowDialog(this);
            this.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.comboBox_outputFormat.Text = dlg.ProjectName;
        }

        public string Seller
        {
            get
            {
                return this.comboBox_seller.Text;
            }
            set
            {
                this.comboBox_seller.Text = value;
            }
        }

        public string OutputFormat
        {
            get
            {
                return this.comboBox_outputFormat.Text;
            }
            set
            {
                this.comboBox_outputFormat.Text = value;
            }
        }

        int m_nInDropDown = 0;

        private void comboBox_seller_DropDown(object sender, EventArgs e)
        {
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {

                ComboBox combobox = (ComboBox)sender;

                if (combobox.Items.Count == 0
                    && this.GetValueTable != null)
                {
                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    e1.DbName = "";

                    if (combobox == this.comboBox_seller)
                        e1.TableName = "orderSeller";
                    else
                    {
                        Debug.Assert(false, "��֧�ֵ�sender");
                        return;
                    }

                    this.GetValueTable(this, e1);

                    if (e1.values != null)
                    {
                        for (int i = 0; i < e1.values.Length; i++)
                        {
                            string strValue = e1.values[i];

                            // ֻ����ExcludeSeller�����ֵ
                            if (this.ExcludeSellers.IndexOf(strValue) == -1)
                                combobox.Items.Add(e1.values[i]);
                        }
                    }
                    else
                    {
                        combobox.Items.Add("<not found>");
                    }
                }
            }
            finally
            {
                this.Cursor = oldCursor;
                this.m_nInDropDown--;
            }

        }

        private void comboBox_outputFormat_DropDownClosed(object sender, EventArgs e)
        {
        }

        private void comboBox_outputFormat_TextChanged(object sender, EventArgs e)
        {
            if (this.comboBox_outputFormat.Text == "<ѡ��һ�����Ƹ�ʽ...>")
            {
                API.PostMessage(this.Handle, WM_FINDOUTPUTFORMAT, 0, 0);
            }
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_FINDOUTPUTFORMAT:
                    this.comboBox_outputFormat.Text = "";
                    this.button_findOutputFormat_Click(this, null);
                    return;
            }
            base.DefWndProc(ref m);
        }
    }
}