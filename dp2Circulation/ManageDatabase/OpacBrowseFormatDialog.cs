using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using DigitalPlatform.Xml;

namespace dp2Circulation
{
    /// <summary>
    /// OPAC��¼�����ʽ����Ի���
    /// </summary>
    internal partial class OpacBrowseFormatDialog : Form
    {
        XmlDocument dom = null;

        public OpacBrowseFormatDialog()
        {
            InitializeComponent();
        }

        private void OpacBrowseFormatDialog_Load(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = LoadCaptionsXml(out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
            }
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";

            // ��鵱ǰ������ʽ���Ƿ�Ϸ�
            // return:
            //      -1  �����̱������
            //      0   ��ʽ�д���
            //      1   ��ʽû�д���
            int nRet = this.captionEditControl_formatName.Verify(out strError);
            if (nRet <= 0)
            {
                strError = "��ʽ��������: " + strError;
                this.captionEditControl_formatName.Focus();
                goto ERROR1;
            }

            if (this.dom == null)
            {
                this.dom = new XmlDocument();
                this.dom.LoadXml("<root />");
            }
            this.dom.DocumentElement.InnerXml = this.captionEditControl_formatName.Xml;

            if (String.IsNullOrEmpty(this.FormatName) == true)
            {
                strError = "ȱ�����Դ���Ϊzh�ĸ�ʽ��";
                goto ERROR1;
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

        // ��dom�е�captionƬ��������ʼ��CaptionEditControl
        int LoadCaptionsXml(out string strError)
        {
            strError = "";
            if (this.dom == null)
                return 0;

            try
            {
                this.captionEditControl_formatName.Xml = this.dom.DocumentElement.InnerXml;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            return 1;
        }

        public string FormatName
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetCaptionExt("zh", this.dom.DocumentElement);
            }
        }
 

        public string FormatType
        {
            get
            {
                return this.comboBox_type.Text;
            }
            set
            {
                this.comboBox_type.Text = value;
            }
        }

        public string FormatStyle
        {
            get
            {
                return this.textBox_style.Text;
            }
            set
            {
                this.textBox_style.Text = value;
            }
        }

        public string ScriptFile
        {
            get
            {
                return this.textBox_scriptFile.Text;
            }
            set
            {
                this.textBox_scriptFile.Text = value;
            }
        }

        /*
            <format name="��ϸ" type="biblio">
                <caption lang="zh-CN">��ϸ</caption>
                <caption lang="en">Detail</caption>
		    </format>
         * * */

        public string CaptionsXml
        {
            get
            {
                if (this.dom == null)
                    return "";

                return this.dom.DocumentElement.InnerXml;
            }
            set
            {
                if (this.dom == null)
                {
                    this.dom = new XmlDocument();
                    this.dom.LoadXml("<root />");
                }

                this.dom.DocumentElement.InnerXml = value;
            }
        }

        private void button_virtualDatabaseName_newBlankLine_Click(object sender, EventArgs e)
        {
            this.captionEditControl_formatName.NewElement();
        }
    }
}