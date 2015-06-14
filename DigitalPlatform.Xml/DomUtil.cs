using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace DigitalPlatform.Xml
{
	// DomUtil�����XML DOM��һЩ��չ���ܺ���
	public class DomUtil
	{
        public static XmlNode RenameNode(XmlNode node, 
            string namespaceURI, 
            string qualifiedName)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                XmlElement oldElement = (XmlElement)node;
                XmlElement newElement =
                node.OwnerDocument.CreateElement(qualifiedName, namespaceURI);

                while (oldElement.HasAttributes)
                {
                    newElement.SetAttributeNode(oldElement.RemoveAttributeNode(oldElement.Attributes[0]));
                }

                while (oldElement.HasChildNodes)
                {
                    newElement.AppendChild(oldElement.FirstChild);
                }

                if (oldElement.ParentNode != null)
                {
                    oldElement.ParentNode.ReplaceChild(newElement, oldElement);
                }

                return newElement;
            }
            else
            {
                return null;
            }
        }


        // 2010/12/18
        // ��һ��Ԫ�ص��¼��Ķ��<strElementName>Ԫ����, ��ȡ���Է��ϵ�XmlNode��InnerText
        // parameters:
        //      bReturnFirstNode    ����Ҳ���������Եģ��Ƿ񷵻ص�һ��<strElementName>
        public static string GetLangedNodeText(
            string strLang,
            XmlNode parent,
            string strElementName,
            bool bReturnFirstNode = true)
        {
            XmlNode node = GetLangedNode(
        strLang,
        parent,
        strElementName,
        bReturnFirstNode);
            if (node == null)
                return null;
            return node.InnerText;
        }

        // 2010/12/18
        // ��һ��Ԫ�ص��¼��Ķ��<strElementName>Ԫ����, ��ȡ���Է��ϵ�XmlNode
        // parameters:
        //      bReturnFirstNode    ����Ҳ���������Եģ��Ƿ񷵻ص�һ��<strElementName>
        public static XmlNode GetLangedNode(
            string strLang,
            XmlNode parent,
            string strElementName,
            bool bReturnFirstNode = true)
        {
            XmlNode node = null;

            if (String.IsNullOrEmpty(strLang) == true)
            {
                return parent.SelectSingleNode(strElementName);  // ��һ��strElementNameԪ��
            }
            else
            {
                node = parent.SelectSingleNode(strElementName + "[@lang='" + strLang + "']");

                if (node != null)
                    return node;
            }

            string strLangLeft = "";
            string strLangRight = "";

            SplitLang(strLang,
               out strLangLeft,
               out strLangRight);

            // ����<caption>Ԫ��
            XmlNodeList nodes = parent.SelectNodes(strElementName);

            for (int i = 0; i < nodes.Count; i++)
            {
                string strThisLang = DomUtil.GetAttr(nodes[i], "lang");

                string strThisLangLeft = "";
                string strThisLangRight = "";

                SplitLang(strThisLang,
                   out strThisLangLeft,
                   out strThisLangRight);

                // �ǲ������Ҷ�ƥ�������?������в��ǵ�һ�����ƥ���

                if (strThisLangLeft == strLangLeft)
                    return nodes[i];
            }

            if (bReturnFirstNode == true)
            {
                // ʵ�ڲ��У���ѡ��һ��<caption>������ֵ
                node = parent.SelectSingleNode(strElementName);
                if (node != null)
                    return node;
            }

            return null;    // not found
        }

        // ��һ��Ԫ�ص��¼��Ķ��<strElementName>Ԫ����, ��ȡ���Է��ϵ�XmlNode��InnerText
        // parameters:
        //      bReturnFirstNode    ����Ҳ���������Եģ��Ƿ񷵻ص�һ��<strElementName>
        public static string GetXmlLangedNodeText(
            string strLang,
            XmlNode parent,
            string strElementName,
            bool bReturnFirstNode = true)
        {
            XmlNode node = GetXmlLangedNode(
        strLang,
        parent,
        strElementName,
        bReturnFirstNode);
            if (node == null)
                return null;
            return node.InnerText;
        }

        // ��һ��Ԫ�ص��¼��Ķ��<strElementName>Ԫ����, ��ȡ���Է��ϵ�XmlNode
        // parameters:
        //      bReturnFirstNode    ����Ҳ���������Եģ��Ƿ񷵻ص�һ��<strElementName>
        public static XmlNode GetXmlLangedNode(
            string strLang,
            XmlNode parent,
            string strElementName,
            bool bReturnFirstNode = true)
        {
            XmlNode node = null;

            if (String.IsNullOrEmpty(strLang) == true)
            {
                return parent.SelectSingleNode(strElementName);  // ��һ��strElementNameԪ��
            }
            else
            {
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
                nsmgr.AddNamespace("xml", Ns.xml);

                node = parent.SelectSingleNode(strElementName + "[@xml:lang='" + strLang + "']", nsmgr);

                if (node != null)
                    return node;
            }

            string strLangLeft = "";
            string strLangRight = "";

            SplitLang(strLang,
               out strLangLeft,
               out strLangRight);

            // ����<strElementName>Ԫ��
            XmlNodeList nodes = parent.SelectNodes(strElementName);

            for (int i = 0; i < nodes.Count; i++)
            {
                string strThisLang = DomUtil.GetAttr(Ns.xml, nodes[i], "lang");

                string strThisLangLeft = "";
                string strThisLangRight = "";

                SplitLang(strThisLang,
                   out strThisLangLeft,
                   out strThisLangRight);

                // �ǲ������Ҷ�ƥ�������?������в��ǵ�һ�����ƥ���

                if (strThisLangLeft == strLangLeft)
                    return nodes[i];
            }

            if (bReturnFirstNode == true)
            {
                // ʵ�ڲ��У���ѡ��һ��<strElementName>������ֵ
                node = parent.SelectSingleNode(strElementName);
                if (node != null)
                    return node;
            }

            return null;    // not found
        }

        // ��һ��Ԫ�ص��¼�<caption>Ԫ����, ��ȡ���Է��ϵ�����ֵ
        // ��GetCaption()�����Ĳ��죬��������Ҳ���������Եģ��������ص�һ��<caption>
        public static string GetCaptionExt(string strLang,
            XmlNode parent)
        {
            XmlNode node = null;

            if (String.IsNullOrEmpty(strLang) == true)
            {
                node = parent.SelectSingleNode("caption");  // ��һ��captionԪ��
                if (node != null)
                    return node.InnerText;

                return null;
            }
            else
            {
                node = parent.SelectSingleNode("caption[@lang='" + strLang + "']");

                if (node != null)
                    return node.InnerText;
            }

            string strLangLeft = "";
            string strLangRight = "";

            SplitLang(strLang,
               out strLangLeft,
               out strLangRight);

            // ����<caption>Ԫ��
            XmlNodeList nodes = parent.SelectNodes("caption");

            for (int i = 0; i < nodes.Count; i++)
            {
                string strThisLang = DomUtil.GetAttr(nodes[i], "lang");

                string strThisLangLeft = "";
                string strThisLangRight = "";

                SplitLang(strThisLang,
                   out strThisLangLeft,
                   out strThisLangRight);

                // �ǲ������Ҷ�ƥ�������?������в��ǵ�һ�����ƥ���

                if (strThisLangLeft == strLangLeft)
                    return nodes[i].InnerText;
            }

            /*
            // ʵ�ڲ��У���ѡ��һ��<caption>������ֵ
            node = parent.SelectSingleNode("caption");
            if (node != null)
                return node.InnerText;
             * */

            return null;    // not found
        }

        // ��һ��Ԫ�ص��¼�<caption>Ԫ����, ��ȡ���Է��ϵ�����ֵ
        public static string GetCaption(string strLang,
            XmlNode parent)
        {
            XmlNode node = null;

            if (String.IsNullOrEmpty(strLang) == true)
            {
                node = parent.SelectSingleNode("caption");  // ��һ��captionԪ��
                if (node != null)
                    return node.InnerText;

                return null;
            }
            else
            {
                node = parent.SelectSingleNode("caption[@lang='" + strLang + "']");

                if (node != null)
                    return node.InnerText;
            }

            string strLangLeft = "";
            string strLangRight = "";

            SplitLang(strLang,
               out strLangLeft,
               out strLangRight);

            // ����<caption>Ԫ��
            XmlNodeList nodes = parent.SelectNodes("caption");

            for (int i = 0; i < nodes.Count; i++)
            {
                string strThisLang = DomUtil.GetAttr(nodes[i], "lang");

                string strThisLangLeft = "";
                string strThisLangRight = "";

                SplitLang(strThisLang,
                   out strThisLangLeft,
                   out strThisLangRight);

                // �ǲ������Ҷ�ƥ�������?������в��ǵ�һ�����ƥ���

                if (strThisLangLeft == strLangLeft)
                    return nodes[i].InnerText;
            }

            // ʵ�ڲ��У���ѡ��һ��<caption>������ֵ
            node = parent.SelectSingleNode("caption");
            if (node != null)
                return node.InnerText;

            return null;    // not found
        }

        public static void SplitLang(string strLang,
    out string strLangLeft,
    out string strLangRight)
        {
            strLangLeft = "";
            strLangRight = "";

            int nRet = strLang.IndexOf("-");
            if (nRet == -1)
                strLangLeft = strLang;
            else
            {
                strLangLeft = strLang.Substring(0, nRet);
                strLangRight = strLang.Substring(nRet + 1);
            }
        }

        // �ѱ�ʾ����ֵ���ַ�������Ϊ����ֵ
        // ע�⣬strValue����Ϊ�գ��������޷�����ȱʡֵ
        public static bool IsBooleanTrue(string strValue)
        {
            // 2008/6/4
            if (String.IsNullOrEmpty(strValue) == true)
                throw new Exception("DomUtil.IsBoolean() ���ܽ��ܿ��ַ�������");

            strValue = strValue.ToLower();  // 2008/6/4

            if (strValue == "yes" || strValue == "on"
                    || strValue == "1" || strValue == "true")
                return true;

            return false;
        }

        public static bool IsBooleanTrue(string strValue, bool bDefaultValue)
        {
            if (string.IsNullOrEmpty(strValue) == true)
                return bDefaultValue;
            return IsBooleanTrue(strValue);
        }

        // ��װ�汾
        public static bool GetBooleanParam(XmlNode node,
            string strParamName,
            bool bDefaultValue)
        {
            bool bValue = bDefaultValue;
            string strError = "";
            GetBooleanParam(node,
                strParamName,
                bDefaultValue,
                out bValue,
                out strError);
            return bValue;
        }

        // ��װ��İ汾���������Ȼ��Ԫ�ص� Node
        public static bool GetBooleanParam(
            XmlNode root,
            string strElementPath,
            string strParamName,
            bool bDefaultValue)
        {
            XmlNode node = root.SelectSingleNode(strElementPath);
            if (node == null)
                return bDefaultValue;
            return GetBooleanParam(node,
                strParamName,
                bDefaultValue);
        }

        // ���� bool ���͵Ĳ���
        // parameters:
        //      root    ��� XmlNode
        //      strElementPath  Ԫ��·��
        //      strParamName    ������
        //      bValue  Ҫ���õ�ֵ
        // return:
        //      �����Ƿ񴴽����µ�Ԫ��
        public static bool SetBooleanParam(
            XmlNode root,
            string strElementPath,
            string strParamName,
            bool bValue)
        {
            bool bCreateElement = false;
            XmlNode node = root.SelectSingleNode(strElementPath);
            if (node == null)
            {
                string[] aNodeName = strElementPath.Split(new Char[] { '/' });
                node = CreateNode(root, aNodeName);
                bCreateElement = true;
            }
            if (node == null)
            {
                throw (new Exception("SetBooleanParam() CreateNode error"));
            }

            SetAttr(node, strParamName, bValue == true ? "true" : "false");
            return bCreateElement;
        }

        // ��ò����͵����Բ���ֵ
        // return:
        //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
        //      0   ���������ȷ����Ĳ���ֵ
        //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
        public static int GetBooleanParam(XmlNode node,
            string strParamName,
            bool bDefaultValue,
            out bool bValue,
            out string strError)
        {
            strError = "";
            bValue = bDefaultValue;

            string strValue = DomUtil.GetAttr(node, strParamName);

            strValue = strValue.Trim();

            if (String.IsNullOrEmpty(strValue) == true)
            {
                bValue = bDefaultValue;
                return 1;
            }

            strValue = strValue.ToLower();

            if (strValue == "yes" || strValue == "on"
                || strValue == "1" || strValue == "true")
            {
                bValue = true;
                return 0;
            }

            // TODO: ���Լ���ַ�����Ҫ�ڹ涨��ֵ��Χ��

            bValue = false;
            return 0;
        }



        // ��������͵����Բ���ֵ
        // return:
        //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
        //      0   ���������ȷ����Ĳ���ֵ
        //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
        public static int GetIntegerParam(XmlNode node,
            string strParamName,
            int nDefaultValue,
            out int nValue,
            out string strError)
        {
            strError = "";
            nValue = nDefaultValue;

            string strValue = DomUtil.GetAttr(node, strParamName);


            if (String.IsNullOrEmpty(strValue) == true)
            {
                nValue = nDefaultValue;
                return 1;
            }

            try
            {
                nValue = Convert.ToInt32(strValue);
            }
            catch (Exception ex)
            {
                strError = "���� " + strParamName + " ��ֵӦ��Ϊ��ֵ�͡�������Ϣ: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // ��������͵����Բ���ֵ
        // return:
        //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
        //      0   ���������ȷ����Ĳ���ֵ
        //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
        public static int GetIntegerParam(XmlNode node,
            string strParamName,
            long nDefaultValue,
            out long nValue,
            out string strError)
        {
            strError = "";
            nValue = nDefaultValue;

            string strValue = DomUtil.GetAttr(node, strParamName);


            if (String.IsNullOrEmpty(strValue) == true)
            {
                nValue = nDefaultValue;
                return 1;
            }

            try
            {
                nValue = Convert.ToInt64(strValue);
            }
            catch (Exception ex)
            {
                strError = "���� " + strParamName + " ��ֵӦ��Ϊ��ֵ�͡�������Ϣ: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // ��ø������͵����Բ���ֵ
        // return:
        //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
        //      0   ���������ȷ����Ĳ���ֵ
        //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
        public static int GetDoubleParam(XmlNode node,
            string strParamName,
            double nDefaultValue,
            out double nValue,
            out string strError)
        {
            strError = "";
            nValue = nDefaultValue;

            string strValue = DomUtil.GetAttr(node, strParamName);


            if (String.IsNullOrEmpty(strValue) == true)
            {
                nValue = nDefaultValue;
                return 1;
            }

            try
            {
                nValue = Convert.ToDouble(strValue);
            }
            catch (Exception ex)
            {
                strError = "���� " + strParamName + " ��ֵӦ��Ϊ(����)��ֵ�͡�������Ϣ: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // ��װ��İ汾
        // ������prolog
        public static int GetIndentXml(string strXml,
            out string strOutXml,
            out string strError)
        {
            return GetIndentXml(strXml,
                false,
                out strOutXml,
                out strError);
        }

        public static string GetIndentXml(string strXml)
        {
            string strOutXml = "";
            string strError = "";
            int nRet = GetIndentXml(strXml,
    false,
    out strOutXml,
    out strError);
            if (nRet == -1)
                return strError;
            return strOutXml;
        }

        // parameters:
        //      bHasProlog  �Ƿ�prolog
		public static int GetIndentXml(string strXml,
            bool bHasProlog,
			out string strOutXml,
			out string strError)
		{
			strOutXml = "";
			strError = "";

			if (String.IsNullOrEmpty(strXml) == true)
			{
				return 0;
			}

			XmlDocument dom = new XmlDocument();
			try 
			{
				dom.LoadXml(strXml);
			}
			catch (Exception ex)
			{
				strError = ex.Message;
				return -1;
			}

            if (bHasProlog == true)
                strOutXml = GetIndentXml(dom);
            else
			    strOutXml = GetIndentXml(dom.DocumentElement);

			return 0;
		}



        // ���������XMLԴ����
        public static string GetIndentXml(XmlNode node)
        {
            MemoryStream m = new MemoryStream();

            XmlTextWriter w = new XmlTextWriter(m, Encoding.UTF8);
            w.Formatting = Formatting.Indented;
            w.Indentation = 4;
            node.WriteTo(w);
            w.Flush();

            m.Seek(0, SeekOrigin.Begin);

            StreamReader sr = new StreamReader(m, Encoding.UTF8);
            string strText = sr.ReadToEnd();
            sr.Close();

            w.Close();

            return strText;
        }

        public static string GetIndentInnerXml(XmlNode node)
        {
            MemoryStream m = new MemoryStream();

            XmlTextWriter w = new XmlTextWriter(m, Encoding.UTF8);
            w.Formatting = Formatting.Indented;
            w.Indentation = 4;
            node.WriteContentTo(w);
            w.Flush();

            m.Seek(0, SeekOrigin.Begin);

            StreamReader sr = new StreamReader(m, Encoding.UTF8);
            string strText = sr.ReadToEnd();
            sr.Close();

            w.Close();

            return strText;
        }

        public static string GetDomEncodingString(XmlDocument dom)
        {
            if (dom.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
            {
                XmlDeclaration dec = (XmlDeclaration)dom.FirstChild;
                return dec.Encoding;
            }

            return null;
        }

        public static bool SetDomEncodingString(XmlDocument dom,
            string strEncoding)
        {
            if (dom.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
            {
                XmlDeclaration dec = (XmlDeclaration)dom.FirstChild;
                dec.Encoding = strEncoding;
                return true;
            }

            return false;
        }

		// ���������XMLԴ����
        // ע������prolog�ȡ�������������Щ������GetIndentXml(XmlNode)�汾
		public static string GetIndentXml(XmlDocument dom)
		{
            string strEncoding = GetDomEncodingString(dom);
            Encoding encoding = Encoding.UTF8;
            if (string.IsNullOrEmpty(strEncoding) == false)
            {
                try
                {
                    encoding = Encoding.GetEncoding(strEncoding);
                }
                catch
                {
                    encoding = Encoding.UTF8;
                }
            }

			// 
			MemoryStream m = new MemoryStream();

            XmlTextWriter w = new XmlTextWriter(m, encoding);
			w.Formatting = Formatting.Indented;
			w.Indentation = 4;
            dom.Save(w);
			w.Flush();

			m.Seek(0, SeekOrigin.Begin);

            StreamReader sr = new StreamReader(m, encoding);
			string strText = sr.ReadToEnd();
			sr.Close();

			w.Close();

			return strText;
		}


		// �õ�һ���ڵ��ڸ��׵Ķ��Ӽ��е���� ��0��ʼ
        // parameters:
        //      node    ���ӽڵ�
        // return:
        //      �����ڸ��׵Ķ��Ӽ��е���ţ�-1û�ҵ�
		// ��д��: ���ӻ�
		public static int GetIndex(XmlNode node)
		{
            Debug.Assert(node != null, "GetIndex()���ó���node����ֵ����Ϊnull��");

			XmlNode parentNode = node.ParentNode;
			for(int i=0;i<parentNode.ChildNodes.Count;i++)
			{
				XmlNode curNode = parentNode.ChildNodes[i];
				if (curNode == node)
					return i;
			}
            return -1;
		}


		// �õ�parentNode�ĵ�һ��element���ӽڵ�
		// parameter:
		//		parentNode	���׽ڵ�
		// return:
		//		��һ��element���ӽڵ㣬δ�ҵ�����null
		// ��д��: ���ӻ�
		public static XmlElement GetFirstElementChild(XmlNode parentNode)
		{
            Debug.Assert(parentNode != null, "GetFirstElementChild()����parentNode����ֵ����Ϊnull��");

			for(int i=0;i<parentNode.ChildNodes.Count;i++)
			{
				XmlNode node = parentNode.ChildNodes[i];
				if (node.NodeType == XmlNodeType.Element)
					return (XmlElement)node;
			}
			return null;
		}

		// �õ�parentNode�ĵ�һ��CDATA���ӽڵ�
		// parameter:
		//		parentNode	���׽ڵ�
		// return:
		//		��һ��XmlCDataSection���ӽڵ�
		// ��д��: ���ӻ�
		public static XmlCDataSection GetFirstCDATAChild(XmlNode parentNode)
		{
            Debug.Assert(parentNode != null, "GetFirstCDATAChild()����parentNode����ֵ����Ϊnull��");

			for(int i=0;i<parentNode.ChildNodes.Count;i++)
			{
				XmlNode node = parentNode.ChildNodes[i];
				if (node.NodeType == XmlNodeType.CDATA)
					return (XmlCDataSection)node;
			}
			return null;
		}


		// �Ӹ��ڵ㿪ʼ������ָ����Ԫ�ؽڵ�xpath�����������õ�����ֵ
        // ��д�ߣ�л��
		public static string GetAttr(XmlNode nodeRoot, 
			string strNodePath,
			string strAttrName)
		{
			XmlNode node = nodeRoot.SelectSingleNode(strNodePath);

			if (node == null)
				return "";

			return GetAttr(node, strAttrName);
		}

        // ̽��XmlNode�ڵ��ָ�������Ƿ����
        // parameters:
        //      node        XmlNode�ڵ�
        //      strAttrName    ��������
        // return:
        public static bool HasAttr(XmlNode node,
            string strAttrName)
        {
            Debug.Assert(node != null, "GetAttr()���ô���node����ֵ����Ϊnull��");
            Debug.Assert(strAttrName != null && strAttrName != "", "GetAttr()���ô���strAttrName����ֵ����Ϊnull����ַ�����");

            // 2012/4/25 NodeType == Document�Ľڵ㣬��Attributes��ԱΪnull
            if (node.Attributes == null)
                return false;

            if (node.Attributes[strAttrName] == null)
                return false;
            return true;
        }

		// �õ�XmlNode�ڵ��ָ�����Ե�ֵ
        // TODO: ������ʹ�õ�SelectSingleNode()�������Ƿ���˷�ʱ�䣬����������ֱ�Ӵ�node��Ӧ�����Լ������������õ�ʱ��Ƚϡ�
        // parameters:
        //      node        XmlNode�ڵ�
        //      strAttrName    ��������
        // return:
        //      ��������ֵ
        //      ע�����δ�ҵ�ָ�������Խڵ㣬����""
		public static string GetAttr(XmlNode node,
            string strAttrName)
		{
            Debug.Assert(node != null, "GetAttr()���ô���node����ֵ����Ϊnull��");
            Debug.Assert(strAttrName != null && strAttrName != "", "GetAttr()���ô���strAttrName����ֵ����Ϊnull����ַ�����");
            
            // 2012/4/25 NodeType == Document�Ľڵ㣬��Attributes��ԱΪnull
            if (node.Attributes == null)
                return "";

            // 2012/2/16 �Ż�
            XmlAttribute attr = node.Attributes[strAttrName];
            if (attr == null)
                return "";
            return attr.Value;
		}

		// �õ�XmlNode�ڵ�ָ�����ƿռ�����Ե�ֵ
        // parameters:
        //      strNameSpaceUrl ���Ե����ֿռ��url
        //      node            XmlNode�ڵ�
        //      strAttrName        ��������
        // return:
        //      ָ�����Ե�ֵ
        //      ע�����δ�ҵ�ָ�������Խڵ㣬����"";
        // ???������ʹ�õ�SelectSingleNode()�������Ƿ���˷�ʱ�䣬����������ֱ�Ӵ�node��Ӧ�����Լ������������õ�ʱ��Ƚϡ�
		public static string GetAttr(string strAttrNameSpaceUri,
			XmlNode node,
			string strAttrName)
		{
            Debug.Assert(node != null, "GetAttr()���ô���node����ֵ����Ϊnull��");
            Debug.Assert(strAttrName != null && strAttrName != "", "GetAttr()���ô���strAttrName����ֵ����Ϊnull����ַ�����");
            Debug.Assert(strAttrNameSpaceUri != null && strAttrNameSpaceUri != "",
                "GetAttr()���ô���strNameSpaceUri����ֵ����Ϊnull����ַ�����");

			XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("abc", strAttrNameSpaceUri);
            XmlNode nodeAttr = node.SelectSingleNode("@abc:" + strAttrName, nsmgr);

			if (nodeAttr == null)
				return "";
			else
				return nodeAttr.Value;
		}


        // �õ�XmlNode�ڵ��ָ�����Ե�ֵ
        // parameters:
        //      node        XmlNode�ڵ�
        //      strAttrName    ��������
        // return:
        //      ��������ֵ
        //      ע�����δ�ҵ�ָ�������Խڵ㣬����null
        // ��д�ߣ����ӻ�
        // ???������ʹ�õ�SelectSingleNode()�������Ƿ���˷�ʱ�䣬����������ֱ�Ӵ�node��Ӧ�����Լ������������õ�ʱ��Ƚϡ�
        public static string GetAttrDiff(XmlNode node,
            string strAttrName)
        {
            Debug.Assert(node != null, "GetAttrDiff()���ô���node����ֵ����Ϊnull��");
            Debug.Assert(strAttrName != null && strAttrName != "", "GetAttrDiff()���ô���strAttrName����ֵ����Ϊnull����ַ�����");


            /*
            XmlNode nodeAttr = node.SelectSingleNode("@" + strAttrName);

            if (nodeAttr == null)
                return null;
            else
                return nodeAttr.Value;
             * */
            // 2012/4/25 NodeType == Document�Ľڵ㣬��Attributes��ԱΪnull
            if (node.Attributes == null)
                return null;

            // 2012/2/16 �Ż�
            XmlAttribute attr = node.Attributes[strAttrName];
            if (attr == null)
                return null;
            return attr.Value;
        }

        // ��д�ߣ�л��
		public static string GetAttrOrDefault(XmlNode node,
            string strAttrName,
			string strDefault)
		{
			if (node == null)
				return strDefault;
            /*
			XmlNode nodeAttr = node.SelectSingleNode("@" + attrName);

			if (nodeAttr == null)
				return strDefault;
			else
				return nodeAttr.Value;
             * */

            Debug.Assert(node.Attributes != null, "");
            /*
            // 2012/4/25 NodeType == Document�Ľڵ㣬��Attributes��ԱΪnull
            if (node.Attributes == null)
                return strDefault;
             * */

            // 2012/2/16 �Ż�
            XmlAttribute attr = node.Attributes[strAttrName];
            if (attr == null)
                return strDefault;
            return attr.Value;

		}


        // ����XmlNode�ڵ�ָ�����Ե�ֵ
        // ע: 2013/2/22 �������� XmlElement node.SetAttribute(strAttrName, strAttrValue) �����������
        // parameters:
        //      node            XmlNode�ڵ�
        //      strAttrName     ��������
        //      strAttrValue    ����ֵ,����Ϊ""��null,���==null,��ʾɾ���������
		public static void SetAttr(XmlNode node,
			string strAttrName,
			string strAttrValue)
		{
            Debug.Assert(node != null, "SetAttr()���ô���node����ֵ����Ϊnull��");
            Debug.Assert(strAttrName != null && strAttrName != "", "SetAttr()���ô���strAttrName����ֵ����Ϊnull����ַ�����");

            // 2012/4/25 NodeType == Document�Ľڵ㣬��Attributes��ԱΪnull
            Debug.Assert(node.Attributes != null, "");

			XmlAttributeCollection listAttr = node.Attributes;
            XmlAttribute attrFound = listAttr[strAttrName];

            if (attrFound == null) 
			{
                if (strAttrValue == null)
					return ;	// �����Ͳ�����

                XmlElement element = (XmlElement)node; 
                element.SetAttribute(strAttrName, strAttrValue);
			}
			else 
			{
                if (strAttrValue == null)
                    node.Attributes.Remove(attrFound);
				else
                    attrFound.Value = strAttrValue;
			}
		}

        // ����XmlNodeԪ�ؽڵ������ֵ�����ֿռ�汾
        // parameters:
        //      node                XmlNode�ڵ�
        //      strAttrName         ��������
        //      strAttrNameSpaceURI �������ֿռ��URI
        //      strAttrValue        ����ֵ,���==null,��ɾ���������
		public static void SetAttr(XmlNode node,
			string strAttrName,
			string strAttrNameSpaceURI,
			string strAttrValue)
		{
            Debug.Assert(node != null, "SetAttr()���ô���node����ֵ����Ϊnull��");
            Debug.Assert(strAttrName != null && strAttrName != "", "SetAttr()���ô���strAttrName����ֵ����Ϊnull����ַ�����");
            Debug.Assert(strAttrNameSpaceURI != null && strAttrNameSpaceURI != "", "SetAttr()���ô���strAttrNameSpaceURI����ֵ����Ϊnull����ַ�����");

            // 2012/4/25 NodeType == Document�Ľڵ㣬��Attributes��ԱΪnull
            Debug.Assert(node.Attributes != null, "");

			XmlAttributeCollection listAttr = node.Attributes;
            XmlAttribute attrFound = listAttr[strAttrName,strAttrNameSpaceURI];

            if (attrFound == null)
            {
                if (strAttrValue == null)
                    return;	// �����Ͳ�����

                XmlElement element = (XmlElement)node;
                element.SetAttribute(strAttrName, strAttrNameSpaceURI, strAttrValue);
            }
            else
            {
                if (strAttrValue == null)
                    node.Attributes.Remove(attrFound);
                else
                    attrFound.Value = strAttrValue;
            }
		}

        // ����XmlNodeԪ�ؽڵ������ֵ��ǰ׺�����ֿռ�汾
        // parameters:
        //      node                XmlNode�ڵ�
        //      strAttrName         ��������
        //      strPrefix   ǰ׺
        //      strAttrNameSpaceURI �������ֿռ��URI
        //      strAttrValue        ����ֵ,���==null,��ɾ���������
        public static void SetAttr(XmlNode node,
            string strName,
            string strPrefix,
            string strNamespaceURI,
            string strValue)
        {
            Debug.Assert(node != null, "SetAttr()���ô���node����ֵ����Ϊnull��");
            Debug.Assert(String.IsNullOrEmpty(strName) == false, "SetAttr()���ô���strName����ֵ����Ϊnull����ַ�����");
            Debug.Assert(String.IsNullOrEmpty(strNamespaceURI) == false, "SetAttr()���ô���strNamespaceURI����ֵ����Ϊnull����ַ�����");
            Debug.Assert(String.IsNullOrEmpty(strPrefix) == false, "SetAttr()���ô���strPrefix����ֵ����Ϊnull����ַ�����");

            // 2012/4/25 NodeType == Document�Ľڵ㣬��Attributes��ԱΪnull
            Debug.Assert(node.Attributes != null, "");

            XmlAttribute attrFound = node.Attributes[strName, strNamespaceURI];

            if (attrFound == null)
            {
                if (strValue == null)
                    return;	// �����Ͳ�����

                XmlElement element = (XmlElement)node;
                XmlAttribute attr = node.OwnerDocument.CreateAttribute(strPrefix, strName, strNamespaceURI);
                attr.Value = strValue;
                element.SetAttributeNode(attr);
            }
            else
            {
                if (strValue == null)
                    node.Attributes.Remove(attrFound);
                else
                    attrFound.Value = strValue;
            }
        }

        // �õ�childNodes�����У����е�CDATA�ڵ�
		// parameters:
        //      childNodes: ���ӽڵ㼯�ϣ������и������͵Ľڵ�
        // return:
        //      ��������CDATA�ڵ���ɵ�����,���һ��CDATA�ڵ㶼û�У�����һ���ռ���
        // ��д�ߣ����ӻ�
		public static ArrayList GetCdataNodes(XmlNodeList childNodes)
		{
            Debug.Assert(childNodes != null, "GetCdataNodes()���ô���childNodes����ֵ����Ϊnull��");

			ArrayList aCDATA = new ArrayList();
			foreach(XmlNode item in childNodes)
			{
				if (item.NodeType == XmlNodeType.CDATA)
					aCDATA.Add(item);
			}
			return aCDATA;
		}		
			

		// ͨ��strXpath·���𼶴���node�����strXpath��Ӧ�Ľڵ��Ѵ��ڣ���ֱ�ӷ���
		// paramter:
        //		nodeRoot	���ڵ�
		//		strXpath	�򵥵�xpath�����һ���������������(��@������)
        // return:
        //      ����strXpath��Ӧ�Ľڵ�
        // ��д�ߣ����ӻ�
		public static XmlNode CreateNodeByPath(XmlNode nodeRoot,
			string strXpath)
		{
            Debug.Assert(nodeRoot != null, "CreateNodeByPath()���ô���nodeRoot����ֵ����Ϊnull��");

            XmlNode nodeFound = nodeRoot.SelectSingleNode(strXpath);
			if (nodeFound != null)
				return nodeFound;

			string[] aNodeName = strXpath.Split(new Char [] {'/'});
            return DomUtil.CreateNode(nodeRoot, aNodeName);
		}

        // �������������𼶴����ڵ�
        // parameters:
        //      nodeRoot    ���ڵ�
        //      aNodeName   �ڵ���������
        // return:
        //      �����´�����XmlNode�ڵ�
        // ��д�ߣ����ӻ�
        public static XmlNode CreateNode(XmlNode nodeRoot,
            string[] aNodeName)
        {
            XmlDocument dom = nodeRoot.OwnerDocument;
            if (dom == null)
            {
                if (nodeRoot is XmlDocument)
                    dom = (XmlDocument)nodeRoot;
                else
                    throw (new Exception("CreateNode()�����쳣��nodeRoot��OwnerDocument����ֵΪnull����nodeRoot����XmlDocument���͡�"));
            }

            if (aNodeName.Length == 0)
                return null;

            int i = 0;
            if (aNodeName[0] == "")
                i = 1;

            XmlNode nodeCurrent = nodeRoot;
            XmlNode temp = null;
            for (; i < aNodeName.Length; i++)
            {
                string strOneName = aNodeName[i];
                if (strOneName == "")
                    throw new Exception("ͨ��CreateNode()����Ԫ��ʱ����'" + Convert.ToInt32(i) + "'��������Ϊ�ա�");

                temp = nodeCurrent.SelectSingleNode(strOneName);
                if (temp == null)
                {
                    Char firstChar = strOneName[0];
                    if (firstChar == '@' && i == aNodeName.Length - 1)
                    {
                        string strAttrName = strOneName.Substring(1);
                        if (strAttrName == "")
                            throw new Exception("ͨ��CreateNode()����Ԫ��ʱ����'" + Convert.ToInt32(i) + "'������������Ϊ�ա�");
                        DomUtil.SetAttr(nodeCurrent, strAttrName, "");
                        temp = nodeCurrent.SelectSingleNode("@" + strAttrName);
                        if (temp == null)
                            throw new Exception("�Ѿ�������'" + strAttrName + "'���ԣ��������Ҳ�����");
                    }
                    else
                    {
                        temp = dom.CreateElement(aNodeName[i]);
                        nodeCurrent.AppendChild(temp);
                    }
                }
                nodeCurrent = temp;
            }

            return nodeCurrent;
        }

        // TODO: �𽥷�ֹ�������
        // �õ�node�ڵ�ĵ�һ���ı��ڵ��ֵ,�൱��GetNodeFirstText()
        // parameter:
        //		node    XmlNode�ڵ�
        // result:
        //		node�ĵ�һ���ı��ڵ���ַ�������ȥ�հ�
        //      ע�����node�¼��������ı��ڵ㣬����"";
        // ��д�ߣ����ӻ�
        public static string GetNodeText(XmlNode node)
        {
            Debug.Assert(node != null, "GetNodeText()���ó���node����ֵ����Ϊnull��");

            XmlNode nodeText = node.SelectSingleNode("text()");
            if (nodeText == null)
                return "";
            else
                return nodeText.Value;
        }

        // TODO: �𽥷�ֹ�������
        // �õ�node�ڵ�ĵ�һ���ı��ڵ��ֵ
        // parameter:
        //		node    XmlNode�ڵ�
        // result:
        //		node�ĵ�һ���ı��ڵ���ַ���������ȥ�հ�
        //      ע�����node�¼��������ı��ڵ㣬����null;
        // ��д�ߣ����ӻ�
		public static string  GetNodeTextDiff(XmlNode node)
		{
            Debug.Assert(node != null, "GetNodeTextDiff()���ó���node����ֵ����Ϊnull��");

            XmlNode nodeText = node.SelectSingleNode("text()");
			if (nodeText == null)
				return null;
			else
				return nodeText.Value;
		}

		
		// �õ�node�ڵ�ĵ�һ���ı��ڵ��ֵ
		// parameter:
		//		node    XmlNode�ڵ�
		// result:
		//		node�ĵ�һ���ı��ڵ���ַ���������ȥ�հ�
        //      ע�����node�¼��������ı��ڵ㣬����"";
		// ��д�ߣ����ӻ�
		public static string GetNodeFirstText(XmlNode node)
		{
            Debug.Assert(node != null, "GetNodeFirstText()���ó���node����ֵ����Ϊnull��");

			XmlNode nodeText = node.SelectSingleNode("text()");
            if (nodeText == null)
				return "";
			else
				return nodeText.Value.Trim();
		}

#if NO
        // TODO: �𽥷�ֹ�������
        // �õ���ǰ�ڵ����е��ı��ڵ�ֵ
		// parameter:
		//      node    XmlNode�ڵ�
		// result:
		//		node�������ı��ڵ�����������ַ������м䲻���κη��ţ�ȥÿ�����ֽڵ����ݵ����ҿհ�
        //      ע�����node�¼��������ı��ڵ㣬����"";
		// ��д�ߣ����ӻ�
		public static string  GetNodeAllText(XmlNode node)
		{
            Debug.Assert(node != null, "GetNodeAllText()���ó���node����ֵ����Ϊnull��");

			XmlNodeList nodeTextList = node.SelectNodes("text()");
			string strResult = "";
			foreach(XmlNode oneNode in nodeTextList)
			{
				strResult += oneNode.Value.Trim ();   //�����ҿհ׶�ȥ��
			}
			return strResult;
		}
#endif

        // ��node�ڵ�ĵ�һ���ı��ڵ������
        // parameters:
        //      node    XmlNode�ڵ�
        //      strNewText  �µ���������
        // return:
        //      void
        // ��д�ߣ����ӻ�
        public static void SetNodeText(XmlNode node,
            string newText)
        {
            Debug.Assert(node != null, "SetNodeText()���ô���node����ֵ����Ϊnull��");

            XmlNode nodeText = node.SelectSingleNode("text()");
            if (nodeText == null)
                node.AppendChild(node.OwnerDocument.CreateTextNode(newText));
            else
                nodeText.Value = newText;
        }

        // ��ָ���Ľڵ����һ���ı��ڵ��ֵ
        // �����һ���ı��ڵ����,��ֱ�Ӹ�text��ֵ��
        // �����һ���ı��ڵ㲻���ڣ����CreateNode()�𼶴����ڵ㣬Ȼ��ֵ
        // parameters:
        //      nodeRoot    ���ڵ�
        //      strXpath    �ڵ�·��
        //      strNewText  ���ı�ֵ
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        // ��д�ߣ����ӻ�
        public static int SetNodeValue(XmlNode nodeRoot,
            string strXpath,
            string strNewText,
            out string strError)
        {
            strError = "";

            Debug.Assert(nodeRoot != null, "SetNodeValue()���ô���nodeRoot����ֵ����Ϊnull��");
            Debug.Assert(strXpath != null && strXpath != "", "SetNodeValue()���ô���strXpath����ֵ����Ϊnull����ַ�����");


            XmlNode nodeFound = nodeRoot.SelectSingleNode(strXpath);
            if (nodeFound == null)
            {
                string[] aNodeName = strXpath.Split(new Char[] { '/' });
                try
                {
                    nodeFound = DomUtil.CreateNode(nodeRoot, aNodeName);
                }
                catch (Exception ex)
                {
                    strError = "CreateNode()����ԭ��" + ex.Message;
                    return -1;
                }
            }

            if (nodeFound == null)
            {
                strError = "SetNodeValue()����ʱnodeFound������Ϊnull�ˡ�";
                return -1;
            }

            DomUtil.SetNodeText(nodeFound, strNewText);
            return 0;
        }

        // TODO: ��������Ĺ������˷ѽ⣬�𲽷�ֹ?
        // ��д��: л��
        public static int SetNodeValue(XmlNode nodeRoot,
            string strXpath,
            XmlNode newNode)
        {
            if (nodeRoot == null)
                return -1;

            XmlNode nodeFound = nodeRoot.SelectSingleNode(strXpath);

            if (nodeFound == null)
            {
                string[] aNodeName = strXpath.Split(new Char[] { '/' });
                nodeFound = CreateNode(nodeRoot, aNodeName);
            }

            if (nodeFound == null)
                return -1;


            //XmlNode nodeTemp = nodeFound.OwnerDocument.CreateElement("test");
            //nodeTemp = newNode.CloneNode(true);

            nodeFound.InnerXml = newNode.OuterXml;

            //nodeFound.AppendChild(newNode.CloneNode(true));


            return 0;
        }


        // 2006/11/29
        public static string GetElementInnerXml(XmlNode nodeRoot,
    string strXpath)
        {
            XmlNode node = nodeRoot.SelectSingleNode(strXpath);
            if (node == null)
                return "";

            return node.InnerXml;
        }

        // д��һ��Ԫ���ı�
        // return:
        //      ���ظ�Ԫ�ص�XmlNode
        public static XmlNode SetElementInnerXml(XmlNode nodeRoot,
            string strXpath,
            string strInnerXml)
        {
            if (nodeRoot == null)
            {
                throw (new Exception("nodeRoot��������Ϊnull"));
            }

            XmlNode nodeFound = nodeRoot.SelectSingleNode(strXpath);

            if (nodeFound == null)
            {
                string[] aNodeName = strXpath.Split(new Char[] { '/' });
                nodeFound = CreateNode(nodeRoot, aNodeName);
            }

            if (nodeFound == null)
            {
                throw (new Exception("SetElementInnerXml() CreateNode error"));
            }

            nodeFound.InnerXml = strInnerXml;
            return nodeFound;
        }

        // 2006/11/29
        public static string GetElementOuterXml(XmlNode nodeRoot,
    string strXpath)
        {
            XmlNode node = nodeRoot.SelectSingleNode(strXpath);
            if (node == null)
                return "";

            return node.OuterXml;
        }

        public static bool IsEmptyElement(XmlNode nodeRoot,
    string strXpath)
        {
            XmlNode node = nodeRoot.SelectSingleNode(strXpath);
            if (node == null)
                return true;

            if (node.Attributes.Count == 0 && node.ChildNodes.Count == 0)
                return true;

            return false;
        }

        // д��һ��Ԫ���ı�
        // return:
        //      ���ظ�Ԫ�ص�XmlNode
        public static XmlNode SetElementOuterXml(XmlNode nodeRoot,
            string strXpath,
            string strOuterXml)
        {
            if (nodeRoot == null)
            {
                throw (new Exception("nodeRoot��������Ϊnull"));
            }

            XmlNode nodeFound = nodeRoot.SelectSingleNode(strXpath);

            if (nodeFound == null)
            {
                string[] aNodeName = strXpath.Split(new Char[] { '/' });
                nodeFound = CreateNode(nodeRoot, aNodeName);
            }

            if (nodeFound == null)
            {
                throw (new Exception("SetElementOuterXml() CreateNode error"));
            }

            XmlDocumentFragment fragment = nodeFound.OwnerDocument.CreateDocumentFragment();
            fragment.InnerXml = strOuterXml;

            nodeFound.ParentNode.InsertAfter(fragment, nodeFound);

            nodeFound.ParentNode.RemoveChild(nodeFound);

            nodeFound = nodeRoot.SelectSingleNode(strXpath);
            return nodeFound;
        }

        // 2009/10/31
        // д��һ��Ԫ�ص�OuterXml
        // return:
        //      ���ر䶯���Ԫ�ص�XmlNode
        public static XmlNode SetElementOuterXml(XmlNode node,
            string strOuterXml)
        {
            if (node == null)
            {
                throw (new Exception("node��������Ϊnull"));
            }

            XmlDocumentFragment fragment = node.OwnerDocument.CreateDocumentFragment();
            fragment.InnerXml = strOuterXml;

            node.ParentNode.InsertAfter(fragment, node);

            XmlNode new_node = node.NextSibling;    // 2012/12/12 ������

            node.ParentNode.RemoveChild(node);

            return new_node;
        }

        // �����¶��󵽶����ǵ���ǰ��
        public static XmlNode InsertFirstChild(XmlNode parent, XmlNode newChild)
        {
            XmlNode refChild = null;
            if (parent.ChildNodes.Count > 0)
                refChild = parent.ChildNodes[0];

            return parent.InsertBefore(newChild, refChild);
        }

        // 2012/9/30
        public static string GetElementInnerText(XmlNode nodeRoot,
    string strXpath)
        {
            XmlNode node = nodeRoot.SelectSingleNode(strXpath);
            if (node == null)
                return null;

            return node.InnerText.Trim();
        }

        // 2012/9/30
        // ���һ��Ԫ�ص�һ������ֵ
        public static string GetElementAttr(XmlNode nodeRoot,
            string strXpath,
            string strAttrName)
        {
            XmlNode node = nodeRoot.SelectSingleNode(strXpath);
            if (node == null)
                return null;
            XmlAttribute attr = node.Attributes[strAttrName];
            if (attr == null)
                return null;
            return attr.Value.Trim();
        }

        // ��д��: л��
        public static string GetElementText(XmlNode nodeRoot,
            string strXpath)
        {
            XmlNode node = nodeRoot.SelectSingleNode(strXpath);
            if (node == null)
                return "";

            XmlNode nodeText;
            nodeText = node.SelectSingleNode("text()");

            if (nodeText == null)
                return "";
            else
                return nodeText.Value;
        }

        // �°汾 2006/10/24
        // ���һ��Ԫ�ص��¼��ı�
        // һ������Ԫ�ؽڵ����
        public static string GetElementText(XmlNode nodeRoot,
            string strXpath,
            out XmlNode node)
        {
            node = nodeRoot.SelectSingleNode(strXpath);
            if (node == null)
                return "";

            return node.InnerText;
        }

        // ��д��: л��
        public static string GetElementText(XmlNode nodeRoot,
            string strXpath,
            XmlNamespaceManager mngr)
        {
            XmlNode node = nodeRoot.SelectSingleNode(strXpath, mngr);
            if (node == null)
                return "";

            XmlNode nodeText;
            nodeText = node.SelectSingleNode("text()");

            if (nodeText == null)
                return "";
            else
                return nodeText.Value;
        }

        // ɾ��һ��Ԫ�� 2006/10/26
        // return:
        //      ���ر�ɾ������XmlNode
        public static XmlNode DeleteElement(XmlNode nodeRoot,
            string strXpath)
        {
            if (nodeRoot == null)
            {
                throw (new Exception("nodeRoot��������Ϊnull"));
            }

            XmlNode nodeFound = nodeRoot.SelectSingleNode(strXpath);

            if (nodeFound == null)
                return null;    // ��Ȼ�����ڣ�����Ҳ����ɾ����

            return nodeFound.ParentNode.RemoveChild(nodeFound);
        }

        // ɾ�����ɸ�Ԫ�� 2011/1/11
        // return:
        //      ���ر�ɾ������XmlNode����
        public static List<XmlNode> DeleteElements(XmlNode nodeRoot,
            string strXpath)
        {
            if (nodeRoot == null)
            {
                throw (new Exception("nodeRoot��������Ϊnull"));
            }


            XmlNodeList nodes = nodeRoot.SelectNodes(strXpath);

            if (nodes.Count == 0)
                return null;    // ��Ȼ�����ڣ�����Ҳ����ɾ����

            List<XmlNode> deleted_nodes = new List<XmlNode>();
            foreach (XmlNode node in nodes)
            {
                if (node.ParentNode == null)
                    continue;
                deleted_nodes.Add(node.ParentNode.RemoveChild(node));
            }

            return deleted_nodes;
        }

        /*
        // �Ƴ�һ��Ԫ��
        // 2007/6/19
        public static XmlNode RemoveElement(XmlNode nodeRoot,
            string strXPath)
        {
            if (nodeRoot == null)
            {
                throw (new Exception("nodeRoot��������Ϊnull"));
            }

            XmlNode nodeFound = nodeRoot.SelectSingleNode(strXPath);

            if (nodeFound == null)
            {
                // ���ò����ڣ�Ҳ����ɾ����
                return null;
            }

            nodeFound.ParentNode.RemoveChild(nodeFound);

            return nodeFound;
        }*/

        // �滻ȫ�������ַ�
        // parameters:
        //      chReplace   Ҫ�滻�ɵ��ַ������Ϊ 0 ����ʾɾ�������ַ�
        static string ReplaceControlChars(string strText,
            char chReplace)
        {
            if (String.IsNullOrEmpty(strText) == true)
                return strText;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < strText.Length; i++)
            {
                char ch = strText[i];
                if (ch >= 0x1 && ch <= 0x1f)
                {
                    if (chReplace != 0)
                        sb.Append(chReplace);
                }
                else
                    sb.Append(ch);
            }

            return sb.ToString();
        }

        // �滻�����ַ��������滻 \0d \0a
        // parameters:
        //      chReplace   Ҫ�滻�ɵ��ַ������Ϊ 0 ����ʾɾ�������ַ�
        static string ReplaceControlCharsButCrLf(string strText,
    char chReplace)
        {
            if (String.IsNullOrEmpty(strText) == true)
                return strText;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < strText.Length; i++)
            {
                char ch = strText[i];
                if (ch >= 0x1 && ch <= 0x1f && ch != 0x0d && ch != 0x0a)
                {
                    if (chReplace != 0)
                        sb.Append(chReplace);
                }
                else
                    sb.Append(ch);
            }

            return sb.ToString();
        }

        // 2010/21/16
        // д��һ��Ԫ���ı�
        // ��ȥ�滻ControlChars
        // return:
        //      ���ظ�Ԫ�ص�XmlNode
        public static XmlNode SetElementTextPure(XmlNode nodeRoot,
            string strXpath,
            string strText)
        {
            if (nodeRoot == null)
            {
                throw (new Exception("nodeRoot��������Ϊnull"));
            }

            XmlNode nodeFound = nodeRoot.SelectSingleNode(strXpath);

            /*
            // 2007/6/19
            if (nodeFound == null && strText == null)
            {
                // ���ò����ڣ�Ҳ����ɾ����
                return null;
            }*/


            if (nodeFound == null)
            {
                string[] aNodeName = strXpath.Split(new Char[] { '/' });
                nodeFound = CreateNode(nodeRoot, aNodeName);
            }

            if (nodeFound == null)
            {
                throw (new Exception("SetElementText() CreateNode error"));
            }

            if (String.IsNullOrEmpty(strText) == true)
                nodeFound.InnerText = strText;
            else
                nodeFound.InnerText = strText;

            return nodeFound;
        }

        // д��һ��Ԫ���ı�
        // �ı������п��԰����س����з��ţ������������ַ���д���ʱ��ᱻ����Ϊ�Ǻ�
        // return:
        //      ���ظ�Ԫ�ص�XmlNode
        public static XmlNode SetElementText(XmlNode nodeRoot,
            string strXpath,
            string strText)
        {
            if (nodeRoot == null)
            {
                throw (new Exception("nodeRoot��������Ϊnull"));
            }

            XmlNode nodeFound = nodeRoot.SelectSingleNode(strXpath);

            /*
            // 2007/6/19
            if (nodeFound == null && strText == null)
            {
                // ���ò����ڣ�Ҳ����ɾ����
                return null;
            }*/


            if (nodeFound == null)
            {
                string[] aNodeName = strXpath.Split(new Char[] { '/' });
                nodeFound = CreateNode(nodeRoot, aNodeName);
            }

            if (nodeFound == null)
            {
                throw (new Exception("SetElementText() CreateNode error"));
            }

            /*
            if (strText == null)
            {
                // 2007/6/19
                nodeFound.ParentNode.RemoveChild(nodeFound);
            }
            else
             * */

            if (String.IsNullOrEmpty(strText) == true)
                nodeFound.InnerText = strText;
            else
                nodeFound.InnerText = ReplaceControlCharsButCrLf(strText, '*'); // 2013/3/12 ReplaceControlCharsButCrLf()   // 2008/12/19 ReplaceControlChars()

            return nodeFound;
        }
		
		// �õ�node�ڵ������nodeRoot�ڵ��xpath·��
        // parameters:
        //      nodeRoot    ���ڵ�
        //      node        ָ���Ľڵ�
        //      strXpath    out����������node�����nodeRoot��xpath·��
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����,��node������nodeRoot�¼�ʱ
        //      0   �ɹ�
		// ��д��: ���ӻ�
		public static int Node2Path(XmlNode nodeRoot,
            XmlNode node,
            out string strXpath,
            out string strError)
		{
            strXpath = "";
            strError = "";

            Debug.Assert(nodeRoot != null, "Node2Path()���ô���nodeRoot����ֵ����Ϊnull��");
            Debug.Assert(node != null, "Node2Path()���ô���node����ֵ����Ϊnull��");

			//��nodeΪ���Խڵ�ʱ����������xpath�ַ���
			string strAttr = "";
			if (node.NodeType == XmlNodeType.Attribute)
			{
				strAttr = "/@" + node.Name;
				XmlAttribute AttrNode = (XmlAttribute)node;
				node = AttrNode.OwnerElement;
			}

            bool bBelongRoot = false;

			while(node != null)
			{
                if (node == nodeRoot)
                {
                    bBelongRoot = true;
                    break;
                }

                XmlNode nodeMyself = node;

				node = node.ParentNode;
				if (node == null)
					break;
				
				XmlNode nodeTemp = node.FirstChild;
				int nIndex = 1;
				while(nodeTemp != null)
				{
					if (nodeTemp == nodeMyself) //Equals(nodeTemp,nodeMyself))
					{
                        if (strXpath != "")
                            strXpath = "/" + strXpath;

                        strXpath = nodeMyself.Name + "[" + System.Convert.ToString(nIndex) + "]" + strXpath;
						break;
					}
					if (nodeTemp.Name == nodeMyself.Name)
						nIndex += 1;

                    nodeTemp = nodeTemp.NextSibling;
				}
			}

            if (bBelongRoot == false)
            {
                strError = "Node2Path()���ô���node������nodeRoot���¼�";
                return -1;
            }

			strXpath = strXpath + strAttr;
            return 0;
		}



	} // DomUtil�����


}
