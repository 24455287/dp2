using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform;

namespace dp2Circulation
{
    /// <summary>
    /// ͨ�����κŽ��м���
    /// </summary>
    internal partial class SearchByBatchnoForm : Form
    {
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// ��ȡ���κ�key+countֵ�б�
        /// </summary>
        public event GetKeyCountListEventHandler GetBatchNoTable = null;

        // ������Ϣ��С����
        public string CfgSectionName = "SearchByBatchnoForm";

        public event GetValueTableEventHandler GetLocationValueTable = null;

        public string RefDbName = "";

        public SearchByBatchnoForm()
        {
            InitializeComponent();
        }

        private void SearchByBatchnoForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
            if (this.comboBox_batchNo.Text == "")
            {
                this.comboBox_batchNo.Text = this.MainForm.AppInfo.GetString(
                    this.CfgSectionName, // "SearchByBatchnoForm",
                    "batchno",
                    "");
                this.comboBox_location.Text = this.MainForm.AppInfo.GetString(
                    this.CfgSectionName, // "SearchByBatchnoForm",
                    "location",
                    "<��ָ��>");
            }
            else
            {
                // ��batchno����Ԥ��׼����ֵ��ʱ��location����Ҫ��ɡ���ָ�����ˣ������õ���ǰ������ֵ
                this.comboBox_location.Text = "<��ָ��>";
            }
        }

        private void SearchByBatchnoForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.MainForm.AppInfo.SetString(
                this.CfgSectionName,    // "SearchByBatchnoForm",
                "batchno",
                this.comboBox_batchNo.Text);
            this.MainForm.AppInfo.SetString(
                this.CfgSectionName,    // "SearchByBatchnoForm",
                "location",
                this.comboBox_location.Text);

        }

        public string BatchNo
        {
            get
            {
                return this.comboBox_batchNo.Text;
            }
            set
            {
                this.comboBox_batchNo.Text = value;
            }
        }

        public string ItemLocation
        {
            get
            {
                return this.comboBox_location.Text;
            }
            set
            {
                this.comboBox_location.Text = value;
            }
        }

        private void button_search_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void comboBox_location_DropDown(object sender, EventArgs e)
        {
            FillDropDown((ComboBox)sender);
        }

        // ��ֹ���� 2009/7/19
        int m_nInDropDown = 0;

        void FillDropDown(ComboBox combobox)
        {
            // ��ֹ���� 2009/7/19
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {
                if (combobox.Items.Count == 0
                    && this.GetLocationValueTable != null)
                {
                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    e1.DbName = this.RefDbName;

                    /*
                    if (combobox == this.comboBox_bookType)
                        e1.TableName = "bookType";
                    else if (combobox == this.comboBox_location)
                        e1.TableName = "location";
                    else if (combobox == this.comboBox_state)
                        e1.TableName = "state";
                    else
                    {
                        Debug.Assert(false, "��֧�ֵ�combobox");
                    }*/

                    if (combobox == this.comboBox_location)
                        e1.TableName = "location";
                    else
                    {

                        Debug.Assert(false, "��֧�ֵ�combobox");
                    }


                    this.GetLocationValueTable(this, e1);

                    combobox.Items.Add("<��ָ��>");

                    if (e1.values != null)
                    {
                        for (int i = 0; i < e1.values.Length; i++)
                        {
                            combobox.Items.Add(e1.values[i]);
                        }
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

        // �Ƿ���ʾ�ݲصص� ComboBox
        public bool DisplayLocationList
        {
            get
            {
                return this.comboBox_location.Visible;
            }
            set
            {
                this.comboBox_location.Visible = value;
                this.label_location.Visible = value;
            }
        }

        // dropdown�¼����������combobox.Enabled���޸ģ�������޷���ס����״̬�����Ը��÷�ֹ���������
        private void comboBox_batchNo_DropDown(object sender, EventArgs e)
        {
            // ��ֹ����
            if (this.m_nInDropDown > 0)
                return;

            ComboBox combobox = (ComboBox)sender;
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {
                if (combobox.Items.Count == 0
                    && this.GetBatchNoTable != null)
                {
                    GetKeyCountListEventArgs e1 = new GetKeyCountListEventArgs();
                    this.GetBatchNoTable(this, e1);

                    // 2013/3/25
                    // ������ ComboBox ����ʾ��ʱ�򣬲Ŷ� ���κ� �б�����������
                    if (this.comboBox_location.Visible == true)
                        combobox.Items.Add("<��ָ��>");

                    if (e1.KeyCounts != null)
                    {
                        for (int i = 0; i < e1.KeyCounts.Count; i++)
                        {
                            KeyCount item = e1.KeyCounts[i];
                            combobox.Items.Add(item.Key + "\t" + item.Count.ToString() + "��");
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

        private void comboBox_batchNo_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_batchNo.Invalidate();
        }

        private void comboBox_location_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_location.Invalidate();
        }
    }

    /// <summary>
    /// �ؼ��ʺ��������ֵ
    /// </summary>
    public class KeyCount
    {
        /// <summary>
        /// �ؼ���
        /// </summary>
        public string Key = "";

        /// <summary>
        /// ����
        /// </summary>
        public int Count = 0;
    }

    /// <summary>
    /// ���key+countֵ�б��¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void GetKeyCountListEventHandler(object sender,
        GetKeyCountListEventArgs e);

    /// <summary>
    /// ���key+countֵ�б��¼��Ĳ���
    /// </summary>
    public class GetKeyCountListEventArgs : EventArgs
    {
        /// <summary>
        /// ֵ�б�
        /// </summary>
        public List<KeyCount> KeyCounts = null;
    }
}