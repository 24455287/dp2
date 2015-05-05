using System;
using System.Collections;

using System.IO;
using System.Text;

using System.Xml;
using System.Text.RegularExpressions;

namespace DigitalPlatform.Xml
{	
	public class Ns
	{
		public const string dc = "http://purl.org/dc/elements/1.1/";
		public const string xlink = "http://www.w3.org/1999/xlink";
		public const string xml = "http://www.w3.org/XML/1998/namespace";
        public const string usmarcxml = "http://www.loc.gov/MARC21/slim";
		public const string unimarcxml = "http://dp2003.com/UNIMARC";
	}

	public class DpNs
	{
		public const string dprms = "http://dp2003.com/dprms";
		public const string dpdc = "http://dp2003.com/dpdc";
		public const string unimarcxml = "http://dp2003.com/UNIMARC";
	}

	// ���㽫�ַ����е�xml�����ַ�ת��Ϊʵ�巽ʽ.
	// ��������������static����ʵ�ֹ���, ���ǿ��ǵ�ÿ�κ�������ʱ,
	// new XmlTextWriter��StringWriter����ķ�ʱ�����Դ,
	// ���,������Ƴɶ����ڰ�������2����,��ʹ����,����ʵ����
	// ��������, ���ñ���, ֻ����WriteString()+GetString()�Ϳ�ʵ��
	// ���蹦�ܡ�
	// ������˵�����ÿ��ʹ�ö���ʵ�����������Ȼ���������٣����������
	// ���޷������ˡ����Կ��������������static����ʵ��ͬ�����ܡ�
	public class XmlStringWriter
	{
		public XmlTextWriter xmlTextWriter = null;
		public TextWriter textWrite = null;

		public XmlStringWriter()
		{
			ClearTextWriter();
		}

		public void ClearTextWriter()
		{
			textWrite = new StringWriter ();
			xmlTextWriter = new XmlTextWriter(textWrite);
			//xmlTextWriter.Formatting = Formatting.Indented ;
		}

		public void WriteElement(string strElementName,string strText)
		{
			xmlTextWriter.WriteStartElement (strElementName);

			xmlTextWriter.WriteString (strText);

			xmlTextWriter.WriteEndElement ();
		}

		public string GetString(string strText)
		{
			xmlTextWriter.WriteString (strText);
			return textWrite.ToString ();
		}

	

		public void WriteString(string strText)
		{
			xmlTextWriter.WriteString (strText);
		}


		public string GetString()
		{
			return textWrite.ToString ();
		}

		public void FreeTextWrite()
		{
			textWrite = null;
			xmlTextWriter = null;
		}

	}


	//

	// �����ͼ:���ڴ�������ռ�
	// �ڲ�����һ��XmlNamespacemanager��Ա��������dom������
	// ���ڵ���������dom��������dom�����������ռ��ҵ������ӵ�m_nsmgr��
	// ���������ļ���������ʹ��ʱ����һ������dom���Żᴴ��m_nsmgr����������dom�����ռ��ҵ����ӵ�m_nsmgr��
	// �ⲿ��SelectNodes()��SelectSingleNode()ʱ��ֻ����ʹ�øö����m_nsmgr���ɡ�
	public class PrefixURIColl : ArrayList
	{
		public XmlNamespaceManager nsmgr = null;

		public int nSeed = 1;

		#region ���캯��

		//strDataFileName:�����ļ���
		public PrefixURIColl(string strDataFileName)
		{
			CreateNSOfData(strDataFileName);
		}
		//dom_data:����dom
		public PrefixURIColl(XmlDocument dom_data)
		{
			CreateNSOfData(dom_data);
		}

		//dom_data:����dom
		//dom_cfg:����dom
		public PrefixURIColl(string strDataFileName,
			string strCfgFileName)
		{
			CreateNSOfCfg(strDataFileName,
				strCfgFileName);
		}

		//dom_data:����dom
		//dom_cfg:����dom
		public PrefixURIColl(XmlDocument domData,
			XmlDocument domCfg)
		{
			CreateNSOfCfg(domData,domCfg);
		}

		public PrefixURIColl(XmlDocument domData,
			string strCfgFileName)
		{
			CreateNSOfCfg(domData,strCfgFileName);
		}

		#endregion 

		#region ��������

		//����������dom
		public void CreateNSOfData(string strDataFileName)
		{
			XmlDocument domData = new XmlDocument ();
			try
			{
				domData.Load(strDataFileName);
			}
			catch(Exception ex)
			{
				throw(new Exception ("CreateNSOfData()�����dom���Ϸ�" + ex.Message));
			}
			CreateNSOfData(domData);
		}
		public void CreateNSOfData(XmlDocument domData)
		{
			XmlNode root = domData.DocumentElement ;
			AddNS(root);
			this.Sort ();
			this.DumpRep ();

			//if (this.Count > 0)
			//{
			this.nsmgr = new XmlNamespaceManager (domData.NameTable );
			Add2nsmgr();
			//}
		}

		public void AddNS(XmlNode node)
		{
			if (node.NodeType != XmlNodeType.Element )
				return;

			PrefixURI prefixUri= new PrefixURI ();
			prefixUri.strPrefix = node.Prefix ;
			prefixUri.strURI = node.NamespaceURI ;

			prefixUri.strNodeName = node.Name ;

			if (prefixUri.strNodeName != ""
				&& prefixUri.strURI != "")
			{
				if (prefixUri.strPrefix == "")
				{
					prefixUri.strPrefix = "__pub" + Convert.ToString (nSeed);
					nSeed++;
				}
				this.Add (prefixUri);
			}

			foreach(XmlNode child in node.ChildNodes )
			{
				AddNS(child);
			}
		}


		//���������ļ�
		public void CreateNSOfCfg(string strDataFileName,
			string strCfgFileName)
		{
			XmlDocument domData = new XmlDocument ();
			try
			{
				domData.Load(strDataFileName);
			}
			catch(Exception ex)
			{
				throw(new Exception ("CreateNSOfCfg()���������dom���Ϸ�" + ex.Message));
			}

			CreateNSOfCfg(domData,strCfgFileName);
		}

		public void CreateNSOfCfg(XmlDocument domData,
			string strCfgFileName)
		{
			XmlDocument domCfg = new XmlDocument ();
			try
			{
				domCfg.Load(strCfgFileName);
			}
			catch(Exception ex1)
			{
				throw(new Exception ("CreateNSOfCfg()���������dom���Ϸ�" + ex1.Message));
			}
			CreateNSOfCfg(domData,domCfg);
		}

		public void CreateNSOfCfg(XmlDocument domData,
			XmlDocument domCfg)
		{
			XmlNodeList nsitemList = domCfg.DocumentElement.SelectNodes ("/root/nstable/item");
			foreach(XmlNode nsitemNode in nsitemList)
			{
				XmlNode nsNode = nsitemNode.SelectSingleNode ("nameSpace");
				XmlNode prefixNode = nsitemNode.SelectSingleNode ("prefix");

				PrefixURI prefixUri = new PrefixURI();
                if (prefixNode != null)
				    prefixUri.strPrefix = DomUtil.GetNodeText(prefixNode);
                if (nsNode != null)
				    prefixUri.strURI  = DomUtil.GetNodeText(nsNode);
				
				if (prefixUri.strPrefix != ""
					&& prefixUri.strURI != "")  //�������ļ��ﲻ����ǰ׺Ϊ��
				{
					this.Add (prefixUri);
				}
			}

			this.Sort ();
			this.DumpRep ();

			//if (this.Count > 0)
			//{
			this.nsmgr = new XmlNamespaceManager (domData.NameTable );
			Add2nsmgr();
			//}
		}


		#endregion


		//�������ϵ�ֵ�Լӵ�nsmgr��
		public void Add2nsmgr()
		{
			foreach(PrefixURI ns in this)
			{
				this.nsmgr.AddNamespace (ns.strPrefix ,ns.strURI );
			}
		}

		//ȥ��
		public void DumpRep()
		{
			int i,j;
			for(i=0;i<this.Count ;i++)
			{
				PrefixURI prefixUri1 = (PrefixURI)this[i];
				for(j=i+1;j<this.Count;j++)
				{
					PrefixURI prefixUri2 = (PrefixURI)this[j];
					if (prefixUri1.strPrefix == prefixUri2.strPrefix 
						&& prefixUri1.strURI == prefixUri2.strURI )
					{
						j--;
						this.Remove (prefixUri2);
					}
	
				}
			}
		}
		
		//��Ϣ����
		public string Dump()
		{
			string strInfo = "";

			//strInfo += this.m_nsmgr .LookupNamespace("pub") + "\r\n";
			foreach(PrefixURI ns in this)
			{
				strInfo += "strPrefix:" + ns.strPrefix + "---" + "strURI:" + ns.strURI + "---" + "strNodeName:" + ns.strNodeName + "\r\n";
			}

			return strInfo;
		}
	}

	// �����ͼ:�������ռ��
	// ��д�ߣ����ӻ�
	public class PrefixURI:IComparable
	{
		public string strPrefix;  //ǰ׺
		public string strURI;     //URI
		public string strNodeName;

		//��ʽִ�У�����ֱ��ͨ��DpKey�Ķ���ʵ��������
		//obj: �ȽϵĶ���
		//0��ʾ��ȣ�������ʾ����
		public int CompareTo(object obj)
		{
			PrefixURI prefixURI = (PrefixURI)obj;
			return String.Compare(this.strPrefix,prefixURI.strPrefix );
		}
	}



}	
