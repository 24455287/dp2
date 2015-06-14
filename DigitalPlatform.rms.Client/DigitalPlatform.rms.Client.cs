using System;
using System.Windows .Forms ;
using System.Drawing ;
using System.Xml ;
using System.Collections ;
using System.Threading ;
using System.Net ;
using System.IO ;

using DigitalPlatform.Xml  ;
using DigitalPlatform.Text  ;
using DigitalPlatform.IO ;
using DigitalPlatform.Range  ;
using DigitalPlatform.rms ;

namespace DigitalPlatform.rms.Client
{

	public class FileItem
	{
		public string strClientPath = null;
		public string strItemPath = null;
		public string strFileNo = null;
	}

	//�������б�
	public class HostList:ArrayList
	{
		public string m_strFileName;	// XML�ļ���

		public ReaderWriterLock m_lock = new ReaderWriterLock();
		public static int m_nLockTimeout = 5000;	// 5000=5��

		public HostList(string strFileName)
		{
			m_strFileName = strFileName;
			Load(strFileName);
		}

		// ����hosturl�ҵ�Host����
		public HostItem GetHost(string strHostUrl)
		{
			// �Ա�listҪ���ж�ȡ�Ĳ���
			m_lock.AcquireReaderLock(m_nLockTimeout);

			try 
			{
				for(int i=0;i<this.Count;i++) 
				{
					HostItem obj = (HostItem)this[i];
					if (obj.m_strHostURL == strHostUrl)
						return obj;
				}
			}
			finally 
			{
				m_lock.ReleaseReaderLock();
			}
			return null;	// not found
		}


		// ����hosturl�ҵ�Host����
		public HostItem NewHost(string strHostUrl)
		{
			if (strHostUrl == "")
				return null;
			// �Ա�listҪ���ж�ȡ�Ĳ���
			m_lock.AcquireWriterLock(m_nLockTimeout);

			try 
			{
				for(int i=0;i<this.Count;i++) 
				{
					HostItem obj = (HostItem)this[i];
					if (obj.m_strHostURL == strHostUrl)
						return obj;	// �Ѿ�����
				}

				HostItem newhost = new HostItem();
				newhost.m_strHostURL = strHostUrl;
				this.Add(newhost);

				return newhost;
			}
			finally 
			{
				m_lock.ReleaseWriterLock();
			}
		}


		public HostList()
		{}

		//���ݸ��ڵ㣬����HostList
		public static HostList CreateBy(XmlNode nodeRoot)
		{
			HostList hostlistObj = new HostList();
			XmlNodeList nodes = nodeRoot.SelectNodes("host");

			// ��listҪ���в���Ԫ�صĲ���
			hostlistObj.m_lock.AcquireWriterLock(m_nLockTimeout);

			try 
			{
				for(int i=0;i<nodes.Count;i++) 
				{
					HostItem hostObj = HostItem.CreateBy(nodes[i]);
					hostlistObj.Add(hostObj);
				}
			}
			finally 
			{
				hostlistObj.m_lock.ReleaseWriterLock();
			}

			return hostlistObj;
		}

		
		public string GetXml()
		{
			string strCode = "";

			strCode = "<root";
			strCode += " >";
			// �����¼�

			// �Ա�listҪ���ж�ȡ�Ĳ���
			m_lock.AcquireReaderLock(m_nLockTimeout);
			try 
			{
				for(int i=0;i<this.Count;i++) 
				{
					strCode += ((HostItem)this[i]).GetXml();
				}
			}
			finally
			{
				m_lock.ReleaseReaderLock();
			}

			strCode += "</root>";
			return strCode;
		}


		~HostList()
		{
			if (m_strFileName != "") 
			{				
				Save(m_strFileName);
			}
		}


		//��XML�ļ���װ��ȫ����Ϣ
		//strFileName: XmlL�ļ���
		public void Load(string strFileName)
		{
			XmlDocument dom = new XmlDocument();
			dom.Load(strFileName);
			// ��<root>�����<user>Ԫ�س�ʼ��User����
			CreateBy(dom.DocumentElement);
		}


		//���ⲿ���ı��浽�ļ��ĺ���
		public void Save()
		{
			this.Save(this.m_strFileName );
		}

		// ����ȫ����Ϣ��xml�ļ�
		private void Save(string strFileName)
		{
			if (strFileName == null)
				return;
			if (strFileName == "")
				return ;
			string strCode = "";

			strCode = "<?xml version='1.0' encoding='utf-8' ?>"
				+ GetXml();

			StreamWriter sw = new StreamWriter(strFileName, 
				false,	// overwrite
				System.Text.Encoding.UTF8);
			sw.Write(strCode);
			sw.Close();
		}
	}


	// ����������
	public class HostItem
	{
		public string m_strHostURL;
		public CookieContainer Cookies = new System.Net.CookieContainer();
		
		//����node����������
		public static HostItem CreateBy (XmlNode node)
		{
			HostItem newHost = new HostItem ();
			newHost.m_strHostURL = DomUtil.GetAttr (node,"name");
			return newHost;
		}

		
		//�õ�AttrXml�ַ���
		public string GetAttrXml()
		{
			string strCode;
			strCode = " name=\"" + m_strHostURL + "\"";
			return strCode;
		}


		//�õ�Xml
		public string GetXml()
		{
			string strCode = "";

			strCode = "<host";
			strCode += GetAttrXml();
			strCode += " >";
			strCode += "</host>";
			return strCode;
		}
	}


	// ��ѯʽ����
	public class QueryClient
	{
	
		// �м��ʽ����ʽ ת��Ϊ dprmsϵͳ�����XML����ʽ
		// ��ν�м��ʽ������ target|query word
		public static string ProcessQuery2Xml(
			string strQuery,
			string strLanguage)
		{
			if (strQuery == "")
				return "";

			string strTarget = "";
			string strAllWord = "";
			int nPosition = strQuery.IndexOf("|");
			if (nPosition >= 0)
			{
				strAllWord = strQuery.Substring(0,nPosition);
				strTarget = strQuery.Substring(nPosition+1);
			}
			else
			{
				strTarget = strQuery;
			}
	
			string[] aWord;
			aWord = strAllWord.Split(new Char [] {' '});
	
			if (aWord == null)
				aWord[0] = strAllWord;
	
			string strXml = "";
			string strWord;
			string strMatch;
			string strRelation;
			string strDataType;	
			foreach(string strOneWord in aWord)
			{
				if (strXml != "")
					strXml += "<operator value='OR'/>";
				string strID1;
				string strID2;
				SplitRangeID(strOneWord,out strID1, out strID2);
				if (StringUtil.IsNum(strID1)==true 
					&& StringUtil.IsNum(strID2) && strOneWord!="")
				{
					strWord = strOneWord;
					strMatch = "exact";
                    strRelation = "range";  // 2012/3/29
					strDataType = "number";
				}
				else
				{
					string strOperatorTemp;
					string strRealText;
				
					int ret;
					ret = GetPartCondition(strOneWord, out strOperatorTemp,out strRealText);
				
					if (ret == 0 && strOneWord!="")
					{
						strWord = strRealText;
						strMatch = "exact";
						strRelation = strOperatorTemp;
						if(StringUtil.IsNum(strRealText) == true)
							strDataType = "number";					
						else
							strDataType = "string";
					}
					else
					{
						strWord = strOneWord;
						strMatch = "left";
						strRelation = "=";
						strDataType = "string";					
					}
				}

                // 2007/4/5 ���� ������ GetXmlStringSimple()
				strXml += "<item><word>"
                    +StringUtil.GetXmlStringSimple(strWord)+
					"</word><match>"+strMatch+
					"</match><relation>"+strRelation+
					"</relation><dataType>"+strDataType+
					"</dataType></item>";
			}
			if (strLanguage == "")
				MessageBox.Show ("������ѡ�У���ôΪ���أ�");
			strXml = "<target list='"
                + StringUtil.GetXmlStringSimple(strTarget)     // 2007/9/14
                +"'>"+strXml+"<lang>"+strLanguage+"</lang></target>";
			return strXml;
		}

		
		//��"***-***"��ֳ�������
		public static int SplitRangeID(string strRange ,
			out string strID1, 
			out string strID2)
		{
			int nPosition;
			nPosition = strRange.IndexOf("-");
			strID1 = "";
			strID2 = "";
			if (nPosition > 0)
			{
				strID1 = strRange.Substring(0,nPosition).Trim();
				strID2 = strRange.Substring(nPosition+1).Trim();
				if (strID2 == "")
					strID2 = "9999999999";
			}
			if (nPosition == 0)
			{
				strID1 = "0";
				strID2 = strRange.Substring(1).Trim();
			}
			if (nPosition < 0)
			{
				strID1 = strRange.Trim();
				strID2 = strRange.Trim();
			}
			return 0;
		}


		// ���ݱ�ʾʽ���õ���������ֵ
		// return:
		//		0	�й�ϵ������
		//		-1	�޹�ϵ������				
		public static int GetPartCondition(string strText,
			out string strOperator,
			out string strRealText)
		{
			strText = strText.Trim();
			strOperator = "=";
			strRealText = strText;
			int nPosition;
			nPosition = strText.IndexOf(">=");
			if(nPosition >= 0)
			{
				strRealText = strText.Substring(nPosition+2);

				strOperator = ">=";
				return 0;
			}
			nPosition = strText.IndexOf("<=");
			if(nPosition >= 0)
			{
				strRealText = strText.Substring(nPosition+2);
				strOperator = "<=";
				return 0;
			}
			nPosition = strText.IndexOf("<>");
			if(nPosition >= 0)
			{
				strRealText = strText.Substring(nPosition+2);
				strOperator = "<>";
				return 0;
			}

			nPosition = strText.IndexOf("><");
			if(nPosition >= 0)
			{
				strRealText = strText.Substring(nPosition+2);
				strOperator = "<>";
				return 0;
			}
			nPosition = strText.IndexOf("!=");
			if(nPosition >= 0)
			{
				strRealText = strText.Substring(nPosition+2);
				strOperator = "<>";
				return 0;
			}
			nPosition = strText.IndexOf(">");
			int nPosition2 = strText.IndexOf(">=");
			if(nPosition2<0 && nPosition >= 0)
			{
				strRealText = strText.Substring(nPosition+1);
				strOperator = ">";
				return 0;
			}
			nPosition = strText.IndexOf("<");
			nPosition2 = strText.IndexOf("<=");
			if(nPosition2<0 && nPosition >= 0)
			{
				strRealText = strText.Substring(nPosition+1);
				strOperator = "<";
				return 0;
			}
			return -1;
		}


		//��������ϳ�XML��һ��
		public static int combination(
			string strTarget,
			string strWord,
			string strMatch,
			string strRelation,
			string strDataType,
			string strOperator,
			ref string strXml)
		{
			if (strXml!="")  // && (i != nLine-1) && (i != 0))
			{
				strXml += "<operator value='"+strOperator+"'/>";
			}
			strXml += "<target list='"+
                StringUtil.GetXmlStringSimple(strTarget)     // 2007/9/14
                + "'>" + 
				"<item>" +
				"<word>" + StringUtil.GetXmlStringSimple(strWord) 
                +"</word>"+
				"<match>" + strMatch +"</match>"+
				"<relation>" + strRelation +"</relation>"+
				"<dataType>" + strDataType +"</dataType>" +
				"</item>" +
				"</target>";
			return 0;
		}
	}



	public class ClientUtil
	{
/*
		public static int DaBagFileAdded(string strXmlText,
			ArrayList aFileName,
			Stream target,
			out string strInfo)
		{
			strInfo = "";

			XmlDocument dom = new XmlDocument ();
			try
			{
				dom.LoadXml(strXmlText);
			}
			catch(Exception ex )
			{
				strInfo += "���Ϸ���XML\r\n"+ex.Message ;
				return -1;
			}

			XmlNodeList listFile = dom.SelectNodes("//file");
			

			//�õ������ļ���
			int nMaxFileNo = 0;
			foreach(XmlNode node in listFile)
			{
				string strFileNo = DomUtil.GetNodeText (node);
				int nPosition = strFileNo.IndexOf (".");
				if (nPosition > 0)
				{
					int nTempNo = Convert.ToInt32 (strFileNo.Substring (0,nPosition));
					if (nTempNo > nMaxFileNo)
						nMaxFileNo = nTempNo;
				}
			}

			//�Ӵ�һ��
			nMaxFileNo ++;

			//ƴ��ÿ���ļ���ID�������浽xml��
			ArrayList aFileID = new ArrayList ();
			for(int i=0;i<aFileName.Count ;i++)
			{
				string strFileName = (string)aFileName[i];
				string strExtention = Path.GetExtension (strFileName);
				string strFileID = Convert.ToString (nMaxFileNo++)+strExtention;
				//�ȸ�xml�����
				XmlNode nodeFile = listFile[i];
				DomUtil.SetNodeText (nodeFile,strFileID);
				//�ӵ�aFileID������ڴ��ʱ�õ�
				aFileID.Add (strFileID);
			}

			long lTotalLength = 0;  //�����ܳ��ȣ���������ͷ��8���ֽ�
			//�����ֽ�����
			byte[] bufferLength = new byte[8];

			//��סд�ܳ��ȵ�λ��
			long lPositon = target.Position ;

			//1.��ͷ�ճ�8�ֽڣ����д�ܳ���*****************
			target.Write(bufferLength,0,8);

			//2.дXMl�ļ�*******************
			MemoryStream ms = new MemoryStream ();
			dom.Save (ms);

			//���ַ���ת�����ַ�����
			//byte[] bufferXmlText = System.Text.Encoding.UTF8.GetBytes(strXmlText);
			
			//���XML�ļ����ֽ���
			long lXmlLength = ms.Length  ;//(long)bufferXmlText.Length;
			bufferLength =	System.BitConverter.GetBytes(lXmlLength);
			
			target.Write(bufferLength,0,8);
			lTotalLength += 8;

			//target.Write (bufferXmlText,0,lXmlLength);
			ms.Seek (0,SeekOrigin.Begin );
			StreamUtil.DumpStream (ms,target);
			lTotalLength += lXmlLength ;

			//3.д�ļ�
			long lFileLengthTotal = 0;  //ȫ���ļ��ĳ���,Ҳ���Լ�����lTotalLength����������һ������������������С
			for(int i=0;i<aFileName.Count ;i++)
			{
				FileStream streamFile = File.Open ((string)aFileName[i],FileMode.Open);
				WriteFile(streamFile,
					(string)aFileID[i],
					target,
					ref lFileLengthTotal);
				streamFile.Close ();
			}
			lTotalLength += lFileLengthTotal;

			//4.д�ܳ���
			bufferLength = System.BitConverter.GetBytes(lTotalLength);
			target.Seek (lPositon,SeekOrigin.Begin);
			target.Write (bufferLength,0,8);

			//��ָ���Ƶ����
			target.Seek (0,SeekOrigin.End);
			return 0;
		}
*/



		//��һ����¼�������Ķ����Դ�ļ����
		//Ӧ��֤�ⲿ��target��λ�ö���
		//0:�ɹ�
		//-1:����
		public static int DaBag(string strXmlText,
			ArrayList aFileItem,
			Stream target,
			out string strInfo)
		{
			strInfo = "";

			XmlDocument dom = new XmlDocument ();
			try
			{
				dom.LoadXml(strXmlText);
			}
			catch(Exception ex )
			{
				strInfo += "���Ϸ���XML\r\n"+ex.Message ;
				return -1;
			}

			long lTotalLength = 0;  //�����ܳ��ȣ���������ͷ��8���ֽ�
			//�����ֽ�����
			byte[] bufferLength = new byte[8];

			//��סд�ܳ��ȵ�λ��
			long lPositon = target.Position ;

			//1.��ͷ�ճ�8�ֽڣ����д�ܳ���*****************
			target.Write(bufferLength,0,8);

			//2.дXMl�ļ�*******************
			MemoryStream ms = new MemoryStream ();
			dom.Save (ms);

			//���ַ���ת�����ַ�����
			//byte[] bufferXmlText = System.Text.Encoding.UTF8.GetBytes(strXmlText);
			
			//���XML�ļ����ֽ���
			long lXmlLength = ms.Length  ;//(long)bufferXmlText.Length;
			bufferLength =	System.BitConverter.GetBytes(lXmlLength);
			
			target.Write(bufferLength,0,8);
			lTotalLength += 8;

			//target.Write (bufferXmlText,0,lXmlLength);
			ms.Seek (0,SeekOrigin.Begin );
			StreamUtil.DumpStream (ms,target);
			lTotalLength += lXmlLength ;

			//3.д�ļ�
			long lFileLengthTotal = 0;  //ȫ���ļ��ĳ���,Ҳ���Լ�����lTotalLength����������һ������������������С
			
			FileItem fileItem = null;
			for(int i=0;i<aFileItem.Count ;i++)
			{
				fileItem = (FileItem)aFileItem[i];
				//MessageBox.Show (fileItem.strClientPath + " --- " + fileItem.strItemPath + " --- " + fileItem.strFileNo  );
			
				FileStream streamFile = File.Open (fileItem.strClientPath ,FileMode.Open);
				WriteFile(streamFile,
					fileItem.strFileNo ,
					target,
					ref lFileLengthTotal);
				streamFile.Close ();
			}

			lTotalLength += lFileLengthTotal;

			//4.д�ܳ���
			bufferLength = System.BitConverter.GetBytes(lTotalLength);
			target.Seek (lPositon,SeekOrigin.Begin);
			target.Write (bufferLength,0,8);

			//��ָ���Ƶ����
			target.Seek (0,SeekOrigin.End);
			return 0;
		}



		//д���ļ�����	ע.�ⲿ��֤��λ���ƺ�
		//source: ��������
		//strID: ��¼ID
		//target: Ŀ����
		//lFileLengthTotal: �ļ��ܳ���
		//0: �����õ��ļ����� -1:�ļ���Ϊ��
		public static int WriteFile(Stream source,
			string strID,
			Stream target,
			ref long lFileLengthTotal)
		{
			long lTotalLength = 0;  //�ܳ���
			//�����ֽ�����
			byte[] bufferLength = new byte[8];

			//��סд�ܳ��ȵ�λ��
			long lPosition = target.Position ;

			//1.��ͷ�ճ�8�ֽڣ����д�ܳ���*****************
			target.Write(bufferLength,0,8);

			//2.��д�����ַ����ĳ���;
			//���ַ���ת�����ַ�����
			byte[] bufferID = System.Text.Encoding.UTF8 .GetBytes(strID);
			bufferLength = System.BitConverter.GetBytes((long)bufferID.Length);
			target.Write (bufferLength,0,8);
			lTotalLength += 8;

			//3.д�����ַ���
			target.Write (bufferID,
				0,
				bufferID.Length );
			lTotalLength += bufferID.Length;

			//4.д�������ļ�
			bufferLength = System.BitConverter.GetBytes(source.Length);
			//�������ļ��ĳ���;
			target.Write (bufferLength,0,8);
			lTotalLength += 8;
			//д�������ļ�����
			source.Seek (0,SeekOrigin.Begin);
			StreamUtil.DumpStream (source,
				target);
			lTotalLength += source.Length ;


			//5.���ؿ�ͷд�ܳ���
			bufferLength =	System.BitConverter.GetBytes(lTotalLength);
			target.Seek (lPosition,SeekOrigin.Begin);
			target.Write (bufferLength,0,8);

			//��ָ���Ƶ����
			target.Seek (0,SeekOrigin.End);
			lFileLengthTotal += (lTotalLength+8);
			return 0;
		}



		/*
		//��һ�����ļ��ֳɷ�Χ����
		public static int SplitFile2FragmentList(string strClientFilePath,
			FragmentList fragmentList)
		{
			FileInfo fi = new FileInfo(strClientFilePath);
			string[] aRange = null;
			string strErrorInfo;

			if (fi.Length == 0)
				return -1;

			int nPackageMaxSize = 500*1024;
			if (fi.Length > nPackageMaxSize) 
			{
				string strRangeWhole = "0-" + Convert.ToString(fi.Length-1);
				aRange = RangeList.ChunkRange(strRangeWhole, nPackageMaxSize);
			}

			if (aRange != null) 
			{
				for(long i=0; i<aRange.Length; i++) 
				{
					string strContentRange = aRange[i];

					FragmentItem fragmentItem = fragmentList.newItem(
						strClientFilePath,
						strContentRange,
						false,    //�Ƿ�����������ʱ�ļ�? true:��ʾ����������ʱ�ļ�;false:������,���鲻Ҫ���̸��ƣ��ȵ�����ʱ�ٸ���
						out strErrorInfo);

					if (fragmentItem == null)
						return -1 ;
				}
			}
			else 
			{
				// �ļ��ߴ�û�г������ߴ�����
				FragmentItem fragmentItem = fragmentList.newItem(
					strClientFilePath,
					"",	// ���ַ�����ʾ�ļ���ȫ�����ݽ���
					false,
					out strErrorInfo);
				if (fragmentItem == null)
					return -1;
			}
			return 0;
		}
		*/

	}

}
