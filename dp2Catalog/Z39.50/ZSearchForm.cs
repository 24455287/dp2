using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Threading;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Z3950;

using DigitalPlatform.Script;
using DigitalPlatform.Marc;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.CirculationClient;    // for NormalDbProperty

namespace dp2Catalog
{
    public partial class ZSearchForm : Form, ISearchForm, IZSearchForm
    {
        public string UsedLogFilename = ""; // �����ù���ͨѶ����־�ļ���

        // int m_nGroupSearchCount = 0;
        Stop m_stopDir = null;
        ZConnection m_connectionDir = null;
        int m_nTotalHitCount = 0;
        int m_nCompleteServerCount = 0; // �Ѿ���ɼ����ķ�������
        int m_nServerCount = 0; // ��Ҫ�����ķ�������������m_stops.CountӦ�����
        List<Stop> m_stops = new List<Stop>();
        bool m_bStartGroupSearch = false;

        VirtualItemCollection CurrentBrowseItems = null;
        // string m_strInitialResultInfo = ""; // ��ʼ�������Ϣ

        // �������������±�
        public const int BROWSE_TYPE_NORMAL = 0;   // ��ͨ��¼
        public const int BROWSE_TYPE_DIAG = 1;     // ��ϼ�¼ ���� 4
        public const int BROWSE_TYPE_BRIEF = 2;     // �򻯸�ʽ
        public const int BROWSE_TYPE_FULL = 3;     // ��ϸ��ʽ

        const int WM_LOADSIZE = API.WM_USER + 201;


        public string CurrentRefID = "0";   // "1 0 116101 11 1";

        MainForm m_mainForm = null;

        public MainForm MainForm
        {
            get
            {
                return this.m_mainForm;
            }
            set
            {
                this.m_mainForm = value;
            }
        }

        // DigitalPlatform.Stop Stop = null;

        // public ZChannel ZChannel = new ZChannel();
        public ZConnectionCollection ZConnections = new ZConnectionCollection();


        public string BinDir = "";

        // MarcFilter���󻺳��
        public FilterCollection Filters = new FilterCollection();

        // ����ʽ�ͽ��������
        // public TargetInfo CurrentTargetInfo = null;
        // public string CurrentQueryString = "";
        // public int ResultCount = 0;

        // Encoding ForcedRecordsEncoding = null;

        // 



        #region ����



        #endregion

        public ZSearchForm()
        {
            InitializeComponent();
        }

        private void ZSearchForm_Load(object sender, EventArgs e)
        {
            if (this.m_mainForm != null)
            {
                GuiUtil.SetControlFont(this, this.m_mainForm.DefaultFont);
            }

            this.ZConnections.IZSearchForm = this;

            this.BinDir = Environment.CurrentDirectory;

            /*
            Stop = new DigitalPlatform.Stop();
            Stop.Register(MainForm.stopManager);	// ����������
             * */

            string strWidths = this.m_mainForm.AppInfo.GetString(
"zsearchform",
"record_list_column_width",
"");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_browse,
                    strWidths,
                    true);
            }
            string[] fromlist = this.m_mainForm.GetFromList();

            for (int i = 0; i < 4; i++)
            {
                this.queryControl1.AddLine(fromlist);
            }

            int nRet = 0;
            string strError = "";
            nRet = this.zTargetControl1.Load(Path.Combine(m_mainForm.UserDir, "zserver.xml"),
                out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);

            this.zTargetControl1.Marc8Encoding = this.m_mainForm.Marc8Encoding;

            this.zTargetControl1.MainForm = this.m_mainForm;  // 2007/12/16

            //// this.ZChannel.CommIdle += new CommIdleEventHandle(ZChannel_CommIdle);
            this.zTargetControl1.AllowCheckbox = false;


            // �ָ��ϴ����µļ���ʽ
            string strContentsXml = m_mainForm.AppInfo.GetString(
                "zsearchform",
                "query_contents",
                "");
            /*
            if (String.IsNullOrEmpty(strContentXml) == false)
                this.queryControl1.SetContent(strContentXml);
             * */
            this.ZConnections.SetAllQueryXml(strContentsXml,
                this.zTargetControl1);

            // ѡ���ϴ�ѡ�������ڵ�
            string strLastTargetPath = m_mainForm.AppInfo.GetString(
                "zsearchform",
                "last_targetpath",
                "");
            if (String.IsNullOrEmpty(strLastTargetPath) == false)
            {
                TreeViewUtil.SelectTreeNode(this.zTargetControl1,
                    strLastTargetPath,
                    '\\');
            }

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);
        }

        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_LOADSIZE:
                    LoadSize();
                    return;
            }
            base.DefWndProc(ref m);
        }

        public void LoadSize()
        {
            // ���ô��ڳߴ�״̬
            m_mainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state",
                MainForm.DefaultMdiWindowWidth,
                MainForm.DefaultMdiWindowHeight);


            // ���splitContainer_main��״̬
            /*
            int nValue = MainForm.AppInfo.GetInt(
            "zsearchform",
            "splitContainer_main",
            -1);
            if (nValue != -1)
            {
                try
                {
                    this.splitContainer_main.SplitterDistance = nValue;
                }
                catch
                {
                }
            }
             * */
            this.m_mainForm.LoadSplitterPos(
this.splitContainer_main,
"zsearchform",
"splitContainer_main");


            // ���splitContainer_up��״̬
            /*
            nValue = MainForm.AppInfo.GetInt(
            "zsearchform",
            "splitContainer_up",
            -1);
            if (nValue != -1)
            {
                try
                {
                    this.splitContainer_up.SplitterDistance = nValue;
                }
                catch
                {
                }
            }
             * */
            this.m_mainForm.LoadSplitterPos(
this.splitContainer_up,
"zsearchform",
"splitContainer_up");

            // ���splitContainer_queryAndResultInfo��״̬
            /*
            nValue = MainForm.AppInfo.GetInt(
            "zsearchform",
            "splitContainer_queryAndResultInfo",
            -1);
            if (nValue != -1)
            {
                try
                {
                    this.splitContainer_queryAndResultInfo.SplitterDistance = nValue;
                }
                catch
                {
                }
            }
             * */
            this.m_mainForm.LoadSplitterPos(
this.splitContainer_queryAndResultInfo,
"zsearchform",
"splitContainer_queryAndResultInfo");

        }

        public void SaveSize()
        {
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                m_mainForm.AppInfo.SaveMdiChildFormStates(this,
                    "mdi_form_state");

                // ����splitContainer_main��״̬
                /*
                MainForm.AppInfo.SetInt(
                    "zsearchform",
                    "splitContainer_main",
                    this.splitContainer_main.SplitterDistance);
                 * */
                this.m_mainForm.SaveSplitterPos(
        this.splitContainer_main,
        "zsearchform",
        "splitContainer_main");

                // ����splitContainer_up��״̬
                /*
                MainForm.AppInfo.SetInt(
                    "zsearchform",
                    "splitContainer_up",
                    this.splitContainer_up.SplitterDistance);
                 * */
                this.m_mainForm.SaveSplitterPos(
    this.splitContainer_up,
    "zsearchform",
    "splitContainer_up");

                // ����splitContainer_queryAndResultInfo��״̬
                /*
                MainForm.AppInfo.SetInt(
                    "zsearchform",
                    "splitContainer_queryAndResultInfo",
                    this.splitContainer_queryAndResultInfo.SplitterDistance);
                 * */
                this.m_mainForm.SaveSplitterPos(
    this.splitContainer_queryAndResultInfo,
    "zsearchform",
    "splitContainer_queryAndResultInfo");
            }
        }

        void ZChannel_CommIdle(object sender, CommIdleEventArgs e)
        {
            Application.DoEvents();
        }

        private void ZSearchForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                ZConnection connection = this.GetCurrentZConnection();
                if (connection != null)
                {
                    if (connection.Stop.State == 0)
                    {
                        DialogResult result = MessageBox.Show(this,
    "�������ڽ��С���Ҫ��ֹͣ�������������ܹرմ��ڡ�\r\n\r\nҪֹͣ��������ô?",
    "ZSearchForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                        if (result == DialogResult.Yes)
                        {
                            connection.Stop.DoStop();
                        }
                        e.Cancel = true;
                        return;

                    }
                }
            }
            catch
            {
            }

            if (this.m_stops != null
                && this.m_stops.Count > 0)
            {
                DialogResult result = MessageBox.Show(this,
                "Ⱥ�����ڽ��С���Ҫ��ֹͣ�������������ܹرմ��ڡ�\r\n\r\nҪֹͣ��������ô?",
                "ZSearchForm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                {
                    StopDirSearchStops(true);

                }
                e.Cancel = true;
                return;
            }

            if (this.m_stops != null)
            {
                StopDirSearchStops(true);
            }

            //// this.ZChannel.CloseSocket();
            this.ZConnections.CloseAllSocket();
        }



        private void ZSearchForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            /*
            if (Stop != null) // �������
            {
                Stop.DoStop();

                Stop.Unregister();	// ����������
                Stop = null;
            }*/
            this.ZConnections.UnlinkAllStop();

            //// this.ZChannel.CommIdle -= new CommIdleEventHandle(ZChannel_CommIdle);

            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                string strLastTargetPath = ZTargetControl.GetNodeFullPath(this.zTargetControl1.SelectedNode,
                    '\\');

                // TODO: applicationInfo��ʱΪnull
                m_mainForm.AppInfo.SetString(
                    "zsearchform",
                    "last_targetpath",
                    strLastTargetPath);

                // ��ʹ��ǰһ��treenode�ļ���ʽ�͵�connections�ṹ�У��Ա㱣��
                zTargetControl1_BeforeSelect(null, null);

                m_mainForm.AppInfo.SetString(
                    "zsearchform",
                    "query_contents",
                    this.ZConnections.GetAllQueryXml());

                // "query_content",
                // this.queryControl1.GetContent());

                string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_browse);
                this.m_mainForm.AppInfo.SetString(
                    "zsearchform",
                    "record_list_column_width",
                    strWidths);
            }

            SaveSize();

            this.zTargetControl1.Save();

        }



        // 2007/7/28
        // ��õ�ǰѡ�е�һ��������Ŀ����ص�ZConnection
        // ���û��ZConnection�����Զ�����
        // �����ǰѡ�����database���ͣ���Ҫ�����ҵ�����������server�ڵ�
        // TODO: �����ǰѡ�����һ��dir���ͽڵ㣬�ǲ�����ζ��Ҫ�ж����Ŀ¼�е����нڵ���?
        ZConnection FindCurrentZConnection()
        {
            TreeNode curTreeNode = ZTargetControl.GetServerNode(this.zTargetControl1.SelectedNode);
            if (curTreeNode == null)
                return null;

            ZConnection result = this.ZConnections.FindZConnection(curTreeNode);

            return result;
        }


        // 2007/7/28
        // ��ú�һ�����������ڵ���ص�ZConnection
        // ���û��ZConnection���Զ�����
        ZConnection GetZConnection(TreeNode node)
        {

            /*
            if (ZTargetControl.IsServer(nodeServerOrDatabase.ImageIndex) == false
                && ZTargetControl.IsDatabaseType(nodeServerOrDatabase) == false)
            {
                string strError = "�����������ڵ㲻�Ƿ��������� �� ���ݿ�����";
                throw new Exception(strError);
            }
             * 
            TreeNode nodeServer = ZTargetControl.GetServerNode(nodeServerOrDatabase);
            if (nodeServer == null)
            {
                string strError = "�����������ڵ������ݿ����ͣ����丸�ڵ��Ȼ���Ƿ���������";
                throw new Exception(strError);
                // return null;
            }


            ZConnection connection = this.ZConnections.GetZConnection(nodeServer);
             * */
            ZConnection connection = this.ZConnections.GetZConnection(node);

            if (connection.TargetInfo == null
                && ZTargetControl.IsDirType(node) == false)
            {
                string strError = "";
                TargetInfo targetinfo = null;
                int nRet = this.zTargetControl1.GetTarget(
                    node,
                    out targetinfo,
                    out strError);
                if (nRet == -1)
                {
                    throw new Exception("GetCurrentZConnection() error: " + strError);
                    // return null;
                }

                connection.TargetInfo = targetinfo;

                /*
                // ��ǰѡ��������database���ͽڵ㣬���޸�targetinfo
                // TODO: ע�⣬���������server���ͽڵ���ѡ��ʱ���ɲ�Ҫ��������޸Ĺ���targetinfo
                if (nodeServer != nodeServerOrDatabase)
                {
                    targetinfo.DbNames = new string[1];
                    targetinfo.DbNames[0] = nodeServerOrDatabase.Text;  // ע�������û�����������֣�
                }*/
            }

            return connection;
        }

        // 2007/7/28
        // ��õ�ǰѡ�е�һ��������Ŀ����ص�ZConnection
        // ���û��ZConnection���Զ�����
        // �����ǰѡ�����database���ͣ���Ҫ�����ҵ�����������server�ڵ�
        ZConnection GetCurrentZConnection()
        {
            /*
            // �����Ҫ������Ϊserver���ͽڵ�
            TreeNode curTreeNode = this.zTargetControl1.GetServerNode(this.zTargetControl1.SelectedNode);
            if (curTreeNode == null)
                return null;
             * */
            TreeNode curTreeNode = this.zTargetControl1.SelectedNode;
            if (curTreeNode == null)
                return null;

            ZConnection connection = this.ZConnections.GetZConnection(curTreeNode);

            if (connection.TargetInfo == null
                && ZTargetControl.IsDirType(curTreeNode) == false)
            {
                string strError = "";
                TargetInfo targetinfo = null;
                int nRet = this.zTargetControl1.GetTarget(
                    curTreeNode,
                    out targetinfo,
                    out strError);
                if (nRet == -1)
                {
                    throw new Exception("GetCurrentZConnection() error: " + strError);
                    // return null;
                }

                connection.TargetInfo = targetinfo;
            }

            return connection;
        }

        // ֹͣ��ǰ��һ��Z����
        void DoStop(object sender, StopEventArgs e)
        {
            /*
            if (this.ZChannel.Connected == true)
            {
                CloseConnection();
            }
            else if (this.ZChannel != null)
            {
                // �������û�����ӵ�״̬
                this.ZChannel.Stop();
            }
             * */
            ZConnection connection = this.FindCurrentZConnection();
            if (connection != null)
            {
                connection.DoStop();
            }
        }

        // ����һ��Ŀ¼��
        // ͬʱ����Ŀ¼�µ����з�����
        public int PrepareSearchOneDir(
            TreeNode nodeDir,
            string strQueryXml,
            ref List<ZConnection> connections,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // ����ÿյ��ã���ʾʹ�õ�ǰ����ѡ���Ľڵ�
            if (nodeDir == null)
            {
                nodeDir = this.zTargetControl1.SelectedNode;
                if (ZTargetControl.IsDirType(nodeDir) == false)
                {
                    strError = "��ǰ���ڵ����Ͳ���Ŀ¼�� ������nodeDirΪ�յ���DoSearchOneDir()";
                    goto ERROR1;
                }
            }

            TreeNodeCollection nodes = nodeDir.Nodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                TreeNode node = nodes[i];

                if (ZTargetControl.IsDirType(node) == true)
                {
                    nRet = PrepareSearchOneDir(node,
                        strQueryXml,
                        ref connections,
                        out strError);
                }
                else if (ZTargetControl.IsDatabaseType(node) == true
                    || ZTargetControl.IsServerType(node) == true)
                {
                    // return:
                    //      -2  ��δ���������
                    //      -1  һ�����
                    //      0   �ɹ�׼������
                    nRet = PrepareSearchOneServer(
                        node,
                        strQueryXml,
                        ref connections,
                        out strError);
                    // TODO: ���������ʾ�ڸ��Եļ����������
                }

                /*
                if (nRet == -1)
                    return -1;
                 * */
            }

            return 0;
        ERROR1:
            return -1;
        }

        int m_nInTestSearching = 0;
        bool m_bTestStop = false;

        public int DoTestSearch()
        {
            if (this.m_nInTestSearching > 1)
            {
                this.m_bTestStop = true;
                return 0;
            }

            m_nInTestSearching++;
            try
            {
                m_bTestStop = false;
                for (; ; )
                {
                    if (m_bTestStop == true)
                        break;

                    DoSearch();

                    ZConnection connection = this.GetZConnection(this.zTargetControl1.SelectedNode);

                    while (true)
                    {
                        Application.DoEvents();
                        Thread.Sleep(1000);
                        if (connection.Searching == 2)
                        {
                            break;
                        }
                    }

                    connection.CloseConnection();
                }
            }
            finally
            {
                m_nInTestSearching--;
            }

            MessageBox.Show(this, "end");
            return 0;
        }

        // ����һ������������Ŀ¼
        // ���ǰ�װ������ⲿ���õİ汾 -- ������ǰĿ������ѡ���Ľڵ�
        public int DoSearch()
        {
            string strError = "";
            int nRet = 0;

            TreeNode node = this.zTargetControl1.SelectedNode;
            if (ZTargetControl.IsServerType(node) == true
                || ZTargetControl.IsDatabaseType(node) == true)
            {
                // return:
                //      -2  ��δ���������
                //      -1  һ�����
                //      0   �ɹ���������
                nRet = DoSearchOneServer(
                    node,
                    out strError);
                if (nRet == -1 || nRet == -2)
                    goto ERROR1;
            }
            else
            {
                Debug.Assert(ZTargetControl.IsDirType(node) == true, "");

                if (this.m_stops.Count != 0)
                {
                    // TODO: �Ƿ���Ҫ��ʾ���ĸ�Ŀ¼���ڽ���Ⱥ��?
                    strError = "��ǰ���ڽ�����Ⱥ�죬������������Ⱥ��";
                    goto ERROR1;
                }

                ZConnection connection = this.GetCurrentZConnection();

                // ��ù�ͬ�ļ���ʽ
                connection.QueryXml = this.queryControl1.GetContent(true);
                if (String.IsNullOrEmpty(connection.QueryXml) == true)
                {
                    strError = "��δ�������ʽ";
                    goto ERROR1;
                }

                // ׼������
                this.m_nTotalHitCount = 0;
                this.m_nCompleteServerCount = 0;
                this.m_nServerCount = 0;

                this.m_stopDir = connection.Stop;
                this.m_connectionDir = connection;
                this.m_stops.Clear();
                this.m_bStartGroupSearch = false;

                node.Expand();

                /*
                lock (this)
                {
                } 
                 * */

#if NOOOOOOOOOOOOO
                this.m_stopDir.OnStop += new StopEventHandler(m_stopDir_OnStop);
                    this.m_stopDir.SetMessage("��ʼ���� ...");

                    this.m_stopDir.BeginLoop();
                    this.m_connectionDir.EnableControls(false);
#endif


                List<ZConnection> connections = new List<ZConnection>();

                nRet = PrepareSearchOneDir(node,
                    connection.QueryXml,
                    ref connections,
                    out strError);
                if (nRet == -1 || this.m_stops.Count == 0)
                {
#if NOOOOOOOOOOOOOO
                    lock (this)
                    {
                        this.m_connectionDir.EnableControls(true);
                        this.m_stopDir.EndLoop();
                        this.m_stopDir.OnStop -= new StopEventHandler(m_stopDir_OnStop);
                        this.m_stopDir.Initial("");

                        // ���ȫ���¼�
                        for (int i = 0; i < this.m_stops.Count; i++)
                        {
                            Stop stop = this.m_stops[i];

                            stop.OnBeginLoop -= new BeginLoopEventHandler(Stop_OnBeginLoop);
                            stop.OnEndLoop -= new EndLoopEventHandler(Stop_OnEndLoop);
                        }

                        this.m_stops.Clear();
                    }
#endif
                    goto ERROR1;
                }

                this.m_nServerCount = this.m_stops.Count;

                // ��������
                for (int i = 0; i < connections.Count; i++)
                {
                    ZConnection temp = connections[i];
#if THREAD_POOLING
                    List<string> commands = new List<string>();
                    commands.Add("search");
                    commands.Add("present");

                    temp.SetSearchParameters(
            temp.QueryString,
            temp.TargetInfo.DefaultQueryTermEncoding,
            temp.TargetInfo.DbNames,
            temp.TargetInfo.DefaultResultSetName);

                    temp.SetPresentParameters(
            temp.TargetInfo.DefaultResultSetName,
            0, // nStart,
            temp.TargetInfo.PresentPerBatchCount, // nCount,
            temp.TargetInfo.PresentPerBatchCount,   // �Ƽ���ÿ������
            temp.DefaultElementSetName,    // "F" strElementSetName,
            temp.PreferredRecordSyntax,
            true);

                    temp.BeginCommands(commands);

#else
                    temp.Search();
#endif
                }
            }


            return 0;
        ERROR1:
            // ����ı������ڽ����̵߳ı���
            try // ��ֹ����˳�ʱ����
            {
                MessageBox.Show(this, strError);
                this.queryControl1.Focus();
            }
            catch
            {
            }
            return -1;
        }

        // ����һ��������
        // ��װ������ⲿ���õİ汾 -- ������ǰĿ������ѡ���ķ������ڵ�
        public int DoSearchOneServer()
        {
            string strError = "";
            int nRet = 0;

            TreeNode nodeServerOrDatabase = this.zTargetControl1.SelectedNode;
            if (ZTargetControl.IsServerType(nodeServerOrDatabase) == false
                && ZTargetControl.IsDatabaseType(nodeServerOrDatabase) == false)
            {
                strError = "��ǰѡ��Ľڵ㲻�Ƿ��������� �� ���ݿ�����";
                goto ERROR1;
            }

            // return:
            //      -2  ��δ���������
            //      -1  һ�����
            //      0   �ɹ���������
            nRet = DoSearchOneServer(nodeServerOrDatabase, 
                out strError);
            if (nRet == -1 || nRet == -2)
                goto ERROR1;

            return 0;
        ERROR1:
            // ����ı������ڽ����̵߳ı���
            try // ��ֹ����˳�ʱ����
            {
                MessageBox.Show(this, strError);
                this.queryControl1.Focus();
            }
            catch
            {
            }
            return -1;
        }

        // ׼������һ��������
        // ������������
        // thread:
        //      �����߳�
        // return:
        //      -2  ��δ���������
        //      -1  һ�����
        //      0   �ɹ�׼������
        public int PrepareSearchOneServer(
            TreeNode nodeServerOrDatabase,
            string strQueryXml,
            ref List<ZConnection> connections,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            ZConnection connection = this.GetZConnection(nodeServerOrDatabase);
            Debug.Assert(connection.TargetInfo != null, "");

            Debug.Assert(connection.TreeNode == nodeServerOrDatabase, "");

            string strQueryString = "";

            IsbnConvertInfo isbnconvertinfo = new IsbnConvertInfo();
            isbnconvertinfo.IsbnSplitter = this.m_mainForm.IsbnSplitter;
            isbnconvertinfo.ConvertStyle =
                (connection.TargetInfo.IsbnAddHyphen == true ? "addhyphen," : "")
                + (connection.TargetInfo.IsbnRemoveHyphen == true ? "removehyphen," : "")
                + (connection.TargetInfo.IsbnForce10 == true ? "force10," : "")
                + (connection.TargetInfo.IsbnForce13 == true ? "force13," : "")
                + (connection.TargetInfo.IsbnWild == true ? "wild," : "");


            nRet = ZQueryControl.GetQueryString(
                this.m_mainForm.Froms,
                strQueryXml,
                isbnconvertinfo,
                out strQueryString,
                out strError);
            if (nRet == -1)
                return -1;

            connection.QueryString = strQueryString;
            connection.QueryXml = strQueryXml;


            if (strQueryString == "")
            {
                strError = "��δ���������";
                return -2;
            }


            // this.m_nServerCount++;  // �ۼӷ�������
            this.m_stops.Add(connection.Stop);

            connection.Stop.OnBeginLoop -= new BeginLoopEventHandler(Stop_OnBeginLoop);
            connection.Stop.OnEndLoop -= new EndLoopEventHandler(Stop_OnEndLoop);

            connection.Stop.OnBeginLoop += new BeginLoopEventHandler(Stop_OnBeginLoop);
            connection.Stop.OnEndLoop += new EndLoopEventHandler(Stop_OnEndLoop);

            connections.Add(connection);

            return 0;
        }

        // ����һ��������
        // ���������Ժ���ƾ���������
        // thread:
        //      �����߳�
        // return:
        //      -2  ��δ���������
        //      -1  һ�����
        //      0   �ɹ���������
        public int DoSearchOneServer(
            // bool bInDirSearch,
            TreeNode nodeServerOrDatabase,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            ZConnection connection = null;

            try
            {
                connection = this.GetZConnection(nodeServerOrDatabase);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            Debug.Assert(connection.TargetInfo != null, "");

            if (connection.TargetInfo.DbNames == null
                || connection.TargetInfo.DbNames.Length == 0)
            {
                strError = "�������ڵ� '" + nodeServerOrDatabase.Text + "' �µ� " + nodeServerOrDatabase.Nodes.Count.ToString() + "  �����ݿ�ڵ�ȫ��Ϊ '��ȫѡʱ���������' ���ԣ�����ͨ��ѡ���÷������ڵ��޷�ֱ�ӽ��м�����ֻ��ͨ��ѡ�����µ�ĳ�����ݿ�ڵ���м���";
                return -1;
            }

            connection.Searching = 0;

            Debug.Assert(connection.TreeNode == nodeServerOrDatabase, "");

            string strQueryString = "";
            if (nodeServerOrDatabase == this.zTargetControl1.SelectedNode)
            {

                connection.QueryXml = this.queryControl1.GetContent(true);

                connection.TargetInfo.PreferredRecordSyntax = this.comboBox_recordSyntax.Text;
                connection.TargetInfo.DefaultElementSetName = this.comboBox_elementSetName.Text; 

                // this.ClearResultInfo(connection);
            }
            else
            {
                // strQueryString = connection.QueryString;

            }
            IsbnConvertInfo isbnconvertinfo = new IsbnConvertInfo();
            isbnconvertinfo.IsbnSplitter = this.m_mainForm.IsbnSplitter;
            isbnconvertinfo.ConvertStyle =
                (connection.TargetInfo.IsbnAddHyphen == true ? "addhyphen," : "")
                + (connection.TargetInfo.IsbnRemoveHyphen == true ? "removehyphen," : "")
                + (connection.TargetInfo.IsbnForce10 == true ? "force10," : "")
                + (connection.TargetInfo.IsbnForce13 == true ? "force13," : "")
                + (connection.TargetInfo.IsbnWild == true ? "wild," : "");


                nRet = ZQueryControl.GetQueryString(
                    this.m_mainForm.Froms,
                    connection.QueryXml,
                    isbnconvertinfo,
                    out strQueryString,
                    out strError);
                if (nRet == -1)
                    return -1;

                connection.QueryString = strQueryString;

            if (strQueryString == "")
            {
                strError = "��δ���������";
                return -2;
            }

#if THREAD_POOLING
            List<string> commands = new List<string>();
            commands.Add("search");
            commands.Add("present");

            connection.SetSearchParameters(
    connection.QueryString,
    connection.TargetInfo.DefaultQueryTermEncoding,
    connection.TargetInfo.DbNames,
    connection.TargetInfo.DefaultResultSetName);

            connection.SetPresentParameters(
    connection.TargetInfo.DefaultResultSetName,
    0, // nStart,
    connection.TargetInfo.PresentPerBatchCount, // nCount,
    connection.TargetInfo.PresentPerBatchCount,   // �Ƽ���ÿ������
    connection.DefaultElementSetName,    // "F" strElementSetName,
    connection.PreferredRecordSyntax,
    true);

            connection.BeginCommands(commands);
#else
            connection.Search();
#endif
            return 0;
        }

        void Stop_OnBeginLoop(object sender, BeginLoopEventArgs e)
        {
            lock (this)
            {
                // ��һ��OnBegin����
                if (this.m_bStartGroupSearch == false)
                {
                    this.m_stopDir.OnStop += new StopEventHandler(m_stopDir_OnStop);
                    this.m_stopDir.SetMessage("��ʼ���� ...");

                    /*
                    Stop active = this.MainForm.stopManager.ActiveStop;
                    Debug.Assert(this.MainForm.stopManager.IsActive(this.m_stopDir) == true, "");
                     * */

                    this.m_stopDir.BeginLoop();
                    this.m_connectionDir.EnableControls(false);
                }

                this.m_bStartGroupSearch = true;
            }
#if NOOOOOOOOOOOOOOOO
            lock (this)
            {
                this.m_stops.Add((Stop)sender);

                // ��һ��OnBegin����
                if (this.m_nGroupSearchCount == 0)
                {
                    this.m_stopDir.OnStop += new StopEventHandler(m_stopDir_OnStop);
                    this.m_stopDir.SetMessage("��ʼ���� ...");

                    /*
                    Stop active = this.MainForm.stopManager.ActiveStop;
                    Debug.Assert(this.MainForm.stopManager.IsActive(this.m_stopDir) == true, "");
                     * */

                    this.m_stopDir.BeginLoop();
                    this.m_connectionDir.EnableControls(false);
                }


                this.m_nGroupSearchCount++;
                this.m_nServerCount++;

                this.m_stopDir.SetMessage("���ڼ��� (Ŀ���� " + this.m_nGroupSearchCount.ToString() + ") ...");
            }
#endif
        }

        // Ŀ¼����ֹͣ��ť������
        void m_stopDir_OnStop(object sender, StopEventArgs e)
        {
            lock (this)
            {
                /*
                for (int i = 0; i < this.m_stops.Count; i++)
                {
                    Stop stop = this.m_stops[i];
                    if (stop.State == 0)
                        stop.DoStop(false);
                }*/
                StopDirSearchStops(false);
            }
        }

        // ֹͣ���ڽ��е�Ŀ¼����
        // parameters:
        //      bForce  �Ƿ������ǿ����ֹͣ(ǿ��ָclose socket)��==false��ʾ���бȽ��º͵�ֹͣ��Ҳ���Ƕ����Ѿ�����������(socket��������)����Ҫȥclose socket
        void StopDirSearchStops(bool bForce)
        {
            for (int i = 0; i < this.m_stops.Count; i++)
            {
                Stop stop = this.m_stops[i];
                if (bForce == true)
                    stop.DoStop();
                else
                {
                    if (stop.State == 0)
                        stop.DoStop();
                }
            }
        }

        void Stop_OnEndLoop(object sender, EndLoopEventArgs e)
        {
            lock (this)
            {
                this.m_nCompleteServerCount++;
            }

            ZConnection connection = this.ZConnections.FindZConnection((Stop)sender);
            int nResultCount = Math.Max(0, connection.ResultCount);
            this.m_nTotalHitCount += nResultCount;

            this.m_connectionDir.ResultCount = this.m_nTotalHitCount;
            this.m_connectionDir.ShowQueryResultInfo("���н������: " + this.m_nTotalHitCount.ToString()
                + (this.m_nCompleteServerCount == this.m_nServerCount ? "" : "..."));

            this.m_stopDir.SetMessage("���ڼ���������ɼ����ķ������� " + this.m_nCompleteServerCount.ToString() + " (����������������� " + this.m_nServerCount.ToString() + ")...");

            // ���һ��OnEnd����
            if (this.m_nCompleteServerCount == this.m_nServerCount)
            {
                this.m_connectionDir.EnableControls(true);

                this.m_stopDir.EndLoop();
                this.m_stopDir.OnStop -= new StopEventHandler(m_stopDir_OnStop);
                // this.m_stopDir.Initial("");

                // ��β
                this.m_connectionDir = null;
                this.m_stopDir = null;
                this.m_nTotalHitCount = 0;
                this.m_nCompleteServerCount = 0;
                this.m_nServerCount = 0;

                // ���ȫ���¼�
                for (int i = 0; i < this.m_stops.Count; i++)
                {
                    Stop stop = this.m_stops[i];

                    stop.OnBeginLoop -= new BeginLoopEventHandler(Stop_OnBeginLoop);
                    stop.OnEndLoop -= new EndLoopEventHandler(Stop_OnEndLoop);
                }

                this.m_stops.Clear();
            }
        }



#if NOOOOOOOOOOOOOO
        // ���������(�Ѿ���ȡ��ǰ��)�������Ϣ����Ӧ����ʾ
        // thread:
        //      �����߳�
        void ClearResultInfo(ZConnection connection)
        {
            ZConnection current_connection = this.GetCurrentZConnection();

            connection.ResultCount = -2;    // ��ʾ���ڼ���

            if (connection.Records != null)
                connection.Records.Clear();

            // this.listView_browse.Items.Clear();
            if (connection.VirtualItems != null)
            {
                connection.VirtualItems.Clear();

                if (current_connection == connection)
                    LinkRecordsToListView(connection.VirtualItems); // listview�ǹ��õ�
            }

            /*
            if (current_connection == connection)
            {
                this.textBox_resultInfo.Text = "";  // ���textbox�ǹ��õ�
            }
             * */
            if (current_connection == connection)
            {
                ShowQueryResultInfo(connection, "");
            }
           
        }

#endif

        /*
        void SetResultInfo(ZConnection connection)
        {
            this.textBox_resultInfo.Text = "���н������:" + connection.ResultCount.ToString();
        }*/

        void EnableControls(bool bEnable)
        {
            this.zTargetControl1.Enabled = bEnable;
            this.queryControl1.Enabled = bEnable;
            this.listView_browse.Enabled = bEnable;
            this.textBox_resultInfo.Enabled = bEnable;

            this.comboBox_elementSetName.Enabled = bEnable;
            this.comboBox_recordSyntax.Enabled = bEnable;
        }

        // ����Server���ܷ�����Close
        // return:
        //      -1  error
        //      0   ����Close
        //      1   ��Close���Ѿ���ʹZChannel������δ��ʼ��״̬
        int CheckServerCloseRequest(
            ZConnection connection,
            out string strMessage,
            out string strError)
        {
            strMessage = "";
            strError = "";

            if (connection.ZChannel.DataAvailable == false)
                return 0;

            int nRecvLen = 0;
            byte [] baPackage = null;
            int nRet = connection.ZChannel.RecvTcpPackage(
                        out baPackage,
                        out nRecvLen,
                        out strError);
            if (nRet == -1)
                return -1;

            BerTree tree1 = new BerTree();
            int nTotlen = 0;

            tree1.m_RootNode.BuildPartTree(baPackage,
                0,
                baPackage.Length,
                out nTotlen);

            if (tree1.GetAPDuRoot().m_uTag != BerTree.z3950_close)
            {
                // ����Close
                return 0;
            }

            CLOSE_REQUEST closeStruct = new CLOSE_REQUEST();
            nRet = BerTree.GetInfo_closeRequest(
                tree1.GetAPDuRoot(),
                ref closeStruct,
                out strError);
            if (nRet == -1)
                return -1;

            strMessage = closeStruct.m_strDiagnosticInformation;

            /*
            this.ZChannel.CloseSocket();
            this.ZChannel.Initialized = false;  // ��ʹ���³�ʼ��
            if (this.CurrentTargetInfo != null)
                this.CurrentTargetInfo.OfflineServerIcon();
             * */
            connection.CloseConnection();

            return 1;
        }


        // ����ԭʼ��
        // parameters:
        //      strResultInfo   [out]����˵����ʼ�����������
        int DoSendOriginPackage(
            ZConnection connection,
            byte [] baPackage,
            out string strError)
        {
            strError = "";

            TargetInfo targetinfo = connection.TargetInfo;
            /*
            if (connection.ZChannel.Initialized == true)
            {
                strError = "Already Initialized";
                goto ERROR1;
            }*/

            if (connection.ZChannel.Connected == false)
            {
                strError = "socket��δ���ӻ����Ѿ����ر�";
                goto ERROR1;
            }


            byte[] baOutPackage = null;
            int nRecvLen = 0;
            int nRet = connection.ZChannel.SendAndRecv(
                baPackage,
                out baOutPackage,
                out nRecvLen,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }

            return 0;
        ERROR1:
            return -1;
        }

        // parameters:
        //      strResultInfo   [out]����˵����ʼ�����������
        int DoInitial(
            ZConnection connection,
            out string strResultInfo,
            out string strError)
        {
            strResultInfo = "";
            strError = "";

            byte[] baPackage = null;
            BerTree tree = new BerTree();
            INIT_REQUEST struInit_request = new INIT_REQUEST();
            int nRet;
            int nRecvLen;

            TargetInfo targetinfo = connection.TargetInfo;

            if (connection.ZChannel.Initialized == true)
            {
                strError = "Already Initialized";
                goto ERROR1;
            }

            struInit_request.m_strReferenceId = this.CurrentRefID;  //  "0";!!!
            struInit_request.m_strOptions = "yynnnnnnnnnnnnnnnn";   // "yyynynnyynynnnyn";

            struInit_request.m_lPreferredMessageSize = 0x100000; ////16384;
            struInit_request.m_lExceptionalRecordSize = 0x100000;

            if (String.IsNullOrEmpty(targetinfo.UserName) == false)
            {
                struInit_request.m_strID = targetinfo.UserName;
                struInit_request.m_strPassword = targetinfo.Password;
                struInit_request.m_strGroupID = targetinfo.GroupID;
                struInit_request.m_nAuthenticationMethod = targetinfo.AuthenticationMethod;
            }
            else
            {
                struInit_request.m_strID = "";
                struInit_request.m_strPassword = "";
                struInit_request.m_strGroupID = "";
                struInit_request.m_nAuthenticationMethod = -1;
            }

            /*
            struInit_request.m_strImplementationId = "81";    // "81";
            struInit_request.m_strImplementationVersion = "2.0.3 WIN32 Debug";
            struInit_request.m_strImplementationName = "Index Data/YAZ";
             * */

            struInit_request.m_strImplementationId = "DigitalPlatform";
            struInit_request.m_strImplementationVersion = "1.1.0";
            struInit_request.m_strImplementationName = "dp2Catalog";

            if (targetinfo.CharNegoUTF8 == true)
            {
                struInit_request.m_charNego = new CharsetNeogatiation();
                struInit_request.m_charNego.EncodingLevelOID = CharsetNeogatiation.Utf8OID; //  "1.0.10646.1.0.8";   // utf-8
                struInit_request.m_charNego.RecordsInSelectedCharsets = (targetinfo.CharNegoRecordsUTF8 == true ? 1 : 0);
            }

            nRet = tree.InitRequest(struInit_request,
                   targetinfo.DefaultQueryTermEncoding,
                    out baPackage);
            if (nRet == -1)
            {
                strError = "CBERTree::InitRequest() fail!";
                goto ERROR1;
            }

            if (connection.ZChannel.Connected == false)
            {
                strError = "socket��δ���ӻ����Ѿ����ر�";
                goto ERROR1;
            }


#if DUMPTOFILE
	DeleteFile("initrequest.bin");
	DumpPackage("initrequest.bin",
				(char *)baPackage.GetData(),
				baPackage.GetSize());
	DeleteFile ("initrequest.txt");
	tree.m_RootNode.DumpToFile("initrequest.txt");
#endif

            /*
            nRet = this.ZChannel.SendTcpPackage(
                baPackage,
                baPackage.Length,
                out strError);
            if (nRet == -1 || nRet == 1)
            {
                // CloseZAssociation();
                return -1;
            }

            baPackage = null;
            nRet = this.ZChannel.RecvTcpPackage(
                        out baPackage,
                        out nRecvLen,
                        out strError);
            if (nRet == -1)
            {
                // CloseZAssociation();
                return -1;
            }
             * */

            byte[] baOutPackage = null;
            nRet = connection.ZChannel.SendAndRecv(
                baPackage,
                out baOutPackage,
                out nRecvLen,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }



#if DUMPTOFILE
	DeleteFile("initresponse.bin");
	DumpPackage("initresponse.bin",
				(char *)baOutPackage.GetData(),
				baOutPackage.GetSize());
#endif

            ////////////////////////////////////////////////////////////////
            BerTree tree1 = new BerTree();
            int nTotlen = 0;

            tree1.m_RootNode.BuildPartTree(baOutPackage,
                0,
                baOutPackage.Length,
                out nTotlen);


#if DUMPTOFILE
	DeleteFile("InitResponse.txt"); 
	tree1.m_RootNode.DumpDebugInfoToFile("InitResponse.txt");
#endif

            /*
	nRet = FitDebugInfo_InitResponse(&tree1,
							  strError);
	if (nRet == -1) {
		return -1;
	}
	*/


            INIT_RESPONSE init_response = new INIT_RESPONSE();
            nRet = BerTree.GetInfo_InitResponse(tree1.GetAPDuRoot(),
                                 ref init_response,
                                 out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }

            if (targetinfo.IgnoreReferenceID == false)
            {
                // 2007/11/2�����԰������־ɰ汾dp2zserver�Ĵ���
                if (struInit_request.m_strReferenceId != init_response.m_strReferenceId)
                {
                    strError = "����� reference id [" + struInit_request.m_strReferenceId + "] �� ��Ӧ�� reference id [" + init_response.m_strReferenceId + "] ��һ�£�";
                    goto ERROR1;
                }
            }


            if (init_response.m_nResult != 0)
            {
                strError = "Initial OK";
            }
            else
            {
                strError = "Initial���ܾ���\r\n\r\n������ ["
                    + init_response.m_lErrorCode.ToString()
                    + "]\r\n������Ϣ["
                    + init_response.m_strErrorMessage + "]";

                strResultInfo = ZConnection.BuildInitialResultInfo(init_response);
                return -1;
            }

            /*
	this->m_init_strOption = init_response.m_strOptions;
	this->m_init_lPreferredMessageSize = init_response.m_lPreferredMessageSize;
	this->m_init_lExceptionalRecordSize = init_response.m_lExceptionalRecordSize;
	this->m_init_nResult = init_response.m_nResult;
             * */

            connection.ZChannel.Initialized = true;

            // �ַ���Э��
            if (init_response.m_charNego != null
                && BerTree.GetBit(init_response.m_strOptions, 17) == true)
            {
                if (init_response.m_charNego.EncodingLevelOID == CharsetNeogatiation.Utf8OID)
                {
                    // ��ʱ�޸ļ����ʵı��뷽ʽ��
                    // ���ǻ��޷���ӳ��PropertyDialog�ϡ�����ܷ�����
                    targetinfo.DefaultQueryTermEncoding = Encoding.UTF8;
                    targetinfo.Changed = true;

                    if (init_response.m_charNego.RecordsInSelectedCharsets == 1)
                        connection.ForcedRecordsEncoding = Encoding.UTF8;
                }
            }

            strResultInfo = ZConnection.BuildInitialResultInfo(init_response);

            return 0;
        ERROR1:
            strResultInfo = strError;
            return -1;
        }



        // ��������ǰ�������
        int CheckConnect(
            ZConnection connection,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (connection.ZChannel.DataAvailable == true)
            {
                string strMessage = "";
                // ����Server���ܷ�����Close
                // return:
                //      -1  error
                //      0   ����Close
                //      1   ��Close���Ѿ���ʹZChannel������δ��ʼ��״̬
                nRet = CheckServerCloseRequest(
                    connection,
                    out strMessage,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 1)
                {
                    nRet = connection.ZChannel.NewConnectSocket(connection.TargetInfo.HostName,
                        connection.TargetInfo.Port,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    connection.TargetInfo.OnlineServerIcon(true);

                    string strInitialResultInfo = "";
                    nRet = this.DoInitial(
                        connection,
                        // connection.TargetInfo,
                        out strInitialResultInfo,
                        out strError);
                    if (nRet == -1)
                    {
                        connection.TargetInfo.OnlineServerIcon(false);
                        return -1;
                    }

                    // ���õ�ǰ�����Ѿ�ѡ��Ľڵ����չ��Ϣ
                    nRet = ZTargetControl.SetCurrentTargetExtraInfo(
                        this.zTargetControl1.SelectedNode,
                        strInitialResultInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                }
            }

            return 0;
        }

#if NO_USE

        // return:
        //		-1	error
        //		0	fail
        //		1	succeed
        int DoSearch(
            ZConnection connection,
            string strQuery,
            Encoding queryTermEncoding,
            string[] dbnames,
            string strResultSetName,
            out int nResultCount,
            out string strError)
        {
            strError = "";

            BerTree tree = new BerTree();
            SEARCH_REQUEST struSearch_request = new SEARCH_REQUEST();
            byte[] baPackage = null;
            int nRet;
            int nRecvLen;
            //int nMax;
            //int i;



            // -->
            BerTree tree1 = new BerTree();
            int nTotlen = 0;


            nResultCount = 0;

            struSearch_request.m_dbnames = dbnames;

            Debug.Assert(struSearch_request.m_dbnames.Length != 0, "");

            struSearch_request.m_strReferenceId = this.CurrentRefID;
            struSearch_request.m_lSmallSetUpperBound = 0;
            struSearch_request.m_lLargeSetLowerBound = 1;
            struSearch_request.m_lMediumSetPresentNumber = 0;
            struSearch_request.m_nReplaceIndicator = 1;
            struSearch_request.m_strResultSetName = strResultSetName;   // "default";
            struSearch_request.m_strSmallSetElementSetNames = "";
            struSearch_request.m_strMediumSetElementSetNames = "";
            struSearch_request.m_strPreferredRecordSyntax = ZTargetControl.GetLeftValue(this.comboBox_recordSyntax.Text);    //  this.CurrentTargetInfo.PreferredRecordSyntax;   // BerTree.MARC_SYNTAX;
            struSearch_request.m_strQuery = strQuery;
            struSearch_request.m_nQuery_type = 1;
            struSearch_request.m_queryTermEncoding = queryTermEncoding;
           

            // m_search_response.m_lErrorCode = 0;

            nRet = tree.SearchRequest(struSearch_request,
                out baPackage);

            if (nRet == -1)
            {
                strError = "CBERTree::SearchRequest() fail!";
                return -1;
            }
#if NOTCPIP
	if (m_hSocket == INVALID_SOCKET) {
		strError = "socket�Ѿ��ر�!";
		return -1;
	}
#endif


            #if DUMPTOFILE
            string strBinFile = this.MainForm.DataDir + "\\searchrequest.bin";
            File.Delete(strBinFile);
            DumpPackage(strBinFile,
                baPackage);
            string strLogFile = this.MainForm.DataDir + "\\searchrequest.txt";
            File.Delete(strLogFile);
            tree.m_RootNode.DumpToFile(strLogFile);
            #endif



            nRet = CheckConnect(
                connection,
                out strError);
            if (nRet == -1)
                return -1;

            /*
            nRet = this.ZChannel.SendTcpPackage(
                baPackage,
                baPackage.Length,
                out strError);
            if (nRet == -1 || nRet == 1)
            {
                // CloseZAssociation();
                return -1;
            }
            //AfxMessageBox("���ͳɹ�");


            baPackage = null;
            nRet = this.ZChannel.RecvTcpPackage(
                        out baPackage,
                        out nRecvLen,
                        out strError);
            if (nRet == -1)
            {
                // CloseZAssociation();
                return -1;
            }
             * */

            byte [] baOutPackage = null;
            nRet = connection.ZChannel.SendAndRecv(
                baPackage,
                out baOutPackage,
                out nRecvLen,
                out strError);
            if (nRet == -1)
                return -1;

#if DEBUG
            if (nRet == 0)
            {
                Debug.Assert(strError == "", "");
            }
#endif

#if DUPMTOFILE
	DeleteFile("searchresponse.bin");
	DumpPackage("searchresponse.bin",
				(char *)baOutPackage.GetData(),
				baOutPackage.GetSize());
#endif

            tree1.m_RootNode.BuildPartTree(baOutPackage,
                0,
                baOutPackage.Length,
                out nTotlen);

            SEARCH_RESPONSE search_response = new SEARCH_RESPONSE();
            nRet = BerTree.GetInfo_SearchResponse(tree1.GetAPDuRoot(),
                                   ref search_response,
                                   true,
                                   out strError);
            if (nRet == -1)
                return -1;

#if DUMPTOFILE
	DeleteFile("SearchResponse.txt"); 
	tree1.m_RootNode.DumpDebugInfoToFile("SearchResponse.txt");
#endif
            /*
	nRet = FitDebugInfo_SearchResponse(&tree1,
							  strError);
	if (nRet == -1) {
		AfxMessageBox(strError);
		return;
	}
	*/
            nResultCount = (int)search_response.m_lResultCount;

            if (search_response.m_nSearchStatus != 0)	// ��һ����1
                return 1;

            strError = "Search Fail: diagRecords:\r\n" + search_response.m_diagRecords.GetMessage();
            return 0;	// search fail
        }



        // ��ü�¼
        // ȷ��һ�����Ի��nCount��
        int DoPresent(
            ZConnection connection,
            string strResultSetName,
            int nStart,
            int nCount,
            string strElementSetName,
            string strPreferredRecordSyntax,
            out RecordCollection records,
            out string strError)
        {
            records = new RecordCollection();
            if (nCount == 0)
            {
                strError = "nCountΪ0";
                return 0;
            }

            int nGeted = 0;
            for (; ; )
            {
                RecordCollection temprecords = null;
                int nRet = DoOncePresent(
                    connection,
                    strResultSetName,
                    nStart + nGeted,
                    nCount - nGeted,
                    strElementSetName,
                    strPreferredRecordSyntax,
                    out temprecords,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (temprecords == null)
                    break;

                nGeted += temprecords.Count;
                if (temprecords.Count > 0)
                    records.AddRange(temprecords);

                if (nGeted >= nCount || temprecords.Count == 0)
                    break;
            }

            return 0;
        }

        // ��ü�¼
        // ��ȷ��һ�����Ի��nCount��
        // parameters:
        //		nStart	��ʼ��¼(��0����)
        int DoOncePresent(
            ZConnection connection,
            string strResultSetName,
            int nStart,
            int nCount,
            string strElementSetName,
            string strPreferredRecordSyntax,
            out RecordCollection records,
            out string strError)
        {
            records = null;
            strError = "";

            if (nCount == 0)
            {
                strError = "nCountΪ0";
                return 0;
            }


            BerTree tree = new BerTree();
            PRESENT_REQUEST struPresent_request = new PRESENT_REQUEST();
            byte[] baPackage = null;
            int nRet;
            int nRecvLen;

            // -->
            BerTree tree1 = new BerTree();
            int nTotlen = 0;

            struPresent_request.m_strReferenceId = this.CurrentRefID;
            struPresent_request.m_strResultSetName = strResultSetName; // "default";
            struPresent_request.m_lResultSetStartPoint = nStart + 1;
            struPresent_request.m_lNumberOfRecordsRequested = nCount;
            struPresent_request.m_strElementSetNames = strElementSetName;
            struPresent_request.m_strPreferredRecordSyntax = strPreferredRecordSyntax;

            nRet = tree.PresentRequest(struPresent_request,
                                     out baPackage);
            if (nRet == -1)
            {
                strError = "CBERTree::PresentRequest() fail!";
                return -1;
            }


#if DUMPTOFILE
	DeleteFile("presentrequest.bin");
	DumpPackage("presentrequest.bin",
		(char *)baPackage.GetData(),
		baPackage.GetSize());
	DeleteFile ("presentrequest.txt");
	tree.m_RootNode.DumpToFile("presentrequest.txt");
#endif

            nRet = CheckConnect(
                connection,
                out strError);
            if (nRet == -1)
                return -1;

            /*
            nRet = this.ZChannel.SendTcpPackage(
                baPackage,
                baPackage.Length,
                out strError);
            if (nRet == -1 || nRet == 1)
            {
                // CloseZAssociation();
                return -1;
            }

            //////////////


            baPackage = null;
            nRet = this.ZChannel.RecvTcpPackage(
                        out baPackage,
                        out nRecvLen,
                        out strError);
            if (nRet == -1)
            {
                // CloseZAssociation();
                goto ERROR1;
            }
             * */

            byte [] baOutPackage = null;
            nRet = connection.ZChannel.SendAndRecv(
                baPackage,
                out baOutPackage,
                out nRecvLen,
                out strError);
            if (nRet == -1)
                return -1;

#if DUMPTOFILE	
	DeleteFile("presendresponse.bin");
	DumpPackage("presentresponse.bin",
				(char *)baPackage.GetData(),
				baPackage.GetSize());
#endif


            tree1.m_RootNode.BuildPartTree(baOutPackage,
                0,
                baOutPackage.Length,
                out nTotlen);

#if DUMPTOFILE
	DeleteFile("PresentResponse.txt"); 
	tree1.m_RootNode.DumpDebugInfoToFile("PresentResponse.txt");
#endif

            SEARCH_RESPONSE search_response = new SEARCH_RESPONSE();
            nRet = BerTree.GetInfo_PresentResponse(tree1.GetAPDuRoot(),
                                   ref search_response,
                                   out records,
                                   true,
                                   out strError);
            if (nRet == -1)
                goto ERROR1;

            /*
            nRet = FitDebugInfo_PresentResponse(&tree1,
                                      strError);
            if (nRet == -1) {
                goto ERROR1;
            }

            DeleteFile("PresentResponse.txt"); 
            tree1.m_RootNode.DumpDebugInfoToFile("PresentResponse.txt");
            */


            if (search_response.m_diagRecords.Count != 0)
            {
                /*
                string strDiagText;
                string strAddInfo;

                nRet = GetDiagTextByNumber("bib1diag.txt",
                                m_search_response.m_nDiagCondition,
                                strDiagText,
                                strAddInfo,
                                strError);
                if (nRet == -1) {
                    if (this->m_bAllowMessageBox)
                        AfxMessageBox(strError);
                    return -1;
                }
                if (strDiagText.GetLength())
                    strError = strDiagText;
                else
                    strError.Format("diag condition[%d] diag set id[%s]",
                    m_search_response.m_nDiagCondition,
                    m_search_response.m_strDiagSetID);
                 * */
                strError = "error diagRecords:\r\n\r\n---\r\n" + search_response.m_diagRecords.GetMessage();
                return -1;
            }

            return 0;
        ERROR1:
            return -1;
        }

#endif


        void DumpPackage(string strFileName,
            byte[] baPackage)
        {
            Stream stream = File.Create(strFileName);

            stream.Write(baPackage, 0, baPackage.Length);

            stream.Close();
        }

        #region IZSearchForm �ӿ�ʵ��

        public delegate void Delegate_EnableQueryControl(
            ZConnection connection,
            bool bEnable);

        // ����connection�Ƿ�Ϊ��ǰconnection�������Ƿ�ִ��
        // Enable/Disable����ʽ�ؼ��Ĳ���
        void __EnableQueryControl(
            ZConnection connection,
            bool bEnable)
        {
            ZConnection cur_connection = this.GetCurrentZConnection();
            if (cur_connection == connection)
            {
                EnableQueryControl(bEnable);
            }
        }

        public void EnableQueryControl(
    ZConnection connection,
    bool bEnable)
        {
            object[] pList = { connection, bEnable };
            this.Invoke(
                new ZSearchForm.Delegate_EnableQueryControl(__EnableQueryControl), pList);
        }

        void EnableQueryControl(bool bEnable)
        {
            this.queryControl1.Enabled = bEnable;
            this.comboBox_elementSetName.Enabled = bEnable;
            this.comboBox_recordSyntax.Enabled = bEnable;
        }

        public delegate bool Delegate_DisplayBrowseItems(ZConnection connection,
            bool bTriggerSelChanged);

        // ��ʾ��ǰ�»�õ������¼
        // �������Զ��жϣ�ֻ�е�ǰZConnection�Ż���ʾ����
        bool __DisplayBrowseItems(ZConnection connection,
            bool bTriggerSelChanged = false)
        {
            bool bRet = false;  // û�б���ʾ
            if (connection == this.GetCurrentZConnection())
            {
                LinkRecordsToListView(connection.VirtualItems);
                bRet = true;    // ����ʾ��
            }

            if (bTriggerSelChanged == true)
            {
                listView_browse_SelectedIndexChanged(null, null);
            }

            return bRet;   
        }

        public bool DisplayBrowseItems(ZConnection connection,
            bool bTriggerSelChanged = false)
        {
            object[] pList = { connection, bTriggerSelChanged };
            return (bool)this.Invoke(
                new ZSearchForm.Delegate_DisplayBrowseItems(__DisplayBrowseItems), pList);
        }

        public delegate bool Delegate_ShowMessageBox(ZConnection connection,
            string strText);

        // ��ʾMessageBox()
        // �������Զ��жϣ�ֻ�е�ǰZConnection�Ż���ʾ����
        bool __ShowMessageBox(ZConnection connection,
            string strText)
        {
            if (connection == this.GetCurrentZConnection())
            {
                MessageBox.Show(this, strText);
                return true;    // ����ʾ��
            }

            return false;   // û�б���ʾ
        }

        public bool ShowMessageBox(ZConnection connection,
           string strText)
        {
            object[] pList = { connection, strText };
            if (this.IsDisposed == true)
                return false; // ��ֹ���ڹرպ������׳��쳣 2014/5/15
            return (bool)this.Invoke(
                new ZSearchForm.Delegate_ShowMessageBox(__ShowMessageBox), pList);
        }

        public delegate bool Delegate_ShowQueryResultInfo(ZConnection connection,
            string strText);

        // ��ʾ��ѯ�����Ϣ���ڼ���ʽ���²�textbox��
        // �������Զ��жϣ�ֻ�е�ǰZConnection�Ż���ʾ����
        bool __ShowQueryResultInfo(ZConnection connection,
            string strText)
        {
                // �޸�treenode�ڵ��ϵ���������ʾ
                ZTargetControl.SetNodeResultCount(connection.TreeNode,
                    connection.ResultCount);


            if (connection == this.GetCurrentZConnection())
            {

                this.textBox_resultInfo.Text = strText;
                return true;    // ����ʾ��
            }


            return false;   // û�б���ʾ
        }

        public bool ShowQueryResultInfo(ZConnection connection,
           string strText)
        {
            if (this.IsDisposed == true)
                return false;

            object[] pList = { connection, strText };
            return (bool)this.Invoke(
                new ZSearchForm.Delegate_ShowQueryResultInfo(__ShowQueryResultInfo), pList);

        }

        // ���ݲ�ͬ��ʽ�Զ����������ʽ
        public int BuildBrowseText(
            ZConnection connection,
            DigitalPlatform.Z3950.Record record,
            string strStyle,
            out string strBrowseText,
            out int nImageIndex,
            out string strError)
        {
            strBrowseText = "";
            strError = "";
            int nRet = 0;

            nImageIndex = BROWSE_TYPE_NORMAL;

            if (record.m_nDiagCondition != 0)
            {
                strBrowseText = "��ϼ�¼ condition=" + record.m_nDiagCondition.ToString() + "; addinfo=\"" + record.m_strAddInfo + "\"; diagSetOID=" + record.m_strDiagSetID;
                nImageIndex = BROWSE_TYPE_DIAG;
                return 0;
            }

            string strElementSetName = record.m_strElementSetName;

            if (strElementSetName == "B")
                nImageIndex = BROWSE_TYPE_BRIEF;
            else if (strElementSetName == "F")
                nImageIndex = BROWSE_TYPE_FULL;

            Encoding currrentEncoding = connection.GetRecordsEncoding(
                this.m_mainForm,
                record.m_strSyntaxOID);

            string strSytaxOID = record.m_strSyntaxOID;
            string strData = currrentEncoding.GetString(record.m_baRecord);

            // string strOutFormat = "";
            string strMARC = "";    // �ݴ�MARC���ڸ�ʽ����

            // ���ΪXML��ʽ
            if (record.m_strSyntaxOID == "1.2.840.10003.5.109.10")
            {
                // ���ƫ��MARC
                if (StringUtil.IsInList("marc", strStyle) == true)
                {
                    // �����ڵ�����ֿռ䣬�������MARCXML, ����ת��ΪUSMARC�����򣬾�ֱ�Ӹ������ֿռ�����ʽ�����ת��
                    string strNameSpaceUri = "";
                    nRet = GetRootNamespace(strData,
                        out strNameSpaceUri,
                        out strError);
                    if (nRet == -1)
                    {
                        // ȡ���ڵ�����ֿռ�ʱ����
                        return -1;
                    }

                    if (strNameSpaceUri == Ns.usmarcxml)
                    {
                        string strOutMarcSyntax = "";

                        // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
                        // parameters:
                        //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
                        //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
                        //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
                        nRet = MarcUtil.Xml2Marc(strData,
                            true,
                            "usmarc",
                            out strOutMarcSyntax,
                            out strMARC,
                            out strError);
                        if (nRet == -1)
                        {
                            // XMLת��ΪMARCʱ����
                            return -1;
                        }

                        // strOutFormat = "marc";
                        strSytaxOID = "1.2.840.10003.5.10";
                        goto DO_BROWSE;
                    }

                }

                // ����MARCXML��ʽ
                // strOutFormat = "xml";
                goto DO_BROWSE;
            }

            // SUTRS
            if (record.m_strSyntaxOID == "1.2.840.10003.5.101")
            {
                // strOutFormat = "sutrs";
                goto DO_BROWSE;
            }

            if (record.m_strSyntaxOID == "1.2.840.10003.5.1"    // unimarc
                || record.m_strSyntaxOID == "1.2.840.10003.5.10")  // usmarc
            {
                // ISO2709ת��Ϊ���ڸ�ʽ
                nRet = Marc8Encoding.ConvertByteArrayToMarcRecord(
                    record.m_baRecord,
                    connection.GetRecordsEncoding(this.m_mainForm, record.m_strSyntaxOID),  // Encoding.GetEncoding(936),
                    true,
                    out strMARC,
                    out strError);
                if (nRet < 0)
                {
                    return -1;
                }

                // �����Ҫ�Զ�̽��MARC��¼�����ĸ�ʽ��
                if (connection.TargetInfo.DetectMarcSyntax == true)
                {
                    // return:
                    //		-1	�޷�̽��
                    //		1	UNIMARC	���򣺰���200�ֶ�
                    //		10	USMARC	���򣺰���008�ֶ�(innopac��UNIMARC��ʽҲ��һ����ֵ�008)
                    nRet = ZSearchForm.DetectMARCSyntax(strMARC);
                    if (nRet == 1)
                        strSytaxOID = "1.2.840.10003.5.1";
                    else if (nRet == 10)
                        strSytaxOID = "1.2.840.10003.5.10";

                    // ���Զ�ʶ��Ľ����������
                    record.AutoDetectedSyntaxOID = strSytaxOID;
                }

                // strOutFormat = "marc";
                goto DO_BROWSE;
            }

            // ����ʶ��ĸ�ʽ��ԭ������
            strBrowseText = strData;
            return 0;

        DO_BROWSE:

            if (strSytaxOID == "1.2.840.10003.5.1"    // unimarc
                || strSytaxOID == "1.2.840.10003.5.10")  // usmarc
            {
                return BuildMarcBrowseText(
                strSytaxOID,
                strMARC,
                out strBrowseText,
                out strError);
            }

            // XML����ʱû��ת���취
            strBrowseText = strData;
            return 0;
        }



        // ����MARC��ʽ��¼�������ʽ
        // paramters:
        //      strMARC MARC���ڸ�ʽ
        public int BuildMarcBrowseText(
            string strSytaxOID,
            string strMARC,
            out string strBrowseText,
            out string strError)
        {
            strBrowseText = "";
            strError = "";

            FilterHost host = new FilterHost();
            host.ID = "";
            host.MainForm = this.m_mainForm;

            BrowseFilterDocument filter = null;

            string strFilterFileName = this.m_mainForm.DataDir + "\\" + strSytaxOID.Replace(".", "_") + "\\marc_browse.fltx";

            int nRet = this.PrepareMarcFilter(
                host,
                strFilterFileName,
                out filter,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            try
            {
                nRet = filter.DoRecord(null,
        strMARC,
        0,
        out strError);
                if (nRet == -1)
                    goto ERROR1;

                strBrowseText = host.ResultString;

            }
            finally
            {
                // �黹����
                this.Filters.SetFilter(strFilterFileName, filter);
            }

            return 0;
        ERROR1:
            return -1;
        }

        #endregion

#if NOOOOOOOOOOOOOOOOO
        // �����׷�ӵ�listview��
        // parameters:
        //      records ��ǰ�»��һ����¼����Ҫ׷�ӵ�connection��Records��
        int FillRecordsToBrowseView(
            Stop stop,
            ZConnection connection,
            RecordCollection records,
            out string strError)
        {

            Debug.Assert(connection == this.GetCurrentZConnection(), "���ǵ�ǰconnection��װ��listview���ƻ�����");

            strError = "";
            if (connection.Records == null)
                connection.Records = new RecordCollection();

            int nExistCount = connection.Records.Count;
            Debug.Assert(this.listView_browse.Items.Count == nExistCount, "");

            // �����µ�һ��
            connection.Records.AddRange(records);

            int nRet = FillRecordsToBrowseView(
                stop,
                connection,
                connection.Records,
                nExistCount,
                records.Count,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }
#endif

        public int PrepareMarcFilter(
    FilterHost host,
    string strFilterFileName,
    out BrowseFilterDocument filter,
    out string strError)
        {
            strError = "";

            // �����Ƿ����ֳɿ��õĶ���
            filter = (BrowseFilterDocument)this.Filters.GetFilter(strFilterFileName);

            if (filter != null)
            {
                filter.FilterHost = host;
                return 1;
            }

            // �´���
            // string strFilterFileContent = "";

            filter = new BrowseFilterDocument();

            filter.FilterHost = host;
            filter.strOtherDef = "FilterHost Host = null;";

            filter.strPreInitial = " BrowseFilterDocument doc = (BrowseFilterDocument)this.Document;\r\n";
            filter.strPreInitial += " Host = ("
                + "FilterHost" + ")doc.FilterHost;\r\n";

            // filter.Load(strFilterFileName);

            try
            {
                filter.Load(strFilterFileName);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            string strCode = "";    // c#����

            int nRet = filter.BuildScriptFile(out strCode,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string[] saAddRef1 = {
										 this.BinDir + "\\digitalplatform.marcdom.dll",
										 // this.BinDir + "\\digitalplatform.marckernel.dll",
										 // this.BinDir + "\\digitalplatform.libraryserver.dll",
										 this.BinDir + "\\digitalplatform.dll",
										 this.BinDir + "\\digitalplatform.Text.dll",
										 this.BinDir + "\\digitalplatform.IO.dll",
										 this.BinDir + "\\digitalplatform.Xml.dll",
										 this.BinDir + "\\dp2catalog.exe" };

            Assembly assembly = null;
            string strWarning = "";
            string strLibPaths = "";

            string[] saRef2 = filter.GetRefs();

            string[] saRef = new string[saRef2.Length + saAddRef1.Length];
            Array.Copy(saRef2, saRef, saRef2.Length);
            Array.Copy(saAddRef1, 0, saRef, saRef2.Length, saAddRef1.Length);

            // ����Script��Assembly
            nRet = ScriptManager.CreateAssembly_1(strCode,
                saRef,
                strLibPaths,
                out assembly,
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
                // MessageBox.Show(this, strWarning);
            }

            filter.Assembly = assembly;

            return 0;
        ERROR1:
            return -1;
        }



        public void GetAllRecords()
        {
            ZConnection connection = this.GetCurrentZConnection();

            GetNextAllBatch(connection);
        }

        public void GetNextAllBatch(ZConnection connection)
        {
            if (/*this.listView_browse.Items.Count*/
                connection.Records.Count
                >= connection.ResultCount)
            {
                MessageBox.Show(this, "�Ѿ������ȫ�����м�¼");
                return;
            }


            /*
            while(
                connection.Records.Count < connection.ResultCount )
            {
                connection.NextBatch(true);
            }
             * */
            connection.NextAllBatch(true);
        }

        // �����һ�������������
        // ��������ƾ���������
        // thread:
        //      �����߳�
        // return:
        //      -1  error
        //      0   �߳��Ѿ�����������û�е�������
        //      1   �߳��Ѿ�����
        public int NextBatch()
        {
            ZConnection connection = this.GetCurrentZConnection();

            if (connection.Records.Count >= connection.ResultCount)
            {
                MessageBox.Show(this, "�Ѿ�����˽������ȫ����¼");
                return 1;
            }

            return connection.NextBatch(false);
        }

#if NO_USE
        public int NextBatch()
        {
            ZConnection connection = this.GetCurrentZConnection();
            return NextBatch(connection);
        }

        public int NextBatch(ZConnection connection)
        {
            string strError = "";
            int nRet = 0;

            // ��װ��һ����¼
            int nCount = Math.Min(connection.TargetInfo.PresentPerBatchCount,
                connection.ResultCount - this.listView_browse.Items.Count);

            if (nCount <= 0)
            {
                // û�б�Ҫô
                strError = "���н���Ѿ�ȫ����ȡ��ϡ�";
                goto ERROR1;
            }

            // ZConnection connection = this.GetCurrentZConnection();

            connection.Stop.OnStop += new StopEventHandler(this.DoStop);
            connection.Stop.SetMessage("�ӷ�����װ���¼ ...");
            connection.Stop.BeginLoop();

            EnableControls(false);

            this.Update();
            this.MainForm.Update();

            try
            {

                string strElementSetName = ZTargetControl.GetLeftValue(this.comboBox_elementSetName.Text);  // this.CurrentTargetInfo.DefaultElementSetName;

                if (strElementSetName == "B"
                    && connection.TargetInfo.FirstFull == true)
                    strElementSetName = "F";

                RecordCollection records = null;

                nRet = DoPresent(
                    connection,
                    connection.TargetInfo.DefaultResultSetName,
                    this.listView_browse.Items.Count, // nStart,
                    nCount, // nCount,
                    strElementSetName,    // "F" strElementSetName,
                    ZTargetControl.GetLeftValue(this.comboBox_recordSyntax.Text), // this.CurrentTargetInfo.PreferredRecordSyntax,
                    out records,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�� " + this.listView_browse.Items.Count.ToString()
                        + " ��ʼװ���µ�һ����¼ʱ����" + strError;
                    goto ERROR1;
                }
                else
                {
                    nRet = FillRecordsToBrowseView(
                        connection.Stop,
                        connection,
                        records,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }
            }
            finally
            {
                connection.Stop.EndLoop();
                connection.Stop.OnStop -= new StopEventHandler(this.DoStop);
                connection.Stop.Initial("");

                EnableControls(true);
            }

            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

#endif

        #region ISearchForm �ӿں���

        // ���󡢴����Ƿ���Ч?
        public bool IsValid()
        {
            if (this.IsDisposed == true)
                return false;

            return true;
        }

        public string CurrentProtocol
        {
            get
            {
                return "Z39.50";
            }
        }

        public string CurrentResultsetPath
        {
            get
            {
                ZConnection connection = this.GetCurrentZConnection();
                if (connection == null)
                    return "";

                return connection.TargetInfo.HostName
                    + ":" + connection.TargetInfo.Port.ToString()
                    + "/" + string.Join(",", connection.TargetInfo.DbNames)
                    + "/default";
            }
        }

        // ˢ��һ��MARC��¼
        // parameters:
        //      strAction   refresh / delete
        // return:
        //      -2  ��֧��
        //      -1  error
        //      0   ��ش����Ѿ����٣�û�б�Ҫˢ��
        //      1   �Ѿ�ˢ��
        //      2   �ڽ������û���ҵ�Ҫˢ�µļ�¼
        public int RefreshOneRecord(
            string strPathParam,
            string strAction,
            out string strError)
        {
            strError = "";

            int nRet = 0;

            if (this.IsDisposed == true)
            {
                strError = "��ص�Z39.50�������Ѿ����٣�û�б�Ҫˢ��";
                return 0;
            }

            int index = -1;
            string strPath = "";
            string strDirection = "";
            nRet = Global.ParsePathParam(strPathParam,
                out index,
                out strPath,
                out strDirection,
                out strError);
            if (nRet == -1)
                return -1;

            if (index == -1)
            {
                strError = "��ʱ��֧��û�� index ���÷�";
                return -1;
            }

            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
            {
                strError = "��ǰZConnectionΪ��";
                return -1;
            }

            if (index >= connection.ResultCount)
            {
                strError = "Խ�������β��";
                return -1;
            }

            if (strAction == "refresh")
            {

                // ��װ��һ����¼
                int nCount = 1;

                Debug.Assert(connection.Stop != null, "");

                connection.Stop.OnStop += new StopEventHandler(this.DoStop);
                connection.Stop.SetMessage("�ӷ�����װ��һ����¼ ...");
                connection.Stop.BeginLoop();

                // EnableControls(false);
                EnableQueryControl(false);

                this.Update();
                this.m_mainForm.Update();

                try
                {
                    string strElementSetName = ZTargetControl.GetLeftValue(this.comboBox_elementSetName.Text);

                    if (strElementSetName == "B"
                        && connection.TargetInfo.FirstFull == true)
                        strElementSetName = "F";

                    RecordCollection records = null;

                    // TODO: ����Ҫ�Ĳ���׷��Ч���������滻һ���Ѿ����ڵ�����
                    nRet = connection.DoPresent(
                        connection.TargetInfo.DefaultResultSetName,
                        index, // nStart,
                        nCount, // nCount,
                        connection.TargetInfo.PresentPerBatchCount, // �Ƽ���ÿ������
                        strElementSetName,    // "F" strElementSetName,
                        connection.PreferredRecordSyntax,  //this.comboBox_recordSyntax.Text),    //this.CurrentTargetInfo.PreferredRecordSyntax,
                        true,   // ������ʾ����
                        out records,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "�� " + index.ToString()
                            + " λ��(��0��ʼ����)װ��һ����¼ʱ����" + strError;
                        return -1;
                    }
                }
                finally
                {
                    connection.Stop.EndLoop();
                    connection.Stop.OnStop -= new StopEventHandler(this.DoStop);
                    connection.Stop.Initial("");

                    // EnableControls(true);
                    EnableQueryControl(true);
                }
            }

            return 1;
        }

#if NOOOOOOOOOOOOOOOOOOOO
        // return:
        //      -1  error
        //      0   û��̽�����
        //      1   ̽������ˣ������strMarcSyntaxOID������
        public static int DetectMarcSyntax(string strOID,
            string strContent,
            out string strMarcSyntaxOID,
            out string strError)
        {
            strError = "";
            strMarcSyntax = "";

            // ����ΪXML��ʽ
            if (strOID == "1.2.840.10003.5.109.10")
            {
                // �����ڵ�����ֿռ䣬�������MARCXML, ����ת��ΪUSMARC�����򣬾�ֱ�Ӹ������ֿռ�����ʽ�����ת��
                string strNameSpaceUri = "";
                nRet = GetRootNamespace(strContent,
                    out strNameSpaceUri,
                    out strError);
                if (nRet == -1)
                {
                    return -1;
                }

                if (strNameSpaceUri == Ns.usmarcxml)
                {
                    strMarcSyntaxOID = "1.2.840.10003.5.109.10";
                    return 1;
                }
            }

            return 0;
        }

#endif

        public int SyncOneRecord(string strPath,
            ref long lVersion,
            ref string strSyntax,
            ref string strMARC,
            out string strError)
        {
            strError = "";
            return 0;
        }

        // ���һ��MARC/XML��¼
        // return:
        //      -1  error
        //      0   suceed
        //      1   Ϊ��ϼ�¼
        public int GetOneRecord(
            string strStyle,
            int nTest,
            string strPathParam,
            string strParameters,   // bool bHilightBrowseLine,
            out string strSavePath,
            out string strMARC,
            out string strXmlFragment,
            out string strOutStyle,
            out byte[] baTimestamp,
            out long lVersion,
            out DigitalPlatform.Z3950.Record record,
            out Encoding currrentEncoding,
            out LoginInfo logininfo,
            out string strError)
        {
            strXmlFragment = "";
            strMARC = "";
            record = null;
            strError = "";
            currrentEncoding = null;
            baTimestamp = null;
            strSavePath = "";
            strOutStyle = "";
            logininfo = new LoginInfo();
            lVersion = 0;

            int nRet = 0;

            int index = -1;
            string strPath = "";
            string strDirection = "";
            nRet = Global.ParsePathParam(strPathParam,
                out index,
                out strPath,
                out strDirection,
                out strError);
            if (nRet == -1)
                return -1;

            if (index == -1)
            {
                strError = "��ʱ��֧��û�� index ���÷�";
                return -1;
            }

            bool bHilightBrowseLine = StringUtil.IsInList("hilight_browse_line", strParameters);
            bool bForceFullElementSet = StringUtil.IsInList("force_full", strParameters);

            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
            {
                strError = "��ǰZConnectionΪ��";
                return -1;
            }

            if (index >= this.listView_browse.Items.Count)
            {
                if (index >= connection.ResultCount)
                {
                    strError = "Խ�������β��";
                    return -1;
                }

                // ��װ��һ����¼
                int nCount = Math.Min(connection.TargetInfo.PresentPerBatchCount,
                    connection.ResultCount - this.listView_browse.Items.Count);

                if (nCount <= 0)
                {
                    strError = "��ʱ������nCountΪ " + nCount.ToString();
                    return -1;
                }

                connection.Stop.OnStop += new StopEventHandler(this.DoStop);
                connection.Stop.SetMessage("�ӷ�����װ���¼ ...");
                connection.Stop.BeginLoop();

                // EnableControls(false);
                EnableQueryControl(false);

                this.Update();
                this.m_mainForm.Update();

                ActivateStopDisplay();  // 2011/9/11

                try
                {
                    string strElementSetName = ZTargetControl.GetLeftValue(this.comboBox_elementSetName.Text);  // this.CurrentTargetInfo.DefaultElementSetName;

                    if (strElementSetName == "B"
                        && connection.TargetInfo.FirstFull == true)
                        strElementSetName = "F";

                    if (bForceFullElementSet == true)
                        strElementSetName = "F";

                    RecordCollection records = null;

                    nRet = connection.DoPresent(
                        connection.TargetInfo.DefaultResultSetName,
                        this.listView_browse.Items.Count, // nStart,
                        nCount, // nCount,
                        connection.TargetInfo.PresentPerBatchCount, // �Ƽ���ÿ������
                        strElementSetName,    // "F" strElementSetName,
                        connection.PreferredRecordSyntax,  //this.comboBox_recordSyntax.Text),    //this.CurrentTargetInfo.PreferredRecordSyntax,
                        true,   // ������ʾ����
                        out records,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "�� " + this.listView_browse.Items.Count.ToString()
                            + " ��ʼװ���µ�һ����¼ʱ����" + strError;
                        return -1;
                    }
                    else
                    {
                        /*
                        nRet = connection.FillRecordsToVirtualItems(
                            connection.Stop,
                            records,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        DisplayBrowseItems(connection);
                         * */
                    }
                }
                finally
                {
                    connection.Stop.EndLoop();
                    connection.Stop.OnStop -= new StopEventHandler(this.DoStop);
                    connection.Stop.Initial("");

                    // EnableControls(true);
                    EnableQueryControl(true);
                }

                if (index >= this.listView_browse.Items.Count)
                {
                    strError = "indexԽ��";
                    return -1;
                }
            }

            if (bHilightBrowseLine == true)
            {
                // �޸�listview�������ѡ��״̬
                for (int i = 0; i < connection.VirtualItems.SelectedIndices.Count; i++)
                {
                    int temp_index = connection.VirtualItems.SelectedIndices[i];
                    if (temp_index != index)
                    {
                        if (this.listView_browse.Items[temp_index].Selected != false)
                            this.listView_browse.Items[temp_index].Selected = false;
                    }
                }

                ListViewItem curListViewItem = this.listView_browse.Items[index];
                if (curListViewItem.Selected != true)
                    curListViewItem.Selected = true;
                curListViewItem.EnsureVisible();
            }

            // 
            // strSavePath = (index+1).ToString();

            // 
            record = (DigitalPlatform.Z3950.Record)
                connection.VirtualItems[index].Tag; //  curListViewItem.Tag;

            if (record == null)
            {
                strError = "VirtualItem TagΪ��";
                return -1;
            }

            if (record.m_nDiagCondition != 0)
            {
                strError = "����һ����ϼ�¼";
                strOutStyle = "marc";
                strMARC = "012345678901234567890123001����һ����ϼ�¼";
                return 1;
            }


            {
                Debug.Assert(string.IsNullOrEmpty(record.m_strElementSetName) == false, "");

                string strCurrentElementSetName = record.m_strElementSetName;
                string strElementSetName = strCurrentElementSetName;

                if (strCurrentElementSetName == "B"
                    && connection.TargetInfo.FirstFull == true)
                    strElementSetName = "F";

                if (bForceFullElementSet == true)
                    strElementSetName = "F";

                if (strCurrentElementSetName != strElementSetName)
                {
                    connection.Stop.OnStop += new StopEventHandler(this.DoStop);
                    connection.Stop.SetMessage("�ӷ�����װ���¼ ...");
                    connection.Stop.BeginLoop();

                    EnableQueryControl(false);
                    try
                    {

                        RecordCollection records = null;
                        nRet = connection.DoPresent(
            connection.TargetInfo.DefaultResultSetName,
            index, // nStart,
            1, // nCount,
            connection.TargetInfo.PresentPerBatchCount, // �Ƽ���ÿ������
            strElementSetName,    // "F" strElementSetName,
            connection.PreferredRecordSyntax,  //this.comboBox_recordSyntax.Text),    //this.CurrentTargetInfo.PreferredRecordSyntax,
            true,   // ������ʾ����
            out records,
            out strError);
                        if (nRet == -1)
                        {
                            strError = "�� " + index.ToString()
                                + " λ��װ���¼ʱ����" + strError;
                            return -1;
                        }
                        if (records != null && records.Count > 0)
                        {
                            record = records[0];
                        }
                        else
                        {
                            Debug.Assert(false, "");
                        }
                    }
                    finally
                    {
                        connection.Stop.EndLoop();
                        connection.Stop.OnStop -= new StopEventHandler(this.DoStop);
                        connection.Stop.Initial("");

                        // EnableControls(true);
                        EnableQueryControl(true);
                    }
                }

            }

            byte[] baRecord = record.m_baRecord;    // Encoding.ASCII.GetBytes(record.m_strRecord);

            currrentEncoding = connection.GetRecordsEncoding(
                this.m_mainForm,
                record.m_strSyntaxOID);


            // ����ΪXML��ʽ
            if (record.m_strSyntaxOID == "1.2.840.10003.5.109.10")
            {

                // string strContent = Encoding.UTF8.GetString(baRecord);
                string strContent = currrentEncoding.GetString(baRecord);

                if (strStyle == "marc")
                {
                    // �����ڵ�����ֿռ䣬�������MARCXML, ����ת��ΪUSMARC�����򣬾�ֱ�Ӹ������ֿռ�����ʽ�����ת��

                    string strNameSpaceUri = "";
                    nRet = GetRootNamespace(strContent,
                        out strNameSpaceUri,
                        out strError);
                    if (nRet == -1)
                    {
                        return -1;
                    }

                    if (strNameSpaceUri == Ns.usmarcxml)
                    {
                        string strOutMarcSyntax = "";

                        // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
                        // parameters:
                        //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
                        //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
                        //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
                        nRet = MarcUtil.Xml2Marc(strContent,
                            true,
                            "usmarc",
                            out strOutMarcSyntax,
                            out strMARC,
                            out strError);
                        if (nRet == -1)
                        {
                            return -1;
                        }

                        strOutStyle = "marc";
                        // currrentEncoding = connection.GetRecordsEncoding(this.MainForm, "1.2.840.10003.5.10");
                        return 0;
                    }
                }

                // ����MARCXML��ʽ
                // currrentEncoding = connection.GetRecordsEncoding(this.MainForm, record.m_strMarcSyntaxOID);
                strMARC = strContent;
                strOutStyle = "xml";
                return 0;
            }

            // SUTRS
            if (record.m_strSyntaxOID == "1.2.840.10003.5.101")
            {
                string strContent = currrentEncoding.GetString(baRecord);
                if (strStyle == "marc")
                {
                    // TODO: ���ջس�����ת��ΪMARC
                    strMARC = strContent;

                    // strMarcSyntaxOID = "1.2.840.10003.5.10";
                    strOutStyle = "marc";
                    return 0;
                }

                // ����MARCXML��ʽ
                strMARC = strContent;
                strOutStyle = "xml";
                return 0;
            }

            // ISO2709ת��Ϊ���ڸ�ʽ
            nRet = Marc8Encoding.ConvertByteArrayToMarcRecord(
                baRecord,
                connection.GetRecordsEncoding(this.m_mainForm, record.m_strSyntaxOID),  // Encoding.GetEncoding(936),
                true,
                out strMARC,
                out strError);
            if (nRet < 0)
            {
                return -1;
            }

            // �۲�
            // connection.TargetInfo.UnionCatalogBindingDp2ServerUrl
            // ��������а󶨵�dp2serverurl���򿴿���¼����û��901�ֶΣ�
            // ����У�����ΪstrSavePath��baTimestamp
            if (connection.TargetInfo != null
                && String.IsNullOrEmpty(connection.TargetInfo.UnionCatalogBindingDp2ServerName) == false)
            {
                string strLocalPath = "";
                // ��MARC��¼�еõ�901�ֶ������Ϣ
                // return:
                //      -1  error
                //      0   not found field 901
                //      1   found field 901
                nRet = GetField901Info(strMARC,
                    out strLocalPath,
                    out baTimestamp,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    strError = "MARC��¼��δ����901�ֶΣ��޷���ɰ󶨲�����Ҫ�߱�901�ֶΣ���Ϊdp2ZServer��������������ݿ�����addField901='true'���ԡ�Ҫ����˱���Ҳ����Z39.50������������ȥ�����ϱ�Ŀ�󶨶���";
                    return -1;
                }
                strSavePath = "dp2library:" + strLocalPath + "@" + connection.TargetInfo.UnionCatalogBindingDp2ServerName;
                logininfo.UserName = connection.TargetInfo.UserName;
                logininfo.Password = connection.TargetInfo.Password;
            }

            if (connection.TargetInfo != null
    && String.IsNullOrEmpty(connection.TargetInfo.UnionCatalogBindingUcServerUrl) == false)
            {
                string strLocalPath = "";
                // ��MARC��¼�еõ�901�ֶ������Ϣ
                // return:
                //      -1  error
                //      0   not found field 901
                //      1   found field 901
                nRet = GetField901Info(strMARC,
                    out strLocalPath,
                    out baTimestamp,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    strError = "MARC��¼��δ����901�ֶΣ��޷���ɰ󶨲�����Ҫ�߱�901�ֶΣ���Ϊdp2ZServer��������������ݿ�����addField901='true'���ԡ�Ҫ����˱���Ҳ����Z39.50������������ȥ�����ϱ�Ŀ�󶨶���";
                    return -1;
                }
                strSavePath = "unioncatalog:" + strLocalPath + "@" + connection.TargetInfo.UnionCatalogBindingUcServerUrl;
                logininfo.UserName = connection.TargetInfo.UserName;
                logininfo.Password = connection.TargetInfo.Password;
            }


            currrentEncoding = connection.GetRecordsEncoding(this.m_mainForm, record.m_strSyntaxOID);
            strOutStyle = "marc";
            return 0;
        }

        // ��MARC��¼�еõ�901�ֶ������Ϣ
        // parameters:
        //      strPath [out]��¼�ı���·�������磺"����ͼ��/1"
        // return:
        //      -1  error
        //      0   not found field 901
        //      1   found field 901
        public static int GetField901Info(string strMARC,
            out string strPath,
            out byte [] baTimestamp,
            out string strError)
        {
            strPath = "";
            strError = "";
            baTimestamp = null;

            string strField = "";
            string strNextFieldName = "";
            // �Ӽ�¼�еõ�һ���ֶ�
            // parameters:
            //		strMARC		���ڸ�ʽMARC��¼
            //		strFieldName	�ֶ��������������==null����ʾ���ȡ�����ֶ��еĵ�nIndex��
            //		nIndex		ͬ���ֶ��еĵڼ�������0��ʼ����(�����ֶ��У����0�����ʾͷ����)
            //		strField	[out]����ֶΡ������ֶ�������Ҫ���ֶ�ָʾ�����ֶ����ݡ��������ֶν�������
            //					ע��ͷ��������һ���ֶη��أ���ʱstrField�в������ֶ���������һ��ʼ����ͷ��������
            //		strNextFieldName	[out]˳�㷵�����ҵ����ֶ�����һ���ֶε�����
            // return:
            //		-1	����
            //		0	��ָ�����ֶ�û���ҵ�
            //		1	�ҵ����ҵ����ֶη�����strField������
            int nRet = MarcUtil.GetField(strMARC,
                "901",
                0,
                out strField,
                out strNextFieldName);
            if (nRet == -1)
            {
                strError = "GetField 901 error";
                return -1;
            }
            if (nRet == 0)
            {
                strError = "Field 901 not found";
                return 0;
            }

            string strSubfield = "";
            string strNextSubfieldName = "";
            		// ���ֶλ����ֶ����еõ�һ�����ֶ�
		// parameters:
		//		strText		�ֶ����ݣ��������ֶ������ݡ�
		//		textType	��ʾstrText�а��������ֶ����ݻ��������ݡ���ΪItemType.Field����ʾstrText������Ϊ�ֶΣ���ΪItemType.Group����ʾstrText������Ϊ���ֶ��顣
		//		strSubfieldName	���ֶ���������Ϊ1λ�ַ������==null����ʾ�������ֶ�
		//					��ʽΪ'a'�����ġ�
		//		nIndex			��Ҫ���ͬ�����ֶ��еĵڼ�������0��ʼ���㡣
		//		strSubfield		[out]������ֶΡ����ֶ���(1�ַ�)�����ֶ����ݡ�
		//		strNextSubfieldName	[out]��һ�����ֶε����֣�����һ���ַ�
		// return:
		//		-1	����
		//		0	��ָ�������ֶ�û���ҵ�
		//		1	�ҵ����ҵ������ֶη�����strSubfield������
            nRet = MarcUtil.GetSubfield(strField,
                DigitalPlatform.Marc.ItemType.Field,
                "p",
                0,
                out strSubfield,
                out strNextSubfieldName);
            if (nRet == -1)
            {
                strError = "GetSubfield p error";
                return -1;
            }

            if (strSubfield.Length > 1)
                strPath = strSubfield.Substring(1);

            nRet = MarcUtil.GetSubfield(strField,
                DigitalPlatform.Marc.ItemType.Field,
                "t",
                0,
                out strSubfield,
                out strNextSubfieldName);
            if (nRet == -1)
            {
                strError = "GetSubfield t error";
                return -1;
            }


            if (strSubfield.Length > 1)
            {
                string strHexTimestamp = "";
                strHexTimestamp = strSubfield.Substring(1);
                baTimestamp = ByteArray.GetTimeStampByteArray(strHexTimestamp);
            }

            return 1;
        }

        #endregion

        // �������˫��
        private void listView_browse_DoubleClick(object sender, EventArgs e)
        {
            int nIndex = -1;


            // TODO: �����¼ΪSUTRS��ʽ����ֻ��װ��XML�괰��
            // �����¼ΪMARCXML�������ִ��ڶ�����װ����ѡװ��MARC�괰

            if (this.listView_browse.SelectedIndices.Count > 0)
                nIndex = this.listView_browse.SelectedIndices[0];
            else
            {
                if (this.listView_browse.FocusedItem == null)
                    return;
                nIndex = this.listView_browse.Items.IndexOf(this.listView_browse.FocusedItem);
            }

            LoadDetail(nIndex);
        }

        void menuItem_loadMarcDetail_Click(object sender, EventArgs e)
        {
            int index = -1;

            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
            {
                MessageBox.Show(this, "��ǰZConnectionΪ��");
                return;
            }

            if (connection.VirtualItems.SelectedIndices.Count > 0)
                index = connection.VirtualItems.SelectedIndices[0];
            else
            {
                MessageBox.Show(this, "��δѡ��Ҫװ��ļ�¼");
                return;
            }
            

            DigitalPlatform.Z3950.Record record = null;
            if (index < connection.VirtualItems.Count)
            {
                record = (DigitalPlatform.Z3950.Record)
                    connection.VirtualItems[index].Tag;
                Debug.Assert(record != null, "");
            }
            else
            {
                MessageBox.Show(this, "indexԽ��");
                return;
            }

            MarcDetailForm form = new MarcDetailForm();

            form.MdiParent = this.m_mainForm;
            form.MainForm = this.m_mainForm;


            // �̳��Զ�ʶ���OID
            if (connection.TargetInfo != null
                && connection.TargetInfo.DetectMarcSyntax == true)
            {
                //form.AutoDetectedMarcSyntaxOID = record.AutoDetectedSyntaxOID;
                form.UseAutoDetectedMarcSyntaxOID = true;
            }
            form.Show();

            form.LoadRecord(this, index);
        }

        // װ��DC�괰
        void menuItem_loadDcDetail_Click(object sender, EventArgs e)
        {
            int index = -1;
            string strError = "";

            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
            {
                strError = "��ǰZConnectionΪ��";
                goto ERROR1;
            }

            if (connection.VirtualItems.SelectedIndices.Count > 0)
                index = connection.VirtualItems.SelectedIndices[0];
            else
            {
                strError = "��δѡ��Ҫװ��ļ�¼";
                goto ERROR1;
            }

            DigitalPlatform.Z3950.Record record = null;
            if (index < connection.VirtualItems.Count)
            {
                record = (DigitalPlatform.Z3950.Record)
                    connection.VirtualItems[index].Tag;
            }
            else
            {
                strError = "indexԽ��";
                goto ERROR1;
            }

            // XML��ʽ����SUTRS
            if (record.m_strSyntaxOID == "1.2.840.10003.5.109.10" // XML
                )
            {
                DcForm form = new DcForm();

                form.MdiParent = this.m_mainForm;
                form.MainForm = this.m_mainForm;
                form.Show();

                form.LoadRecord(this, index);
                return;
            }
            else
            {
                strError = "��¼����XML��ʽ";
                goto ERROR1;
            }

            // return;
            ERROR1:
            MessageBox.Show(this, strError);
        }

        void menuItem_loadXmlDetail_Click(object sender, EventArgs e)
        {
            int index = -1;
            string strError = "";

            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
            {
                strError = "��ǰZConnectionΪ��";
                goto ERROR1;
            }

            if (connection.VirtualItems.SelectedIndices.Count > 0)
                index = connection.VirtualItems.SelectedIndices[0];
            else
            {
                strError = "��δѡ��Ҫװ��ļ�¼";
                goto ERROR1;
            }

            DigitalPlatform.Z3950.Record record = null;
            if (index < connection.VirtualItems.Count)
            {
                record = (DigitalPlatform.Z3950.Record)
                    connection.VirtualItems[index].Tag;
            }
            else
            {
                strError = "indexԽ��";
                goto ERROR1;
            }

            // XML��ʽ����SUTRS
            if (record.m_strSyntaxOID == "1.2.840.10003.5.109.10"
                || record.m_strSyntaxOID == "1.2.840.10003.5.101")
            {
                XmlDetailForm form = new XmlDetailForm();

                form.MdiParent = this.m_mainForm;
                form.MainForm = this.m_mainForm;
                form.Show();

                form.LoadRecord(this, index);
                return;
            }
            else
            {
                strError = "��¼����XML��ʽ��SUTRS��ʽ";
                goto ERROR1;
            }

            // return;
            ERROR1:
            MessageBox.Show(this, strError);
        }

        // �Զ����������װ�ص�MARC����XML��¼��
        void LoadDetail(int index)
        {
            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
            {
                MessageBox.Show(this, "��ǰZConnectionΪ��");
                return;
            }

            DigitalPlatform.Z3950.Record record = null;
            if (index < connection.VirtualItems.Count)
            {
                record = (DigitalPlatform.Z3950.Record)
                    connection.VirtualItems[index].Tag;
            }
            else
            {
                MessageBox.Show(this, "indexԽ��");
                return;
            }

            if (record.m_nDiagCondition != 0)
            {
                MessageBox.Show(this, "����һ����ϼ�¼");
                return;
            }

            // 2014/5/18
            string strSyntaxOID = record.AutoDetectedSyntaxOID;
            if (string.IsNullOrEmpty(strSyntaxOID) == true)
                strSyntaxOID = record.m_strSyntaxOID;

            // XML��ʽ����SUTRS��ʽ
            if (strSyntaxOID == "1.2.840.10003.5.109.10"
                || strSyntaxOID == "1.2.840.10003.5.101")
            {
                XmlDetailForm form = new XmlDetailForm();

                form.MdiParent = this.m_mainForm;
                form.MainForm = this.m_mainForm;
                form.Show();

                form.LoadRecord(this, index);
                return;
            }


            {
                MarcDetailForm form = new MarcDetailForm();


                form.MdiParent = this.m_mainForm;
                form.MainForm = this.m_mainForm;

                // �̳��Զ�ʶ���OID
                if (connection.TargetInfo != null
                    && connection.TargetInfo.DetectMarcSyntax == true)
                {
                    // form.AutoDetectedMarcSyntaxOID = record.AutoDetectedSyntaxOID;

                    form.UseAutoDetectedMarcSyntaxOID = true;
                }
                form.Show();

                form.LoadRecord(this, index);
            }
        }

        // ��ʱ����Stop��ʾ
        public void ActivateStopDisplay()
        {
            ZConnection connection = this.GetCurrentZConnection();
            if (connection != null)
            {
                m_mainForm.stopManager.Active(connection.Stop);
            }
            else
            {
                m_mainForm.stopManager.Active(null);
            }
        }

        private void ZSearchForm_Activated(object sender, EventArgs e)
        {
            /*
            if (Stop != null)
                MainForm.stopManager.Active(this.Stop);
             * */
            ZConnection connection = null;

            try
            {
                connection = this.GetCurrentZConnection();
            }
            catch
            {
                return;
            }

            int nSelectedCount = 0;
            if (connection != null)
            {
                m_mainForm.stopManager.Active(connection.Stop);

                nSelectedCount = connection.VirtualItems.SelectedIndices.Count;
            }
            else
            {
                m_mainForm.stopManager.Active(null);
            }

            m_mainForm.SetMenuItemState();

            // �˵�
            if (nSelectedCount == 0)
            {
                m_mainForm.MenuItem_saveOriginRecordToIso2709.Enabled = false;
                m_mainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = false;
            }
            else
            {
                m_mainForm.MenuItem_saveOriginRecordToIso2709.Enabled = true;
                m_mainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = true;
            }


            m_mainForm.MenuItem_font.Enabled = false;

            // ��������ť
            if (nSelectedCount == 0)
            {
                m_mainForm.toolButton_saveTo.Enabled = false;
                m_mainForm.toolButton_loadFullRecord.Enabled = false;
            }
            else
            {
                m_mainForm.toolButton_saveTo.Enabled = true;
                m_mainForm.toolButton_loadFullRecord.Enabled = true;
            }

            m_mainForm.toolButton_save.Enabled = false;
            m_mainForm.toolButton_search.Enabled = true;
            m_mainForm.toolButton_prev.Enabled = false;
            m_mainForm.toolButton_next.Enabled = false;
            m_mainForm.toolButton_nextBatch.Enabled = true;

            m_mainForm.toolButton_getAllRecords.Enabled = true;

            m_mainForm.toolButton_delete.Enabled = false;

            m_mainForm.toolButton_loadTemplate.Enabled = false;

            m_mainForm.toolButton_dup.Enabled = false;
            m_mainForm.toolButton_verify.Enabled = false;
            m_mainForm.toolButton_refresh.Enabled = false;
        }


        // ������ϵ�������Popup menu
        private void listView_browse_MouseUp(object sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripMenuItem menuItem = null;
            /*
            ToolStripMenuItem subMenuItem = null;
            ToolStripSeparator menuSepItem = null;
             * */
            ToolStripSeparator sep = null;

            DigitalPlatform.Z3950.Record record = null;
            int index = -1;

            ZConnection connection = this.GetCurrentZConnection();

            int nSelectedCount = 0;
            if (connection != null)
            {
                // SaveListViewSelectedToVirtual(connection);

                nSelectedCount = connection.VirtualItems.SelectedIndices.Count;
            }

            if (nSelectedCount > 0)
            {
                index = connection.VirtualItems.SelectedIndices[0];
                record = (DigitalPlatform.Z3950.Record)
                 connection.VirtualItems[index].Tag;
            }


            // װ��MARC��¼��
            menuItem = new ToolStripMenuItem("װ��MARC��¼��(&M)");
            menuItem.Click += new EventHandler(menuItem_loadMarcDetail_Click);
            if (record != null
                && (record.m_strSyntaxOID == "1.2.840.10003.5.1"
                || record.m_strSyntaxOID == "1.2.840.10003.5.10")
                )
            {
                menuItem.Enabled = true;
            }
            else if (record != null && record.m_strSyntaxOID == "1.2.840.10003.5.109.10")
            {
                // ��Ҫϸ�ж����ֿռ�
                string strNameSpaceUri = "";
                string strContent = Encoding.UTF8.GetString(record.m_baRecord);
                string strError = "";
                int nRet = GetRootNamespace(strContent,
                    out strNameSpaceUri,
                    out strError);
                if (nRet != -1 && strNameSpaceUri == Ns.usmarcxml)
                    menuItem.Enabled = true;
                else
                    menuItem.Enabled = false;
            }
            else
                menuItem.Enabled = false;

            contextMenu.Items.Add(menuItem);

            // װ��XML��¼��
            menuItem = new ToolStripMenuItem("װ��XML��¼��(&X)");
            menuItem.Click += new EventHandler(menuItem_loadXmlDetail_Click);
            if (record != null 
                && 
                (record.m_strSyntaxOID == "1.2.840.10003.5.109.10"  // XML
                || record.m_strSyntaxOID == "1.2.840.10003.5.101")  // SUTRS
                )
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // װ��DC��¼��
            menuItem = new ToolStripMenuItem("װ��DC��¼��(&D)");
            menuItem.Click += new EventHandler(menuItem_loadDcDetail_Click);
            if (record != null
                &&
                (record.m_strSyntaxOID == "1.2.840.10003.5.109.10"  // XML
                )
                )
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            // ȱʡ���뷽ʽ
            menuItem = new ToolStripMenuItem("ȱʡ���뷽ʽ");
            menuItem.Click += new EventHandler(menuItem_setDefualtEncoding_Click);
            contextMenu.Items.Add(menuItem);

            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            // ȫѡ
            menuItem = new ToolStripMenuItem("ȫѡ(&A)");
            menuItem.Click += new EventHandler(menuItem_selectAll_Click);
            contextMenu.Items.Add(menuItem);

            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            // װ��FullԪ�ؼ���¼
            menuItem = new ToolStripMenuItem("����װ��������ʽ��¼ [" + nSelectedCount.ToString() +"] (&F)...");
            menuItem.Click += new System.EventHandler(this.menu_reloadFullElementSet_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            // ׷�ӱ��浽���ݿ�
            menuItem = new ToolStripMenuItem("��׷�ӷ�ʽ���浽���ݿ� ["+ nSelectedCount.ToString() +"] (&A)...");
            menuItem.Click += new System.EventHandler(this.menu_saveToDatabase_Click);
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ---
            sep = new ToolStripSeparator();
            contextMenu.Items.Add(sep);

            // ����ԭʼ��¼���������ļ�
            menuItem = new ToolStripMenuItem("���浽�������ļ� [" + nSelectedCount.ToString()
                + "] (&W)");
            if (record != null
                && (record.m_strSyntaxOID == "1.2.840.10003.5.1"
                || record.m_strSyntaxOID == "1.2.840.10003.5.10"))
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            menuItem.Click += new EventHandler(menuItem_saveOriginRecordToWorksheet_Click);
            contextMenu.Items.Add(menuItem);


            // ����ԭʼ��¼��ISO2709�ļ�
            menuItem = new ToolStripMenuItem("���浽 MARC �ļ� ["+ nSelectedCount.ToString()
                +"] (&S)");
            if (record != null
                && (record.m_strSyntaxOID == "1.2.840.10003.5.1"
                || record.m_strSyntaxOID == "1.2.840.10003.5.10"))
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            menuItem.Click += new EventHandler(menuItem_saveOriginRecordToIso2709_Click);
            contextMenu.Items.Add(menuItem);

            contextMenu.Show(this.listView_browse, e.Location);
        }

        void menu_reloadFullElementSet_Click(object sender, EventArgs e)
        {
            ReloadFullElementSet();
        }

        // Ϊѡ������װ��FullԪ�ؼ��ļ�¼
        public void ReloadFullElementSet()
        {
            string strError = "";
            int nRet = 0;

            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
            {
                strError = "��ǰZConnectionΪ��";
                goto ERROR1;
            }

            if (connection.VirtualItems.SelectedIndices.Count == 0)
            {
                strError = "��δѡ��Ҫװ��������ʽ�������";
                goto ERROR1;
            }


            DigitalPlatform.Stop stop = null;
            stop = new DigitalPlatform.Stop();
            stop.Register(m_mainForm.stopManager, true);	// ����������

            stop.BeginLoop();

            this.EnableControls(false);
            try
            {
                List<int> selected = new List<int>();
                selected.AddRange(connection.VirtualItems.SelectedIndices);
                stop.SetProgressRange(0, selected.Count);

                for (int i = 0; i < selected.Count; i++)
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

                    int index = selected[i];

                    stop.SetMessage("��������װ�ؼ�¼ "+(index+1).ToString()+" ����ϸ��ʽ...");

                    byte[] baTimestamp = null;
                    string strSavePath = "";
                    string strOutStyle = "";
                    LoginInfo logininfo = null;
                    long lVersion = 0;
                    string strXmlFragment = "";
                    DigitalPlatform.Z3950.Record record = null;
                    Encoding currentEncoding = null;
                    string strMARC = "";

                    nRet = this.GetOneRecord(
                        "marc",
                        index,  // ������ֹ
                        "index:" + index.ToString(),
                        "force_full", // false,
                        out strSavePath,
                        out strMARC,
                        out strXmlFragment,
                        out strOutStyle,
                        out baTimestamp,
                        out lVersion,
                        out record,
                        out currentEncoding,
                        out logininfo,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    stop.SetProgressValue(i);

                }

                return;
            }
            finally
            {
                stop.EndLoop();
                stop.SetMessage("");
                stop.Unregister();	// ����������
                stop = null;

                this.EnableControls(true);
            }

    // return 0;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ѡ���������Ƿ������Brief��ʽ�ļ�¼
        bool HasSelectionContainBriefRecords()
        {
            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
            {
                return false;
            }
            for (int i = 0; i < connection.VirtualItems.SelectedIndices.Count; i++)
            {
                int index = connection.VirtualItems.SelectedIndices[i];
                DigitalPlatform.Z3950.Record record = (DigitalPlatform.Z3950.Record)
    connection.VirtualItems[index].Tag;

                if (record == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                if (record.m_strElementSetName == "B")
                    return true;
            }

            return false;
        }

        // ׷�ӱ��浽���ݿ�
        void menu_saveToDatabase_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            string strLastSavePath = m_mainForm.LastSavePath;
            if (String.IsNullOrEmpty(strLastSavePath) == false)
            {
                string strOutputPath = "";
                nRet = MarcDetailForm.ChangePathToAppendStyle(strLastSavePath,
                    out strOutputPath,
                    out strError);
                if (nRet == -1)
                {
                    m_mainForm.LastSavePath = ""; // �����´μ�������
                    goto ERROR1;
                }
                strLastSavePath = strOutputPath;
            }


            SaveRecordDlg dlg = new SaveRecordDlg();
            GuiUtil.SetControlFont(dlg, this.Font);

            dlg.SaveToDbMode = true;    // ��������textbox���޸�·��

            dlg.MainForm = this.m_mainForm;
            dlg.GetDtlpSearchParam += new GetDtlpSearchParamEventHandle(dlg_GetDtlpSearchParam);
            dlg.GetDp2SearchParam += new GetDp2SearchParamEventHandle(dlg_GetDp2SearchParam);
            {
                dlg.RecPath = strLastSavePath;
                dlg.Text = "��ѡ��Ŀ�����ݿ�";
            }
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);
            if (dlg.DialogResult != DialogResult.OK)
                return;

            m_mainForm.LastSavePath = dlg.RecPath;

            string strProtocol = "";
            string strPath = "";
            nRet = Global.ParsePath(dlg.RecPath,
                out strProtocol,
                out strPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
            {
                strError = "��ǰZConnectionΪ��";
                goto ERROR1;
            }

            if (connection.VirtualItems.SelectedIndices.Count == 0)
            {
                strError = "��δѡ��Ҫ�����¼�������";
                goto ERROR1;
            }

            bool bForceFull = false;

            if (HasSelectionContainBriefRecords() == true)
            {
                DialogResult result = MessageBox.Show(this,
"��������ļ�¼����Brief(��Ҫ)��ʽ�ļ�¼���Ƿ��ڱ���ǰ���»�ȡΪFull(����)��ʽ�ļ�¼?\r\n\r\n(Yes: �ǣ�Ҫ������ʽ�ļ�¼; No: ����Ȼ���������ʽ�ļ�¼�� Cancel: ȡ�������������������",
"ZSearchForm",
MessageBoxButtons.YesNoCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return;
                if (result == System.Windows.Forms.DialogResult.Yes)
                    bForceFull = true;
            }

            // TODO: ��ֹ�ʺ����������ID
            DigitalPlatform.Stop stop = null;
            stop = new DigitalPlatform.Stop();
            stop.Register(m_mainForm.stopManager, true);	// ����������

            stop.BeginLoop();

            this.EnableControls(false);
            try
            {

                // dtlpЭ��ļ�¼����
                if (strProtocol.ToLower() == "dtlp")
                {
                    DtlpSearchForm dtlp_searchform = this.GetDtlpSearchForm();

                    if (dtlp_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�DTLP���������޷������¼";
                        goto ERROR1;
                    }

                    for (int i = 0; i < connection.VirtualItems.SelectedIndices.Count; i++)
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

                        int index = connection.VirtualItems.SelectedIndices[i];

                        byte[] baTimestamp = null;
                        string strSavePath = "";
                        string strOutStyle = "";
                        LoginInfo logininfo = null;
                        long lVersion = 0;
                        string strXmlFragment = "";
                        DigitalPlatform.Z3950.Record record = null;
                        Encoding currentEncoding = null;
                        string strMARC = "";

                        nRet = this.GetOneRecord(
                            "marc",
                            index, // ������ֹ
                            "index:" + index.ToString(),
                            bForceFull == true ? "force_full" : "", // false,
                            out strSavePath,
                            out strMARC,
                            out strXmlFragment,
                            out strOutStyle,
                            out baTimestamp,
                            out lVersion,
                            out record,
                            out currentEncoding,
                            out logininfo,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;


                        string strMarcSyntax = "";
                        if (record.m_strSyntaxOID == "1.2.840.10003.5.1")
                            strMarcSyntax = "unimarc";
                        if (record.m_strSyntaxOID == "1.2.840.10003.5.10")
                            strMarcSyntax = "usmarc";

                        // TODO: ��Щ��ʽ���ʺϱ��浽Ŀ�����ݿ�

                        byte[] baOutputTimestamp = null;
                        string strOutputPath = "";
                        nRet = dtlp_searchform.SaveMarcRecord(
                            strPath,
                            strMARC,
                            baTimestamp,
                            out strOutputPath,
                            out baOutputTimestamp,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }

                    MessageBox.Show(this, "����ɹ�");
                    return;
                }
                else if (strProtocol.ToLower() == "dp2library")
                {
                    dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

                    if (dp2_searchform == null)
                    {
                        strError = "û�����ӵĻ��ߴ򿪵�dp2���������޷������¼";
                        goto ERROR1;
                    }

                    string strDp2ServerName = "";
                    string strPurePath = "";
                    // ������¼·����
                    // ��¼·��Ϊ������̬ "����ͼ��/1 @������"
                    dp2SearchForm.ParseRecPath(strPath,
                        out strDp2ServerName,
                        out strPurePath);

                    string strTargetMarcSyntax = "";

                    try
                    {
                        NormalDbProperty prop = dp2_searchform.GetDbProperty(strDp2ServerName,
             dp2SearchForm.GetDbName(strPurePath));
                        strTargetMarcSyntax = prop.Syntax;
                        if (string.IsNullOrEmpty(strTargetMarcSyntax) == true)
                            strTargetMarcSyntax = "unimarc";
                    }
                    catch (Exception ex)
                    {
                        strError = "�ڻ��Ŀ�������ʱ����: " + ex.Message;
                        goto ERROR1;
                    }

                    bool bSkip = false;
                    int nSavedCount = 0;

                    for (int i = 0; i < connection.VirtualItems.SelectedIndices.Count; i++)
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

                        int index = connection.VirtualItems.SelectedIndices[i];

                        byte[] baTimestamp = null;
                        string strSavePath = "";
                        string strOutStyle = "";
                        LoginInfo logininfo = null;
                        long lVersion = 0;
                        string strXmlFragment = "";
                        DigitalPlatform.Z3950.Record record = null;
                        Encoding currentEncoding = null;
                        string strMARC = "";

                        nRet = this.GetOneRecord(
                            "marc",
                            index,  // ������ֹ
                            "index:" + index.ToString(),
                            bForceFull == true ? "force_full" : "", // false,
                            out strSavePath,
                            out strMARC,
                            out strXmlFragment,
                            out strOutStyle,
                            out baTimestamp,
                            out lVersion,
                            out record,
                            out currentEncoding,
                            out logininfo,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        string strMarcSyntax = "";
                        if (record.m_strSyntaxOID == "1.2.840.10003.5.1")
                            strMarcSyntax = "unimarc";
                        if (record.m_strSyntaxOID == "1.2.840.10003.5.10")
                            strMarcSyntax = "usmarc";
#endif
                        // 2014/5/12
                        string strMarcSyntax = MarcDetailForm.GetMarcSyntax(record.m_strSyntaxOID);

                        // ��Щ��ʽ���ʺϱ��浽Ŀ�����ݿ�
                        if (strTargetMarcSyntax != strMarcSyntax)
                        {
                            if (bSkip == true)
                                continue;
                            strError = "��¼ "+(index+1).ToString()+" �ĸ�ʽ����Ϊ '"+strMarcSyntax+"'����Ŀ���ĸ�ʽ���� '"+strTargetMarcSyntax+"' �����ϣ�����޷����浽Ŀ���";
                            DialogResult result = MessageBox.Show(this,
        strError + "\r\n\r\nҪ������Щ��¼�������������ļ�¼ô?\r\n\r\n(Yes: ������ʽ���Ǻϵļ�¼��������������; No: ���������������)",
        "ZSearchForm",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                            if (result == System.Windows.Forms.DialogResult.No)
                                goto ERROR1;
                            bSkip = true;
                            continue;
                        }

                        string strProtocolPath = this.CurrentProtocol + ":"
    + this.CurrentResultsetPath
    + "/" + (index + 1).ToString();

                        string strOutputPath = "";
                        byte[] baOutputTimestamp = null;
                        string strComment = "copy from " + strProtocolPath; // strSavePath;
                        // return:
                        //      -2  timestamp mismatch
                        //      -1  error
                        //      0   succeed
                        nRet = dp2_searchform.SaveMarcRecord(
                            false,
                            strPath,
                            strMARC,
                            strMarcSyntax,
                            baTimestamp,
                            strXmlFragment,
                            strComment,
                            out strOutputPath,
                            out baOutputTimestamp,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        nSavedCount++;

                    }
                    MessageBox.Show(this, "�������¼ "+nSavedCount.ToString()+" ��");
                    return;
                }
                else if (strProtocol.ToLower() == "z3950")
                {
                    strError = "Ŀǰ�ݲ�֧��Z39.50Э��ı������";
                    goto ERROR1;
                }
                else
                {
                    strError = "�޷�ʶ���Э���� '" + strProtocol + "'";
                    goto ERROR1;
                }

            }
            finally
            {
                stop.EndLoop();

                stop.Unregister();	// ����������
                stop = null;

                this.EnableControls(true);
            }

            // return 0;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        DtlpSearchForm GetDtlpSearchForm()
        {
            DtlpSearchForm dtlp_searchform = null;

            dtlp_searchform = this.m_mainForm.TopDtlpSearchForm;

            if (dtlp_searchform == null)
            {
                // �¿�һ��dtlp������
                FormWindowState old_state = this.WindowState;

                dtlp_searchform = new DtlpSearchForm();
                dtlp_searchform.MainForm = this.m_mainForm;
                dtlp_searchform.MdiParent = this.m_mainForm;
                dtlp_searchform.WindowState = FormWindowState.Minimized;
                dtlp_searchform.Show();

                this.WindowState = old_state;
                this.Activate();

                // ��Ҫ�ȴ���ʼ�������������
                dtlp_searchform.WaitLoadFinish();
            }

            return dtlp_searchform;
        }

        dp2SearchForm GetDp2SearchForm()
        {
            dp2SearchForm dp2_searchform = null;


            dp2_searchform = this.m_mainForm.TopDp2SearchForm;

            if (dp2_searchform == null)
            {
                // �¿�һ��dp2������
                FormWindowState old_state = this.WindowState;

                dp2_searchform = new dp2SearchForm();
                dp2_searchform.MainForm = this.m_mainForm;
                dp2_searchform.MdiParent = this.m_mainForm;
                dp2_searchform.WindowState = FormWindowState.Minimized;
                dp2_searchform.Show();

                this.WindowState = old_state;
                this.Activate();

                // ��Ҫ�ȴ���ʼ�������������
                dp2_searchform.WaitLoadFinish();
            }

            return dp2_searchform;
        }

        void dlg_GetDp2SearchParam(object sender, GetDp2SearchParamEventArgs e)
        {
            dp2SearchForm dp2_searchform = this.GetDp2SearchForm();

            e.dp2Channels = dp2_searchform.Channels;
            e.MainForm = this.m_mainForm;
        }

        void dlg_GetDtlpSearchParam(object sender, GetDtlpSearchParamEventArgs e)
        {
            DtlpSearchForm dtlp_searchform = this.GetDtlpSearchForm();

            e.DtlpChannels = dtlp_searchform.DtlpChannels;
            e.DtlpChannel = dtlp_searchform.DtlpChannel;
        }

        void menuItem_selectAll_Click(object sender,
            EventArgs e)
        {
            for (int i = 0; i < this.listView_browse.Items.Count; i++)
            {
                this.listView_browse.Items[i].Selected = true;
            }
        }

        // �任ISO2709��¼�ı��뷽ʽ
        public static int ChangeIso2709Encoding(
            Encoding sourceEncoding,
            byte [] baSource,
            Encoding targetEncoding,
            string strMarcSyntax,
            out byte [] baTarget,
            out string strError)
        {
            baTarget = null;
            strError = "";

            string strMARC = "";
            // ��byte[]���͵�MARC��¼ת��Ϊ���ڸ�ʽ
            // return:
            //		-2	MARC��ʽ��
            //		-1	һ�����
            //		0	����
            int nRet = MarcUtil.ConvertByteArrayToMarcRecord(
                baSource,
                sourceEncoding,
                true,   // bool bForce,
                out strMARC,
                out strError);
            if (nRet == -1 || nRet == -2)
                return -1;


            // ��MARC���ڸ�ʽת��ΪISO2709��ʽ
            // parameters:
            //		strMarcSyntax   "unimarc" "usmarc"
            //		strSourceMARC		[in]���ڸ�ʽMARC��¼��
            //		targetEncoding	[in]���ISO2709�ı��뷽ʽΪ UTF8 codepage-936�ȵ�
            //		baResult	[out]�����ISO2709��¼���ַ�����nCharset�������ơ�
            //					ע�⣬������ĩβ������0�ַ���
            nRet = MarcUtil.CvtJineiToISO2709(
                strMARC,
                strMarcSyntax,
                targetEncoding,
                out baTarget,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        public void menuItem_saveOriginRecordToWorksheet_Click(object sender,
    EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
            {
                strError = "��ǰZConnectionΪ��";
                goto ERROR1;
            }

            if (connection.VirtualItems.SelectedIndices.Count == 0)
            {
                strError = "��δѡ��Ҫ�����¼�������";
                goto ERROR1;
            }

            bool bForceFull = false;

            if (HasSelectionContainBriefRecords() == true)
            {
                DialogResult result = MessageBox.Show(this,
"��������ļ�¼����Brief(��Ҫ)��ʽ�ļ�¼���Ƿ��ڱ���ǰ���»�ȡΪFull(����)��ʽ�ļ�¼?\r\n\r\n(Yes: �ǣ�Ҫ������ʽ�ļ�¼; No: ����Ȼ���������ʽ�ļ�¼�� Cancel: ȡ�������������������",
"ZSearchForm",
MessageBoxButtons.YesNoCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return;
                if (result == System.Windows.Forms.DialogResult.Yes)
                    bForceFull = true;
            }

            // ѯ���ļ���
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ����Ĺ������ļ���";
            dlg.CreatePrompt = false;
            dlg.OverwritePrompt = false;
            dlg.FileName = m_mainForm.LastWorksheetFileName;
            dlg.Filter = "�������ļ� (*.wor)|*.wor|All files (*.*)|*.*";

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;


            bool bExist = File.Exists(dlg.FileName);
            bool bAppend = false;

            if (bExist == true)
            {
                DialogResult result = MessageBox.Show(this,
        "�ļ� '" + dlg.FileName + "' �Ѵ��ڣ��Ƿ���׷�ӷ�ʽд���¼?\r\n\r\n--------------------\r\nע��(��)׷��  (��)����  (ȡ��)����",
        "ZSearchForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                    bAppend = true;

                if (result == DialogResult.No)
                    bAppend = false;

                if (result == DialogResult.Cancel)
                {
                    strError = "��������...";
                    goto ERROR1;
                }
            }


            m_mainForm.LastWorksheetFileName = dlg.FileName;

            StreamWriter sw = null;

            try
            {
                // �����ļ�
                sw = new StreamWriter(m_mainForm.LastWorksheetFileName,
                    bAppend,	// append
                    System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                strError = "�򿪻򴴽��ļ� " + m_mainForm.LastWorksheetFileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }

            Encoding preferredEncoding = Encoding.UTF8;

            try
            {
                for (int i = 0; i < connection.VirtualItems.SelectedIndices.Count; i++)
                {
                    int index = connection.VirtualItems.SelectedIndices[i];

                    byte[] baTimestamp = null;
                    string strSavePath = "";
                    string strOutStyle = "";
                    LoginInfo logininfo = null;
                    long lVersion = 0;
                    string strXmlFragment = "";
                    DigitalPlatform.Z3950.Record record = null;
                    Encoding currentEncoding = null;
                    string strMARC = "";

                    nRet = this.GetOneRecord(
                        "marc",
                        index,  // ������ֹ
                        "index:" + index.ToString(),
                        bForceFull == true ? "force_full" : "", // false,
                        out strSavePath,
                        out strMARC,
                        out strXmlFragment,
                        out strOutStyle,
                        out baTimestamp,
                        out lVersion,
                        out record,
                        out currentEncoding,
                        out logininfo,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;


                    string strMarcSyntax = "";
                    if (record.m_strSyntaxOID == "1.2.840.10003.5.1")
                        strMarcSyntax = "unimarc";
                    if (record.m_strSyntaxOID == "1.2.840.10003.5.10")
                        strMarcSyntax = "usmarc";

                    // TODO: ��Щ��ʽ���ʺϱ��浽�������ļ�

                    List<string> lines = null;
                    // �����ڸ�ʽ�任Ϊ��������ʽ
                    // return:
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = MarcUtil.CvtJineiToWorksheet(
                        strMARC,
                        -1,
                        out lines,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    foreach (string line in lines)
                    {
                        sw.WriteLine(line);
                    }

                }

                // 
                if (bAppend == true)
                    m_mainForm.MessageText = connection.VirtualItems.SelectedIndices.Count.ToString()
                        + "����¼�ɹ�׷�ӵ��ļ� " + m_mainForm.LastWorksheetFileName + " β��";
                else
                    m_mainForm.MessageText = connection.VirtualItems.SelectedIndices.Count.ToString()
                        + "����¼�ɹ����浽���ļ� " + m_mainForm.LastWorksheetFileName + " β��";

            }
            catch (Exception ex)
            {
                strError = "д���ļ� " + m_mainForm.LastWorksheetFileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }
            finally
            {
                sw.Close();
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        public void menuItem_saveOriginRecordToIso2709_Click(object sender,
            EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
            {
                strError = "��ǰZConnectionΪ��";
                goto ERROR1;
            }

            if (connection.VirtualItems.SelectedIndices.Count == 0)
            {
                strError = "��δѡ��Ҫ�����¼�������";
                goto ERROR1;
            }

            bool bForceFull = false;

            if (HasSelectionContainBriefRecords() == true)
            {
                DialogResult result = MessageBox.Show(this,
"��������ļ�¼����Brief(��Ҫ)��ʽ�ļ�¼���Ƿ��ڱ���ǰ���»�ȡΪFull(����)��ʽ�ļ�¼?\r\n\r\n(Yes: �ǣ�Ҫ������ʽ�ļ�¼; No: ����Ȼ���������ʽ�ļ�¼�� Cancel: ȡ�������������������",
"ZSearchForm",
MessageBoxButtons.YesNoCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return;
                if (result == System.Windows.Forms.DialogResult.Yes)
                    bForceFull = true;
            }


            Encoding preferredEncoding = null;
            

            // string strPreferedMarcSyntax = "";

            {
                // �۲�Ҫ����ĵ�һ����¼��marc syntax
                int first_index = connection.VirtualItems.SelectedIndices[0];
                VirtualItem first_item = connection.VirtualItems[first_index];
                DigitalPlatform.Z3950.Record first_record = (DigitalPlatform.Z3950.Record)first_item.Tag;

                /*
                if (first_record.m_strMarcSyntaxOID == "1.2.840.10003.5.1")
                    strPreferedMarcSyntax = "unimarc";
                if (first_record.m_strMarcSyntaxOID == "1.2.840.10003.5.10")
                    strPreferedMarcSyntax = "usmarc";
                 * */

                preferredEncoding = connection.GetRecordsEncoding(
                    this.m_mainForm,
                    first_record.m_strSyntaxOID);

            }


            /*
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = MainForm.LastIso2709FileName;
            dlg.RestoreDirectory = true;
            dlg.CreatePrompt = true;
            dlg.OverwritePrompt = false;
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.Filter = "iso2709 files (*.mrc)|*.mrc|All files (*.*)|*.*";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            MainForm.LastIso2709FileName = dlg.FileName;
             * */

            OpenMarcFileDlg dlg = new OpenMarcFileDlg();
            GuiUtil.SetControlFont(dlg, this.Font);
            dlg.IsOutput = true;
            dlg.GetEncoding -= new GetEncodingEventHandler(dlg_GetEncoding);
            dlg.GetEncoding += new GetEncodingEventHandler(dlg_GetEncoding);
            dlg.FileName = m_mainForm.LastIso2709FileName;
            dlg.CrLf = m_mainForm.LastCrLfIso2709;
            dlg.RemoveField998Visible = false;
            dlg.Mode880Visible = false; // ��ʱ��֧�� 880 ģʽת��
            dlg.EncodingListItems = Global.GetEncodingList(true);
            dlg.EncodingName =
                (String.IsNullOrEmpty(m_mainForm.LastEncodingName) == true ? GetEncodingForm.GetEncodingName(preferredEncoding) : m_mainForm.LastEncodingName);
            dlg.EncodingComment = "ע: ԭʼ���뷽ʽΪ " + GetEncodingForm.GetEncodingName(preferredEncoding);
            dlg.MarcSyntax = "<�Զ�>";    // strPreferedMarcSyntax;
            dlg.EnableMarcSyntax = false;
            dlg.ShowDialog(this);
            if (dlg.DialogResult != DialogResult.OK)
                return;

            Encoding targetEncoding = null;

            if (dlg.EncodingName == "MARC-8"
                && preferredEncoding.Equals(this.m_mainForm.Marc8Encoding) == false)
            {
                strError = "��������޷����С�ֻ���ڼ�¼��ԭʼ���뷽ʽΪ MARC-8 ʱ������ʹ��������뷽ʽ�����¼��";
                goto ERROR1;
            }

            nRet = this.m_mainForm.GetEncoding(dlg.EncodingName,
                out targetEncoding,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }

            // targetEncoding = dlg.Encoding;

            /*
            strPreferedMarcSyntax = dlg.MarcSyntax;
            if (strPreferedMarcSyntax == "<�Զ�>")
                strPreferedMarcSyntax = "";
             * */

            string strLastFileName = m_mainForm.LastIso2709FileName;
            string strLastEncodingName = m_mainForm.LastEncodingName;


            bool bExist = File.Exists(dlg.FileName);
            bool bAppend = false;

            if (bExist == true)
            {
                DialogResult result = MessageBox.Show(this,
        "�ļ� '" + dlg.FileName + "' �Ѵ��ڣ��Ƿ���׷�ӷ�ʽд���¼?\r\n\r\n--------------------\r\nע��(��)׷��  (��)����  (ȡ��)����",
        "ZSearchForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Yes)
                    bAppend = true;

                if (result == DialogResult.No)
                    bAppend = false;

                if (result == DialogResult.Cancel)
                {
                    strError = "��������...";
                    goto ERROR1;
                }
            }

            // ���ͬһ���ļ�������ʱ��ı��뷽ʽһ����
            if (strLastFileName == dlg.FileName
                && bAppend == true)
            {
                if (strLastEncodingName != ""
                    && strLastEncodingName != dlg.EncodingName)
                {
                    DialogResult result = MessageBox.Show(this,
                        "�ļ� '" + dlg.FileName + "' ������ǰ�Ѿ��� " + strLastEncodingName + " ���뷽ʽ�洢�˼�¼���������Բ�ͬ�ı��뷽ʽ " + dlg.EncodingName + " ׷�Ӽ�¼�����������ͬһ�ļ��д��ڲ�ͬ���뷽ʽ�ļ�¼�����ܻ������޷�����ȷ��ȡ��\r\n\r\n�Ƿ����? (��)׷��  (��)��������",
                        "ZSearchForm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                    {
                        strError = "��������...";
                        goto ERROR1;
                    }

                }
            }

            m_mainForm.LastIso2709FileName = dlg.FileName;
            m_mainForm.LastCrLfIso2709 = dlg.CrLf;
            m_mainForm.LastEncodingName = dlg.EncodingName;

            Stream s = null;

            try
            {
                s = File.Open(m_mainForm.LastIso2709FileName,
                     FileMode.OpenOrCreate);
                if (bAppend == false)
                    s.SetLength(0);
                else
                    s.Seek(0, SeekOrigin.End);
            }
            catch (Exception ex)
            {
                strError = "�򿪻򴴽��ļ� " + m_mainForm.LastIso2709FileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }

            try
            {
                for (int i = 0; i < connection.VirtualItems.SelectedIndices.Count; i++)
                {
                    int index = connection.VirtualItems.SelectedIndices[i];

                    /*
                    VirtualItem item = connection.VirtualItems[index];

                    DigitalPlatform.Z3950.Record record = (DigitalPlatform.Z3950.Record)item.Tag;
                    */
                    byte[] baTimestamp = null;
                    string strSavePath = "";
                    string strOutStyle = "";
                    LoginInfo logininfo = null;
                    long lVersion = 0;
                    string strXmlFragment = "";
                    DigitalPlatform.Z3950.Record record = null;
                    Encoding currentEncoding = null;
                    string strMARC = "";

                    nRet = this.GetOneRecord(
                        "marc",
                        index,  // ������ֹ
                        "index:" + index.ToString(),
                        bForceFull == true ? "force_full" : "", // false,
                        out strSavePath,
                        out strMARC,
                        out strXmlFragment,
                        out strOutStyle,
                        out baTimestamp,
                        out lVersion,
                        out record,
                        out currentEncoding,
                        out logininfo,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    byte[] baTarget = null;

                    Encoding sourceEncoding = connection.GetRecordsEncoding(
                        this.m_mainForm,
                        record.m_strSyntaxOID);

                    string strMarcSyntax = "";
                    if (record.m_strSyntaxOID == "1.2.840.10003.5.1")
                        strMarcSyntax = "unimarc";
                    if (record.m_strSyntaxOID == "1.2.840.10003.5.10")
                        strMarcSyntax = "usmarc";

                    if (sourceEncoding.Equals(targetEncoding) == true)
                    {
                        // source��target���뷽ʽ��ͬ������ת��
                        // baTarget = record.m_baRecord;

                        // �淶�� ISO2709 �����¼
                        // ��Ҫ�Ǽ������ļ�¼�������Ƿ���ȷ��ȥ������ļ�¼������
                        baTarget = MarcUtil.CononicalizeIso2709Bytes(targetEncoding,
                            record.m_baRecord);
                    }
                    else
                    {
                        nRet = ChangeIso2709Encoding(
                            sourceEncoding,
                            record.m_baRecord,
                            targetEncoding,
                            strMarcSyntax,
                            out baTarget,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }

                    s.Write(baTarget, 0,
                        baTarget.Length);

                    if (dlg.CrLf == true)
                    {
                        byte[] baCrLf = targetEncoding.GetBytes("\r\n");
                        s.Write(baCrLf, 0,
                            baCrLf.Length);
                    }
                }

                // 
                if (bAppend == true)
                    m_mainForm.MessageText = connection.VirtualItems.SelectedIndices.Count.ToString() 
                        + "����¼�ɹ�׷�ӵ��ļ� " + m_mainForm.LastIso2709FileName + " β��";
                else
                    m_mainForm.MessageText = connection.VirtualItems.SelectedIndices.Count.ToString()
                        + "����¼�ɹ����浽���ļ� " + m_mainForm.LastIso2709FileName + " β��";

            }
            catch (Exception ex)
            {
                strError = "д���ļ� " + m_mainForm.LastIso2709FileName + " ʧ�ܣ�ԭ��: " + ex.Message;
                goto ERROR1;
            }
            finally
            {
                s.Close();
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void dlg_GetEncoding(object sender, GetEncodingEventArgs e)
        {
            string strError = "";
            Encoding encoding = null;
            int nRet = this.m_mainForm.GetEncoding(e.EncodingName,
                out encoding,
                out strError);
            if (nRet == -1)
            {
                e.ErrorInfo = strError;
                return;
            }
            e.Encoding = encoding;
        }


        void menuItem_setDefualtEncoding_Click(object sender,
            EventArgs e)
        {
            ZConnection connection = this.GetCurrentZConnection();

            GetEncodingForm dlg = new GetEncodingForm();
            GuiUtil.SetControlFont(dlg, this.Font);
            dlg.Encoding = connection.ForcedRecordsEncoding;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            connection.ForcedRecordsEncoding = dlg.Encoding;

            // ˢ��listview�ڵ�ȫ����
            RefreshBrowseViewDisplay();
        }

        // ̽��MARC��¼�����ĸ�ʽ��
        // return:
        //		-1	�޷�̽��
        //		1	UNIMARC	���򣺰���200�ֶ�
        //		10	USMARC	���򣺰���008�ֶ�(innopac��UNIMARC��ʽҲ��һ����ֵ�008)
        public static int DetectMARCSyntax(string strMARC)
        {
            int nRet = 0;

            if (String.IsNullOrEmpty(strMARC) == true)
                return -1;

            string strField = "";
            string strNextFieldName = "";

            nRet = MarcUtil.GetField(strMARC,
                "200",
                0,
                out strField,
                out strNextFieldName);
            if (nRet != -1 && nRet != 0)
                return 1;	// UNIMARC

            nRet = MarcUtil.GetField(strMARC,
                "008",
                0,
                out strField,
                out strNextFieldName);
            if (nRet != -1 && nRet != -1)
                return 10;	// USMARC

            return -1;
        }

        int RefreshBrowseViewDisplay()
        {
            ZConnection connection = this.GetCurrentZConnection();

            for (int i = 0; i < connection.VirtualItems.Count; i++)
            {
                Application.DoEvents();	// ���ý������Ȩ

                /*
                if (stop != null)
                {
                    if (stop.State != 0)
                        break;
                }
                 * */


                DigitalPlatform.Z3950.Record record = (DigitalPlatform.Z3950.Record)
                    connection.VirtualItems[i].Tag;

                string strError = "";
                string strBrowseText = "";
                int nRet = 0;

                /*

                string strMARC = "";
                byte[] baRecord = record.m_baRecord;    //Encoding.ASCII.GetBytes(record.m_strRecord);
                string strMarcSyntaxOID = record.m_strMarcSyntaxOID;
                // ISO2709ת��Ϊ���ڸ�ʽ
                nRet = Marc8Encoding.ConvertByteArrayToMarcRecord(
                    baRecord,
                    connection.GetRecordsEncoding(this.MainForm, strMarcSyntaxOID),  // Encoding.GetEncoding(936),
                    true,
                    out strMARC,
                    out strError);
                if (nRet < 0)
                {
                    strBrowseText = strError;
                    goto DOREFRESH;
                }


                if (connection.TargetInfo.DetectMarcSyntax == true)
                {
                    // ̽��MARC��¼�����ĸ�ʽ��
                    // return:
                    //		-1	�޷�̽��
                    //		1	UNIMARC	���򣺰���200�ֶ�
                    //		10	USMARC	���򣺰���008�ֶ�(innopac��UNIMARC��ʽҲ��һ����ֵ�008)
                    nRet = DetectMARCSyntax(strMARC);
                    if (nRet == 1)
                        strMarcSyntaxOID = "1.2.840.10003.5.1";
                    else if (nRet == 10)
                        strMarcSyntaxOID = "1.2.840.10003.5.10";


                    // ���Զ�ʶ��Ľ����������
                    record.AutoDetectedMarcSyntaxOID = strMarcSyntaxOID;
                }


                nRet = BuildMarcBrowseText(
                    strMarcSyntaxOID,
                    strMARC,
                    out strBrowseText,
                    out strError);
                if (nRet == -1)
                    strBrowseText = strError;
                 * */
                int nImageIndex = 0;

                nRet = BuildBrowseText(
                    connection,
                    record,
                    "marc", // ƫ��MARC
                    out strBrowseText,
                    out nImageIndex,
                    out strError);
                if (nRet == -1)
                    strBrowseText = strError;


            // DOREFRESH:

                VirtualItem item = connection.VirtualItems[i];

                string[] cols = strBrowseText.Split(new char[] { '\t' });
                for (int j = 0; j < cols.Length; j++)
                {
                    item.SubItems[j+1] = cols[j];
                }

                item.ImageIndex = nImageIndex;

            }

            this.listView_browse.Invalidate();
            return 0;
        }



        int GetBrowseListViewSelectedItemCount()
        {
            ZConnection connection = this.GetCurrentZConnection();
            if (connection == null)
                return 0;
            return connection.VirtualItems.SelectedIndices.Count;
        }


        // ������̵�����
        protected override bool ProcessDialogKey(
            Keys keyData)
        {
            // �س�
            if (keyData == Keys.Enter)
            {
                if (this.queryControl1.Focused == true)
                {
                    // ����������س�
                    // this.DoSearchOneServer();
                    this.DoSearch();
                }
                else if (this.listView_browse.Focused == true)
                {
                    // ������лس�
                    listView_browse_DoubleClick(this, null);
                }

                return true;
            }

            return false;
        }

        // ��ø��ڵ�����ֿռ�
        public static int GetRootNamespace(string strXml,
            out string strNameSpaceUri,
            out string strError)
        {
            strNameSpaceUri = "";
            strError = "";

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XML����װ�ص�XMLDOMʱ����: " + ex.Message;
                return -1;
            }

            XmlNode root = dom.DocumentElement;

            strNameSpaceUri = root.NamespaceURI;
            return 0;
        }

        void InitialPresentFormat(TargetInfo targetInfo)
        {
            if (targetInfo == null)
            {
                this.comboBox_elementSetName.Text = "";
                this.comboBox_recordSyntax.Text = "";
            }
            else
            {
                this.comboBox_elementSetName.Text = targetInfo.DefaultElementSetName;
                this.comboBox_recordSyntax.Text = targetInfo.PreferredRecordSyntax;
            }
        }

        // Ŀ������ѡ�񼴽������ı�
        private void zTargetControl1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            try
            {
                ZConnection connection = this.GetCurrentZConnection();

                // �������ʽ�ؼ�����
                if (connection != null)
                {
                    // �������ʽ
                    connection.QueryXml = this.queryControl1.GetContent(true);
                    // �����¼�﷨
                    connection.RecordSyntax = this.comboBox_recordSyntax.Text;
                    // ����Ԫ�ؼ���
                    connection.ElementSetName = this.comboBox_elementSetName.Text;

                    // ����listview�е�ѡ������
                    SaveListViewSelectedToVirtual(connection);
                }
            }
            catch
            {
            }
        }

        void SaveListViewSelectedToVirtual(ZConnection connection)
        {
            // ����listview�е�ѡ������
            for (int i = 0; i < this.listView_browse.Items.Count; i++)
            {
                ListViewItem item = this.listView_browse.Items[i];

                connection.VirtualItems[i].Selected = item.Selected;
            }
        }

        // Ŀ������ѡ���Ѿ������ı�
        private void zTargetControl1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // string strError = "";

            ZConnection connection = null;

            try
            {
                connection = this.GetCurrentZConnection();
            }
            catch
            {
                return;
            }

            // ���ǰStop����
            m_mainForm.stopManager.Active(connection == null ? null : connection.Stop);

            /*
            if (connection.TargetInfo == null)
            {
                TargetInfo targetinfo = null;
                int nRet = this.zTargetControl1.GetCurrentTarget(
                    out targetinfo,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                connection.TargetInfo = targetinfo;
            }
             * */

            // Debug.Assert(connection.TargetInfo != null, "");

            // ��ʼ��present format�����ʾ
            InitialPresentFormat(connection == null ? null : connection.TargetInfo);

            // ���ü���ʽ�ؼ�����
            this.queryControl1.SetContent(connection == null ? null : connection.QueryXml);

            // ���ü�¼�﷨
            if (String.IsNullOrEmpty(connection.RecordSyntax) == false)
                this.comboBox_recordSyntax.Text = connection.RecordSyntax;

            // ����Ԫ�ؼ���
            if (String.IsNullOrEmpty(connection.ElementSetName) == false)
                this.comboBox_elementSetName.Text = connection.ElementSetName;


            // ��������ʽ�ؼ�Enabled״̬
            EnableQueryControl(connection == null ? false : connection.Enabled);

            /*
            // ��ʼ�������ʾ
            this.listView_browse.Items.Clear();
             * */

            LinkRecordsToListView(connection == null ? null : connection.VirtualItems);
            /*
            if (connection != null
                && connection.Records != null
                && connection.Records.Count != 0)
            {
                Debug.Assert(connection.Stop.State != 0, "������stop��ʾ���ڴ����ʱ����ʹ��ͬһ��stop��ʾ�µ�ѭ��");
                // TODO: �����ڳ����������������ʱ����listview�ĵ�һ�з�һ��������Ϣ�������û������������л��ķ�������������listview

                connection.Stop.OnStop += new StopEventHandler(this.DoStop);
                connection.Stop.SetMessage("����װ�������Ϣ ...");
                connection.Stop.BeginLoop();

                // EnableControls(false);

                try
                {

                    int nRet = FillRecordsToBrowseView(
                        connection.Stop,
                        connection,
                        connection.Records,
                        0,
                        connection.Records.Count,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }
                finally
                {
                    connection.Stop.EndLoop();
                    connection.Stop.OnStop -= new StopEventHandler(this.DoStop);
                    connection.Stop.Initial("");

                    // EnableControls(true);
                }
            }
             * */

            // this.textBox_resultInfo.Text = (connection == null ? "" : connection.ErrorInfo);
            this.textBox_resultInfo.Text = "";
            if (connection != null)
            {
                if (String.IsNullOrEmpty(connection.ErrorInfo) == false)
                    this.textBox_resultInfo.Text = connection.ErrorInfo;
                else
                {
                    if (connection.ResultCount >= 0)
                        this.textBox_resultInfo.Text = "���н����: " + connection.ResultCount.ToString();
                }
            }


            return;
            /*
        ERROR1:
            MessageBox.Show(this, strError);
            return;
             * */
        }

        // �����⼯���������ӵ���ǰlistview��
        // ע�⣺listview�ǹ��õģ�����Ӧ�����ӵ�ǰZConnection�����⼯�ϣ���ҪŪ����
        void LinkRecordsToListView(VirtualItemCollection items)
        {
            this.CurrentBrowseItems = items;
            items.ExpireSelectedIndices();

            if (this.CurrentBrowseItems != null)
                this.listView_browse.VirtualListSize = this.CurrentBrowseItems.Count;
            else
                this.listView_browse.VirtualListSize = 0;

            // �ָ�Selected״̬
            if (items != null)
            {
                for (int i = 0; i < items.SelectedIndices.Count; i++)
                {
                    int index = items.SelectedIndices[i];
                    this.listView_browse.Items[index].Selected = true;
                }
            }

            // ��ʹˢ��
            this.listView_browse.Invalidate();
        }


        /*
        void LinkRecordsToListView(RecordCollection records)
        {
            this.CurrentRecords = records;

            if (this.CurrentRecords != null)
                this.listView_browse.VirtualListSize = this.CurrentRecords.Count;
            else
                this.listView_browse.VirtualListSize = 0;

            // ��ʹˢ��
            this.listView_browse.Invalidate();
        }*/

#if NOOOOOOOOOOOOOOOOOOOOO
        // �Ѵ洢��records�ṹ�е���Ϣ����listview
        // parameters:
        int FillRecordsToBrowseView(
            Stop stop,
            ZConnection connection,
            RecordCollection records,
            int nStart,
            int nCount,
            out string strError)
        {
            strError = "";

            if (records == null)
                return 0;

            if (nStart + nCount > records.Count)
            {
                strError = "nStart["+nStart.ToString()+"]��nCount["+nCount.ToString()+"]����֮�ͳ���records���ϵĳߴ�["+records.Count.ToString()+"]";
                return -1;
            }

            for (int i = nStart; i < nStart + nCount; i++)
            {
                Application.DoEvents();	// ���ý������Ȩ

                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                        // TODO: �жϺ���ô�죿�����һ����¼������Records�е������
                    }
                }

                DigitalPlatform.Z3950.Record record = records[i];

                ListViewItem item = new ListViewItem(
                    (nStart + i + 1).ToString(),
                    record.m_nDiagCondition == 0 ? BROWSE_TYPE_NORMAL : BROWSE_TYPE_DIAG);

                string strBrowseText = "";

                int nRet = 0;
                string[] cols = null;

                if (record.m_nDiagCondition != 0)
                {
                    strBrowseText = "��ϼ�¼ condition=" + record.m_nDiagCondition.ToString() + "; addinfo=\"" + record.m_strAddInfo + "\"; diagSetOID=" + record.m_strDiagSetID;
                    goto DOADD;
                }
                else
                {
                    byte[] baRecord = record.m_baRecord;    //Encoding.ASCII.GetBytes(record.m_strRecord);

                    string strMARC = "";
                    string strMarcSyntaxOID = "";

                    // ����ΪXML��ʽ
                    if (record.m_strMarcSyntaxOID == "1.2.840.10003.5.109.10")
                    {
                        // �����ڵ�����ֿռ䣬�������MARCXML, ����ת��ΪUSMARC�����򣬾�ֱ�Ӹ������ֿռ�����ʽ�����ת��
                        string strContent = Encoding.UTF8.GetString(baRecord);

                        string strNameSpaceUri = "";
                        nRet = GetRootNamespace(strContent,
                            out strNameSpaceUri,
                            out strError);
                        if (nRet == -1)
                        {
                            strBrowseText = strError;
                            goto DOADD;
                        }

                        if (strNameSpaceUri == Ns.usmarcxml)
                        {
                            string strOutMarcSyntax = "";
                            // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
                            // parameters:
                            //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
                            //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
                            //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
                            nRet = MarcUtil.Xml2Marc(strContent,
                                true,
                                "usmarc",
                                out strOutMarcSyntax,
                                out strMARC,
                                out strError);
                            if (nRet == -1)
                            {
                                strBrowseText = strError;
                                goto DOADD;
                            }

                            strMarcSyntaxOID = "1.2.840.10003.5.10";

                            nRet = GetBrowseText(
                                strMarcSyntaxOID,
                                strMARC,
                                out strBrowseText,
                                out strError);
                            if (nRet == -1)
                                strBrowseText = strError;

                            goto DOADD;

                        }

                        cols = new string[1];
                        cols[0] = strContent;
                        goto DOADDCOLS;
                    }

                    strMarcSyntaxOID = record.m_strMarcSyntaxOID;

                    // ISO2709ת��Ϊ���ڸ�ʽ
                    nRet = Marc8Encoding.ConvertByteArrayToMarcRecord(
                        baRecord,
                        connection.GetRecordsEncoding(this.MainForm, strMarcSyntaxOID),  // Encoding.GetEncoding(936),
                        true,
                        out strMARC,
                        out strError);
                    if (nRet < 0)
                    {
                        strBrowseText = strError;
                        goto DOADD;
                    }


                    if (connection.TargetInfo.DetectMarcSyntax == true)
                    {
                        // ̽��MARC��¼�����ĸ�ʽ��
                        // return:
                        //		-1	�޷�̽��
                        //		1	UNIMARC	���򣺰���200�ֶ�
                        //		10	USMARC	���򣺰���008�ֶ�(innopac��UNIMARC��ʽҲ��һ����ֵ�008)
                        nRet = DetectMARCSyntax(strMARC);
                        if (nRet == 1)
                            strMarcSyntaxOID = "1.2.840.10003.5.1";
                        else if (nRet == 10)
                            strMarcSyntaxOID = "1.2.840.10003.5.10";

                        // ���Զ�ʶ��Ľ����������
                        record.AutoDetectedMarcSyntaxOID = strMarcSyntaxOID;
                    }


                    nRet = GetBrowseText(
                        strMarcSyntaxOID,
                        strMARC,
                        out strBrowseText,
                        out strError);
                    if (nRet == -1)
                        strBrowseText = strError;


                }

            DOADD:
                cols = strBrowseText.Split(new char[] { '\t' });
            DOADDCOLS:
                for (int j = 0; j < cols.Length; j++)
                {
                    item.SubItems.Add(cols[j]);
                }

                item.Tag = record;
                this.listView_browse.Items.Add(item);
            }
            return 0;
        }
#endif

        // ����ztargetcontrol��popup�˵�
        private void zTargetControl1_OnSetMenu(object sender, DigitalPlatform.GUI.GuiAppendMenuEventArgs e)
        {
            ContextMenuStrip contextMenu = e.ContextMenuStrip;

            ToolStripMenuItem menuItem = null;
            // ToolStripMenuItem subMenuItem = null;

            TreeNode node = this.zTargetControl1.SelectedNode;


            // --
            contextMenu.Items.Add(new ToolStripSeparator());

            // Z39.50��ʼ��
            menuItem = new ToolStripMenuItem("��ʼ������(&I)");
            if (node == null
                || (node != null && ZTargetControl.IsServerType(node) == false)
                || (ZTargetControl.IsServerOnlineType(node) == true))
                menuItem.Enabled = false;
            menuItem.Click += new EventHandler(menuItem_initialZAssociation_Click);
            contextMenu.Items.Add(menuItem);


            // �ж�����
            menuItem = new ToolStripMenuItem("�Ͽ�����(&C)");
            if (node == null
                || (node != null && ZTargetControl.IsServerType(node) == false)
                || (ZTargetControl.IsServerOfflineType(node) == true))
                menuItem.Enabled = false;
            menuItem.Click += new EventHandler(menuItem_closeZAssociation_Click);
            contextMenu.Items.Add(menuItem);

            // --
            contextMenu.Items.Add(new ToolStripSeparator());

            // ����ԭʼ��
            menuItem = new ToolStripMenuItem("����ԭʼ��(&S)");
            menuItem.Click += new EventHandler(menuItem_sendOriginPackage_Click);
            contextMenu.Items.Add(menuItem);

        }


        // ����ԭʼ���������á�
        void menuItem_sendOriginPackage_Click(object sender,
            EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            SelectLogRecordDlg dlg = new SelectLogRecordDlg();
            GuiUtil.SetControlFont(dlg, this.Font);

            dlg.Filename = this.UsedLogFilename;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.UsedLogFilename = dlg.Filename;

            ZConnection connection = this.GetCurrentZConnection();
            Debug.Assert(connection.TargetInfo != null, "");

            /*
            connection.QueryString = "";
            connection.QueryXml = this.queryControl1.GetContent(true);
            connection.ClearResultInfo();
             * */

            EnableQueryControl(false);

            connection.Stop.OnStop += new StopEventHandler(this.DoStop);
            connection.Stop.SetMessage("׼������ԭʼ�� ...");
            connection.Stop.BeginLoop();

            this.Update();
            this.m_mainForm.Update();


            try
            {
                connection.Stop.SetMessage("�������� " + connection.TargetInfo.HostName + " : " + connection.TargetInfo.Port.ToString() + " ...");

                if (connection.ZChannel.Connected == false
                    || connection.ZChannel.HostName != connection.TargetInfo.HostName
                    || connection.ZChannel.Port != connection.TargetInfo.Port)
                {
                    nRet = connection.ZChannel.NewConnectSocket(connection.TargetInfo.HostName,
                        connection.TargetInfo.Port,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    connection.Stop.SetMessage("���ڷ���ԭʼ�� ...");

                    connection.TargetInfo.OnlineServerIcon(true);

                    // 
                    nRet = DoSendOriginPackage(
                        connection,
                        dlg.Package,
                        out strError);
                    if (nRet == -1)
                    {
                        connection.TargetInfo.OnlineServerIcon(false);
                        goto ERROR1;
                    }

                }

            }
            finally
            {
                try
                {
                    connection.Stop.EndLoop();
                    connection.Stop.OnStop -= new StopEventHandler(this.DoStop);
                    connection.Stop.Initial("");

                    EnableQueryControl(true);
                }
                catch { }
            }

            MessageBox.Show(this, "��Է����� " + connection.TargetInfo.Name + " ("
                + connection.TargetInfo.HostNameAndPort
                + ") ����ԭʼ���ɹ���");
            this.queryControl1.Focus();

            return;
        ERROR1:
            try // ��ֹ����˳�ʱ����
            {
                MessageBox.Show(this, strError);
                this.queryControl1.Focus();
            }
            catch
            {
            }
            return;

        }

        // ��ʼ��Z����
        void menuItem_initialZAssociation_Click(object sender,
            EventArgs e)
        {
            string strError = "";
            int nRet = 0;
            string strInitialResultInfo = "";

            ZConnection connection = this.GetCurrentZConnection();
            Debug.Assert(connection.TargetInfo != null, "");

            /*
            if (connection.TargetInfo == null)
            {

                TargetInfo targetinfo = null;
                nRet = this.zTargetControl1.GetCurrentTarget(
                    out targetinfo,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                connection.TargetInfo = targetinfo;
            }*/


            connection.QueryString = "";
            connection.QueryXml = this.queryControl1.GetContent(true);


            connection.ClearResultInfo();

            // ZConnection connection = this.GetCurrentZConnection();

            //EnableControls(false);
            EnableQueryControl(false);

            connection.Stop.OnStop += new StopEventHandler(this.DoStop);
            connection.Stop.SetMessage("��ʼ��ʼ������ ...");
            connection.Stop.BeginLoop();

            this.Update();
            this.m_mainForm.Update();


            try
            {
                connection.Stop.SetMessage("�������� " + connection.TargetInfo.HostName + " : " + connection.TargetInfo.Port.ToString() + " ...");

                if (connection.ZChannel.Connected == false
                    || connection.ZChannel.HostName != connection.TargetInfo.HostName
                    || connection.ZChannel.Port != connection.TargetInfo.Port)
                {
                    nRet = connection.ZChannel.NewConnectSocket(connection.TargetInfo.HostName,
                        connection.TargetInfo.Port,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    connection.Stop.SetMessage("����ִ��Z39.50��ʼ�� ...");

                    connection.TargetInfo.OnlineServerIcon(true);

                    // Initial
                    nRet = DoInitial(
                        connection,
                        // this.CurrentTargetInfo,
                        out strInitialResultInfo,
                        out strError);
                    if (nRet == -1)
                    {
                        connection.TargetInfo.OnlineServerIcon(false);
                        goto ERROR1;
                    }

                    // ���õ�ǰ�����Ѿ�ѡ��Ľڵ����չ��Ϣ
                    nRet = ZTargetControl.SetCurrentTargetExtraInfo(
                        this.zTargetControl1.SelectedNode,
                        strInitialResultInfo,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;


                }

            }
            finally
            {
                try
                {
                    connection.Stop.EndLoop();
                    connection.Stop.OnStop -= new StopEventHandler(this.DoStop);
                    connection.Stop.Initial("");

                    // EnableControls(true);
                    EnableQueryControl(true);
                }
                catch { }
            }

            MessageBox.Show(this, "��Է����� " + connection.TargetInfo.Name + " ("
                + connection.TargetInfo.HostNameAndPort
                + ") ��ʼ�����ӳɹ���\r\n\r\n��ʼ����Ϣ:\r\n"
                + strInitialResultInfo);

            this.queryControl1.Focus();

            return;
        ERROR1:
            try // ��ֹ����˳�ʱ����
            {
                MessageBox.Show(this, strError);
                this.queryControl1.Focus();
            }
            catch
            {
            }
            return;
        }

        // �ж�Z����
        void menuItem_closeZAssociation_Click(object sender,
            EventArgs e)
        {
            // string strError = "";
//             int nRet = 0;

            ZConnection connection = this.GetCurrentZConnection();
            Debug.Assert(connection.TargetInfo != null, "");

            /*
            if (connection.TargetInfo == null)
            {
                TargetInfo targetinfo = null;
                nRet = this.zTargetControl1.GetCurrentTarget(
                    out targetinfo,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                connection.TargetInfo = targetinfo;
            }*/


            // EnableControls(false);
            EnableQueryControl(false);

            connection.Stop.OnStop += new StopEventHandler(this.DoStop);
            connection.Stop.SetMessage("�����ж����� ...");
            connection.Stop.BeginLoop();

            this.Update();
            this.m_mainForm.Update();
            try
            {

                connection.Stop.SetMessage("�����ж����� " + connection.TargetInfo.HostName + " : " + connection.TargetInfo.Port.ToString() + " ...");

                if (connection.ZChannel.Connected == true
                    && connection.ZChannel.HostName == connection.TargetInfo.HostName
                    && connection.ZChannel.Port == connection.TargetInfo.Port)
                {
                    connection.CloseConnection();
                }


            }
            finally
            {
                try
                {
                    connection.Stop.EndLoop();
                    connection.Stop.OnStop -= new StopEventHandler(this.DoStop);
                    connection.Stop.Initial("");

                    // EnableControls(true);
                    EnableQueryControl(true);
                }
                catch { }
            }

            return;

            /*
        ERROR1:
            try // ��ֹ����˳�ʱ����
            {
                MessageBox.Show(this, strError);
                this.queryControl1.Focus();
            }
            catch
            {
            }
            return;
            */
        }

        private void listView_browse_RetrieveVirtualItem(object sender,
            RetrieveVirtualItemEventArgs e)
        {
            /*
            e.Item = new ListViewItem(e.ItemIndex.ToString());
            for (int i = 0; i < this.listView_browse.Columns.Count - 1; i++)
            {
                e.Item.SubItems.Add("");
            }*/
            e.Item = this.CurrentBrowseItems[e.ItemIndex].GetListViewItem(this.listView_browse.Columns.Count);
        }

        private void listView_browse_VirtualItemsSelectionRangeChanged(object sender,
            ListViewVirtualItemsSelectionRangeChangedEventArgs e)
        {
            ZConnection connection = this.GetCurrentZConnection();
            if (connection != null)
                SaveListViewSelectedToVirtual(connection);

            // ��Ҫ����SelectedChanged��Ϣ�����򣬺����ı���Ϣ�������ʹ�˵��͹�������ť״̬�����仯
            listView_browse_SelectedIndexChanged(null, null);

#if NOOOOOOOOOOOOOOOO

            if (e.IsSelected == false)
            {
                /*
                if (e.StartIndex == 0
                    && e.EndIndex == 0)
                {
                    Debug.Assert(this.listView_browse.VirtualListSize
                    == this.CurrentBrowseItems.Count, "");
                    for (int i = 0; i < this.listView_browse.VirtualListSize; i++)
                        this.CurrentBrowseItems[i].Selected = false;
                }
                else
                {*/
                    for (int i = e.StartIndex; i <= e.EndIndex; i++)
                        this.CurrentBrowseItems[i].Selected = false;
                // }
            }
            else
            {
                /*
                if (e.StartIndex == 0
                    && e.EndIndex == 0)
                {
                    Debug.Assert(this.listView_browse.VirtualListSize
                    == this.CurrentBrowseItems.Count, "");

                    for (int i = 0; i < this.listView_browse.VirtualListSize; i++)
                        this.CurrentBrowseItems[i].Selected = true;
                }
                else
                {*/
                    for (int i = e.StartIndex; i <= e.EndIndex; i++)
                        this.CurrentBrowseItems[i].Selected = true;
                // }
            }
#endif
        }

        private void listView_browse_ItemSelectionChanged(object sender, 
            ListViewItemSelectionChangedEventArgs e)
        {
            // this.CurrentBrowseItems[e.ItemIndex].Selected = e.IsSelected;
        }

        private void listView_browse_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 2011/9/10
            if (this.CurrentBrowseItems != null)
                this.CurrentBrowseItems.ExpireSelectedIndices();


            ZConnection connection = this.GetCurrentZConnection();
            if (connection != null
                && !(sender == null && e == null))  // �ų�listView_browse_VirtualItemsSelectionRangeChanged()����ר��ת�����ĵ���
                SaveListViewSelectedToVirtual(connection);

            int nSelectedCount = GetBrowseListViewSelectedItemCount();

            // �˵���̬�仯
            if (nSelectedCount == 0)
            {
                m_mainForm.toolButton_saveTo.Enabled = false;
                m_mainForm.MenuItem_saveOriginRecordToIso2709.Enabled = false;
                m_mainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = false;
                m_mainForm.toolButton_loadFullRecord.Enabled = false;
            }
            else
            {
                m_mainForm.toolButton_saveTo.Enabled = true;
                m_mainForm.MenuItem_saveOriginRecordToIso2709.Enabled = true;
                m_mainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = true;
                m_mainForm.toolButton_loadFullRecord.Enabled = true;
            }

        }

        // ��������������������
        private void zTargetControl1_OnServerChanged(object sender, ServerChangedEventArgs e)
        {
            ZConnection connection = this.ZConnections.GetZConnection(e.TreeNode);
            if (connection == null)
                return;

            connection.TargetInfo = null;
        }

        private void comboBox_recordSyntax_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_recordSyntax.Invalidate();
        }

        private void comboBox_elementSetName_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_elementSetName.Invalidate();
        }

        /*
        void CloseConnection()
        {
            this.ZChannel.CloseSocket();
            this.ZChannel.Initialized = false;  // ��ʹ���³�ʼ��
            if (this.CurrentTargetInfo != null)
                this.CurrentTargetInfo.OfflineServerIcon();
            // ���õ�ǰ�����Ѿ�ѡ��Ľڵ����չ��Ϣ
            string strError = "";
            int nRet = this.zTargetControl1.SetCurrentTargetExtraInfo(
                "", // strInitialResultInfo,
                out strError);
        }*/

    }


}