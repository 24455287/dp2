using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;
using System.Diagnostics;

using System.Runtime.InteropServices;


using DigitalPlatform;
using DigitalPlatform.GUI;

namespace DigitalPlatform.CommonControl
{
    public partial class BinaryEditor : Control
    {
        // �и�
        int m_nLineHeight = 14;
        int m_nCellWidth = 40;
        int m_nLineTitleWidth = 110;
        int m_nCommentWidth = 200;

        string m_strTempFileName = "";
        Stream m_stream = null;


        ///

        BorderStyle borderStyle = BorderStyle.Fixed3D;

        int nNestedSetScrollBars = 0;

        // ��������� С�ڵ���1.0F
        double m_v_ratio = 1.0F;
        double m_h_ratio = 1.0F;

        int m_nLeftBlank = 10;	// �߿�
        int m_nRightBlank = 10;
        int m_nTopBlank = 10;
        int m_nBottomBlank = 10;

        long m_lWindowOrgX = 0;    // ����ԭ��
        long m_lWindowOrgY = 0;

        long m_lContentWidth = 0;    // ���ݲ��ֵĿ��
        long m_lContentHeight = 0;   // ���ݲ��ֵĸ߶�

        public BinaryEditor()
        {
            InitializeComponent();
        }

        [Category("Appearance")]
        [DescriptionAttribute("Border style of the control")]
        [DefaultValue(typeof(System.Windows.Forms.BorderStyle), "None")]
        public BorderStyle BorderStyle
        {
            get
            {
                return borderStyle;
            }
            set
            {
                borderStyle = value;

                // Get Styles using Win32 calls
                int style = API.GetWindowLong(Handle, API.GWL_STYLE);
                int exStyle = API.GetWindowLong(Handle, API.GWL_EXSTYLE);

                // Modify Styles to match the selected border style
                BorderStyleToWindowStyle(ref style, ref exStyle);

                // Set Styles using Win32 calls
                API.SetWindowLong(Handle, API.GWL_STYLE, style);
                API.SetWindowLong(Handle, API.GWL_EXSTYLE, exStyle);

                // Tell Windows that the frame changed
                API.SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, 0, 0,
                    API.SWP_NOACTIVATE | API.SWP_NOMOVE | API.SWP_NOSIZE |
                    API.SWP_NOZORDER | API.SWP_NOOWNERZORDER |
                    API.SWP_FRAMECHANGED);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams param = base.CreateParams;

                if (borderStyle == BorderStyle.FixedSingle)
                {
                    param.Style |= API.WS_BORDER;
                }
                else if (borderStyle == BorderStyle.Fixed3D)
                {
                    param.ExStyle |= API.WS_EX_CLIENTEDGE;
                }

                return param;
            }
        }



        private void BorderStyleToWindowStyle(ref int style, ref int exStyle)
        {
            style &= ~API.WS_BORDER;
            exStyle &= ~API.WS_EX_CLIENTEDGE;
            switch (borderStyle)
            {
                case BorderStyle.Fixed3D:
                    exStyle |= API.WS_EX_CLIENTEDGE;
                    break;

                case BorderStyle.FixedSingle:
                    style |= API.WS_BORDER;
                    break;

                case BorderStyle.None:
                    // No border style values
                    break;
            }
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case API.WM_GETDLGCODE:
                    m.Result = new IntPtr(API.DLGC_WANTALLKEYS | API.DLGC_WANTARROWS | API.DLGC_WANTCHARS);
                    return;
                case API.WM_VSCROLL:
                    {
                        int CellWidth = 100;
                        switch (API.LoWord(m.WParam.ToInt32()))
                        {
                            case API.SB_BOTTOM:
                                break;
                            case API.SB_TOP:
                                break;
                            case API.SB_THUMBPOSITION:
                            case API.SB_THUMBTRACK:
                                this.Update();
                                int v = API.HiWord(m.WParam.ToInt32());
                                if (this.m_v_ratio != 1.0F)
                                    DocumentOrgY = -(long)((double)v / this.m_v_ratio);
                                else
                                    DocumentOrgY = -v;
                                break;
                            case API.SB_LINEDOWN:
                                DocumentOrgY -= (int)CellWidth;
                                break;
                            case API.SB_LINEUP:
                                DocumentOrgY += (int)CellWidth;
                                break;
                            case API.SB_PAGEDOWN:
                                DocumentOrgY -= this.ClientSize.Height;
                                break;
                            case API.SB_PAGEUP:
                                DocumentOrgY += this.ClientSize.Height;
                                break;
                        }
                        // MessageBox.Show("this");
                    }
                    break;

                case API.WM_HSCROLL:
                    {

                        switch (API.LoWord(m.WParam.ToInt32()))
                        {
                            case API.SB_THUMBPOSITION:
                            case API.SB_THUMBTRACK:
                                int v = API.HiWord(m.WParam.ToInt32());
                                if (this.m_h_ratio != 1.0F)
                                    DocumentOrgX = -(long)((double)v / this.m_h_ratio);
                                else
                                    DocumentOrgX = -v;
                                break;
                            case API.SB_LINEDOWN:
                                DocumentOrgX -= 20;
                                break;
                            case API.SB_LINEUP:
                                DocumentOrgX += 20;
                                break;
                            case API.SB_PAGEDOWN:
                                DocumentOrgX -= this.ClientSize.Width;
                                break;
                            case API.SB_PAGEUP:
                                DocumentOrgX += this.ClientSize.Width;
                                break;
                        }
                    }
                    break;

                default:
                    break;

            }

            base.DefWndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {

            long lLineCount = this.LineCount;

            long x = m_lWindowOrgX + m_nLeftBlank;
            long y = m_lWindowOrgY + m_nTopBlank;
            for (long i = 0; i < lLineCount; i++)
            {
                if (TooLarge(x) == true
                    || TooLarge(y) == true)
                    goto CONTINUE;

                // �Ż�
                RectangleF rect = new RectangleF((int)x,
                    (int)y,
                    this.m_lContentWidth,
                    this.m_nLineHeight);

                if (y > pe.ClipRectangle.Y + pe.ClipRectangle.Height)
                    break;


                if (rect.IntersectsWith(pe.ClipRectangle) == false)
                    goto CONTINUE;

                PaintLine(
                    x,
                    y,
                    i,
                    pe);

                CONTINUE:
                y += this.m_nLineHeight;

            }

            // Calling the base class OnPaint
            base.OnPaint(pe);
        }

        public long LineCount
        {
            get
            {
                if (this.m_stream == null)
                    return 0;

                return (this.m_stream.Length / 16) + ((this.m_stream.Length % 16) != 0 ? 1 : 0);
            }
        }

        protected override void DestroyHandle()
        {
            base.DestroyHandle();

            DeleteTempFile();
        }

        // ����һ����
        public void PaintLine(
            long x0,
            long y0,
            long lLineNumber,
            PaintEventArgs e)
        {
            long x = x0;
            long y = y0;

            // ȡ������
            byte[] buffer = new byte[16];

            this.m_stream.Seek(lLineNumber * 16, SeekOrigin.Begin);
            int nBytes = this.m_stream.Read(buffer, 0, 16);


            // Font font = new Font("Courier New", this.m_nLineHeight, FontStyle.Regular, GraphicsUnit.Pixel);
            Font font = this.Font;
            Brush brushText = null;

            brushText = new SolidBrush(this.ForeColor);

            // ��߱���
            {
                string strText = Convert.ToString(lLineNumber*16, 16).PadLeft(8, '0');

                StringFormat stringFormat = new StringFormat();

                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                RectangleF rect = new RectangleF(
                    x,
                    y,
                    this.m_nLineTitleWidth,
                    this.m_nLineHeight);

                e.Graphics.DrawString(strText,
                    font,
                    brushText,
                    rect,
                    stringFormat);


            }


            // ����
            x += this.m_nLineTitleWidth;
            for (int i = 0; i < nBytes; i++)
            {
                string strText = Convert.ToString(buffer[i], 16).PadLeft(2, '0').ToUpper();

                int nDelta = 0;
                if (i >= 8)
                    nDelta = this.m_nCellWidth/2;

                StringFormat stringFormat = new StringFormat();

                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                RectangleF rect = new RectangleF(
                    x + i * this.m_nCellWidth + nDelta,
                    y,
                    this.m_nCellWidth,
                    this.m_nLineHeight);

                e.Graphics.DrawString(strText,
                    font,
                    brushText,
                    rect,
                    stringFormat);

            }

            // ע��
            x += (this.m_nCellWidth * 16) + (this.m_nCellWidth / 2);
            {
                string strText = "";
                /*
                for (int i = 0; i < nBytes; i++)
                {
                    char c = (char)buffer[i];
                    if (char.IsLetterOrDigit(c) == true)
                        strText += c;
                    else
                        strText += '.';
                }
                 * */
                string strTextSource = Encoding.ASCII.GetString(buffer, 0, nBytes);

                for (int i = 0; i < strTextSource.Length; i++)
                {
                    char c = strTextSource[i];
                    if (char.IsLetterOrDigit(c) == true)
                        strText += c;
                    else
                        strText += '.';
                }

                strText.PadRight(16, ' ');

                StringFormat stringFormat = new StringFormat();

                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Center;

                RectangleF rect = new RectangleF(
                    x,
                    y,
                    this.m_nCommentWidth,
                    this.m_nLineHeight);

                e.Graphics.DrawString(strText,
                    font,
                    brushText,
                    rect,
                    stringFormat);

            }

        }

        public override Font Font
        {
            get
            {
                return base.Font;
            }
            set
            {
                base.Font = value;

                InitialSizes(value);

                this.AfterDocumentChanged(ScrollBarMember.Both);
                this.Invalidate();
            }
        }

        // ���������ʼ�����ֳߴ����
        void InitialSizes(Font font)
        {
            // �и߶� �������м��
            this.m_nLineHeight = (int)(font.SizeInPoints + (font.SizeInPoints / 2));   // �����������߶�һ����м��

            // �����ַ�һ����Ԫ�Ŀ��
            using (Graphics graphicsTemp = Graphics.FromHwnd(this.Handle))
            {

                string strTextTemp = "MM";
                SizeF size = graphicsTemp.MeasureString(
                    strTextTemp,
                    font);

                this.m_nCellWidth = (int)(size.Width + (size.Width / 2));

                strTextTemp = "MMMMMMMM";   // 8
                size = graphicsTemp.MeasureString(
                    strTextTemp,
                    font);
                this.m_nLineTitleWidth = (int)(size.Width + (size.Width / 8));

                this.m_nCommentWidth = (int)(size.Width * 2 + (size.Width / 8));
            }
        }

        // ��������
        public void SetData(byte [] baData)
        {

            // �ڲ���Ҫһ����ʱ�ļ���������Ͻ�����ݸ��ƹ�����
            // ���ڲ������ڲ��ļ����������ݿ����ر��Ҳ��Ӱ���ڴ�

            DeleteTempFile();

            if (baData == null || baData.Length == 0)
            {
                AfterDocumentChanged(ScrollBarMember.Both);
                this.Invalidate();
                return;
            }

            this.m_strTempFileName = Path.GetTempFileName();

            this.m_stream = File.Create(this.m_strTempFileName);

            this.m_stream.Write(baData, 0, baData.Length);

            AfterDocumentChanged(ScrollBarMember.Both);
            this.Invalidate();
        }

        // return:
        //      null    ������󳤶���
        //      ����    ����
        public byte[] GetData(int maxlen)
        {
            if (this.m_stream.Length > (long)maxlen)
                return null;

            this.m_stream.Seek(0, SeekOrigin.Begin);
            byte [] result = new byte[this.m_stream.Length];
            this.m_stream.Read(result, 0, result.Length);
            return result;
        }
        
        // ��������
        public void SetData(Stream stream,
            long start,
            long len)
        {

            // �ڲ���Ҫһ����ʱ�ļ���������Ͻ�����ݸ��ƹ�����
            // ���ڲ������ڲ��ļ����������ݿ����ر��Ҳ��Ӱ���ڴ�

            DeleteTempFile();

            if (stream == null)
            {
                AfterDocumentChanged(ScrollBarMember.Both);
                this.Invalidate();
                return;
            }

            this.m_strTempFileName = Path.GetTempFileName();

            this.m_stream = File.Create(this.m_strTempFileName);

            stream.Seek(start, SeekOrigin.Begin);
            // ��������
            byte [] buffer = new byte[4096];
            long lWrited = 0;
            for (; ; )
            {
                int nThisBytes = (int)Math.Min((long)4096, len - lWrited);
                int nRet = stream.Read(buffer, 0, nThisBytes);
                if (nRet != 0)
                {
                    this.m_stream.Write(buffer, 0, nRet);
                }
                lWrited += nThisBytes;
                if (nRet < nThisBytes
                    || lWrited >= len)
                    break;
            }

            AfterDocumentChanged(ScrollBarMember.Both);
            this.Invalidate();
        }

        void DeleteTempFile()
        {
            if (this.m_stream != null)
            {
                this.m_stream.Close();
                this.m_stream = null;
            }
            if (String.IsNullOrEmpty(this.m_strTempFileName) == false)
            {
                File.Delete(this.m_strTempFileName);
                this.m_strTempFileName = "";
            }
        }


        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            InitialSizes(this.Font);

            AfterDocumentChanged(ScrollBarMember.Both);

            /*
            // �״���ʾǰ, OnSizeChanged()һ��Ҳû�б�����ǰ, ��ʾ�þ����
            SetScrollBars(ScrollBarMember.Both);
             * */
        }

        protected override void OnSizeChanged(System.EventArgs e)
        {

            try
            {
                SetScrollBars(ScrollBarMember.Both);
            }
            catch
            {
            }


            // ���client�����㹻�󣬵���org�����⿴����ĳ����
            DocumentOrgY = DocumentOrgY;
            DocumentOrgX = DocumentOrgX;


            base.OnSizeChanged(e);
        }



        public long DocumentWidth
        {
            get
            {
                return m_lContentWidth + (long)m_nLeftBlank + (long)m_nRightBlank;
            }

        }
        public long DocumentHeight
        {
            get
            {
                return m_lContentHeight + (long)m_nTopBlank + (long)m_nBottomBlank;
            }
        }

        public long DocumentOrgX
        {
            get
            {
                return m_lWindowOrgX;
            }
            set
            {
                long lWidth = DocumentWidth;
                int nViewportWidth = this.ClientSize.Width;

                long lWindowOrgX_old = m_lWindowOrgX;


                if (nViewportWidth >= lWidth)
                    m_lWindowOrgX = 0;
                else
                {
                    if (value <= -lWidth + nViewportWidth)
                        m_lWindowOrgX = -lWidth + nViewportWidth;
                    else
                        m_lWindowOrgX = value;

                    if (m_lWindowOrgX > 0)
                        m_lWindowOrgX = 0;
                }

                // AfterDocumentChanged(ScrollBarMember.Horz);
                SetScrollBars(ScrollBarMember.Horz);



                if (this.BackgroundImage != null)
                {
                    this.Invalidate();
                    return;
                }

                long lDelta = m_lWindowOrgX - lWindowOrgX_old;

                if (lDelta != 0)
                {
                    // �������ľ��볬��32λ������Χ
                    if (lDelta >= Int32.MaxValue || lDelta <= Int32.MinValue)
                        this.Invalidate();
                    else
                    {
                        RECT rect1 = new RECT();
                        rect1.left = 0;
                        rect1.top = 0;
                        rect1.right = this.ClientSize.Width;
                        rect1.bottom = this.ClientSize.Height;


                        API.ScrollWindowEx(this.Handle,
                            (int)lDelta,
                            0,
                            ref rect1,
                            IntPtr.Zero,	//	ref RECT lprcClip,
                            0,	// int hrgnUpdate,
                            IntPtr.Zero,	// ref RECT lprcUpdate,
                            API.SW_INVALIDATE /*| API.SW_SCROLLCHILDREN*/ /*int fuScroll*/);
                    }
                }

                // this.Invalidate();
            }
        }

        public long DocumentOrgY
        {
            get
            {
                return m_lWindowOrgY;
            }
            set
            {
                // Debug.Assert(value != 0, "");
                long lHeight = DocumentHeight;
                int nViewportHeight = this.ClientSize.Height;

                long lWindowOrgY_old = m_lWindowOrgY;

                if (nViewportHeight >= lHeight)
                    m_lWindowOrgY = 0;
                else
                {
                    if (value <= -lHeight + nViewportHeight)
                        m_lWindowOrgY = -lHeight + nViewportHeight;
                    else
                        m_lWindowOrgY = value;

                    if (m_lWindowOrgY > 0)
                        m_lWindowOrgY = 0;
                }


                // AfterDocumentChanged(ScrollBarMember.Vert);
                SetScrollBars(ScrollBarMember.Vert);

                if (this.BackgroundImage != null)
                {
                    this.Invalidate();
                    return;
                }

                long lDelta = m_lWindowOrgY - lWindowOrgY_old;
                if (lDelta != 0)
                {
                    // �������ľ��볬��32λ������Χ
                    if (lDelta >= Int32.MaxValue || lDelta <= Int32.MinValue)
                        this.Invalidate();
                    else
                    {

                        RECT rect1 = new RECT();
                        rect1.left = 0;
                        rect1.top = 0;
                        rect1.right = this.ClientSize.Width;
                        rect1.bottom = this.ClientSize.Height;


                        API.ScrollWindowEx(this.Handle,
                            0,
                            (int)lDelta,
                            ref rect1,
                            IntPtr.Zero,	//	ref RECT lprcClip,
                            0,	// int hrgnUpdate,
                            IntPtr.Zero,	// ref RECT lprcUpdate,
                            API.SW_INVALIDATE /*| API.SW_SCROLLCHILDREN*/ /*int fuScroll*/);

                    }

                }

                // this.Invalidate();
            }
        }

        // ���ĵ��ߴ���ĵ�ԭ��ı�󣬸��¾�����ȵ���ʩ״̬���Ա��ĵ��ɼ�
        void AfterDocumentChanged(ScrollBarMember member)
        {
            if (member == ScrollBarMember.Both
                || member == ScrollBarMember.Horz)
                this.m_lContentWidth = this.m_nCellWidth * 16 + (long)this.m_nLineTitleWidth + (long)this.m_nCommentWidth;

            if (member == ScrollBarMember.Both
               || member == ScrollBarMember.Vert)
                this.m_lContentHeight = this.m_nLineHeight * this.LineCount; 

            SetScrollBars(member);
        }

        public enum ScrollBarMember
        {
            Vert = 0,
            Horz = 1,
            Both = 2,
        };

        // ���һ��long�Ƿ�Խ��int16�ܱ���ֵ��Χ
        public static bool TooLarge(long lValue)
        {
            if (lValue >= Int16.MaxValue || lValue <= Int16.MinValue)
                return true;
            return false;
        }


        void SetScrollBars(ScrollBarMember member)
        {

            nNestedSetScrollBars++;


            try
            {
                int nClientWidth = this.ClientSize.Width;
                int nClientHeight = this.ClientSize.Height;

                // �ĵ��ߴ�
                long lDocumentWidth = DocumentWidth;
                long lDocumentHeight = DocumentHeight;

                long lWindowOrgX = this.m_lWindowOrgX;
                long lWindowOrgY = this.m_lWindowOrgY;

                if (member == ScrollBarMember.Horz
                    || member == ScrollBarMember.Both)
                {

                    if (TooLarge(lDocumentWidth) == true)
                    {
                        this.m_h_ratio = (double)(Int16.MaxValue - 1) / (double)lDocumentWidth;

                        lDocumentWidth = (long)((double)lDocumentWidth * m_h_ratio);
                        nClientWidth = (int)((double)nClientWidth * m_h_ratio);
                        lWindowOrgX = (long)((double)lWindowOrgX * m_h_ratio);
                    }
                    else
                        this.m_h_ratio = 1.0F;

                    // ˮƽ����
                    API.ScrollInfoStruct si = new API.ScrollInfoStruct();

                    si.cbSize = Marshal.SizeOf(si);
                    si.fMask = API.SIF_RANGE | API.SIF_POS | API.SIF_PAGE;
                    si.nMin = 0;
                    si.nMax = (int)lDocumentWidth;
                    si.nPage = nClientWidth;
                    si.nPos = -(int)lWindowOrgX;
                    API.SetScrollInfo(this.Handle, API.SB_HORZ, ref si, true);
                }


                if (member == ScrollBarMember.Vert
                    || member == ScrollBarMember.Both)
                {
                    if (TooLarge(lDocumentHeight) == true)
                    {
                        this.m_v_ratio = (double)(Int16.MaxValue - 1) / (double)lDocumentHeight;

                        lDocumentHeight = (long)((double)lDocumentHeight * m_v_ratio);
                        nClientHeight = (int)((double)nClientHeight * m_v_ratio);
                        lWindowOrgY = (long)((double)lWindowOrgY * m_v_ratio);

                    }
                    else
                        this.m_v_ratio = 1.0F;

                    // ��ֱ����
                    API.ScrollInfoStruct si = new API.ScrollInfoStruct();

                    si.cbSize = Marshal.SizeOf(si);
                    si.fMask = API.SIF_RANGE | API.SIF_POS | API.SIF_PAGE;
                    si.nMin = 0;
                    si.nMax = (int)lDocumentHeight;
                    si.nPage = nClientHeight;
                    si.nPos = -(int)lWindowOrgY;
                    // Debug.Assert(si.nPos != 0, "");
                    API.SetScrollInfo(this.Handle, API.SB_VERT, ref si, true);
                }

            }
            finally
            {
                nNestedSetScrollBars--;
            }
        }

    }
}
