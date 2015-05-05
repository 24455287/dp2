using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.Text;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// �����޸Ĳ� �������� �Ի���
    /// �� QuickChangeEntityForm ��ʹ��
    /// </summary>
    internal partial class ChangeEntityActionDialog : Form
    {
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        public string RefDbName = "";

        public ChangeEntityActionDialog()
        {
            InitializeComponent();
        }

        private void ChangeParamDlg_Load(object sender, EventArgs e)
        {
            // ��伸��combobox

            // װ��ֵ
            this.comboBox_state.Text = this.MainForm.AppInfo.GetString(
                "change_param",
                "state",
                "<���ı�>");
            this.checkedComboBox_stateAdd.Text = this.MainForm.AppInfo.GetString(
    "change_param",
    "state_add",
    "");
            this.checkedComboBox_stateRemove.Text = this.MainForm.AppInfo.GetString(
    "change_param",
    "state_remove",
    "");

            this.comboBox_location.Text = this.MainForm.AppInfo.GetString(
    "change_param",
    "location",
    "<���ı�>");

            this.comboBox_bookType.Text = this.MainForm.AppInfo.GetString(
    "change_param",
    "bookType",
    "<���ı�>");

            this.comboBox_batchNo.Text = this.MainForm.AppInfo.GetString(
"change_param",
"batchNo",
"<���ı�>");

            this.comboBox_focusAction.Text = this.MainForm.AppInfo.GetString(
    "change_param",
    "focusAction",
    "������ţ���ȫѡ");

            this.comboBox_state_TextChanged(null, null);
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            // ����ֵ
            this.MainForm.AppInfo.SetString(
                "change_param",
                "state",
                this.comboBox_state.Text);
            this.MainForm.AppInfo.SetString(
    "change_param",
    "state_add",
    this.checkedComboBox_stateAdd.Text);
            this.MainForm.AppInfo.SetString(
    "change_param",
    "state_remove",
    this.checkedComboBox_stateRemove.Text);

            this.MainForm.AppInfo.SetString(
    "change_param",
    "location",
    this.comboBox_location.Text);

            this.MainForm.AppInfo.SetString(
    "change_param",
    "bookType",
    this.comboBox_bookType.Text);

            this.MainForm.AppInfo.SetString(
    "change_param",
    "batchNo",
    this.comboBox_batchNo.Text);


            this.MainForm.AppInfo.SetString(
    "change_param",
    "focusAction",
    this.comboBox_focusAction.Text);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // 2009/7/19 new add
        int m_nInDropDown = 0;

        void FillDropDown(ComboBox combobox)
        {
            // ��ֹ����
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {
                if (combobox.Items.Count == 0
                    && this.GetValueTable != null)
                {
                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    e1.DbName = this.RefDbName;

                    if (combobox == this.comboBox_bookType)
                        e1.TableName = "bookType";
                    else if (combobox == this.comboBox_location)
                        e1.TableName = "location";
                    else if (combobox == this.comboBox_state)
                        e1.TableName = "state";
                    else
                    {
                        Debug.Assert(false, "��֧�ֵ�combobox");
                    }

                    this.GetValueTable(this, e1);

                    combobox.Items.Add("<���ı�>");
                    if (combobox == this.comboBox_state)
                    {
                        combobox.Items.Add("<������>");
                    }

                    if (e1.values != null)
                    {
                        List<string> results = null;

                        string strLibraryCode = "";
                        string strPureName = "";

                        string strLocationString = this.comboBox_location.Text;
                        if (strLocationString == "<���ı�>")
                            strLocationString = "";

                        Global.ParseCalendarName(strLocationString,
                    out strLibraryCode,
                    out strPureName);

                        if (combobox != this.comboBox_location  // �ݲصص��б�Ҫ������
                            && String.IsNullOrEmpty(strLocationString) == false)
                        {
                            // ���˳����Ϲݴ������Щֵ�ַ���
                            results = Global.FilterValuesWithLibraryCode(strLibraryCode,
                                StringUtil.FromStringArray(e1.values));
                        }
                        else
                        {
                            results = StringUtil.FromStringArray(e1.values);
                        }

                        foreach (string s in results)
                        {
                            combobox.Items.Add(s);
                        }

#if NO
                        for (int i = 0; i < e1.values.Length; i++)
                        {
                            combobox.Items.Add(e1.values[i]);
                        }
#endif
                    }
                    else
                    {
                        combobox.Items.Add("{not found}");
                    }
                }
            }
            finally
            {
                this.Cursor = oldCursor;
                this.m_nInDropDown--;
            }
        }

        private void comboBox_location_DropDown(object sender, EventArgs e)
        {
            FillDropDown((ComboBox)sender);
        }

        private void comboBox_bookType_DropDown(object sender, EventArgs e)
        {
            FillDropDown((ComboBox)sender);
        }

        private void comboBox_state_DropDown(object sender, EventArgs e)
        {
            FillDropDown((ComboBox)sender);
        }

        private void checkedComboBox_stateAdd_DropDown(object sender, EventArgs e)
        {
            if (this.checkedComboBox_stateAdd.Items.Count > 0)
                return;
            FillItemStateDropDown(this.checkedComboBox_stateAdd);
        }

        private void checkedComboBox_stateRemove_DropDown(object sender, EventArgs e)
        {
            if (this.checkedComboBox_stateRemove.Items.Count > 0)
                return;
            FillItemStateDropDown(this.checkedComboBox_stateRemove);
        }

        void FillItemStateDropDown(CheckedComboBox combobox)
        {
            // ��ֹ����
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {
                if (combobox.Items.Count <= 0
                    && this.GetValueTable != null)
                {
                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    e1.DbName = this.RefDbName;

                    e1.TableName = "state";

                    this.GetValueTable(this, e1);

                    if (e1.values != null)
                    {
                        List<string> results = null;

                        string strLibraryCode = "";
                        string strPureName = "";

                        string strLocationString = this.comboBox_location.Text;
                        if (strLocationString == "<���ı�>")
                            strLocationString = "";

                        Global.ParseCalendarName(strLocationString,
                    out strLibraryCode,
                    out strPureName);

                        if (String.IsNullOrEmpty(strLocationString) == false)
                        {
                            // ���˳����Ϲݴ������Щֵ�ַ���
                            results = Global.FilterValuesWithLibraryCode(strLibraryCode,
                                StringUtil.FromStringArray(e1.values));
                        }
                        else
                        {
                            results = StringUtil.FromStringArray(e1.values);
                        }

                        foreach (string s in results)
                        {
                            combobox.Items.Add(s);
                        }
#if NO
                        for (int i = 0; i < e1.values.Length; i++)
                        {
                            combobox.Items.Add(e1.values[i]);
                        }
#endif
                    }
                    else
                    {
                        // combobox.Items.Add("{not found}");
                    }
                }
            }
            finally
            {
                this.Cursor = oldCursor;
                this.m_nInDropDown--;
            }
        }

        private void comboBox_state_TextChanged(object sender, EventArgs e)
        {
            string strText = this.comboBox_state.Text;

            if (strText == "<������>")
            {
                this.checkedComboBox_stateAdd.Enabled = true;
                this.checkedComboBox_stateRemove.Enabled = true;
            }
            else
            {
                this.checkedComboBox_stateAdd.Text = "";
                this.checkedComboBox_stateAdd.Enabled = false;

                this.checkedComboBox_stateRemove.Text = "";
                this.checkedComboBox_stateRemove.Enabled = false;
            }

            if (strText == "<���ı�>")
                this.label_state.BackColor = this.BackColor;
            else
                this.label_state.BackColor = Color.Green;

        }

        private void comboBox_location_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_location.Invalidate();
        }

        private void comboBox_bookType_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_bookType.Invalidate();
        }

        private void comboBox_state_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_state.Invalidate();
        }

        private void comboBox_batchNo_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_batchNo.Invalidate();
        }

        private void comboBox_focusAction_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_focusAction.Invalidate();
        }

        private void comboBox_location_TextChanged(object sender, EventArgs e)
        {
            this.comboBox_state.Items.Clear();
            this.checkedComboBox_stateAdd.Items.Clear();
            this.checkedComboBox_stateRemove.Items.Clear();
            this.comboBox_bookType.Items.Clear();

            string strText = this.comboBox_location.Text;

            if (strText == "<���ı�>")
                this.label_location.BackColor = this.BackColor;
            else
                this.label_location.BackColor = Color.Green;
        }

        private void comboBox_bookType_TextChanged(object sender, EventArgs e)
        {
            string strText = this.comboBox_bookType.Text;

            if (strText == "<���ı�>")
                this.label_bookType.BackColor = this.BackColor;
            else
                this.label_bookType.BackColor = Color.Green;

        }

        private void comboBox_batchNo_TextChanged(object sender, EventArgs e)
        {
            string strText = this.comboBox_batchNo.Text;

            if (strText == "<���ı�>")
                this.label_batchNo.BackColor = this.BackColor;
            else
                this.label_batchNo.BackColor = Color.Green;
        }

#if NO
        delegate void Delegate_filterValue(Control control);

        // ���˵� {} ��Χ�Ĳ���
        void FileterValue(Control control)
        {
            string strText = Global.GetPureSeletedValue(control.Text);
            if (control.Text != strText)
                control.Text = strText;
        }

        // ���˵� {} ��Χ�Ĳ���
        // �����б�ֵȥ�صĹ���
        void FileterValueList(Control control)
        {
            List<string> results = StringUtil.FromListString(Global.GetPureSeletedValue(control.Text));
            StringUtil.RemoveDupNoSort(ref results);
            string strText = StringUtil.MakePathList(results);
            if (control.Text != strText)
                control.Text = strText;
        }
#endif

        private void checkedComboBox_stateAdd_TextChanged(object sender, EventArgs e)
        {
            Global.FilterValueList(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValueList);
            this.BeginInvoke(d, new object[] { sender });
#endif
        }

        private void checkedComboBox_stateRemove_TextChanged(object sender, EventArgs e)
        {
            Global.FilterValueList(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValueList);
            this.BeginInvoke(d, new object[] { sender });
#endif
        }

        private void comboBox_bookType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValue);
            this.BeginInvoke(d, new object[] { sender });
#endif
        }

        private void comboBox_state_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValue);
            this.BeginInvoke(d, new object[] { sender });
#endif
        }

    }
}