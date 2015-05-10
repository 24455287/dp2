using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform.IO;

namespace DigitalPlatform.Xml
{
	//******************************************************
	// BoxVisual ��
	//******************************************************
	public class Box : Visual
	{

		public   ArrayList childrenVisual = null;   //������visual,���������ݵĲ��


		// ��һ����visual
		public void AddChildVisual(Visual visual)
		{
			if (childrenVisual == null)
				childrenVisual = new ArrayList ();
			childrenVisual.Add (visual);
		}


		// ���������visual
		public void ClearChildVisual()
		{
			if (childrenVisual != null)
				childrenVisual.Clear ();
		}


	

		public override ItemRegion GetRegionName()
		{
			if (this.Name == "BoxTotal")
				return ItemRegion.BoxTotal ;
			else if (this.Name == "BoxAttributes")
				return ItemRegion.BoxAttributes ;
			else if (this.Name == "BoxContent")
				return ItemRegion.BoxContent ;
			return ItemRegion.No ;
		}	

		
		public override int HitTest(Point p,
			out Visual retVisual)
		{
			retVisual = null;
			
			//�ж���ʱ,�ȿ�����
			if (this.childrenVisual != null)
			{
				Point p1 = new Point (p.X - this.Rect.X,
					p.Y - this.Rect.Y);

				//��������,������û�б�Ҫ�ģ���Ϊ��������Ե�����ܣ�
				Visual visual = null;
				int nCount = this.childrenVisual.Count ;
				for(int i = nCount -1;i>=0;i--)
				{
					visual = (Visual)childrenVisual[i];

					if (p1.X >= visual.Rect.X 
						&& p1.X < (visual.Rect.X + visual.Rect.Width)
						&& p1.Y >= visual.Rect.Y 
						&& p1.Y < (visual.Rect.Y + visual.Rect.Height ))
					{
						// int nRet = -1;
						return visual.HitTest(p1,out retVisual);
						
					}
				}
			}

			//��û���ӣ����߶���һ���������ϣ���ô�Ϳ��Լ���


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
				&& p.Y >= this.Rect.Y
				&& p.Y < this.Rect.Y + this.Rect.Height)
			{
				retVisual = this;
				return -1;
			}

			// 2.�������հ״�
			if (p.X > this.Rect.X + this.Rect.Width - this.RightResWidth
				&& p.X < this.Rect.X + this.Rect.Width
				&& p.Y >= this.Rect.Y
				&& p.Y < this.Rect.Y + this.Rect.Height)
			{
				retVisual = this;
				return -1;
			}
			// 3.�������հ״�
			if (p.Y >= this.Rect.Y
				&& p.Y < this.Rect.Y + this.TopResHeight
				&& p.X >= this.Rect.X
				&& p.X < this.Rect.X + this.Rect.Width)
			{
				retVisual = this;
				return -1;
			}
			// 4.�������հ״�
			if (p.Y >= this.Rect.Y + this.Rect.Height - this.BottomResHeight
				&& p.Y < this.Rect.Y + this.Rect.Height
				&& p.X >= this.Rect.X
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



		// ����
		public override void Layout(int x,
			int y,
			int nInitialWidth,
			int nInitialHeight,
			int nTimeStamp,
			out int nRetWidth,
			out int nRetHeight,
			LayoutMember layoutMember)
		{
			nRetWidth = nInitialWidth;
			nRetHeight = nInitialHeight;

			bool bEnlargeWidth = false;
			if ((layoutMember & LayoutMember.EnlargeWidth  ) == LayoutMember.EnlargeWidth  )
				bEnlargeWidth = true;
			bool bEnlargeHeight = false;
			if ((layoutMember & LayoutMember.EnLargeHeight ) == LayoutMember.EnLargeHeight )
				bEnlargeHeight = true;
			if (bEnlargeWidth == true	|| bEnlargeHeight == true)
			{
				//���׺��ֵܶ�Ӱ����
				if ((layoutMember & LayoutMember.Up ) == LayoutMember.Up )
				{
					if (bEnlargeHeight == true)
					{
						this.Rect.Height = nInitialHeight;
						Box myContainer = (Box)(this.container );
						if (myContainer == null)
							return;
	
						//����
						if (myContainer.LayoutStyle == LayoutStyle.Horizontal )
						{
							//Ӱ���ֵ�
							foreach(Visual child in myContainer.childrenVisual )
							{
								if (child.Equals (this) == true)
									continue;

								child.Layout (
									child.Rect.X,
									child.Rect.Y,
									child.Rect.Width,
									this.Rect.Height,
									nTimeStamp,
									out nRetWidth,
									out nRetHeight,
									LayoutMember.EnLargeHeight );
							}

							int nMyHeight = this.Rect.Height;

							foreach(Visual child in myContainer.childrenVisual )
							{
								if (child.Rect.Height > nMyHeight)
									nMyHeight = child.Rect.Height ;
							}
							nMyHeight += myContainer.TotalRestHeight;

							myContainer.Layout (
								myContainer.Rect.X,
								myContainer.Rect.Y,
								myContainer.Rect.Width ,
								nMyHeight ,
								nTimeStamp,
								out nRetWidth,
								out nRetHeight,
								layoutMember);
						}
						//����
						if (myContainer.LayoutStyle == LayoutStyle.Vertical )
						{
							int nTempTotalHeight = 0;
							foreach(Visual childVisual in myContainer.childrenVisual )
							{
								nTempTotalHeight += childVisual.Rect.Height ;
							}
							nTempTotalHeight += myContainer.TotalRestHeight;;

							
							myContainer.Layout (
								myContainer.Rect.X,
								myContainer.Rect.Y,
								myContainer.Rect.Width,
								nTempTotalHeight,
								nTimeStamp,
								out nRetWidth,
								out nRetHeight,
								layoutMember);


							//���ֵ�����
							int nXDelta = myContainer.LeftResWidth;
							int nYDelta = myContainer.TopResHeight;
							foreach(Visual childVisual in myContainer.childrenVisual )
							{
								childVisual.Rect.X = nXDelta;
								childVisual.Rect.Y = nYDelta;
								nYDelta += childVisual.Rect.Height;
							}
							
						}
						return;
					}
				}
			
				if (LayoutStyle == LayoutStyle.Horizontal )
				{
					if (bEnlargeHeight == true)
					{
						this.Rect.Height  = nInitialHeight;
						foreach(Visual child in this.childrenVisual )
						{
							child.Layout (0,
								0,
								0,
								nInitialHeight,
								nTimeStamp,
								out nRetWidth,
								out nRetHeight,
								layoutMember);
						}
					}
				}
				else if (LayoutStyle == LayoutStyle.Vertical )
				{
					if (bEnlargeWidth== true)
					{
						this.Rect.Width = nInitialWidth;
						foreach(Visual child in this.childrenVisual )
						{
							child.Layout (0,
								0,
								nInitialWidth,
								0,
								nTimeStamp,
								out nRetWidth,
								out nRetHeight,
								layoutMember);
						}
					}
				}
				return;	
			}

			Item item = GetItem();
			item.m_document.nTime ++;
			string strTempInfo = "";
			int nTempLevel = this.GetVisualLevel ();
			string strLevelString = this.GetStringFormLevel (nTempLevel);
			if (this.IsWriteInfo == true)
			{
				strTempInfo = "\r\n"
					+ strLevelString + "******************************\r\n"
					+ strLevelString + "���ǵ�" + nTempLevel + "���" + this.GetType ().Name + "��layout��ʼ\r\n" 
					+ strLevelString + "����Ϊ:\r\n"
					+ strLevelString + "x=" + x + "\r\n"
					+ strLevelString + "y=" + y + "\r\n"
					+ strLevelString + "nInitialWidth=" + nInitialWidth + "\r\n"
					+ strLevelString + "nInitialHeight=" + nInitialHeight + "\r\n"
					+ strLevelString + "nTimeStamp=" + nTimeStamp + "\r\n"
					+ strLevelString + "layoutMember=" + layoutMember.ToString () + "\r\n"
					+ strLevelString + "LayoutStyle=" + this.LayoutStyle.ToString () + "\r\n"
					+ strLevelString + "ʹ�ܴ�����Ϊ" + item.m_document.nTime  + "\r\n";
				StreamUtil.WriteText ("I:\\debug.txt",strTempInfo);
			}

			if (Catch == true)
			{
				//�����������ͬʱ��ֱ�ӷ���catch����
				if (sizeCatch.nInitialWidth == nInitialWidth
					&& sizeCatch.nInitialHeight == nInitialHeight
					&& (sizeCatch.layoutMember == layoutMember))
				{
					if (this.IsWriteInfo == true)
					{
						strTempInfo = "\r\n"
							+ strLevelString + "------------------"
							+ strLevelString + "�뻺��ʱ��ͬ\r\n"
							+ strLevelString + "�����ֵ: initialWidth:"+nInitialWidth + " initialHeight:" + nInitialHeight + " timeStamp: " + nTimeStamp + " layoutMember:" + layoutMember.ToString () + "\r\n"
							+ strLevelString + "�����ֵ: initialWidth:"+sizeCatch.nInitialWidth + " initialHeight:" + sizeCatch.nInitialHeight + " timeStamp: " + sizeCatch.nTimeStamp + " layoutMember:" + sizeCatch.layoutMember.ToString () + "\r\n";
					}

					if ((layoutMember & LayoutMember.Layout) != LayoutMember.Layout )
					{
						if (this.IsWriteInfo == true)
						{
							strTempInfo += strLevelString + "����ʵ����ֱ�ӷ��ػ�����ֵ\r\n";
						}

						nRetWidth = sizeCatch.nRetWidth  ;
						nRetHeight = sizeCatch.nRetHeight  ;

						if (this.IsWriteInfo == true)
						{
							strTempInfo +=   strLevelString + "----------����------\r\n";
							StreamUtil.WriteText ("I:\\debug.txt",strTempInfo);
						}
						goto END1;
					}
					else
					{
						if (this.IsWriteInfo == true)
						{
							strTempInfo += strLevelString + "����ʵ�������¼���\r\n";
						}
					}

					if (this.IsWriteInfo == true)
					{				
						strTempInfo += strLevelString + "----------����------\r\n";
						StreamUtil.WriteText ("I:\\debug.txt",strTempInfo);
					}
				}
				else
				{
					if (this.IsWriteInfo == true)
					{
						strTempInfo = "\r\n"
							+ strLevelString + "------------------"
							+ strLevelString + "�뻺��ʱ��ͬ\r\n"
							+ strLevelString + "�����ֵ: initialWidth:"+nInitialWidth + " initialHeight:" + nInitialHeight + " timeStamp: " + nTimeStamp + " layoutMember:" + layoutMember.ToString () + "\r\n"
							+ strLevelString + "�����ֵ: initialWidth:"+sizeCatch.nInitialWidth + " initialHeight:" + sizeCatch.nInitialHeight + " timeStamp: " + sizeCatch.nTimeStamp + " layoutMember:" + sizeCatch.layoutMember.ToString () + "\r\n";

						strTempInfo +=   strLevelString + "----------����------\r\n";
						StreamUtil.WriteText ("I:\\debug.txt",strTempInfo);
					}			
				}
			}




			//����ÿһС���õò���
			int nPartWidth = 0;  
			int nPartHeight = 0;
			int nRetPartWidth = 0;;  //����
			int nRetPartHeight = 0;

			int nTotalWidth = 0; //�����ܿ��
			int nMaxHeight = 0; //����ʱ�����߶�,�����Ҫ������

			int nMaxWidth = 0; //����ʱ������ȣ������Ҫ������
			int nTotalHeight = 0; //�����ܸ߶�

			Visual visual = null;

			ArrayList aVisualUnDefineWidth = null;  //û�ж����ȵ�Visual��ɵ�����
			PartInfo partInfo = null;  //�����ƿ�ȵĶ���

			//����
			if (LayoutStyle == LayoutStyle.Horizontal )
			{
				//******************************************
				//1.ֻ����,�õȺ�
				//*******************************************
				if ((layoutMember == LayoutMember.CalcuWidth ))
				{
					nTotalWidth = 0;  //�ܿ�ȸ�0
					if (aVisualUnDefineWidth != null)
						aVisualUnDefineWidth.Clear ();
					else 
						aVisualUnDefineWidth = new ArrayList ();

					if (this.childrenVisual != null)
					{
						//���������ҵ�����Ŀ��
						for(int i = 0 ; i < this.childrenVisual.Count ;i++)
						{
							visual = (Visual)childrenVisual[i];
							PartWidth  partWidth = item.GetPartWidth (visual.GetType ().Name );
						
							//û�ҵ����󣬻򼶱��С�ڵ���0���ӵ�δ����������
							if (partWidth == null
								|| partWidth.nGradeNo <= 0)
							{
								aVisualUnDefineWidth.Add (visual);
								continue;
							}
							nPartWidth = partWidth.nWidth ;
							nTotalWidth += nPartWidth;
						}
					}

					//����Щû���������ﶨ��Ŀ�ȵ�����
					if (aVisualUnDefineWidth != null 
						&& aVisualUnDefineWidth.Count >0)
					{
						//����õ��������ƽ�����
						int nTemp = nInitialWidth
							- nTotalWidth
							- this.TotalRestWidth;

						nPartWidth = nTemp/(aVisualUnDefineWidth.Count);

						for(int i=0;i<aVisualUnDefineWidth.Count ;i++)
						{
							visual = (Visual)aVisualUnDefineWidth[i];

							visual.Layout (0,
								0,
								nPartWidth,
								0,   //�����ĸ߶�
								nTimeStamp,
								out nRetPartWidth,
								out nRetPartHeight,
								LayoutMember.CalcuWidth );   //ֻ����

							nTotalWidth += nRetPartWidth;
						}
					}

					//�㷵�ؿ��
					nTotalWidth += this.TotalRestWidth;
					nRetWidth = (nRetWidth > nTotalWidth) ? nRetWidth : nTotalWidth;
					
					goto END1;
				}

				//*****************************************
				//2.�����ȣ��ֲ�߶�
				//*******************************************
				if (((layoutMember & LayoutMember.CalcuWidth ) == LayoutMember.CalcuWidth )
					&& ((layoutMember & LayoutMember.CalcuHeight) == LayoutMember.CalcuHeight ))
				{
					nTotalWidth = 0;  //�ܿ�ȸ�0
					if (aVisualUnDefineWidth != null)
						aVisualUnDefineWidth.Clear ();
					else
						aVisualUnDefineWidth = new ArrayList ();


					//���߶�
					nMaxHeight = nInitialHeight 
						- this.TotalRestHeight;

					if (this.childrenVisual != null)
					{
						for(int i = 0 ; i < this.childrenVisual.Count ;i++)
						{
							visual = (Visual)childrenVisual[i];
							PartWidth  partWidth = item.GetPartWidth (visual.GetType ().Name );
						
							//û�ҵ����󣬻򼶱��С�ڵ���0���ӵ�δ����������
							if (partWidth == null
								|| partWidth.nGradeNo <= 0)
							{
								aVisualUnDefineWidth.Add (visual);
								continue;
							}

							nPartWidth = partWidth.nWidth ;

							visual.Layout (0,
								0,
								nPartWidth,  //����һ���̶����
								nMaxHeight,
								nTimeStamp,
								out nRetPartWidth,
								out nRetPartHeight,
								LayoutMember.CalcuWidth | LayoutMember.CalcuHeight  );   //ֻ����

							if (nRetPartHeight > nMaxHeight)
								nMaxHeight = nRetPartHeight;

							//�ܿ������?������Ƿ�ᷢ���ı�
							nTotalWidth += nRetPartWidth;
						}
					}

					//����Щû���������ﶨ��Ŀ�ȵ�����
					if (aVisualUnDefineWidth != null && aVisualUnDefineWidth.Count >0)
					{
						int nTemp = nInitialWidth
							- nTotalWidth
							- this.TotalRestWidth;

						nPartWidth = nTemp/(aVisualUnDefineWidth.Count);

						for(int i=0;i<aVisualUnDefineWidth.Count ;i++)
						{
							visual = (Visual)aVisualUnDefineWidth[i];

							visual.Layout (0,
								0,
								nPartWidth,
								nMaxHeight,   //0,   //�����ĸ߶�
								nTimeStamp,
								out nRetPartWidth,
								out nRetPartHeight,
								LayoutMember.CalcuWidth | LayoutMember.CalcuHeight );   //ֻ����

							nTotalWidth += nRetPartWidth;
							if (nRetPartHeight > nMaxHeight)
								nMaxHeight = nRetPartHeight;
						}
					}

					nTotalWidth += this.TotalRestWidth;
					nRetWidth = (nRetWidth > nTotalWidth) ? nRetWidth : nTotalWidth;
					if (nRetWidth < 0)
						nRetWidth = 0;
					
					nMaxHeight += this.TotalRestHeight;;
					nRetHeight = (nRetHeight > nMaxHeight) ? nRetHeight : nMaxHeight;
					if (nRetHeight < 0)
						nRetHeight = 0;
					goto END1;
				}

				//******************************************
				//3.ʵ��
				//********************************************
				if( (layoutMember & LayoutMember.Layout )== LayoutMember.Layout )
				{
					nTotalWidth = 0;  //�ܿ�ȸ�0
					if (aVisualUnDefineWidth != null)
						aVisualUnDefineWidth.Clear ();
					else
						aVisualUnDefineWidth = new ArrayList ();

					//���߶�
					nMaxHeight = nInitialHeight
						- this.TotalRestHeight;

					//���������������ÿ��part�Ŀ��
					ArrayList aWidth = new ArrayList ();

					if (this.childrenVisual != null)
					{
						for(int i = 0 ; i < this.childrenVisual.Count ;i++)
						{
							visual = (Visual)childrenVisual[i];
							PartWidth  partWidth = item.GetPartWidth (visual.GetType ().Name );
						
							//û�ҵ����󣬻򼶱��С�ڵ���0���ӵ�δ����������
							if (partWidth == null
								|| partWidth.nGradeNo <= 0)
							{
								aVisualUnDefineWidth.Add (visual);
								continue;
							}
							nPartWidth = partWidth.nWidth ;
							nTotalWidth += nPartWidth;    //����ӵ�Ŀ����Ϊ���Ժ����

							//�ǵ�������
							partInfo = new PartInfo ();
							partInfo.strName = visual.GetType ().Name ;
							partInfo.nWidth = nPartWidth;
							aWidth.Add(partInfo);
						}
					}

					//����Щû���������ﶨ��Ŀ�ȵ�����
					if (aVisualUnDefineWidth != null && aVisualUnDefineWidth.Count >0)
					{
						//���������û������С��ȼ���
						int nTemp = nInitialWidth
							- nTotalWidth
							- this.TotalRestWidth;

						nPartWidth = nTemp/(aVisualUnDefineWidth.Count);
						//nPartWidth����Ϊ����

						for(int i=0;i<aVisualUnDefineWidth.Count ;i++)
						{
							visual = (Visual)aVisualUnDefineWidth[i];
							nTotalWidth += nPartWidth;

							//�ǵ�������
							partInfo = new PartInfo ();
							partInfo.strName = visual.GetType ().Name ;
							partInfo.nWidth = nPartWidth;
							aWidth.Add(partInfo);
						}
					}

					item.SetValue (this.GetType().Name,nRetWidth);

					//���ݲ�����ʽ����һ��
					int nXDelta = this.LeftResWidth;
					int nYDelta = this.TopResHeight;

					if (this.childrenVisual != null)
					{
						nTotalWidth = 0;
						int i;
						for(i = 0 ; i < childrenVisual.Count ;i++)
						{
							visual = (Visual)childrenVisual[i];
							nPartWidth = GetRememberWidth(aWidth,visual.GetType ().Name );
						
							visual.Layout (0 + nXDelta,
								0 + nYDelta,
								nPartWidth,
								0,
								nTimeStamp,
								out nRetPartWidth,
								out nRetPartHeight,
								LayoutMember.Layout );
						
							nXDelta += visual.Rect.Width ;
							nTotalWidth += visual.Rect.Width ;
							if (visual.Rect.Height > nMaxHeight)
								nMaxHeight = visual.Rect.Height ;
						}

						for(i = 0 ; i < childrenVisual.Count ;i++)
						{
							visual = (Visual)childrenVisual[i];
							if (visual.Rect.Height < nMaxHeight)
								visual.Rect.Height = nMaxHeight;
						}
					}

					nTotalWidth += this.TotalRestWidth;
					nRetWidth = (nRetWidth > nTotalWidth) ? nRetWidth : nTotalWidth;
					if (nRetWidth < 0)
						nRetWidth = 0;
					
					nMaxHeight += this.TotalRestHeight;
					nRetHeight = (nRetHeight > nMaxHeight) ? nRetHeight : nMaxHeight;
					if (nRetHeight < 0)
						nRetHeight = 0;

					//���Լ���rect���
					this.Rect = new Rectangle (x,
						y,
						nRetWidth,
						nRetHeight);

					//goto END1;
				}

			}

			//����
			if (LayoutStyle == LayoutStyle.Vertical  )
			{
				//******************************************
				//1.ֻ����,�õȺ�
				//*******************************************
				if ((layoutMember == LayoutMember.CalcuWidth ))
				{
					nMaxWidth = nInitialWidth
						- this.TotalRestWidth;

					if (nMaxWidth < 0 )
						nMaxWidth = 0;
					if (this.childrenVisual != null)
					{
						for(int i = 0 ; i < this.childrenVisual.Count ;i++)
						{
							visual = (Visual)childrenVisual[i];
							visual.Layout (0,
								0,
								nMaxWidth,
								0,
								nTimeStamp,
								out nRetPartWidth,
								out nRetPartHeight,
								LayoutMember.CalcuWidth );

							if (nRetPartWidth > nMaxWidth)
								nMaxWidth = nRetPartWidth;
						}
					}
					nMaxWidth += this.TotalRestWidth;
					nRetWidth = (nRetWidth > nMaxWidth) ? nRetWidth : nMaxWidth;

					goto END1;
				}

				//*****************************************
				//2.�����ȣ��ֲ�߶�
				//*******************************************
				if (((layoutMember & LayoutMember.CalcuWidth ) == LayoutMember.CalcuWidth )
					&& ((layoutMember & LayoutMember.CalcuHeight) == LayoutMember.CalcuHeight ))
				{
					//�����
					nMaxWidth = nInitialWidth 
						- this.TotalRestWidth;

					nTotalHeight= 0;  //�ܸ߶ȸ�0

					if (this.childrenVisual != null)
					{
						for(int i = 0 ; i < this.childrenVisual.Count ;i++)
						{
							visual = (Visual)childrenVisual[i];
							visual.Layout (0,
								0,
								nMaxWidth,
								0,
								nTimeStamp,
								out nRetPartWidth,
								out nRetPartHeight,
								LayoutMember.CalcuWidth | LayoutMember.CalcuHeight  );

							if (nRetPartWidth > nMaxWidth)
								nMaxWidth = nRetPartWidth;

							nTotalHeight += nRetPartHeight;
						}
					}

					nMaxWidth += this.TotalRestWidth;
					nRetWidth = (nRetWidth > nMaxWidth) ? nRetWidth : nMaxWidth;

					nTotalHeight += this.TotalRestHeight;
					nRetHeight = (nRetHeight > nTotalHeight) ? nRetHeight : nTotalHeight;
					goto END1;
				}

				//******************************************
				//3.ʵ��
				//********************************************
				if( (layoutMember & LayoutMember.Layout )== LayoutMember.Layout )
				{
					//�����
					nMaxWidth = nInitialWidth 
						- this.TotalRestWidth;
					
					nTotalHeight= 0;  //�ܸ߶ȸ�0

					item.SetValue (this.GetType().Name,nRetWidth);

					//���ݲ�����ʽ����һ��
					int nXDelta = this.LeftResWidth;
					int nYDelta = this.TopResHeight;

					if (this.childrenVisual != null)
					{
						for(int i=0 ;i<childrenVisual.Count ;i++)
						{
							visual = (Visual)childrenVisual[i];
							nPartHeight = 0;
						
							visual.Layout (0 + nXDelta ,
								0 + nYDelta,
								nMaxWidth,
								nPartHeight,
								nTimeStamp,
								out nRetPartWidth,
								out nRetPartHeight,
								LayoutMember.Layout  );
				
							nYDelta += visual.Rect.Height;

							if (visual.Rect.Width > nMaxWidth)
								nMaxWidth = visual.Rect.Width;

							nTotalHeight += visual.Rect.Height;
						}

					}
					nMaxWidth += this.TotalRestWidth;
					nRetWidth = (nRetWidth > nMaxWidth) ? nRetWidth : nMaxWidth;
					if (nRetWidth < 0)
						nRetWidth = 0;

					nTotalHeight += this.TotalRestHeight;
					nRetHeight = (nRetHeight > nTotalHeight) ? nRetHeight : nTotalHeight;
					if (nRetHeight < 0)
						nRetHeight = 0;


					//���Լ���rect���
					this.Rect = new Rectangle (x,
						y,
						nRetWidth,
						nRetHeight);


					//goto END1;
				}

				//****************************************
				//4.ֻ��߶�,����������
				//*****************************************
				if (layoutMember == LayoutMember.CalcuHeight)
				{
					//�����
					nMaxWidth = nInitialWidth 
						- this.TotalRestWidth;

					nTotalHeight= 0;  //�ܸ߶ȸ�0
					if (this.childrenVisual != null)
					{
						for(int i = 0 ; i < this.childrenVisual.Count ;i++)
						{
							visual = (Visual)childrenVisual[i];
							visual.Layout (0,
								0,
								nMaxWidth,
								0,
								nTimeStamp,
								out nRetPartWidth,
								out nRetPartHeight,
								LayoutMember.CalcuHeight  );

							if (nRetPartWidth > nMaxWidth)
								nMaxWidth = nRetPartWidth;

							nTotalHeight += nRetPartHeight;
						}
					}
					nTotalHeight += this.TotalRestHeight;
					nRetHeight = (nRetHeight > nTotalHeight) ? nRetHeight : nTotalHeight;

					goto END1;
				}
			}

			if ((layoutMember & LayoutMember.Up  ) == LayoutMember.Up )
			{
				Visual.UpLayout(this,nTimeStamp);
			}

			END1:
				sizeCatch.SetValues (nInitialWidth,
					nInitialHeight,
					nRetWidth,
					nRetHeight,
					nTimeStamp,
					layoutMember);

			if (this.IsWriteInfo == true)
			{
				strTempInfo = "";
				strTempInfo = "\r\n"
					+ strLevelString + "���ǵ�" + nTempLevel + "���" + this.GetType ().Name + "��layout����\r\n" 
					+ strLevelString + "����ֵΪ: \r\n"
					+ strLevelString + "x=" + x + "\r\n"
					+ strLevelString + "y=" + y + "\r\n"
					+ strLevelString + "nRetWidth=" + nRetWidth + "\r\n"
					+ strLevelString + "nRetHeight=" + nRetHeight + "\r\n"
					+ strLevelString + "Rect.X=" + this.Rect.X + "\r\n"
					+ strLevelString + "Rect.Y=" + this.Rect.Y + "\r\n"
					+ strLevelString + "Rect.Width=" + this.Rect.Width + "\r\n"
					+ strLevelString + "Rect.Height=" + this.Rect.Height + "\r\n"
					+ strLevelString + "****************************\r\n\r\n" ;

				StreamUtil.WriteText ("I:\\debug.txt",strTempInfo);
			}	
		}


		//�õ��ڼ��µĿ��
		public int GetRememberWidth(ArrayList aWidth,string strName)
		{
			foreach(PartInfo partInfo in aWidth)
			{
				if (partInfo.strName == strName)
					return partInfo.nWidth;
			}
			return -1;
		}


		public override void Paint(PaintEventArgs pe,
			int nBaseX,
			int nBaseY,
			PaintMember paintMember)
		{
			Rectangle rectPaintThis = new Rectangle (0,0,0,0);
			rectPaintThis = new Rectangle (nBaseX + this.Rect.X,
				nBaseY + this.Rect.Y,
				this.Rect.Width,
				this.Rect.Height);

			if (rectPaintThis.IntersectsWith (pe.ClipRectangle )== false)
				return;

			Brush brush = null;
			Item item = this.GetItem ();
			if (item == null)
				return;
			
			//?
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
			pe.Graphics .FillRectangle (brush,rectPaintThis);

			SKIPDRAWBACK:

				if (editor != null && editor.VisualCfg == null)
				{
				}
				else
				{
					//��DrawLines���߿�
					this.DrawLines(rectPaintThis,
						this.TopBorderHeight,
						this.BottomBorderHeight,
						this.LeftBorderWidth,
						this.RightBorderWidth,
						this.BorderColor);
				}

			if (childrenVisual == null)
				return;

			for(int i=0;i<this.childrenVisual.Count;i++)
			{
				Visual visual = (Visual)(this.childrenVisual[i]);

				Rectangle rectPaintChild = 
					new Rectangle(
					nBaseX + this.Rect.X + visual.Rect.X,
					nBaseY + this.Rect.Y + visual.Rect.Y,
					visual.Rect.Width,
					visual.Rect.Height);

				if (rectPaintChild.IntersectsWith(pe.ClipRectangle ) == true)
				{
					visual.Paint(pe,
						nBaseX + this.Rect.X,
						nBaseY + this.Rect.Y,
						paintMember);
				}


				if (editor != null 
					&& editor.VisualCfg == null)
				{

					if (i == this.childrenVisual.Count-1)
					{
						int nDelta = this.RectAbs.Y + this.Rect.Height 
							- visual.RectAbs.Y - visual.Rect.Height - Visual.BOTTOMBORDERHEIGHT;

						if (nDelta > 0)
						{
							// ���·�����
							this.DrawLines(new Rectangle(rectPaintChild.X,
								rectPaintChild.Y,
								rectPaintChild.Width,
								rectPaintChild.Height + Visual.BOTTOMBORDERHEIGHT),
								0,
								Visual.BOTTOMBORDERHEIGHT,
								0,
								0,
								visual.BorderColor);
						}

					}

					if (i <= 0)
						continue;

			
					if (this.LayoutStyle == LayoutStyle.Vertical)
					{
						// ֻ���Ϸ�����
						this.DrawLines(rectPaintChild,
							visual.TopBorderHeight,
							0,
							0,
							0,
							visual.BorderColor);
					}
					else if (this.LayoutStyle == LayoutStyle.Horizontal)
					{
						this.DrawLines(rectPaintChild,
							0,
							0,
							visual.LeftBorderWidth,
							0,
							visual.BorderColor);
					}
				}
			}
		}


	}

}
