using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Remoting;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Script;
using DigitalPlatform.IO;
using DigitalPlatform.Xml;

namespace dp2Circulation
{
    /// <summary>
    /// ��־ͳ�ƴ�
    /// </summary>
    public partial class OperLogStatisForm : MyScriptForm
    {
        // bool Running = false;   // ����ִ������

        string m_strMainCsDllName = "";

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        public MainForm MainForm
        {
            get
            {
                return (MainForm)this.MdiParent;
            }
        }
        
        DigitalPlatform.Stop stop = null;
#endif

#if NO
        /// <summary>
        /// �ű�������
        /// </summary>
        public ScriptManager ScriptManager = new ScriptManager();
#endif

        OperLogStatis objStatis = null;
        Assembly AssemblyMain = null;

#if NO
        int AssemblyVersion 
        {
            get
            {
                return MainForm.OperLogStatisAssemblyVersion;
            }
            set
            {
                MainForm.OperLogStatisAssemblyVersion = value;
            }
        }
#endif

        /// <summary>
        /// ���캯��
        /// </summary>
        public OperLogStatisForm()
        {
            InitializeComponent();
        }

        private void OperLogStatisForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
#if NO
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            ScriptManager.CfgFilePath =
    this.MainForm.DataDir + "\\statis_projects.xml";

#if NO
            ScriptManager.applicationInfo = this.MainForm.AppInfo;
            ScriptManager.CfgFilePath =
                this.MainForm.DataDir + "\\statis_projects.xml";
            ScriptManager.DataDir = this.MainForm.DataDir;

            ScriptManager.CreateDefaultContent -= new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);
            ScriptManager.CreateDefaultContent += new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);

            try
            {
                ScriptManager.Load();
            }
            catch (FileNotFoundException)
            {
                // ���ر��� 2009/2/4 new add
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
#endif

            // ������
            this.textBox_projectName.Text = this.MainForm.AppInfo.GetString(
                "operlogstatisform",
                "projectname",
                "");

            // ��ʼ����
            this.dateControl_start.Text = this.MainForm.AppInfo.GetString(
                 "operlogstatisform",
                 "start_date",
                 "");

            // ��������
            this.dateControl_end.Text = this.MainForm.AppInfo.GetString(
                "operlogstatisform",
                "end_date",
                "");

            /*
            // ���������
            this.checkBox_startToEndTable.Checked = this.MainForm.AppInfo.GetBoolean(
                "operlogstatisform",
                "startToEndTable",
                true);
            this.checkBox_perYearTable.Checked = this.MainForm.AppInfo.GetBoolean(
                "operlogstatisform",
                "perYearTable",
                false);
            this.checkBox_perMonthTable.Checked = this.MainForm.AppInfo.GetBoolean(
                "operlogstatisform",
                "perMonthTable",
                false);
            this.checkBox_perDayTable.Checked = this.MainForm.AppInfo.GetBoolean(
                "operlogstatisform",
                "perDayTable",
                false);
             * */

        }

        private void OperLogStatisForm_FormClosing(object sender, FormClosingEventArgs e)
        {
#if NO
            if (stop != null)
            {
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }

            }
#endif
        }

        private void OperLogStatisForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            // ������
            this.MainForm.AppInfo.SetString(
                "operlogstatisform",
                "projectname",
                this.textBox_projectName.Text);

            // ��ʼ����
            this.MainForm.AppInfo.SetString(
                "operlogstatisform",
                "start_date",
                this.dateControl_start.Text);
            // ��������
            this.MainForm.AppInfo.SetString(
                "operlogstatisform",
                "end_date",
                this.dateControl_end.Text);

            /*
            // ���������
            this.MainForm.AppInfo.SetBoolean(
                "operlogstatisform",
                "startToEndTable",
                this.checkBox_startToEndTable.Checked);
            this.MainForm.AppInfo.SetBoolean(
                "operlogstatisform",
                "perYearTable",
                this.checkBox_perYearTable.Checked);
            this.MainForm.AppInfo.SetBoolean(
                "operlogstatisform",
                "perMonthTable",
                this.checkBox_perMonthTable.Checked);
            this.MainForm.AppInfo.SetBoolean(
                "operlogstatisform",
                "perDayTable",
                this.checkBox_perDayTable.Checked);
             * */

        }

        /*
        public bool OutputAllInOneTable
        {
            get
            {
                return this.checkBox_startToEndTable.Checked;
            }
        }

        public bool OutputYearTable
        {
            get
            {
                return this.checkBox_perYearTable.Checked;
            }
        }
        public bool OutputMonthTable
        {
            get
            {
                return this.checkBox_perMonthTable.Checked;
            }
        }
        public bool OutputDayTable
        {
            get
            {
                return this.checkBox_perDayTable.Checked;
            }
        }
         * */

#if NO
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }
#endif

#if NO
        private void scriptManager_CreateDefaultContent(object sender, CreateDefaultContentEventArgs e)
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
#endif
        internal override void CreateDefaultContent(CreateDefaultContentEventArgs e)
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
        // 
        /// <summary>
        /// ����ȱʡ�� main.cs �ļ�
        /// </summary>
        /// <param name="strFileName">�ļ�ȫ·��</param>
        static void CreateDefaultMainCsFile(string strFileName)
        {
            StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8);
            sw.WriteLine("using System;");
            sw.WriteLine("using System.Windows.Forms;");
            sw.WriteLine("using System.IO;");
            sw.WriteLine("using System.Text;");
            sw.WriteLine("using System.Xml;");
            sw.WriteLine("");

            //sw.WriteLine("using DigitalPlatform.MarcDom;");
            //sw.WriteLine("using DigitalPlatform.Statis;");
            sw.WriteLine("using dp2Circulation;");

            sw.WriteLine("using DigitalPlatform.Xml;");

            sw.WriteLine("public class MyStatis : OperLogStatis");

            sw.WriteLine("{");

            sw.WriteLine("	public override void OnBegin(object sender, StatisEventArgs e)");
            sw.WriteLine("	{");
            sw.WriteLine("	}");

            sw.WriteLine("}");
            sw.Close();
        }

        // ��������
        private void button_projectManage_Click(object sender, EventArgs e)
        {
            ProjectManageDlg dlg = new ProjectManageDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.ProjectsUrl = "http://dp2003.com/dp2circulation/projects/projects.xml";
            dlg.HostName = "OperLogStatisForm";
            dlg.scriptManager = this.ScriptManager;
            dlg.AppInfo = this.MainForm.AppInfo;
            dlg.DataDir = this.MainForm.DataDir;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.button_getProjectName.Enabled = bEnable;

            this.dateControl_start.Enabled = bEnable;
            this.dateControl_end.Enabled = bEnable;

            this.button_next.Enabled = bEnable;

            this.button_projectManage.Enabled = bEnable;
        }

        int RunScript(string strProjectName,
            string strProjectLocate,
            out string strError)
        {
            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ִ�нű� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {

                int nRet = 0;
                strError = "";

                // Assembly assemblyMain = null;

                this.objStatis = null;
                this.AssemblyMain = null;

                // 2009/11/5 changed
                // ��ֹ��ǰ�����Ĵ򿪵��ļ���Ȼû�йر�
                Global.ForceGarbageCollection();

                /*
                AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(CurrentDomain_AssemblyResolve);
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
                */

                nRet = PrepareScript(strProjectName,
                    strProjectLocate,
                    // out assemblyMain,
                    out objStatis,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;


                /*
                 * 
                 * 
                string strDllName = "";
                nRet = PrepareScript(strProjectName,
                    strProjectLocate,
                    out strDllName,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                System.AppDomain NewAppDomain = System.AppDomain.CreateDomain("NewApplicationDomain");

                ObjectHandle h = NewAppDomain.CreateInstanceFrom(strDllName,
                    "scriptcode.MyStatis");
                objStatis = (Statis)h.Unwrap();

                m_strMainCsDllName = strDllName;

                // ΪStatis���������ò���
                objStatis.OperLogStatisForm = this;
                objStatis.ProjectDir = strProjectLocate;
                 * */


                // this.AssemblyMain = assemblyMain;

                objStatis.ProjectDir = strProjectLocate;
                objStatis.Console = this.Console;
                objStatis.StartDate = this.dateControl_start.Value;
                objStatis.EndDate = this.dateControl_end.Value;


                // ִ�нű���OnInitial()

                // ����Script��OnInitial()����
                // OnInitial()��OnBegin�ı�������, ����OnInitial()�ʺϼ�������������
                if (objStatis != null)
                {
                    StatisEventArgs args = new StatisEventArgs();
                    objStatis.OnInitial(this, args);
                    if (args.Continue == ContinueType.SkipAll)
                        goto END1;
                }


                // ����Script��OnBegin()����
                // OnBegin()����Ȼ���޸�MainForm��������
                if (objStatis != null)
                {
                    StatisEventArgs args = new StatisEventArgs();
                    objStatis.OnBegin(this, args);
                    if (args.Continue == ContinueType.SkipAll)
                        goto END1;
                }

                // ѭ��
                nRet = DoLoop(DoRecord, out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (nRet == 1)
                    goto END1;  // TODO: SkipAll���ִ��? �Ƿ���OnEndҲ��ִ���ˣ�

            END1:
                // ����Script��OnEnd()����
                if (objStatis != null)
                {
                    StatisEventArgs args = new StatisEventArgs();
                    objStatis.OnEnd(this, args);
                }

                return 0;

            ERROR1:
                return -1;

            }
            catch (Exception ex)
            {
                strError = "�ű�ִ�й����׳��쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
                return -1;
            }
            finally
            {
                if (objStatis != null)
                    objStatis.FreeResources();

                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                stop.HideProgress();

                this.AssemblyMain = null;

                EnableControls(true);
            }
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Debug.Assert(false, "");

            string strName = args.Name;

            // return this.AssemblyMain;
            return Assembly.LoadFile(m_strMainCsDllName);

            // return null;
        }

        int DoRecord(string strLogFileName,
    string strXml,
    bool bInCacheFile,
    long lHint,
    long lIndex,
    long lAttachmentTotalLength,
    object param,
    out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(strXml) == true)
                return 0;

            string strDate = "";
            int nRet = strLogFileName.IndexOf(".");
            if (nRet != -1)
                strDate = strLogFileName.Substring(0, nRet);
            else
                strDate = strLogFileName;

            DateTime currentDate = DateTimeUtil.Long8ToDateTime(strDate);
            // strXml��Ϊ��־��¼

            // ����Script��OnRecord()����
            if (objStatis != null)
            {
                objStatis.Xml = strXml;
                objStatis.CurrentDate = currentDate;
                objStatis.CurrentLogFileName = strLogFileName;
                objStatis.CurrentRecordIndex = lIndex;

                StatisEventArgs args = new StatisEventArgs();
                objStatis.OnRecord(this, args);
                if (args.Continue == ContinueType.SkipAll)
                    return 1;
            }

            return 0;
        }



        // ��ÿ����־�ļ���ÿ����־��¼����ѭ��
        // return:
        //      0   ��ͨ����
        //      1   Ҫȫ���ж�
        int DoLoop(
            OperLogForm.Delegate_doRecord procDoRecord,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            // long lRet = 0;

            List<string> LogFileNames = null;

            // TODO: �Ƿ���Ҫ�����ֹ�����Ƿ�Ϊ��ֵ����ֵ�Ǿ��滹�Ǿ͵������죿

            string strStartDate = DateTimeUtil.DateTimeToString8(this.dateControl_start.Value);
            string strEndDate = DateTimeUtil.DateTimeToString8(this.dateControl_end.Value);

            string strWarning = "";

            // �������ڷ�Χ��������־�ļ���
            // parameters:
            //      strStartDate    ��ʼ���ڡ�8�ַ�
            //      strEndDate  �������ڡ�8�ַ�
            // return:
            //      -1  ����
            //      0   �ɹ�
            nRet = MakeLogFileNames(strStartDate,
                strEndDate,
                true,
                out LogFileNames,
                out strWarning,
                out strError);
            if (nRet == -1)
                return -1;

            if (String.IsNullOrEmpty(strWarning) == false)
                MessageBox.Show(this, strWarning);

            string strStyle = "";
            if (this.MainForm.AutoCacheOperlogFile == true)
                strStyle = "autocache";

            ProgressEstimate estimate = new ProgressEstimate();

            nRet = OperLogForm.ProcessFiles(this,
stop,
estimate,
Channel,
LogFileNames,
this.MainForm.OperLogLevel,
strStyle,
this.MainForm.OperLogCacheDir,
null,   // param,
procDoRecord,   // DoRecord,
out strError);
            if (nRet == -1)
                return -1;

            return nRet;
        }

        // ׼���ű�����
        int PrepareScript(string strProjectName,
            string strProjectLocate,
            // out Assembly assemblyMain,
            out OperLogStatis objStatis,
            out string strError)
        {
            this.AssemblyMain = null;

            objStatis = null;

            string strWarning = "";
            string strMainCsDllName = PathUtil.MergePath(this.InstanceDir, "\\~operlog_statis_main_" + Convert.ToString(AssemblyVersion++) + ".dll");    // ++

            string strLibPaths = "\"" + this.MainForm.DataDir + "\""
                + ","
                + "\"" + strProjectLocate + "\"";

            string[] saAddRef = {
                                    // 2011/4/20 ����
                                    "system.dll",
                                    "system.drawing.dll",
                                    "system.windows.forms.dll",
                                    "system.xml.dll",
                                    "System.Runtime.Serialization.dll",

									//Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.Client.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.library.dll",
									// Environment.CurrentDirectory + "\\digitalplatform.statis.dll",
									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
   									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Script.dll",  // 2011/8/25 ����
									// Environment.CurrentDirectory + "\\digitalplatform.dp2.statis.dll",
									// Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
									Environment.CurrentDirectory + "\\dp2circulation.exe",
            };


            // ����Project��Script main.cs��Assembly
            // return:
            //		-2	���������Ѿ���ʾ��������Ϣ�ˡ�
            //		-1	����
            int nRet = ScriptManager.BuildAssembly(
                "OperLogStatisForm",
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
                MessageBox.Show(this, strWarning);
            }


            this.AssemblyMain = Assembly.LoadFrom(strMainCsDllName);
            if (this.AssemblyMain == null)
            {
                strError = "LoadFrom " + strMainCsDllName + " fail";
                goto ERROR1;
            }

            // �õ�Assembly��Statis������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                this.AssemblyMain,
                "dp2Circulation.OperLogStatis");

            if (entryClassType == null)
            {
                strError = strMainCsDllName + "��û���ҵ� dp2Circulation.OperLogStatis �����ࡣ";
                goto ERROR1;
            }

            // newһ��Statis��������
            objStatis = (OperLogStatis)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            /*
            this.AssemblyMain = Assembly.LoadFrom(strMainCsDllName);
            if (this.AssemblyMain == null)
            {
                strError = "LoadFrom " + strMainCsDllName + " fail";
                goto ERROR1;
            }

            // �õ�Assembly��Statis������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                this.AssemblyMain,
                "dp2Circulation.OperLogStatis");


            objStatis = (Statis)AppDomain.CurrentDomain.CreateInstanceAndUnwrap(this.AssemblyMain.FullName,
                entryClassType.FullName);

            // assemblyMain = null;

            this.m_strMainCsDllName = strMainCsDllName;
             * */



            // ΪStatis���������ò���
            objStatis.OperLogStatisForm = this;
            objStatis.ProjectDir = strProjectLocate;
            objStatis.InstanceDir = this.InstanceDir;

            return 0;
        ERROR1:
            return -1;
        }


        // ׼���ű�����(2)
        int PrepareScript(string strProjectName,
            string strProjectLocate,
            out string strMainCsDllName,
            out string strError)
        {
            this.AssemblyMain = null;
            string strWarning = "";

            strMainCsDllName = strProjectLocate + "\\~main_" + Convert.ToString(AssemblyVersion) + ".dll";    // ++

            string strLibPaths = "\"" + this.MainForm.DataDir + "\""
                + ","
                + "\"" + strProjectLocate + "\"";

            string[] saAddRef = {
                                    // 2011/4/20 ����
                                    "system.dll",
                                    "system.drawing.dll",
                                    "system.windows.forms.dll",
                                    "system.xml.dll",
                                    "System.Runtime.Serialization.dll",

									//Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.Client.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.library.dll",
									// Environment.CurrentDirectory + "\\digitalplatform.statis.dll",
									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
   									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Script.dll",  // 2011/8/25 ����
									// Environment.CurrentDirectory + "\\digitalplatform.dp2.statis.dll",
									// Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
									Environment.CurrentDirectory + "\\dp2circulation.exe",
            };


            // ����Project��Script main.cs��Assembly
            // return:
            //		-2	���������Ѿ���ʾ��������Ϣ�ˡ�
            //		-1	����
            int nRet = ScriptManager.BuildAssembly(
                "OperLogStatisForm",
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
                MessageBox.Show(this, strWarning);
            }



            return 0;
        ERROR1:
            return -1;
        }

        // �������ڷ�Χ��������־�ļ���
        // parameters:
        //      strStartDate    ��ʼ���ڡ�8�ַ�
        //      strEndDate  �������ڡ�8�ַ�
        // return:
        //      -1  ����
        //      0   �ɹ�
        /// <summary>
        /// �������ڷ�Χ��������־�ļ���
        /// </summary>
        /// <param name="strStartDate">��ʼ���ڡ�8�ַ�</param>
        /// <param name="strEndDate">�������ڡ�8�ַ�</param>
        /// <param name="bExt">�Ƿ������չ�� ".log"</param>
        /// <param name="LogFileNames">���ش������ļ���</param>
        /// <param name="strWarning">���ؾ�����Ϣ</param>
        /// <param name="strError">���ش�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public static int MakeLogFileNames(string strStartDate,
            string strEndDate,
            bool bExt,  // �Ƿ������չ�� ".log"
            out List<string> LogFileNames,
            out string strWarning,
            out string strError)
        {
            LogFileNames = new List<string>();
            strError = "";
            strWarning = "";
            int nRet = 0;

            if (String.Compare(strStartDate, strEndDate) > 0)
            {
                strError = "��ʼ���� '" + strStartDate + "' ��Ӧ���ڽ������� '" + strEndDate + "'��";
                return -1;
            }

            string strLogFileName = strStartDate;

            for (; ; )
            {
                LogFileNames.Add(strLogFileName + (bExt == true ? ".log" : ""));

                string strNextLogFileName = "";
                // ��ã������ϣ���һ����־�ļ���
                // return:
                //      -1  error
                //      0   ��ȷ
                //      1   ��ȷ������strLogFileName�Ѿ��ǽ����������
                nRet = NextLogFileName(strLogFileName,
                    out strNextLogFileName,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (nRet == 1)
                {
                    if (String.Compare(strLogFileName, strEndDate) < 0)
                    {
                        strWarning = "�����ڷ�Χ��β�� " + strEndDate + " ��������(" + DateTime.Now.ToLongDateString() + ")���������ڱ���ȥ...";
                        break;
                    }
                }

                strLogFileName = strNextLogFileName;
                if (String.Compare(strLogFileName, strEndDate) > 0)
                    break;
            }

            return 0;
        }

        // ��ã������ϣ���һ����־�ļ���
        // return:
        //      -1  error
        //      0   ��ȷ
        //      1   ��ȷ������strLogFileName�Ѿ��ǽ����������
        static int NextLogFileName(string strLogFileName,
            out string strNextLogFileName,
            out string strError)
        {
            strError = "";
            strNextLogFileName = "";
            int nRet = 0;

            string strYear = strLogFileName.Substring(0, 4);
            string strMonth = strLogFileName.Substring(4, 2);
            string strDay = strLogFileName.Substring(6, 2);

            int nYear = 0;
            int nMonth = 0;
            int nDay = 0;

            try
            {
                nYear = Convert.ToInt32(strYear);
            }
            catch
            {
                strError = "��־�ļ��� '" + strLogFileName + "' �е� '"
                    + strYear + "' ���ָ�ʽ����";
                return -1;
            }

            try
            {
                nMonth = Convert.ToInt32(strMonth);
            }
            catch
            {
                strError = "��־�ļ��� '" + strLogFileName + "' �е� '"
                    + strMonth + "' ���ָ�ʽ����";
                return -1;
            }

            try
            {
                nDay = Convert.ToInt32(strDay);
            }
            catch
            {
                strError = "��־�ļ��� '" + strLogFileName + "' �е� '"
                    + strDay + "' ���ָ�ʽ����";
                return -1;
            }

            DateTime time = DateTime.Now;
            try
            {
                time = new DateTime(nYear, nMonth, nDay);
            }
            catch (Exception ex)
            {
                strError = "���� " + strLogFileName + " ��ʽ����: " + ex.Message;
                return -1;
            }

            DateTime now = DateTime.Now;

            // ���滯ʱ��
            nRet = RoundTime("day",
                ref now,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = RoundTime("day",
                ref time,
                out strError);
            if (nRet == -1)
                return -1;

            bool bNow = false;
            if (time >= now)
                bNow = true;

            time = time + new TimeSpan(1, 0, 0, 0); // ����һ��

            strNextLogFileName = time.Year.ToString().PadLeft(4, '0')
            + time.Month.ToString().PadLeft(2, '0')
            + time.Day.ToString().PadLeft(2, '0');

            if (bNow == true)
                return 1;

            return 0;
        }

        // ����ʱ�䵥λ,��ʱ��ֵ��ͷȥ��,���滯,���ں��������
        /*public*/ static int RoundTime(string strUnit,
            ref DateTime time,
            out string strError)
        {
            strError = "";

            time = time.ToLocalTime();
            if (strUnit == "day")
            {
                time = new DateTime(time.Year, time.Month, time.Day,
                    12, 0, 0, 0);
            }
            else if (strUnit == "hour")
            {
                time = new DateTime(time.Year, time.Month, time.Day,
                    time.Hour, 0, 0, 0);
            }
            else
            {
                strError = "δ֪��ʱ�䵥λ '" + strUnit + "'";
                return -1;
            }
            time = time.ToUniversalTime();

            return 0;
        }

        // ��һ�� ��ť
        private void button_next_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.tabControl_main.SelectedTab == this.tabPage_selectProject)
            {
                string strProjectName = this.textBox_projectName.Text;

                if (String.IsNullOrEmpty(strProjectName) == true)
                {
                    strError = "��δָ��������";
                    this.textBox_projectName.Focus();
                    goto ERROR1;
                }


                // �л���ʱ�䷶Χpage
                this.tabControl_main.SelectedTab = this.tabPage_timeRange;
                return;
            }

            if (this.tabControl_main.SelectedTab == this.tabPage_timeRange)
            {
                // ������������Ƿ�Ϊ�գ��ʹ�С��ϵ
                if (this.dateControl_start.Value == new DateTime((long)0))
                {
                    strError = "��δָ����ʼ����";
                    this.dateControl_start.Focus();
                    goto ERROR1;
                }

                if (this.dateControl_end.Value == new DateTime((long)0))
                {
                    strError = "��δָ����������";
                    this.dateControl_end.Focus();
                    goto ERROR1;
                }

                if (this.dateControl_start.Value.Ticks > this.dateControl_end.Value.Ticks)
                {
                    strError = "��ʼ���ڲ��ܴ��ڽ�������";
                    goto ERROR1;
                }

                string strProjectName = this.textBox_projectName.Text;
                if (String.IsNullOrEmpty(strProjectName) == true)
                {
                    strError = "��δָ��������";
                    this.textBox_projectName.Focus();
                    goto ERROR1;
                }

#if NO
                if (this.textBox_projectName.Text[0] == '#')
                {
                    nRet = DoTask1(out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    return;
                }
#endif

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
                    strError = "���� " + strProjectName + " û���ҵ�...";
                    this.tabControl_main.SelectedTab = this.tabPage_selectProject;
                    goto ERROR1;
                }
                if (nRet == -1)
                {
                    strError = "scriptManager.GetProjectData() error ...";
                    goto ERROR1;
                }

                // �л���ִ��page
                this.tabControl_main.SelectedTab = this.tabPage_runStatis;

                this.Running = true;
                try
                {

                    nRet = RunScript(strProjectName,
                        strProjectLocate,
                        out strError);

                    if (nRet == -1)
                        goto ERROR1;
                }
                finally
                {
                    this.Running = false;
                }

                this.tabControl_main.SelectedTab = this.tabPage_runStatis;
                MessageBox.Show(this, "ͳ����ɡ�");

                return;
            }



            if (this.tabControl_main.SelectedTab == this.tabPage_runStatis)
            {
                // �л���...
                this.tabControl_main.SelectedTab = this.tabPage_print;

                this.button_next.Enabled = false;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��÷�����
        private void button_getProjectName_Click(object sender, EventArgs e)
        {
            // ���ֶԻ���ѯ��Project����
            GetProjectNameDlg dlg = new GetProjectNameDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.scriptManager = this.ScriptManager;
            dlg.ProjectName = this.textBox_projectName.Text;
            dlg.NoneProject = false;

            this.MainForm.AppInfo.LinkFormState(dlg, "GetProjectNameDlg_state");
            dlg.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.textBox_projectName.Text = dlg.ProjectName;
        }

        /// <summary>
        /// ���������Ϣ�Ŀ���̨(������ؼ�)
        /// </summary>
        public WebBrowser Console
        {
            get
            {
                return this.webBrowser1_running;
            }
        }

        private void button_print_Click(object sender, EventArgs e)
        {
            if (this.objStatis == null)
            {
                MessageBox.Show(this, "��δִ��ͳ�ƣ��޷���ӡ");
                return;
            }

            HtmlPrintForm printform = new HtmlPrintForm();

            printform.Text = "��ӡͳ�ƽ��";
            printform.MainForm = this.MainForm;

            Debug.Assert(this.objStatis != null, "");
            printform.Filenames = this.objStatis.OutputFileNames;
            this.MainForm.AppInfo.LinkFormState(printform, "printform_state");
            printform.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(printform);
        }

        private void tabControl_main_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.Running == true)
                return;

            if (this.tabControl_main.SelectedTab == this.tabPage_print)
            {
                this.button_next.Enabled = false;
                return;
            }

            this.button_next.Enabled = true;

        }

        // 
        /// <summary>
        /// ��ö��߼�¼
        /// </summary>
        /// <param name="strReaderBarcode">����֤�����</param>
        /// <param name="strResultTypeList">��������б�</param>
        /// <param name="results">���ؽ���ַ�������</param>
        /// <param name="strRecPath">���ض��߼�¼·��</param>
        /// <param name="baTimestamp">���ض��߼�¼ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û���ҵ�; 1: �ҵ�; >1: ���ж��� 1 ��</returns>
        public int GetReaderInfo(string strReaderBarcode,
            string strResultTypeList,
            out string [] results,
            out string strRecPath,
            out byte [] baTimestamp,
            out string strError)
        {
            long lRet = Channel.GetReaderInfo(
                stop,
                strReaderBarcode,
                strResultTypeList,
                out results,
                out strRecPath,
                out baTimestamp,
                out strError);
            return (int)lRet;
        }


        //
        /// <summary>
        /// ��ȡ��ĿժҪ
        /// </summary>
        /// <param name="strItemBarcode">�������</param>
        /// <param name="strConfirmItemRecPath">(������ŷ����ظ�ʱ)����ȷ�ϵĲ��¼·��</param>
        /// <param name="strBiblioRecPathExclude">Ҫ�ų�����Ŀ��¼·���б��ö��ż���������б��е���Щ��Ŀ��¼·��, �ŷ���ժҪ����, �������������Ŀ��¼·��</param>
        /// <param name="strBiblioRecPath">������Ŀ��¼·��</param>
        /// <param name="strSummary">������ĿժҪ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û���ҵ�; 1: �ҵ�</returns>
        public int GetBiblioSummary(string strItemBarcode,
            string strConfirmItemRecPath,
            string strBiblioRecPathExclude,
            out string strBiblioRecPath,
            out string strSummary,
            out string strError)
        {
            long lRet = Channel.GetBiblioSummary(
                stop,
                strItemBarcode,
                strConfirmItemRecPath,
                strBiblioRecPathExclude,
                out strBiblioRecPath,
                out strSummary,
                out strError);
            return (int)lRet;
        }

        // 2012/10/6
        // ��ò��¼����ĿժҪ
        /// <summary>
        /// ��ȡ��ĿժҪ
        /// </summary>
        /// <param name="strItemBarcode">�������</param>
        /// <param name="nMaxLength">��ĿժҪ������ַ�����-1 ��ʾ���ضϡ���������ַ�������ĿժҪ���ضϣ�ĩβ���"..."</param>
        /// <returns>��ĿժҪ�ַ���</returns>
        public string GetItemSummary(string strItemBarcode,
            int nMaxLength = -1)
        {
            string strSummary = "";
            string strBiblioRecPath = "";
            string strError = "";
            int nRet = GetBiblioSummary(strItemBarcode,
                "",
                "",
                out strBiblioRecPath,
                out strSummary,
                out strError);
            if (nRet == -1)
                return strError;

            if (nMaxLength == -1 || strSummary.Length <= nMaxLength)
                return strSummary;

            return strSummary.Substring(0, nMaxLength) + "...";
        }

        // 2012/10/6
        // ��ö���ժҪ
        /// <summary>
        /// ��ö���ժҪ
        /// </summary>
        /// <param name="strPatronBarcode">����֤�����</param>
        /// <returns>����ժҪ</returns>
        public string GetPatronSummary(string strPatronBarcode)
        {
            string strError = "";
            string strSummary = "";

            int nRet = strPatronBarcode.IndexOf("|");
            if (nRet != -1)
                return "֤������ַ��� '" + strPatronBarcode + "' �в�Ӧ���������ַ�";


            // ����cache���Ƿ��Ѿ�����
            StringCacheItem item = null;
            item = this.MainForm.SummaryCache.SearchItem(
                "P:" + strPatronBarcode);   // ǰ׺��Ϊ�˺Ͳ����������
            if (item != null)
            {
                Application.DoEvents();
                strSummary = item.Content;
                return strSummary;
            }

            string strXml = "";
            string[] results = null;
            long lRet = Channel.GetReaderInfo(stop,
                strPatronBarcode,
                "xml",
                out results,
                out strError);
            if (lRet == -1)
            {
                strSummary = strError;
                return strSummary;
            }
            else if (lRet > 1)
            {
                strSummary = "����֤����� " + strPatronBarcode + " ���ظ���¼ " + lRet.ToString() + "��";
                return strSummary;
            }

            // 2012/10/1
            if (lRet == 0)
                return "";  // not found

            Debug.Assert(results.Length > 0, "");
            strXml = results[0];

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strSummary = "���߼�¼XMLװ��DOMʱ����: " + ex.Message;
                return strSummary;
            }

            // ��������
            strSummary = DomUtil.GetElementText(dom.DocumentElement,
                "name");

            // ���cache��û�У������cache
            item = this.MainForm.SummaryCache.EnsureItem(
                "P:" + strPatronBarcode);
            item.Content = strSummary;

            return strSummary;
        }

        // 
        /// <summary>
        /// ��ȡ��Ŀ��¼�ľֲ�
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strBiblioXml">��Ŀ��¼ XML</param>
        /// <param name="strPartName">�ֲ���</param>
        /// <param name="strResultValue">���ؽ���ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û���ҵ�; 1: �ҵ�</returns>
        public int GetBiblioPart(string strBiblioRecPath,
            string strBiblioXml,
            string strPartName,
            out string strResultValue,
            out string strError)
        {
                long lRet = Channel.GetBiblioInfo(
                    stop,
                    strBiblioRecPath,
                    strBiblioXml,
                    strPartName,    // ����'@'����
                    out strResultValue,
                    out strError);
                return (int)lRet;
        }

        // �������ǲ����ø�C#���ο����ű������õģ�
        /// <summary>
        /// ��ú�ֵ
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strMacroName">����</param>
        /// <returns>��ֵ</returns>
        public string GetMacroValue(
            string strBiblioRecPath,
            string strMacroName)
        {
            // return strMacroName + "--";
            string strError = "";
            string strResultValue = "";
            int nRet = 0;
            // ��ȡ��Ŀ��¼�ľֲ�
            nRet = GetBiblioPart(strBiblioRecPath,
                "", // strBiblioXml
                strMacroName,
                out strResultValue,
                out strError);
            if (nRet == -1)
            {
                if (String.IsNullOrEmpty(strResultValue) == true)
                    return strError;

                return strResultValue;
            }

            return strResultValue;
        }

        private void OperLogStatisForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);
        }

        private void comboBox_quickSetFilenames_SelectedIndexChanged(object sender, EventArgs e)
        {
            Delegate_QuickSetFilenames d = new Delegate_QuickSetFilenames(QuickSetFilenames);
            this.BeginInvoke(d, new object[] { sender });

        }

        delegate void Delegate_QuickSetFilenames(Control control);

        void QuickSetFilenames(Control control)
        {
            string strStartDate = "";
            string strEndDate = "";

            string strName = control.Text.Replace(" ", "").Trim();

            if (strName == "����")
            {
                DateTime now = DateTime.Now;

                strStartDate = DateTimeUtil.DateTimeToString8(now);
                strEndDate = DateTimeUtil.DateTimeToString8(now);
            }
            else if (strName == "����")
            {
                DateTime now = DateTime.Now;
                int nDelta = (int)now.DayOfWeek; // 0-6 sunday - saturday
                DateTime start = now - new TimeSpan(nDelta, 0, 0, 0);

                strStartDate = DateTimeUtil.DateTimeToString8(start);
                // strEndDate = DateTimeUtil.DateTimeToString8(start + new TimeSpan(7, 0,0,0));
                strEndDate = DateTimeUtil.DateTimeToString8(now);
            }
            else if (strName == "����")
            {
                DateTime now = DateTime.Now;
                strEndDate = DateTimeUtil.DateTimeToString8(now);
                strStartDate = strEndDate.Substring(0, 6) + "01";
            }
            else if (strName == "����")
            {
                DateTime now = DateTime.Now;
                strEndDate = DateTimeUtil.DateTimeToString8(now);
                strStartDate = strEndDate.Substring(0, 4) + "0101";
            }
            else if (strName == "�������" || strName == "���7��")
            {
                DateTime now = DateTime.Now;
                DateTime start = now - new TimeSpan(7 - 1, 0, 0, 0);

                strStartDate = DateTimeUtil.DateTimeToString8(start);
                strEndDate = DateTimeUtil.DateTimeToString8(now);
            }
            else if (strName == "�����ʮ��" || strName == "���30��")
            {
                DateTime now = DateTime.Now;
                DateTime start = now - new TimeSpan(30 - 1, 0, 0, 0);
                strStartDate = DateTimeUtil.DateTimeToString8(start);
                strEndDate = DateTimeUtil.DateTimeToString8(now);
            }
            else if (strName == "�����ʮһ��" || strName == "���31��")
            {
                DateTime now = DateTime.Now;
                DateTime start = now - new TimeSpan(31 - 1, 0, 0, 0);
                strStartDate = DateTimeUtil.DateTimeToString8(start);
                strEndDate = DateTimeUtil.DateTimeToString8(now);
            }
            else if (strName == "���������ʮ����" || strName == "���365��")
            {
                DateTime now = DateTime.Now;
                DateTime start = now - new TimeSpan(365 - 1, 0, 0, 0);
                strStartDate = DateTimeUtil.DateTimeToString8(start);
                strEndDate = DateTimeUtil.DateTimeToString8(now);
            }
            else if (strName == "���ʮ��" || strName == "���10��")
            {
                DateTime now = DateTime.Now;
                DateTime start = now - new TimeSpan(10 * 365 - 1, 0, 0, 0);
                strStartDate = DateTimeUtil.DateTimeToString8(start);
                strEndDate = DateTimeUtil.DateTimeToString8(now);
            }
            else
            {
                MessageBox.Show(this, "�޷�ʶ������� '" + strName + "'");
                return;
            }

            this.dateControl_start.Value = DateTimeUtil.Long8ToDateTime(strStartDate);
            this.dateControl_end.Value = DateTimeUtil.Long8ToDateTime(strEndDate);
        }

        // ����ͳ�Ʒ��� #1
        private void button_defaultProject_1_Click(object sender, EventArgs e)
        {
            this.textBox_projectName.Text = "#1";
        }


    }
}