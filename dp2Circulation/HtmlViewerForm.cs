using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace dp2Circulation
{
    /// <summary>
    /// �쿴 HTML ��ʾЧ���Ĵ���
    /// </summary>
    public partial class HtmlViewerForm : Form
    {
        string m_strHtmlString = "";

        /// <summary>
        /// ��ǰ HTML �ַ���
        /// </summary>
        public string HtmlString
        {
            get
            {
                return m_strHtmlString;
            }
            set
            {
                m_strHtmlString = value;
                Global.SetHtmlString(this.webBrowser1, value);
            }

        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public HtmlViewerForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// д�� HTML �ַ���
        /// </summary>
        /// <param name="strHtml">HTML �ַ���</param>
        public void WriteHtml(string strHtml)
        {
            Global.WriteHtml(this.webBrowser1,
                strHtml);
        }

     }
}