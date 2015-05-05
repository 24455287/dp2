#define USE_LOCAL_CHANNEL
#define USE_THREAD   // Ҫʹ�ö������̡߳��ƺ��������ӻ��˼򵥵����⣬û�б�Ҫ

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Net;   // for WebClient class
using System.IO;
using System.Web;
using System.Threading;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.Script;
using DigitalPlatform.IO;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.CirculationClient.localhost;

// 2013/3/16 ��� XML ע��

namespace dp2Circulation
{
    /// <summary>
    /// ������ʷ
    /// </summary>
    public class OperHistory : ThreadBase
    {
        int m_inOnTimer = 0;
#if USE_THREAD
        List<OneCall> m_calls = new List<OneCall>();

        internal ReaderWriterLock m_lock = new ReaderWriterLock();
        internal static int m_nLockTimeout = 5000;	// 5000=5��

#endif

        #region Thread

#if NO
#if USE_THREAD
        bool m_bStopThread = true;
        internal Thread _thread = null;

        internal AutoResetEvent eventClose = new AutoResetEvent(false);	// true : initial state is signaled 
        internal AutoResetEvent eventActive = new AutoResetEvent(false);	// �����ź�
        internal AutoResetEvent eventFinished = new AutoResetEvent(false);	// true : initial state is signaled 

        public int PerTime = 1000;   // 1 �� 5 * 60 * 1000;	// 5 ����
#endif
#endif

        #endregion

        bool m_bNeedReload = false; // �Ƿ���Ҫ����װ��project xml

        /// <summary>
        /// IE ������ؼ���������ʾ������ʷ��Ϣ
        /// </summary>
        public WebBrowser WebBrowser = null;

        WebExternalHost m_webExternalHost = new WebExternalHost();

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

#if USE_LOCAL_CHANNEL
        // 2011/12/5
        /// <summary>
        /// ͨѶͨ��
        /// </summary>
        public LibraryChannel Channel = new LibraryChannel();
#endif

        int m_nCount = 0;

        /// <summary>
        /// �ű�������
        /// </summary>
        public ScriptManager ScriptManager = new ScriptManager();

        Assembly PrintAssembly = null;   // ��ӡ�����Assembly

        /// <summary>
        /// �ű������� PrintHost ���������ʵ��
        /// </summary>
        public PrintHost PrintHostObj = null;   // 

        int m_nAssenblyVersion = 0;

        int AssemblyVersion
        {
            get
            {
                return this.m_nAssenblyVersion;
            }
            set
            {
                this.m_nAssenblyVersion = value;
            }
        }

        /// <summary>
        /// ��ȡ���ò�������ǰ����ʹ�õĳ��ɴ�ӡ������
        /// </summary>
        public string CurrentProjectName
        {
            get
            {
                return this.MainForm.AppInfo.GetString(
                "charging_print",
                "projectName",
                "");
            }
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public void EnableControls(bool bEnable)
        {

        }

        /// <summary>
        /// ������е� HTML ��ʾ
        /// </summary>
        public void ClearHtml()
        {
            // string strCssUrl = this.MainForm.LibraryServerDir + "/history.css";
            string strCssUrl = PathUtil.MergePath(this.MainForm.DataDir, "/history.css");

            string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";

            string strJs = "";

            /*
            // 2009/2/11 new add
            if (String.IsNullOrEmpty(this.MainForm.LibraryServerDir) == false)
                strJs = "<SCRIPT language='javaSCRIPT' src='" + this.MainForm.LibraryServerDir + "/getsummary.js" + "'></SCRIPT>";
            */
            // strJs = "<SCRIPT language='javaSCRIPT' src='" + PathUtil.MergePath(this.MainForm.DataDir, "getsummary.js") + "'></SCRIPT>";

            {
                HtmlDocument doc = WebBrowser.Document;

                if (doc == null)
                {
                    WebBrowser.Navigate("about:blank");
                    doc = WebBrowser.Document;
                }
                doc = doc.OpenNew(true);
            }

            Global.WriteHtml(this.WebBrowser,
                "<html><head>" + strLink + strJs + "</head><body>");
        }

        /// <summary>
        /// ��ʼ�� OperHistory ����
        /// ��ʼ�������У�Ҫ������ɴ�ӡ�����ű����룬ʹ�����ھ���״̬
        /// </summary>
        /// <param name="main_form">��ܴ���</param>
        /// <param name="webbrowser">������ʾ������ʷ��Ϣ�� IE ������ؼ�</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError�У�0: �ɹ�</returns>
        public int Initial(MainForm main_form,
            WebBrowser webbrowser,
            out string strError)
        {
            int nRet = 0;
            strError = "";

            this.MainForm = main_form;

#if USE_LOCAL_CHANNEL
            this.Channel.Url = this.MainForm.LibraryServerUrl;
            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);
#endif


            /*
            string strLibraryServerUrl = this.MainForm.AppInfo.GetString(
"config",
"circulation_server_url",
"");
            int pos = strLibraryServerUrl.LastIndexOf("/");
            if (pos != -1)
                strLibraryServerUrl = strLibraryServerUrl.Substring(0, pos);
             * */


            this.WebBrowser = webbrowser;

            // webbrowser
            this.m_webExternalHost.Initial(this.MainForm, this.WebBrowser);
            this.WebBrowser.ObjectForScripting = this.m_webExternalHost;

            this.ClearHtml();

#if USE_THREAD
            this.BeginThread();
#endif

            /*
            Global.WriteHtml(this.WebBrowser,
    "<br/><br/><br/><br/><br/><br/><br/><br/><br/><br/><br/><br/><br/><br/><br/><br/><br/><br/><br/><br/><br/>");
             * */

            /*

            // ׼��script����
            string strCsFileName = this.MainForm.DataDir + "\\charging_print.cs";
            string strRefFileName = this.MainForm.DataDir + "\\charging_print.cs.ref";

            if (File.Exists(strCsFileName) == true)
            {
                Encoding encoding = FileUtil.DetectTextFileEncoding(strCsFileName);

                StreamReader sr = null;

                try
                {
                    // TODO: ������Զ�̽���ļ����뷽ʽ���ܲ���ȷ��
                    // ��Ҫר�ű�дһ��������̽���ı��ļ��ı��뷽ʽ
                    // Ŀǰֻ����UTF-8���뷽ʽ
                    sr = new StreamReader(strCsFileName, encoding);
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }
                string strCode = sr.ReadToEnd();
                sr.Close();
                sr = null;

                // .ref�ļ�����ȱʡ
                string strRef = "";
                if (File.Exists(strRefFileName) == true)
                {

                    try
                    {
                        sr = new StreamReader(strRefFileName, true);
                    }
                    catch (Exception ex)
                    {
                        strError = ex.Message;
                        return -1;
                    }
                    strRef = sr.ReadToEnd();
                    sr.Close();
                    sr = null;

                    // ��ǰ���
                    string[] saRef = null;
                    nRet = ScriptManager.GetRefsFromXml(strRef,
                        out saRef,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = strRefFileName + " �ļ�����(ӦΪXML��ʽ)��ʽ����: " + strError;
                        return -1;
                    }
                }

                nRet = PrepareScript(strCode,
                   strRef,
                   out strError);
                if (nRet == -1)
                {
                    strError = "C#�ű��ļ� " + strCsFileName + " ׼�����̷�������(���ɵ��ݴ�ӡ���������ʱʧЧ)��\r\n\r\n" + strError;
                    return -1;
                }
            }
             * */

            ScriptManager.applicationInfo = this.MainForm.AppInfo;
            ScriptManager.CfgFilePath =
                this.MainForm.DataDir + "\\charging_print_projects.xml";
            ScriptManager.DataDir = this.MainForm.DataDir;

            ScriptManager.CreateDefaultContent -= new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);
            ScriptManager.CreateDefaultContent += new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);

            try
            {
                ScriptManager.Load();
            }
            catch (FileNotFoundException ex)
            {
                strError = "file not found : " + ex.Message;
                return 0;   // ������������
            }
            catch (Exception ex)
            {
                strError = "load script manager error: " + ex.Message;
                return -1;
            }

            // ��÷�����
            string strProjectName = CurrentProjectName;

            if (String.IsNullOrEmpty(strProjectName) == false)
            {
                string strProjectLocate = "";
                // ��÷�������
                // strProjectNamePath	������������·��
                // return:
                //		-1	error
                //		0	not found project
                //		1	found
                nRet = this.ScriptManager.GetProjectData(
                    strProjectName,
                    out strProjectLocate);
                if (nRet == 0)
                {
                    strError = "ƾ����ӡ���� " + strProjectName + " û���ҵ�...";
                    return -1;
                }
                if (nRet == -1)
                {
                    strError = "scriptManager.GetProjectData() error ...";
                    return -1;
                }

                // 
                nRet = PrepareScript(strProjectName,
                    strProjectLocate,
                    out strError);
                if (nRet == -1)
                    return -1;

                // 2008/5/9 new add
                this.Initial();
            }

            return 0;
        }

#if USE_LOCAL_CHANNEL
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            MainForm.Channel_BeforeLogin(this, e);
        }
#endif

        /// <summary>
        /// �رյ�ǰ���󡣰����ر�ͨѶͨ��
        /// </summary>
        public void Close()
        {
#if USE_THREAD
            this.StopThread(false);
#endif

            if (this.m_webExternalHost != null)
                this.m_webExternalHost.Destroy();

#if USE_LOCAL_CHANNEL
            if (this.Channel != null)
            {
                this.Channel.Close();
                this.Channel = null;
            }
#endif

        }

        private void scriptManager_CreateDefaultContent(object sender,
            CreateDefaultContentEventArgs e)
        {
            string strPureFileName = Path.GetFileName(e.FileName);

            if (String.Compare(strPureFileName, "main.cs", true) == 0)
            {
                CreateDefaultMainCsFile(e.FileName);
                e.Created = true;
            }
            else
            {
                e.Created = false;
            }

        }

        /// <summary>
        /// Ϊ���ɴ�ӡ����������ʼ�ĵ� main.cs �ļ�
        /// </summary>
        /// <param name="strFileName">�ļ���</param>
        public static void CreateDefaultMainCsFile(string strFileName)
        {
            using (StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8))
            {
                sw.WriteLine("using System;");
                sw.WriteLine("using System.Windows.Forms;");
                sw.WriteLine("using System.IO;");
                sw.WriteLine("using System.Text;");
                sw.WriteLine("using System.Xml;");
                sw.WriteLine("");
                sw.WriteLine("using DigitalPlatform.Xml;");
                sw.WriteLine("using DigitalPlatform.IO;");
                sw.WriteLine("");
                sw.WriteLine("using dp2Circulation;");
                sw.WriteLine("");
                sw.WriteLine("public class MyPrint : PrintHost");
                sw.WriteLine("{");
                sw.WriteLine("");
                sw.WriteLine("\tpublic override void OnTestPrint(object sender, PrintEventArgs e)");
                sw.WriteLine("\t{");
                sw.WriteLine("\t}");
                sw.WriteLine("");
                sw.WriteLine("");
                sw.WriteLine("\tpublic override void OnPrint(object sender, PrintEventArgs e)");
                sw.WriteLine("\t{");
                sw.WriteLine("\t}");
                sw.WriteLine("");
                sw.WriteLine("}");
            }
        }

        /// <summary>
        /// �򿪳��ɴ�ӡ����������
        /// </summary>
        /// <param name="owner">��������</param>
        public void OnProjectManager(IWin32Window owner)
        {
            ProjectManageDlg dlg = new ProjectManageDlg();
            MainForm.SetControlFont(dlg, this.MainForm.DefaultFont, false);
            dlg.ProjectsUrl = "http://dp2003.com/dp2circulation/projects/projects.xml";
            dlg.HostName = "OperHistory";
            dlg.scriptManager = this.ScriptManager;
            dlg.AppInfo = this.MainForm.AppInfo;
            dlg.DataDir = this.MainForm.DataDir;
            dlg.StartPosition = FormStartPosition.CenterScreen;

            this.m_bNeedReload = false;

            dlg.CreateProjectXmlFile -= new AutoCreateProjectXmlFileEventHandle(dlg_CreateProjectXmlFile);
            dlg.CreateProjectXmlFile += new AutoCreateProjectXmlFileEventHandle(dlg_CreateProjectXmlFile);

            dlg.ShowDialog(owner);

            // �����Ҫ����װ��project xml
            if (this.m_bNeedReload == true)
            {
                string strError = "";
                try
                {
                    ScriptManager.Load();
                }
                catch (Exception ex)
                {
                    strError = "load script manager error: " + ex.Message;
                    MessageBox.Show(owner, strError);
                }
            }
        }

        // �������Զ�����project xml�ļ����¼�
        void dlg_CreateProjectXmlFile(object sender, AutoCreateProjectXmlFileEventArgs e)
        {
            m_bNeedReload = true;
        }

        /// <summary>
        /// ���ó��ɴ�ӡ�ű��е� OnInitial() ��������ʼ������״̬
        /// </summary>
        public void Initial()
        {
            // ����Script����
            if (this.PrintAssembly != null)
            {
                EventArgs e = new EventArgs();

                this.PrintHostObj.MainForm = this.MainForm;
                this.PrintHostObj.Assembly = this.PrintAssembly;
                try
                {
                    this.PrintHostObj.OnInitial(this, e);
                }
                catch (Exception ex)
                {
                    string strErrorInfo = "<br/>OnInitial()ʱ����: " + HttpUtility.HtmlEncode(ExceptionUtil.GetDebugText(ex));
                    AppendHtml(strErrorInfo);
                }
            }
        }

        /// <summary>
        /// ������֤�����ɨ��ʱ������
        /// </summary>
        /// <param name="strReaderBarcode">����֤�����</param>
        public void ReaderBarcodeScaned(string strReaderBarcode)
        {
            // ����Script����
            if (this.PrintAssembly != null)
            {
                ReaderBarcodeScanedEventArgs e = new ReaderBarcodeScanedEventArgs();
                e.ReaderBarcode = strReaderBarcode;

                this.PrintHostObj.MainForm = this.MainForm;
                this.PrintHostObj.Assembly = this.PrintAssembly;
                try
                {
                    this.PrintHostObj.OnReaderBarcodeScaned(this, e);
                }
                catch (Exception ex)
                {
                    string strErrorInfo = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(ExceptionUtil.GetDebugText(ex));
                    AppendHtml(strErrorInfo);
                }
            }
        }

        // ������ӡ���ݲ���ӡ����
        /// <summary>
        /// ��ӡʱ������
        /// </summary>
        public void Print()
        {
            // ����Script����
            if (this.PrintAssembly != null)
            {
                PrintEventArgs e = new PrintEventArgs();
                e.PrintInfo = this.PrintHostObj.PrintInfo;
                e.Action = "print";

                this.PrintHostObj.MainForm = this.MainForm;
                this.PrintHostObj.Assembly = this.PrintAssembly;
                try
                {
                    this.PrintHostObj.OnPrint(this, e);
                }
                catch (Exception ex)
                {
                    string strErrorInfo = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(ExceptionUtil.GetDebugText(ex));
                    AppendHtml(strErrorInfo);
                }
            }
        }

        // ������ӡ���ݲ���ӡ����
        /// <summary>
        /// ��ӡʱ������
        /// </summary>
        /// <param name="info">��ӡ��Ϣ</param>
        public void Print(PrintInfo info)
        {
            // ����Script����
            if (this.PrintAssembly != null)
            {
                PrintEventArgs e = new PrintEventArgs();
                e.PrintInfo = info;
                e.Action = "print";

                this.PrintHostObj.MainForm = this.MainForm;
                this.PrintHostObj.Assembly = this.PrintAssembly;
                try
                {
                    this.PrintHostObj.OnPrint(this, e);
                }
                catch (Exception ex)
                {
                    string strErrorInfo = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(ExceptionUtil.GetDebugText(ex));
                    AppendHtml(strErrorInfo);
                }
            }
        }

        // ������ӡ����
        /// <summary>
        /// Ҫ������ӡ����ʱ������
        /// </summary>
        /// <param name="info">��ӡ��Ϣ</param>
        /// <param name="strResultString">����ַ���</param>
        /// <param name="strResultFormat">����ַ����ĸ�ʽ</param>
        public void GetPrintContent(PrintInfo info,
            out string strResultString,
            out string strResultFormat)
        {
            strResultString = "";
            strResultFormat = "";

            // ����Script����
            if (this.PrintAssembly != null)
            {
                PrintEventArgs e = new PrintEventArgs();
                e.PrintInfo = info;
                e.Action = "create";

                this.PrintHostObj.MainForm = this.MainForm;
                this.PrintHostObj.Assembly = this.PrintAssembly;
                try
                {
                    this.PrintHostObj.OnPrint(this, e);
                }
                catch (Exception ex)
                {
                    string strErrorInfo = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(ExceptionUtil.GetDebugText(ex));
                    AppendHtml(strErrorInfo);
                }

                strResultString = e.ResultString;
                strResultFormat = e.ResultFormat;
            }

        }

        /// <summary>
        /// Ҫ�����ӡ������ʱ������
        /// </summary>
        public void ClearPrinterPreference()
        {
            // ����Script����
            if (this.PrintAssembly != null)
            {
                PrintEventArgs e = new PrintEventArgs();
                e.PrintInfo = this.PrintHostObj.PrintInfo;

                this.PrintHostObj.MainForm = this.MainForm;
                this.PrintHostObj.Assembly = this.PrintAssembly;
                try
                {
                    this.PrintHostObj.OnClearPrinterPreference(this, e);
                }
                catch (Exception ex)
                {
                    string strErrorInfo = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(ExceptionUtil.GetDebugText(ex));
                    AppendHtml(strErrorInfo);
                }
            }
        }

        /// <summary>
        /// Ҫ���Դ�ӡ��ʱ�򱻴���
        /// </summary>
        public void TestPrint()
        {
            // ����Script����
            if (this.PrintAssembly != null)
            {
                PrintEventArgs e = new PrintEventArgs();
                e.PrintInfo = this.PrintHostObj.PrintInfo;

                this.PrintHostObj.MainForm = this.MainForm;
                this.PrintHostObj.Assembly = this.PrintAssembly;
                try
                {
                    this.PrintHostObj.OnTestPrint(this, e);
                }
                catch (Exception ex)
                {
                    string strErrorInfo = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(ExceptionUtil.GetDebugText(ex));
                    AppendHtml(strErrorInfo);
                }
            }
        }

        /*
        public void Action(string strActionName)
        {
            // ����Script����
            if (this.PrintAssembly != null)
            {
                ActionEventArgs e = new ActionEventArgs();
                e.Operation = strActionName;
                e.OperName = strActionName;

                string strError = "";
                int nRet = this.TriggerScriptAction(e, out strError);
                if (nRet == -1)
                {
                    string strText = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(strError);
                    AppendHtml(strText);
                }
            }
        }*/

        #region Thread

#if USE_THREAD

        // �����߳�ÿһ��ѭ����ʵ���Թ���
        public override void Worker()
        {
            List<OneCall> calls = new List<OneCall>();
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            this.m_inOnTimer++;
            try
            {
                for (int i = 0; i < this.m_calls.Count; i++)
                {
                    OneCall call = this.m_calls[i];

                    calls.Add(call);
                }

                this.m_calls.Clear();
            }
            finally
            {
                this.m_inOnTimer--;
                this.m_lock.ReleaseWriterLock();
            }

            foreach (OneCall call in calls)
            {
                if (call.name == "borrow")
                {
                    /*
                    Delegate_Borrow d = (Delegate_Borrow)call.func;
                    this.MainForm.Invoke(d, call.parameters);
                     * */
                    Borrow((IChargingForm)call.parameters[0],
                        (bool)call.parameters[1],
                        (string)call.parameters[2],
                        (string)call.parameters[3],
                        (string)call.parameters[4],
                        (string)call.parameters[5],
                        (string)call.parameters[6],
                        (BorrowInfo)call.parameters[7],
                        (DateTime)call.parameters[8],
                        (DateTime)call.parameters[9]);
                }
                else if (call.name == "return")
                {
                    /*
                    Delegate_Return d = (Delegate_Return)call.func;
                    this.MainForm.Invoke(d, call.parameters);
                     * */
                    Return((IChargingForm)call.parameters[0],
                        (bool)call.parameters[1],
                        (string)call.parameters[2],
                        (string)call.parameters[3],
                        (string)call.parameters[4],
                        (string)call.parameters[5],
                        (string)call.parameters[6],
                        (ReturnInfo)call.parameters[7],
                        (DateTime)call.parameters[8],
                        (DateTime)call.parameters[9]);
                }
                else if (call.name == "amerce")
                {
                    /*
                    Delegate_Amerce d = (Delegate_Amerce)call.func;
                    this.MainForm.Invoke(d, call.parameters);
                     * */
                    Amerce((string)call.parameters[0],
    (string)call.parameters[1],
    (List<OverdueItemInfo>)call.parameters[2],
    (string)call.parameters[3],
    (DateTime)call.parameters[4],
    (DateTime)call.parameters[5]);
                }
            }

#if NO
            if (calls.Count > 0)
            {
                this.m_lock.AcquireWriterLock(m_nLockTimeout);
                this.m_inOnTimer++;
                try
                {
                    for (int i = 0; i < calls.Count; i++)
                    {
                        this.m_calls.RemoveAt(0);
                    }
                }
                finally
                {
                    this.m_inOnTimer--;
                    this.m_lock.ReleaseWriterLock();
                }
            }
#endif
        }

#if NO
        void ThreadMain()
        {
            m_bStopThread = false;
            try
            {
                WaitHandle[] events = new WaitHandle[2];

                events[0] = eventClose;
                events[1] = eventActive;

                while (m_bStopThread == false)
                {
                    int index = 0;
                    try
                    {
                        index = WaitHandle.WaitAny(events, PerTime, false);
                    }
                    catch (System.Threading.ThreadAbortException /*ex*/)
                    {
                        break;
                    }

                    if (index == WaitHandle.WaitTimeout)
                    {
                        // ��ʱ
                        eventActive.Reset();
                        Worker();
                        eventActive.Reset();

                    }
                    else if (index == 0)
                    {
                        break;
                    }
                    else
                    {
                        // �õ������ź�
                        eventActive.Reset();
                        Worker();
                        eventActive.Reset();
                    }
                }

                return;
            }
            finally
            {
                m_bStopThread = true;
            }
        }

        public bool Stopped
        {
            get
            {
                return m_bStopThread;
            }
        }

        void StopThread(bool bForce)
        {
            if (this._thread == null)
                return;

            // �����ǰ����������ֹͣ
            m_bStopThread = true;
            this.eventClose.Set();

            if (bForce == true)
            {
                if (this._thread != null)
                {
                    if (!this._thread.Join(2000))
                        this._thread.Abort();
                    this._thread = null;
                }
            }
        }

        public void BeginThread()
        {
            if (this._thread != null)
                return;

            // �����ǰ����������ֹͣ
            StopThread(true);



            this._thread = new Thread(new ThreadStart(this.ThreadMain));
            this._thread.Start();
        }

        public void Activate()
        {
            eventActive.Set();
        }
#endif

        void AddCall(OneCall call)
        {
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                this.m_calls.Add(call);
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

            Activate();
        }

#endif

        #endregion


#if NO
        internal void OnTimer()
        {
            if (this.m_inOnTimer > 0)
                return;
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            this.m_inOnTimer++;
            try
            {
                for (int i = 0; i < this.m_calls.Count; i++)
                {
                    OneCall call = this.m_calls[i];

                    if (call.name == "borrow")
                    {
                        Delegate_Borrow d = (Delegate_Borrow)call.func;
                        this.MainForm.Invoke(d, call.parameters);
                    }
                    else if (call.name == "return")
                    {
                        Delegate_Return d = (Delegate_Return)call.func;
                        this.MainForm.Invoke(d, call.parameters);
                    }
                    else if (call.name == "amerce")
                    {
                        Delegate_Amerce d = (Delegate_Amerce)call.func;
                        this.MainForm.Invoke(d, call.parameters);
                    }
                }

                this.m_calls.Clear();
            }
            finally
            {
                this.m_inOnTimer--;
                this.m_lock.ReleaseWriterLock();
            }
        }
#endif

        internal delegate void Delegate_Borrow(IChargingForm charging_form,
            bool bRenew,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            string strReaderSummary,
            string strItemXml, 
            BorrowInfo borrow_info,
            DateTime start_time,
            DateTime end_time);

        // ���Ķ����첽�¼�
        internal void BorrowAsync(IChargingForm charging_form,
            bool bRenew,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            string strReaderSummary,
            string strItemXml,
            BorrowInfo borrow_info,
            DateTime start_time,
            DateTime end_time)
        {

#if !USE_THREAD
            Delegate_Borrow d = new Delegate_Borrow(Borrow);
            this.MainForm.BeginInvoke(d, new object[] { charging_form,
            bRenew,
            strReaderBarcode,
            strItemBarcode,
            strConfirmItemRecPath,
            strReaderSummary,
            strItemXml, 
            borrow_info,
            start_time,
            end_time});
#else
            OneCall call = new OneCall();
            call.name = "borrow";
            call.func = new Delegate_Borrow(Borrow);
            call.parameters = new object[] { charging_form,
            bRenew,
            strReaderBarcode,
            strItemBarcode,
            strConfirmItemRecPath,
            strReaderSummary,
            strItemXml, 
            borrow_info,
            start_time,
            end_time};

            AddCall(call);
#endif
        }

        static string DoubleToString(double v)
        {
            return v.ToString("0.00");
        }

        /// <summary>
        /// �����ĿժҪ
        /// </summary>
        /// <param name="strItemBarcode">�������</param>
        /// <param name="strConfirmItemRecPath">����ȷ�ϵĲ��¼·��������Ϊ��</param>
        /// <param name="strSummary">��ĿժҪ</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError�У�0: û���ҵ�; 1: �ҵ���</returns>
        public int GetBiblioSummary(string strItemBarcode,
    string strConfirmItemRecPath,
    out string strSummary,
    out string strError)
        {
#if USE_LOCAL_CHANNEL
            string strBiblioRecPath = "";

            int nRet = this.MainForm.GetCachedBiblioSummary(strItemBarcode,
strConfirmItemRecPath,
out strSummary,
out strError);
            if (nRet == -1 || nRet == 1)
                return nRet;

            Debug.Assert(nRet == 0, "");

            long lRet = Channel.GetBiblioSummary(
                null,
                strItemBarcode,
                strConfirmItemRecPath,
                null,
                out strBiblioRecPath,
                out strSummary,
                out strError);
            if (lRet == -1)
            {
                return -1;
            }
            else
            {
                // 2013/12/13
                this.MainForm.SetBiblioSummaryCache(strItemBarcode,
                     strConfirmItemRecPath,
                     strSummary);
            }
            return (int)lRet;
#else
            return this.MainForm.GetBiblioSummary(strItemBarcode,
                        strConfirmItemRecPath,
                        out strSummary,
                        out strError);
#endif
        }

        internal void Borrow(
            IChargingForm charging_form,
            bool bRenew,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            string strReaderSummary,
            string strItemXml,  // 2008/5/9 new add
            BorrowInfo borrow_info,
            DateTime start_time,
            DateTime end_time)
        {
            TimeSpan delta = end_time - start_time; // δ����GetSummary()��ʱ��

            string strText = "";
            int nRet = 0;

            string strOperName = "��";
            if (bRenew == true)
                strOperName = "����";

            string strError = "";
            string strSummary = "";

            nRet = this.GetBiblioSummary(strItemBarcode,
                    strConfirmItemRecPath,
                    out strSummary,
                    out strError);
            if (nRet == -1)
                strSummary = strError;

            string strOperClass = "even";
            if ((this.m_nCount % 2) == 1)
                strOperClass = "odd";

            string strItemLink = "<a href='javascript:void(0);' onclick=\"window.external.OpenForm('ItemInfoForm', this.innerText, true);\">" + HttpUtility.HtmlEncode(strItemBarcode) + "</a>";
            string strReaderLink = "<a href='javascript:void(0);' onclick=\"window.external.OpenForm('ReaderInfoForm', this.innerText, true);\">" + HttpUtility.HtmlEncode(strReaderBarcode) + "</a>";

            strText = "<div class='item " + strOperClass + " borrow'>"
                + "<div class='time_line'>"
                + " <div class='time'>" + DateTime.Now.ToLongTimeString() + "</div>"
                + " <div class='time_span'>��ʱ " + DoubleToString(delta.TotalSeconds) + "��</div>"
                + " <div class='clear'></div>"
                + "</div>"
                + "<div class='reader_line'>"
                + " <div class='reader_prefix_text'>����</div>"
                + " <div class='reader_barcode'>" + strReaderLink + "</div>"
                + " <div class='reader_summary'>" + HttpUtility.HtmlEncode(strReaderSummary) + "</div>"
                + " <div class='clear'></div>"
                + "</div>"
                + "<div class='opername_line'>"
                + " <div class='opername'>" + HttpUtility.HtmlEncode(strOperName) + "</div>"
                + " <div class='clear'></div>"
                + "</div>"
                + "<div class='item_line'>"
                + " <div class='item_prefix_text'>��</div>"
                + " <div class='item_barcode'>" + strItemLink + "</div> "
                + " <div class='item_summary'>" + HttpUtility.HtmlEncode(strSummary) + "</div>"
                + " <div class='clear'></div>"
                + "</div>"
                + " <div class='clear'></div>"
                + "</div>";
            /*
            strText = "<div class='" + strOperClass + "'>"
    + "<div class='time_line'><span class='time'>" + DateTime.Now.ToLongTimeString() + "</span> <span class='time_span'>��ʱ " + delta.TotalSeconds.ToString() + "��</span></div>"
    + "<div class='reader_line'><span class='reader_prefix_text'>����</span> <span class='reader_barcode'>[" + strReaderBarcode + "]</span>"
+ " <span class='reader_summary'>" + strReaderSummary + "<span></div>"
+ "<div class='opername_line'><span class='opername'>" + strOperName + "<span></div>"
+ "<div class='item_line'><span class='item_prefix_text'>��</span> <span class='item_barcode'>[" + strItemBarcode + "]</span> "
+ "<span class='item_summary' id='" + m_nCount.ToString() + "' onreadystatechange='GetOneSummary(\"" + m_nCount.ToString() + "\");'>" + strItemBarcode + "</span></div>"
+ "</div>";
             * */

            AppendHtml(strText);
            m_nCount++;


            // ����Script����
            if (this.PrintAssembly != null)
            {
                BorrowedEventArgs e = new BorrowedEventArgs();
                e.OperName = strOperName;
                e.BiblioSummary = strSummary;
                e.ItemBarcode = strItemBarcode;
                e.ReaderBarcode = strReaderBarcode;
                e.TimeSpan = delta;
                e.ReaderSummary = strReaderSummary;
                e.ItemXml = strItemXml;
                e.ChargingForm = charging_form;

                if (borrow_info != null)
                {
                    if (String.IsNullOrEmpty(borrow_info.LatestReturnTime) == true)
                        e.LatestReturnDate = new DateTime(0);
                    else
                        e.LatestReturnDate = DateTimeUtil.FromRfc1123DateTimeString(borrow_info.LatestReturnTime).ToLocalTime();
                    e.Period = borrow_info.Period;
                    e.BorrowCount = borrow_info.BorrowCount;
                    e.BorrowOperator = borrow_info.BorrowOperator;
                }

                this.PrintHostObj.MainForm = this.MainForm;
                this.PrintHostObj.Assembly = this.PrintAssembly;
                try
                {
                    this.PrintHostObj.OnBorrowed(this, e);
                }
                catch (Exception ex)
                {
                    string strErrorInfo = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(ExceptionUtil.GetDebugText(ex));
                    AppendHtml(strErrorInfo);
                }
                /*
                if (nRet == -1)
                {
                    strText = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(strError);
                    AppendHtml(strText);
                }*/
            }

            // ��tips�ɳ���ʾ���ߺͲ��ժҪ��Ϣ��������ȷ��ʾ��������棿
            // ����֤�Ͳ�����ű������ê�㣿
            // ����ժҪҪô����ǰ�ˣ�ͨ��XML������Ҫô���ڷ��������ù̶���������
        }

        internal delegate void Delegate_Return(IChargingForm charging_form,
            bool bLost,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            string strReaderSummary,
            string strItemXml,
            ReturnInfo return_info,
            DateTime start_time,
            DateTime end_time);

        internal void ReturnAsync(IChargingForm charging_form,
            bool bLost,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            string strReaderSummary,
            string strItemXml,
            ReturnInfo return_info,
            DateTime start_time,
            DateTime end_time)
        {
#if !USE_THREAD

            Delegate_Return d = new Delegate_Return(Return);
            this.MainForm.BeginInvoke(d, new object[] {charging_form,
            bLost,
            strReaderBarcode,
            strItemBarcode,
            strConfirmItemRecPath,
            strReaderSummary,
            strItemXml,
            return_info,
            start_time,
            end_time});
#else
            OneCall call = new OneCall();
            call.name = "return";
            call.func = new Delegate_Return(Return);
            call.parameters = new object[] { charging_form,
            bLost,
            strReaderBarcode,
            strItemBarcode,
            strConfirmItemRecPath,
            strReaderSummary,
            strItemXml,
            return_info,
            start_time,
            end_time};

            AddCall(call);
#endif
        }

        internal void Return(
            IChargingForm charging_form,
            bool bLost,
            string strReaderBarcode,
            string strItemBarcode,
            string strConfirmItemRecPath,
            string strReaderSummary,
            string strItemXml,  // 2008/5/9 new add
            ReturnInfo return_info,
            DateTime start_time,
            DateTime end_time)
        {
            TimeSpan delta = end_time - start_time; // δ����GetSummary()��ʱ��

            string strText = "";
            int nRet = 0;

            string strOperName = "��";
            if (bLost == true)
                strOperName = "��ʧ";

            string strError = "";
            string strSummary = "";
            nRet = this.GetBiblioSummary(strItemBarcode,
                    strConfirmItemRecPath,
                    out strSummary,
                    out strError);
            if (nRet == -1)
                strSummary = strError;

            string strOperClass = "even";
            if ((this.m_nCount % 2) == 1)
                strOperClass = "odd";

            string strLocation = "";
            if (return_info != null)
                strLocation = return_info.Location;

            string strItemLink = "<a href='javascript:void(0);' onclick=\"window.external.OpenForm('ItemInfoForm', this.innerText, true);\">" + HttpUtility.HtmlEncode(strItemBarcode) + "</a>";
            string strReaderLink = "<a href='javascript:void(0);' onclick=\"window.external.OpenForm('ReaderInfoForm', this.innerText, true);\">" + HttpUtility.HtmlEncode(strReaderBarcode) + "</a>";

            strText = "<div class='item " + strOperClass + " return'>"
                + "<div class='time_line'>"
                + " <div class='time'>" + DateTime.Now.ToLongTimeString() + "</div>"
                + " <div class='time_span'>��ʱ " + DoubleToString(delta.TotalSeconds) + "��</div>"
                + " <div class='clear'></div>"
                + "</div>"
                + "<div class='reader_line'>"
                + " <div class='reader_prefix_text'>����</div>"
                + " <div class='reader_barcode'>" + strReaderLink + "</div>"
                + " <div class='reader_summary'>" + HttpUtility.HtmlEncode(strReaderSummary) + "</div>"
                + " <div class='clear'></div>"
                + "</div>"
                + "<div class='opername_line'>"
                + " <div class='opername'>" + HttpUtility.HtmlEncode(strOperName) + "</div>"
                + " <div class='clear'></div>"
                + "</div>"
                + "<div class='item_line'>"
                + " <div class='item_prefix_text'>��</div>"
                + " <div class='item_barcode'>" + strItemLink + "</div> "
                + " <div class='item_summary'>" + HttpUtility.HtmlEncode(strSummary) + "</div>"

                + (string.IsNullOrEmpty(strLocation) == false ? " <div class='item_location'>" + HttpUtility.HtmlEncode(strLocation) + "</div>" : "")
                + " <div class='clear'></div>"
                + "</div>"
                + " <div class='clear'></div>"
                + "</div>";

            /*
            strText = "<div class='" + strOperClass + "'>"
    + "<div class='time_line'><span class='time'>" + DateTime.Now.ToLongTimeString() + "</span> <span class='time_span'>��ʱ " + delta.TotalSeconds.ToString() + "��</span></div>"
    + "<div class='reader_line'><span class='reader_prefix_text'>����</span> <span class='reader_barcode'>[" + strReaderBarcode + "]</span>"
+ " <span class='reader_summary'>" + strReaderSummary + "<span></div>"
+ "<div class='opername_line'><span class='opername'>" + strOperName + "<span></div>"
+ "<div class='item_line'><span class='item_prefix_text'>��</span> <span class='item_barcode'>[" + strItemBarcode + "]</span> "
+ "<span class='item_summary' id='" + m_nCount.ToString() + "' onreadystatechange='GetOneSummary(\"" + m_nCount.ToString() + "\");'>" + strItemBarcode + "</span></div>"
+ "</div>";
             * */
            AppendHtml(strText);
            m_nCount++;

            // ����Script����
            if (this.PrintAssembly != null)
            {
                ReturnedEventArgs e = new ReturnedEventArgs();
                e.OperName = strOperName;
                e.BiblioSummary = strSummary;
                e.ItemBarcode = strItemBarcode;
                e.ReaderBarcode = strReaderBarcode;
                e.TimeSpan = delta;
                e.ReaderSummary = strReaderSummary;
                e.ItemXml = strItemXml;
                e.ChargingForm = charging_form;

                if (return_info != null)
                {
                    if (String.IsNullOrEmpty(return_info.BorrowTime) == true)
                        e.BorrowDate = new DateTime(0);
                    else
                        e.BorrowDate = DateTimeUtil.FromRfc1123DateTimeString(return_info.BorrowTime).ToLocalTime();

                    if (String.IsNullOrEmpty(return_info.LatestReturnTime) == true)
                        e.LatestReturnDate = new DateTime(0);
                    else
                        e.LatestReturnDate = DateTimeUtil.FromRfc1123DateTimeString(return_info.LatestReturnTime).ToLocalTime();
                    e.Period = return_info.Period;
                    e.BorrowCount = return_info.BorrowCount;
                    e.OverdueString = return_info.OverdueString;

                    e.BorrowOperator = return_info.BorrowOperator;
                    e.ReturnOperator = return_info.ReturnOperator;

                    // 2013/4/2
                    e.Location = return_info.Location;
                    e.BookType = return_info.BookType;
                }

                this.PrintHostObj.MainForm = this.MainForm;
                this.PrintHostObj.Assembly = this.PrintAssembly;
                try
                {
                    this.PrintHostObj.OnReturned(this, e);
                }
                catch (Exception ex)
                {
                    string strErrorInfo = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(ExceptionUtil.GetDebugText(ex));
                    AppendHtml(strErrorInfo);
                }
            }
        }

        delegate void Delegate_AppendHtml(string strText);
        /// <summary>
        /// �� IE �ؼ���׷��һ�� HTML ����
        /// </summary>
        /// <param name="strText">HTML ����</param>
        public void AppendHtml(string strText)
        {
            if (this.MainForm.InvokeRequired)
            {
                Delegate_AppendHtml d = new Delegate_AppendHtml(AppendHtml);
                this.MainForm.BeginInvoke(d, new object[] { strText });
                return;
            }

            Global.WriteHtml(this.WebBrowser,
                strText);
            // Global.ScrollToEnd(this.WebBrowser);

            // ��ΪHTMLԪ������û����β��������Щ�������ܲ���Ч
            this.WebBrowser.Document.Window.ScrollTo(0,
    this.WebBrowser.Document.Body.ScrollRectangle.Height);
        }

        internal delegate void Delegate_Amerce(string strReaderBarcode,
            string strReaderSummary,
            List<OverdueItemInfo> overdue_infos,
            string strAmerceOperator,
            DateTime start_time,
            DateTime end_time);

        internal void AmerceAsync(string strReaderBarcode,
            string strReaderSummary,
            List<OverdueItemInfo> overdue_infos,
            string strAmerceOperator,
            DateTime start_time,
            DateTime end_time)
        {

#if !USE_THREAD
            Delegate_Amerce d = new Delegate_Amerce(Amerce);
            this.MainForm.BeginInvoke(d, new object[] {strReaderBarcode,
                strReaderSummary,
                overdue_infos,
                strAmerceOperator,
                start_time,
                end_time});
#else
            OneCall call = new OneCall();
            call.name = "amerce";
            call.func = new Delegate_Amerce(Amerce);
            call.parameters = new object[] { strReaderBarcode,
            strReaderSummary,
            overdue_infos,
            strAmerceOperator,
            start_time,
            end_time};

            AddCall(call);
#endif
        }

        internal void Amerce(
            string strReaderBarcode,
            string strReaderSummary,
            List<OverdueItemInfo> overdue_infos,
            string strAmerceOperator,
            DateTime start_time,
            DateTime end_time)
        {
            string strOperName = "����";
            TimeSpan delta = end_time - start_time;

            string strText = "";
            int nRet = 0;


            foreach (OverdueItemInfo info in overdue_infos)
            {
                string strOperClass = "even";
                if ((this.m_nCount % 2) == 1)
                    strOperClass = "odd";

                string strSummary = "";

                string strItemLink = "";

                if (string.IsNullOrEmpty(info.ItemBarcode) == false)
                {
                    strItemLink = "<a href='javascript:void(0);' onclick=\"window.external.OpenForm('ItemInfoForm', this.innerText, true);\">" + HttpUtility.HtmlEncode(info.ItemBarcode) + "</a>";
                    string strError = "";
                    nRet = this.GetBiblioSummary(info.ItemBarcode,
    info.RecPath,
    out strSummary,
    out strError);
                    if (nRet == -1)
                        strSummary = strError;

                    strItemLink += " <div class='item_summary'>" + HttpUtility.HtmlEncode(strSummary) + "</div>";
                }

                string strReaderLink = "<a href='javascript:void(0);' onclick=\"window.external.OpenForm('ReaderInfoForm', this.innerText, true);\">" + HttpUtility.HtmlEncode(strReaderBarcode) + "</a>";

                string strTimePrefix = "";
                if (overdue_infos.Count > 1)
                    strTimePrefix = overdue_infos.Count.ToString() + "�ʽ��ѹ�";

                strText = "<div class='item " + strOperClass + " amerce'>"
                    + "<div class='time_line'>"
                    + " <div class='time'>" + DateTime.Now.ToLongTimeString() + "</div>"
                    + " <div class='time_span'>" + strTimePrefix + "��ʱ " + DoubleToString(delta.TotalSeconds) + "��</div>"
                    + " <div class='clear'></div>"
                    + "</div>"
                    + "<div class='reader_line'>"
                    + " <div class='reader_prefix_text'>����</div>"
                    + " <div class='reader_barcode'>" + strReaderLink + "</div>"
                    + " <div class='reader_summary'>" + HttpUtility.HtmlEncode(strReaderSummary) + "</div>"
                    + " <div class='clear'></div>"
                    + "</div>"
                    + "<div class='opername_line'>"
                    + " <div class='opername'>" + HttpUtility.HtmlEncode(strOperName) + "</div>"
                    + " <div class='clear'></div>"
                    + "</div>"
                    + "<div class='item_line'>"
                    + info.ToHtmlString(strItemLink)
                    + "</div>"
                    + " <div class='clear'></div>"
                    + "</div>";
                AppendHtml(strText);
                m_nCount++;

            }

            // ����Script����
            if (this.PrintAssembly != null)
            {
                AmercedEventArgs e = new AmercedEventArgs();
                e.OperName = strOperName;
                e.ReaderBarcode = strReaderBarcode;
                e.ReaderSummary = strReaderSummary;
                e.TimeSpan = delta;

                e.OverdueInfos = overdue_infos;

                e.AmerceOperator = strAmerceOperator;

                this.PrintHostObj.MainForm = this.MainForm;
                this.PrintHostObj.Assembly = this.PrintAssembly;
                try
                {
                    this.PrintHostObj.OnAmerced(this, e);
                }
                catch (Exception ex)
                {
                    string strErrorInfo = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(ExceptionUtil.GetDebugText(ex));
                    AppendHtml(strErrorInfo);
                }
                /*
                if (nRet == -1)
                {
                    strText = "<br/>���ݴ�ӡ�ű�����ʱ����: " + HttpUtility.HtmlEncode(strError);
                    AppendHtml(strText);
                }*/
            }

        }

#if NOOOOOOOOOOOOOO
        int PrepareScript(string strCode,
            string strRef,
            out string strError)
        {
            strError = "";
            string[] saRef = null;
            int nRet;

            nRet = ScriptManager.GetRefsFromXml(strRef,
                out saRef,
                out strError);
            if (nRet == -1)
            {
                strError = "strRef����\r\n\r\n" + strRef + "\r\n\r\n��ʽ����: " + strError;
                return -1;
            }

            // 2007/12/4 new add
            ScriptManager.RemoveRefsBinDirMacro(ref saRef);

            string[] saAddRef = {
									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marceditor.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcfixedfieldcontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.gcatclient.dll",
									Environment.CurrentDirectory + "\\dp2circulation.exe"
								};

            if (saAddRef != null)
            {
                string[] saTemp = new string[saRef.Length + saAddRef.Length];
                Array.Copy(saRef, 0, saTemp, 0, saRef.Length);
                Array.Copy(saAddRef, 0, saTemp, saRef.Length, saAddRef.Length);
                saRef = saTemp;
            }

            string strErrorInfo = "";
            string strWarningInfo = "";
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saRef,
                null,   // strLibPaths,
                out this.PrintAssembly,
                out strErrorInfo,
                out strWarningInfo);
            if (nRet == -1)
            {
                strError = "�ű����뷢�ִ���򾯸�:\r\n" + strErrorInfo;
                return -1;
            }

            // �õ�Assembly��PrintHost������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                PrintAssembly,
                "dp2Circulation.PrintHost");
            if (entryClassType == null)
            {
                strError = "dp2Circulation.PrintHost������û���ҵ�";
                return -1;
            }

            // newһ��PrintHost��������
            this.PrintHostObj = (PrintHost)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            if (PrintHostObj == null)
            {
                strError = "new PrintHost���������ʧ��";
                return -1;
            }



            return 0;
        }
#endif

        string m_strInstanceDir = "";
        // ����Ψһ��ʵ��Ŀ¼��dp2Circulation�˳������Ŀ¼���ᱻ����
        string InstanceDir
        {
            get
            {
                if (string.IsNullOrEmpty(this.m_strInstanceDir) == false)
                    return this.m_strInstanceDir;

                this.m_strInstanceDir = PathUtil.MergePath(this.MainForm.DataDir, "~bin_" + Guid.NewGuid().ToString());
                PathUtil.CreateDirIfNeed(this.m_strInstanceDir);

                return this.m_strInstanceDir;
            }
        }

        // ׼���ű�����
        int PrepareScript(string strProjectName,
            string strProjectLocate,
            out string strError)
        {
            strError = "";
            this.PrintAssembly = null;

            PrintHostObj = null;

            string strWarning = "";


            string strMainCsDllName = PathUtil.MergePath(this.InstanceDir, "\\~charging_print_main_" + Convert.ToString(AssemblyVersion++) + ".dll");    // ++

            string strLibPaths = "\"" + this.MainForm.DataDir + "\""
                + ","
                + "\"" + strProjectLocate + "\"";

            string[] saAddRef = {
									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marceditor.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcfixedfieldcontrol.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
									Environment.CurrentDirectory + "\\digitalplatform.gcatclient.dll",
									Environment.CurrentDirectory + "\\dp2circulation.exe"
            };


            // ����Project��Script main.cs��Assembly
            // return:
            //		-2	���������Ѿ���ʾ��������Ϣ�ˡ�
            //		-1	����
            int nRet = ScriptManager.BuildAssembly(
                "OperHistory",
                strProjectName,
                "main.cs",
                saAddRef,
                strLibPaths,
                strMainCsDllName,
                out strError,
                out strWarning);
            if (nRet == -2)
                goto ERROR1;
            if (nRet == -1)
            {
                if (strWarning == "")
                    goto ERROR1;
                MessageBox.Show(this.MainForm, strWarning);
            }


            this.PrintAssembly = Assembly.LoadFrom(strMainCsDllName);
            if (this.PrintAssembly == null)
            {
                strError = "LoadFrom " + strMainCsDllName + " fail";
                goto ERROR1;
            }

            // �õ�Assembly��PrintHost������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                PrintAssembly,
                "dp2Circulation.PrintHost");
            if (entryClassType == null)
            {
                strError = "dp2Circulation.PrintHost������û���ҵ�";
                return -1;
            }

            // newһ��PrintHost��������
            this.PrintHostObj = (PrintHost)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            if (this.PrintHostObj == null)
            {
                strError = "new PrintHost���������ʧ��";
                return -1;
            }

            this.PrintHostObj.ProjectDir = strProjectLocate;
            this.PrintHostObj.InstanceDir = this.InstanceDir;
            return 0;
        ERROR1:
            return -1;
        }
    }

    class OneCall
    {
        public string name = "";
        public object func = null;
        public object [] parameters = null;
    }
}
