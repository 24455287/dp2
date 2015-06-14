using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using System.Text.RegularExpressions;

using System.Threading;
// using System.Resources;
using System.Globalization;

using DigitalPlatform.Text;

using DigitalPlatform.Xml;

// 2005/4/18	����PrevName NextName DupCount����
// 2005/4/18	�ı�ṹ��Ԫ��name���ݶ���취����@Ϊregular expression, ����Ϊԭ����*�ַ���

namespace DigitalPlatform.MarcDom
{
	/// <summary>
	// MARC��¼������
	// ������FilterDocument�ࡣ�������Ϳ�����������չһЩ���ڴ洢�ĳ�Ա��
	// ������������host���󡣿�ϧ��Script������FilterItem.Document�Ƕȿ�����
	// ���ܻ���FilterDocument�����ͣ�ʹ������Ҫcast������Ҳ������
	// script������ΪDocument��Ա���غ���������ʵ������?
	// ���Կ���Ϊ<def>��<begin>����һ���ڶ������ش��룬������ʵ������ת���Ĵ��롣
	/// </summary>
	public class FilterDocument
	{
        public XmlDocument Dom
        {
            get
            {
                return this.dom;
            }
        }

		XmlDocument dom = new XmlDocument();

		Hashtable NodeTable = new Hashtable();

		Assembly assembly = null;

		public string strOtherDef = "";

		public string strPreInitial = "";

		public bool CheckBreakException = true;


		public Assembly Assembly 
		{
			get 
			{
				return assembly;
			}
			set 
			{
				assembly = value;
				// �õ�һ������type��Ϣ

				if (value == null)
				{
					this.NodeTable.Clear();	// �ͷ���Щtype entryָ��
                    Debug.WriteLine("NodeTable Cleared. count" + NodeTable.Count.ToString());

				}
				else 
				{
					string strError;
					int nRet = FillOneLevelType(dom.DocumentElement,
						out strError);
					if (nRet == -1)
						throw(new Exception("FillOneLevelType() error :" + strError));
				}
			}
		}


		public void Load(string strFileName)
		{
			dom.Load(strFileName);

			BuildOneLevelItem(dom.DocumentElement);
		}

        public void LoadContent(string strFileContent)
        {
            dom.LoadXml(strFileContent);

            BuildOneLevelItem(dom.DocumentElement);
        }

        // ���һ��ָ�����Ե��ַ���
        public string GetString(string strLang,
            string strID)
        {
            if (this.dom == null)
                return null;
            XmlNode node = dom.DocumentElement.SelectSingleNode("//stringTable/s[@id='" + strID + "']");
            if (node == null)
                return null;

            return GetT(strLang, node);
        }

        // ��õ�ǰ���Ե��ַ���
        // ���û�о�ȷƥ������ԣ���ģ��ƥ�䣬�򷵻ص�һ�����Ե�
        // �����id�����ڣ�����null
        public string GetString(string strID)
        {
            if (this.dom == null)
                return null;
            XmlNode node = dom.DocumentElement.SelectSingleNode("//stringTable/s[@id='" + strID + "']");
            if (node == null)
                return null;

            string strLang = Thread.CurrentThread.CurrentUICulture.Name;
            return GetT(strLang, node);
        }

        // ȷ����������Ҳ����strID����
        public string GetStringSafe(string strID)
        {
            string strResult = this.GetString(strID);

            if (String.IsNullOrEmpty(strResult) == true)
                return strID;

            return strResult;
        }


        // ��һ��Ԫ�ص��¼�<t>Ԫ����, ��ȡ���Է��ϵ�����ֵ
        public static string GetT(string strLang,
            XmlNode parent)
        {
            XmlNode node = null;

            if (String.IsNullOrEmpty(strLang) == true)
            {
                node = parent.SelectSingleNode("t");  // ��һ��captionԪ��
                if (node != null)
                    return node.InnerText;

                return null;
            }
            else
            {
                node = parent.SelectSingleNode("t[@lang='" + strLang + "']");

                if (node != null)
                    return node.InnerText;
            }

            string strLangLeft = "";
            string strLangRight = "";

            DomUtil.SplitLang(strLang,
               out strLangLeft,
               out strLangRight);

            // ����<t>Ԫ��
            XmlNodeList nodes = parent.SelectNodes("t");

            for (int i = 0; i < nodes.Count; i++)
            {
                string strThisLang = DomUtil.GetAttr(nodes[i], "lang");

                string strThisLangLeft = "";
                string strThisLangRight = "";

                DomUtil.SplitLang(strThisLang,
                   out strThisLangLeft,
                   out strThisLangRight);

                // �ǲ������Ҷ�ƥ�������?������в��ǵ�һ�����ƥ���

                if (strThisLangLeft == strLangLeft)
                    return nodes[i].InnerText;
            }

            // ʵ�ڲ��У���ѡ��һ��<t>������ֵ
            node = parent.SelectSingleNode("t");
            if (node != null)
                return node.InnerText;

            return null;    // not found
        }

        /// <summary>
        /// ȥ����ĩһ��������
        /// </summary>
        /// <param name="strText"></param>
        /// <returns></returns>
        public static string TrimEndChar(string strText, string strDelimeters = "./,;:")
        {
            if (string.IsNullOrEmpty(strText) == true)
                return "";
            strText = strText.Trim();
            if (string.IsNullOrEmpty(strText) == true)
                return "";

            char tail = strText[strText.Length - 1];
            if (strDelimeters.IndexOf(tail) != -1)
                return strText.Substring(0, strText.Length - 1);
            return strText;
        }

		// ����.fltx.cs�ļ�
        // д��ָ���ļ��İ汾
		public int BuildScriptFile(string strOutputFile,
			out string strError)
		{
			string strText = "";
			strError = "";

			int nRet = BuildOneLevelScript(dom.DocumentElement,
				out strText,
				out strError);

			if (nRet == -1)
				return -1;

			// д���ļ�
			StreamWriter sw = new StreamWriter(strOutputFile, false, Encoding.UTF8);
			sw.WriteLine(strText);
			sw.Close();

			return 0;
		}

        // ����.fltx.cs�ļ�
        // �����ַ����İ汾
        public int BuildScriptFile(out string strCode,
            out string strError)
        {
            strCode = "";
            strError = "";

            int nRet = BuildOneLevelScript(dom.DocumentElement,
                out strCode,
                out strError);

            if (nRet == -1)
                return -1;

            return 0;
        }
		
		FilterItem GetRootFilterItem(FilterItem start)
		{
			FilterItem item = start;
			for(;item!=null;) 
			{
				if (item.FilterRoot == item)
					return item;
				item = item.Container;
			}

			return null;
		}

        // ��װ�汾
        // ����һ����¼
		// return:
		//		-1	����
        public int DoRecord(
            object objParam,
            string strMarcRecord,
            int nIndex,
            out string strError)
        {
            return DoRecord(
                objParam,
                strMarcRecord,
                "", // strMarcSyntax��""��ʾ��ÿ��<record>Ԫ�ؾ�ƥ��
                nIndex,
                out strError);
        }

        // ԭʼ�汾
		// ����һ����¼
        // parameters:
        //      strMarcSyntax   MARC�﷨
		// return:
		//		-1	����
		public int DoRecord(
			object objParam,
			string strMarcRecord,
            string strMarcSyntax,
			int nIndex,
			out string strError)
		{
			int nRet;
			strError = "";

            strMarcSyntax = strMarcSyntax.ToLower();

			FilterItem itemFilter = null;

			// ���fltx�ж�����<filter>�ڵ�Ļ�
			XmlNode nodeFilter = dom.DocumentElement.SelectSingleNode("//filter");
			if (nodeFilter != null) 
			{

				itemFilter = this.NewFilterItem(
					objParam,
					nodeFilter,
					out strError);
				if (itemFilter == null)
					return -1;
                
				// ִ��begin���ִ���

				itemFilter.Container = null;
				itemFilter.FilterRoot = itemFilter;
				itemFilter.Index = nIndex;
				itemFilter.OnBegin();

				if (itemFilter.Break == BreakType.SkipCase)
					goto DOEND;
				if (itemFilter.Break == BreakType.SkipCaseEnd)
					return 0;	// ��������
			}

            // ***
            itemFilter.IncChildDupCount("");

			// fltx.cs�п��ܶ�����<record>��Ӧ�࣬Ӧ�����δ�����ִ��

			XmlNodeList nodes = dom.DocumentElement.SelectNodes("//record");
			if (nodes.Count == 0)
				goto DOEND;	// һ��<record>Ҳû�ж���

			BreakType thisBreak = BreakType.None;

			for(int i=0;i<nodes.Count;i++) 
			{
				XmlNode node = nodes[i];

                /*
                 * ע��
                 * 1) ����ÿ�strMarcSyntaxֵ���ñ����������κ�<record>Ԫ�ض�ƥ�䡣����Ϊ�˺���ǰ�ļ���
                 * 2) ���<record>Ԫ��û��syntax���ԣ������÷ǿյ�strMarcSyntax���ñ�������������Ԫ��Ҳ��ƥ��
                 * 3) ���<record>Ԫ����syntax���ԣ���Ҫ�ͷǿյ�strMarcSyntaxֵƥ�䣬����ƥ��<record>��Ԫ��
                 * 
                 * */

                // 2009/10/8
                // ���marc syntax�Ƿ�ƥ��
                if (String.IsNullOrEmpty(strMarcSyntax) == false)
                {
                    string strNodeSyntax = DomUtil.GetAttr(node, "syntax").ToLower();

                    if (String.IsNullOrEmpty(strNodeSyntax) == false)
                    {
                        if (strNodeSyntax != strMarcSyntax)
                            continue;
                    }
                }



				nRet = DoSingleItem(
					objParam,
					node, 
					nIndex,
					itemFilter,
					strMarcRecord, 
					"",
					out thisBreak,
					out strError);
				if (nRet == -1)
					return -1;
				if (thisBreak != BreakType.None)
					break;

				// DoSingleItem()ִ������ȫ���ܸĵ�filter�ĳ�Ա����
				if (itemFilter != null) 
				{
					if (itemFilter.Break == BreakType.SkipCase)
						goto DOEND;
					if (itemFilter.Break == BreakType.SkipCaseEnd)
						return 0;	// ��������
				}

			}

			DOEND:

				if (itemFilter != null) 
				{
					// ִ��end���ִ���
					itemFilter.OnEnd();
				}


			return 0;
		}


		#region �ڲ��߼�

		// ����һ��FilterItem����
		int BuildOneLevelItem(XmlNode xmlNode)
		{
            if (xmlNode.ParentNode == null)
            {
                NodeTable.Clear();
                Debug.WriteLine("NodeTable Cleared. count" + NodeTable.Count.ToString());

            }

			if (xmlNode.NodeType != XmlNodeType.Element) 
			{
				Debug.Assert(false,
					"xmlNode���ͱ���ΪXmlNodeType.Element");
				return -1;
			}


			if (IsStructureElementName(xmlNode.Name) == false) 
			{
				Debug.Assert(false,
					"xmlNode��name����ֵֻ��Ϊ�ṹԪ��");
				return -1;
			}


			HashFilterItem item = new HashFilterItem();

			item.xmlNode = xmlNode;
			item.Name = DomUtil.GetAttr(xmlNode, "name");
			item.ItemType = (ItemType)Enum.Parse(typeof(ItemType), xmlNode.Name, true);
			NodeTable.Add(xmlNode, item);
            Debug.WriteLine("add new NodeTable count" + NodeTable.Count.ToString());

			for(int i =0;i<xmlNode.ChildNodes.Count; i++)
			{
				XmlNode node = xmlNode.ChildNodes[i];

				if (node.NodeType != XmlNodeType.Element)
					continue;


				if (IsStructureElementName(node.Name) == false)
					continue;

				int nRet = BuildOneLevelItem(node);
				if (nRet == -1)
					return -1;
			}


			return 0;
		}

		// ����һ���������Script����
		int BuildOneLevelScript(XmlNode xmlNode,
			out string strResult,
			out string strError)
		{
			strResult = "";
			int nRet;

			string strUsingScript = "";

			if (xmlNode.ParentNode == xmlNode.OwnerDocument) 
			{
				nRet = GetUsingScript(xmlNode,
					out strUsingScript,
					out strError);
				if (nRet == -1)
					return -1;
			}

			string strTab = GetTabString(xmlNode);

			string strBeginScript = "";
			string strEndScript = "";
			string strDefScript = "";

			nRet = GetDefBeginEndScript(xmlNode,
				out strDefScript,
				out strBeginScript,
				out strEndScript,
				out strError);
			if (nRet == -1)
				return -1;

			string strClassName = GetClassName(xmlNode);

			// 
			HashFilterItem item = (HashFilterItem)this.NodeTable[xmlNode];
			if (item == null)
			{
				Debug.Assert(false, "xml�ڵ�" + xmlNode.OuterXml + "û����NodeTable�д�����Ӧ������");
				return -1;
			}
			item.FunctionName = strClassName;

			if (strUsingScript != "")
				strResult = strUsingScript;

			strResult += strTab + "public class " + strClassName + " : FilterItem { ";

			strResult += "// name=" + DomUtil.GetAttr(xmlNode, "name") + "\r\n";

			if (strOtherDef != null)
				strResult += strOtherDef + "\r\n";

			// def
			if (strDefScript != "") 
			{
				strResult += strTab + "// fltx def\r\n";
				strResult += strDefScript;
				strResult += strTab + "\r\n";
			}


			// ����Parent���Դ���
			HashFilterItem itemParent = null;
			if (xmlNode != dom.DocumentElement
				&& xmlNode.ParentNode != null
				// && xmlNode.ParentNode != xmlNode.OwnerDocument
				)
			{
				itemParent = (HashFilterItem)this.NodeTable[xmlNode.ParentNode];
				if (itemParent == null) 
				{
					Debug.Assert(false, "xml�ڵ�" + xmlNode.ParentNode.OuterXml + "û����NodeTable�д�����Ӧ������");
					return -1;
				}			
			}


			if (itemParent != null) 
			{
				strResult += strTab + "public " + itemParent.FunctionName +
					" Parent { get { return (" + itemParent.FunctionName + ")Container;} } \r\n";
			}
			else 
			{
				strResult += strTab + "public " + "FilterItem" +
					" Parent { get { return (" + "FilterItem" + ")null;} } \r\n";
			}

			// ����Root���Դ���
			HashFilterItem itemRoot = null;
			itemRoot = (HashFilterItem)this.NodeTable[xmlNode.OwnerDocument.DocumentElement];
			if (itemRoot == null) 
			{
				Debug.Assert(false, "xml�ڵ�" + xmlNode.OwnerDocument.DocumentElement.OuterXml + "û����NodeTable�д�����Ӧ������");
				return -1;
			}

			strResult += strTab + "public " + itemRoot.FunctionName + 
				" Root { get { return (" + itemRoot.FunctionName + ")FilterRoot;} } \r\n";


			if (strPreInitial != "") 
			{
				// ��ʼ������
				strResult += strTab + "public override void PreInitial() {\r\n";

				strResult += strPreInitial + "\r\n";

				strResult += strTab + "}\r\n";
			}
				




			// begin
			if (strBeginScript != "") 
			{
				strResult += strTab + "// begin\r\n";
				strResult += strTab + "public override void OnBegin() {\r\n";
				strResult += strBeginScript;
				strResult += "\r\n";
				strResult += strTab + "}";
				strResult += strTab + "\r\n";
			}

			for(int i =0;i<xmlNode.ChildNodes.Count; i++)
			{
				XmlNode node = xmlNode.ChildNodes[i];

				if (node.NodeType != XmlNodeType.Element)
					continue;

				if (IsStructureElementName(node.Name) == false)
					continue;

				string strThis;
				nRet = BuildOneLevelScript(node, 
					out strThis,
					out strError);
				if (nRet == -1)
					return -1;

				strResult += strTab + "// \r\n";
				strResult += strThis;
				strResult += strTab + "\r\n";
			}

			// end
			if (strEndScript != "") 
			{
				strResult += strTab + "// end\r\n";
				strResult += strTab + "public override void OnEnd() {\r\n";
				strResult += strEndScript;
				strResult += "\r\n";
				strResult += strTab + "}";
				strResult += strTab + "\r\n";

			}

			strResult += "\r\n" + strTab + "} // end of class " + strClassName + "\r\n";


			return 0;
		}

		static string GetClassName(XmlNode node)
		{
			XmlNode parent = node.ParentNode;

			if (parent == node.OwnerDocument)
				return "__" + node.Name;

			if (parent == null)
				return "__" + node.Name;

			for(int i=0;i<parent.ChildNodes.Count; i++)
			{
				if (parent.ChildNodes[i] == node) 
				{
					if (node.Name == parent.Name)
						return "__" + node.Name + node.Name + Convert.ToString(i);

					return "__" + node.Name + Convert.ToString(i);
				}
			}

			return "__" + node.Name;
		}

		static string GetTabString(XmlNode node)
		{
			string strResult = "";
			for(int i=0;node!=null;i++)
			{
				strResult += "    ";
				node = node.ParentNode;
			}

			return strResult;
		}

		static bool IsStructureElementName(string strName)
		{
			if (strName == "filter"
				|| strName == "record"
				|| strName == "field"
				|| strName == "subfield"
				|| strName == "group"
				|| strName == "char")
				return true;
			return false;
		}

        static string GetFirstChildInnerText(XmlNode node)
        {
            if (node.ChildNodes.Count == 0)
                return "";
            return node.ChildNodes[0].InnerText;
        }

		int GetDefBeginEndScript(XmlNode parent,
			out string strDefinitionScript,
			out string strBeginScript,
			out string strEndScript,
			out string strError)
		{
			strError = "";
			strBeginScript = "";
			strEndScript = "";
			strDefinitionScript = "";


			for(int i =0;i<parent.ChildNodes.Count; i++)
			{
				XmlNode node = parent.ChildNodes[i];

				if (node.Name == "def"
					|| node.Name == "begin"
					|| node.Name == "end"
					) 
				{
                    /*
					if (node.ChildNodes.Count != 1) 
					{
						strError = "<" + node.Name + ">Ԫ����Ӧ���ж���ֻ��һ��CDATA�ڵ�...";
						return -1;
					}

					if (node.ChildNodes[0].NodeType!= XmlNodeType.CDATA)
					{
						strError = "<" + node.Name + ">Ԫ����Ӧ���ж���ֻ��һ��CDATA�ڵ�...";
						return -1;
					}
                     */

				}
				else 
				{
                    if (node.NodeType == XmlNodeType.Text || 
                        node.NodeType == XmlNodeType.CDATA)
                        strBeginScript += node.InnerText;
					continue;
				}

				if (node.Name == "def") 
				{

                    // strDefinitionScript += node.ChildNodes[0].InnerText;
                    strDefinitionScript += GetFirstChildInnerText(node);
				}
				if (node.Name == "begin") 
				{
                    // strBeginScript += node.ChildNodes[0].InnerText;
                    strBeginScript += GetFirstChildInnerText(node);
				}
				if (node.Name == "end") 
				{
                    // strEndScript += node.ChildNodes[0].InnerText;
                    strEndScript += GetFirstChildInnerText(node);
				}
			}
			return 0;
		}

		int GetUsingScript(XmlNode parent,
			out string strUsingScript,
			out string strError)
		{
			strError = "";
			strUsingScript = "";

			for(int i = 0;i<parent.ChildNodes.Count; i++)
			{
				XmlNode node = parent.ChildNodes[i];

				if (node.Name == "using") 
				{
					if (node.ChildNodes.Count != 1) 
					{
						strError = "<using>Ԫ����Ӧ���ж���ֻ��һ��CDATA�ڵ�...";
						return -1;
					}

					if (node.ChildNodes[0].NodeType!= XmlNodeType.CDATA)
					{
						strError = "<using>Ԫ����Ӧ���ж���ֻ��һ��CDATA�ڵ�...";
						return -1;
					}

					strUsingScript += node.ChildNodes[0].Value + "\r\n";
				}
			}
			return 0;
		}

		// �õ�һ������type��Ϣ
		int FillOneLevelType(XmlNode xmlNode,
			out string strError)
		{
			strError = "";

			Debug.Assert(assembly != null, "����FillOneLevelType()��ǰ�������ȸ�assembly��ֵ");

			// 
			HashFilterItem item = (HashFilterItem)this.NodeTable[xmlNode];
            if (item == null)
            {
                Debug.Assert(false, "xml�ڵ�" + xmlNode.OuterXml + "û����NodeTable�д�����Ӧ������");
                return -1;
            }

			if (item.FunctionName == "")
			{
				Debug.Assert(false, "xml�ڵ�"+ xmlNode.OuterXml + "����Ӧ��FilterItem��FunctionNameΪ���ַ�����������");
				return -1;
			}

			XmlNode parentNode = xmlNode.ParentNode;
			HashFilterItem parentItem = (HashFilterItem)this.NodeTable[parentNode];
			Type parentType = null;
			if (parentItem != null)
			{
				parentType = parentItem.FunctionType;
			}

			if (parentType != null) 
			{
				item.FunctionType = parentType.GetNestedType(
					item.FunctionName);
			}
			else 
			{

				// �õ�Assembly��Batch������Type
				item.FunctionType = assembly.GetType(
					item.FunctionName,
					false,	//   bool throwOnError,
					false	//bool ignoreCase
					);
			}
			if (item.FunctionType == null) 
			{
				Debug.Assert(false, "xml�ڵ�"+ xmlNode.OuterXml + " Ӧ��fltx.cs�д��ڶ�Ӧ��" + item.FunctionName);
				strError =  "xml�ڵ�"+ xmlNode.OuterXml + " Ӧ��fltx.cs�д��ڶ�Ӧ��" + item.FunctionName;
				return -1;
			}

			if (item.FunctionType.IsClass == false) 
			{
				strError = "�ű��У�[" +item.FunctionName+ "]Ϊϵͳ�����֣��û����벻��ʹ�á�";
				return -1;
			}
			

			for(int i =0;i<xmlNode.ChildNodes.Count; i++)
			{
				XmlNode node = xmlNode.ChildNodes[i];

				if (node.NodeType != XmlNodeType.Element)
					continue;

				if (IsStructureElementName(node.Name) == false)
					continue;

				int nRet = FillOneLevelType(node, 
					out strError);
				if (nRet == -1)
					return -1;

			}

			return 0;
		}

		// ƥ���ֶ���/���ֶ���
		// pamameters:
		//		strName	����
		//		strMatchCase	Ҫƥ���Ҫ��
		// return:
		//		-1	error
		//		0	not match
		//		1	match
		public static int MatchName(string strName,
			string strMatchCase)
		{
			if (strMatchCase == "")	// ���strMatchCaseΪ�գ���ʾ����ʲô���ֶ�ƥ��
				return 1;

			// Regular expression
			if (strMatchCase.Length >= 1 
				&& strMatchCase[0] == '@') 
			{
				if (StringUtil.RegexCompare(strMatchCase.Substring(1),
					RegexOptions.None,
					strName) == true)
					return 1;
				return 0;
			}
			else // ԭ����*ģʽ
			{
				if (CmpName(strName, strMatchCase) == 0)
					return 1;
				return 0;
			}
		}

        // 2013/1/7
        // t�ĳ��ȿ�����s��������
        public static int CmpName(string s, string t)
        {
            if (s.Length == t.Length)
                return CmpOneName(s, t);

            if ((t.Length % s.Length) != 0)
            {
                throw new Exception("t '"+t+"'�ĳ��� "+t.Length.ToString()+" Ӧ��Ϊs '"+s+"' �ĳ��� "+s.Length.ToString()+"  ��������");
            }
            int nCount = t.Length / s.Length;
            for (int i = 0; i < nCount; i++)
            {
                int nRet = CmpOneName(s, t.Substring(i * s.Length, s.Length));
                if (nRet == 0)
                    return 0;
            }

            return 1;
        }

        // ��ͨ����ıȽ�
        public static int CmpOneName(string s,
            string t)
        {
            int len = Math.Min(s.Length, t.Length);
            for (int i = 0; i < len; i++)
            {
                if (s[i] == '*' || t[i] == '*')
                    continue;
                if (s[i] != t[i])
                    return (s[i] - t[i]);
            }
            if (s.Length > t.Length)
                return 1;
            if (s.Length < t.Length)
                return -1;
            return 0;
        }

		// ��hash�����ҵ�xml�ڵ��Ӧ�Ĵ���Type
		FilterItem NewFilterItem(
			object objParam,
			XmlNode node,
			out string strError)
		{
			strError = "";

			Debug.Assert(node != null, "node��������Ϊnull");

			HashFilterItem itemNode = (HashFilterItem)NodeTable[node];

			if (itemNode == null) 
			{
				Debug.Assert(false, "NodeTable��ȱ������");
				return null;
			}

			Debug.Assert(node == itemNode.xmlNode, "item��ԱxmlNode����ȷ");

			Type entryClassType = itemNode.FunctionType;

			if (entryClassType == null) 
			{
				Debug.Assert(false, itemNode.FunctionName + "û��Ԥ�����Type");
				return null;
			}
			FilterItem itemHost = (FilterItem)entryClassType.InvokeMember(null, 
				BindingFlags.DeclaredOnly | 
				BindingFlags.Public | BindingFlags.NonPublic | 
				BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
				null);

			itemHost.Param = objParam;
			itemHost.Document = this;
            try
            {
                itemHost.PreInitial();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

			return itemHost;
		}


		// ����һ����¼��Ӧ��һ��<record>����
		// container������ȫ����Ϊnull�����ʾ<record>Ϊ��Ԫ��
		// return:
		//		-1	����
		//		0	��������
		int DoSingleItem(
			object objParam,
			XmlNode node,
			int nIndex,
			FilterItem container,
			string strData,
			string strNextName,
			out BreakType breakType,
			out string strError)
		{
			strError = "";
			breakType = BreakType.None;

			/*
			Debug.Assert(node != null, "node��������Ϊnull");

			HashFilterItem itemNode = (HashFilterItem)NodeTable[node];

			if (itemNode == null) 
			{
				Debug.Assert(false, "NodeTable��ȱ������");
				return -1;
			}

			Debug.Assert(node == itemNode.xmlNode, "item��ԱxmlNode����ȷ");

			Type entryClassType = itemNode.FunctionType;

			if (entryClassType == null) 
			{
				Debug.Assert(false, itemNode.FunctionName + "û��Ԥ�����Type");
				return -1;
			}

			// ��fltx.cs�����е�Batch�����new��������
			// newһ��Batch��������
			FilterItem itemHost = (FilterItem)entryClassType.InvokeMember(null, 
				BindingFlags.DeclaredOnly | 
				BindingFlags.Public | BindingFlags.NonPublic | 
				BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
				null);
			*/
			// ����һ����FilterItem����
			FilterItem itemHost = NewFilterItem(
				objParam,
				node,
				out strError);
			if (itemHost == null)
				return -1;

			itemHost.Data = strData;
			itemHost.Index = nIndex;
			itemHost.Container = container;
			// itemHost.FilterRoot = container != null ? container : itemHost;
			itemHost.FilterRoot = GetRootFilterItem(itemHost);

			Debug.Assert(itemHost.FilterRoot != null, "itemHost.FilterRoot��Ӧ��==null");

			if (node.Name == "record") 
			{
                itemHost.NodeType = NodeType.Record;
				itemHost.Data = strData;
				itemHost.Name = "";
				itemHost.Content = strData;
			}
			else if (node.Name == "field")
			{
                itemHost.NodeType = NodeType.Field;
                itemHost.Data = strData;
				if (strData.Length < 3) 
				{
					strError = "�ֶ�ȫ�����ݳ��Ȳ���3�ַ�";
					goto ERROR1;
				}
				itemHost.Name = strData.Substring(0, 3);	// ����Ҫ����ñ������ģ�׼��ͷ����������⡰�ֶΡ�ʱ��Ҫ����'hdr'2�ַ�������ǰ��
				// control field  001-009û�����ֶ�
				if (FilterItem.IsControlFieldName(itemHost.Name) == true)
				{
					itemHost.Indicator = "";
					itemHost.Content = strData.Substring(3);
				}
				else 
				{
                    if (strData.Length >= 5)
                    {
                        itemHost.Indicator = strData.Substring(3, 2);
                        itemHost.Content = strData.Substring(5);
                    }
                    else
                    {
                        // 2006/11/24
                        itemHost.Indicator = "";
                        itemHost.Content = "";
                    }

				}
			}
			else if (node.Name == "group") 
			{
                itemHost.NodeType = NodeType.Group;
                itemHost.Data = strData;
				itemHost.Name = "";
				itemHost.Content = strData;
			}
			else if (node.Name == "subfield")
			{
                itemHost.NodeType = NodeType.Subfield;
                itemHost.Data = strData;
				if (strData.Length < 1) 
				{
					strError = "���ֶ�ȫ�����ݳ��Ȳ���1�ַ�";
					goto ERROR1;
				}
				itemHost.Name = strData.Substring(0, 1);
				itemHost.Content = strData.Substring(1);
			}

			itemHost.SetDupCount();
			itemHost.NextName = strNextName;
			if (itemHost.Container != null) 
			{
				itemHost.PrevName = itemHost.Container.LastChildName;	// �����ϴ�������

				// ��һ���е���ࡣ��Ϊ���������غ�, �������������޸�LastChildName������
				itemHost.Container.LastChildName = itemHost.Name;	// ������ε�
			}

			itemHost.OnBegin();

			// ����������break�������
			if (CheckBreakException == true
				&& node.Name == "subfield"
				&& (itemHost.Break == BreakType.SkipCaseEnd
				|| itemHost.Break == BreakType.SkipCase) )
			{
				throw(new Exception("<subfield>Ԫ����script��������Break = ???�ı�ṹƥ���������κ�����..."));
			}


			if (itemHost.Break == BreakType.SkipCaseEnd)
				goto SKIP1;	// ����OnEnd()
			if (itemHost.Break == BreakType.SkipCase)
				goto SKIP;	// ����OnBegin���ֵ�case������Ҫ��OnEnd()


			// int i;
			int nRet;
			// XmlNode child = null;
			BreakType thisBreak = BreakType.None;

			// <record>ϣ���¼���<field>
			if (node.Name == "record") 
			{
				// �и��¼Ϊ�����ֶΣ�ƥ��case
				for(int r=0;;r++) 
				{
					string strField;
					string strNextFieldName;

					// �Ӽ�¼�еõ�һ���ֶ�
					// parameters:
					//		strMARC		MARC��¼
					//		strFieldName	�ֶ��������==null����ʾ�����ֶ�
					//		nIndex		ͬ���ֶ��еĵڼ�������0��ʼ����(0��ʾͷ����)
					//		strField	[out]����ֶΡ������ֶ�������Ҫ���ֶ�ָʾ�����ֶ����ݡ��������ֶν�������
					//					ע��ͷ��������һ���ֶη��أ�strField�в������ֶ�����һ��������ͷ��������
					// return:
					//		-1	error
					//		0	not found
					//		1	found
					nRet = MarcDocument.GetField(strData,
						null,
						r,
						out strField,
						out strNextFieldName);
					if (nRet == -1)
					{
                        // 2009/11/1
                        if (String.IsNullOrEmpty(strData) == true)
                            break;

                        strError = "DoSingleItem() GetField() error";
						return -1;
					}
					if (nRet == 0)
						break;

					if (strField.Length < 3)
						goto SKIP;


					string strFieldName = "";
					if (r != 0)
						strFieldName = strField.Substring(0,3);
					else 
					{
						strFieldName = "hdr";
						strField = strFieldName + strField;
					}
                    // ***
                    itemHost.IncChildDupCount(strFieldName);

					// for(i=0;i<node.ChildNodes.Count;i++) 
                    foreach(XmlNode child in node.ChildNodes)
					{
						// child = node.ChildNodes[i];

						if (child.NodeType != XmlNodeType.Element)
							continue;

						if (child.Name != "field")
							continue;

						// ƥ���ֶ���
						nRet = MatchName( strFieldName, DomUtil.GetAttr(child, "name"));
						if (nRet == 1) 
						{
							nRet = DoSingleItem(
								objParam,
								child,
								r,
								itemHost,
								strField,
								strNextFieldName,
								out thisBreak,
								out strError);
							if (nRet == -1)
								return -1;

							if (itemHost.Break != BreakType.None)
								break;
						}


					} // end of for

					itemHost.LastChildName = strFieldName;	// ������ε�
					if (itemHost.Break != BreakType.None)
						goto SKIP;

				}

			}
			else if (node.Name == "field")
			{

				// ���¼�Ϊsubfield
				string strFirstChildName = GetFirstChildElementType(node);
				// field�µ�subfield
				if (strFirstChildName == "subfield") 
				{
					// �и��¼Ϊ�������ֶΣ�ƥ��case
					for(int s=0;;s++) 
					{
						string strSubfield;
						string strNextSubfieldName;

						// ���ֶλ����еõ�һ�����ֶ�
						// parameters:
						//		strText		�ֶ����ݣ��������ֶ������ݡ�
						//		textType	��ʾstrText�а��������ֶ����ݻ��������ݡ�
						//		strSubfieldName	���ֶ��������==null����ʾ�������ֶ�
						//					��ʽΪ'a'�����ġ�
						//		nIndex			ͬ�����ֶ��еĵڼ�������0��ʼ���㡣
						//		strSubfield		������ֶΡ����ֶ���(1�ַ�)�����ֶ����ݡ�
						//		strNextSubfieldName	��һ�����ֶε����֣�һ���ַ�
						// return:
						//		-1	error
						//		0	not found
						//		1	found
						nRet = MarcDocument.GetSubfield(strData,
							ItemType.Field,
							null,
							s,
							out strSubfield,
							out strNextSubfieldName);
						if (nRet == -1)
						{
							strError = "GetSubfield() error";
							return -1;
						}
						if (nRet == 0)
							break;

						if (strSubfield.Length < 1)
							goto SKIP;

						string strSubfieldName = strSubfield.Substring(0,1);

                        // ***
                        itemHost.IncChildDupCount(strSubfieldName);

						// for(i=0;i<node.ChildNodes.Count;i++) 
                        foreach(XmlNode child in node.ChildNodes)
						{
							// child = node.ChildNodes[i];

							if (child.NodeType != XmlNodeType.Element)
								continue;

							if (child.Name != "subfield")
								continue;

							// ƥ�����ֶ���
							nRet = MatchName( strSubfieldName,  DomUtil.GetAttr(child, "name"));
							if (nRet == 1) 
							{
								nRet = DoSingleItem(
									objParam,
									child, 
									s,
									itemHost,
									strSubfield,
									strNextSubfieldName,
									out thisBreak,
									out strError);
								if (nRet == -1)
									return -1;
								if (itemHost.Break != BreakType.None)
									break;

							}

						} // end of for

						itemHost.LastChildName = strSubfieldName;	// ������ε�
						if (itemHost.Break != BreakType.None)
							goto SKIP;

					}
				}
				// field��Ƕ�׵�field
				if (strFirstChildName == "field") 
				{
					// �и��ַ���Ϊ�����ֶΣ�ƥ��case
					for(int r=0;;r++) 
					{
						string strField;
						string strNextFieldName;

						// �Ӽ�¼�еõ�һ���ֶ�
						// parameters:
						//		strMARC		MARC��¼
						//		strFieldName	�ֶ��������==null����ʾ�����ֶ�
						//		nIndex		ͬ���ֶ��еĵڼ�������0��ʼ����(0��ʾͷ����)
						//		strField	[out]����ֶΡ������ֶ�������Ҫ���ֶ�ָʾ�����ֶ����ݡ��������ֶν�������
						//					ע��ͷ��������һ���ֶη��أ�strField�в������ֶ�����һ��������ͷ��������
						// return:
						//		-1	error
						//		0	not found
						//		1	found
						nRet = MarcDocument.GetNestedField(strData,
							null,
							r,
							out strField,
							out strNextFieldName);
						if (nRet == -1)
						{
							strError = "GetNestedField() error";
							return -1;
						}
						if (nRet == 0)
							break;

						if (strField.Length < 3)
							goto SKIP;


						string strFieldName = "";
						strFieldName = strField.Substring(0,3);

                        // ***
                        itemHost.IncChildDupCount(strFieldName);

						// Ƕ���ֶβ�����ͷ����'hdr'�ֶ�����?

						//for(i=0;i<node.ChildNodes.Count;i++) 
                        foreach(XmlNode child in node.ChildNodes)
						{
							//child = node.ChildNodes[i];

							if (child.NodeType != XmlNodeType.Element)
								continue;

							if (child.Name != "field")
								continue;

							// ƥ���ֶ���
							nRet = MatchName( strFieldName, DomUtil.GetAttr(child, "name"));
							if (nRet == 1) 
							{
								nRet = DoSingleItem(
									objParam,
									child,
									r,
									itemHost,
									strField,
									strNextFieldName,
									out thisBreak,
									out strError);
								if (nRet == -1)
									return -1;
								if (itemHost.Break != BreakType.None)
									break;
							}


						} // end of for

						itemHost.LastChildName = strFieldName;	// ������ε�
						if (itemHost.Break != BreakType.None)
							goto SKIP;

					}

				}

				// field �µ�group
				else if (strFirstChildName == "group") 
				{
					// �и��¼Ϊ�������ֶΣ�ƥ��case
					for(int g=0;;g++) 
					{
						string strGroup;

						// ���ֶ��еõ����ֶ���
						// parameters:
						//		strGroup	[out]�����
						// return:
						//		-1	error
						//		0	not found
						//		1	found
						nRet = MarcDocument.GetGroup(strData,
							g,
							out strGroup);
						if (nRet == -1)
						{
							strError = "GetGroup() error";
							return -1;
						}
						if (nRet == 0)
							break;

						string strGroupName = Convert.ToString(g);

                        // ***
                        itemHost.IncChildDupCount(strGroupName);

						// for(i=0;i<node.ChildNodes.Count;i++) 
                        foreach(XmlNode child in node.ChildNodes)
						{
							// child = node.ChildNodes[i];

							if (child.NodeType != XmlNodeType.Element)
								continue;

							if (child.Name != "group")
								continue;

							// ƥ������
							nRet = MatchName( strGroupName,  DomUtil.GetAttr(child, "name"));
							if (true/*nRet == 1*/) 
							{
								nRet = DoSingleItem(
									objParam,
									child,
									g,
									itemHost,
									strGroup, 
									"",
									out thisBreak,
									out strError);
								if (nRet == -1)
									return -1;
								if (itemHost.Break != BreakType.None)
									break;

							}

						} // end of for

						itemHost.LastChildName = "";	// ������ε�
						if (itemHost.Break != BreakType.None)
							goto SKIP;

					}
				}

			}
			else if (node.Name == "group")
			{
				// ���¼�Ϊsubfield
				string strFirstChildName = GetFirstChildElementType(node);
				if (strFirstChildName != "subfield") 
				{
					strError = ".fltx��<group>�¼�����Ϊ<subfield>Ԫ��";
					return -1;
				}


				// �и��¼Ϊ�������ֶΣ�ƥ��case
				for(int s=0;;s++) 
				{
					string strSubfield;
					string strNextSubfieldName;

					// ���ֶλ����еõ�һ�����ֶ�
					// parameters:
					//		strText		�ֶ����ݣ��������ֶ������ݡ�
					//		textType	��ʾstrText�а��������ֶ����ݻ��������ݡ�
					//		strSubfieldName	���ֶ��������==null����ʾ�������ֶ�
					//					��ʽΪ'a'�����ġ�
					//		nIndex			ͬ�����ֶ��еĵڼ�������0��ʼ���㡣
					//		strSubfield		������ֶΡ����ֶ���(1�ַ�)�����ֶ����ݡ�
					//		strNextSubfieldName	��һ�����ֶε����֣�һ���ַ�
					// return:
					//		-1	error
					//		0	not found
					//		1	found
					nRet = MarcDocument.GetSubfield(strData,
						ItemType.Group,
						null,
						s,
						out strSubfield,
						out strNextSubfieldName);
					if (nRet == -1)
					{
						strError = "GetSubfield() error";
						return -1;
					}
					if (nRet == 0)
						break;

					if (strSubfield.Length < 1)
						goto SKIP;

					string strSubfieldName = strSubfield.Substring(0,1);
                    // ***
                    itemHost.IncChildDupCount(strSubfieldName);

					// for(i=0;i<node.ChildNodes.Count;i++) 
                    foreach (XmlNode child in node.ChildNodes)
                    {
						// child = node.ChildNodes[i];

						if (child.NodeType != XmlNodeType.Element)
							continue;

						if (child.Name != "subfield")
							continue;

						// ƥ�����ֶ���
						nRet = MatchName( strSubfieldName,  DomUtil.GetAttr(child, "name"));
						if (nRet == 1) 
						{
							nRet = DoSingleItem(
								objParam,
								child,
								s,
								itemHost,
								strSubfield,
								strNextSubfieldName,
								out thisBreak,
								out strError);
							if (nRet == -1)
								return -1;
							if (itemHost.Break != BreakType.None)
								break;

						}

					} // end of for

					itemHost.LastChildName = strSubfieldName;	// ������ε�
					if (itemHost.Break != BreakType.None)
						goto SKIP;


				}

			}			
			else if (node.Name == "subfield")
			{
				// ��ʱû��ʲô����

			}

			SKIP:

				if (itemHost.Break != BreakType.SkipCaseEnd) 
				{
					itemHost.OnEnd();
				}

           
			SKIP1:

				/*
				if (itemHost.Break != BreakType.None)
					return 1;
				*/

				breakType = itemHost.Break;

			return 0;

			ERROR1:
				return -1;
		}

		// �õ������е�һ����def/begin/end�Ķ��ӵ�Ԫ����
		string GetFirstChildElementType(XmlNode parent)
		{
			for(int i=0;i<parent.ChildNodes.Count;i++)
			{
				XmlNode node = parent.ChildNodes[i];
				if (node.NodeType != XmlNodeType.Element)
					continue;
				if (IsStructureElementName(node.Name) == false)
					continue;

				return node.Name;
			}

			return "";
		}

        // ���.fltx��<ref>������Ĳο���
        public string[] GetRefs()
        {
            XmlNodeList nodes = this.dom.SelectNodes("//ref");
            List<string> refs = new List<string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                string strText = DomUtil.GetNodeText(nodes[i]);
                if (strText == "")
                    continue;
                refs.Add(strText);
            }
            string[] results = new string[refs.Count];

            for (int i = 0; i < refs.Count; i++)
            {
                results[i] = refs[i];
            }

            return results;
        }


        // ����Assembly
        // parameters:
        //	strCode:	�ű�����
        //	refs:	���ӵ��ⲿassembly
        // strResult:������Ϣ
        // objDb:���ݿ�����ڳ����getErrorInfo�õ�
        // ����ֵ:�����õ�Assembly
        public Assembly CreateAssembly(string strCode,
			string[] refs,
			out string strErrorInfo)
		{
			// System.Reflection.Assembly compiledAssembly = null;
			strErrorInfo = "";
 
			// CompilerParameters����
			System.CodeDom.Compiler.CompilerParameters compilerParams;
			compilerParams = new CompilerParameters();
			compilerParams.GenerateInMemory = true; //Assembly is created in memory
			compilerParams.TreatWarningsAsErrors = false;
			compilerParams.WarningLevel = 4;
 
			compilerParams.ReferencedAssemblies.AddRange(refs);
 
			CSharpCodeProvider provider;

			// System.CodeDom.Compiler.ICodeCompiler compiler;
			System.CodeDom.Compiler.CompilerResults results = null;
			try 
			{
                /*
				provider = new CSharpCodeProvider();
				compiler = provider.CreateCompiler();
				results = compiler.CompileAssemblyFromSource(
					compilerParams, 
					strCode);
                 */
                provider = new CSharpCodeProvider();
                results = provider.CompileAssemblyFromSource(
                    compilerParams,
                    strCode);

			}
			catch (Exception ex) 
			{
				strErrorInfo = "���� " + ex.Message;
				return null;
			}
 
			if (results.Errors.Count == 0) 
			{
			}
			else 
			{
				strErrorInfo = "�������������:" + Convert.ToString(results.Errors.Count) + "\r\n";
				strErrorInfo += getErrorInfo(results.Errors);

				return null;
			}
 
			return results.CompiledAssembly;
		}

		// ���������Ϣ�ַ���
		public string getErrorInfo(CompilerErrorCollection errors)
		{
			string strResult = "";
 
			if (errors == null)
			{
				strResult = "error����Ϊnull";
				return strResult;
			}
   
 
			foreach(CompilerError oneError in errors)
			{
				strResult += "(" + Convert.ToString(oneError.Line) + "," + Convert.ToString(oneError.Column) + ")\r\n";
				strResult += (oneError.IsWarning) ? "warning " : "error ";
				strResult += oneError.ErrorNumber + " ";
				strResult += ":" + oneError.ErrorText + "\r\n";
			}
			return strResult;
		}

		#endregion

	}

	public enum ItemType 
	{
		Filter = 0,
		Record = 1,
		Field = 2,
		Subfield = 3,
		Group = 4,
		Char = 5,
		/*
		Begin = 6,
		End = 7,
		Def = 8,
		*/
	}

	// hash table�к�XmlNode��Ӧ��Item
	public class HashFilterItem
	{
		public XmlNode xmlNode = null;
		public ItemType ItemType;
		public string Name = "";
		public string FunctionName = "";
		public Type FunctionType = null;

	}

	public enum BreakType
	{
		None = 0,	// ��break
		SkipCase = 1,	// ����case���֣����ǲ�����end����
		SkipCaseEnd = 2,	// ����case��end����
		SkipDataLoop = 3,	// ������������ݴ���ѭ�� ��
	}

    public enum NodeType
    {
        None = 0,
        Record = 1,
        Field = 2,
        Group = 3,
        Subfield = 4,
    }

	// 
    /// <summary>
    /// Script����Ļ���
    /// </summary>
	public class FilterItem
	{
        /// <summary>
        /// �ڵ�����
        /// </summary>
        public NodeType NodeType = NodeType.None;

        /// <summary>
        /// �ڵ��� (�ֶ�/���ֶ�...)����һ����� Name + Indicator + Content = Data
        /// </summary>
		public string Name = "";	// ����(�ֶ�/���ֶ�...)��

        /// <summary>
        /// ָʾ����һ����� Name + Indicator + Content = Data
        /// </summary>
		public string Indicator = "";

        /// <summary>
        /// ���ġ�һ����� Name + Indicator + Content = Data
        /// </summary>
		public string Content = "";	// һ����� Name + Indicator + Content = Data

        /// <summary>
        /// �ϼ�����
        /// </summary>
		public FilterItem Container = null;

        /// <summary>
        /// ������
        /// </summary>
		public FilterItem FilterRoot = null;

		//
        /// <summary>
        /// ������������ݡ�һ����� Name + Indicator + Content = Data
        /// </summary>
		public string	Data = "";

        /// <summary>
        /// �жϱ�־�������Ƿ�����ͬ�������case/end
        /// </summary>
		public BreakType Break = BreakType.None;	// �Ƿ�����ͬ�������case/end

        /// <summary>
        /// �±�
        /// </summary>
		public int		Index = -1;	// ȱʡֵ-1��Ϊ�˱�¶����

        /// <summary>
        /// Document ����
        /// </summary>
		public FilterDocument Document = null;	// Document���󡣿��������ﱣ����Ҫ��record֮��־õ�ֵ

        /// <summary>
        /// ����Ĵ������ݵ�ָ��
        /// </summary>
		public object	Param;	// ����Ĵ������ݵ�ָ��

		// ͬ���ṹ���໥��ϵ������ʩ
        /// <summary>
        /// ͬ��ǰһ������
        /// </summary>
		public string	PrevName = "";	// ǰһ��

        /// <summary>
        /// ͬ����һ������
        /// </summary>
		public string	NextName = "";	// ��һ��

        /// <summary>
        /// ͬ���кͱ�����ͬ���ĸ���
        /// </summary>
		public int		DupCount = 0;	// ͬ���ظ�����

		private Hashtable ChildDupTable = new Hashtable();	// ���Ӷ����ظ��������
		internal string LastChildName = "";	// ����ù������һ���¼��ṹ������

        /// <summary>
        /// ���캯��
        /// </summary>
		public FilterItem()
		{
		}

        // ���õ�ǰ����DupCount����
        // Container��Name�����ʼ��
        internal void IncChildDupCount(string strChildName)
        {
            if (this.ChildDupTable.Contains(strChildName) == false)
            {
                this.ChildDupTable.Add(strChildName, (object)1);
            }
            else
            {
                int nOldDupCount = (int)this.ChildDupTable[strChildName] + 1;

                this.ChildDupTable[strChildName] = (object)nOldDupCount;
            }
        }

        internal void SetDupCount()
        {
            if (Container == null)
                return;

            // �Ӹ������ҵ�ChildDupTable
            if (Container.ChildDupTable.Contains(this.Name) == false)
            {
                Debug.Assert(false, "");
                this.DupCount = 1;
            }
            else
            {
                this.DupCount = (int)Container.ChildDupTable[this.Name];
            }
        }


#if NO
		// ���õ�ǰ����DupCount����
		// Container��Name�����ʼ��
		internal void SetDupCount()
		{
			if (Container == null)
				return;

			// �Ӹ������ҵ�ChildDupTable
			if (Container.ChildDupTable.Contains(this.Name) == false)
			{
				Container.ChildDupTable.Add(this.Name, (object)1);
				this.DupCount = 1;
			}
			else 
			{
				this.DupCount = (int)Container.ChildDupTable[this.Name] + 1;

				Container.ChildDupTable[this.Name] = (object)this.DupCount;
			}
		}
#endif

        /// <summary>
        /// Begin �׶�
        /// </summary>
		public virtual void OnBegin() 
		{
		}

        /// <summary>
        /// End �׶�
        /// </summary>
		public virtual void OnEnd() 
		{
		}

		// 
        /// <summary>
        /// ��һ���ֶ����Ƿ��ǿ����ֶΡ���ν�����ֶ�û��ָʾ������
        /// </summary>
        /// <param name="strFieldName">�ֶ���</param>
        /// <returns>�Ƿ�Ϊ�����ֶ�</returns>
		public static bool IsControlFieldName(string strFieldName)
		{
			if (String.Compare(strFieldName,"hdr",true) == 0)
				return true;

			if (
				(
				String.Compare(strFieldName, "001") >= 0
				&& String.Compare(strFieldName, "009") <= 0
				)

				|| String.Compare(strFieldName, "-01") == 0
				)
				return true;

			return false;
		}

        /// <summary>
        /// �������Ƿ�Ϊ�����ֶΡ�(������������ֶζ��󣬷��� false)
        /// </summary>
		public bool IsControlField
		{
			get 
			{
				if (this.Name.Length != 3)
					return false;
				return FilterItem.IsControlFieldName(this.Name);
			}
		}

        /// <summary>
        /// Initial �׶�ǰ��Ԥ����
        /// </summary>
		virtual public void PreInitial()
		{

		}

        /// <summary>
        /// ���� ID ��õ�ǰ�����µ��ַ���
        /// ���û�о�ȷƥ������ԣ���ģ��ƥ�䣬�򷵻ص�һ�����Ե�
        /// �����id�����ڣ�����null
        /// </summary>
        /// <param name="strID">ID</param>
        /// <returns>�ַ���</returns>
        public string GetString(string strID)
        {
            return this.Document.GetString(strID);
        }

        /// <summary>
        /// ���Դ���
        /// </summary>
        public string Lang
        {
            get
            {
                return Thread.CurrentThread.CurrentUICulture.Name;
            }
        }

        /// <summary>
        /// ���� ID ��õ�ǰ�����µ��ַ����������»᷵�� ID ����
        /// </summary>
        /// <param name="strID">ID</param>
        /// <returns>�ַ���</returns>
        public string GetStringSafe(string strID)
        {
            return this.Document.GetStringSafe(strID);
        }

        // �����ư汾
        /// <summary>
        /// ͬ GetString()
        /// </summary>
        /// <param name="strID">ID</param>
        /// <returns>�ַ���</returns>
        public string S(string strID)
        {
            return this.Document.GetString(strID);
        }

        // �����ư汾
        /// <summary>
        /// ͬ GetStringSafe()
        /// </summary>
        /// <param name="strID">ID</param>
        /// <returns>�ַ���</returns>
        public string SS(string strID)
        {
            return this.Document.GetStringSafe(strID);
        }

        /// <summary>
        /// ��ÿ����ڶ�λ�������λ���ַ���
        /// </summary>
        public string LocationString
        {
            get
            {
                return this.GetLocationString();
            }
        }

        // 
        /// <summary>
        /// ����ض����ֵĵ�һ�����ֶ����ġ������ǰ�������ֶζ����򷵻� null
        /// </summary>
        /// <param name="strSubfieldName">���ֶ���</param>
        /// <returns>���ֶ������ַ���</returns>
        public string GetFirstSubfieldValue(string strSubfieldName)
        {
            if (this.NodeType != MarcDom.NodeType.Field && this.NodeType != MarcDom.NodeType.Group)
                return null;

            string strSubfield = "";
            string strNextSubfieldName = "";
            // return:
            //		-1	error
            //		0	not found
            //		1	found
            int nRet = MarcDocument.GetSubfield(this.Content,
                this.NodeType == MarcDom.NodeType.Field ? ItemType.Field : ItemType.Group,
                strSubfieldName,
                0,
                out strSubfield,
                out strNextSubfieldName);
            if (strSubfield.Length < 1)
                return "";
            return strSubfield.Substring(1);
        }

        /// <summary>
        /// ��ÿ����ڶ�λ�������λ���ַ���
        /// </summary>
        /// <param name="nCharPos">�ڱ������ڵ�ƫ����</param>
        /// <returns>λ���ַ���</returns>
        public string GetLocationString(int nCharPos = 0)
        {
            string strResult = "";
            // field
            if (this.Name.Length == 3)
            {
                strResult = this.Name;
                if (this.DupCount != 1)
                    strResult += "#" + this.DupCount.ToString();
                if (nCharPos != 0)
                    strResult += ",," + nCharPos.ToString();
                return strResult;
            }
            // subfield
            if (this.Name.Length == 1)
            {
                strResult = this.Container.GetLocationString() + "," + this.Name;
                if (this.DupCount != 1)
                    strResult += "#" + this.DupCount.ToString();
                if (nCharPos != 0)
                    strResult += "," + nCharPos.ToString();
                return strResult;
            }

            return "";
        }
	}
}
