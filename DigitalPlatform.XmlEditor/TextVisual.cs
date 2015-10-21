using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform.IO;

namespace DigitalPlatform.Xml
{
	public class TextVisual : Visual
	{
		#region ��Ա����

		private string strText = null;

		public bool Editable = true;

		#endregion

		#region TextVisual������

		// �������Ե�ԭ���ǽ�����ʱ�������ļ�����,����m_*��������
		// TextVisual�ǻ��࣬��������һЩ��
		public virtual string Text
		{
			get
			{
				Debug.Assert(this.strText != null,"��δ��ʼ��111");
				return strText;
			}
			set
			{
				strText = value;
			}
		}
		public override int GetWidth()
		{
			return this.TotalRestWidth;
		}

		#endregion

		#region ����Cfg������

		public Font GetFont() 
		{
			Item item = this.GetItem();
			if (item == null)
				return null;

			ItemRegion region = GetRegionName();
			return item.GetFont(region);
		}

		public Color TextColor
		{
			get
			{
				Item item = this.GetItem ();
				if (item == null)
				{
					return Color.Black ;
				}

				ItemRegion region = GetRegionName();
				if (region == ItemRegion.No )
				{
					return Color.Black ;
				}

				return item.GetTextColor  (region);
			}
		}
		#endregion

		#region ��д������麯��

        // ���ݴ����������꣬�õ����е�Visual����
        // parameters:
        //      p           ������������
        //      retVisual   out���������ػ��е�visual
        // return:
        //      -1  ���겻�ڱ�����
        //      0   ������
        //      1   �հ�
        //      2   ��϶��
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



		public override int GetHeight(int nWidth)
		{
			if (this.Text == null)
				return 0;

			Item item = this.GetItem ();
            using (Graphics g = Graphics.FromHwnd(item.m_document.Handle))
            {
                SizeF size = g.MeasureString(this.Text,
                    GetFont(),
                    nWidth,
                    new StringFormat());
                int nTempHeight = (int)size.Height;
                if (nTempHeight < 0)
                    nTempHeight = 0;

                return nTempHeight + this.TotalRestHeight;
            }
		}

		public override void Paint(PaintEventArgs pe,
			int nBaseX,
			int nBaseY,
			PaintMember paintMember)
		{
			if (this.Rect.Width == 0
				|| this.Rect.Height == 0)
				return;

			Rectangle rectPaint = new Rectangle (nBaseX + this.Rect.X ,
				nBaseY + this.Rect.Y,
				this.Rect.Width, 
				this.Rect.Height);

			Brush brush = null;

			//����ɫ
			Item item = this.GetItem ();
			Object colorDefault = null;
			XmlEditor editor = item.m_document;
			if (editor != null && editor.VisualCfg != null)
				colorDefault = editor.VisualCfg.transparenceColor ;
			if (colorDefault != null)
			{
				if (((Color)colorDefault).Equals (BackColor) == true)
					goto SKIPDRAWBACK;

			}

			brush = new SolidBrush(this.BackColor);
			pe.Graphics .FillRectangle (brush,rectPaint);

			SKIPDRAWBACK:

				//��DrawLines���߿�
				if (editor != null && editor.VisualCfg == null)
				{
				}
				else
				{
					this.DrawLines(rectPaint,
						this.TopBorderHeight,
						this.BottomBorderHeight,
						this.LeftBorderWidth,
						this.RightBorderWidth,
						this.BorderColor);
				}

			//��������
			rectPaint = new Rectangle (nBaseX + this.Rect.X + this.LeftResWidth/*LeftBlank*/,
				nBaseY + this.Rect.Y + this.TopResHeight/*this.TopBlank*/,
				this.Rect.Width - this.TotalRestWidth/*this.LeftBlank - this.RightBlank*/,
				this.Rect.Height - this.TotalRestHeight/*this.TopBlank - this.BottomBlank*/);
			
			brush = new SolidBrush(TextColor);
			Font font1 = this.GetFont();
			Font font = new Font(font1.Name,font1.Size);

			pe.Graphics.DrawString(Text,
				font, 
				brush,	
				rectPaint,
				new StringFormat());
	
			font.Dispose();

			brush.Dispose ();
			font.Dispose ();
		}


		#endregion
	}
}
