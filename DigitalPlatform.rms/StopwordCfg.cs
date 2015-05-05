using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;

using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;



namespace DigitalPlatform.rms
{

    // stopword��Ӧ����
	public class StopwordCfg
	{
		//public XmlDocument dom = null;

		Hashtable tableStopwordTable = new Hashtable();

		public StopwordCfg()
		{
			//
			// TODO: �ڴ˴���ӹ��캯���߼�
			//
		}
/*
		public int Initial(string strStopwordFileName,
			out string strError)
		{
			strError = "";

			Debug.Assert(strStopwordFileName != "" && strStopwordFileName != null,"strStopwordFileName��������Ϊnull��ա�");


			if (File.Exists(strStopwordFileName) == false)
			{
				strError = "stopword��ɫ��Ӧ�������ļ�������";
				return -1;
			}
			// ���stopword�ļ�������Ϊ�գ���û�п�ȥ�ķ����֣���������
			StreamReader sw = new StreamReader(strStopwordFileName,Encoding.UTF8);
			string strStopwordText = sw.ReadToEnd();
			sw.Close();
			if (strStopwordText == "")
				return 0;


			dom = new XmlDocument();
			try
			{
				this.dom.Load(strStopwordFileName);
			}
			catch(Exception ex)
			{
				strError = "����stopword�����ļ�'" + strStopwordFileName + "'��domʱ����" + ex.Message;
				return -1;
			}


			string strXpath = "//stopwordTable";

			XmlNodeList nodeListStopwordTable = dom.DocumentElement.SelectNodes(strXpath);
			for(int i=0;i<nodeListStopwordTable.Count;i++)
			{
				XmlNode nodeStopwordTable = nodeListStopwordTable[i];
				
				string strName = DomUtil.GetAttr(nodeStopwordTable,"name");
				StopwordTable stopwordTable = new StopwordTable(nodeStopwordTable);

				if (i == 0)
				{
					this.tableStopwordTable[""] = stopwordTable;
				}
				// �ӵ�Hashtable������
				this.tableStopwordTable[strName] = stopwordTable;
			}

			return 0;
		}
*/


        public int Initial(XmlNode nodeRoot,
            out string strError)
        {
            strError = "";

            Debug.Assert(nodeRoot != null, "nodeRoot��������Ϊnull��ա�");


            string strXpath = "//stopwordTable";

            XmlNodeList nodeListStopwordTable = nodeRoot.SelectNodes(strXpath);
            for (int i = 0; i < nodeListStopwordTable.Count; i++)
            {
                XmlNode nodeStopwordTable = nodeListStopwordTable[i];

                string strName = DomUtil.GetAttr(nodeStopwordTable, "name");
                StopwordTable stopwordTable = new StopwordTable(nodeStopwordTable);

                if (i == 0)
                {
                    // ��һ����洢һ�Σ����ڽ����ÿ�����ֵ�����
                    this.tableStopwordTable[""] = stopwordTable;
                }
                // �ӵ�Hashtable������
                this.tableStopwordTable[strName] = stopwordTable;
            }

            return 0;
        }
		
		// ��һ���ַ����������ȥ������
		// parameter:
		//		texts	���ӹ����ַ�������
		//		strStopwordFileName	�������ļ���
		//		strStopwordTable	����ʹ�÷������ĸ��� Ϊ""��null��ʾȡ��һ����
		//		strError	out ������Ϣ
		// return:
		//		-1	����
		//		0	�ɹ�
		public int DoStopword(string strStopwordTableName,
			ref List<string> texts,
			out string strError)
		{
			strError = "";

			//if (this.dom == null)
			//	return 0;

			StopwordTable stopwordTable = (StopwordTable)this.tableStopwordTable[strStopwordTableName];
			if (stopwordTable == null)
			{
				strError = "û�ҵ���Ϊ'" + strStopwordTableName + "'��<stopwordTable>Ԫ�ء�";
				return -1;
			}

			for(int i=0;i<texts.Count;i++)
			{
				texts[i] = DeleteStopword(texts[i],
					stopwordTable);
			}
			return 0;
		}

		// ��һ���ַ���ɾ��������
		// parameter:
		//		strText	���ӹ����ַ���
		//		aSeparator	���������
		//		aWord	����������
		// return:
		//		string ȥ�����ֺ���ַ���
		public string DeleteStopword(string strText,
			StopwordTable stopwordTable)
		{
			// -----------------�������ת��Ϊ'^'---------------
			string strResult = strText;
			for(int i=0; i< stopwordTable.aSeparator.Count ; i++)
			{
				string strOneSeparator = (string)stopwordTable.aSeparator[i];
				strResult = strResult.Replace(strOneSeparator,"^");
			}
			strResult = "^" + strResult + "^";
	
			//---------------------ȥ������------------
			int nPosition;
			int nLength;
			for(int i=0;i<stopwordTable.aWord.Count;i++)
			{
				string strOneWord = (string)stopwordTable.aWord[i];
				int nStart = 0;
				while(true)
				{
                    /*
					nPosition = StringUtil.FindSubstring(strResult,
						strOneWord,
						nStart);
                     * */
                    // 2012/2/20 ��ͼ�޸ģ�δ�����ԣ��ٶ��Ƿ��ܿ��Ҳδ֪
                    nPosition = strResult.IndexOf(strOneWord,
                        nStart,
                        StringComparison.InvariantCultureIgnoreCase);

					if (nPosition<0)
						break;
					nLength = strOneWord.Length;
					nStart += nLength;
					string strStart = strResult.Substring(nPosition-1,1);
					string strEnd = strResult.Substring(nPosition+nLength,1);
					string strStopwordStart = strOneWord.Substring(0,1);
					string strStopwordEnd = strOneWord.Substring(strOneWord.Length-1);

					if (((strStart == "^") 
						|| (StringUtil.IsChineseChar(strStart) == true) 
						|| (StringUtil.IsChineseChar(strStopwordStart) == true))  && ((strEnd == "^") 
						|| (StringUtil.IsChineseChar(strEnd) == true) 
						|| (StringUtil.IsChineseChar(strStopwordEnd) == true)) )
					{
						strResult = strResult.Remove(nPosition,nLength);

                        // 2013/7/25
                        if (nStart >= nPosition && nStart < nPosition + nLength)
                            nStart = nPosition;
					}
				}
			}
			strResult = strResult.Replace("^","");
			return strResult;
		}

		public int IsInStopword(string strSplitChar,
			string strStopwordTableName,
			out bool bInStopword,
			out string strError)
		{
			bInStopword = false;
			strError = "";

			StopwordTable stopwordTable = (StopwordTable)this.tableStopwordTable[strStopwordTableName];
			if (stopwordTable == null)
			{
				strError = "û�ҵ���Ϊ'" + strStopwordTableName + "'��<stopwordTable>Ԫ�ء�";
				return -1;
			}

			foreach(string strSep in stopwordTable.aSeparator)
			{
				if (strSep == strSplitChar)
				{
					bInStopword = true;
					return 0;
				}
			}

			foreach(string strWord in stopwordTable.aWord)
			{
				if (strWord == strSplitChar)
				{
					bInStopword = true;
					return 0;
				}
			}
			return 0;
		}

		
	}

	public class StopwordTable
	{
		public string Name = "";
		public XmlNode m_node = null;
		
		public ArrayList aSeparator = new ArrayList();
		public ArrayList aWord = new ArrayList();

		public StopwordTable(XmlNode node)
		{
			this.Initial(node);

		}

		public void Initial(XmlNode node)
		{
            Debug.Assert(node != null, "Initial()���ô���node����ֵ����Ϊnull��");
			this.m_node = node;
			string strName = DomUtil.GetAttr(node,"name");
			this.Name = strName;


			string strXpath = "";
			
			// ��÷ָ�������
			strXpath = "separator/t";
			XmlNodeList listSeparator =	this.m_node.SelectNodes(strXpath);
			foreach(XmlNode nodeSeparator in listSeparator)
			{
				string strText = nodeSeparator.InnerText.Trim();  // 2012/2/16
				if (string.IsNullOrEmpty(strText) == false)
				{
					if (strText == "_")
						strText = " ";

					this.aSeparator.Add(strText);
				}
			}
	
			// ��÷���������
			strXpath = "word/t";
			XmlNodeList listWord = this.m_node.SelectNodes(strXpath);;
			foreach(XmlNode nodeWord in listWord)
			{
				string strText = nodeWord.InnerText.Trim();  // 2012/2/16
				if (string.IsNullOrEmpty(strText) == false)
					this.aWord.Add(strText);
			}	
		}


	}

}
