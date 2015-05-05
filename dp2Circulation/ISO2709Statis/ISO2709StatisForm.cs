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
using DigitalPlatform.Text;

using DigitalPlatform.dp2.Statis;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ISO2709 ͳ�ƴ�
    /// </summary>
    public partial class Iso2709StatisForm : MyScriptForm
    {
        OpenMarcFileDlg _openMarcFileDialog = null;
        // public HtmlViewerForm ErrorInfoForm = null;

        // bool Running = false;   // ����ִ������

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        DigitalPlatform.Stop stop = null;
#endif

        // public ScriptManager ScriptManager = new ScriptManager();

        Iso2709Statis objStatis = null;
        Assembly AssemblyMain = null;

#if NO
        public Stop Stop
        {
            get
            {
                return this.stop;
            }
        }
#endif

#if NO
        public MainForm MainForm
        {
            get
            {
                return (MainForm)this.MdiParent;
            }
        }

        int AssemblyVersion
        {
            get
            {
                return MainForm.Iso2709StatisAssemblyVersion;
            }
            set
            {
                MainForm.Iso2709StatisAssemblyVersion = value;
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
        public Iso2709StatisForm()
        {
            InitializeComponent();

            _openMarcFileDialog = new OpenMarcFileDlg();
            _openMarcFileDialog.IsOutput = false;
            this.tabPage_source.Padding = new Padding(4, 4, 4, 4);
            this.tabPage_source.Controls.Add(_openMarcFileDialog.MainPanel);
            _openMarcFileDialog.MainPanel.Dock = DockStyle.Fill;

        }

        private void Iso2709StatisForm_Load(object sender, EventArgs e)
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
    this.MainForm.DataDir + "\\iso2709_statis_projects.xml";

#if NO
            ScriptManager.applicationInfo = this.MainForm.AppInfo;
            ScriptManager.CfgFilePath =
                this.MainForm.DataDir + "\\iso2709_statis_projects.xml";
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

            // �����ISO2709�ļ���
            this._openMarcFileDialog.FileName = this.MainForm.AppInfo.GetString(
                "iso2709statisform",
                "input_iso2709_filename",
                "");

            // ���뷽ʽ
            this._openMarcFileDialog.EncodingName = this.MainForm.AppInfo.GetString(
    "iso2709statisform",
    "input_iso2709_file_encoding",
    "");

            this._openMarcFileDialog.MarcSyntax = this.MainForm.AppInfo.GetString(
    "iso2709statisform",
    "input_marc_syntax",
    "unimarc");

            this._openMarcFileDialog.Mode880 = this.MainForm.AppInfo.GetBoolean(
    "iso2709statisform",
    "input_mode880",
    false);

            // ������
            this.textBox_projectName.Text = this.MainForm.AppInfo.GetString(
                "iso2709statisform",
                "projectname",
                "");


        }

        private void Iso2709StatisForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void Iso2709StatisForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            // �����ISO2709�ļ���
            this.MainForm.AppInfo.SetString(
                "iso2709statisform",
                "input_iso2709_filename",
                this._openMarcFileDialog.FileName);

            // ���뷽ʽ
            this.MainForm.AppInfo.SetString(
    "iso2709statisform",
    "input_iso2709_file_encoding",
    this._openMarcFileDialog.EncodingName);

            this.MainForm.AppInfo.SetString(
"iso2709statisform",
"input_marc_syntax",
this._openMarcFileDialog.MarcSyntax);

            this.MainForm.AppInfo.SetBoolean(
"iso2709statisform",
"input_mode880",
this._openMarcFileDialog.Mode880);

            // ������
            this.MainForm.AppInfo.SetString(
                "iso2709statisform",
                "projectname",
                this.textBox_projectName.Text);

#if NO
            if (this.ErrorInfoForm != null)
            {
                try
                {
                    this.ErrorInfoForm.Close();
                }
                catch
                {
                }
            }
#endif

        }

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

            sw.WriteLine("using dp2Circulation;");

            sw.WriteLine("using DigitalPlatform.Xml;");
            sw.WriteLine("using DigitalPlatform.Marc;");


            sw.WriteLine("public class MyStatis : Iso2709Statis");

            sw.WriteLine("{");

            sw.WriteLine("	public override void OnBegin(object sender, StatisEventArgs e)");
            sw.WriteLine("	{");
            sw.WriteLine("	}");


            sw.WriteLine("}");
            sw.Close();
        }

        private void button_projectManage_Click(object sender, EventArgs e)
        {
            ProjectManageDlg dlg = new ProjectManageDlg();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.ProjectsUrl = "http://dp2003.com/dp2circulation/projects/projects.xml";
            dlg.HostName = "Iso2709StatisForm";
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
            this._openMarcFileDialog.MainPanel.Enabled = bEnable;
            // this.button_findInputIso2709Filename.Enabled = bEnable;

            this.button_getProjectName.Enabled = bEnable;

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

                this.objStatis = null;
                this.AssemblyMain = null;

                // 2009/11/5 new add
                // ��ֹ��ǰ�����Ĵ򿪵��ļ���Ȼû�йر�
                Global.ForceGarbageCollection();

                nRet = PrepareScript(strProjectName,
                    strProjectLocate,
                    out objStatis,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                objStatis.ProjectDir = strProjectLocate;
                objStatis.Console = this.Console;
                objStatis.InputFilename = this._openMarcFileDialog.FileName;


                // ִ�нű���OnInitial()

                // ����Script��OnInitial()����
                // OnInitial()��OnBegin�ı�������, ����OnInitial()�ʺϼ�������������
                if (objStatis != null)
                {
                    StatisEventArgs args = new StatisEventArgs();
                    objStatis.OnInitial(this, args);
                    if (args.Continue == ContinueType.SkipAll)
                        goto END1;
                    if (args.Continue == ContinueType.Error)
                    {
                        strError = args.ParamString;
                        goto ERROR1;
                    }
                }


                // ����Script��OnBegin()����
                // OnBegin()����Ȼ���޸�MainForm��������
                if (objStatis != null)
                {
                    StatisEventArgs args = new StatisEventArgs();
                    objStatis.OnBegin(this, args);
                    if (args.Continue == ContinueType.SkipAll)
                        goto END1;
                    if (args.Continue == ContinueType.Error)
                    {
                        strError = args.ParamString;
                        goto ERROR1;
                    }
                }

                // ѭ��
                nRet = DoLoop(out strError);
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
                    if (args.Continue == ContinueType.Error)
                    {
                        strError = args.ParamString;
                        goto ERROR1;
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
            out Iso2709Statis objStatis,
            out string strError)
        {
            this.AssemblyMain = null;

            objStatis = null;

            string strWarning = "";
            string strMainCsDllName = PathUtil.MergePath(this.InstanceDir, "\\~iso2709_statis_main_" + Convert.ToString(AssemblyVersion++) + ".dll");    // ++

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

									Environment.CurrentDirectory + "\\digitalplatform.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Text.dll",
									Environment.CurrentDirectory + "\\digitalplatform.IO.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Xml.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marckernel.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll",
									Environment.CurrentDirectory + "\\digitalplatform.marcquery.dll",
   									Environment.CurrentDirectory + "\\digitalplatform.circulationclient.dll",
									Environment.CurrentDirectory + "\\digitalplatform.Script.dll",  // 2011/8/25 ����
									Environment.CurrentDirectory + "\\digitalplatform.dp2.statis.dll",
                Environment.CurrentDirectory + "\\dp2circulation.exe",
            };


            // ����Project��Script main.cs��Assembly
            // return:
            //		-2	���������Ѿ���ʾ��������Ϣ�ˡ�
            //		-1	����
            int nRet = ScriptManager.BuildAssembly(
                "Iso2709StatisForm",
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

            // �õ�Assembly��Iso2709Statis������Type
            Type entryClassType = ScriptManager.GetDerivedClassType(
                this.AssemblyMain,
                "dp2Circulation.Iso2709Statis");
            if (entryClassType == null)
            {
                strError = strMainCsDllName + "��û���ҵ� dp2Circulation.Iso2709Statis �����ࡣ";
                goto ERROR1;
            }
            // newһ��Iso2709Statis��������
            objStatis = (Iso2709Statis)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            // ΪIso2709Statis���������ò���
            objStatis.Iso2709StatisForm = this;
            objStatis.ProjectDir = strProjectLocate;
            objStatis.InstanceDir = this.InstanceDir;

            return 0;
        ERROR1:
            return -1;
        }

        // ע�⣺�ϼ�����RunScript()�Ѿ�ʹ����BeginLoop()��EnableControls()
        // ��ÿ��Iso2709Statis��¼����ѭ��
        // return:
        //      0   ��ͨ����
        //      1   Ҫȫ���ж�
        int DoLoop(out string strError)
        {
            strError = "";
            // int nRet = 0;
            // long lRet = 0;
            Encoding encoding = null;

            if (string.IsNullOrEmpty(this._openMarcFileDialog.EncodingName) == true)
            {
                strError = "��δѡ�� ISO2709 �ļ��ı��뷽ʽ";
                return -1;
            }

            if (StringUtil.IsNumber(this._openMarcFileDialog.EncodingName) == true)
                encoding = Encoding.GetEncoding(Convert.ToInt32(this._openMarcFileDialog.EncodingName));
            else
                encoding = Encoding.GetEncoding(this._openMarcFileDialog.EncodingName);

#if NO
            // ���������Ϣ�����в��������
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
#endif
            ClearErrorInfoForm();


            string strInputFileName = "";

            try
            {
                strInputFileName = this._openMarcFileDialog.FileName;

                Stream file = null;

                try
                {
                    file = File.Open(strInputFileName,
                        FileMode.Open,
                        FileAccess.Read);
                }
                catch (Exception ex)
                {
                    strError = "���ļ� " + strInputFileName + " ʧ��: " + ex.Message;
                    return -1;
                }

                this.progressBar_records.Minimum = 0;
                this.progressBar_records.Maximum = (int)file.Length;
                this.progressBar_records.Value = 0;

                /*
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.Initial("���ڻ�ȡISO2709��¼ ...");
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
                                    "Iso2709StatisForm",
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


                        string strMARC = "";

                        // ��ISO2709�ļ��ж���һ��MARC��¼
                        // return:
                        //	-2	MARC��ʽ��
                        //	-1	����
                        //	0	��ȷ
                        //	1	����(��ǰ���صļ�¼��Ч)
                        //	2	����(��ǰ���صļ�¼��Ч)
                        int nRet = MarcUtil.ReadMarcRecord(file,
                            encoding,
                            true,	// bRemoveEndCrLf,
                            true,	// bForce,
                            out strMARC,
                            out strError);
                        if (nRet == -2 || nRet == -1)
                        {
                            DialogResult result = MessageBox.Show(this,
                                "����MARC��¼(" + nCount.ToString() + ")����: " + strError + "\r\n\r\nȷʵҪ�жϵ�ǰ���������?",
                                "Iso2709StatisForm",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button2);
                            if (result == DialogResult.Yes)
                            {
                                break;
                            }
                            else
                            {
                                strError = "����MARC��¼(" + nCount.ToString() + ")����: " + strError;
                                GetErrorInfoForm().WriteHtml(strError + "\r\n");
                                continue;
                            }
                        }

                        if (nRet != 0 && nRet != 1)
                            return 0;	// ����

                        stop.SetMessage("���ڻ�ȡ�� " + (i + 1).ToString() + " �� ISO2709 ��¼");
                        this.progressBar_records.Value = (int)file.Position;

                        // ����̫�̵ļ�¼
                        if (string.IsNullOrEmpty(strMARC) == true
                            || strMARC.Length <= 24)
                            continue;

                        if (this._openMarcFileDialog.Mode880 == true
                            && (this._openMarcFileDialog.MarcSyntax == "usmarc" || this._openMarcFileDialog.MarcSyntax == "<�Զ�>"))
                        {
                            MarcRecord temp = new MarcRecord(strMARC);
                            MarcQuery.ToParallel(temp);
                            strMARC = temp.Text;
                        }

                        // ����Script��OnRecord()����
                        if (objStatis != null)
                        {
                            objStatis.MARC = strMARC;
                            objStatis.CurrentRecordIndex = i;

                            StatisEventArgs args = new StatisEventArgs();
                            objStatis.OnRecord(this, args);
                            if (args.Continue == ContinueType.SkipAll)
                                return 1;
                            if (args.Continue == ContinueType.Error)
                            {
                                strError = args.ParamString;
                                return -1;
                            }
                        }

                        nCount++;
                    }

                    /*
                    Global.WriteHtml(this.webBrowser_batchAddItemPrice,
                        "����������������۸��ַ��� " + nCount.ToString() + " ����\r\n");
                     * */

                    return 0;
                }
                finally
                {
                    /*
                    EnableControls(true);

                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                     * */

                    if (file != null)
                        file.Close();
                }
            }
            finally
            {
            }

            // return 0;
        }

        private void button_next_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_source)
            {
                if (string.IsNullOrEmpty(this._openMarcFileDialog.FileName) == true)
                {
                    strError = "��δָ�������ISO2709�ļ���";
                    goto ERROR1;
                }

                if (string.IsNullOrEmpty(this._openMarcFileDialog.EncodingName) == true)
                {
                    strError = "��δѡ�� ISO2709 �ļ��ı��뷽ʽ";
                    goto ERROR1;
                }

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

#if NO
        private void button_findInputIso2709Filename_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵�ISO2709�ļ���";
            dlg.FileName = this.textBox_inputIso2709Filename.Text;
            dlg.Filter = "ISO2709�ļ� (*.iso)|*.iso|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_inputIso2709Filename.Text = dlg.FileName;

        }
#endif

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
#endif

        private void Iso2709StatisForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);

        }

        // 
        /// <summary>
        /// �޸���Ŀ��Ϣ��
        /// ��ο� dp2Library API SetBiblioInfo() ����ϸ˵��
        /// </summary>
        /// <param name="strAction">����</param>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strBiblioType">��Ŀ����</param>
        /// <param name="strBiblio">��Ŀ��¼</param>
        /// <param name="timestamp">ʱ���</param>
        /// <param name="strOutputBiblioRecPath">����ʵ��д�����Ŀ��¼·��</param>
        /// <param name="baNewTimestamp">��������ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int SetBiblioInfo(string strAction,
                string strBiblioRecPath,
                string strBiblioType,
                string strBiblio,
                byte[] timestamp,
                out string strOutputBiblioRecPath,
                out byte[] baNewTimestamp,
                out string strError)
        {
            long lRet = Channel.SetBiblioInfo(
                stop,
                strAction,
                strBiblioRecPath,
                strBiblioType,
                strBiblio,
                timestamp,
                "",
                out strOutputBiblioRecPath,
                out baNewTimestamp,
                out strError);
            return (int)lRet;
        }

        // 
        /// <summary>
        /// ����һ��ʵ���¼
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strItemXml">ʵ���¼ XML</param>
        /// <param name="strStyle">�������"force"��ʾǿ��д��</param>
        /// <param name="strNewItemRecPath">����ʵ��д��Ĳ��¼·��</param>
        /// <param name="strNewXml">����ʵ��д���ʵ���¼ XML</param>
        /// <param name="baNewTimestamp">����ʵ���¼������ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int CreateItemInfo(
            string strBiblioRecPath,
            string strItemXml,
            string strStyle,
            out string strNewItemRecPath,
            out string strNewXml,
            out byte[] baNewTimestamp,
            out string strError)
        {
            strError = "";

            strNewItemRecPath = "";
            strNewXml = "";
            baNewTimestamp = null;

            EntityInfo info = new EntityInfo();
            info.RefID = Guid.NewGuid().ToString();

            string strTargetBiblioRecID = Global.GetRecordID(strBiblioRecPath);

            XmlDocument item_dom = new XmlDocument();
            try
            {
                item_dom.LoadXml(strItemXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ�ص�DOMʱ��������: " + ex.Message;
                return -1;
            }

            DomUtil.SetElementText(item_dom.DocumentElement,
                "parent", strTargetBiblioRecID);

            info.Action = "new";
            info.NewRecPath = "";
            info.NewRecord = item_dom.OuterXml;
            info.NewTimestamp = null;
            info.Style = strStyle;

            // 
            EntityInfo[] entities = new EntityInfo[1];
            entities[0] = info;

            EntityInfo[] errorinfos = null;

            long lRet = Channel.SetEntities(
                stop,
                strBiblioRecPath,
                entities,
                out errorinfos,
                out strError);
            if (lRet == -1)
                return -1;

            if (errorinfos != null && errorinfos.Length > 0)
            {
                int nErrorCount = 0;
                for (int i = 0; i < errorinfos.Length; i++)
                {
                    EntityInfo error = errorinfos[i];
                    if (error.ErrorCode != ErrorCodeValue.NoError)
                    {
                        if (String.IsNullOrEmpty(strError) == false)
                            strError += "; ";
                        strError += errorinfos[0].ErrorInfo;
                        nErrorCount++;
                    }
                    else
                    {
                        strNewItemRecPath = error.NewRecPath;
                        strNewXml = error.NewRecord;
                        baNewTimestamp = error.NewTimestamp;
                    }
                }
                if (nErrorCount > 0)
                {
                    return -1;
                }
            }

            return 1;
        }

        // 
        /// <summary>
        /// ����һ��������¼
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strOrderXml">������¼ XML</param>
        /// <param name="strStyle">�������"force"��ʾǿ��д��</param>
        /// <param name="strNewOrderRecPath">����ʵ��д��Ķ�����¼·��</param>
        /// <param name="strNewXml">����ʵ��д��Ķ�����¼ XML</param>
        /// <param name="baNewTimestamp">���ض�����¼������ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 1: �ɹ�</returns>
        public int CreateOrderInfo(
            string strBiblioRecPath,
            string strOrderXml,
            string strStyle,
            out string strNewOrderRecPath,
            out string strNewXml,
            out byte[] baNewTimestamp,
            out string strError)
        {
            strError = "";

            strNewOrderRecPath = "";
            strNewXml = "";
            baNewTimestamp = null;

            EntityInfo info = new EntityInfo();
            info.RefID = Guid.NewGuid().ToString();

            string strTargetBiblioRecID = Global.GetRecordID(strBiblioRecPath);

            XmlDocument item_dom = new XmlDocument();
            try
            {
                item_dom.LoadXml(strOrderXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ�ص�DOMʱ��������: " + ex.Message;
                return -1;
            }

            DomUtil.SetElementText(item_dom.DocumentElement,
                "parent", strTargetBiblioRecID);

            info.Action = "new";
            info.NewRecPath = "";
            info.NewRecord = item_dom.OuterXml;
            info.NewTimestamp = null;
            info.Style = strStyle;

            // 
            EntityInfo[] orders = new EntityInfo[1];
            orders[0] = info;

            EntityInfo[] errorinfos = null;

            long lRet = Channel.SetOrders(
                stop,
                strBiblioRecPath,
                orders,
                out errorinfos,
                out strError);
            if (lRet == -1)
                return -1;

            if (errorinfos != null && errorinfos.Length > 0)
            {
                int nErrorCount = 0;
                for (int i = 0; i < errorinfos.Length; i++)
                {
                    EntityInfo error = errorinfos[i];
                    if (error.ErrorCode != ErrorCodeValue.NoError)
                    {
                        if (String.IsNullOrEmpty(strError) == false)
                            strError += "; ";
                        strError += errorinfos[0].ErrorInfo;
                        nErrorCount++;
                    }
                    else
                    {
                        strNewOrderRecPath = error.NewRecPath;
                        strNewXml = error.NewRecord;
                        baNewTimestamp = error.NewTimestamp;
                    }
                }
                if (nErrorCount > 0)
                {
                    return -1;
                }
            }

            return 1;
        }
    
    }
}