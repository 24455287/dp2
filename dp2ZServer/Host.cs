using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.IO;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using System.ServiceModel;
using System.ServiceModel.Description;

using Microsoft.Win32;

using DigitalPlatform.IO;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.CirculationClient;

namespace dp2ZServer
{
    public partial class Service : ServiceBase
    {
        ServiceHost m_hostUnionCatalog = null;
        Thread m_threadLoadUnionCatalog = null;
        bool m_bConsoleRun = false;

        internal AutoResetEvent eventClose = new AutoResetEvent(false);	// true : initial state is signaled 
        internal AutoResetEvent eventActive = new AutoResetEvent(false);	// �����ź�

        private TcpListener Listener = null;

        private string m_IPAddress = "ALL";   // Holds IP Address, which to listen incoming calls.
        private int m_port = 210;      // �˿ں�
        private int m_nMaxThreads = -1;      // Holds maximum allowed Worker Threads.

        private Hashtable m_SessionTable = null;


        public XmlDocument CfgDom = null;   // z.xml�����ļ�����
        public string LibraryServerUrl = "";
        public string ManagerUserName = ""; // ����Ա�õ��û���
        public string ManagerPassword = ""; // ����Ա�õ�����

        public string AnonymousUserName = "";   // ������¼�õ��û���
        public string AnonymousPassword = "";   // ������¼�õ�����

        string EncryptKey = "dp2zserver_password_key";

        public EventLog Log = null;


        // ר�������ϵͳ��Ϣ��dp2libraryͨ��
        public LibraryChannel Channel = new LibraryChannel();

        public List<BiblioDbProperty> BiblioDbProperties = null;

        // ���п���ܹ��������м�¼��������
        public int MaxResultCount = -1;

        public Service()
        {
            InitializeComponent();

            // ��ʼ���¼���־
            this.Log = new EventLog();
            this.Log.Source = "dp2ZServer";

            this.m_threadLoadUnionCatalog = new Thread(new ThreadStart(ThreadLoadUnionCatalog));
        }

        public static void Main(string[] args)
        {
            if (args.Length == 1 && args[0].Equals("console"))
            {
                new Service().ConsoleRun();
            }
            else
            {
                ServiceBase.Run(new Service());
            }
        }

        private void ConsoleRun()
        {
            this.m_bConsoleRun = true;

            Console.WriteLine("{0}::starting...", GetType().FullName);

            OnStart(null);

            Console.WriteLine("{0}::ready (ENTER to exit)", GetType().FullName);

            Console.ReadLine();

            OnStop();
            Console.WriteLine("{0}::stopped", GetType().FullName);
        }

        protected override void OnStart(string[] args)
        {
            this.Log.WriteEntry("dp2ZServer OnStart() begin",
    EventLogEntryType.Information);
            
            try
            {
                if (!this.DesignMode)
                {
                    m_SessionTable = new Hashtable();

                    Thread startZ3950Server = new Thread(new ThreadStart(Run));
                    startZ3950Server.Start();

                    Thread defaultManagerThread = new Thread(new ThreadStart(ManagerRun));
                    defaultManagerThread.Start();
                }
            }
            catch (Exception x)
            {
                Log.WriteEntry("dp2ZServer OnStart() error : " + x.Message, 
                    EventLogEntryType.Error);
            }
            /*
����dp2Zserver�Զ�������ϵͳ��־�б�������⣺
             * 
http://www.devnewsgroups.net/group/microsoft.public.dotnet.framework.windowsforms/topic15625.aspx
...
Hi,

We had the very same problem - .NET service failed to start
automatically during machine reboot (timeout) but started without
problems when started later manually.
This seems to be a potential problem for any .NET service on a slower
machine (or heavy loaded one) - it just takes some time for CLR to
translate from MSIL to native code and after restart many other
services are being started simultaneously, so sometimes the default
timeout of 30 seconds is not enough :(

The solution was quite simple: we increased the timeout in registry
(HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\ServicesPipeTimeout) ע������һ��DWORD��������
and since then our service starts without problems :)
             * */

            // UnionCatalog
            this.m_threadLoadUnionCatalog.Start();

            this.Log.WriteEntry("dp2ZServer OnStart() end",
EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            try
            {
                if (Listener != null)
                {
                    Listener.Stop();
                }
            }
            catch (Exception x)
            {
                Log.WriteEntry("dp2ZServer OnStop() error : " + x.Message,
                    EventLogEntryType.Error);
            }

            // UnionCatalog
            if (this.m_threadLoadUnionCatalog != null)
            {
                this.m_threadLoadUnionCatalog.Abort();
                this.m_threadLoadUnionCatalog = null;
            }

            if (this.m_hostUnionCatalog != null)
            {
                this.m_hostUnionCatalog.Close();
                this.m_hostUnionCatalog = null;
            }
        }

        #region UnionCatalog

        void ThreadLoadUnionCatalog()
        {
            if (this.m_hostUnionCatalog != null)
            {
                this.m_hostUnionCatalog.Close();
                this.m_hostUnionCatalog = null;
            }

            string strInstanceName = "";
            string strDataDir = "";
            string[] existing_urls = null;
            bool bRet = GetInstanceInfo("dp2ZServer",
                0,
                out strInstanceName,
                out strDataDir,
                out existing_urls);
            if (bRet == false)
            {
                /*
                this.Log.WriteEntry("dp2ZServer OnStart() ʱ��������: ע������Ҳ���instance��Ϣ",
EventLogEntryType.Error);
                 * */
                return;
            }

            string strHttpHostUrl = FindUrl("http", existing_urls);
            if (string.IsNullOrEmpty(strHttpHostUrl) == true)
            {
                string strUrls = string.Join(";", existing_urls);
                this.Log.WriteEntry("dp2ZServer OnStart() ʱ��������: Э��� '"+strUrls+"' �У�û�а���httpЭ�飬���û������UnionCatalogService",
EventLogEntryType.Error);
                return;
            }

            this.m_hostUnionCatalog = new ServiceHost(typeof(UnionCatalogService));

            HostInfo info = new HostInfo();
            info.DataDir = strDataDir;
            this.m_hostUnionCatalog.Extensions.Add(info);

            if (String.IsNullOrEmpty(strHttpHostUrl) == false)
            {
                this.m_hostUnionCatalog.AddServiceEndpoint(typeof(IUnionCatalogService),
                    CreateBasicHttpBinding0(),
                    strHttpHostUrl);
            }

            // metadata����
            if (this.m_hostUnionCatalog.Description.Behaviors.Find<ServiceMetadataBehavior>() == null)
            {
                string strMetadataUrl = strHttpHostUrl;
                if (String.IsNullOrEmpty(strMetadataUrl) == true)
                    strMetadataUrl = "http://localhost/unioncatalog/";
                if (strMetadataUrl[strMetadataUrl.Length - 1] != '/')
                    strMetadataUrl += "/";
                strMetadataUrl += "metadata";

                ServiceMetadataBehavior behavior = new ServiceMetadataBehavior();
                behavior.HttpGetEnabled = true;
                behavior.HttpGetUrl = new Uri(strMetadataUrl);
                this.m_hostUnionCatalog.Description.Behaviors.Add(behavior);
            }

            if (this.m_hostUnionCatalog.Description.Behaviors.Find<ServiceThrottlingBehavior>() == null)
            {
                ServiceThrottlingBehavior behavior = new ServiceThrottlingBehavior();
                behavior.MaxConcurrentCalls = 50;
                behavior.MaxConcurrentInstances = 1000;
                behavior.MaxConcurrentSessions = 1000;
                this.m_hostUnionCatalog.Description.Behaviors.Add(behavior);
            }

            // IncludeExceptionDetailInFaults
            ServiceDebugBehavior debug_behavior = this.m_hostUnionCatalog.Description.Behaviors.Find<ServiceDebugBehavior>();
            if (debug_behavior == null)
            {
                this.m_hostUnionCatalog.Description.Behaviors.Add(new ServiceDebugBehavior() { IncludeExceptionDetailInFaults = true });
            }
            else
            {
                if (debug_behavior.IncludeExceptionDetailInFaults == false)
                    debug_behavior.IncludeExceptionDetailInFaults = true;
            }

            this.m_hostUnionCatalog.Opening += new EventHandler(host_Opening);
            this.m_hostUnionCatalog.Closing += new EventHandler(m_host_Closing);

            try
            {
                this.m_hostUnionCatalog.Open();
            }
            catch (Exception ex)
            {
                // �õ������ܸо���
                if (this.m_bConsoleRun == true)
                    throw ex;

                this.Log.WriteEntry("dp2ZServer OnStart() host.Open() ʱ��������: " + ex.Message,
EventLogEntryType.Error);
                return;
            }

            this.Log.WriteEntry("dp2ZServer OnStart() end",
EventLogEntryType.Information);

            this.m_threadLoadUnionCatalog = null;
        }

        void m_host_Closing(object sender, EventArgs e)
        {

        }

        void host_Opening(object sender, EventArgs e)
        {

        }

        // bs0: 
        System.ServiceModel.Channels.Binding CreateBasicHttpBinding0()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Namespace = "http://dp2003.com/unioncatalog/";
            binding.Security.Mode = BasicHttpSecurityMode.None;
            binding.MaxReceivedMessageSize = 1024 * 1024;
            binding.MessageEncoding = WSMessageEncoding.Mtom;
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = 1024 * 1024;
            quotas.MaxStringContentLength = 1024 * 1024;
            binding.ReaderQuotas = quotas;
            binding.SendTimeout = new TimeSpan(0, 20, 0);
            binding.ReceiveTimeout = new TimeSpan(0, 20, 0);    // ����Session���

            return binding;
        }

        // ���instance��Ϣ
        // parameters:
        //      urls ��ð󶨵�Urls
        // return:
        //      false   instanceû���ҵ�
        //      true    �ҵ�
        public static bool GetInstanceInfo(string strProductName,
            int nIndex,
            out string strInstanceName,
            out string strDataDir,
            out string[] urls)
        {
            strInstanceName = "";
            strDataDir = "";
            urls = null;

            string strLocation = "SOFTWARE\\DigitalPlatform";

            /*
            if (Environment.Is64BitProcess == true)
                strLocation = "SOFTWARE\\Wow6432Node\\DigitalPlatform";
             * */

            using (RegistryKey digitalplatform = Registry.LocalMachine.CreateSubKey(strLocation))
            {
                using (RegistryKey product = digitalplatform.CreateSubKey(strProductName))
                {
                    RegistryKey instance = product.OpenSubKey("instance_" + nIndex.ToString());
                    if (instance == null)
                        return false;   // not found

                    using (instance)
                    {
                        strInstanceName = (string)instance.GetValue("name");

                        strDataDir = (string)instance.GetValue("datadir");

                        urls = (string[])instance.GetValue("bindings");
                        if (urls == null)
                            urls = new string[0];

                        return true;    // found
                    }
                }
            }
        }

        // ����Э�����ҵ�һ��URL
        public static string FindUrl(string strProtocol,
            string[] urls)
        {
            for (int i = 0; i < urls.Length; i++)
            {
                string strUrl = urls[i].Trim();
                if (String.IsNullOrEmpty(strUrl) == true)
                    continue;

                try
                {
                    Uri uri = new Uri(strUrl);
                    if (uri.Scheme.ToLower() == strProtocol.ToLower())
                        return strUrl;
                }
                catch
                {
                }

            }

            return null;
        }

        #endregion

        // װ�������ļ�dp2zserver.xml
        int LoadCfgDom(out string strError)
        {
            lock (this)
            {
                strError = "";
                int nRet = 0;

                /*
                string strDir = Directory.GetCurrentDirectory();

                strDir = PathUtil.MergePath(strDir, "dp2zserver");
                 * */
                string strCurrentDir = System.Reflection.Assembly.GetExecutingAssembly().Location;   //  Environment.CurrentDirectory;

                strCurrentDir = PathUtil.PathPart(strCurrentDir);


                string strFileName = PathUtil.MergePath(strCurrentDir, "dp2zserver.xml");

                this.CfgDom = new XmlDocument();

                try
                {
                    this.CfgDom.Load(strFileName);
                }
                catch (Exception ex)
                {
                    strError = "�������ļ� '" + strFileName + "' װ�ص�DOMʱ����: " + ex.Message;
                    return -1;
                }


                // ȡ���������
                XmlNode nodeNetwork = this.CfgDom.DocumentElement.SelectSingleNode("//network");
                if (nodeNetwork != null)
                {
                    // port

                    // ��������͵����Բ���ֵ
                    // return:
                    //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
                    //      0   ���������ȷ����Ĳ���ֵ
                    //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
                    nRet = DomUtil.GetIntegerParam(nodeNetwork,
                        "port",
                        210,
                        out this.m_port,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "<network>Ԫ��" + strError;
                        return -1;
                    }

                    // maxSessions

                    // ��������͵����Բ���ֵ
                    // return:
                    //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
                    //      0   ���������ȷ����Ĳ���ֵ
                    //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
                    nRet = DomUtil.GetIntegerParam(nodeNetwork,
                        "maxSessions",
                        -1,
                        out this.m_nMaxThreads,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "<network>Ԫ��" + strError;
                        return -1;
                    }

                }

                // ȡ��һЩ���õ�ָ��

                // 1) ͼ���Ӧ�÷�����URL
                // 2) ����Ա�õ��ʻ�������
                XmlNode node = this.CfgDom.DocumentElement.SelectSingleNode("//libraryserver");
                if (node != null)
                {
                    this.LibraryServerUrl = DomUtil.GetAttr(node, "url");

                    this.ManagerUserName = DomUtil.GetAttr(node, "username");
                    string strPassword = DomUtil.GetAttr(node, "password");
                    this.ManagerPassword = DecryptPasssword(strPassword);

                    this.AnonymousUserName = DomUtil.GetAttr(node, "anonymousUserName");
                    strPassword = DomUtil.GetAttr(node, "anonymousPassword");
                    this.AnonymousPassword = DecryptPasssword(strPassword);
                }
                else
                {
                    this.LibraryServerUrl = "";

                    this.ManagerUserName = "";
                    this.ManagerUserName = "";

                    this.AnonymousUserName = "";
                    this.AnonymousPassword = "";
                }


                // ׼��ͨ��
                this.Channel.Url = this.LibraryServerUrl;

                this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
                this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

                //

                XmlNode nodeDatabases = this.CfgDom.DocumentElement.SelectSingleNode("databases");
                if (nodeDatabases != null)
                {
                    // maxResultCount

                    // ��������͵����Բ���ֵ
                    // return:
                    //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
                    //      0   ���������ȷ����Ĳ���ֵ
                    //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
                    nRet = DomUtil.GetIntegerParam(nodeDatabases,
                        "maxResultCount",
                        -1,
                        out this.MaxResultCount,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "<databases>Ԫ��" + strError;
                        return -1;
                    }

                }

                return 0;
            }
        }

        // ���һЩ�ȽϺ�ʱ�����ò�����
        // return:
        //      -2  �����������������
        //      -1  �������治������
        //      0   �ɹ�
        int GetSlowCfgInfo(out string strError)
        {
            lock (this)
            {
                strError = "";
                int nRet = 0;

                // Ԥ�Ȼ�ñ�Ŀ�������б�
                nRet = GetBiblioDbProperties(out strError);
                if (nRet == -1)
                    return -2;

                // Ϊ���ݿ����Լ�����������Ҫ��xml�ļ��л�õ���������
                nRet = AppendDbProperties(out strError);
                if (nRet == -1)
                    return -1;

                return 0;
            }
        }

        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            if (e.FirstTry == true)
            {
                e.UserName = this.ManagerUserName;
                e.Password = this.ManagerPassword;
                e.Parameters = "location=z39.50 server manager,type=worker";
                /*
                e.IsReader = false;
                e.Location = "z39.50 server manager";
                 * */
                if (String.IsNullOrEmpty(e.UserName) == true)
                {
                    e.ErrorInfo = "û��ָ�������û������޷��Զ���¼";
                    e.Failed = true;
                    return;
                }

                return;
            }

            e.ErrorInfo = "z39.50 service first tryʧ�ܺ��޷��Զ���¼";
            e.Failed = true;
            return;
        }

        public string DecryptPasssword(string strEncryptedText)
        {
            if (String.IsNullOrEmpty(strEncryptedText) == false)
            {
                try
                {
                    string strPassword = Cryptography.Decrypt(
        strEncryptedText,
        EncryptKey);
                    return strPassword;
                }
                catch
                {
                    return "errorpassword";
                }

            }

            return "";
        }

        public string EncryptPassword(string strPlainText)
        {
            return Cryptography.Encrypt(strPlainText, this.EncryptKey);
        }

        DateTime m_lastRetryTime = DateTime.Now;
        int m_nRetryAfterMinutes = 5;   // ÿ������ٷ����Ժ�����һ��

        private void ManagerRun()
        {
            REDO:
            try
            {
                WaitHandle[] events = new WaitHandle[2];

                events[0] = eventClose;
                events[1] = eventActive;

                while (true)
                {
                    int index = 0;
                    try
                    {
                        index = WaitHandle.WaitAny(events, 1000, false);
                    }
                    catch (System.Threading.ThreadAbortException /*ex*/)
                    {
                        return;
                    }

                    if (index == WaitHandle.WaitTimeout)
                    {
                        // ��ʱ
                        eventActive.Reset();
                    }
                    else if (index == 0)
                    {
                        // Close
                        return;
                    }
                    else
                    {
                        // �õ������ź�
                        eventActive.Reset();
                    }

                    List<String> keys = new List<string>();

                    // ������ʱ��Ҫ��
                    lock (this.m_SessionTable)
                    {
                        foreach (string key in this.m_SessionTable.Keys)
                        {
                            keys.Add(key);
                        }
                    }

                    // ��������Χ����ݴ���
                    foreach (string key in keys)
                    {
                        Session session = (Session)this.m_SessionTable[key];
                        if (session == null)
                            continue;

                        TimeSpan delta = DateTime.Now - session.ActivateTime;
                        if (delta.TotalMinutes > 5) // 20
                            RemoveSession(key);
                    }

                    if (this.BiblioDbProperties == null
                        && (DateTime.Now - this.m_lastRetryTime).TotalMinutes >= m_nRetryAfterMinutes)  // ÿ������������������һ��
                    {
                        string strError = "";
                        // return:
                        //      -2  �����������������
                        //      -1  �������治������
                        //      0   �ɹ�
                        int nRet = GetSlowCfgInfo(out strError);
                        if (nRet == -1 || nRet == -2)
                        {
                            Log.WriteEntry("ERR003 ��ʼ����Ϣʧ��(ϵͳ����������): " + strError,
                                EventLogEntryType.Error);
                        }

                        m_lastRetryTime = DateTime.Now;
                        m_nRetryAfterMinutes++; // �����Եļ��ʱ�𽥱䳤���˾ٿ��������Զ�β��ɹ�������£���������־�ļ���д��������Ŀ
                    }

                }
            }
            catch (Exception ex)
            {
                Log.WriteEntry("Manager Thread exception: " + ex.Message,
                    EventLogEntryType.Error);
                goto REDO;
            }
        }

        private void Run()
        {
            eventClose.Reset();
            try
            {
                string strError = "";

                // Log.WriteEntry("dp2ZServer service start step 1");

                // װ�������ļ�
                int nRet = LoadCfgDom(out strError);
                if (nRet == -1)
                {
                    Log.WriteEntry("dp2ZServer error : " + strError,
                        EventLogEntryType.Error);
                    return;
                }

                // return:
                //      -2  �����������������
                //      -1  �������治������
                //      0   �ɹ�
                nRet = GetSlowCfgInfo(out strError);
                if (nRet == -1)
                {
                    Log.WriteEntry("ERR001 �״γ�ʼ����Ϣʧ��(ϵͳ��������): " + strError,
                        EventLogEntryType.Error);
                    return;
                } 
                if (nRet == -2)
                {
                    Log.WriteEntry("ERR002 �״γ�ʼ����Ϣʧ��(ϵͳ����������): " + strError,
                        EventLogEntryType.Error);
                }

                // check which ip's to listen (all or assigned)
                if (m_IPAddress.ToLower().IndexOf("all") > -1)
                {
                    Listener = new TcpListener(IPAddress.Any, m_port);
                }
                else
                {
                    Listener = new TcpListener(IPAddress.Parse(m_IPAddress), m_port);
                }

                // Log.WriteEntry("dp2ZServer service start step 3");


                // Start listening
                Listener.Start();


                //-------- Main Server message loop --------------------------------//
                while (true)
                {
                    // Check if maximum allowed thread count isn't exceeded
                    if (this.m_nMaxThreads == -1
                        || m_SessionTable.Count <= this.m_nMaxThreads)
                    {

                        // Thread is sleeping, until a client connects
                        TcpClient client = Listener.AcceptTcpClient();

                        string sessionID = client.GetHashCode().ToString();

                        //****
                        // _LogWriter logWriter = new _LogWriter(this.SessionLog);
                        Session session = new Session(client, this, sessionID);

                        Thread clientThread = new Thread(new ThreadStart(session.Processing));

                        // Add session to session list
                        AddSession(sessionID, session);

                        // Start proccessing
                        clientThread.Start();
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
                // string dummy = e.Message;     // Neede for to remove compile warning

                Thread.CurrentThread.Abort();
            }
            catch (Exception x)
            {
                // һ�������������� WSACancelBlockingCall �ĵ����ж�
                if (x.Message != "A blocking operation was interrupted by a call to WSACancelBlockingCall")
                {

                    Log.WriteEntry("dp2ZServer Exception Name: " + x.GetType().Name + ", Listener::Run() error : " + x.Message,
                        EventLogEntryType.Error);
                }
            }
            finally
            {
                eventClose.Set();
            }
        }

        /// <summary>
        /// Removes session.
        /// </summary>
        /// <param name="sessionID">Session ID.</param>
        /// <param name="logWriter">Log writer.</param>
        internal void RemoveSession(string sessionID)
        {
            lock (m_SessionTable)
            {
                if (!m_SessionTable.Contains(sessionID))
                {
                    // OnSysError(new Exception("Session '" + sessionID + "' doesn't exist."),new System.Diagnostics.StackTrace());
                    return;
                }

                Session session = (Session)m_SessionTable[sessionID];
                if (session != null)
                {
                    session.Dispose();
                }

                m_SessionTable.Remove(sessionID);
            }

            /*
            if(m_LogCmds)
            {
                logWriter.AddEntry("//----- Sys: 'Session:'" + sessionID + " removed " + DateTime.Now);
            }
            */
        }

        /// <summary>
        /// Adds session.
        /// </summary>
        /// <param name="sessionID">Session ID.</param>
        /// <param name="session">Session object.</param>
        internal void AddSession(string sessionID,
            Session session)
        {
            lock (m_SessionTable)
            {
                m_SessionTable.Add(sessionID, session);
            }

            /*
            if(m_LogCmds)
            {
                logWriter.AddEntry("//----- Sys: 'Session:'" + sessionID + " added " + DateTime.Now);
            }
            */
        }


        // ��ñ�Ŀ�������б�
        int GetBiblioDbProperties(out string strError)
        {
            strError = "";
            try
            {
                this.BiblioDbProperties = new List<BiblioDbProperty>();

                string strValue = "";
                long lRet = Channel.GetSystemParameter(null,
                    "biblio",
                    "dbnames",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " ��ñ�Ŀ�����б���̷�������" + strError;
                    goto ERROR1;
                }

                string[] biblioDbNames = strValue.Split(new char[] { ',' });

                for (int i = 0; i < biblioDbNames.Length; i++)
                {
                    BiblioDbProperty property = new BiblioDbProperty();
                    property.DbName = biblioDbNames[i];
                    this.BiblioDbProperties.Add(property);
                }

                // ����﷨��ʽ
                lRet = Channel.GetSystemParameter(null,
                    "biblio",
                    "syntaxs",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " ��ñ�Ŀ�����ݸ�ʽ�б���̷�������" + strError;
                    goto ERROR1;
                }

                string[] syntaxs = strValue.Split(new char[] { ',' });

                if (syntaxs.Length != this.BiblioDbProperties.Count)
                {
                    strError = "��Է����� " + Channel.Url + " ��ñ�Ŀ����Ϊ " + this.BiblioDbProperties.Count.ToString() + " ���������ݸ�ʽΪ " + syntaxs.Length.ToString() + " ����������һ��";
                    goto ERROR1;
                }

                // �������ݸ�ʽ
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    this.BiblioDbProperties[i].Syntax = syntaxs[i];
                }


                ///

                // ��ö�Ӧ��ʵ�����
                lRet = Channel.GetSystemParameter(null,
                    "item",
                    "dbnames",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " ���ʵ������б���̷�������" + strError;
                    goto ERROR1;
                }

                string[] itemdbnames = strValue.Split(new char[] { ',' });

                if (itemdbnames.Length != this.BiblioDbProperties.Count)
                {
                    strError = "��Է����� " + Channel.Url + " ��ñ�Ŀ����Ϊ " + this.BiblioDbProperties.Count.ToString() + " ������ʵ�����Ϊ " + itemdbnames.Length.ToString() + " ����������һ��";
                    goto ERROR1;
                }

                // �������ݸ�ʽ
                for (int i = 0; i < this.BiblioDbProperties.Count; i++)
                {
                    this.BiblioDbProperties[i].ItemDbName = itemdbnames[i];
                }


                // ����������ݿ���
                lRet = Channel.GetSystemParameter(null,
                    "virtual",
                    "dbnames",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��Է����� " + Channel.Url + " �����������б���̷�������" + strError;
                    goto ERROR1;
                }
                string[] virtualDbNames = strValue.Split(new char[] { ',' });

                for (int i = 0; i < virtualDbNames.Length; i++)
                {
                    BiblioDbProperty property = new BiblioDbProperty();
                    property.DbName = virtualDbNames[i];
                    property.IsVirtual = true;
                    this.BiblioDbProperties.Add(property);
                }


            }
            finally
            {
            }

            return 0;
        ERROR1:
            this.BiblioDbProperties = null;
            return -1;
        }

        // Ϊ���ݿ����Լ�����������Ҫ��xml�ļ��л�õ���������
        int AppendDbProperties(out string strError)
        {
            strError = "";

            // ����MaxResultCount
            if (this.CfgDom == null)
            {
                strError = "���� GetBiblioDbProperties()��ǰ����Ҫ�ȳ�ʼ����װ��CfgDom";
                return -1;
            }

            Debug.Assert(this.CfgDom != null, "");

            for (int i = 0; i < this.BiblioDbProperties.Count; i++)
            {
                BiblioDbProperty prop = this.BiblioDbProperties[i];

                string strDbName = prop.DbName;

                XmlNode nodeDatabase = this.CfgDom.DocumentElement.SelectSingleNode("//databases/database[@name='"+strDbName+"']");
                if (nodeDatabase == null)
                    continue;

                // maxResultCount

                // ��������͵����Բ���ֵ
                // return:
                //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
                //      0   ���������ȷ����Ĳ���ֵ
                //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
                int nRet = DomUtil.GetIntegerParam(nodeDatabase,
                    "maxResultCount",
                    -1,
                    out prop.MaxResultCount,
                    out strError);
                if (nRet == -1)
                {
                    strError = "Ϊ���ݿ� '" + strDbName + "' ���õ�<databases/database>Ԫ�ص�" + strError;
                    return -1;
                }

                // alias
                prop.DbNameAlias = DomUtil.GetAttr(nodeDatabase, "alias");


                // addField901
                // 2007/12/16
                nRet = DomUtil.GetBooleanParam(nodeDatabase,
                    "addField901",
                    false,
                    out prop.AddField901,
                    out strError);
                if (nRet == -1)
                {
                    strError = "Ϊ���ݿ� '" + strDbName + "' ���õ�<databases/database>Ԫ�ص�" + strError;
                    return -1;
                }
            }


            return 0;
        }

        // ������Ŀ���������Ŀ�����Զ���
        public BiblioDbProperty GetDbProperty(string strBiblioDbName,
            bool bSearchAlias)
        {
            for (int i = 0; i < this.BiblioDbProperties.Count; i++)
            {
                if (this.BiblioDbProperties[i].DbName == strBiblioDbName)
                {
                    return this.BiblioDbProperties[i];
                }

                if (bSearchAlias == true)
                {
                    if (this.BiblioDbProperties[i].DbNameAlias.ToLower() == strBiblioDbName.ToLower())
                    {
                        return this.BiblioDbProperties[i];
                    }
                }

            }

            return null;
        }


        // ������Ŀ�������MARC��ʽ�﷨��
        public string GetMarcSyntax(string strBiblioDbName)
        {
            for (int i = 0; i < this.BiblioDbProperties.Count; i++)
            {
                if (this.BiblioDbProperties[i].DbName == strBiblioDbName)
                {
                    string strResult = this.BiblioDbProperties[i].Syntax;
                    if (String.IsNullOrEmpty(strResult) == true)
                        strResult = "unimarc";  // ȱʡΪunimarc
                    return strResult;
                }
            }

            // 2007/8/9
            // �����this.BiblioDbProperties�����Ҳ���������ֱ����xml���õ�<database>Ԫ������
            XmlNode nodeDatabase = this.CfgDom.DocumentElement.SelectSingleNode("//databases/database[@name='" + strBiblioDbName + "']");
            if (nodeDatabase == null)
                return null;

            return DomUtil.GetAttr(nodeDatabase, "marcSyntax");
        }

        // ������Ŀ����(���߱���)��ü���;����
        // parameters:
        //      strOutputDbName ��������ݿ���������Z39.50����������ı����������������ݿ�����
        public string GetFromName(string strDbNameOrAlias,
            long lAttributeValue,
            out string strOutputDbName,
            out string strError)
        {
            strError = "";
            strOutputDbName = "";

            // ��ΪXMLDOM���޷����д�Сд�����е����������԰�����������������񽻸�properties
            Debug.Assert(this.CfgDom != null, "");
            BiblioDbProperty prop = this.GetDbProperty(strDbNameOrAlias, true);
            if (prop == null)
            {
                strError = "���ֻ��߱���Ϊ '" + strDbNameOrAlias + "' �����ݿⲻ����";
                return null;
            }

            strOutputDbName = prop.DbName;


            XmlNode nodeDatabase = this.CfgDom.DocumentElement.SelectSingleNode("//databases/database[@name='" + strOutputDbName + "']");

            if (nodeDatabase == null)
            {
                strError = "����Ϊ '" + strOutputDbName + "' �����ݿⲻ����";
            }

            XmlNode nodeUse = nodeDatabase.SelectSingleNode("use[@value='"+lAttributeValue.ToString()+"']");
            if (nodeUse == null)
            {
                strError = "���ݿ� '" + strDbNameOrAlias + "' ��û���ҵ����� '" + lAttributeValue.ToString() + "' �ļ���;������";
                return null;
            }

            string strFrom =  DomUtil.GetAttr(nodeUse, "from");
            if (String.IsNullOrEmpty(strFrom) == true)
            {
                strError = "���ݿ� '" + strDbNameOrAlias + "' <database>Ԫ���й��� '" + lAttributeValue.ToString() + "' ��<use>����ȱ��from����ֵ";
                return null;
            }

            return strFrom;
        }
    }


    // ��Ŀ������
    public class BiblioDbProperty
    {
        // dp2library���������
        public string DbName = "";  // ��Ŀ����
        public string Syntax = "";  // ��ʽ�﷨
        public string ItemDbName = "";  // ��Ӧ��ʵ�����

        public bool IsVirtual = false;  // �Ƿ�Ϊ�����

        // ��dp2zserver.xml�ж��������
        public int MaxResultCount = -1; // �������е��������
        public string DbNameAlias = ""; // ���ݿ����

        public bool AddField901 = false;    // �Ƿ���MARC�ֶ��м����ʾ��¼·����ʱ����ĵ�901�ֶ�
    }
}
