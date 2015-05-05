using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

using DigitalPlatform;


namespace DigitalPlatform.Z3950
{
    public class ZChannel
    {
        public TcpClient client = null;

        public const int DefaultPort = 210;

        public string m_strHostName = "";
        public int m_nPort = 210;

        bool m_bInitialized = false;

        public event CommIdleEventHandle CommIdle = null;

        // �첽���ͺͽ���
        public byte[] baSend = null;
        public byte[] baRecv = null;
        public string strErrorString = "";
        public int nErrorCode = 0;

        internal AutoResetEvent eventClose = new AutoResetEvent(false);	// true : initial state is signaled 
        internal AutoResetEvent eventFinished = new AutoResetEvent(false);	// true : initial state is signaled 

        public int Timeout = 60*1000;   // 60��


        /// <summary>
        /// �Ƿ�Z39.50��ʼ��
        /// </summary>
        public bool Initialized
        {
            get
            {
                return this.m_bInitialized;
            }
            set
            {
                this.m_bInitialized = false;
            }
        }


        public bool Connected
        {
            get
            {
                if (this.client == null)
                    return false;

                return this.client.Connected;
                /*
                if (this.client != null)
                    return true;

                return false;
                 * */
            }
        }

        public string HostName
        {
            get
            {
                return this.m_strHostName;
            }
        }

        public int Port
        {
            get
            {
                return this.m_nPort;
            }
        }

#if NOOOOOOOOOOOOOOO
        // ���������̵߳�SendAndRecv
        public int SendAndRecv(byte[] baSend,
            out byte[] baRecv,
            out int nRecvLength,
            out string strError)
        {
            baRecv = null;
            nRecvLength = 0;
            strError = "";

            this.eventClose.Reset();
            this.eventFinished.Reset();

            this.baSend = baSend;
            this.strErrorString = "";
            this.nErrorCode = 0;

            try
            {
                SendAndRecvThread();
            }
            catch (System.Threading.ThreadAbortException ex)
            {
                strError = "�̱߳�ɱ��";
                goto ERROR1;
            }


            if (nErrorCode != 0)
            {
                if (nErrorCode == -1)
                    this.CloseSocket();

                strError = this.strErrorString;
                return nErrorCode;
            }

            baRecv = this.baRecv;
            nRecvLength = baRecv.Length;
            return 0;

        ERROR1:
            this.CloseSocket();
            return -1;
        }
#endif

        // ������һ���̵߳�SendAndRecv
        public int SendAndRecv(byte[] baSend,
            out byte [] baRecv,
            out int nRecvLength,
            out string strError)
        {
            baRecv = null;
            nRecvLength = 0;
            strError = "";

            this.eventClose.Reset();
            this.eventFinished.Reset();

            this.baSend = baSend;
            this.strErrorString = "";
            this.nErrorCode = 0;

            Thread clientThread = new Thread(new ThreadStart(SendAndRecvThread));
            clientThread.Start();

            // �ȴ��߳̽���
            WaitHandle[] events = new WaitHandle[2];

            events[0] = this.eventClose;
            events[1] = this.eventFinished;

            int nIdleTimeCount = 0;
            int nIdleTicks = 100;

        REDO:
            DoIdle();


            int index = 0;
            try
            {
                index = WaitHandle.WaitAny(events, nIdleTicks, false);
            }
            catch (System.Threading.ThreadAbortException ex)
            {
                strError = "�̱߳�ɱ��";
                goto ERROR1;
            }

            if (index == WaitHandle.WaitTimeout)
            {
                nIdleTimeCount += nIdleTicks;

                if (nIdleTicks >= this.Timeout)
                {
                    // ��ʱ
                    strError = "��ʱ (" + this.Timeout + "����)";
                    return -1;
                }

                goto REDO;
            }
            else if (index == 0)
            {
                // �õ�Close�ź�
                strError = "ͨ�����ж�";
                goto ERROR1;
            }
            else
            {
                // �õ�finish�ź�
                if (nErrorCode != 0)
                {
                    if (nErrorCode == -1)
                        this.CloseSocket();

                    strError = this.strErrorString;
                    return nErrorCode;
                }

                baRecv = this.baRecv;
                nRecvLength = baRecv.Length;
                return 0;
            }

        ERROR1:
            this.CloseSocket();
            return -1;
        }

        void SendAndRecvThread()
        {
            string strError = "";

            int nRet = this.SimpleSendTcpPackage(
                this.baSend,
                this.baSend.Length,
                out strError);
            if (nRet == -1 || nRet == 1)
                goto ERROR1;

            byte [] baPackage = null;
            int nRecvLen = 0;
            nRet = this.SimpleRecvTcpPackage(
                        out baPackage,
                        out nRecvLen,
                        out strError);
            if (nRet == -1)
                goto ERROR1;

#if DEBUG
            if (baPackage != null)
            {
                Debug.Assert(baPackage.Length == nRecvLen, "");
            }
            else
            {
                Debug.Assert(nRecvLen == 0, "");
            }
#endif

            this.baRecv = baPackage;
            
            this.eventFinished.Set();
            return;
        ERROR1:
            this.strErrorString = strError;
            this.nErrorCode = -1;
            this.eventFinished.Set();
            return;
        }

        // ���������
        // return:
        //      -1  ����
        //      0   ��ȷ����
        //      1   ����ǰ������������δ���������
        public int SimpleSendTcpPackage(byte[] baPackage,
            int nLen,
            out string strError)
        {
            strError = "";

            if (client == null)
            {
                strError = "client��δ��ʼ�������������Ӻͼ�����";
                return -1;
            }

            if (this.client == null)
            {
                strError = "�û��ж�";
                return -1;
            }


            NetworkStream stream = client.GetStream();

            if (stream.DataAvailable == true)
            {
                // Debug.Assert(false, "����ǰ��Ȼ������δ��������" );
                strError = "����ǰ����������δ��������";
                return 1;
            }

            try
            {
                stream.Write(baPackage, 0, nLen);
            }
            catch (Exception ex)
            {
                strError = "send����: " + ex.Message;
                // this.CloseSocket();
                return -1;
            }

            return 0;
        }

        // ������Ӧ��
        public int SimpleRecvTcpPackage(out byte[] baPackage,
            out int nLen,
            out string strError)
        {
            strError = "";

            int nInLen;
            int wRet = 0;
            bool bInitialLen = false;

            Debug.Assert(client != null, "clientΪ��");

            baPackage = new byte[4096];
            nInLen = 0;
            nLen = 4096; //COMM_BUFF_LEN;

            while (nInLen < nLen)
            {
                if (client == null)
                {
                    strError = "ͨѶ�ж�";
                    goto ERROR1;
                }

                try
                {

                    wRet = client.GetStream().Read(baPackage,
                        nInLen,
                        baPackage.Length - nInLen);
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10035)
                    {
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }
                    strError = "recv����: " + ex.Message;
                    goto ERROR1;
                }
                catch (Exception ex)
                {
                    strError = "recv����: " + ex.Message;
                    goto ERROR1;
                }

                if (wRet == 0)
                {
                    strError = "Closed by remote peer";
                    goto ERROR1;
                }

                // �õ����ĳ���

                if ((wRet >= 1 || nInLen >= 1)
                    && bInitialLen == false)
                {
                    long remainder = 0;
                    bool bRet = BerNode.IsCompleteBER(baPackage,
                        0,
                        nInLen + wRet,
                        out remainder);
                    if (bRet == true)
                    {
                        nLen = nInLen + wRet;
                        break;
                    }
                }

                nInLen += wRet;
                if (nInLen >= baPackage.Length
                    && bInitialLen == false)
                {
                    // ���󻺳���
                    byte[] temp = new byte[baPackage.Length + 4096];
                    Array.Copy(baPackage, 0, temp, 0, nInLen);
                    baPackage = temp;
                    nLen = baPackage.Length;
                }
            }

            // �������������ߴ磬�����Ҫ�Ļ�
            if (baPackage.Length > nLen)
            {
                byte[] temp = new byte[nLen];
                Array.Copy(baPackage, 0, temp, 0, nLen);
                baPackage = temp;
            }

            return 0;
        ERROR1:
            // this.CloseSocket();
            baPackage = null;
            return -1;
        }

        // �߳�connect()������
        public int NewConnectSocket(string strHostName,
            int nPort,
            out string strError)
        {
            strError = "";

            this.m_strHostName = strHostName;
            this.m_nPort = nPort;


            // ���߳�֮ǰ��̽Close();
            this.CloseSocket();

            this.eventClose.Reset();
            this.eventFinished.Reset();

            this.strErrorString = "";
            this.nErrorCode = 0;


            Thread clientThread = new Thread(new ThreadStart(ConnectThread));
            clientThread.Start();

            // �ȴ��߳̽���
            WaitHandle[] events = new WaitHandle[2];
            events[0] = this.eventClose;
            events[1] = this.eventFinished;

            int nIdleTimeCount = 0;
            int nIdleTicks = 100;

            REDO:
            DoIdle();

            int index = 0;
            try
            {
                index = WaitHandle.WaitAny(events, nIdleTicks, false);
            }
            catch (System.Threading.ThreadAbortException ex)
            {
                strError = "�̱߳�ɱ��";
                return -1;
            }

            if (index == WaitHandle.WaitTimeout)
            {
                nIdleTimeCount += nIdleTicks;

                if (nIdleTicks >= this.Timeout)
                {
                    // ��ʱ
                    strError = "��ʱ (" + this.Timeout + "����)";
                    return -1;
                }

                goto REDO;
            }
            else if (index == 0)
            {
                // �õ�Close�ź�
                strError = "ͨ�����ж�";
                return -1;
            }
            else
            {
                // �õ�finish�ź�
                if (nErrorCode != 0)
                {
                    strError = this.strErrorString;
                    return nErrorCode;
                }
                return 0;
            }

            return -1;
        }

        void ConnectThread()
        {
            string strError = "";

            try
            {
                client = new TcpClient(this.m_strHostName, this.m_nPort);
                // client.NoDelay = true;
            }
            catch (Exception ex)  // SocketException
            {
                strError = "Connect����: " + ex.Message;
                goto ERROR1;
            }

            this.eventFinished.Set();
            return;
        ERROR1:
            this.strErrorString = strError;
            this.nErrorCode = -1;
            this.eventFinished.Set();
            return;
        }

        #region �����첽����

        byte[] m_baSend = null;
        byte[] m_baRecv = null;

        public byte[] RecvPackage
        {
            get
            {
                return this.m_baRecv;
            }
        }

        string m_strAsycError = "";

        public string ErrorInfo
        {
            get
            {
                return m_strAsycError;
            }
        }

        byte[] m_baTempRecv = null;

        public event EventHandler SendRecvComplete = null;
        public event EventHandler ConnectComplete = null;

        // ����
        // return:
        //      -1  ����
        //      0   �ɹ�����
        public int ConnectAsync(string strHostName,
            int nPort)
        {
            m_strAsycError = "";

            try
            {
                this.m_strHostName = strHostName;
                this.m_nPort = nPort;

                client = new TcpClient(AddressFamily.InterNetwork);

                IAsyncResult ar = client.BeginConnect(strHostName, nPort,
                    new AsyncCallback(ConnectCallback),
                    null);
            }
            catch (Exception ex)  // SocketException
            {
                m_strAsycError = "���� "+this.m_strHostName + ":" + this.m_nPort.ToString()+" ����: " + ex.Message;
                ClearHostNameAndPort();
                if (this.ConnectComplete != null)
                    this.ConnectComplete(this, new EventArgs());
                return -1;
            }

            return 0;
        }

        void ClearHostNameAndPort()
        {
            this.m_strHostName = "";
            this.m_nPort = 210;
        }

        void ConnectCallback(IAsyncResult ar)
        {
            m_strAsycError = "";

            if (client == null)
            {
                m_strAsycError = "���Ӳ������û��ж�";
                ClearHostNameAndPort();
                goto END1;
            }

            try
            {
                client.EndConnect(ar);
            }
            catch (Exception ex)  // SocketException
            {
                m_strAsycError = "���� " + this.m_strHostName + ":" + this.m_nPort.ToString() + " ����: " + ex.Message;
                ClearHostNameAndPort();
            }
            END1:
            if (this.ConnectComplete != null)
            {
                this.ConnectComplete(this, new EventArgs());
            }
        }

        // �����������������Ӧ��
        // return:
        //      -1  ����
        //      0   �ɹ�����
        //      1   ����ǰ������������δ���������
        public int SendRecvAsync(byte[] baPackage)
        {
            m_strAsycError = "";

            if (client == null)
            {
                m_strAsycError = "client��δ��ʼ�������������Ӻͼ�����";
                return -1;
            }

            if (this.client == null)
            {
                m_strAsycError = "�û��ж�";
                return -1;
            }

            this.m_baRecv = null;
            this.m_baSend = baPackage;

            NetworkStream stream = client.GetStream();

            if (stream.DataAvailable == true)
            {
                m_strAsycError = "����ǰ����������δ��������";
                return 1;
            }

            try
            {
                IAsyncResult result = stream.BeginWrite(m_baSend,
                    0,
                    m_baSend.Length,
                    new AsyncCallback(SendCallback),
                    null);
            }
            catch (Exception ex)
            {
                m_strAsycError = "���ͳ���: " + ex.Message;
                // this.CloseSocket();
                return -1;
            }

            return 0;
        }

        void SendCallback(IAsyncResult ar)
        {
            m_strAsycError = "";

            NetworkStream stream = client.GetStream();

            stream.EndWrite(ar);
            try
            {
                m_baTempRecv = new byte[4096];

                IAsyncResult result = stream.BeginRead(m_baTempRecv,
                    0,
                    4096,
                    new AsyncCallback(RecvCallback),
                    ar.AsyncState);
            }
            catch (Exception ex)
            {
                m_strAsycError = "���ճ���: " + ex.Message;
                // this.CloseSocket();
                // ���ͺͽ������
                if (SendRecvComplete != null)
                    SendRecvComplete(this, new EventArgs());
                return;
            }
        }

        void RecvCallback(IAsyncResult ar)
        {
            try
            {
                if (client == null)
                {
                    m_strAsycError = "�û��ж�";
                    goto ERROR1;
                }
                Debug.Assert(client != null, "");
                NetworkStream stream = client.GetStream();
                Debug.Assert(stream != null, "");

                int nReaded = stream.EndRead(ar);
                if (nReaded == 0)
                {
                    m_strAsycError = "ͨ�����Է��ж�";
                    goto ERROR1;
                }

                m_baRecv = ByteArray.Add(m_baRecv, m_baTempRecv, nReaded);
                Debug.Assert(m_baRecv != null, "");

                long remainder = 0;
                bool bRet = BerNode.IsCompleteBER(m_baRecv,
        0,
        m_baRecv.Length,
        out remainder);
                if (bRet == true)
                {
                    // ���ͺͽ������
                    if (SendRecvComplete != null)
                        SendRecvComplete(this, new EventArgs());
                    return;
                }


                // �����������
                try
                {
                    m_baTempRecv = new byte[4096];

                    IAsyncResult result = stream.BeginRead(m_baTempRecv,
                        0,
                        4096,
                        new AsyncCallback(RecvCallback),
                        null);
                }
                catch (Exception ex)
                {
                    m_strAsycError = "���ճ���: " + ex.Message;

                    goto ERROR1;
                }
            }
            // System.ObjectDisposedException
            catch (Exception ex)
            {
                m_strAsycError = "RecvCallback()����: " + ExceptionUtil.GetDebugText(ex);

                goto ERROR1;
            }
            return;
        ERROR1:
            // ���ͺͽ������
            if (SendRecvComplete != null)
                SendRecvComplete(this, new EventArgs());
            return;
        }

        #endregion

        ////

        // connect()������
        public int ConnectSocket(string strHostName,
            int nPort,
            out string strError)
        {
            strError = "";

            this.CloseSocket();

            DoIdle();

            this.m_strHostName = strHostName;
            this.m_nPort = nPort;

            try
            {
                client = new TcpClient(strHostName, nPort);

                client.NoDelay = true;
                // client.Client.Blocking = false;
            }
            catch (Exception ex)  // SocketException
            {
                strError = "Connect����: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // �����߳��ж��ź�
        public void Stop()
        {
            this.eventClose.Set();
        }

        public void CloseSocket()
        {
            if (client != null)
            {
                TcpClient temp_client = client;
                this.client = null;

                try
                {
                        temp_client.Close();
                        goto END1;
                }
                catch
                {
                }

#if NO
                try
                {
                    NetworkStream stream = temp_client.GetStream();
                    stream.Close();
                }
                catch { }
                try
                {
                    temp_client.Close();
                }
                catch { }
#endif
            }

            END1:
            this.m_bInitialized = false;

            this.eventClose.Set();
        }

        // �����Ƿ���δ���������
        public bool DataAvailable
        {
            get
            {
                if (client == null)
                    return false;

                NetworkStream stream = client.GetStream();

                if (stream == null)
                    return false;

                bool bOldBlocking = this.client.Client.Blocking;
                this.client.Client.Blocking = true;
                try
                {

                    return stream.DataAvailable;
                }
                finally
                {
                    this.client.Client.Blocking = bOldBlocking;
                }

            }
        }

        // ���������
        // return:
        //      -1  ����
        //      0   ��ȷ����
        //      1   ����ǰ������������δ���������
        public int SendTcpPackage(byte[] baPackage,
            int nLen,
            out string strError)
        {
            strError = "";

            if (client == null)
            {
                strError = "client��δ��ʼ�������������Ӻͼ�����";
                return -1;
            }

            DoIdle();

            if (this.client == null)
            {
                strError = "�û��ж�";
                return -1;
            }

            bool bOldBlocking = this.client.Client.Blocking;
            this.client.Client.Blocking = true;
            try
            {

                NetworkStream stream = client.GetStream();

                if (stream.DataAvailable == true)
                {
                    // Debug.Assert(false, "����ǰ��Ȼ������δ��������" );
                    strError = "����ǰ����������δ��������";
                    return 1;
                }

                try
                {
                    stream.Write(baPackage, 0, nLen);
                }
                catch (Exception ex)
                {
                    strError = "send����: " + ex.Message;
                    this.CloseSocket();
                    return -1;
                }

                // stream.Flush();

                return 0;
            }
            finally
            {
                this.client.Client.Blocking = bOldBlocking;
            }
        }

        bool DoIdle()
        {
            if (CommIdle == null)
                return false;

            CommIdleEventArgs e = new CommIdleEventArgs();
            this.CommIdle(this, e);
            if (e.Cancel == true)
                return true;

            return false;
        }

        // ������Ӧ��
        public int RecvTcpPackage(out byte[] baPackage,
            out int nLen,
            out string strError)
        {
            strError = "";

            int nInLen;
            int wRet = 0;
            bool bInitialLen = false;

            Debug.Assert(client != null, "clientΪ��");

            baPackage = new byte[4096];
            nInLen = 0;
            nLen = 4096; //COMM_BUFF_LEN;

            long lIdleCount = 0;

            while (nInLen < nLen)
            {
                if (client != null)
                {
                    /*
                    if (client.Client.Poll(-1, SelectMode.SelectError))
                    {
                        strError = "ͨѶ�ж�0";
                        goto ERROR1;
                    }


                    if (client.Client.Connected == false)
                    {
                        strError = "ͨѶ�ж�0";
                        goto ERROR1;
                    }
                     * */
                }

                if (CommIdle != null 
                    && client != null
                    && lIdleCount < 20)
                {
                    int nCount = 0;

                    try
                    {
                        nCount = client.Available;
                    }
                    catch (SocketException ex)
                    {
                        if (ex.ErrorCode == 0)
                        {
                            strError = "ͨѶ�ж�0";
                            goto ERROR1;
                        }

                    }
                    if (nCount == 0)
                    {
                        CommIdleEventArgs e = new CommIdleEventArgs();
                        this.CommIdle(this, e);
                        if (e.Cancel == true)
                        {
                            strError = e.ErrorInfo;
                            goto ERROR1;
                        }
                        System.Threading.Thread.Sleep(10);
                        lIdleCount++;
                        continue;
                    }
                }

                if (client == null)
                {
                    strError = "ͨѶ�ж�";
                    goto ERROR1;
                }

                try
                {

                    wRet = client.GetStream().Read(baPackage,
                        nInLen,
                        baPackage.Length - nInLen);

                    lIdleCount = 0;

                    /*
                    wRet = client.Client.Receive(baPackage,
                        nInLen,
                        SocketFlags.None);
                     * */
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10035)
                    {
                        CommIdleEventArgs e = new CommIdleEventArgs();
                        this.CommIdle(this, e);
                        if (e.Cancel == true)
                        {
                            strError = e.ErrorInfo;
                            goto ERROR1;
                        }
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }
                    strError = "recv����: " + ex.Message;
                    goto ERROR1;
                }
                catch (Exception ex)
                {
                    strError = "recv����: " + ex.Message;
                    goto ERROR1;
                }

                if (wRet == 0)
                {
                    strError = "Closed by remote peer";
                    goto ERROR1;
                }

                // �õ����ĳ���

                if ((wRet >= 1 || nInLen >= 1)
                    && bInitialLen == false)
                {
                    long remainder = 0;
                    bool bRet = BerNode.IsCompleteBER(baPackage,
                        0,
                        nInLen + wRet,
                        out remainder);
                    if (bRet == true)
                    {
                        /*
                        // ��ʽ���仺�����ߴ�
                        byte[] temp = new byte[nLen];
                        Array.Copy(baPackage, 0, temp, 0, nInLen + wRet);
                        baPackage = temp;

                        bInitialLen = true;
                         * */
                        nLen = nInLen + wRet;
                        break;
                    }
                }

                nInLen += wRet;
                if (nInLen >= baPackage.Length
                    && bInitialLen == false)
                {
                    // ���󻺳���
                    byte[] temp = new byte[baPackage.Length + 4096];
                    Array.Copy(baPackage, 0, temp, 0, nInLen);
                    baPackage = temp;
                    nLen = baPackage.Length;
                }
            }

            // �������������ߴ磬�����Ҫ�Ļ�
            if (baPackage.Length > nLen)
            {
                byte[] temp = new byte[nLen];
                Array.Copy(baPackage, 0, temp, 0, nLen);
                baPackage = temp;
            }

            return 0;
        ERROR1:
            this.CloseSocket();
            baPackage = null;
            return -1;
        }

    }


    // �¼�: ͨѶ����
    public delegate void CommIdleEventHandle(object sender,
    CommIdleEventArgs e);

    public class CommIdleEventArgs : EventArgs
    {
        public bool Cancel = false;
        public string ErrorInfo = "";
    }

}
