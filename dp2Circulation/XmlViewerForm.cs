using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace dp2Circulation
{
    /// <summary>
    /// ���ڲ鿴 XML ���ݵĴ���
    /// </summary>
    public partial class XmlViewerForm : Form
    {
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        string m_strXmlString = "";

        // string m_strTempFileName = "";

        /// <summary>
        /// XML �ַ���
        /// </summary>
        public string XmlString
        {
            get
            {
                return m_strXmlString;
            }
            set
            {
                m_strXmlString = value;
                this.SetXmlToWebbrowser(this.webBrowser1, 
                    m_strXmlString);
            }

        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public XmlViewerForm()
        {
            InitializeComponent();
        }

        void SetXmlToWebbrowser(WebBrowser webbrowser,
             string strXml)
        {
            string strTargetFileName = this.MainForm.DataDir + "\\xml.xml";

            StreamWriter sw = new StreamWriter(strTargetFileName,
                false,	// append
                System.Text.Encoding.UTF8);
            sw.Write(strXml);
            sw.Close();

            webbrowser.Navigate(strTargetFileName);
        }

        private void XmlViewerForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);
        }

        private void XmlViewerForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
        }
    }
}