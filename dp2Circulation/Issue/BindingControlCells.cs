using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Xml;

using System.Runtime.InteropServices;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    internal class CellBase
    {
        internal bool m_bHover = false;
        internal bool m_bFocus = false;

        // �Ƿ�ѡ��?
        internal bool m_bSelected = false;

        public bool Selected
        {
            get
            {
                return this.m_bSelected;
            }
            set
            {
                this.m_bSelected = value;
                // TODO: �ı���ʾ
            }
        }

        public virtual int Width
        {
            get
            {
                throw new Exception("Width not implement");
            }
        }

        public virtual int Height
        {
            get
            {
                throw new Exception("Height not implement");
            }
        }

        // ѡ��
        // ע�⣺��������ʧЧ�������
        // return:
        //      true    ״̬�����仯
        //      false   ״̬û�б仯
        public virtual bool Select(SelectAction action)
        {
            bool bOldSelected = this.m_bSelected;

            if (action == SelectAction.Off)
                this.m_bSelected = false;
            else if (action == SelectAction.On)
                this.m_bSelected = true;
            else
            {
                Debug.Assert(action == SelectAction.Toggle, "");
                if (this.m_bSelected == true)
                    this.m_bSelected = false;
                else
                    this.m_bSelected = true;
            }

            /*
            // �ݹ�
            if (bRecursive == true)
            {
                for (int i = 0; i < this.ChildrenCollection.Count; i++)
                {
                    AreaBase obj = this.ChildrenCollection[i];
                    obj.Select(action, true);
                }
            }*/

            return (bOldSelected == this.m_bSelected ? false : true);
        }
    }

    // ����HitTest()�����������࣬��ʾҪ���Ǳ�ڸ�����Ϣ
    internal class NullCell : CellBase
    {
        public int X = -1;
        public int Y = -1;

        public NullCell(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        // ����NullCellÿ��ĸ���
        internal void Paint(
            BindingControl control,
            long start_x,
            long start_y,
            PaintEventArgs e)
        {
            // Debug.Assert(control != null, "");

            if (BindingControl.TooLarge(start_x) == true
                || BindingControl.TooLarge(start_y) == true)
                return;


            int x0 = (int)start_x;
            int y0 = (int)start_y;

            RectangleF rect;

            // ��������
            if (this.m_bFocus == true)
            {
                rect = new RectangleF(
    (int)start_x,
    (int)start_y,
    control.m_nCellWidth,
    control.m_nCellHeight);

                rect.Inflate(-4, -4);
                ControlPaint.DrawFocusRectangle(e.Graphics,
                    Rectangle.Round(rect));
            }
        }
    }

    // ÿһ����ʾ��Ԫ
    internal class Cell : CellBase
    {
        public IssueBindingItem Container = null;

        public ItemBindingItem item = null;

        public bool OutofIssue = false; // ���ں��Ƿ��������в���? == true ������== false ����

        internal bool m_bDisplayCheckBox = false;   // �Ƿ�Ҫ��hoverʱ��ʾcheckboxͼ��

        // public bool Binded = false; // ��itemΪnullʱ�������Binded��ʾ����װ����ռ��λ�õĵ�Ԫ

        public ItemBindingItem ParentItem = null;  // ���������Ϊװ����Ա���������������ĺ϶�������

        // �Ƿ�Ϊ�϶���Ա����?
        // ע�⣬����Ϊ�϶���Ա���ӣ�Ҳ�����ǿհ׸���(��this.item == null)
        public bool IsMember
        {
            get
            {
                if (this.ParentItem != null)
                    return true;
                return false;
            }
        }

        public override int Width
        {
            get
            {
                return this.Container.Container.m_nCellWidth;
            }
        }

        public override int Height
        {
            get
            {
                return this.Container.Container.m_nCellHeight;
            }
        }

        public int LineHeight
        {
            get
            {
                return this.Container.Container.m_nLineHeight;
            }
        }



        // ѡ��
        // ע�⣺��������ʧЧ�������
        // return:
        //      true    ״̬�����仯
        //      false   ״̬û�б仯
        public override bool Select(SelectAction action)
        {
            bool bOldSelected = this.m_bSelected;

            if (action == SelectAction.Off)
                this.m_bSelected = false;
            else if (action == SelectAction.On)
                this.m_bSelected = true;
            else
            {
                Debug.Assert(action == SelectAction.Toggle, "");
                if (this.m_bSelected == true)
                    this.m_bSelected = false;
                else
                    this.m_bSelected = true;
            }

            /*
            // �ݹ�
            if (bRecursive == true)
            {
                for (int i = 0; i < this.ChildrenCollection.Count; i++)
                {
                    AreaBase obj = this.ChildrenCollection[i];
                    obj.Select(action, true);
                }
            }*/

            return (bOldSelected == this.m_bSelected ? false : true);
        }

        // ��������������ϵ �ľ��� ת��Ϊ ��������������ϵ
        public RectangleF ToRootCoordinate(RectangleF rect)
        {
            float x_offs = 0;
            float y_offs = 0;

            IssueBindingItem issue = this.Container;
            Debug.Assert(issue != null, "");

            x_offs += issue.Container.m_nLeftTextWidth;
            int index = issue.Cells.IndexOf(this);
            Debug.Assert(index != -1, "");

            x_offs += issue.Container.m_nCellWidth * index;

            BindingControl control = issue.Container;
            index = control.Issues.IndexOf(issue);
            Debug.Assert(index != -1, "");

            y_offs += index * control.m_nCellHeight;

            rect.X += x_offs;
            rect.Y += y_offs;
            return rect;
        }


        internal void RefreshOutofIssue()
        {
            if (this.Container == null)
                return;

            IssueBindingItem issue = this.Container;

            int nIndex = issue.IndexOfCell(this);
            if (nIndex == -1)
            {
                Debug.Assert(nIndex != -1, "");
                return;
            }

            /*
                        if ((nIndex % 2) == 0)
                        {
                            this.OutofIssue = false;
                            return; // ˫������û�б�Ҫ����?
                        }
             * */

            issue.RefreshOutofIssueValue(nIndex);
        }

        // Cell �������
        // parameters:
        //      p_x   �Ѿ����ĵ����ꡣ���ĵ����Ͻ�Ϊ(0,0)
        //      type    Ҫ���Ե����¼���Ҷ������������͡����Ϊnull����ʾһֱ��ĩ��
        public void HitTest(long p_x,
            long p_y,
            Type dest_type,
            out HitTestResult result)
        {
            result = new HitTestResult();

            if (GuiUtil.PtInRect((int)p_x,
                (int)p_y,
                this.Container.Container.RectGrab) == true)
            {
                result.AreaPortion = AreaPortion.Grab;
                result.X = p_x;
                result.Y = p_y;
                result.Object = this;
                return;
            }

            int nCenterX = this.Container.Container.m_nCellWidth / 2;
            int nCenterY = this.Container.Container.m_nCellHeight / 2;
            int nWidth = this.Container.Container.RectGrab.Width;
            Rectangle rectCheckBox = new Rectangle(
                nCenterX - nWidth / 2,
                nCenterY - nWidth / 2,
                nWidth,
                nWidth);
            if (GuiUtil.PtInRect((int)p_x,
    (int)p_y,
    rectCheckBox) == true)
            {
                result.AreaPortion = AreaPortion.CheckBox;
                result.X = p_x;
                result.Y = p_y;
                result.Object = this;
                return;
            }

            result.AreaPortion = AreaPortion.Content;
            result.X = p_x;
            result.Y = p_y;
            result.Object = this;
            return;
        }


        public PaintInfo GetPaintInfo()
        {
            PaintInfo info = new PaintInfo();
            // ��ͨ����
            info.ForeColor = this.Container.Container.SingleForeColor;

            if (this.IsMember == true)
            {
                // ��Ա��
                info.ForeColor = this.Container.Container.MemberForeColor;
                info.BackColor = this.Container.Container.MemberBackColor;
            }
            else if (this.item != null
                && this.item.Calculated == true)
            {
                // Ԥ��ĵ���
                info.ForeColor = this.Container.Container.CalculatedForeColor;
                info.BackColor = this.Container.Container.CalculatedBackColor;
            }
            else if (this.item != null
           && this.item.IsParent == true)
            {
                // �϶���
                info.ForeColor = this.Container.Container.ParentForeColor;
                info.BackColor = this.Container.Container.ParentBackColor;
            }
            else
            {
                info.BackColor = this.Container.Container.SingleBackColor;
            }

            return info;
        }

        public void PaintBorder(long start_x,
            long start_y,
            int nWidth,
            int nHeight,
            PaintEventArgs e)
        {
            Debug.Assert(this.Container != null, "");
            bool bSelected = this.Selected;
            RectangleF rect;

            // �Ƿ������������ת
            bool bRotate = false;

            // ��ͨ����
            Color colorText = this.Container.Container.SingleForeColor;
            Color colorGray = this.Container.Container.SingleGrayColor;
            Brush brushBack = null;
            float fBorderWidth = 2;
            Color colorBorder = Color.FromArgb(255, Color.Gray);

            // ����
            if (this.IsMember == true)
            {
                // ��Ա��
#if DEBUG
                if (this.item != null)
                {
                    Debug.Assert(this.item.IsMember == true, "");
                }
#endif
                fBorderWidth = 1;
                Color colorBack = this.Container.Container.MemberBackColor;
                {
                    brushBack = new SolidBrush(colorBack);
                }

                colorText = this.Container.Container.MemberForeColor;
                colorGray = this.Container.Container.MemberGrayColor;
            }
            else if (this.item != null
                && this.item.Calculated == true)
            {
                // Ԥ��ĵ���
                fBorderWidth = (float)1;    //  0.2;
                Color colorBack = this.Container.Container.CalculatedBackColor;
                {
                    brushBack = new SolidBrush(colorBack);
                }
                colorText = this.Container.Container.CalculatedForeColor;
                colorGray = this.Container.Container.CalculatedGrayColor;
                colorBorder = Color.FromArgb(255, Color.White);
            }
            else if (this.item != null
           && this.item.IsParent == true)
            {
                // �϶���
                fBorderWidth = 3;
                Color colorBack = this.Container.Container.ParentBackColor;
                {
                    brushBack = new SolidBrush(colorBack);
                }

                colorText = this.Container.Container.ParentForeColor;
                colorGray = this.Container.Container.ParentGrayColor;
            }
            else
            {
                brushBack = null;
                Color colorBack = this.Container.Container.SingleBackColor;
                {
                    brushBack = new SolidBrush(colorBack);
                }
            }


            // �߿�ͱ���
            {

                rect = new RectangleF(start_x,
                    start_y,
                    nWidth,
                    nHeight);

                /*
                // û�н���ʱҪСһЩ
                rect = GuiUtil.PaddingRect(this.Container.Container.CellMargin,
                    rect);
                 * */

                RectangleF rectTest = rect;
                rectTest.Inflate(1, 1);
                rectTest.Width += 2;
                rectTest.Height += 2;

                // �Ż�
                if (rectTest.IntersectsWith(e.ClipRectangle) == true
                    || bRotate == true)
                {
                    float round_radius = 10;
                    // ��Ӱ
                    RectangleF rectShadow = rect;
                    rectShadow.Offset((float)1.5, (float)1.5);  // 0.5
                    // rectShadow.Inflate((float)-0.9, (float)-0.9);
                    // Pen penShadow = new Pen(Color.FromArgb(160, 190,190,180),5);
                    Brush brushShadow = new SolidBrush(Color.FromArgb(160, 190, 190, 180));
                    BindingControl.RoundRectangle(e.Graphics,
                        null,
                        brushShadow,
                        rectShadow,
                        round_radius);

                    float que_radius = 0;
                    if (this.item != null)
                    {
                        string strIntact = this.item.GetText("intact");
                        float r = GetIntactRatio(strIntact);
                        if (r < (float)1.0)
                        {
                            float h = Math.Min(rect.Width, rect.Height) - (round_radius * 2);
                            que_radius = h - (h * r);
                            if (que_radius < round_radius)
                                que_radius = round_radius;
                        }
                    }


                    rect.Inflate(-(fBorderWidth / 2), -(fBorderWidth / 2));
                    Pen penBorder = new Pen(colorBorder, fBorderWidth);
                    penBorder.LineJoin = LineJoin.Bevel;
                    if (que_radius == 0)
                        BindingControl.RoundRectangle(e.Graphics,
                            penBorder,
                            brushBack,
                            rect,
                            round_radius);
                    else
                        BindingControl.QueRoundRectangle(e.Graphics,
                            penBorder,
                            brushBack,
                            rect,
                            round_radius,
                            que_radius);

                }
            }
        }


        // ����ÿ��ĸ���
        internal virtual void Paint(
            long start_x,
            long start_y,
            PaintEventArgs e)
        {
            Debug.Assert(this.Container != null, "");

            if (BindingControl.TooLarge(start_x) == true
                || BindingControl.TooLarge(start_y) == true)
                return;

#if DEBUG
            if (this.item != null)
            {
                Debug.Assert(this.item.IsMember == this.IsMember, "");
            }
            else
            {
            }
#endif


            //int x0 = (int)start_x;
            //int y0 = (int)start_y;

            bool bSelected = this.Selected;

            RectangleF rect;

            GraphicsState gstate = null;

            // �Ƿ������������ת
            bool bRotate = this.OutofIssue == true
                | (this.m_bFocus == true && this.m_bHover == false);

            if (bRotate == true)
            {
                rect = new RectangleF(start_x,
                    start_y,
                    this.Width,
                    this.Height);

                gstate = e.Graphics.Save();
                e.Graphics.Clip = new Region(rect);
                // Setup the transformation matrix
                Matrix x = new Matrix();
                if (this.OutofIssue == true
                    && (this.m_bFocus == true && this.m_bHover == false))
                    x.RotateAt(-35, new PointF(start_x + (this.Width / 2), start_y + (this.Height / 2)));
                else if (this.OutofIssue == true)
                    x.RotateAt(-45, new PointF(start_x + (this.Width / 2), start_y + (this.Height / 2)));
                else
                    x.RotateAt(10, new PointF(start_x + (this.Width / 2), start_y + (this.Height / 2)));
                e.Graphics.Transform = x;
            }

            // ��ͨ����
            Color colorText = this.Container.Container.SingleForeColor;
            Color colorGray = this.Container.Container.SingleGrayColor;
            Brush brushBack = null;
            float fBorderWidth = 1; // 2
            Color colorBorder = Color.FromArgb(255, Color.Gray);


            // ����
            if (bSelected == true)
            {
                // ѡ���˵ĸ���
                Color colorBack = this.Container.Container.SelectedBackColor;
                if (this.m_bFocus == true)
                {
                    // �� -- ��
                    brushBack = new LinearGradientBrush(
        new PointF(start_x, start_y + this.Height),
        new PointF(start_x + this.Width, start_y),
        Color.FromArgb(120, colorBack),
        Color.FromArgb(255, ControlPaint.Dark(colorBack))   // 0-150
        );
                }
                else
                {
                    brushBack = new SolidBrush(colorBack);
                }
                colorText = this.Container.Container.SelectedForeColor;
                colorGray = this.Container.Container.SelectedGrayColor;
            }
            else if (this.IsMember == true)
            {
                // ��Ա��
#if DEBUG
                if (this.item != null)
                {
                    Debug.Assert(this.item.IsMember == true, "");
                }
#endif
                fBorderWidth = 1;
                Color colorBack = this.Container.Container.MemberBackColor;
                if (this.m_bFocus == true)
                {
                    // �� -- ��
                    brushBack = new LinearGradientBrush(
        new PointF(start_x, start_y + this.Height),
        new PointF(start_x + this.Width, start_y),
        Color.FromArgb(0, colorBack),
        Color.FromArgb(150, ControlPaint.Dark(colorBack))
        );
                }
                else
                {
                    brushBack = new SolidBrush(colorBack);
                }

                colorText = this.Container.Container.MemberForeColor;
                colorGray = this.Container.Container.MemberGrayColor;
            }
            else if (this.item != null
                && this.item.Calculated == true)
            {
                // Ԥ��ĵ���
                fBorderWidth = (float)1; 
                Color colorBack = this.Container.Container.CalculatedBackColor;
                if (this.m_bFocus == true)
                {
                    // �� -- ��
                    brushBack = new LinearGradientBrush(
        new PointF(start_x, start_y + this.Height),
        new PointF(start_x + this.Width, start_y),
        Color.FromArgb(0, colorBack),
        Color.FromArgb(150, ControlPaint.Dark(colorBack))
        );
                }
                else
                {
                    brushBack = new SolidBrush(colorBack);
                }
                colorText = this.Container.Container.CalculatedForeColor;
                colorGray = this.Container.Container.CalculatedGrayColor;
                colorBorder = Color.FromArgb(255, Color.White);
            }
            else if (this.item != null
           && this.item.IsParent == true)
            {
                // �϶���
                fBorderWidth = 1;   // 3
                Color colorBack = this.Container.Container.ParentBackColor;
                if (this.m_bFocus == true)
                {
                    // �� -- ��
                    brushBack = new LinearGradientBrush(
        new PointF(start_x, start_y + this.Height),
        new PointF(start_x + this.Width, start_y),
        Color.FromArgb(0, colorBack),
        Color.FromArgb(150, ControlPaint.Dark(colorBack))
        );
                }
                else
                {
                    brushBack = new SolidBrush(colorBack);
                }

                colorText = this.Container.Container.ParentForeColor;
                colorGray = this.Container.Container.ParentGrayColor;
            }
            else
            {
                brushBack = null;
                Color colorBack = this.Container.Container.SingleBackColor;
                if (this.m_bFocus == true)
                {
                    // �� -- ��
                    brushBack = new LinearGradientBrush(
        new PointF(start_x, start_y + this.Height),
        new PointF(start_x + this.Width, start_y),
        Color.FromArgb(0, colorBack),   // 0
        Color.FromArgb(100, ControlPaint.Dark(colorBack))   // 255
        );
                }
                else
                {
                    brushBack = new SolidBrush(colorBack);
                }
            }

            Color colorSideBar = Color.FromArgb(0, 255, 255, 255);

            // �½��ĺͷ������޸ĵģ��������ɫ��Ҫ�趨
            if (this.item != null
                && this.item.NewCreated == true)
            {
                // �´����ĵ���
                colorSideBar = this.Container.Container.NewBarColor;
            }
            else if (this.item != null
           && this.item.Changed == true)
            {
                // �޸Ĺ��ĵĵ���
                colorSideBar = this.Container.Container.ChangedBarColor;
            }

            // �߿�ͱ���
            {

                rect = new RectangleF(start_x,
                    start_y,
                    this.Container.Container.m_nCellWidth,
                    this.Container.Container.m_nCellHeight);

                {
                    // û�н���ʱҪСһЩ
                    rect = GuiUtil.PaddingRect(this.Container.Container.CellMargin,
                        rect);
                }
                // rect = RectangleF.Inflate(rect, -4, -4);

                RectangleF rectTest = rect;
                rectTest.Inflate(1, 1);
                rectTest.Width += 2;
                rectTest.Height += 2;

                // �Ż�
                if (rectTest.IntersectsWith(e.ClipRectangle) == true
                    || bRotate == true)
                {
                    float round_radius = 10;

                    // ��Ӱ
                    RectangleF rectShadow = rect;
                    rectShadow.Offset((float)1.5, (float)1.5);  // 0.5
                    // rectShadow.Inflate((float)-0.9, (float)-0.9);
                    // Pen penShadow = new Pen(Color.FromArgb(160, 190,190,180),5);
                    Brush brushShadow = new SolidBrush(Color.FromArgb(160, 190, 190, 180));
                    BindingControl.RoundRectangle(e.Graphics,
                        null,
                        brushShadow,
                        rectShadow,
                        round_radius);

                    float que_radius = 0;
                    if (this.item != null)
                    {
                        string strIntact = this.item.GetText("intact");
                        float r = GetIntactRatio(strIntact);
                        if (r < (float)1.0)
                        {
                            float h = Math.Min(rect.Width, rect.Height) - (round_radius + fBorderWidth);
                            que_radius = h - (h * r);
                            if (que_radius < round_radius)
                                que_radius = round_radius;
                        }
                    }


                    rect.Inflate(-(fBorderWidth / 2), -(fBorderWidth / 2));
                    Pen penBorder = new Pen(colorBorder, fBorderWidth);
                    penBorder.LineJoin = LineJoin.Bevel;
                    if (que_radius == 0)
                        BindingControl.RoundRectangle(e.Graphics,
                            penBorder,
                            brushBack,
                            rect,
                            round_radius);
                    else
                        BindingControl.QueRoundRectangle(e.Graphics,
                            penBorder,
                            brushBack,
                            rect,
                            round_radius,
                            que_radius);

                    // ���������
                    Brush brushSideBar = new SolidBrush(colorSideBar);
                    RectangleF rectSideBar = new RectangleF(
                        rect.X + penBorder.Width,
                        rect.Y + 10,
                        10/2,
                        rect.Height - 2*10);
                    e.Graphics.FillRectangle(brushSideBar, rectSideBar);
                }
            }

            // ��������
            if (this.item != null)
            {
                Debug.Assert(this.item.Missing == false, "MissingΪtrue��Item����Ӧ���ڳ�ʼ���ս���ʱ�Ͷ���");

                // ����״̬�ĵ�Ԫ
                if (this.item.Locked == true && this.IsMember == false)
                {
                    Padding margin = this.Container.Container.CellMargin;
                    Padding padding = this.Container.Container.CellPadding;

                    float nLittleWidth = Math.Min(this.Width - margin.Horizontal - padding.Horizontal,
                        this.Height - margin.Vertical - padding.Vertical);

                    RectangleF rectMask = new RectangleF(
                        //start_x + this.Width / 2 - nLittleWidth / 2,
                        //start_y + this.Height / 2 - nLittleWidth / 2,
                        start_x + this.Width / 2 - nLittleWidth / 2,
                        start_y + this.Height- nLittleWidth,
                        nLittleWidth,
                        nLittleWidth);
                    PaintLockedMask(rectMask,
                        this.item.Calculated == true ? ControlPaint.Dark(colorGray) : colorGray,
                        e,
                        bRotate);
                }

                if (this.item.Calculated == true)
                {
                    // ���Ƶ�ɫ�ġ�?������
                    this.PaintQue(start_x,
                        start_y,
                        "?",
                        colorGray,
                        e,
                        bRotate);
                }
                // �����ݿ��¼���Ѿ�ɾ���ĵ�Ԫ
                if (this.item.Deleted == true)
                {
                    /*
                    float nCenterX = start_x + (this.Container.Container.m_nCellWidth / 2);
                    float nCenterY = start_y + (this.Container.Container.m_nCellHeight / 2);
                    float nWidth = Math.Min(this.Container.Container.m_nCellWidth,
                        this.Container.Container.m_nCellHeight);
                    nWidth = nWidth * (float)0.6;
                    rect = new RectangleF(nCenterX - nWidth/2,
    nCenterY - nWidth/2,
    nWidth,
    nWidth);
                    rect = PaddingRect(this.Container.Container.CellMargin,
                        rect);
                    rect = RectangleF.Inflate(rect, -4, -4);

                    Pen pen = new Pen(Color.LightGray,
                        nWidth/10);
                    e.Graphics.DrawArc(pen, rect, 0, 360);
                    e.Graphics.DrawLine(pen, new PointF(rect.X, rect.Y + rect.Height / 2),
                    new PointF(rect.X + rect.Width, rect.Y + rect.Height / 2));
                     * */
                    Padding margin = this.Container.Container.CellMargin;
                    Padding padding = this.Container.Container.CellPadding;

                    float nLittleWidth = Math.Min(this.Width - margin.Horizontal - padding.Horizontal,
                        this.Height - margin.Vertical - padding.Vertical);

                    RectangleF rectMask = new RectangleF(
                        start_x + this.Width/2 - nLittleWidth/2,
                        start_y + this.Height/2 - nLittleWidth/2,
                        nLittleWidth,
                        nLittleWidth);
                    PaintDeletedMask(rectMask,
                        colorGray,
                        e,
                        bRotate);
                }

                // ���˽��ĵĸ���
                if (String.IsNullOrEmpty(this.item.Borrower) == false)
                {
                    Padding margin = this.Container.Container.CellMargin;
                    Padding padding = this.Container.Container.CellPadding;

                    float nLittleWidth = Math.Min(this.Width - margin.Horizontal - padding.Horizontal,
                        this.Height - margin.Vertical - padding.Vertical);

                    RectangleF rectMask = new RectangleF(
                        start_x + this.Width / 2 - nLittleWidth / 2,
                        start_y + this.Height / 2 - nLittleWidth / 2,
                        nLittleWidth,
                        nLittleWidth);
                    PaintBorrowedMask(rectMask,
                        colorGray,
                        e,
                        bRotate);
                }

                if (StringUtil.IsInList("ע��", this.item.State) == true)
                {
                    this.PaintTextLines(start_x, start_y, true,
                        colorText,
                        e, bRotate);
                }
                else
                {
                    this.PaintTextLines(start_x, start_y, false,
                        colorText,
                        e, bRotate);
                }

                if (this.m_bHover == true
                    && this.m_bDisplayCheckBox == false)    // Ҫ��ʾcheckbox���Ͳ�Ҫ��ʾ�б���
                    this.PaintLineLabels(start_x, start_y, e, bRotate);

                // ���˲ɹ���Ϣ�ĸ��ӣ���ʾxyֵ
                if (this.Container.Container.DisplayOrderInfoXY == true
                    && this.item != null && this.item.OrderInfoPosition.X != -1)
                {
                    string strText = this.item.OrderInfoPosition.X.ToString() + "," + this.item.OrderInfoPosition.Y.ToString();

                    Font font = this.Container.Container.m_fontLine;
                    SizeF size = e.Graphics.MeasureString(strText, font);

                    rect = new RectangleF(start_x,
                        start_y,
                        this.Width,
                        this.Height);
                    rect = GuiUtil.PaddingRect(this.Container.Container.CellMargin,
                        rect);
                    rect = GuiUtil.PaddingRect(this.Container.Container.CellPadding,
                        rect);

                    // ���Ͻ�
                    RectangleF rectText = new RectangleF(
                        rect.X + rect.Width - size.Width,
                        rect.Y,
                        size.Width,
                        size.Height);

                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Near;
                    stringFormat.LineAlignment = StringAlignment.Near;
                    stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                    Brush brushText = new SolidBrush(this.Container.Container.ForeColor);
                    e.Graphics.DrawString(strText,
                        font,
                        brushText,
                        rectText,
                        stringFormat);
                }
            }
            else
            {
                /*
                // �հ׸��ӡ����Ƶ�ɫ�ġ�ȱ������
                this.PaintQue(start_x,
                    start_y,
                    "ȱ",
                    colorGray,
                    e,
                    bRotate);
                 * */
                {
                    Padding margin = this.Container.Container.CellMargin;
                    Padding padding = this.Container.Container.CellPadding;

                    float nLittleWidth = Math.Min(this.Width - margin.Horizontal - padding.Horizontal,
                        this.Height - margin.Vertical - padding.Vertical);

                    RectangleF rectMask = new RectangleF(
                        start_x + this.Width / 2 - nLittleWidth / 2,
                        start_y + this.Height / 2 - nLittleWidth / 2,
                        nLittleWidth,
                        nLittleWidth);
                    PaintMissingMask(rectMask,
                        colorGray,
                        e,
                        bRotate);
                }

#if NOOOOOOOOOOOOOOO
                rect = new RectangleF(start_x,
                    start_y,
                    this.Container.Container.m_nCellWidth,
                    this.Container.Container.m_nCellHeight);
                rect = PaddingRect(this.Container.Container.CellMargin,
                    rect);
                float nWidthDelta = (rect.Width / 4);
                float nHeightDelta = (rect.Height / 4);
                rect = RectangleF.Inflate(rect, -nWidthDelta, -nHeightDelta);

                // �Ż�
                if (rect.IntersectsWith(e.ClipRectangle) == true)
                {
                    /*
                    GraphicsState gstate = e.Graphics.Save();
                    e.Graphics.TranslateTransform(rect.X+(rect.Width/2), rect.Y+(rect.Height/2));
                    e.Graphics.RotateTransform(-10, MatrixOrder.Append);
                     * */

                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Center;
                    stringFormat.LineAlignment = StringAlignment.Center;
                    stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                    /*
                    rect.X = rect.Width / 2;
                    rect.Y = rect.Height / 2;
                     * */
                    e.Graphics.DrawString("ȱ",
    new Font("΢���ź�",    // "Arial",
        rect.Height,
        FontStyle.Regular,
        GraphicsUnit.Pixel),
    new SolidBrush(Color.LightGray),
    rect,
    stringFormat);


                    /*
                    e.Graphics.DrawString("ȱ",
                        new Font("΢���ź�",    // "Arial",
                            rect.Height,
                            FontStyle.Regular,
                            GraphicsUnit.Pixel),
                        new SolidBrush(Color.LightGray),
                        rect,
                        stringFormat);
                     * */
                    /*
                    e.Graphics.DrawString("ȱ",
    new Font("΢���ź�",    // "Arial",
        rect.Height,
        FontStyle.Regular,
        GraphicsUnit.Pixel),
    new SolidBrush(Color.LightGray),
    0,
    0);
                     * */
                    /// e.Graphics.Restore(gstate);

                }
#endif
            }


            // ��������
            if (this.m_bFocus == true)
            {
                rect = new RectangleF(
    start_x,
    start_y,
    this.Width,
    this.Height);
                rect.Inflate(-1, -1);
                /*
                 rect = PaddingRect(this.Container.Container.CellMargin,
     rect);

                 rect.Inflate(-4, -4);
                  * */
                ControlPaint.DrawFocusRectangle(e.Graphics,
                    Rectangle.Round(rect));
            }

            if (gstate != null)
            {
                Debug.Assert(bRotate == true);
                e.Graphics.Restore(gstate);
            }

            // �ƶ����֣���Ҫ��ת����Ϊ��ת���������Ĳ�һ��
            if (this.m_bHover == true)
            {
                // ����
                Rectangle rect1 = this.Container.Container.RectGrab;
                rect1.Offset((int)start_x, (int)start_y);
                ControlPaint.DrawContainerGrabHandle(
        e.Graphics,
        rect1);

                // checkbox
                if (this.item != null
                    && this.m_bDisplayCheckBox == true)
                {
                    long nCenterX = start_x + this.Width / 2;
                    long nCenterY = start_y + this.Height / 2;
                    int nWidth = this.Container.Container.RectGrab.Width;
                    Rectangle rectCheckBox = new Rectangle(
                        (int)nCenterX - nWidth/2,
                        (int)nCenterY - nWidth/2,
                        nWidth,
                        nWidth);

                    // ��ʾ��͸��������
                    RectangleF rectShadow = rectCheckBox;
                    int nDelta = this.Width / 8;
                    rectShadow.Inflate(nDelta, nDelta);
                    BindingControl.Circle(e.Graphics,
                        new Pen(Color.FromArgb(100, 200, 200, 200), 2),
                        new SolidBrush(Color.FromArgb(150, 200, 200, 200)),
                        rectShadow);

                    if (this.item.Calculated == true)
                    {
                        ControlPaint.DrawCheckBox(e.Graphics,
                            rectCheckBox,
                            ButtonState.Normal);
                    }
                    else if (this.item.OrderInfoPosition.X != -1
                        && this.item.NewCreated == true)
                    {
                        ControlPaint.DrawCheckBox(e.Graphics,
                            rectCheckBox,
                            ButtonState.Checked);
                    }
                }
            }
        }

        // ���ơ�ȱ�ڡ���־
        internal static void PaintMissingMask(RectangleF rect,
            Color colorGray,
            PaintEventArgs e,
            bool bDoNotSpeedUp)
        {
            float delta = (rect.Width / 2) * (float)0.35;
            rect.Inflate(-delta, -delta);
            // �Ż�
            if (rect.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
            {
                Pen pen = new Pen(colorGray,
                    rect.Width / 10);
                rect.Inflate(-pen.Width / 2, -pen.Width / 2);

                float start = 0;
                for (int i = 0; i < 10; i++)
                {
                    e.Graphics.DrawArc(pen, rect, start, 18);
                    start += 36;
                }
            }
        }

        // ���ơ����˽��ġ���־
        internal static void PaintBorrowedMask(RectangleF rect,
            Color colorGray,
            PaintEventArgs e,
            bool bDoNotSpeedUp)
        {
            float delta = (rect.Width / 2) * (float)0.35;
            rect.Inflate(-delta, -delta);
            // �Ż�
            if (rect.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
            {
                Pen pen = new Pen(colorGray,
                    rect.Width / 10);
                pen.LineJoin = LineJoin.Bevel;

                rect.Inflate(-pen.Width / 2, -pen.Width / 2);

                float up_height = rect.Width / 2;
                float down_height = up_height;

                GraphicsPath path = new GraphicsPath();

                // ͷ��
                RectangleF rectUp = new RectangleF(rect.X + rect.Width / 2 - up_height / 2,
                    rect.Y, up_height, up_height);
                path.AddArc(rectUp, 0, 360);

                // ����
                RectangleF rectDown = new RectangleF(rect.X,
                    rect.Y + up_height, rect.Width, down_height*2);
                path.AddArc(rectDown, 180, 180);

                e.Graphics.DrawPath(pen, path);
                path.Dispose();
            }
        }

        // ���ơ���¼��ɾ������־
        internal static void PaintLockedMask(RectangleF rect,
            Color colorGray,
            PaintEventArgs e,
            bool bDoNotSpeedUp)
        {
            float delta = (rect.Width / 2) * (float)0.35;
            rect.Inflate(-delta, -delta);
            // �Ż�
            if (rect.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
            {
                Pen pen = new Pen(colorGray,
                    rect.Width / 10);
                pen.LineJoin = LineJoin.Bevel;

                rect.Inflate(-pen.Width / 2, -pen.Width / 2);
                float little = rect.Width / 7;

                RectangleF rectBox = new RectangleF(rect.X + little, rect.Y + rect.Height / 2, rect.Width - little * 2, rect.Height / 2);

                GraphicsPath path = new GraphicsPath();

                RectangleF rectArc = new RectangleF(rect.X + rect.Width / 4, rect.Y, rect.Width / 2, rect.Height / 2);
                // �������
                path.AddLine(rect.X + rect.Width / 4, rect.Y + rect.Height / 2,
                    rect.X + rect.Width / 4, rect.Y + +rect.Height / 4);
                // ��Բ��
                path.AddArc(rectArc, 180, 180);
                // �ұ�����
                path.AddLine(rect.X + rect.Width - rect.Width / 4,rect.Y + +rect.Height / 4 ,
                    rect.X + rect.Width - rect.Width / 4, rect.Y + rect.Height / 2);
                // 
                path.AddRectangle(rectBox);

                e.Graphics.DrawPath(pen, path);
                path.Dispose();

            }
        }

        // ���ơ���¼��ɾ������־
        internal static void PaintDeletedMask(RectangleF rect,
            Color colorGray,
            PaintEventArgs e,
            bool bDoNotSpeedUp)
        {
            float delta = (rect.Width / 2) * (float)0.35;
            rect.Inflate(-delta, -delta);
            // �Ż�
            if (rect.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
            {
                Pen pen = new Pen(colorGray,
                    rect.Width / 10);
                pen.LineJoin = LineJoin.Bevel;

                rect.Inflate(-pen.Width / 2, -pen.Width / 2);
                float little = rect.Width / 7;

                GraphicsPath path = new GraphicsPath();

                path.AddArc(rect, 20, 360 - 20);

                path.AddLine(
                    new PointF(rect.X + rect.Width - little - little/2, rect.Y + rect.Height / 2 - little),
    new PointF(rect.X + rect.Width, rect.Y + rect.Height / 2));
                path.AddLine(
                    new PointF(rect.X + rect.Width, rect.Y + rect.Height / 2),
new PointF(rect.X + rect.Width + little, rect.Y + rect.Height / 2 - little - little/2));

                e.Graphics.DrawPath(pen, path);
                path.Dispose();

                /*
                e.Graphics.DrawArc(pen, rect, 20, 360 - 20);
                float little = rect.Width/7;
                e.Graphics.DrawLine(pen,
    new PointF(rect.X + rect.Width - little, rect.Y + rect.Height / 2 - little),
    new PointF(rect.X + rect.Width, rect.Y + rect.Height / 2));
                e.Graphics.DrawLine(pen,
new PointF(rect.X + rect.Width, rect.Y + rect.Height / 2),
new PointF(rect.X + rect.Width + little, rect.Y + rect.Height / 2 - little));
                */
            }
        }

        /*
        // ���ơ���¼��ɾ������־
        internal static void PaintDeletedMask(RectangleF rect,
            Color colorGray,
            PaintEventArgs e,
            bool bDoNotSpeedUp)
        {
            float delta = (rect.Width/2) * (float)0.35;
            rect.Inflate(-delta, -delta);
            // �Ż�
            if (rect.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
            {
                Pen pen = new Pen(colorGray,
                    rect.Width / 10);
                rect.Inflate(-pen.Width/2, -pen.Width/2);
                e.Graphics.DrawArc(pen, rect, 0, 360);
                e.Graphics.DrawLine(pen,
                    new PointF(rect.X, rect.Y + rect.Height / 2),
                    new PointF(rect.X + rect.Width, rect.Y + rect.Height / 2));
            }
        }
         * */


        internal virtual void PaintQue(float start_x,
            float start_y,
            string strText,
            Color colorGray,
            PaintEventArgs e,
            bool bDoNotSpeedUp)
        {
            Debug.Assert(String.IsNullOrEmpty(strText) == false, "");

            RectangleF rect = new RectangleF(start_x,
    start_y,
    this.Container.Container.m_nCellWidth,
    this.Container.Container.m_nCellHeight);
            rect = GuiUtil.PaddingRect(this.Container.Container.CellMargin,
                rect);
            float nWidthDelta = (rect.Width / 4);
            float nHeightDelta = (rect.Height / 4);
            rect = RectangleF.Inflate(rect, -nWidthDelta, -nHeightDelta);

            // �Ż�
            if (rect.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
            {
                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;
                stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                e.Graphics.DrawString(strText,
new Font("΢���ź�",    // "Arial",
    rect.Height,
    FontStyle.Regular,
    GraphicsUnit.Pixel),
new SolidBrush(colorGray),
rect,
stringFormat);

#if NOOOOOOOOOOOOOOO
                GraphicsState gstate = e.Graphics.Save();
                // Setup the transformation matrix
                Matrix x = new Matrix();
                // Translate to the desired co-ordinates
                // x.Translate(1, 1);

                x.RotateAt(-45, new PointF(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2)));

                e.Graphics.Transform = x;

                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;
                stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                e.Graphics.DrawString(strText,
new Font("΢���ź�",    // "Arial",
rect.Height,
FontStyle.Regular,
GraphicsUnit.Pixel),
new SolidBrush(Color.LightGray),
rect,
stringFormat);
                e.Graphics.Restore(gstate);
#endif
            }
        }


        internal virtual void PaintTextLines(float x0,
            float y0,
            bool bGrayText,
            Color colorText,
            PaintEventArgs e,
            bool bDoNotSpeedUp)
        {
            int nLineHeight = this.LineHeight;
            Padding margin = this.Container.Container.CellMargin;
            Padding padding = this.Container.Container.CellPadding;
            x0 += margin.Left + padding.Left;
            y0 += margin.Top + padding.Top;
            int nWidth = this.Width - margin.Horizontal - padding.Horizontal;
            int nHeight = this.Height - margin.Vertical - padding.Vertical;

            int nUsedHeight = 0;    // ʹ�ù����ۻ��߶�
            // Color colorText = this.Container.Container.MemberForeColor;

            if (bGrayText == true)
                colorText = ControlPaint.Light(colorText, 1.5F);

            // �� -- ��
            LinearGradientBrush brushGradient = new LinearGradientBrush(
new PointF(x0, 0),
new PointF(x0 + 6, 0),
Color.FromArgb(255, Color.Gray),
Color.FromArgb(0, Color.Gray)
);

            Font font = this.Container.Container.m_fontLine;

            Pen penLine = new Pen(brushGradient, (float)1);
            for (int i = 0; i < this.Container.Container.TextLineNames.Length / 2; i++)
            {
                int nRestHeight = nHeight - nUsedHeight;

                // ������
                RectangleF rect = new RectangleF(
                    x0,
                    y0,
                    nWidth,
                    Math.Min(this.LineHeight, nRestHeight));

                // �Ż�
                if (rect.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
                {
                    string strName = this.Container.Container.TextLineNames[i * 2];

                    string strText = this.item.GetText(strName);
                    if (strName == "intact")
                    {
                        // Ԥ����ӣ�û�б�Ҫ��ʾ�����
                        if (this.item.Calculated == false)
                        {
                            PaintIntactBar(
                            x0,
                            y0,
                            nWidth,
                            Math.Min(nLineHeight, nRestHeight),
                            strText,
                            colorText,
                            e);
                        }
                    }
                    else
                    {
                        PaintText(
                            x0,
                            y0,
                            nWidth,
                            Math.Min(nLineHeight, nRestHeight),
                            strText,
                            colorText,
                            font,
                            e);
                    }

                    // �·�����
                    if (nLineHeight < nRestHeight)
                    {
                        e.Graphics.DrawLine(penLine,
                            new PointF(rect.X, rect.Y + nLineHeight - 1),
                            new PointF(rect.X + 5, rect.Y + nLineHeight - 1));
                    }
                }


                y0 += nLineHeight;
                nUsedHeight += nLineHeight;

                if (nUsedHeight > nHeight)
                    break;
            }
        }

        static float GetIntactRatio(string strIntact)
        {
            if (String.IsNullOrEmpty(strIntact) == true)
                return (float)1.0;

            strIntact = strIntact.Replace("%", "");

            float r = (float)1.0;

            try
            {
                r = (float)Convert.ToDecimal(strIntact) / (float)100;
            }
            catch
            {
                return 0;
            }

            if (r > 1.0)
                r = (float)1.0;
            if (r < 0)
                r = 0;

            return r;
        }

        internal virtual void PaintIntactBar(float x0,
            float y0,
            int nWidth,
            int nHeight,
            string strIntact,
            Color colorText,
            PaintEventArgs e)
        {
            if (String.IsNullOrEmpty(strIntact) == true)
                strIntact = "100";
            else
                strIntact = strIntact.Replace("%", "");

            float r = (float)1.0;

            try
            {
                r = (float)Convert.ToDecimal(strIntact) / (float)100;
            }
            catch
            {
                r = 0;
                strIntact = "error '" + strIntact + "'";
            }

            if (r > 1.0)
                r = (float)1.0;
            if (r < 0)
                r = 0;

            int nLeftWidth = (int)((float)nWidth * r);
            if (nLeftWidth > 0)
            {
                // �� -- ��
                LinearGradientBrush brushGradient = new LinearGradientBrush(
    new PointF(x0, y0),
    new PointF(x0 + nLeftWidth, y0 + nHeight),
    Color.FromArgb(100, Color.Gray),
    Color.FromArgb(255, Color.Gray)
    );

                //  Brush brushLeft = new SolidBrush(Color.Gray);
                RectangleF rectLeft = new RectangleF(x0, y0, nLeftWidth, nHeight);
                e.Graphics.FillRectangle(brushGradient,
                    rectLeft);
            }

            int nRightWidth = nWidth - nLeftWidth;
            if (nRightWidth > 0)
            {
                Brush brushRight = new SolidBrush(Color.FromArgb(100, Color.LightGray));
                RectangleF rectRight = new RectangleF(x0 + nLeftWidth, y0, nRightWidth, nHeight);
                e.Graphics.FillRectangle(brushRight,
                    rectRight);
            }

            // ��ɫ������
            PaintText(
x0,
y0,
nWidth,
nHeight,
strIntact,
Color.White,
           new Font(this.Container.Container.m_fontLine, FontStyle.Bold),

e);
            /*
            PaintText(
    x0,
    y0,
    nWidth,
    nHeight,
    strIntact,
    colorText,
               this.Container.Container.m_fontLine,
    e);
             * */
        }

        internal virtual void PaintLineLabels(float x0,
            float y0,
            PaintEventArgs e,
            bool bDoNotSpeedUp)
        {
            int nLineHeight = this.LineHeight;

            Padding margin = this.Container.Container.CellMargin;
            Padding padding = this.Container.Container.CellPadding;
            x0 += margin.Left + padding.Left;
            y0 += margin.Top + padding.Top;
            int nWidth = this.Width - margin.Horizontal - padding.Horizontal;
            int nHeight = this.Height - margin.Vertical - padding.Vertical;

            int nUsedHeight = 0;    // ʹ�ù����ۻ��߶�
            Color colorText = Color.FromArgb(200, 0, 100, 0);

            Font font = this.Container.Container.m_fontLine;
            font = new Font(font, FontStyle.Bold);

            // ������ֵ������
            float fMaxTextWidth = 0;
            for (int i = 0; i < this.Container.Container.TextLineNames.Length / 2; i++)
            {
                string strLabel = this.Container.Container.TextLineNames[i * 2 + 1];
                SizeF size = e.Graphics.MeasureString(strLabel, font);
                if (size.Width > fMaxTextWidth)
                    fMaxTextWidth = size.Width;
            }

            // ���ư�͸������
            {
                RectangleF rect1 = new RectangleF(
                    x0 + nWidth - (fMaxTextWidth + 4),
                    y0,
                    fMaxTextWidth + 4,
                    nHeight);

                if (rect1.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
                {

                    // �� -- ��
                    LinearGradientBrush brushGradient = new LinearGradientBrush(
    new PointF(rect1.X, rect1.Y),
    new PointF(rect1.X + rect1.Width, rect1.Y),
    Color.FromArgb(150, Color.White),
    Color.FromArgb(200, Color.White)
    );

                    e.Graphics.FillRectangle(brushGradient,
                        rect1);
                }

            }

            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Far;
            stringFormat.LineAlignment = StringAlignment.Near;
            stringFormat.FormatFlags = StringFormatFlags.NoWrap;

            Brush brushText = new SolidBrush(colorText);
            Pen penLine = new Pen(colorText, (float)1);
            for (int i = 0; i < this.Container.Container.TextLineNames.Length / 2; i++)
            {
                int nRestHeight = nHeight - nUsedHeight;

                // ������
                RectangleF rect = new RectangleF(
                    x0 + nWidth - fMaxTextWidth,
                    y0,
                    fMaxTextWidth,
                    Math.Min(nLineHeight, nRestHeight));

                // �Ż�
                if (rect.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
                {
                    /*
                    PaintText(
                        x0,
                        y0,
                        nWidth,
                        Math.Min(this.LineHeight, nRestHeight),
                        strLabel,
                        colorText,
                        stringFormat,
                        e);
                     * */
                    string strLabel = this.Container.Container.TextLineNames[i * 2 + 1];


                    e.Graphics.DrawString(strLabel,
                        font,
                        brushText,
                        rect,
                        stringFormat);

                    // �·�����
                    if (nLineHeight < nRestHeight)
                    {
                        e.Graphics.DrawLine(penLine,
                            new PointF(rect.X, rect.Y + nLineHeight - 1),
                            new PointF(rect.X + rect.Width, rect.Y + nLineHeight - 1));
                    }
                }


                y0 += nLineHeight;
                nUsedHeight += nLineHeight;

                if (nUsedHeight > nHeight)
                    break;
            }
        }



        /*
        //
        void PaintText(
    int x0,
    int y0,
    int nMaxWidth,
    int nHeight,
    string strText,
            Font font,
            Color colorText,
            StringFormat stringFormat,
            PaintEventArgs e)
        {
            Brush brushText = null;

            brushText = new SolidBrush(colorText);
            SizeF size = e.Graphics.MeasureString(strText, font);
            RectangleF rect = new RectangleF(
                x0,
                y0,
                Math.Min(size.Width, nMaxWidth),
                Math.Min(nHeight, size.Height));

            e.Graphics.DrawString(strText,
                font,
                brushText,
                rect,
                stringFormat);
        }*/

        void PaintText(
            RectangleF rect,
            string strText,
            Color colorText,
            Font font,
            StringFormat stringFormat,
            PaintEventArgs e)
        {
            // Pen penBold = new Pen(Color.FromArgb(50, Color.Gray), (float)2);

            /*
            int nFontHeight = this.LineHeight - 4;   // Math.Min(nWidth, nHeight / 5);

            Font font = new Font("΢���ź�",    // "Arial",
                nFontHeight,
                FontStyle.Regular,
                GraphicsUnit.Pixel);
             * */

            // Font font = this.Container.Container.m_fontLine;
            Brush brushText = null;

            brushText = new SolidBrush(colorText);
            SizeF size = e.Graphics.MeasureString(strText, font);
            /*
            RectangleF rect = new RectangleF(
                x0,
                y0,
                Math.Min(size.Width, nWidth),
                Math.Min(size.Height, nHeight));
             * */

            e.Graphics.DrawString(strText,
                font,
                brushText,
                rect,
                stringFormat);
        }

        internal void PaintText(
            float x0,
            float y0,
            int nMaxWidth,
            int nHeight,
            string strText,
            Color colorText,
            Font font,
            PaintEventArgs e)
        {
            // Pen penBold = new Pen(Color.FromArgb(50, Color.Gray), (float)2);

            /*
            int nFontHeight = this.LineHeight - 4;   // Math.Min(nWidth, nHeight / 5);

            Font font = new Font("΢���ź�",    // "Arial",
                nFontHeight,
                FontStyle.Regular,
                GraphicsUnit.Pixel);
             * */

            Brush brushText = null;

            brushText = new SolidBrush(colorText);
            SizeF size = e.Graphics.MeasureString(strText, font);
            RectangleF rect = new RectangleF(
                x0,
                y0,
                Math.Min(size.Width, nMaxWidth),
                Math.Min(nHeight, size.Height));

            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;
            stringFormat.FormatFlags = StringFormatFlags.NoWrap;

            e.Graphics.DrawString(strText,
                font,
                brushText,
                rect,
                stringFormat);
        }

    }

    // �Ƚϳ������ڡ�С����ǰ
    internal class CellPublishTimeComparer : IComparer<Cell>
    {
        int IComparer<Cell>.Compare(Cell x, Cell y)
        {
            string s1 = x.Container.PublishTime;
            string s2 = y.Container.PublishTime;

            int nRet = String.Compare(s1, s2);
            if (nRet == 0)
            {
                // �������������ͬ����ѿհ׸�������ǰ��
                // �������ĺô��ǣ��ÿհ׸����ȴ����������Ǳ����������ͬ�ڸ��Ӹ���
                if (x.item == null && y.item == null)
                    return 0;
                if (x.item == null)
                    return -1;
                return 1;
            }

            return nRet;
        }
    }


    // һ����������ʾ��Ԫ
    // ��ʾ������Ϣ�����̡��ʽ���Դ���۸�ʱ�䷶Χ
    internal class GroupCell : Cell
    {
        public OrderBindingItem order = null;

        public bool EndBracket = false; // == false �����Ķ��������ţ�==true���ұߵ�����

        // ������ͬ�����ͷ������
        public GroupCell HeadGroupCell
        {
            get
            {
                Debug.Assert(this.EndBracket == true, "ֻ�ܶ�β������ʹ��HeadGroupCell");
                IssueBindingItem issue = this.Container;
                Debug.Assert(issue != null, "");
                int index = issue.IndexOfCell(this);
                Debug.Assert(index != -1, "");
                for (int i = index-1; i >= 0; i--)
                {
                    Cell cell = issue.Cells[i];
                    if (cell == null)
                    {
                        Debug.Assert(false, "");
                        continue;
                    }

                    if (cell is GroupCell)
                    {
                        Debug.Assert(((GroupCell)cell).EndBracket == false, "");
                        return (GroupCell)cell;
                    }
                }

                return null;
            }
        }

        public List<Cell> MemberCells
        {
            get
            {
                Debug.Assert(this.EndBracket == false, "ֻ�ܶ�ͷ������ʹ��MemberCells");
                return GetMemberCells(0x03);
            }
        }

        // parameters:
        //      0x01    Ԥ���
        //      0x02    �Ѿ�����
        //      0x03    ȫ��
        List<Cell> GetMemberCells(int nStyle)
        {
            Debug.Assert(this.EndBracket == false, "ֻ�ܶ�ͷ������ʹ��GetMemberCells()");
            List<Cell> results = new List<Cell>();
            IssueBindingItem issue = this.Container;
            if (issue == null)
            {
                Debug.Assert(issue != null, "");
                return results;
            }
            int index = issue.IndexOfCell(this);
            if (index == -1)
            {
                Debug.Assert(false, "");
                return results;
            }
            for (int i = index + 1; i < issue.Cells.Count; i++)
            {
                Cell cell = issue.GetCell(i);
                if (cell is GroupCell)
                    break;
                if ((nStyle & 0x01) != 0)
                {
                    if (cell != null && cell.item != null
        && cell.item.Calculated == true)
                    {
                        results.Add(cell);
                        continue;
                    }
                }
                if ((nStyle & 0x02) != 0)
                {
                    if (cell != null && cell.item != null
        && cell.item.Calculated == false)
                    {
                        results.Add(cell);
                        continue;
                    }
                }
            }

            return results;
        }

        // ˢ������ÿ�����ӵ�OrderInfoXY��Ϣ
        internal void RefreshGroupMembersOrderInfo(int nOrderCountDelta,
            int nArrivedCountDelta)
        {
            string strError = "";
            Debug.Assert(this.EndBracket == false, "ֻ�ܶ�ͷ������ʹ��RefreshOrderInfoXY()");
            IssueBindingItem issue = this.Container;
            if (issue == null)
            {
                Debug.Assert(issue != null, "");
                strError = "this.Container == null";
                throw new Exception(strError);
            }

            int head_index = issue.IndexOfCell(this);
            if (head_index == -1)
            {
                Debug.Assert(false, "");
                strError = "��������.Cells������û���ҵ��Լ�";
                throw new Exception(strError);
            }

            int nOrderIndex = issue.OrderItems.IndexOf(this.order);
            Debug.Assert(nOrderIndex != -1, "");

            // ��ͬһ����ȫ�����ӵĶ�����Ϣ��λ����ˢ��
            int y = 0;
            for (int i = head_index + 1; i < issue.Cells.Count; i++)
            {
                Cell cell = issue.GetCell(i);
                if (cell is GroupCell)
                    break;
                Debug.Assert(cell != null, "");
                Debug.Assert(cell.item != null, "");
                if (cell.item != null)
                {
                    cell.item.OrderInfoPosition = new Point(nOrderIndex, y);
                }
                y++;
            }
            bool bChanged = false;

            if (nOrderCountDelta != 0 || nArrivedCountDelta != 0)
            {
                string strNewValue = "";
                string strOldValue = "";
                OrderDesignControl.ParseOldNewValue(this.order.Copy,
                    out strOldValue,
                    out strNewValue);
                int nOldCopy = IssueBindingItem.GetNumberValue(strOldValue);
                int nNewCopy = IssueBindingItem.GetNumberValue(strNewValue);
                nOldCopy += nOrderCountDelta;
                // 2010/4/13
                if (nOldCopy < 0)
                    nOldCopy = 0;
                Debug.Assert(nOldCopy >= 0, "");
                nNewCopy += nArrivedCountDelta;
                // 2010/4/13
                if (nNewCopy < 0)
                    nNewCopy = 0;
                Debug.Assert(nNewCopy >= 0, "");
                this.order.Copy = OrderDesignControl.LinkOldNewValue(nOldCopy.ToString(),
                     nNewCopy.ToString());
                bChanged = true;
            }

            if (this.order.UpdateDistributeString(this) == true)
                bChanged = true;
            if (bChanged == true)
                issue.AfterMembersChanged();
        }

                // �����ڲ����µĸ���(Ԥ�����)
        // parameters:
        //      nInsertPos  ����λ�á����Ϊ-1����ʾ������β��
        // return:
        //      ���ز����index(����issue.Cells�±�)
        internal int InsertNewMemberCell(
            int nInsertPos,
            out string strError)
        {
            Debug.Assert(this.EndBracket == false, "ֻ�ܶ�ͷ������ʹ��InsertGroupMemberCell()");
            IssueBindingItem issue = this.Container;
            if (issue == null)
            {
                Debug.Assert(issue != null, "");
                strError = "this.Container == null";
                return -1;
            }
            int head_index = issue.IndexOfCell(this);
            if (head_index == -1)
            {
                Debug.Assert(false, "");
                strError = "��������.Cells������û���ҵ��Լ�";
                return -1;
            }

            int nStartIndex = -1;
            for (int i = head_index + 1; i < issue.Cells.Count; i++)
            {
                Cell cell = issue.GetCell(i);
                if (cell is GroupCell)
                {
                    if (nInsertPos == -1)
                    {
                        nStartIndex = i;
                        break;
                    }
                    break;
                }
                if (nInsertPos == i - head_index - 1)
                {
                    nStartIndex = i;
                    break;
                }
            }

            if (nStartIndex == -1)
            {
                strError = "nInsertPosֵ " + nInsertPos.ToString() + " ��������ķ�Χ";
                return -1;
            }

            int nOrderIndex = issue.OrderItems.IndexOf(this.order);
            Debug.Assert(nOrderIndex != -1, "");

            {
                Cell cell = new Cell();
                cell.item = new ItemBindingItem();
                cell.item.Container = issue;
                cell.item.Initial("<root />", out strError);
                cell.item.RefID = "";
                cell.item.LocationString = "";
                cell.item.Calculated = true;


                IssueBindingItem.SetFieldValueFromOrderInfo(
                    false,
                    cell.item,
                    this.order);
                Debug.Assert(nStartIndex - head_index - 1 >= 0, "");
                cell.item.OrderInfoPosition = new Point(nOrderIndex, nStartIndex - head_index - 1);

                issue.Cells.Insert(nStartIndex, cell);
                cell.Container = issue;

                // 2010/4/1
                cell.item.PublishTime = issue.PublishTime;
                cell.item.Volume = VolumeInfo.BuildItemVolumeString(
                    IssueUtil.GetYearPart(issue.PublishTime),
                    issue.Issue,
                    issue.Zong,
                    issue.Volume);
            }

            // ��ͬһ����λ���ұߵĸ��ӵĶ�����Ϣ��λ�����ı�
            for (int i = nStartIndex + 1; i < issue.Cells.Count; i++)
            {
                Cell cell = issue.GetCell(i);
                if (cell is GroupCell)
                    break;
                Debug.Assert(cell != null, "");
                Debug.Assert(cell.item != null, "");
                if (cell.item != null)
                {
                    // ��Ϊ������������
                    cell.item.OrderInfoPosition.Y++;
                }
            }


            // �����޸�<distribute>���ݣ���Ҫ�޸�<copy>����
            {
                string strNewValue = "";
                string strOldValue = "";
                OrderDesignControl.ParseOldNewValue(this.order.Copy,
                    out strOldValue,
                    out strNewValue);
                int nOldCopy = IssueBindingItem.GetNumberValue(strOldValue);
                int nNewCopy = IssueBindingItem.GetNumberValue(strNewValue);
                nOldCopy ++;
                this.order.Copy = OrderDesignControl.LinkOldNewValue(nOldCopy.ToString(),
                     nNewCopy.ToString());
            }

            this.order.UpdateDistributeString(this);
            issue.AfterMembersChanged();
            return nStartIndex;
        }

#if NOOOOOOOOOOOOO
        // �����ڲ����µĸ���(Ԥ�����)
        // parameters:
        //      nInsertPos  ����λ�á����Ϊ-1����ʾ������β��
        // return:
        //      ���ز����index(����issue.Cells�±�)
        internal int InsertNewMemberCell(
            int nInsertPos,
            out string strError)
        {
            Debug.Assert(this.EndBracket == false, "ֻ�ܶ�ͷ������ʹ��InsertGroupMemberCell()");
            IssueBindingItem issue = this.Container;
            if (issue == null)
            {
                Debug.Assert(issue != null, "");
                strError = "this.Container == null";
                return -1;
            }
            int head_index = issue.IndexOfCell(this);
            if (head_index == -1)
            {
                Debug.Assert(false, "");
                strError = "��������.Cells������û���ҵ��Լ�";
                return -1;
            }

            int nStartndex = -1;
            for (int i = head_index + 1; i < issue.Cells.Count; i++)
            {
                Cell cell = issue.GetCell(i);
                if (cell is GroupCell)
                {
                    if (nInsertPos == -1)
                    {
                        nStartndex = i;
                        break;
                    }
                    break;
                }
                if (nInsertPos == i - head_index - 1)
                {
                    nStartndex = i;
                    break;
                }
            }

            if (nStartndex == -1)
            {
                strError = "nInsertPosֵ " + nInsertPos.ToString() + " ��������ķ�Χ";
                return -1;
            }

            int nOrderIndex = issue.OrderItems.IndexOf(this.order);
            Debug.Assert(nOrderIndex != -1, "");

            {
                Cell cell = new Cell();
                cell.item = new ItemBindingItem();
                cell.item.Container = issue;
                cell.item.Initial("<root />", out strError);
                cell.item.RefID = "";
                cell.item.LocationString = "";
                cell.item.Calculated = true;
                IssueBindingItem.SetFieldValueFromOrderInfo(
                    false,
                    cell.item,
                    this.order);
                Debug.Assert(nStartndex - head_index - 1 >= 0, "");
                cell.item.OrderInfoPosition = new Point(nOrderIndex, nStartndex - head_index - 1);

                issue.Cells.Insert(nStartndex, cell);
                cell.Container = issue;
            }

            // ��ͬһ����λ���ұߵĸ��ӵĶ�����Ϣ��λ�����ı�
            for (int i = nStartndex+1; i < issue.Cells.Count; i++)
            {
                Cell cell = issue.GetCell(i);
                if (cell is GroupCell)
                    break;
                Debug.Assert(cell != null, "");
                Debug.Assert(cell.item != null, "");
                if (cell.item != null)
                {
                    // ��Ϊ������������
                    cell.item.OrderInfoPosition.Y++;
                }
            }

            // ע�⣺�����޸���<distribute>���ݣ���û���޸�<copy>����
            // ��˱�����´������������ӵĸ����ֻ᲻����

            bool bChanged = this.order.UpdateDistributeString(this);
            if (bChanged == true)
                issue.AfterMembersChanged();

            return nStartndex;
        }
#endif

        public List<Cell> CalculatedMemberCells
        {
            get
            {
                Debug.Assert(this.EndBracket == false, "ֻ�ܶ�ͷ������ʹ��CalculatedMemberCells");
                return GetMemberCells(0x01);
            }
        }

        public List<Cell> AcceptedMemberCells
        {
            get
            {
                Debug.Assert(this.EndBracket == false, "ֻ�ܶ�ͷ������ʹ��AcceptedMemberCells");
                return GetMemberCells(0x02);
            }
        }

        // ����ÿ�������������ĸ���
        internal override void Paint(
        long start_x,
        long start_y,
        PaintEventArgs e)
        {
            Debug.Assert(this.Container != null, "");

            if (BindingControl.TooLarge(start_x) == true
                || BindingControl.TooLarge(start_y) == true)
                return;

            if (this.EndBracket == false)
            {
                Debug.Assert(this.order != null, "");
            }
            else
            {
                Debug.Assert(this.order == null, "");
            }

            Debug.Assert(this.IsMember == false, "");

            bool bSelected = this.Selected;

            RectangleF rect;

            GraphicsState gstate = null;

            // �Ƿ������������ת
            bool bRotate = this.OutofIssue == true
                | (this.m_bFocus == true && this.m_bHover == false);

            if (bRotate == true)
            {
                rect = new RectangleF(start_x,
                    start_y,
                    this.Width,
                    this.Height);

                gstate = e.Graphics.Save();
                e.Graphics.Clip = new Region(rect);
                // Setup the transformation matrix
                Matrix x = new Matrix();
                if (this.OutofIssue == true
                    && (this.m_bFocus == true && this.m_bHover == false))
                    x.RotateAt(-35, new PointF(start_x + (this.Width / 2), start_y + (this.Height / 2)));
                else if (this.OutofIssue == true)
                    x.RotateAt(-45, new PointF(start_x + (this.Width / 2), start_y + (this.Height / 2)));
                else
                    x.RotateAt(10, new PointF(start_x + (this.Width / 2), start_y + (this.Height / 2)));
                e.Graphics.Transform = x;
            }

            // ��ͨ����
            Color colorText = this.Container.Container.ForeColor;
            Color colorGray = this.Container.Container.GrayColor;
            Brush brushBack = null;


            // ����
            if (bSelected == true)
            {
                // ѡ���˵ĸ���
                Color colorBack = this.Container.Container.SelectedBackColor;
                if (this.m_bFocus == true)
                {
                    // �� -- ��
                    brushBack = new LinearGradientBrush(
        new PointF(start_x, start_y + this.Height),
        new PointF(start_x + this.Width, start_y),
        Color.FromArgb(0, colorBack),
        Color.FromArgb(255, ControlPaint.Dark(colorBack))
        );
                }
                else
                {
                    brushBack = new SolidBrush(colorBack);
                }
                colorText = this.Container.Container.SelectedForeColor;
                colorGray = this.Container.Container.SelectedGrayColor;
            }
            else if (this.EndBracket == false)
            {
                // ������

                // brushBack = null;
                Color colorBack = this.Container.Container.BackColor;
                if (this.m_bFocus == true)
                {
                    // �� -- ��
                    brushBack = new LinearGradientBrush(
        new PointF(start_x, start_y + this.Height),
        new PointF(start_x + this.Width, start_y),
        Color.FromArgb(0, colorBack),
        Color.FromArgb(255, ControlPaint.Dark(colorBack))
        );
                }
                else
                {
                    // brushBack = new SolidBrush(colorBack);
                    // �� -- ��
                    brushBack = new LinearGradientBrush(
        new PointF(start_x, start_y),
        new PointF(start_x + this.Width, start_y),
        Color.FromArgb(50, colorBack),
        Color.FromArgb(255, colorBack)
        );

                }
            }
            else
            {
                // �һ�����

                // brushBack = null;
                Color colorBack = this.Container.Container.BackColor;
                if (this.m_bFocus == true)
                {
                    // �� -- ��
                    brushBack = new LinearGradientBrush(
        new PointF(start_x, start_y + this.Height),
        new PointF(start_x + this.Width, start_y),
        Color.FromArgb(255, colorBack),
        Color.FromArgb(0, ControlPaint.Dark(colorBack))
        );
                }
                else
                {
                    // brushBack = new SolidBrush(colorBack);
                    brushBack = new LinearGradientBrush(
        new PointF(start_x, start_y),
        new PointF(start_x + this.Width, start_y),
        Color.FromArgb(255, colorBack),
        Color.FromArgb(50, colorBack)
        );
                }
            }

            Color colorSideBar = Color.FromArgb(0, 255, 255, 255);

            // �½��ĺͷ������޸ĵģ��������ɫ��Ҫ�趨
            if (this.item != null
                && this.item.NewCreated == true)
            {
                // �´����ĵ���
                colorSideBar = this.Container.Container.NewBarColor;
            }
            else if (this.item != null
           && this.item.Changed == true)
            {
                // �޸Ĺ��ĵĵ���
                colorSideBar = this.Container.Container.ChangedBarColor;
            }


            // �߿�ͱ���
            {

                rect = new RectangleF(start_x,
                    start_y,
                    this.Container.Container.m_nCellWidth,
                    this.Container.Container.m_nCellHeight);

                {
                    // û�н���ʱҪСһЩ
                    rect = GuiUtil.PaddingRect(this.Container.Container.CellMargin,
                        rect);
                }
                // rect = RectangleF.Inflate(rect, -4, -4);

                // �Ż�
                if (rect.IntersectsWith(e.ClipRectangle) == true
                    || bRotate == true)
                {
                    float fPenWidth = 6;

                    e.Graphics.FillRectangle(brushBack, rect);


                    // rect.Inflate(-(fPenWidth / 2), -(fPenWidth / 2));
                    Pen penBorder = new Pen(Color.FromArgb(100, Color.Gray), fPenWidth);
                    penBorder.LineJoin = LineJoin.Bevel;
                    BindingControl.Bracket(e.Graphics,
                        penBorder,
                        this.EndBracket == false ? true : false,   //left
                        rect,
                        10);

                    // brushBack?
                    if (this.EndBracket == false)
                    {
                        int height = 20;
                        int width = 20;
                        // ���Ͻ�
                        RectangleF rectCircle = new RectangleF(
                            rect.X+rect.Width-20-20,  // radius * 2
                            rect.Y, // +rect.Height/2-height/2,
                            width,
                            height);
                        Color colorDark = this.Container.Container.ForeColor;
                        // e.Graphics.FillRectangle(new SolidBrush(Color.Red), rectCircle);
                        BindingControl.Circle(e.Graphics,
                            null,
                            new SolidBrush(colorDark),
                            rectCircle);

                        IssueBindingItem issue = this.Container;
                        Debug.Assert(issue != null, "");
                        int nOrderIndex = issue.OrderItems.IndexOf(this.order);
                        Debug.Assert(nOrderIndex != -1, "");
                        string strText = new String((char)((int)'a' + nOrderIndex), 1);

                        StringFormat stringFormat = new StringFormat();
                        stringFormat.Alignment = StringAlignment.Center;
                        stringFormat.LineAlignment = StringAlignment.Center;
                        stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                        Brush brushText = new SolidBrush(this.Container.Container.BackColor);
                        Font font = new Font("΢���ź�",
                            rectCircle.Height,
                            FontStyle.Bold,
                            GraphicsUnit.Pixel);
                        e.Graphics.DrawString(strText,
                            font,
                            brushText,
                            rectCircle,
                            stringFormat);

                    }

                    // ���������
                    Brush brushSideBar = new SolidBrush(colorSideBar);
                    RectangleF rectSideBar = new RectangleF(
                        rect.X + penBorder.Width,
                        rect.Y + 10,
                        10 / 2,
                        rect.Height - 2 * 10);
                    e.Graphics.FillRectangle(brushSideBar, rectSideBar);

                }
            }

            // ��������
            if (this.order != null)
            {
                    this.PaintTextLines(start_x, start_y, false,
                        colorText,
                        e, bRotate);

                if (this.m_bHover == true)
                    this.PaintLineLabels(start_x, start_y, e, bRotate);
            }
            else
            {
                /*
                // �հ׸��ӡ����Ƶ�ɫ�ġ�ȱ������
                this.PaintQue(start_x,
                    start_y,
                    "ȱ",
                    colorGray,
                    e,
                    bRotate);
                */
            }


            // ��������
            if (this.m_bFocus == true)
            {
                rect = new RectangleF(
    start_x,
    start_y,
    this.Width,
    this.Height);
                rect.Inflate(-1, -1);
                ControlPaint.DrawFocusRectangle(e.Graphics,
                    Rectangle.Round(rect));
            }

            if (gstate != null)
            {
                Debug.Assert(bRotate == true);
                e.Graphics.Restore(gstate);
            }

            // �ƶ����֣���Ҫ��ת����Ϊ��ת���������Ĳ�һ��
            Rectangle rect1 = this.Container.Container.RectGrab;
            rect1.Offset((int)start_x, (int)start_y);
            if (this.m_bHover == true)
            {
                ControlPaint.DrawContainerGrabHandle(
        e.Graphics,
        rect1);
            }
        }

        // ���ƶ�������ӵ�������
        internal override void PaintTextLines(float x0,
    float y0,
    bool bGrayText,
    Color colorText,
    PaintEventArgs e,
    bool bDoNotSpeedUp)
        {
            int nLineHeight = this.LineHeight;
            Padding margin = this.Container.Container.CellMargin;
            Padding padding = this.Container.Container.CellPadding;
            x0 += margin.Left + padding.Left;
            y0 += margin.Top + padding.Top;
            int nWidth = this.Width - margin.Horizontal - padding.Horizontal;
            int nHeight = this.Height - margin.Vertical - padding.Vertical;

            int nUsedHeight = 0;    // ʹ�ù����ۻ��߶�
            // Color colorText = this.Container.Container.MemberForeColor;

            if (bGrayText == true)
                colorText = ControlPaint.Light(colorText, 1.5F);

            // �� -- ��
            LinearGradientBrush brushGradient = new LinearGradientBrush(
new PointF(x0, 0),
new PointF(x0 + 6, 0),
Color.FromArgb(255, Color.Gray),
Color.FromArgb(0, Color.Gray)
);

            Font font = this.Container.Container.m_fontLine;

            Pen penLine = new Pen(brushGradient, (float)1);
            for (int i = 0; i < this.Container.Container.GroupTextLineNames.Length / 2; i++)
            {
                int nRestHeight = nHeight - nUsedHeight;

                // ������
                RectangleF rect = new RectangleF(
                    x0,
                    y0,
                    nWidth,
                    Math.Min(this.LineHeight, nRestHeight));

                // �Ż�
                if (rect.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
                {
                    string strName = this.Container.Container.GroupTextLineNames[i * 2];

                    string strText = this.order.GetText(strName);

                    PaintText(
                        x0,
                        y0,
                        nWidth,
                        Math.Min(nLineHeight, nRestHeight),
                        strText,
                        colorText,
                        font,
                        e);

                    // �·�����
                    if (nLineHeight < nRestHeight)
                    {
                        e.Graphics.DrawLine(penLine,
                            new PointF(rect.X, rect.Y + nLineHeight - 1),
                            new PointF(rect.X + 5, rect.Y + nLineHeight - 1));
                    }
                }


                y0 += nLineHeight;
                nUsedHeight += nLineHeight;

                if (nUsedHeight > nHeight)
                    break;
            }
        }

        // ���ƶ�������ӵ����ֱ�ǩ(�ֶ���)
        internal override void PaintLineLabels(float x0,
    float y0,
    PaintEventArgs e,
    bool bDoNotSpeedUp)
        {
            int nLineHeight = this.LineHeight;

            Padding margin = this.Container.Container.CellMargin;
            Padding padding = this.Container.Container.CellPadding;
            x0 += margin.Left + padding.Left;
            y0 += margin.Top + padding.Top;
            int nWidth = this.Width - margin.Horizontal - padding.Horizontal;
            int nHeight = this.Height - margin.Vertical - padding.Vertical;

            int nUsedHeight = 0;    // ʹ�ù����ۻ��߶�
            Color colorText = Color.FromArgb(200, 0, 100, 0);

            Font font = this.Container.Container.m_fontLine;
            font = new Font(font, FontStyle.Bold);

            // ������ֵ������
            float fMaxTextWidth = 0;
            for (int i = 0; i < this.Container.Container.GroupTextLineNames.Length / 2; i++)
            {
                string strLabel = this.Container.Container.GroupTextLineNames[i * 2 + 1];
                SizeF size = e.Graphics.MeasureString(strLabel, font);
                if (size.Width > fMaxTextWidth)
                    fMaxTextWidth = size.Width;
            }

            // ���ư�͸������
            {
                RectangleF rect1 = new RectangleF(
                    x0 + nWidth - (fMaxTextWidth + 4),
                    y0,
                    fMaxTextWidth + 4,
                    nHeight);

                if (rect1.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
                {

                    // �� -- ��
                    LinearGradientBrush brushGradient = new LinearGradientBrush(
    new PointF(rect1.X, rect1.Y),
    new PointF(rect1.X + rect1.Width, rect1.Y),
    Color.FromArgb(150, Color.White),
    Color.FromArgb(200, Color.White)
    );

                    e.Graphics.FillRectangle(brushGradient,
                        rect1);
                }

            }

            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Far;
            stringFormat.LineAlignment = StringAlignment.Near;
            stringFormat.FormatFlags = StringFormatFlags.NoWrap;

            Brush brushText = new SolidBrush(colorText);
            Pen penLine = new Pen(colorText, (float)1);
            for (int i = 0; i < this.Container.Container.GroupTextLineNames.Length / 2; i++)
            {
                int nRestHeight = nHeight - nUsedHeight;

                // ������
                RectangleF rect = new RectangleF(
                    x0 + nWidth - fMaxTextWidth,
                    y0,
                    fMaxTextWidth,
                    Math.Min(nLineHeight, nRestHeight));

                // �Ż�
                if (rect.IntersectsWith(e.ClipRectangle) == true
                    || bDoNotSpeedUp == true)
                {
                    /*
                    PaintText(
                        x0,
                        y0,
                        nWidth,
                        Math.Min(this.LineHeight, nRestHeight),
                        strLabel,
                        colorText,
                        stringFormat,
                        e);
                     * */
                    string strLabel = this.Container.Container.GroupTextLineNames[i * 2 + 1];


                    e.Graphics.DrawString(strLabel,
                        font,
                        brushText,
                        rect,
                        stringFormat);

                    // �·�����
                    if (nLineHeight < nRestHeight)
                    {
                        e.Graphics.DrawLine(penLine,
                            new PointF(rect.X, rect.Y + nLineHeight - 1),
                            new PointF(rect.X + rect.Width, rect.Y + nLineHeight - 1));
                    }
                }


                y0 += nLineHeight;
                nUsedHeight += nLineHeight;

                if (nUsedHeight > nHeight)
                    break;
            }
        }
    }

    /// <summary>
    /// ǰ��������ɫ
    /// </summary>
    public class PaintInfo
    {
        /// <summary>
        /// ������ɫ
        /// </summary>
        public Color BackColor;

        /// <summary>
        /// ǰ����ɫ
        /// </summary>
        public Color ForeColor;
    }
}
