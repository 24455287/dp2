using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using System.Text;

using DigitalPlatform;

namespace DigitalPlatform.IO
{
	// ��ʱ�ļ�
	public class TempFileItem 
	{
		public Stream m_stream;
		public string m_strFileName;
	}

	// ��ʱ�ļ�����
	public class TempFileCollection : ArrayList
	{
		public TempFileCollection() 
		{
		}

		~TempFileCollection() 
		{
			Clear();
		}

		public new void Clear() 
		{

			int l;
			for(l=0; l<this.Count; l++) 
			{

				TempFileItem item = (TempFileItem)this[l];
				if (item.m_stream != null) 
				{
					item.m_stream.Close();
					item.m_stream = null;
				}

				try 
				{
					File.Delete(item.m_strFileName);
				}
				catch
				{
				}

			}

			base.Clear();
		}
	}

	public delegate bool FlushOutput();
	public delegate bool ProgressOutput(long lCur);


	/// <remarks>
	/// StreamUtil�࣬Stream������չ����
	/// </remarks>
	public class StreamUtil
	{
		public static long DumpStream(Stream streamSource,Stream streamTarget)
		{
			return DumpStream(streamSource, streamTarget, false);
		}

		// ��Flush����IsConnected״̬
		// �������true��ʾ������false��ʾ������Ѿ��жϣ�û�б�Ҫ�ټ���dump��

		public static long DumpStream(Stream streamSource,
			Stream streamTarget,
			FlushOutput flushOutputMethod)
		{
			int nChunkSize = 8192;
			byte[] bytes = new byte[nChunkSize];
			long lLength = 0;
			while (true) 
			{
				int n = streamSource.Read(bytes,0,nChunkSize);

				if (n != 0)	// 2005/6/8
					streamTarget.Write(bytes,0,n);

				if (flushOutputMethod != null) 
				{
					if (flushOutputMethod()  == false)
						break;
				}

				if (n<=0)
					break;

				lLength += n;
				//if (n<1000)
				//	break;
			}

			return lLength;
		}

		public static long DumpStream(Stream streamSource,
			Stream streamTarget,
			ProgressOutput progressOutputMethod)
		{
			int nChunkSize = 8192;
			byte[] bytes = new byte[nChunkSize];
			long lLength = 0;
			while (true) 
			{
				if (progressOutputMethod != null) 
				{
					if (progressOutputMethod(lLength) == false)
						break;
				}

				int n = streamSource.Read(bytes,0,nChunkSize);

				if (n != 0)	// 2005/6/8
					streamTarget.Write(bytes,0,n);


				if (n<=0)
					break;

				lLength += n;
				//if (n<1000)
				//	break;
			}

			if (progressOutputMethod != null) 
			{
				progressOutputMethod(lLength);
			}

			return lLength;
		}
		

		/// <summary>
		/// ��Դ�����뵽Ŀ����
        /// ����ǰҪȷ���ļ�ָ�����ʵ���λ�á����Ǹ�λ�ã��ʹ��Ǹ�λ�ÿ�ʼdump
		/// </summary>
		/// <param name="streamSource">Դ��</param>
		/// <param name="streamTarget">Ŀ����</param>
		/// <returns>�ɹ�ִ�з���0�������ϣ�����ֵ��������д��ĳ���</returns>
		public static long DumpStream(Stream streamSource,
			Stream streamTarget,
			bool bFlush)
		{
			int nChunkSize = 8192;
			byte[] bytes = new byte[nChunkSize];
			long lLength = 0;
			while (true) 
			{
				int n = streamSource.Read(bytes,0,nChunkSize);

				if (n != 0)	// 2005/6/8
					streamTarget.Write(bytes,0,n);

				if (bFlush == true)
					streamTarget.Flush();

				if (n<=0)
					break;

				lLength += n;
				//if (n<1000)
				//	break;
			}

			return lLength;
		}

		public static long DumpStream(Stream streamSource, 
			Stream streamTarget,
			long lLength)
		{
			return DumpStream(streamSource, streamTarget, lLength, false);
		}


		public static long DumpStream(Stream streamSource, 
			Stream streamTarget,
			long lLength,
			bool bFlush)
		{
			long lWrited = 0;
			long lThisRead = 0;
			int nChunkSize = 8192;
			byte[] bytes = new byte[nChunkSize];
			while (true) 
			{
				long lLeft = lLength - lWrited;
				if (lLeft > nChunkSize)
					lThisRead = nChunkSize;
				else
					lThisRead = lLeft;
				long n = streamSource.Read(bytes,0,(int)lThisRead);

				if (n != 0) // 2005/6/8
				{
					streamTarget.Write(bytes,0,(int)n);
				}

				if (bFlush == true)
					streamTarget.Flush();


				//if (n<nChunkSize)
				//	break;
				if (n <= 0)
					break;

				lWrited += n;
			}

			return lWrited;
		}

		public static long DumpStream(Stream streamSource, 
			Stream streamTarget,
			long lLength,
			FlushOutput flushOutputMethod)
		{
			long lWrited = 0;
			long lThisRead = 0;
			int nChunkSize = 8192;
			byte[] bytes = new byte[nChunkSize];
			while (true) 
			{
				long lLeft = lLength - lWrited;
				if (lLeft > nChunkSize)
					lThisRead = nChunkSize;
				else
					lThisRead = lLeft;
				long n = streamSource.Read(bytes,0,(int)lThisRead);

				if (n != 0)	// 2005/6/8
					streamTarget.Write(bytes,0,(int)n);

				if (flushOutputMethod != null) 
				{
					if (flushOutputMethod()  == false)
						break;
				}


				//if (n<nChunkSize)
				//	break;
				if (n <= 0)
					break;

				lWrited += n;
			}

			return lWrited;
		}

        // д���ı��ļ���
        // ����ļ�������, ���Զ��������ļ�
        // ����ļ��Ѿ����ڣ���׷����β����
        public static void WriteText(string strFileName,
            string strText)
        {
            using (FileStream file = File.Open(
strFileName,
FileMode.Append,	// append
FileAccess.Write,
FileShare.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(file,
                    System.Text.Encoding.UTF8))
                {
                    sw.Write(strText);
                }
            }
        }

#if NO
		// д���ı��ļ���
        // ����ļ�������, ���Զ��������ļ�
		// ����ļ��Ѿ����ڣ���׷����β����
		public static void WriteText(string strFileName, 
			string strText)
		{
            using (StreamWriter sw = new StreamWriter(strFileName,
                true,	// append
                System.Text.Encoding.UTF8))
            {
                sw.Write(strText);
            }
		}
#endif



		// �滻�ļ�ĳһ����
		// ע�⣬�����ǰ�ļ�ΪUNICODE�ַ�������Ҫ�ƻ��ļ���ͷ��UNICODE�����־
		// parameters:
		//		nStartOffs	���滻����Ŀ�ʼƫ��(��byte����)
		//		nEndOffs	���滻����Ľ���ƫ��(��byte����) nEndOffs���Ժ�nStartOffs�غϣ���ʾ��������
		//		baNew		�µ����ݡ������ɾ���ļ���һ�����ݣ������øչ������CByteArray������Ϊ��������
		public static int Replace(Stream s,
			long nStartOffs,
			long nEndOffs,
			byte[] baNew)
		{
			long nFileLen;
			long nOldBytes;
			int nNewBytes;
			long nDelta = 0;
			byte[] baBuffer = null;
			int nMaxChunkSize = 4096;
			int nChunkSize;
			long nRightPartLen;
			long nHead;
			// BOOL bRet;
			int nReadBytes/*, nWriteBytes*/;

			// ��⿪ʼ�������λ�Ƿ�Ϸ�
			if (nStartOffs < 0) 
			{
				throw(new Exception("nStartOffs����С��0"));
			}
			if (nEndOffs < nStartOffs) 
			{
				throw(new Exception("nStartOffs���ܴ���nEndOffs"));
			}
			nFileLen = s.Length;

			if (nEndOffs > nFileLen) 
			{
				throw(new Exception("nEndOffs���ܴ����ļ�����"));
			}


			// ���㱻�滻����ԭ���ĳ���
			nOldBytes = nEndOffs - nStartOffs;

			// �����³���
			nNewBytes = baNew.Length;
	
			if (nNewBytes != nOldBytes)	
			{ // �¾ɳ��Ȳ��ȣ���Ҫ�ƶ����������ļ�����

				// �������ӻ��Ǽ��ٳ��ȣ���Ҫ��ȡ��ͬ���ƶ�����
				// | prev | cur | trail |
				// | prev | cur + delta --> | trial | �����ҷ���Ŀ�
				// | prev | cur - delta <-- | trial | ���ƶ�����Ŀ�
				nDelta = nNewBytes - nOldBytes;
				if (nDelta > 0) 
				{	// ���������ƶ��� ������Ŀ鿪ʼ��
					nRightPartLen = nFileLen - nStartOffs - nOldBytes;
					nChunkSize = (int)Math.Min(nRightPartLen, nMaxChunkSize);
					nHead = nFileLen - nChunkSize;
					if (baBuffer == null || baBuffer.Length < nChunkSize) 
					{
						baBuffer = new byte[nChunkSize];
					}
					// baBuffer.SetSize(nChunkSize);
					for(;;) 
					{
						s.Seek(nHead, SeekOrigin.Begin); 
						//SetFilePointer(m_hFile,nHead,NULL,FILE_BEGIN);

						nReadBytes = s.Read(baBuffer, 0, nChunkSize);
						/*
						bRet = ReadFile(m_hFile, baBuffer.GetData(), nChunkSize,
							(LPDWORD)&nReadBytes, NULL);
						if (bRet == FALSE)
							goto ERROR1;
						*/

						s.Seek(nHead+nDelta, SeekOrigin.Begin);
						// SetFilePointer(m_hFile,nHead+nDelta,NULL,FILE_BEGIN);

						s.Write(baBuffer,0, nReadBytes);
						/*
						bRet = WriteFile(m_hFile, baBuffer.GetData(), nReadBytes, 
							(LPDWORD)&nWriteBytes, NULL);
						if (bRet == FALSE)
							goto ERROR1;
						if (nWriteBytes != nReadBytes) 
						{
							// ������
							m_nErrNo = ERROR_DISK_FULL;
							return -1;
						}
						*/
						// �ƶ�ȫ�����
						if (nHead <= nStartOffs + nOldBytes)	// < ��Ϊ�˰�ȫ
							break;
						// �ƶ�ͷλ��
						nHead -= nChunkSize;
						if (nHead < nStartOffs + nOldBytes) 
						{
							nChunkSize -= (int)(nStartOffs + nOldBytes - nHead);
							nHead = nStartOffs + nOldBytes;
						}

					}
				}

				if (nDelta < 0) 
				{	// ���������ƶ��� ������Ŀ鿪ʼ��
					nRightPartLen = nFileLen - nStartOffs - nOldBytes;
					nChunkSize = (int)Math.Min(nRightPartLen, nMaxChunkSize);
					if (baBuffer == null || baBuffer.Length < nChunkSize) 
					{
						baBuffer = new byte[nChunkSize];
					}

					//baBuffer.SetSize(nChunkSize);
					nHead = nStartOffs + nOldBytes;
					for(;;) 
					{
						s.Seek(nHead, SeekOrigin.Begin); 
						// SetFilePointer(m_hFile,nHead,NULL,FILE_BEGIN);

						nReadBytes = s.Read(baBuffer, 0, nChunkSize);
						/*
						bRet = ReadFile(m_hFile, baBuffer.GetData(), nChunkSize,
							(LPDWORD)&nReadBytes, NULL);
						if (bRet == FALSE)
							goto ERROR1;
						*/

						s.Seek(nHead+nDelta, SeekOrigin.Begin);
						// SetFilePointer(m_hFile,nHead+nDelta,NULL,FILE_BEGIN);

						s.Write(baBuffer,0, nReadBytes);
						/*
						bRet = WriteFile(m_hFile, baBuffer.GetData(), nReadBytes, 
							(LPDWORD)&nWriteBytes, NULL);
						if (bRet == FALSE)
							goto ERROR1;
						if (nWriteBytes != nReadBytes) 
						{
							// ������
							m_nErrNo = ERROR_DISK_FULL;
							return -1;
						}
						*/
						// �ƶ�ȫ�����
						if (nHead + nChunkSize >= nFileLen)
							break;
						// �ƶ�ͷλ��
						nHead += nChunkSize;
						if (nHead + nChunkSize > nFileLen) 
						{ // >��Ϊ�˰�ȫ
							nChunkSize -= (int)(nHead + nChunkSize - nFileLen);
						}

					}
					// �ض��ļ�(��Ϊ��С���ļ��ߴ�)
					//ASSERT(nFileLen+nDelta>=0);
					if (nFileLen + nDelta < 0) 
					{
						throw(new Exception("nFileLen + nDelta < 0"));
					}

					s.SetLength(nFileLen+nDelta);
					/*
					SetFilePointer(m_hFile, nFileLen+nDelta, NULL, FILE_BEGIN);
					SetEndOfFile(m_hFile);
					*/
				}

				//ASSERT(nDelta != 0);
				if (nDelta == 0) 
				{
					throw(new Exception("nDelta == 0"));
				}

			}

			// ��������д��

			if (nNewBytes != 0) 
			{
				s.Seek(nStartOffs, SeekOrigin.Begin);
				//SetFilePointer(m_hFile,nStartOffs,NULL,FILE_BEGIN);

				s.Write(baNew,0,nNewBytes);
				/*
				bRet = WriteFile(m_hFile, baNew.GetData(), nNewBytes, 
					(LPDWORD)&nWriteBytes, NULL);
				if (bRet == FALSE)
					goto ERROR1;
				if (nWriteBytes != nNewBytes) 
				{
					// ������
					m_nErrNo = ERROR_DISK_FULL;
					return -1;
				}
				*/
			}

			/*
			// �ָ�д��ǰ���ļ�ָ�룬������ʹ������ʧЧ
			if (m_nPos > nFileLen + nDelta)
				m_nPos = nFileLen + nDelta;
			SetFilePointer(m_hFile, m_nPos, NULL, FILE_BEGIN);
			m_baCache.RemoveAll();
			m_bEOF = FALSE;
			return 0;
			ERROR1:
				m_nErrNo = GetLastError();
			return -1;
			*/

			return 0;
		}

		// �ƶ��ļ�ĳһ����
        // ����������ǰ���Ƿ�֤���ļ�ָ�벻��?
		// parameters:
		//		nSourceOffs	���ƶ�����Ŀ�ʼƫ��(��byte����)
		//		nLength		���ƶ�����ĳ���(��byte����)
		//		nTargetOffs	Ҫ�ƶ�����Ŀ��λ��ƫ��(��byte����)
		public static int Move(Stream s,
			long nSourceOffs,
			long nLength,
			long nTargetOffs)
		{
			long nFileLen;
			long nDelta = 0;
			byte[] baBuffer = null;
			int nMaxChunkSize = 4096;
			int nChunkSize;
			long nRightPartLen;
			long nHead;
			int nReadBytes;

			// ��⿪ʼ�������λ�Ƿ�Ϸ�
			if (nSourceOffs < 0) 
			{
				throw(new Exception("nSourceOffs����С��0"));
			}
			if (nTargetOffs < 0) 
			{
				throw(new Exception("nTargetOffs����С��0"));
			}
			nFileLen = s.Length;

			if (nSourceOffs + nLength > nFileLen) 
			{
				throw(new Exception("�ƶ�ǰ����β������Խ���ļ�����"));
			}


			if (nSourceOffs != nTargetOffs)	
			{

				// �������ӻ��Ǽ��ٳ��ȣ���Ҫ��ȡ��ͬ���ƶ�����
				// | prev | block | 
				// | prev | + delta --> | block | �����ҷ���Ŀ�
				// | prev | - delta <-- | block | ���ƶ�����Ŀ�
				nDelta = nTargetOffs - nSourceOffs;	//nNewBytes - nOldBytes;
				if (nDelta > 0) 
				{	// ���������ƶ��� ������Ŀ鿪ʼ��
					nRightPartLen = nLength;	// nRightMost - nStartOffs - nOldBytes;
					nChunkSize = (int)Math.Min(nRightPartLen, nMaxChunkSize);
					nHead = nSourceOffs + nLength - nChunkSize;
					if (baBuffer == null || baBuffer.Length < nChunkSize) 
					{
						baBuffer = new byte[nChunkSize];
					}

					for(;;) 
					{
						s.Seek(nHead, SeekOrigin.Begin); 


						nReadBytes = s.Read(baBuffer, 0, nChunkSize);

						s.Seek(nHead+nDelta, SeekOrigin.Begin);


						s.Write(baBuffer,0, nReadBytes);

						// �ƶ�ȫ�����
						if (nHead <= nSourceOffs)	// < ��Ϊ�˰�ȫ
							break;
						// �ƶ�ͷλ��
						nHead -= nChunkSize;
						if (nHead < nSourceOffs) // ����һ��chunk
						{
							nChunkSize -= (int)(nSourceOffs - nHead);
							if (nChunkSize <= 0) 
							{
								throw(new Exception("nChunkSizeС�ڻ����0!"));
							}
							nHead = nSourceOffs;
						}

					}
				}

				if (nDelta < 0) 
				{	// ���������ƶ��� ������Ŀ鿪ʼ��
					nRightPartLen = nLength;	// nRightMost - nStartOffs - nOldBytes;
					nChunkSize = (int)Math.Min(nRightPartLen, nMaxChunkSize);
					if (baBuffer == null || baBuffer.Length < nChunkSize) 
					{
						baBuffer = new byte[nChunkSize];
					}

					nHead = nSourceOffs;
					for(;;) 
					{
						s.Seek(nHead, SeekOrigin.Begin); 

						nReadBytes = s.Read(baBuffer, 0, nChunkSize);

						s.Seek(nHead+nDelta, SeekOrigin.Begin);


						s.Write(baBuffer,0, nReadBytes);
						// �ƶ�ȫ�����
						if (nHead + nChunkSize >= nSourceOffs + nLength)
							break;
						// �ƶ�ͷλ��
						nHead += nChunkSize;
						if (nHead + nChunkSize > nSourceOffs + nLength) // ���һ���飬��ͷ
						{ // >��Ϊ�˰�ȫ
							nChunkSize -= (int)(nHead + nChunkSize - (nSourceOffs + nLength));
							if (nChunkSize <= 0) 
							{
								throw(new Exception("nChunkSizeС�ڻ����0!"));
							}
						}

					}


				}

				if (nDelta == 0) 
				{
					throw(new Exception("nDelta == 0"));
				}

			}

			return 0;
		}


	} // end of class StreamUtil


	/// <summary>
	/// Path������չ����
	/// </summary>
	public class PathUtil
	{

        // ���һ��Ŀ¼�µ�ȫ���ļ��ĳߴ��ܺ͡�������Ŀ¼�е�
        public static long GetAllFileSize(string strDataDir, ref long count)
        {
            long size = 0;
            DirectoryInfo di = new DirectoryInfo(strDataDir);
            FileInfo[] fis = di.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
                count++;
            }

            // �����¼�Ŀ¼���ݹ�
            DirectoryInfo[] dis = di.GetDirectories();
            foreach (DirectoryInfo subdir in dis)
            {
                size += GetAllFileSize(subdir.FullName, ref count);
            }

            return size;
        }

        // get clickonce shortcut filename
        // parameters:
        //      strApplicationName  "DigitalPlatform/dp2 V2/dp2���� V2"
        public static string GetShortcutFilePath(string strApplicationName)
        {
            // string publisherName = "Publisher Name";
            // string applicationName = "Application Name";
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), strApplicationName) + ".appref-ms";
        }

        public static void DeleteDirectory(string strDirPath)
        {
            try
            {
                Directory.Delete(strDirPath, true);
            }
            catch (DirectoryNotFoundException)
            {
                // �����ھ�����
            }
        }

        // �Ƴ��ļ�Ŀ¼�������ļ��� ReadOnly ����
        public static void RemoveReadOnlyAttr(string strSourceDir)
        {
            string strCurrentDir = Directory.GetCurrentDirectory();

            DirectoryInfo di = new DirectoryInfo(strSourceDir);

            FileSystemInfo[] subs = di.GetFileSystemInfos();

            for (int i = 0; i < subs.Length; i++)
            {

                // �ݹ�
                if ((subs[i].Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    RemoveReadOnlyAttr(subs[i].FullName);
                }
                else
                    File.SetAttributes(subs[i].FullName, FileAttributes.Normal);

            }
        }

		// ����Ŀ¼
		public static int CopyDirectory(string strSourceDir,
			string strTargetDir,
			bool bDeleteTargetBeforeCopy,
			out string strError)
		{
			strError = "";

			try 
			{

				DirectoryInfo di = new DirectoryInfo(strSourceDir);

				if (di.Exists == false)
				{
					strError = "ԴĿ¼ '" + strSourceDir + "' ������...";
					return -1;
				}

				if (bDeleteTargetBeforeCopy == true)
				{
					if (Directory.Exists(strTargetDir) == true)
						Directory.Delete(strTargetDir, true);
				}

				CreateDirIfNeed(strTargetDir);


				FileSystemInfo [] subs = di.GetFileSystemInfos();

				for(int i=0;i<subs.Length;i++) 
				{
					// ����Ŀ¼
					if ((subs[i].Attributes & FileAttributes.Directory) == FileAttributes.Directory) 
					{
						int nRet = CopyDirectory(subs[i].FullName,
							strTargetDir + "\\" + subs[i].Name,
							bDeleteTargetBeforeCopy,
							out strError);
						if (nRet == -1)
							return-1;
						continue;
					}
					// �����ļ�
					File.Copy(subs[i].FullName, strTargetDir + "\\" + subs[i].Name, true);
				}

			}
			catch (Exception ex)
			{
				strError = ex.Message;
				return -1;
			}


			return 0;
		}

		// ���Ŀ¼�������򴴽�֮
        // return:
        //      false   �Ѿ�����
        //      true    �ո��´���
		public static bool CreateDirIfNeed(string strDir)
		{
			DirectoryInfo di = new DirectoryInfo(strDir);
            if (di.Exists == false)
            {
                di.Create();
                return true;
            }

            return false;
		}

        // ɾ��һ��Ŀ¼�ڵ������ļ���Ŀ¼
        public static bool ClearDir(string strDir)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(strDir);
                if (di.Exists == false)
                    return true;

                // ɾ�����е��¼�Ŀ¼
                DirectoryInfo[] dirs = di.GetDirectories();
                foreach (DirectoryInfo childDir in dirs)
                {
                    Directory.Delete(childDir.FullName, true);
                }

                // ɾ�������ļ�
                FileInfo[] fis = di.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    File.Delete(fi.FullName);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // ��ô��ļ�������
		public static string PureName(string strPath)
		{
            // 2012/11/30
            if (string.IsNullOrEmpty(strPath) == true)
                return strPath;

			string sResult = "";
			sResult = strPath;
			sResult = sResult.Replace("/", "\\");
			if (sResult.Length > 0) 
			{
				if (sResult[sResult.Length-1] == '\\')
					sResult = sResult.Substring(0, sResult.Length - 1);
			}
			int nRet = sResult.LastIndexOf("\\");
			if (nRet != -1)
				sResult = sResult.Substring(nRet + 1);

			return sResult;
		}

		public static string PathPart(string strPath)
		{
			string sResult = "";
			sResult = strPath;
			sResult = sResult.Replace("/", "\\");
			if (sResult.Length > 0) 
			{
				if (sResult[sResult.Length-1] == '\\')
					sResult = sResult.Substring(0, sResult.Length - 1);
			}
			int nRet = sResult.LastIndexOf("\\");
			if (nRet != -1)
				sResult = sResult.Substring(0, nRet);
			else
				sResult = "";

			return sResult;
		}

		public static string MergePath(string s1, string s2)
		{
			string sResult = "";

			if (s1 != null) 
			{
				sResult = s1;
				sResult = sResult.Replace("/", "\\");
				if (sResult != "") 
				{
					if (sResult[sResult.Length -1] != '\\')
						sResult += "\\";
				}
				else 
				{
					sResult += "\\";
				}
			}
			if (s2 != null) 
			{
				s2 = s2.Replace("/","\\");
				if (s2 != "") 
				{
					if (s2[0] == '\\')
						s2 = s2.Remove(0,1);
					sResult += s2;
				}

			}

			return sResult;
		}

        // ���滯Ŀ¼·�������������ַ�'/'�滻Ϊ'\'������Ϊĩβȷ�����ַ�'\'
        public static string CanonicalizeDirectoryPath(string strPath)
        {
            if (string.IsNullOrEmpty(strPath) == true)
                return "";

            strPath = strPath.Replace('/', '\\');

            if (strPath[strPath.Length - 1] != '\\')
                strPath += "\\";

            return strPath;
        }

		// ����strPath1�Ƿ�ΪstrPath2���¼�Ŀ¼���ļ�
		//	strPath1���õ���strPath2�����Ҳ����true
		public static bool IsChildOrEqual(string strPath1, string strPath2)
		{
			FileSystemInfo fi1 = new DirectoryInfo(strPath1);

			FileSystemInfo fi2 = new DirectoryInfo(strPath2);

			string strNewPath1 = fi1.FullName;
			string strNewPath2 = fi2.FullName;

			if (strNewPath1.Length != 0) 
			{
				if (strNewPath1[strNewPath1.Length-1] != '\\')
					strNewPath1 += "\\";
			}
			if (strNewPath2.Length != 0) 
			{
				if (strNewPath2[strNewPath2.Length-1] != '\\')
					strNewPath2 += "\\";
			}

			// ·��1�ַ������ȱ�·��2�̣�˵��·��1�Ѳ������Ƕ��ӣ���Ϊ���ӵ�·�������
			if (strNewPath1.Length < strNewPath2.Length)
				return false;


			// ��ȡ·��1��ǰ��һ�ν��бȽ�
			string strPart = strNewPath1.Substring(0, strNewPath2.Length);
			strPart.ToUpper();
			strNewPath2.ToUpper();

			if (strPart != strNewPath2)
				return false;

			return true;
		}

		// ����strPath1�Ƿ��strPath2Ϊͬһ�ļ���Ŀ¼
		public static bool IsEqual(string strPath1, string strPath2)
		{
            if (String.IsNullOrEmpty(strPath1) == true
                && String.IsNullOrEmpty(strPath2) == true)
                return true;

            if (String.IsNullOrEmpty(strPath1) == true)
                return false;

            if (String.IsNullOrEmpty(strPath2) == true)
                return false;

            if (strPath1 == strPath2)
                return true;

            FileSystemInfo fi1 = new DirectoryInfo(strPath1);
			FileSystemInfo fi2 = new DirectoryInfo(strPath2);

			string strNewPath1 = fi1.FullName;
			string strNewPath2 = fi2.FullName;

			if (strNewPath1.Length != 0) 
			{
				if (strNewPath1[strNewPath1.Length-1] != '\\')
					strNewPath1 += "\\";
			}
			if (strNewPath2.Length != 0) 
			{
				if (strNewPath2[strNewPath2.Length-1] != '\\')
					strNewPath2 += "\\";
			}

			if (strNewPath1.Length != strNewPath2.Length)
				return false;

			strNewPath1.ToUpper();
			strNewPath2.ToUpper();

			if (strNewPath1 == strNewPath2)
				return true;

			return false;
		}

        // ����strPath1�Ƿ��strPath2Ϊͬһ�ļ���Ŀ¼
        public static bool IsEqualEx(string strPath1, string strPath2)
        {
            string strNewPath1 = strPath1;
            string strNewPath2 = strPath2;

            if (strNewPath1.Length != 0)
            {
                if (strNewPath1[strNewPath1.Length - 1] != '\\')
                    strNewPath1 += "\\";
            }
            if (strNewPath2.Length != 0)
            {
                if (strNewPath2[strNewPath2.Length - 1] != '\\')
                    strNewPath2 += "\\";
            }

            if (strNewPath1.Length != strNewPath2.Length)
                return false;

            strNewPath1.ToUpper();
            strNewPath2.ToUpper();

            if (strNewPath1 == strNewPath2)
                return true;

            return false;
        }


		public static string EnsureTailBackslash(string strPath)
		{
			if (strPath == "")
				return "\\";

			string sResult = "";

			sResult = strPath.Replace("/", "\\");

			if (sResult[sResult.Length-1] != '\\')
				sResult += "\\";

			return sResult;
		}

		// ĩβ�Ƿ���'\'������߱�����ʾ����һ��Ŀ¼·����
		public static bool HasTailBackslash(string strPath)
		{
			if (strPath == "")
				return true;	// ���Ϊ'\'

			string sResult = "";

			sResult = strPath.Replace("/", "\\");

			if (sResult[sResult.Length-1] == '\\')
				return true;

			return false;
		}


		// ����strPathChild�Ƿ�ΪstrPathParent���¼�Ŀ¼���ļ�
		// ������¼�����strPathChild�к�strPathParent�غϵĲ����滻Ϊ
		// strMacro�еĺ��ַ���������strResult�У����Һ�������true��
		// ����������false��strResult�䷵�����ݣ������滻��
		//	strPath1���õ���strPath2�����Ҳ����true
		// 
		// Exception:
		//	System.NotSupportedException
		// Testing:
		//	��testIO.exe��
		public static bool MacroPathPart(string strPathChild,
			string strPathParent,
			string strMacro,
			out string strResult)
		{
			strResult = strPathChild;

			FileSystemInfo fiChild = new DirectoryInfo(strPathChild);

			FileSystemInfo fiParent = new DirectoryInfo(strPathParent);

			string strNewPathChild = fiChild.FullName;
			string strNewPathParent = fiParent.FullName;

			if (strNewPathChild.Length != 0) 
			{
				if (strNewPathChild[strNewPathChild.Length-1] != '\\')
					strNewPathChild += "\\";
			}
			if (strNewPathParent.Length != 0) 
			{
				if (strNewPathParent[strNewPathParent.Length-1] != '\\')
					strNewPathParent += "\\";
			}

			// ·��1�ַ������ȱ�·��2�̣�˵��·��1�Ѳ������Ƕ��ӣ���Ϊ���ӵ�·�������
			if (strNewPathChild.Length < strNewPathParent.Length)
				return false;


			// ��ȡ·��1��ǰ��һ�ν��бȽ�
			string strPart = strNewPathChild.Substring(0, strNewPathParent.Length);
			strPart.ToUpper();
			strNewPathParent.ToUpper();

			if (strPart != strNewPathParent)
				return false;

			strResult = strMacro + "\\" + fiChild.FullName.Substring(strNewPathParent.Length);
			// fiChild.FullName��β��δ��'\'��ǰ����ʽ

			return true;
		}

		// ��·���е�%%�겿���滻Ϊ��������
		// parameters:
		//		macroTable	���������ݵĶ��ձ�
		//		bThrowMacroNotFoundException	�Ƿ��׳�MacroNotFoundException�쳣
		// Exception:
		//	MacroNotFoundException
		//	MacroNameException	����NextMacro()�����׳�
		// Testing:
		//	��testIO.exe��
		public static string UnMacroPath(Hashtable macroTable,
			string strPath,
			bool bThrowMacroNotFoundException)
		{
			int nCurPos = 0;
			string strPart = "";

			string strResult = "";

			for(;;) 
			{
				strPart = NextMacro(strPath, ref nCurPos);
				if (strPart == "")
					break;

				if (strPart[0] == '%') 
				{
					string strValue = (string)macroTable[strPart];
					if (strValue == null) 
					{
						if (bThrowMacroNotFoundException) 
						{
							MacroNotFoundException ex = new MacroNotFoundException("macro " + strPart + " not found in macroTable");
							throw ex;
						}
						else 
						{
							// ��û���ҵ��ĺ�Żؽ����
							strResult += strPart;
							continue;
						}
					}

					strResult += strValue;
				}
				else 
				{
					strResult += strPart;
				}

			}

			return strResult;
		}

		// ������ΪUnMacroPath()�ķ�����
		// ˳�εõ���һ������
		// nCurPos�ڵ�һ�ε���ǰ��ֵ��������Ϊ0��Ȼ�󣬵�����Ҫ�ı���ֵ
		// Exception:
		//	MacroNameException
		static string NextMacro(string strText,
			ref int nCurPos)
		{
			if (nCurPos >= strText.Length)
				return "";

			string strResult = "";
			bool bMacro = false;	// �����Ƿ���macro��

			if (strText[nCurPos] == '%')
				bMacro = true;

			int nRet = -1;
			
			if (bMacro == false)
				nRet = strText.IndexOf("%", nCurPos);
			else
				nRet = strText.IndexOf("%", nCurPos+1);

			if (nRet == -1) 
			{
				strResult = strText.Substring(nCurPos);
				nCurPos = strText.Length;
				if (bMacro == true) 
				{
					// �����쳣���������%ֻ��ͷ��һ��
					throw(new MacroNameException("macro " + strResult + " format error"));
				}
				return strResult;
			}

			if (bMacro == true) 
			{
				strResult = strText.Substring(nCurPos, nRet - nCurPos + 1);
				nCurPos = nRet + 1;
				return strResult;
			}
			else 
			{
				Debug.Assert(strText[nRet] == '%', "��ǰλ�ò���%���쳣");
				strResult = strText.Substring(nCurPos, nRet - nCurPos);
				nCurPos = nRet;
				return strResult;
			}

		}

		public static string GetShortFileName(string strFileName)
		{
			StringBuilder shortPath = new StringBuilder(300);
			int nRet = API.GetShortPathName(
				strFileName,
				shortPath,
				shortPath.Capacity);
			if (nRet == 0 || nRet >= 300) 
			{
				// MessageBox.Show("file '" +strFileName+ "' get short error");
				// return strFileName;
				throw(new Exception("GetShortFileName error"));
			}

			return shortPath.ToString(); 
		}


	}

	// �ڶ��ձ��к겻����
	public class MacroNotFoundException : Exception
	{

		public MacroNotFoundException (string s) : base(s)
		{
		}

	}

	// ������ʽ��
	public class MacroNameException : Exception
	{

		public MacroNameException (string s) : base(s)
		{
		}

	}


	/// <summary>
	/// File������չ����
	/// </summary>
	public class FileUtil
	{
        // ����ַ����Ƿ�Ϊ������(������'-','.'��)
        public static bool IsPureNumber(string s)
        {
            if (s == null)
                return false;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] > '9' || s[i] < '0')
                    return false;
            }
            return true;
        }

        // �ļ���չ���Ƿ�Ϊ ._1 ._2 ...
        public static bool IsBackupFile(string strFileName)
        {
            string strExt = Path.GetExtension(strFileName);
            if (string.IsNullOrEmpty(strExt) == true)
                return false;
            if (strExt.StartsWith("._") == false)
                return false;
            string strNumber = strExt.Substring(2);
            if (IsPureNumber(strNumber) == true)
                return true;

            return false;
        }
        /// <summary>
        /// ����ļ��ĳ���
        /// </summary>
        /// <param name="strFileName">�ļ���ȫ·��</param>
        /// <returns>�����ļ����ȡ������ -1����ʾ�ļ�������</returns>
        public static long GetFileLength(string strFileName)
        {
            FileInfo fi = new FileInfo(strFileName);
            if (fi.Exists == false)
                return -1;
            return fi.Length;
        }
        // ����ʱ�����Ϣ�������ļ�������޸�ʱ��
        public static void SetFileLastWriteTimeByTimestamp(string strFilePath,
            byte[] baTimeStamp)
        {
            if (baTimeStamp == null || baTimeStamp.Length < 8)
                return;
#if NO
            byte [] baTime = new byte[8];
            Array.Copy(baTimeStamp,
    0,
    baTime,
    0,
    8);
#endif
            long lTicks = BitConverter.ToInt64(baTimeStamp, 0);

            FileInfo fileInfo = new FileInfo(strFilePath);
            if (fileInfo.Exists == false)
                return;
            fileInfo.LastWriteTimeUtc = new DateTime(lTicks);
        }

        // �����ļ�������޸�ʱ��ͳߴ�, ���ʱ�����Ϣ
        public static byte[] GetFileTimestamp(string strFilePath)
        {
            byte[] baTimestamp = null;
            FileInfo fileInfo = new FileInfo(strFilePath);
            if (fileInfo.Exists == false)
                return baTimestamp;

            long lTicks = fileInfo.LastWriteTimeUtc.Ticks;
            byte[] baTime = BitConverter.GetBytes(lTicks);

            byte[] baLength = BitConverter.GetBytes((long)fileInfo.Length);
            //Array.Reverse(baLength);

            baTimestamp = new byte[baTime.Length + baLength.Length];
            Array.Copy(baTime,
                0,
                baTimestamp,
                0,
                baTime.Length);
            Array.Copy(baLength,
                0,
                baTimestamp,
                baTime.Length,
                baLength.Length);

            return baTimestamp;
        }

        // д����־�ļ���ÿ�촴��һ����������־�ļ�
        public static void WriteErrorLog(
            object lockObj,
            string strLogDir,
            string strText,
            string strPrefix = "log_",
            string strPostfix = ".txt")
        {

            lock (lockObj)
            {
                DateTime now = DateTime.Now;
                // ÿ��һ����־�ļ�
                string strFilename = PathUtil.MergePath(strLogDir, strPrefix + DateTimeUtil.DateTimeToString8(now) + strPostfix);
                string strTime = now.ToString();
                StreamUtil.WriteText(strFilename,
                    strTime + " " + strText + "\r\n");
            }
        }

        // ���Զ�ʶ���ļ����ݵı��뷽ʽ�Ķ����ı��ļ�����ģ��
        // parameters:
        //      lMaxLength  װ�����󳤶ȡ�����������򳬹��Ĳ��ֲ�װ�롣���Ϊ-1����ʾ������װ�볤��
        // return:
        //      -1  ���� strError���з���ֵ
        //      0   �ļ������� strError���з���ֵ
        //      1   �ļ�����
        //      2   ��������ݲ���ȫ��
        public static int ReadTextFileContent(string strFilePath,
            long lMaxLength,
            out string strContent,
            out Encoding encoding,
            out string strError)
        {
            strError = "";
            strContent = "";
            encoding = null;

            if (File.Exists(strFilePath) == false)
            {
                strError = "�ļ� '" + strFilePath + "' ������";
                return 0;
            }

            encoding = FileUtil.DetectTextFileEncoding(strFilePath);

            try
            {
                bool bExceed = false;

                using (FileStream file = File.Open(
        strFilePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.ReadWrite))
                {
                    // TODO: ������Զ�̽���ļ����뷽ʽ���ܲ���ȷ��
                    // ��Ҫר�ű�дһ��������̽���ı��ļ��ı��뷽ʽ
                    // Ŀǰֻ����UTF-8���뷽ʽ
                    using (StreamReader sr = new StreamReader(file, encoding))
                    {
                        if (lMaxLength == -1)
                        {
                            strContent = sr.ReadToEnd();
                        }
                        else
                        {
                            long lLoadedLength = 0;
                            StringBuilder temp = new StringBuilder(4096);
                            for (; ; )
                            {
                                string strLine = sr.ReadLine();
                                if (strLine == null)
                                    break;
                                if (lLoadedLength + strLine.Length > lMaxLength)
                                {
                                    strLine = strLine.Substring(0, (int)(lMaxLength - lLoadedLength));
                                    temp.Append(strLine + " ...");
                                    bExceed = true;
                                    break;
                                }

                                temp.Append(strLine + "\r\n");
                                lLoadedLength += strLine.Length + 2;
                                if (lLoadedLength > lMaxLength)
                                {
                                    temp.Append(strLine + " ...");
                                    bExceed = true;
                                    break;
                                }
                            }
                            strContent = temp.ToString();
                        }
                        /*
                    sr.Close();
                    sr = null;
                         * */
                    }
                }

                if (bExceed == true)
                    return 2;
            }
            catch (Exception ex)
            {
                strError = "�򿪻�����ļ� '" + strFilePath + "' ʱ����: " + ex.Message;
                return -1;
            }

            return 1;
        }

        // ���һ����ʱ�ļ���
        // parameters:
        //      strPostFix  ������ڱ�ʾ�ļ���չ����Ӧ�����㡣������Ե���һ���׺�ַ���ʹ��
        public static string NewTempFileName(string strDir,
            string strPrefix,
            string strPostFix)
        {
            if (string.IsNullOrEmpty(strDir) == true)
            {
                return Path.GetTempFileName();
            }

            string strFileName = "";
            for (int i = 0; ; i++)
            {
                strFileName = PathUtil.MergePath(strDir, strPrefix + Convert.ToString(i) + strPostFix);

                FileInfo fi = new FileInfo(strFileName);
                if (fi.Exists == false)
                {
                    // ����һ��0 byte���ļ�
                    FileStream f = File.Create(strFileName);
                    f.Close();
                    return strFileName;
                }
            }
        }

		// ��һ��byte[] д��ָ�����ļ�
		// parameter:
		//		strFileName: �ļ���,ÿ���´���,����ԭ�����ļ�
		//		data: byte����
		// ��д��: ���ӻ�
		public static void WriteByteArray(string strFileName,
			byte[] data)
		{
			FileStream s = File.Create(strFileName);
			try
			{
				s.Write(data,
					0,
					data.Length);
			}
			finally
			{
				s.Close();
			}
		}

		// �Ķ��ļ��ġ�����޸�ʱ�䡱����
		// ע������׳��쳣
		public static void ChangeFileLastModifyTimeUtc(string strPath,
			string strTime)
		{
			FileInfo fi = new FileInfo(strPath);
			DateTime time = DateTimeUtil.FromRfc1123DateTimeString(strTime);
			fi.LastWriteTimeUtc = time;
			fi.CreationTimeUtc = time;
		}

        // �ļ��Ƿ���ڲ����Ƿǿ�
        public static bool IsFileExsitAndNotNull(string strFilename)
        {

            try
            {
                FileStream file = File.Open(
                    strFilename,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                if (file.Length == 0)
                    return false;

                file.Close();
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

		// �ļ��Ƿ����?
		public static bool FileExist(string strFilePath)
		{
			FileInfo fi = new FileInfo(strFilePath);
			return fi.Exists;
		}


		// ����:��һ���ַ���д���ļ�,���ļ�������ʱ,�����ļ�;����,�����ļ�
		// paramter:
		//		strContent:  �ַ���
		//		strFileName: �ļ���
		// ��д�ߣ����ӻ�
		public static void String2File(string strContent,
			string strFileName)
		{
			StreamWriter s  = File.CreateText(strFileName);
			try
			{
				s.Write(strContent);
			}
			finally
			{
				s.Close();
			}
		}


		// ����:�ļ����ַ�����ʹ��ֱ�Ӷ���β�ķ���
		// strFileName: �ļ���
		public static string File2StringE(string strFileName)
		{
			if (strFileName == null
				|| strFileName == "")
				return "";
			StreamReader sr = new StreamReader(strFileName, true);
			string strText = sr.ReadToEnd();
			sr.Close();

			return strText;
		}

        // ���δ��̽����������� 936
        public static Encoding DetectTextFileEncoding(string strFilename)
        {
            Encoding encoding = DetectTextFileEncoding(strFilename, null);
            if (encoding == null)
                return Encoding.GetEncoding(936);    // default

            return encoding;
        }

        // ����ı��ļ���encoding
        /*
UTF-8: EF BB BF 
UTF-16 big-endian byte order: FE FF 
UTF-16 little-endian byte order: FF FE 
UTF-32 big-endian byte order: 00 00 FE FF 
UTF-32 little-endian byte order: FF FE 00 00 
         * */
        public static Encoding DetectTextFileEncoding(string strFilename,
            Encoding default_encoding)
        {

            byte[] buffer = new byte[4];

            try
            {
                FileStream file = File.Open(
        strFilename,
        FileMode.Open,
        FileAccess.Read,
        FileShare.ReadWrite);
                try
                {

                    if (file.Length >= 2)
                    {
                        file.Read(buffer, 0, 2);    // 1, 2 BUG

                        if (buffer[0] == 0xff && buffer[1] == 0xfe)
                        {
                            return Encoding.Unicode;    // little-endian
                        }

                        if (buffer[0] == 0xfe && buffer[1] == 0xff)
                        {
                            return Encoding.BigEndianUnicode;
                        }
                    }

                    if (file.Length >= 3)
                    {
                        file.Read(buffer, 2, 1);
                        if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                        {
                            return Encoding.UTF8;
                        }

                    }

                    if (file.Length >= 4)
                    {
                        file.Read(buffer, 3, 1);

                        // UTF-32 big-endian byte order: 00 00 FE FF 
                        // UTF-32 little-endian byte order: FF FE 00 00 

                        if (buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xfe && buffer[3] == 0xff)
                        {
                            return Encoding.UTF32;    // little-endian
                        }

                        if (buffer[0] == 0xff && buffer[1] == 0xfe && buffer[2] == 0x00 && buffer[3] == 0x00)
                        {
                            return Encoding.GetEncoding(65006);    // UTF-32 big-endian
                        }
                    }

                }
                finally
                {
                    file.Close();
                }
            }
            catch
            {
            }

            return default_encoding;    // default
        }
	}

}
