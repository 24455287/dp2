using System;
using System.Xml;
using System.IO;

namespace DigitalPlatform.Xml
{
    /*

    <?xml version='1.0' encoding='utf-8' ?>
    <stringtable>
        <s id="1">
            <v lang="zh-CN">����</v>
            <v lang="en">Chinese</v>
        </s>
        <s id="����id">
            <v lang="en">Chinese value</v>
            <v lang="zh-CN">����ֵ</v>

        </s>


    </stringtable>

    */

    /* ������Ϊ�淶�����Ա�ʾ����
    <stringtable>
    <!-- /////////////////////////////////// login ////////////////////////////-->

        <s id="�û���">
            <v lang="zh-CN">�û���</v>
            <v lang="en-us">User name</v>
        </s>
        <s id="����">
            <v lang="zh-CN">����</v>
            <v lang="en-us">Password</v>
        </s>
    </stringtable>	
    */


    /// <summary>
	/// �������ַ�������
	/// </summary>
	public class StringTable
	{
		XmlDocument dom = new XmlDocument();

		public string ContainerElementName = "stringtable";
        public string CurrentLang = "zh-CN"; // ȱʡΪ����
		public string DefaultValue = "????";
		public bool ThrowException = false;

		public string ItemElementName = "s";
		public string ValueElementName = "v";
		public string IdAttributeName = "id";

		public StringTable()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public StringTable(string strFileName)
		{
			this.dom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue

			dom.Load(strFileName);
		}

		public StringTable(Stream s)
		{
			dom.Load(s);
		}

		// ��ָ�������Եõ��������ַ���
		public string this[string strID, string strLang]
		{
			get 
			{
				return GetString(strID,
					strLang,
					ThrowException,
					this.DefaultValue);
			}
			set 
			{
			}
		}

		// �Ե�ǰ���Եõ����������ַ���
		public string this[string strID]
		{
			get 
			{
				return GetString(strID,
					CurrentLang,
					ThrowException,
					this.DefaultValue);
			}
			set 
			{
			}

		}

		// �ɶԳ��ֵ��ַ���
		public string[] GetStrings(string strLang)
		{
			string xpath = "";

			xpath = "//"
				+ ContainerElementName 
				+ "/"+ItemElementName + "/"
				+ ValueElementName + "[@lang='" + strLang + "']";

			XmlNodeList nodes = dom.DocumentElement.SelectNodes(xpath);

			string [] result = new string [nodes.Count*2];

			for(int i=0;i<nodes.Count;i++)
			{
				result[i*2] = DomUtil.GetAttr(nodes[i].ParentNode, "id");
				result[i*2 + 1] = DomUtil.GetNodeText(nodes[i]);
			}

			return result;
		}

		public string GetString(string strID,
			string strLang,
			bool bThrowException,
			string strDefault)
		{
			XmlNode node = null;

			string xpath = "";
			
			if (strLang == null || strLang == "")
			{
				xpath = "//"
					+ ContainerElementName 
					+ "/" + ItemElementName + "[@"
                    + IdAttributeName + "='" + strID + "']/"
					+ ValueElementName;

				node = dom.DocumentElement.SelectSingleNode(xpath);
			}
			else 
			{
			REDO:
				xpath = "//"
					+ ContainerElementName 
					+ "/"+ItemElementName +"[@"
					+ IdAttributeName + "='" + strID + "']/"
					+ ValueElementName + "[@lang='" +strLang + "']";

				node = dom.DocumentElement.SelectSingleNode(xpath);

				// ���ӻ���
				if (node == null)
				{
					int nIndex = strLang.IndexOf('-');
					if (nIndex != -1)
					{
						strLang = strLang.Substring(nIndex+1);
						goto REDO;
					}
				}
			}
			if (node == null) 
			{
				if (bThrowException)
					throw(new StringNotFoundException("idΪ" +strID+ "langΪ"+strLang+"���ַ���û���ҵ�"));

				if (strDefault == "@id")
					return strID;

				return strDefault;
			}

			return DomUtil.GetNodeText(node);
		}


	}

	// �ַ����ڶ��ձ���û���ҵ�
	public class StringNotFoundException : Exception
	{
		public StringNotFoundException (string s) : base(s)
		{
		}
	}
}
