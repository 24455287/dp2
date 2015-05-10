using System;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.Windows.Forms;
using System.IO;

using DigitalPlatform.Text;


namespace DigitalPlatform.Xml
{
	// Ԫ�ؽڵ�
	public class ElementItem : ElementAttrBase
	{

		internal ItemList attrs = new ItemList(); // ���Լ���, ������ͨ���Խڵ㣬Ҳ�������ֿռ�ڵ�   
		internal ItemList children = new ItemList(); // ���Ӽ���,����Ԫ�ؽڵ㡢ע�͵ȵȣ�Ҳ�ɰ����ı��ڵ�

		internal ExpandStyle m_childrenExpand = ExpandStyle.None; // ����Ԫ�ؼ���ͼ��״̬
		internal ExpandStyle m_attrsExpand = ExpandStyle.None; // ���Լ���ͼ��״̬

		internal int m_bWantAttrsInitial = -1;  // -1 δ��ֵ 0 ���øı� 1ϣ���ı�
		internal int m_bWantChildInitial = -1;  // -1 δ��ֵ 0 ���øı� 1ϣ���ı�

		internal int m_xmlAttrsTimestamp = 0;  // ����xml�༭��ʱ���
		internal int m_objAttrsTimestamp = 0;  // �ڴ�����ʱ���

		internal int m_xmlChildrenTimestamp = 0; // ����xml�༭��ʱ���
		internal int m_objChildrenTimestmap = 0; // �ڴ�����ʱ���

		public bool IsEmpty = false; // һ���ڵ��Ƿ���<aa/>��ʽ,ֻ��Ԫ�ؽڵ��ǲ�����˼


		internal ElementItem(XmlEditor document)
		{
			this.m_document = document;
			this.m_paraValue1 = null;
		}


		// ��ʼ�����ݣ���node�л�ȡ
		public virtual void InitialData(XmlNode node)
		{
			this.Name = node.Name;
			this.Prefix = node.Prefix;

			if (node.NodeType == XmlNodeType.Element)
			{
				XmlElement elementNode = (XmlElement)node;
				this.IsEmpty = elementNode.IsEmpty;
			}
			this.LocalName = node.LocalName;
			this.m_strTempURI = node.NamespaceURI;
		}

		public void SetStyle(ElementInitialStyle style)
		{

			// ������ɵ�״̬
			ExpandStyle oldChildExpand = this.m_childrenExpand;
			ExpandStyle oldAttrsExpand = this.m_attrsExpand;

			// ������������״̬
			ElementInitialStyle elementStyle = style;
			ExpandStyle newAttrsExpand = elementStyle.attrsExpandStyle;
			ExpandStyle newChildExpand = elementStyle.childrenExpandStyle;

			// ����ɲ��øı�״̬
			this.m_bWantAttrsInitial = 0;
			this.m_bWantChildInitial = 0;

			// �������Ƿ���Ҫ�ı�״̬
			if (elementStyle.bReinitial == false)	// �״γ�ʼ��
			{
				this.m_bWantAttrsInitial = 1;
			}
			else 
			{
				if (newAttrsExpand != oldAttrsExpand) 
					this.m_bWantAttrsInitial = 1;
				else
					this.m_bWantAttrsInitial = 0;
			}

			// �����״̬
			if (elementStyle.bReinitial == false)	// �״γ�ʼ��
			{
				this.m_bWantChildInitial = 1;
			}
			else
			{
				if (newChildExpand != oldChildExpand)
					this.m_bWantChildInitial = 1;
				else
					this.m_bWantChildInitial = 0;
			}
		}

		// ��node��ʼ����������¼�
		// return:
		//		-1  ����
		//		-2  ��;cancel
		//		0   �ɹ�
		public override int Initial(XmlNode node, 
			ItemAllocator allocator,
			object style,
            bool bConnect)
		{
			// this.m_bConnected = true;
            this.m_bConnected = bConnect;   // new change

			if (!(style is ElementInitialStyle))
			{
				Debug.Assert(false,"style����ΪElementInitialStyle����");
				return -1;
			}

			if ((node.NodeType != XmlNodeType.Element)
				&& node.NodeType != XmlNodeType.Document)
			{
				Debug.Assert(false, "'" + node.NodeType.ToString() + "'���Ǻ��ʵĽڵ�����");
				return 0;
			}

			// ��״̬
			this.SetStyle((ElementInitialStyle)style);

			// ������������״̬
			ElementInitialStyle elementStyle = (ElementInitialStyle)style;
			ExpandStyle newAttrsExpand = elementStyle.attrsExpandStyle;
			ExpandStyle newChildExpand = elementStyle.childrenExpandStyle;

			// ��ʼ�����ݣ���node�л�ȡ
			this.InitialData(node);

			// ��������
			if (this.m_bWantAttrsInitial == 1) 
			{
				if (node.Attributes == null
					|| node.Attributes.Count == 0)
				{
					this.m_attrsExpand = ExpandStyle.None;
					goto SKIPATTRS;
				}

				// ����ϣ������չ��
				if (newAttrsExpand == ExpandStyle.Expand)
				{
					this.ClearAttrs();
					foreach(XmlNode attrXmlNode in node.Attributes )
					{
						AttrItem attr = null;

						attr = (AttrItem)allocator.newItem(attrXmlNode,
							this.m_document);
						if (attr != null) 
						{
							int nRet = attr.Initial(attrXmlNode, 
								allocator,
								null,
                                true);
							if (nRet == -1)
								return -1;

							this.AppendAttrInternal(attr,false,true);


						}

						attr.m_bConnected = true;
					}

					this.m_objAttrsTimestamp ++ ;
					this.m_attrsExpand = ExpandStyle.Expand;
				}
				else if (newAttrsExpand == ExpandStyle.Collapse)
				{
					// ע,����ʱ��Ҫ�����Իٵ�

					// ����ϣ����������
					this.m_attrsExpand = ExpandStyle.Collapse;
				}
				else
				{
					this.m_attrsExpand = ExpandStyle.None;
				}
			}

			SKIPATTRS:

				// �����¼�
				if (this.m_bWantChildInitial == 1)
				{
					if (node.ChildNodes.Count == 0)  //û��null�����
					{
						this.m_childrenExpand = ExpandStyle.None;
						goto SKIPCHILDREN;
					}

					// ϣ���¼�չ��
					if (newChildExpand == ExpandStyle.Expand)
					{
						if (elementStyle.bReinitial == false
							|| this.m_xmlChildrenTimestamp > this.m_objChildrenTimestmap)
						{
							this.ClearChildren();

							// �����¼�
							foreach(XmlNode child in node.ChildNodes) 
							{
								Item item;
								item = allocator.newItem(child,
									this.m_document);

								if (item == null)
									continue;


								ElementInitialStyle childStyle = 
									new ElementInitialStyle();
								childStyle.attrsExpandStyle = ExpandStyle.Expand;
								childStyle.childrenExpandStyle = ExpandStyle.Expand;
								childStyle.bReinitial = false;
								int nRet = item.Initial(child,
									allocator,
									childStyle,
                                    bConnect);  // �̳����洫��Ĳ���
								if (nRet == -2)
									return -2;
								if (nRet <= -1)
								{
									return nRet;
								}

								// ���ﲻ��AppendChildInternal����flush��ԭ����
								// Initial()�׶α�ʾֻ�޸ĵ�һ�Σ�����ÿ��Ԫ���޸ĸ���һ��
								this.AppendChildInternal(item,false,true);

								//item.m_bConnected = true;
							}
							this.m_objChildrenTimestmap ++;
						}
						this.m_childrenExpand = ExpandStyle.Expand;
					
					}
					else if (newChildExpand == ExpandStyle.Collapse)
					{
						this.m_childrenExpand = ExpandStyle.Collapse;
					}
					else
					{
						this.m_childrenExpand = ExpandStyle.None;
					}
				}

			SKIPCHILDREN:
				/*
							// end
							ItemCreatedEventArgs args = new ItemCreatedEventArgs();
							args.item = this;
							args.bInitial = true;
							this.m_document.fireItemCreated(this,args);
				*/

				return 0;
		}



		// ������ػ����InitialVisual()
		public override void InitialVisual()
		{
			bool bHasVisual = false;
			Label label = this.GetLable();

			if (label == null)
			{
				// ��Label
				label = new Label();
				label.container = this;
				label.Text = this.Name;
				this.AddChildVisual(label);
			}
			else
			{
				label.Text = this.Name;
				bHasVisual = true;
			}

			Box boxTotal = null;
			if (bHasVisual == false)
			{
				// ����һ���ܿ�
				boxTotal = new Box ();
				boxTotal.Name = "BoxTotal";
				boxTotal.container = this;
				this.AddChildVisual (boxTotal);

				// �����ܿ��layoutStyle��ʽΪ����
				boxTotal.LayoutStyle = LayoutStyle.Vertical;
			}
			else
			{
				boxTotal = this.GetBoxTotal();
				if (boxTotal == null)
				{
					Debug.Assert(false,"��Lable����,������û��BoxTotal����");
					throw new Exception("��Lable����,������û��BoxTotal����");
				}
			}

			///
			if (boxTotal != null)
				InitialVisualSpecial(boxTotal);
			///

			/*
						if (bHasLable == false)
						{
							//���boxTotalֻ��һ��box������Ϊ����
							if (boxTotal.childrenVisual != null && boxTotal.childrenVisual .Count == 1)
								boxTotal.LayoutStyle = LayoutStyle.Horizontal ;

							Comment comment = new Comment ();
							comment.container = this;
							this.AddChildVisual(comment);
						}
			*/	
		
			this.m_strTempURI = null;

		}


		public override void InitialVisualSpecial(Box boxTotal)
		{
			boxTotal.LayoutStyle = LayoutStyle.Vertical;
			if (this.m_bWantAttrsInitial == 1)
			{
				// �ҵ��ɵ�BoxAttributes,ɾ��
				Box oldBoxAttributes = this.GetBoxAttributes(boxTotal);
				if (oldBoxAttributes != null)
					boxTotal.childrenVisual.Remove(oldBoxAttributes);

				if (this.m_attrsExpand != ExpandStyle.None)
				{
					if (this.m_attrsExpand == ExpandStyle.Expand
						&& this.attrs.Count == 0)
					{
						goto SKIPATTRS;
					}
					//�������չ����ť�����ԵĴ��
					Box boxAttrs = new Box ();
					boxAttrs.Name = "BoxAttributes";
					boxAttrs.LayoutStyle = LayoutStyle.Horizontal;
					boxAttrs.container = boxTotal;

					//����չ����ť
					ExpandHandle expandAttributesHandle = new ExpandHandle ();
					expandAttributesHandle.Name = "ExpandAttributes";
					expandAttributesHandle.container = boxAttrs;
					boxAttrs.AddChildVisual(expandAttributesHandle);

					//�������Կ�
					Attributes attributes = new Attributes();
					attributes.container = boxAttrs;
					attributes.LayoutStyle = LayoutStyle.Vertical; //һ����Ԫ�ض������ţ�����ֻ���޸�����
					boxAttrs.AddChildVisual(attributes);

					// ���Դ��ӵ��ܴ����
					// ���Դ����Զ�����ܴ��ĵ�һλ
					if (boxTotal.childrenVisual == null)
						boxTotal.childrenVisual = new ArrayList();
					boxTotal.childrenVisual.Insert(0,boxAttrs);//.AddChildVisual (boxAttrs);

					//���Կ��ʼ��
					attributes.InitialVisual();   //��仰����Ҫ
				}
			}

			SKIPATTRS:

				if (this.m_bWantChildInitial == 1)
				{
					// �ҵ��ɵ�BoxContent,ɾ��
					Box oldBoxContent = this.GetBoxContent(boxTotal);
					if (oldBoxContent != null)
						boxTotal.childrenVisual.Remove(oldBoxContent);


					if (this.m_childrenExpand != ExpandStyle.None)
					{
						if (this.m_childrenExpand == ExpandStyle.Expand
							&& this.children.Count == 0)
						{
							return;
						}
						Box boxContent = new Box ();
						boxContent.Name = "BoxContent";
						boxContent.container = boxTotal;
						boxContent.LayoutStyle = LayoutStyle.Horizontal;

						ExpandHandle expandContentHandle = new ExpandHandle();
						expandContentHandle.Name = "ExpandContent";
						expandContentHandle.container = boxContent;
						boxContent.AddChildVisual(expandContentHandle);

						Content content = new Content();
						content.container = boxContent;
						content.LayoutStyle = LayoutStyle.Vertical  ;//Vertical ;
						boxContent.AddChildVisual(content);

						boxTotal.AddChildVisual(boxContent);
					
						//����contna�ĳ�ʼ��visual����
						content.InitialVisual();
					}
				}
		}


	
		// �ؽ�attributes�ڲ��ṹ
		public void AttributesReInitial()
		{
			Attributes attributes = this.GetAttributes();
			if (attributes == null)
			{
				this.InitialVisual();
				return;
			}
			else
			{
				attributes.InitialVisual();
			}
		}


		// �ؽ�content�ڲ��ṹ
		public void ContentReInitial()
		{
			Content content = this.GetContent();
			if (content == null)
			{
				this.InitialVisual();
				return;
			}
			else
			{
				content.InitialVisual();		
			}
		}


		public Box GetBoxAttributes(Box boxTotal)
		{
			if (boxTotal.childrenVisual == null)
				return null;

			Box boxAttributes = null;
			for(int i=0;i<boxTotal.childrenVisual.Count;i++)
			{
				Visual visual = (Visual)boxTotal.childrenVisual[i];
				if (visual.Name == "BoxAttributes")
				{
					boxAttributes = (Box)visual;
					break;
				}
			}
			return boxAttributes;
		}

		public Box GetBoxContent(Box boxTotal)
		{
			if (boxTotal.childrenVisual == null)
				return null;

			Box boxContent = null;
			for(int i=0;i<boxTotal.childrenVisual.Count;i++)
			{
				Visual visual = (Visual)boxTotal.childrenVisual[i];
				if (visual.Name == "BoxContent")
				{
					boxContent = (Box)visual;
					break;
				}
			}
			return boxContent;
		}

		public Content GetContent()
		{
			Box boxTotal = this.GetBoxTotal();
			if (boxTotal == null)
				return null;

			Box boxContent = this.GetBoxContent(boxTotal);
			if (boxContent == null)
				return null;

			foreach(Visual visual in boxContent.childrenVisual )
			{
				if (visual is Content )
					return (Content)visual;
			}
			return null;
		}

		
		// ��box���ҵ�Content
		public Attributes GetAttributes()
		{
			Box boxTotal = this.GetBoxTotal();
			if (boxTotal == null)
				return null;

			Box boxAttributes = this.GetBoxAttributes(boxTotal);
			if (boxAttributes == null)
				return null;

			foreach(Visual visual in boxAttributes.childrenVisual )
			{
				if (visual is Attributes )
					return (Attributes)visual;
			}
			return null;
		}

		
		//�õ�attributes�ر�ʱ��text
		public Text GetAttributesText()
		{
			Box boxTotal = this.GetBoxTotal();
			if (boxTotal == null)
				return null;

			Box boxAttributes = this.GetBoxAttributes(boxTotal);
			if (boxAttributes == null)
				return null;

			foreach(Visual visual in boxAttributes.childrenVisual )
			{
				if (visual is Text )
					return (Text)visual;				
			}
			return null;
		}

		
		// �õ�content�ر�ʱ��text
		public Text GetContentText()
		{
			Box boxTotal = this.GetBoxTotal();
			if (boxTotal == null)
				return null;

			Box boxContent = this.GetBoxContent(boxTotal);
			if (boxContent == null)
				return null;

			foreach(Visual visual in boxContent.childrenVisual )
			{
				if (visual is Text )
					return (Text)visual;				
			}
			return null;
		}


		public ItemList Children
		{
			get
			{
				return this.children;
			}
		}

		public ItemList Attributes
		{
			get
			{
				return this.attrs;
			}
		}

		public override string NamespaceURI 
		{
			get 
			{
				if (m_strTempURI != null)
					return m_strTempURI;	// �ڲ���ǰ��ժ����������

				string strURI = "";
				AttrItem namespaceAttr = null;
				// ����һ��ǰ׺�ַ���, �����Ԫ�ؿ�ʼ����, �����ǰ׺�ַ����������ﶨ���URI��
				// Ҳ����Ҫ�ҵ�xmlns:???=???���������Զ��󣬷�����namespaceAttr�����С�
				// �����ӷ��ص�namespaceAttr�����п����ҵ�����URI��Ϣ������Ϊ��ʹ���������㣬
				// ������Ҳֱ����strURI�����з��������е�URI
				// parameters:
				//		startItem	���element����
				//		strPrefix	Ҫ���ҵ�ǰ׺�ַ���
				//		strURI		[out]���ص�URI
				//		namespaceAttr	[out]���ص�AttrItem�ڵ����
				// return:
				//		ture	�ҵ�(strURI��namespaceAttr���з���ֵ)
				//		false	û���ҵ�
				bool bRet = ItemUtil.LocateNamespaceByPrefix(
					this,
					this.Prefix,
					out strURI,
					out namespaceAttr);
				if (bRet == false) 
				{
					if (this.Prefix == "")
						return "";
					return null;
				}
				return strURI;
			}
		}

		public ExpandStyle AttrsExpand
		{
			get
			{
				return this.m_attrsExpand;
			}
			set
			{
				this.m_attrsExpand = value;
			}
		}

		public ExpandStyle ChildrenExpand
		{
			get
			{
				return this.m_childrenExpand;
			}
			set
			{
				this.m_childrenExpand = value;
			}
		}


		public static XmlNamespaceManager GatherOuterNamespaces(
			ElementItem element,
			NameTable nt)
		{
			XmlNamespaceManager nsColl = new XmlNamespaceManager(nt);

			ElementItem current = element;
			string strName = element.Name;
		
			while(true)
			{
				if (current == null
					|| current == current.m_document.VirtualRoot)
				{
					break;
				}

				nsColl.PushScope();

				foreach(AttrItem attr in current.attrs)
				{
					if (attr.IsNamespace == false)
						continue;

					nsColl.AddNamespace(attr.LocalName, attr.GetValue());	// ֻҪprefix�ؾͲ�����
				}

				current = (ElementItem)current.parent;
			}

			return nsColl;
		}



		// ����xml�༭���е����ݴ�������
		void BuildAttrs()
		{
			//this.SetFreshValue();

			Debug.Assert(m_attrsExpand == ExpandStyle.Collapse, "���Ǳպ�״̬��Ҫ�ñ�����");

			NameTable nt = new NameTable();

			// �������ֿռ�
			XmlNamespaceManager nsmgr = GatherOuterNamespaces(
				this,
				nt);

			XmlParserContext context = new XmlParserContext(nt,
				nsmgr,
				null,
				XmlSpace.None);


			string strAttrsXml = "";
			// 2.�õ�attributes�ر�ʱ��text
			Text oText = this.GetAttributesText();
			if (oText != null) 
			{
				strAttrsXml = oText.Text.Trim();

				if (strAttrsXml != "")
					strAttrsXml = " " + strAttrsXml;
			}
			else 
			{
				Debug.Assert(false, "������text����");
			}

			string strFragmentXml = "<" + this.Name + strAttrsXml + "/>";

            /*
            TextReader tr = new StringReader(strFragmentXml);
            XmlReaderSettings settings = new XmlReaderSettings();
            settings..XmlResolver = resolver;

            XmlReader reader = XmlReader.Create(tr, settings,
                context);
             * */

			// ��һ��ʱ����XmlSchemaУ��
			XmlValidatingReader reader =
				new XmlValidatingReader(strFragmentXml,
				XmlNodeType.Element,
				context);
			
			// ������schema����У��
			reader.ValidationType = ValidationType.None; 

			this.attrs = new ItemList();

			while(true)
			{
				if (reader.Read() == false)
					break;

				if (reader.MoveToFirstAttribute() == false)
					goto CONTINUE;

				while(true)
				{
					AttrItem attr = this.m_document.CreateAttrItem(reader.Name);
					attr.Initial(reader);
					this.AppendAttrInternal(attr,false,true);
					if (reader.MoveToNextAttribute() == false)
						break;
				}

			CONTINUE:
				break;
			}
		}


		// ����xml�༭���е����ݴ������
		void BuildDescendant()
		{
			// ���ü�����ķ���

			//this.SetFreshValue();

			Debug.Assert(this.m_childrenExpand == ExpandStyle.Collapse, "���Ǳպ�״̬��Ҫ�ñ�����");

			string strAdditionalNsString = "";

			string strInnerXml = "";
			// 2.�õ�attributes�ر�ʱ��text
			Text oText = this.GetContentText();
			if (oText != null) 
			{
				strInnerXml = oText.Text.Trim();
			}
			else 
			{
				Debug.Assert(false, "������text����");
			}

			NamespaceItemCollection nsColl = NamespaceItemCollection.GatherOuterNamespaces(
				(ElementItem)this);

			if (nsColl.Count > 0)
			{
				strAdditionalNsString = nsColl.MakeAttrString();
			}

			string strXml = "";
			if (this == this.m_document.VirtualRoot)
			{
				strXml = strInnerXml;
			}
			else
			{
				strXml = "<root "+ strAdditionalNsString + " >\r\n" + strInnerXml + "</root>";
			}

			this.ClearChildren();

			this.children = new ItemList();


			XmlDocument dom = new XmlDocument();
			dom.LoadXml(strXml);

			XmlNode root = null;
			if (this == this.m_document.VirtualRoot)
			{
				root = dom;
			}
			else
			{
				root = dom.DocumentElement;
			}

			foreach(XmlNode node in root.ChildNodes)
			{
				Item item = this.m_document.allocator.newItem(node,
					this.m_document);

			
				ElementInitialStyle style = new ElementInitialStyle();
				style.attrsExpandStyle = ExpandStyle.Expand;
				style.childrenExpandStyle = ExpandStyle.Expand;
				style.bReinitial = false;

				item.Initial(node,
					this.m_document.allocator,
					style,
                    true);

				// ���������ΪFlush����ĺ��������Բ�Ӧ��ʹ��ʱ����Ӵ�
				this.AppendChildInternal(item,false,true);
			}

		}


		// ����ռ��б�
		public ItemList NamespaceList
		{
			get
			{
				ItemList namespaceList = new ItemList();
				
				ElementItem item = this;
				while(item != null)
				{
					for(int i=item.attrs.Count-1;i>=0;i--)
					{
						if (((AttrItem)item.attrs[i]).IsNamespace == true)
							namespaceList.Add(item.attrs[i]);
					}
					item = (ElementItem)item.parent;
				}

				return namespaceList;
			}
		}


		// �����б�
		// �������ֿռ�����Խڵ㲻��������
		public ItemList PureAttrList
		{
			get
			{
				ItemList attrList = new ItemList();
				for(int i=0;i<this.attrs.Count;i++)
				{
					AttrItem attr = (AttrItem)this.attrs[i];

					if (attr.IsNamespace == false)
						attrList.Add(attr);
				}
				return attrList;
			}
			
		}


		public string AttrsXml
		{
			get
			{
				//SetFreshValue();
				return GetAttrsXml();
			}
		}



		// �Ż�xml
		public void YouHua()
		{
			this.YouHuaOneLevel();

			// Ŀ���ǰ�"������"Ҳȥ��
			this.InitialVisual();

			int nWidth , nHeight;
			this.Layout(this.Rect.X,
				this.Rect.Y,
				this.Rect.Width,
				0,   //��Ϊ0����Ҫ�Ǹ߶ȱ仯
				this.m_document.nTimeStampSeed++,
				out nWidth,
				out nHeight,
				LayoutMember.Layout | LayoutMember.Up);


			//��Ϊ��ǰֵ
			if (ItemUtil.IsBelong(this.m_document.m_selectedItem,
				this) == true)
			{
				this.m_document.SetCurText(this,null);
				this.m_document.SetActiveItem(this);
			}
			else
			{
				// û�иı�curText������Ҫ���裬��ʵ������Ϸ�ʱ�����Ż���
				this.m_document.SetEditPos();
			}

			this.m_document.AfterDocumentChanged(ScrollBarMember.Both);
			this.m_document.Invalidate();

			// �ĵ������仯��
			this.m_document.FireTextChanged();

			this.Flush();
			return;

		}


		// �Ż���������ֿռ�
		public void YouHuaOneLevel()
		{
			NamespaceItemCollection namespaceItems = new NamespaceItemCollection();
	
			ItemList parentNamespaces = null;
			if (this.parent != null)
			{
				parentNamespaces = ((ElementItem)this.parent).NamespaceList;
			}

			if (this.attrs != null
				&& parentNamespaces != null)
			{
				ArrayList aNamespaceAttr = new ArrayList();
				foreach(AttrItem attr in this.attrs)
				{
					if (attr.IsNamespace == true)
					{
						//namespaceItems.Add(attr.Name,attr.Value);
							
						foreach(Item parentNamespace in parentNamespaces)
						{
                            if (parentNamespace.Name == attr.Name
                                && parentNamespace.Value != attr.Value)
                            {
                                break;
                            }

							if (parentNamespace.Name == attr.Name
								&& parentNamespace.Value == attr.Value)
							{
								aNamespaceAttr.Add(attr);
							}
						}
					}
				}

				foreach(AttrItem namespaceAttr in aNamespaceAttr)
				{
					this.RemoveAttrInternal(namespaceAttr,true);   // Ҫ�ĳ�Internal
				}
			}

			if (this.children != null)
			{
				foreach(Item item in this.children)
				{
					if (item is ElementItem)
					{
						ElementItem element = (ElementItem)item;
						element.YouHuaOneLevel();
					}
				}
			}
		}


		// ��ն��ӣ�������ItemDeleted�¼�,�һ��ElementItem���͵Ķ��ӽ��е���
		public void ClearChildren()
		{
			for(int i=0;i<this.children.Count;i++)
			{
				Item child = this.children[i];
				this.RemoveChildInternal(child,false);
			}
			this.m_objChildrenTimestmap ++;
		}


		// ������ԣ�������ItemDeleted�¼�
		public void ClearAttrs()
		{
			foreach(AttrItem attr in this.attrs)
			{
				string strXPath = this.GetXPath();
				////////////////////////////////////////////////
				// ItemDeleted
				///////////////////////////////////////////////
				ItemDeletedEventArgs args = 
					new ItemDeletedEventArgs();
				args.item = attr;
				args.XPath = strXPath;

				// ÿ�ΰ�off��,������Ҫʱ��Ϊon
				args.RiseAttrsEvents = false;
				args.RecursiveChildEvents = false;
				this.m_document.fireItemDeleted(this.m_document,args);
			}

			this.attrs.Clear();
		}

		// �������������̬Ҫ�����Ƚ���Flush()
		public void FlushAncestor()
		{
			ElementItem element = this.parent;
			while(true)
			{
				if (element == null)
					break;
				if (element.m_attrsExpand == ExpandStyle.Collapse
					|| element.m_childrenExpand == ExpandStyle.Collapse)
				{
					element.Flush();
				}
				element = element.parent;
			}
		}



		public void Flush()
		{
			///////////////////////////////////
			//1. �����Խ���Flush
			////////////////////////////////////

			// �ڴ���� ���� xml�༭������Ҫ�����ݶ�������ݶ��ֵ�xml�༭��
			if (this.m_objAttrsTimestamp >= this.m_xmlAttrsTimestamp)
			{
				Text text = this.GetAttributesText();
				if (text != null)
				{
					text.Text = GetAttrsXmlWithoutFlush();
					if (this.m_document.m_curText == text)
						this.m_document.VisualTextToEditControl();
				}
			}
				// �ڴ���� С�� xml�༭��,����Ҫ����xml�༭��������ؽ��ڴ����
			else if (this.m_objAttrsTimestamp < this.m_xmlAttrsTimestamp)
			{
				// �ؽ��ڴ�ṹ
				BuildAttrs();
			}

			this.m_xmlAttrsTimestamp = 0;
			this.m_objAttrsTimestamp = 0;



			///////////////////////////////////
			//2. �Զ��ӽ���Flush
			////////////////////////////////////
			
			// �ڴ���� ���� xml�༭������Ҫ�����ݶ�������ݶ��ֵ�xml�༭��
			if (this.m_objChildrenTimestmap >= this.m_xmlChildrenTimestamp)
			{
				Text text = this.GetContentText();
				if (text != null)
				{
					text.Text = this.GetInnerXml(null);
					if (this.m_document.m_curText == text)
						this.m_document.VisualTextToEditControl();
				}
			}
				// �ڴ���� С�� xml�༭��,����Ҫ����xml�༭��������ؽ��ڴ����
			else if (this.m_objChildrenTimestmap < this.m_xmlChildrenTimestamp)
			{
				// �ؽ��ڴ�ṹ
				this.BuildDescendant();
			}

			this.m_objChildrenTimestmap  = 0;
			this.m_xmlChildrenTimestamp = 0;


			// ʧЧ
			this.m_document.Invalidate(this.Rect);

			// Ӱ���ϼ�
			this.FlushAncestor();
		}
	

		// ׷����Ԫ�ؽڵ� 
		// ����ж�һ�¼��������Ԫ�ض�������
		// ������ElementItem ���� TextItem
		internal void AppendChildInternal(Item item,
			bool bAddObjTimestamp,
			bool bInitial)
		{
			Debug.Assert(item != null,"item��������Ϊnull");

			Debug.Assert(!(item is AttrItem),"item��������ΪAttrItem����");
			
			item.parent = this;
			this.children.Add(item);

			// ���NamespaceURI�Ƿ����
			if (item is ElementItem
				&& bInitial == false)
			{
				ElementItem element = (ElementItem)item;
				string strError;
				int nRet = element.ProcessElementNsURI(bInitial,
					out strError);
				if (nRet == -1)
				{
					this.RemoveChildInternal(item,true);
					throw(new PrefixNotDefineException(strError));
				}
			}

			if (this.m_bConnected == true)
			{
                /*
                if (item is ElementItem)
                    FireItemCreatedTree((ElementItem)item);  // �ݹ鴥���¼�
                else
                {*/
                    // end ���¼�
                    ItemCreatedEventArgs endArgs = new ItemCreatedEventArgs();
                    endArgs.item = item;
                    item.m_bConnected = true;

                    this.m_document.fireItemCreated(this.m_document, endArgs);
                //}

				if (item is ElementItem)
				{
					ElementItem elem = (ElementItem)item;
					elem.SendAttrsCreatedEvent();  // �����������ԺͶ���
				}
			}

			if (bAddObjTimestamp == true)
				this.m_objChildrenTimestmap ++;
		}

        /*
        static void FireItemCreatedTree(ElementItem element)
        {
            ItemCreatedEventArgs endArgs = new ItemCreatedEventArgs();
            endArgs.item = element;
            element.m_bConnected = true;

            element.m_document.fireItemCreated(element.m_document, endArgs);

        
            // �ݹ�
            for (int i = 0; i < element.children.Count; i++)
            {
                Item item = element.Children[i];
                if (!(item is ElementItem))
                    continue;
                FireItemCreatedTree((ElementItem)item);
            }
        
        }
        */


        // ׷�����Խڵ�
        public AttrItem AppendAttrInternal(AttrItem attr,
			bool bAddObjTimestamp,
			bool bInitial)
		{
			// ȥ��ԭͬ�������Խڵ�
			AttrItem oldAttr = this.GetAttrItem(attr.Name);
			if (oldAttr != null)
				this.RemoveAttrInternal(oldAttr,false);  //���Ժ�׷�ӽڵ�����һ��

			attr.parent = this;
			this.attrs.Add(attr);

			if (bInitial == false)
			{
				string strError;
				int nRet = this.ProcessAttrNsURI(attr,
					bInitial,
					out strError);
				if (nRet == -1)
				{
					this.RemoveAttrInternal(attr,true);
					throw(new PrefixNotDefineException(strError));
				}
			}

			// ��ItemCreated��Ϣ
			if (this.m_bConnected == true)
			{
				// ItemCreated�¼�
				ItemCreatedEventArgs endArgs = new ItemCreatedEventArgs();
				endArgs.item = attr;
				endArgs.bInitial = false;
				this.m_document.fireItemCreated(this.m_document,endArgs);	

				attr.m_bConnected = true;
			}

			if (bAddObjTimestamp == true)
				this.m_objAttrsTimestamp ++;

			return attr;
		}
		
		// ɾ���� item������ElementItem �� TextItem
		internal void RemoveChildInternal(Item item,
			bool bAddObjTimestamp)
		{
			Debug.Assert(item != null,"RemoveChild() item����Ϊnull,���ó���");

			// ��ǰ�ڵ� �����ڶ��Ӽ���
			int nIndex = this.children.IndexOf(item);
			if (nIndex == -1)
			{
				Debug.Assert(false,"RemoveChild() item�����ڶ��Ӽ��ϣ����ó���");
				return;
			}

			////////////////////////////////////////////////
			// BeforeItemDelete
			///////////////////////////////////////////////
			string strXPath = item.GetXPath();  // �ȵõ�Xpath,����ɾ�����û����
			BeforeItemDeleteEventArgs beforeArgs = 
				new BeforeItemDeleteEventArgs();
			beforeArgs.item = item;
			this.m_document.fireBeforeItemDelete(this.m_document,beforeArgs);


			// ��һЩ���õĳ�ֵ��ã�����NamespaceURi,Value
			if (item is ElementItem)
			{
				// ע��ݹ��¼�
				this.SetNamespaceURI((ElementAttrBase)item);  

				// �ݹ��¼���value???????????
			}
			else
			{
				// ����ʱ������ã�Ŀ������һ��Ԫ�ر�ɾ���󣬻����Լ���ʹ������Value����
				item.m_paraValue1 = item.GetValue();
			}

			if (ItemUtil.IsBelong(item,this.m_document.m_selectedItem))
			{
				this.m_document.SetActiveItem(null);
				this.m_document.SetCurText(null,null);
			}

			// ��Remove()����
			this.children.Remove(item);

			////////////////////////////////////////////////
			// ItemDeleted
			///////////////////////////////////////////////
			if (item is ElementItem)
			{
				ElementItem element = (ElementItem)item;
				
				element.FireTreeRemoveEvents(strXPath);
			}
			else
			{
				ItemDeletedEventArgs args = 
					new ItemDeletedEventArgs();
				args.item = item;
				args.XPath = strXPath;

				// ÿ�ΰ�off��,������Ҫʱ��Ϊon
				args.RiseAttrsEvents = false;
				args.RecursiveChildEvents = false;
				this.m_document.fireItemDeleted(this.m_document,args);
			}

			if (bAddObjTimestamp == true)
				this.m_objChildrenTimestmap ++;
		}

		
		// ɾ����
		internal void RemoveAttrInternal(AttrItem attr,
			bool bAddObjTimestamp)
		{
			Debug.Assert(attr != null,"RemoveAttr() attr����Ϊnull,���ó���");

			// ��ǰ�ڵ� �����ڶ��Ӽ���
			int nIndex = this.attrs.IndexOf(attr);
			if (nIndex == -1)
			{
				Debug.Assert(false,"RemoveChild() attr���������Լ��ϣ��������");
				return;
			}

			////////////////////////////////////////////////
			// BeforeItemDelete
			///////////////////////////////////////////////
			string strXPath = attr.GetXPath();  // �ȵõ�Xpath,����ɾ�����û����
			BeforeItemDeleteEventArgs beforeArgs = 
				new BeforeItemDeleteEventArgs();
			beforeArgs.item = attr;
			this.m_document.fireBeforeItemDelete(this.m_document,beforeArgs);


			// ��һЩ���õĳ�ֵ��ã�����NamespaceURi
			this.SetNamespaceURI((ElementAttrBase)attr);  
			attr.m_paraValue1 = attr.GetValue();


			// ����Remove����
			this.attrs.Remove(attr);


			////////////////////////////////////////////////
			// ItemDeleted
			///////////////////////////////////////////////
			ItemDeletedEventArgs args = 
				new ItemDeletedEventArgs();
			args.item = attr;
			args.XPath = strXPath;

			// ÿ�ΰ�off��,������Ҫʱ��Ϊon
			args.RiseAttrsEvents = false;
			args.RecursiveChildEvents = false;
			this.m_document.fireItemDeleted(this.m_document,args);

			if (bAddObjTimestamp == true)
				this.m_objAttrsTimestamp ++;
		}

        // �������������Ի�Ԫ�أ�
        // paramters:
        //      referenceItem   ����λ�òο�Ԫ�ء������뵽��Ԫ��֮ǰ
        // Exception:
        //      ���ܻ��׳�PrefixNotDefineException�쳣
        internal void InsertChildInternal(Item referenceItem,
			Item newItem,
			bool bAddObjTimestamp,
			bool bInitial)
		{
			int nIndex = -1;
			nIndex = this.children.IndexOf (referenceItem);
			if (nIndex == -1)
			{
				Debug.Assert (false,"Insert()ʱ��startItem��Ȼ����children��");
				return;
			}
			this.InsertChildInternal(nIndex,
				newItem,
				bAddObjTimestamp,
				bInitial); 
		}

		// �������Ի����¼�Ԫ��(���������)
        // Exception:
        //      ���ܻ��׳�PrefixNotDefineException�쳣
        internal void InsertChildInternal(int nIndex,
			Item newItem,
			bool bAddObjTimestamp,
			bool bInitial)
		{
			newItem.parent = this;
			this.children.Insert(nIndex,newItem);

			// ���NamespaceURI�Ƿ����
			if (newItem is ElementItem && bInitial == false)
			{
				string strError;
                int nRet = ((ElementItem)newItem).ProcessElementNsURI( // old : this
					bInitial,
					out strError);
				if (nRet == -1)
				{
					this.RemoveChildInternal(newItem,true);
					throw(new PrefixNotDefineException(strError));
				}
			}

			if (this.m_bConnected == true)
			{
                /*
				// end ���¼�
                if (newItem is ElementItem)
                    FireItemCreatedTree((ElementItem)newItem);  // �ݹ鴥���¼�
                else
                {*/

                    ItemCreatedEventArgs args = new ItemCreatedEventArgs();
                    args.item = newItem;
                    //args.bInitial = false;

                    newItem.m_bConnected = true;
                    this.m_document.fireItemCreated(this.m_document, args);
                //}

				if (newItem is ElementItem)
				{
					ElementItem elem = (ElementItem)newItem;
					elem.SendAttrsCreatedEvent();
				}
			}

			if (bAddObjTimestamp == true)
				this.m_objChildrenTimestmap ++;
		}

		// �������Ի����¼�Ԫ��(���ڵ�ָ��)
		internal void InsertAttrInternal(AttrItem startAttr,
			AttrItem newAttr,
			bool bAddObjTimestamp,
			bool bInitial)
		{
			int nIndex = this.attrs.IndexOf (startAttr);
			if (nIndex == -1)
			{
				Debug.Assert (false,"Insertʱ��startItem��Ȼ����children��");
				return;
			}
			this.InsertAttrInternal(nIndex,
				newAttr,
				bAddObjTimestamp,
				bInitial);
		}

		// �������Ի����¼�Ԫ��(���������)
		internal void InsertAttrInternal(int nIndex,
			AttrItem newAttr,
			bool bAddObjTimestamp,
			bool bInitial)
		{
			newAttr.parent = this;

			this.attrs.Insert(nIndex,newAttr);

			if (bInitial == false)
			{
				string strError;
				int nRet = this.ProcessAttrNsURI(newAttr,
					bInitial,
					out strError);
				if (nRet == -1)
				{
					this.RemoveAttrInternal(newAttr,true);
					throw(new PrefixNotDefineException(strError));
				}
			}
			// ��ItemCreated()
			if (this.m_bConnected == true)
			{
				// ItemCreated�¼�
				ItemCreatedEventArgs endArgs = new ItemCreatedEventArgs();
				endArgs.item = newAttr;
				//endArgs.bInitial = false;
				this.m_document.fireItemCreated(this.m_document,endArgs);	
				newAttr.m_bConnected = true;
			}
			
			if (bAddObjTimestamp == true)
				this.m_objAttrsTimestamp ++;  
		}


		// ȡһ�����Խڵ�
		public AttrItem GetAttrItem(string strAttrName)
		{
			if (this.attrs == null)
				return null;

			foreach(AttrItem attr in this.attrs)
			{
				if (attr.Name == strAttrName)
					return attr;
			}
			return null;
		}

		// �õ�һ�����Խڵ��ֵ
		public string GetAttrValue(string strAttrName)
		{
			AttrItem attr = this.GetAttrItem(strAttrName);
			if (attr != null)
				return attr.GetValue();

			return "";
		}

		// ����һ�����Խڵ��ֵ
		// ���ָ�������Բ����ڣ����´�������
		public void SetAttrValue(string strAttrName,
			string strNewAttrValue)
		{
			if (strNewAttrValue == null)
				strNewAttrValue = "";

			AttrItem attr = this.GetAttrItem(strAttrName);
			if (attr == null)
			{
				attr = this.m_document.CreateAttrItem(strAttrName);
				attr.SetValue(strNewAttrValue);
				this.AppendAttrInternal(attr,true,false);
			}
			else
			{
				string strOldValue = attr.GetValue();

				// �޸�ֵ
				if (strOldValue != strNewAttrValue)
				{
					// ��Ҫ��
					attr.SetValue(strNewAttrValue);
					this.m_objAttrsTimestamp ++;

					// �����¼�
					////////////////////////////////////////////////////
					// ItemAttrChanged
					////////////////////////////////////////////////////
					ItemChangedEventArgs args = 
						new ItemChangedEventArgs();
					args.item = attr;
					args.NewValue = strNewAttrValue;
					args.OldValue = strOldValue;
					this.m_document.fireItemChanged(this.m_document,args);
				}
			}

			this.AfterAttrCreateOrChange(attr);
		}


		// ��������ռ���һ������
		public virtual string GetAttribute(string strLocalName,
			string strNamespaceURI)
		{
			if (this.attrs == null)
				return "";

			for(int i=0;i<this.attrs.Count;i++)
			{
				AttrItem attr = (AttrItem)this.attrs[i];
				if (attr.Name == strLocalName
					&& attr.NamespaceURI == strNamespaceURI)
				{
					return attr.GetValue();
				}
			}

			return "";
		}

		
		#region �����Ż����ֿռ�ĺ���

		public enum GetPrefixStyle
		{
			ElementNameUsed = 1,
			AttributesUsed = 2,
			Defined = 4,
			All = 7,
		}

		// ���element�����ù��Ļ��߶����prefix��֮һ��������ϡ�
		// parameters:
		//		style	���ȡ
		public Hashtable GetPrefix(GetPrefixStyle style)
		{
			Hashtable aPrefix = new Hashtable();
			
			// Ԫ������ǰ׺
			if ((style & GetPrefixStyle.ElementNameUsed) == GetPrefixStyle.ElementNameUsed) 
			{
				aPrefix.Add(this.Prefix, null);
			}

			for(int i=0; i<this.attrs.Count; i++)
			{
				AttrItem attr = (AttrItem)this.attrs[i];

				bool bProcess = false;
				string strPrefix = "";

				if ((style & GetPrefixStyle.Defined) == GetPrefixStyle.Defined) 
				{
					if (attr.IsNamespace == true) 
					{
						strPrefix = attr.LocalName;
						bProcess = true;
					}
				}
				if (bProcess == false
					&& (style & GetPrefixStyle.AttributesUsed) == GetPrefixStyle.AttributesUsed) 
				{
					if (attr.IsNamespace == false) 
					{
						strPrefix = attr.Prefix;
						bProcess = true;
					}
				}

				if (bProcess == false)
					continue;

				if (aPrefix.Contains(strPrefix) == false)
					aPrefix.Add(strPrefix, null);

			}


			return aPrefix;
		}

		// ���element�����ù���prefix
		// parameters:
		//		bRemoveDefined	�Ƿ����߱����Ѿ������ǰ׺
		public Hashtable GetUsedPrefix(bool bRemoveDefined)
		{
			Hashtable aDefinedPrefix = null;
			
			if (bRemoveDefined == true) 
			{
				aDefinedPrefix = new Hashtable();

				for(int j=0; j<this.attrs.Count; j++)
				{
					AttrItem attr = (AttrItem)this.attrs[j];

					Debug.Assert(attr.LocalName != null, "");

					if (attr.IsNamespace == false)
						continue;

/*
					if (attr.LocalName == "xml")
						continue;
*/
					if (aDefinedPrefix.Contains(attr.LocalName) == false)
						aDefinedPrefix.Add(attr.LocalName, null);
					else 
					{
						Debug.Assert(false, "��һ��Ԫ�ز���У������ܳ����ظ���������ֿռ�ǰ׺");
					}

				}

				if (aDefinedPrefix.Count == 0)
					aDefinedPrefix = null;
			}

			Hashtable aPrefix = new Hashtable();


			// Ԫ������ǰ׺
			if (aDefinedPrefix != null) // �����Ƿ����ڱ�����Ѷ����
			{
				if (aDefinedPrefix.Contains(this.Prefix) == false)
					aPrefix.Add(this.Prefix, null);
			}
			else 
			{
				aPrefix.Add(this.Prefix, null);
			}

			for(int i=0; i<this.attrs.Count; i++)
			{
				AttrItem attr = (AttrItem)this.attrs[i];

				if (attr.IsNamespace == true)
					continue;


				if (attr.Prefix == "") 
					continue;

				// �����Ƿ����ڱ�����Ѷ����
				if (aDefinedPrefix != null)
				{
					if (aDefinedPrefix.Contains(attr.Prefix) == true)
						continue;
				}			

				if (aPrefix.Contains(attr.Prefix) == false)
					aPrefix.Add(attr.Prefix, null);
			}


			/*
			ArrayList aResult = new ArrayList();

			aResult.AddRange(aPrefix.Keys);
			*/

			return aPrefix;
		}


		#endregion


		public override string GetValue()
		{
			Debug.Assert(false,"��������ʹ�øú���");
			return null;
		}

		public override void SetValue(string strValue)
		{
			Debug.Assert(false,"��������ʹ�øú���");
		}

		internal override Text GetVisualText()
		{
			Debug.Assert(false,"��������ʹ�øú���");
			return null;
		}


		#region InnerXml �� OuterXml


		public void SetText(string strValue)
		{
			this.ClearChildren();

			TextItem text = this.m_document.CreateTextItem();
			text.SetValue(strValue);

			this.AppendChild(text);
		}


		public string GetText()
		{
			return this.GetInnerText(false);
		}


		// ������е��ı��ڵ��ֵ
		// parameter:
		//		bRecursion	�Ƿ���¼����еݹ�
		public string GetInnerText(bool bRecursion)
		{
			string strInnerText = "";
			foreach(Item item in this.children)
			{
				if (item is ElementItem)
				{
					if (bRecursion == true)
						strInnerText += ((ElementItem)item).GetInnerText(bRecursion);
				}
				if (item is TextItem)
					strInnerText += item.Value;
			}
			return strInnerText;
		}



		// �õ�InnerXml����
		// parameters:
		//		FragmentRoot	�Ƿ�����ⲿ���ֿռ���Ϣ�����Ҫ���룬Ƭ�θ�Ԫ����ʲô��
		//				���==null�����еĽڵ㶼������������ƿռ���Ϣ�����!=null�����в������Ҫ�����ܻ���϶�������ֿռ���Ϣ
		public string GetInnerXml(ElementItem FragmentRoot)
		{
			string strContent = "";
			// ͨ���ݹ���ӻ��strContent
			for(int i=0; i<this.children.Count;i++)
			{
				Item child = (Item)this.children[i];

				if (child is ElementAttrBase) 
				{
					strContent += child.GetOuterXml(FragmentRoot != null ? (ElementItem)child : null);
				}
				else 
				{
					strContent += child.GetOuterXml(null);
				}
			}
			return strContent;
		}

		internal string GetAttrsXmlWithoutFlush()
		{
			string strAttrXml = "";

			// �ڴ���� ���� xml�༭��
			if (this.m_objAttrsTimestamp >= this.m_xmlAttrsTimestamp)
			{
				for(int i=0; i<this.attrs.Count;i++)
				{
					AttrItem attr = (AttrItem)this.attrs[i];

					strAttrXml += " " + attr.GetOuterXml(null);
				}

				return strAttrXml;
			}
			else
			{
				// 2.�õ�attributes�ر�ʱ��text
				Text oText = this.GetAttributesText();
				if (oText != null) 
				{
					strAttrXml = oText.Text.Trim();

					if (strAttrXml != "")
						strAttrXml = " " + strAttrXml;

					return strAttrXml;
				}
				else 
				{
					Debug.Assert(false, "������text����");
					return "";
				}
			}
		}

		// ��ñ�����������������XML�ַ���
		// ������Բ����ڣ�����""������������ݣ���ǰ�ն�û�к��
		internal string GetAttrsXml()
		{
			string strAttrXml = "";

			for(int i=0; i<this.attrs.Count;i++)
			{
				AttrItem attr = (AttrItem)this.attrs[i];
				strAttrXml += " " + attr.GetOuterXml(null);
			}
			return strAttrXml;
		}

		// ������չ����������״̬���ܻ�ö������ֿռ��GetOuterXml()����汾��������չ�����������
		// ע�⣺���������ַ�������һ���������������
		public string GetOuterXmlSpecial()
		{
			Debug.Assert (this != this.m_document.VirtualRoot,
				"��Ҫ��������,Ӧ��������������GetInnerXml()����GetOuterXml()");

			string strAdditionalNsString = "";
			string strXml = this.GetOuterXml(null);

			NamespaceItemCollection nsColl = NamespaceItemCollection.GatherOuterNamespaces(
				(ElementItem)this.parent);

			if (nsColl.Count > 0)
			{
				strAdditionalNsString = nsColl.MakeAttrString();
			}

			return "<root "+ strAdditionalNsString + " >\r\n" + strXml + "</root>";
		}


		// parameters:
		//		FragmentRoot	���Ҫ����������ֿռ�Ļ���Ƭ�εĶ���element����
		//			���==null����ʾ���ؼ����������ֿռ���Ϣ
		internal override string GetOuterXml(ElementItem FragmentRoot)
		{
			int i;
			string strOuterXml = "";

			string strContent = "";
			string strAttrXml = "";

			// ͨ���ݹ���ӻ��strContent
			for(i=0; i<this.children.Count;i++)
			{
				Item child = (Item)this.children[i];
				strContent += child.GetOuterXml(FragmentRoot);
			}

			if (this == this.m_document.VirtualRoot) 
			{
				return strContent;
			}

			string strAdditional = "";
			if (FragmentRoot != null)  //��Ҫ�Ӷ�������ֿռ�
			{
				// ������Ҫ����Ķ������ֿռ�����
				ArrayList aPrefix = null;
				ItemUtil.GetUndefinedPrefix(this,
					FragmentRoot,
					out aPrefix);

				for(i=0;i<aPrefix.Count;i++)
				{
					string strPrefix = (string)aPrefix[i];
					if (strPrefix == "xml")
						continue;

					string strURI = "";
					AttrItem foundAttr = null;

					bool bRet = ItemUtil.LocateNamespaceByPrefix(this,	// �����Ż�ΪFragmentRoot�ĸ���
						strPrefix,
						out strURI,
						out foundAttr);
					if (bRet == false)
					{
						if (strPrefix != "")
						{
							throw(new Exception("ǰ׺" +strPrefix+ "û���ҵ�����λ��"));
						}
						else
							continue;
					}

					if (strPrefix != "")
						strAdditional += " xmlns:" + strPrefix + "='" + StringUtil.GetXmlStringSimple(strURI) + "'";
					else
						strAdditional += " xmlns='" + StringUtil.GetXmlStringSimple(strURI) + "'";

				}
			}

			// �ƺ������Ż�������GetAttrsXml()?
			for(i=0; i<this.attrs.Count;i++)
			{
				AttrItem attr = (AttrItem)this.attrs[i];

				strAttrXml += " " + attr.GetOuterXml(FragmentRoot);
			}

			if (strAdditional != "")
				strAttrXml += strAdditional;


			//if (strAttrXml != "")
			//	strAttrXml += " ";

			Debug.Assert(this.Name != "", "ElementItem��Name��ӦΪ��");

			Debug.Assert(this != this.m_document.VirtualRoot, "ǰ���Ѿ�������,�������ߵ�����");	// ǰ���Ѿ�������,�������ߵ�����

			strOuterXml = "<" + this.Name + strAttrXml + ">" + strContent + "</" + this.Name + ">";
			return strOuterXml;
		}


		// �õ�InnerXml����
		// �ڲ���Ҫ���˺���
		public string InnerXml
		{
			get
			{
				return this.GetInnerXml(this);
			}
		}

		// �ڲ����Բ��ܵ�������,��Ϊ���ж�������ֿռ���Ϣ
		public override string OuterXml 
		{
			get
			{
				return this.GetOuterXml(this);
			}
			set 
			{
				string strError = "";
				int nRet = this.m_document.PasteOverwrite(value,
					this,
					false,
					out strError);
				if (nRet == -1)
				{
					throw(new Exception(strError));
				}
			}
		}


		#endregion


		#region ����չ������

		// expandAttrs	    ����״̬
		// expandChildren	����״̬
		public void ExpandAttrsOrChildren(ExpandStyle expandAttrs,
			ExpandStyle expandChildren,
			bool bChangeDisplay)
		{
			bool bOldChanged = this.m_document.m_bChanged;

			//����Ϊ�ȴ�״̬
			Cursor cursorSave = this.m_document.Cursor;
			if (bChangeDisplay == true) 
			{
				this.m_document.Cursor = Cursors.WaitCursor;
			}

			ElementInitialStyle style = new ElementInitialStyle();
			style.childrenExpandStyle = expandChildren;
			style.attrsExpandStyle = expandAttrs;
			style.bReinitial = true;

			string strXml = "";
			if (this == this.m_document.VirtualRoot)
			{
				strXml = this.GetOuterXml(null);
			}
			else
			{
				strXml = this.GetOuterXmlSpecial();
			}


			XmlDocument dom = new XmlDocument ();
			try
			{
				dom.LoadXml(strXml);
			}
			catch(Exception ex)
			{
				throw(new Exception("ExpandChild()�ڲ�����" + ex.Message));
			}
			
			if (this == this.m_document.VirtualRoot)
			{
				// ��ʼ��item��Σ�ע��ʹ�ø��µĵ�һ��Ԫ��
				this.Initial(dom,
					this.m_document.allocator,
					style,
                    true);
			}
			else 
			{
				Debug.Assert(dom.DocumentElement.ChildNodes.Count == 1, "�����xml�ַ���doc����������ֻ��һ������");

				// ��ʼ��item��Σ�ע��ʹ�ø��µĵ�һ��Ԫ��
				this.Initial(dom.DocumentElement.ChildNodes[0],
					this.m_document.allocator,
					style,
                    true);
			}


			// ���³�ʼ��Visual���
			this.InitialVisual();


			// 5.item����layout,���ﻹ��ԭ����rect,��˼��item�ߴ粻�䣬��Ҫ�޸�����ĳߴ�
			int nRetWidth,nRetHeight;
			this.Layout (this.Rect.X,
				this.Rect.Y,
				this.Rect.Width,
				0,
				this.m_document.nTimeStampSeed,
				out nRetWidth,
				out nRetHeight,
				LayoutMember.Layout);

			this.Layout(this.Rect.X,
				this.Rect.Y,
				this.Rect.Width,  
				this.Rect.Height,
				this.m_document.nTimeStampSeed ++,
				out nRetWidth,
				out nRetHeight,
				LayoutMember.EnLargeHeight | LayoutMember.Up  );


			this.m_document.nTimeStampSeed++;


			/*
						if (this.SelectedItem != null) 
						{
							// ���CurItem1��element������֮һ
							if (this.SelectedItem is AttrItem
								&& this.SelectedItem.parent == element)
							{
								if (expandAttrs == ExpandStyle.Collapse)
								{
									this.SelectedItem = element;
								}
							}
							else
							{
								if (ItemUtil.IsBelong(this.SelectedItem,
									element))
								{
									this.SelectedItem = element;
								}
							}
						}
			*/			
			if (bChangeDisplay == true) 
			{
				//layout���ĵ��ߴ緢���仯�����Ե��˺���
				this.m_document.AfterDocumentChanged(ScrollBarMember.Both);

				// ���ΪʲôҪ�������أ���Ҳ��̫���
				this.m_document.DocumentOrgX = this.m_document.DocumentOrgX;
				this.m_document.DocumentOrgY = this.m_document.DocumentOrgY;
				
				//�Ļ�ԭ������״̬
				this.m_document.Cursor = cursorSave;

				this.m_document.m_bChanged = bOldChanged;

				this.m_document.Invalidate();	
			}

			//this.SelectedItem = element;

			this.m_childrenExpand = expandChildren;
			this.m_attrsExpand = expandAttrs;

			this.Flush();
		}

		#endregion

		#region ���Բ���
		

		// parameter:
		//		element ����������Խڵ�
		// return:
		//		-1	error
		//		0	successed
		internal int ProcessAttrNsURI(AttrItem attr,
			bool bInitial,
			out string strError)
		{
			strError = "";

			// ������������ ��ȱʡ���ֿռ�
			if (attr.Prefix == null
				|| attr.Prefix == "")
			{
				return 0;
			}

			string strSaveURI = attr.NamespaceURI;

			attr.m_strTempURI = null;

			string strUpDefineURI = attr.NamespaceURI;
			if (strUpDefineURI != null)
				return 0;

			if (bInitial == true)
				return 0;

			attr.m_strTempURI = strSaveURI;

			// ˵���Ѿ�������ˣ�������ʱ����
			if (attr.m_strTempURI == null)
			{
				strError = "δ�ҵ�ǰ׺'" + attr.Prefix + "'��Ӧ��URI";
				return 0;
			}

			AttrItem attrNs = this.m_document.CreateAttrItem("xmlns:" + attr.Prefix);
			attrNs.SetValue(attr.m_strTempURI);
			attrNs.IsNamespace = true;
			attr.parent.AppendAttrInternal(attrNs,true,false);

			// �Ѳ����ÿ�
			attr.m_strTempURI = null;
			return 0;
		}

		
		// ����: ��һ��Ԫ����ǰ����һ��ͬ��Ԫ�ػ�������
		// parameter:
		//		newItem     Ҫ�������Item
		//		refChild    �ο�λ�õ�Ԫ��
		//		strError    out���������س�����Ϣ
		// return:
        //      -1  ����
        //      0   �ɹ�
		public int InsertAttr(AttrItem startAttr,
			AttrItem newAttr, 
			out string strError)
		{
			Cursor oldCursor = this.m_document.Cursor;
			this.m_document.Cursor =  Cursors.WaitCursor;
			try
			{
				strError = "";
				if (startAttr == null)
				{
					Debug.Assert(false,"InsertAttr()ʱ�������startAttrΪnull");
					strError = "InsertAttr()ʱ�������startAttrΪnull";
					return -1;
				}
				if (newAttr == null)
				{
					Debug.Assert(false,"InsertAttr()ʱ�������newAttrΪnull");
					strError = "InsertAttr()ʱ�������newAttrΪnull";
					return -1;
				}


				// 1.��InsertAttr()�������Ѹ��׹�ϵ����
				this.InsertAttrInternal(startAttr,
					newAttr,
					true,
					false);




				this.AfterAttrCreateOrChange(newAttr);

				return 0;
			}
			finally
			{
				this.m_document.Cursor = oldCursor;
			}
		}


		// �ı�����:�ϼ����������ԣ������޸ĵ�����ֵ���˺����������������
		public void AfterAttrCreateOrChange(AttrItem attr)
		{
			Cursor oldCursor = this.m_document.Cursor;
			this.m_document.Cursor =  Cursors.WaitCursor;
			try
			{
				// �踸��״̬
				this.m_bWantAttrsInitial = 1;
				if (this.attrs.Count > 0
					&& this.AttrsExpand == ExpandStyle.None)
				{
					this.AttrsExpand = ExpandStyle.Expand;
				}
			
				// ���³�ʼ��Attributes����
				this.AttributesReInitial();



				int nWidth, nHeight;
				this.Layout(this.Rect.X,
					this.Rect.Y,
					this.Rect.Width,
					0,
					this.m_document.nTimeStampSeed++,
					out nWidth,
					out nHeight,
					LayoutMember.Layout | LayoutMember.Up );

				// ���²�������Ա�Ϊ��ǰ��Ķ���
				// this.m_document.SetActiveItem(attr);
				// this.m_document.SetCurText(attr);

				// ������
				this.m_document.AfterDocumentChanged(ScrollBarMember.Both);
				this.m_document.Invalidate();  //??���Χ
			
				// �ĵ������仯��
				this.m_document.FireTextChanged();

				this.Flush();
			}
			finally
			{
				this.m_document.Cursor = oldCursor;
			}
		}

		

		// ׷������
		public int AppendAttr(AttrItem attr,
			out string strError)
		{
			Cursor oldCursor = this.m_document.Cursor;
			this.m_document.Cursor =  Cursors.WaitCursor;
			try
			{
				strError = "";
				this.AppendAttrInternal(attr,true,false);

				this.AfterAttrCreateOrChange(attr);
				return 0;
			}
			finally
			{
				this.m_document.Cursor = oldCursor;
			}
		}

		#endregion

		#region ���Ӳ���

		
		// parameter:
		//		bInitial	�Ƿ���Initial�׶�
		// return:
		//		-1	����
		//		0	�ɹ�
		internal int ProcessElementNsURI(bool bInitial,
			out string strError)
		{
			strError = "";

			// ������������ ��ȱʡ���ֿռ�
			if (this.Prefix == null
				|| this.Prefix == "")
			{
				return 0;
			}

			string strSaveURI = this.NamespaceURI;
			
			this.m_strTempURI = null;

			string strUpDefineURI = this.NamespaceURI;
			// ˵���ϼ��Ѿ��������prefix��Ӧ��URI�����ô������ֿռ����Խڵ���
			if (strUpDefineURI != null)
				return 0;

			if (bInitial == true)
				return 0;

			this.m_strTempURI = strSaveURI;
			// ˵���Ѿ�������ˣ�������ʱ����
			if (this.m_strTempURI == null)
			{
				strError = "û��ָ��'" + this.Prefix + "'ǰ׺��Ӧ��URI";
				return -1;
			}


			AttrItem attrNs = this.m_document.CreateAttrItem("xmlns:" + this.Prefix);
			attrNs.SetValue(this.m_strTempURI);
			attrNs.IsNamespace = true;

			this.AppendAttrInternal(attrNs,true,false);

			// m_strTempURI������null
			this.m_strTempURI = null;

			return 0;
		}


		// �Զ�������PrefixNotDefineException�쳣��AppendChild�汾
		public void AutoAppendChild(Item newChildItem)
		{
			REDO:
				try 
				{
					this.AppendChild(newChildItem);
				}
				catch (PrefixNotDefineException ex)
				{
					AddNsDefineDlg dlg = new AddNsDefineDlg();

					if (!(newChildItem is ElementItem)) 
					{
						Debug.Assert(false, "");
						throw ex;
					}

					dlg.textBox_prefix.Text = ((ElementItem)newChildItem).Prefix;
					dlg.label_message.Text = "�ڲ���Ԫ�� '" + newChildItem.Name + "' �����У�����ǰ׺ '"
						+ dlg.textBox_prefix.Text + "' δ���塣�������ǰ׺�Ķ��塣";
					dlg.ShowDialog(this.m_document);

					if (dlg.DialogResult != DialogResult.OK) 
					{
						MessageBox.Show(this.m_document, "��������Ԫ��");
						return;
					}

					// �˴�����Ҳ���Կ����ڸ��׻�����Ԫ�����������ֿռ䶨��Ŀ��ܡ�

					((ElementItem)newChildItem).Prefix = dlg.textBox_prefix.Text;
					((ElementItem)newChildItem).m_strTempURI = dlg.textBox_uri.Text;
					goto REDO;
				}

			// �����쳣��������׳�
		}

		// ׷���¼�
		public void AppendChild(Item newChildItem)
		{
			Cursor oldCursor = this.m_document.Cursor;
			this.m_document.Cursor =  Cursors.WaitCursor;
			try
			{
				if (newChildItem == null)
				{
					Debug.Assert(false,"newChildItem ����Ϊ null");
					throw new Exception("newChildItem ����Ϊ null");
				}


				// ʹ�ڴ����ʱ����Ӵ�
				this.AppendChildInternal(newChildItem,true,false);




				// �踸�׵Ķ�������״̬
				this.m_bWantChildInitial = 1;
				if (this.children.Count > 0
					&& this.ChildrenExpand == ExpandStyle.None)
				{
					this.m_childrenExpand = ExpandStyle.Expand;
				}


				// ���׶�Content��������IntialVisual()
				this.ContentReInitial();

				int nWidth , nHeight;
				this.Layout(this.Rect.X,
					this.Rect.Y,
					this.Rect.Width,
					0,
					this.m_document.nTimeStampSeed++,
					out nWidth,
					out nHeight,
					LayoutMember.Layout | LayoutMember.Up);
		
				this.m_document.AfterDocumentChanged(ScrollBarMember.Both );
				this.m_document.Invalidate();

				// �ĵ������仯
				this.m_document.FireTextChanged();

				this.Flush();
			}
			finally
			{
				this.m_document.Cursor = oldCursor;
			}
		}


		public void SendAttrsCreatedEvent()
		{
			foreach(AttrItem attr in this.attrs)
			{
				if (attr.m_bConnected == true)
					continue;

				ItemCreatedEventArgs endArgs = new ItemCreatedEventArgs();
				endArgs.item = attr;
				endArgs.bInitial = false;
				this.m_document.fireItemCreated(this.m_document,endArgs);

				attr.m_bConnected = true;
			}
		}

		// ��һ��Ԫ��׷��һ���¼�Ԫ�ؽڵ�
		// parameter:
		//		newChildItem	������ElementItem ���� TextItem
		//		bCallBeforeDelegate �Ƿ��BeforeDelegate
		// return:
        //      -1  ����
        //      -2  ����
        //      0   �ɹ�
		public int AppendChild(Item newChildItem,
			out string strError)
		{
			strError = "";
			try
			{
				this.AppendChild(newChildItem);
			}
			catch(Exception ex)
			{
				strError = ex.Message;
				return -1;
			}
			return 0;
		}



		// ����: ��һ��Ԫ����ǰ����һ��ͬ��Ԫ��
		// parameter:
        //		referenceChild    �ο�λ�õ�Ԫ��
		//		newItem     Ҫ�������Item
		//		strError    out���������س�����Ϣ
		// return:
        //      -1  ����
		//		0   �ɹ�
        // Exception:
        //      ���ܻ��׳�PrefixNotDefineException�쳣
		public int InsertChild(Item referenceChild,
			Item newChild, //�κζ�������
			out string strError)
		{
			Cursor oldCursor = this.m_document.Cursor;
			this.m_document.Cursor =  Cursors.WaitCursor;
			try
			{
				strError = "";
				if (referenceChild == null)
				{
					Debug.Assert(false,"InsertChild()ʱ�������startChildΪnull");
					strError = "InsertChild()ʱ�������startChildΪnull";
					return -1;
				}
				if (newChild == null)
				{
					Debug.Assert(false,"InsertChild()ʱ�������newChildΪnull");
					strError = "InsertChild()ʱ�������newChildΪnull";
					return -1;
				}

				// ������ڵ�û���ٲ���ͬ��
				if (referenceChild == this.m_document.VirtualRoot)
				{
					strError = "���Ԫ�ز��ܲ���ͬ��Ԫ��";
					return -1;
				}

				// �Ǹ��ڵ�û���ٲ���ͬ��
				if (referenceChild == this.m_document.docRoot)
				{
					strError = "��Ԫ�ز��ܲ���ͬ��Ԫ��";
					return -1;
				}


				// 1.��InsertAttr()�������Ѹ��׹�ϵ����
				// ʹ�ڴ����Ӵ�

                // !!! ������ܻ��׳�PrefixNotDefineException�쳣
                    this.InsertChildInternal(referenceChild,
                        newChild,
                        true,
                        false);

				// ����ͬ������Ȼ��չ����״̬������ѡ�в��˵�ǰ�ڵ㣬���Բ����踸�׵�״̬��



				// 2.�����ؽ�Visual��ϵ
				this.ContentReInitial();

				// 4.��Layout()
				int nWidth,nHeight;
				this.Layout(this.Rect.X,
					this.Rect.Y,
					this.Rect.Width,  //����ڵ㣬���ı���
					0,
					this.m_document.nTimeStampSeed++,
					out nWidth,
					out nHeight,
					LayoutMember.Layout  | LayoutMember.Up); //Ӱ���ϼ�

				this.m_document.AfterDocumentChanged (ScrollBarMember.Both);
				this.m_document.Invalidate ();

				// �ĵ������ı�
				this.m_document.FireTextChanged();

				this.Flush();
				return 0;
			}
			finally
			{
				this.m_document.Cursor = oldCursor;
			}
		}

		#endregion

		#region ɾ��ǰ׺

		// ����ǰ׺����һ�����ֿռ��Ƿ����ɾ��
		public bool CoundDeleteNs(string strPrefix)
		{
			bool bDefinded = this.CheckDefindedByAncestor(strPrefix);
			if (bDefinded == true)
				return true;

			bool bUsing = this.CheckUseNs(strPrefix);
			if (bUsing == false)
				return true;

			return false;

		}

		// ���һ��ǰ׺�Ƿ����ϼ����˶���
		public bool CheckDefindedByAncestor(string strPrefix)
		{
			ElementItem curElement = this.parent;
			while(true)
			{
				if (curElement == null)
					break;

				foreach(AttrItem attr in curElement.attrs)
				{
					if (attr.LocalName == strPrefix)
						return true;
				}
				curElement = curElement.parent;
			}
			return false;

		}

		// �����Լ������Լ��¼�ʹ��ʹ�����ǰ׺
		public bool CheckUseNs(string strPrefix)
		{
			if (this.Prefix == strPrefix)
				return true;

			foreach(AttrItem attr in this.attrs)
			{
				if (attr.Prefix == strPrefix)
					return true;
			}
			foreach(Item child in this.children)
			{
				if (!(child is ElementItem))
					continue;
				
				bool bUser = ((ElementItem)child).CheckUseNs(strPrefix);
				if (bUser == true)
					return true;
			}
			return false;
		}

		#endregion

		#region ɾ��һ���ڵ�
		
		// parameter:
		//		item	Ҫɾ���Ľڵ�
		//		bForceDelete	��Ϊ���ֿռ䣬�����ڱ���ʱ���Ƿ�ǿ��ɾ���ڵ�
		// return
		//		false	δɾ��
		//		true	ɾ����
		public bool Remove(Item item,
			bool bForceDelete)
		{

			if (!(item is AttrItem))
				return this.Remove(item);

			AttrItem attr = (AttrItem)item;
			if (attr.IsNamespace == false)
				return this.Remove(item);

			bool bCoundDelete = this.CoundDeleteNs(attr.LocalName);
			if (bCoundDelete == true)
				return this.Remove(item);

			if (bForceDelete == true)
				return this.Remove(item);

			return false;

		}

		// ɾ��һ���¼��ڵ�
		internal bool RemoveChild(Item item)
		{
			if (item == null)
			{
				Debug.Assert(false,"RemoveChild(),item��������Ϊnull");
				return false;
			}
			if (item is AttrItem)
			{
				Debug.Assert(false,"�˴�item����ΪAttrItem����");
				return false;
			}
			// ��ʱitem����������������ô���
			if (item == this.m_document.VirtualRoot)  ///???????????
			{
				Debug.Assert(false,"��ʱitem����������������ô���");
				return false;
			}


			// ��ǰ��ڵ��ǲ��ǰ����ڱ�ɾ���Ľڵ��ڣ�
			// ����ǣ����item�����ҵ�һ������Ľڵ㡣���ѻ�ڵ� , curText , edit����ȷ
			bool bBeLong = false;
			Item hereAboutItem = null;
			if (ItemUtil.IsBelong(this.m_document.m_selectedItem,
				item) == true)
			{
				hereAboutItem = ItemUtil.GetNearItem(item,
					MoveMember.Auto);
				bBeLong = true;
			}


			// �ڴ����λ
			this.RemoveChildInternal(item,true);

			// ��ͼ����λ
			if (this.children.Count == 0)
			{
				// Ŀ���ǰ�"������"Ҳȥ��
				this.InitialVisual();
			}
			else
			{
				this.ContentReInitial();
			}

			int nWidth , nHeight;
			this.Layout(this.Rect.X,
				this.Rect.Y,
				this.Rect.Width,
				0,   //��Ϊ0����Ҫ�Ǹ߶ȱ仯
				this.m_document.nTimeStampSeed++,
				out nWidth,
				out nHeight,
				LayoutMember.Layout | LayoutMember.Up);


			//��Ϊ��ǰֵ
			if (bBeLong == true)
			{
				this.m_document.SetCurText(hereAboutItem,null);
				this.m_document.SetActiveItem(hereAboutItem);
			}
			else
			{
				// û�иı�curText������Ҫ���裬��ʵ������Ϸ�ʱ�����Ż���
				this.m_document.SetEditPos();
			}

			this.m_document.AfterDocumentChanged(ScrollBarMember.Both);
			this.m_document.Invalidate();

			// �ĵ������仯
			this.m_document.FireTextChanged();

			this.Flush();
			return true;
		}

		internal bool RemoveAttr(AttrItem attr)
		{
			if (attr == null)
			{
				Debug.Assert(false,"RemoveAttr() attr��������Ϊnull");
				return false;
			}
			if (this == this.m_document.VirtualRoot)
			{
				Debug.Assert(false,"this����Ϊ���");
				return false;
			}
	
			// ��ǰ��ڵ��ǲ��Ǿ���Ҫɾ���Ľڵ�
			// ����ǣ����attr�����ҵ�һ������Ľڵ㡣���ѻ�ڵ� , curText , edit����ȷ
			bool bBeLong = false;
			Item hereAboutItem = null;
			if (this.m_document.m_selectedItem == attr)
			{
				hereAboutItem = ItemUtil.GetNearItem(attr,MoveMember.Auto);
				bBeLong = true;
			}

				
			this.RemoveAttrInternal(attr,true);


			if (this.attrs.Count == 0)
			{
				// Ŀ����Ϊ�˰�"������"Ҳȥ��
				this.InitialVisual();
			}
			else
			{
				this.AttributesReInitial();
			}

			int nWidth , nHeight;
			this.Layout(this.Rect.X,
				this.Rect.Y,
				this.Rect.Width,
				0,   //��Ϊ0����Ҫ�Ǹ߶ȱ仯
				this.m_document.nTimeStampSeed++,
				out nWidth,
				out nHeight,
				LayoutMember.Layout | LayoutMember.Up );


			//��Ϊ��ǰֵ
			if (bBeLong == true)
			{
				this.m_document.SetCurText(hereAboutItem,null);
				this.m_document.SetActiveItem(hereAboutItem);
			}
			else
			{
				this.m_document.SetEditPos ();
			}


			this.m_document.AfterDocumentChanged(ScrollBarMember.Both);
			this.m_document.Invalidate();

			// �ĵ������仯
			this.m_document.FireTextChanged();

			this.Flush();

			return true;
		}


		// ɾ��һ��ָ����Ԫ�� ������ ElemtnItem ,AttrItem ,TextItem
		public bool Remove(Item item)
		{
			Cursor oldCursor = this.m_document.Cursor;
			this.m_document.Cursor =  Cursors.WaitCursor;
			try
			{
				if (item == null)
				{
					Debug.Assert(false,"Remove() item��������Ϊnull");
					return false;
				}

				if (item is AttrItem)
					return this.RemoveAttr((AttrItem)item);
				else
					return this.RemoveChild(item);
			}
			finally
			{
				this.m_document.Cursor = oldCursor;
			}
		}


		// ��ΪNamespaceURI��ͨ����һ�����ԣ���ͨ���ϼ��õ��ģ�
		// ��������Ŀ������ɾ��itemʱ�����������ֿռ�����ò�����ã��Ա����ѹ�ϵʱ�����Լ���ʹ��
		internal void SetNamespaceURI(ElementAttrBase item)
		{
			item.m_strTempURI = item.NamespaceURI;

			if (item is ElementItem)
			{
				ElementItem element = (ElementItem)item;
				
				foreach(AttrItem attr in element.attrs)
				{
					this.SetNamespaceURI(attr);
				}

				foreach(Item child in element.children)
				{
					if (!(child is ElementItem))
						continue;
					this.SetNamespaceURI((ElementItem)child);
				}
			}
		}


		// Ӧ����ɾ��ǰ���¼�����
		public void FireTreeRemoveEvents(string strXPath)
		{
			// ���Լ���ItemDeleted�¼�

			ItemDeletedEventArgs args = 
				new ItemDeletedEventArgs();
			args.item = this;
			args.XPath = strXPath;

			// ÿ�ΰ�off��,������Ҫʱ��Ϊon
			//args.RiseAttrsEvents = false;
			//args.RecursiveChildEvents = false;
			args.RiseAttrsEvents = true;
			args.RecursiveChildEvents = true;
			this.m_document.fireItemDeleted(this.m_document,args);


			if (args.RiseAttrsEvents == true)
			{
				for(int i=0;i<this.attrs.Count;i++)
				{
					AttrItem attr = (AttrItem)this.attrs[i];

					ItemDeletedEventArgs argsAttr = 
						new ItemDeletedEventArgs();
					argsAttr.item = attr;
					argsAttr.XPath = strXPath + "/@" + attr.Name + "";

					// ÿ�ΰ�off��,������Ҫʱ��Ϊon
					//argsAttr.RiseAttrsEvents = false;
					//argsAttr.RecursiveChildEvents = false;
					
					argsAttr.RiseAttrsEvents = true;
					argsAttr.RecursiveChildEvents = true;

					this.m_document.fireItemDeleted(this.m_document,argsAttr);
				}
			}
			if (args.RecursiveChildEvents == true)
			{
				for(int i=0;i<this.children.Count;i++)
				{
					Item child = this.children[i];
					if (child is ElementItem)
					{
						ElementItem element = (ElementItem)this.children[i];

						string strPartXpath = ItemUtil.GetPartXpath(this,
							element);

						string strChildXPath = strXPath + "/" + strPartXpath;

						element.FireTreeRemoveEvents(strChildXPath);

					}
					else
					{
						ItemDeletedEventArgs argsChild = 
							new ItemDeletedEventArgs();
						argsChild.item = child;
						argsChild.XPath = strXPath;

						// ÿ�ΰ�off��,������Ҫʱ��Ϊon
						//argsChild.RiseAttrsEvents = false;
						//argsChild.RecursiveChildEvents = false;

						
						argsChild.RiseAttrsEvents = true;
						argsChild.RecursiveChildEvents = true;
						this.m_document.fireItemDeleted(this.m_document,argsChild);
					}

				}

			}



		}
		#endregion

	}

    public class PrefixNotDefineException : Exception
    {
        public PrefixNotDefineException(string strMessage)
            : base(strMessage)
        { }
    }

}
