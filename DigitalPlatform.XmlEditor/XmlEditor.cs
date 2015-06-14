using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.InteropServices;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Xml.XPath;

using DigitalPlatform.GUI;
using DigitalPlatform.IO;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.Drawing;

namespace DigitalPlatform.Xml
{
	// XmlEditor��Xml�༭�ؼ�
	public class XmlEditor : System.Windows.Forms.Control
	{
		#region ��Ա����

		// �����Ӿ���ʽ
		public VisualCfg VisualCfg = null;  // ��ʾ����ʽ�����ù�ϵ��
		public ItemWidthList widthList = new ItemWidthList (); // ����һ���ؼ�����Ŀ����Ϣ��
		public LayoutStyle m_layoutStyle = LayoutStyle.Horizontal; // �ؼ�����������ַ��

		// �ڴ����
		public ItemAllocator allocator = new MyItemAllocator();
		public VirtualRootItem VirtualRoot = null;
		internal ElementItem docRoot = null;  //Xml document Item ��

		// ����catch
		public bool bCatch = true;        // layout�׶��Ƿ�Ҫ����ߴ����
		public int nTime = 0;             // layout����
		internal int nTimeStampSeed = 0;  // ʱ���

		// С�ı��༭�ؼ�
		public MyEdit curEdit = new MyEdit();	//edit�ؼ�

		// ȱʡֵ
		public Color BackColorDefault = SystemColors.Control; // ��ͨ���򱳾�ɫ
        public Color AttrBackColorDefault = ColorUtil.String2Color("#BFCAE6");//Color.Green;  //����������ɫ
		public Color BackColorDefaultForEditable = SystemColors.Window; 	// �ɱ༭���򱳾�ɫ
		public Color TextColorDefault = SystemColors.WindowText; // ������ɫ
		public Color BorderColorDefault = SystemColors.ControlDark;    // .ControlText;	// ����߿�������ɫ
		private BorderStyle borderStyle = BorderStyle.Fixed3D;  // �ؼ����ڱ߿�

		// �����϶�
		private int nLastTrackerX = -1;      // ���һ���ϵ���X
		private Visual dragVisual = null;    // �϶���visual

		// �����ĵ��������
		public int nClientWidth = 0;    //�ͻ������
		public int nClientHeight = 0;   //�ͻ����߶�
		public int nDocumentOrgX = 0;   //�ĵ�ƫ����X
		public int nDocumentOrgY = 0;   //�ĵ�ƫ����Y
		private int nAverageItemHeight = 20 ;   // ƽ���������ָ߶ȣ�������������ƶ���ֵ

		// ����
		public Item m_selectedItem = null; // ��ǰѡ�е�Item����
		public XmlText m_curText = null;   // ��ǰ�༭��Text����

		public bool m_bChanged = false;       //�Ƿ����ı�
		bool m_bAllowPaint = true;       // �� BeginUpdate() �� EndUpdate()���� 
		public bool m_bFocused = false;  // �Ƿ����ý���״̬
		private bool bAutoSize = false;      // �Ƿ��Զ����ݴ��ڿͻ�����ȸı��ĵ����

        public event GenerateDataEventHandler GenerateData = null;

		private System.ComponentModel.Container components = null;

		#endregion

		#region ���캯��

		public XmlEditor()
		{
			InitializeComponent();
		}

		#region Component Designer generated code

		private void InitializeComponent()
		{
			// 
			// XmlEditor
			// 
			this.EnabledChanged += new System.EventHandler(this.XmlEditorCtrl_EnabledChanged);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.XmlEditorCtrl_KeyDown);
		}

		#endregion

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		#endregion


        // Ϊ�˽�������,���ش˺���
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                // ����������
                case API.WM_VSCROLL:
                    {
                        switch (API.LoWord(m.WParam.ToInt32()))
                        {
                            case API.SB_BOTTOM:
                                MessageBox.Show("SB_BOTTOM");
                                break;
                            case API.SB_TOP:
                                MessageBox.Show("SB_TOP");
                                break;
                            case API.SB_THUMBTRACK:
                                this.Update();
                                DocumentOrgY = -API.HiWord(m.WParam.ToInt32());
                                break;
                            case API.SB_LINEDOWN:
                                DocumentOrgY -= nAverageItemHeight;
                                break;
                            case API.SB_LINEUP:
                                DocumentOrgY += nAverageItemHeight;
                                break;
                            case API.SB_PAGEDOWN:
                                DocumentOrgY -= this.ClientSize.Height;
                                break;
                            case API.SB_PAGEUP:
                                DocumentOrgY += this.ClientSize.Height;
                                break;
                        }
                    }
                    break;

                // ���ƺ�����
                case API.WM_HSCROLL:
                    {
                        switch (API.LoWord(m.WParam.ToInt32()))
                        {
                            case API.SB_THUMBPOSITION:
                            case API.SB_THUMBTRACK:
                                DocumentOrgX = -API.HiWord(m.WParam.ToInt32());
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
            }
            base.DefWndProc(ref m);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // ��ʼ��Сedit�ؼ�
            string strError = "";
            int nRet = this.curEdit.Initial(this,
                out strError);
            if (nRet == -1)
                throw new Exception(strError);
        }


		// ���Ʊ���ͼ
		protected override void OnPaintBackground(PaintEventArgs e)
		{
            base.OnPaintBackground(e);

			if (this.m_bAllowPaint == false)
				return;

			if (this.VisualCfg == null)
				goto DEFAULT;

			if (this.VisualCfg.strBackPicUrl == "")
				goto DEFAULT;
			
			// ���Ʊ���ͼ��
            Image image = null;
            string strError = "";
            int nRet = DrawingUtil.GetImageFormUrl(this.VisualCfg.strBackPicUrl,
                out image,
                out strError);
            if (nRet == -1)
                throw new Exception(strError);

			this.PaintImageBackground(image);

            image.Dispose();    // 2006/7/26 add

			return;

			DEFAULT:
			{
                Color defaultBackColor = SystemColors.Control;
                Brush brush = new SolidBrush(defaultBackColor);
				e.Graphics.FillRectangle(brush, e.ClipRectangle);
				brush.Dispose();
			}
		}
       
       
        // ���Ʊ���
        public void PaintImageBackground(Image imageFile)
        {
            Graphics graphics = Graphics.FromImage(imageFile);

            if (this.VisualCfg.backPicStyle == BackPicStyle.Tile)
            {
                int nXDelta = 0;
                int nYDelta = 0;

                int nXCount = 0;
                int nClientWidth;
                int nXStart = (this.nDocumentOrgX) % imageFile.Width;

                nXDelta = (-this.nDocumentOrgX) % imageFile.Width;
                nXDelta = imageFile.Width - nXDelta;

                if (nXDelta != 0)
                    nXCount++;

                nClientWidth = this.ClientSize.Width - nXDelta;
                if (nClientWidth > 0)
                {
                    nXCount += nClientWidth / imageFile.Width;
                }
                nXDelta = nClientWidth % imageFile.Width;
                if (nXDelta != 0)
                    nXCount++;


                int nYCount = 0;
                int nClientHeight;
                int nYStart = (this.nDocumentOrgY) % imageFile.Height;
                nYDelta = (-this.nDocumentOrgY) % imageFile.Height;
                nYDelta = imageFile.Height - nYDelta;

                if (nYDelta != 0)
                    nYCount++;

                nClientHeight = this.ClientSize.Height - nYDelta;
                if (nClientHeight > 0)
                {
                    nYCount += nClientHeight / imageFile.Height;
                }
                nYDelta = nClientHeight % imageFile.Height;
                if (nYDelta != 0)
                    nYCount++;

                for (int i = 0; i < nXCount; i++)
                {
                    for (int j = 0; j < nYCount; j++)
                    {
                        graphics.DrawImage(imageFile,
                            i * imageFile.Width + nXStart,
                            j * imageFile.Height + nYStart,
                            imageFile.Width,
                            imageFile.Height);
                    }
                }
            }
            else if (this.VisualCfg.backPicStyle == BackPicStyle.Fill)
            {
                graphics.DrawImage(imageFile,
                    0 + this.nDocumentOrgX,
                    0 + this.nDocumentOrgY,
                    this.DocumentWidth,
                    this.DocumentHeight);
            }
            else if (this.VisualCfg.backPicStyle == BackPicStyle.Center)
            {
                int nX = this.DocumentWidth / 2 - imageFile.Width / 2;
                int nY = this.DocumentHeight / 2 - imageFile.Height / 2;

                graphics.DrawImage(imageFile,
                    nX + this.nDocumentOrgX,
                    nY + this.nDocumentOrgY,
                    imageFile.Width,
                    imageFile.Height);

            }
            graphics.Dispose();
        }
		
		// ���ػ��ƺ���
		protected override void OnPaint(PaintEventArgs pe)
		{
			if (this.m_bAllowPaint == false)
				return;	// �Ż��ٶ�

			if (this.VirtualRoot == null) 
			{
				base.OnPaint(pe);
				return;
			}

			// ����Ԫ�ص�Paint()
			this.VirtualRoot.Paint(pe,
				nDocumentOrgX,
				nDocumentOrgY,
				PaintMember.Both);
		}


		// Ϊ�˴��ϱ߿����ظ�����
		protected override CreateParams CreateParams
		{
			get 
			{
				CreateParams param = base.CreateParams;
				
                //��Ϊ���߿���ʽ
				if (borderStyle == BorderStyle.FixedSingle) 
					param.Style |= API.WS_BORDER;
				else if (borderStyle == BorderStyle.Fixed3D) 
					param.ExStyle |= API.WS_EX_CLIENTEDGE;

				return param;
			}
		}
		
		#region ����Сtextbox control��һЩ����

		// ��Edit�ؼ��е��������ݶ��ֵ�Visual��ͼ��
		// �������޸���Ļͼ��
		internal void EditControlTextToVisual()
		{
			if (this.m_curText != null)
			{
				string strOldValue = this.m_curText.Text;
				if (this.m_curText.Text != this.curEdit.Text)
				{
					this.m_curText.Text = this.curEdit.Text;
					Item item = this.m_curText.GetItem();
					if (item != this.m_selectedItem)
					{
						Debug.Assert(false,"��ǰText��Ӧ��Item��SelectedItem��һ��");
						throw(new Exception("��ǰText��Ӧ��Item��SelectedItem��һ��"));
					}
					if (this.m_selectedItem is ElementItem)
					{
						ElementItem element = (ElementItem)this.m_selectedItem;
						
						if (this.m_curText.Name == "attributes")
							element.m_xmlAttrsTimestamp ++;
						if (this.m_curText.Name == "content")
							element.m_xmlChildrenTimestamp ++;

						element.Flush();	
					}

					if (this.m_selectedItem is AttrItem)
					{
						// �����¼�
						////////////////////////////////////////////////////
						// ItemAttrChanged
						////////////////////////////////////////////////////
						ItemChangedEventArgs args = 
							new ItemChangedEventArgs();
						args.item = this.m_selectedItem;
						args.NewValue = this.m_curText.Text;
						args.OldValue = strOldValue;
						this.fireItemChanged(this,args);
					}

					// �ĵ������仯
					this.FireTextChanged();
				}
			}
		}


		// ����ǰVisual��ͼ��������ݶ��ֵ�Edit�ؼ�
		// �������޸���Ļͼ��
		internal void VisualTextToEditControl()
		{
			if (this.m_curText != null)
			{
				this.curEdit.Text = this.m_curText.Text;
			}
		}


		// ΪSetEditPos()��д��˽�к���
		public void ChangeEditSizeAndMove(XmlText text)
		{
			if (text == null)
			{
				Debug.Assert (false,"ChangeEditSizeAndMove()�������textΪnull");
				return;
			}

			// edit�ľ�size
			Size oldsize = curEdit.Size;

			// edit�������������size
			Size newsize = new Size(0,0);
			newsize = new System.Drawing.Size(
				text.Rect.Width - text.TotalRestWidth,
				text.Rect.Height - text.TotalRestHeight);

			// text����ڴ��ڵľ�������
			Rectangle rectLoc = text.RectAbs;
			rectLoc.Offset(this.nDocumentOrgX ,
				this.nDocumentOrgY);

			// ��edit�������(��textȥ����߱߿���հ�)
			Point loc = new Point(0,0);
			loc = new System.Drawing.Point(
				rectLoc.X + text.LeftResWidth,
				rectLoc.Y + text.TopResHeight);

			// ������������
			// ��С�����moveȻ��ı�size
			if (oldsize.Height < newsize.Height)
			{
				curEdit.Location = loc;
				curEdit.Size = newsize;
			}
			else 
			{
				// �Ӵ��С����sizeȻ��ı�move
				curEdit.Size = newsize;
				curEdit.Location = loc;
			}
			curEdit.Font = text.GetFont();
		}

		
		// ����edit�ؼ�λ�ô�С����ǰ��ͼ����
		public void SetEditPos()
		{
			if (this.m_curText == null)
			{
				//curEdit.Hide();
                curEdit.Size = new Size(0, 0);
                //curEdit.Enabled = false;
				return;
			}

			this.curEdit.Show();
            //this.curEdit.Enabled = true;

			// ��δѡ�������ڵ�,ֱ�ӵ���xmleditor��������ؼ�,
			// Ҳ���CurText����,�Ա�����µ����ݷ����ڴ������
			// ��edit��Ӧ�ٻ�ý���,������������һ������bEditorGetFocus�����������⡣
			//if (this.bEditGetFocus == true)  
			if (this.m_bFocused == true)
				this.curEdit.Focus();
			ChangeEditSizeAndMove(this.m_curText);
		}

		#endregion
	

		#region ���ھ�����ĺ���
		
		// �ĵ��ߴ緢���仯
		internal void AfterDocumentChanged(ScrollBarMember member)
		{
			if (bAutoSize == true) 
			{
				this.ClientSize = new Size(this.DocumentWidth, this.DocumentHeight);
			}
			else 
			{
				SetScrollBars(member);
			}
		}

		// ������
		void SetScrollBars(ScrollBarMember member)
		{
			if (bAutoSize == true) 
			{
				API.ShowScrollBar(this.Handle,
					API.SB_HORZ,
					false);
				API.ShowScrollBar(this.Handle,
					API.SB_VERT,
					false);
				return;
			}

			nClientWidth = this.ClientSize.Width;
			nClientHeight = this.ClientSize.Height;

			if (member == ScrollBarMember.Horz
				|| member == ScrollBarMember.Both) 
			{
				// ˮƽ����
				API.ScrollInfoStruct si = new API.ScrollInfoStruct();
				si.cbSize = Marshal.SizeOf(si);
				si.fMask = API.SIF_RANGE | API.SIF_POS | API.SIF_PAGE;
				si.nMin = 0;
				si.nMax = DocumentWidth ;
				si.nPage = nClientWidth;
				si.nPos = -nDocumentOrgX;
				API.SetScrollInfo(this.Handle, API.SB_HORZ, ref si, true);
			}


			if (member == ScrollBarMember.Vert
				|| member == ScrollBarMember.Both) 
			{
				// ��ֱ����
				API.ScrollInfoStruct si = new API.ScrollInfoStruct();
				si.cbSize = Marshal.SizeOf(si);
				si.fMask = API.SIF_RANGE | API.SIF_POS | API.SIF_PAGE;
				si.nMin = 0;
				si.nMax = DocumentHeight ;
				si.nPage = nClientHeight;
				si.nPos = -nDocumentOrgY;
				API.SetScrollInfo(this.Handle, API.SB_VERT, ref si, true);
			}
		}

		// �ĵ����������
		public int DocumentOrgX
		{
			get 
			{
				return nDocumentOrgX;
			}
			set 
			{
				int nWidth = DocumentWidth ;
				int nViewportWidth = this.ClientSize.Width;

				int nDocumentOrgX_old = nDocumentOrgX;

				if (nViewportWidth >= nWidth)
					nDocumentOrgX = 0;
				else 
				{
					if (value <= - nWidth + nViewportWidth)
						nDocumentOrgX = -nWidth + nViewportWidth;
					else
						nDocumentOrgX = value;

					if (nDocumentOrgX > 0)
						nDocumentOrgX = 0;
				}

				SetEditPos();
				AfterDocumentChanged(ScrollBarMember.Horz);


				int nDelta = nDocumentOrgX - nDocumentOrgX_old;
				if ( nDelta != 0 ) 
				{
					RECT rect1 = new RECT();
					rect1.left = 0;
					rect1.top = 0;
					rect1.right = this.ClientSize.Width;
					rect1.bottom = this.ClientSize.Height;

					API.ScrollWindowEx(this.Handle,
						nDelta,
						0,
						ref rect1,
						IntPtr.Zero,	//	ref RECT lprcClip,
						0,	// int hrgnUpdate,
						IntPtr.Zero,	// ref RECT lprcUpdate,
						API.SW_INVALIDATE /*| API.SW_SCROLLCHILDREN*/ /*int fuScroll*/);
				}
				//this.Invalidate();
			}
		}

		// �ĵ�����ƫ����
		public int DocumentOrgY
		{
			get 
			{
				return nDocumentOrgY;
			}
			set 
			{
				int nHeight = DocumentHeight ;
				int nViewportHeight = this.ClientSize.Height;

				int nDocumentOrgY_old = nDocumentOrgY;

				if (nViewportHeight >= nHeight)
					nDocumentOrgY = 0;
				else 
				{
					if (value <= - nHeight + nViewportHeight)
						nDocumentOrgY = -nHeight + nViewportHeight;
					else
						nDocumentOrgY = value;

					if (nDocumentOrgY > 0)
						nDocumentOrgY = 0;
				}

				SetEditPos();
				AfterDocumentChanged(ScrollBarMember.Vert);

				int nDelta = nDocumentOrgY - nDocumentOrgY_old;
				if ( nDelta != 0 ) 
				{
					RECT rect1 = new RECT();
					rect1.left = 0;
					rect1.top = 0;
					rect1.right = this.ClientSize.Width;
					rect1.bottom = this.ClientSize.Height;

					API.ScrollWindowEx(this.Handle,
						0,
						nDelta,
						ref rect1,
						IntPtr.Zero,	//	ref RECT lprcClip,
						0,	// int hrgnUpdate,
						IntPtr.Zero,	// ref RECT lprcUpdate,
						API.SW_INVALIDATE /*| API.SW_SCROLLCHILDREN*/ /*int fuScroll*/);
				}
				//this.Invalidate();
			}
		}

		// ��visual���rectCaret�ߴ�ɼ�
		private void EnsureVisible(Visual visual,
			Rectangle rectCaret)
		{
			if (visual == null)
				return;

			int nDelta = visual.RectAbs.Y + visual.Rect.Height
				+ this.nDocumentOrgX 
				+ rectCaret.Y;

			if (nDelta + rectCaret.Height >= this.ClientSize.Height) 
			{
				if (rectCaret.Height >= this.ClientSize.Height) 
					DocumentOrgY = DocumentOrgY - (nDelta + rectCaret.Height) + ClientSize.Height + /*����ϵ��*/ (rectCaret.Height/2) - (this.ClientSize.Height/2);
				else
					DocumentOrgY = DocumentOrgY - (nDelta + rectCaret.Height) + ClientSize.Height;
			}
			else if (nDelta < 0)
			{
				if (rectCaret.Height >= this.ClientSize.Height) 
					DocumentOrgY = DocumentOrgY - (nDelta) - /*����ϵ��*/ ( (rectCaret.Height/2) - (this.ClientSize.Height/2));
				else 
					DocumentOrgY = DocumentOrgY - (nDelta);
			}
			else 
			{
				// y����Ҫ���
			}

			////
			// ˮƽ����
			nDelta = 0;

			nDelta = visual.RectAbs .X + visual.Rect.Width 
				+ this.nDocumentOrgX 
				+ rectCaret.X ;
			

			if (nDelta + rectCaret.Width >= this.ClientSize.Width) 
			{
				if (rectCaret.Width >= this.ClientSize.Width) 
					DocumentOrgX = DocumentOrgX - (nDelta + rectCaret.Width) + ClientSize.Width + /*����ϵ��*/ (rectCaret.Width/2) - (this.ClientSize.Width/2);
				else
					DocumentOrgX = DocumentOrgX - (nDelta + rectCaret.Width) + ClientSize.Width;
			}
			else if (nDelta < 0)
			{
				if (rectCaret.Width >= this.ClientSize.Width) 
					DocumentOrgX = DocumentOrgX - (nDelta) - /*����ϵ��*/ ( (rectCaret.Width/2) - (this.ClientSize.Width/2));
				else 
					DocumentOrgX = DocumentOrgX - (nDelta);
			}
			else 
			{
				// x����Ҫ���
			}

		}


		#endregion

		#region overrideһЩ�¼�

		// ��갴�µ��������
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (this.VirtualRoot == null)
			{
				base.OnMouseDown(e);
				return;
			}

			this.Capture = true;

			Point p = new Point(e.X, e.Y);
			//��������doucment������
			p = new Point (p.X - this.nDocumentOrgX ,
				p.Y - this.nDocumentOrgY );

			Visual visual = null;
			int nRet = this.VirtualRoot.HitTest(p,out visual);

			if (visual == null)
				goto FINISH;
			if (nRet == -1)
				goto FINISH;

			Item item = visual.GetItem();



			//**************************************
			//������չ����ť
			//***************************************
			ExpandStyle expandChildren = ExpandStyle.None;
			ExpandStyle expandAttrs = ExpandStyle.None;

			ExpandStyle oldChildrenExpand = ExpandStyle.None;
			ExpandStyle oldAttrsExpand = ExpandStyle.None;

			if (visual.IsExpandHandle() == true)
			{
				this.EditControlTextToVisual();

				ElementItem element = (ElementItem)item;	// ��Ȼ�ǿ�չ���ģ����Ȼ��ElementItem

				expandChildren = element.m_childrenExpand;
				expandAttrs = element.m_attrsExpand;

				//3.����չ����ť�������жϳ����ﻻ��״̬
				ExpandHandle myExpandHandle = ((ExpandHandle)visual);
				if (myExpandHandle.Name == "ExpandContent")
				{
					if (expandChildren == ExpandStyle.None)
					{
						Debug.Assert(false, "");
					}
					else 
					{
						expandChildren = (expandChildren == ExpandStyle.Expand) ? ExpandStyle.Collapse : ExpandStyle.Expand;
					}
				}
				else if (myExpandHandle.Name == "ExpandAttributes")
				{
					if (expandAttrs == ExpandStyle.None)
					{
						Debug.Assert(false, "");
					}
					else 
					{
						expandAttrs = (expandAttrs == ExpandStyle.Expand) ? ExpandStyle.Collapse : ExpandStyle.Expand;
					}
				}

				
				oldChildrenExpand = element.m_childrenExpand;
				oldAttrsExpand = element.m_attrsExpand;

				element.ExpandAttrsOrChildren(expandAttrs,
					expandChildren,
					true);


				goto END1;
			}

			//*****************************************
			//�ڷ���
			//*****************************************
			if (nRet == 2) 
			{
				dragVisual = visual;

				//-----------------------------------------
				//�������ã���˼�ǰ��϶���visualʧЧ���Ӷ���ɺ�ɫ
				dragVisual.bDrag = true;
				Rectangle rectTemp = new Rectangle (dragVisual.RectAbs.X + this.nDocumentOrgX ,
					dragVisual.RectAbs .Y + this.nDocumentOrgY ,
					dragVisual.RectAbs .Width ,
					dragVisual.RectAbs .Height);
				this.Invalidate ( rectTemp);
				//------------------------------------------

				// ��һ��
				nLastTrackerX = e.X;
				DrawTracker();
				goto END1;
			}

			END1:

				//m_clickVisual = visual;
				if (visual != null )
				{
					//ʧЧǰһ��
					if (this.m_selectedItem != null)
					{
						Rectangle rectTemp = new Rectangle (
							this.m_selectedItem.RectAbs.X + this.nDocumentOrgX ,
							this.m_selectedItem.RectAbs .Y + this.nDocumentOrgY ,
							this.m_selectedItem.RectAbs .Width ,
							this.m_selectedItem.RectAbs .Height);
						this.Invalidate (rectTemp);
					}

					this.EditControlTextToVisual();

					//this.m_selectedItem = item;
					if (visual is XmlText)
					{
						this.SetCurText(item,(XmlText)visual);
					}
					else
					{
						this.SetCurText(item,null);
					}
					this.SetActiveItem(item);

					if (this.m_selectedItem != null)
					{
						Rectangle rectTemp = new Rectangle (
							this.m_selectedItem.RectAbs.X + this.nDocumentOrgX ,
							this.m_selectedItem.RectAbs .Y + this.nDocumentOrgY ,
							this.m_selectedItem.RectAbs .Width ,
							this.m_selectedItem.RectAbs .Height);
						this.Invalidate (rectTemp);
					}
				}

			//���ı���
			if ((visual is XmlText) &&  (nRet == 0))
			{
				//����ģ�ⵥ��һ�£���λ������һ���
				curEdit.Focus();

				int x = e.X - curEdit.Location.X;
				int y = e.Y - curEdit.Location.Y;

				API.SendMessage(curEdit.Handle, 
					API.WM_LBUTTONDOWN, 
					new UIntPtr(API.MK_LBUTTON),	//	UIntPtr wParam,
					API.MakeLParam(x,y));
			
			}

			FINISH:
				base.OnMouseDown (e);
		}


		// �Ҽ��˵�
		private void PopupMenu(Point p)
		{
			ContextMenu contextMenu = new ContextMenu();

			MenuItem menuItem;
			MenuItem subMenuItem;

			string strName = "''";

			Item item = this.m_selectedItem;
			ElementItem element = null;
				
			if (item is ElementItem)
				element = (ElementItem)item;

			if (item != null)
				strName = "'" + item.Name  + "'";

			// չ��
			menuItem = new MenuItem("չ��");
			contextMenu.MenuItems.Add(menuItem);
			if (element != null)
				menuItem.Enabled = true;
			else
				menuItem.Enabled = false;


			// չ������
			subMenuItem = new MenuItem("����");
			subMenuItem.Click += new System.EventHandler(this.menuItem_ExpandAttrs);
			menuItem.MenuItems.Add(subMenuItem);
			if (element != null
				&& element != this.VirtualRoot
				&& element.m_attrsExpand == ExpandStyle.Collapse)
			{
				subMenuItem.Enabled = true;
			}
			else
				subMenuItem.Enabled = false;


			// չ���¼�
			subMenuItem = new MenuItem("�¼�");
			subMenuItem.Click += new System.EventHandler(this.menuItem_ExpandChildren);
			menuItem.MenuItems.Add(subMenuItem);
			if (element != null
				&& element.m_childrenExpand == ExpandStyle.Collapse)
			{
				subMenuItem.Enabled = true;
			}
			else
				subMenuItem.Enabled = false;


			// ����
			menuItem = new MenuItem("����");
			contextMenu.MenuItems.Add(menuItem);
			if (element != null)
				menuItem.Enabled = true;
			else
				menuItem.Enabled = false;


			// ��������
			subMenuItem = new MenuItem("����");
			subMenuItem.Click += new System.EventHandler(this.menuItem_CollapseAttrs);
			menuItem.MenuItems.Add(subMenuItem);
			if (element != null
				&& element != this.VirtualRoot
				&& element.m_attrsExpand == ExpandStyle.Expand)
			{
				subMenuItem.Enabled = true;
			}
			else
				subMenuItem.Enabled = false;


			// �����¼�
			subMenuItem = new MenuItem("�¼�");
			subMenuItem.Click += new System.EventHandler(this.menuItem_CollapseChildren);
			menuItem.MenuItems.Add(subMenuItem);
			if (element != null
				&& element.m_childrenExpand == ExpandStyle.Expand)
			{
				subMenuItem.Enabled = true;
			}
			else
				subMenuItem.Enabled = false;

			//--------------
			menuItem = new MenuItem ("-");
			contextMenu.MenuItems .Add (menuItem);

			// ��������
			menuItem = new MenuItem("��������");
			contextMenu.MenuItems.Add(menuItem);
			if ((element != null && element != this.VirtualRoot)
				|| item is AttrItem )
			{
				menuItem.Enabled = true;
			}
			else
				menuItem.Enabled = false;


			//1.������
			subMenuItem = new MenuItem("������(β��)");// + strName);
			subMenuItem.Click += new System.EventHandler(this.menuItem_AppendAttr);
			menuItem.MenuItems.Add(subMenuItem);
			if (( element != null && element != this.VirtualRoot)
				|| item is AttrItem)
			{
				subMenuItem.Enabled = true;
			}
			else
				subMenuItem.Enabled = false;


			//2.ǰ��
			subMenuItem = new MenuItem("ǰ��");// + strName );
			subMenuItem.Click += new System.EventHandler(this.menuItem_InsertSiblingAttr);
			menuItem.MenuItems.Add(subMenuItem);
			if (item is AttrItem)
				subMenuItem.Enabled = true;
			else
				subMenuItem.Enabled = false;

			//--------------
			menuItem = new MenuItem ("-");
			contextMenu.MenuItems .Add (menuItem);


			// ���¼�
			menuItem = new MenuItem("���¼�");
			contextMenu.MenuItems.Add(menuItem);
			if (element != null)
			{
				menuItem.Enabled = true;
			}
			else
				menuItem.Enabled = false;

			//3.��
			subMenuItem = new MenuItem("Ԫ��");// + strName );
			subMenuItem.Click += new System.EventHandler(this.menuItem_AppendChild);
			menuItem.MenuItems.Add(subMenuItem);
			if (element != null)
			{
				if(element == this.VirtualRoot)
				{
					if (element.children.Count == 0)
						subMenuItem.Enabled = true;
					else
						subMenuItem.Enabled = false;
				}
				else
				{
					subMenuItem.Enabled = true;
				}
			}
			else
				subMenuItem.Enabled = false;


			//4.�ı�
			subMenuItem = new MenuItem("�ı�");// + strName );
			subMenuItem.Click += new System.EventHandler(this.menuItem_AppendText);
			menuItem.MenuItems.Add(subMenuItem);
			if (element != null && element != this.VirtualRoot)
			{
				bool bText = false;
				int nCount = element.children.Count;
				if (nCount > 0 && (element.children[nCount-1] is TextItem))
					bText = true;

				if (bText == true)
					subMenuItem.Enabled = false;
				else
					subMenuItem.Enabled = true;
			}
			else
			{
				subMenuItem.Enabled = false;
			}


			menuItem = new MenuItem("��ͬ��");
			contextMenu.MenuItems.Add(menuItem);
			if ((element != null && element != this.VirtualRoot && element != this.docRoot)
				|| item is TextItem)
			{
				menuItem.Enabled = true;
			}
			else
				menuItem.Enabled = false;


			//5.��ͬ��Ԫ��
			subMenuItem = new MenuItem("Ԫ��");// + strName );
			subMenuItem.Click += new System.EventHandler(this.menuItem_InsertSiblingChild);
			menuItem.MenuItems.Add(subMenuItem);
			if ((element != null && element != this.VirtualRoot && element != this.docRoot)
				|| item is TextItem)
			{
				subMenuItem.Enabled = true;
			}
			else
				subMenuItem.Enabled = false;

			//6.��ͬ���ı�
			subMenuItem = new MenuItem("�ı�");// + strName );
			subMenuItem.Click += new System.EventHandler(this.menuItem_InsertSiblingText);
			menuItem.MenuItems.Add(subMenuItem);
			if ((element != null && element != this.VirtualRoot && element != this.docRoot))
			{
				Item frontItem = ItemUtil.GetNearItem(element,
					MoveMember.Front );

				//ǰ�󶼲����ı��ڵ�ʱ��Ч
				if (!(frontItem is TextItem ))
				{
					subMenuItem.Enabled = true;
				}
				else
				{
					subMenuItem.Enabled = false;
				}
			}
			else
			{
				subMenuItem.Enabled = false;
			}

			//����һ���½���Ԫ������

			//�½���Ԫ��
			menuItem = new MenuItem("�½���Ԫ��");
			menuItem.Click += new System.EventHandler(this.menuItem_CreateRoot);
			contextMenu.MenuItems.Add(menuItem);
			if (this.VirtualRoot == null)
				menuItem.Enabled = true;
			else
				menuItem.Enabled = false;


			//-------
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			//7.ɾ��
			menuItem = new MenuItem("ɾ��");// + strName);
			menuItem.Click += new System.EventHandler(this.menuItem_Delete);
			contextMenu.MenuItems.Add(menuItem);
			if (item != null)
				menuItem.Enabled = true;
			else
				menuItem.Enabled = false;

			//--------------
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			// ����
			menuItem = new MenuItem("����");// + strName);
			menuItem.Click += new System.EventHandler(this.menuItem_Cut);
			contextMenu.MenuItems.Add(menuItem);
			if (item != null)
				menuItem.Enabled = true;
			else
				menuItem.Enabled = false;

			//����
			menuItem = new MenuItem("����");// + strName);
			menuItem.Click += new System.EventHandler(this.menuItem_Copy);
			contextMenu.MenuItems.Add(menuItem);
			if (item != null)
				menuItem.Enabled = true;
			else
				menuItem.Enabled = false;


			//ճ������
			menuItem = new MenuItem("ճ������");// + strName);
			menuItem.Click += new System.EventHandler(this.menuItem_PasteOver);
			contextMenu.MenuItems.Add(menuItem);
			IDataObject ido = Clipboard.GetDataObject();
			if (ido.GetDataPresent(DataFormats.Text))
				menuItem.Enabled = true;
			else
				menuItem.Enabled = false;

			//ճ������
			menuItem = new MenuItem("ճ������");
			contextMenu.MenuItems.Add(menuItem);
			if (ido.GetDataPresent(DataFormats.Text) && item != null)
				menuItem.Enabled = true;
			else
				menuItem.Enabled = false;


			subMenuItem = new MenuItem("ͬ��ǰ��");// + strName );
			subMenuItem.Click += new System.EventHandler(this.menuItem_PasteInsert_InsertBefore);
			menuItem.MenuItems.Add(subMenuItem);
			if (ido.GetDataPresent(DataFormats.Text)
				&& item != null
				&& item != this.VirtualRoot
				&& item != this.docRoot)
			{
				subMenuItem.Enabled = true;
			}
			else
				subMenuItem.Enabled = false;

			subMenuItem = new MenuItem("ͬ�����");
			subMenuItem.Click += new System.EventHandler(this.menuItem_PasteInsert_InsertAfter);
			menuItem.MenuItems.Add(subMenuItem);
			if (ido.GetDataPresent(DataFormats.Text)
				&& item != null
				&& item != this.VirtualRoot
				&& item != this.docRoot)
			{
				subMenuItem.Enabled = true;
			}
			else
				subMenuItem.Enabled = false;

			subMenuItem = new MenuItem("�¼�ĩβ");
			subMenuItem.Click += new System.EventHandler(this.menuItem_PasteInsert_AppendChild);
			menuItem.MenuItems.Add(subMenuItem);
			if (ido.GetDataPresent(DataFormats.Text) && element != null)
			{
				subMenuItem.Enabled = true;
			}
			else
				subMenuItem.Enabled = false;



			//-------
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);



			menuItem = new MenuItem("����");
			contextMenu.MenuItems.Add(menuItem);

			//���Ų���
			subMenuItem = new MenuItem("����");
			subMenuItem.Click += new System.EventHandler(this.menuItem_Horz);
			menuItem.MenuItems.Add(subMenuItem);
			if (this.LayoutStyle == LayoutStyle.Vertical)
				subMenuItem.Enabled = true;
			else
				subMenuItem.Enabled = false;
				


			//���Ų���
			subMenuItem = new MenuItem("����");
			subMenuItem.Click += new System.EventHandler(this.menuItem_Vert);
			menuItem.MenuItems.Add(subMenuItem);
			if (this.LayoutStyle == LayoutStyle.Horizontal)
				subMenuItem.Enabled = true;
			else
				subMenuItem.Enabled = false;



			//-------
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			//�Ż����ֿռ�
			menuItem = new MenuItem("�Ż����ֿռ�");
			menuItem.Click += new System.EventHandler(this.menuItem_YuHua);
			contextMenu.MenuItems.Add(menuItem);
			if (element != null)
				menuItem.Enabled = true;
			else
				menuItem.Enabled = false;


			//-------
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("Properties");
			menuItem.Click += new System.EventHandler(this.menuItem_Properties);
			contextMenu.MenuItems.Add(menuItem);
			if (item != null)
				menuItem.Enabled = true;
			else
				menuItem.Enabled = false;
			/*
						//-------
						menuItem = new MenuItem("-");
						contextMenu.MenuItems.Add(menuItem);

						menuItem = new MenuItem("Flush");
						menuItem.Click += new System.EventHandler(this.menuItem_Flush);
						contextMenu.MenuItems.Add(menuItem);
						if (seletectedElement != null)
							menuItem.Enabled = true;
						else
							menuItem.Enabled = false;
			*/				
/*
			//-------
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			menuItem = new MenuItem("����EnsureVisible");
			menuItem.Click += new System.EventHandler(this.menuItem_test_EnsureVisible);
			contextMenu.MenuItems.Add(menuItem);

*/
			contextMenu.Show(this, p);
		}


		// ��ק�ſ�ʱ
		private void DragUp()
		{
			this.Capture = false;

			if (dragVisual != null) 
			{
				// ���������һ��
				DrawTracker();
				Item item = dragVisual.GetItem ();

				//��������ĵ���x
				//��ת������Դ��ڵ�x
				int x0 = dragVisual.getAbsX() + dragVisual.Rect.Width;
				x0 += this.nDocumentOrgX ;
				
				// ������
				int delta = nLastTrackerX - x0;
				if (item != null)
				{
					int nTemp = dragVisual.Rect.Width + delta;
					if (nTemp <= 0)
						nTemp = 2;
					//���¿���赽���������ֵ�Ĺ����У���Ѽ��������
					item.SetValue(dragVisual.GetType().Name,
						nTemp);

					PartWidth partWidth = item.GetPartWidth (dragVisual.GetType ().Name );
					if (partWidth != null )
					{
						if (partWidth.nGradeNo >0)
						{
							//������ȼ���Ŵ���0ʱ�������еļ������������µ��϶��̶����ʱ������Ӱ���������
							ItemWidth itemWidth = this.widthList .GetItemWidth (item.GetLevel());
							foreach(PartWidth part in itemWidth )
							{
								part.UpGradeNo ();
							}
						}
						else
						{
							partWidth.UpGradeNo ();
						}
					}
					
					//�ı�visual�Ŀ��
					dragVisual.Rect.Width = nTemp;// dragVisual.rect .Width + delta;

					//�Ӷ�layout���¼�
					int nWidth,nHeight;
					dragVisual.Layout(dragVisual.Rect.X,
						dragVisual.Rect.Y,
						dragVisual.Rect.Width,
						dragVisual.Rect.Height,
						nTimeStampSeed,
						out nWidth,
						out nHeight,
						LayoutMember.Layout | LayoutMember.Up );
						
					Visual tempContainer = dragVisual.container ;
					if (tempContainer != null)
					{
						//MessageBox.Show (tempContainer.rect .ToString ());
					}

					nTimeStampSeed++;
				{
					//�Ƚ���ǰ����ļ���Ž���ȱʡֵ
					partWidth = item.GetPartWidth (dragVisual.GetType ().Name );
					if (partWidth != null)
						partWidth.BackDefaultGradeNo ();

					//�����еĿ�ȵļ���Ž���ȱʡֵ
					ItemWidth itemWidth = this.widthList .GetItemWidth (item.GetLevel());
					foreach(PartWidth part in itemWidth )
					{
						part.BackDefaultGradeNo  ();
					}
				}

					//curText������Item�ڱ��仯�ķ�Χ�ڲ����裬���ı��п�����е�Item��Ӱ�쵽�ˣ���������������ж���
					SetEditPos();

					//�ĵ��ߴ�仯����һЩ�ƺ�����
					this.AfterDocumentChanged (ScrollBarMember.Both );

					this.Invalidate ();
				}
				dragVisual.bDrag = false;
				this.dragVisual = null;

				nLastTrackerX = -1;
			}
		}
	

		// ���ſ����������
		protected override void OnMouseUp(MouseEventArgs e)
		{
			if(e.Button == MouseButtons.Right)
			{	
				PopupMenu(new Point(e.X, e.Y) );
			}
			else 
			{
				this.DragUp();
			}
			//END1:
			base.OnMouseUp(e);
		}
		
		private void DragMove(Point p)
		{
			if (this.VirtualRoot == null)
				return;

			if (dragVisual != null) 
			{
				Cursor = Cursors.SizeWE;
				// ���ϴβ����һ��
				DrawTracker();
				nLastTrackerX = p.X;
				// ���Ʊ��ε�һ��
				DrawTracker();
			}
			else 
			{
				//�õ��൱�ĵ�������
				p = new Point (p.X - this.nDocumentOrgX ,
					p.Y - this.nDocumentOrgY );

				Visual visual = null;
				int nRet = -1;
				nRet = this.VirtualRoot.HitTest(p, out visual); //HitT

				if (nRet == 0 && visual is XmlText )  //��һ��������û������
					Cursor = Cursors.IBeam;
				else if (nRet == 2)
					Cursor = Cursors.SizeWE;
				else
					Cursor = Cursors.Arrow;
			}
		}
		

		// ����ƶ�
		protected override void OnMouseMove(MouseEventArgs e)
		{
			this.DragMove(new Point(e.X, e.Y));
			base.OnMouseMove(e);
		}

		// Ϊ�϶����ߺۼ��ĺ���
		private void DrawTracker()
		{
			Point p1 = new Point(nLastTrackerX,0);
			p1 = this.PointToScreen(p1);

			Point p2 = new Point(nLastTrackerX, this.ClientSize.Height);
			p2 = this.PointToScreen(p2);

			ControlPaint.DrawReversibleLine(p1,
				p2,
				SystemColors.Control);
		}

		
		// ������,��editControl����
		public void MyOnMouseWheel(MouseEventArgs e)
		{
			int numberOfTextLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
			int numberOfPixelsToMove = numberOfTextLinesToMove * (int)this.FontTextDefault.GetHeight();

			DocumentOrgY += numberOfPixelsToMove;

			// base.OnMouseWheel(e);
		}


		// �ͻ��˳ߴ�仯
		protected override void OnSizeChanged(System.EventArgs e)
		{
			if (this.ClientSize .Width -1 > this.DocumentWidth )
			{
				int nRetWidth,nRetHeight;
				if (this.VirtualRoot != null) 
				{
					this.VirtualRoot.Layout (0,
						0,
						this.ClientSize .Width -1,
						0,
						nTimeStampSeed++,
						out nRetWidth,
						out nRetHeight,
						LayoutMember.Layout );

					this.Invalidate ();
				}

			}

			SetScrollBars(ScrollBarMember.Both);
			DocumentOrgY = DocumentOrgY;
			DocumentOrgX = DocumentOrgX;
			base.OnSizeChanged(e);
		}
		
		// EnabledChanged
		private void XmlEditorCtrl_EnabledChanged(object sender, System.EventArgs e)
		{
			if (this.Enabled == false)
			{
				this.SetCurText(null,null);
			}
			else
			{
				this.SetCurText(this.m_selectedItem,null);
			}
		}

		// OnGetFocus
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			this.m_bFocused = true;
			this.curEdit.Focus();
		}

		// OnLostFocus
		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			this.m_bFocused = false;
		}

		// ���̰��£����������ƶ�
		private void XmlEditorCtrl_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			switch (e.KeyCode) 
			{
				case Keys.Up:
				{

					//�õ���ǰ��Item����һ��Item
					Item frontItem = ItemUtil.GetNearItem (this.m_selectedItem,
						MoveMember.Front );

					if (frontItem != null)
					{
						//��Ϊ��ǰ��Item
						//this.m_selectedItem = frontItem;
						this.SetCurText(frontItem,null);
						this.SetActiveItem(frontItem);


						e.Handled = true;
						this.Invalidate();
					}
				}
					break;
				case Keys.Down:
				{

					//�õ���һ��Item
					Item behindItem = ItemUtil.GetNearItem (this.m_selectedItem,
						MoveMember.Behind );

					if (behindItem != null)
					{
						//this.m_selectedItem = behindItem;
						this.SetCurText(behindItem,null);
						this.SetActiveItem(behindItem);


						e.Handled = true;

						this.Invalidate();

					}

				}
					break;
				case Keys.Left:
					break;
				case Keys.Right:
					break;
				default:
					break;
			}
		}
		#endregion


        public void OnGenerateData(GenerateDataEventArgs e)
        {
            if (this.GenerateData != null)
                this.GenerateData(this, e);
        }

		#region �޸��ڴ�������� �� �Ҽ��˵�

		// �Ż����ֿ�
		private void menuItem_YuHua(object sender,
			System.EventArgs e)
		{ 
			Item item = this.m_selectedItem;
			if (item == null)
			{
				MessageBox.Show("δѡ������");
				return;
			}

			if (!(item is ElementItem))
			{
				MessageBox.Show("��ǰѡ�нڵ����Ͳ�ƥ�䣬������ElementItem����");
				return;
			}

			ElementItem element = (ElementItem)item;
			element.YouHua();
		}

		#region չ������

		private void menuItem_ExpandChildren(object sender, EventArgs e)
		{
			this.menuItem_Expand(true,ExpandStyle.Expand);
		}
		private void menuItem_ExpandAttrs(object sender, EventArgs e)
		{
			this.menuItem_Expand(false,ExpandStyle.Expand);
		}
		private void menuItem_CollapseChildren(object sender, EventArgs e)
		{
			this.menuItem_Expand(true,ExpandStyle.Collapse);
		}
		private void menuItem_CollapseAttrs(object sender, EventArgs e)
		{
			this.menuItem_Expand(false,ExpandStyle.Collapse);
		}

		public void menuItem_Expand(bool bChildren,
			ExpandStyle expandStyle)
		{
			if (this.m_selectedItem == null)
			{
				MessageBox.Show(this, "��δѡ��ѡ��Ԫ�ؽڵ�");
				return;
			}
			if (!(this.m_selectedItem is ElementItem))
			{
				MessageBox.Show(this, "��ѡ��Ԫ�ؽڵ����չ������");
				return;
			}

			if (bChildren == false)
			{
				this.ExpandAttrs((ElementItem)this.m_selectedItem,
					expandStyle);
			}
			else
			{
				this.ExpandChildren((ElementItem)this.m_selectedItem,
					expandStyle);
			}
		}

		public void ExpandAttrs(ElementItem element,
			ExpandStyle expandStyle)
		{
			element.ExpandAttrsOrChildren(expandStyle,
				element.m_childrenExpand, 
				true);
		}

		public void ExpandChildren(ElementItem element,
			ExpandStyle expandStyle)
		{

			element.ExpandAttrsOrChildren(element.m_attrsExpand, 
				expandStyle,
				true);
		}



		#endregion

		#region Flush()ģ��

		public void Flush()
		{
			this.SetCurText(this.m_selectedItem,this.m_curText);

			if (this.m_selectedItem == null)
			{
				return;
			}

			if ((this.m_selectedItem is ElementItem))
			{
				((ElementItem)this.m_selectedItem).Flush();
			}
		}

		// �� -- Flush
		void menuItem_Flush(object sender,
			System.EventArgs e)
		{
			this.Flush();
		}


		#endregion

		#region Item����

		public void ShowProperties()
		{
			if (this.m_selectedItem == null)
			{
				Debug.Assert (false,"����ѡ������");
				return;
			}

			string strText = "";

			strText += "Changed=" +this.Changed.ToString() + "\r\n";


			strText += "Name=[" + Convert.ToString(this.m_selectedItem.Name)+ "]\r\n";
			if (!(this.m_selectedItem is ElementItem))
				strText += "Value=[" + this.m_selectedItem.Value + "]\r\n";

			//strText += "OuterXml=[" + Convert.ToString(this.m_selectedItem.OuterXml)+ "]\r\n\r\n";
			
			if (this.m_selectedItem is ElementItem)
			{
				ElementItem element = (ElementItem)this.m_selectedItem;

				strText += "AttrsExpand=[" + Convert.ToString(element.AttrsExpand) + "]\r\n";
				strText += "ChildrenExpand=[" + Convert.ToString(element.ChildrenExpand) + "]\r\n\r\n";
				/*
								strText += "NamespaceURI='" + element.NamespaceURI + "'\r\n\r\n";
				*/
				strText += "m_xmlAttrsTimestamp=[" + Convert.ToString(element.m_xmlAttrsTimestamp)+ "]\r\n";
				strText += "m_objAttrsTimestamp=[" + Convert.ToString(element.m_objAttrsTimestamp)+ "]\r\n\r\n";

				strText += "m_xmlChildrenTimestamp=[" + Convert.ToString(element.m_xmlChildrenTimestamp)+ "]\r\n";
				strText += "m_objChildrenTimestamp=[" + Convert.ToString(element.m_objChildrenTimestmap)+ "]\r\n\r\n";

			}

			if (this.m_selectedItem is AttrItem)
			{
				AttrItem attr = (AttrItem)this.m_selectedItem;
				strText += "NamespaceURI=" + attr.NamespaceURI + "\r\n";
			}





			PropertyDlg dlg = new PropertyDlg();
			dlg.textBox_message.Text = strText;
			dlg.ShowDialog(this);
		}
		
		// �� -- Properties
		void menuItem_Properties(object sender,
			System.EventArgs e)
		{
			this.ShowProperties();

		}

		void menuItem_test_EnsureVisible(object sender,
			System.EventArgs e)
		{
			Item item = this.docRoot.children[this.docRoot.children.Count -1];
			Rectangle rect = new Rectangle(0,
				0,
				0,
				0);
			this.EnsureVisible(item,rect);

		}
		#endregion

		#region �������Բ���

		// parameter:
		//		strFullName: ���Դ�ǰ׺ prefix:name
		//		strURi: null ���� ���ַ��� ����URI
		public int CreateAttrItemFromUI(string strFullName,
			string strURI,
			out AttrItem attr,
			out string strError)
		{
			strError = "";
			attr = null;

			int nIndex = strFullName.IndexOf(":");
			if (nIndex == 0)
			{
				strError = "Ԫ������'" + strFullName + "'���Ϸ�";
				return -1;
			}
			else if (nIndex > 0)
			{
				string strPrefix = strFullName.Substring(0,nIndex);
				string strLocalName = strFullName.Substring(nIndex+1);
				if (strLocalName == "")
				{
					strError = "Ԫ������'" + strFullName + "'���Ϸ�";
					return -1;
				}
				if (strPrefix == "xmlns")
				{
					attr = this.CreateAttrItem(strFullName);
					attr.IsNamespace = true;
					//attr.LocalName = ""
				}
				else
				{
					if (strURI != null && strURI != "")
					{
						attr =  this.CreateAttrItem(strPrefix,
							strLocalName,
							strURI);
					}
					else
					{
						attr = this.CreateAttrItem(strPrefix,
							strLocalName);
					}
				}
			}
			else
			{
				if (strURI != null && strURI != "")
				{
					strError = "��������'" + strFullName + "'δָ��ǰ׺";
					return -1;
				}
				attr = this.CreateAttrItem(strFullName);
			}
			return 0;
		}


		// ׷�����ԣ����Ի���
		// return:
		//		-1	error
		//		0	successed
		//		-2	ȡ��
		public int AppendAttrWithDlg(ElementItem item,
			out string strError)
		{
			strError = "";

			AttrNameDlg dlg = new AttrNameDlg ();
			dlg.SetInfo("������",
				"��'" + item.Name + "'׷��������",
				item);
			dlg.ShowDialog();
			if (dlg.DialogResult != DialogResult.OK)
				return -2;

			AttrItem attr = null;
			int nRet = this.CreateAttrItemFromUI(dlg.textBox_strElementName.Text,
				dlg.textBox_URI.Text,
				out attr,
				out strError);
			if (nRet == -1)
				return -1;
				
			attr.SetValue(dlg.textBox_value.Text);

			return item.AppendAttr(attr,
				out strError);
		}





		//1.�� -- ������
		private void menuItem_AppendAttr(object sender,
			System.EventArgs e)
		{ 
			if (this.m_selectedItem == null) 
			{
				MessageBox.Show(this, "��δѡ���׼�ڵ�");
				return;
			}

			if ((!(this.m_selectedItem is ElementItem))
				&& (!(this.m_selectedItem is AttrItem)))
			{
				MessageBox.Show(this, "��ѡ��Ļ�׼�ڵ����Ͳ���ȷ��������ElementItem���� ���� AttrItem����");
				return;
			}

			ElementItem selected = null;

			// Ҫ��ǰѡ��Ľڵ�һ����ElementItem����
			if (this.m_selectedItem is ElementItem)
				selected = (ElementItem)this.m_selectedItem;
			else if (this.m_selectedItem is AttrItem)
				selected = this.m_selectedItem.parent;

			if (selected == null)
			{
				Debug.Assert(false,"�����ܵ������ǰ���Ѿ��жϺ���");
				return;
			}

			string strError;
			int nRet = this.AppendAttrWithDlg(selected,
				out strError);
			if (nRet == -1)
				MessageBox.Show(strError);
		}


		//2.�� -- ��ͬ������
		private void menuItem_InsertSiblingAttr(object sender,
			System.EventArgs e)
		{ 
			if (this.m_selectedItem == null)
			{
				Debug.Assert (false,"��'��ͬ������'ʱ��SelectedItemΪnull");
				return;
			}

			//�Ǹ��ڵ�û���ٲ���ͬ��
			if (!(this.m_selectedItem is AttrItem))
			{
				Debug.Assert (false,"��'��ͬ������'ʱ��SelectedItem����AttrItem����");
				return;
			}

			AttrItem startAttr = (AttrItem)this.m_selectedItem;

			string strError;
			int nRet = InsertAttrWithDlg(startAttr,out strError);
			if (nRet == -1)
				MessageBox.Show(strError);
		}

		// return:
		//		-1	error
		//		0	successed
		//		-2	ȡ��
		private int InsertAttrWithDlg(AttrItem startAttr,
			out string strError)
		{
			strError = "";

			ElementItem element = (ElementItem)(startAttr.parent);
			if (element == null)
			{
				strError = "InsertAttrWithDlg()��element������Ϊnull��";
				return -1;
			}

			AttrNameDlg dlg = new AttrNameDlg ();
			dlg.SetInfo ("��ͬ������",
				"��'" + startAttr.Name + "'������ͬ������",
				element);
			dlg.ShowDialog  ();
			if (dlg.DialogResult != DialogResult.OK )
				return -2;

			AttrItem newAttr = null;
			int nRet = this.CreateAttrItemFromUI(dlg.textBox_strElementName.Text,
				dlg.textBox_URI.Text,
				out newAttr,
				out strError);
			if (nRet == -1)
				return -1;

			newAttr.SetValue(dlg.textBox_value.Text);


			return element.InsertAttr(startAttr,
				newAttr,
				out strError);
		}


		#endregion


		#region ���Ӳ���

		// parameter:
		//		strFullName: ���Դ�ǰ׺ prefix:name
		//		strURi: null ���� ���ַ��� ����URI
		public int CreateElementItemFromUI(string strFullName,
			string strURI,
			out ElementItem element,
			out string strError)
		{
			strError = "";
			element = null;

			int nIndex = strFullName.IndexOf(":");
			if (nIndex == 0)
			{
				strError = "Ԫ������'" + strFullName + "'���Ϸ�";
				return -1;
			}
			else if (nIndex > 0)
			{
				string strPrefix = strFullName.Substring(0,nIndex);
				string strLocalName = strFullName.Substring(nIndex+1);
				if (strLocalName == "")
				{
					strError = "Ԫ������'" + strFullName + "'���Ϸ�";
					return -1;
				}
				if (strURI != null && strURI != "")
				{
					element =  this.CreateElementItem(strPrefix,
						strLocalName,
						strURI);
				}
				else
				{
					element = this.CreateElementItem(strPrefix,
						strLocalName);
				}
			}
			else
			{
				if (strURI != null && strURI != "")
				{
					strError = "Ԫ������'" + strFullName + "'δָ��ǰ׺";
					return -1;
				}
				element = this.CreateElementItem(strFullName);
			}
			return 0;
		}


				
		private void menuItem_AppendChild(object sender,
			System.EventArgs e)
		{ 
			Item item = this.m_selectedItem;
			if (item == null)
			{
				MessageBox.Show(this,"��δѡ���׼�ڵ�");
				return;
			}
			if (!(item is ElementItem))
			{
				MessageBox.Show(this,"��ǰ�ڵ����Ͳ��Ϸ���������ElementItem����");
				return;
			}

			ElementItem element = (ElementItem)item;

			//1.����"���¼�Ԫ��"�Ի���,�õ�Ԫ����
			ElementNameDlg dlg = new ElementNameDlg ();
			dlg.SetInfo ("���¼�Ԫ��",
				"��'" + element.Name + "'׷�����¼�Ԫ��");
			dlg.ShowDialog  ();
			if (dlg.DialogResult != DialogResult.OK)
				return;

			// 3.�����ڵ�
			string strError;
			ElementItem childItem = null;

			int nRet = this.CreateElementItemFromUI(
				dlg.textBox_strElementName.Text,
				dlg.textBox_URI.Text,
				out childItem,
				out strError);
			if (nRet == -1)
			{
				MessageBox.Show(this,strError);
				return;
			}
			
			// 4.���뵽����
			element.AutoAppendChild(childItem);	


			TextItem textItem = null;
			if (dlg.textBox_text.Text != "")
			{
				textItem = this.CreateTextItem();
				textItem.Value = dlg.textBox_text.Text;
				childItem.AutoAppendChild(textItem);  // һ�����Ӵ����¼����ڴ����Ӵ����
			}
			
			return;	
		}

		// �����¼��ı�
		private void menuItem_AppendText(object sender,
			System.EventArgs e)
		{ 
			Item item = this.m_selectedItem;

			if (item == null)
			{
				MessageBox.Show(this,"��δѡ���׼�ڵ�");
				return;
			}
			if (!(item is ElementItem))
			{
				MessageBox.Show(this,"��ǰ�ڵ����Ͳ��Ϸ���������ElementItem����");
				return;
			}

			ElementItem element = (ElementItem)item;

			// 1.����һ���ı��ڵ�
			TextItem textItem = this.CreateTextItem();

			// 2.���뵽����
			string strError;
			int nRet = element.AppendChild(textItem,
				out strError);
			if (nRet == -1)
				MessageBox.Show(this,strError);	
		}


		// return:
		//		-1	error
		//		0	successed
		//		-2	ȡ��
		public int InsertChildWithDlg(Item startItem,
			out string strError)
		{
			strError = "";

			ElementItem myParent = this.m_selectedItem.parent ;
			if (myParent == null)
			{
				strError = "����Ϊnull�������ܵ����";
				return -1;
			}
			
			//1.��"��ͬ��"�Ի��򣬵õ�Ԫ����
			ElementNameDlg dlg = new ElementNameDlg ();
			dlg.SetInfo ("��ͬ��Ԫ��",
				"��'" + this.m_selectedItem.Name + "'Ԫ��������ͬ��Ԫ��");
			dlg.ShowDialog  ();
			if (dlg.DialogResult != DialogResult.OK )
				return -2;

			// 3.����һ��Ԫ��
			ElementItem siblingItem = null;
			int nRet = this.CreateElementItemFromUI(dlg.textBox_strElementName.Text,
				dlg.textBox_URI.Text,
				out siblingItem,
				out strError);
			if (nRet == -1)
				return -1;
			
			TextItem textItem = null;
			if (dlg.textBox_text.Text != "")
			{
				textItem = this.CreateTextItem();
				textItem.SetValue(dlg.textBox_text.Text);
				siblingItem.AppendChildInternal(textItem,false,false); //һ�����Ӵ����¼����ڴ����Ӵ����
			}

			// 4.�ӵ���ǰԪ�ص�ǰ��
            // TODO: try
            // Exception:
            //      ���ܻ��׳�PrefixNotDefineException�쳣
			nRet = myParent.InsertChild(this.m_selectedItem,
				siblingItem,
				out strError);
			if (nRet == -1)
				return -1;

			return 0;
		}

        // ͬ��ǰ��Ԫ��
		private void menuItem_InsertSiblingChild(object sender,
			System.EventArgs e)
		{
			Item item = this.m_selectedItem;
			if (item == null)
			{
				MessageBox.Show(this,"��δѡ���׼�ڵ�");
				return;
			}

			// ������ܲ���ͬ��
			if (item == this.VirtualRoot)
			{
				MessageBox.Show (this,"����ڵ㲻�ܲ���ͬ��Ԫ��");
				return;
			}

			//�ĵ����ڵ㲻�ܲ���ͬ��
			if (item == this.docRoot)
			{
				MessageBox.Show (this,"�ĵ����ڵ㲻�ܲ���ͬ��Ԫ��");
				return;
			}

			ElementItem myParent = item.parent ;
			if (myParent == null)
			{
				MessageBox.Show(this,"����Ϊnull�������ܵ����");
				return;
			}

            bool bInputUri = false;
            string strOldElementName = null;

            REDOINPUT:
			
			//1.��"��ͬ��"�Ի��򣬵õ�Ԫ����
			ElementNameDlg dlg = new ElementNameDlg();
            dlg.InputUri = bInputUri;
            if (String.IsNullOrEmpty(strOldElementName) == false)
                dlg.textBox_strElementName.Text = strOldElementName;
			dlg.SetInfo ("��ͬ��Ԫ��",
				"��'" + item.Name + "'Ԫ��������ͬ��Ԫ��");
			dlg.ShowDialog();
			if (dlg.DialogResult != DialogResult.OK )
				return;

			string strError;
			// 3.����һ��Ԫ��
			ElementItem siblingItem = null;
			int nRet = this.CreateElementItemFromUI(
                dlg.textBox_strElementName.Text,
				dlg.textBox_URI.Text,
				out siblingItem,
				out strError);
			if (nRet == -1)
			{
				MessageBox.Show(this,strError);
				return;
			}
			
			// 4.�ӵ���ǰԪ�ص�ǰ��
            try
            {
                nRet = myParent.InsertChild(item,
                    siblingItem,
                    out strError);
            }
            catch (PrefixNotDefineException ex) // ǰ׺�ַ���û���ҵ�URI
            {
                MessageBox.Show(this, ex.Message);
                strOldElementName = dlg.textBox_strElementName.Text;
                bInputUri = true;   // �ر�Ҫ������URI�ַ���
                goto REDOINPUT; // Ҫ����������
            }

			if (nRet == -1)
			{
				MessageBox.Show(this,strError);
				return;
			}

			TextItem textItem = null;
			if (dlg.textBox_text.Text != "")
			{
				textItem = this.CreateTextItem();
				textItem.Value = dlg.textBox_text.Text;
				siblingItem.AppendChild(textItem);
			}

			return;

		}


		private void menuItem_InsertSiblingText(object sender,
			System.EventArgs e)
		{
			Item item = this.m_selectedItem;
			if (item == null)
			{
				MessageBox.Show(this,"��δѡ�л�׼�ڵ�!");
				return;
			}

			// ����ڵ㲻�ܲ���ͬ���ı�
			if (item == this.VirtualRoot)
			{
				MessageBox.Show(this,"����ڵ㲻�ܲ���ͬ���ı�!");
				return;
			}

			// �ĵ����ڵ㲻�ܲ���ͬ���ı�
			if (item == this.docRoot)
			{
				MessageBox.Show(this,"�ĵ����ڵ㲻�ܲ���ͬ���ı�!");
				return;
			}

			ElementItem myParent = item.parent ;
			if (myParent == null)
			{
				MessageBox.Show(this,"��ǰ�ڵ�ĸ��ײ�����Ϊnull!");
				return;
			}
				
			TextItem siblingText = this.CreateTextItem();

            // TODO: try
            // Exception:
            //      ���ܻ��׳�PrefixNotDefineException�쳣
            string strError;
			int nRet = myParent.InsertChild(item,
				siblingText,
				out strError);
			if (nRet == -1)
			{
				MessageBox.Show(strError);
			}
		}


		#endregion

		#region �½���Ԫ��

		public void CreateRootWithDlg()
		{
			//1.�򿪶Ի���ѯ��Ԫ����
			ElementNameDlg dlg = new ElementNameDlg ();
			dlg.SetInfo ("�½���Ԫ��",
				"�½���Ԫ��");
			dlg.ShowDialog();
			if (dlg.DialogResult != DialogResult.OK )
				return;

			if (this.VirtualRoot != null)
			{
				Debug.Assert(false,"��ǰ�Ѵ�������������ܵ����");
			}

			string strXml = "";
			string strName = dlg.textBox_strElementName.Text;
			string strValue = dlg.textBox_text.Text;
			strXml = "<" + strName + ">" + strValue + "</" + strName + ">";
			this.SetXml(strXml);
		}

		//7.�� -- �½���Ԫ��
		private void menuItem_CreateRoot(object sender,
			System.EventArgs e)
		{
			this.CreateRootWithDlg();
		}

		#endregion


		#region ɾ��

		


		//8.�� -- ɾ��
		public void menuItem_Delete()
		{
			if (this.m_selectedItem == null)
			{
				Debug.Assert (false,"��'ɾ��'ʱ��m_curItemΪnull");
				return;
			}

			if (this.m_selectedItem == this.VirtualRoot)
			{
				MessageBox.Show("����ɾ�����Ԫ�أ�");
				return;
			}
			/*
						if (this.m_selectedItem == this.docRoot)
						{
							MessageBox.Show("����ɾ����Ԫ�أ�");
							return;
						}
			*/
			this.RemoveWithDlg(this.m_selectedItem);
		}
		private void menuItem_Delete(object sender,
			System.EventArgs e)
		{ 
			this.menuItem_Delete();

		}

		public void RemoveWithDlg(Item item)
		{
			string strText = "ȷʵҪɾ��'" 
				+ item.Name 
				+ "'�ڵ���?";

			if (item is AttrItem)
			{
				strText = "ȷʵҪɾ��'" 
					+ item.Name 
					+ "'���Խڵ���?";

				AttrItem attr = (AttrItem)item;
				if (attr.IsNamespace == true)
				{
					bool bCound = attr.parent.CoundDeleteNs(attr.LocalName);
					if (bCound == false)
					{
						strText = "���ֿռ�ڵ�'" + attr.Name + "'���ڱ�ʹ�ã�ȷʵҪǿ��ɾ���ýڵ���?\r\n"
							+ "(ʹ�ø����ֿռ�Ľڵ㽫���Ϸ�!)";
					}
					else
					{
						strText += "\r\n(�����Խڵ������ֿռ����͡�)";
					}
				}
/*
                ElementItem element = (ElementItem)attr.Parent.GetItem();
                if (element != null)
                {
                    if (element.Name == "dprms:file")
                    {
                        MessageBox.Show(this, "����ɾ��<dp2rms:file>Ԫ�ص�id���ԡ�");
                        return;
                    }
                }
*/
			}
			else if (item is ElementItem) 
			{
				ElementItem element = (ElementItem)item;

				strText = "ȷʵҪɾ��'" 
					+ element.Name 
					+ "'Ԫ�ؽڵ���?";

				if (element.children.Count > 0)
				{
					if (element.children.Count == 1
						&& (element.children[0] is TextItem))
					{
						// ���ֻ��һ���ı��ڵ㣬�Ͳ�����
					}
					else
					{
						strText += "\r\n(��Ԫ�ذ����¼�Ԫ��)";
					}
				}
			}
			DialogResult result = MessageBox.Show(this,
				strText,
				"XmlEditor",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button2);

			if (result == DialogResult.No) 
				return;

			ElementItem myParent = item.parent;
			if (myParent == null)
			{

			}
			myParent.Remove(item);
		}


		#endregion

		#region ���У����ƣ�ճ��

		// �� -- ����
		public void CopyToClipboard(Item item)
		{
			string strXml = item.OuterXml;
			Clipboard.SetDataObject(strXml);
		}

		public void menuItem_Copy()
		{

		}

		private void menuItem_Copy(object sender,
			System.EventArgs e)
		{ 
			if (this.m_selectedItem == null)
			{
				MessageBox.Show(this,"��δѡ�л�׼�ڵ�");
				return;
			}

			this.CopyToClipboard(this.m_selectedItem);

		}

		// �� -- ����

		public void CutToClipboard(Item item)
		{
			string strXml = item.OuterXml;
			Clipboard.SetDataObject(strXml);

			// ???????���
			if (item == this.VirtualRoot)
			{
				this.Xml = "";
				this.VirtualRoot = null;
				this.docRoot = null;
			}
			else
			{
				ElementItem myParent = item.parent;
				// ���ߵ�ǰ�ڵ�
				myParent.Remove(item);
			}
		}


		private void menuItem_Cut(object sender,
			System.EventArgs e)
		{ 
			if (this.m_selectedItem == null)
			{
				Debug.Assert(false,"��'����'ʱ��SelectedItemΪnull");
				return;
			}
			this.CutToClipboard(this.m_selectedItem);

		}

		// ճ������
		private void menuItem_PasteOver(object sender,
			System.EventArgs e)
		{ 
			IDataObject ido = Clipboard.GetDataObject();
			if (ido.GetDataPresent (DataFormats.UnicodeText) == false)
				return;
			string strInputText = (string)ido.GetData(DataFormats.UnicodeText);

			string strError;
			int nRet = this.PasteOverwrite(strInputText,
				this.m_selectedItem,
				true,
				out strError);
			if (nRet == -1)
			{
				MessageBox.Show(strError);
			}
		}

		// ճ������_ͬ��ǰ��
		private void menuItem_PasteInsert_InsertBefore(object sender,
			System.EventArgs e)
		{
			Item item = this.m_selectedItem;
			if (item == null)
			{
				MessageBox.Show(this,"δѡ�л�׼�ڵ�");
				return;
			}
			IDataObject ido = Clipboard.GetDataObject();
			if (ido.GetDataPresent (DataFormats.UnicodeText) == false)
			{
				MessageBox.Show(this,"���а�û������");
				return;
			}
			string strInputText = (string)ido.GetData(DataFormats.UnicodeText);


			if (item is AttrItem)
			{
				AttrItem tempAttr = this.CreateAttrItem("temp");
				item.parent.InsertAttrInternal((AttrItem)item,tempAttr,
					true,
					false);
				string strError;
				int nRet = this.PasteOverwrite(strInputText,
					tempAttr,
					false,
					out strError);
				if (nRet == -1)
				{
					MessageBox.Show(strError);
				}
			}
			else
			{
				ElementItem element = this.CreateElementItem("temp");
				if (item != this.VirtualRoot)
				{
                    // TODO: try
                    // Exception:
                    //      ���ܻ��׳�PrefixNotDefineException�쳣
                    item.parent.InsertChildInternal(item,
						element,
						true,
						false);
				}
				else
				{
					element = (ElementItem)item;
				}
                // ���������PasteOverwrite������������Ԫ�ؽṹ
				string strError;
				int nRet = this.PasteOverwrite(strInputText,
					element,
					false,
					out strError);
				if (nRet == -1)
				{
					MessageBox.Show(strError);
				}
			}
		}

		// ճ������_ͬ�����
		private void menuItem_PasteInsert_InsertAfter(object sender,
			System.EventArgs e)
		{
			Item item = this.m_selectedItem;
			if (item == null)
			{
				MessageBox.Show(this,"δѡ�л�׼�ڵ�");
				return;
			}
			IDataObject ido = Clipboard.GetDataObject();
			if (ido.GetDataPresent (DataFormats.UnicodeText) == false)
			{
				MessageBox.Show(this,"���а�û������");
				return;
			}
			string strInputText = (string)ido.GetData(DataFormats.UnicodeText);

			if (item is AttrItem)
			{
				AttrItem tempAttr = this.CreateAttrItem("temp");

				int nIndex = item.parent.attrs.IndexOf((AttrItem)item);
				if (nIndex == -1)
				{
					MessageBox.Show(this,"���Բ���attrs���ϣ������ܵ����");
					return;
				}
				if (nIndex < item.parent.attrs.Count-1)
				{
					item.parent.InsertAttrInternal((AttrItem)item,
						tempAttr,
						true,
						false);
				}
				else
				{
					item.parent.AppendAttrInternal(tempAttr,
						true,
						false);
				}

				string strError;
				int nRet = this.PasteOverwrite(strInputText,
					tempAttr,
					false,
					out strError);
				if (nRet == -1)
				{
					MessageBox.Show(strError);
				}
			}
			else
			{
				if (item == this.VirtualRoot
					|| item == this.docRoot)
				{
					MessageBox.Show(this,"�������ʵ���ϲ��ܲ���ͬ��");
					return;
				}
				ElementItem elementTemp = this.CreateElementItem("temp");

				int nIndex = item.parent.children.IndexOf(item);
				if (nIndex == -1)
				{
					MessageBox.Show(this,"���Բ���attrs���ϣ������ܵ����");
					return;
				}
				if (nIndex < item.parent.children.Count-1)
				{
                    // TODO: try
                    // Exception:
                    //      ���ܻ��׳�PrefixNotDefineException�쳣
                    item.parent.InsertChildInternal(nIndex + 1,
						elementTemp,
						true,
						false);
				}
				else
				{
					item.parent.AppendChildInternal(elementTemp,
						true,
						false);
				}


				string strError;
				int nRet = this.PasteOverwrite(strInputText,
					elementTemp,
					false,
					out strError);
				if (nRet == -1)
				{
					MessageBox.Show(strError);
				}
			}
		}

		// ճ������_�¼�ĩβ
		private void menuItem_PasteInsert_AppendChild(object sender,
			System.EventArgs e)
		{

			Item item = this.m_selectedItem;
			if (item == null)
			{
				MessageBox.Show(this,"δѡ�л�׼�ڵ�");
				return;
			}
			if (!(item is ElementItem))
			{
				MessageBox.Show(this,"��׼�ڵ����Ͳ�ƥ�䣬������ElementItem����");
				return;
			}

			IDataObject ido = Clipboard.GetDataObject();
			if (ido.GetDataPresent (DataFormats.UnicodeText) == false)
			{
				MessageBox.Show(this,"���а�û������");
				return;
			}
			string strInputText = (string)ido.GetData(DataFormats.UnicodeText);

			ElementItem elementTemp = this.CreateElementItem("temp");

			ElementItem element = (ElementItem)item;
			element.ChildrenExpand = ExpandStyle.Expand;
			element.m_bWantChildInitial = 1;
			element.AppendChildInternal(elementTemp,true,false);

			string strError;
			int nRet = this.PasteOverwrite(strInputText,
				elementTemp,
				false,
				out strError);
			if (nRet == -1)
			{
				MessageBox.Show(strError);
			}



		}

		// ��XML�ı������ǰ�ڵ��Լ�ȫ���¼�
		// parameter:
		//		strInputText	����ļ�������ճ�����ı�
		//		startItem	��ʼitem
		public int PasteOverwrite(string strInputText,
			Item startItem,
			bool bSetFocus,
			out string strError)
		{
			strError = "";

			if (String.IsNullOrEmpty(strInputText) == true)
			{
				Debug.Assert(false,"Paste(),strInputTextΪnull ���� ���ַ���");
				strError = "Paste(),strInputTextΪnull ���� ���ַ���";
				return -1;
			}

			if (startItem == null)
			{
				this.SetXml(strInputText);
				return 0;
			}

			if (startItem == this.VirtualRoot)
			{
				this.SetXml(strInputText);
				return 0;
			}

			// ����startItem�����ͣ���������ַ���ƴ��xml
			string strXml = "";
			if (startItem is AttrItem)
				strXml = "<root " + strInputText + " />";
			else
				strXml = "<root>" + strInputText + "</root>";


			XmlDocument dom = new XmlDocument();
			try
			{
				dom.LoadXml(strXml);
			}
			catch(Exception ex)
			{
				strError = "paste() error,ԭ��:" + ex.Message;
				return -1;
			}

            // item���´�������ʱԪ��
			ElementItem item = new ElementItem(this);

			ElementInitialStyle style = new ElementInitialStyle();
			style.attrsExpandStyle = ExpandStyle.Expand;
			style.childrenExpandStyle = ExpandStyle.Expand;
			style.bReinitial = false;

			item.Initial(dom.DocumentElement,this.allocator,style, false);  // !!!

            // myParent��Ҫ���ǵ�Ԫ�صĸ���
			ElementItem myParent = (ElementItem)startItem.parent;



			int nIndex = 0;
			bool bAttr = false;
			if (startItem is AttrItem)
			{
				bAttr = true;
				nIndex = myParent.attrs.IndexOf(startItem);
				Debug.Assert(nIndex != -1,"�����ܵ����");

				AttrItem startAttr = (AttrItem)startItem;
				foreach(AttrItem attr in item.attrs)
				{
					myParent.InsertAttrInternal(startAttr,
						attr,
						false,
						false);
				}
				myParent.RemoveAttrInternal(startAttr,false);
			}
			else
			{
				bAttr = false;
                // �ҵ�startItem��myParent���ж����е�����λ��
				nIndex = myParent.children.IndexOf(startItem);
				Debug.Assert(nIndex != -1,"�����ܵ����");

                // ��startItemλ��ǰ�����item�����ж���
				foreach(Item child in item.children)
				{
                    // TODO: try
                    // Exception:
                    //      ���ܻ��׳�PrefixNotDefineException�쳣
                    myParent.InsertChildInternal(startItem,
						child,
						false,
						false);
				}
                // ɾ��startItem
				myParent.RemoveChildInternal(startItem,false);
			}

			myParent.InitialVisual();

			int nWidth , nHeight;
			myParent.Layout(myParent.Rect.X,
				myParent.Rect.Y,
				myParent.Rect.Width,
				0,   //��Ϊ0����Ҫ�Ǹ߶ȱ仯
				this.nTimeStampSeed++,
				out nWidth,
				out nHeight,
				LayoutMember.Layout | LayoutMember.Up);


			if (bSetFocus == true)
			{
				if (bAttr == true)
				{
					Item curItem = myParent.attrs[nIndex];
					this.SetCurText(curItem,null);
					this.SetActiveItem(curItem);
				}
				else
				{
					Item curItem = myParent.children[nIndex];
					this.SetCurText(curItem,null);
					this.SetActiveItem(curItem);
				}
			}
			else
			{
				this.SetCurText(this.m_selectedItem,this.m_curText);
			}

            // ���ܻ�ı��ĵ���, ����һ��
            if (startItem.Parent == this.VirtualRoot)
                this.docRoot = this.GetDocRoot();   // 2006/6/22 xietao



			this.AfterDocumentChanged(ScrollBarMember.Both);
			this.Invalidate();

			// �ĵ������仯
			this.FireTextChanged();

			myParent.Flush();
			return 0;
		}



		#endregion

		#region ����
	
		//�� -- ���Ų���
		private void menuItem_Horz(object sender,
			System.EventArgs e)
		{    
			this.LayoutStyle = LayoutStyle.Horizontal ;
		}

		//�� -- ���Ų���
		private void menuItem_Vert(object sender,
			System.EventArgs e)
		{    
			this.LayoutStyle = LayoutStyle.Vertical ;
		}

		#endregion

		# endregion


		#region һЩ��������

		public Item ActiveItem
		{
			get
			{
				return this.m_selectedItem;
			}
			set
			{
				Item item = value;
				this.SetCurText(item,null);
				this.SetActiveItem(item);
				Rectangle rect = new Rectangle(0,
					0,
					0,
					0);
				this.EnsureVisible(item,rect);
			}
		}

		// ֻ����m_selectedItem
		public void SetActiveItem(Item item)
		{
			Item oldItem = this.m_selectedItem;

			this.m_selectedItem = item;

			/////////////////////////////////////
			// ����ActiveItemChanged�¼�
			///////////////////////////////////////
			ActiveItemChangedEventArgs args = new ActiveItemChangedEventArgs();
			args.Lastitem = oldItem;
			args.ActiveItem = this.m_selectedItem;
			args.CurText = this.m_curText;

			this.fireActiveItemChanged(this,args);
		}


		public bool Changed
		{
			get
			{
				this.SetCurText(this.m_selectedItem,this.m_curText);
				this.Flush();
				return this.m_bChanged;
			}
			set
			{
				this.InternalSetChanged(value);
			}
		}

		// �ڲ���bChanged��ֵ
		private void InternalSetChanged(bool bChanged)
		{
			this.m_bChanged = bChanged;		
		}

		// ��ı�
		internal void FireTextChanged()
		{
			this.InternalSetChanged(true);

			EventArgs e = new EventArgs();
			this.OnTextChanged(e);


/*
			// ����TextChanged�¼�
			if (this.MyTextChanged != null)
			{
				EventArgs e = new EventArgs();
				this.MyTextChanged(this,e);
			}
*/			
		}

		public void BeginUpdate()
		{
			this.m_bAllowPaint = false;
		}

		public void EndUpdate()
		{
			this.m_bAllowPaint = true;
			this.Invalidate();
			this.Update();
		}






		#endregion

		#region һЩ��������


		public ElementItem DocumentElement 
		{
			get 
			{
				return this.docRoot;
			}
		}

		public ElementItem GetDocRoot()
		{
			if (this.VirtualRoot == null)
				return null;

			foreach(Item item in this.VirtualRoot.children)
			{
				if (item is ElementItem)
					return (ElementItem)item;
			}
			return null;
		}

		public Font FontTextDefault
		{
			get
			{
				Debug.Assert(this.Font != null,"Form �� Font���Բ�����Ϊnull");
				return this.Font;
			}
		}



	
		[Category("Appearance")]
		[DescriptionAttribute("Border style of the control")]
        [DefaultValue(typeof(System.Windows.Forms.BorderStyle), "Fixed3D")]
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


		private void BorderStyleToWindowStyle(ref int style, ref int exStyle)
		{
			style &= ~API.WS_BORDER;
			exStyle &= ~API.WS_EX_CLIENTEDGE;
			switch(borderStyle)
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

		//�ĵ����
		public int DocumentWidth  
		{
			get 
			{
				if (this.VirtualRoot == null)
					return -1;
				return this.VirtualRoot.Rect.Width;
			}
		}

		//�ĵ��߶�
		public int DocumentHeight  
		{
			get
			{
				if (this.VirtualRoot == null)
					return -1;	// /???

				return this.VirtualRoot.Rect.Height;
			}
		}

		public void SetCurText(Item item,XmlText text)
		{
			this.EditControlTextToVisual();

			if (text == null)
			{
				if (item != null
					&& (!(item is ElementItem))
					)
				{
					this.m_curText = item.GetVisualText();
				}
				else
					this.m_curText = null;
			}
			else
			{
				this.m_curText = text;
			}

			// �赱ǰEditor��λ��
			this.SetEditPos();

			// �ѵ�ǰText�����ݸ���edit��
			this.VisualTextToEditControl(); 
		}

		
		// ������xml
		public string Xml
		{
			get 
			{
				this.Flush();

				if (this.VirtualRoot == null)
					return "";
				return this.VirtualRoot.GetOuterXml(this.VirtualRoot);
			}
			set
			{
				SetXml(value);
			}
		}


		// רΪ��Xml�������˽�к���
		private void SetXml(string strXml)
		{
			strXml = strXml.Trim();
			if (strXml == "")
			{
				if (this.VirtualRoot != null)
				{
					this.VirtualRoot.FireTreeRemoveEvents(this.VirtualRoot.GetXPath());
				}
				this.VirtualRoot = null;
				this.docRoot = null;
				
				this.SetCurText(null,null);
				this.SetActiveItem(null);

				AfterDocumentChanged(ScrollBarMember.Both);
				this.Invalidate();

				// �ĵ������仯
				this.FireTextChanged();
				return;
			}

			if (this.VirtualRoot == null)
			{
				this.VirtualRoot = new VirtualRootItem(this);
				this.VirtualRoot.LayoutStyle = this.m_layoutStyle ;
				this.VirtualRoot.m_bConnected = true;
			}
			else
			{
				this.VirtualRoot.ClearAttrs();
				this.VirtualRoot.ClearChildren();
			}


			XmlDocument dom = new XmlDocument();
            dom.PreserveWhitespace = true;
			dom.LoadXml(strXml); 

			ElementInitialStyle style = new ElementInitialStyle();
			style.attrsExpandStyle = ExpandStyle.Expand;
			style.childrenExpandStyle = ExpandStyle.Expand;
			style.bReinitial = false;

			this.VirtualRoot.Initial(dom,//dom.DocumentElement,
				allocator,
				style,
                true);

			this.docRoot = this.GetDocRoot();
				

			this.VirtualRoot.InitialVisual();

			int nWidth = 0;
			int nHeight = 0;
			this.VirtualRoot.Layout(0,
				0,
				this.ClientSize .Width -1,
				0 ,
				nTimeStampSeed++,
				out nWidth,
				out nHeight,
				LayoutMember.Layout );	

			this.SetCurText(this.VirtualRoot,null);
			this.SetActiveItem(this.VirtualRoot);


			if (this.m_bFocused == true)
				this.curEdit.Focus();


			AfterDocumentChanged(ScrollBarMember.Both);
			this.Invalidate();

			// �ĵ������仯
			this.FireTextChanged();
		}


		//����ʽ�����ļ�
		public void SetCfg(VisualCfg visualCfg)
		{
			if (this.VirtualRoot == null)
				return;
			this.VisualCfg = visualCfg;
			int nRetWidth,nRetHeight;
			this.VirtualRoot.Layout (0,
				0,
				this.ClientSize .Width ,
				0,
				this.nTimeStampSeed ++,
				out nRetWidth,
				out nRetHeight,
				LayoutMember.Layout );

			this.AfterDocumentChanged(ScrollBarMember.Both);

			this.Invalidate ();
		}

		// ��������ʽ
		public LayoutStyle LayoutStyle
		{
			get
			{
				return m_layoutStyle;
			}
			set
			{
				this.m_layoutStyle  = value;
				int nWidth;
				int nHeight;

				if (this.VirtualRoot == null)
					return;

				//������Щ���ʣ�����ôͳһ����layoutStyle
				this.VirtualRoot.LayoutStyle = this.m_layoutStyle ;

		
				this.widthList = null;
				this.widthList = new ItemWidthList ();
				
				this.VirtualRoot.Layout (0,
					0,
					this.ClientSize.Width-1 ,
					0,
					nTimeStampSeed++,
					out nWidth,
					out nHeight,
					LayoutMember.Layout );

				this.SetEditPos ();
				AfterDocumentChanged(ScrollBarMember.Both );

				this.Invalidate();
			}
		}
		
		//size��ʽ
		public override bool AutoSize 
		{
			get 
			{
				return bAutoSize;
			}
			set 
			{
				bAutoSize = value;
				AfterDocumentChanged(ScrollBarMember.Both);
			}
		}

		
		//��visual������������ļ�
		public void WriteVisualOrg(string strName)
		{
			this.VirtualRoot.WriteRect (strName);
		}
		
		# endregion

		#region �¼�

		//public event EventHandler MyTextChanged;

		public event ActiveItemChangedEventHandle ActiveItemChanged;
		public void fireActiveItemChanged(object sender,
			ActiveItemChangedEventArgs args)
		{
			if (ActiveItemChanged != null)
			{
				ActiveItemChanged(sender,args);
			}
		}


		public event BeforeItemCreateEventHandle BeforeItemCreate;
		
		public event ItemCreatedEventHandle ItemCreated;
		public void fireBeforeItemCreate(object sender,
			BeforeItemCreateEventArgs args)
		{
			if (BeforeItemCreate != null)
			{
				BeforeItemCreate(sender,args);
			}
		}
		public void fireItemCreated(object sender,
			ItemCreatedEventArgs args)
		{
			if (ItemCreated != null)
			{
				ItemCreated(sender,args);
			}
		}

		public event BeforeItemTextChangeEventHandle BeforeItemTextChange;
		public event ItemTextChangedEventHandle ItemTextChanged;
		public void fireBeforeItemTextChange(object sender,
			BeforeItemTextChangeEventArgs args)
		{
			if (BeforeItemTextChange != null)
			{
				BeforeItemTextChange(sender,args);
			}
		}
		public void fireItemTextChanged(object sender,
			ItemTextChangedEventArgs args)
		{
			if (ItemTextChanged != null)
			{
				ItemTextChanged(sender,args);
			}
		}

		public event BeforeItemChangeEventHandle BeforeItemChange;
		public event ItemChangedEventHandle ItemChanged;
		public void fireBeforeItemChange(object sender,
			BeforeItemChangeEventArgs args)
		{
			if (BeforeItemChange != null)
			{
				BeforeItemChange(sender,args);
			}
		}
		public void fireItemChanged(object sender,
			ItemChangedEventArgs args)
		{
			if (ItemChanged != null)
			{
				ItemChanged(sender,args);
			}
		}

		public event BeforeItemDeleteEventHandle BeforeItemDelete;
		public event ItemDeletedEventHandle ItemDeleted;
		public void fireBeforeItemDelete(object sender,
			BeforeItemDeleteEventArgs args)
		{
			if (BeforeItemDelete != null)
			{
				BeforeItemDelete(sender,args);
			}
		}
		public void fireItemDeleted(object sender,
			ItemDeletedEventArgs args)
		{
			if (ItemDeleted != null)
			{
				ItemDeleted(sender,args);
			}
		}


		#endregion

		#region ʹ��Xpathѡ�ڵ�

/*
		public virtual XPathNavigator CreateNavigator()
		{
			XmlEditorNavigator nav = new XmlEditorNavigator(this);
			return nav;
		}
*/

		#endregion

		#region �����ڵ�
		
		// ����һ��Ԫ�ؽڵ�
		// strName: Ԫ������
		// ע�Ȿ�������Ը���ɴ�����ǰ׺��Ԫ�ؽڵ� strName��ʽΪ: abc:test
		// ǰ׺����ӦURI�Ķ�����ϼ��ڵ��ң�����ҵ����򴴽��ɹ������δ�ҵ�������ʧ�ܡ�
		public ElementItem CreateElementItem(string strName)
		{
			ElementItem item = new ElementItem(this);
			item.Name = strName;
			item.Prefix = "";
			item.LocalName = ItemUtil.GetLocalName(strName);

			// �½��Ľڵ�϶���Ҫ��ʼ��visual�ṹ������չ��״̬��
			item.m_bWantAttrsInitial =1;
			item.m_bWantChildInitial = 1;
			item.AttrsExpand = ExpandStyle.Expand;
			item.ChildrenExpand = ExpandStyle.Expand;
			
			return item;
		}

		// parameter:
		//		strPrefix	ǰ׺
		//		strName	����
		// ˵��: �Զ����ϼ��ҵ���Ӧ��URI������Ҳ����򴴽��ڵ㲻�ɹ�
		public ElementItem CreateElementItem(string strPrefix,
			string strName)
		{
			Debug.Assert(strPrefix != null,"CreateElementItem(),strPrefix��������Ϊnull");
			Debug.Assert((strName.IndexOf(":") == -1),"CreateElementItem(),strName���������ٺ���ǰ׺");
			
			ElementItem element =
				this.CreateElementItem(strPrefix + ":" + strName);

			element.Prefix = strPrefix;  // ǰ׺�ȼӺ�,�������ӹ�ϵʱ�ٱ���
			
			return element;
		}

		public ElementItem CreateElementItem(string strPrefix,
			string strName,
			string strNamespaceURI)
		{
			Debug.Assert(strPrefix != null,"CreateElementItem(),strPrefix��������Ϊnull");
			Debug.Assert(strNamespaceURI != null,"CreateElementItem(),strNamespaceURI��������Ϊnull");
			Debug.Assert((strName.IndexOf(":") == -1),"CreateElementItem(),strName���������ٺ���ǰ׺");

			ElementItem element = this.CreateElementItem(
				strPrefix,
				strName);

			element.m_strTempURI = strNamespaceURI; 

			return element;
		}

		// ����һ������
		// ע�Ȿ�������Ը���ɴ�����ǰ׺�����Խڵ� strName��ʽΪ: abc:test
		// ǰ׺����ӦURI�Ķ�����ϼ�Ԫ�ؽڵ��ң�����ҵ����򴴽��ɹ������δ�ҵ�������ʧ�ܡ�
		public AttrItem CreateAttrItem(string strName)
		{
			AttrItem item = new AttrItem(this);
			item.Name = strName;
			item.Prefix = "";
			item.LocalName = ItemUtil.GetLocalName(strName);

			return item;
		}

		// parameter:
		//		strPrefix	ǰ׺
		//		strName	����
		// ˵��: �Զ����ϼ��ҵ���Ӧ��URI������Ҳ����򴴽��ڵ㲻�ɹ�
		public AttrItem CreateAttrItem(string strPrefix,
			string strName)
		{
			Debug.Assert(strPrefix != null,"strPrefix��������Ϊnull");
			Debug.Assert((strName.IndexOf(":") == -1),"strName���������ٺ���ǰ׺");

			if (strPrefix == null)
			{
				throw new Exception("strPrefix��������Ϊnull");
			}

			if (strName.IndexOf(":") != -1)
			{
				throw new Exception("strName���������ٺ���ǰ׺");
			}


			AttrItem attr = 
				this.CreateAttrItem(strPrefix + ":" + strName);
			attr.Prefix = strPrefix;
			return attr;
		}

		public AttrItem CreateAttrItem(string strPrefix,
			string strName,
			string strNamespaceURI)
		{
			Debug.Assert(strPrefix != null,"strPrefix��������Ϊnull");
			Debug.Assert(strNamespaceURI != null,"strNamespaceURI��������Ϊnull");

			Debug.Assert((strName.IndexOf(":") == -1),"strName���������ٺ���ǰ׺");

			if (strPrefix == null)
			{
				throw new Exception("strPrefix��������Ϊnull");
			}
			if (strNamespaceURI == null)
			{
				throw new Exception("strNamespaceURI��������Ϊnull");
			}

			if (strName.IndexOf(":") != -1)
			{
				throw new Exception("strName���������ٺ���ǰ׺");
			}

			AttrItem attr = this.CreateAttrItem(strPrefix,
				strName);
			attr.m_strTempURI = strNamespaceURI;
			return attr;
		}
		// ����һ���ı��ڵ�
		public TextItem CreateTextItem()
		{
			TextItem item = new TextItem(this);
			return item;
		}

		// ����һ��ProcessingInstructionItem
		public ProcessingInstructionItem CreateProcessingInstructionItem(string strName,
			string strValue)
		{
			ProcessingInstructionItem item = new ProcessingInstructionItem(this);
			item.Name = strName;
			item.SetValue(strValue);
			return item;
		}

		// ����һ��DeclarationItem
		public DeclarationItem CreateDeclarationItem(string strValue)
		{
			DeclarationItem item = new DeclarationItem(this);
			item.SetValue(strValue);
			return item;
		}

		// ����һ��CommentItem
		// parameter:
		//		strValue	ֵ
		public CommentItem CreateCommentItem(string strValue)
		{
			CommentItem item = new CommentItem(this);
			item.SetValue(strValue);
			return item;
		}

		// ����һ��CDATAItem
		// parameter:
		//		strValue	ֵ
		public CDATAItem CreateCDATAItem(string strValue)
		{
			CDATAItem item = new CDATAItem(this);
			item.SetValue(strValue);
			return item;
		}

		// ����һ��DocumentTypeItem
		// parameter:
		//		strName	����
		public DocumentTypeItem CreateDocumentTypeItem(string strName,
			string strValue)
		{
			DocumentTypeItem item = new DocumentTypeItem(this);
			item.Name = strName;
			item.SetValue(strValue);
			return item;
		}
		
		// ����һ��EntityReferenceItem
		// parameter:
		//		strName	����
		public EntityReferenceItem CreateEntityReferenceItem(string strName)
		{
			EntityReferenceItem item = new EntityReferenceItem(this);
			item.Name = strName;
			return item;
		}
		

		#endregion
	}

	#region xmleditor�Զ����¼�

	public delegate void ActiveItemChangedEventHandle(object sender,
	ActiveItemChangedEventArgs e);
	public class ActiveItemChangedEventArgs: EventArgs
	{
		public Item Lastitem = null;
		public Item ActiveItem = null;
		public XmlText CurText = null;
	}

	// 1.BeforeItemCreate
	// 
	public delegate void BeforeItemCreateEventHandle(object sender,
	BeforeItemCreateEventArgs e);
	public class BeforeItemCreateEventArgs: EventArgs
	{
		public Item item = null;
		public Item parent = null;
		public bool bInitial = false;
		public bool Cancel = false;
	}

	// 2.ItemCreated
	public delegate void ItemCreatedEventHandle(object sender,
	ItemCreatedEventArgs e);
	
	public class ItemCreatedEventArgs: EventArgs
	{
		public Item item = null;
		public bool bInitial = false;
	}

	// 3.BeforeItemTextChange
	public delegate void BeforeItemTextChangeEventHandle(object sender,
	BeforeItemTextChangeEventArgs e);
	public class BeforeItemTextChangeEventArgs: EventArgs
	{
		public Item item = null;
		public string OldText = "";
		public string NewText = "";
	}

	// 4.ItemTextChanged
	public delegate void ItemTextChangedEventHandle(object sender,
	ItemTextChangedEventArgs e);
	public class ItemTextChangedEventArgs: EventArgs
	{
		public Item item = null;
		public string OldText = "";
		public string NewText = "";
	}

	// 5.BeforeItemAttrChange
	public delegate void BeforeItemChangeEventHandle(object sender,
	BeforeItemChangeEventArgs e);
	public class BeforeItemChangeEventArgs: EventArgs
	{
		public Item item = null;
		public string OldValue = "";
		public string NewValue = "";
	}

	// 6.ItemChanged
	public delegate void ItemChangedEventHandle(object sender,
	ItemChangedEventArgs e);

	public class ItemChangedEventArgs: EventArgs
	{
		public Item item = null;  // �κ����ͣ���ʱ����
		public string OldValue = "";
		public string NewValue = "";
	}


	// 7.BeforeItemDelete
	public delegate void BeforeItemDeleteEventHandle(object sender,
	BeforeItemDeleteEventArgs e);
	public class BeforeItemDeleteEventArgs: EventArgs
	{
		public Item item = null;
	}

	// 8.ItemDeleted
	public delegate void ItemDeletedEventHandle(object sender,
	ItemDeletedEventArgs e);
	public class ItemDeletedEventArgs: EventArgs
	{
		public Item item = null;
		public string XPath = "";
		public bool RecursiveChildEvents = false;
		public bool RiseAttrsEvents = false;

	}

	#endregion

}
