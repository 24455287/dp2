using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.Threading;

using DigitalPlatform.IO;
using DigitalPlatform.rms.Client;
using DigitalPlatform.Text;
using System.Runtime.Serialization;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// Summary description for SessionInfo.
    /// </summary>
    public class SessionInfo
    {
#if NO
        public int LoginErrorCount = 0; // ��������������Ĵ���
#endif

        public const int DEFAULT_MAX_CLIENTS = 5;

        bool _closed = false;

        public bool Closed
        {
            get
            {
                return this._closed;
            }
        }

        /// <summary>
        /// �Ƿ�Ϊ����ģʽ
        /// </summary>
        public bool TestMode
        {
            get;
            set;
        }

        public string ClientIP = "";  // ǰ�� IP ��ַ
        public string Via = ""; // ����ʲôЭ��
        public string SessionID = "";   // Session Ψһ�� ID

        public bool NeedAutoClean = true;   // �Ƿ���Ҫ�Զ����
        public long CallCount = 0;

        public SessionTime SessionTime = null;

        public string Lang = "";

        // �ղ����������һ��Amerce��ID�б�
        public List<string> AmerceIds = null;
        public string AmerceReaderBarcode = "";

        // TODO: ����������ʱ�ļ�Ҫ�ڹ涨��Ŀ¼��
        // TODO: �۲����Ƿ��ͷ�
        public DupResultSet DupResultSet = null;

        public LibraryApplication App = null;
        public RmsChannelCollection Channels = new RmsChannelCollection();

        private string m_strTempDir = "";	// ��ʱ�ļ�Ŀ¼ 2008/3/31


        //public string UserName = "";
        //public string Rights = "";

        //string m_strDp2UserName = "";
        //string m_strDp2Password = "";

        // public string GlobalErrorInfo = "";

#if NO
        public QuestionCollection Questions = new QuestionCollection();

        public int Step = 0;
#endif

        public Account Account = null;

        // public Stack LoginCallStack = new Stack();

        // public event ItemLoadEventHandler ItemLoad = null;
        // public event SetStartEventHandler SetStart = null;

        public string Dp2UserName
        {
            get
            {
                if (this.Account != null)
                {
                    if (this.Account.RmsUserName != "")
                        return this.Account.RmsUserName;
                }

                return App.ManagerUserName;
            }
        }

        public string Dp2Password
        {
            get
            {
                if (this.Account != null)
                {
                    if (this.Account.RmsUserName != "")
                        return this.Account.RmsPassword;
                }

                return App.ManagerPassword;
            }
        }

        public string UserID
        {
            get
            {
                if (this.Account == null)
                    return "";
                return this.Account.UserID;
            }
        }

        public string UserType
        {
            get
            {
                if (this.Account == null)
                    return "";
                return this.Account.Type;
            }
        }

        public string Rights
        {
            get
            {
                if (this.Account == null)
                    return "";
                return this.Account.Rights;
            }
        }

        // 2010/10/27
        public string RightsOrigin
        {
            get
            {
                if (this.Account == null)
                    return "";
                return this.Account.RightsOrigin;
            }
        }

        // 2012/9/24
        public QuickList RightsOriginList
        {
            get
            {
                if (this.Account == null)
                    return new QuickList();
                return this.Account.RightsOriginList;
            }
        }

        public string LibraryCodeList
        {
            get
            {
                if (this.Account == null)
                    return "";
                return this.Account.AccountLibraryCode;
            }
        }

        // �Ƿ�Ϊȫ���û�? ��νȫ���û����ǹ�Ͻ���йݴ�����û�
        public bool GlobalUser
        {
            get
            {
                return IsGlobalUser(this.LibraryCodeList);
            }
        }

        public static bool IsGlobalUser(string strLibraryCodeList)
        {
            if (strLibraryCodeList == "*" || string.IsNullOrEmpty(strLibraryCodeList) == true)
                return true;
            /*
            if (strLibraryCodeList == "*" || strLibraryCodeList == "<global>")
                return true;
            */

            return false;
        }

        public string Access
        {
            get
            {
                if (this.Account == null)
                    return "";
                return this.Account.Access;
            }
        }

        /// <summary>
        /// ������־���ص�ǰ�˵�ַ������ IP �� Via ��������
        /// </summary>
        public string ClientAddress
        {
            get
            {
                return this.ClientIP + "@" + this.Via;
            }
        }

        public SessionInfo(LibraryApplication app,
            string strSessionID = "",
            string strIP = "",
            string strVia = "")
        {
            this.App = app;
            this.Channels.GUI = false;

            this.Channels.AskAccountInfo -= new AskAccountInfoEventHandle(Channels_AskAccountInfo);
            this.Channels.AskAccountInfo += new AskAccountInfoEventHandle(Channels_AskAccountInfo);
            // this.Channels.procAskAccountInfo = new Delegate_AskAccountInfo(this.AskAccountInfo);

            this.m_strTempDir = PathUtil.MergePath(app.SessionDir, this.GetHashCode().ToString());

            this.SessionID = strSessionID;
            this.ClientIP = strIP;
            this.Via = strVia;
        }


        public string GetTempDir()
        {
            Debug.Assert(this.m_strTempDir != "", "");

            PathUtil.CreateDirIfNeed(this.m_strTempDir);	// ȷ��Ŀ¼����
            return this.m_strTempDir;
        }


        public void CloseSession()
        {
            if (this._closed == true)
                return;

            this._closed = true;

            if (String.IsNullOrEmpty(this.m_strTempDir) == false)
            {
                try
                {
                    DirectoryInfo di = new DirectoryInfo(this.m_strTempDir);
                    if (di.Exists == true)
                        di.Delete(true);
                }
                catch
                {
                }
            }

            if (this.Channels != null)
                this.Channels.Dispose();

            this.ClientIP = "";
        }

        void Channels_AskAccountInfo(object sender, AskAccountInfoEventArgs e)
        {
            e.Owner = null;

            ///
            e.UserName = this.Dp2UserName;
            e.Password = this.Dp2Password;
            e.Result = 1;
        }

        // ��¼
        // TODO: ��������ʾ
        // parameters:
        //      strPassword ���Ϊnull����ʾ����֤���롣�����Ҫ����ע�⣬�����ǿ����룬���Ҫ��֤Ҳ��Ҫʹ��""
        // return:
        //      -1  error
        //      0   user not found, or password error
        //      1   succeed
        public int Login(
            string strUserID,
            string strPassword,
            string strLocation,
            bool bPublicError,  // �Ƿ�ģ���û��������벻ƥ����ʾ?
            string strClientIP,
            string strGetToken,
            out string strRights,
            out string strLibraryCode,
            out string strError)
        {
            strError = "";
            strRights = "";
            strLibraryCode = "";

            if (this.App == null)
            {
                strError = "App == null";
                return -1;
            }

            Account account = null;

            int nRet = this.App.GetAccount(strUserID,
                out account, 
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                if (bPublicError == true)
                    strError = this.App.GetString("�ʻ������ڻ����벻��ȷ");
                return 0;
            }

            if (strPassword != null)
            {
                if (StringUtil.HasHead(strPassword, "token:") == true)
                {
                    string strToken = strPassword.Substring("token:".Length);
                    string strHashedPassword = "";
                    try
                    {
                        strHashedPassword = Cryptography.GetSHA1(account.Password);
                    }
                    catch
                    {
                        strError = "�ڲ�����";
                        return -1;
                    }
                    // return:
                    //      -1  ����
                    //      0   ��֤��ƥ��
                    //      1   ��֤ƥ��
                    nRet = LibraryApplication.VerifyToken(
                        strClientIP,
                        strToken,
                        strHashedPassword,
                        out strError);
                    if (nRet != 1)
                        return nRet;
                }
#if NO
                // ��ǰ������
                else if (strPassword != account.Password)
                {
                    if (bPublicError == true)
                        strError = this.App.GetString("�ʻ������ڻ����벻��ȷ");
                    else
                        strError = this.App.GetString("���벻��ȷ");
                    return 0;
                }
#endif
                else
                {
                    nRet = LibraryServerUtil.MatchUserPassword(strPassword, account.Password, out strError);
                    if (nRet == -1)
                    {
                        strError = "MatchUserPassword() error: " + strError;
                        return -1;
                    }
                    if (nRet == 0)
                    {
                        if (bPublicError == true)
                            strError = this.App.GetString("�ʻ������ڻ����벻��ȷ");
                        else
                            strError = this.App.GetString("���벻��ȷ");
                        return 0;
                    }
                }
            }

            this.Account = account;

            if (this.Account != null)
                this.Account.Location = strLocation;

            strRights = this.RightsOrigin;
            strLibraryCode = this.LibraryCodeList;

            if (string.IsNullOrEmpty(strGetToken) == false)
            {
                string strHashedPassword = "";
                try
                {
                    strHashedPassword = Cryptography.GetSHA1(account.Password);
                }
                catch
                {
                    strError = "�ڲ�����";
                    return -1;
                }
                string strToken = "";
                nRet = LibraryApplication.MakeToken(strClientIP,
                    LibraryApplication.GetTimeRangeByStyle(strGetToken),
                    strHashedPassword,
                    out strToken,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (string.IsNullOrEmpty(strToken) == false)
                    strRights += ",token:" + strToken;
            }

            return 1;
        }

        /*
		// ���ȱʡ�ʻ���Ϣ
		// return:
		//		2	already login succeed
		//		1	dialog return OK
		//		0	dialog return Cancel
		//		-1	other error
		public int AskAccountInfo(ChannelCollection Channels, 
			string strComment,
			string strUrl,
			string strPath,
			LoginStyle loginStyle,
			out IWin32Window owner,	// �����Ҫ���ֶԻ������ﷵ�ضԻ��������Form
			out string strUserName,
			out string strPassword)
		{
			owner = null;

			///
			strUserName = this.Dp2UserName;
			strPassword = this.Dp2Password;

			return 1;
		}
         */

#if NO
        // ������������
        public int SearchItems(
            LibraryApplication app,
            string strItemDbName,
            string strBiblioRecId,
            out string strError)
        {
            strError = "";
            string strXml = "";

            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(strItemDbName + ":" + "����¼")       // 2007/9/14 new add
                + "'><item><word>"
                + strBiblioRecId
                + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            RmsChannel channel = this.Channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            long lRet = channel.DoSearch(strQueryXml,
                "default",
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = "û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            List<string> aPath = null;
            lRet = channel.DoGetSearchResult(
                "default",
                0,
                -1,
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            if (aPath.Count == 0)
            {
                strError = "DoGetSearchResult aPath error";
                goto ERROR1;
            }

            for (int i = 0; i < aPath.Count; i++)
            {
                string strMetaData = "";
                byte[] timestamp = null;
                string strOutputPath = "";

                lRet = channel.GetRes(aPath[i],
                    out strXml,
                    out strMetaData,
                    out timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (this.ItemLoad != null)
                {
                    ItemLoadEventArgs e = new ItemLoadEventArgs();
                    e.Path = aPath[i];
                    e.Index = i;
                    e.Count = aPath.Count;
                    e.Xml = strXml;

                    this.ItemLoad(this, e);
                }
            }

            return aPath.Count;
        ERROR1:
            return -1;
        }
#endif

#if NO
        // ������������
        // ����ƫ�����İ汾
        // 2009/6/9 new add
        // return:
        //      ���е�ȫ�����������
        public int SearchItems(
            LibraryApplication app,
            string strItemDbName,
            string strBiblioRecId,
            int nStart,
            int nMaxCount,
            out string strError)
        {
            strError = "";
            string strXml = "";

            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(strItemDbName + ":" + "����¼")       // 2007/9/14 new add
                + "'><item><order>DESC</order><word>"
                + strBiblioRecId
                + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            RmsChannel channel = this.Channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            long lRet = channel.DoSearch(strQueryXml,
                "default",
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = "û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            List<string> aPath = null;
            lRet = channel.DoGetSearchResult(
                "default",
                nStart, // 0,
                nMaxCount, // -1,
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            if (aPath.Count == 0)
            {
                strError = "DoGetSearchResult aPath error";
                goto ERROR1;
            }

            for (int i = 0; i < aPath.Count; i++)
            {
                string strMetaData = "";
                byte[] timestamp = null;
                string strOutputPath = "";

                lRet = channel.GetRes(aPath[i],
                    out strXml,
                    out strMetaData,
                    out timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (this.ItemLoad != null)
                {
                    ItemLoadEventArgs e = new ItemLoadEventArgs();
                    e.Path = aPath[i];
                    e.Index = i;
                    e.Count = aPath.Count;
                    e.Xml = strXml;

                    this.ItemLoad(this, e);
                }
            }

            return (int)lHitCount;
        ERROR1:
            return -1;
        }
#endif

#if NO
        // ��������ע����
        // return:
        //      ���е�ȫ�����������
        public long SearchComments(
            LibraryApplication app,
            string strCommentDbName,
            string strBiblioRecId,
            out string strError)
        {
            strError = "";
            // string strXml = "";

            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(strCommentDbName + ":" + "����¼")       // 2007/9/14 new add
                + "'><item><order>DESC</order><word>"
                + strBiblioRecId
                + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            RmsChannel channel = this.Channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            long lRet = channel.DoSearch(strQueryXml,
                "default",
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                return -1;

            // not found
            if (lRet == 0)
            {
                strError = "û���ҵ�";
                return 0;
            }

            return lRet;
        }
#endif

#if NO
        // ���һ����Χ�ļ������н��
        // return:
        public int GetCommentsSearchResult(
            LibraryApplication app,
            int nStart,
            int nMaxCount,
            bool bGetRecord,
            out string strError)
        {
            strError = "";

            RmsChannel channel = this.Channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            List<string> aPath = null;
            long lRet = channel.DoGetSearchResultEx(
                "default",
                nStart, // 0,
                nMaxCount, // -1,
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            long lHitCount = lRet;

            if (aPath.Count == 0)
            {
                strError = "DoGetSearchResult aPath error";
                goto ERROR1;
            }

            for (int i = 0; i < aPath.Count; i++)
            {

                if (bGetRecord == true)
                {
                    string strXml = "";
                    string strMetaData = "";
                    byte[] timestamp = null;
                    string strOutputPath = "";

                    lRet = channel.GetRes(aPath[i],
                        out strXml,
                        out strMetaData,
                        out timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (this.ItemLoad != null)
                    {
                        ItemLoadEventArgs e = new ItemLoadEventArgs();
                        e.Path = aPath[i];
                        e.Index = i;
                        e.Count = aPath.Count;
                        e.Xml = strXml;
                        e.Timestamp = timestamp;
                        e.TotalCount = (int)lHitCount;

                        this.ItemLoad(this, e);
                    }
                }
                else
                {

                    if (this.ItemLoad != null)
                    {
                        ItemLoadEventArgs e = new ItemLoadEventArgs();
                        e.Path = aPath[i];
                        e.Index = i;
                        e.Count = aPath.Count;
                        e.Xml = "";
                        e.Timestamp = null;
                        e.TotalCount = (int)lHitCount;

                        this.ItemLoad(this, e);
                    }
                }
            }

            return 0;
        ERROR1:
            return -1;
        }

        // �������Ե���ע��¼·�������һ����Χ�ļ������н��
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        public int GetCommentsSearchResult(
            LibraryApplication app,
            int nPerCount,
            string strCommentRecPath,
            bool bGetRecord,
            out int nStart,
            out string strError)
        {
            strError = "";
            nStart = -1;

            RmsChannel channel = this.Channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            long lHitCount = 0;

            bool bFound = false;
            List<string> aPath = null;
            for (int j = 0; ; j++)
            {
                nStart = j * nPerCount;
                long lRet = channel.DoGetSearchResultEx(
                    "default",
                    nStart, // 0,
                    nPerCount, // -1,
                    "zh",
                    null,
                    out aPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                lHitCount = lRet;

                if (lHitCount == 0)
                    return 0;

                if (aPath.Count == 0)
                    break;

                for (int i = 0; i < aPath.Count; i++)
                {
                    if (aPath[i] == strCommentRecPath)
                    {
                        bFound = true;
                        break;
                    }
                }

                if (bFound == true)
                    break;

                if (nStart >= lHitCount)
                    break;
            }

            if (bFound == true)
            {
                if (this.SetStart != null)
                {
                    SetStartEventArgs e = new SetStartEventArgs();
                    e.StartIndex = nStart;

                    this.SetStart(this, e);
                }

                for (int i = 0; i < aPath.Count; i++)
                {
                    if (bGetRecord == true)
                    {
                        string strXml = "";
                        string strMetaData = "";
                        byte[] timestamp = null;
                        string strOutputPath = "";

                        long lRet = channel.GetRes(aPath[i],
                            out strXml,
                            out strMetaData,
                            out timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;

                        if (this.ItemLoad != null)
                        {
                            ItemLoadEventArgs e = new ItemLoadEventArgs();
                            e.Path = aPath[i];
                            e.Index = i;
                            e.Count = aPath.Count;
                            e.Xml = strXml;
                            e.Timestamp = timestamp;
                            e.TotalCount = (int)lHitCount;

                            this.ItemLoad(this, e);
                        }
                    }
                    else
                    {
                        if (this.ItemLoad != null)
                        {
                            ItemLoadEventArgs e = new ItemLoadEventArgs();
                            e.Path = aPath[i];
                            e.Index = i;
                            e.Count = aPath.Count;
                            e.Xml = "";
                            e.Timestamp = null;
                            e.TotalCount = (int)lHitCount;

                            this.ItemLoad(this, e);
                        }
                    }
                }

                return 1;   // �ҵ�
            }

            nStart = -1;
            strError = "·��Ϊ '"+strCommentRecPath+"' �ļ�¼�ڽ������û���ҵ�";
            return 0;   // û���ҵ�
        ERROR1:
            return -1;
        }
#endif
    }

#if NO
    /// <summary>
    /// ����Ϣ�����¼�
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ItemLoadEventHandler(object sender,
ItemLoadEventArgs e);

    /// <summary>
    /// ����Ϣ�����¼��Ĳ���
    /// </summary>
    public class ItemLoadEventArgs : EventArgs
    {
        /// <summary>
        /// ��¼ȫ·����
        /// </summary>
        public string Path = "";

        public int Index = -1;  // �����ɲ��¼�е�˳��,��0��ʼ����

        public int Count = 0;   // ������ ����Index�漰�ķ�Χ

        public int TotalCount = 0;  // �������е���������// 2010/11/9

        public string Xml = ""; // ��¼

        public byte[] Timestamp = null; // 2010/11/8
    }

    public delegate void SetStartEventHandler(object sender,
SetStartEventArgs e);

    /// <summary>
    /// ����Ϣ�����¼��Ĳ���
    /// </summary>
    public class SetStartEventArgs : EventArgs
    {
        public int StartIndex = -1; 
    }
#endif

    public class SessionTime
    {
        public DateTime CreateTime = DateTime.Now;
        public DateTime LastUsedTime = DateTime.Now;
        // public string SessionID = "";
        // TODO: �Ƿ��¼����Ҳ������ͨ���Ŀ�������ʱ��
    }

    public class SessionTable : Hashtable
    {
        ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();
        static int m_nLockTimeout = 5000;	// 5000=5��

        int _nMaxCount = 10000;

        /// <summary>
        /// ���������� dp2Library ��ǰ�˻�������
        /// </summary>
        public int MaxClients = SessionInfo.DEFAULT_MAX_CLIENTS; // -1 ��ʾ������ (0 ��ʾ���� localhost ����һ�Ų�������)

        Hashtable _ipTable = new Hashtable();   // IP -- Session ���� ���ձ�

#if NO
        Hashtable _ipNullTable = new Hashtable();   // IP -- (û��Session��)Channel ���� ���ձ�

        public void IncNullIpCount(string strIP, int nDelta)
        {
            lock (_ipNullTable)
            {
                long v = 0;
                if (this._ipNullTable.ContainsKey(strIP) == true)
                    v = (long)this._ipNullTable[strIP];
                this._ipNullTable[strIP] = v + nDelta;
            }
        }
#endif

        public void IncNullIpCount(string strIP, int nDelta)
        {
            return;

            if (this.m_lock.TryEnterWriteLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {
                _incIpCount(strIP, nDelta);
            }
            finally
            {
                this.m_lock.ExitWriteLock();
            }
        }


#if NO
        public void PrepareNullSession( // LibraryApplication app,
    string strSessionID,
    string strIP,
    string strVia)
        {
            if (strSessionID == null)
                return;

            if (this.m_lock.TryEnterReadLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {
                if (this.ContainsKey(strSessionID) == true)
                    return;
            }
            finally
            {
                this.m_lock.ExitReadLock();
            }

            if (this.Count > _nMaxCount)
                throw new ApplicationException("Session �������� " + _nMaxCount.ToString());

            if (this.m_lock.TryEnterWriteLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {
                this[strSessionID] = null;
                IncIpCount(strIP, 1);
            }
            finally
            {
                this.m_lock.ExitWriteLock();
            }
        }

        public void DeleteSession(string strSessionID, string strIP)
        {
            if (strSessionID == null)
                return;

            SessionInfo sessioninfo = null;

            if (this.m_lock.TryEnterWriteLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {
                // this.Remove(sessioninfo.SessionTime.SessionID);
                sessioninfo = (SessionInfo)this[strSessionID];
                this.Remove(strSessionID);
                IncIpCount(strIP, -1);
            }
            finally
            {
                this.m_lock.ExitWriteLock();
            }

            if (sessioninfo != null)
                sessioninfo.CloseSession();
        }
#endif
        public int MaxSessionsPerIp = 50;
        public int MaxSessionsLocalHost = 150;

        public SessionInfo PrepareSession(LibraryApplication app,
            string strSessionID,
            string strIP,
            string strVia)
        {
            SessionInfo sessioninfo = null;

            if (this.m_lock.TryEnterReadLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {
                sessioninfo = (SessionInfo)this[strSessionID];
            }
            finally
            {
                this.m_lock.ExitReadLock();
            }

            if (sessioninfo != null)
            {
                Debug.Assert(sessioninfo.SessionTime != null, "");
                sessioninfo.SessionTime.LastUsedTime = DateTime.Now;
#if NO
                if (sessioninfo.SessionTime.SessionID != strSessionID)
                {
                    Debug.Assert(false, "");
                    sessioninfo.SessionTime.SessionID = strSessionID;
                }
#endif
                if (sessioninfo.SessionID != strSessionID)
                {
                    Debug.Assert(false, "");
                    sessioninfo.SessionID = strSessionID;
                }
                return sessioninfo;
            }


            if (this.Count > _nMaxCount)
                throw new ApplicationException("Session �������� " + _nMaxCount.ToString());

            sessioninfo = new SessionInfo(app, strSessionID, strIP, strVia);
            sessioninfo.SessionTime = new SessionTime();
#if NO
            sessioninfo.SessionTime.SessionID = strSessionID;
#endif

            if (this.m_lock.TryEnterWriteLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {
                long v = _incIpCount(strIP, 1);

                int nMax = this.MaxSessionsPerIp;
                // if (strIP == "::1" || strIP == "127.0.0.1" || strIP == "localhost")
                if (IsLocalhost(strIP) == true)
                    nMax = this.MaxSessionsLocalHost;

                if (v >= nMax)
                {
                    // ע�� Session �Ƿ� Dispose() ?
                    _incIpCount(strIP, -1);
                    throw new OutofSessionException("Session ��Դ���㣬������� " + this.MaxSessionsPerIp.ToString());
                }

                // û�г������Ĳż���
                this[strSessionID] = sessioninfo;

                return sessioninfo;
            }
            finally
            {
                this.m_lock.ExitWriteLock();
            }
        }

        public int CloseSessionByClientIP(string strClientIP)
        {
            List<string> remove_keys = new List<string>();

            if (this.m_lock.TryEnterReadLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {
                foreach (string key in this.Keys)
                {
                    SessionInfo info = (SessionInfo)this[key];

                    if (info == null)
                        continue;

                    if (info.ClientIP == strClientIP)
                    {
                        remove_keys.Add(key);   // ���ﲻ��ɾ������Ϊ foreach ��Ҫ��ö����
                    }
                }
            }
            finally
            {
                this.m_lock.ExitReadLock();
            }

            if (remove_keys.Count == 0)
                return 0;   // û���ҵ�

            int nCount = 0;
            List<SessionInfo> delete_sessions = new List<SessionInfo>();
            if (this.m_lock.TryEnterWriteLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {

                foreach (string key in remove_keys)
                {
                    SessionInfo sessioninfo = (SessionInfo)this[key];
                    if (sessioninfo == null)
                        continue;

                    // DeleteSession(sessioninfo, false);

                    // �� sessionid �� hashtable �����ϵ
                    this.Remove(key);

                    delete_sessions.Add(sessioninfo);

                    if (string.IsNullOrEmpty(sessioninfo.ClientIP) == false)
                    {
                        _incIpCount(sessioninfo.ClientIP, -1);
                        sessioninfo.ClientIP = "";
                    }

                    nCount++;
                }
            }
            finally
            {
                this.m_lock.ExitWriteLock();
            }

            // �� CloseSession ����������Χ���棬��Ҫ���뾡������������ʱ��
            foreach (SessionInfo info in delete_sessions)
            {
                info.CloseSession();
            }
            return nCount;
        }

        public bool CloseSessionBySessionID(string strSessionID)
        {
            SessionInfo sessioninfo = null;

            if (this.m_lock.TryEnterReadLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {
                sessioninfo = (SessionInfo)this[strSessionID];
            }
            finally
            {
                this.m_lock.ExitReadLock();
            }

            if (sessioninfo == null)
                return false;   // û���ҵ�

            DeleteSession(sessioninfo);

            return true;
        }

        public void DeleteSession(SessionInfo sessioninfo,
            bool bLock = true)
        {
            if (sessioninfo == null)
                return;

            if (bLock == true)
            {
                if (this.m_lock.TryEnterWriteLock(m_nLockTimeout) == false)
                    throw new ApplicationException("���������г�ʱ");
            }
            try
            {
                // this.Remove(sessioninfo.SessionTime.SessionID);
                this.Remove(sessioninfo.SessionID);
                if (string.IsNullOrEmpty(sessioninfo.ClientIP) == false)
                {
                    _incIpCount(sessioninfo.ClientIP, -1);
                    sessioninfo.ClientIP = "";   // ��������ε���ʱ�ظ���ȥ ip ����
                }
            }
            finally
            {
                if (bLock == true)
                    this.m_lock.ExitWriteLock();
            }

            sessioninfo.CloseSession();
        }

        public bool IsFull
        {
            get
            {
                if (this.Count >= _nMaxCount)
                    return true;

                return false;
            }
        }

        public void CleanSessions(TimeSpan delta)
        {
            List<string> remove_keys = new List<string>();

            // �����������谭һ���Է���
            if (this.m_lock.TryEnterReadLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {
                foreach (string key in this.Keys)
                {
                    SessionInfo info = (SessionInfo)this[key];

                    if (info == null)
                        continue;

                    if (info.NeedAutoClean == false)
                        continue;

                    if (info.SessionTime == null)
                    {
                        Debug.Assert(false, "");
                        continue;
                    }

                    if ((DateTime.Now - info.SessionTime.LastUsedTime) >= delta)
                    {
                        remove_keys.Add(key);   // ���ﲻ��ɾ������Ϊ foreach ��Ҫ��ö����
                    }
                }
            }
            finally
            {
                this.m_lock.ExitReadLock();
            }

            if (remove_keys.Count == 0)
                return;

            // ��ΪҪɾ��ĳЩԪ�أ�������д����
            List<SessionInfo> delete_sessions = new List<SessionInfo>();
            if (this.m_lock.TryEnterWriteLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {            // 2013.11.1
                foreach (string key in remove_keys)
                {
                    SessionInfo info = (SessionInfo)this[key];
                    if (info == null)
                        continue;   // sessionid û���ҵ���Ӧ�� Session ����

                    // �� sessionid �� hashtable �����ϵ
                    this.Remove(key);

                    delete_sessions.Add(info);

                    if (string.IsNullOrEmpty(info.ClientIP) == false)
                    {
                        _incIpCount(info.ClientIP, -1);
                        info.ClientIP = "";
                    }
                }
            }
            finally
            {
                this.m_lock.ExitWriteLock();
            }

            // �� CloseSession ����������Χ���棬��Ҫ���뾡������������ʱ��
            foreach (SessionInfo info in delete_sessions)
            {
                info.CloseSession();
            }
        }

        // ��� IP �������������޶���׳��쳣
        public long IncIpCount(string strIP, int nDelta)
        {
            if (this.m_lock.TryEnterWriteLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {
                return _incIpCount(strIP, nDelta);
            }
            finally
            {
                this.m_lock.ExitWriteLock();
            }
        }

        public static bool IsLocalhost(string strIP)
        {
            if (strIP == "::1" || strIP == "127.0.0.1" || strIP == "localhost")
                return true;
            return false;
        }

        // ���� IP ͳ������
        // ��� IP �������������޶���׳��쳣
        // parameters:
        //      strIP   ǰ�˻����� IP ��ַ�������ڸ����ж��Ƿ񳬹� MaxClients��localhost �ǲ��������ڵ�
        long _incIpCount(string strIP, int nDelta)
        {
            // this.MaxClients = 0;    // test

            long v = 0;
            if (this._ipTable.ContainsKey(strIP) == true)
                v = (long)this._ipTable[strIP];
            else
            {
                if (this.Count > _nMaxCount
                    && v + nDelta != 0)
                    throw new OutofSessionException("IP ��Ŀ�������� " + _nMaxCount.ToString());

                // �ж�ǰ�˻���̨���Ƿ񳬹��������� 2014/8/23
                if (this.MaxClients != -1
                    && IsLocalhost(strIP) == false
                    && this.GetClientIpAmount() >= this.MaxClients
                    && v + nDelta != 0)
                    throw new OutofClientsException("ǰ�˻��������Ѿ��ﵽ " + this.GetClientIpAmount().ToString() + " �� ( ����IP: " + StringUtil.MakePathList(GetIpList(), ", ") + " ��ͼ�����IP: " + strIP + ")�������ͷų�ͨ��Ȼ�����·���");

            }

            if (v + nDelta == 0)
                this._ipTable.Remove(strIP); // ��ʱ���߼�����Ϊ 0 ����Ŀ������ hashtable �ߴ�̫��
            else
                this._ipTable[strIP] = v + nDelta;

            return v;   // ��������ǰ������
        }

        // ��õ�ǰ���� localhost ����� IP ����
        int GetClientIpAmount()
        {
            if (this._ipTable.Count == 0)
                return 0;

            // �ų� localhost
            int nDelta = 0;
            if (this._ipTable.ContainsKey("::1") == true)
                nDelta -= 1;
            if (this._ipTable.ContainsKey("127.0.0.1") == true)
                nDelta -= 1;
            if (this._ipTable.ContainsKey("localhost") == true)
                nDelta -= 1;

            return this._ipTable.Count + nDelta;
        }

        // ��õ�ǰ����ʹ�õ� IP �б�Ϊ������ʾ��;������ localhost ���� (δ����)
        List<string> GetIpList()
        {
            List<string> results = new List<string>();
            foreach (string ip in this._ipTable.Keys)
            {
                if (IsLocalhost(ip) == true)
                    results.Add("localhost(δ����)");
                else
                    results.Add(ip);
            }

            return results;
        }

        // ���� ip ��ַ�ۼ������ֶ���Ϣ
        void GatherFields(ref List<ChannelInfo> infos)
        {
            List<string> ips = new List<string>();
            foreach (ChannelInfo info in infos)
            {
                ips.Add(info.ClientIP);
            }

            List<ChannelInfo> results = GatherFields(ips);
            int i = 0;
            foreach (string ip in ips)
            {
                ChannelInfo info = infos[i];
                foreach(ChannelInfo result in results)
                {
                    if (result.ClientIP == ip)
                    {
                        result.Count = info.Count;
                        infos[i] = result;
                        break;
                    }
                }

                i++;
            }

        }

        public class ChannelInfoComparer : IComparer<ChannelInfo>
        {
            int IComparer<ChannelInfo>.Compare(ChannelInfo x, ChannelInfo y)
            {
                // ���Ȩֵ��ͬ����������š����С�ĸ���ǰ
                return string.Compare(x.ClientIP, y.ClientIP);
            }
        }

        List<ChannelInfo> GatherFields(List<string> ips)
        {
            List<ChannelInfo> results = new List<ChannelInfo>();

            List<ChannelInfo> infos = new List<ChannelInfo>();

#if NO
            if (this.m_lock.TryEnterReadLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {
#endif
                foreach (string sessionid in this.Keys)
                {
                    SessionInfo session = (SessionInfo)this[sessionid];
                    if (session == null)
                        continue;

                    if (ips.IndexOf(session.ClientIP) == -1)
                        continue;

                    ChannelInfo info = new ChannelInfo();
                    info.SessionID = session.SessionID;
                    info.ClientIP = session.ClientIP;
                    info.UserName = session.UserID;
                    info.LibraryCode = session.LibraryCodeList;
                    info.Via = session.Via;
                    info.Count = 1;
                    info.CallCount = session.CallCount;
                    if (session.Account != null)
                        info.Location = session.Account.Location;

                    infos.Add(info);
                }
#if NO
            }
            finally
            {
                this.m_lock.ExitReadLock();
            }
#endif

            // ���� IP ��ַ����
            infos.Sort(new ChannelInfoComparer());

            List<string> usernames = new List<string>();
            List<string> locations = new List<string>();
            List<string> librarycodes = new List<string>();
            List<string> vias = new List<string>();
            ChannelInfo current = null;
            foreach (ChannelInfo info in infos)
            {
                if (current != null && info.ClientIP != current.ClientIP)
                {
                    // ���һ�����
                    ChannelInfo result = new ChannelInfo();
                    result.ClientIP = current.ClientIP;

                    StringUtil.RemoveDupNoSort(ref usernames);
                    result.UserName = StringUtil.MakePathList(usernames);

                    StringUtil.RemoveDupNoSort(ref locations);
                    result.Location = StringUtil.MakePathList(locations);

                    StringUtil.RemoveDupNoSort(ref librarycodes);
                    result.LibraryCode = StringUtil.MakePathList(librarycodes);

                    StringUtil.RemoveDupNoSort(ref vias);
                    result.Via = StringUtil.MakePathList(vias);

                    results.Add(result);

                    current = info;

                    usernames.Clear();
                    locations.Clear();
                    librarycodes.Clear();
                    vias.Clear();
                }

                usernames.Add(info.UserName);
                locations.Add(info.Location);
                librarycodes.Add(info.LibraryCode);
                vias.Add(info.Via);

                if (current == null)
                    current = info;
            }

            if (current != null && usernames.Count > 0)
            {
                // ������һ��
                ChannelInfo result = new ChannelInfo();
                result.ClientIP = current.ClientIP;

                StringUtil.RemoveDupNoSort(ref usernames);
                result.UserName = StringUtil.MakePathList(usernames);

                StringUtil.RemoveDupNoSort(ref locations);
                result.Location = StringUtil.MakePathList(locations);

                StringUtil.RemoveDupNoSort(ref librarycodes);
                result.LibraryCode = StringUtil.MakePathList(librarycodes);

                StringUtil.RemoveDupNoSort(ref vias);
                result.Via = StringUtil.MakePathList(vias);

                results.Add(result);
            }

            return results;
        }

        // �г�ָ����ͨ����Ϣ
        public int ListChannels(
            string strClientIP,
            string strUserName,
            string strStyle,
            out List<ChannelInfo> infos,
            out string strError)
        {
            strError = "";
            infos = new List<ChannelInfo>();

            if (this.m_lock.TryEnterReadLock(m_nLockTimeout) == false)
                throw new ApplicationException("���������г�ʱ");
            try
            {

                // ���� IP ��ַ�ۼ�
                // strClientIP ����������ɸѡ
                if (strStyle == "ip-count")
                {

                    foreach (string ip in this._ipTable.Keys)
                    {
                        if (string.IsNullOrEmpty(strClientIP) == true
                            || strClientIP == "*")
                        {
                        }
                        else if (ip != strClientIP)
                            continue;

                        ChannelInfo info = new ChannelInfo();
                        info.ClientIP = ip;
                        // TODO: UserName �����ۻ������ù���
                        info.Count = (long)this._ipTable[ip];

                        infos.Add(info);
                    }

                    // ���� ip ��ַ�ۼ������ֶ���Ϣ
                    if (infos.Count > 0)
                        GatherFields(ref infos);


#if NO
                    // �г�û�з��� Session ��ͨ������
                    foreach (string ip in this._ipNullTable.Keys)
                    {
                        if (string.IsNullOrEmpty(strClientIP) == true
                            || strClientIP == "*")
                        {
                        }
                        else if (ip != strClientIP)
                            continue;
                        ChannelInfo info = new ChannelInfo();
                        info.ClientIP = ip;
                        info.Location = "<null>";
                        // TODO: UserName �����ۻ������ù���
                        info.Count = (long)this._ipNullTable[ip];

                        infos.Add(info);
                    }
#endif

                    return 0;
                }

                // ȫ���г�
                // strClientIP strUserName ����������ɸѡ
                if (string.IsNullOrEmpty(strStyle) == true)
                {
                    foreach (string sessionid in this.Keys)
                    {
                        SessionInfo session = (SessionInfo)this[sessionid];

                        if (session == null)
                        {
#if NO
                            ChannelInfo info = new ChannelInfo();
                            info.SessionID = sessionid;
                            info.Location = "<null>";

                            infos.Add(info);
#endif
                            continue;
                        }

                        if (string.IsNullOrEmpty(strClientIP) == true
                            || strClientIP == "*")
                        {
                        }
                        else if (session.ClientIP != strClientIP)
                            continue;

                        if (string.IsNullOrEmpty(strUserName) == true
    || strUserName == "*")
                        {
                        }
                        else if (session.UserID != strUserName)
                            continue;

                        ChannelInfo info = new ChannelInfo();
                        info.SessionID = session.SessionID;
                        info.ClientIP = session.ClientIP;
                        info.UserName = session.UserID;
                        info.LibraryCode = session.LibraryCodeList;
                        info.Via = session.Via;
                        info.Count = 1;
                        info.CallCount = session.CallCount;
                        if (session.Account != null)
                            info.Location = session.Account.Location;

                        infos.Add(info);
                    }

                    return 0;
                }
            }
            finally
            {
                this.m_lock.ExitReadLock();
            }

            return 0;
        }
    }

    /// <summary>
    /// Session ��Դ�����쳣
    /// </summary>
    public class OutofSessionException : Exception
    {
        public OutofSessionException(string strText)
            : base(strText)
        {
        }
    }

    /// <summary>
    /// �����涨ǰ�˻���̨���쳣
    /// </summary>
    public class OutofClientsException : Exception
    {
        public OutofClientsException(string strText)
            : base(strText)
        {
        }
    }

    /// <summary>
    /// ͨѶͨ����Ϣ
    /// </summary>
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class ChannelInfo
    {
        [DataMember]
        public string SessionID = "";    // Session id�� Session Ψһ�ı�ʶ

        [DataMember]
        public string UserName = "";    // �û���

        [DataMember]
        public string ClientIP = "";    // ǰ�� IP

        [DataMember]
        public string Via = "";  // ����ʲôЭ��

        [DataMember]
        public long Count = 0;    // ������Ŀ

        [DataMember]
        public string LibraryCode = ""; // ͼ��ݴ���

        [DataMember]
        public string Location = "";    // ǰ�˵ص�ע��

        [DataMember]
        public long CallCount = 0;    // ͨ�����񱻵��õĴ���
    }
}
