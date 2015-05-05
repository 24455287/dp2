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

// 2013/3/26 ��� XML ע��

namespace dp2Circulation
{
    /// <summary>
    /// ��ͳ�ƴ�
    /// </summary>
    public partial class ItemStatisForm : MyScriptForm
    {
        // ���ݿ�����
        /// <summary>
        /// ���ݿ����͡� item/order/issue/comment ֮һ
        /// </summary>
        public string DbType = "item";  // comment order issue

        /// <summary>
        /// �Ƿ�Ҫһ��ʼ�ͻ����Ŀ��¼ XML
        /// </summary>
        public bool FirstGetBiblbioXml = false;

        /// <summary>
        /// ��ȡ���κ�key+countֵ�б�
        /// </summary>
        public event GetKeyCountListEventHandler GetBatchNoTable = null;

#if NO
        /// <summary>
        /// ������Ϣ��
        /// </summary>
        public HtmlViewerForm ErrorInfoForm = null;
#endif

        // bool Running = false;   // ����ִ������

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

        ItemStatis objStatis = null;
        Assembly AssemblyMain = null;

        Assembly AssemblyFilter = null;
        AnotherFilterDocument MarcFilter = null;

#if NO
        /// <summary>
        /// ���ȿ���
        /// </summary>
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
                return MainForm.ItemStatisAssemblyVersion;
            }
            set
            {
                MainForm.ItemStatisAssemblyVersion = value;
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
        public ItemStatisForm()
        {
            InitializeComponent();
        }

        private void ItemStatisForm_Load(object sender, EventArgs e)
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
    this.MainForm.DataDir + "\\" + this.DbType + "_statis_projects.xml";

#if NO
            ScriptManager.applicationInfo = this.MainForm.AppInfo;
            ScriptManager.CfgFilePath =
                this.MainForm.DataDir + "\\"+this.DbType+"_statis_projects.xml";
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
            this.GetBatchNoTable -= new GetKeyCountListEventHandler(ItemStatisForm_GetBatchNoTable);
            this.GetBatchNoTable += new GetKeyCountListEventHandler(ItemStatisForm_GetBatchNoTable);

            this.radioButton_inputStyle_barcodeFile.Checked = this.MainForm.AppInfo.GetBoolean(
                this.DbType + "statisform",
                "inputstyle_barcodefile",
                false);

            this.radioButton_inputStyle_recPathFile.Checked = this.MainForm.AppInfo.GetBoolean(
                this.DbType + "statisform",
                "inputstyle_recpathfile",
                false);

            /*
            this.radioButton_inputStyle_batchNo.Checked = this.MainForm.AppInfo.GetBoolean(
                "itemstatisform",
                "inputstyle_batchno",
                false);
             * */


            this.radioButton_inputStyle_readerDatabase.Checked = this.MainForm.AppInfo.GetBoolean(
                this.DbType + "statisform",
                "inputstyle_itemdatabase",
                true);


            // �����������ļ���
            this.textBox_inputBarcodeFilename.Text = this.MainForm.AppInfo.GetString(
                this.DbType + "statisform",
                "input_barcode_filename",
                "");

            // ����ļ�¼·���ļ���
            this.textBox_inputRecPathFilename.Text = this.MainForm.AppInfo.GetString(
                this.DbType + "statisform",
                "input_recpath_filename",
                "");

            // ���κ�
            this.tabComboBox_inputBatchNo.Text = this.MainForm.AppInfo.GetString(
                this.DbType + "statisform",
                "input_batchno",
                "");

            // �����ʵ�����
            this.comboBox_inputItemDbName.Text = this.MainForm.AppInfo.GetString(
                this.DbType + "statisform",
                "input_itemdbname",
                "<ȫ��>");

            // ������
            this.textBox_projectName.Text = this.MainForm.AppInfo.GetString(
                this.DbType + "statisform",
                "projectname",
                "");

            // �ݲصص��б�
            this.textBox_locationNames.Text = this.MainForm.AppInfo.GetString(
                 this.DbType + "statisform",
                 "locations",
                 "*");

            // �������б�
            this.textBox_itemTypes.Text = this.MainForm.AppInfo.GetString(
                 this.DbType + "statisform",
                 "itemtypes",
                 "*");

            this.SetWindowTitle();
        }

        void SetWindowTitle()
        {
                this.Text = this.DbTypeCaption + "ͳ�ƴ�";
                // this.label_entityDbName.Text = this.DbTypeCaption + "��(&D)";

                if (this.DbType == "item")
                {
                    this.radioButton_inputStyle_barcodeFile.Visible = true;
                    this.textBox_inputBarcodeFilename.Visible = true;
                    this.button_findInputBarcodeFilename.Visible = true;

                    if (this.tabControl_main.TabPages.IndexOf(this.tabPage_filter) == -1)
                    {
                        this.tabControl_main.TabPages.Insert(1, this.tabPage_filter);
                    }

                    this.label_inputItemDbName.Text = "ʵ�����(&I)";
                }
                else
                {
                    this.radioButton_inputStyle_barcodeFile.Visible = false;
                    this.textBox_inputBarcodeFilename.Visible = false;
                    this.button_findInputBarcodeFilename.Visible = false;

                    this.tabControl_main.TabPages.Remove(this.tabPage_filter);

                    this.label_inputItemDbName.Text = this.DbTypeCaption + "����(&I)";
                }
        }

        /// <summary>
        /// ���ݿ����͵���ʾ���ַ���
        /// </summary>
        public string DbTypeCaption
        {
            get
            {
                if (this.DbType == "item")
                    return "��";
                else if (this.DbType == "comment")
                    return "��ע";
                else if (this.DbType == "order")
                    return "����";
                else if (this.DbType == "issue")
                    return "��";
                else
                    throw new Exception("δ֪��DbType '" + this.DbType + "'");
            }
        }

        void ItemStatisForm_GetBatchNoTable(object sender, GetKeyCountListEventArgs e)
        {
            Global.GetBatchNoTable(e,
                this,
                "", // Ŀǰ����ͼ����ڿ��� TODO: ��ʵ�������԰�pubtype�б�Ϊ3̬������һ���ǡ���+����
                this.DbType,    // "item",
                this.stop,
                this.Channel);
        }

        private void ItemStatisForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void ItemStatisForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
#endif

            this.MainForm.AppInfo.SetBoolean(
                this.DbType + "statisform",
                "inputstyle_barcodefile",
                this.radioButton_inputStyle_barcodeFile.Checked);

            this.MainForm.AppInfo.SetBoolean(
                this.DbType + "statisform",
                "inputstyle_recpathfile",
                this.radioButton_inputStyle_recPathFile.Checked);

            /*
            this.MainForm.AppInfo.SetBoolean(
                "itemstatisform",
                "inputstyle_batchno",
                this.radioButton_inputStyle_batchNo.Checked);
             * */

            this.MainForm.AppInfo.SetBoolean(
                this.DbType + "statisform",
                "inputstyle_itemdatabase",
                this.radioButton_inputStyle_readerDatabase.Checked);


            // �����������ļ���
            this.MainForm.AppInfo.SetString(
                this.DbType + "statisform",
                "input_barcode_filename",
                this.textBox_inputBarcodeFilename.Text);

            // ����ļ�¼·���ļ���
            this.MainForm.AppInfo.SetString(
                this.DbType + "statisform",
                "input_recpath_filename",
                this.textBox_inputRecPathFilename.Text);

            // ���κ�
            this.MainForm.AppInfo.SetString(
                this.DbType + "statisform",
                "input_batchno",
                this.tabComboBox_inputBatchNo.Text);

            // �����ʵ�����
            this.MainForm.AppInfo.SetString(
                this.DbType + "statisform",
                "input_itemdbname",
                this.comboBox_inputItemDbName.Text);

            // ������
            this.MainForm.AppInfo.SetString(
                this.DbType + "statisform",
                "projectname",
                this.textBox_projectName.Text);

            // �ݲصص��б�
            this.MainForm.AppInfo.SetString(
                 this.DbType + "statisform",
                 "locations",
                 this.textBox_locationNames.Text);

            // �������б�
            this.MainForm.AppInfo.SetString(
                 this.DbType + "statisform",
                 "itemtypes",
                 this.textBox_itemTypes.Text);

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

        // ����ȱʡ��main.cs�ļ�
        /// <summary>
        /// ����ȱʡ��main.cs�ļ�
        /// </summary>
        /// <param name="strFileName">Ҫ�������ļ���</param>
        static void CreateDefaultMainCsFile(string strFileName)
        {
            using (StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8))
            {
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


                sw.WriteLine("public class MyStatis : ItemStatis");

                sw.WriteLine("{");

                sw.WriteLine("	public override void OnBegin(object sender, StatisEventArgs e)");
                sw.WriteLine("	{");
                sw.WriteLine("	}");

                sw.WriteLine("}");
            }
        }

        // ����ȱʡ��marcfilter.fltx�ļ�
        /// <summary>
        /// ����ȱʡ��marcfilter.fltx�ļ�
        /// </summary>
        /// <param name="strFileName">Ҫ������ MARC �������ļ���</param>
        public static void CreateDefaultMarcFilterFile(string strFileName)
        {
            using (StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8))
            {

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

            }
        }

        private void button_projectManage_Click(object sender, EventArgs e)
        {
            ProjectManageDlg dlg = new ProjectManageDlg();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.ProjectsUrl = "http://dp2003.com/dp2circulation/projects/projects.xml";
            if (this.DbType == "item")
                dlg.HostName = "ItemStatisForm";
            else if (this.DbType == "order")
                dlg.HostName = "OrderStatisForm";
            else if (this.DbType == "issue")
                dlg.HostName = "IssueStatisForm";
            else if (this.DbType == "comment")
                dlg.HostName = "CommentStatisForm";
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

        // TODO: OnEnd()�п����׳��쳣��Ҫ�ܹ��ػ�ʹ���
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

                // 2009/11/5 new add
                // ��ֹ��ǰ�����Ĵ򿪵��ļ���Ȼû�йر�
                /*
                if (this.objStatis != null)
                {
                    try
                    {
                        this.objStatis.FreeResources();
                    }
                    catch
                    {
                    }
                }
                 * */

                this.objStatis = null;
                this.AssemblyMain = null;
                AnotherFilterDocument filter = null;

                // 2009/11/5 new add
                // ��ֹ��ǰ�����Ĵ򿪵��ļ���Ȼû�йر�
                Global.ForceGarbageCollection();

                nRet = PrepareScript(strProjectName,
                    strProjectLocate,
                    out objStatis,
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

                objStatis.LocationNames = this.textBox_locationNames.Text;

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
                strError = "�ű� '" + strProjectName + "' ִ�й����׳��쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
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
            out ItemStatis objStatis,
            out AnotherFilterDocument filter,
            out string strError)
        {
            this.AssemblyMain = null;

            objStatis = null;
            filter = null;

            string strWarning = "";
            string strMainCsDllName = PathUtil.MergePath(this.InstanceDir, "\\~item_statis_main_" + Convert.ToString(AssemblyVersion++) + ".dll");    // ++

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
   									Environment.CurrentDirectory + "\\digitalplatform.Script.dll",
									Environment.CurrentDirectory + "\\digitalplatform.dp2.statis.dll",
									// Environment.CurrentDirectory + "\\Interop.SHDocVw.dll",
									Environment.CurrentDirectory + "\\dp2circulation.exe",
            };

            string strHostName = "";
            if (this.DbType == "item")
                strHostName = "ItemStatisForm";
            else if (this.DbType == "order")
                strHostName = "OrderStatisForm";
            else if (this.DbType == "issue")
                strHostName = "IssueStatisForm";
            else if (this.DbType == "comment")
                strHostName = "CommentStatisForm";


            // ����Project��Script main.cs��Assembly
            // return:
            //		-2	���������Ѿ���ʾ��������Ϣ�ˡ�
            //		-1	����
            int nRet = ScriptManager.BuildAssembly(
                strHostName,
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
                "dp2Circulation.ItemStatis");
            if (entryClassType == null)
            {
                strError = strMainCsDllName + "��û���ҵ� dp2Circulation.ItemStatis �����ࡣ";
                goto ERROR1;
            }
            // newһ��Statis��������
            objStatis = (ItemStatis)entryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);

            // ΪStatis���������ò���
            objStatis.ItemStatisForm = this;
            objStatis.ProjectDir = strProjectLocate;
            objStatis.InstanceDir = this.InstanceDir;

            ////////////////////////////
            // װ��marfilter.fltx
            string strFilterFileName = strProjectLocate + "\\marcfilter.fltx";

            if (FileUtil.FileExist(strFilterFileName) == true)
            {
                filter = new AnotherFilterDocument();
                filter.ItemStatis = objStatis;
                filter.strOtherDef = entryClassType.FullName + " ItemStatis = null;";


                filter.strPreInitial = " AnotherFilterDocument doc = (AnotherFilterDocument)this.Document;\r\n";
                filter.strPreInitial += " ItemStatis = ("
                    + entryClassType.FullName + ")doc.ItemStatis;\r\n";

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
                string[] saAdditionalRef = filter.GetRefs();

                // �ϲ������ӿ�
                string[] saTotalFilterRef = new string[saAddRef1.Length + saAdditionalRef.Length];
                Array.Copy(saAddRef1, saTotalFilterRef, saAddRef1.Length);
                Array.Copy(saAdditionalRef, 0,
                    saTotalFilterRef, saAddRef1.Length,
                    saAdditionalRef.Length);


                string strfilterCsDllName = strProjectLocate + "\\~marcfilter_" + Convert.ToString(AssemblyVersion++) + ".dll";

                // ����Project��Script��Assembly
                nRet = ScriptManager.BuildAssembly(
                    strHostName,
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

        internal int DoMarcFilter(
            int nIndex,
            string strMarcRecord,
            string strMarcSyntax,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (this.MarcFilter == null)
                return 0;

            // ����filter�е�Record��ض���
            nRet = this.MarcFilter.DoRecord(
                null,
                strMarcRecord,
                strMarcSyntax,
                nIndex,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // ע�⣺�ϼ�����RunScript()�Ѿ�ʹ����BeginLoop()��EnableControls()
        // ��ÿ��ʵ���¼����ѭ��
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


            // �ݲصص�����б�
            string strLocationList = this.textBox_locationNames.Text.Trim();
            if (String.IsNullOrEmpty(strLocationList) == true)
                strLocationList = "*";

            string[] locations = strLocationList.Split(new char[] { ',' });

            StringMatchList location_matchlist = new StringMatchList(locations);

            // ʵ�����͹����б�
            string strItemTypeList = this.textBox_itemTypes.Text.Trim();
            if (String.IsNullOrEmpty(strItemTypeList) == true)
                strItemTypeList = "*";

            string[] itemtypes = strItemTypeList.Split(new char[] { ',' });

            StringMatchList itemtype_matchlist = new StringMatchList(itemtypes);

            // ��¼·����ʱ�ļ�
            string strTempRecPathFilename = Path.GetTempFileName();

            string strInputFileName = "";   // �ⲿ�ƶ��������ļ���Ϊ������ļ����߼�¼·���ļ���ʽ
            string strAccessPointName = "";

            try
            {

                if (this.InputStyle == ItemStatisInputStyle.BatchNo)
                {
                    nRet = SearchItemRecPath(
                        this.tabComboBox_inputBatchNo.Text,
                        strTempRecPathFilename,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    strInputFileName = strTempRecPathFilename;
                    strAccessPointName = "��¼·��";
                }
                else if (this.InputStyle == ItemStatisInputStyle.BarcodeFile)
                {
                    Debug.Assert(this.DbType == "item", "");

                    strInputFileName = this.textBox_inputBarcodeFilename.Text;
                    strAccessPointName = "������";
                }
                else if (this.InputStyle == ItemStatisInputStyle.RecPathFile)
                {
                    strInputFileName = this.textBox_inputRecPathFilename.Text;
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
                stop.Initial("���ڻ�ȡ���¼ ...");
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
                                    this.DbType + "statisform",
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

                        OutputDebugInfo("������" + (i + 1).ToString() + " '" + strRecPathOrBarcode + "'");

                        stop.SetMessage("���ڻ�ȡ�� " + (i + 1).ToString() + " ��"+this.DbTypeCaption+"��¼��" + strAccessPointName + "Ϊ " + strRecPathOrBarcode);
                        this.progressBar_records.Value = (int)sr.BaseStream.Position;

                        // ��ò��¼
                        string strOutputRecPath = "";
                        byte[] baTimestamp = null;


                        string strResult = "";

                        string strAccessPoint = "";
                        if (this.InputStyle == ItemStatisInputStyle.BatchNo)
                            strAccessPoint = "@path:" + strRecPathOrBarcode;
                        else if (this.InputStyle == ItemStatisInputStyle.RecPathFile)
                            strAccessPoint = "@path:" + strRecPathOrBarcode;
                        else if (this.InputStyle == ItemStatisInputStyle.BarcodeFile)
                            strAccessPoint = strRecPathOrBarcode;
                        else
                        {
                            Debug.Assert(false, "");
                        }

                        string strBiblio = "";
                        string strBiblioRecPath = "";
                        string strBiblioType = "recpath";
                        if (this.FirstGetBiblbioXml == true)
                            strBiblioType = "xml";

                        if (this.DbType == "item")
                        {
                            // Result.Value -1���� 0û���ҵ� 1�ҵ� >1���ж���1��
                            lRet = Channel.GetItemInfo(
                                stop,
                                strAccessPoint,
                                "xml", // strResultType,
                                out strResult,
                                out strOutputRecPath,
                                out baTimestamp,
                                strBiblioType,
                                out strBiblio,
                                out strBiblioRecPath,
                                out strError);
                        }
                        else if (this.DbType == "order")
                        {
                            // Result.Value -1���� 0û���ҵ� 1�ҵ� >1���ж���1��
                            lRet = Channel.GetOrderInfo(
                                stop,
                                strAccessPoint,
                                "xml", // strResultType,
                                out strResult,
                                out strOutputRecPath,
                                out baTimestamp,
                                strBiblioType,
                                out strBiblio,
                                out strBiblioRecPath,
                                out strError);
                        }
                        else if (this.DbType == "issue")
                        {
                            // Result.Value -1���� 0û���ҵ� 1�ҵ� >1���ж���1��
                            lRet = Channel.GetIssueInfo(
                                stop,
                                strAccessPoint,
                                "xml", // strResultType,
                                out strResult,
                                out strOutputRecPath,
                                out baTimestamp,
                                strBiblioType,
                                out strBiblio,
                                out strBiblioRecPath,
                                out strError);
                        }
                        else if (this.DbType == "comment")
                        {
                            // Result.Value -1���� 0û���ҵ� 1�ҵ� >1���ж���1��
                            lRet = Channel.GetCommentInfo(
                                stop,
                                strAccessPoint,
                                "xml", // strResultType,
                                out strResult,
                                out strOutputRecPath,
                                out baTimestamp,
                                strBiblioType,
                                out strBiblio,
                                out strBiblioRecPath,
                                out strError);
                        }                        
                        if (lRet == -1)
                        {
                            strError = "���"+this.DbTypeCaption+"��¼ " + strAccessPoint + " ʱ��������: " + strError;
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                            continue;
                        }

                        if (lRet == 0)
                        {
                            strError = "" + strAccessPointName + " " + strRecPathOrBarcode + " ��Ӧ��XML����û���ҵ���";
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                            continue;
                        }

                        if (lRet > 1)
                        {
                            strError = "" + strAccessPointName + " " + strRecPathOrBarcode + " ��Ӧ���ݶ���һ����";
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                            continue;
                        }

                        string strXml = "";

                        strXml = strResult;


                        // �����Ƿ���ϣ��ͳ�Ƶķ�Χ��
                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(strXml);
                        }
                        catch (Exception ex)
                        {
                            strError = "���¼װ��DOM��������: " + ex.Message;
                            GetErrorInfoForm().WriteHtml(strError + "\r\n");
                            continue;
                        }

                        if (this.DbType == "item")
                        {
                            // ���չݲصص�ɸѡ
                            if (this.textBox_locationNames.Text != ""
                                && this.textBox_locationNames.Text != "*")
                            {
                                // ע�����ַ�������"*"��ʾʲô�����㡣Ҳ�͵��ڲ�ʹ�ô�ɸѡ��

                                string strLocation = DomUtil.GetElementText(dom.DocumentElement,
                                    "location");
                                if (location_matchlist.Match(strLocation) == false)
                                {
                                    OutputDebugInfo("�ݲص� '"+strLocation+"' ��ɸѡȥ��");
                                    continue;
                                }
                            }

                            // ���ղ�����ɸѡ
                            if (this.textBox_itemTypes.Text != ""
                                && this.textBox_itemTypes.Text != "*")
                            {
                                // ע�����ַ�������"*"��ʾʲô�����㡣Ҳ�͵��ڲ�ʹ�ô�ɸѡ��

                                string strItemType = DomUtil.GetElementText(dom.DocumentElement,
                                    "bookType");
                                if (itemtype_matchlist.Match(strItemType) == false)
                                {
                                    OutputDebugInfo("������ '" + strItemType + "' ��ɸѡȥ��");
                                    continue;
                                }
                            }
                        }

                        // Debug.Assert(false, "");

                        // strXml��Ϊ���¼

                        // ����Script��OnRecord()����
                        if (objStatis != null)
                        {
                            objStatis.Xml = strXml;
                            objStatis.Timestamp = baTimestamp;
                            objStatis.ItemDom = dom;
                            objStatis.CurrentRecPath = strOutputRecPath;
                            objStatis.CurrentRecordIndex = i;
                            objStatis.CurrentBiblioRecPath = strBiblioRecPath;

                            if (this.FirstGetBiblbioXml == true)
                            {
                                objStatis.m_strBiblioXml = strBiblio;
                            }
                            else
                            {
                                objStatis.m_strBiblioXml = null;   // ��ʹ�õ���ʱ�����»�ȡ
                            }

                            objStatis.m_biblioDom = null;   // ��ʹ�õ���ʱ�����»�ȡ
                            objStatis.m_strMarcRecord = null;   // ��ʹ�õ���ʱ�����»�ȡ
                            objStatis.m_strMarcSyntax = null;   // ��ʹ�õ���ʱ�����»�ȡ


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

        void OutputDebugInfo(string strText)
        {
            if (this.checkBox_selectProject_outputDebugInfo.Checked == true)
                GetErrorInfoForm().WriteHtml(strText + "\r\n");
        }

        // ע�⣺�ϼ�����RunScript()�Ѿ�ʹ����BeginLoop()��EnableControls()
        // ��������ض����κţ��������в��¼·��(������ļ�)
        int SearchItemRecPath(
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

                    // ��ָ�����κţ���ζ���ض���ȫ����¼��2013/1/25
                    if (String.IsNullOrEmpty(strBatchNo) == true)
                    {
                        if (this.DbType == "item")
                        {
                            // TODO: �Ƿ�Ӧ����__id�����ʣ���ΪһЩ��¼û�в������
                            lRet = Channel.SearchItem(stop,
                                this.comboBox_inputItemDbName.Text,
                                 "",
                                 -1,
                                 "__id", // 2013/1/25   // "������",
                                 "left",
                                 this.Lang,
                                 null,   // strResultSetName
                                 "",    // strSearchStyle
                                 "", // strOutputStyle
                                 out strError);
                        }
                        else if (this.DbType == "order")
                        {
                            lRet = Channel.SearchOrder(stop,
                                this.comboBox_inputItemDbName.Text,
                                 "",
                                 -1,
                                 "__id",
                                 "left",
                                 this.Lang,
                                 null,   // strResultSetName
                                 "",    // strSearchStyle
                                 "", // strOutputStyle
                                 out strError);
                        }
                        else if (this.DbType == "issue")
                        {
                            lRet = Channel.SearchIssue(stop,
                                this.comboBox_inputItemDbName.Text,
                                 "",
                                 -1,
                                 "__id",
                                 "left",
                                 this.Lang,
                                 null,   // strResultSetName
                                 "",    // strSearchStyle
                                 "", // strOutputStyle
                                 out strError);
                        }
                        else if (this.DbType == "comment")
                        {
                            lRet = Channel.SearchComment(stop,
                                this.comboBox_inputItemDbName.Text,
                                 "",
                                 -1,
                                 "__id",
                                 "left",
                                 this.Lang,
                                 null,   // strResultSetName
                                 "",    // strSearchStyle
                                 "", // strOutputStyle
                                 out strError);
                        }
                        if (lRet == -1)
                            goto ERROR1;
                    }
                    else
                    {
                        // ָ�����κš��ض��⡣
                        if (this.DbType == "item")
                        {
                            lRet = Channel.SearchItem(stop,
                                    this.comboBox_inputItemDbName.Text,
                                    strBatchNo,
                                    -1,
                                    "���κ�",
                                    "exact",
                                    this.Lang,
                                    null,   // strResultSetName
                                    "",    // strSearchStyle
                                    "", // strOutputStyle
                                    out strError);
                        }
                        else if (this.DbType == "order")
                        {
                            lRet = Channel.SearchOrder(stop,
                                    this.comboBox_inputItemDbName.Text,
                                    strBatchNo,
                                    -1,
                                    "���κ�",
                                    "exact",
                                    this.Lang,
                                    null,   // strResultSetName
                                    "",    // strSearchStyle
                                    "", // strOutputStyle
                                    out strError);
                        }
                        else if (this.DbType == "issue")
                        {
                            lRet = Channel.SearchIssue(stop,
                                    this.comboBox_inputItemDbName.Text,
                                    strBatchNo,
                                    -1,
                                    "���κ�",
                                    "exact",
                                    this.Lang,
                                    null,   // strResultSetName
                                    "",    // strSearchStyle
                                    "", // strOutputStyle
                                    out strError);
                        }
                        else if (this.DbType == "comment")
                        {
                            lRet = Channel.SearchComment(stop,
                                    this.comboBox_inputItemDbName.Text,
                                    strBatchNo,
                                    -1,
                                    "���κ�",
                                    "exact",
                                    this.Lang,
                                    null,   // strResultSetName
                                    "",    // strSearchStyle
                                    "", // strOutputStyle
                                    out strError);
                        }
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

        private void button_next_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.tabControl_main.SelectedTab == this.tabPage_source)
            {
                if (this.DbType == "item")
                {
                    if (this.radioButton_inputStyle_barcodeFile.Checked == true)
                    {
                        if (this.textBox_inputBarcodeFilename.Text == "")
                        {
                            strError = "��δָ�������������ļ���";
                            goto ERROR1;
                        }
                    }
                }
                if (this.radioButton_inputStyle_recPathFile.Checked == true)
                {
                    if (this.textBox_inputRecPathFilename.Text == "")
                    {
                        strError = "��δָ������ļ�¼·���ļ���";
                        goto ERROR1;
                    }
                }
                else
                {
                    if (this.comboBox_inputItemDbName.Text == "")
                    {
                        strError = "��δָ��"+this.DbTypeCaption+"����";
                        goto ERROR1;
                    }
                }

                if (this.DbType == "item")
                {
                    // �л�����������page
                    this.tabControl_main.SelectedTab = this.tabPage_filter;
                }
                else
                {
                    this.tabControl_main.SelectedTab = this.tabPage_selectProject;
                }
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

                this.tabComboBox_inputBatchNo.Enabled = false;
                this.comboBox_inputItemDbName.Enabled = false;
            }
            else if (this.radioButton_inputStyle_recPathFile.Checked == true)
            {
                this.textBox_inputBarcodeFilename.Enabled = false;
                this.button_findInputBarcodeFilename.Enabled = false;

                this.textBox_inputRecPathFilename.Enabled = true;
                this.button_findInputRecPathFilename.Enabled = true;


                this.tabComboBox_inputBatchNo.Enabled = false;
                this.comboBox_inputItemDbName.Enabled = false;
            }
            else
            {
                this.textBox_inputBarcodeFilename.Enabled = false;
                this.button_findInputBarcodeFilename.Enabled = false;

                this.textBox_inputRecPathFilename.Enabled = false;
                this.button_findInputRecPathFilename.Enabled = false;

                this.tabComboBox_inputBatchNo.Enabled = true;
                this.comboBox_inputItemDbName.Enabled = true;
            }
        }

        // ������
        /// <summary>
        /// ���뷽ʽ
        /// </summary>
        public ItemStatisInputStyle InputStyle
        {
            get
            {
                if (this.radioButton_inputStyle_barcodeFile.Checked == true)
                    return ItemStatisInputStyle.BarcodeFile;
                else if (this.radioButton_inputStyle_recPathFile.Checked == true)
                    return ItemStatisInputStyle.RecPathFile;
                else
                    return ItemStatisInputStyle.BatchNo;
            }
        }

        private void button_findInputBarcodeFilename_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "��ָ��Ҫ�򿪵Ĳ�������ļ���";
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

            dlg.Title = "��ָ��Ҫ�򿪵Ĳ��¼·���ļ���";
            dlg.FileName = this.textBox_inputRecPathFilename.Text;
            dlg.Filter = "��¼·���ļ� (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            this.textBox_inputRecPathFilename.Text = dlg.FileName;

        }

        private void comboBox_inputItemDbName_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_inputItemDbName.Items.Count > 0)
                return;

            this.comboBox_inputItemDbName.Items.Add("<ȫ��>");
            this.comboBox_inputItemDbName.Items.Add("<ȫ���ڿ�>");
            this.comboBox_inputItemDbName.Items.Add("<ȫ��ͼ��>");

            if (this.MainForm.BiblioDbProperties != null)
            {
                for (int i = 0; i < this.MainForm.BiblioDbProperties.Count; i++)
                {
                    BiblioDbProperty prop = this.MainForm.BiblioDbProperties[i];

                    string strDbName = "";
                    if (this.DbType == "item")
                        strDbName = prop.ItemDbName;
                    else if (this.DbType == "order")
                        strDbName = prop.OrderDbName;
                    else if (this.DbType == "issue")
                        strDbName = prop.IssueDbName;
                    else if (this.DbType == "comment")
                        strDbName = prop.CommentDbName;

                    if (String.IsNullOrEmpty(strDbName) == true)
                        continue;

                    this.comboBox_inputItemDbName.Items.Add(strDbName);
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

        /// <summary>
        /// �����Ŀ��Ϣ
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strBiblioType">Ҫ��õ���Ϣ��ʽ�������ö��ָ�ʽ֮һ��xml / html / text / @??? / summary / outputpath</param>
        /// <param name="strBiblio">������Ŀ��Ϣ</param>
        /// <param name="strError">���ش�����Ϣ</param>
        /// <returns>-1������������Ϣ�ڲ��� strError �з��أ� 0��û���ҵ��� 1���ҵ�</returns>
        public int GetBiblioInfo(string strBiblioRecPath,
            string strBiblioType,
            out string strBiblio,
            out string strError)
        {
            strError = "";
            strBiblio = "";

            string strBiblioXml = "";   // ��������ṩ��XML��¼
            long lRet = this.Channel.GetBiblioInfo(
                null,   // this.stop,
                strBiblioRecPath,
                strBiblioXml,
                strBiblioType,
                out strBiblio,
                out strError);
            return (int)lRet;
        }

        private void ItemStatisForm_Activated(object sender, EventArgs e)
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

    }

    /// <summary>
    /// ��ͳ�ƴ������뷽ʽ
    /// </summary>
    public enum ItemStatisInputStyle
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
        /// ���κ� ������ȫ�������
        /// </summary>
        BatchNo = 3,    // ���κ� ������ȫ�������
    }

    /// <summary>
    /// ���ڲ�ͳ�Ƶ� FilterDocument ������(MARC �������ĵ���)
    /// </summary>
    public class AnotherFilterDocument : FilterDocument
    {
        /// <summary>
        /// ��������
        /// </summary>
        public ItemStatis ItemStatis = null;
    }
}