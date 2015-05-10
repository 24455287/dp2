using System;
using System.Windows.Forms;
using System.Drawing;

namespace DigitalPlatform.Xml
{
	// �ĵ����ڵ�
	public class VirtualRootItem : ElementItem
	{
		internal VirtualRootItem(XmlEditor document):base(document)
		{
			this.Name = "#document";
			this.m_document = document;
		}

		// ΪʲôҪ��д�������
		// ��Ϊֻ��������ʱ,ֻ�и��Ż�����,�������ײ�������
		public override void Paint(PaintEventArgs pe,
			int nBaseX,
			int nBaseY,
			PaintMember paintMember)
		{
			// 1.���������
			Rectangle rectPaintThis = new Rectangle (0,0,0,0);
			rectPaintThis = new Rectangle (nBaseX + this.Rect.X,
				nBaseY + this.Rect.Y,
				this.Rect.Width,
				this.Rect.Height);
			if (rectPaintThis.IntersectsWith(pe.ClipRectangle )== false)
				return;

			Brush brush = null;

			// 2.������ɫ
			//	����ȱʡ͸��ɫ,��ǰ��ɫ��͸��ɫ��ͬ�򲻻���
			//?
			Object colorDefault = null;
			XmlEditor editor = this.m_document;
			if (editor != null && editor.VisualCfg != null)
				colorDefault = editor.VisualCfg.transparenceColor;
			if (colorDefault != null)  //ȱʡ��ɫ
			{
				if (((Color)colorDefault).Equals (BackColor) == true)
					goto SKIPDRAWBACK;
			}
			brush = new SolidBrush(BackColor);
			pe.Graphics .FillRectangle (brush,rectPaintThis);

			SKIPDRAWBACK:

				// 4.������
				if (childrenVisual == null)
					goto END1;

			for(int i=0;i<this.childrenVisual.Count;i++)
			{
				Visual visual = (Visual)(this.childrenVisual[i]);

				Rectangle rectPaintChild = 
					new Rectangle(
					nBaseX + this.Rect.X + visual.Rect.X,
					nBaseY + this.Rect.Y + visual.Rect.Y,
					visual.Rect.Width,
					visual.Rect.Height );

				if(rectPaintChild.IntersectsWith (pe.ClipRectangle ) == true)
				{
					visual.Paint(pe,
						nBaseX + this.Rect.X,
						nBaseY + this.Rect.Y,
						paintMember);
				}

				if (i <= 0)
					continue;

				if (editor != null
					&& editor.VisualCfg == null)
				{
					if (this.layoutStyle == LayoutStyle.Horizontal)
					{
						this.DrawLines(rectPaintChild,
							0,
							0,
							visual.LeftBorderWidth,
							0,
							visual.BorderColor);
					}
					else
					{
						this.DrawLines(rectPaintChild,
							visual.TopBorderHeight,
							0,
							0,
							0,
							visual.BorderColor);
					}
							
				}
			}

			END1:
				// 3.����Ԫ�ص���߿�����

				this.DrawLines(rectPaintThis,
					this.TopBorderHeight,
					this.BottomBorderHeight,
					this.LeftBorderWidth,
					this.RightBorderWidth,
					this.BorderColor);

		
		}
	}

}
