using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using System.Windows.Forms;

using DigitalPlatform.Xml;
using DigitalPlatform.Text;

namespace DigitalPlatform.Marc
{
    // ģ����
	public class TemplateLine:IComparable
	{
		public TemplateRoot container = null;

        public bool IsSensitive = false;
        internal LineState m_lineState = LineState.None;

		private XmlNode m_charNode = null;
		private string m_strLang = null;

		internal string m_strLabel = null;
		internal string m_strName = null;
		internal string m_strValue = null;

        // public XmlNode ValueListNode1 = null;   // ��TemplateLine.Initial()��ʼ��
        public List<XmlNode> ValueListNodes = null;   // ��TemplateLine.Initial()��ʼ��

		public Label Label_label = null;
		public Label Label_Name = null;
        public Label Label_state = null;
        public ValueEditBox TextBox_value = null;   // changed 2006/5/15

		internal int m_nValueLength = 0;
		internal int m_nStart = 0;

        public string DefaultValue = null;

		// parameter:
		//		node	char�ڵ�
		//		strLang	���԰汾
		public TemplateLine(TemplateRoot templateRoot,
			XmlNode node,
			string strLang)
		{
			this.container = templateRoot;
			this.m_charNode = node;
			this.m_strLang = strLang;

			string strError;
			// ͨ��һ��Char�ڵ㣬��ʼ�����е�ֵ
			// parameter:
			//		node	char�ڵ�
			//		strLang	���԰汾
			//		strError	������Ϣ
			// return:
			//		-1	ʧ��
			//		0	�ɹ�
			int nRet = this.Initial(this.m_charNode,
				this.m_strLang,
				out strError);
			if (nRet == -1)
				throw new Exception(strError);
		}

        static string Trim(string s)
        {
            if (string.IsNullOrEmpty(s) == true)
                return s;
            return s.Trim();
        }

        public LineState LineState
        {
            get
            {
                return this.m_lineState;
            }
            set
            {
                this.m_lineState = value;

                if (this.Label_state != null)
                {
                    if (value == Marc.LineState.Macro)
                        this.Label_state.ImageIndex = 0;
                    else if (value == Marc.LineState.Sensitive)
                        this.Label_state.ImageIndex = 1;
                    else if (value == (Marc.LineState.Macro | Marc.LineState.Sensitive))
                        this.Label_state.ImageIndex = 2;
                    else
                        this.Label_state.ImageIndex = -1;
                }
            }
        }


/*
		<Char name='0/5'>
			<Property>
				<Label xml:lang='en'>?</Label>
				<Label xml:lang='cn'>��¼����</Label>
				<Help xml:lang='cn'></Help>
				<ValueList name='header_0/5'>
					<Item>
						<Value>?????</Value>
						<Label xml:lang='cn'>������Զ���д</Label>
					</Item>
				</ValueList>
			</Property>
		</Char>
*/
		// ͨ��һ��Char�ڵ㣬��ʼ�����е�ֵ
		// parameter:
		//		node	char�ڵ�
		//		strLang	���԰汾
		//		strError	������Ϣ
		// return:
		//		-1	ʧ��
		//		0	�ɹ�
		public int Initial(XmlNode node,
			string strLang,
			out string strError)
		{
			strError = "";

			if (node == null)
			{
				strError = "���ô���node��������Ϊnull";
				Debug.Assert(false,strError);
				return -1;
			}

			this.m_strName = Trim(DomUtil.GetAttr(node,"name"));
			if (this.m_strName == "")
			{
				strError = "<Char>Ԫ�ص�name���Կ��ܲ����ڻ���ֵΪ�գ������ļ����Ϸ���";
				Debug.Assert(false,strError);
				return -1;					
			}

			XmlNode propertyNode = node.SelectSingleNode("Property");
			if (propertyNode == null)
			{
				strError = "<Char>Ԫ���¼�δ����<Property>Ԫ�أ������ļ����Ϸ�";
				Debug.Assert(false,strError);
				return -1;
			}

            // <Property>/<sensitive>
            if (propertyNode.SelectSingleNode("sensitive") != null)
            {
                this.IsSensitive = true;
                this.m_lineState |= LineState.Sensitive;
            }
            else
                this.IsSensitive = false;

            // <Property>/<DefaultValue>
            if (propertyNode.SelectSingleNode("DefaultValue") != null)
                this.m_lineState |= LineState.Macro;

            // <Property>/<DefaultValue>
            XmlNode nodeDefaultValue = propertyNode.SelectSingleNode("DefaultValue");
            if (nodeDefaultValue != null)
                this.DefaultValue = nodeDefaultValue.InnerText;

            // ��һ��Ԫ�ص��¼��Ķ��<strElementName>Ԫ����, ��ȡ���Է��ϵ�XmlNode��InnerText
            // parameters:
            //      bReturnFirstNode    ����Ҳ���������Եģ��Ƿ񷵻ص�һ��<strElementName>
            this.m_strLabel = DomUtil.GetXmlLangedNodeText(
        strLang,
        propertyNode,
        "Label",
        true);
            if (string.IsNullOrEmpty(this.m_strLabel) == true)
                this.m_strLabel = "<��δ����>";
            else
                this.m_strLabel = StringUtil.Trim(this.m_strLabel);
#if NO
			XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
			nsmgr.AddNamespace("xml", Ns.xml);
			XmlNode labelNode = propertyNode.SelectSingleNode("Label[@xml:lang='" + strLang + "']",nsmgr);
			if (labelNode == null
                || string.IsNullOrEmpty(labelNode.InnerText.Trim()) == true)
			{
                // ����Ҳ��������ҵ���һ����ֵ��
                XmlNodeList nodes = propertyNode.SelectNodes("Label", nsmgr);
                foreach (XmlNode temp_node in nodes)
                {
                    if (string.IsNullOrEmpty(temp_node.InnerText.Trim()) == false)
                    {
                        labelNode = temp_node;
                        break;
                    }
                }

				//Debug.Assert(false,"����Ϊ'" + this.m_strName + "'��<char>Ԫ��δ����Label��'" + strLang + "'���԰汾��ֵ");
			}
            if (labelNode == null)
                this.m_strLabel = "<��δ����>";
            else
                this.m_strLabel = Trim(DomUtil.GetNodeText(labelNode));
#endif

			// ��value����ֵ
			int nIndex = this.m_strName.IndexOf("/");
			if (nIndex >= 0)
			{
				string strLetterCount = this.m_strName.Substring(nIndex+1);
				this.m_nValueLength = Convert.ToInt32(strLetterCount);
				this.m_nStart = Convert.ToInt32(this.m_strName.Substring(0,nIndex));
			}
			if (this.m_strValue == null)
				this.m_strValue = new string('*',this.m_nValueLength);


            XmlNodeList valuelist_nodes = propertyNode.SelectNodes("ValueList");
            this.ValueListNodes = new List<XmlNode>();
            foreach (XmlNode valuelist_node in valuelist_nodes)
            {
                this.ValueListNodes.Add(valuelist_node);
            }

            return 0;

		}


		// �Ƚ�
		public int CompareTo(object obj)
		{
			TemplateLine line = (TemplateLine)obj;

			return this.m_nStart - line.m_nStart;
		}
	}

    // ֵ����
    public class ValueItem
    {
        public string Lable = null;
        public string Value = null;

        public ValueItem(string strLable,
            string strValue)
        {
            this.Lable = strLable;
            this.Value = strValue;
        }
    }

    // �е�״̬
    [Flags]
    public enum LineState
    {
        None = 0,
        Macro = 0x01,
        Sensitive = 0x02,
    }
}
