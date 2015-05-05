using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DigitalPlatform.Text;

namespace DigitalPlatform.CirculationClient
{
    /// <summary>
    /// �ݲص�Ի���
    /// </summary>
    public partial class LocationItemDialog : Form
    {
        /// <summary>
        /// �Ƿ�Ϊ����ģʽ
        /// </summary>
        public bool CreateMode = false; // �Ƿ�Ϊ����ģʽ��==trueΪ����ģʽ��==falseΪ�޸�ģʽ

        /// <summary>
        /// ͼ��ݴ����б��ַ������ṩ�� combobox ʹ��
        /// </summary>
        public string LibraryCodeList
        {
            get
            {
                StringBuilder text = new StringBuilder();
                foreach (string s in this.comboBox_libraryCode.Items)
                {
                    if (text.Length > 0)
                        text.Append(",");
                    text.Append(s);
                }
                return text.ToString();
            }
            set
            {
                List<string> values = StringUtil.SplitList(value);
                this.comboBox_libraryCode.Items.Clear();
                foreach (string s in values)
                {
                    this.comboBox_libraryCode.Items.Add(s);
                }
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public LocationItemDialog()
        {
            InitializeComponent();
        }

        private void LocationItemDialog_Load(object sender, EventArgs e)
        {
            // ���ֻ��һ���б��������ǰΪ�հף����Զ����ú���һ��
            if (this.CreateMode == true
                && string.IsNullOrEmpty(this.comboBox_libraryCode.Text) == true
                && this.comboBox_libraryCode.Items.Count > 0)
                this.comboBox_libraryCode.Text = (string)this.comboBox_libraryCode.Items[0];
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            if (this.textBox_location.Text == "")
            {
                // ����ݲصص�Ϊ�գ�����Ҫȷ��һ��

                DialogResult msgResult = MessageBox.Show(this,
                    "ȷʵҪ�ѹݲصص���������Ϊ��?",
                    "LocationItemDialog",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (msgResult == DialogResult.No)
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

        /// <summary>
        /// �ݴ���
        /// </summary>
        public string LibraryCode
        {
            get
            {
                return this.comboBox_libraryCode.Text;
            }
            set
            {
                this.comboBox_libraryCode.Text = value;
            }
        }

        /// <summary>
        /// �ݲص��ַ���
        /// </summary>
        public string LocationString
        {
            get
            {
                return this.textBox_location.Text;
            }
            set
            {
                this.textBox_location.Text = value;
            }
        }

        /// <summary>
        /// �Ƿ��������
        /// </summary>
        public bool CanBorrow
        {
            get
            {
                return this.checkBox_canBorrow.Checked;
            }
            set
            {
                this.checkBox_canBorrow.Checked = value;
            }
        }

        /// <summary>
        /// ������ſ�Ϊ��
        /// </summary>
        public bool ItemBarcodeNullable
        {
            get
            {
                return this.checkBox_itemBarcodeNullable.Checked;
            }
            set
            {
                this.checkBox_itemBarcodeNullable.Checked = value;
            }
        }
    }
}