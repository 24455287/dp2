using System;
using System.Collections;
using System.Diagnostics;

using System.Text;

namespace DigitalPlatform.DTLP
{

	// ͨѶ���е�һ����Ԫ
	public class Cell
	{
		public string	Path = null;	// m_strPath ǰ�����ֺ�ö�ٲ��ֵ����
		public string	Lead = null;	// m_strLead ǰ������
		public string	Content = null;	// m_strContent ö�ٲ���
		public byte []	ContentBytes = null;	// m_baContent
		// public int		m_nContentCharset;
		public Int32	Mask = 0;		// m_lMask
	}

	public enum PackageFormat 
	{
		String = 1,
		Binary = 2,
	}

	/// <summary>
	/// ����ͨѶ������
	/// </summary>
	public class Package : ArrayList
	{

		byte[]	m_baPackage;	// ͨѶ��

		public string	ContinueString = "";	// m_strNext
		public byte[]	ContinueBytes = null;	// m_baNext Ϊ����dt1000/dt1500 ansi�汾���ںˣ������������

		Encoding	m_encoding	= Encoding.GetEncoding(936);

		public Package()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		// װ�ذ�����
		public int LoadPackage(byte [] baPackage, 
			Encoding encoding)
		{
			Int32 lPackageLen;

			m_baPackage = null;

			if (baPackage == null) 
			{
				Debug.Assert(false,
					"baPackage��������Ϊnull");
				return -1;
			}

			if (baPackage.Length < 4) 
			{
				Debug.Assert(false,
					"baPackage���ݳߴ�С��4������ȷ");
				return -1;
			}

			lPackageLen =  BitConverter.ToInt32(baPackage, 0);
			if (lPackageLen < 0) 
			{
				Debug.Assert(false,
					"baPackage���ݳߴ�С��0������ȷ");
				m_baPackage = null;
				return -1;
			}

			if ( baPackage.Length < lPackageLen) 
			{
				// ͨѶ����ʽ�������ش���ͷ������ĳ��ȴ���ʵ�ʳ���
				Debug.Assert(false,
					"ͨѶ����ʽ�������ش���ͷ������ĳ��ȴ���ʵ�ʳ���");
				m_baPackage = null;
				return -1;
			}

			m_baPackage = new byte [lPackageLen];
			Array.Copy(baPackage, 0, m_baPackage, 0, lPackageLen);

			m_encoding = encoding;

			return 0;
		}

		// ����ansi�ַ����ַ����ĳ���
		public static int strlen(byte [] baContent, int nOffs)
		{
			int nResult = 0;
			for(int i=nOffs;i<baContent.Length;i++) 
			{
				if (baContent[i] == 0)
					return nResult;
				nResult ++;
			}

			return nResult;
		}



		// ��ͨѶ��ת��Ϊ���ڴ�����и�ʽ������m_LineArray��
		// ע�Ȿ��������LoadPackage()������ʹ�á�
		// ���������Ӱ��е����ݵ���C�����ַ���������һ���Ӱ����Ϊ���ɸ��ַ�����
		// ����-1��ʾʧ�ܡ�
		public int Parse(PackageFormat format)
		{
			Int32 lPackageLen;

			Int32 lBaoLen;
			Int32 lPathLen;
			int i;
			int wholelen,len;
			//char far *lpBao;
			//char far *lpPath;
			int j;
			Int32 lMask;
	
			// char *src = (char *)m_baPackage.GetData();

			Cell cell = null;
			int nOffs = 0;
			//byte [] baPath = null;
			//byte [] baBao = null;
			int nPathStart = -1;
			int nBaoStart = -1;
	
			this.Clear();
			ContinueString = "";
			ContinueBytes = null;

			lPackageLen =  BitConverter.ToInt32(m_baPackage, 0);

			Debug.Assert( lPackageLen == m_baPackage.Length,
				"��ͷ���ߴ粻��ȷ");
	
//			pp = src + 4;
			nOffs += 4;
			Int32 lWholeLen = 4;
			for(i=0;;i++) 
			{
				lPathLen = BitConverter.ToInt32(m_baPackage, nOffs);

				Debug.Assert(lPathLen < 1000, "lPathLen����ȷ");

				// lpPath = pp + 4;
				nPathStart = nOffs + 4;
		
				nOffs += lPathLen;

				lWholeLen += lPathLen;

				if (lWholeLen >= lPackageLen)
					break;

				if (lWholeLen == lPackageLen) 
				{
					// û��ö�ٲ���
					lBaoLen = 0;
					lMask = 0;
				}
				else 
				{
					lBaoLen = BitConverter.ToInt32(m_baPackage, nOffs);
					lMask = BitConverter.ToInt32(m_baPackage, nOffs + 8);
				}

				if ((lMask & DtlpChannel.TypeBreakPoint) != 0) 
				{
					if (lBaoLen > 12) 
					{
						// lpBao = pp+12;

						len = strlen(m_baPackage, nOffs + 12) + 1;

						ContinueString = Encoding.GetEncoding(936).GetString(m_baPackage, nOffs+12, len-1);

						// ����ģ��
						ContinueBytes = new byte[len-1];
						Array.Copy(m_baPackage, nOffs+12, ContinueBytes, 0, len -1);

					}
					goto SKIP;
				}
		
				//lpBao = pp+12;  // 8
				if (lWholeLen == lPackageLen) 
					nBaoStart = -1;
				else 
					nBaoStart = nOffs + 12;

				// ��ͨѶ��ת��Ϊ���Ӱ�Ϊ��Ԫ�ĸ�ʽ������m_LineArray��
				// �����������Ӱ��е����ݵ���C�����ַ�������
				// ע��! pCell->ContentBytes���ַ���δ��ת��(������Ա�Ѿ�ת��)�����������¼�����ʽ��
				//	1)C�����ַ�����ʽ��DBCS/UTF8�ַ�����
				//	2)MARC��¼��ǰ��9�ֽ�Ϊ���������ݣ�����ΪC�����ַ������������Ͳ��ܰ�����ContentBytes
				//		����һ���ַ������з��룬��Ϊǰ��9�ֽ��м���ܰ���0�ַ����������ַ�����ֹ��
				//		ʱ�����MARC��¼��һ����÷����ڿ�����һ����ܱʡ�
				// ����-1��ʾʧ�ܡ�

				if (format == PackageFormat.Binary) 
				{
					// lMask
					cell = new Cell();

					this.Add(cell);

					cell.Mask = lMask;
			
					if (lPathLen > 0 && m_baPackage[nPathStart] != 0) 
					{
						Debug.Assert(nPathStart!=-1,
							"nPathStart��δ��ʼ��");

						Debug.Assert(strlen(m_baPackage, nPathStart) == lPathLen-4-1,
							"lPathLenֵ����ȷ");

						cell.Path += m_encoding.GetString(m_baPackage, nPathStart, lPathLen-4-1);
						cell.Lead += m_encoding.GetString(m_baPackage, nPathStart, lPathLen-4-1);

						/*
						// ���һ���ַ�����'/'������ΪUTF8�ַ����������Կ���ʹ��DBCS�жϷ���
						if (cell.Path.Length !=0
							&& cell.Path[Math.Max(0, cell.Path.Length-1)] != '/' )
							cell.Path += "/";
						*/

					}


					if (lWholeLen == lPackageLen) 
						break;

					if (lBaoLen <= 12)
						goto SKIP;

					// ö�ٲ��֣�����һ������
					Debug.Assert( lBaoLen >= 12 ,
						"lBaoLen����ȷ");

					if (lBaoLen > 12) // ����== 12������
					{
						cell.ContentBytes  = new byte[lBaoLen - 12];

						Array.Copy(m_baPackage, nBaoStart, 
							cell.ContentBytes, 0, lBaoLen - 12);
					}

				}

				// nOffs += 12;

				if (lBaoLen <= 12)	
					goto SKIP;
		
				if (format == PackageFormat.String) 
				{
					for(len=0,wholelen=0,j=0;;j++) 
					{
			
						// lMask
						cell = new Cell();

						this.Add(cell);

						cell.Mask = lMask;
			
						if (lPathLen > 0 && m_baPackage[nPathStart] != 0) 
						{
							Debug.Assert(nPathStart!=-1,
								"nPathStart��δ��ʼ��");

							Debug.Assert(strlen(m_baPackage, nPathStart) == lPathLen-4-1,
								"lPathLenֵ����ȷ");

							cell.Path += m_encoding.GetString(m_baPackage, nPathStart, lPathLen-4-1);
							cell.Lead += m_encoding.GetString(m_baPackage, nPathStart, lPathLen-4-1);

							// ���һ���ַ�����'/'������ΪUTF8�ַ����������Կ���ʹ��DBCS�жϷ���
							if (cell.Path.Length !=0
								&& cell.Path[Math.Max(0, cell.Path.Length-1)] != '/' )
								cell.Path += "/";

						}

						/*
						if (*lpPath) 
						{  // jia
							ASSERT(strlen(lpPath)==(unsigned int)lPathLen-4L-1L);
				
							if (nSrcCharset == CHARSET_UTF8) 
							{
								CAdvString advstrPath;
								advstrPath.SetString(lpPath,
									nSrcCharset == CHARSET_UTF8 ? _CHARSET_UTF8 : _CHARSET_DBCS);
								pCell->Path += (LPCTSTR)advstrPath; 
								pCell->Lead += (LPCTSTR)advstrPath;
							}
							else 
							{
								// DBCSʱ���Ż�������һ�ζ���ĸ���
								pCell->Path += (LPCSTR)lpPath; 
								pCell->Lead += (LPCSTR)lpPath;
							}

				
							// ���һ���ַ�����'/'������ΪUTF8�ַ����������Կ���ʹ��DBCS�жϷ���
							if ( strlen(lpPath)!=0 && 
								*(lpPath+max(0,strlen(lpPath)-1))!='/' )
								pCell->Path += _T("/");
				
						}
						*/
			
						if (lBaoLen <= 12L) 
							break;



						len = strlen(m_baPackage, nBaoStart) + 1;

						cell.Path += m_encoding.GetString(m_baPackage, nBaoStart, len-1);
						cell.Content += m_encoding.GetString(m_baPackage, nBaoStart, len-1);


						wholelen += len;
						if (wholelen >= lBaoLen-12) // 8
							break;

						nBaoStart += len;
			
					}
				}


		
			SKIP:
				//pp+= lBaoLen;
				nOffs += lBaoLen;
		
				lWholeLen += lBaoLen;
				if (lWholeLen >= lPackageLen)
					break;
			}
	
			return i; // lTgtPos
		}



		// �õ����е�һ���Ӱ������ݲ���
		// ע�����ں���Parse(PackageFormat.Binary)��ʹ�ã�����û�����ݿɴ���
		// return:
		//		-1	error
		//		0	not found
		//		1	found
        public int GetFirstBin(out byte[] baContent)
        {
            Cell cell;
            baContent = null;

            if (this.Count == 0)
                return 0;	// not found

            cell = (Cell)this[0];

            Debug.Assert(cell.ContentBytes != null, "");

            baContent = ByteArray.EnsureSize(baContent,
                cell.ContentBytes.Length);
            Array.Copy(cell.ContentBytes, 0, baContent, 0,
                cell.ContentBytes.Length);
            return 1;
		}


		public string GetFirstPath()
		{
            Cell cell;

            if (this.Count == 0)
                return "";

				cell = (Cell)this[0];
				return cell.Path;
		}

		public string GetFirstContent()
		{
			if (this.Count == 0)
				return "";	// not found
			Cell cell = (Cell)this[0];
			return cell.Content;
		}
	}

	//renyh edit
	//xietao edit
	// line3
}
