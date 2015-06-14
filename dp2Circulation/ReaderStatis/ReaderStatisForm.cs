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
using DigitalPlatform.Text;

using DigitalPlatform.dp2.Statis;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ����ͳ�ƴ�
    /// </summary>
    public partial class ReaderStatisForm : MyScriptForm
    {
        // public HtmlViewerForm ErrorInfoForm = null;

        // bool Running = false;   // ����ִ������

        // string m_strMainCsDllName = "";
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

        // public ScriptManager ScriptManager = new ScriptManager();

        ReaderStatis objStatis = null;
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
        int AssemblyVersion
        {
            get
            {
                return MainForm.ReaderStatisAssemblyVersion;
            }
            set
            {
                MainForm.ReaderStatisAssemblyVersion = value;
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
        public ReaderStatisForm()
        {
            InitializeComponent();
        }

        private void ReaderStatisForm_Load(object sender, EventArgs e)
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
    this.MainForm.DataDir + "\\reader_statis_projects.xml";

#if NO
            ScriptManager.applicationInfo = this.MainForm.AppInfo;
            ScriptManager.CfgFilePath =
                this.MainForm.DataDir + "\\reader_statis_projects.xml";
            ScriptManager.DataDir = this.MainForm.DataDir;

            ScriptManager.CreateDefaultContent -= new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);
            ScriptManager.CreateDefaultContent += new CreateDefaultContentEventHandler(scriptManager_CreateDefaultContent);

            try
            {
                ScriptManager.Load();
            }
            catch (FileNotFoundException)
            {
                // ���ر��� 2009/2/4
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
#endif

            this.radioButton_inputStyle_barcodeFile.Checked = this.MainForm.AppInfo.GetBoolean(
                "readerstatisform",
                "inputstyle_barcodefile",
                false);

            this.radioButton_inputStyle_recPathFile.Checked = this.MainForm.AppInfo.GetBoolean(
                "readerstatisform",
                "inputstyle_recpathfile",
                false);

            this.radioButton_inputStyle_readerDatabase.Checked = this.MainForm.AppInfo.GetBoolean(
                "readerstatisform",
                "inputstyle_readerdatabase",
                true);


            // �����������ļ���
            this.textBox_inputBarcodeFilename.Text = this.MainForm.AppInfo.GetString(
                "readerstatisform",
                "input_barcode_filename",
                "");

            // ����ļ�¼·���ļ���
            this.textBox_inputRecPathFilename.Text = this.MainForm.AppInfo.GetString(
                "readerstatisform",
                "input_recpath_filename",
                "");


            // ����Ķ��߿���
            this.comboBox_inputReaderDbName.Text = this.MainForm.AppInfo.GetString(
                "readerstatisform",
                "input_readerdbname",
                "<ȫ��>");

            // ������
            this.textBox_projectName.Text = this.MainForm.AppInfo.GetString(
                "readerstatisform",
                "projectname",
                "");

            // ���������б�
            this.textBox_departmentNames.Text = this.MainForm.AppInfo.GetString(
                 "readerstatisform",
                 "departments",
                 "*");

            // ���������б�
            this.textBox_readerTypes.Text = this.MainForm.AppInfo.GetString(
                 "readerstatisform",
                 "readertypes",
                 "*");

            // ��֤���ڷ�Χ
            this.textBox_createTimeRange.Text = this.MainForm.AppInfo.GetString(
                 "readerstatisform",
                 "create_timerange",
                 "");


            // ʧЧ���ڷ�Χ
            this.textBox_expireTimeRange.Text = this.MainForm.AppInfo.GetString(
                "readerstatisform",
                "expire_timerange",
                "");

            // ���������
            this.checkBox_departmentTable.Checked = this.MainForm.AppInfo.GetBoolean(
                "readerstatisform",
                "departmentTable",
                false);

            // SetInputPanelEnabled();

        }

        private void ReaderStatisForm_FormClosing(object sender, FormClosingEventArgs e)
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



        private void ReaderStatisForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            this.MainForm.AppInfo.SetBoolean(
                "readerstatisform",
                "inputstyle_barcodefile",
                this.radioButton_inputStyle_barcodeFile.Checked);

            this.MainForm.AppInfo.SetBoolean(
                "readerstatisform",
                "inputstyle_recpathfile",
                this.radioButton_inputStyle_recPathFile.Checked);


            this.MainForm.AppInfo.SetBoolean(
                "readerstatisform",
                "inputstyle_readerdatabase",
                this.radioButton_inputStyle_readerDatabase.Checked);


            // �����������ļ���
            this.MainForm.AppInfo.SetString(
                "readerstatisform",
                "input_barcode_filename",
                this.textBox_inputBarcodeFilename.Text);

            // ����ļ�¼·���ļ���
            this.MainForm.AppInfo.SetString(
                "readerstatisform",
                "input_recpath_filename",
                this.textBox_inputRecPathFilename.Text);

            // ����Ķ��߿���
            this.MainForm.AppInfo.SetString(
                "readerstatisform",
                "input_readerdbname",
                this.comboBox_inputReaderDbName.Text);

            // ������
            this.MainForm.AppInfo.SetString(
                "readerstatisform",
                "projectname",
                this.textBox_projectName.Text);

            // ���������б�
            this.MainForm.AppInfo.SetString(
                 "readerstatisform",
                 "departments",
                 this.textBox_departmentNames.Text);

            // ���������б�
            this.MainForm.AppInfo.SetString(
                 "readerstatisform",
                 "readertypes",
                 this.textBox_readerTypes.Text);

            // ��֤���ڷ�Χ
            this.MainForm.AppInfo.SetString(
                 "readerstatisform",
                 "create_timerange",
                 this.textBox_createTimeRange.Text);


            // ʧЧ���ڷ�Χ
            this.MainForm.AppInfo.GetString(
                "readerstatisform",
                "expire_timerange",
                this.textBox_expireTimeRange.Text);

            // ���������
            this.MainForm.AppInfo.SetBoolean(
                "readerstatisform",
                "departmentTable",
                this.checkBox_departmentTable.Checked);

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

        private void button_inputCreateTimeRange_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;
            DateTime start;
            DateTime end;

            nRet = Global.ParseTimeRangeString(this.textBox_createTimeRange.Text,
                false,
                out start,
                out end,
                out strError);
            /*
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }*/


            TimeRangeDlg dlg = new TimeRangeDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.Text = "��֤���ڷ�Χ";
            dlg.StartDate = start;
            dlg.EndDate = end;

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult == DialogResult.Cancel)
                return;

            this.textBox_createTimeRange.Text = Global.MakeTimeRangeString(dlg.StartDate, dlg.EndDate);

        }

        private void button_inputExpireTimeRange_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;
            DateTime start;
            DateTime end;

            nRet = Global.ParseTimeRangeString(this.textBox_expireTimeRange.Text,
                false,
                out start,
                out end,
                out strError);
            /*
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }*/


            TimeRangeDlg dlg = new TimeRangeDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.Text = "ʧЧ�ڷ�Χ";
            dlg.StartDate = start;
            dlg.EndDate = end;

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult == DialogResult.Cancel)
                return;

            this.textBox_expireTimeRange.Text = Global.MakeTimeRangeString(dlg.StartDate, dlg.EndDate);

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

            //sw.WriteLine("using DigitalPlatform.MarcDom;");
            //sw.WriteLine("using DigitalPlatform.Statis;");
            sw.WriteLine("using dp2Circulation;");

            sw.WriteLine("using DigitalPlatform.Xml;");


            sw.WriteLine("public class MyStatis : ReaderStatis");

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
            dlg.HostName = "ReaderStatisForm";
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

            this.textBox_createTimeRange.Enabled = bEnable;
            this.textBox_expireTimeRange.Enabled = bEnable;

            this.checkBox_departmentTable.Enabled = bEnable;

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

                // 2009/11/5
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

                objStatis.DepartmentNames = this.textBox_departmentNames.Text;
                objStatis.ReaderTypes = this.textBox_readerTypes.Text;

                objStatis.CreateTimeRange = this.textBox_createTimeRange.Text;
                objStatis.ExpireTimeRange = this.textBox_expireTimeRange.Text;

                // TODO: ������ʱ�䷶Χ�����������?

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
            out ReaderStatis objStatis,
            out string strError)
        {
            this.AssemblyMain = null;

            objStatis = null;

            string strWarning = "";
            string strMainCsDllName = PathUtil.MergePath(this.InstanceDir, "\\~reader_statis_main_" + Convert.ToString(AssemblyVersion++) + ".dll");    // ++

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
                "ReaderStatisForm",
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
                "dp2Circulation.ReaderStatis");
            if (entryClassType == null)
            {
                strError = strMainCsDllName + "��û���ҵ� dp2Circulation.ReaderStatis �����ࡣ";
                goto ERROR1;
            }
            // newһ��Statis��������
            objStatis = (ReaderStatis)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            // ΪStatis���������ò���
            objStatis.ReaderStatisForm = this;
            objStatis.ProjectDir = strProjectLocate;
            objStatis.InstanceDir = this.InstanceDir;

            return 0;
        ERROR1:
            return -1;
        }

        // ע�⣺�ϼ�����RunScript()�Ѿ�ʹ����BeginLoop()��EnableControls()
        // ��ÿ�����߼�¼����ѭ��
        // return:
        //      0   ��ͨ����
        //      1   Ҫȫ���ж�
        int DoLoop(out string strError)
        {
            strError = "";
            int nRet = 0;
            long lRet = 0;

            // List<string> LogFileNames = null;

            // ���������Ϣ�����в��������
#if NO
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

            // ׼������ʱ�����

            DateTime startCreate = new DateTime(0);
            DateTime endCreate = new DateTime(0);

            if (this.textBox_createTimeRange.Text != "")
            {
                nRet = Global.ParseTimeRangeString(this.textBox_createTimeRange.Text,
                    false,
                    out startCreate,
                    out endCreate,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            DateTime startExpire = new DateTime(0);
            DateTime endExpire = new DateTime(0);

            if (this.textBox_expireTimeRange.Text != "")
            {
                nRet = Global.ParseTimeRangeString(this.textBox_expireTimeRange.Text,
                    false,
                    out startExpire,
                    out endExpire,
                    out strError);
                if (nRet == -1)
                    return -1;
            }



            // �����������б�
            string strDepartmentList = this.textBox_departmentNames.Text;
            if (String.IsNullOrEmpty(strDepartmentList) == true)
                strDepartmentList = "*";

            string[] departments = strDepartmentList.Split(new char[] { ',' });

            StringMatchList department_matchlist = new StringMatchList(departments);

            // �������͹����б�
            string strReaderTypeList = this.textBox_readerTypes.Text;
            if (String.IsNullOrEmpty(strReaderTypeList) == true)
                strReaderTypeList = "*";

            string[] readertypes = strReaderTypeList.Split(new char[] { ',' });

            StringMatchList readertype_matchlist = new StringMatchList(readertypes);

            // ��¼·����ʱ�ļ�
            string strTempRecPathFilename = Path.GetTempFileName();

            string strInputFileName = "";   // �ⲿ�ƶ��������ļ���Ϊ������ļ����߼�¼·���ļ���ʽ
            string strAccessPointName = "";

            try
            {

                if (this.InputStyle == ReaderStatisInputStyle.WholeReaderDatabase)
                {
                    nRet = SearchAllReaderRecPath(strTempRecPathFilename,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    strInputFileName = strTempRecPathFilename;
                    strAccessPointName = "��¼·��";
                }
                else if (this.InputStyle == ReaderStatisInputStyle.BarcodeFile)
                {
                    strInputFileName = this.textBox_inputBarcodeFilename.Text;
                    strAccessPointName = "֤����";
                }
                else if (this.InputStyle == ReaderStatisInputStyle.RecPathFile)
                {
                    strInputFileName = this.textBox_inputRecPathFilename.Text;
                    strAccessPointName = "��¼·��";
                }
                else
                {
                    Debug.Assert(false, "");
                }

                StreamReader sr = null;

                // 2008/4/3
                Encoding encoding = FileUtil.DetectTextFileEncoding(strInputFileName);

                try
                {
                    sr = new StreamReader(strInputFileName, encoding);
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
                stop.Initial("���ڻ�ȡ���߼�¼ ...");
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
                                    "ReaderStatisForm",
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
                        string strRecPathOrBarcode = sr.ReadLine();

                        if (strRecPathOrBarcode == null)
                            break;

                        if (String.IsNullOrEmpty(strRecPathOrBarcode) == true)
                            continue;

                        stop.SetMessage("���ڻ�ȡ�� " + (i + 1).ToString() + " �����߼�¼��" + strAccessPointName + "Ϊ " + strRecPathOrBarcode);
                        this.progressBar_records.Value = (int)sr.BaseStream.Position;

                        // ��ö��߼�¼
                        string strOutputRecPath = "";
                        byte[] baTimestamp = null;


                        string[] results = null;

                        string strAccessPoint = "";
                        if (this.InputStyle == ReaderStatisInputStyle.WholeReaderDatabase)
                            strAccessPoint = "@path:" + strRecPathOrBarcode;
                        else if (this.InputStyle == ReaderStatisInputStyle.RecPathFile)
                            strAccessPoint = "@path:" + strRecPathOrBarcode;
                        else if (this.InputStyle == ReaderStatisInputStyle.BarcodeFile)
                            strAccessPoint = strRecPathOrBarcode;
                        else
                        {
                            Debug.Assert(false, "");
                        }

                        if (StringUtil.IsInList("xml", objStatis.XmlFormat) == false
                            && StringUtil.IsInList("advancexml", objStatis.XmlFormat) == false)
                        {
                            strError = "ReaderStatis��ԱXmlFormat��ֵӦ���ٰ���xml��advancexml֮�е�һ��";
                            return -1;
                        }

                        // Result.Value -1���� 0û���ҵ� 1�ҵ� >1���ж���1��
                        lRet = Channel.GetReaderInfo(
                            stop,
                            strAccessPoint,
                            objStatis.XmlFormat,    // "xml",   // strResultType
                            out results,
                            out strOutputRecPath,
                            out baTimestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            strError = "��ö��߼�¼ " + strAccessPoint + " ʱ��������: " + strError;
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                            /*
                            Global.WriteHtml(this.webBrowser_batchAddItemPrice,
                                "�����¼ " + strReaderBarcode + " ʱ����(1): " + strError + "\r\n");
                             * */
                            continue;
                        }

                        if (lRet == 0)
                        {
                            strError = "����" + strAccessPointName + " " + strRecPathOrBarcode + " ��Ӧ��XML����û���ҵ���";
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                            continue;
                        }

                        if (lRet > 1)
                        {
                            strError = "����" + strAccessPointName + " " + strRecPathOrBarcode + " ��Ӧ���ݶ���һ����";
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                            continue;
                        }

                        string strXml = "";

                        strXml = results[0];


                        // �����Ƿ���ϣ��ͳ�Ƶķ�Χ��
                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(strXml);
                        }
                        catch (Exception ex)
                        {
                            strError = "���߼�¼װ��DOM��������: " + ex.Message;
                            continue;
                        }

                        // ���ղ�������ɸѡ
                        if (this.textBox_departmentNames.Text != ""
                            && this.textBox_departmentNames.Text != "*")
                        {
                            // ע�����ַ�������"*"��ʾʲô�����㡣Ҳ�͵��ڲ�ʹ�ô�ɸѡ��

                            string strDepartment = DomUtil.GetElementText(dom.DocumentElement,
                                "department");
                            if (department_matchlist.Match(strDepartment) == false)
                                continue;
                        }

                        // ���ն�������ɸѡ
                        if (this.textBox_readerTypes.Text != ""
                            && this.textBox_readerTypes.Text != "*")
                        {
                            // ע�����ַ�������"*"��ʾʲô�����㡣Ҳ�͵��ڲ�ʹ�ô�ɸѡ��

                            string strReaderType = DomUtil.GetElementText(dom.DocumentElement,
                                "readerType");
                            if (readertype_matchlist.Match(strReaderType) == false)
                                continue;
                        }

                        // Debug.Assert(false, "");

                        // ���հ�֤����ɸѡ
                        if (this.textBox_createTimeRange.Text != "")
                        {
                            // ע�����ַ�����ʾʲô�����㡣Ҳ�͵��ڲ�ʹ�ô�ɸѡ��

                            string strCreateDate = DomUtil.GetElementText(dom.DocumentElement, "createDate");

                            if (String.IsNullOrEmpty(strCreateDate) == false)
                            {
                                try
                                {
                                    DateTime createTime = DateTimeUtil.FromRfc1123DateTimeString(strCreateDate);
                                    createTime = createTime.ToLocalTime();

                                    if (createTime >= startCreate && createTime <= endCreate)
                                    {
                                    }
                                    else
                                        continue;
                                }
                                catch
                                {
                                    strError = "<createDate>�������ַ��� '" + strCreateDate + "' ��ʽ����";
                                    GetErrorInfoForm().WriteHtml(HttpUtility.HtmlEncode(strError) + "\r\n");
                                }
                            }
                        }


                        // ����ʧЧ����ɸѡ
                        if (this.textBox_expireTimeRange.Text != "")
                        {
                            // ע�����ַ�����ʾʲô�����㡣Ҳ�͵��ڲ�ʹ�ô�ɸѡ��

                            string strExpireDate = DomUtil.GetElementText(dom.DocumentElement, "expireDate");

                            if (String.IsNullOrEmpty(strExpireDate) == false)
                            {
                                try
                                {
                                    DateTime expireTime = DateTimeUtil.FromRfc1123DateTimeString(strExpireDate);
                                    expireTime = expireTime.ToLocalTime();

                                    if (expireTime >= startExpire && expireTime <= endExpire)
                                    {
                                    }
                                    else
                                        continue;
                                }
                                catch
                                {
                                    strError = "<expireDate>�������ַ��� '" + strExpireDate + "' ��ʽ����";
                                    GetErrorInfoForm().WriteHtml(HttpUtility.HtmlEncode(strError) + "\r\n");
                                }
                            }
                        }


                        // strXml��Ϊ��־��¼

                        // ����Script��OnRecord()����
                        if (objStatis != null)
                        {
                            objStatis.Xml = strXml;
                            objStatis.ReaderDom = dom;
                            objStatis.CurrentRecPath = strOutputRecPath;    // 2009/10/21 changed // BUG !!! strRecPathOrBarcode;
                            objStatis.CurrentRecordIndex = i;
                            objStatis.Timestamp = baTimestamp;

                            StatisEventArgs args = new StatisEventArgs();
                            objStatis.OnRecord(this, args);
                            if (args.Continue == ContinueType.SkipAll)
                                return 1;
                        }

                        nCount++;
                    }

                    /*
                    Global.WriteHtml(this.webBrowser_batchAddItemPrice,
                        "����������������۸��ַ��� " + nCount.ToString() + " ����\r\n");
                     * */


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
        // ����������ж��߼�¼·��(������ļ�)
        int SearchAllReaderRecPath(string strRecPathFilename,
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
                    long lRet = Channel.SearchReader(stop,
                        this.comboBox_inputReaderDbName.Text,
                        "",
                        -1,
                        "֤����",
                        "left",
                        this.Lang,
                        null,   // strResultSetName
                        "", // strOutputStyle
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    long lHitCount = lRet;

                    long lStart = 0;
                    long lCount = lHitCount;

                    /*
                    Global.WriteHtml(this.webBrowser_resultInfo,
        "���� " + lHitCount.ToString() + "�����߼�¼��\r\n");
                     * */


                    Record[] searchresults = null;

                    // װ�������ʽ
                    for (; ; )
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                goto ERROR1;
                            }
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
                            // sw.Write(searchresults[i].Cols[0] + "\r\n");
                            // TODO: ��ʵ����ȡ��¼·������������ȡ��¼�����������
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

        // ��һ�� ��ť
        private void button_next_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_source)
            {
                if (this.radioButton_inputStyle_barcodeFile.Checked == true)
                {
                    if (this.textBox_inputBarcodeFilename.Text == "")
                    {
                        strError = "��δָ�������������ļ���";
                        goto ERROR1;
                    }
                }
                else
                {
                    if (this.comboBox_inputReaderDbName.Text == "")
                    {
                        strError = "��δָ�����߿���";
                        goto ERROR1;
                    }
                }

                // �л�����������page
                this.tabControl_main.SelectedTab = this.tabPage_filter;
                return;

            }

            if (this.tabControl_main.SelectedTab == this.tabPage_filter)
            {
                /*
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
                 * */

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

        private void radioButton_inputStyle_barcodeFile_CheckedChanged(object sender, EventArgs e)
        {
            SetInputPanelEnabled();
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
            if (this.radioButton_inputStyle_barcodeFile.Checked == true)
            {
                this.textBox_inputBarcodeFilename.Enabled = true;
                this.button_findInputBarcodeFilename.Enabled = true;

                this.textBox_inputRecPathFilename.Enabled = false;
                this.button_findInputRecPathFilename.Enabled = false;

                this.comboBox_inputReaderDbName.Enabled = false;
            }
            else if (this.radioButton_inputStyle_recPathFile.Checked == true)
            {
                this.textBox_inputBarcodeFilename.Enabled = false;
                this.button_findInputBarcodeFilename.Enabled = false;

                this.textBox_inputRecPathFilename.Enabled = true;
                this.button_findInputRecPathFilename.Enabled = true;


                this.comboBox_inputReaderDbName.Enabled = false;
            }
            else
            {
                this.textBox_inputBarcodeFilename.Enabled = false;
                this.button_findInputBarcodeFilename.Enabled = false;

                this.textBox_inputRecPathFilename.Enabled = false;
                this.button_findInputRecPathFilename.Enabled = false;

                this.comboBox_inputReaderDbName.Enabled = true;
            }
        }

        // ������
        /// <summary>
        /// ���뷽ʽ
        /// </summary>
        public ReaderStatisInputStyle InputStyle
        {
            get
            {
                if (this.radioButton_inputStyle_barcodeFile.Checked == true)
                    return ReaderStatisInputStyle.BarcodeFile;
                else if (this.radioButton_inputStyle_recPathFile.Checked == true)
                    return ReaderStatisInputStyle.RecPathFile;
                else
                    return ReaderStatisInputStyle.WholeReaderDatabase;
            }
        }

        private void button_findInputBarcodeFilename_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵Ķ���֤������ļ���";
            dlg.FileName = this.textBox_inputBarcodeFilename.Text;
            dlg.Filter = "������ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_inputBarcodeFilename.Text = dlg.FileName;
        }

        private void button_findInputRecPathFilename_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵Ķ��߼�¼·���ļ���";
            dlg.FileName = this.textBox_inputRecPathFilename.Text;
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_inputRecPathFilename.Text = dlg.FileName;

        }

        private void comboBox_inputReaderDbName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_inputReaderDbName.Items.Count > 0)
                return;

            this.comboBox_inputReaderDbName.Items.Add("<ȫ��>");

            if (this.MainForm.ReaderDbNames != null)    // 2009/3/29
            {
                for (int i = 0; i < this.MainForm.ReaderDbNames.Length; i++)
                {
                    this.comboBox_inputReaderDbName.Items.Add(this.MainForm.ReaderDbNames[i]);
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
#endif

        // �Ƿ��յ�λ�������������?
        /// <summary>
        /// �Ƿ��յ�λ�������������?
        /// </summary>
        public bool OutputDepartmentTable
        {
            get
            {
                return this.checkBox_departmentTable.Checked;
            }
            set
            {
                this.checkBox_departmentTable.Checked = value;
            }
        }

        private void button_clearCreateTimeRange_Click(object sender, EventArgs e)
        {
            this.textBox_createTimeRange.Text = "";
        }

        private void button_clearExpireTimeRange_Click(object sender, EventArgs e)
        {
            this.textBox_expireTimeRange.Text = "";
        }

        // ������߼�¼
        // ���ⲿC#�ű����á��򱾺�����ѭ���б����ã�����Ҫ�ٵ���BeginLoop()
        /// <summary>
        /// ������߼�¼��
        /// </summary>
        /// <param name="strRecPath">���߼�¼·��</param>
        /// <param name="strAction">����</param>
        /// <param name="strOldXml">�޸�ǰ���߼�¼ XML</param>
        /// <param name="baOldTimestamp">�޸�ǰ���߼�¼��ʱ���</param>
        /// <param name="strNewXml">�޸ĺ�Ķ��߼�¼�� XML</param>
        /// <param name="baNewTimestamp">��������ʱ���</param>
        /// <param name="strSavedPath">����ʵ�ʱ���ļ�¼·��</param>
        /// <param name="strSavedXml">����ʵ�ʱ���Ķ��߼�¼ XML</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ʧ��; 0: ����; 1: �����ֶα��ܾ�</returns>
        public int SaveReaderRecord(
            string strRecPath,
            string strAction,
            string strOldXml,
            byte [] baOldTimestamp,
            string strNewXml,
            out byte [] baNewTimestamp,
            out string strSavedPath,
            out string strSavedXml,
            out string strError)
        {
            strError = "";
            baNewTimestamp = null;
            strSavedXml = "";
            strSavedPath = "";

            /*
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ�����߼�¼");
            stop.BeginLoop();

            EnableControls(false);
             * */

            try
            {
                ErrorCodeValue kernel_errorcode;

                string strExistingXml = "";

                long lRet = Channel.SetReaderInfo(
                    stop,
                    strAction,
                    strRecPath,
                    strNewXml,
                    strOldXml, // this.readerEditControl1.OldRecord,
                    baOldTimestamp,
                    out strExistingXml,
                    out strSavedXml,
                    out strSavedPath,
                    out baNewTimestamp,
                    out kernel_errorcode,
                    out strError);
                if (lRet == -1)
                    return -1;

                if (lRet == 1)
                {
                    // �����ֶα��ܾ�
                }

                return (int)lRet;
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

        private void ReaderStatisForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);
        }


    }

    /// <summary>
    /// ����ͳ�ƴ����뷽ʽ
    /// </summary>
    public enum ReaderStatisInputStyle
    {
        /// <summary>
        /// ������ļ�
        /// </summary>
        BarcodeFile = 1,  // ������ļ�
        /// <summary>
        /// ��¼·���ļ�
        /// </summary>
        RecPathFile = 2,    // ��¼·���ļ�
        /// <summary>
        /// ȫ��
        /// </summary>
        WholeReaderDatabase = 3,    // ȫ��
    }

    // 
    /// <summary>
    /// һ���ַ���ģʽ
    /// </summary>
    public class StringMatch
    {
        /// <summary>
        /// �Ƿ�Ϊ�϶��жϡ����Ϊ true ��ʾ���ǡ�Ϊ���У����Ϊ false ��ʾ����Ϊ����
        /// </summary>
        public bool Is = true;  // �Ƿ�Ϊ�϶��жϡ����Ϊtrue��ʾ���ǡ�Ϊ���У����Ϊfalse��ʾ����Ϊ����
        /// <summary>
        /// Pattern
        /// </summary>
        public string Pattern = "";
        /// <summary>
        /// WildMatch
        /// </summary>
        public WildMatch WildMatch = null;
    }

    // 
    /// <summary>
    /// �ַ���ģʽ�б����ƥ����
    /// </summary>
    public class StringMatchList : List<StringMatch>
    {
        // 
        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="departments">��������</param>
        public StringMatchList(string[] departments)
        {
            for (int i = 0; i < departments.Length; i++)
            {
                string strPattern = departments[i];
                if (String.IsNullOrEmpty(strPattern) == true)
                    continue;

                bool bIs = true;

                if (strPattern.Length >= 1
                    && strPattern[0] == '!')
                {
                    bIs = false;
                    strPattern = strPattern.Substring(1);
                }
                else
                    bIs = true;

                WildMatch wildmatch = new WildMatch(strPattern,
                    "*?[]");    // ����DOSͨ���ϰ��

                StringMatch match = new StringMatch();
                match.Pattern = strPattern;
                match.WildMatch = wildmatch;
                match.Is = bIs;

                this.Add(match);
            }

            MoveReverseItemForward();
        }

        // �ѷ���ģʽ��ǰ�ƶ�
        void MoveReverseItemForward()
        {
            int j=0;    // �ƶ��������β��ָ��
            for (int i = 0; i < this.Count; i++)
            {
                StringMatch match = this[i];
                if (match.Is == false)
                {
                    if (i != 0)
                    {
                        this.RemoveAt(i);
                        this.Insert(j, match);
                    }
                    j++;
                }
            }
        }

        // 
        /// <summary>
        /// ��һ��ʵ������ƥ��
        /// </summary>
        /// <param name="strText">Ҧƥ���ʵ��</param>
        /// <returns>true: ƥ��; false: ��ƥ��</returns>
        public bool Match(string strText)
        {
            string strResult = "";
            StringMatch match = null;
            for (int i = 0; i < this.Count; i++)
            {
                match = this[i];
                int nRet = match.WildMatch.Match(strText, out strResult);
                if (match.Is == true)
                {
                    if (nRet != -1)   // match
                        return true; 
                }
                else
                {
                    if (nRet != -1)   // match
                        return false; 
                }
            }

            if (match.Is == false)  // �ղŲ�������һ���Ƿ���
                return true;

            return false;   // not match
        }
    }

    // һ�������ַ�������
    // ���ڰѲ���������ָ����滯
    /// <summary>
    /// һ�������ַ�������
    /// </summary>
    public class RegularString
    {
        /// <summary>
        /// ��������
        /// </summary>
        public string RegularText = ""; // ��������
        /// <summary>
        /// ƥ���б�
        /// </summary>
        public StringMatchList match_list = null;   // ƥ���б�
    }

    // 
    /// <summary>
    /// �����ַ������顣����һ�������ַ�������
    /// </summary>
    public class RegularStringCollection : List<RegularString>
    {
        // ���캯�����������ļ�������
        // parameters:
        //      strCfgFilename  �����ļ�����һ�����ı��ļ���Ҫ��ΪUTF-8���롣
        //              ���ݸ�ʽ����
        /*
        ��ѧϵ=*��ѧ*,!*��ѧ��*
        ����ϵ=*����*,!*������*
         * */
        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="strCfgFilename">�����ļ���ȫ·��</param>
        public RegularStringCollection(string strCfgFilename)
        {
            string strError = "";

            StreamReader sr = null;

            // 2008/4/3
            Encoding encoding = FileUtil.DetectTextFileEncoding(strCfgFilename);

            try
            {
                sr = new StreamReader(strCfgFilename, encoding);
            }
            catch (Exception ex)
            {
                strError = "���ļ� " + strCfgFilename + " ʧ��: " + ex.Message;
                throw new Exception(strError);
            }

            try
            {
                for (int i = 0; ; i++)
                {
                    string strLine = sr.ReadLine();

                    if (strLine == null)
                        break;

                    if (String.IsNullOrEmpty(strLine) == true)
                        continue;

                    strLine = strLine.Trim();

                    // ע����
                    if (strLine.Length >= 2)
                    {
                        if (strLine[0] == '/' && strLine[1] == '/')
                            continue;
                    }

                    int nRet = strLine.IndexOf("=");
                    if (nRet == -1)
                        throw (new Exception("�� '" +strLine+ "' ��ʽ����ȷ��ȱ��=��"));

                    string strName = strLine.Substring(0, nRet).Trim();

                    if (String.IsNullOrEmpty(strName) == true)
                        throw (new Exception("�� '" + strLine + "' ��ʽ����ȷ��=�����ȱ������������"));


                    string strList = strLine.Substring(nRet + 1).Trim();

                    if (String.IsNullOrEmpty(strList) == true)
                        throw (new Exception("�� '" + strLine + "' ��ʽ����ȷ��=���ұ�ȱ��ƥ���б���"));

                    RegularString regular = new RegularString();
                    regular.RegularText = strName;
                    regular.match_list = new StringMatchList(strList.Split(new char[] {','}));

                    this.Add(regular);
                }

            }
            finally
            {
                sr.Close();
            }
        }

        // ���һ���ַ�����������ʽ
        // return:
        //      null    û�������κ�ƥ������
        //      ����    ��������
        /// <summary>
        ///  ���һ���ַ�����������ʽ
        /// </summary>
        /// <param name="strOriginText">ԭʼ����</param>
        /// <returns>������ʽ�����֡����Ϊ null ��ʾû�������κ�ƥ������</returns>
        public string GetRegularText(string strOriginText)
        {
            for (int i = 0; i < this.Count; i++)
            {
                RegularString regular = this[i];

                if (regular.match_list.Match(strOriginText) == true)
                    return regular.RegularText;
            }

            return null;
        }
    }
}