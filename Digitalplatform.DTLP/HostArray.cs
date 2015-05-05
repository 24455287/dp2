using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections;

using DigitalPlatform.Xml;

namespace DigitalPlatform.DTLP
{
	public class HostArray : ArrayList
	{
		ApplicationInfo appinfo = null;

		public DtlpChannel Container = null;

		// ��ini�ļ�����registryװ���Ѿ����õ�������������
		public int InitialHostArray(ApplicationInfo appInfoParam)
		{
			int i, nMax;
			HostEntry entry = null;

			this.Clear();

			appinfo = appInfoParam;	// ������������

            if (appInfoParam == null)   // 2006/11/21 new add
                return 0;

			ArrayList saHost = LoadHosts(appInfoParam);
			nMax = saHost.Count;
			for(i=0; i<nMax; i++) 
			{
				entry = new HostEntry();
				entry.m_strHostName = (string)saHost[i];
				this.Add(entry);
				entry.Container = this;
			}

			return 0;
		}

		// ��ini�ļ�����registryװ���Ѿ����õ�������������
		public static ArrayList LoadHosts(ApplicationInfo appInfo)
		{
			ArrayList saResult = new ArrayList();

			for(int i=0; ;i++) 
			{
				string strEntry = "entry" + Convert.ToString(i+1);
					
				string strValue = appInfo.GetString("ServerAddress",
					strEntry,
					"");
				if (strValue == "")
					break;
				saResult.Add(strValue);
			}

			return saResult;
		}

		// ��CStringArray�е���������д��ini�ļ�����registry
		public static void SaveHosts(ApplicationInfo appInfo,
			ArrayList saHost)
		{
			string strEntry = null;
			int i = 0;

			for(i=0; i<saHost.Count; i++) 
			{

				strEntry = "entry" + Convert.ToString(i+1);
			
				string strValue = (string)saHost[i];
			
				appInfo.SetString("ServerAddress",
					strEntry,
					strValue);
			}

			// ���һ�Σ��ض�
			strEntry = "entry" + Convert.ToString(i+1);
			appInfo.SetString("ServerAddress",
				strEntry,
				"");
	
		}

		// �ݻ�һ��Host����
		public int DestroyHostEntry(HostEntry entry)
		{
			this.Remove(entry);
			return 0; // not found
		}

		// ���������ֻ��߱���Ѱ����������
		public HostEntry MatchHostEntry(string strHostName)
		{

			for(int i=0;i<this.Count;i++) 
			{
				HostEntry entry = (HostEntry)this[i];
				Debug.Assert(entry != null, "HostEntry�г��ֿ�Ԫ��");

				if ( (String.Compare(strHostName,entry.m_strHostName, true)==0)
					|| (String.Compare(strHostName,entry.m_strAlias, true)==0) )
					return entry;
			}
			return null;
		}

		public void CloseAllSockets()
		{

			for(int i=0;i<this.Count;i++) 
			{
				HostEntry entry = (HostEntry)this[i];
				Debug.Assert(entry != null, "HostEntry�г��ֿ�Ԫ��");

				if (entry.client != null) 
				{
					entry.client.Close();
					entry.client = null;
				}
			}
		}


	}


	/// <summary>
	/// Summary description for HostArray.
	/// </summary>
	public class HostEntry
	{

		// SOCKET		m_hSocket;

		public TcpClient client = null;

		public string		m_strHostName = "";	// IP��ַ��������
		public string		m_strAlias = "";		// ����

		public int			m_nDTLPCharset = DtlpChannel.CHARSET_DBCS;

		// int			m_nLock = 0;

//		bool		m_bWantDel = false;
		public int			m_lUsrID = 0;
		public int			m_lChannel = -1;
	
//		int			m_nStatus = 0;	// 0:���� 1:���� 2:����


		public HostArray	Container = null;


		public HostEntry()
		{
			//m_hSocket = INVALID_SOCKET;
			// m_nDTLPCharset = CHARSET_DBCS;
		}


		~HostEntry()
		{
			if (m_lChannel != -1 && client != null) 
			{
				// RmtDestroyChannel();
			}
			if (client != null) 
			{
				client.Close();
				client = null;
			}
		}

		// Զ�̽���ͨ��
		// ���� -1 ��ʾʧ��
		public int RmtCreateChannel(int usrid)
		{

			// send:long usrid
			// recv:
			// return long
	
			DTLPParam	param = new DTLPParam();
			int		lRet;
			int			nRet;
			int			nLen;
			byte []	baSendBuffer = null;
			byte []	baRecvBuffer = null;
			int			nErrorNo = 0;

			Debug.Assert(client != null,
				"clientΪ��");
	
			param.Clear();
			param.ParaLong(usrid);
			lRet = param.ParaToPackage(DtlpChannel.FUNC_CREATECHANNEL,
				nErrorNo,
				out baSendBuffer );

			if (lRet == -1) 
			{
				return -1; // error
			}

			nRet = SendTcpPackage(baSendBuffer,
                baSendBuffer.Length,
				out nErrorNo);
	
			if (nRet < 0) 
			{
				return -1;
			}

			nRet = RecvTcpPackage(out baRecvBuffer, 
				out nLen,
				out nErrorNo);
			if (nRet<0) 
			{
				return -1;
			}
	
			param.Clear();
			param.DefPara(Param.STYLE_LONG);


			int nTempFuncNum = 0;
			param.PackageToPara(baRecvBuffer,
                ref nErrorNo,
				out nTempFuncNum);

			lRet = param.lValue(0);

			m_lChannel = lRet;
			m_lUsrID = usrid;

			return lRet;
		}


		// ע��Զ��ͨ��
		public int RmtDestroyChannel()

		{
			// send:long usrid
			//      long Channel
			// recv:
			// return long
	
			DTLPParam	param = new DTLPParam();
			int		lRet;
			int			nRet;
			int			nLen;
			byte [] baSendBuffer = null;
			byte []	baRecvBuffer = null;
			int			nErrorNo = 0;
	
			Debug.Assert(client != null,
				"clientΪ��");
	
			param.Clear();
			param.ParaLong(m_lUsrID);
			param.ParaLong(m_lChannel);

			lRet = param.ParaToPackage(DtlpChannel.FUNC_DESTROYCHANNEL,
				nErrorNo,
				out baSendBuffer );

			if (lRet == -1) 
			{
				return -1; // error
			}
	
			nRet = SendTcpPackage(baSendBuffer,
                baSendBuffer.Length,
				out nErrorNo);
			if (nRet < 0) 
			{
				return -1;
			}

			nRet = RecvTcpPackage(out baRecvBuffer,
				out nLen,
				out nErrorNo);

			if (nRet<0) 
			{
				return -1;
			}
	
			param.Clear();
			param.DefPara(Param.STYLE_LONG);


			int nTempFuncNum = 0;
			param.PackageToPara(baRecvBuffer,
				ref nErrorNo,
				out nTempFuncNum);

			lRet = param.lValue(0);
	
			m_lChannel = -1;

			return lRet;
		}



		// connect()������
		// ԭ����ģ�飬���ȼ��ո�����У�ȥ���ո��ұߡ�
		// Ȼ�󣬿��Ƿ���"()"������У�ȥ���м������(��������)��"()"���Զ�γ���
		public int ConnectSocket(string strHostName,
			out int nErrorNo)
		{
			string strPort = "";
			nErrorNo = 0;

			m_strHostName = strHostName;	// �ӹ�ǰ���ַ���

			int nRet = strHostName.IndexOf(":", 0);
			if (nRet != -1) 
			{
				strPort = strHostName.Substring(nRet+1);
				strPort.Trim();
				strHostName = strHostName.Substring(0, nRet);
				strHostName.Trim();
			}

			int nPort = 3001;
		
			if (strPort != "")
				nPort = Convert.ToInt32(strPort);

			try 
			{

				client = new TcpClient(strHostName, nPort);
			}

			catch (SocketException) 
			{
				nErrorNo = DtlpChannel.GL_CONNECT;
				// �Ƿ񷵻ش����ַ���? ��ȷ���ִ�������
				return -1;
			}

	
	
			return 0;
			/*
	
			ERROR1:
				if (client != null)
					client.Close();

			client = null;
			return -1;
			*/
		}


		public int CloseSocket()
		{
			if (client != null) 
			{
				client.Close();
				client = null;
			}

			return 0;
		}


#if OLD
		// ���������
		public int SendTcpPackage(byte []baPackage,
			int nLen,
			out int nErrorNo)
		{
			// nErrorNo = 0;
			nErrorNo = DtlpChannel.GL_INTR;

			if (client == null)
				return -1;

            try
            {

                NetworkStream stream = client.GetStream();

                stream.Write(baPackage, 0, nLen);
            }
            catch (Exception /*ex*/)  // 2006/11/13 new add
            {
                nErrorNo = DtlpChannel.GL_SEND;

                // 2008/10/7 new add
                if (client != null)
                {
                    client.Close();
                    client = null;
                }


                return -1;
            }


			/*
	int nOutLen;
	int wRet;

	ASSERT(m_hSocket != INVALID_SOCKET);
	
	nOutLen = 0;
	while (nOutLen < nLen) {
		
		
		wRet = send (m_hSocket,
			pPackage + nOutLen,
			nLen-nOutLen,
			0);
		
		if ( wRet==0 || wRet == SOCKET_ERROR ) {
			nErrorNo = WSAGetLastError();
			if (nErrorNo == WSAEWOULDBLOCK)
				continue;
			// close socket
			closesocket(m_hSocket);
			m_hSocket = INVALID_SOCKET;
			return -1;
		}
		nOutLen += wRet;
	}
	*/
	
			nErrorNo = 0;
			return 0;
		}
#else

        // ���������
        public int SendTcpPackage(byte[] baPackage,
            int nLen,
            out int nErrorNo)
        {
            // nErrorNo = 0;
            nErrorNo = DtlpChannel.GL_INTR;

            if (client == null)
                return -1;

            try
            {

                NetworkStream stream = client.GetStream();

                IAsyncResult result = stream.BeginWrite(baPackage, 0, nLen,
                    null, null);
                for (; ; )
                {
                    /*
                    int nRet = Container.Container.Container.procIdle(this);
                    if (nRet == 1)
                        return -1;
                    System.Threading.Thread.Sleep(100);
                     * */
                    if ( Container.Container.Container.DoIdle(this) == true)
                        return -1;

                    if (result.IsCompleted)
                        break;
                }

                stream.EndWrite(result);
            }
            catch (Exception /*ex*/) 
            {
                nErrorNo = DtlpChannel.GL_SEND;

                // 2008/10/7 new add
                if (client != null)
                {
                    client.Close();
                    client = null;
                }
                return -1;
            }

            nErrorNo = 0;
            return 0;
        }

#endif


#if OLD
		// ������Ӧ��
		// �����յ�4byte����֪���˰��ĳߴ�
		public int RecvTcpPackage(out byte []baPackage,
			out int nLen,
			out int nErrorNo)
		{
			// nErrorNo = 0;
			nErrorNo = DtlpChannel.GL_INTR;

			int nInLen;
			int wRet;
			int l;
			bool bInitialLen = false;

			Debug.Assert(client != null, "clientΪ��");
	
			baPackage = new byte [4096];
			nInLen = 0;
			nLen = 4096; //COMM_BUFF_LEN;
	
			while ( nInLen < nLen ) 
			{

				if (Container.Container.Container.procIdle != null)
				{
					if (client != null && client.GetStream().DataAvailable == false) 
					{
						int nRet = Container.Container.Container.procIdle(this);
						if (nRet == 1)
							goto ERROR1;
						System.Threading.Thread.Sleep(100);
						continue;
					}
				}
					

				if (client == null) 
				{
					goto ERROR1;
				}
		
				wRet = client.GetStream().Read(baPackage, 
					nInLen,
					baPackage.Length - nInLen);

				if ( wRet == 0) 
				{
					goto ERROR1;
				}

				// �õ����ĳ���
		
				if ((wRet>=4||nInLen>=4)
					&& bInitialLen == false) 
				{

					l = BitConverter.ToInt32(baPackage, 0);
					l = IPAddress.NetworkToHostOrder((Int32)l);
					nLen = (int)l;

                    if (nLen >= (1000 * 1024))  // 2006/11/26 new add
                    {
                        // ����λ�����쳣
                        goto ERROR1;
                    }

					// ��ʽ���仺�����ߴ�
					byte [] temp = new byte [nLen];
					Array.Copy(baPackage, 0, temp, 0, nInLen + wRet);
					baPackage = temp;

					bInitialLen = true;
				}

				nInLen += wRet;
				if (nInLen >= baPackage.Length
					&& bInitialLen == false) // ��̫���ܷ���
				{
					byte [] temp = new byte [baPackage.Length + 4096];
					Array.Copy(baPackage, 0, temp, 0, nInLen);
					baPackage = temp;
				}
			}

			// �������������ߴ磬�����Ҫ�Ļ�
			if (baPackage.Length > nLen) 
			{
				byte [] temp = new byte [nLen];
				Array.Copy(baPackage, 0, temp, 0, nLen);
				baPackage = temp;

			}

			nErrorNo = 0;
			return 0;
			ERROR1:
				if (client != null) 
				{
					client.Close();
					client = null;
				}

			return -1;
		}
#else  
        // ������Ӧ��
        // �����յ�4byte����֪���˰��ĳߴ�
        public int RecvTcpPackage(out byte[] baPackage,
            out int nLen,
            out int nErrorNo)
        {
            // nErrorNo = 0;
            nErrorNo = DtlpChannel.GL_INTR;

            int nInLen;
            int wRet;
            int l;
            bool bInitialLen = false;

            Debug.Assert(client != null, "clientΪ��");

            baPackage = new byte[4096];
            nInLen = 0;
            nLen = 4096; //COMM_BUFF_LEN;

            NetworkStream stream = client.GetStream();

            while (nInLen < nLen)
            {

                /*
                if (Container.Container.Container.procIdle != null)
                {
                 * */
                    if (client != null && stream.DataAvailable == false)
                    {
                        /*
                        int nRet = Container.Container.Container.procIdle(this);
                        if (nRet == 1)
                            goto ERROR1;
                        System.Threading.Thread.Sleep(100);
                         * */
                        if (Container.Container.Container.DoIdle(this) == true)
                            goto ERROR1;
                        continue;
                    }
                /*
                }
                 * */


                if (client == null)
                {
                    goto ERROR1;
                }

                IAsyncResult result = stream.BeginRead(baPackage, nInLen, baPackage.Length - nInLen,
                    null, null);
                for (; ; )
                {
                    /*
                    int nRet = Container.Container.Container.procIdle(this);
                    if (nRet == 1)
                        goto ERROR1;
                    System.Threading.Thread.Sleep(100);
                     * */
                    if (Container.Container.Container.DoIdle(this) == true)
                        goto ERROR1;

                    if (result.IsCompleted)
                        break;
                }

                wRet = stream.EndRead(result);

                if (wRet == 0)
                {
                    goto ERROR1;
                }

                // �õ����ĳ���

                if ((wRet >= 4 || nInLen >= 4)
                    && bInitialLen == false)
                {

                    l = BitConverter.ToInt32(baPackage, 0);
                    l = IPAddress.NetworkToHostOrder((Int32)l);
                    nLen = (int)l;

                    if (nLen >= (1000 * 1024))  // 2006/11/26 new add
                    {
                        // ����λ�����쳣
                        goto ERROR1;
                    }

                    // ��ʽ���仺�����ߴ�
                    byte[] temp = new byte[nLen];
                    Array.Copy(baPackage, 0, temp, 0, nInLen + wRet);
                    baPackage = temp;

                    bInitialLen = true;
                }

                nInLen += wRet;
                if (nInLen >= baPackage.Length
                    && bInitialLen == false) // ��̫���ܷ���
                {
                    byte[] temp = new byte[baPackage.Length + 4096];
                    Array.Copy(baPackage, 0, temp, 0, nInLen);
                    baPackage = temp;
                }
            }

            // �������������ߴ磬�����Ҫ�Ļ�
            if (baPackage.Length > nLen)
            {
                byte[] temp = new byte[nLen];
                Array.Copy(baPackage, 0, temp, 0, nLen);
                baPackage = temp;
            }

            nErrorNo = 0;
            return 0;
        ERROR1:
            if (client != null)
            {
                client.Close();
                client = null;
            }

            return -1;
        }

#endif


	}
}
