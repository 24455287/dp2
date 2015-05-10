using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform.IO;

namespace DigitalPlatform.Xml
{
	public class ExpandHandle : Visual
	{
		// �ж�һ��visual�Ƿ���ExpandHandle
		public override bool IsExpandHandle()
		{
			return true;
		}

		public ExpandIconStyle expandIconStyle
		{
			get
			{
				ElementItem item = (ElementItem)this.GetItem();
				if (item == null)
					return ExpandIconStyle.Minus;

				//����������Ե�չ����ť
				if (this.Name == "ExpandAttributes")
				{
					if (item.m_attrsExpand == ExpandStyle.Expand)
						return ExpandIconStyle.Minus;
					else
						return ExpandIconStyle.Plus;
				}
				else if (this.Name == "ExpandContent")
				{
					if (item.m_childrenExpand  == ExpandStyle.Expand)
						return ExpandIconStyle.Minus;
					else
						return ExpandIconStyle.Plus;
				}
				return ExpandIconStyle.Minus ;
			}
		}



		public override ItemRegion GetRegionName()
		{
			if (this.Name == "ExpandAttributes")
				return ItemRegion.ExpandAttributes ;
			else
				return ItemRegion.ExpandContent;
			
		}	

        // return:
        //      -1  ���겻�ڱ�����
        //      0   ������
        //      1   �հ�
        //      2   ��϶
		public override int HitTest(Point p,
			out Visual retVisual)
		{
			retVisual = null;
			int nResizeAreaWidth = 4;   //��϶�Ŀ��
			//�ڷ���
			if ( p.X >= this.Rect.X + this.Rect.Width - (nResizeAreaWidth/2)
				&& p.X < this.Rect.X + this.Rect.Width + (nResizeAreaWidth/2)) 
			{
				retVisual = this;
				return  2;
			}

			//��������
			if (p.X < this.Rect.X 
				|| p.Y < this.Rect.Y )
			{
				return -1;
			}
			if (p.X > this.Rect.X + this.Rect.Width 
				|| p.Y > this.Rect.Y + this.Rect.Height )
			{
				return -1;
			}

			//�������Ϳհ�
			//1. �������հ״�
			if (p.X > this.Rect.X 
				&& p.X < this.Rect.X + this.LeftResWidth
				&& p.Y > this.Rect.Y
				&& p.Y < this.Rect.Y + this.Rect.Height)
			{
				retVisual = this;
				return -1;
			}

			// 2.�������հ״�
			if (p.X > this.Rect.X + this.Rect.Width - this.RightResWidth
				&& p.X < this.Rect.X + this.Rect.Width
				&& p.Y > this.Rect.Y
				&& p.Y < this.Rect.Y + this.Rect.Height)
			{
				retVisual = this;
				return -1;
			}
			// 3.�������հ״�
			if (p.Y > this.Rect.Y
				&& p.Y < this.Rect.Y + this.TopResHeight
				&& p.X > this.Rect.X
				&& p.X < this.Rect.X + this.Rect.Width)
			{
				retVisual = this;
				return -1;
			}
			// 4.�������հ״�
			if (p.Y > this.Rect.Y + this.Rect.Height - this.BottomResHeight
				&& p.Y < this.Rect.Y + this.Rect.Height
				&& p.X > this.Rect.X
				&& p.X < this.Rect.X + this.Rect.Width)
			{
				retVisual = this;
				return -1;
			}

			
			//��������
			if (p.X >= this.Rect.X + this.LeftResWidth 
				&& p.Y >= this.Rect.Y + this.TopResHeight 
				&& p.X < this.Rect.X + this.Rect.Width - this.RightResWidth
				&& p.Y < this.Rect.Y + this.Rect.Height - this.BottomResHeight)
			{
				retVisual = this;
				return 0;
			}
			return -1;
		}



		public override int GetWidth()
		{
			Item item = this.GetItem ();
			PartWidth parteWidth = item.GetPartWidth (this.GetType ().Name );
			return parteWidth.nWidth ;
		}

		public override int GetHeight(int nWidth)
		{
			return GetWidth();
		}

		public override void Paint(PaintEventArgs pe,
			int nBaseX,
			int nBaseY,
			PaintMember paintMember)
		{
			Rectangle rectPaint = new Rectangle (nBaseX + this.Rect.X,
				nBaseY + this.Rect.Y,
				this.Rect.Width,
				this.Rect.Height);

			Color backColor = this.BackColor ;
			Brush brush = new SolidBrush(this.BackColor );
			pe.Graphics .FillRectangle (brush,rectPaint);

			Item item = this.GetItem();
			XmlEditor editor = null;
			if (item != null)
			{
				editor = item.m_document;
			}

			if (editor != null && editor.VisualCfg == null)
			{
			}
			else
			{
				//��DrawLines���߿�
				this.DrawLines (rectPaint,
					this.TopBorderHeight,
					this.BottomBorderHeight,
					this.LeftBorderWidth,
					this.RightBorderWidth,
					this.BorderColor);
			}

			int nWidth =(this.Rect.Width
				- this.LeftBlank
				- this.RightBlank);

			PaintButton(pe.Graphics,
				rectPaint.X + this.LeftBlank,
				rectPaint.Y,
				nWidth,
				this.expandIconStyle);

			/*
						//�����������
						Pen pen = new Pen (Color.Gray );
						pe.Graphics.DrawLine  (pen,
							rectPaint.X + rectPaint.Width /2,
							rectPaint.Y + nWidth ,
							rectPaint.X + rectPaint.Width /2,
							rectPaint.Y + rectPaint.Height -5);

						pe.Graphics.DrawLine  (pen,
							rectPaint.X + rectPaint.Width /2,
							rectPaint.Y + rectPaint.Height -5,
							rectPaint.X + rectPaint.Width /2+3,
							rectPaint.Y + rectPaint.Height -5);
			*/
		}

		
		public void PaintButton(Graphics g,
			int x,
			int y,
			int nWidth,
			ExpandIconStyle iconStyle)
		{
			Pen pen = new Pen (Color.Gray );
			Pen penMid = new Pen (Color.Black );

			//����
			g.DrawLine(pen,
				x ,
				y ,
				x + nWidth -1,
				y );
						
			//����
			g.DrawLine (pen,
				x,
				y + nWidth -1,
				x + nWidth -1,
				y + nWidth -1);


			//����
			g.DrawLine (pen,
				x,
				y,
				x,
				y + nWidth -1);

			//����
			g.DrawLine (pen,
				x + nWidth -1,
				y  ,
				x + nWidth -1,
				y + nWidth -1);

			//�м����
			g.DrawLine (penMid,
				x + 2,
				y + nWidth/2,
				x + nWidth -2 -1,
				y + nWidth/2);

			if (iconStyle == ExpandIconStyle.Plus)
			{
				//�м�����
				g.DrawLine (penMid,
					x + nWidth/2,
					y + 2,
					x + nWidth/2,
					y + nWidth -2 -1);
			}
			penMid.Dispose ();
			pen .Dispose ();
		}


	
	}

}
