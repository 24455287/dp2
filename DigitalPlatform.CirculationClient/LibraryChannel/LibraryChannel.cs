// #define BASIC_HTTP // Ϊ�˲��� basic.http://
// #define NEW_API

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.ServiceModel;
using System.Globalization;

using System.ServiceModel.Security;
using System.ServiceModel.Channels;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Selectors;

using System.IdentityModel.Policy;
using System.IdentityModel.Claims;
using System.ServiceModel.Security.Tokens;

using DigitalPlatform;
using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.Text;
using DigitalPlatform.Range;    // SaveResObject()
using DigitalPlatform.Xml;  // BuildMetadata()

namespace DigitalPlatform.CirculationClient
{
    /// <summary>
    /// ��¼ʧ�ܵ�ԭ��
    /// </summary>
    public enum LoginFailCondition
    {
        /// <summary>
        /// û�г���
        /// </summary>
        None = 0,   // û�г���
        /// <summary>
        /// һ�����
        /// </summary>
        NormalError = 1,    // һ�����
        /// <summary>
        /// ���벻��ȷ
        /// </summary>
        PasswordError = 2,  // ���벻��ȷ
    }

    /// <summary>
    /// ��¼ǰ���¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void BeforeLoginEventHandle(object sender,
    BeforeLoginEventArgs e);

    /// <summary>
    /// ��½ǰʱ��Ĳ���
    /// </summary>
    public class BeforeLoginEventArgs : EventArgs
    {
        /// <summary>
        /// [in] �Ƿ�Ϊ��һ�δ���
        /// </summary>
        public bool FirstTry = true;    // [in] �Ƿ�Ϊ��һ�δ���
        /// <summary>
        /// [in] ͼ���Ӧ�÷����� URL
        /// </summary>
        public string LibraryServerUrl = "";    // [in] ͼ���Ӧ�÷�����URL
        /// <summary>
        /// [out] �û���
        /// </summary>
        public string UserName = "";    // [out] �û���
        /// <summary>
        /// [out] ����
        /// </summary>
        public string Password = "";    // [out] ����
        /// <summary>
        /// [out] ����̨��
        /// </summary>
        public string Parameters = "";    // [out] ����̨��
        // public bool IsReader = false;   // [out] ��¼���Ƿ�Ϊ����
        /// <summary>
        /// [out] ���ڱ�������
        /// </summary>
        public bool SavePasswordShort = false;  // [out] ���ڱ�������
        /// <summary>
        /// [out] ���ڱ�������
        /// </summary>
        public bool SavePasswordLong = false;   // [out] ���ڱ�������
        /// <summary>
        /// [out] �¼������Ƿ�ʧ��
        /// </summary>
        public bool Failed = false; // [out] �¼������Ƿ�ʧ��
        /// <summary>
        /// [out] �¼������Ƿ񱻷���
        /// </summary>
        public bool Cancel = false; // [out] �¼������Ƿ񱻷���
        /// <summary>
        /// [in, out] �¼����ô�����Ϣ
        /// </summary>
        public string ErrorInfo = "";   // [in, out] �¼����ô�����Ϣ
        /// <summary>
        /// [in, out] ǰ�ε�¼ʧ�ܵ�ԭ�򣬱��ε�¼ʧ�ܵ�ԭ��
        /// </summary>
        public LoginFailCondition LoginFailCondition = LoginFailCondition.NormalError;  // [in, out] ǰ�ε�¼ʧ�ܵ�ԭ�򣬱��ε�¼ʧ�ܵ�ԭ��
    }

    /// <summary>
    /// ��¼����¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void AfterLoginEventHandle(object sender,
    AfterLoginEventArgs e);

    /// <summary>
    /// ��¼���¼��Ĳ���
    /// </summary>
    public class AfterLoginEventArgs : EventArgs
    {
        public string ErrorInfo = "";
        // public bool Canceled = false;
    }

#if NO
    public enum CertMode
    {
        None = 0,   // �����
        Strict = 1, // �ϸ�֤��
        Downgrade = 2,  // ���ϸ񽵼����ڲ�֤��
        Loss = 3, // �ڲ�֤��
    }
#endif

    /// <summary>
    /// ͨѶͨ��
    /// </summary>
    public class LibraryChannel
    {
        /// <summary>
        /// dp2Library �������� URL
        /// </summary>
        public string Url = "";

        /// <summary>
        /// RecieveTimeout
        /// </summary>
        public TimeSpan RecieveTimeout = new TimeSpan(0, 1, 0); // 40

        /// <summary>
        /// SendTimeout
        /// </summary>
        public TimeSpan SendTimeout = new TimeSpan(0, 1, 0);

        /// <summary>
        /// CloseTimeout
        /// </summary>
        public TimeSpan CloseTimeout = new TimeSpan(0, 0, 30);

        /// <summary>
        /// OpenTimeout
        /// </summary>
        public TimeSpan OpenTimeout = new TimeSpan(0, 1, 0);

        /// <summary>
        /// InactivityTimeout
        /// </summary>
        public TimeSpan InactivityTimeout = new TimeSpan(0, 20, 0);

        /// <summary>
        /// OperationTimeout
        /// </summary>
        public TimeSpan OperationTimeout = new TimeSpan(0, 40, 0);

        /// <summary>
        /// ��û����ó�ʱʱ�䡣�൱��ͨ�� �� OperationTimeout
        /// </summary>
        public TimeSpan Timeout
        {
            get
            {
                if (this.m_ws == null)
                    return this.OperationTimeout;

                return this.m_ws.InnerChannel.OperationTimeout;
            }
            set
            {
                if (this.m_ws == null)
                    this.OperationTimeout = value;
                else
                {
                    this.m_ws.InnerChannel.OperationTimeout = this.OperationTimeout;
                    this.OperationTimeout = value;
                }
            }
        }
        
#if BASIC_HTTP
        localhost.dp2libraryRESTClient m_ws = null;	// ӵ��
#else
        localhost.dp2libraryClient m_ws = null;	// ӵ��
#endif

        bool m_bStoped = false; // �����Ƿ��жϹ�һ��
        int m_nInSearching = 0;
        int m_nRedoCount = 0;   // MessageSecurityException�Ժ����ԵĴ���

        /// <summary>
        /// ���һ�ε�¼ʱ�õ����û���
        /// </summary>
        public string UserName = "";
        /// <summary>
        /// ���һ�ε�¼ʱ�õ�������
        /// </summary>
        public string Password = "";

        /// <summary>
        /// ��ǰ�û���Ȩ���ַ��������һ�ε�¼�ɹ���ӷ��������ص�
        /// </summary>
        public string Rights = "";

        /// <summary>
        /// ��ǰ�ѵ�¼�û�����Ͻ�Ĺݴ���(�б�)
        /// </summary>
        public string LibraryCodeList = ""; // ��ǰ�ѵ�¼�û�����Ͻ�Ĺݴ��� 2012/9/19

        /// <summary>
        /// ��ǰͨ����ʹ�õ� HTTP Cookies
        /// </summary>
        public CookieContainer Cookies = new System.Net.CookieContainer();

        /// <summary>
        /// ��ǰͨ���ĵ�¼ǰ�¼�
        /// </summary>
        public event BeforeLoginEventHandle BeforeLogin;
        /// <summary>
        /// ��ǰͨ���ĵ�¼���¼�
        /// </summary>
        public event AfterLoginEventHandle AfterLogin;

        /// <summary>
        /// �����¼�
        /// </summary>
        public event IdleEventHandler Idle = null;

        /// <summary>
        /// ��ǰͨ������Я������չ����
        /// </summary>
        public object Param = null;

        //object resultParam = null;
        //AutoResetEvent eventComplete = new AutoResetEvent(false);

        /// <summary>
        /// ���һ�ε��ô� dp2Library ���صĴ�����
        /// </summary>
        public ErrorCode ErrorCode = ErrorCode.NoError;

        /// <summary>
        /// ���һ�ε��� WCF �������� Exception
        /// </summary>
        public Exception WcfException = null;  // ���һ�ε�Exception 2012/5/7

#if NO
        // ����� Param
        /// <summary>
        /// ��ǰ�������Я������չ����
        /// </summary>
        public object Tag = null;   // 2008/10/28 //
#endif

        /// <summary>
        /// ��������Ϣ�ĳߴ�
        /// </summary>
        public int MaxReceivedMessageSize = 1024 * 1024;

        // np0: namedpipe
        System.ServiceModel.Channels.Binding CreateNp0Binding()
        {
            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            binding.Security.Mode = NetNamedPipeSecurityMode.None;

            binding.MaxReceivedMessageSize = MaxReceivedMessageSize;
            // binding.MessageEncoding = WSMessageEncoding.Mtom;
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = 1024 * 1024;
            quotas.MaxStringContentLength = 1024 * 1024;
            binding.ReaderQuotas = quotas;
            SetTimeout(binding);

            //binding.ReliableSession.Enabled = true;

            return binding;
        }

        // basic0: basic http
        System.ServiceModel.Channels.Binding CreateBasic0Binding()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.None;
            binding.AllowCookies = true;
            binding.MaxReceivedMessageSize = MaxReceivedMessageSize;
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = 1024 * 1024;
            quotas.MaxStringContentLength = 1024 * 1024;
            binding.ReaderQuotas = quotas;
            SetTimeout(binding);
            return binding;
        }

        // nt0: net.tcp
        System.ServiceModel.Channels.Binding CreateNt0Binding()
        {
            NetTcpBinding binding = new NetTcpBinding();
            binding.Security.Mode = SecurityMode.None;

            binding.MaxReceivedMessageSize = MaxReceivedMessageSize;
            // binding.MessageEncoding = WSMessageEncoding.Mtom;
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = 1024 * 1024;
            quotas.MaxStringContentLength = 1024 * 1024;
            binding.ReaderQuotas = quotas;
            SetTimeout(binding);
            binding.ReliableSession.InactivityTimeout = this.InactivityTimeout;

            //binding.ReliableSession.Enabled = true;

            return binding;
        }

        // ws0:windows
        System.ServiceModel.Channels.Binding CreateWs0Binding()
        {
            WSHttpBinding binding = new WSHttpBinding();
            binding.Security.Mode = SecurityMode.Message;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.Windows;

            binding.MaxReceivedMessageSize = MaxReceivedMessageSize;
            binding.MessageEncoding = WSMessageEncoding.Mtom;
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = 1024 * 1024;
            quotas.MaxStringContentLength = 1024 * 1024;
            binding.ReaderQuotas = quotas;
            SetTimeout(binding);
            binding.ReliableSession.InactivityTimeout = this.InactivityTimeout;

            //binding.ReliableSession.Enabled = true;

            return binding;
        }

        void SetTimeout(System.ServiceModel.Channels.Binding binding)
        {
            binding.SendTimeout = this.SendTimeout;
            binding.ReceiveTimeout = this.RecieveTimeout;
            binding.CloseTimeout = this.CloseTimeout;
            binding.OpenTimeout = this.OpenTimeout;
        }

        // ws1:anonymouse
        System.ServiceModel.Channels.Binding CreateWs1Binding()
        {
            WSHttpBinding binding = new WSHttpBinding();
            binding.Security.Mode = SecurityMode.Message;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.None;
            // binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;


            binding.MaxReceivedMessageSize = MaxReceivedMessageSize;
            binding.MessageEncoding = WSMessageEncoding.Mtom;
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = 1024 * 1024;
            quotas.MaxStringContentLength = 1024 * 1024;
            binding.ReaderQuotas = quotas;

            SetTimeout(binding);

            // binding.ReliableSession.Enabled = true;
            binding.ReliableSession.InactivityTimeout = this.InactivityTimeout;

            // binding.Security.Message.EstablishSecurityContext = false;

            // return binding;

            // Get the SecurityBindingElement and cast to a SymmetricSecurityBindingElement to set the IdentityVerifier.
            BindingElementCollection outputBec = binding.CreateBindingElements();
            SymmetricSecurityBindingElement ssbe = (SymmetricSecurityBindingElement)outputBec.Find<SecurityBindingElement>();

            //Set the Custom IdentityVerifier.
            ssbe.LocalClientSettings.IdentityVerifier = new CustomIdentityVerifier();

            //
            // Get the System.ServiceModel.Security.Tokens.SecureConversationSecurityTokenParameters 
            SecureConversationSecurityTokenParameters secureTokenParams =
                (SecureConversationSecurityTokenParameters)ssbe.ProtectionTokenParameters;
            // From the collection, get the bootstrap element.
            SecurityBindingElement bootstrap = secureTokenParams.BootstrapSecurityBindingElement;
            // Set the MaxClockSkew on the bootstrap element.
            bootstrap.LocalClientSettings.IdentityVerifier = new CustomIdentityVerifier();


            return new CustomBinding(outputBec);

        }

        // return:
        //      -1  error
        //      0   dp2Library�İ汾�Ź��͡�������Ϣ��strError��
        //      1   dp2Library�汾�ŷ���Ҫ��
        public static int GetServerVersion(
            LibraryChannel channel,
            Stop stop,
            out double version,
            out string strError)
        {
            strError = "";
            version = 0;

            string strVersion = "";
            long lRet = channel.GetVersion(stop,
out strVersion,
out strError);
            if (lRet == -1)
            {
                if (channel.WcfException is System.ServiceModel.Security.MessageSecurityException)
                {
                    // ԭ����dp2Library���߱�GetVersion() API�����ߵ�����
                    version = 0;
                    strError = "dp2 ǰ����Ҫ�� dp2Library 2.1 �����ϰ汾����ʹ�� (����ǰ dp2Library �汾��Ϊ '2.0������' )�������� dp2Library �����°汾��";
                    return 0;
                }

                strError = "��Է����� " + channel.Url + " ��ð汾�ŵĹ��̷�������" + strError;
                return -1;
            }

            double value = 0;

            if (string.IsNullOrEmpty(strVersion) == true)
            {
                strVersion = "2.0����";
                value = 2.0;
            }
            else
            {
                // �����Ͱ汾��
                if (double.TryParse(strVersion, out value) == false)
                {
                    strError = "dp2Library �汾�� '" + strVersion + "' ��ʽ����ȷ";
                    return -1;
                }
            }

            version = value;

            double base_version = 2.12; // 2.33;
            if (value < base_version)   // 2.12
            {
                strError = "dp2 ǰ����Ҫ�� dp2Library " + base_version + " �����ϰ汾����ʹ�� (����ǰ dp2Library �汾��Ϊ " + strVersion + " )��\r\n\r\n�뾡������ dp2Library �����°汾��";
                return 0;
            }

#if NO
                if (this.TestMode == true && this.Version < 2.34)
                {
                    strError = "dp2Circulation ������ģʽֻ���������ӵ� dp2library �汾Ϊ 2.34 ����ʱ����ʹ�� (��ǰ dp2library �汾Ϊ " + this.Version.ToString() + ")";
                    return -1;
                }
#endif

            return 1;
        }

        // public localhost.LibraryWse ws
        /// <summary>
        /// ��ȡ localhost.dp2libraryClient �������� WCF ���ͨ������
        /// </summary>
#if BASIC_HTTP
        localhost.dp2libraryRESTClient 
#else
        localhost.dp2libraryClient 
#endif 
            ws
        {
            get
            {
                if (m_ws == null)
                {
                    string strUrl = this.Url;


                    bool bWs0 = false;
                    Uri uri = new Uri(strUrl);

#if !BASIC_HTTP
                    if (uri.Scheme.ToLower() == "net.pipe")
                    {
                        EndpointAddress address = new EndpointAddress(strUrl);

                        this.m_ws = new localhost.dp2libraryClient(CreateNp0Binding(), address);
                    }
                    else
#endif
                        
                    if (uri.Scheme.ToLower() == "basic.http")
                    {
                        EndpointAddress address = new EndpointAddress(strUrl.Substring(6));

#if BASIC_HTTP
                        this.m_ws = new localhost.dp2libraryRESTClient(CreateBasic0Binding(), address);
#else
                        throw new Exception("��ǰ��������汾��֧�� basic.http Э�鷽ʽ");
#endif

                    }
#if !BASIC_HTTP
                    else if (uri.Scheme.ToLower() == "net.tcp")
                    {
                        EndpointAddress address = new EndpointAddress(strUrl);

                        this.m_ws = new localhost.dp2libraryClient(CreateNt0Binding(), address);
                    }
                    else
                    {
                        if (uri.AbsolutePath.ToLower().IndexOf("/ws0") != -1)
                            bWs0 = true;

                        if (bWs0 == false)
                        {
                            // ws1 
                            EndpointAddress address = null;

                            {
                                address = new EndpointAddress(strUrl);
                                this.m_ws = new localhost.dp2libraryClient(CreateWs1Binding(), address);

                                this.m_ws.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.Custom;
                                this.m_ws.ClientCredentials.ServiceCertificate.Authentication.CustomCertificateValidator =
                new MyValidator();

#if NO
                                ////
                                this.m_ws.ClientCredentials.UserName.UserName = "test";
                                this.m_ws.ClientCredentials.UserName.Password = "";
#endif
                            }
                            
                            /*
                            {

                                EndpointIdentity identity = EndpointIdentity.CreateDnsIdentity("DigitalPlatform");
                                address = new EndpointAddress(new Uri(strUrl),
                                    identity, new AddressHeaderCollection());
                                this.m_ws = new localhost.dp2libraryClient(CreateWs1Binding(), address);

                                // this.m_ws.ClientCredentials.ClientCertificate.SetCertificate(
                                this.m_ws.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.Custom;
                                this.m_ws.ClientCredentials.ServiceCertificate.Authentication.CustomCertificateValidator =
                new MyValidator();
                            }
                             * */

                        }
                        else
                        {
                            // ws0
                            EndpointAddress address = new EndpointAddress(strUrl);

                            this.m_ws = new localhost.dp2libraryClient(CreateWs0Binding(), address);
                            this.m_ws.ClientCredentials.UserName.UserName = "test";
                            this.m_ws.ClientCredentials.UserName.Password = "";
                        }
                    }
#endif

#if BASIC_HTTP
                    if (this.m_ws == null)
                        throw new Exception("��ǰ����汾ֻ��ʹ�� basic.http �󶨷�ʽ");
#endif


                }
                if (String.IsNullOrEmpty(this.Url) == true)
                {
                    throw (new Exception("Urlֵ��ʱӦ�������ڿ�"));
                }
                Debug.Assert(this.Url != "", "Urlֵ��ʱӦ�������ڿ�");

#if NO
                if (m_ws == null)
                {

                    /*
                    EndpointAddress address = new EndpointAddress(this.Url);
                    this.m_ws = new localhost.dp2libraryClient(binding, address);
                     * */
                    EndpointIdentity identity = EndpointIdentity.CreateDnsIdentity("DigitalPlatform");
                    EndpointAddress address = new EndpointAddress(new Uri(this.Url),
                        identity, new AddressHeaderCollection());
                    this.m_ws = new localhost.dp2libraryClient(binding, address);

                    // this.m_ws.ClientCredentials.ClientCertificate.SetCertificate(
                    this.m_ws.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = 
                        System.ServiceModel.Security.X509CertificateValidationMode.Custom;
                    this.m_ws.ClientCredentials.ServiceCertificate.Authentication.CustomCertificateValidator =
    new MyValidator();


                }
                if (String.IsNullOrEmpty(this.Url) == true)
                {
                    throw (new Exception("Urlֵ��ʱӦ�������ڿ�"));
                }
                Debug.Assert(this.Url != "", "Urlֵ��ʱӦ�������ڿ�");
#endif 

                // m_ws.Url = this.Url;
                // m_ws.CookieContainer = this.Cookies;


                this.m_ws.InnerChannel.OperationTimeout = this.OperationTimeout;

                this.WcfException = null;

                return m_ws;
            }
        }

        /// <summary>
        /// �Ƿ����ڽ��м���
        /// </summary>
        public int IsInSearching
        {
            get
            {
                return m_nInSearching;
            }
        }

        void BeginSearch()
        {
            m_bStoped = false;
            m_nInSearching++;
        }

        void EndSearch()
        {
            m_nInSearching--;
        }

        /*
        // 2007/11/20
        public int Timeout
        {
            get
            {
                return ws.Timeout;
            }
            set
            {
                ws.Timeout = value;
            }
        }
         * */

        /// <summary>
        /// ��¼��
        /// �������� dp2Library API Login() ǳ��װ���ɡ�
        /// ��ο����� dp2Library API Login() ����ϸ˵����
        /// ��¼�ɹ��󣬻��Զ����ú� Rights UserName LibraryCodeList �⼸����Ա
        /// </summary>
        /// <param name="strUserName">�û���</param>
        /// <param name="strPassword">����</param>
        /// <param name="strParameters">��¼����������һ�����ż�����б��ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    ��¼δ�ɹ�</para>
        /// <para>1:    ��¼�ɹ�</para>
        /// </returns>
        public long IdleLogin(string strUserName,
    string strPassword,
    string strParameters,
    out string strError)
        {
            string strRights = "";
            string strOutputUserName = "";
            string strLibraryCode = "";

            long lRet = this.IdleLogin(
                strUserName,
                strPassword,
                strParameters,
                out strOutputUserName,
                out strRights,
                out strLibraryCode,
                out strError);
            this.Rights = strRights;
            this.UserName = strOutputUserName;    // 2011/7/29
            this.LibraryCodeList = strLibraryCode;
            return lRet;
        }

        // ����������汾
        // return:
        //      -1  error
        //      0   ��¼δ�ɹ�
        //      1   ��¼�ɹ�
        /// <summary>
        /// ��¼��
        /// �������� dp2Library API Login() ǳ��װ���ɡ�
        /// ��ο����� dp2Library API Login() ����ϸ˵����
        /// ��¼�ɹ��󣬻��Զ����ú� Rights UserName LibraryCodeList �⼸����Ա
        /// </summary>
        /// <param name="strUserName">�û���</param>
        /// <param name="strPassword">����</param>
        /// <param name="strParameters">��¼����������һ�����ż�����б��ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    ��¼δ�ɹ�</para>
        /// <para>1:    ��¼�ɹ�</para>
        /// </returns>
        public long Login(string strUserName,
    string strPassword,
    string strParameters,
    out string strError)
        {
            string strRights = "";
            string strOutputUserName = "";
            string strLibraryCode = "";

            long lRet = this.Login(
                strUserName,
                strPassword,
                strParameters,
                out strOutputUserName,
                out strRights,
                out strLibraryCode,
                out strError);
            this.Rights = strRights;
            this.UserName = strOutputUserName;    // 2011/7/29
            this.LibraryCodeList = strLibraryCode;
            return lRet;
        }

        // �첽�İ汾�������õ� DoIdle
        // return:
        //      -1  error
        //      0   ��¼δ�ɹ�
        //      1   ��¼�ɹ�
        /// <summary>
        /// ��¼��
        /// ��ο����� dp2Library API Login() ����ϸ˵����
        /// ���ǱȽϵײ�İ汾���������� Rights UserName LibraryCodeList �⼸����Ա��������
        /// </summary>
        /// <param name="strUserName">�û���</param>
        /// <param name="strPassword">����</param>
        /// <param name="strParameters">��¼����������һ�����ż�����б��ַ���</param>
        /// <param name="strOutputUserName">����ʵ�ʵ�¼���û���</param>
        /// <param name="strRights">�����û���Ȩ���ַ���</param>
        /// <param name="strLibraryCode">�����û�����Ͻ��ͼ��ݴ����б�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    ��¼δ�ɹ�</para>
        /// <para>1:    ��¼�ɹ�</para>
        /// </returns>
        public long IdleLogin(string strUserName,
            string strPassword,
            string strParameters,
            out string strOutputUserName,
            out string strRights,
            out string strLibraryCode,
            out string strError)
        {
            strError = "";
            strRights = "";
            strOutputUserName = "";
            strLibraryCode = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginLogin(
                    strUserName,
                    strPassword,
                    strParameters,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndLogin(
                    out strOutputUserName,
                    out strRights,
                    out strLibraryCode,
                    soapresult);
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }

#if NO
            try
            {
                LibraryServerResult result = ws.Login(out strOutputUserName,
                    out strRights,
                    out strLibraryCode,
                    strUserName,
                    strPassword,
                    strParameters
                    );

                strError = result.ErrorInfo;
                return result.Value;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }
#endif
        }

        // �����첽�İ汾�������� DoIdle()
        // һ�㲻Ҫ������汾
        // return:
        //      -1  error
        //      0   ��¼δ�ɹ�
        //      1   ��¼�ɹ�
        /// <summary>
        /// ��¼��
        /// ��ο����� dp2Library API Login() ����ϸ˵����
        /// ���ǱȽϵײ�İ汾���������� Rights UserName LibraryCodeList �⼸����Ա��������
        /// </summary>
        /// <param name="strUserName">�û���</param>
        /// <param name="strPassword">����</param>
        /// <param name="strParameters">��¼����������һ�����ż�����б��ַ���</param>
        /// <param name="strOutputUserName">����ʵ�ʵ�¼���û���</param>
        /// <param name="strRights">�����û���Ȩ���ַ���</param>
        /// <param name="strLibraryCode">�����û�����Ͻ��ͼ��ݴ����б�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    ��¼δ�ɹ�</para>
        /// <para>1:    ��¼�ɹ�</para>
        /// </returns>
        public long Login(string strUserName,
            string strPassword,
            string strParameters,
            out string strOutputUserName,
            out string strRights,
            out string strLibraryCode,
            out string strError)
        {
            strError = "";
            strRights = "";
            strOutputUserName = "";
            strLibraryCode = "";

            try
            {
                LibraryServerResult result = ws.Login(out strOutputUserName,
                    out strRights,
                    out strLibraryCode,
                    strUserName,
                    strPassword,
                    strParameters
                    );

                strError = result.ErrorInfo;
                return result.Value;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }
        }

        // return:
        //      -1  error
        //      0   succeed
        /// <summary>
        /// �ǳ���
        /// ��ο� dp2Library API Logout() ����ϸ˵��
        /// </summary>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long Logout(out string strError)
        {
            strError = "";

            LibraryServerResult result = ws.Logout();

            strError = result.ErrorInfo;
            return result.Value;
        }

#if NO
        // true ֹͣ
        // false ����
        bool DoIdle(Stop stop)
        {
            if (stop != null)
            {
                if (stop.State != 0)
                    return true;
            }

            System.Threading.Thread.Sleep(1);	// ����CPU��Դ���Ⱥķ�

            try
            {
                Application.DoEvents();	// ���ý������Ȩ
            }
            catch
            {
            }

            return false;
        }
#endif
        void DoIdle()
        {
            System.Threading.Thread.Sleep(1);	// ����CPU��Դ���Ⱥķ�

            bool bDoEvents = true;
            if (this.Idle != null)
            {
                IdleEventArgs e = new IdleEventArgs();
                this.Idle(this, e);
                bDoEvents = e.bDoEvents;
            }

            if (bDoEvents == true)
            {
                try
                {
                    Application.DoEvents();	// ���ý������Ȩ
                }
                catch
                {
                }
            }

            System.Threading.Thread.Sleep(1);	// ����CPU��Դ���Ⱥķ�
        }

        // return:
        //      0   �������践��-1
        //      1   ��Ҫ����API
        int ConvertWebError(Exception ex0,
            out string strError)
        {
            strError = "";

            this.WcfException = ex0;

            // System.TimeoutException
            if (ex0 is System.TimeoutException)
            {
                this.ErrorCode = ErrorCode.RequestTimeOut;
                this.m_ws.Abort();
                this.m_ws = null;
                strError = GetExceptionMessage(ex0);
                return 0;
            }

            if (ex0 is System.ServiceModel.Security.MessageSecurityException)
            {
                System.ServiceModel.Security.MessageSecurityException ex = (System.ServiceModel.Security.MessageSecurityException)ex0;
                this.ErrorCode = ErrorCode.RequestError;	// һ�����
                this.m_ws.Abort();
                this.m_ws = null;
                // return ex.Message + (ex.InnerException != null ? " InnerException: " + ex.InnerException.Message : "") ;
                strError = GetExceptionMessage(ex);
                if (this.m_nRedoCount == 0)
                {
                    this.m_nRedoCount++;
                    return 1;   // ����
                }
                return 0;
            }

            if (ex0 is CommunicationObjectFaultedException)
            {
                CommunicationObjectFaultedException ex = (CommunicationObjectFaultedException)ex0;
                this.ErrorCode = ErrorCode.RequestError;	// һ�����
                this.m_ws.Abort();
                this.m_ws = null;
                strError = GetExceptionMessage(ex);
                // 2011/7/2
                if (this.m_nRedoCount == 0)
                {
                    this.m_nRedoCount++;
                    return 1;   // ����
                }
                return 0;
            }

            if (ex0 is EndpointNotFoundException)
            {
                EndpointNotFoundException ex = (EndpointNotFoundException)ex0;
                this.ErrorCode = ErrorCode.RequestError;	// һ�����
                this.m_ws.Abort();
                this.m_ws = null;
                strError = "������ " + this.Url + " û����Ӧ";
                return 0;
            }

            if (ex0 is System.ServiceModel.CommunicationException
                && ex0.InnerException is System.ServiceModel.QuotaExceededException)
            {
                this.ErrorCode = ErrorCode.RequestError;	// һ�����
                this.MaxReceivedMessageSize *= 2;    // �´�����һ��
                this.m_ws.Abort();
                this.m_ws = null;
                strError = GetExceptionMessage(ex0);
                if (this.m_nRedoCount == 0
                    && this.MaxReceivedMessageSize < 1024 * 1024 * 10)
                {
                    this.m_nRedoCount++;
                    return 1;   // ����
                }
                return 0;
            }

            /*
            if (ex0 is CommunicationException)
            {
                CommunicationException ex = (CommunicationException)ex0;

            }
             * */

            this.ErrorCode = ErrorCode.RequestError;	// һ�����
            if (this.m_ws != null)
            {
                this.m_ws.Abort();
                this.m_ws = null;   // һ����˵�쳣����Ҫ���·���Client()����������⣬������ǰ���֧
            }
            strError = GetExceptionMessage(ex0);
            return 0;
        }

        static string GetExceptionMessage(Exception ex)
        {
            string strResult = ex.GetType().ToString() + ":" + ex.Message;
            while (ex != null)
            {
                if (ex.InnerException != null)
                    strResult += "\r\n" + ex.InnerException.GetType().ToString() + ": " + ex.InnerException.Message;

                ex = ex.InnerException;
            }

            return strResult;
        }

        // ����������Ϣ
        // parameters:
        //      stop    ֹͣ����
        //      strReaderDbNames    ���߿���������Ϊ����������Ҳ�����Ƕ���(���)�ָ�Ķ��߿����б�������Ϊ <ȫ��>/<all> ֮һ����ʾȫ�����߿⡣
        //      strQueryWord    ������
        //      nPerMax һ���������е�����¼����-1��ʾ�����ơ�
        //      strFrom ����;��
        //      strMatchStyle   ƥ�䷽ʽ��ֵΪleft/right/exact/middle֮һ��
        //      strLang �������Դ��롣һ��Ϊ"zh"��
        //      strResultSetName    �����������ʹ��null����ָ�������ֵĽ�������������������ϲ�ͬĿ�ĵļ�����������಻��ͻ��
        // Ȩ�ޣ�
        //      ���߲��ܼ����κ��˵Ķ��߼�¼���������Լ��ģ�
        //      ������Ա��Ҫ searchreader Ȩ��
        // return:
        //      -1  error
        //      >=0 ���н����¼����
        /// <summary>
        /// �������߼�¼��
        /// ��ο� dp2Library API SearchReader() ����ϸ˵��
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strReaderDbNames">���߿���������Ϊ����������Ҳ�����Ƕ���(���)�ָ�Ķ��߿����б�������Ϊ &lt;ȫ��&gt;/&lt;all&gt; ֮һ����ʾȫ�����߿⡣</param>
        /// <param name="strQueryWord">������</param>
        /// <param name="nPerMax">һ���������е�����¼����-1��ʾ������</param>
        /// <param name="strFrom">����;��</param>
        /// <param name="strMatchStyle">ƥ�䷽ʽ��ֵΪleft/right/exact/middle֮һ</param>
        /// <param name="strLang">�������Դ��롣���� "zh"</param>
        /// <param name="strResultSetName">�����������ʹ��null����ͬ�� "default"����ָ�������ֵĽ�������������������ϲ�ͬĿ�ĵļ�����������Թ���</param>
        /// <param name="strOutputStyle">������keyid / keycount ֮һ��ȱʡΪ keyid</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>&gt;=0:  �������еļ�¼��</para>
        /// </returns>
        public long SearchReader(
            DigitalPlatform.Stop stop,
            string strReaderDbNames,
            string strQueryWord,
            int nPerMax,
            string strFrom,
            string strMatchStyle,
            string strLang,
            string strResultSetName,
            string strOutputStyle,
            out string strError)
        {
            strError = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchReader(
                    strReaderDbNames,
                        strQueryWord,
                        nPerMax,
                        strFrom,
                        strMatchStyle,
                        strLang,
                        strResultSetName,
                        strOutputStyle,
                        null,
                        null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchReader(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }

        }

        /// <summary>
        /// ���ú��ѹ�ϵ
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strAction">����</param>
        /// <param name="strReaderBarcode">����֤�����</param>
        /// <param name="strComment">ע��</param>
        /// <param name="strStyle">���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <para>-1:   ����</para>
        /// <para>0:  ����ɹ�(ע�⣬��������Է�ͬ��)</para>
        /// <para>1:  ����ǰ�Ѿ��Ǻ��ѹ�ϵ�ˣ�û�б�Ҫ�ظ�����</para>
        /// <para>2:  �Ѿ��ɹ����</para>
        public long SetFriends(
            DigitalPlatform.Stop stop,
            string strAction,
            string strReaderBarcode,
            string strComment,
            string strStyle,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetFriends(
                    strAction,
                    strReaderBarcode,
                    strComment,
                    strStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetFriends(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ���dp2Library�汾��
        /// <summary>
        /// ��� dp2Library �������汾�š�
        /// ��ο� dp2Library API GetVersion() ����ϸ˵��
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strVersion">���ذ汾��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>0: �ɹ�</returns>
        public long GetVersion(
    DigitalPlatform.Stop stop,
    out string strVersion,
    out string strError)
        {
            strError = "";
            strVersion = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetVersion(
                        null,
                        null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetVersion(
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();

                strVersion = result.ErrorInfo;
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����ͨ������
        /// <summary>
        /// ����ͨ����ǰ���ԡ�
        /// ��ο� dp2Library API SetLang() ����ϸ˵��
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strLang">���Դ���</param>
        /// <param name="strOldLang">���ر��ε���ǰ��ͨ��ʹ�õ����Դ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long SetLang(
    DigitalPlatform.Stop stop,
    string strLang,
    out string strOldLang,
    out string strError)
        {
            strError = "";
            strOldLang = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetLang(
                    strLang,
                        null,
                        null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetLang(
                    out strOldLang,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        void ClearRedoCount()
        {
            this.m_nRedoCount = 0;
        }

        // ��������Ϣ
        // parameters:
        //      stop    ֹͣ����
        //      strItemDbNames  ʵ��������б����԰����������������֮���ö���(���)�ָ�
        //      strQueryWord    ������
        //      nPerMax һ���������е�����¼����-1��ʾ�����ơ�
        //      strFrom ����;��
        //      strMatchStyle   ƥ�䷽ʽ��ֵΪleft/right/exact/middle֮һ��
        //      strLang �������Դ��롣һ��Ϊ"zh"��
        //      strResultSetName    �����������ʹ��null����ָ�������ֵĽ�������������������ϲ�ͬĿ�ĵļ�����������಻��ͻ��
        // Ȩ��: 
        //      ��Ҫ searchitem Ȩ��
        // return:
        //      -1  error
        //      >=0 ���н����¼����
        // ע��
        //      ʵ�������ݸ�ʽ����ͳһ�ģ�����;���������Ϊ���������/���κ�/��¼�� ...
        /// <summary>
        /// ����ʵ����¼��
        /// ��ο� dp2Library API SearchItem() ����ϸ˵��
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strItemDbNames">ʵ������б�</param>
        /// <param name="strQueryWord">������</param>
        /// <param name="nPerMax">�����������-1 ��ʾ������</param>
        /// <param name="strFrom">����;��</param>
        /// <param name="strMatchStyle">ƥ�䷽ʽ</param>
        /// <param name="strLang">���Դ���</param>
        /// <param name="strResultSetName">�������</param>
        /// <param name="strSearchStyle">������ʽ��Ϊ asc / desc֮һ��ȱʡΪ asc</param>
        /// <param name="strOutputStyle">�����ʽ��keyid / keycount ֮һ��ȱʡΪ keyid</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>&gt;=0:  �������еļ�¼��</para>
        /// </returns>
        public long SearchItem(
            DigitalPlatform.Stop stop,
            string strItemDbNames,
            string strQueryWord,
            int nPerMax,
            string strFrom,
            string strMatchStyle,
            string strLang,
            string strResultSetName,
            string strSearchStyle,
            string strOutputStyle,
            out string strError)
        {
            strError = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchItem(
                    strItemDbNames,
                    strQueryWord,
                    nPerMax,
                    strFrom,
                    strMatchStyle,
                    strLang,
                    strResultSetName,
                    strSearchStyle,
                    strOutputStyle,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchItem(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }

        /// <summary>
        /// �� KeyFrom ���鹹��Ϊ�ʺ���ʾ���ַ���
        /// </summary>
        /// <param name="keys">���������顣�� KeyFrom ��������</param>
        /// <returns>�ַ���</returns>
        public static string BuildDisplayKeyString(DigitalPlatform.CirculationClient.localhost.KeyFrom[] keys)
        {
            if (keys == null || keys.Length == 0)
                return "";
            string strResult = "";
            foreach (KeyFrom entry in keys)
            {
                if (String.IsNullOrEmpty(entry.Logic) == false)
                {
                    strResult += " " + entry.Logic + " ";
                }
                else if (String.IsNullOrEmpty(strResult) == false)
                    strResult += " | ";

                strResult += entry.Key + ":" + entry.From;
            }

            return strResult;
        }

        // ��װ��汾
        // ֻ���·����ȷ����Ҫ��lStart lCount��Χȫ�����
        /// <summary>
        /// ��ü��������
        /// �������� dp2Library API GetSearchResult() ǳ��װ���ɡ�
        /// ��ο����� dp2Library API GetSearchResult() �Ľ���
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strResultSetName">�������</param>
        /// <param name="lStart">��ʼ����</param>
        /// <param name="lCount">Ҫ��õ�����</param>
        /// <param name="strLang">���Դ���</param>
        /// <param name="paths">�������н���ļ�¼·������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>&gt;=0:  ������ڵļ�¼����ע�⣬���Ǳ��ε��÷��صĽ����</para>
        /// </returns>
        public long GetSearchResult(
    DigitalPlatform.Stop stop,
    string strResultSetName,
    long lStart,
    long lCount,
    string strLang,
    out List<string> paths,
    out string strError)
        {
            strError = "";
            paths = new List<string>();

            long _lStart = lStart;
            long _lCount = lCount;
            long lHitCount = 0;
            for (; ; )
            {
                Record[] searchresults = null;
                long lRet = GetSearchResult(
            stop,
            strResultSetName,
            _lStart,
            _lCount,
            "id",
            strLang,
            out searchresults,
            out strError);
                if (lRet == -1)
                    return -1;
                lHitCount = lRet;
                if (_lCount == -1)
                    _lCount = lHitCount - _lStart;

                for (int j = 0; j < searchresults.Length; j++)
                {
                    Record record = searchresults[j];
                    paths.Add(record.Path);
                }

                _lStart += searchresults.Length;
                _lCount -= searchresults.Length;

                if (_lStart >= lHitCount)
                    break;
                if (_lCount <= 0)
                    break;
            }

            return lHitCount;
        }

        // ��������
        // parameters:
        //      strAction   share/remove �ֱ��ʾ����Ϊȫ�ֽ��������/ɾ��ȫ�ֽ��������
        /// <summary>
        /// ����������
        /// ������ʵ�������� dp2Library API GetSearchResult() ��װ��������ο�����ϸ���ܡ�
        /// strAction Ϊ "share" ʱ��strResultSetName ��ΪҪ�����ȥ��ͨ�����������strGlobalResultName ΪҪ����ɵ�ȫ�ֽ��������
        /// strAction Ϊ "remove" ʱ��strResultSetName ������ʹ��(����Ϊ�ռ���)��strGlobalResultName ΪҪɾ����ȧ�ǽ������
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strAction">������Ϊ share / remove ֮һ</param>
        /// <param name="strResultSetName">(��ǰͨ��)�������</param>
        /// <param name="strGlobalResultName">ȫ�ֽ������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long ManageSearchResult(
            DigitalPlatform.Stop stop,
            string strAction,
            string strResultSetName,
            string strGlobalResultName,
            out string strError)
        {
            Record[] searchresults = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetSearchResult(
                    strResultSetName,
                    0,
                    0,
                    "@" + strAction + ":" + strGlobalResultName,
                    "zh",
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetSearchResult(
                    out searchresults,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }

        }

        // ��ü������еĽ������Ϣ
        // parameters:
        //      strResultSetName    ������������Ϊ�գ���ʾʹ�õ�ǰȱʡ�����"default"
        //      lStart  Ҫ��ȡ�Ŀ�ʼλ�á���0��ʼ����
        //      lCount  Ҫ��ȡ�ĸ���
        //      strBrowseInfoStyle  �����ص�SearchResult�а�����Щ��Ϣ��Ϊ���ŷָ����ַ����б�ֵ��ȡֵ��Ϊ id/cols ֮һ�����磬"id,cols"��ʾͬʱ��ȡid�������Ϣ���У���"id"��ʾ��ȡ��id�С�
        //      strLang ���Դ��롣һ��Ϊ"zh"
        //      searchresults   ���ذ�����¼��Ϣ��SearchResult��������
        // rights:
        //      û������
        // return:
        //      -1  ����
        //      >=0 ������ڼ�¼������(ע�⣬�����Ǳ������صļ�¼��)
        /// <summary>
        /// ��ü��������
        /// ��ο����� dp2Library API GetSearchResult() �Ľ���
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strResultSetName">�������</param>
        /// <param name="lStart">��ʼ����</param>
        /// <param name="lCount">Ҫ��õ�����</param>
        /// <param name="strBrowseInfoStyle">������Ϣ�ķ�ʽ��
        /// id / cols / xml / timestamp / metadata / keycount / keyid ����ϡ�keycount �� keyid ����ֻ��ʹ��һ����ȱʡΪ keyid��
        /// ���������ʹ�� format:???? �������Ӵ�����ʾʹ���ض�������и�ʽ
        /// </param>
        /// <param name="strLang">���Դ���</param>
        /// <param name="searchresults">���� Record ��������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>&gt;=0:  ������ڵļ�¼����ע�⣬���Ǳ��ε��÷��صĽ����</para>
        /// </returns>
        public long GetSearchResult(
            DigitalPlatform.Stop stop,
            string strResultSetName,
            long lStart,
            long lCount,
            string strBrowseInfoStyle,
            string strLang,
            out Record[] searchresults,
            out string strError)
        {
            searchresults = null;
            strError = "";

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetSearchResult(
                    strResultSetName,
                    lStart,
                    lCount,
                    strBrowseInfoStyle,
                    strLang,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetSearchResult(
                    out searchresults,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }

        }

        // 2009/11/6
        // ���ָ����¼�������Ϣ
        // parameters:
        // rights:
        //      û������
        // return:
        //      -1  ����
        /// <summary>
        /// ���ָ����¼���������ϸ��Ϣ��
        /// ��ο� dp2Library API GetBrowseRecords() ����ϸ˵��
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="paths">��¼·���ַ�������</param>
        /// <param name="strBrowseInfoStyle">������Ϣ�ķ�ʽ��
        /// id / cols / xml / timestamp / metadata ����ϡ�
        /// ���������ʹ�� format:???? �������Ӵ�����ʾʹ���ض�������и�ʽ
        /// </param>
        /// <param name="searchresults">���� Record ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long GetBrowseRecords(
            DigitalPlatform.Stop stop,
            string[] paths,
            string strBrowseInfoStyle,
            out Record[] searchresults,
            out string strError)
        {
            searchresults = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetBrowseRecords(
                    paths,
                    strBrowseInfoStyle,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetBrowseRecords(
                    out searchresults,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ������ݿ��¼
        // �������������ʵ�塢��Ŀ����������������¼
        // parameters:
        //      stop    ֹͣ����
        //      strPath ��¼·��
        //      timestamp   ���ؼ�¼��ʱ���
        //      strXml  ���ؼ�¼��XML�ַ���
        /// <summary>
        /// ������ݿ��¼��
        /// ��ο� dp2Library API GetRecord() ����ϸ˵��
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strPath">��¼·��</param>
        /// <param name="timestamp">����ʱ���</param>
        /// <param name="strXml">���ؼ�¼ XML</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long GetRecord(
            DigitalPlatform.Stop stop,
            string strPath,
            out byte[] timestamp,
            out string strXml,
            out string strError)
        {
            timestamp = null;
            strXml = "";
            strError = "";

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetRecord(
                    strPath,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetRecord(
                    out timestamp,
                    out strXml,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ������߼�¼
        /// <summary>
        /// д����߼�¼��
        /// ��ο� dp2Library API SetReaderInfo() ����ϸ��Ϣ
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strAction">������Ϊ new / change / delete /changestate / changeforegift ֮һ</param>
        /// <param name="strRecPath">��¼·��</param>
        /// <param name="strNewXml">�¼�¼ XML</param>
        /// <param name="strOldXml">�ɼ�¼ XML</param>
        /// <param name="baOldTimestamp">ʱ���</param>
        /// <param name="strExistingXml">�������ݿ����Ѿ����ڵļ�¼�� XML</param>
        /// <param name="strSavedXml">����ʵ�ʱ���ļ�¼ XML</param>
        /// <param name="strSavedRecPath">����ʵ�ʱ����¼��·��</param>
        /// <param name="baNewTimestamp">��������ʱ���</param>
        /// <param name="kernel_errorcode">�ں˴�����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// <para>1:    �ɹ����������ֶα��ܾ�</para>
        /// </returns>
        public long SetReaderInfo(
            DigitalPlatform.Stop stop,
            string strAction,
            string strRecPath,
            string strNewXml,
            string strOldXml,
            byte[] baOldTimestamp,
            out string strExistingXml,
            out string strSavedXml,
            out string strSavedRecPath,
            out byte[] baNewTimestamp,
            out ErrorCodeValue kernel_errorcode,
            out string strError)
        {
            strError = "";

            strExistingXml = "";
            strSavedXml = "";
            strSavedRecPath = "";
            baNewTimestamp = null;
            kernel_errorcode = ErrorCodeValue.NoError;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetReaderInfo(
                    strAction,
                    strRecPath,
                    strNewXml,
                    strOldXml,
                    baOldTimestamp,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetReaderInfo(
                    out strExistingXml,
                    out strSavedXml,
                    out strSavedRecPath,
                    out baNewTimestamp,
                    out kernel_errorcode,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ��װ��İ汾
        /// <summary>
        /// ��ö��߼�¼��
        /// �������Ƕ� dp2Library API GetReaderInfo() ��ǳ��װ��
        /// ��ο� dp2Library API GetReaderInfo() ����ϸ˵��
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strBarcode">����֤����ţ������������</param>
        /// <param name="strResultTypeList">ϣ����õķ��ؽ�����͵��б�Ϊ xml / html / text / calendar / advancexml / timestamp �����</param>
        /// <param name="results">���ؽ����Ϣ���ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    û���ҵ����߼�¼</para>
        /// <para>1:    �ҵ����߼�¼</para>
        /// <para>&gt;>1:   �ҵ�����һ�����߼�¼������ֵ���ҵ��ļ�¼��������һ�ֲ����������</para>
        /// </returns>
        public long GetReaderInfo(
            DigitalPlatform.Stop stop,
            string strBarcode,
            string strResultTypeList,
            out string [] results,
            out string strError)
        {
            byte[] baTimestamp = null;
            string strRecPath = "";

            return GetReaderInfo(stop,
                strBarcode,
                strResultTypeList,
                out results,
                out strRecPath,
                out baTimestamp,
                out strError);
        }

        // ��ö��߼�¼
        /// <summary>
        /// ��ö��߼�¼
        /// ��ο� dp2Library API GetReaderInfo() ����ϸ˵��
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strBarcode">����֤����ţ������������</param>
        /// <param name="strResultTypeList">ϣ����õķ��ؽ�����͵��б�Ϊ xml / html / text / calendar / advancexml / timestamp �����</param>
        /// <param name="results">���ؽ����Ϣ���ַ�������</param>
        /// <param name="strRecPath">����ʵ�ʻ�ȡ�ļ�¼��·��</param>
        /// <param name="baTimestamp">����ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    û���ҵ����߼�¼</para>
        /// <para>1:    �ҵ����߼�¼</para>
        /// <para>&gt;>1:   �ҵ�����һ�����߼�¼������ֵ���ҵ��ļ�¼��������һ�ֲ����������</para>
        /// </returns>
        public long GetReaderInfo(
            DigitalPlatform.Stop stop,
            string strBarcode,
            string strResultTypeList,
            out string [] results,
            out string strRecPath,
            out byte [] baTimestamp,
            out string strError)
        {
            results = null;
            strError = "";
            strRecPath = "";
            baTimestamp = null;

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetReaderInfo(
                    strBarcode, 
                    strResultTypeList,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetReaderInfo(
                    out results,
                    out strRecPath,
                    out baTimestamp,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // TODO: ��������һ��ʱ����������ض�ʱ�䷶Χ�ڿ�������������һ��ʱ������¼��������Ҳ����˵�����������ܼ��ظ���¼
        int _loginCount = 0;

        // �����¼����
        /// <summary>
        /// �����¼����
        /// </summary>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 1: ��¼�ɹ�</returns>
        public int DoNotLogin(ref string strError)
        {
            this.ClearRedoCount();

            if (this.BeforeLogin != null)
            {
                BeforeLoginEventArgs ea = new BeforeLoginEventArgs();
                ea.LibraryServerUrl = this.Url;
                ea.FirstTry = true;
                ea.ErrorInfo = strError;

            REDOLOGIN:
                this.BeforeLogin(this, ea);

                if (ea.Cancel == true)
                {
                    if (String.IsNullOrEmpty(ea.ErrorInfo) == true)
                        strError = "�û�������¼";
                    else
                        strError = ea.ErrorInfo;
                    this.ErrorCode = localhost.ErrorCode.NotLogin;
                    return -1;
                }

                if (ea.Failed == true)
                {
                    strError = ea.ErrorInfo;
                    return -1;
                }

                // 2006/12/30
                if (this.Url != ea.LibraryServerUrl)
                {
                    this.Close();   // ��ʹ���¹���m_ws 2011/11/22
                    this.Url = ea.LibraryServerUrl;
                }

                string strMessage = "";
                if (ea.FirstTry == true)
                    strMessage = strError;

                if (_loginCount > 100)
                {
                    strError = "���µ�¼����̫�࣬���� 100 �Σ������¼ API �Ƿ�������߼�����";
                    _loginCount = 0;    // ���¿�ʼ����
                    return -1;
                }

                _loginCount++;
                long lRet = this.Login(ea.UserName,
                    ea.Password,
                    ea.Parameters,
                    out strError);
                if (lRet == -1 || lRet == 0)
                {
                    if (String.IsNullOrEmpty(strMessage) == false)
                        ea.ErrorInfo = strMessage + "\r\n\r\n�״��Զ���¼����: ";
                    else
                        ea.ErrorInfo = "";
                    ea.ErrorInfo += strError;
                    ea.FirstTry = false;
                    ea.LoginFailCondition = LoginFailCondition.PasswordError;
                    goto REDOLOGIN;
                }

                // this.m_nRedoCount = 0;
                if (this.AfterLogin != null)
                {
                    AfterLoginEventArgs e1 = new AfterLoginEventArgs();
                    this.AfterLogin(this, e1);
                    if (string.IsNullOrEmpty(e1.ErrorInfo) == false)
                    {
                        strError = e1.ErrorInfo;
                        return -1;
                    }
                }
                return 1;   // ��¼�ɹ�,��������API������
            }

            return -1;
        }

        // ���ʵ���¼(��װ�汾��ʡ��3���������)
        public long GetItemInfo(
            DigitalPlatform.Stop stop,
            string strBarcode,
            string strResultType,
            out string strResult,
            string strBiblioType,
            out string strBiblio,
            out string strError)
        {
            string strItemRecPath = "";
            string strBiblioRecPath = "";
            byte[] item_timestamp = null;

            return GetItemInfo(
                stop,
                "item",
                strBarcode,
                "",
                strResultType,
                out strResult,
                out strItemRecPath,
                out item_timestamp,
                strBiblioType,
                out strBiblio,
                out strBiblioRecPath,
                out strError);
        }

        // ��װ��İ汾
        public long GetItemInfo(
            DigitalPlatform.Stop stop,
            string strBarcode,
            string strResultType,
            out string strResult,
            out string strItemRecPath,
            out byte[] item_timestamp,
            string strBiblioType,
            out string strBiblio,
            out string strBiblioRecPath,
            out string strError)
        {
            return GetItemInfo(
                stop,
                "item",
                strBarcode,
                "",
                strResultType,
                out strResult,
                out strItemRecPath,
                out item_timestamp,
                strBiblioType,
                out strBiblio,
                out strBiblioRecPath,
                out strError);
        }

        // ���ʵ���¼
        // Result.Value -1���� 0���¼û���ҵ� 1���¼�ҵ� >1���¼���ж���1��
        /// <summary>
        /// ���ʵ���¼
        /// ��ο� dp2Library API GetItemInfo() ����ϸ˵��
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strItemDbType">���ݿ������</param>
        /// <param name="strBarcode">�������</param>
        /// <param name="strItemXml">���¼XML��������Ҫǰ���ύ���ݵĳ���</param>
        /// <param name="strResultType">ϣ���� strResult �����з��صĲ��¼��Ϣ���͡�ֵΪ xml text html ֮һ</param>
        /// <param name="strResult">���ز��¼����Ϣ</param>
        /// <param name="strItemRecPath">���ز��¼��·��</param>
        /// <param name="item_timestamp">���ز��¼��ʱ���</param>
        /// <param name="strBiblioType">ϣ���� strBiblio �����з��ص���Ŀ��Ϣ���͡�ֵΪ xml text html ֮һ</param>
        /// <param name="strBiblio">����(���¼��������)��Ŀ��¼����Ϣ</param>
        /// <param name="strBiblioRecPath">����(���¼��������)��Ŀ��¼��·��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    û���ҵ����¼</para>
        /// <para>1:    �ҵ����¼</para>
        /// <para>&gt;1:   �ҵ�����һ�����¼������ֵ���ҵ��ļ�¼��������һ�ֲ����������</para>
        /// </returns>
        public long GetItemInfo(
            DigitalPlatform.Stop stop,
            string strItemDbType,
            string strBarcode,
            string strItemXml,
            string strResultType,
            out string strResult,
            out string strItemRecPath,
            out byte [] item_timestamp,
            string strBiblioType,
            out string strBiblio,
            out string strBiblioRecPath,
            out string strError)
        {
            strResult = "";
            strBiblio = "";
            strError = "";

            strBiblioRecPath = "";
            strItemRecPath = "";

            item_timestamp = null;

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetItemInfo(
                    strItemDbType,
                    strBarcode, 
                    strItemXml,
                    strResultType,
                    strBiblioType,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetItemInfo(
                    out strResult,
                    out strItemRecPath,
                    out item_timestamp,
                    out strBiblio,
                    out strBiblioRecPath,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����
        /// <summary>
        /// ���������
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="bRenew">�Ƿ�Ϊ���衣true ��ʾxujie��false ��ʾ��ͨ����</param>
        /// <param name="strReaderBarcode">����֤����ţ���������֤��</param>
        /// <param name="strItemBarcode">Ҫ���ĵĲ������</param>
        /// <param name="strConfirmItemRecPath">����ȷ�ϲ��¼��·��</param>
        /// <param name="bForce">�˲���Ŀǰδʹ�ã���Ϊ false ����</param>
        /// <param name="saBorrowedItemBarcode">���ͬһ���ߵ������������Ѿ����ĵĲ���������顣�����ڶ�����Ϣ HTML ������Ϊ��Щ�����Ϣ���������ⱳ��ɫ</param>
        /// <param name="strStyle">�������</param>
        /// <param name="strItemFormatList">ָ���� item_records �����з�����Ϣ�ĸ�ʽ�б�</param>
        /// <param name="item_records">���ز���ص���Ϣ����</param>
        /// <param name="strReaderFormatList">ָ���� reader_records �����з�����Ϣ�ĸ�ʽ�б�</param>
        /// <param name="reader_records">���ض�����ص���Ϣ����</param>
        /// <param name="strBiblioFormatList">ָ���� biblio_records �����з�����Ϣ�ĸ�ʽ�б�</param>
        /// <param name="biblio_records">������Ŀ��ص���Ϣ����</param>
        /// <param name="aDupPath">�������������ظ������ﷵ������ز��¼��·��</param>
        /// <param name="strOutputReaderBarcode">����ʵ�ʲ�����ԵĶ���֤�����</param>
        /// <param name="borrow_info">���� BorrowInfo �ṹ����������һЩ���ڽ��ĵ���ϸ��Ϣ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �����ɹ�</para>
        /// </returns>
        public long Borrow(
            DigitalPlatform.Stop stop,
            bool bRenew,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            bool bForce,
            string [] saBorrowedItemBarcode,
            string strStyle,
            string strItemFormatList,
            out string [] item_records,
            string strReaderFormatList,
            out string [] reader_records,
            string strBiblioFormatList,
            out string[] biblio_records,
            out string[] aDupPath,
            out string strOutputReaderBarcode,
            out BorrowInfo borrow_info,
            out string strError)
        {
            reader_records = null;
            item_records = null;
            biblio_records = null;
            aDupPath = null;
            strOutputReaderBarcode = "";
            borrow_info = null;
            strError = "";


            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginBorrow(
                    bRenew,
                    strReaderBarcode,
                    strItemBarcode,
                    strConfirmItemRecPath,
                    bForce,
                    saBorrowedItemBarcode,
                    strStyle,
                    strItemFormatList,
                    strReaderFormatList,
                    strBiblioFormatList,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndBorrow(
                    out item_records,
                    out reader_records,
                    out biblio_records,
                    out borrow_info,
                    out aDupPath,
                    out strOutputReaderBarcode,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }


        }

        // ����
        // return:
        //      -1  ����
        //      0   ����
        //      1   �г������
        /// <summary>
        /// �����������ʧ
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strAction">����������Ϊ return lost ֮һ</param>
        /// <param name="strReaderBarcode">����֤����ţ���������֤��</param>
        /// <param name="strItemBarcode">Ҫ���ػ�������ʧ�Ĳ������</param>
        /// <param name="strConfirmItemRecPath">����ȷ�ϲ��¼��·��</param>
        /// <param name="bForce">�˲���Ŀǰδʹ�ã���Ϊ false ����</param>
        /// <param name="strStyle">�������</param>
        /// <param name="strItemFormatList">ָ���� item_records �����з�����Ϣ�ĸ�ʽ�б�</param>
        /// <param name="item_records">���ز���ص���Ϣ����</param>
        /// <param name="strReaderFormatList">ָ���� reader_records �����з�����Ϣ�ĸ�ʽ�б�</param>
        /// <param name="reader_records">���ض�����ص���Ϣ����</param>
        /// <param name="strBiblioFormatList">ָ���� biblio_records �����з�����Ϣ�ĸ�ʽ�б�</param>
        /// <param name="biblio_records">������Ŀ��ص���Ϣ����</param>
        /// <param name="aDupPath">�������������ظ������ﷵ������ز��¼��·��</param>
        /// <param name="strOutputReaderBarcode">����ʵ�ʲ�����ԵĶ���֤�����</param>
        /// <param name="return_info">���� ReturnInfo �ṹ����������һЩ���ڻ������ϸ��Ϣ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �����ɹ�</para>
        /// <para>1:    �����ɹ���������ֵ�ò�����Ա������������ʾ��Ϣ�� strError ��</para>
        /// </returns>
        public long Return(
            DigitalPlatform.Stop stop,
            string strAction,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            bool bForce,
            string strStyle,
            string strItemFormatList,
            out string[] item_records,
            string strReaderFormatList,
            out string [] reader_records,
            string strBiblioFormatList,
            out string[] biblio_records,
            out string [] aDupPath,
            out string strOutputReaderBarcode,
            out ReturnInfo return_info,
            out string strError)
        {
            item_records = null;
            reader_records = null;
            biblio_records = null;
            strError = "";
            aDupPath = null;
            strOutputReaderBarcode = "";
            return_info = null;

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginReturn(
                    strAction,
                    strReaderBarcode,
                    strItemBarcode,
                    strConfirmItemRecPath,
                    bForce,
                    strStyle,
                    strItemFormatList,
                    strReaderFormatList,
                    strBiblioFormatList,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndReturn(
                    out item_records,
                    out reader_records,
                    out biblio_records,
                    out aDupPath,
                    out strOutputReaderBarcode,
                    out return_info,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����
        // return:
        //      -1  ����
        //      0   ����
        //      1   ���ֳɹ�
        /// <summary>
        /// ΥԼ��/Ѻ����
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strFunction">��������</param>
        /// <param name="strReaderBarcode">����֤�����</param>
        /// <param name="amerce_items">������Ϣ����</param>
        /// <param name="failed_items">���ز���ʧ�ܵķ�����Ϣ����</param>
        /// <param name="strReaderXml">���ظı��Ķ��߼�¼ XML �ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �����ɹ�</para>
        /// <para>1:    ���ֳɹ�����ʾ��Ϣ�� strError ��</para>
        /// </returns>
        public long Amerce(
            DigitalPlatform.Stop stop,
            string strFunction,
            string strReaderBarcode,
            AmerceItem[] amerce_items,
            out AmerceItem[] failed_items,  // 2011/6/27
            out string strReaderXml,
            out string strError)
        {
            strReaderXml = "";
            strError = "";
            failed_items = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginAmerce(
                    strFunction,
                    strReaderBarcode,
                    amerce_items,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }

                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndAmerce(
                    out failed_items,
                    out strReaderXml,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ���ʵ����Ϣ
        // return:
        //      -1  ����
        //      0   ����
        /// <summary>
        /// ���ͬһ��Ŀ��¼�µ����ɲ��¼��Ϣ
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="lStart">�Ӻδ���ʼ��ȡ</param>
        /// <param name="lCount">��ȡ���ٸ�����Ϣ</param>
        /// <param name="strStyle">��ȡ��ʽ</param>
        /// <param name="strLang">���Դ���</param>
        /// <param name="entityinfos">���ز��¼��Ϣ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    û���ҵ�</para>
        /// <para>&gt;=1:    �ɹ�������ֵ�Ƿ��ҵ��Ĳ��¼���������η��صĲ��¼��ϢԪ�ظ�����ͨ�� entity_infos.Length ��ӳ</para>
        /// </returns>
        public long GetEntities(
            DigitalPlatform.Stop stop,
            string strBiblioRecPath,
            long lStart,
            long lCount,
            string strStyle,
            string strLang, 
            out EntityInfo[] entityinfos,
            out string strError)
        {
            entityinfos = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetEntities(
                    strBiblioRecPath,
                    lStart,
                    lCount,
                    strStyle,
                    strLang,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetEntities(
                    out entityinfos,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����ʵ����Ϣ
        // return:
        //      -1  ����
        //      0   ����
        /// <summary>
        /// ����ͬһ��Ŀ��¼�µ����ɲ��¼��Ϣ
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="entityinfos">Ҫ���õĲ���Ϣ����</param>
        /// <param name="errorinfos">���ز����з�������ģ�������Ȼ�ɹ�����Ҫ��һ����Ϣ�Ĳ��¼��Ϣ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>&gt;=0:    �ɹ�������ֵ�� errorinfos ��Ԫ�صĸ���</para>
        /// </returns>
        public long SetEntities(
            DigitalPlatform.Stop stop,
            string strBiblioRecPath,
            EntityInfo [] entityinfos,
            out EntityInfo[] errorinfos,
            out string strError)
        {
            errorinfos = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetEntities(
                    strBiblioRecPath,
                    entityinfos,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetEntities(
                    out errorinfos,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �г���Ŀ�����;����Ϣ
        // parameters:
        //      stop    ֹͣ����
        //      strLang ���Դ��롣һ��Ϊ"zh"
        //      infos   ���ؼ���;����Ϣ����
        // rights:
        //      ��Ҫ listbibliodbfroms (����listdbfroms) Ȩ��
        // return:
        //      -1  ����
        //      0   ����
        /// <summary>
        /// ��ø������ݿ�ļ���;����Ϣ
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strDbType">���ݿ�����</param>
        /// <param name="strLang">���Դ���</param>
        /// <param name="infos">���ؼ���;����Ϣ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    ��ǰϵͳ��û�д������ݿ⣬�����޷���������;����Ϣ</para>
        /// <para>1:    �ɹ�</para>
        /// </returns>
        public long ListDbFroms(
            DigitalPlatform.Stop stop,
            string strDbType,
            string strLang,
            out BiblioDbFromInfo[] infos,
            out string strError)
        {
            infos = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginListBiblioDbFroms(
                    strDbType,
                    strLang,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndListBiblioDbFroms(
                    out infos,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ������Ŀ��Ϣ
        // parameters:
        //      strBiblioDbNames    ��Ŀ����������Ϊ����������Ҳ�����Ƕ���(���)�ָ�Ķ��߿����б�������Ϊ <ȫ��>/<all> ֮һ����ʾȫ����Ŀ�⡣
        //      strQueryWord    ������
        //      nPerMax һ�����н��������������Ϊ-1����ʾ�����ơ�
        //      strFromStyle ����;����ɫֵ��
        //      strMathStyle    ƥ�䷽ʽ exact left right middle
        //      strLang ���Դ��롣һ��Ϊ"zh"
        //      strResultSetName    ���������
        //      strQueryXml �������ݿ��ں˲���ʹ�õ�XML����ʽ�����ڽ��е���
        // rights:
        //      ��Ҫ searchbiblio Ȩ��
        // return:
        //      -1  ����
        //      >=0 ���н������
        /// <summary>
        /// ������Ŀ��
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strBiblioDbNames">��Ŀ�����б�</param>
        /// <param name="strQueryWord">������</param>
        /// <param name="nPerMax">�����������-1 ��ʾ������</param>
        /// <param name="strFromStyle">����;����ɫ�������Ƕ����ɫֵ���о�</param>
        /// <param name="strMatchStyle">ƥ�䷽ʽ</param>
        /// <param name="strLang">���Դ���</param>
        /// <param name="strResultSetName">�������</param>
        /// <param name="strSearchStyle">������ʽ����������Ӵ�"desc"��ʾ���н�����ս������У������Ӵ�"asc"��ʾ�����������С�ȱʡΪ��������</param>
        /// <param name="strOutputStyle">�����ʽ</param>
        /// <param name="strQueryXml">���� dp2Library �������ļ���ʽ XML �ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    û������</para>
        /// <para>&gt;=1:   ���С�ֵΪ���еļ�¼����</para>
        /// </returns>
        public long SearchBiblio(
            DigitalPlatform.Stop stop,
            string strBiblioDbNames,
            string strQueryWord,
            int nPerMax,
            string strFromStyle,
            string strMatchStyle,
            string strLang,
            string strResultSetName,
            string strSearchStyle,
            string strOutputStyle,
            out string strQueryXml,
            out string strError)
        {
            strError = "";
            strQueryXml = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchBiblio(
                    strBiblioDbNames,
                    strQueryWord,
                    nPerMax,
                    strFromStyle,
                    strMatchStyle,
                    strLang,
                    strResultSetName,
                    strSearchStyle,
                    strOutputStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchBiblio(
                    out strQueryXml,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }


        // �����Ŀ��¼
        /// <summary>
        /// �����Ŀ��¼
        /// </summary>
        /// <param name="stop">Stop ����</param>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strBiblioXml">XML ��ʽ����Ŀ��¼���ݡ�����ǰ����������ύ��</param>
        /// <param name="strBiblioType">Ҫ��ȡ����Ϣ��ʽ����</param>
        /// <param name="strBiblio">������Ϣ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    û���ҵ�ָ��·������Ŀ��¼</para>
        /// <para>1:    �ɹ�</para>
        /// </returns>
        public long GetBiblioInfo(
            DigitalPlatform.Stop stop,
            string strBiblioRecPath,
            string strBiblioXml,
            string strBiblioType,
            out string strBiblio,
            out string strError)
        {
            strBiblio = "";
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetBiblioInfo(
                    strBiblioRecPath,
                    strBiblioXml,
                    strBiblioType,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetBiblioInfo(
                    out strBiblio,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // *** ��API�Ѿ���ֹ ***
#if NO
        // ������Ų���
        public long SearchItemDup(
            DigitalPlatform.Stop stop,
            string strBarcode,
            int nMax,
            out string [] paths,
            out string strError)
        {
            paths = null;
            strError = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchItemDup(
                    strBarcode,
                    nMax,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchItemDup(
                    out paths,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }
#endif

        // ���ֵ�б�
        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strTableName">ֵ�б������</param>
        /// <param name="strDbName">���ݿ���������Ϊ��</param>
        /// <param name="values">����ֵ�б��ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long GetValueTable(
            DigitalPlatform.Stop stop,
            string strTableName,
            string strDbName,
            out string[] values,
            out string strError)
        {
            values = null;
            strError = "";

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetValueTable(
                    strTableName,
                    strDbName,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetValueTable(
                    out values,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // result.Value
        //      -1  error
        //      0   file not found
        //      1   succeed
        //      2   ������Χ
        /// <summary>
        /// ��ò�����־��һ�λ�ö���������־
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strFileName">��־�ļ�������̬Ϊ"20120101.log"</param>
        /// <param name="lIndex">��־��¼��ţ��� 0 ��ʼ����</param>
        /// <param name="lHint">��ʾ����</param>
        /// <param name="nCount">Ҫ��õ���־��¼����</param>
        /// <param name="strStyle">��ȡ���</param>
        /// <param name="strFilter">���˷��</param>
        /// <param name="records">������־��¼��Ϣ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    ָ������־�ļ�û���ҵ�</para>
        /// <para>1:    �ɹ�</para>
        /// <para>2:    ������Χ</para>
        /// </returns>
        public long GetOperLogs(
            DigitalPlatform.Stop stop,
            string strFileName,
            long lIndex,
            long lHint,
            int nCount,
            string strStyle,
            string strFilter,
            out OperLogInfo[] records,
            out string strError)
        {
            strError = "";
            records = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetOperLogs(
                    strFileName,
                    lIndex,
                    lHint,
                    nCount,
                    strStyle,
                    strFilter,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetOperLogs(
                    out records,
                    soapresult);
                strError = result.ErrorInfo;
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    strError = result.ErrorInfo;    // 2013/11/20
                    return -1;
                }
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        //
        // �����־
        // result.Value
        //      -1  error
        //      0   file not found
        //      1   succeed
        //      2   ������Χ
        /// <summary>
        /// �����־
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strFileName">��־�ļ�������̬Ϊ"20120101.log"</param>
        /// <param name="lIndex">��־��¼��ţ��� 0 ��ʼ����</param>
        /// <param name="lHint">��ʾ����</param>
        /// <param name="strStyle">��ȡ���</param>
        /// <param name="strFilter">���˷��</param>
        /// <param name="strXml">������־��¼ XML</param>
        /// <param name="lHintNext">������һ����־��¼�İ�ʾ����</param>
        /// <param name="lAttachmentFragmentStart">����Ƭ�ο�ʼλ��</param>
        /// <param name="nAttachmentFragmentLength">Ҫ��ȡ�ĸ���Ƭ�ϳ���</param>
        /// <param name="attachment_data">���ظ�������</param>
        /// <param name="lAttachmentTotalLength">���ظ����ܳ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    ָ������־�ļ�û���ҵ�</para>
        /// <para>1:    �ɹ�</para>
        /// <para>2:    ������Χ</para>
        /// </returns>
        public long GetOperLog(
            DigitalPlatform.Stop stop,
            string strFileName,
            long lIndex,
            long lHint,
            string strStyle,
            string strFilter,
            out string strXml,
            out long lHintNext,
            long lAttachmentFragmentStart,
            int nAttachmentFragmentLength,
            out byte[] attachment_data,
            out long lAttachmentTotalLength,
            out string strError)
        {
            strError = "";

            strXml = "";
            lHintNext = -1;

            attachment_data = null;
            lAttachmentTotalLength = 0;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetOperLog(
                    strFileName,
                    lIndex,
                    lHint,
                    strStyle,
                    strFilter,
                    lAttachmentFragmentStart,
                    nAttachmentFragmentLength,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetOperLog(
                    out strXml,
                    out lHintNext,
                    out attachment_data,
                    out lAttachmentTotalLength,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }




        }

        // �������
        /// <summary>
        /// �����ͨ����
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strAction">��������</param>
        /// <param name="strName">������</param>
        /// <param name="nStart">Ҫ��õ�Ԫ�ؿ�ʼλ�á��� 0 ��ʼ����</param>
        /// <param name="nCount">Ҫ��õ�Ԫ����������Ϊ -1 ��ʾϣ����þ����ܶ��Ԫ��</param>
        /// <param name="contents">���ص�������Ϣ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>&gt;=0:   �������</para>
        /// </returns>
        public long GetCalendar(
            DigitalPlatform.Stop stop,
            string strAction,
            string strName,
            int nStart,
            int nCount,
            out CalenderInfo [] contents,
            out string strError)
        {
            strError = "";

            contents = null;
        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetCalendar(
                    strAction,
                    strName,
                    nStart,
                    nCount,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetCalendar(
                    out contents,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ���á��޸�����
        /// <summary>
        /// ������ͨ����
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strAction">��������</param>
        /// <param name="info">������Ϣ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long SetCalendar(
    DigitalPlatform.Stop stop,
    string strAction,
    CalenderInfo info,
    out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetCalendar(
                    strAction,
                    info,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetCalendar(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����������
        /// <summary>
        /// ����������
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strName">������������</param>
        /// <param name="strAction">��������</param>
        /// <param name="info">������Ϣ</param>
        /// <param name="resultInfo">����������Ϣ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0 �� 1:    �ɹ�</para>
        /// </returns>
        public long BatchTask(
            DigitalPlatform.Stop stop,
            string strName,
            string strAction,
            BatchTaskInfo info,
            out BatchTaskInfo resultInfo,
            out string strError)
        {
            strError = "";
            resultInfo = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginBatchTask(
                    strName,
                    strAction,
                    info,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndBatchTask(
                    out resultInfo,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����������Ϣ
        /// <summary>
        /// ֱ���� XML ����ʽ���м���
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strQueryXml">����ʽ������ dp2Kernel ������� XML ����ʽ��ʽ</param>
        /// <param name="strResultSetName">�������</param>
        /// <param name="strOutputStyle">�����ʽ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    û������</para>
        /// <para>&gt;=1:   ���С�����ֵΪ���еļ�¼����</para>
        /// </returns>
        public long Search(
            DigitalPlatform.Stop stop,
            string strQueryXml,
            string strResultSetName,
            string strOutputStyle,
            out string strError)
        {
            strError = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearch(
                    strQueryXml,
                    strResultSetName,
                    strOutputStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearch(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }

        // �����ĿժҪ
        /// <summary>
        /// �����ĿժҪ
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strItemBarcode">�������</param>
        /// <param name="strConfirmItemRecPath">����ȷ�ϲ��¼��·��</param>
        /// <param name="strBiblioRecPathExclude">ϣ���ų�������Ŀ��¼·������ʽΪ���ż���Ķ����¼·��</param>
        /// <param name="strBiblioRecPath">������Ŀ��¼·��</param>
        /// <param name="strSummary">������ĿժҪ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    ָ���Ĳ��¼������Ŀ��¼û���ҵ�</para>
        /// <para>1:    �ɹ�</para>
        /// </returns>
        public long GetBiblioSummary(
            DigitalPlatform.Stop stop,
            string strItemBarcode,
            string strConfirmItemRecPath,
            string strBiblioRecPathExclude,
            out string strBiblioRecPath,
            out string strSummary,
            out string strError)
        {
            strError = "";
            strBiblioRecPath = "";
            strSummary = "";
#if NO
            // ����
            strSummary = "test";
            return 0;
#endif

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetBiblioSummary(
                    strItemBarcode,
                    strConfirmItemRecPath,
                    strBiblioRecPathExclude,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetBiblioSummary(
                    out strBiblioRecPath,
                    out strSummary,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }

        // ����ʱ��
        // return:
        //      -1  ����
        //      0   ����
        /// <summary>
        /// ����ϵͳ��ǰʱ��
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strTime">Ҫ���õĵ�ǰʱ�䡣��ʽΪ RFC1123</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long SetClock(
            DigitalPlatform.Stop stop,
            string strTime,
            out string strError)
        {
            strError = "";

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetClock(
                    strTime,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetClock(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ���ʱ��
        /// <summary>
        /// ���ϵͳ��ǰʱ��
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strTime">����ϵͳ�ĵ�ǰʱ�䡣��ʽΪ RFC1123</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long GetClock(
            DigitalPlatform.Stop stop,
            out string strTime,
            out string strError)
        {
            strTime = "";
            strError = "";

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetClock(
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetClock(
                    out strTime,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ��֤��������
        /// <summary>
        /// ��֤�����ʻ�������
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strReaderBarcode">����֤�����</param>
        /// <param name="strReaderPassword">Ҫ��֤�Ķ����ʻ�����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ��֤���̳���</para>
        /// <para>0:    ���벻��ȷ</para>
        /// <para>1:    ������ȷ</para>
        /// </returns>
        public long VerifyReaderPassword(
            DigitalPlatform.Stop stop,
            string strReaderBarcode,
            string strReaderPassword,
            out string strError)
        {
            strError = "";

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginVerifyReaderPassword(
                    strReaderBarcode,
                    strReaderPassword,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndVerifyReaderPassword(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �޸Ķ�������
        /// <summary>
        /// �޸Ķ����ʻ�������
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strReaderBarcode">����֤�����</param>
        /// <param name="strReaderOldPassword">�����ʻ��ľ�����</param>
        /// <param name="strReaderNewPassword">Ҫ�޸ĳɵ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �����벻��ȷ</para>
        /// <para>1:    ��������ȷ,���޸�Ϊ������</para>
        /// </returns>
        public long ChangeReaderPassword(
            DigitalPlatform.Stop stop,
            string strReaderBarcode,
            string strReaderOldPassword,
            string strReaderNewPassword,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginChangeReaderPassword(
                    strReaderBarcode,
                    strReaderOldPassword,
                    strReaderNewPassword,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndChangeReaderPassword(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �����������
        // ��ʵ��� API ����Ҫ��¼
        /// <summary>
        /// ����������롣�� API ����Ҫ��¼���ɵ���
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strParameters">�����ַ���������Ϊ tel=?????,barcode=?????,name=????? �� email=?????,barcode=??????,name=?????? �� librarycode=????</param>
        /// <param name="strMessageTemplate">��Ϣ����ģ�塣���п���ʹ�� %name% %barcode% %temppassword% %expiretime% %period% �Ⱥ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    ��Ϊ�������߱�����û�гɹ�ִ��</para>
        /// <para>1:     ���ܳɹ�ִ��</para>
        /// </returns>
        public long ResetPassword(
            DigitalPlatform.Stop stop,
            string strParameters,
            string strMessageTemplate,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginResetPassword(
                    strParameters,
                    strMessageTemplate,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndResetPassword(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����������ݿ��ڵ�����
        /// <summary>
        /// ����������ݿ�ļ�¼
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long ClearAllDbs(
            DigitalPlatform.Stop stop,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginClearAllDbs(
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndClearAllDbs(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �������ݿ�
        /// <summary>
        /// �������ݿ�
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strAction">��������</param>
        /// <param name="strDatabaseName">���ݿ���</param>
        /// <param name="strDatabaseInfo">���ݿ���Ϣ</param>
        /// <param name="strOutputInfo">���ز���������ݿ���Ϣ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0 �� 1:    �ɹ�</para>
        /// </returns>
        public long ManageDatabase(
            DigitalPlatform.Stop stop,
            string strAction,
            string strDatabaseName,
            string strDatabaseInfo,
            out string strOutputInfo,
            out string strError)
        {
            strError = "";
            strOutputInfo = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginManageDatabase(
                    strAction,
                    strDatabaseName,
                    strDatabaseInfo,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndManageDatabase(
                    out strOutputInfo,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����û���Ϣ
        /// <summary>
        /// ����û���Ϣ
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strAction">��������</param>
        /// <param name="strName">�û���</param>
        /// <param name="nStart">Ҫ��ȡ�û���ϢԪ�صĿ�ʼλ��</param>
        /// <param name="nCount">Ҫ��ȡ���û���ϢԪ�صĸ�����-1 ��ʾϣ����ȡ�����ܶ��Ԫ��</param>
        /// <param name="contents">�����û���Ϣ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>&gt;=0:   ����û���ϢԪ�ص�����</para>
        /// </returns>
        public long GetUser(
            DigitalPlatform.Stop stop,
            string strAction,
            string strName,
            int nStart,
            int nCount,
            out UserInfo[] contents,
            out string strError)
        {
            strError = "";

            contents = null;
        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetUser(
                    strAction,
                    strName,
                    nStart,
                    nCount,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetUser(
                    out contents,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �����û���Ϣ
        /// <summary>
        /// �����û���Ϣ
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strAction">��������</param>
        /// <param name="info">�û���Ϣ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long SetUser(
            DigitalPlatform.Stop stop,
            string strAction,
            UserInfo info,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetUser(
                    strAction,
                    info,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetUser(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // 
        /// <summary>
        /// ���ͨ����Ϣ
        /// </summary>
        /// <param name="stop">ֹͣ����</param>
        /// <param name="strQuery">����ʽ������ "ip=...,username=..."</param>
        /// <param name="strStyle">���"ip-count"�����߿�</param>
        /// <param name="nStart">��ʼƫ�ƣ��� 0 ��ʼ����</param>
        /// <param name="nCount">ϣ������ȡ���ٸ����-1 ��ʾ�뾡���ܶ�ػ�ȡ</param>
        /// <param name="contents">����ͨ����Ϣ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; ����: ��������������������ܻ���ڷ��ص� contents �а���������</returns>
        public long GetChannelInfo(
            DigitalPlatform.Stop stop,
            string strQuery,
            string strStyle,
            int nStart,
            int nCount,
            out ChannelInfo[] contents,
            out string strError)
        {
            strError = "";

            contents = null;
        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetChannelInfo(
                    strQuery,
                    strStyle,
                    nStart,
                    nCount,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetChannelInfo(
                    out contents,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����ͨ��
        /// <summary>
        /// ����ͨ��
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strAction">��������</param>
        /// <param name="strStyle">���</param>
        /// <param name="requests">��������</param>
        /// <param name="results">���ؽ������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>&gt;=0:   �����ܽ������</para>
        /// </returns>
        public long ManageChannel(
    DigitalPlatform.Stop stop,
    string strAction,
    string strStyle,
    ChannelInfo[] requests,
    out ChannelInfo[] results,
    out string strError)
        {
            strError = "";

            results = null;
        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginManageChannel(
                    strAction,
                    strStyle,
                    requests,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndManageChannel(
                    out results,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �ƶ����߼�¼
        // return:
        //      -1  error
        //      0   �Ѿ��ɹ��ƶ�
        /// <summary>
        /// �ƶ����߼�¼
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strSourceRecPath">Դ��¼·��</param>
        /// <param name="strTargetRecPath">����ǰ����Ŀ���¼·�������ú󷵻�ʵ���ƶ�����Ŀ���¼·��</param>
        /// <param name="target_timestamp">����Ŀ���¼����ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long MoveReaderInfo(
            DigitalPlatform.Stop stop,
            string strSourceRecPath,
            ref string strTargetRecPath,
            out byte[] target_timestamp,
            out string strError)
        {
            strError = "";
            target_timestamp = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginMoveReaderInfo(
                    strSourceRecPath,
                    ref strTargetRecPath,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndMoveReaderInfo(
                    ref strTargetRecPath,
                    out target_timestamp,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // DevolveReaderInfo
        /// <summary>
        /// ת�ƽ�����Ϣ
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strSourceReaderBarcode">Դ֤�����</param>
        /// <param name="strTargetReaderBarcode">Ŀ��֤�����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    û�б�Ҫת�ơ���Դ���߼�¼��û����Ҫת�ƵĽ�����Ϣ</para>
        /// <para>1:    �Ѿ��ɹ�ת��</para>
        /// </returns>
        public long DevolveReaderInfo(
            DigitalPlatform.Stop stop,
            string strSourceReaderBarcode,
            string strTargetReaderBarcode,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginDevolveReaderInfo(
                    strSourceReaderBarcode,
                    strTargetReaderBarcode,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndDevolveReaderInfo(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �޸��Լ�������
        /// <summary>
        /// �޸��Լ�������
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strUserName">�û���</param>
        /// <param name="strOldPassword">������</param>
        /// <param name="strNewPassword">������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0:    �ɹ�</para>
        /// </returns>
        public long ChangeUserPassword(
            DigitalPlatform.Stop stop,
            string strUserName,
            string strOldPassword,
            string strNewPassword,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginChangeUserPassword(
                    strUserName,
                    strOldPassword,
                    strNewPassword,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndChangeUserPassword(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // У������
        /// <summary>
        /// У�������
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strBarcode">�����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>0/1/2:    �ֱ��Ӧ�����Ϸ��ı���š�/���Ϸ��Ķ���֤����š�/���Ϸ��Ĳ�����š�</para>
        /// </returns>
        public long VerifyBarcode(
            DigitalPlatform.Stop stop,
            string strLibraryCode,
            string strBarcode,
            out string strError)
        {
            strError = "";

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginVerifyBarcode(
                    strLibraryCode,
                    strBarcode,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndVerifyBarcode(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        public long ListFile(
            DigitalPlatform.Stop stop,
            string strAction,
            string strCategory,
            string strFileName,
            long lStart,
            long lLength,
            out FileItemInfo [] infos,
            out string strError)
        {
            strError = "";
            infos = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginListFile(
                    strAction,
                    strCategory,
                    strFileName,
                    lStart,
                    lLength,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndListFile(
                    out infos,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ���ϵͳ�����ļ�
        // parameters:
        //      strCategory �ļ����ࡣĿǰֻ��ʹ�� cfgs
        //      lStart  ��Ҫ����ļ����ݵ���㡣���Ϊ-1����ʾ(baContent��)�������ļ�����
        //      lLength ��Ҫ��õĴ�lStart��ʼ�����byte�������Ϊ-1����ʾϣ�������ܶ��ȡ��(���ǲ��ܱ�֤һ����β)
        // rights:
        //      ��Ҫ getsystemparameter Ȩ��
        // return:
        //      result.Value    -1 �������� �ļ����ܳ���
        /// <summary>
        /// ���ϵͳ�����ļ�
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="strCategory">�ļ�����</param>
        /// <param name="strFileName">�ļ���</param>
        /// <param name="lStart">ϣ�����ص��ļ����ݵ���ʼλ��</param>
        /// <param name="lLength">ϣ�����ص��ļ����ݵĳ���</param>
        /// <param name="baContent">�����ļ�����</param>
        /// <param name="strFileTime">�����ļ�������޸�ʱ�䡣RFC1123 ��ʽ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1:   ����</para>
        /// <para>&gt;=0:   �ɹ���ֵΪ��ָ���ļ��ĳ���</para>
        /// </returns>
        public long GetFile(
            DigitalPlatform.Stop stop,
            string strCategory,
            string strFileName,
            long lStart,
            long lLength,
            out byte[] baContent,
            out string strFileTime,
            out string strError)
        {
            strError = "";
            strFileTime = "";
            baContent = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetFile(
                    strCategory,
                    strFileName,
                    lStart,
                    lLength,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetFile(
                    out baContent,
                    out strFileTime,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ���ϵͳ����
        // parameters:
        //      stop    ֹͣ����
        //      strCategory ��������Ŀ¼
        //      strName ������
        //      strValue    ���ز���ֵ
        // rights:
        //      ��Ҫ getsystemparameter Ȩ��
        // return:
        //      -1  ����
        //      0   û�еõ���Ҫ��Ĳ���ֵ
        //      1   �õ���Ҫ��Ĳ���ֵ
        public long GetSystemParameter(
            DigitalPlatform.Stop stop,
            string strCategory,
            string strName,
            out string strValue,
            out string strError)
        {
            strError = "";
            strValue = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetSystemParameter(
                    strCategory,
                    strName,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetSystemParameter(
                    out strValue,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����ϵͳ����
        // parameters:
        //      stop    ֹͣ����
        //      strCategory ��������Ŀ¼
        //      strName ������
        //      strValue    ����ֵ
        // rights:
        //      ��Ҫ setsystemparameter Ȩ��
        // return:
        //      -1  ����
        //      0   �ɹ�
        public long SetSystemParameter(
            DigitalPlatform.Stop stop,
            string strCategory,
            string strName,
            string strValue,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetSystemParameter(
                    strCategory,
                    strName,
                    strValue,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetSystemParameter(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �����ָ�
        public long UrgentRecover(
            DigitalPlatform.Stop stop,
            string strXML,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginUrgentRecover(
                    strXML,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndUrgentRecover(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        public long RepairBorrowInfo(
            DigitalPlatform.Stop stop,
            string strAction,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            int nStart,   // 2008/10/27
            int nCount,   // 2008/10/27
            out int nProcessedBorrowItems,   // 2008/10/27
            out int nTotalBorrowItems,   // 2008/10/27
            out string strOutputReaderBarcode,
            out string[] aDupPath,
            out string strError)
        {
            strError = "";
            nProcessedBorrowItems = 0;
            nTotalBorrowItems = 0;
            aDupPath = null;
            strOutputReaderBarcode = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginRepairBorrowInfo(
                    strAction,
                    strReaderBarcode,
                    strItemBarcode,
                    strConfirmItemRecPath,
                    nStart,
                    nCount,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndRepairBorrowInfo(
                    out nProcessedBorrowItems,
                    out nTotalBorrowItems,
                    out strOutputReaderBarcode,
                    out aDupPath,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }


        }

        // �����Ŀ��¼��Ϣ(һ�ο��Ի�ö���)
        // parameters:
        //      strBiblioRecPath    ��Ŀ��¼��·��
        //      formats ��ʽ�б������ú��еĶ��ָ�ʽ��xml html text @??? summary
        //      results ���صĽ���ַ�������
        //      baTimestamp ���صļ�¼ʱ���
        // rights:
        //      ��Ҫ getbiblioinfo Ȩ��
        //      ���formats�а�����"summary"��ʽ������Ҫ getbibliosummary Ȩ��
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        public long GetBiblioInfos(
            DigitalPlatform.Stop stop,
            string strBiblioRecPath,
            string strBiblioXml,    // 2013/3/6
            string[] formats,
            out string[] results,
            out byte[] baTimestamp,
            out string strError)
        {
            results = null;
            baTimestamp = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetBiblioInfos(
                    strBiblioRecPath,
                    strBiblioXml,
                    formats,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetBiblioInfos(
                    out results,
                    out baTimestamp,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ������Ŀ��Ϣ
        public long SetBiblioInfo(
            DigitalPlatform.Stop stop,
            string strAction,
            string strBiblioRecPath,
            string strBiblioType,
            string strBiblio,
            byte[] baTimestamp,
            string strComment,
            out string strOutputBiblioRecPath,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            strError = "";
            strOutputBiblioRecPath = "";
            baOutputTimestamp = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetBiblioInfo(
                    strAction,
                    strBiblioRecPath,
                    strBiblioType,
                    strBiblio,
                    baTimestamp,
                    strComment,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetBiblioInfo(
                    out strOutputBiblioRecPath,
                    out baOutputTimestamp,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ������Ŀ��Ϣ
        public long CopyBiblioInfo(
            DigitalPlatform.Stop stop,
            string strAction,
            string strBiblioRecPath,
            string strBiblioType,
            string strBiblio,
            byte[] baTimestamp,
            string strNewBiblioRecPath,
            string strNewBiblio,
            string strMergeStyle,
            out string strOutputBiblio,
            out string strOutputBiblioRecPath,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            strError = "";
            strOutputBiblioRecPath = "";
            baOutputTimestamp = null;
            strOutputBiblio = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginCopyBiblioInfo(
                    strAction,
                    strBiblioRecPath,
                    strBiblioType,
                    strBiblio,
                    baTimestamp,
                    strNewBiblioRecPath,
                    strNewBiblio,
                    strMergeStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndCopyBiblioInfo(
                    out strOutputBiblio,
                    out strOutputBiblioRecPath,
                    out baOutputTimestamp,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ��ݵǼ�
        public long PassGate(
            DigitalPlatform.Stop stop,
            string strReaderBarcode,
            string strGateName,
            string strResultTypeList,
            out string [] results,
            out string strError)
        {
            strError = "";
            results = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginPassGate(
                    strReaderBarcode,
                    strGateName,
                    strResultTypeList,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndPassGate(
                    out results,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }


        // ����Ѻ�𽻷�(�����˷�)����
        // parameters:
        //      strAction   ֵΪforegift return֮һ
        public long Foregift(
            DigitalPlatform.Stop stop,
            string strAction,
            string strReaderBarcode,
            out string strOutputReaderXml,
            out string strOutputID,
            out string strError)
        {
            strOutputReaderXml = "";
            strOutputID = ""; 
            
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginForegift(
                    strAction,
                    strReaderBarcode,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndForegift(out strOutputReaderXml,
                    out strOutputID,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ������𽻷�����
        public long Hire(
            DigitalPlatform.Stop stop,
            string strAction,
            string strReaderBarcode,
            out string strOutputReaderXml,
            out string strOutputID,
            out string strError)
        {
            strOutputReaderXml = "";
            strOutputID = ""; 
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginHire(
                    strAction,
                    strReaderBarcode,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndHire(out strOutputReaderXml,
                    out strOutputID, soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����
        public long Settlement(
            DigitalPlatform.Stop stop,
            string strAction,
            string [] ids,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSettlement(
                    strAction,
                    ids,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSettlement(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // (����һ���ż���ϵ)������ĳһ���ͬ�������ȡ��
        public long SearchOneClassCallNumber(
            DigitalPlatform.Stop stop,
            string strArrangeGroupName,
            string strClass,
            string strResultSetName,
            out string strQueryXml,
            out string strError)
        {
            strError = "";
            strQueryXml = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchOneClassCallNumber(
                    strArrangeGroupName,
                    strClass,
                    strResultSetName,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchOneClassCallNumber(
                    out strQueryXml,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }


        // �����ȡ�ż���������Ϣ
        public long GetCallNumberSearchResult(
            DigitalPlatform.Stop stop,
            string strArrangeGroupName,
            string strResultSetName,
            long lStart,
            long lCount,
            string strBrowseInfoStyle,
            string strLang,
            out CallNumberSearchResult[] searchresults,
            out string strError)
        {
            strError = "";
            searchresults = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetCallNumberSearchResult(
                    strArrangeGroupName,
                    strResultSetName,
                    lStart,
                    lCount,
                    strBrowseInfoStyle,
                    strLang,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetCallNumberSearchResult(
                    out searchresults,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        public long GetOneClassTailNumber(
    DigitalPlatform.Stop stop,
    string strArrangeGroupName,
    string strClass,
    out string strTailNumber,
    out string strError)
        {
            strError = "";
            strTailNumber = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetOneClassTailNumber(
                    strArrangeGroupName,
                    strClass,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetOneClassTailNumber(
                    out strTailNumber,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �����ִκ�β��
        public long SetOneClassTailNumber(
            DigitalPlatform.Stop stop,
            string strAction,
            string strArrangeGroupName,
            string strClass,
            string strTestNumber,
            out string strOutputNumber,
            out string strError)
        {
            strError = "";
            strOutputNumber = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetOneClassTailNumber(
                    strAction,
                    strArrangeGroupName,
                    strClass,
                    strTestNumber,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetOneClassTailNumber(
                    out strOutputNumber,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����ͬ�����¼�������ִκź�ժҪ��Ϣ
        public long SearchUsedZhongcihao(
            DigitalPlatform.Stop stop,
            string strZhongcihaoGroupName,
            string strClass,
            string strResultSetName,
            out string strQueryXml,
            out string strError)
        {
            strError = "";
            strQueryXml = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchUsedZhongcihao(
                    strZhongcihaoGroupName,
                    strClass, 
                    strResultSetName,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchUsedZhongcihao(
                    out strQueryXml,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����ִκż���������Ϣ
        public long GetZhongcihaoSearchResult(
            DigitalPlatform.Stop stop,
            string strZhongcihaoGroupName,
            string strResultSetName,
            long lStart,
            long lCount,
            string strBrowseInfoStyle,
            string strLang,
            out ZhongcihaoSearchResult[] searchresults,
            out string strError)
        {
            strError = "";
            searchresults = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetZhongcihaoSearchResult(
                    strZhongcihaoGroupName,
                    strResultSetName, lStart,
                    lCount,
                    strBrowseInfoStyle,
                    strLang,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetZhongcihaoSearchResult(
                    out searchresults,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        public long GetZhongcihaoTailNumber(
            DigitalPlatform.Stop stop,
            string strZhongcihaoGroupName,
            string strClass,
            out string strTailNumber,
            out string strError)
        {
            strError = "";
            strTailNumber = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetZhongcihaoTailNumber(
                    strZhongcihaoGroupName,
                    strClass,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetZhongcihaoTailNumber(
                    out strTailNumber,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �����ִκ�β��
        public long SetZhongcihaoTailNumber(
            DigitalPlatform.Stop stop,
            string strAction,
            string strZhongcihaoGroupName,
            string strClass,
            string strTestNumber,
            out string strOutputNumber,
            out string strError)
        {
            strError = "";
            strOutputNumber = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetZhongcihaoTailNumber(
                    strAction,
                    strZhongcihaoGroupName,
                    strClass,
                    strTestNumber,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetZhongcihaoTailNumber(
                    out strOutputNumber,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

         // ����
        // parameters:
        //      strUsedProjectName  ʵ��ʹ�õĲ��ط�����
        public long SearchDup(
            DigitalPlatform.Stop stop,
            string strOriginBiblioRecPath,
            string strOriginBiblioRecXml,
            string strProjectName,
            string strStyle,
            out string strUsedProjectName,
            out string strError)
        {
            strError = "";
            strUsedProjectName = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchDup(
                    strOriginBiblioRecPath,
                    strOriginBiblioRecXml,
                    strProjectName,
                    strStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchDup(
                    out strUsedProjectName,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }

        // �г����ط�����Ϣ
        public long ListDupProjectInfos(
            DigitalPlatform.Stop stop,
            string strOriginBiblioDbName,
            out DupProjectInfo[] results,
            out string strError)
        {
            strError = "";
            results = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginListDupProjectInfos(
                    strOriginBiblioDbName,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndListDupProjectInfos(
                    out results,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ��ò��ؼ������н��
        public long GetDupSearchResult(
            DigitalPlatform.Stop stop,
            long lStart,
            long lCount,
            string strBrowseInfoStyle,
            out DupSearchResult[] searchresults,
            out string strError)
        {
            strError = "";
            searchresults = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetDupSearchResult(
                    lStart,
                    lCount,
                    strBrowseInfoStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetDupSearchResult(
                    out searchresults,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        public long GetUtilInfo(
            DigitalPlatform.Stop stop,
            string strAction,
            string strDbName,
            string strFrom,
            string strKey,
            string strValueAttrName,
            out string strValue,
            out string strError)
        {
            strError = "";
            strValue = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetUtilInfo(
                    strAction,
                    strDbName,
                    strFrom,
                    strKey,
                    strValueAttrName,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetUtilInfo(
                    out strValue,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        public long SetUtilInfo(
            DigitalPlatform.Stop stop,
            string strAction,
            string strDbName,
            string strFrom,
            string strRootElementName,
            string strKeyAttrName,
            string strValueAttrName,
            string strKey,
            string strValue,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetUtilInfo(
                    strAction,
                    strDbName,
                    strFrom,
                    strRootElementName,
                    strKeyAttrName,
                    strValueAttrName,
                    strKey,
                    strValue,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetUtilInfo(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ��ȡ��Դ
        // parameters:
        //      strResPath  ��Դ��·����һ�����ݿ��¼Ϊ"���ݿ���/1"��̬�������ݿ��¼�����Ķ�����Դ����Ϊ"���ݿ���/object/0"��̬
        //      nStart  ����Ҫ��õ�byte��ʼλ��
        //      nLength ����Ҫ��õ�byte����
        //      strStyle    ����б�Ϊ���ŷָ����ַ���ֵ�б�ȡֵΪdata/metadata/timestamp/outputpath֮һ
        //                  data��ʾҪ��baContent�����ڷ�����Դ��������
        //                  metadata��ʾҪ��strMetadata�����ڷ���Ԫ��������
        //                  timestamp��ʾҪ��baOutputTimestam�����ڷ�����Դ��ʱ�������
        //                  outputpath��ʾҪ��strOutputResPath�����ڷ���ʵ�ʼ�¼·������
        //      baContent   ���ص�byte����
        //      strMetadata ���ص�Ԫ��������
        //      strOutputResPath    ���ص�ʵ�ʼ�¼·��
        //      baOutputTimestamp   ���ص���Դʱ���
        // rights:
        //      ��Ҫ getres Ȩ��
        // return:
        //      -1  ����
        //      0   �ɹ�
        public long GetRes(
            DigitalPlatform.Stop stop,
            string strResPath,
            long lStart,
            int nLength,
            string strStyle,
            out byte[] baContent,
            out string strMetadata,
            out string strOutputResPath,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            strError = "";
            baContent = null;
            strMetadata = "";
            strOutputResPath = "";
            baOutputTimestamp = null;

            /*
            if (stop != null)
            {
                Debug.Assert(stop.State == -1 || stop.State == 0, "");
            }*/

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetRes(
                    strResPath,
                    lStart,
                    nLength,
                    strStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetRes(
                    out baContent,
                    out strMetadata,
                    out strOutputResPath,
                    out baOutputTimestamp,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        public const string GETRES_ALL_STYLE = "content,data,metadata,timestamp,outputpath";

        // �����Դ����װ�汾 -- �����ַ����汾��
        // return:
        //		strStyle	һ������Ϊ"content,data,metadata,timestamp,outputpath";
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetRes(
            DigitalPlatform.Stop stop,
            string strPath,
            string strStyle,
            out string strResult,
            out string strMetaData,
            out byte[] baOutputTimeStamp,
            out string strOutputResPath,
            out string strError)
        {
            strMetaData = "";
            strResult = "";
            strError = "";
            strOutputResPath = "";
            baOutputTimeStamp = null;

            byte[] baContent = null;

            int nStart = 0;
            int nPerLength = -1;

            byte[] baTotal = null;

            for (; ; )
            {
                if (stop != null)
                    Application.DoEvents();	// ���ý������Ȩ

                long lRet = this.GetRes(stop,
                        strPath,
                        nStart,
                        nPerLength,
                        strStyle,
                        out baContent,
                        out strMetaData,
                        out strOutputResPath,
                        out baOutputTimeStamp,
                        out strError);
                if (lRet == -1)
                    return -1;

                if (StringUtil.IsInList("data", strStyle) != true)
                    break;

                // 2011/1/22
                if (StringUtil.IsInList("content", strStyle) == false)
                    return lRet;

                baTotal = ByteArray.Add(baTotal, baContent);

                Debug.Assert(baContent != null, "");
                Debug.Assert(baContent.Length <= (int)lRet, "ÿ�η��صİ��ߴ�[" + Convert.ToString(baContent.Length) + "]Ӧ��С��result.Value[" + Convert.ToString(lRet) + "]");

                nStart += baContent.Length;
                if (nStart >= (int)lRet)
                    break;	// ����


            } // end of for

            if (StringUtil.IsInList("data", strStyle) != true)
                return 0;


            // ת�����ַ���
            strResult = ByteArray.ToString(baTotal/*,
				Encoding.UTF8*/
                               );	// �������Զ�ʶ����뷽ʽ

            return 0;   // TODO: return lRet?
        }

        // �����Դ����װ�汾 -- �����ַ����汾��Cache�汾��
        // parameters:
        //      remote_timestamp    Զ��ʱ��������Ϊ null����ʾҪ�ӷ�����ʵ�ʻ�ȡʱ���
        // return:
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetRes(
            DigitalPlatform.Stop stop,
            CfgCache cache,
            string strPath,
            string strStyle,
            byte [] remote_timestamp,
            out string strResult,
            out string strMetaData,
            out byte[] baOutputTimeStamp,
            out string strOutputResPath,
            out string strError)
        {
            strError = "";
            strResult = "";
            strMetaData = "";
            baOutputTimeStamp = null;
            strOutputResPath = "";

            byte[] cached_timestamp = null;
            string strTimeStamp;
            string strLocalName;
            long lRet = 0;

            string strFullPath = this.Url + "?" + strPath;

            if (StringUtil.IsInList("forceget", strStyle) == true)
            {
                // ǿ�ƻ�ȡ
                StringUtil.RemoveFromInList("forceget",
                    true,
                    ref strStyle);
                goto GETDATA;
            }

            // ��cache�еõ�timestamp
            int nRet = cache.FindLocalFile(strFullPath,
                out strLocalName,
                out strTimeStamp);
            if (nRet == 1)
            {
                Debug.Assert(strLocalName != "", "FindLocalFile()���ص�strLocalNameΪ��");

                if (strTimeStamp == "")
                    goto GETDATA;	// ʱ�������, �Ǿ�ֻ�����»�ȡ������������

                Debug.Assert(strTimeStamp != "", "FindLocalFile()��õ�strTimeStampΪ��");
                cached_timestamp = ByteArray.GetTimeStampByteArray(strTimeStamp);
                // bExistInCache = true;
            }
            else
                goto GETDATA;

            if (remote_timestamp == null)
            {
                // ̽��ʱ�����ϵ
                string strNewStyle = strStyle;

                StringUtil.RemoveFromInList("content,data,metadata",    // 2012/12/31 BUG ��ǰ�����˼���content
                    true,
                    ref strNewStyle);	// ��Ҫ�������metadata

                lRet = GetRes(stop,
                    strPath,
                    strNewStyle,
                    out strResult,
                    out strMetaData,
                    out baOutputTimeStamp,
                    out strOutputResPath,
                    out strError);
                if (lRet == -1)
                    return -1;
            }
            else
                baOutputTimeStamp = remote_timestamp;

            // ���֤��timestampû�б仯, ���Ǳ��β�δ��������,���cache��ȡԭ��������

            if (ByteArray.Compare(baOutputTimeStamp, cached_timestamp) == 0)	// ʱ������
            {
                Debug.Assert(strLocalName != "", "strLocalName��ӦΪ��");

                StreamReader sr = null;

                try
                {
                    sr = new StreamReader(strLocalName, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }
                strResult = sr.ReadToEnd();
                sr.Close();

                return 0;	// ���޴�����̬����
            }

        GETDATA:

            // ������ʽ��ȡ����
            lRet = GetRes(
                stop,
                strPath,
                strStyle,
                out strResult,
                out strMetaData,
                out baOutputTimeStamp,
                out strOutputResPath,
                out strError);
            if (lRet == -1)
                return -1;

            // ��Ϊʱ�����ƥ����»��������
            // ���浽cache
            cache.PrepareLocalFile(strFullPath, out strLocalName);
            Debug.Assert(strLocalName != "", "PrepareLocalFile()���ص�strLocalNameΪ��");

            // д���ļ�,�Ա��Ժ��cache��ȡ
            StreamWriter sw = new StreamWriter(strLocalName,
                false,	// append
                System.Text.Encoding.UTF8);
            sw.Write(strResult);
            sw.Close();
            sw = null;

            Debug.Assert(baOutputTimeStamp != null, "�²�GetRes()���ص�baOutputTimeStampΪ��");
            nRet = cache.SetTimeStamp(strFullPath,
                ByteArray.GetHexTimeStampString(baOutputTimeStamp),
                out strError);
            if (nRet == -1)
                return -1;

            return lRet;
        }

        // �����Դ����װ�汾 -- ���ر���ӳ���ļ���Cache�汾��
        // return:
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetResLocalFile(
            DigitalPlatform.Stop stop,
            CfgCache cache,
            string strPath,
            string strStyle,
            out string strOutputFilename,
            out string strMetaData,
            out byte[] baOutputTimeStamp,
            out string strOutputResPath,
            out string strError)
        {
            strOutputFilename = "";

            byte[] cached_timestamp = null;
            string strTimeStamp;
            string strLocalName;
            string strResult = "";

            string strFullPath = this.Url + "?" + strPath;

            if (StringUtil.IsInList("forceget", strStyle) == true)
            {
                // ǿ�ƻ�ȡ
                StringUtil.RemoveFromInList("forceget",
                    true,
                    ref strStyle);
                goto GETDATA;
            }


            // ��cache�еõ�timestamp
            int nRet = cache.FindLocalFile(strFullPath,
                out strLocalName,
                out strTimeStamp);
            if (nRet == 1)
            {
                Debug.Assert(strLocalName != "", "FindLocalFile()���ص�strLocalNameΪ��");

                if (strTimeStamp == "")
                    goto GETDATA;	// ʱ�������, �Ǿ�ֻ�����»�ȡ������������

                Debug.Assert(strTimeStamp != "", "FindLocalFile()��õ�strTimeStampΪ��");
                cached_timestamp = ByteArray.GetTimeStampByteArray(strTimeStamp);
                // bExistInCache = true;
            }
            else
                goto GETDATA;

            // ̽��ʱ�����ϵ
            string strNewStyle = strStyle;

            /*
            StringUtil.RemoveFromInList("data",
                true,
                ref strNewStyle);	// ��Ҫ������
             * */
            StringUtil.RemoveFromInList("content,data,metadata",    // 2012/12/31 BUG ��ǰ�����˼���content
    true,
    ref strNewStyle);	// ��Ҫ�������metadata

            long lRet = GetRes(stop,
                strPath,
                strNewStyle,
                out strResult,
                out strMetaData,
                out baOutputTimeStamp,
                out strOutputResPath,
                out strError);
            if (lRet == -1)
                return -1;

            // ���֤��timestampû�б仯, ���Ǳ��β�δ��������,���cache��ȡԭ��������

            if (ByteArray.Compare(baOutputTimeStamp, cached_timestamp) == 0)	// ʱ������
            {
                Debug.Assert(strLocalName != "", "strLocalName��ӦΪ��");

                strOutputFilename = strLocalName;
                return 0;	// ���޴�����̬����
            }

        GETDATA:

            // ������ʽ��ȡ����
            lRet = GetRes(
                stop,
                strPath,
                strStyle,
                out strResult,
                out strMetaData,
                out baOutputTimeStamp,
                out strOutputResPath,
                out strError);
            if (lRet == -1)
                return -1;

            // ��Ϊʱ�����ƥ����»��������
            // ���浽cache
            cache.PrepareLocalFile(strFullPath, out strLocalName);
            Debug.Assert(strLocalName != "", "PrepareLocalFile()���ص�strLocalNameΪ��");

            // д���ļ�,�Ա��Ժ��cache��ȡ
            StreamWriter sw = new StreamWriter(strLocalName,
                false,	// append
                System.Text.Encoding.UTF8);
            sw.Write(strResult);
            sw.Close();
            sw = null;

            Debug.Assert(baOutputTimeStamp != null, "�²�GetRes()���ص�baOutputTimeStampΪ��");
            nRet = cache.SetTimeStamp(strFullPath,
                ByteArray.GetHexTimeStampString(baOutputTimeStamp),
                out strError);
            if (nRet == -1)
                return -1;

            strOutputFilename = strLocalName;

            return lRet;
        }

        // �����Դ����װ�汾 -- д���ļ��İ汾���ر������ڻ����Դ��Ҳ�����ڻ������¼�塣
        // parameters:
        //		fileTarget	�ļ���ע���ڵ��ú���ǰ�ʵ������ļ�ָ��λ�á�����ֻ���ڵ�ǰλ�ÿ�ʼ���д��д��ǰ���������ı��ļ�ָ�롣
        //		strStyleParam	һ������Ϊ"content,data,metadata,timestamp,outputpath";
        //		input_timestamp	��!=null���򱾺�����ѵ�һ�����ص�timestamp�ͱ��������ݱȽϣ��������ȣ��򱨴�
        // return:
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetRes(
            DigitalPlatform.Stop stop,
            string strPath,
            Stream fileTarget,
            string strStyleParam,
            byte[] input_timestamp,
            out string strMetaData,
            out byte[] baOutputTimeStamp,
            out string strOutputPath,
            out string strError)
        {
            strError = "";
            baOutputTimeStamp = null;
            strMetaData = "";
            strOutputPath = "";

            string strStyle = strStyleParam;

            if (StringUtil.IsInList("attachment", strStyle) == true)
            {
                Debug.Assert(false, "attachment style��ʱ����ʹ��");
            }


            // ������
            if (StringUtil.IsInList("data", strStyle) == false)
            {
                if (fileTarget != null)
                {
                    strError = "strStyle��������������data������޷��������...";
                    return -1;
                }
            }

            if (StringUtil.IsInList("data", strStyle) == true)
            {
                if (fileTarget == null)
                {
                    strError = "strStyle������������data��񣬶�fileTargetΪnull�����˷�ͨѶ��Դ...";
                    return -1;
                }
            }

            bool bHasMetadataStyle = false;
            if (StringUtil.IsInList("metadata", strStyle) == true)
            {
                bHasMetadataStyle = true;
            }

            // string id = "";
            byte[] baContent = null;

            long lStart = 0;
            int nPerLength = -1;

            byte[] old_timestamp = null;
            byte[] timestamp = null;

            long lTotalLength = -1;

            for (; ; )
            {
                Application.DoEvents();	// ���ý������Ȩ

                if (stop != null && stop.State != 0)
                {
                    strError = "�û��ж�";
                    return -1;
                }

                // REDO:

                string strMessage = "";

                string strPercent = "";
                if (lTotalLength != -1)
                {
                    double ratio = (double)lStart / (double)lTotalLength;
                    strPercent = String.Format("{0,3:N}", ratio * (double)100) + "%";
                }

                if (stop != null)
                {
                    strMessage = "�������� " + Convert.ToString(lStart) + "-"
                        + (lTotalLength == -1 ? "?" : Convert.ToString(lTotalLength))
                        + " " + strPercent + " "
                        + strPath;
                    stop.SetMessage(strMessage);
                }

                long lRet = this.GetRes(stop,
                    strPath,
                    fileTarget == null ? 0 : lStart,
                    fileTarget == null ? 0 : nPerLength,
                    strStyle,
                    out baContent,
                    // out id,
                    out strMetaData,
                    out strOutputPath,
                    out timestamp,
                    out strError);
                if (lRet == -1)
                    return -1;


                if (bHasMetadataStyle == true)
                {
                    StringUtil.RemoveFromInList("metadata",
                        true,
                        ref strStyle);
                    bHasMetadataStyle = false;
                }

                lTotalLength = lRet;


                if (StringUtil.IsInList("timestamp", strStyle) == true)
                {
                    if (input_timestamp != null)
                    {
                        if (ByteArray.Compare(input_timestamp, timestamp) != 0)
                        {
                            strError = "���ع����з���ʱ�����input_timestamp�����е�ʱ�����һ�£�����ʧ�� ...";
                            return -1;
                        }
                    }
                    if (old_timestamp != null)
                    {
                        if (ByteArray.Compare(old_timestamp, timestamp) != 0)
                        {
                            strError = "���ع����з���ʱ����仯������ʧ�� ...";
                            return -1;
                        }
                    }
                }

                old_timestamp = timestamp;

                if (fileTarget == null)
                    break;

                // д���ļ�
                if (StringUtil.IsInList("attachment", strStyle) == true)
                {
                    Debug.Assert(false, "attachment style��ʱ����ʹ��");
                }
                else
                {
                    Debug.Assert(StringUtil.IsInList("content", strStyle) == true,
                        "����attachment��񣬾�Ӧ��content���");

                    Debug.Assert(baContent != null, "���ص�baContent����Ϊnull");
                    Debug.Assert(baContent.Length <= lRet, "ÿ�η��صİ��ߴ�[" + Convert.ToString(baContent.Length) + "]Ӧ��С��result.Value[" + Convert.ToString(lRet) + "]");

                    fileTarget.Write(baContent, 0, baContent.Length);
                    fileTarget.Flush(); // 2013/5/17
                    lStart += baContent.Length;

                    if (lRet > 0)
                    {
                        // 2012/8/26
                        Debug.Assert(baContent.Length > 0, "");
                    }
                }

                if (lStart >= lRet)
                    break;	// ����

            } // end of for

            baOutputTimeStamp = timestamp;

            return 0;
        }

        // �����Դ����װ�汾 -- д���ļ��İ汾���ر������ڻ����Դ��Ҳ�����ڻ������¼�塣
        // parameters:
        //		strOutputFileName	����ļ���������Ϊnull���������ǰ�ļ��Ѿ�����, �ᱻ���ǡ�
        // return:
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetRes(
            DigitalPlatform.Stop stop,
            string strPath,
            string strOutputFileName,
            out string strMetaData,
            out byte[] baOutputTimeStamp,
            out string strOutputPath,
            out string strError)
        {
            FileStream fileTarget = null;

            string strStyle = "content,data,metadata,timestamp,outputpath";

            if (String.IsNullOrEmpty(strOutputFileName) == false)
                fileTarget = File.Create(strOutputFileName);
            else
            {
                strStyle = "metadata,timestamp,outputpath";
            }

            try
            {
                return GetRes(
                    stop,
                    strPath,
                    fileTarget,
                    strStyle,
                    null,
                    out strMetaData,
                    out baOutputTimeStamp,
                    out strOutputPath,
                    out strError);
            }

            finally
            {
                if (fileTarget != null)
                    fileTarget.Close();
            }
        }

        // д����Դ
        public long WriteRes(
            DigitalPlatform.Stop stop,
            string strResPath,
            string strRanges,
            long lTotalLength,
            byte[] baContent,
            string strMetadata,
            string strStyle,
            byte[] baInputTimestamp,
            out string strOutputResPath,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            strError = "";
            strOutputResPath = "";
            baOutputTimestamp = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginWriteRes(
                    strResPath,
                    strRanges,
                    lTotalLength,
                    baContent,
                    strMetadata,
                    strStyle,
                    baInputTimestamp,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }


                LibraryServerResult result = this.ws.EndWriteRes(
                    out strOutputResPath,
                    out baOutputTimestamp,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }



        }

        // ����Xml��¼����װ�汾�����ڱ����ı����͵���Դ��
        public long WriteRes(
            DigitalPlatform.Stop stop,
            string strPath,
            string strXml,
            bool bInlucdePreamble,
            string strStyle,
            byte[] timestamp,
            out byte[] output_timestamp,
            out string strOutputPath,
            out string strError)
        {
            strError = "";
            strOutputPath = "";
            output_timestamp = null;

            int nChunkMaxLength = 4096;	// chunk

            int nStart = 0;

            byte[] baInputTimeStamp = null;

            byte[] baPreamble = Encoding.UTF8.GetPreamble();

            byte[] baTotal = Encoding.UTF8.GetBytes(strXml);

            if (bInlucdePreamble == true
                && baPreamble != null && baPreamble.Length > 0)
            {
                byte[] temp = null;
                temp = ByteArray.Add(temp, baPreamble);
                baTotal = ByteArray.Add(temp, baTotal);
            }

            int nTotalLength = baTotal.Length;

            if (timestamp != null)
            {
                baInputTimeStamp = ByteArray.Add(baInputTimeStamp, timestamp);
            }

            for (; ; )
            {
                Application.DoEvents();	// ���ý������Ȩ

                // Debug.Assert(false, "");

                // �г�chunk
                int nThisChunkSize = nChunkMaxLength;

                if (nThisChunkSize + nStart > nTotalLength)
                {
                    nThisChunkSize = nTotalLength - nStart;	// ���һ��
                    if (nThisChunkSize <= 0)
                        break;
                }

                byte[] baChunk = new byte[nThisChunkSize];
                Array.Copy(baTotal, nStart, baChunk, 0, baChunk.Length);

                string strMetadata = "";
                string strRange = Convert.ToString(nStart) + "-" + Convert.ToString(nStart + baChunk.Length - 1);
                long lRet = this.WriteRes(stop,
                    strPath,
                    strRange,
                    nTotalLength,
                    baChunk,
                    strMetadata,
                    strStyle,
                    baInputTimeStamp,
                    out strOutputPath,
                    out output_timestamp,
                    out strError);
                if (lRet == -1)
                    return -1;
                /*
            REDOSAVE:
                soapresult = this.ws.BeginWriteRes(strPath,
                    strRange,
                    //nStart,
                    nTotalLength,	// �����������ߴ磬���Ǳ���chunk�ĳߴ硣��Ϊ��������Ȼ���Դ�baChunk�п�����ߴ磬������ר����һ��������ʾ����ߴ���
                    baChunk,
                    null,	// attachmentid
                    strMetadata,
                    strStyle,
                    baInputTimeStamp,
                    null,
                    null);

                for (; ; )
                {
                    Idle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    if (soapresult.IsCompleted)
                        break;
                }

                Result result = this.ws.EndWriteRes(soapresult,
                    out strOutputPath,
                    out output_timestamp);

                this.ErrorInfo = result.ErrorString;	// �����Ƿ񷵻ش��󣬶���result��ErrorString�ŵ�Channel��
                 * */

                nStart += baChunk.Length;

                if (nStart >= nTotalLength)
                    break;

                Debug.Assert(strOutputPath != "", "outputpath����Ϊ��");

                strPath = strOutputPath;	// �����һ�ε�strPath�а���'?'id, ������outputpath������ȷ����
                baInputTimeStamp = output_timestamp;	//baOutputTimeStamp;

            } // end of for
            return 0;
        }

        // 2009/11/24
        static string BuildMetadata(string strMime,
            string strLocalPath)
        {
            // string strMetadata = "<file mimetype='" + strMime + "' localpath='" + strLocalPath + "'/>";
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<file />");
            DomUtil.SetAttr(dom.DocumentElement,
                "mimetype",
                strMime);
            DomUtil.SetAttr(dom.DocumentElement,
                "localpath",
                strLocalPath);
            return dom.DocumentElement.OuterXml;
        }

        // 2014/3/6
        // ������Դ��¼
        // parameters:
        //		strPath	��ʽ: ����/��¼��/object/����xpath
        public long SaveResObject(
            DigitalPlatform.Stop stop,
            string strPath,
            string strObjectFileName,  // �ò��������Ŷ������ݵ��ļ���
            string strLocalPath,       // �ò����������ļ���,��ʱ����strObjectFileName��ͬ
            string strMime,
            string strRange,
            byte[] timestamp,
            string strStyle,
            out byte[] output_timestamp,
            out string strError)
        {
            strError = "";
            output_timestamp = null;

            FileInfo fi = new FileInfo(strObjectFileName);
            if (fi.Exists == false)
            {
                strError = "�ļ� '" + strObjectFileName + "'������...";
                return -1;
            }

            byte[] baTotal = null;
            long lRet = RangeList.CopyFragment(
                strObjectFileName,
                strRange,
                out baTotal,
                out strError);
            if (lRet == -1)
                return -1;

            // 
            string strOutputResPath = "";

            string strMetadata = BuildMetadata(strMime, strLocalPath);
            // string strMetadata = "<file mimetype='" + strMime + "' localpath='" + strLocalPath + "'/>";

            // д����Դ
            lRet = WriteRes(
                stop,
                strPath,
                strRange,
                fi.Length,	// �����������ߴ磬���Ǳ���chunk�ĳߴ硣��Ϊ��������Ȼ���Դ�baChunk�п�����ߴ磬������ר����һ��������ʾ����ߴ���
                baTotal,
                strMetadata,
                strStyle,
                timestamp,
                out strOutputResPath,
                out output_timestamp,
                out strError);
            if (lRet == -1)
                return -1;

            return 0;
        }

        // ������Դ��¼
        // parameters:
        //		strPath	��ʽ: ����/��¼��/object/����xpath
        //		bTailHint	�Ƿ�Ϊ���һ��д�����������һ����ʾ�����������������ݴ˲���Ϊ���һ��д�������������ĳ�ʱʱ�䡣
        //					�ٶ���ʱ������Դ�ߴ�ܴ���Ȼÿ�ξֲ�д���ʱ���࣬�������һ��д����Ϊ������Ҫִ��������Դת��
        //					�Ĳ�����API�ŷ��أ����Կ��ܻ�ķ�����20���������ĳ�ʱ�䣬����WebService API��ʱʧ�ܡ�
        //					��������һ����ʾ����(������Ҳ������һ��Ҫ��ʲô����)����������߲�������ĺ��壬����ʹ��false��
        public long SaveResObject(
            DigitalPlatform.Stop stop,
            string strPath,
            string strObjectFileName,  // �ò��������Ŷ������ݵ��ļ���
            string strLocalPath,       // �ò����������ļ���,��ʱ����strObjectFileName��ͬ
            string strMime,
            string strRange,
            bool bTailHint,
            byte[] timestamp,
            out byte[] output_timestamp,
            out string strError)
        {
            strError = "";
            output_timestamp = null;

            FileInfo fi = new FileInfo(strObjectFileName);
            if (fi.Exists == false)
            {
                strError = "�ļ� '" + strObjectFileName + "'������...";
                return -1;
            }

            byte[] baTotal = null;
            long lRet = RangeList.CopyFragment(
                strObjectFileName,
                strRange,
                out baTotal,
                out strError);
            if (lRet == -1)
                return -1;

            // string strOutputPath = "";

            int nOldTimeout = -1;
            if (bTailHint == true)
            {
                /*
                nOldTimeout = this.Timeout;
                // TODO: ����ͨ���ļ��ߴ�������
                this.Timeout = 40 * 60 * 1000;  // 40����
                 * */
            }

            // 
            string strOutputResPath = "";

            string strMetadata = BuildMetadata(strMime, strLocalPath);
            // string strMetadata = "<file mimetype='" + strMime + "' localpath='" + strLocalPath + "'/>";

            // д����Դ
            lRet = WriteRes(
                stop,
                strPath,
                strRange,
                fi.Length,	// �����������ߴ磬���Ǳ���chunk�ĳߴ硣��Ϊ��������Ȼ���Դ�baChunk�п�����ߴ磬������ר����һ��������ʾ����ߴ���
                baTotal,
                strMetadata,
                "", // strStyle,
                timestamp,
                out strOutputResPath,
                out output_timestamp,
                out strError);
            if (lRet == -1)
                return -1;

            if (bTailHint == true)
            {
                /*
                this.Timeout = nOldTimeout;
                 * */
            }

            return 0;
        }

        /// *** ����ع���

        // �������Ϣ
        // return:
        //      -1  ����
        //      0   ����
        public long GetIssues(
            DigitalPlatform.Stop stop,
            string strBiblioRecPath,
                   long lStart,
                   long lCount,
                   string strStyle,
                   string strLang,
            out EntityInfo[] issueinfos,
            out string strError)
        {
            issueinfos = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetIssues(
                    strBiblioRecPath,
                    lStart,
                    lCount,
                    strStyle,
                    strLang,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetIssues(
                    out issueinfos,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ��������Ϣ
        // return:
        //      -1  ����
        //      0   ����
        public long SetIssues(
            DigitalPlatform.Stop stop,
            string strBiblioRecPath,
            EntityInfo[] issueinfos,
            out EntityInfo[] errorinfos,
            out string strError)
        {
            errorinfos = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetIssues(
                    strBiblioRecPath, 
                    issueinfos,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetIssues(
                    out errorinfos,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // *** ��API�Ѿ���ֹ ***
#if NO
        // (�ڿ�)�������ڲ���
        public long SearchIssueDup(
            DigitalPlatform.Stop stop,
            string strPublishTime,
            string strBiblioRecPath,
            int nMax,
            out string[] paths,
            out string strError)
        {
            paths = null;
            strError = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchIssueDup(
                    strPublishTime,
                    strBiblioRecPath,
                    nMax,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchIssueDup(
                    out paths,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }
#endif

        // ��װ��İ汾
        public long GetIssueInfo(
    DigitalPlatform.Stop stop,
    string strRefID,
    string strResultType,
    out string strResult,
    out string strIssueRecPath,
    out byte[] issue_timestamp,
    string strBiblioType,
    out string strBiblio,
    out string strOutputBiblioRecPath,
    out string strError)
        {
            return GetIssueInfo(
            stop,
            strRefID,
            "",
            strResultType,
            out strResult,
            out strIssueRecPath,
            out issue_timestamp,
            strBiblioType,
            out strBiblio,
            out strOutputBiblioRecPath,
            out strError);
        }

#if NEW_API
        // ���÷�
        // ���� GetItemInfo ��ʵ��
        public long GetIssueInfo(
    DigitalPlatform.Stop stop,
    string strRefID,
    string strItemXml,
    string strResultType,
    out string strResult,
    out string strIssueRecPath,
    out byte[] issue_timestamp,
    string strBiblioType,
    out string strBiblio,
    out string strOutputBiblioRecPath,
    out string strError)
        {
            return GetItemInfo(
                stop,
                "issue",
                strRefID,
                strItemXml,
                strResultType,
            out strResult,
            out strIssueRecPath,
            out issue_timestamp,
            strBiblioType,
            out strBiblio,
            out strOutputBiblioRecPath,
            out strError);
        }
#else
        // ���ⲻ����� API ��
        // ����ڼ�¼
        public long GetIssueInfo(
            DigitalPlatform.Stop stop,
            string strRefID,
            // string strBiblioRecPath,
            string strItemXml,
            string strResultType,
            out string strResult,
            out string strIssueRecPath,
            out byte[] issue_timestamp,
            string strBiblioType,
            out string strBiblio,
            out string strOutputBiblioRecPath,
            out string strError)
        {
            strResult = "";
            strBiblio = "";
            strOutputBiblioRecPath = "";
            strError = "";

            strIssueRecPath = "";
            issue_timestamp = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetIssueInfo(
                    strRefID,
                    // strBiblioRecPath,
                    strItemXml,
                    strResultType,
                    strBiblioType,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetIssueInfo(
                    out strResult,
                    out strIssueRecPath,
                    out issue_timestamp,
                    out strBiblio,
                    out strOutputBiblioRecPath,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }
#endif

        // 2009/2/2
        // ��������Ϣ
        // parameters:
        //      stop    ֹͣ����
        //      strIssueDbNames  �ڿ������б����԰����������������֮���ö���(���)�ָ���<ȫ��> <all>��ʾȫ���ڿ�
        //      strQueryWord    ������
        //      nPerMax һ���������е�����¼����-1��ʾ�����ơ�
        //      strFrom ����;��
        //      strMatchStyle   ƥ�䷽ʽ��ֵΪleft/right/exact/middle֮һ��
        //      strLang �������Դ��롣һ��Ϊ"zh"��
        //      strResultSetName    �����������ʹ��null����ָ�������ֵĽ�������������������ϲ�ͬĿ�ĵļ�����������಻��ͻ��
        // Ȩ��: 
        //      ��Ҫ searchissue Ȩ��
        // return:
        //      -1  error
        //      >=0 ���н����¼����
        // ע��
        //      ʵ�������ݸ�ʽ����ͳһ�ģ�����;���������Ϊ���������/���κ�/��¼��
        public long SearchIssue(
            DigitalPlatform.Stop stop,
            string strIssueDbNames,
            string strQueryWord,
            int nPerMax,
            string strFrom,
            string strMatchStyle,
            string strLang,
            string strResultSetName,
            string strSearchStyle,
            string strOutputStyle,
            out string strError)
        {
            strError = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchIssue(
                    strIssueDbNames,
                    strQueryWord,
                    nPerMax,
                    strFrom,
                    strMatchStyle,
                    strLang,
                    strResultSetName,
                    strSearchStyle,
                    strOutputStyle,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchIssue(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }

        //  *** ������ع���

        // ��ö�����Ϣ
        // return:
        //      -1  ����
        //      0   ����
        public long GetOrders(
            DigitalPlatform.Stop stop,
            string strBiblioRecPath,
                   long lStart,
                   long lCount,
                   string strStyle,
                   string strLang,
            out EntityInfo[] orderinfos,
            out string strError)
        {
            orderinfos = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetOrders(
                    strBiblioRecPath,
                    lStart,
                    lCount,
                    strStyle,
                    strLang,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetOrders(
                    out orderinfos,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }


        // ���ö�����Ϣ
        // return:
        //      -1  ����
        //      0   ����
        public long SetOrders(
            DigitalPlatform.Stop stop,
            string strBiblioRecPath,
            EntityInfo[] orderinfos,
            out EntityInfo[] errorinfos,
            out string strError)
        {
            errorinfos = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetOrders(
                    strBiblioRecPath,
                    orderinfos,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetOrders(
                    out errorinfos,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // *** ��API�Ѿ���ֹ ***
#if NO
        // (������)��Ų���
        public long SearchOrderDup(
            DigitalPlatform.Stop stop,
            string strIndex,
            string strBiblioRecPath,
            int nMax,
            out string[] paths,
            out string strError)
        {
            paths = null;
            strError = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchOrderDup(
                    strIndex,
                    strBiblioRecPath,
                    nMax,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchOrderDup(
                    out paths,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }
#endif

        // ��װ��İ汾
        public long GetOrderInfo(
    DigitalPlatform.Stop stop,
    string strRefID,
    string strResultType,
    out string strResult,
    out string strItemRecPath,
    out byte[] item_timestamp,
    string strBiblioType,
    out string strBiblio,
    out string strOutputBiblioRecPath,
    out string strError)
        {
            return GetOrderInfo(
            stop,
            strRefID,
            "",
            strResultType,
            out strResult,
            out strItemRecPath,
            out item_timestamp,
            strBiblioType,
            out strBiblio,
            out strOutputBiblioRecPath,
            out strError);
        }

#if NEW_API
        // ���÷�
        // ���� GetItemInfo ��ʵ��
        public long GetOrderInfo(
    DigitalPlatform.Stop stop,
    string strRefID,
    string strItemXml,
    string strResultType,
    out string strResult,
    out string strItemRecPath,
    out byte[] item_timestamp,
    string strBiblioType,
    out string strBiblio,
    out string strOutputBiblioRecPath,
    out string strError)
        {
            return GetItemInfo(
                stop,
                "order",
                strRefID,
                strItemXml,
                strResultType,
            out strResult,
            out strItemRecPath,
            out item_timestamp,
            strBiblioType,
            out strBiblio,
            out strOutputBiblioRecPath,
            out strError);
        }
#else
        // ���ⲻ����� API ��
        // ��ö�����¼
        // parameters:
        //      strIndex  ��š���������£�����ʹ��"@path:"�����Ķ�����¼·��(ֻ��Ҫ������id��������)��Ϊ������ڡ�
        //      strBiblioRecPath    ָ����Ŀ��¼·��
        //      strResultType   ָ����Ҫ��strResult�����з��ص����ݸ�ʽ��Ϊ"xml" "html"֮һ��
        //                      ���Ϊ�գ����ʾstrResult�����в������κ����ݡ������������Ϊʲôֵ��strItemRecPath�ж��ط��ز��¼·��(��������˵Ļ�)
        //      strItemRecPath  ���ز��¼·��������Ϊ���ż�����б��������·��
        //      strBiblioType   ָ����Ҫ��strBiblio�����з��ص����ݸ�ʽ��Ϊ"xml" "html"֮һ��
        //                      ���Ϊ�գ����ʾstrBiblio�����в������κ����ݡ�
        //      strOutputBiblioRecPath  �������Ŀ��¼·������strIndex�ĵ�һ�ַ�Ϊ'@'ʱ��strBiblioRecPath����Ϊ�գ��������غ�strOutputBiblioRecPath�л������������Ŀ��¼·��
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        //      >1  ���ж���1��
        public long GetOrderInfo(
            DigitalPlatform.Stop stop,
            string strRefID,
            // string strBiblioRecPath,
            string strItemXml,
            string strResultType,
            out string strResult,
            out string strItemRecPath,
            out byte[] item_timestamp,
            string strBiblioType,
            out string strBiblio,
            out string strOutputBiblioRecPath,
            out string strError)
        {
            strResult = "";
            strBiblio = "";
            strOutputBiblioRecPath = "";
            strError = "";

            strItemRecPath = "";
            item_timestamp = null;
        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetOrderInfo(
                    strRefID,
                    // strBiblioRecPath,
                    strItemXml,
                    strResultType,
                    strBiblioType,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetOrderInfo(
                    out strResult,
                    out strItemRecPath,
                    out item_timestamp,
                    out strBiblio,
                    out strOutputBiblioRecPath,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }
#endif


        // ����������Ϣ
        // parameters:
        //      stop    ֹͣ����
        //      strOrderDbNames  �����������б����԰����������������֮���ö���(���)�ָ���<ȫ��> <all>��ʾȫ��������(����ͼ����ڿ���)��<ȫ��ͼ��> <all book>Ϊȫ��ͼ�����͵Ķ����⣬<ȫ���ڿ�> <all series>Ϊȫ���ڿ����͵Ķ�����
        //      strQueryWord    ������
        //      nPerMax һ���������е�����¼����-1��ʾ�����ơ�
        //      strFrom ����;��
        //      strMatchStyle   ƥ�䷽ʽ��ֵΪleft/right/exact/middle֮һ��
        //      strLang �������Դ��롣һ��Ϊ"zh"��
        //      strResultSetName    �����������ʹ��null����ָ�������ֵĽ�������������������ϲ�ͬĿ�ĵļ�����������಻��ͻ��
        // Ȩ��: 
        //      ��Ҫ searchorder Ȩ��
        // return:
        //      -1  error
        //      >=0 ���н����¼����
        // ע��
        //      ʵ�������ݸ�ʽ����ͳһ�ģ�����;���������Ϊ���������/���κ�/��¼��
        public long SearchOrder(
            DigitalPlatform.Stop stop,
            string strOrderDbNames,
            string strQueryWord,
            int nPerMax,
            string strFrom,
            string strMatchStyle,
            string strLang,
            string strResultSetName,
            string strSearchStyle,
            string strOutputStyle,
            out string strError)
        {
            strError = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchOrder(
                    strOrderDbNames,
                    strQueryWord,
                    nPerMax,
                    strFrom,
                    strMatchStyle,
                    strLang,
                    strResultSetName,
                    strSearchStyle,
                    strOutputStyle,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchOrder(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }

        //  *** ��ע��ع���

        // �����ע��Ϣ
        // return:
        //      -1  ����
        //      0   ����
        public long GetComments(
            DigitalPlatform.Stop stop,
            string strBiblioRecPath,
                   long lStart,
                   long lCount,
                   string strStyle,
                   string strLang,
            out EntityInfo[] commentinfos,
            out string strError)
        {
            commentinfos = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetComments(
                    strBiblioRecPath,
                    lStart,
                    lCount,
                    strStyle,
                    strLang,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetComments(
                    out commentinfos,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ������ע��Ϣ
        // return:
        //      -1  ����
        //      0   ����
        public long SetComments(
            DigitalPlatform.Stop stop,
            string strBiblioRecPath,
            EntityInfo[] commentinfos,
            out EntityInfo[] errorinfos,
            out string strError)
        {
            errorinfos = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetComments(
                    strBiblioRecPath,
                    commentinfos,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetComments(
                    out errorinfos,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ��װ��İ汾
        public long GetCommentInfo(
    DigitalPlatform.Stop stop,
    string strRefID,
    string strResultType,
    out string strResult,
    out string strCommentRecPath,
    out byte[] comment_timestamp,
    string strBiblioType,
    out string strBiblio,
    out string strOutputBiblioRecPath,
    out string strError)
        {
            return  GetCommentInfo(
            stop,
            strRefID,
            "",
            strResultType,
            out strResult,
            out strCommentRecPath,
            out comment_timestamp,
            strBiblioType,
            out strBiblio,
            out strOutputBiblioRecPath,
            out strError);
        }

#if NEW_API
        // ���÷�
        // ���� GetItemInfo ��ʵ��
        public long GetCommentInfo(
    DigitalPlatform.Stop stop,
    string strRefID,
    string strItemXml,
    string strResultType,
    out string strResult,
    out string strCommentRecPath,
    out byte[] comment_timestamp,
    string strBiblioType,
    out string strBiblio,
    out string strOutputBiblioRecPath,
    out string strError)
        {
            return GetItemInfo(
                stop,
                "comment",
                strRefID,
                strItemXml,
                strResultType,
            out strResult,
            out strCommentRecPath,
            out comment_timestamp,
            strBiblioType,
            out strBiblio,
            out strOutputBiblioRecPath,
            out strError);
        }
#else
        // ���ⲻ����� API ��
        // �����ע��¼
        // parameters:
        //      strIndex  ��š���������£�����ʹ��"@path:"�����Ķ�����¼·��(ֻ��Ҫ������id��������)��Ϊ������ڡ�
        //      strBiblioRecPath    ָ����Ŀ��¼·��
        //      strResultType   ָ����Ҫ��strResult�����з��ص����ݸ�ʽ��Ϊ"xml" "html"֮һ��
        //                      ���Ϊ�գ����ʾstrResult�����в������κ����ݡ������������Ϊʲôֵ��strItemRecPath�ж��ط��ز��¼·��(��������˵Ļ�)
        //      strItemRecPath  ���ز��¼·��������Ϊ���ż�����б��������·��
        //      strBiblioType   ָ����Ҫ��strBiblio�����з��ص����ݸ�ʽ��Ϊ"xml" "html"֮һ��
        //                      ���Ϊ�գ����ʾstrBiblio�����в������κ����ݡ�
        //      strOutputBiblioRecPath  �������Ŀ��¼·������strIndex�ĵ�һ�ַ�Ϊ'@'ʱ��strBiblioRecPath����Ϊ�գ��������غ�strOutputBiblioRecPath�л������������Ŀ��¼·��
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        //      >1  ���ж���1��
        public long GetCommentInfo(
            DigitalPlatform.Stop stop,
            string strRefID,
            // string strBiblioRecPath,
            string strItemXml,
            string strResultType,
            out string strResult,
            out string strCommentRecPath,
            out byte[] comment_timestamp,
            string strBiblioType,
            out string strBiblio,
            out string strOutputBiblioRecPath,
            out string strError)
        {
            strResult = "";
            strBiblio = "";
            strOutputBiblioRecPath = "";
            strError = "";

            strCommentRecPath = "";
            comment_timestamp = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetCommentInfo(strRefID,
                    // strBiblioRecPath,
                    strItemXml,
                    strResultType,
                    strBiblioType,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetCommentInfo(
                    out strResult,
                    out strCommentRecPath,
                    out comment_timestamp,
                    out strBiblio,
                    out strOutputBiblioRecPath,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }
#endif

        // *** ��API�Ѿ���ֹ ***
#if NO
        // (��ע��)��Ų���
        public long SearchCommentDup(
            DigitalPlatform.Stop stop,
            string strIndex,
            string strBiblioRecPath,
            int nMax,
            out string[] paths,
            out string strError)
        {
            paths = null;
            strError = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchCommentDup(strIndex,
                    strBiblioRecPath,
                    nMax,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchCommentDup(
                    out paths,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }
#endif

        // 2011/1/21
        // ԤԼ
        // parameters:
        //      strItemBarcodeList  ��������б����ż��
        // Ȩ�ޣ���Ҫ��reservationȨ��
        public long Reservation(
            DigitalPlatform.Stop stop,
            string strFunction,
            string strReaderBarcode,
            string strItemBarcodeList,
            out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginReservation(
                    strFunction,
                    strReaderBarcode,
                    strItemBarcodeList,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndReservation(
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ������ע��Ϣ
        // parameters:
        //      stop    ֹͣ����
        //      strCommentDbName  ��ע�������б����԰����������������֮���ö���(���)�ָ���<ȫ��> <all>��ʾȫ����ע��(����ͼ����ڿ���)
        //      strQueryWord    ������
        //      nPerMax һ���������е�����¼����-1��ʾ�����ơ�
        //      strFrom ����;��
        //      strMatchStyle   ƥ�䷽ʽ��ֵΪleft/right/exact/middle֮һ��
        //      strLang �������Դ��롣һ��Ϊ"zh"��
        //      strResultSetName    �����������ʹ��null����ָ�������ֵĽ�������������������ϲ�ͬĿ�ĵļ�����������಻��ͻ��
        // Ȩ��: 
        //      ��Ҫ searchorder Ȩ��
        // return:
        //      -1  error
        //      >=0 ���н����¼����
        public long SearchComment(
            DigitalPlatform.Stop stop,
            string strCommentDbName,
            string strQueryWord,
            int nPerMax,
            string strFrom,
            string strMatchStyle,
            string strLang,
            string strResultSetName,
            string strSearchStyle,
            string strOutputStyle,
            out string strError)
        {
            strError = "";

        REDO:
            this.BeginSearch();
            try
            {
                IAsyncResult soapresult = this.ws.BeginSearchComment(
                    strCommentDbName,
                    strQueryWord,
                    nPerMax,
                    strFrom,
                    strMatchStyle,
                    strLang,
                    strResultSetName,
                    strSearchStyle,
                    strOutputStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSearchComment(soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                this.EndSearch();
            }
        }


        public long GetMessage(
            string[] message_ids,
            MessageLevel messagelevel,
            out MessageData[] messages,
            out string strError)
        {
            strError = "";
            messages = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetMessage(
                    message_ids,
                    messagelevel,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetMessage(
                    out messages,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        public long ListMessage(
            string strStyle,
            string strResultsetName,
            string strBoxType,
            MessageLevel messagelevel,
            int nStart,
            int nCount,
            out int nTotalCount,
            out MessageData[] messages,
            out string strError)
        {
            strError = "";
            messages = null;
            nTotalCount = 0;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginListMessage(
                    strStyle,
            strResultsetName,
            strBoxType,
            messagelevel,
            nStart,
            nCount,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndListMessage(
                    out nTotalCount,
                    out messages,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // return:
        //      -1  ����
        //      >=0 δ��������Ϣ����
        public int GetUntouchedMessageCount(string strBoxType)
        {
            string strError = "";
            int nTotalCount = 0;
            MessageData[] messages = null;
            long lRet = ListMessage(
                "search,untouched",
                "message_untouched",
                strBoxType,
                MessageLevel.ID,
                0,
                0,
                out nTotalCount,
                out messages,
                out strError);
            if (lRet == -1)
                return -1;
            return nTotalCount;
        }


        public long SetMessage(string strAction,
            string strStyle,
            MessageData[] messages,
            out MessageData[] output_messages,
    out string strError)
        {
            strError = "";
            output_messages = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginSetMessage(
                    strAction,
                    strStyle,
                    messages,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndSetMessage(
                    out output_messages,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }


        public long GetStatisInfo(string strDateRangeString,
            string strStyle,
            out RangeStatisInfo info,
            out string strXml,
            out string strError)
        {
            strError = "";
            info = null;
            strXml = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetStatisInfo(
                    strDateRangeString,
                    strStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetStatisInfo(
                    out info,
                    out strXml,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        public long ExistStatisInfo(string strDateRangeString,
            out DateExist [] dates,
            out string strError)
        {
            strError = "";
            dates = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginExistStatisInfo(
                    strDateRangeString,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    if (soapresult.IsCompleted)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndExistStatisInfo(
                    out dates,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        public void DoStop()
        {
            IAsyncResult result = this.ws.BeginStop(
                null,
                null);
        }

        // �쳣:���ܻ��׳��쳣
        public void Abort()
        {
            if (m_nInSearching > 0)
            {
                if (this.m_ws != null)
                {
                    if (this.m_bStoped == false)
                    {
                        this.DoStop();
                        // TODO: ���ʱ��̫���˲����أ������Abort()?
                        this.m_bStoped = true;
                        return;
                    }

                    // ���򣬾��ߵ�Abort()����
                }
            }

            if (this.m_ws != null)
            {
#if NO
                // TODO: Search()Ҫ��������
                // this.m_ws.Abort();
                this.m_ws.Close();  // test
                this.m_ws = null;
#endif
                this.Close();
            }
#if NO
            // ws.Abort();
            if (String.IsNullOrEmpty(this.Url) == false)
            {
                ws.CancelAsync(null);

                /*
                // 2011/1/7 add
                this.m_ws = null;
                 * */
            }
#endif
        }

        public void Close()
        {
            if (this.m_ws != null)
            {
                // TODO: Search()Ҫ��������
                try
                {
                    if (this.m_ws.State != CommunicationState.Faulted)
                        this.m_ws.Close();
                }
                catch
                {
                    this.m_ws.Abort();
                }
                this.m_ws = null;
            }
        }

        // �ṩ��
        //             stop.OnStop += new StopEventHandler(this.DoStop);
        public void DoStop(object sender, StopEventArgs e)
        {
             this.Abort();
        }
    }

    /// <summary>
    /// ��¼�¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void LoginEventHandle(object sender,
        LoginEventArgs e);

    /// <summary>
    /// LoginEventHandle �¼��Ĳ���
    /// </summary>
    public class LoginEventArgs : EventArgs
    {
        /// <summary>
        /// ͨѶͨ��
        /// </summary>
        public LibraryChannel Channel = null;

        /// <summary>
        /// �Ƿ�Ҫ������¼
        /// </summary>
        public bool Cancel = false;

        /// <summary>
        /// ������Ϣ
        /// </summary>
        public string ErrorInfo = "";
    }

    public class MyValidator : X509CertificateValidator
    {
        public override void Validate(X509Certificate2 certificate)
        {
            return;
        }
    }

    class CustomIdentityVerifier : IdentityVerifier
    {
        public override bool CheckAccess(EndpointIdentity identity, AuthorizationContext authContext)
        {
            foreach (ClaimSet claimset in authContext.ClaimSets)
            {
                if (claimset.ContainsClaim(identity.IdentityClaim))
                    return true;

                // string expectedSpn = null;
                if (ClaimTypes.Dns.Equals(identity.IdentityClaim.ClaimType))
                {
                    string strHost = (string)identity.IdentityClaim.Resource;

                    /*
                    expectedSpn = string.Format(CultureInfo.InvariantCulture, "host/{0}",
                        strHost);
                     * */
                    Claim claim = CheckDnsEquivalence(claimset, strHost);
                    if (claim != null)
                    {
                        return true;
                    }
                }
            }

            //Stopwatch stopwath = new Stopwatch();
            //stopwath.Start();

            bool bRet = IdentityVerifier.CreateDefault().CheckAccess(identity, authContext);

            //stopwath.Stop();
            //Debug.WriteLine("CheckAccess " + stopwath.Elapsed.ToString());

            if (bRet == true)
                return true;

            return false;
        }

        Claim CheckDnsEquivalence(ClaimSet claimSet, string expedtedDns)
        {
            IEnumerable<Claim> claims = claimSet.FindClaims(ClaimTypes.Dns, Rights.PossessProperty);
            foreach (Claim claim in claims)
            {
                // ��������"localhost"
                if (expedtedDns.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    return claim;
                }

                string strCurrent = (string)claim.Resource;

                // ��������"DigitalPlatform"����������ַ���ƥ��
                if (strCurrent.Equals("DigitalPlatform", StringComparison.OrdinalIgnoreCase))
                    return claim;

                if (expedtedDns.Equals(strCurrent, StringComparison.OrdinalIgnoreCase))
                {
                    return claim;
                }
            }
            return null;
        }

        public override bool TryGetIdentity(EndpointAddress reference, out EndpointIdentity identity)
        {
            return IdentityVerifier.CreateDefault().TryGetIdentity(reference, out identity);
        }
    }

    public class OrgEndpointIdentity : EndpointIdentity
    {
        private string orgClaim;
        public OrgEndpointIdentity(string orgName)
        {
            orgClaim = orgName;
        }

        public string OrganizationClaim
        {
            get { return orgClaim; }
            set { orgClaim = value; }
        }
    }

}
