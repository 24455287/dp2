using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Serialization;

using DigitalPlatform;
using DigitalPlatform.Z3950;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Marc;
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2ZServer
{
    public class Session : IDisposable
    {
        // private Socket m_clientSocket = null;    // Referance to client Socket.
        TcpClient client = null;

        private Service m_service = null;    // 
        private string m_SessionID = "";      // Holds session ID.
        private DateTime m_SessionStartTime;    // Session������ʱ��
        private DateTime m_ActivateTime;    // ���һ��ʹ�ù���ʱ��

        public DateTime ActivateTime
        {
            get
            {
                return this.m_ActivateTime;
            }
        }

        LibraryChannel Channel = new LibraryChannel(); 

        string strGroupId = "";
        string strUserName = "";
        string strPassword = "";


        // �����ʵı��뷽ʽ
        Encoding SearchTermEncoding = Encoding.GetEncoding(936);    // ȱʡΪGB2312���뷽ʽ
        // MARC��¼�ı��뷽ʽ
        Encoding MarcRecordEncoding = Encoding.GetEncoding(936);    // ȱʡΪGB2312���뷽ʽ

        bool m_bInitialized = false;    // �Ƿ�Initial��ʼ���ɹ������Ϊfalse����Initial()����������Ҫ���ܾ���
        long m_lPreferredMessageSize = 500 * 1024;
        long m_lExceptionalRecordSize = 500 * 1024;

        const long MaxPreferredMessageSize = 1024 * 1024;
        const long MaxExceptionalRecordSize = 1024 * 1024;

        public void Dispose()
        {
            if (this.client != null)
            {
                try
                {
                    this.client.Close();
                }
                catch
                {
                }
                this.client = null;
            }

            if (this.Channel != null)
            {
                this.Channel.Close();
                this.Channel = null;
            }
        }

        internal Session(TcpClient client,
			Service server,
			string sessionID)
		{			
			this.client    = client;
			m_service     = server;
			m_SessionID        = sessionID;
			m_SessionStartTime = DateTime.Now;
            m_ActivateTime = m_SessionStartTime;

            this.Channel.Url = m_service.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);
		}

        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            if (e.FirstTry == true)
            {
                e.UserName = this.strUserName;
                e.Password = this.strPassword;
                e.Parameters = "location=z39.50 server, type=worker";
                /*
                e.IsReader = false;
                e.Location = "z39.50 server";
                 * */
                if (String.IsNullOrEmpty(e.UserName) == true)
                {
                    e.ErrorInfo = "û��ָ���û������޷��Զ���¼";
                    e.Failed = true;
                    return;
                }

                return;
            }

            e.ErrorInfo = "first tryʧ�ܺ��޷��Զ���¼";
            e.Failed = true;
            return;
        }

        public string SessionID
        {
            get
            {
                return m_SessionID;
            }
        }


        // ���������
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

            // long lIdleCount = 0;

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


        // ������Ӧ��
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
                strError = "client��δ��ʼ��";
                return -1;
            }

            // DoIdle();

            if (this.client == null)
            {
                strError = "�û��ж�";
                return -1;
            }

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

            }
        }

        public void CloseSocket()
        {
            if (client != null)
            {

                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Close();
                }
                catch { }
                try
                {
                    client.Close();
                }
                catch { }

                client = null;
            }

            // this.m_bInitialized = false;
        }
		
		/// <summary>
		/// Session�����ֻ�
		/// </summary>
		public void Processing()
		{
            int nRet = 0;			
            string strError = "";

			try
			{
                byte [] baPackage = null;
                int nLen = 0;
                byte[] baResponsePackage = null;

                for (; ; )
                {
                    m_ActivateTime = DateTime.Now;
                    // ����ǰ������
                    nRet = RecvTcpPackage(out baPackage,
                        out nLen,
                        out strError);
                    if (nRet == -1)
                        goto ERROR_NOT_LOG;

                    // ���������
                    BerTree tree1 = new BerTree();
                    int nTotlen = 0;

                    tree1.m_RootNode.BuildPartTree(baPackage,
                        0,
                        baPackage.Length,
                        out nTotlen);

                    BerNode root = tree1.GetAPDuRoot();

                    switch (root.m_uTag)
                    {
                        case BerTree.z3950_initRequest:
                            {
                                InitRequestInfo info = null;
                                string strDebugInfo = "";
                                nRet = Decode_InitRequest(
                                    root,
                                    out info,
                                    out strDebugInfo,
                                    out strError);
                                if (nRet == -1)
                                    goto ERROR1;

                                // ������groupid����ʾ�ַ�����Ϣ

                                InitResponseInfo response_info = new InitResponseInfo();

                                // �ж�info�е���Ϣ�������Ƿ����Init����

                                if (String.IsNullOrEmpty(info.m_strID) == true)
                                {
                                    // �������������������¼
                                    if (String.IsNullOrEmpty(this.m_service.AnonymousUserName) == false)
                                    {
                                        info.m_strID = this.m_service.AnonymousUserName;
                                        info.m_strPassword = this.m_service.AnonymousPassword;
                                    }
                                    else
                                    {
                                        response_info.m_nResult = 0;
                                        this.m_bInitialized = false;

                                        SetInitResponseUserInfo(response_info,
                                            "", // string strOID,
                                            0,  // long lErrorCode,
                                            "������������¼");
                                        goto DO_RESPONSE;
                                    }
                                }

                                // ���е�¼
                                // return:
                                //      -1  error
                                //      0   ��¼δ�ɹ�
                                //      1   ��¼�ɹ�
                                nRet = DoLogin(info.m_strGroupID,
                                    info.m_strID,
                                    info.m_strPassword,
                                    out strError);
                                if (nRet == -1 || nRet == 0)
                                {
                                    response_info.m_nResult = 0;
                                    this.m_bInitialized = false;

                                    SetInitResponseUserInfo(response_info,
                                        "", // string strOID,
                                        0,  // long lErrorCode,
                                        strError);
                                }
                                else
                                {
                                    response_info.m_nResult = 1;
                                    this.m_bInitialized = true;
                                }

                            DO_RESPONSE:
                                // ���response_info�������ṹ
                                response_info.m_strReferenceId = info.m_strReferenceId;  // .m_strID; BUG!!! 2007/11/2

                                if (info.m_lPreferredMessageSize != 0)
                                    this.m_lPreferredMessageSize = info.m_lPreferredMessageSize;
                                // ����
                                if (this.m_lPreferredMessageSize > MaxPreferredMessageSize)
                                    this.m_lPreferredMessageSize = MaxPreferredMessageSize;
                                response_info.m_lPreferredMessageSize = this.m_lPreferredMessageSize;

                                if (info.m_lExceptionalRecordSize != 0)
                                    this.m_lExceptionalRecordSize = info.m_lExceptionalRecordSize;
                                // ����
                                if (this.m_lExceptionalRecordSize > MaxExceptionalRecordSize)
                                    this.m_lExceptionalRecordSize = MaxExceptionalRecordSize;
                                response_info.m_lExceptionalRecordSize = this.m_lExceptionalRecordSize;

                                response_info.m_strImplementationId = "Digital Platform";
                                response_info.m_strImplementationName = "dp2ZServer";
                                response_info.m_strImplementationVersion = "1.0";

                                if (info.m_charNego != null)
                                {
                                    /* option
* 
search                 (0), 
present                (1), 
delSet                 (2),
resourceReport         (3),
triggerResourceCtrl    (4),
resourceCtrl           (5), 
accessCtrl             (6),
scan                   (7),
sort                   (8), 
--                     (9) (reserved)
extendedServices       (10),
level-1Segmentation    (11),
level-2Segmentation    (12),
concurrentOperations   (13),
namedResultSets        (14)
15 Encapsulation  Z39.50-1995 Amendment 3: Z39.50 Encapsulation 
16 resultCount parameter in Sort Response  See Note 8 Z39.50-1995 Amendment 1: Add resultCount parameter to Sort Response  
17 Negotiation Model  See Note 9 Model for Z39.50 Negotiation During Initialization  
18 Duplicate Detection See Note 1  Z39.50 Duplicate Detection Service  
19 Query type 104 
* }
*/
                                    response_info.m_strOptions = "yynnnnnnnnnnnnn";

                                    if (info.m_charNego.EncodingLevelOID == CharsetNeogatiation.Utf8OID)
                                    {
                                        BerTree.SetBit(ref response_info.m_strOptions,
                                            17,
                                            true);
                                        response_info.m_charNego = info.m_charNego;
                                        this.SearchTermEncoding = Encoding.UTF8;
                                        if (info.m_charNego.RecordsInSelectedCharsets != -1)
                                        {
                                            response_info.m_charNego.RecordsInSelectedCharsets = info.m_charNego.RecordsInSelectedCharsets; // ����ǰ�˵�����
                                            if (response_info.m_charNego.RecordsInSelectedCharsets == 1)
                                                this.MarcRecordEncoding = Encoding.UTF8;
                                        }
                                    }
                                }
                                else
                                {
                                    response_info.m_strOptions = "yynnnnnnnnnnnnn";
                                }



                                BerTree tree = new BerTree();
                                nRet = Encode_InitialResponse(response_info,
                                    out baResponsePackage);
                                if (nRet == -1)
                                    goto ERROR1;


                            }
                            break;
                        case BerTree.z3950_searchRequest:
                            {
                                SearchRequestInfo info = null;
                                // ����Search�����
                                nRet = Decode_SearchRequest(
                                    root,
                                    out info,
                                    out strError);
                                if (nRet == -1)
                                    goto ERROR1;

                                if (m_bInitialized == false)
                                    goto ERROR_NOT_LOG;


                                // ����Search��Ӧ��
                                nRet = Encode_SearchResponse(info,
                                    out baResponsePackage,
                                    out strError);
                                if (nRet == -1)
                                    goto ERROR1;

                            }
                            break;

                        case BerTree.z3950_presentRequest:
                            {
                                PresentRequestInfo info = null;
                                // ����Search�����
                                nRet = Decode_PresentRequest(
                                    root,
                                    out info,
                                    out strError);
                                if (nRet == -1)
                                    goto ERROR1;

                                if (m_bInitialized == false)
                                    goto ERROR_NOT_LOG;

                                // ����Present��Ӧ��
                                nRet = Encode_PresentResponse(info,
                                    out baResponsePackage);
                                if (nRet == -1)
                                    goto ERROR1;

                            }
                            break;
                        default:
                            break;
                    }


                    // ������Ӧ��
                    // return:
                    //      -1  ����
                    //      0   ��ȷ����
                    //      1   ����ǰ������������δ���������
                    nRet = SendTcpPackage(baResponsePackage,
                        baResponsePackage.Length,
                        out strError);
                    if (nRet == -1)
                        goto ERROR_NOT_LOG;
                }

			}
			catch(ThreadInterruptedException)
			{
				// string dummy = e.Message;     // Needed for to remove compile warning
			}
			catch(Exception x)
			{
                /*
				if(m_clientSocket.Connected)
				{
					// SendData("421 Service not available, closing transmission channel\r\n");

					// m_pSMTP_Server.WriteErrorLog(x.Message);
				}
				else
				{

				}
                 * */
                strError = "Session Processing()�����쳣: " + ExceptionUtil.GetDebugText(x);
                goto ERROR1;
			}
			finally
			{				
				m_service.RemoveSession(this.SessionID);
                this.CloseSocket();
			}
            return;
        ERROR1:
            // ��strErrorд����־
            this.m_service.Log.WriteEntry(strError, EventLogEntryType.Error);
            return;
        ERROR_NOT_LOG:
            // ��д����־
            return;
		}

        void SetInitResponseUserInfo(InitResponseInfo response_info,
            string strOID,
            long lErrorCode,
            string strErrorMessage)
        {
            if (response_info.UserInfoField == null)
                response_info.UserInfoField = new External();

            response_info.UserInfoField.m_strDirectRefenerce = strOID;
            response_info.UserInfoField.m_lIndirectReference = lErrorCode;
            if (String.IsNullOrEmpty(strErrorMessage) == false)
            {
                response_info.UserInfoField.m_octectAligned = Encoding.UTF8.GetBytes(strErrorMessage);
            }
        }

        // ���е�¼
        // return:
        //      -1  error
        //      0   ��¼δ�ɹ�
        //      1   ��¼�ɹ�
        int DoLogin(string strGroupId,
            string strUserName,
            string strPassword,
            out string strError)
        {
            strError = "";

            // return:
            //      -1  error
            //      0   ��¼δ�ɹ�
            //      1   ��¼�ɹ�
            long lRet = this.Channel.Login(strUserName,
                strPassword,
                "location=z39.50 server,type=worker",
                /*
                "z39.50 server",    // string strLocation,
                false,  // bReader,
                 * */
                out strError);
            if (lRet == -1)
                return -1;

            // �������������Ժ�ʹ��
            this.strGroupId = strGroupId;
            this.strUserName = strUserName;
            this.strPassword = strPassword;
            

            return (int)lRet;
        }


        #region BER������

        // ����Initial�����
        public static int Decode_InitRequest(
            BerNode root,
            out InitRequestInfo info,
            out string strDebugInfo,
            out string strError)
        {
            strError = "";
            strDebugInfo = "";
            info = new InitRequestInfo();

            Debug.Assert(root != null, "");

            if (root.m_uTag != BerTree.z3950_initRequest)
            {
                strError = "root tag is not z3950_initRequest";
                return -1;
            }

            for (int i = 0; i < root.ChildrenCollection.Count; i++)
            {
                BerNode node = root.ChildrenCollection[i];
                switch (node.m_uTag)
                {
                    case BerTree.z3950_ReferenceId:
                        info.m_strReferenceId = node.GetCharNodeData();
                        strDebugInfo += "ReferenceID='" + node.GetCharNodeData() + "'\r\n";
                        break;
                    case BerTree.z3950_ProtocolVersion:
                        info.m_strProtocolVersion = node.GetBitstringNodeData();
                        strDebugInfo += "ProtocolVersion='" + node.GetBitstringNodeData() + "'\r\n";
                        break;
                    case BerTree.z3950_Options:
                        info.m_strOptions = node.GetBitstringNodeData();
                        strDebugInfo += "Options='" + node.GetBitstringNodeData() + "'\r\n";
                        break;
                    case BerTree.z3950_PreferredMessageSize:
                        info.m_lPreferredMessageSize = node.GetIntegerNodeData();
                        strDebugInfo += "PreferredMessageSize='" + node.GetIntegerNodeData() + "'\r\n";
                        break;
                    case BerTree.z3950_ExceptionalRecordSize:
                        info.m_lExceptionalRecordSize = node.GetIntegerNodeData();
                        strDebugInfo += "ExceptionalRecordSize='" + node.GetIntegerNodeData() + "'\r\n";
                        break;
                    case BerTree.z3950_idAuthentication:
                        {
                            string strGroupId = "";
                            string strUserId = "";
                            string strPassword = "";
                            int nAuthentType = 0;

                            int nRet = DecodeAuthentication(
                                node,
                                out strGroupId,
                                out strUserId,
                                out strPassword,
                                out nAuthentType,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            info.m_nAuthenticationMethod = nAuthentType;	// 0: open 1:idPass
                            info.m_strGroupID = strGroupId;
                            info.m_strID = strUserId;
                            info.m_strPassword = strPassword;

                            strDebugInfo += "idAuthentication struct occur\r\n";
                        }
                        break;
                    case BerTree.z3950_ImplementationId:
                        info.m_strImplementationId = node.GetCharNodeData();
                        strDebugInfo += "ImplementationId='" + node.GetCharNodeData() + "'\r\n";
                        break;
                    case BerTree.z3950_ImplementationName:
                        info.m_strImplementationName = node.GetCharNodeData();
                        strDebugInfo += "ImplementationName='" + node.GetCharNodeData() + "'\r\n";
                        break;
                    case BerTree.z3950_ImplementationVersion:
                        info.m_strImplementationVersion = node.GetCharNodeData();
                        strDebugInfo += "ImplementationVersion='" + node.GetCharNodeData() + "'\r\n";
                        break;
                    case BerTree.z3950_OtherInformationField:
                        info.m_charNego = new CharsetNeogatiation();
                        info.m_charNego.DecodeProposal(node);
                        break;
                    default:
                        strDebugInfo += "Undefined tag = [" + node.m_uTag.ToString() + "]\r\n";
                        break;
                }
            }

            return 0;
        }


        // ����Search�����
        public static int Decode_SearchRequest(
            BerNode root,
            out SearchRequestInfo info,
            out string strError)
        {
            strError = "";

            int nRet = 0;

            info = new SearchRequestInfo();

            Debug.Assert(root != null, "");

            if (root.m_uTag != BerTree.z3950_searchRequest)
            {
                strError = "root tag is not z3950_searchRequest";
                return -1;
            }

            for (int i = 0; i < root.ChildrenCollection.Count; i++)
            {
                BerNode node = root.ChildrenCollection[i];

                switch (node.m_uTag)
                {
                    case BerTree.z3950_ReferenceId: // 2
                        info.m_strReferenceId = node.GetCharNodeData();
                        break;
                    case BerTree.z3950_smallSetUpperBound: // 13 smallSetUpperBound (Integer)
                        info.m_lSmallSetUpperBound = node.GetIntegerNodeData();
                        break;
                    case BerTree.z3950_largeSetLowerBound: // 14 largeSetLowerBound  (Integer)         
                        info.m_lLargeSetLowerBound = node.GetIntegerNodeData();
                        break;
                    case BerTree.z3950_mediumSetPresentNumber: // 15 mediumSetPresentNumber (Integer)      
                        info.m_lMediumSetPresentNumber = node.GetIntegerNodeData();
                        break;
                    case BerTree.z3950_replaceIndicator: // 16 replaceIndicator, (boolean)
                        info.m_lReplaceIndicator = node.GetIntegerNodeData();
                        break;
                    case BerTree.z3950_resultSetName: // 17 resultSetName (string)
                        info.m_strResultSetName = node.GetCharNodeData();
                        break;
                    case BerTree.z3950_databaseNames: // 18 dbNames (sequence)
                        /*
                        // sequence is constructed, // have child with case = 105, (string)
                        m_saDBName.RemoveAll();
                        DecodeDBName(pNode, m_saDBName, m_bIsCharSetUTF8);
                         * */
                        {
                            List<string> dbnames = null;
                            nRet = DecodeDbnames(node,
                                out dbnames,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            info.m_dbnames = dbnames;
                        }
                        break;
                    case BerTree.z3950_query: // 21 query (query)
                        //			DecodeSearchQuery(pNode, m_strSQLWhere, pRPNStructureRoot);
                        {
                            BerNode rpn_root = GetRPNStructureRoot(node,
                                out strError);
                            if (rpn_root == null)
                                return -1;

                            info.m_rpnRoot = rpn_root;
                        }
                        break;
                    default:
                        break;
                }

            }

            return 0;
        }

        // ����(����) Search��Ӧ��
        int Encode_SearchResponse(SearchRequestInfo info,
            out byte[] baPackage,
            out string strError)
        {
            baPackage = null;
            int nRet = 0;
            long lRet = 0;
            strError = "";

            DiagFormat diag = null;

            BerTree tree = new BerTree();
            BerNode root = null;

            long lSearchStatus = 0; // 0 ʧ�ܣ�1�ɹ�
            long lHitCount = 0;

            string strQueryXml = "";
            // �����沨������м���

            // return:
            //      -1  error
            //      0   succeed
            nRet = BuildQueryXml(
                info.m_dbnames,
                info.m_rpnRoot,
                out strQueryXml,
                out strError);
            if (nRet == -1)
            {
                SetPresentDiagRecord(ref diag,
                    2,  // temporary system error
                    strError);
            }

            string strResultSetName = info.m_strResultSetName;
            if (String.IsNullOrEmpty(strResultSetName) == true)
                strResultSetName = "default";

            if (diag == null)
            {
                lRet = Channel.Search(null,
                    strQueryXml,
                    strResultSetName,
                    "", // strOutputStyle
                    out strError);

                /*
                // ���Լ���ʧ��
                lRet = -1;
                strError = "���Լ���ʧ��";
                 * */

                if (lRet == -1)
                {
                    lSearchStatus = 0;  // failed

                    SetPresentDiagRecord(ref diag,
                        2,  // temporary system error
                        strError);
                }
                else
                {
                    lHitCount = lRet;
                    lSearchStatus = 1;  // succeed
                }
            }


            root = tree.m_RootNode.NewChildConstructedNode(
                BerTree.z3950_searchResponse,
                BerNode.ASN1_CONTEXT);

            // reference id
            if (String.IsNullOrEmpty(info.m_strReferenceId) == false)
            {
                root.NewChildCharNode(BerTree.z3950_ReferenceId,
                    BerNode.ASN1_CONTEXT,
                    Encoding.UTF8.GetBytes(info.m_strReferenceId));
            }


            // resultCount
            root.NewChildIntegerNode(BerTree.z3950_resultCount, // 23
                BerNode.ASN1_CONTEXT,   // ASNI_PRIMITIVE BUG!!!!
                BitConverter.GetBytes((long)lHitCount));

            // numberOfRecordsReturned
            root.NewChildIntegerNode(BerTree.z3950_NumberOfRecordsReturned, // 24
                BerNode.ASN1_CONTEXT,   // ASNI_PRIMITIVE BUG!!!!
                BitConverter.GetBytes((long)0/*info.m_lNumberOfRecordReturned*/));    // 0

            // nextResultSetPosition
            root.NewChildIntegerNode(BerTree.z3950_NextResultSetPosition, // 25
                BerNode.ASN1_CONTEXT,   // ASNI_PRIMITIVE BUG!!!!
                BitConverter.GetBytes((long)1/*info.m_lNextResultSetPosition*/));


            // 2007/11/7 ԭ������λ�ò��ԣ������ƶ�������
            // bool
            // searchStatus
            root.NewChildIntegerNode(BerTree.z3950_searchStatus, // 22
                BerNode.ASN1_CONTEXT,   // ASNI_PRIMITIVE BUG!!!!
                BitConverter.GetBytes((long)lSearchStatus));

            // resultSetStatus OPTIONAL

            // 2007/11/7
            // presentStatus
            root.NewChildIntegerNode(BerTree.z3950_presentStatus, // 27
                BerNode.ASN1_CONTEXT,   // ASNI_PRIMITIVE BUG!!!!
                BitConverter.GetBytes((long)0));


            // ��ϼ�¼
            if (diag != null)
            {
                BerNode nodeDiagRoot = root.NewChildConstructedNode(BerTree.z3950_nonSurrogateDiagnostic,    // 130
                    BerNode.ASN1_CONTEXT);

                diag.BuildBer(nodeDiagRoot);
            }

            baPackage = null;
            root.EncodeBERPackage(ref baPackage);

            return 0;
        }

        // ����Present�����
        public static int Decode_PresentRequest(
            BerNode root,
            out PresentRequestInfo info,
            out string strError)
        {
            strError = "";

            int nRet = 0;

            info = new PresentRequestInfo();

            Debug.Assert(root != null, "");

            if (root.m_uTag != BerTree.z3950_presentRequest)
            {
                strError = "root tag is not z3950_presentRequest";
                return -1;
            }

            for (int i = 0; i < root.ChildrenCollection.Count; i++)
            {
                BerNode node = root.ChildrenCollection[i];

                switch (node.m_uTag)
                {
                    case BerTree.z3950_ReferenceId: // 2
                        info.m_strReferenceId = node.GetCharNodeData();
                        break;
                    case BerTree.z3950_ResultSetId: // 31 resultSetId (IntenationalString)
                        info.m_strResultSetID = node.GetCharNodeData();
                        break;
                    case BerTree.z3950_resultSetStartPoint: // 30 resultSetStartPoint  (Integer)         
                        info.m_lResultSetStartPoint = node.GetIntegerNodeData();
                        break;
                    case BerTree.z3950_numberOfRecordsRequested: // 29 numberOfRecordsRequested (Integer)      
                        info.m_lNumberOfRecordsRequested = node.GetIntegerNodeData();
                        break;
                    case BerTree.z3950_ElementSetNames: // 19 ElementSetNames (complicates)
                        {
                            List<string> elementset_names = null;
                            nRet = DecodeElementSetNames(node,
                                out elementset_names,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            info.m_elementSetNames = elementset_names;
                        }
                        break;
                    default:
                        break;
                }
            }

            return 0;
        }

        // ����present response�е���ϼ�¼
        static void SetPresentDiagRecord(ref DiagFormat diag,
            int nCondition,
            string strAddInfo)
        {
            if (diag == null)
            {
                diag = new DiagFormat();
                diag.m_strDiagSetID = "1.2.840.10003.4.1";
            }

            diag.m_nDiagCondition = nCondition;
            diag.m_strAddInfo = strAddInfo;
        }

        // ����(����) Present��Ӧ��
        int Encode_PresentResponse(PresentRequestInfo info,
            out byte[] baPackage)
        {
            baPackage = null;
            int nRet = 0;
            string strError = "";

            DiagFormat diag = null;
            
            BerTree tree = new BerTree();
            BerNode root = null;

            string strResultSetName = info.m_strResultSetID;
            if (String.IsNullOrEmpty(strResultSetName) == true)
                strResultSetName = "default";
            long lStart = info.m_lResultSetStartPoint - 1;
            long lNumber = info.m_lNumberOfRecordsRequested;

            long lPerCount = lNumber;

            long lHitCount = 0;

            List<string> paths = new List<string>();

            int nPresentStatus = 5; // failed

            // ��ȡ���������Ҫ���ֵļ�¼path
            long lOffset = lStart;
            int nCount = 0;
            for (; ; )
            {
                DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                long lRet = this.Channel.GetSearchResult(
                    null,   // stop,
                    strResultSetName,   // strResultSetName
                    lOffset,
                    lPerCount,
                    "id",
                    "zh",   // this.Lang,
                    out searchresults,
                    out strError);
                /*
                // ���Ի�ȡ�����ʧ�ܵ���������طǴ�����ϼ�¼
                lRet = -1;
                strError = "���Լ���������Ϣ��";
                 * */

                if (lRet == -1)
                {
                    SetPresentDiagRecord(ref diag,
                        2,  // temporary system error
                        strError);
                    break;
                }
                if (lRet == 0)
                {
                    // goto ERROR1 ?
                }

                lHitCount = lRet;   // ˳��õ����м�¼������

                // ת��
                for (int i = 0; i < searchresults.Length; i++)
                {
                    paths.Add(searchresults[i].Path);
                }

                lOffset += searchresults.Length;
                lPerCount -= searchresults.Length;
                nCount += searchresults.Length;

                if (lOffset >= lHitCount
                    || lPerCount <= 0
                    || nCount >= lNumber)
                {
                    // 
                    break;
                }
            }

            // TODO: ��Ҫע���������Ƿ��γɶ��diag��¼��V2������������V3��������
            if (lHitCount < info.m_lResultSetStartPoint
                && diag == null)
            {
                strError = "start����ֵ "
                    +info.m_lResultSetStartPoint
                    +" ����������м�¼���� "
                    + lHitCount;
                // return -1;  // �����ʾ����״̬��
                SetPresentDiagRecord(ref diag,
                    13,  // Present request out-of-range
                    strError);
            }

            int MAX_PRESENT_RECORD = 100;

            // ����ÿ�� present �ļ�¼����
            if (lNumber > MAX_PRESENT_RECORD)
                lNumber = MAX_PRESENT_RECORD;

            long nNextResultSetPosition = 0;

            // 
            if (lHitCount < (lStart - 1) + lNumber)
            {
                // �� present ���󣬵������Ե��� lNumber
                lNumber = lHitCount - (lStart - 1);
                nNextResultSetPosition = 0;
            }
            else
            {
                //
                nNextResultSetPosition = lStart + lNumber + 1;
            }

            root = tree.m_RootNode.NewChildConstructedNode(
                BerTree.z3950_presentResponse,
                BerNode.ASN1_CONTEXT);

            // reference id
            if (String.IsNullOrEmpty(info.m_strReferenceId) == false)
            {
                root.NewChildCharNode(BerTree.z3950_ReferenceId,
                    BerNode.ASN1_CONTEXT,
                    Encoding.UTF8.GetBytes(info.m_strReferenceId));
            }

            List<RetrivalRecord> records = new List<RetrivalRecord>();

            // ��ȡҪ���ص�MARC��¼
            if (diag == null)
            {

                // ��¼�����ʽΪ GRS-1 (generic-record-syntax-1) :
                //		EXTERNAL 
                //			--- OID (Object Identifier)
                //			--- MARC (OCTET STRING)
                //	m_strOID = _T("1.2.840.10003.5.1");  // OID of UNIMARC
                //	m_strOID = _T("1.2.840.10003.5.10"); // OID of USMARC //
                // ��Ҫ����һ�����ݿ�����oid�Ķ��ձ��������ȡ�����ݿ�MARC syntax OID

                // TODO: ��������У����ܻᷢ�ּ�¼̫�࣬�ܳߴ糬��Initial�й涨��prefered message size��
                // ������Ҫ���ٷ��صļ�¼����������������Ҫ���������ѭ�����������⼸������
                int nSize = 0;
                for (int i = 0; i < (int)lNumber; i++)
                {
                    // ���� N �� MARC ��¼
                    //
                    // if (m_bStop) return false;

                    // ȡ�����ݿ�ָ��
                    // lStart ���� 0 ����
                    string strPath = paths[i];

                    // ���������ݿ�����ID
                    string strDbName = Global.GetDbName(strPath);
                    string strRecID = Global.GetRecordID(strPath);

                    // ���ȡ�õ���xml��¼�����Ԫ�ؿ��Կ�����¼��marc syntax����һ�����Ի��oid��
                    // ���ȡ�õ���MARC��ʽ��¼������Ҫ�������ݿ�Ԥ�����marc syntax������oid��
                    string strMarcSyntaxOID = GetMarcSyntaxOID(strDbName);

                    byte[] baMARC = null;

                    RetrivalRecord record = new RetrivalRecord();
                    record.m_strDatabaseName = strDbName;

                    // ������Ŀ���������Ŀ�����Զ���
                    BiblioDbProperty prop = this.m_service.GetDbProperty(
                        strDbName,
                        false);

                    nRet = GetMARC(strPath,
                        info.m_elementSetNames,
                        prop != null ? prop.AddField901 : false,
                        out baMARC,
                        out strError);

                    /*
                    // ���Լ�¼Ⱥ�а�����ϼ�¼
                    if (i == 1)
                    {
                        nRet = -1;
                        strError = "���Ի�ȡ��¼����";
                    }*/
                    if (nRet == -1)
                    {
                        record.m_surrogateDiagnostic = new DiagFormat();
                        record.m_surrogateDiagnostic.m_strDiagSetID = "1.2.840.10003.4.1";
                        record.m_surrogateDiagnostic.m_nDiagCondition = 14;  // system error in presenting records
                        record.m_surrogateDiagnostic.m_strAddInfo = strError;
                    }
                    else if (nRet == 0)
                    {
                        record.m_surrogateDiagnostic = new DiagFormat();
                        record.m_surrogateDiagnostic.m_strDiagSetID = "1.2.840.10003.4.1";
                        record.m_surrogateDiagnostic.m_nDiagCondition = 1028;  // record deleted
                        record.m_surrogateDiagnostic.m_strAddInfo = strError;
                    }
                    else if (String.IsNullOrEmpty(strMarcSyntaxOID) == true)
                    {
                        // �������ݿ����޷����marc syntax oid�������������������м�¼���ڵ������û����dp2zserver.xml�����á�
                        record.m_surrogateDiagnostic = new DiagFormat();
                        record.m_surrogateDiagnostic.m_strDiagSetID = "1.2.840.10003.4.1";
                        record.m_surrogateDiagnostic.m_nDiagCondition = 109;  // database unavailable // �ƺ�235:database dos not existҲ����
                        record.m_surrogateDiagnostic.m_strAddInfo = "�������ݿ��� '" + strDbName + "' �޷����marc syntax oid";
                    }
                    else
                    {
                        record.m_external = new External();
                        record.m_external.m_strDirectRefenerce = strMarcSyntaxOID;
                        record.m_external.m_octectAligned = baMARC;
                    }

                    nSize += record.GetPackageSize();

                    if (i == 0)
                    {
                        // ��һ����¼Ҳ�Ų���
                        if (nSize > this.m_lExceptionalRecordSize)
                        {
                            Debug.Assert(diag == null, "");
                            SetPresentDiagRecord(ref diag,
                                17, // record exceeds Exceptional_record_size
                                "��¼�ߴ� " + nSize.ToString() + " ���� Exceptional_record_size " + this.m_lExceptionalRecordSize.ToString());
                            lNumber = 0;
                            break;
                        }
                    }
                    else
                    {
                        if (nSize >= this.m_lPreferredMessageSize)
                        {
                            // �������صļ�¼��
                            lNumber = i;
                            break;
                        }
                    }

                    records.Add(record);
                }
            }


            // numberOfRecordsReturned
            root.NewChildIntegerNode(BerTree.z3950_NumberOfRecordsReturned, // 24
                BerNode.ASN1_CONTEXT,   // ASN1_PRIMITIVE BUG!!!
                BitConverter.GetBytes((long)lNumber));

            if (diag != null)
                nPresentStatus = 5;
            else 
                nPresentStatus = 0;

            // nextResultSetPosition
            // if 0, that's end of the result set
            // else M+1, M is ���һ�� present response �����һ����¼�� result set �е� position
            root.NewChildIntegerNode(BerTree.z3950_NextResultSetPosition, // 25
                BerNode.ASN1_CONTEXT,   // ASN1_PRIMITIVE BUG!!!
                BitConverter.GetBytes((long)nNextResultSetPosition));

            // presentStatus
            // success      (0),
            // partial-1    (1),
            // partial-2    (2),
            // partial-3    (3),
            // partial-4    (4),
            // failure      (5).
            root.NewChildIntegerNode(BerTree.z3950_presentStatus, // 27
                BerNode.ASN1_CONTEXT,   // ASN1_PRIMITIVE BUG!!!
               BitConverter.GetBytes((long)nPresentStatus));


            // ��ϼ�¼
            if (diag != null)
            {
                BerNode nodeDiagRoot = root.NewChildConstructedNode(BerTree.z3950_nonSurrogateDiagnostic,    // 130
                    BerNode.ASN1_CONTEXT);

                diag.BuildBer(nodeDiagRoot);

                /*
                nodeDiagRoot.NewChildOIDsNode(6,
                    BerNode.ASN1_UNIVERSAL,
                    diag.m_strDiagSetID);   // "1.2.840.10003.4.1"

                nodeDiagRoot.NewChildIntegerNode(2,
                    BerNode.ASN1_UNIVERSAL,
                    BitConverter.GetBytes((long)diag.m_nDiagCondition));

                if (String.IsNullOrEmpty(diag.m_strAddInfo) == false)
                {
                    nodeDiagRoot.NewChildCharNode(26,
                        BerNode.ASN1_UNIVERSAL,
                        Encoding.UTF8.GetBytes(diag.m_strAddInfo));
                }
                 * */
            }


            // ��� present �ǷǷ��ģ�����������ɣ����Է�����
            if (0 != nPresentStatus)
                goto END1;

            // �����¼BER��

            // ����Ϊ present �ɹ�ʱ��������ؼ�¼��
            // present success
            // presRoot records child, constructed (choice of ... ... optional)
            // if present fail, then may be no records 'node'
            // Records ::= CHOICE {
            //		responseRecords              [28]   IMPLICIT SEQUENCE OF NamePlusRecord,
            //		nonSurrogateDiagnostic       [130]  IMPLICIT DefaultDiagFormat,
            //		multipleNonSurDiagnostics    [205]  IMPLICIT SEQUENCE OF DiagRec} 

            // �� present �ɹ�ʱ��response ѡ���� NamePlusRecord (���ݿ��� +����¼)
            BerNode node = root.NewChildConstructedNode(BerTree.z3950_dataBaseOrSurDiagnostics,    // 28
                            BerNode.ASN1_CONTEXT);

            for (int i = 0; i < records.Count; i++)
            {
                RetrivalRecord record = records[i];

                record.BuildNamePlusRecord(node);
            }

        END1:

            baPackage = null;
            root.EncodeBERPackage(ref baPackage);

            return 0;
        }

        string GetMarcSyntaxOID(string strBiblioDbName)
        {
            string strSyntax = this.m_service.GetMarcSyntax(strBiblioDbName);
            if (strSyntax == null)
                return null;
            if (strSyntax == "unimarc")
                return "1.2.840.10003.5.1";
            if (strSyntax == "usmarc")
                return "1.2.840.10003.5.10";

            return null;
        }

        // ���MARC��¼
        // parameters:
        //      bAddField901    �Ƿ����901�ֶΣ�
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetMARC(string strPath,
            List<string> elementSetNames,
            bool bAddField901,
            out byte[] baMARC,
            out string strError)
        {
            baMARC = null;
            strError = "";

            string strXml = "";
            byte [] timestamp = null;
        // return:
        //      -1  error
        //      0   not found
        //      1   found
            int nRet = GetMarcXml(strPath,
                out strXml,
                out timestamp,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;

            //
            string strMarcSyntax = "";
            string strOutMarcSyntax = "";
            string strMarc = "";

            // ת��Ϊ���ڸ�ʽ
            nRet = MarcUtil.Xml2Marc(strXml,
                true,
                strMarcSyntax,
                out strOutMarcSyntax,
                out strMarc,
                out strError);
            if (nRet == -1)
            {
                strError = "XMLת����MARC��¼ʱ����: " + strError;
                return -1;
            }

            if (bAddField901 == true)
            {
                // 901  $p��¼·��$tʱ���
                string strField = "901  "
                    + new string(MarcUtil.SUBFLD, 1) + "p" + strPath
                    + new string(MarcUtil.SUBFLD, 1) + "t" + ByteArray.GetHexTimeStampString(timestamp);

                // �滻��¼�е��ֶ����ݡ�
                // ���ڼ�¼����ͬ���ֶ�(��nIndex��)������ҵ������滻�����û���ҵ���
                // ����˳��λ�ò���һ�����ֶΡ�
                // parameters:
                //		strMARC		[in][out]MARC��¼��
                //		strFieldName	Ҫ�滻���ֶε��������Ϊnull����""�����ʾ�����ֶ������ΪnIndex�е��Ǹ����滻
                //		nIndex		Ҫ�滻���ֶε�������š����Ϊ-1����ʼ��Ϊ�ڼ�¼��׷�����ֶ����ݡ�
                //		strField	Ҫ�滻�ɵ����ֶ����ݡ������ֶ�������Ҫ���ֶ�ָʾ�����ֶ����ݡ�����ζ�ţ����������滻һ���ֶε����ݣ�Ҳ�����滻�����ֶ�����ָʾ�����֡�
                // return:
                //		-1	����
                //		0	û���ҵ�ָ�����ֶΣ���˽�strField���ݲ��뵽�ʵ�λ���ˡ�
                //		1	�ҵ���ָ�����ֶΣ�����Ҳ�ɹ���strField�滻���ˡ�
                nRet = MarcUtil.ReplaceField(
                    ref strMarc,
                    "901",
                    0,
                    strField);
                if (nRet == -1)
                    return -1;
            }

            // ת��ΪISO2709
            nRet = MarcUtil.CvtJineiToISO2709(
                strMarc,
                strOutMarcSyntax,
                this.MarcRecordEncoding,
                out baMARC,
                out strError);
            if (nRet == -1)
                return -1;


            return 1;
        }

        // ���MARC XML��¼
        // parameters:
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetMarcXml(string strBiblioRecPath,
            out string strXml,
            out byte[] timestamp,
            out string strError)
        {
            strXml = "";
            strError = "";
            timestamp = null;

            try
            {
                string[] formats = new string[1];
                formats[0] = "xml";

                string[] results = null;

                long lRet = Channel.GetBiblioInfos(
                    null,
                    strBiblioRecPath,
                    "",
                    formats,
                    out results,
                    out timestamp,
                    out strError);
                if (lRet == 0)
                {
                    return 0;   // not found
                }
                if (lRet == -1)
                    return -1;

                strXml = results[0];
            }
            finally
            {
            }

            return 1;
            /*
        ERROR1:
            return -1;
             * */
        }


        // ������search�����е� ���ݿ����б�
        static int DecodeElementSetNames(BerNode root,
            out List<string> elementset_names,
            out string strError)
        {
            elementset_names = new List<string>();
            strError = "";

            for (int i = 0; i < root.ChildrenCollection.Count; i++)
            {
                BerNode node = root.ChildrenCollection[i];
                /*
                if (node.m_uTag == 105)
                {
                    dbnames.Add(node.GetCharNodeData());
                }
                 * */
                // TODO: ������Ҫ��һ��PDU���壬�����Ƿ���Ҫ�ж�m_uTag
                elementset_names.Add(node.GetCharNodeData());
            }

            return 0;
        }


        // ���search�����е�RPN���ڵ�
        static BerNode GetRPNStructureRoot(BerNode root,
            out string strError)
        {
            strError = "";

            if (root == null)
            {
                strError = "query root is null";
                return null;
            }

            if (root.ChildrenCollection.Count < 1)
            {
                strError = "no query item";
                return null;
            }

            BerNode RPNRoot = root.ChildrenCollection[0];
            if (1 != RPNRoot.m_uTag) // type-1 query
            {
                strError = "not type-1 query. unsupported query type";
                return null;
            }

            string strAttributeSetId = ""; //attributeSetId OBJECT IDENTIFIER
            // string strQuery = "";


            for (int i = 0; i < RPNRoot.ChildrenCollection.Count; i++)
            {
                BerNode node = RPNRoot.ChildrenCollection[i];
                switch (node.m_uTag)
                {
                    case 6: // attributeSetId (OBJECT IDENTIFIER)
                        strAttributeSetId = node.GetOIDsNodeData();
                        if (strAttributeSetId != "1.2.840.10003.3.1") // bib-1
                        {
                            strError = "support bib-1 only";
                            return null;
                        }
                        break;
                    // RPNStructure (CHOICE 0, 1)
                    case 0:
                    case 1:
                        return node; // this is RPN Stucture root
                }
            }

            strError = "not found";
            return null;
        }

        // ������search�����е� ���ݿ����б�
        static int DecodeDbnames(BerNode root,
            out List<string> dbnames,
            out string strError)
        {
            dbnames  = new List<string>();
            strError = "";

            for (int i = 0; i < root.ChildrenCollection.Count; i++)
            {
                BerNode node = root.ChildrenCollection[i];
                if (node.m_uTag == 105)
                {
                    dbnames.Add(node.GetCharNodeData());
        		}
	        }

            return 0;
        }


        // ������init�����е� ������Ϣ
        // parameters:
        //      nAuthentType 0: open(simple) 1:idPass(group)
        static int DecodeAuthentication(
            BerNode root,
            out string strGroupId,
            out string strUserId,
            out string strPassword,
            out int nAuthentType,
            out string strError)
        {
            strGroupId = "";
            strUserId = "";
            strPassword = "";
            nAuthentType = 0;
            strError = "";

            if (root == null)
            {
                strError = "root == null";
                return -1;
            }

            string strOpen = ""; // open mode authentication


            for (int i = 0; i < root.ChildrenCollection.Count; i++)
            {
                BerNode node = root.ChildrenCollection[i];
                switch (node.m_uTag)
                {
                    case BerNode.ASN1_SEQUENCE:

                        nAuthentType = 1;   //  "GROUP";
                        for (int k = 0; k < node.ChildrenCollection.Count; k++)
                        {
                            BerNode nodek = node.ChildrenCollection[k];
                            switch (nodek.m_uTag)
                            {
                                case 0: // groupId
                                    strGroupId = nodek.GetCharNodeData();
                                    break;
                                case 1: // userId
                                    strUserId = nodek.GetCharNodeData();
                                    break;
                                case 2: // password
                                    strPassword = nodek.GetCharNodeData();
                                    break;
                            }
                        }

                        break;
                    case BerNode.ASN1_VISIBLESTRING:
                    case BerNode.ASN1_GENERALSTRING:
                        nAuthentType = 0; //  "SIMPLE";
                        strOpen = node.GetCharNodeData();

                        break;


                }
            }


            if (nAuthentType == 0)
            {
                int nRet = strOpen.IndexOf("/");
                if (nRet != -1)
                {
                    strUserId = strOpen.Substring(0, nRet);
                    strPassword = strOpen.Substring(nRet + 1);
                }
                else
                {
                    strUserId = strOpen;
                }
            }

            return 0;
        }

        // ����RPN�ṹ�е�Attribute + Term�ṹ
        static int DecodeAttributeAndTerm(
            Encoding term_encoding,
            BerNode pNode,
            out long lAttributeType,
            out long lAttributeValue,
            out string strTerm,
            out string strError)
        {
            lAttributeType = 0;
            lAttributeValue = 0;
            strTerm = "";
            strError = "";

            if (pNode == null)
            {
                strError = "node == null";
                return -1;
            }


            if (pNode.ChildrenCollection.Count < 2) //attriblist + term
            {
                strError = "bad RPN query";
                return -1;
            }

            BerNode pAttrib = pNode.ChildrenCollection[0]; // attriblist
            BerNode pTerm = pNode.ChildrenCollection[1]; // term

            if (44 != pAttrib.m_uTag) // Attributes
            {
                strError = "only support Attributes";
                return -1;
            }

            if (45 != pTerm.m_uTag) // Term
            {
                strError = "only support general Term";
                return -1;
            }

            // get attribute type and value
            if (pAttrib.ChildrenCollection.Count < 1) //attribelement
            {
                strError = "bad RPN query";
                return -1;
            }

            pAttrib = pAttrib.ChildrenCollection[0];
            if (16 != pAttrib.m_uTag) //attribelement (SEQUENCE) 
            {
                strError = "only support Attributes";
                return -1;
            }

            for (int i = 0; i < pAttrib.ChildrenCollection.Count; i++)
            {
                BerNode pTemp = pAttrib.ChildrenCollection[i];
                switch (pTemp.m_uTag)
                {
                    case 120: // attributeType
                        lAttributeType = pTemp.GetIntegerNodeData();
                        break;
                    case 121: // attributeValue
                        lAttributeValue = pTemp.GetIntegerNodeData();
                        break;
                }
            }

            // get term
            strTerm = pTerm.GetCharNodeData(term_encoding);


            if (-1 == lAttributeType
                || -1 == lAttributeValue
                || String.IsNullOrEmpty(strTerm) == true)
            {
                strError = "bad RPN query";
                return -1;
            }

            return 0;
        }

        static int DecodeRPNOperator(BerNode pNode)
        {
            if (pNode == null)
                return -1;

            if (46 == pNode.m_uTag)
            {
                if (pNode.ChildrenCollection.Count > 0)
                {
                    return pNode.ChildrenCollection[0].m_uTag;
                }
            }

            return -1;
        }

        #endregion // BER������

        // ����һ�������ʵ�XML����ʽ�ֲ�
        // ���������ݹ�
        int BuildOneXml(
            List<string> dbnames,
            string strTerm,
            long lAttritueValue,
            out string strQueryXml,
            out string strError)
        {
            strQueryXml = "";
            strError = "";

            if (dbnames.Count == 0)
            {
                strError = "һ�����ݿ���Ҳδ��ָ��";
                return -1;
            }

            // string strFrom = "";    // ����nAttributeType nAttributeValue�õ�����;����


            // ������һ�£��ǲ���ÿ�����ݿⶼ��һ����maxResultCount������
            // ����ǣ�����԰���Щ���ݿⶼ���Ϊһ��<target>��
            // ������ǣ������ͬ����ѡ������Ϊһ��<target>��Ȼ����<target>��OR�������

            // Ϊ�ˣ������Ȱ����ݿ����Զ�����maxResultCount���������Ա�ۺ�����<target>��
            // ���������һ�����⣺������ļ�������Ⱥ�˳�򣬾Ͳ����û�Ҫ����Ǹ�˳���ˡ�
            // ���������ð����û�ָ�������ݿ�˳��������<item>����ô���Ͳ��ò����;ۺϵĿ��ܣ�
            // �������ۺ����ڵġ�maxResultCountֵ��ͬ����Щ

            int nPrevMaxResultCount = -1;   // ǰһ��MaxResultCount����ֵ
            List<List<BiblioDbProperty>> prop_groups = new List<List<BiblioDbProperty>>();

            List<BiblioDbProperty> props = new List<BiblioDbProperty>();
            for (int i = 0; i < dbnames.Count; i++)
            {
                string strDbName = dbnames[i];

                BiblioDbProperty prop = this.m_service.GetDbProperty(strDbName,
                    true);
                if (prop == null)
                {
                    strError = "���ݿ� '" + strDbName + "' ������";
                    return -1;
                }

                // �����ǰ���MaxResultCount������ǰ����ڵĲ�һ���ˣ�����Ҫ���뵱ǰ����ʹ�õ�props������һ��props
                if (prop.MaxResultCount != nPrevMaxResultCount
                    && props.Count != 0)
                {
                    Debug.Assert(props.Count > 0, "��Ϊ�յ�props�������� (1)");
                    prop_groups.Add(props);
                    props = new List<BiblioDbProperty>();   // ������һ��props
                }

                props.Add(prop);

                nPrevMaxResultCount = prop.MaxResultCount;
            }

            Debug.Assert(props.Count > 0, "��Ϊ�յ�props�������� (2)");
            prop_groups.Add(props); // �����һ��props���뵽group������


            for (int i = 0; i < prop_groups.Count; i++)
            {
                props = prop_groups[i];

                string strTargetListValue = "";
                int nMaxResultCount = -1;
                for (int j = 0; j < props.Count; j++)
                {
                    BiblioDbProperty prop = props[j];

                    string strDbName = prop.DbName;
#if DEBUG
                    if (j != 0)
                    {
                        Debug.Assert(prop.MaxResultCount == nMaxResultCount, "props�ڵ�ÿ�����ݿⶼӦ������ͬ��MaxResultCount����ֵ");
                    }
#endif

                    if (j == 0)
                        nMaxResultCount = prop.MaxResultCount;  // ֻȡ��һ��prop��ֵ����

                    string strOutputDbName = "";
                    string strFrom = this.m_service.GetFromName(strDbName,
                        lAttritueValue,
                        out strOutputDbName,
                        out strError);
                    if (strFrom == null)
                        return -1;  // Ѱ��from���Ĺ��̷�������

                    if (strTargetListValue != "")
                        strTargetListValue += ";";

                    Debug.Assert(strOutputDbName != "", "");

                    strTargetListValue += strOutputDbName + ":" + strFrom;
                }

                if (i != 0)
                    strQueryXml += "<operator value='OR' />";

                strQueryXml += "<target list='" + strTargetListValue + "'>"
                + "<item><word>"
                + StringUtil.GetXmlStringSimple(strTerm)
                + "</word><match>left</match><relation>=</relation><dataType>string</dataType>"
                + "<maxCount>" + nMaxResultCount.ToString() + "</maxCount></item>"
                + "<lang>zh</lang></target>";
            }

            // ����ж��props������Ҫ�ڼ���XML�������һ��<target>Ԫ�أ�����Ϊһ��������������������߼�����
            if (prop_groups.Count > 1)
                strQueryXml = "<target>" + strQueryXml + "</target>";

            return 0;
        }

#if NOOOOOOOOOOOOOOOOOOO
        // ����һ�������ʵ�XML����ʽ�ֲ�
        int BuildOneXml(
            List<string> dbnames,
            string strTerm,
            long lAttritueValue,
            out string strQueryXml,
            out string strError)
        {
            strQueryXml = "";
            strError = "";

            if (dbnames.Count == 0)
            {
                strError = "һ�����ݿ���Ҳδ��ָ��";
                return -1;
            }

            // string strFrom = "";    // ����nAttributeType nAttributeValue�õ�����;����


            // ������һ�£��ǲ���ÿ�����ݿⶼ��һ����maxResultCount������
            // ����ǣ�����԰���Щ���ݿⶼ���Ϊһ��<target>��
            // ������ǣ������ͬ����ѡ������Ϊһ��<target>��Ȼ����<target>��OR�������

            // Ϊ�ˣ������Ȱ����ݿ����Զ�����maxResultCount���������Ա�ۺ�����<target>��
            // ���������һ�����⣺������ļ�������Ⱥ�˳�򣬾Ͳ����û�Ҫ����Ǹ�˳���ˡ�
            // ���������ð����û�ָ�������ݿ�˳��������<item>����ô���Ͳ��ò����;ۺϵĿ��ܣ�
            // �������ۺ����ڵġ�maxResultCountֵ��ͬ����Щ

            int nPrevMaxResultCount = -1;   // ǰһ��MaxResultCount����ֵ
            List<List<BiblioDbProperty>> prop_groups = new List<List<BiblioDbProperty>>();

            List<BiblioDbProperty> props = new List<BiblioDbProperty>();
            for (int i = 0; i < dbnames.Count; i++)
            {
                string strDbName = dbnames[i];

                BiblioDbProperty prop = this.m_service.GetDbProperty(strDbName);
                if (prop == null)
                {
                    strError = "���ݿ� '" + strDbName + "' ������";
                    return -1;
                }

                // �����ǰ���MaxResultCount������ǰ����ڵĲ�һ���ˣ�����Ҫ���뵱ǰ����ʹ�õ�props������һ��props
                if (prop.MaxResultCount != nPrevMaxResultCount
                    && props.Count != 0)
                {
                    Debug.Assert(props.Count > 0, "��Ϊ�յ�props�������� (1)");
                    prop_groups.Add(props);
                    props = new List<BiblioDbProperty>();   // ������һ��props
                }

                props.Add(prop);
            }

            Debug.Assert(props.Count > 0, "��Ϊ�յ�props�������� (2)");
            prop_groups.Add(props); // �����һ��props���뵽group������


            for (int i = 0; i < prop_groups.Count; i++)
            {
                props = prop_groups[i];

                string strTargetListValue = "";
                int nMaxResultCount = -1;
                for (int j = 0; j < props.Count; j++)
                {
                    BiblioDbProperty prop = props[j];

                    string strDbName = prop.DbName;
                    /*
                    string strDbName = dbnames[j];

                    BiblioDbProperty prop = this.m_service.GetDbProperty(strDbName);
                    if (prop == null)
                    {
                        strError = "���ݿ� '" + strDbName + "' ������";
                        return -1;
                    }
                     * */

#if DEBUG
                    if (j != 0)
                    {
                        Debug.Assert(prop.MaxResultCount == nMaxResultCount, "props�ڵ�ÿ�����ݿⶼӦ������ͬ��MaxResultCount����ֵ");
                    }
#endif

                    if (j==0)
                        nMaxResultCount = prop.MaxResultCount;  // ֻȡ��һ��prop��ֵ����

                    string strOutputDbName = "";
                    string strFrom = this.m_service.GetFromName(strDbName,
                        lAttritueValue,
                        out strOutputDbName,
                        out strError);
                    if (strFrom == null)
                        return -1;  // Ѱ��from���Ĺ��̷�������

                    if (strTargetListValue != "")
                        strTargetListValue += ";";

                    Debug.Assert(strOutputDbName != "", "");

                    strTargetListValue += strOutputDbName + ":" + strFrom;
                }

                if (i != 0)
                    strQueryXml += "<operator value='OR' />";

                strQueryXml += "<target list='" + strTargetListValue + "'>"
                + "<item><word>"
                + StringUtil.GetXmlStringSimple(strTerm)
                + "</word><match>left</match><relation>=</relation><dataType>string</dataType>"
                + "<maxCount>" + nMaxResultCount.ToString() + "</maxCount></item>"
                + "<lang>zh</lang></target>";
            }

            // ����ж��props������Ҫ�ڼ���XML�������һ��<target>Ԫ�أ�����Ϊһ��������������������߼�����
            if (prop_groups.Count > 1)
                strQueryXml = "<target>" + strQueryXml + "</target>";

            return 0;
        }

#endif

        // ����RPN����XML����ʽ
        // ������Ҫ�ݹ���ã��������ݿⲢ���ؽ����
        // parameters:
        //		node    RPN �ṹ�ĸ����
        //		strXml[out] ���ؾֲ�XML����ʽ
        // return:
        //      -1  error
        //      0   succeed
        int BuildQueryXml(
            List<string> dbnames,
            BerNode node,
            out string strQueryXml,
            out string strError)
        {
            strQueryXml = "";
            strError = "";
            int nRet = 0;

            if (node == null)
            {
                strError = "node == null";
                return -1;
            }


            if (0 == node.m_uTag)
            {
                // operand node

                // �����õ� saRecordID
                if (node.ChildrenCollection.Count < 1)
                {
                    strError = "bad RPN structure";
                    return -1;
                }

                BerNode pChild = node.ChildrenCollection[0];

                if (102 == pChild.m_uTag)
                {
                    // AttributesPlusTerm
                    long nAttributeType = -1;
                    long nAttributeValue = -1;
                    string strTerm = "";

                    nRet = DecodeAttributeAndTerm(
                        this.SearchTermEncoding,
                        pChild,
                        out nAttributeType,
                        out nAttributeValue,
                        out strTerm,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    nRet = BuildOneXml(
                        dbnames,
                        strTerm,
                        nAttributeValue,
                        out strQueryXml,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    return 0;

                    /*
			// ���Ҫȥ�������ݿ���
			SearchDBMulti(pResult, nAttributeValue, strTerm);
                     * */
                }
                else if (31 == pChild.m_uTag)
                {
                    // �ǽ������Ԥ�˼���
                    string strResultSetID = pChild.GetCharNodeData();

                    strQueryXml = "<item><resultSetName>" + strResultSetID + "</resultSetName></item>";
                    /*
                    //
                    // Ϊ�˱����ڵݹ�����ʱɾ������ǰ�����Ľ������copy һ��
                    if (!FindAndCopyExistResultSet(strResultSetID, pResult)) {
                        throw_exception(0, _T("referred resultset not exist"));
                    }
                    //
                     * */
                }
                else
                {
                    //
                    strError = "Unsurported RPN structure";
                }

            }
            else if (1 == node.m_uTag)
            { // rpnRpnOp
                //
                if (3 != node.ChildrenCollection.Count)
                {
                    strError = "bad RPN structure";
                    return -1;
                }
                //
                string strXmlLeft = "";
                string strXmlRight = "";
                int nOperator = -1;

                nRet = BuildQueryXml(
                    dbnames,
                    node.ChildrenCollection[0],
                    out strXmlLeft,
                    out strError);
                if (nRet == -1)
                    return -1;

                nRet = BuildQueryXml(
                    dbnames,
                    node.ChildrenCollection[1],
                    out strXmlRight,
                    out strError);
                if (nRet == -1)
                    return -1;


                //	and     [0] 
                //	or      [1] 
                //	and-not [2] 
                nOperator = DecodeRPNOperator(node.ChildrenCollection[2]);
                if (nOperator == -1)
                {
                    strError = "DecodeRPNOperator() return -1";
                    return -1;
                }

                switch (nOperator)
                {
                    case 0: // and
                        strQueryXml = "<group>" + strXmlLeft + "<operator value='AND' />" + strXmlRight + "</group>";
                        break;
                    case 1: // or 
                        strQueryXml = "<group>" + strXmlLeft + "<operator value='OR' />" + strXmlRight + "</group>";
                        break;
                    case 2: // and-not
                        strQueryXml = "<group>" + strXmlLeft + "<operator value='SUB' />" + strXmlRight + "</group>";
                        break;
                    default:
                        // ��֧�ֵĲ�����
                        strError = "unsurported operator";
                        return -1;
                }
            }
            else
            {
                strError = "bad RPN structure";
            }

            return 0;
        }

        // 2007/7/18
        //	 build a z39.50 Init response
        public static int Encode_InitialResponse(InitResponseInfo info,
            out byte[] baPackage)
        {
            baPackage = null;

            BerNode root = null;

            BerTree tree = new BerTree();

            root = tree.m_RootNode.NewChildConstructedNode(BerTree.z3950_initResponse,
                BerNode.ASN1_CONTEXT);

            if (String.IsNullOrEmpty(info.m_strReferenceId) == false)
            {
                root.NewChildCharNode(BerTree.z3950_ReferenceId,
                    BerNode.ASN1_CONTEXT,
                    Encoding.UTF8.GetBytes(info.m_strReferenceId));
            }

            root.NewChildBitstringNode(BerTree.z3950_ProtocolVersion,   // 3
                BerNode.ASN1_CONTEXT,
                "yy");

            /* option
         search                 (0), 
         present                (1), 
         delSet                 (2),
         resourceReport         (3),
         triggerResourceCtrl    (4),
         resourceCtrl           (5), 
         accessCtrl             (6),
         scan                   (7),
         sort                   (8), 
         --                     (9) (reserved)
         extendedServices       (10),
         level-1Segmentation    (11),
         level-2Segmentation    (12),
         concurrentOperations   (13),
         namedResultSets        (14)
            15 Encapsulation  Z39.50-1995 Amendment 3: Z39.50 Encapsulation 
            16 resultCount parameter in Sort Response  See Note 8 Z39.50-1995 Amendment 1: Add resultCount parameter to Sort Response  
            17 Negotiation Model  See Note 9 Model for Z39.50 Negotiation During Initialization  
            18 Duplicate Detection See Note 1  Z39.50 Duplicate Detection Service  
            19 Query type 104 
*/
            root.NewChildBitstringNode(BerTree.z3950_Options,   // 4
                BerNode.ASN1_CONTEXT,
                info.m_strOptions);    // "110000000000001"


            root.NewChildIntegerNode(BerTree.z3950_PreferredMessageSize,    // 5
                BerNode.ASN1_CONTEXT,
                BitConverter.GetBytes((long)info.m_lPreferredMessageSize));

            root.NewChildIntegerNode(BerTree.z3950_ExceptionalRecordSize,   // 6
                BerNode.ASN1_CONTEXT,
                BitConverter.GetBytes((long)info.m_lExceptionalRecordSize));

            // 2007/11/7 ԭ�������������λ�ò��ԣ����ڵ���������
            // bool
            root.NewChildIntegerNode(BerTree.z3950_result,  // 12
                BerNode.ASN1_CONTEXT,
                BitConverter.GetBytes((long)info.m_nResult));


            root.NewChildCharNode(BerTree.z3950_ImplementationId,   // 110
                BerNode.ASN1_CONTEXT,
                Encoding.UTF8.GetBytes(info.m_strImplementationId));

            root.NewChildCharNode(BerTree.z3950_ImplementationName, // 111
                BerNode.ASN1_CONTEXT,
                Encoding.UTF8.GetBytes(info.m_strImplementationName));

            root.NewChildCharNode(BerTree.z3950_ImplementationVersion,  // 112
                BerNode.ASN1_CONTEXT,
                Encoding.UTF8.GetBytes(info.m_strImplementationVersion));  // "3"


            // userInformationField
            if (info.UserInfoField != null)
            {
                BerNode nodeUserInfoRoot = root.NewChildConstructedNode(BerTree.z3950_UserInformationField,    // 11
                    BerNode.ASN1_CONTEXT);
                info.UserInfoField.Build(nodeUserInfoRoot);
            }

            if (info.m_charNego != null)
            {
                info.m_charNego.EncodeResponse(root);
            }

            baPackage = null;
            root.EncodeBERPackage(ref baPackage);

            return 0;
        }

    }

    // Init������Ϣ�ṹ
    public class InitRequestInfo
    {
        public string m_strReferenceId = "";
        public string m_strProtocolVersion = "";

        public string m_strOptions = "";

        public long m_lPreferredMessageSize = 0;
        public long m_lExceptionalRecordSize = 0;

        public int m_nAuthenticationMethod = 0;	// 0: open 1:idPass
        public string m_strGroupID = "";
        public string m_strID = "";
        public string m_strPassword = "";

        public string m_strImplementationId = "";
        public string m_strImplementationName = "";
        public string m_strImplementationVersion = "";

        public CharsetNeogatiation m_charNego = null;
    }

    public class InitResponseInfo
    {
        public string m_strReferenceId = "";
        public string m_strOptions = "";
        public long m_lPreferredMessageSize = 0;
        public long m_lExceptionalRecordSize = 0;
        public long m_nResult = 0;

        public string m_strImplementationId = "";
        public string m_strImplementationName = "";
        public string m_strImplementationVersion = "";

        // public long m_lErrorCode = 0;

        // public string m_strErrorMessage = "";
        public External UserInfoField = null;

        public CharsetNeogatiation m_charNego = null;
    }

    // Search������Ϣ�ṹ
    public class SearchRequestInfo
    {
        public string m_strReferenceId = "";

        public long m_lSmallSetUpperBound = 0;
        public long m_lLargeSetLowerBound = 0;
        public long m_lMediumSetPresentNumber = 0;

        // bool
        public long m_lReplaceIndicator = 0;

        public string m_strResultSetName = "default";
        public List<string> m_dbnames = null;

        public BerNode m_rpnRoot = null;
    }

    // Search��Ӧ��Ϣ�ṹ
    public class SearchResponseInfo
    {
        public string m_strReferenceId = "";

        public long m_lResultCount = 0;
        public long m_lNumberOfRecordReturned = 0;
        public long m_lNextResultSetPosition = 0;

        // bool
        public long m_lSearchStatus = 0;
    }


        // Present������Ϣ�ṹ
    public class PresentRequestInfo
    {
        public string m_strReferenceId = "";

        public string m_strResultSetID = "";
        public long m_lResultSetStartPoint = 0;
        public long m_lNumberOfRecordsRequested = 0;
        public List<string> m_elementSetNames = null;
    }

    /*
    // Present��Ӧ��Ϣ�ṹ
    public class PresentResponseInfo
    {
        public string m_strReferenceId = "";

        public string m_strResultSetID = "default"; // �����������present������
        public long m_lNumberOfRecordReturned = 0;  // �������еĽ���������ӽ�������

        public long m_lResultSetStartPoint = 0; // Ҫ��ȡ�Ŀ�ʼƫ�ơ���present������
        public long m_lNumberOfRecordsRequested = 0;    // Ҫ��ȡ�ļ�¼������present������



        // nextResultSetPosition
        // if 0, that's end of the result set
        // else M+1, M is ���һ�� present response �����һ����¼�� result set �е� position
        public long m_lNextResultSetPosition = 0;

        // presentStatus
        // success      (0),
        // partial-1    (1),
        // partial-2    (2),
        // partial-3    (3),
        // partial-4    (4),
        // failure      (5).
        public long m_lPresentStatus = 0;

        public List<string> m_paths = null; // Ҫ��õı�����¼��·��

        public List<string> m_elementSetNames = null;   // Ԫ�ؼ����ǡ���present������
    }
     * */

    /*
External is defined in the ASN.1 standard.

EXTERNAL ::= [UNIVERSAL 8] IMPLICIT SEQUENCE
    {direct-reference      OBJECT IDENTIFIER OPTIONAL,
     indirect-reference    INTEGER           OPTIONAL,
     data-value-descriptor ObjectDescriptor  OPTIONAL,
     encoding              CHOICE
        {single-ASN1-type  [0] ANY,
         octet-aligned     [1] IMPLICIT OCTET STRING,
         arbitrary         [2] IMPLICIT BIT STRING}}

In Z39.50, we use the direct-reference option and omit the
indirect-reference and data-value-descriptor.  For the encoding, we use
single-asn1-type if the record has been defined with ASN.1.  Examples would
be GRS-1 and SUTRS records.  We use octet-aligned for non-ASN.1 records.
The most common example of this would be a MARC record.

Hope this helps!

Ralph
     * */
    // �������еļ�¼
    public class RetrivalRecord
    {
        public string m_strDatabaseName = "";    //
        public External m_external = null;
        public DiagFormat m_surrogateDiagnostic = null;

        // ����������ռ�İ��ߴ�
        public int GetPackageSize()
        {
            int nSize = 0;

            if (String.IsNullOrEmpty(this.m_strDatabaseName) == false)
            {
                nSize += Encoding.UTF8.GetByteCount(this.m_strDatabaseName);
            }

            if (this.m_external != null)
                nSize += this.m_external.GetPackageSize();

            if (this.m_surrogateDiagnostic != null)
                nSize += this.m_surrogateDiagnostic.GetPackageSize();

            return nSize;
        }

        // ����NamePlusRecord����
        // parameters:
        //      node    NamePlusRecord�������ڵ㡣Ҳ����Present Response�ĸ��ڵ�
        public void BuildNamePlusRecord(BerNode node)
        {
            if (this.m_external == null
                && this.m_surrogateDiagnostic == null)
                throw new Exception("m_external �� m_surrogateDiagnostic ����ͬʱΪ��");

            if (this.m_external != null
                && this.m_surrogateDiagnostic != null)
                throw new Exception("m_external �� m_surrogateDiagnostic ����ͬʱΪ�ǿա�ֻ����һ��Ϊ��");


            BerNode pSequence = node.NewChildConstructedNode(
                BerNode.ASN1_SEQUENCE,    // 16
                BerNode.ASN1_UNIVERSAL);

            // ���ݿ���
            pSequence.NewChildCharNode(0,
                BerNode.ASN1_CONTEXT,   // ASN1_PRIMITIVE, BUG!!!
                Encoding.UTF8.GetBytes(this.m_strDatabaseName));

            // record(һ����¼)
            BerNode nodeRecord = pSequence.NewChildConstructedNode(
                1,
                BerNode.ASN1_CONTEXT);


            if (this.m_external != null)
            {
                // extenal
                BerNode nodeRetrievalRecord = nodeRecord.NewChildConstructedNode(
                    1,
                    BerNode.ASN1_CONTEXT);

                // real extenal!
                BerNode nodeExternal = nodeRetrievalRecord.NewChildConstructedNode(
                    8,  // UNI_EXTERNAL
                    BerNode.ASN1_UNIVERSAL);

                // TODO: ��ǰһ���ظ��Ŀ�����marc syntax oid����ʡ�ԣ�

                Debug.Assert(String.IsNullOrEmpty(this.m_external.m_strDirectRefenerce) == false, "");

                nodeExternal.NewChildOIDsNode(6,   // UNI_OBJECTIDENTIFIER,
                    BerNode.ASN1_UNIVERSAL,
                    this.m_external.m_strDirectRefenerce);

                // 1 �� MARC ��¼
                nodeExternal.NewChildCharNode(1,
                    BerNode.ASN1_CONTEXT,
                    this.m_external.m_octectAligned);
            }

            // ������MARC��¼����������Ҫ����SurrogateDiagnostic record
            if (this.m_surrogateDiagnostic != null)
            {
                BerNode nodeSurrogateDiag = nodeRecord.NewChildConstructedNode(
                    2,
                    BerNode.ASN1_CONTEXT);

                BerNode nodeDiagRoot = nodeSurrogateDiag.NewChildConstructedNode(
                    BerNode.ASN1_SEQUENCE, // sequence
                    BerNode.ASN1_UNIVERSAL);

                this.m_surrogateDiagnostic.BuildBer(nodeDiagRoot);

                /*
                nodeDiagRoot.NewChildOIDsNode(6,
                    BerNode.ASN1_UNIVERSAL,
                    this.m_surrogateDiagnostic.m_strDiagSetID);   // "1.2.840.10003.4.1"

                nodeDiagRoot.NewChildIntegerNode(2,
                    BerNode.ASN1_UNIVERSAL,
                    BitConverter.GetBytes((long)this.m_surrogateDiagnostic.m_nDiagCondition));

                if (String.IsNullOrEmpty(this.m_surrogateDiagnostic.m_strAddInfo) == false)
                {
                    nodeDiagRoot.NewChildCharNode(26,
                        BerNode.ASN1_UNIVERSAL,
                        Encoding.UTF8.GetBytes(this.m_surrogateDiagnostic.m_strAddInfo));
                }
                 * */


            }

        }

    }

}
