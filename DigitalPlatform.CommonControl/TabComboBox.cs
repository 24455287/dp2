using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace DigitalPlatform.CommonControl
{
    /// <summary>
    /// �����б���������ʾ�Ʊ����λ�� ComboBox
    /// </summary>
    public partial class TabComboBox : ComboBox
    {
        [Category("Appearance")]
        [DescriptionAttribute("Left Part FontStyle")]
        [DefaultValue(typeof(FontStyle),"Regular")]
        public FontStyle LeftFontStyle 
        {
            get
            {
                return this.m_leftFontStyle;
            }
            set
            {
                this.m_leftFontStyle = value;
            }

        }
        FontStyle m_leftFontStyle = FontStyle.Regular;


        [Category("Appearance")]
        [DescriptionAttribute("Right Part FontStyle")]
        [DefaultValue(typeof(FontStyle),"Regular")]
        public FontStyle RightFontStyle
        {
            get
            {
                return this.m_rightFontStyle;
            }
            set
            {
                this.m_rightFontStyle = value;
            }
        }
        
        FontStyle m_rightFontStyle = FontStyle.Regular;

        public bool RemoveRightPartAtTextBox = true;    // ��textbox����ɾ�����ֵ��ұ߲���(Ҳ����'\t'�ұߵĲ��֣�����'\t')

        const int WM_CHANGED = API.WM_USER + 300;

        public TabComboBox()
        {
            InitializeComponent();
        }

        private void TabComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            string strText = (string)this.Items[e.Index];
            string strLeft = "";
            string strRight = "";
            int nRet = strText.IndexOf("\t");
            if (nRet == -1)
                strLeft = strText;
            else
            {
                strLeft = strText.Substring(0, nRet);
                strRight = strText.Substring(nRet + 1);
            }

            Brush brushBack = new SolidBrush(e.BackColor);
            e.Graphics.FillRectangle(brushBack, e.Bounds);
            brushBack.Dispose();
            brushBack = null;


            Brush brush = new SolidBrush(e.ForeColor);
            Font font = null;
            if (this.LeftFontStyle == FontStyle.Regular)
                font = e.Font;
            else
                font = new Font(e.Font, this.LeftFontStyle);

            e.Graphics.DrawString(strLeft, font, brush, e.Bounds);

            SizeF size = e.Graphics.MeasureString(strRight, e.Font);
            RectangleF rightBound = new RectangleF(e.Bounds.Right - size.Width,
                e.Bounds.Y,
                e.Bounds.Width,
                e.Bounds.Height);

            if (this.RightFontStyle == FontStyle.Regular)
                font = e.Font;
            else
                font = new Font(e.Font, this.RightFontStyle);

            e.Graphics.DrawString(strRight, font, brush, rightBound);

            brush.Dispose();
        }

        private void TabComboBox_TextChanged(object sender, EventArgs e)
        {
            if (RemoveRightPartAtTextBox == true)
                API.PostMessage(this.Handle, WM_CHANGED, 0, 0);
        }

        public static string GetLeftPart(string strText)
        {
            int nRet = strText.IndexOf("\t");
            if (nRet == -1)
                return strText;

            return strText.Substring(0, nRet).Trim();
        }

        // ɾ�������κ��ұߵĲ���
        void RemoveRightPart()
        {
            string strText = this.Text;
            int nRet = strText.IndexOf("\t");
            if (nRet != -1)
                this.Text = strText.Substring(0, nRet);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_CHANGED:
                    RemoveRightPart();
                    return;
            }

            // Debug.WriteLine(m.Msg.ToString());

            base.DefWndProc(ref m);
        }
    }
}
