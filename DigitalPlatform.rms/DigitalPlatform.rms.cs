using System;
using System.IO;
using System.Collections;
using System.Xml;
using System.Runtime.Serialization;

using DigitalPlatform.Range;
using DigitalPlatform.IO;
using DigitalPlatform.Xml;

namespace DigitalPlatform.rms
{
	public class rmsUtil
	{
#if NO
		// ��Ƭ����(sourceStream)��ȫ�����ݸ���contentrange�ַ��������λ��
		// ��ԭ���Ƶ�Ŀ���ļ�(strOriginFileName)��
		// Ҳ����˵,contentrange�ַ���ʵ���϶�����Ǵ�Ŀ���ļ���ȡ��Ƭ�ϵĹ���
		// ��strContentRange��ֵΪ""ʱ����ʾ���������ļ�
		// paramter:
		//		streamFragment:    Ƭ����
		//		strContentRange:   Ƭ�������ļ��д��ڵ�λ��
		//		strOriginFileName: Ŀ���ļ�
		//		strError:          out ����,return error info
		// return:
		//		-1  ����
		//		>=  ʵ�ʸ��Ƶ��ܳߴ�
		public static long RestoreFragment(
			Stream streamFragment,
			string strContentRange,
			string strOriginFileName,
			out string strErrorInfo)
		{
			long lTotalBytes = 0;
			strErrorInfo = "";

			if (streamFragment.Length == 0)
				return 0;

			// ��ʾ��Χ���ַ���Ϊ�գ�ǡǡ��ʾҪ����ȫ����Χ
			if (strContentRange == "") 
			{
				strContentRange = "0-" + Convert.ToString(streamFragment.Length - 1);
			}

			// ����RangeList��������ⷶΧ�ַ���
			RangeList rl = new RangeList(strContentRange);

			FileStream fileOrigin = File.Open(
				strOriginFileName,
				FileMode.OpenOrCreate, // ԭ����Open�������޸�ΪOpenOrCreate����������ʱ�ļ���ϵͳ����Ա�ֶ�����ɾ��(����xml�ļ�����Ȼ����������)������ܹ���Ӧ��������׳�FileNotFoundException�쳣
				FileAccess.Write,
				FileShare.ReadWrite);


			// ѭ��������ÿ������Ƭ��
			for(int i=0; i<rl.Count; i++) 
			{
				RangeItem ri = (RangeItem)rl[i];

				fileOrigin.Seek(ri.lStart,SeekOrigin.Begin);
				StreamUtil.DumpStream(streamFragment, fileOrigin, ri.lLength);

				lTotalBytes += ri.lLength;
			}

			fileOrigin.Close();
  			return lTotalBytes;
		}
#endif

		// �õ����͵��ַ�������Ϸ���һ���ļ����ַ���
		public static string makeFilePath(string strDir,
			string strPrefix,
			string strFileName)
		{
			string strResult = "";
			strPrefix = strPrefix.Replace("?","_");
			strResult = strDir + "~" + strPrefix + "_" + strFileName;
			return strResult;
		}
	}

	//��ʾ�ļ�Ƭ�ϵ���
	public class FragmentItem
	{
		public string strClientFilePath = "";	// ��������ǰ���ļ���
		public string strContentRange = "";		// ����Ӧ��Ƭ�Ϸ�Χ����
		public string strTempFileName = "";		// ��ʱ�ļ���

		~FragmentItem()
		{
			DeleteTempFile();
		}


		// ɾ����ʱ�ļ�
		public void DeleteTempFile()
		{
			if (strTempFileName != "") 
			{
				File.Delete(strTempFileName);
				strTempFileName = "";
			}
		}


		public long Copy(out string strErrorInfo)
		{
			if (strClientFilePath == "") 
			{
				strErrorInfo = "strClientFilePath����Ϊ��...";
				return -1;
			}
			if (strTempFileName == "") 
			{
				// �����ʱ�ļ���
				strTempFileName = Path.GetTempFileName();
			}

			//MessageBox.Show ("��ʱ�ļ���:"+strTempFileName);
			return RangeList.CopyFragment(
				strClientFilePath,
				strContentRange,
				strTempFileName,
				out strErrorInfo);
		}


		//�õ���ʱ�ļ��ĳ���
		public long GetTempFileLength()
		{
			if (strTempFileName == "")
				return -1;
			FileInfo fi = new FileInfo(strTempFileName);
			return fi.Length;
		}


		// ��ñ�Ƭ�ϵ��ܳߴ�
		public long lengthOf()
		{
			if (strContentRange == "") 
			{
				if (strClientFilePath == "")
					return -1;	// ��ʾ�Ƿ�ֵ
				FileInfo fi = new FileInfo(strClientFilePath);
				return fi.Length;
			}
			return lengthOf(strContentRange);
		}


		// ��һ��contentrange�ַ�������Ϊ�ܳߴ�
		public static long lengthOf(string strContentRange)
		{
			long lTotalBytes = 0;

			// ����RangeList��������ⷶΧ�ַ���
			RangeList rl = new RangeList(strContentRange);
			// ѭ��������ÿ������Ƭ��
			for(int i=0; i<rl.Count; i++) 
			{
				RangeItem ri = (RangeItem)rl[i];

				lTotalBytes += ri.lLength;
			}
			return lTotalBytes;
		}
	} 



	//FragmentItem�ļ���
	public class FragmentList : ArrayList 
	{
		//����һ���µ�FragmentItem���󣬲����뼯��
		//�������strClientFilePath��ContentRange�����ͼ������Ѿ����ڵ�Item��ͬ���򷵻ش���
		//strClientFilePath: �ļ���
		//ContentRange: ��Χ
		//bCreateTempFile: �Ƿ�����������ʱ�ļ�
		//strErrorInfo: ������Ϣ
		public FragmentItem newItem(string strClientFilePath,
			string strContentRange,
			bool bCreateTempFile,
			out string strErrorInfo)
		{
			strErrorInfo = "";

			FragmentItem fi = new FragmentItem();

			fi.strClientFilePath = strClientFilePath;
			fi.strContentRange = strContentRange;
			if (bCreateTempFile == true)
			{
				long ret = fi.Copy(out strErrorInfo);
				if (ret == -1)
					return null;
			}

			this.Add(fi);

			return fi;
		}
	} 


	//FileNameHolder����Ķ��������ΪFileNameItem
	public class FileNameHolder:ArrayList
	{
		//��ʱĿ¼��ַ
		public string m_strDir;           

		//ǰ׺������ΪsessionID + recordID
		public string m_strPrefix;
  
		//������Ϣ
		public string strFileNameHolderInfo = "";

		//����Dir���ԣ���ʱĿ¼��ַ
		public string Dir
		{
			get
			{
				return m_strDir;
			}
			set
			{
				m_strDir = value;
			}
		}

		//����Prefix���ԣ���ʾǰ׺
		public string Prefix
		{
			get
			{
				return m_strPrefix;
			}
			set
			{
				m_strPrefix = value;
			}
		}

		//LeaveFiles����Clear()����������ж���
		//��ô������ʱ�Ͳ���ɾ�������ˣ�������Ȩ����xmledit.
		public void LeaveFiles()
		{
			Clear();
		}

		//������ɾ�����еĶ�������ռ��ϡ�
		//�ɹ�����0
		public int DeleteAllFiles()
		{
			foreach(FileNameItem objFileName in this)
			{
				try
				{
					File.Delete(m_strDir + "~" + m_strPrefix + "_" + objFileName.FileName);
				}
				catch (Exception ex)
				{
					//Exception ex = new Exception("��DeleteAllFiles��ɾ����" + objFileName.FileName + "�ļ�ʧ��");
					throw(ex);
					//strFileNameHolderInfo +="��DeleteAllFiles��ɾ����"+objFileName.FileName+"�ļ�ʧ��";
				}
			}
			Clear();
			return 0;
		}

		//�г����ϵ��е�������
		//����ַ���
		public string Dump()
		{
			string strResult = "";
			strResult += "<table border='1'><tr><td>�ļ���</td></tr>";

			foreach(FileNameItem objFileName in this)
			{
				strResult += "<tr><td>" + m_strDir + "~" + m_strPrefix + "_" + objFileName.FileName + "</td></tr>";
			}		
			strResult += "</table>";

			return strResult;
		}

		//��������ɾ�����ж���
		~FileNameHolder()
		{
			DeleteAllFiles();
		}
	} //FileNameHolder�����


	//FileNameHolder�ĳ�Ա����
	public class FileNameItem
	{
		//�ļ�����
		private string m_strFileName;

		//�ļ�����
		private string m_strContentType;

		//���캯������һ������:strFileName,��ʾ�����ļ�������ֵ��m_strFileName
		//strFileName: �ļ���
		public FileNameItem(string strFileName)
		{
			m_strFileName = strFileName;
		}

		//�ļ���
		public string FileName
		{
			get
			{
				return m_strFileName;
			}
		}

		//�ļ�����
		public string ContentType
		{
			get
			{
				return m_strContentType;
			}
			set
			{
				m_strContentType = value;
			}
		}
	} //FileNameItem�����


	// �����ͼ:Ϊ�˴���"���ݿ���:��¼ID"�Լ�ID������Ƶ�DbPath��
	public class DbPath
	{
		//˽�г�Ա�ֶΣ�������ݿ�����
		private string m_strName = "";
		
		//˽�г�Ա�ֶΣ���ż�¼ID
		private string m_strID = "";

		//���캯��:��������߼�ID���Ϊ�����֣����ݿ����ͼ�¼ID���ֱ�ֵ��m_strName��m_strID
		//strDpPath: ����������ʽ
		public DbPath(string strDbPath)
		{
			int nPosition = strDbPath.LastIndexOf ("/"); //:
			//ֻ������/��ʱ��ֻ��һ������ID
			if (nPosition < 0)
			{
//				m_strID = strDbPath;
				this.m_strName = strDbPath;
				return;
			}
			m_strName = strDbPath.Substring(0,nPosition);
			m_strID = strDbPath.Substring(nPosition+1);
		}

		//����Name���ԣ���ʾ���ݿ������ṩ���ⲿ�������
		public string Name
		{
			get
			{
				return m_strName;
			}
			set
			{
				m_strName=value;
			}
		}

		//����ID���ԣ���ʾ��¼ID���ṩ���ⲿ�������
		public string ID
		{
			get
			{
				return m_strID;
			}
			set
			{
				m_strID = value;
			}
		}

		//����Path���ԣ���ʾ�����߼�ID�����������ͼ�¼ID����ֻ��
		public string Path
		{
			get
			{
				return m_strName + "/" + ID10; //:
			}
		}

		//����ID10���ԣ�����һ��10λ���ȵļ�¼ID
		public string ID10
		{
			get
			{
				return this.m_strID.PadLeft(10,'0');
/*
				if (m_strID.Length < 10)
				{
					string strAdd = new string('0',10-m_strID.Length);
					return strAdd + m_strID;
				}
				
				return m_strID;
*/				
			}
		}


		// ȷ�� ID �ַ���Ϊ 10 λ������̬
		public static string GetID10(string strID)
		{
			return strID.PadLeft(10, '0');	
		}

		// ���ȥ����ǰ�� '0' �Ķ̺��� ID �ַ���
		public string CompressedID
		{
			get
			{
                return GetCompressedID(m_strID);

				// return m_strID.TrimStart(new char[]{'0'});
/*
				string strTemp = m_strID;
				while(strTemp.Substring(0,1) == "0")
				{
					strTemp = strTemp.Substring(1);
				}
				return strTemp;
*/				
			}
		}

		public static string GetCompressedID(string strID)
		{
			return strID.TrimStart(new char[]{'0'});
/*
			while(strID.Substring(0,1) == "0")
			{
				strID = strID.Substring(1);
			}
			return strID;
*/			
		}
	}

    public class LogicNameItem
    {
        [DataMember]
        public string Lang = "";
        [DataMember]
        public string Value = "";
    }
}
