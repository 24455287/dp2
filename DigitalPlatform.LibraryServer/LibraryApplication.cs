// #define OPTIMIZE_API
#define LOG_INFO

using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Web;
using System.Drawing;
using System.Runtime.Serialization;

// using DigitalPlatform.Drawing;

using DigitalPlatform;	// Stop��
using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.MarcDom;
// using DigitalPlatform.Library;  // LoanFilterDocument
using DigitalPlatform.Marc;
using DigitalPlatform.Range;

using DigitalPlatform.Message;
using DigitalPlatform.rms.Client.rmsws_localhost;

namespace DigitalPlatform.LibraryServer
{
    public class StopState
    {
        public bool Stopped = false;

        public void Stop()
        {
            this.Stopped = true;
        }
    }

    /// <summary>
    /// ���Ӧ�ó���ȫ����Ϣ����
    /// ������partial class��дΪ�����ļ�����Сÿ���ļ��ĳߴ�
    /// </summary>
    public partial class LibraryApplication : IDisposable
    {
        //      2.1 (2012/4/5) ��һ�����а汾�ŵİ汾���ص��������˸�����GetIssueInfo() GetOrderInfo() GetCoomentInfo() �޸��˵�һ��������ȥ���˵ڶ�����
        //      2.11 (2012/5/5) ΪListBiblioDbFroms() API������ item order issue ��������
        //      2.12 (2012/5/15) SearchBiblio() API �ԡ�����ʱ�䡱����;�����������⴦��
        //      2.13 (2012/5/16) SearchBiblio() API ͨ��strFromStyle�а���_time�Ӵ���ʶ��ʱ���������
        //      2.14 (2012/8/26) GetRes() API �� nStart ������int�޸�Ϊlong����
        //      2.15 (2012/9/10) ��ʼ���зֹ��û�����
        //      2.16 (2012/9/19) Login() API����out strLibraryCode����
        //      2.17 (2012/11/8) ΪListBiblioDbFroms() API������ amerce invoice ��������
        //      2.18 (2013/2/13) Ϊ�˺� dp2Kernel 2.54 ����
        //      2.19 (2013/12/21) �������� log ��Ϣ
        //      2.20 (2013/12/4) ���� HTML ��ʽ�ܽ��� html:noborrowhistory �������÷� (�ֲ����д�)
        //      2.21 (2013/12/5) ���� HTML ��ʽ�ܽ��� html:noborrowhistory �������÷�
        //      2.22 (2013/12/8) GetReaderInfo() �� strBarcode ����ʹ�� "@barcode:" ��������ʾ������֤������в���
        //      2.23 (2013/12/8) GetSysParameters() ���� cfgs listFileNamesEx; cfgs/get_res_timestamps
        //      2.24 (2013/12/15) Borrow() Return() ���� item ��ʽ���� xml:noborrowhistory; ���� ��ʽ���� summary
        //      2.25 (2013/12/17)  GetReaderInfo() ���� xml:noborrowhistory
        //      2.26 (2013/12/30) SearchBiblio() ��, �� strStyle ������ "class,__class" ����ȷȥ��
        //      2.27 (2014/1/2) SearchBiblbio() �����һ�� formstyle û���ҵ����᷵�� ErrorCode.FromNotFound ������
        //      2.28 (2014/1/15) GetBiblioInfos() API ����ǰ�˷������� XML ��¼��ÿ����¼֮���� <!--> ���
        //      2.29 (2014/3/2) GetCalendar() API ���� strAction �� list getcount get , strName �������������á�Ϊ�� ���ȫ�����ע�� list / getcount ��Ҫʹ�ÿ�ֵ�� strName����ǰ�汾���� list / getcount ʱ����� strName ������Ч���ǻ��ȫ�����룻�� get ��Ч����ֻ�ܻ��һ������  
        //      2.30 (2014/3/17) GetBiblioInfo() �� GetBiblioInfos() API������ʹ�� subcount:??? format
        //      2.31 (2014/4/29) GetOperLogs() ���� level-2 �� SetEntity SetOrder SetIssue SetComment ��¼�У�<oldRecord> ������ parent_id ����
        //      2.32 (2014/9/16) ����ͼ��ݹ��ܣ��������֮����н軹����
        //      2.33 (2014/9/24) Borrow() Return() API ������ @refID: ǰ׺�Ĳ�����������н��黹��
        //      2.34 (2014/10/23) ������ֹ���ʹ������ģʽ
        //      2.35 (2014/11/14) Borrow() API �����蹦������ strReaderBarcode Ϊ��
        //      2.36 (2014/11/15) Login() API �� mac ������������ MAC ��ַ�������߷ָ�
        //      2.37 (2014/11/17) Foregift() �� Hire() ���� API ������������ out ����
        //      2.38 (2014/11/26) ManageDatabase() API �� refresh ���ܣ������Զ������ؽ������������������
        //      2.39 (2015/1/21) CopyBiblioInfo() API ������ strMergeStyle �� strOutputBiblio ������SetBiblioInfo() API ������ onlydeletesubrecord action
        //      2.40 (2015/1/25) Login() API ���Է��� token �ַ���, VerifyReaderPassword() API ������֤ token �ַ�����dp2OPAC ���ʵ���˱����û���¼״̬�Ĺ��ܣ��͵����� SSO ���� dp2OPAC ��¼�Ĺ���
        //      2.41 (2015/1/26) Login() API �����˶���̽����ѭ�������ķ������ܣ�ÿ�ν�ֹ��� IP ʹ�� Login() API 10 ����
        //      2.42 (2015/1/29) GetItemInfo() GetOrderInfo() GetIssueInfo() GetCommentInfo() API ������ strItemXml �����������ü�¼�ļ�������Ϣ
        //      2.43 (2015/1/30) GetItemInfo() API ��һ�������� strItemDbType ��������������ԭ�ȵ� GetItemInfo GetOrderInfo GetIssuInfo GetCommentInfo API ��ȫ�����ܡ����ˣ�GetItemInfo() API ��ȡ������������ API ��Ҫ��ֹ��Ϊ�˱��ּ����ԣ���ʱ����һ��ʱ���⼸�� API
        //      2.44 (2015/4/30) GetSystemParameter() API ������ category=cfgs name=getDataDir �������Ŀ¼����·�� 
        //      2.45 (2015/5/15) �ļ��ϴ��� WriteRes() API ���õ��˳�ʵ��֧�� dp2libraryconsole ǰ�˽����ļ��ϴ��͹�������� 
        //      2.46 (2015/5/18) ���� API ListFile()
        //      2.47 (2015/6/13) GetSystemParameter() API ������ category=arrived name=dbname
        //      2.48 (2015/6/16) GetVersion() API ������ out uid ����
        public static string Version = "2.48";
#if NO
        int m_nRefCount = 0;
        public int AddRef()
        {
            int v = m_nRefCount;
            m_nRefCount++;

            return v;
        }

        public int GetRef()
        {
            return m_nRefCount;
        }

        public int ReleaseRef()
        {
            m_nRefCount--;

            return m_nRefCount;
        }
#endif
        public const string qrkey = "dpqrhello";

        /// <summary>
        /// �Ƿ�Ϊ����״̬
        /// </summary>
        public bool TestMode = false;

        // �洢���ֲ�����Ϣ
        // ΪC#�ű���׼��
        public Hashtable ParamTable = new Hashtable();

        // ��ֹ��̽���빥������ʩ
        public UserNameTable UserNameTable = new UserNameTable("dp2library");

        // Session����
        public SessionTable SessionTable = new SessionTable();

        /// <summary>
        /// ���������� dp2Library ��ǰ�˻�������
        /// </summary>
        public int MaxClients
        {
            get
            {
                return this.SessionTable.MaxClients;
            }
            set
            {
                this.SessionTable.MaxClients = value;
            }
        }

        /// <summary>
        /// �������
        /// "server" ��ʾ��������֤�������Լ������кţ��Ͳ�Ҫ��ǰ����֤ǰ���Լ������к���
        /// </summary>
        public string LicenseType
        {
            get;
            set;
        }

        /// <summary>
        /// ʧЧ��ǰ�� MAC ��ַ����
        /// Key Ϊ MAC ��ַ����д����� Key �� Hashtable ���Ѿ����ڣ����ʾ��� MAC ��ַ�Ѿ�ʧЧ��
        /// </summary>
        public Hashtable ExpireMacTable = new Hashtable();

        /// <summary>
        /// ���ٶ������״̬����
        /// </summary>
        public Garden Garden = new Garden();

        public IssueItemDatabase IssueItemDatabase = null;
        public OrderItemDatabase OrderItemDatabase = null;
        public CommentItemDatabase CommentItemDatabase = null;

        public Semaphore PictureLimit = new Semaphore(10, 10);

        public HangupReason HangupReason = HangupReason.None;

        public bool PauseBatchTask = false; // �Ƿ���ͣ��̨����

        public string DataDir = "";

        public string HostDir = "";

        public string GlobalErrorInfo = ""; // ���ȫ�ֳ�����Ϣ������������ƣ���������ֵ��ʱ������������ģ������ٿ�Application["errorinfo"]�ַ���
        const string EncryptKey = "dp2circulationpassword";
        // http://localhost/dp2bbs/passwordutil.aspx

        string m_strFileName = "";  // library.xml�����ļ�ȫ·��

        // string m_strWebuiFileName = ""; // webui.xml�����ļ�ȫ·��

        public string BinDir = "";	// binĿ¼

        public string CfgDir = "";  // cfgĿ¼

        public string CfgMapDir = "";  // cfgmapĿ¼

        public string LogDir = "";	// �¼���־Ŀ¼

        // public string OperLogDir = "";  // ������־Ŀ¼

        public string ZhengyuanDir = "";    // ��Ԫһ��ͨ����Ŀ¼
        public string DkywDir = "";    // �Ͽ�Զ��һ��ͨ����Ŀ¼
        public string PatronReplicationDir = "";    // ͨ�� ������Ϣͬ�� Ŀ¼

        public string StatisDir = "";   // ͳ���ļ����Ŀ¼

        public string SessionDir = "";  // session��ʱ�ļ�

        public string TempDir = "";  // ����ͨ����ʱ�ļ� 2014/12/5

        public string WsUrl = "";	// dp2rms WebService URL

        public string ManagerUserName = "";
        public string ManagerPassword = "";

        public bool DebugMode = false;
        public string UID = "";

        // ԤԼ������п�ļ���;����Ϣ 2015/5/7
        public BiblioDbFromInfo[] ArrivedDbFroms = null;

        public string ArrivedDbName = "";   // ԤԼ����������ݿ���
        public string ArrivedReserveTimeSpan = "";  // ֪ͨ�����ı���ʱ�䡣��ʱ�䵥λ
        public int OutofReservationThreshold = 10;  // ԤԼ������ٲ�ȡ�κ󣬱��ͷ���ֹԤԼ
        public bool CanReserveOnshelf = true;   // �Ƿ����ԤԼ�ڼ�ͼ��
        public string NotifyDef = "";       // ����֪ͨ�Ķ��塣"15day,50%,70%"

        DefaultThread defaultManagerThread = null; // ȱʡ�����̨����

        // ȫ�����߿⼯��(������������ͨ�Ķ��߿�)
        public List<ReaderDbCfg> ReaderDbs = null;

        public List<ItemDbCfg> ItemDbs = null;

        // Applicationͨ������������������GlobalCfgDom��
        public ReaderWriterLock m_lock = new ReaderWriterLock();

        // ���߼�¼����������̸߳�дͬһ���߼�¼��ɵĹ���
        public RecordLockCollection ReaderLocks = new RecordLockCollection();

        public XmlDocument LibraryCfgDom = null;   // library.xml�����ļ�����

        public Clock Clock = new Clock();

        bool m_bChanged = false;

        FileSystemWatcher watcher = null;

        public CfgsMap CfgsMap = null;

        public BatchTaskCollection BatchTasks = new BatchTaskCollection();

        public MessageCenter MessageCenter = null;
        // public string MessageDbName = "";
        string m_strMessageDbName = "";
        public string MessageDbName
        {
            get
            {
                return m_strMessageDbName;
            }
            set
            {
                m_strMessageDbName = value;
                if (this.MessageCenter != null)
                    this.MessageCenter.MessageDbName = value;
            }
        }


        public string MessageReserveTimeSpan = "365day";  // ��Ϣ�������еı������ޡ���ʱ�䵥λ��ȱʡΪһ��

        public string OpacServerUrl = "";

        // �������ֹ�������
        public string LibraryServerUrl
        {
            get
            {
                return this.OpacServerUrl;
            }
        }

        public AccountTable AccountTable = new AccountTable();

        public VirtualDatabaseCollection vdbs = null;

        public OperLog OperLog = new OperLog();

        public long m_lSeed = 0;

        public string InvoiceDbName = "";   // ��Ʊ���� 2012/11/6

        public string AmerceDbName = "";    // ΥԼ�����

        public string OverdueStyle = "";    // ���ڷ������취 <amerce overdueStyle="..." />

        public KernelDbInfoCollection kdbs = null;

        // ʵ���¼����������̸߳�дͬһʵ���¼, ������������Ų��ع���
        public RecordLockCollection EntityLocks = new RecordLockCollection();

        // ��Ŀ��¼����������̸߳�дͬһ��Ŀ��¼��������ʵ���¼
        public RecordLockCollection BiblioLocks = new RecordLockCollection();

        // ���ؽ��������������̸߳�дͬһ�����
        public RecordLockCollection ResultsetLocks = new RecordLockCollection();

        public Hashtable StopTable = new Hashtable();

        // �ȴ�����Ļ����ļ�
        public List<String> PendingCacheFiles = new List<string>();

        // public CacheBuilder CacheBuilder = null;

        public int SearchMaxResultCount = 5000;

        public Statis Statis = null;

        // public XmlDocument WebUiDom = null;   // webui.xml�����ļ�����

        public bool PassgateWriteToOperLog = true;

        // GetRes() API ��ȡ����Ķ����Ƿ�д�������־
        public bool GetObjectWriteToOperLog = false;

        // 2013/5/24
        // ���ڳ��ɲ����ĸ����Եļ���;��
        public List<string> PatronAdditionalFroms = new List<string>();


        // ���캯��
        public LibraryApplication()
        {
        }

        		// Use C# destructor syntax for finalization code.
		// This destructor will run only if the Dispose method 
		// does not get called.
		// It gives your base class the opportunity to finalize.
		// Do not provide destructors in types derived from this class.
        ~LibraryApplication()      
		{
			// Do not re-create Dispose clean-up code here.
			// Calling Dispose(false) is optimal in terms of
			// readability and maintainability.
			Dispose(false);
		}


		// Implement IDisposable.
		// Do not make this method virtual.
		// A derived class should not be able to override this method.
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue 
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

        bool disposed = false;

		// Dispose(bool disposing) executes in two distinct scenarios.
		// If disposing equals true, the method has been called directly
		// or indirectly by a user's code. Managed and unmanaged resources
		// can be disposed.
		// If disposing equals false, the method has been called by the 
		// runtime from inside the finalizer and you should not reference 
		// other objects. Only unmanaged resources can be disposed.
		private void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if(!this.disposed)
			{
				// If disposing equals true, dispose all managed 
				// and unmanaged resources.
				if(disposing)
				{
					// Dispose managed resources.

                    // ������һ�����⣺������������������Close()
					// this.Close();
				}

                this.Close();   // 2007/6/8 �ƶ��������

             
				/*
				// Call the appropriate methods to clean up 
				// unmanaged resources here.
				// If disposing is false, 
				// only the following code is executed.
				CloseHandle(handle);
				handle = IntPtr.Zero;            
				*/
			}
			disposed = true;         
		}

        public int LoadCfg(
            bool bReload,
            string strDataDir,
            string strHostDir,  // Ϊ�˽ű�����ʱ����dllĿ¼
            out string strError)
        {
            strError = "";
            int nRet = 0;
            LibraryApplication app = this;  // new CirculationApplication();

            try
            {
                DateTime start = DateTime.Now;

                this.DataDir = strDataDir;
                this.HostDir = strHostDir;

                string strFileName = PathUtil.MergePath(strDataDir, "library.xml");
                string strBinDir = strHostDir;  //  PathUtil.MergePath(strHostDir, "bin");
                string strCfgDir = PathUtil.MergePath(strDataDir, "cfgs");
                string strCfgMapDir = PathUtil.MergePath(strDataDir, "cfgsmap");
                string strLogDir = PathUtil.MergePath(strDataDir, "log");
                string strOperLogDir = PathUtil.MergePath(strDataDir, "operlog");
                string strZhengyuanDir = PathUtil.MergePath(strDataDir, "zhengyuan");
                string strDkywDir = PathUtil.MergePath(strDataDir, "dkyw");
                string strPatronReplicationDir = PathUtil.MergePath(strDataDir, "patronreplication");
                string strStatisDir = PathUtil.MergePath(strDataDir, "statis");
                string strSessionDir = PathUtil.MergePath(strDataDir, "session");
                string strColumnDir = PathUtil.MergePath(strDataDir, "column");
                string strTempDir = PathUtil.MergePath(strDataDir, "temp");

                app.m_strFileName = strFileName;

                app.CfgDir = strCfgDir;

                app.CfgMapDir = strCfgMapDir;
                PathUtil.CreateDirIfNeed(app.CfgMapDir);	// ȷ��Ŀ¼����


                // log
                app.LogDir = strLogDir;	// ��־�洢Ŀ¼
                PathUtil.CreateDirIfNeed(app.LogDir);	// ȷ��Ŀ¼����

                // zhengyuan һ��ͨ
                app.ZhengyuanDir = strZhengyuanDir;
                PathUtil.CreateDirIfNeed(app.ZhengyuanDir);	// ȷ��Ŀ¼����

                // dkyw һ��ͨ
                app.DkywDir = strDkywDir;
                PathUtil.CreateDirIfNeed(app.DkywDir);	// ȷ��Ŀ¼����

                // patron replication
                app.PatronReplicationDir = strPatronReplicationDir;
                PathUtil.CreateDirIfNeed(app.PatronReplicationDir);	// ȷ��Ŀ¼����


                // statis ͳ���ļ�
                app.StatisDir = strStatisDir;
                PathUtil.CreateDirIfNeed(app.StatisDir);	// ȷ��Ŀ¼����

                // session��ʱ�ļ�
                app.SessionDir = strSessionDir;
                PathUtil.CreateDirIfNeed(app.SessionDir);	// ȷ��Ŀ¼����

                if (bReload == false)
                    CleanSessionDir(this.SessionDir);

                // ������ʱ�ļ�
                app.TempDir = strTempDir;
                PathUtil.CreateDirIfNeed(app.TempDir);	// ȷ��Ŀ¼����

                if (bReload == false)
                {
#if NO
                    try
                    {
                        string strTempFileName = Path.GetTempFileName();
                        File.Delete(strTempFileName);
                        string strTempDir1 = Path.GetDirectoryName(strTempFileName);
                        long count = 0;
                        long size = PathUtil.GetAllFileSize(strTempDir1, ref count);
                        app.WriteErrorLog("ϵͳ��ʱ�ļ�Ŀ¼ " + Path.GetTempPath() + " �ڵ�ȫ����ʱ�ļ��ߴ�Ϊ " + size.ToString() + "�� �ļ�����Ϊ " + count.ToString());
                    }
                    catch
                    {
                    }
#endif

                    if (PathUtil.ClearDir(app.TempDir) == false)
                        app.WriteErrorLog("�����ʱ�ļ�Ŀ¼ " + app.TempDir + " ʱ����");
                }

                this.InitialLoginCache();

                if (bReload == false)
                {
                    if (app.HasAppBeenKilled() == true)
                    {
                        app.WriteErrorLog("*** ����library application��ǰ����������ֹ ***");
                    }
                }

                this.WriteErrorLog("*********");

                if (bReload == true)
                    app.WriteErrorLog("library (" + Version + ") application ��ʼ����װ�� " + this.m_strFileName);
                else
                    app.WriteErrorLog("library (" + Version + ") application ��ʼ��ʼ����");

                //
#if NO
            if (bReload == false)
            {
                app.m_strWebuiFileName = PathUtil.MergePath(strDataDir, "webui.xml");
                // string strWebUiFileName = PathUtil.MergePath(strDataDir, "webui.xml");
                nRet = LoadWebuiCfgDom(out strError);
                if (nRet == -1)
                {
                    // strError = "װ�������ļ�-- '" + strWebUiFileName + "'ʱ��������ԭ��" + ex.Message;
                    app.WriteErrorLog(strError);
                    goto ERROR1;
                }
            }
#endif

#if LOG_INFO
                app.WriteErrorLog("INFO: ��ʼװ�� " + strFileName + " �� XMLDOM");
#endif

                //

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.Load(strFileName);
                }
                catch (FileNotFoundException)
                {
                    strError = "file '" + strFileName + "' not found ...";
                    goto ERROR1;
                }
                catch (Exception ex)
                {
                    strError = "װ�������ļ�-- '" + strFileName + "' ʱ�������󣬴������ͣ�" + ex.GetType().ToString() + "��ԭ��" + ex.Message;
                    app.WriteErrorLog(strError);
                    // throw ex;
                    goto ERROR1;
                }

                app.LibraryCfgDom = dom;

#if LOG_INFO
                app.WriteErrorLog("INFO: ��ʼ���ڴ����");
#endif

                // *** �����ڴ�Ĳ�����ʼ
                // ע���޸�����Щ�����Ľṹ�󣬱�����Ӧ�޸�Save()���������Ƭ��

                // 2011/1/7
                bool bValue = false;
                DomUtil.GetBooleanParam(app.LibraryCfgDom.DocumentElement,
                    "debugMode",
                    false,
                    out bValue,
                    out strError);
                this.DebugMode = bValue;

                // 2013/4/10 
                // uid
                this.UID = app.LibraryCfgDom.DocumentElement.GetAttribute("uid");
                if (string.IsNullOrEmpty(this.UID) == true)
                {
                    this.UID = Guid.NewGuid().ToString();
                    this.Changed = true;
                    WriteErrorLog("�Զ�Ϊ library.xml ��� uid '" + this.UID + "'");
                }

                // �ں˲���
                // Ԫ��<rmsserver>
                // ����url/username/password
                XmlNode node = dom.DocumentElement.SelectSingleNode("//rmsserver");
                if (node != null)
                {
                    app.WsUrl = DomUtil.GetAttr(node, "url");

                    if (app.WsUrl.IndexOf(".asmx") != -1)
                    {
                        strError = "װ�������ļ� '" + strFileName + "' �����з�������: <rmsserver>Ԫ��url�����е� dp2�ں� ������URL '" + app.WsUrl + "' ����ȷ��Ӧ��Ϊ��.asmx��̬�ĵ�ַ...";
                        app.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    app.ManagerUserName = DomUtil.GetAttr(node,
                        "username");

                    try
                    {
                        app.ManagerPassword = Cryptography.Decrypt(
                            DomUtil.GetAttr(node, "password"),
                            EncryptKey);
                    }
                    catch
                    {
                        strError = "<rmsserver>Ԫ��password�����е��������ò���ȷ";
                        // throw new Exception();
                        goto ERROR1;
                    }

                    CfgsMap = new CfgsMap(this.CfgMapDir,
                        this.WsUrl);
                    CfgsMap.Clear();
                }

                // ԤԼ����
                // Ԫ��<arrived>
                // ����dbname/reserveTimeSpan/outofReservationThreshold/canReserveOnshelf
                node = dom.DocumentElement.SelectSingleNode("//arrived");
                if (node != null)
                {
                    app.ArrivedDbName = DomUtil.GetAttr(node, "dbname");
                    app.ArrivedReserveTimeSpan = DomUtil.GetAttr(node, "reserveTimeSpan");

                    int nValue = 0;
                    nRet = DomUtil.GetIntegerParam(node,
                        "outofReservationThreshold",
                        10,
                        out nValue,
                        out strError);
                    if (nRet == -1)
                    {
                        app.WriteErrorLog("Ԫ��<arrived>����outofReservationThreshold����ʱ��������: " + strError);
                        goto ERROR1;
                    }

                    app.OutofReservationThreshold = nValue;

                    /*
                    string strOutofThreshold = DomUtil.GetAttr(node, "outofReservationThreshold");
                    if (String.IsNullOrEmpty(strOutofThreshold) == true)
                        strOutofThreshold = "10";   // ȱʡֵ
                    try
                    {
                        app.OutofReservationThreshold = Convert.ToInt32(strOutofThreshold);
                    }
                    catch
                    {
                        strError = "<arrived>Ԫ�ص�outofReservationThreshold����ֵ���Ϸ���ӦΪ�����֡�";
                        app.WriteErrorLog(strError);
                        goto ERROR1;
                    }
                     * */

                    bValue = false;
                    nRet = DomUtil.GetBooleanParam(node,
                        "canReserveOnshelf",
                        true,
                        out bValue,
                        out strError);
                    if (nRet == -1)
                    {
                        app.WriteErrorLog("Ԫ��<arrived>����canReserveOnshelf����ʱ��������: " + strError);
                        goto ERROR1;
                    }

                    this.CanReserveOnshelf = bValue;

                    /*
                    string strCanReserveOnshelf = DomUtil.GetAttr(node, "canReserveOnshelf");
                    this.CanReserveOnshelf = ToBoolean(strCanReserveOnshelf,
                        true);
                     * */

                }

                // 2013/9/24
                // ��������֪ͨ����
                // Ԫ�� <monitors/readersMonitor>
                // ���� notifyDef
                node = dom.DocumentElement.SelectSingleNode("monitors/readersMonitor");
                if (node != null)
                {
                    // ����֪ͨ�Ķ���
                    app.NotifyDef = DomUtil.GetAttr(node, "notifyDef");
                }


                // <circulation>
                node = dom.DocumentElement.SelectSingleNode("circulation");
                if (node != null)
                {
                    string strList = DomUtil.GetAttr(node, "patronAdditionalFroms");
                    if (string.IsNullOrEmpty(strList) == false)
                    {
                        this.PatronAdditionalFroms = StringUtil.SplitList(strList);
                    }

                    int v = 0;
                    nRet = DomUtil.GetIntegerParam(node,
                        "maxPatronHistoryItems",
                        100,
                        out v,
                        out strError);
                    if (nRet == -1)
                        app.WriteErrorLog(strError);
                    this.MaxPatronHistoryItems = v;

                    nRet = DomUtil.GetIntegerParam(node,
    "maxItemHistoryItems",
    100,
    out v,
    out strError);
                    if (nRet == -1)
                        app.WriteErrorLog(strError);
                    this.MaxItemHistoryItems = v;

                    this.VerifyBarcode = DomUtil.GetBooleanParam(node, "verifyBarcode", false);

                    this.AcceptBlankItemBarcode = DomUtil.GetBooleanParam(node, "acceptBlankItemBarcode", true);

                    this.AcceptBlankReaderBarcode = DomUtil.GetBooleanParam(node, "acceptBlankReaderBarcode", true);

                    this.VerifyBookType = DomUtil.GetBooleanParam(node, "verifyBookType", false);
                    this.VerifyReaderType = DomUtil.GetBooleanParam(node, "verifyReaderType", false);
                    this.BorrowCheckOverdue = DomUtil.GetBooleanParam(node, "borrowCheckOverdue", true);
                }

                // <channel>
                node = dom.DocumentElement.SelectSingleNode("channel");
                if (node != null)
                {
                    int v = 0;
                    nRet = DomUtil.GetIntegerParam(node,
                        "maxChannelsPerIP",
                        50,
                        out v,
                        out strError);
                    if (nRet == -1)
                        app.WriteErrorLog(strError);
                    if (this.SessionTable != null)
                        this.SessionTable.MaxSessionsPerIp = v;

                    nRet = DomUtil.GetIntegerParam(node,
    "maxChannelsLocalhost",
    150,
    out v,
    out strError);
                    if (nRet == -1)
                        app.WriteErrorLog(strError);
                    if (this.SessionTable != null)
                        this.SessionTable.MaxSessionsLocalHost = v;
                }

                // <cataloging>
                node = dom.DocumentElement.SelectSingleNode("cataloging");
                if (node != null)
                {
                    // �Ƿ�����ɾ�������¼���¼����Ŀ��¼
                    bValue = true;
                    nRet = DomUtil.GetBooleanParam(node,
                        "deleteBiblioSubRecords",
                        true,
                        out bValue,
                        out strError);
                    if (nRet == -1)
                        app.WriteErrorLog(strError);
                    this.DeleteBiblioSubRecords = bValue;
                }

                // ��ݵǼ�
                // Ԫ��<passgate>
                // ����writeOperLog
                node = dom.DocumentElement.SelectSingleNode("//passgate");
                if (node != null)
                {
                    string strWriteOperLog = DomUtil.GetAttr(node, "writeOperLog");

                    this.PassgateWriteToOperLog = ToBoolean(strWriteOperLog,
                        true);
                }

                // �������
                // Ԫ��<object>
                // ���� writeOperLog
                node = dom.DocumentElement.SelectSingleNode("//object");
                if (node != null)
                {
                    string strWriteOperLog = DomUtil.GetAttr(node, "writeGetResOperLog");

                    this.GetObjectWriteToOperLog = ToBoolean(strWriteOperLog,
                        false);
                }
                // ��Ϣ
                // Ԫ��<message>
                // ����dbname/reserveTimeSpan
                node = dom.DocumentElement.SelectSingleNode("//message");
                if (node != null)
                {
                    app.MessageDbName = DomUtil.GetAttr(node, "dbname");
                    app.MessageReserveTimeSpan = DomUtil.GetAttr(node, "reserveTimeSpan");

                    // 2010/12/31 add
                    if (String.IsNullOrEmpty(app.MessageReserveTimeSpan) == true)
                        app.MessageReserveTimeSpan = "365day";
                }


                /*
                // ͼ���ҵ�������
                // Ԫ��<libraryserver>
                // ����url
                node = dom.DocumentElement.SelectSingleNode("//libraryserver");
                if (node != null)
                {
                    app.LibraryServerUrl = DomUtil.GetAttr(node, "url");
                }
                 * */

                // OPAC������
                // Ԫ��<opacServer>
                // ����url
                node = dom.DocumentElement.SelectSingleNode("//opacServer");
                if (node != null)
                {
                    app.OpacServerUrl = DomUtil.GetAttr(node, "url");
                }


                // ΥԼ��
                // Ԫ��<amerce>
                // ����dbname/overdueStyle
                node = dom.DocumentElement.SelectSingleNode("//amerce");
                if (node != null)
                {
                    app.AmerceDbName = DomUtil.GetAttr(node, "dbname");
                    app.OverdueStyle = DomUtil.GetAttr(node, "overdueStyle");
                }

                // ��Ʊ
                // Ԫ��<invoice>
                // ����dbname
                node = dom.DocumentElement.SelectSingleNode("invoice");
                if (node != null)
                {
                    app.InvoiceDbName = DomUtil.GetAttr(node, "dbname");
                }

                // *** �����ڴ�Ĳ�������

                // bin dir
                app.BinDir = strBinDir;

                nRet = 0;

                {



                    /*
                    // ׼������: ӳ�����ݿ���
                    nRet = this.GetGlobalCfg(session.Channels,
                        out strError);
                    if (nRet == -1)
                    {
                        app.WriteErrorLog(strError);
                        goto ERROR1;
                    }*/
#if LOG_INFO
                    app.WriteErrorLog("INFO: LoadReaderDbGroupParam");
#endif
                    // <readerdbgroup>
                    app.LoadReaderDbGroupParam(dom);

#if LOG_INFO
                    app.WriteErrorLog("INFO: LoadItemDbGroupParam");
#endif

                    // <itemdbgroup> 
                    nRet = app.LoadItemDbGroupParam(dom,
                        out strError);
                    if (nRet == -1)
                    {
                        app.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    // ��ʱ��SessionInfo����
                    SessionInfo session = new SessionInfo(this);
                    try
                    {
#if LOG_INFO
                        app.WriteErrorLog("INFO: InitialKdbs");
#endif

                        // ��ʼ��kdbs
                        nRet = InitialKdbs(session.Channels,
                            out strError);
                        if (nRet == -1)
                        {
                            app.WriteErrorLog("ERR001 �״γ�ʼ��kdbsʧ��: " + strError);
                            // DefaultThread�������Գ�ʼ��

                            // session.Close();
                            // goto ERROR1;
                        }
                        else
                        {
#if LOG_INFO
                            app.WriteErrorLog("INFO: CheckKernelVersion");
#endif

                            // ��� dpKernel �汾��
                            nRet = CheckKernelVersion(session.Channels,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                        }

#if LOG_INFO
                        app.WriteErrorLog("INFO: InitialVdbs");
#endif

                        // 2008/6/6  ���³�ʼ������ⶨ��
                        // �����������ط����õ�InitialVdbs()�Ϳ���ȥ����
                        // TODO: Ϊ����������ٶȣ������Ż�Ϊ��ֻ�е�<virtualDatabases>Ԫ���µ������иı�ʱ�������½��������ʼ��
                        this.vdbs = null;
                        nRet = app.InitialVdbs(session.Channels,
                            out strError);
                        if (nRet == -1)
                        {
                            app.WriteErrorLog("ERR002 �״γ�ʼ��vdbsʧ��: " + strError);
                            // DefaultThread�������Գ�ʼ��

                            // session.Close();
                            // goto ERROR1;
                        }

                    }
                    finally
                    {
                        session.CloseSession();
                        session = null;

#if LOG_INFO
                        app.WriteErrorLog("INFO: ��ʱ session ʹ�����");
#endif

                    }

                }

                // ʱ��
                string strClock = DomUtil.GetElementText(dom.DocumentElement, "clock");
                try
                {
                    this.Clock.Delta = Convert.ToInt64(strClock);
                }
                catch
                {
                }

                // *** ��ʼ��������־����
                if (bReload == false)   // 2014/4/2
                {
                    // this.OperLogDir = strOperLogDir;    // 2006/12/7 
#if LOG_INFO
                    app.WriteErrorLog("INFO: OperLog.Initial");
#endif

                    // oper log
                    nRet = this.OperLog.Initial(this,
                        strOperLogDir,
                        out strError);
                    if (nRet == -1)
                    {
                        app.WriteErrorLog(strError);
                        goto ERROR1;
                    }


                }

                // *** ��ʼ��ͳ�ƶ���
                // if (bReload == false)   // 2014/4/2
                {
#if LOG_INFO
                    app.WriteErrorLog("INFO: Statis.Initial");
#endif

                    this.Statis = new Statis();
                    nRet = this.Statis.Initial(this, out strError);
                    if (nRet == -1)
                    {
                        app.WriteErrorLog(strError);
                        goto ERROR1;
                    }
                }

#if LOG_INFO
                app.WriteErrorLog("INFO: InitialLibraryHostAssembly");
#endif
                // ��ʼ��LibraryHostAssembly����
                // ������ReadersMonitor��ǰ���������������õ��ű�����ʱ�����2007/10/10 changed
                // return:
                //		-1	����
                //		0	�ɹ�
                nRet = this.InitialLibraryHostAssembly(out strError);
                if (nRet == -1)
                {
                    app.WriteErrorLog(strError);
                    goto ERROR1;
                }

#if LOG_INFO
                app.WriteErrorLog("INFO: InitialExternalMessageInterfaces");
#endif

                // ��ʼ����չ��Ϣ�ӿ�
                nRet = app.InitialExternalMessageInterfaces(
                out strError);
                if (nRet == -1)
                {
                    strError = "��ʼ����չ����Ϣ�ӿ�ʱ����: " + strError;
                    app.WriteErrorLog(strError);
                    // goto ERROR1;
                }

                // ��������������
                // TODO: ��һ�ο��Ƿ��뵽һ��������
                if (bReload == false)
                {
                    string strBreakPoint = "";

#if LOG_INFO
                    app.WriteErrorLog("INFO: DefaultThread");
#endif
                    // ����DefaultThread
                    try
                    {
                        DefaultThread defaultThread = new DefaultThread(this, null);
                        this.BatchTasks.Add(defaultThread);

                        defaultThread.StartWorkerThread();

                        this.defaultManagerThread = defaultThread;
                    }
                    catch (Exception ex)
                    {
                        app.WriteErrorLog("��������������DefaultThreadʱ����" + ex.Message);
                        goto ERROR1;
                    }

#if LOG_INFO
                    app.WriteErrorLog("INFO: ArriveMonitor");
#endif
                    // ����ArriveMonitor
                    try
                    {
                        ArriveMonitor arriveMonitor = new ArriveMonitor(this, null);
                        this.BatchTasks.Add(arriveMonitor);

                        arriveMonitor.StartWorkerThread();
                    }
                    catch (Exception ex)
                    {
                        app.WriteErrorLog("��������������ArriveMonitorʱ����" + ex.Message);
                        goto ERROR1;
                    }

#if LOG_INFO
                    app.WriteErrorLog("INFO: ReadersMonitor");
#endif
                    // ����ReadersMonitor
                    try
                    {
                        ReadersMonitor readersMonitor = new ReadersMonitor(this, null);
                        this.BatchTasks.Add(readersMonitor);

                        readersMonitor.StartWorkerThread();
                    }
                    catch (Exception ex)
                    {
                        app.WriteErrorLog("��������������ReadersMonitorʱ����" + ex.Message);
                        goto ERROR1;
                    }

#if LOG_INFO
                    app.WriteErrorLog("INFO: MessageMonitor");
#endif
                    // ����MessageMonitor
                    try
                    {
                        MessageMonitor messageMonitor = new MessageMonitor(this, null);
                        this.BatchTasks.Add(messageMonitor);

                        // �Ӷϵ�����ļ��ж�����Ϣ
                        // return:
                        //      -1  error
                        //      0   file not found
                        //      1   found
                        nRet = ReadBatchTaskBreakPointFile(messageMonitor.DefaultName,
                            out strBreakPoint,
                            out strError);
                        if (nRet == -1)
                        {
                            app.WriteErrorLog("ReadBatchTaskBreakPointFileʱ����" + strError);
                        }



                        if (messageMonitor.StartInfo == null)
                            messageMonitor.StartInfo = new BatchTaskStartInfo();   // ����ȱʡֵ��

                        // �����Ҫ�Ӷϵ�����
                        if (nRet == 1)
                            messageMonitor.StartInfo.Start = "!breakpoint";  //strBreakPoint;

                        messageMonitor.ClearProgressFile();   // ��������ļ�����
                        messageMonitor.StartWorkerThread();
                    }
                    catch (Exception ex)
                    {
                        app.WriteErrorLog("��������������MessageMonitorʱ����" + ex.Message);
                        goto ERROR1;
                    }


                    // ����DkywReplication
                    // <dkyw>
                    node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//dkyw");
                    if (node != null)
                    {
#if LOG_INFO
                        app.WriteErrorLog("INFO: DkywReplication");
#endif
                        try
                        {
                            DkywReplication dkyw = new DkywReplication(this, null);
                            this.BatchTasks.Add(dkyw);

                            /*
                            // �Ӷϵ�����ļ��ж�����Ϣ
                            // return:
                            //      -1  error
                            //      0   file not found
                            //      1   found
                            nRet = ReadBatchTaskBreakPointFile(dkyw.DefaultName,
                                out strBreakPoint,
                                out strError);
                            if (nRet == -1)
                            {
                                app.WriteErrorLog("ReadBatchTaskBreakPointFileʱ����" + strError);
                            }
                             * */
                            bool bLoop = false;
                            string strLastNumber = "";

                            // return:
                            //      -1  ����
                            //      0   û���ҵ��ϵ���Ϣ
                            //      1   �ҵ��˶ϵ���Ϣ
                            nRet = dkyw.ReadLastNumber(
                                out bLoop,
                                out strLastNumber,
                                out strError);
                            if (nRet == -1)
                            {
                                app.WriteErrorLog("ReadLastNumberʱ����" + strError);
                            }

                            if (dkyw.StartInfo == null)
                                dkyw.StartInfo = new BatchTaskStartInfo();   // ����ȱʡֵ��

                            if (bLoop == true)
                            {
                                // ��Ҫ�Ӷϵ�����
                                if (nRet == 1)
                                    dkyw.StartInfo.Start = "!breakpoint";  //strBreakPoint;

                                dkyw.ClearProgressFile();   // ��������ļ�����
                                dkyw.StartWorkerThread();
                            }
                        }
                        catch (Exception ex)
                        {
                            app.WriteErrorLog("��������������DkywReplicationʱ����" + ex.Message);
                            goto ERROR1;
                        }
                    }

                    // ����PatronReplication
                    // <patronReplication>
                    // ���߿�����ͬ�� ����������
                    // �ӿ�����ͬ����������
                    node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//patronReplication");
                    if (node != null)
                    {
#if LOG_INFO
                        app.WriteErrorLog("INFO: PatronReplication");
#endif
                        try
                        {
                            PatronReplication patron_rep = new PatronReplication(this, null);
                            this.BatchTasks.Add(patron_rep);

                            patron_rep.StartWorkerThread();
                        }
                        catch (Exception ex)
                        {
                            app.WriteErrorLog("��������������PatronReplicationʱ����" + ex.Message);
                            goto ERROR1;
                        }
                    }

                    // ���� LibraryReplication

#if LOG_INFO
                    app.WriteErrorLog("INFO: LibraryReplication ReadBatchTaskBreakPointFile");
#endif
                    // �Ӷϵ�����ļ��ж�����Ϣ
                    // return:
                    //      -1  error
                    //      0   file not found
                    //      1   found
                    nRet = ReadBatchTaskBreakPointFile("dp2Library ͬ��",
                        out strBreakPoint,
                        out strError);
                    if (nRet == -1)
                    {
                        app.WriteErrorLog("ReadBatchTaskBreakPointFile() ʱ����" + strError);
                    }
                    // ���nRet == 0����ʾû�жϵ��ļ����ڣ�Ҳ�Ͳ����Զ������������

                    // strBreakPoint ��δ��ʹ�á����Ƕϵ��ļ��Ƿ���ڣ���һ��Ϣ�м�ֵ��

                    if (nRet == 1)
                    {
#if LOG_INFO
                        app.WriteErrorLog("INFO: LibraryReplication");
#endif
                        try
                        {

                            // �Ӷϵ��ļ���ȡ���ϵ��ַ���
                            // �ϵ��ַ�����ʽ�����.ƫ����@��־�ļ���
                            //  ���ߣ����@��־�ļ���
                            // ��öϵ���Ϣ���������̵Ĵ��룬�Ƿ����˹���TraceDTLP�ࣿ
                            // ������죬���Թ�����ΪBatchTask�����һ�����ԡ�

                            LibraryReplication replication = new LibraryReplication(this, null);
                            this.BatchTasks.Add(replication);

                            if (replication.StartInfo == null)
                                replication.StartInfo = new BatchTaskStartInfo();   // ����ȱʡֵ��
                            replication.StartInfo.Start = "date=continue";  // �Ӷϵ㿪ʼ��
                            replication.ClearProgressFile();   // ��������ļ�����
                            replication.StartWorkerThread();
                        }
                        catch (Exception ex)
                        {
                            app.WriteErrorLog("��������������ʱ����" + ex.Message);
                            goto ERROR1;
                        }
                    }

                    // ���� RebuildKeys

#if LOG_INFO
                    app.WriteErrorLog("INFO: RebuildKeys ReadBatchTaskBreakPointFile");
#endif
                    // �Ӷϵ�����ļ��ж�����Ϣ
                    // return:
                    //      -1  error
                    //      0   file not found
                    //      1   found
                    nRet = ReadBatchTaskBreakPointFile("�ؽ�������",
                        out strBreakPoint,
                        out strError);
                    if (nRet == -1)
                    {
                        app.WriteErrorLog("ReadBatchTaskBreakPointFile() ʱ����" + strError);
                    }
                    // ���nRet == 0����ʾû�жϵ��ļ����ڣ�Ҳ�Ͳ����Զ������������

                    // strBreakPoint ��δ��ʹ�á����Ƕϵ��ļ��Ƿ���ڣ���һ��Ϣ�м�ֵ��

                    if (nRet == 1)
                    {
#if LOG_INFO
                        app.WriteErrorLog("INFO: RebuildKeys");
#endif
                        try
                        {

                            // �Ӷϵ��ļ���ȡ���ϵ��ַ���
                            RebuildKeys replication = new RebuildKeys(this, null);
                            this.BatchTasks.Add(replication);

                            if (replication.StartInfo == null)
                                replication.StartInfo = new BatchTaskStartInfo();   // ����ȱʡֵ��
                            replication.StartInfo.Start = "dbnamelist=continue";  // �Ӷϵ㿪ʼ��
                            replication.ClearProgressFile();   // ��������ļ�����
                            replication.StartWorkerThread();
                        }
                        catch (Exception ex)
                        {
                            app.WriteErrorLog("��������������ʱ����" + ex.Message);
                            goto ERROR1;
                        }
                    }

                }


                // ������ѯ���������
                {
                    XmlNode nodeTemp = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//virtualDatabases");
                    if (nodeTemp != null)
                    {
                        try
                        {
                            string strMaxCount = DomUtil.GetAttr(nodeTemp, "searchMaxResultCount");
                            if (String.IsNullOrEmpty(strMaxCount) == false)
                                this.SearchMaxResultCount = Convert.ToInt32(strMaxCount);
                        }
                        catch
                        {
                        }
                    }
                }

#if LOG_INFO
                app.WriteErrorLog("INFO: ׼���������ݿ����");
#endif
                //
                this.IssueItemDatabase = new IssueItemDatabase(this);
                this.OrderItemDatabase = new OrderItemDatabase(this);
                this.CommentItemDatabase = new CommentItemDatabase(this);

#if LOG_INFO
                app.WriteErrorLog("INFO: MessageCenter");
#endif
                // 
                this.MessageCenter = new MessageCenter();
                this.MessageCenter.ServerUrl = this.WsUrl;
                this.MessageCenter.MessageDbName = this.MessageDbName;

                this.MessageCenter.VerifyAccount -= new VerifyAccountEventHandler(MessageCenter_VerifyAccount); // 2008/6/6 
                this.MessageCenter.VerifyAccount += new VerifyAccountEventHandler(MessageCenter_VerifyAccount);

#if NO
            if (bReload == false)
            {
                PathUtil.CreateDirIfNeed(strColumnDir);	// ȷ��Ŀ¼����
                nRet = LoadCommentColumn(
                    PathUtil.MergePath(strColumnDir, "comment"),
                    out strError);
                if (nRet == -1)
                {
                    app.WriteErrorLog("װ����Ŀ�洢ʱ����: " + strError);
                }
            }
#endif

                // ����library.xml�ļ��汾
                if (bReload == false)
                {
#if LOG_INFO
                    app.WriteErrorLog("INFO: UpgradeLibraryXml");
#endif
                    nRet = this.UpgradeLibraryXml(out strError);
                    if (nRet == -1)
                    {
                        app.WriteErrorLog("����library.xmlʱ����" + strError);
                    }
                }

                if (bReload == true)
                    app.WriteErrorLog("library application��������װ�� " + this.m_strFileName);
                else
                {
                    TimeSpan delta = DateTime.Now - start;
                    app.WriteErrorLog("library application�ɹ���ʼ������ʼ�������ķ�ʱ�� " + delta.TotalSeconds.ToString() + " ��");

                    // д��down������ļ�
                    app.WriteAppDownDetectFile("library application�ɹ���ʼ����");

                    if (this.watcher == null)
                    {
#if LOG_INFO
                        app.WriteErrorLog("INFO: BeginWatcher");
#endif

                        BeginWatcher();
#if LOG_INFO
                        app.WriteErrorLog("INFO: End BeginWatcher");
#endif
                    }

#if NO
                if (this.virtual_watcher == null)
                    BeginVirtualDirWatcher();
#endif
                }

                // Application["errorinfo"] = "";  // �����ǰ���ܲ����Ĵ�����Ϣ 2007/10/10

                // 2013/4/10
                if (this.Changed == true)
                    this.ActivateManagerThread();
            }
            catch (Exception ex)
            {
                strError = "LoadCfg() �׳��쳣: " + ExceptionUtil.GetDebugText(ex);
                goto ERROR1;
            }

            return 0;
            // 2008/10/13 
        ERROR1:
            if (bReload == false)
            {
                if (this.watcher == null)
                {
#if LOG_INFO
                    app.WriteErrorLog("INFO: BeginWatcher");
#endif

                    BeginWatcher();
#if LOG_INFO
                    app.WriteErrorLog("INFO: End BeginWatcher");
#endif
                }
#if NO
                if (this.virtual_watcher == null)
                    BeginVirtualDirWatcher();
#endif

            }

            if (bReload == true)
                app.WriteErrorLog("library application����װ�� " + this.m_strFileName + " �Ĺ��̷������ش��� ["+strError+"]�������ڲ�ȱ״̬���뼰ʱ�ų����Ϻ���������");
            else
                app.WriteErrorLog("library application��ʼ�����̷������ش��� [" + strError + "]����ǰ�˷����ڲ�ȱ״̬���뼰ʱ�ų����Ϻ���������");

            return -1;
        }

        void CleanSessionDir(string strSessionDir)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(strSessionDir);

                // ɾ�����е��¼�Ŀ¼
                DirectoryInfo[] dirs = di.GetDirectories();
                foreach (DirectoryInfo childDir in dirs)
                {
                    Directory.Delete(childDir.FullName, true);
                }
            }
            catch (Exception ex)
            {
                this.WriteErrorLog("ɾ�� session �¼�Ŀ¼ʱ����: " + ExceptionUtil.GetDebugText(ex));
            }
        }

        public string GetTempFileName(string strPrefix)
        {
            return Path.Combine(this.TempDir, "~" + strPrefix + "_" + Guid.NewGuid().ToString());
        }

        public int CheckKernelVersion(RmsChannelCollection Channels,
    out string strError)
        {
            strError = "";

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            string strVersion = "";
            long lRet = channel.GetVersion(out strVersion,
                out strError);
            if (lRet == -1)
            {
                strError = "��ȡ dpKernel �汾�Ź��̷�������" + strError;
                return -1;
            }

            // �����Ͱ汾��
            double value = 0;
            if (double.TryParse(strVersion, out value) == false)
            {
                strError = "dp2Kernel�汾�� '"+strVersion+"' ��ʽ����ȷ";
                return -1;
            }

            double base_version = 2.57;

            if (value < base_version)
            {
                strError = "��ǰ dp2Library �汾��Ҫ�� dp2Kernel " + base_version + " ���ϰ汾����ʹ�á����������� dp2Kernel �����°汾��";
                return -1;
            }

            return 0;
        }

#if NO
        public void ActivateCacheBuilder()
        {
            // ����CacheBuilder
            try
            {
                if (this.CacheBuilder == null)
                {
                    this.CacheBuilder = new CacheBuilder(this, null);
                    this.BatchTasks.Add(this.CacheBuilder);
                }
                this.CacheBuilder.StartWorkerThread();
            }
            catch (Exception ex)
            {
                this.WriteErrorLog("��������������CacheBuilderʱ����" + ex.Message);
            }

            this.CacheBuilder.Activate();
        }
#endif

        int UpgradeLibraryXml(out string strError)
        {
            strError = "";
            bool bChanged = false;

            // �ҵ�<version>Ԫ��
            XmlNode nodeVersion = this.LibraryCfgDom.DocumentElement.SelectSingleNode("version");
            if (nodeVersion == null)
            {
                nodeVersion = this.LibraryCfgDom.CreateElement("version");

                /*
                 * û�б�Ҫ����Ϊsave()ʱ����������λ��
                // �������뵽��һ����λ��
                if (this.LibraryCfgDom.DocumentElement.ChildNodes.Count > 0)
                    this.LibraryCfgDom.DocumentElement.InsertBefore(nodeVersion,
                        this.LibraryCfgDom.DocumentElement.ChildNodes[0]);
                else
                 * */
                    this.LibraryCfgDom.DocumentElement.AppendChild(nodeVersion);

                nodeVersion.InnerText = "0.01";    // ��δ�й�<version>Ԫ�ص�library.xml�汾������Ϊ��0.01��
                bChanged = true;
            }

            string strVersion = nodeVersion.InnerText;
            if (String.IsNullOrEmpty(strVersion) == true)
                strVersion = "0.01";

            double version = 0.01;
            try
            {
                version = Convert.ToDouble(strVersion);
            }
            catch
            {
                version = 0.01;
            }

            // ��0.01������
            if (version == 0.01)
            {
                /*
                 * ������Ƭ���г��<group>Ԫ�ص�zhongcihaodb����ֵ��ȥ�أ�Ȼ�����<utilDb>Ԫ����
    <zhongcihao>
        <nstable name="nstable">
            <item prefix="marc" uri="http://dp2003.com/UNIMARC" />
        </nstable>
        <group name="������Ŀ" zhongcihaodb="�ִκ�">
            <database name="����ͼ��" leftfrom="��ȡ���" rightxpath="//marc:record/marc:datafield[@tag='905']/marc:subfield[@code='e']/text()" titlexpath="//marc:record/marc:datafield[@tag='200']/marc:subfield[@code='a']/text()" authorxpath="//marc:record/marc:datafield[@tag='200']/marc:subfield[@code='f' or @code='g']/text()" />
        </group>
    </zhongcihao>
                 */
                List<string> dbnames = new List<string>();
                XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("zhongcihao/group");
                for (int i = 0; i < nodes.Count; i++)
                {
                    string strDbName = DomUtil.GetAttr(nodes[i], "zhongcihaodb");
                    if (String.IsNullOrEmpty(strDbName) == true)
                        continue;
                    if (dbnames.IndexOf(strDbName) != -1)
                        continue;
                    dbnames.Add(strDbName);
                }

                XmlNode nodeUtilDb = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb");
                if (nodeUtilDb == null)
                {
                    nodeUtilDb = this.LibraryCfgDom.CreateElement("utilDb");
                    this.LibraryCfgDom.DocumentElement.AppendChild(nodeUtilDb);
                    bChanged = true;
                }

                for (int i = 0; i < dbnames.Count; i++)
                {
                    string strDbName = dbnames[i];
                    // ����<utilDb>���Ƿ��Ѿ�����
                    XmlNode nodeExist = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strDbName + "']");
                    if (nodeExist != null)
                    {
                        string strType = DomUtil.GetAttr(nodeExist, "type");
                        if (strType != "zhongcihao")
                        {
                            strError = "<utilDb>��name����ֵΪ'"+strDbName+"'��<database>Ԫ�أ���type����ֵ��Ϊ'zhongcihao'(����'"+strType+"')�����<zhongcihao>Ԫ���µĳ�ʼ����ì�ܡ���ϵͳ����Ա���˽���������ʵ���ͺ��ֶ��������ļ������޸ġ�";
                            return -1;
                        }
                        continue;
                    }

                    XmlNode nodeDatabase = this.LibraryCfgDom.CreateElement("database");
                    nodeUtilDb.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                    DomUtil.SetAttr(nodeDatabase, "type", "zhongcihao");

                    bChanged = true;
                }

                // ������ɺ��޸İ汾��
                nodeVersion.InnerText = "0.02";
                bChanged = true;
                WriteErrorLog("�Զ�����library.xml v0.01��v0.02");
                version = 0.02;
            }

            // 2009/3/10
            // ��0.02������
            if (version == 0.02)
            {
                // ��<rightstable>Ԫ�����޸�Ϊ<rightsTable>
                XmlNode nodeRightsTable = this.LibraryCfgDom.DocumentElement.SelectSingleNode("rightstable");
                if (nodeRightsTable != null)
                {
                    // ����һ����Ԫ��
                    XmlNode nodeNew = this.LibraryCfgDom.CreateElement("rightsTable");
                    this.LibraryCfgDom.DocumentElement.InsertAfter(nodeNew, nodeRightsTable);

                    nodeNew.InnerXml = nodeRightsTable.InnerXml;

                    // ɾ����Ԫ��
                    nodeRightsTable.ParentNode.RemoveChild(nodeRightsTable);

                    nodeRightsTable = nodeNew;
                }
                else
                {
                    nodeRightsTable = this.LibraryCfgDom.CreateElement("rightsTable");
                    this.LibraryCfgDom.DocumentElement.AppendChild(nodeRightsTable);
                }

                // �����µ�<readertypes>��<booktypes>�ƶ���<rightsTable>Ԫ���£����Ұ�Ԫ�����޸�Ϊ<readerTypes>��<bookTypes>
                XmlNode nodeReaderTypes = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readertypes");
                if (nodeReaderTypes != null)
                {
                    // ����һ����Ԫ��
                    XmlNode nodeNew = this.LibraryCfgDom.CreateElement("readerTypes");
                    nodeRightsTable.AppendChild(nodeNew);

                    nodeNew.InnerXml = nodeReaderTypes.InnerXml;
                    nodeReaderTypes.ParentNode.RemoveChild(nodeReaderTypes);
                }

                XmlNode nodeBookTypes = this.LibraryCfgDom.DocumentElement.SelectSingleNode("booktypes");
                if (nodeBookTypes != null)
                {
                    // ����һ����Ԫ��
                    XmlNode nodeNew = this.LibraryCfgDom.CreateElement("bookTypes");
                    nodeRightsTable.AppendChild(nodeNew);

                    nodeNew.InnerXml = nodeBookTypes.InnerXml;
                    nodeBookTypes.ParentNode.RemoveChild(nodeBookTypes);
                }

                // ��<locationtypes>Ԫ�����޸�Ϊ<locationTypes>
                XmlNode nodeLocationTypes = this.LibraryCfgDom.DocumentElement.SelectSingleNode("locationtypes");
                if (nodeLocationTypes != null)
                {
                    // ����һ����Ԫ��
                    XmlNode nodeNew = this.LibraryCfgDom.CreateElement("locationTypes");
                    this.LibraryCfgDom.DocumentElement.InsertAfter(nodeNew, nodeLocationTypes);

                    nodeNew.InnerXml = nodeLocationTypes.InnerXml;
                }

                // ������ɺ��޸İ汾��
                nodeVersion.InnerText = "0.03";
                bChanged = true;
                WriteErrorLog("�Զ�����library.xml v0.02��v0.03");
                version = 0.03;
            }



#if NO
            // �� 2.00 ������
            // 2013/12/10
            if (version <= 2.00)
            {
                // bool bChanged = false;
                XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("accounts/account");
                foreach (XmlNode node in nodes)
                {
                    string strLibraryCode = DomUtil.GetAttr(node, "libraryCode");
                    if (string.IsNullOrEmpty(strLibraryCode) == true)
                    {
                        DomUtil.SetAttr(node, "libraryCode", "<global>");
                        // bChanged = true;
                    }
                }

                // ������ɺ��޸İ汾��
                nodeVersion.InnerText = "2.01";
                bChanged = true;
                WriteErrorLog("�Զ����� library.xml v2.00 �� v2.01");
                version = 2.01;
            }
#endif

            // 2015/5/20
            // ��2.00������
            if (version <= 2.00)
            {
                // ���� library.xml �е��û��˻������Ϣ
                // �ļ���ʽ 0.03-->0.04
                // accounts/account �� password �洢��ʽ�ı�
                int nRet = LibraryServerUtil.UpgradeLibraryXmlUserInfo(
                    EncryptKey,
                    ref this.LibraryCfgDom,
                    out strError);
                if (nRet == -1)
                    WriteErrorLog("�Զ����� library.xml v2.00(������)��v2.01 ʱ����: " + strError + "��Ϊ���޸�������⣬��ϵͳ����Ա�������й�����Ա�˻�������");

                // ������ɺ��޸İ汾��
                nodeVersion.InnerText = "2.01";
                bChanged = true;
                WriteErrorLog("�Զ����� library.xml v2.00(������)��v2.01");
                version = 2.01;
            }

            if (bChanged == true)
            {
                this.Changed = true;
                this.ActivateManagerThread();   // 2009/3/10 
            }

            return 0;
        }

        // 2008/5/8
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int InitialKdbs(
            RmsChannelCollection Channels,
            out string strError)
        {
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
#if NO
                this.kdbs = new KernelDbInfoCollection();
                int nRet = this.kdbs.Initial(Channels,
                            this.WsUrl,
                            "zh",
                            out strError);
                if (nRet == -1)
                {
                    // this.vdbs = null;   // BUG!!!
                    this.kdbs = null;
                    return -1;
                }
#endif

                // kdbs ��ʼ���Ĺ�������Ҫ�ķ�ʱ��ģ���������м���ʣ�������Щ��Ϣ��������ʼ�����Ҳ���
                // ����������������ʼ�������Ժ�Ȼ��Źҽӵ� this.kdbs ��
                // ������һ������������ʹ�õĵط��� ���� this.m_lock ����������ȱ����̫�鷳
                this.kdbs = null;
                KernelDbInfoCollection kdbs = new KernelDbInfoCollection();
                // return:
                //      -1  ����
                //      0   �ɹ�
                int nRet = kdbs.Initial(Channels,
                            this.WsUrl,
                            "zh",
                            out strError);
                if (nRet == -1)
                    return -1;

                this.kdbs = kdbs;

                // 2015/5/7
                BiblioDbFromInfo[] infos = null;
                // �г�ĳ�����ݿ�ļ���;����Ϣ
                // return:
                //      -1  ����
                //      0   û�ж���
                //      1   �ɹ�
                nRet = this.ListDbFroms("arrived",
                    "zh",
                    "",
                    out infos,
                    out strError);
                this.ArrivedDbFroms = infos;

                return 0;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
        }

        // ��������̨����һ��������ʹд��cfgdom��xml�ļ�
        public void ActivateManagerThread()
        {
            if (this.defaultManagerThread != null)
                this.defaultManagerThread.Activate();
        }

        // ��������̨����һ��������ʹ�������³�ʼ��kdbs��vdbs
        public void ActivateManagerThreadForLoad()
        {
            if (this.defaultManagerThread != null)
            {
                this.defaultManagerThread.ClearRetryDelay();
                this.defaultManagerThread.Activate();
            }
        }

        

#if NO
        // 2007/7/11 
        int LoadWebuiCfgDom(out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(this.m_strWebuiFileName) == true)
            {
                strError = "m_strWebuiFileName��δ��ʼ��������޷�װ��webui.xml�����ļ���DOM";
                return -1;
            }

            XmlDocument webuidom = new XmlDocument();
            try
            {
                webuidom.Load(this.m_strWebuiFileName);
            }
            catch (FileNotFoundException)
            {
                /*
                strError = "file '" + strWebUiFileName + "' not found ...";
                return -1;
                 * */
                webuidom.LoadXml("<root/>");
            }
            catch (Exception ex)
            {
                strError = "װ�������ļ�-- '" + this.m_strWebuiFileName + "'ʱ��������ԭ��" + ex.Message;
                // app.WriteErrorLog(strError);
                return -1;
            }

            this.WebUiDom = webuidom;
            return 0;
        }
#endif

        public static bool ToBoolean(string strText,
            bool bDefaultValue)
        {
            if (String.IsNullOrEmpty(strText) == true)
                return bDefaultValue;

            strText = strText.ToLower();

            if (strText == "true" || strText == "on" || strText == "yes")
                return true;

            return false;
        }

        void MessageCenter_VerifyAccount(object sender, VerifyAccountEventArgs e)
        {
            string strError = "";

            if (e.Name == "public")
            {
                e.Exist = false;
                e.Error = true;
                e.ErrorInfo = "ϵͳ��ֹ��public�û�����Ϣ��";
                return;
            }

             // �������˺��Ƿ����
        // return:
        //      -1  error
        //      0   ������
        //      1   ����
        //      >1  ����һ��
            int nRet = VerifyReaderAccount(e.Channels,
                e.Name,
                out strError);
            if (nRet == -1 || nRet > 1)
            {
                e.Exist = false;
                e.Error = true;
                e.ErrorInfo = strError;
                return;
            }
            if (nRet == 1)
            {
                e.Exist = true;
                return;
            }

            // ��鹤����Ա�˺�
            Account account = null;

            /*
            if (e.Name == "public")
            {
            }*/

            // ��library.xml�ļ����� ���һ���ʻ�����Ϣ
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = this.GetAccount(e.Name,
                out account,
                out strError);
            if (nRet == -1)
            {
                e.Exist = false;
                e.Error = true;
                e.ErrorInfo = strError;
                return;
            }
            if (nRet == 0)
            {
                e.Exist = false;
                e.Error = false;
                e.ErrorInfo = "�û��� '" + e.Name + "' �����ڡ�";
                return;
            }


            e.Exist = true;
        }

#if NO
        void BeginVirtualDirWatcher()
        {
            virtual_watcher = new FileSystemWatcher();
            virtual_watcher.Path = this.HostDir;

            /* Watch for changes in LastAccess and LastWrite times, and 
               the renaming of files or directories. */
            virtual_watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Security;

            virtual_watcher.Filter = "*.*"; // Path.GetFileName(this.m_strFileName);  //"*.*";
            virtual_watcher.IncludeSubdirectories = true;

            // Add event handlers.
            virtual_watcher.Changed -= new FileSystemEventHandler(virtual_watcher_Changed);
            virtual_watcher.Changed += new FileSystemEventHandler(virtual_watcher_Changed);

            // Begin watching.
            virtual_watcher.EnableRaisingEvents = true;

        }


        void virtual_watcher_Changed(object sender, FileSystemEventArgs e)
        {
            string strError = "*** ����Ŀ¼�ڷ����ı�: name: " + e.Name.ToString()
                + "; changetype: " + e.ChangeType.ToString()
                + "; fullpath: " + e.FullPath.ToString();
            this.WriteErrorLog(strError);
        }

#endif

        // ����library.xml�ļ��仯
        void BeginWatcher()
        {
            watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(this.m_strFileName);

            /* Watch for changes in LastAccess and LastWrite times, and 
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes;

            watcher.Filter = "*.*"; // Path.GetFileName(this.m_strFileName);  //"*.*";
            watcher.IncludeSubdirectories = true;

            // Add event handlers.
            watcher.Changed -= new FileSystemEventHandler(watcher_Changed);
            watcher.Changed += new FileSystemEventHandler(watcher_Changed);

            // Begin watching.
            watcher.EnableRaisingEvents = true;

        }

        void EndWather()
        {
            if (this.watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= new FileSystemEventHandler(watcher_Changed);
                this.watcher.Dispose();
                this.watcher = null;
            }
        }

        void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if ((e.ChangeType & WatcherChangeTypes.Changed) != WatcherChangeTypes.Changed)
                return;

            int nRet = 0;

            // this.WriteErrorLog("file1='"+this.m_strFileName+"' file2='" + e.FullPath + "'");
            if (PathUtil.IsEqual(this.m_strFileName, e.FullPath) == true)
            {
                string strError = "";

                // ��΢��ʱһ�£�����ܿ����װ�����ú� ���ڸ�дlibrary.xml�ļ��ĵĽ��̷�����ͻ
                Thread.Sleep(500);

                nRet = this.LoadCfg(
                    true,
                    this.DataDir,
                    this.HostDir,
                    out strError);
                if (nRet == -1)
                {
                    strError = "reload " + this.m_strFileName + " error: " + strError;
                    this.WriteErrorLog(strError);
                    this.GlobalErrorInfo = strError;
                }
                else
                {
                    this.GlobalErrorInfo = "";
                }
            }

            nRet = e.FullPath.IndexOf(".fltx");
            if (nRet != -1)
            {
                this.Filters.ClearFilter(e.FullPath);
            }

#if NO
            // ����webui.xml
            if (PathUtil.IsEqual(this.m_strWebuiFileName, e.FullPath) == true)
            {
                string strError = "";
                nRet = this.LoadWebuiCfgDom(out strError);
                if (nRet == -1)
                {
                    strError = "reload " + this.m_strWebuiFileName + " error: " + strError;
                    this.WriteErrorLog(strError);
                    this.GlobalErrorInfo = strError;
                }
                else
                {
                    this.GlobalErrorInfo = "";
                }
            }
#endif

        }

        // ����<readerdbgroup>�������
        // return:
        //      <readerdbgroup>Ԫ����<database>Ԫ�صĸ��������==0����ʾ���ò�����
        int LoadReaderDbGroupParam(XmlDocument dom)
        {
            this.ReaderDbs = new List<ReaderDbCfg>();

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//readerdbgroup/database");

            if (nodes.Count == 0)
                return 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                ReaderDbCfg item = new ReaderDbCfg();

                item.DbName = DomUtil.GetAttr(node, "name");

                bool bValue = true;
                string strError = "";
                int nRet = DomUtil.GetBooleanParam(node,
                    "inCirculation",
                    true,
                    out bValue,
                    out strError);
                if (nRet == -1)
                {
                    this.WriteErrorLog("Ԫ��<//readerdbgroup/database>����inCirculation����ʱ��������: " + strError);
                    bValue = true;
                }

                item.InCirculation = bValue;

                item.LibraryCode = DomUtil.GetAttr(node, "libraryCode");

                this.ReaderDbs.Add(item);
            }

            return nodes.Count;
        }

        // д��<readerdbgroup>���������Ϣ
        void WriteReaderDbGroupParam(XmlTextWriter writer)
        {
            writer.WriteStartElement("readerdbgroup");
            for (int i = 0; i < this.ReaderDbs.Count; i++)
            {
                writer.WriteStartElement("database");

                writer.WriteAttributeString("name", this.ReaderDbs[i].DbName);

                // 2008/6/3 
                writer.WriteAttributeString("inCirculation", this.ReaderDbs[i].InCirculation == true ? "true" : "false");

                // 2012/9/7
                writer.WriteAttributeString("libraryCode", this.ReaderDbs[i].LibraryCode);

                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }


        // ����<itemdbgroup>�������
        // return:
        //      <itemdbgroup>Ԫ����<database>Ԫ�صĸ��������==0����ʾ���ò�����
        int LoadItemDbGroupParam(XmlDocument dom,
            out string strError)
        {
            strError = "";

            /*
            if (this.GlobalCfgDom == null)
            {
                strError = "LoadItemDbGroupParam()ʧ��, ��ΪGlobalCfgDom��δ��ʼ��";
                return -1;
            }*/

            this.ItemDbs = new List<ItemDbCfg>();

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//itemdbgroup/database");

            if (nodes.Count == 0)
                return 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                ItemDbCfg item = new ItemDbCfg();

                item.DbName = DomUtil.GetAttr(node, "name");

                item.BiblioDbName = DomUtil.GetAttr(node, "biblioDbName");
                if (String.IsNullOrEmpty(item.BiblioDbName) == true)
                {
                    strError = "<itemdbgroup>�У�ʵ��� '" + item.DbName + "' <database>Ԫ����biblioDbName����û������";
                    return -1;
                }

                item.BiblioDbSyntax = DomUtil.GetAttr(node, "syntax");

                item.IssueDbName = DomUtil.GetAttr(node, "issueDbName");

                item.OrderDbName = DomUtil.GetAttr(node, "orderDbName");

                item.CommentDbName = DomUtil.GetAttr(node, "commentDbName");

                item.UnionCatalogStyle = DomUtil.GetAttr(node, "unionCatalogStyle");

                item.Replication = DomUtil.GetAttr(node, "replication");

                {
                    Hashtable table = StringUtil.ParseParameters(item.Replication);
                    item.ReplicationServer = (string)table["server"];
                    item.ReplicationDbName = (string)table["dbname"];
                }


                // 2008/6/4 
                bool bValue = true;
                int nRet = DomUtil.GetBooleanParam(node,
                    "inCirculation",
                    true,
                    out bValue,
                    out strError);
                if (nRet == -1)
                {
                    strError = "Ԫ��<//itemdbgroup/database>����inCirculation����ʱ��������: " + strError;
                    return -1;
                }

                item.InCirculation = bValue;

                item.Role = DomUtil.GetAttr(node, "role");

                this.ItemDbs.Add(item);
            }

            return nodes.Count;
        }

        // д��<itemdbgroup>���������Ϣ
        void WriteItemDbGroupParam(XmlTextWriter writer)
        {
            writer.WriteStartElement("itemdbgroup");
            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                ItemDbCfg cfg = this.ItemDbs[i];

                writer.WriteStartElement("database");

                writer.WriteAttributeString("name", cfg.DbName);

                writer.WriteAttributeString("biblioDbName", cfg.BiblioDbName);  // 2005/5/25 

                // ��������ȱ����ΪBUG
                if (String.IsNullOrEmpty(cfg.IssueDbName) == false)
                    writer.WriteAttributeString("issueDbName", cfg.IssueDbName);  // 2007/10/22 
                if (String.IsNullOrEmpty(cfg.BiblioDbSyntax) == false)
                    writer.WriteAttributeString("syntax", cfg.BiblioDbSyntax);   // 2007/10/22 

                if (String.IsNullOrEmpty(cfg.OrderDbName) == false)
                    writer.WriteAttributeString("orderDbName", cfg.OrderDbName);  // 2007/11/27 

                if (String.IsNullOrEmpty(cfg.CommentDbName) == false)
                    writer.WriteAttributeString("commentDbName", cfg.CommentDbName);  // 2008/12/8 

                if (String.IsNullOrEmpty(cfg.UnionCatalogStyle) == false)
                    writer.WriteAttributeString("unionCatalogStyle", cfg.UnionCatalogStyle);  // 2007/12/15 

                // 2008/6/4 
                writer.WriteAttributeString("inCirculation", cfg.InCirculation == true ? "true" : "false");

                if (String.IsNullOrEmpty(cfg.Role) == false)
                    writer.WriteAttributeString("role", cfg.Role);  // 2009/10/23 

                if (String.IsNullOrEmpty(cfg.Replication) == false)
                    writer.WriteAttributeString("replication", cfg.Replication);

                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        /*
        void SaveReaderDbGrouParam(XmlDocument dom)
        {
            XmlNode node = dom.DocumentElement.SelectSingleNode("//readerdbgroup");
            if (node == null)
            {
                node = (XmlNode)dom.CreateElement("readerdbgroup");
                node = dom.DocumentElement.AppendChild(node);
            }

            node.InnerXml = ""; // ɾ��ԭ��ȫ����Ԫ��

            for (int i = 0; i < this.ReaderDbs.Count; i++)
            {
                XmlElement newnode = dom.CreateElement("database");
                node.AppendChild(newnode);

                newnode.SetAttribute("name", this.ReaderDbs[i].DbName);
            }
        }
         */


        // ���ȫ�����ò����Ƿ��������
        public int Verify(out string strError)
        {
            strError = "";
            bool bError = false;
            if (this.WsUrl == "")
            {
                if (strError != "")
                    strError += ", ";

                strError += "<root>Ԫ����wsurl����δ����";
                bError = true;
            }

            if (this.ManagerUserName == "")
            {
                if (strError != "")
                    strError += ", ";
                strError += "<root>Ԫ����managerusername����δ����";
                bError = true;
            }

            if (bError == true)
                return -1;

            return 0;
        }


        public void RestartApplication()
        {
            try
            {
                // ��binĿ¼��дһ����ʱ�ļ�
                using (Stream stream = File.Open(Path.Combine(this.BinDir, "temp.temp"),
                    FileMode.Create))
                {

                }

                // stream.Close();

                this.WriteErrorLog("library application �����³�ʼ����");
            }
            catch (Exception ex)
            {
                this.WriteErrorLog("library application ���³�ʼ��ʱ��������" + ExceptionUtil.GetDebugText(ex));
            }
        }

        public void WriteErrorLog(string strText)
        {
            try
            {
                lock (this.LogDir)
                {
                    DateTime now = DateTime.Now;
                    // ÿ��һ����־�ļ�
                    string strFilename = PathUtil.MergePath(this.LogDir, "log_" + DateTimeUtil.DateTimeToString8(now) + ".txt");
                    string strTime = now.ToString();
                    StreamUtil.WriteText(strFilename,
                        strTime + " " + strText + "\r\n");
                }
            }
            catch (Exception ex)
            {
                // TODO: Ҫ�ڰ�װ������Ԥ�ȴ����¼�Դ
                // ������Բο� unhandle.txt (�ڱ�project��)

                /*
                // Create the source, if it does not already exist.
                if (!EventLog.SourceExists("dp2library"))
                {
                    EventLog.CreateEventSource("dp2library", "DigitalPlatform");
                }*/

                EventLog Log = new EventLog();
                Log.Source = "dp2library";
                Log.WriteEntry("��Ϊԭ��Ҫд����־�ļ��Ĳ��������쳣�� ���Բ��ò���Ϊд��Windowsϵͳ��־(����һ��)���쳣��Ϣ���£�'" + ExceptionUtil.GetDebugText(ex) + "'", EventLogEntryType.Error);
                Log.WriteEntry(strText, EventLogEntryType.Error);
            }
        }

        // д��Windowsϵͳ��־
        public static void WriteWindowsLog(string strText)
        {
            WriteWindowsLog(strText, EventLogEntryType.Error);
        }

        // д��Windowsϵͳ��־
        public static void WriteWindowsLog(string strText,
            EventLogEntryType type)
        {
            EventLog Log = new EventLog();
            Log.Source = "dp2library";
            Log.WriteEntry(strText, type);
        }

        public static void WriteErrorLog(string strFileName,
            string strText)
        {
            try
            {
                string strTime = DateTime.Now.ToString();
                StreamUtil.WriteText(strFileName,
                    strTime + " " + strText + "\r\n");
            }
            catch
            {
                WriteWindowsLog(strText, EventLogEntryType.Error);
            }
        }

        /*
        // д��ϵͳ��־
        public static void WriteWindowsErrorLog(string strText)
        {
            // Create the source, if it does not already exist.
            if (!EventLog.SourceExists("dp2library"))
            {
                EventLog.CreateEventSource("dp2library", "DigitalPlatform");
            }

            EventLog Log = new EventLog();
            Log.Source = "dp2library";
            Log.WriteEntry(strText, EventLogEntryType.Error);

        }
         * */

        public void WriteDebugInfo(string strTitle)
        {
            if (this.DebugMode == false)
                return;
            StreamUtil.WriteText(this.LogDir + "\\debug.txt", "-- " + DateTime.Now.ToString("u") + " " + strTitle + "\r\n");
        }

        public void WriteAppDownDetectFile(string strText)
        {
            string strTime = DateTime.Now.ToString();
            StreamUtil.WriteText(this.LogDir + "\\app_down_detect.txt",
                strTime + " " + strText + "\r\n");
        }

        public void RemoveAppDownDetectFile()
        {
            try
            {
                File.Delete(this.LogDir + "\\app_down_detect.txt");
            }
            catch
            {
            }
        }

        public bool HasAppBeenKilled()
        {
            try
            {
                FileInfo fi = new FileInfo(this.LogDir + "\\app_down_detect.txt");

                if (fi.Exists == true)
                    return true;

                return false;
            }
            catch
            {
                return true;    // �׳��쳣ʱ������������
            }
        }

        // �쳣�������׳��쳣
        public void Flush()
        {
            try
            {
                this.Save(null, true);
            }
            catch (Exception ex)
            {
                this.WriteErrorLog("Flush()�з����쳣 " + ex.Message);
            }
        }

        // ����
        // ��ʵ,�����ڴ����Ե�XMLƬ��,������this.LibraryCfgDom��ɾ��.���ֱ�Ӻϲ��������dom����.
        // parameters:
        //      bFlush  �Ƿ�Ϊˢ�����Σ�����ǣ���д�������־
        public void Save(string strFileName,
            bool bFlush)
        {
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {

                if (this.m_bChanged == false)
                {
                    /*
                    // ������
                    LibraryApplication.WriteWindowsLog("û�н���Save()����Ϊm_bChanged==false", EventLogEntryType.Information);
                     * */

                    return;
                }


                // �ر��ļ�����
                bool bOldState = false;
                if (this.watcher != null)
                {
                    bOldState = watcher.EnableRaisingEvents;
                    watcher.EnableRaisingEvents = false;
                }


                if (strFileName == null)
                    strFileName = m_strFileName;

                if (strFileName == null)
                {
                    throw (new Exception("m_strFileNameΪ��"));
                }

                string strBackupFilename = strFileName + ".bak";

                if (FileUtil.IsFileExsitAndNotNull(strFileName) == true)
                {
                    this.WriteErrorLog("���� " + strFileName + " �� " + strBackupFilename);
                    File.Copy(strFileName, strBackupFilename, true);
                }

                if (bFlush == false)
                {
                    this.WriteErrorLog("��ʼ ���ڴ�д�� " + strFileName);
                }

                using (XmlTextWriter writer = new XmlTextWriter(strFileName,
                    Encoding.UTF8))
                {

                    // ����
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 4;

                    writer.WriteStartDocument();

                    writer.WriteStartElement("root");
                    if (this.DebugMode == true)
                        writer.WriteAttributeString("debugMode", "true");

                    if (string.IsNullOrEmpty(this.UID) == false)
                        writer.WriteAttributeString("uid", this.UID);

                    // 2008/6/6 nwe add
                    // <version>
                    {
                        XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("version");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }
                    }

                    // �ں˲���
                    // Ԫ��<rmsserver>
                    // ����url/username/password
                    writer.WriteStartElement("rmsserver");
                    writer.WriteAttributeString("url", this.WsUrl);
                    writer.WriteAttributeString("username", this.ManagerUserName);
                    writer.WriteAttributeString("password",
                        Cryptography.Encrypt(this.ManagerPassword, EncryptKey)
                        );
                    writer.WriteEndElement();

                    //2013/11/18
                    // <center>
                    {
                        XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("center");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }
                    }

                    // ԤԼ����
                    // Ԫ��<arrived>
                    // ����dbname/reserveTimeSpan/outofReservationThreshold/canReserveOnshelf
                    writer.WriteStartElement("arrived");
                    writer.WriteAttributeString("dbname", this.ArrivedDbName);
                    writer.WriteAttributeString("reserveTimeSpan", this.ArrivedReserveTimeSpan);

                    // 2007/11/5 
                    writer.WriteAttributeString("outofReservationThreshold", this.OutofReservationThreshold.ToString());
                    writer.WriteAttributeString("canReserveOnshelf", this.CanReserveOnshelf == true ? "true" : "false");

                    writer.WriteEndElement();

                    /*
                    // <arrived>
                    node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//arrived");
                    if (node != null)
                    {
                        //writer.WriteRaw(node.OuterXml);
                        node.WriteTo(writer);
                    }*/

                    // -----------
                    // 2007/11/5 
                    // ��ݵǼ�
                    // Ԫ��<passgate>
                    // ����writeOperLog
                    writer.WriteStartElement("passgate");
                    writer.WriteAttributeString("writeOperLog", this.PassgateWriteToOperLog == true ? "true" : "false");
                    writer.WriteEndElement();

                    // -----------
                    // 2015/7/14
                    // �������
                    // Ԫ��<object>
                    // ���� writeOperLog
                    writer.WriteStartElement("object");
                    writer.WriteAttributeString("writeGetResOperLog", this.GetObjectWriteToOperLog == true ? "true" : "false");
                    writer.WriteEndElement();

                    // ��Ϣ
                    // Ԫ��<message>
                    // ����dbname/reserveTimeSpan
                    writer.WriteStartElement("message");
                    writer.WriteAttributeString("dbname", this.MessageDbName);
                    writer.WriteAttributeString("reserveTimeSpan", this.MessageReserveTimeSpan);    // 2007/11/5 
                    writer.WriteEndElement();

                    /*
                    // ͼ���ҵ�������
                    // Ԫ��<libraryserver>
                    // ����url
                    writer.WriteStartElement("libraryserver");
                    writer.WriteAttributeString("url", this.LibraryServerUrl);
                    writer.WriteEndElement();
                     * */

                    // OPAC������
                    // Ԫ��<opacServer>
                    // ����url
                    writer.WriteStartElement("opacServer");
                    writer.WriteAttributeString("url", this.OpacServerUrl);
                    writer.WriteEndElement();

                    // ΥԼ��
                    // Ԫ��<amerce>
                    // ����dbname/overdueStyle
                    writer.WriteStartElement("amerce");
                    writer.WriteAttributeString("dbname", this.AmerceDbName);
                    writer.WriteAttributeString("overdueStyle", this.OverdueStyle); // 2007/11/5 
                    writer.WriteEndElement();

                    // ��Ʊ
                    // Ԫ��<invoice>
                    // ����dbname
                    writer.WriteStartElement("invoice");
                    writer.WriteAttributeString("dbname", this.InvoiceDbName);
                    writer.WriteEndElement();

                    WriteReaderDbGroupParam(writer);

                    WriteItemDbGroupParam(writer);

                    // û�н����ڴ����Ե�����XMLƬ��
                    if (this.LibraryCfgDom != null)
                    {
                        // <rightsTable>
                        XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//rightsTable");    // 0.02��ǰΪrightstable
                        if (node != null)
                        {
                            // writer.WriteRaw(node.OuterXml);
                            node.WriteTo(writer);
                        }

                        /*
                        // <readertypes>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//readertypes");    // 0.02��ǰΪreadertypes
                        if (node != null)
                        {
                            //writer.WriteRaw(node.OuterXml);
                            node.WriteTo(writer);
                        }

                        // <booktypes>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//booktypes");    // 0.02��ǰΪbooktypes
                        if (node != null)
                        {
                            // writer.WriteRaw(node.OuterXml);
                            node.WriteTo(writer);
                        }
                         * */

                        // <locationTypes>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//locationTypes");    // 0.02��ǰΪlocationtypes
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <accounts>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//accounts");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <browseformats>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//browseformats");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }


                        // <foregift>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//foregift");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <virtualDatabases>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//virtualDatabases");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <valueTables>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//valueTables");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <calendars>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//calendars");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <traceDTLP>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//traceDTLP");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <zhengyuan>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//zhengyuan");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <dkyw>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//dkyw");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <patronReplication>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//patronReplication");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // 2009/7/20 
                        // <clientFineInterface>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//clientFineInterface");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // 2009/9/23 
                        // <yczb>
                        /*
        <yczb>
            <sso appID='CBPM_Library' validateWsUrl='http://portal.cbpmc.cbpm/AuthCenter/services/validate' />
        </yczb>
                         * */
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//yczb");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <script>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("script");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <mailTemplates>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("mailTemplates");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <smtpServer>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("smtpServer");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <externalMessageInterface>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("externalMessageInterface");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        /* ǰ���Ѿ�����
                        // <passgate>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("passgate");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }
                         * */

                        // <zhongcihao>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("zhongcihao");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <callNumber>
                        // 2009/2/18 
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("callNumber");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <monitors>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("monitors");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <dup>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("dup");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <utilDb>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <libraryInfo>
                        // ע: <libraryName>Ԫ���ڴ�����
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("libraryInfo");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <circulation>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("circulation");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <channel>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("channel");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }

                        // <cataloging>
                        node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("cataloging");
                        if (node != null)
                        {
                            node.WriteTo(writer);
                        }
                    }

                    // ʱ��
                    writer.WriteElementString("clock", Convert.ToString(this.Clock.Delta));

                    writer.WriteEndElement();

                    writer.WriteEndDocument();
                }
                // writer.Close();

                if (bFlush == false)
                    this.WriteErrorLog("��� ���ڴ�д�� " + strFileName);

                this.m_bChanged = false;

                if (this.watcher != null)
                {
                    watcher.EnableRaisingEvents = bOldState;
                }

            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

        }

        public void StopAll()
        {
            // ֹͣ���г�����
            lock (this.StopTable)
            {
                foreach (string key in this.StopTable.Keys)
                {
                    StopState stop = (StopState)this.StopTable[key];
                    if (stop != null)
                        stop.Stop();
                }
            }
        }

        public void StopHead(string strHead)
        {
            // ֹͣ����keyƥ��ĳ�����
            lock (this.StopTable)
            {
                foreach (string key in this.StopTable.Keys)
                {
                    if (StringUtil.HasHead(key, strHead) == false)
                        continue;
                    StopState stop = (StopState)this.StopTable[key];
                    stop.Stop();
                }
            }
        }

        public void Stop(string strName)
        {
            // ֹͣ����keyƥ��ĳ�����
            lock (this.StopTable)
            {
                foreach (string key in this.StopTable.Keys)
                {
                    if (key != strName)
                        continue;
                    StopState stop = (StopState)this.StopTable[key];
                    stop.Stop();
                }
            }
        }
        public StopState BeginLoop(string strTitle)
        {
            lock (this.StopTable)
            {
                StopState stop = (StopState)this.StopTable[strTitle];
                if (stop == null)
                {
                    stop = new StopState();
                    this.StopTable[strTitle] = stop;
                }

                stop.Stopped = false;

                return stop;
            }
        }

        public StopState EndLoop(string strTitle,
            bool bRemoveObject)
        {
            lock (this.StopTable)
            {
                if (this.StopTable.Contains(strTitle) == false)
                    return null;
                StopState stop = (StopState)this.StopTable[strTitle];
                stop.Stopped = true;

                if (bRemoveObject == true)
                    this.StopTable.Remove(strTitle);

                return stop;
            }
        }

        public void Close()
        {
            this.EndWather();

            this.HangupReason = LibraryServer.HangupReason.Exit;    // ��ֹ��� API ����

            this.WriteErrorLog("LibraryApplication ��ʼ�½�");

            DateTime start = DateTime.Now;
            try
            {
                // ֹͣ���г�����
                this.StopAll();

                // 2014/12/3
                this.Flush();

                if (this.OperLog != null)
                {
                    this.OperLog.Close(true);   // �Զ�����С�ļ�ģʽ��������Ȼ��Ч
                    // this.OperLog = null; // ����Ҫ�ͷţ���Ȼ������
                }

                if (this.Garden != null)
                {
                    // ����д������ͳ��ָ��
                    this.Garden.CleanPersons(new TimeSpan(0, 0, 0), this.Statis);
                    this.Garden = null;
                }

                if (this.Statis != null)
                {
                    this.Statis.Close();
                    this.Statis = null;
                }

                /*
                if (this.ArriveMonitor != null)
                    this.ArriveMonitor.Close();
                 * */
                if (this.BatchTasks != null)
                {
                    this.BatchTasks.Close();
                    this.BatchTasks = null;
                }

            }
            catch (Exception ex)
            {
                this.WriteErrorLog("LibraryApplication Close()�����쳣: " + ExceptionUtil.GetDebugText(ex));
            }

            TimeSpan delta = DateTime.Now - start;
            this.WriteErrorLog("LibraryApplication ��ֹͣ��ֹͣ�����ķ�ʱ�� " + delta.TotalSeconds.ToString() + " ��");

            this.RemoveAppDownDetectFile();	// ɾ������ļ�

            disposed = true;
        }



        // ��ʼ������⼯�϶������
        public int InitialVdbs(
            RmsChannelCollection Channels,
            out string strError)
        {
            strError = "";

            if (this.vdbs != null)
                return 0;   // �Ż�

            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode(
                    "virtualDatabases");
                if (root == null)
                {
                    strError = "��δ����<virtualDatabases>Ԫ��";
                    return -1;
                }

                XmlNode biblio_dbs_root = this.LibraryCfgDom.DocumentElement.SelectSingleNode(
                    "itemdbgroup");
                /*
                if (root == null)
                {
                    strError = "��δ����<itemdbgroup>Ԫ��";
                    return -1;
                }
                 * */

                this.vdbs = new VirtualDatabaseCollection();
                int nRet = vdbs.Initial(root,
                    Channels,
                    this.WsUrl,
                    biblio_dbs_root,
                    out strError);
                if (nRet == -1)
                {
                    this.vdbs = null;   // 2011/1/29
                    return -1;
                }

                return 0;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
        }


        /*
        // �ж�һ�����ݿ����ǲ��ǺϷ���ʵ�����
        public bool IsItemDbName(string strItemDbName)
        {
            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@name='" + strItemDbName + "']");

            if (node == null)
                return false;

            return true;
        }*/

        // �Ƿ������õĶ��߿���֮��?
        // ע������Ͳ�������ͨ�Ķ��߿ⶼ������
        public bool IsReaderDbName(string strReaderDbName)
        {
            // 2014/11/6
            if (string.IsNullOrEmpty(strReaderDbName) == true)
                return false;

            for (int i = 0; i < this.ReaderDbs.Count; i++)
            {
                if (strReaderDbName == this.ReaderDbs[i].DbName)
                    return true;
            }

            // 2012/7/10
            // �������������ԵĶ��߿���
            if (this.kdbs != null)
            {
                for (int i = 0; i < this.ReaderDbs.Count; i++)
                {
                    KernelDbInfo db = this.kdbs.FindDb(this.ReaderDbs[i].DbName);
                    if (db == null)
                        continue;
                    foreach (Caption caption in db.Captions)
                    {
                        if (strReaderDbName == caption.Value)
                            return true;
                    }
                }
            }

            return false;
        }

        // ��װ�汾
        public bool IsReaderDbName(string strReaderDbName,
    out bool IsInCirculation)
        {
            string strLibraryCode = "";
            return IsReaderDbName(strReaderDbName,
                out IsInCirculation,
                out strLibraryCode);
        }

        // ��װ�汾
        public bool IsReaderDbName(string strReaderDbName,
    out string strLibraryCode)
        {
            bool IsInCirculation = false;
            return IsReaderDbName(strReaderDbName,
                out IsInCirculation,
                out strLibraryCode);
        }

        // �Ƿ������õĶ��߿���֮��?
        // ��һ�汾�������Ƿ������ͨ
        public bool IsReaderDbName(string strReaderDbName,
            out bool IsInCirculation,
            out string strLibraryCode)
        {
            IsInCirculation = false;
            strLibraryCode = "";

            for (int i = 0; i < this.ReaderDbs.Count; i++)
            {
                if (strReaderDbName == this.ReaderDbs[i].DbName)
                {
                    IsInCirculation = this.ReaderDbs[i].InCirculation;
                    strLibraryCode = this.ReaderDbs[i].LibraryCode;


                    return true;
                }
            }

            return false;
        }

        // ���(��Ŀ����ؽ�ɫ)���ݿ�����ͣ�˳�㷵������������Ŀ����
        public string GetDbType(string strDbName,
            out string strBiblioDbName)
        {
            strBiblioDbName = "";

            if (String.IsNullOrEmpty(strDbName) == true)
                return null;

            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                ItemDbCfg cfg = this.ItemDbs[i];
                strBiblioDbName = cfg.BiblioDbName;

                if (strDbName == cfg.DbName)
                    return "item";
                if (strDbName == cfg.BiblioDbName)
                    return "biblio";
                if (strDbName == cfg.IssueDbName)
                    return "issue";
                if (strDbName == cfg.OrderDbName)
                    return "order";
                if (strDbName == cfg.CommentDbName)
                    return "comment";
            }

            // 2012/7/10
            // �������������Ե����ݿ���
            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                ItemDbCfg cfg = this.ItemDbs[i];
                strBiblioDbName = cfg.BiblioDbName;

                if (IsOtherLangName(strDbName, cfg.DbName) == true)
                    return "item";
                if (IsOtherLangName(strDbName, cfg.BiblioDbName) == true)
                    return "biblio";
                if (IsOtherLangName(strDbName, cfg.IssueDbName) == true)
                    return "issue";
                if (IsOtherLangName(strDbName, cfg.OrderDbName) == true)
                    return "order";
                if (IsOtherLangName(strDbName, cfg.CommentDbName) == true)
                    return "comment";
            }

            strBiblioDbName = "";
            return null;
        }

        // ������ݿ������
        public string GetDbType(string strDbName)
        {
            if (String.IsNullOrEmpty(strDbName) == true)
                return null;

            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                if (strDbName == this.ItemDbs[i].DbName)
                    return "item";
                if (strDbName == this.ItemDbs[i].BiblioDbName)
                    return "biblio";
                if (strDbName == this.ItemDbs[i].IssueDbName)
                    return "issue";
                if (strDbName == this.ItemDbs[i].OrderDbName)
                    return "order";
                if (strDbName == this.ItemDbs[i].CommentDbName)
                    return "comment";
            }

            for (int i = 0; i < this.ReaderDbs.Count; i++)
            {
                if (strDbName == this.ReaderDbs[i].DbName)
                    return "reader";
            }

            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strDbName + "']");
            if (node != null)
                return "util";

            return null;
        }

        // 2012/7/6
        // ����Ƿ�Ϊ�������Եĵ�ͬ����
        // parameters:
        //      strDbName   Ҫ�������ݿ���
        //      strNeutralDbName    ��֪�������������ݿ���
        public bool IsOtherLangName(string strDbName,
            string strNeutralDbName)
        {
            if (this.kdbs == null)
                return false;

            KernelDbInfo db = this.kdbs.FindDb(strNeutralDbName);
            if (db == null)
                return false;

            if (db != null)
            {
                foreach (Caption caption in db.Captions)
                {
                    if (strDbName == caption.Value)
                        return true;
                }
            }

            return false;
        }

        // �Ƿ������õ�ʵ�����֮��?
        public bool IsItemDbName(string strItemDbName)
        {
            // 2008/10/16 
            if (String.IsNullOrEmpty(strItemDbName) == true)
                return false;

            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                if (strItemDbName == this.ItemDbs[i].DbName)
                    return true;

                // 2012/7/6
                // �������������ԵĿ���
                if (IsOtherLangName(strItemDbName, this.ItemDbs[i].DbName) == true)
                    return true;
            }

            return false;
        }

        // �Ƿ������õ�ʵ�����֮��?
        // ��һ�汾�������Ƿ������ͨ
        public bool IsItemDbName(string strItemDbName,
            out bool IsInCirculation)
        {
            IsInCirculation = false;

            // 2008/10/16 
            if (String.IsNullOrEmpty(strItemDbName) == true)
                return false;

            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                if (strItemDbName == this.ItemDbs[i].DbName)
                {
                    IsInCirculation = this.ItemDbs[i].InCirculation;
                    return true;
                }

                // 2012/7/6
                // �������������ԵĿ���
                if (IsOtherLangName(strItemDbName, this.ItemDbs[i].DbName) == true)
                    return true;
            }

            return false;
        }

        // �Ƿ�Ϊʵ�ÿ���
        // ʵ�ÿ���� publisher / zhongcihao / dictionary ����
        public bool IsUtilDbName(string strUtilDbName)
        {
            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='"+strUtilDbName+"']");
            if (node == null)
                return false;

            return true;
        }

        // �Ƿ������õ���Ŀ����֮��?
        public ItemDbCfg GetBiblioDbCfg(string strBiblioDbName)
        {
            // 2008/10/16 
            if (String.IsNullOrEmpty(strBiblioDbName) == true)
                return null;


            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                ItemDbCfg cfg = this.ItemDbs[i];
                if (strBiblioDbName == this.ItemDbs[i].BiblioDbName)
                    return cfg;
            }
            return null;
        }

        // �Ƿ����orderWork��ɫ
        public bool IsOrderWorkBiblioDb(string strBiblioDbName)
        {
            // 2008/10/16 
            if (String.IsNullOrEmpty(strBiblioDbName) == true)
                return false;

            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                ItemDbCfg cfg = this.ItemDbs[i];
                if (strBiblioDbName == this.ItemDbs[i].BiblioDbName)
                    return StringUtil.IsInList("orderWork", cfg.Role);
            }
            return false;
        }

        // �Ƿ������õ��ڿ���֮��?
        public bool IsIssueDbName(string strIssueDbName)
        {
            // 2008/10/16 
            if (String.IsNullOrEmpty(strIssueDbName) == true)
                return false;


            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                if (strIssueDbName == this.ItemDbs[i].IssueDbName)
                    return true;

                // 2012/7/6
                // �������������ԵĿ���
                if (IsOtherLangName(strIssueDbName, this.ItemDbs[i].IssueDbName) == true)
                    return true;
            }

            return false;
        }

        // �Ƿ������õĶ�������֮��?
        public bool IsOrderDbName(string strOrderDbName)
        {
            // 2008/10/16 
            if (String.IsNullOrEmpty(strOrderDbName) == true)
                return false;


            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                if (strOrderDbName == this.ItemDbs[i].OrderDbName)
                    return true;

                // 2012/7/6
                // �������������ԵĿ���
                if (IsOtherLangName(strOrderDbName, this.ItemDbs[i].OrderDbName) == true)
                    return true;

            }

            return false;
        }

        // �Ƿ������õ���ע����֮��?
        // 2008/12/8 
        public bool IsCommentDbName(string strCommentDbName)
        {
            if (String.IsNullOrEmpty(strCommentDbName) == true)
                return false;

            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                if (strCommentDbName == this.ItemDbs[i].CommentDbName)
                    return true;

                // 2012/7/6
                // �������������ԵĿ���
                if (IsOtherLangName(strCommentDbName, this.ItemDbs[i].CommentDbName) == true)
                    return true;
            }

            return false;
        }

        // 2012/7/2
        // (ͨ���������Ե���Ŀ����)��������ļ�����ʹ�õ��Ǹ���Ŀ����
        public string GetCfgBiblioDbName(string strBiblioDbName)
        {
            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");

            if (node != null)
                return strBiblioDbName;

            // Ȼ���ע����
            if (this.kdbs == null)
                return null;

            // 2012/7/2
            KernelDbInfo db = this.kdbs.FindDb(strBiblioDbName);
            if (db != null)
            {
                foreach (Caption caption in db.Captions)
                {
                    node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + caption.Value + "']");
                    if (node != null)
                        return caption.Value;
                }
            }

            return null;
        }

        // �ж�һ�����ݿ����ǲ��ǺϷ�����Ŀ����
        public bool IsBiblioDbName(string strBiblioDbName)
        {
            if (GetCfgBiblioDbName(strBiblioDbName) == null)
                return false;
            return true;
        }

#if NO
        // �ж�һ�����ݿ����ǲ��ǺϷ�����Ŀ����
        public bool IsBiblioDbName(string strBiblioDbName)
        {
            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");

            if (node != null)
                return true;

            // Ȼ���ע����
            if (this.kdbs == null)
                return false;

            // 2012/7/2
            KernelDbInfo db = this.kdbs.FindDb(strBiblioDbName);
            foreach (Caption caption in db.Captions)
            {
                node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + caption.Value + "']");
                if (node != null)
                    return true;
            }

            return false;
        }
#endif

        // TODO: �����Ը���
        // ������Ŀ��������, �ҵ���Ӧ����Ŀ����
        // ע�⣬����1��ʱ��strBiblioDbNameҲ�п���Ϊ��
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        public int GetBiblioDbNameByChildDbName(string strChildDbName,
            out string strBiblioDbName,
            out string strError)
        {
            strError = "";
            strBiblioDbName = "";

            string[] names = new string[] { "name", "orderDbName", "issueDbName", "commentDbName" };

            XmlNode node = null;

            foreach (string strName in names)
            {
                node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@" + strName + "='" + strChildDbName + "']");
                if (node != null)
                    goto FOUND;
            }

            strError = "û���ҵ���Ϊ '" + strChildDbName + "' ����������";
            return 0;

        FOUND:
            strBiblioDbName = DomUtil.GetAttr(node, "biblioDbName");
            return 1;
        }

        // ����ʵ�����, �ҵ���Ӧ����Ŀ����
        // ע�⣬����1��ʱ��strBiblioDbNameҲ�п���Ϊ��
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        public int GetBiblioDbNameByItemDbName(string strItemDbName,
            out string strBiblioDbName,
            out string strError)
        {
            strError = "";
            strBiblioDbName = "";

            // 2007/5/25 new changed
            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@name='" + strItemDbName + "']");

            if (node == null)
            {
                // 2012/7/9
                // Ȼ���ע����
                if (this.kdbs != null)
                {
                    KernelDbInfo db = this.kdbs.FindDb(strItemDbName);
                    if (db != null)
                    {
                        foreach (Caption caption in db.Captions)
                        {
                            node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@name='" + caption.Value + "']");
                            if (node != null)
                                goto FOUND;
                        }
                    }
                }

                strError = "û���ҵ���Ϊ '" + strItemDbName + "' ��ʵ���";
                return 0;
            }

            FOUND:
            strBiblioDbName = DomUtil.GetAttr(node, "biblioDbName");
            return 1;

            /*
            if (this.GlobalCfgDom == null)
            {
                strError = "GlobalCfgDom��δ��ʼ��";
                return -1;
            }

            XmlNodeList nodes = this.GlobalCfgDom.DocumentElement.SelectNodes("//dblink");

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strBiblioDb = DomUtil.GetAttr(node, "bibliodb");

                string strItemDb = DomUtil.GetAttr(node, "itemdb");

                if (strItemDbName == strItemDb)
                {
                    strBiblioDbName = strBiblioDb;
                    return 1;
                }
            }

            return 0;
             * */
        }

        // ������ע����, �ҵ���Ӧ����Ŀ����
        // ע�⣬����1��ʱ��strBiblioDbNameҲ�п���Ϊ��
        // 2009/10/18 
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        public int GetBiblioDbNameByCommentDbName(string strCommentDbName,
            out string strBiblioDbName,
            out string strError)
        {
            strError = "";
            strBiblioDbName = "";

            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@commentDbName='" + strCommentDbName + "']");

            if (node == null)
            {
                // 2012/7/9
                // Ȼ���ע����
                if (this.kdbs != null)
                {
                    KernelDbInfo db = this.kdbs.FindDb(strCommentDbName);
                    if (db != null)
                    {
                        foreach (Caption caption in db.Captions)
                        {
                            node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@commentDbName='" + caption.Value + "']");
                            if (node != null)
                                goto FOUND;
                        }
                    }
                }

                strError = "û���ҵ���Ϊ '" + strCommentDbName + "' ����ע��";
                return 0;
            }

            FOUND:
            strBiblioDbName = DomUtil.GetAttr(node, "biblioDbName");
            return 1;
        }

        // ���ݶ�������, �ҵ���Ӧ����Ŀ����
        // ע�⣬����1��ʱ��strBiblioDbNameҲ�п���Ϊ��
        // 2008/8/28 
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        public int GetBiblioDbNameByOrderDbName(string strOrderDbName,
            out string strBiblioDbName,
            out string strError)
        {
            strError = "";
            strBiblioDbName = "";

            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@orderDbName='" + strOrderDbName + "']");

            if (node == null)
            {
                // 2012/7/9
                // Ȼ���ע����
                if (this.kdbs != null)
                {
                    KernelDbInfo db = this.kdbs.FindDb(strOrderDbName);
                    if (db != null)
                    {
                        foreach (Caption caption in db.Captions)
                        {
                            node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@orderDbName='" + caption.Value + "']");
                            if (node != null)
                                goto FOUND;
                        }
                    }
                }

                strError = "û���ҵ���Ϊ '" + strOrderDbName + "' �Ķ�����";
                return 0;
            }

            FOUND:
            strBiblioDbName = DomUtil.GetAttr(node, "biblioDbName");
            return 1;
        }

        // �����ڿ���, �ҵ���Ӧ����Ŀ����
        // ע�⣬����1��ʱ��strBiblioDbNameҲ�п���Ϊ��
        // 2009/2/2 
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        public int GetBiblioDbNameByIssueDbName(string strIssueDbName,
            out string strBiblioDbName,
            out string strError)
        {
            strError = "";
            strBiblioDbName = "";

            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@issueDbName='" + strIssueDbName + "']");

            if (node == null)
            {
                // 2012/7/9
                // Ȼ���ע����
                if (this.kdbs != null)
                {
                    KernelDbInfo db = this.kdbs.FindDb(strIssueDbName);
                    if (db != null)
                    {
                        foreach (Caption caption in db.Captions)
                        {
                            node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@issueDbName='" + caption.Value + "']");
                            if (node != null)
                                goto FOUND;
                        }
                    }
                }

                strError = "û���ҵ���Ϊ '" + strIssueDbName + "' ���ڿ�";
                return 0;
            }

            FOUND:
            strBiblioDbName = DomUtil.GetAttr(node, "biblioDbName");
            return 1;
        }

        // ��ü����洢�����б�
        // ��ν�����洢�⣬���������洢�����Ƽ�������Ŀ��¼��Ŀ���
        public List<string> GetOrderRecommendStoreDbNames()
        {
            List<string> results = new List<string>();
            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                ItemDbCfg cfg = this.ItemDbs[i];
                if (StringUtil.IsInList("orderRecommendStore", cfg.Role) == true)
                    results.Add(cfg.BiblioDbName);
            }
            return results;
        }

        // ������Ŀ����, �ҵ���Ӧ��ʵ�����
        // ע������1��ʱ��strItemDbName��Ȼ����Ϊ�ա�1ֻ�Ǳ�ʾ�ҵ�����Ŀ�ⶨ�壬���ǲ�ȷ����ʵ��ⶨ��
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�(��Ŀ�ⶨ�壬���ǲ�ȷ��ʵ������)
        public int GetItemDbName(string strBiblioDbName,
            out string strItemDbName,
            out string strError)
        {
            strError = "";
            strItemDbName = "";

            // 2007/5/25 new changed
            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");

            if (node == null)
            {
                // ���û���ҵ�������<caption>
                VirtualDatabase vdb = this.vdbs[strBiblioDbName];
                if (vdb == null)
                    return 0;

                strBiblioDbName = vdb.GetName("zh");
                if (String.IsNullOrEmpty(strBiblioDbName) == true)
                {
                    strError = "���ݿ� "+vdb.GetName(null) +" ��Ȼû�� zh ���Ե�����";
                    return -1;
                }

                // �ٴλ��
                node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");

                if (node == null)
                    return 0;
            }
            strItemDbName = DomUtil.GetAttr(node, "name");
            return 1;

            /*
            if (this.GlobalCfgDom == null)
            {
                strError = "GlobalCfgDom��δ��ʼ��";
                return -1;
            }

            XmlNodeList nodes = this.GlobalCfgDom.DocumentElement.SelectNodes("//dblink");

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strBiblioDb = DomUtil.GetAttr(node, "bibliodb");

                string strItemDb = DomUtil.GetAttr(node, "itemdb");

                if (strBiblioDbName == strBiblioDb)
                {
                    strItemDbName = strItemDb;
                    return 1;
                }
            }
            return 0;
             * */

        }

        // ������Ŀ����, �ҵ���Ӧ���ڿ���
        // return:
        //      -1  ����
        //      0   û���ҵ�(��Ŀ��)
        //      1   �ҵ�
        public int GetIssueDbName(string strBiblioDbName,
            out string strIssueDbName,
            out string strError)
        {
            strError = "";
            strIssueDbName = "";

            // 2007/5/25 new changed
            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");

            if (node == null)
            {
                // ���û���ҵ�������<caption>
                VirtualDatabase vdb = this.vdbs[strBiblioDbName];
                if (vdb == null)
                    return 0;

                strBiblioDbName = vdb.GetName("zh");
                if (String.IsNullOrEmpty(strBiblioDbName) == true)
                {
                    strError = "���ݿ� " + vdb.GetName(null) + " ��Ȼû�� zh ���Ե�����";
                    return -1;
                }

                // �ٴλ��
                node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");

                if (node == null)
                    return 0;
            }

            strIssueDbName = DomUtil.GetAttr(node, "issueDbName");
            return 1;   // ע����ʱ��Ȼ�ҵ�����Ŀ�⣬����issueDbName����ȱʡ����Ϊ��
        }

        // ������Ŀ����, �ҵ���Ӧ�Ķ�������
        // return:
        //      -1  ����
        //      0   û���ҵ�(��Ŀ��)
        //      1   �ҵ�
        public int GetOrderDbName(string strBiblioDbName,
            out string strOrderDbName,
            out string strError)
        {
            strError = "";
            strOrderDbName = "";

            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");

            if (node == null)
            {
                // ���û���ҵ�������<caption>
                VirtualDatabase vdb = this.vdbs[strBiblioDbName];
                if (vdb == null)
                    return 0;

                strBiblioDbName = vdb.GetName("zh");
                if (String.IsNullOrEmpty(strBiblioDbName) == true)
                {
                    strError = "���ݿ� " + vdb.GetName(null) + " ��Ȼû�� zh ���Ե�����";
                    return -1;
                }

                // �ٴλ��
                node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");

                if (node == null)
                    return 0;
            }

            strOrderDbName = DomUtil.GetAttr(node, "orderDbName");
            return 1;   // ע����ʱ��Ȼ�ҵ�����Ŀ�⣬����orderDbName����ȱʡ����Ϊ��
        }

        // ������Ŀ����, �ҵ���Ӧ����ע����
        // 2008/12/8
        // return:
        //      -1  ����
        //      0   û���ҵ�(��Ŀ��)
        //      1   �ҵ�
        public int GetCommentDbName(string strBiblioDbName,
            out string strCommentDbName,
            out string strError)
        {
            strError = "";
            strCommentDbName = "";

            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");

            if (node == null)
            {
                // ���û���ҵ�������<caption>
                VirtualDatabase vdb = this.vdbs[strBiblioDbName];
                if (vdb == null)
                    return 0;

                strBiblioDbName = vdb.GetName("zh");
                if (String.IsNullOrEmpty(strBiblioDbName) == true)
                {
                    strError = "���ݿ� " + vdb.GetName(null) + " ��Ȼû�� zh ���Ե�����";
                    return -1;
                }

                // �ٴλ��
                node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");

                if (node == null)
                    return 0;
            }
            strCommentDbName = DomUtil.GetAttr(node, "commentDbName");
            return 1;   // ע����ʱ��Ȼ�ҵ�����Ŀ�⣬����commentDbName����ȱʡ����Ϊ��
        }

        // ��δָ�����Ե�����»��ȫ��<caption>��
        public static List<string> GetAllNames(XmlNode parent)
        {
            List<string> results = new List<string>();

            XmlNodeList nodes = parent.SelectNodes("caption");
            for (int i = 0; i < nodes.Count; i++)
            {
                results.Add(nodes[i].InnerText);
            }

            return results;
        }

        static string m_strKernelBrowseFomatsXml = 
            "<formats> "
            + "<format name='browse' type='kernel'>"
            + "    <caption lang='zh-cn'>���</caption>"
            + "    <caption lang='en'>Browse</caption>"
            + "</format>"
            + "<format name='MARC' type='kernel'>"
            + "    <caption lang='zh-cn'>MARC</caption>"
            + "    <caption lang='en'>MARC</caption>"
            + "</format>"
            + "</formats>";

        // 2011/1/2
        // �Ƿ�Ϊ���ø�ʽ��
        // paramters:
        //      strNeutralName  �������������֡����� browse / MARC����Сд������
        public static bool IsKernelFormatName(string strName,
            string strNeutralName)
        {
            if (strName.ToLower() == strNeutralName.ToLower())
                return true;

            // �ȴ����õĸ�ʽ������
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(m_strKernelBrowseFomatsXml);

            XmlNodeList format_nodes = dom.DocumentElement.SelectNodes("format");
            for (int j = 0; j < format_nodes.Count; j++)
            {
                XmlNode node = format_nodes[j];

                List<string> captions = GetAllNames(node);
                if (captions.IndexOf(strName) == -1)
                    continue;

                if (DomUtil.GetAttr(node, "name").ToLower() == strNeutralName.ToLower())
                    return true;
            }

            return false;
        }

        // 2011/1/2
        // ����ض����Եĸ�ʽ��
        // �������õĸ�ʽ
        public string GetBrowseFormatName(string strName,
            string strLang)
        {
            // �ȴ����õĸ�ʽ������
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(m_strKernelBrowseFomatsXml);

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("format");
            string strFormat = GetBrowseFormatName(
                nodes,
                strName,
                strLang);
            if (String.IsNullOrEmpty(strFormat) == false)
                return strFormat;

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("browseformats");
            if (root == null)
            {
                // string strError = "<browseformats>Ԫ����δ����...";
                // TODO: �׳��쳣?
                return null;
            }

            // Ȼ����û�����ĸ�ʽ������
            nodes = root.SelectNodes("database/format");
            return GetBrowseFormatName(
                nodes,
                strName,
                strLang);
        }

        // 2011/1/2
        static string GetBrowseFormatName(
            XmlNodeList format_nodes,
            string strName,
            string strLang)
        {

            for (int j = 0; j < format_nodes.Count; j++)
            {
                XmlNode node = format_nodes[j];

                List<string> captions = GetAllNames(node);
                if (captions.IndexOf(strName) == -1)
                    continue;

                string strFormatName = DomUtil.GetCaption(strLang, node);
                if (String.IsNullOrEmpty(strFormatName) == false)
                    return strFormatName;
            }

            return null;    // not found
        }

#if NO
        // ����ض����Եĸ�ʽ��
        public string GetBrowseFormatName(
            string strName,
            string strLang)
        {
            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("browseformats");
            if (root == null)
            {
                // string strError = "<browseformats>Ԫ����δ����...";
                // TODO: �׳��쳣?
                return null;
            }

            XmlNodeList dbnodes = root.SelectNodes("database");
            for (int i = 0; i < dbnodes.Count; i++)
            {
                XmlNode nodeDatabase = dbnodes[i];

                string strDbName = DomUtil.GetAttr(nodeDatabase, "name");


                XmlNodeList nodes = nodeDatabase.SelectNodes("format");
                for (int j = 0; j < nodes.Count; j++)
                {
                    XmlNode node = nodes[j];

                    List<string> captions = GetAllNames(node);
                    if (captions.IndexOf(strName) == -1)
                        continue;

                    string strFormatName = DomUtil.GetCaption(strLang, node);
                    if (String.IsNullOrEmpty(strFormatName) == false)
                        return strFormatName;
                }
            }

            return null;    // not found
        }
#endif

        // ���һЩ���ݿ��ȫ�������ʽ������Ϣ
        // parameters:
        //      dbnames Ҫ�г���Щ���ݿ�������ʽ�����==null, ���ʾ�г�ȫ�����ܵĸ�ʽ��
        // return:
        //      -1  ����
        //      >=0 formatname����
        public int GetBrowseFormatNames(
            string strLang,
            List<string> dbnames,
            out List<string> formatnames,
            out string strError)
        {
            strError = "";
            formatnames = new List<string>();

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("browseformats");
            if (root == null)
            {
                strError = "<browseformats>Ԫ����δ����...";
                return -1;
            }

            XmlNodeList dbnodes = root.SelectNodes("database");
            for (int i = 0; i < dbnodes.Count; i++)
            {
                XmlNode nodeDatabase = dbnodes[i];

                string strDbName = DomUtil.GetAttr(nodeDatabase, "name");

                // dbnames���==null, ���ʾ�г�ȫ�����ܵĸ�ʽ��
                if (dbnames != null)
                {
                    if (dbnames.IndexOf(strDbName) == -1)
                        continue;
                }

                XmlNodeList nodes = nodeDatabase.SelectNodes("format");
                for (int j = 0; j < nodes.Count; j++)
                {
                    XmlNode node = nodes[j];

                    string strFormatName = DomUtil.GetCaption(strLang, node);
                    if (String.IsNullOrEmpty(strFormatName) == true)
                        strFormatName = DomUtil.GetAttr(node, "name");

                    /*
                    if (String.IsNullOrEmpty(strFormatName) == true)
                    {
                        strError = "��ʽ����Ƭ�� '" + node.OuterXml + "' ��ʽ����ȷ...";
                        return -1;
                    }*/

                    if (formatnames.IndexOf(strFormatName) == -1)
                        formatnames.Add(strFormatName);
                }

            }

            // 2011/1/2
            // �����õĸ�ʽ������
            // TODO: ��һЩ��������MARC��ʽ�����ݿ⣬�ų�"MARC"��ʽ��
            {
                XmlDocument dom = new XmlDocument();
                dom.LoadXml(m_strKernelBrowseFomatsXml);

                XmlNodeList nodes = dom.DocumentElement.SelectNodes("format");
                for (int j = 0; j < nodes.Count; j++)
                {
                    XmlNode node = nodes[j];

                    string strFormatName = DomUtil.GetCaption(strLang, node);
                    if (String.IsNullOrEmpty(strFormatName) == true)
                        strFormatName = DomUtil.GetAttr(node, "name");

                    if (formatnames.IndexOf(strFormatName) == -1)
                        formatnames.Add(strFormatName);
                }
            }

            return formatnames.Count;
        }

        // ���һ�����ݿ��ȫ�������ʽ������Ϣ
        // return:
        //      -1  ����
        //      0   û�����á�����ԭ����strError��
        //      >=1 format����
        public int GetBrowseFormats(string strDbName,
            out List<BrowseFormat> formats,
            out string strError)
        {
            strError = "";
            formats = null;

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("browseformats");
            if (root == null)
            {
                strError = "<browseformats>Ԫ����δ����...";
                return -1;
            }

            XmlNode node = root.SelectSingleNode("database[@name='" + strDbName + "']");
            if (node == null)
            {
                strError = "������ݿ� '" + strDbName + "' û����<browseformats>������<database>����";
                return 0;
            }

            formats = new List<BrowseFormat>();

            XmlNodeList nodes = node.SelectNodes("format");
            for (int i = 0; i < nodes.Count; i++)
            {
                node = nodes[i];
                BrowseFormat format = new BrowseFormat();
                format.Name = DomUtil.GetAttr(node, "name");
                format.Type = DomUtil.GetAttr(node, "type");
                format.ScriptFileName = DomUtil.GetAttr(node, "scriptfile");
                formats.Add(format);
            }

            if (nodes.Count == 0)
            {
                strError = "���ݿ� '" + strDbName + "' ��<browseformats>�µ�<database>Ԫ���£�һ��<format>Ԫ��Ҳδ���á�";
            }

            return nodes.Count;
        }

        // ���һ�����ݿ��һ�������ʽ������Ϣ
        // parameters:
        //      strDbName   "zh"���Ե����ݿ�����Ҳ����<browseformats>��<database>Ԫ�ص�name�����ڵ����ݿ�����
        //      strFormatName   ������ѡ���ĸ�ʽ����ע�⣬��һ������������this.Lang���Ե�
        // return:
        //      0   û������
        //      1   �ɹ�
        public int GetBrowseFormat(string strDbName,
            string strFormatName,
            out BrowseFormat format,
            out string strError)
        {
            strError = "";
            format = null;

            // �ȴ�ȫ��<format>Ԫ�������ȫ��<caption>����
            XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode(
                "browseformats/database[@name='" + strDbName + "']");

            if (nodeDatabase == null)
            {
                strError = "���ݿ��� '" + strDbName + "' ��<browseformats>Ԫ����û���ҵ�ƥ���<database>Ԫ��";
                return -1;
            }

            XmlNode nodeFormat = null;

            XmlNodeList nodes = nodeDatabase.SelectNodes("format");
            for (int j = 0; j < nodes.Count; j++)
            {
                XmlNode node = nodes[j];

                List<string> captions = GetAllNames(node);
                if (captions.IndexOf(strFormatName) != -1)
                {
                    nodeFormat = node;
                    break;
                }
            }

            // �ٴ�<format>Ԫ�ص�name��������
            if (nodeFormat == null)
            {
                nodeFormat = nodeDatabase.SelectSingleNode(
                    "format[@name='" + strFormatName + "']");
                if (nodeFormat == null)
                {
                    return 0;
                }
            }

            format = new BrowseFormat();
            format.Name = DomUtil.GetAttr(nodeFormat, "name");
            format.Type = DomUtil.GetAttr(nodeFormat, "type");
            format.ScriptFileName = DomUtil.GetAttr(nodeFormat, "scriptfile");

            return 1;
        }


        // ��library.xml�ļ����� ���һ���ʻ�����Ϣ
        // TODO: ��������ʾ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int GetAccount(string strUserID,
            out Account account,
            out string strError)
        {
            strError = "";
            account = null;

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("accounts");
            if (root == null)
            {
                strError = "<accounts>Ԫ����δ����...";
                return -1;
            }

            XmlElement node = root.SelectSingleNode("account[@name='"+strUserID+"']") as XmlElement;
            if (node == null)
            {
                strError = "�û� '"+strUserID+"' ������";
                return 0;
            }

            account = new Account();
            account.XmlNode = node;
            account.LoginName = node.GetAttribute("name");
            account.UserID = node.GetAttribute("name");

            string strText = "";
            try
            {
                strText =  node.GetAttribute("password");
                if (String.IsNullOrEmpty(strText) == true)
                    account.Password = "";
                else
                {
#if NO
                    // ��ǰ��������ȡ����������
                    account.Password = Cryptography.Decrypt(
                                strText,
                                EncryptKey);
#endif
                    // ���ڵ�������ȡ������� hashed �ַ���
                    account.Password = strText;
                }
            }
            catch
            {
                strError = "�û���Ϊ '" + strUserID + "' ��<account> password����ֵ����";
                return -1;
            }
            account.Type = DomUtil.GetAttr(node, "type");
            account.Rights = DomUtil.GetAttr(node, "rights");
            account.AccountLibraryCode = DomUtil.GetAttr(node, "libraryCode");

            account.Access = DomUtil.GetAttr(node, "access");
            account.RmsUserName = DomUtil.GetAttr(node, "rmsUserName");

            try
            {
                strText =  DomUtil.GetAttr(node, "rmsPassword");
                if (String.IsNullOrEmpty(strText) == true)
                    account.RmsPassword = "";
                else
                {
                    account.RmsPassword = Cryptography.Decrypt(
                              strText,
                              EncryptKey);
                }
            }
            catch
            {
                strError = "�û���Ϊ '" + strUserID + "' ��<account> rmsPassword����ֵ����";
                return -1;
            }                

            return 1;
        }

        // TODO���ж�strItemBarcode�Ƿ�Ϊ��
        // ���ԤԼ������м�¼
        // parameters:
        //      strItemBarcodeParam  ������š�����ʹ�� @refID: ǰ׺
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetArrivedQueueRecXml(
            RmsChannelCollection channels,
            string strItemBarcodeParam,
            out string strXml,
            out byte[] timestamp,
            out string strOutputPath,
            out string strError)
        {
            strOutputPath = "";
            strXml = "";
            strError = "";
            timestamp = null;

            LibraryApplication app = this;
            string strFrom = "������";

            // ע���ɵģ�Ҳ���� 2015/5/7 ��ǰ�� ԤԼ������п����沢û�� �ο�ID �����㣬����ֱ���ô��� @refID ǰ׺���ַ������м������ɡ�
            // �ȶ��п��ձ�ˢ�¼������Ժ󣬸�Ϊʹ������һ�δ���
            if (this.ArrivedDbKeysContainsRefIDKey() == true)
            {
                string strHead = "@refID:";

                if (StringUtil.HasHead(strItemBarcodeParam, strHead, true) == true)
                {
                    strFrom = "��ο�ID";
                    strItemBarcodeParam = strItemBarcodeParam.Substring(strHead.Length).Trim();
                    if (string.IsNullOrEmpty(strItemBarcodeParam) == true)
                    {
                        strError = "���� strItemBarcodeParam ֵ�вο�ID���ֲ�ӦΪ��";
                        return -1;
                    }
                }
            }

            // �������ʽ
            // 2007/4/5 ���� ������ GetXmlStringSimple()
            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(app.ArrivedDbName + ":" + strFrom)
                + "'><item><word>"
                + StringUtil.GetXmlStringSimple(strItemBarcodeParam)
                + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>1000</maxCount></item><lang>zh</lang></target>";

            RmsChannel channel = channels.GetChannel(app.WsUrl);
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
                1,
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

            string strMetaData = "";

            lRet = channel.GetRes(aPath[0],
                out strXml,
                out strMetaData,
                out timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

        // 2007/6/27
        // ���ͨ�ü�¼
        // �������ɻ�ó���1�����ϵ�·��
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetRecXml(
            RmsChannelCollection channels,
            string strQueryXml,
            out string strXml,
            int nMax,
            out List<string> aPath,
            out byte[] timestamp,
            out string strError)
        {
            aPath = null;

            strXml = "";
            strError = "";
            timestamp = null;

            LibraryApplication app = this;

            RmsChannel channel = channels.GetChannel(app.WsUrl);
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
                strError = "û�����м�¼";
                return 0;
            }

            long lHitCount = lRet;

            // List<string> aPath = null;
            lRet = channel.DoGetSearchResult(
                "default",
                0,
                Math.Min(nMax, lHitCount),
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            Debug.Assert(aPath != null, "");

            if (aPath.Count == 0)
            {
                strError = "DoGetSearchResult aPath error";
                goto ERROR1;
            }

            string strMetaData = "";
            string strOutputPath = "";

            lRet = channel.GetRes(aPath[0],
                out strXml,
                out strMetaData,
                out timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

        // ��װ��İ汾
        public int GetReaderRecXml(
    RmsChannelCollection channels,
    string strBarcode,
    out string strXml,
    out string strOutputPath,
    out string strError)
        {
            byte[] timestamp = null;

            return GetReaderRecXml(
                channels,
                strBarcode,
                out strXml,
                out strOutputPath,
                out timestamp,
                out strError);
        }

        // TODO: �ж�strBorrowItemBarcode�Ƿ�Ϊ��
        // ͨ�������������š���ö��߼�¼
        // �������ɻ�ó���1�����ϵ�·��
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetReaderRecXml(
            RmsChannelCollection channels,
            string strBorrowItemBarcode,
            out string strXml,
            int nMax,
            out List<string> aPath,
            out byte[] timestamp,
            out string strError)
        {
            aPath = null;

            strXml = "";
            strError = "";
            timestamp = null;

            LibraryApplication app = this;

            // �������ʽ
            string strQueryXml = "";
            for (int i = 0; i < app.ReaderDbs.Count; i++)
            {
                string strDbName = app.ReaderDbs[i].DbName;

                Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "");

                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + "���������")
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strBorrowItemBarcode)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>"+nMax.ToString()+"</maxCount></item><lang>zh</lang></target>";

                if (i > 0)
                    strQueryXml += "<operator value='OR'/>";

                strQueryXml += strOneDbQuery;
            }

            if (app.ReaderDbs.Count > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
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
                strError = "���������� '" + strBorrowItemBarcode + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            // List<string> aPath = null;
            lRet = channel.DoGetSearchResult(
                "default",
                0,
                Math.Min(nMax, lHitCount),
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            Debug.Assert(aPath != null, "");

            if (aPath.Count == 0)
            {
                strError = "DoGetSearchResult aPath error";
                goto ERROR1;
            }

            string strMetaData = "";
            // byte[] timestamp = null;
            string strOutputPath = "";

            lRet = channel.GetRes(aPath[0],
                out strXml,
                out strMetaData,
                out timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

#if SLOWLY
        // TODO�� �ж�strBarcode�Ƿ�Ϊ��
        // ͨ������֤����Ż�ö��߼�¼
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetReaderRecXml(
            RmsChannelCollection channels,
            string strBarcode,
            out string strXml,
            out string strOutputPath,
            out byte[] timestamp,
            out string strError)
        {
            strOutputPath = "";
            timestamp = null;
            strXml = "";
            strError = "";

            LibraryApplication app = this;

            int nInCount = 0;   // ������ͨ�Ķ��߿����

            // �������ʽ
            string strQueryXml = "";
            for (int i = 0; i < app.ReaderDbs.Count; i++)
            {
                nInCount++;

                string strDbName = app.ReaderDbs[i].DbName;

                Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "");

                // 2007/4/5 ���� ������ GetXmlStringSimple()
                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + "֤����")       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strBarcode)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>1000</maxCount></item><lang>zh</lang></target>";

                if (i > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
            }

            if (nInCount == 0)
            {
                strError = "��ǰ��û�����ö��߿�";
                return -1;
            }

            if (app.ReaderDbs.Count > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
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
                strError = "����֤����� '" + strBarcode + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;


#if OPTIMIZE_API
            List<RichRecord> records = null;
            lRet = channel.GetRichRecords(
                "default",
                0,
                1,
                "path,xml,timestamp",
                "zh",
                out records,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            if (records == null)
            {
                strError = "records == null";
                goto ERROR1;
            }

            if (records.Count < 1)
            {
                strError = "records.Count < 1";
                goto ERROR1;
            }

            strXml = records[0].Xml;
            timestamp = records[0].baTimestamp;
            strOutputPath = records[0].Path;
#else 

            List<string> aPath = null;
            lRet = channel.DoGetSearchResult(
                "default",
                0,
                1,
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

            string strMetaData = "";
            // byte[] timestamp = null;
            // string strOutputPath = "";

            lRet = channel.GetRes(aPath[0],
                out strXml,
                out strMetaData,
                out timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;
#endif

            return (int)lHitCount;
        ERROR1:
            return -1;
        }
#endif

        // 2013/5/23
        // ��װ�Ժ�İ汾
        public int GetReaderRecXml(
    RmsChannelCollection channels,
    string strBarcode,
    out string strXml,
    out string strOutputPath,
    out byte[] timestamp,
    out string strError)
        {
            strOutputPath = "";
            List<string> recpaths = null;
            int nRet = GetReaderRecXml(
            channels,
            strBarcode,
            1,
            "",
            out recpaths,
            out strXml,
            out timestamp,
            out strError);
            if (recpaths != null && recpaths.Count > 0)
                strOutputPath = recpaths[0];

            return nRet;
        }

        // 2012/1/5 ����ΪPiggyBack����
        // 2013/5/23 ����Ϊ���Է����������е� ��¼·��
        // TODO�� �ж�strBarcode�Ƿ�Ϊ��
        // ͨ������֤����Ż�ö��߼�¼
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetReaderRecXml(
            RmsChannelCollection channels,
            string strBarcode,
            int nMax,
            string strLibraryCodeList,
            out List<string> recpaths,
            out string strXml,
            // out string strOutputPath,
            out byte[] timestamp,
            out string strError)
        {
            // strOutputPath = "";
            timestamp = null;
            strXml = "";
            strError = "";
            int nRet = 0;

            recpaths = new List<string>();

            LibraryApplication app = this;

            List<string> dbnames = new List<string>();
            // ��ö��߿����б�
            // parameters:
            //      strReaderDbNames    �����б��ַ��������Ϊ�գ����ʾȫ�����߿�
            nRet = GetDbNameList("",
                strLibraryCodeList,
                out dbnames,
                out strError);
            if (nRet == -1)
                return -1;


            // �������ʽ
            string strQueryXml = "";
            // int nInCount = 0;   // ������ͨ�Ķ��߿����
            foreach (string strDbName in dbnames)
            {
                // string strDbName = app.ReaderDbs[i].DbName;

                if (string.IsNullOrEmpty(strDbName) == true)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                // 2007/4/5 ���� ������ GetXmlStringSimple()
                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + "֤����")  // TODO: ����ͳһ�޸�Ϊ��֤����š�     // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strBarcode)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>1000</maxCount></item><lang>zh</lang></target>";

                if (String.IsNullOrEmpty(strQueryXml) == false) // i > 0
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;

                // nInCount++;
            }

            if (string.IsNullOrEmpty(strQueryXml) == true /*nInCount == 0*/)
            {
                if (app.ReaderDbs.Count == 0)
                    strError = "��ǰ��û�����ö��߿�";
                else
                    strError = "��ǰû�п��Բ����Ķ��߿�";
                return -1;
            }

            if (dbnames.Count > 0/*nInCount > 0*/)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            Record[] records = null;
            long lRet = channel.DoSearchEx(strQueryXml,
                "default",
                "", // strOuputStyle
                1,
                "zh",
                "id,xml,timestamp",
                out records,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = "����֤����� '" + strBarcode + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            if (records == null || records.Length == 0)
            {
                strError = "records error";
                return -1;
            }

            Debug.Assert(records[0].RecordBody != null, "");

            // strOutputPath = records[0].Path;
            if (nMax >= 1)
                recpaths.Add(records[0].Path);
            strXml = records[0].RecordBody.Xml;
            timestamp = records[0].RecordBody.Timestamp;

            // ������н������һ�����������õ�һ���Ժ�ĸ�����path
            if (lHitCount > 1 && nMax > 1)
            {
                // List<string> temp = null;
                lRet = channel.DoGetSearchResult(
                    "default",
                    0,
                    Math.Min(nMax, lHitCount),
                    "zh",
                    null,
                    out recpaths,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                Debug.Assert(recpaths != null, "");

                if (recpaths.Count == 0)
                {
                    strError = "DoGetSearchResult aPath error";
                    goto ERROR1;
                }
            }

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

        // ��װ��汾
        // TODO: �ж�strDisplayName�Ƿ�Ϊ��
        // ͨ��������ʾ����ö��߼�¼
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetReaderRecXmlByDisplayName(
            RmsChannelCollection channels,
            string strDisplayName,
            out string strXml,
            out string strOutputPath,
            out byte[] timestamp,
            out string strError)
        {
            return GetReaderRecXmlByFrom(
            channels,
            strDisplayName,
            "��ʾ��",
            out strXml,
            out strOutputPath,
            out timestamp,
            out strError);
        }

        // ��װ��İ汾
        public int GetReaderRecXmlByFrom(
    RmsChannelCollection channels,
    string strWord,
    string strFrom,
    out string strXml,
    out string strOutputPath,
    out byte[] timestamp,
    out string strError)
        {
            return GetReaderRecXmlByFrom(
    channels,
    null,
    strWord,
    strFrom,
    out strXml,
    out strOutputPath,
    out timestamp,
    out strError);
        }

#if SLOWLY
        // TODO: �ж�strWord�Ƿ�Ϊ��
        // ͨ���ض�����;����ö��߼�¼
        // parameters:
        //      strReaderDbNames    ���߿����б����Ϊ�գ���ʾ���õ�ǰ���õ�ȫ�����߿�
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetReaderRecXmlByFrom(
            RmsChannelCollection channels,
            string strReaderDbNames,
            string strWord,
            string strFrom,
            out string strXml,
            out string strOutputPath,
            out byte[] timestamp,
            out string strError)
        {
            strOutputPath = "";
            timestamp = null;
            strXml = "";
            strError = "";

            LibraryApplication app = this;

            List<string> dbnames = new List<string>();
            if (string.IsNullOrEmpty(strReaderDbNames) == true)
            {
                for (int i = 0; i < app.ReaderDbs.Count; i++)
                {
                    string strDbName = app.ReaderDbs[i].DbName;

                    if (string.IsNullOrEmpty(strDbName) == true)
                    {
                        Debug.Assert(false, "");
                        continue;
                    }

                    dbnames.Add(strDbName);
                }

                if (dbnames.Count == 0)
                {
                    strError = "��ǰ��û�����ö��߿�";
                    return -1;
                }
            }
            else
            {
                dbnames = StringUtil.SplitList(strReaderDbNames);
                StringUtil.RemoveBlank(ref dbnames);

                if (dbnames.Count == 0)
                {
                    strError = "����strReaderDbNamesֵ '" + strReaderDbNames + "' ��û�а�����Ч�Ķ��߿���";
                    return -1;
                }
            }

            // �������ʽ
            string strQueryXml = "";
            for (int i = 0; i < dbnames.Count; i++)
            {
                string strDbName = dbnames[i];

                Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "");

                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strWord)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>1000</maxCount></item><lang>zh</lang></target>";

                if (i > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
            }

            if (dbnames.Count > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
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
                strError = "����"+strFrom+" '" + strWord + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            List<string> aPath = null;
            lRet = channel.DoGetSearchResult(
                "default",
                0,
                1,
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

            string strMetaData = "";

            lRet = channel.GetRes(aPath[0],
                out strXml,
                out strMetaData,
                out timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            return (int)lHitCount;
        ERROR1:
            return -1;
        }
#endif

        // 2013/5/21
        // ��װ��İ汾
        public int GetReaderRecXmlByFrom(
            RmsChannelCollection channels,
            string strReaderDbNames,
            string strWord,
            string strFrom,
            out string strXml,
            out string strOutputPath,
            out byte[] timestamp,
            out string strError)
        {
            strOutputPath = "";
            List<string> recpaths = null;
            int nRet = GetReaderRecXmlByFrom(
                channels,
                strReaderDbNames,
                strWord,
                strFrom,
                1,
                "",
                out recpaths,
                out strXml,
                out timestamp,
                out strError);
            if (recpaths != null && recpaths.Count > 0)
                strOutputPath = recpaths[0];

            return nRet;
        }

        // ��ö��߿����б�
        // parameters:
        //      strReaderDbNames    �����б��ַ��������Ϊ�գ����ʾȫ�����߿�
        int GetDbNameList(string strReaderDbNames,
            string strLibraryCodeList,
            out List<string> dbnames,
            out string strError)
        {
            strError = "";

            dbnames = new List<string>();
            if (string.IsNullOrEmpty(strReaderDbNames) == true)
            {
                for (int i = 0; i < this.ReaderDbs.Count; i++)
                {
                    string strDbName = this.ReaderDbs[i].DbName;

                    if (string.IsNullOrEmpty(strDbName) == true)
                    {
                        Debug.Assert(false, "");
                        continue;
                    }

                    dbnames.Add(strDbName);
                }

                if (dbnames.Count == 0)
                {
                    strError = "��ǰ��û�����ö��߿�";
                    return -1;
                }
            }
            else
            {
                dbnames = StringUtil.SplitList(strReaderDbNames);
                StringUtil.RemoveBlank(ref dbnames);

                if (dbnames.Count == 0)
                {
                    strError = "���� strReaderDbNames ֵ '" + strReaderDbNames + "' ��û�а�����Ч�Ķ��߿���";
                    return -1;
                }
            }

            // ����
            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
            {
                List<string> results = new List<string>();
                foreach (string s in dbnames)
                {
                    if (IsCurrentChangeableReaderPath(s + "/?", strLibraryCodeList) == false)
                        continue;
                    results.Add(s);
                }
                dbnames = results;
            }

            return 0;
        }

        // 2012/1/6 ����ΪPiggyBack����
        // TODO: �ж�strWord�Ƿ�Ϊ��
        // ͨ���ض�����;����ö��߼�¼
        // parameters:
        //      strReaderDbNames    ���߿����б����Ϊ�գ���ʾ���õ�ǰ���õ�ȫ�����߿�
        //      nMax                ϣ���� recpaths ����෵�ض��ٸ���¼·��
        //      strLibraryCodeList  �ݴ����б���������������б��Ͻ�Ķ��߿�ļ�¼��·�������Ϊ�գ���ʾ������
        //      recpaths        [out]�������еļ�¼·������������ظ�������᷵�ض���һ��·��
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetReaderRecXmlByFrom(
            RmsChannelCollection channels,
            string strReaderDbNames,
            string strWord,
            string strFrom,
            int nMax,
            string strLibraryCodeList,
            out List<string> recpaths,
            out string strXml,
            // out string strOutputPath,
            out byte[] timestamp,
            out string strError)
        {
            // strOutputPath = "";
            timestamp = null;
            strXml = "";
            strError = "";
            int nRet = 0;

            recpaths = new List<string>();

            LibraryApplication app = this;
#if NO
            List<string> dbnames = new List<string>();
            if (string.IsNullOrEmpty(strReaderDbNames) == true)
            {
                for (int i = 0; i < app.ReaderDbs.Count; i++)
                {
                    string strDbName = app.ReaderDbs[i].DbName;

                    if (string.IsNullOrEmpty(strDbName) == true)
                    {
                        Debug.Assert(false, "");
                        continue;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false
                        && IsCurrentChangeableReaderPath(strDbName + "/?",
                            strLibraryCodeList) == false)
                        continue;

                    dbnames.Add(strDbName);
                }

                if (dbnames.Count == 0)
                {
                    strError = "��ǰ��û�����ö��߿�";
                    return -1;
                }
            }
            else
            {
                dbnames = StringUtil.SplitList(strReaderDbNames);
                StringUtil.RemoveBlank(ref dbnames);

                if (dbnames.Count == 0)
                {
                    strError = "����strReaderDbNamesֵ '" + strReaderDbNames + "' ��û�а�����Ч�Ķ��߿���";
                    return -1;
                }
            }
#endif
            List<string> dbnames = new List<string>();
            // ��ö��߿����б�
            // parameters:
            //      strReaderDbNames    �����б��ַ��������Ϊ�գ����ʾȫ�����߿�
            nRet = GetDbNameList(strReaderDbNames,
                strLibraryCodeList,
                out dbnames,
                out strError);
            if (nRet == -1)
                return -1;

            if (dbnames.Count == 0)
            {
                if (app.ReaderDbs.Count == 0)
                    strError = "��ǰ��û�����ö��߿�";
                else
                    strError = "��ǰû�п��Բ����Ķ��߿�";
                return -1;
            }

            // �������ʽ
            string strQueryXml = "";
            for (int i = 0; i < dbnames.Count; i++)
            {
                string strDbName = dbnames[i];

                Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "");

                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strWord)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>1000</maxCount></item><lang>zh</lang></target>";

                if (i > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
            }

            if (dbnames.Count > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            Record[] records = null;
            long lRet = channel.DoSearchEx(strQueryXml,
                "default",
                "", // strOuputStyle
                1,
                "zh",
                "id,xml,timestamp",
                out records,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = "����" + strFrom + " '" + strWord + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            if (records == null || records.Length == 0)
            {
                strError = "records error";
                return -1;
            }

            Debug.Assert(records[0].RecordBody != null, "");

            // strOutputPath = records[0].Path;
            if (nMax >= 1)
                recpaths.Add(records[0].Path);
            strXml = records[0].RecordBody.Xml;
            timestamp = records[0].RecordBody.Timestamp;

            // ������н������һ�����������õ�һ���Ժ�ĸ�����path
            if (lHitCount > 1 && nMax > 1)
            {
                // List<string> temp = null;
                lRet = channel.DoGetSearchResult(
                    "default",
                    0,
                    Math.Min(nMax, lHitCount),
                    "zh",
                    null,
                    out recpaths,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                Debug.Assert(recpaths != null, "");

                if (recpaths.Count == 0)
                {
                    strError = "DoGetSearchResult aPath error";
                    goto ERROR1;
                }
            }

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

        /*
        // ����ʹ�õ� ����֤����Ų��ؽ������
        List<string> m_searchReaderDupResultsetNames = new List<string>();
         * 
        // ���һ����δʹ�õ� ����֤����Ų��ؽ������
        string GetSearchReaderDupResultsetName()
        {
            lock (this.m_searchReaderDupResultsetNames)
            {
                for (int i = 0; ; i++)
                {
                    string strResultSetName = "search_reader_dup_" + i.ToString();

                    int index = this.m_searchReaderDupResultsetNames.IndexOf(strResultSetName);
                    if (index == -1)
                    {
                        this.m_searchReaderDupResultsetNames.Add(strResultSetName);
                        return strResultSetName;
                    }
                }
            }
        }

        // �ͷ�һ�� ����֤����Ų��ؽ���� ��
        void ReleaseSearchReaderDupResultsetName(string strResultSetName)
        {
            lock (this.m_searchReaderDupResultsetNames)
            {
                this.m_searchReaderDupResultsetNames.Remove(strResultSetName);
            }
        }
         * */

        // ���ݶ���֤����ŶԶ��߿���в���
        // ������ֻ�������, ������ü�¼��
        // parameters:
        //      strBarcode  ����֤�����
        // return:
        //      -1  error
        //      ����    ���м�¼����(������nMax�涨�ļ���)
        public int SearchReaderRecDup(
            RmsChannelCollection channels,
            string strBarcode,
            int nMax,
            out List<string> aPath,
            out string strError)
        {
            strError = "";
            aPath = null;

            Debug.Assert(String.IsNullOrEmpty(strBarcode) == false, "");

            LibraryApplication app = this;

            // �������ʽ
            // ����Ҫ���ȫ�����߿���У��������ǵ�ǰ�û��ܹ�Ͻ�Ŀ�
            string strQueryXml = "";
            int nCount = 0;
            for (int i = 0; i < app.ReaderDbs.Count; i++)
            {
                string strDbName = app.ReaderDbs[i].DbName;

                Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "");

                if (nCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                // 2007/4/5 ���� ������ GetXmlStringSimple()
                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + "֤����")       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strBarcode)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>"+nMax.ToString()+"</maxCount></item><lang>zh</lang></target>";
                nCount++;

                strQueryXml += strOneDbQuery;
            }

            if (nCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            string strResultSetName = "search_reader_dup_001";

            long lRet = channel.DoSearch(strQueryXml,
                strResultSetName,
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = "����֤����� '" + strBarcode + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            lRet = channel.DoGetSearchResult(
                strResultSetName,
                0,
                nMax,
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;


            if (aPath.Count == 0)
            {
                strError = "DoGetSearchResult aPath error ��ǰ���Ѿ����е�����ì��";
                goto ERROR1;
            }

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

        // TODO: �ж�strDisplayName�Ƿ�Ϊ��
        // ������ʾ���Զ��߿���в���
        // ������ֻ�������, ������ü�¼��
        // parameters:
        //      strBarcode  ����֤�����
        // return:
        //      -1  error
        //      ����    ���м�¼����(������nMax�涨�ļ���)
        public int SearchReaderDisplayNameDup(
            RmsChannelCollection channels,
            string strDisplayName,
            int nMax,
            out List<string> aPath,
            out string strError)
        {
            strError = "";
            aPath = null;

            LibraryApplication app = this;

            Debug.Assert(String.IsNullOrEmpty(strDisplayName) == false, "");

            // �������ʽ
            // ����Ҫ���ȫ�����߿����
            string strQueryXml = "";
            int nCount = 0;
            for (int i = 0; i < app.ReaderDbs.Count; i++)
            {
                string strDbName = app.ReaderDbs[i].DbName;

                Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "");

                if (nCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                string strOneDbQuery = "<target list='"
        + StringUtil.GetXmlStringSimple(strDbName + ":" + "��ʾ��")
        + "'><item><word>"
        + StringUtil.GetXmlStringSimple(strDisplayName)
        + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>"+nMax.ToString()+"</maxCount></item><lang>zh</lang></target>";
                nCount++;

                strQueryXml += strOneDbQuery;
            }

            if (nCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            string strResultSetName = "search_reader_dup_001";

            // TODO: ���ּ�����Ľ����������ظ��ɣ���Ҫ������֤

            long lRet = channel.DoSearch(strQueryXml,
                strResultSetName,
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = "��ʾ�� '" + strDisplayName + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            lRet = channel.DoGetSearchResult(
                strResultSetName,
                0,
                nMax,
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;


            if (aPath.Count == 0)
            {
                strError = "DoGetSearchResult aPath error ��ǰ���Ѿ����е�����ì��";
                goto ERROR1;
            }

            return (int)lHitCount;
        ERROR1:
            return -1;
        }


        // ���ݶ���֤״̬�Զ��߿���м���
        // parameters:
        //      strMatchStyle   ƥ�䷽ʽ left exact right middle
        //      strState  ����֤״̬
        //      bOnlyIncirculation  �Ƿ��������������ͨ�����ݿ�? true ������������ false : ����ȫ��
        //      bGetPath    == true ���path; == false ���barcode
        // return:
        //      -1  error
        //      ����    ���м�¼����(������nMax�涨�ļ���)
        public int SearchReaderState(
            RmsChannelCollection channels,
            string strState,
            string strMatchStyle,
            bool bOnlyIncirculation,
            bool bGetPath,
            int nMax,
            out List<string> aPathOrBarcode,
            out string strError)
        {
            strError = "";
            aPathOrBarcode = null;

            LibraryApplication app = this;

            // �������ʽ
            string strQueryXml = "";
            int nDbCount = 0;
            for (int i = 0; i < app.ReaderDbs.Count; i++)
            {
                string strDbName = app.ReaderDbs[i].DbName;

                if (bOnlyIncirculation == true)
                {
                    if (app.ReaderDbs[i].InCirculation == false)
                        continue;
                }

                Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "");

                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + "״̬")
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strState)
                    + "</word><match>"+strMatchStyle+"</match><relation>=</relation><dataType>string</dataType><maxCount>"+nMax.ToString()+"</maxCount></item><lang>zh</lang></target>";

                if (nDbCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
                nDbCount++;
            }

            if (nDbCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }
            else
            {
                strError = "Ŀǰ��û�в�����ͨ�Ķ��߿�";
                return -1;
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            string strResultSetName = "search_reader_state_001";


            long lRet = channel.DoSearch(strQueryXml,
                strResultSetName,
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = "����֤״̬ '" + strState + "' (ƥ�䷽ʽ: "+strMatchStyle+") û������";
                return 0;
            }

            long lHitCount = lRet;

            if (bGetPath == true)
            {
                lRet = channel.DoGetSearchResult(
                    strResultSetName,
                    0,
                    nMax,
                    "zh",
                    null,
                    out aPathOrBarcode,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
            }
            else
            {
                // ��ȡ�������н��
                // ���ĳһ����Ϣ�İ汾
                lRet = channel.DoGetSearchResultOneColumn(
                    strResultSetName,
                    0,
                    nMax,
                    "zh",
                    null,
                    0,  // nColumn,
                    out aPathOrBarcode,
                    out strError);
            }

            if (aPathOrBarcode.Count == 0)
            {
                strError = "DoGetSearchResult aPath error ��ǰ���Ѿ����е�����ì��";
                goto ERROR1;
            }

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

#if NO
        // ��ò��¼(��װ��İ汾)
        // ������Ϊ��ִ��Ч�ʷ����ԭ��, ��ȥ��ó���1�����ϵ�·��
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��(���������������, strOutputPathҲ�����˵�һ����·��)
        public int GetItemRecXml(
            RmsChannelCollection channels,
            string strBarcode,
            out string strXml,
            out string strOutputPath,
            out string strError)
        {
            byte [] timestamp = null;

            RmsChannel channel = channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            return GetItemRecXml(
                channel,
                strBarcode,
                out strXml,
                out strOutputPath,
                out timestamp,
                out strError);
        }
#endif

        public int GetItemRecXml(
            RmsChannel channel,
    string strBarcode,
    out string strXml,
    out string strOutputPath,
    out string strError)
        {
            byte[] timestamp = null;

            return GetItemRecXml(
                channel,
                strBarcode,
                out strXml,
                out strOutputPath,
                out timestamp,
                out strError);
        }


#if SLOWLY
        // TODO: �ж�strBarcode�Ƿ�Ϊ��
        // ��ò��¼
        // ������Ϊ��ִ��Ч�ʷ����ԭ��, ��ȥ��ó���1�����ϵ�·���������ص��ظ��������Ϊ1000
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��(���������������, strOutputPathҲ�����˵�һ����·��)
        public int GetItemRecXml(
            RmsChannelCollection channels,
            string strBarcode,
            out string strXml,
            out string strOutputPath,
            out byte[] timestamp,
            out string strError)
        {
            strOutputPath = "";
            strXml = "";
            strError = "";
            timestamp = null;

            LibraryApplication app = this;

            // �������ʽ
            string strQueryXml = "";
            int nDbCount = 0;
            for (int i = 0; i < app.ItemDbs.Count; i++)
            {
                string strDbName = app.ItemDbs[i].DbName;

                // 2008/10/16 
                if (String.IsNullOrEmpty(strDbName) == true)
                    continue;

                // 2007/4/5 ���� ������ GetXmlStringSimple()
                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + "������")       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strBarcode) + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>1000</maxCount></item><lang>zh</lang></target>";
                // 1000 2011/9/5

                if (nDbCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
                nDbCount++;
            }

            if (nDbCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
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
                strError = "������� '" + strBarcode + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            List<string> aPath = null;
            lRet = channel.DoGetSearchResult(
                "default",
                0,
                1,
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

            string strMetaData = "";
            // byte[] timestamp = null;
            // string strOutputPath = "";

            lRet = channel.GetRes(aPath[0],
                out strXml,
                out strMetaData,
                out timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            return (int)lHitCount;
        ERROR1:
            return -1;
        }
#endif 

        // 2014/9/19 strBarcode ���԰��� @refID: ǰ׺��
        // 2012/1/5 ����ΪPiggyBack����
        // TODO: �ж�strBarcode�Ƿ�Ϊ��
        // ��ò��¼
        // ������Ϊ��ִ��Ч�ʷ����ԭ��, ��ȥ��ó���1�����ϵ�·���������ص��ظ��������Ϊ1000
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��(���������������, strOutputPathҲ�����˵�һ����·��)
        public int GetItemRecXml(
            RmsChannel channel,
            string strBarcodeParam,
            out string strXml,
            out string strOutputPath,
            out byte[] timestamp,
            out string strError)
        {
            strOutputPath = "";
            strXml = "";
            strError = "";
            timestamp = null;

            LibraryApplication app = this;

            string strBarcode = strBarcodeParam;
            string strHead = "@refID:";

            string strFrom = "������";
            if (StringUtil.HasHead(strBarcode, strHead, true) == true)
            {
                strFrom = "�ο�ID";
                strBarcode = strBarcode.Substring(strHead.Length).Trim();
                if (string.IsNullOrEmpty(strBarcode) == true)
                {
                    strError = "�ַ��� '" + strBarcodeParam + "' �� �ο�ID ���ֲ�ӦΪ��";
                    return -1;
                }
            }

            // �������ʽ
            string strQueryXml = "";
            int nDbCount = 0;
            for (int i = 0; i < app.ItemDbs.Count; i++)
            {
                string strDbName = app.ItemDbs[i].DbName;

                // 2008/10/16 
                if (String.IsNullOrEmpty(strDbName) == true)
                    continue;

                // 2007/4/5 ���� ������ GetXmlStringSimple()
                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strBarcode) + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>1000</maxCount></item><lang>zh</lang></target>";
                // 1000 2011/9/5

                if (nDbCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
                nDbCount++;
            }

            if (nDbCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }


            Record[] records = null;
            long lRet = channel.DoSearchEx(strQueryXml,
                "default",
                "", // strOuputStyle
                1,
                "zh",
                "id,xml,timestamp",
                out records,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = "������� '" + strBarcode + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            if (records == null || records.Length == 0)
            {
                strError = "records error";
                return -1;
            }

            Debug.Assert(records[0].RecordBody != null, "");

            strOutputPath = records[0].Path;
            strXml = records[0].RecordBody.Xml;
            timestamp = records[0].RecordBody.Timestamp;

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

#if SLOWLY
        // TODO: �ж�strBarcode�Ƿ�Ϊ��
        // ��ò��¼
        // �������ɻ�ó���1�����ϵ�·��
        // parameters:
        //      strBarcode  ������š�Ҳ����Ϊ "@refID:ֵ" ��̬
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetItemRecXml(
            RmsChannelCollection channels,
            string strBarcode,
            out string strXml,
            int nMax,
            out List<string> aPath,
            out byte[] timestamp,
            out string strError)
        {
            aPath = null;

            strXml = "";
            strError = "";
            timestamp = null;

            LibraryApplication app = this;

            string strHead = "@refID:";

            string strFrom = "������";
            if (StringUtil.HasHead(strBarcode, strHead) == true)
            {
                strFrom = "�ο�ID";
                strBarcode = strBarcode.Substring(strHead.Length);
            }

            // �������ʽ
            string strQueryXml = "";
            int nDbCount = 0;
            for (int i = 0; i < app.ItemDbs.Count; i++)
            {
                string strDbName = app.ItemDbs[i].DbName;

                // 2008/10/16 
                if (String.IsNullOrEmpty(strDbName) == true)
                    continue;

                // 2007/4/5 ���� ������ GetXmlStringSimple()
                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strBarcode)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>"+nMax.ToString()+"</maxCount></item><lang>zh</lang></target>";

                if (nDbCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
                nDbCount++;
            }

            if (nDbCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
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
                strError = strFrom + " '" + strBarcode + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            // List<string> aPath = null;
            lRet = channel.DoGetSearchResult(
                "default",
                0,
                Math.Min(nMax, lHitCount),
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            Debug.Assert(aPath != null, "");

            if (aPath.Count == 0)
            {
                strError = "DoGetSearchResult aPath error";
                goto ERROR1;
            }

            string strMetaData = "";
            // byte[] timestamp = null;
            string strOutputPath = "";

            lRet = channel.GetRes(aPath[0],
                out strXml,
                out strMetaData,
                out timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

#endif

        // ��װ��İ汾
        public int GetItemRecXml(
    RmsChannelCollection channels,
    string strBarcode,
    out string strXml,
    int nMax,
    out List<string> aPath,
    out byte[] timestamp,
    out string strError)
        {
            return GetItemRecXml(channels,
                strBarcode,
                "",
                out strXml,
                nMax,
                out aPath,
                out timestamp,
                out strError);
        }

        // ������ǰ�İ汾����װ�����̬
        // ��ò��¼
        // �������ɻ�ó���1�����ϵ�·��
        // parameters:
        //      strBarcode  ������š�Ҳ����Ϊ "@refID:ֵ" ��̬
        //      strStyle    ������� withresmetadata ,��ʾҪ��XML��¼�з���<dprms:file>Ԫ���ڵ� __xxx ���� 2012/11/19
        //                  ������� noxml�� ���ʾ������ XML ��¼��
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetItemRecXml(
            RmsChannelCollection channels,
            string strBarcodeParam,
            string strStyle,
            out string strXml,
            int nMax,
            out List<string> aPath,
            out byte[] timestamp,
            out string strError)
        {
            string strBarcode = strBarcodeParam;
            string strHead = "@refID:";

            string strFrom = "������";
            if (StringUtil.HasHead(strBarcode, strHead, true) == true)
            {
                strFrom = "�ο�ID";
                strBarcode = strBarcode.Substring(strHead.Length).Trim();
                if (string.IsNullOrEmpty(strBarcode) == true)
                {
                    strError = "�ַ��� '" + strBarcodeParam + "' �� �ο�ID ���ֲ�ӦΪ��";
                    aPath = new List<string>();
                    timestamp = null;
                    strXml = "";
                    return -1;
                }
            }

            return GetOneItemRec(
                channels,
                "item",
                strBarcode,
                strFrom,
                strStyle + ",xml,timestamp",
                out strXml,
                nMax,
                out aPath,
                out timestamp,
                out strError);
        }

#if NO
        // ����ͨ�õİ汾 GetOneItemRec() ��� 
        // 2012/11/27����Ϊ���Բ����XML��ʱ���
        // 2012/1/5����ΪPiggyBack����
        // TODO: �ж�strBarcode�Ƿ�Ϊ��
        // ��ò��¼
        // �������ɻ�ó���1�����ϵ�·��
        // parameters:
        //      strBarcode  ������š�Ҳ����Ϊ "@refID:ֵ" ��̬
        //      strStyle    ������� withresmetadata ,��ʾҪ��XML��¼�з���<dprms:file>Ԫ���ڵ� __xxx ���� 2012/11/19
        //                  ������� xml�� ���ʾ���� XML ��¼��
        //                  ������� timestamp, ���ʾ����ʱ���
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetItemRec(
            RmsChannelCollection channels,
            string strBarcode,
            string strStyle,
            out string strXml,
            int nMax,
            out List<string> aPath,
            out byte[] timestamp,
            out string strError)
        {
            aPath = null;

            strXml = "";
            strError = "";
            timestamp = null;

            LibraryApplication app = this;

            string strHead = "@refID:";

            string strFrom = "������";
            if (StringUtil.HasHead(strBarcode, strHead) == true)
            {
                strFrom = "�ο�ID";
                strBarcode = strBarcode.Substring(strHead.Length);
            }

            // �������ʽ
            string strQueryXml = "";
            int nDbCount = 0;
            for (int i = 0; i < app.ItemDbs.Count; i++)
            {
                string strDbName = app.ItemDbs[i].DbName;

                if (String.IsNullOrEmpty(strDbName) == true)
                    continue;

                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strBarcode)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>" + nMax.ToString() + "</maxCount></item><lang>zh</lang></target>";

                if (nDbCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
                nDbCount++;
            }

            if (nDbCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            /*
            string strGetStyle = "id,xml,timestamp";

            if (StringUtil.IsInList("withresmetadata", strStyle) == true)
                strGetStyle += ",withresmetadata";
            */

            Record[] records = null;
            long lRet = channel.DoSearchEx(strQueryXml,
                "default",
                "", // strOuputStyle
                1,
                "zh",
                strStyle + ",id",    // "id,xml,timestamp",
                out records,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = strFrom + " '" + strBarcode + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            if (records == null || records.Length == 0)
            {
                strError = "records error";
                return -1;
            }

#if DEBUG
            if (StringUtil.IsInList("xml", strStyle) == true
                || StringUtil.IsInList("timestamp", strStyle) == true)
            {
                Debug.Assert(records[0].RecordBody != null, "");
            }
#endif

            aPath = new List<string>();
            aPath.Add(records[0].Path);
            if (records[0].RecordBody != null)
            {
                strXml = records[0].RecordBody.Xml;
                timestamp = records[0].RecordBody.Timestamp;
            }

            // ������н������һ�����������õ�һ���Ժ�ĸ�����path
            if (lHitCount > 1)
            {
                // List<string> aPath = null;
                lRet = channel.DoGetSearchResult(
                    "default",
                    0,
                    Math.Min(nMax, lHitCount),
                    "zh",
                    null,
                    out aPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                Debug.Assert(aPath != null, "");

                if (aPath.Count == 0)
                {
                    strError = "DoGetSearchResult aPath error";
                    goto ERROR1;
                }
            }

            return (int)lHitCount;
        ERROR1:
            return -1;
        }
#endif

        // ����һ�����¼������ע\����\�ڼ�¼
        // �������ɻ�ó���1�����ϵ�·��
        // parameters:
        //      strBarcode  ������š�Ҳ����Ϊ "@refID:ֵ" ��̬
        //      strStyle    ������� withresmetadata ,��ʾҪ��XML��¼�з���<dprms:file>Ԫ���ڵ� __xxx ���� 2012/11/19
        //                  ������� xml�� ���ʾ���� XML ��¼��
        //                  ������� timestamp, ���ʾ����ʱ���
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetOneItemRec(
            RmsChannelCollection channels,
            string strDbType,
            string strBarcode,
            string strFrom,
            string strStyle,
            out string strXml,
            int nMax,
            out List<string> aPath,
            out byte[] timestamp,
            out string strError)
        {
            aPath = null;

            strXml = "";
            strError = "";
            timestamp = null;

            LibraryApplication app = this;

            List<string> dbnames = null;
            int nRet = app.GetDbNames(
    strDbType,
    out dbnames,
    out strError);
            if (nRet == -1)
                return -1;

            // �������ʽ
            string strQueryXml = "";
            int nDbCount = 0;
            for (int i = 0; i < dbnames.Count; i++)
            {
                string strDbName = dbnames[i];

                if (String.IsNullOrEmpty(strDbName) == true)
                    continue;

                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strBarcode)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>" + nMax.ToString() + "</maxCount></item><lang>zh</lang></target>";

                if (nDbCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
                nDbCount++;
            }

            if (nDbCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            Record[] records = null;
            long lRet = channel.DoSearchEx(strQueryXml,
                "default",
                "", // strOuputStyle
                1,
                "zh",
                strStyle + ",id",    // "id,xml,timestamp",
                out records,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = strFrom + " '" + strBarcode + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            if (records == null || records.Length == 0)
            {
                strError = "records error";
                return -1;
            }

#if DEBUG
            if (StringUtil.IsInList("xml", strStyle) == true
                || StringUtil.IsInList("timestamp", strStyle) == true)
            {
                Debug.Assert(records[0].RecordBody != null, "");
            }
#endif

            aPath = new List<string>();
            aPath.Add(records[0].Path);
            if (records[0].RecordBody != null)
            {
                strXml = records[0].RecordBody.Xml;
                timestamp = records[0].RecordBody.Timestamp;
            }

            // ������н������һ�����������õ�һ���Ժ�ĸ�����path
            if (lHitCount > 1)  // TODO: && nMax > 1
            {
                // List<string> aPath = null;
                lRet = channel.DoGetSearchResult(
                    "default",
                    0,
                    Math.Min(nMax, lHitCount),
                    "zh",
                    null,
                    out aPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                Debug.Assert(aPath != null, "");

                if (aPath.Count == 0)
                {
                    strError = "DoGetSearchResult aPath error";
                    goto ERROR1;
                }
            }

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

        // ������ݿ����͵���������
        public static string GetDbTypeName(string strDbType)
        {
            if (strDbType == "biblio")
            {
                return "��Ŀ";
            }
            else if (strDbType == "reader")
            {
                return "����";
            }
            else if (strDbType == "item")
            {
                return "ʵ��";
            }
            else if (strDbType == "issue")
            {
                return "��";
            }
            else if (strDbType == "order")
            {
                return "����";
            }
            else if (strDbType == "comment")
            {
                return "��ע";
            }
            else if (strDbType == "invoice")
            {
                return "��Ʊ";
            }
            else if (strDbType == "amerce")
            {
                return "ΥԼ��";
            }
            else
            {
                return null;
            }
        }

        // �����ض����ݿ����ͣ��г��������ݿ���
        // ���������߿�
        public int GetDbNames(
            string strDbType,
            out List<string> dbnames,
            out string strError)
        {
            strError = "";
            dbnames = new List<string>();

            if (strDbType == "biblio")
            {
                for (int i = 0; i < this.ItemDbs.Count; i++)
                {
                    // ʵ����Ӧ����Ŀ����
                    string strBiblioDbName = this.ItemDbs[i].BiblioDbName;

                    if (String.IsNullOrEmpty(strBiblioDbName) == false)
                        dbnames.Add(strBiblioDbName);
                }
            }
            else if (strDbType == "item")
            {
                for (int i = 0; i < this.ItemDbs.Count; i++)
                {
                    // ʵ�����
                    string strItemDbName = this.ItemDbs[i].DbName;

                    if (String.IsNullOrEmpty(strItemDbName) == false)
                        dbnames.Add(strItemDbName);
                }
            }
            else if (strDbType == "issue")
            {
                for (int i = 0; i < this.ItemDbs.Count; i++)
                {
                    // �ڿ���
                    string strIssueDbName = this.ItemDbs[i].IssueDbName;

                    if (String.IsNullOrEmpty(strIssueDbName) == false)
                        dbnames.Add(strIssueDbName);
                }
            }
            else if (strDbType == "order")
            {
                for (int i = 0; i < this.ItemDbs.Count; i++)
                {
                    // ��������
                    string strOrderDbName = this.ItemDbs[i].OrderDbName;

                    if (String.IsNullOrEmpty(strOrderDbName) == false)
                        dbnames.Add(strOrderDbName);
                }
            }
            else if (strDbType == "comment")
            {
                for (int i = 0; i < this.ItemDbs.Count; i++)
                {
                    // ʵ�����
                    string strCommentDbName = this.ItemDbs[i].CommentDbName;

                    if (String.IsNullOrEmpty(strCommentDbName) == false)
                        dbnames.Add(strCommentDbName);
                }
            }
            else if (strDbType == "invoice")
            {
                if (string.IsNullOrEmpty(this.InvoiceDbName) == false)
                    dbnames.Add(this.InvoiceDbName);
            }
            else if (strDbType == "amerce")
            {
                if (string.IsNullOrEmpty(this.AmerceDbName) == false)
                    dbnames.Add(this.AmerceDbName);
            }
            else if (strDbType == "arrived")
            {
                if (string.IsNullOrEmpty(this.ArrivedDbName) == false)
                    dbnames.Add(this.ArrivedDbName);
            }
            else
            {
                strError = "δ֪�����ݿ����� '" + strDbType + "'��ӦΪbiblio reader item issue order comment invoice amerce arrived֮һ";
                return -1;
            }

            return 0;
        }

        // ���ȷ�� kdbs != null
        public string EnsureKdbs(bool bThrowException = true)
        {
            if (this.kdbs == null)
            {
                this.ActivateManagerThreadForLoad();
                string strError = "app.kdbs == null������ԭ������dp2Library��־�����Ժ����Բ���";
                if (bThrowException == true)
                    throw new Exception(strError);

                return strError;
            }

            return null;    // û�г���
        }

        // �г�ĳ�����ݿ�ļ���;����Ϣ
        // parameters:
        //          strLibraryCodeList  ��ǰ�û���Ͻ�Ĺݴ����б�����������г���Щ���߿�ļ���;�����������ʱ strDbType ���� "reader"����������Ϊ�ռ��� 
        // return:
        //      -1  ����
        //      0   û�ж���
        //      1   �ɹ�
        public int ListDbFroms(string strDbType,
            string strLang,
            string strLibraryCodeList,
            out BiblioDbFromInfo[] infos,
            out string strError)
        {
            infos = null;
            strError = "";

            strError = EnsureKdbs(false);
            if (strError != null)
                goto ERROR1;

            if (string.IsNullOrEmpty(strDbType) == true)
                strDbType = "biblio";

            // long lRet = 0;

            List<string> dbnames = null;
            if (strDbType == "reader")
            {
                dbnames = this.GetCurrentReaderDbNameList(strLibraryCodeList);    // sessioninfo.LibraryCodeList
            }
            else
            {
                int nRet = this.GetDbNames(
                    strDbType,
                    out dbnames,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

#if NO
                List<string> dbnames = new List<string>();

                string strDbTypeName = "";

                if (strDbType == "biblio")
                {
                    strDbTypeName = "��Ŀ";
                    for (int i = 0; i < app.ItemDbs.Count; i++)
                    {
                        // ʵ����Ӧ����Ŀ����
                        string strBiblioDbName = app.ItemDbs[i].BiblioDbName;

                        if (String.IsNullOrEmpty(strBiblioDbName) == false)
                            dbnames.Add(strBiblioDbName);
                    }
                }
                else if (strDbType == "reader")
                {
                    strDbTypeName = "����";
                    dbnames = app.GetCurrentReaderDbNameList(sessioninfo.LibraryCodeList);
                }
                else if (strDbType == "item")   // 2012/5/5
                {
                    strDbTypeName = "ʵ��";
                    for (int i = 0; i < app.ItemDbs.Count; i++)
                    {
                        // ʵ�����
                        string strItemDbName = app.ItemDbs[i].DbName;

                        if (String.IsNullOrEmpty(strItemDbName) == false)
                            dbnames.Add(strItemDbName);
                    }
                }
                else if (strDbType == "issue")   // 2012/5/5
                {
                    strDbTypeName = "��";
                    for (int i = 0; i < app.ItemDbs.Count; i++)
                    {
                        // �ڿ���
                        string strIssueDbName = app.ItemDbs[i].IssueDbName;

                        if (String.IsNullOrEmpty(strIssueDbName) == false)
                            dbnames.Add(strIssueDbName);
                    }
                }
                else if (strDbType == "order")   // 2012/5/5
                {
                    strDbTypeName = "����";
                    for (int i = 0; i < app.ItemDbs.Count; i++)
                    {
                        // ��������
                        string strOrderDbName = app.ItemDbs[i].OrderDbName;

                        if (String.IsNullOrEmpty(strOrderDbName) == false)
                            dbnames.Add(strOrderDbName);
                    }
                }
                else if (strDbType == "comment")   // 2012/5/5
                {
                    strDbTypeName = "��ע";
                    for (int i = 0; i < app.ItemDbs.Count; i++)
                    {
                        // ʵ�����
                        string strCommentDbName = app.ItemDbs[i].CommentDbName;

                        if (String.IsNullOrEmpty(strCommentDbName) == false)
                            dbnames.Add(strCommentDbName);
                    }
                }
                else if (strDbType == "invoice")
                {
                    strDbTypeName = "��Ʊ";
                    if (string.IsNullOrEmpty(app.InvoiceDbName) == false)
                        dbnames.Add(app.InvoiceDbName);
                }
                else if (strDbType == "amerce")
                {
                    strDbTypeName = "ΥԼ��";
                    if (string.IsNullOrEmpty(app.AmerceDbName) == false)
                        dbnames.Add(app.AmerceDbName);
                }
                else
                {
                    strError = "δ֪�����ݿ����� '"+strDbType+"'��ӦΪbiblio reader item issue order comment invoice amerce֮һ";
                    goto ERROR1;
                }
#endif

            StringUtil.RemoveDupNoSort(ref dbnames);

            if (dbnames.Count == 0)
            {
                strError = "��ǰϵͳ��û�ж���������ݿ⣬�����޷���֪�����;����Ϣ";
                return 0;
            }

            // ���Ե�ʱ���г��������洢?
            // ���洢��ȱ���ǣ��ȵ���������ʽ��ʱ�򣬾Ͳ�֪���ĸ�������Щstyleֵ�ˡ�
            // ����һ����caption�������г�������styleֵ��ҪԤ�ȳ�ʼ���ʹ洢������������ʱ�������ʽ��
            List<From> froms = new List<From>();

            for (int i = 0; i < dbnames.Count; i++)
            {
                string strDbName = dbnames[i];

                if (String.IsNullOrEmpty(strDbName) == true)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                /*
                // 2011/12/17
                if (app.kdbs == null)
                {
                    app.ActivateManagerThreadForLoad();
                    strError = "app.kdbs == null������ԭ������dp2Library��־";
                    goto ERROR1;
                }
                 * */

                KernelDbInfo db = this.kdbs.FindDb(strDbName);

                if (db == null)
                {
                    strError = "kdbs��û�й���" + LibraryApplication.GetDbTypeName(strDbType) + "���ݿ� '" + strDbName + "' ����Ϣ";
                    goto ERROR1;
                }

                // �����п��from�ۼ�����
                froms.AddRange(db.Froms);
            }

            // ����styleֵȥ��
            if (dbnames.Count > 1)
            {
                if (strDbType != "biblio")
                    KernelDbInfoCollection.RemoveDupByCaption(ref froms,
                        strLang);
                else
                    KernelDbInfoCollection.RemoveDupByStyle(ref froms);
            }

            List<BiblioDbFromInfo> info_list = new List<BiblioDbFromInfo>();

            int nIndexOfID = -1;    // __id;�����ڵ��±�

            for (int i = 0; i < froms.Count; i++)
            {
                From from = froms[i];

                Caption caption = from.GetCaption(strLang);
                if (caption == null)
                {
                    caption = from.GetCaption(null);
                    if (caption == null)
                    {
                        strError = "��һ��from�����captions������";
                        goto ERROR1;
                    }
                }

                if (caption.Value == "__id")
                    nIndexOfID = i;

                BiblioDbFromInfo info = new BiblioDbFromInfo();
                info.Caption = caption.Value;
                info.Style = from.Styles;

                info_list.Add(info);
            }

            // ����������ֹ� __id caption
            if (nIndexOfID != -1)
            {
                BiblioDbFromInfo temp = info_list[nIndexOfID];
                info_list.RemoveAt(nIndexOfID);
                info_list.Add(temp);
            }

            infos = new BiblioDbFromInfo[info_list.Count];
            info_list.CopyTo(infos);

            return infos.Length;
        ERROR1:
            return -1;
        }

        // һ�μ������������
        // "������";
        // "�ο�ID";
        // return:
        //      -1  ����
        //      0   һ��Ҳû������
        //      >0  ���е��ܸ�����ע�⣬�ⲻһ����results�з��ص�Ԫ�ظ�����results�з��صĸ�����Ҫ�ܵ�nMax�����ƣ���һ������ȫ�����и���
        public int GetItemRec(
            RmsChannelCollection channels,
            string strDbType,
            string strWordList,
            string strFrom,
            int nMax,
            string strStyle,
            out List<Record> results,
            out string strError)
        {
            strError = "";

            results = new List<Record>();

            LibraryApplication app = this;

            List<string> dbnames = null;
            int nRet = app.GetDbNames(
    strDbType,
    out dbnames,
    out strError);
            if (nRet == -1)
                return -1;

            // �������ʽ
            string strQueryXml = "";
            int nDbCount = 0;
            for (int i = 0; i < dbnames.Count; i++)
            {
                string strDbName = dbnames[i];

                if (String.IsNullOrEmpty(strDbName) == true)
                    continue;

                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strWordList)
                    + "</word><match>exact</match><relation>list</relation><dataType>string</dataType><maxCount>" + nMax.ToString() + "</maxCount></item><lang>zh</lang></target>";

                if (nDbCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
                nDbCount++;
            }

            if (nDbCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            Record[] records = null;
            long lRet = channel.DoSearchEx(strQueryXml,
                "default",
                strStyle, // strOuputStyle
                nMax,
                "zh",
                strStyle + ",id",    // "id,xml,timestamp",
                out records,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = "���м�����һ��Ҳû������";
                return 0;
            }

            long lHitCount = lRet;
            if (nMax == -1)
                nMax = (int)lHitCount;
            else
            {
                if (nMax > lHitCount)
                    nMax = (int)lHitCount;
            }

            if (records == null || records.Length == 0)
            {
                strError = "records error";
                return -1;
            }

            results.AddRange(records);

            if (results.Count == lHitCount)
                return (int)lHitCount;

            // �����һ��û��ȡ�꣬��Ҫ����ȡ��
            if (nMax > records.Length)
            {
                long lStart = records.Length;
                long lCount = nMax - lStart;
                for (; ; )
                {
                    lRet = channel.DoGetSearchResult(
                    "default",
                    lStart,
                    lCount,
                    strStyle + ",id",    // "id,xml,timestamp",
                    "zh",
                    null,
                    out records,
                    out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    Debug.Assert(records != null, "");

                    if (records.Length == 0)
                    {
                        strError = "DoGetSearchResult records error";
                        goto ERROR1;
                    }

                    results.AddRange(records);
                    lStart += records.Length;
                    if (lStart >= lHitCount
                        || lStart >= nMax)
                        break;
                    lCount -= records.Length;
                }
            }

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

        // �����ע��¼(��װ��İ汾)
        // ������Ϊ��ִ��Ч�ʷ����ԭ��, ��ȥ��ó���1�����ϵ�·��
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��(���������������, strOutputPathҲ�����˵�һ����·��)
        public int GetCommentRecXml(
            RmsChannelCollection channels,
            string strRefID,
            out string strXml,
            out string strOutputPath,
            out string strError)
        {
            byte[] timestamp = null;

            return GetCommentRecXml(
                channels,
                strRefID,
                out strXml,
                out strOutputPath,
                out timestamp,
                out strError);
        }

        // TODO���ж�strRedID�Ƿ�Ϊ��
        // �����ע��¼
        // ������Ϊ��ִ��Ч�ʷ����ԭ��, ��ȥ��ó���1�����ϵ�·��
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��(���������������, strOutputPathҲ�����˵�һ����·��)
        public int GetCommentRecXml(
            RmsChannelCollection channels,
            string strRefID,
            out string strXml,
            out string strOutputPath,
            out byte[] timestamp,
            out string strError)
        {
            strOutputPath = "";
            strXml = "";
            strError = "";
            timestamp = null;

            LibraryApplication app = this;

            // �������ʽ
            string strQueryXml = "";
            int nDbCount = 0;
            for (int i = 0; i < app.ItemDbs.Count; i++)
            {
                string strDbName = app.ItemDbs[i].CommentDbName;

                if (String.IsNullOrEmpty(strDbName) == true)
                    continue;

                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + "�ο�ID") 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strRefID) + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>1000</maxCount></item><lang>zh</lang></target>";

                if (nDbCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
                nDbCount++;
            }

            if (nDbCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
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
                strError = "�ο�ID '" + strRefID + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            List<string> aPath = null;
            lRet = channel.DoGetSearchResult(
                "default",
                0,
                1,
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

            string strMetaData = "";
            lRet = channel.GetRes(aPath[0],
                out strXml,
                out strMetaData,
                out timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

        // TODO: �ж�strBarcode�Ƿ�Ϊ��
        // ���ݲ�����Ŷ�ʵ�����в���
        // ������ֻ�������, ������ü�¼��
        // return:
        //      -1  error
        //      ����    ���м�¼����(������nMax�涨�ļ���)
        public int SearchItemRecDup(
            // RmsChannelCollection channels,
            RmsChannel channel,
            string strBarcode,
            int nMax,
            out List<string> aPath,
            out string strError)
        {
            strError = "";
            aPath = null;

            LibraryApplication app = this;

            /* �����ں˳����⵫��û��strError���ݵ�ʽ��
<group>
	<operator value='OR'/>
	<target list='ͼ���Ŀʵ��:������'>
		<item><word>0000001</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item>
<lang>zh</lang>
</target>
</group>             * */

            // �������ʽ
            string strQueryXml = "";
            int nDbCount = 0;
            for (int i = 0; i < app.ItemDbs.Count; i++)
            {
                string strDbName = app.ItemDbs[i].DbName;

                // 2008/10/16 
                if (String.IsNullOrEmpty(strDbName) == true)
                    continue;


                // 2007/4/5 ���� ������ GetXmlStringSimple()
                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + "������")       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strBarcode)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>"+nMax.ToString()+"</maxCount></item><lang>zh</lang></target>";

                if (nDbCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
                nDbCount++;
            }

            if (nDbCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            /*
            RmsChannel channel = channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }
             * */
            Debug.Assert(channel != null, "");

            long lRet = channel.DoSearch(strQueryXml,
                "default",
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
            {
                // TODO: Ϊ�˸�������ķ��㣬������strError�м���strQueryXml����
                strError = "SearchItemRecDup() DoSearch() error: " + strError;
                goto ERROR1;
            }

            // not found
            if (lRet == 0)
            {
                strError = "������� '" + strBarcode + "' û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            lRet = channel.DoGetSearchResult(
                "default",
                0,
                nMax,
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
            {
                strError = "SearchItemRecDup() DoGetSearchResult() error: " + strError;
                goto ERROR1;
            }

            if (aPath.Count == 0)
            {
                strError = "DoGetSearchResult aPath error ��ǰ���Ѿ����е�����ì��";
                goto ERROR1;
            }

            return (int)lHitCount;
        ERROR1:
            return -1;
        }



        // ����¼���и�Ϊǰ׺������ֵ��������
        void SplitLoginName(string strLoginName,
            out string strPrefix,
            out string strName)
        {
            int nRet = 0;

            strLoginName = strLoginName.Trim();

            List<string> prefixes = new List<string>();
            prefixes.Add("NB:");
            prefixes.Add("EM:");
            prefixes.Add("TP:");
            prefixes.Add("ID:");    // 2009/9/22 
            prefixes.Add("CN:");    // 2012/11/7

            for (int i = 0; i < prefixes.Count; i++)
            {
                nRet = strLoginName.ToUpper().IndexOf(prefixes[i]);
                if (nRet == 0)
                {
                    strPrefix = prefixes[i];
                    strName = strLoginName.Substring(nRet + prefixes[i].Length).Trim();
                    return;
                }
            }

            strPrefix = "";
            strName = strLoginName;
        }

        // ��ö��߼�¼, ����������Ƿ���ϡ�Ϊ��¼��;
        // �ú��������������ڣ��������ö��ּ�����ڣ����������������
        // parameters:
        //      strQueryWord ��¼��
        //          1) �����"NB:"��ͷ����ʾ�����������ս��м���������������֮������'|'��������������������Ϊ8�ַ���ʽ
        //          2) �����"EM:"��ͷ����ʾ����email��ַ���м���
        //          3) �����"TP:"��ͷ����ʾ���õ绰������м���
        //          4) �����"ID:"��ͷ����ʾ�������֤�Ž��м���
        //          5) �����"CN:"��ͷ����ʾ����֤��������м���
        //          6) ������֤����Ž��м���
        //      strPassword ���롣���Ϊnull����ʾ�����������жϡ�ע�⣬����""
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        int GetReaderRecXmlForLogin(
            RmsChannelCollection channels,
            string strLibraryCodeList,
            string strQueryWord,
            string strPassword,
            int nIndex,
            string strClientIP,
            string strGetToken,
            out bool bTempPassword,
            out string strXml,
            out string strOutputPath,
            out byte [] output_timestamp,
            out string strToken,
            out string strError)
        {
            strOutputPath = "";
            strXml = "";
            strError = "";
            output_timestamp = null;
            bTempPassword = false;
            strToken = "";

            int nRet = 0;
            LibraryApplication app = this;
            string strFrom = "֤����";
            string strMatch = "exact";

            // �������ʽ
            string strQueryXml = "";

            // int nRet = 0;
            strQueryWord = strQueryWord.Trim();

            string strPrefix = "";
            string strName = "";

            SplitLoginName(strQueryWord, out strPrefix, out strName);

            bool bBarcode = false;

            // ע��������������µ�prefix�� ���� SplitLoginName() ҲҪͬ���޸�
            // û��ǰ׺
            if (strPrefix == "")
            {
                bBarcode = true;
                strFrom = "֤����";
                strMatch = "exact";
            }
            else if (strPrefix == "NB:")
            {
                bBarcode = false;
                strFrom = "��������";
                strMatch = "left";
                strQueryWord = strName;
            }
            else if (strPrefix == "EM:")
            {
                bBarcode = false;
                strFrom = "Email";
                strMatch = "exact";
                strQueryWord = strName;
            }
            else if (strPrefix == "TP:")
            {
                bBarcode = false;
                strFrom = "�绰";
                strMatch = "exact";
                strQueryWord = strName;
            }
            else if (strPrefix == "ID:")
            {
                bBarcode = false;
                strFrom = "���֤��";
                strMatch = "exact";
                strQueryWord = strName;
            }
            else if (strPrefix == "CN:")
            {
                bBarcode = false;
                strFrom = "֤��";
                strMatch = "exact";
                strQueryWord = strName;
            }
            else
            {
                strError = "δ֪�ĵ�¼��ǰ׺ '" + strPrefix + "'";
                return -1;
            }

            List<string> dbnames = new List<string>();
            // ��ö��߿����б�
            // parameters:
            //      strReaderDbNames    �����б��ַ��������Ϊ�գ����ʾȫ�����߿�
            nRet = GetDbNameList("",
                strLibraryCodeList,
                out dbnames,
                out strError);
            if (nRet == -1)
                return -1;

            if (dbnames.Count == 0)
            {
                if (app.ReaderDbs.Count == 0)
                    strError = "��ǰ��û�����ö��߿�";
                else
                    strError = "��ǰû�п��Բ����Ķ��߿�";
                return -1;
            }

            {
                int i = 0;
                foreach (string strDbName in dbnames)
                {
                    if (string.IsNullOrEmpty(strDbName) == true)
                        continue;

                    Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "");

                    // ���100��
                    // 2007/4/5 ���� ������ GetXmlStringSimple()
                    string strOneDbQuery = "<target list='"
                        + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 
                        + "'><item><word>"
                        + StringUtil.GetXmlStringSimple(strQueryWord)
                        + "</word><match>" + strMatch + "</match><relation>=</relation><dataType>string</dataType><maxCount>100</maxCount></item><lang>zh</lang></target>";

                    if (string.IsNullOrEmpty(strQueryXml) == false)
                    {
                        Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                        strQueryXml += "<operator value='OR'/>";
                    }

                    strQueryXml += strOneDbQuery;
                    i++;
                }

                if (i > 1)
                {
                    strQueryXml = "<group>" + strQueryXml + "</group>";
                }
            }

#if NO
            if (app.ReaderDbs.Count == 0)
            {
                strError = "��δ���ö��߿�";
                return -1;
            }

            {
                for (int i = 0; i < app.ReaderDbs.Count; i++)
                {
                    string strDbName = app.ReaderDbs[i].DbName;

                    Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "");

                    // ���100��
                    // 2007/4/5 ���� ������ GetXmlStringSimple()
                    string strOneDbQuery = "<target list='"
                        + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 
                        + "'><item><word>"
                        + StringUtil.GetXmlStringSimple(strQueryWord)
                        + "</word><match>"+strMatch+"</match><relation>=</relation><dataType>string</dataType><maxCount>100</maxCount></item><lang>zh</lang></target>";

                    if (i > 0)
                    {
                        Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                        strQueryXml += "<operator value='OR'/>";
                    }

                    strQueryXml += strOneDbQuery;
                }

                if (app.ReaderDbs.Count > 0)
                {
                    strQueryXml = "<group>" + strQueryXml + "</group>";
                }
            }
#endif

            if (String.IsNullOrEmpty(strQueryXml) == true)
            {
                strError = "��δ���ö��߿�";
                return -1;
            }

            RmsChannel channel = channels.GetChannel(app.WsUrl);
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
            {
                strError = "channel.DoSearch() error : " + strError;
                goto ERROR1;
            }

            // not found
            if (lRet == 0)
            {
                strError = "û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            if (lHitCount > 1 && bBarcode == true)
            {
                strError = "ϵͳ����: ֤�����Ϊ '" + strQueryWord + "' �Ķ��߼�¼����һ��";
                return -1;
            }

            lHitCount = Math.Min(lHitCount, 100);

            List<string> aPath = null;
            lRet = channel.DoGetSearchResult(
                "default",
                0,
                lHitCount,
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

            /*
            // ֻ����һ��
            if (aPath.Count == 1)
                goto LOADONE;
             * */


            // �ų���֤״̬��ʧ����Щ
            List<string> aPathNew = new List<string>();
            List<string> aXml = new List<string>();
            List<string> aOutputPath = new List<string>();
            List<byte[]> aTimestamp = new List<byte[]>();

            for (int i = 0; i < aPath.Count; i++)
            {
                string strMetaData = "";
                byte[] timestamp = null;

                lRet = channel.GetRes(aPath[i],
                    out strXml,
                    out strMetaData,
                    out timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (strPassword != null)
                {
                    XmlDocument readerdom = null;
                    nRet = LibraryApplication.LoadToDom(strXml,
                        out readerdom,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "װ�ض��߼�¼ '" + aPath[i] + "' ����XML DOMʱ��������: " + strError;
                        return -1;
                    }

                    /*
                    string strState = DomUtil.GetElementText(readerdom.DocumentElement,
                        "state");
                     * */

                    if (strPassword != null)    // 2009/9/22 
                    {
                        // ��֤��������
                        // return:
                        //      -1  error
                        //      0   ���벻��ȷ
                        //      1   ������ȷ
                        nRet = VerifyReaderPassword(
                            strClientIP,
                            readerdom,
                            strPassword,
                            this.Clock.Now,
                            out bTempPassword,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 0)
                            continue;

                        if (string.IsNullOrEmpty(strGetToken) == false)
                        {
                            string strHashedPassword = DomUtil.GetElementInnerText(readerdom.DocumentElement, "password");
                            nRet = MakeToken(strClientIP,
                                GetTimeRangeByStyle(strGetToken),
                                strHashedPassword,
                                out strToken,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }
                    }
                }

                aPathNew.Add(aPath[i]);
                aXml.Add(strXml);
                aOutputPath.Add(strOutputPath);
                aTimestamp.Add(timestamp);
            }

            // ���˺�ȴ�ַ���һ����û���ˡ��պ��Ÿ�����ǰ�ĵ�һ��?
            if (aPathNew.Count == 0)
            {
                /*
                aPathNew.Add(aPath[0]);
                aPath = aPathNew;
                lHitCount = 1;
                goto LOADONE;
                 * */
                return 0;
            }

            if (nIndex >= aXml.Count)
            {
                strError = "ѡ������ֵ " + nIndex.ToString() + " Խ����Χ��";
                return -1;
            }

            if (aXml.Count == 1 && nIndex == -1)
                nIndex = 0;

            if (nIndex != -1)
            {
                strXml = aXml[nIndex];
                strOutputPath = aOutputPath[nIndex];
                output_timestamp = aTimestamp[nIndex];
            }
            return aPathNew.Count;
        ERROR1:
            return -1;
            /*
        LOADONE:
        {
                string strMetaData = "";
                byte[] timestamp = null;

                lRet = channel.GetRes(aPath[0],
                    out strXml,
                    out strMetaData,
                    out timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }
            return (int)lHitCount;
             * */
        }



        // ��XMLװ��DOM
        public static int LoadToDom(string strXml,
            out XmlDocument dom,
            out string strError)
        {
            strError = "";

            dom = new XmlDocument();

            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����" + ex.Message;
                return -1;
            }

            return 0;
        }

        /*
        public int ConvertReaderXmlToHtml(
            string strXml,
            OperType opertype,
            string[] saBorrowedItemBarcode,
            string strCurrentItemBarcode,
            out string strResult,
            out string strError)
        {
            return ConvertReaderXmlToHtml(
                this.CfgDir + "\\readerxml2html.cs",
                this.CfgDir + "\\readerxml2html.cs.ref",
                strXml,
                opertype,
                saBorrowedItemBarcode,
                strCurrentItemBarcode,
                out strResult,
                out strError);
        }
         */

        // parameters:
        //      strMessageTempate   ��Ϣ����ģ�塣���п���ʹ�� %name% %barcode% %temppassword% %expiretime% %period% �Ⱥ�
        // return:
        //      -1  ����
        //      0   ��Ϊ�������߱�����û�гɹ�ִ��
        //      1   ���ܳɹ�ִ��
        public int ResetPassword(
            // string strLibraryCodeList,
            string strParameters,
            string strMessageTemplate,
            out string strError)
        {
            strError = "";

            MessageInterface external_interface = this.GetMessageInterface("sms");

            if (external_interface == null)
            {
                strError = "��ǰϵͳ��δ���ö���Ϣ (sms) �ӿڣ��޷�������������Ĳ���";
                return -1;
            }

            Hashtable parameters = StringUtil.ParseParameters(strParameters, ',', '=');
            string strLoginName = (string)parameters["barcode"];
            string strNameParam = (string)parameters["name"];
            string strTelParam = (string)parameters["tel"];
            string strLibraryCodeList = (string)parameters["librarycode"];  // ���Ƽ������߼�¼�ķ�Χ

            if (string.IsNullOrEmpty(strLoginName) == true)
            {
                strError = "ȱ�� barcode ����";
                return -1;
            }
            if (string.IsNullOrEmpty(strNameParam) == true)
            {
                strError = "ȱ�� name ����";
                return -1;
            }
            if (string.IsNullOrEmpty(strTelParam) == true)
            {
                strError = "ȱ�� tel ����";
                return -1;
            }

            // �жϵ绰�����Ƿ�Ϊ�ֻ�����
            if (strTelParam.Length != 11)
            {
                strError = "���ṩ�ĵ绰����Ӧ���� 11 λ���ֻ�����";
                return 0;
            }

            string strXml = "";
            string strOutputPath = "";

            byte[] timestamp = null;

            // ��ʱ��SessionInfo����
            SessionInfo sessioninfo = new SessionInfo(this);
            try
            {
                bool bTempPassword = false;
                string strToken = "";
                // ��ö��߼�¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                int nRet = this.GetReaderRecXmlForLogin(
                    sessioninfo.Channels,
                    strLibraryCodeList,
                    strLoginName,
                    null,
                    -1,
                    sessioninfo.ClientIP,
                    null,
                    out bTempPassword,
                    out strXml,
                    out strOutputPath,
                    out timestamp,
                    out strToken,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�Ե�¼�� '" + strLoginName + "' �������߼�¼����: " + strError;
                    return -1;
                }
                if (nRet == 0)
                {
                    strError = "�����ʻ� '" + strLoginName + "' ������";
                    return 0;
                }
                if (nRet > 1)
                {
                    strError = "��¼�� '" + strLoginName + "' ��ƥ����ʻ�����һ��";
                    return 0;
                }

                Debug.Assert(nRet == 1);

                string strLibraryCode = "";
                // ��ö��߿�Ĺݴ���
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = GetLibraryCode(
                    strOutputPath,
                    out strLibraryCode,
                    out strError);
                if (nRet == -1)
                    return -1;

                XmlDocument readerdom = null;
                nRet = LibraryApplication.LoadToDom(strXml,
                    out readerdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                    return -1;
                }

                // �۲� password Ԫ�ص� lastResetTime ���ԣ����ڹ涨��ʱ�䳤����������ٴν�������

                // �˶� barcode
                string strBarcode = DomUtil.GetElementText(readerdom.DocumentElement, "barcode");
                if (strBarcode.Trim() != strLoginName.Trim())
                {
                    strError = "֤����Ų�ƥ��";
                    return -1;
                }

                // �˶� name
                string strName = DomUtil.GetElementText(readerdom.DocumentElement, "name");
                if (strName.Trim() != strNameParam.Trim())
                {
                    strError = "������ƥ��";
                    return 0;
                }

                // �˶� tel
                string strTel = DomUtil.GetElementText(readerdom.DocumentElement, "tel");
                if (string.IsNullOrEmpty(strTel) == true)
                {
                    strError = "���߼�¼��û�еǼǵ绰���룬�޷�������������Ĳ���";
                    return 0;
                }

                string strResultTel = ""; ;
                string[] tels = strTel.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string tel in tels)
                {
                    string strOneTel = tel.Trim();
                    if (strOneTel == strTelParam.Trim())
                    {
                        strResultTel = strOneTel;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(strResultTel) == true)
                {
                    strError = "���ṩ�ĵ绰����Ͷ��߼�¼�еĵ绰���벻ƥ��";
                    return -1;
                }

                DateTime end;
                // �۲��� password Ԫ�� tempPasswordExpire �����в�����ʧЧ�ڣ����������ʱ���Ժ���ܽ��б��β���
                // parameters:
                //      now ��ǰʱ�䡣����ʱ��
                // return:
                //      -1  ����
                //      0   �Ѿ�����ʧЧ��
                //      1   ����ʧЧ������
                nRet = CheckOldExpireTime(readerdom,
                    this.Clock.Now,
                    out end,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 1)
                {
                    strError = "������������Ĳ��������ϴβ����������һСʱ���������ܾ������� "+end.ToShortTimeString()+" �Ժ��ٽ��в���";
                    return 0;
                }

                // �����趨һ������
                Random rnd = new Random();
                string strReaderTempPassword = rnd.Next(1, 999999).ToString();

                DateTime expire = this.Clock.Now + new TimeSpan(1, 0, 0);   // ����ʱ��
                string strExpireTime = DateTimeUtil.Rfc1123DateTimeStringEx(expire);

                if (string.IsNullOrEmpty(strMessageTemplate) == true)
                    strMessageTemplate = "�𾴵� %name% ���ã�\n���Ķ����ʻ�(֤�����Ϊ %barcode%)������ʱ���� %temppassword%�������� %period% ��������¼���Զ���Ϊ��ʽ����";

                string strBody = strMessageTemplate.Replace("%barcode%", strBarcode)
                    .Replace("%name%", strName)
                    .Replace("%temppassword%", strReaderTempPassword)
                    .Replace("%expiretime%", expire.ToLongTimeString())
                    .Replace("%period%", "һ��Сʱ");
                // string strBody = "����(֤�����) " + strBarcode + " ���ʻ������Ѿ�������Ϊ " + strReaderNewPassword + "";

                // ���ֻ����뷢�Ͷ���
                {
                    // ������Ϣ
                    try
                    {
                        // ����һ����Ϣ
                        // parameters:
                        //      strPatronBarcode    ����֤�����
                        //      strPatronXml    ���߼�¼XML�ַ����������Ҫ��֤����������ĳЩ�ֶ���ȷ����Ϣ���͵�ַ�����Դ�XML��¼��ȡ
                        //      strMessageText  ��Ϣ����
                        //      strError    [out]���ش����ַ���
                        // return:
                        //      -1  ����ʧ��
                        //      0   û�б�Ҫ����
                        //      >=1   ���ͳɹ�������ʵ�ʷ��͵���Ϣ����
                        nRet = external_interface.HostObj.SendMessage(
                            strBarcode,
                            readerdom.DocumentElement.OuterXml,
                            strBody,
                            strLibraryCode,
                            out strError);
                    }
                    catch (Exception ex)
                    {
                        strError = external_interface.Type + " ���͵��ⲿ��Ϣ�ӿ�Assembly��SendMessage()�����׳��쳣: " + ex.Message;
                        nRet = -1;
                    }
                    if (nRet == -1)
                    {
                        strError = "����� '" + strBarcode + "' ����" + external_interface.Type + " messageʱ����: " + strError;
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            "��������֪ͨ",
                            external_interface.Type + " message ��������֪ͨ��Ϣ���ʹ�����",
                            1);
                        this.WriteErrorLog(strError);
                        return -1;
                    }
                    else
                    {
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(
        strLibraryCode,
        "��������֪ͨ",
        external_interface.Type + " message ��������֪ͨ��Ϣ������",
        nRet);  // �����������ܶ��ڴ���
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(strLibraryCode,
                            "��������֪ͨ",
                            external_interface.Type + " message ��������֪ͨ����",
                            1);
                    }
                }

                byte[] output_timestamp = null;
                nRet = ChangeReaderTempPassword(
        sessioninfo,
        strOutputPath,
        readerdom,
        strReaderTempPassword,
        strExpireTime,
        timestamp,
        out output_timestamp,
        out strError);
                if (nRet == -1)
                    return -1;  // ��ʱ�����Ѿ�����������ʱ���벢δ�޸ĳɹ�

            }
            finally
            {
                sessioninfo.CloseSession();
                sessioninfo = null;
            }

            strError = "��ʱ������ͨ�����ŷ�ʽ���͵��ֻ� " + strTelParam + "���밴���ֻ�������ʾ���в���";
            return 1;
        }

        // �۲��� password Ԫ�� tempPasswordExpire �����в�����ʧЧ�ڣ����������ʱ���Ժ���ܽ��б��β���
        // parameters:
        //      now ��ǰʱ�䡣����ʱ��
        //      expire  ʧЧ��ĩ��ʱ�䡣����ʱ��
        // return:
        //      -1  ����
        //      0   �Ѿ�����ʧЧ��
        //      1   ����ʧЧ������
        static int CheckOldExpireTime(XmlDocument readerdom,
            DateTime now,
            out DateTime expire,
            out string strError)
        {
            strError = "";
            expire = new DateTime(0);

            XmlNode node = readerdom.DocumentElement.SelectSingleNode("password");
            if (node == null)
                return 0;

            string strExpireTime = DomUtil.GetAttr(node,
"tempPasswordExpire");
            if (string.IsNullOrEmpty(strExpireTime) == true)
                return 0;

            try
            {
                expire = DateTimeUtil.FromRfc1123DateTimeString(strExpireTime).ToLocalTime();

                if (now > expire)
                {
                    // ʧЧ���Ѿ�����
                    return 0;
                }
            }
            catch (Exception)
            {
                strError = "��ʱ����ʧЧ��ʱ���ַ��� '" + strExpireTime + "' ��ʽ����ȷ��ӦΪ RFC1123 ��ʽ";
                return -1;
            }

            return 1;   // ����ʧЧ������
        }

        // �������˺��Ƿ����
        // return:
        //      -1  error
        //      0   ������
        //      1   ����
        //      >1  ����һ��
        public int VerifyReaderAccount(
            RmsChannelCollection channels,
            string strLoginName,
            out string strError)
        {
            strError = "";
            string strXml = "";
            string strOutputPath = "";

            byte[] timestamp = null;
            bool bTempPassword = false;
            string strToken = "";

            // ��ö��߼�¼
            // return:
            //      -1  error
            //      0   not found
            //      1   ����1��
            //      >1  ���ж���1��
            int nRet = this.GetReaderRecXmlForLogin(
                channels,
                "",    // ȫ���ֹݵĶ��߿�
                strLoginName,
                null,
                -1,
                null,
                null,
                out bTempPassword,
                out strXml,
                out strOutputPath,
                out timestamp,
                out strToken,
                out strError);
            if (nRet == -1)
            {
                strError = "�Ե�¼�� '" + strLoginName + "' �������߼�¼����: " + strError;
                return -1;
            }
            if (nRet == 0)
            {
                strError = "�ʻ� '"+strLoginName+"' ������";
                return 0;
            }
            if (nRet > 1)
            {
                strError = "��¼�� '" + strLoginName + "' ��ƥ����ʻ�����һ��";
                return nRet;
            }

            Debug.Assert(nRet == 1);

            return 1;
        }

        // xxxx|||xxxx �ұ߲����� timerange
        static string GetTimeRangeFromToken(string strToken)
        {
            string strLeft = "";
            string strRight = "";
            StringUtil.ParseTwoPart(strToken, "|||", out strLeft, out strRight);
            return strRight;
        }

        public static string GetTimeRangeByStyle(string strStyle)
        {
            if (string.IsNullOrEmpty(strStyle) == true)
                return DateTimeUtil.DateTimeToString8(DateTime.Now);
            if (strStyle == "day")
                return DateTimeUtil.DateTimeToString8(DateTime.Now);
            if (strStyle == "month")
            {
                return DateTimeUtil.DateTimeToString8(DateTime.Now) + "-" + DateTimeUtil.DateTimeToString8(DateTime.Now.AddDays(31));
            }
            if (strStyle == "year")
            {
                return DateTimeUtil.DateTimeToString8(DateTime.Now) + "-" + DateTimeUtil.DateTimeToString8(DateTime.Now.AddDays(365));
            }

            // default
            return DateTimeUtil.DateTimeToString8(DateTime.Now);
        }

        // ���� token
        public static int MakeToken(string strClientIP,
            string strTimeRange,
            string strHashedPassword,
            out string strToken,
            out string strError)
        {
            strError = "";
            strToken = "";

            if (string.IsNullOrEmpty(strTimeRange) == true)
                strTimeRange = GetTimeRangeByStyle(null);

            string strHashed = "";
            string strPlainText = strClientIP + strHashedPassword + strTimeRange;
            try
            {
                strHashed = Cryptography.GetSHA1(strPlainText);
            }
            catch
            {
                strError = "�ڲ�����";
                return -1;
            }

            strToken = strHashed.Replace(",", "").Replace("=","") + "|||" + strTimeRange;
            return 0;
        }

        static bool IsInTimeRange(DateTime now,
            string strTimeRange)
        {
            int nRet = strTimeRange.IndexOf("-");
            if (nRet == -1)
            {
                if (strTimeRange == DateTimeUtil.DateTimeToString8(now))
                    return true;
                return false;
            }

            try
            {
                string strStart = "";
                string strEnd = "";
                StringUtil.ParseTwoPart(strTimeRange, "-", out strStart, out strEnd);
                DateTime start = DateTimeUtil.Long8ToDateTime(strStart);
                DateTime end = DateTimeUtil.Long8ToDateTime(strEnd);
                if (now > start && now < end)
                    return true;
                return false;
            }
            catch
            {
                return false;
            }
        }

        // ��֤ TOKEN
        // Token �ķ�������Ϊ�� client ip + hashed password + time range Ȼ�� Hash�� Hash ���Ժ� time range �ַ����ٷ�������һ��
        // return:
        //      -1  ����
        //      0   ��֤��ƥ��
        //      1   ��֤ƥ��
        public static int VerifyToken(
            string strClientIP,
            string strToken,
            string strHashedPassword,
            out string strError)
        {
            strError = "";
            string strTimeRange = GetTimeRangeFromToken(strToken);
            if (string.IsNullOrEmpty(strTimeRange) == true)
                strTimeRange = GetTimeRangeByStyle(null);

            // ����ʱ���Ƿ�ʧЧ
            if (IsInTimeRange(DateTime.Now, strTimeRange) == false)
            {
                strError = "token �Ѿ�ʧЧ";
                return 0;
            }

            string strHashed = "";
            string strPlainText = strClientIP + strHashedPassword + strTimeRange;
            try
            {
                strHashed = Cryptography.GetSHA1(strPlainText);
            }
            catch
            {
                strError = "�ڲ�����";
                return -1;
            }
            strHashed = strHashed.Replace(",", "").Replace("=", "");
            strHashed += "|||" + strTimeRange;
            if (strHashed == strToken)
                return 1;   // ƥ��

            return 0;   // ��ƥ��
        }

        // ������ѯ���ߵ�¼
        // text-level: �û���ʾ
        // parameters:
        //      strLoginName ��¼��
        //          1) �����"NB:"��ͷ����ʾ�����������ս��м���������������֮������'|'��������������������Ϊ8�ַ���ʽ
        //          2) �����"EM:"��ͷ����ʾ����email��ַ���м���
        //          3) �����"TP:"��ͷ����ʾ���õ绰������м���
        //          4) ������֤����Ž��м���
        //      strPassword ���롣���Ϊnull����ʾ�����������жϡ�ע�⣬����""
        //              ������Ϊ token: ��̬
        //      nIndex  ����ж��ƥ��Ķ��߼�¼���˲�����ʾҪѡ��������һ����
        //              ���Ϊ-1����ʾ�״ε��ô˺���������֪���ѡ�����ʱ���ж���������᷵��>1��ֵ
        //      strGetToken �Ƿ�Ҫ��� token ������Ч�ڡ� �� /  day / month / year
        //      strOutputUserName   ���ض���֤�����
        // return:
        //      -1  error
        //      0   ��¼δ�ɹ�
        //      1   ��¼�ɹ�
        //      >1  �ж���˻�����������
        public int LoginForReader(SessionInfo sessioninfo,
            string strLoginName,
            string strPassword,
            string strLocation,
            string strLibraryCodeList,
            int nIndex,
            string strGetToken,
            out string strOutputUserName,
            out string strRights,
            out string strLibraryCode,
            out string strError)
        {
            strError = "";
            // ������ݵ�¼
            string strXml = "";
            string strOutputPath = "";
            byte[] timestamp = null;
            strRights = "";
            strOutputUserName = "";
            strLibraryCode = "";

            // 2009/9/22 
            if (String.IsNullOrEmpty(strLoginName) == true)
            {
                strError = "���� strLoginName ����Ϊ��";
                return -1;
            }

            if (this.LoginCache != null)
            {
                Account temp_account = this.LoginCache.Get(strLoginName) as Account;
                if (temp_account != null)
                {
                    if (strPassword != null)    // 2014/12/20
                    {
                        if (temp_account.Password != strPassword)
                        {
                            bool bIsToken1 = StringUtil.HasHead(strPassword, "token:");
                            bool bIsToken2 = StringUtil.HasHead(temp_account.Password, "token:");

                            if (bIsToken1 == bIsToken2)
                            {
                                // text-level: �û���ʾ
                                strError = this.GetString("�ʻ������ڻ����벻��ȷ");    // "�ʻ������ڻ����벻��ȷ"
                                return -1;
                            }
                            else
                                goto DO_LOGIN;  // ��������ͨ��¼
                        }
                    }

                    sessioninfo.Account = temp_account;

                    strRights = temp_account.RightsOrigin;
                    strOutputUserName = temp_account.UserID;
                    return 1;
                }
            }

            DO_LOGIN:

            bool bTempPassword = false;
            string strToken = "";

            // ��ö��߼�¼, ����������Ƿ���ϡ�Ϊ��¼��;
            // �ú��������������ڣ��������ö��ּ�����ڣ����������������
            // parameters:
            //      strQueryWord ��¼��
            //          1) �����"NB:"��ͷ����ʾ�����������ս��м���������������֮������'|'��������������������Ϊ8�ַ���ʽ
            //          2) �����"EM:"��ͷ����ʾ����email��ַ���м���
            //          3) �����"TP:"��ͷ����ʾ���õ绰������м���
            //          4) �����"ID:"��ͷ����ʾ�������֤�Ž��м���
            //          5) �����"CN:"��ͷ����ʾ����֤��������м���
            //          6) ������֤����Ž��м���
            //      strPassword ���롣���Ϊnull����ʾ�����������жϡ�ע�⣬����""
            // return:
            //      -1  error
            //      0   not found
            //      1   ����1��
            //      >1  ���ж���1��
            int nRet = this.GetReaderRecXmlForLogin(
                sessioninfo.Channels,
                strLibraryCodeList,
                strLoginName,
                strPassword,
                nIndex,
                sessioninfo.ClientIP,
                strGetToken,
                out bTempPassword,
                out strXml,
                out strOutputPath,
                out timestamp,
                out strToken,
                out strError);
            if (nRet == -1)
            {
                // text-level: �û���ʾ
                strError = string.Format(this.GetString("�Ե�¼��s��¼ʱ, ���������ʻ���¼����s"),  // "�Ե�¼�� '{0}' ��¼ʱ, ���������ʻ���¼����: {1}";
                    strLoginName,
                    strError);
                    
                    // "�Ե�¼�� '" + strLoginName + "' ��¼ʱ, ���������ʻ���¼����: " + strError;
                return -1;
            }

            if (nRet == 0)
            {
                // text-level: �û���ʾ
                strError = this.GetString("�ʻ������ڻ����벻��ȷ");    // "�ʻ������ڻ����벻��ȷ"
                return -1;
            }

            if (nRet > 1)
            {
                // ��δ����ѡ��
                if (nIndex == -1)
                {
                    // text-level: �û���ʾ
                    strError = string.Format(this.GetString("�Ե�¼��s��¼ʱ, ����ƥ����ʻ�����һ�����޷���¼"),  // "�Ե�¼�� '{0}' ��¼ʱ, ����ƥ����ʻ�����һ�����޷���¼������֤�������Ϊ��¼�����½��е�¼��"
                        strLoginName);
                        // "�Ե�¼�� '" + strLoginName + "' ��¼ʱ, ����ƥ����ʻ�����һ�����޷���¼������֤�������Ϊ��¼�����½��е�¼��";
                    return nRet;
                }
            }

            XmlDocument readerdom = null;
            nRet = LibraryApplication.LoadToDom(strXml,
                out readerdom,
                out strError);
            if (nRet == -1)
            {
                strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                return -1;
            }


            // ���һ���ο��ʻ�

            Account accountref = null;
            // ��library.xml�ļ����� ���һ���ʻ�����Ϣ
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            nRet = GetAccount("reader",
                out accountref,
                out strError);
            if (nRet == -1)
            {
                // text-level: �û���ʾ
                strError = string.Format(this.GetString("���reader�ο��ʻ�ʱ����s"),    // "���reader�ο��ʻ�ʱ����: {0}"
                    strError);
                    // "���reader�ο��ʻ�ʱ����: " + strError;
                return -1;
            }

            if (nRet == 0)
                accountref = null;

            Account account = new Account();
            account.LoginName = strLoginName;
            account.Password = strPassword; // TODO: ����������� strPassword == null �����ã������ null �Ͳ���ʵ�ʵ������ַ�����
            account.Rights = "";    // �Ƿ���Ҫȱʡֵ?
            account.AccountLibraryCode = "";
            account.Access = "";
            if (accountref != null)
            {
                account.Rights = accountref.Rights;
                // account.LibraryCode = accountref.LibraryCode;
                account.Access = accountref.Access;
            }

            // ׷�Ӷ��߼�¼�ж����Ȩ��ֵ
            string strAddRights = DomUtil.GetElementText(readerdom.DocumentElement, "rights");
            if (string.IsNullOrEmpty(strAddRights) == false)
                account.Rights += "," + strAddRights;

            // 2015/1/15
            if (string.IsNullOrEmpty(strToken) == false)
                account.Rights += ",token:" + strToken;  // ��������ڻ����У���ξ�����ʱʧЧ?

            // ׷�Ӷ��߼�¼�ж���Ĵ�ȡ����
            string strAddAccess = DomUtil.GetElementText(readerdom.DocumentElement, "access");
            if (string.IsNullOrEmpty(strAddAccess) == false)
            {
                // TODO: �����Ż�Ϊ���������ǰһ���ַ���ĩβ�Ѿ��� ';' �Ͳ��� ';' �ˡ�
                account.Access += ";" + strAddAccess;
            }

            account.Type = "reader";
            account.Barcode = DomUtil.GetElementText(readerdom.DocumentElement,
                "barcode");
            account.UserID = account.Barcode;

            // 2012/9/8
            // string strLibraryCode = "";
            nRet = this.GetLibraryCode(strOutputPath,
                    out strLibraryCode,
                    out strError);
            if (nRet == -1)
                return -1;
            account.AccountLibraryCode = strLibraryCode;

            // 2009/9/26 
            if (String.IsNullOrEmpty(account.Barcode) == true)
            {
                // text-level: �û���ʾ
                strError = string.Format(this.GetString("���߼�¼��֤���������Ϊ�գ���¼ʧ��"),    // "���߼�¼��֤���������Ϊ�գ���¼ʧ��"
                    strError);
                return -1;
            }

            account.Name = DomUtil.GetElementText(readerdom.DocumentElement,
                "name");
            // 2010/11/11
            account.DisplayName = DomUtil.GetElementText(readerdom.DocumentElement,
"displayName");
            account.PersonalLibrary = DomUtil.GetElementText(readerdom.DocumentElement,
"personalLibrary");


            // 2007/2/15 
            account.ReaderDom = readerdom;
            account.ReaderDomLastTime = DateTime.Now;


            account.Location = strLocation;
            account.ReaderDomPath = strOutputPath;
            account.ReaderDomTimestamp = timestamp;

            sessioninfo.Account = account;

            strRights = account.RightsOrigin;   //  sessioninfo.RightsOrigin;

            strOutputUserName = account.UserID; // 2011/7/29 ����֤�����

            // ����ʱ��������Ϊ��ʽ����
            if (bTempPassword == true)
            {
                byte[] output_timestamp = null;
                // �޸Ķ�������
                nRet = ChangeReaderPassword(
                    sessioninfo,
                    strOutputPath,
                    ref readerdom,
                    strPassword,    // TODO: ��� strPassword == null ����ô����
                    timestamp,
                    out output_timestamp,
                    out strError);
                if (nRet == -1)
                    return -1;
                timestamp = output_timestamp;

                account.ReaderDom = readerdom;
                account.ReaderDomTimestamp = timestamp;
            }

            if (this.LoginCache != null && string.IsNullOrEmpty(account.LoginName) == false
                && account.Password != null)    // ��ֹ null password ���� cache ������������ 2014/12/20
            {
                DateTimeOffset offset = DateTimeOffset.Now.AddMinutes(20);
                this.LoginCache.Set(account.Barcode, account, offset);
            }

            return 1;
        }

        // readerdom�����仯��ˢ�������
        public static void RefreshReaderAccount(ref Account account,
            XmlDocument readerdom)
        {
            account.DisplayName = DomUtil.GetElementText(readerdom.DocumentElement,
"displayName");
            account.PersonalLibrary = DomUtil.GetElementText(readerdom.DocumentElement,
"personalLibrary");
            account.ReaderDomLastTime = DateTime.Now;

        }

        // �����ǰ�Ѿ���¼�Ķ��������û��Ķ��߼�¼DOM cache
        public void ClearLoginReaderDomCache(SessionInfo sessioninfo)
        {
            if (sessioninfo == null)
                return;

            if (sessioninfo.Account == null)
                return;

            if (sessioninfo.UserType != "reader")
                return;

            // �ڴ��������Ѿ����޸ģ�Ҫ�ȱ���DOM�����ݿ�
            if (sessioninfo.Account.ReaderDomChanged == true)
            {
                // �˴����Զ����棬�ͱ��水ťì���� -- һ��ˢ�£��ͻ��Զ����档
                string strError = "";
                // �����޸ĺ�Ķ��߼�¼DOM
                // return:
                //      -1  error
                //      0   û�б�Ҫ����(changed��־Ϊfalse)
                //      1   �ɹ�����
                int nRet = SaveLoginReaderDom(sessioninfo,
                    out strError);
                // ����������α���?
            }

            sessioninfo.Account.ReaderDom = null;
        }


        public void SetLoginReaderDomChanged(SessionInfo sessioninfo)
        {
            if (sessioninfo == null)
            {
                throw new Exception("sessioninfo = null");
            }

            if (sessioninfo.Account == null)
            {
                throw new Exception("sessioninfo.Account = null");
            }

            if (sessioninfo.Account.Type != "reader")
            {
                throw new Exception("sessioninfo.Account.Type != \"reader\"");
            }

            sessioninfo.Account.ReaderDomChanged = true;
        }

        // �����޸ĺ�Ķ��߼�¼DOM
        // return:
        //      -2  ʱ�����ͻ
        //      -1  error
        //      0   û�б�Ҫ����(changed��־Ϊfalse)
        //      1   �ɹ�����
        public int SaveLoginReaderDom(SessionInfo sessioninfo,
            out string strError)
        {
            strError = "";
            if (sessioninfo == null)
            {
                strError = "sessioninfo = null";
                return -1;
            }

            if (sessioninfo.Account == null)
            {
                strError = "sessioninfo.Account = null";
                return -1;
            }

            if (sessioninfo.Account.Type != "reader")
            {
                strError = "sessioninfo.Account.Type != \"reader\"";
                return -1;
            }

            if (sessioninfo.Account.ReaderDomChanged == false)
                return 0;

            XmlDocument readerdom = sessioninfo.Account.ReaderDom;

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

#if NO
            int nRedoCount = 0;
            REDOSAVE:
#endif
            byte[] output_timestamp = null;
            string strOutputPath = "";
            string strOutputXml = "";

            long lRet = 0;

                /*
                // ������߼�¼
                lRet = channel.DoSaveTextRes(sessioninfo.Account.ReaderDomPath,
                    readerdom.OuterXml,
                    false,
                    "content",
                    sessioninfo.Account.ReaderDomTimestamp,   // timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                */
                string strExistingXml = "";
                DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue kernel_errorcode = ErrorCodeValue.NoError;
                LibraryServerResult result = this.SetReaderInfo(sessioninfo,
                    "change",
                    sessioninfo.Account.ReaderDomPath,
                    readerdom.OuterXml,
                    "", // sessioninfo.Account.ReaderDomOldXml,    // strOldXml
                    sessioninfo.Account.ReaderDomTimestamp,
                    out strExistingXml,
                    out strOutputXml,
                    out strOutputPath,
                    out output_timestamp,
                    out kernel_errorcode);
                strError = result.ErrorInfo;
                lRet = result.Value;




            if (lRet == -1)
            {
                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                    || kernel_errorcode == ErrorCodeValue.TimestampMismatch)
                    return -2;

#if NO
                // TODO: ���Բ�Ӧ��������������Ӧ���ڵ���������
                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                    && nRedoCount < 10)
                {
                    // ����<preference>Ԫ��innerxml
                    string strPreferenceInnerXml = "";
                    XmlNode preference = readerdom.DocumentElement.SelectSingleNode("preference");
                    if (preference != null)
                        strPreferenceInnerXml = preference.InnerXml;

                    // ���»�ö��߼�¼
                    // return:
                    //      -2  ��ǰ��¼���û�����reader����
                    //      -1  ����
                    //      0   ��δ��¼
                    //      1   �ɹ�
                    int nRet = GetLoginReaderDom(sessioninfo,
                        out readerdom,
                        out strError);
                    if (nRet != 1)
                    {
                        strError = "������߼�¼ʱ����ʱ�����ͻ�����»�ȡ���߼�¼ʱ�ֳ���: " + strError;
                        return -1;
                    }

                    // �޸�<preference>Ԫ��
                    if (String.IsNullOrEmpty(strPreferenceInnerXml) == false)
                    {
                        preference = readerdom.DocumentElement.SelectSingleNode("preference");
                        if (preference == null)
                        {
                            preference = readerdom.CreateElement("preference");
                            readerdom.DocumentElement.AppendChild(preference);
                        }

                        preference.InnerXml = strPreferenceInnerXml;
                    }

                    // ���±���
                    nRedoCount++;
                    goto REDOSAVE;
                }
#endif

                return -1;
            }

            int nRet = LibraryApplication.LoadToDom(strOutputXml,
                out readerdom,
                out strError);
            if (nRet == -1)
            {
                strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                return -1;
            }
            sessioninfo.Account.ReaderDom = readerdom;
            RefreshReaderAccount(ref sessioninfo.Account, readerdom);

            sessioninfo.Account.ReaderDomChanged = false;
            sessioninfo.Account.ReaderDomTimestamp = output_timestamp;

            return 1;
            /*
        ERROR1:
            return -1;
             * */
        }

        // ����Ա��ݱ����޸ĺ�Ķ��߼�¼DOM
        // return:
        //      -2  ʱ�����ͻ
        //      -1  error
        //      0   û�б�Ҫ����(changed��־Ϊfalse)
        //      1   �ɹ�����
        public int SaveOtherReaderDom(SessionInfo sessioninfo,
            out string strError)
        {
            strError = "";
            if (sessioninfo == null)
            {
                strError = "sessioninfo = null";
                return -1;
            }

            if (sessioninfo.Account == null)
            {
                strError = "sessioninfo.Account = null";
                return -1;
            }

            if (sessioninfo.Account.Type == "reader")
            {
                strError = "sessioninfo.Account.Type == \"reader\"�������ǹ�����Ա���";
                return -1;
            }

            if (sessioninfo.Account.ReaderDomChanged == false)
                return 0;

            XmlDocument readerdom = sessioninfo.Account.ReaderDom;

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

#if NO
            int nRedoCount = 0;
        REDOSAVE:
#endif
            byte[] output_timestamp = null;
            string strOutputPath = "";
            string strOutputXml = "";

            long lRet = 0;


                /*
                // ������߼�¼
                lRet = channel.DoSaveTextRes(sessioninfo.Account.ReaderDomPath,
                    readerdom.OuterXml,
                    false,
                    "content",
                    sessioninfo.Account.ReaderDomTimestamp,   // timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                 * */
                string strExistingXml = "";
                DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue kernel_errorcode = ErrorCodeValue.NoError;
                LibraryServerResult result = this.SetReaderInfo(sessioninfo,
                    "change",
                    sessioninfo.Account.ReaderDomPath,
                    readerdom.OuterXml,
                    "", // sessioninfo.Account.ReaderDomOldXml,    // strOldXml
                    sessioninfo.Account.ReaderDomTimestamp,
                    out strExistingXml,
                    out strOutputXml,
                    out strOutputPath,
                    out output_timestamp,
                    out kernel_errorcode);
                strError = result.ErrorInfo;
                lRet = result.Value;


            if (lRet == -1)
            {
                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                    || kernel_errorcode == ErrorCodeValue.TimestampMismatch)
                    return -2;
#if NO
                // TODO: ���Բ�Ӧ��������������Ӧ���ڵ���������

                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                    && nRedoCount < 10)
                {
                    // ����<preference>Ԫ��innerxml
                    string strPreferenceInnerXml = "";
                    XmlNode preference = readerdom.DocumentElement.SelectSingleNode("preference");
                    if (preference != null)
                        strPreferenceInnerXml = preference.InnerXml;

                    // ���»�ö��߼�¼
                    // return:
                    //      -2  ��ǰ��¼���û�����reader����
                    //      -1  ����
                    //      0   ��δ��¼
                    //      1   �ɹ�
                    int nRet = GetLoginReaderDom(sessioninfo,
                        out readerdom,
                        out strError);
                    if (nRet != 1)
                    {
                        strError = "������߼�¼ʱ����ʱ�����ͻ�����»�ȡ���߼�¼ʱ�ֳ���: " + strError;
                        return -1;
                    }

                    // �޸�<preference>Ԫ��
                    if (String.IsNullOrEmpty(strPreferenceInnerXml) == false)
                    {
                        preference = readerdom.DocumentElement.SelectSingleNode("preference");
                        if (preference == null)
                        {
                            preference = readerdom.CreateElement("preference");
                            readerdom.DocumentElement.AppendChild(preference);
                        }

                        preference.InnerXml = strPreferenceInnerXml;
                    }

                    // ���±���
                    nRedoCount++;
                    goto REDOSAVE;
                }
#endif

                return -1;
            }

            int nRet = LibraryApplication.LoadToDom(strOutputXml,
    out readerdom,
    out strError);
            if (nRet == -1)
            {
                strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                return -1;
            }
            sessioninfo.Account.ReaderDom = readerdom;
            RefreshReaderAccount(ref sessioninfo.Account, readerdom);

            sessioninfo.Account.ReaderDomChanged = false;
            sessioninfo.Account.ReaderDomTimestamp = output_timestamp;

            return 1;
            /*
        ERROR1:
            return -1;
             * */
        }


        // ��õ�ǰsession���Ѿ���¼�Ķ��߼�¼DOM
        // return:
        //      -2  ��ǰ��¼���û�����reader����
        //      -1  ����
        //      0   ��δ��¼
        //      1   �ɹ�
        public int GetLoginReaderDom(SessionInfo sessioninfo,
            out XmlDocument readerdom,
            out string strError)
        {
            strError = "";
            readerdom = null;

            if (sessioninfo == null)
            {
                strError = "sessioninfo == null";
                goto ERROR1;
            }

            if (sessioninfo.Account == null)
            {
                strError = "��δ��¼";
                return 0;
            }

            if (sessioninfo.Account.Type != "reader")
            {
                strError = "��ǰ��¼���û����Ƕ�������";
                return -2;
            }

            // ���������readerdom�Ƿ�ʧЧ
            TimeSpan delta = DateTime.Now - sessioninfo.Account.ReaderDomLastTime;
            if (delta.TotalSeconds > 60
                && sessioninfo.Account.ReaderDomChanged == false)
            {
                sessioninfo.Account.ReaderDom = null;
            }

            if (sessioninfo.Account.ReaderDom == null)
            {
                string strBarcode = "";

                strBarcode = sessioninfo.Account.Barcode;
                if (strBarcode == "")
                {
                    strError = "�ʻ���Ϣ�ж���֤�����Ϊ�գ��޷���λ���߼�¼��";
                    goto ERROR1;
                }

                string strXml = "";
                string strOutputPath = "";
                byte[] timestamp = null;
                // ��ö��߼�¼
                int nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
                    strBarcode,
                    out strXml,
                    out strOutputPath,
                    out timestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nRet == 0)
                    goto ERROR1;

                readerdom = new XmlDocument();

                try
                {
                    readerdom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "װ�ض���XML��¼����DOMʱ����: " + ex.Message;
                    goto ERROR1;
                }

                sessioninfo.Account.ReaderDomPath = strOutputPath;
                sessioninfo.Account.ReaderDomTimestamp = timestamp;
                sessioninfo.Account.ReaderDom = readerdom;
                sessioninfo.Account.ReaderDomLastTime = DateTime.Now;
            }
            else
            {
                readerdom = sessioninfo.Account.ReaderDom;  // ����cache�е�
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ����Ա����ض�֤����ŵĶ��߼�¼DOM
        // return:
        //      -2  ��ǰ��¼���û�����librarian����
        //      -1  ����
        //      0   ��δ��¼
        //      1   �ɹ�
        public int GetOtherReaderDom(SessionInfo sessioninfo,
            string strReaderBarcode,
            out XmlDocument readerdom,
            out string strError)
        {
            strError = "";
            readerdom = null;

            if (sessioninfo == null)
            {
                strError = "sessioninfo == null";
                goto ERROR1;
            }

            if (sessioninfo.Account == null)
            {
                strError = "��δ��¼";
                return 0;
            }

            if (sessioninfo.Account.Type == "reader")
            {
                strError = "��ǰ��¼���û����ǹ�����Ա����";
                return -2;
            }

            // ���������readerdom�Ƿ�ʧЧ
            TimeSpan delta = DateTime.Now - sessioninfo.Account.ReaderDomLastTime;
            if (delta.TotalSeconds > 60
                && sessioninfo.Account.ReaderDomChanged == false)
            {
                sessioninfo.Account.ReaderDom = null;
            }

            if (sessioninfo.Account.ReaderDom == null
                || sessioninfo.Account.ReaderDomBarcode != strReaderBarcode)
            {
                string strBarcode = "";

                strBarcode = strReaderBarcode;
                if (strBarcode == "")
                {
                    strError = "strReaderBarcode����Ϊ�գ��޷���λ���߼�¼��";
                    goto ERROR1;
                }

                string strXml = "";
                string strOutputPath = "";
                byte[] timestamp = null;
                // ��ö��߼�¼
                int nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
                    strBarcode,
                    out strXml,
                    out strOutputPath,
                    out timestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nRet == 0)
                    goto ERROR1;

                readerdom = new XmlDocument();

                try
                {
                    readerdom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "װ�ض���XML��¼����DOMʱ����: " + ex.Message;
                    goto ERROR1;
                }

                sessioninfo.Account.ReaderDomBarcode = strReaderBarcode;
                sessioninfo.Account.ReaderDomPath = strOutputPath;
                sessioninfo.Account.ReaderDomTimestamp = timestamp;
                sessioninfo.Account.ReaderDom = readerdom;
                sessioninfo.Account.ReaderDomLastTime = DateTime.Now;
            }
            else
            {
                Debug.Assert(strReaderBarcode == sessioninfo.Account.ReaderDomBarcode, "");
                readerdom = sessioninfo.Account.ReaderDom;  // ����cache�е�
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ��֤�������롣������ͨ�������ʱ����
        // parameters:
        //      bTempPassword   [out] �Ƿ�Ϊ��ʱ����ƥ��ɹ�
        // return:
        //      -1  error
        //      0   ���벻��ȷ
        //      1   ������ȷ
        public static int VerifyReaderPassword(
            string strClientIP,
            XmlDocument readerdom,
            string strPassword,
            DateTime now,
            out bool bTempPassword,
            out string strError)
        {
            bTempPassword = false;
            int nRet = VerifyReaderNormalPassword(
                strClientIP,
                readerdom,
                strPassword,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 1)
                return 1;
            nRet = VerifyReaderTempPassword(readerdom,
                strPassword,
                now,
                out strError);
            if (nRet == 1)
                bTempPassword = true;
            return nRet;
        }

        // ��֤�������롣������ͨ�������ʱ���룬���� token
        // return:
        //      -1  error
        //      0   ���벻��ȷ
        //      1   ������ȷ
        public static int VerifyReaderPassword(
            string strClientIP,
            XmlDocument readerdom,
            string strPassword,
            DateTime now,
            out string strError)
        {
            int nRet = VerifyReaderNormalPassword(
                strClientIP,
                readerdom,
                strPassword,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 1)
                return 1;
            return VerifyReaderTempPassword(readerdom,
                strPassword,
                now,
                out strError);
        }

        // ��֤������ͨ������� token
        // return:
        //      -1  error
        //      0   ���벻��ȷ
        //      1   ������ȷ
        public static int VerifyReaderNormalPassword(
            string strClientIP,
            XmlDocument readerdom,
            string strPassword,
            out string strError)
        {
            strError = "";
            // int nRet = 0;

            // ��֤����
            string strSha1Text = DomUtil.GetElementText(readerdom.DocumentElement,
                "password");

            if (StringUtil.HasHead(strPassword, "token:") == true)
            {
                string strToken = strPassword.Substring("token:".Length);
                return VerifyToken(
                    strClientIP,
                    strToken,
                    strSha1Text,
                    out strError);
            }

            // ������߼�¼�������Ŀ�����
            if (String.IsNullOrEmpty(strSha1Text) == true)
            {
                if (strPassword != strSha1Text)
                {
                    strError = "���벻��ȷ";
                    return 0;
                }

                return 1;
            }


            try
            {
                strPassword = Cryptography.GetSHA1(strPassword);
            }
            catch
            {
                strError = "�ڲ�����";
                return -1;
            }

            if (strPassword != strSha1Text)
            {
                strError = "���벻��ȷ";
                return 0;
            }

            return 1;
        }

        // ��֤������ʱ����
        // parameters:
        //      now ��ǰʱ�䡣����ʱ��
        // return:
        //      -1  error
        //      0   ���벻��ȷ
        //      1   ������ȷ
        public static int VerifyReaderTempPassword(
            XmlDocument readerdom,
            string strPassword,
            DateTime now,
            out string strError)
        {
            strError = "";

            XmlNode node = readerdom.DocumentElement.SelectSingleNode("password");
            if (node == null)
                return 0;

            // ʧЧ��
            string strExpireTime = DomUtil.GetAttr(node,
                "tempPasswordExpire");
            if (string.IsNullOrEmpty(strExpireTime) == true)
                return 0;   // ������ʹ��ʧЧ��

            try
            {
                DateTime expire = DateTimeUtil.FromRfc1123DateTimeString(strExpireTime).ToLocalTime();

                if (now > expire)
                {
                    // ��ʱ�����Ѿ�ʧЧ
                    return 0;
                }
            }
            catch (Exception )
            {
                strError = "��ʱ����ʧЧ��ʱ���ַ��� '" + strExpireTime + "' ��ʽ����ȷ��ӦΪ RFC1123 ��ʽ";
                return -1;
            }

            // ��֤����
            string strSha1Text = DomUtil.GetAttr(node,
                "tempPassword");

            // ������߼�¼�������Ŀ�����
            if (String.IsNullOrEmpty(strSha1Text) == true)
            {
                if (strPassword != strSha1Text)
                {
                    strError = "���벻��ȷ";
                    return 0;
                }

                return 1;
            }

            try
            {
                strPassword = Cryptography.GetSHA1(strPassword);
            }
            catch
            {
                strError = "�ڲ�����";
                return -1;
            }

            if (strPassword != strSha1Text)
            {
                strError = "���벻��ȷ";
                return 0;
            }

            return 1;
        }

        // �޸Ķ�������
        // return:
        //      -1  error
        //      0   �ɹ�
        public static int ChangeReaderPassword(
            XmlDocument readerdom,
            string strNewPassword,
            ref XmlDocument domOperLog,
            out string strError)
        {
            strError = "";

            try
            {
                strNewPassword = Cryptography.GetSHA1(strNewPassword);
            }
            catch
            {
                strError = "�ڲ�����";
                return -1;
            }

            XmlNode node = DomUtil.SetElementText(readerdom.DocumentElement,
                "password", strNewPassword);
            // 2013/11/2
            if (node != null)
            {
                // ������ʱ����
                DomUtil.SetAttr(node, "tempPassword", null);
                // ��ʧЧ�ڲ����
            }

            if (domOperLog != null)
            {
                Debug.Assert(domOperLog.DocumentElement != null, "");

                // ����־�б������SHA1��̬�����롣�������Է�ֹ����й¶��
                // ����־�ָ��׶�, ���������ֱ��д����߼�¼����, ����Ҫ�ټӹ�
                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "newPassword", strNewPassword);
            }

            return 0;
        }

        // �޸Ķ�����ʱ����
        // return:
        //      -1  error
        //      0   �ɹ�
        public static int ChangeReaderTempPassword(
            XmlDocument readerdom,
            string strTempPassword,
            string strExpireTime,
            ref XmlDocument domOperLog,
            out string strError)
        {
            strError = "";

            try
            {
                strTempPassword = Cryptography.GetSHA1(strTempPassword);
            }
            catch
            {
                strError = "�ڲ�����";
                return -1;
            }

            XmlNode node = readerdom.DocumentElement.SelectSingleNode("password");
            if (node == null)
            {
                node = readerdom.CreateElement("password");
                readerdom.DocumentElement.AppendChild(node);
            }

            DomUtil.SetAttr(node,
                "tempPassword", strTempPassword);
            DomUtil.SetAttr(node,
                "tempPasswordExpire", strExpireTime);

            // ����־�б������SHA1��̬�����롣�������Է�ֹ����й¶��
            // ����־�ָ��׶�, ���������ֱ��д����߼�¼����, ����Ҫ�ټӹ�
            DomUtil.SetElementText(domOperLog.DocumentElement,
                "tempPassword", strTempPassword);
            DomUtil.SetElementText(domOperLog.DocumentElement,
                "tempPasswordExpire", strExpireTime);

            return 0;
        }

        #region ʵ�ù���

        // ͨ��������ŵ�֪�������ּ�¼·��
        // parameters:
        //      strItemBarcode  �������
        //      strReaderBarcodeParam ������֤����š�����������ظ���ʱ�򸽼��жϡ�
        // return:
        //      -1  error
        //      0   ���¼û���ҵ�(strError����˵����Ϣ)
        //      1   �ҵ�
        public int GetBiblioRecPath(
            SessionInfo sessioninfo,
            string strItemBarcode,
            string strReaderBarcode,
            out string strBiblioRecPath,
            out string strWarning,
            out string strError)
        {
            strError = "";
            strWarning = "";
            strBiblioRecPath = "";
            int nRet = 0;

            string strOutputItemPath = "";
            string strItemXml = "";
            byte[] item_timestamp = null;

            int nResultCount = 0;

            // strItemBarcode����״̬ 2006/12/24 
            if (strItemBarcode[0] == '@')
            {
                // ��ò��¼��ͨ�����¼·��

                string strLead = "@path:";
                if (strItemBarcode.Length <= strLead.Length)
                {
                    strError = "����ļ����ʸ�ʽ: '" + strItemBarcode + "'";
                    return -1;
                }
                string strPart = strItemBarcode.Substring(0, strLead.Length);

                if (strPart != strLead)
                {
                    strError = "��֧�ֵļ����ʸ�ʽ: '" + strItemBarcode + "'��Ŀǰ��֧��'@path:'�����ļ�����";
                    return -1;
                }

                string strItemRecPath = strItemBarcode.Substring(strLead.Length);

                {
                    string strItemDbName0 = ResPath.GetDbName(strItemRecPath);
                    // ��Ҫ���һ�����ݿ����Ƿ��������ʵ�����֮��
                    if (this.IsItemDbName(strItemDbName0) == false)
                    {
                        strError = "���¼·�� '" + strItemRecPath + "' �е����ݿ��� '" + strItemDbName0 + "' �������õ�ʵ�����֮�У���˾ܾ�������";
                        return -1;
                    }
                }

                string strMetaData = "";
                // string strTempOutputPath = "";

                RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    return -1;
                }

                long lRet = channel.GetRes(strItemRecPath,
                    out strItemXml,
                    out strMetaData,
                    out item_timestamp,
                    out strOutputItemPath,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��ȡ���¼ " + strItemRecPath + " ʱ��������: " + strError;
                    return -1;
                }
            }
            else // ��ͨ�����
            {

                List<string> aPath = null;

                // ��ò��¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                nRet = this.GetItemRecXml(
                    sessioninfo.Channels,
                    strItemBarcode,
                    out strItemXml,
                    100,
                    out aPath,
                    out item_timestamp,
                    out strError);
                if (nRet == 0)
                {
                    strError = "�������Ϊ '" + strItemBarcode + "' �Ĳ��¼û���ҵ�";
                    return 0;
                }
                if (nRet == -1)
                    return -1;

                if (aPath.Count > 1)
                {
                    // bItemBarcodeDup = true; // ��ʱ�Ѿ���Ҫ����״̬����Ȼ������Խ�һ��ʶ��������Ĳ��¼

                    // ����strDupBarcodeList
                    /*
                    string[] pathlist = new string[aPath.Count];
                    aPath.CopyTo(pathlist);
                    string strDupPathList = String.Join(",", pathlist);
                     * */
                    string strDupPathList = StringUtil.MakePathList(aPath);

                    List<string> aFoundPath = null;
                    List<byte[]> aTimestamp = null;
                    List<string> aItemXml = null;

                    if (String.IsNullOrEmpty(strReaderBarcode) == true)
                    {
                        // ���û�и�������֤����Ų���
                        /*
                        strError = "�������Ϊ '" + strItemBarcode + "' ���¼�� " + aPath.Count.ToString() + " �����޷���λ���¼��";

                        return -1;
                         * */
                        strOutputItemPath = aPath[0];
                        nResultCount = aPath.Count;
                        strWarning = "�������Ϊ '" + strItemBarcode + "' ���¼�� " + aPath.Count.ToString() + " ��(��¼·��Ϊ " + strDupPathList + " )����û���ṩ������Ϣ������£��޷�׼ȷ��λ���¼����Ȩ��ȡ�����еĵ�һ��(" + aPath[0] + ")��";
                        goto GET_BIBLIO;
                    }

                    RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "get channel error";
                        return -1;
                    }

                    // �������ظ�����ŵĲ��¼�У�ѡ�����з��ϵ�ǰ����֤����ŵ�
                    // return:
                    //      -1  ����
                    //      ����    ѡ��������
                    nRet = FindItem(
                        channel,
                        strReaderBarcode,
                        aPath,
                        true,   // �Ż�
                        out aFoundPath,
                        out aItemXml,
                        out aTimestamp,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ѡ���ظ�����ŵĲ��¼ʱ��������: " + strError;
                        return -1;
                    }

                    if (nRet == 0)
                    {
                        strError = "������� '" + strItemBarcode + "' �������� " + aPath.Count + " ����¼(��¼·��Ϊ " + strDupPathList + " )�У�û���κ�һ����<borrower>Ԫ�ر����˱����� '" + strReaderBarcode + "' ���ġ�";
                        return -1;
                    }

                    if (nRet > 1)
                    {
                        /*
                        string[] pathlist1 = new string[aFoundPath.Count];
                        aFoundPath.CopyTo(pathlist1);
                        string strDupPathList1 = String.Join(",", pathlist1);
                         * */
                        string strDupPathList1 = StringUtil.MakePathList(aFoundPath);

                        strError = "�������Ϊ '" + strItemBarcode + "' ����<borrower>Ԫ�ر���Ϊ���� '" + strReaderBarcode + "' ���ĵĲ��¼�� " + aFoundPath.Count.ToString() + " ��(��¼·��Ϊ " + strDupPathList1 + " )���޷���λ���¼��";
                        return -1;
                    }

                    Debug.Assert(nRet == 1, "");

                    strOutputItemPath = aFoundPath[0];
                    item_timestamp = aTimestamp[0];
                    strItemXml = aItemXml[0];

                }
                else
                {
                    Debug.Assert(nRet == 1, "");
                    Debug.Assert(aPath.Count == 1, "");
                    if (nRet == 1)
                    {
                        strOutputItemPath = aPath[0];
                        nResultCount = 1;
                        // strItemXml�Ѿ��в��¼��
                    }
                }
            }

            GET_BIBLIO:

            string strItemDbName = "";  // ʵ�����
            string strBiblioRecID = ""; // �ּ�¼id

            // �����Ҫ�Ӳ��¼�л���ּ�¼·��

            /*
            // ׼������: ӳ�����ݿ���
            nRet = this.GetGlobalCfg(sessioninfo.Channels,
                out strError);
            if (nRet == -1)
                return -1;
             * */

            strItemDbName = ResPath.GetDbName(strOutputItemPath);
            string strBiblioDbName = "";

            // �����Ӧ������ʱ�����ˣ�
            // ����ʵ�����, �ҵ���Ӧ����Ŀ����
            // return:
            //      -1  ����
            //      0   û���ҵ�
            //      1   �ҵ�
            nRet = this.GetBiblioDbNameByItemDbName(strItemDbName,
                out strBiblioDbName,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "ʵ����� '" + strItemDbName + "' û���ҵ���Ӧ����Ŀ����";
                return -1;
            }

            // ��ò��¼�е�<parent>�ֶ�
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strItemXml);
            }
            catch (Exception ex)
            {
                strError = "���¼XMLװ�ص�DOM����:" + ex.Message;
                return -1;
            }

            strBiblioRecID = DomUtil.GetElementText(dom.DocumentElement, "parent"); //
            if (String.IsNullOrEmpty(strBiblioRecID) == true)
            {
                strError = "���¼XML��<parent>Ԫ��ȱ������ֵΪ��, ����޷���λ�ּ�¼";
                return -1;
            }

            strBiblioRecPath = strBiblioDbName + "/" + strBiblioRecID;

            return nResultCount;
        }

        // ͨ����ע��¼·����֪�������ּ�¼·��
        // parameters:
        // return:
        //      -1  error
        //      0   ��ע��¼û���ҵ�(strError����˵����Ϣ)
        //      1   �ҵ�
        public int GetBiblioRecPathByCommentRecPath(
            SessionInfo sessioninfo,
            string strCommentRecPath,
            out string strBiblioRecPath,
            out string strError)
        {
            strError = "";
            strBiblioRecPath = "";
            int nRet = 0;

            string strOutputItemPath = "";
            string strItemXml = "";
            byte[] item_timestamp = null;

            {
                string strCommentDbName0 = ResPath.GetDbName(strCommentRecPath);
                // ��Ҫ���һ�����ݿ����Ƿ��������ʵ�����֮��
                if (this.IsCommentDbName(strCommentDbName0) == false)
                {
                    strError = "��ע��¼·�� '" + strCommentRecPath + "' �е����ݿ��� '" + strCommentDbName0 + "' �������õ���ע����֮�У���˾ܾ�������";
                    return -1;
                }
            }

            string strMetaData = "";
            // string strTempOutputPath = "";

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            long lRet = channel.GetRes(strCommentRecPath,
                out strItemXml,
                out strMetaData,
                out item_timestamp,
                out strOutputItemPath,
                out strError);
            if (lRet == -1)
            {
                strError = "��ȡ��ע��¼ " + strCommentRecPath + " ʱ��������: " + strError;
                return -1;
            }

            string strCommentDbName = "";  // ʵ�����
            string strBiblioRecID = ""; // �ּ�¼id

            // �����Ҫ����ע��¼�л���ּ�¼·��
            strCommentDbName = ResPath.GetDbName(strOutputItemPath);
            string strBiblioDbName = "";

            // �����Ӧ������ʱ�����ˣ�
            // ����ʵ�����, �ҵ���Ӧ����Ŀ����
            // return:
            //      -1  ����
            //      0   û���ҵ�
            //      1   �ҵ�
            nRet = this.GetBiblioDbNameByCommentDbName(strCommentDbName,
                out strBiblioDbName,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "��ע���� '" + strCommentDbName + "' û���ҵ���Ӧ����Ŀ����";
                return -1;
            }

            // �����ע��¼�е�<parent>�ֶ�
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strItemXml);
            }
            catch (Exception ex)
            {
                strError = "��ע��¼XMLװ�ص�DOM����:" + ex.Message;
                return -1;
            }

            strBiblioRecID = DomUtil.GetElementText(dom.DocumentElement, "parent"); //
            if (String.IsNullOrEmpty(strBiblioRecID) == true)
            {
                strError = "��ע��¼XML��<parent>Ԫ��ȱ������ֵΪ��, ����޷���λ�ּ�¼";
                return -1;
            }

            strBiblioRecPath = strBiblioDbName + "/" + strBiblioRecID;
            return 1;
        }

        // 2011/9/5
        // ͨ�����¼·����parentid��֪�������ּ�¼·��
        // parameters:
        // return:
        //      -1  error
        //      1   �ҵ�
        public int GetBiblioRecPathByItemRecPath(
            string strItemRecPath,
            string strParentID,
            out string strBiblioRecPath,
            out string strError)
        {
            strError = "";
            strBiblioRecPath = "";
            int nRet = 0;


            {
                string strItemDbName0 = ResPath.GetDbName(strItemRecPath);
                // ��Ҫ���һ�����ݿ����Ƿ��������ʵ�����֮��
                if (this.IsItemDbName(strItemDbName0) == false)
                {
                    strError = "���¼·�� '" + strItemRecPath + "' �е����ݿ��� '" + strItemDbName0 + "' �������õ�ʵ�����֮�У���˾ܾ�������";
                    return -1;
                }
            }

            string strItemDbName = "";  // ʵ�����

            // �����Ҫ�Ӳ��¼�л���ּ�¼·��
            strItemDbName = ResPath.GetDbName(strItemRecPath);
            string strBiblioDbName = "";

            // �����Ӧ������ʱ�����ˣ�
            // ����ʵ�����, �ҵ���Ӧ����Ŀ����
            // return:
            //      -1  ����
            //      0   û���ҵ�
            //      1   �ҵ�
            nRet = this.GetBiblioDbNameByItemDbName(strItemDbName,
                out strBiblioDbName,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "ʵ����� '" + strItemDbName + "' û���ҵ���Ӧ����Ŀ����";
                return -1;
            }

            strBiblioRecPath = strBiblioDbName + "/" + strParentID;
            return 1;
        }

        // ͨ�����¼·����֪�������ּ�¼·��
        // parameters:
        // return:
        //      -1  error
        //      0   ���¼û���ҵ�(strError����˵����Ϣ)
        //      1   �ҵ�
        public int GetBiblioRecPathByItemRecPath(
            SessionInfo sessioninfo,
            string strItemRecPath,
            out string strBiblioRecPath,
            out string strError)
        {
            strError = "";
            strBiblioRecPath = "";
            int nRet = 0;

            string strOutputItemPath = "";
            string strItemXml = "";
            byte[] item_timestamp = null;

            {
                string strItemDbName0 = ResPath.GetDbName(strItemRecPath);
                // ��Ҫ���һ�����ݿ����Ƿ��������ʵ�����֮��
                if (this.IsItemDbName(strItemDbName0) == false)
                {
                    strError = "���¼·�� '" + strItemRecPath + "' �е����ݿ��� '" + strItemDbName0 + "' �������õ�ʵ�����֮�У���˾ܾ�������";
                    return -1;
                }
            }

            string strMetaData = "";
            // string strTempOutputPath = "";

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            long lRet = channel.GetRes(strItemRecPath,
                out strItemXml,
                out strMetaData,
                out item_timestamp,
                out strOutputItemPath,
                out strError);
            if (lRet == -1)
            {
                strError = "��ȡ��ע��¼ " + strItemRecPath + " ʱ��������: " + strError;
                return -1;
            }

            string strItemDbName = "";  // ʵ�����
            string strBiblioRecID = ""; // �ּ�¼id

            // �����Ҫ�Ӳ��¼�л���ּ�¼·��
            strItemDbName = ResPath.GetDbName(strOutputItemPath);
            string strBiblioDbName = "";

            // �����Ӧ������ʱ�����ˣ�
            // ����ʵ�����, �ҵ���Ӧ����Ŀ����
            // return:
            //      -1  ����
            //      0   û���ҵ�
            //      1   �ҵ�
            nRet = this.GetBiblioDbNameByItemDbName(strItemDbName,
                out strBiblioDbName,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "ʵ����� '" + strItemDbName + "' û���ҵ���Ӧ����Ŀ����";
                return -1;
            }

            // ��ò��¼�е�<parent>�ֶ�
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strItemXml);
            }
            catch (Exception ex)
            {
                strError = "ʵ���¼XMLװ�ص�DOM����:" + ex.Message;
                return -1;
            }

            strBiblioRecID = DomUtil.GetElementText(dom.DocumentElement, "parent"); //
            if (String.IsNullOrEmpty(strBiblioRecID) == true)
            {
                strError = "ʵ���¼XML��<parent>Ԫ��ȱ������ֵΪ��, ����޷���λ�ּ�¼";
                return -1;
            }

            strBiblioRecPath = strBiblioDbName + "/" + strBiblioRecID;
            return 1;
        }

        #endregion

        // ��װ�汾
                // ���·���еĿ������ǲ���ʵ�����
        // return:
        //      -1  error
        //      0   ����ʵ�����
        //      1   ��ʵ�����
        public int CheckItemRecPath(string strItemRecPath,
            out string strError)
        {
            return CheckRecPath(strItemRecPath,
                "item",
                out strError);
        }

        // ���·���еĿ������ǲ����ض����͵����ݿ���
        // return:
        //      -1  error
        //      0   ������Ҫ�����͵�
        //      1   ��Ҫ�����͵�
        public int CheckRecPath(string strItemRecPath,
            string strDbTypeList,
            out string strError)
        {
            strError = "";

            string strTempDbName = ResPath.GetDbName(strItemRecPath);

            // 2008/10/16 
            if (String.IsNullOrEmpty(strTempDbName) == true)
            {
                strError = "��·�� '" + strItemRecPath + "' ���޷������������...";
                return -1;
            }

            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                // item
                if (strTempDbName == this.ItemDbs[i].DbName)
                {
                    if (StringUtil.IsInList("item", strDbTypeList) == true)
                        return 1;
                }

                // order
                if (strTempDbName == this.ItemDbs[i].OrderDbName)
                {
                    if (StringUtil.IsInList("order", strDbTypeList) == true)
                        return 1;
                }

                // issue
                if (strTempDbName == this.ItemDbs[i].IssueDbName)
                {
                    if (StringUtil.IsInList("issue", strDbTypeList) == true)
                        return 1;
                }

                // comment
                if (strTempDbName == this.ItemDbs[i].CommentDbName)
                {
                    if (StringUtil.IsInList("comment", strDbTypeList) == true)
                        return 1;
                }

                // biblio
                if (strTempDbName == this.ItemDbs[i].BiblioDbName)
                {
                    if (StringUtil.IsInList("biblio", strDbTypeList) == true)
                        return 1;
                }
            }
            strError = "·�� '" + strItemRecPath + "' �а��������ݿ��� '" + strTempDbName + "' �����Ѷ�������� "+strDbTypeList+" ����֮�С�";
            return 0;
        }

        #region APIs



        // �����ļ����ǲ�����.cs��β
        public static bool IsCsFileName(string strFileName)
        {
            strFileName = strFileName.Trim().ToLower();
            int nRet = strFileName.LastIndexOf(".cs");
            if (nRet == -1)
                return false;
            if (nRet + 3 == strFileName.Length)
                return true;
            return false;
        }



#if NOOOOOOOOOOOOOO

        // ��û����
        // ���ַ���ĩβ׷��һ���µĲ���������
        // ������1:��������1<����ʱ��1>;������2:��������2<����ʱ��2>;...
        // parameters:
        //      strOpertimeRfc1123 ����ʱ�䡣����ΪRFC1123��̬�����Ϊnull����ʾ�Զ�ȡ��ǰʱ��
        public static int AppendOperatorHistory(ref string strValue,
            string strOperator,
            string strOperation,
            string strOpertimeRfc1123,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strOperator) == true)
            {
                strError = "strOperator��������Ϊ��";
                return -1;
            }

            if (String.IsNullOrEmpty(strOperation) == true)
            {
                strError = "strOperation��������Ϊ��";
                return -1;
            }

            if (String.IsNullOrEmpty(strValue) == true)
                strValue = "";
            else
                strValue += ";";

            strValue += strOperator;
            strValue += ":";
            strValue += strOperation;
            strValue += "<";
            if (String.IsNullOrEmpty(strOpertimeRfc1123) == true)
            {
                strValue += DateTimeUtil.Rfc1123DateTimeString(? this.Clock().UtcNow /*DateTime.UtcNow*/);
            }
            else
            {
                strValue += strOpertimeRfc1123;
            }
            strValue += ">";

            return 0;
        }
#endif


        // �޸Ķ�������
        // Result.Value -1���� 0�����벻��ȷ 1��������ȷ,���޸�Ϊ������
        // Ȩ��: 
        //		������Ա���߶��ߣ�������changereaderpasswordȨ��
        //		���Ϊ����, �������ƻ�ֻ���޸������Լ�������
        public LibraryServerResult ChangeReaderPassword(
            SessionInfo sessioninfo,
            string strReaderBarcode,
            string strReaderOldPassword,
            string strReaderNewPassword)
        {
            LibraryServerResult result = new LibraryServerResult();

            // Ȩ���ж�

            // Ȩ���ַ���
            if (StringUtil.IsInList("changereaderpassword", sessioninfo.RightsOrigin) == false)
            {
                result.Value = -1;
                result.ErrorInfo = "�޸Ķ������뱻�ܾ������߱�changereaderpasswordȨ�ޡ�";
                result.ErrorCode = ErrorCode.AccessDenied;
                return result;
            }

            // �Զ�����ݵĸ����ж�
            if (sessioninfo.UserType == "reader")
            {
                if (strReaderBarcode != sessioninfo.Account.Barcode)
                {
                    result.Value = -1;
                    result.ErrorInfo = "�޸Ķ������뱻�ܾ�����Ϊ����ֻ���޸��Լ�������";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }

            string strError = "";

            // �Ӷ��߼�¼��
            this.ReaderLocks.LockForWrite(strReaderBarcode);

            try
            {
                string strXml = "";
                string strOutputPath = "";
                byte[] timestamp = null;

                // ��ö��߼�¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                int nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
                    strReaderBarcode,
                    out strXml,
                    out strOutputPath,
                    out timestamp,
                    out strError);
                if (nRet == 0)
                {
                    strError = "֤�����Ϊ '" + strReaderBarcode + "' �Ķ��߲�����";
                    goto ERROR1;
                }
                if (nRet == -1)
                {
                    strError = "���֤�����Ϊ '" + strReaderBarcode + "' �Ķ��߼�¼ʱ����: " + strError;
                    goto ERROR1;
                }

                if (nRet > 1)
                {
                    strError = "ϵͳ����: ֤�����Ϊ '" + strReaderBarcode + "' �Ķ��߼�¼����һ��";
                    goto ERROR1;
                }

                string strLibraryCode = "";

                // �������߼�¼�������Ķ��߿�Ĺݴ��룬�Ƿ񱻵�ǰ�û���Ͻ
                if (String.IsNullOrEmpty(strOutputPath) == false)
                {
                    // ��鵱ǰ�������Ƿ��Ͻ������߿�
                    // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                    if (this.IsCurrentChangeableReaderPath(strOutputPath,
            sessioninfo.LibraryCodeList,
            out strLibraryCode) == false)
                    {
                        strError = "���߼�¼·�� '" + strOutputPath + "' �����Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                        goto ERROR1;
                    }
                }

                XmlDocument readerdom = null;
                nRet = LibraryApplication.LoadToDom(strXml,
                    out readerdom,
                    out strError);
                if (nRet == -1)
                {
                    strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                    goto ERROR1;
                }

                string strExistingBarcode = DomUtil.GetElementText(readerdom.DocumentElement, "barcode");

                // ����Ƕ������, ��Ҫ��֤������
                if (sessioninfo.UserType == "reader")
                {
                    // ��֤��������
                    // return:
                    //      -1  error
                    //      0   ���벻��ȷ
                    //      1   ������ȷ
                    nRet = LibraryApplication.VerifyReaderPassword(
                        sessioninfo.ClientIP,
                        readerdom,
                        strReaderOldPassword,
                        this.Clock.Now,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 0)
                    {
                        result.Value = 0;
                        result.ErrorInfo = "�����벻��ȷ��";
                        return result;
                    }
                    else
                    {
                        result.Value = 1;
                    }
                }

                byte[] output_timestamp = null;
                nRet = ChangeReaderPassword(
                    sessioninfo,
                    strOutputPath,
                    ref readerdom,
                    strReaderNewPassword,
                    timestamp,
                    out output_timestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ��� LoginCache
                this.ClearLoginCache(strExistingBarcode);

                result.Value = 1;   // �ɹ�
            }
            finally
            {
                this.ReaderLocks.UnlockForWrite(strReaderBarcode);
            }

            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        // �޸Ķ�������
        // parameters:
        //      readerdom [in,out] ���߼�¼ XMLDOM�����ܻ���Ϊʱ�����ƥ���������װ��
        int ChangeReaderPassword(
            SessionInfo sessioninfo,
            string strReaderRecPath,
            ref XmlDocument readerdom,
            string strReaderNewPassword,
            byte [] timestamp,
            out byte [] output_timestamp,
            out string strError)
        {
            strError = "";
            output_timestamp = null;

            int nRet = 0;

            string strLibraryCode = "";

            // ��ö��߿�Ĺݴ���
            // return:
            //      -1  ����
            //      0   �ɹ�
            nRet = GetLibraryCode(
                strReaderRecPath,
                out strLibraryCode,
                out strError);
            if (nRet == -1)
                return -1;

            // ׼����־DOM
            XmlDocument domOperLog = new XmlDocument();
            domOperLog.LoadXml("<root />");
            DomUtil.SetElementText(domOperLog.DocumentElement,
"libraryCode",
strLibraryCode);    // �������ڵĹݴ���
            DomUtil.SetElementText(domOperLog.DocumentElement, "operation",
                "changeReaderPassword");

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            int nRedoCount = 0;
            REDO:

            // �޸Ķ�������
            // return:
            //      -1  error
            //      0   �ɹ�
            nRet = LibraryApplication.ChangeReaderPassword(
                readerdom,
                strReaderNewPassword,
                ref domOperLog,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // byte[] output_timestamp = null;
            string strOutputPath = "";

            // ������߼�¼
            long lRet = channel.DoSaveTextRes(strReaderRecPath,
                readerdom.OuterXml,
                false,
                "content", // "content,ignorechecktimestamp",
                timestamp,   // timestamp,
                out output_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                    && nRedoCount < 10)
                {
                    // ����װ�ض��߼�¼
                    string strXml = "";
                    string strMetaData = "";
                    timestamp = null;

                    lRet = channel.GetRes(strReaderRecPath,
                        out strXml,
                        out strMetaData,
                        out timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "������߼�¼ '" + strReaderRecPath + "' ʱ����ʱ�����ƥ�䣬����װ�ص�ʱ������������: " + strError;
                        goto ERROR1;
                    }

                    readerdom = new XmlDocument();
                    try
                    {
                        readerdom.LoadXml(strXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "����װ�ض��߼�¼���� XMLDOM ʱ����: " +ex.Message;
                        goto ERROR1;
                    }
                    nRedoCount++;
                    goto REDO;
                }
                goto ERROR1;
            }

            // д����־
            string strReaderBarcode = DomUtil.GetElementText(domOperLog.DocumentElement, "barcode");

            // ����֤�����
            DomUtil.SetElementText(domOperLog.DocumentElement,
                "readerBarcode", strReaderBarcode);

            // ���߼�¼
            XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                "readerRecord",
                readerdom.OuterXml);
            // ���߼�¼·��
            DomUtil.SetAttr(node, "recPath", strOutputPath);

            string strOperTime = this.Clock.GetClock();
            DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                sessioninfo.UserID);   // ������
            DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                strOperTime);   // ����ʱ��

            nRet = this.OperLog.WriteOperLog(domOperLog,
                sessioninfo.ClientAddress,
                out strError);
            if (nRet == -1)
            {
                strError = "ChangeReaderPassword() API д����־ʱ��������: " + strError;
                goto ERROR1;
            }

            return 0;
        ERROR1:
            return -1;
        }

        // �޸Ķ�����ʱ����
        // parameters:
        //      timeExpire  ��ʱ����ʧЧʱ��
        //      readerdom [in,out] ���߼�¼ XMLDOM�����ܻ���Ϊʱ�����ƥ���������װ��
        int ChangeReaderTempPassword(
            SessionInfo sessioninfo,
            string strReaderRecPath,
            XmlDocument readerdom,
            string strReaderTempPassword,
            string strExpireTime,
            byte[] timestamp,
            out byte[] output_timestamp,
            out string strError)
        {
            strError = "";
            output_timestamp = null;

            int nRet = 0;

            string strLibraryCode = "";

            // ��ö��߿�Ĺݴ���
            // return:
            //      -1  ����
            //      0   �ɹ�
            nRet = GetLibraryCode(
                strReaderRecPath,
                out strLibraryCode,
                out strError);
            if (nRet == -1)
                return -1;

            // ׼����־DOM
            XmlDocument domOperLog = new XmlDocument();
            domOperLog.LoadXml("<root />");
            DomUtil.SetElementText(domOperLog.DocumentElement,
"libraryCode",
strLibraryCode);    // �������ڵĹݴ���

            DomUtil.SetElementText(domOperLog.DocumentElement, "operation",
                "changeReaderTempPassword");

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            int nRedoCount = 0;
        REDO:

            // �޸Ķ�����ʱ����
            // return:
            //      -1  error
            //      0   �ɹ�
            nRet = LibraryApplication.ChangeReaderTempPassword(
                readerdom,
                strReaderTempPassword,
                strExpireTime,
                ref domOperLog,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // byte[] output_timestamp = null;
            string strOutputPath = "";

            // ������߼�¼
            long lRet = channel.DoSaveTextRes(strReaderRecPath,
                readerdom.OuterXml,
                false,
                "content", // "content,ignorechecktimestamp",
                timestamp,   // timestamp,
                out output_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
    && nRedoCount < 10)
                {
                    // ����װ�ض��߼�¼
                    string strXml = "";
                    string strMetaData = "";
                    timestamp = null;

                    lRet = channel.GetRes(strReaderRecPath,
                        out strXml,
                        out strMetaData,
                        out timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "������߼�¼ '" + strReaderRecPath + "' ʱ����ʱ�����ƥ�䣬����װ�ص�ʱ������������: " + strError;
                        goto ERROR1;
                    }

                    readerdom = new XmlDocument();
                    try
                    {
                        readerdom.LoadXml(strXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "����װ�ض��߼�¼���� XMLDOM ʱ����: " + ex.Message;
                        goto ERROR1;
                    }
                    nRedoCount++;
                    goto REDO;
                }

                goto ERROR1;
            }

            // д����־
            string strReaderBarcode = DomUtil.GetElementText(domOperLog.DocumentElement, "barcode");

            // ����֤�����
            DomUtil.SetElementText(domOperLog.DocumentElement,
                "readerBarcode", strReaderBarcode);

            // ���߼�¼
            XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                "readerRecord",
                readerdom.OuterXml);

            // ���߼�¼·��
            DomUtil.SetAttr(node, "recPath", strOutputPath);

            string strOperTime = this.Clock.GetClock();
            DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                sessioninfo.UserID);   // ������
            DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                strOperTime);   // ����ʱ��

            nRet = this.OperLog.WriteOperLog(domOperLog,
                sessioninfo.ClientAddress,
                out strError);
            if (nRet == -1)
            {
                strError = "ChangeReaderPassword() API д����־ʱ��������: " + strError;
                goto ERROR1;
            }

            // this.LoginCache.Remove(strReaderBarcode);   // ��ʱʧЧ��¼����
            this.ClearLoginCache(strReaderBarcode);   // ��ʱʧЧ��¼����

            return 0;
        ERROR1:
            return -1;
        }

        #endregion

        // չ��Ȩ���ַ���ΪԭʼȨ�޶�����̬
        public static string ExpandRightString(string strOriginRight)
        {
            string strResult = strOriginRight;

            return strResult;
        }

        // ��װ�汾
        public string GetBarcodesSummary(SessionInfo sessioninfo,
            string strBarcodes,
            string strStyle,
            string strOtherParams)
        {
            return GetBarcodesSummary(
            sessioninfo,
            strBarcodes,
            "",
            strStyle,
            strOtherParams);
        }

        // ���һϵ�в��ժҪ�ַ���
        // �������㱾��WebControl�İ汾
        // paramters:
        //      strStyle    ��񡣶��ż�����б�html text
        //      strOtherParams  ��ʱû��ʹ��
        public string GetBarcodesSummary(
            SessionInfo sessioninfo,
            string strBarcodes,
            string strArrivedItemBarcode,
            string strStyle,
            string strOtherParams)
        {
            string strSummary = "";

            if (strOtherParams == null)
                strOtherParams = "";

            string strDisableClass = "";
            if (string.IsNullOrEmpty(strArrivedItemBarcode) == false)
                strDisableClass = "deleted";

            string strPrevBiblioRecPath = "";
            string[] barcodes = strBarcodes.Split(new char[] { ',' });
            for (int j = 0; j < barcodes.Length; j++)
            {
                string strBarcode = barcodes[j];
                if (String.IsNullOrEmpty(strBarcode) == true)
                    continue;

                // ���ժҪ
                string strOneSummary = "";
                string strBiblioRecPath = "";

                // 2012/3/28
                RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                if (channel == null)
                {
                    return "get channel error";
                }

                LibraryServerResult result = this.GetBiblioSummary(sessioninfo,
                    channel,
    strBarcode,
    null,
    strPrevBiblioRecPath,   // ǰһ��path
    out strBiblioRecPath,
    out strOneSummary);
                if (result.Value == -1 || result.Value == 0)
                    strOneSummary = result.ErrorInfo;

                if (strOneSummary == ""
                    && strPrevBiblioRecPath == strBiblioRecPath)
                    strOneSummary = "(ͬ��)";

                if (StringUtil.IsInList("html", strStyle) == true)
                {
                    /*
                    string strBarcodeLink = "<a href='" + this.OpacServerUrl + "/book.aspx?barcode=" + strBarcode +
                        (bForceLogin == true ? "&forcelogin=userid" : "")
                        + "' " + strOtherParams + " >" + strBarcode + "</a>";
                    */

                    string strBarcodeLink = "<a "
    + (string.IsNullOrEmpty(strDisableClass) == false && strBarcode != strArrivedItemBarcode ? "class='" + strDisableClass + "'" : "")
    + " href='javascript:void(0);' onclick=\"window.external.OpenForm('ItemInfoForm', this.innerText, true);\"  onmouseover=\"window.external.HoverItemProperty(this.innerText);\">" + strBarcode + "</a>";


                    strSummary += strBarcodeLink + " : " + strOneSummary + "<br/>";
                }
                else
                {
                    strSummary += strBarcode + " : " + strOneSummary + "<br/>";
                }

                strPrevBiblioRecPath = strBiblioRecPath;
            }

            return strSummary;
        }

#if NO
        // ���һϵ�в��ժҪ�ַ���
        // 
        // paramters:
        //      strStyle    ��񡣶��ż�����б��������html text��ʾ��ʽ��forcelogin
        //      strOtherParams  <a>����������Ĳ���������" target='_blank' "�����������´���
        public string GetBarcodesSummary(
            SessionInfo sessioninfo,
            string strBarcodes,
            string strArrivedItemBarcode,
            string strStyle,
            string strOtherParams)
        {
            string strSummary = "";

            if (strOtherParams == null)
                strOtherParams = "";

            string strDisableClass = "";
            if (string.IsNullOrEmpty(strArrivedItemBarcode) == false)
                strDisableClass = "deleted";

            bool bForceLogin = false;
            if (StringUtil.IsInList("forcelogin", strStyle) == true)
                bForceLogin = true;

            string strPrevBiblioRecPath = "";
            string[] barcodes = strBarcodes.Split(new char[] { ',' });
            for (int j = 0; j < barcodes.Length; j++)
            {
                string strBarcode = barcodes[j];
                if (String.IsNullOrEmpty(strBarcode) == true)
                    continue;

                // ���ժҪ
                string strOneSummary = "";
                string strBiblioRecPath = "";

                LibraryServerResult result = this.GetBiblioSummary(sessioninfo,
    strBarcode,
    null,
    strPrevBiblioRecPath,   // ǰһ��path
    out strBiblioRecPath,
    out strOneSummary);
                if (result.Value == -1 || result.Value == 0)
                    strOneSummary = result.ErrorInfo;

                if (strOneSummary == ""
                    && strPrevBiblioRecPath == strBiblioRecPath)
                    strOneSummary = "(ͬ��)";

                if (StringUtil.IsInList("html", strStyle) == true)
                {
                    /*
                    string strBarcodeLink = "<a href='" + this.OpacServerUrl + "/book.aspx?barcode=" + strBarcode +
                        (bForceLogin == true ? "&forcelogin=userid" : "")
                        + "' " + strOtherParams + " >" + strBarcode + "</a>";
                    */

                    string strBarcodeLink = "<a "
    + (string.IsNullOrEmpty(strDisableClass) == false && strBarcode != strArrivedItemBarcode ? "class='" + strDisableClass + "'" : "")
    + " href='" + this.OpacServerUrl + "/book.aspx?barcode=" + strBarcode +
    (bForceLogin == true ? "&forcelogin=userid" : "")
    + "' " + strOtherParams + " >" + strBarcode + "</a>";


                    strSummary += strBarcodeLink + " : " + strOneSummary + "<br/>";
                }
                else
                {
                    strSummary += strBarcode + " : " + strOneSummary + "<br/>";
                }

                strPrevBiblioRecPath = strBiblioRecPath;
            }

            return strSummary;
        }

#endif

        static List<XmlNode> MatchTableNodes(XmlNode root,
            string strName,
            string strDbName)
        {
            List<XmlNode> results = new List<XmlNode>();

            XmlNodeList nodes = root.SelectNodes("table[@name='" + strName + "']");
            if (nodes.Count == 0)
                return results;

            for (int i = 0; i < nodes.Count; i++)
            {
                string strCurDbName = DomUtil.GetAttr(nodes[i], "dbname");
                if (String.IsNullOrEmpty(strCurDbName) == true
                    && String.IsNullOrEmpty(strDbName) == true)
                {
                    results.Add(nodes[i]);
                    continue;
                }

                if (strCurDbName == strDbName)
                    results.Add(nodes[i]);
            }

            return results;
        }

        // TODO: ��Ҫ������Էֹ��û��ĸ���
        // �޸�ֵ�б�
        // 2008/8/21 
        // parameters:
        //      strAction   "new" "change" "overwirte" "delete"
        // return:
        //      -1  error
        //      0   not change
        //      1   changed
        public int SetValueTable(string strAction,
            string strName,
            string strDbName,
            string strValue,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strName) == true)
            {
                strError = "strName����ֵ����Ϊ��";
                return -1;
            }
            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//valueTables");
            if (root == null)
            {
                root = this.LibraryCfgDom.CreateElement("valueTables");
                this.LibraryCfgDom.DocumentElement.AppendChild(root);
                this.Changed = true;
            }

            if (strAction == "new")
            {
                List<XmlNode> nodes = MatchTableNodes(root,
                    strName,
                    strDbName);
                if (nodes.Count > 0)
                {
                    strError = "nameΪ '"+strName+"' dbnameΪ '"+strDbName+"' ��ֵ�б������Ѿ�����";
                    return -1;
                }

                XmlNode new_node = root.OwnerDocument.CreateElement("table");
                root.AppendChild(new_node);

                DomUtil.SetAttr(new_node, "name", strName);
                DomUtil.SetAttr(new_node, "dbname", strDbName);

                new_node.InnerText = strValue;
                this.Changed = true;
                return 1;
            }
            else if (strAction == "delete")
            {
                List<XmlNode> nodes = MatchTableNodes(root,
                    strName,
                    strDbName);
                if (nodes.Count == 0)
                {
                    strError = "nameΪ '" + strName + "' dbnameΪ '" + strDbName + "' ��ֵ�б��������";
                    return 0;
                }

                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].ParentNode.RemoveChild(nodes[i]);
                }

                this.Changed = true;
                return 1;
            }
            else if (strAction == "change")
            {
                List<XmlNode> nodes = MatchTableNodes(root,
                    strName,
                    strDbName);
                if (nodes.Count == 0)
                {
                    strError = "nameΪ '" + strName + "' dbnameΪ '" + strDbName + "' ��ֵ�б��������";
                    return 0;
                }

                XmlNode exist_node = nodes[0];
                for (int i = 1; i < nodes.Count; i++)
                {
                    nodes[i].ParentNode.RemoveChild(nodes[i]);
                }

                exist_node.InnerText = strValue;
                this.Changed = true;
                return 1;
            }
            else if (strAction == "overwrite")
            {
                List<XmlNode> nodes = MatchTableNodes(root,
                    strName,
                    strDbName);
                if (nodes.Count == 0)
                {
                    XmlNode new_node = root.OwnerDocument.CreateElement("table");
                    root.AppendChild(new_node);

                    DomUtil.SetAttr(new_node, "name", strName);
                    DomUtil.SetAttr(new_node, "dbname", strDbName);

                    new_node.InnerText = strValue;
                }
                else
                {
                    XmlNode exist_node = nodes[0];
                    for (int i = 1; i < nodes.Count; i++)
                    {
                        nodes[i].ParentNode.RemoveChild(nodes[i]);
                    }

                    exist_node.InnerText = strValue;
                }
                this.Changed = true;
                return 1;
            }
            else
            {
                strError = "δ֪��strActionֵ '" + strAction + "'";
                return -1;
            }
        }

        // ���ַ����б��У����˳���Щ����ָ���ݴ��뷶Χ���ַ���
        static string[] FilterValues(string strLibraryCodeList,
            string strValueList)
        {
            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == true)
            {
                return strValueList.Trim().Split(new char[] { ',' });
            }

            List<string> results = new List<string>();
            List<string> values = StringUtil.FromListString(strValueList);
            foreach (string s in values)
            {
                string strLibraryCode = "";
                string strPureName = "";

                // ����������
                ParseCalendarName(s,
            out strLibraryCode,
            out strPureName);

                if (string.IsNullOrEmpty(strLibraryCode) == true)
                    continue;

                if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == true)
                    results.Add(s);
            }

            if (results.Count == 0)
                return new string [0];

            string [] array = new string[results.Count];
            results.CopyTo(array);
            return array;
        }

#if NO
        // ���ֵ�б�
        // parameters:
        //      strLibraryCodeList  ��ǰ�û���Ͻ�Ĺݴ����б�
        //      strTableName    ���������Ϊ�գ���ʾ����name����ֵ��ƥ��
        //      strDbName   ���ݿ��������Ϊ�գ���ʾ����dbname����ֵ��ƥ�䡣
        public string[] GetValueTable(
            string strLibraryCodeList,
            string strTableNameParam,
            string strDbNameParam)
        {
            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//valueTables");
            if (root == null)
                return null;

            if (strTableNameParam == "location")
            {
            }
            else
            {
                // ������
                strLibraryCodeList = "";
            }

            // 2009/2/15 changed
            if (String.IsNullOrEmpty(strDbNameParam) == false)
            {
                XmlNode default_node = null;

                XmlNodeList nodes = root.SelectNodes("table[@name='" + strTableNameParam + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode table = nodes[i];

                    // string strName = DomUtil.GetAttr(table, "name");
                    string strDbName = DomUtil.GetAttr(table, "dbname");

                    if (String.IsNullOrEmpty(strDbName) == true
                        && default_node == null)    // ����������ǰ���һ��ȱʡԪ��
                    {
                        default_node = table;
                        continue;
                    }


                    if (StringUtil.IsInList(strDbNameParam, strDbName) == true)
                    {
                        // ����
                        // return table.InnerText.Trim().Split(new char[] { ',' });
                                // ���ַ����б��У����˳���Щ����ָ���ݴ��뷶Χ���ַ���
                        return FilterValues(strLibraryCodeList,
                                table.InnerText);
                    }
                }

                // ��Ȼ"dbname"û�����У����ǿ��Է���ȱʡ��ֵ(dbname����Ϊ�յ�)
                if (default_node != null)
                {
                    // return default_node.InnerText.Trim().Split(new char[] { ',' });
                    return FilterValues(strLibraryCodeList,
        default_node.InnerText);
                }

                return null;
            }
            else
            {
                // û��dbname����������
                XmlNodeList nodes = root.SelectNodes("table[@name='" + strTableNameParam + "']");
                if (nodes.Count == 0)
                    return null;
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode table = nodes[i];

                    // string strName = DomUtil.GetAttr(table, "name");
                    string strDbName = DomUtil.GetAttr(table, "dbname");

                    // ����ѡ��һ��dbname����Ϊ�յ�Ԫ��
                    if (String.IsNullOrEmpty(strDbName) == true)
                    {
                        // ����
                        // return table.InnerText.Trim().Split(new char[] { ',' });
                        return FilterValues(strLibraryCodeList,
                            table.InnerText);
                    }
                }

                // ���򷵻ء�û���ҵ��������ܻ�������dbname������ֵ��Ԫ��
                return null;
            }


            /*
            XmlNodeList nodes = root.SelectNodes("table");

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode table = nodes[i];

                string strName = DomUtil.GetAttr(table, "name");
                string strDbName = DomUtil.GetAttr(table, "dbname");

                if (String.IsNullOrEmpty(strTableNameParam) == false)
                {
                    if (String.IsNullOrEmpty(strName) == false
                        && strTableNameParam != strName)
                        continue;
                }
                if (String.IsNullOrEmpty(strDbNameParam) == false)
                {
                    if (String.IsNullOrEmpty(strDbName) == false
                        && strDbNameParam != strDbName)
                        continue;
                }

                // ����
                string strValue = table.InnerText.Trim();
                return strValue.Split(new char[] {','});
            }
             * */

            // return null;    // not found
        }
#endif
        // ���һ��ͼ��ݴ����µ�ֵ�б�
        // parameters:
        //      strLibraryCode  �ݴ���
        //      strTableName    ���������Ϊ�գ���ʾ����name����ֵ��ƥ��
        //      strDbName   ���ݿ��������Ϊ�գ���ʾ����dbname����ֵ��ƥ�䡣
        public List<string> GetOneLibraryValueTable(
            string strLibraryCode,
            string strTableNameParam,
            string strDbNameParam)
        {
            List<string> results = new List<string>();

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//valueTables");
            if (root == null)
                return results;

            string strFilter = "";

            if (string.IsNullOrEmpty(strLibraryCode) == false)
            {
                XmlNode temp = root.SelectSingleNode("descendant::library[@code='" + strLibraryCode + "']");
                if (temp == null)
                    return results;
                root = temp;
            }
            else
            {
                // TODO: �����һ�����ϵ�<library>Ԫ�أ�����Ҫ���Ƴ�һ���µ�DOM��Ȼ���<library>Ԫ��ȫ��ɾ���ɾ�
                strFilter = "[count(ancestor::library) = 0]";
            }

            // 2009/2/15 changed
            if (String.IsNullOrEmpty(strDbNameParam) == false)
            {
                XmlNode default_node = null;

                XmlNodeList nodes = root.SelectNodes("table[@name='" + strTableNameParam + "']" + strFilter);
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode table = nodes[i];

                    string strDbName = DomUtil.GetAttr(table, "dbname");

                    if (String.IsNullOrEmpty(strDbName) == true
                        && default_node == null)    // ����������ǰ���һ��ȱʡԪ��
                    {
                        default_node = table;
                        continue;
                    }

                    if (StringUtil.IsInList(strDbNameParam, strDbName) == true)
                    {
                        // ����
                        return StringUtil.FromListString(table.InnerText.Trim(), ',', false);   // Ҫ���ؿ��ַ�����Ա
                    }
                }

                // ��Ȼ"dbname"û�����У����ǿ��Է���ȱʡ��ֵ(dbname����Ϊ�յ�)
                if (default_node != null)
                {
                    return StringUtil.FromListString(default_node.InnerText.Trim(), ',', false);   // Ҫ���ؿ��ַ�����Ա
                }

                return results;
            }
            else
            {
                // û��dbname����������
                XmlNodeList nodes = root.SelectNodes("table[@name='" + strTableNameParam + "']" + strFilter);
                if (nodes.Count == 0)
                    return results; // return null;
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode table = nodes[i];

                    // string strName = DomUtil.GetAttr(table, "name");
                    string strDbName = DomUtil.GetAttr(table, "dbname");

                    // ����ѡ��һ��dbname����Ϊ�յ�Ԫ��
                    if (String.IsNullOrEmpty(strDbName) == true)
                    {
                        // ����
                        return StringUtil.FromListString(table.InnerText.Trim(), ',', false);   // Ҫ���ؿ��ַ�����Ա
                    }
                }

                // ���򷵻ء�û���ҵ��������ܻ�������dbname������ֵ��Ԫ��
                return results;
            }
        }

        // 2014/9/7
        // ��ֵ�б���� {} ����
        static List<string> ConvertValueList(string strLibraryCode,
            List<string> values)
        {
            Debug.Assert(values != null, "");

            if (string.IsNullOrEmpty(strLibraryCode) == true)
                return values;

            List<string> results = new List<string>();
            foreach (string s in values)
            {
                if (s.IndexOf('{') == -1)
                    results.Add("{" + strLibraryCode + "} " + s);
                else
                    results.Add(s); // ����������� {} ���֣��Ͳ��������
            }

            return results;
        }

        // ���ֵ�б�
        // parameters:
        //      strLibraryCodeList  ��ǰ�û���Ͻ�Ĺݴ����б�
        //      strTableName    ���������Ϊ�գ���ʾ����name����ֵ��ƥ��
        //      strDbName   ���ݿ��������Ϊ�գ���ʾ����dbname����ֵ��ƥ�䡣
        public string[] GetValueTable(
            string strLibraryCodeList,
            string strTableNameParam,
            string strDbNameParam)
        {
            List<string> librarycodes = new List<string>();
            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == true)
            {
#if NO
                // ��õ�ǰ<valueTables>Ԫ��������<library>Ԫ���е�ͼ��ݴ���
                XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("valueTables/library");
                foreach (XmlNode node in nodes)
                {
                    string strLibraryCode = DomUtil.GetAttr(node, "code");
                    if (string.IsNullOrEmpty(strLibraryCode) == true)
                        continue;
                    librarycodes.Add(strLibraryCode);
                }
                librarycodes.Insert(0, "");
#endif
                // ��õ�ǰ<readerdbgroup>Ԫ��������<database>Ԫ���е�ͼ��ݴ���
                XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("readerdbgroup/database");
                foreach (XmlNode node in nodes)
                {
                    string strLibraryCode = DomUtil.GetAttr(node, "libraryCode");
                    if (string.IsNullOrEmpty(strLibraryCode) == true)
                        continue;
                    librarycodes.Add(strLibraryCode);
                }
                librarycodes.Insert(0, "");
            }
            else
            {
                librarycodes = StringUtil.FromListString(strLibraryCodeList);
            }

            List<string> results = new List<string>();
            foreach (string strLibraryCode in librarycodes)
            {
                List<string> temp = GetOneLibraryValueTable(
                    strLibraryCode,
                    strTableNameParam,
                    strDbNameParam);


                // ���û���ҵ�
                if (temp == null || temp.Count == 0)
                {
                    if (strTableNameParam == "location")
                    {
                        // ��Ϊ�� <locationTypes> ��Ѱ��
                        temp = GetOneLibraryLocationValueList(strLibraryCode);
                    }
                    else if (strTableNameParam == "bookType"
                        || strTableNameParam == "readerType")
                    {
                        // ��Ϊ�� <rightsTable>Ԫ���µ�<readerTypes>��<bookTypes> ��Ѱ��
                        temp = GetOneLibraryBookReaderTypeValueList(strLibraryCode,
                            strTableNameParam);
                    }
                }

                // ���� {} ����
                if (strTableNameParam != "location"
                    && temp != null)
                {
                    temp = ConvertValueList(strLibraryCode, temp);
                }

                if (temp == null || temp.Count == 0)
                    continue;

                results.AddRange(temp);
            }

            if (results.Count == 0)
                return new string[0];

            StringUtil.RemoveDupNoSort(ref results);

            string[] array = new string[results.Count];
            results.CopyTo(array);
            return array;
        }

#if NO
        // �� <locationTypes> Ԫ���л��ֵ�б�
        public string[] GetLocationValueList(string strLibraryCodeList)
        {
            List<string> librarycodes = new List<string>();
            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == true)
            {
#if NOOO
                // ��õ�ǰ<valueTables>Ԫ��������<library>Ԫ���е�ͼ��ݴ���
                XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("valueTables/library");
                foreach (XmlNode node in nodes)
                {
                    string strLibraryCode = DomUtil.GetAttr(node, "code");
                    if (string.IsNullOrEmpty(strLibraryCode) == true)
                        continue;
                    librarycodes.Add(strLibraryCode);
                }
                librarycodes.Insert(0, "");
#endif
                // ��õ�ǰ<readerdbgroup>Ԫ��������<database>Ԫ���е�ͼ��ݴ���
                XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("readerdbgroup/database");
                foreach (XmlNode node in nodes)
                {
                    string strLibraryCode = DomUtil.GetAttr(node, "libraryCode");
                    if (string.IsNullOrEmpty(strLibraryCode) == true)
                        continue;
                    librarycodes.Add(strLibraryCode);
                }
                librarycodes.Insert(0, "");
            }
            else
            {
                librarycodes = StringUtil.FromListString(strLibraryCodeList);
            }

            List<string> results = new List<string>();
            foreach (string strLibraryCode in librarycodes)
            {

                List<string> temp = GetOneLibraryLocationValueList(
                    strLibraryCode);
                if (temp == null)
                    continue;
                if (temp.Count == 0)
                    continue;
                results.AddRange(temp);
            }

            if (results.Count == 0)
                return new string[0];

            StringUtil.RemoveDupNoSort(ref results);

            string[] array = new string[results.Count];
            results.CopyTo(array);
            return array;
        }
#endif

        // ���һ��ͼ��ݴ����µ� <locationTypes> �� <item> Ԫ��
        // parameters:
        //      strLibraryCode  �ݴ���
        public XmlElement GetLocationItemElement(
            string strLibraryCode,
            string strPureName)
        {
            List<string> results = new List<string>();

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("locationTypes");
            if (root == null)
                return null;

            string strFilter = "";

            if (string.IsNullOrEmpty(strLibraryCode) == false)
            {
                XmlNode temp = root.SelectSingleNode("descendant::library[@code='" + strLibraryCode + "']");
                if (temp == null)
                    return null;
                root = temp;
            }
            else
            {
                // TODO: �����һ�����ϵ�<library>Ԫ�أ�����Ҫ���Ƴ�һ���µ�DOM��Ȼ���<library>Ԫ��ȫ��ɾ���ɾ�
                strFilter = "[count(ancestor::library) = 0]";
            }

            return (XmlElement)root.SelectSingleNode("item[text()='"+strPureName+"']" + strFilter);
        }

        // ���һ��ͼ��ݴ����µ� <locationTypes> �� <item> ֵ�б�
        // parameters:
        //      strLibraryCode  �ݴ���
        public List<string> GetOneLibraryLocationValueList(
            string strLibraryCode)
        {
            List<string> results = new List<string>();

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("locationTypes");
            if (root == null)
                return results;

            string strFilter = "";

            if (string.IsNullOrEmpty(strLibraryCode) == false)
            {
                XmlNode temp = root.SelectSingleNode("descendant::library[@code='" + strLibraryCode + "']");
                if (temp == null)
                    return results;
                root = temp;
            }
            else
            {
                // TODO: �����һ�����ϵ�<library>Ԫ�أ�����Ҫ���Ƴ�һ���µ�DOM��Ȼ���<library>Ԫ��ȫ��ɾ���ɾ�
                strFilter = "[count(ancestor::library) = 0]";
            }

            XmlNodeList nodes = root.SelectNodes("item" + strFilter);
            if (nodes.Count == 0)
                return results; // return null;
            foreach (XmlElement item in nodes)
            {
                string strValue = "";
                if (string.IsNullOrEmpty(strLibraryCode) == true)
                    strValue = item.InnerText.Trim();
                else
                    strValue = strLibraryCode + "/" + item.InnerText.Trim();

                results.Add(strValue);
            }
            return results;
        }

        // ���һ��ͼ��ݴ����µ� <rightsTable>Ԫ���µ�<readerTypes>��<bookTypes> ֵ�б�
        // parameters:
        //      strLibraryCode  �ݴ���
        public List<string> GetOneLibraryBookReaderTypeValueList(
            string strLibraryCode,
            string strTableName)
        {
            List<string> results = new List<string>();

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("rightsTable");
            if (root == null)
                return results;

            string strFilter = "";

            if (string.IsNullOrEmpty(strLibraryCode) == false)
            {
                XmlNode temp = root.SelectSingleNode("descendant::library[@code='" + strLibraryCode + "']");
                if (temp == null)
                    return results;
                root = temp;
            }
            else
            {
                // TODO: �����һ�����ϵ�<library>Ԫ�أ�����Ҫ���Ƴ�һ���µ�DOM��Ȼ���<library>Ԫ��ȫ��ɾ���ɾ�
                strFilter = "[count(ancestor::library) = 0]";
            }

            string strTypesElementName = "bookTypes";
            if (strTableName == "readerType")
                strTypesElementName = "readerTypes";

            Debug.Assert(strTableName == "bookType" || strTableName == "readerType", "");

            XmlNodeList nodes = root.SelectNodes(strTypesElementName + "/item" + strFilter);
            if (nodes.Count == 0)
                return results; // return null;
            foreach (XmlElement item in nodes)
            {
                string strValue = "";
                strValue = item.InnerText.Trim();
                if (strValue == "[��]")
                    strValue = "";
#if NO
                if (string.IsNullOrEmpty(strLibraryCode) == true)
                    strValue = item.InnerText.Trim();
                else
                    strValue = strLibraryCode + "/" + item.InnerText.Trim();
#endif
                results.Add(strValue);
            }
            return results;
        }

        // ���library.xml�����õ�dtlp�ʻ���Ϣ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int GetDtlpAccountInfo(string strPath,
            out string strUserName,
            out string strPassword,
            out string strError)
        {
            strError = "";
            strUserName = "";
            strPassword = "";

            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//traceDTLP");
            if (node == null)
            {
                strError = "��δ����<traceDTLP>";
                return -1;
            }

            // ��·��������������������
            int nRet = strPath.IndexOf("/");
            if (nRet != -1)
                strPath = strPath.Substring(0, nRet);

            node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//traceDTLP/origin[@serverAddr='"+strPath+"']");
            if (node == null)
            {
                strError = "�����ڵ�ַΪ '" + strPath + "' ��DTLP������<origin>���ò���...";
                return 0;
            }

            strUserName = DomUtil.GetAttr(node, "UserName");
            strPassword = DomUtil.GetAttr(node, "Password");

            try
            {
                strPassword = DecryptPassword(strPassword);
            }
            catch
            {
                strPassword = "errorpassword";
            }

            return 1;
        }


        public bool Changed
        {
            get
            {
                return m_bChanged;
            }
            set
            {
                m_bChanged = value;
            }
        }

        // ӳ���ں˽ű������ļ�������
        // return:
        //      -1  error
        //      0   �ɹ���Ϊ.cs�ļ�
        //      1   �ɹ���Ϊ.fltx�ļ�
        public int MapKernelScriptFile(
            SessionInfo sessioninfo,
            string strBiblioDbName,
            string strScriptFileName,
            out string strLocalPath,
            out string strError)
        {
            strError = "";
            strLocalPath = "";
            int nRet = 0;

            // ���ּ�¼���ݴ�XML��ʽת��ΪHTML��ʽ
            // ��Ҫ���ں�ӳ������ļ�
            // string strScriptFileName = "./cfgs/loan_biblio.fltx";
            // ���ű��ļ������滯
            // ��Ϊ�ڶ���ű��ļ���ʱ��, ��һ����ǰ��������,
            // �������Ϊ ./cfgs/filename ��ʾ�ڵ�ǰ���µ�cfgsĿ¼��,
            // ���������Ϊ /cfgs/filename ���ʾ��ͬ�������ĸ���
            string strRemotePath = LibraryApplication.CanonicalizeScriptFileName(
                strBiblioDbName,
                strScriptFileName);

            // TODO: �����Կ���֧��http://�����������ļ���

            nRet = this.CfgsMap.MapFileToLocal(
                sessioninfo.Channels,
                strRemotePath,
                out strLocalPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                strError = "�ں������ļ� " + strRemotePath + "û���ҵ�������޷������Ŀhtml��ʽ����";
                goto ERROR1;
            }

            bool bFltx = false;
            // �����һ��.cs�ļ�, ����Ҫ���.cs.ref�����ļ�
            if (LibraryApplication.IsCsFileName(
                strScriptFileName) == true)
            {
                string strTempPath = "";
                nRet = this.CfgsMap.MapFileToLocal(
                    sessioninfo.Channels,
                    strRemotePath + ".ref",
                    out strTempPath,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�ں������ļ� " + strRemotePath + ".ref" + "û���ҵ�������޷������Ŀhtml��ʽ����";
                    goto ERROR1;
                }

                bFltx = false;
            }
            else
            {
                bFltx = true;
            }


            if (bFltx == true)
                return 1;   // Ϊ.fltx�ļ�

            return 0;

        ERROR1:
            return -1;
        }

        // ���ű��ļ������滯
        // ��Ϊ�ڶ���ű��ļ���ʱ��, ��һ����ǰ��������,
        // �������Ϊ ./cfgs/filename ��ʾ�ڵ�ǰ���µ�cfgsĿ¼��,
        // ���������Ϊ /cfgs/filename ���ʾ��ͬ�������ĸ���
        public static string CanonicalizeScriptFileName(string strDbName,
            string strScriptFileNameParam)
        {
            int nRet = 0;
            nRet = strScriptFileNameParam.IndexOf("./");
            if (nRet == 0)  // != -1   2006/12/24 changed
            {
                // ��Ϊ�ǵ�ǰ����
                return strDbName + strScriptFileNameParam.Substring(1);
            }

            nRet = strScriptFileNameParam.IndexOf("/");
            if (nRet == 0)  // != -1   2006/12/24 changed
            {
                // ��Ϊ�Ӹ���ʼ
                return strScriptFileNameParam.Substring(1);
            }

            return strScriptFileNameParam;  // ����ԭ��
        }

        // reutrn:
        //      -1  error
        //      0   not found start.xml
        //      1   found start.xml
        public static int GetDataDir(string strStartXmlFileName,
            out string strDataDir,
            out string strError)
        {
            strError = "";
            strDataDir = "";

            if (File.Exists(strStartXmlFileName) == false)
            {
                strError = "�ļ� " + strStartXmlFileName + " ������...";
                return 0;
            }

            // �Ѵ���start.xml�ļ�
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.Load(strStartXmlFileName);
            }
            catch (Exception ex)
            {
                strError = "����start.xml��dom����" + ex.Message;
                return -1;
            }

            strDataDir = DomUtil.GetAttr(dom.DocumentElement, "datadir");
            if (strDataDir == "")
            {
                strError = "start.xml�ļ��и�Ԫ��δ����'datadir'���ԣ���'datadir'����ֵΪ�ա�";
                return -1;
            }

            if (Directory.Exists(strDataDir) == false)
            {
                strError = "start.xml�ļ��и�Ԫ��'datadir'���Զ��������Ŀ¼ '" + strDataDir + "' �����ڡ�";
                return -1;
            }

            return 1;
        }

        // ������ֻ���
        public void ClearCache()
        {
            this.m_lockXml2HtmlAssemblyTable.AcquireWriterLock(m_nLockTimeout);
            try
            {
                this.Xml2HtmlAssemblyTable.Clear();
            }
            finally
            {
                this.m_lockXml2HtmlAssemblyTable.ReleaseWriterLock();
            }

            this.Filters.Clear();

        }



        // ����������XML����ʽ
        public static int BuildVirtualQuery(
            Hashtable db_dir_results,
            VirtualDatabase vdb,
            string strWord,
            string strVirtualFromName,
            string strMatchStyle,
            int nMaxCount,
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            int nUsed = 0;

            string strLogic = "OR";

            List<string> realdbs = vdb.GetRealDbNames();

            if (realdbs.Count == 0)
            {
                strError = "����� '" + vdb.GetName(null) + "' �¾�Ȼû�ж����κ������";
                return -1;
            }

            string strWarning = "";

            for (int i = 0; i < realdbs.Count; i++)
            {

                // ���ݿ���
                string strDbName = realdbs[i];


                string strFrom = vdb.GetRealFromName(
                    db_dir_results,
                    strDbName,
                    strVirtualFromName);
                if (strFrom == null)
                {
                    strWarning += "����� '" + vdb.GetName(null) + " '���������� '" + strDbName + "' �ж�����From '" + strVirtualFromName + "' δ�ҵ���Ӧ������From��; ";
                    // strError = "����� '" + vdb.GetName(null) + " '���������� '" + strDbName + "' �ж�����From '" + strVirtualFromName + "' δ�ҵ���Ӧ������From��";
                    // return -1;
                    continue;
                }

                // 2007/4/5 ���� ������ GetXmlStringSimple()
                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strWord)
                    + "</word><match>" + strMatchStyle + "</match><relation>=</relation><dataType>string</dataType>"
                    + "<maxCount>" + nMaxCount.ToString() + "</maxCount></item><lang>zh</lang></target>";

                if (i > 0)
                    strXml += "<operator value='" + strLogic + "'/>";

                strXml += strOneDbQuery;

                nUsed++;
            }

            if (nUsed > 0)
            {
                strXml = "<group>" + strXml + "</group>";
            }

            // һ�������Ҳû��ƥ����
            if (nUsed == 0)
            {
                strError = strWarning;
                return -1;
            }

            return 0;
        }


        // ���ݼ�����������XML����ʽ
        // return:
        //      -1  ����
        //      0   ��������ָ�������ݿ���߼���;����һ����û��
        //      1   �ɹ�
        public static int BuildQueryXml(
            LibraryApplication app,
            string strDbName,
            string strWord,
            string strFrom,
            string strMatchStyle,
            string strRelation,
            string strDataType,
            int nMaxCount,
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            if (app == null)
            {
                strError = "app == null";
                return -1;
            }

            if (app.vdbs == null)
            {
                strError = "app.vdbs == null";
                return -1;
            }

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "strDbName��������Ϊ�ա�";
                return -1;
            }

            if (String.IsNullOrEmpty(strMatchStyle) == true)
                strMatchStyle = "middle";

            if (String.IsNullOrEmpty(strRelation) == true)
                strRelation = "=";

            if (String.IsNullOrEmpty(strDataType) == true)
                strDataType = "string";

            //
            // ���ݿ��ǲ��������?
            VirtualDatabase vdb = app.vdbs[strDbName];  // ��Ҫ����һ��������

            string strOneDbQuery = "";

            // ����������
            if (vdb != null && vdb.IsVirtual == true)
            {
                int nRet = BuildVirtualQuery(
                    app.vdbs.db_dir_results,
                    vdb,
                    strWord,
                    strFrom,
                    strMatchStyle,
                    nMaxCount,
                    out strOneDbQuery,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            else
            {
                /*
                // 2007/4/5 ���� ������ GetXmlStringSimple()
                strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strWord) + "</word><match>" + strMatchStyle + "</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";
                 * */

                string strTargetList = "";

                if (String.IsNullOrEmpty(strDbName) == true
                    || strDbName.ToLower() == "<all>"
                    || strDbName == "<ȫ��>")
                {
                    List<string> found_dup = new List<string>();    // ����ȥ��

                    if (app.vdbs.Count == 0)
                    {
                        strError = "Ŀǰlibrary.xml��<virtualDatabases>����δ���ü���Ŀ��";
                        return 0;
                    }


                    // ��������������ȥ�غ��������� ��ȫ�������� (����ȥ��һ��)
                    // Ҫע�����ض���from������������Ƿ���ڣ�������������ų��ÿ���
                    for (int j = 0; j < app.vdbs.Count; j++)
                    {
                        VirtualDatabase temp_vdb = app.vdbs[j];  // ��Ҫ����һ��������

                        // ���Ծ���notInAll���ԵĿ�
                        if (temp_vdb.NotInAll == true)
                            continue;

                        List<string> realdbs = new List<string>();

                        // if (temp_vdb.IsVirtual == true)
                        realdbs = temp_vdb.GetRealDbNames();

                        for (int k = 0; k < realdbs.Count; k++)
                        {
                            // ���ݿ���
                            string strOneDbName = realdbs[k];

                            if (found_dup.IndexOf(strOneDbName) != -1)
                                continue;

                            strTargetList += StringUtil.GetXmlStringSimple(strOneDbName + ":" + strFrom) + ";";

                            found_dup.Add(strOneDbName);
                        }
                    }
                }
                else if (String.IsNullOrEmpty(strDbName) == true
                    || strDbName.ToLower() == "<all items>"
                    || strDbName == "<ȫ��ʵ��>"
                    || strDbName.ToLower() == "<all comments>"
                    || strDbName == "<ȫ����ע>")
                {
                    if (app.ItemDbs.Count == 0)
                    {
                        strError = "Ŀǰlibrary.xml��<itemdbgroup>����δ�������ݿ�";
                        return -1;
                    }

                    string strDbType = "";
                    if (strDbName.ToLower() == "<all items>"
                    || strDbName == "<ȫ��ʵ��>")
                        strDbType = "item";
                    else if (strDbName.ToLower() == "<all comments>"
                    || strDbName == "<ȫ����ע>")
                        strDbType = "comment";
                    else
                    {
                        Debug.Assert(false, "");
                    }


                    for (int j = 0; j < app.ItemDbs.Count; j++)
                    {
                        ItemDbCfg cfg = app.ItemDbs[j];

                        string strOneDbName = "";
                        
                        if (strDbType == "item")
                            strOneDbName = cfg.DbName;
                        else if (strDbType == "comment")
                            strOneDbName = cfg.CommentDbName;

                        if (String.IsNullOrEmpty(strOneDbName) == true)
                            continue;
                        strTargetList += StringUtil.GetXmlStringSimple(strOneDbName + ":" + strFrom) + ";";
                    }
                }
                else
                {
                    strTargetList = StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom);
                }

                if (String.IsNullOrEmpty(strTargetList) == true)
                {
                    strError = "���߱��κμ���Ŀ��";
                    return 0;
                }

                strOneDbQuery = "<target list='"
                    + strTargetList
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strWord)
                    + "</word><match>"
                    + StringUtil.GetXmlStringSimple(strMatchStyle)
                    + "</match>"
                    + "<relation>"
                    + StringUtil.GetXmlStringSimple(strRelation)
                    + "</relation>"
                    + "<dataType>"
                    + StringUtil.GetXmlStringSimple(strDataType)
                    + "</dataType>"
                    + "<maxCount>" + (-1).ToString() + "</maxCount></item><lang>zh</lang></target>";

            }

            strXml = strOneDbQuery;

            return 1;
        }

#if NOOOOOOOOOOOOOOOOOOOOO
        // ���ݼ�����������XML����ʽ
        public static int BuildQueryXml(
            LibraryApplication app,
            string strDbName,
            string strWord,
            string strFrom,
            string strMatchStyle,
            int nMaxCount,
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            if (app == null)
            {
                strError = "app == null";
                return -1;
            }

            if (app.vdbs == null)
            {
                strError = "app.vdbs == null";
                return -1;
            }

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "strDbName��������Ϊ�ա�";
                return -1;
            }

            //
            // ���ݿ��ǲ��������?
            VirtualDatabase vdb = app.vdbs[strDbName];  // ��Ҫ����һ��������

            if (vdb == null)
            {
                strError = "��Ŀ���� '" + strDbName + "' �����ڡ�";
                return -1;
            }

            string strOneDbQuery = "";

            // ����������
            if (vdb.IsVirtual == true)
            {
                int nRet = BuildVirtualQuery(
                    app.vdbs.db_dir_results,
                    vdb,
                    strWord,
                    strFrom,
                    strMatchStyle,
                    nMaxCount,
                    out strOneDbQuery,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            else
            {
                // 2007/4/5 ���� ������ GetXmlStringSimple()
                strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14 
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strWord) + "</word><match>" + strMatchStyle + "</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";
            }

            strXml = strOneDbQuery;

            return 0;
        }
#endif

        // ��������
        public static string EncryptPassword(string PlainText)
        {
            return Cryptography.Encrypt(PlainText, EncryptKey);
        }

        // ���ܼ��ܹ�������
        public static string DecryptPassword(string EncryptText)
        {
            return Cryptography.Decrypt(EncryptText, EncryptKey);
        }





        // ��õ�ǰȫ�����߿���ʹ�ù��Ĺݴ����б�
        public List<string> GetAllLibraryCode()
        {
            List<string> results = new List<string>();
            bool bBlank = false;    // �Ƿ����ٳ��ֹ�һ�οյĹݴ���
            foreach (ReaderDbCfg item in this.ReaderDbs)
            {
                if (string.IsNullOrEmpty(item.LibraryCode) == true)
                {
                    bBlank = true;
                    continue;
                }
                results.Add(item.LibraryCode);
            }

            if (bBlank == true)
                results.Insert(0, "");

            return results;
        }

        // ��������ļ�Ƭ���������¼�<library>Ԫ�ص�code���ԡ���δȥ��
        public List<string> GetAllLibraryCode(XmlNode root)
        {
            List<string> all_librarycodes = new List<string>();
            XmlNodeList nodes = root.SelectNodes("descendant::library");
            foreach (XmlNode node in nodes)
            {
                string strCode = DomUtil.GetAttr(node, "code");
                if (string.IsNullOrEmpty(strCode) == true)
                    continue;

                all_librarycodes.Add(strCode);
            }
            return all_librarycodes;
        }

                // ���Ȩ�޶����HTML�ַ���
        // parameters:
        //      strSource   ���ܻ����<readerTypes>��<bookTypes>����
        //      strLibraryCodeList  ��ǰ�û���Ͻ�ķֹݴ����б�
        public int GetRightTableHtml(
            string strSource,
            string strLibraryCodeList,
            out string strResult,
            out string strError)
        {
            strError = "";
            strResult = "";

            XmlDocument cfg_dom = null;
            if (String.IsNullOrEmpty(strSource) == true)
                cfg_dom = this.LibraryCfgDom;
            else
            {
                cfg_dom = new XmlDocument();
                try
                {
                    cfg_dom.LoadXml("<rightsTable>" + strSource + "</rightsTable>");
                }
                catch (Exception ex)
                {
                    strError = "strSource����(��Ӹ�Ԫ�غ�)װ��XMLDOMʱ���ִ���: " + ex.Message;
                    return -1;
                }
            }

            List<string> librarycodes = new List<string>();
            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == true)
            {
                // XML�����е�ȫ���ݴ���
                librarycodes = GetAllLibraryCode(cfg_dom.DocumentElement);
                StringUtil.RemoveDupNoSort(ref librarycodes);   // ȥ��

                // ���߿����ù���ȫ���ݴ���
                List<string> temp = GetAllLibraryCode();
                if (temp.Count > 0 && temp[0] == "")
                    librarycodes.Insert(0, "");
            }
            else
            {
                librarycodes = StringUtil.FromListString(strLibraryCodeList);
            }

            return LoanParam.GetRightTableHtml(
                cfg_dom,
                // strLibraryCodeList,
                librarycodes,
                out strResult,
                out strError);
        }



        // ����û�ʹ�� WriteRes API ��Ȩ��
        // ע�� 
        //      writetemplate д��ģ�������ļ� template ����Ҫ��Ȩ��; 
        //      writeobject д���������Ҫ��Ȩ��; 
        //      writerecord д�����ݿ��ļ�����Ҫ��Ȩ��
        //      writeres д�����ݿ��¼�������ļ������������Ҫ����ͳ��Ȩ��
        // parameters:
        //      strLibraryCodeList  ��ǰ�û�����Ͻ�Ĺݴ����б�
        //      strLibraryCode  [out]�����д����߿⣬���ﷵ��ʵ��д��Ķ��߿�Ĺݴ��롣�������д����߿⣬�򷵻ؿ�
        // return:
        //      -1  error
        //      0   ���߱�Ȩ��
        //      1   �߱�Ȩ��
        public int CheckWriteResRights(
            string strLibraryCodeList,
            string strRights,
            string strResPath,
            out string strLibraryCode,
            out string strError)
        {
            strError = "";
            strLibraryCode = "";

            string strPath = strResPath;

            // д�� dp2library �����ļ�
            if (string.IsNullOrEmpty(strPath) == false
                && strPath[0] == '!')
            {
                strPath = strPath.Substring(1);

                string strTargetDir = this.DataDir;
                string strFilePath = Path.Combine(strTargetDir, strPath);

                string strFirstLevel = StringUtil.GetFirstPartPath(ref strPath);
                if (string.Compare(strFirstLevel, "upload", true) != 0)
                {
                    strError = "��һ��Ŀ¼������Ϊ 'upload'";
                    return -1;
                }
                if (StringUtil.IsInList("upload", strRights) == false)
                {
                    strError = "д���ļ� " + strResPath + " ���ܾ������߱� upload Ȩ��";
                    return 0;
                }
                // �����޶��ĸ�Ŀ¼
                string strLimitDir = Path.Combine(strTargetDir, strFirstLevel);
                if (PathUtil.IsChildOrEqual(strFilePath, strLimitDir) == false)
                {
                    strError = "·�� '" + strResPath + "' Խ�����޶��ķ�Χ���޷�����";
                    return 0;
                }
                return 1;
            }

            string strDbName = StringUtil.GetFirstPartPath(ref strPath);

            // ��Ŀ��
            if (this.IsBiblioDbName(strDbName) == true)
            {
                string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

                // cfgs
                if (strFirstPart == "cfgs")
                {
                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    if (strFirstPart == "template")
                    {
                        if (StringUtil.IsInList("writetemplate", strRights) == false)
                        {
                            strError = "д��ģ�������ļ� " + strResPath + " ���ܾ������߱�writetemplateȨ��";
                            return 0;
                        }
                        return 1;   // �������writetemplateȨ�ޣ��Ͳ�����ҪwriteresȨ��
                    }
                }

                // ��¼ID
                if (StringUtil.IsPureNumber(strFirstPart) == true
                    || strFirstPart == "?")
                {
                    // ֻ����¼ID��һ��
                    if (strPath == "")
                    {
                        if (StringUtil.IsInList("writerecord", strRights) == false)
                        {
                            strError = "ֱ��д���¼ " + strResPath + " ���ܾ������߱�writerecordȨ��";
                            return 0;
                        }
                        return 1;   // �������writerecordȨ�ޣ��Ͳ�����ҪwriteresȨ��
                    }

                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

                    // ������Դ
                    if (strFirstPart == "object")
                    {
                        if (StringUtil.IsInList("writeobject", strRights) == false)
                        {
                            strError = "д�������Դ " + strResPath + " ���ܾ������߱�writeobjectȨ��";
                            return 0;
                        }
                        return 1;   // �������writeobjectȨ�ޣ��Ͳ�����ҪwriteresȨ��
                    }
                }

                if (StringUtil.IsInList("writeres", strRights) == false)
                {
                    strError = "д����Դ " + strResPath + " ���ܾ������߱�writeresȨ��";
                    return 0;
                }
            }

            // ���߿�
            if (this.IsReaderDbName(strDbName, out strLibraryCode) == true)
            {
                // 2012/9/22
                if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                {
                    if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                    {
                        strError = "д����Դ " + strResPath + " ���ܾ������߿� '"+strDbName+"' ���ڵ�ǰ�û��Ĺ�Ͻ��Χ '"+strLibraryCodeList+"' ��";
                        return 0;
                    }
                }

                string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

                // cfgs
                if (strFirstPart == "cfgs")
                {
                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    if (strFirstPart == "template")
                    {
                        if (StringUtil.IsInList("writetemplate", strRights) == false)
                        {
                            strError = "д��ģ�������ļ� " + strResPath + " ���ܾ������߱�writetemplateȨ��";
                            return 0;
                        }
                        return 1;   // �������writetemplateȨ�ޣ��Ͳ�����ҪwriteresȨ��
                    }
                }

                // ��¼ID
                if (StringUtil.IsPureNumber(strFirstPart) == true
                    || strFirstPart == "?")
                {
                    // ֻ����¼ID��һ��
                    if (strPath == "")
                    {
                        /*
                        if (StringUtil.IsInList("writerecord", strRights) == false)
                        {
                            strError = "ֱ��д���¼ " + strResPath + " ���ܾ������߱�writerecordȨ�ޡ�";
                            return 0;
                        }
                        return 1;   // �������writerecordȨ�ޣ��Ͳ�����ҪwriteresȨ��
                         * */
                        strError = "������ʹ��WriteRes()д����߿��¼";
                        return 0;
                    }

                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

                    // ������Դ
                    if (strFirstPart == "object")
                    {
                        if (StringUtil.IsInList("writeobject", strRights) == false)
                        {
                            strError = "д�������Դ " + strResPath + " ���ܾ������߱�writeobjectȨ��";
                            return 0;
                        }
                        return 1;   // �������writeobjectȨ�ޣ��Ͳ�����ҪwriteresȨ��
                    }
                }

                if (StringUtil.IsInList("writeres", strRights) == false)
                {
                    strError = "д����Դ " + strResPath + " ���ܾ������߱�writeresȨ��";
                    return 0;
                }
            }


            /*
            if (StringUtil.IsInList("writeres", strRights) == false)
            {
                strError = "д����Դ " + strResPath + " ���ܾ������߱�writeresȨ�ޡ�";
                return 0;
            }*/

            // ��ע��
            if (this.IsCommentDbName(strDbName) == true)
            {
                string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

                // cfgs
                if (strFirstPart == "cfgs")
                {
                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    if (strFirstPart == "template")
                    {
                        if (StringUtil.IsInList("writetemplate", strRights) == false)
                        {
                            strError = "д��ģ�������ļ� " + strResPath + " ���ܾ������߱�writetemplateȨ��";
                            return 0;
                        }
                        return 1;   // �������writetemplateȨ�ޣ��Ͳ�����ҪwriteresȨ��
                    }
                }

                // ��¼ID
                if (StringUtil.IsPureNumber(strFirstPart) == true
                    || strFirstPart == "?")
                {
                    // ֻ����¼ID��һ��
                    if (strPath == "")
                    {
                        strError = "������ʹ��WriteRes()д����ע���¼";
                        return 0;
                    }

                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

                    // ������Դ
                    if (strFirstPart == "object")
                    {
                        if (StringUtil.IsInList("writeobject", strRights) == false)
                        {
                            strError = "д�������Դ " + strResPath + " ���ܾ������߱�writeobjectȨ��";
                            return 0;
                        }
                        return 1;   // �������writeobjectȨ�ޣ��Ͳ�����ҪwriteresȨ��
                    }
                }

                if (StringUtil.IsInList("writeres", strRights) == false)
                {
                    strError = "д����Դ " + strResPath + " ���ܾ������߱�writeresȨ��";
                    return 0;
                }
            }

            // ʵ�ÿ� 2013/10/30
            if (this.IsUtilDbName(strDbName) == true)
            {
                string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

                // cfgs
                if (strFirstPart == "cfgs")
                {
                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    if (strFirstPart == "template")
                    {
                        if (StringUtil.IsInList("writetemplate", strRights) == false)
                        {
                            strError = "д��ģ�������ļ� " + strResPath + " ���ܾ������߱�writetemplateȨ��";
                            return 0;
                        }
                        return 1;   // �������writetemplateȨ�ޣ��Ͳ�����ҪwriteresȨ��
                    }

                }

                // ��¼ID
                if (StringUtil.IsPureNumber(strFirstPart) == true
                    || strFirstPart == "?")
                {
                    // ֻ����¼ID��һ��
                    if (strPath == "")
                    {
                        if (StringUtil.IsInList("writerecord", strRights) == false)
                        {
                            strError = "ֱ��д���¼ " + strResPath + " ���ܾ������߱�writerecordȨ��";
                            return 0;
                        }
                        return 1;   // �������writerecordȨ�ޣ��Ͳ�����ҪwriteresȨ��

                    }

                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

                    // ������Դ
                    if (strFirstPart == "object")
                    {
                        if (StringUtil.IsInList("writeobject", strRights) == false)
                        {
                            strError = "д�������Դ " + strResPath + " ���ܾ������߱�writeobjectȨ��";
                            return 0;
                        }
                        return 1;   // �������writeobjectȨ�ޣ��Ͳ�����ҪwriteresȨ��
                    }
                }

                if (StringUtil.IsInList("writeres", strRights) == false)
                {
                    strError = "д����Դ " + strResPath + " ���ܾ������߱�writeresȨ��";
                    return 0;
                }
            }

            strError = "д����Դ " + strResPath + " ���ܾ������߱��ض���Ȩ��";
            return 0;
        }

        // ����û�ʹ�� GetRes API ��Ȩ��
        // parameters:
        //      strLibraryCodeList  ��ǰ�û�����Ͻ�Ĺݴ����б�
        //      strRights   �����ߵ�Ȩ��
        //      strLibraryCode  [out]����Ƿ��ʶ��߿⣬���ﷵ��ʵ�ʷ��ʵĶ��߿�Ĺݴ��롣������Ƿ��ʶ��߿⣬�򷵻ؿ�
        //      strFilePath  [out]�����ļ�·��
        // return:
        //      -1  error
        //      0   ���߱�Ȩ��
        //      1   �߱�Ȩ��
        public int CheckGetResRights(
            SessionInfo sessioninfo,
            string strLibraryCodeList,
            string strRights,
            string strResPath,
            out string strLibraryCode,
            out string strFilePath,
            out string strError)
        {
            strError = "";
            strLibraryCode = "";
            strFilePath = "";

            string strPath = strResPath;

            // ��ȡ dp2library �����ļ�
            if (string.IsNullOrEmpty(strPath) == false
                && strPath[0] == '!')
            {
                strPath = strPath.Substring(1);

                string strTargetDir = this.DataDir;
                strFilePath = Path.Combine(strTargetDir, strPath);

                // ע�⣺ strPath �е�б��Ӧ���� '/'
                string strFirstLevel = StringUtil.GetFirstPartPath(ref strPath);
                if (string.Compare(strFirstLevel, "upload", true) != 0)
                {
                    strError = "��һ��Ŀ¼������Ϊ 'upload'";
                    return -1;
                }
                if (StringUtil.IsInList("download", strRights) == false)
                {
                    strError = "��ȡ�ļ� " + strResPath + " ���ܾ������߱� download Ȩ��";
                    return 0;
                }
                // �����޶��ĸ�Ŀ¼
                string strLimitDir = Path.Combine(strTargetDir, strFirstLevel);
                if (PathUtil.IsChildOrEqual(strFilePath, strLimitDir) == false)
                {
                    strError = "·�� '" + strResPath + "' Խ�����޶��ķ�Χ���޷�����";
                    return 0;
                }
                return 1;
            }

            // ����߱� writeobject Ȩ�ޣ���߱����ж���Ķ�ȡȨ����
            if (StringUtil.IsInList("writeobject", strRights) == true
                || StringUtil.IsInList("writeres", strRights) == true)
                return 1;

            string strDbName = StringUtil.GetFirstPartPath(ref strPath);

            // ��Ŀ��
            if (this.IsBiblioDbName(strDbName) == true)
            {
                string strRecordID = StringUtil.GetFirstPartPath(ref strPath);

                // cfgs
                if (strRecordID == "cfgs")
                {
                    return 1;   // ��Ŀ�������������ļ�
                }

                // ��¼ID
                if (StringUtil.IsPureNumber(strRecordID) == true
                    || strRecordID == "?")
                {
                    // ֻ����¼ID��һ��
                    if (string.IsNullOrEmpty(strPath) == true)
                    {
                        return 1;
                    }

                    string strObject = StringUtil.GetFirstPartPath(ref strPath);

                    // ������Դ
                    if (strObject == "object")
                    {
                        string strObjectID = StringUtil.GetFirstPartPath(ref strPath);
                        // ���� ID �õ�Ȩ�޶�������ж�
                        string strXmlRecordPath = strDbName + "/" + strRecordID;

                        string strObjectRights = "";
                        // ��ö���� rights ����
                        // ��Ҫ�Ȼ��Ԫ���� XML��Ȼ����еõ� file Ԫ�ص� rights ����
                        // return:
                        //      -1  ����
                        //      0   û���ҵ� object id ��ص���Ϣ
                        //      1   �ҵ�
                        int nRet = GetObjectRights(
            sessioninfo,
            strXmlRecordPath,
            strObjectID,
            out strObjectRights,
            out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 0)
                            return 1;   // TODO: ��ʱ�Ƿ��������?
                        if (string.IsNullOrEmpty(strObjectRights) == true)
                            return 1;   // û�ж��� rights �Ķ����������κη���������ȡ��

                        if (CanGet(strRights, strObjectRights) == true)
                            return 1;

                        strError = "��ȡ��Դ " + strResPath + " ���ܾ������߱���Ӧ��Ȩ��";
                        return 0;
                    }
                }

                strError = "��ȡ��Դ " + strResPath + " ���ܾ������߱���Ӧ��Ȩ��";
                return 0;
            }

#if NO
            // ���߿�
            if (this.IsReaderDbName(strDbName, out strLibraryCode) == true)
            {
                // 2012/9/22
                if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                {
                    if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                    {
                        strError = "д����Դ " + strResPath + " ���ܾ������߿� '" + strDbName + "' ���ڵ�ǰ�û��Ĺ�Ͻ��Χ '" + strLibraryCodeList + "' ��";
                        return 0;
                    }
                }

                string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

                // cfgs
                if (strFirstPart == "cfgs")
                {
                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    if (strFirstPart == "template")
                    {
                        if (StringUtil.IsInList("writetemplate", strRights) == false)
                        {
                            strError = "д��ģ�������ļ� " + strResPath + " ���ܾ������߱�writetemplateȨ��";
                            return 0;
                        }
                        return 1;   // �������writetemplateȨ�ޣ��Ͳ�����ҪwriteresȨ��
                    }

                }

                // ��¼ID
                if (StringUtil.IsPureNumber(strFirstPart) == true
                    || strFirstPart == "?")
                {
                    // ֻ����¼ID��һ��
                    if (strPath == "")
                    {
                        /*
                        if (StringUtil.IsInList("writerecord", strRights) == false)
                        {
                            strError = "ֱ��д���¼ " + strResPath + " ���ܾ������߱�writerecordȨ�ޡ�";
                            return 0;
                        }
                        return 1;   // �������writerecordȨ�ޣ��Ͳ�����ҪwriteresȨ��
                         * */
                        strError = "������ʹ��WriteRes()д����߿��¼";
                        return 0;
                    }

                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

                    // ������Դ
                    if (strFirstPart == "object")
                    {
                        if (StringUtil.IsInList("writeobject", strRights) == false)
                        {
                            strError = "д�������Դ " + strResPath + " ���ܾ������߱�writeobjectȨ��";
                            return 0;
                        }
                        return 1;   // �������writeobjectȨ�ޣ��Ͳ�����ҪwriteresȨ��
                    }
                }

                if (StringUtil.IsInList("writeres", strRights) == false)
                {
                    strError = "д����Դ " + strResPath + " ���ܾ������߱�writeresȨ��";
                    return 0;
                }
            }
#endif

            return 1;
#if NO
            strError = "��ȡ��Դ " + strResPath + " ���ܾ������߱��ض���Ȩ��";
            return 0;
#endif
        }

        // �����Ƿ�������ȡ?
        public static bool CanGet(string strUserRights, string strObjectRights)
        {
            string[] users = strUserRights.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
            string[] objects = strObjectRights.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(string o in objects)
            {
                if (IndexOf(users, o) != -1)
                    return true;
                if (StringUtil.HasHead(o, "level-") == true)
                {
                    if (HasLevel(o, users) == true)
                        return true;
                }
            }

            return false;
        }

        // strList ���Ƿ�����˸��ڻ��ߵ��� strLevel Ҫ����ַ���?
        static bool HasLevel(string strLevel, string [] list)
        {
            int level = GetLevelNumber(strLevel);

            // string[] list = strList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string o in list)
            {
                if (StringUtil.HasHead(o, "level-") == true)
                {
                    int current = GetLevelNumber(o);
                    if (current >= level)
                        return true;
                }
            }

            return false;
        }

        // ��� "level-10" �ַ����е�����ֵ
        static int GetLevelNumber(string strText)
        {
            int nRet = strText.IndexOf("-");
            if (nRet == -1)
                return -1;
            strText = strText.Substring(nRet + 1);
            int number = -1;
            int.TryParse(strText, out number);
            return number;
        }

        static int IndexOf(string[] strings, string s)
        {
            int i = 0;
            foreach (string o in strings)
            {
                if (s == o)
                    return i;
                i++;
            }
            return -1;
        }

        // ��ö���� rights ����
        // ��Ҫ�Ȼ��Ԫ���� XML��Ȼ����еõ� file Ԫ�ص� rights ����
        // return:
        //      -1  ����
        //      0   û���ҵ� object id ��ص���Ϣ
        //      1   �ҵ�
        int GetObjectRights(
            SessionInfo sessioninfo,
            string strXmlRecordPath,
            string strObjectID,
            out string strRights,
            out string strError)
        {
            strError = "";
            strRights = "";

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            string strXml = "";
            string strMetaData = "";
            byte[] timestamp = null;
            string strTempOutputPath = "";
            long lRet = channel.GetRes(strXmlRecordPath,
                out strXml,
                out strMetaData,
                out timestamp,
                out strTempOutputPath,
                out strError);
            if (lRet == -1)
            {
                strError = "���Ԫ���ݼ�¼ '" + strXmlRecordPath + "' ʱ����: " + strError;
                return -1;
            }

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "Ԫ���ݼ�¼ XML װ�� DOM ʱ����: " + ex.Message;
                return -1;
            }

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);

            var node = dom.DocumentElement.SelectSingleNode("//dprms:file[@id='"+strObjectID+"']", 
                nsmgr) as XmlElement;
            if (node == null)
                return 0;
            strRights = node.GetAttribute("rights");
            return 1;
        }

        public class ReaderDbCfg
        {
            public string DbName = "";
            public bool InCirculation = true;   // 2008/6/3 

            public string LibraryCode = "";     // 2012/9/7
        }

        public enum ResPathType
        {
            None = 0,
            Record = 1,
            CfgFile = 2,
            Object = 3,
        }

        // �ж�һ��·���Ƿ�Ϊ����·��
        public static ResPathType GetResPathType(string strPath)
        {
            string strDbName = StringUtil.GetFirstPartPath(ref strPath);

            string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

            // cfgs
            if (strFirstPart == "cfgs")
            {
                return ResPathType.CfgFile;
            }

            // ��¼ID
            if (StringUtil.IsPureNumber(strFirstPart) == true
                || strFirstPart == "?")
            {
                // ֻ����¼ID��һ��
                if (String.IsNullOrEmpty(strPath) == true)
                {
                    return ResPathType.Record;
                }

                strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

                // ������Դ
                if (strFirstPart == "object")
                {
                    return ResPathType.Object;
                }
            }

            return ResPathType.None;
        }



    }

    // ϵͳ���������
    public enum HangupReason
    {
        None = 0,   // û�й���
        LogRecover = 1, // ��־�ָ�
        Backup = 2, // �󱸷�
        Normal = 3, // ��ͨά��
        OperLogError = 4,   // ������־����������־�ռ�����
        Exit = 5,  // ϵͳ�����˳�
    }

    // API������
    public enum ErrorCode
    {
        NoError = 0,
        SystemError = 1,    // ϵͳ����ָapplication����ʱ�����ش���
        NotFound = 2,   // û���ҵ�
        ReaderBarcodeNotFound = 3,  // ����֤����Ų�����
        ItemBarcodeNotFound = 4,  // ������Ų�����
        Overdue = 5,    // ������̷����г���������Ѿ������鴦����ϣ������Ѿ���������Ϣ���ص����߼�¼�У�������Ҫ���Ѷ��߼�ʱ���г���ΥԼ���������
        NotLogin = 6,   // ��δ��¼
        DupItemBarcode = 7, // ԤԼ�б����ύ��ĳЩ������ű���������ǰ��ԤԼ��
        InvalidParameter = 8,   // ���Ϸ��Ĳ���
        ReturnReservation = 9,    // ��������ɹ�, �����ڱ�ԤԼͼ��, �����ԤԼ������
        BorrowReservationDenied = 10,    // �������ʧ��, �����ڱ�ԤԼ(����)������ͼ��, �ǵ�ǰԤԼ�߲��ܽ���
        RenewReservationDenied = 11,    // �������ʧ��, �����ڱ�ԤԼ��ͼ��
        AccessDenied = 12,  // ��ȡ���ܾ�
        // ChangePartDenied = 13,    // �����޸ı��ܾ�
        ItemBarcodeDup = 14,    // ��������ظ�
        Hangup = 15,    // ϵͳ����
        ReaderBarcodeDup = 16,  // ����֤������ظ�
        HasCirculationInfo = 17,    // ������ͨ��Ϣ(����ɾ��)
        SourceReaderBarcodeNotFound = 18,  // Դ����֤����Ų�����
        TargetReaderBarcodeNotFound = 19,  // Ŀ�����֤����Ų�����
        FromNotFound = 20,  // ����;��(from caption����style)û���ҵ�
        ItemDbNotDef = 21,  // ʵ���û�ж���
        IdcardNumberDup = 22,   // ���֤�ż��������ж��߼�¼��Ψһ����Ϊ�޷��������黹�顣���ǿ�����֤�����������
        IdcardNumberNotFound = 23,  // ���֤�Ų�����
        PartialDenied = 24,  // �в����޸ı��ܾ�
        ChannelReleased = 25,   // ͨ����ǰ���ͷŹ������β���ʧ��
        OutofSession = 26,   // ͨ���ﵽ�������
        InvalidReaderBarcode = 27,  // ����֤����Ų��Ϸ�
        InvalidItemBarcode = 28,    // ������Ų��Ϸ�

        // ����Ϊ�����ں˴������������ͬ��������
        AlreadyExist = 100, // ����
        AlreadyExistOtherType = 101,
        ApplicationStartError = 102,
        EmptyRecord = 103,
        // None = 104, ������NoError
        NotFoundSubRes = 105,
        NotHasEnoughRights = 106,
        OtherError = 107,
        PartNotFound = 108,
        RequestCanceled = 109,
        RequestCanceledByEventClose = 110,
        RequestError = 111,
        RequestTimeOut = 112,
        TimestampMismatch = 113,
    }

    // API�������
    public class LibraryServerResult
    {
        public long Value = 0;
        public string ErrorInfo = "";
        public ErrorCode ErrorCode = ErrorCode.NoError;

        public LibraryServerResult Clone()
        {
            LibraryServerResult other = new LibraryServerResult();
            other.Value = this.Value;
            other.ErrorCode = this.ErrorCode;
            other.ErrorInfo = this.ErrorInfo;
            return other;
        }
    }

    // �ʻ���Ϣ
    public class Account
    {
        public string Location = "";

        public XmlElement XmlNode = null;  // library.xml �����ļ������С��

        public string LoginName = "";   // ��¼�� ����ǰ׺�ĸ��������ĵ�¼����

        public string Password = "";
        public string Type = "";

        string m_strRights = "";
        public string Rights
        {
            get
            {
                return this.m_strRights;
            }
            set
            {
                this.m_strRights = value;

                this.m_rightsOriginList.Text = LibraryApplication.ExpandRightString(value);
            }
        }

        QuickList m_rightsOriginList = new QuickList();

        public QuickList RightsOriginList
        {
            get
            {
                return this.m_rightsOriginList;
            }
        }

        public string AccountLibraryCode = ""; // 2007/12/15 
        public string Access = "";  // ��ȡȨ�޴��� 2008/2/28 

        public string UserID = "";  // �û�Ψһ��ʶ�����ڶ��ߣ������֤�����

        public string RmsUserName = "";
        public string RmsPassword = "";

        public string Barcode = ""; // ֤����š����ڶ����͵��ʻ�������

        public string Name = "";    // ���������ڶ����͵��ʻ�������

        public string DisplayName = ""; // ��ʾ�������ڶ����͵��ʻ�������

        public string PersonalLibrary;  // ��ի�������ڶ����͵��ʻ�������

        public string Token = "";   // ��������ı��

        public XmlDocument ReaderDom = null;    // ����Ƕ����ʻ��������Ƕ��߼�¼DOM
        public string ReaderDomBarcode = "";   // �����DOM����Ķ���֤�����
        public byte[] ReaderDomTimestamp = null;    // ���߼�¼ʱ���
        public string ReaderDomPath = "";   // ���߼�¼·��
        public DateTime ReaderDomLastTime = new DateTime((long)0);  // ���װ�ص�ʱ��
        public bool ReaderDomChanged = false;

        public Account()
        {
            Random random = new Random(unchecked((int)DateTime.Now.Ticks));
            long number = random.Next(0, 9999);	// 4λ����

            Token = Convert.ToString(DateTime.Now.Ticks) + "__" + Convert.ToString(number);
        }

        // ��ԭʼ��Ȩ�޶���
        public string RightsOrigin
        { 
            get
            {
                return LibraryApplication.ExpandRightString(this.Rights);
            }
        }
    }

    public class BrowseFormat
    {
        public string Name = "";
        public string ScriptFileName = "";
        public string Type = "";


        // ���ű��ļ������滯
        // ��Ϊ�ڶ���ű��ļ���ʱ��, ��һ����ǰ��������,
        // �������Ϊ ./cfgs/filename ��ʾ�ڵ�ǰ���µ�cfgsĿ¼��,
        // ���������Ϊ /cfgs/filename ���ʾ��ͬ�������ĸ���
        public static string CanonicalizeScriptFileName(string strDbName,
            string strScriptFileNameParam)
        {
            int nRet = 0;
            nRet = strScriptFileNameParam.IndexOf("./");
            if (nRet != -1)
            {
                // ��Ϊ�ǵ�ǰ����
                return strDbName + strScriptFileNameParam.Substring(1);
            }

            nRet = strScriptFileNameParam.IndexOf("/");
            if (nRet != -1)
            {
                // ��Ϊ�Ӹ���ʼ
                return strScriptFileNameParam.Substring(1);
            }

            return strScriptFileNameParam;  // ����ԭ��
        }
    }


    // ������Ϣ
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class CalenderInfo
    {
        [DataMember]
        public string Name = "";    // ��������������ȫ�ֵģ����硰������������Ҳ����������ʽ������ֹ�/�������������ֹ��û�ֻ���޸������Լ��ֹݵ������������Կ���ȫ������
        [DataMember]
        public string Range = "";
        [DataMember]
        public string Content = "";
        [DataMember]
        public string Comment = "";
    }

    // ������������ȷ����Щ�����ǹ�����
    public class Calendar
    {
        public string Name = "";
        RangeList m_range = null;

        public Calendar(string strName,
            string strData)
        {
            this.Name = strName;
            this.m_range = new RangeList(strData);
            this.m_range.Sort();
            this.m_range.Merge();
        }


        // ���һ��ʱ��ֵ�Ƿ��ڷǹ������ڣ�
        // ����ǣ�ͬʱ�����������һ�������յ�ʱ�̣�������ǣ��򲻷��أ�
        public bool IsInNonWorkingDay(DateTime time,
            out DateTime nextWorkingDay)
        {
            nextWorkingDay = DateTime.MinValue;

            long lDay = DateTimeUtil.DateTimeToLong8(time);

            bool bFound = false;

            long lNextWorkingDay = 0;

            for (int i = 0; i < this.m_range.Count; i++)
            {
                RangeItem item = (RangeItem)this.m_range[i];

                Debug.Assert(item.lLength >= 1, "");

                if (bFound == false)
                {
                    if (lDay >= item.lStart
                        && lDay < item.lStart + item.lLength)
                    {
                        // ��itemĩ��ʱ��
                        long lEndDay = item.lStart + item.lLength - 1;

                        DateTime t = DateTimeUtil.Long8ToDateTime(lEndDay);

                        // 24Сʱ���ʱ��
                        TimeSpan delta = new TimeSpan(24, 0, 0);
                        nextWorkingDay = t + delta;
                        lNextWorkingDay = DateTimeUtil.DateTimeToLong8(nextWorkingDay);
                        bFound = true;
                    }
                }
                else // bFound == true
                {
                    if (lNextWorkingDay >= item.lStart
                        && lNextWorkingDay < item.lStart + item.lLength)
                    {
                        long lEndDay = item.lStart + item.lLength - 1;

                        // ˵��Ԥ��ķǹ�����������һ�ηǹ����շ�Χ�ڣ���ô�ͻ�Ҫ�������Ҷϵ�
                        DateTime t = DateTimeUtil.Long8ToDateTime(lEndDay);
                        TimeSpan delta = new TimeSpan(24, 0, 0);    // 24Сʱ
                        nextWorkingDay = t + delta;
                        lNextWorkingDay = DateTimeUtil.DateTimeToLong8(nextWorkingDay);
                    }
                    else
                    {
                        // �ҵ��ϵ��ˣ�����
                        return true;
                    }
                }
            }

            if (bFound == false)
                return false;

            return true;
        }

        // �ų��ǹ����գ���ú����ʱ�����һ�ξ����ĩ��ʱ��ֵ
        public DateTime GetEndTime(DateTime start,
            TimeSpan distance)
        {
            Debug.Assert(distance.Ticks >= 0, "distance����Ϊ��ֵ");

            // long lDay = DateTimeToLong8(start);

            long nDeltaDays = (long)distance.TotalDays;

            long nDayCount = 0;

            DateTime curDay = start;

            //    DateTime curDay = Long8ToDateTime(lDay);
            for (; ;)
            {
                bool bNon = IsNonWorkingDay(DateTimeUtil.DateTimeToLong8(curDay));

                if (bNon == true)   // BUG !!! 2007/1/15
                    goto CONTINUE;

                if (nDayCount >= nDeltaDays)
                    break;

                nDayCount++;


            CONTINUE:
                TimeSpan delta = new TimeSpan(24, 0, 0);    // 24Сʱ
                curDay = curDay + delta;
            }

            return curDay;
        }

        // �ǲ��� �ǹ�����?
        public bool IsNonWorkingDay(long lDay)
        {
            for (int i = 0; i < this.m_range.Count; i++)
            {
                RangeItem item = (RangeItem)this.m_range[i];

                Debug.Assert(item.lLength >= 1, "");

                if (lDay >= item.lStart
    && lDay < item.lStart + item.lLength)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class ItemDbCfg
    {
        public string DbName = "";  // ʵ�����
        public string BiblioDbName = "";    // ��Ŀ����
        public string BiblioDbSyntax = "";  // ��Ŀ��MARC�﷨

        public string IssueDbName = ""; // �ڿ�
        public string OrderDbName = ""; // ������ 2007/11/27 
        public string CommentDbName = "";   // ��ע�� 2008/12/8 

        public string UnionCatalogStyle = "";   // ���ϱ�Ŀ���� 905  // 2007/12/15 

        public string Replication = "";   // ����  // 2013/11/19
        public string ReplicationServer = "";   // ����-�������� ���ڼ��ٷ���
        public string ReplicationDbName = "";   // ����-��Ŀ���� ���ڼ��ٷ���

        public bool InCirculation = true;   // 2008/6/4 

        public string Role = "";    // ��ɫ biblioSource/orderWork // 2009/10/23 
    }

    // API ListBiblioDbFroms()��ʹ�õĽṹ
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class BiblioDbFromInfo
    {
        [DataMember]
        public string Caption = ""; // �����ǩ
        [DataMember]
        public string Style = "";   // ��ɫ
    }

    // API ListFile()��ʹ�õĽṹ
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class FileItemInfo
    {
        [DataMember]
        public string Name = ""; // �ļ�(��Ŀ¼)��
        [DataMember]
        public string CreateTime = "";   // ����ʱ�䡣����ʱ�� "u" �ַ���
        [DataMember]
        public long Size = 0;   // �ߴ硣-1 ��ʾ����Ŀ¼����
    }
}
