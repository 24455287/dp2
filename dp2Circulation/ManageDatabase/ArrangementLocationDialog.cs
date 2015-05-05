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
using DigitalPlatform.LibraryServer;

namespace dp2Circulation
{
    internal partial class ArrangementLocationDialog : Form
    {
        public string LibraryCodeList = ""; // ��ǰ�û���Ͻ�Ĺݴ���

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        public List<string> ExcludingLocationNames = new List<string>();   // Ҫ�ų��ġ��Ѿ���ʹ���˵��ִκſ���

        public ArrangementLocationDialog()
        {
            InitializeComponent();
        }

        private void ArrangementLocationDialog_Load(object sender, EventArgs e)
        {

        }

        private void ArrangementLocationDialog_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void ArrangementLocationDialog_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        static bool MatchLocationNames(List<string> names, string name)
        {
            bool bNamePattern = false;  // name �����Ƿ����ͨ���?
            if (name.IndexOf("*") != -1)
                bNamePattern = true;

            foreach (string current in names)
            {
                bool bCurrentPattern = false;  // current �����Ƿ����ͨ���?
                if (current.IndexOf("*") != -1)
                    bCurrentPattern = true;

                if (bNamePattern == true)
                {
                    if (LibraryServerUtil.MatchLocationName(current, name) == true)
                        return true;

                    if (bCurrentPattern == true)
                    {
                        if (LibraryServerUtil.MatchLocationName(name, current) == true)
                            return true;
                    }
                }
                else
                {
                    if (bCurrentPattern == false)
                    {
                        if (current == name)
                            return true;
                    }
                    else
                    {
                        Debug.Assert(bCurrentPattern == true, "");
                        if (LibraryServerUtil.MatchLocationName(name, current) == true)
                            return true;
                    }
                }
            }

            return false;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.comboBox_location.Text == "")
            {
                strError = "��δָ���ݲصص�";
                goto ERROR1;
            }

            // ���Ի���������Ĺݲصص㣬�ǲ��Ǳ����ù��ģ�
            if (String.IsNullOrEmpty(this.comboBox_location.Text) == false
                && this.ExcludingLocationNames != null)
            {
                string strLocation = this.LocationString;   // �������滯�˵�ֵ����"<��>"��ת������ʵ�ʵ�ֵ

#if NO
                if (this.ExcludingLocationNames.IndexOf(strLocation) != -1)
                {
                    strError = "����ָ���Ĺݲصص� '" + this.comboBox_location.Text + "' �Ѿ���ʹ�ù���";
                    goto ERROR1;
                }
#endif
                if (MatchLocationNames(this.ExcludingLocationNames, strLocation) == true)
                {
                    strError = "����ָ���Ĺݲصص� '" + this.comboBox_location.Text + "' �Ѿ���ʹ�ù���";
                    goto ERROR1;
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

        public string LocationString
        {
            get
            {
                /*
                if (this.comboBox_location.Text == "<��>"
                    || this.comboBox_location.Text == "<blank>")
                    return "";

                return this.comboBox_location.Text;
                 * */
                string strText = this.comboBox_location.Text;
                return strText.Replace("<��>", "").Replace("<blank>", "");
            }
            set
            {
                /*
                if (String.IsNullOrEmpty(value) == true)
                    this.comboBox_location.Text = "<��>";
                else
                    this.comboBox_location.Text = value;
                 * */

                this.comboBox_location.Text = GetDisplayString(value);
            }
        }

        public static string GetDisplayString(string value)
        {
            if (String.IsNullOrEmpty(value) == true)
            {
                return "<��>";
            }

            string strLibraryCode = "";
            string strPureName = "";

            LocationCollection.ParseLocationName(value,
                out strLibraryCode,
                out strPureName);
            if (String.IsNullOrEmpty(strPureName) == true)
                return strLibraryCode + "/<��>";
            else
                return value;
        }

        int m_nInDropDown = 0;

        private void comboBox_location_DropDown(object sender, EventArgs e)
        {
            // ��ֹ���� 2009/2/23 new add
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {
                ComboBox combobox = (ComboBox)sender;
                int nCount = combobox.Items.Count;

                if (combobox.Items.Count == 0
                    && this.GetValueTable != null)
                {
                    // combobox.Items.Add("<��>");

                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    e1.DbName = "";

                    if (combobox == this.comboBox_location)
                        e1.TableName = "location";
                    else
                    {
                        Debug.Assert(false, "��֧�ֵ�sender");
                        return;
                    }

                    this.GetValueTable(this, e1);

                    if (e1.values != null)
                    {
                        List<string> values = new List<string>();
                        for (int i = 0; i < e1.values.Length; i++)
                        {
                            // �ų������Ѿ��ù���ֵ
                            if (this.ExcludingLocationNames != null)
                            {
                                if (this.ExcludingLocationNames.IndexOf(e1.values[i]) != -1)
                                    continue;
                            }

                            values.Add(e1.values[i]);
                        }

                        List<string> results = null;

                        if (String.IsNullOrEmpty(this.LibraryCodeList) == false)
                        {
                            // ���˳����Ϲݴ������Щֵ�ַ���
                            results = Global.FilterLocationsWithLibraryCodeList(this.LibraryCodeList,
                                values);
                        }
                        else
                        {
                            results = values;
                        }

                        foreach (string s in results)
                        {
                            combobox.Items.Add(GetDisplayString(s));
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
    }
}