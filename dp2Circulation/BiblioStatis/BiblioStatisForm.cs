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
using System.Web;   // HttpUtility

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Script;
using DigitalPlatform.IO;
using DigitalPlatform.Xml;
using DigitalPlatform.Marc;
using DigitalPlatform.MarcDom;

using DigitalPlatform.dp2.Statis;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ��Ŀͳ�ƴ�
    /// </summary>
    public partial class BiblioStatisForm : MyScriptForm
    {
        /// <summary>
        /// ��ȡ���κ�key+countֵ�б�
        /// </summary>
        public event GetKeyCountListEventHandler GetBatchNoTable = null;

        // public HtmlViewerForm ErrorInfoForm = null;

        // bool Running = false;   // ����ִ������

        /*
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        public MainForm MainForm = null;
        DigitalPlatform.Stop stop = null;
         * */

        // public ScriptManager ScriptManager = new ScriptManager();

        BiblioStatis objStatis = null;
        Assembly AssemblyMain = null;

        Assembly AssemblyFilter = null;
        MyFilterDocument MarcFilter = null;


        void DisposeBilbioStatisObject()
        {
            BiblioStatis temp = this.objStatis;
            this.objStatis = null;
            if (temp != null)
            {
                temp.Dispose();
                GC.WaitForPendingFinalizers();
            }
        }

#if NO
        public Stop Stop
        {
            get
            {
                return this.stop;
            }
        }
#endif


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

        /// <summary>
        /// ���캯��
        /// </summary>
        public BiblioStatisForm()
        {
            InitializeComponent();
        }

        private void BiblioStatisForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

            /*
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
             * */

            ScriptManager.CfgFilePath =
                this.MainForm.DataDir + "\\biblio_statis_projects.xml";

#if NO
            ScriptManager.applicationInfo = this.MainForm.AppInfo;
            ScriptManager.CfgFilePath =
                this.MainForm.DataDir + "\\biblio_statis_projects.xml";
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

            // batchno
            this.GetBatchNoTable -= new GetKeyCountListEventHandler(BiblioStatisForm_GetBatchNoTable);
            this.GetBatchNoTable += new GetKeyCountListEventHandler(BiblioStatisForm_GetBatchNoTable);

            this.radioButton_inputStyle_recPathFile.Checked = this.MainForm.AppInfo.GetBoolean(
                "bibliostatisform",
                "inputstyle_recpathfile",
                false);


            this.radioButton_inputStyle_biblioDatabase.Checked = this.MainForm.AppInfo.GetBoolean(
                "bibliostatisform",
                "inputstyle_bibliodatabase",
                true);

            this.radioButton_inputStyle_recPaths.Checked = this.MainForm.AppInfo.GetBoolean(
    "bibliostatisform",
    "inputstyle_recpaths",
    false);


            // ����ļ�¼·���ļ���
            this.textBox_inputRecPathFilename.Text = this.MainForm.AppInfo.GetString(
                "bibliostatisform",
                "input_recpath_filename",
                "");


            // �������Ŀ����
            this.comboBox_inputBiblioDbName.Text = this.MainForm.AppInfo.GetString(
                "bibliostatisform",
                "input_bibliodbname",
                "<ȫ��>");

            // ������
            this.textBox_projectName.Text = this.MainForm.AppInfo.GetString(
                "bibliostatisform",
                "projectname",
                "");


            // ��¼·��
            this.textBox_inputStyle_recPaths.Text = this.MainForm.AppInfo.GetString(
                "bibliostatisform",
                "recpaths",
                "").Replace(",","\r\n");

        }

        void BiblioStatisForm_GetBatchNoTable(object sender, GetKeyCountListEventArgs e)
        {
            Global.GetBatchNoTable(e,
                this,
                "", // ����ͼ����ڿ�
                "biblio",
                this.stop,
                this.Channel);

#if NOOOOOOOOOOOOOOOOOOOOOOOOOO
            string strError = "";

            if (e.KeyCounts == null)
                e.KeyCounts = new List<KeyCount>();

            EnableControls(false);
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("�����г�ȫ����Ŀ���κ� ...");
            stop.BeginLoop();

            try
            {
                string strQueryXml = "";

                long lRet = Channel.SearchBiblio(
                    stop,
                    "<all>",    // ���ܿ����� this.comboBox_inputBiblioDbName.Text, �Ա��ú�������Ŀ����ص����κ�ʵ�����������������᣺��Ϊ���ݿ����б�ˢ�º�����ȴ����ˢ�£�
                    "", // strBatchNo,
                    2000,   // -1,    // nPerMax
                    "batchno",
                    "left",
                    this.Lang,
                    "batchno",   // strResultSetName
                    "keycount", // strOutputStyle
                    out strQueryXml,
                    out strError);
                /*
                long lRet = Channel.SearchItem(
                    stop,
                    "<all>",
                    "", // strBatchNo
                    -1,
                    "���κ�",
                    "left",
                    this.Lang,
                    "batchno",   // strResultSetName
                    "keycount", // strOutputStyle
                    out strError);
                 * */
                if (lRet == -1)
                    goto ERROR1;

                if (lRet == 0)
                {
                    strError = "û���ҵ��κα�Ŀ���κż�����";
                    return;
                }

                long lHitCount = lRet;

                long lStart = 0;
                long lCount = lHitCount;
                SearchResult[] searchresults = null;

                // װ�������ʽ
                for (; ; )
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            MessageBox.Show(this, "�û��ж�");
                            return;
                        }
                    }

                    lRet = Channel.GetSearchResult(
                        stop,
                        "batchno",   // strResultSetName
                        lStart,
                        lCount,
                        "keycount",
                        this.Lang,
                        out searchresults,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "GetSearchResult() error: " + strError;
                        goto ERROR1;
                    }

                    if (lRet == 0)
                    {
                        // MessageBox.Show(this, "δ����");
                        return;
                    }

                    // ����������
                    for (int i = 0; i < searchresults.Length; i++)
                    {
                        if (searchresults[i].Cols == null)
                        {
                            strError = "�����Ӧ�÷����������ݿ��ں˵����°汾";
                            goto ERROR1;
                        }

                        KeyCount keycount = new KeyCount();
                        keycount.Key = searchresults[i].Path;
                        keycount.Count = Convert.ToInt32(searchresults[i].Cols[0]);
                        e.KeyCounts.Add(keycount);
                    }

                    lStart += searchresults.Length;
                    lCount -= searchresults.Length;

                    stop.SetMessage("������ " + lHitCount.ToString() + " ������װ�� " + lStart.ToString() + " ��");

                    if (lStart >= lHitCount || lCount <= 0)
                        break;
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
        }

        private void BiblioStatisForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            /*
            if (stop != null)
            {
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }

            }
             * */

        }

        private void BiblioStatisForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {

                this.MainForm.AppInfo.SetBoolean(
                    "bibliostatisform",
                    "inputstyle_recpathfile",
                    this.radioButton_inputStyle_recPathFile.Checked);


                this.MainForm.AppInfo.SetBoolean(
                    "bibliostatisform",
                    "inputstyle_bibliodatabase",
                    this.radioButton_inputStyle_biblioDatabase.Checked);

                this.MainForm.AppInfo.SetBoolean(
    "bibliostatisform",
    "inputstyle_recpaths",
    this.radioButton_inputStyle_recPaths.Checked);

                // ����ļ�¼·���ļ���
                this.MainForm.AppInfo.SetString(
                    "bibliostatisform",
                    "input_recpath_filename",
                    this.textBox_inputRecPathFilename.Text);

                // �������Ŀ����
                this.MainForm.AppInfo.SetString(
                    "bibliostatisform",
                    "input_bibliodbname",
                    this.comboBox_inputBiblioDbName.Text);

                // ������
                this.MainForm.AppInfo.SetString(
                    "bibliostatisform",
                    "projectname",
                    this.textBox_projectName.Text);

                // ��¼·��
                this.MainForm.AppInfo.SetString(
                    "bibliostatisform",
                    "recpaths",
                    this.textBox_inputStyle_recPaths.Text.Replace("\r\n", ","));
            }

        }

        internal override void CreateDefaultContent(CreateDefaultContentEventArgs e)
        {
            string strPureFileName = Path.GetFileName(e.FileName);

            if (String.Compare(strPureFileName, "main.cs", true) == 0)
            {
                CreateDefaultMainCsFile(e.FileName);
                e.Created = true;
            }
            else if (String.Compare(strPureFileName, "marcfilter.fltx", true) == 0)
            {
                CreateDefaultMarcFilterFile(e.FileName);
                e.Created = true;
            }
            else
            {
                e.Created = false;
            }
        }

#if NO
        private void scriptManager_CreateDefaultContent(object sender, CreateDefaultContentEventArgs e)
        {
            string strPureFileName = Path.GetFileName(e.FileName);

            if (String.Compare(strPureFileName, "main.cs", true) == 0)
            {
                CreateDefaultMainCsFile(e.FileName);
                e.Created = true;
            }
            else if (String.Compare(strPureFileName, "marcfilter.fltx", true) == 0)
            {
                CreateDefaultMarcFilterFile(e.FileName);
                e.Created = true;
            }
            else
            {
                e.Created = false;
            }

        }
#endif

        // ����ȱʡ��main.cs�ļ�
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


            sw.WriteLine("public class MyStatis : BiblioStatis");

            sw.WriteLine("{");

            sw.WriteLine("	public override void OnBegin(object sender, StatisEventArgs e)");
            sw.WriteLine("	{");
            sw.WriteLine("	}");

            sw.WriteLine("}");
            sw.Close();
        }

        // ����ȱʡ��marcfilter.fltx�ļ�
        static void CreateDefaultMarcFilterFile(string strFileName)
        {
            StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8);

            sw.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
            sw.WriteLine("<filter>");
            sw.WriteLine("<using>");
            sw.WriteLine("<![CDATA[");
            sw.WriteLine("using System;");
            sw.WriteLine("using System.IO;");
            sw.WriteLine("using System.Text;");
            sw.WriteLine("using System.Windows.Forms;");
            sw.WriteLine("using DigitalPlatform.MarcDom;");
            sw.WriteLine("using DigitalPlatform.Marc;");

            sw.WriteLine("using dp2Circulation;");

            sw.WriteLine("]]>");
            sw.WriteLine("</using>");
            sw.WriteLine("	<record>");
            sw.WriteLine("		<def>");
            sw.WriteLine("		<![CDATA[");
            sw.WriteLine("			int i;");
            sw.WriteLine("			int j;");
            sw.WriteLine("		]]>");
            sw.WriteLine("		</def>");
            sw.WriteLine("		<begin>");
            sw.WriteLine("		<![CDATA[");
            sw.WriteLine("			MessageBox.Show(\"record data:\" + this.Data);");
            sw.WriteLine("		]]>");
            sw.WriteLine("		</begin>");
            sw.WriteLine("			 <field name=\"200\">");
            sw.WriteLine("");
            sw.WriteLine("			 </field>");
            sw.WriteLine("		<end>");
            sw.WriteLine("		<![CDATA[");
            sw.WriteLine("");
            sw.WriteLine("			j ++;");
            sw.WriteLine("		]]>");
            sw.WriteLine("		</end>");
            sw.WriteLine("	</record>");
            sw.WriteLine("</filter>");

            sw.Close();
        }

        private void button_projectManage_Click(object sender, EventArgs e)
        {
            ProjectManageDlg dlg = new ProjectManageDlg();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.ProjectsUrl = "http://dp2003.com/dp2circulation/projects/projects.xml";
            dlg.HostName = "BiblioStatisForm";
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

            // this.checkBox_departmentTable.Enabled = bEnable;

            this.button_next.Enabled = bEnable;

            this.button_projectManage.Enabled = bEnable;
        }

        int RunScript(string strProjectName,
            string strProjectLocate,
            string strInitialParamString,
            out string strError,
            out string strWarning)
        {
            strError = "";
            strWarning = "";

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
                strWarning = "";

                this.DisposeBilbioStatisObject();
                // this.objStatis = null;

                this.AssemblyMain = null;
                MyFilterDocument filter = null;

                // 2009/11/5 new add
                // ��ֹ��ǰ�����Ĵ򿪵��ļ���Ȼû�йر�
                Global.ForceGarbageCollection();


                nRet = PrepareScript(strProjectName,
                    strProjectLocate,
                    out this.objStatis,
                    out filter,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                //
                if (filter != null)
                    this.AssemblyFilter = filter.Assembly;
                else
                    this.AssemblyFilter = null;

                this.MarcFilter = filter;
                //


                objStatis.ProjectDir = strProjectLocate;
                objStatis.Console = this.Console;

                // ִ�нű���OnInitial()

                // ����Script��OnInitial()����
                // OnInitial()��OnBegin�ı�������, ����OnInitial()�ʺϼ�������������
                if (objStatis != null)
                {
                    StatisEventArgs args = new StatisEventArgs();
                    args.ParamString = strInitialParamString;
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
                nRet = DoLoop(out strError,
                    out strWarning);
                if (nRet == -1)
                    goto ERROR1;

                if (nRet == 1)
                    goto END1;  // ʵ���� SkipAll ��Ҫִ�� OnEnd() �ģ��� Error ���ǲ�ִ�� OnEnd()

            END1:
                // ����Script��OnEnd()����
                if (objStatis != null)
                {
                    StatisEventArgs args = new StatisEventArgs();
                    objStatis.OnEnd(this, args);
                    if (args.Continue == ContinueType.Error)
                    {
                        strError = args.ParamString;
                        return -1;
                    }
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

                this.AssemblyMain = null;

                EnableControls(true);
            }
        }

        // ׼���ű�����
        int PrepareScript(string strProjectName,
            string strProjectLocate,
            out BiblioStatis objStatisParam,
            out MyFilterDocument filter,
            out string strError)
        {
            this.AssemblyMain = null;

            objStatisParam = null;
            filter = null;

            string strWarning = "";

            /*
            string strInstanceDir = PathUtil.MergePath(strProjectLocate, "~bin_" + Guid.NewGuid().ToString());
            PathUtil.CreateDirIfNeed(strInstanceDir);
             * */

            string strMainCsDllName = PathUtil.MergePath(this.InstanceDir, "\\~biblio_statis_main_" + Convert.ToString(AssemblyVersion++) + ".dll");    // ++

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

									Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.rms.Client.dll",
									//Environment.CurrentDirectory + "\\digitalplatform.library.dll",
									// Environment.CurrentDirectory + "\\digitalplatform.statis.dll",
									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
   									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Script.dll",  // 2011/8/25 ����
									Environment.CurrentDirectory + "\\digitalplatform.dp2.statis.dll",
									// Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
									Environment.CurrentDirectory + "\\dp2circulation.exe",
            };


            // ����Project��Script main.cs��Assembly
            // return:
            //		-2	���������Ѿ���ʾ��������Ϣ�ˡ�
            //		-1	����
            int nRet = ScriptManager.BuildAssembly(
                "BiblioStatisForm",
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
                "dp2Circulation.BiblioStatis");
            if (entryClassType == null)
            {
                strError = strMainCsDllName + "��û���ҵ� dp2Circulation.BiblioStatis �����ࡣ";
                goto ERROR1;
            }
            // newһ��Statis��������
            objStatisParam = (BiblioStatis)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            // ΪStatis���������ò���
            objStatisParam.BiblioStatisForm = this;
            objStatisParam.ProjectDir = strProjectLocate;
            objStatisParam.InstanceDir = this.InstanceDir;


            ////

            ////////////////////////////
            // װ��marfilter.fltx
            string strFilterFileName = strProjectLocate + "\\marcfilter.fltx";

            if (FileUtil.FileExist(strFilterFileName) == true)
            {
                filter = new MyFilterDocument();
                filter.BiblioStatis = objStatisParam;
                filter.strOtherDef = entryClassType.FullName + " BiblioStatis = null;";


                filter.strPreInitial = " MyFilterDocument doc = (MyFilterDocument)this.Document;\r\n";
                filter.strPreInitial += " BiblioStatis = ("
                    + entryClassType.FullName + ")doc.BiblioStatis;\r\n";

                try
                {
                    filter.Load(strFilterFileName);
                }
                catch (Exception ex)
                {
                    strError = "�ļ� " + strFilterFileName + " װ�ص�MarcFilterʱ��������: " + ex.Message;
                    goto ERROR1;
                }

                nRet = filter.BuildScriptFile(strProjectLocate + "\\marcfilter.fltx.cs",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // һЩ��Ҫ�����ӿ�
                string[] saAddRef1 = {
										 Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
										 //Environment.CurrentDirectory + "\\digitalplatform.rms.client.dll",
										 //Environment.CurrentDirectory + "\\digitalplatform.library.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
										 Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
										 // Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
										 Environment.CurrentDirectory + "\\dp2circulation.exe",
										 strMainCsDllName};

                // fltx�ļ�����ʽ���������ӿ�
                string [] saAdditionalRef = filter.GetRefs();

                // �ϲ������ӿ�
                string [] saTotalFilterRef = new string[saAddRef1.Length + saAdditionalRef.Length];
                Array.Copy(saAddRef1, saTotalFilterRef, saAddRef1.Length);
                Array.Copy(saAdditionalRef, 0,
                    saTotalFilterRef, saAddRef1.Length,
                    saAdditionalRef.Length);


                string strfilterCsDllName = strProjectLocate + "\\~marcfilter_" + Convert.ToString(AssemblyVersion++) + ".dll";

                // ����Project��Script��Assembly
                nRet = ScriptManager.BuildAssembly(
                    "BiblioStatisForm",
                    strProjectName,
                    "marcfilter.fltx.cs",
                    saTotalFilterRef,
                    strLibPaths,
                    strfilterCsDllName,
                    out strError,
                    out strWarning);
                if (nRet == -2)
                    goto ERROR1;
                if (nRet == -1)
                {
                    if (strWarning == "")
                    {
                        goto ERROR1;
                    }
                    MessageBox.Show(this, strWarning);
                }

                Assembly assemblyFilter = null;

                assemblyFilter = Assembly.LoadFrom(strfilterCsDllName);
                if (assemblyFilter == null)
                {
                    strError = "LoadFrom " + strfilterCsDllName + "fail";
                    goto ERROR1;
                }


                filter.Assembly = assemblyFilter;

            }


            return 0;
        ERROR1:
            return -1;
        }

        // ע�⣺�ϼ�����RunScript()�Ѿ�ʹ����BeginLoop()��EnableControls()
        // ��ÿ����Ŀ��¼����ѭ��
        // return:
        //      0   ��ͨ����
        //      1   Ҫȫ���ж�
        int DoLoop(out string strError,
            out string strWarning)
        {
            strError = "";
            strWarning = "";

            int nRet = 0;
            long lRet = 0;

            bool bSyntaxWarned = false;
            bool bFilterWarned = false;

            // List<string> LogFileNames = null;

            // ���������Ϣ�����в��������
            ClearErrorInfoForm();

            /*
            // �ݲصص�����б�
            string strLocationList = this.textBox_locationNames.Text;
            if (String.IsNullOrEmpty(strLocationList) == true)
                strLocationList = "*";

            string[] locations = strLocationList.Split(new char[] { ',' });

            StringMatchList location_matchlist = new StringMatchList(locations);

            // �������͹����б�
            string strItemTypeList = this.textBox_itemTypes.Text;
            if (String.IsNullOrEmpty(strItemTypeList) == true)
                strItemTypeList = "*";

            string[] itemtypes = strItemTypeList.Split(new char[] { ',' });

            StringMatchList itemtype_matchlist = new StringMatchList(itemtypes);
             * */

            // ��¼·����ʱ�ļ�
            string strTempRecPathFilename = Path.GetTempFileName();

            string strInputFileName = "";   // �ⲿ�ƶ��������ļ���Ϊ������ļ����߼�¼·���ļ���ʽ
            string strAccessPointName = "";

            try
            {

                if (this.InputStyle == BiblioStatisInputStyle.BatchNo)
                {
                    nRet = SearchBiblioRecPath(
                        this.tabComboBox_inputBatchNo.Text,
                        strTempRecPathFilename,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    strInputFileName = strTempRecPathFilename;
                    strAccessPointName = "��¼·��";
                }
                else if (this.InputStyle == BiblioStatisInputStyle.RecPathFile)
                {
                    strInputFileName = this.textBox_inputRecPathFilename.Text;
                    strAccessPointName = "��¼·��";
                }
                else if (this.InputStyle == BiblioStatisInputStyle.RecPaths)
                {
                    using (StreamWriter sw = new StreamWriter(strTempRecPathFilename, false, Encoding.UTF8))
                    {
                        sw.Write(this.textBox_inputStyle_recPaths.Text);
                    }

                    strInputFileName = strTempRecPathFilename;
                    strAccessPointName = "��¼·��";
                }
                else
                {
                    Debug.Assert(false, "");
                }

                StreamReader sr = null;

                try
                {
                    sr = new StreamReader(strInputFileName, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    strError = "���ļ� " + strInputFileName + " ʧ��: " + ex.Message;
                    return -1;
                }

                this.progressBar_records.Minimum = 0;
                this.progressBar_records.Maximum = (int)sr.BaseStream.Length;
                this.progressBar_records.Value = 0;

                /*
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڻ�ȡ��Ŀ��¼ ...");
                stop.BeginLoop();

                EnableControls(false);
                 * */

                try
                {
                    int nCount = 0;

                    for (int i = 0; ; i++)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                DialogResult result = MessageBox.Show(this,
                                    "׼���жϡ�\r\n\r\nȷʵҪ�ж�ȫ������? (Yes ȫ���жϣ�No �ж�ѭ�������Ǽ�����β����Cancel �����жϣ���������)",
                                    "bibliostatisform",
                                    MessageBoxButtons.YesNoCancel,
                                    MessageBoxIcon.Question,
                                    MessageBoxDefaultButton.Button3);

                                if (result == DialogResult.Yes)
                                {
                                    strError = "�û��ж�";
                                    return -1;
                                }
                                if (result == DialogResult.No)
                                    return 0;   // ��װloop��������

                                stop.Continue(); // ����ѭ��
                            }
                        }

                        // string strItemBarcode = barcodes[i];
                        string strRecPath = sr.ReadLine();

                        if (strRecPath == null)
                            break;

                        strRecPath = strRecPath.Trim();
                        nRet = strRecPath.IndexOf("\t");
                        if (nRet != -1)
                            strRecPath = strRecPath.Substring(0, nRet).Trim();

                        if (String.IsNullOrEmpty(strRecPath) == true)
                            continue;

                        stop.SetMessage("���ڻ�ȡ�� " + (i + 1).ToString() + " ����Ŀ��¼��" + strAccessPointName + "Ϊ " + strRecPath);
                        this.progressBar_records.Value = (int)sr.BaseStream.Position;

                        // �����Ŀ��¼
                        // string strOutputRecPath = "";
                        // byte[] baTimestamp = null;


                        string strAccessPoint = "";
                        if (this.InputStyle == BiblioStatisInputStyle.BatchNo)
                            strAccessPoint = strRecPath;
                        else if (this.InputStyle == BiblioStatisInputStyle.RecPathFile)
                            strAccessPoint = strRecPath;
                        else if (this.InputStyle == BiblioStatisInputStyle.RecPaths)
                            strAccessPoint = strRecPath;
                        else
                        {
                            Debug.Assert(false, "");
                        }

                        string strBiblio = "";
                        // string strBiblioRecPath = "";

#if NO
                        // Result.Value -1���� 0û���ҵ� 1�ҵ� >1���ж���1��
                        lRet = Channel.GetBiblioInfo(
                            stop,
                            strAccessPoint,
                            "", // strBiblioXml
                            "xml",   // strResultType
                            out strBiblio,
                            out strError);
#endif
                        string[] formats = new string[1];
                        formats[0] = "xml";
                        string[] results = null;
                        byte[] baTimestamp = null;
                        lRet = Channel.GetBiblioInfos(
                            stop,
                            strAccessPoint,
                    "",
                            formats,
                            out results,
                            out baTimestamp,
                            out strError);

                        if (lRet == -1)
                        {
                            strError = "�����Ŀ��¼ " + strAccessPoint + " ʱ��������: " + strError;
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                            continue;
                        }

                        if (lRet == 0)
                        {
                            strError = "��Ŀ��¼" + strAccessPointName + " " + strRecPath + " ��Ӧ��XML����û���ҵ���";
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                            continue;
                        }

                        if (lRet > 1)
                        {
                            strError = "��Ŀ��¼" + strAccessPointName + " " + strRecPath + " ��Ӧ���ݶ���һ����";
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                            continue;
                        }

                        if (results == null || results.Length == 0)
                        {
                            strError = "��Ŀ��¼" + strAccessPointName + " " + strRecPath + " ��ȡʱ results ����";
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                            continue;
                        }
                        strBiblio = results[0];
                        objStatis.Timestamp = baTimestamp;


                        string strXml = "";

                        strXml = strBiblio;


                        // �����Ƿ���ϣ��ͳ�Ƶķ�Χ��
                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(strXml);
                        }
                        catch (Exception ex)
                        {
                            strError = "��Ŀ��¼װ��DOM��������: " + ex.Message;
                            continue;
                        }

                        /*
                        // ���չݲصص�ɸѡ
                        if (this.textBox_locationNames.Text != ""
                            && this.textBox_locationNames.Text != "*")
                        {
                            // ע�����ַ�������"*"��ʾʲô�����㡣Ҳ�͵��ڲ�ʹ�ô�ɸѡ��

                            string strLocation = DomUtil.GetElementText(dom.DocumentElement,
                                "location");
                            if (location_matchlist.Match(strLocation) == false)
                                continue;
                        }

                        // ���ղ�����ɸѡ
                        if (this.textBox_itemTypes.Text != ""
                            && this.textBox_itemTypes.Text != "*")
                        {
                            // ע�����ַ�������"*"��ʾʲô�����㡣Ҳ�͵��ڲ�ʹ�ô�ɸѡ��

                            string strItemType = DomUtil.GetElementText(dom.DocumentElement,
                                "bookType");
                            if (itemtype_matchlist.Match(strItemType) == false)
                                continue;
                        }
                         * */

                        // Debug.Assert(false, "");

                        // strXml��Ϊ��Ŀ��¼
                        string strBiblioDbName = Global.GetDbName(strRecPath);

                        string strSyntax = this.MainForm.GetBiblioSyntax(strBiblioDbName);
                        if (String.IsNullOrEmpty(strSyntax) == true)
                            strSyntax = "unimarc";

                        bool bItemDomsCleared = false;

                        if (strSyntax == "usmarc" || strSyntax == "unimarc")
                        {
                            // ��XML��Ŀ��¼ת��ΪMARC��ʽ
                            string strOutMarcSyntax = "";
                            string strMarc = "";

                            // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
                            // parameters:
                            //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
                            //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
                            //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
                            nRet = MarcUtil.Xml2Marc(strXml,
                                true,   // 2013/1/12 �޸�Ϊtrue
                                "", // strMarcSyntax
                                out strOutMarcSyntax,
                                out strMarc,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            if (String.IsNullOrEmpty(strOutMarcSyntax) == false)
                            {
                                if (strOutMarcSyntax != strSyntax
                                    && bSyntaxWarned == false)
                                {
                                    strWarning += "��Ŀ��¼ " + strRecPath + " ��syntax '" + strOutMarcSyntax + "' �����������ݿ� '" + strBiblioDbName + "' �Ķ���syntax '" + strSyntax + "' ��һ��\r\n";
                                    bSyntaxWarned = true;
                                }
                            }

                            objStatis.MarcRecord = strMarc;


                            if (this.MarcFilter != null)
                            {
                                // ����Script��PreFilter()����
                                if (objStatis != null)
                                {
                                    objStatis.Xml = strXml;
                                    objStatis.BiblioDom = dom;
                                    objStatis.CurrentDbSyntax = strSyntax;  // strOutputMarcSyntax?
                                    objStatis.CurrentRecPath = strRecPath;
                                    objStatis.CurrentRecordIndex = i;
                                    bItemDomsCleared = true;
                                    objStatis.ClearItemDoms();
                                    objStatis.ClearOrderDoms();
                                    objStatis.ClearIssueDoms();
                                    objStatis.ClearCommentDoms();

                                    StatisEventArgs args = new StatisEventArgs();
                                    objStatis.PreFilter(this, args);
                                    if (args.Continue == ContinueType.SkipAll)
                                        return 1;
                                }

                                // ����filter�е�Record��ض���
                                nRet = this.MarcFilter.DoRecord(
                                    null,
                                    objStatis.MarcRecord,
                                    strOutMarcSyntax,   // 2012/9/6
                                    i,
                                    out strError);
                                if (nRet == -1)
                                    return -1;
                            }
                        }
                        else
                        {
                            objStatis.MarcRecord = "";

                            if (this.MarcFilter != null
                                && bFilterWarned == false)
                            {
                                // TODO: �Ƿ���Ҫ���棿��Ϊ������filter, ������Ϊ���漰�Ŀⲻ��MARC��ʽ��û�а취Ӧ��
                                // ��������о���һ��
                                strWarning += "��ǰͳ�Ʒ�����������MarcFilter��������Ϊ���ݿ� '" + strBiblioDbName + "' (���ܲ���������һ�����ݿ�)�Ķ���syntax '" + strSyntax + "' ����MARC���ʽ������ͳ�ƹ��������ٶ�������޷�����MarcFilter���ܡ�\r\n";
                                bFilterWarned = true;
                            }
                        }


                        // ����Script��OnRecord()����
                        if (objStatis != null)
                        {
                            objStatis.Xml = strXml;
                            objStatis.BiblioDom = dom;
                            objStatis.CurrentDbSyntax = strSyntax;  // strOutputMarcSyntax?
                            objStatis.CurrentRecPath = strRecPath;
                            objStatis.CurrentRecordIndex = i;
                            if (bItemDomsCleared == false)
                            {
                                objStatis.ClearItemDoms();
                                objStatis.ClearOrderDoms();
                                objStatis.ClearIssueDoms();
                                objStatis.ClearCommentDoms();
                                bItemDomsCleared = true;
                            }

                            StatisEventArgs args = new StatisEventArgs();
                            objStatis.OnRecord(this, args);
                            if (args.Continue == ContinueType.SkipAll)
                                return 1;
                        }

                        nCount++;
                    }

                }
                finally
                {
                    /*
                    EnableControls(true);

                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                     * */

                    if (sr != null)
                        sr.Close();
                }
            }
            finally
            {
                File.Delete(strTempRecPathFilename);
            }


            return 0;
        }

        // ע�⣺�ϼ�����RunScript()�Ѿ�ʹ����BeginLoop()��EnableControls()
        // ��������ض����κţ�����������Ŀ��¼·��(������ļ�)
        int SearchBiblioRecPath(
            string strBatchNo,
            string strRecPathFilename,
            out string strError)
        {
            strError = "";

            // �����ļ�
            StreamWriter sw = new StreamWriter(strRecPathFilename,
                false,	// append
                System.Text.Encoding.UTF8);

            try
            {
                /*
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڼ��� ...");
                stop.BeginLoop();

                EnableControls(false);
                 * */

                try
                {
                    long lRet = 0;
                    string strQueryXml = "";

                    // ��ָ�����κţ���ζ���ض���ȫ������
                    if (String.IsNullOrEmpty(strBatchNo) == true)
                    {
                        lRet = Channel.SearchBiblio(stop,
                             this.comboBox_inputBiblioDbName.Text,
                             "",
                             -1,    // nPerMax
                             "recid",
                             "left",
                             this.Lang,
                             null,   // strResultSetName
                             "",    // strSearchStyle
                            "", // strOutputStyle
                             out strQueryXml,
                             out strError);
                        if (lRet == -1)
                            goto ERROR1;
                    }
                    else
                    {
                        // ָ�����κš��ض��⡣
                        lRet = Channel.SearchBiblio(stop,
                             this.comboBox_inputBiblioDbName.Text,
                             strBatchNo,
                             -1,    // nPerMax
                             "batchno",
                             "exact",
                             this.Lang,
                             null,   // strResultSetName
                             "",    // strSearchStyle
                            "", // strOutputStyle
                             out strQueryXml,
                             out strError);
                        if (lRet == -1)
                            goto ERROR1;
                    }

                    long lHitCount = lRet;

                    long lStart = 0;
                    long lCount = lHitCount;


                    DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

                    // װ�������ʽ
                    for (; ; )
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null && stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }

                        lRet = Channel.GetSearchResult(
                            stop,
                            null,   // strResultSetName
                            lStart,
                            lCount,
                            "id",   // "id,cols",
                            this.Lang,
                            out searchresults,
                            out strError);
                        if (lRet == -1)
                            goto ERROR1;

                        if (lRet == 0)
                        {
                            strError = "δ����";
                            goto ERROR1;
                        }

                        Debug.Assert(searchresults != null, "");


                        // ����������
                        for (int i = 0; i < searchresults.Length; i++)
                        {
                            sw.Write(searchresults[i].Path + "\r\n");
                        }


                        lStart += searchresults.Length;
                        lCount -= searchresults.Length;

                        stop.SetMessage("���м�¼ " + lHitCount.ToString() + " �����ѻ�ü�¼ " + lStart.ToString() + " ��");

                        if (lStart >= lHitCount || lCount <= 0)
                            break;
                    }

                }
                finally
                {
                    /*
                    EnableControls(true);

                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                     * */
                }
            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }

            return 0;
        ERROR1:
            return -1;
        }

        private void button_next_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strWarning = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_source)
            {
                if (this.radioButton_inputStyle_recPathFile.Checked == true)
                {
                    if (this.textBox_inputRecPathFilename.Text == "")
                    {
                        strError = "��δָ������ļ�¼·���ļ���";
                        goto ERROR1;
                    }
                }
                else if (this.radioButton_inputStyle_recPaths.Checked == true)
                {
                    if (this.textBox_inputStyle_recPaths.Text == "")
                    {
                        strError = "��δָ����¼·��(ÿ��һ��)";
                        goto ERROR1;
                    }
                }
                else
                {
                    if (this.comboBox_inputBiblioDbName.Text == "")
                    {
                        strError = "��δָ����Ŀ����";
                        goto ERROR1;
                    }
                }

                // �л�����������page
                this.tabControl_main.SelectedTab = this.tabPage_filter;
                return;

            }

            if (this.tabControl_main.SelectedTab == this.tabPage_filter)
            {


                // �л���ִ��ѡ�񷽰���page
                this.tabControl_main.SelectedTab = this.tabPage_selectProject;
                return;
            }

            if (this.tabControl_main.SelectedTab == this.tabPage_selectProject)
            {
                string strProjectName = this.textBox_projectName.Text;

                if (String.IsNullOrEmpty(strProjectName) == true)
                {
                    strError = "��δָ��������";
                    this.textBox_projectName.Focus();
                    goto ERROR1;
                }

                string strProjectLocate = "";
                // ��÷�������
                // strProjectNamePath	������������·��
                // return:
                //		-1	error
                //		0	not found project
                //		1	found
                int nRet = this.ScriptManager.GetProjectData(
                    strProjectName,
                    out strProjectLocate);

                if (nRet == 0)
                {
                    strError = "���� " + strProjectName + " û���ҵ�...";
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
                        "", // strInitialParamString
                        out strError,
                        out strWarning);

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

            if (String.IsNullOrEmpty(strWarning) == false)
                MessageBox.Show(this, "����: \r\n" + strWarning);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
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

        private void button_print_Click(object sender, EventArgs e)
        {
            /*
            if (this.objStatis == null)
            {
                MessageBox.Show(this, "��δִ��ͳ�ƣ��޷���ӡ");
                return;
            }*/

            HtmlPrintForm printform = new HtmlPrintForm();

            printform.Text = "��ӡͳ�ƽ��";
            printform.MainForm = this.MainForm;
            if (this.objStatis != null)
                printform.Filenames = this.objStatis.OutputFileNames;
            else
                printform.Filenames = null;
            this.MainForm.AppInfo.LinkFormState(printform, "printform_state");
            printform.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(printform);

        }

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

        private void radioButton_inputStyle_recPathFile_CheckedChanged(object sender, EventArgs e)
        {
            SetInputPanelEnabled();
        }

        private void radioButton_inputStyle_readerDatabase_CheckedChanged(object sender, EventArgs e)
        {
            SetInputPanelEnabled();
        }

        void SetInputPanelEnabled()
        {
            if (this.radioButton_inputStyle_recPathFile.Checked == true)
            {
                this.textBox_inputRecPathFilename.Enabled = true;
                this.button_findInputRecPathFilename.Enabled = true;

                this.tabComboBox_inputBatchNo.Enabled = false;
                this.comboBox_inputBiblioDbName.Enabled = false;

                this.textBox_inputStyle_recPaths.Enabled = false;
            }
            else if (this.radioButton_inputStyle_recPaths.Checked == true)
            {
                this.textBox_inputRecPathFilename.Enabled = false;
                this.button_findInputRecPathFilename.Enabled = false;

                this.tabComboBox_inputBatchNo.Enabled = false;
                this.comboBox_inputBiblioDbName.Enabled = false;

                this.textBox_inputStyle_recPaths.Enabled = true;
            }
            else
            {
                this.textBox_inputRecPathFilename.Enabled = false;
                this.button_findInputRecPathFilename.Enabled = false;

                this.tabComboBox_inputBatchNo.Enabled = true;
                this.comboBox_inputBiblioDbName.Enabled = true;

                this.textBox_inputStyle_recPaths.Enabled = false;
            }
        }

        // ������
        /// <summary>
        /// ���뷽ʽ
        /// </summary>
        public BiblioStatisInputStyle InputStyle
        {
            get
            {
                if (this.radioButton_inputStyle_recPathFile.Checked == true)
                    return BiblioStatisInputStyle.RecPathFile;
                else if (this.radioButton_inputStyle_recPaths.Checked == true)
                    return BiblioStatisInputStyle.RecPaths;
                else
                    return BiblioStatisInputStyle.BatchNo;
            }
            set
            {
                if (value == BiblioStatisInputStyle.RecPathFile)
                    this.radioButton_inputStyle_recPathFile.Checked = true;
                else if (value == BiblioStatisInputStyle.BatchNo)
                    this.radioButton_inputStyle_biblioDatabase.Checked = true;
                else if (value == BiblioStatisInputStyle.RecPaths)
                    this.radioButton_inputStyle_recPaths.Checked = true;
            }
        }

        private void button_findInputRecPathFilename_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵���Ŀ��¼·���ļ���";
            dlg.FileName = this.textBox_inputRecPathFilename.Text;
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_inputRecPathFilename.Text = dlg.FileName;

        }

        private void comboBox_inputBiblioDbName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_inputBiblioDbName.Items.Count > 0)
                return;

            this.comboBox_inputBiblioDbName.Items.Add("<ȫ��>");

            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty prop = this.MainForm.BiblioDbProperties[i];

                    this.comboBox_inputBiblioDbName.Items.Add(prop.DbName);
                }
            }

        }

#if NO
        // ��ô�����Ϣ��
        HtmlViewerForm GetErrorInfoForm()
        {
            if (this.ErrorInfoForm == null
                || this.ErrorInfoForm.IsDisposed == true
                || this.ErrorInfoForm.IsHandleCreated == false)
            {
                this.ErrorInfoForm = new HtmlViewerForm();
                this.ErrorInfoForm.ShowInTaskbar = false;
                this.ErrorInfoForm.Text = "������Ϣ";
                this.ErrorInfoForm.Show(this);
                this.ErrorInfoForm.WriteHtml("<pre>");  // ׼���ı����
            }

            return this.ErrorInfoForm;
        }


        // ���������Ϣ�����в��������
        void ClearErrorInfoForm()
        {
            if (this.ErrorInfoForm != null)
            {
                try
                {
                    this.ErrorInfoForm.HtmlString = "<pre>";
                }
                catch
                {
                }
            }
        }
#endif

        private void BiblioStatisForm_Activated(object sender, EventArgs e)
        {
            // MyForm�����Ѿ�����
            // this.MainForm.stopManager.Active(this.stop);
        }

        int m_nInDropDown = 0;

        private void tabComboBox_inputBatchNo_DropDown(object sender, EventArgs e)
        {
            // ��ֹ����
            if (this.m_nInDropDown > 0)
                return;

            ComboBox combobox = (ComboBox)sender;
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {
                if (combobox.Items.Count == 0
                    && this.GetBatchNoTable != null)
                {
                    GetKeyCountListEventArgs e1 = new GetKeyCountListEventArgs();
                    this.GetBatchNoTable(this, e1);

                    if (e1.KeyCounts != null)
                    {
                        for (int i = 0; i < e1.KeyCounts.Count; i++)
                        {
                            KeyCount item = e1.KeyCounts[i];
                            combobox.Items.Add(item.Key + "\t" + item.Count.ToString() + "��");
                        }
                    }
                    else
                    {
                        combobox.Items.Add("<not found>");
                    }
                }
            }
            finally
            {
                this.Cursor = oldCursor;
                this.m_nInDropDown--;
            }
        }

        private void radioButton_inputStyle_recPaths_CheckedChanged(object sender, EventArgs e)
        {
            SetInputPanelEnabled();
        }

        // ���ż��
        /// <summary>
        /// ��������Դ������ҳ�еļ�¼·���б��ַ��������·���ö��ż��
        /// </summary>
        public string RecPathList
        {
            get
            {
                return this.textBox_inputStyle_recPaths.Text.Replace("\r\n", ",");
            }
            set
            {
                this.textBox_inputStyle_recPaths.Text = value.Replace(",", "\r\n");
            }
        }

        // �ṩ C# �ű�����
        /// <summary>
        /// ִ��ͳ�Ʒ���
        /// </summary>
        /// <param name="strProjectName">ͳ�Ʒ�����</param>
        /// <param name="strInitialParamString">��ʼ�������ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int RunProject(string strProjectName,
            string strInitialParamString,
            out string strError)
        {
            strError = "";
            string strWarning = "";

            if (String.IsNullOrEmpty(strProjectName) == true)
            {
                strError = "��δָ��������";
                this.textBox_projectName.Focus();
                goto ERROR1;
            }

            string strProjectLocate = "";
            // ��÷�������
            // strProjectNamePath	������������·��
            // return:
            //		-1	error
            //		0	not found project
            //		1	found
            int nRet = this.ScriptManager.GetProjectData(
                strProjectName,
                out strProjectLocate);

            if (nRet == 0)
            {
                strError = "���� " + strProjectName + " û���ҵ�...";
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
                    strInitialParamString,
                    out strError,
                    out strWarning);

                if (nRet == -1)
                    goto ERROR1;
            }
            finally
            {
                this.Running = false;
            }

            this.tabControl_main.SelectedTab = this.tabPage_runStatis;
            if (String.IsNullOrEmpty(strWarning) == false)
                MessageBox.Show(this, "����: \r\n" + strWarning);

            // MessageBox.Show(this, "ͳ����ɡ�");
            return 0;
        ERROR1:
            return -1;
        }

        /*
        // ����ʵ���¼
        // ������ˢ�½���ͱ���
        int SaveEntityRecords(string strBiblioRecPath,
            EntityInfo[] entities,
            out EntityInfo[] errorinfos,
            out string strError)
        {
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ������Ϣ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();


            try
            {
                long lRet = Channel.SetEntities(
                    stop,
                    strBiblioRecPath,
                    entities,
                    out errorinfos,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }
         * */

        // 
        /// <summary>
        /// ����XML��ʽ����Ŀ��¼�����ݿ�
        /// </summary>
        /// <param name="strPath">��¼·��</param>
        /// <param name="strXml">XML ��¼��</param>
        /// <param name="baTimestamp">ʱ���</param>
        /// <param name="strOutputPath">����ʵ�ʱ���ļ�¼·��</param>
        /// <param name="baNewTimestamp">��������ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 1: ����ɹ�</returns>
        public int SaveXmlBiblioRecordToDatabase(string strPath,
            string strXml,
            byte[] baTimestamp,
            out string strOutputPath,
            out byte[] baNewTimestamp,
            out string strError)
        {
            strError = "";
            baNewTimestamp = null;
            strOutputPath = "";


            string strAction = "change";

            if (Global.IsAppendRecPath(strPath) == true)
                strAction = "new";

            /*
            if (String.IsNullOrEmpty(strPath) == true)
                strAction = "new";
            else
            {
                string strRecordID = Global.GetRecordID(strPath);
                if (String.IsNullOrEmpty(strRecordID) == true
                    || strRecordID == "?")
                    strAction = "new";
            }
            */
            long lRet = Channel.SetBiblioInfo(
                stop,
                strAction,
                strPath,
                "xml",
                strXml,
                baTimestamp,
                "",
                out strOutputPath,
                out baNewTimestamp,
                out strError);
            if (lRet == -1)
            {
                strError = "������Ŀ��¼ '" + strPath + "' ʱ����: " + strError;
                goto ERROR1;
            }

            return 1;
        ERROR1:
            return -1;
        }
    }

    /// <summary>
    /// ��Ŀͳ�ƴ�������������
    /// </summary>
    public enum BiblioStatisInputStyle
    {
        /// <summary>
        /// ��¼·���ļ�
        /// </summary>
        RecPathFile = 1,    // ��¼·���ļ�
        /// <summary>
        /// ���κ� ������ȫ�������
        /// </summary>
        BatchNo = 2,    // ���κ� ������ȫ�������
        /// <summary>
        /// ��¼·��
        /// </summary>
        RecPaths = 3,   // ��¼·��
    }

    /// <summary>
    /// ������Ŀͳ�Ƶ� FilterDocument ������(MARC �������ĵ���)
    /// </summary>
    public class MyFilterDocument : FilterDocument
    {
        /// <summary>
        /// ��������
        /// </summary>
        public BiblioStatis BiblioStatis = null;
    }
}