using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using System.IO;

using DigitalPlatform.IO;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;

using System.Diagnostics;

namespace DigitalPlatform.Xml
{
	public abstract class Item : Box,IXPathNavigable
	{
		#region ��Ա����

		public ElementItem parent = null;       // ����
		public XmlEditor m_document = null;     // XmlEditor

		public string BaseURI = "";  // XPathNavigator����Ҫ,�κνڵ㶼��BaseURI
		
		// ��û�ж�Ӧ��visual�ṹʱ,��m_paraValue��ʾ��Item��ֵ
		// ������visual�ṹ,��ֱ��ʹ��visual��text
		// ��������ElementItem����������
		public string m_paraValue1 = "";

		public bool m_bConnected = false;

		#endregion

	

		#region ������ʽ�����ļ���һЩ����

		/////////////////////////////////////////////////////////
		// some property about cfg
		/////////////////////////////////////////////////////////

		public override ItemRegion GetRegionName()
		{
			return ItemRegion.Frame;
		}


		// ��GetBackColor(region)��GetTextColor(region)��GetBorderColor(region)����˽�к���
        // parameters:
		//      region        ö��ֵ��ȡ�ĸ�����
		//      valueStyle    ö��ֵ��ȡ�������͵�ֵ
        // return:
        //      Color����
		Color GetColor(ItemRegion region,ValueStyle valueStyle)
		{
			XmlEditor editor = this.m_document;
			if (editor.VisualCfg == null)
				goto END1;

			VisualStyle style = editor.VisualCfg.GetVisualStyle(this,region);
			if (style == null)
				goto END1;

			if (valueStyle == ValueStyle.BackColor )
				return style.BackColor ;
			else if (valueStyle == ValueStyle.TextColor )
				return style.TextColor ;
			else if (valueStyle == ValueStyle.BorderColor )
				return style.BorderColor  ;
			
			END1:
				//ȱʡֵ
				if (valueStyle == ValueStyle.BackColor)
				{
                    if (region == ItemRegion.Text)
                        return editor.BackColorDefaultForEditable;
                    else if (this is AttrItem)
                        return editor.AttrBackColorDefault;
                    else
                        return editor.BackColorDefault;
				}
				else if (valueStyle == ValueStyle.TextColor )
				{
					return editor.TextColorDefault;
				}
				else if (valueStyle == ValueStyle.BorderColor )
				{
					return editor.BorderColorDefault;
				}
				else 
				{
					return Color.Red;
				}
		}
	
		
		// ���ߴ�ֵ�õ���˽�к���.
        // parameters:
		//      region      ö��ֵ��ȡ�ĸ�����
		//      valueStyle  ö��ֵ��ȡ�������͵�ֵ
		int GetPixelValue(ItemRegion region,ValueStyle valueStyle)
		{
			XmlEditor editor = this.m_document;

			if (editor == null)
				goto END1;

			if (editor.VisualCfg == null)
				goto END1;

			VisualStyle style = editor.VisualCfg.GetVisualStyle(this,region);
			if (style == null)
				goto END1;

			if (valueStyle == ValueStyle.LeftBlank)
				return style.LeftBlank ;
			else if (valueStyle == ValueStyle.RightBlank)
				return style.RightBlank ;
			else if (valueStyle == ValueStyle.TopBlank)
				return style.TopBlank ;
			else if (valueStyle == ValueStyle.BottomBlank)
				return style.BottomBlank ;
			else if (valueStyle == ValueStyle.TopBorderHeight)
				return style.TopBorderHeight;
			else if (valueStyle == ValueStyle.BottomBorderHeight)
				return style.BottomBorderHeight;
			else if (valueStyle == ValueStyle.LeftBorderWidth)
				return style.LeftBorderWidth;
			else if (valueStyle == ValueStyle.RightBorderWidth)
				return style.RightBorderWidth;			
			END1:
				//ȱʡֵ
				if (valueStyle == ValueStyle.LeftBlank )
				{
					if (region == ItemRegion.ExpandAttributes 
						|| region == ItemRegion.ExpandContent )
						return 2;  //?
					else
						return 0;
				}
				else if (valueStyle == ValueStyle.RightBlank)
				{
					if (region == ItemRegion.ExpandAttributes 
						|| region == ItemRegion.ExpandContent )
						return 2;  // ?
					else
						return 0;
				}
				else if (valueStyle == ValueStyle.TopBlank)
					return 0;
				else if (valueStyle == ValueStyle.BottomBlank )
					return 0;
				else if (valueStyle == ValueStyle.TopBorderHeight)
				{
					return -1;
				}
				else if (valueStyle == ValueStyle.BottomBorderHeight)
				{
					return -1;
				}
				else if (valueStyle == ValueStyle.LeftBorderWidth)
				{
					return -1;
				}
				else if (valueStyle == ValueStyle.RightBorderWidth)
				{
					return -1;
				}

			return 0;
		}

		
		// �õ�����
        // parameters:
		//      region  ö��ֵ���ĸ����������
        // return:
        //      Font����
		public Font GetFont(ItemRegion region)
		{
			XmlEditor editor = this.m_document;
			if (editor.VisualCfg == null)
				goto END1;
			
			VisualStyle style = editor.VisualCfg.GetVisualStyle (this,region);
			if (style == null)
				goto END1;

			return style.Font ;
		
			END1:
				return editor.FontTextDefault;
		}

		
		// ����Ϊ����ʹ�õľ���С����/////////////////////////////////////////
		public int GetLeftBlank(ItemRegion region)
		{
			return GetPixelValue(region,ValueStyle.LeftBlank );
		}


		public int GetRightBlank(ItemRegion region)
		{
			return GetPixelValue(region,ValueStyle.RightBlank );
		}


		public int GetTopBlank(ItemRegion region)
		{
			return GetPixelValue(region,ValueStyle.TopBlank );
		}


		public int GetBottomBlank(ItemRegion region)
		{
			return GetPixelValue(region,ValueStyle.BottomBlank );
		}


		public Color GetBackColor(ItemRegion region)
		{
			return GetColor(region,ValueStyle.BackColor);
		}


		public Color GetTextColor(ItemRegion region)
		{
			return GetColor(region,ValueStyle.TextColor);
		}

		public int GetTopBorderHeight(ItemRegion region)
		{
			return this.GetPixelValue(region,ValueStyle.TopBorderHeight);
		}

		public int GetBottomBorderHeight(ItemRegion region)
		{
			return this.GetPixelValue(region,ValueStyle.BottomBorderHeight);
		}

		public int GetLeftBorderWidth(ItemRegion region)
		{
			return this.GetPixelValue(region,ValueStyle.LeftBorderWidth);
		}

		public int GetRightBorderWidth(ItemRegion region)
		{
			return this.GetPixelValue(region,ValueStyle.RightBorderWidth);
		}

		public Color GetBorderColor(ItemRegion region)
		{
			return GetColor(region, ValueStyle.BorderColor  );
		}

		#endregion

		#region ���ڿ��������������Ϣ�ĺ���

		// ����������ֵ
		public void SetValue(string strName,int nValue)
		{
			ItemWidth width = m_document.widthList. GetItemWidth(this.GetLevel());
			if (width != null)
			{
				width.SetValue (strName,nValue);
			}
		}

		// ��������ȡֵ
		public int GetValue(string strName)
		{
			ItemWidth width = m_document.widthList. GetItemWidth(this.GetLevel());
			if (width == null)
				return -1;

			return width.GetValue (strName);
		}


		public PartWidth GetPartWidth(string strName)
		{
			ItemWidth width = m_document.widthList. GetItemWidth(this.GetLevel());

			if (width == null)
				return null;

			return width.GetPartWidth  (strName);
		}

		#endregion

		#region ��������
        
		public ElementItem Parent
		{
			get
			{
				return this.parent;
			}
		}

		// ���⹫��������
		public string Value
		{
			get
			{
				return this.GetValue();
			}
			set
			{
				this.SetValue(value);
			}
		}

		// �õ����, ��1��ʼ����(1�����)
		internal int GetLevel()
		{
			int nLevel = 1;
			Item item = this;
			while(true) 
			{
				if (item == null)
					break;
				item = item.parent;
				nLevel ++;
			}
			return nLevel;
		}

		// �õ�XPath
		public string GetXPath()
		{
			ElementItem root = (ElementItem)this.m_document.docRoot;

			string strPath;
			ItemUtil.Item2Path (root,this,out strPath);
			return strPath;
		}

		// ��ElementItem������,������GetContentText() �� GetAttributesText()
		// һ�����ڳ�ʼ����visual�ṹ,���ܵ��˺���
		// region:
		//		-1	һ����ı��ڵ㣬��������
		//		0	������
		//		1	������
		// style:
		//		��ǰ�����״̬
		internal virtual Text GetVisualText()
		{
			if (this.m_paraValue1 != null)
			{
				Debug.Assert(false,"��û�г�ʼ��visual�ṹ,���ܵ��˺���");
				return null;
			}

			if (this.childrenVisual == null)
			{
				Debug.Assert(false,"��û�г�ʼ��visual�ṹ,���ܵ��˺���");
				return null;
			}

			Box boxTotal = this.GetBoxTotal();
			if (boxTotal == null)
			{
				Debug.Assert(false,"������û��BoxTotal");
				return null;
			}


			if (boxTotal.childrenVisual == null)
				return null;

			Text text = null;
			foreach(Visual childVisual in boxTotal.childrenVisual)
			{
				if (childVisual is Text
					/*&& childVisual.Name == ""*/)
				{
					text = (Text)childVisual;
				}
			}

			if (text == null)
			{
				Debug.Assert(false,"������û��Text����");
				return null;
			}
			return text;
		}

	
		// ElementItemҪ��д�������
		public virtual string GetValue()
		{
			string strValue = null;

			if (this.m_paraValue1 == null)
			{
				// ˵���ѽ�����visual�ṹ,��visual��õ�ֵ
				Text text = this.GetVisualText();
				if (text == null)
				{
					Debug.Assert(false,"m_paraValueΪnullʱ,visual�����ܲ�����");
					throw new Exception("m_paraValueΪnullʱ,visual�����ܲ�����");
				}
				Debug.Assert(text.Text != null,"");
				strValue = text.Text;  //ע����ת����xmlstring
			}
			else
			{
				strValue = this.m_paraValue1;
			}

			return StringUtil.GetVisualableStringSimple(strValue);
		}

		// ElementItemҪ��д�������
		public virtual void SetValue(string strValue)
		{
			strValue = StringUtil.GetXmlStringSimple(strValue);

			if (this.m_paraValue1 == null)
			{
				// ˵���ѽ�����visual�ṹ,��visual��õ�ֵ
				Text text = this.GetVisualText();
				if (text == null)
				{
					Debug.Assert(false,"m_paraValueΪnullʱ,visual�����ܲ�����");
					throw new Exception("m_paraValueΪnullʱ,visual�����ܲ�����");
				}
				text.Text = strValue;  //??????����ת����xmlstring?����
			}
			else
			{
				this.m_paraValue1 = strValue;
			}

			//????
			ElementItem myParent = this.parent;
			if (myParent != null)
				myParent.m_objAttrsTimestamp++;
		}

		// ElementItem	<a>test</a>
		// AttrItem	a="test"
		// TextItem test
		// �����಻��Ҫ����д������,ֻ����дGetOutXml()�Ϳ�����
		public virtual string OuterXml 
		{
			get
			{
				return this.GetOuterXml(null);
			}
			set 
			{
				throw(new Exception("��δʵ��OuterXml set����"));
			}
		}

		// parameter:
		//		FragmentRoot	�Ƿ�����ֿռ�,�ò�������ElementItem������
		internal virtual string GetOuterXml(ElementItem FragmentRoot)
		{
			return "";
		}



		// �õ�childrenVisual�е�Label����
		public Label GetLable()
		{
			if (this.childrenVisual == null)
				return null;

			Label label = null;
			for(int i=0;i<this.childrenVisual.Count;i++)
			{
				Visual visual = (Visual)this.childrenVisual[i];
				if (visual is Label)
				{
					label = (Label)visual;
					break;
				}
			}
			return label;
		}

		public Box GetBoxTotal()
		{
			if (this.childrenVisual == null)
				return null;

			Box boxTotal = null;
			for(int i=0;i<this.childrenVisual.Count;i++)
			{
				Visual visual = (Visual)this.childrenVisual[i];
				if (visual.Name == "BoxTotal")
				{
					boxTotal = (Box)visual;
					break;
				}
			}
			return boxTotal;
		}


		
		public Item GetNextSibling()
		{

			ElementItem myParent = this.parent;
			if (myParent == null)
				return null;

			int nIndex = myParent.children.IndexOf (this);
			if (nIndex == -1)
				return null;

			if (myParent.children.Count <= nIndex +1)
				return null;

			return myParent.children [nIndex + 1];
		}

		#region ��xpathѡ�ڵ�

		public virtual XPathNavigator CreateNavigator()
		{
			XmlEditorNavigator nav = new XmlEditorNavigator(this);
			return nav;
		}

		// strXpath	���������·��
		public ItemList SelectItems(string strXpath)
		{
			ItemList items = new ItemList();

			XPathNavigator nav = this.CreateNavigator();
		
			XPathNodeIterator ni = nav.Select(strXpath);
			while(ni.MoveNext())
			{
				Item item = ((XmlEditorNavigator)ni.Current).Item;
				items.Add(item);
			}
			return items;
		}


		public ItemList SelectItems(string strXpath,
			XmlNamespaceManager mngr)
		{
			ItemList items = new ItemList();

			XPathNavigator nav = this.CreateNavigator();
			XPathExpression expr = nav.Compile(strXpath);
			expr.SetContext(mngr);


			XPathNodeIterator ni = nav.Select(expr);
			while(ni.MoveNext())
			{
				Item item = ((XmlEditorNavigator)ni.Current).Item;
				items.Add(item);
			}
			return items;
		}

		public Item SelectSingleItem(string strXpath)
		{
			XPathNavigator nav = this.CreateNavigator();
			XPathNodeIterator ni = nav.Select(strXpath);
			ni.MoveNext();
			return ((XmlEditorNavigator)ni.Current).Item;
		}

		public Item SelectSingleItem(string strXpath,
			XmlNamespaceManager mngr)
		{
			ItemList items = this.SelectItems(strXpath,
				mngr);
			if (items.Count == 0)
				return null;

			return items[0];
		}

		#endregion

		#endregion

		#region ��ʼ��Item���


		// ��node��ʼ����������¼�
		// parameters:
		//		node	XmlNode�ڵ�
		//		allocator	���󴴽��������������¼�Ԫ�ض���
		//		style	״̬,չ�� ������ ����ElementItem������
		// return:
		//		-1  ����
		//		-2  ��;cancel;
		//		0   successed
		public virtual int Initial(XmlNode node, 
			ItemAllocator allocator,
            object style,
            bool bConnect)
		{
			return 0;
		}


		#endregion

		#region ��ʼ��Visual���

		// �˺����ȴ���������д
		public virtual void InitialVisualSpecial(Box boxtotal)
		{

		}

		// ������������������˵һ�㲻Ҫ���ء�һ������InitialVisualSpecial()���ɡ�
		// ��Ϊ����������ǰ�󲿷ֻ����ǹ��õģ�ֻ���жβ��õ���InitialVisualSpecial()�İ취��
		// ������������˱����������Ҳ��������е���InitialVisualSpecial()������Ҫ����ʵ��ȫ������
		public virtual void  InitialVisual()
		{
			if (this.childrenVisual != null)
				this.childrenVisual.Clear();

			// ��Label
			Label label = new Label();
			label.container = this;
			label.Text = this.Name;
			this.AddChildVisual(label);

			// ����һ���ܿ�
			Box boxTotal = new Box ();
			boxTotal.Name = "BoxTotal";
			boxTotal.container = this;
			this.AddChildVisual (boxTotal);

			// �����ܿ��layoutStyle��ʽΪ����
			boxTotal.LayoutStyle = LayoutStyle.Vertical;

			///
			InitialVisualSpecial(boxTotal);
			///

			//���boxTotalֻ��һ��box������Ϊ����
			if (boxTotal.childrenVisual != null 
				&& boxTotal.childrenVisual .Count == 1)
			{
				boxTotal.LayoutStyle = LayoutStyle.Horizontal ;
			}

			/*  ��ʱ����comment����
			Comment comment = new Comment ();
			comment.container = this;
			this.AddChildVisual  (comment);
			*/

		}


	
		#endregion

		#region ��д�麯��
		
		// ΪʲôҪ��д������,item�Ǵ�visual����,��visual�����ಢû�о���ʵ���������
		public override void Paint(PaintEventArgs pe,
			int nBaseX,
			int nBaseY,
			PaintMember paintMember)
		{
			// 1.������������ʵ������
			Rectangle rectPaintThis = new Rectangle (0,0,0,0);
			rectPaintThis = new Rectangle (nBaseX + this.Rect.X,
				nBaseY + this.Rect.Y,
				this.Rect.Width ,
				this.Rect.Height);
			if (rectPaintThis.IntersectsWith(pe.ClipRectangle )== false)
				return;

			Brush brush = null;

			// 2.������ɫ
			//	����ȱʡ͸��ɫ,��ǰ��ɫ��͸��ɫ��ͬ�򲻻���
			Object colorDefault = null;
			XmlEditor editor = this.m_document;
			if (editor != null && editor.VisualCfg != null)
				colorDefault = editor.VisualCfg.transparenceColor;
			if (colorDefault != null)  //ȱʡ��ɫ
			{
				if (((Color)colorDefault).Equals(BackColor) == true)
					goto SkipDrawBack;
			}

			brush = new SolidBrush(this.BackColor);
			pe.Graphics.FillRectangle (brush,rectPaintThis);

			// ����������ɫ			
			SkipDrawBack:



				// 3.������
				// ��������ʱ���Լ�����,����������
				if (editor != null
					&& editor.VisualCfg == null)
				{
				}
				else
				{
					// �����ļ����˵�ʱ��
					this.DrawLines(rectPaintThis,
						this.TopBorderHeight,
						this.BottomBorderHeight,
						this.LeftBorderWidth,
						this.RightBorderWidth,
						this.BorderColor);
				}

			// 4.������
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

				if (i <= 0)
					continue;

				if (editor != null
					&& editor.VisualCfg == null)
				{
					this.DrawLines(rectPaintChild,
						0,
						0,
						visual.LeftBorderWidth,
						0,
						visual.BorderColor);
				}
			}


			/*

						// 5.����ǰ���Item������ʾ
						XmlEditor myEditor = this.m_document;
						if (myEditor != null)
						{
							if (this == myEditor.SelectedItem)
							{
								//������������
								rectPaint = new Rectangle (nBaseX + this.rect .X,
									nBaseY + this.rect .Y,
									this.rect .Width ,
									this.rect .Height-1);
								Pen pen = new Pen(Color.White,1);
								pe.Graphics .DrawRectangle (pen,rectPaint );
								pen.Dispose ();
							}
						}
			*/			
		}


		#endregion
	} 



	// Ԫ�غ����Թ��еĻ��ࡣһ�㲻ֱ��ʵ����
	public class ElementAttrBase : Item
	{
		public string Prefix = null;
		
		public string LocalName = null;

		internal string m_strTempURI = null;

		// ��Ԫ���ǲ��������ռ�ڵ� 
		// ˵��,Ŀǰ���ǵ�����ռ�ڵ������Խڵ��Ƿ���һ���,������һ������������
		public bool IsNamespace = false;  

		public virtual string NamespaceURI 
		{
			get 
			{
				return null;
			}
		}
	}



	// Item����
	public class ItemList : CollectionBase
	{
		public Item this[int index]
		{
			get 
			{
				return (Item)InnerList[index];
			}
			set
			{
				InnerList[index] = value;
			}
		}
		public void Add(Item item)
		{
			InnerList.Add(item);
		}

		public  void Insert(int index,Item item)
		{
			InnerList.Insert (index,item);
		}

		public void Remove(Item item)
		{
			InnerList.Remove (item);
		}


		public int IndexOf (Item item)
		{
			return InnerList.IndexOf (item);
		}
	}
}
