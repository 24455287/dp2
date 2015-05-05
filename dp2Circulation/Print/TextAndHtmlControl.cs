using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace dp2Circulation.Print
{
    internal partial class TextAndHtmlControl : UserControl
    {
        bool m_bEnableHtml = false;

        public TextAndHtmlControl()
        {
            InitializeComponent();

            if (this.m_bEnableHtml == false)
            {
                // Ҫ����html��ز���
                HideHtmlControls(true);
            }
        }

        [Category("Appearance")]
        [DescriptionAttribute("Enable Html")]
        [DefaultValue(false)]
        public bool EnableHtml
        {
            get
            {
                return this.m_bEnableHtml;
            }
            set
            {
                bool bOldValue = this.m_bEnableHtml;
                this.m_bEnableHtml = value;

                if (bOldValue != value)
                {
                    if (value == true)
                    {
                        HideHtmlControls(false);
                        Global.SetHtmlString(this.webBrowser1, this.textBox_text.Text);
                    }
                    else
                    {
                        HideHtmlControls(true);
                        Global.SetHtmlString(this.webBrowser1, "<blank>");
                    }
                }
            }
        }

        [Category("Appearance")]
        [DescriptionAttribute("Text")]
        [DefaultValue("")]
        public override string Text
        {
            get
            {
                return this.textBox_text.Text;
            }
            set
            {
                this.textBox_text.Text = value;

                if (this.EnableHtml == true)
                {
                    Global.SetHtmlString(this.webBrowser1, value);
                }
            }
        }

        [Category("Appearance")]
        [DescriptionAttribute("ReadOnly")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get
            {
                return this.textBox_text.ReadOnly;
            }
            set
            {
                this.textBox_text.ReadOnly = value;
            }
        }

        void HideHtmlControls(bool bHide)
        {
            if (bHide == true)
            {
                this.tabControl1.TabPages.Remove(this.tabPage_html);

                this.tabPage_text.Text = "���ı�";
            }
            else
            {
                this.tabControl1.TabPages.Add(this.tabPage_html);
                this.tabControl1.SelectTab(this.tabPage_html);

                this.tabPage_text.Text = "HTML����";
                this.tabPage_html.Text = "��ӡЧ��";
            }
        }
    }
}
