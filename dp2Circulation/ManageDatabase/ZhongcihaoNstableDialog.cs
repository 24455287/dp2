using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DigitalPlatform.Xml;

namespace dp2Circulation
{
    internal partial class ZhongcihaoNstableDialog : Form
    {
        public ZhongcihaoNstableDialog()
        {
            InitializeComponent();
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (String.IsNullOrEmpty(this.textBox_xml.Text.Trim()) == true)
            {
                strError = "XML���벻��Ϊ��";
                goto ERROR1;
            }

            // У�鿴��XML�Ƿ���ȷ
            string strOutXml = "";
            int nRet = DomUtil.GetIndentXml(this.textBox_xml.Text,
                out strOutXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

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

        public string XmlString
        {
            get
            {
                return this.textBox_xml.Text;
            }
            set
            {
                this.textBox_xml.Text = value;
            }

        }
    }
}