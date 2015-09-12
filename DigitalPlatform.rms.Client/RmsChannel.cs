using System;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using System.Text;
using System.IO;
using System.Xml;
using System.ServiceModel;


using System.ServiceModel.Security;
using System.ServiceModel.Channels;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Selectors;
using System.IdentityModel.Policy;
using System.IdentityModel.Claims;
using System.ServiceModel.Security.Tokens;

using DigitalPlatform;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Range;
using DigitalPlatform.Xml;

using DigitalPlatform.rms.Client.rmsws_localhost;

namespace DigitalPlatform.rms.Client
{
    public class dp2opacRecord
    {
        public Record Record;
        public long IndexOfResult;
    }

    public enum LoginStyle
    {
        None = 0x0,	// ʲô���Ҳû��
        AutoLogin = 0x1,	// ��һ�β����ֶԻ��������ȱʡ�ʻ������¼һ�Ρ�
        FillDefaultInfo = 0x2,	// ���ֵ�¼�Ի���ʱ���Ƿ����ȱʡ�ʻ�����������Ϣ��
    }

    public enum ChannelErrorCode
    {
        // ���´�����Ϊǰ�˶���
        None = 0,
        RequestTimeOut = 1,	// ����ʱ
        RequestCanceled = 2,	// �����ж�
        RequestError = 3,	// ����ͨѶ����
        RequestCanceledByEventClose = 4,	// �����жϣ���ΪeventClose����
        QuotaExceeded = 5,  // ����ͨѶ���ߴ����
        OtherError = 6,	// δ����Ĵ�����


        // ���´�����ͷ�������Ӧ
        TimestampMismatch = 10,	// ʱ�����ƥ��
        NotFound = 11,	// δ����
        NotLogin = 12,	// ��δ��¼
        EmptyRecord = 13,	// �ռ�¼
        //NoHasManagement = 14,	// ���߱�����ԱȨ��
        NotHasEnoughRights = 15, // û���㹻��Ȩ��
        PartNotFound = 16,	// ��¼�ֲ�û���ҵ�
        AlreadyExist = 17,	// Ҫ������ͬ�������Ѿ�����
        AlreadyExistOtherType = 18,	// Ҫ������ͬ�������Ѿ����ڣ�����Ϊ��ͬ����

        ApplicationStartError = 24,	//Application��������

        NotFoundSubRes = 25,    // �����¼���Դ��¼������

        // LoginFail = 26, // dp2library��dp2Kernel��¼ʧ�ܡ�����ζ��library.xml�еĴ����ʻ�������

    }


    // һ��ͨѶͨ��
    public class RmsChannel
    {
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
        


        public const int MAX_RECEIVE_MESSAGE_SIZE = 2 * 1024 * 1024;
        public RmsChannelCollection Container = null;
        public string Url = "";

        KernelServiceClient m_ws = null;	// ӵ��

        bool m_bStoped = false; // �����Ƿ��жϹ�һ��
        int m_nInSearching = 0;
        int m_nRedoCount = 0;   // MessageSecurityException�Ժ����ԵĴ���
        // public AutoResetEvent eventFinish = new AutoResetEvent(false);

        public ChannelErrorCode ErrorCode = ChannelErrorCode.None;

        public ErrorCodeValue OriginErrorCode = ErrorCodeValue.NoError;

        public string ErrorInfo = "";

        // IAsyncResult soapresult = null;

        public event IdleEventHandler Idle = null;
        public object Param = null;

        // [NonSerialized]
        public CookieContainer Cookies = new System.Net.CookieContainer();

        static void SetTimeout(System.ServiceModel.Channels.Binding binding)
        {
            binding.SendTimeout = new TimeSpan(0, 20, 0);
            binding.ReceiveTimeout = new TimeSpan(0, 20, 0);
            binding.CloseTimeout = new TimeSpan(0, 20, 0);
            binding.OpenTimeout = new TimeSpan(0, 20, 0);
        }

        // np0: namedpipe
        public static System.ServiceModel.Channels.Binding CreateNp0Binding()
        {
            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            binding.Security.Mode = NetNamedPipeSecurityMode.None;

            binding.MaxReceivedMessageSize = MAX_RECEIVE_MESSAGE_SIZE;
            // binding.MessageEncoding = WSMessageEncoding.Mtom;
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = 1024 * 1024;
            quotas.MaxStringContentLength = 1024 * 1024;
            binding.ReaderQuotas = quotas;

            SetTimeout(binding);
            //binding.ReliableSession.Enabled = true;

            return binding;
        }

        // nt0: net.tcp
        public static System.ServiceModel.Channels.Binding CreateNt0Binding()
        {
            NetTcpBinding binding = new NetTcpBinding();
            binding.Security.Mode = SecurityMode.None;

            binding.MaxReceivedMessageSize = MAX_RECEIVE_MESSAGE_SIZE;
            // binding.MessageEncoding = WSMessageEncoding.Mtom;
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = 1024 * 1024;
            quotas.MaxStringContentLength = 1024 * 1024;
            binding.ReaderQuotas = quotas;
            SetTimeout(binding);
            binding.ReliableSession.InactivityTimeout = new TimeSpan(0, 20, 0);

            //binding.ReliableSession.Enabled = true;

            return binding;
        }

        // ws0:windows
        public static System.ServiceModel.Channels.Binding CreateWs0Binding()
        {
            WSHttpBinding binding = new WSHttpBinding();
            binding.Security.Mode = SecurityMode.Message;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.Windows;

            binding.MaxReceivedMessageSize = MAX_RECEIVE_MESSAGE_SIZE;
            binding.MessageEncoding = WSMessageEncoding.Mtom;
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = 1024 * 1024;
            quotas.MaxStringContentLength = 1024 * 1024;
            binding.ReaderQuotas = quotas;
            SetTimeout(binding);

            //binding.ReliableSession.Enabled = true;
            binding.ReliableSession.InactivityTimeout = new TimeSpan(0, 20, 0);

            return binding;
        }

        // ws1:anonymouse
        public static System.ServiceModel.Channels.Binding CreateWs1Binding()
        {
            WSHttpBinding binding = new WSHttpBinding();
            binding.Security.Mode = SecurityMode.Message;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.None;
            binding.MaxReceivedMessageSize = MAX_RECEIVE_MESSAGE_SIZE;
            binding.MessageEncoding = WSMessageEncoding.Mtom;
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = 1024 * 1024;
            quotas.MaxStringContentLength = 1024 * 1024;
            binding.ReaderQuotas = quotas;
            SetTimeout(binding);

            //binding.ReliableSession.Enabled = true;
            binding.ReliableSession.InactivityTimeout = new TimeSpan(0, 20, 0);

            // return binding;

#if NO
            //Clients are anonymous to the service.
            binding.Security.Message.ClientCredentialType = MessageCredentialType.None;
            //Secure conversation is turned off for simplification. If secure conversation is turned on, then 
            //you also need to set the IdentityVerifier on the secureconversation bootstrap binding.
            // binding.Security.Message.EstablishSecurityContext = false;

            // Get the SecurityBindingElement and cast to a SymmetricSecurityBindingElement to set the IdentityVerifier.
            BindingElementCollection outputBec = binding.CreateBindingElements();
            SymmetricSecurityBindingElement ssbe = (SymmetricSecurityBindingElement)outputBec.Find<SecurityBindingElement>();

            //Set the Custom IdentityVerifier.
            ssbe.LocalClientSettings.IdentityVerifier = new CustomIdentityVerifier();

            return new CustomBinding(outputBec);
#endif
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

        // ws2:username
        public static System.ServiceModel.Channels.Binding CreateWs2Binding()
        {
            WSHttpBinding binding = new WSHttpBinding();
            binding.Security.Mode = SecurityMode.Message;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
            // binding.Security.Message.NegotiateServiceCredential = false;
            // binding.Security.Message.EstablishSecurityContext = false;

            binding.MaxReceivedMessageSize = MAX_RECEIVE_MESSAGE_SIZE;
            binding.MessageEncoding = WSMessageEncoding.Mtom;
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = 1024 * 1024;
            quotas.MaxStringContentLength = 1024 * 1024;
            binding.ReaderQuotas = quotas;
            SetTimeout(binding);

            //binding.ReliableSession.Enabled = true;
            binding.ReliableSession.InactivityTimeout = new TimeSpan(0, 20, 0);

            return binding;
        }

        public KernelServiceClient ws
        {
            get
            {
                if (m_ws == null)
                {
                    bool bWs0 = false;
                    Uri uri = new Uri(this.Url);

                    if (uri.Scheme.ToLower() == "net.pipe")
                    {
                        EndpointAddress address = new EndpointAddress(this.Url);

                        this.m_ws = new KernelServiceClient(CreateNp0Binding(), address);
                    }
                    else if (uri.Scheme.ToLower() == "net.tcp")
                    {
                        EndpointAddress address = new EndpointAddress(this.Url);

                        this.m_ws = new KernelServiceClient(CreateNt0Binding(), address);
                    }
                    else
                    {
                        if (uri.AbsolutePath.ToLower().IndexOf("/ws0") != -1)
                            bWs0 = true;

                        if (bWs0 == false)
                        {
                            // ws1 
                            /*
                            EndpointIdentity identity = EndpointIdentity.CreateDnsIdentity("DigitalPlatform");
                            EndpointAddress address = new EndpointAddress(new Uri(this.Url),
                                identity, new AddressHeaderCollection());
                             * */
                            EndpointAddress address = null;
                            address = new EndpointAddress(this.Url);

                            this.m_ws = new KernelServiceClient(CreateWs1Binding(), address);

                            // this.m_ws.ClientCredentials.ClientCertificate.SetCertificate(
                            this.m_ws.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.Custom;
                            this.m_ws.ClientCredentials.ServiceCertificate.Authentication.CustomCertificateValidator =
            new MyValidator();
                        }
                        else
                        {
                            // ws0
                            EndpointAddress address = new EndpointAddress(this.Url);

                            this.m_ws = new KernelServiceClient(CreateWs0Binding(), address);
                            this.m_ws.ClientCredentials.UserName.UserName = "test";
                            this.m_ws.ClientCredentials.UserName.Password = "";
                        }
                    }

                }
                if (String.IsNullOrEmpty(this.Url) == true)
                {
                    throw(new Exception("Urlֵ��ʱӦ�������ڿ�"));
                }
                Debug.Assert(this.Url != "", "Urlֵ��ʱӦ�������ڿ�");

                // m_ws.Url = this.Url;
                // m_ws.CookieContainer = this.Cookies;

                // this.m_ws.InnerChannel.OperationTimeout = TimeSpan.FromMinutes(20);
                this.m_ws.InnerChannel.OperationTimeout = this.OperationTimeout;

                return m_ws;
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
                ws.Abort();

            // ws.servicepoint.CloseConnectionGroup(ws.ConnectionGroupName);
            /*
            if (soapresult != null)
                ((WebClientAsyncResult)soapresult).Abort();
            else
                ws.Abort();
             * */

            // 2011/1/7 add
            this.m_ws = null;
        }

#if NO
        public void Close()
        {
            if (this.m_ws != null)
            {
                this.m_ws.Close();
                this.m_ws = null;
            }
        }
#endif
        // 2015/5/4
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

        void ConvertErrorCode(Result result)
        {
            this.ClearRedoCount();

            this.OriginErrorCode = result.ErrorCode;

            if (result.ErrorCode == ErrorCodeValue.NoError)
            {
                this.ErrorCode = ChannelErrorCode.None; // 2008/7/29
            }
            else if (result.ErrorCode == ErrorCodeValue.NotFound)
            {
                this.ErrorCode = ChannelErrorCode.NotFound;
            }
            else if (result.ErrorCode == ErrorCodeValue.PartNotFound)
            {
                this.ErrorCode = ChannelErrorCode.PartNotFound;
            }
            else if (result.ErrorCode == ErrorCodeValue.EmptyContent)
            {
                this.ErrorCode = ChannelErrorCode.EmptyRecord;
            }
            else if (result.ErrorCode == ErrorCodeValue.TimestampMismatch)
            {
                this.ErrorCode = ChannelErrorCode.TimestampMismatch;
            }
            else if (result.ErrorCode == ErrorCodeValue.NotLogin)
            {
                this.ErrorCode = ChannelErrorCode.NotLogin;
            }
            /*
                        else if (result.ErrorCode == ErrorCodeValue.NoHasManagement)
                        {
                            this.ErrorCode = ChannelErrorCode.NoHasManagement;
                        }
            */
            else if (result.ErrorCode == ErrorCodeValue.NotHasEnoughRights)
            {
                this.ErrorCode = ChannelErrorCode.NotHasEnoughRights;
            }
            else if (result.ErrorCode == ErrorCodeValue.AlreadyExist)
            {
                this.ErrorCode = ChannelErrorCode.AlreadyExist;
            }
            else if (result.ErrorCode == ErrorCodeValue.AlreadyExistOtherType)
            {
                this.ErrorCode = ChannelErrorCode.AlreadyExistOtherType;
            }
            else if (result.ErrorCode == ErrorCodeValue.ApplicationStartError)
            {
                this.ErrorCode = ChannelErrorCode.ApplicationStartError;
            }
            else if (result.ErrorCode == ErrorCodeValue.NotFoundSubRes)
            {
                this.ErrorCode = ChannelErrorCode.NotFoundSubRes;
            }
            else if (result.ErrorCode == ErrorCodeValue.Canceled)
            {
                this.ErrorCode = ChannelErrorCode.RequestCanceled;
            }
            else
            {
                this.ErrorCode = ChannelErrorCode.OtherError;
            }
        }

        // return:
        //      0   �������践��-1
        //      1   ��Ҫ����API
        int ConvertWebError(Exception ex0,
            out string strError)
        {
            // ������������
            if (ex0 is System.ServiceModel.Security.MessageSecurityException)
            {
                System.ServiceModel.Security.MessageSecurityException ex = (System.ServiceModel.Security.MessageSecurityException)ex0;
                this.ErrorCode = ChannelErrorCode.RequestError;	// һ�����
                this.ErrorInfo = GetExceptionMessage(ex);
                this.m_ws = null;
                strError = this.ErrorInfo;
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
                this.ErrorCode = ChannelErrorCode.RequestError;	// һ�����
                this.ErrorInfo = GetExceptionMessage(ex);
                this.m_ws = null;
                strError = this.ErrorInfo;
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
                this.ErrorCode = ChannelErrorCode.RequestError;	// һ�����
                this.ErrorInfo = "������ " + this.Url + " û����Ӧ";
                this.m_ws = null;
                strError = this.ErrorInfo;
                return 0;
            }

            /*
            if (ex0 is CommunicationException)
            {
                CommunicationException ex = (CommunicationException)ex0;

            }
             * */

            if (ex0 is WebException)
            {
                WebException ex = (WebException)ex0;

                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    this.ErrorCode = ChannelErrorCode.RequestTimeOut;
                    this.ErrorInfo = "����ʱ";    // (��ǰ��ʱ����Ϊ" + Convert.ToString(this.Timeout) + ")";
                    strError = this.ErrorInfo;
                    return 0;
                }
                if (ex.Status == WebExceptionStatus.RequestCanceled)
                {
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    this.ErrorInfo = "�û��ж�";
                    strError = this.ErrorInfo;
                    return 0;
                }

                this.ErrorCode = ChannelErrorCode.RequestError;	// һ�����
                this.ErrorInfo = GetExceptionMessage(ex);
                strError = this.ErrorInfo;
                return 0;
            }

            // 2013/1/11
            if (ex0 is System.ServiceModel.CommunicationException
                && ex0.InnerException is System.ServiceModel.QuotaExceededException)
            {
                this.ErrorCode = ChannelErrorCode.QuotaExceeded;
                this.ErrorInfo = GetExceptionMessage(ex0);
                strError = this.ErrorInfo;
                if (this.m_nRedoCount == 0)
                {
                    this.m_nRedoCount++;
                    return 1;   // ����
                }
                return 0;
            }

            this.ErrorCode = ChannelErrorCode.RequestError;	// һ�����
            this.ErrorInfo = GetExceptionMessage(ex0);
            this.m_ws = null;   // ��֪�Ƿ���ȷ
            strError = this.ErrorInfo;
            return 0;
        }

        static string GetExceptionMessage(Exception ex)
        {
            string strResult = ex.GetType().ToString() + ":" + ex.Message;
            while(ex != null)
            {
                if (ex.InnerException != null)
                    strResult += "\r\n" + ex.InnerException.GetType().ToString() + ": " + ex.InnerException.Message;

                ex = ex.InnerException;
            }

            return strResult;
        }

        void DoIdle()
        {
            bool bDoEvents = true;
            // 2012/3/18
            // 2012/11/28
            if (this.Container != null
                && this.Container.GUI == false)
                bDoEvents = false;

            // System.Threading.Thread.Sleep(1);	// ����CPU��Դ���Ⱥķ�

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

            // System.Threading.Thread.Sleep(1);	// ����CPU��Դ���Ⱥķ�
        }

        // ��¼�������Ҫ�����ֶԻ���
        // parameters:
        //		strPath	��Դ·����������URL���֡�
        //		bAutoLogin	�Ƿ񲻳��ֶԻ������Զ���¼һ�Ρ�
        // return:
        //		-1	error
        //		0	login failed��������Ϣ��strError��
        //		1	login succeed
        public int UiLogin(
            string strPath,
            out string strError)
        {
            return UiLogin(null,
                strPath,
                LoginStyle.AutoLogin | LoginStyle.FillDefaultInfo,
                out strError);
        }

        // ��¼�������Ҫ�����ֶԻ���
        // parameters:
        //		strPath	��Դ·����������URL���֡�
        //		bAutoLogin	�Ƿ񲻳��ֶԻ������Զ���¼һ�Ρ�
        // return:
        //		-1	error
        //		0	login failed��������Ϣ��strError��
        //		1	login succeed
        public int UiLogin(
            string strComment,
            string strPath,
            LoginStyle loginStyle,
            out string strError)
        {
            strError = "";

            /*
            if (this.Container.AskAccountInfo == null)
            {
                strError = "AskAccountInfo�¼�����δ����";
                return -1;
            }
             */

            string strUserName;
            string strPassword;
            IWin32Window owner = null;

        REDOINPUT:

            // ���ȱʡ�ʻ���Ϣ
            // return:
            //		2	already login succeed
            //		1	dialog return OK
            //		0	dialog return Cancel
            //		-1	other error
            /*
            int nRet = this.Container.procAskAccountInfo(
                this.Container,
                strComment,
                this.Url,
                strPath,
                loginStyle,
                out owner,
                out strUserName,
                out strPassword);
             */
            AskAccountInfoEventArgs e = new AskAccountInfoEventArgs();
            e.Channels = this.Container;
            e.Comment = strComment;
            e.Url = this.Url;
            e.Path = strPath;
            e.LoginStyle = loginStyle;
            e.Channel = this;   // 2013/2/14

            this.Container.OnAskAccountInfo(this, e);

            owner = e.Owner;
            strUserName = e.UserName;
            strPassword = e.Password;

            if (e.Result == -1)
            {
                if (e.ErrorInfo == "")
                    strError = "procAskAccountInfo() error";
                else
                    strError = e.ErrorInfo;
                return -1;
            }


            if (e.Result == 2)
                return 1;

            if (e.Result == 1)
            {
                // ��¼
                // return:
                //		-1	����������Ϣ��strError��
                //		0	��¼ʧ�ܡ�������ϢҲ��strError
                //		1	��¼�ɹ�
                int nRet = this.Login(strUserName,
                    strPassword,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    if (this.Container.GUI == true)
                        MessageBox.Show(owner, strError);
                    else
                    {
                        return -1;
                    }
                    goto REDOINPUT;
                }
                // this.m_nRedoCount = 0;
                return 1;   // ��¼�ɹ�,��������API������
            }

            if (e.Result == 0)
            {
                strError = "������¼";
                return -1;
            }

            strError = "UiLogin() ��Ӧ���ߵ�����";
            return -1;
        }


        // ��¼(����)
        // return:
        //		-1	����������Ϣ��strError��
        //		0	��¼ʧ�ܡ�������ϢҲ��strError
        //		1	��¼�ɹ�
        public int LoginOld(string strUserName,
            string strPassword,
            out string strError)
        {
            strError = "";

            REDO:
            Result result = null;
            try
            {
                result = this.ws.Login(strUserName, strPassword);
            }
            catch (Exception ex)
            {
                /*
                this.ErrorCode = ChannelErrorCode.RequestError;	// һ�����
                this.ErrorInfo = ex.Message;
                strError = this.ErrorInfo;
                */
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }

            if (result.Value == -1)
            {
                ConvertErrorCode(result);
                strError = result.ErrorString;
                return -1;
            }

            this.ClearRedoCount();

            if (result.Value == 0)
            {
                strError = result.ErrorString;
                return 0;
            }

            return 1;
        }

        // ���dpKernel�汾��
        // return:
        //		-1	����������Ϣ��strError��
        //		0	�ɹ�
        public int GetVersion(out string strVersion,
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

                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                    /*
                    if (soapresult.IsCompleted)
                        break;
                     * */
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }

                Result result = this.ws.EndGetVersion(soapresult);

                if (result.Value == -1)
                {
                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                if (result.Value == 0)
                {
                    strVersion = result.ErrorString;
                    return 0;
                }

                this.ClearRedoCount();
                return 1;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ��¼
        // return:
        //		-1	����������Ϣ��strError��
        //		0	��¼ʧ�ܡ�������ϢҲ��strError
        //		1	��¼�ɹ�
        public int Login(string strUserName,
            string strPassword,
            out string strError)
        {
            strError = "";

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginLogin(strUserName,
                    strPassword,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }

                Result result = this.ws.EndLogin(soapresult);

                if (result.Value == -1)
                {
                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                if (result.Value == 0)
                {
                    strError = result.ErrorString;
                    return 0;
                }

                this.ClearRedoCount();
                return 1;
            }

            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �޸�����
        // ����strUserName�� strOldPassword����Ϊnull����������£�
        // ������ֱ��ȥ�޸����룬�����ʱchannelȷʵ�Ѿ���¼���������ò���Ҫ
        // ����ǰ�������������ʱchannel��δ��¼��������Login()�Ĺ��л���
        // ���ܻᵯ����¼�Ի���
        // parameters:
        //		bManager	�Ƿ��Թ���Ա��ݽ����޸ġ�����Ա��ʽ���ص㣬��
        //				1) �����ù���Ա�ʻ��ȵ�¼(����ʻ��ͼ������޸ĵ��ʻ�����û����ϵ)
        //				2) �������WebServiceAPI ChangeOtherPassword()
        // return:
        //		-1	����������Ϣ��strError��
        //		0	�ɹ���
        public int ChangePassword(
            string strUserName,
            string strOldPassword,
            string strNewPassword,
            bool bManager,
            out string strError)
        {
            strError = "";

            int nRet = 0;

            if (bManager == true)
            {
                if (strUserName == null)
                {
                    strError = "bManager����Ϊtrueʱ��strUserName��������Ϊnull...";
                    return -1;
                }
            }


            if (strUserName != null && strOldPassword != null
                && bManager == false)
            {
                // return:
                //		-1	����������Ϣ��strError��
                //		0	��¼ʧ�ܡ�������ϢҲ��strError
                //		1	��¼�ɹ�
                nRet = this.Login(strUserName,
                    strOldPassword,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    strError = "ԭ���벻��ȷ";
                    return -1;
                }
            }


            REDO:
            try
            {
                IAsyncResult soapresult = null;

                if (bManager == false)
                {
                    soapresult = this.ws.BeginChangePassword(
                        strNewPassword,
                        null,
                        null);
                }
                else
                {
                    soapresult = this.ws.BeginChangeOtherPassword(
                        strUserName,
                        strNewPassword,
                        null,
                        null);
                }

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = null;

                if (bManager == false)
                {
                    result = this.ws.EndChangePassword(soapresult);
                }
                else
                {
                    result = this.ws.EndChangeOtherPassword(soapresult);
                }

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        nRet = this.UiLogin(
                            strUserName == null ?
                            ("�����޸Ĳ������ӳ١������þ����� �� ���޸�������ʻ�����һ�ε�¼���Ա��޸���������Զ���������...")
                            : ("�����þ�������ʻ� '" + strUserName + "' ����һ�ε�¼���Ա��޸��������˳������...")
                            ,
                            "",
                            LoginStyle.None,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDO;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;

                    if (result.ErrorCode == ErrorCodeValue.NotHasEnoughRights)//ErrorCodeValue.NoHasManagement) 
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        nRet = this.UiLogin(
                            "�����þ߱�����ԱȨ�޵��ʻ���¼�������޸��ʻ� '" + strUserName + "' ����...",
                            "",
                            LoginStyle.None,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDO;
                    }

                    /*
                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                     * */
                    // ԭ�����������
                    return -1;
                }

                this.ClearRedoCount();
                return 0;

            }

            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        void ClearRedoCount()
        {
            this.m_nRedoCount = 0;
        }

        // ��ʼ�����ݿ�
        // return:
        //		-1	����
        //		0	�ɹ�(����WebService�ӿ�InitializeDb�ķ���ֵ)
        public long DoInitialDB(string strDBName,
            out string strError)
        {
            strError = "";

            REDO:
            try
            {
            REDOINITIAL:
                IAsyncResult soapresult = this.ws.BeginInitializeDb(strDBName, null, null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndInitializeDb(soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin(strDBName,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDOINITIAL;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }


        // 2008/11/14
        // ˢ�����ݿⶨ��
        // return:
        //		-1	����
        //		0	�ɹ�(����WebService�ӿ�InitializeDb�ķ���ֵ)
        public long DoRefreshDB(
            string strAction,
            string strDBName,
            bool bClearAllKeyTables,
            out string strError)
        {
            strError = "";
            /*
            int nOldTimeout = this.Timeout;
            this.Timeout = 20 * 60 * 1000;  // �Ӵ�ʱʱ��
             * */
            REDO:
            try
            {
            REDO_REFRESH:
                IAsyncResult soapresult = this.ws.BeginRefreshDb(
                    strAction,
                    strDBName,
                    bClearAllKeyTables,
                    null,
                    null);
                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndRefreshDb(soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin(strDBName,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDO_REFRESH;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                // this.Timeout = nOldTimeout;
            }

        }

        // ɾ�����ݿ�
        // return:
        //		-1	����
        //		0	�ɹ�(����WebService�ӿ�DeleteDb�ķ���ֵ)
        public long DoDeleteDB(string strDBName,
            out string strError)
        {
            strError = "";

            REDO:
            try
            {
            REDOINITIAL:
                IAsyncResult soapresult = this.ws.BeginDeleteDb(strDBName, null, null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndDeleteDb(soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin(strDBName,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDOINITIAL;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                this.ClearRedoCount();
                return result.Value;
            }

            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }

        }

        // �������ݿ�
        // parameters:
        //		logicNames	�߼�������ArrayList��ÿ��Ԫ��Ϊһ��string[2]���͡����е�һ���ַ���Ϊ���֣��ڶ���Ϊ���Դ���
        // return:
        //		-1	����
        //		0	�ɹ�(����WebService�ӿ�CreateDb�ķ���ֵ)
        public long DoCreateDB(
            List<string[]> logicNames,
            string strType,
            string strSqlDbName,
            string strKeysDef,
            string strBrowseDef,
            out string strError)
        {
            strError = "";

            LogicNameItem[] logicnames = new LogicNameItem[logicNames.Count];
            for (int i = 0; i < logicnames.Length; i++)
            {
                logicnames[i] = new LogicNameItem();
                string[] cols = (string[])logicNames[i];
                logicnames[i].Lang = cols[1];
                logicnames[i].Value = cols[0];
            }

        REDO:
            try
            {
            REDOCREATE:
                IAsyncResult soapresult = this.ws.BeginCreateDb(logicnames,
                    strType,
                    strSqlDbName,
                    strKeysDef,
                    strBrowseDef,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndCreateDb(soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin("",
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDOCREATE;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                this.ClearRedoCount();
                return result.Value;
            }

            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }



        }


        // ������ݿ���Ϣ
        // parameters:
        //		logicNames	�߼�������ArrayList��ÿ��Ԫ��Ϊһ��string[2]���͡����е�һ���ַ���Ϊ���֣��ڶ���Ϊ���Դ���
        // return:
        //		-1	����
        //		0	�ɹ�(����WebService�ӿ�CreateDb�ķ���ֵ)
        public long DoGetDBInfo(
            string strDbName,
            string strStyle,
            out List<string[]> logicNames,
            out string strType,
            out string strSqlDbName,
            out string strKeysDef,
            out string strBrowseDef,
            out string strError)
        {
            strError = "";
            logicNames = new List<string[]>();
            strType = "";
            strSqlDbName = "";
            strKeysDef = "";
            strBrowseDef = "";

            LogicNameItem[] logicnames = null;

        REDO:
            try
            {
            REDOCREATE:
                IAsyncResult soapresult = this.ws.BeginGetDbInfo(strDbName,
                    strStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndGetDbInfo(
                    out logicnames,
                    out strType,
                    out strSqlDbName,
                    out strKeysDef,
                    out strBrowseDef,
                    soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin("",
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDOCREATE;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }



                for (int i = 0; i < logicnames.Length; i++)
                {
                    string[] cols = new string[2];
                    cols[1] = logicnames[i].Lang;
                    cols[0] = logicnames[i].Value;
                    logicNames.Add(cols);
                }

                this.ClearRedoCount();
                return result.Value;
            }

            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // �޸����ݿ���Ϣ
        // parameters:
        //		logicNames	�߼�������ArrayList��ÿ��Ԫ��Ϊһ��string[2]���͡����е�һ���ַ���Ϊ���֣��ڶ���Ϊ���Դ���
        // return:
        //		-1	����
        //		0	�ɹ�(����WebService�ӿ�CreateDb�ķ���ֵ)
        public long DoSetDBInfo(
            string strOldDbName,
            List<string[]> logicNames,
            string strType,
            string strSqlDbName,
            string strKeysDef,
            string strBrowseDef,
            out string strError)
        {
            strError = "";

            LogicNameItem[] logicnames = new LogicNameItem[logicNames.Count];
            for (int i = 0; i < logicnames.Length; i++)
            {
                logicnames[i] = new LogicNameItem();
                string[] cols = (string[])logicNames[i];
                logicnames[i].Lang = cols[1];
                logicnames[i].Value = cols[0];
            }

        REDO:
            try
            {
            REDOCREATE:
                IAsyncResult soapresult = this.ws.BeginSetDbInfo(strOldDbName,
                    logicnames,
                    strType,
                    strSqlDbName,
                    strKeysDef,
                    strBrowseDef,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndSetDbInfo(soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin("",
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDOCREATE;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                this.ClearRedoCount();
                return result.Value;
            }

            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }


        }


        // �ǳ�
        // return:
        //		-1	����
        //		0	�ɹ�
        public long DoLogout(out string strError)
        {
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginLogout(null, null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndLogout(soapresult);

                if (result.Value == -1)
                {
                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ��װ��İ汾
        public int DoCopyRecord(string strOriginRecordPath,
    string strTargetRecordPath,
    bool bDeleteOriginRecord,
    out byte[] baOutputTimeStamp,
    out string strOutputPath,
    out string strError)
        {
            string strIdChangeList = "";
            return DoCopyRecord(strOriginRecordPath,
                strTargetRecordPath,
                bDeleteOriginRecord,
                "",
                out strIdChangeList,
                out baOutputTimeStamp,
                out strOutputPath,
                out strError);
        }

        // ���Ƽ�¼
        // return:
        //		-1	����������Ϣ��strError��
        //		0��������		�ɹ�
        public int DoCopyRecord(string strOriginRecordPath,
            string strTargetRecordPath,
            bool bDeleteOriginRecord,
            string strMergeStyle,
            out string strIdChangeList,
            out byte[] baOutputTimeStamp,
            out string strOutputPath,
            out string strError)
        {
            strIdChangeList = "";
            baOutputTimeStamp = null;
            strOutputPath = "";
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginCopyRecord(strOriginRecordPath,
                    strTargetRecordPath,
                    bDeleteOriginRecord,
                    strMergeStyle,
                    null,
                    null);

                for (; ; )
                {

                    /*
                    try 
                    {
                        Application.DoEvents();	// ���ý������Ȩ
                    }
                    catch
                    {
                    }
					

                    // System.Threading.Thread.Sleep(10);	// ����CPU��Դ���Ⱥķ�
                     */
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndCopyRecord(
                    out strIdChangeList,
                    out strOutputPath,
                    out baOutputTimeStamp, soapresult);

                if (result.Value == -1)
                {
                    // 2011/4/21
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin("",
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDO;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                this.ClearRedoCount();

                if (result.Value == 0)
                {
                    strError = result.ErrorString;
                    return 0;
                }

                return 0;
            }

            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }

        }

        // ����������
        // return:
        //		-1	����������Ϣ��strError��
        //		0��������		�ɹ�
        public int DoBatchTask(string strName,
            string strAction,
            TaskInfo info,
            out TaskInfo [] results,
            out string strError)
        {
            results = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginBatchTask(strName,
                    strAction,
                    info,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndBatchTask(
                    out results,
                    soapresult);

                if (result.Value == -1)
                {
                    // 2011/4/21
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin("",
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDO;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                this.ClearRedoCount();

                if (result.Value == 0)
                {
                    strError = result.ErrorString;
                    return 0;
                }

                return 0;

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
        // ��װ��İ汾
        public long DoSearch(string strQueryXml,
            string strResultSetName,
            out string strError)
        {
            return this.DoSearch(strQueryXml,
                strResultSetName,
                "",
                out strError);
        }

#if NO
        object resultParam = null;
        AutoResetEvent eventComplete = new AutoResetEvent(false);

        // ����
        // return:
        //		-1	error
        //		0	not found
        //		>=1	���м�¼����
        public long DoSearch(string strQueryXml,
            string strResultSetName,
            string strOutputStyle,
            out string strError)
        {
            strError = "";

            try
            {
            REDOSEARCH:
                ws.SearchCompleted += new SearchCompletedEventHandler(ws_SearchCompleted);

                try
                {

                    this.eventComplete.Reset();
                    ws.SearchAsync(strQueryXml,
                        strResultSetName,
                        strOutputStyle);

                    while (true)
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                        bool bRet = this.eventComplete.WaitOne(10, true);
                        if (bRet == true)
                            break;
                    }

                }
                finally
                {
                    ws.SearchCompleted -= new SearchCompletedEventHandler(ws_SearchCompleted);
                }

                SearchCompletedEventArgs e = (SearchCompletedEventArgs)this.resultParam;

                if (e.Error != null)
                {
                    strError = e.Error.Message;
                    return -1;
                }

                /*
                if (e.Cancelled == true)
                    strError = "�û��ж�2";
                else
                    strError = e.Result.ErrorString;
                 * */

                Result result = e.Result;

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin("",
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDOSEARCH;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                this.ClearRedoCount();
                return result.Value;

            }
            catch (Exception ex)
            {
                strError = ConvertWebError(ex);
                return -1;
            }
            finally
            {
                soapresult = null;
            }
        }

        void ws_SearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            this.resultParam = e;
            this.eventComplete.Set();
        }
#endif

        // ( ��չ���ܺ��)����
        // parameters:
        //		strQuery	XML����ʽ
        //      strResultSetName    �������
        //      strSearchStyle  �������
        //      lRecordCount    ϣ����õļ�¼������-1��ʾ�����ܶࡣ���Ϊ0����ʾ�������κμ�¼
        //                      ���Ǵ�ƫ����0��ʼ��ü�¼
        //      strRecordStyle  ��ü�¼�ķ���Զ��ŷָ���id��ʾȡid,cols��ʾȡ�����ʽ
        //                      xml timestamp metadata �ֱ��ʾҪ��ȡ�ļ�¼���XML�ַ�����ʱ�����Ԫ����
        // return:
        //		-1	error
        //		0	not found
        //		>=1	���м�¼����
        public long DoSearchEx(string strQueryXml,
            string strResultSetName,
            string strSearchStyle,
            long lRecordCount,
            string strLang,
            string strRecordStyle,
            out Record[] records,
            out string strError)
        {
            strError = "";
            records = null;

        REDO:
            this.BeginSearch();
            try
            {
            REDOSEARCH:
                IAsyncResult soapresult = this.ws.BeginSearchEx(
                    strQueryXml,
                    strResultSetName,
                    strSearchStyle,
                    lRecordCount,
                    strLang,
                    strRecordStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }

                Result result = this.ws.EndSearchEx(
                    out records,
                    soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin("",
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDOSEARCH;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

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

        // ����
        // return:
        //		-1	error
        //		0	not found
        //		>=1	���м�¼����
        public long DoSearch(string strQueryXml,
            string strResultSetName,
            string strOutputStyle,
            out string strError)
        {
            strError = "";

        REDO:
            this.BeginSearch();
            try
            {
            REDOSEARCH:
                IAsyncResult soapresult = this.ws.BeginSearch(
                    strQueryXml,
                    strResultSetName,
                    strOutputStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }

                Result result = this.ws.EndSearch(soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin("",
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDOSEARCH;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }

                this.ClearRedoCount();
                return result.Value;

            }
            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
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


        // ����
        // return:
        //		-1	error
        //		0	not found
        //		>=1	���м�¼����
        public long DoSearchWithoutLoginDlg(
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
                //REDOSEARCH:
                IAsyncResult soapresult = this.ws.BeginSearch(
                    strQueryXml,
                    strResultSetName,
                    strOutputStyle,
                    null, null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndSearch(soapresult);
                if (result.Value == -1)
                {
                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
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

        // 2009/11/6
        // ����ָ���ļ�¼·����������ʽ��¼
        // ǳ��װ�汾
        // parameter:
        public long GetBrowseRecords(string[] paths,
            string strStyle,
            out Record[] searchresults,
            out string strError)
        {
            strError = "";
            searchresults = null;

                REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetBrowse(
                    paths,
                    strStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Record[] records = null;
                Result result = this.ws.EndGetBrowse(
                    out records,soapresult);

                if (result.Value == -1)
                {
                    // 2011/4/21
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin("",
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDO;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }
                else
                {
                    if (records == null)
                        throw new Exception("WebService GetBrowse() API record��������ֵ��ӦΪnull");

                    //lTotalCount = result.Value;
                }

                // ������Ƴ�
                searchresults = new Record[records.Length]; // SearchResult
                for (int i = 0; i < records.Length; i++)
                {
                    searchresults[i] = records[i];
                    /*
                    SearchResult searchresult = new SearchResult();
                    searchresults[i] = searchresult;

                    Record record = records[i];

                    searchresult.Path = record.ID;
                    searchresult.Keys = record.Keys;
                    searchresult.Cols = record.Cols;
                     * */
                }
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }

        }

        // ����ָ���ļ�¼·����������ʽ��¼
        // parameter:
        //		aRecord	���ص������¼��Ϣ��һ��ArrayList���顣ÿ��Ԫ��Ϊһ��string[]��������������
        //				����strStyle���������strStyle����id����aRecordÿ��Ԫ���е�string[]��һ���ַ�������id�������Ǹ������ݡ�
        public int GetBrowseRecords(string[] paths,
            string strStyle,
            out ArrayList aRecord,
            out string strError)
        {
            strError = "";
            aRecord = new ArrayList();

            int nStart = 0;

            bool bIncludeID = StringUtil.IsInList("id", strStyle, true);

            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                try
                {
                    int nPerCount = paths.Length - nStart;

                    string[] temppaths = new string[nPerCount];
                    Array.Copy(paths, nStart, temppaths, 0, nPerCount);

                    IAsyncResult soapresult = this.ws.BeginGetBrowse(
                        temppaths,
                        strStyle,
                        null,
                        null);

                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    Record[] records = null;
                    Result result = this.ws.EndGetBrowse(
                        out records,
                        soapresult);

                    if (result.Value == -1)
                    {
                        strError = result.ErrorString;
                        return -1;
                    }
                    else
                    {
                        if (records == null)
                            throw new Exception("WebService GetBrowse() API record��������ֵ��ӦΪnull");

                        //lTotalCount = result.Value;
                    }

                    // ����
                    for (int i = 0; i < records.Length; i++)
                    {
                        Record record = records[i];

                        if (bIncludeID == true)
                        {

                            string[] cols = new string[record.Cols.Length + (bIncludeID == true ? 1 : 0)];

                            if (bIncludeID)
                                cols[0] = record.Path;

                            if (record.Cols.Length > 0)
                                Array.Copy(record.Cols, 0, cols, (bIncludeID == true ? 1 : 0), record.Cols.Length);

                            aRecord.Add(cols);
                        }
                        else
                        {
                            aRecord.Add(record.Cols);
                        }
                    }


                    nStart += records.Length;

                    if (nStart >= paths.Length)
                        break;
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }

            } // end of for

            return 0;

        }


        // ��������ʽ��¼
        // parameter:
        //		nStart	��ʼ���
        //		nLength	����
        public int GetRecords(
            string strResultSetName,
            long nStart,
            long nLength,
            string strLang,
            out ArrayList aRecord,
            out string strError)
        {
            strError = "";
            aRecord = new ArrayList();

            long nPerCount = -1;    // BUG? ������һ�����⣬���ܻ�ȡ����ļ�¼������nLength

            int nCount = 0;
            long lTotalCount = nLength;
            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                try
                {
                    IAsyncResult soapresult = this.ws.BeginGetRecords(
                        strResultSetName,
                        nStart,
                        nPerCount,
                        strLang,
                        "id,cols",
                        null,
                        null);

                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    Record[] records = null;
                    Result result = this.ws.EndGetRecords(
                        out records,soapresult);

                    if (result.Value == -1)
                    {
                        strError = result.ErrorString;
                        return -1;
                    }
                    else
                    {
                        if (records == null)
                            throw new Exception("WebService GetRecords() API record��������ֵ��ӦΪnull");

                        //lTotalCount = result.Value;
                    }

                    // ����
                    for (int i = 0; i < records.Length; i++)
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                        Record record = records[i];

                        // �����Լ������ͣ�����һ���ڽ���������
                        dp2opacRecord opacRecord = new dp2opacRecord();
                        opacRecord.Record = record;
                        opacRecord.IndexOfResult = nStart;
                        aRecord.Add(opacRecord);

                        nStart++;
                        nCount++;

                        if (lTotalCount != -1
                            && nCount >= lTotalCount)
                            break;
                    }

                    if (lTotalCount != -1
                        && nCount >= lTotalCount)
                        break;

                    if (nCount >= result.Value)
                        break;

                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }

            } // end of for

            return 0;

        }


        // ��������ʽ��¼
        // parameter:
        //		nStart	��ʼ���
        //		nLength	����
        public int GetRichRecords(
            string strResultSetName,
            long nStart,
            long nLength,
            string strStyle,
            string strLang,
            out List<RichRecord> aRecord,
            out string strError)
        {
            strError = "";
            aRecord = new List<RichRecord>();

            long nPerCount = nLength;

            int nCount = 0;
            long lTotalCount = nLength;
            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                try
                {
                    string strRange = nStart.ToString() + "-" + (nStart + nPerCount - 1).ToString();

                    IAsyncResult soapresult = this.ws.BeginGetRichRecords(
                        strResultSetName,
                        strRange,
                        strLang,
                        strStyle,
                        null,
                        null);

                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    RichRecord[] records = null;
                    Result result = this.ws.EndGetRichRecords(
                        out records,soapresult);

                    if (result.Value == -1)
                    {
                        strError = result.ErrorString;
                        return -1;
                    }
                    else
                    {
                        if (records == null)
                            throw new Exception("WebService GetRichRecords() API record��������ֵ��ӦΪnull");

                        //lTotalCount = result.Value;
                    }

                    // ����
                    for (int i = 0; i < records.Length; i++)
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                        RichRecord record = records[i];

                        aRecord.Add(record);

                        nStart++;
                        nCount++;

                        if (lTotalCount != -1
                            && nCount >= lTotalCount)
                            break;
                    }

                    nPerCount -= records.Length;
                    if (nPerCount <= 0)
                        break;

                    if (lTotalCount != -1
                        && nCount >= lTotalCount)
                        break;

                    if (nCount >= result.Value)
                        break;

                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }
            } // end of for

            return 0;
        }

        public static string BuildDisplayKeyString(KeyFrom[] keys)
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

        // ��ȡ�����¼
        public long DoBrowse(
            BrowseList listView,
            string strLang,
            DigitalPlatform.Stop stop,
            string strResultSetName,
            string strOutputStyle,
            out string strError)
        {
            strError = "";

            Record[] records = null;

            long nStart = 0;
            long nPerCount = -1;

            int nCount = 0;

            bool bOutputKeyID = StringUtil.IsInList("keyid", strOutputStyle);

            long lTotalCount = -1;
            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }
                }

                nPerCount = -1; // 2013/2/12
            REDO:
                try
                {
                    IAsyncResult soapresult = this.ws.BeginGetRecords(
                        strResultSetName,
                        nStart,
                        nPerCount,
                        strLang,
                        strOutputStyle, //"id,cols",
                        null,
                        null);

                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    Result result = this.ws.EndGetRecords(
                        out records,soapresult);

                    if (result.Value == -1)
                    {
                        // 2011/4/21
                        if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                        {
                            // return:
                            //		-1	error
                            //		0	login failed
                            //		1	login succeed
                            int nRet = this.UiLogin("",
                                out strError);
                            if (nRet == -1 || nRet == 0)
                            {
                                return -1;
                            }

                            goto REDO;
                        }

                        ConvertErrorCode(result);
                        strError = result.ErrorString;
                        return -1;
                    }
                    else
                    {
                        Debug.Assert(records != null, "WebService GetRecords() API record��������ֵ��ӦΪnull");

                        lTotalCount = result.Value;

                        if (nStart == 0 && stop != null)
                            stop.SetProgressRange(0, lTotalCount);
                    }

                    if (records != null)
                    {
                        nCount += records.Length;
                    }

                    listView.BeginUpdate();
                    try
                    {
                        // ����
                        for (int i = 0; i < records.Length; i++)
                        {
                            DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                            if (stop != null)
                            {
                                if (stop.State != 0)
                                {
                                    strError = "�û��ж�";
                                    return -1;
                                }

                                stop.SetMessage("����װ�� " + Convert.ToString(nStart + i) + " / "
                                    + ((lTotalCount == -1) ? "?" : Convert.ToString(lTotalCount)));
                            }

                            Record record = records[i];

                            string[] cols = null;
                            if (bOutputKeyID == true)
                            {
                                cols = new string[(record.Cols == null ? 0 : record.Cols.Length) + 1];
                                cols[0] = BuildDisplayKeyString(record.Keys);
                                if (cols.Length > 1)
                                    Array.Copy(record.Cols, 0, cols, 1, cols.Length - 1);
                            }
                            else
                                cols = record.Cols;

                            listView.NewLine(record.Path + " @" + this.Url,
                                cols);
                            // record.ID �����һ��
                            // record.Cols ����������(���Ϊkeyid��ʽ��key����һȺ�ĵ�һ��)
                        }

                        if (stop != null)
                            stop.SetProgressValue(nStart + records.Length);
                    }
                    finally
                    {
                        listView.EndUpdate();
                    }

                    if (nCount >= result.Value)
                        break;

                    if (records != null)
                    {
                        nStart += records.Length;
                    }

                }
                catch (Exception ex)
                {
                    /*
                    strError = ConvertWebError(ex);
                    return -1;
                     * */
                    int nRet = ConvertWebError(ex, out strError);
                    if (nRet == 0)
                        return -1;
                    // 2013/2/11
                    if (this.ErrorCode == ChannelErrorCode.QuotaExceeded)
                    {
                        if (nPerCount > 1 || nPerCount == -1)
                            nPerCount = 1;   // �޸�Ϊ��С��������һ��
                        else
                            return -1;
                    }
                    goto REDO;
                }
            }

            this.ClearRedoCount();
            if (stop != null)
                stop.HideProgress();
            return 0;
        }

        public long DoGetSearchResult(
            string strResultSetName,
            long lMax,
            string strLang,
            DigitalPlatform.Stop stop,
            out List<string> aPath,
            out string strError)
        {
            return DoGetSearchResult(
                strResultSetName,
                0,
                lMax,
                strLang,
                stop,
                out aPath,
                out strError);
        }

        // ϣ���𲽷�ֹ������
        // ��ȡ�������н��
        // ֻ��ȡid��
        // return:
        //      -1  ����
        //      ����    aPath�еĽ����Ŀ
        public long DoGetSearchResult(
            string strResultSetName,
            long lStart,
            long lMax,
            string strLang,
            DigitalPlatform.Stop stop,
            out List<string> aPath,
            out string strError)
        {
            strError = "";
            aPath = new List<string>();

            Record[] records = null;

            long nPerCount = lMax;	// -1;

            // int nCount = 0;

            long lTotalCount = -1;
            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }
                }

REDO:
                try
                {
                    IAsyncResult soapresult = this.ws.BeginGetRecords(
                        strResultSetName,
                        lStart,
                        nPerCount,
                        strLang,
                        "id",	// ��Ҫcols
                        null,
                        null);

                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    Result result = this.ws.EndGetRecords(
                        out records,soapresult);

                    if (result.Value == -1)
                    {
                        // 2011/4/21
                        if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                        {
                            // return:
                            //		-1	error
                            //		0	login failed
                            //		1	login succeed
                            int nRet = this.UiLogin("",
                                out strError);
                            if (nRet == -1 || nRet == 0)
                            {
                                return -1;
                            }

                            goto REDO;
                        }
                        ConvertErrorCode(result);
                        strError = result.ErrorString;
                        return -1;
                    }
                    else
                    {
                        Debug.Assert(records != null, "WebService GetRecords() API record��������ֵ��ӦΪnull");

                        lTotalCount = result.Value;
                        if (lMax != -1)
                            lTotalCount = Math.Min(lTotalCount, lMax);
                    }

                    if (records != null)
                    {
                        lStart += records.Length;
                        // nCount += records.Length;
                        nPerCount = lTotalCount - lStart;
                    }

                    // ����
                    for (int i = 0; i < records.Length; i++)
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                return -1;
                            }

                            stop.SetMessage("����װ�� " + Convert.ToString(lStart + i) + " / "
                                + ((lTotalCount == -1) ? "?" : Convert.ToString(lTotalCount)));
                        }

                        Record record = records[i];
                        aPath.Add(record.Path);
                    }

                    // BUG�޸� 2010/11/16
                    if (/*lStart + nCount >= result.Value
                        || */ lStart >= lTotalCount)
                        break;
                }

                catch (Exception ex)
                {
                    /*
                    strError = ConvertWebError(ex);
                    return -1;
                     * */
                    int nRet = ConvertWebError(ex, out strError);
                    if (nRet == 0)
                        return -1;
                    // 2013/2/11
                    if (this.ErrorCode == ChannelErrorCode.QuotaExceeded)
                    {
                        if (nPerCount > 1 || nPerCount == -1)
                            nPerCount = 1;   // �޸�Ϊ��С��������һ��
                        else
                            return -1;
                    }
                    goto REDO;
                }
            }

            this.ClearRedoCount();
            return aPath.Count;
        }

        // �Ľ���
        // ��ȡ�������н��
        // ֻ��ȡid��
        // return:
        //      -1  ����
        //      ����    ������ڼ�¼����
        public long DoGetSearchResultEx(
            string strResultSetName,
            long lStart,
            long lMax,
            string strLang,
            DigitalPlatform.Stop stop,
            out List<string> aPath,
            out string strError)
        {
            strError = "";
            aPath = new List<string>();

            Record[] records = null;

            long nPerCount = lMax;	// -1;

            // int nCount = 0;

            long lResultTotalCount = -1;
            long lTempTotalCount = -1;
            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }
                }

            REDO:
                try
                {
                    IAsyncResult soapresult = this.ws.BeginGetRecords(
                        strResultSetName,
                        lStart,
                        nPerCount,
                        strLang,
                        "id",	// ��Ҫcols
                        null,
                        null);

                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    Result result = this.ws.EndGetRecords(
                        out records,soapresult);

                    if (result.Value == -1)
                    {
                        // 2011/4/21
                        if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                        {
                            // return:
                            //		-1	error
                            //		0	login failed
                            //		1	login succeed
                            int nRet = this.UiLogin("",
                                out strError);
                            if (nRet == -1 || nRet == 0)
                            {
                                return -1;
                            }

                            goto REDO;
                        }
                        ConvertErrorCode(result);
                        strError = result.ErrorString;
                        return -1;
                    }
                    else
                    {
                        Debug.Assert(records != null, "WebService GetRecords() API record��������ֵ��ӦΪnull");

                        lResultTotalCount = result.Value;

                        lTempTotalCount = result.Value;
                        if (lMax != -1)
                            lTempTotalCount = Math.Min(lTempTotalCount, lMax);
                    }

                    if (records != null)
                    {
                        lStart += records.Length;
                        // nCount += records.Length;
                        nPerCount = lTempTotalCount - lStart;
                    }

                    // ����
                    for (int i = 0; i < records.Length; i++)
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                return -1;
                            }

                            stop.SetMessage("����װ�� " + Convert.ToString(lStart + i) + " / "
                                + ((lTempTotalCount == -1) ? "?" : Convert.ToString(lTempTotalCount)));
                        }

                        Record record = records[i];

                        aPath.Add(record.Path);
                    }

                    // BUG�޸� 2010/11/16
                    if (/*lStart + nCount >= result.Value
                        || */
                             lStart >= lTempTotalCount)
                        break;
                }
                catch (Exception ex)
                {
                    /*
                    strError = ConvertWebError(ex);
                    return -1;
                     * */
                    int nRet = ConvertWebError(ex, out strError);
                    if (nRet == 0)
                        return -1;
                    // 2013/2/11
                    if (this.ErrorCode == ChannelErrorCode.QuotaExceeded)
                    {
                        if (nPerCount > 1 || nPerCount == -1)
                            nPerCount = 1;   // �޸�Ϊ��С��������һ��
                        else
                            return -1;
                    } 
                    
                    goto REDO;
                }
            }

            this.ClearRedoCount();
            return lResultTotalCount; // ������ڵļ�¼����
        }

        // ��ȡ�������н��
        // ���ÿ����ϸ��Ϣ�İ汾
        public long DoGetSearchFullResult(
            string strResultSetName,
            long lStart,
            long lMax,
            string strLang,
            DigitalPlatform.Stop stop,
            out ArrayList aLine,
            out string strError)
        {
            strError = "";
            aLine = new ArrayList();

            Record[] records = null;

            long nPerCount = lMax;	// -1;

            int nCount = 0;

            long lTotalCount = -1;
            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }
                }


                    REDO:
                try
                {
                    IAsyncResult soapresult = this.ws.BeginGetRecords(
                        strResultSetName,
                        lStart,
                        nPerCount,
                        strLang,
                        "id,cols",	// ��Ҫcols
                        null,
                        null);

                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    Result result = this.ws.EndGetRecords(
                        out records,soapresult);

                    if (result.Value == -1)
                    {
                        // 2011/4/21
                        if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                        {
                            // return:
                            //		-1	error
                            //		0	login failed
                            //		1	login succeed
                            int nRet = this.UiLogin("",
                                out strError);
                            if (nRet == -1 || nRet == 0)
                            {
                                return -1;
                            }

                            goto REDO;
                        }
                        ConvertErrorCode(result);
                        strError = result.ErrorString;
                        return -1;
                    }
                    else
                    {
                        Debug.Assert(records != null, "WebService GetRecords() API record��������ֵ��ӦΪnull");

                        lTotalCount = result.Value;
                        if (lMax != -1)
                            lTotalCount = Math.Min(lTotalCount, lMax);
                    }

                    if (records != null)
                    {
                        lStart += records.Length;
                        nCount += records.Length;
                        nPerCount = lTotalCount - lStart;
                    }

                    // ����
                    for (int i = 0; i < records.Length; i++)
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                return -1;
                            }

                            stop.SetMessage("����װ�� " + Convert.ToString(lStart + i) + " / "
                                + ((lTotalCount == -1) ? "?" : Convert.ToString(lTotalCount)));
                        }



                        Record record = records[i];
                        string[] acol = new string[record.Cols.Length + 1];
                        acol[0] = record.Path;
                        for (int j = 0; j < record.Cols.Length; j++)
                        {
                            acol[j + 1] = record.Cols[j];
                        }

                        aLine.Add(acol);
                    }

                    if (nCount >= result.Value || nCount >= lTotalCount)
                        break;

                }

                catch (Exception ex)
                {
                    /*
                    strError = ConvertWebError(ex);
                    return -1;
                     * */
                    int nRet = ConvertWebError(ex, out strError);
                    if (nRet == 0)
                        return -1;

                    // 2013/2/11
                    if (this.ErrorCode == ChannelErrorCode.QuotaExceeded)
                    {
                        if (nPerCount > 1 || nPerCount == -1)
                            nPerCount = 1;   // �޸�Ϊ��С��������һ��
                        else
                            return -1;
                    } 
                    
                    goto REDO;
                }
            }

            this.ClearRedoCount();
            return 0;
        }

        // 2009/7/19
        // ��ȡ�������н��
        // ���ĳһ����Ϣ�İ汾
        public long DoGetSearchResultOneColumn(
            string strResultSetName,
            long lStart,
            long lMax,
            string strLang,
            DigitalPlatform.Stop stop,
            int nColumn,
            out List<string> aLine,
            out string strError)
        {
            strError = "";
            aLine = new List<string>();

            Record[] records = null;

            long nPerCount = lMax;	// -1;

            int nCount = 0;

            long lTotalCount = -1;
            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }
                }


                    REDO:
                try
                {
                    IAsyncResult soapresult = this.ws.BeginGetRecords(
                        strResultSetName,
                        lStart,
                        nPerCount,
                        strLang,
                        "id,cols",	// ��Ҫcols
                        null,
                        null);

                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    Result result = this.ws.EndGetRecords(
                        out records,soapresult);

                    if (result.Value == -1)
                    {
                        // 2011/4/21
                        if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                        {
                            // return:
                            //		-1	error
                            //		0	login failed
                            //		1	login succeed
                            int nRet = this.UiLogin("",
                                out strError);
                            if (nRet == -1 || nRet == 0)
                            {
                                return -1;
                            }

                            goto REDO;
                        }
                        ConvertErrorCode(result);
                        strError = result.ErrorString;
                        return -1;
                    }
                    else
                    {
                        Debug.Assert(records != null, "WebService GetRecords() API record��������ֵ��ӦΪnull");

                        lTotalCount = result.Value;
                        if (lMax != -1)
                            lTotalCount = Math.Min(lTotalCount, lMax);
                    }

                    if (records != null)
                    {
                        lStart += records.Length;
                        nCount += records.Length;
                        nPerCount = lTotalCount - lStart;
                    }

                    // ����
                    for (int i = 0; i < records.Length; i++)
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                return -1;
                            }

                            stop.SetMessage("����װ�� " + Convert.ToString(lStart + i) + " / "
                                + ((lTotalCount == -1) ? "?" : Convert.ToString(lTotalCount)));
                        }



                        Record record = records[i];
                        aLine.Add(record.Cols[nColumn]);
                    }

                    if (nCount >= result.Value || nCount >= lTotalCount)
                        break;

                }

                catch (Exception ex)
                {
                    /*
                    strError = ConvertWebError(ex);
                    return -1;
                     * */
                    int nRet = ConvertWebError(ex, out strError);
                    if (nRet == 0)
                        return -1;

                    // 2013/2/11
                    if (this.ErrorCode == ChannelErrorCode.QuotaExceeded)
                    {
                        if (nPerCount > 1 || nPerCount == -1)
                            nPerCount = 1;   // �޸�Ϊ��С��������һ��
                        else
                            return -1;
                    }

                    goto REDO;
                }
            }

            this.ClearRedoCount();
            return 0;
        }

        // 2012/11/11
        // ����д��XML��¼
        // ǳ��װ�汾
        // ÿ��Ԫ����Xml��Ա�ڷ���һ��������XML��¼�������¼���������벻Ҫʹ�ô�API��
        // results�з��غ�inputsһ����Ŀ��Ԫ�أ�ÿ��Ԫ�ر�ʾ��Ӧ��inputsԪ��д���Ƿ�ɹ�������ʱ�����ʵ��д���·��
        // ����;���������£�results�е�Ԫ����Ŀ���inputs�е��٣�����ǰ����˳���ǹ̶��ģ����Զ�Ӧ
        public long DoWriteRecords(
            DigitalPlatform.Stop stop,
            RecordBody[] inputs,
            string strStyle,
            out RecordBody[] results,
            out string strError)
        {
            strError = "";

            results = null;
        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginWriteRecords(
                    inputs,
                    strStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                // Record[] records = null;

                Result result = this.ws.EndWriteRecords(
                    out results, soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin("",
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDO;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;
                    return -1;
                }
                else
                {
                    // �����������鷵�ز���
                }

                this.ClearRedoCount();
                return result.Value;    // �����������
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ��ü�������������ʽ
        // ǳ��װ�汾
        public long DoGetSearchResult(
            string strResultSetName,
            long lStart,
            long lMax,
            string strColumnStyle,
            string strLang,
            DigitalPlatform.Stop stop,
            out Record[] searchresults,
            out string strError)
        {
            strError = "";

            searchresults = null;

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetRecords(
                    strResultSetName,
                    lStart,
                    lMax,
                    strLang,
                    strColumnStyle, // "id,cols"
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                // Record[] records = null;

                Result result = this.ws.EndGetRecords(
                    out searchresults,  // records,
                    soapresult);

                if (result.Value == -1)
                {
                    // 2011/4/18
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin("",
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDO;
                    }

                    ConvertErrorCode(result);

                    strError = result.ErrorString;
                    return -1;
                }
                else
                {
                    // Debug.Assert(records != null, "WebService GetRecords() API record��������ֵ��ӦΪnull");
                }

#if NO
                // ������Ƴ�
                searchresults = new Record[records.Length]; // SearchResult
                for (int i = 0; i < records.Length; i++)
                {
                    searchresults[i] = records[i];
                }
#endif

                this.ClearRedoCount();
                return result.Value;    // �����������
            }

            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;

                // 2013/2/11
                if (this.ErrorCode == ChannelErrorCode.QuotaExceeded)
                {
                    if (lMax > 1 || lMax == -1)
                        lMax = 1;   // �޸�Ϊ��С��������һ��
                    else
                        return -1;
                }

                goto REDO;
            }
        }

        // ģ�ⴴ��������
        public long DoGetKeys(
            string strRecPath,
            string strXmlBody,
            string strLang,
            // string strStyle,
            DigitalPlatform.Stop stop,
            out List<AccessKeyInfo> aLine,
            out string strError)
        {
            strError = "";
            aLine = null;

            if (strRecPath == "")
            {
                strError = "��¼·��Ϊ��ʱ�޷�ģ�ⴴ��������";
                return -1;
            }

            KeyInfo[] keys = null;

            int nStart = 0;
            int nPerCount = -1;

            int nCount = 0;

            aLine = new List<AccessKeyInfo>();

            long lTotalCount = -1;
            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }
                }


                    REDO:
                try
                {
                    IAsyncResult soapresult = this.ws.BeginCreateKeys(
                        strXmlBody,
                        strRecPath,
                        nStart,
                        nPerCount,
                        strLang,
                        // strStyle,
                        null,
                        null);

                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    Result result = this.ws.EndCreateKeys(
                        out keys,soapresult);

                    if (result.Value == -1)
                    {
                        if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                        {
                            // return:
                            //		-1	error
                            //		0	login failed
                            //		1	login succeed
                            int nRet = this.UiLogin(strRecPath,
                                out strError);
                            if (nRet == -1 || nRet == 0)
                            {
                                return -1;
                            }

                            goto REDO;
                        }


                        ConvertErrorCode(result);
                        strError = result.ErrorString;
                        return -1;
                    }
                    else
                    {
                        Debug.Assert(keys != null, "WebService GetRecords() API record��������ֵ��ӦΪnull");

                        lTotalCount = result.Value;
                    }

                    if (keys != null)
                    {
                        nStart += keys.Length;
                        nCount += keys.Length;
                    }

                    // ����
                    for (int i = 0; i < keys.Length; i++)
                    {
                        /*
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null) 
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                return -1;
                            }

                            stop.SetMessage("����װ�� " + Convert.ToString(nStart+i)+" / "
                                + ((lTotalCount == -1) ? "?" : Convert.ToString(lTotalCount)) );
                        }
                        */
                        KeyInfo keyInfo = keys[i];

                        AccessKeyInfo info = new AccessKeyInfo();
                        info.FromValue = keyInfo.FromValue;
                        info.ID = keyInfo.ID;
                        info.Key = keyInfo.Key;
                        info.KeyNoProcess = keyInfo.KeyNoProcess;
                        info.Num = keyInfo.Num;
                        info.FromName = keyInfo.FromName;

                        aLine.Add(info);
                    }

                    if (nCount >= result.Value)
                        break;
                }

                catch (Exception ex)
                {
                    /*
                    strError = ConvertWebError(ex);
                    return -1;
                     * */
                    int nRet = ConvertWebError(ex, out strError);
                    if (nRet == 0)
                        return -1;
                    goto REDO;
                }
            }

            this.ClearRedoCount();
            return 0;
        }

        // ģ�ⴴ��������
        public long DoGetKeys(
            string strRecPath,
            string strXmlBody,
            string strLang,
            // string strStyle,
            ViewAccessPointForm dlg,
            DigitalPlatform.Stop stop,
            out string strError)
        {
            strError = "";

            if (strRecPath == "")
            {
                strError = "��¼·��Ϊ��ʱ�޷�ģ�ⴴ��������";
                return -1;
            }

            KeyInfo[] keys = null;

            int nStart = 0;
            int nPerCount = -1;

            int nCount = 0;

            dlg.Clear();

            long lTotalCount = -1;
            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }
                }


                    REDO:
                try
                {
                    IAsyncResult soapresult = this.ws.BeginCreateKeys(
                        strXmlBody,
                        strRecPath,
                        nStart,
                        nPerCount,
                        strLang,
                        // strStyle,
                        null,
                        null);

                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    Result result = this.ws.EndCreateKeys(
                        out keys,soapresult);

                    if (result.Value == -1)
                    {
                        // 2011/4/21
                        if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                        {
                            // return:
                            //		-1	error
                            //		0	login failed
                            //		1	login succeed
                            int nRet = this.UiLogin("",
                                out strError);
                            if (nRet == -1 || nRet == 0)
                            {
                                return -1;
                            }

                            goto REDO;
                        }
                        ConvertErrorCode(result);
                        strError = result.ErrorString;
                        return -1;
                    }
                    else
                    {
                        Debug.Assert(keys != null, "WebService GetRecords() API record��������ֵ��ӦΪnull");

                        lTotalCount = result.Value;
                    }

                    if (keys != null)
                    {
                        nStart += keys.Length;
                        nCount += keys.Length;
                    }

                    // ����
                    for (int i = 0; i < keys.Length; i++)
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                return -1;
                            }

                            stop.SetMessage("����װ�� " + Convert.ToString(nStart + i) + " / "
                                + ((lTotalCount == -1) ? "?" : Convert.ToString(lTotalCount)));
                        }



                        KeyInfo keyInfo = keys[i];

                        dlg.NewLine(keyInfo);
                    }

                    if (nCount >= result.Value)
                        break;
                }

                catch (Exception ex)
                {
                    /*
                    strError = ConvertWebError(ex);
                    return -1;
                     * */
                    int nRet = ConvertWebError(ex, out strError);
                    if (nRet == 0)
                        return -1;
                    goto REDO;
                }

            }

            this.ClearRedoCount();
            return 0;
        }


        // ��Ŀ¼�������ַ�������ļ򻯰汾
        // parameters:
        //      nType   ֻ�����ض�����Դ����
        public long DoDir(string strPath,
            string strLang,
            string strStyle,
            int nType,
            out string[] results,
            out string strError)
        {
            results = null;

            ResInfoItem[] results1 = null;
            long lRet = DoDir(strPath,
                strLang,
                strStyle,
                out results1,
                out strError);
            if (lRet == -1)
                return -1;
            int i = 0;
            ArrayList aResult = new ArrayList();
            for (i = 0; i < results1.Length; i++)
            {
                if (results1[i].Type != nType)
                    continue;
                aResult.Add(results1[i].Name);
            }

            results = new string[aResult.Count];
            for (i = 0; i < aResult.Count; i++)
            {
                results[i] = (string)aResult[i];
            }

            return lRet;
        }

        // ����ԴĿ¼
        public long DoDir(string strPath,
            string strLang,
            string strStyle,
            out ResInfoItem[] results,
            out string strError)
        {
            strError = "";
            results = null;

            ResInfoItem[] items = null;

            int nStart = 0;
            int nPerCount = -1;

            int nCount = 0;

            ArrayList aItem = new ArrayList();

            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

            REDO:
                try
                {
                REDODIR:
                    IAsyncResult soapresult = this.ws.BeginDir(strPath,
                        nStart,
                        nPerCount,
                        strLang,
                        strStyle,
                        null,
                        null);


                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    Result result = this.ws.EndDir(
                        out items,soapresult);

                    if (result.Value == -1)
                    {
                        if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                        {
                            // return:
                            //		-1	error
                            //		0	login failed
                            //		1	login succeed
                            int nRet = this.UiLogin(strPath,
                                out strError);
                            if (nRet == -1 || nRet == 0)
                            {
                                return -1;
                            }

                            goto REDODIR;
                        }

                        ConvertErrorCode(result);
                        strError = result.ErrorString;
                        return -1;
                    }
                    if (items != null)
                    {
                        nStart += items.Length;
                        nCount += items.Length;
                    }

                    // ����
                    for (int i = 0; i < items.Length; i++)
                    {
                        aItem.Add(items[i]);
                    }

                    if (nCount >= result.Value)
                        break;

                }
                catch (Exception ex)
                {
                    /*
                    strError = ConvertWebError(ex);
                    return -1;
                     * */
                    int nRet = ConvertWebError(ex, out strError);
                    if (nRet == 0)
                        return -1;
                    goto REDO;
                }
            } // end of for

            results = new ResInfoItem[aItem.Count];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = (ResInfoItem)aItem[i];
            }

            this.ClearRedoCount();
            return 0;
        }


        // д����Դ��ԭʼ�汾��2007/5/27
        public long WriteRes(string strResPath,
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
            this.ErrorInfo = "";
            strError = "";
            strOutputResPath = "";
            baOutputTimestamp = null;

        REDO:
            try
            {
            REDOSAVE:
                IAsyncResult soapresult = this.ws.BeginWriteRes(strResPath,
                    strRanges,
                    lTotalLength,
                    baContent,
                    // null,	// attachmentid
                    strMetadata,
                    strStyle,
                    baInputTimestamp,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndWriteRes(
                    out strOutputResPath,
                    out baOutputTimestamp,soapresult);

                this.ErrorInfo = result.ErrorString;	// �����Ƿ񷵻ش��󣬶���result��ErrorString�ŵ�Channel��

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin(strResPath,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDOSAVE;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;

                    if (result.ErrorCode == ErrorCodeValue.TimestampMismatch)
                    {
                        this.ErrorCode = ChannelErrorCode.TimestampMismatch;
                        strError = "ʱ�����ƥ�䡣\r\n\r\n�����ʱ��� [" + ByteArray.GetHexTimeStampString(baInputTimestamp) + "] ��Ӧ��ʱ��� [" + ByteArray.GetHexTimeStampString(baOutputTimestamp) + "]";
                        return -1;
                    }

                    // ԭ��Convert....���������
                    return -1;
                }

                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
        }

        // ����Xml��¼
        public long DoSaveTextRes(string strPath,
            string strXml,
            bool bInlucdePreamble,
            string strStyle,
            byte[] timestamp,
            out byte[] output_timestamp,
            out string strOutputPath,
            out string strError)
        {
            this.ErrorInfo = "";
            strError = "";
            strOutputPath = "";
            output_timestamp = null;
            int nDoCount = 0;

            int nChunkMaxLength = 500 * 1024;	// chunk size��Ϊ�������ٶȣ�Ӧ�þ����� ԭ���� 4096

            int nStart = 0;

            byte[] baInputTimeStamp = null;
            //byte[] baOutputTimeStamp = null;
            output_timestamp = null;

            byte[] baPreamble = Encoding.UTF8.GetPreamble();

            byte[] baTotal = Encoding.UTF8.GetBytes(strXml);

            if (bInlucdePreamble == true
                && baPreamble != null && baPreamble.Length > 0)
            {
                byte[] temp = null;
                temp = ByteArray.Add(temp, baPreamble);
                baTotal = ByteArray.Add(temp, baTotal);
            }

            long lTotalLength = baTotal.Length;

            if (timestamp != null)
            {
                baInputTimeStamp = ByteArray.Add(baInputTimeStamp, timestamp);
            }

            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                // �г�chunk
                int nThisChunkSize = nChunkMaxLength;

                if (nThisChunkSize + nStart > lTotalLength)
                {
                    nThisChunkSize = (int)lTotalLength - nStart;	// ���һ��
                    if (nThisChunkSize <= 0 && nDoCount > 1)
                        break;
                }

                byte[] baChunk = new byte[nThisChunkSize];
                Array.Copy(baTotal, nStart, baChunk, 0, baChunk.Length);

            REDO:
                try
                {
                REDOSAVE:
                    string strMetadata = "";
                    string strRange = "";
                    int nEnd = nStart + baChunk.Length - 1;

                    // 2008/10/17 changed
                    if (nEnd >= nStart)
                        strRange = Convert.ToString(nStart) + "-" + Convert.ToString(nEnd);

                    IAsyncResult soapresult = this.ws.BeginWriteRes(strPath,
                        strRange,
                        //nStart,
                        lTotalLength,	// �����������ߴ磬���Ǳ���chunk�ĳߴ硣��Ϊ��������Ȼ���Դ�baChunk�п�����ߴ磬������ר����һ��������ʾ����ߴ���
                        baChunk,
                        // null,	// attachmentid
                        strMetadata,
                        strStyle,
                        baInputTimeStamp,
                        null,
                        null);
                    nDoCount++;

                    for (; ; )
                    {
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    Result result = this.ws.EndWriteRes(
                        out strOutputPath,
                        out output_timestamp/*baOutputTimeStamp*/,soapresult);

                    this.ErrorInfo = result.ErrorString;	// �����Ƿ񷵻ش��󣬶���result��ErrorString�ŵ�Channel��
                    strError = result.ErrorString;  // 2007/6/28 ������ ���оֲ�path�ı����з���ֵ����strError�����

                    if (result.Value == -1)
                    {
                        if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                        {
                            // return:
                            //		-1	error
                            //		0	login failed
                            //		1	login succeed
                            int nRet = this.UiLogin(strPath,
                                out strError);
                            if (nRet == -1 || nRet == 0)
                            {
                                return -1;
                            }

                            goto REDOSAVE;
                        }

                        ConvertErrorCode(result);
                        // strError = result.ErrorString;


                        if (result.ErrorCode == ErrorCodeValue.TimestampMismatch)
                        {
                            this.ErrorCode = ChannelErrorCode.TimestampMismatch;
                            strError = "ʱ�����ƥ�䡣\r\n\r\n�����ʱ��� [" + ByteArray.GetHexTimeStampString(baInputTimeStamp) + "] ��Ӧ��ʱ��� [" + ByteArray.GetHexTimeStampString(output_timestamp/*baOutputTimeStamp*/) + "]";
                            return -1;
                        }

                        // ԭ��Convert....���������
                        return -1;
                    }

                    nStart += baChunk.Length;

                    if (nStart >= lTotalLength)
                        break;

                    Debug.Assert(strOutputPath != "", "outputpath����Ϊ��");

                    strPath = strOutputPath;	// �����һ�ε�strPath�а���'?'id, ������outputpath������ȷ����
                    baInputTimeStamp = output_timestamp;	//baOutputTimeStamp;

                }

                catch (Exception ex)
                {
                    /*
                    strError = ConvertWebError(ex);
                    return -1;
                     * */
                    int nRet = ConvertWebError(ex, out strError);
                    if (nRet == 0)
                        return -1;
                    goto REDO;
                }

            } // end of for

            // output_timestamp = baOutputTimeStamp;
            this.ClearRedoCount();
            return 0;
        }

        // ��װ��İ汾
                // ɾ�����ݿ��¼
        public long DoDeleteRes(string strPath,
            byte[] timestamp,
            out byte[] output_timestamp,
            out string strError)
        {
            return DoDeleteRes(strPath,
                timestamp,
                "",
                out output_timestamp,
                out strError);
        }

        // ɾ�����ݿ��¼
        public long DoDeleteRes(string strPath,
            byte[] timestamp,
            string strStyle,
            out byte[] output_timestamp,
            out string strError)
        {
            strError = "";
            output_timestamp = null;

            /*
            if (timestamp == null)
            {
                Debug.Assert(true, "timestamp��������Ϊnull");
                strError = "timestamp��������Ϊnull";
                return -1;
            }
             */

            /*
            int nOldTimeout = this.Timeout;
            this.Timeout = 20 * 60 * 1000;
             * */

            // byte[] baOutputTimeStamp = null;
            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginDeleteRes(strPath,
                    timestamp,
                    strStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndDeleteRes(
                    out output_timestamp,soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin(strPath,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDO;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;

                    if (result.ErrorCode == ErrorCodeValue.TimestampMismatch)
                    {
                        this.ErrorCode = ChannelErrorCode.TimestampMismatch;
                        Debug.Assert(output_timestamp != null, "WebService API DeleteRes() TimestampMismatchʱ���뷵�ؾ�ʱ��� ...");
                        strError = "ʱ�����ƥ�䡣\r\n\r\n�����ʱ��� [" + ByteArray.GetHexTimeStampString(timestamp) + "] ��Ӧ��ʱ��� [" + ByteArray.GetHexTimeStampString(output_timestamp) + "]";
                        return -1;
                    }

                    // ԭ�����������
                    return -1;
                }

            }
            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                // this.Timeout = nOldTimeout;

            }

            this.ClearRedoCount();
            return 0;
        }

        // ˢ�����ݿ��¼keys
        // parameters:
        //      strStyle    next prev outputpath forcedeleteoldkeys
        //                  forcedeleteoldkeys Ҫ�ڴ�����keysǰǿ��ɾ��һ�¾��е�keys? ���Ϊ��������ǿ��ɾ��ԭ�е�keys�����Ϊ������������̽�Ŵ����µ�keys������оɵ�keys���´��㴴����keys�غϣ��ǾͲ��ظ�����������ɵ�keys�в���û�б�ɾ����Ҳ����������
        //                          ���� һ�����ڵ�����¼�Ĵ��������� һ������Ԥ��ɾ��������keys����������Ժ���ѭ���ؽ�����ÿ����¼��������ʽ
        public long DoRebuildResKeys(string strPath,
            string strStyle,
            out string strOutputResPath,
            out string strError)
        {
            strError = "";
            strOutputResPath = "";

            /*
            int nOldTimeout = this.Timeout;
            this.Timeout = 20 * 60 * 1000;
            */

            REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginRebuildResKeys(strPath,
                    strStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndRebuildResKeys(
                    out strOutputResPath,soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin(strPath,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDO;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;

                    /*
                    if (result.ErrorCode == ErrorCodeValue.TimestampMismatch)
                    {
                        this.ErrorCode = ChannelErrorCode.TimestampMismatch;
                        Debug.Assert(output_timestamp != null, "WebService API RebuildResKeys() TimestampMismatchʱ���뷵�ؾ�ʱ��� ...");
                        strError = "ʱ�����ƥ�䡣\r\n\r\n�����ʱ��� [" + ByteArray.GetHexTimeStampString(timestamp) + "] ��Ӧ��ʱ��� [" + ByteArray.GetHexTimeStampString(output_timestamp) + "]";
                        return -1;
                    }*/

                    // ԭ�����������
                    return -1;
                }

            }

            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }

            finally
            {
                // this.Timeout = nOldTimeout;
            }
            this.ClearRedoCount();

            return 0;
        }

        // �����Դ�������ַ����汾�������ڻ������¼�塣
        // ��������������ļ�
        // return:
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetRes(
            CfgCache cache,
            string strPath,
            out string strResult,
            out string strMetaData,
            out byte[] baOutputTimeStamp,
            out string strOutputPath,
            out string strError)
        {

            return GetRes(
                cache,
                strPath,
                "content,data,metadata,timestamp,outputpath",
                out strResult,
                out strMetaData,
                out baOutputTimeStamp,
                out strOutputPath,
                out strError);
        }


        // �����Դ�������ַ����汾��Cache�汾��
        // return:
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetRes(
            CfgCache cache,
            string strPath,
            string strStyle,
            out string strResult,
            out string strMetaData,
            out byte[] baOutputTimeStamp,
            out string strOutputResPath,
            out string strError)
        {
            byte[] cached_timestamp = null;
            string strTimeStamp;
            string strLocalName;
            // bool bExistInCache = false;

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
            // return:
            //      -1  error
            //		0	not found
            //		1	found
            int nRet = cache.FindLocalFile(strFullPath,
                out strLocalName,
                out strTimeStamp);
            if (nRet == -1)
            {
                strResult = "";
                strMetaData = "";
                baOutputTimeStamp = null;
                strOutputResPath = "";
                strError = "CfgCache ��δ��ʼ��";
                return -1;
            }
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
            StringUtil.RemoveFromInList("metadata",
                true,
                ref strNewStyle);	// ��Ҫmetadata
            */
            StringUtil.RemoveFromInList("content,data,metadata",    // 2012/12/31 BUG ��ǰ�����˼���content
true,
ref strNewStyle);	// ��Ҫ�������metadata

            long lRet = GetRes(strPath,
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
            lRet = GetRes(strPath,
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

        // �����Դ�������ַ����汾�������ڻ������¼�塣
        // ��������������ļ�
        // return:
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetRes(string strPath,
            out string strResult,
            out string strMetaData,
            out byte[] baOutputTimeStamp,
            out string strOutputPath,
            out string strError)
        {

            return GetRes(strPath,
                "content,data,metadata,timestamp,outputpath",
                out strResult,
                out strMetaData,
                out baOutputTimeStamp,
                out strOutputPath,
                out strError);
        }

        // �����Դ��ԭʼ�汾��2007/5/27
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetRes(string strResPath,
            long lStart,
            int nLength,
            string strStyle,
            out byte[] baContent,
            out string strMetadata,
            out string strOutputResPath,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baContent = null;
            strMetadata = "";
            strError = "";
            strOutputResPath = "";
            baOutputTimestamp = null;

            // string strID = "";
            this.ErrorCode = ChannelErrorCode.None;
            this.ErrorInfo = "";

        REDO:
            try
            {

                // string strStyle = "content,data";
                IAsyncResult soapresult = this.ws.BeginGetRes(strResPath,
                    lStart,
                    nLength,
                    strStyle,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndGetRes(
                    out baContent,
                    // out strID,
                    out strMetadata,
                    out strOutputResPath,
                    out baOutputTimestamp,soapresult);

                // ���㲻�Ƿ���-1,Ҳ�����д�����ʹ�����Ϣ�ַ���
                ConvertErrorCode(result);
                strError = result.ErrorString;

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin(strResPath,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDO;
                    }
                    return -1;
                }


                this.ClearRedoCount();
                return result.Value;
            } // end try
            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            // return 0;
        }

        // �����Դ�������ַ����汾�������ڻ������¼�塣
        // return:
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetRes(string strPath,
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

            this.ErrorCode = ChannelErrorCode.None;
            this.ErrorInfo = "";

            // string id = "";
            byte[] baContent = null;

            long lStart = 0;
            int nPerLength = -1;

            byte[] baTotal = null;

            // 2012/3/28
            // List<byte> bytes = new List<byte>();

            if (StringUtil.IsInList("attachmentid", strStyle) == true)
            {
                strError = "Ŀǰ��֧�� attachmentid";
                return -1;
            }

            for (; ; )
            {
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                REDO:
                try
                {

                    // string strStyle = "content,data";
                    IAsyncResult soapresult = this.ws.BeginGetRes(strPath,
                        lStart,
                        nPerLength,
                        strStyle,
                        null,
                        null);

                    for (; ; )
                    {

                        /*
                        try 
                        {
                            Application.DoEvents();	// ���ý������Ȩ
                        }
                        catch
                        {
                        }
					

                        // System.Threading.Thread.Sleep(10);	// ����CPU��Դ���Ⱥķ�
                         */
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    // string strMetadata;
                    // string strOutputResPath;
                    Result result = this.ws.EndGetRes(
                        out baContent,
                        // out id,
                        out strMetaData,
                        out strOutputResPath,
                        out baOutputTimeStamp,soapresult);

                    // ���㲻�Ƿ���-1,Ҳ�����д�����ʹ�����Ϣ�ַ���
                    ConvertErrorCode(result);
                    strError = result.ErrorString;

                    if (result.Value == -1)
                    {
                        if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                        {
                            // return:
                            //		-1	error
                            //		0	login failed
                            //		1	login succeed
                            int nRet = this.UiLogin(strPath,
                                out strError);
                            if (nRet == -1 || nRet == 0)
                            {
                                return -1;
                            }

                            goto REDO;
                        }

                        /*
                        ConvertErrorCode(result);

                        strError = result.ErrorString;
                         */
                        return -1;
                    }



                    if (StringUtil.IsInList("data", strStyle) != true)
                        break;


                    baTotal = ByteArray.Add(baTotal, baContent);
                    // bytes.AddRange(baContent);

                    Debug.Assert(baContent.Length <= result.Value, "ÿ�η��صİ��ߴ�[" + Convert.ToString(baContent.Length) + "]Ӧ��С��result.Value[" + Convert.ToString(result.Value) + "]");

                    lStart += baContent.Length;
                    if (lStart >= result.Value)
                        break;	// ����

                    baContent = null;
                } // end try
                catch (Exception ex)
                {
                    /*
                    strError = ConvertWebError(ex);
                    return -1;
                     * */
                    int nRet = ConvertWebError(ex, out strError);
                    if (nRet == 0)
                        return -1;
                    goto REDO;
                }

            } // end of for


            this.ClearRedoCount();

            if (StringUtil.IsInList("data", strStyle) != true)
                return 0;

#if NO
            byte [] baTemp = new byte[bytes.Count];
            bytes.CopyTo(baTemp);

            strResult = Encoding.UTF8.GetString(baTemp);
#endif

            // ת�����ַ���
            strResult = ByteArray.ToString(baTotal/*,
				Encoding.UTF8*/
                               );	// �������Զ�ʶ����뷽ʽ

            return 0;
        }


        // �����Դ��д���ļ��İ汾���ر������ڻ����Դ��Ҳ�����ڻ������¼�塣
        // parameters:
        //		strOutputFileName	����ļ���������Ϊnull���������ǰ�ļ��Ѿ�����, �ᱻ���ǡ�
        // return:
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetRes(string strPath,
            string strOutputFileName,
            DigitalPlatform.Stop stop,
            out string strMetaData,
            out byte[] baOutputTimeStamp,
            out string strOutputPath,
            out string strError)
        {
            FileStream fileTarget = null;

            string strStyle = "content,data,metadata,timestamp,outputpath";
            // string strStyle = "attachment,data,metadata,timestamp,outputpath";

            if (strOutputFileName != null)
                fileTarget = File.Create(strOutputFileName);
            else
            {
                strStyle = "metadata,timestamp,outputpath";
            }

            try
            {

                return GetRes(strPath,
                    fileTarget,
                    stop,
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

        /* ���ӻ��ӵģ�������ȥ���ˣ���Ϊ���Դ�WebPageStop��
                public long GetRes(string strPath,
                    Stream targetStream,
                    out string strMetaData,
                    out byte[] baOutputTimeStamp,
                    out string strOutputPath,
                    out string strError)
                {
                    string strStyle = "content,data,metadata,timestamp,outputpath";

                    return GetRes(strPath,
                        targetStream,
                        null, // stop
                        strStyle,
                        null, // baInputTimestamp
                        out strMetaData,
                        out baOutputTimeStamp,
                        out strOutputPath,
                        out strError);
                }
        */

        // ��װ�汾
        // ����һ��flushOutputMethod����
        public long GetRes(string strPath,
    Stream fileTarget,
    DigitalPlatform.Stop stop,
    string strStyleParam,
    byte[] input_timestamp,
    out string strMetaData,
    out byte[] baOutputTimeStamp,
    out string strOutputPath,
    out string strError)
        {
            return GetRes(strPath,
            fileTarget,
            null,
            stop,
            strStyleParam,
            input_timestamp,
            out strMetaData,
            out baOutputTimeStamp,
            out strOutputPath,
            out strError);
        }

        // �����Դ��д���ļ��İ汾���ر������ڻ����Դ��Ҳ�����ڻ������¼�塣
        // parameters:
        //		fileTarget	�ļ���ע���ڵ��ú���ǰ�ʵ������ļ�ָ��λ�á�����ֻ���ڵ�ǰλ�ÿ�ʼ���д��д��ǰ���������ı��ļ�ָ�롣
        //		strStyleParam	һ������Ϊ"content,data,metadata,timestamp,outputpath";
        //		input_timestamp	��!=null���򱾺�����ѵ�һ�����ص�timestamp�ͱ��������ݱȽϣ��������ȣ��򱨴�
        // return:
        //		-1	�����������ԭ����this.ErrorCode�С�this.ErrorInfo���г�����Ϣ��
        //		0	�ɹ�
        public long GetRes(string strPath,
            Stream fileTarget,
			FlushOutput flushOutputMethod,
            DigitalPlatform.Stop stop,
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

            this.ErrorCode = ChannelErrorCode.None;
            this.ErrorInfo = "";

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
                DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                if (stop != null && stop.State != 0)
                {
                    strError = "�û��ж�";
                    return -1;
                }

                REDO:
                try
                {

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

                    IAsyncResult soapresult = this.ws.BeginGetRes(strPath,
                        fileTarget == null ? 0 : lStart,
                        fileTarget == null ? 0 : nPerLength,
                        strStyle,
                        null,
                        null);

                    for (; ; )
                    {

                        /*
                        try 
                        {
                            Application.DoEvents();	// ���ý������Ȩ
                        }
                        catch
                        {
                        }
					

                        // System.Threading.Thread.Sleep(10);	// ����CPU��Դ���Ⱥķ�
                         */
                        DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���

                        bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                        if (bRet == true)
                            break;
                    }
                    if (this.m_ws == null)
                    {
                        strError = "�û��ж�";
                        this.ErrorCode = ChannelErrorCode.RequestCanceled;
                        return -1;
                    }
                    // string strOutputResPath;
                    Result result = this.ws.EndGetRes(
                        out baContent,
                        // out id,
                        out strMetaData,
                        out strOutputPath,
                        out timestamp,soapresult);

                    // ���㲻�Ƿ���-1,Ҳ�����д�����ʹ�����Ϣ�ַ���
                    ConvertErrorCode(result);
                    strError = result.ErrorString;

                    if (result.Value == -1)
                    {
                        if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                        {
                            // return:
                            //		-1	error
                            //		0	login failed
                            //		1	login succeed
                            int nRet = this.UiLogin(strPath,
                                out strError);
                            if (nRet == -1 || nRet == 0)
                            {
                                return -1;
                            }

                            goto REDO;
                        }

                        /*
                        ConvertErrorCode(result);
                        strError = result.ErrorString;
                         */
                        return -1;
                    }

                    if (bHasMetadataStyle == true)
                    {
                        StringUtil.RemoveFromInList("metadata",
                            true,
                            ref strStyle);
                        bHasMetadataStyle = false;
                    }


                    lTotalLength = result.Value;


                    if (StringUtil.IsInList("timestamp", strStyle) == true
                        /*
                        && lTotalLength > 0
                         * */ )    // 2012/1/11
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
                        /*
						Attachment attachment = ws.ResponseSoapContext.Attachments[id];
						if (attachment == null)
						{
							strError = "idΪ '" +id+ "' ��attachment��WebService��Ӧ��û���ҵ�...";
							return -1;
						}
						StreamUtil.DumpStream(attachment.Stream, fileTarget);
						nStart += (int)attachment.Stream.Length;

						Debug.Assert(attachment.Stream.Length <= result.Value, "ÿ�η��صİ��ߴ�["+Convert.ToString(attachment.Stream.Length)+"]Ӧ��С��result.Value["+Convert.ToString(result.Value)+"]");
                         */

                    }
                    else
                    {
                        Debug.Assert(StringUtil.IsInList("content", strStyle) == true,
                            "����attachment��񣬾�Ӧ��content���");

                        Debug.Assert(baContent != null, "���ص�baContent����Ϊnull");
                        Debug.Assert(baContent.Length <= result.Value, "ÿ�η��صİ��ߴ�[" + Convert.ToString(baContent.Length) + "]Ӧ��С��result.Value[" + Convert.ToString(result.Value) + "]");

                        fileTarget.Write(baContent, 0, baContent.Length);
                        if (flushOutputMethod != null)
                        {
                            if (flushOutputMethod() == false)
                            {
                                strError = "FlushOutputMethod()�û��ж�";
                                return -1;
                            }
                        } 
                        lStart += baContent.Length;
                    }

                    if (lStart >= result.Value)
                        break;	// ����

                } // end try


                catch (Exception ex)
                {
                    /*
                    strError = ConvertWebError(ex);
                    return -1;
                     * */
                    int nRet = ConvertWebError(ex, out strError);
                    if (nRet == 0)
                        return -1;
                    goto REDO;
                }

            } // end of for

            baOutputTimeStamp = timestamp;
            this.ClearRedoCount();
            return 0;
        }

        string BuildMetadataXml(string strMime,
            string strLocalPath,
            string strLastModifyTime)
        {
            // string strMetadata = "<file mimetype='" + strMime + "' localpath='" + strLocalPath + "'/>";
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<file />");
            DomUtil.SetAttr(dom.DocumentElement, "mimetype", strMime);
            DomUtil.SetAttr(dom.DocumentElement, "localpath", strLocalPath);
            DomUtil.SetAttr(dom.DocumentElement, "lastmodifytime", strLastModifyTime);

            return dom.OuterXml;
        }

        // ������Դ��¼
        // parameters:
        //		strPath	��ʽ: ����/��¼��/object/����xpath
        //		bTailHint	�Ƿ�Ϊ���һ��д�����������һ����ʾ�����������������ݴ˲���Ϊ���һ��д�������������ĳ�ʱʱ�䡣
        //					�ٶ���ʱ������Դ�ߴ�ܴ���Ȼÿ�ξֲ�д���ʱ���࣬�������һ��д����Ϊ������Ҫִ��������Դת��
        //					�Ĳ�����API�ŷ��أ����Կ��ܻ�ķ�����20���������ĳ�ʱ�䣬����WebService API��ʱʧ�ܡ�
        //					��������һ����ʾ����(������Ҳ������һ��Ҫ��ʲô����)����������߲�������ĺ��壬����ʹ��false��
        public long DoSaveResObject(string strPath,
            string strObjectFileName,  // ���ӻ���,�ò��������Ŷ������ݵ��ļ���
            string strLocalPath,       // �ò����������ļ���,��ʱ����strObjectFileName��ͬ
            string strMime,
            string strLastModifyTime,   // 2007/12/13
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

            string strOutputPath = "";


            // int nOldTimeout = -1;
            if (bTailHint == true)
            {
                /*
                nOldTimeout = this.Timeout;
                this.Timeout = 20 * 60 * 1000;
                 * */
            }

        REDO:
            try
            {
            REDOSAVE:
                // string strMetadata = "<file mimetype='" + strMime + "' localpath='" + strLocalPath + "'/>";
                string strMetadata = BuildMetadataXml(strMime,
                    strLocalPath,
                    strLastModifyTime);

                IAsyncResult soapresult = this.ws.BeginWriteRes(strPath,
                    strRange,
                    fi.Length,	// �����������ߴ磬���Ǳ���chunk�ĳߴ硣��Ϊ��������Ȼ���Դ�baChunk�п�����ߴ磬������ר����һ��������ʾ����ߴ���
                    baTotal,
                    // null,	// attachmentid
                    strMetadata,
                    "",	// style
                    timestamp,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndWriteRes(
                    out strOutputPath,
                    out output_timestamp,soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin(strPath,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDOSAVE;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;

                    if (result.ErrorCode == ErrorCodeValue.TimestampMismatch)
                    {
                        this.ErrorCode = ChannelErrorCode.TimestampMismatch;
                        strError = "ʱ�����ƥ�䡣\r\n\r\n�����ʱ��� [" + ByteArray.GetHexTimeStampString(timestamp) + "] ��Ӧ��ʱ��� [" + ByteArray.GetHexTimeStampString(output_timestamp) + "]";
                        return -1;
                    }

                    // ԭ�����������
                    return -1;
                }
            }
            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            finally
            {
                /*
                if (bTailHint == true)
                    this.Timeout = nOldTimeout;
                 * */

            }

        this.ClearRedoCount();
            return 0;
        }


        // ������Դ��¼
        // parameters:
        //		strPath	��ʽ: ����/��¼��/object/����xpath
        //		strRange	�����뷢�͸��������ľֲ��������������ⲿ�����ݸ��Ƶ��ڴ�byte[]�ṹ�У�
        //					��ˣ������߱��뿼���ú��ʵĳߴ磬���ⳬ���ڴ漫��������̱�ɱ����
        //		bTailHint	�Ƿ�Ϊ���һ��д�����������һ����ʾ�����������������ݴ˲���Ϊ���һ��д�������������ĳ�ʱʱ�䡣
        //					�ٶ���ʱ������Դ�ߴ�ܴ���Ȼÿ�ξֲ�д���ʱ���࣬�������һ��д����Ϊ������Ҫִ��������Դת��
        //					�Ĳ�����API�ŷ��أ����Կ��ܻ�ķ�����20���������ĳ�ʱ�䣬����WebService API��ʱʧ�ܡ�
        //					��������һ����ʾ����(������Ҳ������һ��Ҫ��ʲô����)����������߲�������ĺ��壬����ʹ��false��
        public long DoSaveResObject(string strPath,
            Stream file,
            long lTotalLength,
            string strStyle,	// 2005/11/4
            string strMetadata,
            string strRange,
            bool bTailHint,
            byte[] timestamp,
            out byte[] output_timestamp,
            out string strOutputPath,
            out string strError)
        {
            //string strLocalPath,       // �ò����������ļ���,��ʱ����strObjectFileName��ͬ
            //string strMime;

            this.ErrorCode = ChannelErrorCode.None;
            strError = "";
            output_timestamp = null;
            strOutputPath = "";

            byte[] baTotal = null;

            if (file != null)
            {

                if (file.Position + lTotalLength > file.Length)
                {
                    strError = "�ļ��ӵ�ǰλ�� " + Convert.ToString(file.Position) + " ��ʼ��ĩβ���Ȳ��� " + Convert.ToString(lTotalLength);
                    return -1;
                }

                long lRet = RangeList.CopyFragment(
                    file,
                    lTotalLength,
                    strRange,
                    out baTotal,
                    out strError);
                if (lRet == -1)
                    return -1;
            }
            else
            {
                baTotal = new byte[0];	// ����һ��ȱ����Ӧ�����Ϊnull
            }


            // int nOldTimeout = -1;
            if (bTailHint == true)
            {
                /*
                nOldTimeout = this.Timeout;
                this.Timeout = 20 * 60 * 1000;
                 * */
            }

        REDO:
            try
            {
            REDOSAVE:
                // string strMetadata = "<file mimetype='"+strMime+"' localpath='" + strLocalPath + "'/>";
                IAsyncResult soapresult = this.ws.BeginWriteRes(strPath,
                    strRange,
                    lTotalLength,	// �����������ߴ磬���Ǳ���chunk�ĳߴ硣��Ϊ��������Ȼ���Դ�baChunk�п�����ߴ磬������ר����һ��������ʾ����ߴ���
                    baTotal,
                    // null,	// attachmentid
                    strMetadata,
                    strStyle,	// style
                    timestamp,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }
                if (this.m_ws == null)
                {
                    strError = "�û��ж�";
                    this.ErrorCode = ChannelErrorCode.RequestCanceled;
                    return -1;
                }
                Result result = this.ws.EndWriteRes(
                    out strOutputPath,
                    out output_timestamp, soapresult);

                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCodeValue.NotLogin
                            && this.Container != null)
                    {
                        // return:
                        //		-1	error
                        //		0	login failed
                        //		1	login succeed
                        int nRet = this.UiLogin(strPath,
                            out strError);
                        if (nRet == -1 || nRet == 0)
                        {
                            return -1;
                        }

                        goto REDOSAVE;
                    }

                    ConvertErrorCode(result);
                    strError = result.ErrorString;

                    if (result.ErrorCode == ErrorCodeValue.TimestampMismatch)
                    {
                        this.ErrorCode = ChannelErrorCode.TimestampMismatch;
                        // output_timestamp �ڳ�������£�Ҳ�᷵�ط�������ϣ����ʱ���
                        strError = "ʱ�����ƥ�䡣\r\n\r\n�����ʱ��� [" + ByteArray.GetHexTimeStampString(timestamp) + "] ��Ӧ��ʱ��� [" + ByteArray.GetHexTimeStampString(output_timestamp) + "]";
                        return -1;
                    }

                    // ԭ�����������
                    return -1;
                }
            }



            catch (Exception ex)
            {
                /*
                strError = ConvertWebError(ex);
                return -1;
                 * */
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }

            finally
            {
                /*
                if (bTailHint == true)
                    this.Timeout = nOldTimeout;
                 * */

            }

            this.ClearRedoCount();
            return 0;
        }

        public void DoStop()
        {
            IAsyncResult result = this.ws.BeginStop(
                null,
                null);
        }

        public int DoTest(string strText)
        {
                IAsyncResult soapresult = this.ws.BeginDoTest(
                    strText,
                    null,
                    null);

                for (; ; )
                {
                    DoIdle(); // ���ÿ���Ȩ������CPU��Դ�ķѹ���
                    bool bRet = soapresult.AsyncWaitHandle.WaitOne(100, false);
                    if (bRet == true)
                        break;
                }

                try
                {
                    return this.ws.EndDoTest(soapresult);
                }
                catch (WebException ex)
                {
                    return -1;
                }


        }

    }


    public class MyValidator : X509CertificateValidator
    {
        public override void Validate(X509Certificate2 certificate)
        {
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

            bool bRet = IdentityVerifier.CreateDefault().CheckAccess(identity, authContext);
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

#if NO
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

    class CustomIdentityVerifier : IdentityVerifier
    {
        public override bool CheckAccess(EndpointIdentity identity, AuthorizationContext authContext)
        {
            bool returnvalue = false;

            foreach (ClaimSet claimset in authContext.ClaimSets)
            {
                foreach (Claim claim in claimset)
                {
                    if (claim.ClaimType == "http://schemas.microsoft.com/ws/2005/05/identity/claims/x500distinguishedname")
                    {
                        X500DistinguishedName name = (X500DistinguishedName)claim.Resource;
                        if (name.Name.Contains(((OrgEndpointIdentity)identity).OrganizationClaim))
                        {
                            //Console.WriteLine("Claim Type: {0}", claim.ClaimType);
                            //Console.WriteLine("Right: {0}", claim.Right);
                            //Console.WriteLine("Resource: {0}", claim.Resource);
                            //Console.WriteLine();
                            returnvalue = true;
                        }
                    }
                }

            }
            // return returnvalue;

            return true;
        }

        public override bool TryGetIdentity(EndpointAddress reference, out EndpointIdentity identity)
        {
            return IdentityVerifier.CreateDefault().TryGetIdentity(reference, out identity);
        }
    }

#endif

}
